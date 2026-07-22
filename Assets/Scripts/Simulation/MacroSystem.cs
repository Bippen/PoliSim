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
        private const float MinimumWageEmploymentSensitivity = 1.5f;

        /// <summary>
        /// This turn's Unemployment nudge from how far Country.MinimumWagePercentOfMedian has moved
        /// from its own seeded baseline, an ONGOING stock effect of the current level (like
        /// WelfareProgram's approval term, not a one-time shock) - zero for a country with no
        /// statutory minimum wage (Sweden, Italy - see Country.MinimumWageImplemented) and zero at
        /// the seeded starting level for every other country.
        /// </summary>
        private static float GetMinimumWageUnemploymentAdjustment(Country country)
        {
            if (!country.MinimumWageImplemented)
            {
                return 0f;
            }

            float gap = country.MinimumWagePercentOfMedian - country.BaselineMinimumWagePercentOfMedian;
            return MinimumWageEmploymentSensitivity * gap / 100f;
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
        public static void ApplyOkunsLaw(Country country, float actualGrowthRatePercent)
        {
            EconomyState state = country.State;
            float growthGap = actualGrowthRatePercent - country.PotentialGrowthRate;
            float unemploymentChange = -OkunCoefficient * growthGap;
            unemploymentChange += GetWelfareAdjustedReversionSpeed(country) * (country.NaturalUnemploymentRate - state.Unemployment);
            unemploymentChange += GetMinimumWageUnemploymentAdjustment(country);

            state.Unemployment = Mathf.Clamp(state.Unemployment + unemploymentChange, 0f, MaxUnemploymentPercent);
        }

        /// <summary>
        /// UnemploymentReversionSpeed, nudged by any implemented UBI (real debated labor-supply
        /// effect - a full-generosity UBI slows reversion slightly) or ChildcareSubsidies (documented
        /// labor-force-participation effect for parents - speeds reversion slightly). Both are small
        /// and the result is floored so neither can meaningfully destabilize Okun's Law's own
        /// mean-reversion, only tilt it.
        /// </summary>
        private static float GetWelfareAdjustedReversionSpeed(Country country)
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

        /// <summary>Adaptive expectations: next turn's expected inflation moves partway toward this turn's realized inflation.</summary>
        public static void ApplyInflationExpectations(EconomyState state)
        {
            state.InflationExpectations += (state.Inflation - state.InflationExpectations) * ExpectationsAdaptationSpeed;
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
        private const float MinimumWagePovertyReductionSensitivity = 5f;

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
        private static float GetPovertyReductionSensitivity(WelfareProgramType type)
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
        /// GetPovertyReductionSensitivity). Hard-clamped to [0, 100].
        /// </summary>
        public static void ApplyPovertyRate(Country country)
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

            float target = baseline - welfareReduction - minimumWageReduction;
            state.PovertyRate = Mathf.Clamp(state.PovertyRate + PovertyReversionSpeed * (target - state.PovertyRate), 0f, MaxPovertyRatePercent);
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
        /// by the same Unemployment-versus-NAIRU gap already used elsewhere (a proven driver - see
        /// ApplyPovertyRate/ApplyApprovalRating) rather than a new one. A tracked stat only - nothing
        /// currently targets it directly with a policy lever (see CLAUDE.md's "Labor Market Basics").
        /// Hard-clamped to [0, 100].
        /// </summary>
        public static void ApplyLaborForceParticipationRate(Country country)
        {
            EconomyState state = country.State;
            float unemploymentGap = state.Unemployment - country.NaturalUnemploymentRate;
            float target = country.BaselineLaborForceParticipationRate - DiscouragedWorkerSensitivity * unemploymentGap;
            state.LaborForceParticipationRate = Mathf.Clamp(
                state.LaborForceParticipationRate + LaborForceParticipationReversionSpeed * (target - state.LaborForceParticipationRate),
                0f, MaxLaborForceParticipationPercent);
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

        /// <summary>Approval points lost per percentage point a tax rate hike this turn.</summary>
        private const float TaxHikeApprovalSensitivity = 1.5f;

        /// <summary>Approval points per percentage-point-of-GDP of (multiplier-weighted) net discretionary spending change.</summary>
        private const float SpendingApprovalSensitivity = 0.8f;

        /// <summary>Healthcare/education are relatively popular spending; defense is relatively less so; infrastructure is the baseline (no special bonus or penalty).</summary>
        private const float HealthcareApprovalMultiplier = 1.5f;
        private const float EducationApprovalMultiplier = 1.5f;
        private const float DefenseApprovalMultiplier = 0.5f;
        private const float InfrastructureApprovalMultiplier = 1.0f;

        /// <summary>
        /// Distinctly higher than any Discretionary category's multiplier above - entitlement
        /// programs (Social Security, Medicare, Medicaid, etc.) are politically far more sensitive
        /// than an equivalent-percentage change to a Discretionary line, so the same relative-size
        /// change to Mandatory spending moves approval by roughly double the strongest Discretionary
        /// multiplier, in either direction (a cut hurts more, but an increase also helps more).
        /// </summary>
        private const float MandatorySpendingApprovalMultiplier = 3.0f;

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

        private static float GetWelfareApprovalSensitivity(WelfareProgramType type)
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
        private static float GetWelfareApprovalEffect(Country country)
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
            float miseryPenalty = UnemploymentApprovalSensitivity * unemploymentPenaltyGap + InflationApprovalSensitivity * inflationPenaltyGap;

            float taxHikePenalty = TaxHikeApprovalSensitivity * totalTaxHike;

            float weightedSpendingPercent =
                HealthcareApprovalMultiplier * PercentOfGdp(decision.HealthcareSpendingChange, state.GDP) +
                DefenseApprovalMultiplier * PercentOfGdp(decision.DefenseSpendingChange, state.GDP) +
                InfrastructureApprovalMultiplier * PercentOfGdp(decision.InfrastructureSpendingChange, state.GDP) +
                EducationApprovalMultiplier * PercentOfGdp(decision.EducationSpendingChange, state.GDP) +
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

            float reversion = ApprovalReversionSpeed * (NeutralApprovalRating - state.ApprovalRating);
            float delta = reversion + growthEffect - miseryPenalty - taxHikePenalty + spendingEffect + welfareApprovalEffect;
            state.ApprovalRating = Mathf.Clamp(state.ApprovalRating + delta, 0f, 100f);
        }

        // --- Category spending side-effects: small, separable per-category profiles (v1, not a full policy tree) ---

        /// <summary>PotentialGrowthRate points gained per percentage-point-of-GDP spent on infrastructure - a lasting (if small) trend-growth boost.</summary>
        private const float InfrastructureGrowthSensitivity = 0.01f;

        /// <summary>ConsumerConfidence gained per percentage-point-of-GDP spent on healthcare - "long-run productivity/wellbeing" modeled as consumer confidence.</summary>
        private const float HealthcareConfidenceSensitivity = 0.002f;

        /// <summary>BusinessConfidence gained per percentage-point-of-GDP spent on education - a better-skilled workforce modeled as business confidence.</summary>
        private const float EducationConfidenceSensitivity = 0.002f;

        /// <summary>Ceiling on PotentialGrowthRate - repeated infrastructure spending over many turns shouldn't be able to push trend growth past a sane bound.</summary>
        private const float MaxPotentialGrowthRate = 8f;

        /// <summary>Confidence bounds around the neutral 1.0 - repeated healthcare/education spending shouldn't be able to push Consumer/BusinessConfidence (which multiply Consumption/Investment) arbitrarily far, since that would eventually destabilize GDP.</summary>
        private const float MinConfidence = 0.7f;
        private const float MaxConfidence = 1.3f;

        /// <summary>
        /// Defense spending has no growth/confidence side-effect (only the approval effect in
        /// ApplyApprovalRating) - it's pure consumption in the G identity. Infrastructure nudges
        /// PotentialGrowthRate; healthcare nudges ConsumerConfidence; education nudges
        /// BusinessConfidence - each small and independently clamped.
        /// </summary>
        public static void ApplyCategorySpendingEffects(Country country, PolicyDecision decision)
        {
            EconomyState state = country.State;

            float infrastructurePercent = PercentOfGdp(decision.InfrastructureSpendingChange, state.GDP);
            country.PotentialGrowthRate = Mathf.Clamp(country.PotentialGrowthRate + InfrastructureGrowthSensitivity * infrastructurePercent, 0f, MaxPotentialGrowthRate);

            float healthcarePercent = PercentOfGdp(decision.HealthcareSpendingChange, state.GDP);
            state.ConsumerConfidence = Mathf.Clamp(state.ConsumerConfidence + HealthcareConfidenceSensitivity * healthcarePercent, MinConfidence, MaxConfidence);

            float educationPercent = PercentOfGdp(decision.EducationSpendingChange, state.GDP);
            state.BusinessConfidence = Mathf.Clamp(state.BusinessConfidence + EducationConfidenceSensitivity * educationPercent, MinConfidence, MaxConfidence);
        }

        // --- Welfare program side-effects: small, separable per-program profiles, mirroring the category spending effects above ---

        /// <summary>ConsumerConfidence gained per 100% GenerosityLevel of UBI - "modest Consumption/GDP boost" per the task's own framing, modeled as consumer confidence the same way Healthcare spending already is.</summary>
        private const float UbiConsumerConfidenceSensitivity = 0.03f;

        /// <summary>BusinessConfidence gained per 100% GenerosityLevel of UniversalHealthcare - reduced employer healthcare-cost burden, modeled as business confidence the same way Education spending already is.</summary>
        private const float UniversalHealthcareBusinessConfidenceSensitivity = 0.03f;

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
