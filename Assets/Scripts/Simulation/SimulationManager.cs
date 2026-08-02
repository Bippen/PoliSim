using System.Collections.Generic;
using PoliSim.Data;
using UnityEngine;

namespace PoliSim.Simulation
{
    /// <summary>
    /// Snapshot of one country's fiscal computation for a single turn - the revenue/spending
    /// breakdown behind that turn's change to Budget/GovernmentDebt. Exposed for tools/UI (e.g.
    /// GameController's Trade &amp; Spending panel) that want to show where the money went without
    /// duplicating SimulationManager's formulas.
    /// </summary>
    public class FiscalTurnReport
    {
        public float Revenue;
        public float BaselineGovernmentSpending;
        public float DiscretionarySpending;
        public float MandatorySpending;
        public float UnemploymentBenefitCost;
        public float InterestOnDebt;
        public float TariffRevenue;
        public float WelfareCost;
        public float SwfContribution;
        public float SwfReturns;

        /// <summary>
        /// This turn's total spending and budget balance EXACTLY as ApplyRevenueAndSpending computed
        /// them, recorded rather than recomputed.
        ///
        /// They cannot be reconstructed from the fields above: that method sums
        /// DetailedSpendingResult.GovernmentSpending, while this report carries
        /// BaselineGovernmentSpending - a different field - and DiscretionarySpending here is a
        /// per-turn CHANGE, not a level. Any caller adding the components up would produce a
        /// plausible number that is not the one the simulation used, which is the StatTile
        /// formatting bug's failure shape applied to arithmetic instead of display.
        ///
        /// BudgetBalance is signed the same way the simulation signs it: positive is a SURPLUS.
        /// </summary>
        public float TotalSpending;
        public float BudgetBalance;
    }

    /// <summary>
    /// Estimated single-turn effect of a not-yet-committed PolicyDecision, computed by
    /// SimulationManager.PreviewTurn against a throwaway clone - see that method for what it does
    /// and doesn't reproduce faithfully. Purely a display-layer estimate; nothing here is ever
    /// written back into real EconomyState/Country/World.
    /// </summary>
    public class PolicyPreview
    {
        public float GdpGrowthPercent;
        public float UnemploymentChange;
        public float InflationChange;
        public float ApprovalChange;
        public float NetBudgetImpact;
        public float PovertyRateChange;
        public float LaborForceParticipationRateChange;
        public float CrimeIndexChange;
        public float SwfContributionEstimate;
        public float SwfReturnsEstimate;
    }

    /// <summary>
    /// Drives the turn-based simulation loop for every country in the world: currency/trade
    /// effects resolve first (including each country's own tariff-policy change), then each
    /// country's domestic policy - fiscal (tax/spending/debt), the national accounts identity
    /// (GDP), Okun's Law (unemployment), the Phillips Curve (inflation), approval, and a random
    /// event roll - produces next turn's state. See MacroSystem for the macroeconomic theory and
    /// approval formula themselves, ElectionSystem/EventSystem for the rest of the political layer;
    /// this class only orchestrates turn order and the fiscal-accounting rules.
    ///
    /// Each rule is a small, separately named method with named constants rather than one large
    /// update function, so individual pieces of theory can be tuned or replaced independently.
    /// </summary>
    public class SimulationManager : MonoBehaviour
    {
        public int CurrentTurn { get; private set; }

        /// <summary>
        /// Continuous Time Migration Phase 0 (Master Sequence step 3): 1 turn = 121 in-game days
        /// (~4 months), the SAME conversion "Why this exists" in POLISIM_MASTER_ROADMAP.md's Part One
        /// has used implicitly since ElectionCycle (12 turns = ~4 years, a real presidential term)
        /// was first calibrated. Unchanged by this phase - no constant here gets translated to a daily
        /// rate yet; that's Phases 1-5 (Master Sequence step 7), deliberately much later.
        /// </summary>
        public const int DaysPerTurn = 121;

        /// <summary>The in-game calendar's epoch (Turn 0's date) - a clean, honestly-arbitrary flavor choice, not independently researched, chosen to roughly align with this project's own "seeded with real mid-2026 policy rates" starting data (see WorldFactory).</summary>
        /// <summary>Game start. Public since Step A: PublicationSystem must know it to suppress releases for reference periods that predate the simulation, which have no data behind them.</summary>
        public static readonly System.DateTime EpochDate = new System.DateTime(2026, 1, 1);

        /// <summary>Advances one day at a time via AdvanceDay, driven by GameController's own real-time speed-controlled Update loop - never touched by PreviewTurn's own throwaway clone, so a live slider drag can never leak a phantom day into the real calendar.</summary>
        public System.DateTime CurrentDate { get; private set; } = EpochDate;

        /// <summary>
        /// Continuous Time Migration Phase 0: advances CurrentDate by exactly one in-game day.
        /// Returns true on the day a turn boundary is crossed (every DaysPerTurn-th day since
        /// EpochDate) - the caller (GameController's own Update loop) is still the one that actually
        /// resolves the turn via the existing, UNCHANGED AdvanceTurn(decisions), since only the
        /// caller has visibility into the player's current live PolicyDecision draft. This method
        /// only tracks calendar time and signals when a resolution is due - it never calls
        /// AdvanceTurn itself, and never touches any economic state.
        /// </summary>
        public bool AdvanceDay()
        {
            CurrentDate = CurrentDate.AddDays(1);

            // Master Sequence step 9, Step A: statistics are published on their real release schedules,
            // which are date-driven ("first Friday", "the 12th", "t+30 after quarter end"), so this is
            // the only correct place for it - once per simulated day, after the date has advanced.
            //
            // PublicationSystem READS country.State and WRITES only country.Published. It returns no
            // value and nothing below consumes its output, so it cannot influence the turn boundary
            // computed here or anything AdvanceTurn later does. Its revision noise draws from
            // SimulationRandom's own PublicationRevision stream, so it cannot perturb any other
            // consumer's draw sequence either.
            if (_world != null)
            {
                foreach (Country country in _world.Countries)
                {
                    PublicationSystem.PublishDueFigures(country, CurrentDate);

                    // Step C4's scheduled rating review, deliberately AFTER the day's closings are
                    // recorded above so a review landing on a period boundary reads that period's own
                    // closing rather than the previous one. Same one-directional guarantee as
                    // publishing: it reads State/Published and writes only Country.Rating, which nothing
                    // in the simulation consumes, so it cannot influence the turn boundary computed
                    // below. It draws no randomness either, so the seeded trajectory is unaffected.
                    CreditRatingSystem.ReviewIfDue(country, CurrentDate);
                }
            }

            int daysSinceEpoch = (int)(CurrentDate - EpochDate).TotalDays;
            return daysSinceEpoch > 0 && daysSinceEpoch % DaysPerTurn == 0;
        }

        [SerializeField]
        private World _world;

        public World World => _world;

        // --- Fiscal accounting: automatic stabilizers + sovereign risk premium on debt ---

        /// <summary>Conventional "safe" debt-to-GDP benchmark (the EU Stability & Growth Pact reference value) above which lenders start charging extra.</summary>
        private const float RiskFreeDebtToGdpPercent = 60f;

        /// <summary>Extra interest-rate points charged per point of debt-to-GDP above the risk-free threshold.</summary>
        private const float DebtRiskPremiumRate = 0.02f;

        /// <summary>Caps the risk premium - otherwise it scales with Debt/GDP while also multiplying Debt, making InterestOnDebt quadratic in Debt and able to diverge to infinity within a handful of turns.</summary>
        private const float MaxDebtRiskPremium = 5f;

        /// <summary>Hard ceiling on debt-to-GDP - a sustained structural deficit with no policy response (e.g. this turn's GovernmentSpendingRate exceeding TaxRate) shouldn't be able to grow without bound.</summary>
        private const float MaxDebtToGdpPercent = 300f;

        /// <summary>
        /// The fiscal reaction function's slope: how much the effective-revenue multiplier moves per
        /// point of gap between DebtToGdpRatio and the country's own ComfortableDebtToGdpPercent (see
        /// GetFiscalReactionMultiplier). This is the missing NEGATIVE feedback the debt-to-GDP system
        /// previously lacked - GetDebtRiskPremium is a real, separate mechanism (the market's own cost
        /// of lending more to an already-indebted borrower) and stays completely unchanged; it was
        /// never a substitute for a government's own countercyclical fiscal response, which is what
        /// this represents. See "Fiscal Reaction Function" in CLAUDE.md for the empirical case this
        /// value was calibrated against (every one of 0.05-0.3 failed to escape the pre-existing
        /// bimodal 0%/~294% outcome; 1.0+ genuinely stabilizes all six countries at distinct,
        /// country-appropriate levels, confirmed flat from turn 500 through turn 2000).
        /// </summary>
        private const float FiscalReactionSensitivity = 1.5f;

        /// <summary>Bounds on GetFiscalReactionMultiplier's output - a 2x range (0.5x-1.5x effective revenue) is what the calibration above needed to actually overcome the debt-risk-premium's own reinforcing loop at realistic debt-to-GDP extremes, not a "modest" single-digit-percent cap that empirically failed to do so.</summary>
        private const float MinFiscalReactionMultiplier = 0.5f;
        private const float MaxFiscalReactionMultiplier = 1.5f;

        /// <summary>
        /// The missing negative feedback in the debt-to-GDP system: a country's own government
        /// modestly tightens (collects relatively more of its theoretical tax revenue) as debt rises
        /// above its ComfortableDebtToGdpPercent anchor, and loosens (collects relatively less) as debt
        /// falls below it - independent of, and stacked on top of, CollectionEfficiency and
        /// GetDebtRiskPremium (which represents the market's side of the equation, not the
        /// government's). Without this, the system was found to be bimodal - every country's
        /// DebtToGdpRatio eventually settled at either exactly 0% or pinned near the 300% ceiling,
        /// never anything in between, however Discretionary/Mandatory spending growth was tuned (see
        /// "SpendingLine Amount Ceiling - Debt-to-Zero Fix" in CLAUDE.md for that investigation).
        /// </summary>
        private float GetFiscalReactionMultiplier(Country country)
        {
            float debtGap = country.State.DebtToGdpRatio - country.ComfortableDebtToGdpPercent;
            float multiplier = 1f + FiscalReactionSensitivity * debtGap / 100f;
            return Mathf.Clamp(multiplier, MinFiscalReactionMultiplier, MaxFiscalReactionMultiplier);
        }

        /// <summary>Sane bounds for a country's own tariff policy (see PolicyDecision.TariffRateChange).</summary>
        private const float MinBaseTariffRate = 0f;
        private const float MaxBaseTariffRate = 50f;

        /// <summary>Bounds for a WelfareProgram's GenerosityLevel - uniform across every WelfareProgramType (unlike TaxLine's per-type MinRate/MaxRate), since the task specifies a single 0-100% range for all six.</summary>
        private const float MinGenerosityLevel = 0f;
        private const float MaxGenerosityLevel = 100f;

        /// <summary>Bounds for Country.MinimumWagePercentOfMedian (a Kaitz-index-style percent of median wage) - a gameplay ceiling above any real-world minimum wage's Kaitz index, not a researched maximum.</summary>
        private const float MinMinimumWagePercent = 0f;
        private const float MaxMinimumWagePercent = 100f;

        /// <summary>Bounds for Country.PoliceFundingLevel/SentencingSeverity - both share one uniform 0-100 range (unlike TaxLine's per-type ranges), since there's no per-country real-world figure to bound them against.</summary>
        private const float MinPolicyDialLevel = 0f;
        private const float MaxPolicyDialLevel = 100f;

        /// <summary>Bounds for Sector.SubsidyLevel/RegulationLevel - reuses the same [0,100] range as the crime/justice dials, since there's likewise no per-sector real-world figure to bound them against.</summary>
        private const float MinSectorDialLevel = 0f;
        private const float MaxSectorDialLevel = 100f;

        /// <summary>
        /// Bounds for SovereignWealthFund.ContributionRatePercent - a gameplay ceiling (10% of GDP/
        /// turn is already an aggressive contribution rate for any country), not a researched
        /// maximum. The negative half of this range (Round 3 item 1, the SWF drawdown mechanic) is
        /// the same magnitude as the positive half, deliberately - the player might reasonably want
        /// to unwind an aggressive contribution habit just as quickly as they built it during a real
        /// emergency, and this is a policy LEVER the player chooses to pull, not an automatic
        /// recession-triggered drawdown, so no separate, narrower cap was invented for this pass.
        /// </summary>
        private const float MinSwfContributionRate = -10f;
        private const float MaxSwfContributionRate = 10f;

        /// <summary>Bounds for SovereignWealthFund's DomesticAllocationPercent and four asset-class weights - shares the same [0,100] range idiom as the other uniform policy dials in this session's work.</summary>
        private const float MinSwfDialLevel = 0f;
        private const float MaxSwfDialLevel = 100f;

        /// <summary>Bounds for Country.PaidFamilyLeaveWeeks - a gameplay ceiling (104 weeks = 2 years, comfortably above Sweden's real ~69-week benchmark), not a researched maximum.</summary>
        private const float MinPaidFamilyLeaveWeeks = 0f;
        private const float MaxPaidFamilyLeaveWeeks = 104f;

        /// <summary>Bounds for Country.OvertimeRegulationLevel/RetrainingProgramLevel - shares the same [0,100] uniform-dial idiom as PoliceFundingLevel/SentencingSeverity.</summary>
        private const float MinLaborDialLevel = 0f;
        private const float MaxLaborDialLevel = 100f;

        /// <summary>
        /// Ceiling on SovereignWealthFund.TotalAssets, as a percentage of GDP - matches
        /// MaxDebtToGdpPercent's own number for consistency, a gameplay safety bound not a realistic
        /// target (found necessary during validation: sustained maximum contribution (10% of GDP)
        /// into 100% Equities, held for 500 turns with no rebalancing, compounds far faster than GDP
        /// forever since the fund's average return (9%) structurally exceeds trend GDP growth - left
        /// unclamped, this drove the budget's cumulative total to an astronomically large (still
        /// finite, but unrealistic) figure within a few hundred turns. Mirrors GovernmentDebt's own
        /// clamp exactly - the flow (this turn's contribution/returns) is still computed and reported
        /// accurately even in a turn that hits the ceiling; only the STOCK stops compounding further.
        /// </summary>
        private const float MaxSwfToGdpPercent = 300f;

