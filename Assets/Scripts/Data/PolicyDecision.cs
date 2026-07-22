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
        /// This turn's requested ABSOLUTE GenerosityLevel per WelfareProgramType (not a delta) - e.g.
        /// 40f means "set this program's generosity to 40%", not "raise it by 40 points". Only
        /// meaningful for WelfareProgramTypes the country currently has implemented;
        /// SimulationManager.ApplyWelfareGenerosityChanges clamps the requested value to [0, 100] and
        /// sets WelfareProgram.GenerosityLevel directly to the clamped result - the exact same pattern
        /// as TaxRateOverrides/TaxLine.Rate. Implementing/removing a welfare program is a separate,
        /// immediate action (see Country.WelfarePrograms/WelfareProgram.IsImplemented), not part of
        /// this dictionary.
        /// </summary>
        public Dictionary<WelfareProgramType, float> WelfareGenerosityOverrides = new Dictionary<WelfareProgramType, float>();

        /// <summary>
        /// This turn's requested PERCENTAGE change per SpendingCategory (e.g. 15f means "+15% of that
        /// line's own current Amount", NOT a flat dollar delta and NOT an absolute target like
        /// TaxRateOverrides) - only consumed for a country with a non-empty Country.SpendingLines
        /// (Phase 1: USA only). Both Mandatory and Discretionary categories are adjustable;
        /// SimulationManager.ApplySpendingLineChanges clamps the requested percentage to a
        /// category-appropriate range before applying it (narrower for Mandatory - see
        /// SimulationManager.MandatoryPercentChangeRange/DiscretionaryPercentChangeRange) and converts
        /// it to that line's actual dollar change (Amount * percent / 100) before applying, so a +15%
        /// slider on an $850B line and a +15% slider on a $1B line move by proportionally different
        /// dollar amounts. For a country with a detailed portfolio, this REPLACES the four legacy
        /// fields below as the player's actual spending input; SimulationManager derives equivalent
        /// dollar values for those four fields from specific categories' actual applied change (see
        /// BuildEffectiveDecisionForDetailedSpending) so MacroSystem's existing category-effect/
        /// approval formulas keep working unmodified. Mandatory changes additionally feed
        /// MacroSystem.ApplyApprovalRating's own, distinctly higher-weighted approval term - see
        /// MandatorySpendingApprovalMultiplier.
        /// </summary>
        public Dictionary<SpendingCategory, float> SpendingLineChanges = new Dictionary<SpendingCategory, float>();

        /// <summary>Discretionary healthcare spending change this turn - see MacroSystem.ApplyCategorySpendingEffects for its confidence/approval profile. For a country with detailed SpendingLines, this is derived from SpendingLineChanges (HHSDiscretionary alone - Medicaid is Mandatory and feeds its own approval term instead, see MacroSystem.MandatorySpendingApprovalMultiplier) rather than set directly by the player - see PolicyDecision.SpendingLineChanges.</summary>
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
        /// This turn's requested ABSOLUTE tariff-rate override per trade-partner CountryId (not a
        /// delta) - e.g. 25f means "set my tariff on imports from this partner to 25%", overriding
        /// BaseTariffRate/trade-bloc resolution for that one relationship only. See
        /// TradeSystem.GetTariffRate's precedence and TradePartner.PlayerTariffOverride.
        /// SimulationManager.ApplyPartnerTariffOverrides clamps the requested rate (the same
        /// [MinBaseTariffRate, MaxBaseTariffRate] range BaseTariffRate itself uses) and sets it
        /// directly on that partner's TradePartner.PlayerTariffOverride, where it then persists turn
        /// to turn like TaxLine.Rate does - a no-op for any partner with no entry here. Clearing an
        /// override back to "no override" (default bloc/base-rate resolution) is a separate,
        /// immediate action on TradePartner.PlayerTariffOverride itself (see GameController's Trade
        /// tab Reset control), not something this dictionary does - matching how implementing/
        /// removing a tax is a separate immediate action from TaxRateOverrides.
        /// </summary>
        public Dictionary<CountryId, float> PartnerTariffOverrides = new Dictionary<CountryId, float>();

        /// <summary>
        /// This turn's requested ABSOLUTE minimum-wage level (percent of median wage, not a delta) -
        /// e.g. 55f means "set the minimum wage to 55% of the median wage". -1 (the default) means
        /// no change requested this turn - the same sentinel idiom Country.BaseDebtInterestRateOverride
        /// uses. Only meaningful for a country where Country.MinimumWageImplemented is true;
        /// SimulationManager.ApplyMinimumWageChange clamps the requested value to
        /// [MinMinimumWagePercent, MaxMinimumWagePercent] and sets Country.MinimumWagePercentOfMedian
        /// directly to the clamped result - the same pattern as TaxRateOverrides/TaxLine.Rate. There
        /// is no implement/remove action for this one (unlike TaxLine/WelfareProgram) - whether a
        /// country has a statutory minimum wage at all is a structural fact, not a player choice.
        /// </summary>
        public float MinimumWageOverride = -1f;

        /// <summary>
        /// This turn's requested ABSOLUTE police funding level (0-100, not a delta) - -1 (the
        /// default) means no change requested this turn. See SimulationManager.ApplyCrimePolicyChanges
        /// and Country.PoliceFundingLevel.
        /// </summary>
        public float PoliceFundingOverride = -1f;

        /// <summary>
        /// This turn's requested ABSOLUTE sentencing-policy level (0-100, not a delta) - -1 (the
        /// default) means no change requested this turn. See SimulationManager.ApplyCrimePolicyChanges
        /// and Country.SentencingSeverity.
        /// </summary>
        public float SentencingSeverityOverride = -1f;

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
