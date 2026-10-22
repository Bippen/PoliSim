using System.Collections.Generic;
using PoliSim.Data;
using UnityEngine;

namespace PoliSim.Simulation
{
    /// <summary>
    /// PS-6 US-24 (`docs/specs/USA_STAGE_PLAN.md` US-24, "after that span: Congress's raise or suspension, as a bill through both chambers (reconciliation may
    /// carry it, 641(a)(3)) and the veto - or the game's cut of spending to revenue"; R-US21 (iv) and (vi) as ruled): THE RAISE BEFORE CONGRESS AND THE CUT'S
    /// ARMING, in the player's day tick - so only a world where the game's Congress votes (`UsBudgetProcedure.Applies`) reaches them; the limit's bookkeeping is
    /// every world's (`UsDebtLimit.Track`, in <see cref="AdvanceDay"/>), and where no Congress of the game votes the AI-governed USA raises by act there.
    /// <list type="bullet">
    /// <item><b>Who tables it</b> (DECLARED, the continuing resolution's reading): the government of the day - the AI's where it governs, the player's where the
    /// player's party holds the presidency - a bill's course (`ParliamentSystem.BillDurationDays`) before the measures run out, so it is voted on that day
    /// (`UsDebtLimit.RaiseDue`); fallen, it is tabled again the next day while the limit binds.</item>
    /// <item><b>How it is voted</b> (DECLARED): by reconciliation - a majority in both chambers, the House first (2 U.S.C. 641(a)(3) lets reconciliation change the
    /// limit; the record's last raise, P.L. 119-21, was such an act) - on the budget's own concern builder, which reads a bill moving no rate, line, benefit or
    /// fund as asking the blocs nothing, so it passes uncontested (a party's withholding it, as the continuing resolution's (P), is asked); then presented
    /// (`UsVetoGate`): an AI president answers by his rule, a player-president's desk holds it on the Docket.</item>
    /// <item><b>What it does</b>: law, the limit rises by `UsDebtLimit.RaiseUsd` (`UsDebtLimit.Raise`) and the episode closes; fallen, its failed vote is charged
    /// as a continuing resolution's is (`Congress.GovernmentBears`).</item>
    /// <item><b>The cut</b>: each day tick arms the next day's flows while the limit binds (`Country.DebtLimitCutFlowDay`, read by `UsDebtLimit.CutToday`) - the
    /// lapse's own timing (`UsLapse`), since the economy's day runs before the tick.</item>
    /// </list>
    /// </summary>
    public partial class SimulationManager
    {
        /// <summary>PS-6 US-24: the debt-limit raise before Congress or on the player-president's desk (its `BudgetBill.Docket`), one slot per country. Saved
        /// (`SimulationPendingState.PendingDebtLimitBills`, format 53).</summary>
        private readonly Dictionary<CountryId, BudgetBill> _pendingDebtLimitBillByCountry = new Dictionary<CountryId, BudgetBill>();

        /// <summary>PS-6 US-24: the debt-limit raise before Congress or on the President's desk, or null.</summary>
        public BudgetBill GetPendingDebtLimitBill(CountryId countryId)
            => _pendingDebtLimitBillByCountry.TryGetValue(countryId, out BudgetBill bill) ? bill : null;

