using System.Globalization;
using PoliSim.Data;
using PoliSim.Elections;
using UnityEngine;

namespace PoliSim.UI
{
    /// <summary>
    /// PS-7 (Elias's ruling K204): ITALY'S NIGHT INSTALLS NOTHING. The mandates run from the new chambers' first sitting (`SimulationManager.Italy.cs`), each
    /// government to the confidence of both chambers (art. 94); the verdict says when the chambers sit and names the procedure's own projection as what is
    /// EXPECTED TO FORM (q20's words) - or, where three mandates are projected to fail, the dissolution (art. 88) and the polling day art. 61 gives. Leaving
    /// the chamber still ends the run, as everywhere.
    /// </summary>
    public partial class GameController
    {
        private void ResolveItalianVerdict(ElectionRecord latest, GovernmentFormation.Formed government)
        {
            string mine = _playerCountry.PlayerPartyAbbrev;
            string day = _pendingElectionDate.ToString("d MMMM yyyy", CultureInfo.InvariantCulture);
            if (latest.Seats != null && !string.IsNullOrEmpty(mine) && (!latest.Seats.TryGetValue(mine, out int seats) || seats <= 0))
            {
                _pendingElectionVerdict = $"Out of parliament after the election of {day}: {PartySystems.ShortName(_playerCountry.Id, mine)} won no seat, and a run ends when its party leaves the chamber.";
                _pendingElectionVerdictEndsGame = true;
                return;
            }
            string sitting = WorldClock.ItalyFirstSittingAfter(latest.Date).ToString("d MMMM yyyy", CultureInfo.InvariantCulture);
            string steps = $"The Camera and the Senato first sit on {sitting}; the President's mandates follow, each government to the confidence of both chambers (art. 94).";
            // the review's L5 (the Sejm's precedent): a projection that throws degrades the night's sentence, never the election step it is read in
            FormationProjection expected = null;
            try { expected = _simulationManager.ProjectFormation(_playerCountry, latest.Date); }
            catch (System.Exception e) { Debug.LogError($"FORMATION: Italy's projection THREW ({e.GetType().Name}: {e.Message}) - the night names no expected government"); }
            if (expected != null) { Debug.Log("FORMATION: the night projects " + expected.Describe() + " | " + string.Join(" | ", expected.Steps)); }
            if (expected == null)
            {
                _pendingElectionVerdict = $"{steps} The outcome of the mandates cannot be projected tonight. The government in office carries on until a new one wins both chambers' confidence.";
            }
            else if (!expected.Expected)
            {
                _pendingElectionVerdict = $"{steps} No government is expected to win both chambers - {expected.Reason ?? "no formation"}. The government in office carries on as a caretaker.";
            }
            else
            {
                string standing = mine == expected.PmParty ? "leading it" : expected.Cabinet.Contains(mine) ? "in the cabinet" : expected.Support.Contains(mine) ? "supporting it from outside" : "in opposition";
                _pendingElectionVerdict = $"{steps} Expected to form: a {expected.CabinetLine()} government, with {PartySystems.ShortName(_playerCountry.Id, mine)} {standing}."
                    + (government.DeclarationsSourced ? string.Empty : " (Projected on DERIVED red lines only - this country's declared refusals are not sourced.)");   // the general verdict's note, kept
            }
            _pendingElectionVerdictEndsGame = false;
            Debug.Log($"ROLE: after the election of {latest.Date:yyyy-MM-dd} the mandates decide who governs; {_playerCountry.Government?.PmParty ?? "none"} governs until the first sitting {WorldClock.ItalyFirstSittingAfter(latest.Date):yyyy-MM-dd}");
        }
    }
}
