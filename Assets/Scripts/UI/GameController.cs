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
        /// Master Sequence step 5e, Phase A (tab/IA restructuring): the consolidated top-level tabs (7 originally, 6 since the Tax/Spending merge below),
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
            // Merged 2026-08-01, was two tabs (Tax and Spending). They were never two screens: both
            // dispatched to the exact same DrawBudgetProcessTab, differing only in which of ITS OWN
            // five sub-categories (Tax/Spending/Welfare/Infrastructure/SWF) they pre-selected on entry.
            // Two top-level tabs that open the same screen and then hand you the same sub-selector is a
            // duplicated entry point, not a navigation choice - and it implied Tax and Spending were
            // peers of Statistics/Politics when they are actually peers of Welfare and Infrastructure,
            // which were already sub-categories here.
            Budget,
            PolicyLaws,
            Politics
        }


        /// <summary>
        /// Statistics tab's two sub-categories, restructured 2026-08-01 from the previous
        /// RecentTurns/WorldMap/Trade split.
        ///
        /// "Recent Turns" was a name inherited from the turn-based era and no longer describes anything
        /// the player sees under continuous time. "Domestic" says what the content actually is. Trade
        /// stopped being a peer sub-tab and folded into International, because trade IS international
        /// relations - it was only ever a sibling for historical reasons, not conceptual ones.
        ///
        /// All statistics now live in this tab, numbers and graphs together. The left column keeps
        /// headline numbers only.
        /// </summary>
        private enum StatisticsCategory { Domestic, International }

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

        /// <summary>
        /// Bounds for the EMERGENCY drawdown bill's slider — a one-off withdrawal, not the standing
        /// contribution rate above. The ceiling is deliberately higher than `MaxSwfContributionRate`'s
        /// 10%: that one is an annual flow that repeats, this is a single crisis transfer, and capping an
        /// emergency at the same rate as routine policy would defeat the purpose of having an emergency
        /// path at all. The floor is zero because a negative withdrawal is a contribution, which already
        /// has its own control — a slider that silently changes meaning at zero is how sign errors reach
        /// production.
        /// </summary>
        private const float MinSwfDrawdownPercentOfGdp = 0f;
        private const float MaxSwfDrawdownPercentOfGdp = 25f;

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

        /// <summary>The emergency drawdown slider's draft, in % of GDP. Not cleared by ResetPolicyInputs, matching every other draft here — a player who dialled in a number and then advanced a day should not find it reset.</summary>
        private float _swfDrawdownPercentInput;

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
                // Deliberately NOT "Full Turn (121 days)". That label alone was ~180px wide, and with the
                // other three on the same row it set a ~400px hard minimum for a column that can be as
                // narrow as 199px - the buttons then drew straight past the column edge (IMGUI does not
                // clip children to a fixed-width group) and off the viewport, which is what clipped the
                // preview panel's own headings. No information is lost: the day count is stated in full in
                // the sentence directly beneath these buttons.
                case PreviewHorizon.FullTurn: return "Full Turn";
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

        /// <summary>Master Sequence step 9, Step B: the PUBLISHED GDP series, drawn directly beneath the live one so the reporting lag and any revision are legible by comparison rather than in isolation. GDP is the right stat to show this on - it is the only one with a real multi-stage revision cycle (BEA advance/second/third, Eurostat flash/regular), so it is where a revision can actually be watched happening.</summary>
        private readonly GraphRenderer _gdpPublishedGraph = new GraphRenderer();
        private readonly GraphRenderer _unemploymentGraph = new GraphRenderer();
        private readonly GraphRenderer _approvalGraph = new GraphRenderer();

        // Restructure 2026-08-01: all graphs moved out of the left column into the Statistics tab, which
        // has the width and height they need. These are the stats that previously had no graph at all
        // because the strip could only fit three.
        private readonly GraphRenderer _inflationGraph = new GraphRenderer();
        private readonly GraphRenderer _povertyGraph = new GraphRenderer();
        private readonly GraphRenderer _debtGraph = new GraphRenderer();
        private readonly GraphRenderer _unemploymentPublishedGraph = new GraphRenderer();

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
        private StatisticsCategory _statisticsCategory = StatisticsCategory.Domestic;
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
        private GUIStyle _cardKindStyle;

        // Master Sequence step 5e, Phase C batch 2: decision-card chrome. Fill sits slightly lighter
        // than the app background so a card reads as raised without a border, matching PoliSimTheme's
        // own Card token rather than inventing a second card color for this one screen.
        // v2.0: a card is a sheet of paper, not a dark panel. Tinting the paper sprite by the old
        // near-black fill would have multiplied it down to mud - the sprite carries its own colour now, so
        // this is white, i.e. "leave the paper as authored".
        private static readonly Color AreaCardFill = Color.white;
        private const int AreaCardCornerRadius = 9;
        private const int AreaCardPadding = 12;
        private const int AreaCardSpineWidth = 8;

        // Full-scale deflection for a pending bill's lean bar. Seat-weighted alignment is bounded by the
        // strongest party stance (~0.7) but sits far lower in practice - the documented tied-parties
        // case lands near -0.036 - so scaling to the theoretical maximum would render every real bill as
        // a flat line. Presentation only: nothing is derived from this value, and no number is printed
        // from it (see DrawPendingBillCard).
        private const float PendingBillLeanDisplayRange = 0.15f;

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
                _simulationManager.AdvanceSwfDrawdownBillDay(PlayerCountryId);

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
            DrawMenuBackground();

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

                // The national flag, laid into the button's own left gutter. These sprites were delivered
                // and imported weeks ago and had never once been drawn - see IconLibrary.GetFlag for why
                // nothing ever failed. Drawn AFTER the button so it sits on top, and read from
                // GetLastRect so it tracks whatever height RescaleStylesToScreen just gave the control.
                //
                // Not tinted: unlike the chrome pack and the area icons, a flag is authored in its own
                // colours. Null-safe by IconLibrary's standing contract, so a missing file degrades to the
                // plain coloured plate this screen has always shown.
                //
                // Placed in the gutter rather than reserved in the style, and that IS the weaker of the
                // two options this project already knows about (see UiPalette.BuildCardStyle's note on art
                // laid over a control that does not know it is there). It is safe HERE specifically: the
                // button is 40% of the window wide with a short centred label, so the gutter is empty at
                // every size the game runs at. It would not be safe on a narrow control.
                Texture2D flag = IconLibrary.GetFlag(country.Id);
                if (flag != null)
                {
                    Rect buttonRect = GUILayoutUtility.GetLastRect();
                    float flagHeight = buttonRect.height * 0.5f;
                    float flagWidth = flagHeight * 1.5f;
                    GUI.DrawTexture(
                        new Rect(buttonRect.x + flagHeight * 0.5f, buttonRect.y + (buttonRect.height - flagHeight) * 0.5f, flagWidth, flagHeight),
                        flag, ScaleMode.ScaleToFit);
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
        /// The country-selector's background: a flat wash, then `menu_pattern_tile` repeated over it.
        ///
        /// Drawn in the order the asset pack's own README specifies - wash underneath, tile on top, one
        /// tile per 256px of screen via `DrawTextureWithTexCoords` rather than a stretched copy, which is
        /// the whole reason the texture is authored seamless and imported with Wrap Mode Repeat. Stretching
        /// it would blur the lattice into a smear at any window size but one.
        ///
        /// The tile is white-on-transparent at very low alpha (the sampled values are 0, 6 and 21 out of
        /// 255), so it reads as texture on the wash rather than as a pattern in its own right. That is
        /// deliberate on the artist's part and is why it must not be tinted or brightened here.
        ///
        /// **The wash is drawn whether or not the texture loads.** `IconLibrary` returns null for a
        /// missing sprite, and this screen previously drew no background at all - so a failed import
        /// degrades to a flat dark panel, which is a fine screen, instead of taking the wash down with it.
        /// </summary>
        private void DrawMenuBackground()
        {
            var screen = new Rect(0f, 0f, Screen.width, Screen.height);
            GUI.DrawTexture(screen, Texture2D.whiteTexture, ScaleMode.StretchToFill, false, 0f,
                PoliSimTheme.AppBackground, Vector4.zero, Vector4.zero);

            Texture2D tile = IconLibrary.GetTexture("menu_pattern_tile");
            if (tile == null)
            {
                return;
            }

            GUI.DrawTextureWithTexCoords(screen, tile,
                new Rect(0f, 0f, screen.width / tile.width, screen.height / tile.height));
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
            // v2.0: the desk. Everything the UI draws is furniture standing on it, so this is the one
            // surface that stayed dark when the theme inverted.
            camera.backgroundColor = PoliSimTheme.Desk;
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
            float sectionSpacing = areaHeight * SectionSpacingFraction;

            // Elias's directive (2026-08-01): the Budget tab takes the WHOLE window rather than the right
            // column, so its three columns (category / line-items / live estimate) get roughly double the
            // width and the whole budget is visible at once. This is standing behaviour for that tab, not
            // a toggle. It also retires the width pressure that screen had been fighting - at 1227x690 the
            // row goes from ~624px to ~1180px, comfortably more than everything that was overflowing.
            //
            // Elias chose to hide the left column entirely, including the calendar/speed controls, having
            // been shown the tradeoff. That strip is normally pinned outside the scroll view on every tab
            // precisely so a player can always see WHY time is blocked (working discipline item 2, which
            // exists because of a real "time silently stopped and I couldn't tell why" bug). To keep that
            // guarantee without giving the space back, DrawBudgetProcessTab re-surfaces any pending
            // interrupt as its own banner - see DrawFullScreenPendingInterruptBanner.
            bool budgetFullScreen = _consolidatedTab == ConsolidatedTab.Budget;
            float leftColumnWidth = budgetFullScreen ? 0f : areaWidth * LeftColumnWidthFraction;
            float rightColumnWidth = budgetFullScreen ? areaWidth : areaWidth - leftColumnWidth - columnSpacing;

            GUILayout.BeginArea(new Rect(marginX, marginY, areaWidth, areaHeight));
            GUILayout.BeginHorizontal();

            if (!budgetFullScreen)
            {
            // v2.0: the left column stands on paper like every other panel. Without a sheet under it its
            // banner, dashboard headings and preview text were ink on bare desk - present, and unreadable.
            GUILayout.BeginVertical(_boxStyle, GUILayout.Width(leftColumnWidth));

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
            }

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
                case ConsolidatedTab.Budget:
                    GUI.enabled = !_isGameOver;
                    DrawBudgetProcessTab(tabContentHeight, rightColumnWidth);
                    GUI.enabled = true;
                    break;
                case ConsolidatedTab.PolicyLaws:
                    DrawPolicyLawsTab(tabContentHeight, rightColumnWidth);
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
            _cardKindStyle = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold, wordWrap = false };

            // v2.0 typography (Elias's direction, 2026-08-03): ONE humanist serif across headers and body,
            // with the monospace reserved for genuine document artifacts and deliberately absent here -
            // nothing on these screens is a printed instrument. The display face differs from the body
            // face only in weight, so the distinction stays a matter of size and weight rather than of two
            // competing families. PoliSimWidgets applies the same faces to its own styles; see
            // PoliSimTheme for why the fonts are owned there rather than in this class.
            PoliSimTheme.WithDisplay(_headerStyle);
            PoliSimTheme.WithDisplay(_tabButtonStyle);
            PoliSimTheme.WithDisplay(_eventBannerStyle);
            PoliSimTheme.WithDisplay(_gameOverStyle);
            PoliSimTheme.WithDisplay(_cardKindStyle);
            PoliSimTheme.WithDisplay(_buttonStyle);

            PoliSimTheme.WithBody(_labelStyle);
            PoliSimTheme.WithBody(_boxStyle);

            // v2.0: ink on paper. Every style above derives from GUI.skin, whose text colour was authored
            // for a dark ground and is now unreadable, so each one is re-inked here rather than left to
            // whatever the skin happened to carry.
            foreach (GUIStyle inked in new[] { _headerStyle, _labelStyle, _boxStyle, _tabButtonStyle, _cardKindStyle })
            {
                inked.normal.textColor = PoliSimTheme.TextPrimary;
                inked.hover.textColor = PoliSimTheme.TextPrimary;
                inked.active.textColor = PoliSimTheme.TextPrimary;
                inked.focused.textColor = PoliSimTheme.TextPrimary;
            }

            // The two banners keep their own emphatic inks rather than the ramp - they exist to be
            // noticed. Amber for a pending interrupt, brick for game over.
            _eventBannerStyle.normal.textColor = PoliSimTheme.Draft;
            _gameOverStyle.normal.textColor = PoliSimTheme.Bad;

            StyleScrollbars();
            StyleSliders();
            StyleBoxAsPaper(_boxStyle);

            _stylesInitialized = true;
        }

        /// <summary>
        /// v2.0: `_boxStyle` is the container every tab wraps its content in, and it inherited
        /// `GUI.skin.box` — which on a dark ground was a faint panel and on the desk is nothing at all.
        ///
        /// **This is the other half of the theme inversion, and leaving it out was visible immediately:**
        /// the ink ramp had already been applied, so every label inside these containers was rendering
        /// dark-on-dark and had effectively vanished. Ink needs paper under it; the two changes are one
        /// change and cannot ship apart.
        /// </summary>
        private static void StyleBoxAsPaper(GUIStyle box)
        {
            Texture2D paper = IconLibrary.GetChrome("ui_panel_paper");
            if (paper == null)
            {
                return;
            }

            box.normal.background = paper;
            box.border = new RectOffset(22, 22, 22, 28);
            box.padding = new RectOffset(14, 14, 12, 14);
        }

        /// <summary>
        /// v2.0: the scrollbars, restyled globally on `GUI.skin` rather than per call site.
        ///
        /// **There are 16 scroll views in this game** — the left column, all six tabs, both Budget
        /// columns, every Policy/Laws sub-screen — and IMGUI takes their appearance from the skin, not
        /// from any argument a caller passes. One place is therefore both the cheapest fix and the only
        /// consistent one. Left unstyled they render Unity's grey against baked paper, three at once on
        /// the Budget screen; Design called that the most visible way the illusion breaks.
        ///
        /// ⚠ **THE ARROW BUTTONS TAKE TWO CHANGES, NOT ONE.** Design's call was that a ledger has no arrow
        /// furniture, so they are styled to a fully transparent 4×4 sprite — but pointing the style at a
        /// blank sprite is NOT enough on its own. IMGUI still reserves the button's fixed size, leaving a
        /// gap at each end of every track. `fixedWidth`/`fixedHeight` must be zeroed AND the margins
        /// cleared as well. Both, or the furniture is invisible and still occupying space.
        ///
        /// Null-safe throughout: a missing sprite leaves that piece of the skin as Unity supplied it.
        /// </summary>
        private static void StyleScrollbars()
        {
            ApplyScrollbarPiece(GUI.skin.verticalScrollbar, "ui_scrollbar_track_v", null, null, new RectOffset(4, 4, 7, 7));
            ApplyScrollbarPiece(GUI.skin.verticalScrollbarThumb, "ui_scrollbar_thumb_v", "ui_scrollbar_thumb_v_hover", "ui_scrollbar_thumb_v_pressed", new RectOffset(4, 4, 8, 8));
            ApplyScrollbarPiece(GUI.skin.horizontalScrollbar, "ui_scrollbar_track_h", null, null, new RectOffset(7, 7, 4, 4));
            ApplyScrollbarPiece(GUI.skin.horizontalScrollbarThumb, "ui_scrollbar_thumb_h", "ui_scrollbar_thumb_h_hover", "ui_scrollbar_thumb_h_pressed", new RectOffset(8, 8, 4, 4));

            foreach (GUIStyle arrow in new[]
            {
                GUI.skin.verticalScrollbarUpButton, GUI.skin.verticalScrollbarDownButton,
                GUI.skin.horizontalScrollbarLeftButton, GUI.skin.horizontalScrollbarRightButton
            })
            {
                Texture2D none = IconLibrary.GetChrome("ui_scrollbar_button_none");
                if (none != null)
                {
                    arrow.normal.background = none;
                    arrow.hover.background = none;
                    arrow.active.background = none;
                }

                arrow.fixedWidth = 0f;
                arrow.fixedHeight = 0f;
                arrow.margin = new RectOffset(0, 0, 0, 0);
                arrow.padding = new RectOffset(0, 0, 0, 0);
                arrow.border = new RectOffset(0, 0, 0, 0);
            }
        }

        /// <summary>
        /// v2.0: the policy sliders — 31 of them across the game, and the single control the player
        /// actually manipulates. Styled on `_sliderStyle`/`_sliderThumbStyle` rather than on `GUI.skin`
        /// because those two are what every call site passes.
        ///
        /// The knob is a fixed-size brass piece and must never stretch, so its border stays zero; the
        /// track is the only part that 9-slices. `ui_slider_tick` ships for the standing-value mark and is
        /// deliberately NOT wired here — it is drawn per row against a real value, which is call-site
        /// work rather than a style.
        /// </summary>
        /// <remarks>
        /// Instance, not static, unlike its two neighbours — those write to `GUI.skin` or to a style
        /// handed in, this one writes to `_sliderStyle`/`_sliderThumbStyle`, which are fields on the
        /// controller.
        /// </remarks>
        private void StyleSliders()
        {
            Texture2D track = IconLibrary.GetChrome("ui_slider_track");
            if (track != null)
            {
                _sliderStyle.normal.background = track;
                _sliderStyle.border = new RectOffset(5, 5, 2, 6);
            }

            Texture2D knob = IconLibrary.GetChrome("ui_slider_knob");
            Texture2D knobDisabled = IconLibrary.GetChrome("ui_slider_knob_disabled");
            if (knob != null)
            {
                _sliderThumbStyle.normal.background = knob;
                _sliderThumbStyle.hover.background = knob;
                _sliderThumbStyle.active.background = knob;
                _sliderThumbStyle.focused.background = knobDisabled ?? knob;
                _sliderThumbStyle.border = new RectOffset(0, 0, 0, 0);
            }
        }

        private static void ApplyScrollbarPiece(GUIStyle style, string normalName, string hoverName, string pressedName, RectOffset border)
        {
            Texture2D normal = IconLibrary.GetChrome(normalName);
            if (normal == null)
            {
                return;
            }

            style.normal.background = normal;
            style.hover.background = IconLibrary.GetChrome(hoverName) ?? normal;
            style.active.background = IconLibrary.GetChrome(pressedName) ?? normal;
            style.focused.background = normal;
            style.border = border;
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
            // A tab/category button WRAPS to two lines for the long labels ("Sovereign Wealth Fund"), and
            // its fixedHeight has to be able to hold both of them. It could not: the height came from
            // Screen.height alone, and the serif introduced in v2.0 has a taller line box than the default
            // sans it replaced, so the second line was cut off in the Budget category rail. Taking the max
            // against the style's own two-line height makes it font-aware rather than font-specific -
            // exactly the correction PolicyScreenStatsRenderer.LineHeightFor documents at more length.
            _tabButtonStyle.fixedHeight = Mathf.Max(tabButtonHeight, _tabButtonStyle.lineHeight * 2f + 8f);

            _eventBannerStyle.fontSize = bannerFontSize;
            _gameOverStyle.fontSize = bannerFontSize;
            // Deliberately the smallest text on screen - a card's kind caption is a wayfinding label,
            // not content, and must not compete with the decision's own headline underneath it.
            _cardKindStyle.fontSize = Mathf.Max(10, Mathf.RoundToInt(labelFontSize * 0.62f));

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
        /// Four of the 6 don't have one dominant existing SystemArea (Statistics/Decisions/Demographics/
        /// PolicyLaws each aggregate multiple old areas) - picked for visual DISTINCTNESS across all 6
        /// tab buttons (the actual property "reuse the existing tab-bar mechanics" needs to preserve),
        /// not because any one is a uniquely "correct" fit: Statistics keeps Global (its dominant piece,
        /// World Map, already used it), Politics keeps Political (strong precedent - Parliament/Cabinet/
        /// FedReserve all already used it), Budget keeps Fiscal unchanged. Decisions and Policy/Laws get
        /// two more distinct existing hues (CrimeJustice, Sectors) that aren't already claimed above.
        ///
        /// **Said "7" until 2026-08-03**, and had been wrong since the 2026-08-01 Tax+Spending merge:
        /// those two tabs became the single Budget tab (see ConsolidatedTab's own comment) and nothing
        /// updated the counts here. Found by the v2.0 UI survey, which needed an exact screen inventory
        /// and got a stale one from the code. Recorded rather than silently fixed, because it is the
        /// same shape as working-discipline rule 12: **a comment carrying a COUNT ages badly, since the
        /// change that invalidates it is never in the same place as the comment.**
        /// </summary>
        private static UiPalette.SystemArea GetConsolidatedTabArea(ConsolidatedTab tab)
        {
            switch (tab)
            {
                case ConsolidatedTab.Statistics: return UiPalette.SystemArea.Global;
                case ConsolidatedTab.Decisions: return UiPalette.SystemArea.CrimeJustice;
                case ConsolidatedTab.Demographics: return UiPalette.SystemArea.Labor;
                case ConsolidatedTab.Budget: return UiPalette.SystemArea.Fiscal;
                case ConsolidatedTab.PolicyLaws: return UiPalette.SystemArea.Sectors;
                case ConsolidatedTab.Politics: return UiPalette.SystemArea.Political;
                default: return UiPalette.SystemArea.Neutral;
            }
        }

        /// <summary>
        /// Master Sequence step 9, Step B2: which SystemArea a Policy/Laws SUB-SCREEN actually belongs
        /// to, for the contextual stat row.
        ///
        /// **Deliberately not <see cref="GetConsolidatedTabArea"/>.** That method's own doc says it
        /// picks hues for visual DISTINCTNESS across the tab bar, not correctness - it answers
        /// PolicyLaws with Sectors purely because Sectors was an unclaimed colour. Driving a stat row
        /// off it would put sector stats (PotentialGrowth, Unemployment) on the Labor and Crime &amp;
        /// Justice screens, which is worse than showing nothing: a confidently-wrong number is harder
        /// to disbelieve than an absent one. At sub-screen granularity the mapping is exact, because
        /// each of these screens already declares its own area for its bill card - Labor Market draws
        /// "LABOR MARKET BILL" as SystemArea.Labor, and so on. This reads the same truth those cards do.
        ///
        /// Policy Web returns Neutral on purpose: it IS the whole edge list this row is derived from,
        /// so a four-stat summary above it would be a worse view of the same data.
        /// </summary>
        private static UiPalette.SystemArea GetPolicyScreenArea(PolicyLawsCategory category)
        {
            switch (category)
            {
                case PolicyLawsCategory.LaborMarket: return UiPalette.SystemArea.Labor;
                case PolicyLawsCategory.CrimeJustice: return UiPalette.SystemArea.CrimeJustice;
                case PolicyLawsCategory.Sectors: return UiPalette.SystemArea.Sectors;
                case PolicyLawsCategory.Trade: return UiPalette.SystemArea.Trade;
                default: return UiPalette.SystemArea.Neutral;
            }
        }

        /// <summary>
        /// The same exact mapping for the Budget Process sub-screens. Tax and Spending both answer
        /// Fiscal, which is correct rather than a shortcut: every TaxType and every spending line runs
        /// through the same two Policy Web channels (approval on a hike, and revenue/outlay feeding
        /// DebtToGdp), so they genuinely move the same stats and their bill card is one shared
        /// SystemArea.Fiscal card.
        ///
        /// Infrastructure answers Infrastructure and will draw nothing, because no Infrastructure
        /// policy node has a Policy Web edge yet. That is the honest result of a real gap in the edge
        /// list - the row appears by itself the day one is added, with no change here.
        /// </summary>
        private static UiPalette.SystemArea GetPolicyScreenArea(BudgetProcessCategory category)
        {
            switch (category)
            {
                case BudgetProcessCategory.Tax: return UiPalette.SystemArea.Fiscal;
                case BudgetProcessCategory.Spending: return UiPalette.SystemArea.Fiscal;
                case BudgetProcessCategory.Welfare: return UiPalette.SystemArea.Welfare;
                case BudgetProcessCategory.Infrastructure: return UiPalette.SystemArea.Infrastructure;
                case BudgetProcessCategory.Swf: return UiPalette.SystemArea.SovereignWealth;
                default: return UiPalette.SystemArea.Neutral;
            }
        }

        /// <summary>
        /// Master Sequence step 5e, Phase C batch 6: a policy label that turns amber the moment its draft
        /// diverges from the standing value. This is the one genuinely load-bearing idea taken from
        /// `PoliSimWidgets.StandingDraftPair`, whose own documentation calls it "the only cue that a
        /// change is pending a vote - so it is never optional"; the widget itself was rejected (see the
        /// roadmap) because its hardcoded pixel offsets ignore rect.width and would break the
        /// variable-width Budget Process columns. The signal survives, the fragile geometry doesn't.
        ///
        /// White is not a hardcoded "normal" colour here - DrawColoredLabel MULTIPLIES GUI.color by the
        /// style's own text colour, so white means "leave the style alone" and this stays correct if the
        /// label palette ever changes.
        ///
        /// Applied to every draft-bearing control in one pass on purpose: a missing amber cue reads as
        /// "nothing changed here", so covering only some screens would actively mislead on the rest.
        /// </summary>
        private void DrawDraftLabel(string text, bool changed)
        {
            // v2.0: the unchanged case was `Color.white`, which was the ink ramp's own colour on a dark
            // ground and is near-invisible on paper. Amber still means drafted-not-enacted (B1) and is
            // untouched; only the "no change" colour moved, from white to ink.
            DrawColoredLabel(text, _labelStyle, changed ? PoliSimTheme.Draft : PoliSimTheme.TextPrimary);
        }

        /// <summary>Overload for the common "Standing: X, Draft: Y" pair, so each call site states the two values it compares instead of hand-rolling an approximate-equality test 20 times over.</summary>
        private void DrawDraftLabel(string text, float standing, float draft)
        {
            DrawDraftLabel(text, !Mathf.Approximately(standing, draft));
        }

        /// <summary>One-off tinted label (GUI.color multiplies the style's own text color, restored immediately after) - used for every signed-delta readout in the UI so its color always reflects UiPalette.GetDeltaColor rather than a hand-picked one-time color.</summary>
        /// <summary>
        /// ⚠ **v2.0: THIS NOW SETS THE COLOUR RATHER THAN MULTIPLYING BY IT, and the change was forced.**
        ///
        /// It used to set `GUI.color`, which MULTIPLIES the style's own text colour. That worked while the
        /// ramp was near-white — white × hue = hue. The theme is ink on paper now, so the ramp is
        /// `#2B2620`, and multiplying near-black by a hue produces near-black: every coloured header in
        /// the game would have rendered as an indistinguishable dark smudge.
        ///
        /// Setting the style's `textColor` for the duration of the call keeps the FUNCTION the callers
        /// depend on — "render this label in this colour", which is how the amber draft cue and every area
        /// header work — while making the colour absolute, which is what a paper ground requires. Restored
        /// immediately, because these styles are shared across the whole frame.
        /// </summary>
        private static void DrawColoredLabel(string text, GUIStyle style, Color color)
        {
            Color previous = style.normal.textColor;
            style.normal.textColor = color;
            GUILayout.Label(text, style);
            style.normal.textColor = previous;
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

            // Graphs deliberately absent (2026-08-01). The left column is numbers only now - headline
            // tiles, the policy preview, and the calendar/speed controls. Every graph lives in the
            // Statistics tab, where a stat and its history can sit side by side at a readable size
            // instead of being split across two parts of the screen at strip height.

            GUILayout.EndVertical();
        }

        /// <summary>
        /// Master Sequence step 5e, Phase B pilot: the dashboard's headline stats restyled onto
        /// <see cref="PoliSimWidgets.StatTile"/> in a 3-column grid, replacing the old raw
        /// GUILayout.Label two-column list - this is Phase B's actual sprite-pilot target (see
        /// POLISIM_MASTER_ROADMAP.md), not the Statistics tab's own content, since this is the one
        /// surface visible on every tab. Ten tiles now (nine without an independent currency): Step
        /// C4's Credit Rating joined the grid 2026-08-02, beside Debt-to-GDP.
        ///
        /// **Only two tiles can show a delta pill, and for different reasons.** GDP has a real
        /// turn-over-turn delta (_lastGrowthPercent, tracked via _prevGdp). Credit Rating shows its
        /// OUTLOOK, which is not a delta at all but a forward signal, and only when that signal is
        /// Positive or Negative. Every other tile gets no pill rather than a fabricated one, since no
        /// comparable prior-turn value is tracked for them.
        /// DrawHeadlineGraphs (the procedural line graphs) is untouched by this pass - rule 10's own
        /// carve-out keeps every data visualization procedural; only the icon/portrait/background
        /// layer moves to sprite art.
        /// </summary>
        private void DrawHeadlineStatTiles(EconomyState state, bool hasIndependentCurrency)
        {
            // Screen-derived, not a fixed 1.0. Hardcoding 1.0 meant the tile's own type sizes stayed
            // constant (FontStatHero is 42px) while the tiles themselves narrowed with the window - so
            // below roughly 1080p the headline figures no longer fit, which is what let a wrapped value
            // render as a fragment (see PoliSimWidgets.StatTile). Every other control in this class
            // already derives its size from Screen.height; this one was the exception.
            float scale = Mathf.Clamp(Screen.height / 1080f, 0.6f, 1.5f);
            const int columns = 3;
            float tileHeight = 92f * scale;
            float gap = 8f * scale;

            var tiles = new List<(string label, string value, string suffix, string delta, bool deltaIsGood, UiPalette.SystemArea area)>
            {
                // The GDP tile is where this bug has now appeared three times - "9,3", then "29k". The
                // suffix column stays null because the amount now carries its own ("$29.0T"), and a
                // tile suffix would render "$29.0T B".
                //
                // States MoneyUnit.Billions directly rather than reading the stat metadata, because this
                // reads the FIELD rather than a StatNodeId: billions is a fact about EconomyState.GDP,
                // and GetStatUnit(...).Value would throw inside OnGUI if that entry were ever cleared -
                // the sparkline crash is what an exception in a draw call costs. Generic code that
                // formats an arbitrary stat still asks the metadata, which is what it is for.
                ("GDP", UiFormat.Money(state.GDP, MoneyUnit.Billions), null, _lastGrowthPercent.ToString("+0.00;-0.00;0") + "%", _lastGrowthPercent >= 0f, UiPalette.SystemArea.Global),
                ("Unemployment", state.Unemployment.ToString("F2"), "%", null, false, UiPalette.SystemArea.Labor),
                ("Inflation", state.Inflation.ToString("F2"), "%", null, false, UiPalette.SystemArea.Fiscal),
                ("Approval Rating", state.ApprovalRating.ToString("F1"), null, null, false, UiPalette.SystemArea.Political),
            };

            if (hasIndependentCurrency)
            {
                tiles.Add(("Currency Strength", state.CurrencyStrength.ToString("F1"), null, null, false, UiPalette.SystemArea.Trade));
            }

            tiles.Add(("Poverty Rate", state.PovertyRate.ToString("F1"), "%", null, false, UiPalette.SystemArea.Welfare));
            tiles.Add(("Government Debt", UiFormat.Money(state.GovernmentDebt, MoneyUnit.Billions), null, null, false, UiPalette.SystemArea.Fiscal));
            tiles.Add(("Debt-to-GDP", state.DebtToGdpRatio.ToString("F1"), "%", null, false, UiPalette.SystemArea.Fiscal));

            // Step C4, placed 2026-08-02 (PROVISIONAL - see roadmap; revisable after visual review).
            // Directly after Debt-to-GDP on purpose: a sovereign rating is a judgment ABOUT the fiscal
            // position, not an independent variable, so it belongs beside the number it is mostly a
            // judgment about.
            //
            // Reads the STANDING rating rather than recomputing per frame. Per Elias's A1 ruling
            // (2026-08-02) the rating is set by scheduled annual review and is unchanged between
            // reviews - that is the design, not a staleness bug, and recomputing here would reintroduce
            // exactly the per-turn thrash the review cadence exists to remove.
            SovereignRatingState rating = _playerCountry.Rating;
            // Only a Positive or Negative outlook gets a pill. StatTile's pill is binary - good or bad -
            // and "Stable" is genuinely neither, so colouring it either way would assert something the
            // model does not claim. An absent pill is already this grid's norm (seven of the other tiles
            // show none), so absence reads as "nothing to telegraph", which is precisely what Stable
            // means. The outlook exists to warn of a MOVE; no move, no warning.
            bool hasOutlookSignal = rating.HasBeenReviewed && rating.Outlook != RatingOutlook.Stable;
            tiles.Add((
                "Credit Rating",
                // Em dash until the first review runs. An unrated sovereign is not a top-rated one, and
                // defaulting to AAA would be the confident-wrong-number failure this project keeps
                // finding. In practice the first review runs on day one, so this is a guard rather than
                // a state the player normally sees.
                rating.HasBeenReviewed ? CreditRatingSystem.Format(rating.Rating) : "-",
                null,
                hasOutlookSignal ? (rating.Outlook == RatingOutlook.Positive ? "OUTLOOK +" : "OUTLOOK -") : null,
                rating.Outlook == RatingOutlook.Positive,
                UiPalette.SystemArea.Fiscal));

            // Signed on purpose: a budget balance's direction is the whole reading, and "+$120B" against
            // "-$120B" should not depend on the player noticing a minus sign in a headline-size figure.
            tiles.Add(("Budget Balance", UiFormat.MoneyDelta(state.Budget, MoneyUnit.Billions), null, null, false, UiPalette.SystemArea.Fiscal));

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
            _interestRateGraph.DrawNeutral("Interest Rate", _playerCountry.History.InterestRate.Quarterly, null, _labelStyle, moneyUnit: null);

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
            GUILayout.BeginHorizontal();
            DrawPersonPortrait(IconLibrary.GetFedChairPortrait(candidate.Name), UiPalette.SystemArea.Political);

            GUILayout.BeginVertical();
            GUILayout.Label($"{candidate.Name} ({candidate.Philosophy})", _labelStyle);
            GUILayout.Label(candidate.Description, _labelStyle);
            if (GUILayout.Button($"Appoint {candidate.Name}", _neutralActionButtonStyle))
            {
                _playerCountry.CurrentFedChair = candidate;
                _fedChairCandidates = null;
                RecomputePolicyPreview();
            }
            GUILayout.EndVertical();

            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
        }

        /// <summary>
        /// Master Sequence step 5e, Phase C batch 3: draws one person's imported portrait art, reserving
        /// its space through GUILayoutUtility.GetRect so surrounding GUILayout content flows around it
        /// rather than being drawn over (the same discipline the tab-bar icons had to learn - see
        /// DrawConsolidatedTabButton). Falls back to `PoliSimWidgets.Portrait`'s procedural silhouette
        /// when <paramref name="portrait"/> is null, which is what happens for any name added to the
        /// CabinetSystem/FederalReserveSystem pools later without matching art - a generic placeholder
        /// is honest, whereas reusing some other candidate's face would actively misinform.
        /// Sized off the current label font rather than a fixed pixel count so it tracks the same
        /// screen-derived scale as everything around it.
        /// </summary>
        private void DrawPersonPortrait(Texture2D portrait, UiPalette.SystemArea area)
        {
            float size = _labelStyle.fontSize * 3.2f;
            Rect rect = GUILayoutUtility.GetRect(size, size, GUILayout.Width(size), GUILayout.Height(size));

            if (Event.current.type != EventType.Repaint)
            {
                return;
            }

            if (portrait != null)
            {
                GUI.DrawTexture(rect, portrait, ScaleMode.ScaleAndCrop, true);
            }
            else
            {
                PoliSimWidgets.Portrait(rect, area, 1f);
            }
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

            BeginAreaCard("CRIME & JUSTICE BILL", UiPalette.SystemArea.CrimeJustice);
            DrawCrimeJusticeBillStatusAndIntroduce();
            DrawCrimeJusticeLiveEstimate();
            EndAreaCard(UiPalette.SystemArea.CrimeJustice);

            float draftPoliceFunding = GetPoliceFundingInput(_playerCountry.PoliceFundingLevel);
            DrawDraftLabel($"Police Funding - Standing: {_playerCountry.PoliceFundingLevel:F0}, Draft: {draftPoliceFunding:F0}", _playerCountry.PoliceFundingLevel, draftPoliceFunding);
            _policeFundingInput = GUILayout.HorizontalSlider(draftPoliceFunding, MinPolicyDialLevel, MaxPolicyDialLevel, _sliderStyle, _sliderThumbStyle);

            float draftSentencingSeverity = GetSentencingSeverityInput(_playerCountry.SentencingSeverity);
            DrawDraftLabel($"Sentencing Severity - Standing: {_playerCountry.SentencingSeverity:F0}, Draft: {draftSentencingSeverity:F0} (0 = lenient, 100 = harsh)", _playerCountry.SentencingSeverity, draftSentencingSeverity);
            _sentencingSeverityInput = GUILayout.HorizontalSlider(draftSentencingSeverity, MinPolicyDialLevel, MaxPolicyDialLevel, _sliderStyle, _sliderThumbStyle);

            float draftBailReform = GetBailReformInput(_playerCountry.BailReformLevel);
            DrawDraftLabel($"Bail Reform - Standing: {_playerCountry.BailReformLevel:F0}, Draft: {draftBailReform:F0} (0 = traditional cash bail, 100 = full reform)", _playerCountry.BailReformLevel, draftBailReform);
            _bailReformInput = GUILayout.HorizontalSlider(draftBailReform, MinPolicyDialLevel, MaxPolicyDialLevel, _sliderStyle, _sliderThumbStyle);

            float draftDrugPolicy = GetDrugPolicyInput(_playerCountry.DrugPolicyLevel);
            DrawDraftLabel($"Drug Policy - Standing: {_playerCountry.DrugPolicyLevel:F0}, Draft: {draftDrugPolicy:F0} (0 = decriminalized, 100 = strict criminalization)", _playerCountry.DrugPolicyLevel, draftDrugPolicy);
            _drugPolicyInput = GUILayout.HorizontalSlider(draftDrugPolicy, MinPolicyDialLevel, MaxPolicyDialLevel, _sliderStyle, _sliderThumbStyle);

            float draftJudicialFunding = GetJudicialFundingInput(_playerCountry.JudicialFundingLevel);
            DrawDraftLabel($"Judicial Funding - Standing: {_playerCountry.JudicialFundingLevel:F0}, Draft: {draftJudicialFunding:F0}", _playerCountry.JudicialFundingLevel, draftJudicialFunding);
            _judicialFundingInput = GUILayout.HorizontalSlider(draftJudicialFunding, MinPolicyDialLevel, MaxPolicyDialLevel, _sliderStyle, _sliderThumbStyle);

            float draftBorderEnforcement = GetBorderEnforcementInput(_playerCountry.BorderEnforcementLevel);
            DrawDraftLabel($"Border Enforcement - Standing: {_playerCountry.BorderEnforcementLevel:F0}, Draft: {draftBorderEnforcement:F0} (0 = open/lenient, 100 = strict)", _playerCountry.BorderEnforcementLevel, draftBorderEnforcement);
            _borderEnforcementInput = GUILayout.HorizontalSlider(draftBorderEnforcement, MinPolicyDialLevel, MaxPolicyDialLevel, _sliderStyle, _sliderThumbStyle);

            GUILayout.Space(10f);
            _crimeIndexGraph.Draw("Crime Index", _playerCountry.History.CrimeIndex.Quarterly, null, _labelStyle, higherIsBetter: false, moneyUnit: null);
            _organizedCrimeGraph.Draw("Organized Crime Index", _playerCountry.History.OrganizedCrimeIndex.Quarterly, null, _labelStyle, higherIsBetter: false, moneyUnit: null);
            _corruptionGraph.Draw("Corruption Index", _playerCountry.History.CorruptionIndex.Quarterly, null, _labelStyle, higherIsBetter: false, moneyUnit: null);
            _prisonPopulationGraph.DrawNeutral("Incarceration Rate per 100k", _playerCountry.History.PrisonPopulationRate.Quarterly, null, _labelStyle, moneyUnit: null);

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
            DrawBillLiveEstimate(ParliamentSystem.GetCrimeJusticeBillDirection(_playerCountry, BuildCrimeJusticeBillFromDrafts()));
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

            BeginAreaCard("LABOR MARKET BILL", UiPalette.SystemArea.Labor);
            DrawLaborBillStatusAndIntroduce();
            DrawLaborLiveEstimate();
            EndAreaCard(UiPalette.SystemArea.Labor);

            DrawMinimumWageControl();

            float draftPaidLeave = GetPaidFamilyLeaveWeeksInput(_playerCountry.PaidFamilyLeaveWeeks);
            DrawDraftLabel($"Paid Family Leave - Standing: {_playerCountry.PaidFamilyLeaveWeeks:F0}, Draft: {draftPaidLeave:F0} weeks", _playerCountry.PaidFamilyLeaveWeeks, draftPaidLeave);
            _paidFamilyLeaveWeeksInput = GUILayout.HorizontalSlider(draftPaidLeave, MinPaidFamilyLeaveWeeks, MaxPaidFamilyLeaveWeeks, _sliderStyle, _sliderThumbStyle);

            float draftOvertimeRegulation = GetOvertimeRegulationInput(_playerCountry.OvertimeRegulationLevel);
            DrawDraftLabel($"Overtime/Working-Hour Regulation - Standing: {_playerCountry.OvertimeRegulationLevel:F0}, Draft: {draftOvertimeRegulation:F0} (0 = unregulated, 100 = strict caps)", _playerCountry.OvertimeRegulationLevel, draftOvertimeRegulation);
            _overtimeRegulationInput = GUILayout.HorizontalSlider(draftOvertimeRegulation, MinLaborDialLevel, MaxLaborDialLevel, _sliderStyle, _sliderThumbStyle);

            float draftRetraining = GetRetrainingProgramInput(_playerCountry.RetrainingProgramLevel);
            DrawDraftLabel($"Workforce Retraining Programs - Standing: {_playerCountry.RetrainingProgramLevel:F0}, Draft: {draftRetraining:F0}", _playerCountry.RetrainingProgramLevel, draftRetraining);
            _retrainingProgramInput = GUILayout.HorizontalSlider(draftRetraining, MinLaborDialLevel, MaxLaborDialLevel, _sliderStyle, _sliderThumbStyle);

            GUILayout.Space(8f);
            float draftFamilyPolicy = GetFamilyPolicyInput(_playerCountry.FamilyPolicyLevel);
            DrawDraftLabel($"Family Policy - Standing: {_playerCountry.FamilyPolicyLevel:F0}, Draft: {draftFamilyPolicy:F0} (0 = minimal support, 100 = maximal pro-natalist support)", _playerCountry.FamilyPolicyLevel, draftFamilyPolicy);
            _familyPolicyInput = GUILayout.HorizontalSlider(draftFamilyPolicy, MinPolicyDialLevel, MaxPolicyDialLevel, _sliderStyle, _sliderThumbStyle);

            float draftImmigrationPolicy = GetImmigrationPolicyInput(_playerCountry.ImmigrationPolicyLevel);
            DrawDraftLabel($"Immigration Policy - Standing: {_playerCountry.ImmigrationPolicyLevel:F0}, Draft: {draftImmigrationPolicy:F0} (0 = maximally restrictive, 100 = maximally open)", _playerCountry.ImmigrationPolicyLevel, draftImmigrationPolicy);
            _immigrationPolicyInput = GUILayout.HorizontalSlider(draftImmigrationPolicy, MinPolicyDialLevel, MaxPolicyDialLevel, _sliderStyle, _sliderThumbStyle);

            GUILayout.Space(10f);
            _laborForceParticipationGraph.Draw("Labor Force Participation", _playerCountry.History.LaborForceParticipationRate.Quarterly, null, _labelStyle, higherIsBetter: true, moneyUnit: null);

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

        private void DrawLaborLiveEstimate()
        {
            DrawBillLiveEstimate(ParliamentSystem.GetLaborBillDirection(_playerCountry, BuildLaborBillFromDrafts()));
        }

        /// <summary>
        /// Master Sequence step 5e, Phase C batch 4: the shared live pass/fail estimate for every
        /// standalone bill tier. All four policy screens (Labor, Crime &amp; Justice, Sectors, Trade) had a
        /// byte-for-byte identical copy of this, differing only in which draft they built and which
        /// direction getter they called - so the only thing that ever varied is the float now passed in.
        ///
        /// Collapsing them matters for more than duplication: the zero-direction trap below has to be
        /// handled identically everywhere, and four copies is four chances to get it wrong (the Parliament
        /// card already shipped that exact bug once - see DrawPendingBillCard).
        ///
        /// The lean bar shows ParliamentSystem.GetSeatWeightedAlignment, the quantity the vote is really
        /// decided on, so a player can see HOW close a bill is rather than only which side of the line it
        /// currently sits. Deliberately not PoliSimWidgets.SupportBar - this model has no seats-based
        /// majority for it to draw (see DrawPendingBillCard's own comment for the full reasoning).
        /// </summary>
        private void DrawBillLiveEstimate(float direction)
        {
            // Unity's Mathf.Sign(0f) returns 1, not 0, so an unchanged draft would otherwise be scored as
            // parliament's raw net stance - negative in the documented tied-parties case - and contradict
            // the WOULD PASS verdict printed directly above it. WouldBillPass short-circuits on exactly
            // this condition, so the bar must too.
            bool contested = !Mathf.Approximately(direction, 0f);
            bool wouldPass = ParliamentSystem.WouldBillPass(_playerCountry, direction);

            string directionLabel = !contested ? "Neutral" : direction > 0f ? "Expansionary" : "Contractionary";
            GUILayout.Label($"Bill direction: {directionLabel} ({direction:+0.0;-0.0;0})", _labelStyle);
            DrawColoredLabel(wouldPass ? "Current seat composition: WOULD PASS" : "Current seat composition: WOULD FAIL",
                _labelStyle, UiPalette.GetDeltaColor(wouldPass ? 1f : -1f, higherIsBetter: true));

            Rect barRect = GUILayoutUtility.GetRect(10f, _labelStyle.fontSize * 0.7f, GUILayout.ExpandWidth(true));
            if (Event.current.type == EventType.Repaint)
            {
                UiPalette.DrawDivergingBar(barRect, contested ? ParliamentSystem.GetSeatWeightedAlignment(_playerCountry, direction) : 0f, PendingBillLeanDisplayRange);
            }
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
            DrawDraftLabel($"Minimum Wage - Standing: {_playerCountry.MinimumWagePercentOfMedian:F0}%, Draft: {draftMinimumWage:F0}% of median wage", _playerCountry.MinimumWagePercentOfMedian, draftMinimumWage);
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
            // Header and horizon buttons sit on SEPARATE rows. Previously they shared one row with a
            // FlexibleSpace between them, which set this panel's minimum width at roughly
            // "header + four buttons" (~864px measured) - far more than it gets inside the Budget
            // Process's three-column row, and the main reason that row overflowed into a horizontal
            // scrollbar. Stacking costs one row of height, which this panel has, and removes the
            // width floor entirely.
            GUILayout.Label("Estimated Effects", _headerStyle);
            // Two rows of two rather than one row of four. Even with the shortened "Full Turn" label, four
            // side by side still demand more than this column gets at small window sizes; 2x2 halves that
            // minimum. The buttons expand to share each row so the block stays tidy at any width.
            GUILayout.BeginHorizontal();
            DrawHorizonButton(PreviewHorizon.OneDay);
            DrawHorizonButton(PreviewHorizon.OneWeek);
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
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
            // ExpandWidth(true), not false: content-sized buttons let the longest label dictate the row's
            // width, which is how this block came to demand more than its column had. Sharing the row
            // makes each button half the column instead, whatever the label says.
            if (GUILayout.Button(GetHorizonLabel(horizon), style, GUILayout.ExpandWidth(true)))
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

            _cachedSwfContributionText = FormatMoneyEstimate(preview.SwfContributionEstimate, MoneyUnit.Billions);
            _cachedSwfReturnsText = FormatMoneyEstimate(preview.SwfReturnsEstimate, MoneyUnit.Billions);

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
            _cachedNetBudgetScaledText = FormatMoneyEstimate(_cachedNetBudgetImpactScaled, MoneyUnit.Billions);
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
        /// <see cref="FormatEstimate"/> for a currency figure. Same margin roll, same "value ± margin"
        /// shape, but both numbers render through <see cref="UiFormat.Money"/>.
        ///
        /// The three call sites for this previously passed the literal suffix " units" - the clearest
        /// single example of the P2 finding that this game states its units nowhere. A player reading
        /// "+120.00 units" had no way to learn that meant $120 billion.
        /// </summary>
        private string FormatMoneyEstimate(float value, MoneyUnit unit)
        {
            float marginPercent = MinPreviewMarginPercent + (float)_previewRandom.NextDouble() * (MaxPreviewMarginPercent - MinPreviewMarginPercent);
            float marginAmount = Mathf.Abs(value) * marginPercent / 100f;
            return $"{UiFormat.MoneyDelta(value, unit)} (±{UiFormat.Money(marginAmount, unit)})";
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
            _turnLog.Add($"Turn {_simulationManager.CurrentTurn}: GDP={UiFormat.Money(state.GDP, MoneyUnit.Billions)} ({_lastGrowthPercent:+0.00;-0.00;0}%), " +
                $"Unemp={state.Unemployment:F2}%, Infl={state.Inflation:F2}%, Approval={state.ApprovalRating:F1}, Debt/GDP={state.DebtToGdpRatio:F1}%");

            while (_turnLog.Count > MaxLogEntries)
            {
                _turnLog.RemoveAt(0);
            }
        }

        /// <summary>
        /// Master Sequence step 5e, Phase A: the 6 consolidated top-level tabs (7 before the Tax/Spending merge), all fitting in ONE row
        /// (short labels, unlike the old 18-tab bar's "Sovereign Wealth Fund"-length names) - replaces
        /// the old 6-per-row/5-row layout entirely. Each tab is tinted by its own SystemArea (see
        /// GetConsolidatedTabArea) - selected uses the bright TabSelected variant, unselected the
        /// dimmer Tab variant, same mechanic the old bar used, per Phase A's own "no visual style
        /// change, only navigation changes" constraint.
        /// </summary>
        private const int ConsolidatedTabsPerRow = 6;

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
            // Master Sequence step 5e, Phase C: all 6 tabs carry their icon. The four icon_nav_* ones
            // exist precisely because Statistics/Decisions/Demographics/Policy-Laws map to no single
            // UiPalette.SystemArea; Budget and Politics instead reuse the existing area icons directly,
            // exactly as the 5E asset manifest specified (see COMPLETED.md section 8) ("Tax/Spending/
            // Politics tabs reuse the existing icon_area_fiscal/icon_area_political icons directly - no
            // new art needed").
            //
            // **Said "7 tabs", and described a Tax/Spending icon SHARE, until 2026-08-03.** Both were
            // left behind by the 2026-08-01 merge that turned those two tabs into the single Budget tab -
            // there is no longer any sharing to flag, because there is only one tab. Same stale-count
            // failure as GetConsolidatedTabArea's own comment; see there for why it is worth naming.
            DrawConsolidatedTabButton("Statistics", ConsolidatedTab.Statistics, buttonWidth, "icon_nav_statistics");
            DrawConsolidatedTabButton("Decisions", ConsolidatedTab.Decisions, buttonWidth, "icon_nav_decisions");
            DrawConsolidatedTabButton("Demographics", ConsolidatedTab.Demographics, buttonWidth, "icon_nav_demographics");
            DrawConsolidatedTabButton("Budget", ConsolidatedTab.Budget, buttonWidth, "icon_area_fiscal");
            DrawConsolidatedTabButton("Policy/Laws", ConsolidatedTab.PolicyLaws, buttonWidth, "icon_nav_policylaws");
            DrawConsolidatedTabButton("Politics", ConsolidatedTab.Politics, buttonWidth, "icon_area_political");
            GUILayout.EndHorizontal();
        }

        /// <summary>
        /// Each tab is tinted by its own SystemArea (see GetConsolidatedTabArea) - selected uses the
        /// bright TabSelected variant, unselected the dimmer Tab variant, so the currently-open tab
        /// reads as visibly "lit up" in its own area's hue rather than just bold+yellow text. A click now
        /// does nothing except change which tab is selected - the Tax/Spending merge (see ConsolidatedTab)
        /// removed the one exception, which used to seed `_budgetProcessCategory` so those two entry
        /// points landed on different sub-categories of the same screen.
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
                Color iconTint = selected ? Color.white : UiPalette.MutedIconTint;
                UiPalette.DrawTintedIcon(iconRect, icon, iconTint);
            }

            if (clicked)
            {
                // Nothing to seed since Tax and Spending merged into one Budget tab: the Budget Process
                // screen's own category selector is now the only thing that sets _budgetProcessCategory,
                // so it keeps whatever the player last chose instead of a tab click silently resetting it.
                _consolidatedTab = tab;
            }
        }

        /// <summary>Generic sub-category tab button, shared by Statistics/Policy-Laws/Politics' own category rows - mirrors DrawBudgetProcessCategoryButton's exact established pattern (Primary when selected, Neutral otherwise - no per-area tinting at this second level, unlike the top-level tabs above).</summary>
        private void DrawSubCategoryButton<T>(string label, T category, ref T selectedCategory) where T : struct, System.Enum
        {
            bool selected = EqualityComparer<T>.Default.Equals(selectedCategory, category);
            GUIStyle style = BuildSubTabStyle(selected);

            // REVIEW ITEM 5 ("trade is cut off") WAS HERE, and it is the WIDTH variant of the label
            // class. Five buttons share the Policy/Laws row with ExpandWidth(true) and no width budget:
            // GUILayout divides the row evenly, so when the natural widths exceed the container the
            // longest labels lose their tails - and the last button, Trade, is where it shows.
            //
            // MinWidth is what ExpandWidth was missing. ExpandWidth says "take a share"; it never says
            // "and this much is the minimum I need". With a floor measured in the style the text actually
            // renders in, GUILayout gives each button at least its own content and the row wraps or
            // compresses evenly instead of silently truncating the end of it.
            float minWidth = PoliSimWidgets.MeasuredWidth(label, style, style.padding.horizontal + 6f);
            if (GUILayout.Button(label, style, GUILayout.ExpandWidth(true), GUILayout.MinWidth(minWidth),
                GUILayout.MinHeight(_tabButtonStyle.fixedHeight)))
            {
                selectedCategory = category;
            }
        }

        /// <summary>
        /// Shared style for every sub-tab button - the Statistics/Policy-Laws/Politics selectors and the
        /// Budget Process category column. Identical to the tab-bar style except that it CLEARS
        /// fixedHeight.
        ///
        /// That one property was the cause of the sub-tabs' unreadable labels: `_tabButtonStyle` sets
        /// wordWrap AND a fixed height, so a label too wide for its button wrapped to two or three lines
        /// and then had everything past the first clipped off by that fixed height. "Sovereign Wealth
        /// Fund" in the 94px-wide category column is the worst case. With the height free, a button grows
        /// to fit its own wrapped text; callers pass MinHeight so buttons never render SHORTER than the
        /// tab bar's, which keeps a row looking even when nothing needs to wrap.
        ///
        /// Callers also pass ExpandWidth(true) so buttons in a horizontal row share it evenly instead of
        /// the longest label dictating the row's width - the same overflow that clipped the Budget Process
        /// columns. A wrapping style's minimum width is its longest WORD rather than its whole label, so
        /// sharing genuinely fits rather than merely deferring the overflow.
        /// </summary>
        private GUIStyle BuildSubTabStyle(bool selected)
        {
            GUIStyle style = UiPalette.BuildButtonStyle(_tabButtonStyle, selected ? UiPalette.ButtonKind.Primary : UiPalette.ButtonKind.Neutral);
            style.fixedHeight = 0f;
            return style;
        }

        /// Statistics tab. RESTRUCTURED 2026-08-01 into two sub-tabs: Domestic (this country's own
        /// numbers AND graphs together) and International (world map, trade, and world-wide activity).
        /// Replaces the previous RecentTurns/WorldMap/Trade split - "Recent Turns" was a turn-based-era
        /// name that describes nothing under continuous time, and Trade was only ever a peer sub-tab for
        /// historical rather than conceptual reasons. All statistics live here now; the left column
        /// keeps headline numbers only.
        /// favors reusing existing rendering wholesale over extracting content-only pieces that don't
        /// already exist, even at the cost of a harmless nested box for those two categories.
        /// </summary>
        private void DrawStatisticsTab(float availableHeight)
        {
            GUILayout.BeginVertical(_boxStyle);
            DrawColoredLabel("Statistics", _headerStyle, UiPalette.GetAreaColor(UiPalette.SystemArea.Global));
            GUILayout.BeginHorizontal();
            DrawSubCategoryButton("Domestic", StatisticsCategory.Domestic, ref _statisticsCategory);
            DrawSubCategoryButton("International", StatisticsCategory.International, ref _statisticsCategory);
            GUILayout.EndHorizontal();
            GUILayout.Space(6f);

            float contentHeight = availableHeight - _headerStyle.fontSize - _tabButtonStyle.fixedHeight - 14f;
            float scrollHeight = contentHeight - _labelStyle.fontSize * 2f;
            _statisticsContentScrollPosition = GUILayout.BeginScrollView(_statisticsContentScrollPosition, GUILayout.Height(scrollHeight));
            switch (_statisticsCategory)
            {
                case StatisticsCategory.Domestic:
                    DrawDomesticStatisticsContent();
                    break;
                case StatisticsCategory.International:
                    DrawInternationalStatisticsContent(contentHeight);
                    break;
            }
            GUILayout.EndScrollView();
            GUILayout.EndVertical();
        }

        /// <summary>
        /// Domestic statistics: this country's own numbers AND their graphs, together. Restructured
        /// 2026-08-01 - the left column now carries headline numbers only, so a stat and its history are
        /// no longer split across two parts of the screen where they could not be read against each
        /// other.
        /// </summary>
        private void DrawDomesticStatisticsContent()
        {
            EconomyState state = _playerCountry.State;
            bool hasIndependentCurrency = !CurrencySystem.SharesCurrencyZoneWithOthers(_playerCountry, _world);

            DrawColoredLabel("Domestic", _headerStyle, UiPalette.GetAreaColor(UiPalette.SystemArea.Global));
            DrawHeadlineStatTiles(state, hasIndependentCurrency);
            GUILayout.Space(10f);
            DrawDerivedStatsRow();
            GUILayout.Space(10f);

            // Next-turn projections carried over from the old left-column graphs rather than dropped in
            // the move - the dashed segment is a real feature, and losing it silently would have been a
            // regression disguised as a relocation.
            float? projectedGdp = null;
            float? projectedUnemployment = null;
            float? projectedApproval = null;
            if (_hasCachedPreview)
            {
                projectedGdp = state.GDP * (1f + _cachedGdpGrowthPercentRaw / 100f);
                projectedUnemployment = state.Unemployment + _cachedUnemploymentChangeRaw;
                projectedApproval = state.ApprovalRating + _cachedApprovalChangeRaw;
            }

            StatHistory history = _playerCountry.History;
            // The unit comes from the stat's own metadata rather than a MoneyUnit literal here. A literal
            // would be a second place that knows GDP is in billions, which is how the P2 unit bug spread
            // across 21 sites in the first place.
            _gdpGraph.Draw("GDP (dashed = next-turn estimate)", history.Gdp.Quarterly, projectedGdp, _labelStyle, higherIsBetter: true,
                moneyUnit: PolicyWebRenderer.GetStatUnit(StatNodeId.Gdp));
            _unemploymentGraph.Draw("Unemployment (dashed = next-turn estimate)", history.Unemployment.Quarterly, projectedUnemployment, _labelStyle, higherIsBetter: false, moneyUnit: null,
                thresholdValue: _playerCountry.NaturalUnemploymentRate, thresholdLabel: "NAIRU");
            _inflationGraph.Draw("Inflation", history.Inflation.Quarterly, null, _labelStyle, higherIsBetter: false, moneyUnit: null);
            _approvalGraph.Draw("Approval Rating (dashed = next-turn estimate)", history.ApprovalRating.Quarterly, projectedApproval, _labelStyle, higherIsBetter: true, moneyUnit: null);
            _povertyGraph.Draw("Poverty Rate", history.PovertyRate.Quarterly, null, _labelStyle, higherIsBetter: false, moneyUnit: null);
            _debtGraph.Draw("Debt-to-GDP", history.DebtToGdpRatio.Quarterly, null, _labelStyle, higherIsBetter: false, moneyUnit: null,
                thresholdValue: _playerCountry.ComfortableDebtToGdpPercent, thresholdLabel: "comfortable");

            GUILayout.Space(12f);
            DrawColoredLabel("As published", _headerStyle, UiPalette.GetAreaColor(UiPalette.SystemArea.Global));
            GUILayout.Label("What the public sees: lagged, and revised as later estimates arrive. Compare against the live figures above.", _labelStyle);
            GUILayout.Space(4f);

            _gdpPublishedGraph.DrawPublished("GDP as published",
                _playerCountry.Published.Series.TryGetValue(PublishedStat.Gdp, out PublishedSeries gdpPublished) ? gdpPublished : null,
                _labelStyle, higherIsBetter: true, _simulationManager.CurrentDate, moneyUnit: PolicyWebRenderer.GetStatUnit(StatNodeId.Gdp));

            _unemploymentPublishedGraph.DrawPublished("Unemployment as published",
                _playerCountry.Published.Series.TryGetValue(PublishedStat.Unemployment, out PublishedSeries unemploymentPublished) ? unemploymentPublished : null,
                _labelStyle, higherIsBetter: false, _simulationManager.CurrentDate, moneyUnit: null);

            // The [DEBUG] publication-lag dump lived here from `dd7e323` until 2026-08-02. It printed the
            // live GDP figure beside every published entry's reference period, publication date, value and
            // revision status, so the graph above could be cross-checked against the data behind it.
            //
            // REMOVED because it did its job: review item 8 (revision treatment) passed, which is the
            // confirmation that the graph and the data agree - the exact question the dump existed to
            // answer. It was explicitly never to ship, and keeping a diagnostic past the confirmation it
            // was waiting for is how a temporary thing becomes permanent.
            //
            // If provenance ever needs re-checking, `PublicationSystem`'s own tests reach the same data
            // without a UI surface, which is the better place for it.
        }

        /// <summary>
        /// International statistics: the world map plus everything cross-country, now including Trade -
        /// which absorbed the old peer sub-tab because trade IS international relations, and was only
        /// ever a sibling for historical reasons. The turn log lives here too, since its content is
        /// world-wide rather than domestic.
        /// </summary>
        private void DrawInternationalStatisticsContent(float availableHeight)
        {
            DrawColoredLabel("International", _headerStyle, UiPalette.GetAreaColor(UiPalette.SystemArea.Global));
            DrawWorldMapContent();

            GUILayout.Space(10f);
            DrawTradeStatsContent();

            GUILayout.Space(10f);
            DrawColoredLabel("Recent activity", _headerStyle, UiPalette.GetAreaColor(UiPalette.SystemArea.Global));
            for (int i = _turnLog.Count - 1; i >= 0 && i > _turnLog.Count - 12; i--)
            {
                GUILayout.Label(_turnLog[i], _labelStyle);
            }
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
                BeginAreaCard("FEDERAL RESERVE", UiPalette.SystemArea.Political);
                DrawFedChairSelectionModal();
                EndAreaCard(UiPalette.SystemArea.Political);
                anyPending = true;
            }

            ForeignPolicyMeeting pendingMeeting = _simulationManager.GetPendingForeignPolicyMeeting(PlayerCountryId);
            if (pendingMeeting != null)
            {
                BeginAreaCard("FOREIGN POLICY", UiPalette.SystemArea.Global);
                DrawForeignPolicyMeetingModal(pendingMeeting, drawOwnFrame: false);
                EndAreaCard(UiPalette.SystemArea.Global);
                anyPending = true;
            }

            foreach ((CabinetPortfolio portfolio, CabinetDecision decision) in _simulationManager.GetPendingCabinetDecisions(PlayerCountryId))
            {
                // Tinted by the PORTFOLIO's own area, not one flat "cabinet" color - two simultaneous
                // cabinet decisions from different portfolios should not read as the same thing.
                UiPalette.SystemArea portfolioArea = UiPalette.GetPortfolioArea(portfolio);
                BeginAreaCard("CABINET", portfolioArea);
                DrawCabinetDecisionModal(portfolio, decision, drawOwnFrame: false);
                EndAreaCard(portfolioArea);
                anyPending = true;
            }

            if (_simulationManager.GetPendingBudgetProcess(PlayerCountryId))
            {
                BeginAreaCard("BUDGET PROCESS", UiPalette.SystemArea.Fiscal);
                DrawBudgetBillStatusAndIntroduce();
                EndAreaCard(UiPalette.SystemArea.Fiscal);
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

        /// <summary>
        /// Master Sequence step 5e, Phase C: opens a rounded card, optionally captioned with a small caps
        /// <paramref name="kind"/> label, spined in <paramref name="area"/>'s own color (see EndAreaCard,
        /// which draws the spine once the card's real height is known). Pass a null caption for a card
        /// whose content already names itself.
        ///
        /// Introduced in batch 2 for the Decisions tab, generalized in batch 4 (hence the rename from
        /// BeginDecisionCard) once Parliament's pending bills and the four Policy/Laws bill panels wanted
        /// the same chrome. It deliberately wraps EXISTING renderers rather than replacing them: several
        /// are shared across tabs belonging to different batches, so rewriting their internals would
        /// silently restyle a screen this batch was not supposed to touch.
        /// </summary>
        private void BeginAreaCard(string kind, UiPalette.SystemArea area)
        {
            GUILayout.BeginVertical(UiPalette.BuildCardStyle(AreaCardFill, AreaCardCornerRadius, AreaCardPadding, AreaCardSpineWidth));
            if (!string.IsNullOrEmpty(kind))
            {
                DrawColoredLabel(kind, _cardKindStyle, UiPalette.GetAreaColor(area));
            }
        }

        /// <summary>Closes a card opened by BeginDecisionCard and draws its area spine, using the rect GUILayout just resolved for the whole card - the height isn't knowable until now, which is the entire reason the spine is drawn here rather than up front.</summary>
        private void EndAreaCard(UiPalette.SystemArea area)
        {
            GUILayout.EndVertical();
            if (Event.current.type == EventType.Repaint)
            {
                UiPalette.DrawCardSpine(GUILayoutUtility.GetLastRect(), area, AreaCardSpineWidth - 1f);
            }

            GUILayout.Space(10f);
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
        private void DrawPolicyLawsTab(float availableHeight, float availableWidth)
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

            // Step B2: the stats THIS sub-screen's own levers move, directly under its selector so the
            // numbers change with the screen. Measured before it is drawn and subtracted from the
            // content budget below, so it takes space from the tab rather than pushing the content
            // scroll view past the bottom of the tab.
            float statRowWidth = availableWidth - _boxStyle.padding.horizontal - 8f;
            UiPalette.SystemArea statArea = GetPolicyScreenArea(_policyLawsCategory);
            float statRowHeight = PolicyScreenStatsRenderer.MeasureHeight(statArea, _labelStyle, statRowWidth);
            PolicyScreenStatsRenderer.Draw(statArea, _playerCountry, _labelStyle, statRowWidth);

            float contentHeight = availableHeight - _headerStyle.fontSize - _tabButtonStyle.fixedHeight - 14f - statRowHeight;
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
        /// <summary>World-map content, extracted from the former World Map sub-tab so the International
        /// sub-tab can compose it alongside Trade. Wrapper (box + scroll view) removed - the caller owns
        /// scrolling now, and nesting a scroll view inside one breaks wheel handling.</summary>
        private void DrawWorldMapContent()
        {
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

        }

        /// <summary>
        /// Macro overhaul Step A4's derived stats, finally on screen. The directive defines A4 as "pure
        /// display arithmetic" — it was built (`70798e9`) and trajectory-validated (`3d77b11`) but
        /// displayed nothing for a day, which is precisely the "built but uncalled" state this project
        /// keeps mistaking for done.
        ///
        /// **Every figure here is DERIVED, never stored.** Nothing in this method may write to the
        /// model, and nothing else may read these numbers back — that is what makes A4 safe to have
        /// landed after the release-calendar machinery rather than before it.
        ///
        /// Two unit traps live in this one row, which is why it is worth having in a single place:
        /// - **GDP per capita is in THOUSANDS per person**, not billions like every other money value in
        ///   the model — billions divided by millions. It is the one exception `MoneyUnit.Thousands`
        ///   exists for, and the P2 formatter was built to carry it.
        /// - **Deficit is signed so positive means a DEFICIT**, inverting the simulation's own
        ///   `BudgetBalance` convention, which is positive for a surplus. `DerivedStats` flips it once so
        ///   no call site has to remember.
        ///
        /// Reads LIVE state rather than the published series, deliberately: this sits directly beneath
        /// the live headline tiles and above the "As published" section, so mixing a lagged figure in
        /// here would misrepresent which of the two a player is looking at.
        /// </summary>
        private void DrawDerivedStatsRow()
        {
            FiscalTurnReport report = _simulationManager.GetLastFiscalReport(PlayerCountryId);

            GUILayout.BeginVertical(_boxStyle);
            GUILayout.Label("Derived", _headerStyle);

            float? perCapita = DerivedStats.GdpPerCapita(_playerCountry);
            GUILayout.Label(perCapita.HasValue
                ? $"GDP per capita: {UiFormat.Money(perCapita.Value, MoneyUnit.Thousands)}"
                : "GDP per capita: n/a (no population)", _labelStyle);

            // "advance a turn" rather than a zero: no turn has produced a FiscalTurnReport yet, and a
            // 0.0% tax burden is a confident wrong number of exactly the kind this project keeps finding.
            float? taxBurden = DerivedStats.TaxBurdenPercentOfGdp(_playerCountry, report);
            GUILayout.Label(taxBurden.HasValue
                ? $"Tax burden: {taxBurden.Value:F1}% of GDP"
                : "Tax burden: not yet computed (advance a turn)", _labelStyle);

            float? spending = DerivedStats.SpendingPercentOfGdp(_playerCountry, report);
            GUILayout.Label(spending.HasValue
                ? $"Government spending: {spending.Value:F1}% of GDP"
                : "Government spending: not yet computed (advance a turn)", _labelStyle);

            float? deficit = DerivedStats.DeficitPercentOfGdp(_playerCountry, report);
            if (deficit.HasValue)
            {
                // Positive is a deficit, so "higher is better" is FALSE here - the opposite of the
                // BudgetBalance colouring elsewhere, because the sign convention is the opposite too.
                DrawColoredLabel($"{(deficit.Value >= 0f ? "Deficit" : "Surplus")}: {Mathf.Abs(deficit.Value):F1}% of GDP",
                    _labelStyle, UiPalette.GetDeltaColor(deficit.Value, higherIsBetter: false));
            }
            else
            {
                GUILayout.Label("Deficit: not yet computed (advance a turn)", _labelStyle);
            }

            List<(SectorType Type, float SharePercent)> shares = DerivedStats.SectorSharesOfGdp(_playerCountry);
            if (shares.Count > 0)
            {
                var sb = new System.Text.StringBuilder("Sector shares of GDP: ");
                for (int i = 0; i < shares.Count; i++)
                {
                    if (i > 0) { sb.Append(" | "); }
                    sb.Append($"{shares[i].Type} {shares[i].SharePercent:F1}%");
                }
                GUILayout.Label(sb.ToString(), _labelStyle);
            }
            else
            {
                GUILayout.Label("Sector shares of GDP: not tracked for this country.", _labelStyle);
            }

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
            GUILayout.Label($"GDP: {UiFormat.Money(state.GDP, MoneyUnit.Billions)}", _labelStyle);
            GUILayout.Label($"Unemployment: {state.Unemployment:F2}%", _labelStyle);
            GUILayout.Label($"Inflation: {state.Inflation:F2}%", _labelStyle);
            GUILayout.Label($"Approval Rating: {state.ApprovalRating:F1}", _labelStyle);
            GUILayout.Label($"Debt-to-GDP: {state.DebtToGdpRatio:F1}%", _labelStyle);

            if (isPlayer)
            {
                GUILayout.Label($"Poverty Rate: {state.PovertyRate:F1}%", _labelStyle);
                GUILayout.Label($"Budget Balance (cumulative): {UiFormat.MoneyDelta(state.Budget, MoneyUnit.Billions)}", _labelStyle);
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
                // The one graph that draws an arbitrary stat, and therefore the one that would have needed
                // a hand-maintained "which of these are money" list. It asks the stat instead.
                graph.DrawNeutral($"{PolicyWebRenderer.GetStatName(node)} (last 50 turns)", history, null, _labelStyle,
                    moneyUnit: PolicyWebRenderer.GetStatUnit(node));
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

        /// <summary>
        /// Master Sequence step 5e, Phase C batch 2: <paramref name="drawOwnFrame"/> defaults to true so
        /// the Politics tab's own Cabinet screen (a LATER batch, which must not change yet) keeps its
        /// existing box exactly. The Decisions tab passes false because it has already wrapped this in a
        /// rounded card - without the opt-out the two frames nest and a flat grey box renders inside the
        /// card, which is what happens if you only look at one of a shared renderer's call sites.
        /// </summary>
        private void DrawCabinetDecisionModal(CabinetPortfolio portfolio, CabinetDecision decision, bool drawOwnFrame = true)
        {
            if (drawOwnFrame)
            {
                GUILayout.BeginVertical(_boxStyle);
            }

            GUILayout.Label($"DECISION - {GetPortfolioName(portfolio)}: {decision.Name}", _eventBannerStyle);
            GUILayout.Label(decision.Description, _labelStyle);
            foreach (CabinetDecisionOption option in decision.Options)
            {
                if (GUILayout.Button(option.Label, _neutralActionButtonStyle))
                {
                    _simulationManager.ResolveCabinetDecision(PlayerCountryId, portfolio, decision, option);
                }
            }

            if (drawOwnFrame)
            {
                GUILayout.EndVertical();
            }
        }

        /// <summary>
        /// Master Sequence step 5e, Phase A: the old standalone Foreign Policy tab is fully retired,
        /// not split - its ENTIRE content was always just this interrupt (confirmed by reading its old
        /// body: explanatory text + either the modal or "No meeting currently pending," nothing else),
        /// so it moves to Decisions wholesale (see DrawDecisionsTab) with nothing left behind. Only
        /// this modal renderer survives, reused as-is from Decisions.
        /// </summary>
        private void DrawForeignPolicyMeetingModal(ForeignPolicyMeeting meeting, bool drawOwnFrame = true)
        {
            if (drawOwnFrame)
            {
                GUILayout.BeginVertical(_boxStyle);
            }

            GUILayout.Label($"MEETING: {meeting.Name}", _eventBannerStyle);
            GUILayout.Label(meeting.Description, _labelStyle);
            foreach (ForeignPolicyMeetingOption option in meeting.Options)
            {
                if (GUILayout.Button(option.Label, _neutralActionButtonStyle))
                {
                    _simulationManager.ResolveForeignPolicyMeeting(PlayerCountryId, option);
                }
            }

            if (drawOwnFrame)
            {
                GUILayout.EndVertical();
            }
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

            // Master Sequence step 5e, Phase C batch 3: each pending bill now carries the DIRECTION it
            // was scored on, not just a pre-formatted sentence, so the lean bar below can show the
            // seat-weighted alignment Parliament actually decides on rather than only its sign.
            var pending = new List<(string Label, float Direction, UiPalette.SystemArea Area)>();

            BudgetBill budgetBill = _simulationManager.GetPendingBudgetBill(PlayerCountryId);
            if (budgetBill != null)
            {
                pending.Add(($"Annual budget bill - resolves in {budgetBill.DaysRemaining} day(s).",
                    ParliamentSystem.GetBillDirection(_playerCountry, budgetBill), UiPalette.SystemArea.Fiscal));
            }

            foreach (TaxProgramBill bill in _simulationManager.GetPendingTaxProgramBills(PlayerCountryId))
            {
                pending.Add(($"{(bill.IsAdd ? "Implement" : "Remove")} {bill.Type} - resolves in {bill.DaysRemaining} day(s).",
                    ParliamentSystem.GetTaxProgramBillDirection(_playerCountry, bill), UiPalette.SystemArea.Fiscal));
            }

            foreach (WelfareProgramBill bill in _simulationManager.GetPendingWelfareProgramBills(PlayerCountryId))
            {
                pending.Add(($"{(bill.IsAdd ? "Implement" : "Remove")} {bill.Type} - resolves in {bill.DaysRemaining} day(s).",
                    ParliamentSystem.GetWelfareProgramBillDirection(_playerCountry, bill), UiPalette.SystemArea.Welfare));
            }

            LaborPolicyBill laborBill = _simulationManager.GetPendingLaborBill(PlayerCountryId);
            if (laborBill != null)
            {
                pending.Add(($"Labor Market bill - resolves in {laborBill.DaysRemaining} day(s).",
                    ParliamentSystem.GetLaborBillDirection(_playerCountry, laborBill), UiPalette.SystemArea.Labor));
            }

            CrimeJusticePolicyBill crimeJusticeBill = _simulationManager.GetPendingCrimeJusticeBill(PlayerCountryId);
            if (crimeJusticeBill != null)
            {
                pending.Add(($"Crime & Justice bill - resolves in {crimeJusticeBill.DaysRemaining} day(s).",
                    ParliamentSystem.GetCrimeJusticeBillDirection(_playerCountry, crimeJusticeBill), UiPalette.SystemArea.CrimeJustice));
            }

            SectorPolicyBill sectorBill = _simulationManager.GetPendingSectorBill(PlayerCountryId);
            if (sectorBill != null)
            {
                pending.Add(($"Economic Sectors bill - resolves in {sectorBill.DaysRemaining} day(s).",
                    ParliamentSystem.GetSectorBillDirection(_playerCountry, sectorBill), UiPalette.SystemArea.Sectors));
            }

            TradePolicyBill tradeBill = _simulationManager.GetPendingTradeBill(PlayerCountryId);
            if (tradeBill != null)
            {
                pending.Add(($"Trade bill - resolves in {tradeBill.DaysRemaining} day(s).",
                    ParliamentSystem.GetTradeBillDirection(_playerCountry, tradeBill), UiPalette.SystemArea.Trade));
            }

            SwfDrawdownBill drawdownBill = _simulationManager.GetPendingSwfDrawdownBill(PlayerCountryId);
            if (drawdownBill != null)
            {
                // Names its amount, unlike the other four. A drawdown IS its number - "an emergency
                // drawdown bill" tells a player nothing about what they are about to be committed to.
                pending.Add(($"SWF emergency drawdown - {drawdownBill.WithdrawalPercentOfGdp:F1}% of GDP, resolves in {drawdownBill.DaysRemaining} day(s).",
                    ParliamentSystem.GetSwfDrawdownBillDirection(_playerCountry, drawdownBill), UiPalette.SystemArea.SovereignWealth));
            }

            if (pending.Count == 0)
            {
                GUILayout.Label("No bill currently before Parliament.", _labelStyle);
                return;
            }

            foreach ((string label, float direction, UiPalette.SystemArea area) in pending)
            {
                DrawPendingBillCard(label, direction, area);
            }
        }

        /// <summary>
        /// One pending bill: its description, the PASS/FAIL verdict, and a bar showing HOW comfortably -
        /// <see cref="ParliamentSystem.GetSeatWeightedAlignment"/>, the quantity the vote is actually
        /// decided on, drawn diverging from a centre threshold.
        ///
        /// Deliberately NOT `PoliSimWidgets.SupportBar`, despite that widget existing and looking like
        /// the obvious fit. SupportBar renders "N of 200 seats, majority 101", and this simulation has
        /// no seats-based majority at all: parties are weighted by the STRENGTH of their fiscal stance,
        /// so a bill can pass with fewer aligned seats than opposed ones and fail with more. Drawing a
        /// majority line here would assert a rule the model does not implement. The magnitude is shown
        /// as a bar only, with no number attached, because its display range is a presentation choice
        /// rather than anything the simulation claims precision about.
        /// </summary>
        private void DrawPendingBillCard(string label, float direction, UiPalette.SystemArea area)
        {
            bool wouldPass = ParliamentSystem.WouldBillPass(_playerCountry, direction);

            // A zero-direction bill (drafts introduced unchanged) passes unconditionally - WouldBillPass
            // short-circuits before scoring it. The bar has to short-circuit on the SAME condition:
            // Unity's Mathf.Sign(0f) returns 1, not 0, so scoring it anyway yields parliament's raw net
            // stance - negative in this game's documented tied-parties case - and would paint a red bar
            // directly beside the words "leans PASS".
            bool contested = !Mathf.Approximately(direction, 0f);
            float alignment = contested ? ParliamentSystem.GetSeatWeightedAlignment(_playerCountry, direction) : 0f;

            BeginAreaCard(null, area);
            GUILayout.Label(label, _labelStyle);
            DrawColoredLabel(
                contested ? (wouldPass ? "Currently leans PASS" : "Currently leans FAIL") : "Unopposed - no change requested",
                _labelStyle,
                UiPalette.GetDeltaColor(wouldPass ? 1f : -1f, higherIsBetter: true));

            Rect barRect = GUILayoutUtility.GetRect(10f, _labelStyle.fontSize * 0.7f, GUILayout.ExpandWidth(true));
            if (Event.current.type == EventType.Repaint)
            {
                UiPalette.DrawDivergingBar(barRect, alignment, PendingBillLeanDisplayRange);
            }

            EndAreaCard(area);
        }

        private void DrawCabinetPortfolioPanel(CabinetPortfolio portfolio)
        {
            GUILayout.BeginVertical(_boxStyle);
            // Headed in the PORTFOLIO's own area color now (see UiPalette.GetPortfolioArea), not the
            // flat Political every cabinet surface used before - so the three portfolio panels read as
            // three different departments rather than one repeated block.
            DrawColoredLabel(GetPortfolioName(portfolio), _headerStyle, UiPalette.GetAreaColor(UiPalette.GetPortfolioArea(portfolio)));

            if (_playerCountry.CabinetMinisters.TryGetValue(portfolio, out CabinetMinister minister))
            {
                GUILayout.BeginHorizontal();
                DrawPersonPortrait(IconLibrary.GetCabinetPortrait(portfolio, minister.Name), UiPalette.GetPortfolioArea(portfolio));

                GUILayout.BeginVertical();
                GUILayout.Label($"{minister.Name} ({minister.Philosophy})", _labelStyle);
                GUILayout.Label(minister.Description, _labelStyle);
                if (GUILayout.Button("Reshuffle", _neutralActionButtonStyle))
                {
                    _playerCountry.CabinetMinisters.Remove(portfolio);
                    _playerCountry.State.ApprovalRating = Mathf.Clamp(_playerCountry.State.ApprovalRating - CabinetSystem.ReshuffleApprovalCost, 0f, 100f);
                    _cabinetCandidatesByPortfolio[portfolio] = CabinetSystem.GenerateCandidates(portfolio);
                }
                GUILayout.EndVertical();

                GUILayout.EndHorizontal();
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
            GUILayout.BeginHorizontal();
            DrawPersonPortrait(IconLibrary.GetCabinetPortrait(portfolio, candidate.Name), UiPalette.GetPortfolioArea(portfolio));

            GUILayout.BeginVertical();
            GUILayout.Label($"{candidate.Name} ({candidate.Philosophy})", _labelStyle);
            GUILayout.Label(candidate.Description, _labelStyle);
            if (GUILayout.Button($"Appoint {candidate.Name}", _neutralActionButtonStyle))
            {
                _playerCountry.CabinetMinisters[portfolio] = candidate;
                _cabinetCandidatesByPortfolio.Remove(portfolio);
                RecomputePolicyPreview();
            }
            GUILayout.EndVertical();

            GUILayout.EndHorizontal();
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
                _labelStyle, "F1", moneyUnit: null);
            GUILayout.Space(10f);

            var sectorSlices = new List<PieSlice>();
            int sectorIndex = 0;
            foreach (Sector sector in _playerCountry.Sectors)
            {
                sectorSlices.Add(new PieSlice(sector.Type.ToString(), sector.EmploymentShare, UiPalette.GetCategoricalColor(sectorIndex)));
                sectorIndex++;
            }
            _sectorEmploymentPieChart.Draw($"{_playerCountry.Name}: Employment Share by Sector", sectorSlices, _labelStyle, "F1", moneyUnit: null);
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
                _spendingAllocationPieChart.Draw($"{_playerCountry.Name}: Spending Allocation", spendingSlices, _labelStyle, valueFormat: null, moneyUnit: MoneyUnit.Billions);
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
            _taxRevenuePieChart.Draw($"{_playerCountry.Name}: Theoretical Tax Revenue by Source", taxSlices, _labelStyle, valueFormat: null, moneyUnit: MoneyUnit.Billions);
            GUILayout.Space(10f);

            var populationSlices = new List<PieSlice>();
            foreach (Country country in _world.Countries)
            {
                populationSlices.Add(new PieSlice(country.Name, country.State.Population, UiPalette.GetCountryColor(country.Id)));
            }
            _populationPieChart.Draw("Population Share by Country (millions)", populationSlices, _labelStyle, "F1", moneyUnit: null);
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
            DrawColoredLabel($"Overall Trade Balance: {UiFormat.MoneyDelta(state.TradeBalance, MoneyUnit.Billions)}", _labelStyle, UiPalette.GetDeltaColor(state.TradeBalance, higherIsBetter: true));
            _tradeBalanceGraph.Draw("Trade Balance", _playerCountry.History.TradeBalance.Quarterly, null, _labelStyle, higherIsBetter: true,
                moneyUnit: PolicyWebRenderer.GetStatUnit(StatNodeId.TradeBalance));
        }

        /// <summary>Policy half of the old Trade tab (the TradePolicyBill and every per-partner row) - see DrawTradeStatsContent's own doc comment for the split reasoning. Called from DrawPolicyLawsTab.</summary>
        private void DrawTradePolicyContent()
        {
            DrawColoredLabel("Trade Policy", _headerStyle, UiPalette.GetAreaColor(UiPalette.SystemArea.Trade));
            GUILayout.Label("Master Sequence step 5d: the base rate and every partner override's RATE below are DRAFTS - nothing happens until you introduce them as one standalone bill, which resolves independently of the annual budget cycle. Setting/resetting whether a partner override exists at all stays an immediate, structural action, unchanged.", _labelStyle);
            GUILayout.Space(6f);

            BeginAreaCard("TRADE BILL", UiPalette.SystemArea.Trade);
            DrawTradeBillStatusAndIntroduce();
            DrawTradeLiveEstimate();
            EndAreaCard(UiPalette.SystemArea.Trade);

            float draftTariffRate = GetTariffRateInput(_playerCountry.BaseTariffRate);
            DrawDraftLabel($"General Base Tariff Rate - Standing: {_playerCountry.BaseTariffRate:F2}%, Draft: {draftTariffRate:F2}% (range {MinBaseTariffRate:F0}-{MaxBaseTariffRate:F0}%; applies to any partner with no override, and only where it isn't superseded by trade-bloc membership)", _playerCountry.BaseTariffRate, draftTariffRate);
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
                DrawDraftLabel($"Override rate - Standing: {link.PlayerTariffOverride:F2}%, Draft: {draftRate:F2}% (range {PartnerTariffOverrideMin:F0}-{PartnerTariffOverrideMax:F0}%; applies via the Trade bill below)", link.PlayerTariffOverride, draftRate);
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

        /// <summary>
        /// The SWF emergency drawdown's own bill row, on the Sovereign Wealth Fund tab. Elias's A2
        /// ruling, closing the gap where a genuine emergency could sit behind a fiscal-year vote up to a
        /// year away because every SWF change rode the annual omnibus.
        ///
        /// **Every control renders every frame**, with availability expressed through `GUI.enabled`
        /// composed with the ambient state rather than by omitting controls — the stable-control-layout
        /// pattern this project adopted after the mid-drag freeze investigation. A fund that does not
        /// exist yet disables the button; it does not remove it.
        /// </summary>
        private void DrawSwfDrawdownBillStatusAndIntroduce()
        {
            SwfDrawdownBill pendingBill = _simulationManager.GetPendingSwfDrawdownBill(PlayerCountryId);
            bool fundExists = _playerCountry.SovereignWealthFund != null;

            GUILayout.Label("Emergency Drawdown (standalone bill - does not wait for the annual budget)", _headerStyle);

            string statusText = pendingBill != null
                ? $"A drawdown bill is before Parliament - {pendingBill.WithdrawalPercentOfGdp:F1}% of GDP, resolves in {pendingBill.DaysRemaining} day(s)."
                : fundExists
                    ? "No drawdown bill before Parliament. Introduce one to withdraw from the fund now rather than at the fiscal year."
                    : "No fund exists to draw down. Create one through the annual budget first.";
            GUILayout.Label(statusText, _labelStyle);

            bool ambientEnabled = GUI.enabled;
            GUI.enabled = ambientEnabled && fundExists && pendingBill == null;

            GUILayout.BeginHorizontal();
            GUILayout.Label($"Withdraw: {_swfDrawdownPercentInput:F1}% of GDP", _labelStyle, GUILayout.Width(200f));
            _swfDrawdownPercentInput = GUILayout.HorizontalSlider(_swfDrawdownPercentInput,
                MinSwfDrawdownPercentOfGdp, MaxSwfDrawdownPercentOfGdp, _sliderStyle, _sliderThumbStyle);
            GUILayout.EndHorizontal();

            if (fundExists)
            {
                // What the request would actually deliver, not what it asks for. The fund may hold less,
                // and finding that out only after a multi-day vote would be the worst moment to learn it.
                float requested = _playerCountry.State.GDP * _swfDrawdownPercentInput / 100f;
                float deliverable = Mathf.Min(requested, _playerCountry.SovereignWealthFund.TotalAssets);
                string capped = deliverable < requested ? "  (CAPPED - the fund holds less than this)" : string.Empty;
                GUILayout.Label($"Would release {UiFormat.Money(deliverable, MoneyUnit.Billions)} into the budget{capped}", _labelStyle);
            }

            if (GUILayout.Button("Introduce Emergency Drawdown Bill", _neutralActionButtonStyle))
            {
                _simulationManager.IntroduceSwfDrawdownBill(PlayerCountryId,
                    new SwfDrawdownBill { WithdrawalPercentOfGdp = _swfDrawdownPercentInput });
            }
            GUI.enabled = ambientEnabled;
        }

        /// <summary>See DrawCrimeJusticeLiveEstimate's own doc comment - identical pattern. Only the base rate sways this estimate (see ParliamentSystem.GetTradeBillDirection's own doc comment on why partner overrides are excluded).</summary>
        private void DrawTradeLiveEstimate()
        {
            DrawBillLiveEstimate(ParliamentSystem.GetTradeBillDirection(_playerCountry, BuildTradeBillFromDrafts()));
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

            GUILayout.Label($"Revenue (Tax): {UiFormat.Money(report.Revenue, MoneyUnit.Billions)}", _labelStyle);
            GUILayout.Label($"Baseline Government Spending: {UiFormat.Money(report.BaselineGovernmentSpending, MoneyUnit.Billions)}", _labelStyle);
            GUILayout.Label($"Discretionary Spending Change (this turn): {UiFormat.MoneyDelta(report.DiscretionarySpending, MoneyUnit.Billions)}", _labelStyle);
            GUILayout.Label($"Mandatory Spending: {UiFormat.Money(report.MandatorySpending, MoneyUnit.Billions)}", _labelStyle);
            GUILayout.Label($"Unemployment Benefit Cost: {UiFormat.Money(report.UnemploymentBenefitCost, MoneyUnit.Billions)}", _labelStyle);
            GUILayout.Label($"Interest On Debt: {UiFormat.Money(report.InterestOnDebt, MoneyUnit.Billions)}", _labelStyle);
            GUILayout.Label($"Welfare Program Cost: {UiFormat.Money(report.WelfareCost, MoneyUnit.Billions)}", _labelStyle);
            GUILayout.Label($"Tariff Revenue Collected: {UiFormat.Money(report.TariffRevenue, MoneyUnit.Billions)}", _labelStyle);
            GUILayout.Space(6f);
            DrawColoredLabel($"Net (matches this turn's Budget change): {UiFormat.MoneyDelta(net, MoneyUnit.Billions)}", _headerStyle, UiPalette.GetDeltaColor(net, higherIsBetter: true));
        }

        /// <summary>
        /// Re-surfaces any pending blocking interrupt inside the Budget screen itself.
        /// </summary>
        private void DrawFullScreenPendingInterruptBanner()
        {
            // The Budget tab runs full-screen (see OnGUI), which hides the calendar/speed strip - and that
            // strip is normally the ONLY always-visible indicator of why simulated time has stopped.
            // Working discipline item 2 exists because of a real bug where time silently halted and the
            // player had no way to tell why, since the resolving UI lived on a tab they weren't looking at.
            // Going full-screen here would recreate exactly that, in the one screen most likely to be open
            // when an interrupt fires - so any pending interrupt is re-surfaced here instead.
            //
            // The Budget Process's own pause is deliberately NOT listed: this screen already states that
            // status directly, and repeating it would train players to ignore the banner.
            var blocking = new List<string>();
            if (_fedChairCandidates != null && _fedChairCandidates.Count > 0)
            {
                blocking.Add("a Fed Chair appointment");
            }

            if (_simulationManager.GetPendingCabinetDecisions(PlayerCountryId).Count > 0)
            {
                blocking.Add("a Cabinet decision");
            }

            if (_simulationManager.GetPendingForeignPolicyMeeting(PlayerCountryId) != null)
            {
                blocking.Add("a Foreign Policy meeting");
            }

            if (blocking.Count == 0)
            {
                return;
            }

            GUILayout.Label(
                $"TIME IS PAUSED - waiting on {string.Join(" and ", blocking)}. Open the Decisions tab to resolve it; speed controls are on any other tab.",
                _eventBannerStyle);
            GUILayout.Space(4f);
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
            DrawFullScreenPendingInterruptBanner();
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
            // FIXED 2026-08-01: all three columns are now derived from availableWidth - the width this TAB
            // actually owns - and are guaranteed to sum to LESS than it, so the row cannot scroll
            // horizontally at any window size.
            //
            // The bug: summaryColumnWidth was `Screen.width * LeftColumnWidthFraction` (0.45), i.e. 45% of
            // the WHOLE window - but this tab lives in the right column and owns only ~52% of the window.
            // That single column therefore claimed ~86% of the row, and the center column holding every
            // bill line item was pushed off-screen behind a horizontal scrollbar that could not
            // practically be used. The reasoning previously recorded here - give the preview panel "its
            // natural width unconditionally" - measured that natural width against the panel's ORIGINAL
            // home in the 45%-wide LEFT column, and was never re-derived when it was reused inside the
            // much narrower right column.
            float scrollbarAllowance = 18f;
            float usableWidth = availableWidth - columnSpacing * 2f - scrollbarAllowance;
            // Floor raised from 5x to 7x the label font: even with wrapping, a button's minimum width is
            // its longest WORD, and "Sovereign" needs ~97px at the smallest supported font - more than the
            // 94px this column got at 16% on a 1227x690 window. Below this floor the category buttons
            // overflow their own column, which is the exact failure the rest of this screen just had.
            float categoryColumnWidth = Mathf.Clamp(usableWidth * 0.16f, _labelStyle.fontSize * 7f, _labelStyle.fontSize * 10f);
            float summaryColumnWidth = usableWidth * 0.34f;
            float centerColumnWidth = usableWidth - categoryColumnWidth - summaryColumnWidth;
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

            // Step B2: pinned ABOVE this column's scroll view, not inside it. The row describes what
            // the line items below it move, so scrolling the list must not scroll the summary of the
            // list out of sight - the same reasoning that keeps the calendar panel outside the left
            // column's scroll view.
            float statRowWidth = centerColumnWidth - _boxStyle.padding.horizontal - 8f;
            UiPalette.SystemArea statArea = GetPolicyScreenArea(_budgetProcessCategory);
            float statRowHeight = PolicyScreenStatsRenderer.MeasureHeight(statArea, _labelStyle, statRowWidth);
            PolicyScreenStatsRenderer.Draw(statArea, _playerCountry, _labelStyle, statRowWidth);

            _budgetProcessCenterScrollPosition = GUILayout.BeginScrollView(_budgetProcessCenterScrollPosition, GUILayout.Height(columnsHeight - _labelStyle.fontSize - statRowHeight));
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
            GUIStyle style = BuildSubTabStyle(selected);
            // Same treatment as the horizontal sub-tabs (see BuildSubTabStyle), and this column is where
            // it matters most: it is the narrowest surface in the UI, so "Sovereign Wealth Fund" and
            // "Infrastructure" wrap here even at large window sizes.
            if (GUILayout.Button(label, style, GUILayout.ExpandWidth(true), GUILayout.MinHeight(_tabButtonStyle.fixedHeight)))
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
        /// <summary>
        /// Master Sequence step 5e, Phase C batch 5: the annual budget bill's own live estimate. This was
        /// a FIFTH copy of the same renderer batch 4 collapsed for the four standalone tiers - and the
        /// one on the most important screen in the game. It now shares DrawBillLiveEstimate, so the
        /// budget bill gains the same lean bar as every other bill and, more importantly, the same
        /// zero-direction handling: WouldBillPass's BudgetBill overload is documented as computing the
        /// bill's direction and delegating to the float core, so passing that direction through is
        /// exactly equivalent to the overload this used to call.
        /// </summary>
        private void DrawLegislativeSupportEstimate()
        {
            GUILayout.Label("Legislative Support (current draft)", _headerStyle);
            DrawBillLiveEstimate(ParliamentSystem.GetBillDirection(_playerCountry, BuildBudgetBillFromDrafts()));
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
            TaxProgramBill pendingBill = FindPendingTaxProgramBill(taxLine.Type);

            GUILayout.BeginHorizontal();
            GUILayout.Label(taxLine.Type.ToString(), _labelStyle, GUILayout.Width(labelWidth));

            string toggleLabel = pendingBill != null
                // Short labels on purpose. "Introduce Implement Bill" plus the tax/program name beside it
                // needed ~362px inside a column that is 293px at ordinary window sizes, so the button drew
                // straight past the column edge - the same overflow that clipped the preview panel. The
                // words dropped are recoverable from context: the row already names the program, and this
                // screen's own header explains that implementing or removing submits a standalone bill.
                ? $"Pending ({pendingBill.DaysRemaining}d)"
                : taxLine.IsImplemented ? "Remove" : "Implement";
            GUIStyle toggleStyle = taxLine.IsImplemented ? _removeButtonStyle : _implementButtonStyle;
            bool ambientEnabledForButton = GUI.enabled;
            GUI.enabled = ambientEnabledForButton && pendingBill == null;
            if (GUILayout.Button(toggleLabel, toggleStyle, GUILayout.ExpandWidth(true)))
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
            // Only an IMPLEMENTED line can have a pending rate change - an unimplemented one is changed
            // by its own standalone Implement/Remove bill above, not by this slider, so it must never
            // show the amber cue regardless of what the (inactive) draft value happens to hold.
            DrawDraftLabel(draftLabel, taxLine.IsImplemented && !Mathf.Approximately(draftRate, taxLine.Rate));

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
            _povertyRateGraph.Draw("Poverty Rate", _playerCountry.History.PovertyRate.Quarterly, null, _labelStyle, higherIsBetter: false, moneyUnit: null);
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
            WelfareProgramBill pendingBill = FindPendingWelfareProgramBill(welfareProgram.Type);

            GUILayout.BeginHorizontal();
            GUILayout.Label(welfareProgram.Type.ToString(), _labelStyle, GUILayout.Width(labelWidth));

            string toggleLabel = pendingBill != null
                // Short labels on purpose. "Introduce Implement Bill" plus the tax/program name beside it
                // needed ~362px inside a column that is 293px at ordinary window sizes, so the button drew
                // straight past the column edge - the same overflow that clipped the preview panel. The
                // words dropped are recoverable from context: the row already names the program, and this
                // screen's own header explains that implementing or removing submits a standalone bill.
                ? $"Pending ({pendingBill.DaysRemaining}d)"
                : welfareProgram.IsImplemented ? "Remove" : "Implement";
            GUIStyle toggleStyle = welfareProgram.IsImplemented ? _removeButtonStyle : _implementButtonStyle;
            bool ambientEnabledForButton = GUI.enabled;
            GUI.enabled = ambientEnabledForButton && pendingBill == null;
            if (GUILayout.Button(toggleLabel, toggleStyle, GUILayout.ExpandWidth(true)))
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
            // See DrawTaxLineRow's equivalent - an unimplemented program is changed by its own
            // standalone bill, not by this slider, so it never shows the amber pending-change cue.
            DrawDraftLabel(draftLabel, welfareProgram.IsImplemented && !Mathf.Approximately(draftGenerosity, welfareProgram.GenerosityLevel));

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

            BeginAreaCard("ECONOMIC SECTORS BILL", UiPalette.SystemArea.Sectors);
            DrawSectorBillStatusAndIntroduce();
            DrawSectorLiveEstimate();
            EndAreaCard(UiPalette.SystemArea.Sectors);

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
            DrawDraftLabel($"Subsidy - Standing: {sector.SubsidyLevel:F0}, Draft: {draftSubsidy:F0}", sector.SubsidyLevel, draftSubsidy);
            _sectorSubsidyInputs[sector.Type] = GUILayout.HorizontalSlider(draftSubsidy, MinPolicyDialLevel, MaxPolicyDialLevel, _sliderStyle, _sliderThumbStyle);

            float draftRegulation = GetSectorRegulationInput(sector.Type, sector.RegulationLevel);
            DrawDraftLabel($"Regulation - Standing: {sector.RegulationLevel:F0}, Draft: {draftRegulation:F0} (0 = light-touch, 100 = heavily regulated)", sector.RegulationLevel, draftRegulation);
            _sectorRegulationInputs[sector.Type] = GUILayout.HorizontalSlider(draftRegulation, MinPolicyDialLevel, MaxPolicyDialLevel, _sliderStyle, _sliderThumbStyle);

            float draftTaxCredit = GetSectorTaxCreditInput(sector.Type, sector.TaxCreditLevel);
            DrawDraftLabel($"Tax Credits - Standing: {sector.TaxCreditLevel:F0}, Draft: {draftTaxCredit:F0}", sector.TaxCreditLevel, draftTaxCredit);
            _sectorTaxCreditInputs[sector.Type] = GUILayout.HorizontalSlider(draftTaxCredit, MinPolicyDialLevel, MaxPolicyDialLevel, _sliderStyle, _sliderThumbStyle);

            float draftResearchGrants = GetSectorResearchGrantsInput(sector.Type, sector.ResearchGrantsLevel);
            DrawDraftLabel($"Research Grants - Standing: {sector.ResearchGrantsLevel:F0}, Draft: {draftResearchGrants:F0}", sector.ResearchGrantsLevel, draftResearchGrants);
            _sectorResearchGrantsInputs[sector.Type] = GUILayout.HorizontalSlider(draftResearchGrants, MinPolicyDialLevel, MaxPolicyDialLevel, _sliderStyle, _sliderThumbStyle);

            float draftDeregulation = GetSectorDeregulationInput(sector.Type, sector.DeregulationNationalizationLevel);
            DrawDraftLabel($"Deregulation/Nationalization - Standing: {sector.DeregulationNationalizationLevel:F0}, Draft: {draftDeregulation:F0} (0 = fully nationalized, 100 = fully deregulated/private)", sector.DeregulationNationalizationLevel, draftDeregulation);
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
            DrawBillLiveEstimate(ParliamentSystem.GetSectorBillDirection(_playerCountry, BuildSectorBillFromDrafts()));
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
                // Net position is signed deliberately - a net CREDITOR (Sweden is one from turn 1) shows
                // a negative net position, and that is a real fiscal state rather than a display error.
                ? $"Standing: fund exists. Total Assets: {UiFormat.Money(fund.TotalAssets, MoneyUnit.Billions)}  |  Government Debt (gross): {UiFormat.Money(_playerCountry.State.GovernmentDebt, MoneyUnit.Billions)}  |  Net Government Position: {UiFormat.MoneyDelta(_playerCountry.State.GovernmentDebt - fund.TotalAssets, MoneyUnit.Billions)}"
                : "Standing: no fund exists. Creating one (once the annual budget bill passes) starts a new budget expense (the contribution) in exchange for market returns on its growing assets - it can also be drawn down during a recession or emergency instead of borrowing.";
            GUILayout.Label(standingText, _labelStyle);

            string estimateText = fund != null
                ? $"Estimated this turn - Contribution/Withdrawal: {_cachedSwfContributionText}, Returns: {_cachedSwfReturnsText}"
                : "Estimated this turn - not applicable (no fund).";
            DrawColoredLabel(estimateText, _labelStyle, fund != null
                ? UiPalette.GetDeltaColor(_cachedSwfReturnsEstimateRaw, higherIsBetter: true)
                : UiPalette.GetDeltaColor(0f, higherIsBetter: true));
            // The fund's own existence is a draft too: amber whenever the drafted existence differs from
            // whether a fund actually stands today.
            DrawDraftLabel(draftExists ? "Draft: fund drafted to exist." : "Draft: not implemented.", draftExists != (fund != null));
            GUILayout.Space(8f);

            SovereignWealthFund standingDefaults = fund ?? new SovereignWealthFund();
            bool ambientEnabled = GUI.enabled;

            float draftContributionRate = GetSwfContributionRateInput(standingDefaults.ContributionRatePercent);
            DrawDraftLabel($"Contribution/Withdrawal Rate: {draftContributionRate:+0.0;-0.0;0}% of GDP per turn (negative draws the fund down - use during a recession or emergency instead of borrowing)", standingDefaults.ContributionRatePercent, draftContributionRate);
            GUI.enabled = ambientEnabled && draftExists;
            float newContributionRate = GUILayout.HorizontalSlider(draftContributionRate, MinSwfContributionRate, MaxSwfContributionRate, _sliderStyle, _sliderThumbStyle);
            GUI.enabled = ambientEnabled;
            if (draftExists)
            {
                _swfContributionRateInput = newContributionRate;
            }

            float draftDomesticAllocation = GetSwfDomesticAllocationInput(standingDefaults.DomesticAllocationPercent);
            DrawDraftLabel($"Domestic Allocation: {draftDomesticAllocation:F0}% (rest international - this pass doesn't model differing returns by allocation)", standingDefaults.DomesticAllocationPercent, draftDomesticAllocation);
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

            // The emergency path, last on the tab and visually separated: everything above it rides the
            // annual budget bill, and this one deliberately does not. Placing it beside those controls
            // would blur exactly the distinction it exists to draw.
            GUILayout.Space(14f);
            DrawSwfDrawdownBillStatusAndIntroduce();
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
            _debtToGdpGraph.Draw("Debt-to-GDP", _playerCountry.History.DebtToGdpRatio.Quarterly, null, _labelStyle, higherIsBetter: false, moneyUnit: null,
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
            string valueText = report != null ? UiFormat.Money(report.InterestOnDebt, MoneyUnit.Billions) : "not yet computed (advance a turn)";
            GUILayout.Label($"Interest on Debt (automatic, last turn): {valueText}", _labelStyle);
        }

        /// <summary>One SpendingLine's row: a slider representing a PERCENTAGE change of its own current Amount, bounded by <paramref name="rangePercent"/> (narrower for Mandatory - see DrawSpendingPolicy), showing both the requested percentage and the dollar amount it implies at the line's current size, plus a bar sized relative to <paramref name="maxAmountInGroup"/> (its own Mandatory/Discretionary group's largest line) for an at-a-glance size comparison.</summary>
        private void DrawSpendingLineRow(SpendingLine spendingLine, float rangePercent, float maxAmountInGroup)
        {
            float draftPercent = GetSpendingLineInput(spendingLine.Category);
            float impliedDollarChange = spendingLine.Amount * draftPercent / 100f;
            // Spending drafts are expressed as a percentage CHANGE rather than a standing/draft pair, so
            // "differs from standing" here means a non-zero change - the same amber cue, reached from the
            // other direction.
            DrawDraftLabel(
                $"{spendingLine.Category}: {UiFormat.Money(spendingLine.Amount, MoneyUnit.Billions)}  Change: {draftPercent:+0.0;-0.0;0}% ({UiFormat.MoneyDelta(impliedDollarChange, MoneyUnit.Billions)})",
                !Mathf.Approximately(draftPercent, 0f));
            UiPalette.DrawBar(spendingLine.Amount / maxAmountInGroup, UiPalette.GetAreaColor(UiPalette.SystemArea.Fiscal), 8f);
            float newPercent = GUILayout.HorizontalSlider(draftPercent, -rangePercent, rangePercent, _sliderStyle, _sliderThumbStyle);
            _spendingLineInputs[spendingLine.Category] = newPercent;
        }
    }
}