        /// <summary>Bounds for this turn's requested PERCENTAGE change to a Discretionary SpendingLine (see PolicyDecision.SpendingLineChanges).</summary>
        private const float DiscretionaryPercentChangeRange = 30f;

        /// <summary>
        /// Narrower than DiscretionaryPercentChangeRange - reflects the real political difficulty of
        /// entitlement reform (Social Security, Medicare, Medicaid, etc. aren't cut or expanded as
        /// freely, in one turn, as a Discretionary line).
        /// </summary>
        private const float MandatoryPercentChangeRange = 15f;

        /// <summary>
        /// Hard floor/ceiling on every SpendingLine's Amount, expressed as a multiple of that line's
        /// own SpendingLine.SeedAmount - not of its current Amount. Percentage-of-current-value changes
        /// compound geometrically if the same large percentage is held for many turns in a row (see
        /// "Percentage-Based Spending Sliders" in CLAUDE.md for the runaway-divergence finding this
        /// closes off); anchoring the clamp to SeedAmount, rather than letting it ride with the current
        /// Amount, is what actually stops the compounding - a clamp relative to the current value would
        /// just get carried along by the same exponential growth it's supposed to bound. Applies to
        /// both Discretionary (ApplySpendingLineChanges) and Mandatory categories' PLAYER-driven
        /// changes, regardless of how many turns of repeated changes are stacked.
        ///
        /// For a Discretionary line, SeedAmount is NOT frozen at construction - ApplyDiscretionarySpendingGrowth
        /// grows it in lockstep with the automatic GDP-tracking growth applied to Amount (see that
        /// method's doc comment for why this was necessary - an earlier version left SeedAmount fixed
        /// forever, which silently froze this ceiling in absolute dollar terms and broke the "G tracks
        /// GDP" property "Discretionary Spending Growth" depends on; see "SpendingLine Amount Ceiling
        /// - Debt-to-Zero Fix" in CLAUDE.md). A Mandatory line's SeedAmount stays genuinely fixed at
        /// construction, since Mandatory lines have no automatic growth mechanism to track in the
        /// first place.
        /// </summary>
        private const float MinSpendingLineAmountRatio = 0.2f;
        private const float MaxSpendingLineAmountRatio = 3.0f;

        private static float ClampToSeedRange(SpendingLine line, float amount)
        {
            return Mathf.Clamp(amount, line.SeedAmount * MinSpendingLineAmountRatio, line.SeedAmount * MaxSpendingLineAmountRatio);
        }

        private readonly Dictionary<CountryId, FiscalTurnReport> _lastFiscalReports = new Dictionary<CountryId, FiscalTurnReport>();
        private readonly Dictionary<CountryId, EconomicEvent> _lastEventsByCountry = new Dictionary<CountryId, EconomicEvent>();

        /// <summary>
        /// Political Systems Overhaul Part A: cabinet decisions rolled but not yet resolved by a
        /// player-picked response, per country - unlike EconomicEvent (auto-applied, so only ever
        /// this turn's single result needs remembering), these persist across frames until
        /// ResolveCabinetDecision clears them, since GameController blocks Advance Turn while any
        /// remain (mirrors the existing hasPendingFedChairSelection gate - see OnGUI).
        /// </summary>
        private readonly Dictionary<CountryId, List<(CabinetPortfolio Portfolio, CabinetDecision Decision)>> _pendingCabinetDecisionsByCountry =
            new Dictionary<CountryId, List<(CabinetPortfolio, CabinetDecision)>>();

        /// <summary>
        /// Continuous Time Migration Phase 0 short-term gameplay scaffolding: at most ONE pending
        /// foreign policy meeting per country at a time (unlike Cabinet's per-minister list, there's
        /// only one "foreign ministry" rolling here) - see TryRollForeignPolicyMeeting/
        /// ResolveForeignPolicyMeeting and GameController.Update's own gate on it.
        /// </summary>
        private readonly Dictionary<CountryId, ForeignPolicyMeeting> _pendingForeignPolicyMeetingByCountry =
            new Dictionary<CountryId, ForeignPolicyMeeting>();

        /// <summary>
        /// Political Systems Overhaul Part B, full rollout (Master Sequence step 5a): a country's
        /// annual budget process is open (its FiscalYearData start date has arrived) and the player
        /// hasn't yet introduced a BudgetBill - single-slot per country, mirroring
        /// _pendingForeignPolicyMeetingByCountry's own pattern. This is Phase 1 of the annual cycle
        /// (mandatory pause, blocks GameController.Update's day-loop); IntroduceBudgetBill clears this
        /// AND starts Phase 2 (_pendingBudgetBillByCountry's own 21-day countdown, which does NOT
        /// pause). Only ever set for PlayerCountryId (see TryOpenBudgetProcess) - the other five
        /// countries are AI-controlled, never enter this set, and their own annual budget dates are
        /// currently a no-op (Master Sequence step 5c scope note: no AI budget-drafting logic exists
        /// anywhere in this codebase yet, so there's nothing to bundle into a bill for them - resolve
        /// this properly, not by guessing at AI policy-making, if/when it's actually needed).
        /// </summary>
        private readonly HashSet<CountryId> _pendingBudgetProcessByCountry = new HashSet<CountryId>();

        /// <summary>
        /// Political Systems Overhaul Part B, full rollout (Master Sequence step 5c): at most one
        /// pending omnibus BudgetBill per country, mirroring _pendingForeignPolicyMeetingByCountry's
        /// own single-slot pattern - Phase 2 of the annual cycle (see _pendingBudgetProcessByCountry's
        /// own doc comment). Counts down via AdvanceBudgetBillDay (called once per simulated day,
        /// independent of the 121-day turn boundary), the same deterministic-countdown idiom the
        /// Master Sequence step 4 pilot's TaxBill/AdvanceLegislativeDay already established (retired -
        /// see git history - BudgetBill generalizes it to Tax+Spending+Welfare+SWF together).
        /// </summary>
        private readonly Dictionary<CountryId, BudgetBill> _pendingBudgetBillByCountry =
            new Dictionary<CountryId, BudgetBill>();

        /// <summary>
        /// Master Sequence step 5d, tier 2: standalone program add/remove bills, keyed by CountryId
        /// then by the specific TaxType/WelfareProgramType being added or removed - unlike
        /// _pendingBudgetBillByCountry's single slot, MULTIPLE different program bills can be pending
        /// for the same country at once (e.g. "add UBI" and "remove CarbonTax" simultaneously), just
        /// never two for the SAME program at the same time. Introducible anytime, no mandatory pause -
        /// counts down independently via AdvanceTaxProgramBillsDay/AdvanceWelfareProgramBillsDay, the
        /// same non-blocking daily idiom _pendingBudgetBillByCountry's own Phase 2 already established.
        /// </summary>
        private readonly Dictionary<CountryId, Dictionary<TaxType, TaxProgramBill>> _pendingTaxProgramBillsByCountry =
            new Dictionary<CountryId, Dictionary<TaxType, TaxProgramBill>>();

        /// <summary>WelfareProgramType equivalent of _pendingTaxProgramBillsByCountry - see that field's own doc comment, same pattern.</summary>
        private readonly Dictionary<CountryId, Dictionary<WelfareProgramType, WelfareProgramBill>> _pendingWelfareProgramBillsByCountry =
            new Dictionary<CountryId, Dictionary<WelfareProgramType, WelfareProgramBill>>();

        /// <summary>
        /// Master Sequence step 5d, tier 3: at most one pending standalone bill per country per
        /// non-budget policy tab (Labor Market/Crime &amp; Justice/Economic Sectors/Trade), mirroring
        /// _pendingBudgetBillByCountry's own single-slot-per-country pattern - one bill per TAB, not
        /// per dial (see the roadmap's own "one bill per tab" design confirmation). Introducible
        /// anytime, no mandatory pause, same non-blocking daily countdown idiom as the tier-2
        /// dictionaries above.
        /// </summary>
        private readonly Dictionary<CountryId, LaborPolicyBill> _pendingLaborBillByCountry =
            new Dictionary<CountryId, LaborPolicyBill>();

        private readonly Dictionary<CountryId, CrimeJusticePolicyBill> _pendingCrimeJusticeBillByCountry =
            new Dictionary<CountryId, CrimeJusticePolicyBill>();

        private readonly Dictionary<CountryId, SectorPolicyBill> _pendingSectorBillByCountry =
            new Dictionary<CountryId, SectorPolicyBill>();

        private readonly Dictionary<CountryId, TradePolicyBill> _pendingTradeBillByCountry =
            new Dictionary<CountryId, TradePolicyBill>();

        /// <summary>The most recent turn's fiscal breakdown for a country, or null if no turn has been advanced yet.</summary>
        public FiscalTurnReport GetLastFiscalReport(CountryId countryId)
        {
            return _lastFiscalReports.TryGetValue(countryId, out FiscalTurnReport report) ? report : null;
        }

        /// <summary>The event that fired for a country this turn, or null if none did (most turns).</summary>
        public EconomicEvent GetLastEvent(CountryId countryId)
        {
            return _lastEventsByCountry.TryGetValue(countryId, out EconomicEvent economicEvent) ? economicEvent : null;
        }

        /// <summary>Every cabinet decision rolled for this country that the player hasn't responded to yet (usually empty) - see GameController's Cabinet tab and hasPendingCabinetDecisions gate.</summary>
        public List<(CabinetPortfolio Portfolio, CabinetDecision Decision)> GetPendingCabinetDecisions(CountryId countryId)
        {
            return _pendingCabinetDecisionsByCountry.TryGetValue(countryId, out var pending) ? pending : new List<(CabinetPortfolio, CabinetDecision)>();
        }

        /// <summary>Applies the player's chosen response to one pending cabinet decision and clears it from the pending list - called once per response, from GameController.</summary>
        public void ResolveCabinetDecision(CountryId countryId, CabinetPortfolio portfolio, CabinetDecision decision, CabinetDecisionOption chosenOption)
        {
            Country country = _world.GetCountry(countryId);
            CabinetSystem.ApplyDecisionOption(country, chosenOption);

            if (_pendingCabinetDecisionsByCountry.TryGetValue(countryId, out var pending))
            {
                pending.RemoveAll(p => p.Portfolio == portfolio && p.Decision == decision);
            }
        }

        /// <summary>The pending foreign policy meeting for this country, or null if none is currently awaiting a response (the common case) - see GameController's Foreign Policy tab and the Update() gate on it.</summary>
        public ForeignPolicyMeeting GetPendingForeignPolicyMeeting(CountryId countryId)
        {
            return _pendingForeignPolicyMeetingByCountry.TryGetValue(countryId, out ForeignPolicyMeeting meeting) ? meeting : null;
        }

        /// <summary>
        /// Rolls, once per simulated day, whether a new meeting fires for this country - a no-op if
        /// one is already pending (single-slot, see _pendingForeignPolicyMeetingByCountry's own doc
        /// comment), so GameController.Update can safely call this every day the loop advances without
        /// checking first itself. Called only for the player's country - unlike Cabinet decisions
        /// (rolled for every country every turn as part of AdvanceTurn), NPC countries have no UI to
        /// ever resolve a meeting through, so there'd be nothing to roll for them.
        /// </summary>
        public void TryRollForeignPolicyMeeting(CountryId countryId)
        {
            if (_pendingForeignPolicyMeetingByCountry.ContainsKey(countryId))
            {
                return;
            }

            ForeignPolicyMeeting meeting = ForeignPolicySystem.TryRollMeeting();
            if (meeting != null)
            {
                _pendingForeignPolicyMeetingByCountry[countryId] = meeting;
            }
        }

        /// <summary>Applies the player's chosen response to the pending foreign policy meeting and clears it - called once per response, from GameController.</summary>
        public void ResolveForeignPolicyMeeting(CountryId countryId, ForeignPolicyMeetingOption chosenOption)
        {
            Country country = _world.GetCountry(countryId);
            ForeignPolicySystem.ApplyMeetingOption(country, chosenOption);
            _pendingForeignPolicyMeetingByCountry.Remove(countryId);
        }

        /// <summary>The pending omnibus BudgetBill for this country, or null if none is currently before Parliament (the common case).</summary>
        public BudgetBill GetPendingBudgetBill(CountryId countryId)
        {
            return _pendingBudgetBillByCountry.TryGetValue(countryId, out BudgetBill bill) ? bill : null;
        }

        /// <summary>
        /// Submits a new omnibus BudgetBill for this country - a no-op (returns false) if one is
        /// already pending, since only one bill may be before Parliament at a time (see
        /// _pendingBudgetBillByCountry's own doc comment). Also closes out the mandatory pause this
        /// bill's introduction was blocking (_pendingBudgetProcessByCountry) - introducing IS the
        /// action the pause exists to force, so time resumes the moment this succeeds, and the bill
        /// then resolves quietly in the background over BillDurationDays, never pausing again.
        /// DaysRemaining is set here, not by the caller, so GameController never has to know
        /// ParliamentSystem.BillDurationDays itself.
        /// </summary>
        public bool IntroduceBudgetBill(CountryId countryId, BudgetBill bill)
        {
            if (_pendingBudgetBillByCountry.ContainsKey(countryId))
            {
                return false;
            }

            bill.DaysRemaining = ParliamentSystem.BillDurationDays;
            _pendingBudgetBillByCountry[countryId] = bill;
            _pendingBudgetProcessByCountry.Remove(countryId);
            return true;
        }

        /// <summary>
        /// Counts down the pending BudgetBill (if any) by one day, resolving it PASS/FAIL via
        /// ParliamentSystem once DaysRemaining reaches 0 - called once per simulated day from
        /// GameController.Update's day-processing loop, independent of the 121-day turn boundary
        /// (unlike the mandatory pause that preceded introduction, resolving a bill never pauses time -
        /// it's a deterministic countdown, not something needing a player response, the same idiom the
        /// retired TaxBill/AdvanceLegislativeDay already established).
        /// </summary>
        public void AdvanceBudgetBillDay(CountryId countryId)
        {
            if (!_pendingBudgetBillByCountry.TryGetValue(countryId, out BudgetBill bill))
            {
                return;
            }

            bill.DaysRemaining--;
            if (bill.DaysRemaining > 0)
            {
                return;
            }

            Country country = _world.GetCountry(countryId);
            bool passed = ParliamentSystem.WouldBillPass(country, bill);
            ParliamentSystem.ApplyBillResult(country, bill, passed, ApplyBudgetBillSpendingAndSwf);
            _pendingBudgetBillByCountry.Remove(countryId);
        }

