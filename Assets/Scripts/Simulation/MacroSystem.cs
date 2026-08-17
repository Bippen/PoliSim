using PoliSim.Data;
using UnityEngine;

namespace PoliSim.Simulation
{
    /// <summary>
    /// Core macroeconomic theory driving GDP, unemployment, and inflation: the national accounts
    /// identity (GDP = C + I + G + NX), Okun's Law (unemployment vs. growth gap), and an
    /// expectations-augmented Phillips Curve (inflation vs. unemployment gap), plus the political
    /// layer built on top of it: ApprovalRating (a Phillips-curve-adjacent "misery index" of
    /// economic performance plus policy-shock effects) and each spending category's small,
    /// separable growth/confidence side-effect. Each relationship is its own small method with
    /// named, tunable constants rather than one combined formula.
    /// </summary>
    public static class MacroSystem
    {
        // --- National accounts identity: GDP = Consumption + Investment + Government + NetExports ---

        /// <summary>Baseline consumption as a share of the prior turn's GDP (the marginal propensity to consume).</summary>
        private const float BaseConsumptionRate = 0.60f;

        /// <summary>Baseline investment as a share of the prior turn's GDP.</summary>
        private const float BaseInvestmentRate = 0.20f;

        /// <summary>Fraction of consumption removed per percentage point of interest rate.</summary>
        private const float ConsumptionInterestSensitivity = 0.5f;

        /// <summary>Fraction of investment removed per percentage point of interest rate - investment is more rate-sensitive than consumption.</summary>
        private const float InvestmentInterestSensitivity = 1.5f;

        /// <summary>
        /// How much of the gap between the identity's raw C+I+G+NX result and PotentialGDP closes
        /// each turn - real economies drift back toward trend output rather than compounding a
        /// one-turn imbalance (e.g. baseline C+I+G shares that don't sum to exactly 100% of GDP)
        /// forever. Named "output gap reversion" to mirror Okun's Law/the Phillips Curve, which
        /// already treat the gap between actual and potential/natural values as the thing that
        /// drives change, not something that free-accumulates.
        /// </summary>
        private const float OutputGapReversionSpeed = 0.5f;

        /// <summary>Smallest GDP a country can fall to - keeps a shrinking economy able to recover instead of locking at exactly 0 (0 * anything is still 0). Public so EventSystem's GDP shocks share the same floor.</summary>
        public const float MinGdp = 1f;

        /// <summary>
        /// Computes this turn's Consumption and Investment from prior GDP, the interest rate, and
        /// confidence, then sets GDP to their sum plus government spending and net exports
        /// (TradeBalance, already computed by TradeSystem for this turn), reverted partway toward
        /// PotentialGDP. Interest-rate dampening is measured against TaylorRule.NeutralRealRate, not
        /// zero, since every seeded country sits at a positive policy rate and none of them should be
        /// permanently penalized just for being at a normal, neutral rate.
        /// </summary>
        public static void ApplyNationalAccounts(Country country, float governmentSpending, float interestRate)
        {
            EconomyState state = country.State;
            float priorGdp = state.GDP;
            float rateAboveNeutral = interestRate - TaylorRule.NeutralRealRate;

            float consumptionInterestFactor = Mathf.Max(0f, 1f - rateAboveNeutral / 100f * ConsumptionInterestSensitivity);
            float investmentInterestFactor = Mathf.Max(0f, 1f - rateAboveNeutral / 100f * InvestmentInterestSensitivity);

            state.Consumption = priorGdp * BaseConsumptionRate * consumptionInterestFactor * state.ConsumerConfidence;
            state.Investment = priorGdp * BaseInvestmentRate * investmentInterestFactor * state.BusinessConfidence;

            float gdpFromIdentity = state.Consumption + state.Investment + governmentSpending + state.TradeBalance;
            float gdpAfterReversion = gdpFromIdentity + OutputGapReversionSpeed * (state.PotentialGDP - gdpFromIdentity);
            state.GDP = Mathf.Max(MinGdp, gdpAfterReversion);
        }

        // --- Potential GDP: trend output, independent of this turn's actual GDP ---

        /// <summary>Grows PotentialGDP by the country's structural PotentialGrowthRate, independent of actual GDP shocks.</summary>
        public static void ApplyPotentialGdpGrowth(Country country)
        {
            EconomyState state = country.State;
            state.PotentialGDP = Mathf.Max(0f, state.PotentialGDP * (1f + country.PotentialGrowthRate / 100f));
        }

        // --- Okun's Law: unemployment moves with the growth gap ---

        /// <summary>How many points unemployment moves per percentage point that actual growth falls short of potential growth.</summary>
        private const float OkunCoefficient = 0.5f;

        /// <summary>Fraction of the gap versus NAIRU that closes each turn on its own, absent a growth shock - unemployment drifts home to its structural rate rather than accumulating a growth-gap delta forever.</summary>
        private const float UnemploymentReversionSpeed = 0.7f;

        /// <summary>Gameplay ceiling for unemployment - a bug elsewhere in the feedback chain should never be able to push this past a sane bound.</summary>
        private const float MaxUnemploymentPercent = 30f;

        /// <summary>UBI's small, debated labor-supply effect: SLOWS unemployment's reversion toward NAIRU at full generosity - kept subtle deliberately, since the real-world effect is itself debated, not settled.</summary>
        private const float UbiUnemploymentReversionPenalty = 0.05f;

        /// <summary>ChildcareSubsidies' labor-force-participation effect (particularly documented for parents): SPEEDS unemployment's reversion toward NAIRU at full generosity.</summary>
        private const float ChildcareUnemploymentReversionBonus = 0.03f;

        /// <summary>Floor on the welfare-adjusted reversion speed - UBI's penalty (see above) should never be able to stall or reverse Okun's Law's own mean-reversion, only slow it somewhat.</summary>
        private const float MinUnemploymentReversionSpeed = 0.3f;

        /// <summary>
        /// Unemployment points added per point MinimumWagePercentOfMedian sits above the country's own
        /// BaselineMinimumWagePercentOfMedian (its real seeded starting level, not a universal
        /// constant - the same "gap versus a country-specific anchor" idiom ComfortableDebtToGdpPercent/
        /// BaselinePovertyRate already use, chosen so a fresh game opens at zero gap rather than an
        /// artificial turn-1 shock, and so this doesn't double-count against NAIRU, which already
        /// reflects each country's real structural conditions including its actual minimum wage).
        /// Small and directionally grounded (not precisely fitted) by the CBO's 2019 estimate that a
        /// federal $15/hr minimum wage (raising the effective Kaitz index roughly 20-30 points) would
        /// cost a median-estimate ~1.3 million jobs against a ~160 million labor force (~0.8%) - a
        /// modest, debated, real-world-scale effect, not the dominant driver of Unemployment the
        /// growth gap is.
        /// </summary>
        internal const float MinimumWageEmploymentSensitivity = 1.5f;

        /// <summary>
        /// This turn's Unemployment nudge from how far Country.MinimumWagePercentOfMedian has moved
        /// from its own seeded baseline, an ONGOING stock effect of the current level (like
        /// WelfareProgram's approval term, not a one-time shock) - zero for a country with no
        /// statutory minimum wage (Sweden, Italy - see Country.MinimumWageImplemented) and zero at
        /// the seeded starting level for every other country.
        /// </summary>
        internal static float GetMinimumWageUnemploymentAdjustment(Country country)
        {
            if (!country.MinimumWageImplemented)
            {
                return 0f;
            }

            float gap = country.MinimumWagePercentOfMedian - country.BaselineMinimumWagePercentOfMedian;
            return MinimumWageEmploymentSensitivity * gap / 100f;
        }

        /// <summary>
        /// Unemployment points removed per point Country.OvertimeRegulationLevel sits above its
        /// neutral 50 (added per point below) - the "work-sharing" argument behind France's 35-hour
        /// week (stricter hour caps spread the same total work across more workers). A GENUINELY
        /// CONTESTED real economic claim, not a settled fact - some empirical studies find the
        /// 35-hour week didn't meaningfully reduce French unemployment as intended - so this is
        /// deliberately small, representing one side of that debate, not a confident modeling choice.
        /// </summary>
        internal const float OvertimeUnemploymentSensitivity = 0.008f;

        internal static float GetOvertimeUnemploymentAdjustment(Country country)
        {
            return -OvertimeUnemploymentSensitivity * (country.OvertimeRegulationLevel - NeutralPolicyDialLevel);
        }

        /// <summary>Unemployment points removed per point Country.RetrainingProgramLevel sits above its neutral 50 (added per point below) - the well-established real economic rationale that retraining eases job transitions, smaller than the overtime effect since it's a more indirect mechanism.</summary>
        internal const float RetrainingUnemploymentSensitivity = 0.006f;

        internal static float GetRetrainingUnemploymentAdjustment(Country country)
        {
            return -RetrainingUnemploymentSensitivity * (country.RetrainingProgramLevel - NeutralPolicyDialLevel);
        }

        /// <summary>
        /// Okun's Law: unemployment rises when actual GDP growth runs below potential/trend growth,
        /// and falls when it runs above, plus mean-reversion pulling it back toward the country's
        /// NAIRU. <paramref name="actualGrowthRatePercent"/> is this turn's realized GDP growth,
        /// computed by the caller from GDP before/after ApplyNationalAccounts. The reversion speed
        /// itself is nudged by any implemented Country.WelfarePrograms (UBI/ChildcareSubsidies - see
        /// GetWelfareAdjustedReversionSpeed), both small and clamped so this stays a subtle secondary
        /// effect, not a new primary driver of unemployment.
        /// </summary>
        public static void ApplyOkunsLaw(Country country, float actualGrowthRatePercent, float sliceFraction = 1f,
            float? reversionReferenceUnemployment = null)
        {
            // CONTINUOUS TIME PHASE 5: `sliceFraction` scales the flow families - the Okun RESPONSE
            // (the growth gap arrives annualized either way, so the per-step response is its slice;
            // daily growth stays CAUSAL - a mid-period shock moves unemployment the same day) and
            // the four level-gap nudges (ongoing per-turn flows, linear like Phase 4's drifts).
            // OkunCoefficient itself takes NO transform - a relationship, not a flow.
            //
            // ⚠ THE REVERSION IS THE SHAPE THAT FAILED FIRST, and the record keeps the failure: the
            // obvious PerDayReversion form (self-referencing, compounding) interleaves with the
            // daily Okun response - each day's reversion acts on a U the response already moved, and
            // under a large within-period transient the two fight across the NAIRU crossing
            // (measured: USA at an 8% shock, turn 0.60 vs daily 2.54 - 325% drift against a 3% bar;
            // still failing at a 2% shock on the seeded 13% output gap). The turn form applies both
            // terms against the PERIOD-START state simultaneously, so the equivalent daily shape is
            // Phase 3's own precedent - the FIXED-REFERENCE DISTRIBUTED APPLICATION, the FRF's
            // frozen-stance pattern: the reversion references the unemployment the period OPENED at
            // (<paramref name="reversionReferenceUnemployment"/>) and distributes LINEARLY (a
            // fixed-reference distribution sums exactly to the turn's single application - the
            // linear slice, NOT PerDayReversion, which is exact only for self-referencing forms).
            // "Drift home to NAIRU" is structural, computed against where the period started - a
            // stance, per Phase 3's stance-vs-flow question. No constant VALUE changed.
            EconomyState state = country.State;
            float growthGap = actualGrowthRatePercent - country.PotentialGrowthRate;
            float unemploymentChange = -OkunCoefficient * growthGap * sliceFraction;
            float reversionReference = reversionReferenceUnemployment ?? state.Unemployment;
            unemploymentChange += GetWelfareAdjustedReversionSpeed(country) * sliceFraction
                * (country.NaturalUnemploymentRate - reversionReference);
            unemploymentChange += (GetMinimumWageUnemploymentAdjustment(country)
                + GetOvertimeUnemploymentAdjustment(country)
                + GetRetrainingUnemploymentAdjustment(country)
                + GetSectorUnemploymentAdjustment(country)) * sliceFraction;

            state.Unemployment = Mathf.Clamp(state.Unemployment + unemploymentChange, 0f, MaxUnemploymentPercent);
        }

        /// <summary>Phase 5 daily wrapper. <paramref name="annualizedDailyGrowthPercent"/> is the
        /// day's realized GDP growth times DaysPerTurn - annualized so the gap against the annual
        /// PotentialGrowthRate is dimensionally honest, then sliced back down inside.
        /// <paramref name="unemploymentAtPeriodOpen"/> is the reversion's fixed reference - see the
        /// turn form's Phase 5 comment for why it is the period-open value, not today's.</summary>
        public static void ApplyOkunsLawDaily(Country country, float annualizedDailyGrowthPercent, float unemploymentAtPeriodOpen)
            => ApplyOkunsLaw(country, annualizedDailyGrowthPercent, MacroSliceFractionPerDay, unemploymentAtPeriodOpen);

        /// <summary>
        /// UnemploymentReversionSpeed, nudged by any implemented UBI (real debated labor-supply
        /// effect - a full-generosity UBI slows reversion slightly) or ChildcareSubsidies (documented
        /// labor-force-participation effect for parents - speeds reversion slightly). Both are small
        /// and the result is floored so neither can meaningfully destabilize Okun's Law's own
        /// mean-reversion, only tilt it.
        /// </summary>
        internal static float GetWelfareAdjustedReversionSpeed(Country country)
        {
            float adjustment = 0f;
            foreach (WelfareProgram program in country.WelfarePrograms)
            {
                if (!program.IsImplemented)
                {
                    continue;
                }

                float generosityFraction = program.GenerosityLevel / 100f;
                if (program.Type == WelfareProgramType.UBI)
                {
                    adjustment -= UbiUnemploymentReversionPenalty * generosityFraction;
                }
                else if (program.Type == WelfareProgramType.ChildcareSubsidies)
                {
                    adjustment += ChildcareUnemploymentReversionBonus * generosityFraction;
                }
            }

            return Mathf.Clamp(UnemploymentReversionSpeed + adjustment, MinUnemploymentReversionSpeed, 1f);
        }

        // --- Expectations-augmented Phillips Curve: inflation moves with the unemployment gap ---

        /// <summary>How many inflation points move per percentage point of unemployment gap versus NAIRU.</summary>
        private const float PhillipsCurveSlope = 0.3f;

        /// <summary>Gameplay ceiling for inflation - a bug elsewhere in the feedback chain should never be able to push this past a sane bound. Public so EventSystem's inflation shocks share the same ceiling.</summary>
        public const float MaxInflationPercent = 30f;

        /// <summary>
        /// Phillips Curve: inflation equals expected inflation, minus the unemployment gap versus
        /// NAIRU scaled by PhillipsCurveSlope. Unemployment above NAIRU (slack) is disinflationary;
        /// below NAIRU (overheating) is inflationary. Unemployment's own mean-reversion (see
        /// ApplyOkunsLaw) keeps this gap from growing without bound, so inflation settling back down
        /// is a consequence of that rather than a separate correction here.
        /// </summary>
        public static void ApplyPhillipsCurveInflation(Country country)
        {
            EconomyState state = country.State;
            float unemploymentGap = state.Unemployment - country.NaturalUnemploymentRate;
            float inflation = state.InflationExpectations - PhillipsCurveSlope * unemploymentGap;

            state.Inflation = Mathf.Clamp(inflation, 0f, MaxInflationPercent);
        }

        /// <summary>How quickly inflation expectations adapt toward realized inflation each turn (0-1).</summary>
        private const float ExpectationsAdaptationSpeed = 0.5f;

        /// <summary>Adaptive expectations: next turn's expected inflation moves partway toward this turn's realized inflation. Phase 5: the speed converts through PerDayReversion in the daily wrapper below - the adaptation is a plain reversion, Phase 2's mechanical shape.</summary>
        public static void ApplyInflationExpectations(EconomyState state, float adaptationSpeed = ExpectationsAdaptationSpeed)
        {
            state.InflationExpectations += (state.Inflation - state.InflationExpectations) * adaptationSpeed;
        }

        // --- Continuous Time Phase 5: the core macro engine's daily forms ---
        // Derived, never typed in - the standing discipline.
        //
        // ⚠ EXPECTATIONS DELIBERATELY HAVE NO DAILY FORM, and a failed one existed briefly: a
        // PerDayReversion adaptation toward the day's inflation fails the equivalence bar wherever
        // inflation moves materially within a period (measured: Italy 6.2%, USA 10.3% at an 8%
        // drive) - and no fixed-reference variant can reproduce the turn semantics, because the
        // turn form adapts ONCE toward the period's CLOSING print, a boundary-anchored quantity by
        // its own definition. That is not an implementation obstacle but a model statement worth
        // keeping: expectations anchor to the period's headline figure, so adaptation is a PERIOD
        // STANCE (the Phase 3 lesson at full strength - not everything becomes a flow), applied at
        // the boundary in ApplyDomesticPolicy, reading the boundary day's inflation exactly as the
        // turn regime always did. InflationExpectations is CONSTANT within a period - "sticky
        // expectations" - which the daily Phillips step then reads all period long.
        private const float MacroSliceFractionPerDay = 1f / SimulationManager.DaysPerTurn;

