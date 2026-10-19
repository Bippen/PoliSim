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
        private void ResolveSejmVerdict(ElectionRecord latest, GovernmentFormation.Formed government)
        {
            SejmFormation f = _simulationManager.OpenSejmFormation(_playerCountry, latest.Date);
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
            // A Sejm of record: the cabinet the record's Sejm elected, where its parties hold a majority of tonight's count - SejmFormation.TryRecordCabinet,
            // the rule the procedure's install reads, so the night and the install cannot disagree. DECLARED, put to Elias: a player's party in the record's
            // cabinet is seated in it as on the record, and the night says so.
            if (SejmFormation.TryRecordCabinet(_playerCountry, latest.Date, out System.Collections.Generic.List<string> recordCabinet, out System.Collections.Generic.List<string> recordSupport,
                out string recordPm, out string recordHead, out bool recordCarries, out int recordYes, out int recordMembers) && recordCarries)
            {
                var names = new System.Collections.Generic.List<string>();
                foreach (string key in recordCabinet) { names.Add(PartySystems.ShortName(_playerCountry.Id, key)); }
                string place = mine == recordPm ? "leading it - your party heads the record's cabinet, as on the record"
                    : recordCabinet.Contains(mine) ? "in the cabinet - your party joins the record's cabinet, as on the record"
                    : recordSupport.Contains(mine) ? "supporting it from outside, as on the record"
                    : "in opposition";
                _pendingElectionVerdict = $"{steps} Expected to form: the record's {string.Join("+", names)} government led by {recordHead}, its parties holding {recordYes} of {recordMembers} seats, "
                    + $"with {PartySystems.ShortName(_playerCountry.Id, mine)} {place}.";
            }
            else if (!government.HasGovernment)
            {
                _pendingElectionVerdict = $"{steps} No government is expected to form from this Sejm - {government.Reason ?? "no formation"}. The outgoing cabinet carries on until one is appointed (Art. 162).";
            }
            else
            {
                string standing = government.PlayerInCabinet ? "in the cabinet" : government.PlayerSupports ? "supporting it from outside" : "in opposition";
                _pendingElectionVerdict = $"{steps} Expected to form: a {government.CabinetDescription} government, with {PartySystems.ShortName(_playerCountry.Id, mine)} {standing}."
                    + (government.DeclarationsSourced ? string.Empty : " (Projected on DERIVED red lines only - this country's declared refusals are not sourced.)");   // the general verdict's note (GameController.cs), kept
            }
            _pendingElectionVerdictEndsGame = false;
            Debug.Log($"ROLE: after the election of {latest.Date:yyyy-MM-dd} the Sejm's steps decide who governs; {_playerCountry.Government?.PmParty ?? "none"} governs until the first sitting {(f != null ? f.FirstSitting.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) : "(no procedure)")}");
        }
    }
}
