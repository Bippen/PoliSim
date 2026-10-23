using PoliSim.Data;
using PoliSim.Elections;
using PoliSim.Simulation;
using UnityEngine;

namespace PoliSim.UI
{
    /// <summary>
    /// SP-7b (`docs/specs/POLAND_PRESIDENTIAL_START_PLAN.md` §6; Elias's ruling I14): POLAND'S PRESIDENTIAL START ON THE CONTROLLER - the candidate picker's
    /// commit (the start point handed to the world's opening, the plan's §4.2 class B) and the signing plate's two answers for a statute on the
    /// player-president's docket. The verdict is the President row's (`GameController.ParliamentRows.cs`) and the desk's chip's (`GameController.Desk.cs`).
    /// </summary>
    public partial class GameController
    {
        /// <summary>SP-7b: the start chosen on the sheet when it is not the country's default - read once by <see cref="OpenWorldAt"/> (the world opens on its
        /// day) and cleared when the selection completes. Null on every other path (the party panel, the harness, a load).</summary>
        private StartPoints.StartPoint? _chosenStart;

        /// <summary>SP-7b: set by the capture driver's hook (<see cref="TriggerAnswerPlateForNewestDivision"/>) - the next plate is the answer plate, staged.</summary>
        private bool _answerPlateStaged;

        /// <summary>SP-7b: the capture driver's hook - the newest division queued for the plate with its two answers, staged (no statute is held, so the
        /// answers write nothing): the REAL film of the plate's answer row, a Canvas surface (the plan's §6 step 4).</summary>
        private void TriggerAnswerPlateForNewestDivision()
        {
            _answerPlateStaged = true;
            TriggerSigningForNewestDivision();
        }

        /// <summary>SP-7b: the passage the open plate answers - set where the plate offers the two answers, null for every ceremony.</summary>
        private DivisionRecord _answering;

        /// <summary>
        /// SP-7b: the candidate picker's commit - the candidate of record seated on the selector's world (the backing party as the player's party, the candidate
        /// and the start's key on the country: `PresidentialStart.Seat`), then the country chosen as the party picker chooses it, the world opened on the
        /// start's own day (the Marshal's order) with the seat carried across. A candidate the start does not offer seats nothing and says so.
        /// </summary>
        private void SelectPresidentialCandidate(CountryId countryId, StartPoints.StartPoint start, string candidate)
        {
            Country country = _world?.GetCountry(countryId);
            if (!PresidentialStart.Seat(country, start, candidate, out string why))
            {
                Debug.LogError($"CANDIDATE PICKER: {candidate} cannot be seated on {countryId}'s {start.Kind} - {why}");
                return;
            }
            _chosenStart = start;
            try { SelectPlayerCountry(countryId); }
            finally { _chosenStart = null; }
            Debug.Log($"CANDIDATE PICKER: {countryId} - you stand as {_playerCountry?.PlayerCandidate}, your party {_playerCountry?.PlayerPartyAbbrev}; the world opens {SimulationManager.EpochDate:yyyy-MM-dd} ({_playerCountry?.PlayerStart})");
        }

