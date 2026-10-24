using System.Collections.Generic;
using PoliSim.Data;
using UnityEngine;

namespace PoliSim.Simulation
{
    /// <summary>
    /// SP-7b (`docs/specs/POLAND_PRESIDENTIAL_START_PLAN.md` §5a and §6; Elias's rulings I14, 2026-10-18, installed §857, and J6 (iii), 2026-10-19, installed
    /// §870): POLAND'S PLAYER-PRESIDENT.
    /// <list type="bullet">
    /// <item><b>The office</b> (ask 2 (2a)): the player's candidate elected, the player becomes the President on the oath's day (E2: the day the predecessor's
    /// term ends) - <see cref="NotePlayerPresidency"/> writes `Country.PlayerHoldsPresidency` at each day's start. While he holds it the party's levers pass
    /// to the AI (READING of Konstytucja Art. 132, [TK-KONST]: "Prezydent Rzeczypospolitej nie może piastować żadnego innego urzędu ani pełnić żadnej
    /// funkcji publicznej" - a party role is not an office, but a player directing both would sign his own party's bills): <see cref="PlayerGoverns"/> is
    /// false, so the AI ministries govern where the party leads the government; the role gate refuses every lever in the office's words
    /// (<see cref="PresidentOfficeWords"/>), and so do the party's own verbs - leaving the government, withdrawing support, moving no confidence
    /// (<see cref="LeaveGovernment"/>, <see cref="WithdrawSupport"/>, <see cref="MoveNoConfidence"/>; their chips are not drawn); the party's chairs and its
    /// Finance dial are not the player's (`CabinetSystem.ChairIsThePlayers`, `FinancePartner.PlayerHolds`); a budget window open at the oath is closed
    /// (`CloseBudgetWindowIfNotGoverning`, §698's class), so the AI government tables the next budget on its day. A loss, and the presidency's end (two terms, or a re-election lost), leave the player the backing party (L1, end-i):
    /// the flag falls and every lever is the party's again.</item>
    /// <item><b>The verb</b>: signing or vetoing each statute the Sejm passes - the President's own act, no countersignature needed (Art. 144 ust. 3 pkt 6,
    /// [TK-KONST]) - in place of F3's draw for a player-president (the draw stays for every AI president, `PresidentialVetoGate`). A statute presented to
    /// him is HELD (<see cref="PolishDocketGate"/>, at the gate every bill site already reaches with its slot, US-21b's docket) until he answers
    /// (<see cref="SignDocketBill"/>, <see cref="VetoDocketBill"/>) or its term runs out: J6 (iii)'s terms - 7 days for the budget act (Art. 224 ust. 1)
    /// and an urgent bill (Art. 123 ust. 3), 21 otherwise (Art. 122 ust. 2), `PresidentialDocket` - and unanswered it is signed (I12 (i), DECLARED). An
    /// answer acts at once, as US-21c's docket acts on every answer (J6 (i), read for Poland too - DECLARED), its money from the next day step; a veto goes to the Sejm's 3/5 override as built (B2), on the
    /// passage's sides (<see cref="RecordPresidentialVeto"/>, one writer with the draw's). The budget act cannot be vetoed (Art. 224) and neither can a
    /// constitutional amendment (Art. 235 ust. 7): the docket holds them for their terms and takes only a signature.</item>
    /// <item><b>The budget</b>: adopted while the player holds the office, Poland's budget act and each statute act the Sejm passes go to the docket together
    /// (<see cref="HoldPolishBudget"/> - the budget act at 7 days, each act at 21); a statute act the Sejm rejects falls on the day, charged as
    /// `PolishStatuteActs` charges it. The bill keeps its slot, and applies once - less every fallen act's part - the day the last held act is decided
    /// (<see cref="FinishPolishBudget"/>), as the US budget's held acts do (`FinishUsBudgetActs`).</item>
    /// </list>
    /// Nothing here runs where the player holds no Polish presidency, so a game that never starts as a candidate - every no-player dump and every Sejm-start
    /// game - takes none of these paths (the dump diff proves it).
    /// </summary>
    public partial class SimulationManager
    {
        /// <summary>SP-7b: the role gate's words while the player holds the presidency - the office, who holds the party, what the office does.</summary>
        public const string PresidentOfficeWords = "PRESIDENT · YOUR PARTY IS THE AI'S WHILE YOU HOLD THE OFFICE · YOU SIGN OR VETO WHAT THE SEJM PASSES";

