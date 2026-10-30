using System.Collections.Generic;
using System.Globalization;
using PoliSim.Data;
using PoliSim.Elections;
using UnityEngine;

namespace PoliSim.UI
{
    /// <summary>
    /// AF-8 (Elias's ruling q8, 2026-10-18): POLAND'S NIGHT INSTALLS NOTHING. The government changes by the statute's steps - the cabinet resigns at the new
    /// Sejm's first sitting (Konstytucja Art. 162), then Art. 154's and, failing them, Art. 155's (K302) - which the manager runs from the day after (`SimulationManager.Sejm.cs`). The verdict
    /// names the formation's projection as what is EXPECTED TO FORM (q20's words), never as a government in office, and says when the Sejm sits; leaving
    /// the chamber still ends the run, as everywhere.
    /// <para>S11 (Design's return of 2026-10-30, board 11b): <b>THE OUTCOME FIRST</b>. The verdict is built as clauses - the outcome, the figure that decided it, the
    /// date (or, where none is expected, the snap election and the caretaker) - which the night's foot drops whole from the end to its room and to K320's cap
    /// (<see cref="SejmFoot"/>); the whole sentence, the outcome and then the procedure as first built, is the foot's slip and `_pendingElectionVerdict`. The steps
    /// themselves are <see cref="SejmNightSteps"/>'s, read against the projection, for WHO GOVERNS.</para>
    /// </summary>
    public partial class GameController
    {
        /// <summary>S11: the night's steps and foot, resolved with the verdict and read by the Polish night's build (`ShowPolishSejmNight`); the screen's alone, not
        /// saved - a game loaded before its night is dismissed keeps the verdict's sentence, as every night does.</summary>
        private SejmNightSteps _pendingSejmSteps;
        private SejmFoot _pendingSejmFoot;