        /// <summary>Phase 5 daily wrapper for trend output - the annual-rate POWER SLICE, the shape
        /// Phase 4's verdict predicted for exactly this method.</summary>
        public static void ApplyPotentialGdpGrowthDaily(Country country)
        {
            EconomyState state = country.State;
            state.PotentialGDP = Mathf.Max(0f, state.PotentialGDP * Mathf.Pow(1f + country.PotentialGrowthRate / 100f, MacroSliceFractionPerDay));
        }

        /// <summary>
        /// Phase 5: the identity's daily form - the AFFINE POWER SLICE, the one genuinely new shape
        /// this phase adds to the taxonomy. The turn form is an affine contraction
        /// `GDP' = A·GDP + B` with `A = (1−s)·a` (s = OutputGapReversionSpeed, a = the C+I share of
        /// prior GDP after rate/confidence factors) and `B = (1−s)(G+NX) + s·PotentialGDP`. Neither
        /// the plain power slice nor PerDayReversion fits an affine map, but its exact per-day
        /// factorization does: `A_d = A^(1/D)`, `GDP_{d+1} = A_d·GDP_d + B·(1−A_d)/(1−A)` - D
        /// compositions telescope to exactly `A·GDP + B` at constant inputs, so the validated
        /// per-period dynamics (including how fast a shock decays) are preserved rather than
        /// re-derived. Inputs DO move within a period (PotentialGDP grows daily, confidences drift),
        /// so a small residual is EXPECTED at the equivalence bar, Phase 3's class.
        ///
        /// Iterating the raw turn map daily was considered and rejected: 365 applications of a
        /// ~0.4-contraction snap GDP to its fixed point within days - a different, unvalidated
        /// dynamics, not a finer version of the validated one. Consumption/Investment stay
        /// annualized LEVELS recomputed from prior-day GDP, same semantics the turn form set.
        /// `A` is clamped to 0.99 before the pow so the geometric-sum denominator (1−A) is never
        /// near zero; with shares ≤0.8 and confidences ~1, A sits near 0.4.
        /// </summary>
        public static void ApplyNationalAccountsDaily(Country country, float governmentSpending, float interestRate,
            float potentialGdpAtPeriodOpen)
        {
            // <paramref name="potentialGdpAtPeriodOpen"/> is the FOURTH fixed reference of this
            // phase, and the one the equivalence bar demanded last: the turn form's identity read
            // the PERIOD-OPEN PotentialGDP (trend growth applied AFTER the identity in
            // AdvanceTurn's order), so a daily attractor that tracks the live, daily-compounding
            // PotentialGDP lands GDP ~0.65% high per period - within GDP's own bar, but Okun
            // multiplies it into a failing unemployment residual (measured 0.34 points on the US
            // seeded gap). "The gap pulls toward the trend as assessed at the period's planning
            // boundary" - the same anchor semantics as the planned G beside it and the FRF stance.
            // PotentialGDP itself still compounds daily as a stat; the next period anchors afresh.
            EconomyState state = country.State;
            float priorGdp = state.GDP;
            float rateAboveNeutral = interestRate - TaylorRule.NeutralRealRate;

            float consumptionInterestFactor = Mathf.Max(0f, 1f - rateAboveNeutral / 100f * ConsumptionInterestSensitivity);
            float investmentInterestFactor = Mathf.Max(0f, 1f - rateAboveNeutral / 100f * InvestmentInterestSensitivity);

            state.Consumption = priorGdp * BaseConsumptionRate * consumptionInterestFactor * state.ConsumerConfidence;
            state.Investment = priorGdp * BaseInvestmentRate * investmentInterestFactor * state.BusinessConfidence;

            float share = BaseConsumptionRate * consumptionInterestFactor * state.ConsumerConfidence
                + BaseInvestmentRate * investmentInterestFactor * state.BusinessConfidence;
            float contraction = Mathf.Clamp((1f - OutputGapReversionSpeed) * share, 0f, 0.99f);
            float attractorTerm = (1f - OutputGapReversionSpeed) * (governmentSpending + state.TradeBalance)
                + OutputGapReversionSpeed * potentialGdpAtPeriodOpen;

            float contractionPerDay = Mathf.Pow(contraction, MacroSliceFractionPerDay);
            state.GDP = Mathf.Max(MinGdp,
                contractionPerDay * priorGdp + attractorTerm * (1f - contractionPerDay) / (1f - contraction));
        }

        // --- Poverty Rate: mean-reverts toward a baseline driven by the same unemployment/inflation gaps that already drive Approval's misery index ---

        /// <summary>Fraction of the gap versus this turn's baseline that closes each turn on its own - moderate-slow, since real poverty rates don't swing wildly turn to turn the way unemployment/inflation can.</summary>
        private const float PovertyReversionSpeed = 0.15f;

        /// <summary>Poverty-baseline points added per percentage point unemployment sits above NAIRU - unemployment is the more direct driver of poverty (lost income), so this is the larger of the two sensitivities.</summary>
        private const float PovertyUnemploymentSensitivity = 0.8f;

        /// <summary>Poverty-baseline points added per percentage point inflation sits away from target (either direction, like Approval's own misery index) - inflation erodes real income too, but less directly than unemployment.</summary>
        private const float PovertyInflationSensitivity = 0.3f;

        /// <summary>Gameplay ceiling/floor - a percentage, like Unemployment/Inflation, not a raw 0-1 fraction.</summary>
        private const float MaxPovertyRatePercent = 100f;

        private const float UbiPovertyReductionSensitivity = 8f;
        private const float NegativeIncomeTaxPovertyReductionSensitivity = 7f;
        private const float MeansTestedWelfarePovertyReductionSensitivity = 7.5f;
        private const float UniversalHealthcarePovertyReductionSensitivity = 4f;
        private const float HousingAssistancePovertyReductionSensitivity = 3f;
        private const float ChildcareSubsidiesPovertyReductionSensitivity = 3f;

        /// <summary>
        /// Poverty-baseline points reduced per point MinimumWagePercentOfMedian sits above the
        /// country's own BaselineMinimumWagePercentOfMedian (see GetMinimumWageUnemploymentAdjustment
        /// for why the gap is versus a country-specific anchor, not a universal constant). Smaller
        /// than the welfare programs' own sensitivities above - directionally grounded by the CBO's
        /// 2019 finding that a federal $15/hr minimum wage would lift roughly as many people out of
        /// poverty as it cost in jobs (~1.3 million each), a modest effect since a minimum wage only
        /// reaches low-wage workers, not the whole poor population the way a direct transfer does.
        /// </summary>
        internal const float MinimumWagePovertyReductionSensitivity = 5f;

        /// <summary>
        /// PovertyRate-points-per-100%-GenerosityLevel each WelfareProgramType reduces the poverty
        /// baseline by. UBI/MeansTestedWelfare are the strongest (direct income transfers); NIT is
        /// nearly as strong as UBI but at less than half UBI's CostShareOfGdp - deliberately more
        /// cost-efficient per point of poverty reduction, reflecting the real economic argument that
        /// targeted transfers move the needle on poverty more efficiently per dollar than universal
        /// ones (efficiency = sensitivity/CostShareOfGdp: NIT 7/8=0.875 vs UBI 8/18=0.444 vs
        /// MeansTestedWelfare 7.5/6=1.25, the most cost-efficient of the three, consistent with
        /// targeting being the most efficient - if also the most politically contentious - lever
        /// real welfare-policy debates raise). UniversalHealthcare/HousingAssistance/
        /// ChildcareSubsidies are modest, matching the task's own framing.
        /// </summary>
        internal static float GetPovertyReductionSensitivity(WelfareProgramType type)
        {
            switch (type)
            {
                case WelfareProgramType.UBI: return UbiPovertyReductionSensitivity;
                case WelfareProgramType.NegativeIncomeTax: return NegativeIncomeTaxPovertyReductionSensitivity;
                case WelfareProgramType.MeansTestedWelfare: return MeansTestedWelfarePovertyReductionSensitivity;
                case WelfareProgramType.UniversalHealthcare: return UniversalHealthcarePovertyReductionSensitivity;
                case WelfareProgramType.HousingAssistance: return HousingAssistancePovertyReductionSensitivity;
                case WelfareProgramType.ChildcareSubsidies: return ChildcareSubsidiesPovertyReductionSensitivity;
                default: return 0f;
            }
        }

        /// <summary>
        /// PovertyRate mean-reverts toward a baseline of Country.BaselinePovertyRate (the country's
        /// own structural, real-OECD-sourced "steady-state" rate) adjusted by the SAME
        /// unemployment/inflation gaps that already drive ApplyApprovalRating's misery index (gaps
        /// versus NAIRU/target, not absolute levels - a healthy economy at its own structural
        /// equilibrium shouldn't show elevated poverty just for having nonzero unemployment/inflation),
        /// minus the combined reduction from any implemented Country.WelfarePrograms (see
        /// GetPovertyReductionSensitivity), minus the Health &amp; Social Affairs Cabinet minister's
        /// passive competence bias, if one is appointed (Political Systems Overhaul Part A - see
        /// CabinetSystem.GetCompetenceBias; folded in as one more reduction term alongside
        /// welfareReduction/minimumWageReduction, landing inside the SAME final Clamp(0, 100) that
        /// already serves as this stat's combined ceiling). Hard-clamped to [0, 100].
        /// </summary>
        public static void ApplyPovertyRate(Country country, float reversionSpeed = PovertyReversionSpeed)
        {
            EconomyState state = country.State;
            float unemploymentGap = state.Unemployment - country.NaturalUnemploymentRate;
            float inflationGap = Mathf.Abs(state.Inflation - TaylorRule.InflationTarget);
            float baseline = country.BaselinePovertyRate
                + PovertyUnemploymentSensitivity * unemploymentGap
                + PovertyInflationSensitivity * inflationGap;

            float welfareReduction = 0f;
            foreach (WelfareProgram program in country.WelfarePrograms)
            {
                if (!program.IsImplemented)
                {
                    continue;
                }

                welfareReduction += GetPovertyReductionSensitivity(program.Type) * (program.GenerosityLevel / 100f);
            }

            float minimumWageReduction = 0f;
            if (country.MinimumWageImplemented)
            {
                float minimumWageGap = country.MinimumWagePercentOfMedian - country.BaselineMinimumWagePercentOfMedian;
                minimumWageReduction = MinimumWagePovertyReductionSensitivity * minimumWageGap / 100f;
            }

            float healthSocialAffairsCompetenceBias = CabinetSystem.GetCompetenceBias(country, CabinetPortfolio.HealthSocialAffairs);

            float target = baseline - welfareReduction - minimumWageReduction - healthSocialAffairsCompetenceBias;
            state.PovertyRate = Mathf.Clamp(state.PovertyRate + reversionSpeed * (target - state.PovertyRate), 0f, MaxPovertyRatePercent);
        }

        // --- Labor Force Participation Rate: a tracked stat, mean-reverting toward its own baseline ---

        /// <summary>Fraction of the gap versus the baseline that closes each turn on its own - moderate-slow, matching PovertyRate's own reversion speed (real participation rates don't swing wildly turn to turn either).</summary>
        private const float LaborForceParticipationReversionSpeed = 0.15f;

        /// <summary>
        /// LaborForceParticipationRate points reduced per percentage point Unemployment sits above
        /// NaturalUnemploymentRate (and, symmetrically, added when Unemployment sits below it) - the
        /// discouraged/encouraged-worker effect, reusing the same unemployment gap that already
        /// drives ApplyApprovalRating's misery index and ApplyPovertyRate's baseline rather than
        /// inventing a new driver.
        /// </summary>
        private const float DiscouragedWorkerSensitivity = 0.3f;

        /// <summary>Gameplay bound - a percentage, like Unemployment/PovertyRate, not a raw 0-1 fraction.</summary>
        private const float MaxLaborForceParticipationPercent = 100f;

        /// <summary>
        /// LaborForceParticipationRate mean-reverts toward Country.BaselineLaborForceParticipationRate
        /// (the country's own structural, real-World-Bank/OECD-sourced "steady-state" rate), adjusted
        /// by the same Unemployment-versus-NAIRU gap already used elsewhere (a proven, ALREADY-
        /// established driver - see ApplyPovertyRate/ApplyApprovalRating - kept OUTSIDE the combined
        /// ceiling below, the same way PotentialGrowthRate's own combined ceiling leaves
        /// BasePotentialGrowthRate itself outside it). Also targeted by two deeper-labor-market policy
        /// levers (see "Deeper Labor Market Policies" in CLAUDE.md): paid family leave (a gap versus
        /// the country's own seeded baseline, the same idiom MinimumWage's employment effect uses) and
        /// workforce retraining (a gap versus the shared neutral 50, the same idiom Police Funding/
        /// Sentencing Severity use). Hard-clamped to [0, 100].
        /// </summary>
        internal const float PaidFamilyLeaveParticipationSensitivity = 0.02f;
        internal const float RetrainingParticipationSensitivity = 0.01f;

        /// <summary>Round 3 item 5, Part A: LaborForceParticipationRate points reduced per point DependencyRatio sits above its own Country.BaselineDependencyRatio - a real, well-documented effect: an aging population structurally shrinks the working-age share, lowering participation even with no change in any individual's own behavior.</summary>
        internal const float DependencyRatioParticipationSensitivity = 0.02f;

        /// <summary>Round 3 item 5, Part A: LaborForceParticipationRate points added per point NetMigrationRate sits above its own Country.BaselineNetMigrationRate - a real, well-documented effect: immigrants skew disproportionately working-age, so higher net migration than a country's own starting norm raises participation.</summary>
        internal const float NetMigrationParticipationSensitivity = 0.03f;

        /// <summary>
        /// Round 3 item 5, Part A: combined ceiling on the SUM of every term that writes to
        /// LaborForceParticipationRate's target DIRECTLY - paid leave, retraining, and the two new
        /// demographic terms (dependency ratio, net migration). Verified by direct audit (not assumed)
        /// that this is the COMPLETE set of direct writers: minimum wage, overtime regulation, and
        /// childcare subsidies do NOT write to this variable at all, direct or otherwise - all three
        /// only affect Unemployment (GetMinimumWageUnemploymentAdjustment/GetOvertimeUnemploymentAdjustment/
        /// GetWelfareAdjustedReversionSpeed in ApplyOkunsLaw), a genuinely separate variable with its
        /// own independent hard clamp ([0, MaxUnemploymentPercent]). This ceiling clamps the SUM of the
        /// four DIRECT terms, not each source individually, the same seriousness PotentialGrowthRate's
        /// own MaxTotalPotentialGrowthAdjustment ceiling got in "Sector Integration" - so no combination
        /// of the FOUR DIRECT levers (now or once Part B's Immigration Policy makes the migration term
        /// genuinely large) can stack past one sane bound. The Unemployment-gap term (DiscouragedWorkerSensitivity)
        /// is deliberately OUTSIDE this ceiling, mirroring how PotentialGrowthRate's own ceiling leaves
        /// BasePotentialGrowthRate itself outside it - it is not itself a policy lever, it's a
        /// pass-through of whatever Unemployment level results from ALL of Unemployment's own drivers
        /// (growth gap, minimum wage, overtime, retraining's OWN separate Unemployment term, UBI/
        /// childcare's reversion-speed nudge, sector employment), each already bounded by Unemployment's
        /// own [0, MaxUnemploymentPercent] clamp rather than needing a second, redundant ceiling here -
        /// the same "each variable owns its own hard bound" pattern this project uses throughout, not a
        /// gap in this ceiling's coverage. Note RetrainingProgramLevel specifically feeds
        /// LaborForceParticipationRate through BOTH this direct term AND, independently, its own
        /// pre-existing Unemployment term (GetRetrainingUnemploymentAdjustment, from "Deeper Labor
        /// Market Policies," predating this task) - a real second-order reinforcement of the same lever
        /// through two channels, but not something this task introduced or that requires fixing here.
        /// </summary>
        private const float MaxLaborForceParticipationAdjustment = 1.0f;

        public static void ApplyLaborForceParticipationRate(Country country, float reversionSpeed = LaborForceParticipationReversionSpeed)
        {
            EconomyState state = country.State;
            float unemploymentGap = state.Unemployment - country.NaturalUnemploymentRate;
            float paidLeaveGap = country.PaidFamilyLeaveWeeks - country.BaselinePaidFamilyLeaveWeeks;
            float retrainingGap = country.RetrainingProgramLevel - NeutralPolicyDialLevel;
            float dependencyGap = state.DependencyRatio - country.BaselineDependencyRatio;
            float netMigrationGap = state.NetMigrationRate - country.BaselineNetMigrationRate;

            float combinedAdjustment = PaidFamilyLeaveParticipationSensitivity * paidLeaveGap
                + RetrainingParticipationSensitivity * retrainingGap
                - DependencyRatioParticipationSensitivity * dependencyGap
                + NetMigrationParticipationSensitivity * netMigrationGap;
            combinedAdjustment = Mathf.Clamp(combinedAdjustment, -MaxLaborForceParticipationAdjustment, MaxLaborForceParticipationAdjustment);

            float target = country.BaselineLaborForceParticipationRate
                - DiscouragedWorkerSensitivity * unemploymentGap
                + combinedAdjustment;
            state.LaborForceParticipationRate = Mathf.Clamp(
                state.LaborForceParticipationRate + reversionSpeed * (target - state.LaborForceParticipationRate),
                0f, MaxLaborForceParticipationPercent);
        }