        /// <summary>SP-7b: whether the player is Poland's President today (`Country.PlayerHoldsPresidency`, written at the day's start) in the player's own country.</summary>
        private bool PlayerPresidentDockets(Country country) =>
            country != null && PlayerCountryId.HasValue && PlayerCountryId.Value == country.Id && country.PlayerHoldsPresidency;

        /// <summary>SP-7b: the office, written at the day's start for the player's country - true from the oath of a contest his candidate won to the day a
        /// successor takes office. Inert where the player stood no candidate and holds no office (no write, no read of the contests).</summary>
        private void NotePlayerPresidency()
        {
            Country country = PlayerCountryId.HasValue ? _world?.GetCountry(PlayerCountryId.Value) : null;
            if (country == null || (!country.PlayerHoldsPresidency && string.IsNullOrEmpty(country.PlayerCandidate))) { return; }
            bool holds = Elections.PresidentialElection.PlayerHoldsOffice(country, CurrentDate, out string president);
            if (holds == country.PlayerHoldsPresidency) { return; }
            HashSet<CabinetPortfolio> chairsBefore = CabinetSystem.PlayersChairs(country);   // K59 (the review of wave/form, L2): none while the office is held
            country.PlayerHoldsPresidency = holds;
            CabinetSystem.SeatOnEntry(country, chairsBefore);   // K59: the office ends and the party's chairs are the player's again - every one standing vacant is seated at the average (DECLARED: an entry, as at an install)
            if (holds) { CloseBudgetWindowIfNotGoverning(country); }   // the review's M3, §698's class: a window open at the oath is the party's, which no longer governs for the player - closed, so the AI government tables the next budget
            Debug.Log(holds
                ? $"PRESIDENTIAL: {country.Id} - the oath, {CurrentDate:yyyy-MM-dd}: you are the President ({president}); your party, {country.PlayerPartyAbbrev}, is the AI's while you hold the office (Art. 132, READING)"
                : $"PRESIDENTIAL: {country.Id} - the presidency ends on {CurrentDate:yyyy-MM-dd}: the run goes on as your party, {country.PlayerPartyAbbrev} (end-i)");
        }

        /// <summary>SP-7b: the player's verdict in the log on a round's day - a contest his candidate stood in (the President row says it on the desk).</summary>
        private void NotePlayerVerdict(Country country, Elections.PresidentialElection.Contest held)
        {
            if (held == null || held.PlayerCandidate == null) { return; }
            string verdict = !held.PlayerStood() ? "your candidate does not stand - " + string.Join("; ", held.NotStanding)
                : held.PlayerWon() ? $"your candidate, {held.PlayerCandidate}, is elected - you take office on {held.TakesOffice:yyyy-MM-dd}"
                : held.Decided() ? $"your candidate, {held.PlayerCandidate}, is not elected - you play on as your party, {country.PlayerPartyAbbrev} (L1)"
                : held.RunOffPending() && (held.RunOffA == held.PlayerCandidate || held.RunOffB == held.PlayerCandidate) ? $"your candidate, {held.PlayerCandidate}, is in the run-off on {held.RunOffOn:yyyy-MM-dd}"
                : held.RunOffPending() ? $"your candidate, {held.PlayerCandidate}, is out in the first vote - you play on as your party, {country.PlayerPartyAbbrev} (L1)"
                : "nothing is decided";
            Debug.Log($"PRESIDENTIAL: {country.Id} - the verdict for you: {verdict}");
        }