        /// <summary>
        /// The Spending/SWF half of BudgetBill application that ParliamentSystem.ApplyBillResult
        /// delegates back here (passed in as a callback so ParliamentSystem never needs a direct
        /// dependency on SimulationManager's private internals) - reuses ApplySpendingLineChanges/
        /// ApplySwfPolicyChanges via a throwaway PolicyDecision built from the bill's own fields,
        /// rather than duplicating their clamping logic, plus direct SWF create/dissolve handling
        /// (neither existing function owns that - both assume the fund's existence is already settled
        /// going in). Only ever called from ApplyBillResult on a PASS - never on FAIL, never standalone.
        /// </summary>
        private void ApplyBudgetBillSpendingAndSwf(Country country, BudgetBill bill)
        {
            var spendingDecision = new PolicyDecision { SpendingLineChanges = bill.SpendingPercentChanges };
            ApplySpendingLineChanges(country, spendingDecision);

            if (!bill.SwfShouldExist)
            {
                country.SovereignWealthFund = null;
                return;
            }

            if (country.SovereignWealthFund == null)
            {
                country.SovereignWealthFund = new SovereignWealthFund();
            }

            var swfDecision = new PolicyDecision
            {
                SwfContributionRateOverride = bill.SwfContributionRatePercent,
                SwfDomesticAllocationOverride = bill.SwfDomesticAllocationPercent,
                SwfEquitiesWeightOverride = bill.SwfEquitiesWeight,
                SwfBondsWeightOverride = bill.SwfBondsWeight,
                SwfInfrastructureWeightOverride = bill.SwfInfrastructureWeight,
                SwfRealEstateWeightOverride = bill.SwfRealEstateWeight
            };
            ApplySwfPolicyChanges(country, swfDecision);
        }

        /// <summary>Every TaxProgramBill currently pending for this country, or an empty collection if none - see _pendingTaxProgramBillsByCountry's own doc comment.</summary>
        public IEnumerable<TaxProgramBill> GetPendingTaxProgramBills(CountryId countryId)
        {
            return _pendingTaxProgramBillsByCountry.TryGetValue(countryId, out var pending) ? pending.Values : System.Array.Empty<TaxProgramBill>();
        }

        /// <summary>Submits a new standalone TaxProgramBill - a no-op (returns false) if one is already pending for this SAME TaxType (different TaxTypes may all have their own bill pending at once - see _pendingTaxProgramBillsByCountry's own doc comment). No mandatory pause to close - unlike IntroduceBudgetBill, this tier never blocks time in the first place.</summary>
        public bool IntroduceTaxProgramBill(CountryId countryId, TaxType type, bool isAdd)
        {
            if (!_pendingTaxProgramBillsByCountry.TryGetValue(countryId, out var pending))
            {
                pending = new Dictionary<TaxType, TaxProgramBill>();
                _pendingTaxProgramBillsByCountry[countryId] = pending;
            }

            if (pending.ContainsKey(type))
            {
                return false;
            }

            pending[type] = new TaxProgramBill { Type = type, IsAdd = isAdd, DaysRemaining = ParliamentSystem.BillDurationDays };
            return true;
        }

        /// <summary>Counts down every pending TaxProgramBill for this country by one day, resolving any that reach 0 - called once per simulated day, the same non-blocking idiom AdvanceBudgetBillDay already established.</summary>
        public void AdvanceTaxProgramBillsDay(CountryId countryId)
        {
            if (!_pendingTaxProgramBillsByCountry.TryGetValue(countryId, out var pending) || pending.Count == 0)
            {
                return;
            }

            Country country = _world.GetCountry(countryId);
            var resolved = new List<TaxType>();
            foreach (TaxProgramBill bill in pending.Values)
            {
                bill.DaysRemaining--;
                if (bill.DaysRemaining > 0)
                {
                    continue;
                }

                bool passed = ParliamentSystem.WouldBillPass(country, ParliamentSystem.GetTaxProgramBillDirection(country, bill));
                ParliamentSystem.ApplyTaxProgramBillResult(country, bill, passed);
                resolved.Add(bill.Type);
            }

            foreach (TaxType type in resolved)
            {
                pending.Remove(type);
            }
        }

        /// <summary>WelfareProgramType equivalent of GetPendingTaxProgramBills - see that method's own doc comment, same pattern.</summary>
        public IEnumerable<WelfareProgramBill> GetPendingWelfareProgramBills(CountryId countryId)
        {
            return _pendingWelfareProgramBillsByCountry.TryGetValue(countryId, out var pending) ? pending.Values : System.Array.Empty<WelfareProgramBill>();
        }

        /// <summary>WelfareProgramType equivalent of IntroduceTaxProgramBill - see that method's own doc comment, same pattern.</summary>
        public bool IntroduceWelfareProgramBill(CountryId countryId, WelfareProgramType type, bool isAdd)
        {
            if (!_pendingWelfareProgramBillsByCountry.TryGetValue(countryId, out var pending))
            {
                pending = new Dictionary<WelfareProgramType, WelfareProgramBill>();
                _pendingWelfareProgramBillsByCountry[countryId] = pending;
            }

            if (pending.ContainsKey(type))
            {
                return false;
            }

            pending[type] = new WelfareProgramBill { Type = type, IsAdd = isAdd, DaysRemaining = ParliamentSystem.BillDurationDays };
            return true;
        }

        /// <summary>WelfareProgramType equivalent of AdvanceTaxProgramBillsDay - see that method's own doc comment, same pattern.</summary>
        public void AdvanceWelfareProgramBillsDay(CountryId countryId)
        {
            if (!_pendingWelfareProgramBillsByCountry.TryGetValue(countryId, out var pending) || pending.Count == 0)
            {
                return;
            }

            Country country = _world.GetCountry(countryId);
            var resolved = new List<WelfareProgramType>();
            foreach (WelfareProgramBill bill in pending.Values)
            {
                bill.DaysRemaining--;
                if (bill.DaysRemaining > 0)
                {
                    continue;
                }

                bool passed = ParliamentSystem.WouldBillPass(country, ParliamentSystem.GetWelfareProgramBillDirection(country, bill));
                ParliamentSystem.ApplyWelfareProgramBillResult(country, bill, passed);
                resolved.Add(bill.Type);
            }

            foreach (WelfareProgramType type in resolved)
            {
                pending.Remove(type);
            }
        }

        /// <summary>The pending standalone Labor Market bill for this country, or null if none is currently before Parliament.</summary>
        public LaborPolicyBill GetPendingLaborBill(CountryId countryId)
        {
            return _pendingLaborBillByCountry.TryGetValue(countryId, out LaborPolicyBill bill) ? bill : null;
        }

        /// <summary>Submits a new standalone LaborPolicyBill - a no-op (returns false) if one is already pending, same single-slot pattern as IntroduceBudgetBill. No mandatory pause to close - this tier never blocks time.</summary>
        public bool IntroduceLaborBill(CountryId countryId, LaborPolicyBill bill)
        {
            if (_pendingLaborBillByCountry.ContainsKey(countryId))
            {
                return false;
            }

            bill.DaysRemaining = ParliamentSystem.BillDurationDays;
            _pendingLaborBillByCountry[countryId] = bill;
            return true;
        }

        /// <summary>Counts down the pending LaborPolicyBill (if any) by one day, resolving it once DaysRemaining reaches 0 - same non-blocking idiom as AdvanceBudgetBillDay.</summary>
        public void AdvanceLaborBillDay(CountryId countryId)
        {
            if (!_pendingLaborBillByCountry.TryGetValue(countryId, out LaborPolicyBill bill))
            {
                return;
            }

            bill.DaysRemaining--;
            if (bill.DaysRemaining > 0)
            {
                return;
            }

            Country country = _world.GetCountry(countryId);
            bool passed = ParliamentSystem.WouldBillPass(country, ParliamentSystem.GetLaborBillDirection(country, bill));
            ParliamentSystem.ApplyLaborBillResult(country, bill, passed, ApplyLaborBillEffects);
            _pendingLaborBillByCountry.Remove(countryId);
        }

        /// <summary>The Labor Market bill's apply delegate - reuses the existing ApplyMinimumWageChange/ApplyLaborPolicyChanges/ApplyDemographicPolicyChanges (private, clamp-owning) via a throwaway PolicyDecision, the same reuse pattern ApplyBudgetBillSpendingAndSwf already established.</summary>
        private void ApplyLaborBillEffects(Country country, LaborPolicyBill bill)
        {
            var decision = new PolicyDecision
            {
                MinimumWageOverride = bill.MinimumWage,
                PaidFamilyLeaveWeeksOverride = bill.PaidFamilyLeaveWeeks,
                OvertimeRegulationOverride = bill.OvertimeRegulation,
                RetrainingProgramOverride = bill.RetrainingProgram,
                FamilyPolicyOverride = bill.FamilyPolicy,
                ImmigrationPolicyOverride = bill.ImmigrationPolicy
            };
            ApplyMinimumWageChange(country, decision);
            ApplyLaborPolicyChanges(country, decision);
            ApplyDemographicPolicyChanges(country, decision);
        }

        /// <summary>The pending standalone Crime &amp; Justice bill for this country, or null if none is currently before Parliament.</summary>
        public CrimeJusticePolicyBill GetPendingCrimeJusticeBill(CountryId countryId)
        {
            return _pendingCrimeJusticeBillByCountry.TryGetValue(countryId, out CrimeJusticePolicyBill bill) ? bill : null;
        }

        /// <summary>See IntroduceLaborBill's own doc comment - identical pattern.</summary>
        public bool IntroduceCrimeJusticeBill(CountryId countryId, CrimeJusticePolicyBill bill)
        {
            if (_pendingCrimeJusticeBillByCountry.ContainsKey(countryId))
            {
                return false;
            }

            bill.DaysRemaining = ParliamentSystem.BillDurationDays;
            _pendingCrimeJusticeBillByCountry[countryId] = bill;
            return true;
        }

        /// <summary>See AdvanceLaborBillDay's own doc comment - identical pattern.</summary>
        public void AdvanceCrimeJusticeBillDay(CountryId countryId)
        {
            if (!_pendingCrimeJusticeBillByCountry.TryGetValue(countryId, out CrimeJusticePolicyBill bill))
            {
                return;
            }

            bill.DaysRemaining--;
            if (bill.DaysRemaining > 0)
            {
                return;
            }

            Country country = _world.GetCountry(countryId);
            bool passed = ParliamentSystem.WouldBillPass(country, ParliamentSystem.GetCrimeJusticeBillDirection(country, bill));
            ParliamentSystem.ApplyCrimeJusticeBillResult(country, bill, passed, ApplyCrimeJusticeBillEffects);
            _pendingCrimeJusticeBillByCountry.Remove(countryId);
        }

        /// <summary>See ApplyLaborBillEffects' own doc comment - identical pattern, reuses ApplyCrimePolicyChanges/ApplyCrimeJusticeDeeperChanges.</summary>
        private void ApplyCrimeJusticeBillEffects(Country country, CrimeJusticePolicyBill bill)
        {
            var decision = new PolicyDecision
            {
                PoliceFundingOverride = bill.PoliceFunding,
                SentencingSeverityOverride = bill.SentencingSeverity,
                BailReformOverride = bill.BailReform,
                DrugPolicyOverride = bill.DrugPolicy,
                JudicialFundingOverride = bill.JudicialFunding,
                BorderEnforcementOverride = bill.BorderEnforcement
            };
            ApplyCrimePolicyChanges(country, decision);
            ApplyCrimeJusticeDeeperChanges(country, decision);
        }

        /// <summary>The pending standalone Economic Sectors bill for this country, or null if none is currently before Parliament.</summary>
        public SectorPolicyBill GetPendingSectorBill(CountryId countryId)
        {
            return _pendingSectorBillByCountry.TryGetValue(countryId, out SectorPolicyBill bill) ? bill : null;
        }

        /// <summary>See IntroduceLaborBill's own doc comment - identical pattern.</summary>
        public bool IntroduceSectorBill(CountryId countryId, SectorPolicyBill bill)
        {
            if (_pendingSectorBillByCountry.ContainsKey(countryId))
            {
                return false;
            }

            bill.DaysRemaining = ParliamentSystem.BillDurationDays;
            _pendingSectorBillByCountry[countryId] = bill;
            return true;
        }

        /// <summary>See AdvanceLaborBillDay's own doc comment - identical pattern.</summary>
        public void AdvanceSectorBillDay(CountryId countryId)
        {
            if (!_pendingSectorBillByCountry.TryGetValue(countryId, out SectorPolicyBill bill))
            {
                return;
            }

            bill.DaysRemaining--;
            if (bill.DaysRemaining > 0)
            {
                return;
            }

            Country country = _world.GetCountry(countryId);
            bool passed = ParliamentSystem.WouldBillPass(country, ParliamentSystem.GetSectorBillDirection(country, bill));
            ParliamentSystem.ApplySectorBillResult(country, bill, passed, ApplySectorBillEffects);
            _pendingSectorBillByCountry.Remove(countryId);
        }

        /// <summary>See ApplyLaborBillEffects' own doc comment - identical pattern, reuses ApplySectorPolicyChanges.</summary>
        private void ApplySectorBillEffects(Country country, SectorPolicyBill bill)
        {
            var decision = new PolicyDecision
            {
                SectorSubsidyOverrides = bill.SubsidyLevels,
                SectorRegulationOverrides = bill.RegulationLevels,
                SectorTaxCreditOverrides = bill.TaxCreditLevels,
                SectorResearchGrantsOverrides = bill.ResearchGrantsLevels,
                SectorDeregulationNationalizationOverrides = bill.DeregulationLevels
            };
            ApplySectorPolicyChanges(country, decision);
        }

        /// <summary>The pending standalone Trade bill for this country, or null if none is currently before Parliament.</summary>
        public TradePolicyBill GetPendingTradeBill(CountryId countryId)
        {
            return _pendingTradeBillByCountry.TryGetValue(countryId, out TradePolicyBill bill) ? bill : null;
        }

        /// <summary>See IntroduceLaborBill's own doc comment - identical pattern.</summary>
        public bool IntroduceTradeBill(CountryId countryId, TradePolicyBill bill)
        {
            if (_pendingTradeBillByCountry.ContainsKey(countryId))
            {
                return false;
            }

            bill.DaysRemaining = ParliamentSystem.BillDurationDays;
            _pendingTradeBillByCountry[countryId] = bill;
            return true;
        }