        private void ResolveSejmVerdict(ElectionRecord latest, GovernmentFormation.Formed government, SejmFormation f)
        {
            // FORK-2: the procedure `f` was opened on the outgoing government by the manager's seating (`SimulationManager.SeatPlayerElection`), which every
            // world that holds the player's election runs
            _pendingSejmSteps = null;
            _pendingSejmFoot = null;
            string mine = _playerCountry.PlayerPartyAbbrev;
            string day = _pendingElectionDate.ToString("d MMMM yyyy", CultureInfo.InvariantCulture);
            if (latest.Seats != null && !string.IsNullOrEmpty(mine) && (!latest.Seats.TryGetValue(mine, out int seats) || seats <= 0))
            {
                string shortName = PartySystems.ShortName(_playerCountry.Id, mine);
                _pendingElectionVerdict = $"Out of parliament after the election of {day}: {shortName} won no seat, and a run ends when its party leaves the chamber.";
                _pendingElectionVerdictEndsGame = true;
                _pendingSejmFoot = new SejmFoot { Carried = false, Whole = _pendingElectionVerdict };
                _pendingSejmFoot.Clauses.Add("Out of parliament · no seat for " + shortName);
                _pendingSejmFoot.Clauses.Add("end of the run");
                return;
            }
            System.DateTime firstSitting = f != null ? f.FirstSitting : SejmFormation.FirstSittingAfter(_playerCountry, latest.Date);
            string sitting = firstSitting.ToString("d MMMM yyyy", CultureInfo.InvariantCulture);
            string steps = $"The Sejm first sits on {sitting}, where the cabinet resigns; the next is appointed by Art. 154's steps, or failing them Art. 155's.";   // K302
            string sitsClause = "first sitting " + firstSitting.ToString("d MMM yyyy", CultureInfo.InvariantCulture);
            // GATE-18 q20 (Elias's ruling of 2026-10-18: "one formation code"): what the steps are expected to form is the steps' own projection
            // (`SimulationManager.ProjectFormation`, in the order `SejmStep` runs them: the President's designee and the Sejm's confidence, then the Sejm's own
            // election - the record's cabinet where it carries tonight's count, `SejmFormation.TryRecordCabinet`, else the formation's projection), so the
            // night and the install cannot disagree. DECLARED, put to Elias: a player's party in the record's cabinet is seated in it as on the record, and the
            // night says so.
            // the review's L5: a projection that throws degrades the night's sentence (no government named), never the election step it is read in
            FormationProjection expected = null;
            try { expected = _simulationManager.ProjectFormation(_playerCountry, latest.Date); }
            catch (System.Exception e) { Debug.LogError($"FORMATION: the Sejm's projection THREW ({e.GetType().Name}: {e.Message}) - the night names no expected government"); }
            if (expected != null) { Debug.Log("FORMATION: the night projects " + expected.Describe() + " | " + string.Join(" | ", expected.Steps)); }
            SejmNightSteps read = SejmNightSteps.Read(_playerCountry, latest.Date, expected);
            _pendingSejmSteps = expected != null ? read : null;   // no projection is not a projection of none: WHO GOVERNS says "No projection tonight"
            var foot = new SejmFoot();
            string outcome;
            if (expected == null)
            {
                // the delta review's L-f: no projection is not a projection of none - the sentence says the outcome cannot be projected tonight
                outcome = "The outcome of the steps cannot be projected tonight. The outgoing cabinet carries on until a new one is appointed (Art. 162).";
                foot.Clauses.Add("No projection tonight");
                foot.Clauses.Add(sitsClause);
            }
            else if (!expected.Expected)
            {
                // K302: the Sejm's projection forms none only where Art. 155's vote is refused - the President's cabinet then carries on as caretaker until the new Sejm's
                outcome = $"No government is expected to form from this Sejm - {expected.Reason ?? "no formation"}. A caretaker carries on until the new Sejm's cabinet is appointed (Art. 162).";
                foot.Carried = false;
                foot.Clauses.Add("No government expected");
                if (read.SnapPollingDay != System.DateTime.MinValue) { foot.Clauses.Add("snap election by " + read.SnapPollingDay.ToString("d MMM yyyy", CultureInfo.InvariantCulture)); }
                if (!string.IsNullOrEmpty(read.CaretakerParty)) { foot.Clauses.Add(PartySystems.ShortName(_playerCountry.Id, read.CaretakerParty) + " caretaker"); }
            }
            else
            {
                if (expected.Head != null)
                {
                    // a cabinet of record - the President's designee, or the cabinet the record's Sejm elected - where its parties carry tonight's count
                    string place = mine == expected.PmParty ? "leading it - your party heads the record's cabinet, as on the record"
                        : expected.Cabinet.Contains(mine) ? "in the cabinet - your party joins the record's cabinet, as on the record"
                        : expected.Support.Contains(mine) ? "supporting it from outside, as on the record"
                        : "in opposition";
                    outcome = $"Expected to form: the record's {expected.CabinetLine()} government led by {expected.Head}, its parties holding {expected.VotesFor} of {expected.Members} seats, "
                        + $"with {PartySystems.ShortName(_playerCountry.Id, mine)} {place}.";
                }
                else
                {
                    string standing = expected.Cabinet.Contains(mine) ? "in the cabinet" : expected.Support.Contains(mine) ? "supporting it from outside" : "in opposition";
                    outcome = $"Expected to form: a {expected.CabinetLine()} government, with {PartySystems.ShortName(_playerCountry.Id, mine)} {standing}."
                        + (government.DeclarationsSourced ? string.Empty : " (Projected on DERIVED red lines only - this country's declared refusals are not sourced.)");   // the general verdict's note (GameController.cs), kept
                }
                foot.Carried = true;
                foot.Clauses.Add(ExpectedClause(expected.Cabinet, read.EndStep));
                SejmNightSteps.Step end = read.Steps[read.EndStep - 1];
                if (end.Voted && end.Members > 0)
                {
                    foot.Clauses.Add(end.SimpleMajority
                        ? (end.Against >= 0 ? end.For.ToString(CultureInfo.InvariantCulture) + " to " + end.Against.ToString(CultureInfo.InvariantCulture) : end.For.ToString(CultureInfo.InvariantCulture) + " for")
                        : end.For.ToString(CultureInfo.InvariantCulture) + " for, " + SejmNightSteps.Needed(end.Members).ToString(CultureInfo.InvariantCulture) + " needed");
                }
                foot.Clauses.Add(sitsClause);
            }
            _pendingElectionVerdict = outcome + " " + steps;
            foot.Whole = _pendingElectionVerdict;
            _pendingSejmFoot = foot;
            _pendingElectionVerdictEndsGame = false;
            Debug.Log($"ROLE: after the election of {latest.Date:yyyy-MM-dd} the Sejm's steps decide who governs; {_playerCountry.Government?.PmParty ?? "none"} governs until the first sitting {(f != null ? f.FirstSitting.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) : "(no procedure)")}");
            Debug.Log("CANVAS: SEJM NIGHT - the Sejm night's foot: " + string.Join(" · ", foot.Clauses) + (read.Partial ? " (the steps drawn from the projection's end alone)" : string.Empty));
        }

        /// <summary>S11 (11b), the outcome's clause under K320's cap: the expected cabinet in short names (a lone party "alone") and the step it comes from; a cabinet
        /// too long for the cap is folded to its first party and "+N" (the slip and WHO GOVERNS name every party).</summary>
        internal static string ExpectedClause(IReadOnlyList<string> cabinet, int endStep)
        {
            string stepWords = endStep == 1 ? "President's step" : endStep == 2 ? "Sejm's step" : "second step";
            string names = cabinet.Count == 1 ? PartySystems.ShortName(CountryId.Poland, cabinet[0]) + " alone" : SejmNightSteps.Names(cabinet);
            string clause = "Expected: " + names + ", " + stepWords;
            if (SejmNightSteps.CountedWords(clause) > SejmNightSteps.RestWords && cabinet.Count > 1)
            {
                clause = "Expected: " + PartySystems.ShortName(CountryId.Poland, cabinet[0]) + " +" + (cabinet.Count - 1).ToString(CultureInfo.InvariantCulture) + ", " + stepWords;
            }
            return clause;
        }
    }
}
