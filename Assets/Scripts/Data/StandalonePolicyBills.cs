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
    /// apply time (see SimulationManager.ApplyTradeBillEffects). PartnerTariffOverrides used to be
    /// EXCLUDED from the vote direction by analogy to SWF's asset-mix terms; since pass 6
    /// (2026-08-27) the direction is the change in the import-weighted average tariff the bill would
    /// charge, overrides included (see ParliamentSystem.GetTradeBillDirection).
    /// </summary>
    public class TradePolicyBill
    {
        public float NewBaseTariffRate;
        public Dictionary<CountryId, float> PartnerTariffOverrides = new Dictionary<CountryId, float>();
        public int DaysRemaining;
    }

    /// <summary>
    /// The SWF emergency drawdown - a FIFTH standalone tier-3 bill, added 2026-08-02 on Elias's A2
    /// ruling. See LaborPolicyBill's own doc comment for the pattern this reuses wholesale.
    ///
    /// **The gap it closes was a gameplay bug wearing the costume of a design question.** Since step 5c
    /// every SWF change - the contribution rate and the asset mix - rides the annual omnibus BudgetBill,
    /// which is right for ordinary policy and wrong for an emergency: a recession calling for the fund
    /// could be stuck behind a fiscal-year vote up to a year away. A sovereign wealth fund that cannot be
    /// reached during the emergency it exists for is not modelling the instrument.
    ///
    /// **Deliberately NOT bundled into BudgetBill and NOT exempt from a vote**, both halves of Elias's
    /// ruling. Bundling would reintroduce the delay this exists to remove; exempting it - the
    /// Fed/Eurozone carve-out shape - would make the fund a reserve the executive can raid without
    /// Parliament, which is neither how these funds work nor a power this game grants anywhere else.
    /// Standalone and votable is the narrow change that fixes the timing without inventing a new power.
    ///
    /// **One field, on purpose.** A drawdown is an amount, not a policy mix; the asset allocation it
    /// liquidates against stays on the annual bill where it belongs. Keeping it to a single number is
    /// what makes the emergency path fast to introduce, which is the entire point.
    /// </summary>
    public class SwfDrawdownBill
    {
        /// <summary>
        /// How much to withdraw, as a percentage of GDP, applied once on passage. **Positive means a
        /// withdrawal** - the opposite sign to <see cref="SovereignWealthFund.ContributionRatePercent"/>,
        /// which is positive when paying IN. Named for what it does rather than inheriting the
        /// contribution's convention, because "a negative contribution" is precisely the phrasing that
        /// gets a sign wrong at a call site.
        /// </summary>
        public float WithdrawalPercentOfGdp;

        public int DaysRemaining;
    }
}