        /// <summary>
        /// SP-7b: THE POLISH DOCKET'S GATE, reached from `UsVetoGate` at every bill site with the site's slot (<paramref name="docket"/>). A statute the Sejm
        /// passed and an AI president answered at <see cref="PresidentialVetoGate"/> passes through as decided there. Presented to the PLAYER-president it is
        /// held: the entry written with the passage, the day and the bill's kind (<paramref name="bill"/>), and null returned - the site neither applies nor
        /// charges it. Each later day the site returns here with the held entry, no fresh vote: an answer is acted on - signed, it is law; vetoed, the Sejm
        /// votes on the veto (<see cref="RecordPresidentialVeto"/>) - and stands whoever holds the office by then (presented as of the day it was given);
        /// unanswered once the term has run (<see cref="DocketLastDay"/>, by the bill's kind), it is signed (I12 (i), DECLARED); unanswered with the player no longer President,
        /// the president of the day answers by his own rule, the draw, that day. Returns whether the statute stands, or null while held.
        /// </summary>
        private bool? PolishDocketGate(Country country, DivisionRecord passage, bool passed, ref DocketEntry docket, Elections.PresidentialDocket.Bill bill)
        {
            if (docket != null) { passage = docket.Passage; passed = passage != null; bill = (Elections.PresidentialDocket.Bill)docket.Bill; }
            if (!passed || passage == null) { docket = null; return passed; }
            Elections.PresidentialVeto.Act act = bill == Elections.PresidentialDocket.Bill.BudgetAct ? Elections.PresidentialVeto.Act.BudgetAct
                : bill == Elections.PresidentialDocket.Bill.ConstitutionalAmendment ? Elections.PresidentialVeto.Act.ConstitutionalAmendment : Elections.PresidentialVeto.Act.OrdinaryStatute;
            if (docket == null)
            {
                if (!PlayerPresidentDockets(country)) { return passed; }   // an AI president answered at the passage's own gate (PresidentialVetoGate) - nothing is held
                docket = new DocketEntry { Passage = passage, PresentedOn = CurrentDate.Date, Bill = (int)bill };
                Debug.Log($"DOCKET: {country.Id} - presented to the President (you) on {CurrentDate:yyyy-MM-dd}: {passage.Title} - answered by {DocketLastDay(country.Id, docket):yyyy-MM-dd}, or it is signed ({Elections.PresidentialDocket.PeriodWords(country.Id, bill)})");
                return null;
            }
            if (docket.Answer != 0)
            {
                bool vetoes = docket.Answer < 0 && Elections.PresidentialVeto.MayVeto(act);
                System.DateTime answeredOn = docket.AnsweredOn == System.DateTime.MinValue ? CurrentDate : docket.AnsweredOn;
                docket = null;
                if (!vetoes) { Debug.Log($"VETO: {country.Id} - signed by the President (you, on {answeredOn:yyyy-MM-dd}): {passage.Title}"); return true; }
                Elections.PresidentialVeto.Outcome veto = Elections.PresidentialVeto.Decide(country.Id, country.PresidentialElections, answeredOn, act, passage.Sides);
                if (veto == null)
                {
                    Debug.LogError($"VETO: {country.Id} - no president found on {answeredOn:yyyy-MM-dd} for '{passage.Title}' - the veto is not recorded and the statute stands, a defect");
                    return true;
                }
                veto.VetoedByThePlayer();
                return RecordPresidentialVeto(country, passage, veto, "the President's own answer, given " + answeredOn.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture));
            }
            if (!PlayerPresidentDockets(country))
            {
                docket = null;   // unanswered, and the president of the day is not the player: he answers by his own rule, today
                return PresidentialVetoDrawn(country, passage, true, act);
            }
            if (CurrentDate.Date <= DocketLastDay(country.Id, docket)) { return null; }   // the docket's one reader of the term (US-22c's, by the bill's kind): held through its last day, signed the day after
            Debug.Log($"DOCKET: {country.Id} - unanswered since {docket.PresentedOn:yyyy-MM-dd} ({Elections.PresidentialDocket.PeriodWords(country.Id, bill)} ran out on {DocketLastDay(country.Id, docket):yyyy-MM-dd}): {passage.Title} - signed");
            docket = null;
            return true;
        }