        /// <summary>See AdvanceLaborBillDay's own doc comment - identical pattern.</summary>
        public void AdvanceTradeBillDay(CountryId countryId)
        {
            if (!_pendingTradeBillByCountry.TryGetValue(countryId, out TradePolicyBill bill))
            {
                return;
            }

            bill.DaysRemaining--;
            if (bill.DaysRemaining > 0)
            {
                return;
            }

            Country country = _world.GetCountry(countryId);
            bool passed = ParliamentSystem.WouldBillPass(country, ParliamentSystem.GetTradeBillDirection(country, bill));
            ParliamentSystem.ApplyTradeBillResult(country, bill, passed, ApplyTradeBillEffects);
            _pendingTradeBillByCountry.Remove(countryId);
        }

        /// <summary>
        /// The Trade bill's apply delegate. NewBaseTariffRate is an ABSOLUTE target (see
        /// TradePolicyBill's own doc comment), but the existing ApplyTariffRateChange only understands
        /// a DELTA (PolicyDecision.TariffRateChange) - converted here as target-minus-current
        /// immediately before applying, so ApplyTariffRateChange's own clamp
        /// (Clamp(current + delta, min, max) = Clamp(target, min, max)) lands on exactly the bill's
        /// requested target without duplicating its clamp bounds here. PartnerTariffOverrides is
        /// already absolute (matches PolicyDecision.PartnerTariffOverrides' own semantics exactly), so
        /// it's passed through unchanged.
        /// </summary>
        private void ApplyTradeBillEffects(Country country, TradePolicyBill bill)
        {
            var decision = new PolicyDecision
            {
                TariffRateChange = bill.NewBaseTariffRate - country.BaseTariffRate,
                PartnerTariffOverrides = bill.PartnerTariffOverrides
            };
            ApplyTariffRateChange(country, decision);
            ApplyPartnerTariffOverrides(country, decision);
        }

        /// <summary>
        /// True on the one day per year that matches <paramref name="countryId"/>'s own
        /// FiscalYearData start date (month/day only, so this fires exactly once per year, the same
        /// "compare against a specific calendar date" idiom AdvanceDay's own turn-boundary check
        /// uses). Pure query - unlike AdvanceDay, has no side effects and isn't itself responsible for
        /// opening anything; see TryOpenBudgetProcess for the caller that acts on it.
        /// </summary>
        public bool IsFiscalYearStart(CountryId countryId, System.DateTime date)
        {
            (int month, int day) = FiscalYearData.GetFiscalYearStart(countryId);
            return date.Month == month && date.Day == day;
        }

        /// <summary>True while countryId's annual budget process is open and not yet resolved - see _pendingBudgetProcessByCountry's own doc comment and GameController's pause-gate/banner use of this.</summary>
        public bool GetPendingBudgetProcess(CountryId countryId)
        {
            return _pendingBudgetProcessByCountry.Contains(countryId);
        }

        /// <summary>
        /// Master Sequence step 5a: opens countryId's annual budget process on its own fiscal-year
        /// start date - a no-op if today isn't that date, or if one's already open (single-slot,
        /// mirroring TryRollForeignPolicyMeeting's own "safe to call every day unconditionally"
        /// idiom). Called once per simulated day from GameController.Update's day-processing loop, for
        /// PlayerCountryId only (see _pendingBudgetProcessByCountry's own doc comment on why the other
        /// five countries never enter this set at this phase).
        /// </summary>
        public void TryOpenBudgetProcess(CountryId countryId, System.DateTime date)
        {
            if (_pendingBudgetProcessByCountry.Contains(countryId))
            {
                return;
            }

            if (IsFiscalYearStart(countryId, date))
            {
                _pendingBudgetProcessByCountry.Add(countryId);
            }
        }

        /// <summary>Lets tools/tests (e.g. SimulationTestRunner) inject a specific World instead of the Awake-created default.</summary>
        public void SetWorld(World world)
        {
            _world = world;
            SeedPublishedHistory();
        }

        private void Awake()
        {
            if (_world == null)
            {
                _world = WorldFactory.CreateDefault();
            }

            SeedPublishedHistory();
        }

        /// <summary>Gives every country the one inherited published quarter it takes office with - see PublicationSystem.SeedInheritedHistory. Called from BOTH world-entry paths (injection and Awake) so batch runs and real play start from an identical published record; the seeding method is itself idempotent, so the overlap is harmless.</summary>
        private void SeedPublishedHistory()
        {
            if (_world == null)
            {
                return;
            }

            foreach (Country country in _world.Countries)
            {
                PublicationSystem.SeedInheritedHistory(country);
            }
        }

        /// <summary>
        /// Advances the simulation by one turn for every country. Countries with no entry in
        /// <paramref name="decisions"/> get a no-op policy decision for the turn.
        ///
        /// Order matters: interest rates and currency strength must resolve before trade (export
        /// competitiveness depends on this turn's currency strength), and trade must resolve before
        /// domestic policy (the national accounts identity needs this turn's TradeBalance as NX).
        /// </summary>
        public void AdvanceTurn(Dictionary<CountryId, PolicyDecision> decisions)
        {
            CurrencySystem.ApplyInterestRateChanges(_world, decisions);

            foreach (Country country in _world.Countries)
            {
                CurrencySystem.ApplyCurrencyStrength(country, _world);
            }

            foreach (Country country in _world.Countries)
            {
                PolicyDecision tariffDecision = decisions != null && decisions.TryGetValue(country.Id, out var td)
                    ? td
                    : PolicyDecision.None();

                ApplyTariffRateChange(country, tariffDecision);
                ApplyPartnerTariffOverrides(country, tariffDecision);
            }

            var tariffRevenueByCountry = new Dictionary<CountryId, float>();
            foreach (Country country in _world.Countries)
            {
                tariffRevenueByCountry[country.Id] = TradeSystem.ApplyTradeEffects(country, _world);
            }

            foreach (Country country in _world.Countries)
            {
                PolicyDecision decision = decisions != null && decisions.TryGetValue(country.Id, out var d)
                    ? d
                    : PolicyDecision.None();

                ApplyDomesticPolicy(country, decision, tariffRevenueByCountry[country.Id]);

                // Political Systems Overhaul Part B (Parliament), Master Sequence step 4: recomputed
                // for EVERY country every turn (not just the player's - see ParliamentSeats' own doc
                // comment), after ApplyDomesticPolicy so this turn's freshly-updated ApprovalRating is
                // what the seat-share formula actually reads, not last turn's stale value.
                ParliamentSystem.UpdateSeats(country);

                // Recorded here (once per real, committed turn) rather than inside
                // ApplyDomesticPolicy itself, so PreviewTurn's throwaway clone - which calls
                // ApplyDomesticPolicy's constituent steps directly, not this loop - never appends a
                // phantom data point into the real history.
                country.History.Append(CurrentDate, country.State, country.CurrencyZone.InterestRate);
            }

            CurrentTurn++;
        }

        /// <summary>
        /// Applies one country's domestic feedback rules for the turn, in place: fiscal policy,
        /// the national accounts identity (GDP), Okun's Law (unemployment), the Phillips Curve
        /// (inflation), and approval. <paramref name="tariffRevenue"/> was already collected (and
        /// already added to Budget) by TradeSystem earlier this same turn - it's threaded through
        /// only to record it on this turn's FiscalTurnReport, not applied again here.
        /// </summary>
        private void ApplyDomesticPolicy(Country country, PolicyDecision decision, float tariffRevenue)
        {
            EconomyState state = country.State;
            float interestRate = country.CurrencyZone.InterestRate;
            float gdpBeforeThisTurn = state.GDP;

            float totalTaxHike = ApplyTaxRateChanges(country, decision);
            ApplyWelfareGenerosityChanges(country, decision);
            ApplyMinimumWageChange(country, decision);
            ApplyCrimePolicyChanges(country, decision);
            ApplySectorPolicyChanges(country, decision);
            ApplySwfPolicyChanges(country, decision);
            ApplyLaborPolicyChanges(country, decision);
            ApplyCrimeJusticeDeeperChanges(country, decision);
            // Round 3 item 5, Part B: must run BEFORE ApplyDemographicRates, same reasoning as
            // ApplyCrimeJusticeDeeperChanges above.
            ApplyDemographicPolicyChanges(country, decision);
            // Round 3 item 5, Part A: must run BEFORE ResolveSpendingForTurn (pension/healthcare
            // pressure read this turn's freshly-updated DependencyRatio) and before
            // ApplyLaborForceParticipationRate below (reads DependencyRatio/NetMigrationRate).
            MacroSystem.ApplyDemographicRates(country);
            MacroSystem.ApplyPopulationGrowth(country);
            DetailedSpendingResult spendingResult = ResolveSpendingForTurn(country, decision);
            MacroSystem.ApplyCategorySpendingEffects(country, spendingResult.EffectiveDecision);
            MacroSystem.ApplyInfrastructureCondition(country, spendingResult.EffectiveDecision);
            MacroSystem.ApplySectorEffects(country);
            MacroSystem.ApplySectorGrowthEffect(country);
            MacroSystem.ApplyWelfareProgramEffects(country);

            float unemploymentBenefitCost = GetUnemploymentBenefitCost(country);
            float interestOnDebt = GetInterestOnDebt(country);
            float welfareCost = GetTotalWelfareCost(country);

            float swfContribution = GetSwfContribution(country);
            float swfReturns = 0f;
            if (country.SovereignWealthFund != null)
            {
                country.SovereignWealthFund.TotalAssets += swfContribution;
                swfReturns = SovereignWealthFundSystem.ApplyReturns(country.SovereignWealthFund);
                float maxSwfAssets = MaxSwfToGdpPercent / 100f * state.GDP;
                country.SovereignWealthFund.TotalAssets = Mathf.Clamp(country.SovereignWealthFund.TotalAssets, 0f, maxSwfAssets);
            }

            float revenue = ApplyRevenueAndSpending(country, spendingResult.GovernmentSpending, spendingResult.MandatorySpending, unemploymentBenefitCost, interestOnDebt, welfareCost, swfContribution, swfReturns, out float totalSpendingThisTurn, out float budgetBalanceThisTurn);

            _lastFiscalReports[country.Id] = new FiscalTurnReport
            {
                Revenue = revenue,
                BaselineGovernmentSpending = spendingResult.BaselineGovernmentSpending,
                DiscretionarySpending = spendingResult.DiscretionarySpendingChangeThisTurn,
                MandatorySpending = spendingResult.MandatorySpending,
                UnemploymentBenefitCost = unemploymentBenefitCost,
                InterestOnDebt = interestOnDebt,
                TariffRevenue = tariffRevenue,
                WelfareCost = welfareCost,
                SwfContribution = swfContribution,
                SwfReturns = swfReturns,
                TotalSpending = totalSpendingThisTurn,
                BudgetBalance = budgetBalanceThisTurn
            };

            MacroSystem.ApplyNationalAccounts(country, spendingResult.GovernmentSpending, interestRate);
            MacroSystem.ApplyPotentialGdpGrowth(country);

            float actualGrowthRate = (state.GDP - gdpBeforeThisTurn) / Mathf.Max(gdpBeforeThisTurn, 1f) * 100f;
            MacroSystem.ApplyOkunsLaw(country, actualGrowthRate);
            MacroSystem.ApplyPhillipsCurveInflation(country);
            MacroSystem.ApplyInflationExpectations(state);
            MacroSystem.ApplyPovertyRate(country);
            MacroSystem.ApplyLaborForceParticipationRate(country);
            // Round 3 item 3: must run BEFORE ApplyCrimeIndex, which reads this turn's freshly-updated
            // OrganizedCrimeIndex.
            MacroSystem.ApplyOrganizedCrimeIndex(country);
            MacroSystem.ApplyCorruptionIndex(country);
            MacroSystem.ApplyCrimeIndex(country);
            MacroSystem.ApplyCrimeEffects(country);
            MacroSystem.ApplyPrisonPopulationRate(country);

            MacroSystem.ApplyApprovalRating(country, spendingResult.EffectiveDecision, actualGrowthRate, totalTaxHike, spendingResult.MandatorySpendingChangeThisTurn);

            EconomicEvent economicEvent = EventSystem.TryRollEvent();
            _lastEventsByCountry[country.Id] = economicEvent;
            EventSystem.ApplyEvent(country, economicEvent);

            // Political Systems Overhaul Part A: unlike EconomicEvent, a fired decision needs a
            // player-picked response before its effect lands (see ResolveCabinetDecision) - appended,
            // not overwritten, though in practice this list is always empty going in, since
            // GameController blocks Advance Turn while any previous decision is still unresolved.
            if (!_pendingCabinetDecisionsByCountry.TryGetValue(country.Id, out var pendingDecisions))
            {
                pendingDecisions = new List<(CabinetPortfolio, CabinetDecision)>();
                _pendingCabinetDecisionsByCountry[country.Id] = pendingDecisions;
            }
            pendingDecisions.AddRange(CabinetSystem.TryRollDecisions(country));
        }