        // --- Crime & Justice: a stylized CrimeIndex, mean-reverting toward its own baseline ---

        /// <summary>Fraction of the gap versus the target that closes each turn on its own - matches PovertyRate/LaborForceParticipationRate's own moderate-slow reversion speed.</summary>
        private const float CrimeIndexReversionSpeed = 0.15f;

        /// <summary>CrimeIndex points added per point Unemployment sits above NaturalUnemploymentRate - reuses an already-proven driver (the same gap PovertyRate/ApplyApprovalRating already use) rather than inventing a new one; property crime's real-world link to joblessness is well documented, though modest relative to policy's own effect below.</summary>
        private const float CrimeUnemploymentSensitivity = 0.3f;

        /// <summary>CrimeIndex points reduced per point Country.PoliceFundingLevel sits above its neutral 50 (and increased per point below) - a real, well-documented deterrence/response-capacity effect. The larger of the two policy sensitivities - see SentencingSensitivity.</summary>
        internal const float PoliceFundingSensitivity = 0.16f;

        /// <summary>CrimeIndex points reduced per point Country.SentencingSeverity sits above its neutral 50 - deliberately HALF of PoliceFundingSensitivity, reflecting the well-established criminology finding (Nagin and others) that the CERTAINTY of enforcement deters crime more reliably than the SEVERITY of punishment, which has a smaller, more debated effect.</summary>
        internal const float SentencingSensitivity = 0.08f;

        /// <summary>Neutral reference point for both policy dials - both start here for every country (a uniform placeholder, unlike CrimeIndex's own per-country baseline), so a gap versus this constant (not a country-specific anchor) is the correct comparison.</summary>
        private const float NeutralPolicyDialLevel = 50f;

        private const float MaxCrimeIndexPercent = 100f;

        // --- Round 3 item 3: Organized Crime and Corruption, two more stylized 0-100 tracked stats ---
        // ApplyOrganizedCrimeIndex/ApplyCorruptionIndex must run BEFORE ApplyCrimeIndex (see below) so
        // its own OrganizedCrimeIndex term reads THIS turn's freshly-updated value, the same
        // "must see this turn's just-updated value" timing requirement Infrastructure Feedback's
        // condition-drag already established - see SimulationManager's call ordering.

        /// <summary>Fraction of the gap versus the target that closes each turn on its own - matches CrimeIndex/PovertyRate's own moderate-slow reversion speed.</summary>
        private const float OrganizedCrimeReversionSpeed = 0.15f;

        /// <summary>OrganizedCrimeIndex points reduced per point Country.PoliceFundingLevel sits above its neutral 50 (and increased per point below) - policing already fights organized crime in reality, reusing this existing lever rather than requiring a brand-new one for this specific link. Smaller than its own primary levers below - a secondary contributor.</summary>
        internal const float PoliceFundingOrganizedCrimeSensitivity = 0.06f;

        /// <summary>OrganizedCrimeIndex points reduced per point Country.BorderEnforcementLevel sits above its neutral 50 (and increased per point below) - stricter border enforcement disrupts cross-border smuggling/trafficking, organized crime's real, well-documented core activity. The primary lever for this stat.</summary>
        internal const float BorderEnforcementOrganizedCrimeSensitivity = 0.12f;

        /// <summary>OrganizedCrimeIndex points reduced per point Country.JudicialFundingLevel sits above its neutral 50 (and increased per point below) - better-funded prosecution capacity disrupts organized-crime networks, a real secondary contributor alongside BorderEnforcementLevel's more direct effect.</summary>
        internal const float JudicialFundingOrganizedCrimeSensitivity = 0.06f;

        /// <summary>
        /// OrganizedCrimeIndex mean-reverts toward a target of Country.BaselineOrganizedCrimeIndex,
        /// adjusted by how far PoliceFundingLevel/BorderEnforcementLevel/JudicialFundingLevel sit from
        /// their shared neutral 50 - all three reduce the target when above it. Hard-clamped to
        /// [0, 100], the same scale as CrimeIndex.
        /// </summary>
        public static void ApplyOrganizedCrimeIndex(Country country, float reversionSpeed = OrganizedCrimeReversionSpeed)
        {
            EconomyState state = country.State;
            float target = country.BaselineOrganizedCrimeIndex
                - PoliceFundingOrganizedCrimeSensitivity * (country.PoliceFundingLevel - NeutralPolicyDialLevel)
                - BorderEnforcementOrganizedCrimeSensitivity * (country.BorderEnforcementLevel - NeutralPolicyDialLevel)
                - JudicialFundingOrganizedCrimeSensitivity * (country.JudicialFundingLevel - NeutralPolicyDialLevel);

            state.OrganizedCrimeIndex = Mathf.Clamp(state.OrganizedCrimeIndex + reversionSpeed * (target - state.OrganizedCrimeIndex), 0f, MaxCrimeIndexPercent);
        }

        /// <summary>Fraction of the gap versus the target that closes each turn on its own - matches CrimeIndex/PovertyRate's own moderate-slow reversion speed.</summary>
        private const float CorruptionReversionSpeed = 0.15f;

        /// <summary>CorruptionIndex points reduced per point Country.JudicialFundingLevel sits above its neutral 50 (and increased per point below) - an independent, well-funded judiciary is a canonical real-world anti-corruption mechanism. The sole lever for this stat in this pass.</summary>
        internal const float JudicialFundingCorruptionSensitivity = 0.14f;

        /// <summary>
        /// CorruptionIndex mean-reverts toward a target of Country.BaselineCorruptionIndex, adjusted
        /// by how far JudicialFundingLevel sits from its neutral 50 - higher funding reduces the
        /// target. Hard-clamped to [0, 100], the same scale as CrimeIndex.
        /// </summary>
        public static void ApplyCorruptionIndex(Country country, float reversionSpeed = CorruptionReversionSpeed)
        {
            EconomyState state = country.State;
            float target = country.BaselineCorruptionIndex
                - JudicialFundingCorruptionSensitivity * (country.JudicialFundingLevel - NeutralPolicyDialLevel);

            state.CorruptionIndex = Mathf.Clamp(state.CorruptionIndex + reversionSpeed * (target - state.CorruptionIndex), 0f, MaxCrimeIndexPercent);
        }

        // --- ROUND 4 BATCH 1 (C3): Youth unemployment and life expectancy - inputs-only tracked stats ---
        // Both READ existing state and WRITE nothing back (the Round 4 standing first-pass rule,
        // ruled 2026-08-16) - neither enters any existing combined ceiling. Both are reverting
        // quantities and take the taxonomy's standard shapes: parameterized turn form (whose only
        // caller is the equivalence check) + PerDayReversion daily wrapper, daily-native from day one.

        /// <summary>Youth unemployment points per point of headline unemployment gap versus NAIRU -
        /// the well-documented youth-cyclicality multiplier: under-25 unemployment swings roughly
        /// twice as hard as headline through a cycle (directionally grounded, not precisely fitted,
        /// like every sensitivity in this file). This is the ONE channel every existing lever reaches
        /// the stat through.</summary>
        private const float YouthUnemploymentCyclicalSensitivity = 2f;

        /// <summary>Faster than the structural drifts, slower than headline's own reversion - youth
        /// labour markets churn quickly, but the stat still follows the cycle rather than snapping.</summary>
        private const float YouthUnemploymentReversionSpeed = 0.3f;

        /// <summary>Generous gameplay ceiling - southern-Europe crisis peaks reached the high 50s
        /// (Spain 55.5% in 2013, Greece ~60%), so 60 is the honest historical extreme, not a guess.</summary>
        private const float MaxYouthUnemploymentPercent = 60f;

        /// <summary>Reverts EconomyState.YouthUnemployment toward the country's structural baseline
        /// plus the amplified headline-unemployment gap, minus the Education Cabinet minister's
        /// passive competence bias if one is appointed (ROUND 4 BATCH R4-4, ruling R3 - the
        /// ApplyPovertyRate idiom exactly: a target-side reduction term at point-of-use, landing
        /// inside the same final clamp that already serves as this stat's ceiling; the "youth
        /// retraining" follow-on the R4-1 record itself named). Inputs-only otherwise: reads
        /// Unemployment/NAIRU and the appointment, writes only itself - and youth-U still has no
        /// downstream readers, so the term's rule-11 ceiling audit is empty.</summary>
        public static void ApplyYouthUnemployment(Country country, float reversionSpeed = YouthUnemploymentReversionSpeed)
        {
            EconomyState state = country.State;
            float unemploymentGap = state.Unemployment - country.NaturalUnemploymentRate;
            float target = country.BaselineYouthUnemploymentRate
                + YouthUnemploymentCyclicalSensitivity * unemploymentGap
                - CabinetSystem.GetCompetenceBias(country, CabinetPortfolio.Education);
            state.YouthUnemployment = Mathf.Clamp(
                state.YouthUnemployment + reversionSpeed * (target - state.YouthUnemployment),
                0f, MaxYouthUnemploymentPercent);
        }

        /// <summary>Years of life expectancy lost per point PovertyRate sits above the country's own
        /// baseline - the poverty-mortality link is among the best-documented social gradients; the
        /// scale here is modest (5 points of excess poverty costs 0.4 years).</summary>
        private const float LifeExpectancyPovertySensitivity = 0.08f;

        /// <summary>Years added at FULL generosity of an implemented UniversalHealthcare program -
        /// universal-coverage literature puts the effect at one to two years; 1.5 sits mid-range,
        /// scaled by GenerosityLevel like every welfare effect.</summary>
        private const float LifeExpectancyHealthcareSensitivity = 1.5f;

        /// <summary>Generational reversion - the same "slowest-moving quantities in the model" reasoning
        /// PopulationGrowthReversionSpeed documents; life expectancy moves over decades, not quarters.</summary>
        private const float LifeExpectancyReversionSpeed = 0.05f;

        private const float MinLifeExpectancyYears = 60f;
        private const float MaxLifeExpectancyYears = 95f;

        /// <summary>Reverts EconomyState.LifeExpectancy toward the structural baseline, dragged by
        /// excess poverty and lifted by an implemented UniversalHealthcare program. Inputs-only.
        ///
        /// ⚠ Zero-gap seeding leans on every country starting with UniversalHealthcare
        /// UNIMPLEMENTED (WorldFactory's welfare block): the seeded real-world figures already
        /// contain each country's actual health system, so the baseline absorbs it and the lift
        /// models a NEW program on top. If countries ever start with the program implemented,
        /// their BaselineLifeExpectancy must absorb the starting lift or every seeded value
        /// drifts upward from turn one with no player action.</summary>
        public static void ApplyLifeExpectancy(Country country, float reversionSpeed = LifeExpectancyReversionSpeed)
        {
            EconomyState state = country.State;
            float povertyGap = Mathf.Max(0f, state.PovertyRate - country.BaselinePovertyRate);

            float healthcareLift = 0f;
            foreach (WelfareProgram program in country.WelfarePrograms)
            {
                if (program.IsImplemented && program.Type == WelfareProgramType.UniversalHealthcare)
                {
                    healthcareLift = LifeExpectancyHealthcareSensitivity * (program.GenerosityLevel / 100f);
                    break;
                }
            }

            float target = country.BaselineLifeExpectancy - LifeExpectancyPovertySensitivity * povertyGap + healthcareLift;
            state.LifeExpectancy = Mathf.Clamp(
                state.LifeExpectancy + reversionSpeed * (target - state.LifeExpectancy),
                MinLifeExpectancyYears, MaxLifeExpectancyYears);
        }

        private static readonly float YouthUnemploymentReversionSpeedPerDay = PerDayReversion(YouthUnemploymentReversionSpeed);
        private static readonly float LifeExpectancyReversionSpeedPerDay = PerDayReversion(LifeExpectancyReversionSpeed);

        public static void ApplyYouthUnemploymentDaily(Country country) => ApplyYouthUnemployment(country, YouthUnemploymentReversionSpeedPerDay);
        public static void ApplyLifeExpectancyDaily(Country country) => ApplyLifeExpectancy(country, LifeExpectancyReversionSpeedPerDay);

        // --- ROUND 4 BATCH 2 (C2): Gini and the real wage index - inputs-only tracked stats ---
        // Both READ existing state and WRITE nothing back, per the standing Round 4 first-pass rule.
        // Gini is a reverting quantity on the PovertyRate idiom exactly (baseline + slack push,
        // minus redistribution pulls); the real wage index is a COMPOUNDING quantity on the
        // PotentialGDP idiom (annual-rate POWER SLICE), because a wage level is a stock of
        // accumulated growth, not a gap closing toward an anchor.

        /// <summary>Gini points added per point Unemployment sits above NAIRU - recessions raise
        /// measured inequality modestly (job loss concentrates at the bottom of the distribution);
        /// directionally grounded, deliberately small against the policy pulls below.</summary>
        private const float GiniUnemploymentSensitivity = 0.4f;

        /// <summary>Gini points removed per point of income-tax rate above the country's own seeded
        /// rate (SIGNED - cutting below the seed raises inequality by the same coefficient).
        /// Modest by design: marginal-rate changes move measured Gini slowly, and the strong
        /// redistribution levers are the transfer programs, matching the real pre/post-transfer
        /// decomposition where transfers do most of the work.</summary>
        private const float GiniIncomeTaxSensitivity = 0.08f;

        /// <summary>Gini points removed per 100 points of MinimumWagePercentOfMedian above the
        /// country's own anchor - a wage floor compresses the bottom of the distribution; smaller
        /// than the transfer programs for the same only-reaches-low-wage-workers reason
        /// MinimumWagePovertyReductionSensitivity documents.</summary>
        private const float GiniMinimumWageSensitivity = 2f;

        /// <summary>Slower than PovertyRate's 0.15 - inequality is a structural distribution, and
        /// real national Ginis move by tenths of a point per year outside genuine upheavals.</summary>
        private const float GiniReversionSpeed = 0.1f;

        /// <summary>Historical extremes as honest gameplay bounds: no national equivalised-income
        /// Gini sits below ~20 (the Nordics' floor era) or above ~65 (South Africa ~63, the recorded
        /// world maximum).</summary>
        private const float MinGiniPercent = 15f;
        private const float MaxGiniPercent = 65f;

        /// <summary>Gini-points-per-100%-GenerosityLevel each program removes from the baseline -
        /// the GetPovertyReductionSensitivity idiom at roughly half scale, because the Gini range in
        /// this set is compressed (26.0-39.5) where poverty spans wider: direct transfers (UBI/NIT/
        /// MeansTested) do the heavy redistribution, the in-kind three are modest.</summary>
        internal static float GetGiniReductionSensitivity(WelfareProgramType type)
        {
            switch (type)
            {
                case WelfareProgramType.UBI: return 4f;
                case WelfareProgramType.NegativeIncomeTax: return 3.5f;
                case WelfareProgramType.MeansTestedWelfare: return 3f;
                case WelfareProgramType.UniversalHealthcare: return 1f;
                case WelfareProgramType.HousingAssistance: return 1f;
                case WelfareProgramType.ChildcareSubsidies: return 1f;
                default: return 0f;
            }
        }

        /// <summary>Reverts EconomyState.Gini toward the structural baseline pushed by labour-market
        /// slack and pulled by welfare programs, income tax above its seeded anchor
        /// (Country.BaselineIncomeTaxRate) and a minimum wage above its own anchor. Inputs-only.
        /// Zero-gap at world creation: gap 0, no program implemented, tax at seed, wage at anchor.</summary>
        public static void ApplyGini(Country country, float reversionSpeed = GiniReversionSpeed)
        {
            EconomyState state = country.State;
            float unemploymentGap = state.Unemployment - country.NaturalUnemploymentRate;

            float welfareReduction = 0f;
            foreach (WelfareProgram program in country.WelfarePrograms)
            {
                if (!program.IsImplemented)
                {
                    continue;
                }

                welfareReduction += GetGiniReductionSensitivity(program.Type) * (program.GenerosityLevel / 100f);
            }

            // Effective rate 0 when the line is removed entirely - abolishing the income tax raises
            // inequality through the same signed coefficient, which is the correct direction.
            float incomeTaxRate = 0f;
            foreach (TaxLine line in country.TaxLines)
            {
                if (line.IsImplemented && line.Type == TaxType.IncomeTax)
                {
                    incomeTaxRate = line.Rate;
                    break;
                }
            }
            float taxReduction = GiniIncomeTaxSensitivity * (incomeTaxRate - country.BaselineIncomeTaxRate);

            float minimumWageReduction = 0f;
            if (country.MinimumWageImplemented)
            {
                minimumWageReduction = GiniMinimumWageSensitivity
                    * (country.MinimumWagePercentOfMedian - country.BaselineMinimumWagePercentOfMedian) / 100f;
            }

            float target = country.BaselineGini
                + GiniUnemploymentSensitivity * unemploymentGap
                - welfareReduction - taxReduction - minimumWageReduction;
            state.Gini = Mathf.Clamp(state.Gini + reversionSpeed * (target - state.Gini), MinGiniPercent, MaxGiniPercent);
        }

        /// <summary>Real wage growth tracks trend productivity one-for-one at equilibrium - the
        /// textbook long-run relation, and the reason the index needs NO per-country growth seed:
        /// PotentialGrowthRate already differentiates the six countries.</summary>
        private const float RealWageProductivityPassThrough = 1f;