        /// <summary>SP-7b: the player-president's answer recorded on a held statute (`AnswerDocket`'s Polish half) - refused where he does not hold the office
        /// today, where the bill is not held or already answered, and a veto refused on the budget act (Art. 224) and a constitutional amendment (Art. 235
        /// ust. 7). Acted on at once (`ActOnDocketAnswers`, J6 (i)'s rule read for Poland).</summary>
        private bool AnswerPolishDocket(Country country, DivisionRecord passage, int answer, out string refusedBecause)
        {
            refusedBecause = null;
            if (!PlayerPresidentDockets(country)) { refusedBecause = "you do not hold the presidency today"; return false; }
            DocketEntry answered = null;
            foreach (DocketEntry entry in DocketEntries(country.Id))
            {
                if (!ReferenceEquals(entry.Passage, passage)) { continue; }
                if (entry.Answer != 0) { refusedBecause = "already answered"; return false; }
                if (answer < 0 && !MayVetoDocketEntry(entry))
                {
                    refusedBecause = entry.Bill == (int)Elections.PresidentialDocket.Bill.BudgetAct ? "the budget act cannot be vetoed" : "a constitutional amendment cannot be vetoed";
                    return false;
                }
                answered = entry;
                break;
            }
            if (answered == null) { refusedBecause = "that bill is not on your docket"; return false; }
            answered.Answer = answer;
            answered.AnsweredOn = CurrentDate.Date;
            Debug.Log($"DOCKET: {country.Id} - the President {(answer > 0 ? "signs" : "vetoes")} on {CurrentDate:yyyy-MM-dd}: {passage.Title} (acted on at once)");
            // J6 (i)'s rule, the US docket's, read for Poland too (DECLARED: one docket, one rule): the answer acts at once - signed, law today; vetoed, the Sejm
            // votes on the veto today; the money from the next day step. After the walk, as US-21c's verbs do - acting inside it would change the slots it walks
            ActOnDocketAnswers(country.Id);
            return true;
        }

        /// <summary>SP-7b: whether a held entry takes a veto - an ordinary (or urgent) statute; not the budget act (Art. 224), not an amendment (Art. 235 ust. 7).</summary>
        public static bool MayVetoDocketEntry(DocketEntry entry) =>
            entry != null && (entry.Bill == (int)Elections.PresidentialDocket.Bill.Ordinary || entry.Bill == (int)Elections.PresidentialDocket.Bill.Urgent);

        /// <summary>SP-7b: the docket item held for <paramref name="passage"/> in the country's slots, unanswered - what the signing plate asks the player-president
        /// to answer. False where none is held, or it is answered.</summary>
        public bool TryUnansweredDocketItem(CountryId countryId, DivisionRecord passage, out DocketItem item)
        {
            item = default;
            if (passage == null) { return false; }
            foreach (DocketItem i in GetDocket(countryId))
            {
                if (ReferenceEquals(i.Entry.Passage, passage) && i.Answer == 0) { item = i; return true; }
            }
            return false;
        }