        /// <summary>
        /// Estimates what ApplyDomesticPolicy would do to <paramref name="countryId"/> this turn
        /// under <paramref name="decision"/>, WITHOUT mutating the real World/Country/EconomyState
        /// or recording a FiscalTurnReport - runs the same formulas (this class's own private fiscal
        /// helpers plus MacroSystem's national accounts identity, Okun's Law, Phillips Curve, and
        /// ApplyApprovalRating) against a throwaway clone of the country's EconomyState, so the
        /// result stays grounded in the actual model rather than a separate hand-rolled estimate.
        ///
        /// Two deliberate simplifications, both because they'd otherwise require mutating shared
        /// state (the country's CurrencyZone can be shared with other countries, e.g. the Eurozone)
        /// just to compute a display-only estimate: the previewed interest rate is threaded through
        /// as a local value into ApplyNationalAccounts rather than actually changing the
        /// CurrencyZone, so GetInterestOnDebt's rate still reflects the current (not previewed) rate;
        /// and this turn's CurrencyStrength is used as-is rather than re-deriving its (heavily
        /// damped, slow-moving) drift for the preview's trade-balance estimate.
        ///
        /// Never rolls an EventSystem event and never advances CurrentTurn - a preview should be
        /// deterministic and side-effect-free, not spend part of the "will an event fire" randomness
        /// budget on a turn the player might not even commit to.
        /// </summary>
        public PolicyPreview PreviewTurn(CountryId countryId, PolicyDecision decision)
        {
            Country previewCountry = ClonePreviewCountry(_world.GetCountry(countryId));
            EconomyState state = previewCountry.State;

            float gdpBeforeThisTurn = state.GDP;
            float unemploymentBefore = state.Unemployment;
            float inflationBefore = state.Inflation;
            float approvalBefore = state.ApprovalRating;
            float budgetBefore = state.Budget;
            float povertyBefore = state.PovertyRate;
            float laborForceParticipationBefore = state.LaborForceParticipationRate;
            float crimeIndexBefore = state.CrimeIndex;

            ApplyTariffRateChange(previewCountry, decision);
            ApplyPartnerTariffOverrides(previewCountry, decision);
            TradeSystem.ApplyTradeEffects(previewCountry, _world);

            float totalTaxHike = ApplyTaxRateChanges(previewCountry, decision);
            ApplyWelfareGenerosityChanges(previewCountry, decision);
            ApplyMinimumWageChange(previewCountry, decision);
            ApplyCrimePolicyChanges(previewCountry, decision);
            ApplySectorPolicyChanges(previewCountry, decision);
            ApplySwfPolicyChanges(previewCountry, decision);
            ApplyLaborPolicyChanges(previewCountry, decision);
            ApplyCrimeJusticeDeeperChanges(previewCountry, decision);
            ApplyDemographicPolicyChanges(previewCountry, decision);
            MacroSystem.ApplyDemographicRates(previewCountry);
            MacroSystem.ApplyPopulationGrowth(previewCountry);
            DetailedSpendingResult spendingResult = ResolveSpendingForTurn(previewCountry, decision);
            MacroSystem.ApplyCategorySpendingEffects(previewCountry, spendingResult.EffectiveDecision);
            MacroSystem.ApplyInfrastructureCondition(previewCountry, spendingResult.EffectiveDecision);
            MacroSystem.ApplySectorEffects(previewCountry);
            MacroSystem.ApplySectorGrowthEffect(previewCountry);
            MacroSystem.ApplyWelfareProgramEffects(previewCountry);

            float unemploymentBenefitCost = GetUnemploymentBenefitCost(previewCountry);
            float interestOnDebt = GetInterestOnDebt(previewCountry);
            float welfareCost = GetTotalWelfareCost(previewCountry);

            // Preview uses the deterministic AVERAGE return, never an actual random draw - PreviewTurn
            // is documented to be side-effect-free/deterministic (it never rolls an EventSystem event
            // either), and rolling SovereignWealthFundSystem's real isolated RNG here would consume part
            // of its sequence for a turn the player might not even commit to.
            float swfContribution = GetSwfContribution(previewCountry);
            float swfReturns = 0f;
            if (previewCountry.SovereignWealthFund != null)
            {
                previewCountry.SovereignWealthFund.TotalAssets += swfContribution;
                swfReturns = SovereignWealthFundSystem.GetAverageReturnEstimate(previewCountry.SovereignWealthFund);
                float maxSwfAssets = MaxSwfToGdpPercent / 100f * state.GDP;
                previewCountry.SovereignWealthFund.TotalAssets = Mathf.Clamp(previewCountry.SovereignWealthFund.TotalAssets, 0f, maxSwfAssets);
            }

            ApplyRevenueAndSpending(previewCountry, spendingResult.GovernmentSpending, spendingResult.MandatorySpending, unemploymentBenefitCost, interestOnDebt, welfareCost, swfContribution, swfReturns, out _, out _);

            float previewedInterestRate;
            if (previewCountry.CurrentFedChair != null)
            {
                previewedInterestRate = Mathf.Clamp(
                    TaylorRule.GetSuggestedInterestRate(previewCountry) + previewCountry.CurrentFedChair.RateBias,
                    CurrencySystem.MinInterestRate, CurrencySystem.MaxInterestRate);
            }
            else if (CurrencySystem.SharesCurrencyZoneWithOthers(previewCountry, _world))
            {
                float blended = EurozoneRateSystem.GetBlendedSuggestedRate(_world, previewCountry);
                float push = Mathf.Clamp(decision.InterestRateChange, -EurozoneRateSystem.MemberRatePushRange, EurozoneRateSystem.MemberRatePushRange);
                previewedInterestRate = Mathf.Clamp(blended + push, CurrencySystem.MinInterestRate, CurrencySystem.MaxInterestRate);
            }
            else
            {
                previewedInterestRate = Mathf.Clamp(
                    previewCountry.CurrencyZone.InterestRate + decision.InterestRateChange,
                    CurrencySystem.MinInterestRate, CurrencySystem.MaxInterestRate);
            }
            MacroSystem.ApplyNationalAccounts(previewCountry, spendingResult.GovernmentSpending, previewedInterestRate);
            MacroSystem.ApplyPotentialGdpGrowth(previewCountry);

            float actualGrowthRate = (state.GDP - gdpBeforeThisTurn) / Mathf.Max(gdpBeforeThisTurn, 1f) * 100f;
            MacroSystem.ApplyOkunsLaw(previewCountry, actualGrowthRate);
            MacroSystem.ApplyPhillipsCurveInflation(previewCountry);
            MacroSystem.ApplyInflationExpectations(state);
            MacroSystem.ApplyPovertyRate(previewCountry);
            MacroSystem.ApplyLaborForceParticipationRate(previewCountry);
            MacroSystem.ApplyOrganizedCrimeIndex(previewCountry);
            MacroSystem.ApplyCorruptionIndex(previewCountry);
            MacroSystem.ApplyCrimeIndex(previewCountry);
            MacroSystem.ApplyCrimeEffects(previewCountry);
            MacroSystem.ApplyPrisonPopulationRate(previewCountry);

            MacroSystem.ApplyApprovalRating(previewCountry, spendingResult.EffectiveDecision, actualGrowthRate, totalTaxHike, spendingResult.MandatorySpendingChangeThisTurn);

            return new PolicyPreview
            {
                GdpGrowthPercent = actualGrowthRate,
                UnemploymentChange = state.Unemployment - unemploymentBefore,
                InflationChange = state.Inflation - inflationBefore,
                ApprovalChange = state.ApprovalRating - approvalBefore,
                NetBudgetImpact = state.Budget - budgetBefore,
                PovertyRateChange = state.PovertyRate - povertyBefore,
                LaborForceParticipationRateChange = state.LaborForceParticipationRate - laborForceParticipationBefore,
                CrimeIndexChange = state.CrimeIndex - crimeIndexBefore,
                SwfContributionEstimate = swfContribution,
                SwfReturnsEstimate = swfReturns
            };
        }

        /// <summary>
        /// A throwaway Country for PreviewTurn: its own EconomyState clone (so GDP/Inflation/etc.
        /// mutations never touch the real one), its own copy of the structural fields that
        /// MacroSystem.ApplyCategorySpendingEffects/ApplyTariffRateChange mutate (PotentialGrowthRate,
        /// BaseTariffRate), and its own deep-cloned TaxLines (ApplyTaxRateChanges mutates TaxLine.Rate,
        /// so these can't be shared references the way the CurrencyZone reference is) - but the SAME
        /// CurrencyZone reference (read-only here - see PreviewTurn's remarks on why its InterestRate
        /// is never written). CollectionEfficiency, BaseDebtInterestRateOverride,
        /// RiskPremiumSensitivity, ComfortableDebtToGdpPercent, and BaselinePovertyRate are all copied
        /// explicitly since none is a constructor parameter (each defaults the same as on a real
        /// Country) - without this the preview would overstate revenue for every country whose real
        /// CollectionEfficiency is below 1, overstate InterestOnDebt for a reserve-currency issuer (the
        /// USA) whose real risk-premium sensitivity is near zero, misjudge GetFiscalReactionMultiplier
        /// for every country whose real comfort anchor isn't the 60f default, and misjudge
        /// ApplyPovertyRate's baseline for every country whose real baseline isn't the 10f default.
        /// BaselineLaborForceParticipationRate, MinimumWageImplemented, MinimumWagePercentOfMedian, and
        /// BaselineMinimumWagePercentOfMedian are copied for the same reason - none is a constructor
        /// parameter, and ApplyMinimumWageChange mutates MinimumWagePercentOfMedian directly.
        /// SovereignWealthFund is deep-cloned (via SovereignWealthFund.Clone(), null-safe) for the
        /// same reason TaxLines/SpendingLines/WelfarePrograms are - ApplySwfPolicyChanges and the
        /// contribution/returns steps in PreviewTurn both mutate it, so it can't be a shared reference
        /// the way CurrentFedChair is.
        /// SpendingLines is deep-cloned for the same reason TaxLines is -
        /// ApplySpendingLineChanges mutates SpendingLine.Amount, so these can't be shared references
        /// either. WelfarePrograms is ALSO deep-cloned (via WelfareProgram.Clone()) for the same reason -
        /// ApplyWelfareGenerosityChanges mutates WelfareProgram.GenerosityLevel. TradePartners is ALSO
        /// deep-cloned (via TradePartner.Clone()) for the same reason -
        /// ApplyPartnerTariffOverrides mutates TradePartner.PlayerTariffOverride, so a shared reference
        /// would leak the preview's draft override into the real World the moment PreviewTurn ran, not
        /// just when the player commits it. CurrentFedChair is a shared (not cloned) reference -
        /// nothing in PreviewTurn or ApplyDomesticPolicy ever mutates a FedChair's own fields, only
        /// reads RateBias, so this is safe unlike TaxLines/SpendingLines/TradePartners/WelfarePrograms.
        /// </summary>
        private static Country ClonePreviewCountry(Country country)
        {
            return new Country(
                country.Id, country.Name, country.State.Clone(), country.CurrencyZone, country.BaseTariffRate,
                country.NaturalUnemploymentRate, country.PotentialGrowthRate, country.GovernmentSpendingRate,
                country.BenefitRatePerUnemployed)
            {
                TradePartners = ClonePreviewTradePartners(country.TradePartners),
                TaxLines = ClonePreviewTaxLines(country.TaxLines),
                SpendingLines = ClonePreviewSpendingLines(country.SpendingLines),
                WelfarePrograms = ClonePreviewWelfarePrograms(country.WelfarePrograms),
                Sectors = ClonePreviewSectors(country.Sectors),
                InfrastructureAssets = ClonePreviewInfrastructureAssets(country.InfrastructureAssets),
                CollectionEfficiency = country.CollectionEfficiency,
                BaseDebtInterestRateOverride = country.BaseDebtInterestRateOverride,
                RiskPremiumSensitivity = country.RiskPremiumSensitivity,
                ComfortableDebtToGdpPercent = country.ComfortableDebtToGdpPercent,
                BaselinePovertyRate = country.BaselinePovertyRate,
                BaselineLaborForceParticipationRate = country.BaselineLaborForceParticipationRate,
                MinimumWageImplemented = country.MinimumWageImplemented,
                MinimumWagePercentOfMedian = country.MinimumWagePercentOfMedian,
                BaselineMinimumWagePercentOfMedian = country.BaselineMinimumWagePercentOfMedian,
                BaselineCrimeIndex = country.BaselineCrimeIndex,
                PoliceFundingLevel = country.PoliceFundingLevel,
                SentencingSeverity = country.SentencingSeverity,
                CurrentFedChair = country.CurrentFedChair,
                SovereignWealthFund = country.SovereignWealthFund?.Clone(),
                PaidFamilyLeaveWeeks = country.PaidFamilyLeaveWeeks,
                BaselinePaidFamilyLeaveWeeks = country.BaselinePaidFamilyLeaveWeeks,
                OvertimeRegulationLevel = country.OvertimeRegulationLevel,
                RetrainingProgramLevel = country.RetrainingProgramLevel,
                BaselinePrisonPopulationRate = country.BaselinePrisonPopulationRate,
                BailReformLevel = country.BailReformLevel,
                DrugPolicyLevel = country.DrugPolicyLevel,
                BaselineOrganizedCrimeIndex = country.BaselineOrganizedCrimeIndex,
                BaselineCorruptionIndex = country.BaselineCorruptionIndex,
                JudicialFundingLevel = country.JudicialFundingLevel,
                BorderEnforcementLevel = country.BorderEnforcementLevel,
                BaselineDependencyRatio = country.BaselineDependencyRatio,
                BaselineNetMigrationRate = country.BaselineNetMigrationRate,
                SteadyStateGrowthRate = country.SteadyStateGrowthRate,
                FamilyPolicyLevel = country.FamilyPolicyLevel,
                ImmigrationPolicyLevel = country.ImmigrationPolicyLevel,
                BasePotentialGrowthRate = country.BasePotentialGrowthRate,
                InfrastructureSpendingGrowthAdjustment = country.InfrastructureSpendingGrowthAdjustment
            };
        }

        private static List<TradePartner> ClonePreviewTradePartners(List<TradePartner> tradePartners)
        {
            var clones = new List<TradePartner>(tradePartners.Count);
            foreach (TradePartner tradePartner in tradePartners)
            {
                clones.Add(tradePartner.Clone());
            }
            return clones;
        }

        private static List<TaxLine> ClonePreviewTaxLines(List<TaxLine> taxLines)
        {
            var clones = new List<TaxLine>(taxLines.Count);
            foreach (TaxLine taxLine in taxLines)
            {
                clones.Add(taxLine.Clone());
            }
            return clones;
        }

        private static List<InfrastructureAsset> ClonePreviewInfrastructureAssets(List<InfrastructureAsset> infrastructureAssets)
        {
            var clones = new List<InfrastructureAsset>(infrastructureAssets.Count);
            foreach (InfrastructureAsset asset in infrastructureAssets)
            {
                clones.Add(asset.Clone());
            }
            return clones;
        }

        private static List<Sector> ClonePreviewSectors(List<Sector> sectors)
        {
            var clones = new List<Sector>(sectors.Count);
            foreach (Sector sector in sectors)
            {
                clones.Add(sector.Clone());
            }
            return clones;
        }

        private static List<SpendingLine> ClonePreviewSpendingLines(List<SpendingLine> spendingLines)
        {
            var clones = new List<SpendingLine>(spendingLines.Count);
            foreach (SpendingLine spendingLine in spendingLines)
            {
                clones.Add(spendingLine.Clone());
            }
            return clones;
        }

        private static List<WelfareProgram> ClonePreviewWelfarePrograms(List<WelfareProgram> welfarePrograms)
        {
            var clones = new List<WelfareProgram>(welfarePrograms.Count);
            foreach (WelfareProgram welfareProgram in welfarePrograms)
            {
                clones.Add(welfareProgram.Clone());
            }
            return clones;
        }

