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
        /// <summary>Pass 6 (2026-08-27): the tariff pass-through that ACTUALLY printed over the period
        /// that just closed, in inflation points (FiscalPeriod.AppliedTariffPassThroughPp on the boundary
        /// day) - the change in the tariff take the previous boundary planned, as a price-level term for
        /// one year, net of the [0, MaxInflationPercent] clamp. The Trade stats line reads it.</summary>
        public float TariffPassThroughPp;
        public float WelfareCost;
        public float SwfContribution;
        public float SwfReturns;

        /// <summary>
        /// This turn's total spending and budget balance EXACTLY as ApplyRevenueAndSpending computed
        /// them, recorded rather than recomputed.
        ///
        /// ⚠ Corrected, pass 5 (2026-08-26): this doc used to claim the components could not be added
        /// back up because BaselineGovernmentSpending + DiscretionarySpending was "a different field"
        /// from the summed G. That was wrong - the two DO reconstruct the summed G exactly (the plan's
        /// baseline plus this period's change is what was spent). What a hand sum actually misses is
        /// SwfContribution (part of TotalSpending) and, since tariffs ride inside Revenue, any line that
        /// adds TariffRevenue on top counts it twice. Read BudgetBalance; never re-add.
        ///
        /// BudgetBalance is signed the same way the simulation signs it: positive is a SURPLUS.
        /// TariffRevenue is the accrued tariff flow at STANCE 1 - what TradeSystem computed and the
        /// period planned - while Revenue carries it times the period's fiscal-reaction multiplier;
        /// a surface showing it as "of which" says so.
        /// </summary>
        public float TotalSpending;
        public float BudgetBalance;
    }

    /// <summary>
    /// Pass 6 (2026-08-27): what a Trade bill DRAFT would do at the next boundary, from
    /// SimulationManager.EstimateTradeBill - see it for how each figure is produced (the real
    /// functions on throwaway clones, never a hand sum). Display-layer only; nothing here is ever
    /// applied.
    /// </summary>
    public class TradeBillEstimate
    {
        /// <summary>The take one period would yield at the bill's rates (ComputeTariffRevenue on the clone the bill was applied to).</summary>
        public float Take;
        /// <summary>Take at the bill's rates minus take at the standing rates.</summary>
        public float TakeDelta;
        /// <summary>TradeBalance at the bill's rates minus at the standing rates - the partners' mirrored tariffs included, the currency factor inside.</summary>
        public float TradeBalanceDelta;
        /// <summary>The price pass-through the boundary would plan, in inflation points for the coming year.</summary>
        public float PassThroughPp;
    }

    /// <summary>
    /// Estimated single-turn effect of a not-yet-committed PolicyDecision, computed by
    /// SimulationManager.PreviewTurn against a throwaway clone - see that method for what it does
    /// and doesn't reproduce faithfully. Purely a display-layer estimate; nothing here is ever
    /// written back into real EconomyState/Country/World.
    /// </summary>
    public class PolicyPreview
    {
        /// <summary>Step 2: the preview clone's approval term ledger - the same named locals the
        /// real boundary records, computed on the clone. Read by the preview-parity diagnostic
        /// (term-by-term against the real boundary's ledger) and available to future preview
        /// explanation UI.</summary>
        public ApprovalAttribution ApprovalTerms;

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
        /// 1 turn = 1 in-game year.
        ///
        /// **Was 121 days (~4 months) until 2026-08-10, and that was the source of a live 3.017x fiscal
        /// defect.** Every fiscal quantity in this game is an ANNUAL-rate figure - the spending seeds are
        /// FY2025 federal outlays ($1,530B Social Security, $850B Defense, 25 lines totalling $5,761B),
        /// GDP is annual ($29T for the USA), tax revenue is `GDP x rate x BaseShareOfGdp`, and interest
        /// is an annual rate on the debt stock. None of them was ever divided by a period. So a 121-day
        /// turn charged a full year of spending, revenue and interest every 121 days: 365/121 = 3.017x
        /// too fast, which is what Elias saw as "a full year's deficit every turn".
        ///
        /// **The same defect was found and fixed once before, in demographics.** See
        /// `MacroSystem.YearsPerTurn` - its doc comment describes "3x over-compounding via too many
        /// applications of an annual-scale rate", diagnosed and fixed there by scaling each turn's growth
        /// to the turn's real-time slice. The fiscal path has the identical defect and was never brought
        /// along. `SwfStructuralDrawPerTurnFraction` was the one flow that got the conversion.
        ///
        /// **Why the turn moved to the year rather than the flows moving to the turn** (Elias's ruling,
        /// 2026-08-10): the economy was already annual-per-turn in every respect, so making the calendar
        /// agree with the model is one constant, while making the model agree with the calendar is a
        /// divisor on every flow plus a recalibration of every debt-anchored constant. This direction
        /// also costs nothing in the continuous-time migration, because **every per-day constant in
        /// Phases 1-3 was derived from this value rather than typed** - see `MacroSystem`'s
        /// `PerDayReversion`, `CrimeEffectsDailyScale`, `InfrastructureDecayRatePerDay` and
        /// `FiscalFlowPerDayFraction`, all of which retune themselves. That discipline is what made a
        /// one-line change possible here.
        ///
        /// **`ElectionSystem.ElectionCycle` moved with it**, 12 turns to 4, so a presidential term stays
        /// four years. The two constants are a pair: they are this project's only two statements of how
        /// long a turn is, and they disagreed by 0.5% before this change (121/365 = 0.3315 years against
        /// `YearsPerTurn`'s 4/12 = 0.3333). They now agree exactly at 1.0.
        /// </summary>
        public const int DaysPerTurn = 365;

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
        /// <summary>
        /// The sim-side calls one simulated day makes for a country BEYOND <see cref="AdvanceDay"/>
        /// itself, in the order GameController.Update has always made them: the foreign-policy roll,
        /// the nine bill countdowns, and the budget-process date check.
        ///
        /// ⚠ EXTRACTED 2026-08-12 (ruled) after the capture driver re-derived this list by COPYING it,
        /// and the copy immediately proved the point: three separate "never captured" findings in one
        /// day were the driver's day differing from the controller's day (the arm calls, then the
        /// countdowns, then CheckElection — each family found by pinning nothing and measuring). One
        /// method, two callers — GameController.Update and UiScreenshotDriver's state searches — so a
        /// call added here reaches both, which is the entire reason it exists.
        ///
        /// <para>Deliberately NOT here: <c>AdvanceDay</c>, whose turn-boundary return belongs to the
        /// caller; and the controller's <c>CheckElection</c>/<c>UpdateFedChairSelectionState</c>, which
        /// build UI-pending state rather than simulation state. The per-call comments that used to sit
        /// on these ten lines in Update — which calls need a gate re-check and why — are preserved at
        /// the call site, where the gates are.</para>
        /// </summary>
        public void AdvanceCountryDayTick(CountryId countryId)
        {
            TryRollForeignPolicyMeeting(countryId);
            AdvanceBudgetBillDay(countryId);
            AdvanceTaxProgramBillsDay(countryId);
            AdvanceWelfareProgramBillsDay(countryId);
            AdvanceLaborBillDay(countryId);
            AdvanceCrimeJusticeBillDay(countryId);
            AdvanceSectorBillDay(countryId);
            AdvanceTradeBillDay(countryId);
            AdvanceSwfDrawdownBillDay(countryId);
            AdvanceLawBillsDay(countryId);
            TryOpenBudgetProcess(countryId, CurrentDate);
        }

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

                    // CONTINUOUS TIME PHASE 1 (2026-08-02): Sectors and Infrastructure now evolve DAILY.
                    // This is the first economic state AdvanceDay has ever touched - Phase 0 deliberately
                    // moved only the calendar - so the ordering guarantee that made publishing safe no
                    // longer holds for these two, and they are placed after it on purpose: publication
                    // reads a day's state, so it must see the state that day actually produced.
                    //
                    // Both are aggregation-equivalent to their turn forms by construction rather than by
                    // tolerance; see the two Daily methods for the per-constant reasoning.
                    MacroSystem.ApplySectorEffectsDaily(country);
                    MacroSystem.ApplyInfrastructureConditionDaily(country);

                    // CONTINUOUS TIME PHASE 4 (2026-08-16): Demographics daily. Rates BEFORE population
                    // (ApplyPopulationGrowth reads the same-step freshly-updated Birth/Death/Migration,
                    // the turn form's own documented contract), and BOTH before the Phase 2 block below
                    // (ApplyLaborForceParticipationRateDaily reads DependencyRatio/NetMigrationRate gaps -
                    // it now sees them move daily instead of jumping once per turn, which is the point).
                    // The boundary-day ordering contract survives unchanged: AdvanceDay completes before
                    // AdvanceTurn, so ResolveSpendingForTurn still reads a DependencyRatio that finished
                    // this period's accumulation. One deliberate 1/365 timing shift, stated: a
                    // Family/Immigration policy change committed at a boundary reaches the rates from the
                    // NEXT day rather than the same instant - Phase 3's cash-lands-next-period precedent
                    // in miniature.
                    MacroSystem.ApplyDemographicRatesDaily(country);
                    MacroSystem.ApplyPopulationGrowthDaily(country);

                    // CONTINUOUS TIME PHASE 2: Labor Market and Crime & Justice. The ORDER matters and is
                    // preserved exactly from AdvanceTurn - OrganizedCrime and Corruption run BEFORE
                    // CrimeIndex, which reads that day's freshly-updated OrganizedCrimeIndex, and
                    // ApplyCrimeEffects runs after all three because it reads all of their gaps.
                    // Phase 3, part 1: PovertyRate's reversion.
                    MacroSystem.ApplyPovertyRateDaily(country);
                    MacroSystem.ApplyLaborForceParticipationRateDaily(country);
                    MacroSystem.ApplyOrganizedCrimeIndexDaily(country);
                    MacroSystem.ApplyCorruptionIndexDaily(country);
                    MacroSystem.ApplyCrimeIndexDaily(country);
                    MacroSystem.ApplyCrimeEffectsDaily(country);
                    MacroSystem.ApplyPrisonPopulationRateDaily(country);

                    // ROUND 4 BATCH 1 (C3): daily-native from day one, per the foundations gate.
                    // Youth unemployment AFTER the Phase 5 macro block below would read yesterday's
                    // headline... it sits here, BEFORE the macro block, so it reads the same
                    // start-of-day Unemployment its inputs-only contract names - and life expectancy
                    // reads the PovertyRate the poverty daily above just settled. Neither writes
                    // anything any later system reads (inputs-only, the ruled Round 4 posture).
                    MacroSystem.ApplyYouthUnemploymentDaily(country);
                    MacroSystem.ApplyLifeExpectancyDaily(country);

                    // ROUND 4 BATCH 2 (C2): same slot, same reasoning - Gini reads the same
                    // start-of-day Unemployment youth-U reads; the wage index reads yesterday's
                    // settled Inflation/Expectations and the structural trend rate. Neither writes
                    // anything any later system reads (inputs-only, the ruled posture).
                    MacroSystem.ApplyGiniDaily(country);
                    // Q5: wages read trend + productivity's hoarding cycle, and the cycle takes the
                    // PERIOD-OPEN unemployment as its anchor - the same fixed reference Okun already
                    // uses, applied here preemptively because a daily-moving driver inside a power
                    // slice is Q2's measured failure shape.
                    MacroSystem.ApplyRealWageIndexDaily(country, GetOrSeedFiscalPeriod(country).UnemploymentAtPeriodOpen);

                    // ROUND 4 BATCH 3 (C1): same slot; all three read the policy rate against the
                    // zone's epoch anchor (the arc's first monetary coupling - one-way, stated at
                    // MacroSystem's C1 header) and write nothing any later system reads. Overburden
                    // early-outs for the USA per the recorded asymmetry ruling.
                    MacroSystem.ApplyHousingOverburdenDaily(country);
                    MacroSystem.ApplyHomeownershipDaily(country);
                    MacroSystem.ApplyHousePriceIndexDaily(country);

                    // ROUND 4 BATCH R4-5 (C5): same slot, the arc's last stat - pure trend
                    // compounding, reads PotentialGrowthRate only, nothing consumes it (the
                    // coupling is ruled out of Round 4; see MacroSystem's C5 header).
                    MacroSystem.ApplyProductivityDaily(country, GetOrSeedFiscalPeriod(country).UnemploymentAtPeriodOpen);

                    // CONTINUOUS TIME PHASE 3, part 2: the money resolution. Revenue, benefits, welfare,
                    // interest, the SWF's contribution/return/draw and the debt stock itself all move
                    // daily now; only the BUDGET RESOLUTION that decides what to spend stays on the turn
                    // boundary, because a budget passing is an event on a date rather than a flow.
                    //
                    // Placed last in the day so it charges interest against the debt every earlier
                    // system has finished with, and reads the same GDP the rest of the day did.
                    AccrueDailyFiscalFlows(country);

                    // CONTINUOUS TIME PHASE 5 (2026-08-16): the core macro engine, daily - the last
                    // and riskiest conversion, deliberately last. Order preserved from AdvanceTurn's
                    // own sequence: identity (reading the CURRENT period's planned G - the same plan
                    // the fiscal accrual above spends - and the live zone rate), then trend growth,
                    // then Okun from the day's realized growth (annualized for the gap, sliced back
                    // inside), then Phillips (a level map, same form both regimes), then expectations.
                    // The day's growth is measured across the WHOLE day-step so far... deliberately
                    // NOT: it is measured across this block alone (gdp before the identity vs after),
                    // matching the turn form's own "growth produced by the identity" semantics rather
                    // than folding in event shocks, which land at boundaries and decay through the
                    // identity exactly as they always did.
                    FiscalPeriod macroPeriod = GetOrSeedFiscalPeriod(country);
                    float gdpBeforeMacroStep = country.State.GDP;
                    float anchoredPotential = macroPeriod.PotentialGdpAtPeriodOpen > 0f
                        ? macroPeriod.PotentialGdpAtPeriodOpen
                        : country.State.PotentialGDP;
                    MacroSystem.ApplyNationalAccountsDaily(country, macroPeriod.PlannedGovernmentSpending, country.CurrencyZone.InterestRate, anchoredPotential, macroPeriod.WageGrowthGapAtPeriodOpen);
                    MacroSystem.ApplyPotentialGdpGrowthDaily(country);
                    // The day's growth increment is measured against the PERIOD-OPEN GDP, not the
                    // day's own base - the third fixed reference of this phase: daily linear
                    // increments over a fixed denominator sum EXACTLY to the turn form's linear
                    // period growth, where per-day-base percents sum to log growth and leave a
                    // second-order Okun residual that failed the bar on the seeded US output gap
                    // (measured 0.21 unemployment points at a 2% drive). Still causal - the
                    // increment is today's.
                    float openGdp = macroPeriod.GdpAtPeriodOpen > 0f ? macroPeriod.GdpAtPeriodOpen : gdpBeforeMacroStep;
                    float annualizedDailyGrowth = (country.State.GDP - gdpBeforeMacroStep) / Mathf.Max(openGdp, 1f) * 100f * DaysPerTurn;
                    MacroSystem.ApplyOkunsLawDaily(country, annualizedDailyGrowth, macroPeriod.UnemploymentAtPeriodOpen);
                    // Pass 6: the period's tariff pass-through rides the level map for the whole
                    // period (a price-LEVEL stance planned at the boundary); what actually printed
                    // is kept on the period so the boundary's expectations step can look through it.
                    macroPeriod.AppliedTariffPassThroughPp = MacroSystem.ApplyPhillipsCurveInflation(country, macroPeriod.PlannedTariffPassThroughPp);
                    // Expectations deliberately absent here - a boundary stance; see MacroSystem's
                    // Phase 5 block comment for the measured failure of the daily form.

                    // PHASE 4: the history point, moved here from AdvanceTurn - see the comment at its
                    // old site for the finding. After every system, so the day's point is the state the
                    // day actually produced (the publication-placement reasoning). Phase 5 note: with
                    // the macro core daily too, the old "turn-stepped stats jump on the next day's
                    // point" caveat has retired itself - every economic quantity now moves on the day
                    // its point records.
                    country.History.Append(CurrentDate, country.State, country.CurrencyZone.InterestRate);
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
        // ⚠ FRF SWEEP (2026-08-16): `static` fields rather than `const`, because the ruled real-Unity
        // sweep of this PAIR (the harness-fitted values the record itself says to re-derive) needs to
        // vary them per run and a const is folded at compile time. The DEFAULTS are the standing
        // values, bit-identical in normal play; ONLY FrfSweepDiagnostic may write them, through
        // SetFiscalReactionPairForSweep below, and always restores the defaults. The doc comments
        // above and below carry the original calibration story unchanged - including that it was
        // fitted in the harness whose four-significant-figure stability claim real Unity refuted.
        internal const float DefaultFiscalReactionSensitivity = 1.5f;
        internal const float DefaultMinFiscalReactionMultiplier = 0.5f;
        internal const float DefaultMaxFiscalReactionMultiplier = 1.5f;
        private static float FiscalReactionSensitivity = DefaultFiscalReactionSensitivity;
        private static float MinFiscalReactionMultiplier = DefaultMinFiscalReactionMultiplier;
        private static float MaxFiscalReactionMultiplier = DefaultMaxFiscalReactionMultiplier;

        /// <summary>SWEEP-ONLY hook - see the field block above. PUBLIC because the sweep lives in
        /// the Editor assembly (the ApplyPeriodFiscalStepForValidation precedent); nothing in play
        /// code calls it. The revenue-capacity wall is enforced HERE, not left to the caller's
        /// discipline: an upper bound meaningfully above 1.5 asserts something false about fiscal
        /// capacity (the standing ruling), so this throws rather than accepts one.</summary>
        public static void SetFiscalReactionPairForSweep(float sensitivity, float minMultiplier, float maxMultiplier)
        {
            if (maxMultiplier > 1.5001f)
            {
                throw new System.ArgumentOutOfRangeException(nameof(maxMultiplier),
                    "The revenue-capacity wall: no upper bound meaningfully above 1.5 (ruled).");
            }

            FiscalReactionSensitivity = sensitivity;
            MinFiscalReactionMultiplier = minMultiplier;
            MaxFiscalReactionMultiplier = maxMultiplier;
        }

        public static void ResetFiscalReactionPair()
        {
            FiscalReactionSensitivity = DefaultFiscalReactionSensitivity;
            MinFiscalReactionMultiplier = DefaultMinFiscalReactionMultiplier;
            MaxFiscalReactionMultiplier = DefaultMaxFiscalReactionMultiplier;
        }

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

        /// <summary>
        /// The SWF's STRUCTURAL DRAW into the budget, as a percentage of fund assets per YEAR — Norway's
        /// own fiscal rule (the *handlingsregel*), which permits spending the fund's expected real return
        /// rather than its realised one, whatever the market did that year.
        ///
        /// **This replaces booking the realised return as revenue, and fixes two defects at once
        /// (2026-08-02, Elias's smoothing ruling):**
        ///
        /// 1. **VOLATILITY.** A realised return is market variance arriving in the budget. Sweden's fund
        ///    reaches ~10,900 against a GDP near 1,200, so a single turn's equity swing could exceed 100%
        ///    of GDP — which is the whole of C4's "settled deficit ranges −135.5% to +170.8%" blocker.
        ///    A draw proportional to fund SIZE is smooth by construction.
        /// 2. **DOUBLE-COUNTING.** The realised return was added to `TotalAssets` *and* the same figure
        ///    was then added to revenue — the fund kept it and the government spent it, so the money
        ///    existed twice. The contribution one line above is handled correctly (budget pays, fund
        ///    receives; money MOVES). The draw is now likewise withdrawn from the fund.
        ///
        /// 3% is Norway's own figure. Chosen over a rolling average of realised returns because an
        /// average still transmits the LEVEL of a very large fund into the budget and damps only its
        /// variance — and because this is the rule the real instrument actually uses.
        /// </summary>
        private const float SwfStructuralDrawPercentPerYear = 3f;

        /// <summary>The annual structural draw expressed per TURN. A turn is <see cref="DaysPerTurn"/> days, so a year is 365/121 turns - derived rather than hardcoded, because the continuous-time migration will change the turn length and a baked-in constant would silently become a different policy.</summary>
        private static float SwfStructuralDrawPerTurnFraction()
        {
            float turnsPerYear = 365f / DaysPerTurn;
            return SwfStructuralDrawPercentPerYear / 100f / turnsPerYear;
        }

        /// <summary>
        /// How far below zero net government debt may run before it is caught, as a percentage of GDP.
        ///
        /// **This is a RUNAWAY GUARD, not a calibrated bound**, and the distinction is the whole point of
        /// the number being this large. Its job is to stop an unbounded excursion from reaching infinity
        /// during and after the SWF-returns fix; it is NOT meant to shape any live value. Norway - the
        /// real world's most extreme net creditor, and this project's own SWF calibration reference -
        /// sits near -250% of GDP, so nothing plausible comes within a factor of four of this.
        ///
        /// ⚠ **If a country ever reaches it, that is a bug report rather than a clamp working.** The
        /// previous attempt used -300%, which France settled AT (-297.6%) - a bound that binds is a
        /// number the model reads instead of its own state, and C4's rating was reading it.
        /// </summary>
        private const float NetCreditorRunawayGuardPercent = 1000f;

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
        /// CONTINUOUS TIME PHASE 3: the fraction of a period-shaped flow that lands in one day.
        ///
        /// Translation shape #1, LINEAR. A budget flow is an accumulating quantity with no target to
        /// revert toward, so the multiplicative form MacroSystem.PerDayReversion uses does not apply -
        /// 121 payments of `flow/121` are exactly one period's payment, which is the whole translation.
        /// Derived from DaysPerTurn rather than typed, like every other daily constant in this migration.
        /// </summary>
        private const float FiscalFlowPerDayFraction = 1f / DaysPerTurn;

        /// <summary>
        /// CONTINUOUS TIME PHASE 3: one country's in-flight fiscal period - the spending plan its
        /// current 121 days are executing, plus the running sum of what those days have actually accrued.
        ///
        /// **This is the "NEW PER-COUNTRY STATE" the Phase 3 handoff named as the blocker**, and it
        /// exists because the daily step cannot re-derive everything it needs. Tax revenue, unemployment
        /// benefits, welfare cost, interest and the SWF contribution are all functions of persistent
        /// country state, so AccrueDailyFiscalFlows recomputes each of them from scratch every day.
        /// Government and Mandatory spending are NOT: for the five countries without a detailed
        /// SpendingLines portfolio the discretionary figure comes from
        /// PolicyDecision.TotalDiscretionarySpending, which exists only at a turn boundary and is
        /// persisted nowhere else. ResolveSpendingForTurn's answer is stored here so the days that follow
        /// can spend it.
        ///
        /// **A PLAN IS RESOLVED AT ONE BOUNDARY AND EXECUTED OVER THE NEXT 121 DAYS.** That ordering is
        /// forced rather than chosen - the boundary that resolves a budget is 121 days of play before the
        /// next one - and it is also what a budget is: a government passes it on a date and then spends
        /// it. The consequence worth stating plainly is that a policy change's CASH effect now lands one
        /// period after the boundary that made it, where it used to land instantly. Its effect on the GDP
        /// identity's G term does NOT move - MacroSystem.ApplyNationalAccounts still reads the plan at the
        /// boundary that resolved it - because the core macro engine is Phase 5, not this phase.
        ///
        /// **THE SWF RETURN IS DRAWN ONCE PER PERIOD AND ACCRUED DAILY** (decision recorded 2026-08-02 by
        /// the Phase 3 part 1 commit, flagged for overrule and left standing here). Drawing daily would
        /// consume 121x the RNG and invalidate every recorded baseline in the project, for no modelling
        /// gain - the draw's granularity is not what this migration is about.
        /// </summary>
        // PUBLIC since save/load (item 8, 2026-08-16): SimulationPendingState carries these whole -
        // plan, frozen multiplier and mid-period accruals - because a save taken mid-period that
        // dropped the accruals would close its next boundary against a partial year. Public-NESTED,
        // not promoted to its own file: nothing but this class and the save shape may construct one.
        public class FiscalPeriod
        {
            // THE PLAN - set by the boundary that opened this period, spent across its days.
            public float PlannedGovernmentSpending;
            public float PlannedMandatorySpending;
            public float PlannedBaselineGovernmentSpending;
            public float PlannedDiscretionarySpending;
            public float PlannedSwfReturn;

            /// <summary>Pass 5 (2026-08-26): the period's tariff revenue - TradeSystem's figure at the
            /// boundary that opened this period (the seed period reads the same pure function before
            /// any turn), accrued daily inside ApplyRevenueAndSpending as one more revenue term. A
            /// zero from a pre-pass-5 save degrades to "no tariff flow for the loaded period's
            /// remainder", self-correcting at the next boundary - the WageGrowthGapAtPeriodOpen
            /// posture, no guard needed.</summary>
            public float PlannedTariffRevenue;

            /// <summary>Pass 6 (2026-08-27): the tariff pass-through planned for this period -
            /// TradeCosts.ImportPricePassThrough x 100 x (this boundary's take - the closing period's
            /// planned take) / GDP, in inflation points, read every day by the Phillips level map. Exactly
            /// 0 when no rate changed (the same pure sum on unchanged state). An old-save zero degrades to
            /// "no pass-through for the loaded period's remainder" - the WageGrowthGapAtPeriodOpen posture,
            /// no guard.</summary>
            public float PlannedTariffPassThroughPp;

            /// <summary>Pass 6: what of the planned pass-through ACTUALLY printed on the latest day
            /// (ApplyPhillipsCurveInflation's return - the clamped print with the term minus the clamped
            /// print without it). The boundary reads the closing day's value into the FiscalTurnReport and
            /// into ApplyInflationExpectations' look-through, so a cut whose negative wedge floors the
            /// print at 0 cannot ratchet expectations. Overwritten daily; nothing reads it between the
            /// boundary and the next day, so ResetAccrual leaves it alone.</summary>
            public float AppliedTariffPassThroughPp;

            /// <summary>
            /// GetFiscalReactionMultiplier as it stood when this period opened, held FIXED for its whole
            /// 121 days - and this is the one Phase 3 constant that is a modelling call rather than a
            /// mechanical translation.
            ///
            /// **Recomputing it daily was tried first and FAILED the aggregation bar outright** (Sweden
            /// 24.8% drift on budget balance, Germany 22.7%, against a 3% bar). The cause is not a bug:
            /// FiscalReactionSensitivity is 1.5 and a single period moves a country's debt ratio by ten
            /// points or more, so a multiplier that re-reads the ratio every day walks a long way down
            /// its own surplus during the period it is supposed to be governing. The turn form could not
            /// do this - its debt stock moved exactly once - so daily recomputation was not a finer
            /// version of the validated model, it was a different one.
            ///
            /// Freezing it is also the more defensible reading of what the mechanism IS. Its own doc
            /// comment describes "a country's own government modestly tightens... as debt rises above its
            /// ComfortableDebtToGdpPercent anchor" - a fiscal STANCE, and a stance is adopted when the
            /// budget is set, not re-derived every morning. The stance still responds fully to the debt
            /// the country has actually accumulated; it does so at the boundary, which is where every
            /// other budget decision in this game is made.
            /// </summary>
            public float PlannedFiscalReactionMultiplier;

            /// <summary>PHASE 5: GDP as the period opened. With the identity daily, "this turn's
            /// realized growth" (ApprovalRating's input) is the PERIOD's growth, measured from here -
            /// the top-of-AdvanceTurn snapshot the turn form used no longer exists, because by the
            /// boundary the days have already moved GDP. A zero (an old save from before this field)
            /// reads as "no growth measurable yet" via the guard at the read site.</summary>
            public float GdpAtPeriodOpen;

            /// <summary>PHASE 5: unemployment as the period opened - the FIXED REFERENCE for Okun's
            /// distributed reversion (the FRF frozen-stance pattern; see ApplyOkunsLaw's own Phase 5
            /// comment for the measured failure of the self-referencing form). A zero from an old
            /// save degrades to "revert toward NAIRU from zero" for one period's remainder - visible
            /// and self-correcting at the next boundary, preferred over a special case.</summary>
            public float UnemploymentAtPeriodOpen;

            /// <summary>PHASE 5: PotentialGDP as the period opened - the identity's attractor anchor
            /// (the fourth fixed reference; see ApplyNationalAccountsDaily for the measured Okun
            /// amplification that a live attractor causes). Old-save zero degrades through the same
            /// guard the identity call site applies to GdpAtPeriodOpen.</summary>
            public float PotentialGdpAtPeriodOpen;

            /// <summary>Q2: the wage-growth gap (pp/turn) as the period opened - the sentiment
            /// factor's anchor (the FIFTH fixed reference; see the anchored
            /// EffectiveConsumerConfidence overload for the measured @8%shock divergence a live
            /// gap causes). An old-save zero needs NO guard: gap 0 means factor 1 - the identity
            /// simply reads the base for the loaded period's remainder and self-corrects at the
            /// next boundary, the same degradation posture as UnemploymentAtPeriodOpen.</summary>
            public float WageGrowthGapAtPeriodOpen;

            // THE ACCRUAL - summed day by day, closed out into a FiscalTurnReport at the next boundary.
            public float AccruedRevenue;
            public float AccruedMandatorySpending;
            public float AccruedUnemploymentBenefitCost;
            public float AccruedInterestOnDebt;
            public float AccruedWelfareCost;
            public float AccruedSwfContribution;
            public float AccruedSwfReturns;
            public float AccruedTotalSpending;
            public float AccruedBudgetBalance;
            /// <summary>Pass 5: the tariff portion of AccruedRevenue, kept separately so the
            /// FiscalTurnReport can show "of which tariffs" as a true reading of what accrued.</summary>
            public float AccruedTariffRevenue;

            public void ResetAccrual()
            {
                AccruedRevenue = 0f;
                AccruedMandatorySpending = 0f;
                AccruedUnemploymentBenefitCost = 0f;
                AccruedInterestOnDebt = 0f;
                AccruedWelfareCost = 0f;
                AccruedSwfContribution = 0f;
                AccruedSwfReturns = 0f;
                AccruedTotalSpending = 0f;
                AccruedBudgetBalance = 0f;
                AccruedTariffRevenue = 0f;
            }
        }

        private readonly Dictionary<CountryId, FiscalPeriod> _fiscalPeriods = new Dictionary<CountryId, FiscalPeriod>();

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
        /// Law system MVP slice: standalone law bills, keyed by CountryId then by LawId - the SAME
        /// nested-dictionary shape _pendingTaxProgramBillsByCountry/_pendingWelfareProgramBillsByCountry
        /// already use, for the identical reason: multiple different laws (each independently
        /// enacted or repealed) can have their own bill pending for the same country at once, just
        /// never two for the SAME law simultaneously. Introducible anytime, no mandatory pause - the
        /// same non-blocking daily countdown idiom every standalone bill tier already uses. See
        /// LawBill/LawDefinition/LawCatalog.
        /// </summary>
        private readonly Dictionary<CountryId, Dictionary<string, LawBill>> _pendingLawBillsByCountry =
            new Dictionary<CountryId, Dictionary<string, LawBill>>();

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

        /// <summary>Elias's A2 ruling: the SWF emergency drawdown is a fifth standalone tier-3 bill, so it gets its own pending slot exactly like the other four.</summary>
        private readonly Dictionary<CountryId, SwfDrawdownBill> _pendingSwfDrawdownBillByCountry =
            new Dictionary<CountryId, SwfDrawdownBill>();

        /// <summary>
        /// R-S3e: the active scenario's foreign-policy pacing multiplier, or 1 in free play - set by
        /// GameController when a scenario starts and re-derived from the scenario id on load, so it
        /// needs no save field of its own. **1 leaves the standing cadence exactly as it was.**
        /// </summary>
        public float ForeignPolicyCadenceMultiplier { get; set; } = 1f;

        /// <summary>The most recent turn's fiscal breakdown for a country, or null if no turn has been advanced yet.</summary>
        public FiscalTurnReport GetLastFiscalReport(CountryId countryId)
        {
            return _lastFiscalReports.TryGetValue(countryId, out FiscalTurnReport report) ? report : null;
        }

        /// <summary>
        /// Step 2: the wage-growth gap the daily identity is consuming THIS period - the period-open
        /// stance (the fifth fixed reference), not the live gap. This is the number the trace panel's
        /// single-book confidence line explains, because it is the number the economy actually used.
        /// 0 before the first period exists (factor 1 - the honest nothing).
        /// </summary>
        public float GetWageGrowthGapAtPeriodOpen(CountryId countryId)
        {
            return _fiscalPeriods.TryGetValue(countryId, out FiscalPeriod period) ? period.WageGrowthGapAtPeriodOpen : 0f;
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
            float approvalBeforeOption = country.State.ApprovalRating;
            // Step 2's third section: F1's BudgetImpact reaches the debt stock through this option
            // (ApplyOneTimeBudgetImpact) - observed here, beside the approval observation, as a
            // dated Class B event on the debt ledger. Zero impacts are skipped inside RecordEvent.
            float debtBeforeOption = country.State.GovernmentDebt;
            CabinetSystem.ApplyDecisionOption(country, chosenOption);
            ApprovalLedgerRecorder.RecordEvent(country, CurrentDate, $"Cabinet: {chosenOption.Label}", country.State.ApprovalRating - approvalBeforeOption);
            DebtLedgerRecorder.RecordEvent(country, CurrentDate, $"Cabinet: {chosenOption.Label}", debtBeforeOption, country.State.GovernmentDebt);

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

            ForeignPolicyMeeting meeting = ForeignPolicySystem.TryRollMeeting(ForeignPolicyCadenceMultiplier);
            if (meeting != null)
            {
                _pendingForeignPolicyMeetingByCountry[countryId] = meeting;
            }
        }

        /// <summary>Applies the player's chosen response to the pending foreign policy meeting and clears it - called once per response, from GameController.</summary>
        public void ResolveForeignPolicyMeeting(CountryId countryId, ForeignPolicyMeetingOption chosenOption)
        {
            Country country = _world.GetCountry(countryId);
            float approvalBeforeOption = country.State.ApprovalRating;
            // Step 2's third section: the meeting option's BudgetImpact is F1's second writer -
            // observed on the debt ledger exactly as the cabinet option is.
            float debtBeforeOption = country.State.GovernmentDebt;
            ForeignPolicySystem.ApplyMeetingOption(country, chosenOption);
            ApprovalLedgerRecorder.RecordEvent(country, CurrentDate, $"Foreign policy: {chosenOption.Label}", country.State.ApprovalRating - approvalBeforeOption);
            DebtLedgerRecorder.RecordEvent(country, CurrentDate, $"Foreign policy: {chosenOption.Label}", debtBeforeOption, country.State.GovernmentDebt);
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
            float direction = ParliamentSystem.GetBillDirection(country, bill);
            bool passed = ParliamentSystem.WouldBillPass(country, direction);
            ParliamentSystem.RecordDivision(country, "Annual budget bill", direction, passed, CurrentDate);
            float approvalBeforeBill = country.State.ApprovalRating;
            ParliamentSystem.ApplyBillResult(country, bill, passed, ApplyBudgetBillSpendingAndSwf);
            ApprovalLedgerRecorder.RecordEvent(country, CurrentDate, passed ? "Budget bill passed (tax hike)" : "Budget bill failed", country.State.ApprovalRating - approvalBeforeBill);
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

                float direction = ParliamentSystem.GetTaxProgramBillDirection(country, bill);
                bool passed = ParliamentSystem.WouldBillPass(country, direction);
                ParliamentSystem.RecordDivision(country, $"{(bill.IsAdd ? "Implement" : "Remove")} {bill.Type}", direction, passed, CurrentDate);
                float approvalBeforeTaxBill = country.State.ApprovalRating;
                ParliamentSystem.ApplyTaxProgramBillResult(country, bill, passed);
                ApprovalLedgerRecorder.RecordEvent(country, CurrentDate, $"{(bill.IsAdd ? "Implement" : "Remove")} {bill.Type} bill {(passed ? "passed" : "failed")}", country.State.ApprovalRating - approvalBeforeTaxBill);
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

                float direction = ParliamentSystem.GetWelfareProgramBillDirection(country, bill);
                bool passed = ParliamentSystem.WouldBillPass(country, direction);
                ParliamentSystem.RecordDivision(country, $"{(bill.IsAdd ? "Implement" : "Remove")} {bill.Type}", direction, passed, CurrentDate);
                float approvalBeforeWelfareBill = country.State.ApprovalRating;
                ParliamentSystem.ApplyWelfareProgramBillResult(country, bill, passed);
                ApprovalLedgerRecorder.RecordEvent(country, CurrentDate, $"{(bill.IsAdd ? "Implement" : "Remove")} {bill.Type} bill {(passed ? "passed" : "failed")}", country.State.ApprovalRating - approvalBeforeWelfareBill);
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
            float direction = ParliamentSystem.GetLaborBillDirection(country, bill);
            bool passed = ParliamentSystem.WouldBillPass(country, direction);
            ParliamentSystem.RecordDivision(country, "Labor Market bill", direction, passed, CurrentDate);
            float approvalBeforeLaborBill = country.State.ApprovalRating;
            ParliamentSystem.ApplyLaborBillResult(country, bill, passed, ApplyLaborBillEffects);
            ApprovalLedgerRecorder.RecordEvent(country, CurrentDate, passed ? "Labor Market bill passed" : "Labor Market bill failed", country.State.ApprovalRating - approvalBeforeLaborBill);
            _pendingLaborBillByCountry.Remove(countryId);
        }

        /// <summary>The Labor Market bill's apply delegate - REWRITTEN for the coexistence ruling
        /// (pass 3, 2026-08-26, "keeps sliders"): a passed bill now sets the six STATUTORY BASE
        /// fields (absolute targets, the same clamp bounds the old direct path applied - the
        /// bill's own book), then recomposes the effective dials through
        /// RecomputeLaborDialsFromEnactedLaws so enacted labor laws' delta offsets (the other
        /// book) stack on top instead of being stomped by the bill - and vice versa. With no labor
        /// laws enacted the recompute writes exactly the clamped bill values, identical to the old
        /// direct path. The -1 "no change" sentinel discipline and the no-statutory-minimum-wage
        /// no-op (Sweden/Italy) both carry over from the appliers this used to call directly.</summary>
        private void ApplyLaborBillEffects(Country country, LaborPolicyBill bill)
        {
            if (country.MinimumWageImplemented && bill.MinimumWage >= 0f)
            {
                country.MinimumWagePercentOfMedianBase = Mathf.Clamp(bill.MinimumWage, MinMinimumWagePercent, MaxMinimumWagePercent);
            }

            if (bill.PaidFamilyLeaveWeeks >= 0f)
            {
                country.PaidFamilyLeaveWeeksBase = Mathf.Clamp(bill.PaidFamilyLeaveWeeks, MinPaidFamilyLeaveWeeks, MaxPaidFamilyLeaveWeeks);
            }

            if (bill.OvertimeRegulation >= 0f)
            {
                country.OvertimeRegulationBase = Mathf.Clamp(bill.OvertimeRegulation, MinLaborDialLevel, MaxLaborDialLevel);
            }

            if (bill.RetrainingProgram >= 0f)
            {
                country.RetrainingProgramBase = Mathf.Clamp(bill.RetrainingProgram, MinLaborDialLevel, MaxLaborDialLevel);
            }

            if (bill.FamilyPolicy >= 0f)
            {
                country.FamilyPolicyBase = Mathf.Clamp(bill.FamilyPolicy, MinPolicyDialLevel, MaxPolicyDialLevel);
            }

            if (bill.ImmigrationPolicy >= 0f)
            {
                country.ImmigrationPolicyBase = Mathf.Clamp(bill.ImmigrationPolicy, MinPolicyDialLevel, MaxPolicyDialLevel);
            }

            RecomputeLaborDialsFromEnactedLaws(country);
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
            float direction = ParliamentSystem.GetCrimeJusticeBillDirection(country, bill);
            bool passed = ParliamentSystem.WouldBillPass(country, direction);
            ParliamentSystem.RecordDivision(country, "Crime & Justice bill", direction, passed, CurrentDate);
            float approvalBeforeCrimeBill = country.State.ApprovalRating;
            ParliamentSystem.ApplyCrimeJusticeBillResult(country, bill, passed, ApplyCrimeJusticeBillEffects);
            ApprovalLedgerRecorder.RecordEvent(country, CurrentDate, passed ? "Crime & Justice bill passed" : "Crime & Justice bill failed", country.State.ApprovalRating - approvalBeforeCrimeBill);
            _pendingCrimeJusticeBillByCountry.Remove(countryId);
        }

        /// <summary>Every LawBill currently pending for this country, keyed by LawId, or an empty collection if none - see _pendingLawBillsByCountry's own doc comment.</summary>
        public IReadOnlyDictionary<string, LawBill> GetPendingLawBills(CountryId countryId)
        {
            return _pendingLawBillsByCountry.TryGetValue(countryId, out var pending) ? pending : EmptyLawBills;
        }

        private static readonly Dictionary<string, LawBill> EmptyLawBills = new Dictionary<string, LawBill>();

        /// <summary>The pending bill for one specific law, or null if none is currently before Parliament for it.</summary>
        public LawBill GetPendingLawBill(CountryId countryId, string lawId)
        {
            return _pendingLawBillsByCountry.TryGetValue(countryId, out var pending) && pending.TryGetValue(lawId, out LawBill bill) ? bill : null;
        }

        /// <summary>Submits a new law bill (enact or repeal, per bill.IsRepeal) - a no-op (returns false) if one is already pending for this SAME LawId. Mirrors IntroduceTaxProgramBill's own pattern exactly.</summary>
        public bool IntroduceLawBill(CountryId countryId, LawBill bill)
        {
            if (!_pendingLawBillsByCountry.TryGetValue(countryId, out var pending))
            {
                pending = new Dictionary<string, LawBill>();
                _pendingLawBillsByCountry[countryId] = pending;
            }

            if (pending.ContainsKey(bill.LawId))
            {
                return false;
            }

            bill.DaysRemaining = ParliamentSystem.BillDurationDays;
            pending[bill.LawId] = bill;
            return true;
        }

        /// <summary>Counts down every pending law bill for this country by one day, resolving any that reach 0 - mirrors AdvanceTaxProgramBillsDay's own pattern exactly.</summary>
        public void AdvanceLawBillsDay(CountryId countryId)
        {
            if (!_pendingLawBillsByCountry.TryGetValue(countryId, out var pending) || pending.Count == 0)
            {
                return;
            }

            Country country = _world.GetCountry(countryId);
            var resolved = new List<string>();
            foreach (LawBill bill in pending.Values)
            {
                bill.DaysRemaining--;
                if (bill.DaysRemaining > 0)
                {
                    continue;
                }

                LawDefinition law = LawCatalog.GetById(bill.LawId);
                string lawName = law != null ? law.Name : bill.LawId;
                float direction = ParliamentSystem.GetLawBillDirection(country, bill);
                bool passed = ParliamentSystem.WouldBillPass(country, direction);
                ParliamentSystem.RecordDivision(country, $"{(bill.IsRepeal ? "Repeal" : "Enact")}: {lawName}", direction, passed, CurrentDate);
                // Found by the fiscal-ledger pass's own bar (2026-08-25), pre-existing since the MVP
                // slice: this was the ONE bill type resolving OUTSIDE the approval ledger's
                // observation sites - a failed vote's BillFailedApprovalCost and a passed law's
                // EnactmentApprovalCost both moved approval with no ledger entry, so the round-trip
                // harness's two coverage law bills (both failing in a no-op world, 2 x 1.5 = the
                // exact -3.0 the audit reported) had been failing the approval self-audit in every
                // scenario since 08-24 - unnoticed, because that harness's own verdict line does not
                // grep ATTRIB. Observed here exactly as the other eight bill types are.
                float approvalBeforeLawResult = country.State.ApprovalRating;
                ParliamentSystem.ApplyLawBillResult(country, bill, passed, ApplyLawBillEffects);
                ApprovalLedgerRecorder.RecordEvent(country, CurrentDate,
                    $"{(bill.IsRepeal ? "Repeal" : "Enact")}: {lawName} ({(passed ? "passed" : "failed")})",
                    country.State.ApprovalRating - approvalBeforeLawResult);
                resolved.Add(bill.LawId);
            }

            foreach (string lawId in resolved)
            {
                pending.Remove(lawId);
            }
        }

        /// <summary>
        /// Applies an enacted (or repealed) law's dial deltas by building a throwaway
        /// PolicyDecision and reusing the EXISTING ApplyCrimePolicyChanges/
        /// ApplyCrimeJusticeDeeperChanges - the same "reuse the plumbing" pattern
        /// ApplyCrimeJusticeBillEffects/ApplyLaborBillEffects already establish, applied to a law's
        /// DELTA instead of a slider's absolute submitted value. Each target is pre-clamped to
        /// [MinPolicyDialLevel, MaxPolicyDialLevel] HERE, before being written into the
        /// PolicyDecision - not left to ApplyCrimePolicyChanges' own clamp - because a repeal
        /// subtracting a delta from a low current value can compute a raw negative number, and
        /// PolicyDecision's own "-1 sentinel means no change" convention would silently swallow it
        /// as a no-op if it were ever negative before this clamp ran.
        ///
        /// Enact appends an EnactedLaw record (Country.EnactedLaws) and charges the law's own
        /// EnactmentApprovalCost once, on passage. Repeal removes the record and applies the SAME
        /// six deltas with the opposite sign - laws apply as NUDGES (deltas), not absolute
        /// overrides, specifically so this composes: enacting two laws that touch the same dial
        /// stacks both effects, and repealing either one subtracts back out only its own
        /// contribution, decomposable to zero net effect when nothing else has touched the dial in
        /// between (clamp saturation is the one honest exception - the same caveat every clamped
        /// dial in this codebase already carries).
        /// </summary>
        private void ApplyLawBillEffects(Country country, LawBill bill)
        {
            LawDefinition law = LawCatalog.GetById(bill.LawId);
            if (law == null)
            {
                return;
            }

            // Defensive idempotency guard, not reachable through the real UI today (DrawLawCard only
            // ever offers "Enact" while !enacted and "Repeal" while enacted, and IntroduceLawBill
            // already refuses a second bill for the same LawId while one is pending) - but cheap, and
            // it closes a real latent double-application class: without it, enacting an
            // already-enacted law would add a duplicate EnactedLaws entry; repealing a law that isn't
            // enacted would have nothing to remove. Both are now unconditional no-ops instead - the
            // same "idempotent, not merely rejected" spirit IntroduceLawBill already applies one
            // layer up.
            bool alreadyEnacted = country.EnactedLaws.Exists(e => e.LawId == bill.LawId);
            if (bill.IsRepeal == !alreadyEnacted)
            {
                return;
            }

            if (bill.IsRepeal)
            {
                country.EnactedLaws.RemoveAll(e => e.LawId == bill.LawId);
            }
            else
            {
                country.EnactedLaws.Add(new EnactedLaw { LawId = bill.LawId, EnactedOn = CurrentDate });
                country.State.ApprovalRating = Mathf.Clamp(country.State.ApprovalRating - law.EnactmentApprovalCost, 0f, 100f);
            }

            // Both categories' recomputes run unconditionally (pass 3): each is idempotent and a
            // law's foreign-category deltas are 0f defaults, so the wrong-category recompute is an
            // exact no-op - cheaper to prove than a category dispatch that could silently skip a
            // mixed future law.
            RecomputeCrimeJusticeDialsFromEnactedLaws(country);
            RecomputeLaborDialsFromEnactedLaws(country);
        }

        /// <summary>
        /// Content-marathon finding (2026-08-25): the dial each of the six Crime &amp; Justice fields
        /// shows is recomputed FRESH from the current Country.EnactedLaws set every time it changes -
        /// never mutated incrementally by adding one law's signed delta to whatever the dial
        /// currently reads. The difference matters specifically at the clamp boundary.
        ///
        /// The FIRST version of this method did the incremental thing (this file's own earlier
        /// doc comments called clamp saturation "the one honest exception" to decomposability, which
        /// was the right instinct but understated the actual failure): with enough laws pushing
        /// SentencingSeverity past 100, the clamp silently absorbs the overshoot, and each individual
        /// law's OWN delta gets baked against whatever the CLAMPED prior value happened to be rather
        /// than the true unclamped total - repealing the same laws in ANY order then subtracts each
        /// nominal delta from a value that already "spent" some of that delta's effect on the clamp,
        /// landing measurably below the pre-enactment baseline rather than back on it. A real,
        /// realistic-scale composition test (ten laws, several of them touching SentencingSeverity at
        /// MAJOR/SWEEPING magnitude - not a contrived case; five or six of this category's now-twenty
        /// Sentencing-touching laws reach the ceiling on their own) measured this directly: full
        /// repeal of a set that had driven SentencingSeverity to its 100 ceiling landed at 29.0000, not
        /// 50.0000 - the ceiling had silently eaten 21 points nothing gave back.
        ///
        /// The fix treats Country.EnactedLaws as the sole source of truth and every dial as a PURE,
        /// STATELESS function of it: sum every enacted law's delta on a dial (from the seeded 50
        /// baseline - the same "laws are now the sole driver of these six dials" ruling the sliders'
        /// read-only conversion already established, restated as a computation rather than a policy),
        /// clamp exactly ONCE at the end, and set that as the dial. Enact and repeal both just change
        /// which laws are in the set, then call this - correct for ANY history of enactments and
        /// repeals in ANY order, not merely the one sequence a hand-written test happened to check,
        /// because there is no accumulated clamped state left to disagree with a fresh recomputation.
        /// An EnactedLaws entry citing a law no longer in LawCatalog (a hypothetical stale save) is
        /// skipped rather than crashing - the same "missing entry, not a crash" idiom LawCatalog.GetById
        /// itself already documents.
        /// </summary>
        private void RecomputeCrimeJusticeDialsFromEnactedLaws(Country country)
        {
            const float baseline = 50f;
            float police = baseline, sentencing = baseline, bail = baseline;
            float drug = baseline, judicial = baseline, border = baseline;

            foreach (EnactedLaw enacted in country.EnactedLaws)
            {
                LawDefinition law = LawCatalog.GetById(enacted.LawId);
                if (law == null)
                {
                    continue;
                }

                police += law.PoliceFundingDelta;
                sentencing += law.SentencingSeverityDelta;
                bail += law.BailReformDelta;
                drug += law.DrugPolicyDelta;
                judicial += law.JudicialFundingDelta;
                border += law.BorderEnforcementDelta;
            }

            var recomputed = new PolicyDecision
            {
                PoliceFundingOverride = Mathf.Clamp(police, MinPolicyDialLevel, MaxPolicyDialLevel),
                SentencingSeverityOverride = Mathf.Clamp(sentencing, MinPolicyDialLevel, MaxPolicyDialLevel),
                BailReformOverride = Mathf.Clamp(bail, MinPolicyDialLevel, MaxPolicyDialLevel),
                DrugPolicyOverride = Mathf.Clamp(drug, MinPolicyDialLevel, MaxPolicyDialLevel),
                JudicialFundingOverride = Mathf.Clamp(judicial, MinPolicyDialLevel, MaxPolicyDialLevel),
                BorderEnforcementOverride = Mathf.Clamp(border, MinPolicyDialLevel, MaxPolicyDialLevel)
            };
            ApplyCrimePolicyChanges(country, recomputed);
            ApplyCrimeJusticeDeeperChanges(country, recomputed);
        }

        /// <summary>
        /// THE LABOR RECOMPUTE (pass 3, coexistence ruling 2026-08-26): the labor sibling of
        /// RecomputeCrimeJusticeDialsFromEnactedLaws, generalized in exactly two ways (the
        /// pass-3 generalization verdict records both): (1) PER-COUNTRY BASELINES - each
        /// accumulator starts at the country's own bill-owned STATUTORY BASE field
        /// (Country.*Base: Kaitz points, weeks, or dial points), not the uniform 50, because two
        /// labor dials are real-unit dials with per-country seeds and, under coexistence, the
        /// base itself is player-legislated; (2) TWO WRITERS, ONE COMPOSITION - LaborPolicyBill
        /// sets base (ApplyLaborBillEffects), enacted laws contribute a pure delta sum on top,
        /// and this method is the ONLY writer of the effective dials, clamping ONCE at
        /// composition (the 555f4cc lesson restated: no clamped state ever persists into either
        /// component, so any history of bills, enactments and repeals in any order lands exactly
        /// where a fresh recomputation says - and full repeal lands exactly on base). Funnels
        /// through the existing clamp-owning appliers via a throwaway PolicyDecision;
        /// ApplyMinimumWageChange's own !MinimumWageImplemented no-op keeps Sweden/Italy's
        /// minimum-wage law deltas honestly inert. A stale EnactedLaws entry is skipped (the
        /// GetById null contract).
        /// </summary>
        private void RecomputeLaborDialsFromEnactedLaws(Country country)
        {
            float minimumWage = country.MinimumWagePercentOfMedianBase;
            float paidLeave = country.PaidFamilyLeaveWeeksBase;
            float overtime = country.OvertimeRegulationBase;
            float retraining = country.RetrainingProgramBase;
            float family = country.FamilyPolicyBase;
            float immigration = country.ImmigrationPolicyBase;

            foreach (EnactedLaw enacted in country.EnactedLaws)
            {
                LawDefinition law = LawCatalog.GetById(enacted.LawId);
                if (law == null)
                {
                    continue;
                }

                minimumWage += law.MinimumWageDelta;
                paidLeave += law.PaidFamilyLeaveWeeksDelta;
                overtime += law.OvertimeRegulationDelta;
                retraining += law.RetrainingProgramDelta;
                family += law.FamilyPolicyDelta;
                immigration += law.ImmigrationPolicyDelta;
            }

            var recomputed = new PolicyDecision
            {
                MinimumWageOverride = Mathf.Clamp(minimumWage, MinMinimumWagePercent, MaxMinimumWagePercent),
                PaidFamilyLeaveWeeksOverride = Mathf.Clamp(paidLeave, MinPaidFamilyLeaveWeeks, MaxPaidFamilyLeaveWeeks),
                OvertimeRegulationOverride = Mathf.Clamp(overtime, MinLaborDialLevel, MaxLaborDialLevel),
                RetrainingProgramOverride = Mathf.Clamp(retraining, MinLaborDialLevel, MaxLaborDialLevel),
                FamilyPolicyOverride = Mathf.Clamp(family, MinPolicyDialLevel, MaxPolicyDialLevel),
                ImmigrationPolicyOverride = Mathf.Clamp(immigration, MinPolicyDialLevel, MaxPolicyDialLevel)
            };
            ApplyMinimumWageChange(country, recomputed);
            ApplyLaborPolicyChanges(country, recomputed);
            ApplyDemographicPolicyChanges(country, recomputed);
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
            float direction = ParliamentSystem.GetSectorBillDirection(country, bill);
            bool passed = ParliamentSystem.WouldBillPass(country, direction);
            ParliamentSystem.RecordDivision(country, "Economic Sectors bill", direction, passed, CurrentDate);
            float approvalBeforeSectorBill = country.State.ApprovalRating;
            ParliamentSystem.ApplySectorBillResult(country, bill, passed, ApplySectorBillEffects);
            ApprovalLedgerRecorder.RecordEvent(country, CurrentDate, passed ? "Economic Sectors bill passed" : "Economic Sectors bill failed", country.State.ApprovalRating - approvalBeforeSectorBill);
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

        /// <summary>The pending SWF emergency drawdown bill for this country, or null if none is before Parliament.</summary>
        public SwfDrawdownBill GetPendingSwfDrawdownBill(CountryId countryId)
        {
            return _pendingSwfDrawdownBillByCountry.TryGetValue(countryId, out SwfDrawdownBill bill) ? bill : null;
        }

        /// <summary>
        /// See IntroduceLaborBill's own doc comment - identical pattern, with one extra guard: a country
        /// with no fund has nothing to draw down, so the bill cannot be introduced rather than passing
        /// and quietly doing nothing. A vote that resolves to no effect is worse than an unavailable
        /// control, because the player spends the wait learning nothing.
        /// </summary>
        public bool IntroduceSwfDrawdownBill(CountryId countryId, SwfDrawdownBill bill)
        {
            if (_pendingSwfDrawdownBillByCountry.ContainsKey(countryId))
            {
                return false;
            }

            Country guardCountry = _world.GetCountry(countryId);
            if (guardCountry == null || guardCountry.SovereignWealthFund == null)
            {
                return false;
            }

            bill.DaysRemaining = ParliamentSystem.BillDurationDays;
            _pendingSwfDrawdownBillByCountry[countryId] = bill;
            return true;
        }

        /// <summary>See AdvanceLaborBillDay's own doc comment - identical pattern.</summary>
        public void AdvanceSwfDrawdownBillDay(CountryId countryId)
        {
            if (!_pendingSwfDrawdownBillByCountry.TryGetValue(countryId, out SwfDrawdownBill bill))
            {
                return;
            }

            bill.DaysRemaining--;
            if (bill.DaysRemaining > 0)
            {
                return;
            }

            Country country = _world.GetCountry(countryId);
            float direction = ParliamentSystem.GetSwfDrawdownBillDirection(country, bill);
            bool passed = ParliamentSystem.WouldBillPass(country, direction);
            ParliamentSystem.RecordDivision(country, $"SWF emergency drawdown - {bill.WithdrawalPercentOfGdp:F1}% of GDP", direction, passed, CurrentDate);
            float approvalBeforeSwfBill = country.State.ApprovalRating;
            // Pass 5 (2026-08-26): the drawdown is F1's THIRD writer, found by the retirement sweep -
            // it now reaches the stock through ApplyOneTimeBudgetImpact, so the debt ledger observes
            // it here exactly as the cabinet and foreign-policy sites observe theirs.
            float debtBeforeSwfBill = country.State.GovernmentDebt;
            ParliamentSystem.ApplySwfDrawdownBillResult(country, bill, passed, ApplySwfDrawdownBillEffects);
            ApprovalLedgerRecorder.RecordEvent(country, CurrentDate, passed ? "SWF drawdown bill passed" : "SWF drawdown bill failed", country.State.ApprovalRating - approvalBeforeSwfBill);
            DebtLedgerRecorder.RecordEvent(country, CurrentDate, "SWF emergency drawdown", debtBeforeSwfBill, country.State.GovernmentDebt);
            _pendingSwfDrawdownBillByCountry.Remove(countryId);
        }

        /// <summary>
        /// The drawdown's apply delegate: a ONE-OFF transfer out of the fund into the budget, unlike
        /// every other SWF term, which sets a standing rate.
        ///
        /// **Clamped to what the fund actually holds.** A withdrawal larger than the fund cannot be
        /// honoured, and letting `TotalAssets` go negative would invent money and hand the budget a
        /// credit that never existed - the same class of error as the negative-debt interest inversion
        /// caught when the debt floor came off. The bill is not rejected for asking too much; it delivers
        /// what is there, which is what an emergency drawdown of a depleted fund really produces.
        ///
        /// ⚠ CORRECTED, pass 5 (2026-08-26). This doc used to say the proceeds "land on Budget ... so
        /// the drawdown reaches debt the same way a surplus does" - and that was FALSE from the day it
        /// was written: nothing fiscal reads State.Budget (it is the cumulative display accumulator), so
        /// a passed drawdown destroyed fund assets and lowered no debt. The two-books defect F1 closed
        /// for interrupt impacts had a third writer, found by pass 5's retirement sweep. It is a one-time
        /// settlement, so it takes F1's own path: ApplyOneTimeBudgetImpact moves the stock and records
        /// the same entry in the accumulator, and the resolution site observes it on the debt ledger.
        /// </summary>
        private void ApplySwfDrawdownBillEffects(Country country, SwfDrawdownBill bill)
        {
            SovereignWealthFund fund = country.SovereignWealthFund;
            if (fund == null)
            {
                return;
            }

            float requested = country.State.GDP * bill.WithdrawalPercentOfGdp / 100f;
            float withdrawn = Mathf.Clamp(requested, 0f, fund.TotalAssets);

            fund.TotalAssets -= withdrawn;
            ApplyOneTimeBudgetImpact(country, withdrawn);
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
            float direction = ParliamentSystem.GetTradeBillDirection(country, bill, _world);
            bool passed = ParliamentSystem.WouldBillPass(country, direction);
            ParliamentSystem.RecordDivision(country, "Trade bill", direction, passed, CurrentDate);
            float approvalBeforeTradeBill = country.State.ApprovalRating;
            ParliamentSystem.ApplyTradeBillResult(country, bill, passed, ApplyTradeBillEffects);
            ApprovalLedgerRecorder.RecordEvent(country, CurrentDate, passed ? "Trade bill passed" : "Trade bill failed", country.State.ApprovalRating - approvalBeforeTradeBill);
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

        /// <summary>
        /// SAVE/LOAD (item 8): the explicit capture of every private pending structure - the
        /// reviewable surface the 2026-08-01 scoping asked for, so a future pending-bill type nobody
        /// wired in reads as an obvious omission in this method rather than silently half-persisting.
        /// Containers are copied one level deep (fresh dictionaries/lists holding the same bill
        /// instances) so the returned state object cannot alias this manager's live collections;
        /// the bills themselves are snapshotted by the serializer immediately after.
        /// </summary>
        public Persistence.SimulationPendingState CaptureSaveState()
        {
            var state = new Persistence.SimulationPendingState
            {
                FiscalPeriods = new Dictionary<CountryId, FiscalPeriod>(_fiscalPeriods),
                PendingBudgetProcess = new List<CountryId>(_pendingBudgetProcessByCountry),
                PendingBudgetBills = new Dictionary<CountryId, BudgetBill>(_pendingBudgetBillByCountry),
                PendingLaborBills = new Dictionary<CountryId, LaborPolicyBill>(_pendingLaborBillByCountry),
                PendingCrimeJusticeBills = new Dictionary<CountryId, CrimeJusticePolicyBill>(_pendingCrimeJusticeBillByCountry),
                PendingSectorBills = new Dictionary<CountryId, SectorPolicyBill>(_pendingSectorBillByCountry),
                PendingTradeBills = new Dictionary<CountryId, TradePolicyBill>(_pendingTradeBillByCountry),
                PendingSwfDrawdownBills = new Dictionary<CountryId, SwfDrawdownBill>(_pendingSwfDrawdownBillByCountry),
                PendingForeignPolicyMeetings = new Dictionary<CountryId, ForeignPolicyMeeting>(_pendingForeignPolicyMeetingByCountry),
                LastFiscalReports = new Dictionary<CountryId, FiscalTurnReport>(_lastFiscalReports),
                LastEvents = new Dictionary<CountryId, EconomicEvent>(_lastEventsByCountry)
            };

            foreach (KeyValuePair<CountryId, Dictionary<TaxType, TaxProgramBill>> pair in _pendingTaxProgramBillsByCountry)
            {
                state.PendingTaxProgramBills[pair.Key] = new Dictionary<TaxType, TaxProgramBill>(pair.Value);
            }

            foreach (KeyValuePair<CountryId, Dictionary<WelfareProgramType, WelfareProgramBill>> pair in _pendingWelfareProgramBillsByCountry)
            {
                state.PendingWelfareProgramBills[pair.Key] = new Dictionary<WelfareProgramType, WelfareProgramBill>(pair.Value);
            }

            foreach (KeyValuePair<CountryId, Dictionary<string, LawBill>> pair in _pendingLawBillsByCountry)
            {
                state.PendingLawBills[pair.Key] = new Dictionary<string, LawBill>(pair.Value);
            }

            foreach (KeyValuePair<CountryId, List<(CabinetPortfolio Portfolio, CabinetDecision Decision)>> pair in _pendingCabinetDecisionsByCountry)
            {
                var records = new List<Persistence.PendingCabinetDecisionRecord>(pair.Value.Count);
                foreach ((CabinetPortfolio portfolio, CabinetDecision decision) in pair.Value)
                {
                    records.Add(new Persistence.PendingCabinetDecisionRecord { Portfolio = portfolio, Decision = decision });
                }

                state.PendingCabinetDecisions[pair.Key] = records;
            }

            return state;
        }

        /// <summary>
        /// SAVE/LOAD (item 8): the restore half. Adopts the restored world (through SetWorld, whose
        /// published-history seeding is idempotent by that method's own contract), sets the clock
        /// through the private setters, then rebuilds every pending structure from the state object.
        /// Every section tolerates null - a save from before a structure existed restores it empty,
        /// the same forward-tolerance MissingMemberHandling gives the serialized layer. Callers do
        /// not call this directly in normal play: SaveGameService.RestoreInto pairs it with the RNG
        /// restore so the two cannot drift apart across call sites.
        /// </summary>
        public void RestoreSaveState(World world, int currentTurn, System.DateTime currentDate, Persistence.SimulationPendingState state)
        {
            SetWorld(world);
            CurrentTurn = currentTurn;
            CurrentDate = currentDate;

            // OLD-SAVE BASE ADOPTION (pass 3, coexistence ruling 2026-08-26): a save written
            // before the statutory-base fields existed restores them at their -1 sentinel; adopt
            // the saved dial value as the base, which is exactly right because no pre-pass-3 save
            // can hold an enacted labor law (effective == base there by construction). A -1 never
            // survives into a post-pass-3 save.
            foreach (Country restored in world.Countries)
            {
                if (restored.MinimumWagePercentOfMedianBase < 0f) { restored.MinimumWagePercentOfMedianBase = restored.MinimumWagePercentOfMedian; }
                if (restored.PaidFamilyLeaveWeeksBase < 0f) { restored.PaidFamilyLeaveWeeksBase = restored.PaidFamilyLeaveWeeks; }
                if (restored.OvertimeRegulationBase < 0f) { restored.OvertimeRegulationBase = restored.OvertimeRegulationLevel; }
                if (restored.RetrainingProgramBase < 0f) { restored.RetrainingProgramBase = restored.RetrainingProgramLevel; }
                if (restored.FamilyPolicyBase < 0f) { restored.FamilyPolicyBase = restored.FamilyPolicyLevel; }
                if (restored.ImmigrationPolicyBase < 0f) { restored.ImmigrationPolicyBase = restored.ImmigrationPolicyLevel; }
            }

            _fiscalPeriods.Clear();
            _pendingBudgetProcessByCountry.Clear();
            _pendingBudgetBillByCountry.Clear();
            _pendingTaxProgramBillsByCountry.Clear();
            _pendingWelfareProgramBillsByCountry.Clear();
            _pendingLaborBillByCountry.Clear();
            _pendingCrimeJusticeBillByCountry.Clear();
            _pendingSectorBillByCountry.Clear();
            _pendingTradeBillByCountry.Clear();
            _pendingSwfDrawdownBillByCountry.Clear();
            _pendingLawBillsByCountry.Clear();
            _pendingForeignPolicyMeetingByCountry.Clear();
            _pendingCabinetDecisionsByCountry.Clear();
            _lastFiscalReports.Clear();
            _lastEventsByCountry.Clear();

            if (state == null)
            {
                return;
            }

            CopyInto(state.FiscalPeriods, _fiscalPeriods);
            if (state.PendingBudgetProcess != null)
            {
                foreach (CountryId id in state.PendingBudgetProcess)
                {
                    _pendingBudgetProcessByCountry.Add(id);
                }
            }

            CopyInto(state.PendingBudgetBills, _pendingBudgetBillByCountry);
            CopyInto(state.PendingLaborBills, _pendingLaborBillByCountry);
            CopyInto(state.PendingCrimeJusticeBills, _pendingCrimeJusticeBillByCountry);
            CopyInto(state.PendingSectorBills, _pendingSectorBillByCountry);
            CopyInto(state.PendingTradeBills, _pendingTradeBillByCountry);
            CopyInto(state.PendingSwfDrawdownBills, _pendingSwfDrawdownBillByCountry);
            CopyInto(state.PendingForeignPolicyMeetings, _pendingForeignPolicyMeetingByCountry);
            CopyInto(state.LastFiscalReports, _lastFiscalReports);
            CopyInto(state.LastEvents, _lastEventsByCountry);

            if (state.PendingTaxProgramBills != null)
            {
                foreach (KeyValuePair<CountryId, Dictionary<TaxType, TaxProgramBill>> pair in state.PendingTaxProgramBills)
                {
                    _pendingTaxProgramBillsByCountry[pair.Key] = new Dictionary<TaxType, TaxProgramBill>(pair.Value);
                }
            }

            if (state.PendingWelfareProgramBills != null)
            {
                foreach (KeyValuePair<CountryId, Dictionary<WelfareProgramType, WelfareProgramBill>> pair in state.PendingWelfareProgramBills)
                {
                    _pendingWelfareProgramBillsByCountry[pair.Key] = new Dictionary<WelfareProgramType, WelfareProgramBill>(pair.Value);
                }
            }

            if (state.PendingLawBills != null)
            {
                foreach (KeyValuePair<CountryId, Dictionary<string, LawBill>> pair in state.PendingLawBills)
                {
                    _pendingLawBillsByCountry[pair.Key] = new Dictionary<string, LawBill>(pair.Value);
                }
            }

            if (state.PendingCabinetDecisions != null)
            {
                foreach (KeyValuePair<CountryId, List<Persistence.PendingCabinetDecisionRecord>> pair in state.PendingCabinetDecisions)
                {
                    var pending = new List<(CabinetPortfolio Portfolio, CabinetDecision Decision)>(pair.Value.Count);
                    foreach (Persistence.PendingCabinetDecisionRecord record in pair.Value)
                    {
                        pending.Add((record.Portfolio, record.Decision));
                    }

                    _pendingCabinetDecisionsByCountry[pair.Key] = pending;
                }
            }
        }

        private static void CopyInto<TKey, TValue>(Dictionary<TKey, TValue> source, Dictionary<TKey, TValue> target)
        {
            if (source == null)
            {
                return;
            }

            foreach (KeyValuePair<TKey, TValue> pair in source)
            {
                target[pair.Key] = pair.Value;
            }
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

                // PHASE 4 FINDING (2026-08-16): History.Append lived HERE, once per turn, from Phase 0
                // until this pass - which meant the multi-resolution buckets built FOR daily data had
                // never received a daily offer, and Daily/Weekly/Monthly/Quarterly held identical
                // one-point-per-turn series through Phases 1-3's genuinely daily variation. The append
                // now runs in AdvanceDay (offered daily; each resolution's own gate accepts at its
                // cadence, exactly the design intent its class doc always stated). PreviewTurn's
                // phantom-point protection carries over unchanged: AdvanceDay never runs on the
                // preview clone at all - stronger isolation than the old once-per-turn site had.
            }

            CurrentTurn++;
        }

        /// <summary>
        /// Applies one country's domestic feedback rules for the turn, in place: fiscal policy,
        /// the national accounts identity (GDP), Okun's Law (unemployment), the Phillips Curve
        /// (inflation), and approval. <paramref name="tariffRevenue"/> was already collected (and
        /// already added to Budget) by TradeSystem earlier this same turn - it's threaded through
        /// only to record it on this turn's FiscalTurnReport, not applied again here.
        ///
        /// CONTINUOUS TIME PHASE 3: "fiscal policy" here no longer means moving money. The budget is
        /// RESOLVED at this boundary and then EXECUTED daily by AccrueDailyFiscalFlows over the 121 days
        /// that follow; what remains in this method is the resolution, the close-out of the period that
        /// just ended, and everything downstream of them that is still turn-shaped.
        /// </summary>
        private void ApplyDomesticPolicy(Country country, PolicyDecision decision, float tariffRevenue)
        {
            EconomyState state = country.State;

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
            // CONTINUOUS TIME PHASE 4: the demographic rates and population growth have already been
            // charged day by day in AdvanceDay - applying either again here would double-count a full
            // turn's worth on top of the daily steps (Phase 1's exact wording, same reason). The old
            // ordering contract this comment carried - "must run BEFORE ResolveSpendingForTurn, which
            // reads this turn's freshly-updated DependencyRatio" - still holds by construction:
            // AdvanceDay finishes the boundary day before AdvanceTurn runs, so the DependencyRatio
            // read below completed this period's accumulation. PreviewTurn keeps the turn forms as
            // their only remaining callers, correctly rather than as dead code.
            DetailedSpendingResult spendingResult = ResolveSpendingForTurn(country, decision);
            MacroSystem.ApplyCategorySpendingEffects(country, spendingResult.EffectiveDecision);
            // Phase 1: the DECAY and the sector reversion have already been charged day by day in
            // AdvanceDay. Only the discrete investment credit belongs here now - applying either of the
            // continuous flows again would double-count a full turn's worth on top of the daily steps.
            MacroSystem.ApplyInfrastructureInvestment(country, spendingResult.EffectiveDecision);
            MacroSystem.ApplySectorGrowthEffect(country);
            MacroSystem.ApplyWelfareProgramEffects(country);

            // CONTINUOUS TIME PHASE 3: the money already moved, day by day, in AccrueDailyFiscalFlows.
            // What is left at a boundary is what a boundary is actually for - CLOSING the period that
            // just ended and RESOLVING the plan the next one executes. No cash changes hands here, and
            // deliberately so: charging a period's flows here on top of 121 daily accruals is the
            // double-count Phase 1 already had to reason about for infrastructure investment.
            FiscalPeriod period = GetOrSeedFiscalPeriod(country);

            // Every flow below is the SUM of this period's 121 daily accruals, not a single step's
            // figure. BaselineGovernmentSpending/DiscretionarySpending are the decomposition of the plan
            // those days were spending - read BEFORE the re-plan below overwrites it - so their sum still
            // equals the government spending actually accrued, which is the property every existing
            // caller of this report relies on.
            //
            // Pass 5 (2026-08-26): TariffRevenue is no longer the exception. TradeSystem still computes
            // the figure once per boundary, but it is the coming period's PLAN, accrued daily inside
            // Revenue like every other flow - so the report shows the tariff portion that actually
            // accrued over the period that just closed, a true reading of the one real path. (Under the
            // pre-pass-5 books it was a turn figure booked to the accumulator alone.)
            _lastFiscalReports[country.Id] = new FiscalTurnReport
            {
                Revenue = period.AccruedRevenue,
                BaselineGovernmentSpending = period.PlannedBaselineGovernmentSpending,
                DiscretionarySpending = period.PlannedDiscretionarySpending,
                MandatorySpending = period.AccruedMandatorySpending,
                UnemploymentBenefitCost = period.AccruedUnemploymentBenefitCost,
                InterestOnDebt = period.AccruedInterestOnDebt,
                TariffRevenue = period.AccruedTariffRevenue,
                TariffPassThroughPp = period.AppliedTariffPassThroughPp,
                WelfareCost = period.AccruedWelfareCost,
                SwfContribution = period.AccruedSwfContribution,
                SwfReturns = period.AccruedSwfReturns,
                TotalSpending = period.AccruedTotalSpending,
                BudgetBalance = period.AccruedBudgetBalance
            };

            // Step 2's third section (2026-08-25): the debt ledger closes exactly where the
            // FiscalTurnReport does - the last daily slice has been observed (AdvanceDay finished
            // the boundary day before AdvanceTurn ran), the stock is the period's closing stock,
            // and the rate pair read here is the same pair the boundary day's slice was charged
            // at. The close runs the Σ(terms)+Σ(events)+clamp == observed-Δ self-audit (ATTRIB:
            // on red) and opens the next period's ledger at the post-boundary stock.
            DebtLedgerRecorder.CloseAtBoundary(country, CurrentDate, GetDebtIssuanceRate(country),
                country.EffectiveDebtInterestRate >= 0f ? country.EffectiveDebtInterestRate : GetDebtIssuanceRate(country));

            // CONTINUOUS TIME PHASE 5: "this turn's realized growth" is now the PERIOD's growth,
            // measured from the GDP the closing period opened at - by the time a boundary runs, the
            // days have already moved GDP, so the old top-of-method snapshot would read ~zero. The
            // guard covers a pre-Phase-5 save whose restored period carries no opening GDP.
            float actualGrowthRate = period.GdpAtPeriodOpen > 0f
                ? (state.GDP - period.GdpAtPeriodOpen) / Mathf.Max(period.GdpAtPeriodOpen, 1f) * 100f
                : 0f;

            // Pass 6: the closing period's tariff pass-through as it actually printed on the boundary
            // day, and the take it planned - both captured before the re-plan below overwrites the
            // period. The expectations step runs AFTER the re-plan and must look through the CLOSING
            // period's term, not the coming one's (the ordering trap, named).
            float closingAppliedTariffPassThroughPp = period.AppliedTariffPassThroughPp;
            float closingPlannedTariffRevenue = period.PlannedTariffRevenue;

            // Open the next period. The SWF return is drawn ONCE here and accrued daily - see
            // FiscalPeriod's doc comment for that decision and why daily draws were rejected. The draw
            // sits at the same point in ApplyDomesticPolicy the old ApplyReturns call did, so the
            // SovereignWealthFund random stream still advances exactly once per country per turn.
            period.ResetAccrual();
            period.PlannedGovernmentSpending = spendingResult.GovernmentSpending;
            period.PlannedMandatorySpending = spendingResult.MandatorySpending;
            period.PlannedBaselineGovernmentSpending = spendingResult.BaselineGovernmentSpending;
            period.PlannedDiscretionarySpending = spendingResult.DiscretionarySpendingChangeThisTurn;
            period.PlannedSwfReturn = country.SovereignWealthFund != null
                ? SovereignWealthFundSystem.DrawPeriodReturn(country.SovereignWealthFund)
                : 0f;
            // Pass 5: this boundary's tariff figure (rates and volumes as they stand after this
            // turn's tariff decisions resolved) is the coming period's tariff flow.
            // Pass 6: the CHANGE in that figure against the closing period's plan is the coming
            // period's price pass-through (TradeCosts.ImportPricePassThrough) - the tariff-weighted
            // import-price change, as inflation points for one year, over the GDP this period opens
            // at. Exactly 0f when no rate changed: the same pure sum on unchanged state. A branch, not
            // a multiply, so the wired-inert control is the old code to the bit.
            period.PlannedTariffPassThroughPp = TradeCosts.ImportPricePassThrough > 0f
                ? TradeCosts.ImportPricePassThrough * TradeCosts.PassThroughMeasurementScale
                    * 100f * (tariffRevenue - closingPlannedTariffRevenue) / Mathf.Max(state.GDP, 1f)
                : 0f;
            period.PlannedTariffRevenue = tariffRevenue;

            // Read AFTER 121 days of accrual have finished moving the debt stock, so the stance the next
            // period adopts responds to the debt the country actually ended this one with - the same
            // instant the turn form read it, for the same reason.
            period.PlannedFiscalReactionMultiplier = GetFiscalReactionMultiplier(country);
            period.GdpAtPeriodOpen = state.GDP;
            period.UnemploymentAtPeriodOpen = state.Unemployment;
            period.PotentialGdpAtPeriodOpen = state.PotentialGDP;
            // Q5: the gap the daily identity consumes now includes productivity's hoarding cycle,
            // computed from the unemployment this period is opening at - the same value recorded on
            // the line above, so the two anchors describe one instant.
            period.WageGrowthGapAtPeriodOpen = MacroSystem.RealWageGrowthGapPerTurnPercent(country,
                MacroSystem.ProductivityCycleGrowthPerTurnPercent(country, state.Unemployment));

            // CONTINUOUS TIME PHASE 5: the identity, trend growth, Okun, Phillips and expectations
            // have already been charged day by day in AdvanceDay - applying any of them again here
            // would double-count a full turn's worth on top of the daily steps (the standing Phase
            // 1/4 wording, for the last time: this was the final turn-stepped economic system).
            // PreviewTurn keeps ALL the turn forms as their only remaining callers - it models one
            // whole turn on a throwaway clone without advancing days, which is exactly what the turn
            // forms are. ApplySectorGrowthEffect stays at the boundary above deliberately: it
            // FINALIZES PotentialGrowthRate from sources Phases 1-3 already move daily, and a
            // finalization tied to the spending resolution is a boundary decision, not a flow -
            // PotentialGDP then compounds daily at that standing rate.
            // Phase 2/3: PovertyRate, LFPR, the three crime indices and prison population run DAILY
            // in AdvanceDay; the Round 3 ordering constraint moved with them and is preserved there.

            // PHASE 5: the ONE macro step that stays at the boundary, deliberately - adaptive
            // expectations anchor to the period's closing print, and every daily form measurably
            // fails the equivalence bar (see MacroSystem's Phase 5 block comment). Reads the
            // boundary day's inflation, exactly what the turn regime always read.
            // Pass 6: NET of the tariff pass-through that actually printed on that day - the closing
            // period's applied term, captured above before the re-plan - so a price-level wedge never
            // enters the rate expectations (see ApplyInflationExpectations). Named, never positional.
            MacroSystem.ApplyInflationExpectations(state, lookThroughPp: closingAppliedTariffPassThroughPp);

            // Step 2: the formula keeps its exact pre-ledger body (the observation gate measured
            // a one-ulp codegen shift when recording lived inside it); the recorder recomputes
            // the terms AFTER, under the boundary audit's twin-drift detector. EnsureAccruing
            // runs BEFORE the formula: a run's FIRST boundary lazy-creates the ledger, and a
            // ledger created after the formula would open at the post-formula value - the audit
            // caught exactly that (observed Δ 0 vs nonzero terms, 2026-08-18).
            float approvalBeforeFormula = country.State.ApprovalRating;
            ApprovalLedgerRecorder.EnsureAccruing(country, CurrentDate, approvalBeforeFormula);
            MacroSystem.ApplyApprovalRating(country, spendingResult.EffectiveDecision, actualGrowthRate, totalTaxHike, spendingResult.MandatorySpendingChangeThisTurn);
            MacroSystem.RecordApprovalAttribution(country, spendingResult.EffectiveDecision, actualGrowthRate, totalTaxHike, spendingResult.MandatorySpendingChangeThisTurn, CurrentDate, approvalBeforeFormula);

            EconomicEvent economicEvent = EventSystem.TryRollEvent();
            _lastEventsByCountry[country.Id] = economicEvent;
            // Step 2: observed, not recomputed - the ledger records the post-clamp delta the
            // event actually landed (which can be smaller than its face value at the [0,100]
            // edges). Zero deltas are skipped inside RecordEvent.
            float approvalBeforeEvent = country.State.ApprovalRating;
            EventSystem.ApplyEvent(country, economicEvent);
            if (economicEvent != null)
            {
                ApprovalLedgerRecorder.RecordEvent(country, CurrentDate, economicEvent.Name, country.State.ApprovalRating - approvalBeforeEvent);
            }

            // Step 2: the period closes AFTER the boundary event lands, so a boundary-day shock
            // belongs to the period the player just watched, never silently to the next one. The
            // close runs the Σ(events)+Σ(terms)+clamp == observed-Δ self-audit (ATTRIB: on red).
            ApprovalLedgerRecorder.CloseAtBoundary(country, CurrentDate);

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
            // Pass 5: the clone's tariff figure is threaded into its fiscal step below, exactly as the
            // real boundary plans it - the preview's NetBudgetImpact stays a true reading.
            float previewTariffRevenue = TradeSystem.ApplyTradeEffects(previewCountry, _world);
            // Pass 6: the price pass-through this turn's tariff decision would plan, against the take
            // the current period planned - the boundary's own expression (a preview seeds nothing:
            // StandingPlannedTariffRevenue reads the period with TryGetValue).
            float previewTariffPassThroughPp = TradeCosts.ImportPricePassThrough > 0f
                ? TradeCosts.ImportPricePassThrough * TradeCosts.PassThroughMeasurementScale
                    * 100f * (previewTariffRevenue - StandingPlannedTariffRevenue(countryId)) / Mathf.Max(gdpBeforeThisTurn, 1f)
                : 0f;

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
            // Phase 1: the preview deliberately keeps the TURN-level forms. It models one whole turn on a
            // throwaway clone WITHOUT advancing any days, so the daily methods would never be called on it
            // - the turn forms are exactly the 121-day aggregate the preview is trying to show. These are
            // now their only remaining callers, which is correct rather than dead code.
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
            float swfDraw = 0f;
            if (previewCountry.SovereignWealthFund != null)
            {
                previewCountry.SovereignWealthFund.TotalAssets += swfContribution;
                swfReturns = SovereignWealthFundSystem.GetAverageReturnEstimate(previewCountry.SovereignWealthFund);
                float maxSwfAssets = MaxSwfToGdpPercent / 100f * state.GDP;
                previewCountry.SovereignWealthFund.TotalAssets = Mathf.Clamp(previewCountry.SovereignWealthFund.TotalAssets, 0f, maxSwfAssets);

                // Mirrors AdvanceTurn exactly: the budget sees the structural DRAW, not the return. The
                // preview must model the same flow the turn will, or the estimate it shows is of a
                // mechanism the game no longer has.
                swfDraw = previewCountry.SovereignWealthFund.TotalAssets * SwfStructuralDrawPerTurnFraction();
                swfDraw = Mathf.Clamp(swfDraw, 0f, previewCountry.SovereignWealthFund.TotalAssets);
                previewCountry.SovereignWealthFund.TotalAssets -= swfDraw;
            }

            ApplyRevenueAndSpending(previewCountry, spendingResult.GovernmentSpending, spendingResult.MandatorySpending, unemploymentBenefitCost, interestOnDebt, welfareCost, swfContribution, swfDraw, previewTariffRevenue, out _, out _);

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
            float previewAppliedTariffPassThroughPp = MacroSystem.ApplyPhillipsCurveInflation(previewCountry, previewTariffPassThroughPp);
            MacroSystem.ApplyInflationExpectations(state, lookThroughPp: previewAppliedTariffPassThroughPp);
            MacroSystem.ApplyPovertyRate(previewCountry);
            MacroSystem.ApplyLaborForceParticipationRate(previewCountry);
            MacroSystem.ApplyOrganizedCrimeIndex(previewCountry);
            MacroSystem.ApplyCorruptionIndex(previewCountry);
            MacroSystem.ApplyCrimeIndex(previewCountry);
            MacroSystem.ApplyCrimeEffects(previewCountry);
            MacroSystem.ApplyPrisonPopulationRate(previewCountry);

            float previewApprovalBeforeFormula = state.ApprovalRating;
            MacroSystem.ApplyApprovalRating(previewCountry, spendingResult.EffectiveDecision, actualGrowthRate, totalTaxHike, spendingResult.MandatorySpendingChangeThisTurn);
            MacroSystem.RecordApprovalAttribution(previewCountry, spendingResult.EffectiveDecision, actualGrowthRate, totalTaxHike, spendingResult.MandatorySpendingChangeThisTurn, CurrentDate, previewApprovalBeforeFormula);

            return new PolicyPreview
            {
                // Step 2: the clone's own ledger, filled by the ApplyApprovalRating call above -
                // the preview-parity diagnostic compares these terms against the real boundary's,
                // which is what turns a clone-escape into a mismatch that names its term.
                ApprovalTerms = previewCountry.ApprovalLedgerAccruing,
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

        /// <summary>The tariff take the country's CURRENT fiscal period planned (what the next boundary's pass-through is measured against), read with TryGetValue so a preview or an estimate never seeds a period; before the first period exists, the seed take from the same pure function.</summary>
        private float StandingPlannedTariffRevenue(CountryId countryId)
        {
            return _fiscalPeriods.TryGetValue(countryId, out FiscalPeriod period)
                ? period.PlannedTariffRevenue
                : TradeSystem.ComputeTariffRevenue(_world.GetCountry(countryId), _world);
        }

        /// <summary>
        /// Pass 6 (2026-08-27): what a Trade bill DRAFT would do at the next boundary, for the Trade
        /// bill card - computed by the REAL functions on throwaway clones, never a hand sum (pass 5's
        /// own lesson on the Budget "Net" line): one clone at the standing rates, one with the bill
        /// applied through the same delegate a passed bill uses, both run through
        /// TradeSystem.ApplyTradeEffects (so the currency factor and the partners' mirrored rates are
        /// inside the figure) and ComputeTariffRevenue; the pass-through is the boundary's own
        /// expression against the take the current period planned. Drafts never reach PreviewTurn
        /// (BuildPlayerDecision carries no tariff terms), so this is the one surface a draft's cost can
        /// be read from. Side-effect-free: clones only, the real World untouched.
        /// </summary>
        public TradeBillEstimate EstimateTradeBill(CountryId countryId, TradePolicyBill bill)
        {
            Country real = _world.GetCountry(countryId);
            Country standing = ClonePreviewCountry(real);
            Country proposed = ClonePreviewCountry(real);
            ApplyTradeBillEffects(proposed, bill);

            float standingTake = TradeSystem.ApplyTradeEffects(standing, _world);
            float proposedTake = TradeSystem.ApplyTradeEffects(proposed, _world);
            return new TradeBillEstimate
            {
                Take = proposedTake,
                TakeDelta = proposedTake - standingTake,
                TradeBalanceDelta = proposed.State.TradeBalance - standing.State.TradeBalance,
                PassThroughPp = TradeCosts.ImportPricePassThrough > 0f
                    ? TradeCosts.ImportPricePassThrough * TradeCosts.PassThroughMeasurementScale
                        * 100f * (proposedTake - StandingPlannedTariffRevenue(countryId)) / Mathf.Max(real.State.GDP, 1f)
                    : 0f
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
                // Seed-spread ruling (2026-08-27): the welfare anchor rides the hand-list too (the
                // R4-1 Clone-escape class) - without it the clone's WelfareEffectDelta would measure
                // every seeded program as a fresh implementation.
                BaselineWelfarePrograms = ClonePreviewWelfarePrograms(country.BaselineWelfarePrograms),
                Sectors = ClonePreviewSectors(country.Sectors),
                InfrastructureAssets = ClonePreviewInfrastructureAssets(country.InfrastructureAssets),
                CollectionEfficiency = country.CollectionEfficiency,
                BaseDebtInterestRateOverride = country.BaseDebtInterestRateOverride,
                RiskPremiumSensitivity = country.RiskPremiumSensitivity,
                // R4: both maturity-lag fields ride the preview clone's hand-list - the R4-1
                // Clone-escape lesson applies to THIS list too (a missed field here silently
                // previews at the sentinel fallback, i.e. instant repricing).
                AverageDebtMaturityYears = country.AverageDebtMaturityYears,
                EffectiveDebtInterestRate = country.EffectiveDebtInterestRate,
                // Q1: BaselineGini joins the hand-list the day ApplyApprovalRating started reading
                // it - without this line the preview computes the Gini gap against the 30f field
                // default (a phantom -0.5/turn for the USA at its 39.5 seed). The R4-1
                // Clone-escape class, caught by the containment check BEFORE the bar this time.
                BaselineGini = country.BaselineGini,
                // Step 2: a FRESH ledger, never the real country's reference - ApplyApprovalRating
                // records into it on the clone, and sharing the reference would corrupt the real
                // period's attribution the moment a preview ran. LastPeriod stays null: a preview
                // has no history and nothing reads it. The open date mirrors the real accruing
                // ledger's (this method is static, so no CurrentDate here); only the TERMS matter
                // to the parity diagnostic either way.
                // Step 2's third section: the clone carries NO fiscal ledger at all. The preview
                // never runs the daily path, so nothing on the clone could record into one - but
                // a memberwise clone would share the real country's REFERENCE, and the hand-list's
                // whole lesson (the BaselineGini class) is that a shared reference is a latent
                // escape. Null on both, and the parity diagnostic asserts the real ledger is
                // untouched across a preview.
                FiscalLedgerAccruing = null,
                FiscalLedgerLastPeriod = null,
                ApprovalLedgerAccruing = new ApprovalAttribution
                {
                    PeriodOpenDate = country.ApprovalLedgerAccruing != null ? country.ApprovalLedgerAccruing.PeriodOpenDate : default,
                    ApprovalAtPeriodOpen = country.State.ApprovalRating
                },
                // Q3: the trend field rides too (its sentinel fallback happens to be exact here
                // - the property falls back to PotentialGrowthRate, copied above via the ctor -
                // but the hand-list carries it anyway: exact-by-fallback is a coincidence to a
                // future edit, exact-by-copy is not).
                ProductivityTrendGrowthRate = country.ProductivityTrendGrowthRate,
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
        /// this amount (see AccrueDailyFiscalFlows/PreviewTurn), so a drawdown correctly shrinks the fund by the
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
            // Seed-spread ruling (2026-08-27): live cost minus the cost of the portfolio AS SEEDED -
            // the sourced spending seeds already carry each country's real programs, so a program
            // implemented at seed books nothing here and only a player's deviation moves the budget
            // (a removal below the seed books NEGATIVE cost: spending below the sourced line). Bit-
            // identical to the pre-ruling sum while no country seeds a program (x - 0f == x).
            return WelfareCostOf(country.WelfarePrograms, country.State.GDP) - WelfareCostOf(country.BaselineWelfarePrograms, country.State.GDP);
        }

        private static float WelfareCostOf(List<WelfareProgram> programs, float gdp)
        {
            float cost = 0f;
            if (programs == null)
            {
                return cost;
            }

            foreach (WelfareProgram program in programs)
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
        /// defensive, so a future TaxLine accidentally created for it could never double-count the
        /// tariff flow, which enters ApplyRevenueAndSpending as its own term (pass 5: TradeSystem's
        /// figure, planned per period and accrued daily, outside CollectionEfficiency). See
        /// ApplyRevenueAndSpending for where CollectionEfficiency is applied to get the actual
        /// collected revenue.
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
                ApplyEnforcementCostPressure(country);
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

        /// <summary>
        /// THE COUPLINGS PASS (build-order item 2, terminal rulings 2026-08-26, "line-resident,
        /// feeds G"): enforcement costs money, and the money lands on REAL spending lines - never
        /// an abstract Budget delta, which would invent money outside the line structure the
        /// recalibration just made honest. Two cost targets, both NEUTRAL-ANCHORED (zero at dial
        /// 50 / prison rate at baseline, because the status-quo enforcement apparatus is already
        /// inside the recalibrated seed totals): the JUSTICE target = the police + judicial dial
        /// gaps at their ruled shares of GDP, PLUS the incarceration variable cost - the prison
        /// stock's gap above its own baseline at the ruled cost-per-inmate (the honest chain:
        /// sentencing -&gt; prison stock, on its ~4-year half-life -&gt; budget); the BORDER target =
        /// the border dial gap at its share. Line routing, perimeter-consistent per country:
        /// USA Justice + HomelandSecurity (CBP/ICE's real federal line); Sweden Justice (UO4
        /// rattsvasendet) + Migration (UO8); the four generics PublicServices for both (no finer
        /// line exists until their decomposition passes). All lines are Discretionary, so the
        /// cost flows into the national-accounts G term through the existing line sum - police
        /// and prisons are government PURCHASES, and this is the ruling's point.
        ///
        /// STATELESS TARGET ON A STATEFUL LINE: the two Applied* trackers on Country record the
        /// last applied dollar target, and each boundary applies only the DIFFERENCE - so the
        /// dial cost composes with the five existing line writers (growth, pressure, player
        /// changes) instead of overwriting them. ClampToSeedRange applies like every other line
        /// mutation; if it binds (the USA's small federal Justice line saturates at 3x seed under
        /// extreme SWEEPING-law stacks), the tracker still records the REQUESTED target, so the
        /// un-achieved remainder is honestly lost to the line's own bound - bounded and explained,
        /// never silently re-applied. Amount only, never SeedAmount, per the pressure methods'
        /// own reconciliation rule. Runs inside ResolveSpendingForTurn (boundary-resident: dials
        /// change only at boundaries via law composition, and the period plan idiom carries the
        /// cost through the daily accrual automatically). Old saves carry Applied* = 0 and
        /// self-correct at their first boundary (at neutral dials the target IS zero).
        /// </summary>
        private void ApplyEnforcementCostPressure(Country country)
        {
            float gdp = country.State.GDP;
            float justiceTarget = gdp / 100f * (
                    CrimeJusticeCouplings.PoliceFundingBudgetCostPercentOfGdpPerPoint * (country.PoliceFundingLevel - CrimeJusticeCouplings.NeutralDialLevel)
                  + CrimeJusticeCouplings.JudicialFundingBudgetCostPercentOfGdpPerPoint * (country.JudicialFundingLevel - CrimeJusticeCouplings.NeutralDialLevel))
                + gdp * CrimeJusticeCouplings.IncarcerationCostGdpPerCapitaPerInmate
                      * (country.State.PrisonPopulationRate - country.BaselinePrisonPopulationRate) / 100000f;
            float borderTarget = gdp / 100f * CrimeJusticeCouplings.BorderEnforcementBudgetCostPercentOfGdpPerPoint
                * (country.BorderEnforcementLevel - CrimeJusticeCouplings.NeutralDialLevel);

            SpendingLine justiceLine = FindSpendingLine(country, SpendingCategory.Justice)
                ?? FindSpendingLine(country, SpendingCategory.PublicServices);
            if (justiceLine != null)
            {
                justiceLine.Amount = ClampToSeedRange(justiceLine, justiceLine.Amount + (justiceTarget - country.AppliedJusticeEnforcementCost));
                country.AppliedJusticeEnforcementCost = justiceTarget;
            }

            SpendingLine borderLine = FindSpendingLine(country, SpendingCategory.HomelandSecurity)
                ?? FindSpendingLine(country, SpendingCategory.Migration)
                ?? FindSpendingLine(country, SpendingCategory.PublicServices);
            if (borderLine != null)
            {
                borderLine.Amount = ClampToSeedRange(borderLine, borderLine.Amount + (borderTarget - country.AppliedBorderEnforcementCost));
                country.AppliedBorderEnforcementCost = borderTarget;
            }
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
                // Playtest-2 item 4: Sweden's UO9 line joins the healthcare effect exactly as the
                // generic InfrastructureAndDevelopment joined Transportation's - one country's
                // portfolio has HHSDiscretionary, the other HealthcareAndSocialCare, never both, so
                // the other term resolves to 0 via GetActualDollarChange's own fallback. Zero at
                // seed (changes only), so the byte-identity bar is untouched.
                HealthcareSpendingChange = GetActualDollarChange(changeResult, SpendingCategory.HHSDiscretionary)
                    + GetActualDollarChange(changeResult, SpendingCategory.HealthcareAndSocialCare),
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
        /// F1 (mechanism-report finding, ruling R5; built as the close-out's Phase 2): the ONE
        /// path an interrupt-layer BudgetImpact takes to the books. Before this method existed,
        /// CabinetSystem.ApplyDecisionOption and ForeignPolicySystem.ApplyMeetingOption wrote
        /// state.Budget ONLY - the cumulative display accumulator - and the debt stock, which
        /// moves solely by budgetBalance in ApplyRevenueAndSpending, never saw them: "Bank it
        /// against the debt: +200" had never touched the debt path. (EventSystem was named in
        /// F1's first statement and is corrected here: EconomicEvent carries no budget field -
        /// the writers were exactly two.) ⚠ Corrected again, pass 5 (2026-08-26): there was a
        /// THIRD - ApplySwfDrawdownBillEffects wrote the accumulator alone - closed onto this same
        /// path by pass 5's retirement sweep. State.Budget's writers are now budgetBalance, this
        /// helper's callers (cabinet, foreign policy, the drawdown), and nothing else.
        ///
        /// THE ROUTING CLAIM, derived before wiring: every authored interrupt impact is a
        /// ONE-TIME SETTLEMENT (a windfall banked, a package funded, an evacuation chartered) -
        /// none is a recurring cost - so the honest entry is STOCK-SIDE: debt falls by a positive
        /// impact and rises by a negative one, clamped exactly as the main stock update clamps,
        /// with the SAME entry recorded in the Budget accumulator so it remains a TRUE READING of
        /// the one real path rather than a parallel ledger (two books for one quantity was the
        /// defect). ⚠ THE BOUNDARY, for future authors: a RECURRING cost is the budget process's
        /// channel (spending lines, welfare programs) and must never be expressed as a repeated
        /// interrupt impact - if a decision wants an ongoing program, it routes through Part B.
        /// </summary>
        public static void ApplyOneTimeBudgetImpact(Country country, float amount)
        {
            EconomyState state = country.State;
            state.Budget += amount;
            float maxDebt = MaxDebtToGdpPercent / 100f * state.GDP;
            float netCreditorGuard = NetCreditorRunawayGuardPercent / 100f * state.GDP;
            state.GovernmentDebt = Mathf.Clamp(state.GovernmentDebt - amount, -netCreditorGuard, maxDebt);
        }

        /// <summary>
        /// THE MATURITY RATE-LAG (ruling R4): the rate NEW ISSUANCE prices at today - base rate
        /// (the reserve-currency override where set, else the zone's spot rate) plus the risk
        /// premium scaled by sensitivity. **The premium reprices at ISSUANCE ONLY** - it is a
        /// market price set at auction, so existing bondholders keep their coupons and today's
        /// risk price reaches the stock only at rollover speed. That is the derivation's answer
        /// to "what does the premium reprice": the claim it makes about the world is that a
        /// sovereign's borrowing cost responds to its debt ratio with a lag of years, not days -
        /// and it is exactly the channel the erosion pass measured as the residual (instant
        /// repricing at premium-loaded zone rates outrunning π).
        ///
        /// THE OVERRIDE'S FATE, derived as the build directive required: the general lag with the
        /// USA's real maturity does NOT reproduce the frozen 3.3% - the model's own Fed path sits
        /// at spot 0-6% for long stretches, so a lag toward SPOT would drift the USA's rate away
        /// from the validated blended figure. What the override captures that a spot-lag doesn't
        /// is reserve-currency ISSUANCE-YIELD ANCHORING (Treasury auctions clear near the blended
        /// rate regardless of the model Fed's excursions - the global-reserve-asset demand the
        /// original treatment reasoned from). So the override RETIRES INTO the general mechanism
        /// as the USA's issuance-rate claim rather than being deleted or duplicated: it feeds the
        /// lag TARGET here, the lag applies uniformly to all six, and the USA's effective rate
        /// stays anchored (~3.3 + its near-zero premium) by construction - the boundary named,
        /// both mechanisms kept, one line each.
        /// </summary>
        private float GetDebtIssuanceRate(Country country)
        {
            float baseRate = country.BaseDebtInterestRateOverride >= 0f
                ? country.BaseDebtInterestRateOverride
                : country.CurrencyZone.InterestRate;
            return baseRate + GetDebtRiskPremium(country.State) * country.RiskPremiumSensitivity;
        }

        /// <summary>
        /// Advances the effective (blended) rate toward the current issuance rate at
        /// 1/AverageDebtMaturityYears per year - a stock with average maturity M rolls ~1/M of
        /// itself over each year, and only that share reprices. SHAPE (the taxonomy): a reversion
        /// whose speed is an annual fraction, so the daily form is the standard
        /// 1-(1-s)^fraction slice - PerDayReversion's exact composition at constant target; the
        /// target moves with the ratio's premium, which is the Phase-3 within-period feedback
        /// class, budgeted as such in the equivalence check's enumeration. Sentinel init on first
        /// advance: a fresh world or a pre-mechanism save starts AT the issuance rate - exactly
        /// what the old code charged - so behavior diverges only once rates move (the
        /// recalibration is confined to rate-moving regimes by construction). Called once per
        /// daily accrual and once per turn-form validation step; never from preview.
        /// </summary>
        private void AdvanceEffectiveDebtRate(Country country, float periodFraction)
        {
            float issuanceRate = GetDebtIssuanceRate(country);
            if (country.EffectiveDebtInterestRate < 0f)
            {
                country.EffectiveDebtInterestRate = issuanceRate;
                return;
            }

            float rolloverPerYear = 1f / Mathf.Max(1f, country.AverageDebtMaturityYears);
            float slice = 1f - Mathf.Pow(1f - rolloverPerYear, periodFraction);
            country.EffectiveDebtInterestRate += slice * (issuanceRate - country.EffectiveDebtInterestRate);
        }

        private float GetInterestOnDebt(Country country)
        {
            EconomyState state = country.State;
            // A NET CREDITOR PAYS NOTHING AND EARNS NOTHING (2026-08-02, Elias's ruling, made when the
            // zero floor was removed and negative debt became representable).
            //
            // Without this guard the arithmetic would silently invert: negative debt times a positive
            // rate is negative interest, which flows through ApplyRevenueAndSpending as a REDUCTION in
            // total spending - free money, growing with the size of the surplus, compounding into
            // exactly the runaway the floor was masking. That is a worse defect than the one being
            // fixed, and it would look like a well-run economy rather than like a bug.
            //
            // Zero rather than an interest income is the deliberately conservative half of the ruling.
            // Interest earned on net assets is a real thing a net creditor gets, and modelling it is a
            // SEPARATE decision Elias has explicitly deferred - the SWF already models the return on
            // government assets, so paying a second return on the same position here would double-count
            // it. Scoping this to "no free money" keeps the debt fix a debt fix.
            if (state.GovernmentDebt <= 0f)
            {
                return 0f;
            }

            // THE MATURITY RATE-LAG (ruling R4): charge the BLENDED rate the stock actually pays,
            // not today's issuance price - the sentinel fallback is READ-ONLY (the R4-3 pattern:
            // preview must never mutate), returning the issuance rate itself, which is bit-for-bit
            // what this method charged before the lag existed.
            float effectiveRate = country.EffectiveDebtInterestRate >= 0f
                ? country.EffectiveDebtInterestRate
                : GetDebtIssuanceRate(country);
            return state.GovernmentDebt * (effectiveRate / 100f);
        }

        /// <summary>
        /// This country's in-flight fiscal period, seeded on first use.
        ///
        /// **The seed is not a formality.** Day 1 of a new game arrives 121 days BEFORE the first
        /// AdvanceTurn, so no plan has been resolved yet and without one every country would spend
        /// nothing at all for its opening third of a year. It is derived directly from the portfolio the
        /// country was seeded with, and deliberately WITHOUT calling ResolveSpendingForTurn: that method
        /// is not idempotent - it applies a period of spending growth and demographic pressure - so
        /// calling it here would charge the world an extra turn of both before the game had advanced one.
        ///
        /// PlannedSwfReturn seeds to the fund's DETERMINISTIC average estimate rather than a random draw,
        /// so no RNG is consumed before the first budget resolution. A draw here would shift the
        /// SovereignWealthFund stream by one per country and invalidate every recorded baseline on day 1,
        /// which is a large price for the opening period's return being drawn rather than expected.
        /// </summary>
        private FiscalPeriod GetOrSeedFiscalPeriod(Country country)
        {
            if (_fiscalPeriods.TryGetValue(country.Id, out FiscalPeriod existing))
            {
                return existing;
            }

            bool hasDetailedPortfolio = country.SpendingLines.Count > 0;
            float governmentSpending = hasDetailedPortfolio
                ? GetSpendingLineTotal(country, mandatory: false)
                : GetBaselineGovernmentSpending(country);

            var seeded = new FiscalPeriod
            {
                PlannedGovernmentSpending = governmentSpending,
                PlannedMandatorySpending = hasDetailedPortfolio ? GetSpendingLineTotal(country, mandatory: true) : 0f,
                // The opening period has no player decision behind it, so all of G is baseline and none
                // of it is this period's discretionary CHANGE - the same split ResolveSpendingForTurn
                // reports for a turn in which the player changed nothing.
                PlannedBaselineGovernmentSpending = governmentSpending,
                PlannedDiscretionarySpending = 0f,
                PlannedSwfReturn = country.SovereignWealthFund != null
                    ? SovereignWealthFundSystem.GetAverageReturnEstimate(country.SovereignWealthFund)
                    : 0f,
                // Pass 5: the opening period's tariff flow, from the same pure function the boundary
                // reports - so turn 1 accrues the seed rates' take rather than a period of nothing.
                PlannedTariffRevenue = TradeSystem.ComputeTariffRevenue(country, _world),
                // Pass 6: no previous boundary, so no tariff change to pass through.
                PlannedTariffPassThroughPp = 0f,
                AppliedTariffPassThroughPp = 0f,
                PlannedFiscalReactionMultiplier = GetFiscalReactionMultiplier(country),
                GdpAtPeriodOpen = country.State.GDP,
                UnemploymentAtPeriodOpen = country.State.Unemployment,
                PotentialGdpAtPeriodOpen = country.State.PotentialGDP,
                WageGrowthGapAtPeriodOpen = MacroSystem.RealWageGrowthGapPerTurnPercent(country,
                    MacroSystem.ProductivityCycleGrowthPerTurnPercent(country, country.State.Unemployment))
            };

            _fiscalPeriods[country.Id] = seeded;
            return seeded;
        }

        /// <summary>
        /// CONTINUOUS TIME PHASE 3: one day's money. This is the fiscal engine's daily step - the half of
        /// Phase 3 the part-1 commit scoped and deliberately did not start.
        ///
        /// Every flow is one period's figure times FiscalFlowPerDayFraction (shape #1, linear). Tax
        /// revenue, benefits, welfare, interest and the SWF contribution are all RECOMPUTED from live
        /// country state each day rather than frozen at plan time. Today that only matters for interest -
        /// GDP, unemployment and every policy rate are still turn-level until Phase 5, so nothing else
        /// can move between two days - but it is the shape the rest of the migration needs, and interest
        /// genuinely should be charged on the debt the country holds today rather than the debt it held
        /// four months ago. That intra-period interest accrual is the ONE deliberate difference from the
        /// turn form, and the aggregation-equivalence check exists to keep it inside tolerance.
        ///
        /// The fiscal reaction multiplier is the exception and is held fixed for the period; recomputing
        /// it daily failed the bar by a wide margin, and FiscalPeriod records both the measurement and
        /// why freezing it is the better model rather than merely the passing one.
        ///
        /// The SWF sequence is the turn form's, unchanged in ORDER: contribute, earn, clamp, then draw -
        /// the structural draw comes out of post-return assets, not pre-. Only the size of each step
        /// changed.
        ///
        /// Runs for every country including NPCs, exactly as the turn form did.
        /// </summary>
        private void AccrueDailyFiscalFlows(Country country)
        {
            FiscalPeriod period = GetOrSeedFiscalPeriod(country);
            EconomyState state = country.State;

            // THE MATURITY RATE-LAG (ruling R4): the blended rate rolls toward today's issuance
            // price before interest is charged - repricing is continuous, so advance-then-charge;
            // the one-day ordering difference is inside Phase 3's stated drift class either way.
            AdvanceEffectiveDebtRate(country, FiscalFlowPerDayFraction);

            float governmentSpending = period.PlannedGovernmentSpending * FiscalFlowPerDayFraction;
            float mandatorySpending = period.PlannedMandatorySpending * FiscalFlowPerDayFraction;
            float unemploymentBenefitCost = GetUnemploymentBenefitCost(country) * FiscalFlowPerDayFraction;
            float interestOnDebt = GetInterestOnDebt(country) * FiscalFlowPerDayFraction;
            float welfareCost = GetTotalWelfareCost(country) * FiscalFlowPerDayFraction;
            float swfContribution = GetSwfContribution(country) * FiscalFlowPerDayFraction;
            // Pass 5: the planned tariff flow, sliced exactly like mandatory spending - a fixed period
            // figure distributed linearly, so the daily form sums to the turn form by construction.
            float tariffRevenue = period.PlannedTariffRevenue * FiscalFlowPerDayFraction;

            float swfReturns = 0f;
            float swfDraw = 0f;
            if (country.SovereignWealthFund != null)
            {
                country.SovereignWealthFund.TotalAssets += swfContribution;

                swfReturns = period.PlannedSwfReturn * FiscalFlowPerDayFraction;
                country.SovereignWealthFund.TotalAssets = Mathf.Max(0f, country.SovereignWealthFund.TotalAssets + swfReturns);

                float maxSwfAssets = MaxSwfToGdpPercent / 100f * state.GDP;
                country.SovereignWealthFund.TotalAssets = Mathf.Clamp(country.SovereignWealthFund.TotalAssets, 0f, maxSwfAssets);

                swfDraw = country.SovereignWealthFund.TotalAssets * SwfStructuralDrawPerTurnFraction() * FiscalFlowPerDayFraction;
                swfDraw = Mathf.Clamp(swfDraw, 0f, country.SovereignWealthFund.TotalAssets);
                country.SovereignWealthFund.TotalAssets -= swfDraw;
            }

            // Step 2's third section (2026-08-25): the debt ledger observes THIS write. The three
            // inputs the split needs beyond what the call already returns are read here, before
            // the write, from exactly the state the charge above read - the stock GetInterestOnDebt
            // charged, the issuance rate AdvanceEffectiveDebtRate just targeted, and the blended
            // rate it just advanced - so the recorded interest split is the model's own pair, not a
            // reconstruction. The ledger opens at the pre-write stock on a period's first day.
            float debtBeforeWrite = state.GovernmentDebt;
            float issuanceRateToday = GetDebtIssuanceRate(country);
            float effectiveRateToday = country.EffectiveDebtInterestRate >= 0f ? country.EffectiveDebtInterestRate : issuanceRateToday;
            float interestAtIssuanceToday = debtBeforeWrite > 0f ? debtBeforeWrite * (issuanceRateToday / 100f) * FiscalFlowPerDayFraction : 0f;
            DebtLedgerRecorder.EnsureAccruing(country, CurrentDate, debtBeforeWrite);

            float revenue = ApplyRevenueAndSpending(country, governmentSpending, mandatorySpending, unemploymentBenefitCost, interestOnDebt, welfareCost, swfContribution, swfDraw, tariffRevenue, out float totalSpending, out float budgetBalance, FiscalFlowPerDayFraction, period.PlannedFiscalReactionMultiplier);

            period.AccruedRevenue += revenue;
            period.AccruedTariffRevenue += tariffRevenue;
            period.AccruedMandatorySpending += mandatorySpending;
            period.AccruedUnemploymentBenefitCost += unemploymentBenefitCost;
            period.AccruedInterestOnDebt += interestOnDebt;
            period.AccruedWelfareCost += welfareCost;
            period.AccruedSwfContribution += swfContribution;
            period.AccruedSwfReturns += swfReturns;
            period.AccruedTotalSpending += totalSpending;
            period.AccruedBudgetBalance += budgetBalance;

            // The erosion factor is the ONE recomputation - the same expression as the stock
            // update's, on the same state - and the ledger's twin-drift detector is what keeps it
            // so. The clamp is detected by landing ON either bound, tested against the same two
            // limits the update clamps to.
            float erosionFactorApplied = Mathf.Pow(Mathf.Max(0.01f, 1f - state.Inflation / 100f), FiscalFlowPerDayFraction);
            float debtAfterWrite = state.GovernmentDebt;
            float ceilingToday = MaxDebtToGdpPercent / 100f * state.GDP;
            float guardToday = NetCreditorRunawayGuardPercent / 100f * state.GDP;
            bool clampBoundToday = debtAfterWrite >= ceilingToday || debtAfterWrite <= -guardToday;
            DebtLedgerRecorder.RecordDay(country, CurrentDate, debtBeforeWrite, debtAfterWrite, erosionFactorApplied,
                revenue, totalSpending, interestOnDebt, interestAtIssuanceToday, budgetBalance,
                period.PlannedFiscalReactionMultiplier, issuanceRateToday, effectiveRateToday, clampBoundToday);
        }

        /// <summary>
        /// CONTINUOUS TIME PHASE 3 VALIDATION HOOK - the money resolution EXACTLY as AdvanceTurn applied
        /// it before this phase: one whole period's flows, in one step, against a caller-supplied plan.
        ///
        /// Kept rather than deleted because it IS the aggregation-equivalence bar - the already-validated
        /// turn-level answer that 121 daily accruals have to land within tolerance of. Nothing in the
        /// simulation loop calls it; its only caller is AggregationEquivalenceCheck.
        /// </summary>
        public void ApplyPeriodFiscalStepForValidation(Country country, float governmentSpending, float mandatorySpending, float swfPeriodReturn, float tariffRevenue)
        {
            EconomyState state = country.State;
            // R4: the turn form advances the rate lag one whole period, mirroring the daily path's
            // advance-then-charge order - the equivalence bar's two sides must share the shape.
            AdvanceEffectiveDebtRate(country, 1f);
            float unemploymentBenefitCost = GetUnemploymentBenefitCost(country);
            float interestOnDebt = GetInterestOnDebt(country);
            float welfareCost = GetTotalWelfareCost(country);
            float swfContribution = GetSwfContribution(country);

            float swfDraw = 0f;
            if (country.SovereignWealthFund != null)
            {
                country.SovereignWealthFund.TotalAssets += swfContribution;
                country.SovereignWealthFund.TotalAssets = Mathf.Max(0f, country.SovereignWealthFund.TotalAssets + swfPeriodReturn);

                float maxSwfAssets = MaxSwfToGdpPercent / 100f * state.GDP;
                country.SovereignWealthFund.TotalAssets = Mathf.Clamp(country.SovereignWealthFund.TotalAssets, 0f, maxSwfAssets);

                swfDraw = country.SovereignWealthFund.TotalAssets * SwfStructuralDrawPerTurnFraction();
                swfDraw = Mathf.Clamp(swfDraw, 0f, country.SovereignWealthFund.TotalAssets);
                country.SovereignWealthFund.TotalAssets -= swfDraw;
            }

            ApplyRevenueAndSpending(country, governmentSpending, mandatorySpending, unemploymentBenefitCost, interestOnDebt, welfareCost, swfContribution, swfDraw, tariffRevenue, out _, out _);
        }

        /// <summary>
        /// CONTINUOUS TIME PHASE 3 VALIDATION HOOK - installs an explicit plan and runs exactly one day of
        /// the REAL production accrual (AccrueDailyFiscalFlows), so AggregationEquivalenceCheck can drive
        /// 121 of them against the turn-shaped step above with both paths spending a plan it chose.
        ///
        /// It delegates rather than reproducing the daily maths on purpose: a validation path that
        /// re-implements what it validates can pass while the shipped code is wrong.
        /// </summary>
        public void AccrueDayForValidation(Country country, float governmentSpending, float mandatorySpending, float swfPeriodReturn, float tariffRevenue)
        {
            FiscalPeriod period = GetOrSeedFiscalPeriod(country);
            period.PlannedGovernmentSpending = governmentSpending;
            period.PlannedMandatorySpending = mandatorySpending;
            period.PlannedSwfReturn = swfPeriodReturn;
            period.PlannedTariffRevenue = tariffRevenue;
            AccrueDailyFiscalFlows(country);
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
        ///
        /// PASS 5 (2026-08-26): <paramref name="tariffRevenue"/> is the tariff flow for the same slice
        /// the spending figures cover (the caller applies the period fraction, as for spending) - a
        /// RECURRING revenue that joins actual revenue here beside taxes and the fund draw, INSIDE the
        /// fiscal-reaction multiplier (the 2026-08-02 SWF ruling - "returns run INSIDE the fiscal
        /// reaction multiplier" - extended by analogy: a stance that loosens on a windfall should see
        /// every windfall; it is also what keeps the debt ledger's revenue/m split exact with no new
        /// term, and it is what bounds the tariff lever - a 50%-override windfall is partly given back
        /// as the stance loosens) and OUTSIDE CollectionEfficiency (customs are not the tax
        /// administration's collection). Derived, not inherited from F1: F1's interrupt impacts are
        /// one-time settlements and go stock-side; a flow that recurs every period is the budget
        /// process's channel, which F1's own boundary rule names. Revenue-neutral at seed by
        /// construction: each country's CollectionEfficiency gives back exactly its seed take
        /// (WorldFactory) so the recalibration's landed T1 primaries - the anchored quantity - are
        /// unchanged; whether the real tax targets already contained customs is recorded as unverified.
        ///
        /// CONTINUOUS TIME PHASE 3: every spending figure is passed IN, so the caller decides whether it
        /// is handing over a whole period's or one day's. Revenue is the exception - it is computed here,
        /// from the country's own portfolio - so <paramref name="revenuePeriodFraction"/> is what tells
        /// this method which of the two it is being asked for. It defaults to a whole period, which keeps
        /// PreviewTurn (the turn form's remaining caller) reading exactly as it did.
        ///
        /// <paramref name="fiscalReactionMultiplierOverride"/> is the -1 sentinel by default (this file's
        /// existing idiom for "unset" - see Country.BaseDebtInterestRateOverride), meaning "compute the
        /// multiplier from the debt ratio right now", which is what the turn form always did. The daily
        /// path passes the value its period opened with instead; see FiscalPeriod for why that one is
        /// held fixed while every other component is recomputed daily.
        /// </summary>
        private float ApplyRevenueAndSpending(Country country, float governmentSpending, float mandatorySpending, float unemploymentBenefitCost, float interestOnDebt, float welfareCost, float swfContribution, float swfReturns, float tariffRevenue, out float totalSpending, out float budgetBalance, float revenuePeriodFraction = 1f, float fiscalReactionMultiplierOverride = -1f)
        {
            EconomyState state = country.State;
            float theoreticalRevenue = GetTotalTaxRevenue(country) * revenuePeriodFraction;
            float fiscalReactionMultiplier = fiscalReactionMultiplierOverride >= 0f
                ? fiscalReactionMultiplierOverride
                : GetFiscalReactionMultiplier(country);
            float financeTreasuryCompetenceBias = CabinetSystem.GetCompetenceBias(country, CabinetPortfolio.FinanceTreasury);
            float effectiveCollectionEfficiency = Mathf.Clamp01(country.CollectionEfficiency + financeTreasuryCompetenceBias);
            // NOTE: `swfReturns` here is now the STRUCTURAL DRAW, not the realised market return - see
            // SwfStructuralDrawPercentPerYear. The parameter name is retained because it is the fund's
            // contribution to revenue either way, and renaming it across every call site would obscure
            // the diff that matters. The realised return no longer reaches the budget at all.
            //
            // SWF INCOME IS INSIDE THE MULTIPLIER (2026-08-02, Elias's ruling: fix the cause).
            //
            // They used to be added AFTER it - `... * fiscalReactionMultiplier + swfReturns` - which put
            // the fastest-growing component of a net creditor's revenue permanently beyond the reach of
            // the one mechanism that pushes back. Measured, not assumed: Sweden's multiplier pinned at its
            // 0.5 floor for 104 of 120 turns while fund returns reached 405% of the tax revenue the
            // multiplier could still act on, and the fund compounded 386 -> 10,902 while tax revenue grew
            // 174 -> 767. The stabiliser worked perfectly on the shrinking half and not at all on the
            // growing one.
            //
            // Inside the multiplier, the semantics are also the more defensible ones: a government whose
            // debt is far below its comfortable level LOOSENS - it spends its fund's windfall rather than
            // banking it - which is exactly what the fiscal reaction function already claims to model for
            // tax revenue. The symmetry holds at the other end too: a heavily indebted government leans
            // harder on its fund.
            // Pass 5: the tariff flow sits with the fund draw - inside the multiplier, outside CE.
            float actualRevenue = (theoreticalRevenue * effectiveCollectionEfficiency + swfReturns + tariffRevenue) * fiscalReactionMultiplier;
            totalSpending = governmentSpending + mandatorySpending + unemploymentBenefitCost + interestOnDebt + welfareCost + swfContribution;
            budgetBalance = actualRevenue - totalSpending;

            state.Budget += budgetBalance;

            // NO FLOOR (2026-08-02, Elias's ruling). Debt may go NEGATIVE, which means the country is a
            // net creditor - a real fiscal state, and specifically Norway's, the country this project
            // already used to calibrate SWF returns. The game has always DISPLAYED "Net Government
            // Position (debt minus fund assets)"; only the simulation refused to represent it.
            //
            // The old `Mathf.Clamp(..., 0f, maxDebt)` is what produced the debt-to-zero bimodality:
            // a stock driven below zero was held at zero and then released, which is the exact shape of
            // a bimodal attractor. Sweden hit the floor on 67 of 120 baseline turns, France on 14, and
            // Germany only under `swfstress` - precisely the set whose SWF drives net position negative.
            // The CEILING is retained and is not implicated: no country has ever reached it.
            //
            // THE NEGATIVE SIDE IS A RUNAWAY GUARD, NOT A BOUND (2026-08-02, Elias's ruling).
            //
            // A -300% symmetric bound was tried first and rejected: France settled at -297.6% against it,
            // which is not "close to a bound" but PINNED TO one, so every downstream reader - C4's rating
            // above all - was reading the clamp rather than the model. The cause is now fixed upstream
            // (SWF returns run through the fiscal reaction multiplier), and this exists only to stop an
            // unbounded excursion during and after that fix.
            //
            // ⚠ IT IS SET WHERE NOTHING SHOULD EVER REACH IT. If a country touches -1000% of GDP that is
            // a BUG REPORT, not a clamp doing its job - the guard has caught a runaway the stabiliser
            // should have prevented, and the correct response is to investigate rather than to widen it.
            //
            // The CEILING is unchanged and is not a guard: MaxDebtToGdpPercent is a calibrated gameplay
            // bound that no country has ever reached either, but it predates this and stays as it was.
            float maxDebt = MaxDebtToGdpPercent / 100f * state.GDP;
            float netCreditorGuard = NetCreditorRunawayGuardPercent / 100f * state.GDP;

            // THE EROSION TERM (2026-08-17; mechanism-report rulings R1-R3). The standard
            // debt-dynamics identity's −π·b, previously missing: per R2's DECLARATION (recorded in
            // CLAUDE.md's "Accounting Convention" section) this model's dollars are constant-price
            // units and GovernmentDebt is the ONE nominal quantity - as sovereign debt is - so
            // inflation erodes its real value at π per year and deflation grows it, correctly
            // signed with no special case. SYMMETRIC per R3's reasoning: a net creditor's real
            // claim erodes the same way, so the factor applies to whichever position exists and
            // always shrinks it toward zero under inflation - no free money in either direction,
            // and no interaction with the surplus-spiral hazard (the term is self-limiting: it
            // scales with the position it erodes).
            //
            // SHAPE (the taxonomy): an annual rate on a SELF-REFERENCE, so the daily form is the
            // COMPOUNDING (power) slice - (1 − π/100)^fraction - not the linear slice. At constant
            // π the slices compose to the turn factor exactly; the interleaving with the daily
            // budgetBalance subtraction is the same within-period feedback Phase 3's stated drift
            // budget already covers (see the equivalence check's erosion enumeration). The inner
            // Max is defensive only - Inflation is produced clamped far below 100 everywhere.
            float erosionFactor = Mathf.Pow(Mathf.Max(0.01f, 1f - state.Inflation / 100f), revenuePeriodFraction);
            state.GovernmentDebt = Mathf.Clamp(state.GovernmentDebt * erosionFactor - budgetBalance, -netCreditorGuard, maxDebt);

            return actualRevenue;
        }
    }
}