        /// <summary>
        /// The vote on a Polish President's veto (§761, B2) - one writer for the draw's veto (<see cref="PresidentialVetoGate"/>) and the player-president's
        /// (<see cref="PolishDocketGate"/>): the passage marked a motion (no signing for a statute the President returned), the re-pass on the passage's own
        /// sides at 3/5 of those voting (`DivisionRecord.Required`), passed where overridden; a veto that stands is the day's loss (F5). <paramref name="how"/>
        /// says how the veto was reached, in the log. Returns whether the statute stands.
        /// </summary>
        private bool RecordPresidentialVeto(Country country, DivisionRecord passage, Elections.PresidentialVeto.Outcome veto, string how)
        {
            passage.Motion = true;
            var sides = new List<DivisionSide>();
            foreach (DivisionSide s in passage.Sides)
            {
                bool backing = s.Abbrev == veto.BackingParty;
                sides.Add(new DivisionSide { Abbrev = s.Abbrev, ShortName = s.ShortName, Seats = s.Seats, Side = s.Side, Alignment = s.Alignment,
                    Reason = !backing ? s.Reason : s.Side < 0 ? "the President's backing party - voted against the statute; against overriding the veto"
                        : s.Side > 0 ? "the President's backing party - voted for the statute; for the override, on the passage's sides"   // SP-7b: only a player-president vetoes a statute his party voted for
                        : "the President's backing party - abstained on the statute; abstains on the override",
                    ReasonShort = !backing ? s.ReasonShort : s.Side < 0 ? "the President's party - against the override" : s.Side > 0 ? "the President's party - for the override" : "the President's party - abstains" });
            }
            string title = $"Vetoed by the President ({veto.President}): {passage.Title} - " + (veto.Overridden ? "overridden" : "the veto stands") + $", {veto.Yes} for, {veto.Required} needed (3/5 of those voting, Art. 122 ust. 5)";
            DivisionRecord overrideVote = country.Divisions.Append(title, CurrentDate, passage.Alignment, veto.Overridden, passage.Direction, passage.Axis, sides);
            overrideVote.Required = veto.Required;
            if (!veto.Overridden) { ParliamentSystem.RecordBillLost(country, CurrentDate); }   // Elias's ruling F5: a veto that stands is a bill lost that day
            Debug.Log($"VETO: {country.Id} - {title} ({how})");
            return veto.Overridden;
        }

        /// <summary>
        /// SP-7b: a Polish budget the Sejm has just adopted (<paramref name="budgetVote"/>), presented to the PLAYER-president - the budget act on the docket at
        /// 7 days (Art. 224 ust. 1), and each statute act the bill carries voted by the Sejm now, on its own division as `PolishStatuteActs` votes it: passed,
        /// on the docket at 21 days; rejected, fallen on the day and charged as every failed act is. The bill keeps its slot (`BudgetBill.ActsHeld`, with
        /// `BudgetBill.ActsLaw` and `BudgetBill.ActsFallen` - the US budget's fields, read here for Poland) and nothing of it applies until the last held act is
        /// decided (<see cref="FinishPolishBudget"/>). False - nothing done - where the player is not Poland's President today, so every other budget's day
        /// runs as before.
        /// </summary>
        private bool HoldPolishBudget(Country country, BudgetBill bill, DivisionRecord budgetVote)
        {
            if (bill == null || budgetVote == null || !Elections.WorldClock.StatutePartsAreActs(country.Id) || !PlayerPresidentDockets(country)) { return false; }
            var held = new List<DocketEntry> { new DocketEntry { Passage = budgetVote, PresentedOn = CurrentDate.Date, Bill = (int)Elections.PresidentialDocket.Bill.BudgetAct } };
            var fallen = new List<int>();
            foreach (BudgetBill.StatutePart part in BudgetBill.StatuteParts)
            {
                if (!bill.Changes(part, country)) { continue; }
                BudgetBill act = bill.PartOf(part);
                BillConcern concern = ParliamentSystem.GetBudgetBillConcern(country, act);
                bool passed = ParliamentSystem.WouldBillPass(country, concern);
                string title = StatuteActTitle(country, part, act);
                DivisionRecord vote = ParliamentSystem.RecordDivision(country, title, concern, passed, CurrentDate);
                if (passed) { held.Add(new DocketEntry { Passage = vote, PresentedOn = CurrentDate.Date, Act = (int)part }); continue; }
                fallen.Add((int)part);
                ChargeFallenAct(country, part);
                Debug.Log($"BUDGET: {country.Id} - {title}: falls in the Sejm - what it would set stays as it is, and the rest goes to the President");
            }
            bill.ActsHeld = held;
            bill.ActsLaw = new List<int>();
            bill.ActsFallen = fallen;
            Debug.Log($"BUDGET: {country.Id} - the budget adopted on {CurrentDate:yyyy-MM-dd}: the budget act and {held.Count - 1} statute act(s) presented to the President (you) - it applies the day the last is decided");
            return true;
        }

