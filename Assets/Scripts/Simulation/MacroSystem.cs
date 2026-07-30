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
        /// by the same Unemployment-versus-NAIRU gap already used elsewhere (a proven, ALREADY-
        /// established driver - see ApplyPovertyRate/ApplyApprovalRating - kept OUTSIDE the combined
        /// ceiling below, the same way PotentialGrowthRate's own combined ceiling leaves
        /// BasePotentialGrowthRate itself outside it). Also targeted by two deeper-labor-market policy
        /// levers (see "Deeper Labor Market Policies" in CLAUDE.md): paid family leave (a gap versus
        /// the country's own seeded baseline, the same idiom MinimumWage's employment effect uses) and
        /// workforce retraining (a gap versus the shared neutral 50, the same idiom Police Funding/
        /// Sentencing Severity use). Hard-clamped to [0, 100].
        /// </summary>
        private const float PaidFamilyLeaveParticipationSensitivity = 0.02f;
        private const float RetrainingParticipationSensitivity = 0.01f;

        /// <summary>Round 3 item 5, Part A: LaborForceParticipationRate points reduced per point DependencyRatio sits above its own Country.BaselineDependencyRatio - a real, well-documented effect: an aging population structurally shrinks the working-age share, lowering participation even with no change in any individual's own behavior.</summary>
        private const float DependencyRatioParticipationSensitivity = 0.02f;

        /// <summary>Round 3 item 5, Part A: LaborForceParticipationRate points added per point NetMigrationRate sits above its own Country.BaselineNetMigrationRate - a real, well-documented effect: immigrants skew disproportionately working-age, so higher net migration than a country's own starting norm raises participation.</summary>
        private const float NetMigrationParticipationSensitivity = 0.03f;

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

        public static void ApplyLaborForceParticipationRate(Country country)
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

        // --- Round 3 item 3: Organized Crime and Corruption, two more stylized 0-100 tracked stats ---
        // ApplyOrganizedCrimeIndex/ApplyCorruptionIndex must run BEFORE ApplyCrimeIndex (see below) so
        // its own OrganizedCrimeIndex term reads THIS turn's freshly-updated value, the same
        // "must see this turn's just-updated value" timing requirement Infrastructure Feedback's
        // condition-drag already established - see SimulationManager's call ordering.

        /// <summary>Fraction of the gap versus the target that closes each turn on its own - matches CrimeIndex/PovertyRate's own moderate-slow reversion speed.</summary>
        private const float OrganizedCrimeReversionSpeed = 0.15f;

        /// <summary>OrganizedCrimeIndex points reduced per point Country.PoliceFundingLevel sits above its neutral 50 (and increased per point below) - policing already fights organized crime in reality, reusing this existing lever rather than requiring a brand-new one for this specific link. Smaller than its own primary levers below - a secondary contributor.</summary>
        private const float PoliceFundingOrganizedCrimeSensitivity = 0.06f;

        /// <summary>OrganizedCrimeIndex points reduced per point Country.BorderEnforcementLevel sits above its neutral 50 (and increased per point below) - stricter border enforcement disrupts cross-border smuggling/trafficking, organized crime's real, well-documented core activity. The primary lever for this stat.</summary>
        private const float BorderEnforcementOrganizedCrimeSensitivity = 0.12f;

        /// <summary>OrganizedCrimeIndex points reduced per point Country.JudicialFundingLevel sits above its neutral 50 (and increased per point below) - better-funded prosecution capacity disrupts organized-crime networks, a real secondary contributor alongside BorderEnforcementLevel's more direct effect.</summary>
        private const float JudicialFundingOrganizedCrimeSensitivity = 0.06f;

        /// <summary>
        /// OrganizedCrimeIndex mean-reverts toward a target of Country.BaselineOrganizedCrimeIndex,
        /// adjusted by how far PoliceFundingLevel/BorderEnforcementLevel/JudicialFundingLevel sit from
        /// their shared neutral 50 - all three reduce the target when above it. Hard-clamped to
        /// [0, 100], the same scale as CrimeIndex.
        /// </summary>
        public static void ApplyOrganizedCrimeIndex(Country country)
        {
            EconomyState state = country.State;
            float target = country.BaselineOrganizedCrimeIndex
                - PoliceFundingOrganizedCrimeSensitivity * (country.PoliceFundingLevel - NeutralPolicyDialLevel)
                - BorderEnforcementOrganizedCrimeSensitivity * (country.BorderEnforcementLevel - NeutralPolicyDialLevel)
                - JudicialFundingOrganizedCrimeSensitivity * (country.JudicialFundingLevel - NeutralPolicyDialLevel);

            state.OrganizedCrimeIndex = Mathf.Clamp(state.OrganizedCrimeIndex + OrganizedCrimeReversionSpeed * (target - state.OrganizedCrimeIndex), 0f, MaxCrimeIndexPercent);
        }

        /// <summary>Fraction of the gap versus the target that closes each turn on its own - matches CrimeIndex/PovertyRate's own moderate-slow reversion speed.</summary>
        private const float CorruptionReversionSpeed = 0.15f;

        /// <summary>CorruptionIndex points reduced per point Country.JudicialFundingLevel sits above its neutral 50 (and increased per point below) - an independent, well-funded judiciary is a canonical real-world anti-corruption mechanism. The sole lever for this stat in this pass.</summary>
        private const float JudicialFundingCorruptionSensitivity = 0.14f;

        /// <summary>
        /// CorruptionIndex mean-reverts toward a target of Country.BaselineCorruptionIndex, adjusted
        /// by how far JudicialFundingLevel sits from its neutral 50 - higher funding reduces the
        /// target. Hard-clamped to [0, 100], the same scale as CrimeIndex.
        /// </summary>
        public static void ApplyCorruptionIndex(Country country)
        {
            EconomyState state = country.State;
            float target = country.BaselineCorruptionIndex
                - JudicialFundingCorruptionSensitivity * (country.JudicialFundingLevel - NeutralPolicyDialLevel);

            state.CorruptionIndex = Mathf.Clamp(state.CorruptionIndex + CorruptionReversionSpeed * (target - state.CorruptionIndex), 0f, MaxCrimeIndexPercent);
        }

        // --- Demographics: Population, birth/death/migration rates, and a single dependency-ratio aging proxy (Round 3 item 5, Part A) ---

        /// <summary>Points BirthRate declines per turn on its own - a real, well-documented, near-universal secular fertility decline across developed nations, not a country-specific policy response. Deliberately small (over a 500-turn run this alone would move BirthRate by 5 points, well before which the lower-starting countries hit MinBirthRate and stop).</summary>
        private const float BirthRateSecularDeclineRate = 0.01f;

        /// <summary>Realistic low-fertility floor, informed by the real world's own lowest-ever recorded national crude birth rates (some East Asian countries have fallen to roughly this range in recent years) - not literally zero, since no country's birth rate has realistically approached that.</summary>
        private const float MinBirthRate = 5f;

        /// <summary>Points DeathRate rises per point DependencyRatio sits above its own Country.BaselineDependencyRatio - a real, well-documented mechanical effect: an aging population structurally raises the crude death rate even with no change in age-specific mortality, since a larger share of the population is simply older.</summary>
        private const float DeathRateAgingDriftSensitivity = 0.003f;

        /// <summary>Generous gameplay safety bound on DeathRate - comfortably above any real-world crude death rate this model's own DependencyRatio ceiling could mechanically produce.</summary>
        private const float MaxDeathRate = 25f;

        /// <summary>Points NetMigrationRate rises per point DependencyRatio sits above its own Country.BaselineDependencyRatio - a real, discussed phenomenon: aging developed economies lean more on immigration over time to offset a shrinking working-age population. Deliberately a SEPARATE driver from BirthRate's own independent secular-decline drift - fertility decline isn't itself "caused" by a country's current dependency ratio the way this migration-reliance trend plausibly is.</summary>
        private const float MigrationAgingDriftSensitivity = 0.002f;

        /// <summary>Generous gameplay safety bounds on NetMigrationRate - wide enough for Part B's Immigration Policy lever to swing meaningfully in either direction (open vs. restrictive) without an artificial mid-range ceiling getting in the way first.</summary>
        private const float MinNetMigrationRate = -15f;
        private const float MaxNetMigrationRate = 15f;

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
        /// Must run BEFORE ApplyPopulationGrowth, which reads these same-turn freshly-updated rates -
        /// the same "must see this turn's just-updated value" timing requirement Infrastructure
        /// Feedback's condition-drag already established.
        /// </summary>
        public static void ApplyDemographicRates(Country country)
        {
            EconomyState state = country.State;

            state.BirthRate = Mathf.Max(MinBirthRate, state.BirthRate - BirthRateSecularDeclineRate);

            float birthDeathGap = state.DeathRate - state.BirthRate;
            state.DependencyRatio = Mathf.Clamp(
                state.DependencyRatio + DependencyRatioDriftSensitivity * Mathf.Max(0f, birthDeathGap),
                MinDependencyRatio, MaxDependencyRatio);

            float dependencyGap = Mathf.Max(0f, state.DependencyRatio - country.BaselineDependencyRatio);
            state.DeathRate = Mathf.Clamp(state.DeathRate + DeathRateAgingDriftSensitivity * dependencyGap, 0f, MaxDeathRate);
            state.NetMigrationRate = Mathf.Clamp(state.NetMigrationRate + MigrationAgingDriftSensitivity * dependencyGap, MinNetMigrationRate, MaxNetMigrationRate);
        }

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
        /// Real years represented by one turn, derived from this project's own established
        /// turn-to-calendar-time convention (`ElectionSystem.ElectionCycle` = 12 turns per presidential
        /// term = 4 real years - NOT the looser "1 turn ~= 1 year" approximation this section originally
        /// assumed). BirthRate/DeathRate/NetMigrationRate/SteadyStateGrowthRate are all real, per-1,000-
        /// population-PER-YEAR figures (that's how the source data is expressed and how they're
        /// documented) - applying them to Population UNSCALED on every turn would apply a full year's
        /// worth of demographic change 3x too often relative to real elapsed time (500 turns is ~167
        /// real years, not 500), which is exactly what the original version of this fix did: the growth
        /// RATE itself plateaued correctly, but Population still ended up compounding roughly 3x more
        /// cumulative change than a faithful real-world extrapolation over the correct 167-year horizon
        /// supports. Scaling each turn's applied growth by this fraction is the actual fix - tightening
        /// MaxPopulationGrowthRateDeviation/PopulationGrowthReversionSpeed alone was tried first and
        /// confirmed (empirically, via a throwaway diagnostic) insufficient, since neither one addresses
        /// the real defect (over-compounding via too many applications of an annual-scale rate), only how
        /// far/fast the rate itself moves.
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
        public static void ApplyPopulationGrowth(Country country)
        {
            EconomyState state = country.State;
            float impliedRate = state.BirthRate - state.DeathRate + state.NetMigrationRate;
            float boundedGap = Mathf.Clamp(impliedRate - country.SteadyStateGrowthRate, -MaxPopulationGrowthRateDeviation, MaxPopulationGrowthRateDeviation);
            float target = country.SteadyStateGrowthRate + PopulationGrowthRateSensitivity * boundedGap;
            state.PopulationGrowthRate += PopulationGrowthReversionSpeed * (target - state.PopulationGrowthRate);
            state.Population = Mathf.Clamp(state.Population * (1f + state.PopulationGrowthRate / 1000f * YearsPerTurn), MinPopulation, MaxPopulation);
        }

        /// <summary>
        /// CrimeIndex points added per point Country.BailReformLevel sits above its neutral 50 (Round
        /// 2's "Deeper Crime &amp; Justice") - small and HONESTLY CONTESTED, the same "flag the real
        /// debate, don't pretend it's settled" treatment OvertimeRegulationLevel's own Unemployment
        /// effect already got in "Deeper Labor Market Policies": bail reform's real effect on crime is
        /// genuinely disputed in criminology research, not a settled empirical fact.
        /// </summary>
        private const float BailReformCrimeIndexSensitivity = 0.02f;

        /// <summary>Round 3 item 3: CrimeIndex points added per point OrganizedCrimeIndex sits above Country.BaselineOrganizedCrimeIndex (and reduced per point below) - organized crime activity is a real, direct contributor to overall crime levels in most criminological frameworks. Deliberately modest so overall CrimeIndex isn't dominated by this one secondary contributor.</summary>
        private const float OrganizedCrimeIndexSensitivity = 0.1f;

        /// <summary>
        /// CrimeIndex mean-reverts toward a target of Country.BaselineCrimeIndex, adjusted by the
        /// Unemployment-versus-NAIRU gap (a modest, already-proven driver), by how far
        /// PoliceFundingLevel/SentencingSeverity sit from their shared neutral 50 - higher police
        /// funding or harsher sentencing both reduce the target, funding more strongly than
        /// sentencing (see PoliceFundingSensitivity/SentencingSensitivity) - by BailReformLevel (see
        /// BailReformCrimeIndexSensitivity), and now by the OrganizedCrimeIndex gap versus its own
        /// baseline (Round 3 item 3 - see OrganizedCrimeIndexSensitivity). Hard-clamped to [0, 100].
        /// </summary>
        public static void ApplyCrimeIndex(Country country)
        {
            EconomyState state = country.State;
            float unemploymentGap = state.Unemployment - country.NaturalUnemploymentRate;
            float organizedCrimeGap = state.OrganizedCrimeIndex - country.BaselineOrganizedCrimeIndex;
            float target = country.BaselineCrimeIndex
                + CrimeUnemploymentSensitivity * unemploymentGap
                - PoliceFundingSensitivity * (country.PoliceFundingLevel - NeutralPolicyDialLevel)
                - SentencingSensitivity * (country.SentencingSeverity - NeutralPolicyDialLevel)
                + BailReformCrimeIndexSensitivity * (country.BailReformLevel - NeutralPolicyDialLevel)
                + OrganizedCrimeIndexSensitivity * organizedCrimeGap;

            state.CrimeIndex = Mathf.Clamp(state.CrimeIndex + CrimeIndexReversionSpeed * (target - state.CrimeIndex), 0f, MaxCrimeIndexPercent);
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
        public static void ApplyCrimeEffects(Country country)
        {
            EconomyState state = country.State;
            float crimeGap = state.CrimeIndex - country.BaselineCrimeIndex;
            float organizedCrimeGap = state.OrganizedCrimeIndex - country.BaselineOrganizedCrimeIndex;
            float corruptionGap = state.CorruptionIndex - country.BaselineCorruptionIndex;
            float confidenceAdjustment = CrimeBusinessConfidenceSensitivity * crimeGap
                + OrganizedCrimeBusinessConfidenceSensitivity * organizedCrimeGap
                + CorruptionBusinessConfidenceSensitivity * corruptionGap;
            state.BusinessConfidence = Mathf.Clamp(state.BusinessConfidence - confidenceAdjustment, MinConfidence, MaxConfidence);
        }

        // --- Prison Population Rate: a real, per-100k tracked stat, mean-reverting toward its own baseline (Round 2's "Deeper Crime & Justice") ---

        /// <summary>Fraction of the gap versus the target that closes each turn on its own - matches CrimeIndex/PovertyRate's own moderate-slow reversion speed.</summary>
        private const float PrisonPopulationReversionSpeed = 0.15f;

        /// <summary>PrisonPopulationRate points reduced per point Country.BailReformLevel sits above its neutral 50 (and added per point below) - bail reform's primary real-world goal is reducing pretrial detention, a direct and substantial real effect (pretrial detainees are a significant share of incarcerated populations, especially in the US).</summary>
        private const float BailReformPrisonPopulationSensitivity = 2.0f;

        /// <summary>PrisonPopulationRate points added per point Country.DrugPolicyLevel sits above its neutral 50 (and reduced per point below) - the well-documented real link between strict drug enforcement and mass incarceration (the US "war on drugs" being the clearest real-world example).</summary>
        private const float DrugPolicyPrisonPopulationSensitivity = 1.6f;

        /// <summary>Round 3 item 3: PrisonPopulationRate points reduced per point Country.JudicialFundingLevel sits above its neutral 50 (and added per point below) - a real, well-documented indirect effect: well-funded courts process cases faster, reducing the pretrial-detention backlog that swells incarceration in underfunded systems. Deliberately smaller than BailReformPrisonPopulationSensitivity's direct mechanical effect, since this is a secondary, capacity-driven channel, not bail policy's own primary lever.</summary>
        private const float JudicialFundingPrisonPopulationSensitivity = 0.8f;

        /// <summary>Gameplay safety bound, comfortably above any real-world incarceration rate (the USA's real ~531 per 100k is already the highest among developed nations).</summary>
        private const float MaxPrisonPopulationRate = 1000f;

        /// <summary>
        /// PrisonPopulationRate mean-reverts toward a target of Country.BaselinePrisonPopulationRate,
        /// adjusted by BailReformLevel (reform reduces it), DrugPolicyLevel (stricter enforcement
        /// raises it), and JudicialFundingLevel (more funding reduces it via faster case processing -
        /// Round 3 item 3) - all gaps versus their shared neutral 50. Hard-clamped to [0, 1000].
        /// </summary>
        public static void ApplyPrisonPopulationRate(Country country)
        {
            EconomyState state = country.State;
            float target = country.BaselinePrisonPopulationRate
                - BailReformPrisonPopulationSensitivity * (country.BailReformLevel - NeutralPolicyDialLevel)
                + DrugPolicyPrisonPopulationSensitivity * (country.DrugPolicyLevel - NeutralPolicyDialLevel)
                - JudicialFundingPrisonPopulationSensitivity * (country.JudicialFundingLevel - NeutralPolicyDialLevel);

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

        /// <summary>Round 3 item 3: Approval points lost per point CorruptionIndex sits above Country.BaselineCorruptionIndex (and gained per point below) - corruption scandals hurt approval, a real and well-documented political effect. Slightly smaller than CrimeApprovalSensitivity, since corruption's political salience varies more by country/culture than crime's does - a stylized judgment call, not a precisely-fitted figure.</summary>
        private const float CorruptionApprovalSensitivity = 0.15f;

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