        /// <summary>SP-7b: the plate for a division - its answers where it is a statute held on Poland's player-president's docket and not yet answered (the
        /// plan's §6 step 3), else the ceremony as built. PS-6 US-23: the US player-president's docket on the same plate - every bill both chambers sent and his
        /// desk holds, the out-party majority's reconciliation among them (one surface for both countries, not a second). The plate can be put off (LATER): the
        /// statute stays held and is answered on the Docket page (<see cref="DrawPresidentDeskItems"/>) - the one surface that reaches it after a load, or after a
        /// ceremony was dropped.</summary>
        private SigningScreen BuildSigningOrAnswer(DivisionRecord signing)
        {
            _answering = null;
            if (_answerPlateStaged && _playerCountry != null)
            {
                // the capture driver's staging (TriggerAnswerPlateForNewestDivision): the plate's answers on the newest division, its line the 21 days from
                // today - nothing is held, so an answer writes nothing and the beat alone closes it
                _answerPlateStaged = false;
                string staged = DeskLine(true, PresidentialDocket.Bill.Ordinary, "UNANSWERED, SIGNED ON " + DeskDay(PresidentialDocket.LawDay(CountryId.Poland, _simulationManager.CurrentDate, PresidentialDocket.Bill.Ordinary)));
                // PS-6 US-35 (the review's L8): a US plate staged for the film is a bill on the desk too - presented today, its law day the ten days' - so its path
                // reads ON YOUR DESK, never SIGNED
                SigningScreen.DeskContext stagedDesk = UsVeto.Applies(_playerCountry.Id)
                    ? new SigningScreen.DeskContext { PresentedOn = _simulationManager.CurrentDate, LawDay = PresidentialDocket.LawDay(_playerCountry.Id, _simulationManager.CurrentDate), Today = _simulationManager.CurrentDate, MayVeto = true }
                    : null;
                return SigningScreen.BuildAnswer(_playerCountry, signing, staged, AnswerSign, AnswerVeto, AnswerLater, stagedDesk);
            }
            if (_playerCountry != null && _simulationManager.PlayerAnswersDocket(_playerCountry.Id) && _simulationManager.TryUnansweredDocketItem(_playerCountry.Id, signing, out SimulationManager.DocketItem item))   // PS-6 US-23: the US desk too - one gate with the Docket page's (the review's L5)
            {
                _answering = signing;
                bool mayVeto = SimulationManager.MayVetoDocketEntry(item.Entry);
                // PS-6 US-35 (board 30b): on the US desk the plate carries the bill's path, the ten days and each verb's consequence
                SigningScreen.DeskContext desk = UsVeto.Applies(_playerCountry.Id)
                    ? new SigningScreen.DeskContext { PresentedOn = item.PresentedOn, LawDay = SimulationManager.DocketLawDay(_playerCountry.Id, item.Entry), Today = _simulationManager.CurrentDate,
                        MayVeto = mayVeto, CountsTenDays = item.Entry.Deadline == System.DateTime.MinValue }
                    : null;
                return SigningScreen.BuildAnswer(_playerCountry, signing, DeskLine(mayVeto, (PresidentialDocket.Bill)item.Entry.Bill, UnansweredLine(item.Entry)), AnswerSign,
                    mayVeto ? AnswerVeto : (System.Action)null, AnswerLater, desk);
            }
            return SigningScreen.Build(_playerCountry, signing, SignPendingDivision);
        }

        /// <summary>SP-7b: the answer plate's line - on the desk, the answers it takes, that LATER keeps it on the Docket page, and what an unanswered bill becomes
        /// (<see cref="UnansweredLine"/>).</summary>
        private static string DeskLine(bool mayVeto, PresidentialDocket.Bill bill, string unanswered) =>
            (mayVeto ? "ON YOUR DESK · SIGN OR VETO, OR ANSWER LATER ON THE DOCKET" : bill == PresidentialDocket.Bill.BudgetAct ? "ON YOUR DESK · THE BUDGET ACT TAKES NO VETO · SIGN, OR LATER ON THE DOCKET"
                : "ON YOUR DESK · AN AMENDMENT TAKES NO VETO · SIGN, OR LATER ON THE DOCKET") + " · " + unanswered;

        /// <summary>PS-6 US-23: what a bill on the desk becomes unanswered, and on which day (`SimulationManager.DocketLawDay`): a Polish statute is signed (SP-7b);
        /// a US bill is law without the President's signature after the ten days (Art. I §7), save one with a deadline of its own - the eve's continuing
        /// resolution, signed (J7).</summary>
        private string UnansweredLine(DocketEntry entry) => UnansweredWords(entry).ToUpperInvariant() + " " + DeskDay(SimulationManager.DocketLawDay(_playerCountry.Id, entry));

        /// <summary>PS-6 US-23: <see cref="UnansweredLine"/>'s words before its day, as a card prints them.</summary>
        private string UnansweredWords(DocketEntry entry) => UsVeto.Applies(_playerCountry.Id) && entry.Deadline == System.DateTime.MinValue ? "unanswered, law on" : "unanswered, signed on";