        /// <summary>Per-turn growth points per point Unemployment sits BELOW NAIRU - the wage
        /// Phillips channel: tight labour markets bid wages up, slack suppresses them.</summary>
        private const float RealWageTightnessSensitivity = 0.3f;

        /// <summary>Per-turn growth points lost per point actual Inflation runs above
        /// InflationExpectations - settlements anchor to expectations, so SURPRISE inflation erodes
        /// real wages until they catch up. SIGNED deliberately: disinflation surprise boosts real
        /// wages by the same channel, which is exactly the seed doc's own Poland 2024 story
        /// ("strong nominal growth plus rapidly falling inflation").</summary>
        private const float RealWageInflationErosionSensitivity = 0.3f;

        /// <summary>Safety clamp on the per-turn growth rate, not on the index level - the index is
        /// unbounded by construction (display furniture per the ruling); clamps never scale.</summary>
        private const float MaxRealWageGrowthPerTurnPercent = 10f;
        private const float MinRealWageIndex = 1f;

        /// <summary>Compounds EconomyState.RealWageIndex by the period's real wage growth: trend
        /// pass-through + labour-market tightness − inflation surprise. Inputs-only: reads
        /// PotentialGrowthRate/Unemployment/NAIRU/Inflation/InflationExpectations, writes only
        /// itself. The LEVEL means nothing across countries (base 100 at epoch per country, by
        /// ruling); only growth is consumed and displayed with meaning.
        /// <paramref name="sliceExponent"/> is 1 for the turn form (equivalence's caller) and
        /// MacroSliceFractionPerDay for the daily wrapper - the annual-rate POWER SLICE, exact by
        /// telescoping at constant inputs.</summary>
        public static void ApplyRealWageIndex(Country country, float sliceExponent = 1f)
        {
            EconomyState state = country.State;
            float growthPerTurnPercent = Mathf.Clamp(
                RealWageProductivityPassThrough * country.PotentialGrowthRate
                + RealWageTightnessSensitivity * (country.NaturalUnemploymentRate - state.Unemployment)
                - RealWageInflationErosionSensitivity * (state.Inflation - state.InflationExpectations),
                -MaxRealWageGrowthPerTurnPercent, MaxRealWageGrowthPerTurnPercent);
            state.RealWageIndex = Mathf.Max(MinRealWageIndex,
                state.RealWageIndex * Mathf.Pow(1f + growthPerTurnPercent / 100f, sliceExponent));
        }

        private static readonly float GiniReversionSpeedPerDay = PerDayReversion(GiniReversionSpeed);

        public static void ApplyGiniDaily(Country country) => ApplyGini(country, GiniReversionSpeedPerDay);
        public static void ApplyRealWageIndexDaily(Country country) => ApplyRealWageIndex(country, MacroSliceFractionPerDay);

        // --- ROUND 4 BATCH 3 (C1): housing - overburden, homeownership, house prices ---
        // Inputs-only per the standing rule, and THE ARC'S FIRST MONETARY COUPLING, stated
        // explicitly for the mechanism report's namespace claim: all three stats READ
        // country.CurrencyZone.InterestRate (the live policy rate) against
        // CurrencyZone.HousingRateAnchor (the zone's rate at epoch; inert-fallback on pre-R4-3
        // saves) and WRITE NOTHING back to the rate, the zone, or any monetary quantity. One-way
        // by construction, same as PublishedData's read/write split. Rate-sensitivity is C1's
        // design feature, not a side effect.
        //
        // ⚠ THE DELIBERATE ASYMMETRY: overburden runs for the EU five only
        // (Country.TracksHousingOverburden; the USA has no source-comparable figure and takes
        // homeownership as primary, per the recorded ruling). The early-out below, the missing UI
        // row, the seed comments and the equivalence check's USA-unmoved assert all state the same
        // fact - anywhere it appears without explanation would read as a gap, so it never does.

        /// <summary>Overburden points added per point the policy rate sits above the zone's epoch
        /// anchor - mortgage and rent costs track rates with force; this is the strongest of the
        /// three rate sensitivities because the >40%-of-income threshold is exactly where rate
        /// pass-through bites (directionally grounded like every sensitivity here).</summary>
        private const float OverburdenRateSensitivity = 1.5f;

        /// <summary>Overburden points removed per 100% GenerosityLevel of an implemented
        /// HousingAssistance program - the program's own dedicated stat, stronger than its poverty
        /// side-effect (3) because housing support reaches the housing-cost margin directly.</summary>
        private const float OverburdenHousingAssistanceSensitivity = 4f;

        /// <summary>Moderate-slow, the PovertyRate class - housing stress follows rate moves within
        /// quarters (mortgage resets, rent renewals), faster than tenure change, slower than markets.</summary>
        private const float HousingOverburdenReversionSpeed = 0.15f;

        /// <summary>Greece 28.9 is the recorded EU maximum - 50 is the honest generous ceiling.</summary>
        private const float MaxHousingOverburdenPercent = 50f;

        /// <summary>Homeownership points lost per point of rate above the epoch anchor - higher
        /// rates price buyers out; smaller than overburden's sensitivity and much slower to arrive
        /// (see the reversion speed), because tenure is a stock that turns over in years.</summary>
        private const float HomeownershipRateSensitivity = 0.5f;

        /// <summary>Homeownership points added per 100% GenerosityLevel of an implemented
        /// HousingAssistance program - deposit/purchase support at the margin, modest.</summary>
        private const float HomeownershipHousingAssistanceSensitivity = 2f;

        /// <summary>Generational, the LifeExpectancy class - a housing stock's tenure mix moves
        /// over decades.</summary>
        private const float HomeownershipReversionSpeed = 0.05f;

        /// <summary>Germany's real 41.0 is the low structural outlier and Poland's 86.8 the high -
        /// [10, 95] leaves honest room beyond both without being reachable in practice.</summary>
        private const float MinHomeownershipPercent = 10f;
        private const float MaxHomeownershipPercent = 95f;

        /// <summary>Reverts EconomyState.HousingOverburden toward the structural baseline plus the
        /// rate-gap push, minus HousingAssistance relief. EARLY-OUTS where the country does not
        /// track the stat (the USA) - see the asymmetry note on the section header.</summary>
        public static void ApplyHousingOverburden(Country country, float reversionSpeed = HousingOverburdenReversionSpeed)
        {
            if (!country.TracksHousingOverburden)
            {
                return;
            }

            EconomyState state = country.State;
            float rateGap = country.CurrencyZone.InterestRate - country.CurrencyZone.HousingRateAnchor;
            float target = country.BaselineHousingOverburden
                + OverburdenRateSensitivity * rateGap
                - OverburdenHousingAssistanceSensitivity * GetImplementedGenerosityFraction(country, WelfareProgramType.HousingAssistance);
            state.HousingOverburden = Mathf.Clamp(
                state.HousingOverburden + reversionSpeed * (target - state.HousingOverburden),
                0f, MaxHousingOverburdenPercent);
        }

        /// <summary>Reverts EconomyState.Homeownership toward the structural baseline minus the
        /// rate-gap drag, plus HousingAssistance support. Runs for all six countries - this is the
        /// USA's PRIMARY housing metric per the ruling.</summary>
        public static void ApplyHomeownership(Country country, float reversionSpeed = HomeownershipReversionSpeed)
        {
            EconomyState state = country.State;
            float rateGap = country.CurrencyZone.InterestRate - country.CurrencyZone.HousingRateAnchor;
            float target = country.BaselineHomeownership
                - HomeownershipRateSensitivity * rateGap
                + HomeownershipHousingAssistanceSensitivity * GetImplementedGenerosityFraction(country, WelfareProgramType.HousingAssistance);
            state.Homeownership = Mathf.Clamp(
                state.Homeownership + reversionSpeed * (target - state.Homeownership),
                MinHomeownershipPercent, MaxHomeownershipPercent);
        }

        /// <summary>House-price growth tracks trend growth one-for-one long-run (the same
        /// income-tracking argument RealWageIndex documents - and the same reason the index needs
        /// no per-country growth seed).</summary>
        private const float HpiTrendPassThrough = 1f;

        /// <summary>Per-turn growth points per point the policy rate sits BELOW the epoch anchor -
        /// cheap credit inflates house prices, the best-documented rate channel of the three.
        /// Signed: tightening above the anchor drags prices by the same coefficient.</summary>
        private const float HpiRateSensitivity = 0.5f;

        /// <summary>Same safety clamp shape as the real wage index: growth clamps, the level never
        /// does (unbounded by construction, display furniture per the R4-2 convention).</summary>
        private const float MaxHpiGrowthPerTurnPercent = 10f;
        private const float MinHousePriceIndex = 1f;

        /// <summary>Compounds EconomyState.HousePriceIndex - the R4-2 compounding-index kit
        /// verbatim (power slice via <paramref name="sliceExponent"/>, growth clamp, level floor,
        /// base 100 at epoch, §A.9b display). Growth = trend pass-through + rate gap, SIGNED.</summary>
        public static void ApplyHousePriceIndex(Country country, float sliceExponent = 1f)
        {
            EconomyState state = country.State;
            float rateGap = country.CurrencyZone.InterestRate - country.CurrencyZone.HousingRateAnchor;
            float growthPerTurnPercent = Mathf.Clamp(
                HpiTrendPassThrough * country.PotentialGrowthRate - HpiRateSensitivity * rateGap,
                -MaxHpiGrowthPerTurnPercent, MaxHpiGrowthPerTurnPercent);
            state.HousePriceIndex = Mathf.Max(MinHousePriceIndex,
                state.HousePriceIndex * Mathf.Pow(1f + growthPerTurnPercent / 100f, sliceExponent));
        }

        /// <summary>The one implemented program's GenerosityLevel as a 0-1 fraction, or 0 when not
        /// implemented - the recurring welfare-read shape, extracted because the two housing stats
        /// above both need it for the same program.</summary>
        private static float GetImplementedGenerosityFraction(Country country, WelfareProgramType type)
        {
            foreach (WelfareProgram program in country.WelfarePrograms)
            {
                if (program.IsImplemented && program.Type == type)
                {
                    return program.GenerosityLevel / 100f;
                }
            }

            return 0f;
        }

        private static readonly float HousingOverburdenReversionSpeedPerDay = PerDayReversion(HousingOverburdenReversionSpeed);
        private static readonly float HomeownershipReversionSpeedPerDay = PerDayReversion(HomeownershipReversionSpeed);

        public static void ApplyHousingOverburdenDaily(Country country) => ApplyHousingOverburden(country, HousingOverburdenReversionSpeedPerDay);
        public static void ApplyHomeownershipDaily(Country country) => ApplyHomeownership(country, HomeownershipReversionSpeedPerDay);
        public static void ApplyHousePriceIndexDaily(Country country) => ApplyHousePriceIndex(country, MacroSliceFractionPerDay);

        // --- Demographics: Population, birth/death/migration rates, and a single dependency-ratio aging proxy (Round 3 item 5, Part A) ---

        /// <summary>Points BirthRate declines per turn on its own - a real, well-documented, near-universal secular fertility decline across developed nations, not a country-specific policy response. Deliberately small (over a 500-turn run this alone would move BirthRate by 5 points, well before which the lower-starting countries hit MinBirthRate and stop).</summary>
        private const float BirthRateSecularDeclineRate = 0.01f;

        /// <summary>Realistic low-fertility floor, informed by the real world's own lowest-ever recorded national crude birth rates (some East Asian countries have fallen to roughly this range in recent years) - not literally zero, since no country's birth rate has realistically approached that.</summary>
        private const float MinBirthRate = 5f;

        /// <summary>
        /// Round 3 item 5, Part B: generous upper safety bound on BirthRate - unreachable in Part A
        /// (BirthRate only ever declined), but now needed since FamilyPolicyLevel can push it up.
        /// Comfortably above any modern developed-nation crude birth rate (even historical baby-boom
        /// peaks rarely exceeded the high teens) - a gameplay safety bound, not a realistic ceiling
        /// this lever is expected to actually reach.
        /// </summary>
        private const float MaxBirthRate = 20f;

        /// <summary>
        /// Round 3 item 5, Part B: points BirthRate moves per point Country.FamilyPolicyLevel sits
        /// away from its neutral 50 - at the slider's full extremes (0 or 100) this is +/-1.5 points,
        /// deliberately SMALL: real-world evidence on pro-natalist policy's effect on fertility is
        /// itself small and contested (already flagged honestly in EconomyState.BirthRate's own doc
        /// comment when Part A was written). Feeds directly into the same BirthRate the secular-decline
        /// drift below already updates - no separate channel, so this lever automatically flows
        /// through the same (YearsPerTurn-scaled, capped/reverting) ApplyPopulationGrowth pipeline
        /// every other BirthRate driver already uses.
        /// </summary>
        internal const float FamilyPolicyBirthRateSensitivity = 0.03f;

        /// <summary>Points DeathRate rises per point DependencyRatio sits above its own Country.BaselineDependencyRatio - a real, well-documented mechanical effect: an aging population structurally raises the crude death rate even with no change in age-specific mortality, since a larger share of the population is simply older.</summary>
        private const float DeathRateAgingDriftSensitivity = 0.003f;

        /// <summary>Generous gameplay safety bound on DeathRate - comfortably above any real-world crude death rate this model's own DependencyRatio ceiling could mechanically produce.</summary>
        private const float MaxDeathRate = 25f;

        /// <summary>Points NetMigrationRate rises per point DependencyRatio sits above its own Country.BaselineDependencyRatio - a real, discussed phenomenon: aging developed economies lean more on immigration over time to offset a shrinking working-age population. Deliberately a SEPARATE driver from BirthRate's own independent secular-decline drift - fertility decline isn't itself "caused" by a country's current dependency ratio the way this migration-reliance trend plausibly is.</summary>
        private const float MigrationAgingDriftSensitivity = 0.002f;

        /// <summary>Generous gameplay safety bounds on NetMigrationRate - wide enough for Part B's Immigration Policy lever to swing meaningfully in either direction (open vs. restrictive) without an artificial mid-range ceiling getting in the way first.</summary>
        private const float MinNetMigrationRate = -15f;
        private const float MaxNetMigrationRate = 15f;

        /// <summary>
        /// Round 3 item 5, Part B: points NetMigrationRate moves per point Country.ImmigrationPolicyLevel
        /// sits away from its neutral 50 - at the slider's full extremes (0 or 100) this is +/-5 points,
        /// deliberately WIDER than FamilyPolicyBirthRateSensitivity's swing: immigration policy is a
        /// genuinely more responsive real-world lever than fertility (visa/asylum/quota changes can move
        /// actual migration flows within a single term, unlike birth rates) - see EconomyState.
        /// NetMigrationRate's own doc comment from Part A, which anticipated exactly this. Feeds
        /// directly into the same NetMigrationRate the aging-drift term below already updates - no
        /// separate channel, so this lever automatically flows through the same (YearsPerTurn-scaled)
        /// ApplyPopulationGrowth pipeline AND the existing NetMigrationRate-gap term in
        /// ApplyLaborForceParticipationRate's combined ceiling, rather than adding a second, parallel
        /// immigration-to-labor-force channel - avoiding the double-counting risk this item's own
        /// roadmap brief flagged structurally, not just by convention.
        /// </summary>
        internal const float ImmigrationPolicyNetMigrationSensitivity = 0.1f;

        /// <summary>Points DependencyRatio rises per point the DeathRate-versus-BirthRate gap sits above zero (natural decrease - more deaths than births) - the single derived aging/dependency proxy's own drift mechanism, deliberately simple, not a full age-cohort/population-pyramid model. Never decreases in this pass - real developed-world aging trends are one-directional over any timescale this game's turns plausibly represent.</summary>
        private const float DependencyRatioDriftSensitivity = 0.0015f;

        /// <summary>Sane floor - no real country's old-age dependency ratio has fallen meaningfully below this in the modern era, and this pass has no mechanism that would ever decrease DependencyRatio below its own seeded baseline anyway (this is defense-in-depth, not an actively-reachable bound).</summary>
        private const float MinDependencyRatio = 15f;

        /// <summary>Generous gameplay safety ceiling - well above even the most extreme real-world demographic projections for any of this game's six countries over any realistic time horizon.</summary>
        private const float MaxDependencyRatio = 70f;

