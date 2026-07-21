using PoliSim.Data;
using UnityEngine;

namespace PoliSim.Simulation
{
    /// <summary>
    /// Textbook Taylor Rule: computes a "suggested" interest rate from a country's inflation gap
    /// and output gap. Pure reference data - never called from SimulationManager.AdvanceTurn, and
    /// never mutates state. Intended for a future UI hint or an AI-controlled country's decision
    /// logic; the player (or AI) is always free to set a different rate via PolicyDecision.
    /// </summary>
    public static class TaylorRule
    {
        /// <summary>Central bank inflation target, in percent.</summary>
        public const float InflationTarget = 2f;

        /// <summary>Assumed neutral real interest rate, in percent.</summary>
        public const float NeutralRealRate = 2f;

        /// <summary>Weight on the inflation gap (actual inflation minus target).</summary>
        public const float InflationGapWeight = 0.5f;

        /// <summary>Weight on the output gap (actual GDP minus potential GDP, as a percent of potential GDP).</summary>
        public const float OutputGapWeight = 0.5f;

        /// <summary>Output gap as a percentage of potential GDP: positive means the economy is running above trend.</summary>
        public static float GetOutputGapPercent(Country country)
        {
            EconomyState state = country.State;
            if (state.PotentialGDP <= 0f)
            {
                return 0f;
            }

            return (state.GDP - state.PotentialGDP) / state.PotentialGDP * 100f;
        }

        /// <summary>
        /// Suggested interest rate = neutral real rate + inflation + weighted inflation gap +
        /// weighted output gap. This is advisory only - nothing applies it automatically.
        /// </summary>
        public static float GetSuggestedInterestRate(Country country)
        {
            EconomyState state = country.State;
            float inflationGap = state.Inflation - InflationTarget;
            float outputGap = GetOutputGapPercent(country);

            float suggested = NeutralRealRate + state.Inflation + InflationGapWeight * inflationGap + OutputGapWeight * outputGap;
            return Mathf.Max(0f, suggested);
        }
    }
}
