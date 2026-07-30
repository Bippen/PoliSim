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
        /// Unemployment points removed per point Country.OvertimeRegulationLevel sits above its
        /// neutral 50 (added per point below) - the "work-sharing" argument behind France's 35-hour
        /// week (stricter hour caps spread the same total work across more workers). A GENUINELY
        /// CONTESTED real economic claim, not a settled fact - some empirical studies find the
        /// 35-hour week didn't meaningfully reduce French unemployment as intended - so this is
        /// deliberately small, representing one side of that debate, not a confident modeling choice.
        /// </summary>
        private const float OvertimeUnemploymentSensitivity = 0.008f;

        private static float GetOvertimeUnemploymentAdjustment(Country country)
        {
            return -OvertimeUnemploymentSensitivity * (country.OvertimeRegulationLevel - NeutralPolicyDialLevel);
        }

        /// <summary>Unemployment points removed per point Country.RetrainingProgramLevel sits above its neutral 50 (added per point below) - the well-established real economic rationale that retraining eases job transitions, smaller than the overtime effect since it's a more indirect mechanism.</summary>
        private const float RetrainingUnemploymentSensitivity = 0.006f;

        private static float GetRetrainingUnemploymentAdjustment(Country country)
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
        public static void ApplyOkunsLaw(Country country, float actualGrowthRatePercent)
        {
            EconomyState state = country.State;
            float growthGap = actualGrowthRatePercent - country.PotentialGrowthRate;
            float unemploymentChange = -OkunCoefficient * growthGap;
            unemploymentChange += GetWelfareAdjustedReversionSpeed(country) * (country.NaturalUnemploymentRate - state.Unemployment);
            unemploymentChange += GetMinimumWageUnemploymentAdjustment(country);
            unemploymentChange += GetOvertimeUnemploymentAdjustment(country);
            unemploymentChange += GetRetrainingUnemploymentAdjustment(country);
            unemploymentChange += GetSectorUnemploymentAdjustment(country);

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
        /// ApplyPovertyRate/ApplyApprovalRating) rather than a new one. Now also targeted by two
        /// deeper-labor-market policy levers (see "Deeper Labor Market Policies" in CLAUDE.md):
        /// paid family leave (a gap versus the country's own seeded baseline, the same idiom
        /// MinimumWage's employment effect uses) and workforce retraining (a gap versus the shared
        /// neutral 50, the same idiom Police Funding/Sentencing Severity use). Hard-clamped to
        /// [0, 100].
        /// </summary>
        private const float PaidFamilyLeaveParticipationSensitivity = 0.02f;
        private const float RetrainingParticipationSensitivity = 0.01f;

        public static void ApplyLaborForceParticipationRate(Country country)
        {
            EconomyState state = country.State;
            float unemploymentGap = state.Unemployment - country.NaturalUnemploymentRate;
            float paidLeaveGap = country.PaidFamilyLeaveWeeks - country.BaselinePaidFamilyLeaveWeeks;
            float retrainingGap = country.RetrainingProgramLevel - NeutralPolicyDialLevel;
            float target = country.BaselineLaborForceParticipationRate
                - DiscouragedWorkerSensitivity * unemploymentGap
                + PaidFamilyLeaveParticipationSensitivity * paidLeaveGap
                + RetrainingParticipationSensitivity * retrainingGap;
            state.LaborForceParticipationRate = Mathf.Clamp(
                state.LaborForceParticipationRate + LaborForceParticipationReversionSpeed * (target - state.LaborForceParticipationRate),
                0f, MaxLaborForceParticipationPercent);
        }

        // --- Crime & Justice: a stylized CrimeIndex, mean-reverting toward its own baseline ---

        /// <summary>Fraction of the gap versus the target that closes each turn on its own - matches PovertyRate/LaborForceParticipationRate's own moderate-slow reversion speed.</summary>
        private const float CrimeIndexReversionSpeed = 0.15f;

        /// <summary>CrimeIndex points added per point Unemployment sits above NaturalUnemploymentRate - reuses an already-proven driver (the same gap PovertyRate/ApplyApprovalRating already use) rather than inventing a new one; property crime's real-world link to joblessness is well documented, though modest relative to policy's own effect below.</summary>
        private const float CrimeUnemploymentSensitivity = 0.3f;

        /// <summary>CrimeIndex points reduced per point Country.PoliceFundingLevel sits above its neutral 50 (and increased per point below) - a real, well-documented deterrence/response-capacity effect. The larger of the two policy sensitivities - see SentencingSensitivity.</summary>
        private const float PoliceFundingSensitivity = 0.16f;

        /// <summary>CrimeIndex points reduced per point Country.SentencingSeverity sits above its neutral 50 - deliberately HALF of PoliceFundingSensitivity, reflecting the well-established criminology finding (Nagin and others) that the CERTAINTY of enforcement deters crime more reliably than the SEVERITY of punishment, which has a smaller, more debated effect.</summary>
        private const float SentencingSensitivity = 0.08f;

        /// <summary>Neutral reference point for both policy dials - both start here for every country (a uniform placeholder, unlike CrimeIndex's own per-country baseline), so a gap versus this constant (not a country-specific anchor) is the correct comparison.</summary>
        private const float NeutralPolicyDialLevel = 50f;

        private const float MaxCrimeIndexPercent = 100f;

        /// <summary>
        /// CrimeIndex points added per point Country.BailReformLevel sits above its neutral 50 (Round
        /// 2's "Deeper Crime &amp; Justice") - small and HONESTLY CONTESTED, the same "flag the real
        /// debate, don't pretend it's settled" treatment OvertimeRegulationLevel's own Unemployment
        /// effect already got in "Deeper Labor Market Policies": bail reform's real effect on crime is
        /// genuinely disputed in criminology research, not a settled empirical fact.
        /// </summary>
        private const float BailReformCrimeIndexSensitivity = 0.02f;

        /// <summary>
        /// CrimeIndex mean-reverts toward a target of Country.BaselineCrimeIndex, adjusted by the
        /// Unemployment-versus-NAIRU gap (a modest, already-proven driver), by how far
        /// PoliceFundingLevel/SentencingSeverity sit from their shared neutral 50 - higher police
        /// funding or harsher sentencing both reduce the target, funding more strongly than
        /// sentencing (see PoliceFundingSensitivity/SentencingSensitivity) - and now by BailReformLevel
        /// (see BailReformCrimeIndexSensitivity). Hard-clamped to [0, 100].
        /// </summary>
        public static void ApplyCrimeIndex(Country country)
        {
            EconomyState state = country.State;
            float unemploymentGap = state.Unemployment - country.NaturalUnemploymentRate;
            float target = country.BaselineCrimeIndex
                + CrimeUnemploymentSensitivity * unemploymentGap
                - PoliceFundingSensitivity * (country.PoliceFundingLevel - NeutralPolicyDialLevel)
                - SentencingSensitivity * (country.SentencingSeverity - NeutralPolicyDialLevel)
                + BailReformCrimeIndexSensitivity * (country.BailReformLevel - NeutralPolicyDialLevel);

            state.CrimeIndex = Mathf.Clamp(state.CrimeIndex + CrimeIndexReversionSpeed * (target - state.CrimeIndex), 0f, MaxCrimeIndexPercent);
        }

        /// <summary>BusinessConfidence points lost per point CrimeIndex sits above Country.BaselineCrimeIndex (and gained per point below) - higher-than-baseline crime deters investment, a real and well-documented effect, kept small since Confidence directly multiplies Investment.</summary>
        private const float CrimeBusinessConfidenceSensitivity = 0.0015f;

        /// <summary>
        /// CrimeIndex's ongoing effect on BusinessConfidence - a GAP versus Country.BaselineCrimeIndex
        /// (not an absolute level), the same "gaps, not levels" idiom ApplyApprovalRating/
        /// ApplyPovertyRate already use, so a country with a structurally higher baseline (e.g. the
        /// USA) isn't penalized just for sitting at its own normal equilibrium. Clamped to
        /// [MinConfidence, MaxConfidence] alongside ApplyCategorySpendingEffects/
        /// ApplyWelfareProgramEffects' own nudges.
        /// </summary>
        public static void ApplyCrimeEffects(Country country)
        {
            EconomyState state = country.State;
            float crimeGap = state.CrimeIndex - country.BaselineCrimeIndex;
            state.BusinessConfidence = Mathf.Clamp(state.BusinessConfidence - CrimeBusinessConfidenceSensitivity * crimeGap, MinConfidence, MaxConfidence);
        }

        // --- Prison Population Rate: a real, per-100k tracked stat, mean-reverting toward its own baseline (Round 2's "Deeper Crime & Justice") ---

        /// <summary>Fraction of the gap versus the target that closes each turn on its own - matches CrimeIndex/PovertyRate's own moderate-slow reversion speed.</summary>
        private const float PrisonPopulationReversionSpeed = 0.15f;

        /// <summary>PrisonPopulationRate points reduced per point Country.BailReformLevel sits above its neutral 50 (and added per point below) - bail reform's primary real-world goal is reducing pretrial detention, a direct and substantial real effect (pretrial detainees are a significant share of incarcerated populations, especially in the US).</summary>
        private const float BailReformPrisonPopulationSensitivity = 2.0f;

        /// <summary>PrisonPopulationRate points added per point Country.DrugPolicyLevel sits above its neutral 50 (and reduced per point below) - the well-documented real link between strict drug enforcement and mass incarceration (the US "war on drugs" being the clearest real-world example).</summary>
        private const float DrugPolicyPrisonPopulationSensitivity = 1.6f;

        /// <summary>Gameplay safety bound, comfortably above any real-world incarceration rate (the USA's real ~531 per 100k is already the highest among developed nations).</summary>
        private const float MaxPrisonPopulationRate = 1000f;

        /// <summary>
        /// PrisonPopulationRate mean-reverts toward a target of Country.BaselinePrisonPopulationRate,
        /// adjusted by BailReformLevel (reform reduces it) and DrugPolicyLevel (stricter enforcement
        /// raises it) - both gaps versus their shared neutral 50. Hard-clamped to [0, 1000].
        /// </summary>
        public static void ApplyPrisonPopulationRate(Country country)
        {
            EconomyState state = country.State;
            float target = country.BaselinePrisonPopulationRate
                - BailReformPrisonPopulationSensitivity * (country.BailReformLevel - NeutralPolicyDialLevel)
                + DrugPolicyPrisonPopulationSensitivity * (country.DrugPolicyLevel - NeutralPolicyDialLevel);

            state.PrisonPopulationRate = Mathf.Clamp(state.PrisonPopulationRate + PrisonPopulationReversionSpeed * (target - state.PrisonPopulationRate), 0f, MaxPrisonPopulationRate);
        }

        // --- Economic Sectors: descriptive tracked breakdowns, isolated from the core GDP/unemployment/inflation loop (see CLAUDE.md's "Economic Sectors") ---

        /// <summary>Fraction of the gap versus each sector stat's target that closes each turn on its own - matches PovertyRate/CrimeIndex's own moderate-slow reversion speed.</summary>
        private const float SectorReversionSpeed = 0.15f;

        /// <summary>Points added per point a sector's SubsidyLevel sits above its neutral 50 (and removed per point below) - applied uniformly to Output/Employment/SectorMetric in this first pass, deliberately not wired to the budget (see CLAUDE.md).</summary>
        private const float SectorSubsidySensitivity = 0.04f;

        /// <summary>Points removed per point a sector's RegulationLevel sits above its neutral 50 (and added per point below) - a compliance-cost tradeoff, deliberately smaller than nothing else competes with it in this isolated pass.</summary>
        private const float SectorRegulationSensitivity = 0.04f;

        /// <summary>Round 3 item 2: points added per point a sector's TaxCreditLevel sits above its neutral 50 - same magnitude and uniform-across-stats shape as SectorSubsidySensitivity, since a tax credit and a direct subsidy have a similar practical effect in this stylized model.</summary>
        private const float SectorTaxCreditSensitivity = 0.04f;

        /// <summary>Round 3 item 2: points added to Output/SectorMetric per point a sector's ResearchGrantsLevel sits above its neutral 50 - same magnitude as SectorSubsidySensitivity, since R&D funding most directly targets output/innovation.</summary>
        private const float SectorResearchGrantsSensitivity = 0.04f;

        /// <summary>Round 3 item 2: points added to Employment per point a sector's ResearchGrantsLevel sits above its neutral 50 - HALF SectorResearchGrantsSensitivity, deliberately smaller: grants fund research projects and output, not broad hiring, unlike a direct Subsidy.</summary>
        private const float SectorResearchGrantsEmploymentSensitivity = 0.02f;

        /// <summary>Round 3 item 2: points added to Output/SectorMetric (and REMOVED from Employment - see ApplySectorEffects) per point a sector's DeregulationNationalizationLevel sits above its neutral 50 - the real, well-documented state-owned-enterprise tradeoff (privatization/deregulation gains efficiency by shedding excess labor; nationalization preserves jobs at an efficiency cost).</summary>
        private const float SectorDeregulationSensitivity = 0.04f;

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
                sector.OutputShareOfGdp = Mathf.Max(0f, sector.OutputShareOfGdp + SectorReversionSpeed * (outputTarget - sector.OutputShareOfGdp));

                float employmentTarget = sector.BaselineEmploymentShare + employmentAdjustment;
                sector.EmploymentShare = Mathf.Max(0f, sector.EmploymentShare + SectorReversionSpeed * (employmentTarget - sector.EmploymentShare));

                float metricTarget = sector.BaselineSectorMetric + outputAndMetricAdjustment;
                sector.SectorMetric = Mathf.Max(0f, sector.SectorMetric + SectorReversionSpeed * (metricTarget - sector.SectorMetric));
            }
        }

        // --- Infrastructure Condition: a decay/investment stock model (Round 2's "Infrastructure system") ---

        /// <summary>ConditionIndex points lost per turn to deferred maintenance, absent any incremental Infrastructure spending increase this turn - infrastructure needs growing real investment merely to hold steady (rising usage, materials aging, tech obsolescence), so a flat spending level still implies gradual real degradation. Deliberately small, and hard-clamped below so it can never diverge - see InfrastructureAsset.cs for why this is a stock model, not a gap-to-baseline one.</summary>
        private const float InfrastructureDecayRatePerTurn = 0.08f;

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
        private static float GetSectorGrowthAdjustment(Country country)
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

        private static float GetSectorUnemploymentAdjustment(Country country)
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

        /// <summary>Approval points gained per week Country.PaidFamilyLeaveWeeks sits above its own seeded BaselinePaidFamilyLeaveWeeks (and lost per week below) - a small, real political effect (paid-leave policy tends to be popular).</summary>
        private const float PaidFamilyLeaveApprovalSensitivity = 0.05f;

        /// <summary>Approval points gained per point Country.DrugPolicyLevel sits above its neutral 50 (and lost per point below) - a small "tough on crime" political effect, gap versus the shared neutral 50 rather than a country-specific anchor (DrugPolicyLevel has no real per-country seed the way PaidFamilyLeaveWeeks does).</summary>
        private const float DrugPolicyApprovalSensitivity = 0.02f;

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
        /// Phase 2 (see "Detailed Spending Portfolio Phase 2" in CLAUDE.md) - four more categories
        /// join the weighted-spending approval term. Justice/energy sit at the baseline (like
        /// Infrastructure); homeland security sits between Defense's low popularity and the
        /// baseline (broad, if not universal, appeal for border/disaster-response spending); housing
        /// is relatively popular (like Healthcare/Education, though slightly less so) - illustrative,
        /// gameplay-tuning judgment calls, the same as the original four's own multipliers.
        /// </summary>
        private const float JusticeApprovalMultiplier = 1.0f;
        private const float HomelandSecurityApprovalMultiplier = 0.7f;
        private const float EnergyApprovalMultiplier = 1.0f;
        private const float HousingApprovalMultiplier = 1.3f;

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
            float crimePenaltyGap = state.CrimeIndex - country.BaselineCrimeIndex;
            float miseryPenalty = UnemploymentApprovalSensitivity * unemploymentPenaltyGap
                + InflationApprovalSensitivity * inflationPenaltyGap
                + CrimeApprovalSensitivity * crimePenaltyGap;

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
        private const float InfrastructureGrowthSensitivity = 0.01f;

        /// <summary>ConsumerConfidence gained per percentage-point-of-GDP spent on healthcare - "long-run productivity/wellbeing" modeled as consumer confidence.</summary>
        private const float HealthcareConfidenceSensitivity = 0.002f;

        /// <summary>BusinessConfidence gained per percentage-point-of-GDP spent on education - a better-skilled workforce modeled as business confidence.</summary>
        private const float EducationConfidenceSensitivity = 0.002f;

        /// <summary>
        /// Phase 2 (see "Detailed Spending Portfolio Phase 2" in CLAUDE.md) - three more categories
        /// get their own persistent, lasting effect, mirroring Infrastructure/Healthcare/Education's
        /// own "one-turn spending change permanently nudges a structural value" pattern exactly
        /// (HomelandSecurity deliberately gets none, mirroring Defense's own "approval only" pattern).
        /// CrimeIndex points reduced (permanently, off Country.BaselineCrimeIndex) per
        /// percentage-point-of-GDP spent on justice - court/prosecution capacity genuinely affects
        /// case backlogs and enforcement outcomes.
        /// </summary>
        private const float JusticeCrimeIndexSensitivity = 0.02f;

        /// <summary>BusinessConfidence gained per percentage-point-of-GDP spent on energy - lower/stabler energy costs for businesses, distinct from Education's own BusinessConfidence nudge.</summary>
        private const float EnergyConfidenceSensitivity = 0.0015f;

        /// <summary>PovertyRate baseline points reduced (permanently, off Country.BaselinePovertyRate) per percentage-point-of-GDP spent on housing - HUD-style baseline federal housing support, smaller than the dedicated player-adjustable WelfareProgramType.HousingAssistance's own sensitivity since this is a much narrower, less-targeted budget line.</summary>
        private const float HousingPovertyReductionSensitivity = 0.015f;

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