        /// <summary>
        /// Evolves BirthRate/DeathRate/NetMigrationRate/DependencyRatio for one turn - BirthRate
        /// declines on its own secular trend; DependencyRatio rises when DeathRate exceeds BirthRate
        /// (natural decrease); DeathRate and NetMigrationRate then both drift further based on how far
        /// DependencyRatio has risen above its own baseline (population aging mechanically raises
        /// crude death rate and, separately, developed economies' real-world reliance on immigration).
        ///
        /// BirthRate/NetMigrationRate are each computed as a policy-INDEPENDENT "natural" trajectory
        /// (EconomyState.NaturalBirthRate/NaturalNetMigrationRate, which only ever see the secular-
        /// decline/aging-drift terms above) plus this turn's FamilyPolicyLevel/ImmigrationPolicyLevel
        /// offset, recomputed FRESH each turn rather than compounded onto the stored rate directly
        /// (Round 3 item 5, Part B). This is deliberate, not incidental: a first version applied the
        /// policy term as a constant per-turn ADDITION to BirthRate/NetMigrationRate themselves, which
        /// ratchets either rate to its hard ceiling within single-digit turns and parks it there for
        /// the rest of the run whenever the slider sits away from neutral - reintroducing, one layer
        /// upstream, the exact "no reversion, runs to an extreme and stays" failure pattern the
        /// Population growth-rate corrections above (see CLAUDE.md) were written to fix. Recomputing
        /// fresh from the natural trajectory instead means holding a slider at any fixed value produces
        /// a constant, bounded shift from the underlying secular trend - not an ever-growing one - while
        /// still being fully responsive if the player changes the slider mid-run.
        ///
        /// Both policy offsets land on BirthRate/NetMigrationRate themselves - the SAME quantities
        /// every other driver here already updates - so they automatically inherit
        /// ApplyPopulationGrowth's YearsPerTurn-scaled, capped/reverting pipeline with no separate
        /// application path. Must run BEFORE ApplyPopulationGrowth, which reads these same-turn
        /// freshly-updated rates - the same "must see this turn's just-updated value" timing
        /// requirement Infrastructure Feedback's condition-drag already established.
        /// </summary>
        public static void ApplyDemographicRates(Country country, float sliceFraction = 1f)
        {
            // CONTINUOUS TIME PHASE 4: `sliceFraction` scales ONLY the four accumulating drift terms
            // (secular decline, dependency drift, the two aging drifts) - they are flows with no
            // target, so they take the LINEAR transform (the ApplyCrimeEffects precedent). The two
            // policy SENSITIVITIES take no transform at all: each maps a slider LEVEL to a standing
            // offset recomputed fresh every application (idempotent per day by construction), so
            // scaling them would change what a slider position MEANS, not how fast it arrives -
            // Phase 1's sector-sensitivity distinction, and Phase 3's stance-not-flow question
            // answered per constant. Clamps unscaled, per the translation table's own warning.
            EconomyState state = country.State;

            state.NaturalBirthRate = Mathf.Clamp(state.NaturalBirthRate - BirthRateSecularDeclineRate * sliceFraction, MinBirthRate, MaxBirthRate);
            float familyPolicyEffect = FamilyPolicyBirthRateSensitivity * (country.FamilyPolicyLevel - 50f);
            state.BirthRate = Mathf.Clamp(state.NaturalBirthRate + familyPolicyEffect, MinBirthRate, MaxBirthRate);

            float birthDeathGap = state.DeathRate - state.BirthRate;
            state.DependencyRatio = Mathf.Clamp(
                state.DependencyRatio + DependencyRatioDriftSensitivity * sliceFraction * Mathf.Max(0f, birthDeathGap),
                MinDependencyRatio, MaxDependencyRatio);

            float dependencyGap = Mathf.Max(0f, state.DependencyRatio - country.BaselineDependencyRatio);
            state.DeathRate = Mathf.Clamp(state.DeathRate + DeathRateAgingDriftSensitivity * sliceFraction * dependencyGap, 0f, MaxDeathRate);

            state.NaturalNetMigrationRate = Mathf.Clamp(
                state.NaturalNetMigrationRate + MigrationAgingDriftSensitivity * sliceFraction * dependencyGap,
                MinNetMigrationRate, MaxNetMigrationRate);
            float immigrationPolicyEffect = ImmigrationPolicyNetMigrationSensitivity * (country.ImmigrationPolicyLevel - 50f);
            state.NetMigrationRate = Mathf.Clamp(state.NaturalNetMigrationRate + immigrationPolicyEffect, MinNetMigrationRate, MaxNetMigrationRate);
        }

        // --- Continuous Time Phase 4: Demographics daily forms ---
        // Derived, never typed in - the Phase 1 discipline that made the 121->365 turn change a
        // one-line edit applies here from birth.
        private const float DemographicSliceFractionPerDay = 1f / SimulationManager.DaysPerTurn;
        private static readonly float PopulationGrowthReversionSpeedPerDay = PerDayReversion(PopulationGrowthReversionSpeed);

        /// <summary>Phase 4 daily wrapper - the four drift flows at their per-day slice; sensitivities
        /// and clamps untouched (see the turn form's own Phase 4 comment for the shape reasoning).</summary>
        public static void ApplyDemographicRatesDaily(Country country) => ApplyDemographicRates(country, DemographicSliceFractionPerDay);

        /// <summary>Phase 4 daily wrapper - reversion at PerDayReversion speed, the population factor
        /// as the 1/DaysPerTurn power slice (algebraically exact at constant rate).</summary>
        public static void ApplyPopulationGrowthDaily(Country country) => ApplyPopulationGrowth(country, PopulationGrowthReversionSpeedPerDay, DemographicSliceFractionPerDay);

        /// <summary>Small positive floor so a shrinking population can still recover instead of locking at exactly 0 (0 times anything is still 0) - mirrors MacroSystem.MinGdp's own reasoning.</summary>
        private const float MinPopulation = 0.1f;

        /// <summary>Generous gameplay safety ceiling, not a realistic constraint - comfortably above any real or plausible national population, ever.</summary>
        private const float MaxPopulation = 10000f;

        /// <summary>
        /// How much weight the raw, unbounded (BirthRate - DeathRate + NetMigrationRate) signal gets
        /// in pulling PopulationGrowthRate's own reversion TARGET away from Country.
        /// SteadyStateGrowthRate, each turn. Deliberately well below 1.0 (moderate, not full weight) -
        /// this is what keeps a persistent birth/death/migration gap from driving Population to an
        /// extreme indefinitely, the exact failure this constant was added to fix, while still giving
        /// Part B's future Family/Immigration Policy levers (which act through BirthRate/
        /// NetMigrationRate) real, felt, but bounded influence.
        /// </summary>
        private const float PopulationGrowthRateSensitivity = 0.4f;

        /// <summary>
        /// Hard cap, per 1000 population per turn, on how far the raw (BirthRate - DeathRate +
        /// NetMigrationRate) signal is allowed to pull PopulationGrowthRate's reversion TARGET away
        /// from Country.SteadyStateGrowthRate, applied to the gap BEFORE PopulationGrowthRateSensitivity
        /// weights it - the same "bound the aggregate once, don't just trust each input's own clamp"
        /// idiom PotentialGrowthRate's MaxTotalPotentialGrowthAdjustment already uses. Necessary because
        /// DependencyRatio (see ApplyDemographicRates) is explicitly one-directional and never reverts,
        /// so DeathRate/NetMigrationRate keep drifting for a very long transient (hundreds of turns) even
        /// though each is individually bounded - without this cap, that slow transient drift would keep
        /// dragging the TARGET further from the anchor for the entire 500-turn validation horizon, which
        /// defeats the point of reverting toward a bounded steady state.
        ///
        /// NOTE on what this constant does and does NOT fix: an earlier pass at this correction (2, then
        /// briefly 1.5/1 alone) treated the cap as the primary lever for matching real-world plausibility,
        /// but empirically tightening it (and separately, reversion speed) barely moved the modeled
        /// outcome - because the actual defect was structural, not a calibration problem with this
        /// constant: `ApplyPopulationGrowth` was applying a full annual-scale rate every TURN, but this
        /// project's own turn-to-calendar-time convention (`ElectionSystem.ElectionCycle` = 12 turns per
        /// presidential term = 4 real years) means 500 turns is ~167 real years, not 500 - a structural
        /// 3x over-compounding no cap or speed value could fix (see YearsPerTurn below, the actual fix).
        /// With that fixed, 1 (not a looser value) brings the plateaued rate close enough to each
        /// country's own SteadyStateGrowthRate anchor that the 500-turn/167-year outcome lands within
        /// the same order of magnitude as a faithful extrapolation of each country's own anchor rate over
        /// the correct 167-year horizon, for all six countries - see CLAUDE.md for the full per-country
        /// comparison and derivation. Still non-zero (not simply removed) so PopulationGrowthRate
        /// genuinely plateaus within the validation horizon rather than still trending in one direction
        /// at turn 500, unlike every other successfully-stabilized variable in this model (Unemployment,
        /// Inflation, DebtToGdpRatio all visibly flatten out).
        /// </summary>
        private const float MaxPopulationGrowthRateDeviation = 1f;

        /// <summary>
        /// How fast PopulationGrowthRate itself reverts toward its target each turn - the same
        /// mean-reversion idiom Unemployment's reversion toward NaturalUnemploymentRate and Inflation's
        /// reversion toward target already use, but deliberately SLOWER than this project's usual
        /// ~0.15 reversion speeds: real demographic momentum (age structure, cultural/fertility norms)
        /// changes over generations, not years, so a population growth rate should be one of the
        /// slowest-moving reverting quantities in this model, not one of the fastest.
        /// </summary>
        private const float PopulationGrowthReversionSpeed = 0.05f;

        /// <summary>
        /// Real years represented by one turn, derived from `ElectionSystem.ElectionCycle` (turns per
        /// 4-year presidential term) rather than typed - since `d8f55ce` made a turn 365 days and a
        /// term 4 turns, this derives to EXACTLY 1.0, and the Phase 4 throwaway diagnostic asserts
        /// that agreement (with `DaysPerTurn`/365) before trusting anything downstream.
        ///
        /// ⚠ HISTORY, kept because it is the reason this constant exists at all:
        /// BirthRate/DeathRate/NetMigrationRate/SteadyStateGrowthRate are real per-1,000-per-YEAR
        /// figures, and in the 121-day-turn era applying them unscaled each turn compounded a year's
        /// demographic change 3x too often - demographics' first structural bug, found by a throwaway
        /// diagnostic after tightening the cap/reversion constants was tried first and measured
        /// insufficient. The turn-length change did not retire the constant: it is the ONE place the
        /// annual-rate-to-turn conversion is stated, and Phase 4's daily form slices through the same
        /// statement (`YearsPerTurn`/`DaysPerTurn`), so a future turn-length change still lands here
        /// exactly once. *(This comment previously still narrated the 121-day derivation as current -
        /// corrected 2026-08-16, the Phase 4 pass's stale-comment catch.)*
        /// </summary>
        private const float YearsPerTurn = 4f / ElectionSystem.ElectionCycle;

        /// <summary>
        /// Population evolves by PopulationGrowthRate/1000 x YearsPerTurn x Population each turn -
        /// YearsPerTurn converts the annual-scale rate to this turn's actual real-time slice (see above).
        /// PopulationGrowthRate is itself a mean-reverting quantity - the FIX for this pass's original
        /// design, which applied the raw (BirthRate - DeathRate + NetMigrationRate) figure to Population
        /// directly and indefinitely. That raw figure is realistic at the individual-rate level (each of
        /// BirthRate/DeathRate/NetMigrationRate is independently bounded - see ApplyDemographicRates) but,
        /// with no pull back toward any long-run figure, compounds without limit (near-extinction for the
        /// countries already running more deaths than births, near-quadrupling for the fastest-growing).
        /// Each turn the raw figure's gap versus Country.SteadyStateGrowthRate is first hard-capped at
        /// +/-MaxPopulationGrowthRateDeviation (bounding the aggregate pull itself, since DependencyRatio's
        /// own one-directional drift means the raw figure can keep moving for far longer than the 167-year
        /// validation horizon even though each of its own inputs is individually clamped), then weighted
        /// by PopulationGrowthRateSensitivity to set this turn's reversion TARGET; PopulationGrowthRate
        /// then reverts toward that target at PopulationGrowthReversionSpeed - the same two-step "gap sets
        /// a target, state reverts toward it" shape used throughout this model (e.g. Taylor Rule ->
        /// InterestRate). This still allows genuine sustained long-run growth (USA) or decline (Germany/
        /// Poland/Italy) - SteadyStateGrowthRate itself is not zero for any of the six - it just guarantees
        /// the actual growth rate permanently stays within a bounded band around that plausible long-run
        /// figure, rather than drifting further from it for as long as the underlying aging drift
        /// continues. Must run AFTER ApplyDemographicRates, which updates the three rates this reads for
        /// the same turn.
        /// </summary>
        public static void ApplyPopulationGrowth(Country country, float reversionSpeed = PopulationGrowthReversionSpeed, float sliceFraction = 1f)
        {
            // CONTINUOUS TIME PHASE 4: three constants, three different non-conversions and one
            // conversion, stated so the shapes stay reviewable. `PopulationGrowthRateSensitivity`
            // shapes the TARGET (no time dimension - unscaled); `MaxPopulationGrowthRateDeviation`
            // bounds the gap feeding that target (a state-space clamp - the translation table's own
            // named trap, unscaled); the REVERSION converts through PerDayReversion like every
            // reverting quantity since Phase 2; and the population application takes the POWER slice
            // below. The `sliceFraction == 1f` branch keeps the turn form bit-identical to its
            // pre-Phase-4 arithmetic (PreviewTurn and the equivalence check's turn side still run it).
            EconomyState state = country.State;
            float impliedRate = state.BirthRate - state.DeathRate + state.NetMigrationRate;
            float boundedGap = Mathf.Clamp(impliedRate - country.SteadyStateGrowthRate, -MaxPopulationGrowthRateDeviation, MaxPopulationGrowthRateDeviation);
            float target = country.SteadyStateGrowthRate + PopulationGrowthRateSensitivity * boundedGap;
            state.PopulationGrowthRate += reversionSpeed * (target - state.PopulationGrowthRate);

            // The annual factor taken as a per-day POWER, not a divided rate: 365 daily
            // multiplications by (1+x)^(1/365) compose to exactly (1+x) at constant rate - the same
            // algebraic-equivalence-over-approximation choice Phase 1's sector translation made. The
            // rate does move daily under reversion, so a small residual is EXPECTED at the
            // equivalence check, same class as Phase 3's moving-debt-stock drift.
            float annualFactor = 1f + state.PopulationGrowthRate / 1000f * YearsPerTurn;
            float factor = sliceFraction == 1f ? annualFactor : Mathf.Pow(annualFactor, sliceFraction);
            state.Population = Mathf.Clamp(state.Population * factor, MinPopulation, MaxPopulation);
        }

        /// <summary>
        /// CrimeIndex points added per point Country.BailReformLevel sits above its neutral 50 (Round
        /// 2's "Deeper Crime &amp; Justice") - small and HONESTLY CONTESTED, the same "flag the real
        /// debate, don't pretend it's settled" treatment OvertimeRegulationLevel's own Unemployment
        /// effect already got in "Deeper Labor Market Policies": bail reform's real effect on crime is
        /// genuinely disputed in criminology research, not a settled empirical fact.
        /// </summary>
        internal const float BailReformCrimeIndexSensitivity = 0.02f;

        /// <summary>Round 3 item 3: CrimeIndex points added per point OrganizedCrimeIndex sits above Country.BaselineOrganizedCrimeIndex (and reduced per point below) - organized crime activity is a real, direct contributor to overall crime levels in most criminological frameworks. Deliberately modest so overall CrimeIndex isn't dominated by this one secondary contributor.</summary>
        private const float OrganizedCrimeIndexSensitivity = 0.1f;

        /// <summary>
        /// CrimeIndex mean-reverts toward a target of Country.BaselineCrimeIndex, adjusted by the
        /// Unemployment-versus-NAIRU gap (a modest, already-proven driver), by how far
        /// PoliceFundingLevel/SentencingSeverity sit from their shared neutral 50 - higher police
        /// funding or harsher sentencing both reduce the target, funding more strongly than
        /// sentencing (see PoliceFundingSensitivity/SentencingSensitivity) - by BailReformLevel (see
        /// BailReformCrimeIndexSensitivity), and now by the OrganizedCrimeIndex gap versus its own
        /// baseline (Round 3 item 3 - see OrganizedCrimeIndexSensitivity), and by the Interior/Justice
        /// Cabinet minister's passive competence bias, if one is appointed (Political Systems
        /// Overhaul Part A - see CabinetSystem.GetCompetenceBias; a folded-in gap-based term exactly
        /// like every other lever here, landing inside the SAME final Clamp(0, 100) that already
        /// serves as this stat's combined ceiling). Hard-clamped to [0, 100].
        /// </summary>
        public static void ApplyCrimeIndex(Country country, float reversionSpeed = CrimeIndexReversionSpeed)
        {
            EconomyState state = country.State;
            float unemploymentGap = state.Unemployment - country.NaturalUnemploymentRate;
            float organizedCrimeGap = state.OrganizedCrimeIndex - country.BaselineOrganizedCrimeIndex;
            float interiorJusticeCompetenceBias = CabinetSystem.GetCompetenceBias(country, CabinetPortfolio.InteriorJustice);
            float target = country.BaselineCrimeIndex
                + CrimeUnemploymentSensitivity * unemploymentGap
                - PoliceFundingSensitivity * (country.PoliceFundingLevel - NeutralPolicyDialLevel)
                - SentencingSensitivity * (country.SentencingSeverity - NeutralPolicyDialLevel)
                + BailReformCrimeIndexSensitivity * (country.BailReformLevel - NeutralPolicyDialLevel)
                + OrganizedCrimeIndexSensitivity * organizedCrimeGap
                - interiorJusticeCompetenceBias;

            state.CrimeIndex = Mathf.Clamp(state.CrimeIndex + reversionSpeed * (target - state.CrimeIndex), 0f, MaxCrimeIndexPercent);
        }

        /// <summary>BusinessConfidence points lost per point CrimeIndex sits above Country.BaselineCrimeIndex (and gained per point below) - higher-than-baseline crime deters investment, a real and well-documented effect, kept small since Confidence directly multiplies Investment.</summary>
        private const float CrimeBusinessConfidenceSensitivity = 0.0015f;

