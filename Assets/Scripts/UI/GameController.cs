using System.Collections.Generic;
using System.Globalization;
using PoliSim.Data;
using PoliSim.Persistence;
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

        /// <summary>Policy/Laws tab's 6 sub-categories - each already has (or, for Trade/Policy Web, now gains) its own standalone-bill or reference-tool identity. Laws (law system MVP slice, 2026-08-24): the named-preset browser over the existing dial space - see DrawLawsTab.</summary>
        private enum PolicyLawsCategory { LaborMarket, CrimeJustice, Sectors, PolicyWeb, Trade, Laws }

        /// <summary>Law system MVP slice: the Laws browser's category filter - "All" plus one member per LawCategory. A separate UI-only enum from Data.LawCategory (which has no "All" concept) rather than a nullable LawCategory?, since DrawSubCategoryButton&lt;T&gt; requires T : struct, System.Enum - Nullable&lt;LawCategory&gt; does not satisfy that constraint, so this can't self-derive from LawCategory's members at compile time. <b>The browser rebuild's own finding (2026-08-25): this filter has never once narrowed anything, and that is NOT a mechanism defect - it is a real, reported coupling.</b> LawCategory has exactly one populated member (CrimeJustice), so "All" and "Crime & Justice" render byte-identical lists; the fix for that is more law CATEGORIES, not a UI change. What this enum's shape does cost: it must be hand-extended in lockstep with LawCategory every time a second category ships, because the generic constraint above rules out deriving it automatically. That coupling - not a bug - is the honest cause.</summary>
        private enum LawBrowserFilter { All, CrimeJustice, LaborMarket }

        /// <summary>Law system MVP slice, browser rebuild (2026-08-25): the status filter/sort dimension the marathon's own stop condition found missing - "the top two rows both un-enacted, no sort-by-status" (CLAUDE.md, run_85g_bill_laws.png). All four values are always offered regardless of LawCategory's population, unlike LawBrowserFilter above - status is a property of ENACTMENT, not of catalog content, so this dimension is never inert the way the category one currently is.</summary>
        private enum LawStatusFilter { All, Enacted, Pending, Available }

        /// <summary>Code-review pass (2026-08-25): one law plus its enactment state, computed ONCE per
        /// frame in the filter loop and threaded through the list row, the detail pane and the
        /// affordability count - the same law's `EnactedLaws.Exists`/`GetPendingLawBill` lookup was
        /// previously repeated independently in three places, a real duplication risk (one call site
        /// updated, e.g. a new null-guard, and the other two silently drift). A readonly struct rather
        /// than three parallel lists, so a row and its state can never desync by index.</summary>
        private readonly struct LawRowEntry
        {
            public readonly LawDefinition Law;
            public readonly bool Enacted;
            public readonly LawBill PendingBill;

            public LawRowEntry(LawDefinition law, bool enacted, LawBill pendingBill)
            {
                Law = law;
                Enacted = enacted;
                PendingBill = pendingBill;
            }
        }

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
                // TURN->YEAR (non-trivial #2 of 2): was "Full Turn" - the one label of the four that
                // didn't follow the "1 [Unit]" pattern its siblings do, because a turn wasn't a round
                // real-world unit worth counting "1" of. Now that a turn IS a year, "1 Year" both fits
                // that pattern and is shorter than "Full Turn" was - so the original width concern
                // (a wide fourth label forcing a ~400px minimum onto a column as narrow as 199px, which
                // clipped straight past the column edge) is if anything eased, not reintroduced. No
                // information is lost either way: the day count is stated in full in the sentence
                // directly beneath these buttons.
                case PreviewHorizon.FullTurn: return "1 Year";
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

        /// <summary>Inflation publishes monthly like Unemployment, so it gets the comparison graph treatment rather than the badged-figure one - see PublishedFigure for why cadence decides that.</summary>
        private readonly GraphRenderer _inflationPublishedGraph = new GraphRenderer();

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
        // Spending (29 categories) and tax revenue (13 types) both outgrew the eight-ink categorical
        // cap, so they render as ranked single-ink bar ledgers rather than as pies - see
        // UiPalette.GetCategoricalColor and RankedBarLedgerRenderer. Sector employment (8) sits exactly
        // at the cap and stays a pie.
        private readonly RankedBarLedgerRenderer _spendingAllocationLedger = new RankedBarLedgerRenderer();
        private readonly RankedBarLedgerRenderer _taxRevenueLedger = new RankedBarLedgerRenderer();
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

        /// <summary>Law system MVP slice: the Laws browser's own scroll position and active category filter - navigation state, the same "not captured by UiDraftState" idiom every other scroll position/selected-tab field in this class already follows. Browser rebuild (2026-08-25) adds the status filter and the list+detail split's selection - same idiom, still navigation state, still uncaptured.</summary>
        private Vector2 _lawsScrollPosition;
        /// <summary>Code-review pass (2026-08-25): the detail pane's own scroll position. The pane is
        /// bounded to the list's own height so a law with several dial deltas and a long Citation
        /// cannot push the tab past the height DrawPolicyLawsTab reserves for it - a deliberate,
        /// narrow departure from LAW_BROWSER_BOARD_RULINGS.md's "the detail pane does not scroll":
        /// the scrollbar only appears when content genuinely overflows, so the common case looks
        /// identical to the ruling's intent, and the uncommon case degrades to a scroll instead of a
        /// layout-budget overrun.</summary>
        private Vector2 _lawDetailScrollPosition;
        private LawBrowserFilter _lawBrowserFilter = LawBrowserFilter.All;
        private LawStatusFilter _lawStatusFilter = LawStatusFilter.All;

        /// <summary>Board 1j (2026-08-26): the within-group ORDER control - "the instrument that
        /// actually reduces 40 rows," taking the space the category chips held. Magnitude is the
        /// default and the one that renders AVAILABLE as four weight-class bands.</summary>
        private enum LawOrder { Magnitude, Alphabetical, Cost }
        private LawOrder _lawOrder = LawOrder.Magnitude;
        /// <summary>Board 1j: the statute-book search slot. A plain name-contains filter; always
        /// present (one TextField every frame - stable control layout), and row-count changes it
        /// causes are player-typed, never background state.</summary>
        private string _lawSearchText = string.Empty;

        private string _selectedLawId;
        /// <summary>Code-review pass (2026-08-25): StatTracePanel's own IMGUI-safety idiom - a row
        /// click stages the new selection here instead of writing `_selectedLawId` directly, and the
        /// pending value is only applied during the Layout event (see the top of DrawLawsTab), so
        /// Layout and Repaint always agree within a frame. Selecting a law with a different nonzero-
        /// delta count than the previous one changes how many GUILayoutUtility.GetRect calls the
        /// detail pane makes later in the same pass - applying the change mid-frame is exactly the
        /// control-count mismatch this codebase already found and fixed once, in StatTracePanel.</summary>
        private string _pendingSelectedLawId;
        private bool _hasPendingLawSelection;
        /// <summary>Code-review pass (2026-08-25): reused and Clear()'d every frame instead of
        /// `new List&lt;&gt;()`'d, since the OnGUI Layout/Repaint pair (and every intermediate event)
        /// re-runs DrawLawsTab even when neither the catalog nor any law's enactment state changed
        /// since the previous frame.</summary>
        private readonly List<LawRowEntry> _lawEnactedRows = new List<LawRowEntry>();
        private readonly List<LawRowEntry> _lawPendingRows = new List<LawRowEntry>();
        private readonly List<LawRowEntry> _lawAvailableRows = new List<LawRowEntry>();
        private readonly List<LawRowEntry> _lawVisibleRows = new List<LawRowEntry>();
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

        private bool _stylesInitialized;
        private GUIStyle _headerStyle;
        private GUIStyle _labelStyle;
        private GUIStyle _buttonStyle;
        private GUIStyle _sliderStyle;
        private GUIStyle _sliderThumbStyle;
        private GUIStyle _boxStyle;
        private GUIStyle _tabButtonStyle;
        private GUIStyle _eventBannerStyle;
        /// <summary>v2.0 chrome: `_eventBannerStyle` dressed in the `ui_banner_hold` dark desk plate — the B8 interrupt indicator's own style, built in InitializeStylesIfNeeded and drawn via DrawHoldBannerLabel. Degrades to a plain clone of `_eventBannerStyle` when the sprite is missing.</summary>
        private GUIStyle _holdBannerStyle;
        // v2.0 chrome: the desk calendar (see DrawCalendarPad). The plate style carries only the
        // ui_calendar_pad background and its 9-slice border; the three text styles are the pad's own
        // type — month band, day numeral, year·turn line — scaled beside the pad in
        // RescaleStylesToScreen so furniture and type track together.
        private GUIStyle _calendarPadPlateStyle;
        private GUIStyle _calendarMonthStyle;
        private GUIStyle _calendarDayStyle;
        private GUIStyle _calendarMetaStyle;
        /// <summary>Calendar Panel (the month page - see CLAUDE.md's "Calendar Panel" section for the data contract). Weekday header row and in-grid day numbers - both bold Display type, matching the pad's own month/day styling rather than inventing a third convention for one screen.</summary>
        private GUIStyle _calendarWeekdayStyle;
        private GUIStyle _calendarDayNumberStyle;
        /// <summary>Item 1a: the Division Records panel's mono date column — Courier per §A.4 (dates and timestamps are document artifacts). Built in InitializeStylesIfNeeded, sized in RescaleStylesToScreen.</summary>
        private GUIStyle _divisionMetaStyle;
        /// <summary>v2.0 chrome: `ui_tab_spine` (B7) — the white-on-alpha area-hue strip drawn across each consolidated tab's top edge, tinted per area at the draw site through GUI.color. Background + border only; empty background when the sprite is missing, and the spine simply doesn't draw.</summary>
        private GUIStyle _tabSpineStyle;

        // ── v2.0 folder-tongue pass ──
        /// <summary>Whether all three `ui_tab_folder_*` faces resolved — refreshed every frame in
        /// RescaleStylesToScreen (a cached-dictionary hit, not a load) because everything the pass
        /// touches (row height, bar-to-sheet gap, deferred paint) must branch the SAME way within a
        /// frame.</summary>
        private bool _folderTabsLive;
        /// <summary>The selected tongue's deferred-paint state — written by DrawConsolidatedTabButton
        /// every event, consumed by DrawActiveFolderTongue later the SAME OnGUI pass (see the
        /// paintDeferred comment there for why the tongue cannot paint in bar order).</summary>
        private GUIStyle _activeTongueStyle;
        private Rect _activeTongueRect;
        private string _activeTongueLabel;
        private Texture2D _activeTongueIcon;
        private float _activeTongueIconSize;
        private float _activeTongueIconTop;
        private UiPalette.SystemArea _activeTongueArea;

        // ── SAVE/LOAD UI (item 8's menu pass, 2026-08-16) ──
        /// <summary>While true, OnGUI draws ONLY the saves screen (the codebase's screen-swap idiom -
        /// an IMGUI overlay cannot block events to controls drawn earlier, so exclusivity is how a
        /// modal actually modals here) and Update's day loop holds.</summary>
        private bool _savesMenuOpen;
        private string _saveNameInput = "";
        private List<SaveGameService.SaveHeader> _saveList = new List<SaveGameService.SaveHeader>();
        /// <summary>Path whose Delete button is in its confirm beat - the confirmation is the SAME
        /// button re-labelled, never an extra control (stable-control-layout).</summary>
        private string _confirmDeletePath;
        /// <summary>Path whose Load button awaits the unsaved-progress confirmation.</summary>
        private string _confirmLoadPath;
        /// <summary>Set by the menu's confirmed Load, EXECUTED by Update at its safe point - a load
        /// swaps the world, and doing that mid-OnGUI is the Layout/Repaint corruption class the
        /// canvas seam exists to prevent. F5/F9 already run in Update; the menu defers to the same
        /// safe point.</summary>
        private string _pendingLoadPath;
        private string _savesMenuStatus = "";
        private Vector2 _savesScrollPosition;
        /// <summary>The calendar stamp at the last save, load or new game - "unsaved progress" =
        /// CurrentDate has moved past this. Draft-only changes (sliders dragged, nothing advanced)
        /// deliberately do NOT trip it; stated in the menu record rather than silently true.</summary>
        private System.DateTime _lastPersistenceDate;
        // v2.0 chrome: the Decisions dossier (§A.11) — `ui_folder_dossier` as a card background with
        // its baked tab shoulder, plus the shoulder's own caption style. Empty background = sprite
        // missing = BeginAreaCard falls back to the ordinary procedural area card.
        private GUIStyle _dossierCardStyle;
        private GUIStyle _dossierShoulderStyle;
        /// <summary>Set by BeginAreaCard, consumed by EndAreaCard in the same OnGUI pass — Begin is the single authority on whether the open card is a dossier and what its shoulder reads, so the two halves cannot be told different stories. Cards never nest, so one pair of fields suffices.</summary>
        private bool _openCardDossier;
        private string _openCardKind;
        /// <summary>v2.0 chrome: `ui_portrait_frame` — the brass roster frame drawn over every DrawPersonPortrait through its transparent opening. Empty background = sprite missing = the old unframed draw.</summary>
        private GUIStyle _portraitFrameStyle;
        /// <summary>How far the portrait art is inset inside the frame's rect, so the ~7px bezel (native pixel scale, like all 9-sliced chrome) overlaps the art's edge with no seam at any size.</summary>
        private const float PortraitFrameArtInset = 5f;
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
            // SAVE/LOAD (item 8): the TEMPORARY debug entry point - F5 saves, F9 loads, both logged
            // loudly. Deliberately ahead of the game-over/election gates (a finished game should
            // still be saveable and a load should rescue it) but behind country selection and the
            // Canvas takeover (mid-ceremony state is deliberately not a save point - the queue
            // drains first). The real load/save UI is the NEXT pass, after the round-trip diagnostic
            // is green; this hook exists so a batch-proven system is reachable in play at all.
            // New Input System exclusively (activeInputHandler: 1, see CanvasChrome) - the legacy
            // Input class would throw here. Keyboard.current is null on a machine with no keyboard.
            UnityEngine.InputSystem.Keyboard keyboard = UnityEngine.InputSystem.Keyboard.current;
            if (keyboard != null && _selectedPlayerCountryId.HasValue && !_canvasLive
                && _canvasPhase == CanvasPhase.None && _signingQueue.Count == 0)
            {
                if (keyboard.f5Key.wasPressedThisFrame)
                {
                    DebugSaveGame();
                }
                else if (keyboard.f9Key.wasPressedThisFrame)
                {
                    DebugLoadGame();
                }
            }

            // The saves MENU's confirmed load, executed at the same safe point the F9 path uses -
            // OnGUI only ever queues it (see _pendingLoadPath's own doc for why).
            if (_pendingLoadPath != null)
            {
                string path = _pendingLoadPath;
                _pendingLoadPath = null;
                LoadFromPath(path);
            }

            // The saves screen holds time exactly like the interrupt modals below - browsing saves
            // with days ticking underneath would be the background-mutation class in miniature.
            if (_savesMenuOpen)
            {
                return;
            }

            if (!_selectedPlayerCountryId.HasValue || _isGameOver || _pendingElectionResult != null)
            {
                return;
            }

            // The verdict holds the clock for the same reason the reveal does: the run is over and
            // the player has not read why yet.
            if (_scenarioVerdictPending)
            {
                return;
            }
            // A Canvas takeover stops the clock — the signing-screen pattern addition to the seam's
            // discipline statement. The selector never exposed this (the no-selection gate above
            // already froze time); a mid-game ceremony with days ticking behind it would resolve
            // MORE bills behind the document being signed.
            if (_canvasLive || _canvasPhase != CanvasPhase.None || _signingQueue.Count > 0)
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

                // ⚠ EXTRACTED 2026-08-12 (ruled) — the ten sim-side daily calls that used to sit here
                // verbatim now live in AdvanceCountryDayTick, in the same order, so the capture driver
                // makes the SAME day play makes instead of a copy that drifts (the copy produced three
                // "never captured" findings in one day). The gate reasoning those lines carried, kept
                // where the gates are: the foreign-policy roll and the budget-process date check both
                // need the re-check below (each opens a mandatory pause); the nine bill countdowns
                // never do — a bill resolving is a deterministic countdown, not something awaiting a
                // player response (the idiom the retired TaxBill/AdvanceLegislativeDay established).
                _simulationManager.AdvanceCountryDayTick(PlayerCountryId);

                // The signing trigger — play's own day tick, and only this (see
                // QueueNewlyResolvedDivisions for why the harness never fires ceremonies).
                QueueNewlyResolvedDivisions();

                if (turnBoundaryCrossed)
                {
                    AdvanceTurn();
                }

                // A newly-fired election reveal/Fed-Chair selection/Cabinet decision/foreign policy
                // meeting/budget process (or game over) must stop the clock immediately, not keep
                // draining _daySpeedTimer toward days/turns that can't happen yet - re-check every gate
                // before this same frame's loop continues.
                if (_isGameOver || _pendingElectionResult != null || _signingQueue.Count > 0
                    || UpdateFedChairSelectionState()
                    || _simulationManager.GetPendingCabinetDecisions(PlayerCountryId).Count > 0
                    || _simulationManager.GetPendingForeignPolicyMeeting(PlayerCountryId) != null
                    || _simulationManager.GetPendingBudgetProcess(PlayerCountryId))
                {
                    break;
                }
            }
        }

        // ── SAVE/LOAD (item 8, 2026-08-16): layer 3's capture pair and the debug entry point ──────
        //
        // Built to the mechanism report in CLAUDE.md. The capture/restore pair is the explicit,
        // reviewable surface over this class's ~30 draft fields (the exact inventory the report
        // enumerates at "UiDraftState") - a draft added to this class and not to this pair is an
        // obvious omission HERE, which is the property the shape exists for. ⚠ OPEN VERIFICATION
        // GAP: the UI-draft round trip cannot be exercised by the batch diagnostic (no OnGUI, no
        // keyboard) - it queues behind Editor access beside the folder-tongue hover items, and must
        // not be read as proven until that checklist runs.

        private void DebugSaveGame()
        {
            SaveToPath(SaveGameService.DefaultSlotPath);
        }

        private void DebugLoadGame()
        {
            LoadFromPath(SaveGameService.DefaultSlotPath);
        }

        /// <summary>Writes the running game to <paramref name="path"/>. Returns whether it landed;
        /// the outcome line goes to both the console and the saves menu's status label.</summary>
        private bool SaveToPath(string path)
        {
            try
            {
                SaveGame save = SaveGameService.CreateSaveGame(_simulationManager, _world, PlayerCountryId, CaptureUiDrafts());
                bool overwrote = System.IO.File.Exists(path);
                SaveGameService.SaveToFile(path, save);
                _lastPersistenceDate = _simulationManager.CurrentDate;
                _savesMenuStatus = overwrote
                    ? $"Saved over {System.IO.Path.GetFileNameWithoutExtension(path)} - the previous file is kept as .bak."
                    : $"Saved {System.IO.Path.GetFileNameWithoutExtension(path)}.";
                Debug.Log($"SAVE: wrote {path} (turn {_simulationManager.CurrentTurn}, {_simulationManager.CurrentDate:yyyy-MM-dd}).");
                return true;
            }
            catch (System.Exception e)
            {
                _savesMenuStatus = $"Save FAILED - {e.Message}";
                Debug.LogError($"SAVE: FAILED - {e.Message}");
                return false;
            }
        }

        /// <summary>The one load path - F9 and the menu's confirmed Load both land here, always from
        /// Update's safe point, never from OnGUI.</summary>
        private void LoadFromPath(string path)
        {
            try
            {
                SaveGame save = SaveGameService.LoadFromFile(path);
                RestoreFromSave(save);
                _lastPersistenceDate = _simulationManager.CurrentDate;
                _savesMenuOpen = false;
                _savesMenuStatus = "";
                Debug.Log($"LOAD: restored {path} (turn {_simulationManager.CurrentTurn}, {_simulationManager.CurrentDate:yyyy-MM-dd}) - PAUSED.");
            }
            catch (SaveLoadException e)
            {
                _savesMenuStatus = $"Load refused - {e.Message}";
                Debug.LogError($"LOAD: refused - {e.Message}");
            }
            catch (System.Exception e)
            {
                _savesMenuStatus = $"Load FAILED - {e.Message}";
                Debug.LogError($"LOAD: FAILED - {e.Message}");
            }
        }

        /// <summary>
        /// Adopts a deserialized save wholesale. Hazard 2 lives here: every reference this class
        /// holds INTO the world graph is re-resolved by id from the restored world - never kept from
        /// the old graph, never deserialized separately - or the controller and the simulation would
        /// quietly govern two different worlds.
        /// </summary>
        private void RestoreFromSave(SaveGame save)
        {
            SaveGameService.RestoreInto(_simulationManager, save);
            _world = save.World;
            _selectedPlayerCountryId = save.PlayerCountryId;
            _playerCountry = _world.GetCountry(save.PlayerCountryId);
            RestoreUiDrafts(save.Ui);

            // The preview cache indexes into the OLD world's figures; the signing queue's entries
            // reference the OLD log's records. Both rebuild from live state on their own.
            _hasCachedPreview = false;
            _cachedPreviewTurn = -1;
            _signingQueue.Clear();
            _daySpeedTimer = 0f;
        }

        internal UiDraftState CaptureUiDrafts()
        {
            return new UiDraftState
            {
                TaxRateInputs = new Dictionary<TaxType, float>(_taxRateInputs),
                WelfareGenerosityInputs = new Dictionary<WelfareProgramType, float>(_welfareGenerosityInputs),
                SectorSubsidyInputs = new Dictionary<SectorType, float>(_sectorSubsidyInputs),
                SectorRegulationInputs = new Dictionary<SectorType, float>(_sectorRegulationInputs),
                SectorTaxCreditInputs = new Dictionary<SectorType, float>(_sectorTaxCreditInputs),
                SectorResearchGrantsInputs = new Dictionary<SectorType, float>(_sectorResearchGrantsInputs),
                SectorDeregulationInputs = new Dictionary<SectorType, float>(_sectorDeregulationInputs),
                SpendingLineInputs = new Dictionary<SpendingCategory, float>(_spendingLineInputs),
                PartnerTariffInputs = new Dictionary<CountryId, float>(_partnerTariffInputs),
                SwfExistsDraft = _swfExistsDraft,
                SwfDrawdownPercentInput = _swfDrawdownPercentInput,
                SwfContributionRateInput = _swfContributionRateInput,
                SwfDomesticAllocationInput = _swfDomesticAllocationInput,
                SwfEquitiesWeightInput = _swfEquitiesWeightInput,
                SwfBondsWeightInput = _swfBondsWeightInput,
                SwfInfrastructureWeightInput = _swfInfrastructureWeightInput,
                SwfRealEstateWeightInput = _swfRealEstateWeightInput,
                InterestRateChangeInput = _interestRateChangeInput,
                TariffRateInput = _tariffRateInput,
                MinimumWageInput = _minimumWageInput,
                PaidFamilyLeaveWeeksInput = _paidFamilyLeaveWeeksInput,
                OvertimeRegulationInput = _overtimeRegulationInput,
                RetrainingProgramInput = _retrainingProgramInput,
                PoliceFundingInput = _policeFundingInput,
                SentencingSeverityInput = _sentencingSeverityInput,
                BailReformInput = _bailReformInput,
                DrugPolicyInput = _drugPolicyInput,
                JudicialFundingInput = _judicialFundingInput,
                BorderEnforcementInput = _borderEnforcementInput,
                FamilyPolicyInput = _familyPolicyInput,
                ImmigrationPolicyInput = _immigrationPolicyInput,
                IsGameOver = _isGameOver,
                GameOverReason = _gameOverReason,
                PendingElectionResult = _pendingElectionResult,
                PendingElectionTurn = _pendingElectionTurn,
                Scenario = _scenarioProgress,
                ScenarioVerdictPending = _scenarioVerdictPending,
                GameSpeedValue = (int)_gameSpeed,
                FedChairCandidates = _fedChairCandidates == null ? null : new List<FedChair>(_fedChairCandidates),
                FedChairCandidatesForTurn = _fedChairCandidatesForTurn,
                SeenDivisionNumber = _seenDivisionNumber,
                PrevGdp = _prevGdp,
                LastGrowthPercent = _lastGrowthPercent
            };
        }

        /// <summary>Null-tolerant (a batch-written save has no UI layer): null restores every draft
        /// to its virgin default, which is exactly what a fresh controller holds. A load always
        /// resumes PAUSED regardless of the recorded speed - the player rejoins an unfamiliar
        /// moment, and time running before they have looked at it is the hostile default; the
        /// recorded GameSpeedValue is data for a future load UI, not an instruction.</summary>
        private void RestoreUiDrafts(UiDraftState ui)
        {
            _taxRateInputs.Clear();
            _welfareGenerosityInputs.Clear();
            _sectorSubsidyInputs.Clear();
            _sectorRegulationInputs.Clear();
            _sectorTaxCreditInputs.Clear();
            _sectorResearchGrantsInputs.Clear();
            _sectorDeregulationInputs.Clear();
            _spendingLineInputs.Clear();
            _partnerTariffInputs.Clear();

            if (ui != null)
            {
                CopyDrafts(ui.TaxRateInputs, _taxRateInputs);
                CopyDrafts(ui.WelfareGenerosityInputs, _welfareGenerosityInputs);
                CopyDrafts(ui.SectorSubsidyInputs, _sectorSubsidyInputs);
                CopyDrafts(ui.SectorRegulationInputs, _sectorRegulationInputs);
                CopyDrafts(ui.SectorTaxCreditInputs, _sectorTaxCreditInputs);
                CopyDrafts(ui.SectorResearchGrantsInputs, _sectorResearchGrantsInputs);
                CopyDrafts(ui.SectorDeregulationInputs, _sectorDeregulationInputs);
                CopyDrafts(ui.SpendingLineInputs, _spendingLineInputs);
                CopyDrafts(ui.PartnerTariffInputs, _partnerTariffInputs);
            }

            _swfExistsDraft = ui?.SwfExistsDraft;
            _swfDrawdownPercentInput = ui?.SwfDrawdownPercentInput ?? 0f;
            _swfContributionRateInput = ui?.SwfContributionRateInput;
            _swfDomesticAllocationInput = ui?.SwfDomesticAllocationInput;
            _swfEquitiesWeightInput = ui?.SwfEquitiesWeightInput;
            _swfBondsWeightInput = ui?.SwfBondsWeightInput;
            _swfInfrastructureWeightInput = ui?.SwfInfrastructureWeightInput;
            _swfRealEstateWeightInput = ui?.SwfRealEstateWeightInput;
            _interestRateChangeInput = ui?.InterestRateChangeInput ?? 0f;
            _tariffRateInput = ui?.TariffRateInput;
            _minimumWageInput = ui?.MinimumWageInput;
            _paidFamilyLeaveWeeksInput = ui?.PaidFamilyLeaveWeeksInput;
            _overtimeRegulationInput = ui?.OvertimeRegulationInput;
            _retrainingProgramInput = ui?.RetrainingProgramInput;
            _policeFundingInput = ui?.PoliceFundingInput;
            _sentencingSeverityInput = ui?.SentencingSeverityInput;
            _bailReformInput = ui?.BailReformInput;
            _drugPolicyInput = ui?.DrugPolicyInput;
            _judicialFundingInput = ui?.JudicialFundingInput;
            _borderEnforcementInput = ui?.BorderEnforcementInput;
            _familyPolicyInput = ui?.FamilyPolicyInput;
            _immigrationPolicyInput = ui?.ImmigrationPolicyInput;
            _isGameOver = ui?.IsGameOver ?? false;
            _gameOverReason = ui?.GameOverReason;
            _pendingElectionResult = ui?.PendingElectionResult;
            _pendingElectionTurn = ui?.PendingElectionTurn ?? 0;

            // STEP 3: progress is restored from the save; the DEFINITION is looked up by id, and the
            // cadence multiplier re-derived from it rather than persisted (one source of truth for an
            // authored value). A save naming a scenario this build no longer has is REFUSED loudly -
            // silently resuming it as free play would drop the player's objectives mid-run, which is
            // the silent-gap class the ledger's own persistence ruling exists to prevent.
            _scenarioProgress = ui?.Scenario;
            _scenarioVerdictPending = ui?.ScenarioVerdictPending ?? false;
            _scenario = _scenarioProgress != null ? ScenarioLibrary.ById(_scenarioProgress.ScenarioId) : null;
            if (_scenarioProgress != null && _scenario == null)
            {
                Debug.LogError($"SCENARIO: this save names '{_scenarioProgress.ScenarioId}', which this build does not have - " +
                               "its objectives cannot be judged. The world loaded; the scenario did not.");
                _scenarioProgress = null;
                _scenarioVerdictPending = false;
            }

            _simulationManager.ForeignPolicyCadenceMultiplier = _scenario?.ForeignPolicyCadenceMultiplier ?? 1f;
            _fedChairCandidates = ui?.FedChairCandidates == null ? null : new List<FedChair>(ui.FedChairCandidates);
            _fedChairCandidatesForTurn = ui?.FedChairCandidatesForTurn ?? -1;
            // A save with no UI layer (batch-written) carries no high-water mark; defaulting to 0
            // would replay a ceremony for every division still in the 24-entry log. Everything
            // already in the log counts as seen; only divisions resolved AFTER the load ceremonial.
            _seenDivisionNumber = ui?.SeenDivisionNumber
                ?? (_playerCountry != null && _playerCountry.Divisions.Entries.Count > 0
                    ? _playerCountry.Divisions.Entries[_playerCountry.Divisions.Entries.Count - 1].Number
                    : 0);
            _prevGdp = ui?.PrevGdp ?? 0f;
            _lastGrowthPercent = ui?.LastGrowthPercent ?? 0f;
            _gameSpeed = GameSpeed.Paused;
        }

        private static void CopyDrafts<TKey>(Dictionary<TKey, float> source, Dictionary<TKey, float> target)
        {
            if (source == null)
            {
                return;
            }

            foreach (KeyValuePair<TKey, float> pair in source)
            {
                target[pair.Key] = pair.Value;
            }
        }

        /// <summary>Opens the saves screen: list refreshed once (the menu never polls the disk per
        /// frame), name field pre-filled with a country-and-turn suggestion, stale confirm beats
        /// cleared.</summary>
        private void OpenSavesMenu()
        {
            _savesMenuOpen = true;
            _savesMenuStatus = "";
            _confirmDeletePath = null;
            _confirmLoadPath = null;
            _saveNameInput = $"{_playerCountry.Name}_turn{_simulationManager.CurrentTurn}".Replace(" ", "_");
            RefreshSaveList();
        }

        private void RefreshSaveList()
        {
            _saveList = SaveGameService.ListSaves(SaveGameService.DefaultSaveDirectory);
        }

        /// <summary>
        /// The saves screen (item 8's menu pass): the discoverable path over the batch-proven core -
        /// list, load-with-confirmation, save-as, delete-with-confirmation. Every row renders its
        /// Load and Delete buttons every frame; a confirmation beat is the SAME button re-labelled
        /// (stable-control-layout), and an incompatible save still lists, disabled, with its reason
        /// in the row - a save that vanished from the menu would read as data loss. The row list
        /// itself only changes through this menu's own actions; no background system writes the
        /// saves directory, so the list cannot mutate under a drag.
        /// </summary>
        private void DrawSavesMenuScreen()
        {
            DrawMenuBackground();

            float width = Mathf.Min(Screen.width * 0.62f, 1100f);
            float height = Screen.height * 0.8f;
            var area = new Rect((Screen.width - width) * 0.5f, (Screen.height - height) * 0.5f, width, height);
            GUILayout.BeginArea(area);
            GUILayout.BeginVertical(_boxStyle);

            GUILayout.Label("SAVED GAMES", _headerStyle);
            // One label either way, per the stable-layout idiom the status line downstairs uses.
            GUILayout.Label(string.IsNullOrEmpty(_savesMenuStatus) ? " " : _savesMenuStatus, _labelStyle);

            GUILayout.BeginHorizontal();
            // Paper-idiom field, not Unity's grey default - caught by eye on this screen's first
            // capture (savusa1600_92), the dark-chrome-on-paper cousin of the inversion class.
            _saveNameInput = GUILayout.TextField(_saveNameInput, 48,
                UiPalette.BuildTextFieldStyle(_labelStyle.fontSize), GUILayout.ExpandWidth(true));
            string sanitized = SaveGameService.SanitizeSaveName(_saveNameInput);
            bool couldSave = sanitized.Length > 0;
            GUI.enabled = couldSave;
            if (GUILayout.Button("Save", _implementButtonStyle, GUILayout.Width(width * 0.18f)) && couldSave)
            {
                if (SaveToPath(System.IO.Path.Combine(SaveGameService.DefaultSaveDirectory, sanitized + ".json")))
                {
                    RefreshSaveList();
                }
            }
            GUI.enabled = true;
            GUILayout.EndHorizontal();

            _savesScrollPosition = GUILayout.BeginScrollView(_savesScrollPosition);
            bool anySaves = _saveList.Count > 0;
            if (!anySaves)
            {
                GUILayout.Label("No saved games yet. Name one above and Save, or press F5 in play for a quicksave (slot1).", _labelStyle);
            }

            foreach (SaveGameService.SaveHeader header in _saveList)
            {
                GUILayout.BeginHorizontal();
                string summary = header.Compatible
                    ? $"{header.Name}   -   {DisplayName.Of(header.PlayerCountryId.ToString())}, Turn {header.Turn}, {header.Date:yyyy-MM-dd}   (saved {header.SavedAtUtc:yyyy-MM-dd HH:mm} UTC)"
                    : $"{header.Name}   -   incompatible: {header.Error}";
                GUILayout.Label(summary, _labelStyle, GUILayout.ExpandWidth(true));

                bool confirmingLoad = header.Path == _confirmLoadPath;
                bool dirty = _simulationManager.CurrentDate != _lastPersistenceDate;
                GUI.enabled = header.Compatible;
                if (GUILayout.Button(confirmingLoad ? "Replace unsaved game?" : "Load", _neutralActionButtonStyle, GUILayout.Width(width * 0.2f)))
                {
                    if (confirmingLoad || !dirty)
                    {
                        _pendingLoadPath = header.Path;
                        _confirmLoadPath = null;
                    }
                    else
                    {
                        _confirmLoadPath = header.Path;
                        _confirmDeletePath = null;
                    }
                }
                GUI.enabled = true;

                bool confirmingDelete = header.Path == _confirmDeletePath;
                if (GUILayout.Button(confirmingDelete ? "Really delete?" : "Delete", _removeButtonStyle, GUILayout.Width(width * 0.15f)))
                {
                    if (confirmingDelete)
                    {
                        try
                        {
                            SaveGameService.DeleteSave(header.Path);
                            _savesMenuStatus = $"Deleted {header.Name} (and its .bak backup).";
                        }
                        catch (System.Exception e)
                        {
                            _savesMenuStatus = $"Delete FAILED - {e.Message}";
                        }

                        _confirmDeletePath = null;
                        RefreshSaveList();
                        GUILayout.EndHorizontal();
                        break;
                    }

                    _confirmDeletePath = header.Path;
                    _confirmLoadPath = null;
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.EndScrollView();

            GUILayout.Label("Loading replaces the running game and resumes PAUSED. Saving over a name keeps the previous file as .bak; Delete removes the save AND its .bak. F5 quicksaves to slot1, F9 loads it.", _labelStyle);
            if (GUILayout.Button("Close", _neutralActionButtonStyle))
            {
                _savesMenuOpen = false;
                _confirmDeletePath = null;
                _confirmLoadPath = null;
            }

            GUILayout.EndVertical();
            GUILayout.EndArea();
        }

        // ── CANVAS PILOT (2026-08-12): THE TAKEOVER SEAM ──────────────────────────────────────────
        //
        // The state machine every future Canvas screen shares. Four phases around a live surface:
        //
        //   CoverIn  — the IMGUI scrim fades 0→100% over whatever IMGUI was showing.
        //   (swap)   — at full cover the Canvas screen activates; nothing visible changes, which is
        //              the point: full opacity is what hides the swap. (§A.13 quotes an 85% hold; a
        //              swap behind 85% shows through, so the cover goes to 100% — declared deviation.)
        //   Reveal   — the scrim fades 100→0% OVER the now-active Canvas (possible precisely because
        //              IMGUI is always topmost — the same fact that forces the suppression).
        //   [live]   — phase None with _canvasLive: IMGUI draws only the hold banner (B8 survives the
        //              takeover by construction: the banner is IMGUI, IMGUI is above Canvas).
        //   CoverOut — selection made (detected by the machine, so a reflection-driven
        //              SelectPlayerCountry triggers the exit exactly like a click): scrim covers the
        //              Canvas, screen deactivates behind it.
        //   Restore  — the scrim lifts over the redrawn IMGUI, drawn at OnGUI's end.
        //
        // ⚠ SEAM DEFECT CLASSES, NAMED (the pilot's charter asked what can go wrong here):
        //   1. Layout/Repaint divergence — a phase advancing mid-frame gives the two passes different
        //      control sets. Prevented: phases advance on the Layout event only.
        //   2. Input falling through to suppressed IMGUI — impossible by construction: suppressed
        //      IMGUI emits no controls, and an overlay draw is not a control.
        //   3. The swap showing — prevented by covering to 100%, checked by eye in the entering
        //      capture.
        //   4. State set on Canvas not reaching the sim — prevented by routing selection through the
        //      SAME SelectPlayerCountry the IMGUI path uses; the machine watches the RESULT, not the
        //      caller.
        //   5. The harness racing the envelope — the transitions span ~25 frames against the driver's
        //      4-frame settle; CanvasTransitionSettled is the flag the driver waits on.
        //   6. An early-returning IMGUI takeover during Restore dropping the scrim — named at the
        //      draw site; cannot co-occur with the selector.
        //   7. IMGUI furniture washing the Canvas from above (the desk grain) — skipped during
        //      suppression; any future always-on IMGUI draw must make the same choice explicitly.
        //   8. A THROWING screen builder retrying every frame while corrupting IMGUI's Layout pass —
        //      found by the pilot's first run, not named in advance. Prevented: any build failure,
        //      null OR throw, sets the failed flag exactly once (see the entry case).
        private enum CanvasPhase { None, CoverIn, Reveal, CoverOut, Restore }

        /// <summary>Which Canvas screen the takeover currently owns. One screen at a time by design — the seam is a single boundary, not a window manager.</summary>
        private enum CanvasScreenKind { None, Selector, Signing }

        private CanvasScreenKind _canvasScreenKind = CanvasScreenKind.None;
        private SigningScreen _signingScreen;

        /// <summary>Divisions awaiting their signing ceremony, drained one takeover at a time. Filled ONLY from the controller's own day tick (see QueueNewlyResolvedDivisions) — harness sim-advances never fire ceremonies mid-pass; the driver pins the screen through TriggerSigningForNewestDivision, the same queue the day tick fills.</summary>
        private readonly System.Collections.Generic.Queue<DivisionRecord> _signingQueue = new System.Collections.Generic.Queue<DivisionRecord>();

        /// <summary>High-water mark by DivisionRecord.Number, NOT list count — the log evicts past 24 entries, so a count comparison would miss resolutions once the buffer rolls.</summary>
        private int _seenDivisionNumber;

        private CanvasPhase _canvasPhase = CanvasPhase.None;
        private float _canvasPhaseStart;
        private bool _canvasLive;
        private bool _canvasSelectorFailed;
        private CountrySelectorScreen _countrySelector;

        private const float CanvasCoverSeconds = 0.18f;
        private const float CanvasRevealSeconds = 0.24f;

        /// <summary>True when no takeover transition is in flight — the harness waits on this instead of guessing frame counts (seam defect class 5).</summary>
        public bool CanvasTransitionSettled => _canvasPhase == CanvasPhase.None;

        /// <summary>True while the Canvas surface is the live screen. With <see cref="CanvasTransitionSettled"/>, the pair distinguishes settled-before-entry from settled-canvas-live from settled-after-exit — three states one boolean cannot carry.</summary>
        public bool CanvasSelectorActive => _canvasLive;

        /// <summary>Phase transitions, Layout-event only (seam defect class 1). Time-based rather than frame-based so the envelope reads the same at any frame rate.</summary>
        private void AdvanceCanvasSeam()
        {
            float elapsed = Time.unscaledTime - _canvasPhaseStart;
            switch (_canvasPhase)
            {
                case CanvasPhase.None when !_selectedPlayerCountryId.HasValue && !_canvasLive && !_canvasSelectorFailed:
                    // ⚠ SEAM DEFECT CLASS 8, found by the pilot's own FIRST run rather than named in
                    // advance: a THROWING screen builder is worse than a null one. The throw escaped
                    // this Layout-event call, aborted OnGUI mid-Layout (corrupting the Layout/Repaint
                    // balance the whole machine exists to protect), and — because the failure flag was
                    // only set on a null RETURN — the build retried every frame forever. A builder
                    // failure of ANY kind must fail INTO the degradation path exactly once.
                    try
                    {
                        _countrySelector = CountrySelectorScreen.Build(_world, SelectPlayerCountry,
                            ScenarioLibrary.All, StartScenario);
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogError($"CANVAS: selector build THREW ({e.GetType().Name}: {e.Message}) - IMGUI selector stays the live path.");
                        _countrySelector?.Destroy();
                        _countrySelector = null;
                    }

                    if (_countrySelector == null)
                    {
                        _canvasSelectorFailed = true; // IMGUI selector stays the live path
                        return;
                    }

                    _canvasScreenKind = CanvasScreenKind.Selector;
                    _countrySelector.SetVisible(false);
                    BeginCanvasPhase(CanvasPhase.CoverIn);
                    break;

                // SIGNING entry — the first MID-GAME takeover: CoverIn fades over the live dashboard
                // (see CanvasSeamSuppressesImgui for why CoverIn no longer suppresses). Never fires
                // over another takeover screen, and drops the ceremony per class 8 on any build
                // failure — a dropped ceremony is a silent resolution, which is today's behaviour.
                case CanvasPhase.None when !_canvasLive && _canvasScreenKind == CanvasScreenKind.None
                    && _signingQueue.Count > 0 && _selectedPlayerCountryId.HasValue
                    && _pendingElectionResult == null && !_isGameOver:
                    DivisionRecord signing = _signingQueue.Dequeue();
                    try
                    {
                        _signingScreen = SigningScreen.Build(_playerCountry, signing, SignPendingDivision);
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogError($"CANVAS: signing build THREW ({e.GetType().Name}: {e.Message}) - ceremony dropped, resolution stays silent.");
                        _signingScreen?.Destroy();
                        _signingScreen = null;
                    }

                    if (_signingScreen == null)
                    {
                        return;
                    }

                    _canvasScreenKind = CanvasScreenKind.Signing;
                    _signingScreen.SetVisible(false);
                    BeginCanvasPhase(CanvasPhase.CoverIn);
                    break;

                case CanvasPhase.CoverIn when elapsed >= CanvasCoverSeconds:
                    _canvasLive = true;
                    _countrySelector?.SetVisible(true);
                    _signingScreen?.SetVisible(true);
                    BeginCanvasPhase(CanvasPhase.Reveal);
                    break;

                case CanvasPhase.Reveal when elapsed >= CanvasRevealSeconds:
                    BeginCanvasPhase(CanvasPhase.None);
                    break;

                case CanvasPhase.None when _canvasLive && _canvasScreenKind == CanvasScreenKind.Selector
                    && _selectedPlayerCountryId.HasValue:
                    BeginCanvasPhase(CanvasPhase.CoverOut);
                    break;

                // The signing exits when the seal has settled — watching the RESULT of the SIGN
                // action, so the driver's reflection call exits exactly like a click (the selector's
                // own idiom).
                case CanvasPhase.None when _canvasLive && _canvasScreenKind == CanvasScreenKind.Signing
                    && _signingScreen != null && _signingScreen.Sealed:
                    BeginCanvasPhase(CanvasPhase.CoverOut);
                    break;

                case CanvasPhase.CoverOut when elapsed >= CanvasCoverSeconds:
                    _countrySelector?.Destroy();
                    _countrySelector = null;
                    _signingScreen?.Destroy();
                    _signingScreen = null;
                    _canvasScreenKind = CanvasScreenKind.None;
                    _canvasLive = false;
                    BeginCanvasPhase(CanvasPhase.Restore);
                    break;

                case CanvasPhase.Restore when elapsed >= CanvasRevealSeconds:
                    BeginCanvasPhase(CanvasPhase.None);
                    break;
            }
        }

        private void BeginCanvasPhase(CanvasPhase phase)
        {
            _canvasPhase = phase;
            _canvasPhaseStart = Time.unscaledTime;
        }

        /// <summary>
        /// IMGUI is suppressed only while the Canvas surface is LIVE. ⚠ REFINED for the signing
        /// screen (the first mid-game takeover): CoverIn originally suppressed too, which the
        /// selector masked — behind IT was only the void, so nobody saw the pop. A mid-game entry
        /// suppressing at CoverIn would vanish the dashboard a full cover-fade before the scrim
        /// reached opacity. CoverIn now mirrors Restore exactly: the normal UI draws, and the scrim
        /// overlays it at OnGUI's end. The two edge phases are symmetric; the two inner phases
        /// (Reveal/CoverOut) draw over the live Canvas from the suppressed path.
        /// </summary>
        private bool CanvasSeamSuppressesImgui()
        {
            return _canvasLive;
        }

        /// <summary>The seam's own IMGUI: the hold banner (B8 — above the Canvas by render order) and the scrim at its phase alpha.</summary>
        private void DrawCanvasSeamOverlay()
        {
            if (_selectedPlayerCountryId.HasValue)
            {
                string interrupt = BuildFullScreenInterruptText();
                if (interrupt != null)
                {
                    float marginX = Screen.width * ScreenMarginFraction;
                    float marginY = Screen.height * ScreenMarginFraction;
                    GUILayout.BeginArea(new Rect(marginX, marginY, Screen.width - marginX * 2f, Screen.height - marginY * 2f));
                    DrawHoldBannerLabel(interrupt);
                    GUILayout.EndArea();
                }
            }

            float elapsed = Time.unscaledTime - _canvasPhaseStart;
            float alpha = _canvasPhase switch
            {
                CanvasPhase.Reveal => 1f - Mathf.Clamp01(elapsed / CanvasRevealSeconds),
                CanvasPhase.CoverOut => Mathf.Clamp01(elapsed / CanvasCoverSeconds),
                _ => 0f,
            };

            if (alpha > 0f)
            {
                DrawCanvasScrim(alpha);
            }
        }

        /// <summary>The two EDGE phases — CoverIn rising, Restore falling — drawn over the live IMGUI at OnGUI's very end so the scrim sits above everything. The symmetric halves of the same cover.</summary>
        private void DrawCanvasRestoreScrim()
        {
            float elapsed = Time.unscaledTime - _canvasPhaseStart;
            switch (_canvasPhase)
            {
                case CanvasPhase.CoverIn:
                    DrawCanvasScrim(Mathf.Clamp01(elapsed / CanvasCoverSeconds));
                    break;
                case CanvasPhase.Restore:
                    DrawCanvasScrim(1f - Mathf.Clamp01(elapsed / CanvasRevealSeconds));
                    break;
            }
        }

        /// <summary>
        /// `ui_scrim_takeover`'s call site at last — stretched whole, opacity the only animated
        /// property, exactly as its manifest row specifies. Real-colour art, so the tint is pure
        /// white at the phase alpha (§3.0a). Degrades to a desk-coloured wash when missing — a
        /// takeover without its wash still covers, it just covers plainly.
        /// </summary>
        private void DrawCanvasScrim(float alpha)
        {
            var screen = new Rect(0f, 0f, Screen.width, Screen.height);
            Texture2D scrim = IconLibrary.GetChrome("ui_scrim_takeover");
            if (scrim != null)
            {
                GUI.DrawTexture(screen, scrim, ScaleMode.StretchToFill, true, 0f,
                    new Color(1f, 1f, 1f, alpha), Vector4.zero, Vector4.zero);
            }
            else
            {
                Color desk = PoliSimTheme.DeskDeep;
                GUI.DrawTexture(screen, Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0f,
                    new Color(desk.r, desk.g, desk.b, alpha), Vector4.zero, Vector4.zero);
            }
        }

        /// <summary>
        /// STEP 3: the active scenario and its progress, both null in free play. The definition is
        /// authored data (`ScenarioLibrary`); the progress is what persists.
        /// </summary>
        private ScenarioDefinition _scenario;
        private ScenarioProgress _scenarioProgress;

        /// <summary>True while the verdict screen is the live takeover - the screen-swap idiom the
        /// saves menu and the election reveal already use, because an IMGUI overlay cannot stop
        /// events reaching controls drawn under it.</summary>
        private bool _scenarioVerdictPending;

        /// <summary>
        /// STEP 3's ENTRY SEAM (R-S3f): apply the scenario's deltas to the freshly-built world, then
        /// commit its country through the ONE existing selection path - so the Canvas selector's exit
        /// envelope, the signing high-water mark and the persistence stamp all behave exactly as they
        /// do for free play. Deltas run BEFORE selection commits: no turn has advanced, nothing has
        /// been observed, and the player's first day sees the scenario's world as its starting state.
        /// </summary>
        private void StartScenario(ScenarioDefinition definition)
        {
            if (definition == null)
            {
                return;
            }

            Country country = _world.GetCountry(definition.Country);
            // Step 2's third section (2026-08-25): a scenario's seed delta is the one debt writer
            // outside the daily path and the interrupt layer (Italy writes 165% of GDP straight
            // onto the stock). Observed here as a dated Class B event on the debt ledger - at a
            // fresh game's turn 0 this is the ledger's first touch and it opens at the PRE-seed
            // stock, so the first period's panel shows the seed as the event it is; mid-session
            // (the capture driver's Italy block) the accruing period's audit stays exact instead
            // of failing on an unrecorded writer.
            float debtBeforeDeltas = country.State.GovernmentDebt;
            definition.ApplyDeltas?.Invoke(_world, country);
            DebtLedgerRecorder.RecordEvent(country, _simulationManager.CurrentDate, $"Scenario start: {definition.Name}", debtBeforeDeltas, country.State.GovernmentDebt);

            _scenario = definition;
            _scenarioProgress = ScenarioEvaluator.Begin(definition, _simulationManager.CurrentTurn);
            _scenarioVerdictPending = false;
            _simulationManager.ForeignPolicyCadenceMultiplier = definition.ForeignPolicyCadenceMultiplier;

            Debug.Log($"SCENARIO: '{definition.Name}' started - {definition.Country}, {definition.Objectives.Count} objectives, " +
                      $"ends turn {definition.EndTurn}, FA cadence x{definition.ForeignPolicyCadenceMultiplier:0.##}.");

            SelectPlayerCountry(definition.Country);
        }

        /// <summary>
        /// STEP 3's EVALUATION HOOK (R-S3c): boundary-resident, called from the same post-turn site
        /// `CheckElection` occupies. A resolved verdict raises the verdict screen; the run does not
        /// end until the player dismisses it, exactly as an election loss does not end until the
        /// reveal is dismissed.
        /// </summary>
        private void CheckScenarioObjectives()
        {
            if (_scenario == null || _scenarioProgress == null || _scenarioProgress.Verdict != ScenarioVerdict.Undecided)
            {
                return;
            }

            ScenarioEvaluator.EvaluateAtBoundary(_scenario, _scenarioProgress, _playerCountry, _simulationManager.CurrentTurn);
            if (_scenarioProgress.Verdict != ScenarioVerdict.Undecided)
            {
                _scenarioVerdictPending = true;
                Debug.Log($"SCENARIO: '{_scenario.Name}' resolved {_scenarioProgress.Verdict} at turn {_simulationManager.CurrentTurn} - {_scenarioProgress.VerdictReason}");
            }
        }

        /// <summary>Dismissing the verdict ends the run through the EXISTING game-over path - the
        /// scenario's outcome becomes the reason string, so every downstream gate (Update's day loop,
        /// every `GUI.enabled = !_isGameOver` panel) behaves as it always has.</summary>
        private void DismissScenarioVerdict()
        {
            _scenarioVerdictPending = false;
            if (_scenarioProgress != null && _scenario != null)
            {
                _isGameOver = true;
                _gameOverReason = _scenarioProgress.Verdict == ScenarioVerdict.Won
                    ? $"Scenario complete - {_scenario.Name}: {_scenarioProgress.VerdictReason}"
                    : $"Scenario failed - {_scenario.Name}: {_scenarioProgress.VerdictReason}";
            }
        }

        /// <summary>Commits the player's country choice from DrawCountrySelector - together with <see cref="ResetPlayerCountrySelection"/>, the only two places _selectedPlayerCountryId is ever set.</summary>
        private void SelectPlayerCountry(CountryId countryId)
        {
            _selectedPlayerCountryId = countryId;
            _playerCountry = _world.GetCountry(countryId);
            _prevGdp = _playerCountry.State.GDP;

            // Signing high-water mark starts at the current newest division, so pre-existing history
            // never fires a backlog of ceremonies on selection.
            System.Collections.Generic.List<DivisionRecord> divisions = _playerCountry.Divisions.Entries;
            _seenDivisionNumber = divisions.Count > 0 ? divisions[divisions.Count - 1].Number : 0;

            // A brand-new game starts clean: "unsaved progress" means days advanced past this stamp.
            _lastPersistenceDate = _simulationManager.CurrentDate;
        }

        /// <summary>
        /// The symmetric counterpart to <see cref="SelectPlayerCountry"/> - clears the selection back
        /// to "no country chosen yet", reopening the country selector on the next frame. Real play never
        /// calls this: for an ordinary session, a country choice is a one-time, permanent commitment -
        /// there is still no in-game "change country" feature, and that is unchanged and deliberate.
        ///
        /// This exists for <c>UiScreenshotDriver</c>, which reuses <see cref="SelectPlayerCountry"/> as
        /// a disposable per-run label (<c>-shotcountry=</c>) rather than a real player's permanent
        /// choice - see the country-leak fix: the driver's own exit path now guarantees this runs
        /// before it tears the process down, the same way every other pending-state screen in this game
        /// (the election reveal, a Cabinet decision, a Foreign Policy meeting) clears its own state on
        /// dismissal rather than leaving it stuck.
        /// </summary>
        private void ResetPlayerCountrySelection()
        {
            _selectedPlayerCountryId = null;
            _playerCountry = null;
        }

        /// <summary>
        /// Enqueues every division newer than the high-water mark for its signing ceremony. Called
        /// ONLY from the controller's own day loop — the deliberate trigger pattern: ceremonies fire
        /// from PLAY's day tick, never from harness sim-advances, so capture passes stay clean and
        /// the driver pins the screen through <see cref="TriggerSigningForNewestDivision"/>, which
        /// fills the same queue.
        /// </summary>
        private void QueueNewlyResolvedDivisions()
        {
            foreach (DivisionRecord record in _playerCountry.Divisions.Entries)
            {
                if (record.Number > _seenDivisionNumber)
                {
                    _signingQueue.Enqueue(record);
                    _seenDivisionNumber = record.Number;
                }
            }
        }

        /// <summary>Harness entry: pin the signing screen for the newest division on demand — the same queue the day tick fills, so the driver exercises the REAL path minus only the trigger.</summary>
        private void TriggerSigningForNewestDivision()
        {
            System.Collections.Generic.List<DivisionRecord> divisions = _playerCountry.Divisions.Entries;
            if (divisions.Count > 0)
            {
                _signingQueue.Enqueue(divisions[divisions.Count - 1]);
                _seenDivisionNumber = divisions[divisions.Count - 1].Number;
            }
        }

        /// <summary>The SIGN button's own method (and the driver's, by reflection — one path): starts the seal beat; the seam watches SigningScreen.Sealed to cover out.</summary>
        private void SignPendingDivision()
        {
            _signingScreen?.Sign();
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

            // STEP 3: the scenario strip on the degradation path too - a broken sprite import must
            // not be what decides whether scenarios are reachable.
            foreach (ScenarioDefinition definition in ScenarioLibrary.All)
            {
                if (GUILayout.Button($"Scenario: {definition.Name}", UiPalette.BuildButtonStyle(_buttonStyle, UiPalette.ButtonKind.Primary)))
                {
                    StartScenario(definition);
                }
            }

            GUILayout.Space(12f);

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
        /// <summary>
        /// `ui_grain_tile` across the whole desk, tiled — v2.0 chrome, 2026-08-12.
        ///
        /// <para>The desk itself is `camera.backgroundColor`, not a drawn surface, so the grain is the
        /// first thing GUI puts on it. **Drawn before everything else on purpose**: every panel, plate
        /// and tile that follows covers it, which is exactly what §3.2 requires — *"ui_grain_tile must
        /// never sit under a numeral plate"*, because grain behind a figure that redraws every frame
        /// shimmers. Painting it first and letting the furniture occlude it satisfies that without any
        /// site needing to know about it.</para>
        ///
        /// ⚠ **TILED WITH `DrawTextureWithTexCoords`, WHICH NEEDS WRAP MODE REPEAT — and that was checked
        /// rather than assumed.** `ui_grain_tile.png.meta` carries `wrapU: 0` (Repeat), matching
        /// `menu_pattern_tile`, whose Clamp-instead-of-Repeat defect is the recorded instance of getting
        /// this wrong: Clamp does not fail, it stretches the edge pixel across the screen and reads as a
        /// design choice.
        ///
        /// <para>Drawn untinted. The spec bakes the colour in — *"repeating-linear 93°
        /// rgba(255,235,200,0.018)"* — so the sprite carries both its hue and its 1.8% opacity, and a
        /// tint would multiply an alpha that is already deliberately almost nothing.</para>
        /// </summary>
        private void DrawDeskGrain()
        {
            if (Event.current.type != EventType.Repaint)
            {
                return;
            }

            Texture2D grain = IconLibrary.GetChrome("ui_grain_tile");
            if (grain == null || grain.width <= 0 || grain.height <= 0)
            {
                return;
            }

            GUI.DrawTextureWithTexCoords(
                new Rect(0f, 0f, Screen.width, Screen.height),
                grain,
                new Rect(0f, 0f, Screen.width / (float)grain.width, Screen.height / (float)grain.height));
        }

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

            // ── CANVAS PILOT: THE SEAM ─────────────────────────────────────────────────────────────
            // The takeover machine ticks ONCE per frame, on the Layout event only — advancing a phase
            // between one frame's Layout and Repaint would give the two passes different control
            // sets, which is the documented IMGUI desync trigger the stable-control-layout pattern
            // exists to prevent, arriving through time instead of through state.
            if (Event.current.type == EventType.Layout)
            {
                AdvanceCanvasSeam();
            }

            // While the Canvas surface is active (or being covered/revealed), IMGUI draws ONLY the
            // seam overlay: the scrim and the hold banner. This suppression is not a workaround — the
            // render-order spike measured IMGUI as ALWAYS topmost, so "a Canvas screen is visible
            // exactly when OnGUI suppresses itself" is the architecture's own screen-granularity rule,
            // enforced by the renderer. The desk grain is skipped too: it is desk furniture, and
            // drawing it here would wash the Canvas from above.
            if (CanvasSeamSuppressesImgui())
            {
                DrawCanvasSeamOverlay();
                return;
            }

            DrawDeskGrain();

            if (!_selectedPlayerCountryId.HasValue)
            {
                if (_canvasSelectorFailed)
                {
                    // Degradation path only: the Canvas selector failed to build (missing sprite), so
                    // the IMGUI selector remains the live screen — a broken import costs the new
                    // look, never the ability to start a game.
                    DrawCountrySelector();
                }
                else
                {
                    // CoverIn is in flight over the startup void — the pattern ground under the
                    // rising scrim, never the IMGUI selector (which would flash for a cover-fade).
                    DrawMenuBackground();
                    DrawCanvasRestoreScrim();
                }

                return;
            }

            if (_pendingElectionResult != null)
            {
                DrawElectionResultsScreen(_pendingElectionResult);
                return;
            }

            // STEP 3: the verdict is exclusive for the same reason the reveal is - a screen swap, not
            // an overlay. IMGUI on the bare desk by ruling R-S3c: Canvas 3-of-3 (election night)
            // defines the ceremony grammar at Step 4, and a verdict is information-dense anyway
            // (objectives, margins, epilogue) - the ledger idiom, not the ceremony idiom.
            if (_scenarioVerdictPending && _scenario != null && _scenarioProgress != null)
            {
                DrawScenarioVerdictScreen();
                return;
            }

            // The saves screen is exclusive for the same reason the selector and the reveal are: an
            // IMGUI overlay cannot stop events reaching the controls drawn under it, so a modal here
            // IS a screen swap. Update holds the clock while it is open (its own gate).
            if (_savesMenuOpen)
            {
                DrawSavesMenuScreen();
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
            // ⚠ MEASURED FROM THE STRING THAT WILL BE DRAWN, not assumed from a font size. See
            // CalendarAndSpeedControlsHeight for why the old one-line assumption cut the speed strip.
            bool isTimePaused = hasPendingFedChairSelection || hasPendingCabinetDecisions || hasPendingForeignPolicyMeeting || hasPendingBudgetProcess;
            string timeStatusText = BuildTimeStatusText(hasPendingFedChairSelection, hasPendingCabinetDecisions, hasPendingForeignPolicyMeeting, hasPendingBudgetProcess);
            float calendarAreaHeight = CalendarAndSpeedControlsHeight(timeStatusText, isTimePaused, leftColumnWidth) + sectionSpacing;
            // ⚠ INSTANCE #12, LEFT COLUMN. The scroll view was budgeted against the RAW area height,
            // though it lives inside a `_boxStyle` box whose padding and margin come out of that height
            // first - so the column stood `padding.vertical + margin.vertical` taller than the clip rect
            // containing it, and the overrun landed on whatever was drawn last: the Pause/1x/2x/3x strip,
            // the one control visible from every tab. Floored at zero so a pathological status banner
            // collapses the SCROLL VIEW, which can scroll, rather than pushing the pinned strip off the
            // bottom, which cannot.
            float leftScrollHeight = Mathf.Max(0f, PoliSimWidgets.InnerHeight(areaHeight, _boxStyle) - calendarAreaHeight);

            _leftColumnScrollPosition = GUILayout.BeginScrollView(_leftColumnScrollPosition, GUILayout.Height(leftScrollHeight));
            DrawTopBanner();
            GUILayout.Space(sectionSpacing);
            DrawCalendarPanel();
            GUILayout.Space(sectionSpacing);

            GUI.enabled = !_isGameOver;
            DrawPolicyControls();
            GUI.enabled = true;
            GUILayout.EndScrollView();

            GUILayout.Space(sectionSpacing);

            GUI.enabled = !_isGameOver;
            DrawCalendarAndSpeedControls(timeStatusText, isTimePaused);
            GUI.enabled = true;

            GUILayout.EndVertical();

            GUILayout.Space(columnSpacing);
            }

            GUILayout.BeginVertical(GUILayout.Width(rightColumnWidth));
            DrawConsolidatedTabs(rightColumnWidth);
            // v2.0 folder-tongue pass: with the real faces live there is NO gap — the tongues end flush
            // against the content sheet (§A.7's joined look; the clones zero margin.bottom for the same
            // reason), and the selected tongue is re-painted over the sheet's top keyline after the
            // sheet draws (DrawActiveFolderTongue, below the tab switch). The interim treatment keeps
            // its floating bar.
            float tabPanelGap = _folderTabsLive ? 0f : sectionSpacing * 0.5f;
            if (tabPanelGap > 0f)
            {
                GUILayout.Space(tabPanelGap);
            }

            // Master Sequence step 5e, Phase A: ONE tab row now (7 short-labeled consolidated tabs,
            // see DrawConsolidatedTabs) - replaces the old 5-row reservation entirely.
            float tabRowsHeight = ConsolidatedTabRowHeight();
            // ⚠ INSTANCE #12, RIGHT COLUMN — same defect, same line of reasoning. Every tab wraps its
            // content in a `_boxStyle` box, and that box's padding and margin come out of `areaHeight`
            // before the content gets any. Budgeting against the raw height made the right column
            // overrun its clip rect on every screen where it is the taller of the two.
            float tabContentHeight = PoliSimWidgets.InnerHeight(areaHeight, _boxStyle) - tabRowsHeight - tabPanelGap;
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
                    DrawStatisticsTab(tabContentHeight, rightColumnWidth);
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
                    DrawPoliticsTab(tabContentHeight, rightColumnWidth);
                    break;
            }

            // Folder-tongue pass: the selected tongue paints HERE, after the content sheet, so it sits
            // over the sheet's top keyline — the folder pulled forward. See DrawConsolidatedTabButton's
            // paintDeferred comment for the full reasoning.
            DrawActiveFolderTongue();

            GUILayout.EndVertical();

            GUILayout.EndHorizontal();
            GUILayout.EndArea();

            // CANVAS SEAM, Restore phase: the scrim lifts OVER the just-redrawn IMGUI, so it draws
            // last. ⚠ Named seam risk, not yet bitten: an early-returning takeover screen (the
            // election reveal) during Restore would skip this line and drop the scrim for its frames.
            // No such state can co-occur with the SELECTOR's exit; re-audit when Canvas screen #2
            // can fire mid-game.
            DrawCanvasRestoreScrim();
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

            // ⚠ v2.0 CHROME, 2026-08-12 — B8's carrier gets its own furniture: `ui_banner_hold`, the
            // desk-mounted dark plate, behind the one indicator that must be visible from every tab.
            // The manifest: 256x64 @2x, 9-slice 16/16/16/16 @2x, real colour — so drawn UNTINTED per
            // §3.0a (paper/desk furniture ships in its pixels; tinting would double-apply), and the
            // border is half the manifest's figure because GUIStyle.border is @1x. Text switches from
            // draft amber to TextOnDesk: the amber was cut for a paper ground and sinks into the dark
            // set — the spec's HELD state prints its type in the desk cream and lets the amber lamp
            // (see DrawHoldBannerLabel) carry the urgency. Padding is the spec's 6/10 with the left
            // edge widened per frame by RescaleStylesToScreen to hold the lamp dot. A missing sprite
            // leaves this a plain clone of `_eventBannerStyle` — the pre-chrome amber label, exactly
            // what every other chrome site degrades to.
            _holdBannerStyle = new GUIStyle(_eventBannerStyle);
            Texture2D holdPlate = IconLibrary.GetChrome("ui_banner_hold");
            if (holdPlate != null)
            {
                _holdBannerStyle.normal.background = holdPlate;
                _holdBannerStyle.border = new RectOffset(8, 8, 8, 8);
                _holdBannerStyle.padding = new RectOffset(HoldBannerPadX, HoldBannerPadX, HoldBannerPadY, HoldBannerPadY);
                _holdBannerStyle.normal.textColor = PoliSimTheme.TextOnDesk;
            }

            // ⚠ v2.0 CHROME, 2026-08-12 — the desk calendar (see DrawCalendarPad). The plate is
            // real-colour furniture (drawn untinted, §3.0a), 9-sliced at half the manifest's
            // 18/18/44/22 @2x — the deep top inset is the baked month band above the rule, the bottom
            // the baked drop shadow. The month prints in the spec's own `#9C4238`, which is the same
            // hex as the bad ink BY AUTHORING — referenced literally rather than through
            // PoliSimTheme.Bad, so a future re-tune of the semantic ink cannot silently recolor a
            // calendar that carries no judgment (the draft-amber/Political lesson, pre-empted).
            // The year·turn line is Courier: a date stamp is a document artifact, which is exactly
            // what the mono face is reserved for.
            _calendarPadPlateStyle = new GUIStyle(GUIStyle.none);
            Texture2D calendarPlate = IconLibrary.GetChrome("ui_calendar_pad");
            if (calendarPlate != null)
            {
                _calendarPadPlateStyle.normal.background = calendarPlate;
                _calendarPadPlateStyle.border = new RectOffset(9, 9, 22, 11);
            }

            _calendarMonthStyle = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter, wordWrap = false };
            PoliSimTheme.WithDisplay(_calendarMonthStyle);
            _calendarMonthStyle.normal.textColor = PoliSimTheme.Hex(0x9C4238);
            _calendarDayStyle = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter, wordWrap = false };
            PoliSimTheme.WithDisplay(_calendarDayStyle);
            _calendarDayStyle.normal.textColor = PoliSimTheme.TextPrimary;
            _calendarMetaStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, wordWrap = false };
            if (PoliSimTheme.Document != null) { _calendarMetaStyle.font = PoliSimTheme.Document; }
            _calendarMetaStyle.normal.textColor = PoliSimTheme.TextSecondary;

            // Calendar Panel (month page): the weekday header row is small-caps-style bold Display
            // type, the same face the pad's own month band uses - one calendar, one type convention.
            _calendarWeekdayStyle = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter, wordWrap = false };
            PoliSimTheme.WithDisplay(_calendarWeekdayStyle);
            _calendarWeekdayStyle.normal.textColor = PoliSimTheme.TextSecondary;
            _calendarDayNumberStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.UpperCenter, wordWrap = false };
            PoliSimTheme.WithDisplay(_calendarDayNumberStyle);
            _calendarDayNumberStyle.normal.textColor = PoliSimTheme.TextPrimary;

            _divisionMetaStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleRight, wordWrap = false };
            if (PoliSimTheme.Document != null) { _divisionMetaStyle.font = PoliSimTheme.Document; }
            _divisionMetaStyle.normal.textColor = PoliSimTheme.TextSecondary;

            // ⚠ v2.0 CHROME, 2026-08-12 — B7's spine (see DrawConsolidatedTabButton). WoA, so unlike
            // every real-colour plate above it IS tinted at draw time — through GUI.color, which
            // multiplies the white pixels into the area hue. A style rather than
            // GUI.DrawTexture-with-borders so the 9-slice comes from a RectOffset, whose edge order
            // is unambiguous; the manifest's 14/14/0/0 @2x halves to 7/7/0/0 @1x — rounded ends,
            // full vertical stretch.
            _tabSpineStyle = new GUIStyle(GUIStyle.none);
            Texture2D tabSpine = IconLibrary.GetChrome("ui_tab_spine");
            if (tabSpine != null)
            {
                _tabSpineStyle.normal.background = tabSpine;
                _tabSpineStyle.border = new RectOffset(7, 7, 0, 0);
            }

            // ⚠ v2.0 CHROME, 2026-08-12 — the Decisions dossier (§A.11). Real-colour furniture, drawn
            // untinted (§3.0a); border 14/14/26/15 @1x halves the manifest's 28/28/52/30 @2x. The deep
            // TOP slice is the baked tab shoulder — it sits in the fixed band, so content needs the
            // padding to clear it (top 32); the bottom slice carries the baked drop shadow (bottom
            // padding keeps content off it). Left padding reserves the area spine's width on top of
            // §A.11's 18px, mirroring what BuildCardStyle does for the procedural card. The shoulder
            // caption is a FIXED small size (set here, not in RescaleStylesToScreen): the shoulder
            // band is baked art at native pixel scale — ~13px tall at every resolution — so a caption
            // that scaled with the window would overflow the band it sits in. Ink `#6B6250` is the
            // spec's own shoulder ink, quoted literally like the calendar month's.
            _dossierCardStyle = new GUIStyle(GUIStyle.none);
            Texture2D dossier = IconLibrary.GetChrome("ui_folder_dossier");
            if (dossier != null)
            {
                _dossierCardStyle.normal.background = dossier;
                _dossierCardStyle.border = new RectOffset(14, 14, 26, 15);
                _dossierCardStyle.padding = new RectOffset(18 + AreaCardSpineWidth, 18, 32, 20);
                _dossierCardStyle.margin = new RectOffset(0, 0, 0, 0);
            }

            _dossierShoulderStyle = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold, wordWrap = false, fontSize = 11 };
            PoliSimTheme.WithDisplay(_dossierShoulderStyle);
            _dossierShoulderStyle.normal.textColor = PoliSimTheme.Hex(0x6B6250);

            // ⚠ v2.0 CHROME, 2026-08-12 — the brass roster frame (§A.12's embedded/"rect, roster size"
            // column; the OVAL variant is Canvas-path per the manifest's own "Canvas hero size" note,
            // like the seals and the scrim). Real colour, drawn untinted (§3.0a); border 10/10/10/10
            // @1x halves the manifest's 20/20/20/20 @2x. See DrawPersonPortrait for the draw order.
            _portraitFrameStyle = new GUIStyle(GUIStyle.none);
            Texture2D portraitFrame = IconLibrary.GetChrome("ui_portrait_frame");
            if (portraitFrame != null)
            {
                _portraitFrameStyle.normal.background = portraitFrame;
                _portraitFrameStyle.border = new RectOffset(10, 10, 10, 10);
            }

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

            // v2.0 folder-tongue pass: the one per-frame authority on whether the real §A.7 faces are
            // in play — every branch this pass adds (row height, bar-to-sheet gap, deferred paint,
            // strip insets) reads this field, so a mid-session import failure degrades every site
            // together rather than one at a time.
            _folderTabsLive = IconLibrary.GetChrome("ui_tab_folder_on") != null
                              && IconLibrary.GetChrome("ui_tab_folder_off") != null
                              && IconLibrary.GetChrome("ui_tab_folder_hover") != null;

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
            _holdBannerStyle.fontSize = bannerFontSize;
            if (_holdBannerStyle.normal.background != null)
            {
                // The lamp dot lives inside the plate's left padding, so the reserve tracks the dot's
                // own font-derived size (see HoldBannerLampSize's one-accessor note). Only when the
                // plate loaded - the degraded plain-label form keeps its inherited padding.
                _holdBannerStyle.padding.left = HoldBannerPadX + HoldBannerLampSize() + HoldBannerLampGap;
            }

            // The calendar pad's type, at the board's own ratios to body type (month 8.5 / day 26 /
            // mono 9 beside 12.5 body — §A.6). The pad itself scales from the same labelFontSize base
            // in CalendarPadSize, so type and furniture cannot drift apart.
            _calendarMonthStyle.fontSize = Mathf.Max(9, Mathf.RoundToInt(labelFontSize * (8.5f / 12.5f)));
            _calendarDayStyle.fontSize = Mathf.RoundToInt(labelFontSize * (26f / 12.5f));
            _calendarMetaStyle.fontSize = Mathf.Max(9, Mathf.RoundToInt(labelFontSize * (9f / 12.5f)));
            // Calendar Panel: weekday header a touch smaller than body type (a caption, not content);
            // in-grid day numbers a touch larger, so "has this day passed" reads at a glance.
            _calendarWeekdayStyle.fontSize = Mathf.Max(8, Mathf.RoundToInt(labelFontSize * 0.8f));
            _calendarDayNumberStyle.fontSize = Mathf.Max(9, Mathf.RoundToInt(labelFontSize * 0.95f));
            _divisionMetaStyle.fontSize = Mathf.Max(9, Mathf.RoundToInt(labelFontSize * 0.85f));
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
                // Pass 3 (2026-08-26): the second LawCategory shipped and the browser spans both -
                // the tab-level area goes Neutral (no single system owns the screen; a
                // CrimeJustice-tinted header over a labor law would be the confidently-wrong
                // number this method's own doc warns about). Each row accent, status color and
                // detail kicker carries its own law's category area instead (LawCategoryArea).
                case PolicyLawsCategory.Laws: return UiPalette.SystemArea.Neutral;
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
        private static void DrawColoredLabel(string text, GUIStyle style, Color color, params GUILayoutOption[] options)
        {
            Color previous = style.normal.textColor;
            style.normal.textColor = color;
            GUILayout.Label(text, style, options);
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

        /// <summary>
        /// Calendar Panel — replaces the old dashboard (country header + headline stat-tile grid) in
        /// this slot. See CLAUDE.md's "Calendar Panel" section for the full data contract; in brief:
        ///
        /// The tile grid this displaces is not lost — `DrawHeadlineStatTiles` is the SAME method
        /// Statistics -> Domestic already calls (see that method's own doc comment), so every figure
        /// is still one tab click away, several with a history graph the dashboard never had. What
        /// this slot loses is the "glance from any tab" convenience, for real — that trade is
        /// deliberate, made for a genuinely new capability (a real calendar), not a redundant one.
        /// The one piece of `DrawDashboard`'s content with NO other home anywhere in the UI — the
        /// country-name-plus-year header — is preserved verbatim as this panel's own first line.
        /// `DrawPolicyControls`'s "This Year's Policy" preview is UNTOUCHED and still draws directly
        /// below this panel in the same scroll view — it is deliberately NOT tab-owned (see its own
        /// doc comment), so it stays exactly where a player can see it without tab-hopping.
        /// </summary>
        private void DrawCalendarPanel()
        {
            GUILayout.BeginVertical(_boxStyle);
            GUILayout.Label($"{_playerCountry.Name} - Year {_simulationManager.CurrentTurn}", _headerStyle);

            System.DateTime today = _simulationManager.CurrentDate;
            var monthStart = new System.DateTime(today.Year, today.Month, 1);
            Dictionary<int, List<CalendarMarker>> markers = BuildCalendarMonthMarkers(monthStart, today);

            DrawCalendarMonthGrid(monthStart, today, markers);
            DrawCalendarMonthLedger(monthStart, markers);

            GUILayout.EndVertical();
        }

        /// <summary>One dated item landing on a real calendar day. See CLAUDE.md's "Calendar Panel" data contract for which systems feed this and — just as deliberately — which are excluded.</summary>
        private readonly struct CalendarMarker
        {
            public readonly string Label;
            public readonly UiPalette.SystemArea Area;
            public CalendarMarker(string label, UiPalette.SystemArea area) { Label = label; Area = area; }
        }

        /// <summary>
        /// Every marker landing in <paramref name="monthStart"/>'s month, keyed by day-of-month. Pure
        /// reads against already-computed state (SimulationManager's public Get* API, ReleaseCalendar's
        /// date arithmetic, Country's own lists) — nothing here mutates the sim, so recomputing it
        /// fresh every OnGUI call (the same discipline DrawCalendarPad's own date read already follows)
        /// costs nothing and can never itself move a trajectory.
        ///
        /// ⚠ ONLY the systems named in CLAUDE.md's data contract as DATED (or HISTORY-ONLY within its
        /// stated window) appear here. Cabinet decisions and Foreign Policy meetings are deliberately
        /// absent in BOTH directions — see the contract for why a probability-only roll with no
        /// retained date, before or after it fires, has nothing this method could ever mark.
        /// </summary>
        private Dictionary<int, List<CalendarMarker>> BuildCalendarMonthMarkers(System.DateTime monthStart, System.DateTime today)
        {
            var byDay = new Dictionary<int, List<CalendarMarker>>();

            void Add(System.DateTime date, string label, UiPalette.SystemArea area)
            {
                if (date.Year != monthStart.Year || date.Month != monthStart.Month)
                {
                    return;
                }

                if (!byDay.TryGetValue(date.Day, out List<CalendarMarker> list))
                {
                    list = new List<CalendarMarker>();
                    byDay[date.Day] = list;
                }
                list.Add(new CalendarMarker(label, area));
            }

            // Fiscal year start, the annual budget-process pause, and the sovereign credit rating's
            // scheduled review all land on the SAME real date (confirmed from the code, not assumed —
            // CreditRatingSystem.ReviewIfDue reuses FiscalYearData.GetFiscalYearStart directly). One
            // marker carries both facts rather than two coincident dots for one day.
            (int fyMonth, int fyDay) = FiscalYearData.GetFiscalYearStart(PlayerCountryId);
            if (fyMonth == monthStart.Month)
            {
                Add(new System.DateTime(monthStart.Year, fyMonth, fyDay),
                    "Fiscal year starts - budget process opens; credit rating reviewed",
                    UiPalette.SystemArea.Fiscal);
            }

            // Publication release days: deterministic date arithmetic (ReleaseCalendar), never a
            // probabilistic roll — every day of the displayed month is checked against every tracked
            // stat, the same "ask the real rule, don't approximate a schedule" standard the data
            // contract sets.
            int daysInMonth = System.DateTime.DaysInMonth(monthStart.Year, monthStart.Month);
            foreach (PublishedStat stat in System.Enum.GetValues(typeof(PublishedStat)))
            {
                for (int day = 1; day <= daysInMonth; day++)
                {
                    var candidate = new System.DateTime(monthStart.Year, monthStart.Month, day);
                    if (ReleaseCalendar.IsReleaseDay(_playerCountry, stat, candidate))
                    {
                        Add(candidate, $"{DisplayName.Spaced(stat.ToString())} published", UiPalette.SystemArea.Fiscal);
                    }
                }
            }

            // Pending bill resolution dates: every countdown (DaysRemaining) is exact, never
            // probabilistic — today + DaysRemaining is the real resolution date for as long as the
            // bill stays pending. Cabinet/Foreign-Policy interrupts have no equivalent countdown at
            // all, which is exactly why they're absent below rather than merely unhandled.
            void AddBill(int? daysRemaining, string label)
            {
                if (daysRemaining.HasValue)
                {
                    Add(today.AddDays(daysRemaining.Value), $"{label} bill resolves", UiPalette.SystemArea.Political);
                }
            }
            AddBill(_simulationManager.GetPendingBudgetBill(PlayerCountryId)?.DaysRemaining, "Annual budget");
            foreach (TaxProgramBill bill in _simulationManager.GetPendingTaxProgramBills(PlayerCountryId))
            {
                AddBill(bill.DaysRemaining, $"{DisplayName.Spaced(bill.Type.ToString())} tax");
            }
            foreach (WelfareProgramBill bill in _simulationManager.GetPendingWelfareProgramBills(PlayerCountryId))
            {
                AddBill(bill.DaysRemaining, $"{DisplayName.Spaced(bill.Type.ToString())} welfare");
            }
            AddBill(_simulationManager.GetPendingLaborBill(PlayerCountryId)?.DaysRemaining, "Labor Market");
            AddBill(_simulationManager.GetPendingCrimeJusticeBill(PlayerCountryId)?.DaysRemaining, "Crime & Justice");
            AddBill(_simulationManager.GetPendingSectorBill(PlayerCountryId)?.DaysRemaining, "Economic Sectors");
            AddBill(_simulationManager.GetPendingTradeBill(PlayerCountryId)?.DaysRemaining, "Trade");
            AddBill(_simulationManager.GetPendingSwfDrawdownBill(PlayerCountryId)?.DaysRemaining, "SWF drawdown");

            // Next election: exactly computable (turn number -> real date via the epoch formula, the
            // same one every turn-derived date in this method reuses), never a probabilistic roll.
            // Past elections are deliberately NOT marked - only the most recently resolved one is ever
            // held (transiently, cleared on dismissal), with no persisted log to draw a history from;
            // see CLAUDE.md's data contract and its own cross-reference to the still-open ElectionRecord gap.
            int nextElectionTurn = _simulationManager.CurrentTurn
                - (_simulationManager.CurrentTurn % ElectionSystem.ElectionCycle) + ElectionSystem.ElectionCycle;
            System.DateTime electionDate = SimulationManager.EpochDate.AddDays(nextElectionTurn * (double)SimulationManager.DaysPerTurn);
            Add(electionDate, $"Year {nextElectionTurn} election", UiPalette.SystemArea.Political);

            // Resolved divisions (every bill type, up to the 24 most recent) - real, stored dates,
            // history rather than schedule.
            foreach (DivisionRecord record in _playerCountry.Divisions.Entries)
            {
                Add(record.Date, $"No. {record.Number} - {record.Title} ({(record.Passed ? "Carried" : "Rejected")})",
                    UiPalette.SystemArea.Political);
            }

            // Fired events: EventSystem's own roll is probability-only and never markable in advance
            // (excluded above by simply never being added), but a marker that HAS fired carries a real
            // turn number, and a turn number converts to an exact date via the same epoch formula used
            // throughout this method - "a day that happened is a fact," per the data contract. Bounded
            // to the 6-turn fade window GameController already tracks for the World Map (not persisted
            // across a save/load - see the contract).
            foreach (MapEventMarker marker in _mapEventMarkers)
            {
                if (marker.CountryId != PlayerCountryId)
                {
                    continue;
                }
                System.DateTime eventDate = SimulationManager.EpochDate.AddDays(marker.TurnFired * (double)SimulationManager.DaysPerTurn);
                Add(eventDate, marker.Event.Name, UiPalette.SystemArea.Global);
            }

            return byDay;
        }

        /// <summary>
        /// The month grid itself: a weekday header row (locale-aware — respects the current culture's
        /// FirstDayOfWeek, the same locale-honesty standard this project already applies to date
        /// formatting elsewhere), then one cell per day of the month, blank-padded to align day 1 under
        /// its real weekday. Grid height is DERIVED from the actual day/lead-blank count, never a fixed
        /// guess - the same "measured, not assumed" discipline CalendarAndSpeedControlsHeight documents
        /// for the pad beside it.
        /// </summary>
        private void DrawCalendarMonthGrid(System.DateTime monthStart, System.DateTime today, Dictionary<int, List<CalendarMarker>> markers)
        {
            GUILayout.Space(6f);
            GUILayout.Label(monthStart.ToString("MMMM yyyy", CultureInfo.CurrentCulture).ToUpper(CultureInfo.CurrentCulture), _headerStyle);

            DateTimeFormatInfo dtfi = DateTimeFormatInfo.CurrentInfo;
            System.DayOfWeek firstDow = dtfi.FirstDayOfWeek;

            GUILayout.BeginHorizontal();
            for (int i = 0; i < 7; i++)
            {
                var dow = (System.DayOfWeek)(((int)firstDow + i) % 7);
                GUILayout.Label(dtfi.GetAbbreviatedDayName(dow).ToUpper(CultureInfo.CurrentCulture), _calendarWeekdayStyle, GUILayout.ExpandWidth(true));
            }
            GUILayout.EndHorizontal();

            int daysInMonth = System.DateTime.DaysInMonth(monthStart.Year, monthStart.Month);
            int leadBlanks = ((int)monthStart.DayOfWeek - (int)firstDow + 7) % 7;
            int totalCells = leadBlanks + daysInMonth;
            int rows = Mathf.CeilToInt(totalCells / 7f);

            float scale = Mathf.Clamp(Screen.height / 1080f, 0.6f, 1.5f);
            float cellHeight = CalendarDayCellHeight();
            float gap = 3f * scale;
            Rect gridRect = GUILayoutUtility.GetRect(0f, rows * cellHeight + (rows - 1) * gap, GUILayout.ExpandWidth(true));
            float cellWidth = (gridRect.width - gap * 6f) / 7f;

            for (int cellIndex = 0; cellIndex < totalCells; cellIndex++)
            {
                int day = cellIndex - leadBlanks + 1;
                if (day < 1 || day > daysInMonth)
                {
                    continue; // lead/trail padding - no cell drawn, matching a real wall calendar's blank corners
                }

                int row = cellIndex / 7;
                int col = cellIndex % 7;
                var cellRect = new Rect(gridRect.x + col * (cellWidth + gap), gridRect.y + row * (cellHeight + gap), cellWidth, cellHeight);
                markers.TryGetValue(day, out List<CalendarMarker> dayMarkers);
                DrawCalendarDayCell(cellRect, day, monthStart, today, dayMarkers);
            }
        }

        /// <summary>
        /// The day number's own real rendered height — via <c>CalcHeight</c>, not
        /// <c>lineHeight</c>/<c>fontSize + 4f</c>. ONE ACCESSOR, read by both
        /// <see cref="CalendarDayCellHeight"/> (the grid's own row reserve) and
        /// <see cref="DrawCalendarDayCell"/> (the text rect it actually draws into) — the exact
        /// "reserve and drawing from the same accessor" discipline CalendarPadSize/DrawCalendarPad
        /// already establish for the pad beside this panel.
        ///
        /// ⚠ TWO real measurement bugs found in this one method, both already-catalogued classes in
        /// this project rather than novel ones. (1) The first version used a flat `30f * scale` guess,
        /// unrelated to the style's real size (which scales off `labelFontSize`, a DIFFERENT base) -
        /// caught at 2,004 UiOverflowGuard violations. (2) Switching to
        /// `Mathf.Max(lineHeight, fontSize + 4f)` - LedgerRow.Height's own formula - closed MOST of the
        /// gap (over-by dropped from 10.7 to 1.6) but not all of it: this is CLAUDE.md's own
        /// already-documented "tall class" defect ("a height derived from a font metric that is not
        /// the metric governing rendering") - `lineHeight` excludes the style's vertical PADDING, which
        /// only `CalcHeight` (what `GUI.Label` actually obeys) accounts for. The project's own fix for
        /// that defect was exactly this: measure via `CalcHeight`, not a font-metric formula.
        /// </summary>
        private static readonly GUIContent CalendarDayNumberSample = new GUIContent("99 X");

        private float CalendarDayNumberLineHeight()
        {
            return _calendarDayNumberStyle.CalcHeight(CalendarDayNumberSample, 100f);
        }

        /// <summary>Fixed, not font-derived — the marker dots below the day number are a small graphic indicator, not text, so no style governs their size the way LedgerRow.Height's own font-metric derivation applies to a text row.</summary>
        private const float CalendarDayDotRowHeight = 10f;

        private float CalendarDayCellHeight()
        {
            return CalendarDayNumberLineHeight() + CalendarDayDotRowHeight;
        }

        /// <summary>
        /// One day cell: the number (shrink-never-clip via LedgerRow.Cell, the same never-clipping
        /// primitive every other measured cell in this UI already routes through), an "X" once the day
        /// has passed (per the task's own wording — days marked X, not a glyph this project's fonts
        /// were never asked to carry), a highlighted plate for TODAY, and up to four small dots — one
        /// per marker landing on this day, tinted by that marker's own SystemArea so a fiscal date
        /// reads in fiscal's hue and a division in Political's, the same area-colour convention every
        /// other screen already uses.
        /// </summary>
        private void DrawCalendarDayCell(Rect rect, int day, System.DateTime monthStart, System.DateTime today, List<CalendarMarker> dayMarkers)
        {
            bool isToday = monthStart.Year == today.Year && monthStart.Month == today.Month && day == today.Day;
            bool hasPassed = new System.DateTime(monthStart.Year, monthStart.Month, day) < today.Date;

            if (Event.current.type == EventType.Repaint)
            {
                if (isToday)
                {
                    PoliSimTheme.RoundedCard(rect, PoliSimTheme.AccentWash(UiPalette.SystemArea.Political, 0.35f), PoliSimTheme.HairlineStrong, 3f);
                }
                else
                {
                    PoliSimTheme.Rule(new Rect(rect.x, rect.y + rect.height - 1f, rect.width, 1f), PoliSimTheme.Hairline);
                }
            }

            string dayText = hasPassed && !isToday ? $"{day} X" : day.ToString(CultureInfo.InvariantCulture);
            Color dayInk = isToday ? PoliSimTheme.TextPrimary : (hasPassed ? PoliSimTheme.TextMuted : PoliSimTheme.TextPrimary);
            float dayNumberLine = CalendarDayNumberLineHeight();
            LedgerRow.Cell(new Rect(rect.x, rect.y, rect.width, dayNumberLine), dayText, _calendarDayNumberStyle, dayInk, TextAnchor.UpperCenter);

            if (dayMarkers == null || dayMarkers.Count == 0 || Event.current.type != EventType.Repaint)
            {
                return;
            }

            int dotCount = Mathf.Min(dayMarkers.Count, 4);
            const float dotSize = 5f;
            float totalDotsWidth = dotCount * (dotSize + 2f) - 2f;
            float dotX = rect.x + (rect.width - totalDotsWidth) * 0.5f;
            float dotY = rect.y + dayNumberLine + (CalendarDayDotRowHeight - dotSize) * 0.5f;
            for (int i = 0; i < dotCount; i++)
            {
                PoliSimTheme.Pill(new Rect(dotX + i * (dotSize + 2f), dotY, dotSize, dotSize), UiPalette.GetAreaColor(dayMarkers[i].Area));
            }
        }

        /// <summary>
        /// "This Month," in ledger grammar: one row per marker, date column then label — the same
        /// measured-cell idiom LedgerRow's own Cell helper is built for, so a long bill title shrinks
        /// rather than clips exactly the way every other ledger row in this UI already behaves.
        ///
        /// ⚠ The date column's width is MEASURED against the widest date this month can produce
        /// ("12/31"), not a flat constant — a hardcoded `40f` shipped first here too and, unlike the
        /// day-cell height above, it didn't clip (UiOverflowGuard stayed clean) but it DID wrap at
        /// larger window sizes ("10" over "/1"), caught by eye at 2560px rather than by any guard.
        /// Same lesson, quieter failure: a measurement is only valid at the resolution it was taken.
        /// </summary>
        private void DrawCalendarMonthLedger(System.DateTime monthStart, Dictionary<int, List<CalendarMarker>> markers)
        {
            GUILayout.Space(6f);
            GUILayout.Label("This Month", _headerStyle);

            if (markers.Count == 0)
            {
                GUILayout.Label("Nothing scheduled this month.", _labelStyle);
                return;
            }

            float dateColumnWidth = _labelStyle.CalcSize(new GUIContent("12/31")).x;

            var days = new List<int>(markers.Keys);
            days.Sort();
            foreach (int day in days)
            {
                foreach (CalendarMarker marker in markers[day])
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label($"{monthStart.Month}/{day}", _labelStyle, GUILayout.Width(dateColumnWidth));
                    DrawColoredLabel(marker.Label, _labelStyle, UiPalette.GetAreaColor(marker.Area));
                    GUILayout.EndHorizontal();
                }
            }
        }

        /// <summary>
        /// Master Sequence step 5e, Phase B pilot: the dashboard's headline stats restyled onto
        /// <see cref="PoliSimWidgets.StatTile"/> in a 3-column grid, replacing the old raw
        /// GUILayout.Label two-column list - this was Phase B's actual sprite-pilot target (see
        /// POLISIM_MASTER_ROADMAP.md). Ten tiles now (nine without an independent currency): Step
        /// C4's Credit Rating joined the grid 2026-08-02, beside Debt-to-GDP.
        ///
        /// ⚠ CALENDAR PANEL (see CLAUDE.md): this method's own call from the always-visible left
        /// column was retired when the Calendar Panel replaced that slot - see DrawCalendarPanel's
        /// doc comment for the full reasoning. The ONLY remaining caller is Statistics -> Domestic
        /// (DrawDomesticStatisticsContent), so every tile is still one tab click away and several now
        /// sit beside a history graph the old dashboard never had.
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
            float tileHeight = 0f;   // set from PoliSimWidgets.StatTileHeight once the tiles are known
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
                ("GDP", UiFormat.Money(state.GDP, MoneyUnit.Billions), null, _lastGrowthPercent.ToString("+0.00;-0.00;0", CultureInfo.InvariantCulture) + "%", _lastGrowthPercent >= 0f, UiPalette.SystemArea.Global),
                ("Unemployment", UiFormat.Number(state.Unemployment, 2), "%", null, false, UiPalette.SystemArea.Labor),
                ("Inflation", UiFormat.Number(state.Inflation, 2), "%", null, false, UiPalette.SystemArea.Fiscal),
                ("Approval Rating", UiFormat.Number(state.ApprovalRating, 1), null, null, false, UiPalette.SystemArea.Political),
            };

            if (hasIndependentCurrency)
            {
                tiles.Add(("Currency Strength", UiFormat.Number(state.CurrencyStrength, 1), null, null, false, UiPalette.SystemArea.Trade));
            }

            tiles.Add(("Poverty Rate", UiFormat.Number(state.PovertyRate, 1), "%", null, false, UiPalette.SystemArea.Welfare));
            tiles.Add(("Government Debt", UiFormat.Money(state.GovernmentDebt, MoneyUnit.Billions), null, null, false, UiPalette.SystemArea.Fiscal));
            tiles.Add(("Debt-to-GDP", UiFormat.Number(state.DebtToGdpRatio, 1), "%", null, false, UiPalette.SystemArea.Fiscal));

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

            // ⚠ ASKED, NOT ASSUMED. `92 * scale` was a measurement of a tile WITHOUT a delta, applied to
            // a grid where most tiles have one - so the delta drew past the tile's bottom edge onto the
            // next row's keyline. The grid is uniform, so the question is whether ANY tile carries a
            // delta; sizing every tile for the tallest is what keeps the rows aligned.
            bool anyDelta = false;
            for (int i = 0; i < tiles.Count; i++)
            {
                if (!string.IsNullOrEmpty(tiles[i].delta))
                {
                    anyDelta = true;
                    break;
                }
            }

            tileHeight = PoliSimWidgets.StatTileHeight(scale, anyDelta, hasBar: false);

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
        /// country too - see GetCentralBankName. Since pass 4 (2026-08-26) every branch also draws
        /// the rule's reading - the chair's target for the USA, the blended reading for a Eurozone
        /// member, an advisory reading for Sweden/Poland - one always-drawn Label per branch at a
        /// fixed ordinal (the stable-control-layout pattern; the branches are immutable per country).
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
                // Pass 4 (2026-08-26): the rule's reading and the chair's target, so the mechanism
                // the USA player lives with is visible - one always-drawn Label at a fixed ordinal
                // (the branch is immutable per country), content-only variation.
                float suggested = TaylorRule.GetSuggestedInterestRate(_playerCountry);
                float chairTarget = Mathf.Clamp(suggested + chair.RateBias, CurrencySystem.MinInterestRate, CurrencySystem.MaxInterestRate);
                GUILayout.Label(
                    $"Rule reading {suggested:F2}% (inflation {_playerCountry.State.Inflation:F1}%, unemployment {_playerCountry.State.Unemployment:F1}% against a {_playerCountry.NaturalUnemploymentRate:F1}% structural rate, the NAIRU) plus the chair's lean of {chair.RateBias:+0.00;-0.00;0.00} points = target {chairTarget:F2}% (held within {CurrencySystem.MinInterestRate:F0}-{CurrencySystem.MaxInterestRate:F0}%). The rate moves {FederalReserveSystem.RateAdjustmentSpeed * 100f:F0}% of the way toward the target each turn.",
                    _labelStyle);

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
                    // Playtest-2 item 5, ruled 2026-08-25 (option C): the player-set rate is a
                    // DELIBERATE GAMEPLAY CHOICE, named as such to the player - the Italy-scenario
                    // precedent of shipping a premise as authored text, not an apology. The Eurozone
                    // branch below has carried its own honesty paragraph since the Rate-Voice
                    // mechanic; this branch now carries its counterpart. The recorded destination
                    // (independence with appointment influence - the Fed Chair mechanism
                    // generalized) and its two gates live in the roadmap's Step 4 block and on
                    // PolicyDecision.InterestRateChange's doc.
                    GUILayout.Label(
                        $"The real {GetCentralBankName(PlayerCountryId)} sets its policy rate independently of the government. This game deliberately hands you the central bank, so monetary policy stays a lever you own - a gameplay choice, stated plainly, not a claim about how {_playerCountry.Name}'s institutions work.",
                        _labelStyle);
                    // Pass 4 (2026-08-26): the advisory reading - what an independent central bank
                    // on the same rule would set - Riksbank-B's first visible artefact. One
                    // always-drawn Label ahead of the slider, content-only variation.
                    GUILayout.Label(
                        $"For reference, an independent {GetCentralBankName(PlayerCountryId)} following the same rule the Federal Reserve and the ECB use would read {TaylorRule.GetSuggestedInterestRate(_playerCountry):F2}% right now (inflation {_playerCountry.State.Inflation:F1}%, unemployment {_playerCountry.State.Unemployment:F1}% against a {_playerCountry.NaturalUnemploymentRate:F1}% structural rate, the NAIRU).",
                        _labelStyle);
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
                        $"{_playerCountry.Name} shares the Eurozone's single currency and interest rate with {GetOtherEurozoneMemberNames()}. Each member's own Taylor Rule reading pulls the shared rate toward its own inflation and labour-market situation (its unemployment against its structural rate), weighted by its share of the three countries' combined GDP - a simplified version of the real ECB's \"capital key.\" As {_playerCountry.Name}'s governor you get a modest, bounded push on top of that blend - real influence, not unilateral control, the same way no single member state sets the ECB's rate alone.",
                        _labelStyle);
                    // Pass 4 (2026-08-26): the blend and this member's own reading, so the push has a
                    // visible reference. One always-drawn Label ahead of the slider.
                    GUILayout.Label(
                        $"Blended rule reading this turn: {EurozoneRateSystem.GetBlendedSuggestedRate(_world, _playerCountry):F2}% ({_playerCountry.Name}'s own reading {TaylorRule.GetSuggestedInterestRate(_playerCountry):F2}%, from inflation {_playerCountry.State.Inflation:F1}% and unemployment {_playerCountry.State.Unemployment:F1}% against a {_playerCountry.NaturalUnemploymentRate:F1}% structural rate, the NAIRU).",
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
            // ⚠ v2.0 CHROME, 2026-08-12 — `ui_portrait_frame`, the brass roster frame (§A.12's
            // embedded column). The rect takes the frame's own 74x92 @1x proportion instead of the
            // old square — a portrait frame is portrait-shaped, and ScaleAndCrop has always cropped
            // the art to whatever rect it was given, so no assumption about the art changes. Draw
            // order: art first, inset so the bezel overlaps its edge with no seam, then the frame
            // over it through its transparent opening. Frame missing → the old square unframed draw.
            Texture2D frame = _portraitFrameStyle.normal.background;
            float height = _labelStyle.fontSize * 3.2f;
            float width = frame != null ? Mathf.Round(height * (74f / 92f)) : height;
            Rect rect = GUILayoutUtility.GetRect(width, height, GUILayout.Width(width), GUILayout.Height(height));

            if (Event.current.type != EventType.Repaint)
            {
                return;
            }

            Rect artRect = frame != null
                ? new Rect(rect.x + PortraitFrameArtInset, rect.y + PortraitFrameArtInset,
                    rect.width - PortraitFrameArtInset * 2f, rect.height - PortraitFrameArtInset * 2f)
                : rect;

            if (portrait != null)
            {
                GUI.DrawTexture(artRect, portrait, ScaleMode.ScaleAndCrop, true);
            }
            else
            {
                PoliSimWidgets.Portrait(artRect, area, 1f);
            }

            if (frame != null)
            {
                _portraitFrameStyle.Draw(rect, false, false, false, false);
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
            GUILayout.Label("A new presidential term begins next year - choose the next Fed chair:", _labelStyle);
            foreach (FedChair candidate in _fedChairCandidates)
            {
                DrawFedChairCandidateButton(candidate);
            }
        }

        /// <summary>
        /// Crime &amp; Justice tab (Phase 4 - moved off the dashboard into its own home; converted to a
        /// READ-ONLY summary 2026-08-24, law system MVP slice). The six dials below - Police Funding/
        /// Sentencing Severity/Bail Reform/Drug Policy/Judicial Funding/Border Enforcement - are no
        /// longer player-editable HERE: the standalone CrimeJusticePolicyBill submission this tab used
        /// to offer is retired as a player-facing action (Elias's ruling on "the sliders' fate" - see
        /// CLAUDE.md's law-system section). Going forward these six dials are set exclusively by
        /// enacted law, via the Laws tab (DrawLawsTab) - a slider the player could still move here
        /// WHILE laws also moved the same dial would be the two-books problem again.
        ///
        /// Deliberately NOT a rip-out: CrimeJusticePolicyBill/IntroduceCrimeJusticeBill/
        /// AdvanceCrimeJusticeBillDay/GetPendingCrimeJusticeBill and its save-state field
        /// (SimulationPendingState.PendingCrimeJusticeBills) all stay fully intact in code - only the
        /// player-facing submission UI (the six draft sliders and the "Introduce Crime & Justice
        /// Bill" button) is removed here, per the ruling's own "small, scoped, contained UI change"
        /// framing, not a backend/save-shape change. The six _xInput draft fields, their GetXInput
        /// accessors, and UiDraftState's capture/restore of them are also left untouched - nothing
        /// sets them anymore in real play, so they stay permanently null (harmless dead state, never
        /// read by anything player-visible once this tab stopped writing to them).
        ///
        /// Also still here: CrimeIndex/OrganizedCrimeIndex/CorruptionIndex (a clear direction - lower
        /// is better for all three) and PrisonPopulationRate (deliberately neutral - see
        /// PrisonPopulationRate's own doc comment on BailReformLevel/DrugPolicyLevel's honestly-
        /// contested effects) history graphs, unaffected by this conversion.
        /// </summary>
        private void DrawCrimeJusticeTab(float availableHeight)
        {
            GUILayout.BeginVertical(_boxStyle);

            float scrollHeight = availableHeight - _labelStyle.fontSize * 2f;
            _crimeJusticeScrollPosition = GUILayout.BeginScrollView(_crimeJusticeScrollPosition, GUILayout.Height(scrollHeight));

            DrawColoredLabel("Crime & Justice", _headerStyle, UiPalette.GetAreaColor(UiPalette.SystemArea.CrimeJustice));
            GUILayout.Label("These six dials are now set exclusively by enacted law - see the Laws tab to enact or repeal one. The standalone Crime & Justice bill is retired as a player-facing action.", _labelStyle);
            GUILayout.Space(8f);

            // Annual cadence, so a bulletin rather than a chart - see PublishedFigure.
            PublishedFigure.Draw("Crime index as published",
                _playerCountry.Published.Series.TryGetValue(PublishedStat.CrimeIndex, out PublishedSeries crimePublished) ? crimePublished : null,
                _labelStyle, moneyUnit: null);
            GUILayout.Space(8f);

            Color crimeInk = UiPalette.GetAreaColor(UiPalette.SystemArea.CrimeJustice);
            DrawDerivedStatRow("Police Funding", (_playerCountry.PoliceFundingLevel - MinPolicyDialLevel) / (MaxPolicyDialLevel - MinPolicyDialLevel),
                _playerCountry.PoliceFundingLevel.ToString("F0", CultureInfo.InvariantCulture), null, crimeInk);
            DrawDerivedStatRow("Sentencing Severity", (_playerCountry.SentencingSeverity - MinPolicyDialLevel) / (MaxPolicyDialLevel - MinPolicyDialLevel),
                _playerCountry.SentencingSeverity.ToString("F0", CultureInfo.InvariantCulture), "0 lenient - 100 harsh", crimeInk);
            DrawDerivedStatRow("Bail Reform", (_playerCountry.BailReformLevel - MinPolicyDialLevel) / (MaxPolicyDialLevel - MinPolicyDialLevel),
                _playerCountry.BailReformLevel.ToString("F0", CultureInfo.InvariantCulture), "0 cash bail - 100 reformed", crimeInk);
            DrawDerivedStatRow("Drug Policy", (_playerCountry.DrugPolicyLevel - MinPolicyDialLevel) / (MaxPolicyDialLevel - MinPolicyDialLevel),
                _playerCountry.DrugPolicyLevel.ToString("F0", CultureInfo.InvariantCulture), "0 decriminalized - 100 strict", crimeInk);
            DrawDerivedStatRow("Judicial Funding", (_playerCountry.JudicialFundingLevel - MinPolicyDialLevel) / (MaxPolicyDialLevel - MinPolicyDialLevel),
                _playerCountry.JudicialFundingLevel.ToString("F0", CultureInfo.InvariantCulture), null, crimeInk);
            DrawDerivedStatRow("Border Enforcement", (_playerCountry.BorderEnforcementLevel - MinPolicyDialLevel) / (MaxPolicyDialLevel - MinPolicyDialLevel),
                _playerCountry.BorderEnforcementLevel.ToString("F0", CultureInfo.InvariantCulture), "0 open - 100 strict", crimeInk);

            GUILayout.Space(10f);
            _crimeIndexGraph.Draw("Crime Index", _playerCountry.History.CrimeIndex.Quarterly, null, _labelStyle, higherIsBetter: false, moneyUnit: null);
            _organizedCrimeGraph.Draw("Organized Crime Index", _playerCountry.History.OrganizedCrimeIndex.Quarterly, null, _labelStyle, higherIsBetter: false, moneyUnit: null);
            _corruptionGraph.Draw("Corruption Index", _playerCountry.History.CorruptionIndex.Quarterly, null, _labelStyle, higherIsBetter: false, moneyUnit: null);
            _prisonPopulationGraph.DrawNeutral("Incarceration Rate per 100k", _playerCountry.History.PrisonPopulationRate.Quarterly, null, _labelStyle, moneyUnit: null);

            GUILayout.EndScrollView();
            GUILayout.EndVertical();
        }

        /// <summary>
        /// One Policy/Laws dial as a ledger row - the shared shape behind Labor Market, Crime & Justice,
        /// Economic Sectors and Trade.
        ///
        /// **All four sub-screens were the same two lines repeated**: a `DrawDraftLabel` naming the
        /// standing and draft values plus an explanatory parenthetical, then a bare
        /// `GUILayout.HorizontalSlider`. That is precisely what `LedgerRow` collapses - the standing
        /// value becomes a tick, the draft becomes the knob, and the span between them hatches amber - so
        /// this helper exists so each sub-screen's dials become one line each rather than four
        /// near-identical conversions.
        ///
        /// <para><b>The parenthetical goes to the trailing column, not into the name.</b> Those hints
        /// ("0 = light-touch, 100 = heavily regulated") are what the dial's endpoints MEAN, which is the
        /// same question the trailing column answers everywhere else - estimated revenue on Tax, share of
        /// GDP on Spending, normalised share on SWF. A dial's context is its scale.</para>
        ///
        /// Emits exactly one control, always, enabled or not.
        /// </summary>
        private float DrawDialRow(string name, float standing, float draft, float min, float max,
            string format, string suffix, string trailing, bool interactive = true)
        {
            Rect rowRect = GUILayoutUtility.GetRect(10f, LedgerRow.Height(_labelStyle), GUILayout.ExpandWidth(true));
            bool changed = interactive && !Mathf.Approximately(standing, draft);
            return LedgerRow.Draw(
                rowRect, name, standing, draft, min, max,
                interactive ? standing.ToString(format, CultureInfo.InvariantCulture) + suffix : "n/a",
                changed ? draft.ToString(format, CultureInfo.InvariantCulture) + suffix : null,
                trailing, interactive,
                _labelStyle, _labelStyle, _sliderStyle, _sliderThumbStyle);
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
            GUILayout.Label("Master Sequence step 5d: every dial below is a DRAFT - nothing happens until you introduce them as one standalone bill, which resolves independently of the annual budget cycle. Labor LAWS (the Laws tab) stack their own offsets on top of the statutory base these sliders set - a row's note names the law effect when one is moving its dial.", _labelStyle);
            GUILayout.Space(8f);

            BeginAreaCard("LABOR MARKET BILL", UiPalette.SystemArea.Labor);
            DrawLaborBillStatusAndIntroduce();
            DrawLaborLiveEstimate();
            EndAreaCard(UiPalette.SystemArea.Labor);

            DrawMinimumWageControl();

            // Pass 3 (coexistence ruling): the sliders show and edit the STATUTORY BASE - the
            // book bills own - while the trailing column names the law offset and the composed
            // effective value whenever enacted labor laws are moving a dial (LaborDialTrailing).
            // Drafts fall back to the base too, so introducing an untouched bill is Neutral even
            // with laws in force.
            _paidFamilyLeaveWeeksInput = DrawDialRow("Paid Family Leave",
                _playerCountry.PaidFamilyLeaveWeeksBase, GetPaidFamilyLeaveWeeksInput(_playerCountry.PaidFamilyLeaveWeeksBase),
                MinPaidFamilyLeaveWeeks, MaxPaidFamilyLeaveWeeks, "F0", string.Empty,
                LaborDialTrailing("weeks", _playerCountry.PaidFamilyLeaveWeeksBase, _playerCountry.PaidFamilyLeaveWeeks));

            _overtimeRegulationInput = DrawDialRow("Overtime / Working-Hour Regulation",
                _playerCountry.OvertimeRegulationBase, GetOvertimeRegulationInput(_playerCountry.OvertimeRegulationBase),
                MinLaborDialLevel, MaxLaborDialLevel, "F0", string.Empty,
                LaborDialTrailing("0 unregulated - 100 strict", _playerCountry.OvertimeRegulationBase, _playerCountry.OvertimeRegulationLevel));

            _retrainingProgramInput = DrawDialRow("Workforce Retraining Programs",
                _playerCountry.RetrainingProgramBase, GetRetrainingProgramInput(_playerCountry.RetrainingProgramBase),
                MinLaborDialLevel, MaxLaborDialLevel, "F0", string.Empty,
                LaborDialTrailing(null, _playerCountry.RetrainingProgramBase, _playerCountry.RetrainingProgramLevel));

            GUILayout.Space(8f);
            _familyPolicyInput = DrawDialRow("Family Policy",
                _playerCountry.FamilyPolicyBase, GetFamilyPolicyInput(_playerCountry.FamilyPolicyBase),
                MinPolicyDialLevel, MaxPolicyDialLevel, "F0", string.Empty,
                LaborDialTrailing("0 minimal - 100 pro-natalist", _playerCountry.FamilyPolicyBase, _playerCountry.FamilyPolicyLevel));

            _immigrationPolicyInput = DrawDialRow("Immigration Policy",
                _playerCountry.ImmigrationPolicyBase, GetImmigrationPolicyInput(_playerCountry.ImmigrationPolicyBase),
                MinPolicyDialLevel, MaxPolicyDialLevel, "F0", string.Empty,
                LaborDialTrailing("0 restrictive - 100 open", _playerCountry.ImmigrationPolicyBase, _playerCountry.ImmigrationPolicyLevel));

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
        private void DrawBillLiveEstimate(float direction, float wrapWidth = 0f)
        {
            // Unity's Mathf.Sign(0f) returns 1, not 0, so an unchanged draft would otherwise be scored as
            // parliament's raw net stance - negative in the documented tied-parties case - and contradict
            // the WOULD PASS verdict printed directly above it. WouldBillPass short-circuits on exactly
            // this condition, so the bar must too.
            bool contested = !Mathf.Approximately(direction, 0f);
            bool wouldPass = ParliamentSystem.WouldBillPass(_playerCountry, direction);

            // Free-aspect pass (2026-08-26): callers inside a width-bounded pane pass wrapWidth so
            // these labels WRAP there instead of requesting natural width and stretching the pane's
            // scroll content past its viewport (the intro-label class; the laws detail pane at the
            // 1280x720 floor is the measured case). Zero keeps the four policy-screen callers'
            // existing natural-width behavior byte-for-byte.
            string directionLabel = !contested ? "Neutral" : direction > 0f ? "Expansionary" : "Contractionary";
            if (wrapWidth > 0f)
            {
                GUILayout.Label($"Bill direction: {directionLabel} ({direction:+0.0;-0.0;0})", _labelStyle, GUILayout.Width(wrapWidth));
                DrawColoredLabel(wouldPass ? "Current seat composition: WOULD PASS" : "Current seat composition: WOULD FAIL",
                    _labelStyle, UiPalette.GetDeltaColor(wouldPass ? 1f : -1f, higherIsBetter: true), GUILayout.Width(wrapWidth));
            }
            else
            {
                GUILayout.Label($"Bill direction: {directionLabel} ({direction:+0.0;-0.0;0})", _labelStyle);
                DrawColoredLabel(wouldPass ? "Current seat composition: WOULD PASS" : "Current seat composition: WOULD FAIL",
                    _labelStyle, UiPalette.GetDeltaColor(wouldPass ? 1f : -1f, higherIsBetter: true));
            }

            Rect barRect = GUILayoutUtility.GetRect(10f, _labelStyle.fontSize * 0.7f, GUILayout.ExpandWidth(true));
            if (Event.current.type == EventType.Repaint)
            {
                UiPalette.DrawDivergingBar(barRect, contested ? ParliamentSystem.GetSeatWeightedAlignment(_playerCountry, direction) : 0f, PendingBillLeanDisplayRange);
            }
        }

        /// <summary>See BuildCrimeJusticeBillFromDrafts's own doc comment - identical pattern.
        /// Pass 3: fallbacks are the STATUTORY BASE fields (the book this bill writes), not the
        /// law-composed effective dials - an untouched draft restates the base and scores Neutral,
        /// exactly matching GetLaborBillDirection's own base-vs-bill arithmetic.</summary>
        private LaborPolicyBill BuildLaborBillFromDrafts()
        {
            return new LaborPolicyBill
            {
                MinimumWage = GetMinimumWageInput(_playerCountry.MinimumWagePercentOfMedianBase),
                PaidFamilyLeaveWeeks = GetPaidFamilyLeaveWeeksInput(_playerCountry.PaidFamilyLeaveWeeksBase),
                OvertimeRegulation = GetOvertimeRegulationInput(_playerCountry.OvertimeRegulationBase),
                RetrainingProgram = GetRetrainingProgramInput(_playerCountry.RetrainingProgramBase),
                FamilyPolicy = GetFamilyPolicyInput(_playerCountry.FamilyPolicyBase),
                ImmigrationPolicy = GetImmigrationPolicyInput(_playerCountry.ImmigrationPolicyBase)
            };
        }

        /// <summary>The Labor tab's two-books note (pass 3, coexistence ruling 2026-08-26): a
        /// dial row's trailing column gains "laws +N -> M in effect" whenever enacted labor laws
        /// offset that dial away from its statutory base - the coexistence made LEGIBLE per row
        /// rather than hidden. Pure string content on an always-drawn label; control count never
        /// moves (behaviour 5).</summary>
        private static string LaborDialTrailing(string baseTrailing, float baseValue, float effectiveValue)
        {
            if (Mathf.Abs(effectiveValue - baseValue) < 0.05f)
            {
                return baseTrailing;
            }

            string annotation = $"laws {effectiveValue - baseValue:+0.0;-0.0} -> {effectiveValue:F0} in effect";
            return string.IsNullOrEmpty(baseTrailing) ? annotation : baseTrailing + " - " + annotation;
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
            // ⚠ BEHAVIOUR 5 FIX, found during the v2.0 conversion rather than by a capture. This used to
            // `return` early for a country with no statutory minimum wage (Sweden, which bargains
            // collectively), drawing a sentence and NO SLIDER - an omitted control, which is exactly what
            // behaviour 5 forbids and exactly the hazard DrawTaxPolicyContent's doc comment describes:
            // GUILayout allocates control IDs positionally, so a screen whose control COUNT depends on
            // mutable state can desync a live drag. It is now always drawn, disabled when there is no
            // statutory wage, with the reason in the column that explains a dial's meaning.
            bool hasStatutoryWage = _playerCountry.MinimumWageImplemented;
            float newMinimumWage = DrawDialRow("Minimum Wage",
                _playerCountry.MinimumWagePercentOfMedianBase,
                GetMinimumWageInput(_playerCountry.MinimumWagePercentOfMedianBase),
                MinMinimumWagePercent, MaxMinimumWagePercent, "F0", "%",
                hasStatutoryWage
                    ? LaborDialTrailing("% of median wage", _playerCountry.MinimumWagePercentOfMedianBase, _playerCountry.MinimumWagePercentOfMedian)
                    : "none - collective bargaining",
                hasStatutoryWage);

            if (hasStatutoryWage)
            {
                _minimumWageInput = newMinimumWage;
            }
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

            // READ-ONLY rows, and deliberately not disabled sliders. Condition Index is an OUTPUT of the
            // Infrastructure spending category, not a dial - there is nothing to drag under any
            // circumstances - so LedgerRow.DrawReadOnly emits no control at all. A disabled slider is the
            // right answer where a player COULD change a value but currently cannot (behaviour 5); here
            // it would add a control this screen has never had and misstate what the player can do.
            foreach (InfrastructureAsset asset in _playerCountry.InfrastructureAssets)
            {
                Rect rowRect = GUILayoutUtility.GetRect(10f, LedgerRow.Height(_labelStyle), GUILayout.ExpandWidth(true));
                LedgerRow.DrawReadOnly(
                    rowRect,
                    DisplayName.Of(asset.Type.ToString()),
                    asset.ConditionIndex / 100f,
                    asset.ConditionIndex.ToString("F0", CultureInfo.InvariantCulture) + " / 100",
                    null,
                    UiPalette.GetAreaColor(UiPalette.SystemArea.Infrastructure),
                    _labelStyle,
                    _labelStyle);
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
            GUILayout.Label("This Year's Policy", _headerStyle);
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
            GUILayout.Label($"Over the next {GetHorizonLabel(_previewHorizon)} (±5-10% margin of error) - a linear/compounding-scaled display estimate from the full {SimulationManager.DaysPerTurn}-day projection, not a simulated sub-year value. Projection only, not a guarantee.", _labelStyle);

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
        /// _holdBannerStyle (banner weight on the ui_banner_hold desk plate - originally
        /// _eventBannerStyle's bare bold/orange, dressed by the v2.0 chrome pass of 2026-08-12)
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
        /// <summary>
        /// ⚠ **THE STRING IS BUILT BY THE CALLER NOW, and that is a layout fix rather than a tidy-up.**
        /// Every property the comment above describes — escalating to the larger `_holdBannerStyle`, and
        /// naming every pending reason at once — makes this line TALLER, and the height had to be
        /// reserved before the line could be drawn. Splitting the build out lets
        /// <see cref="CalendarAndSpeedControlsHeight"/> measure the exact string that will be drawn,
        /// rather than a guess about how tall it might be.
        /// </summary>
        private string BuildTimeStatusText(bool hasPendingFedChairSelection, bool hasPendingCabinetDecisions,
            bool hasPendingForeignPolicyMeeting, bool hasPendingBudgetProcess)
        {
            // v2.0 chrome (2026-08-12): the date is no longer this string's prefix — the calendar pad
            // beside it (DrawCalendarPad) is the date's carrier now, in both states. The running form
            // becomes a quiet state readout; the paused form keeps every reason, which is the part
            // that was ever load-bearing.
            bool isPaused = hasPendingFedChairSelection || hasPendingCabinetDecisions || hasPendingForeignPolicyMeeting || hasPendingBudgetProcess;
            if (!isPaused)
            {
                return "Clock running";
            }

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

            return $"TIME PAUSED: {string.Join("; ", reasons)} to continue.";
        }

        /// <summary>
        /// How tall this block will be — the status label at the width it will actually wrap into, plus
        /// the speed-button row, plus the margins GUILayout puts around each.
        ///
        /// ⚠ **THE RESERVE IS MEASURED, NOT ASSUMED, and the assumption is what cut the speed strip in
        /// half.** The old figure was `_labelStyle.fontSize + 8f + _buttonStyle.fixedHeight` — one line of
        /// body type plus a button — but the status line is neither of those things when the clock is
        /// paused: it escalates to the larger `_holdBannerStyle` AND names every pending reason at once,
        /// so it wraps. Two wrapped banner lines against a one-line reserve pushed the Pause/1x/2x/3x row
        /// off the bottom — the single control this UI can least afford to lose, being the only one
        /// visible from every tab.
        ///
        /// <para>This walks the same two controls the drawing walks, from the same string, so the two
        /// cannot disagree — the separate-accessor discipline <see cref="UiContainmentGuard"/> documents
        /// for <c>StatTile</c>, applied to a column instead of a tile.</para>
        ///
        /// <para><paramref name="columnWidth"/> is the left column's BUDGET, so the label's own width is
        /// the <see cref="PoliSimWidgets.InnerWidth"/> of it. Measuring at the wrong width is the quiet
        /// way to get this wrong again: too wide under-counts the lines, and under-counting lines is what
        /// put the buttons off the screen in the first place.</para>
        /// </summary>
        /// <summary>ui_banner_hold plate padding — the spec's `6/10` (vertical/horizontal), plus the lamp's clearance gap. The left padding actually applied each frame is PadX + lamp + gap (see RescaleStylesToScreen), so the type never sits under the dot.</summary>
        private const int HoldBannerPadX = 10;
        private const int HoldBannerPadY = 6;
        private const int HoldBannerLampGap = 6;

        /// <summary>Clearance between the calendar pad and the status line beside it.</summary>
        private const float CalendarPadGap = 12f;

        /// <summary>
        /// Calendar pad geometry — the board's own proportion (a 64px pad beside 12.5px body type,
        /// §A.6) applied to the label size this UI actually renders at, then the sprite's native
        /// 72×80 @1x aspect for the height. ⚠ ONE ACCESSOR, READ BY BOTH SITES:
        /// <see cref="CalendarAndSpeedControlsHeight"/> reserves against it and
        /// <see cref="DrawCalendarPad"/> draws at it — the separation that drifts in silence when
        /// either side keeps its own copy.
        /// </summary>
        private Vector2 CalendarPadSize()
        {
            float width = Mathf.Round(_labelStyle.fontSize * (64f / 12.5f));
            return new Vector2(width, Mathf.Round(width * (80f / 72f)));
        }

        /// <summary>
        /// v2.0 chrome: the desk calendar — `ui_calendar_pad` with the date drawn on it, which is now
        /// the date's home (BuildTimeStatusText no longer carries it as a prefix). Month in the band
        /// above the sprite's baked rule, day numeral in the body, year in Courier beneath it.
        /// The text rects are laid out from the sprite's own proportions (rule closing the top 22/80
        /// of the height, baked shadow below 69/80) so the type tracks the furniture at any scale.
        ///
        /// TURN->YEAR RULING (non-trivial #1 of 2): this used to append " · T{turn}" after the year -
        /// harmless while a turn was ~121 days and the two numbers meant different things, but now that
        /// a turn IS a year, `CurrentTurn` and `date.Year - SeedEpochYear` are the same count wearing
        /// two labels. The ruling, since the package this pass swept from didn't settle it: show the
        /// REAL calendar year everywhere and drop the elapsed-turn suffix here specifically, rather than
        /// relabel it " · Y{turn}" - two numbers that agree would be redundant, and two that read as
        /// disagreeing (elapsed count vs. absolute year, offset by the seed epoch) would be worse.
        /// Absolute years are what this pad already had on hand, they're the more evocative choice for
        /// a document-styled desk UI, and the Calendar Panel (month pages, not just this pad -
        /// DrawCalendarPanel, in the scroll view above this pinned strip) inherits the same epoch
        /// rather than a second one - see CLAUDE.md. This pad and that panel are deliberately BOTH
        /// kept: the panel is the detailed view, scrollable and therefore scrollable-away-from; this
        /// pad stays pinned specifically so today's date is never more than a glance away regardless
        /// of scroll position, the same reasoning that already keeps the speed controls pinned beside it.
        /// Degrades to a procedural plate when the sprite is missing — the date must never vanish
        /// with the art. The plate draw is Repaint-gated because a GUIStyle.Draw is a paint call, not
        /// a control: layout still reserves the rect every frame, so the control count never changes.
        /// </summary>
        private void DrawCalendarPad()
        {
            Vector2 size = CalendarPadSize();
            Rect pad = GUILayoutUtility.GetRect(size.x, size.y, GUILayout.Width(size.x), GUILayout.Height(size.y));

            if (_calendarPadPlateStyle.normal.background != null)
            {
                if (Event.current.type == EventType.Repaint)
                {
                    _calendarPadPlateStyle.Draw(pad, false, false, false, false);
                }
            }
            else
            {
                PoliSimTheme.RoundedCard(pad, PoliSimTheme.Hex(0xF4ECDC), PoliSimTheme.Hairline, 4f);
            }

            System.DateTime date = _simulationManager.CurrentDate;
            var monthRect = new Rect(pad.x, pad.y + pad.height * (2f / 80f), pad.width, pad.height * (18f / 80f));
            var dayRect = new Rect(pad.x, pad.y + pad.height * (24f / 80f), pad.width, pad.height * (34f / 80f));
            var metaRect = new Rect(pad.x, pad.y + pad.height * (56f / 80f), pad.width, pad.height * (13f / 80f));

            GUI.Label(monthRect, date.ToString("MMM").ToUpperInvariant(), _calendarMonthStyle);
            GUI.Label(dayRect, date.Day.ToString(), _calendarDayStyle);
            GUI.Label(metaRect, date.Year.ToString(), _calendarMetaStyle);
        }

        /// <summary>
        /// The HELD lamp dot's diameter. Derived from the banner's own type size at the spec's own
        /// proportion — §A.6 draws an 8px dot beside 10.5px type, and the ratio is what carries over,
        /// not either absolute number (this banner renders at RescaleStylesToScreen's escalated size,
        /// not at board scale). ⚠ ONE ACCESSOR, READ BY BOTH SITES: RescaleStylesToScreen reserves this
        /// much extra left padding and DrawHoldBannerLabel draws the dot into it — a copy in either
        /// place is how a reserve and its drawing drift apart (the instance-#12 shape).
        /// </summary>
        private int HoldBannerLampSize()
        {
            return Mathf.RoundToInt(_holdBannerStyle.fontSize * (8f / 10.5f));
        }

        /// <summary>
        /// B8's carrier: one Label in the `ui_banner_hold` desk plate, with the amber lamp dot drawn
        /// over its reserved left padding. Exactly ONE control either way (the dot is an overlay draw,
        /// not a control), so both call sites keep the stable-control-layout guarantee they had when
        /// this was a bare `_eventBannerStyle` label. The dot centres on the FIRST text line — a
        /// wrapped banner reads top-down and the lamp belongs beside the headline, not mid-paragraph.
        /// No glow: §3.2 says effects are baked or absent, and the plate ships without one.
        /// </summary>
        private void DrawHoldBannerLabel(string text)
        {
            GUILayout.Label(text, _holdBannerStyle);

            if (_holdBannerStyle.normal.background == null)
            {
                return; // degraded to the plain amber label - no plate, so no lamp to mount on it
            }

            Rect plate = GUILayoutUtility.GetLastRect();
            float lamp = HoldBannerLampSize();
            var dotRect = new Rect(
                plate.x + HoldBannerPadX,
                plate.y + HoldBannerPadY + (_holdBannerStyle.lineHeight - lamp) * 0.5f,
                lamp,
                lamp);
            PoliSimTheme.Pill(dotRect, PoliSimTheme.DraftOnDesk);
        }

        private float CalendarAndSpeedControlsHeight(string statusText, bool isPaused, float columnWidth)
        {
            GUIStyle statusStyle = isPaused ? _holdBannerStyle : _labelStyle;
            // v2.0 chrome: the top row is now [calendar pad | status], so the status wraps into the
            // width REMAINING beside the pad, and the row is as tall as the taller of the two. The
            // same subtraction the drawing performs, from the same accessors — measuring at the full
            // width would under-count the wrapped lines, which is the exact quiet failure this
            // method's own doc comment describes.
            Vector2 pad = CalendarPadSize();
            float statusWidth = PoliSimWidgets.InnerWidth(columnWidth, _boxStyle, 1, statusStyle) - pad.x - CalendarPadGap;
            float statusHeight = statusStyle.CalcHeight(new GUIContent(statusText), statusWidth) + statusStyle.margin.vertical;
            float speedRowHeight = _buttonStyle.fixedHeight + _buttonStyle.margin.vertical;

            return Mathf.Max(pad.y, statusHeight) + speedRowHeight;
        }

        private void DrawCalendarAndSpeedControls(string statusText, bool isPaused)
        {
            GUILayout.BeginVertical();

            // v2.0 chrome: [calendar pad | status line] over the speed row. The board (§A.6) lays all
            // three in one row, but this UI's speed buttons are deliberately outsized and would not
            // survive losing the pad's width — so the pad shares the STATUS row, whose text wraps.
            GUILayout.BeginHorizontal();
            DrawCalendarPad();
            GUILayout.Space(CalendarPadGap);

            // One Label either way (DrawHoldBannerLabel is itself a single Label plus an overlay), so
            // the paused/running switch stays a style-and-content change, never a control-count change.
            if (isPaused)
            {
                DrawHoldBannerLabel(statusText);
            }
            else
            {
                GUILayout.Label(statusText, _labelStyle);
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            DrawSpeedButton("Pause", GameSpeed.Paused);
            DrawSpeedButton("1x", GameSpeed.Normal);
            DrawSpeedButton("2x", GameSpeed.Fast);
            DrawSpeedButton("3x", GameSpeed.VeryFast);

            // SAVE/LOAD UI (item 8's menu pass): the discoverable path to the saves screen, in the
            // one panel visible on every tab. Enabled UNCONDITIONALLY on purpose - the caller wraps
            // this whole panel in GUI.enabled = !_isGameOver, and a game-over player is exactly the
            // player who most needs Load; composed (saved and restored), not clobbered, per the
            // stable-layout discipline's own GUI.enabled rule.
            bool wasEnabled = GUI.enabled;
            GUI.enabled = true;
            // Same base style as the speed buttons beside it, so the row stays one even rank -
            // the first capture had it on the smaller tab-button metric, floating short of its
            // neighbours.
            if (GUILayout.Button("Saves", UiPalette.BuildButtonStyle(_buttonStyle, UiPalette.ButtonKind.Neutral)))
            {
                OpenSavesMenu();
            }
            GUI.enabled = wasEnabled;
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
            // STEP 3: the scenario evaluator shares this post-turn site by ruling - one boundary, one
            // place run-ending conditions are judged. After CheckElection, so an election loss on the
            // same boundary keeps its existing precedence.
            CheckScenarioObjectives();
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
                _gameOverReason = $"Lost re-election at year {_pendingElectionTurn} with {_pendingElectionResult.ApprovalAtElection:F1} approval " +
                    $"(needed at least {ElectionSystem.LosingThreshold:F0}).";
            }
            _pendingElectionResult = null;
        }

        /// <summary>
        /// STEP 3's VERDICT SCREEN (R-S3c/R-S3d): pass/fail per objective with its MEASURED MARGIN,
        /// then a legibility-powered epilogue. No score and no leaderboard, by ruling - a composite
        /// would need a weighting across incommensurate objectives (debt points against approval
        /// points), which is the invented-number-that-looks-researched this project's rule 5 forbids,
        /// and a cross-version score is a comparability promise five baseline discontinuities in one
        /// fortnight say the codebase cannot keep.
        ///
        /// ⚠ Bare-desk grammar, and its recorded lesson: this screen draws on the ONE ground with no
        /// paper under it, so every label takes `PoliSimTheme.TextOnDesk` - the paper-ink ramp is
        /// near-invisible here, the defect the election reveal's own first capture found.
        ///
        /// ⚠ **The epilogue's v1 limitation, stated rather than implied**: the attribution ledger
        /// persists exactly ONE period (R-S2e), so the "why" half reads the FINAL period's terms plus
        /// `StatHistory`'s run-long series for the "what". Per-scenario term accumulation is the named
        /// upgrade, and its trigger is an epilogue that needs to explain the whole run rather than
        /// its last year.
        /// </summary>
        /// <summary>
        /// The verdict screen's per-objective figure line - Italy Debt Crisis's own contribution
        /// (the `Sustained` form's first REAL content), fixing the gap
        /// `SustainedObjectiveDiagnostic` found and recorded rather than fixed: a MARGIN
        /// (measured value vs. target) is the obvious, sufficient story for
        /// <see cref="ObjectiveKind.Terminal"/>/<see cref="ObjectiveKind.ThresholdAtDate"/>/
        /// <see cref="ObjectiveKind.NeverBreach"/> - it is exactly what decided the outcome. For
        /// <see cref="ObjectiveKind.Sustained"/> it is NOT: what decided the outcome is the STREAK
        /// (<see cref="ObjectiveProgress.ConsecutiveTurns"/> against
        /// <see cref="ScenarioObjective.RequiredTurns"/>), and the diagnostic's own finding was
        /// that showing only the final turn's margin (e.g. "+1.14") says nothing about the 20-turn
        /// streak that actually decided `Met`. So Sustained gets its own line: the streak first,
        /// the latest measured value second, as context rather than as the headline.
        /// </summary>
        private static string BuildObjectiveFigure(ScenarioObjective objective, ObjectiveProgress state)
        {
            if (state == null || !state.HasValue)
            {
                return "not measured";
            }

            string comparisonGlyph = objective.Comparison == ObjectiveComparison.AtMost ? "≤" : "≥";
            string margin = $"{state.LastValue:F1}{objective.Unit} vs {comparisonGlyph} {objective.Target:F1}{objective.Unit} " +
                             $"({ScenarioEvaluator.MarginOf(objective, state.LastValue):+0.0;-0.0})";

            if (objective.Kind != ObjectiveKind.Sustained)
            {
                return margin;
            }

            // Met is STICKY (ScenarioEvaluator's own fix, same pass): once the streak first
            // reaches RequiredTurns it stays achieved even if a later turn breaks it, so
            // ConsecutiveTurns (the CURRENT streak) and Met (the achievement) can legitimately
            // disagree at verdict time - a player who held it and then had one bad turn near the
            // end sees Met=true with a lower final ConsecutiveTurns, and the line has to stay
            // honest about which of the two it is reporting.
            string streak = state.Met
                ? (state.ConsecutiveTurns >= objective.RequiredTurns
                    ? $"held for {state.ConsecutiveTurns} of {objective.RequiredTurns} required turns"
                    : $"reached the {objective.RequiredTurns}-turn streak earlier (currently at {state.ConsecutiveTurns})")
                : $"never reached the {objective.RequiredTurns}-turn streak (currently at {state.ConsecutiveTurns})";
            return $"{streak} - latest: {margin}";
        }

        private void DrawScenarioVerdictScreen()
        {
            bool won = _scenarioProgress.Verdict == ScenarioVerdict.Won;

            GUILayout.BeginArea(new Rect(0f, 0f, Screen.width, Screen.height));
            GUILayout.FlexibleSpace();
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.BeginVertical(GUILayout.Width(Screen.width * 0.52f));

            var bannerStyle = new GUIStyle(_gameOverStyle);
            bannerStyle.normal.textColor = won ? PoliSimTheme.Hex(0x8FBF7A) : PoliSimTheme.Hex(0xE0907E);
            GUILayout.Label(won ? "SCENARIO COMPLETE" : "SCENARIO FAILED", bannerStyle);

            var deskLabel = new GUIStyle(_labelStyle);
            deskLabel.normal.textColor = PoliSimTheme.TextOnDesk;
            var deskWrap = new GUIStyle(deskLabel) { wordWrap = true };

            GUILayout.Label($"{_scenario.Name} - {_playerCountry.Name}, turn {_simulationManager.CurrentTurn}", deskLabel);
            GUILayout.Space(10f);
            GUILayout.Label(_scenarioProgress.VerdictReason ?? "", deskWrap);
            GUILayout.Space(16f);

            // Per-objective: met/missed, the measured value, and the SIGNED margin - positive is
            // slack, negative is the shortfall, whichever direction the comparison runs.
            //
            // ⚠ ONE STYLE OBJECT PER ROLE, TINTED THROUGH DrawColoredLabel - not a set of copied
            // styles. Two lessons stacked here, both learned on this screen family:
            // (1) DESK INK: the paper palette's change colours are mixed for cream paper, and on the
            //     bare desk the negative one reads as near-black.
            // (2) COPIED STYLES SHARE THEIR GUIStyleState: building metStyle/missedStyle/figureStyle
            //     as copies and assigning each a colour left exactly one line - the MISSED row's
            //     figure - rendering in another row's ink across two capture passes. DrawColoredLabel
            //     is this file's existing answer (set, draw, restore, one object), and it is why that
            //     helper exists at all.
            Color metInk = PoliSimTheme.Hex(0x8FBF7A);
            Color missedInk = PoliSimTheme.Hex(0xE0907E);

            foreach (ScenarioObjective objective in _scenario.Objectives)
            {
                ObjectiveProgress state = ScenarioEvaluator.FindProgress(_scenarioProgress, objective.Id);
                bool met = state != null && state.Met && !state.Failed;
                string mark = met ? "MET" : "MISSED";
                string figure = BuildObjectiveFigure(objective, state);

                DrawColoredLabel($"{mark}  -  {objective.Description}", deskWrap, met ? metInk : missedInk);
                DrawColoredLabel($"        {figure}", deskWrap, PoliSimTheme.TextOnDesk);
            }

            GUILayout.Space(16f);
            GUILayout.Label("What happened", deskLabel);
            foreach (string line in BuildScenarioEpilogue())
            {
                GUILayout.Label($"  {line}", deskWrap);
            }

            GUILayout.Space(24f);
            GUIStyle dismissStyle = UiPalette.BuildButtonStyle(_buttonStyle, UiPalette.ButtonKind.Primary);
            if (GUILayout.Button("Close", dismissStyle))
            {
                DismissScenarioVerdict();
            }

            GUILayout.EndVertical();
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.EndArea();
        }

        /// <summary>
        /// The epilogue: the run's arc from `StatHistory`'s own series (the WHAT), plus the final
        /// period's largest approval terms from the attribution ledger (the WHY, as far as one period
        /// can say). Reads recorded values only - it never recomputes a model quantity, so the
        /// epilogue and the trace panel cannot disagree.
        /// </summary>
        private List<string> BuildScenarioEpilogue()
        {
            var lines = new List<string>();
            StatHistory history = _playerCountry.History;

            AppendArc(lines, "GDP", history.Gdp.Quarterly, "B", 0);
            AppendArc(lines, "Unemployment", history.Unemployment.Quarterly, "%", 1);
            AppendArc(lines, "Poverty", history.PovertyRate.Quarterly, "%", 1);
            AppendArc(lines, "Debt-to-GDP", history.DebtToGdpRatio.Quarterly, "%", 1);
            AppendArc(lines, "Approval", history.ApprovalRating.Quarterly, "", 1);

            ApprovalAttribution ledger = _playerCountry.ApprovalLedgerLastPeriod;
            if (ledger != null && ledger.Closed)
            {
                string biggest = LargestApprovalTerm(ledger);
                if (biggest != null)
                {
                    lines.Add($"Final year, approval moved {ledger.ApprovalAtClose - ledger.ApprovalAtPeriodOpen:+0.0;-0.0}; largest single term: {biggest}.");
                }
            }

            if (lines.Count == 0)
            {
                lines.Add("No history recorded - the run ended before its first period closed.");
            }

            return lines;
        }

        private static void AppendArc(List<string> lines, string name, List<float> series, string unit, int decimals)
        {
            if (series == null || series.Count < 2)
            {
                return;
            }

            float first = series[0];
            float last = series[series.Count - 1];
            string format = "F" + decimals;
            lines.Add($"{name}: {first.ToString(format)}{unit} -> {last.ToString(format)}{unit} ({(last - first).ToString("+0." + new string('0', decimals) + ";-0." + new string('0', decimals))}{unit})");
        }

        /// <summary>The final period's single largest-magnitude approval term, named - the one-line
        /// version of the trace panel, for a player who has just finished and wants the headline.</summary>
        private static string LargestApprovalTerm(ApprovalAttribution ledger)
        {
            (string Name, float Value)[] terms =
            {
                ("reversion toward 50", ledger.Reversion),
                ("growth vs potential", ledger.GrowthEffect),
                ("unemployment above NAIRU", ledger.MiseryUnemployment),
                ("inflation off target", ledger.MiseryInflation),
                ("crime above baseline", ledger.MiseryCrime),
                ("corruption above baseline", ledger.MiseryCorruption),
                ("tax hikes", ledger.TaxHikePenalty),
                ("spending changes", ledger.SpendingEffect),
                ("welfare vs baseline", ledger.WelfareEffect),
                ("paid family leave", ledger.PaidLeaveEffect),
                ("drug policy stance", ledger.DrugPolicyEffect),
                ("inequality vs own norm", ledger.GiniEffect)
            };

            string bestName = null;
            float best = 0f;
            foreach ((string name, float value) in terms)
            {
                if (Mathf.Abs(value) > Mathf.Abs(best))
                {
                    best = value;
                    bestName = name;
                }
            }

            return bestName == null ? null : $"{bestName} ({best:+0.00;-0.00})";
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

            // ⚠ FIX 2026-08-12 (ruled): this takeover draws on the BARE DESK — the one screen with no
            // paper under it — and its body printed the paper-ink ramp, near-invisible. Found by the
            // screen's FIRST capture ever (div2 88a), which is the coverage argument in one line.
            // Desk ground takes the desk ink, same reasoning as the hold banner.
            var deskLabelStyle = new GUIStyle(_labelStyle);
            deskLabelStyle.normal.textColor = PoliSimTheme.TextOnDesk;
            GUILayout.Label($"Year {_pendingElectionTurn} Election - {_playerCountry.Name}", deskLabelStyle);
            GUILayout.Space(16f);

            GUILayout.Label($"Approval Rating: {result.ApprovalAtElection:F1} (needed {ElectionSystem.LosingThreshold:F0} to win)", deskLabelStyle);
            UiPalette.DrawBarWithThreshold(result.ApprovalAtElection / 100f, ElectionSystem.LosingThreshold / 100f, outcomeColor, 24f);
            GUILayout.Label($"Margin: {result.Margin:+0.0;-0.0}", deskLabelStyle);

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
            _turnLog.Add($"Year {_simulationManager.CurrentTurn}: GDP={UiFormat.Money(state.GDP, MoneyUnit.Billions)} ({_lastGrowthPercent:+0.00;-0.00;0}%), " +
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

        // ⚠ v2.0 FOLDER-TONGUE PASS — where each tongue's visible top edge sits inside the shared
        // 256×112 canvas, MEASURED from the delivered PNGs (first pixel row with alpha > 32, halved to
        // @1×), not taken from the manifest: the manifest's "sits 12px lower" note @2× understates the
        // measured on→off delta (y=3 vs y=20 @2×). These are constant SCREEN pixels at every window
        // size, because they live inside GUIStyle.border's top band, which IMGUI never scales.
        private const float FolderOnTongueTop = 2f;     // y=3 @2×
        private const float FolderHoverTongueTop = 7f;  // y=14 @2×
        private const float FolderOffTongueTop = 10f;   // y=20 @2×
        /// <summary>How far the re-painted active tongue extends DOWN over the content sheet — the
        /// manifest's "joined look: draw overlapping content sheet by 2px" (@2× = 1px, taken at 2 so the
        /// sheet's baked top keyline is covered at both capture sizes).</summary>
        private const float FolderTongueJoinOverlap = 2f;
        /// <summary>§A.7: the tab strip is padded `0 14px` — tongues never sit on the content sheet's
        /// rounded top corners. Constant screen px for the same reason the tongue-top offsets are: the
        /// sheet's corner geometry lives in its border bands, which do not scale.</summary>
        private const float FolderTabStripSideInset = 14f;
        /// <summary>The spine sprite's rounded ends stop just inside each tongue's own r≈5 top-corner
        /// curve instead of poking past it.</summary>
        private const float FolderSpineSideInset = 3f;

        /// <summary>The `ui_tab_spine` strip's height — the board's 3px edge on 15px tab type, floored so
        /// it never vanishes at the small clamp. One accessor: the icon inset below budgets around it and
        /// the two spine draw sites size with it, so they cannot drift.</summary>
        private float TabSpineHeight()
        {
            return Mathf.Max(3f, Mathf.Round(_tabButtonStyle.fontSize * (3f / 15f)));
        }

        /// <summary>
        /// Where the stacked icon starts below the button's top. With the folder faces live this is
        /// DERIVED, not the Phase C constant: the OFF tongue's visible edge is 10px down its canvas
        /// (baked) and the spine rides that edge, so the icon must clear tongue drop + spine + 2px of
        /// air on the INACTIVE tabs — and the same inset is used for the selected tab so the stack sits
        /// at one height across the bar and selection never shifts layout. Falls back to the original
        /// constant when the faces are missing, keeping the degraded bar byte-identical to the interim
        /// treatment. One accessor, read by <see cref="ConsolidatedTabButtonHeight"/> (the reserve) and
        /// <see cref="DrawConsolidatedTabButton"/> (the imposition) — the instance-#12 discipline.
        /// </summary>
        private float ConsolidatedTabIconTopInset()
        {
            return _folderTabsLive
                ? FolderOffTongueTop + TabSpineHeight() + 2f
                : ConsolidatedTabIconTopPadding;
        }

        /// <summary>
        /// How tall a consolidated tab button actually is — the larger of its base `fixedHeight` and the
        /// stacked icon+label height Phase C imposes.
        ///
        /// <para>Never smaller than the base height, so a very short window cannot produce a tab bar
        /// shorter than the rest of the UI expects. That floor is also why the two figures could agree at
        /// some font sizes and differ at others, which is the worst way for this kind of bug to
        /// behave.</para>
        ///
        /// ⚠ Computed unconditionally, where <c>DrawConsolidatedTabButton</c> applies the stack only when
        /// an icon actually loaded. That is deliberate and it errs in the safe direction: if every icon
        /// were missing, this over-reserves by the stack height and leaves a gap, rather than
        /// under-reserving and clipping.
        /// </summary>
        private float ConsolidatedTabButtonHeight()
        {
            float iconSize = Mathf.Round(_tabButtonStyle.fontSize * ConsolidatedTabIconFontMultiple);
            int labelFontSize = Mathf.Max(11, Mathf.RoundToInt(_tabButtonStyle.fontSize * ConsolidatedTabLabelFontScale));
            float labelBandHeight = labelFontSize + 6f;
            float stackedHeight = Mathf.RoundToInt(ConsolidatedTabIconTopInset() + iconSize + ConsolidatedTabIconLabelGap)
                                  + labelBandHeight
                                  + ConsolidatedTabLabelBottomPadding;

            return Mathf.Max(_tabButtonStyle.fixedHeight, stackedHeight);
        }

        /// <summary>The tab bar as a ROW: the button plus the margin GUILayout puts above and below it. Separate from <see cref="ConsolidatedTabButtonHeight"/> because the button's own style must not carry the margin term — a `fixedHeight` that included it would draw a taller BUTTON, not a taller row. With the folder faces live only the TOP margin survives: `BuildFolderTabStyle` zeroes `margin.bottom` so the tongues end flush against the content sheet, and this reserve must agree with what the clones actually lay out.</summary>
        private float ConsolidatedTabRowHeight()
        {
            return ConsolidatedTabButtonHeight()
                   + (_folderTabsLive ? _tabButtonStyle.margin.top : _tabButtonStyle.margin.vertical);
        }

        /// <summary>
        /// Explicitly divided evenly across <paramref name="availableWidth"/> - the SAME
        /// rightColumnWidth OnGUI already computes fresh from Screen.width every frame - so the row
        /// can never exceed its actual budget at any window size, matching the screen-relative
        /// approach already used everywhere else in this class (see the old DrawRightColumnTabs' own
        /// doc comment on why this matters, kept in git history).
        /// </summary>
        private void DrawConsolidatedTabs(float availableWidth)
        {
            // ⚠ EACH BUTTON ALSO CARRIES ITS OWN MARGIN, which a bare division does not account for -
            // the exact shape InnerWidth's doc describes and exists to prevent, forgotten here because
            // this row was written before the helper was. GUILayout inserts `_tabButtonStyle.margin`
            // between every pair, so n buttons at `availableWidth / n` sum to wider than the row.
            //
            // v2.0 folder-tongue pass: §A.7 pads the strip `0 14px`, so with the faces live the row is
            // inset from both edges (and the width budget shrinks to match) - tongues must not sit on
            // the content sheet's rounded top corners. The degraded bar keeps the full-width row.
            float stripInset = _folderTabsLive ? FolderTabStripSideInset : 0f;
            float buttonWidth = PoliSimWidgets.InnerWidth(availableWidth - stripInset * 2f, null, ConsolidatedTabsPerRow, _tabButtonStyle);

            GUILayout.BeginHorizontal();
            if (stripInset > 0f)
            {
                GUILayout.Space(stripInset);
            }
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
            if (stripInset > 0f)
            {
                GUILayout.Space(stripInset);
            }
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
            // v2.0 folder-tongue pass: the real §A.7 faces when all three loaded, the interim
            // brass/paper treatment otherwise - BuildFolderTabStyle returns null on any missing face so
            // the bar degrades wholesale, never one mixed tongue at a time.
            GUIStyle style = _folderTabsLive ? UiPalette.BuildFolderTabStyle(_tabButtonStyle, selected) : null;
            if (style == null)
            {
                style = UiPalette.BuildButtonStyle(_tabButtonStyle, selected ? UiPalette.ButtonKind.TabSelected : UiPalette.ButtonKind.Tab, area);
            }

            Texture2D icon = iconName != null ? IconLibrary.Get(iconName) : null;
            float iconSize = 0f;
            float iconTop = ConsolidatedTabIconTopInset();
            if (icon != null)
            {
                iconSize = Mathf.Round(_tabButtonStyle.fontSize * ConsolidatedTabIconFontMultiple);
                int labelFontSize = Mathf.Max(11, Mathf.RoundToInt(_tabButtonStyle.fontSize * ConsolidatedTabLabelFontScale));

                style.fontSize = labelFontSize;
                style.alignment = TextAnchor.MiddleCenter;
                style.padding.top = Mathf.RoundToInt(iconTop + iconSize + ConsolidatedTabIconLabelGap);
                style.padding.bottom = Mathf.RoundToInt(ConsolidatedTabLabelBottomPadding);
                // Left/right trimmed to near-zero so the label gets the button's full width on one
                // line - the whole point of stacking. Never smaller than the base height, so a very
                // short window can't produce a tab bar shorter than the rest of the UI expects.
                style.padding.left = 2;
                style.padding.right = 2;
                // ⚠ ONE ACCESSOR, READ BY BOTH SITES. OnGUI must RESERVE this height before the bar is
                // drawn and this method must IMPOSE it — exactly the separation UiContainmentGuard's doc
                // names as the shape that drifts in silence, and it HAD already drifted: OnGUI reserved
                // `_tabButtonStyle.fixedHeight` while this took the LARGER of that and the stacked
                // icon+label height, so at any font size where the icon won, the tab content below was
                // pushed down by the difference with nothing reporting it.
                style.fixedHeight = ConsolidatedTabButtonHeight();
            }

            // ⚠ THE SELECTED TONGUE IS LAID OUT HERE AND PAINTED LATER (folder faces only). The content
            // sheet draws after this bar and would paint its top keyline across the tongue's bottom -
            // the opposite of §A.7's "folder pulled forward". So the selected tab's BUTTON renders
            // through a fully invisible clone (same geometry, same control, same click handling; the
            // stable-control-layout guarantee needs the control, not its pixels) and its visuals are
            // re-painted OVER the sheet by DrawActiveFolderTongue, extended FolderTongueJoinOverlap
            // over the sheet's edge. Painting it twice instead would double the face's baked
            // semi-transparent shadow into a visible dark rim.
            bool paintDeferred = _folderTabsLive && selected;
            GUIStyle buttonStyle = style;
            if (paintDeferred)
            {
                buttonStyle = new GUIStyle(style);
                buttonStyle.normal.background = null;
                buttonStyle.hover.background = null;
                buttonStyle.active.background = null;
                buttonStyle.focused.background = null;
                Color hidden = new Color(0f, 0f, 0f, 0f);
                buttonStyle.normal.textColor = hidden;
                buttonStyle.hover.textColor = hidden;
                buttonStyle.active.textColor = hidden;
                buttonStyle.focused.textColor = hidden;
            }

            bool clicked = GUILayout.Button(label, buttonStyle, GUILayout.Width(width));
            Rect buttonRect = GUILayoutUtility.GetLastRect();
            bool hovered = buttonRect.Contains(Event.current.mousePosition);

            if (paintDeferred)
            {
                // Stored fresh every event and consumed later the same OnGUI pass, so a stale rect can
                // never be painted (a takeover-suppressed frame never reaches the paint site either).
                _activeTongueStyle = style;
                _activeTongueRect = buttonRect;
                _activeTongueLabel = label;
                _activeTongueIcon = icon;
                _activeTongueIconSize = iconSize;
                _activeTongueIconTop = iconTop;
                _activeTongueArea = area;
            }
            else if (icon != null)
            {
                var iconRect = new Rect(
                    buttonRect.x + (buttonRect.width - iconSize) * 0.5f,
                    buttonRect.y + iconTop,
                    iconSize,
                    iconSize);
                // Ink-weight tints both ways on the folder faces: white read on the interim brass but
                // vanishes on paper stock. (The selected tongue's area-ink icon is painted deferred.)
                Color iconTint = selected ? Color.white : UiPalette.MutedIconTint;
                UiPalette.DrawTintedIcon(iconRect, icon, iconTint);
            }

            // ⚠ v2.0 CHROME, 2026-08-12 — B7: `ui_tab_spine` across the tab's top edge. §A.7's tongue
            // spec: the active tab carries the area INK, the inactive its LIFTED weight — identity on
            // every tongue, full strength only on the folder pulled forward. Height at the board's own
            // ratio (a 3px edge on 15px tab type), floored so it never vanishes at the small clamp.
            // An overlay draw like the icon above — not a control, so the stable-control-layout
            // guarantee is untouched — and Repaint-gated because GUIStyle.Draw is a paint call.
            // GUI.color multiplies the white-on-alpha sprite into the hue (this sprite is WoA — the
            // one rendering class where tinting is the CORRECT handling per §3.0a, unlike the
            // real-colour plates around it).
            //
            // Folder-tongue pass: the spine rides the TONGUE's visible top edge, which the off/hover
            // faces bake lower on their canvas (the measured FolderXTongueTop constants), and stops
            // just inside the tongue's own corner curve. The selected tab's spine is painted deferred
            // with the rest of its tongue.
            if (!paintDeferred && _tabSpineStyle.normal.background != null && Event.current.type == EventType.Repaint)
            {
                float spineHeight = TabSpineHeight();
                float spineTop = 0f;
                float spineSideInset = 0f;
                if (_folderTabsLive)
                {
                    spineTop = selected ? FolderOnTongueTop : (hovered ? FolderHoverTongueTop : FolderOffTongueTop);
                    spineSideInset = FolderSpineSideInset;
                }
                Color savedColor = GUI.color;
                GUI.color = selected ? UiPalette.GetAreaColor(area) : PoliSimTheme.AccentOnDesk(area);
                _tabSpineStyle.Draw(new Rect(buttonRect.x + spineSideInset, buttonRect.y + spineTop,
                    buttonRect.width - spineSideInset * 2f, spineHeight), false, false, false, false);
                GUI.color = savedColor;
            }

            if (clicked)
            {
                // Nothing to seed since Tax and Spending merged into one Budget tab: the Budget Process
                // screen's own category selector is now the only thing that sets _budgetProcessCategory,
                // so it keeps whatever the player last chose instead of a tab click silently resetting it.
                _consolidatedTab = tab;
            }
        }

        /// <summary>
        /// The deferred half of the folder-tongue treatment: paints the SELECTED tab's face, label,
        /// icon and spine after the content sheet has drawn, extended <see cref="FolderTongueJoinOverlap"/>
        /// down over the sheet's top keyline — §A.7's folder pulled forward, and the manifest's own
        /// "joined look: draw overlapping content sheet by 2px". Pure paint over a control that already
        /// laid out and handled its click in bar order (see DrawConsolidatedTabButton's paintDeferred
        /// block), so control count and order are untouched. The 9-slice centre band absorbs the extra
        /// height; the label's position comes from padding.top and does not move.
        /// </summary>
        private void DrawActiveFolderTongue()
        {
            if (!_folderTabsLive || _activeTongueStyle == null || Event.current.type != EventType.Repaint)
            {
                return;
            }

            var tongueRect = new Rect(_activeTongueRect.x, _activeTongueRect.y,
                _activeTongueRect.width, _activeTongueRect.height + FolderTongueJoinOverlap);
            _activeTongueStyle.Draw(tongueRect, new GUIContent(_activeTongueLabel), false, false, false, false);

            if (_activeTongueIcon != null)
            {
                var iconRect = new Rect(
                    _activeTongueRect.x + (_activeTongueRect.width - _activeTongueIconSize) * 0.5f,
                    _activeTongueRect.y + _activeTongueIconTop,
                    _activeTongueIconSize,
                    _activeTongueIconSize);
                // Area INK, not white: the interim treatment's white icon read on brass and would
                // vanish on the paper tongue. Full ink strength is the selected tab's privilege, the
                // same rule the spine below follows.
                UiPalette.DrawTintedIcon(iconRect, _activeTongueIcon, UiPalette.GetAreaColor(_activeTongueArea));
            }

            if (_tabSpineStyle.normal.background != null)
            {
                Color savedColor = GUI.color;
                GUI.color = UiPalette.GetAreaColor(_activeTongueArea);
                _tabSpineStyle.Draw(new Rect(_activeTongueRect.x + FolderSpineSideInset,
                    _activeTongueRect.y + FolderOnTongueTop,
                    _activeTongueRect.width - FolderSpineSideInset * 2f, TabSpineHeight()), false, false, false, false);
                GUI.color = savedColor;
            }
        }

        /// <summary>Generic sub-category tab button, shared by Statistics/Policy-Laws/Politics' own category rows - mirrors DrawBudgetProcessCategoryButton's exact established pattern (Primary when selected, Neutral otherwise - no per-area tinting at this second level, unlike the top-level tabs above). RULED 2026-08-12 (Elias): the no-area-tint decision STANDS against §A.8's "bottom 3px area ink" strip and the manifest's "ui_subtab_on's bottom hue strip = ui_tab_spine flipped" - the main-tab spine carries area identity one level up, so the strip would be redundant, not missing.</summary>
        private void DrawSubCategoryButton<T>(string label, T category, ref T selectedCategory, float maxWidth = 0f, float rowHeight = 0f) where T : struct, System.Enum
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
            // ⚠ AND THAT FIX THEN OVERFLOWED THE ROW - measured 2026-08-10, not guessed. MinWidth is a
            // FLOOR, so when the floors sum to more than the container the row grows past it: it stopped
            // truncating the label and started truncating the PANEL. Policy/Laws measured at five
            // minimums summing to 791.8px against an availableWidth of 814.1 - which looks like it fits -
            // and a usable width of 786.1 once `_boxStyle`'s 28px horizontal padding comes out. A 5.7px
            // overflow: exactly enough to clip the tail of the last button, and enough to widen the whole
            // content group so the ledger rows' trailing column clipped as well.
            //
            // The budget was computed against the width passed IN rather than the width available INSIDE
            // the container. `maxWidth` is that corrected budget - a floor is still a floor, but never
            // larger than this button's share of what actually exists. When a label cannot fit its share,
            // BuildSubTabStyle clears fixedHeight precisely so the button WRAPS to two lines, which is
            // why capping is safe rather than a return to truncation.
            // ⚠ THE FLOOR IS THE ROW'S SHARE, NOT THIS BUTTON'S OWN CONTENT - and that is what makes the
            // row EVEN. Measured 2026-08-10: with a per-button floor, "Trade" (a short label, floor
            // 87.4) laid out at 109px beside siblings at 172px, because ExpandWidth distributes surplus
            // in proportion to what each child asked for, and the button that asked for least got least.
            // The row fitted; it just looked broken, which is the same complaint by a different route.
            //
            // A shared floor makes every button request the same width, so five equal buttons fill the
            // row. A label too long for its share still wraps rather than truncates - BuildSubTabStyle
            // clears fixedHeight for exactly that - so uniformity costs nothing.
            float minWidth = maxWidth > 0f
                ? maxWidth
                : PoliSimWidgets.MeasuredWidth(label, style, style.padding.horizontal + 6f);

            // ⚠ INSTANCE #13: when the caller measured the row (SubTabRowHeight), the height is IMPOSED
            // rather than left to GUILayout, whose own wrap-height derivation is what garbled the ECB
            // label. All three current callers (the Statistics/Policy-Laws/Politics rows) pass a
            // measured height; the MinHeight arm is the pre-#13 form, kept so an unmeasured future
            // caller degrades to the old floor rather than to zero height.
            if (GUILayout.Button(label, style, GUILayout.ExpandWidth(true), GUILayout.MinWidth(minWidth),
                rowHeight > 0f ? GUILayout.Height(rowHeight) : GUILayout.MinHeight(_tabButtonStyle.fixedHeight)))
            {
                selectedCategory = category;
            }

        }

        /// <summary>
        /// The width one button of a <paramref name="count"/>-button sub-tab row may claim as its floor,
        /// given the row's OUTER width, the container padding it will be drawn inside, and the margins
        /// GUILayout puts BETWEEN the buttons.
        ///
        /// ⚠ **The margins are not decoration, they are part of the budget.** Measured 2026-08-10 on
        /// Policy/Laws' five-button row: the row spans 806px, five floors of 157.2 sum to 786 - which
        /// looks like it fits - but each button also carries 4px of margin a side, so the row actually
        /// needs 826. GUILayout does not distribute that 20px shortfall evenly; it satisfies the earlier
        /// children and takes the whole deficit out of the last one, so Trade laid out at 102px beside
        /// siblings at 172px and read as a cut-off button rather than as a squeezed row.
        ///
        /// This is the third term in the same subtraction. The first version divided the outer width,
        /// the second subtracted the container padding (the 5.7px case), and this one subtracts the
        /// margins - each fix correct as far as it went, each leaving a smaller residue behind.
        /// </summary>
        private float SubTabShare(float availableWidth, int count)
        {
            return PoliSimWidgets.InnerWidth(availableWidth, _boxStyle, count, GUI.skin.button);
        }

        /// <summary>
        /// ⚠ LABEL-CLIPPING INSTANCE #13 — and the first reached through the COUNTRY axis (2026-08-12).
        /// The height one sub-tab row needs: the tallest label's measured wrap height at the row's own
        /// share width, in the ACTIVE face's style (the taller padding), floored at the tab bar's
        /// height so an all-short row stays even with the rest of the UI.
        ///
        /// GUILayout cannot be trusted to derive this itself: a wrapping button with flexible width
        /// computes its layout height at a width that is not the width it renders at, so "European
        /// Central Bank (ECB)" laid out two lines tall and rendered three — centred, overflowing BOTH
        /// edges, garbling into the panel above. Only when SELECTED, because the active face's padding
        /// is what pushed it to a third line at 1600-class sizes; clean at 1440p and clean for every
        /// shorter institution name — which is why eleven USA-only capture passes never saw it, and the
        /// evidence Elias named that this class is not exhausted by its known sites.
        ///
        /// ⚠ ONE MEASUREMENT, TWO SITES, per the instance-#12 accessor discipline: each tab's content
        /// reserve subtracts this value and <see cref="DrawSubCategoryButton"/> imposes it via
        /// GUILayout.Height. Measuring at the SHARE width is the safe direction — ExpandWidth can only
        /// widen a button beyond its share, which wraps FEWER lines than measured, never more.
        /// </summary>
        private float SubTabRowHeight(float share, params string[] labels)
        {
            GUIStyle active = BuildSubTabStyle(true);
            float height = _tabButtonStyle.fixedHeight;
            foreach (string label in labels)
            {
                height = Mathf.Max(height, active.CalcHeight(new GUIContent(label), share));
            }

            return height;
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

            // ⚠ v2.0 CHROME, 2026-08-11 — a sub-tab has its OWN pair of sprites, and using them is what
            // makes a sub-tab read as a different KIND of control rather than a smaller button.
            // `BuildButtonStyle` has already dressed this style as `ui_btn_brass`/`ui_btn_paper`; these
            // replace the background and nothing else, so every other property it set survives.
            //
            // ⚠ DRAWN UNTINTED, per §3.0a's question — "does this art get tinted at draw time?" These are
            // REAL-COLOUR paper furniture, exactly like `ui_btn_*`, whose own comment says so twenty
            // lines up — NOT white-on-alpha. Tinting them would double-apply colour already in the
            // pixels, which is the damage §3.0a exists to prevent.
            Texture2D face = IconLibrary.GetChrome(selected ? "ui_subtab_on" : "ui_subtab_off");
            if (face != null)
            {
                style.normal.background = face;
                style.hover.background = face;
                style.active.background = face;
                style.focused.background = face;

                // ⚠ THE INSET IS DERIVED, NOT QUOTED — the manifest gives no 9-slice for these two, and
                // inventing one is the mockup-number trap this project has recorded twice. `ui_btn_*` is
                // 128x64 @2x at 20/20/20/28; these are 128x56 @2x — the SAME WIDTH, 8px shorter. So the
                // horizontal inset carries over exactly (20 @2x = 10 @1x) and the vertical scales by
                // 56/64 (20 -> 17.5, 28 -> 24.5 @2x, so 9 and 12 @1x). `GUIStyle.border` is @1x, and
                // IMGUI reads the slice from the STYLE, never from the texture's own spriteBorder.
                style.border = new RectOffset(10, 10, 9, 12);

                // ⚠ FIX 2026-08-12 — cbdde4e replaced the brass with this pale paper face and left the
                // Primary kind's CREAM text in place: cream on pale paper, and the selected sub-tab
                // label ("Domestic") was unreadable in every holdcal capture while the pre-conversion
                // set read fine as white-on-brass. A face swap changes what ink reads on it — the two
                // are one decision. §A.8's own spec: active = bold inkText, inactive = inkFaint.
                // Re-inked only inside this block, so the degraded brass/paper form keeps the cream
                // that suits it.
                Color subTabInk = selected ? PoliSimTheme.TextPrimary : PoliSimTheme.TextSecondary;
                style.normal.textColor = subTabInk;
                style.hover.textColor = subTabInk;
                style.active.textColor = subTabInk;
                style.focused.textColor = subTabInk;
            }

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
        private void DrawStatisticsTab(float availableHeight, float availableWidth)
        {
            GUILayout.BeginVertical(_boxStyle);
            DrawColoredLabel("Statistics", _headerStyle, UiPalette.GetAreaColor(UiPalette.SystemArea.Global));
            GUILayout.BeginHorizontal();
            float subTabShare = SubTabShare(availableWidth, 2);
            // Instance #13: the row's height is measured once (SubTabRowHeight) and shared between the
            // buttons and the content reserve below - see the accessor's own doc for the ECB case.
            float subTabRowHeight = SubTabRowHeight(subTabShare, "Domestic", "International");
            DrawSubCategoryButton("Domestic", StatisticsCategory.Domestic, ref _statisticsCategory, subTabShare, subTabRowHeight);
            DrawSubCategoryButton("International", StatisticsCategory.International, ref _statisticsCategory, subTabShare, subTabRowHeight);
            GUILayout.EndHorizontal();
            GUILayout.Space(6f);

            float contentHeight = availableHeight - _headerStyle.fontSize - subTabRowHeight - 14f;
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
            _gdpGraph.Draw("GDP (dashed = next-year estimate)", history.Gdp.Quarterly, projectedGdp, _labelStyle, higherIsBetter: true,
                moneyUnit: PolicyWebRenderer.GetStatUnit(StatNodeId.Gdp));
            _unemploymentGraph.Draw("Unemployment (dashed = next-year estimate)", history.Unemployment.Quarterly, projectedUnemployment, _labelStyle, higherIsBetter: false, moneyUnit: null,
                thresholdValue: _playerCountry.NaturalUnemploymentRate, thresholdLabel: "NAIRU");
            _inflationGraph.Draw("Inflation", history.Inflation.Quarterly, null, _labelStyle, higherIsBetter: false, moneyUnit: null);
            _approvalGraph.Draw("Approval Rating (dashed = next-year estimate)", history.ApprovalRating.Quarterly, projectedApproval, _labelStyle, higherIsBetter: true, moneyUnit: null);
            _povertyGraph.Draw("Poverty Rate", history.PovertyRate.Quarterly, null, _labelStyle, higherIsBetter: false, moneyUnit: null);
            _debtGraph.Draw("Debt-to-GDP", history.DebtToGdpRatio.Quarterly, null, _labelStyle, higherIsBetter: false, moneyUnit: null,
                thresholdValue: _playerCountry.ComfortableDebtToGdpPercent, thresholdLabel: "comfortable");

            // ROUND 4 BATCH 1 (C3): the two new social stats, as read-only ledger rows via the
            // Derived-panel pattern rather than headline tiles or StatNodeId entries - a stat can
            // land on screen icon-free this way with zero asset work, which is the pilot batch's
            // display answer (icon/StatNodeId promotion is an arc-level batching decision, deferred).
            //
            // These are STORED simulation stats, not derived arithmetic, so they get their own small
            // block rather than a seat inside "Derived" - putting them there would misstate what the
            // panel's own doc comment promises ("every figure here is DERIVED, never stored").
            GUILayout.Space(12f);
            GUILayout.BeginVertical(_boxStyle);
            GUILayout.Label("Society", _headerStyle);
            DrawDerivedStatRow("Youth unemployment", state.YouthUnemployment / 100f,
                UiFormat.Number(state.YouthUnemployment, 1) + "%",
                "of youth labor force", UiPalette.GetAreaColor(UiPalette.SystemArea.Labor));
            // ⚠ LIFE EXPECTANCY HAS NO DENOMINATOR - years at birth are not a share of anything, so
            // the fill is negative and no gauge is drawn, per §A.9b (the GDP-per-capita precedent).
            DrawDerivedStatRow("Life expectancy", -1f,
                UiFormat.Number(state.LifeExpectancy, 1), "years at birth",
                UiPalette.GetAreaColor(UiPalette.SystemArea.Welfare));
            // ROUND 4 BATCH 2 (C2). Gini lives on a genuine 0-100 scale (the source's own label),
            // so it earns a gauge; the trailing text names the scale rather than a fake unit.
            DrawDerivedStatRow("Income inequality (Gini)", state.Gini / 100f,
                UiFormat.Number(state.Gini, 1), "0-100 scale", UiPalette.GetAreaColor(UiPalette.SystemArea.Welfare));
            // ⚠ A BASE-100 INDEX IS UNBOUNDED BY CONSTRUCTION - §A.9b's negative-fill treatment,
            // decided deliberately per the batch directive, not defaulted: any fill denominator
            // would be an invented ceiling. The trailing text carries the one honest comparison
            // (its own starting level); cross-country level comparison is NOT claimed, by ruling.
            DrawDerivedStatRow("Real wages", -1f,
                UiFormat.Number(state.RealWageIndex, 1), "index, 100 = start of term",
                UiPalette.GetAreaColor(UiPalette.SystemArea.Labor));
            // ROUND 4 BATCH R4-5 (C5): productivity, beside its wage sibling. §A.9b negative-fill
            // DECIDED DELIBERATELY: unlike the two index siblings the level is REAL (USD PPP per
            // hour, one basis), but it is still unbounded, and any fill denominator would be an
            // invented ceiling. The trailing text carries the OECD's own usage rule - this ledger
            // shows only the player's country, so cross-country comparison is structurally absent,
            // and the text keeps it honest anyway.
            DrawDerivedStatRow("Productivity", -1f,
                UiFormat.Number(state.Productivity, 1), "$ per hour (PPP), against your own past",
                UiPalette.GetAreaColor(UiPalette.SystemArea.Labor));
            // ROUND 4 BATCH 3 (C1): the housing three, with THE ASYMMETRY DECIDED DELIBERATELY -
            // the primary metric leads per the ruling: overburden first for the EU five,
            // homeownership first for the USA, whose overburden row is ABSENT (not zero, not
            // greyed - absent). Drawing "0.0%" would fabricate a figure no source publishes;
            // the missing row IS the recorded USA-on-homeownership ruling made visible.
            Color housingInk = UiPalette.GetAreaColor(UiPalette.SystemArea.Welfare);
            if (_playerCountry.TracksHousingOverburden)
            {
                DrawDerivedStatRow("Housing overburden", state.HousingOverburden / 100f,
                    UiFormat.Number(state.HousingOverburden, 1) + "%",
                    "spend >40% of income on housing", housingInk);
            }
            DrawDerivedStatRow("Homeownership", state.Homeownership / 100f,
                UiFormat.Number(state.Homeownership, 1) + "%",
                _playerCountry.TracksHousingOverburden ? "of households" : "of households (primary metric)",
                housingInk);
            // House prices: the R4-2 unbounded-index treatment verbatim (§A.9b negative-fill).
            DrawDerivedStatRow("House prices", -1f,
                UiFormat.Number(state.HousePriceIndex, 1), "index, 100 = start of term",
                housingInk);
            GUILayout.EndVertical();

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

            // Inflation joins the two graphs because it SHARES THEIR CADENCE - monthly, 143 releases over
            // twelve years - so "compare against the live figures above" earns its place here.
            _inflationPublishedGraph.DrawPublished("Inflation as published",
                _playerCountry.Published.Series.TryGetValue(PublishedStat.Inflation, out PublishedSeries inflationPublished) ? inflationPublished : null,
                _labelStyle, higherIsBetter: false, _simulationManager.CurrentDate, moneyUnit: null);

            // ⚠ AND POVERTY RATE DOES NOT, because it publishes ANNUALLY - eleven releases in twelve
            // years, per PublicationCadenceCheck. Eleven points beside a daily live series reads as a
            // broken graph rather than as a comparison. An annual published figure is a bulletin - this
            // number, for this period, released on this date - so it renders as one. Placed here rather
            // than beside its live twin on Welfare so every published figure stays discoverable in one
            // place, which matters more for a bulletin than adjacency does.
            GUILayout.Space(8f);
            PublishedFigure.Draw("Poverty rate as published",
                _playerCountry.Published.Series.TryGetValue(PublishedStat.PovertyRate, out PublishedSeries povertyPublished) ? povertyPublished : null,
                _labelStyle, moneyUnit: null);

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
                BeginAreaCard("FEDERAL RESERVE", UiPalette.SystemArea.Political, blocksTime: true, dossier: true);
                DrawFedChairSelectionModal();
                EndAreaCard(UiPalette.SystemArea.Political);
                anyPending = true;
            }

            ForeignPolicyMeeting pendingMeeting = _simulationManager.GetPendingForeignPolicyMeeting(PlayerCountryId);
            if (pendingMeeting != null)
            {
                BeginAreaCard("FOREIGN POLICY", UiPalette.SystemArea.Global, blocksTime: true, dossier: true);
                DrawForeignPolicyMeetingModal(pendingMeeting, drawOwnFrame: false);
                EndAreaCard(UiPalette.SystemArea.Global);
                anyPending = true;
            }

            foreach ((CabinetPortfolio portfolio, CabinetDecision decision) in _simulationManager.GetPendingCabinetDecisions(PlayerCountryId))
            {
                // Tinted by the PORTFOLIO's own area, not one flat "cabinet" color - two simultaneous
                // cabinet decisions from different portfolios should not read as the same thing.
                UiPalette.SystemArea portfolioArea = UiPalette.GetPortfolioArea(portfolio);
                BeginAreaCard("CABINET", portfolioArea, blocksTime: true, dossier: true);
                DrawCabinetDecisionModal(portfolio, decision, drawOwnFrame: false);
                EndAreaCard(portfolioArea);
                anyPending = true;
            }

            if (_simulationManager.GetPendingBudgetProcess(PlayerCountryId))
            {
                BeginAreaCard("BUDGET PROCESS", UiPalette.SystemArea.Fiscal, blocksTime: true, dossier: true);
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
        private void BeginAreaCard(string kind, UiPalette.SystemArea area, bool blocksTime = false, bool dossier = false)
        {
            // ⚠ v2.0 CHROME, 2026-08-12 — §A.11: a Decisions card is a DOSSIER, drawn on
            // `ui_folder_dossier` with its baked tab shoulder; the kind caption moves ONTO the
            // shoulder (drawn by EndAreaCard, which is the first point the card's rect is known), so
            // the in-flow header row keeps only the urgency chip. Dossier-ness is a per-call-site
            // constant, so within any one screen the control sequence never varies — the
            // stable-control-layout guarantee is per screen, and this holds it. Sprite missing →
            // ordinary procedural area card, kind caption back in the flow, exactly as before.
            // Begin is the single authority on dossier-ness: EndAreaCard reads these two fields
            // rather than taking its own copies of the same facts as parameters, so a call site can
            // never tell Begin "dossier" and End "plain" and put the spine through the shoulder.
            _openCardDossier = dossier && _dossierCardStyle.normal.background != null;
            _openCardKind = kind;

            GUILayout.BeginVertical(_openCardDossier
                ? _dossierCardStyle
                : UiPalette.BuildCardStyle(AreaCardFill, AreaCardCornerRadius, AreaCardPadding, AreaCardSpineWidth));
            if (string.IsNullOrEmpty(kind) && !_openCardDossier)
            {
                return;
            }

            if (_openCardDossier)
            {
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                DrawColoredLabel(blocksTime ? "HOLDS TIME" : "CAN WAIT", _cardKindStyle,
                    blocksTime ? PoliSimTheme.Bad : PoliSimTheme.TextMuted);
                GUILayout.EndHorizontal();
                return;
            }

            // ⚠ THE URGENCY CUE WAS MISSING ENTIRELY, not miscoloured - found by capture, 2026-08-10.
            //
            // A Decisions card can be the reason the clock has stopped, and the left column says so
            // loudly ("TIME PAUSED: choose the next Fed Chair"). The card that RESOLVES it said nothing
            // at all. Behaviour 8 asks that a player can always see why time stopped AND which screen
            // fixes it; half of that was on screen, and the half at the fixing end was not.
            //
            // This is the "a highlight is not a ground" class from the Compass ring, one step further
            // out: there, an emphasis colour stopped working when the ground inverted. Here the emphasis
            // was never drawn, so no amount of repointing could have surfaced it - only looking could.
            //
            // Driven by the SAME booleans that build the TIME PAUSED line rather than by a per-card
            // constant, so the two can never disagree and a decision that stops holding time loses its
            // chip without anyone remembering to. All four block in this build, which makes the chip look
            // redundant today - but that is a fact about the build, not about the cue, and board 1d
            // already specifies the CAN WAIT case it will need.
            GUILayout.BeginHorizontal();
            DrawColoredLabel(kind, _cardKindStyle, UiPalette.GetAreaColor(area));
            GUILayout.FlexibleSpace();
            DrawColoredLabel(blocksTime ? "HOLDS TIME" : "CAN WAIT", _cardKindStyle,
                blocksTime ? PoliSimTheme.Bad : PoliSimTheme.TextMuted);
            GUILayout.EndHorizontal();
        }

        /// <summary>Closes a card opened by BeginAreaCard and draws its area spine, using the rect GUILayout just resolved for the whole card - the height isn't knowable until now, which is the entire reason the spine is drawn here rather than up front. For a dossier card (see BeginAreaCard) this is also where the shoulder caption lands, for the same reason: the shoulder is part of the card's own background, and the card has no rect until now.</summary>
        private void EndAreaCard(UiPalette.SystemArea area)
        {
            GUILayout.EndVertical();
            if (Event.current.type == EventType.Repaint)
            {
                Rect cardRect = GUILayoutUtility.GetLastRect();
                if (_openCardDossier)
                {
                    // §A.11's left hue spine, placed against the SPRITE's geometry rather than
                    // DrawCardSpine's symmetric inset: it starts below the shoulder slice (26px, the
                    // fixed top band) and stops above the baked drop shadow (the 15px bottom slice) —
                    // a symmetric inset would run the spine through the transparent corner beside the
                    // shoulder and down the shadow.
                    var spineRect = new Rect(cardRect.x + 3f, cardRect.y + 26f, 6f, Mathf.Max(0f, cardRect.height - 26f - 15f));
                    PoliSimTheme.RoundedBox(spineRect, UiPalette.GetAreaColor(area), 2f);

                    if (!string.IsNullOrEmpty(_openCardKind))
                    {
                        // The shoulder caption: `DOSSIER · <KIND>` in the spec's shoulder ink, at a
                        // FIXED size matched to the shoulder band's native-pixel height (see the
                        // style's construction note). x offset clears the shoulder's own left corner.
                        var shoulderRect = new Rect(cardRect.x + 22f, cardRect.y + 1f, cardRect.width - 44f, 13f);
                        GUI.Label(shoulderRect, $"DOSSIER · {_openCardKind}", _dossierShoulderStyle);
                    }
                }
                else
                {
                    UiPalette.DrawCardSpine(cardRect, area, AreaCardSpineWidth - 1f);
                }
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
            float subTabShare = SubTabShare(availableWidth, 6);
            // Instance #13: one measured row height, shared with the content reserve below.
            float subTabRowHeight = SubTabRowHeight(subTabShare, "Labor Market", "Crime & Justice", "Economic Sectors", "Policy Web", "Trade", "Laws");
            DrawSubCategoryButton("Labor Market", PolicyLawsCategory.LaborMarket, ref _policyLawsCategory, subTabShare, subTabRowHeight);
            DrawSubCategoryButton("Crime & Justice", PolicyLawsCategory.CrimeJustice, ref _policyLawsCategory, subTabShare, subTabRowHeight);
            DrawSubCategoryButton("Economic Sectors", PolicyLawsCategory.Sectors, ref _policyLawsCategory, subTabShare, subTabRowHeight);
            DrawSubCategoryButton("Policy Web", PolicyLawsCategory.PolicyWeb, ref _policyLawsCategory, subTabShare, subTabRowHeight);
            DrawSubCategoryButton("Trade", PolicyLawsCategory.Trade, ref _policyLawsCategory, subTabShare, subTabRowHeight);
            DrawSubCategoryButton("Laws", PolicyLawsCategory.Laws, ref _policyLawsCategory, subTabShare, subTabRowHeight);
            GUILayout.EndHorizontal();
            GUILayout.Space(6f);

            // Step B2: the stats THIS sub-screen's own levers move, directly under its selector so the
            // numbers change with the screen. Measured before it is drawn and subtracted from the
            // content budget below, so it takes space from the tab rather than pushing the content
            // scroll view past the bottom of the tab.
            float statRowWidth = PoliSimWidgets.InnerWidth(availableWidth, _boxStyle) - 8f;
            UiPalette.SystemArea statArea = GetPolicyScreenArea(_policyLawsCategory);
            float statRowHeight = PolicyScreenStatsRenderer.MeasureHeight(statArea, _labelStyle, statRowWidth);
            PolicyScreenStatsRenderer.Draw(statArea, _playerCountry, _labelStyle, statRowWidth);

            // Step 2: the trace panel, directly under the chips it explains - measured and
            // subtracted from the content budget exactly like the stat row itself.
            float policyTraceGapStance = _simulationManager.GetWageGrowthGapAtPeriodOpen(PlayerCountryId);
            // The host's remaining height under the chips (the same budget contentHeight below is
            // cut from) - the panel takes at most its share of it and scrolls for the rest, so a
            // long section can never push the tab's own body off the window (2026-08-25).
            float policyTraceHostHeight = Mathf.Max(0f, availableHeight - _headerStyle.fontSize - subTabRowHeight - 14f - statRowHeight);
            float policyTraceHeight = StatTracePanel.MeasureHeight(_playerCountry, policyTraceGapStance, _labelStyle, statRowWidth, policyTraceHostHeight);
            StatTracePanel.Draw(_playerCountry, policyTraceGapStance, _labelStyle, _labelStyle, statRowWidth, policyTraceHostHeight);

            float contentHeight = Mathf.Max(0f, availableHeight - _headerStyle.fontSize - subTabRowHeight - 14f - statRowHeight - policyTraceHeight);
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
                case PolicyLawsCategory.Laws:
                    // Code-review pass (2026-08-25): NOT wrapped in `GUI.enabled = !_isGameOver` any
                    // more - that disabled the row-select button too, permanently locking whichever
                    // law happened to be selected at end-of-game with no way to browse another one's
                    // (purely informational) detail. Only the actual state-changing action - the
                    // enact/repeal button inside DrawLawDetailPane - is gated on _isGameOver now.
                    DrawLawsTab(contentHeight, availableWidth);
                    break;
            }
            GUILayout.EndVertical();
        }

        private static int? _crimeJusticeLawCountCache;

        /// <summary>Code-review pass (2026-08-25): computed once and cached rather than rescanning
        /// LawCatalog.All's 38 entries on every single OnGUI pass - the catalog's contents never
        /// change at runtime, so the count that was being recomputed every frame can never change
        /// during a session either.</summary>
        private static int CrimeJusticeLawCount
        {
            get
            {
                if (_crimeJusticeLawCountCache == null)
                {
                    int count = 0;
                    foreach (LawDefinition law in LawCatalog.All)
                    {
                        if (law.Category == LawCategory.CrimeJustice) { count++; }
                    }
                    _crimeJusticeLawCountCache = count;
                }

                return _crimeJusticeLawCountCache.Value;
            }
        }

        private static int? _laborMarketLawCountCache;

        /// <summary>The labor sibling of CrimeJusticeLawCount - same once-per-session cache
        /// reasoning; both feed the returned category chip row's counts (pass 3, 2026-08-26 -
        /// which also gave CrimeJusticeLawCount its first call site).</summary>
        private static int LaborMarketLawCount
        {
            get
            {
                if (_laborMarketLawCountCache == null)
                {
                    int count = 0;
                    foreach (LawDefinition law in LawCatalog.All)
                    {
                        if (law.Category == LawCategory.LaborMarket) { count++; }
                    }
                    _laborMarketLawCountCache = count;
                }

                return _laborMarketLawCountCache.Value;
            }
        }

        /// <summary>
        /// Law system MVP slice, REBUILT 2026-08-25 twice: first against §7's own scale argument,
        /// then against Design's own board 1i ruling (LAW_BROWSER_BOARD_RULINGS.md, delivered as
        /// Progress4) - four real cells (name/category/magnitude/cost) plus a status GUTTER rather
        /// than a status column, status carried instead by GROUPING with counts (IN FORCE first,
        /// the marathon's own capture-evidenced fix, structural rather than an added control), a
        /// sticky column header inside the scroller, and the magnitude taxonomy finally given a
        /// visual (a four-step stepped rule, no new hue, no new sprite - see DrawMagnitudeSteps).
        /// The category filter's own inertness is UNCHANGED by any of this - see LawBrowserFilter's
        /// doc comment for the cause - and board 1i's own count on the two real chips is the
        /// legibility half of that fix, not the bug fix; the two stay separate on purpose.
        /// </summary>
        private void DrawLawsTab(float availableHeight, float availableWidth)
        {
            // Code-review pass (2026-08-25): the staged selection commits HERE, on the Layout event
            // only - see _pendingSelectedLawId's own doc comment. Placed before anything else in the
            // method so every downstream read of _selectedLawId this frame (the stale-selection guard
            // below, the detail-pane lookup) already sees the committed value, exactly like
            // StatTracePanel.MeasureHeight commits before its own callers read _selected.
            if (_hasPendingLawSelection && Event.current != null && Event.current.type == EventType.Layout)
            {
                _selectedLawId = _pendingSelectedLawId;
                _hasPendingLawSelection = false;
            }

            GUILayout.BeginVertical(_boxStyle);
            // Pass 3: Neutral, not CrimeJustice - the browser spans two categories now; see
            // GetPolicyScreenArea's Laws case for the reasoning. Per-row accents carry category.
            DrawColoredLabel("Laws", _headerStyle, UiPalette.GetAreaColor(UiPalette.SystemArea.Neutral));
            // Free-aspect pass (2026-08-26): EXPLICIT width. This width-less label was the root of
            // the playtest's overflow class: CalcSize ignores wordWrap with no width given, so the
            // label requested its NATURAL ~full-sentence width and silently stretched the whole
            // box past the window at free-aspect sizes (1640x707 observed) - pushing the sub-tab
            // row's last child ("Law...") off-screen and dragging every ExpandWidth sibling wide
            // with it. At the capture sizes the stretch hid off-screen (the text clipped at the box
            // edge in every 2560 capture, guard-silent - a width-less label's rect IS its natural
            // size, so UiOverflowGuard cannot see it). The same fix this codebase has recorded
            // twice before, at a new site.
            float lawsInnerWidth = PoliSimWidgets.InnerWidth(availableWidth, _boxStyle, 1, _labelStyle);
            GUILayout.Label("Named presets over the existing dial space, not bespoke effects - a law's dial deltas are the same terms the Crime & Justice and Labor Market tabs track. Enacting or repealing submits a bill exactly like any other; nothing happens until Parliament resolves it.", _labelStyle, GUILayout.Width(lawsInnerWidth));
            GUILayout.Space(6f);

            // Board 1j (2026-08-26, §7.1's answer) said THE CATEGORY CHIPS STEP DOWN while one
            // category held everything, and promised: "The chip row returns, hatched counts and
            // all, the day a second LawCategory ships - at which point _lawBrowserFilter (kept,
            // still honest state) gets its buttons back." PASS 3 (same day) IS THAT DAY: the
            // LaborMarket category shipped, the chips are back in 1i's own counted form
            // ("All - N, Crime & Justice - N, Labor Market - N"), and the filter genuinely
            // narrows for the first time - the inertness bug the 1i/1j notes kept separate is
            // closed by content, exactly as LawBrowserFilter's doc predicted ("the fix for that
            // is more law CATEGORIES, not a UI change"). No hatched "- 0" chips render because
            // every LawCategory member is populated (1i's five hatched chips were drawn
            // categories that never entered the enum). The summary line below drops its
            // "all CRIME & JUSTICE" clause - the chips carry the per-category counts now.
            GUILayout.BeginHorizontal();
            float categoryShare = SubTabShare(availableWidth, 3);
            string allChipLabel = $"All - {LawCatalog.All.Count}";
            string crimeChipLabel = $"Crime & Justice - {CrimeJusticeLawCount}";
            string laborChipLabel = $"Labor Market - {LaborMarketLawCount}";
            float categoryRowHeight = SubTabRowHeight(categoryShare, allChipLabel, crimeChipLabel, laborChipLabel);
            DrawSubCategoryButton(allChipLabel, LawBrowserFilter.All, ref _lawBrowserFilter, categoryShare, categoryRowHeight);
            DrawSubCategoryButton(crimeChipLabel, LawBrowserFilter.CrimeJustice, ref _lawBrowserFilter, categoryShare, categoryRowHeight);
            DrawSubCategoryButton(laborChipLabel, LawBrowserFilter.LaborMarket, ref _lawBrowserFilter, categoryShare, categoryRowHeight);
            GUILayout.EndHorizontal();
            // Free-aspect pass (2026-08-26): the ORDER row's minimum (caption + three measured
            // button floors + the search slot) is MEASURED against the box's inner width, and the
            // search slot reflows onto the summary line when the one-row form doesn't fit - at the
            // 1280x720 floor the one-row minimum (~640px) exceeded the inner width (~585px), and
            // an overflowing row stretches every ExpandWidth sibling in the box (the "L|"/
            // "Availabl|" cuts in the floor sweep). The bucket is a pure function of window size,
            // so Layout and Repaint always agree within a frame.
            float orderCaptionWidth = _labelStyle.CalcSize(new GUIContent("ORDER - STATUS, THEN")).x + 6f;
            GUIStyle orderButtonProbe = BuildSubTabStyle(true);
            float orderButtonsWidth = PoliSimWidgets.MeasuredWidth("Magnitude", orderButtonProbe, orderButtonProbe.padding.horizontal + 6f)
                + PoliSimWidgets.MeasuredWidth("A-Z", orderButtonProbe, orderButtonProbe.padding.horizontal + 6f)
                + PoliSimWidgets.MeasuredWidth("Cost", orderButtonProbe, orderButtonProbe.padding.horizontal + 6f);
            float searchLabelWidth = _labelStyle.CalcSize(new GUIContent("SEARCH")).x + 6f;
            float searchFieldWidth = _labelStyle.fontSize * 7f;
            bool searchInline = orderCaptionWidth + orderButtonsWidth + searchLabelWidth + searchFieldWidth + 40f <= lawsInnerWidth;

            GUILayout.BeginHorizontal();
            float summaryWidth = searchInline
                ? lawsInnerWidth
                : Mathf.Max(_labelStyle.fontSize * 6f, lawsInnerWidth - searchLabelWidth - searchFieldWidth - 16f);
            GUILayout.Label($"{LawCatalog.All.Count} laws - {_playerCountry.EnactedLaws.Count} in force - {CountPendingLawBills()} before the house", _labelStyle, GUILayout.Width(summaryWidth));
            if (!searchInline)
            {
                GUILayout.FlexibleSpace();
                DrawLawSearchSlot(searchLabelWidth, searchFieldWidth);
            }
            GUILayout.EndHorizontal();

            // The caption carries "STATUS, THEN" once and the buttons carry only the variant word
            // - the second capture caught the board's full three-phrase labels summing past the
            // panel's width budget at BOTH sizes (min-widths widen the whole box silently; the
            // sub-tab strip above clipped at the window edge, which no guard measures).
            GUILayout.BeginHorizontal();
            GUILayout.Label("ORDER - STATUS, THEN", _labelStyle, GUILayout.Width(orderCaptionWidth));
            DrawSubCategoryButton("Magnitude", LawOrder.Magnitude, ref _lawOrder);
            DrawSubCategoryButton("A-Z", LawOrder.Alphabetical, ref _lawOrder);
            DrawSubCategoryButton("Cost", LawOrder.Cost, ref _lawOrder);
            GUILayout.FlexibleSpace();
            if (searchInline)
            {
                DrawLawSearchSlot(searchLabelWidth, searchFieldWidth);
            }
            GUILayout.EndHorizontal();

            // Free-aspect pass (2026-08-26): share-capped like every other chip row - without
            // maxWidth these four floors are each label's own measured width, and at the 1280x720
            // floor their sum overran the row ("Availab|" cut at the box edge in the enumeration
            // capture). SubTabShare/SubTabRowHeight is the sub-tab rows' own established pair.
            GUILayout.BeginHorizontal();
            float statusShare = SubTabShare(availableWidth, 4);
            float statusRowHeight = SubTabRowHeight(statusShare, "All statuses", "Enacted", "Pending", "Available");
            DrawSubCategoryButton("All statuses", LawStatusFilter.All, ref _lawStatusFilter, statusShare, statusRowHeight);
            DrawSubCategoryButton("Enacted", LawStatusFilter.Enacted, ref _lawStatusFilter, statusShare, statusRowHeight);
            DrawSubCategoryButton("Pending", LawStatusFilter.Pending, ref _lawStatusFilter, statusShare, statusRowHeight);
            DrawSubCategoryButton("Available", LawStatusFilter.Available, ref _lawStatusFilter, statusShare, statusRowHeight);
            GUILayout.EndHorizontal();
            GUILayout.Space(6f);

            // Three partitions, catalog order preserved within each (List<T> guarantees neither a
            // stable sort nor an unstable one, so building three lists in one pass sidesteps the
            // question rather than trusting one). IN FORCE first - the marathon's own evidenced
            // failure, fixed by construction. Code-review pass (2026-08-25): Clear()'d reusable
            // fields rather than `new List<>()` every call, and each entry carries its own
            // enacted/pending state (computed once here) instead of every consumer re-deriving it.
            _lawEnactedRows.Clear();
            _lawPendingRows.Clear();
            _lawAvailableRows.Clear();
            foreach (LawDefinition law in LawCatalog.All)
            {
                if (_lawBrowserFilter == LawBrowserFilter.CrimeJustice && law.Category != LawCategory.CrimeJustice)
                {
                    continue;
                }

                if (_lawBrowserFilter == LawBrowserFilter.LaborMarket && law.Category != LawCategory.LaborMarket)
                {
                    continue;
                }

                // Board 1j: the search slot - a plain case-insensitive name-contains filter.
                if (!string.IsNullOrEmpty(_lawSearchText)
                    && law.Name.IndexOf(_lawSearchText, System.StringComparison.OrdinalIgnoreCase) < 0)
                {
                    continue;
                }

                bool lawEnacted = _playerCountry.EnactedLaws.Exists(e => e.LawId == law.Id);
                LawBill pendingBill = _simulationManager.GetPendingLawBill(PlayerCountryId, law.Id);
                LawStatusFilter status = pendingBill != null ? LawStatusFilter.Pending : lawEnacted ? LawStatusFilter.Enacted : LawStatusFilter.Available;
                if (_lawStatusFilter != LawStatusFilter.All && _lawStatusFilter != status)
                {
                    continue;
                }

                LawRowEntry entry = new LawRowEntry(law, lawEnacted, pendingBill);
                if (pendingBill != null) { _lawPendingRows.Add(entry); }
                else if (lawEnacted) { _lawEnactedRows.Add(entry); }
                else { _lawAvailableRows.Add(entry); }
            }

            // Board 1j: STATUS, THEN {order} - the within-group sort. Magnitude descends (SWEEPING
            // first inside enacted/pending; AVAILABLE's bands themselves descend), A-Z and cost
            // ascend. List<T>.Sort is unstable, so every comparison ties back to Name - the
            // catalog-order question the three-partition build sidestepped is settled by making
            // the order fully specified instead.
            System.Comparison<LawRowEntry> byOrder =
                _lawOrder == LawOrder.Alphabetical ? (a, b) => string.CompareOrdinal(a.Law.Name, b.Law.Name)
                : _lawOrder == LawOrder.Cost ? (a, b) =>
                    {
                        int c = a.Law.EnactmentApprovalCost.CompareTo(b.Law.EnactmentApprovalCost);
                        return c != 0 ? c : string.CompareOrdinal(a.Law.Name, b.Law.Name);
                    }
                : (a, b) =>
                    {
                        int c = LawMagnitudeTier(b.Law).CompareTo(LawMagnitudeTier(a.Law));
                        return c != 0 ? c : string.CompareOrdinal(a.Law.Name, b.Law.Name);
                    };
            _lawEnactedRows.Sort(byOrder);
            _lawPendingRows.Sort(byOrder);
            _lawAvailableRows.Sort(byOrder);

            _lawVisibleRows.Clear();
            _lawVisibleRows.AddRange(_lawEnactedRows);
            _lawVisibleRows.AddRange(_lawPendingRows);
            _lawVisibleRows.AddRange(_lawAvailableRows);

            // Code-review pass (2026-08-25): reassigns whenever the selection isn't in the CURRENTLY
            // VISIBLE (filtered) set, not merely whenever it no longer exists in the whole catalog.
            // The previous guard only checked LawCatalog.GetById, so switching to a status/category
            // chip that excludes the selected law left the detail pane showing (with a live action
            // button) a law that had disappeared from the list beside it.
            int selectedIndex = _lawVisibleRows.FindIndex(e => e.Law.Id == _selectedLawId);
            if (selectedIndex < 0)
            {
                _selectedLawId = _lawVisibleRows.Count > 0 ? _lawVisibleRows[0].Law.Id : null;
                selectedIndex = _lawVisibleRows.Count > 0 ? 0 : -1;
            }

            // List (scrolls) + detail (bounded to the same height - see _lawDetailScrollPosition's
            // own doc comment on why that's a deliberate, narrow departure from "does not scroll").
            GUILayout.BeginHorizontal();

            // Free-aspect pass (2026-08-26): the split derives from the BOX'S INNER width, not the
            // outer availableWidth - the outer figure spent the box's own ~28px horizontal padding
            // a second time, so list+space+pane overflowed the box by that constant at EVERY size:
            // invisible inside the window margin at 1600/2560 (the "Laws" sub-tab's long-standing
            // edge-kiss was this), visible as "Availabl|"/"L|" cuts at the 1280 floor, where
            // ExpandWidth siblings stretched to the widened parent and the last child paid.
            float listWidth = lawsInnerWidth * 0.56f;
            GUILayout.BeginVertical(GUILayout.Width(listWidth));

            // The sticky header: drawn ONCE, outside the scroll view, at the scrollbar-adjusted row
            // width - board 1i's own named hazard ("a header sibling above the scroller misaligns
            // by the scrollbar width"). LawRowColumns is the one function both this and every row
            // call, so the two cannot independently drift the way two restatements of one layout
            // always eventually do in this codebase. Code-review pass (2026-08-25): the list's own
            // scroll view now forces `alwaysShowVertical: true` below, so this subtraction is correct
            // on EVERY frame rather than only when the list happens to be long enough to need a
            // scrollbar - previously a short filtered list (e.g. 1-2 "Pending" rows) rendered full-
            // width rows under a narrower header.
            float headerRowWidth = Mathf.Max(0f, listWidth - GUI.skin.verticalScrollbar.fixedWidth - 12f);
            Rect headerRect = GUILayoutUtility.GetRect(10f, LedgerRow.Height(_labelStyle), GUILayout.ExpandWidth(true));
            DrawLawRowHeader(new Rect(headerRect.x, headerRect.y, headerRowWidth, headerRect.height));
            GUILayout.Space(2f);

            // Code-review pass (2026-08-25): floored at 0 - DrawPolicyLawsTab's own contentHeight can
            // legitimately reach 0 (it floors itself the same way), and this term is a 7.5x outlier
            // against every sibling tab's `fontSize*2f` precisely because it now budgets for the two
            // filter rows above as well as the bottom bar below - unclamped, a short window fed
            // GUILayout.Height a large negative number with nothing catching it.
            float scrollHeight = Mathf.Max(0f, availableHeight - _labelStyle.fontSize * 15f);
            _lawsScrollPosition = GUILayout.BeginScrollView(_lawsScrollPosition, false, true, GUILayout.Height(scrollHeight));
            DrawLawStatusGroup("IN FORCE", _lawEnactedRows);
            DrawLawStatusGroup("BEFORE THE HOUSE", _lawPendingRows);
            DrawLawAvailableGroup(_lawAvailableRows);
            GUILayout.EndScrollView();
            GUILayout.EndVertical();

            GUILayout.Space(10f);

            // Elias's ruling (2026-08-25): the detail pane's own width was never actually
            // constrained - unlike the list column three lines above (GUILayout.Width(listWidth)),
            // this BeginVertical had no width option at all, so GUILayout sized it by CONTENT
            // instead of by its nominal 44% share.
            //
            // Two real bugs were behind the one symptom, found in sequence by instrumenting rather
            // than re-theorizing after the first fix didn't change the capture: (1) the outer
            // BeginVertical alone doesn't clip - only the ScrollView's own GUIClip group does, and it
            // clips to whatever width IT was told, which nothing set explicitly before. Confirmed
            // fixed structurally by direct Debug.Log of GUILayoutUtility.GetLastRect(): the pane's own
            // background genuinely stops at its correct x, verified pixel-for-pixel against a crop of
            // the boundary - clipping was never the remaining problem. (2) Inside that now-correctly-
            // clipped region, DrawLawDetailPane's plain `GUILayout.Label(text, _labelStyle)` calls
            // still requested their NATURAL, UNWRAPPED single-line width (GUIStyle.CalcSize ignores
            // wordWrap when no width is given) - so text kept extending past the clip and simply
            // disappearing there, which read identically to "nothing was fixed" until the two
            // failures were told apart by inspecting the actual rendered rect, not by looking at the
            // screenshot alone. Fixed by threading this pane's real content width into
            // DrawLawDetailPane and giving every wrapping label an explicit GUILayout.Width - the
            // width CalcHeight/wordWrap actually need to do their job, not merely a container that
            // happens to clip whatever they overflow into.
            float detailPaneWidth = Mathf.Max(0f, lawsInnerWidth - listWidth - 10f);
            GUILayout.BeginVertical(GUILayout.Width(detailPaneWidth));
            _lawDetailScrollPosition = GUILayout.BeginScrollView(_lawDetailScrollPosition,
                GUILayout.Width(detailPaneWidth), GUILayout.Height(scrollHeight));
            DrawLawDetailPane(selectedIndex >= 0 ? _lawVisibleRows[selectedIndex] : (LawRowEntry?)null,
                Mathf.Max(0f, detailPaneWidth - GUI.skin.verticalScrollbar.fixedWidth - 12f));
            GUILayout.EndScrollView();
            GUILayout.EndVertical();

            GUILayout.EndHorizontal();

            DrawLawBottomBar(_lawVisibleRows, lawsInnerWidth);

            GUILayout.EndVertical();
        }

        /// <summary>Board 1i's row grid (glyph gutter, name, category, magnitude, cost), derived as
        /// PROPORTIONS of the row's own width rather than the delivered spec's literal "128px/132px/
        /// 74px" at a 1920-wide reference - copying those directly would be the mockup-number trap
        /// this codebase has already hit nine times (see CLAUDE.md, "the mockup-number rule"): the
        /// list panel here is nowhere near 1920px wide, and every style in this UI rescales with
        /// Screen.height besides. The fractions below are proportioned FROM the spec's own ratios
        /// (128:132:74 for category:magnitude:cost), not copied wholesale. Called by both the sticky
        /// header and every row so the two share one source of truth.
        ///
        /// Code-review pass (2026-08-25): given a floor (LedgerRow.Columns' own precedent) and a
        /// squeeze-to-fit when the floors don't sum within the row - the original version floored
        /// only nameWidth, so a small window or large font scale could shrink category/magnitude/cost
        /// toward unreadable or zero width with nothing catching it.
        ///
        /// ⚠ FIRST CUT OF THIS FLOOR (6f/6.5f/3f multipliers) WAS ITSELF A REGRESSION, caught by the
        /// verification capture immediately after writing it, not by inspection: at this tab's real
        /// operating width (1600x929, listWidth ~445px at fontSize 20 - Screen.height 929 ->
        /// RescaleStylesToScreen's own clamp(round(929*0.022),16,28)=20), those multipliers exceeded
        /// EVERY ONE of the three proportional values, so the floor bound unconditionally and starved
        /// nameWidth down to ~113px - 68 law names overflowed it (UiOverflowGuard, "needs 132-171px in
        /// 112.8"). The exact mistake this file's own "mockup-number rule" warns about, just aimed at
        /// a floor instead of a literal: a number picked without checking it against a real capture.
        /// Recalibrated to sit BELOW the proportional values at this verified resolution (confirmed:
        /// the same capture re-run at 0 text overflows afterward) - still a real floor against zero at
        /// a much narrower window, just not one that binds at the one width this codebase has
        /// actually measured. A narrower-window capture is still open, same as board 1i's own five
        /// unpopulated categories.</summary>
        private static void LawRowColumns(float rowWidth, GUIStyle style, out float glyphWidth, out float nameWidth, out float categoryWidth, out float magnitudeWidth, out float costWidth)
        {
            // Board 1j (2026-08-26) retired THE CATEGORY CELL while one category held everything,
            // and recorded: "It returns as a cell the day a second LawCategory ships, which is
            // also the day the chip row returns." Pass 3 (same day) is that day - the cell is
            // back at the 1i spec's own ratio share (128:132:74 for category:magnitude:cost,
            // re-derived as proportions per the mockup-number rule: category = magnitude's 0.20
            // x 128/132 ≈ 0.19), its width coming back out of the name that had absorbed it.
            // Floors and squeeze keep the 1i code-review discipline - the category floor sits
            // BELOW its proportional value at the verified 1600x929 operating width (~85px vs a
            // 50px floor at fontSize 20), per the recorded floor-regression lesson; the same
            // narrower-window caveat stands.
            glyphWidth = Mathf.Min(14f, rowWidth * 0.03f);

            float fontFloor = Mathf.Max(1f, style.fontSize);
            categoryWidth = Mathf.Max(rowWidth * 0.19f, fontFloor * 2.5f);
            magnitudeWidth = Mathf.Max(rowWidth * 0.20f, fontFloor * 3f);
            costWidth = Mathf.Max(rowWidth * 0.13f, fontFloor * 2f);

            float fixedTotal = categoryWidth + magnitudeWidth + costWidth;
            float availableForFixed = Mathf.Max(0f, rowWidth - glyphWidth);
            if (fixedTotal > availableForFixed && fixedTotal > 0f)
            {
                float squeeze = Mathf.Max(0.35f, availableForFixed / fixedTotal);
                categoryWidth *= squeeze;
                magnitudeWidth *= squeeze;
                costWidth *= squeeze;
            }

            nameWidth = Mathf.Max(0f, rowWidth - glyphWidth - categoryWidth - magnitudeWidth - costWidth);
        }

        /// <summary>The sticky header's own row - column captions only, muted, never interactive (a
        /// label row has no control to keep stable-control-layout safe in the first place).</summary>
        private void DrawLawRowHeader(Rect rect)
        {
            // Board 1j simplified the header to STATUTE / APPROVAL while the category cell was
            // retired. Pass 3 (the cell's return): STATUTE spans the name field, CATEGORY
            // captions the returned cell, APPROVAL keeps the cost cell ("a budget, not a label").
            // The magnitude column stays uncaptioned - the stepped rule is self-carrying and the
            // AVAILABLE bands name the class; re-captioning it would re-add the noise 1j cut.
            LawRowColumns(rect.width, _labelStyle, out float glyphWidth, out float nameWidth, out float categoryWidth, out float magnitudeWidth, out float costWidth);
            float x = rect.x + glyphWidth;
            LedgerRow.Cell(new Rect(x, rect.y, nameWidth, rect.height), "STATUTE", _labelStyle, PoliSimTheme.TextMuted, TextAnchor.MiddleLeft);
            x += nameWidth;
            LedgerRow.Cell(new Rect(x, rect.y, categoryWidth, rect.height), "CATEGORY", _labelStyle, PoliSimTheme.TextMuted, TextAnchor.MiddleLeft);
            x += categoryWidth + magnitudeWidth;
            // Code-review pass (2026-08-25): -4f to match the row's own cost cell exactly (both
            // right-anchored) - previously the header used the full costWidth while the row used
            // costWidth-4f, so their right edges (and the caption above the values) sat 4px apart on
            // every frame, independent of any scrollbar-width consideration.
            //
            // Free-aspect pass (2026-08-26): "APPROVAL" gets a curated abbreviation at narrow
            // widths (the D7 idiom - never clip, never uniform-shrink a caption to illegibility).
            // The threshold is MEASURED, not guessed: the 1280x720 floor sweep's own overflow line
            // ("needs 42.5 wide in 39.9 at 8px") - below that need, the full word cannot fit at
            // any legible size.
            string approvalCaption = costWidth - 4f >= 44f ? "APPROVAL" : "APPR.";
            LedgerRow.Cell(new Rect(x, rect.y, costWidth - 4f, rect.height), approvalCaption, _labelStyle, PoliSimTheme.TextMuted, TextAnchor.MiddleRight);
        }

        /// <summary>One status partition inside the scroller - a plain, non-interactive group
        /// caption with its own count ("IN FORCE - 8"), then its rows; omitted entirely when empty
        /// (which is what makes an active status FILTER and the always-three-groups "All statuses"
        /// view the same code path - a filtered-out group is just an empty list here).</summary>
        private void DrawLawStatusGroup(string label, List<LawRowEntry> rows)
        {
            if (rows.Count == 0)
            {
                return;
            }

            GUILayout.Label($"{label} - {rows.Count}", _labelStyle);
            foreach (LawRowEntry entry in rows)
            {
                DrawLawListRow(entry);
            }
            GUILayout.Space(6f);
        }

        /// <summary>
        /// Board 1j (2026-08-26): AVAILABLE is no longer one undifferentiated block. Under the
        /// default STATUS-THEN-MAGNITUDE order it renders as four weight-class BANDS - the stepped
        /// rule promotes from forty repeating row cells to four band headers, where it reads as an
        /// ordinal again instead of blurring into column texture, and each band names its class and
        /// dial-movement range. Under A-Z/cost order the group is flat (the band structure IS the
        /// magnitude ordering, so it only exists in that mode). Rows inside bands are the compact
        /// three-cell variant. The board draws the band headers position:sticky at a second level;
        /// IMGUI's scroll view has no sticky, so they are plain caption rows - the same adaptation
        /// the 1i sticky column header took (moved outside the scroller), stated not silent; the
        /// column header keeps that treatment, the bands ride the scroll.
        /// </summary>
        private void DrawLawAvailableGroup(List<LawRowEntry> rows)
        {
            if (rows.Count == 0)
            {
                return;
            }

            GUILayout.Label($"AVAILABLE - {rows.Count}", _labelStyle);

            if (_lawOrder != LawOrder.Magnitude)
            {
                foreach (LawRowEntry entry in rows)
                {
                    DrawLawListRow(entry, compact: true);
                }
                GUILayout.Space(6f);
                return;
            }

            // Rows arrive sorted tier-descending (the order comparator), so one pass per tier is a
            // contiguous slice; empty bands are omitted (the empty-group rule).
            for (int tier = 4; tier >= 1; tier--)
            {
                int inBand = 0;
                foreach (LawRowEntry entry in rows)
                {
                    if (LawMagnitudeTier(entry.Law) == tier) { inBand++; }
                }
                if (inBand == 0)
                {
                    continue;
                }

                DrawLawMagnitudeBandCaption(tier, inBand);
                foreach (LawRowEntry entry in rows)
                {
                    if (LawMagnitudeTier(entry.Law) == tier)
                    {
                        DrawLawListRow(entry, compact: true);
                    }
                }
            }
            GUILayout.Space(6f);
        }

        /// <summary>One band header: the stepped rule at the band's own tier, the class name, and
        /// its dial-movement range - "the class you are reading is always named on screen."</summary>
        private void DrawLawMagnitudeBandCaption(int tier, int count)
        {
            Rect rect = GUILayoutUtility.GetRect(10f, LedgerRow.Height(_labelStyle) * 1.2f, GUILayout.ExpandWidth(true));
            if (Event.current.type == EventType.Repaint)
            {
                Color previous = GUI.color;
                GUI.color = new Color(0f, 0f, 0f, 0.06f);
                GUI.DrawTexture(rect, Texture2D.whiteTexture);
                GUI.color = previous;
            }

            float stepWidth = Mathf.Max(4f, _labelStyle.fontSize * 0.35f);
            float stepGap = stepWidth * 0.3f;
            float stepsRun = stepWidth * 4f + stepGap * 3f;
            float stepHeight = rect.height * 0.5f;
            DrawMagnitudeSteps(new Rect(rect.x + 4f, rect.y + (rect.height - stepHeight) * 0.5f, stepsRun, stepHeight), tier, stepWidth, stepGap);

            LedgerRow.Cell(new Rect(rect.x + stepsRun + 12f, rect.y, rect.width - stepsRun - 16f, rect.height),
                $"{LawMagnitudeLabel(tier)} - {count} available - dial movement {LawMagnitudeRangeLabel(tier)}",
                _labelStyle, PoliSimTheme.TextPrimary, TextAnchor.MiddleLeft);
        }

        /// <summary>The taxonomy's own per-tier dial-movement range (LawCatalog's class doc:
        /// MINOR ±3-6, MODERATE ±7-14, MAJOR ±15-22, SWEEPING ±23-30). Bounds derive from
        /// LawCatalog's three named constants; the taxonomy's outer 3 and 30 are its documented
        /// floor and ceiling (the catalog's own doc comment commits to both), not layout numbers.</summary>
        private static string LawMagnitudeRangeLabel(int tier)
        {
            switch (tier)
            {
                case 1: return $"±3-{LawCatalog.MinorMagnitudeMax:0}";
                case 2: return $"±{LawCatalog.MinorMagnitudeMax + 1f:0}-{LawCatalog.ModerateMagnitudeMax:0}";
                case 3: return $"±{LawCatalog.ModerateMagnitudeMax + 1f:0}-{LawCatalog.MajorMagnitudeMax:0}";
                default: return $"±{LawCatalog.MajorMagnitudeMax + 1f:0}-30";
            }
        }

        /// <summary>
        /// One compact, decidable-at-a-glance row: name, category, magnitude, cost - board 1i's own
        /// four cells, status carried by the GROUP this row sits under rather than a fifth column
        /// (the gutter bar is a per-row accent, not a second readout of the same fact). Clicking
        /// anywhere on the row selects it for the detail pane, via an invisible full-row button (the
        /// deferred-visual idiom the folder-tongue tab's own active-tongue paint already
        /// established) - control count and order are identical every frame regardless of
        /// selection, since the LIST of laws is static content, not background state; only a
        /// genuinely new/removed law (a content change, not a turn-to-turn one) would move this
        /// list's own control count, the same "moves because the player acted or because content
        /// shipped, never because a background timer did" distinction DrawPageRow's own fix drew.
        /// </summary>
        private void DrawLawListRow(LawRowEntry row, bool compact = false)
        {
            LawDefinition law = row.Law;
            bool selected = _selectedLawId == law.Id;

            Rect rowRect = GUILayoutUtility.GetRect(10f, LedgerRow.Height(_labelStyle) * 1.4f, GUILayout.ExpandWidth(true));

            // Code-review pass (2026-08-25): stages the click into _pendingSelectedLawId instead of
            // writing _selectedLawId directly - see that field's own doc comment for why (the
            // StatTracePanel precedent for a mid-frame GUILayout control-count mismatch).
            if (GUI.Button(rowRect, string.Empty, GUIStyle.none))
            {
                _pendingSelectedLawId = law.Id;
                _hasPendingLawSelection = true;
            }

            // Pass 3: the row accent carries the LAW'S OWN category area now that two categories
            // share the list (see LawCategoryArea).
            Color areaColor = UiPalette.GetAreaColor(LawCategoryArea(law.Category));
            if (Event.current.type == EventType.Repaint)
            {
                if (selected)
                {
                    Color previous = GUI.color;
                    GUI.color = new Color(areaColor.r, areaColor.g, areaColor.b, 0.18f);
                    GUI.DrawTexture(rowRect, Texture2D.whiteTexture);
                    GUI.color = previous;
                }

                Color glyphColor = row.PendingBill != null ? PoliSimTheme.TextSecondary : row.Enacted ? areaColor : PoliSimTheme.TextMuted;
                Color previousGlyph = GUI.color;
                GUI.color = glyphColor;
                GUI.DrawTexture(new Rect(rowRect.x + 2f, rowRect.y + 3f, 4f, rowRect.height - 6f), Texture2D.whiteTexture);
                GUI.color = previousGlyph;
            }

            LawRowColumns(rowRect.width, _labelStyle, out float glyphWidth, out float nameWidth, out float categoryWidth, out float magnitudeWidth, out float costWidth);
            float x = rowRect.x + glyphWidth;

            // Board 1j: BEFORE THE HOUSE rows name their real countdown in the row itself - the
            // board's VOTE IN chip, carried as a suffix in the name cell (an IMGUI adaptation of
            // the drawn bordered tag; the datum - LawBill.DaysRemaining - is the real countdown,
            // not the board's "next sitting," which stays unbuilt for the recorded reason).
            string rowName = row.PendingBill != null
                ? $"{law.Name} - VOTE IN {row.PendingBill.DaysRemaining}d"
                : law.Name;

            if (compact)
            {
                // Board 1j: the AVAILABLE row's magnitude lives in the band header above it, so
                // the name takes that field. Pass 3 (the category cell's return): the compact row
                // gains the category token too - drawn where the full row's category+magnitude
                // budget ends, right beside cost, so the name keeps 1j's widened field and the
                // token right-aligns consistently WITHIN the group. A stated IMGUI adaptation
                // (compact and full rows place the token at different x), acceptable because the
                // two variants never interleave inside one status group and the bands re-anchor
                // the eye between groups.
                // Pass 3 floor fix: the wrap-first NAME ladder, not the shrink-only cell - long
                // statute names wrap to a second line at the 1280 floor instead of shrinking
                // past the 8px guard floor (the row's 1.4x height already holds two lines).
                LedgerRow.NameCell(new Rect(x, rowRect.y, nameWidth + magnitudeWidth - 4f, rowRect.height), rowName, _labelStyle, PoliSimTheme.TextPrimary);
                x += nameWidth + magnitudeWidth;
                LedgerRow.Cell(new Rect(x, rowRect.y, categoryWidth - 4f, rowRect.height), LawCategoryCellLabel(law.Category, categoryWidth - 4f), _labelStyle, PoliSimTheme.TextMuted, TextAnchor.MiddleLeft);
                x += categoryWidth;
            }
            else
            {
                // Pass 3 floor fix: wrap-first name ladder - see the compact branch's comment.
                LedgerRow.NameCell(new Rect(x, rowRect.y, nameWidth - 4f, rowRect.height), rowName, _labelStyle, PoliSimTheme.TextPrimary);
                x += nameWidth;

                // Pass 3: the returned category cell - 1i's "dimmed token" ink (TextMuted), never
                // a second accent (the glyph bar already carries the category's area color).
                LedgerRow.Cell(new Rect(x, rowRect.y, categoryWidth - 4f, rowRect.height), LawCategoryCellLabel(law.Category, categoryWidth - 4f), _labelStyle, PoliSimTheme.TextMuted, TextAnchor.MiddleLeft);
                x += categoryWidth;

                Rect magnitudeRect = new Rect(x, rowRect.y, magnitudeWidth, rowRect.height);
                int tier = LawMagnitudeTier(law);
                float stepWidth = magnitudeWidth * 0.12f;
                float stepGap = stepWidth * 0.3f;
                float stepHeight = rowRect.height * 0.32f;
                DrawMagnitudeSteps(new Rect(magnitudeRect.x, magnitudeRect.y + (magnitudeRect.height - stepHeight) * 0.5f, magnitudeWidth, stepHeight), tier, stepWidth, stepGap);
                x += magnitudeWidth;
            }

            LedgerRow.Cell(new Rect(x, rowRect.y, costWidth - 4f, rowRect.height),
                law.EnactmentApprovalCost.ToString("F1", CultureInfo.InvariantCulture), _labelStyle, PoliSimTheme.TextSecondary, TextAnchor.MiddleRight);
        }

        /// <summary>Code-review pass (2026-08-25): the one place a LawCategory maps to its display
        /// token, replacing a hardcoded "CRIME & JUSTICE" literal in DrawLawListRow that was
        /// independent of the law passed in. Explicit per-value rather than
        /// `category.ToString().ToUpperInvariant()` alone so a real display name (with its "&amp;")
        /// stays under this method's control even as more categories ship.</summary>
        private static string LawCategoryLabel(LawCategory category)
        {
            switch (category)
            {
                case LawCategory.CrimeJustice: return "CRIME & JUSTICE";
                case LawCategory.LaborMarket: return "LABOR MARKET";
                default: return category.ToString().ToUpperInvariant();
            }
        }

        /// <summary>The category CELL's token at the width it actually has (pass 3's floor sweep,
        /// 2026-08-26): below a measured ~70px the full token cannot fit even at MeasuredLabel's
        /// 8px shrink floor - the 1280x720 run recorded "CRIME &amp; JUSTICE needs 67.5 wide in 57.6
        /// at 8px" - so a curated short form steps in below that threshold (DrawLawRowHeader's own
        /// "APPR." idiom: never clip, never shrink a token past legibility). Above it the full
        /// token stands and MeasuredLabel's shrink lands it near the 1i spec's own "dimmed 9.5px
        /// mono token" size.</summary>
        private static string LawCategoryCellLabel(LawCategory category, float cellWidth)
        {
            if (cellWidth >= 70f)
            {
                return LawCategoryLabel(category);
            }

            return category == LawCategory.LaborMarket ? "LABOR" : "C&J";
        }

        /// <summary>The one place a LawCategory maps to its area color (pass 3, 2026-08-26): with
        /// two categories sharing the browser, per-law surfaces (row accent, ENACTED status color,
        /// detail kicker context) carry the LAW'S OWN category area rather than a tab-wide
        /// CrimeJustice constant - the same truth each category's own bill card already declares
        /// (LABOR MARKET BILL draws SystemArea.Labor).</summary>
        private static UiPalette.SystemArea LawCategoryArea(LawCategory category)
        {
            return category == LawCategory.LaborMarket ? UiPalette.SystemArea.Labor : UiPalette.SystemArea.CrimeJustice;
        }

        /// <summary>The magnitude taxonomy's own four tiers (LawCatalog's class doc: MINOR +-3..6,
        /// MODERATE +-7..14, MAJOR +-15..22, SWEEPING +-23..30), read from the LARGEST
        /// SCALE-NORMALIZED absolute delta among a law's twelve dials - a law's "primary" effect,
        /// matching how every law's own code comment already names one tier for the whole law
        /// rather than one per dial. Pass 3 (per-dial scale ruling, 2026-08-26): real-unit dials
        /// (Kaitz points, weeks) normalize onto the shared grid via LawCatalog.DialMagnitudeScales
        /// before the comparison - index-locked to DialDeltas' documented order.
        ///
        /// Code-review pass (2026-08-25): reads law.DialDeltas (LawDefinition's own single
        /// enumeration) instead of hand-listing the fields here a second time, and the three
        /// tier boundaries are LawCatalog's own named constants instead of a second, silent copy of
        /// the numbers that class's doc comment already states.</summary>
        private static int LawMagnitudeTier(LawDefinition law)
        {
            float maxAbs = 0f;
            float[] deltas = law.DialDeltas;
            for (int i = 0; i < deltas.Length; i++)
            {
                maxAbs = Mathf.Max(maxAbs, Mathf.Abs(deltas[i]) * LawCatalog.DialMagnitudeScales[i]);
            }

            if (maxAbs <= LawCatalog.MinorMagnitudeMax) { return 1; }
            if (maxAbs <= LawCatalog.ModerateMagnitudeMax) { return 2; }
            if (maxAbs <= LawCatalog.MajorMagnitudeMax) { return 3; }
            return 4;
        }

        private static string LawMagnitudeLabel(int tier)
        {
            switch (tier)
            {
                case 1: return "MINOR";
                case 2: return "MODERATE";
                case 3: return "MAJOR";
                default: return "SWEEPING";
            }
        }

        /// <summary>Board 1i's stepped rule, exactly as spec'd: filled steps count from the left in
        /// TextPrimary/#2B2620 ink, empty steps in PoliSimTheme.MagnitudeStepEmpty/#CEC0A2, never
        /// recoloured per level since the scale is ordinal and length is meant to carry it. No new
        /// sprite - each step is Texture2D.whiteTexture tinted, the same "ui_pixel stretched and
        /// tinted" idiom the delivered spec itself names, this codebase's white texture being the
        /// same primitive under a different name.</summary>
        private static void DrawMagnitudeSteps(Rect rect, int tier, float stepWidth, float gap)
        {
            if (Event.current.type != EventType.Repaint)
            {
                return;
            }

            Color previous = GUI.color;
            float x = rect.x;
            for (int i = 0; i < 4; i++)
            {
                GUI.color = i < tier ? PoliSimTheme.TextPrimary : PoliSimTheme.MagnitudeStepEmpty;
                GUI.DrawTexture(new Rect(x, rect.y, stepWidth, rect.height), Texture2D.whiteTexture);
                x += stepWidth + gap;
            }
            GUI.color = previous;
        }

        /// <summary>
        /// The selected law's detail: description, every nonzero dial delta, the magnitude tier
        /// (the detail pane's own stepped rule, board 1i's larger variant), the real-world citation
        /// (LawDefinition.Citation, surfaced in the UI for the first time - see its own doc
        /// comment), a live PASS/FAIL estimate, and the enact/repeal action. The estimate uses the
        /// REAL pending bill's direction if one exists, else the hypothetical the action button
        /// below would submit right now - DrawTaxProgramBillEstimate's own established precedent
        /// (score the exact hypothetical the toggle would submit), applied to a law's action
        /// instead of an implement/remove toggle. The action button is ALWAYS drawn, GUI.enabled
        /// gated off while a bill is pending, rather than omitted (the MVP slice's original shape) -
        /// a pending bill resolves from the background day-tick loop, not from the player's own
        /// click, so omitting the button is exactly the "control count moves because of background
        /// state" hazard DrawTaxProgramBillEstimate's own row already avoids; fixed here to match.
        /// </summary>
        private void DrawLawDetailPane(LawRowEntry? row, float contentWidth)
        {
            if (row == null)
            {
                GUILayout.Label("Select a law from the list to see its detail.", _labelStyle, GUILayout.Width(contentWidth));
                return;
            }

            LawDefinition law = row.Value.Law;
            bool enacted = row.Value.Enacted;
            LawBill pendingBill = row.Value.PendingBill;

            // Code-review pass (2026-08-25): the name label is now given an EXPLICIT width - the
            // remainder after the status label's own measured width - rather than none at all.
            // GUIStyle.CalcSize (what a width-less GUILayout.Label uses to size itself) reports the
            // NATURAL, UNWRAPPED single-line size regardless of wordWrap; wordWrap only engages once
            // a width is actually given, via CalcHeight(content, width). Without it, a long law name
            // requested its full one-line width, invisibly extending past the now-correctly-clipping
            // ScrollView rather than wrapping inside it - clipped, not visible, which is exactly why
            // the FIRST width fix (constraining the ScrollView itself) changed nothing the capture
            // could see: the clip was already correct, the label's own layout input never was.
            // Reserved from the WIDEST possible status string ("ENACTMENT PENDING"), with a generous
            // buffer - CalcSize measured against the CURRENT (often much shorter) string was tried
            // twice and clipped its own last character both times, by a few px each time, a small
            // systematic underestimate rather than a one-off rounding error. Sizing from the worst
            // case instead is what actually stopped the clipping empirically. That alone previously
            // caused its OWN regression - starving the name column so badly that a short title
            // word-wrapped into an unreadable one-word-per-line tower - which the 50%-of-pane floor
            // below fixes: name never drops under half the content width, at the cost of the status
            // itself wrapping to a second line on the rare law where both a long name and "ENACTMENT
            // PENDING" are showing at once, which reads fine where a broken-up name does not.
            // Board 1j: the kicker line - category, weight class, and the law's position within
            // that class ("MAJOR - 3 OF 10 IN CLASS"), position by A-Z within the tier across the
            // whole catalog so it is stable under any list order or filter.
            int kickerTier = LawMagnitudeTier(law);
            int classCount = 0, classIndex = 0;
            foreach (LawDefinition other in LawCatalog.All)
            {
                if (LawMagnitudeTier(other) != kickerTier) { continue; }
                classCount++;
                if (string.CompareOrdinal(other.Name, law.Name) < 0) { classIndex++; }
            }
            DrawColoredLabel($"{LawCategoryLabel(law.Category)} - {LawMagnitudeLabel(kickerTier)} - {classIndex + 1} OF {classCount} IN CLASS",
                _labelStyle, PoliSimTheme.TextMuted, GUILayout.Width(contentWidth));

            GetLawStatusDisplay(row.Value, out string statusLabel, out Color statusColor);
            float wantedStatusWidth = _labelStyle.CalcSize(new GUIContent("ENACTMENT PENDING")).x + 24f;
            float nameWidth = Mathf.Max(contentWidth * 0.5f, contentWidth - wantedStatusWidth);
            float statusWidth = Mathf.Max(0f, contentWidth - nameWidth);
            GUILayout.BeginHorizontal();
            GUILayout.Label(law.Name, _headerStyle, GUILayout.Width(nameWidth));
            GUILayout.FlexibleSpace();
            DrawColoredLabel(statusLabel, _labelStyle, statusColor, GUILayout.Width(statusWidth));
            GUILayout.EndHorizontal();

            GUILayout.Label(law.Description, _labelStyle, GUILayout.Width(contentWidth));
            GUILayout.Space(4f);

            // Code-review pass (2026-08-25): the step run's size is now derived from the live font
            // size against the same reference (13px @ 1080p) LedgerRow itself scales from, instead of
            // the hardcoded 80f/15f/4f literals the rebuild first shipped with - a mockup-number this
            // codebase's own documented rule (CLAUDE.md) says never to leave as a bare constant.
            int tier = LawMagnitudeTier(law);
            float stepScale = Mathf.Max(1f, _labelStyle.fontSize) / 13f;
            float detailStepWidth = 11f * stepScale;
            float detailStepGap = 3f * stepScale;
            float detailStepsWidth = detailStepWidth * 4f + detailStepGap * 3f;
            GUILayout.BeginHorizontal();
            GUILayout.Label($"MAGNITUDE: {LawMagnitudeLabel(tier)}", _labelStyle, GUILayout.Width(_labelStyle.CalcSize(new GUIContent("MAGNITUDE: MODERATE")).x + 8f));
            Rect stepsRect = GUILayoutUtility.GetRect(detailStepsWidth, LedgerRow.Height(_labelStyle), GUILayout.Width(detailStepsWidth));
            DrawMagnitudeSteps(new Rect(stepsRect.x, stepsRect.y + stepsRect.height * 0.25f, detailStepsWidth, stepsRect.height * 0.5f), tier, detailStepWidth, detailStepGap);
            // Board 1j: the band's range, "restated where it is read."
            GUILayout.Space(8f);
            DrawColoredLabel(LawMagnitudeRangeLabel(tier), _labelStyle, PoliSimTheme.TextMuted);
            GUILayout.EndHorizontal();
            GUILayout.Space(4f);

            // Board 1j: IF ENACTED - DIAL MOVEMENT, the six dial deltas as a two-column grid with
            // direction arrows. The arrows carry DIRECTION ONLY, in neutral ink - the board draws
            // them green/red, which collides with two standing records (the DrawLawDeltaRow rule
            // this grid replaces: a dial's sign has no inherent value judgment the model makes;
            // and item 6's ruled neutral-derived honesty). Elias's own ruling outranks the drawn
            // valence where they collide - the layout is the board's, the ink discipline stays.
            GUILayout.Label("IF ENACTED - DIAL MOVEMENT", _labelStyle, GUILayout.Width(contentWidth));
            DrawLawDialMovementGrid(law, contentWidth);
            GUILayout.Space(4f);

            // Item 6 (ruled 2026-08-25): the NEUTRAL DERIVED effects list - per downstream stat,
            // the long-run target shift computed from this law's dial deltas and the declared
            // coupling table the simulation itself reads (CrimeJusticeCouplings), so this text
            // cannot drift from what enacting the law actually does. No authored valence - pro/con
            // is politics, direction and size are the model's. The model's coupling gaps are
            // deliberately visible here (a law moving only SentencingSeverity shows no prison
            // line, because the model has no such edge) - ruled acceptable, logged as the
            // couplings-pass input.
            // Pass 3 dispatch: each category reads ITS OWN declared table - CrimeJusticeCouplings'
            // aggregate is C&J-typed and returns an EMPTY list for a labor law (the generality
            // finding LaborCouplings' class doc records), so the labor branch quotes the labor
            // table instead, its minimum-wage edges gated on the player country's statutory-wage
            // fact exactly as the simulation gates them.
            if (law.Category == LawCategory.LaborMarket)
            {
                var laborEffects = LaborCouplings.AggregateLawEffects(law, _playerCountry.MinimumWageImplemented);
                if (laborEffects.Count > 0)
                {
                    GUILayout.Label("EXPECTED EFFECTS", _labelStyle, GUILayout.Width(contentWidth));
                    foreach (LaborCouplings.LawEffectLine effect in laborEffects)
                    {
                        GUILayout.Label(
                            $"{LaborCouplings.DisplayName(effect.Stat)}: {effect.Amount:+0.00;-0.00} {LaborCouplings.Unit(effect.Stat)}{(effect.Contested ? " (contested)" : "")}",
                            _labelStyle, GUILayout.Width(contentWidth));
                    }
                    GUILayout.Label("Long-run target shifts, from this law's dial deltas and the model's own couplings - as the dials settle, before dial clamps and the LFPR combined ceiling.",
                        _labelStyle, GUILayout.Width(contentWidth));
                    GUILayout.Space(4f);
                }
            }
            else
            {
                var expectedEffects = CrimeJusticeCouplings.AggregateLawEffects(law);
                if (expectedEffects.Count > 0)
                {
                    GUILayout.Label("EXPECTED EFFECTS", _labelStyle, GUILayout.Width(contentWidth));
                    foreach (CrimeJusticeCouplings.LawEffectLine effect in expectedEffects)
                    {
                        GUILayout.Label(
                            $"{CrimeJusticeCouplings.DisplayName(effect.Stat)}: {effect.Amount:+0.00;-0.00} {CrimeJusticeCouplings.Unit(effect.Stat)}{(effect.Contested ? " (contested)" : "")}",
                            _labelStyle, GUILayout.Width(contentWidth));
                    }
                    GUILayout.Label("Long-run target shifts, from this law's dial deltas and the model's own couplings - as the dials settle, before dial clamps.",
                        _labelStyle, GUILayout.Width(contentWidth));
                    GUILayout.Space(4f);
                }
            }

            GUILayout.Label(law.Citation, _labelStyle, GUILayout.Width(contentWidth));
            GUILayout.Label($"Enactment cost: {law.EnactmentApprovalCost.ToString("F1", CultureInfo.InvariantCulture)} approval (paid once, on passage)", _labelStyle, GUILayout.Width(contentWidth));
            GUILayout.Space(6f);

            // Board 1j: IF PUT TO THE HOUSE TODAY - the live estimate under its own title, with
            // the per-party stance rows beneath it.
            GUILayout.Label("IF PUT TO THE HOUSE TODAY", _labelStyle, GUILayout.Width(contentWidth));
            if (pendingBill != null)
            {
                GUILayout.Label($"{(pendingBill.IsRepeal ? "Repeal" : "Enactment")} before Parliament - resolves in {pendingBill.DaysRemaining} day(s).", _labelStyle, GUILayout.Width(contentWidth));
                float pendingDirection = ParliamentSystem.GetLawBillDirection(_playerCountry, pendingBill);
                DrawBillLiveEstimate(pendingDirection, contentWidth);
                DrawLawPartyStances(pendingDirection, contentWidth);
            }
            else
            {
                float direction = ParliamentSystem.GetLawBillDirection(_playerCountry, new LawBill { LawId = law.Id, IsRepeal = enacted });
                DrawBillLiveEstimate(direction, contentWidth);
                DrawLawPartyStances(direction, contentWidth);
            }

            // Code-review pass (2026-08-25): `&& !_isGameOver` moved HERE from the tab-wide
            // GUI.enabled wrapper at the DrawLawsTab call site, which used to disable the row-select
            // button too and permanently lock whichever law was selected at end-of-game. Only the
            // actual state-changing action needs gating on game-over; browsing a law's detail is
            // informational and has no gameplay effect either way.
            bool ambientEnabled = GUI.enabled;
            GUI.enabled = ambientEnabled && pendingBill == null && !_isGameOver;
            string actionLabel = enacted ? $"Repeal {law.Name}" : $"Enact {law.Name}";
            if (GUILayout.Button(actionLabel, _neutralActionButtonStyle))
            {
                _simulationManager.IntroduceLawBill(PlayerCountryId, new LawBill { LawId = law.Id, IsRepeal = enacted });
            }
            GUI.enabled = ambientEnabled;
        }

        /// <summary>Code-review pass (2026-08-25): the status label and its color, computed together
        /// in one set of branches instead of two independent ternary chains over the same three
        /// states - the previous shape required a future edit (e.g. splitting repeal-pending from
        /// enactment-pending more granularly) to update both chains in lockstep, with no compiler
        /// check tying them together.</summary>
        private static void GetLawStatusDisplay(LawRowEntry row, out string label, out Color color)
        {
            if (row.PendingBill != null)
            {
                label = row.PendingBill.IsRepeal ? "REPEAL PENDING" : "ENACTMENT PENDING";
                color = PoliSimTheme.TextSecondary;
            }
            else if (row.Enacted)
            {
                label = "ENACTED";
                color = UiPalette.GetAreaColor(LawCategoryArea(row.Law.Category));
            }
            else
            {
                label = "not enacted";
                color = PoliSimTheme.TextMuted;
            }
        }

        /// <summary>
        /// Board 1i's bottom bar, on 1c's own convention (a flex spacer, then the bar) - approval on
        /// hand (the currency every EnactmentApprovalCost spends) and an affordability line scoped
        /// to the CURRENTLY VISIBLE (filtered) set, so the line answers "of what I'm looking at,"
        /// not the whole catalog regardless of filter. "Next sitting date" from the delivered board
        /// is deliberately NOT built: Parliament here has no shared sitting calendar at all - every
        /// bill (law, tax, budget, standalone) resolves on its OWN independent day-countdown
        /// (LawBill.DaysRemaining and its seven siblings), never a common date every bill waits for
        /// - so there is no real concept to surface, and inventing one would be exactly the "no
        /// invented numbers/concepts dressed as researched" rule 5 exists to forbid.
        /// </summary>
        private void DrawLawBottomBar(List<LawRowEntry> visibleLaws, float innerWidth)
        {
            GUILayout.Space(6f);
            GUILayout.BeginHorizontal();
            float approval = _playerCountry.State.ApprovalRating;
            string approvalText = $"Approval on hand: {approval.ToString("F1", CultureInfo.InvariantCulture)}";
            float approvalWidth = _labelStyle.CalcSize(new GUIContent(approvalText)).x + 4f;
            DrawColoredLabel(approvalText, _labelStyle, PoliSimTheme.TextPrimary, GUILayout.Width(approvalWidth));
            GUILayout.FlexibleSpace();

            int affordable = 0;
            foreach (LawRowEntry row in visibleLaws)
            {
                if (row.Law.EnactmentApprovalCost <= approval) { affordable++; }
            }

            // Free-aspect pass (2026-08-26): the trailing label takes the row's measured remainder
            // and WRAPS there rather than requesting natural width - at the 1280x720 floor the two
            // labels' natural widths summed past the row and widened the whole box (the intro
            // label's class, one row down).
            float affordableWidth = Mathf.Max(_labelStyle.fontSize * 6f, innerWidth - approvalWidth - 16f);
            GUILayout.Label($"Affordable now: {affordable} of {visibleLaws.Count} shown (cost <= approval on hand)", _labelStyle, GUILayout.Width(affordableWidth));
            GUILayout.EndHorizontal();
        }

        /// <summary>Board 1j's two-column dial grid, replacing the single-column DrawLawDeltaRow
        /// list (whose no-good/bad-ink rule this grid keeps: a dial's sign has no inherent value
        /// judgment the model makes - the same reasoning C4's rating tile leaves "Stable"
        /// uncoloured). Zero dials are omitted (never a zero row); nonzero dials pair up left/right
        /// in the board's grid. Arrows carry DIRECTION ONLY in neutral ink - see the call site's
        /// comment for why the board's green/red valence is deliberately not taken.</summary>
        private void DrawLawDialMovementGrid(LawDefinition law, float contentWidth)
        {
            // Pass 3: the grid hand-lists the SELECTED LAW'S OWN CATEGORY's dials - the nonzero
            // filter below would hide the foreign six anyway, but dispatching keeps the
            // real-unit labels honest (Kaitz points and weeks are not 0-100 dial points and must
            // not read as such).
            (string Name, float Delta)[] dials = law.Category == LawCategory.LaborMarket
                ? new (string, float)[]
                {
                    ("Minimum Wage (Kaitz pts)", law.MinimumWageDelta),
                    ("Paid Family Leave (weeks)", law.PaidFamilyLeaveWeeksDelta),
                    ("Overtime Regulation", law.OvertimeRegulationDelta),
                    ("Retraining Programs", law.RetrainingProgramDelta),
                    ("Family Policy", law.FamilyPolicyDelta),
                    ("Immigration Policy", law.ImmigrationPolicyDelta),
                }
                : new (string, float)[]
                {
                    ("Police Funding", law.PoliceFundingDelta),
                    ("Sentencing Severity", law.SentencingSeverityDelta),
                    ("Bail Reform", law.BailReformDelta),
                    ("Drug Policy", law.DrugPolicyDelta),
                    ("Judicial Funding", law.JudicialFundingDelta),
                    ("Border Enforcement", law.BorderEnforcementDelta),
                };

            var nonzero = new List<(string Name, float Delta)>(6);
            foreach ((string name, float delta) in dials)
            {
                if (!Mathf.Approximately(delta, 0f)) { nonzero.Add((name, delta)); }
            }

            // Explicit width, never ExpandWidth: inside the detail scroll view an expanding rect
            // follows the CONTENT width (set by the widest natural-size label anywhere in the
            // pane), not the viewport - the first capture showed these rows running past the
            // visible pane and clipping at its edge. contentWidth is the pane's real measured
            // budget, the same one every wrapping label here already takes.
            float cellWidth = Mathf.Max(0f, (contentWidth - 16f) * 0.5f);
            for (int i = 0; i < nonzero.Count; i += 2)
            {
                Rect rowRect = GUILayoutUtility.GetRect(contentWidth, LedgerRow.Height(_labelStyle), GUILayout.Width(contentWidth));
                DrawLawDialCell(new Rect(rowRect.x, rowRect.y, cellWidth, rowRect.height), nonzero[i]);
                if (i + 1 < nonzero.Count)
                {
                    DrawLawDialCell(new Rect(rowRect.x + cellWidth + 16f, rowRect.y, cellWidth, rowRect.height), nonzero[i + 1]);
                }
            }
        }

        private void DrawLawDialCell(Rect rect, (string Name, float Delta) dial)
        {
            float valueWidth = Mathf.Max(0f, rect.width * 0.34f);
            LedgerRow.Cell(new Rect(rect.x, rect.y, rect.width - valueWidth, rect.height), dial.Name, _labelStyle, PoliSimTheme.TextPrimary, TextAnchor.MiddleLeft);
            string arrow = dial.Delta > 0f ? "▲ " : "▼ ";
            LedgerRow.Cell(new Rect(rect.x + rect.width - valueWidth, rect.y, valueWidth, rect.height),
                arrow + dial.Delta.ToString("+0.0;-0.0;0", CultureInfo.InvariantCulture), _labelStyle, PoliSimTheme.TextSecondary, TextAnchor.MiddleRight);
        }

        /// <summary>
        /// Board 1j's per-party stance rows for the house estimate - REAL data only: each
        /// archetype's actual seats and which side of this bill's direction its fiscal stance
        /// puts it on. ⚠ The board also draws per-party FOR headcounts and an "N of 435, needs
        /// 218" majority line - both are deliberately NOT built: GetSeatWeightedAlignment's own
        /// doc states "this is NOT a headcount, and there is no seats-based majority threshold
        /// anywhere in this model," and Design's own Pass-3 D2 ruling struck headcounts from every
        /// estimate ("no per-instrument support exists"). Stance sign and seat counts are the two
        /// facts the model does keep; the lean bar above remains the quantity the vote is really
        /// decided on.</summary>
        private void DrawLawPartyStances(float direction, float contentWidth)
        {
            if (Mathf.Approximately(direction, 0f))
            {
                return;
            }

            float billSign = Mathf.Sign(direction);
            foreach (PartyArchetype archetype in PartyArchetypeData.AllArchetypes)
            {
                int seats = _playerCountry.ParliamentSeats.TryGetValue(archetype, out int s) ? s : 0;
                if (seats <= 0)
                {
                    continue;
                }

                float stance = PartyArchetypeData.GetFiscalStance(archetype) * billSign;
                string side = stance > 0f ? "FOR" : stance < 0f ? "AGAINST" : "UNALIGNED";
                // Explicit width - see DrawLawDialMovementGrid's comment: an expanding rect inside
                // the pane's scroll view tracks content width, not the viewport, and the first
                // capture clipped these rows' right cells at the pane edge.
                Rect rowRect = GUILayoutUtility.GetRect(contentWidth, LedgerRow.Height(_labelStyle), GUILayout.Width(contentWidth));
                float sideWidth = Mathf.Max(0f, rowRect.width * 0.28f);
                LedgerRow.Cell(new Rect(rowRect.x, rowRect.y, rowRect.width - sideWidth, rowRect.height),
                    $"{DisplayName.Spaced(archetype.ToString())} - {seats} seats", _labelStyle, PoliSimTheme.TextPrimary, TextAnchor.MiddleLeft);
                LedgerRow.Cell(new Rect(rowRect.x + rowRect.width - sideWidth, rowRect.y, sideWidth, rowRect.height),
                    side, _labelStyle, PoliSimTheme.TextSecondary, TextAnchor.MiddleRight);
            }
        }

        /// <summary>The statute-book search slot (board 1j): the label plus the paper-idiom field
        /// (the saves menu's BuildTextFieldStyle precedent - the first capture rendered Unity's
        /// grey default here). One method, two call sites, because the slot reflows between the
        /// ORDER row and the summary row by measured fit - the free-aspect pass's floor case.</summary>
        private void DrawLawSearchSlot(float labelWidth, float fieldWidth)
        {
            GUILayout.Label("SEARCH", _labelStyle, GUILayout.Width(labelWidth));
            _lawSearchText = GUILayout.TextField(_lawSearchText ?? string.Empty, 48,
                UiPalette.BuildTextFieldStyle(_labelStyle.fontSize),
                GUILayout.Width(fieldWidth), GUILayout.ExpandWidth(false));
        }

        /// <summary>Board 1j's summary line needs the pending count before the partition loop runs -
        /// one pass over the catalog against the pending-bill store, nothing cached.</summary>
        private int CountPendingLawBills()
        {
            int count = 0;
            foreach (LawDefinition law in LawCatalog.All)
            {
                if (_simulationManager.GetPendingLawBill(PlayerCountryId, law.Id) != null) { count++; }
            }
            return count;
        }

        /// <summary>
        /// Master Sequence step 5e, Phase A: Politics tab - Parliament, the Political Compass half of
        /// the old "Compass & Demographics" tab, Cabinet's management half, and Federal Reserve (Elias's
        /// own confirmed placement - a real political institution with its own lever, even though the
        /// Fed/Eurozone exemption means it's never Parliament-gated). Per-category gating matches the
        /// old dispatch exactly - Parliament/Compass were never gated, Cabinet/FederalReserve were.
        /// </summary>
        private void DrawPoliticsTab(float availableHeight, float availableWidth)
        {
            GUILayout.BeginVertical(_boxStyle);
            DrawColoredLabel("Politics", _headerStyle, UiPalette.GetAreaColor(UiPalette.SystemArea.Political));
            GUILayout.BeginHorizontal();
            float subTabShare = SubTabShare(availableWidth, 4);
            // Instance #13's OWN row: GetCentralBankName is the label that varies by country, and
            // "European Central Bank (ECB)" is the one that garbled. Measured, imposed, and shared
            // with the reserve below.
            string centralBankName = GetCentralBankName(PlayerCountryId);
            float subTabRowHeight = SubTabRowHeight(subTabShare, "Parliament", "Compass", "Cabinet", centralBankName);
            DrawSubCategoryButton("Parliament", PoliticsCategory.Parliament, ref _politicsCategory, subTabShare, subTabRowHeight);
            DrawSubCategoryButton("Compass", PoliticsCategory.Compass, ref _politicsCategory, subTabShare, subTabRowHeight);
            DrawSubCategoryButton("Cabinet", PoliticsCategory.Cabinet, ref _politicsCategory, subTabShare, subTabRowHeight);
            DrawSubCategoryButton(centralBankName, PoliticsCategory.FederalReserve, ref _politicsCategory, subTabShare, subTabRowHeight);
            GUILayout.EndHorizontal();
            GUILayout.Space(6f);

            float contentHeight = availableHeight - _headerStyle.fontSize - subTabRowHeight - 14f;
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
            GUILayout.Label("Hover a marker for a quick readout, click to pin it below. Colored dots are recent events - green helped, red hurt; size reflects how big a shock it was, and dots fade out over a few years.", _labelStyle);
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

            // ⚠ v2.0 CONVERSION, 2026-08-11 — five concatenated label lines become read-only ledger rows.
            // Every figure here was previously built INTO its own label string ("Tax burden: 38.2% of
            // GDP"), which is the pre-v2.0 pattern and the one §A.9 exists to replace: name, gauge,
            // figure, unit, each in its own column, so a column can be read down instead of a sentence
            // read across.
            //
            // These are `DrawReadOnly` rather than disabled sliders for the same reason Infrastructure is
            // - a derived statistic is an OUTPUT, there is nothing to drag under any circumstances, and a
            // disabled slider would assert a lever this screen has never had (behaviour 5).
            Color fiscalInk = UiPalette.GetAreaColor(UiPalette.SystemArea.Fiscal);

            // ⚠ GDP PER CAPITA HAS NO DENOMINATOR, so it passes a negative fill and draws no gauge -
            // see LedgerRow.DrawReadOnly. It is currency per person, not a share of anything.
            float? perCapita = DerivedStats.GdpPerCapita(_playerCountry);
            DrawDerivedStatRow("GDP per capita", -1f,
                perCapita.HasValue ? UiFormat.Money(perCapita.Value, MoneyUnit.Thousands) : "n/a",
                perCapita.HasValue ? null : "no population",
                UiPalette.GetAreaColor(UiPalette.SystemArea.Global));

            // "advance a year" rather than a zero: no turn has produced a FiscalTurnReport yet, and a
            // 0.0% tax burden is a confident wrong number of exactly the kind this project keeps finding.
            // The gauge is suppressed in that state too - an empty track would BE that wrong number.
            float? taxBurden = DerivedStats.TaxBurdenPercentOfGdp(_playerCountry, report);
            DrawDerivedStatRow("Tax burden", taxBurden.HasValue ? taxBurden.Value / 100f : -1f,
                taxBurden.HasValue ? UiFormat.Number(taxBurden.Value, 1) + "%" : "not yet computed",
                taxBurden.HasValue ? "of GDP" : "advance a year", fiscalInk);

            float? spending = DerivedStats.SpendingPercentOfGdp(_playerCountry, report);
            DrawDerivedStatRow("Government spending", spending.HasValue ? spending.Value / 100f : -1f,
                spending.HasValue ? UiFormat.Number(spending.Value, 1) + "%" : "not yet computed",
                spending.HasValue ? "of GDP" : "advance a year", fiscalInk);

            // Positive is a deficit, so "higher is better" is FALSE here - the opposite of the
            // BudgetBalance colouring elsewhere, because the sign convention is the opposite too. The
            // NAME changes with the sign rather than the number carrying a minus: a row headed "Surplus"
            // showing 4.8% is unambiguous where "Deficit: -4.8%" needs a second reading.
            float? deficit = DerivedStats.DeficitPercentOfGdp(_playerCountry, report);
            DrawDerivedStatRow(
                deficit.HasValue && deficit.Value < 0f ? "Surplus" : "Deficit",
                deficit.HasValue ? Mathf.Abs(deficit.Value) / 100f : -1f,
                deficit.HasValue ? UiFormat.Number(Mathf.Abs(deficit.Value), 1) + "%" : "not yet computed",
                deficit.HasValue ? "of GDP" : "advance a year",
                deficit.HasValue ? UiPalette.GetDeltaColor(deficit.Value, higherIsBetter: false) : fiscalInk);

            // Playtest finding 2 (2026-08-25), per the single-book rider: the row above IS the real
            // balance (BudgetBalance is net of interest - verified at ApplyRevenueAndSpending, where
            // TotalSpending includes InterestOnDebt). The primary balance is worth showing - the
            // fiscal trace panel already decomposes it - so it appears AS a labeled second line from
            // the SAME report, never as an unlabeled "Surplus" that could be mistaken for the book.
            float? primaryDeficit = DerivedStats.PrimaryDeficitPercentOfGdp(_playerCountry, report);
            DrawDerivedStatRow(
                primaryDeficit.HasValue && primaryDeficit.Value < 0f ? "Primary surplus" : "Primary deficit",
                primaryDeficit.HasValue ? Mathf.Abs(primaryDeficit.Value) / 100f : -1f,
                primaryDeficit.HasValue ? UiFormat.Number(Mathf.Abs(primaryDeficit.Value), 1) + "%" : "not yet computed",
                primaryDeficit.HasValue ? "of GDP, excl. interest" : "advance a year",
                primaryDeficit.HasValue ? UiPalette.GetDeltaColor(primaryDeficit.Value, higherIsBetter: false) : fiscalInk);

            // ⚠ EIGHT SECTORS WERE ONE CONCATENATED STRING - "Agriculture 2.1% | Commerce 18.3% | ..."
            // joined with pipes into a single label. That is the densest thing on this screen and the
            // least readable: no two shares can be compared without counting characters, and the line
            // wrapped differently at every window width. One row each, each with its own gauge, is the
            // whole argument for the ledger form.
            List<(SectorType Type, float SharePercent)> shares = DerivedStats.SectorSharesOfGdp(_playerCountry);
            if (shares.Count > 0)
            {
                GUILayout.Space(6f);
                GUILayout.Label("Sector shares of GDP", _labelStyle);
                for (int i = 0; i < shares.Count; i++)
                {
                    // ⚠ Spaced, NOT Of — `SectorType.Energy` resolves through the curated policy table to
                    // "Energy (Spending)", a discretionary spending line rather than an economic sector.
                    // See DisplayName.Spaced.
                    DrawDerivedStatRow(DisplayName.Spaced(shares[i].Type.ToString()), shares[i].SharePercent / 100f,
                        UiFormat.Number(shares[i].SharePercent, 1) + "%", "of GDP",
                        UiPalette.GetCategoricalColor(i));
                }
            }
            else
            {
                GUILayout.Label("Sector shares of GDP: not tracked for this country.", _labelStyle);
            }

            GUILayout.EndVertical();
        }

        /// <summary>
        /// One derived statistic as a read-only ledger row, so the five sites above read as a table
        /// rather than five bespoke label calls. <paramref name="fill"/> below zero draws no gauge — for
        /// a figure with no denominator, or one not yet computed.
        /// </summary>
        private void DrawDerivedStatRow(string name, float fill, string figureText, string trailingText, Color barInk)
        {
            Rect rowRect = GUILayoutUtility.GetRect(10f, LedgerRow.Height(_labelStyle), GUILayout.ExpandWidth(true));
            LedgerRow.DrawReadOnly(rowRect, name, fill, figureText, trailingText, barInk, _labelStyle, _labelStyle);
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
            GUILayout.Label($"Year {marker.TurnFired} (this year: {_simulationManager.CurrentTurn})", _labelStyle);
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
                graph.DrawNeutral($"{PolicyWebRenderer.GetStatName(node)} (last 50 years)", history, null, _labelStyle,
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
                // R4-4. This switch IS the scoped name lookup the pre-report's hazard note requires:
                // portfolio display names come from here and only here - never DisplayName.Of/Spaced,
                // which is how "Education" avoids becoming reference-class-trap instance #4 against
                // SpendingCategory.Education/PolicyNodeId.Education.
                case CabinetPortfolio.Defense: return "Defense";
                case CabinetPortfolio.ForeignAffairs: return "Foreign Affairs";
                case CabinetPortfolio.Education: return "Education";
                default: return portfolio.ToString();
            }
        }

        /// <summary>
        /// Cabinet tab (Political Systems Overhaul Part A, Master Sequence step 1; all six confirmed
        /// portfolios since R4-4): one panel per
        /// implemented portfolio showing the appointed minister (or a candidate picker if vacant),
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
            // R4-4: "most" is deliberate - Defense and Foreign Affairs ministers are decisions-only
            // this pass (ruling R3), so the old "each appointed minister quietly nudges" wording
            // would claim a passive effect four of six portfolios have and two do not.
            // TURN->YEAR: "every turn" -> "every year", same sweep as everywhere else in this file.
            GUILayout.Label("Most appointed ministers quietly nudge their own portfolio's existing channels every year just by serving, and any minister occasionally brings you a real decision with a few response options. Philosophy determines what KIND of decisions a minister brings, not how skilled they are - that's CompetenceBias, a separate trait. Reshuffling a minister costs a modest approval hit but can happen anytime. Pending decisions themselves now show under the Decisions tab.", _labelStyle);
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

            GUILayout.Space(10f);
            DrawRecentDivisions();

            GUILayout.EndScrollView();
            GUILayout.EndVertical();
        }

        /// <summary>
        /// ⚠ ITEM 1a, IN ITS CORRECTED SHAPE (Elias, 2026-08-12) — the FIRST reader of
        /// <c>Country.Divisions</c>. Eight resolution sites had written this log since `a7bd40d` with
        /// nothing reading it back; this block is what closes the stamps ruling, whose "nothing to
        /// stamp" premise the same correction retired.
        ///
        /// NOT LedgerRow, stated per the ruling's own "say why rather than forcing it": LedgerRow's
        /// columns are policy-control furniture — name | tick track | STANDING ✎ DRAFT | SHARE — and
        /// its read-only form is a condition GAUGE. A division has no dial, no draft, no share, and a
        /// verdict is not a meter; what it shares with the ledger is only "rows on paper". The row
        /// here is the record's own five fields: number + title, date (mono — a document artifact, per
        /// §A.4's Courier register), the same diverging lean bar the live estimate draws (the record's
        /// Alignment is captured from the same GetSeatWeightedAlignment precisely so the two can never
        /// disagree — and per DivisionRecord's own doc, never a "186–164" headcount, which is a
        /// quantity this model does not compute), and the verdict stamp.
        ///
        /// ⚠ STAMP TINT FAMILY, checked through the accessors BEFORE drawing — the question four
        /// instances got wrong: `ui_stamp_carried`/`ui_stamp_rejected` are WoA and land on the
        /// parliament PAPER panel, so they take the INK weights — <see cref="PoliSimTheme.Good"/> /
        /// <see cref="PoliSimTheme.Bad"/> — never the lifted on-desk set, which exists solely for the
        /// dark desk ground (the hold banner's lamp is its one current user). The manifest's own note:
        /// "−2° baked; tint good / tint bad" — the rotation ships in the pixels, no runtime transform.
        ///
        /// Newest first — "recent" is the panel's claim. The empty state follows THIS SCREEN's own
        /// precedent (Pending Legislation prints its no-bill line), which is a different case from the
        /// suppressed empty spending GROUP: that was a sub-group with a degenerate denominator inside
        /// a populated screen; this is a top-level section whose emptiness is itself the honest
        /// reading. The stamp fallback draws into the SAME reserved rect (a rect draw, not a control),
        /// so sprite availability never changes the control count.
        /// </summary>
        private void DrawRecentDivisions()
        {
            GUILayout.Label("Division Records", _headerStyle);

            List<DivisionRecord> entries = _playerCountry.Divisions.Entries;
            if (entries.Count == 0)
            {
                GUILayout.Label("No divisions recorded yet - bills that resolve appear here.", _labelStyle);
                return;
            }

            Texture2D carried = IconLibrary.GetChrome("ui_stamp_carried");
            Texture2D rejected = IconLibrary.GetChrome("ui_stamp_rejected");

            for (int i = entries.Count - 1; i >= 0; i--)
            {
                DivisionRecord record = entries[i];
                Color verdictInk = record.Passed ? PoliSimTheme.Good : PoliSimTheme.Bad;
                float rowHeight = Mathf.Max(20f, _labelStyle.fontSize * 1.2f);

                GUILayout.BeginHorizontal();
                GUILayout.Label($"No. {record.Number} · {record.Title}", _labelStyle, GUILayout.ExpandWidth(true));
                GUILayout.Label(record.Date.ToString("yyyy-MM-dd"), _divisionMetaStyle, GUILayout.Width(_divisionMetaStyle.fontSize * 6.5f));

                // The lean bar sits in a slot as tall as the stamp beside it and is drawn centred in
                // it — a GetRect slot is layout, not a control, so the row's control set stays fixed.
                float barWidth = _labelStyle.fontSize * 5f;
                Rect barSlot = GUILayoutUtility.GetRect(barWidth, rowHeight, GUILayout.Width(barWidth), GUILayout.Height(rowHeight));
                if (Event.current.type == EventType.Repaint)
                {
                    float barHeight = _labelStyle.fontSize * 0.55f;
                    var barRect = new Rect(barSlot.x, barSlot.y + (barSlot.height - barHeight) * 0.5f, barSlot.width, barHeight);
                    UiPalette.DrawDivergingBar(barRect, record.Alignment, PendingBillLeanDisplayRange);
                }

                // Stamp geometry from the sprite's own 170x50 @1x proportion.
                float stampWidth = Mathf.Round(rowHeight * (170f / 50f));
                Rect stampRect = GUILayoutUtility.GetRect(stampWidth, rowHeight, GUILayout.Width(stampWidth), GUILayout.Height(rowHeight));
                Texture2D stamp = record.Passed ? carried : rejected;
                if (stamp != null)
                {
                    UiPalette.DrawTintedIcon(stampRect, stamp, verdictInk);
                }
                else if (Event.current.type == EventType.Repaint)
                {
                    Color saved = GUI.color;
                    GUI.color = verdictInk;
                    GUI.Label(stampRect, record.Passed ? "CARRIED" : "REJECTED", _cardKindStyle);
                    GUI.color = saved;
                }
                GUILayout.EndHorizontal();
            }
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
                    ParliamentSystem.GetTradeBillDirection(_playerCountry, tradeBill, _world), UiPalette.SystemArea.Trade));
            }

            SwfDrawdownBill drawdownBill = _simulationManager.GetPendingSwfDrawdownBill(PlayerCountryId);
            if (drawdownBill != null)
            {
                // Names its amount, unlike the other four. A drawdown IS its number - "an emergency
                // drawdown bill" tells a player nothing about what they are about to be committed to.
                pending.Add(($"SWF emergency drawdown - {drawdownBill.WithdrawalPercentOfGdp:F1}% of GDP, resolves in {drawdownBill.DaysRemaining} day(s).",
                    ParliamentSystem.GetSwfDrawdownBillDirection(_playerCountry, drawdownBill), UiPalette.SystemArea.SovereignWealth));
            }

            // Law system MVP slice: every pending law bill, named by its LawDefinition (falling back
            // to the raw LawId if the catalog entry is somehow gone, the same "missing entry, not a
            // crash" idiom LawCatalog.GetById's own doc comment establishes) - multiple can be
            // pending at once, unlike the single-slot tier-3 bills above.
            foreach (KeyValuePair<string, LawBill> lawBillPair in _simulationManager.GetPendingLawBills(PlayerCountryId))
            {
                LawBill lawBill = lawBillPair.Value;
                LawDefinition law = LawCatalog.GetById(lawBill.LawId);
                string lawName = law != null ? law.Name : lawBill.LawId;
                pending.Add(($"{(lawBill.IsRepeal ? "Repeal" : "Enact")} \"{lawName}\" - resolves in {lawBill.DaysRemaining} day(s).",
                    ParliamentSystem.GetLawBillDirection(_playerCountry, lawBill), UiPalette.SystemArea.CrimeJustice));
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

                // ⚠ A DELIBERATE EXCEPTION TO BEHAVIOUR 5's WORDING, and NOT a precedent. Recorded here
                // in 2026-08-10's sweep so a future reader neither "fixes" it nor cites it.
                //
                // The branches below emit different control COUNTS - one Reshuffle button when a
                // minister holds the portfolio, N candidate buttons or one Search button when it is
                // vacant - which is the shape that produced two real defects elsewhere (the minimum-wage
                // slider and the partner tariff override, both fixed). Two specific facts make it safe
                // HERE, and both were verified rather than assumed:
                //
                //   1. `Country.CabinetMinisters` is written ONLY by this class. Every writer in the
                //      repo is in GameController or SimulationTestRunner; no simulation system mutates
                //      it. So the count can only change as the direct result of a click in this method,
                //      never underneath the player.
                //   2. This screen emits NO sliders. Nothing here can be mid-drag, and a button click
                //      resolves within the frame that raised it.
                //
                // The hazard behaviour 5 exists to prevent is a HOT control's ID shifting because
                // background state changed the count. Neither half of that is reachable here. The rule's
                // wording is broader than its hazard, and this is the one place in the codebase where
                // the two come apart - see the sweep note in CLAUDE.md.
                //
                // Contorting this into a fixed control set would also mean rendering N candidate buttons
                // when there are no candidates, which is not a thing.
                if (GUILayout.Button("Reshuffle", _neutralActionButtonStyle))
                {
                    _playerCountry.CabinetMinisters.Remove(portfolio);
                    float approvalBeforeReshuffle = _playerCountry.State.ApprovalRating;
                    _playerCountry.State.ApprovalRating = Mathf.Clamp(_playerCountry.State.ApprovalRating - CabinetSystem.ReshuffleApprovalCost, 0f, 100f);
                    ApprovalLedgerRecorder.RecordEvent(_playerCountry, _simulationManager.CurrentDate, $"Cabinet reshuffle ({DisplayName.Of(portfolio.ToString())})", _playerCountry.State.ApprovalRating - approvalBeforeReshuffle);
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
            GUILayout.Label("Grounded entirely in this game's own tracked policy data - no invented ideology labels. X: average implemented tax rate blended with total government spending (% of GDP) - further right means a bigger fiscal footprint. Y: average sector regulation blended with average implemented welfare generosity - higher means more market regulation and a more generous welfare state. Your own country is ringed in ink.", _labelStyle);
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
                sectorSlices.Add(new PieSlice(DisplayName.Spaced(sector.Type.ToString()), sector.EmploymentShare, UiPalette.GetCategoricalColor(sectorIndex)));
                sectorIndex++;
            }
            _sectorEmploymentPieChart.Draw($"{_playerCountry.Name}: Employment Share by Sector", sectorSlices, _labelStyle, "F1", moneyUnit: null);
            GUILayout.Space(10f);

            // 29 SpendingCategory members against an eight-ink cap, so this is a ranked ledger, not a
            // pie. No index into GetCategoricalColor at all - which is the point: the old code walked
            // to index 28 and the palette silently generated a hue for every one of them.
            if (_playerCountry.SpendingLines.Count > 0)
            {
                var spendingRows = new List<(string Label, float Value)>();
                foreach (SpendingLine line in _playerCountry.SpendingLines)
                {
                    spendingRows.Add((DisplayName.Of(line.Category.ToString()), line.Amount));
                }
                _spendingAllocationLedger.Draw($"{_playerCountry.Name}: Spending Allocation", spendingRows, _labelStyle,
                    UiPalette.GetAreaColor(UiPalette.SystemArea.Fiscal), valueFormat: null, moneyUnit: MoneyUnit.Billions);
            }
            else
            {
                GUILayout.Label($"{_playerCountry.Name}: Spending Allocation", _labelStyle);
                GUILayout.Label("Detailed per-category spending breakdown not tracked for this country yet.", _labelStyle);
            }
            GUILayout.Space(10f);

            // 13 TaxType members - also over the cap, same treatment.
            var taxRows = new List<(string Label, float Value)>();
            foreach (TaxLine taxLine in _playerCountry.TaxLines)
            {
                if (!taxLine.IsImplemented) continue;
                float revenue = state.GDP * (taxLine.Rate / 100f) * taxLine.BaseShareOfGdp;
                taxRows.Add((DisplayName.Of(taxLine.Type.ToString()), revenue));
            }
            _taxRevenueLedger.Draw($"{_playerCountry.Name}: Theoretical Tax Revenue by Source", taxRows, _labelStyle,
                UiPalette.GetAreaColor(UiPalette.SystemArea.Fiscal), valueFormat: null, moneyUnit: MoneyUnit.Billions);
            GUILayout.Space(10f);

            // Annual cadence, so a bulletin rather than a chart - see PublishedFigure.
            PublishedFigure.Draw("Population as published",
                _playerCountry.Published.Series.TryGetValue(PublishedStat.Population, out PublishedSeries populationPublished) ? populationPublished : null,
                _labelStyle, moneyUnit: null);
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

            // The long qualifier - "applies to any partner with no override, and only where it isn't
            // superseded by trade-bloc membership" - is a property of the SCREEN, not of this row, and it
            // is already said by the paragraph below about overrides beating the usual resolution. The
            // row keeps the range, which is what the trailing column carries everywhere else.
            _tariffRateInput = DrawDialRow("General Base Tariff",
                _playerCountry.BaseTariffRate, GetTariffRateInput(_playerCountry.BaseTariffRate),
                MinBaseTariffRate, MaxBaseTariffRate, "F2", "%",
                $"{MinBaseTariffRate:F0}-{MaxBaseTariffRate:F0}% range");
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

            // ⚠ THE PARTNER IS A GROUP HEADER, exactly as a sector is on Economic Sectors. It was a plain
            // label, which was survivable while its override slider sat tight beneath its button - but
            // the behaviour-5 fix below adds a row, and the first capture after it made the boundaries
            // genuinely ambiguous: with no separation, one partner's override row reads as belonging to
            // the partner named beneath it. The name now carries the same weight Manufacturing/Retail do,
            // and the volumes and tariffs follow as its context line.
            GUILayout.BeginHorizontal();
            GUILayout.Label(partner.Name, _headerStyle, GUILayout.Width(GetSectorNameColumnWidth()));
            GUILayout.Label(
                $"Exports={link.ExportVolume:F1}, Imports={link.ImportVolume:F1}, " +
                $"Tariff on our exports={tariffOnOurExports:F2}%, on our imports={tariffOnOurImports:F2}%" +
                (link.HasPlayerTariffOverride ? " (override active)" : ""),
                _labelStyle);
            GUILayout.EndHorizontal();

            GUILayout.Label("Exports:", _labelStyle);
            UiPalette.DrawBar(link.ExportVolume / maxVolume, UiPalette.PositiveChangeColor, 10f);
            GUILayout.Label("Imports:", _labelStyle);
            UiPalette.DrawBar(link.ImportVolume / maxVolume, UiPalette.GetAreaColor(UiPalette.SystemArea.Trade), 10f);

            float buttonWidth = _labelStyle.fontSize * 8f;
            GUILayout.BeginHorizontal();
            // ⚠ BEHAVIOUR 5 FIX, the same shape as DrawMinimumWageControl's. This used to emit a button
            // AND a slider when an override existed, and a button ALONE when it did not - two different
            // control counts on a condition the buttons themselves toggle, which is precisely the
            // positional-control-ID desync DrawTaxPolicyContent's doc comment describes. Both controls
            // are now always emitted; the slider is disabled when there is no override to move.
            bool hasOverride = link.HasPlayerTariffOverride;

            // Control 1 of 2 - the toggle. One button whose label and style switch, rather than two
            // buttons in exclusive branches, so the control COUNT never depends on state.
            if (GUILayout.Button(hasOverride ? "Reset to Default" : "Set Override",
                    hasOverride ? _removeButtonStyle : _implementButtonStyle, GUILayout.Width(buttonWidth)))
            {
                // Both directions are immediate (a structural on/off, like TaxLine.IsImplemented), not a
                // this-turn delta - the preview cache is invalidated right away rather than waiting for
                // the usual slider-changed check. Enabling starts the override at today's EFFECTIVE rate
                // rather than 0, so turning it on never itself changes the tariff.
                link.PlayerTariffOverride = hasOverride
                    ? -1f
                    : Mathf.Clamp(tariffOnOurImports, PartnerTariffOverrideMin, PartnerTariffOverrideMax);
                RecomputePolicyPreview();
            }
            GUILayout.EndHorizontal();

            // Control 2 of 2 - always drawn, disabled when there is no override.
            float standingOverride = hasOverride ? link.PlayerTariffOverride : tariffOnOurImports;
            float newRate = DrawDialRow("    Override rate",
                standingOverride, GetPartnerTariffInput(link.PartnerId, standingOverride),
                PartnerTariffOverrideMin, PartnerTariffOverrideMax, "F2", "%",
                hasOverride ? "via the Trade bill" : "no override set",
                hasOverride);

            if (hasOverride)
            {
                _partnerTariffInputs[link.PartnerId] = newRate;
            }
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
            DrawBillLiveEstimate(ParliamentSystem.GetTradeBillDirection(_playerCountry, BuildTradeBillFromDrafts(), _world));
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
            GUILayout.Label("Spending (Last Year)", _headerStyle);

            FiscalTurnReport report = _simulationManager.GetLastFiscalReport(PlayerCountryId);
            if (report == null)
            {
                GUILayout.Label("No year advanced yet.", _labelStyle);
                return;
            }

            // Pass 5 (2026-08-26): the net is the RECORDED balance, never a hand sum - the old sum here
            // omitted SwfContribution (part of TotalSpending) and would now count the tariff twice,
            // since Revenue already carries it. (Its Baseline + Discretionary terms did reconstruct G
            // exactly; that was never the problem.) Same control count.
            float net = report.BudgetBalance;

            GUILayout.Label($"Revenue (tax, tariffs, fund draw): {UiFormat.Money(report.Revenue, MoneyUnit.Billions)}", _labelStyle);
            GUILayout.Label($"Baseline Government Spending: {UiFormat.Money(report.BaselineGovernmentSpending, MoneyUnit.Billions)}", _labelStyle);
            GUILayout.Label($"Discretionary Spending Change (this year): {UiFormat.MoneyDelta(report.DiscretionarySpending, MoneyUnit.Billions)}", _labelStyle);
            GUILayout.Label($"Mandatory Spending: {UiFormat.Money(report.MandatorySpending, MoneyUnit.Billions)}", _labelStyle);
            GUILayout.Label($"Unemployment Benefit Cost: {UiFormat.Money(report.UnemploymentBenefitCost, MoneyUnit.Billions)}", _labelStyle);
            GUILayout.Label($"Interest On Debt: {UiFormat.Money(report.InterestOnDebt, MoneyUnit.Billions)}", _labelStyle);
            GUILayout.Label($"Welfare Program Cost: {UiFormat.Money(report.WelfareCost, MoneyUnit.Billions)}", _labelStyle);
            GUILayout.Label($"Of which tariff revenue at the stated rates, before the fiscal stance: {UiFormat.Money(report.TariffRevenue, MoneyUnit.Billions)}", _labelStyle);
            GUILayout.Space(6f);
            DrawColoredLabel($"Net (this year's recorded balance): {UiFormat.MoneyDelta(net, MoneyUnit.Billions)}", _headerStyle, UiPalette.GetDeltaColor(net, higherIsBetter: true));
        }

        /// <summary>
        /// Re-surfaces any pending blocking interrupt inside the Budget screen itself.
        /// </summary>
        /// <summary>
        /// The Budget screen's standing explanation. A const rather than a literal at the draw site
        /// because it is now DRAWN in one place and MEASURED in another
        /// (<see cref="BudgetProcessHeaderHeight"/>), and at ordinary window sizes it is the tallest of
        /// the pieces above the columns row — a copy that drifted from the original would take the
        /// reserve with it.
        /// </summary>
        private const string BudgetProcessDescription =
            "Consolidates Tax, Spending, Welfare, Infrastructure, and Sovereign Wealth Fund drafts onto one screen. " +
            "Left: category. Center: that category's line-items (the same draft as its own standalone tab - edits " +
            "apply either place). Right: this year's live estimate across your whole current draft.";

        /// <summary>
        /// Everything <see cref="DrawBudgetProcessTab"/> draws ABOVE its three-column row, measured from
        /// the real strings at the real width rather than assumed from a multiple of the font size.
        ///
        /// ⚠ **This replaces the label-clipping class's original signature.** The old figure was
        /// `_labelStyle.fontSize * 7f + _headerStyle.fontSize + 16f` — a constant standing in for
        /// content, which is precisely the shape CLAUDE.md's seven-instance write-up describes. Seven
        /// site-specific fixes did not end that class; sharing one measurement between the reserve and
        /// the drawing does.
        ///
        /// <para>Five pieces, in the order they are drawn: the header, the interrupt banner when one is
        /// showing, the standing description, the bill-status line, and the Introduce button. Each is
        /// measured in the style it renders in, at
        /// <see cref="PoliSimWidgets.InnerWidth"/> of the available width — measuring at the raw width
        /// would under-count the wrapped lines, which is the quiet way to reintroduce this bug.</para>
        /// </summary>
        private float BudgetProcessHeaderHeight(float availableWidth)
        {
            float textWidth = PoliSimWidgets.InnerWidth(availableWidth, _boxStyle, 1, _labelStyle);

            float height = _headerStyle.CalcHeight(new GUIContent("Budget Process"), textWidth) + _headerStyle.margin.vertical;

            string interruptText = BuildFullScreenInterruptText();
            if (interruptText != null)
            {
                height += _holdBannerStyle.CalcHeight(new GUIContent(interruptText), textWidth) + _holdBannerStyle.margin.vertical + 4f;
            }

            height += _labelStyle.CalcHeight(new GUIContent(BudgetProcessDescription), textWidth) + _labelStyle.margin.vertical;
            height += 8f;

            height += _labelStyle.CalcHeight(new GUIContent(BuildBudgetBillStatusText()), textWidth) + _labelStyle.margin.vertical;
            height += _neutralActionButtonStyle.fixedHeight + _neutralActionButtonStyle.margin.vertical;
            height += 8f;

            return height;
        }

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
            string interruptText = BuildFullScreenInterruptText();
            if (interruptText == null)
            {
                return;
            }

            DrawHoldBannerLabel(interruptText);
            GUILayout.Space(4f);
        }

        /// <summary>
        /// The banner's text, or null when nothing is blocking.
        ///
        /// ⚠ **SPLIT OUT OF THE DRAW SITE SO IT CAN BE MEASURED — the precondition, not a tidy-up.**
        /// <see cref="BudgetProcessHeaderHeight"/> must know how tall this banner will be BEFORE it is
        /// drawn, and a string that exists only as an argument to `GUILayout.Label` cannot be measured by
        /// anything. **You cannot measure what is not a value**, so building it first is step one of the
        /// accessor pattern rather than an incidental cleanup.
        /// </summary>
        private string BuildFullScreenInterruptText()
        {
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

            return blocking.Count == 0
                ? null
                : $"TIME IS PAUSED - waiting on {string.Join(" and ", blocking)}. Open the Decisions tab to resolve it; speed controls are on any other tab.";
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

            // ⚠ availableWidth IS THE WIDTH OF THE PAPER, NOT OF THE SPACE ON IT. Everything below this
            // line sits inside _boxStyle, so the width anything may claim is the paper minus its own
            // padding. Measured at runtime 2026-08-10: a label given availableWidth laid out at 1536
            // starting at x=14, while the box's content ran 14..1522 - overflowing by exactly 28.0,
            // which is padding.horizontal to the pixel.
            //
            // This is the SAME idiom already used by SubTabShare and the Policy/Laws sub-screens; those
            // sites subtract padding and do not clip. These three did not, which is the whole defect.
            float contentWidth = PoliSimWidgets.InnerWidth(availableWidth, _boxStyle);

            DrawColoredLabel("Budget Process", _headerStyle, UiPalette.GetAreaColor(UiPalette.SystemArea.Fiscal));
            DrawFullScreenPendingInterruptBanner();
            // Explicit Width, not left to GUILayout's own inference - the horizontal 3-column row
            // below can otherwise push this outer group's computed "natural" width past the screen
            // edge (a boxed column's GUILayout.Width request plus its GUIStyle's own padding can add
            // up to more than requested), which made this label wrap against an inflated width and
            // clip mid-word rather than wrap. Tying it directly to availableWidth makes its wrap
            // boundary correct regardless of what the row does.
            GUILayout.Label(BudgetProcessDescription, _labelStyle, GUILayout.Width(contentWidth));
            GUILayout.Space(8f);

            DrawBudgetBillStatusAndIntroduce();
            GUILayout.Space(8f);

            // ⚠ INSTANCE #12, BUDGET. The old reserve was
            // `_labelStyle.fontSize * 7f + _headerStyle.fontSize + 16f` - seven notional lines of body
            // type - which is a CONSTANT STANDING IN FOR MEASURED CONTENT, the label-clipping class's
            // original signature. It under-counted whenever the description wrapped past seven lines or
            // the interrupt banner appeared, and the columns row below then ran past the clip rect.
            float columnsHeight = Mathf.Max(0f, availableHeight - BudgetProcessHeaderHeight(availableWidth));
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
            float usableWidth = contentWidth - columnSpacing * 2f - scrollbarAllowance;
            // Floor raised from 5x to 7x the label font: even with wrapping, a button's minimum width is
            // its longest WORD, and "Sovereign" needs ~97px at the smallest supported font - more than the
            // 94px this column got at 16% on a 1227x690 window. Below this floor the category buttons
            // overflow their own column, which is the exact failure the rest of this screen just had.
            float categoryColumnWidth = Mathf.Clamp(usableWidth * 0.16f, _labelStyle.fontSize * 7f, _labelStyle.fontSize * 10f);
            float summaryColumnWidth = usableWidth * 0.34f;
            float centerColumnWidth = usableWidth - categoryColumnWidth - summaryColumnWidth;
            float totalRowWidth = categoryColumnWidth + columnSpacing + centerColumnWidth + columnSpacing + summaryColumnWidth;

            // ⚠ PLAYTEST FIX (2026-08-18): this used to be a SECOND, OUTER scroll wrapping the one
            // below it - a nested pair, found by a project-wide enumeration of every BeginScrollView
            // call site (18 across 2 files; this was the only literal nesting among them). It dates
            // from before the 2026-08-01 fix above: back when the three columns could overflow
            // contentWidth and needed a horizontal safety net. That fix guarantees they now sum to
            // LESS than it - "the row cannot scroll horizontally at any window size" per its own
            // comment above - so the wrapper had nothing left to do. One scroll now: the center
            // column's own, kept deliberately, because its content genuinely does vary by category
            // and does overflow columnsHeight.
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
            float statRowWidth = PoliSimWidgets.InnerWidth(centerColumnWidth, _boxStyle) - 8f;
            UiPalette.SystemArea statArea = GetPolicyScreenArea(_budgetProcessCategory);
            float statRowHeight = PolicyScreenStatsRenderer.MeasureHeight(statArea, _labelStyle, statRowWidth);
            PolicyScreenStatsRenderer.Draw(statArea, _playerCountry, _labelStyle, statRowWidth);

            // Step 2: the trace panel under the chips, pinned with them above the scroll view -
            // its height leaves the scroll budget the same way the stat row's does.
            float budgetTraceGapStance = _simulationManager.GetWageGrowthGapAtPeriodOpen(PlayerCountryId);
            // The host's remaining height under the chips - the panel takes at most its share and
            // scrolls for the rest. Found by the debt section's first capture at 1600 (2026-08-25):
            // this tab's budget-pause state has ~7 rows of room here, and a section measured
            // against the row cap alone ran past the window with every containment guard silent.
            float budgetTraceHostHeight = Mathf.Max(0f, columnsHeight - _labelStyle.fontSize - statRowHeight);
            float budgetTraceHeight = StatTracePanel.MeasureHeight(_playerCountry, budgetTraceGapStance, _labelStyle, statRowWidth, budgetTraceHostHeight);
            StatTracePanel.Draw(_playerCountry, budgetTraceGapStance, _labelStyle, _labelStyle, statRowWidth, budgetTraceHostHeight);

            _budgetProcessCenterScrollPosition = GUILayout.BeginScrollView(_budgetProcessCenterScrollPosition, GUILayout.Height(Mathf.Max(0f, columnsHeight - _labelStyle.fontSize - statRowHeight - budgetTraceHeight)));
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

            GUILayout.Label(BuildBudgetBillStatusText(), _labelStyle);

            bool ambientEnabled = GUI.enabled;
            GUI.enabled = ambientEnabled && pendingBill == null && budgetProcessOpen;
            if (GUILayout.Button("Introduce Budget Bill", _neutralActionButtonStyle))
            {
                _simulationManager.IntroduceBudgetBill(PlayerCountryId, BuildBudgetBillFromDrafts());
            }
            GUI.enabled = ambientEnabled;
        }

        /// <summary>
        /// The bill-status line's text. Split out for the same reason as
        /// <see cref="BuildFullScreenInterruptText"/>: it is drawn here and MEASURED in
        /// <see cref="BudgetProcessHeaderHeight"/>, and its three variants differ in length enough to
        /// change how many lines it wraps to.
        /// </summary>
        private string BuildBudgetBillStatusText()
        {
            BudgetBill pendingBill = _simulationManager.GetPendingBudgetBill(PlayerCountryId);
            if (pendingBill != null)
            {
                return $"An annual budget bill is before Parliament - resolves in {pendingBill.DaysRemaining} day(s).";
            }

            return _simulationManager.GetPendingBudgetProcess(PlayerCountryId)
                ? "The annual budget process is open - introduce your current draft as a bill below to continue."
                : "No budget bill currently before Parliament. One can only be introduced on your country's own fiscal-year date.";
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

            string toggleLabel = pendingBill != null
                // Short labels on purpose. "Introduce Implement Bill" plus the tax/program name beside it
                // needed ~362px inside a column that is 293px at ordinary window sizes, so the button drew
                // straight past the column edge - the same overflow that clipped the preview panel. The
                // words dropped are recoverable from context: the row already names the program, and this
                // screen's own header explains that implementing or removing submits a standalone bill.
                ? $"Pending ({pendingBill.DaysRemaining}d)"
                : taxLine.IsImplemented ? "Remove" : "Implement";
            GUIStyle toggleStyle = taxLine.IsImplemented ? _removeButtonStyle : _implementButtonStyle;

            // ONE ROW, not four stacked lines. Until the first live capture this drew a full-width
            // button, then a sentence-long estimate, then the ledger row - three lines per instrument on
            // the densest screen in the game, where the board draws one. The button and the verdict move
            // onto the row itself; the estimate's prose collapses to the verdict word it was carrying.
            //
            // ⚠ CONTROL ORDER IS PRESERVED EXACTLY: button, then slider, every frame. That is the whole
            // constraint DrawTaxPolicyContent's doc comment describes - GUILayout allocates control IDs
            // positionally and a background bill can resolve mid-drag - and it is order STABILITY that
            // matters, so drawing the same two controls in the same sequence at different rects is safe
            // where varying the sequence would not be.
            Rect fullRow = GUILayoutUtility.GetRect(10f, LedgerRow.Height(_labelStyle), GUILayout.ExpandWidth(true));
            float gap = _labelStyle.fontSize * 0.6f;
            // Measured in the style the button ACTUALLY renders in, not in _labelStyle. The first inline
            // capture measured the label style and "Implement" wrapped to "Implemen / t" - the button
            // style carries its own padding and metrics, so measuring a different style sizes the column
            // for a different string. Same class as the mockup-number rule: a measurement is only valid
            // against the conditions it was taken under.
            float actionWidth = Mathf.Max(fullRow.width * 0.15f, toggleStyle.CalcSize(new GUIContent(toggleLabel)).x + gap);
            float verdictWidth = fullRow.width * 0.13f;

            var actionRect = new Rect(fullRow.xMax - actionWidth, fullRow.y, actionWidth, fullRow.height);
            var verdictRect = new Rect(actionRect.x - verdictWidth - gap, fullRow.y, verdictWidth, fullRow.height);
            var ledgerRect = new Rect(fullRow.x, fullRow.y, verdictRect.x - fullRow.x - gap, fullRow.height);

            // Control 1 of 2.
            bool ambientEnabledForButton = GUI.enabled;
            GUI.enabled = ambientEnabledForButton && pendingBill == null;
            if (GUI.Button(actionRect, toggleLabel, toggleStyle))
            {
                _simulationManager.IntroduceTaxProgramBill(PlayerCountryId, taxLine.Type, !taxLine.IsImplemented);
            }
            GUI.enabled = ambientEnabledForButton;

            DrawTaxProgramBillVerdict(taxLine, pendingBill, verdictRect);

            // v2.0 (POLISIM_V2_SCREEN_SPEC.md §A.9): the three stacked labels this row used to draw -
            // "Standing:", "Draft rate:", and a bare slider - collapse into ONE ledger row where the
            // standing value is a tick on the track, the draft is the knob, and the span between them
            // is hatched in draft amber. Behaviour 1 stops being a colour on a label and becomes a
            // distance the player can read at a glance.
            //
            // The slider IS the current draft (defaulting to the standing Rate until dragged), bounded
            // by this TaxType's own TaxTypeRateRanges - not a small per-turn delta, so a meaningful
            // policy shift (e.g. IncomeTax 37% -> 55%) is reachable in one bill.
            float draftRate = GetTaxRateInput(taxLine.Type, taxLine.Rate);

            // Only an IMPLEMENTED line can have a pending rate change - an unimplemented one is changed
            // by its own standalone Implement/Remove bill above, not by this slider, so it must never
            // show the amber cue regardless of what the (inactive) draft value happens to hold.
            bool hasDraft = taxLine.IsImplemented && !Mathf.Approximately(draftRate, taxLine.Rate);

            // B3: no call site renders currency without naming a MoneyUnit. This is the same per-line
            // figure the revenue breakdown uses, so the two can never disagree.
            float estimatedRevenue = _playerCountry.State.GDP * (taxLine.Rate / 100f) * taxLine.BaseShareOfGdp;

            // GetRect + GUI.HorizontalSlider is exactly what GUILayout.HorizontalSlider does internally,
            // so the control-ID sequence this row emits is unchanged: button, then slider, every frame.
            // See DrawTaxPolicyContent's doc comment on why that ordering is a hang trigger, not taste.
            // Control 2 of 2 - the slider, inside the ledger row.
            float newRate = LedgerRow.Draw(
                ledgerRect,
                DisplayName.Of(taxLine.Type.ToString()),
                taxLine.Rate,
                draftRate,
                taxLine.MinRate,
                taxLine.MaxRate,
                // InvariantCulture, deliberately. UiFormat pins money for this reason and its doc comment
                // names the exact string this machine's sv-SE locale produced ("$29,0T"); a rate printed
                // beside a pinned money figure must not disagree with it about what a decimal point is.
                taxLine.IsImplemented ? taxLine.Rate.ToString("F2", CultureInfo.InvariantCulture) + "%" : "not implemented",
                hasDraft ? draftRate.ToString("F2", CultureInfo.InvariantCulture) + "%" : null,
                taxLine.IsImplemented ? UiFormat.Money(estimatedRevenue, MoneyUnit.Billions) : "-",
                taxLine.IsImplemented,
                _labelStyle,
                _labelStyle,
                _sliderStyle,
                _sliderThumbStyle);

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
        private void DrawTaxProgramBillVerdict(TaxLine taxLine, TaxProgramBill pendingBill, Rect rect)
        {
            TaxProgramBill bill = pendingBill ?? new TaxProgramBill { Type = taxLine.Type, IsAdd = !taxLine.IsImplemented };
            float direction = ParliamentSystem.GetTaxProgramBillDirection(_playerCountry, bill);
            bool wouldPass = ParliamentSystem.WouldBillPass(_playerCountry, direction);

            // The sentence this used to print - "If introduced now: WOULD PASS (current seat
            // composition)" - said the same thing on every one of thirteen rows, so twelve repetitions
            // were carrying no information while costing a line each. The qualifier moves to the
            // screen's own header; the row keeps the verdict, which is the part that varies.
            // "PENDING" when a bill is already in flight, because then the live question is not whether
            // introducing one would pass.
            string text = pendingBill != null ? "PENDING" : wouldPass ? "WOULD PASS" : "WOULD FAIL";
            Color ink = pendingBill != null
                ? PoliSimTheme.TextMuted
                : UiPalette.GetDeltaColor(wouldPass ? 1f : -1f, higherIsBetter: true);

            LedgerRow.Cell(rect, text, _labelStyle, ink, TextAnchor.MiddleRight);
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

            string toggleLabel = pendingBill != null
                // Short labels on purpose. "Introduce Implement Bill" plus the tax/program name beside it
                // needed ~362px inside a column that is 293px at ordinary window sizes, so the button drew
                // straight past the column edge - the same overflow that clipped the preview panel. The
                // words dropped are recoverable from context: the row already names the program, and this
                // screen's own header explains that implementing or removing submits a standalone bill.
                ? $"Pending ({pendingBill.DaysRemaining}d)"
                : welfareProgram.IsImplemented ? "Remove" : "Implement";
            GUIStyle toggleStyle = welfareProgram.IsImplemented ? _removeButtonStyle : _implementButtonStyle;

            // Identical shape to DrawTaxLineRow - see that method for the control-order reasoning, which
            // applies here unchanged: button, then slider, every frame, same sequence at different rects.
            float draftGenerosity = GetWelfareGenerosityInput(welfareProgram.Type, welfareProgram.GenerosityLevel);
            bool hasDraft = welfareProgram.IsImplemented
                && !Mathf.Approximately(draftGenerosity, welfareProgram.GenerosityLevel);

            Rect fullRow = GUILayoutUtility.GetRect(10f, LedgerRow.Height(_labelStyle), GUILayout.ExpandWidth(true));
            float gap = _labelStyle.fontSize * 0.6f;
            float actionWidth = Mathf.Max(fullRow.width * 0.15f, toggleStyle.CalcSize(new GUIContent(toggleLabel)).x + gap);
            float verdictWidth = fullRow.width * 0.13f;

            var actionRect = new Rect(fullRow.xMax - actionWidth, fullRow.y, actionWidth, fullRow.height);
            var verdictRect = new Rect(actionRect.x - verdictWidth - gap, fullRow.y, verdictWidth, fullRow.height);
            var ledgerRect = new Rect(fullRow.x, fullRow.y, verdictRect.x - fullRow.x - gap, fullRow.height);

            // Control 1 of 2.
            bool ambientEnabledForButton = GUI.enabled;
            GUI.enabled = ambientEnabledForButton && pendingBill == null;
            if (GUI.Button(actionRect, toggleLabel, toggleStyle))
            {
                _simulationManager.IntroduceWelfareProgramBill(PlayerCountryId, welfareProgram.Type, !welfareProgram.IsImplemented);
            }
            GUI.enabled = ambientEnabledForButton;

            DrawWelfareProgramBillVerdict(welfareProgram, pendingBill, verdictRect);

            // Control 2 of 2. The slider IS the current draft (defaulting to the standing
            // GenerosityLevel until dragged), bounded 0-100% - not a small per-turn delta, so a
            // meaningful policy shift is reachable in one bill. An unimplemented program is changed by
            // its own standalone bill rather than by this slider, so it never shows the amber cue.
            float newGenerosity = LedgerRow.Draw(
                ledgerRect,
                DisplayName.Of(welfareProgram.Type.ToString()),
                welfareProgram.GenerosityLevel,
                draftGenerosity,
                0f,
                100f,
                welfareProgram.IsImplemented
                    ? welfareProgram.GenerosityLevel.ToString("F0", CultureInfo.InvariantCulture) + "%"
                    : "not implemented",
                hasDraft ? draftGenerosity.ToString("F0", CultureInfo.InvariantCulture) + "%" : null,
                // Cost at FULL generosity, which is what the share is defined against - a real seeded
                // figure rather than one scaled by the draft, so the column answers "how big is this
                // programme" rather than restating the slider.
                welfareProgram.IsImplemented
                    ? welfareProgram.CostShareOfGdp.ToString("F1", CultureInfo.InvariantCulture) + "% GDP"
                    : "-",
                welfareProgram.IsImplemented,
                _labelStyle,
                _labelStyle,
                _sliderStyle,
                _sliderThumbStyle);

            if (welfareProgram.IsImplemented)
            {
                _welfareGenerosityInputs[welfareProgram.Type] = newGenerosity;
            }
        }

        /// <summary>See DrawTaxProgramBillEstimate's own doc comment - identical pattern (GenerosityLevel in place of Rate, WelfareProgramBill in place of TaxProgramBill).</summary>
        private void DrawWelfareProgramBillVerdict(WelfareProgram welfareProgram, WelfareProgramBill pendingBill, Rect rect)
        {
            WelfareProgramBill bill = pendingBill ?? new WelfareProgramBill { Type = welfareProgram.Type, IsAdd = !welfareProgram.IsImplemented };
            float direction = ParliamentSystem.GetWelfareProgramBillDirection(_playerCountry, bill);
            bool wouldPass = ParliamentSystem.WouldBillPass(_playerCountry, direction);

            // See DrawTaxProgramBillVerdict - the "(current seat composition)" qualifier moved to the
            // screen header there for the same reason it moves here, and it is declared to Design as V1.
            string text = pendingBill != null ? "PENDING" : wouldPass ? "WOULD PASS" : "WOULD FAIL";
            Color ink = pendingBill != null
                ? PoliSimTheme.TextMuted
                : UiPalette.GetDeltaColor(wouldPass ? 1f : -1f, higherIsBetter: true);

            LedgerRow.Cell(rect, text, _labelStyle, ink, TextAnchor.MiddleRight);
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
            GUILayout.Label(DisplayName.Spaced(sector.Type.ToString()), _headerStyle, GUILayout.Width(nameColumnWidth));
            GUILayout.Label(
                $"Output {sector.OutputShareOfGdp:F1}% of GDP | Employment {sector.EmploymentShare:F1}% | {GetSectorMetricLabel(sector.Type)} {sector.SectorMetric:F1}",
                _labelStyle);
            GUILayout.EndHorizontal();

            // ⚠ THE SECTOR IS THE GROUP; EACH DIAL IS A ROW. That mapping is not new - it is exactly how
            // Spending groups its 29 lines under Mandatory/Discretionary headings, and the sector's
            // descriptive line above is group CONTEXT (output, employment, its own metric) in the same
            // way "narrower range, higher approval cost" is context for mandatory spending. A dial is the
            // thing with a standing value and a draft; a sector is not.
            //
            // This is the densest sub-screen in the game: eight sectors x five dials is forty rows,
            // against Spending's 29. The group header is what keeps it navigable - it breaks the run
            // into eights, and a reader scans headers rather than rows.
            _sectorSubsidyInputs[sector.Type] = DrawDialRow("Subsidy",
                sector.SubsidyLevel, GetSectorSubsidyInput(sector.Type, sector.SubsidyLevel),
                MinPolicyDialLevel, MaxPolicyDialLevel, "F0", string.Empty, null);

            _sectorRegulationInputs[sector.Type] = DrawDialRow("Regulation",
                sector.RegulationLevel, GetSectorRegulationInput(sector.Type, sector.RegulationLevel),
                MinPolicyDialLevel, MaxPolicyDialLevel, "F0", string.Empty, "0 light - 100 heavy");

            _sectorTaxCreditInputs[sector.Type] = DrawDialRow("Tax Credits",
                sector.TaxCreditLevel, GetSectorTaxCreditInput(sector.Type, sector.TaxCreditLevel),
                MinPolicyDialLevel, MaxPolicyDialLevel, "F0", string.Empty, null);

            _sectorResearchGrantsInputs[sector.Type] = DrawDialRow("Research Grants",
                sector.ResearchGrantsLevel, GetSectorResearchGrantsInput(sector.Type, sector.ResearchGrantsLevel),
                MinPolicyDialLevel, MaxPolicyDialLevel, "F0", string.Empty, null);

            _sectorDeregulationInputs[sector.Type] = DrawDialRow("Deregulation / Nationalization",
                sector.DeregulationNationalizationLevel, GetSectorDeregulationInput(sector.Type, sector.DeregulationNationalizationLevel),
                MinPolicyDialLevel, MaxPolicyDialLevel, "F0", string.Empty, "0 nationalized - 100 private");
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
                ? $"Estimated this year - Contribution/Withdrawal: {_cachedSwfContributionText}, Returns: {_cachedSwfReturnsText}"
                : "Estimated this year - not applicable (no fund).";
            DrawColoredLabel(estimateText, _labelStyle, fund != null
                ? UiPalette.GetDeltaColor(_cachedSwfReturnsEstimateRaw, higherIsBetter: true)
                : UiPalette.GetDeltaColor(0f, higherIsBetter: true));
            // The fund's own existence is a draft too: amber whenever the drafted existence differs from
            // whether a fund actually stands today.
            DrawDraftLabel(draftExists ? "Draft: fund drafted to exist." : "Draft: not implemented.", draftExists != (fund != null));
            GUILayout.Space(8f);

            SovereignWealthFund standingDefaults = fund ?? new SovereignWealthFund();

            // v2.0 (POLISIM_V2_SCREEN_SPEC.md §A.9): six ledger rows. SWF exercises the mapping harder
            // than any other sub-screen, and both of its awkward cases turned out to fit the shape the
            // other four already use rather than needing a new one.
            //
            // ⚠ A NEGATIVE CONTRIBUTION NEEDS NO SPECIAL HANDLING. The rate spans
            // MinSwfContributionRate..Max across zero, so a drawdown is simply a knob left of centre.
            // The standing tick keeps its exact meaning - "where it stands today" - and the hatch still
            // spans standing to draft in whichever direction. A negative value changes where the tick
            // SITS, not what it IS, which is the whole reason for putting it on the track rather than
            // encoding it in a label.
            //
            // Every row emits exactly one control, in the same order as before: Create/Dissolve button
            // above, then contribution, domestic, and the four weights. See DrawTaxPolicyContent's doc
            // comment for why that ordering is a hang trigger rather than a preference.
            float SwfRow(string name, float standing, float draft, float min, float max,
                string format, string suffix, string trailing)
            {
                Rect rowRect = GUILayoutUtility.GetRect(10f, LedgerRow.Height(_labelStyle), GUILayout.ExpandWidth(true));
                bool changed = !Mathf.Approximately(standing, draft);
                return LedgerRow.Draw(
                    rowRect, name, standing, draft, min, max,
                    standing.ToString(format, CultureInfo.InvariantCulture) + suffix,
                    changed ? draft.ToString(format, CultureInfo.InvariantCulture) + suffix : null,
                    trailing,
                    draftExists,
                    _labelStyle, _labelStyle, _sliderStyle, _sliderThumbStyle);
            }

            // Trailing column = what this rate AMOUNTS TO, which is the same question it answers on Tax
            // (estimated revenue) and Spending (share of GDP). Here it is the fund movement this rate
            // actually produces this turn - already computed and cached for the estimate line above, so
            // the row and that line can never disagree.
            float draftContributionRate = GetSwfContributionRateInput(standingDefaults.ContributionRatePercent);
            float newContributionRate = SwfRow(
                "Contribution / Withdrawal",
                standingDefaults.ContributionRatePercent, draftContributionRate,
                MinSwfContributionRate, MaxSwfContributionRate,
                "+0.0;-0.0;0", "%",
                fund != null ? _cachedSwfContributionText : "-");
            if (draftExists)
            {
                _swfContributionRateInput = newContributionRate;
            }

            // The complement, not a restatement: the slider sets the domestic share, so the useful
            // context is what is therefore international.
            float draftDomesticAllocation = GetSwfDomesticAllocationInput(standingDefaults.DomesticAllocationPercent);
            float newDomesticAllocation = SwfRow(
                "Domestic Allocation",
                standingDefaults.DomesticAllocationPercent, draftDomesticAllocation,
                MinPolicyDialLevel, MaxPolicyDialLevel,
                "F0", "%",
                (100f - draftDomesticAllocation).ToString("F0", CultureInfo.InvariantCulture) + "% intl");
            if (draftExists)
            {
                _swfDomesticAllocationInput = newDomesticAllocation;
            }

            GUILayout.Space(8f);
            GUILayout.Label("Asset Class Mix (weights, normalized automatically - don't need to sum to 100)", _labelStyle);

            // ⚠ THE TRAILING COLUMN IS WHAT MAKES NORMALISED WEIGHTS LEGIBLE, and it is why the per-row
            // bars are gone rather than merely moved.
            //
            // These four sliders set RAW weights that normalise against each other, so dragging one
            // silently changes what the other three amount to. Putting each class's normalised "% of
            // fund" in the trailing column means the three rows you did NOT touch visibly move when you
            // drag the fourth - the interaction becomes something you watch rather than something you
            // deduce. The column has answered "what does this row amount to in context" on every
            // sub-screen (revenue on Tax, share of GDP on Spending); on this one the context IS the
            // other rows.
            //
            // Normalised against the DRAFT weights (a throwaway SovereignWealthFund, never the real
            // one) via the same GetNormalizedWeight the real fund uses, rather than duplicating its
            // sum-and-divide logic here.
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

            string Share(SovereignWealthAssetClass assetClass) =>
                (draftWeights.GetNormalizedWeight(assetClass) * 100f).ToString("F0", CultureInfo.InvariantCulture) + "% of fund";

            float newEquities = SwfRow("Equities", standingDefaults.EquitiesWeight, draftEquities,
                MinPolicyDialLevel, MaxPolicyDialLevel, "F0", string.Empty, Share(SovereignWealthAssetClass.Equities));
            if (draftExists)
            {
                _swfEquitiesWeightInput = newEquities;
            }

            float newBonds = SwfRow("Bonds", standingDefaults.BondsWeight, draftBonds,
                MinPolicyDialLevel, MaxPolicyDialLevel, "F0", string.Empty, Share(SovereignWealthAssetClass.Bonds));
            if (draftExists)
            {
                _swfBondsWeightInput = newBonds;
            }

            float newInfrastructure = SwfRow("Infrastructure", standingDefaults.InfrastructureWeight, draftInfrastructure,
                MinPolicyDialLevel, MaxPolicyDialLevel, "F0", string.Empty, Share(SovereignWealthAssetClass.Infrastructure));
            if (draftExists)
            {
                _swfInfrastructureWeightInput = newInfrastructure;
            }

            float newRealEstate = SwfRow("Real Estate", standingDefaults.RealEstateWeight, draftRealEstate,
                MinPolicyDialLevel, MaxPolicyDialLevel, "F0", string.Empty, Share(SovereignWealthAssetClass.RealEstate));
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

            // The per-row size bar is GONE, and its within-group scaling with it.
            //
            // It gave an at-a-glance relative size inside each group, which the ledger's SHARE column now
            // gives numerically and across both groups. At 29 rows the bar cost 8px each - 232px of pure
            // height on the one screen D3 was about - and the board draws no such bar. A row is a line;
            // anything that makes it two is spending the density this screen does not have.
            //
            // MANDATORY vs DISCRETIONARY STAYS EXACTLY AS IT IS: two section headers, each with its own
            // slider range. **The boards never express this distinction at all** - it does not appear
            // anywhere in pass 3's 1b - so there is no spec treatment to adopt and inventing a row-level
            // one would be inventing, not implementing. It is also not a row property: it is a property
            // of a GROUP, and a header is what a group heading looks like. Declared to Design as V2.
            // Each group's bars scale to that group's own largest line, and the header says which scale
            // is in force. Q1's answer keeps SHARE global (a share of GDP means the same thing in both
            // sections, and rebasing it per group would make the column lie), which is only survivable
            // because the bar now carries within-group discrimination that the share column cannot.
            // Ship one without the other and the discretionary tail reads 0.4/0.4/0.3/0.3/0.2 with
            // nothing to tell those rows apart - which is exactly the state Q1 was raised about.
            // ⚠ COUNTRY-COVERAGE FINDING 2, FIXED 2026-08-12 (ruled A): AN EMPTY GROUP RENDERS NOTHING.
            // All five non-USA countries have no Mandatory lines (SeedGenericSpendingLines is
            // discretionary-only), and this method rendered the Mandatory header over zero rows with
            // "bars to $100k" — GroupSpendingMax's divide-by-zero guard formatted as a real money
            // figure. A group header is furniture for its group — the stamps ruling's own logic — and
            // suppressing it is also what keeps the guard value where it belongs: in the arithmetic,
            // never on screen. Group presence varies by COUNTRY, not by frame, so the control set is
            // stable within any session.
            bool hasMandatory = false;
            bool hasDiscretionary = false;
            foreach (SpendingLine spendingLine in _playerCountry.SpendingLines)
            {
                if (spendingLine.IsMandatory) { hasMandatory = true; } else { hasDiscretionary = true; }
            }

            if (hasMandatory)
            {
                float mandatoryMax = GroupSpendingMax(isMandatory: true);
                GUILayout.Label(
                    $"Mandatory (narrower range, higher approval cost) - bars to {UiFormat.Money(mandatoryMax, MoneyUnit.Billions)}",
                    _headerStyle);
                foreach (SpendingLine spendingLine in _playerCountry.SpendingLines)
                {
                    if (!spendingLine.IsMandatory)
                    {
                        continue;
                    }

                    DrawSpendingLineRow(spendingLine, MandatoryPercentChangeRange, mandatoryMax);
                }

                GUILayout.Space(10f);
            }

            if (hasDiscretionary)
            {
                float discretionaryMax = GroupSpendingMax(isMandatory: false);
                GUILayout.Label(
                    $"Discretionary - bars to {UiFormat.Money(discretionaryMax, MoneyUnit.Billions)}",
                    _headerStyle);
                foreach (SpendingLine spendingLine in _playerCountry.SpendingLines)
                {
                    if (spendingLine.IsMandatory)
                    {
                        continue;
                    }

                    DrawSpendingLineRow(spendingLine, DiscretionaryPercentChangeRange, discretionaryMax);
                }
            }
        }

        /// <summary>Interest on Debt is SimulationManager's existing automatic GetInterestOnDebt calculation, not a seeded line - shown as a read-only, clearly-marked-automatic figure from last turn's FiscalTurnReport.</summary>
        private void DrawInterestOnDebtRow()
        {
            FiscalTurnReport report = _simulationManager.GetLastFiscalReport(PlayerCountryId);
            string valueText = report != null ? UiFormat.Money(report.InterestOnDebt, MoneyUnit.Billions) : "not yet computed (advance a year)";
            GUILayout.Label($"Interest on Debt (automatic, last year): {valueText}", _labelStyle);
        }

        /// <summary>One SpendingLine's row: a slider representing a PERCENTAGE change of its own current Amount, bounded by <paramref name="rangePercent"/> (narrower for Mandatory - see DrawSpendingPolicy), showing both the requested percentage and the dollar amount it implies at the line's current size, plus a bar sized relative to <paramref name="maxAmountInGroup"/> (its own Mandatory/Discretionary group's largest line) for an at-a-glance size comparison.</summary>
        /// <summary>The largest line in one spending group - the denominator its bars scale against (V2). Guarded above zero so an empty or all-zero group cannot divide by it.</summary>
        private float GroupSpendingMax(bool isMandatory)
        {
            float max = 0f;
            foreach (SpendingLine spendingLine in _playerCountry.SpendingLines)
            {
                if (spendingLine.IsMandatory == isMandatory)
                {
                    max = Mathf.Max(max, spendingLine.Amount);
                }
            }

            return Mathf.Max(max, 0.0001f);
        }

        private void DrawSpendingLineRow(SpendingLine spendingLine, float rangePercent, float groupMax)
        {
            float draftPercent = GetSpendingLineInput(spendingLine.Category);
            bool hasDraft = !Mathf.Approximately(draftPercent, 0f);
            float draftAmount = spendingLine.Amount * (1f + draftPercent / 100f);

            // ⚠ SPENDING'S STANDING VALUE SITS AT ZERO, and that is the whole translation.
            //
            // A tax row's slider carries the RATE, so its standing tick sits at the enacted rate. A
            // spending row's slider carries a PERCENTAGE CHANGE to its own amount, so the position
            // meaning "as it stands" is 0 - dead centre of a -range..+range track. Mapping standing to 0
            // rather than to the amount is what makes the hatch band read correctly: it spans from
            // no-change to the drafted change, in whichever direction, which is exactly what it means on
            // a tax row too. The units differ; the geometry and behaviour 1 do not.
            Rect rowRect = GUILayoutUtility.GetRect(10f, LedgerRow.Height(_labelStyle), GUILayout.ExpandWidth(true));

            float newPercent = LedgerRow.Draw(
                rowRect,
                DisplayName.Of(spendingLine.Category.ToString()),
                0f,
                draftPercent,
                -rangePercent,
                rangePercent,
                UiFormat.Money(spendingLine.Amount, MoneyUnit.Billions),
                // The draft half prints the AMOUNT it lands at, not the percentage that got it there -
                // the board's column is STANDING then DRAFT, two comparable figures, and "$1.53T then
                // +2.0%" would make the reader do the arithmetic the row exists to have already done.
                hasDraft ? UiFormat.Money(draftAmount, MoneyUnit.Billions) : null,
                SpendingShareOfGdpText(spendingLine.Amount),
                interactive: true,
                _labelStyle,
                _labelStyle,
                _sliderStyle,
                _sliderThumbStyle,
                barFraction: spendingLine.Amount / groupMax);

            _spendingLineInputs[spendingLine.Category] = newPercent;
        }

        /// <summary>This line's share of GDP, the board's trailing column for a spending row. B3: the unit is named, and a share is not money so it takes a format string rather than a MoneyUnit.</summary>
        private string SpendingShareOfGdpText(float amount)
        {
            float gdp = _playerCountry.State.GDP;
            if (gdp <= 0f)
            {
                return "-";
            }

            return (amount / gdp * 100f).ToString("F1", CultureInfo.InvariantCulture) + "% GDP";
        }
    }
}