        /// <summary>
        /// SP-7b (the review's M1): THE PRESIDENT'S DESK ON THE DOCKET PAGE - one card per statute held for the player-president, its answer on the card: the
        /// title, the day it is signed unanswered, and ANSWER, which opens SIGN and VETO (SIGN alone for the budget act and an amendment). An answer acts at once
        /// (J6 (i)'s rule) and the decided entry leaves its slot, so only unanswered statutes are drawn. Drawn where the player is Poland's President - PS-6 US-23:
        /// or the US President of the day (`SimulationManager.PlayerAnswersDocket`); it is how a statute stays answerable after a load (the plate is raised only
        /// for a division newer than the load) or after LATER. Returns whether any card was drawn.
        /// </summary>
        private bool DrawPresidentDeskItems()
        {
            if (_playerCountry == null || !_simulationManager.PlayerAnswersDocket(_playerCountry.Id)) { return false; }
            bool any = false;
            foreach (SimulationManager.DocketItem item in _simulationManager.GetDocket(_playerCountry.Id))
            {
                DivisionRecord passage = item.Entry.Passage;
                if (passage == null) { continue; }
                any = true;
                string key = "desk:" + passage.Number.ToString(System.Globalization.CultureInfo.InvariantCulture) + ":" + item.PresentedOn.ToString("yyyyMMdd", System.Globalization.CultureInfo.InvariantCulture);
                string title = "On your desk · " + passage.Title;
                if (item.Answer != 0) { continue; }   // never held past the answer (it acts at once) - nothing to draw
                bool mayVeto = SimulationManager.MayVetoDocketEntry(item.Entry);
                string line = (mayVeto ? "Sign or veto" : "Sign - it takes no veto") + " · " + UnansweredWords(item.Entry) + " " + DeskDay(SimulationManager.DocketLawDay(_playerCountry.Id, item.Entry));
                if (UsVeto.Applies(_playerCountry.Id) && item.Entry.Deadline == System.DateTime.MinValue)
                {
                    // PS-6 US-35 (board 30b, the Docket's row): the ten days counting down - LAW UNSIGNED IN N DAYS, the counted days still to run (Sundays excepted)
                    // the review's M3: TOMORROW on the period's last day, never "in 0 days"
                    line = (mayVeto ? "Sign or veto" : "Sign - it takes no veto") + " · " + UsBillPath.UnsignedWords(_playerCountry.Id, item.PresentedOn, _simulationManager.CurrentDate)
                           + " · " + UnansweredWords(item.Entry) + " " + DeskDay(SimulationManager.DocketLawDay(_playerCountry.Id, item.Entry));
                }
                _docketSlipBook.Anchors["docket:" + key] = new SlipContent("ON YOUR DESK").Add(passage.Title.ToUpperInvariant()).Add("PRESENTED " + DeskDay(item.PresentedOn) + " · " + UnansweredLine(item.Entry));
                if (DrawDocketItem(key, "brief", UiPalette.GetAreaColor(UiPalette.SystemArea.Political), title, line, holdsTime: false, action: "Answer"))
                {
                    DrawDocketBody(() =>
                    {
                        GUILayout.BeginHorizontal();
                        if (PoliSimWidgets.Button("SIGN", _neutralActionButtonStyle)) { AnswerDocketItem(passage, +1); _docketOpenItem = null; }
                        if (mayVeto)
                        {
                            GUILayout.Space(8f);
                            if (PoliSimWidgets.Button("VETO", _neutralActionButtonStyle)) { AnswerDocketItem(passage, -1); _docketOpenItem = null; }
                        }
                        GUILayout.FlexibleSpace();
                        GUILayout.EndHorizontal();
                    });
                }
            }
            return any;
        }

        /// <summary>SP-7b: an answer given on the Docket page - the manager's verb (acted on at once, J6 (i)); false, logged with why, where refused.</summary>
        private bool AnswerDocketItem(DivisionRecord passage, int answer)
        {
            string why;
            bool done = answer > 0 ? _simulationManager.SignDocketBill(_playerCountry.Id, passage, out why) : _simulationManager.VetoDocketBill(_playerCountry.Id, passage, out why);
            if (!done) { Debug.LogWarning($"DOCKET: the answer was not recorded - {why}"); }
            else { QueueNewlyResolvedDivisions(); }   // the review of the rebase, L1: the answer acted at once - its vote on a veto reaches the ceremony on the answer's day
            return done;
        }