        /// <summary>Direct tariff-policy control: the country's own BaseTariffRate moves by the requested change, clamped to a sane range.</summary>
        private void ApplyTariffRateChange(Country country, PolicyDecision decision)
        {
            country.BaseTariffRate = Mathf.Clamp(country.BaseTariffRate + decision.TariffRateChange, MinBaseTariffRate, MaxBaseTariffRate);
        }

        /// <summary>
        /// Sets a per-partner tariff override directly (an absolute target, not a delta - same
        /// semantics as ApplyTaxRateChanges) for every partner with an entry in
        /// PolicyDecision.PartnerTariffOverrides, clamped to the same [MinBaseTariffRate,
        /// MaxBaseTariffRate] range BaseTariffRate itself uses. A no-op for any partner with no entry
        /// this turn - once set, TradePartner.PlayerTariffOverride persists turn to turn like
        /// TaxLine.Rate does, until the player either changes it again or resets it back to "no
        /// override" (a separate, immediate action - see TradePartner.PlayerTariffOverride's doc
        /// comment). Runs before TradeSystem.ApplyTradeEffects each turn (same ordering as
        /// ApplyTariffRateChange) so this turn's override is what this turn's trade actually resolves
        /// against.
        /// </summary>
        private void ApplyPartnerTariffOverrides(Country country, PolicyDecision decision)
        {
            foreach (TradePartner tradePartner in country.TradePartners)
            {
                if (decision.PartnerTariffOverrides.TryGetValue(tradePartner.PartnerId, out float requestedRate))
                {
                    tradePartner.PlayerTariffOverride = Mathf.Clamp(requestedRate, MinBaseTariffRate, MaxBaseTariffRate);
                }
            }
        }

        /// <summary>
        /// Sets every currently-implemented TaxLine's Rate directly to this turn's requested
        /// PolicyDecision.TaxRateOverrides value (clamped to that TaxLine's own TaxTypeRateRanges), a
        /// no-op for any TaxType with no entry or that isn't implemented - implementing/removing a
        /// tax is a separate, immediate action on TaxLine.IsImplemented, not something this method
        /// does. Returns the sum of every positive rate increase actually applied (clamped target
        /// minus the prior rate, where positive), computed here - before TaxLine.Rate is overwritten -
        /// since PolicyDecision only carries the absolute target, not a delta; the caller threads this
        /// into MacroSystem.ApplyApprovalRating's tax-hike penalty.
        /// </summary>
        private float ApplyTaxRateChanges(Country country, PolicyDecision decision)
        {
            float totalTaxHike = 0f;
            foreach (TaxLine taxLine in country.TaxLines)
            {
                if (!taxLine.IsImplemented)
                {
                    continue;
                }

                if (!decision.TaxRateOverrides.TryGetValue(taxLine.Type, out float requestedRate))
                {
                    continue;
                }

                float clampedRate = Mathf.Clamp(requestedRate, taxLine.MinRate, taxLine.MaxRate);
                float hike = clampedRate - taxLine.Rate;
                if (hike > 0f)
                {
                    totalTaxHike += hike;
                }

                taxLine.Rate = clampedRate;
            }

            return totalTaxHike;
        }

        /// <summary>
        /// Sets every currently-implemented WelfareProgram's GenerosityLevel directly to this turn's
        /// requested PolicyDecision.WelfareGenerosityOverrides value (clamped to [MinGenerosityLevel,
        /// MaxGenerosityLevel]), a no-op for any WelfareProgramType with no entry or that isn't
        /// implemented - implementing/removing a welfare program is a separate, immediate action on
        /// WelfareProgram.IsImplemented, not something this method does. Mirrors ApplyTaxRateChanges
        /// exactly, minus the hike-tracking return value - welfare's approval effect is an ongoing
        /// STOCK effect of the current GenerosityLevel (see MacroSystem.GetWelfareApprovalEffect), not
        /// a one-time "this-turn change" penalty/bonus like the tax-hike term, so there's nothing
        /// equivalent to thread through here.
        /// </summary>
        private void ApplyWelfareGenerosityChanges(Country country, PolicyDecision decision)
        {
            foreach (WelfareProgram program in country.WelfarePrograms)
            {
                if (!program.IsImplemented)
                {
                    continue;
                }

                if (!decision.WelfareGenerosityOverrides.TryGetValue(program.Type, out float requestedGenerosity))
                {
                    continue;
                }

                program.GenerosityLevel = Mathf.Clamp(requestedGenerosity, MinGenerosityLevel, MaxGenerosityLevel);
            }
        }

        /// <summary>
        /// Sets Country.MinimumWagePercentOfMedian directly to this turn's requested
        /// PolicyDecision.MinimumWageOverride value (clamped to [MinMinimumWagePercent,
        /// MaxMinimumWagePercent]) - a no-op if the country has no statutory minimum wage
        /// (Country.MinimumWageImplemented false, e.g. Sweden/Italy) or no request was made this
        /// turn (the -1 sentinel). Mirrors ApplyTaxRateChanges/ApplyWelfareGenerosityChanges' "SET,
        /// not delta" pattern - there is no implement/remove action, since whether a country has a
        /// statutory minimum wage at all is a structural fact, not a player choice.
        /// </summary>
        private void ApplyMinimumWageChange(Country country, PolicyDecision decision)
        {
            if (!country.MinimumWageImplemented || decision.MinimumWageOverride < 0f)
            {
                return;
            }

            country.MinimumWagePercentOfMedian = Mathf.Clamp(decision.MinimumWageOverride, MinMinimumWagePercent, MaxMinimumWagePercent);
        }

        /// <summary>
        /// Sets Country.PoliceFundingLevel/SentencingSeverity directly to this turn's requested
        /// PolicyDecision overrides (each clamped to [MinPolicyDialLevel, MaxPolicyDialLevel]) - a
        /// no-op for either dial with no request this turn (the -1 sentinel). Every country has both
        /// dials (unlike minimum wage's country-specific asymmetry) - police funding and sentencing
        /// policy are universal government functions.
        /// </summary>
        private void ApplyCrimePolicyChanges(Country country, PolicyDecision decision)
        {
            if (decision.PoliceFundingOverride >= 0f)
            {
                country.PoliceFundingLevel = Mathf.Clamp(decision.PoliceFundingOverride, MinPolicyDialLevel, MaxPolicyDialLevel);
            }

            if (decision.SentencingSeverityOverride >= 0f)
            {
                country.SentencingSeverity = Mathf.Clamp(decision.SentencingSeverityOverride, MinPolicyDialLevel, MaxPolicyDialLevel);
            }
        }

        /// <summary>
        /// Sets each Sector's SubsidyLevel/RegulationLevel/TaxCreditLevel/ResearchGrantsLevel/
        /// DeregulationNationalizationLevel directly to this turn's requested PolicyDecision overrides
        /// (clamped to [MinSectorDialLevel, MaxSectorDialLevel]) - a no-op for any SectorType with no
        /// entry in the corresponding dictionary, the same "only requested entries matter" pattern
        /// TaxRateOverrides/WelfareGenerosityOverrides already use.
        /// </summary>
        private void ApplySectorPolicyChanges(Country country, PolicyDecision decision)
        {
            foreach (Sector sector in country.Sectors)
            {
                if (decision.SectorSubsidyOverrides.TryGetValue(sector.Type, out float requestedSubsidy))
                {
                    sector.SubsidyLevel = Mathf.Clamp(requestedSubsidy, MinSectorDialLevel, MaxSectorDialLevel);
                }

                if (decision.SectorRegulationOverrides.TryGetValue(sector.Type, out float requestedRegulation))
                {
                    sector.RegulationLevel = Mathf.Clamp(requestedRegulation, MinSectorDialLevel, MaxSectorDialLevel);
                }

                if (decision.SectorTaxCreditOverrides.TryGetValue(sector.Type, out float requestedTaxCredit))
                {
                    sector.TaxCreditLevel = Mathf.Clamp(requestedTaxCredit, MinSectorDialLevel, MaxSectorDialLevel);
                }

                if (decision.SectorResearchGrantsOverrides.TryGetValue(sector.Type, out float requestedResearchGrants))
                {
                    sector.ResearchGrantsLevel = Mathf.Clamp(requestedResearchGrants, MinSectorDialLevel, MaxSectorDialLevel);
                }

                if (decision.SectorDeregulationNationalizationOverrides.TryGetValue(sector.Type, out float requestedDeregulation))
                {
                    sector.DeregulationNationalizationLevel = Mathf.Clamp(requestedDeregulation, MinSectorDialLevel, MaxSectorDialLevel);
                }
            }
        }

        /// <summary>
        /// Sets SovereignWealthFund's contribution rate/allocation/asset weights directly to this
        /// turn's requested PolicyDecision overrides (each clamped to its own range) - a no-op entirely
        /// if the country has no fund (Country.SovereignWealthFund null) or for any individual field
        /// with no request this turn (the -1 sentinel - SwfContributionRateOverride uses
        /// float.MinValue instead, see PolicyDecision's own remarks on that field).
        /// </summary>
        private void ApplySwfPolicyChanges(Country country, PolicyDecision decision)
        {
            SovereignWealthFund fund = country.SovereignWealthFund;
            if (fund == null)
            {
                return;
            }

            if (decision.SwfContributionRateOverride > float.MinValue)
            {
                fund.ContributionRatePercent = Mathf.Clamp(decision.SwfContributionRateOverride, MinSwfContributionRate, MaxSwfContributionRate);
            }

            if (decision.SwfDomesticAllocationOverride >= 0f)
            {
                fund.DomesticAllocationPercent = Mathf.Clamp(decision.SwfDomesticAllocationOverride, MinSwfDialLevel, MaxSwfDialLevel);
            }

            if (decision.SwfEquitiesWeightOverride >= 0f)
            {
                fund.EquitiesWeight = Mathf.Clamp(decision.SwfEquitiesWeightOverride, MinSwfDialLevel, MaxSwfDialLevel);
            }

            if (decision.SwfBondsWeightOverride >= 0f)
            {
                fund.BondsWeight = Mathf.Clamp(decision.SwfBondsWeightOverride, MinSwfDialLevel, MaxSwfDialLevel);
            }

            if (decision.SwfInfrastructureWeightOverride >= 0f)
            {
                fund.InfrastructureWeight = Mathf.Clamp(decision.SwfInfrastructureWeightOverride, MinSwfDialLevel, MaxSwfDialLevel);
            }

            if (decision.SwfRealEstateWeightOverride >= 0f)
            {
                fund.RealEstateWeight = Mathf.Clamp(decision.SwfRealEstateWeightOverride, MinSwfDialLevel, MaxSwfDialLevel);
            }
        }

        /// <summary>
        /// This turn's sovereign-wealth-fund contribution - GDP * ContributionRatePercent/100. 0 for
        /// a country with no fund. A NEW BUDGET EXPENSE when ContributionRatePercent is positive (the
        /// original mechanic); when the player has set it negative (a drawdown - see
        /// MinSwfContributionRate), this same figure is negative, which ApplyRevenueAndSpending's
        /// plain sum already treats as a revenue offset rather than an expense - no separate
        /// withdrawal code path was needed. Whichever sign it has, TotalAssets is adjusted by exactly
        /// this amount (see AdvanceTurn/PreviewTurn), so a drawdown correctly shrinks the fund by the
        /// withdrawn amount, clamped at 0 (see MaxSwfToGdpPercent's own clamp) - the fund can't be
        /// drawn down past empty.
        /// </summary>
        private float GetSwfContribution(Country country)
        {
            SovereignWealthFund fund = country.SovereignWealthFund;
            return fund == null ? 0f : country.State.GDP * (fund.ContributionRatePercent / 100f);
        }

        /// <summary>
        /// Sets Country.PaidFamilyLeaveWeeks/OvertimeRegulationLevel/RetrainingProgramLevel directly
        /// to this turn's requested PolicyDecision overrides (each clamped to its own range) - a
        /// no-op for any individual field with no request this turn (the -1 sentinel). Every country
        /// has all three (unlike MinimumWage's country-specific asymmetry) - these are universal
        /// government policy levers, even for a country with 0 weeks of paid leave today.
        /// </summary>
        private void ApplyLaborPolicyChanges(Country country, PolicyDecision decision)
        {
            if (decision.PaidFamilyLeaveWeeksOverride >= 0f)
            {
                country.PaidFamilyLeaveWeeks = Mathf.Clamp(decision.PaidFamilyLeaveWeeksOverride, MinPaidFamilyLeaveWeeks, MaxPaidFamilyLeaveWeeks);
            }

            if (decision.OvertimeRegulationOverride >= 0f)
            {
                country.OvertimeRegulationLevel = Mathf.Clamp(decision.OvertimeRegulationOverride, MinLaborDialLevel, MaxLaborDialLevel);
            }

            if (decision.RetrainingProgramOverride >= 0f)
            {
                country.RetrainingProgramLevel = Mathf.Clamp(decision.RetrainingProgramOverride, MinLaborDialLevel, MaxLaborDialLevel);
            }
        }

        /// <summary>
        /// Sets Country.BailReformLevel/DrugPolicyLevel/JudicialFundingLevel/BorderEnforcementLevel
        /// directly to this turn's requested PolicyDecision overrides (each clamped to
        /// [MinPolicyDialLevel, MaxPolicyDialLevel], the same range Crime &amp; Justice Basics' own
        /// dials use) - a no-op for any with no request this turn (the -1 sentinel). The last two were
        /// added in Round 3 item 3.
        /// </summary>
        private void ApplyCrimeJusticeDeeperChanges(Country country, PolicyDecision decision)
        {
            if (decision.BailReformOverride >= 0f)
            {
                country.BailReformLevel = Mathf.Clamp(decision.BailReformOverride, MinPolicyDialLevel, MaxPolicyDialLevel);
            }

            if (decision.DrugPolicyOverride >= 0f)
            {
                country.DrugPolicyLevel = Mathf.Clamp(decision.DrugPolicyOverride, MinPolicyDialLevel, MaxPolicyDialLevel);
            }

            if (decision.JudicialFundingOverride >= 0f)
            {
                country.JudicialFundingLevel = Mathf.Clamp(decision.JudicialFundingOverride, MinPolicyDialLevel, MaxPolicyDialLevel);
            }

            if (decision.BorderEnforcementOverride >= 0f)
            {
                country.BorderEnforcementLevel = Mathf.Clamp(decision.BorderEnforcementOverride, MinPolicyDialLevel, MaxPolicyDialLevel);
            }
        }