        /// <summary>Round 3 item 3: BusinessConfidence points lost per point OrganizedCrimeIndex sits above Country.BaselineOrganizedCrimeIndex (and gained per point below) - organized crime (extortion, market corruption) deters legitimate investment, a real and well-documented effect. Same magnitude as CrimeBusinessConfidenceSensitivity.</summary>
        private const float OrganizedCrimeBusinessConfidenceSensitivity = 0.0015f;

        /// <summary>Round 3 item 3: BusinessConfidence points lost per point CorruptionIndex sits above Country.BaselineCorruptionIndex (and gained per point below) - corruption's drag on foreign direct investment and legitimate business activity is real and well-documented (World Bank/IMF governance literature). Same magnitude as CrimeBusinessConfidenceSensitivity.</summary>
        private const float CorruptionBusinessConfidenceSensitivity = 0.0015f;

        /// <summary>
        /// CrimeIndex/OrganizedCrimeIndex/CorruptionIndex's ongoing effect on BusinessConfidence - each
        /// a GAP versus its own Country.BaselineX (not an absolute level), the same "gaps, not levels"
        /// idiom ApplyApprovalRating/ApplyPovertyRate already use, so a country with a structurally
        /// higher baseline (e.g. Italy's OrganizedCrimeIndex) isn't penalized just for sitting at its
        /// own normal equilibrium. Clamped to [MinConfidence, MaxConfidence] alongside
        /// ApplyCategorySpendingEffects/ApplyWelfareProgramEffects' own nudges. The Organized Crime and
        /// Corruption terms were added in Round 3 item 3.
        /// </summary>
        public static void ApplyCrimeEffects(Country country, float scale = 1f)
        {
            EconomyState state = country.State;
            float crimeGap = state.CrimeIndex - country.BaselineCrimeIndex;
            float organizedCrimeGap = state.OrganizedCrimeIndex - country.BaselineOrganizedCrimeIndex;
            float corruptionGap = state.CorruptionIndex - country.BaselineCorruptionIndex;
            float confidenceAdjustment = CrimeBusinessConfidenceSensitivity * crimeGap
                + OrganizedCrimeBusinessConfidenceSensitivity * organizedCrimeGap
                + CorruptionBusinessConfidenceSensitivity * corruptionGap;
            state.BusinessConfidence = Mathf.Clamp(state.BusinessConfidence - confidenceAdjustment * scale, MinConfidence, MaxConfidence);
        }

        // --- Prison Population Rate: a real, per-100k tracked stat, mean-reverting toward its own baseline (Round 2's "Deeper Crime & Justice") ---

        /// <summary>Fraction of the gap versus the target that closes each turn on its own - matches CrimeIndex/PovertyRate's own moderate-slow reversion speed.</summary>
        private const float PrisonPopulationReversionSpeed = 0.15f;

        /// <summary>PrisonPopulationRate points reduced per point Country.BailReformLevel sits above its neutral 50 (and added per point below) - bail reform's primary real-world goal is reducing pretrial detention, a direct and substantial real effect (pretrial detainees are a significant share of incarcerated populations, especially in the US).</summary>
        internal const float BailReformPrisonPopulationSensitivity = 2.0f;

        /// <summary>PrisonPopulationRate points added per point Country.DrugPolicyLevel sits above its neutral 50 (and reduced per point below) - the well-documented real link between strict drug enforcement and mass incarceration (the US "war on drugs" being the clearest real-world example).</summary>
        internal const float DrugPolicyPrisonPopulationSensitivity = 1.6f;

        /// <summary>Round 3 item 3: PrisonPopulationRate points reduced per point Country.JudicialFundingLevel sits above its neutral 50 (and added per point below) - a real, well-documented indirect effect: well-funded courts process cases faster, reducing the pretrial-detention backlog that swells incarceration in underfunded systems. Deliberately smaller than BailReformPrisonPopulationSensitivity's direct mechanical effect, since this is a secondary, capacity-driven channel, not bail policy's own primary lever.</summary>
        internal const float JudicialFundingPrisonPopulationSensitivity = 0.8f;

        /// <summary>Gameplay safety bound, comfortably above any real-world incarceration rate (the USA's real ~531 per 100k is already the highest among developed nations).</summary>
        private const float MaxPrisonPopulationRate = 1000f;

        /// <summary>
        /// PrisonPopulationRate mean-reverts toward a target of Country.BaselinePrisonPopulationRate,
        /// adjusted by BailReformLevel (reform reduces it), DrugPolicyLevel (stricter enforcement
        /// raises it), and JudicialFundingLevel (more funding reduces it via faster case processing -
        /// Round 3 item 3) - all gaps versus their shared neutral 50. Hard-clamped to [0, 1000].
        /// </summary>
        public static void ApplyPrisonPopulationRate(Country country, float reversionSpeed = PrisonPopulationReversionSpeed)
        {
            EconomyState state = country.State;
            float target = country.BaselinePrisonPopulationRate
                - BailReformPrisonPopulationSensitivity * (country.BailReformLevel - NeutralPolicyDialLevel)
                + DrugPolicyPrisonPopulationSensitivity * (country.DrugPolicyLevel - NeutralPolicyDialLevel)
                - JudicialFundingPrisonPopulationSensitivity * (country.JudicialFundingLevel - NeutralPolicyDialLevel);

            state.PrisonPopulationRate = Mathf.Clamp(state.PrisonPopulationRate + reversionSpeed * (target - state.PrisonPopulationRate), 0f, MaxPrisonPopulationRate);
        }

        // --- Economic Sectors: descriptive tracked breakdowns, isolated from the core GDP/unemployment/inflation loop (see CLAUDE.md's "Economic Sectors") ---

        /// <summary>Fraction of the gap versus each sector stat's target that closes each turn on its own - matches PovertyRate/CrimeIndex's own moderate-slow reversion speed.</summary>
        private const float SectorReversionSpeed = 0.15f;

        /// <summary>
        /// Continuous Time Phase 1: <see cref="SectorReversionSpeed"/> as a per-DAY gap-closing fraction.
        /// **Derived, never typed in** — a hardcoded 0.001342 would silently become a different policy the
        /// moment `DaysPerTurn` changes, and the whole migration exists to change turn length.
        /// </summary>
        private static readonly float SectorReversionSpeedPerDay = PerDayReversion(SectorReversionSpeed);

        /// <summary>
        /// CONTINUOUS TIME: converts a per-TURN gap-closing fraction into its per-DAY equivalent.
        ///
        /// Translation shape #2, multiplicative. A reversion speed is the fraction of the gap that closes
        /// in one step, so the gap SURVIVING one turn is `(1 - s_turn)`, and the daily speed must satisfy
        /// `(1 - s_day)^121 = (1 - s_turn)`. **Dividing by 121 is the first-attempt bug the methodology
        /// warns about** — it would close the gap materially faster than the turn model.
        ///
        /// Every phase's reversion constants go through here rather than being typed, so none can drift
        /// from `DaysPerTurn` when the continuous-time migration changes turn length.
        /// </summary>
        // --- Continuous Time Phase 2: daily entry points. Thin wrappers on purpose - the target maths
        // lives in ONE place per system, so the daily and turn paths can never disagree about what a
        // country is reverting toward. Only the SPEED differs, which is the entire translation.
        public static void ApplyPovertyRateDaily(Country country) => ApplyPovertyRate(country, PovertyReversionSpeedPerDay);
        public static void ApplyLaborForceParticipationRateDaily(Country country) => ApplyLaborForceParticipationRate(country, LaborForceParticipationReversionSpeedPerDay);
        public static void ApplyCrimeIndexDaily(Country country) => ApplyCrimeIndex(country, CrimeIndexReversionSpeedPerDay);
        public static void ApplyOrganizedCrimeIndexDaily(Country country) => ApplyOrganizedCrimeIndex(country, OrganizedCrimeReversionSpeedPerDay);
        public static void ApplyCorruptionIndexDaily(Country country) => ApplyCorruptionIndex(country, CorruptionReversionSpeedPerDay);
        public static void ApplyPrisonPopulationRateDaily(Country country) => ApplyPrisonPopulationRate(country, PrisonPopulationReversionSpeedPerDay);
        public static void ApplyCrimeEffectsDaily(Country country) => ApplyCrimeEffects(country, CrimeEffectsDailyScale);
        private static float PerDayReversion(float turnSpeed)
        {
            return 1f - Mathf.Pow(1f - turnSpeed, 1f / SimulationManager.DaysPerTurn);
        }

        // --- Continuous Time Phase 2: Labor Market and Crime & Justice daily speeds ---
        private static readonly float PovertyReversionSpeedPerDay = PerDayReversion(PovertyReversionSpeed);
        private static readonly float LaborForceParticipationReversionSpeedPerDay = PerDayReversion(LaborForceParticipationReversionSpeed);
        private static readonly float CrimeIndexReversionSpeedPerDay = PerDayReversion(CrimeIndexReversionSpeed);
        private static readonly float OrganizedCrimeReversionSpeedPerDay = PerDayReversion(OrganizedCrimeReversionSpeed);
        private static readonly float CorruptionReversionSpeedPerDay = PerDayReversion(CorruptionReversionSpeed);
        private static readonly float PrisonPopulationReversionSpeedPerDay = PerDayReversion(PrisonPopulationReversionSpeed);

        /// <summary>
        /// Phase 2: scale for <see cref="ApplyCrimeEffects"/>'s BusinessConfidence nudge, which is an
        /// ACCUMULATING drift rather than a reversion — it has no target, so shape #2 does not apply.
        /// Linear, shape #1: 121 daily applications of `sensitivity/121` sum to one turn's application.
        ///
        /// Approximate rather than exact, and knowingly so: the crime gaps driving it now move daily too,
        /// so the sum is over a slightly varying gap instead of a fixed one. Second-order, and far inside
        /// the ±3–5% aggregation bar.
        /// </summary>
        private const float CrimeEffectsDailyScale = 1f / SimulationManager.DaysPerTurn;

        /// <summary>Points added per point a sector's SubsidyLevel sits above its neutral 50 (and removed per point below) - applied uniformly to Output/Employment/SectorMetric in this first pass, deliberately not wired to the budget (see CLAUDE.md).</summary>
        internal const float SectorSubsidySensitivity = 0.04f;

        /// <summary>Points removed per point a sector's RegulationLevel sits above its neutral 50 (and added per point below) - a compliance-cost tradeoff, deliberately smaller than nothing else competes with it in this isolated pass.</summary>
        internal const float SectorRegulationSensitivity = 0.04f;

        /// <summary>Round 3 item 2: points added per point a sector's TaxCreditLevel sits above its neutral 50 - same magnitude and uniform-across-stats shape as SectorSubsidySensitivity, since a tax credit and a direct subsidy have a similar practical effect in this stylized model.</summary>
        internal const float SectorTaxCreditSensitivity = 0.04f;

        /// <summary>Round 3 item 2: points added to Output/SectorMetric per point a sector's ResearchGrantsLevel sits above its neutral 50 - same magnitude as SectorSubsidySensitivity, since R&D funding most directly targets output/innovation.</summary>
        internal const float SectorResearchGrantsSensitivity = 0.04f;

        /// <summary>Round 3 item 2: points added to Employment per point a sector's ResearchGrantsLevel sits above its neutral 50 - HALF SectorResearchGrantsSensitivity, deliberately smaller: grants fund research projects and output, not broad hiring, unlike a direct Subsidy.</summary>
        internal const float SectorResearchGrantsEmploymentSensitivity = 0.02f;

        /// <summary>Round 3 item 2: points added to Output/SectorMetric (and REMOVED from Employment - see ApplySectorEffects) per point a sector's DeregulationNationalizationLevel sits above its neutral 50 - the real, well-documented state-owned-enterprise tradeoff (privatization/deregulation gains efficiency by shedding excess labor; nationalization preserves jobs at an efficiency cost).</summary>
        internal const float SectorDeregulationSensitivity = 0.04f;

        /// <summary>
        /// Each of a country's Sectors mean-reverts Output/Employment/SectorMetric toward its own
        /// BaselineX anchor, adjusted by that sector's five policy dials' gaps versus their shared
        /// neutral 50 (the same uniform-dial idiom Country.PoliceFundingLevel/SentencingSeverity
        /// already use). Subsidy/Regulation/TaxCredit/DeregulationNationalization all push Output and
        /// SectorMetric the same direction as each other; ResearchGrants does too, just at a smaller
        /// weight for Employment specifically (see the sensitivity constants above) -
        /// DeregulationNationalization is the one deliberate divergence, flipping sign for Employment
        /// (see Sector.DeregulationNationalizationLevel's own doc comment for why). Deliberately
        /// isolated from GDP/Unemployment/Inflation/ApprovalRating/Confidence in this pass - a
        /// descriptive breakdown only, not a new driver of the core simulation loop (see "Economic
        /// Sectors" in CLAUDE.md for why).
        /// </summary>
        public static void ApplySectorEffects(Country country)
        {
            ApplySectorEffectsInternal(country, SectorReversionSpeed);
        }

        /// <summary>
        /// CONTINUOUS TIME PHASE 1 — the daily form of <see cref="ApplySectorEffects"/>.
        ///
        /// `SectorReversionSpeed` is a GAP-CLOSING FRACTION, so it takes translation shape #2
        /// (multiplicative), not #1. The gap remaining after one turn is `(1 - 0.15)`, so the daily speed
        /// must satisfy `(1 - s_day)^121 = (1 - s_turn)`, giving `s_day = 1 - (1 - s_turn)^(1/121)` —
        /// about 0.001342. **Dividing 0.15 by 121 is the first-attempt bug the methodology warns about**
        /// and would close the gap measurably faster than the turn model does.
        ///
        /// **The sensitivity constants are deliberately NOT scaled.** They define the TARGET
        /// (`Baseline + adjustment`), which is a level, not a per-step increment — the same distinction
        /// rule 4 draws for clamps. Scaling them would shrink the destination rather than the speed of
        /// travel, which is a different model, not a finer-grained one.
        ///
        /// Aggregation-equivalence is therefore EXACT here rather than within tolerance: for any target
        /// held fixed across the turn, 121 daily steps leave precisely the residual gap one turn step
        /// leaves. Policy dials only change on turn boundaries, so the target is always fixed across the
        /// interval.
        /// </summary>
        public static void ApplySectorEffectsDaily(Country country)
        {
            ApplySectorEffectsInternal(country, SectorReversionSpeedPerDay);
        }

        private static void ApplySectorEffectsInternal(Country country, float reversionSpeed)
        {
            foreach (Sector sector in country.Sectors)
            {
                float subsidyAdjustment = SectorSubsidySensitivity * (sector.SubsidyLevel - NeutralPolicyDialLevel);
                float regulationAdjustment = -SectorRegulationSensitivity * (sector.RegulationLevel - NeutralPolicyDialLevel);
                float taxCreditAdjustment = SectorTaxCreditSensitivity * (sector.TaxCreditLevel - NeutralPolicyDialLevel);
                float deregulationAdjustment = SectorDeregulationSensitivity * (sector.DeregulationNationalizationLevel - NeutralPolicyDialLevel);
                float researchGrantsGap = sector.ResearchGrantsLevel - NeutralPolicyDialLevel;

                float outputAndMetricAdjustment = subsidyAdjustment + regulationAdjustment + taxCreditAdjustment
                    + deregulationAdjustment + SectorResearchGrantsSensitivity * researchGrantsGap;
                float employmentAdjustment = subsidyAdjustment + regulationAdjustment + taxCreditAdjustment
                    - deregulationAdjustment + SectorResearchGrantsEmploymentSensitivity * researchGrantsGap;

                float outputTarget = sector.BaselineOutputShareOfGdp + outputAndMetricAdjustment;
                sector.OutputShareOfGdp = Mathf.Max(0f, sector.OutputShareOfGdp + reversionSpeed * (outputTarget - sector.OutputShareOfGdp));

                float employmentTarget = sector.BaselineEmploymentShare + employmentAdjustment;
                sector.EmploymentShare = Mathf.Max(0f, sector.EmploymentShare + reversionSpeed * (employmentTarget - sector.EmploymentShare));

                float metricTarget = sector.BaselineSectorMetric + outputAndMetricAdjustment;
                sector.SectorMetric = Mathf.Max(0f, sector.SectorMetric + reversionSpeed * (metricTarget - sector.SectorMetric));
            }
        }

        // --- Infrastructure Condition: a decay/investment stock model (Round 2's "Infrastructure system") ---

        /// <summary>ConditionIndex points lost per turn to deferred maintenance, absent any incremental Infrastructure spending increase this turn - infrastructure needs growing real investment merely to hold steady (rising usage, materials aging, tech obsolescence), so a flat spending level still implies gradual real degradation. Deliberately small, and hard-clamped below so it can never diverge - see InfrastructureAsset.cs for why this is a stock model, not a gap-to-baseline one.</summary>
        private const float InfrastructureDecayRatePerTurn = 0.08f;

        /// <summary>Continuous Time Phase 1: the same decay as a per-DAY linear rate (shape #1). Derived from <see cref="SimulationManager.DaysPerTurn"/> rather than typed, for the same reason as <see cref="SectorReversionSpeedPerDay"/>.</summary>
        private const float InfrastructureDecayRatePerDay = InfrastructureDecayRatePerTurn / SimulationManager.DaysPerTurn;

