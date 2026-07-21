using PoliSim.Data;
using UnityEngine;

namespace PoliSim.Simulation
{
    /// <summary>
    /// Core macroeconomic theory driving GDP, unemployment, and inflation: the national accounts
    /// identity (GDP = C + I + G + NX), Okun's Law (unemployment vs. growth gap), and an
    /// expectations-augmented Phillips Curve (inflation vs. unemployment gap). Each relationship is
    /// its own small method with named, tunable constants rather than one combined formula.
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

        /// <summary>Smallest GDP a country can fall to - keeps a shrinking economy able to recover instead of locking at exactly 0 (0 * anything is still 0).</summary>
        private const float MinGdp = 1f;

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

        /// <summary>Gameplay ceiling for inflation - a bug elsewhere in the feedback chain should never be able to push this past a sane bound.</summary>
        private const float MaxInflationPercent = 30f;

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
    }
}
