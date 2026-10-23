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
    /// <item><b>Who tables it</b> (Elias's K23, 2026-10-23: "AI governments keep the automatic tabling"): an AI government of the day tables the raise itself, a
    /// bill's course (`ParliamentSystem.BillDurationDays`) before the measures run out, so it is voted on that day (`UsDebtLimit.RaiseDue`); fallen, it is
    /// tabled again the next day while the limit binds. A PLAYER-PRESIDENT tables nothing by himself: he proposes (`ProposeDebtLimitRaise`) a raise
    /// by a size (added to the excess, K77) or a suspension to a day (K22), voted at the deadline (K25) - or, once the measures have run out, the day it is
    /// tabled (K43). In a counterfactual world, where no player answers, the AI's rule tables it the morning after the limit binds with nothing before Congress
    /// (K49's rule, `AnswerDebtLimitAsTheAi`).</item>
    /// <item><b>How it is voted</b> (K25, as built): by reconciliation - a majority in both chambers, the House first (2 U.S.C. 641(a)(3) lets reconciliation
    /// change the limit; the record's last raise, P.L. 119-21, was such an act) - on the budget's own concern builder, which reads a bill moving no rate, line,
    /// benefit or fund as asking the blocs nothing, so it passes uncontested unless WITHHELD (K22, `WithholdDebtLimitRaise`: the withholder against
    /// in both chambers, every other party for, so a chamber it holds stops it); then presented (`UsVetoGate`): an AI president answers by his rule, a
    /// player-president's desk holds it on the Docket.</item>
    /// <item><b>What it does</b>: law, the limit rises by the day's excess and the bill's size (`UsDebtLimit.Enact`, K77) - or is suspended to its day
    /// (K22) - and the episode closes; fallen, its failed vote is charged as a continuing resolution's is (`Congress.GovernmentBears`).</item>
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

        /// <summary>PS-6 K23: whether the government of the day tables the raise by itself - an AI government's; a player-president proposes his own.</summary>
        private bool TablesTheRaiseItself(Country country) => !PlayerGoverns(country);

        /// <summary>PS-6 US-24: the day's step for the debt limit where the game's Congress votes - the pending raise counted down, voted and presented, or decided on
        /// the docket; else one tabled where it is due and an AI government tables it (K23); then the next day's flows armed for the cut while the limit binds. On
        /// the docket answers' run (US-21c) only an answered bill moves, nothing is tabled and nothing is armed. A standing withhold whose party now holds the
        /// presidency lapses first (K22).</summary>
        private void AdvanceDebtLimitDay(CountryId countryId)
        {
            Country country = _world?.GetCountry(countryId);
            if (!UsDebtLimit.Applies(country) || !UsBudgetProcedure.Applies(country) || !UsDebtLimit.IsRead(country)) { return; }
            if (!_docketAnswersOnly && UsDebtLimit.WithholdLapses(country))
            {
                Debug.Log($"LIMIT: {country.Id} - {country.DebtLimitWithheldBy} now holds the presidency: its withhold of the raise lapses (the raise is its own government's)");
                country.DebtLimitWithheldBy = null;
            }
            if (!_docketAnswersOnly && UsDebtLimit.WithholdOutlived(country, CurrentDate))   // the review's L2: a withhold whose party holds neither chamber no longer bites
            {
                Debug.Log($"LIMIT: {country.Id} - {country.DebtLimitWithheldBy} holds neither chamber now: its withhold of the raise lapses");
                country.DebtLimitWithheldBy = null;
            }
            if (!_docketAnswersOnly) { AiWithholdDay(country); }   // PS-6 K78: the AI's hostile majority, before the day's vote, so a release on the deadline lets it pass
            if (_pendingDebtLimitBillByCountry.TryGetValue(countryId, out BudgetBill bill)) { ResolveDebtLimitBillDay(country, bill); }
            else if (!_docketAnswersOnly && UsDebtLimit.RaiseDue(country, CurrentDate) && TablesTheRaiseItself(country)) { TableDebtLimitRaise(country, ParliamentSystem.BillDurationDays); }
            if (_docketAnswersOnly) { return; }
            System.DateTime flows = UsDebtLimit.Binding(country, CurrentDate) ? CurrentDate.Date.AddDays(1) : System.DateTime.MinValue;
            if (country.DebtLimitCutFlowDay != flows)
            {
                if (flows != System.DateTime.MinValue && country.DebtLimitCutFlowDay == System.DateTime.MinValue) { Debug.Log($"LIMIT: {country.Id} - the measures ran out on {UsDebtLimit.BindsOn(country):yyyy-MM-dd} with no raise law: from {flows:yyyy-MM-dd} the day's non-interest outlays are cut to what revenue leaves after interest, until a raise becomes law"); }
                country.DebtLimitCutFlowDay = flows;
            }
        }

        /// <summary>PS-6 K78 (Elias's ruling of 2026-10-23: "AI hostile majorities may withhold the raise, by a rule fitted on the record's episodes"; the rule
        /// [AUTHORED-DRAFT], n = 3, `ElectionsData/usa/debtlimit_episodes.md`) with K89 ("the AI's withhold BITES"): an AI party holding a chamber against the
        /// presidency withholds the raise from the day the limit is reached through the deadline, and releases it on `UsDebtLimit.AiReleaseDay` - the measures'
        /// end plus the record's largest lead - before that day's vote; a raise voted before then falls, and the cut binds meanwhile. A release is a change of the
        /// episode (K90): a President who chose to let it bind is asked again. A player's party's own withhold is never touched here.</summary>
        private void AiWithholdDay(Country country)
        {
            string ai = UsDebtLimit.AiWithholder(country, CurrentDate);
            bool withholds = UsDebtLimit.AiWithholdsOn(country, CurrentDate);
            string standing = country.DebtLimitWithheldBy;
            bool standingIsAi = standing != null && standing != country.PlayerPartyAbbrev;
            if (standing == null && ai != null && withholds)
            {
                country.DebtLimitWithheldBy = ai;
                Debug.Log($"LIMIT: {country.Id} - {ai}, holding a chamber against the presidency, withholds the raise from {CurrentDate:yyyy-MM-dd} (the record's hostile majorities, K78, K89) - released on {UsDebtLimit.AiReleaseDay(country):yyyy-MM-dd}");
            }
            else if (standingIsAi && (!withholds || standing != ai))
            {
                country.DebtLimitWithheldBy = null;
                country.DebtLimitLetBind = false;   // K90: a change of the episode - the President is asked again
                Debug.Log($"LIMIT: {country.Id} - {standing} releases the raise on {CurrentDate:yyyy-MM-dd} (K89: {UsDebtLimit.AiHoldDaysPastDeadline} day(s) past the measures' end, the record's largest lead)");
            }
        }

        /// <summary>PS-6 US-24: the raise tabled by the government of the day, voted after <paramref name="days"/> - a bill's course; one day where the AI's rule
        /// answers for an unanswering player past the deadline (K49), so it is voted that day.</summary>
        private void TableDebtLimitRaise(Country country, int days)
        {
            var bill = new BudgetBill { GovernmentBill = !PlayerGoverns(country), DaysRemaining = days };
            _pendingDebtLimitBillByCountry[country.Id] = bill;
            Debug.Log($"LIMIT: {country.Id} - {(bill.GovernmentBill ? "the government" : country.PlayerPartyAbbrev + " (by the AI's rule - no player answers in this world)")} tables the raise ({UsDebtLimit.BillTitle(bill)}); Congress votes it in {bill.DaysRemaining} day(s), by a majority in both chambers, then it goes to the President");
        }

        /// <summary>PS-6 US-24: the raise's day - on its vote day both chambers vote it by reconciliation and it is presented; held on the docket, the President's
        /// answer (or the period's end) decides it with no fresh vote. Decided, the slot empties: law raises (or suspends) the limit; fallen, its failed vote is
        /// charged where the government bears it, and while the limit binds the next day tables another where the government tables its own (K23).</summary>
        private void ResolveDebtLimitBillDay(Country country, BudgetBill bill)
        {
            if (_docketAnswersOnly && (bill.Docket == null || bill.Docket.Answer == 0)) { return; }   // PS-6 US-21c (J6 (i)): only an answered bill moves
            if (bill.Docket == null)
            {
                bill.DaysRemaining--;
                if (bill.DaysRemaining > 0) { return; }
            }
            BillConcern concern = UsDebtLimit.ConcernOf(country, bill);   // 2 U.S.C. 641(a)(3): reconciliation may change the debt limit - a majority in the Senate, no cloture (DECLARED); K22: a withheld raise carries its withholder
            DivisionRecord passage = null;
            bool passed = true;
            bool presentedToday = bill.Docket == null;
            string title = UsDebtLimit.BillTitle(bill);
            if (presentedToday) { passage = Congress.Divide(country, title, concern, CurrentDate, out passed, BillAxis.Fiscal, Congress.OriginOf(country, concern, CurrentDate)); }
            bool? decided = UsVetoGate(country, passage, concern, passed, ref bill.Docket);
            if (decided == null)
            {
                if (presentedToday) { Debug.Log($"LIMIT: {country.Id} - the raise is on the President's desk ({bill.Docket?.Passage?.Title})"); }
                return;
            }
            _pendingDebtLimitBillByCountry.Remove(country.Id);
            if (decided.Value) { UsDebtLimit.Enact(country, CurrentDate, bill, "an act of Congress"); return; }
            float approvalBefore = country.State.ApprovalRating;
            ParliamentSystem.ApplyBillResult(country, bill, false, ApplyBudgetBillSpendingAndSwf, CurrentDate.Year, Congress.GovernmentBears(country, concern));   // the failed vote's cost only - the raise is never applied as a budget
            ApprovalLedgerRecorder.RecordEvent(country, CurrentDate, "Debt limit raise failed", country.State.ApprovalRating - approvalBefore);
            country.DebtLimitLetBind = false;   // K90: a raise that falls (or whose veto stands) is a change - the President is asked again
            Debug.Log($"LIMIT: {country.Id} - {title}: falls{(concern.WithheldBy != null ? " - " + concern.WithheldBy + " withholds it" : string.Empty)} - the limit stands at {UsDebtLimit.LawWords(country)}");
        }

        /// <summary>
        /// PS-6 K23 (Elias's ruling of 2026-10-23, `docs/RULINGS_2026-10-23.md`: "a player-president gets a verb: propose the raise, its amount or a suspension";
        /// K25 "voted at the deadline: as built"; K43 "after a fallen or withheld raise the player may propose again, voted the day it is tabled"): THE
        /// PLAYER-PRESIDENT PROPOSES THE RAISE - by the excess and <paramref name="raiseUsd"/> (K77: the size is added to the day's excess when it is law), or a suspension through
        /// <paramref name="suspendThrough"/> (MinValue for a raise). Tabled by reconciliation, it is voted on the day the measures run out; proposed once they have run
        /// out, it is voted at once. FORK-2: the size as tabled is handed to every replay, so the bill's title is the same in
        /// every world. False with the reason where refused (`UsDebtLimit.MayPropose`).
        /// </summary>
        public bool ProposeDebtLimitRaise(CountryId countryId, double raiseUsd, System.DateTime suspendThrough, out string refusedBecause)
        {
            Country country = _world?.GetCountry(countryId);
            double tabled = suspendThrough != System.DateTime.MinValue || country == null ? 0d : raiseUsd;   // K77: the size over the excess, as given - the excess is added on the day it is law
            string why = null;
            bool acted = Answered(new PlayerAnswer(suspendThrough != System.DateTime.MinValue ? "a debt-limit suspension proposed" : "a debt-limit raise proposed", countryId,
                    sim => sim.ProposeDebtLimitRaiseNow(countryId, tabled, suspendThrough, out _)),
                () => ProposeDebtLimitRaiseNow(countryId, tabled, suspendThrough, out why));
            refusedBecause = why;
            return acted;
        }

        /// <summary>FORK-2: `ProposeDebtLimitRaise`'s act, on the size as tabled.</summary>
        private bool ProposeDebtLimitRaiseNow(CountryId countryId, double tabledUsd, System.DateTime suspendThrough, out string refusedBecause)
        {
            Country country = _world?.GetCountry(countryId);
            bool playerPresident = country != null && PlayerCountryId.HasValue && PlayerCountryId.Value == countryId && !string.IsNullOrEmpty(country.PlayerPartyAbbrev) && PlayerGoverns(country);
            if (!UsDebtLimit.MayPropose(country, playerPresident, UsBudgetProcedure.Applies(country), GetPendingDebtLimitBill(countryId), CurrentDate, tabledUsd, suspendThrough, out refusedBecause)) { return false; }
            bool atOnce = UsDebtLimit.VotedTheDayTabled(country, CurrentDate);
            int days = atOnce ? 1 : System.Math.Max(1, (UsDebtLimit.BindsOn(country) - CurrentDate.Date).Days);
            var bill = new BudgetBill { GovernmentBill = false, DaysRemaining = days, DebtLimitRaiseUsd = tabledUsd, DebtLimitSuspendThrough = suspendThrough == System.DateTime.MinValue ? System.DateTime.MinValue : suspendThrough.Date };
            _pendingDebtLimitBillByCountry[countryId] = bill;
            Debug.Log($"LIMIT: {countryId} - the President ({country.PlayerPartyAbbrev}) proposes {UsDebtLimit.BillTitle(bill)}; " + (atOnce
                ? "the measures have run out, so both chambers vote it today (K43)"
                : $"both chambers vote it on {UsDebtLimit.BindsOn(country):yyyy-MM-dd}, the day the measures run out (K25)"));
            if (atOnce) { ResolveDebtLimitBillDay(country, bill); }   // K43: voted the day it is tabled - its course of one day spent here
            return true;
        }

        /// <summary>
        /// PS-6 K90 (Elias's ruling of 2026-10-24: a player-president may answer the K85 hold with "LET IT BIND"): the President lets the binding limit bite - the
        /// hold is released and the cut runs until he proposes later. The rule (DECLARED): the hold does not return in the episode until something changes - a
        /// raise that falls or whose veto stands, the AI's withhold released (K89), the episode closed (a raise or suspension law, or the debt back under the
        /// limit). FORK-2: an answer, replayed in every except-world; a counterfactual world with no answer answers the wait by the AI's rule as before. False
        /// with the reason where refused (nothing waits).
        /// </summary>
        public bool LetDebtLimitBind(CountryId countryId, out string refusedBecause)
        {
            string why = null;
            bool acted = Answered(new PlayerAnswer("the debt limit let bind", countryId, sim => sim.LetDebtLimitBindNow(countryId, out _)), () => LetDebtLimitBindNow(countryId, out why));
            refusedBecause = why;
            return acted;
        }

        /// <summary>FORK-2: `LetDebtLimitBind`'s act.</summary>
        private bool LetDebtLimitBindNow(CountryId countryId, out string refusedBecause)
        {
            refusedBecause = null;
            Country country = _world?.GetCountry(countryId);
            if (country == null || !DebtLimitAwaitsPresident(countryId)) { refusedBecause = "NOTHING WAITS ON THE DEBT LIMIT"; return false; }
            country.DebtLimitLetBind = true;
            Debug.Log($"LIMIT: {countryId} - the President lets the limit bind on {CurrentDate:yyyy-MM-dd} (K90): spending other than interest is cut to revenue until a raise is proposed and law");
            return true;
        }

        /// <summary>
        /// PS-6 K22 (Elias's ruling of 2026-10-23: "add withholding - a hostile chamber can withhold the raise, as with the CR"): THE PLAYER'S PARTY WITHHOLDS (or
        /// releases) THE DEBT-LIMIT RAISE - where it holds a chamber and not the presidency (`UsDebtLimit.MayWithhold`). Withheld, it stands against every raise
        /// voted in both chambers and every other party for (the continuing resolution's fixed sides), until released, until a raise or a suspension is law, or
        /// until it holds the presidency; always releasable. No draw of the withholder's own members is made (none is fitted on the debt-limit record, DECLARED;
        /// the continuing resolution's is on lapse-period cloture votes, and reconciliation has no cloture). An AI party withholds by K78's rule (`AiWithholdDay`).
        /// </summary>
        public bool WithholdDebtLimitRaise(CountryId countryId, bool withhold, out string refusedBecause)
        {
            string why = null;
            bool acted = Answered(new PlayerAnswer(withhold ? "a debt-limit raise withheld" : "a debt-limit raise released", countryId, sim => sim.WithholdDebtLimitRaise(countryId, withhold, out _)),
                () => WithholdDebtLimitRaiseNow(countryId, withhold, out why));
            refusedBecause = why;
            return acted;
        }

        /// <summary>FORK-2: `WithholdDebtLimitRaise`'s act.</summary>
        private bool WithholdDebtLimitRaiseNow(CountryId countryId, bool withhold, out string refusedBecause)
        {
            refusedBecause = null;
            Country country = _world?.GetCountry(countryId);
            if (country == null || !PlayerCountryId.HasValue || PlayerCountryId.Value != countryId || !UsBudgetProcedure.Applies(country)) { refusedBecause = UsDebtLimit.NoLimitWords; return false; }
            if (!withhold)
            {
                if (country.DebtLimitWithheldBy == null) { refusedBecause = "NOTHING IS WITHHELD"; return false; }
                if (country.DebtLimitWithheldBy != country.PlayerPartyAbbrev) { refusedBecause = "THE RAISE IS WITHHELD BY ANOTHER PARTY"; return false; }   // PS-6 K78: an AI party's withhold is its own
                country.DebtLimitWithheldBy = null;
                Debug.Log($"LIMIT: {countryId} - {country.PlayerPartyAbbrev} releases the debt-limit raise");
                return true;
            }
            if (!UsDebtLimit.MayWithhold(country, true, CurrentDate, out refusedBecause)) { return false; }
            country.DebtLimitWithheldBy = country.PlayerPartyAbbrev;
            Debug.Log($"LIMIT: {countryId} - {country.PlayerPartyAbbrev} withholds the debt-limit raise: it votes against it in both chambers");
            return true;
        }

        /// <summary>PS-6 K85 (Elias's ruling of 2026-10-23, on the review's M1): a binding limit with nothing proposed WAITS ON THE PRESIDENT - the played clock holds
        /// on it (`GameController.DebtLimitHolds`), so the player always answers first and every fork replays that answer. A read.</summary>
        public bool DebtLimitAwaitsPresident(CountryId countryId)
        {
            Country country = _world?.GetCountry(countryId);
            bool playerPresident = country != null && PlayerCountryId.HasValue && PlayerCountryId.Value == countryId && !string.IsNullOrEmpty(country.PlayerPartyAbbrev)
                && PlayerGoverns(country) && country.Government != null && country.Government.RoleOf(country.PlayerPartyAbbrev) == Elections.PlayerRole.PrimeMinister;
            return UsDebtLimit.AwaitsPresident(country, playerPresident, UsBudgetProcedure.Applies(country), GetPendingDebtLimitBill(countryId), CurrentDate);
        }

        /// <summary>PS-6 K23 with K49 (Elias's ruling: "in a counterfactual, an unanswered question is answered as an AI-run party would, the next morning") and K85:
        /// the wait above answered by the AI's rule in a world where no player answers it - the morning after the played world held its clock on it and the
        /// replayed answer found no match there, or in the no-policy world (handed no answer, governing with no changes) - the raise tabled and voted that day.
        /// True where it tabled. Never the played world's.</summary>
        private bool AnswerDebtLimitAsTheAi(Country country)
        {
            if (!IsCounterfactual || country == null || !DebtLimitAwaitsPresident(country.Id)) { return false; }
            TableDebtLimitRaise(country, 1);
            return true;
        }
    }
}