        /// <summary>
        /// SP-7b (the plan's §6 step 3: "the verdict's line"; E3: the verdict lands at once on the President row until D-PL builds the night): what the
        /// presidential contest means for the player, in the row's words, and its slip line - before the first vote the candidate he stands as; between the
        /// rounds whether the candidate is in the run-off; after it a win (the oath's day), a loss (the party plays on, L1), or a candidate who did not stand;
        /// in office, the office - except while a re-election's verdict is pending or decided and its term not begun (the review's L5), which it says first.
        /// Null where the player stands no candidate - every Sejm-start game - and once a lost contest's term has begun.
        /// </summary>
        private string PlayerPresidentialWords(System.DateTime today, out string slipLine)
        {
            slipLine = null;
            if (_playerCountry == null || string.IsNullOrEmpty(_playerCountry.PlayerCandidate)) { return null; }
            string LastName(string name) => WorldClock.SurnameOf(name).ToUpperInvariant();
            PresidentialElection.Contest mine = PresidentialElection.PlayerContestOn(_playerCountry, today);
            bool verdictOpen = mine != null && mine.TakesOffice > today;   // a contest the player stood in whose term has not begun - its verdict is the row's news
            if (!verdictOpen && _playerCountry.PlayerHoldsPresidency && PresidentialElection.PlayerHoldsOffice(_playerCountry, today, out string president))
            {
                slipLine = "YOU HOLD THE OFFICE, " + president.ToUpperInvariant() + " · YOU SIGN OR VETO EACH STATUTE THE SEJM PASSES · YOUR PARTY IS THE AI'S WHILE YOU SERVE";
                return "YOU";
            }
            if (mine == null)
            {
                string standing = PresidentialElection.PlayerCandidateFor(_playerCountry, today);
                if (standing == null) { return null; }   // the start's contest is held and over, or the presidency ended (end-i)
                slipLine = "YOU STAND AS " + standing.ToUpperInvariant() + ", BACKED BY " + PartySystems.ShortName(_playerCountry.Id, _playerCountry.PlayerPartyAbbrev).ToUpperInvariant()
                           + " · YOUR VOTE MOVES WITH YOUR PARTY'S STANDING · NO CAMPAIGN IS STAGED";
                return "YOUR CANDIDATE " + LastName(standing);
            }
            if (!verdictOpen) { return null; }   // the term the contest was for has begun and the player does not hold it (lost, or his presidency ended) - the row is the party's again
            if (!mine.PlayerStood())
            {
                slipLine = "YOUR CANDIDATE, " + mine.PlayerCandidate.ToUpperInvariant() + ", DID NOT STAND · YOUR PARTY PLAYS ON";
                return "YOUR CANDIDATE DID NOT STAND";
            }
            if (mine.PlayerWon())
            {
                slipLine = "YOUR CANDIDATE, " + mine.PlayerCandidate.ToUpperInvariant() + ", IS ELECTED · YOU TAKE OFFICE " + DeskDay(mine.TakesOffice) + " · YOUR PARTY IS THE AI'S WHILE YOU SERVE";
                return "YOU WON";
            }
            if (mine.RunOffPending())
            {
                bool inIt = mine.RunOffA == mine.PlayerCandidate || mine.RunOffB == mine.PlayerCandidate;
                slipLine = inIt ? "YOUR CANDIDATE, " + mine.PlayerCandidate.ToUpperInvariant() + ", IS IN THE RUN-OFF OF " + DeskDay(mine.RunOffOn)
                                : "YOUR CANDIDATE, " + mine.PlayerCandidate.ToUpperInvariant() + ", IS OUT IN THE FIRST VOTE · YOUR PARTY PLAYS ON";
                return inIt ? "YOUR CANDIDATE IN THE RUN-OFF" : "YOUR CANDIDATE IS OUT";
            }
            slipLine = mine.Decided() ? "YOUR CANDIDATE, " + mine.PlayerCandidate.ToUpperInvariant() + ", LOST · YOUR PARTY PLAYS ON" : "NOTHING IS DECIDED · YOUR PARTY PLAYS ON";
            return mine.Decided() ? "YOUR CANDIDATE LOST" : null;
        }

        /// <summary>SP-7b: SIGN on the answer plate - the signature recorded on the docket (law at once, J6 (i)), then the seal's beat.</summary>
        private void AnswerSign()
        {
            if (_answering != null)
            {
                if (_simulationManager.SignDocketBill(_playerCountry.Id, _answering, out string why)) { QueueNewlyResolvedDivisions(); }   // L1: anything the answer wrote, queued on its day
                else { Debug.LogWarning($"DOCKET: the signature was not recorded - {why}"); }
            }
            _answering = null;
            _signingScreen?.Sign();
        }

        /// <summary>SP-7b: VETO on the answer plate - the veto recorded on the docket (the Sejm votes on it at once, J6 (i)), then the beat with no seal.</summary>
        private void AnswerVeto()
        {
            if (_answering != null)
            {
                if (_simulationManager.VetoDocketBill(_playerCountry.Id, _answering, out string why)) { QueueNewlyResolvedDivisions(); }   // L1: the Sejm's vote on the veto, queued for its ceremony on the answer's day
                else { Debug.LogWarning($"DOCKET: the veto was not recorded - {why}"); }
            }
            _answering = null;
            _signingScreen?.Veto();
        }

        /// <summary>SP-7b (the review's M1): LATER on the answer plate - nothing recorded; the plate closes on the beat with no seal, and the statute stays held,
        /// answered on the Docket page or signed when its term runs out.</summary>
        private void AnswerLater()
        {
            _answering = null;
            _signingScreen?.Veto();
        }
    }
}
