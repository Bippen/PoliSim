using System.Collections.Generic;

namespace PoliSim.Data
{
    /// <summary>One TaxType's requested standing values, as captured at the moment a BudgetBill is introduced.</summary>
    public struct TaxBillLine
    {
        public bool IsImplemented;
        public float Rate;

        public TaxBillLine(bool isImplemented, float rate)
        {
            IsImplemented = isImplemented;
            Rate = rate;
        }
    }

    /// <summary>One WelfareProgramType's requested standing values, as captured at the moment a BudgetBill is introduced.</summary>
    public struct WelfareBillLine
    {
        public bool IsImplemented;
        public float GenerosityLevel;

        public WelfareBillLine(bool isImplemented, float generosityLevel)
        {
            IsImplemented = isImplemented;
            GenerosityLevel = generosityLevel;
        }
    }

    /// <summary>
    /// Master Sequence step 5c (Political Systems Overhaul Part B, full rollout - Annual Budget tier):
    /// the omnibus bill bundling every EXISTING program's rate/amount change - Tax, Spending, Welfare,
    /// and Sovereign Wealth Fund - at the moment the player introduces it, superseding the Master
    /// Sequence step 4 pilot's Tax-only TaxBill (retired - see git history). Infrastructure has no
    /// direct lever of its own (ConditionIndex is driven entirely by the Spending category's own
    /// "Infrastructure" line - see GameController.DrawInfrastructureContent), so it's covered here only
    /// via that Spending line, not a separate field.
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
        public Dictionary<TaxType, TaxBillLine> TaxLines = new Dictionary<TaxType, TaxBillLine>();
        public Dictionary<SpendingCategory, float> SpendingPercentChanges = new Dictionary<SpendingCategory, float>();
        public Dictionary<WelfareProgramType, WelfareBillLine> WelfarePrograms = new Dictionary<WelfareProgramType, WelfareBillLine>();

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
