using System.Collections.Generic;
using PoliSim.Data;
using PoliSim.Simulation;
using UnityEngine;

namespace PoliSim.UI
{
    /// <summary>
    /// First playable dashboard: an immediate-mode (OnGUI) UI for the player's country (USA,
    /// hardcoded for now) to review its EconomyState, set this turn's PolicyDecision, and advance
    /// the turn. Functional, not styled - validates the core play loop before any visual polish.
    /// Fills the game window and rescales its layout/fonts each frame from Screen.width/height so it
    /// stays usable at any resolution rather than assuming a fixed window size.
    /// </summary>
    public class GameController : MonoBehaviour
    {
        private enum RightPanelTab
        {
            RecentTurns,
            WorldMap,
            Trade,
            TaxPolicy,
            SpendingPolicy,
            FederalReserve,
            WelfarePolicy,
            LaborMarket,
            CrimeJustice,
            SectorPolicy,
            Infrastructure,
            SwfPolicy,
            PolicyWeb,
            Cabinet,
            CompassAndDemographics,
            ForeignPolicy,
            Parliament
        }

        // Country-selection task, Part 1: PlayerCountryId is no longer a compile-time constant - the
        // player picks their country on a selector screen shown before the dashboard (see
        // DrawCountrySelector/SelectPlayerCountry). Kept as a property with the SAME name every
        // existing call site already used, so none of them needed to change - only the declaration
        // itself did. _selectedPlayerCountryId is the actual source of truth for "has selection
        // happened yet" (OnGUI gates the whole dashboard behind it); the property's fallback to USA
        // is never actually observed by anything, since nothing that reads PlayerCountryId runs
        // before selection.
        private CountryId? _selectedPlayerCountryId;
        private CountryId PlayerCountryId => _selectedPlayerCountryId ?? CountryId.USA;

        private const int MaxLogEntries = 10;

        /// <summary>This-turn PERCENTAGE-change slider range for a Discretionary SpendingLine - the same range for every category, but a percentage of that line's OWN Amount, so a $1B SBA line and an $850B Defense line move by proportionally (not identically) different dollar amounts. Must match SimulationManager.DiscretionaryPercentChangeRange.</summary>
        private const float DiscretionaryPercentChangeRange = 30f;

        /// <summary>Narrower than DiscretionaryPercentChangeRange - reflects the real political difficulty of entitlement reform. Must match SimulationManager.MandatoryPercentChangeRange.</summary>
        private const float MandatoryPercentChangeRange = 15f;
        private const float TariffRateChangeRange = 5f;
        private const float InterestRateChangeRange = 2f;

        /// <summary>Bounds for the Minimum Wage slider (percent of median wage) - must match SimulationManager.MinMinimumWagePercent/MaxMinimumWagePercent.</summary>
        private const float MinMinimumWagePercent = 0f;
        private const float MaxMinimumWagePercent = 100f;

        /// <summary>Bounds for the Police Funding / Sentencing Severity sliders - must match SimulationManager.MinPolicyDialLevel/MaxPolicyDialLevel.</summary>
        private const float MinPolicyDialLevel = 0f;
        private const float MaxPolicyDialLevel = 100f;

        /// <summary>Bounds for the SWF Contribution/Withdrawal Rate slider - must match SimulationManager.MinSwfContributionRate/MaxSwfContributionRate. Negative values withdraw from the fund (Round 3 item 1, the SWF drawdown mechanic) rather than contribute to it.</summary>
        private const float MinSwfContributionRate = -10f;
        private const float MaxSwfContributionRate = 10f;

        /// <summary>Bounds for the Paid Family Leave slider (weeks) - must match SimulationManager.MinPaidFamilyLeaveWeeks/MaxPaidFamilyLeaveWeeks.</summary>
        private const float MinPaidFamilyLeaveWeeks = 0f;
        private const float MaxPaidFamilyLeaveWeeks = 104f;

        /// <summary>Bounds for the Overtime Regulation / Retraining Program sliders - must match SimulationManager.MinLaborDialLevel/MaxLaborDialLevel.</summary>
        private const float MinLaborDialLevel = 0f;
        private const float MaxLaborDialLevel = 100f;

        /// <summary>Bounds for a per-partner tariff override slider - the same [0,50] range BaseTariffRate itself uses. Must match SimulationManager's MinBaseTariffRate/MaxBaseTariffRate.</summary>
        private const float PartnerTariffOverrideMin = 0f;
        private const float PartnerTariffOverrideMax = 50f;

        // Cosmetic-only margin of error applied to the live policy preview - display layer, never
        // touches the actual PolicyPreview figures the preview math produces.
        private const float MinPreviewMarginPercent = 5f;
        private const float MaxPreviewMarginPercent = 10f;

        // Layout is expressed as fractions of Screen.width/height, not fixed pixel values, so it
        // scales at any window size instead of sitting in a small fixed-size corner box.
        private const float ScreenMarginFraction = 0.02f;
        private const float ColumnSpacingFraction = 0.02f;
        private const float LeftColumnWidthFraction = 0.45f;
        private const float SectionSpacingFraction = 0.03f;

        /// <summary>Vertical gap between the right column's two tab-button rows (see DrawRightColumnTabs) - 11 tabs no longer fit legibly in one row.</summary>
        private const float TabRowSpacing = 4f;

        /// <summary>Fixed display height (px) for the World Map tab's map rect - the map itself stretches to whatever width the tab gives it (see MapRenderer.Draw's ScaleMode.StretchToFill), so only the height needs pinning to keep its aspect roughly sane.</summary>
        private const float WorldMapHeight = 260f;

        private World _world;
        private SimulationManager _simulationManager;
        private Country _playerCountry;

        private float _prevGdp;
        private float _lastGrowthPercent;

        // Draft ABSOLUTE rate per TaxType (not a delta) for the Tax Policy tab's sliders - defaults
        // to that TaxLine's persisted Rate until the player drags it (see GetTaxRateInput). Not
        // cleared by ResetPolicyInputs after Advance Turn: once committed, TaxLine.Rate already
        // equals whatever was in here, so the slider keeps showing the same (now-persisted) value.
        private readonly Dictionary<TaxType, float> _taxRateInputs = new Dictionary<TaxType, float>();

        // Political Systems Overhaul Part B PILOT (Master Sequence step 4): draft Implement/Remove
        // state per TaxType - defaults to that TaxLine's persisted (standing) IsImplemented until the
        // player toggles it (see GetTaxImplementDraft). Unlike the pre-Parliament version of this tab,
        // toggling this is NO LONGER immediate - it only ever reaches TaxLine.IsImplemented via a
        // PASSED TaxBill (see BuildTaxBillFromDrafts/DrawTaxPolicy's Introduce Bill button). Not
        // cleared by ResetPolicyInputs, for the same reason _taxRateInputs isn't - once a bill passes,
        // TaxLine.IsImplemented already equals whatever was in here.
        private readonly Dictionary<TaxType, bool> _taxImplementDrafts = new Dictionary<TaxType, bool>();

        private Vector2 _parliamentScrollPosition;

        // Draft ABSOLUTE GenerosityLevel per WelfareProgramType (not a delta) for the Welfare Policy
        // tab's sliders - defaults to that WelfareProgram's persisted GenerosityLevel until the player
        // drags it (see GetWelfareGenerosityInput). Not cleared by ResetPolicyInputs, for the exact
        // same reason _taxRateInputs isn't.
        private readonly Dictionary<WelfareProgramType, float> _welfareGenerosityInputs = new Dictionary<WelfareProgramType, float>();

        // Draft ABSOLUTE Subsidy/Regulation levels per SectorType (not deltas) for the Economic
        // Sectors tab's sliders - every country has all four Sectors (unlike TaxLines/
        // WelfarePrograms), so no implement/remove fallback branch is needed. Not cleared by
        // ResetPolicyInputs, for the same reason _taxRateInputs isn't.
        private readonly Dictionary<SectorType, float> _sectorSubsidyInputs = new Dictionary<SectorType, float>();
        private readonly Dictionary<SectorType, float> _sectorRegulationInputs = new Dictionary<SectorType, float>();
        private readonly Dictionary<SectorType, float> _sectorTaxCreditInputs = new Dictionary<SectorType, float>();
        private readonly Dictionary<SectorType, float> _sectorResearchGrantsInputs = new Dictionary<SectorType, float>();
        private readonly Dictionary<SectorType, float> _sectorDeregulationInputs = new Dictionary<SectorType, float>();

        // Draft ABSOLUTE Sovereign Wealth Fund settings (not deltas) - only meaningful while
        // _playerCountry.SovereignWealthFund is non-null (Create/Dissolve is a separate, immediate
        // action, mirroring TaxLine.IsImplemented). Not cleared by ResetPolicyInputs, for the same
        // reason _minimumWageInput isn't.
        private float? _swfContributionRateInput;
        private float? _swfDomesticAllocationInput;
        private float? _swfEquitiesWeightInput;
        private float? _swfBondsWeightInput;
        private float? _swfInfrastructureWeightInput;
        private float? _swfRealEstateWeightInput;

        // Draft PERCENTAGE change per SpendingCategory (both Mandatory and Discretionary, each
        // clamped to its own range in SimulationManager) - unlike _taxRateInputs, this IS cleared by
        // ResetPolicyInputs each turn, since SpendingLine.Amount itself is what persists, not this
        // draft.
        private readonly Dictionary<SpendingCategory, float> _spendingLineInputs = new Dictionary<SpendingCategory, float>();
        private float _interestRateChangeInput;
        private float _tariffRateChangeInput;

        // Draft ABSOLUTE minimum-wage level (percent of median wage, not a delta) - defaults to
        // Country.MinimumWagePercentOfMedian until the player drags it (see GetMinimumWageInput).
        // Nullable rather than a Dictionary since there's only one lever (unlike _taxRateInputs).
        // Not cleared by ResetPolicyInputs, for the same reason _taxRateInputs isn't - once committed,
        // MinimumWagePercentOfMedian already equals whatever was in here.
        private float? _minimumWageInput;

        // Draft ABSOLUTE Paid Family Leave (weeks) / Overtime Regulation / Retraining Program levels
        // (not deltas) - every country has all three (unlike MinimumWage's country-specific
        // asymmetry). Not cleared by ResetPolicyInputs, for the same reason _minimumWageInput isn't.
        private float? _paidFamilyLeaveWeeksInput;
        private float? _overtimeRegulationInput;
        private float? _retrainingProgramInput;

        // Draft ABSOLUTE Police Funding / Sentencing Severity levels (0-100, not deltas) - every
        // country has both dials (unlike minimum wage's country-specific asymmetry), so no fallback-
        // to-"not implemented" branch is needed. Not cleared by ResetPolicyInputs, for the same reason
        // _minimumWageInput isn't.
        private float? _policeFundingInput;
        private float? _sentencingSeverityInput;
        private float? _bailReformInput;
        private float? _drugPolicyInput;
        private float? _judicialFundingInput;
        private float? _borderEnforcementInput;

        // Round 3 item 5, Part B: same "draft slider value, no lag" idiom as every dial above.
        private float? _familyPolicyInput;
        private float? _immigrationPolicyInput;

        // Draft ABSOLUTE per-partner tariff override rate for the Trade tab's sliders (only shown/
        // meaningful while that partner's TradePartner.HasPlayerTariffOverride is true - mirrors
        // _taxRateInputs' relationship to TaxLine.IsImplemented exactly). Not cleared by
        // ResetPolicyInputs after Advance Turn, for the same reason _taxRateInputs isn't - once
        // committed, TradePartner.PlayerTariffOverride already equals whatever was in here.
        private readonly Dictionary<CountryId, float> _partnerTariffInputs = new Dictionary<CountryId, float>();

        private bool _isGameOver;
        private ElectionResult _pendingElectionResult;
        private int _pendingElectionTurn;
        private string _gameOverReason;

        /// <summary>
        /// Continuous Time Migration Phase 0 (Master Sequence step 3): replaces the manual "Advance
        /// Turn" button. Real in-game days pass automatically while unpaused, driven by Update (not
        /// OnGUI, which only runs on repaint) - Paused stops the calendar entirely (the same effect
        /// the old disabled Advance Turn button had while a Fed Chair/Cabinet decision was pending,
        /// now generalized to "time itself doesn't advance" rather than "one button doesn't work").
        /// </summary>
        private enum GameSpeed { Paused, Normal, Fast, VeryFast }
        private GameSpeed _gameSpeed = GameSpeed.Normal;

        /// <summary>Real seconds accumulated toward the next in-game day at the current speed - see Update.</summary>
        private float _daySpeedTimer;

        /// <summary>Real seconds per in-game day per speed setting - a first-pass placeholder pacing choice (2 real minutes per 121-day turn at 1x, ~30 real seconds at 3x), not deeply playtested, same "starting point meant to be tuned by playtesting" caveat every other constant in this project carries. Not itself part of the Continuous Time Migration's own economic-constant translation - this is pure UI pacing, never read by any simulation formula.</summary>
        private static float GetSecondsPerDay(GameSpeed speed)
        {
            switch (speed)
            {
                case GameSpeed.Normal: return 1f;
                case GameSpeed.Fast: return 0.5f;
                case GameSpeed.VeryFast: return 0.25f;
                default: return 1f;
            }
        }

        // Federal Reserve (USA only - see CLAUDE.md's "Federal Reserve" section). _fedChairCandidates
        // is non-null exactly while the player must pick a new chair before Advance Turn can proceed;
        // _fedChairCandidatesForTurn records which upcoming turn they were generated for, so a pick
        // already resolved for that turn doesn't immediately regenerate a fresh set next frame (the
        // condition that triggers generation - "next turn is an election turn" - stays true for the
        // rest of that same frame/turn window).
        private List<FedChair> _fedChairCandidates;
        private int _fedChairCandidatesForTurn = -1;

        // Isolated from UnityEngine.Random and from EventSystem's own System.Random - this instance
        // exists purely to jitter the preview's displayed margin of error, so drawing the preview
        // (or dragging sliders to recompute it) can never perturb the event roll or any other RNG
        // consumer's sequence.
        private System.Random _previewRandom;

        // Cached preview text plus the slider/turn snapshot it was computed from - PreviewTurn and
        // the margin roll only re-run when the draft PolicyDecision or the turn number actually
        // changed since last frame, so the displayed numbers read as one stable forecast rather than
        // flickering every OnGUI call even while the player isn't touching anything. Implementing/
        // removing a tax bypasses this cache entirely (see DrawTaxLineRow) since that's an immediate
        // action, not a draft value tracked here.
        private bool _hasCachedPreview;
        private int _cachedPreviewTurn = -1;
        private readonly Dictionary<TaxType, float> _cachedTaxRateInputs = new Dictionary<TaxType, float>();
        private readonly Dictionary<WelfareProgramType, float> _cachedWelfareGenerosityInputs = new Dictionary<WelfareProgramType, float>();
        private readonly Dictionary<SectorType, float> _cachedSectorSubsidyInputs = new Dictionary<SectorType, float>();
        private readonly Dictionary<SectorType, float> _cachedSectorRegulationInputs = new Dictionary<SectorType, float>();
        private readonly Dictionary<SectorType, float> _cachedSectorTaxCreditInputs = new Dictionary<SectorType, float>();
        private readonly Dictionary<SectorType, float> _cachedSectorResearchGrantsInputs = new Dictionary<SectorType, float>();
        private readonly Dictionary<SectorType, float> _cachedSectorDeregulationInputs = new Dictionary<SectorType, float>();
        private float? _cachedSwfContributionRateInput;
        private float? _cachedSwfDomesticAllocationInput;
        private float? _cachedSwfEquitiesWeightInput;
        private float? _cachedSwfBondsWeightInput;
        private float? _cachedSwfInfrastructureWeightInput;
        private float? _cachedSwfRealEstateWeightInput;
        private readonly Dictionary<SpendingCategory, float> _cachedSpendingLineInputs = new Dictionary<SpendingCategory, float>();
        private readonly Dictionary<CountryId, float> _cachedPartnerTariffInputs = new Dictionary<CountryId, float>();
        private float _cachedInterestRateChangeInput;
        private float _cachedTariffRateChangeInput;
        private float? _cachedMinimumWageInput;
        private float? _cachedPaidFamilyLeaveWeeksInput;
        private float? _cachedOvertimeRegulationInput;
        private float? _cachedRetrainingProgramInput;
        private float? _cachedPoliceFundingInput;
        private float? _cachedSentencingSeverityInput;
        private float? _cachedBailReformInput;
        private float? _cachedDrugPolicyInput;
        private float? _cachedJudicialFundingInput;
        private float? _cachedBorderEnforcementInput;
        private float? _cachedFamilyPolicyInput;
        private float? _cachedImmigrationPolicyInput;

        // Raw (unformatted, no cosmetic margin) numeric counterparts of every preview figure -
        // FormatEstimate's margin is a display-only flourish that has no business perturbing what a
        // graph draws or which way UiPalette.GetDeltaColor colors a line. The first three exist for
        // the Phase 2 dashboard graphs' projected-point calculation; all eight are used by Phase 3's
        // policy-preview coloring. Set alongside the formatted text fields in RecomputePolicyPreview.
        private float _cachedGdpGrowthPercentRaw;
        private float _cachedUnemploymentChangeRaw;
        private float _cachedApprovalChangeRaw;
        private float _cachedInflationChangeRaw;
        private float _cachedPovertyRateChangeRaw;
        private float _cachedLaborForceParticipationRateChangeRaw;
        private float _cachedCrimeIndexChangeRaw;
        private float _cachedNetBudgetImpactRaw;
        private float _cachedSwfReturnsEstimateRaw;

        /// <summary>
        /// Continuous Time Migration Phase 0: which horizon the live Policy Preview currently shows -
        /// "effect-per-day plus a selectable-horizon projection," per the Master Roadmap's own Part
        /// One spec. Defaults to OneDay (the "per-day" figure front and center); Week/Month/FullTurn
        /// are the "selectable" part. This is a DISPLAY-ONLY re-scaling of the SAME full-turn
        /// PreviewTurn output every horizon shares - Phase 0 doesn't simulate sub-turn granularity
        /// (that's Phases 1-5), so there is no more "real" per-day number to show than this.
        /// </summary>
        private enum PreviewHorizon { OneDay, OneWeek, OneMonth, FullTurn }
        private PreviewHorizon _previewHorizon = PreviewHorizon.OneDay;
        private int _cachedPreviewHorizonDays = -1;

        private static int GetHorizonDays(PreviewHorizon horizon)
        {
            switch (horizon)
            {
                case PreviewHorizon.OneWeek: return 7;
                case PreviewHorizon.OneMonth: return 30;
                case PreviewHorizon.FullTurn: return SimulationManager.DaysPerTurn;
                default: return 1;
            }
        }

        private static string GetHorizonLabel(PreviewHorizon horizon)
        {
            switch (horizon)
            {
                case PreviewHorizon.OneWeek: return "1 Week";
                case PreviewHorizon.OneMonth: return "1 Month";
                case PreviewHorizon.FullTurn: return $"Full Turn ({SimulationManager.DaysPerTurn} days)";
                default: return "1 Day";
            }
        }

        /// <summary>
        /// Scales a full-turn (121-day) ADDITIVE/linear estimate (a "points changed" or dollar-amount
        /// figure - Unemployment/Inflation/Approval/PovertyRate/LaborForceParticipation/CrimeIndex/
        /// NetBudgetImpact) down to a shorter display horizon by simple proportion - a display-only
        /// approximation, not a new simulation (Phase 0 doesn't compute genuine sub-turn values yet).
        /// Matches the "linear/additive rates" category POLISIM_MASTER_ROADMAP.md's own translation
        /// methodology describes, applied here purely for display rather than to a real constant.
        /// </summary>
        private static float ScaleLinearForDisplay(float fullTurnValue, int horizonDays)
        {
            return fullTurnValue * horizonDays / SimulationManager.DaysPerTurn;
        }

        /// <summary>
        /// Same display-only horizon scaling as ScaleLinearForDisplay, but geometric/compounding -
        /// the correct shape for a percentage GROWTH rate (GDP), matching the SAME "identify which
        /// mathematical shape a constant is" distinction the translation methodology draws between
        /// additive and compounding rates, applied here for display only.
        /// </summary>
        private static float ScaleCompoundingForDisplay(float fullTurnGrowthPercent, int horizonDays)
        {
            float fullTurnMultiplier = 1f + fullTurnGrowthPercent / 100f;
            if (fullTurnMultiplier <= 0f)
            {
                return ScaleLinearForDisplay(fullTurnGrowthPercent, horizonDays);
            }
            float dailyMultiplier = Mathf.Pow(fullTurnMultiplier, 1f / SimulationManager.DaysPerTurn);
            float horizonMultiplier = Mathf.Pow(dailyMultiplier, horizonDays);
            return (horizonMultiplier - 1f) * 100f;
        }

        // Horizon-scaled counterparts of the Raw fields above, recomputed alongside them in
        // RecomputePolicyPreview whenever the horizon selection itself changes (see
        // PolicyInputsChangedSinceLastPreview) - kept SEPARATE from the Raw fields since those still
        // need to stay full-turn (DrawHeadlineGraphs' next-turn dashed projection genuinely means
        // "next turn," not "next day," regardless of what horizon the preview text panel shows).
        private string _cachedGdpGrowthScaledText;
        private string _cachedUnemploymentScaledText;
        private string _cachedInflationScaledText;
        private string _cachedApprovalScaledText;
        private string _cachedPovertyRateScaledText;
        private string _cachedLaborForceParticipationRateScaledText;
        private string _cachedCrimeIndexScaledText;
        private string _cachedNetBudgetScaledText;
        private float _cachedGdpGrowthPercentScaled;
        private float _cachedUnemploymentChangeScaled;
        private float _cachedApprovalChangeScaled;
        private float _cachedInflationChangeScaled;
        private float _cachedPovertyRateChangeScaled;
        private float _cachedLaborForceParticipationRateChangeScaled;
        private float _cachedCrimeIndexChangeScaled;
        private float _cachedNetBudgetImpactScaled;

        // One GraphRenderer per headline dashboard stat - see GraphRenderer.cs. Each auto-scales its
        // own Y-axis, so instances are never shared across stats with different natural ranges.
        private readonly GraphRenderer _gdpGraph = new GraphRenderer();
        private readonly GraphRenderer _unemploymentGraph = new GraphRenderer();
        private readonly GraphRenderer _approvalGraph = new GraphRenderer();

        // Phase 4's per-tab graph rollout - one GraphRenderer per newly-homed stat, same "never
        // shared across stats" reasoning as the three headline instances above.
        private readonly GraphRenderer _interestRateGraph = new GraphRenderer();
        private readonly GraphRenderer _crimeIndexGraph = new GraphRenderer();
        private readonly GraphRenderer _prisonPopulationGraph = new GraphRenderer();
        private readonly GraphRenderer _organizedCrimeGraph = new GraphRenderer();
        private readonly GraphRenderer _corruptionGraph = new GraphRenderer();
        private readonly GraphRenderer _laborForceParticipationGraph = new GraphRenderer();
        private readonly GraphRenderer _tradeBalanceGraph = new GraphRenderer();
        private readonly GraphRenderer _debtToGdpGraph = new GraphRenderer();
        private readonly GraphRenderer _povertyRateGraph = new GraphRenderer();

        // Phase 5 of the UI revamp: the World Map tab. Event markers are tracked here (not in
        // SimulationManager, which only ever exposes the CURRENT turn's event via GetLastEvent) so a
        // fired event's map dot can fade out over several turns instead of vanishing the instant the
        // next turn advances - see AdvanceTurn for where this list is appended to and pruned.
        private const int EventMarkerFadeTurns = 6;
        private readonly MapRenderer _mapRenderer = new MapRenderer();
        private readonly PolicyWebRenderer _policyWebRenderer = new PolicyWebRenderer();

        // Political Systems Overhaul Part C (UI/graph restyling and political visualization).
        private readonly PoliticalCompassRenderer _politicalCompassRenderer = new PoliticalCompassRenderer();
        private readonly PieChartRenderer _dependencyRatioPieChart = new PieChartRenderer();
        private readonly PieChartRenderer _sectorEmploymentPieChart = new PieChartRenderer();
        private readonly PieChartRenderer _spendingAllocationPieChart = new PieChartRenderer();
        private readonly PieChartRenderer _taxRevenuePieChart = new PieChartRenderer();
        private readonly PieChartRenderer _populationPieChart = new PieChartRenderer();
        private readonly HemicycleRenderer _hemicycleRenderer = new HemicycleRenderer();
        private Vector2 _compassAndDemographicsScrollPosition;

