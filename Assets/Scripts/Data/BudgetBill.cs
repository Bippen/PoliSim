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

        // §768 (Elias's ruling D4), generalised by §773 below: POLAND'S BUDGET IS THE BUDGET ACT AND ITS STATUTES - the budget act stays veto-proof
        // (Konstytucja Art. 224), and any change to a tax rate travels in a separate tax act, an ordinary statute the President may veto (taxes are set by
        // statute, Art. 217). These split a bill's rates so; §773's parts split the rest.

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

        // §773 (Elias's ruling E2): "General rule for Poland: only spending stays in the budget act." What a Polish statute sets travels in its own act,
        // voted and put to the President like the tax act: the rates (D4), the pension age ("set by ordinary statute, so it travels in its own act and
        // can be vetoed, like tax rates"), and - RULED (Elias's ruling F6: "In Poland benefit levels and the fund's rules are set by statute, so they are
        // statute parts. 'Spending' means the budget act's appropriations.") - the benefit levels and the sovereign fund's rules: the parts that leave the
        // budget act are whatever `BudgetBill.StatuteParts` lists. The budget act keeps the spending lines alone. ⚠ The fund's act is UNCONTESTED BY
        // CONSTRUCTION: the chamber's concern weighs none of the fund's terms (`ParliamentSystem.GetBudgetBillConcern`), so it always passes and is
        // signed - what the fund's rules come to is the budget act's outcome alone. Every act is a division of its own, and the fund's act, always
        // passed, is voted last among the acts (`BudgetBill.StatuteParts`); since Elias's ruling F5 ("the check reads every bill lost that day") the
        // cabinet's pressure test reads every bill lost on the day (`Country.BillLostOn`), so an act passed after a fallen one hides nothing.

        /// <summary>§773: the parts of a bill that in Poland travel each in its own act, in the order the acts are voted.</summary>
        public enum StatutePart { Rates, PensionAge, Benefits, Fund }

        /// <summary>§773: every statute part, in the order the acts are voted.</summary>
        public static readonly StatutePart[] StatuteParts = { StatutePart.Rates, StatutePart.PensionAge, StatutePart.Benefits, StatutePart.Fund };

        /// <summary>§773: whether the bill changes what <paramref name="part"/> sets on <paramref name="country"/> - a part at the figure in force is no change.</summary>
        public bool Changes(StatutePart part, Country country)
        {
            switch (part)
            {
                case StatutePart.Rates: return ChangesTaxRates(country);
                case StatutePart.PensionAge: return ChangesPensionAge(country);
                case StatutePart.Benefits: return ChangesBenefits(country);
                default: return ChangesFund(country);
            }
        }

        /// <summary>§773: whether the bill sets a pension age other than the one in force (a negative figure returns the statute's own for the year).</summary>
        public bool ChangesPensionAge(Country country)
        {
            if (!PensionAgeSet || !PensionAgeStatute.Has(country.Id)) { return false; }
            float inForce = PensionAgeStatute.AgeInForce(country, country.CalendarYear);
            float set = PensionAge < 0f ? PensionAgeStatute.AgeInForce(country.Id, country.CalendarYear) : UnityEngine.Mathf.Clamp(PensionAge, PensionAgeMin, PensionAgeMax);
            return System.Math.Abs(set - inForce) > 1e-6f || (PensionAge < 0f) != (country.PensionAgeOverride < 0f);
        }

        /// <summary>§773: whether the bill sets an implemented program's generosity other than its own (the bill names every implemented program).</summary>
        public bool ChangesBenefits(Country country)
        {
            foreach (KeyValuePair<WelfareProgramType, float> kv in WelfarePrograms)
            {
                WelfareProgram standing = country.WelfarePrograms.Find(w => w.Type == kv.Key);
                if (standing != null && standing.IsImplemented && System.Math.Abs(UnityEngine.Mathf.Clamp(kv.Value, 0f, 100f) - standing.GenerosityLevel) > 1e-4f) { return true; }
            }
            return false;
        }

        /// <summary>§773: whether the bill creates or dissolves the sovereign fund, or sets any of its rules other than its own (the bill names them all).</summary>
        public bool ChangesFund(Country country)
        {
            SovereignWealthFund fund = country.SovereignWealthFund;
            if (SwfShouldExist != (fund != null)) { return true; }
            if (fund == null) { return false; }
            bool Moved(float set, float standing) => System.Math.Abs(set - standing) > 1e-4f;
            return Moved(SwfContributionRatePercent, fund.ContributionRatePercent) || Moved(SwfDomesticAllocationPercent, fund.DomesticAllocationPercent)
                || Moved(SwfEquitiesWeight, fund.EquitiesWeight) || Moved(SwfBondsWeight, fund.BondsWeight)
                || Moved(SwfInfrastructureWeight, fund.InfrastructureWeight) || Moved(SwfRealEstateWeight, fund.RealEstateWeight);
        }

        /// <summary>§773: the act <paramref name="part"/> travels in - that part of this bill alone, tabled by whoever tabled the bill, so the chamber votes
        /// on it and reads the same author. ⚠ For the vote's concern and title only, never applied: a bill built new carries no fund
        /// (<see cref="SwfShouldExist"/> false), and applied it would dissolve one.</summary>
        public BudgetBill PartOf(StatutePart part)
        {
            switch (part)
            {
                case StatutePart.Rates: return TaxActPart();
                case StatutePart.PensionAge: return new BudgetBill { GovernmentBill = GovernmentBill, TabledBy = TabledBy, PensionAgeSet = PensionAgeSet, PensionAge = PensionAge };
                case StatutePart.Benefits: return new BudgetBill { GovernmentBill = GovernmentBill, TabledBy = TabledBy, WelfarePrograms = new Dictionary<WelfareProgramType, float>(WelfarePrograms) };
                default:
                    return new BudgetBill
                    {
                        GovernmentBill = GovernmentBill, TabledBy = TabledBy, SwfShouldExist = SwfShouldExist, SwfContributionRatePercent = SwfContributionRatePercent,
                        SwfDomesticAllocationPercent = SwfDomesticAllocationPercent, SwfEquitiesWeight = SwfEquitiesWeight, SwfBondsWeight = SwfBondsWeight,
                        SwfInfrastructureWeight = SwfInfrastructureWeight, SwfRealEstateWeight = SwfRealEstateWeight,
                    };
            }
        }

        /// <summary>§773: this bill with <paramref name="part"/> withheld - what applies where that act falls or its veto stands: what the part sets stays as
        /// it is on <paramref name="country"/> and the rest of the bill applies. The fund's rules are written back as they stand, since the bill always
        /// names them.</summary>
        public BudgetBill Without(StatutePart part, Country country)
        {
            if (part == StatutePart.Rates) { return WithoutRateChanges(); }
            var act = (BudgetBill)MemberwiseClone();
            switch (part)
            {
                case StatutePart.PensionAge:
                    act.PensionAgeSet = false;
                    act.PensionAge = -1f;
                    break;
                case StatutePart.Benefits:
                    act.WelfarePrograms = new Dictionary<WelfareProgramType, float>();
                    break;
                default:
                    SovereignWealthFund fund = country.SovereignWealthFund;
                    act.SwfShouldExist = fund != null;
                    if (fund != null)
                    {
                        act.SwfContributionRatePercent = fund.ContributionRatePercent;
                        act.SwfDomesticAllocationPercent = fund.DomesticAllocationPercent;
                        act.SwfEquitiesWeight = fund.EquitiesWeight;
                        act.SwfBondsWeight = fund.BondsWeight;
                        act.SwfInfrastructureWeight = fund.InfrastructureWeight;
                        act.SwfRealEstateWeight = fund.RealEstateWeight;
                    }
                    break;
            }
            return act;
        }

        /// <summary>§773: the budget act - this bill with every statute part withheld: the spending lines alone (and the Finance partner's bookkeeping).</summary>
        public BudgetBill SpendingOnly(Country country)
        {
            BudgetBill act = this;
            foreach (StatutePart part in StatuteParts) { act = act.Without(part, country); }
            return act;
        }
    }
}
