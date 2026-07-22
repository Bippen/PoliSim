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

        /// <summary>
        /// Okun's Law: unemployment rises when actual GDP growth runs below potential/trend growth,
        /// and falls when it runs above, plus mean-reversion pulling it back toward the country's
        /// NAIRU. <paramref name="actualGrowthRatePercent"/> is this turn's realized GDP growth,
        /// computed by the caller from GDP before/after ApplyNationalAccounts.
        /// </summary>
        public static void ApplyOkunsLaw(Country country, float actualGrowthRatePercent)
        {
            EconomyState state = country.State;
            float growthGap = actualGrowthRatePercent - country.PotentialGrowthRate;
            float unemploymentChange = -OkunCoefficient * growthGap;
            unemploymentChange += UnemploymentReversionSpeed * (country.NaturalUnemploymentRate - state.Unemployment);

            state.Unemployment = Mathf.Clamp(state.Unemployment + unemploymentChange, 0f, MaxUnemploymentPercent);
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

        /// <summary>Debt-to-GDP above this (the same "safe" benchmark SimulationManager's risk premium uses) starts discounting the approval benefit of new spending - fiscal-strain awareness.</summary>
        private const float DeficitAwarenessDebtToGdpThreshold = 60f;

        /// <summary>Fraction of the spending-approval benefit removed per point of debt-to-GDP above the threshold.</summary>
        private const float DeficitAwarenessDampeningPerPoint = 0.01f;

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
        /// category-weighted spending effect whose benefit (not its cut-side penalty) is discounted
        /// the more debt-to-GDP already sits above a "safe" benchmark. Clamped to [0, 100].
        /// </summary>
        /// <param name="totalTaxHike">
        /// Sum of every positive per-tax-type rate increase actually applied this turn (clamped target
        /// minus prior rate, where positive) - computed by SimulationManager.ApplyTaxRateChanges
        /// before it overwrites TaxLine.Rate (PolicyDecision.TaxRateOverrides holds an absolute target,
        /// not a delta, so the hike can only be known by comparing against the pre-change rate) and
        /// threaded through here. Raising several taxes at once still compounds the penalty, same
        /// spirit as the old delta-based hike penalty.
        /// </param>
        public static void ApplyApprovalRating(Country country, PolicyDecision decision, float actualGrowthRatePercent, float totalTaxHike)
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
                EducationApprovalMultiplier * PercentOfGdp(decision.EducationSpendingChange, state.GDP);

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

            float reversion = ApprovalReversionSpeed * (NeutralApprovalRating - state.ApprovalRating);
            float delta = reversion + growthEffect - miseryPenalty - taxHikePenalty + spendingEffect;
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
    }
}