        /// <summary>PS-6 US-24: the day's step for the debt limit where the game's Congress votes - the pending raise counted down, voted and presented, or decided on
        /// the docket; else one tabled where it is due; then the next day's flows armed for the cut while the limit binds. On the docket answers' run (US-21c) only
        /// an answered bill moves, nothing is tabled and nothing is armed.</summary>
        private void AdvanceDebtLimitDay(CountryId countryId)
        {
            Country country = _world?.GetCountry(countryId);
            if (!UsDebtLimit.Applies(country) || !UsBudgetProcedure.Applies(country) || !UsDebtLimit.IsRead(country)) { return; }
            if (_pendingDebtLimitBillByCountry.TryGetValue(countryId, out BudgetBill bill)) { ResolveDebtLimitBillDay(country, bill); }
            else if (!_docketAnswersOnly && UsDebtLimit.RaiseDue(country, CurrentDate)) { TableDebtLimitRaise(country); }
            if (_docketAnswersOnly) { return; }
            System.DateTime flows = UsDebtLimit.Binding(country, CurrentDate) ? CurrentDate.Date.AddDays(1) : System.DateTime.MinValue;
            if (country.DebtLimitCutFlowDay != flows)
            {
                if (flows != System.DateTime.MinValue && country.DebtLimitCutFlowDay == System.DateTime.MinValue) { Debug.Log($"LIMIT: {country.Id} - the measures ran out on {UsDebtLimit.BindsOn(country):yyyy-MM-dd} with no raise law: from {flows:yyyy-MM-dd} the day's non-interest outlays are cut to what revenue leaves after interest, until a raise becomes law"); }
                country.DebtLimitCutFlowDay = flows;
            }
        }

        /// <summary>PS-6 US-24: the raise tabled by the government of the day, voted after a bill's course.</summary>
        private void TableDebtLimitRaise(Country country)
        {
            var bill = new BudgetBill { GovernmentBill = !PlayerGoverns(country), DaysRemaining = ParliamentSystem.BillDurationDays };
            _pendingDebtLimitBillByCountry[country.Id] = bill;
            Debug.Log($"LIMIT: {country.Id} - {(bill.GovernmentBill ? "the government" : country.PlayerPartyAbbrev)} tables the raise ({UsDebtLimit.BillTitle()}); Congress votes it in {bill.DaysRemaining} day(s), by a majority in both chambers, then it goes to the President");
        }

        /// <summary>PS-6 US-24: the raise's day - on its vote day both chambers vote it by reconciliation and it is presented; held on the docket, the President's
        /// answer (or the period's end) decides it with no fresh vote. Decided, the slot empties: law raises the limit; fallen, its failed vote is charged where
        /// the government bears it, and the next day tables another while the limit binds.</summary>
        private void ResolveDebtLimitBillDay(Country country, BudgetBill bill)
        {
            if (_docketAnswersOnly && (bill.Docket == null || bill.Docket.Answer == 0)) { return; }   // PS-6 US-21c (J6 (i)): only an answered bill moves
            if (bill.Docket == null)
            {
                bill.DaysRemaining--;
                if (bill.DaysRemaining > 0) { return; }
            }
            BillConcern concern = UsDebtLimit.ConcernOf(country, bill);   // 2 U.S.C. 641(a)(3): reconciliation may change the debt limit - a majority in the Senate, no cloture (DECLARED)
            DivisionRecord passage = null;
            bool passed = true;
            bool presentedToday = bill.Docket == null;
            if (presentedToday) { passage = Congress.Divide(country, UsDebtLimit.BillTitle(), concern, CurrentDate, out passed, BillAxis.Fiscal, Congress.OriginOf(country, concern, CurrentDate)); }
            bool? decided = UsVetoGate(country, passage, concern, passed, ref bill.Docket);
            if (decided == null)
            {
                if (presentedToday) { Debug.Log($"LIMIT: {country.Id} - the raise is on the President's desk ({bill.Docket?.Passage?.Title})"); }
                return;
            }
            _pendingDebtLimitBillByCountry.Remove(country.Id);
            if (decided.Value) { UsDebtLimit.Raise(country, CurrentDate, "an act of Congress"); return; }
            float approvalBefore = country.State.ApprovalRating;
            ParliamentSystem.ApplyBillResult(country, bill, false, ApplyBudgetBillSpendingAndSwf, CurrentDate.Year, Congress.GovernmentBears(country, concern));   // the failed vote's cost only - the raise is never applied as a budget
            ApprovalLedgerRecorder.RecordEvent(country, CurrentDate, "Debt limit raise failed", country.State.ApprovalRating - approvalBefore);
            Debug.Log($"LIMIT: {country.Id} - the raise falls - the limit stands at {UsDebtLimit.LawWords(country)}");
        }
    }
}
