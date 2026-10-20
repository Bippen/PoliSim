using System.Collections.Generic;
using PoliSim.Data;
using UnityEngine;

namespace PoliSim.Simulation
{
    /// <summary>
    /// PS-6 US-23 (`docs/specs/USA_STAGE_PLAN.md` US-23, "the president's pen"; R-US7 (a) and R-US9 as ruled, F8): THE OUT-PARTY MAJORITY'S RECONCILIATION ON THE
    /// PRESIDENT'S DESK. Where both chambers' majorities belong to the party out of the White House (and it is not the player's), that majority tables its own
    /// reconciliation once a fiscal year before 1 October (`UsMajorityReconciliation.Due`) - its content the AI finance ministry's book for that party
    /// (`UsMajorityReconciliation.Book`) - in a slot of its own beside the president's budget. After a bill's course (`ParliamentSystem.BillDurationDays`) it is
    /// voted as the budget's reconciliation is (`UsBudgetProcedure.ConcernOf`: by a majority in both chambers, the House first - a revenue bill), its author the
    /// majority, then presented (`UsVetoGate`): an AI president answers by R-US17's rule (a); a player-president's desk holds it (the Docket, US-21b) until he
    /// signs or vetoes it (`SignDocketBill`, `VetoDocketBill`, acted on at once - US-21c) or the ten days run out; a veto goes to the override at two thirds
    /// on the passage's sides. Law, it applies once (`ParliamentSystem.ApplyBillResult`; the government bears it only where its author is in the government,
    /// `Congress.GovernmentBears`, so a congressional majority's rates charge the president nothing); fallen, the standing rates hold. The funding of the
    /// government is not its business: the appropriation, the continuing resolution and a lapse are the president's budget's (US-22).
    /// </summary>
    public partial class SimulationManager
    {
        /// <summary>PS-6 US-23: the out-party majority's reconciliation, one slot per country - before Congress (its course in `BudgetBill.DaysRemaining`) or held on
        /// the player-president's docket (`BudgetBill.Docket`). Saved (`SimulationPendingState.PendingMajorityReconciliations`, additive).</summary>
        private readonly Dictionary<CountryId, BudgetBill> _pendingMajorityReconciliationByCountry = new Dictionary<CountryId, BudgetBill>();

        /// <summary>PS-6 US-23: whether the player answers the docket today - the surface's gate (the signing plate's answers, the Docket page's desk): Poland's
        /// office as SP-7b holds it, or the US presidency of the day where it is the player's (`Congress.PresidentOn`).</summary>
        public bool PlayerAnswersDocket(CountryId countryId)
        {
            Country country = _world?.GetCountry(countryId);
            if (country == null) { return false; }
            if (Elections.PresidentialVeto.Applies(countryId)) { return PlayerPresidentDockets(country); }
            return Elections.UsVeto.Applies(countryId) && Congress.PresidentOn(country, CurrentDate, out _, out _, out bool playerPresident) && playerPresident;
        }

        /// <summary>PS-6 US-23: the out-party majority's reconciliation before Congress or on the president's desk, or null.</summary>
        public BudgetBill GetPendingMajorityReconciliation(CountryId countryId)
            => _pendingMajorityReconciliationByCountry.TryGetValue(countryId, out BudgetBill bill) ? bill : null;

        /// <summary>PS-6 US-23: the day's step for the majority's reconciliation - the pending one counted down, voted and presented, or decided on the docket; else
        /// one tabled where it is due. On the docket answers' run (US-21c) only an answered bill moves, and nothing is tabled.</summary>
        private void AdvanceMajorityReconciliationDay(CountryId countryId)
        {
            Country country = _world?.GetCountry(countryId);
            if (country == null) { return; }
            if (_pendingMajorityReconciliationByCountry.TryGetValue(countryId, out BudgetBill bill)) { ResolveMajorityReconciliationDay(country, bill); return; }
            if (_docketAnswersOnly || !UsBudgetProcedure.Applies(country)) { return; }
            TableMajorityReconciliation(country);
        }