        private readonly List<MapEventMarker> _mapEventMarkers = new List<MapEventMarker>();
        private CountryId? _selectedMapCountry;
        private MapEventMarker? _selectedMapEvent;
        private string _cachedSwfContributionText;
        private string _cachedSwfReturnsText;

        private readonly List<string> _turnLog = new List<string>();
        private Vector2 _logScrollPosition;
        private Vector2 _leftColumnScrollPosition;

        /// <summary>Collapsed by default - the "every system has its own tab" routing text is a one-time onboarding note, not something that needs to keep costing vertical space in the dashboard on every turn once a player already knows the layout.</summary>
        private bool _showTabGuide;

        private RightPanelTab _rightPanelTab = RightPanelTab.PolicyWeb;
        private Vector2 _worldMapScrollPosition;
        private Vector2 _policyWebScrollPosition;
        private PolicyNodeId? _selectedPolicyWebPolicyNode;
        private StatNodeId? _selectedPolicyWebStatNode;

        // Political Systems Overhaul Part A (Cabinet). _cabinetCandidatesByPortfolio holds generated
        // candidates awaiting an appointment pick for a currently-vacant (or just-reshuffled)
        // portfolio - absent entry means "no search underway," present-but-not-yet-picked means
        // "candidates are shown, waiting for a click," mirroring _fedChairCandidates' own null-vs-set
        // idiom just keyed per portfolio instead of a single global slot.
        private Vector2 _cabinetScrollPosition;
        private Vector2 _foreignPolicyScrollPosition;
        private readonly Dictionary<CabinetPortfolio, List<CabinetMinister>> _cabinetCandidatesByPortfolio = new Dictionary<CabinetPortfolio, List<CabinetMinister>>();
        private Vector2 _tradeScrollPosition;
        private Vector2 _taxPolicyScrollPosition;
        private Vector2 _spendingPolicyScrollPosition;
        private Vector2 _federalReserveScrollPosition;
        private Vector2 _welfarePolicyScrollPosition;
        private Vector2 _laborMarketScrollPosition;
        private Vector2 _crimeJusticeScrollPosition;
        private Vector2 _sectorPolicyScrollPosition;
        private Vector2 _infrastructureScrollPosition;
        private Vector2 _swfPolicyScrollPosition;

        private bool _stylesInitialized;
        private GUIStyle _headerStyle;
        private GUIStyle _labelStyle;
        private GUIStyle _buttonStyle;
        private GUIStyle _sliderStyle;
        private GUIStyle _sliderThumbStyle;
        private GUIStyle _boxStyle;
        private GUIStyle _tabButtonStyle;
        private GUIStyle _eventBannerStyle;
        private GUIStyle _gameOverStyle;

        // Phase 3 of the UI revamp: action-type-coded button styles (see UiPalette), rebuilt every
        // frame in RescaleStylesToScreen alongside the base styles they're cloned from, so their
        // font size/fixed height always stay in sync with the current screen size too.
        private GUIStyle _implementButtonStyle;
        private GUIStyle _removeButtonStyle;
        private GUIStyle _neutralActionButtonStyle;
        private GUIStyle _primaryButtonStyle;

        private void Start()
        {
            SetupCameraBackground();

            // World/SimulationManager are created immediately (the selector screen needs every
            // country's Name/Id to exist) - only _playerCountry/_prevGdp wait for SelectPlayerCountry,
            // since which country those refer to isn't known until the player picks.
            _world = WorldFactory.CreateDefault();
            _simulationManager = gameObject.AddComponent<SimulationManager>();
            _simulationManager.SetWorld(_world);
            _previewRandom = new System.Random();
        }

        /// <summary>
        /// Continuous Time Migration Phase 0: the real-time clock driving the calendar. Runs every
        /// engine frame (unlike OnGUI, which Unity only calls on repaint) so time passes at a
        /// consistent real-world rate regardless of how often the UI actually redraws. Paused while
        /// there's nothing meaningful to advance into (no country selected yet, game over, an election
        /// reveal showing) or nothing the player can safely advance past (a pending Fed Chair
        /// selection or Cabinet decision) - the same set of gates OnGUI's own
        /// hasPendingFedChairSelection/hasPendingCabinetDecisions checks already enforced for the old
        /// Advance Turn button, generalized here to "time itself doesn't pass" rather than "one button
        /// is disabled".
        /// </summary>
        private void Update()
        {
            if (!_selectedPlayerCountryId.HasValue || _isGameOver || _pendingElectionResult != null)
            {
                return;
            }
            if (_gameSpeed == GameSpeed.Paused)
            {
                return;
            }
            if (UpdateFedChairSelectionState() || _simulationManager.GetPendingCabinetDecisions(PlayerCountryId).Count > 0
                || _simulationManager.GetPendingForeignPolicyMeeting(PlayerCountryId) != null
                || _simulationManager.GetPendingBudgetProcess(PlayerCountryId))
            {
                return;
            }

            _daySpeedTimer += Time.deltaTime;
            float secondsPerDay = GetSecondsPerDay(_gameSpeed);
            while (_daySpeedTimer >= secondsPerDay)
            {
                _daySpeedTimer -= secondsPerDay;
                bool turnBoundaryCrossed = _simulationManager.AdvanceDay();

                // Short-term gameplay scaffolding (Phase 0): rolled every simulated day, independent
                // of the 121-day turn cadence, since these are explicitly meant to land BETWEEN turns.
                _simulationManager.TryRollForeignPolicyMeeting(PlayerCountryId);

                // Political Systems Overhaul Part B PILOT (Master Sequence step 4): a pending TaxBill
                // counts down daily too, independent of the turn boundary - unlike the two calls
                // above, this never needs a gate re-check afterward, since resolving a bill doesn't
                // pause time (it's a deterministic countdown, not something needing a player response).
                _simulationManager.AdvanceLegislativeDay(PlayerCountryId);

                // Master Sequence step 5a: same daily idiom as the two calls above - deterministic
                // date check, not a chance roll, mirroring AdvanceLegislativeDay's own reasoning.
                // Unlike AdvanceLegislativeDay, THIS one DOES need the gate re-check below, since
                // opening the budget process is a mandatory pause (see the revised Part B design).
                _simulationManager.TryOpenBudgetProcess(PlayerCountryId, _simulationManager.CurrentDate);

                if (turnBoundaryCrossed)
                {
                    AdvanceTurn();
                }

                // A newly-fired election reveal/Fed-Chair selection/Cabinet decision/foreign policy
                // meeting/budget process (or game over) must stop the clock immediately, not keep
                // draining _daySpeedTimer toward days/turns that can't happen yet - re-check every gate
                // before this same frame's loop continues.
                if (_isGameOver || _pendingElectionResult != null || UpdateFedChairSelectionState()
                    || _simulationManager.GetPendingCabinetDecisions(PlayerCountryId).Count > 0
                    || _simulationManager.GetPendingForeignPolicyMeeting(PlayerCountryId) != null
                    || _simulationManager.GetPendingBudgetProcess(PlayerCountryId))
                {
                    break;
                }
            }
        }

        /// <summary>Commits the player's country choice from DrawCountrySelector - the one place _selectedPlayerCountryId is ever set.</summary>
        private void SelectPlayerCountry(CountryId countryId)
        {
            _selectedPlayerCountryId = countryId;
            _playerCountry = _world.GetCountry(countryId);
            _prevGdp = _playerCountry.State.GDP;
        }

        /// <summary>
        /// Country-selection task, Part 1: shown once, before the dashboard ever renders (see OnGUI's
        /// gate). One button per country, colored with that country's own already-established
        /// UiPalette identity (see UiPalette.GetCountryArea) - the SAME color the World Map tab uses
        /// for that country's node, so a player's choice here reads consistently everywhere else in
        /// the game. No turn has advanced yet at this point (AdvanceTurn is only ever reachable from
        /// the post-selection dashboard), so picking a country has no cost/commitment beyond the
        /// choice itself.
        /// </summary>
        private void DrawCountrySelector()
        {
            GUILayout.BeginArea(new Rect(0f, 0f, Screen.width, Screen.height));
            GUILayout.FlexibleSpace();
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            GUILayout.BeginVertical(GUILayout.Width(Screen.width * 0.4f));
            GUILayout.Label("PoliSim", _headerStyle);
            GUILayout.Label("Choose your country", _labelStyle);
            GUILayout.Space(20f);

            foreach (Country country in _world.Countries)
            {
                UiPalette.SystemArea area = UiPalette.GetCountryArea(country.Id);
                GUIStyle style = UiPalette.BuildButtonStyle(_buttonStyle, UiPalette.ButtonKind.TabSelected, area);
                if (GUILayout.Button(country.Name, style))
                {
                    SelectPlayerCountry(country.Id);
                }
                GUILayout.Space(10f);
            }

            GUILayout.EndVertical();
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.EndArea();
        }

        /// <summary>
        /// This is an IMGUI-only game (see the class doc comment) - nothing is ever meant to render
        /// behind the UI, so Unity's default Skybox clear (visible as sky/horizon in any gap the UI
        /// doesn't cover) is just visual noise, not a deliberate scene. Solid dark color instead,
        /// matching GraphRenderer's own background tone for a consistent dark theme - no new assets
        /// needed, just a clear-flags/color change on whatever camera is tagged MainCamera.
        /// </summary>
        private static void SetupCameraBackground()
        {
            Camera camera = Camera.main;
            if (camera == null)
            {
                return;
            }

            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.10f, 0.10f, 0.10f, 1f);
        }

