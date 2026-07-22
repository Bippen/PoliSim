using System;
using System.Collections.Generic;

namespace PoliSim.Data
{
    /// <summary>
    /// The set of policy choices the player makes for a single turn.
    /// Most fields are deltas applied on top of the country's current values; TaxRateOverrides is
    /// the one exception (an absolute target, not a delta) - see its own doc comment.
    /// </summary>
    [Serializable]
    public class PolicyDecision
    {
        /// <summary>
        /// This turn's requested ABSOLUTE rate per TaxType (not a delta) - e.g. 45f means "set this
        /// tax's rate to 45%", not "raise it by 45 points". Only meaningful for TaxTypes the country
        /// currently has implemented; SimulationManager.ApplyTaxRateChanges clamps the requested value
        /// to that TaxType's TaxTypeRateRanges and sets TaxLine.Rate directly to the clamped result.
        /// A rate is directly settable in one turn (rather than nudged by a small delta each turn) so
        /// a meaningful policy shift doesn't take dozens of turns to reach. Implementing/removing a
        /// tax is a separate, immediate action (see Country.TaxLines/TaxLine.IsImplemented), not part
        /// of this dictionary.
        /// </summary>
        public Dictionary<TaxType, float> TaxRateOverrides = new Dictionary<TaxType, float>();

        /// <summary>
        /// This turn's requested dollar CHANGE per SpendingCategory (a delta, like the legacy
        /// category-spending fields below, NOT an absolute target like TaxRateOverrides) - only
        /// consumed for a country with a non-empty Country.SpendingLines (Phase 1: USA only), and
        /// only meaningful for Discretionary categories (Mandatory lines aren't player-adjustable in
        /// Phase 1 - see SimulationManager.ApplySpendingLineChanges, which ignores any entry for a
        /// Mandatory category). For such a country, this REPLACES the four legacy fields below as the
        /// player's actual spending input; SimulationManager derives equivalent values for those four
        /// fields from specific categories here (see BuildEffectiveDecisionForDetailedSpending) so
        /// MacroSystem's existing category-effect/approval formulas keep working unmodified.
        /// </summary>
        public Dictionary<SpendingCategory, float> SpendingLineChanges = new Dictionary<SpendingCategory, float>();

        /// <summary>Discretionary healthcare spending change this turn - see MacroSystem.ApplyCategorySpendingEffects for its confidence/approval profile. For a country with detailed SpendingLines, this is derived from SpendingLineChanges (HHSDiscretionary + Medicaid) rather than set directly by the player - see PolicyDecision.SpendingLineChanges.</summary>
        public float HealthcareSpendingChange;

        /// <summary>Discretionary defense spending change this turn - no growth/confidence side-effect, only a (smaller) approval effect. For a country with detailed SpendingLines, this is derived from SpendingLineChanges (Defense) rather than set directly by the player.</summary>
        public float DefenseSpendingChange;

        /// <summary>Discretionary infrastructure spending change this turn - nudges PotentialGrowthRate over time; see MacroSystem.ApplyCategorySpendingEffects. For a country with detailed SpendingLines, this is derived from SpendingLineChanges (Transportation) rather than set directly by the player.</summary>
        public float InfrastructureSpendingChange;

        /// <summary>Discretionary education spending change this turn - nudges BusinessConfidence over time; see MacroSystem.ApplyCategorySpendingEffects. For a country with detailed SpendingLines, this is derived from SpendingLineChanges (Education) rather than set directly by the player.</summary>
        public float EducationSpendingChange;

        /// <summary>
        /// Change to this country's CurrencyZone interest rate this turn, in percentage points.
        /// If multiple countries share a CurrencyZone (e.g. Germany/France/Italy), their changes
        /// are summed into one shared-zone rate change for the turn.
        /// </summary>
        public float InterestRateChange;

        /// <summary>Change to the country's own BaseTariffRate - only affects trade with countries it doesn't share a trade bloc with (see TradeSystem.GetTariffRate's precedence).</summary>
        public float TariffRateChange;

        /// <summary>
        /// Sum of the four legacy spending categories - the discretionary delta layered on top of the
        /// country's baseline GovernmentSpendingRate share of GDP in the national accounts identity's
        /// G term (see SimulationManager.ApplyDomesticPolicy). Positive is net extra stimulus,
        /// negative is a net cut. Only used for a country WITHOUT a detailed SpendingLines portfolio;
        /// such a country's G term instead sums its Discretionary SpendingLine amounts directly.
        /// </summary>
        public float TotalDiscretionarySpending =>
            HealthcareSpendingChange + DefenseSpendingChange + InfrastructureSpendingChange + EducationSpendingChange;

        public static PolicyDecision None()
        {
            return new PolicyDecision();
        }
    }
}
