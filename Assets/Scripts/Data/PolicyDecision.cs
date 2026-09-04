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

        /// <summary>P5-B2 (2026-09-05): the line SET to a nominal amount (the country's -scale unit) this turn - the Budget screen's slider sets the figure directly; clamped to the seed band like every other line mutation. Persisting: the amount stays and indexes from there.</summary>
        public Dictionary<SpendingCategory, float> SpendingNominalTargets = new Dictionary<SpendingCategory, float>();

        /// <summary>P5-B2: pin (true) or unpin (false) a line this turn - a pinned line holds its nominal amount instead of indexing to prices and its driver.</summary>
        public Dictionary<SpendingCategory, bool> SpendingPinChanges = new Dictionary<SpendingCategory, bool>();

        /// <summary>Discretionary healthcare spending change this turn - see MacroSystem.ApplyCategorySpendingEffects for its confidence/approval profile. For a country with detailed SpendingLines, this is derived from SpendingLineChanges (HHSDiscretionary alone - Medicaid is Mandatory and feeds its own approval term instead, see MacroSystem.MandatorySpendingApprovalMultiplier) rather than set directly by the player - see PolicyDecision.SpendingLineChanges.</summary>
        public float HealthcareSpendingChange;

        /// <summary>Discretionary defense spending change this turn - no growth/confidence side-effect, only a (smaller) approval effect. For a country with detailed SpendingLines, this is derived from SpendingLineChanges (Defense) rather than set directly by the player.</summary>
        public float DefenseSpendingChange;

        /// <summary>Discretionary infrastructure spending change this turn - nudges PotentialGrowthRate over time; see MacroSystem.ApplyCategorySpendingEffects. For a country with detailed SpendingLines, this is derived from SpendingLineChanges (Transportation) rather than set directly by the player.</summary>
        public float InfrastructureSpendingChange;

        /// <summary>Discretionary education spending change this turn - nudges BusinessConfidence over time; see MacroSystem.ApplyCategorySpendingEffects. For a country with detailed SpendingLines, this is derived from SpendingLineChanges (Education) rather than set directly by the player.</summary>
        public float EducationSpendingChange;

        /// <summary>Discretionary justice spending change this turn (Phase 2 - see CLAUDE.md's "Detailed Spending Portfolio Phase 2") - nudges CrimeIndex down over time; see MacroSystem.ApplyCrimeIndex. For a country with detailed SpendingLines, this is derived from SpendingLineChanges (Justice) rather than set directly by the player.</summary>
        public float JusticeSpendingChange;

        /// <summary>Discretionary homeland security spending change this turn (Phase 2) - no growth/confidence/CrimeIndex side-effect, only a (moderate) approval effect, mirroring DefenseSpendingChange's own "approval only" pattern. For a country with detailed SpendingLines, this is derived from SpendingLineChanges (HomelandSecurity) rather than set directly by the player.</summary>
        public float HomelandSecuritySpendingChange;

        /// <summary>Discretionary energy spending change this turn (Phase 2) - nudges BusinessConfidence over time (distinct from Education's own BusinessConfidence nudge - lower/stabler energy costs for businesses); see MacroSystem.ApplyCategorySpendingEffects. For a country with detailed SpendingLines, this is derived from SpendingLineChanges (Energy) rather than set directly by the player.</summary>
        public float EnergySpendingChange;

        /// <summary>Discretionary housing spending change this turn (Phase 2) - nudges PovertyRate down over time (distinct from the player-adjustable WelfareProgramType.HousingAssistance - this represents HUD's baseline federal housing-support spending); see MacroSystem.ApplyPovertyRate. For a country with detailed SpendingLines, this is derived from SpendingLineChanges (Housing) rather than set directly by the player.</summary>
        public float HousingSpendingChange;

        /// <summary>
        /// Change to this country's CurrencyZone interest rate this turn, in percentage points.
        /// If multiple countries share a CurrencyZone (e.g. Germany/France/Italy), their changes
        /// are summed into one shared-zone rate change for the turn.
        ///
        /// ⚠ For the two independent-currency countries (Sweden, Poland) this direct player
        /// control is a DELIBERATE GAMEPLAY CHOICE, ruled and named as such (playtest-2 item 5,
        /// 2026-08-25): the real Riksbank/NBP set their rates independently of government, and the
        /// game trades that realism for monetary agency - stated to the player on the Federal
        /// Reserve tab, the same authored-premise honesty the Eurozone Rate-Voice text carries.
        /// The recorded DESTINATION is independence with appointment influence (the Fed Chair
        /// mechanism generalized - Country.CurrentFedChair non-null is already the entire gate),
        /// behind ONE remaining gate: item 10's political machinery for appointments. The other -
        /// the Taylor-path output-gap distortion fix - was CLEARED by pass 4 (2026-08-26): the rule
        /// reads the unemployment gap, and the advisory reading on the central-bank tab is
        /// Riksbank-B's first visible artefact. See the roadmap's Step 4 block.
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
        /// This turn's requested ABSOLUTE subsidy level per SectorType (0-100, not a delta) - only
        /// meaningful entries are read; see SimulationManager.ApplySectorPolicyChanges and
        /// Sector.SubsidyLevel.
        /// </summary>
        public Dictionary<SectorType, float> SectorSubsidyOverrides = new Dictionary<SectorType, float>();

        /// <summary>
        /// This turn's requested ABSOLUTE regulation level per SectorType (0-100, not a delta) - see
        /// SimulationManager.ApplySectorPolicyChanges and Sector.RegulationLevel.
        /// </summary>
        public Dictionary<SectorType, float> SectorRegulationOverrides = new Dictionary<SectorType, float>();

        /// <summary>Round 3 item 2: this turn's requested ABSOLUTE tax credit level per SectorType (0-100, not a delta) - see SimulationManager.ApplySectorPolicyChanges and Sector.TaxCreditLevel.</summary>
        public Dictionary<SectorType, float> SectorTaxCreditOverrides = new Dictionary<SectorType, float>();

        /// <summary>Round 3 item 2: this turn's requested ABSOLUTE research grants level per SectorType (0-100, not a delta) - see SimulationManager.ApplySectorPolicyChanges and Sector.ResearchGrantsLevel.</summary>
        public Dictionary<SectorType, float> SectorResearchGrantsOverrides = new Dictionary<SectorType, float>();

        /// <summary>Round 3 item 2: this turn's requested ABSOLUTE deregulation/nationalization level per SectorType (0-100, not a delta) - see SimulationManager.ApplySectorPolicyChanges and Sector.DeregulationNationalizationLevel.</summary>
        public Dictionary<SectorType, float> SectorDeregulationNationalizationOverrides = new Dictionary<SectorType, float>();

        /// <summary>
        /// This turn's requested ABSOLUTE sovereign-wealth-fund settings (each -1 = no change
        /// requested this turn, the same sentinel idiom as MinimumWageOverride) - only meaningful if
        /// Country.SovereignWealthFund is non-null. See SimulationManager.ApplySwfPolicyChanges.
        /// </summary>
        /// <remarks>
        /// SwfContributionRateOverride is the one exception to the shared "-1 = no change" idiom
        /// above (Round 3 item 1, the SWF drawdown mechanic): its valid range now extends BELOW zero
        /// (a negative rate = withdrawing from the fund during a recession/emergency, a real policy
        /// lever, not automatic - see SimulationManager.MinSwfContributionRate), so -1 is no longer
        /// distinguishable from a legitimate small withdrawal. Uses float.MinValue instead, a value no
        /// real percentage will ever produce.
        /// </remarks>
        public float SwfContributionRateOverride = float.MinValue;
        public float SwfDomesticAllocationOverride = -1f;
        public float SwfEquitiesWeightOverride = -1f;
        public float SwfBondsWeightOverride = -1f;
        public float SwfInfrastructureWeightOverride = -1f;
        public float SwfRealEstateWeightOverride = -1f;

        /// <summary>
        /// This turn's requested ABSOLUTE deeper-labor-market settings (each -1 = no change
        /// requested this turn, the same sentinel idiom as MinimumWageOverride). See
        /// SimulationManager.ApplyLaborPolicyChanges.
        /// </summary>
        public float PaidFamilyLeaveWeeksOverride = -1f;
        public float OvertimeRegulationOverride = -1f;
        public float RetrainingProgramOverride = -1f;

        /// <summary>
        /// This turn's requested ABSOLUTE deeper-crime-and-justice settings (each -1 = no change
        /// requested this turn, the same sentinel idiom as MinimumWageOverride). See
        /// SimulationManager.ApplyCrimeJusticeDeeperChanges.
        /// </summary>
        public float BailReformOverride = -1f;
        public float DrugPolicyOverride = -1f;

        /// <summary>
        /// Round 3 item 3: this turn's requested ABSOLUTE Judicial Funding/Border Enforcement settings
        /// (each -1 = no change requested this turn, the same sentinel idiom as MinimumWageOverride).
        /// See SimulationManager.ApplyCrimeJusticeDeeperChanges.
        /// </summary>
        public float JudicialFundingOverride = -1f;
        public float BorderEnforcementOverride = -1f;

        /// <summary>
        /// Round 3 item 5, Part B: this turn's requested ABSOLUTE Family Policy/Immigration Policy
        /// settings (each -1 = no change requested this turn, the same sentinel idiom as
        /// BorderEnforcementOverride). See SimulationManager.ApplyDemographicPolicyChanges.
        /// </summary>
        public float FamilyPolicyOverride = -1f;
        public float ImmigrationPolicyOverride = -1f;

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