        /// <summary>ConditionIndex points gained per percentage-point-of-GDP this turn's Infrastructure spending change represents - reuses the exact same PercentOfGdp(decision.InfrastructureSpendingChange, GDP) signal ApplyCategorySpendingEffects already computes for its PotentialGrowthRate nudge, per the task's explicit "connect to the existing category, don't invent a parallel system" instruction.</summary>
        private const float InfrastructureInvestmentSensitivity = 6f;

        /// <summary>
        /// Every InfrastructureAsset's ConditionIndex moves via two flows this turn - constant decay
        /// minus investment - and is hard-clamped to [0, 100] immediately after, so it can never grow
        /// or decay past its bounds regardless of how extreme spending gets in either direction (the
        /// same principle "SpendingLine Amount Ceiling"/the Sovereign Wealth Fund's 300%-of-GDP
        /// ceiling already established for this session's other stock-like values).
        /// </summary>
        public static void ApplyInfrastructureCondition(Country country, PolicyDecision decision)
        {
            EconomyState state = country.State;
            float infrastructurePercent = PercentOfGdp(decision.InfrastructureSpendingChange, state.GDP);
            foreach (InfrastructureAsset asset in country.InfrastructureAssets)
            {
                asset.ConditionIndex = Mathf.Clamp(
                    asset.ConditionIndex - InfrastructureDecayRatePerTurn + InfrastructureInvestmentSensitivity * infrastructurePercent,
                    0f, 100f);
            }
        }

        /// <summary>
        /// CONTINUOUS TIME PHASE 1 — the daily DECAY only. Investment is deliberately left on the turn
        /// boundary, and that split is the substantive decision in this conversion.
        ///
        /// **Decay is a continuous physical process** — materials age, usage wears, technology obsolesces
        /// — so it is translation shape #1, linear: `rate_per_day = rate_per_turn / 121`. Nothing about it
        /// is tied to when a budget is passed.
        ///
        /// **Investment is a discrete budget action**, not a continuous flow. `InfrastructureSpendingChange`
        /// is a per-turn policy decision that exists only at a turn boundary; `AdvanceDay` has no
        /// `PolicyDecision` and inventing one so the credit could be smeared across 121 days would model
        /// spending the player has not yet committed to. So the credit stays exactly where it was, applied
        /// once per turn at full strength.
        ///
        /// **Aggregation-equivalence is exact by construction:** 121 × (decay/121) = decay, and the
        /// investment term is untouched, so a turn's total movement is identical to before.
        ///
        /// ⚠ **One real behavioural difference, and it is intended:** the [0, 100] clamp now applies daily
        /// rather than once, so an asset already at 0 stops accruing decay mid-turn instead of absorbing a
        /// full turn's worth. That is more correct — a stock cannot decay past empty — and it is the only
        /// place the two forms can diverge. Rule 4 applies: the clamp itself does NOT scale.
        /// </summary>
        public static void ApplyInfrastructureConditionDaily(Country country)
        {
            foreach (InfrastructureAsset asset in country.InfrastructureAssets)
            {
                asset.ConditionIndex = Mathf.Clamp(asset.ConditionIndex - InfrastructureDecayRatePerDay, 0f, 100f);
            }
        }

        /// <summary>
        /// Continuous Time Phase 1: the INVESTMENT half of <see cref="ApplyInfrastructureCondition"/>,
        /// kept at turn granularity. Split out so the turn path applies exactly the credit and none of the
        /// decay, which <see cref="ApplyInfrastructureConditionDaily"/> has already charged day by day.
        /// </summary>
        public static void ApplyInfrastructureInvestment(Country country, PolicyDecision decision)
        {
            float infrastructurePercent = PercentOfGdp(decision.InfrastructureSpendingChange, country.State.GDP);
            foreach (InfrastructureAsset asset in country.InfrastructureAssets)
            {
                asset.ConditionIndex = Mathf.Clamp(
                    asset.ConditionIndex + InfrastructureInvestmentSensitivity * infrastructurePercent, 0f, 100f);
            }
        }

        // --- Infrastructure Feedback: ConditionIndex/spending nudge PotentialGrowthRate, combined under one ceiling (resolves ROADMAP_BRIEF.md's Open Questions #2 - "Resolved by Elias: FEED BACK") ---

        /// <summary>ConditionIndex value at/above which infrastructure condition is considered "healthy" and applies no growth penalty. 50 - the natural midpoint of the 0-100 ConditionIndex scale, and the same "50 = neutral" convention already used throughout this codebase's policy dials (PoliceFundingLevel, SentencingSeverity, OvertimeRegulationLevel, etc.). Chosen so that no country's seeded ConditionIndex (all >= 55 - see WorldFactory) starts below it, avoiding a turn-1 shock, the same "avoid discontinuity" idiom established since "Turn-1 GDP Consistency."</summary>
        private const float InfrastructureConditionGrowthThreshold = 50f;

        /// <summary>PotentialGrowthRate points lost per point the average ConditionIndex sits below InfrastructureConditionGrowthThreshold - a LIVE, recomputed-every-turn penalty, not an accumulator: unlike the spending-driven boost below, this eases automatically (and can disappear entirely) if condition later recovers back above the threshold.</summary>
        private const float InfrastructureConditionDragSensitivity = 0.02f;

        /// <summary>Cap on the condition-drag component alone (always non-positive), before combining with the spending boost.</summary>
        private const float MaxInfrastructureConditionDrag = 0.5f;

        /// <summary>Cap on the spending-boost accumulator alone (Country.InfrastructureSpendingGrowthAdjustment, always non-negative) - see ApplyCategorySpendingEffects, which increments it.</summary>
        private const float MaxInfrastructureSpendingBoost = 1f;

        /// <summary>
        /// Combined-effect ceiling on the TOTAL infrastructure-related adjustment to
        /// PotentialGrowthRate. The spending-driven boost (a lasting accumulator) and the
        /// condition-driven drag (a live, non-accumulating penalty) both push the SAME variable in
        /// principle, so they are combined and clamped TOGETHER here, not just individually - this is
        /// what actually prevents the two Infrastructure-related sources from stacking past a single
        /// sane bound in the same direction, rather than each independently walking up to its own
        /// separate cap and only being checked in isolation. Deliberately tighter than the sum of the
        /// two individual caps (0.5 + 1.0 = 1.5) so this ceiling is a genuinely active constraint, not
        /// a dead safety net that never binds.
        /// </summary>
        private const float MaxCombinedInfrastructureGrowthAdjustment = 0.75f;

        /// <summary>
        /// Returns the combined, ceilinged Infrastructure-related adjustment to PotentialGrowthRate -
        /// the spending-driven boost (Country.InfrastructureSpendingGrowthAdjustment, only ever
        /// non-negative) plus the condition-driven drag (live, only ever non-positive), clamped
        /// TOGETHER to MaxCombinedInfrastructureGrowthAdjustment. Does NOT itself write
        /// PotentialGrowthRate any more (see "Sector Integration" in CLAUDE.md) - ApplySectorGrowthEffect
        /// is now the single method that combines THIS value with Sector's own contribution under one
        /// further, all-sources ceiling and performs the actual assignment. Must run AFTER
        /// ApplyInfrastructureCondition (so the drag reflects this turn's just-updated ConditionIndex)
        /// and AFTER ApplyCategorySpendingEffects (so the accumulator reflects this turn's spending
        /// change).
        /// </summary>
        public static float ApplyInfrastructureGrowthEffect(Country country)
        {
            float averageCondition = 0f;
            if (country.InfrastructureAssets.Count > 0)
            {
                foreach (InfrastructureAsset asset in country.InfrastructureAssets)
                {
                    averageCondition += asset.ConditionIndex;
                }
                averageCondition /= country.InfrastructureAssets.Count;
            }

            float conditionDrag = Mathf.Clamp(
                -InfrastructureConditionDragSensitivity * Mathf.Max(0f, InfrastructureConditionGrowthThreshold - averageCondition),
                -MaxInfrastructureConditionDrag, 0f);

            return Mathf.Clamp(
                country.InfrastructureSpendingGrowthAdjustment + conditionDrag,
                -MaxCombinedInfrastructureGrowthAdjustment, MaxCombinedInfrastructureGrowthAdjustment);
        }

        // --- Sector Integration: Output/Employment performance nudge PotentialGrowthRate/Unemployment, combined with Infrastructure under one all-sources ceiling (resolves ROADMAP_BRIEF.md's Open Questions #1 - "Resolved by Elias: INTEGRATE") ---

        /// <summary>PotentialGrowthRate points gained per percentage-point-of-GDP the aggregate Sector Output (summed gap vs. each sector's own BaselineOutputShareOfGdp, across all four sectors) sits above its own trend - strong sector performance nudges trend growth up, weak performance drags it down.</summary>
        private const float SectorGrowthSensitivity = 0.05f;

        /// <summary>Cap on the sector-performance growth adjustment alone, before combining with Infrastructure's own contribution.</summary>
        private const float MaxSectorGrowthAdjustment = 0.5f;

        /// <summary>
        /// Combined-effect ceiling across ALL THREE PotentialGrowthRate sources (Infrastructure
        /// spending, Infrastructure condition, and Sector performance) - not just Infrastructure's own
        /// sub-ceiling and Sector's own sub-ceiling checked independently. This is the single most
        /// important safeguard in this mechanism: three simultaneous nudges onto one variable, each
        /// already individually bounded, could still in principle stack toward 0.75 + 0.5 = 1.25 if
        /// only checked separately - clamping their SUM here to 1.0 (tighter than that combined
        /// theoretical max, so this ceiling is a genuinely active constraint under worst-case stacking,
        /// not a dead safety net) is what actually prevents that.
        /// </summary>
        private const float MaxTotalPotentialGrowthAdjustment = 1f;

        /// <summary>PotentialGrowthRate points gained per percentage-point-of-GDP the aggregate Sector Output sits above its own trend.</summary>
        internal static float GetSectorGrowthAdjustment(Country country)
        {
            float aggregateOutputGap = 0f;
            foreach (Sector sector in country.Sectors)
            {
                aggregateOutputGap += sector.OutputShareOfGdp - sector.BaselineOutputShareOfGdp;
            }

            return Mathf.Clamp(SectorGrowthSensitivity * aggregateOutputGap, -MaxSectorGrowthAdjustment, MaxSectorGrowthAdjustment);
        }

        /// <summary>
        /// The single method that finalizes PotentialGrowthRate each turn - combines Infrastructure's
        /// own already-ceilinged contribution (ApplyInfrastructureGrowthEffect) with Sector's own
        /// already-ceilinged contribution (GetSectorGrowthAdjustment) under the further, all-sources
        /// MaxTotalPotentialGrowthAdjustment ceiling, then sets PotentialGrowthRate = Clamp
        /// (BasePotentialGrowthRate + total, 0, MaxPotentialGrowthRate). Must run AFTER ApplySectorEffects
        /// (so Sector's contribution reflects this turn's just-updated Output) and after
        /// ApplyInfrastructureCondition/ApplyCategorySpendingEffects (see ApplyInfrastructureGrowthEffect's
        /// own ordering requirement).
        /// </summary>
        public static void ApplySectorGrowthEffect(Country country)
        {
            float infrastructureAdjustment = ApplyInfrastructureGrowthEffect(country);
            float sectorAdjustment = GetSectorGrowthAdjustment(country);
            float totalAdjustment = Mathf.Clamp(infrastructureAdjustment + sectorAdjustment, -MaxTotalPotentialGrowthAdjustment, MaxTotalPotentialGrowthAdjustment);
            country.PotentialGrowthRate = Mathf.Clamp(country.BasePotentialGrowthRate + totalAdjustment, 0f, MaxPotentialGrowthRate);
        }

        /// <summary>Unemployment points removed per point the aggregate Sector Employment (summed gap vs. each sector's own BaselineEmploymentShare) sits above its own trend - sector employment growth nudges economy-wide Unemployment down, contraction nudges it up. Mirrors GetMinimumWageUnemploymentAdjustment/GetOvertimeUnemploymentAdjustment's own "small, additive term inside ApplyOkunsLaw" pattern exactly.</summary>
        private const float SectorUnemploymentSensitivity = 0.03f;

        /// <summary>Cap on the sector-employment unemployment adjustment.</summary>
        private const float MaxSectorUnemploymentAdjustment = 0.3f;

        internal static float GetSectorUnemploymentAdjustment(Country country)
        {
            float aggregateEmploymentGap = 0f;
            foreach (Sector sector in country.Sectors)
            {
                aggregateEmploymentGap += sector.EmploymentShare - sector.BaselineEmploymentShare;
            }

            return Mathf.Clamp(-SectorUnemploymentSensitivity * aggregateEmploymentGap, -MaxSectorUnemploymentAdjustment, MaxSectorUnemploymentAdjustment);
        }

        // --- Approval Rating: political-economy feedback, Phillips-curve-adjacent (misery index) ---

        /// <summary>Approval mean-reverts toward this absent any other effect - a "neutral" governing position, not an extreme.</summary>
        private const float NeutralApprovalRating = 50f;

        /// <summary>Fraction of the gap versus NeutralApprovalRating that closes each turn on its own.</summary>
        private const float ApprovalReversionSpeed = 0.05f;

        /// <summary>Approval points per percentage-point of growth gap (actual vs. potential) - strong growth is rewarded, weak growth punished.</summary>
        private const float GrowthApprovalSensitivity = 0.3f;

        /// <summary>Approval points lost per percentage point unemployment sits above NAIRU.</summary>
        private const float UnemploymentApprovalSensitivity = 0.4f;

        /// <summary>Approval points lost per percentage point inflation sits away from the Taylor Rule's inflation target (either direction - deflation hurts too).</summary>
        private const float InflationApprovalSensitivity = 0.4f;

        /// <summary>Approval points lost per point CrimeIndex sits above Country.BaselineCrimeIndex (and gained per point below) - smaller than Unemployment/InflationApprovalSensitivity since CrimeIndex gaps tend to run larger in absolute point terms on its 0-100 scale.</summary>
        private const float CrimeApprovalSensitivity = 0.2f;

        /// <summary>Round 3 item 3: Approval points lost per point CorruptionIndex sits above Country.BaselineCorruptionIndex (and gained per point below) - corruption scandals hurt approval, a real and well-documented political effect. Slightly smaller than CrimeApprovalSensitivity, since corruption's political salience varies more by country/culture than crime's does - a stylized judgment call, not a precisely-fitted figure.</summary>
        private const float CorruptionApprovalSensitivity = 0.15f;

        /// <summary>Approval points gained per week Country.PaidFamilyLeaveWeeks sits above its own seeded BaselinePaidFamilyLeaveWeeks (and lost per week below) - a small, real political effect (paid-leave policy tends to be popular).</summary>
        internal const float PaidFamilyLeaveApprovalSensitivity = 0.05f;

        /// <summary>Approval points gained per point Country.DrugPolicyLevel sits above its neutral 50 (and lost per point below) - a small "tough on crime" political effect, gap versus the shared neutral 50 rather than a country-specific anchor (DrugPolicyLevel has no real per-country seed the way PaidFamilyLeaveWeeks does).</summary>
        internal const float DrugPolicyApprovalSensitivity = 0.02f;

        /// <summary>Approval points lost per percentage point a tax rate hike this turn.</summary>
        internal const float TaxHikeApprovalSensitivity = 1.5f;

        /// <summary>Approval points per percentage-point-of-GDP of (multiplier-weighted) net discretionary spending change.</summary>
        internal const float SpendingApprovalSensitivity = 0.8f;

        /// <summary>Healthcare/education are relatively popular spending; defense is relatively less so; infrastructure is the baseline (no special bonus or penalty).</summary>
        internal const float HealthcareApprovalMultiplier = 1.5f;
        internal const float EducationApprovalMultiplier = 1.5f;
        internal const float DefenseApprovalMultiplier = 0.5f;
        internal const float InfrastructureApprovalMultiplier = 1.0f;

        /// <summary>
        /// Phase 2 (see "Detailed Spending Portfolio Phase 2" in CLAUDE.md) - four more categories
        /// join the weighted-spending approval term. Justice/energy sit at the baseline (like
        /// Infrastructure); homeland security sits between Defense's low popularity and the
        /// baseline (broad, if not universal, appeal for border/disaster-response spending); housing
        /// is relatively popular (like Healthcare/Education, though slightly less so) - illustrative,
        /// gameplay-tuning judgment calls, the same as the original four's own multipliers.
        /// </summary>
        internal const float JusticeApprovalMultiplier = 1.0f;
        internal const float HomelandSecurityApprovalMultiplier = 0.7f;
        internal const float EnergyApprovalMultiplier = 1.0f;
        internal const float HousingApprovalMultiplier = 1.3f;

        /// <summary>
        /// Distinctly higher than any Discretionary category's multiplier above - entitlement
        /// programs (Social Security, Medicare, Medicaid, etc.) are politically far more sensitive
        /// than an equivalent-percentage change to a Discretionary line, so the same relative-size
        /// change to Mandatory spending moves approval by roughly double the strongest Discretionary
        /// multiplier, in either direction (a cut hurts more, but an increase also helps more).
        /// </summary>
        internal const float MandatorySpendingApprovalMultiplier = 3.0f;