        private void OnGUI()
        {
            InitializeStylesIfNeeded();
            RescaleStylesToScreen();

            if (!_selectedPlayerCountryId.HasValue)
            {
                DrawCountrySelector();
                return;
            }

            if (_pendingElectionResult != null)
            {
                DrawElectionResultsScreen(_pendingElectionResult);
                return;
            }

            bool hasPendingFedChairSelection = UpdateFedChairSelectionState();
            // Political Systems Overhaul Part A: same "must resolve before advancing" idiom as Fed
            // Chair selection - a fired cabinet decision needs a player-picked response, not something
            // that should be silently skippable by racing ahead to the next turn.
            bool hasPendingCabinetDecisions = _simulationManager.GetPendingCabinetDecisions(PlayerCountryId).Count > 0;
            // Same idiom again - a pending Foreign Policy meeting also blocks Update's day-loop (see
            // its own gate), but until now had NO representation in this always-visible readout, only
            // in DrawForeignPolicyTab's own modal - invisible from every other tab. See
            // DrawCalendarAndSpeedControls' own doc comment.
            bool hasPendingForeignPolicyMeeting = _simulationManager.GetPendingForeignPolicyMeeting(PlayerCountryId) != null;
            // Master Sequence step 5a: the fourth condition on this same gate/banner, per the revised
            // Part B design's explicit instruction to extend the existing pattern rather than build a
            // fourth separate ad-hoc pause-check system.
            bool hasPendingBudgetProcess = _simulationManager.GetPendingBudgetProcess(PlayerCountryId);

            float marginX = Screen.width * ScreenMarginFraction;
            float marginY = Screen.height * ScreenMarginFraction;
            float areaWidth = Screen.width - marginX * 2f;
            float areaHeight = Screen.height - marginY * 2f;

            float columnSpacing = areaWidth * ColumnSpacingFraction;
            float leftColumnWidth = areaWidth * LeftColumnWidthFraction;
            float rightColumnWidth = areaWidth - leftColumnWidth - columnSpacing;
            float sectionSpacing = areaHeight * SectionSpacingFraction;

            GUILayout.BeginArea(new Rect(marginX, marginY, areaWidth, areaHeight));
            GUILayout.BeginHorizontal();

            GUILayout.BeginVertical(GUILayout.Width(leftColumnWidth));

            // Continuous Time Migration Phase 0: the calendar/speed control panel replaces the old
            // Advance Turn button in this same pinned-outside-scroll-view slot, for the same reason -
            // always visible and clickable regardless of how tall the banner/dashboard/sliders/
            // preview content gets. One extra row taller than the single button it replaces (date +
            // status line, then the speed button row). Master Sequence step 5a adds one more always-
            // present row (the temporary Acknowledge budget process button, per DrawCalendarAndSpeedControls'
            // own doc comment) - reserve _buttonStyle.fixedHeight again for it.
            float calendarAreaHeight = _labelStyle.fontSize + 8f + _buttonStyle.fixedHeight * 2f + sectionSpacing;
            float leftScrollHeight = areaHeight - calendarAreaHeight;

            _leftColumnScrollPosition = GUILayout.BeginScrollView(_leftColumnScrollPosition, GUILayout.Height(leftScrollHeight));
            DrawTopBanner();
            GUILayout.Space(sectionSpacing);
            DrawDashboard();
            GUILayout.Space(sectionSpacing);

            GUI.enabled = !_isGameOver;
            DrawPolicyControls();
            GUI.enabled = true;
            GUILayout.EndScrollView();

            GUILayout.Space(sectionSpacing);

            GUI.enabled = !_isGameOver;
            DrawCalendarAndSpeedControls(hasPendingFedChairSelection, hasPendingCabinetDecisions, hasPendingForeignPolicyMeeting, hasPendingBudgetProcess);
            GUI.enabled = true;

            GUILayout.EndVertical();

            GUILayout.Space(columnSpacing);

            GUILayout.BeginVertical(GUILayout.Width(rightColumnWidth));
            DrawRightColumnTabs(rightColumnWidth);
            GUILayout.Space(sectionSpacing * 0.5f);

            // Four tab rows now (Compass & Demographics added a 15th tab, needing a fourth row - see
            // DrawRightColumnTabs) - reserve all four rows' height plus the spacing between them, not
            // just some, or a later row would silently eat into the tab-content area below and this
            // whole panel would creep past its allotted height.
            float tabRowsHeight = _tabButtonStyle.fixedHeight * 5f + TabRowSpacing * 4f;
            float tabContentHeight = areaHeight - tabRowsHeight - sectionSpacing * 0.5f;
            switch (_rightPanelTab)
            {
                case RightPanelTab.RecentTurns:
                    DrawTurnLog(tabContentHeight);
                    break;
                case RightPanelTab.WorldMap:
                    DrawWorldMapTab(tabContentHeight);
                    break;
                case RightPanelTab.Trade:
                    DrawTrade(tabContentHeight);
                    break;
                case RightPanelTab.TaxPolicy:
                    GUI.enabled = !_isGameOver;
                    DrawTaxPolicy(tabContentHeight);
                    GUI.enabled = true;
                    break;
                case RightPanelTab.SpendingPolicy:
                    GUI.enabled = !_isGameOver;
                    DrawSpendingPolicy(tabContentHeight);
                    GUI.enabled = true;
                    break;
                case RightPanelTab.FederalReserve:
                    GUI.enabled = !_isGameOver;
                    DrawFederalReserveTab(tabContentHeight);
                    GUI.enabled = true;
                    break;
                case RightPanelTab.WelfarePolicy:
                    GUI.enabled = !_isGameOver;
                    DrawWelfarePolicy(tabContentHeight);
                    GUI.enabled = true;
                    break;
                case RightPanelTab.LaborMarket:
                    GUI.enabled = !_isGameOver;
                    DrawLaborMarketTab(tabContentHeight);
                    GUI.enabled = true;
                    break;
                case RightPanelTab.CrimeJustice:
                    GUI.enabled = !_isGameOver;
                    DrawCrimeJusticeTab(tabContentHeight);
                    GUI.enabled = true;
                    break;
                case RightPanelTab.SectorPolicy:
                    GUI.enabled = !_isGameOver;
                    DrawSectorPolicy(tabContentHeight);
                    GUI.enabled = true;
                    break;
                case RightPanelTab.Infrastructure:
                    DrawInfrastructureTab(tabContentHeight);
                    break;
                case RightPanelTab.PolicyWeb:
                    DrawPolicyWebTab(tabContentHeight);
                    break;
                case RightPanelTab.Cabinet:
                    GUI.enabled = !_isGameOver;
                    DrawCabinetTab(tabContentHeight);
                    GUI.enabled = true;
                    break;
                case RightPanelTab.CompassAndDemographics:
                    DrawCompassAndDemographicsTab(tabContentHeight);
                    break;
                case RightPanelTab.ForeignPolicy:
                    GUI.enabled = !_isGameOver;
                    DrawForeignPolicyTab(tabContentHeight);
                    GUI.enabled = true;
                    break;
                case RightPanelTab.Parliament:
                    DrawParliamentTab(tabContentHeight);
                    break;
                case RightPanelTab.SwfPolicy:
                    GUI.enabled = !_isGameOver;
                    DrawSwfPolicy(tabContentHeight);
                    GUI.enabled = true;
                    break;
            }

            GUILayout.EndVertical();

            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

        private void InitializeStylesIfNeeded()
        {
            if (_stylesInitialized)
            {
                return;
            }

            _headerStyle = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold, wordWrap = true };
            _labelStyle = new GUIStyle(GUI.skin.label) { wordWrap = true };
            _buttonStyle = new GUIStyle(GUI.skin.button);
            _sliderStyle = new GUIStyle(GUI.skin.horizontalSlider);
            _sliderThumbStyle = new GUIStyle(GUI.skin.horizontalSliderThumb);
            _boxStyle = new GUIStyle(GUI.skin.box);
            // wordWrap so a long label (e.g. "Sovereign Wealth Fund") degrades to two lines at a
            // narrow per-button width instead of being hard-clipped - safe alongside the fixedHeight
            // RescaleStylesToScreen sets below, since a fixed height forces the control to that exact
            // height regardless of how many lines its content wraps to, so this can never push the
            // tab-content area underneath it out of its own reserved space.
            _tabButtonStyle = new GUIStyle(GUI.skin.button) { wordWrap = true };
            _eventBannerStyle = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold, wordWrap = true };
            _eventBannerStyle.normal.textColor = new Color(1f, 0.65f, 0f);
            _gameOverStyle = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold, wordWrap = true };
            _gameOverStyle.normal.textColor = Color.red;

            _stylesInitialized = true;
        }

        /// <summary>Re-derives every style's font size/control size from the current screen size every frame (cheap field writes, no allocation) so a live window resize stays legible instead of squinting-small.</summary>
        private void RescaleStylesToScreen()
        {
            int headerFontSize = Mathf.Clamp(Mathf.RoundToInt(Screen.height * 0.032f), 22, 42);
            int labelFontSize = Mathf.Clamp(Mathf.RoundToInt(Screen.height * 0.022f), 16, 28);
            int buttonFontSize = Mathf.Clamp(Mathf.RoundToInt(Screen.height * 0.032f), 22, 38);
            int tabFontSize = Mathf.Clamp(Mathf.RoundToInt(Screen.height * 0.024f), 18, 30);
            int bannerFontSize = Mathf.Clamp(Mathf.RoundToInt(Screen.height * 0.028f), 20, 36);
            float sliderHeight = Mathf.Clamp(Screen.height * 0.035f, 26f, 50f);
            float sliderThumbWidth = Mathf.Clamp(Screen.width * 0.03f, 26f, 50f);
            float buttonHeight = Mathf.Clamp(Screen.height * 0.09f, 60f, 140f);
            float tabButtonHeight = Mathf.Clamp(Screen.height * 0.05f, 36f, 70f);

            _headerStyle.fontSize = headerFontSize;
            _labelStyle.fontSize = labelFontSize;
            _boxStyle.fontSize = labelFontSize;

            _buttonStyle.fontSize = buttonFontSize;
            _buttonStyle.fixedHeight = buttonHeight;

            _sliderStyle.fixedHeight = sliderHeight;
            _sliderThumbStyle.fixedHeight = sliderHeight;
            _sliderThumbStyle.fixedWidth = sliderThumbWidth;

            _tabButtonStyle.fontSize = tabFontSize;
            _tabButtonStyle.fixedHeight = tabButtonHeight;

            _eventBannerStyle.fontSize = bannerFontSize;
            _gameOverStyle.fontSize = bannerFontSize;

            // Rebuilt every frame (cheap - a handful of GUIStyle clones reusing cached swatch
            // textures, not per-frame texture allocation) so they always match _tabButtonStyle/
            // _buttonStyle's just-updated size above, rather than drifting stale from whatever size
            // they happened to be cloned at in InitializeStylesIfNeeded.
            _implementButtonStyle = UiPalette.BuildButtonStyle(_tabButtonStyle, UiPalette.ButtonKind.Implement);
            _removeButtonStyle = UiPalette.BuildButtonStyle(_tabButtonStyle, UiPalette.ButtonKind.Remove);
            _neutralActionButtonStyle = UiPalette.BuildButtonStyle(_tabButtonStyle, UiPalette.ButtonKind.Neutral);
            _primaryButtonStyle = UiPalette.BuildButtonStyle(_buttonStyle, UiPalette.ButtonKind.Primary);
        }

        /// <summary>Maps a right-column tab to the system area whose hue it should be tinted with (see UiPalette.SystemArea) - Recent Turns is informational, not a system area, so it stays Neutral.</summary>
        private static UiPalette.SystemArea GetTabArea(RightPanelTab tab)
        {
            switch (tab)
            {
                case RightPanelTab.WorldMap: return UiPalette.SystemArea.Global;
                case RightPanelTab.Trade: return UiPalette.SystemArea.Trade;
                case RightPanelTab.TaxPolicy: return UiPalette.SystemArea.Fiscal;
                case RightPanelTab.SpendingPolicy: return UiPalette.SystemArea.Fiscal;
                case RightPanelTab.FederalReserve: return UiPalette.SystemArea.Political;
                case RightPanelTab.WelfarePolicy: return UiPalette.SystemArea.Welfare;
                case RightPanelTab.LaborMarket: return UiPalette.SystemArea.Labor;
                case RightPanelTab.CrimeJustice: return UiPalette.SystemArea.CrimeJustice;
                case RightPanelTab.SectorPolicy: return UiPalette.SystemArea.Sectors;
                case RightPanelTab.Infrastructure: return UiPalette.SystemArea.Infrastructure;
                case RightPanelTab.SwfPolicy: return UiPalette.SystemArea.SovereignWealth;
                case RightPanelTab.PolicyWeb: return UiPalette.SystemArea.Global;
                case RightPanelTab.Cabinet: return UiPalette.SystemArea.Political;
                case RightPanelTab.CompassAndDemographics: return UiPalette.SystemArea.Global;
                case RightPanelTab.ForeignPolicy: return UiPalette.SystemArea.Trade;
                case RightPanelTab.Parliament: return UiPalette.SystemArea.Political;
                default: return UiPalette.SystemArea.Neutral;
            }
        }

        /// <summary>One-off tinted label (GUI.color multiplies the style's own text color, restored immediately after) - used for every signed-delta readout in the UI so its color always reflects UiPalette.GetDeltaColor rather than a hand-picked one-time color.</summary>
        private static void DrawColoredLabel(string text, GUIStyle style, Color color)
        {
            Color previous = GUI.color;
            GUI.color = color;
            GUILayout.Label(text, style);
            GUI.color = previous;
        }

        /// <summary>Above the dashboard: a game-over banner takes priority (the game has effectively ended), otherwise the current turn's event (if any) as "BREAKING: ...".</summary>
        private void DrawTopBanner()
        {
            if (_isGameOver)
            {
                GUILayout.BeginVertical(_boxStyle);
                GUILayout.Label("GAME OVER", _gameOverStyle);
                GUILayout.Label(_gameOverReason, _labelStyle);
                GUILayout.EndVertical();
                return;
            }

            EconomicEvent activeEvent = _simulationManager.GetLastEvent(PlayerCountryId);
            if (activeEvent == null)
            {
                return;
            }

            GUILayout.BeginVertical(_boxStyle);
            GUILayout.Label($"BREAKING: {activeEvent.Name}", _eventBannerStyle);
            GUILayout.Label(activeEvent.Description, _labelStyle);
            GUILayout.Label(
                $"Effects: GDP {activeEvent.GdpShockPercent:+0.0;-0.0}%, Inflation {activeEvent.InflationShockPoints:+0.0;-0.0} pts, Approval {activeEvent.ApprovalEffect:+0.0;-0.0}",
                _labelStyle);
            GUILayout.EndVertical();
        }

        private void DrawDashboard()
        {
            EconomyState state = _playerCountry.State;
            bool hasIndependentCurrency = !CurrencySystem.SharesCurrencyZoneWithOthers(_playerCountry, _world);

            // Phase 4 trims this down to true headline indicators only - Labor Force Participation,
            // Crime Index, Paid Family Leave, Incarceration Rate, Infrastructure Condition, Interest
            // Rate, Tariff Rate, and Sovereign Wealth Fund detail all moved to their own dedicated
            // tabs (see DrawRightColumnTabs) and would just be redundant duplication here now - the
            // "compact home view" the task asked for, not everything at once.
            GUILayout.BeginVertical(_boxStyle);
            GUILayout.Label($"{_playerCountry.Name} - Turn {_simulationManager.CurrentTurn}", _headerStyle);

            // Two columns instead of one long vertical list - halves this block's own height, which
            // matters since it's one of the biggest single contributors to the left column needing to
            // scroll at all. Split is just "first half / second half" of the same headline set, not a
            // meaningful grouping - there's no natural pairing among these nine values worth encoding.
            GUILayout.BeginHorizontal();
            GUILayout.BeginVertical();
            DrawColoredLabel($"GDP: {state.GDP:F1}  ({_lastGrowthPercent:+0.00;-0.00;0}%)", _labelStyle, UiPalette.GetDeltaColor(_lastGrowthPercent, higherIsBetter: true));
            GUILayout.Label($"Unemployment: {state.Unemployment:F2}%", _labelStyle);
            GUILayout.Label($"Inflation: {state.Inflation:F2}%", _labelStyle);
            GUILayout.Label($"Approval Rating: {state.ApprovalRating:F1}", _labelStyle);
            if (hasIndependentCurrency)
            {
                GUILayout.Label($"Currency Strength: {state.CurrencyStrength:F1}", _labelStyle);
            }
            GUILayout.EndVertical();

            GUILayout.BeginVertical();
            GUILayout.Label($"Poverty Rate: {state.PovertyRate:F1}%", _labelStyle);
            GUILayout.Label($"Government Debt: {state.GovernmentDebt:F1}", _labelStyle);
            GUILayout.Label($"Debt-to-GDP: {state.DebtToGdpRatio:F1}%", _labelStyle);
            GUILayout.Label($"Budget Balance (cumulative): {state.Budget:F1}", _labelStyle);
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();

            GUILayout.Space(10f);
            DrawHeadlineGraphs(state);

            GUILayout.EndVertical();
        }

        /// <summary>
        /// Phase 2 of the UI revamp's dashboard graphs - proves the GraphRenderer pattern on the
        /// three headline stats before rolling it out further. Each graph reads its country's own
        /// StatHistory (Phase 1) and, once a policy preview has been computed at least once this
        /// session, extends one point further using that PreviewTurn estimate - not a separate
        /// hand-rolled forecast, the same "reuse the real preview math" idiom the existing text
        /// preview already established.
        /// </summary>
        private void DrawHeadlineGraphs(EconomyState state)
        {
            float? projectedGdp = null;
            float? projectedUnemployment = null;
            float? projectedApproval = null;

            if (_hasCachedPreview)
            {
                projectedGdp = state.GDP * (1f + _cachedGdpGrowthPercentRaw / 100f);
                projectedUnemployment = state.Unemployment + _cachedUnemploymentChangeRaw;
                projectedApproval = state.ApprovalRating + _cachedApprovalChangeRaw;
            }

            // Continuous Time Migration Phase 0: every graph reads the Quarterly resolution
            // specifically - see StatHistory's own class doc comment for why this is the resolution
            // that exactly matches this project's existing one-point-per-turn graph cadence with zero
            // visual change, while Daily/Weekly/Monthly sit ready underneath for Phases 1-5.
            StatHistory history = _playerCountry.History;
            _gdpGraph.Draw("GDP (dashed = next-turn estimate)", history.Gdp.Quarterly, projectedGdp, _labelStyle, higherIsBetter: true);
            _unemploymentGraph.Draw("Unemployment (dashed = next-turn estimate)", history.Unemployment.Quarterly, projectedUnemployment, _labelStyle, higherIsBetter: false,
                thresholdValue: _playerCountry.NaturalUnemploymentRate, thresholdLabel: "NAIRU");
            _approvalGraph.Draw("Approval Rating (dashed = next-turn estimate)", history.ApprovalRating.Quarterly, projectedApproval, _labelStyle, higherIsBetter: true);
        }

        /// <summary>
        /// Generates 2-3 Fed chair candidates the first time the upcoming turn is detected as an
        /// election turn (see ElectionSystem.IsElectionTurn), and remembers which turn they were
        /// generated for so picking a candidate doesn't immediately regenerate a fresh set on the
        /// next frame - only when that specific upcoming turn changes (i.e. the next election cycle)
        /// does a new set get drawn. Returns true while a selection is still pending (blocks Advance
        /// Turn - see OnGUI). No-op (returns false) for a country without an independent Fed chair.
        /// </summary>
        private bool UpdateFedChairSelectionState()
        {
            if (_playerCountry.CurrentFedChair == null)
            {
                return false;
            }

            int upcomingTurn = _simulationManager.CurrentTurn + 1;
            if (!ElectionSystem.IsElectionTurn(upcomingTurn))
            {
                return false;
            }

            if (_fedChairCandidatesForTurn != upcomingTurn)
            {
                _fedChairCandidates = FederalReserveSystem.GenerateCandidates();
                _fedChairCandidatesForTurn = upcomingTurn;
            }

            return _fedChairCandidates != null && _fedChairCandidates.Count > 0;
        }

        /// <summary>
        /// This tab's real institution name per country - previously hardcoded to "Federal Reserve"
        /// for all six (both here and in the tab bar's own button label), even though the underlying
        /// mechanic already correctly varies per country (independent Fed chair for USA, a shared ECB
        /// rate with a national push for the Eurozone trio, a fully independent rate for Sweden/
        /// Poland - see DrawFederalReserveTab). This was a text/branding gap, not a behavior gap.
        /// </summary>
        private static string GetCentralBankName(CountryId countryId)
        {
            switch (countryId)
            {
                case CountryId.USA: return "Federal Reserve";
                case CountryId.Germany:
                case CountryId.France:
                case CountryId.Italy:
                    return "European Central Bank (ECB)";
                case CountryId.Sweden: return "Sveriges Riksbank";
                case CountryId.Poland: return "Narodowy Bank Polski (NBP)";
                default: return "Central Bank";
            }
        }

        /// <summary>A short real flavor line for the two countries whose central bank has a notable, concrete real-world fact worth surfacing - null for USA/Eurozone, which already have their own descriptive text below this point.</summary>
        private static string GetCentralBankFlavorText(CountryId countryId)
        {
            switch (countryId)
            {
                case CountryId.Sweden: return "Founded 1668 - the world's oldest central bank still in operation.";
                case CountryId.Poland: return "Founded 1945, headquartered in Warsaw.";
                default: return null;
            }
        }

        /// <summary>
        /// Federal Reserve tab (Phase 4 - moved off the dashboard into its own home). For USA's
        /// independent chair (see CLAUDE.md's "Federal Reserve" section): current chair's name/
        /// philosophy/description, and - on a turn where a new presidential term begins - the 2-3
        /// candidates as selectable buttons. For a country with no independent chair and an
        /// independent currency (Sweden, Poland), shows the player-controlled Interest Rate Change
        /// slider. For a Eurozone member (Germany/France/Italy), shows a much narrower National Rate
        /// Push slider instead - see CLAUDE.md's "Eurozone Rate Voice" - this tab is that control's
        /// home regardless of which mechanic the player's country actually uses. The tab's own
        /// displayed name (both here and the tab bar button) now reflects the real institution per
        /// country too - see GetCentralBankName.
        /// </summary>
        private void DrawFederalReserveTab(float availableHeight)
        {
            GUILayout.BeginVertical(_boxStyle);

            float scrollHeight = availableHeight - _labelStyle.fontSize * 2f;
            _federalReserveScrollPosition = GUILayout.BeginScrollView(_federalReserveScrollPosition, GUILayout.Height(scrollHeight));

            DrawColoredLabel(GetCentralBankName(PlayerCountryId), _headerStyle, UiPalette.GetAreaColor(UiPalette.SystemArea.Political));
            string centralBankFlavorText = GetCentralBankFlavorText(PlayerCountryId);
            if (centralBankFlavorText != null)
            {
                GUILayout.Label(centralBankFlavorText, _labelStyle);
            }

            if (_playerCountry.CurrentFedChair != null)
            {
                FedChair chair = _playerCountry.CurrentFedChair;
                GUILayout.Label($"Chair: {chair.Name} ({chair.Philosophy})", _labelStyle);
                GUILayout.Label(chair.Description, _labelStyle);

                if (_fedChairCandidates != null && _fedChairCandidates.Count > 0)
                {
                    GUILayout.Space(8f);
                    GUILayout.Label("A new presidential term begins next turn - choose the next Fed chair:", _labelStyle);
                    foreach (FedChair candidate in _fedChairCandidates)
                    {
                        DrawFedChairCandidateButton(candidate);
                    }
                }
            }
            else
            {
                // Shared-currency countries (e.g. Eurozone members) don't set their own rate - only
                // show this for a country with an independent CurrencyZone (Sweden, Poland; the
                // Eurozone trio share one CurrencyZone.InterestRate that none of them set alone).
                bool hasIndependentCurrency = !CurrencySystem.SharesCurrencyZoneWithOthers(_playerCountry, _world);
                if (hasIndependentCurrency)
                {
                    GUILayout.Label($"Interest Rate Change: {_interestRateChangeInput:+0.00;-0.00;0} pts", _labelStyle);
                    _interestRateChangeInput = GUILayout.HorizontalSlider(_interestRateChangeInput, -InterestRateChangeRange, InterestRateChangeRange, _sliderStyle, _sliderThumbStyle);
                }
                else
                {
                    // Eurozone Rate-Voice mechanic (see CLAUDE.md's "Eurozone Rate Voice"): the
                    // shared rate is a GDP-weighted blend of all three members' own Taylor Rule
                    // readings (EurozoneRateSystem.GetBlendedSuggestedRate), not something any one
                    // member sets unilaterally - but whichever member the player is currently
                    // controlling now gets a real, bounded push on top of that blend (reusing the
                    // same InterestRateChange field/slider Sweden/Poland use, just with a much
                    // narrower range - a national governor's real but limited sway, not unilateral
                    // control). Superseded the original country-selection Part 1 framing, which
                    // described this as fully read-only before this mechanic existed.
                    GUILayout.Label(
                        $"{_playerCountry.Name} shares the Eurozone's single currency and interest rate with {GetOtherEurozoneMemberNames()}. Each member's own Taylor Rule reading pulls the shared rate toward its own inflation/output-gap situation, weighted by its share of the three countries' combined GDP - a simplified version of the real ECB's \"capital key.\" As {_playerCountry.Name}'s governor you get a modest, bounded push on top of that blend - real influence, not unilateral control, the same way no single member state sets the ECB's rate alone.",
                        _labelStyle);
                    GUILayout.Label($"National Rate Push: {_interestRateChangeInput:+0.00;-0.00;0} pts", _labelStyle);
                    _interestRateChangeInput = GUILayout.HorizontalSlider(_interestRateChangeInput, -EurozoneRateSystem.MemberRatePushRange, EurozoneRateSystem.MemberRatePushRange, _sliderStyle, _sliderThumbStyle);
                    GUILayout.Label($"Current Eurozone Interest Rate: {_playerCountry.CurrencyZone.InterestRate:F2}%", _labelStyle);
                }
            }

            GUILayout.Space(10f);
            // Neutral (no green/red judgment) - which direction of rate change is "good" depends
            // entirely on the current inflation/growth situation, not a fixed convention.
            _interestRateGraph.DrawNeutral("Interest Rate", _playerCountry.History.InterestRate.Quarterly, null, _labelStyle);

            GUILayout.EndScrollView();
            GUILayout.EndVertical();
        }

        /// <summary>Every OTHER country sharing the player's own CurrencyZone (reference equality, matching CurrencySystem.SharesCurrencyZoneWithOthers' own idiom - see that method), comma-joined - only meaningful when DrawFederalReserveTab has already confirmed the player's country is in a shared zone.</summary>
        private string GetOtherEurozoneMemberNames()
        {
            var otherNames = new List<string>();
            foreach (Country country in _world.Countries)
            {
                if (country.Id != _playerCountry.Id && country.CurrencyZone == _playerCountry.CurrencyZone)
                {
                    otherNames.Add(country.Name);
                }
            }
            return string.Join(" and ", otherNames);
        }

        private void DrawFedChairCandidateButton(FedChair candidate)
        {
            GUILayout.BeginVertical(_boxStyle);
            GUILayout.Label($"{candidate.Name} ({candidate.Philosophy})", _labelStyle);
            GUILayout.Label(candidate.Description, _labelStyle);
            if (GUILayout.Button($"Appoint {candidate.Name}", _neutralActionButtonStyle))
            {
                _playerCountry.CurrentFedChair = candidate;
                _fedChairCandidates = null;
                RecomputePolicyPreview();
            }
            GUILayout.EndVertical();
        }

        /// <summary>
        /// Crime &amp; Justice tab (Phase 4 - moved off the dashboard into its own home): Police
        /// Funding / Sentencing Severity / Bail Reform / Drug Policy / Judicial Funding / Border
        /// Enforcement sliders (the last two added in Round 3 item 3), plus CrimeIndex/
        /// OrganizedCrimeIndex/CorruptionIndex (a clear direction - lower is better for all three) and
        /// PrisonPopulationRate (deliberately neutral - see PrisonPopulationRate's own doc comment on
        /// BailReformLevel/DrugPolicyLevel's honestly-contested effects) history graphs.
        /// </summary>
        private void DrawCrimeJusticeTab(float availableHeight)
        {
            GUILayout.BeginVertical(_boxStyle);

            float scrollHeight = availableHeight - _labelStyle.fontSize * 2f;
            _crimeJusticeScrollPosition = GUILayout.BeginScrollView(_crimeJusticeScrollPosition, GUILayout.Height(scrollHeight));

            DrawColoredLabel("Crime & Justice", _headerStyle, UiPalette.GetAreaColor(UiPalette.SystemArea.CrimeJustice));
            GUILayout.Space(8f);

            float draftPoliceFunding = GetPoliceFundingInput(_playerCountry.PoliceFundingLevel);
            GUILayout.Label($"Police Funding: {draftPoliceFunding:F0}", _labelStyle);
            _policeFundingInput = GUILayout.HorizontalSlider(draftPoliceFunding, MinPolicyDialLevel, MaxPolicyDialLevel, _sliderStyle, _sliderThumbStyle);

            float draftSentencingSeverity = GetSentencingSeverityInput(_playerCountry.SentencingSeverity);
            GUILayout.Label($"Sentencing Severity: {draftSentencingSeverity:F0} (0 = lenient, 100 = harsh)", _labelStyle);
            _sentencingSeverityInput = GUILayout.HorizontalSlider(draftSentencingSeverity, MinPolicyDialLevel, MaxPolicyDialLevel, _sliderStyle, _sliderThumbStyle);

            float draftBailReform = GetBailReformInput(_playerCountry.BailReformLevel);
            GUILayout.Label($"Bail Reform: {draftBailReform:F0} (0 = traditional cash bail, 100 = full reform)", _labelStyle);
            _bailReformInput = GUILayout.HorizontalSlider(draftBailReform, MinPolicyDialLevel, MaxPolicyDialLevel, _sliderStyle, _sliderThumbStyle);

            float draftDrugPolicy = GetDrugPolicyInput(_playerCountry.DrugPolicyLevel);
            GUILayout.Label($"Drug Policy: {draftDrugPolicy:F0} (0 = decriminalized, 100 = strict criminalization)", _labelStyle);
            _drugPolicyInput = GUILayout.HorizontalSlider(draftDrugPolicy, MinPolicyDialLevel, MaxPolicyDialLevel, _sliderStyle, _sliderThumbStyle);

            float draftJudicialFunding = GetJudicialFundingInput(_playerCountry.JudicialFundingLevel);
            GUILayout.Label($"Judicial Funding: {draftJudicialFunding:F0}", _labelStyle);
            _judicialFundingInput = GUILayout.HorizontalSlider(draftJudicialFunding, MinPolicyDialLevel, MaxPolicyDialLevel, _sliderStyle, _sliderThumbStyle);

            float draftBorderEnforcement = GetBorderEnforcementInput(_playerCountry.BorderEnforcementLevel);
            GUILayout.Label($"Border Enforcement: {draftBorderEnforcement:F0} (0 = open/lenient, 100 = strict)", _labelStyle);
            _borderEnforcementInput = GUILayout.HorizontalSlider(draftBorderEnforcement, MinPolicyDialLevel, MaxPolicyDialLevel, _sliderStyle, _sliderThumbStyle);

            GUILayout.Space(10f);
            _crimeIndexGraph.Draw("Crime Index", _playerCountry.History.CrimeIndex.Quarterly, null, _labelStyle, higherIsBetter: false);
            _organizedCrimeGraph.Draw("Organized Crime Index", _playerCountry.History.OrganizedCrimeIndex.Quarterly, null, _labelStyle, higherIsBetter: false);
            _corruptionGraph.Draw("Corruption Index", _playerCountry.History.CorruptionIndex.Quarterly, null, _labelStyle, higherIsBetter: false);
            _prisonPopulationGraph.DrawNeutral("Incarceration Rate per 100k", _playerCountry.History.PrisonPopulationRate.Quarterly, null, _labelStyle);

            GUILayout.EndScrollView();
            GUILayout.EndVertical();
        }

        /// <summary>
        /// Labor Market tab (Phase 4 - moved off the dashboard into its own home, now also including
        /// Minimum Wage since it's a labor-market lever like the other three): Minimum Wage / Paid
        /// Family Leave / Overtime Regulation / Retraining Program, plus a LaborForceParticipationRate
        /// history graph.
        /// </summary>
        private void DrawLaborMarketTab(float availableHeight)
        {
            GUILayout.BeginVertical(_boxStyle);

            float scrollHeight = availableHeight - _labelStyle.fontSize * 2f;
            _laborMarketScrollPosition = GUILayout.BeginScrollView(_laborMarketScrollPosition, GUILayout.Height(scrollHeight));

            DrawColoredLabel("Labor Market", _headerStyle, UiPalette.GetAreaColor(UiPalette.SystemArea.Labor));
            GUILayout.Space(8f);

            DrawMinimumWageControl();

            float draftPaidLeave = GetPaidFamilyLeaveWeeksInput(_playerCountry.PaidFamilyLeaveWeeks);
            GUILayout.Label($"Paid Family Leave: {draftPaidLeave:F0} weeks", _labelStyle);
            _paidFamilyLeaveWeeksInput = GUILayout.HorizontalSlider(draftPaidLeave, MinPaidFamilyLeaveWeeks, MaxPaidFamilyLeaveWeeks, _sliderStyle, _sliderThumbStyle);

            float draftOvertimeRegulation = GetOvertimeRegulationInput(_playerCountry.OvertimeRegulationLevel);
            GUILayout.Label($"Overtime/Working-Hour Regulation: {draftOvertimeRegulation:F0} (0 = unregulated, 100 = strict caps)", _labelStyle);
            _overtimeRegulationInput = GUILayout.HorizontalSlider(draftOvertimeRegulation, MinLaborDialLevel, MaxLaborDialLevel, _sliderStyle, _sliderThumbStyle);

            float draftRetraining = GetRetrainingProgramInput(_playerCountry.RetrainingProgramLevel);
            GUILayout.Label($"Workforce Retraining Programs: {draftRetraining:F0}", _labelStyle);
            _retrainingProgramInput = GUILayout.HorizontalSlider(draftRetraining, MinLaborDialLevel, MaxLaborDialLevel, _sliderStyle, _sliderThumbStyle);

            GUILayout.Space(8f);
            float draftFamilyPolicy = GetFamilyPolicyInput(_playerCountry.FamilyPolicyLevel);
            GUILayout.Label($"Family Policy: {draftFamilyPolicy:F0} (0 = minimal support, 100 = maximal pro-natalist support)", _labelStyle);
            _familyPolicyInput = GUILayout.HorizontalSlider(draftFamilyPolicy, MinPolicyDialLevel, MaxPolicyDialLevel, _sliderStyle, _sliderThumbStyle);

            float draftImmigrationPolicy = GetImmigrationPolicyInput(_playerCountry.ImmigrationPolicyLevel);
            GUILayout.Label($"Immigration Policy: {draftImmigrationPolicy:F0} (0 = maximally restrictive, 100 = maximally open)", _labelStyle);
            _immigrationPolicyInput = GUILayout.HorizontalSlider(draftImmigrationPolicy, MinPolicyDialLevel, MaxPolicyDialLevel, _sliderStyle, _sliderThumbStyle);

            GUILayout.Space(10f);
            _laborForceParticipationGraph.Draw("Labor Force Participation", _playerCountry.History.LaborForceParticipationRate.Quarterly, null, _labelStyle, higherIsBetter: true);

            GUILayout.Space(8f);
            EconomyState demographicState = _playerCountry.State;
            GUILayout.Label(
                $"Population: {demographicState.Population:F1}M ({demographicState.PopulationGrowthRate:+0.0;-0.0}/1,000/yr) - " +
                $"Birth {demographicState.BirthRate:F1}, Death {demographicState.DeathRate:F1}, Net Migration {demographicState.NetMigrationRate:+0.0;-0.0}, " +
                $"Dependency Ratio {demographicState.DependencyRatio:F1}",
                _labelStyle);

            GUILayout.EndScrollView();
            GUILayout.EndVertical();
        }

        /// <summary>
        /// Minimum wage (percent of median wage) - only shown as adjustable if
        /// Country.MinimumWageImplemented (USA - see WorldFactory); Sweden and Italy have no
        /// statutory minimum wage in reality, so this shows a read-only note for them instead (the
        /// player's country is hardcoded to USA, so this branch is currently unreachable in practice,
        /// but kept correct in case PlayerCountryId ever changes).
        /// </summary>
        private void DrawMinimumWageControl()
        {
            if (!_playerCountry.MinimumWageImplemented)
            {
                GUILayout.Label("Minimum Wage: no statutory minimum wage (relies on collective bargaining).", _labelStyle);
                return;
            }

            float draftMinimumWage = GetMinimumWageInput(_playerCountry.MinimumWagePercentOfMedian);
            GUILayout.Label($"Minimum Wage: {draftMinimumWage:F0}% of median wage", _labelStyle);
            _minimumWageInput = GUILayout.HorizontalSlider(draftMinimumWage, MinMinimumWagePercent, MaxMinimumWagePercent, _sliderStyle, _sliderThumbStyle);
        }

        /// <summary>
        /// Infrastructure tab (Phase 4 - new, replacing the old single dashboard summary line):
        /// descriptive only, no player-facing dial (Infrastructure Condition is driven entirely by
        /// the existing Infrastructure spending category - see MacroSystem.ApplyInfrastructureCondition
        /// and GetInfrastructureSummaryLine's own original doc comment for why). Proportional bars,
        /// not a line graph - this is "how do these four assets compare right now" breakdown data,
        /// not a trend-over-time reading, matching the task's own bar-vs-graph guidance.
        /// </summary>
        private void DrawInfrastructureTab(float availableHeight)
        {
            GUILayout.BeginVertical(_boxStyle);

            float scrollHeight = availableHeight - _labelStyle.fontSize * 2f;
            _infrastructureScrollPosition = GUILayout.BeginScrollView(_infrastructureScrollPosition, GUILayout.Height(scrollHeight));

            DrawColoredLabel("Infrastructure", _headerStyle, UiPalette.GetAreaColor(UiPalette.SystemArea.Infrastructure));
            GUILayout.Label("Condition Index (0-100) per asset type - driven by the Infrastructure spending category in the Spending Policy tab, not a dial here.", _labelStyle);
            GUILayout.Space(8f);

            foreach (InfrastructureAsset asset in _playerCountry.InfrastructureAssets)
            {
                GUILayout.Label($"{asset.Type}: {asset.ConditionIndex:F0} / 100", _labelStyle);
                UiPalette.DrawBar(asset.ConditionIndex / 100f, UiPalette.GetAreaColor(UiPalette.SystemArea.Infrastructure));
                GUILayout.Space(8f);
            }

            GUILayout.EndScrollView();
            GUILayout.EndVertical();
        }

        /// <summary>
        /// Phase 4's compact dashboard "home view": routing text pointing at every tab (kept brief
        /// since there are now 11 of them) plus the live Policy Preview panel - the preview is
        /// deliberately NOT tab-owned, since it summarizes the draft PolicyDecision across every tab
        /// at once, letting the player gauge this turn's effect without tab-hopping.
        /// </summary>
        private void DrawPolicyControls()
        {
            GUILayout.BeginVertical(_boxStyle);

            GUILayout.BeginHorizontal();
            GUILayout.Label("This Turn's Policy", _headerStyle);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button(_showTabGuide ? "Hide tab guide" : "Show tab guide", GUILayout.ExpandWidth(false)))
            {
                _showTabGuide = !_showTabGuide;
            }
            GUILayout.EndHorizontal();

            if (_showTabGuide)
            {
                GUILayout.Label("Every system now has its own tab (Tax/Spending/Federal Reserve/Welfare/Labor Market/Crime & Justice/Economic Sectors/Infrastructure/Sovereign Wealth Fund/Trade) - the estimate below reflects your current draft across all of them at once.", _labelStyle);
            }

            DrawPolicyPreview();

            GUILayout.EndVertical();
        }

        /// <summary>
        /// Live estimate of this turn's effect under the sliders' current values, via
        /// SimulationManager.PreviewTurn (reuses the real MacroSystem/SimulationManager formulas
        /// against a throwaway clone rather than a separate hand-rolled estimate) plus a cosmetic
        /// +-5-10% margin of error. Checked every OnGUI call but only actually recomputed (and the
        /// margin re-rolled) when the draft OR the selected horizon has changed since last frame -
        /// see PolicyInputsChangedSinceLastPreview - so it reads as one stable forecast rather than a
        /// flickering number, while still updating live as the player drags a slider or switches
        /// horizon.
        ///
        /// Continuous Time Migration Phase 0: redesigned around a selectable horizon (1 Day/1 Week/1
        /// Month/Full Turn - see PreviewHorizon), defaulting to 1 Day, per the Master Roadmap's own
        /// "effect-per-day plus a selectable-horizon projection" spec. Every figure shown is a
        /// DISPLAY-ONLY re-scaling of the same full-turn PreviewTurn output (see
        /// ScaleLinearForDisplay/ScaleCompoundingForDisplay) - Phase 0 doesn't simulate genuine
        /// sub-turn granularity yet (that's Phases 1-5), so this is honestly labeled as an estimate
        /// derived from the full-turn projection, not a real per-day simulation.
        /// </summary>
        private void DrawPolicyPreview()
        {
            if (PolicyInputsChangedSinceLastPreview())
            {
                RecomputePolicyPreview();
            }

            GUILayout.Space(10f);
            GUILayout.BeginHorizontal();
            GUILayout.Label("Estimated Effects", _headerStyle);
            GUILayout.FlexibleSpace();
            DrawHorizonButton(PreviewHorizon.OneDay);
            DrawHorizonButton(PreviewHorizon.OneWeek);
            DrawHorizonButton(PreviewHorizon.OneMonth);
            DrawHorizonButton(PreviewHorizon.FullTurn);
            GUILayout.EndHorizontal();
            GUILayout.Label($"Over the next {GetHorizonLabel(_previewHorizon)} (±5-10% margin of error) - a linear/compounding-scaled display estimate from the full {SimulationManager.DaysPerTurn}-day projection, not a simulated sub-turn value. Projection only, not a guarantee.", _labelStyle);

            // Each line's color follows UiPalette's single green-good/red-bad convention, honoring
            // which direction is actually good for that specific stat (e.g. Unemployment/Inflation/
            // Poverty/Crime falling is the GOOD direction, the opposite of GDP/Approval/LFP rising).
            // Two columns, same "first half / second half" split as the dashboard's own headline
            // stats - halves this list's own height too.
            GUILayout.BeginHorizontal();
            GUILayout.BeginVertical();
            DrawColoredLabel($"GDP Growth: {_cachedGdpGrowthScaledText}", _labelStyle, UiPalette.GetDeltaColor(_cachedGdpGrowthPercentScaled, higherIsBetter: true));
            DrawColoredLabel($"Unemployment: {_cachedUnemploymentScaledText}", _labelStyle, UiPalette.GetDeltaColor(_cachedUnemploymentChangeScaled, higherIsBetter: false));
            DrawColoredLabel($"Inflation: {_cachedInflationScaledText}", _labelStyle, UiPalette.GetDeltaColor(_cachedInflationChangeScaled, higherIsBetter: false));
            DrawColoredLabel($"Approval: {_cachedApprovalScaledText}", _labelStyle, UiPalette.GetDeltaColor(_cachedApprovalChangeScaled, higherIsBetter: true));
            GUILayout.EndVertical();

            GUILayout.BeginVertical();
            DrawColoredLabel($"Poverty Rate: {_cachedPovertyRateScaledText}", _labelStyle, UiPalette.GetDeltaColor(_cachedPovertyRateChangeScaled, higherIsBetter: false));
            DrawColoredLabel($"Labor Force Participation: {_cachedLaborForceParticipationRateScaledText}", _labelStyle, UiPalette.GetDeltaColor(_cachedLaborForceParticipationRateChangeScaled, higherIsBetter: true));
            DrawColoredLabel($"Crime Index: {_cachedCrimeIndexScaledText}", _labelStyle, UiPalette.GetDeltaColor(_cachedCrimeIndexChangeScaled, higherIsBetter: false));
            DrawColoredLabel($"Net Budget Impact: {_cachedNetBudgetScaledText}", _labelStyle, UiPalette.GetDeltaColor(_cachedNetBudgetImpactScaled, higherIsBetter: true));
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
        }

        private void DrawHorizonButton(PreviewHorizon horizon)
        {
            bool selected = _previewHorizon == horizon;
            GUIStyle style = UiPalette.BuildButtonStyle(_neutralActionButtonStyle, selected ? UiPalette.ButtonKind.Primary : UiPalette.ButtonKind.Neutral);
            if (GUILayout.Button(GetHorizonLabel(horizon), style, GUILayout.ExpandWidth(false)))
            {
                _previewHorizon = horizon;
            }
        }

        /// <summary>True if no preview has been computed yet, the turn has advanced since the last one was, or any slider's value (including any tax line's requested rate change) differs from the snapshot the cached preview was computed from.</summary>
        private bool PolicyInputsChangedSinceLastPreview()
        {
            if (!_hasCachedPreview || _simulationManager.CurrentTurn != _cachedPreviewTurn)
            {
                return true;
            }

            if (GetHorizonDays(_previewHorizon) != _cachedPreviewHorizonDays)
            {
                return true;
            }

            bool nonTaxInputsChanged =
                !Mathf.Approximately(_interestRateChangeInput, _cachedInterestRateChangeInput)
                || !Mathf.Approximately(_tariffRateChangeInput, _cachedTariffRateChangeInput);

            if (nonTaxInputsChanged)
            {
                return true;
            }

            if (_playerCountry.MinimumWageImplemented
                && !Mathf.Approximately(
                    GetMinimumWageInput(_playerCountry.MinimumWagePercentOfMedian),
                    GetCachedMinimumWageInput(_playerCountry.MinimumWagePercentOfMedian)))
            {
                return true;
            }

            if (!Mathf.Approximately(
                    GetPoliceFundingInput(_playerCountry.PoliceFundingLevel),
                    GetCachedPoliceFundingInput(_playerCountry.PoliceFundingLevel))
                || !Mathf.Approximately(
                    GetSentencingSeverityInput(_playerCountry.SentencingSeverity),
                    GetCachedSentencingSeverityInput(_playerCountry.SentencingSeverity)))
            {
                return true;
            }

            if (!Mathf.Approximately(
                    GetBailReformInput(_playerCountry.BailReformLevel),
                    GetCachedBailReformInput(_playerCountry.BailReformLevel))
                || !Mathf.Approximately(
                    GetDrugPolicyInput(_playerCountry.DrugPolicyLevel),
                    GetCachedDrugPolicyInput(_playerCountry.DrugPolicyLevel)))
            {
                return true;
            }

            if (!Mathf.Approximately(
                    GetJudicialFundingInput(_playerCountry.JudicialFundingLevel),
                    GetCachedJudicialFundingInput(_playerCountry.JudicialFundingLevel))
                || !Mathf.Approximately(
                    GetBorderEnforcementInput(_playerCountry.BorderEnforcementLevel),
                    GetCachedBorderEnforcementInput(_playerCountry.BorderEnforcementLevel)))
            {
                return true;
            }

            if (!Mathf.Approximately(
                    GetFamilyPolicyInput(_playerCountry.FamilyPolicyLevel),
                    GetCachedFamilyPolicyInput(_playerCountry.FamilyPolicyLevel))
                || !Mathf.Approximately(
                    GetImmigrationPolicyInput(_playerCountry.ImmigrationPolicyLevel),
                    GetCachedImmigrationPolicyInput(_playerCountry.ImmigrationPolicyLevel)))
            {
                return true;
            }

            if (!Mathf.Approximately(
                    GetPaidFamilyLeaveWeeksInput(_playerCountry.PaidFamilyLeaveWeeks),
                    GetCachedPaidFamilyLeaveWeeksInput(_playerCountry.PaidFamilyLeaveWeeks))
                || !Mathf.Approximately(
                    GetOvertimeRegulationInput(_playerCountry.OvertimeRegulationLevel),
                    GetCachedOvertimeRegulationInput(_playerCountry.OvertimeRegulationLevel))
                || !Mathf.Approximately(
                    GetRetrainingProgramInput(_playerCountry.RetrainingProgramLevel),
                    GetCachedRetrainingProgramInput(_playerCountry.RetrainingProgramLevel)))
            {
                return true;
            }

            foreach (Sector sector in _playerCountry.Sectors)
            {
                if (!Mathf.Approximately(GetSectorSubsidyInput(sector.Type, sector.SubsidyLevel), GetCachedSectorSubsidyInput(sector.Type, sector.SubsidyLevel))
                    || !Mathf.Approximately(GetSectorRegulationInput(sector.Type, sector.RegulationLevel), GetCachedSectorRegulationInput(sector.Type, sector.RegulationLevel))
                    || !Mathf.Approximately(GetSectorTaxCreditInput(sector.Type, sector.TaxCreditLevel), GetCachedSectorTaxCreditInput(sector.Type, sector.TaxCreditLevel))
                    || !Mathf.Approximately(GetSectorResearchGrantsInput(sector.Type, sector.ResearchGrantsLevel), GetCachedSectorResearchGrantsInput(sector.Type, sector.ResearchGrantsLevel))
                    || !Mathf.Approximately(GetSectorDeregulationInput(sector.Type, sector.DeregulationNationalizationLevel), GetCachedSectorDeregulationInput(sector.Type, sector.DeregulationNationalizationLevel)))
                {
                    return true;
                }
            }

            if (_playerCountry.SovereignWealthFund != null)
            {
                SovereignWealthFund fund = _playerCountry.SovereignWealthFund;
                bool swfInputsChanged =
                    !Mathf.Approximately(GetSwfContributionRateInput(fund.ContributionRatePercent), GetCachedSwfContributionRateInput(fund.ContributionRatePercent))
                    || !Mathf.Approximately(GetSwfDomesticAllocationInput(fund.DomesticAllocationPercent), GetCachedSwfDomesticAllocationInput(fund.DomesticAllocationPercent))
                    || !Mathf.Approximately(GetSwfEquitiesWeightInput(fund.EquitiesWeight), GetCachedSwfEquitiesWeightInput(fund.EquitiesWeight))
                    || !Mathf.Approximately(GetSwfBondsWeightInput(fund.BondsWeight), GetCachedSwfBondsWeightInput(fund.BondsWeight))
                    || !Mathf.Approximately(GetSwfInfrastructureWeightInput(fund.InfrastructureWeight), GetCachedSwfInfrastructureWeightInput(fund.InfrastructureWeight))
                    || !Mathf.Approximately(GetSwfRealEstateWeightInput(fund.RealEstateWeight), GetCachedSwfRealEstateWeightInput(fund.RealEstateWeight));

                if (swfInputsChanged)
                {
                    return true;
                }
            }

            // Political Systems Overhaul Part B PILOT: a draft tax-rate change (unlike every other
            // input checked here) no longer changes what the preview would show at all - it only ever
            // reaches the simulation via a passed TaxBill - so this loop no longer needs a
            // GetTaxRateInput/GetCachedTaxRateInput change check.

            foreach (SpendingLine spendingLine in _playerCountry.SpendingLines)
            {
                if (!Mathf.Approximately(GetSpendingLineInput(spendingLine.Category), GetCachedSpendingLineInput(spendingLine.Category)))
                {
                    return true;
                }
            }

            foreach (TradePartner tradePartner in _playerCountry.TradePartners)
            {
                if (!tradePartner.HasPlayerTariffOverride)
                {
                    continue;
                }

                if (!Mathf.Approximately(
                    GetPartnerTariffInput(tradePartner.PartnerId, tradePartner.PlayerTariffOverride),
                    GetCachedPartnerTariffInput(tradePartner.PartnerId, tradePartner.PlayerTariffOverride)))
                {
                    return true;
                }
            }

            foreach (WelfareProgram welfareProgram in _playerCountry.WelfarePrograms)
            {
                if (!welfareProgram.IsImplemented)
                {
                    continue;
                }

                if (!Mathf.Approximately(
                    GetWelfareGenerosityInput(welfareProgram.Type, welfareProgram.GenerosityLevel),
                    GetCachedWelfareGenerosityInput(welfareProgram.Type, welfareProgram.GenerosityLevel)))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>Reruns PreviewTurn, re-rolls each figure's margin, and snapshots the slider values/turn number the result is now valid for.</summary>
        private void RecomputePolicyPreview()
        {
            PolicyPreview preview = _simulationManager.PreviewTurn(PlayerCountryId, BuildPlayerDecision());

            // Raw fields stay full-turn, UNCHANGED - DrawHeadlineGraphs' next-turn dashed projection
            // genuinely means "next turn" (121 days), independent of whatever horizon the preview
            // text panel below currently shows.
            _cachedGdpGrowthPercentRaw = preview.GdpGrowthPercent;
            _cachedUnemploymentChangeRaw = preview.UnemploymentChange;
            _cachedApprovalChangeRaw = preview.ApprovalChange;
            _cachedInflationChangeRaw = preview.InflationChange;
            _cachedPovertyRateChangeRaw = preview.PovertyRateChange;
            _cachedLaborForceParticipationRateChangeRaw = preview.LaborForceParticipationRateChange;
            _cachedCrimeIndexChangeRaw = preview.CrimeIndexChange;
            _cachedNetBudgetImpactRaw = preview.NetBudgetImpact;
            _cachedSwfReturnsEstimateRaw = preview.SwfReturnsEstimate;

            _cachedSwfContributionText = FormatEstimate(preview.SwfContributionEstimate, " units");
            _cachedSwfReturnsText = FormatEstimate(preview.SwfReturnsEstimate, " units");

            // Continuous Time Migration Phase 0: the live Policy Preview panel shows THIS horizon's
            // display-only re-scaling of the same full-turn PreviewTurn output above - see
            // ScaleLinearForDisplay/ScaleCompoundingForDisplay's own doc comments for why GDP growth
            // gets the compounding treatment and everything else (already a "points changed" or
            // dollar-amount figure) gets the linear one.
            int horizonDays = GetHorizonDays(_previewHorizon);
            _cachedGdpGrowthPercentScaled = ScaleCompoundingForDisplay(preview.GdpGrowthPercent, horizonDays);
            _cachedUnemploymentChangeScaled = ScaleLinearForDisplay(preview.UnemploymentChange, horizonDays);
            _cachedInflationChangeScaled = ScaleLinearForDisplay(preview.InflationChange, horizonDays);
            _cachedApprovalChangeScaled = ScaleLinearForDisplay(preview.ApprovalChange, horizonDays);
            _cachedPovertyRateChangeScaled = ScaleLinearForDisplay(preview.PovertyRateChange, horizonDays);
            _cachedLaborForceParticipationRateChangeScaled = ScaleLinearForDisplay(preview.LaborForceParticipationRateChange, horizonDays);
            _cachedCrimeIndexChangeScaled = ScaleLinearForDisplay(preview.CrimeIndexChange, horizonDays);
            _cachedNetBudgetImpactScaled = ScaleLinearForDisplay(preview.NetBudgetImpact, horizonDays);

            _cachedGdpGrowthScaledText = FormatEstimate(_cachedGdpGrowthPercentScaled, "%");
            _cachedUnemploymentScaledText = FormatEstimate(_cachedUnemploymentChangeScaled, " pts");
            _cachedInflationScaledText = FormatEstimate(_cachedInflationChangeScaled, " pts");
            _cachedApprovalScaledText = FormatEstimate(_cachedApprovalChangeScaled, " pts");
            _cachedPovertyRateScaledText = FormatEstimate(_cachedPovertyRateChangeScaled, " pts");
            _cachedLaborForceParticipationRateScaledText = FormatEstimate(_cachedLaborForceParticipationRateChangeScaled, " pts");
            _cachedCrimeIndexScaledText = FormatEstimate(_cachedCrimeIndexChangeScaled, " pts");
            _cachedNetBudgetScaledText = FormatEstimate(_cachedNetBudgetImpactScaled, " units");
            _cachedPreviewHorizonDays = horizonDays;

            _cachedInterestRateChangeInput = _interestRateChangeInput;
            _cachedTariffRateChangeInput = _tariffRateChangeInput;
            _cachedMinimumWageInput = _minimumWageInput;
            _cachedPoliceFundingInput = _policeFundingInput;
            _cachedSentencingSeverityInput = _sentencingSeverityInput;
            _cachedBailReformInput = _bailReformInput;
            _cachedDrugPolicyInput = _drugPolicyInput;
            _cachedJudicialFundingInput = _judicialFundingInput;
            _cachedBorderEnforcementInput = _borderEnforcementInput;
            _cachedFamilyPolicyInput = _familyPolicyInput;
            _cachedImmigrationPolicyInput = _immigrationPolicyInput;
            _cachedPaidFamilyLeaveWeeksInput = _paidFamilyLeaveWeeksInput;
            _cachedOvertimeRegulationInput = _overtimeRegulationInput;
            _cachedRetrainingProgramInput = _retrainingProgramInput;
            _cachedSwfContributionRateInput = _swfContributionRateInput;
            _cachedSwfDomesticAllocationInput = _swfDomesticAllocationInput;
            _cachedSwfEquitiesWeightInput = _swfEquitiesWeightInput;
            _cachedSwfBondsWeightInput = _swfBondsWeightInput;
            _cachedSwfInfrastructureWeightInput = _swfInfrastructureWeightInput;
            _cachedSwfRealEstateWeightInput = _swfRealEstateWeightInput;

            _cachedSectorSubsidyInputs.Clear();
            foreach (KeyValuePair<SectorType, float> kvp in _sectorSubsidyInputs)
            {
                _cachedSectorSubsidyInputs[kvp.Key] = kvp.Value;
            }

            _cachedSectorRegulationInputs.Clear();
            foreach (KeyValuePair<SectorType, float> kvp in _sectorRegulationInputs)
            {
                _cachedSectorRegulationInputs[kvp.Key] = kvp.Value;
            }

            _cachedSectorTaxCreditInputs.Clear();
            foreach (KeyValuePair<SectorType, float> kvp in _sectorTaxCreditInputs)
            {
                _cachedSectorTaxCreditInputs[kvp.Key] = kvp.Value;
            }

            _cachedSectorResearchGrantsInputs.Clear();
            foreach (KeyValuePair<SectorType, float> kvp in _sectorResearchGrantsInputs)
            {
                _cachedSectorResearchGrantsInputs[kvp.Key] = kvp.Value;
            }

            _cachedSectorDeregulationInputs.Clear();
            foreach (KeyValuePair<SectorType, float> kvp in _sectorDeregulationInputs)
            {
                _cachedSectorDeregulationInputs[kvp.Key] = kvp.Value;
            }

            _cachedTaxRateInputs.Clear();
            foreach (KeyValuePair<TaxType, float> kvp in _taxRateInputs)
            {
                _cachedTaxRateInputs[kvp.Key] = kvp.Value;
            }

            _cachedSpendingLineInputs.Clear();
            foreach (KeyValuePair<SpendingCategory, float> kvp in _spendingLineInputs)
            {
                _cachedSpendingLineInputs[kvp.Key] = kvp.Value;
            }

            _cachedPartnerTariffInputs.Clear();
            foreach (KeyValuePair<CountryId, float> kvp in _partnerTariffInputs)
            {
                _cachedPartnerTariffInputs[kvp.Key] = kvp.Value;
            }

            _cachedWelfareGenerosityInputs.Clear();
            foreach (KeyValuePair<WelfareProgramType, float> kvp in _welfareGenerosityInputs)
            {
                _cachedWelfareGenerosityInputs[kvp.Key] = kvp.Value;
            }

            _cachedPreviewTurn = _simulationManager.CurrentTurn;
            _hasCachedPreview = true;
        }

        /// <summary>Formats one estimated figure as "value +- margin", where margin is value's magnitude times a freshly-rolled 5-10% - a range reading, not a precise number, same as any real economic forecast.</summary>
        private string FormatEstimate(float value, string unitSuffix)
        {
            float marginPercent = MinPreviewMarginPercent + (float)_previewRandom.NextDouble() * (MaxPreviewMarginPercent - MinPreviewMarginPercent);
            float marginAmount = Mathf.Abs(value) * marginPercent / 100f;
            return $"{value:+0.00;-0.00;0}{unitSuffix} (±{marginAmount:0.00}{unitSuffix})";
        }

        /// <summary>
        /// Continuous Time Migration Phase 0: replaces the old single "Advance Turn" button - a date
        /// readout (plus a status line while the clock is paused for a reason other than the player's
        /// own Pause choice) and Pause/1x/2x/3x speed buttons, mirroring the tab bar's own
        /// selected-vs-unselected visual idiom (UiPalette.BuildButtonStyle's Primary kind for whichever
        /// speed is currently active, Neutral for the rest) rather than inventing a new button-state
        /// convention just for this row.
        ///
        /// Persistent, unmissable pause indicator (POLISIM_MASTER_ROADMAP.md working discipline's
        /// fifth failure pattern, "background/timed state mutation vs. active UI interaction" -
        /// investigated after a reported freeze that the IMGUI stable-control-layout fix, commit
        /// adb34ae, did NOT resolve). This is drawn in OnGUI's pinned-outside-scroll-view slot (see
        /// OnGUI), so it's the ONE piece of UI guaranteed visible from every tab at any scroll
        /// position, at any time. That matters because all three systems that can legitimately pause
        /// Update's day-loop - Fed Chair term appointment, a Cabinet decision, a Foreign Policy
        /// meeting - render their ACTUAL resolution UI (the candidate picker / DrawCabinetDecisionModal
        /// / DrawForeignPolicyMeetingModal) only inside their own specific tab's draw call. A player
        /// on, say, Tax Policy when one of these fires sees nothing wrong except that simulated days
        /// silently stop advancing - indistinguishable from a hang unless they happen to check this
        /// exact line and then navigate to the right tab. Before this fix, that line existed for Fed
        /// Chair and Cabinet only (a modest _labelStyle line, easy to miss) and said NOTHING at all for
        /// a pending Foreign Policy meeting - the one of the three most likely to fire early in a
        /// fresh session, since it rolls per DAY (~1%) rather than per 121-day TURN like the other two.
        /// Now: exactly one Label control either way (per DrawTaxPolicy's stable-control-layout
        /// pattern - content and style vary, the control itself never does), escalated to
        /// _eventBannerStyle (the same bold/orange weight as the dashboard's own BREAKING banner)
        /// whenever ANY of the three is true, always naming which one and which tab resolves it.
        ///
        /// Master Sequence step 5a adds a fourth condition, hasPendingBudgetProcess, per the revised
        /// Part B design's explicit "extend the existing global banner, don't build a fourth ad-hoc
        /// pause system" instruction. It also adds one ALWAYS-PRESENT "Acknowledge" button beneath the
        /// status line - per DrawTaxPolicy's stable-control-layout pattern, the button itself is
        /// emitted every frame regardless of hasPendingBudgetProcess (GUI.enabled gates whether it's
        /// interactive, composed with the ambient enabled state), never conditionally added/removed,
        /// since THIS specific screen (many sliders, a continuously-recomputing live vote estimate) is
        /// exactly the shape the last real freeze came from. This button is a TEMPORARY 5a-only
        /// placeholder (see SimulationManager.AcknowledgeBudgetProcess's own doc comment) - step 5c
        /// must replace it with the real Budget Process screen entry point once that exists.
        /// </summary>
        private void DrawCalendarAndSpeedControls(bool hasPendingFedChairSelection, bool hasPendingCabinetDecisions, bool hasPendingForeignPolicyMeeting, bool hasPendingBudgetProcess)
        {
            GUILayout.BeginVertical();

            string dateText = _simulationManager.CurrentDate.ToString("MMMM d, yyyy");
            bool isPaused = hasPendingFedChairSelection || hasPendingCabinetDecisions || hasPendingForeignPolicyMeeting || hasPendingBudgetProcess;

            // Priority order matches Update's own pause-gate check order exactly, so this always names
            // whichever reason is actually the one currently blocking AdvanceDay.
            string statusText = dateText;
            if (hasPendingFedChairSelection)
            {
                statusText = $"{dateText} - TIME PAUSED: choose the next Fed Chair (Federal Reserve tab) to continue.";
            }
            else if (hasPendingCabinetDecisions)
            {
                statusText = $"{dateText} - TIME PAUSED: resolve the pending Cabinet decision (Cabinet tab) to continue.";
            }
            else if (hasPendingForeignPolicyMeeting)
            {
                statusText = $"{dateText} - TIME PAUSED: respond to the pending Foreign Policy meeting (Foreign Policy tab) to continue.";
            }
            else if (hasPendingBudgetProcess)
            {
                statusText = $"{dateText} - TIME PAUSED: the annual budget process is open (Budget Process screen not built yet - acknowledge below to continue for now).";
            }
            GUILayout.Label(statusText, isPaused ? _eventBannerStyle : _labelStyle);

            bool ambientEnabled = GUI.enabled;
            GUI.enabled = ambientEnabled && hasPendingBudgetProcess;
            if (GUILayout.Button("Acknowledge budget process (temporary - real screen lands in step 5b/5c)", _neutralActionButtonStyle))
            {
                _simulationManager.AcknowledgeBudgetProcess(PlayerCountryId);
            }
            GUI.enabled = ambientEnabled;

            GUILayout.BeginHorizontal();
            DrawSpeedButton("Pause", GameSpeed.Paused);
            DrawSpeedButton("1x", GameSpeed.Normal);
            DrawSpeedButton("2x", GameSpeed.Fast);
            DrawSpeedButton("3x", GameSpeed.VeryFast);
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();
        }

        private void DrawSpeedButton(string label, GameSpeed speed)
        {
            bool selected = _gameSpeed == speed;
            GUIStyle style = UiPalette.BuildButtonStyle(_buttonStyle, selected ? UiPalette.ButtonKind.Primary : UiPalette.ButtonKind.Neutral);
            if (GUILayout.Button(label, style))
            {
                _gameSpeed = speed;
            }
        }

        private void AdvanceTurn()
        {
            var decisions = new Dictionary<CountryId, PolicyDecision>();
            foreach (Country country in _world.Countries)
            {
                decisions[country.Id] = country.Id == PlayerCountryId ? BuildPlayerDecision() : PolicyDecision.None();
            }

            _simulationManager.AdvanceTurn(decisions);

            EconomyState state = _playerCountry.State;
            _lastGrowthPercent = (state.GDP - _prevGdp) / Mathf.Max(_prevGdp, 1f) * 100f;
            _prevGdp = state.GDP;

            AppendLogEntry(state);
            RecordMapEventMarkers();
            ResetPolicyInputs();
            CheckElection();
        }

        /// <summary>
        /// Records this turn's fired event (if any) for every country as a MapEventMarker for the
        /// World Map tab, then prunes any marker old enough to have fully faded (see
        /// EventMarkerFadeTurns) - SimulationManager.GetLastEvent only ever exposes the CURRENT
        /// turn's event, so this is the only place a rolling, fading history exists.
        /// </summary>
        private void RecordMapEventMarkers()
        {
            foreach (Country country in _world.Countries)
            {
                EconomicEvent economicEvent = _simulationManager.GetLastEvent(country.Id);
                if (economicEvent != null)
                {
                    _mapEventMarkers.Add(new MapEventMarker(country.Id, economicEvent, _simulationManager.CurrentTurn));
                }
            }

            _mapEventMarkers.RemoveAll(marker => _simulationManager.CurrentTurn - marker.TurnFired >= EventMarkerFadeTurns);
        }

        /// <summary>The tax-policy tab's draft absolute rate for a TaxType, or <paramref name="fallbackRate"/> (the TaxLine's actual persisted Rate) if the player hasn't touched that slider this turn.</summary>
        private float GetTaxRateInput(TaxType type, float fallbackRate)
        {
            return _taxRateInputs.TryGetValue(type, out float value) ? value : fallbackRate;
        }

        private bool GetTaxImplementDraft(TaxType type, bool fallbackIsImplemented)
        {
            return _taxImplementDrafts.TryGetValue(type, out bool value) ? value : fallbackIsImplemented;
        }

        private float GetCachedTaxRateInput(TaxType type, float fallbackRate)
        {
            return _cachedTaxRateInputs.TryGetValue(type, out float value) ? value : fallbackRate;
        }

        /// <summary>The Minimum Wage slider's draft absolute level, or <paramref name="fallbackLevel"/> (the country's actual persisted MinimumWagePercentOfMedian) if the player hasn't touched it this turn.</summary>
        private float GetMinimumWageInput(float fallbackLevel)
        {
            return _minimumWageInput ?? fallbackLevel;
        }

        private float GetCachedMinimumWageInput(float fallbackLevel)
        {
            return _cachedMinimumWageInput ?? fallbackLevel;
        }

        private float GetPaidFamilyLeaveWeeksInput(float fallbackLevel) => _paidFamilyLeaveWeeksInput ?? fallbackLevel;
        private float GetCachedPaidFamilyLeaveWeeksInput(float fallbackLevel) => _cachedPaidFamilyLeaveWeeksInput ?? fallbackLevel;
        private float GetOvertimeRegulationInput(float fallbackLevel) => _overtimeRegulationInput ?? fallbackLevel;
        private float GetCachedOvertimeRegulationInput(float fallbackLevel) => _cachedOvertimeRegulationInput ?? fallbackLevel;
        private float GetRetrainingProgramInput(float fallbackLevel) => _retrainingProgramInput ?? fallbackLevel;
        private float GetCachedRetrainingProgramInput(float fallbackLevel) => _cachedRetrainingProgramInput ?? fallbackLevel;

        /// <summary>The Police Funding slider's draft absolute level, or <paramref name="fallbackLevel"/> (the country's actual persisted PoliceFundingLevel) if the player hasn't touched it this turn.</summary>
        private float GetPoliceFundingInput(float fallbackLevel)
        {
            return _policeFundingInput ?? fallbackLevel;
        }

        private float GetCachedPoliceFundingInput(float fallbackLevel)
        {
            return _cachedPoliceFundingInput ?? fallbackLevel;
        }

        /// <summary>The Sentencing Severity slider's draft absolute level, or <paramref name="fallbackLevel"/> (the country's actual persisted SentencingSeverity) if the player hasn't touched it this turn.</summary>
        private float GetSentencingSeverityInput(float fallbackLevel)
        {
            return _sentencingSeverityInput ?? fallbackLevel;
        }

        private float GetCachedSentencingSeverityInput(float fallbackLevel)
        {
            return _cachedSentencingSeverityInput ?? fallbackLevel;
        }

        private float GetBailReformInput(float fallbackLevel) => _bailReformInput ?? fallbackLevel;
        private float GetCachedBailReformInput(float fallbackLevel) => _cachedBailReformInput ?? fallbackLevel;
        private float GetDrugPolicyInput(float fallbackLevel) => _drugPolicyInput ?? fallbackLevel;
        private float GetCachedDrugPolicyInput(float fallbackLevel) => _cachedDrugPolicyInput ?? fallbackLevel;
        private float GetJudicialFundingInput(float fallbackLevel) => _judicialFundingInput ?? fallbackLevel;
        private float GetCachedJudicialFundingInput(float fallbackLevel) => _cachedJudicialFundingInput ?? fallbackLevel;
        private float GetBorderEnforcementInput(float fallbackLevel) => _borderEnforcementInput ?? fallbackLevel;
        private float GetCachedBorderEnforcementInput(float fallbackLevel) => _cachedBorderEnforcementInput ?? fallbackLevel;

        private float GetFamilyPolicyInput(float fallbackLevel) => _familyPolicyInput ?? fallbackLevel;
        private float GetCachedFamilyPolicyInput(float fallbackLevel) => _cachedFamilyPolicyInput ?? fallbackLevel;
        private float GetImmigrationPolicyInput(float fallbackLevel) => _immigrationPolicyInput ?? fallbackLevel;
        private float GetCachedImmigrationPolicyInput(float fallbackLevel) => _cachedImmigrationPolicyInput ?? fallbackLevel;

        /// <summary>The Economic Sectors tab's draft absolute Subsidy level for a SectorType, or <paramref name="fallbackLevel"/> (the Sector's actual persisted SubsidyLevel) if the player hasn't touched that slider this turn.</summary>
        private float GetSectorSubsidyInput(SectorType type, float fallbackLevel)
        {
            return _sectorSubsidyInputs.TryGetValue(type, out float value) ? value : fallbackLevel;
        }

        private float GetCachedSectorSubsidyInput(SectorType type, float fallbackLevel)
        {
            return _cachedSectorSubsidyInputs.TryGetValue(type, out float value) ? value : fallbackLevel;
        }

        /// <summary>The Economic Sectors tab's draft absolute Regulation level for a SectorType, or <paramref name="fallbackLevel"/> (the Sector's actual persisted RegulationLevel) if the player hasn't touched that slider this turn.</summary>
        private float GetSectorRegulationInput(SectorType type, float fallbackLevel)
        {
            return _sectorRegulationInputs.TryGetValue(type, out float value) ? value : fallbackLevel;
        }

        private float GetCachedSectorRegulationInput(SectorType type, float fallbackLevel)
        {
            return _cachedSectorRegulationInputs.TryGetValue(type, out float value) ? value : fallbackLevel;
        }

        /// <summary>Round 3 item 2: the Economic Sectors tab's draft absolute Tax Credit level for a SectorType, or <paramref name="fallbackLevel"/> (the Sector's actual persisted TaxCreditLevel) if the player hasn't touched that slider this turn.</summary>
        private float GetSectorTaxCreditInput(SectorType type, float fallbackLevel)
        {
            return _sectorTaxCreditInputs.TryGetValue(type, out float value) ? value : fallbackLevel;
        }

        private float GetCachedSectorTaxCreditInput(SectorType type, float fallbackLevel)
        {
            return _cachedSectorTaxCreditInputs.TryGetValue(type, out float value) ? value : fallbackLevel;
        }

        /// <summary>Round 3 item 2: the Economic Sectors tab's draft absolute Research Grants level for a SectorType, or <paramref name="fallbackLevel"/> (the Sector's actual persisted ResearchGrantsLevel) if the player hasn't touched that slider this turn.</summary>
        private float GetSectorResearchGrantsInput(SectorType type, float fallbackLevel)
        {
            return _sectorResearchGrantsInputs.TryGetValue(type, out float value) ? value : fallbackLevel;
        }

        private float GetCachedSectorResearchGrantsInput(SectorType type, float fallbackLevel)
        {
            return _cachedSectorResearchGrantsInputs.TryGetValue(type, out float value) ? value : fallbackLevel;
        }

        /// <summary>Round 3 item 2: the Economic Sectors tab's draft absolute Deregulation/Nationalization level for a SectorType, or <paramref name="fallbackLevel"/> (the Sector's actual persisted DeregulationNationalizationLevel) if the player hasn't touched that slider this turn.</summary>
        private float GetSectorDeregulationInput(SectorType type, float fallbackLevel)
        {
            return _sectorDeregulationInputs.TryGetValue(type, out float value) ? value : fallbackLevel;
        }

        private float GetCachedSectorDeregulationInput(SectorType type, float fallbackLevel)
        {
            return _cachedSectorDeregulationInputs.TryGetValue(type, out float value) ? value : fallbackLevel;
        }

        private float GetSwfContributionRateInput(float fallbackLevel) => _swfContributionRateInput ?? fallbackLevel;
        private float GetCachedSwfContributionRateInput(float fallbackLevel) => _cachedSwfContributionRateInput ?? fallbackLevel;
        private float GetSwfDomesticAllocationInput(float fallbackLevel) => _swfDomesticAllocationInput ?? fallbackLevel;
        private float GetCachedSwfDomesticAllocationInput(float fallbackLevel) => _cachedSwfDomesticAllocationInput ?? fallbackLevel;
        private float GetSwfEquitiesWeightInput(float fallbackLevel) => _swfEquitiesWeightInput ?? fallbackLevel;
        private float GetCachedSwfEquitiesWeightInput(float fallbackLevel) => _cachedSwfEquitiesWeightInput ?? fallbackLevel;
        private float GetSwfBondsWeightInput(float fallbackLevel) => _swfBondsWeightInput ?? fallbackLevel;
        private float GetCachedSwfBondsWeightInput(float fallbackLevel) => _cachedSwfBondsWeightInput ?? fallbackLevel;
        private float GetSwfInfrastructureWeightInput(float fallbackLevel) => _swfInfrastructureWeightInput ?? fallbackLevel;
        private float GetCachedSwfInfrastructureWeightInput(float fallbackLevel) => _cachedSwfInfrastructureWeightInput ?? fallbackLevel;
        private float GetSwfRealEstateWeightInput(float fallbackLevel) => _swfRealEstateWeightInput ?? fallbackLevel;
        private float GetCachedSwfRealEstateWeightInput(float fallbackLevel) => _cachedSwfRealEstateWeightInput ?? fallbackLevel;

        /// <summary>The Welfare Policy tab's draft absolute GenerosityLevel for a WelfareProgramType, or <paramref name="fallbackGenerosity"/> (the WelfareProgram's actual persisted GenerosityLevel) if the player hasn't touched that slider this turn.</summary>
        private float GetWelfareGenerosityInput(WelfareProgramType type, float fallbackGenerosity)
        {
            return _welfareGenerosityInputs.TryGetValue(type, out float value) ? value : fallbackGenerosity;
        }

        private float GetCachedWelfareGenerosityInput(WelfareProgramType type, float fallbackGenerosity)
        {
            return _cachedWelfareGenerosityInputs.TryGetValue(type, out float value) ? value : fallbackGenerosity;
        }

        /// <summary>The Trade tab's draft absolute override rate for a partner, or <paramref name="fallbackRate"/> (the TradePartner's actual persisted PlayerTariffOverride) if the player hasn't touched that slider this turn.</summary>
        private float GetPartnerTariffInput(CountryId partnerId, float fallbackRate)
        {
            return _partnerTariffInputs.TryGetValue(partnerId, out float value) ? value : fallbackRate;
        }

        private float GetCachedPartnerTariffInput(CountryId partnerId, float fallbackRate)
        {
            return _cachedPartnerTariffInputs.TryGetValue(partnerId, out float value) ? value : fallbackRate;
        }

        /// <summary>The Spending Policy tab's draft PERCENTAGE change for a SpendingCategory this turn (Mandatory or Discretionary), or 0 if the player hasn't touched that slider.</summary>
        private float GetSpendingLineInput(SpendingCategory category)
        {
            return _spendingLineInputs.TryGetValue(category, out float value) ? value : 0f;
        }

        private float GetCachedSpendingLineInput(SpendingCategory category)
        {
            return _cachedSpendingLineInputs.TryGetValue(category, out float value) ? value : 0f;
        }

        private PolicyDecision BuildPlayerDecision()
        {
            var decision = new PolicyDecision
            {
                InterestRateChange = _interestRateChangeInput,
                TariffRateChange = _tariffRateChangeInput
            };

            // Only meaningful if the country has a statutory minimum wage at all - GetMinimumWageInput's
            // fallback already makes an untouched slider a no-op, so this can be included unconditionally
            // whenever MinimumWageImplemented is true (mirrors TaxRateOverrides' "always safe" reasoning).
            if (_playerCountry.MinimumWageImplemented)
            {
                decision.MinimumWageOverride = GetMinimumWageInput(_playerCountry.MinimumWagePercentOfMedian);
            }

            decision.PoliceFundingOverride = GetPoliceFundingInput(_playerCountry.PoliceFundingLevel);
            decision.SentencingSeverityOverride = GetSentencingSeverityInput(_playerCountry.SentencingSeverity);
            decision.BailReformOverride = GetBailReformInput(_playerCountry.BailReformLevel);
            decision.DrugPolicyOverride = GetDrugPolicyInput(_playerCountry.DrugPolicyLevel);
            decision.JudicialFundingOverride = GetJudicialFundingInput(_playerCountry.JudicialFundingLevel);
            decision.BorderEnforcementOverride = GetBorderEnforcementInput(_playerCountry.BorderEnforcementLevel);
            decision.FamilyPolicyOverride = GetFamilyPolicyInput(_playerCountry.FamilyPolicyLevel);
            decision.ImmigrationPolicyOverride = GetImmigrationPolicyInput(_playerCountry.ImmigrationPolicyLevel);

            decision.PaidFamilyLeaveWeeksOverride = GetPaidFamilyLeaveWeeksInput(_playerCountry.PaidFamilyLeaveWeeks);
            decision.OvertimeRegulationOverride = GetOvertimeRegulationInput(_playerCountry.OvertimeRegulationLevel);
            decision.RetrainingProgramOverride = GetRetrainingProgramInput(_playerCountry.RetrainingProgramLevel);

            foreach (Sector sector in _playerCountry.Sectors)
            {
                decision.SectorSubsidyOverrides[sector.Type] = GetSectorSubsidyInput(sector.Type, sector.SubsidyLevel);
                decision.SectorRegulationOverrides[sector.Type] = GetSectorRegulationInput(sector.Type, sector.RegulationLevel);
                decision.SectorTaxCreditOverrides[sector.Type] = GetSectorTaxCreditInput(sector.Type, sector.TaxCreditLevel);
                decision.SectorResearchGrantsOverrides[sector.Type] = GetSectorResearchGrantsInput(sector.Type, sector.ResearchGrantsLevel);
                decision.SectorDeregulationNationalizationOverrides[sector.Type] = GetSectorDeregulationInput(sector.Type, sector.DeregulationNationalizationLevel);
            }

            if (_playerCountry.SovereignWealthFund != null)
            {
                SovereignWealthFund fund = _playerCountry.SovereignWealthFund;
                decision.SwfContributionRateOverride = GetSwfContributionRateInput(fund.ContributionRatePercent);
                decision.SwfDomesticAllocationOverride = GetSwfDomesticAllocationInput(fund.DomesticAllocationPercent);
                decision.SwfEquitiesWeightOverride = GetSwfEquitiesWeightInput(fund.EquitiesWeight);
                decision.SwfBondsWeightOverride = GetSwfBondsWeightInput(fund.BondsWeight);
                decision.SwfInfrastructureWeightOverride = GetSwfInfrastructureWeightInput(fund.InfrastructureWeight);
                decision.SwfRealEstateWeightOverride = GetSwfRealEstateWeightInput(fund.RealEstateWeight);
            }

            // Political Systems Overhaul Part B PILOT (Master Sequence step 4): Tax Policy no longer
            // feeds PolicyDecision.TaxRateOverrides at all - draft tax changes only ever reach the
            // simulation via a PASSED TaxBill (see ParliamentSystem.ApplyBillResult, which writes
            // directly to TaxLine.Rate/IsImplemented). This is the ONE call site both AdvanceTurn and
            // the live preview share, so removing it here makes the preview honest too (it no longer
            // shows a tax-driven effect that won't actually happen until a bill passes).

            // Both Mandatory and Discretionary lines are adjustable - SimulationManager clamps each
            // to its own percentage range (narrower for Mandatory).
            foreach (SpendingLine spendingLine in _playerCountry.SpendingLines)
            {
                float percent = GetSpendingLineInput(spendingLine.Category);
                if (percent != 0f)
                {
                    decision.SpendingLineChanges[spendingLine.Category] = percent;
                }
            }

            // Only a partner with an ACTIVE override gets an entry - mirrors "only currently-
            // implemented tax lines get an override" above. A partner without one is left alone
            // entirely (ApplyPartnerTariffOverrides is a no-op with no entry), so an untouched
            // partner's tariff keeps resolving dynamically from bloc/base-rate logic rather than
            // silently getting pinned to whatever its current effective rate happens to be.
            foreach (TradePartner tradePartner in _playerCountry.TradePartners)
            {
                if (!tradePartner.HasPlayerTariffOverride)
                {
                    continue;
                }

                decision.PartnerTariffOverrides[tradePartner.PartnerId] = GetPartnerTariffInput(tradePartner.PartnerId, tradePartner.PlayerTariffOverride);
            }

            // Only currently-implemented programs get an override - same reasoning as TaxRateOverrides
            // above (GetWelfareGenerosityInput's fallback already makes an untouched slider a no-op,
            // so every implemented program can be included unconditionally).
            foreach (WelfareProgram welfareProgram in _playerCountry.WelfarePrograms)
            {
                if (!welfareProgram.IsImplemented)
                {
                    continue;
                }

                decision.WelfareGenerosityOverrides[welfareProgram.Type] = GetWelfareGenerosityInput(welfareProgram.Type, welfareProgram.GenerosityLevel);
            }

            return decision;
        }

        private void ResetPolicyInputs()
        {
            // _taxRateInputs is deliberately NOT cleared here - it holds each tax's absolute rate
            // (not a per-turn delta), and after AdvanceTurn commits it, TaxLine.Rate already equals
            // whatever was in the draft, so leaving it in place keeps the slider showing the same
            // (now-persisted) value instead of snapping back to 0. _spendingLineInputs IS cleared -
            // it's a this-turn delta (like the legacy category sliders it replaces), not an absolute
            // setting; SpendingLine.Amount itself is what persists. _partnerTariffInputs is also
            // deliberately NOT cleared, for the same reason as _taxRateInputs - it holds each
            // overridden partner's absolute rate, which TradePartner.PlayerTariffOverride already
            // equals once committed.
            _spendingLineInputs.Clear();
            _interestRateChangeInput = 0f;
            _tariffRateChangeInput = 0f;
        }

        /// <summary>Checks the player's country against ElectionSystem on election turns and, if this is one, stores the result for DrawElectionResultsScreen to reveal (see OnGUI's gate) rather than resolving the game-over state silently in the background - the actual game-rule check (ElectionSystem.RunElection) is unchanged, this just gives it a real presentation.</summary>
        private void CheckElection()
        {
            if (!ElectionSystem.IsElectionTurn(_simulationManager.CurrentTurn))
            {
                return;
            }

            _pendingElectionResult = ElectionSystem.RunElection(_playerCountry.State);
            _pendingElectionTurn = _simulationManager.CurrentTurn;
        }

        /// <summary>Called when the player dismisses the election reveal screen - only NOW does a loss actually set the game-over state (a win just returns to the dashboard).</summary>
        private void DismissElectionResult()
        {
            if (_pendingElectionResult != null && !_pendingElectionResult.Won)
            {
                _isGameOver = true;
                _gameOverReason = $"Lost re-election at turn {_pendingElectionTurn} with {_pendingElectionResult.ApprovalAtElection:F1} approval " +
                    $"(needed at least {ElectionSystem.LosingThreshold:F0}).";
            }
            _pendingElectionResult = null;
        }

        /// <summary>
        /// Election reveal screen: a real full-screen presentation of ElectionSystem's own existing
        /// win/lose logic (unchanged - see CheckElection/ElectionSystem.RunElection), replacing the
        /// previous silent background check. Mirrors DrawCountrySelector's own full-screen centered
        /// layout for consistency. The approval bar reuses UiPalette.DrawBar with a threshold marker
        /// (see UiPalette.DrawBarWithThreshold) rather than inventing a new bar primitive.
        /// </summary>
        private void DrawElectionResultsScreen(ElectionResult result)
        {
            GUILayout.BeginArea(new Rect(0f, 0f, Screen.width, Screen.height));
            GUILayout.FlexibleSpace();
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            GUILayout.BeginVertical(GUILayout.Width(Screen.width * 0.4f));

            Color outcomeColor = result.Won ? UiPalette.PositiveChangeColor : UiPalette.NegativeChangeColor;
            // _gameOverStyle's own base text color is a hardcoded Color.red (see its declaration) -
            // DrawColoredLabel's GUI.color trick MULTIPLIES against that base color, so tinting it
            // green for a win would multiply red x green and produce a muddy dark red, not green.
            // Cloning the style and overriding its text color directly (not multiplicatively) avoids
            // that, while still reusing _gameOverStyle's own large/bold banner sizing.
            var electionBannerStyle = new GUIStyle(_gameOverStyle) { };
            electionBannerStyle.normal.textColor = outcomeColor;
            GUILayout.Label(result.Won ? "RE-ELECTED" : "ELECTION LOST", electionBannerStyle);
            GUILayout.Label($"Turn {_pendingElectionTurn} Election - {_playerCountry.Name}", _labelStyle);
            GUILayout.Space(16f);

            GUILayout.Label($"Approval Rating: {result.ApprovalAtElection:F1} (needed {ElectionSystem.LosingThreshold:F0} to win)", _labelStyle);
            UiPalette.DrawBarWithThreshold(result.ApprovalAtElection / 100f, ElectionSystem.LosingThreshold / 100f, outcomeColor, 24f);
            GUILayout.Label($"Margin: {result.Margin:+0.0;-0.0}", _labelStyle);

            GUILayout.Space(24f);
            GUIStyle continueStyle = UiPalette.BuildButtonStyle(_buttonStyle, UiPalette.ButtonKind.Primary);
            if (GUILayout.Button("Continue", continueStyle))
            {
                DismissElectionResult();
            }

            GUILayout.EndVertical();
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.EndArea();
        }

        private void AppendLogEntry(EconomyState state)
        {
            _turnLog.Add($"Turn {_simulationManager.CurrentTurn}: GDP={state.GDP:F1} ({_lastGrowthPercent:+0.00;-0.00;0}%), " +
                $"Unemp={state.Unemployment:F2}%, Infl={state.Inflation:F2}%, Approval={state.ApprovalRating:F1}, Debt/GDP={state.DebtToGdpRatio:F1}%");

            while (_turnLog.Count > MaxLogEntries)
            {
                _turnLog.RemoveAt(0);
            }
        }

        /// <summary>
        /// Tab/toggle set for the right column - 11 tabs now (Phase 4 gave Federal Reserve/Labor
        /// Market/Crime &amp; Justice/Infrastructure their own homes, split out of the dashboard and
        /// the old combined Trade &amp; Spending tab), split into two rows since that many no longer
        /// fit legibly in one. Each tab is tinted by its own SystemArea (see GetTabArea) - selected
        /// uses the bright TabSelected variant, unselected the dimmer Tab variant, so the currently-
        /// open tab reads as visibly "lit up" in its own area's hue.
        /// </summary>
        private const int TabsPerRow = 6;

        /// <summary>
        /// Each button previously auto-sized to its own label content (no explicit width), which
        /// GUILayout never shrinks to fit - 6 buttons per row summing wider than the actual available
        /// column width (a real risk at smaller window sizes, since column width is itself only a
        /// FRACTION of Screen.width, not a fixed budget) just overflowed silently past the panel/
        /// screen edge instead of wrapping. Now explicitly divided evenly across
        /// <paramref name="availableWidth"/> - the SAME rightColumnWidth OnGUI already computes fresh
        /// from Screen.width every frame - so the row can never exceed its actual budget at any
        /// window size, matching the screen-relative approach already used everywhere else in this
        /// class.
        /// </summary>
        private void DrawRightColumnTabs(float availableWidth)
        {
            float buttonWidth = availableWidth / TabsPerRow;

            GUILayout.BeginHorizontal();
            DrawRightColumnTabButton("Recent Turns", RightPanelTab.RecentTurns, buttonWidth);
            DrawRightColumnTabButton("World Map", RightPanelTab.WorldMap, buttonWidth);
            DrawRightColumnTabButton("Trade", RightPanelTab.Trade, buttonWidth);
            DrawRightColumnTabButton("Tax Policy", RightPanelTab.TaxPolicy, buttonWidth);
            DrawRightColumnTabButton("Spending Policy", RightPanelTab.SpendingPolicy, buttonWidth);
            DrawRightColumnTabButton(GetCentralBankName(PlayerCountryId), RightPanelTab.FederalReserve, buttonWidth);
            GUILayout.EndHorizontal();

            GUILayout.Space(TabRowSpacing);

            GUILayout.BeginHorizontal();
            DrawRightColumnTabButton("Welfare Policy", RightPanelTab.WelfarePolicy, buttonWidth);
            DrawRightColumnTabButton("Labor Market", RightPanelTab.LaborMarket, buttonWidth);
            DrawRightColumnTabButton("Crime & Justice", RightPanelTab.CrimeJustice, buttonWidth);
            DrawRightColumnTabButton("Economic Sectors", RightPanelTab.SectorPolicy, buttonWidth);
            DrawRightColumnTabButton("Infrastructure", RightPanelTab.Infrastructure, buttonWidth);
            DrawRightColumnTabButton("Sovereign Wealth Fund", RightPanelTab.SwfPolicy, buttonWidth);
            GUILayout.EndHorizontal();

            GUILayout.Space(TabRowSpacing);

            // Third row: Policy Web plus Cabinet (Political Systems Overhaul Part A, the 14th tab) -
            // half-width each rather than Policy Web alone stretched full-width, which would look like
            // a sizing bug now that the row has two tabs again.
            GUILayout.BeginHorizontal();
            DrawRightColumnTabButton("Policy Web", RightPanelTab.PolicyWeb, availableWidth * 0.5f);
            DrawRightColumnTabButton("Cabinet", RightPanelTab.Cabinet, availableWidth * 0.5f);
            GUILayout.EndHorizontal();

            GUILayout.Space(TabRowSpacing);

            // Fourth row: Compass & Demographics (Political Systems Overhaul Part C, the 15th tab)
            // plus Foreign Policy (Continuous Time Migration Phase 0's short-term gameplay
            // scaffolding, the 16th tab) - half-width each, same "two new tabs share a row" precedent
            // the Policy Web/Cabinet row above already established.
            GUILayout.BeginHorizontal();
            DrawRightColumnTabButton("Compass & Demographics", RightPanelTab.CompassAndDemographics, availableWidth * 0.5f);
            DrawRightColumnTabButton("Foreign Policy", RightPanelTab.ForeignPolicy, availableWidth * 0.5f);
            GUILayout.EndHorizontal();

            GUILayout.Space(TabRowSpacing);

            // Fifth row: just Parliament (Political Systems Overhaul Part B PILOT, Master Sequence
            // step 4, the 17th tab) - full-width, same "one new tab alone in its own row" precedent
            // Policy Web's original third row established.
            GUILayout.BeginHorizontal();
            DrawRightColumnTabButton("Parliament", RightPanelTab.Parliament, availableWidth);
            GUILayout.EndHorizontal();
        }

        /// <summary>Each tab is tinted by its own SystemArea (see UiPalette/GetTabArea) - selected uses the bright TabSelected variant, unselected the dimmer Tab variant, so the currently-open tab reads as visibly "lit up" in its own area's hue rather than just bold+yellow text. Width is now explicit (see DrawRightColumnTabs) and the style word-wraps (see InitializeStylesIfNeeded) so a long label like "Sovereign Wealth Fund" degrades to two lines at a narrow width instead of being hard-clipped.</summary>
        private void DrawRightColumnTabButton(string label, RightPanelTab tab, float width)
        {
            UiPalette.SystemArea area = GetTabArea(tab);
            bool selected = _rightPanelTab == tab;
            GUIStyle style = UiPalette.BuildButtonStyle(_tabButtonStyle, selected ? UiPalette.ButtonKind.TabSelected : UiPalette.ButtonKind.Tab, area);
            if (GUILayout.Button(label, style, GUILayout.Width(width)))
            {
                _rightPanelTab = tab;
            }
        }

        private void DrawTurnLog(float availableHeight)
        {
            GUILayout.BeginVertical(_boxStyle);

            float scrollHeight = availableHeight - _labelStyle.fontSize * 2f;
            _logScrollPosition = GUILayout.BeginScrollView(_logScrollPosition, GUILayout.Height(scrollHeight));
            foreach (string entry in _turnLog)
            {
                GUILayout.Label(entry, _labelStyle);
                GUILayout.Space(4f);
            }
            GUILayout.EndScrollView();

            GUILayout.EndVertical();
        }

        /// <summary>
        /// World Map tab (Phase 5): a stylized, non-geographic map (see MapRenderer) showing all six
        /// countries as clickable markers plus fading event dots. Clicking a marker/dot pins a detail
        /// panel below the map - clicking a country clears any pinned event and vice versa, so
        /// exactly one detail panel shows at a time. Every stat and event description shown here is
        /// read straight from existing SimulationManager/EconomyState/EconomicEvent data - no new
        /// simulation data of any kind.
        /// </summary>
        private void DrawWorldMapTab(float availableHeight)
        {
            GUILayout.BeginVertical(_boxStyle);

            float scrollHeight = availableHeight - _labelStyle.fontSize * 2f;
            _worldMapScrollPosition = GUILayout.BeginScrollView(_worldMapScrollPosition, GUILayout.Height(scrollHeight));

            DrawColoredLabel("World Map", _headerStyle, UiPalette.GetAreaColor(UiPalette.SystemArea.Global));
            GUILayout.Label("Hover a marker for a quick readout, click to pin it below. Colored dots are recent events - green helped, red hurt; size reflects how big a shock it was, and dots fade out over a few turns.", _labelStyle);
            GUILayout.Space(6f);

            Rect mapRect = GUILayoutUtility.GetRect(10f, WorldMapHeight, GUILayout.ExpandWidth(true));
            _mapRenderer.Draw(
                mapRect,
                _world.Countries,
                PlayerCountryId,
                _mapEventMarkers,
                _simulationManager.CurrentTurn,
                EventMarkerFadeTurns,
                _labelStyle,
                out CountryId? clickedCountry,
                out MapEventMarker? clickedEvent);

            if (clickedCountry.HasValue)
            {
                _selectedMapCountry = clickedCountry;
                _selectedMapEvent = null;
            }
            else if (clickedEvent.HasValue)
            {
                _selectedMapEvent = clickedEvent;
                _selectedMapCountry = null;
            }

            GUILayout.Space(10f);

            if (_selectedMapEvent.HasValue)
            {
                DrawSelectedMapEventPanel(_selectedMapEvent.Value);
            }
            else if (_selectedMapCountry.HasValue)
            {
                DrawSelectedMapCountryPanel(_selectedMapCountry.Value);
            }
            else
            {
                GUILayout.Label("Click a country marker or an event dot for details.", _labelStyle);
            }

            GUILayout.EndScrollView();
            GUILayout.EndVertical();
        }

        /// <summary>Read-only headline readout for the five non-player countries; the full dashboard-level detail set for USA (the player's own country) - matches the task's explicit "read-only for the five, full detail for USA" split.</summary>
        private void DrawSelectedMapCountryPanel(CountryId countryId)
        {
            Country country = _world.GetCountry(countryId);
            if (country == null)
            {
                return;
            }

            EconomyState state = country.State;
            bool isPlayer = countryId == PlayerCountryId;

            GUILayout.BeginVertical(_boxStyle);
            GUILayout.Label(isPlayer ? $"{country.Name} (your country)" : $"{country.Name} (read-only)", _headerStyle);
            GUILayout.Label($"GDP: {state.GDP:F1}", _labelStyle);
            GUILayout.Label($"Unemployment: {state.Unemployment:F2}%", _labelStyle);
            GUILayout.Label($"Inflation: {state.Inflation:F2}%", _labelStyle);
            GUILayout.Label($"Approval Rating: {state.ApprovalRating:F1}", _labelStyle);
            GUILayout.Label($"Debt-to-GDP: {state.DebtToGdpRatio:F1}%", _labelStyle);

            if (isPlayer)
            {
                GUILayout.Label($"Poverty Rate: {state.PovertyRate:F1}%", _labelStyle);
                GUILayout.Label($"Budget Balance (cumulative): {state.Budget:F1}", _labelStyle);
                GUILayout.Label($"Currency Strength: {state.CurrencyStrength:F1}", _labelStyle);
            }

            GUILayout.EndVertical();
        }

        /// <summary>Same effect-description format as the dashboard's own "BREAKING" event banner (see DrawTopBanner) - deliberately not a separate wording.</summary>
        private void DrawSelectedMapEventPanel(MapEventMarker marker)
        {
            Country country = _world.GetCountry(marker.CountryId);
            string countryName = country != null ? country.Name : marker.CountryId.ToString();

            GUILayout.BeginVertical(_boxStyle);
            DrawColoredLabel($"{countryName}: {marker.Event.Name}", _headerStyle, UiPalette.GetAreaColor(UiPalette.SystemArea.Global));
            GUILayout.Label(marker.Event.Description, _labelStyle);
            GUILayout.Label(
                $"Effects: GDP {marker.Event.GdpShockPercent:+0.0;-0.0}%, Inflation {marker.Event.InflationShockPoints:+0.0;-0.0} pts, Approval {marker.Event.ApprovalEffect:+0.0;-0.0}",
                _labelStyle);
            GUILayout.Label($"Turn {marker.TurnFired} (this turn: {_simulationManager.CurrentTurn})", _labelStyle);
            GUILayout.EndVertical();
        }

        /// <summary>
        /// Policy Web tab: a node/connecting-line diagram of which policy levers affect which stats
        /// (see PolicyWebRenderer for the full node/edge data and rendering technique - reuses
        /// MapRenderer's own node+line approach). Clicking a node pins a detail panel below, the same
        /// "click pins a panel, exactly one at a time" idiom the World Map tab already established.
        /// </summary>
        private void DrawPolicyWebTab(float availableHeight)
        {
            GUILayout.BeginVertical(_boxStyle);

            float scrollHeight = availableHeight - _labelStyle.fontSize * 2f;
            _policyWebScrollPosition = GUILayout.BeginScrollView(_policyWebScrollPosition, GUILayout.Height(scrollHeight));

            DrawColoredLabel("Policy Web", _headerStyle, UiPalette.GetAreaColor(UiPalette.SystemArea.Global));
            GUILayout.Label("The ~9 category headers around the ring are always shown. Hover a node (or click to pin it, and its details below, even after you move away) to reveal its own name and ONLY its own connections - too many of the ~73 nodes to label them all at once legibly. Colored by area; line color follows this game's usual green/red convention (from that STAT's own perspective), thickness reflects relative effect strength where that's meaningfully comparable, uniform otherwise.", _labelStyle);
            GUILayout.Space(6f);

            // Sized off the tab's own actual panel space, not a fixed pixel canvas (see
            // PolicyWebRenderer's own class doc comment) - width matches the column's real available
            // width via ExpandWidth, height is a large majority of what's left in the scroll viewport
            // after the header text above, floored/ceilinged in Screen.height terms so a very short or
            // very tall window still gets something reasonable. This is what keeps the diagram itself
            // from ever needing its own scrollbar - it always renders at exactly the size it's given.
            float diagramHeight = Mathf.Clamp(scrollHeight - _labelStyle.fontSize * 4f, Screen.height * 0.5f, Screen.height * 0.92f);
            Rect webRect = GUILayoutUtility.GetRect(10f, diagramHeight, GUILayout.ExpandWidth(true));
            _policyWebRenderer.Draw(webRect, _labelStyle, _selectedPolicyWebPolicyNode, _selectedPolicyWebStatNode, out PolicyNodeId? clickedPolicy, out StatNodeId? clickedStat);

            if (clickedPolicy.HasValue)
            {
                _selectedPolicyWebPolicyNode = clickedPolicy;
                _selectedPolicyWebStatNode = null;
            }
            else if (clickedStat.HasValue)
            {
                _selectedPolicyWebStatNode = clickedStat;
                _selectedPolicyWebPolicyNode = null;
            }

            GUILayout.Space(10f);

            if (_selectedPolicyWebPolicyNode.HasValue)
            {
                DrawSelectedPolicyWebPolicyPanel(_selectedPolicyWebPolicyNode.Value);
            }
            else if (_selectedPolicyWebStatNode.HasValue)
            {
                DrawSelectedPolicyWebStatPanel(_selectedPolicyWebStatNode.Value);
            }
            else
            {
                GUILayout.Label("Click a policy or stat node for details.", _labelStyle);
            }

            GUILayout.EndScrollView();
            GUILayout.EndVertical();
        }

        private void DrawSelectedPolicyWebPolicyPanel(PolicyNodeId node)
        {
            GUILayout.BeginVertical(_boxStyle);
            DrawColoredLabel(PolicyWebRenderer.GetPolicyName(node), _headerStyle, UiPalette.GetAreaColor(PolicyWebRenderer.GetPolicyArea(node)));
            GUILayout.Label(PolicyWebRenderer.GetPolicyDescription(node), _labelStyle);
            GUILayout.Space(4f);

            GUILayout.Label("Current effects:", _labelStyle);
            foreach (string line in PolicyWebRenderer.GetCurrentEffectSummary(node, _playerCountry))
            {
                GUILayout.Label($"  {line}", _labelStyle);
            }

            GUILayout.EndVertical();
        }

        private void DrawSelectedPolicyWebStatPanel(StatNodeId node)
        {
            GUILayout.BeginVertical(_boxStyle);
            DrawColoredLabel(PolicyWebRenderer.GetStatName(node), _headerStyle, UiPalette.GetAreaColor(UiPalette.SystemArea.Neutral));

            List<PolicyWebEdge> incoming = PolicyWebRenderer.GetEdgesForTarget(node);
            if (incoming.Count > 0)
            {
                GUILayout.Label("Affected by:", _labelStyle);
                foreach (PolicyWebEdge edge in incoming)
                {
                    GUILayout.Label($"  {PolicyWebRenderer.GetPolicyName(edge.Source)}", _labelStyle);
                }
            }

            IReadOnlyList<float> history = PolicyWebRenderer.GetHistory(node, _playerCountry.History);
            if (history != null)
            {
                GraphRenderer graph = GetOrCreatePolicyWebStatGraph(node);
                GUILayout.Space(6f);
                graph.DrawNeutral($"{PolicyWebRenderer.GetStatName(node)} (last 50 turns)", history, null, _labelStyle);
            }
            else
            {
                GUILayout.Label("No trend history tracked for this stat yet.", _labelStyle);
            }

            GUILayout.EndVertical();
        }

        private readonly Dictionary<StatNodeId, GraphRenderer> _policyWebStatGraphs = new Dictionary<StatNodeId, GraphRenderer>();

        private GraphRenderer GetOrCreatePolicyWebStatGraph(StatNodeId node)
        {
            if (!_policyWebStatGraphs.TryGetValue(node, out GraphRenderer graph))
            {
                graph = new GraphRenderer();
                _policyWebStatGraphs[node] = graph;
            }
            return graph;
        }

        /// <summary>Display name per CabinetPortfolio - kept separate from the enum's own C# identifier since "FinanceTreasury"/"InteriorJustice"/"HealthSocialAffairs" read awkwardly as UI text, the same "enum identifier vs. display string" separation PolicyWebRenderer.GetPolicyName/GetStatName already established.</summary>
        private static string GetPortfolioName(CabinetPortfolio portfolio)
        {
            switch (portfolio)
            {
                case CabinetPortfolio.FinanceTreasury: return "Finance & Treasury";
                case CabinetPortfolio.InteriorJustice: return "Interior & Justice";
                case CabinetPortfolio.HealthSocialAffairs: return "Health & Social Affairs";
                default: return portfolio.ToString();
            }
        }

        /// <summary>
        /// Cabinet tab (Political Systems Overhaul Part A, Master Sequence step 1): one panel per
        /// implemented portfolio (see CabinetPortfolio's own doc comment for why only three of the
        /// confirmed six exist yet) showing the appointed minister (or a candidate picker if vacant),
        /// plus any pending interactive decisions at the top, presented with the same visual weight as
        /// the dashboard's own "BREAKING" event banner (see DrawTopBanner) per the Master Roadmap's own
        /// spec - reusing _eventBannerStyle rather than inventing a separate modal style.
        /// </summary>
        private void DrawCabinetTab(float availableHeight)
        {
            GUILayout.BeginVertical(_boxStyle);

            float scrollHeight = availableHeight - _labelStyle.fontSize * 2f;
            _cabinetScrollPosition = GUILayout.BeginScrollView(_cabinetScrollPosition, GUILayout.Height(scrollHeight));

            DrawColoredLabel("Cabinet", _headerStyle, UiPalette.GetAreaColor(UiPalette.SystemArea.Political));
            GUILayout.Label("Each appointed minister quietly nudges their own portfolio's existing channels every turn just by serving, and occasionally brings you a real decision with a few response options. Philosophy determines what KIND of decisions a minister brings, not how skilled they are - that's CompetenceBias, a separate trait. Reshuffling a minister costs a modest approval hit but can happen anytime.", _labelStyle);
            GUILayout.Space(6f);

            foreach ((CabinetPortfolio portfolio, CabinetDecision decision) in _simulationManager.GetPendingCabinetDecisions(PlayerCountryId))
            {
                DrawCabinetDecisionModal(portfolio, decision);
                GUILayout.Space(8f);
            }

            foreach (CabinetPortfolio portfolio in System.Enum.GetValues(typeof(CabinetPortfolio)))
            {
                DrawCabinetPortfolioPanel(portfolio);
                GUILayout.Space(8f);
            }

            GUILayout.EndScrollView();
            GUILayout.EndVertical();
        }

        private void DrawCabinetDecisionModal(CabinetPortfolio portfolio, CabinetDecision decision)
        {
            GUILayout.BeginVertical(_boxStyle);
            GUILayout.Label($"DECISION - {GetPortfolioName(portfolio)}: {decision.Name}", _eventBannerStyle);
            GUILayout.Label(decision.Description, _labelStyle);
            foreach (CabinetDecisionOption option in decision.Options)
            {
                if (GUILayout.Button(option.Label, _neutralActionButtonStyle))
                {
                    _simulationManager.ResolveCabinetDecision(PlayerCountryId, portfolio, decision, option);
                }
            }
            GUILayout.EndVertical();
        }

        /// <summary>
        /// Foreign Policy tab (Continuous Time Migration Phase 0 short-term gameplay scaffolding,
        /// Master Sequence step 3): a single small proof-of-pattern interrupt slice reusing Cabinet's
        /// own decision-modal pattern (DrawCabinetTab/DrawCabinetDecisionModal) - at most one pending
        /// meeting at a time (see SimulationManager's own doc comment on
        /// _pendingForeignPolicyMeetingByCountry), rolled per day rather than per turn since meetings
        /// are meant to land between turn boundaries. Explicitly NOT a law-passing mechanic (that's
        /// Political Systems Overhaul Part B's job) and explicitly NOT "ongoing-process budgets" (left
        /// out of this pass's scope entirely, per the Master Roadmap's own Phase 0 spec being treated
        /// as three candidate systems to choose from, not three mandatory builds).
        /// </summary>
        private void DrawForeignPolicyTab(float availableHeight)
        {
            GUILayout.BeginVertical(_boxStyle);

            float scrollHeight = availableHeight - _labelStyle.fontSize * 2f;
            _foreignPolicyScrollPosition = GUILayout.BeginScrollView(_foreignPolicyScrollPosition, GUILayout.Height(scrollHeight));

            DrawColoredLabel("Foreign Policy", _headerStyle, UiPalette.GetAreaColor(UiPalette.SystemArea.Trade));
            GUILayout.Label("Occasionally a foreign counterpart requests a meeting - a trade delegation, a disaster relief appeal, a joint exercise proposal. Each has a few response options with a small, immediate, one-time effect. Meetings can arrive on any day, not just turn boundaries, and pause time until you respond.", _labelStyle);
            GUILayout.Space(6f);

            ForeignPolicyMeeting pendingMeeting = _simulationManager.GetPendingForeignPolicyMeeting(PlayerCountryId);
            if (pendingMeeting != null)
            {
                DrawForeignPolicyMeetingModal(pendingMeeting);
            }
            else
            {
                GUILayout.Label("No meeting currently pending.", _labelStyle);
            }

            GUILayout.EndScrollView();
            GUILayout.EndVertical();
        }

        private void DrawForeignPolicyMeetingModal(ForeignPolicyMeeting meeting)
        {
            GUILayout.BeginVertical(_boxStyle);
            GUILayout.Label($"MEETING: {meeting.Name}", _eventBannerStyle);
            GUILayout.Label(meeting.Description, _labelStyle);
            foreach (ForeignPolicyMeetingOption option in meeting.Options)
            {
                if (GUILayout.Button(option.Label, _neutralActionButtonStyle))
                {
                    _simulationManager.ResolveForeignPolicyMeeting(PlayerCountryId, option);
                }
            }
            GUILayout.EndVertical();
        }

        /// <summary>
        /// Parliament tab (Political Systems Overhaul Part B PILOT, Master Sequence step 4): the
        /// hemicycle (HemicycleRenderer) plus a pending-bill summary. Only Tax Policy is gated this
        /// pilot, so this tab only ever shows a Tax bill - the roadmap's own "every gated tab needs a
        /// visible Standing/Draft value" instruction applies fully once step 5 rolls Parliament out to
        /// the remaining seven tabs, not yet.
        /// </summary>
        private void DrawParliamentTab(float availableHeight)
        {
            GUILayout.BeginVertical(_boxStyle);

            float scrollHeight = availableHeight - _labelStyle.fontSize * 2f;
            _parliamentScrollPosition = GUILayout.BeginScrollView(_parliamentScrollPosition, GUILayout.Height(scrollHeight));

            DrawColoredLabel("Parliament", _headerStyle, UiPalette.GetAreaColor(UiPalette.SystemArea.Political));
            GUILayout.Label("Seats shift gradually with your ApprovalRating. Only Tax Policy is gated by Parliament this pass - see the Tax Policy tab to introduce a bill.", _labelStyle);
            GUILayout.Space(6f);

            _hemicycleRenderer.Draw($"{_playerCountry.Name} - {ParliamentConstants.TotalSeats} seats", _playerCountry.ParliamentSeats, _labelStyle);

            GUILayout.Space(10f);
            GUILayout.Label("Pending Legislation", _headerStyle);
            TaxBill pendingBill = _simulationManager.GetPendingTaxBill(PlayerCountryId);
            if (pendingBill != null)
            {
                bool wouldPass = ParliamentSystem.WouldBillPass(_playerCountry, pendingBill);
                GUILayout.Label($"Tax bill - resolves in {pendingBill.DaysRemaining} day(s). Current seat composition leans {(wouldPass ? "PASS" : "FAIL")}.", _labelStyle);
            }
            else
            {
                GUILayout.Label("No bill currently before Parliament.", _labelStyle);
            }

            GUILayout.EndScrollView();
            GUILayout.EndVertical();
        }

        private void DrawCabinetPortfolioPanel(CabinetPortfolio portfolio)
        {
            GUILayout.BeginVertical(_boxStyle);
            DrawColoredLabel(GetPortfolioName(portfolio), _headerStyle, UiPalette.GetAreaColor(UiPalette.SystemArea.Political));

            if (_playerCountry.CabinetMinisters.TryGetValue(portfolio, out CabinetMinister minister))
            {
                GUILayout.Label($"{minister.Name} ({minister.Philosophy})", _labelStyle);
                GUILayout.Label(minister.Description, _labelStyle);
                if (GUILayout.Button("Reshuffle", _neutralActionButtonStyle))
                {
                    _playerCountry.CabinetMinisters.Remove(portfolio);
                    _playerCountry.State.ApprovalRating = Mathf.Clamp(_playerCountry.State.ApprovalRating - CabinetSystem.ReshuffleApprovalCost, 0f, 100f);
                    _cabinetCandidatesByPortfolio[portfolio] = CabinetSystem.GenerateCandidates(portfolio);
                }
            }
            else
            {
                GUILayout.Label("Vacant.", _labelStyle);
                if (_cabinetCandidatesByPortfolio.TryGetValue(portfolio, out List<CabinetMinister> candidates))
                {
                    foreach (CabinetMinister candidate in candidates)
                    {
                        DrawCabinetCandidateButton(portfolio, candidate);
                    }
                }
                else if (GUILayout.Button("Search for candidates", _neutralActionButtonStyle))
                {
                    _cabinetCandidatesByPortfolio[portfolio] = CabinetSystem.GenerateCandidates(portfolio);
                }
            }

            GUILayout.EndVertical();
        }

        private void DrawCabinetCandidateButton(CabinetPortfolio portfolio, CabinetMinister candidate)
        {
            GUILayout.BeginVertical(_boxStyle);
            GUILayout.Label($"{candidate.Name} ({candidate.Philosophy})", _labelStyle);
            GUILayout.Label(candidate.Description, _labelStyle);
            if (GUILayout.Button($"Appoint {candidate.Name}", _neutralActionButtonStyle))
            {
                _playerCountry.CabinetMinisters[portfolio] = candidate;
                _cabinetCandidatesByPortfolio.Remove(portfolio);
                RecomputePolicyPreview();
            }
            GUILayout.EndVertical();
        }

        /// <summary>
        /// Compass &amp; Demographics tab (Political Systems Overhaul Part C, Master Sequence step 2):
        /// pure visualization, no player-facing controls. The political compass plots all six
        /// countries at once (see PoliticalCompassRenderer); the five pie charts below it are all
        /// scoped to the player's own country except Population, which is inherently comparative.
        /// Ethnicity/religion breakdowns are explicitly OUT OF SCOPE per the Master Roadmap's own
        /// Part C spec - not tracked anywhere in this game's data model.
        /// </summary>
        private void DrawCompassAndDemographicsTab(float availableHeight)
        {
            GUILayout.BeginVertical(_boxStyle);

            float scrollHeight = availableHeight - _labelStyle.fontSize * 2f;
            _compassAndDemographicsScrollPosition = GUILayout.BeginScrollView(_compassAndDemographicsScrollPosition, GUILayout.Height(scrollHeight));

            DrawColoredLabel("Compass & Demographics", _headerStyle, UiPalette.GetAreaColor(UiPalette.SystemArea.Global));
            GUILayout.Label("Grounded entirely in this game's own tracked policy data - no invented ideology labels, and no ethnicity/religion breakdown (not tracked anywhere in this game's data model).", _labelStyle);
            GUILayout.Space(6f);

            DrawColoredLabel("Political Compass", _headerStyle, UiPalette.GetAreaColor(UiPalette.SystemArea.Global));
            GUILayout.Label("X: average implemented tax rate blended with total government spending (% of GDP) - further right means a bigger fiscal footprint. Y: average sector regulation blended with average implemented welfare generosity - higher means more market regulation and a more generous welfare state. Your own country is ringed in white.", _labelStyle);
            float compassSize = Mathf.Clamp(Screen.height * 0.4f, 260f, 520f);
            Rect compassRect = GUILayoutUtility.GetRect(compassSize, compassSize, GUILayout.ExpandWidth(false));
            _politicalCompassRenderer.Draw(compassRect, _world.Countries, PlayerCountryId, _labelStyle);

            GUILayout.Space(12f);

            DrawColoredLabel("Demographics", _headerStyle, UiPalette.GetAreaColor(UiPalette.SystemArea.Global));
            GUILayout.Space(4f);

            EconomyState state = _playerCountry.State;
            _dependencyRatioPieChart.Draw(
                $"{_playerCountry.Name}: Working-Age vs. Dependent Population",
                new[]
                {
                    new PieSlice("Working-age", 100f - state.DependencyRatio, UiPalette.GetAreaColor(UiPalette.SystemArea.Labor)),
                    new PieSlice("Dependents", state.DependencyRatio, UiPalette.GetAreaColor(UiPalette.SystemArea.Neutral)),
                },
                _labelStyle, "F1");
            GUILayout.Space(10f);

            var sectorSlices = new List<PieSlice>();
            int sectorIndex = 0;
            foreach (Sector sector in _playerCountry.Sectors)
            {
                sectorSlices.Add(new PieSlice(sector.Type.ToString(), sector.EmploymentShare, UiPalette.GetCategoricalColor(sectorIndex)));
                sectorIndex++;
            }
            _sectorEmploymentPieChart.Draw($"{_playerCountry.Name}: Employment Share by Sector", sectorSlices, _labelStyle, "F1");
            GUILayout.Space(10f);

            if (_playerCountry.SpendingLines.Count > 0)
            {
                var spendingSlices = new List<PieSlice>();
                int spendingIndex = 0;
                foreach (SpendingLine line in _playerCountry.SpendingLines)
                {
                    spendingSlices.Add(new PieSlice(line.Category.ToString(), line.Amount, UiPalette.GetCategoricalColor(spendingIndex)));
                    spendingIndex++;
                }
                _spendingAllocationPieChart.Draw($"{_playerCountry.Name}: Spending Allocation", spendingSlices, _labelStyle, "F1");
            }
            else
            {
                GUILayout.Label($"{_playerCountry.Name}: Spending Allocation", _labelStyle);
                GUILayout.Label("Detailed per-category spending breakdown not tracked for this country yet.", _labelStyle);
            }
            GUILayout.Space(10f);

            var taxSlices = new List<PieSlice>();
            int taxIndex = 0;
            foreach (TaxLine taxLine in _playerCountry.TaxLines)
            {
                if (!taxLine.IsImplemented) continue;
                float revenue = state.GDP * (taxLine.Rate / 100f) * taxLine.BaseShareOfGdp;
                taxSlices.Add(new PieSlice(taxLine.Type.ToString(), revenue, UiPalette.GetCategoricalColor(taxIndex)));
                taxIndex++;
            }
            _taxRevenuePieChart.Draw($"{_playerCountry.Name}: Theoretical Tax Revenue by Source", taxSlices, _labelStyle, "F0");
            GUILayout.Space(10f);

            var populationSlices = new List<PieSlice>();
            foreach (Country country in _world.Countries)
            {
                populationSlices.Add(new PieSlice(country.Name, country.State.Population, UiPalette.GetCountryColor(country.Id)));
            }
            _populationPieChart.Draw("Population Share by Country (millions)", populationSlices, _labelStyle, "F1");

            GUILayout.EndScrollView();
            GUILayout.EndVertical();
        }

        /// <summary>
        /// Trade tab (Phase 4 - split off the old combined "Trade &amp; Spending" tab; the spending
        /// report half moved into the Spending Policy tab instead, where it belongs alongside the
        /// spending sliders it reports on). Adds a TradeBalance history graph and, per partner,
        /// proportional bars for Export/Import volume - "how do these partners compare right now"
        /// breakdown data, exactly the case the task calls out for bars over a line graph.
        /// </summary>
        private void DrawTrade(float availableHeight)
        {
            GUILayout.BeginVertical(_boxStyle);

            float scrollHeight = availableHeight - _labelStyle.fontSize * 2f;
            _tradeScrollPosition = GUILayout.BeginScrollView(_tradeScrollPosition, GUILayout.Height(scrollHeight));

            EconomyState state = _playerCountry.State;

            DrawColoredLabel("Trade", _headerStyle, UiPalette.GetAreaColor(UiPalette.SystemArea.Trade));
            DrawColoredLabel($"Overall Trade Balance: {state.TradeBalance:F1}", _labelStyle, UiPalette.GetDeltaColor(state.TradeBalance, higherIsBetter: true));
            _tradeBalanceGraph.Draw("Trade Balance", _playerCountry.History.TradeBalance.Quarterly, null, _labelStyle, higherIsBetter: true);
            GUILayout.Space(6f);

            GUILayout.Label($"General Base Tariff Rate: {_playerCountry.BaseTariffRate:F2}% (applies to any partner with no override, and only where it isn't superseded by trade-bloc membership)", _labelStyle);
            GUILayout.Label($"Tariff Rate Change: {_tariffRateChangeInput:+0.0;-0.0;0} pts", _labelStyle);
            _tariffRateChangeInput = GUILayout.HorizontalSlider(_tariffRateChangeInput, -TariffRateChangeRange, TariffRateChangeRange, _sliderStyle, _sliderThumbStyle);
            GUILayout.Space(10f);

            GUILayout.Label("Set a specific tariff override on our imports from one partner - it beats the usual trade-bloc/base-rate resolution for that partner only. Doesn't affect what that partner charges on our exports to them.", _labelStyle);
            GUILayout.Space(6f);

            // Bars are sized relative to the largest volume across every partner (both directions
            // share one scale) so the bars themselves stay comparable to each other, not just within
            // one partner's own row.
            float maxVolume = 1f;
            foreach (TradePartner link in _playerCountry.TradePartners)
            {
                maxVolume = Mathf.Max(maxVolume, link.ExportVolume, link.ImportVolume);
            }

            foreach (TradePartner link in _playerCountry.TradePartners)
            {
                Country partner = _world.GetCountry(link.PartnerId);
                if (partner == null)
                {
                    continue;
                }

                DrawTradePartnerRow(link, partner, maxVolume);
                GUILayout.Space(10f);
            }

            GUILayout.EndScrollView();
            GUILayout.EndVertical();
        }

        private void DrawTradePartnerRow(TradePartner link, Country partner, float maxVolume)
        {
            // Tariffs are asymmetric: the partner charges its own rate on what we export to
            // them, and we charge our own rate on what we import from them - the same two
            // GetTariffRate calls TradeSystem.ApplyTradeEffects itself makes for this link.
            float tariffOnOurExports = TradeSystem.GetTariffRate(partner, _playerCountry, _world.TradeBlocs);
            float tariffOnOurImports = TradeSystem.GetTariffRate(_playerCountry, partner, _world.TradeBlocs);

            GUILayout.Label(
                $"{partner.Name}: Exports={link.ExportVolume:F1}, Imports={link.ImportVolume:F1}, " +
                $"Tariff on our exports={tariffOnOurExports:F2}%, Tariff on our imports={tariffOnOurImports:F2}%" +
                (link.HasPlayerTariffOverride ? " (override active)" : ""),
                _labelStyle);

            GUILayout.Label("Exports:", _labelStyle);
            UiPalette.DrawBar(link.ExportVolume / maxVolume, UiPalette.PositiveChangeColor, 10f);
            GUILayout.Label("Imports:", _labelStyle);
            UiPalette.DrawBar(link.ImportVolume / maxVolume, UiPalette.GetAreaColor(UiPalette.SystemArea.Trade), 10f);

            float buttonWidth = _labelStyle.fontSize * 8f;
            GUILayout.BeginHorizontal();
            if (link.HasPlayerTariffOverride)
            {
                if (GUILayout.Button("Reset to Default", _removeButtonStyle, GUILayout.Width(buttonWidth)))
                {
                    // Reset is immediate (a structural on/off, like TaxLine.IsImplemented), not a
                    // this-turn delta - the preview cache is invalidated right away so it reflects
                    // the reset the moment it happens, rather than waiting for the usual
                    // slider-changed check to catch up.
                    link.PlayerTariffOverride = -1f;
                    RecomputePolicyPreview();
                }

                float draftRate = GetPartnerTariffInput(link.PartnerId, link.PlayerTariffOverride);
                GUILayout.Label($"Override rate: {draftRate:F2}% (range {PartnerTariffOverrideMin:F0}-{PartnerTariffOverrideMax:F0}%)", _labelStyle);
                float newRate = GUILayout.HorizontalSlider(draftRate, PartnerTariffOverrideMin, PartnerTariffOverrideMax, _sliderStyle, _sliderThumbStyle);
                _partnerTariffInputs[link.PartnerId] = newRate;
            }
            else
            {
                if (GUILayout.Button("Set Override", _implementButtonStyle, GUILayout.Width(buttonWidth)))
                {
                    // Enabling is immediate too - starts the override at today's effective rate
                    // (rather than 0) so turning it on never itself changes the tariff; the slider
                    // then lets the player move it from there.
                    link.PlayerTariffOverride = Mathf.Clamp(tariffOnOurImports, PartnerTariffOverrideMin, PartnerTariffOverrideMax);
                    RecomputePolicyPreview();
                }
            }
            GUILayout.EndHorizontal();
        }

        private void DrawSpendingSection()
        {
            GUILayout.Label("Spending (Last Turn)", _headerStyle);

            FiscalTurnReport report = _simulationManager.GetLastFiscalReport(PlayerCountryId);
            if (report == null)
            {
                GUILayout.Label("No turn advanced yet.", _labelStyle);
                return;
            }

            float net = report.Revenue + report.TariffRevenue
                - report.BaselineGovernmentSpending - report.DiscretionarySpending - report.MandatorySpending
                - report.UnemploymentBenefitCost - report.InterestOnDebt - report.WelfareCost;

            GUILayout.Label($"Revenue (Tax): {report.Revenue:F1}", _labelStyle);
            GUILayout.Label($"Baseline Government Spending: {report.BaselineGovernmentSpending:F1}", _labelStyle);
            GUILayout.Label($"Discretionary Spending Change (this turn): {report.DiscretionarySpending:F1}", _labelStyle);
            GUILayout.Label($"Mandatory Spending: {report.MandatorySpending:F1}", _labelStyle);
            GUILayout.Label($"Unemployment Benefit Cost: {report.UnemploymentBenefitCost:F1}", _labelStyle);
            GUILayout.Label($"Interest On Debt: {report.InterestOnDebt:F1}", _labelStyle);
            GUILayout.Label($"Welfare Program Cost: {report.WelfareCost:F1}", _labelStyle);
            GUILayout.Label($"Tariff Revenue Collected: {report.TariffRevenue:F1}", _labelStyle);
            GUILayout.Space(6f);
            DrawColoredLabel($"Net (matches this turn's Budget change): {net:+0.0;-0.0;0}", _headerStyle, UiPalette.GetDeltaColor(net, higherIsBetter: true));
        }

        /// <summary>
        /// Political Systems Overhaul Part B PILOT (Master Sequence step 4): the Tax Policy tab is now
        /// the gated-legislation pilot. Sliders/toggles below remain DRAFT values (adjusting costs
        /// nothing, no vote needed) - the "Introduce Bill" button is the only way a draft ever reaches
        /// Parliament, and a PASSED bill is the only way it ever reaches the real, standing TaxLines.
        ///
        /// STABLE CONTROL LAYOUT PATTERN (mandatory for every gated tab, not just this one - see
        /// "Background/timed state mutation vs. active UI interaction" in POLISIM_MASTER_ROADMAP.md's
        /// working-discipline failure patterns): once a background system can resolve on ANY simulated
        /// day - a bill passing/failing, and every one of the seven remaining tabs will gain this the
        /// moment Master Sequence step 5 lands - it can mutate the exact standing value a slider on
        /// this tab is reading, on a day the player has an active multi-frame drag in progress on that
        /// slider. GUILayout allocates control IDs positionally (call order within OnGUI), not by a
        /// stable key, so DrawTaxBillStatus and DrawTaxLineRow below must NEVER change which controls
        /// they emit, in what order, based on live/mutable state (a bill pending or not, a TaxType
        /// drafted-implemented or not). Swapping a Button for a Label, or omitting a Slider some
        /// frames, changes the control count/sequence a currently-hot (mid-drag) control was allocated
        /// against, which is a documented Unity IMGUI hang/desync trigger inside a ScrollView - this
        /// is genuinely new risk, not previously reachable, because nothing before Parliament + real-
        /// time day advancement could mutate this tab's own state out from under a live drag. The fix:
        /// every control this tab can ever draw is drawn EVERY frame, in the SAME order; "not
        /// currently applicable" is represented via GUI.enabled = false (greyed, non-interactive, but
        /// still present and control-ID-stable), never by branching the control itself in/out of
        /// existence. Every step-5 tab must follow this same shape from its first draft, not
        /// retrofit it after finding the bug fresh a second time.
        /// </summary>
        private void DrawTaxPolicy(float availableHeight)
        {
            GUILayout.BeginVertical(_boxStyle);

            float scrollHeight = availableHeight - _labelStyle.fontSize * 2f;
            _taxPolicyScrollPosition = GUILayout.BeginScrollView(_taxPolicyScrollPosition, GUILayout.Height(scrollHeight));

            DrawColoredLabel("Tax Policy", _headerStyle, UiPalette.GetAreaColor(UiPalette.SystemArea.Fiscal));
            GUILayout.Label("Adjusting implement/remove or a rate below only changes your DRAFT - nothing happens until you introduce it as a bill and Parliament votes. See the Parliament tab for seat composition.", _labelStyle);
            GUILayout.Space(8f);

            DrawTaxBillStatus();
            GUILayout.Space(8f);

            float taxTypeNameColumnWidth = GetTaxTypeNameColumnWidth();
            foreach (TaxLine taxLine in _playerCountry.TaxLines)
            {
                DrawTaxLineRow(taxLine, taxTypeNameColumnWidth);
                GUILayout.Space(10f);
            }

            GUILayout.EndScrollView();
            GUILayout.EndVertical();
        }

        /// <summary>
        /// Pending-bill status plus the "Introduce Bill" action - disabled while a bill is already
        /// before Parliament, since only one may be pending at a time (see
        /// SimulationManager.IntroduceTaxBill). Follows DrawTaxPolicy's stable-control-layout pattern:
        /// the status Label and the "Introduce Bill" Button are BOTH emitted every frame regardless of
        /// pendingBill - only the label's text and the button's GUI.enabled state vary. Previously this
        /// returned early with just a Label while a bill was pending, which meant a bill resolving
        /// (clearing pendingBill) changed the control COUNT this method emits from one frame to the
        /// next; since this is drawn before every tax line row, that shift also renumbers every
        /// row's own positional control IDs the instant it happens - dangerous for any slider on this
        /// tab that's mid-drag that same frame, not just the one tied to the resolving bill.
        /// </summary>
        private void DrawTaxBillStatus()
        {
            TaxBill pendingBill = _simulationManager.GetPendingTaxBill(PlayerCountryId);

            string statusText = pendingBill != null
                ? $"A tax bill is before Parliament - resolves in {pendingBill.DaysRemaining} day(s)."
                : "No tax bill currently before Parliament.";
            GUILayout.Label(statusText, _labelStyle);

            // Compose with, never clobber, whatever ambient GUI.enabled the caller already set (e.g.
            // the tab-switch's own !_isGameOver gate) - restoring a hardcoded true here would
            // incorrectly re-enable this button while the game is over.
            bool ambientEnabled = GUI.enabled;
            GUI.enabled = ambientEnabled && pendingBill == null;
            if (GUILayout.Button("Introduce Bill", _neutralActionButtonStyle, GUILayout.Width(_labelStyle.fontSize * 12f)))
            {
                var lines = new Dictionary<TaxType, TaxBillLine>();
                foreach (TaxLine taxLine in _playerCountry.TaxLines)
                {
                    bool draftImplemented = GetTaxImplementDraft(taxLine.Type, taxLine.IsImplemented);
                    float draftRate = GetTaxRateInput(taxLine.Type, taxLine.Rate);
                    lines[taxLine.Type] = new TaxBillLine(draftImplemented, draftRate);
                }
                _simulationManager.IntroduceTaxBill(PlayerCountryId, lines);
            }
            GUI.enabled = ambientEnabled;
        }

        /// <summary>Widest TaxType name as rendered in _labelStyle (the style DrawTaxLineRow's name column actually uses), plus a small right-side pad - recomputed each call (not cached), same reasoning as GetSectorNameColumnWidth. The original fixed "_labelStyle.fontSize * 8f" heuristic here undersized the column for the longest name ("CapitalGainsTax"), the same label-truncation root cause found in the Sector/World-Map/Policy-Web labels.</summary>
        private float GetTaxTypeNameColumnWidth()
        {
            float widest = 0f;
            foreach (TaxType type in System.Enum.GetValues(typeof(TaxType)))
            {
                widest = Mathf.Max(widest, _labelStyle.CalcSize(new GUIContent(type.ToString())).x);
            }
            return widest + 12f;
        }

        /// <summary>
        /// Political Systems Overhaul Part B PILOT: the Implement/Remove toggle now edits DRAFT state
        /// only (_taxImplementDrafts) - it no longer mutates taxLine.IsImplemented directly. Both the
        /// standing (legislated) value and the draft are shown, so it's always clear whether an
        /// unpassed change is pending.
        ///
        /// Follows DrawTaxPolicy's stable-control-layout pattern: the rate slider is emitted every
        /// frame regardless of draftImplemented - previously it was omitted entirely while
        /// draftImplemented was false, which is exactly the shape that's unsafe once a bill resolving
        /// mid-drag (ApplyBillResult writes taxLine.IsImplemented directly onto this same live TaxLine)
        /// can flip draftImplemented's fallback value out from under an in-progress drag on this exact
        /// row. Greyed out (GUI.enabled = false) and its value not written back to the draft while not
        /// applicable, but always present at the same control position.
        /// </summary>
        private void DrawTaxLineRow(TaxLine taxLine, float labelWidth)
        {
            float buttonWidth = _labelStyle.fontSize * 6f;
            bool draftImplemented = GetTaxImplementDraft(taxLine.Type, taxLine.IsImplemented);

            GUILayout.BeginHorizontal();
            GUILayout.Label(taxLine.Type.ToString(), _labelStyle, GUILayout.Width(labelWidth));

            string toggleLabel = draftImplemented ? "Remove (draft)" : "Implement (draft)";
            GUIStyle toggleStyle = draftImplemented ? _removeButtonStyle : _implementButtonStyle;
            if (GUILayout.Button(toggleLabel, toggleStyle, GUILayout.Width(buttonWidth * 1.6f)))
            {
                _taxImplementDrafts[taxLine.Type] = !draftImplemented;
                RecomputePolicyPreview();
            }
            GUILayout.EndHorizontal();

            GUILayout.Label($"Standing: {(taxLine.IsImplemented ? $"{taxLine.Rate:F2}%" : "not implemented")}", _labelStyle);

            // The slider IS the current draft (defaulting to the standing Rate until dragged), bounded
            // by this TaxType's own TaxTypeRateRanges - not a small per-turn delta, so a meaningful
            // policy shift (e.g. IncomeTax 37% -> 55%) is reachable in one bill.
            float draftRate = GetTaxRateInput(taxLine.Type, taxLine.Rate);
            string draftLabel = draftImplemented
                ? $"Draft: {draftRate:F2}%  (range {taxLine.MinRate:F0}-{taxLine.MaxRate:F0}%)"
                : "Draft: not implemented";
            GUILayout.Label(draftLabel, _labelStyle);

            // Compose with, never clobber, whatever ambient GUI.enabled the caller already set (e.g.
            // the tab-switch's own !_isGameOver gate) - restoring a hardcoded true here would
            // incorrectly re-enable this slider while the game is over.
            bool ambientEnabled = GUI.enabled;
            GUI.enabled = ambientEnabled && draftImplemented;
            float newRate = GUILayout.HorizontalSlider(draftRate, taxLine.MinRate, taxLine.MaxRate, _sliderStyle, _sliderThumbStyle);
            GUI.enabled = ambientEnabled;
            if (draftImplemented)
            {
                _taxRateInputs[taxLine.Type] = newRate;
            }
        }

        /// <summary>Every WelfareProgramType for the player's country: an Implement/Remove toggle (immediate - see DrawWelfareProgramRow) plus, only while implemented, a slider that directly sets this turn's target GenerosityLevel. Mirrors DrawTaxPolicy/DrawTaxLineRow exactly.</summary>
        private void DrawWelfarePolicy(float availableHeight)
        {
            GUILayout.BeginVertical(_boxStyle);

            float scrollHeight = availableHeight - _labelStyle.fontSize * 2f;
            _welfarePolicyScrollPosition = GUILayout.BeginScrollView(_welfarePolicyScrollPosition, GUILayout.Height(scrollHeight));

            DrawColoredLabel("Welfare Policy", _headerStyle, UiPalette.GetAreaColor(UiPalette.SystemArea.Welfare));
            GUILayout.Label("Implement or remove a welfare program, and (while implemented) drag its generosity directly to the target you want.", _labelStyle);
            _povertyRateGraph.Draw("Poverty Rate", _playerCountry.History.PovertyRate.Quarterly, null, _labelStyle, higherIsBetter: false);
            GUILayout.Space(8f);

            float welfareTypeNameColumnWidth = GetWelfareProgramNameColumnWidth();
            foreach (WelfareProgram welfareProgram in _playerCountry.WelfarePrograms)
            {
                DrawWelfareProgramRow(welfareProgram, welfareTypeNameColumnWidth);
                GUILayout.Space(10f);
            }

            GUILayout.EndScrollView();
            GUILayout.EndVertical();
        }

        /// <summary>Widest WelfareProgramType name as rendered in _labelStyle, plus a small right-side pad - recomputed each call, same reasoning as GetSectorNameColumnWidth/GetTaxTypeNameColumnWidth. The original fixed "_labelStyle.fontSize * 10f" heuristic here undersized the column for the longest name ("MeansTestedWelfare").</summary>
        private float GetWelfareProgramNameColumnWidth()
        {
            float widest = 0f;
            foreach (WelfareProgramType type in System.Enum.GetValues(typeof(WelfareProgramType)))
            {
                widest = Mathf.Max(widest, _labelStyle.CalcSize(new GUIContent(type.ToString())).x);
            }
            return widest + 12f;
        }

        private void DrawWelfareProgramRow(WelfareProgram welfareProgram, float labelWidth)
        {
            float buttonWidth = _labelStyle.fontSize * 6f;

            GUILayout.BeginHorizontal();
            GUILayout.Label(welfareProgram.Type.ToString(), _labelStyle, GUILayout.Width(labelWidth));

            string toggleLabel = welfareProgram.IsImplemented ? "Remove" : "Implement";
            GUIStyle toggleStyle = welfareProgram.IsImplemented ? _removeButtonStyle : _implementButtonStyle;
            if (GUILayout.Button(toggleLabel, toggleStyle, GUILayout.Width(buttonWidth)))
            {
                // Implement/Remove is immediate (a structural on/off, not a this-turn delta) - the
                // preview cache is invalidated right away rather than waiting for the usual
                // slider-changed check, so it reflects the toggle the moment it happens.
                welfareProgram.IsImplemented = !welfareProgram.IsImplemented;
                RecomputePolicyPreview();
            }
            GUILayout.EndHorizontal();

            if (welfareProgram.IsImplemented)
            {
                // The slider IS the current setting (defaulting to the persisted GenerosityLevel until
                // dragged), bounded 0-100% - not a small per-turn delta, so a meaningful policy shift
                // is reachable in one turn.
                float draftGenerosity = GetWelfareGenerosityInput(welfareProgram.Type, welfareProgram.GenerosityLevel);
                GUILayout.Label($"Generosity: {draftGenerosity:F0}%", _labelStyle);
                float newGenerosity = GUILayout.HorizontalSlider(draftGenerosity, 0f, 100f, _sliderStyle, _sliderThumbStyle);
                _welfareGenerosityInputs[welfareProgram.Type] = newGenerosity;
            }
            else
            {
                GUILayout.Label($"Not implemented (generosity on file: {welfareProgram.GenerosityLevel:F0}%)", _labelStyle);
            }
        }

        /// <summary>
        /// Every Sector for the player's country: current Output/Employment/SectorMetric (read-only,
        /// descriptive - see Sector.cs for why they don't feed back into GDP/Unemployment in this
        /// pass) plus five always-adjustable sliders (Subsidy/Regulation/Tax Credits/Research Grants/
        /// Deregulation-Nationalization, all absolute targets like TaxLine.Rate - no implement/remove,
        /// every country has all four Sectors always). The last three were added in Round 3 item 2.
        /// </summary>
        private void DrawSectorPolicy(float availableHeight)
        {
            GUILayout.BeginVertical(_boxStyle);

            float scrollHeight = availableHeight - _labelStyle.fontSize * 2f;
            _sectorPolicyScrollPosition = GUILayout.BeginScrollView(_sectorPolicyScrollPosition, GUILayout.Height(scrollHeight));

            DrawColoredLabel("Economic Sectors", _headerStyle, UiPalette.GetAreaColor(UiPalette.SystemArea.Sectors));
            GUILayout.Label("Output/Employment/the sector's own metric are descriptive only in this pass - the five dials below nudge them, but they don't feed back into GDP/Unemployment.", _labelStyle);
            GUILayout.Space(8f);

            // Measured (not guessed) against _headerStyle - the style the name column is actually
            // drawn in - since _headerStyle's font is both bigger and bolder than _labelStyle's, a
            // width budget borrowed from _labelStyle's own metrics (the original bug here) undersizes
            // the column for the longest name ("Manufacturing"), which then wraps and collides with
            // the adjacent stats text sharing the same horizontal row.
            float sectorNameColumnWidth = GetSectorNameColumnWidth();
            foreach (Sector sector in _playerCountry.Sectors)
            {
                DrawSectorRow(sector, sectorNameColumnWidth);
                GUILayout.Space(10f);
            }

            GUILayout.EndScrollView();
            GUILayout.EndVertical();
        }

        /// <summary>Widest SectorType name as rendered in _headerStyle (the style DrawSectorRow's name column actually uses), plus a small right-side pad - recomputed each call (not cached) since _headerStyle's font size itself changes every frame in RescaleStylesToScreen as the window resizes.</summary>
        private float GetSectorNameColumnWidth()
        {
            float widest = 0f;
            foreach (SectorType type in System.Enum.GetValues(typeof(SectorType)))
            {
                widest = Mathf.Max(widest, _headerStyle.CalcSize(new GUIContent(type.ToString())).x);
            }
            return widest + 12f;
        }

        /// <summary>The sector-specific metric's label, matching Sector.SectorMetric's per-Type real-world meaning (see Sector.cs).</summary>
        private static string GetSectorMetricLabel(SectorType type)
        {
            switch (type)
            {
                case SectorType.Manufacturing: return "Capacity Utilization";
                case SectorType.Technology: return "Innovation Index";
                case SectorType.Agriculture: return "Export Share";
                case SectorType.Finance: return "Credit Growth Rate";
                case SectorType.Energy: return "Renewable Share";
                case SectorType.Construction: return "Building Activity Index";
                case SectorType.Retail: return "E-Commerce Share";
                case SectorType.Telecommunications: return "Broadband Penetration";
                default: return "Sector Metric";
            }
        }

        private void DrawSectorRow(Sector sector, float nameColumnWidth)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(sector.Type.ToString(), _headerStyle, GUILayout.Width(nameColumnWidth));
            GUILayout.Label(
                $"Output {sector.OutputShareOfGdp:F1}% of GDP | Employment {sector.EmploymentShare:F1}% | {GetSectorMetricLabel(sector.Type)} {sector.SectorMetric:F1}",
                _labelStyle);
            GUILayout.EndHorizontal();

            float draftSubsidy = GetSectorSubsidyInput(sector.Type, sector.SubsidyLevel);
            GUILayout.Label($"Subsidy: {draftSubsidy:F0}", _labelStyle);
            _sectorSubsidyInputs[sector.Type] = GUILayout.HorizontalSlider(draftSubsidy, MinPolicyDialLevel, MaxPolicyDialLevel, _sliderStyle, _sliderThumbStyle);

            float draftRegulation = GetSectorRegulationInput(sector.Type, sector.RegulationLevel);
            GUILayout.Label($"Regulation: {draftRegulation:F0} (0 = light-touch, 100 = heavily regulated)", _labelStyle);
            _sectorRegulationInputs[sector.Type] = GUILayout.HorizontalSlider(draftRegulation, MinPolicyDialLevel, MaxPolicyDialLevel, _sliderStyle, _sliderThumbStyle);

            float draftTaxCredit = GetSectorTaxCreditInput(sector.Type, sector.TaxCreditLevel);
            GUILayout.Label($"Tax Credits: {draftTaxCredit:F0}", _labelStyle);
            _sectorTaxCreditInputs[sector.Type] = GUILayout.HorizontalSlider(draftTaxCredit, MinPolicyDialLevel, MaxPolicyDialLevel, _sliderStyle, _sliderThumbStyle);

            float draftResearchGrants = GetSectorResearchGrantsInput(sector.Type, sector.ResearchGrantsLevel);
            GUILayout.Label($"Research Grants: {draftResearchGrants:F0}", _labelStyle);
            _sectorResearchGrantsInputs[sector.Type] = GUILayout.HorizontalSlider(draftResearchGrants, MinPolicyDialLevel, MaxPolicyDialLevel, _sliderStyle, _sliderThumbStyle);

            float draftDeregulation = GetSectorDeregulationInput(sector.Type, sector.DeregulationNationalizationLevel);
            GUILayout.Label($"Deregulation/Nationalization: {draftDeregulation:F0} (0 = fully nationalized, 100 = fully deregulated/private)", _labelStyle);
            _sectorDeregulationInputs[sector.Type] = GUILayout.HorizontalSlider(draftDeregulation, MinPolicyDialLevel, MaxPolicyDialLevel, _sliderStyle, _sliderThumbStyle);
        }

        /// <summary>
        /// Sovereign Wealth Fund tab: a Create/Dissolve button (immediate, mirrors TaxLine.
        /// IsImplemented's toggle pattern) plus, only while it exists, TotalAssets/this-turn estimated
        /// contribution-or-withdrawal+returns (read-only) and sliders for every adjustable setting,
        /// including the Contribution/Withdrawal Rate slider that now goes negative to draw the fund
        /// down (Round 3 item 1). Net Government Position (GovernmentDebt minus fund TotalAssets) is
        /// shown ALONGSIDE, not instead of, the raw GovernmentDebt figure already on the dashboard -
        /// per the task's explicit requirement that fund assets must never be used to obscure a real
        /// fiscal problem. This is a GameController-only display computation - it is never written
        /// back into EconomyState/Country and never read by any simulation formula
        /// (GetDebtRiskPremium, GetFiscalReactionMultiplier, etc. all keep reading the real, gross
        /// GovernmentDebt/DebtToGdpRatio exactly as before).
        /// </summary>
        private void DrawSwfPolicy(float availableHeight)
        {
            GUILayout.BeginVertical(_boxStyle);

            float scrollHeight = availableHeight - _labelStyle.fontSize * 2f;
            _swfPolicyScrollPosition = GUILayout.BeginScrollView(_swfPolicyScrollPosition, GUILayout.Height(scrollHeight));

            DrawColoredLabel("Sovereign Wealth Fund", _headerStyle, UiPalette.GetAreaColor(UiPalette.SystemArea.SovereignWealth));

            SovereignWealthFund fund = _playerCountry.SovereignWealthFund;
            string toggleLabel = fund == null ? "Create Fund" : "Dissolve Fund";
            GUIStyle toggleStyle = fund == null ? _implementButtonStyle : _removeButtonStyle;
            if (GUILayout.Button(toggleLabel, toggleStyle))
            {
                _playerCountry.SovereignWealthFund = fund == null ? new SovereignWealthFund() : null;
                RecomputePolicyPreview();
            }

            if (fund == null)
            {
                GUILayout.Label("No fund exists. Creating one starts a new budget expense (the contribution) in exchange for market returns on its growing assets - it can also be drawn down during a recession or emergency instead of borrowing, once it exists.", _labelStyle);
                GUILayout.EndScrollView();
                GUILayout.EndVertical();
                return;
            }

            GUILayout.Space(8f);
            GUILayout.Label($"Total Assets: {fund.TotalAssets:F1}", _labelStyle);
            float netGovernmentPosition = _playerCountry.State.GovernmentDebt - fund.TotalAssets;
            GUILayout.Label($"Government Debt (gross): {_playerCountry.State.GovernmentDebt:F1}  |  Net Government Position (debt minus fund assets): {netGovernmentPosition:F1}", _labelStyle);
            GUILayout.BeginHorizontal();
            GUILayout.Label($"Estimated this turn - Contribution/Withdrawal: {_cachedSwfContributionText}, Returns: ", _labelStyle, GUILayout.ExpandWidth(false));
            DrawColoredLabel(_cachedSwfReturnsText, _labelStyle, UiPalette.GetDeltaColor(_cachedSwfReturnsEstimateRaw, higherIsBetter: true));
            GUILayout.EndHorizontal();
            GUILayout.Space(8f);

            float draftContributionRate = GetSwfContributionRateInput(fund.ContributionRatePercent);
            GUILayout.Label($"Contribution/Withdrawal Rate: {draftContributionRate:+0.0;-0.0;0}% of GDP per turn (negative draws the fund down - use during a recession or emergency instead of borrowing)", _labelStyle);
            _swfContributionRateInput = GUILayout.HorizontalSlider(draftContributionRate, MinSwfContributionRate, MaxSwfContributionRate, _sliderStyle, _sliderThumbStyle);

            float draftDomesticAllocation = GetSwfDomesticAllocationInput(fund.DomesticAllocationPercent);
            GUILayout.Label($"Domestic Allocation: {draftDomesticAllocation:F0}% (rest international - this pass doesn't model differing returns by allocation)", _labelStyle);
            _swfDomesticAllocationInput = GUILayout.HorizontalSlider(draftDomesticAllocation, MinPolicyDialLevel, MaxPolicyDialLevel, _sliderStyle, _sliderThumbStyle);

            GUILayout.Space(8f);
            GUILayout.Label("Asset Class Mix (weights, normalized automatically - don't need to sum to 100)", _labelStyle);

            // Each bar's fraction IS the already-normalized weight (0-1) - no further scaling needed,
            // unlike the spending-line/trade-volume bars above which normalize against a group max.
            Color swfColor = UiPalette.GetAreaColor(UiPalette.SystemArea.SovereignWealth);

            float draftEquities = GetSwfEquitiesWeightInput(fund.EquitiesWeight);
            GUILayout.Label($"Equities: {draftEquities:F0} ({fund.GetNormalizedWeight(SovereignWealthAssetClass.Equities) * 100f:F0}% of fund)", _labelStyle);
            UiPalette.DrawBar(fund.GetNormalizedWeight(SovereignWealthAssetClass.Equities), swfColor, 8f);
            _swfEquitiesWeightInput = GUILayout.HorizontalSlider(draftEquities, MinPolicyDialLevel, MaxPolicyDialLevel, _sliderStyle, _sliderThumbStyle);

            float draftBonds = GetSwfBondsWeightInput(fund.BondsWeight);
            GUILayout.Label($"Bonds: {draftBonds:F0} ({fund.GetNormalizedWeight(SovereignWealthAssetClass.Bonds) * 100f:F0}% of fund)", _labelStyle);
            UiPalette.DrawBar(fund.GetNormalizedWeight(SovereignWealthAssetClass.Bonds), swfColor, 8f);
            _swfBondsWeightInput = GUILayout.HorizontalSlider(draftBonds, MinPolicyDialLevel, MaxPolicyDialLevel, _sliderStyle, _sliderThumbStyle);

            float draftInfrastructure = GetSwfInfrastructureWeightInput(fund.InfrastructureWeight);
            GUILayout.Label($"Infrastructure: {draftInfrastructure:F0} ({fund.GetNormalizedWeight(SovereignWealthAssetClass.Infrastructure) * 100f:F0}% of fund)", _labelStyle);
            UiPalette.DrawBar(fund.GetNormalizedWeight(SovereignWealthAssetClass.Infrastructure), swfColor, 8f);
            _swfInfrastructureWeightInput = GUILayout.HorizontalSlider(draftInfrastructure, MinPolicyDialLevel, MaxPolicyDialLevel, _sliderStyle, _sliderThumbStyle);

            float draftRealEstate = GetSwfRealEstateWeightInput(fund.RealEstateWeight);
            GUILayout.Label($"Real Estate: {draftRealEstate:F0} ({fund.GetNormalizedWeight(SovereignWealthAssetClass.RealEstate) * 100f:F0}% of fund)", _labelStyle);
            UiPalette.DrawBar(fund.GetNormalizedWeight(SovereignWealthAssetClass.RealEstate), swfColor, 8f);
            _swfRealEstateWeightInput = GUILayout.HorizontalSlider(draftRealEstate, MinPolicyDialLevel, MaxPolicyDialLevel, _sliderStyle, _sliderThumbStyle);

            GUILayout.EndScrollView();
            GUILayout.EndVertical();
        }

        /// <summary>
        /// The player country's detailed spending portfolio (Phase 1: USA only - see CLAUDE.md's
        /// "Detailed Spending Portfolio"), grouped Mandatory / Discretionary, plus Interest on Debt
        /// as a read-only automatic line. Both groups now get a this-turn PERCENTAGE-change slider
        /// (SimulationManager.ApplySpendingLineChanges applies it to that line's own Amount) -
        /// Mandatory's range is narrower, reflecting the real political difficulty of entitlement
        /// reform, and a Mandatory change carries a distinctly higher approval-rating penalty per
        /// relative size than a Discretionary one (see MacroSystem.MandatorySpendingApprovalMultiplier).
        /// </summary>
        private void DrawSpendingPolicy(float availableHeight)
        {
            GUILayout.BeginVertical(_boxStyle);

            float scrollHeight = availableHeight - _labelStyle.fontSize * 2f;
            _spendingPolicyScrollPosition = GUILayout.BeginScrollView(_spendingPolicyScrollPosition, GUILayout.Height(scrollHeight));

            DrawColoredLabel("Spending Policy", _headerStyle, UiPalette.GetAreaColor(UiPalette.SystemArea.Fiscal));
            GUILayout.Label("Each line's slider is a percentage change of its OWN current amount, not a flat dollar delta. Mandatory programs have a narrower range and hit approval harder per relative size - entitlement reform is politically costly.", _labelStyle);
            GUILayout.Space(8f);

            // Moved here from the old combined "Trade & Spending" tab (Phase 4) - the last-turn
            // fiscal report belongs next to the sliders it explains, not bolted onto Trade.
            DrawSpendingSection();
            _debtToGdpGraph.Draw("Debt-to-GDP", _playerCountry.History.DebtToGdpRatio.Quarterly, null, _labelStyle, higherIsBetter: false,
                thresholdValue: _playerCountry.ComfortableDebtToGdpPercent, thresholdLabel: "Comfortable");
            GUILayout.Space(16f);

            DrawInterestOnDebtRow();
            GUILayout.Space(10f);

            // Bars are scaled within their own group (Mandatory vs. Discretionary), not against each
            // other - the two groups differ by orders of magnitude (e.g. Social Security vs. SBA), so
            // one shared scale would flatten every Discretionary bar to nothing.
            float maxMandatory = 1f;
            float maxDiscretionary = 1f;
            foreach (SpendingLine spendingLine in _playerCountry.SpendingLines)
            {
                if (spendingLine.IsMandatory)
                {
                    maxMandatory = Mathf.Max(maxMandatory, spendingLine.Amount);
                }
                else
                {
                    maxDiscretionary = Mathf.Max(maxDiscretionary, spendingLine.Amount);
                }
            }

            GUILayout.Label("Mandatory (narrower range, higher approval cost)", _headerStyle);
            foreach (SpendingLine spendingLine in _playerCountry.SpendingLines)
            {
                if (!spendingLine.IsMandatory)
                {
                    continue;
                }

                DrawSpendingLineRow(spendingLine, MandatoryPercentChangeRange, maxMandatory);
                GUILayout.Space(10f);
            }

            GUILayout.Space(10f);
            GUILayout.Label("Discretionary", _headerStyle);
            foreach (SpendingLine spendingLine in _playerCountry.SpendingLines)
            {
                if (spendingLine.IsMandatory)
                {
                    continue;
                }

                DrawSpendingLineRow(spendingLine, DiscretionaryPercentChangeRange, maxDiscretionary);
                GUILayout.Space(10f);
            }

            GUILayout.EndScrollView();
            GUILayout.EndVertical();
        }

        /// <summary>Interest on Debt is SimulationManager's existing automatic GetInterestOnDebt calculation, not a seeded line - shown as a read-only, clearly-marked-automatic figure from last turn's FiscalTurnReport.</summary>
        private void DrawInterestOnDebtRow()
        {
            FiscalTurnReport report = _simulationManager.GetLastFiscalReport(PlayerCountryId);
            string valueText = report != null ? $"{report.InterestOnDebt:F1}" : "not yet computed (advance a turn)";
            GUILayout.Label($"Interest on Debt (automatic, last turn): {valueText}", _labelStyle);
        }

        /// <summary>One SpendingLine's row: a slider representing a PERCENTAGE change of its own current Amount, bounded by <paramref name="rangePercent"/> (narrower for Mandatory - see DrawSpendingPolicy), showing both the requested percentage and the dollar amount it implies at the line's current size, plus a bar sized relative to <paramref name="maxAmountInGroup"/> (its own Mandatory/Discretionary group's largest line) for an at-a-glance size comparison.</summary>
        private void DrawSpendingLineRow(SpendingLine spendingLine, float rangePercent, float maxAmountInGroup)
        {
            float draftPercent = GetSpendingLineInput(spendingLine.Category);
            float impliedDollarChange = spendingLine.Amount * draftPercent / 100f;
            GUILayout.Label(
                $"{spendingLine.Category}: {spendingLine.Amount:F1}  Change: {draftPercent:+0.0;-0.0;0}% ({impliedDollarChange:+0.0;-0.0;0})",
                _labelStyle);
            UiPalette.DrawBar(spendingLine.Amount / maxAmountInGroup, UiPalette.GetAreaColor(UiPalette.SystemArea.Fiscal), 8f);
            float newPercent = GUILayout.HorizontalSlider(draftPercent, -rangePercent, rangePercent, _sliderStyle, _sliderThumbStyle);
            _spendingLineInputs[spendingLine.Category] = newPercent;
        }
    }
}
