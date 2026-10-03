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
        /// <summary>PS-3e (§632): TRUE for a budget the AI GOVERNMENT tabled for the player's country - the finance ministry's rule as a bill the chamber votes on; false for the player's own.</summary>
        public bool GovernmentBill;
        /// <summary>PS-3e (§632): the party that tabled this budget as an ALTERNATIVE to the government's (the Riksdag's budget motion - a shadow budget); null for a government's own.</summary>
        public string TabledBy;
        /// <summary>§755 (Elias's ruling A2; the review's defect 1): the Finance partner's stance step this government bill carries - its holder (null for none),
        /// the points of GDP it moves, the count before it and the government that tabled it - counted only if the chamber adopts the bill (FinancePartner.CreditAdopted).</summary>
        public string FinanceStanceHolder;
        public float FinanceStancePoints;
        /// <summary>§768: the part of <see cref="FinanceStancePoints"/> the household rates carry - withheld from the count where Poland's tax act fell.</summary>
        public float FinanceStanceRatePoints;
        public float FinanceStanceAppliedBefore;
        public System.DateTime FinanceStanceGovernment;
        /// <summary>Requested absolute Rate per TaxType - only meaningful for a TaxType the country currently has implemented (see this class's own doc comment).</summary>
        public Dictionary<TaxType, float> TaxLines = new Dictionary<TaxType, float>();
        /// <summary>F4-4 (2026-09-13): the requested rate per sub-row of a statute's schedule (`TaxLine.BracketRates`' shape) - −1 where the statute's figure stands; only the income tax carries one.</summary>
        public Dictionary<TaxType, float[]> BracketRates = new Dictionary<TaxType, float[]>();
        public Dictionary<SpendingCategory, float> SpendingPercentChanges = new Dictionary<SpendingCategory, float>();
        /// <summary>P5-B2: nominal amounts the bill sets outright, and pins it toggles - see PolicyDecision's fields of the same names.</summary>
        public Dictionary<SpendingCategory, float> SpendingNominalTargets = new Dictionary<SpendingCategory, float>();
        public Dictionary<SpendingCategory, bool> SpendingPinChanges = new Dictionary<SpendingCategory, bool>();

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

        /// <summary>PN-1's dial (§590, DS-3): whether the bill sets the pension age, and the age it sets in years - a NEGATIVE figure returns the statute's own.
        /// Unset (every bill the dial did not touch), the age stands as it is.</summary>
        public bool PensionAgeSet;
        public float PensionAge = -1f;

        /// <summary>CONVENTION: the dial's track, the row's own 60–70 (board 15c) - a bill's figure is clamped to it.</summary>
        public const float PensionAgeMin = 60f, PensionAgeMax = 70f;

        public int DaysRemaining;

        // §768 (Elias's ruling D4): POLAND'S BUDGET IS TWO ACTS - the budget act stays veto-proof (Konstytucja Art. 224), and any change to a tax rate
        // travels in a separate tax act, an ordinary statute the President may veto (taxes are set by statute, Art. 217). These three split a bill so.

        /// <summary>§768: whether the bill changes a tax rate in force on <paramref name="country"/> - a levied line's rate, or a sub-row's rate a schedule
        /// sets (−1 keeps the standing figure). A bill names every levied line at its requested rate, so a line at its current rate is no change.</summary>
        public bool ChangesTaxRates(Country country)
        {
            foreach (KeyValuePair<TaxType, float> kv in TaxLines)
            {
                TaxLine standing = country.TaxLines.Find(t => t.Type == kv.Key);
                if (standing != null && standing.IsImplemented && System.Math.Abs(kv.Value - standing.Rate) > 1e-6f) { return true; }
            }
            foreach (KeyValuePair<TaxType, float[]> kv in BracketRates)
            {
                TaxLine standing = country.TaxLines.Find(t => t.Type == kv.Key);
                if (standing == null || kv.Value == null) { continue; }
                for (int i = 0; i < kv.Value.Length; i++)
                {
                    if (kv.Value[i] < 0f) { continue; }
                    if (standing.BracketRates == null || i >= standing.BracketRates.Length || System.Math.Abs(kv.Value[i] - standing.BracketRates[i]) > 1e-6f) { return true; }
                }
            }
            return false;
        }

        /// <summary>§768: the tax act - this bill's rates alone (its lines and its schedule's sub-rows), tabled by whoever tabled the bill, so the chamber
        /// votes on the rates and reads the same author.</summary>
        public BudgetBill TaxActPart() => new BudgetBill
        {
            GovernmentBill = GovernmentBill, TabledBy = TabledBy,
            TaxLines = new Dictionary<TaxType, float>(TaxLines), BracketRates = new Dictionary<TaxType, float[]>(BracketRates),
        };

        /// <summary>§768: the budget act with the rates withheld - what applies where the tax act fails or its veto stands: the old rates stand and the
        /// budget runs on them. Every other part is this bill's own.</summary>
        public BudgetBill WithoutRateChanges()
        {
            var act = (BudgetBill)MemberwiseClone();
            act.TaxLines = new Dictionary<TaxType, float>();
            act.BracketRates = new Dictionary<TaxType, float[]>();
            return act;
        }
    }
}
