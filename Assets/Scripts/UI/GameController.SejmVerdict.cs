using System.Globalization;
using PoliSim.Data;
using PoliSim.Elections;
using UnityEngine;

namespace PoliSim.UI
{
    /// <summary>
    /// AF-8 (Elias's ruling q8, 2026-10-18): POLAND'S NIGHT INSTALLS NOTHING. The government changes by the statute's steps - the cabinet resigns at the new
    /// Sejm's first sitting (Konstytucja Art. 162), then Art. 154's - which the manager runs from the day after (`SimulationManager.Sejm.cs`). The verdict
    /// says when the Sejm sits and names the formation's projection as what is EXPECTED TO FORM (q20's words), never as a government in office; leaving
    /// the chamber still ends the run, as everywhere.
    /// </summary>
    public partial class GameController
    {
        private void ResolveSejmVerdict(ElectionRecord latest, GovernmentFormation.Formed government, SejmFormation f)
        {
            // FORK-2: the procedure `f` was opened on the outgoing government by the manager's seating (`SimulationManager.SeatPlayerElection`), which every
            // world that holds the player's election runs
            string mine = _playerCountry.PlayerPartyAbbrev;
            string day = _pendingElectionDate.ToString("d MMMM yyyy", CultureInfo.InvariantCulture);
            if (latest.Seats != null && !string.IsNullOrEmpty(mine) && (!latest.Seats.TryGetValue(mine, out int seats) || seats <= 0))
            {
                _pendingElectionVerdict = $"Out of parliament after the election of {day}: {PartySystems.ShortName(_playerCountry.Id, mine)} won no seat, and a run ends when its party leaves the chamber.";
                _pendingElectionVerdictEndsGame = true;
                return;
            }
            string sitting = f != null ? f.FirstSitting.ToString("d MMMM yyyy", CultureInfo.InvariantCulture) : "its first sitting";
            string steps = $"The Sejm first sits on {sitting}, where the cabinet resigns; the next is appointed by Art. 154's steps.";
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
            if (expected == null)
            {
                // the delta review's L-f: no projection is not a projection of none - the sentence says the outcome cannot be projected tonight
                _pendingElectionVerdict = $"{steps} The outcome of the steps cannot be projected tonight. The outgoing cabinet carries on until a new one is appointed (Art. 162).";
            }
            else if (!expected.Expected)
            {
                _pendingElectionVerdict = $"{steps} No government is expected to form from this Sejm - {expected.Reason ?? "no formation"}. The outgoing cabinet carries on until one is appointed (Art. 162).";
            }
            else if (expected.Head != null)
            {
                // a cabinet of record - the President's designee, or the cabinet the record's Sejm elected - where its parties carry tonight's count
                string place = mine == expected.PmParty ? "leading it - your party heads the record's cabinet, as on the record"
                    : expected.Cabinet.Contains(mine) ? "in the cabinet - your party joins the record's cabinet, as on the record"
                    : expected.Support.Contains(mine) ? "supporting it from outside, as on the record"
                    : "in opposition";
                _pendingElectionVerdict = $"{steps} Expected to form: the record's {expected.CabinetLine()} government led by {expected.Head}, its parties holding {expected.VotesFor} of {expected.Members} seats, "
                    + $"with {PartySystems.ShortName(_playerCountry.Id, mine)} {place}.";
            }
            else
            {
                string standing = expected.Cabinet.Contains(mine) ? "in the cabinet" : expected.Support.Contains(mine) ? "supporting it from outside" : "in opposition";
                _pendingElectionVerdict = $"{steps} Expected to form: a {expected.CabinetLine()} government, with {PartySystems.ShortName(_playerCountry.Id, mine)} {standing}."
                    + (government.DeclarationsSourced ? string.Empty : " (Projected on DERIVED red lines only - this country's declared refusals are not sourced.)");   // the general verdict's note (GameController.cs), kept
            }
            _pendingElectionVerdictEndsGame = false;
            Debug.Log($"ROLE: after the election of {latest.Date:yyyy-MM-dd} the Sejm's steps decide who governs; {_playerCountry.Government?.PmParty ?? "none"} governs until the first sitting {(f != null ? f.FirstSitting.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) : "(no procedure)")}");
        }
    }
}