        /// <summary>
        /// Round 3 item 5, Part B: sets Country.FamilyPolicyLevel/ImmigrationPolicyLevel directly to
        /// this turn's requested PolicyDecision overrides (each clamped to [MinPolicyDialLevel,
        /// MaxPolicyDialLevel]) - a no-op for either with no request this turn (the -1 sentinel), the
        /// same pattern ApplyCrimeJusticeDeeperChanges already uses. Must run BEFORE
        /// MacroSystem.ApplyDemographicRates, which reads these same-turn freshly-set levels - the
        /// same "avoid a one-turn lag" timing requirement every other policy-apply method before it
        /// already follows.
        /// </summary>
        private void ApplyDemographicPolicyChanges(Country country, PolicyDecision decision)
        {
            if (decision.FamilyPolicyOverride >= 0f)
            {
                country.FamilyPolicyLevel = Mathf.Clamp(decision.FamilyPolicyOverride, MinPolicyDialLevel, MaxPolicyDialLevel);
            }

            if (decision.ImmigrationPolicyOverride >= 0f)
            {
                country.ImmigrationPolicyLevel = Mathf.Clamp(decision.ImmigrationPolicyOverride, MinPolicyDialLevel, MaxPolicyDialLevel);
            }
        }

        /// <summary>
        /// This turn's total welfare cost: the sum, over every implemented WelfareProgram, of
        /// GDP * (CostShareOfGdp / 100) * (GenerosityLevel / 100) - a new spending category alongside
        /// Mandatory/Discretionary/UnemploymentBenefitCost/InterestOnDebt (see ApplyRevenueAndSpending),
        /// deliberately NOT touching IncomeSecurity or any other existing SpendingLine. Treated as a
        /// transfer (excluded from MacroSystem's national accounts G term), the same reasoning already
        /// applied to Mandatory SpendingLines/UnemploymentBenefitCost/InterestOnDebt - welfare programs
        /// (UBI, means-tested transfers, healthcare/housing/childcare subsidies) are payments to
        /// individuals, not government purchases of goods and services.
        /// </summary>
        private float GetTotalWelfareCost(Country country)
        {
            float cost = 0f;
            float gdp = country.State.GDP;
            foreach (WelfareProgram program in country.WelfarePrograms)
            {
                if (!program.IsImplemented)
                {
                    continue;
                }

                cost += gdp * (program.CostShareOfGdp / 100f) * (program.GenerosityLevel / 100f);
            }

            return cost;
        }

        /// <summary>
        /// This turn's total THEORETICAL tax revenue (before CollectionEfficiency): the sum, over
        /// every implemented TaxLine, of GDP * (Rate / 100) * BaseShareOfGdp. Tariffs is explicitly
        /// skipped even though it's never constructed as a TaxLine (see TaxType's doc comment) -
        /// defensive, so a future TaxLine accidentally created for it could never double-count revenue
        /// TradeSystem already collects. See ApplyRevenueAndSpending for where CollectionEfficiency is
        /// applied to get the actual collected revenue.
        /// </summary>
        private float GetTotalTaxRevenue(Country country)
        {
            float revenue = 0f;
            float gdp = country.State.GDP;
            foreach (TaxLine taxLine in country.TaxLines)
            {
                if (!taxLine.IsImplemented || taxLine.Type == TaxType.Tariffs)
                {
                    continue;
                }

                revenue += gdp * (taxLine.Rate / 100f) * taxLine.BaseShareOfGdp;
            }

            return revenue;
        }

        /// <summary>
        /// This turn's baseline government consumption expenditure - the country's structural share
        /// of GDP, before the player's discretionary category spending is added on top by the
        /// caller. Split out (rather than returning the combined total) so callers can report
        /// baseline and discretionary spending as separate line items. Only used for a country
        /// WITHOUT a detailed SpendingLines portfolio - see ResolveSpendingForTurn.
        /// </summary>
        private float GetBaselineGovernmentSpending(Country country)
        {
            return country.State.GDP * (country.GovernmentSpendingRate / 100f);
        }

        /// <summary>Bundles what ResolveSpendingForTurn resolves this turn's spending down to, for either mechanism (detailed SpendingLines or the legacy baseline+category-delta one).</summary>
        private class DetailedSpendingResult
        {
            public float BaselineGovernmentSpending;
            public float GovernmentSpending;
            public float DiscretionarySpendingChangeThisTurn;
            public float MandatorySpending;
            public float MandatorySpendingChangeThisTurn;
            public PolicyDecision EffectiveDecision;
        }

        /// <summary>Per-category actual dollar change observed this turn (post-clamp against MinSpendingLineAmountRatio/MaxSpendingLineAmountRatio) from ApplySpendingLineChanges, plus the total across Mandatory lines.</summary>
        private class SpendingLineChangeResult
        {
            public readonly Dictionary<SpendingCategory, float> ActualDollarChangeByCategory = new Dictionary<SpendingCategory, float>();
            public float MandatoryDollarChangeTotal;
        }

        /// <summary>
        /// For a country with a detailed SpendingLines portfolio (Phase 1: USA only): applies this
        /// turn's SpendingLineChanges to the Discretionary lines (ApplySpendingLineChanges), then G
        /// is the sum of Discretionary line Amounts AFTER that change (Mandatory lines are transfers,
        /// excluded from G - same reasoning as UnemploymentBenefitCost/InterestOnDebt) and
        /// MandatorySpending is reported separately for ApplyRevenueAndSpending to add to total
        /// budget outflow. BaselineGovernmentSpending/DiscretionarySpendingChangeThisTurn are split
        /// as before-this-turn's-total / actual-net-change-observed (not the raw requested delta sum,
        /// since ApplySpendingLineChanges' seed-ratio clamp can clip an individual line's requested
        /// change) so
        /// their SUM always equals GovernmentSpending, matching the legacy mechanic's semantics below
        /// exactly - callers (e.g. GameController's "net" display) can keep subtracting both without
        /// double-counting this turn's change. EffectiveDecision maps this turn's per-category deltas
        /// onto the four legacy category-spending fields (see BuildEffectiveDecisionForDetailedSpending)
        /// so MacroSystem's existing category-effect/approval formulas keep working unmodified.
        ///
        /// For a country without one, this is exactly the old baselineGovernmentSpending +
        /// decision.TotalDiscretionarySpending mechanic, byte-for-byte unchanged.
        /// </summary>
        private DetailedSpendingResult ResolveSpendingForTurn(Country country, PolicyDecision decision)
        {
            if (country.SpendingLines.Count > 0)
            {
                ApplyDiscretionarySpendingGrowth(country);
                ApplyMandatorySpendingGrowth(country);
                ApplyDemographicPensionPressure(country);
                ApplyDemographicHealthcarePressure(country);
                float discretionaryTotalBefore = GetSpendingLineTotal(country, mandatory: false);
                SpendingLineChangeResult changeResult = ApplySpendingLineChanges(country, decision);
                float discretionaryTotalAfter = GetSpendingLineTotal(country, mandatory: false);
                float mandatoryTotal = GetSpendingLineTotal(country, mandatory: true);

                return new DetailedSpendingResult
                {
                    BaselineGovernmentSpending = discretionaryTotalBefore,
                    GovernmentSpending = discretionaryTotalAfter,
                    DiscretionarySpendingChangeThisTurn = discretionaryTotalAfter - discretionaryTotalBefore,
                    MandatorySpending = mandatoryTotal,
                    MandatorySpendingChangeThisTurn = changeResult.MandatoryDollarChangeTotal,
                    EffectiveDecision = BuildEffectiveDecisionForDetailedSpending(decision, changeResult)
                };
            }

            float baselineGovernmentSpending = GetBaselineGovernmentSpending(country);
            return new DetailedSpendingResult
            {
                BaselineGovernmentSpending = baselineGovernmentSpending,
                GovernmentSpending = baselineGovernmentSpending + decision.TotalDiscretionarySpending,
                DiscretionarySpendingChangeThisTurn = decision.TotalDiscretionarySpending,
                MandatorySpending = 0f,
                MandatorySpendingChangeThisTurn = 0f,
                EffectiveDecision = decision
            };
        }

        /// <summary>
        /// Every Discretionary line drifts up each turn at the country's own PotentialGrowthRate -
        /// the same rate PotentialGDP itself compounds at - restoring the property the old
        /// GDP-proportional GovernmentSpendingRate mechanic had (G growing in step with trend GDP),
        /// which a fixed-dollar SpendingLines portfolio otherwise loses entirely. See "Discretionary
        /// Spending Growth" in CLAUDE.md for why this specific rate is the only stable choice - a
        /// faster rate causes runaway divergence (the growing G term eventually dominates and GDP
        /// explodes), a slower one reintroduces a widening gap. A no-op for a country without a
        /// detailed SpendingLines portfolio (the loop body never runs against an empty list).
        ///
        /// SeedAmount grows by this SAME factor, right alongside Amount - this is what stops the
        /// MaxSpendingLineAmountRatio ceiling (see that constant) from silently freezing G in absolute
        /// dollar terms. An earlier version left SeedAmount fixed at construction forever, so this
        /// passive growth alone reached the 3x ceiling after ~56 turns with zero player input and then
        /// flattened Amount there for the rest of the game - which broke the very "G tracks GDP"
        /// property this method exists to provide: G stopped growing while tax revenue (proportional to
        /// GDP) kept climbing, producing an ever-widening primary surplus that paid USA's debt to
        /// exactly 0 by turn ~70 and flatlined it there - see "SpendingLine Amount Ceiling -
        /// Debt-to-Zero Fix" in CLAUDE.md. Growing both figures by the identical factor leaves their
        /// ratio unchanged when it started in range (so a previously-uncapped line is unaffected), and
        /// still lets ClampToSeedRange correct anything that drifted out - a maxed-out (player-exploited)
        /// line now stays pegged at exactly MaxSpendingLineAmountRatio times a SeedAmount that itself
        /// keeps compounding, so even the exploited case keeps tracking GDP instead of freezing.
        /// </summary>
        private void ApplyDiscretionarySpendingGrowth(Country country)
        {
            foreach (SpendingLine line in country.SpendingLines)
            {
                if (line.IsMandatory)
                {
                    continue;
                }

                float growthFactor = 1f + country.PotentialGrowthRate / 100f;
                line.SeedAmount *= growthFactor;
                line.Amount = ClampToSeedRange(line, line.Amount * growthFactor);
            }
        }

        /// <summary>
        /// Every Mandatory line ALSO drifts up each turn at the country's own PotentialGrowthRate,
        /// mirroring ApplyDiscretionarySpendingGrowth exactly (same growth rate, same lockstep
        /// SeedAmount growth so MaxSpendingLineAmountRatio's ceiling tracks GDP rather than freezing).
        /// Real mandatory/entitlement spending (Social Security, Medicare, Medicaid, etc.) grows with
        /// the economy too - demographics and healthcare-cost growth don't stop just because a program
        /// is Mandatory rather than Discretionary - and its previous fixed-dollar freeze was a second,
        /// separate contributor (alongside the debt-risk-premium feedback loop) to the debt-to-GDP
        /// bimodality "SpendingLine Amount Ceiling - Debt-to-Zero Fix" investigated. Growing Mandatory
        /// at this same rate was tried in isolation during that investigation and found to overshoot
        /// badly (DebtToGdpRatio pegged near 294%) - it only became viable once paired with
        /// GetFiscalReactionMultiplier's negative feedback (see "Fiscal Reaction Function" in
        /// CLAUDE.md), which is the fix that actually closed the gap; this growth is shipped alongside
        /// it, not in isolation. A no-op for a country without a detailed SpendingLines portfolio.
        /// </summary>
        private void ApplyMandatorySpendingGrowth(Country country)
        {
            foreach (SpendingLine line in country.SpendingLines)
            {
                if (!line.IsMandatory)
                {
                    continue;
                }

                float growthFactor = 1f + country.PotentialGrowthRate / 100f;
                line.SeedAmount *= growthFactor;
                line.Amount = ClampToSeedRange(line, line.Amount * growthFactor);
            }
        }

        /// <summary>First SpendingLine matching category, or null if the country has none - a plain linear search, matching this file's existing no-LINQ style.</summary>
        private static SpendingLine FindSpendingLine(Country country, SpendingCategory category)
        {
            foreach (SpendingLine line in country.SpendingLines)
            {
                if (line.Category == category)
                {
                    return line;
                }
            }

            return null;
        }

        /// <summary>Fraction of the pension-equivalent line's own current Amount added per point DependencyRatio sits above its own Country.BaselineDependencyRatio, before MaxPensionPressureFraction caps the result.</summary>
        private const float PensionPressureSensitivity = 0.0002f;

        /// <summary>Cap on ApplyDemographicPensionPressure's per-turn fractional nudge - deliberately small ("small and bounded," the same standard this task set for healthcare cost pressure too), reached once DependencyRatio has drifted roughly 25 points above baseline.</summary>
        private const float MaxPensionPressureFraction = 0.005f;

        /// <summary>
        /// Round 3 item 5, Part A: nudges the pension-equivalent SpendingLine's Amount up as
        /// DependencyRatio rises above its own baseline - real, rising old-age dependency mechanically
        /// raises pension outlays. Targets USA's Mandatory SocialSecurity line, or (for the other five
        /// countries, which have no Mandatory portfolio at all) their Discretionary SocialPrograms line
        /// from "Country Selection" Part 2 - the closest analog they have, honestly an approximation
        /// (SocialPrograms is broader than pensions specifically), not a precise pension-specific line.
        ///
        /// Reconciled against the ALREADY-EXISTING automatic growth mechanism (ApplyMandatorySpendingGrowth/
        /// ApplyDiscretionarySpendingGrowth, both already run earlier this same call) by nudging Amount
        /// ONLY, never SeedAmount - the automatic growth mechanism is the sole thing that ever moves
        /// SeedAmount (and therefore the [0.2x, 3.0x] ceiling's own moving reference point), so this
        /// can never itself become a SECOND source of ceiling drift the way "SpendingLine Amount
        /// Ceiling - Debt-to-Zero Fix" once found and fixed for Discretionary spending. Re-clamped to
        /// the line's existing ceiling via the same ClampToSeedRange every other spending-line mutation
        /// already uses. Runs INSIDE ResolveSpendingForTurn (via the call just above) rather than after
        /// it returns, so this turn's own fiscal totals already reflect the nudge, the same timing the
        /// automatic growth mechanism itself already has - a no-op if neither line exists (shouldn't
        /// happen given WorldFactory's seeding, but defensive).
        /// </summary>
        private void ApplyDemographicPensionPressure(Country country)
        {
            SpendingLine pensionLine = FindSpendingLine(country, SpendingCategory.SocialSecurity)
                ?? FindSpendingLine(country, SpendingCategory.SocialPrograms);
            if (pensionLine == null)
            {
                return;
            }

            float dependencyGap = Mathf.Max(0f, country.State.DependencyRatio - country.BaselineDependencyRatio);
            float pressureFraction = Mathf.Clamp(PensionPressureSensitivity * dependencyGap, 0f, MaxPensionPressureFraction);
            pensionLine.Amount = ClampToSeedRange(pensionLine, pensionLine.Amount * (1f + pressureFraction));
        }

