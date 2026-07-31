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
        /// <summary>
        /// Master Sequence step 5e, Phase A (tab/IA restructuring): the 7 consolidated top-level tabs,
        /// replacing the old 18-tab `RightPanelTab` bar - see POLISIM_MASTER_ROADMAP.md's "5e
        /// implementation plan" for the full old-tab -&gt; new-tab mapping and reasoning behind every
        /// placement (several tabs SPLIT across two destinations - Cabinet, Compass &amp; Demographics,
        /// Trade - and five placements were genuinely ambiguous and required Elias's explicit
        /// confirmation before being built). Deliberately NO visual style change in this phase - same
        /// `DrawRightColumnTabButton` mechanics, same per-area color tinting, same underlying Draw*
        /// content methods reused verbatim wherever possible. The sprite/icon reskin is Phase B/C's job.
        /// </summary>
        private enum ConsolidatedTab
        {
            Statistics,
            Decisions,
            Demographics,
            Tax,
            Spending,
            PolicyLaws,
            Politics
        }

        /// <summary>Statistics tab's 3 sub-categories (Recent Turns, World Map, and Trade's informational half - the Trade Balance graph only, see TradeCategory below for the policy half).</summary>
        private enum StatisticsCategory { RecentTurns, WorldMap, Trade }

        /// <summary>Policy/Laws tab's 5 sub-categories - each already has (or, for Trade/Policy Web, now gains) its own standalone-bill or reference-tool identity.</summary>
        private enum PolicyLawsCategory { LaborMarket, CrimeJustice, Sectors, PolicyWeb, Trade }

        /// <summary>Politics tab's 4 sub-categories - the political institutions, whether or not their own lever is Parliament-gated (Federal Reserve isn't, by design - see the Fed/Eurozone exemption).</summary>
        private enum PoliticsCategory { Parliament, Compass, Cabinet, FederalReserve }

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
        /// <summary>Master Sequence step 5d: the Trade tab's tariff draft moved from a small this-turn delta (the old TariffRateChangeRange) to an absolute target rate, matching TaxLine.Rate's own pattern - must match SimulationManager's own private MinBaseTariffRate/MaxBaseTariffRate.</summary>
        private const float MinBaseTariffRate = 0f;
        private const float MaxBaseTariffRate = 50f;
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

        // Political Systems Overhaul Part B, full rollout (Master Sequence step 5c): draft
        // Create/Dissolve state for the Sovereign Wealth Fund - defaults to whether
        // _playerCountry.SovereignWealthFund is currently non-null until the player toggles it (see
        // GetSwfExistsDraft). It only ever reaches Country.SovereignWealthFund via a PASSED
        // BudgetBill - unlike TaxLine/WelfareProgram's own IsImplemented (Master Sequence step 5d
        // moved those to their own standalone anytime bills, see ProgramBill.cs), SWF create/dissolve
        // stays part of the annual omnibus bill; nullable rather than a Dictionary since there's only
        // one fund. Not cleared by ResetPolicyInputs, for the same reason _taxRateInputs isn't.
        private bool? _swfExistsDraft;

        // Draft ABSOLUTE Sovereign Wealth Fund settings (not deltas) - meaningful whenever the DRAFT
        // says the fund should exist (GetSwfExistsDraft), not just while the real fund already does -
        // see DrawSwfPolicyContent's stable-control-layout note for why this changed at step 5c. Not
        // cleared by ResetPolicyInputs, for the same reason _minimumWageInput isn't.
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

        // Master Sequence step 5d: the Trade tab's General Base Tariff Rate draft - an ABSOLUTE target
        // (not the small this-turn delta it used to be), defaulting to Country.BaseTariffRate until
        // dragged, matching TaxLine.Rate's own pattern. Not cleared by ResetPolicyInputs, for the same
        // reason _taxRateInputs isn't - it only ever reaches BaseTariffRate via a PASSED TradePolicyBill
        // (see BuildTradeBillFromDrafts/DrawTradeBillStatusAndIntroduce).
        private float? _tariffRateInput;

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
        private float? _cachedTariffRateInput;
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

        private ConsolidatedTab _consolidatedTab = ConsolidatedTab.Statistics;
        private StatisticsCategory _statisticsCategory = StatisticsCategory.RecentTurns;
        private PolicyLawsCategory _policyLawsCategory = PolicyLawsCategory.LaborMarket;
        private PoliticsCategory _politicsCategory = PoliticsCategory.Parliament;
        private Vector2 _statisticsContentScrollPosition;
        private Vector2 _decisionsScrollPosition;
        private Vector2 _demographicsScrollPosition;
        private Vector2 _policyLawsContentScrollPosition;
        private Vector2 _politicsContentScrollPosition;
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
        private readonly Dictionary<CabinetPortfolio, List<CabinetMinister>> _cabinetCandidatesByPortfolio = new Dictionary<CabinetPortfolio, List<CabinetMinister>>();
        private Vector2 _federalReserveScrollPosition;
        private Vector2 _laborMarketScrollPosition;
        private Vector2 _crimeJusticeScrollPosition;
        private Vector2 _sectorPolicyScrollPosition;

        /// <summary>Master Sequence step 5b: which category's content DrawBudgetProcessTab's center column currently shows - a left-column selector, not a draft/standing value itself, so no PolicyDecision/bill involvement.</summary>
        private enum BudgetProcessCategory { Tax, Spending, Welfare, Infrastructure, Swf }
        private BudgetProcessCategory _budgetProcessCategory = BudgetProcessCategory.Tax;
        private Vector2 _budgetProcessCenterScrollPosition;
        private Vector2 _budgetProcessRowScrollPosition;

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

                // Political Systems Overhaul Part B, full rollout (Master Sequence step 5c): a pending
                // omnibus BudgetBill counts down daily too, independent of the turn boundary - unlike
                // the two calls above, this never needs a gate re-check afterward, since resolving a
                // bill doesn't pause time (it's a deterministic countdown, not something needing a
                // player response) - the same idiom the retired TaxBill/AdvanceLegislativeDay already
                // established.
                _simulationManager.AdvanceBudgetBillDay(PlayerCountryId);

                // Master Sequence step 5d: the six standalone tier-2/tier-3 bill mechanisms count down
                // daily too, the exact same non-blocking idiom as AdvanceBudgetBillDay above - none of
                // these ever pause time (introducible anytime, no mandatory-pause phase the way the
                // annual budget process has), so none needs a gate re-check either.
                _simulationManager.AdvanceTaxProgramBillsDay(PlayerCountryId);
                _simulationManager.AdvanceWelfareProgramBillsDay(PlayerCountryId);
                _simulationManager.AdvanceLaborBillDay(PlayerCountryId);
                _simulationManager.AdvanceCrimeJusticeBillDay(PlayerCountryId);
                _simulationManager.AdvanceSectorBillDay(PlayerCountryId);
                _simulationManager.AdvanceTradeBillDay(PlayerCountryId);

                // Master Sequence step 5a: same daily idiom as the two calls above - deterministic
                // date check, not a chance roll, mirroring AdvanceBudgetBillDay's own reasoning.
                // Unlike AdvanceBudgetBillDay, THIS one DOES need the gate re-check below, since
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
            // status line, then the speed button row). Master Sequence step 5a briefly reserved a
            // second row for a temporary Acknowledge button (see git history) - removed now that step
            // 5c's real Budget Process introduce-bill flow replaced it, back to the original one-row
            // reservation.
            float calendarAreaHeight = _labelStyle.fontSize + 8f + _buttonStyle.fixedHeight + sectionSpacing;
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
            DrawConsolidatedTabs(rightColumnWidth);
            GUILayout.Space(sectionSpacing * 0.5f);

            // Master Sequence step 5e, Phase A: ONE tab row now (7 short-labeled consolidated tabs,
            // see DrawConsolidatedTabs) - replaces the old 5-row reservation entirely.
            float tabRowsHeight = _tabButtonStyle.fixedHeight;
            float tabContentHeight = areaHeight - tabRowsHeight - sectionSpacing * 0.5f;
            // Master Sequence step 5e, Phase A: game-over gating stays exactly where the OLD 18-tab
            // dispatch had it, not a blanket gate here - several old tabs were deliberately NEVER
            // gated (WorldMap/PolicyWeb/Parliament/Compass are read-only visualizations, still fully
            // legible after game over), and several now-merged aggregate tabs mix gated and ungated
            // pieces (e.g. Politics = Parliament[ungated] + Compass[ungated] + Cabinet[gated] +
            // FederalReserve[gated]) - see each DrawXTab method below for where it applies its own
            // gate at the right granularity, matching the old per-case behavior exactly.
            switch (_consolidatedTab)
            {
                case ConsolidatedTab.Statistics:
                    DrawStatisticsTab(tabContentHeight);
                    break;
                case ConsolidatedTab.Decisions:
                    DrawDecisionsTab(tabContentHeight);
                    break;
                case ConsolidatedTab.Demographics:
                    DrawDemographicsTab(tabContentHeight);
                    break;
                case ConsolidatedTab.Tax:
                case ConsolidatedTab.Spending:
                    GUI.enabled = !_isGameOver;
                    DrawBudgetProcessTab(tabContentHeight, rightColumnWidth);
                    GUI.enabled = true;
                    break;
                case ConsolidatedTab.PolicyLaws:
                    DrawPolicyLawsTab(tabContentHeight);
                    break;
                case ConsolidatedTab.Politics:
                    DrawPoliticsTab(tabContentHeight);
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

        /// <summary>
        /// Maps a consolidated top-level tab to the system area whose hue it should be tinted with -
        /// Phase A provisional choices only (no icons yet, color-only, same `GetTabArea`-style mapping
        /// the old 18-tab bar used), superseded by Phase B/C's real per-tab `icon_nav_*` treatment.
        /// Four of the 7 don't have one dominant existing SystemArea (Statistics/Decisions/Demographics/
        /// PolicyLaws each aggregate multiple old areas) - picked for visual DISTINCTNESS across all 7
        /// tab buttons (the actual property "reuse the existing tab-bar mechanics" needs to preserve),
        /// not because any one is a uniquely "correct" fit: Statistics keeps Global (its dominant piece,
        /// World Map, already used it), Politics keeps Political (strong precedent - Parliament/Cabinet/
        /// FedReserve all already used it), Tax/Spending keep Fiscal unchanged (same screen either way,
        /// so sharing a hue is a feature, not a confusion). Decisions and Policy/Laws get two more
        /// distinct existing hues (CrimeJustice, Sectors) that aren't already claimed above.
        /// </summary>
        private static UiPalette.SystemArea GetConsolidatedTabArea(ConsolidatedTab tab)
        {
            switch (tab)
            {
                case ConsolidatedTab.Statistics: return UiPalette.SystemArea.Global;
                case ConsolidatedTab.Decisions: return UiPalette.SystemArea.CrimeJustice;
                case ConsolidatedTab.Demographics: return UiPalette.SystemArea.Labor;
                case ConsolidatedTab.Tax: return UiPalette.SystemArea.Fiscal;
                case ConsolidatedTab.Spending: return UiPalette.SystemArea.Fiscal;
                case ConsolidatedTab.PolicyLaws: return UiPalette.SystemArea.Sectors;
                case ConsolidatedTab.Politics: return UiPalette.SystemArea.Political;
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

            DrawHeadlineStatTiles(state, hasIndependentCurrency);

            GUILayout.Space(10f);
            DrawHeadlineGraphs(state);

            GUILayout.EndVertical();
        }

        /// <summary>
        /// Master Sequence step 5e, Phase B pilot: the dashboard's nine headline stats restyled onto
        /// <see cref="PoliSimWidgets.StatTile"/> in a 3-column grid, replacing the old raw
        /// GUILayout.Label two-column list - this is Phase B's actual sprite-pilot target (see
        /// POLISIM_MASTER_ROADMAP.md), not the Statistics tab's own content, since this is the one
        /// surface visible on every tab. GDP is the only tile with a real turn-over-turn delta
        /// available (_lastGrowthPercent, tracked via _prevGdp) - the other eight get no delta pill
        /// rather than a fabricated one, since no comparable prior-turn value is tracked for them.
        /// DrawHeadlineGraphs (the procedural line graphs) is untouched by this pass - rule 10's own
        /// carve-out keeps every data visualization procedural; only the icon/portrait/background
        /// layer moves to sprite art.
        /// </summary>
        private void DrawHeadlineStatTiles(EconomyState state, bool hasIndependentCurrency)
        {
            const float scale = 1f;
            const int columns = 3;
            const float tileHeight = 92f;
            const float gap = 8f;

            var tiles = new List<(string label, string value, string suffix, string delta, bool deltaIsGood, UiPalette.SystemArea area)>
            {
                ("GDP", state.GDP.ToString("F1"), null, _lastGrowthPercent.ToString("+0.00;-0.00;0") + "%", _lastGrowthPercent >= 0f, UiPalette.SystemArea.Global),
                ("Unemployment", state.Unemployment.ToString("F2"), "%", null, false, UiPalette.SystemArea.Labor),
                ("Inflation", state.Inflation.ToString("F2"), "%", null, false, UiPalette.SystemArea.Fiscal),
                ("Approval Rating", state.ApprovalRating.ToString("F1"), null, null, false, UiPalette.SystemArea.Political),
            };

            if (hasIndependentCurrency)
            {
                tiles.Add(("Currency Strength", state.CurrencyStrength.ToString("F1"), null, null, false, UiPalette.SystemArea.Trade));
            }

            tiles.Add(("Poverty Rate", state.PovertyRate.ToString("F1"), "%", null, false, UiPalette.SystemArea.Welfare));
            tiles.Add(("Government Debt", state.GovernmentDebt.ToString("F1"), null, null, false, UiPalette.SystemArea.Fiscal));
            tiles.Add(("Debt-to-GDP", state.DebtToGdpRatio.ToString("F1"), "%", null, false, UiPalette.SystemArea.Fiscal));
            tiles.Add(("Budget Balance", state.Budget.ToString("F1"), null, null, false, UiPalette.SystemArea.Fiscal));

            int rows = Mathf.CeilToInt(tiles.Count / (float)columns);
            float totalHeight = rows * tileHeight + (rows - 1) * gap;
            Rect gridRect = GUILayoutUtility.GetRect(0f, totalHeight, GUILayout.ExpandWidth(true));
            float columnWidth = (gridRect.width - gap * (columns - 1)) / columns;

            for (int i = 0; i < tiles.Count; i++)
            {
                int row = i / columns;
                int col = i % columns;
                var tileRect = new Rect(gridRect.x + col * (columnWidth + gap), gridRect.y + row * (tileHeight + gap), columnWidth, tileHeight);
                var tile = tiles[i];
                PoliSimWidgets.StatTile(tileRect, tile.label, tile.value, tile.suffix, tile.delta, tile.deltaIsGood, null, tile.area, scale);
            }
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

                DrawFedChairSelectionModal();
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
        /// Master Sequence step 5e, Phase A: extracted from DrawFederalReserveTab so DrawDecisionsTab
        /// can show the exact same pending Fed Chair selection there too, not just under Politics -
        /// applying Elias's own confirmed reasoning for the Budget Process mandatory-pause interrupt
        /// ("any 'time is blocked until you respond' state belongs in the same place, not treated as
        /// an exception") to Fed Chair selection as well, since it's the same kind of blocking
        /// interrupt (see UpdateFedChairSelectionState/GameController.Update's own pause gate) that
        /// wasn't one of the five items Elias was explicitly asked about. A no-op if nothing is
        /// pending, so both call sites can call it unconditionally.
        /// </summary>
        private void DrawFedChairSelectionModal()
        {
            if (_fedChairCandidates == null || _fedChairCandidates.Count == 0)
            {
                return;
            }

            GUILayout.Space(8f);
            GUILayout.Label("A new presidential term begins next turn - choose the next Fed chair:", _labelStyle);
            foreach (FedChair candidate in _fedChairCandidates)
            {
                DrawFedChairCandidateButton(candidate);
            }
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
            GUILayout.Label("Master Sequence step 5d: every dial below is a DRAFT - nothing happens until you introduce them as one standalone bill, which resolves independently of the annual budget cycle.", _labelStyle);
            GUILayout.Space(8f);

            DrawCrimeJusticeBillStatusAndIntroduce();
            DrawCrimeJusticeLiveEstimate();
            GUILayout.Space(8f);

            float draftPoliceFunding = GetPoliceFundingInput(_playerCountry.PoliceFundingLevel);
            GUILayout.Label($"Police Funding - Standing: {_playerCountry.PoliceFundingLevel:F0}, Draft: {draftPoliceFunding:F0}", _labelStyle);
            _policeFundingInput = GUILayout.HorizontalSlider(draftPoliceFunding, MinPolicyDialLevel, MaxPolicyDialLevel, _sliderStyle, _sliderThumbStyle);

            float draftSentencingSeverity = GetSentencingSeverityInput(_playerCountry.SentencingSeverity);
            GUILayout.Label($"Sentencing Severity - Standing: {_playerCountry.SentencingSeverity:F0}, Draft: {draftSentencingSeverity:F0} (0 = lenient, 100 = harsh)", _labelStyle);
            _sentencingSeverityInput = GUILayout.HorizontalSlider(draftSentencingSeverity, MinPolicyDialLevel, MaxPolicyDialLevel, _sliderStyle, _sliderThumbStyle);

            float draftBailReform = GetBailReformInput(_playerCountry.BailReformLevel);
            GUILayout.Label($"Bail Reform - Standing: {_playerCountry.BailReformLevel:F0}, Draft: {draftBailReform:F0} (0 = traditional cash bail, 100 = full reform)", _labelStyle);
            _bailReformInput = GUILayout.HorizontalSlider(draftBailReform, MinPolicyDialLevel, MaxPolicyDialLevel, _sliderStyle, _sliderThumbStyle);

            float draftDrugPolicy = GetDrugPolicyInput(_playerCountry.DrugPolicyLevel);
            GUILayout.Label($"Drug Policy - Standing: {_playerCountry.DrugPolicyLevel:F0}, Draft: {draftDrugPolicy:F0} (0 = decriminalized, 100 = strict criminalization)", _labelStyle);
            _drugPolicyInput = GUILayout.HorizontalSlider(draftDrugPolicy, MinPolicyDialLevel, MaxPolicyDialLevel, _sliderStyle, _sliderThumbStyle);

            float draftJudicialFunding = GetJudicialFundingInput(_playerCountry.JudicialFundingLevel);
            GUILayout.Label($"Judicial Funding - Standing: {_playerCountry.JudicialFundingLevel:F0}, Draft: {draftJudicialFunding:F0}", _labelStyle);
            _judicialFundingInput = GUILayout.HorizontalSlider(draftJudicialFunding, MinPolicyDialLevel, MaxPolicyDialLevel, _sliderStyle, _sliderThumbStyle);

            float draftBorderEnforcement = GetBorderEnforcementInput(_playerCountry.BorderEnforcementLevel);
            GUILayout.Label($"Border Enforcement - Standing: {_playerCountry.BorderEnforcementLevel:F0}, Draft: {draftBorderEnforcement:F0} (0 = open/lenient, 100 = strict)", _labelStyle);
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
        /// Master Sequence step 5d: pending-bill status plus the "Introduce Crime &amp; Justice Bill"
        /// action - introducible ANYTIME (unlike the annual BudgetBill, no mandatory-pause phase gates
        /// this), enabled only while no CrimeJusticePolicyBill is already pending (one bill per tab at
        /// a time - see SimulationManager.IntroduceCrimeJusticeBill). Follows DrawTaxPolicy's
        /// stable-control-layout pattern: the status Label and the Button are BOTH emitted every frame
        /// regardless of state - only the label's text and the button's GUI.enabled state vary.
        /// </summary>
        private void DrawCrimeJusticeBillStatusAndIntroduce()
        {
            CrimeJusticePolicyBill pendingBill = _simulationManager.GetPendingCrimeJusticeBill(PlayerCountryId);

            string statusText = pendingBill != null
                ? $"A Crime & Justice bill is before Parliament - resolves in {pendingBill.DaysRemaining} day(s)."
                : "No Crime & Justice bill currently before Parliament. Introduce your current draft as a bill below.";
            GUILayout.Label(statusText, _labelStyle);

            bool ambientEnabled = GUI.enabled;
            GUI.enabled = ambientEnabled && pendingBill == null;
            if (GUILayout.Button("Introduce Crime & Justice Bill", _neutralActionButtonStyle))
            {
                _simulationManager.IntroduceCrimeJusticeBill(PlayerCountryId, BuildCrimeJusticeBillFromDrafts());
            }
            GUI.enabled = ambientEnabled;
        }

        /// <summary>Master Sequence step 5d: recomputes every OnGUI call (cheap, same reasoning as DrawLegislativeSupportEstimate) so it updates live as the player edits any Crime &amp; Justice draft.</summary>
        private void DrawCrimeJusticeLiveEstimate()
        {
            CrimeJusticePolicyBill draftBill = BuildCrimeJusticeBillFromDrafts();
            float direction = ParliamentSystem.GetCrimeJusticeBillDirection(_playerCountry, draftBill);
            bool wouldPass = ParliamentSystem.WouldBillPass(_playerCountry, direction);

            string directionLabel = Mathf.Approximately(direction, 0f) ? "Neutral" : direction > 0f ? "Expansionary" : "Contractionary";
            GUILayout.Label($"Bill direction: {directionLabel} ({direction:+0.0;-0.0;0})", _labelStyle);
            DrawColoredLabel(wouldPass ? "Current seat composition: WOULD PASS" : "Current seat composition: WOULD FAIL",
                _labelStyle, UiPalette.GetDeltaColor(wouldPass ? 1f : -1f, higherIsBetter: true));
        }

        /// <summary>Bundles every current Crime &amp; Justice draft into one bill, exactly as it stands at the moment of the call - the SAME snapshot logic for both the live estimate and the real Introduce action, mirroring BuildBudgetBillFromDrafts.</summary>
        private CrimeJusticePolicyBill BuildCrimeJusticeBillFromDrafts()
        {
            return new CrimeJusticePolicyBill
            {
                PoliceFunding = GetPoliceFundingInput(_playerCountry.PoliceFundingLevel),
                SentencingSeverity = GetSentencingSeverityInput(_playerCountry.SentencingSeverity),
                BailReform = GetBailReformInput(_playerCountry.BailReformLevel),
                DrugPolicy = GetDrugPolicyInput(_playerCountry.DrugPolicyLevel),
                JudicialFunding = GetJudicialFundingInput(_playerCountry.JudicialFundingLevel),
                BorderEnforcement = GetBorderEnforcementInput(_playerCountry.BorderEnforcementLevel)
            };
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
            GUILayout.Label("Master Sequence step 5d: every dial below is a DRAFT - nothing happens until you introduce them as one standalone bill, which resolves independently of the annual budget cycle.", _labelStyle);
            GUILayout.Space(8f);

            DrawLaborBillStatusAndIntroduce();
            DrawLaborLiveEstimate();
            GUILayout.Space(8f);

            DrawMinimumWageControl();

            float draftPaidLeave = GetPaidFamilyLeaveWeeksInput(_playerCountry.PaidFamilyLeaveWeeks);
            GUILayout.Label($"Paid Family Leave - Standing: {_playerCountry.PaidFamilyLeaveWeeks:F0}, Draft: {draftPaidLeave:F0} weeks", _labelStyle);
            _paidFamilyLeaveWeeksInput = GUILayout.HorizontalSlider(draftPaidLeave, MinPaidFamilyLeaveWeeks, MaxPaidFamilyLeaveWeeks, _sliderStyle, _sliderThumbStyle);

            float draftOvertimeRegulation = GetOvertimeRegulationInput(_playerCountry.OvertimeRegulationLevel);
            GUILayout.Label($"Overtime/Working-Hour Regulation - Standing: {_playerCountry.OvertimeRegulationLevel:F0}, Draft: {draftOvertimeRegulation:F0} (0 = unregulated, 100 = strict caps)", _labelStyle);
            _overtimeRegulationInput = GUILayout.HorizontalSlider(draftOvertimeRegulation, MinLaborDialLevel, MaxLaborDialLevel, _sliderStyle, _sliderThumbStyle);

            float draftRetraining = GetRetrainingProgramInput(_playerCountry.RetrainingProgramLevel);
            GUILayout.Label($"Workforce Retraining Programs - Standing: {_playerCountry.RetrainingProgramLevel:F0}, Draft: {draftRetraining:F0}", _labelStyle);
            _retrainingProgramInput = GUILayout.HorizontalSlider(draftRetraining, MinLaborDialLevel, MaxLaborDialLevel, _sliderStyle, _sliderThumbStyle);

            GUILayout.Space(8f);
            float draftFamilyPolicy = GetFamilyPolicyInput(_playerCountry.FamilyPolicyLevel);
            GUILayout.Label($"Family Policy - Standing: {_playerCountry.FamilyPolicyLevel:F0}, Draft: {draftFamilyPolicy:F0} (0 = minimal support, 100 = maximal pro-natalist support)", _labelStyle);
            _familyPolicyInput = GUILayout.HorizontalSlider(draftFamilyPolicy, MinPolicyDialLevel, MaxPolicyDialLevel, _sliderStyle, _sliderThumbStyle);

            float draftImmigrationPolicy = GetImmigrationPolicyInput(_playerCountry.ImmigrationPolicyLevel);
            GUILayout.Label($"Immigration Policy - Standing: {_playerCountry.ImmigrationPolicyLevel:F0}, Draft: {draftImmigrationPolicy:F0} (0 = maximally restrictive, 100 = maximally open)", _labelStyle);
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

        /// <summary>See DrawCrimeJusticeBillStatusAndIntroduce's own doc comment - identical pattern (SimulationManager.IntroduceLaborBill/GetPendingLaborBill).</summary>
        private void DrawLaborBillStatusAndIntroduce()
        {
            LaborPolicyBill pendingBill = _simulationManager.GetPendingLaborBill(PlayerCountryId);

            string statusText = pendingBill != null
                ? $"A Labor Market bill is before Parliament - resolves in {pendingBill.DaysRemaining} day(s)."
                : "No Labor Market bill currently before Parliament. Introduce your current draft as a bill below.";
            GUILayout.Label(statusText, _labelStyle);

            bool ambientEnabled = GUI.enabled;
            GUI.enabled = ambientEnabled && pendingBill == null;
            if (GUILayout.Button("Introduce Labor Market Bill", _neutralActionButtonStyle))
            {
                _simulationManager.IntroduceLaborBill(PlayerCountryId, BuildLaborBillFromDrafts());
            }
            GUI.enabled = ambientEnabled;
        }

        /// <summary>See DrawCrimeJusticeLiveEstimate's own doc comment - identical pattern.</summary>
        private void DrawLaborLiveEstimate()
        {
            LaborPolicyBill draftBill = BuildLaborBillFromDrafts();
            float direction = ParliamentSystem.GetLaborBillDirection(_playerCountry, draftBill);
            bool wouldPass = ParliamentSystem.WouldBillPass(_playerCountry, direction);

            string directionLabel = Mathf.Approximately(direction, 0f) ? "Neutral" : direction > 0f ? "Expansionary" : "Contractionary";
            GUILayout.Label($"Bill direction: {directionLabel} ({direction:+0.0;-0.0;0})", _labelStyle);
            DrawColoredLabel(wouldPass ? "Current seat composition: WOULD PASS" : "Current seat composition: WOULD FAIL",
                _labelStyle, UiPalette.GetDeltaColor(wouldPass ? 1f : -1f, higherIsBetter: true));
        }

        /// <summary>See BuildCrimeJusticeBillFromDrafts's own doc comment - identical pattern.</summary>
        private LaborPolicyBill BuildLaborBillFromDrafts()
        {
            return new LaborPolicyBill
            {
                MinimumWage = GetMinimumWageInput(_playerCountry.MinimumWagePercentOfMedian),
                PaidFamilyLeaveWeeks = GetPaidFamilyLeaveWeeksInput(_playerCountry.PaidFamilyLeaveWeeks),
                OvertimeRegulation = GetOvertimeRegulationInput(_playerCountry.OvertimeRegulationLevel),
                RetrainingProgram = GetRetrainingProgramInput(_playerCountry.RetrainingProgramLevel),
                FamilyPolicy = GetFamilyPolicyInput(_playerCountry.FamilyPolicyLevel),
                ImmigrationPolicy = GetImmigrationPolicyInput(_playerCountry.ImmigrationPolicyLevel)
            };
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
            GUILayout.Label($"Minimum Wage - Standing: {_playerCountry.MinimumWagePercentOfMedian:F0}%, Draft: {draftMinimumWage:F0}% of median wage", _labelStyle);
            _minimumWageInput = GUILayout.HorizontalSlider(draftMinimumWage, MinMinimumWagePercent, MaxMinimumWagePercent, _sliderStyle, _sliderThumbStyle);
        }

        /// <summary>
        /// Descriptive only, no player-facing dial (Infrastructure Condition is driven entirely by
        /// the existing Infrastructure spending category - see MacroSystem.ApplyInfrastructureCondition
        /// and GetInfrastructureSummaryLine's own original doc comment for why). Proportional bars,
        /// not a line graph - this is "how do these four assets compare right now" breakdown data,
        /// not a trend-over-time reading, matching the task's own bar-vs-graph guidance. Master
        /// Sequence step 5e, Phase A: the old standalone Infrastructure tab is retired (Elias's own
        /// confirmed placement - folds into Tax/Spending alongside Welfare/SWF, since this content was
        /// already reused verbatim inside Budget Process and the standalone tab had no lever of its
        /// own) - this content-only method is now reached exclusively via DrawBudgetProcessTab.
        /// </summary>
        private void DrawInfrastructureContent()
        {
            DrawColoredLabel("Infrastructure", _headerStyle, UiPalette.GetAreaColor(UiPalette.SystemArea.Infrastructure));
            GUILayout.Label("Condition Index (0-100) per asset type - driven by the Infrastructure spending category in the Spending Policy tab, not a dial here.", _labelStyle);
            GUILayout.Space(8f);

            foreach (InfrastructureAsset asset in _playerCountry.InfrastructureAssets)
            {
                GUILayout.Label($"{asset.Type}: {asset.ConditionIndex:F0} / 100", _labelStyle);
                UiPalette.DrawBar(asset.ConditionIndex / 100f, UiPalette.GetAreaColor(UiPalette.SystemArea.Infrastructure));
                GUILayout.Space(8f);
            }
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

            // Political Systems Overhaul Part B, full rollout (Master Sequence step 5c/5d): draft
            // Tax/Spending/Welfare/SWF (step 5c) and now Minimum Wage/Police Funding/Sentencing
            // Severity/Bail Reform/Drug Policy/Judicial Funding/Border Enforcement/Family Policy/
            // Immigration Policy/Paid Family Leave/Overtime Regulation/Retraining Program/every Sector
            // dial/Tariff Rate/Partner Tariff Overrides (step 5d) no longer change what the preview
            // would show at all - they only ever reach the simulation via a passed bill - so this only
            // needs to check InterestRateChange now (the one input BuildPlayerDecision still reads).
            // See BuildBudgetBillFromDrafts/BuildLaborBillFromDrafts/BuildCrimeJusticeBillFromDrafts/
            // BuildSectorBillFromDrafts/BuildTradeBillFromDrafts for where these same draft
            // dictionaries/fields actually get read now, and each tab's own live vote estimate for the
            // live feedback those drafts DO still drive.
            return !Mathf.Approximately(_interestRateChangeInput, _cachedInterestRateChangeInput);
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
            _cachedTariffRateInput = _tariffRateInput;
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
        /// Master Sequence step 5a added a fourth condition, hasPendingBudgetProcess, per the revised
        /// Part B design's explicit "extend the existing global banner, don't build a fourth ad-hoc
        /// pause system" instruction - 5a's own temporary "Acknowledge" placeholder button (see git
        /// history) is gone now that step 5c built the real Budget Process introduce-bill flow; this
        /// banner now names the Budget Process tab exactly like the other three conditions name their
        /// own tab, rather than trying to resolve the pause from inside the banner itself.
        ///
        /// BUG FOUND VIA LIVE PLAY (fixed here): the original if/else-if chain showed only ONE reason,
        /// in a fixed priority order - if a Foreign Policy meeting became pending at the same time as
        /// (or was already pending when) the annual budget pause opened, the banner showed ONLY the
        /// Foreign Policy message, completely hiding that a budget bill also needed introducing -
        /// Update's own gate correctly kept blocking on BOTH conditions underneath, but the player had
        /// no visible way to know the budget process was one of the reasons. Fixed by listing EVERY
        /// currently-true reason in one combined message, ordered Fed Chair/Cabinet (structural,
        /// pre-existing) then Budget Process then Foreign Policy (an optional meeting is the least
        /// consequential of the four) - "time is paused and here's why" must never be ambiguous. Still
        /// exactly one Label control either way (per DrawTaxPolicy's stable-control-layout pattern -
        /// content and style vary, the control itself never does).
        /// </summary>
        private void DrawCalendarAndSpeedControls(bool hasPendingFedChairSelection, bool hasPendingCabinetDecisions, bool hasPendingForeignPolicyMeeting, bool hasPendingBudgetProcess)
        {
            GUILayout.BeginVertical();

            string dateText = _simulationManager.CurrentDate.ToString("MMMM d, yyyy");
            bool isPaused = hasPendingFedChairSelection || hasPendingCabinetDecisions || hasPendingForeignPolicyMeeting || hasPendingBudgetProcess;

            string statusText = dateText;
            if (isPaused)
            {
                var reasons = new List<string>();
                if (hasPendingFedChairSelection)
                {
                    reasons.Add("choose the next Fed Chair (Federal Reserve tab)");
                }
                if (hasPendingCabinetDecisions)
                {
                    reasons.Add("resolve the pending Cabinet decision (Cabinet tab)");
                }
                if (hasPendingBudgetProcess)
                {
                    reasons.Add("introduce the annual budget bill (Budget Process tab)");
                }
                if (hasPendingForeignPolicyMeeting)
                {
                    reasons.Add("respond to the pending Foreign Policy meeting (Foreign Policy tab)");
                }
                statusText = $"{dateText} - TIME PAUSED: {string.Join("; ", reasons)} to continue.";
            }
            GUILayout.Label(statusText, isPaused ? _eventBannerStyle : _labelStyle);

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

        /// <summary>Master Sequence step 5d: the pending TaxProgramBill for this specific TaxType, or null if none is currently before Parliament - used to grey out DrawTaxLineRow's Implement/Remove button (only one bill per TaxType may be pending at a time) and show its own countdown.</summary>
        private TaxProgramBill FindPendingTaxProgramBill(TaxType type)
        {
            foreach (TaxProgramBill bill in _simulationManager.GetPendingTaxProgramBills(PlayerCountryId))
            {
                if (bill.Type == type)
                {
                    return bill;
                }
            }
            return null;
        }

        /// <summary>WelfareProgramType equivalent of FindPendingTaxProgramBill - see that method's own doc comment, same pattern.</summary>
        private WelfareProgramBill FindPendingWelfareProgramBill(WelfareProgramType type)
        {
            foreach (WelfareProgramBill bill in _simulationManager.GetPendingWelfareProgramBills(PlayerCountryId))
            {
                if (bill.Type == type)
                {
                    return bill;
                }
            }
            return null;
        }

        private bool GetSwfExistsDraft(bool fallbackExists)
        {
            return _swfExistsDraft ?? fallbackExists;
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

        /// <summary>The Trade tab's General Base Tariff Rate draft (an absolute target, matching TaxLine.Rate - see _tariffRateInput's own doc comment), or <paramref name="fallbackRate"/> (Country.BaseTariffRate) if the player hasn't touched it.</summary>
        private float GetTariffRateInput(float fallbackRate) => _tariffRateInput ?? fallbackRate;
        private float GetCachedTariffRateInput(float fallbackRate) => _cachedTariffRateInput ?? fallbackRate;

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
                InterestRateChange = _interestRateChangeInput
            };

            // Political Systems Overhaul Part B, full rollout (Master Sequence step 5c/5d): Tax
            // (step 4 pilot), Spending, Welfare, SWF (step 5c), and now Minimum Wage/Police Funding/
            // Sentencing Severity/Bail Reform/Drug Policy/Judicial Funding/Border Enforcement/Family
            // Policy/Immigration Policy/Paid Family Leave/Overtime Regulation/Retraining Program/every
            // Sector dial/Tariff Rate/Partner Tariff Overrides (step 5d) no longer feed PolicyDecision
            // at all here - draft changes to any of them only ever reach the simulation via a PASSED
            // bill (the omnibus BudgetBill for Tax/Spending/Welfare/SWF, or one of the new standalone
            // LaborPolicyBill/CrimeJusticePolicyBill/SectorPolicyBill/TradePolicyBill for step 5d's
            // tier). Only InterestRateChange remains here - the Federal Reserve/Eurozone exemption
            // means it was never gated in the first place. This is the ONE call site both AdvanceTurn
            // and the live preview share, so removing them here makes the preview honest too (it no
            // longer shows an effect that won't actually happen until a bill passes) - see
            // BuildBudgetBillFromDrafts/BuildLaborBillFromDrafts/BuildCrimeJusticeBillFromDrafts/
            // BuildSectorBillFromDrafts/BuildTradeBillFromDrafts for where these same draft
            // dictionaries/fields actually get read now.

            return decision;
        }

        private void ResetPolicyInputs()
        {
            // _taxRateInputs/_spendingLineInputs/_welfareGenerosityInputs, the SWF draft fields, and
            // (as of Master Sequence step 5d) every Labor/Crime & Justice/Sector/Trade draft field are
            // all deliberately NOT cleared here - none of them are a this-turn delta anymore now that
            // they're all gated behind a bill rather than applied every AdvanceTurn; leaving them in
            // place keeps each slider showing whatever the player last drafted instead of snapping
            // back. Only InterestRateChange stays a genuine this-turn delta (Federal Reserve/Eurozone
            // exemption - never gated), so it's the only one still reset here.
            _interestRateChangeInput = 0f;
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
        /// Master Sequence step 5e, Phase A: the 7 consolidated top-level tabs, all fitting in ONE row
        /// (short labels, unlike the old 18-tab bar's "Sovereign Wealth Fund"-length names) - replaces
        /// the old 6-per-row/5-row layout entirely. Each tab is tinted by its own SystemArea (see
        /// GetConsolidatedTabArea) - selected uses the bright TabSelected variant, unselected the
        /// dimmer Tab variant, same mechanic the old bar used, per Phase A's own "no visual style
        /// change, only navigation changes" constraint.
        /// </summary>
        private const int ConsolidatedTabsPerRow = 7;

        // Master Sequence step 5e, Phase C: the consolidated tab bar stacks its icon ABOVE its label
        // rather than beside it. Beside-it was tried first (Phase B) and genuinely does not fit: the
        // right column is ~55% of the window, so at 1080p each of the 7 tabs gets ~143px, while
        // "Demographics" alone is ~175px of text at the 26px tab font - reserving a left gutter for an
        // icon pushed labels to three lines and clipped them, and shrinking the icon to compensate put
        // it back to the unreadable speck it started as. Stacking gives the label the full button width
        // and the icon a size that actually reads. All five values below are deliberately expressed as
        // multiples of the CURRENT tab font size (itself screen-height-derived, see RescaleStylesToScreen)
        // rather than fixed pixels, so the whole arrangement scales with the window the same way every
        // other control in this class already does.
        private const float ConsolidatedTabIconTopPadding = 7f;
        private const float ConsolidatedTabIconLabelGap = 3f;
        private const float ConsolidatedTabLabelBottomPadding = 5f;
        private const float ConsolidatedTabIconFontMultiple = 1.15f;
        private const float ConsolidatedTabLabelFontScale = 0.72f;

        /// <summary>
        /// Explicitly divided evenly across <paramref name="availableWidth"/> - the SAME
        /// rightColumnWidth OnGUI already computes fresh from Screen.width every frame - so the row
        /// can never exceed its actual budget at any window size, matching the screen-relative
        /// approach already used everywhere else in this class (see the old DrawRightColumnTabs' own
        /// doc comment on why this matters, kept in git history).
        /// </summary>
        private void DrawConsolidatedTabs(float availableWidth)
        {
            float buttonWidth = availableWidth / ConsolidatedTabsPerRow;

            GUILayout.BeginHorizontal();
            // Master Sequence step 5e, Phase C: all 7 tabs now carry their icon. The four icon_nav_*
            // ones exist precisely because Statistics/Decisions/Demographics/Policy-Laws map to no
            // single UiPalette.SystemArea; Tax, Spending and Politics instead reuse the existing area
            // icons directly, exactly as CLAUDE_DESIGN_ASSET_REQUEST_5E.md's own manifest specifies
            // ("Tax/Spending/Politics tabs reuse the existing icon_area_fiscal/icon_area_political
            // icons directly - no new art needed"). Tax and Spending deliberately share one icon:
            // both are GetConsolidatedTabArea -> Fiscal, and both are two differently-labeled entry
            // points into the SAME Budget Process screen, so a shared mark is honest rather than a
            // collision - flagged to Elias rather than silently substituted for something else.
            DrawConsolidatedTabButton("Statistics", ConsolidatedTab.Statistics, buttonWidth, "icon_nav_statistics");
            DrawConsolidatedTabButton("Decisions", ConsolidatedTab.Decisions, buttonWidth, "icon_nav_decisions");
            DrawConsolidatedTabButton("Demographics", ConsolidatedTab.Demographics, buttonWidth, "icon_nav_demographics");
            DrawConsolidatedTabButton("Tax", ConsolidatedTab.Tax, buttonWidth, "icon_area_fiscal");
            DrawConsolidatedTabButton("Spending", ConsolidatedTab.Spending, buttonWidth, "icon_area_fiscal");
            DrawConsolidatedTabButton("Policy/Laws", ConsolidatedTab.PolicyLaws, buttonWidth, "icon_nav_policylaws");
            DrawConsolidatedTabButton("Politics", ConsolidatedTab.Politics, buttonWidth, "icon_area_political");
            GUILayout.EndHorizontal();
        }

        /// <summary>
        /// Each tab is tinted by its own SystemArea (see GetConsolidatedTabArea) - selected uses the
        /// bright TabSelected variant, unselected the dimmer Tab variant, so the currently-open tab
        /// reads as visibly "lit up" in its own area's hue rather than just bold+yellow text. Switching
        /// INTO Tax or Spending also seeds `_budgetProcessCategory` so the shared Budget Process screen
        /// opens at the right starting category (see DrawTaxTab/DrawSpendingTab) - the only place this
        /// button click does anything beyond changing which tab is selected, since Tax/Spending are two
        /// differently-labeled entry points into the exact same underlying screen, not two screens.
        ///
        /// Master Sequence step 5e, Phase C: when <paramref name="iconName"/> is given, the icon is
        /// stacked ABOVE the label (see the ConsolidatedTabIcon* constants for why beside-it was
        /// abandoned). Crucially the space is RESERVED via `style.padding.top` BEFORE the button
        /// draws, rather than the icon being overlaid on top afterwards - overlaying was the actual
        /// cause of the icon-over-text collision Elias reported, since GUILayout centres the label in
        /// whatever box it is given and neither party knew about the other. Padding makes the label's
        /// own layout account for the icon, so they cannot collide at any window size or label length.
        /// Every style change here is made on the per-call CLONE that BuildButtonStyle already returns,
        /// never on `_tabButtonStyle` itself - that shared style also backs the sub-category buttons and
        /// the Implement/Remove/Neutral action buttons, which must not inherit a taller tab bar's
        /// geometry. A missing/failed-to-load texture degrades to the plain text-only button rather
        /// than a gap, since the padding is only applied once the texture is known to be non-null.
        /// </summary>
        private void DrawConsolidatedTabButton(string label, ConsolidatedTab tab, float width, string iconName = null)
        {
            UiPalette.SystemArea area = GetConsolidatedTabArea(tab);
            bool selected = _consolidatedTab == tab;
            GUIStyle style = UiPalette.BuildButtonStyle(_tabButtonStyle, selected ? UiPalette.ButtonKind.TabSelected : UiPalette.ButtonKind.Tab, area);

            Texture2D icon = iconName != null ? IconLibrary.Get(iconName) : null;
            float iconSize = 0f;
            if (icon != null)
            {
                iconSize = Mathf.Round(_tabButtonStyle.fontSize * ConsolidatedTabIconFontMultiple);
                int labelFontSize = Mathf.Max(11, Mathf.RoundToInt(_tabButtonStyle.fontSize * ConsolidatedTabLabelFontScale));
                float labelBandHeight = labelFontSize + 6f;

                style.fontSize = labelFontSize;
                style.alignment = TextAnchor.MiddleCenter;
                style.padding.top = Mathf.RoundToInt(ConsolidatedTabIconTopPadding + iconSize + ConsolidatedTabIconLabelGap);
                style.padding.bottom = Mathf.RoundToInt(ConsolidatedTabLabelBottomPadding);
                // Left/right trimmed to near-zero so the label gets the button's full width on one
                // line - the whole point of stacking. Never smaller than the base height, so a very
                // short window can't produce a tab bar shorter than the rest of the UI expects.
                style.padding.left = 2;
                style.padding.right = 2;
                style.fixedHeight = Mathf.Max(
                    _tabButtonStyle.fixedHeight,
                    style.padding.top + labelBandHeight + ConsolidatedTabLabelBottomPadding);
            }

            bool clicked = GUILayout.Button(label, style, GUILayout.Width(width));

            if (icon != null)
            {
                Rect buttonRect = GUILayoutUtility.GetLastRect();
                var iconRect = new Rect(
                    buttonRect.x + (buttonRect.width - iconSize) * 0.5f,
                    buttonRect.y + ConsolidatedTabIconTopPadding,
                    iconSize,
                    iconSize);
                Color iconTint = selected ? Color.white : new Color(1f, 1f, 1f, 0.6f);
                UiPalette.DrawTintedIcon(iconRect, icon, iconTint);
            }

            if (clicked)
            {
                _consolidatedTab = tab;
                if (tab == ConsolidatedTab.Tax)
                {
                    _budgetProcessCategory = BudgetProcessCategory.Tax;
                }
                else if (tab == ConsolidatedTab.Spending)
                {
                    _budgetProcessCategory = BudgetProcessCategory.Spending;
                }
            }
        }

        /// <summary>Generic sub-category tab button, shared by Statistics/Policy-Laws/Politics' own category rows - mirrors DrawBudgetProcessCategoryButton's exact established pattern (Primary when selected, Neutral otherwise - no per-area tinting at this second level, unlike the top-level tabs above).</summary>
        private void DrawSubCategoryButton<T>(string label, T category, ref T selectedCategory) where T : struct, System.Enum
        {
            bool selected = EqualityComparer<T>.Default.Equals(selectedCategory, category);
            GUIStyle style = UiPalette.BuildButtonStyle(_tabButtonStyle, selected ? UiPalette.ButtonKind.Primary : UiPalette.ButtonKind.Neutral);
            if (GUILayout.Button(label, style))
            {
                selectedCategory = category;
            }
        }

        /// <summary>
        /// Master Sequence step 5e, Phase A: Statistics tab - Recent Turns + World Map (both directly
        /// named in the original 5e scope text) plus Trade's informational half (see
        /// DrawTradeStatsContent's own doc comment on the split). RecentTurns/WorldMap reuse their
        /// full old Draw*Tab methods UNCHANGED (each already owns its own box/scrollview) rather than
        /// being surgically split - Phase A's own "no visual style changes, minimize risk" constraint
        /// favors reusing existing rendering wholesale over extracting content-only pieces that don't
        /// already exist, even at the cost of a harmless nested box for those two categories.
        /// </summary>
        private void DrawStatisticsTab(float availableHeight)
        {
            GUILayout.BeginVertical(_boxStyle);
            DrawColoredLabel("Statistics", _headerStyle, UiPalette.GetAreaColor(UiPalette.SystemArea.Global));
            GUILayout.BeginHorizontal();
            DrawSubCategoryButton("Recent Turns", StatisticsCategory.RecentTurns, ref _statisticsCategory);
            DrawSubCategoryButton("World Map", StatisticsCategory.WorldMap, ref _statisticsCategory);
            DrawSubCategoryButton("Trade", StatisticsCategory.Trade, ref _statisticsCategory);
            GUILayout.EndHorizontal();
            GUILayout.Space(6f);

            float contentHeight = availableHeight - _headerStyle.fontSize - _tabButtonStyle.fixedHeight - 14f;
            switch (_statisticsCategory)
            {
                case StatisticsCategory.RecentTurns:
                    DrawTurnLog(contentHeight);
                    break;
                case StatisticsCategory.WorldMap:
                    DrawWorldMapTab(contentHeight);
                    break;
                case StatisticsCategory.Trade:
                    float scrollHeight = contentHeight - _labelStyle.fontSize * 2f;
                    _statisticsContentScrollPosition = GUILayout.BeginScrollView(_statisticsContentScrollPosition, GUILayout.Height(scrollHeight));
                    DrawTradeStatsContent();
                    GUILayout.EndScrollView();
                    break;
            }
            GUILayout.EndVertical();
        }

        /// <summary>
        /// Master Sequence step 5e, Phase A: Decisions tab - every currently pending interrupt shown
        /// together in one place, not a category selector (there's nothing to browse when nothing's
        /// pending, unlike every other consolidated tab). Covers all four blocking interrupts that
        /// exist in this codebase: Fed Chair selection, Foreign Policy meetings, Cabinet decisions, and
        /// (per Elias's own confirmed reasoning - "any 'time is blocked until you respond' state
        /// belongs in the same place, not treated as an exception") the Budget Process mandatory
        /// pause. Every piece reuses the exact same rendering the old per-tab modals already used
        /// (DrawFedChairSelectionModal/DrawForeignPolicyMeetingModal/DrawCabinetDecisionModal/
        /// DrawBudgetBillStatusAndIntroduce) - Decisions doesn't reimplement anything, it just gathers.
        /// Matches the old dispatch's own gating exactly: all four were only ever reachable through a
        /// `!_isGameOver`-gated tab before, so the whole body stays gated here too.
        /// </summary>
        private void DrawDecisionsTab(float availableHeight)
        {
            GUILayout.BeginVertical(_boxStyle);

            float scrollHeight = availableHeight - _labelStyle.fontSize * 2f;
            _decisionsScrollPosition = GUILayout.BeginScrollView(_decisionsScrollPosition, GUILayout.Height(scrollHeight));

            DrawColoredLabel("Decisions", _headerStyle, UiPalette.GetAreaColor(UiPalette.SystemArea.CrimeJustice));
            GUILayout.Label("Everything currently waiting on your response, gathered in one place.", _labelStyle);
            GUILayout.Space(6f);

            GUI.enabled = !_isGameOver;

            bool anyPending = false;

            if (_fedChairCandidates != null && _fedChairCandidates.Count > 0)
            {
                DrawFedChairSelectionModal();
                GUILayout.Space(8f);
                anyPending = true;
            }

            ForeignPolicyMeeting pendingMeeting = _simulationManager.GetPendingForeignPolicyMeeting(PlayerCountryId);
            if (pendingMeeting != null)
            {
                DrawForeignPolicyMeetingModal(pendingMeeting);
                GUILayout.Space(8f);
                anyPending = true;
            }

            foreach ((CabinetPortfolio portfolio, CabinetDecision decision) in _simulationManager.GetPendingCabinetDecisions(PlayerCountryId))
            {
                DrawCabinetDecisionModal(portfolio, decision);
                GUILayout.Space(8f);
                anyPending = true;
            }

            if (_simulationManager.GetPendingBudgetProcess(PlayerCountryId))
            {
                DrawBudgetBillStatusAndIntroduce();
                GUILayout.Space(8f);
                anyPending = true;
            }

            GUI.enabled = true;

            if (!anyPending)
            {
                GUILayout.Label("No pending decisions.", _labelStyle);
            }

            GUILayout.EndScrollView();
            GUILayout.EndVertical();
        }

        /// <summary>Master Sequence step 5e, Phase A: Demographics tab - just the pie-chart half of the old "Compass & Demographics" tab (see DrawDemographicsContent's own doc comment), no category selector needed since there's only one content source. Never gated on game-over, matching the old tab's own behavior (pure visualization, no player-facing controls).</summary>
        private void DrawDemographicsTab(float availableHeight)
        {
            GUILayout.BeginVertical(_boxStyle);
            float scrollHeight = availableHeight - _labelStyle.fontSize * 2f;
            _demographicsScrollPosition = GUILayout.BeginScrollView(_demographicsScrollPosition, GUILayout.Height(scrollHeight));
            DrawDemographicsContent();
            GUILayout.EndScrollView();
            GUILayout.EndVertical();
        }

        /// <summary>
        /// Master Sequence step 5e, Phase A: Policy/Laws tab - Labor Market/Crime &amp; Justice/
        /// Economic Sectors (each already has its own tier-3 standalone bill from 5d), Policy Web
        /// (Elias's own placement, overriding the original Statistics recommendation - "a relationship/
        /// reference tool consulted while deciding what to change, closer to where bills get drafted
        /// than to a pure stats readout"), and Trade's policy half (DrawTradePolicyContent). Per-
        /// category gating matches the old dispatch exactly, not a blanket gate - Labor/Crime/Sectors
        /// were gated, Policy Web/Trade were not.
        /// </summary>
        private void DrawPolicyLawsTab(float availableHeight)
        {
            GUILayout.BeginVertical(_boxStyle);
            DrawColoredLabel("Policy / Laws", _headerStyle, UiPalette.GetAreaColor(UiPalette.SystemArea.Sectors));
            GUILayout.BeginHorizontal();
            DrawSubCategoryButton("Labor Market", PolicyLawsCategory.LaborMarket, ref _policyLawsCategory);
            DrawSubCategoryButton("Crime & Justice", PolicyLawsCategory.CrimeJustice, ref _policyLawsCategory);
            DrawSubCategoryButton("Economic Sectors", PolicyLawsCategory.Sectors, ref _policyLawsCategory);
            DrawSubCategoryButton("Policy Web", PolicyLawsCategory.PolicyWeb, ref _policyLawsCategory);
            DrawSubCategoryButton("Trade", PolicyLawsCategory.Trade, ref _policyLawsCategory);
            GUILayout.EndHorizontal();
            GUILayout.Space(6f);

            float contentHeight = availableHeight - _headerStyle.fontSize - _tabButtonStyle.fixedHeight - 14f;
            switch (_policyLawsCategory)
            {
                case PolicyLawsCategory.LaborMarket:
                    GUI.enabled = !_isGameOver;
                    DrawLaborMarketTab(contentHeight);
                    GUI.enabled = true;
                    break;
                case PolicyLawsCategory.CrimeJustice:
                    GUI.enabled = !_isGameOver;
                    DrawCrimeJusticeTab(contentHeight);
                    GUI.enabled = true;
                    break;
                case PolicyLawsCategory.Sectors:
                    GUI.enabled = !_isGameOver;
                    DrawSectorPolicy(contentHeight);
                    GUI.enabled = true;
                    break;
                case PolicyLawsCategory.PolicyWeb:
                    DrawPolicyWebTab(contentHeight);
                    break;
                case PolicyLawsCategory.Trade:
                    float scrollHeight = contentHeight - _labelStyle.fontSize * 2f;
                    _policyLawsContentScrollPosition = GUILayout.BeginScrollView(_policyLawsContentScrollPosition, GUILayout.Height(scrollHeight));
                    DrawTradePolicyContent();
                    GUILayout.EndScrollView();
                    break;
            }
            GUILayout.EndVertical();
        }

        /// <summary>
        /// Master Sequence step 5e, Phase A: Politics tab - Parliament, the Political Compass half of
        /// the old "Compass & Demographics" tab, Cabinet's management half, and Federal Reserve (Elias's
        /// own confirmed placement - a real political institution with its own lever, even though the
        /// Fed/Eurozone exemption means it's never Parliament-gated). Per-category gating matches the
        /// old dispatch exactly - Parliament/Compass were never gated, Cabinet/FederalReserve were.
        /// </summary>
        private void DrawPoliticsTab(float availableHeight)
        {
            GUILayout.BeginVertical(_boxStyle);
            DrawColoredLabel("Politics", _headerStyle, UiPalette.GetAreaColor(UiPalette.SystemArea.Political));
            GUILayout.BeginHorizontal();
            DrawSubCategoryButton("Parliament", PoliticsCategory.Parliament, ref _politicsCategory);
            DrawSubCategoryButton("Compass", PoliticsCategory.Compass, ref _politicsCategory);
            DrawSubCategoryButton("Cabinet", PoliticsCategory.Cabinet, ref _politicsCategory);
            DrawSubCategoryButton(GetCentralBankName(PlayerCountryId), PoliticsCategory.FederalReserve, ref _politicsCategory);
            GUILayout.EndHorizontal();
            GUILayout.Space(6f);

            float contentHeight = availableHeight - _headerStyle.fontSize - _tabButtonStyle.fixedHeight - 14f;
            switch (_politicsCategory)
            {
                case PoliticsCategory.Parliament:
                    DrawParliamentTab(contentHeight);
                    break;
                case PoliticsCategory.Compass:
                    float compassScrollHeight = contentHeight - _labelStyle.fontSize * 2f;
                    _politicsContentScrollPosition = GUILayout.BeginScrollView(_politicsContentScrollPosition, GUILayout.Height(compassScrollHeight));
                    DrawPoliticalCompassContent();
                    GUILayout.EndScrollView();
                    break;
                case PoliticsCategory.Cabinet:
                    float cabinetScrollHeight = contentHeight - _labelStyle.fontSize * 2f;
                    GUI.enabled = !_isGameOver;
                    _cabinetScrollPosition = GUILayout.BeginScrollView(_cabinetScrollPosition, GUILayout.Height(cabinetScrollHeight));
                    DrawCabinetManagementContent();
                    GUILayout.EndScrollView();
                    GUI.enabled = true;
                    break;
                case PoliticsCategory.FederalReserve:
                    GUI.enabled = !_isGameOver;
                    DrawFederalReserveTab(contentHeight);
                    GUI.enabled = true;
                    break;
            }
            GUILayout.EndVertical();
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
        /// <summary>
        /// Master Sequence step 5e, Phase A: the old standalone Cabinet tab SPLIT across two new
        /// destinations, per Elias's own confirmed mapping (the original 5e scope text names Cabinet
        /// under both "Decisions" and "Politics") - this content-only piece is the Decisions half
        /// (pending-decision modals only), called from DrawDecisionsTab. See
        /// DrawCabinetManagementContent for the Politics half (portfolio panels). No outer box/
        /// scrollview here, matching this codebase's own established "*Content" convention (e.g.
        /// DrawTaxPolicyContent) - the caller owns the chrome.
        /// </summary>
        private void DrawCabinetPendingDecisionsContent()
        {
            foreach ((CabinetPortfolio portfolio, CabinetDecision decision) in _simulationManager.GetPendingCabinetDecisions(PlayerCountryId))
            {
                DrawCabinetDecisionModal(portfolio, decision);
                GUILayout.Space(8f);
            }
        }

        /// <summary>Politics half of the old Cabinet tab - see DrawCabinetPendingDecisionsContent's own doc comment for the split reasoning. Called from DrawPoliticsTab.</summary>
        private void DrawCabinetManagementContent()
        {
            DrawColoredLabel("Cabinet", _headerStyle, UiPalette.GetAreaColor(UiPalette.SystemArea.Political));
            GUILayout.Label("Each appointed minister quietly nudges their own portfolio's existing channels every turn just by serving, and occasionally brings you a real decision with a few response options. Philosophy determines what KIND of decisions a minister brings, not how skilled they are - that's CompetenceBias, a separate trait. Reshuffling a minister costs a modest approval hit but can happen anytime. Pending decisions themselves now show under the Decisions tab.", _labelStyle);
            GUILayout.Space(6f);

            foreach (CabinetPortfolio portfolio in System.Enum.GetValues(typeof(CabinetPortfolio)))
            {
                DrawCabinetPortfolioPanel(portfolio);
                GUILayout.Space(8f);
            }
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
        /// Master Sequence step 5e, Phase A: the old standalone Foreign Policy tab is fully retired,
        /// not split - its ENTIRE content was always just this interrupt (confirmed by reading its old
        /// body: explanatory text + either the modal or "No meeting currently pending," nothing else),
        /// so it moves to Decisions wholesale (see DrawDecisionsTab) with nothing left behind. Only
        /// this modal renderer survives, reused as-is from Decisions.
        /// </summary>
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
        /// Parliament tab (Political Systems Overhaul Part B, full rollout): the hemicycle
        /// (HemicycleRenderer) plus a pending-bill summary. Master Sequence step 5c generalized the
        /// pending bill from a Tax-only TaxBill to the omnibus BudgetBill (Tax+Spending+Welfare+SWF
        /// together) - see the Budget Process tab to introduce one.
        /// </summary>
        private void DrawParliamentTab(float availableHeight)
        {
            GUILayout.BeginVertical(_boxStyle);

            float scrollHeight = availableHeight - _labelStyle.fontSize * 2f;
            _parliamentScrollPosition = GUILayout.BeginScrollView(_parliamentScrollPosition, GUILayout.Height(scrollHeight));

            DrawColoredLabel("Parliament", _headerStyle, UiPalette.GetAreaColor(UiPalette.SystemArea.Political));
            GUILayout.Label("Seats shift gradually with your ApprovalRating. The annual budget bill and any standalone bills are gated by Parliament - see the Budget Process tab to introduce one.", _labelStyle);
            GUILayout.Space(6f);

            _hemicycleRenderer.Draw($"{_playerCountry.Name} - {ParliamentConstants.TotalSeats} seats", _playerCountry.ParliamentSeats, _labelStyle);

            GUILayout.Space(10f);
            DrawPendingLegislation();

            GUILayout.EndScrollView();
            GUILayout.EndVertical();
        }

        /// <summary>
        /// Master Sequence step 5d: a consolidated list of EVERY bill currently before Parliament,
        /// across all three tiers - the annual BudgetBill, every standalone TaxProgramBill/
        /// WelfareProgramBill, and each of the four standalone tier-3 bills (at most one per tab).
        /// Before 5d this only ever showed the annual bill, since it was the only tier that existed.
        /// A plain list, not a fixed set of Labels, since the tier-2 count varies - fine for
        /// stable-control-layout purposes because nothing here is ever mid-drag (this tab has no
        /// sliders of its own, only read-only status text).
        /// </summary>
        private void DrawPendingLegislation()
        {
            GUILayout.Label("Pending Legislation", _headerStyle);

            var lines = new List<string>();

            BudgetBill budgetBill = _simulationManager.GetPendingBudgetBill(PlayerCountryId);
            if (budgetBill != null)
            {
                bool wouldPass = ParliamentSystem.WouldBillPass(_playerCountry, budgetBill);
                lines.Add($"Annual budget bill - resolves in {budgetBill.DaysRemaining} day(s). Current seat composition leans {(wouldPass ? "PASS" : "FAIL")}.");
            }

            foreach (TaxProgramBill bill in _simulationManager.GetPendingTaxProgramBills(PlayerCountryId))
            {
                bool wouldPass = ParliamentSystem.WouldBillPass(_playerCountry, ParliamentSystem.GetTaxProgramBillDirection(_playerCountry, bill));
                lines.Add($"{(bill.IsAdd ? "Implement" : "Remove")} {bill.Type} - resolves in {bill.DaysRemaining} day(s). Current seat composition leans {(wouldPass ? "PASS" : "FAIL")}.");
            }

            foreach (WelfareProgramBill bill in _simulationManager.GetPendingWelfareProgramBills(PlayerCountryId))
            {
                bool wouldPass = ParliamentSystem.WouldBillPass(_playerCountry, ParliamentSystem.GetWelfareProgramBillDirection(_playerCountry, bill));
                lines.Add($"{(bill.IsAdd ? "Implement" : "Remove")} {bill.Type} - resolves in {bill.DaysRemaining} day(s). Current seat composition leans {(wouldPass ? "PASS" : "FAIL")}.");
            }

            LaborPolicyBill laborBill = _simulationManager.GetPendingLaborBill(PlayerCountryId);
            if (laborBill != null)
            {
                bool wouldPass = ParliamentSystem.WouldBillPass(_playerCountry, ParliamentSystem.GetLaborBillDirection(_playerCountry, laborBill));
                lines.Add($"Labor Market bill - resolves in {laborBill.DaysRemaining} day(s). Current seat composition leans {(wouldPass ? "PASS" : "FAIL")}.");
            }

            CrimeJusticePolicyBill crimeJusticeBill = _simulationManager.GetPendingCrimeJusticeBill(PlayerCountryId);
            if (crimeJusticeBill != null)
            {
                bool wouldPass = ParliamentSystem.WouldBillPass(_playerCountry, ParliamentSystem.GetCrimeJusticeBillDirection(_playerCountry, crimeJusticeBill));
                lines.Add($"Crime & Justice bill - resolves in {crimeJusticeBill.DaysRemaining} day(s). Current seat composition leans {(wouldPass ? "PASS" : "FAIL")}.");
            }

            SectorPolicyBill sectorBill = _simulationManager.GetPendingSectorBill(PlayerCountryId);
            if (sectorBill != null)
            {
                bool wouldPass = ParliamentSystem.WouldBillPass(_playerCountry, ParliamentSystem.GetSectorBillDirection(_playerCountry, sectorBill));
                lines.Add($"Economic Sectors bill - resolves in {sectorBill.DaysRemaining} day(s). Current seat composition leans {(wouldPass ? "PASS" : "FAIL")}.");
            }

            TradePolicyBill tradeBill = _simulationManager.GetPendingTradeBill(PlayerCountryId);
            if (tradeBill != null)
            {
                bool wouldPass = ParliamentSystem.WouldBillPass(_playerCountry, ParliamentSystem.GetTradeBillDirection(_playerCountry, tradeBill));
                lines.Add($"Trade bill - resolves in {tradeBill.DaysRemaining} day(s). Current seat composition leans {(wouldPass ? "PASS" : "FAIL")}.");
            }

            if (lines.Count == 0)
            {
                GUILayout.Label("No bill currently before Parliament.", _labelStyle);
                return;
            }

            foreach (string line in lines)
            {
                GUILayout.Label(line, _labelStyle);
            }
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
        /// Master Sequence step 5e, Phase A: the old standalone "Compass &amp; Demographics" tab SPLIT
        /// across two new destinations, per Elias's own confirmed mapping (the original 5e scope text
        /// separates "Compass" under Politics from "Demographics (population/pie charts)" as its own
        /// tab). This content-only piece is the Political Compass half, called from DrawPoliticsTab.
        /// See DrawDemographicsContent for the Demographics half (all five pie charts). No outer box/
        /// scrollview here, matching this codebase's own established "*Content" convention.
        /// </summary>
        private void DrawPoliticalCompassContent()
        {
            DrawColoredLabel("Political Compass", _headerStyle, UiPalette.GetAreaColor(UiPalette.SystemArea.Global));
            GUILayout.Label("Grounded entirely in this game's own tracked policy data - no invented ideology labels. X: average implemented tax rate blended with total government spending (% of GDP) - further right means a bigger fiscal footprint. Y: average sector regulation blended with average implemented welfare generosity - higher means more market regulation and a more generous welfare state. Your own country is ringed in white.", _labelStyle);
            float compassSize = Mathf.Clamp(Screen.height * 0.4f, 260f, 520f);
            Rect compassRect = GUILayoutUtility.GetRect(compassSize, compassSize, GUILayout.ExpandWidth(false));
            _politicalCompassRenderer.Draw(compassRect, _world.Countries, PlayerCountryId, _labelStyle);
        }

        /// <summary>Demographics half of the old "Compass & Demographics" tab - see DrawPoliticalCompassContent's own doc comment for the split reasoning. Called from DrawDemographicsTab. Ethnicity/religion breakdowns are explicitly OUT OF SCOPE per the Master Roadmap's own Part C spec - not tracked anywhere in this game's data model.</summary>
        private void DrawDemographicsContent()
        {
            DrawColoredLabel("Demographics", _headerStyle, UiPalette.GetAreaColor(UiPalette.SystemArea.Global));
            GUILayout.Label("No ethnicity/religion breakdown (not tracked anywhere in this game's data model). The five charts below are all scoped to the player's own country except Population, which is inherently comparative.", _labelStyle);
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
        }

        /// <summary>
        /// Master Sequence step 5e, Phase A: the old standalone Trade tab SPLIT across two new
        /// destinations, per Elias's own confirmed mapping - informational content (this piece, the
        /// Trade Balance graph) to Statistics, policy content (DrawTradePolicyContent below) to
        /// Policy/Laws. Implementation refinement Elias also confirmed: per-partner rows (bars AND
        /// override controls together) stay bundled as one unit under Policy/Laws rather than
        /// splitting a single row's own bars from its own controls across two different tabs - a
        /// player adjusting an override wants the volume bars right next to it for context. No outer
        /// box/scrollview here, matching this codebase's own established "*Content" convention.
        /// </summary>
        private void DrawTradeStatsContent()
        {
            EconomyState state = _playerCountry.State;
            DrawColoredLabel("Trade", _headerStyle, UiPalette.GetAreaColor(UiPalette.SystemArea.Trade));
            DrawColoredLabel($"Overall Trade Balance: {state.TradeBalance:F1}", _labelStyle, UiPalette.GetDeltaColor(state.TradeBalance, higherIsBetter: true));
            _tradeBalanceGraph.Draw("Trade Balance", _playerCountry.History.TradeBalance.Quarterly, null, _labelStyle, higherIsBetter: true);
        }

        /// <summary>Policy half of the old Trade tab (the TradePolicyBill and every per-partner row) - see DrawTradeStatsContent's own doc comment for the split reasoning. Called from DrawPolicyLawsTab.</summary>
        private void DrawTradePolicyContent()
        {
            DrawColoredLabel("Trade Policy", _headerStyle, UiPalette.GetAreaColor(UiPalette.SystemArea.Trade));
            GUILayout.Label("Master Sequence step 5d: the base rate and every partner override's RATE below are DRAFTS - nothing happens until you introduce them as one standalone bill, which resolves independently of the annual budget cycle. Setting/resetting whether a partner override exists at all stays an immediate, structural action, unchanged.", _labelStyle);
            GUILayout.Space(6f);

            DrawTradeBillStatusAndIntroduce();
            DrawTradeLiveEstimate();
            GUILayout.Space(8f);

            float draftTariffRate = GetTariffRateInput(_playerCountry.BaseTariffRate);
            GUILayout.Label($"General Base Tariff Rate - Standing: {_playerCountry.BaseTariffRate:F2}%, Draft: {draftTariffRate:F2}% (range {MinBaseTariffRate:F0}-{MaxBaseTariffRate:F0}%; applies to any partner with no override, and only where it isn't superseded by trade-bloc membership)", _labelStyle);
            _tariffRateInput = GUILayout.HorizontalSlider(draftTariffRate, MinBaseTariffRate, MaxBaseTariffRate, _sliderStyle, _sliderThumbStyle);
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
                GUILayout.Label($"Override rate - Standing: {link.PlayerTariffOverride:F2}%, Draft: {draftRate:F2}% (range {PartnerTariffOverrideMin:F0}-{PartnerTariffOverrideMax:F0}%; applies via the Trade bill below)", _labelStyle);
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

        /// <summary>See DrawCrimeJusticeBillStatusAndIntroduce's own doc comment - identical pattern (SimulationManager.IntroduceTradeBill/GetPendingTradeBill).</summary>
        private void DrawTradeBillStatusAndIntroduce()
        {
            TradePolicyBill pendingBill = _simulationManager.GetPendingTradeBill(PlayerCountryId);

            string statusText = pendingBill != null
                ? $"A Trade bill is before Parliament - resolves in {pendingBill.DaysRemaining} day(s)."
                : "No Trade bill currently before Parliament. Introduce your current draft as a bill below.";
            GUILayout.Label(statusText, _labelStyle);

            bool ambientEnabled = GUI.enabled;
            GUI.enabled = ambientEnabled && pendingBill == null;
            if (GUILayout.Button("Introduce Trade Bill", _neutralActionButtonStyle))
            {
                _simulationManager.IntroduceTradeBill(PlayerCountryId, BuildTradeBillFromDrafts());
            }
            GUI.enabled = ambientEnabled;
        }

        /// <summary>See DrawCrimeJusticeLiveEstimate's own doc comment - identical pattern. Only the base rate sways this estimate (see ParliamentSystem.GetTradeBillDirection's own doc comment on why partner overrides are excluded).</summary>
        private void DrawTradeLiveEstimate()
        {
            TradePolicyBill draftBill = BuildTradeBillFromDrafts();
            float direction = ParliamentSystem.GetTradeBillDirection(_playerCountry, draftBill);
            bool wouldPass = ParliamentSystem.WouldBillPass(_playerCountry, direction);

            string directionLabel = Mathf.Approximately(direction, 0f) ? "Neutral" : direction > 0f ? "Expansionary" : "Contractionary";
            GUILayout.Label($"Bill direction: {directionLabel} ({direction:+0.0;-0.0;0})", _labelStyle);
            DrawColoredLabel(wouldPass ? "Current seat composition: WOULD PASS" : "Current seat composition: WOULD FAIL",
                _labelStyle, UiPalette.GetDeltaColor(wouldPass ? 1f : -1f, higherIsBetter: true));
        }

        /// <summary>Bundles the base tariff rate draft and every partner override draft into one bill - the SAME snapshot logic for both the live estimate and the real Introduce action, mirroring BuildBudgetBillFromDrafts. Only a partner with an ACTIVE override gets an entry, mirroring BuildPlayerDecision's own former "only currently-implemented" reasoning.</summary>
        private TradePolicyBill BuildTradeBillFromDrafts()
        {
            var bill = new TradePolicyBill { NewBaseTariffRate = GetTariffRateInput(_playerCountry.BaseTariffRate) };
            foreach (TradePartner tradePartner in _playerCountry.TradePartners)
            {
                if (!tradePartner.HasPlayerTariffOverride)
                {
                    continue;
                }
                bill.PartnerTariffOverrides[tradePartner.PartnerId] = GetPartnerTariffInput(tradePartner.PartnerId, tradePartner.PlayerTariffOverride);
            }
            return bill;
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
        /// Master Sequence step 5b: the Budget Process full-screen UI shell - left category selector /
        /// center selected category's line-items / right live summary, consolidating the existing Tax,
        /// Spending, Welfare, Infrastructure, and Sovereign Wealth Fund content onto one screen (their
        /// own standalone tabs stay as independent entry points too, per the design's own "5e does tab
        /// consolidation, not 5b" sequencing - both read/write the exact same draft state, since the
        /// center column calls the SAME *Content methods those tabs call). No new bill logic here -
        /// that's 5c/5d's job; the right column reuses the EXISTING live Policy Preview panel as this
        /// phase's "live summary," not a new estimate.
        ///
        /// Stable-control-layout note: the center column's content switches based on
        /// _budgetProcessCategory, which only the player's own left-column button click can change -
        /// unlike a bill resolving in the background, a click can never race an active drag on a
        /// DIFFERENT control (one mouse, one control at a time), so this particular conditional swap
        /// isn't the hazard class DrawTaxPolicy's own doc comment warns about. Each *Content method
        /// reused here (DrawTaxPolicyContent etc.) already carries its own stable-control-layout
        /// guarantee independently - that safety property carries over automatically by reuse.
        /// </summary>
        private void DrawBudgetProcessTab(float availableHeight, float availableWidth)
        {
            GUILayout.BeginVertical(_boxStyle);

            DrawColoredLabel("Budget Process", _headerStyle, UiPalette.GetAreaColor(UiPalette.SystemArea.Fiscal));
            // Explicit Width, not left to GUILayout's own inference - the horizontal 3-column row
            // below can otherwise push this outer group's computed "natural" width past the screen
            // edge (a boxed column's GUILayout.Width request plus its GUIStyle's own padding can add
            // up to more than requested), which made this label wrap against an inflated width and
            // clip mid-word rather than wrap. Tying it directly to availableWidth makes its wrap
            // boundary correct regardless of what the row does.
            GUILayout.Label("Consolidates Tax, Spending, Welfare, Infrastructure, and Sovereign Wealth Fund drafts onto one screen. Left: category. Center: that category's line-items (the same draft as its own standalone tab - edits apply either place). Right: this turn's live estimate across your whole current draft.", _labelStyle, GUILayout.Width(availableWidth));
            GUILayout.Space(8f);

            DrawBudgetBillStatusAndIntroduce();
            GUILayout.Space(8f);

            float headerAllowance = _labelStyle.fontSize * 7f + _headerStyle.fontSize + 16f;
            float columnsHeight = availableHeight - headerAllowance;
            float columnSpacing = 10f;

            // The right column reuses DrawPolicyPreview UNCHANGED from its original dashboard-left-
            // column home, where it unconditionally gets Screen.width * LeftColumnWidthFraction
            // (0.45). A previous attempt capped this at half the row's own budget "so category/center
            // never collapse to nothing on a narrow window" - confirmed via a debug-instrumented
            // screenshot that this cap, not the Screen.width calculation, was the actual binding
            // constraint at ordinary window sizes (~641px observed vs. the ~864px the panel actually
            // needs), starving the panel at EVERY window size, not just narrow ones. Fixed: the
            // preview panel gets its natural width unconditionally; category/center get sane MINIMUM
            // widths instead of being derived from whatever's left over. If the three don't all fit at
            // their natural/minimum sizes, that's a genuine narrow-window case, handled explicitly via
            // a horizontal scrollview below rather than silently squeezing any one column.
            float summaryColumnWidth = Screen.width * LeftColumnWidthFraction;
            float categoryColumnWidth = Mathf.Max(_labelStyle.fontSize * 9f, availableWidth * 0.15f);
            float centerColumnMinWidth = _labelStyle.fontSize * 20f;
            float centerColumnWidth = Mathf.Max(centerColumnMinWidth, availableWidth - summaryColumnWidth - categoryColumnWidth - columnSpacing * 2f);
            float totalRowWidth = categoryColumnWidth + columnSpacing + centerColumnWidth + columnSpacing + summaryColumnWidth;

            _budgetProcessRowScrollPosition = GUILayout.BeginScrollView(_budgetProcessRowScrollPosition, GUILayout.Width(availableWidth), GUILayout.Height(columnsHeight));
            GUILayout.BeginHorizontal(GUILayout.Width(totalRowWidth), GUILayout.Height(columnsHeight));

            GUILayout.BeginVertical(GUILayout.Width(categoryColumnWidth));
            DrawBudgetProcessCategoryButton("Tax", BudgetProcessCategory.Tax);
            DrawBudgetProcessCategoryButton("Spending", BudgetProcessCategory.Spending);
            DrawBudgetProcessCategoryButton("Welfare", BudgetProcessCategory.Welfare);
            DrawBudgetProcessCategoryButton("Infrastructure", BudgetProcessCategory.Infrastructure);
            DrawBudgetProcessCategoryButton("Sovereign Wealth Fund", BudgetProcessCategory.Swf);
            GUILayout.EndVertical();

            GUILayout.Space(columnSpacing);

            GUILayout.BeginVertical(_boxStyle, GUILayout.Width(centerColumnWidth));
            _budgetProcessCenterScrollPosition = GUILayout.BeginScrollView(_budgetProcessCenterScrollPosition, GUILayout.Height(columnsHeight - _labelStyle.fontSize));
            switch (_budgetProcessCategory)
            {
                case BudgetProcessCategory.Tax:
                    DrawTaxPolicyContent();
                    break;
                case BudgetProcessCategory.Spending:
                    DrawSpendingPolicyContent();
                    break;
                case BudgetProcessCategory.Welfare:
                    DrawWelfarePolicyContent();
                    break;
                case BudgetProcessCategory.Infrastructure:
                    DrawInfrastructureContent();
                    break;
                case BudgetProcessCategory.Swf:
                    DrawSwfPolicyContent();
                    break;
            }
            GUILayout.EndScrollView();
            GUILayout.EndVertical();

            GUILayout.Space(columnSpacing);

            GUILayout.BeginVertical(_boxStyle, GUILayout.Width(summaryColumnWidth));
            DrawLegislativeSupportEstimate();
            GUILayout.Space(10f);
            DrawPolicyPreview();
            GUILayout.EndVertical();

            GUILayout.EndHorizontal();
            GUILayout.EndScrollView();
            GUILayout.EndVertical();
        }

        private void DrawBudgetProcessCategoryButton(string label, BudgetProcessCategory category)
        {
            bool selected = _budgetProcessCategory == category;
            GUIStyle style = UiPalette.BuildButtonStyle(_tabButtonStyle, selected ? UiPalette.ButtonKind.Primary : UiPalette.ButtonKind.Neutral);
            if (GUILayout.Button(label, style))
            {
                _budgetProcessCategory = category;
            }
        }

        /// <summary>
        /// Master Sequence step 5c: pending-bill status plus the "Introduce Budget Bill" action -
        /// enabled only while the mandatory pause (Master Sequence step 5a) is open and no bill is
        /// already pending, since only one bill may be before Parliament at a time and introducing is
        /// only ever meaningful on the country's own fiscal-year date (see SimulationManager.
        /// IntroduceBudgetBill/GetPendingBudgetProcess). Follows DrawTaxPolicy's stable-control-layout
        /// pattern: the status Label and the Button are BOTH emitted every frame regardless of state -
        /// only the label's text and the button's GUI.enabled state vary.
        /// </summary>
        private void DrawBudgetBillStatusAndIntroduce()
        {
            BudgetBill pendingBill = _simulationManager.GetPendingBudgetBill(PlayerCountryId);
            bool budgetProcessOpen = _simulationManager.GetPendingBudgetProcess(PlayerCountryId);

            string statusText = pendingBill != null
                ? $"An annual budget bill is before Parliament - resolves in {pendingBill.DaysRemaining} day(s)."
                : budgetProcessOpen
                    ? "The annual budget process is open - introduce your current draft as a bill below to continue."
                    : "No budget bill currently before Parliament. One can only be introduced on your country's own fiscal-year date.";
            GUILayout.Label(statusText, _labelStyle);

            bool ambientEnabled = GUI.enabled;
            GUI.enabled = ambientEnabled && pendingBill == null && budgetProcessOpen;
            if (GUILayout.Button("Introduce Budget Bill", _neutralActionButtonStyle))
            {
                _simulationManager.IntroduceBudgetBill(PlayerCountryId, BuildBudgetBillFromDrafts());
            }
            GUI.enabled = ambientEnabled;
        }

        /// <summary>
        /// Master Sequence step 5c's "live support estimate" - recomputes every OnGUI call (cheap:
        /// BuildBudgetBillFromDrafts and ParliamentSystem's formulas are all O(a handful of items), no
        /// cloning, unlike PreviewTurn/RecomputePolicyPreview's own caching, which exists specifically
        /// because THAT computation is comparatively expensive) so it updates live as the player edits
        /// ANY draft - budget or standalone - not just after introducing, per the revised Part B
        /// design's own explicit instruction.
        /// </summary>
        private void DrawLegislativeSupportEstimate()
        {
            BudgetBill draftBill = BuildBudgetBillFromDrafts();
            float direction = ParliamentSystem.GetBillDirection(_playerCountry, draftBill);
            bool wouldPass = ParliamentSystem.WouldBillPass(_playerCountry, draftBill);

            GUILayout.Label("Legislative Support (current draft)", _headerStyle);
            string directionLabel = Mathf.Approximately(direction, 0f) ? "Neutral" : direction > 0f ? "Expansionary" : "Contractionary";
            GUILayout.Label($"Bill direction: {directionLabel} ({direction:+0.0;-0.0;0})", _labelStyle);
            DrawColoredLabel(wouldPass ? "Current seat composition: WOULD PASS" : "Current seat composition: WOULD FAIL",
                _labelStyle, UiPalette.GetDeltaColor(wouldPass ? 1f : -1f, higherIsBetter: true));
        }

        /// <summary>
        /// Bundles every current draft - Tax, Spending, Welfare, SWF - into one omnibus BudgetBill,
        /// exactly as it stands at the moment of the call (used both for DrawLegislativeSupportEstimate's
        /// live, continuously-recomputed estimate and for the real bill DrawBudgetBillStatusAndIntroduce
        /// submits - the SAME snapshot logic either way, so the estimate the player saw is exactly what
        /// gets introduced). Infrastructure has no direct lever of its own - see BudgetBill's own doc
        /// comment - so it's covered here only via Spending's own "Infrastructure" category.
        /// </summary>
        private BudgetBill BuildBudgetBillFromDrafts()
        {
            var bill = new BudgetBill();

            foreach (TaxLine taxLine in _playerCountry.TaxLines)
            {
                if (!taxLine.IsImplemented)
                {
                    continue;
                }
                bill.TaxLines[taxLine.Type] = GetTaxRateInput(taxLine.Type, taxLine.Rate);
            }

            foreach (SpendingLine spendingLine in _playerCountry.SpendingLines)
            {
                float percent = GetSpendingLineInput(spendingLine.Category);
                if (percent != 0f)
                {
                    bill.SpendingPercentChanges[spendingLine.Category] = percent;
                }
            }

            foreach (WelfareProgram welfareProgram in _playerCountry.WelfarePrograms)
            {
                if (!welfareProgram.IsImplemented)
                {
                    continue;
                }
                bill.WelfarePrograms[welfareProgram.Type] = GetWelfareGenerosityInput(welfareProgram.Type, welfareProgram.GenerosityLevel);
            }

            SovereignWealthFund fund = _playerCountry.SovereignWealthFund;
            SovereignWealthFund standingDefaults = fund ?? new SovereignWealthFund();
            bill.SwfShouldExist = GetSwfExistsDraft(fund != null);
            bill.SwfContributionRatePercent = GetSwfContributionRateInput(standingDefaults.ContributionRatePercent);
            bill.SwfDomesticAllocationPercent = GetSwfDomesticAllocationInput(standingDefaults.DomesticAllocationPercent);
            bill.SwfEquitiesWeight = GetSwfEquitiesWeightInput(standingDefaults.EquitiesWeight);
            bill.SwfBondsWeight = GetSwfBondsWeightInput(standingDefaults.BondsWeight);
            bill.SwfInfrastructureWeight = GetSwfInfrastructureWeightInput(standingDefaults.InfrastructureWeight);
            bill.SwfRealEstateWeight = GetSwfRealEstateWeightInput(standingDefaults.RealEstateWeight);

            return bill;
        }

        /// <summary>
        /// Political Systems Overhaul Part B, full rollout: the Tax Policy category's sliders/toggles
        /// remain DRAFT values (adjusting costs nothing, no vote needed) - since Master Sequence step
        /// 5c, the "Introduce Budget Bill" action lives centrally on this same Budget Process screen
        /// (an omnibus bill covering Tax+Spending+Welfare+SWF together, superseding the step 4 pilot's
        /// Tax-only TaxBill), and a PASSED bill is the only way a draft here ever reaches the real,
        /// standing TaxLines. Master Sequence step 5e, Phase A: the old standalone Tax Policy tab is
        /// retired (folds into the new Tax/Spending consolidated tabs, both of which are just entry
        /// points into this same Budget Process screen) - this content-only method is now reached
        /// exclusively via DrawBudgetProcessTab.
        ///
        /// STABLE CONTROL LAYOUT PATTERN (mandatory for every gated tab, not just this one - see
        /// "Background/timed state mutation vs. active UI interaction" in POLISIM_MASTER_ROADMAP.md's
        /// working-discipline failure patterns): once a background system can resolve on ANY simulated
        /// day - a bill passing/failing, and every one of the seven remaining tabs will gain this the
        /// moment Master Sequence step 5 lands - it can mutate the exact standing value a slider on
        /// this tab is reading, on a day the player has an active multi-frame drag in progress on that
        /// slider. GUILayout allocates control IDs positionally (call order within OnGUI), not by a
        /// stable key, so DrawTaxLineRow below (and DrawBudgetBillStatusAndIntroduce, which follows the
        /// exact same pattern for the omnibus bill's own status/introduce controls) must NEVER change
        /// which controls they emit, in what order, based on live/mutable state (a bill pending or not, a TaxType
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
        private void DrawTaxPolicyContent()
        {
            DrawColoredLabel("Tax Policy", _headerStyle, UiPalette.GetAreaColor(UiPalette.SystemArea.Fiscal));
            GUILayout.Label("A rate slider below only changes your DRAFT - nothing happens until the annual budget bill is introduced and passes (see the Budget Process tab). Implementing or removing a tax entirely is separate - it submits its own standalone bill immediately (Master Sequence step 5d), resolving independently of the annual cycle. See the Parliament tab for seat composition.", _labelStyle);
            GUILayout.Space(8f);

            float taxTypeNameColumnWidth = GetTaxTypeNameColumnWidth();
            foreach (TaxLine taxLine in _playerCountry.TaxLines)
            {
                DrawTaxLineRow(taxLine, taxTypeNameColumnWidth);
                GUILayout.Space(10f);
            }
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
        /// Master Sequence step 5d: Implement/Remove is now its OWN standalone TaxProgramBill,
        /// introduced immediately on click (not drafted first - a binary implement/remove decision has
        /// no separate "adjust before submitting" step the way a rate does), resolving independently of
        /// the annual budget cycle. The rate slider below stays a DRAFT feeding the annual BudgetBill,
        /// unchanged from 5c.
        ///
        /// Follows DrawTaxPolicy's stable-control-layout pattern: every control here (the toggle
        /// button, both status labels, the slider) renders every frame regardless of taxLine.
        /// IsImplemented or whether a TaxProgramBill is currently pending for this TaxType - "not
        /// currently applicable" is expressed via GUI.enabled = false (composed with, never clobbering,
        /// ambient enabled state) and/or a different label, never by omitting a control. This matters
        /// here specifically because BOTH taxLine.IsImplemented (ParliamentSystem.ApplyTaxProgramBillResult)
        /// and taxLine.Rate (ParliamentSystem.ApplyBillResult) can now change out from under an active
        /// drag on this exact row, from two independently-resolving bill tiers.
        /// </summary>
        private void DrawTaxLineRow(TaxLine taxLine, float labelWidth)
        {
            float buttonWidth = _labelStyle.fontSize * 6f;
            TaxProgramBill pendingBill = FindPendingTaxProgramBill(taxLine.Type);

            GUILayout.BeginHorizontal();
            GUILayout.Label(taxLine.Type.ToString(), _labelStyle, GUILayout.Width(labelWidth));

            string toggleLabel = pendingBill != null
                ? $"{(pendingBill.IsAdd ? "Implement" : "Remove")} bill pending ({pendingBill.DaysRemaining}d)"
                : taxLine.IsImplemented ? "Introduce Remove Bill" : "Introduce Implement Bill";
            GUIStyle toggleStyle = taxLine.IsImplemented ? _removeButtonStyle : _implementButtonStyle;
            bool ambientEnabledForButton = GUI.enabled;
            GUI.enabled = ambientEnabledForButton && pendingBill == null;
            if (GUILayout.Button(toggleLabel, toggleStyle, GUILayout.Width(buttonWidth * 2.4f)))
            {
                _simulationManager.IntroduceTaxProgramBill(PlayerCountryId, taxLine.Type, !taxLine.IsImplemented);
            }
            GUI.enabled = ambientEnabledForButton;
            GUILayout.EndHorizontal();

            DrawTaxProgramBillEstimate(taxLine, pendingBill);
            GUILayout.Label($"Standing: {(taxLine.IsImplemented ? $"{taxLine.Rate:F2}%" : "not implemented")}", _labelStyle);

            // The slider IS the current draft (defaulting to the standing Rate until dragged), bounded
            // by this TaxType's own TaxTypeRateRanges - not a small per-turn delta, so a meaningful
            // policy shift (e.g. IncomeTax 37% -> 55%) is reachable in one bill.
            float draftRate = GetTaxRateInput(taxLine.Type, taxLine.Rate);
            string draftLabel = taxLine.IsImplemented
                ? $"Draft rate: {draftRate:F2}%  (range {taxLine.MinRate:F0}-{taxLine.MaxRate:F0}%, applies via the next Annual Budget bill)"
                : "Draft rate: not implemented";
            GUILayout.Label(draftLabel, _labelStyle);

            // Compose with, never clobber, whatever ambient GUI.enabled the caller already set (e.g.
            // the tab-switch's own !_isGameOver gate) - restoring a hardcoded true here would
            // incorrectly re-enable this slider while the game is over.
            bool ambientEnabled = GUI.enabled;
            GUI.enabled = ambientEnabled && taxLine.IsImplemented;
            float newRate = GUILayout.HorizontalSlider(draftRate, taxLine.MinRate, taxLine.MaxRate, _sliderStyle, _sliderThumbStyle);
            GUI.enabled = ambientEnabled;
            if (taxLine.IsImplemented)
            {
                _taxRateInputs[taxLine.Type] = newRate;
            }
        }

        /// <summary>
        /// Master Sequence step 5d bugfix: DrawTaxLineRow's own Implement/Remove bill had no live
        /// pass/fail indicator, unlike every other bill tier (the annual BudgetBill's Legislative
        /// Support estimate, and every tier-3 tab's own estimate) - a player had no way to see whether
        /// their current click would actually pass BEFORE committing to a 21-day wait, and could easily
        /// end up looking at a DIFFERENT bill's estimate (e.g. the Budget Process tab's) and mistakenly
        /// think it applied here. If no bill is pending, this scores a HYPOTHETICAL bill for "click
        /// Implement/Remove right now" (the exact action the button above would take); if one IS
        /// pending, it scores THAT bill instead, since introducing a new one isn't the live question
        /// anymore.
        /// </summary>
        private void DrawTaxProgramBillEstimate(TaxLine taxLine, TaxProgramBill pendingBill)
        {
            TaxProgramBill bill = pendingBill ?? new TaxProgramBill { Type = taxLine.Type, IsAdd = !taxLine.IsImplemented };
            float direction = ParliamentSystem.GetTaxProgramBillDirection(_playerCountry, bill);
            bool wouldPass = ParliamentSystem.WouldBillPass(_playerCountry, direction);
            string prefix = pendingBill != null ? "Pending bill" : "If introduced now";
            DrawColoredLabel($"{prefix}: {(wouldPass ? "WOULD PASS" : "WOULD FAIL")} (current seat composition)",
                _labelStyle, UiPalette.GetDeltaColor(wouldPass ? 1f : -1f, higherIsBetter: true));
        }

        /// <summary>Every WelfareProgramType for the player's country: an Implement/Remove toggle (immediate - see DrawWelfareProgramRow) plus, only while implemented, a slider that directly sets this turn's target GenerosityLevel. Mirrors DrawTaxPolicyContent/DrawTaxLineRow exactly. Master Sequence step 5e, Phase A: the old standalone Welfare Policy tab is retired (folds into Tax/Spending, same as Tax) - reached exclusively via DrawBudgetProcessTab now.</summary>
        private void DrawWelfarePolicyContent()
        {
            DrawColoredLabel("Welfare Policy", _headerStyle, UiPalette.GetAreaColor(UiPalette.SystemArea.Welfare));
            GUILayout.Label("A generosity slider below only changes your DRAFT - nothing happens until the annual budget bill is introduced and passes (see the Budget Process tab). Implementing or removing a program entirely is separate - it submits its own standalone bill immediately (Master Sequence step 5d), resolving independently of the annual cycle.", _labelStyle);
            _povertyRateGraph.Draw("Poverty Rate", _playerCountry.History.PovertyRate.Quarterly, null, _labelStyle, higherIsBetter: false);
            GUILayout.Space(8f);

            float welfareTypeNameColumnWidth = GetWelfareProgramNameColumnWidth();
            foreach (WelfareProgram welfareProgram in _playerCountry.WelfarePrograms)
            {
                DrawWelfareProgramRow(welfareProgram, welfareTypeNameColumnWidth);
                GUILayout.Space(10f);
            }
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

        /// <summary>
        /// Master Sequence step 5d: Implement/Remove is now its OWN standalone WelfareProgramBill,
        /// introduced immediately on click, resolving independently of the annual budget cycle -
        /// mirrors DrawTaxLineRow's own doc comment exactly (GenerosityLevel in place of Rate).
        /// </summary>
        private void DrawWelfareProgramRow(WelfareProgram welfareProgram, float labelWidth)
        {
            float buttonWidth = _labelStyle.fontSize * 6f;
            WelfareProgramBill pendingBill = FindPendingWelfareProgramBill(welfareProgram.Type);

            GUILayout.BeginHorizontal();
            GUILayout.Label(welfareProgram.Type.ToString(), _labelStyle, GUILayout.Width(labelWidth));

            string toggleLabel = pendingBill != null
                ? $"{(pendingBill.IsAdd ? "Implement" : "Remove")} bill pending ({pendingBill.DaysRemaining}d)"
                : welfareProgram.IsImplemented ? "Introduce Remove Bill" : "Introduce Implement Bill";
            GUIStyle toggleStyle = welfareProgram.IsImplemented ? _removeButtonStyle : _implementButtonStyle;
            bool ambientEnabledForButton = GUI.enabled;
            GUI.enabled = ambientEnabledForButton && pendingBill == null;
            if (GUILayout.Button(toggleLabel, toggleStyle, GUILayout.Width(buttonWidth * 2.4f)))
            {
                _simulationManager.IntroduceWelfareProgramBill(PlayerCountryId, welfareProgram.Type, !welfareProgram.IsImplemented);
            }
            GUI.enabled = ambientEnabledForButton;
            GUILayout.EndHorizontal();

            DrawWelfareProgramBillEstimate(welfareProgram, pendingBill);
            GUILayout.Label($"Standing: {(welfareProgram.IsImplemented ? $"{welfareProgram.GenerosityLevel:F0}%" : "not implemented")}", _labelStyle);

            // The slider IS the current draft (defaulting to the standing GenerosityLevel until
            // dragged), bounded 0-100% - not a small per-turn delta, so a meaningful policy shift is
            // reachable in one bill.
            float draftGenerosity = GetWelfareGenerosityInput(welfareProgram.Type, welfareProgram.GenerosityLevel);
            string draftLabel = welfareProgram.IsImplemented
                ? $"Draft generosity: {draftGenerosity:F0}% (applies via the next Annual Budget bill)"
                : "Draft generosity: not implemented";
            GUILayout.Label(draftLabel, _labelStyle);

            bool ambientEnabled = GUI.enabled;
            GUI.enabled = ambientEnabled && welfareProgram.IsImplemented;
            float newGenerosity = GUILayout.HorizontalSlider(draftGenerosity, 0f, 100f, _sliderStyle, _sliderThumbStyle);
            GUI.enabled = ambientEnabled;
            if (welfareProgram.IsImplemented)
            {
                _welfareGenerosityInputs[welfareProgram.Type] = newGenerosity;
            }
        }

        /// <summary>See DrawTaxProgramBillEstimate's own doc comment - identical pattern (GenerosityLevel in place of Rate, WelfareProgramBill in place of TaxProgramBill).</summary>
        private void DrawWelfareProgramBillEstimate(WelfareProgram welfareProgram, WelfareProgramBill pendingBill)
        {
            WelfareProgramBill bill = pendingBill ?? new WelfareProgramBill { Type = welfareProgram.Type, IsAdd = !welfareProgram.IsImplemented };
            float direction = ParliamentSystem.GetWelfareProgramBillDirection(_playerCountry, bill);
            bool wouldPass = ParliamentSystem.WouldBillPass(_playerCountry, direction);
            string prefix = pendingBill != null ? "Pending bill" : "If introduced now";
            DrawColoredLabel($"{prefix}: {(wouldPass ? "WOULD PASS" : "WOULD FAIL")} (current seat composition)",
                _labelStyle, UiPalette.GetDeltaColor(wouldPass ? 1f : -1f, higherIsBetter: true));
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
            GUILayout.Label("Output/Employment/the sector's own metric are descriptive only in this pass - the five dials below nudge them, but they don't feed back into GDP/Unemployment. Master Sequence step 5d: every dial is a DRAFT across every sector - nothing happens until you introduce them all as one standalone bill, which resolves independently of the annual budget cycle.", _labelStyle);
            GUILayout.Space(8f);

            DrawSectorBillStatusAndIntroduce();
            DrawSectorLiveEstimate();
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
            GUILayout.Label($"Subsidy - Standing: {sector.SubsidyLevel:F0}, Draft: {draftSubsidy:F0}", _labelStyle);
            _sectorSubsidyInputs[sector.Type] = GUILayout.HorizontalSlider(draftSubsidy, MinPolicyDialLevel, MaxPolicyDialLevel, _sliderStyle, _sliderThumbStyle);

            float draftRegulation = GetSectorRegulationInput(sector.Type, sector.RegulationLevel);
            GUILayout.Label($"Regulation - Standing: {sector.RegulationLevel:F0}, Draft: {draftRegulation:F0} (0 = light-touch, 100 = heavily regulated)", _labelStyle);
            _sectorRegulationInputs[sector.Type] = GUILayout.HorizontalSlider(draftRegulation, MinPolicyDialLevel, MaxPolicyDialLevel, _sliderStyle, _sliderThumbStyle);

            float draftTaxCredit = GetSectorTaxCreditInput(sector.Type, sector.TaxCreditLevel);
            GUILayout.Label($"Tax Credits - Standing: {sector.TaxCreditLevel:F0}, Draft: {draftTaxCredit:F0}", _labelStyle);
            _sectorTaxCreditInputs[sector.Type] = GUILayout.HorizontalSlider(draftTaxCredit, MinPolicyDialLevel, MaxPolicyDialLevel, _sliderStyle, _sliderThumbStyle);

            float draftResearchGrants = GetSectorResearchGrantsInput(sector.Type, sector.ResearchGrantsLevel);
            GUILayout.Label($"Research Grants - Standing: {sector.ResearchGrantsLevel:F0}, Draft: {draftResearchGrants:F0}", _labelStyle);
            _sectorResearchGrantsInputs[sector.Type] = GUILayout.HorizontalSlider(draftResearchGrants, MinPolicyDialLevel, MaxPolicyDialLevel, _sliderStyle, _sliderThumbStyle);

            float draftDeregulation = GetSectorDeregulationInput(sector.Type, sector.DeregulationNationalizationLevel);
            GUILayout.Label($"Deregulation/Nationalization - Standing: {sector.DeregulationNationalizationLevel:F0}, Draft: {draftDeregulation:F0} (0 = fully nationalized, 100 = fully deregulated/private)", _labelStyle);
            _sectorDeregulationInputs[sector.Type] = GUILayout.HorizontalSlider(draftDeregulation, MinPolicyDialLevel, MaxPolicyDialLevel, _sliderStyle, _sliderThumbStyle);
        }

        /// <summary>See DrawCrimeJusticeBillStatusAndIntroduce's own doc comment - identical pattern (SimulationManager.IntroduceSectorBill/GetPendingSectorBill).</summary>
        private void DrawSectorBillStatusAndIntroduce()
        {
            SectorPolicyBill pendingBill = _simulationManager.GetPendingSectorBill(PlayerCountryId);

            string statusText = pendingBill != null
                ? $"An Economic Sectors bill is before Parliament - resolves in {pendingBill.DaysRemaining} day(s)."
                : "No Economic Sectors bill currently before Parliament. Introduce your current draft (across every sector) as a bill below.";
            GUILayout.Label(statusText, _labelStyle);

            bool ambientEnabled = GUI.enabled;
            GUI.enabled = ambientEnabled && pendingBill == null;
            if (GUILayout.Button("Introduce Economic Sectors Bill", _neutralActionButtonStyle))
            {
                _simulationManager.IntroduceSectorBill(PlayerCountryId, BuildSectorBillFromDrafts());
            }
            GUI.enabled = ambientEnabled;
        }

        /// <summary>See DrawCrimeJusticeLiveEstimate's own doc comment - identical pattern.</summary>
        private void DrawSectorLiveEstimate()
        {
            SectorPolicyBill draftBill = BuildSectorBillFromDrafts();
            float direction = ParliamentSystem.GetSectorBillDirection(_playerCountry, draftBill);
            bool wouldPass = ParliamentSystem.WouldBillPass(_playerCountry, direction);

            string directionLabel = Mathf.Approximately(direction, 0f) ? "Neutral" : direction > 0f ? "Expansionary" : "Contractionary";
            GUILayout.Label($"Bill direction: {directionLabel} ({direction:+0.0;-0.0;0})", _labelStyle);
            DrawColoredLabel(wouldPass ? "Current seat composition: WOULD PASS" : "Current seat composition: WOULD FAIL",
                _labelStyle, UiPalette.GetDeltaColor(wouldPass ? 1f : -1f, higherIsBetter: true));
        }

        /// <summary>Bundles every current Sector draft, across every SectorType, into one bill - the SAME snapshot logic for both the live estimate and the real Introduce action, mirroring BuildBudgetBillFromDrafts.</summary>
        private SectorPolicyBill BuildSectorBillFromDrafts()
        {
            var bill = new SectorPolicyBill();
            foreach (Sector sector in _playerCountry.Sectors)
            {
                bill.SubsidyLevels[sector.Type] = GetSectorSubsidyInput(sector.Type, sector.SubsidyLevel);
                bill.RegulationLevels[sector.Type] = GetSectorRegulationInput(sector.Type, sector.RegulationLevel);
                bill.TaxCreditLevels[sector.Type] = GetSectorTaxCreditInput(sector.Type, sector.TaxCreditLevel);
                bill.ResearchGrantsLevels[sector.Type] = GetSectorResearchGrantsInput(sector.Type, sector.ResearchGrantsLevel);
                bill.DeregulationLevels[sector.Type] = GetSectorDeregulationInput(sector.Type, sector.DeregulationNationalizationLevel);
            }
            return bill;
        }

        /// <summary>
        /// Sovereign Wealth Fund category: a Create/Dissolve button (immediate, mirrors TaxLine.
        /// IsImplemented's toggle pattern) plus, only while it exists, TotalAssets/this-turn estimated
        /// contribution-or-withdrawal+returns (read-only) and sliders for every adjustable setting,
        /// including the Contribution/Withdrawal Rate slider that now goes negative to draw the fund
        /// down (Round 3 item 1). Net Government Position (GovernmentDebt minus fund TotalAssets) is
        /// shown ALONGSIDE, not instead of, the raw GovernmentDebt figure already on the dashboard -
        /// per the task's explicit requirement that fund assets must never be used to obscure a real
        /// fiscal problem. This is a GameController-only display computation - it is never written
        /// back into EconomyState/Country and never read by any simulation formula
        /// (GetDebtRiskPremium, GetFiscalReactionMultiplier, etc. all keep reading the real, gross
        /// GovernmentDebt/DebtToGdpRatio exactly as before). Master Sequence step 5e, Phase A: the old
        /// standalone SWF Policy tab is retired (folds into Tax/Spending, same as Tax/Welfare) -
        /// reached exclusively via DrawBudgetProcessTab now.
        ///
        /// Political Systems Overhaul Part B, full rollout (Master Sequence step 5c): Create/Dissolve
        /// now edits DRAFT state only (_swfExistsDraft) - it no longer mutates
        /// Country.SovereignWealthFund directly, mirroring DrawTaxLineRow/DrawWelfareProgramRow.
        /// Follows the SAME stable-control-layout pattern: every control below (both info labels, all
        /// six sliders) is emitted every frame regardless of whether a real fund currently exists - a
        /// BudgetBill can create/dissolve the fund in the background (ParliamentSystem.ApplyBillResult
        /// / SimulationManager.ApplyBudgetBillSpendingAndSwf) on any day the countdown reaches zero,
        /// including a day the player has an active drag in progress on one of these sliders. Before
        /// step 5c, the whole rest of this method was behind an early `if (fund == null) return;` -
        /// exactly the omitted-control-block hazard this pattern exists to prevent, now that the
        /// underlying state is genuinely background-mutable. A throwaway SovereignWealthFund
        /// (standingDefaults) supplies fallback "standing" values for the sliders/labels while no real
        /// fund exists, so there's always something sensible to show and edit.
        /// </summary>
        private void DrawSwfPolicyContent()
        {
            DrawColoredLabel("Sovereign Wealth Fund", _headerStyle, UiPalette.GetAreaColor(UiPalette.SystemArea.SovereignWealth));

            SovereignWealthFund fund = _playerCountry.SovereignWealthFund;
            bool draftExists = GetSwfExistsDraft(fund != null);

            string toggleLabel = draftExists ? "Dissolve Fund (draft)" : "Create Fund (draft)";
            GUIStyle toggleStyle = draftExists ? _removeButtonStyle : _implementButtonStyle;
            if (GUILayout.Button(toggleLabel, toggleStyle))
            {
                _swfExistsDraft = !draftExists;
                RecomputePolicyPreview();
            }

            string standingText = fund != null
                ? $"Standing: fund exists. Total Assets: {fund.TotalAssets:F1}  |  Government Debt (gross): {_playerCountry.State.GovernmentDebt:F1}  |  Net Government Position: {_playerCountry.State.GovernmentDebt - fund.TotalAssets:F1}"
                : "Standing: no fund exists. Creating one (once the annual budget bill passes) starts a new budget expense (the contribution) in exchange for market returns on its growing assets - it can also be drawn down during a recession or emergency instead of borrowing.";
            GUILayout.Label(standingText, _labelStyle);

            string estimateText = fund != null
                ? $"Estimated this turn - Contribution/Withdrawal: {_cachedSwfContributionText}, Returns: {_cachedSwfReturnsText}"
                : "Estimated this turn - not applicable (no fund).";
            DrawColoredLabel(estimateText, _labelStyle, fund != null
                ? UiPalette.GetDeltaColor(_cachedSwfReturnsEstimateRaw, higherIsBetter: true)
                : UiPalette.GetDeltaColor(0f, higherIsBetter: true));
            GUILayout.Label(draftExists ? "Draft: fund drafted to exist." : "Draft: not implemented.", _labelStyle);
            GUILayout.Space(8f);

            SovereignWealthFund standingDefaults = fund ?? new SovereignWealthFund();
            bool ambientEnabled = GUI.enabled;

            float draftContributionRate = GetSwfContributionRateInput(standingDefaults.ContributionRatePercent);
            GUILayout.Label($"Contribution/Withdrawal Rate: {draftContributionRate:+0.0;-0.0;0}% of GDP per turn (negative draws the fund down - use during a recession or emergency instead of borrowing)", _labelStyle);
            GUI.enabled = ambientEnabled && draftExists;
            float newContributionRate = GUILayout.HorizontalSlider(draftContributionRate, MinSwfContributionRate, MaxSwfContributionRate, _sliderStyle, _sliderThumbStyle);
            GUI.enabled = ambientEnabled;
            if (draftExists)
            {
                _swfContributionRateInput = newContributionRate;
            }

            float draftDomesticAllocation = GetSwfDomesticAllocationInput(standingDefaults.DomesticAllocationPercent);
            GUILayout.Label($"Domestic Allocation: {draftDomesticAllocation:F0}% (rest international - this pass doesn't model differing returns by allocation)", _labelStyle);
            GUI.enabled = ambientEnabled && draftExists;
            float newDomesticAllocation = GUILayout.HorizontalSlider(draftDomesticAllocation, MinPolicyDialLevel, MaxPolicyDialLevel, _sliderStyle, _sliderThumbStyle);
            GUI.enabled = ambientEnabled;
            if (draftExists)
            {
                _swfDomesticAllocationInput = newDomesticAllocation;
            }

            GUILayout.Space(8f);
            GUILayout.Label("Asset Class Mix (weights, normalized automatically - don't need to sum to 100)", _labelStyle);

            // Each bar's fraction IS the already-normalized weight (0-1) - no further scaling needed,
            // unlike the spending-line/trade-volume bars above which normalize against a group max.
            // Normalized against the DRAFT weights (a throwaway SovereignWealthFund, never the real
            // one) via the same GetNormalizedWeight the real fund uses, rather than duplicating its
            // sum-and-divide logic here.
            Color swfColor = UiPalette.GetAreaColor(UiPalette.SystemArea.SovereignWealth);
            float draftEquities = GetSwfEquitiesWeightInput(standingDefaults.EquitiesWeight);
            float draftBonds = GetSwfBondsWeightInput(standingDefaults.BondsWeight);
            float draftInfrastructure = GetSwfInfrastructureWeightInput(standingDefaults.InfrastructureWeight);
            float draftRealEstate = GetSwfRealEstateWeightInput(standingDefaults.RealEstateWeight);
            var draftWeights = new SovereignWealthFund
            {
                EquitiesWeight = draftEquities,
                BondsWeight = draftBonds,
                InfrastructureWeight = draftInfrastructure,
                RealEstateWeight = draftRealEstate
            };

            GUILayout.Label($"Equities: {draftEquities:F0} ({draftWeights.GetNormalizedWeight(SovereignWealthAssetClass.Equities) * 100f:F0}% of fund)", _labelStyle);
            UiPalette.DrawBar(draftWeights.GetNormalizedWeight(SovereignWealthAssetClass.Equities), swfColor, 8f);
            GUI.enabled = ambientEnabled && draftExists;
            float newEquities = GUILayout.HorizontalSlider(draftEquities, MinPolicyDialLevel, MaxPolicyDialLevel, _sliderStyle, _sliderThumbStyle);
            GUI.enabled = ambientEnabled;
            if (draftExists)
            {
                _swfEquitiesWeightInput = newEquities;
            }

            GUILayout.Label($"Bonds: {draftBonds:F0} ({draftWeights.GetNormalizedWeight(SovereignWealthAssetClass.Bonds) * 100f:F0}% of fund)", _labelStyle);
            UiPalette.DrawBar(draftWeights.GetNormalizedWeight(SovereignWealthAssetClass.Bonds), swfColor, 8f);
            GUI.enabled = ambientEnabled && draftExists;
            float newBonds = GUILayout.HorizontalSlider(draftBonds, MinPolicyDialLevel, MaxPolicyDialLevel, _sliderStyle, _sliderThumbStyle);
            GUI.enabled = ambientEnabled;
            if (draftExists)
            {
                _swfBondsWeightInput = newBonds;
            }

            GUILayout.Label($"Infrastructure: {draftInfrastructure:F0} ({draftWeights.GetNormalizedWeight(SovereignWealthAssetClass.Infrastructure) * 100f:F0}% of fund)", _labelStyle);
            UiPalette.DrawBar(draftWeights.GetNormalizedWeight(SovereignWealthAssetClass.Infrastructure), swfColor, 8f);
            GUI.enabled = ambientEnabled && draftExists;
            float newInfrastructure = GUILayout.HorizontalSlider(draftInfrastructure, MinPolicyDialLevel, MaxPolicyDialLevel, _sliderStyle, _sliderThumbStyle);
            GUI.enabled = ambientEnabled;
            if (draftExists)
            {
                _swfInfrastructureWeightInput = newInfrastructure;
            }

            GUILayout.Label($"Real Estate: {draftRealEstate:F0} ({draftWeights.GetNormalizedWeight(SovereignWealthAssetClass.RealEstate) * 100f:F0}% of fund)", _labelStyle);
            UiPalette.DrawBar(draftWeights.GetNormalizedWeight(SovereignWealthAssetClass.RealEstate), swfColor, 8f);
            GUI.enabled = ambientEnabled && draftExists;
            float newRealEstate = GUILayout.HorizontalSlider(draftRealEstate, MinPolicyDialLevel, MaxPolicyDialLevel, _sliderStyle, _sliderThumbStyle);
            GUI.enabled = ambientEnabled;
            if (draftExists)
            {
                _swfRealEstateWeightInput = newRealEstate;
            }
        }

        /// <summary>
        /// The player country's detailed spending portfolio (Phase 1: USA only - see CLAUDE.md's
        /// "Detailed Spending Portfolio"), grouped Mandatory / Discretionary, plus Interest on Debt
        /// as a read-only automatic line. Both groups now get a this-turn PERCENTAGE-change slider
        /// (SimulationManager.ApplySpendingLineChanges applies it to that line's own Amount) -
        /// Mandatory's range is narrower, reflecting the real political difficulty of entitlement
        /// reform, and a Mandatory change carries a distinctly higher approval-rating penalty per
        /// relative size than a Discretionary one (see MacroSystem.MandatorySpendingApprovalMultiplier).
        /// Master Sequence step 5e, Phase A: the old standalone Spending Policy tab is retired (folds
        /// into Tax/Spending, both just entry points into this same Budget Process screen) - reached
        /// exclusively via DrawBudgetProcessTab now.
        /// </summary>
        private void DrawSpendingPolicyContent()
        {
            DrawColoredLabel("Spending Policy", _headerStyle, UiPalette.GetAreaColor(UiPalette.SystemArea.Fiscal));
            GUILayout.Label("Each line's slider is a DRAFT percentage change of its OWN current amount, not a flat dollar delta - nothing happens until the annual budget bill is introduced and passes (see the Budget Process tab). Mandatory programs have a narrower range and hit approval harder per relative size - entitlement reform is politically costly.", _labelStyle);
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
