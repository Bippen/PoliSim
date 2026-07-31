using System.Collections.Generic;

namespace PoliSim.Data
{
    /// <summary>
    /// Master Sequence step 5c (Political Systems Overhaul Part B, full rollout - Annual Budget tier):
    /// the omnibus bill bundling every EXISTING (already-implemented) program's rate/amount change -
    /// Tax, Spending, Welfare, and Sovereign Wealth Fund - at the moment the player introduces it,
    /// superseding the Master Sequence step 4 pilot's Tax-only TaxBill (retired - see git history).
    /// Infrastructure has no direct lever of its own (ConditionIndex is driven entirely by the Spending
    /// category's own "Infrastructure" line - see GameController.DrawInfrastructureContent), so it's
    /// covered here only via that Spending line, not a separate field.
    ///
    /// Master Sequence step 5d: implementing or removing a tax/welfare program entirely moved OUT of
    /// this bill and into its own standalone, anytime-introducible bill (see ProgramBill.cs) - a
    /// deliberate scope narrowing from 5c's original shape, where implement/remove briefly lived here
    /// too. TaxLines/WelfarePrograms below now carry ONLY a rate/generosity value, meaningful solely
    /// for a TaxType/WelfareProgramType the country ALREADY has implemented at introduce time - a
    /// program with no entry, or one a concurrent ProgramBill un-implements before this bill resolves,
    /// is simply skipped (see ParliamentSystem.GetBillDirection/ApplyBillResult), the same "no-op for
    /// an inapplicable entry" idiom every Apply*Changes method in SimulationManager already uses.
    ///
    /// Takes ParliamentSystem.BillDurationDays real in-game days to resolve (introduction -&gt; a fixed
    /// wait, standing in for the roadmap's "committee/debate" stage without modeling committee mechanics
    /// separately - the SAME simplification the step 4 pilot already used), counted down once per
    /// simulated day (SimulationManager.AdvanceBudgetBillDay), independent of the 121-day turn boundary
    /// and NOT itself a mandatory pause - only one bill may be pending per country at a time
    /// (SimulationManager.IntroduceBudgetBill). The mandatory pause (Master Sequence step 5a) blocks
    /// time only until a bill is introduced on the country's own fiscal-year date; once introduced,
    /// time resumes and this bill resolves quietly in the background exactly like the retired TaxBill
    /// did, never pausing again.
    /// </summary>
    public class BudgetBill
    {
        /// <summary>Requested absolute Rate per TaxType - only meaningful for a TaxType the country currently has implemented (see this class's own doc comment).</summary>
        public Dictionary<TaxType, float> TaxLines = new Dictionary<TaxType, float>();
        public Dictionary<SpendingCategory, float> SpendingPercentChanges = new Dictionary<SpendingCategory, float>();

        /// <summary>Requested absolute GenerosityLevel per WelfareProgramType - only meaningful for a WelfareProgramType the country currently has implemented (see this class's own doc comment).</summary>
        public Dictionary<WelfareProgramType, float> WelfarePrograms = new Dictionary<WelfareProgramType, float>();

        /// <summary>Whether the country should have a Sovereign Wealth Fund once this bill resolves - the SWF equivalent of a TaxLine's/WelfareProgram's own IsImplemented flag.</summary>
        public bool SwfShouldExist;
        public float SwfContributionRatePercent;
        public float SwfDomesticAllocationPercent;
        public float SwfEquitiesWeight;
        public float SwfBondsWeight;
        public float SwfInfrastructureWeight;
        public float SwfRealEstateWeight;

        public int DaysRemaining;
    }
}