        /// <summary>PS-6 US-23: the majority's reconciliation tabled where it is due today - the year marked as it is tabled; nothing where the party's book moves
        /// nothing (its rates already at its position), and the next day asks again.</summary>
        private void TableMajorityReconciliation(Country country)
        {
            if (!UsMajorityReconciliation.Due(country, CurrentDate, out string majority)) { return; }
            BudgetBill bill = UsMajorityReconciliation.Book(country, majority, out _);
            if (bill == null) { return; }
            bill.DaysRemaining = ParliamentSystem.BillDurationDays;
            _pendingMajorityReconciliationByCountry[country.Id] = bill;
            country.MajorityReconciliationYear = UsMajorityReconciliation.YearOf(CurrentDate);
            Debug.Log($"BUDGET: {country.Id} - the {majority} majority tables its reconciliation ({DescribeBudgetBill(bill)}); Congress votes it in {bill.DaysRemaining} day(s), by a majority in both chambers, then it goes to the President");
        }

        /// <summary>PS-6 US-23: the majority's reconciliation's day - on its vote day the book is BUILT AGAIN against the law of that day (the review's M1: a rate
        /// law passed since the tabling is the base; where the book now moves nothing, the bill is withdrawn unvoted) and both chambers vote it, and it is
        /// presented; held on the docket, the president's answer (or the period's end) decides it with no fresh vote. Decided, the slot empties: law applies its
        /// RATES ALONE, each by its vote-day move from the law of the day it applies (`UsMajorityReconciliation.Applied` - no spending, benefit or fund term,
        /// so nothing passed while it waited is undone); fallen, nothing moves.</summary>
        private void ResolveMajorityReconciliationDay(Country country, BudgetBill bill)
        {
            if (_docketAnswersOnly && (bill.Docket == null || bill.Docket.Answer == 0)) { return; }   // PS-6 US-21c (J6 (i)): an answer acted on at once - only an answered bill moves
            if (bill.Docket == null)
            {
                bill.DaysRemaining--;
                if (bill.DaysRemaining > 0) { return; }
                BudgetBill fresh = UsMajorityReconciliation.Book(country, bill.TabledBy, out string why);   // the review's M1: the book on the law of the vote day
                if (fresh == null)
                {
                    _pendingMajorityReconciliationByCountry.Remove(country.Id);
                    Debug.Log($"BUDGET: {country.Id} - the {bill.TabledBy} majority's reconciliation is withdrawn unvoted: {why}");
                    return;
                }
                fresh.DaysRemaining = 0;
                _pendingMajorityReconciliationByCountry[country.Id] = fresh;
                bill = fresh;
            }
            BillConcern concern = UsMajorityReconciliation.ConcernOf(country, bill);
            DivisionRecord passage = null;
            bool passed = true;
            bool presentedToday = bill.Docket == null;
            if (presentedToday)
            {
                string title = UsMajorityReconciliation.TitleOf(country, bill.TabledBy,
                    UsActTitle(country, UsBudgetProcedure.Act.Reconciliation, UsBudgetProcedure.PartOf(bill, UsBudgetProcedure.Act.Reconciliation, country)));
                passage = Congress.Divide(country, title, concern, CurrentDate, out passed, BillAxis.Fiscal, Congress.OriginOf(country, concern, CurrentDate));
            }
            bool? decided = UsVetoGate(country, passage, concern, passed, ref bill.Docket);   // PS-6 US-21: presented to the president of the day; a player-president's desk holds it
            if (decided == null)
            {
                if (presentedToday) { Debug.Log($"BUDGET: {country.Id} - the {bill.TabledBy} majority's reconciliation is on the President's desk ({bill.Docket?.Passage?.Title})"); }   // the review's L6: said once, the day it arrives
                return;
            }
            _pendingMajorityReconciliationByCountry.Remove(country.Id);
            BudgetBill applied = UsMajorityReconciliation.Applied(country, bill);   // the review's M1: the rates alone, from the law of today
            float approvalBefore = country.State.ApprovalRating;
            ParliamentSystem.ApplyBillResult(country, applied, decided.Value, (c, b) => { }, CurrentDate.Year, Congress.GovernmentBears(country, concern));   // no spending, benefit or fund term rides it; the government bears it only where its author is in the government
            ApprovalLedgerRecorder.RecordEvent(country, CurrentDate, decided.Value ? "Majority reconciliation enacted" : "Majority reconciliation failed", country.State.ApprovalRating - approvalBefore);
            Debug.Log($"BUDGET: {country.Id} - the {bill.TabledBy} majority's reconciliation {(decided.Value ? "becomes law - it applies (" + DescribeBudgetBill(applied) + ")" : "falls - the standing rates hold")}");
        }
    }
}