        /// <summary>SP-7b: a held Polish budget's day - each held act decided where the docket's gate decides it today (the answer, or signed when its term runs
        /// out unanswered; a veto to the Sejm's override); a statute act that falls is charged on its day; the budget act takes no veto. Once none is held the
        /// slot is emptied and the bill applies once, less every fallen act's part, as `PolishStatuteActs` and the budget's day would have applied it on one day.</summary>
        private void FinishPolishBudget(Country country, BudgetBill bill)
        {
            int year = bill.ActsHeld[0].PresentedOn.Year;   // the review's L1: the year the Sejm adopted it, whenever the last act is answered - every held act shares the adoption day (PassageYear reads the same)
            var still = new List<DocketEntry>();
            foreach (DocketEntry entry in bill.ActsHeld)
            {
                DocketEntry docket = entry;
                bool? decided = PolishDocketGate(country, entry.Passage, true, ref docket, (Elections.PresidentialDocket.Bill)entry.Bill);
                if (decided == null) { still.Add(entry); continue; }
                if (entry.Act < 0) { Debug.Log($"BUDGET: {country.Id} - {entry.Passage?.Title}: the budget act is signed"); continue; }   // Art. 224: no veto - it is law, answered or not
                var part = (BudgetBill.StatutePart)entry.Act;
                if (decided.Value) { bill.ActsLaw.Add(entry.Act); Debug.Log($"BUDGET: {country.Id} - {entry.Passage?.Title}: stands - it applies"); continue; }
                bill.ActsFallen.Add(entry.Act);
                ChargeFallenAct(country, part);
                Debug.Log($"BUDGET: {country.Id} - {entry.Passage?.Title}: its veto stands - what it would set stays as it is, and the rest applies");
            }
            if (still.Count > 0) { bill.ActsHeld = still; return; }
            bill.ActsHeld = null;
            _pendingBudgetBillByCountry.Remove(country.Id);
            _pendingBudgetAlternativeByCountry.Remove(country.Id);
            BudgetBill applied = bill;
            foreach (BudgetBill.StatutePart part in BudgetBill.StatuteParts) { if (bill.ActsFallen.Contains((int)part)) { applied = applied.Without(part, country); } }
            BillConcern concern = ParliamentSystem.GetBudgetBillConcern(country, ParliamentSystem.BudgetActOf(country, bill));
            float approvalBefore = country.State.ApprovalRating;
            ParliamentSystem.ApplyBillResult(country, applied, true, ApplyBudgetBillSpendingAndSwf, year, Congress.GovernmentBears(country, concern));
            if (bill.GovernmentBill) { FinancePartner.CreditAdopted(country, bill, ratesLanded: !bill.ActsFallen.Contains((int)BudgetBill.StatutePart.Rates)); }   // §755, §768: counted where adopted, its rates where they landed
            ApprovalLedgerRecorder.RecordEvent(country, CurrentDate, bill.GovernmentBill ? "The government's budget adopted" : "Budget bill passed (tax changes)", country.State.ApprovalRating - approvalBefore);
            Debug.Log($"BUDGET: {country.Id} - the budget's acts decided on {CurrentDate:yyyy-MM-dd}: applied once, {bill.ActsFallen.Count} act(s) withheld");
            bill.ActsLaw = null;
            bill.ActsFallen = null;
        }

        /// <summary>A Polish statute act that falls - in the Sejm, or its veto standing - costs what every failed bill costs (`PolishStatuteActs`' charge, §768's review).</summary>
        private void ChargeFallenAct(Country country, BudgetBill.StatutePart part)
        {
            float approvalBefore = country.State.ApprovalRating;
            TaxTerm.Shock(country, -ParliamentSystem.BillFailedApprovalCost);
            ApprovalLedgerRecorder.RecordEvent(country, CurrentDate, ActWords(part) + " failed", country.State.ApprovalRating - approvalBefore);
        }
    }
}
