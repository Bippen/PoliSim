using System.Collections.Generic;

namespace PoliSim.Data
{
    /// <summary>
    /// Master Sequence step 5d (Political Systems Overhaul Part B, full rollout - standalone
    /// non-budget policy tier): the Labor Market tab's dials bundled into one bill, introducible
    /// anytime, resolving via ParliamentSystem.GetLaborBillDirection's seat-weighted alignment
    /// scoring exactly like BudgetBill's own tax/spending/welfare terms - see that method's own doc
    /// comment for the stated sign convention (every dial here reads as "more support/intervention =
    /// positive" on the same FiscalStance axis, so no dial needs a sign flip). One bill per tab, not
    /// per dial - mirrors BudgetBill bundling Tax+Spending+Welfare+SWF together in 5c.
    /// </summary>
    public class LaborPolicyBill
    {
        public float MinimumWage;
        public float PaidFamilyLeaveWeeks;
        public float OvertimeRegulation;
        public float RetrainingProgram;
        public float FamilyPolicy;
        public float ImmigrationPolicy;
        public int DaysRemaining;
    }

    /// <summary>Crime &amp; Justice tab's dials bundled into one bill - see LaborPolicyBill's own doc comment for the pattern. See ParliamentSystem.GetCrimeJusticeBillDirection for this bill's own sign convention (harsher/stricter dials read negative, funding/reform dials read positive).</summary>
    public class CrimeJusticePolicyBill
    {
        public float PoliceFunding;
        public float SentencingSeverity;
        public float BailReform;
        public float DrugPolicy;
        public float JudicialFunding;
        public float BorderEnforcement;
        public int DaysRemaining;
    }

    /// <summary>
    /// Economic Sectors tab's five dials bundled ACROSS EVERY SECTOR into one bill - see
    /// LaborPolicyBill's own doc comment for the pattern. Every dictionary covers all of the
    /// country's Sectors at once (every country has all Sectors always - no implement/remove, unlike
    /// TaxLines/WelfareProgram), the same "one bill per tab" choice, just with SectorType as an extra
    /// key. See ParliamentSystem.GetSectorBillDirection for this bill's own sign convention.
    /// </summary>
    public class SectorPolicyBill
    {
        public Dictionary<SectorType, float> SubsidyLevels = new Dictionary<SectorType, float>();
        public Dictionary<SectorType, float> RegulationLevels = new Dictionary<SectorType, float>();
        public Dictionary<SectorType, float> TaxCreditLevels = new Dictionary<SectorType, float>();
        public Dictionary<SectorType, float> ResearchGrantsLevels = new Dictionary<SectorType, float>();
        public Dictionary<SectorType, float> DeregulationLevels = new Dictionary<SectorType, float>();
        public int DaysRemaining;
    }

    /// <summary>
    /// Trade tab's tariff policy bundled into one bill - see LaborPolicyBill's own doc comment for the
    /// pattern. NewBaseTariffRate is an ABSOLUTE target (like TaxLine.Rate), not the delta
    /// PolicyDecision.TariffRateChange itself uses - SimulationManager converts between the two at
    /// apply time (see SimulationManager.ApplyTradeBillEffects). PartnerTariffOverrides is
    /// deliberately EXCLUDED from the vote direction (see ParliamentSystem.GetTradeBillDirection) -
    /// the same stated simplification BudgetBill already applies to SWF's asset-mix terms - but still
    /// applies in full on PASS.
    /// </summary>
    public class TradePolicyBill
    {
        public float NewBaseTariffRate;
        public Dictionary<CountryId, float> PartnerTariffOverrides = new Dictionary<CountryId, float>();
        public int DaysRemaining;
    }
}