        /// <summary>Fraction of Medicare's own current Amount added per point DependencyRatio sits above its own Country.BaselineDependencyRatio, before MaxHealthcarePressureFraction caps the result.</summary>
        private const float HealthcarePressureSensitivity = 0.00015f;

        /// <summary>Cap on ApplyDemographicHealthcarePressure's per-turn fractional nudge - small and bounded, per this item's own explicit framing.</summary>
        private const float MaxHealthcarePressureFraction = 0.004f;

        /// <summary>
        /// Round 3 item 5, Part A: nudges USA's Medicare SpendingLine's Amount up as DependencyRatio
        /// rises above its own baseline - real, aging-driven healthcare cost pressure (Medicare
        /// specifically serves the elderly population, the one existing line with a genuinely direct
        /// real-world link to population aging - Medicaid/HHSDiscretionary serve broader populations
        /// and were deliberately left untouched). USA-ONLY in this pass - the other five countries have
        /// no Medicare-equivalent line ("Country Selection" Part 2's generic decomposition has no
        /// healthcare-specific category), honestly disclosed rather than forced onto an unrelated line,
        /// the same "USA-first, no clean analog exists yet" precedent "Detailed Spending Portfolio" and
        /// the original Sovereign Wealth Fund both already established. Reconciled against the
        /// automatic growth mechanism the exact same way ApplyDemographicPensionPressure is - Amount
        /// only, never SeedAmount, re-clamped via ClampToSeedRange, run inside ResolveSpendingForTurn
        /// so this turn's own totals already reflect it.
        /// </summary>
        private void ApplyDemographicHealthcarePressure(Country country)
        {
            SpendingLine medicareLine = FindSpendingLine(country, SpendingCategory.Medicare);
            if (medicareLine == null)
            {
                return;
            }

            float dependencyGap = Mathf.Max(0f, country.State.DependencyRatio - country.BaselineDependencyRatio);
            float pressureFraction = Mathf.Clamp(HealthcarePressureSensitivity * dependencyGap, 0f, MaxHealthcarePressureFraction);
            medicareLine.Amount = ClampToSeedRange(medicareLine, medicareLine.Amount * (1f + pressureFraction));
        }

        /// <summary>
        /// Applies this turn's requested PERCENTAGE change (PolicyDecision.SpendingLineChanges) to
        /// every SpendingLine - both Mandatory and Discretionary are adjustable, but the requested
        /// percentage is clamped to a narrower range for Mandatory (MandatoryPercentChangeRange) than
        /// Discretionary (DiscretionaryPercentChangeRange) before being applied to that line's OWN
        /// current Amount (not a flat dollar figure - a +15% slider on an $850B line and a +15%
        /// slider on a $1B line move by proportionally different dollar amounts). The result is then
        /// clamped to [MinSpendingLineAmountRatio, MaxSpendingLineAmountRatio] of the line's own fixed
        /// SeedAmount (replacing the old floor-at-0 - 0.2x SeedAmount is always a stricter, higher
        /// floor than 0) - this is what actually stops the sustained-compounding exploit (see
        /// "Percentage-Based Spending Sliders" in CLAUDE.md): holding a slider at its max every turn
        /// for 100 turns previously produced a ~geometric blowup, and now flattens out once the line
        /// hits 3x its starting size, however many turns that took. Returns the actual dollar change
        /// observed per category (post-clamp) and the total across Mandatory lines, so callers can
        /// feed the real applied effect into MacroSystem's category-effect/approval formulas rather
        /// than the raw requested percentage.
        /// </summary>
        private SpendingLineChangeResult ApplySpendingLineChanges(Country country, PolicyDecision decision)
        {
            var result = new SpendingLineChangeResult();
            foreach (SpendingLine line in country.SpendingLines)
            {
                float amountBefore = line.Amount;

                if (decision.SpendingLineChanges.TryGetValue(line.Category, out float requestedPercent) && requestedPercent != 0f)
                {
                    float maxRange = line.IsMandatory ? MandatoryPercentChangeRange : DiscretionaryPercentChangeRange;
                    float clampedPercent = Mathf.Clamp(requestedPercent, -maxRange, maxRange);
                    line.Amount = ClampToSeedRange(line, line.Amount * (1f + clampedPercent / 100f));
                }

                float actualChange = line.Amount - amountBefore;
                result.ActualDollarChangeByCategory[line.Category] = actualChange;
                if (line.IsMandatory)
                {
                    result.MandatoryDollarChangeTotal += actualChange;
                }
            }

            return result;
        }

        private static float GetSpendingLineTotal(Country country, bool mandatory)
        {
            float total = 0f;
            foreach (SpendingLine line in country.SpendingLines)
            {
                if (line.IsMandatory == mandatory)
                {
                    total += line.Amount;
                }
            }
            return total;
        }

        private static float GetActualDollarChange(SpendingLineChangeResult changeResult, SpendingCategory category)
        {
            return changeResult.ActualDollarChangeByCategory.TryGetValue(category, out float value) ? value : 0f;
        }

        /// <summary>
        /// Maps this turn's ACTUAL dollar change per Discretionary category (not the raw requested
        /// percentage - PolicyDecision.SpendingLineChanges is now a percentage of that line's own
        /// Amount, so it must be converted through ApplySpendingLineChanges' real, clamped effect
        /// before it means anything in dollar terms) onto the four legacy category-spending-effect
        /// fields (Infrastructure -&gt; Transportation, Healthcare -&gt; HHSDiscretionary,
        /// Education -&gt; Education, Defense -&gt; Defense) so MacroSystem.ApplyCategorySpendingEffects/
        /// ApplyApprovalRating (unmodified) still read a meaningful this-turn dollar delta for each of
        /// their four existing effects, without MacroSystem needing to know about SpendingCategory at
        /// all. Medicaid (Mandatory) is deliberately NOT folded into the Healthcare bucket here anymore -
        /// now that Mandatory categories are player-adjustable, doing so would double-count Medicaid's
        /// effect through both this legacy bucket AND the new, distinctly-weighted Mandatory approval
        /// term (see MacroSystem.MandatorySpendingApprovalMultiplier) that now covers it uniformly with
        /// every other Mandatory category. Four more categories (Justice/HomelandSecurity/Energy/
        /// Housing) were given their own effects in Phase 2 (see CLAUDE.md's "Detailed Spending
        /// Portfolio Phase 2") and are mapped here the same way - every other Discretionary category
        /// still gets zero effect, since Phase 2 only extended 4 of the remaining 15 effect-less
        /// categories, not an exhaustive list. Country-selection task, Part 2: InfrastructureSpendingChange
        /// now also checks SpendingCategory.InfrastructureAndDevelopment alongside Transportation - the
        /// generic category Sweden/Germany/France/Italy/Poland's decomposition uses in place of USA's
        /// own Transportation line (see WorldFactory.SeedGenericSpendingLines). Safe to simply ADD both
        /// GetActualDollarChange calls rather than branch on which one applies: a given country's
        /// SpendingLines can only ever contain one of the two (USA has Transportation, the other five
        /// have InfrastructureAndDevelopment, never both), so the OTHER call always resolves to 0 via
        /// GetActualDollarChange's own "not present in the dictionary" fallback. DefenseSpendingChange
        /// needed NO change at all - SpendingCategory.Defense is reused directly (not a new category)
        /// by the same five countries' portfolios, so this already-existing line picks it up
        /// automatically.
        /// </summary>
        private static PolicyDecision BuildEffectiveDecisionForDetailedSpending(PolicyDecision decision, SpendingLineChangeResult changeResult)
        {
            return new PolicyDecision
            {
                TaxRateOverrides = decision.TaxRateOverrides,
                InterestRateChange = decision.InterestRateChange,
                TariffRateChange = decision.TariffRateChange,
                InfrastructureSpendingChange = GetActualDollarChange(changeResult, SpendingCategory.Transportation)
                    + GetActualDollarChange(changeResult, SpendingCategory.InfrastructureAndDevelopment),
                HealthcareSpendingChange = GetActualDollarChange(changeResult, SpendingCategory.HHSDiscretionary),
                EducationSpendingChange = GetActualDollarChange(changeResult, SpendingCategory.Education),
                DefenseSpendingChange = GetActualDollarChange(changeResult, SpendingCategory.Defense),
                JusticeSpendingChange = GetActualDollarChange(changeResult, SpendingCategory.Justice),
                HomelandSecuritySpendingChange = GetActualDollarChange(changeResult, SpendingCategory.HomelandSecurity),
                EnergySpendingChange = GetActualDollarChange(changeResult, SpendingCategory.Energy),
                HousingSpendingChange = GetActualDollarChange(changeResult, SpendingCategory.Housing)
            };
        }

        /// <summary>
        /// Automatic stabilizer: unemployment benefit spending that scales with the unemployment
        /// rate with no player input, via the country's own BenefitRatePerUnemployed. Uses this
        /// turn's starting (prior-turn) Unemployment, matching how GetBaselineGovernmentSpending
        /// uses prior GDP - the value known at the start of the turn, before this turn's updates run.
        /// </summary>
        private float GetUnemploymentBenefitCost(Country country)
        {
            EconomyState state = country.State;
            return country.BenefitRatePerUnemployed * state.Unemployment / 100f * state.GDP;
        }

        /// <summary>Sovereign risk premium: lenders charge more above a conventional "safe" debt-to-GDP benchmark, capped so it can't make InterestOnDebt quadratic in Debt.</summary>
        private float GetDebtRiskPremium(EconomyState state)
        {
            float excessDebtToGdp = Mathf.Max(0f, state.DebtToGdpRatio - RiskFreeDebtToGdpPercent);
            return Mathf.Min(MaxDebtRiskPremium, DebtRiskPremiumRate * excessDebtToGdp);
        }

        /// <summary>
        /// Interest on the country's existing debt stock, at its base rate (CurrencyZone.InterestRate,
        /// unless overridden - see Country.BaseDebtInterestRateOverride) plus the risk premium, scaled
        /// by the country's RiskPremiumSensitivity. For most countries this is unchanged: today's
        /// policy rate plus the full risk premium. A reserve-currency issuer (the USA) uses a real
        /// blended average rate on existing debt instead of today's policy rate, and a near-zero
        /// sensitivity to the risk-premium curve - see "Reserve-Currency Debt Interest Treatment" in
        /// CLAUDE.md for why market risk premium at a given debt-to-GDP ratio isn't equivalent across
        /// countries.
        /// </summary>
        private float GetInterestOnDebt(Country country)
        {
            EconomyState state = country.State;
            float baseRate = country.BaseDebtInterestRateOverride >= 0f
                ? country.BaseDebtInterestRateOverride
                : country.CurrencyZone.InterestRate;
            float effectiveRate = baseRate + GetDebtRiskPremium(state) * country.RiskPremiumSensitivity;
            return state.GovernmentDebt * (effectiveRate / 100f);
        }

        /// <summary>
        /// Government revenue is GetTotalTaxRevenue's theoretical figure scaled down by the country's
        /// CollectionEfficiency (enforcement quality/informal economy/evasion - see Country's doc
        /// comment), nudged by the Finance/Treasury Cabinet minister's passive competence bias if one
        /// is appointed (Political Systems Overhaul Part A - see CabinetSystem.GetCompetenceBias,
        /// applied at point-of-use here rather than mutating the stored CollectionEfficiency field,
        /// since that field has no reversion mechanism of its own to correct a permanent drift - the
        /// same "may be safer to land somewhere more contained" reasoning the Master Roadmap's own
        /// Part A spec calls for), and then by GetFiscalReactionMultiplier (the automatic
        /// fiscal-tightening/-loosening response to this country's own debt-to-GDP gap - see that
        /// method and "Fiscal Reaction Function" in CLAUDE.md); this turn's budget balance is that
        /// actual revenue minus total spending (government spending, Mandatory SpendingLine total (0
        /// for a country without a detailed portfolio), unemployment benefits, interest on debt, and
        /// welfare program cost - see GetTotalWelfareCost - benefits, mandatory transfers, interest,
        /// and welfare are all transfers, not purchases, so they're deliberately excluded from
        /// MacroSystem's national accounts G term). A deficit adds to GovernmentDebt, a surplus
        /// reduces it, hard-clamped to a sane debt-to-GDP range. Returns the actual (post-efficiency,
        /// post-reaction) revenue so the caller can record it on this turn's FiscalTurnReport.
        /// </summary>
        private float ApplyRevenueAndSpending(Country country, float governmentSpending, float mandatorySpending, float unemploymentBenefitCost, float interestOnDebt, float welfareCost, float swfContribution, float swfReturns, out float totalSpending, out float budgetBalance)
        {
            EconomyState state = country.State;
            float theoreticalRevenue = GetTotalTaxRevenue(country);
            float fiscalReactionMultiplier = GetFiscalReactionMultiplier(country);
            float financeTreasuryCompetenceBias = CabinetSystem.GetCompetenceBias(country, CabinetPortfolio.FinanceTreasury);
            float effectiveCollectionEfficiency = Mathf.Clamp01(country.CollectionEfficiency + financeTreasuryCompetenceBias);
            float actualRevenue = theoreticalRevenue * effectiveCollectionEfficiency * fiscalReactionMultiplier + swfReturns;
            totalSpending = governmentSpending + mandatorySpending + unemploymentBenefitCost + interestOnDebt + welfareCost + swfContribution;
            budgetBalance = actualRevenue - totalSpending;

            state.Budget += budgetBalance;
            float maxDebt = MaxDebtToGdpPercent / 100f * state.GDP;
            state.GovernmentDebt = Mathf.Clamp(state.GovernmentDebt - budgetBalance, 0f, maxDebt);

            return actualRevenue;
        }
    }
}
