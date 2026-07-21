using System;

namespace PoliSim.Data
{
    /// <summary>
    /// The set of policy choices the player makes for a single turn.
    /// Deltas are applied on top of the country's current values.
    /// </summary>
    [Serializable]
    public class PolicyDecision
    {
        /// <summary>Change to tax rate this turn, in percentage points (e.g. +2 raises tax rate by 2).</summary>
        public float TaxRateChange;

        /// <summary>
        /// Discretionary government spending change this turn, in the same units as GDP/Budget -
        /// added on top of the country's baseline GovernmentSpendingRate (not the total spending
        /// figure itself). Positive is extra stimulus, negative is a cut.
        /// </summary>
        public float GovernmentSpending;

        /// <summary>
        /// Change to this country's CurrencyZone interest rate this turn, in percentage points.
        /// If multiple countries share a CurrencyZone (e.g. Germany/France/Italy), their changes
        /// are summed into one shared-zone rate change for the turn.
        /// </summary>
        public float InterestRateChange;

        public static PolicyDecision None()
        {
            return new PolicyDecision { TaxRateChange = 0f, GovernmentSpending = 0f, InterestRateChange = 0f };
        }
    }
}