        /// <summary>Debt-to-GDP above this (the same "safe" benchmark SimulationManager's risk premium uses) starts discounting the approval benefit of new spending - fiscal-strain awareness.</summary>
        private const float DeficitAwarenessDebtToGdpThreshold = 60f;

        /// <summary>Fraction of the spending-approval benefit removed per point of debt-to-GDP above the threshold.</summary>
        private const float DeficitAwarenessDampeningPerPoint = 0.01f;

        /// <summary>Approval points per 100% GenerosityLevel for each WelfareProgramType, an ongoing STOCK effect (based on the program's CURRENT GenerosityLevel every turn, same idiom as TaxLine.Rate affecting revenue every turn) rather than a one-time "this-turn change" shock like TaxHikeApprovalSensitivity/spending's own weighted term. UBI/UniversalHealthcare are the strongest (universal, highly visible programs); MeansTestedWelfare/HousingAssistance/ChildcareSubsidies are more modest, per the task's own framing (targeted spending is politically less visible than universal programs of similar poverty-reduction power).</summary>
        private const float UbiApprovalSensitivity = 3.0f;
        private const float NegativeIncomeTaxApprovalSensitivity = 2.0f;
        private const float MeansTestedWelfareApprovalSensitivity = 1.5f;
        private const float UniversalHealthcareApprovalSensitivity = 3.0f;
        private const float HousingAssistanceApprovalSensitivity = 1.5f;
        private const float ChildcareSubsidiesApprovalSensitivity = 1.5f;

        internal static float GetWelfareApprovalSensitivity(WelfareProgramType type)
        {
            switch (type)
            {
                case WelfareProgramType.UBI: return UbiApprovalSensitivity;
                case WelfareProgramType.NegativeIncomeTax: return NegativeIncomeTaxApprovalSensitivity;
                case WelfareProgramType.MeansTestedWelfare: return MeansTestedWelfareApprovalSensitivity;
                case WelfareProgramType.UniversalHealthcare: return UniversalHealthcareApprovalSensitivity;
                case WelfareProgramType.HousingAssistance: return HousingAssistanceApprovalSensitivity;
                case WelfareProgramType.ChildcareSubsidies: return ChildcareSubsidiesApprovalSensitivity;
                default: return 0f;
            }
        }

        /// <summary>Sum over every implemented WelfareProgram of GetWelfareApprovalSensitivity(Type) * (GenerosityLevel / 100) - a direct approval delta, not weighted by PercentOfGdp like the spending-category term (welfare's political popularity tracks how generous/visible the program is to the public, not its share of GDP).</summary>
        internal static float GetWelfareApprovalEffect(Country country)
        {
            float effect = 0f;
            foreach (WelfareProgram program in country.WelfarePrograms)
            {
                if (!program.IsImplemented)
                {
                    continue;
                }

                effect += GetWelfareApprovalSensitivity(program.Type) * (program.GenerosityLevel / 100f);
            }

            return effect;
        }

        private static float PercentOfGdp(float amount, float gdp)
        {
            return gdp > 0f ? amount / gdp * 100f : 0f;
        }

        /// <summary>
        /// Approval mean-reverts toward NeutralApprovalRating, adjusted by: the growth gap (strong
        /// growth helps, weak growth hurts), a Phillips-curve-adjacent "misery index" of how far
        /// unemployment/inflation sit from NAIRU/target (not their absolute level - a healthy economy
        /// at its own structural equilibrium shouldn't be punished just for having nonzero
        /// unemployment/inflation), a tax-hike penalty proportional to the hike, and a
        /// category-weighted spending effect (Mandatory categories weighted distinctly higher than
        /// Discretionary ones - see MandatorySpendingApprovalMultiplier) whose benefit (not its
        /// cut-side penalty) is discounted the more debt-to-GDP already sits above a "safe" benchmark,
        /// and a welfare-program effect (see GetWelfareApprovalEffect - an ongoing effect of any
        /// implemented Country.WelfarePrograms' CURRENT GenerosityLevel, not a one-time change like
        /// the tax-hike/spending-change terms above). Clamped to [0, 100].
        /// </summary>
        /// <param name="totalTaxHike">
        /// Sum of every positive per-tax-type rate increase actually applied this turn (clamped target
        /// minus prior rate, where positive) - computed by SimulationManager.ApplyTaxRateChanges
        /// before it overwrites TaxLine.Rate (PolicyDecision.TaxRateOverrides holds an absolute target,
        /// not a delta, so the hike can only be known by comparing against the pre-change rate) and
        /// threaded through here. Raising several taxes at once still compounds the penalty, same
        /// spirit as the old delta-based hike penalty.
        /// </param>
        /// <param name="totalMandatorySpendingChange">
        /// Sum of this turn's ACTUAL dollar change (after clamping and floor-at-0) across every
        /// Mandatory SpendingLine - computed by SimulationManager.ApplySpendingLineChanges/
        /// ResolveSpendingForTurn. 0 for a country without a detailed SpendingLines portfolio. Weighted
        /// by MandatorySpendingApprovalMultiplier, distinctly higher than any Discretionary category's
        /// multiplier - see that constant's doc comment.
        /// </param>
        public static void ApplyApprovalRating(Country country, PolicyDecision decision, float actualGrowthRatePercent, float totalTaxHike, float totalMandatorySpendingChange)
        {
            EconomyState state = country.State;

            float growthGap = actualGrowthRatePercent - country.PotentialGrowthRate;
            float growthEffect = GrowthApprovalSensitivity * growthGap;

            float unemploymentPenaltyGap = Mathf.Max(0f, state.Unemployment - country.NaturalUnemploymentRate);
            float inflationPenaltyGap = Mathf.Abs(state.Inflation - TaylorRule.InflationTarget);
            float crimePenaltyGap = state.CrimeIndex - country.BaselineCrimeIndex;
            float corruptionPenaltyGap = state.CorruptionIndex - country.BaselineCorruptionIndex;
            float miseryPenalty = UnemploymentApprovalSensitivity * unemploymentPenaltyGap
                + InflationApprovalSensitivity * inflationPenaltyGap
                + CrimeApprovalSensitivity * crimePenaltyGap
                + CorruptionApprovalSensitivity * corruptionPenaltyGap;

            float taxHikePenalty = TaxHikeApprovalSensitivity * totalTaxHike;

            float weightedSpendingPercent =
                HealthcareApprovalMultiplier * PercentOfGdp(decision.HealthcareSpendingChange, state.GDP) +
                DefenseApprovalMultiplier * PercentOfGdp(decision.DefenseSpendingChange, state.GDP) +
                InfrastructureApprovalMultiplier * PercentOfGdp(decision.InfrastructureSpendingChange, state.GDP) +
                EducationApprovalMultiplier * PercentOfGdp(decision.EducationSpendingChange, state.GDP) +
                JusticeApprovalMultiplier * PercentOfGdp(decision.JusticeSpendingChange, state.GDP) +
                HomelandSecurityApprovalMultiplier * PercentOfGdp(decision.HomelandSecuritySpendingChange, state.GDP) +
                EnergyApprovalMultiplier * PercentOfGdp(decision.EnergySpendingChange, state.GDP) +
                HousingApprovalMultiplier * PercentOfGdp(decision.HousingSpendingChange, state.GDP) +
                MandatorySpendingApprovalMultiplier * PercentOfGdp(totalMandatorySpendingChange, state.GDP);

            float spendingEffect;
            if (weightedSpendingPercent >= 0f)
            {
                float excessDebtToGdp = Mathf.Max(0f, state.DebtToGdpRatio - DeficitAwarenessDebtToGdpThreshold);
                float deficitAwarenessFactor = Mathf.Clamp(1f - DeficitAwarenessDampeningPerPoint * excessDebtToGdp, 0f, 1f);
                spendingEffect = SpendingApprovalSensitivity * weightedSpendingPercent * deficitAwarenessFactor;
            }
            else
            {
                spendingEffect = SpendingApprovalSensitivity * weightedSpendingPercent;
            }

            float welfareApprovalEffect = GetWelfareApprovalEffect(country);

            // Paid family leave (see "Deeper Labor Market Policies" in CLAUDE.md) - an ONGOING stock
            // effect of the gap versus the country's own seeded baseline (the same idiom
            // welfareApprovalEffect/GetMinimumWageUnemploymentAdjustment already use), not a one-time
            // shock - paid-leave policy tends to be popular, a small, real political effect.
            float paidLeaveApprovalEffect = PaidFamilyLeaveApprovalSensitivity * (country.PaidFamilyLeaveWeeks - country.BaselinePaidFamilyLeaveWeeks);

            // Drug policy (see "Deeper Crime & Justice" in CLAUDE.md) - stricter enforcement gives a
            // small "tough on crime" approval boost, the same modest political framing
            // PoliceFundingLevel's own crime-reduction effect implicitly carries, gap versus the
            // shared neutral 50.
            float drugPolicyApprovalEffect = DrugPolicyApprovalSensitivity * (country.DrugPolicyLevel - NeutralPolicyDialLevel);

            float reversion = ApprovalReversionSpeed * (NeutralApprovalRating - state.ApprovalRating);
            float delta = reversion + growthEffect - miseryPenalty - taxHikePenalty + spendingEffect + welfareApprovalEffect + paidLeaveApprovalEffect + drugPolicyApprovalEffect;
            state.ApprovalRating = Mathf.Clamp(state.ApprovalRating + delta, 0f, 100f);
        }

        // --- Category spending side-effects: small, separable per-category profiles (v1, not a full policy tree) ---

        /// <summary>PotentialGrowthRate points gained per percentage-point-of-GDP spent on infrastructure - a lasting, ratcheting investment effect, accumulated in Country.InfrastructureSpendingGrowthAdjustment rather than mutating PotentialGrowthRate directly (see ApplyInfrastructureGrowthEffect - Infrastructure now has two growth-related sources, spending and condition, combined under one dedicated ceiling there).</summary>
        internal const float InfrastructureGrowthSensitivity = 0.01f;

        /// <summary>ConsumerConfidence gained per percentage-point-of-GDP spent on healthcare - "long-run productivity/wellbeing" modeled as consumer confidence.</summary>
        internal const float HealthcareConfidenceSensitivity = 0.002f;

        /// <summary>BusinessConfidence gained per percentage-point-of-GDP spent on education - a better-skilled workforce modeled as business confidence.</summary>
        internal const float EducationConfidenceSensitivity = 0.002f;

        /// <summary>
        /// Phase 2 (see "Detailed Spending Portfolio Phase 2" in CLAUDE.md) - three more categories
        /// get their own persistent, lasting effect, mirroring Infrastructure/Healthcare/Education's
        /// own "one-turn spending change permanently nudges a structural value" pattern exactly
        /// (HomelandSecurity deliberately gets none, mirroring Defense's own "approval only" pattern).
        /// CrimeIndex points reduced (permanently, off Country.BaselineCrimeIndex) per
        /// percentage-point-of-GDP spent on justice - court/prosecution capacity genuinely affects
        /// case backlogs and enforcement outcomes.
        /// </summary>
        internal const float JusticeCrimeIndexSensitivity = 0.02f;

        /// <summary>BusinessConfidence gained per percentage-point-of-GDP spent on energy - lower/stabler energy costs for businesses, distinct from Education's own BusinessConfidence nudge.</summary>
        internal const float EnergyConfidenceSensitivity = 0.0015f;

        /// <summary>PovertyRate baseline points reduced (permanently, off Country.BaselinePovertyRate) per percentage-point-of-GDP spent on housing - HUD-style baseline federal housing support, smaller than the dedicated player-adjustable WelfareProgramType.HousingAssistance's own sensitivity since this is a much narrower, less-targeted budget line.</summary>
        internal const float HousingPovertyReductionSensitivity = 0.015f;

        /// <summary>Ceiling on PotentialGrowthRate - repeated infrastructure spending over many turns shouldn't be able to push trend growth past a sane bound.</summary>
        private const float MaxPotentialGrowthRate = 8f;

        /// <summary>Confidence bounds around the neutral 1.0 - repeated healthcare/education spending shouldn't be able to push Consumer/BusinessConfidence (which multiply Consumption/Investment) arbitrarily far, since that would eventually destabilize GDP.</summary>
        private const float MinConfidence = 0.7f;
        private const float MaxConfidence = 1.3f;

        /// <summary>
        /// Defense and HomelandSecurity spending have no growth/confidence/CrimeIndex/PovertyRate
        /// side-effect (only the approval effect in ApplyApprovalRating) - both are pure consumption
        /// in the G identity. Infrastructure nudges PotentialGrowthRate; healthcare nudges
        /// ConsumerConfidence; education and energy nudge BusinessConfidence; justice nudges
        /// Country.BaselineCrimeIndex down; housing nudges Country.BaselinePovertyRate down - each
        /// small, independently clamped, and PERSISTENT (a one-turn spending change permanently
        /// shifts the structural value, the same "lasting trend" idiom Infrastructure's own
        /// PotentialGrowthRate nudge already established - not a one-turn shock that fades).
        /// </summary>
        public static void ApplyCategorySpendingEffects(Country country, PolicyDecision decision)
        {
            EconomyState state = country.State;

            float infrastructurePercent = PercentOfGdp(decision.InfrastructureSpendingChange, state.GDP);
            country.InfrastructureSpendingGrowthAdjustment = Mathf.Clamp(country.InfrastructureSpendingGrowthAdjustment + InfrastructureGrowthSensitivity * infrastructurePercent, 0f, MaxInfrastructureSpendingBoost);

            float healthcarePercent = PercentOfGdp(decision.HealthcareSpendingChange, state.GDP);
            state.ConsumerConfidence = Mathf.Clamp(state.ConsumerConfidence + HealthcareConfidenceSensitivity * healthcarePercent, MinConfidence, MaxConfidence);

            float educationPercent = PercentOfGdp(decision.EducationSpendingChange, state.GDP);
            state.BusinessConfidence = Mathf.Clamp(state.BusinessConfidence + EducationConfidenceSensitivity * educationPercent, MinConfidence, MaxConfidence);

            float justicePercent = PercentOfGdp(decision.JusticeSpendingChange, state.GDP);
            country.BaselineCrimeIndex = Mathf.Clamp(country.BaselineCrimeIndex - JusticeCrimeIndexSensitivity * justicePercent, 0f, MaxCrimeIndexPercent);

            float energyPercent = PercentOfGdp(decision.EnergySpendingChange, state.GDP);
            state.BusinessConfidence = Mathf.Clamp(state.BusinessConfidence + EnergyConfidenceSensitivity * energyPercent, MinConfidence, MaxConfidence);

            float housingPercent = PercentOfGdp(decision.HousingSpendingChange, state.GDP);
            country.BaselinePovertyRate = Mathf.Clamp(country.BaselinePovertyRate - HousingPovertyReductionSensitivity * housingPercent, 0f, MaxPovertyRatePercent);
        }

        // --- Welfare program side-effects: small, separable per-program profiles, mirroring the category spending effects above ---

        /// <summary>ConsumerConfidence gained per 100% GenerosityLevel of UBI - "modest Consumption/GDP boost" per the task's own framing, modeled as consumer confidence the same way Healthcare spending already is.</summary>
        internal const float UbiConsumerConfidenceSensitivity = 0.03f;

        /// <summary>BusinessConfidence gained per 100% GenerosityLevel of UniversalHealthcare - reduced employer healthcare-cost burden, modeled as business confidence the same way Education spending already is.</summary>
        internal const float UniversalHealthcareBusinessConfidenceSensitivity = 0.03f;

        /// <summary>
        /// UBI nudges ConsumerConfidence up; UniversalHealthcare nudges BusinessConfidence up - both
        /// small, ongoing STOCK effects (based on the program's CURRENT GenerosityLevel every turn,
        /// same idiom as ApplyPovertyRate/GetWelfareApprovalEffect) and independently clamped to
        /// [MinConfidence, MaxConfidence] alongside ApplyCategorySpendingEffects' own Healthcare/
        /// Education confidence nudges - repeated turns of a high-generosity program pushing
        /// confidence toward the ceiling is the same accepted behavior those two already have.
        /// Every other WelfareProgramType deliberately has no confidence side-effect here (only the
        /// poverty-reduction/approval effects above) - narrow, targeted programs (MeansTestedWelfare/
        /// HousingAssistance/ChildcareSubsidies) and NegativeIncomeTax are modeled as NOT moving broad
        /// consumer/business sentiment, per the task's own "minimal broad GDP effect" framing.
        /// </summary>
        public static void ApplyWelfareProgramEffects(Country country)
        {
            EconomyState state = country.State;

            foreach (WelfareProgram program in country.WelfarePrograms)
            {
                if (!program.IsImplemented)
                {
                    continue;
                }

                float generosityFraction = program.GenerosityLevel / 100f;
                if (program.Type == WelfareProgramType.UBI)
                {
                    state.ConsumerConfidence = Mathf.Clamp(state.ConsumerConfidence + UbiConsumerConfidenceSensitivity * generosityFraction, MinConfidence, MaxConfidence);
                }
                else if (program.Type == WelfareProgramType.UniversalHealthcare)
                {
                    state.BusinessConfidence = Mathf.Clamp(state.BusinessConfidence + UniversalHealthcareBusinessConfidenceSensitivity * generosityFraction, MinConfidence, MaxConfidence);
                }
            }
        }
    }
}
