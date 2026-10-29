using System.Collections.Generic;
using System.Globalization;
using PoliSim.Data;
using PoliSim.Elections;
using PoliSim.Simulation;
using UnityEngine;

namespace PoliSim.UI
{
    /// <summary>
    /// D-IT (Design's boards 34a-34d): ITALY'S ELECTION NIGHT, opened from the running game on its polling day, built from the election the manager just held
    /// - the record's shares, counted by the game's own count (`ItalianNight` on `ItalianCount`) - at its final index (the night is built where the count
    /// ends, as every night is). The takeover every night is: the clock holds on it and Continue is its one exit. Nothing is installed: Who governs draws the
    /// procedure's projection (`SimulationManager.ProjectFormation`), and the mandates run from the first sitting (K204).
    /// </summary>
    public partial class GameController
    {
        /// <summary>
        /// The inputs of Italy's night at night index <paramref name="index"/> (0 to `ItalianNight.Total`), read from the game: the count on the record's shares
        /// with each chamber's roster share of the valid vote as the election was counted (an election of record's, `RecordFactors`; one on every other day -
        /// the rule `SimulationManager.RunPlayerElectionNow` counts by), the senators for life of the first sitting, the procedure's projection on the chambers the
        /// count seated, the dissolution's dates as the formation's code gives them, the player's party and whether it sits in the government in office, the
        /// campaign ledger's and the standing budget's one line each. Shared with the film's staged states and the check.
        /// </summary>
        internal static ElectionNightScreen.ItalyNight ItalianNightInputs(SimulationManager sim, Country country, ElectionRecord record, int index, ItalianCount.Chamber chamber = ItalianCount.Chamber.Camera)
        {
            if (record == null || record.Shares == null) { return null; }
            System.DateTime pollingDay = record.Date.Date;
            double cameraRoster = 1.0, senatoRoster = 1.0;
            if (RecordFactors.TryFor(CountryId.Italy, pollingDay, out ElectionVintage factored, out Dictionary<string, double> factors) && factors != null)
            {
                cameraRoster = RecordFactors.RosterShareOfVote(factored);
                if (factored == ElectionVintage.Italy2022) { senatoRoster = ItalianCount.SenatoRosterShare2022; }
            }
            System.DateTime firstSitting = WorldClock.ItalyFirstSittingAfter(pollingDay);
            var night = new ElectionNightScreen.ItalyNight
            {
                State = ItalianNight.At(index, record.Shares, cameraRoster, senatoRoster, firstSitting),
                Chamber = chamber, PollingDay = pollingDay, FirstSitting = firstSitting,
                PlayerParty = country?.PlayerPartyAbbrev,
            };
            if (night.State == null) { return null; }
            if (sim != null && country != null)
            {
                try { night.Projection = sim.ProjectFormation(country, pollingDay); }
                catch (System.Exception e) { Debug.LogError($"FORMATION: Italy's projection THREW on the night ({e.GetType().Name}: {e.Message}) - Who governs draws no projection"); }
                // the mandates the projection is drawn from, each with its votes or none - the one formation code's own list (`ItalianFormation.Run`, as
                // `SimulationManager.ProjectFormation` runs it on the same chambers and the same day's declarations); it reads the chambers and writes nothing
                try { night.Mandates = ItalianFormation.Run(country, DeclarationReading.OfElection(country.Id, pollingDay), out ItalianFormation.Mandate _); }
                catch (System.Exception e) { Debug.LogError($"FORMATION: Italy's mandates THREW on the night ({e.GetType().Name}: {e.Message}) - the track draws the projection's cells alone"); }
            }
            {
                // the dissolution's day as the manager dates it after three failed mandates (its Dissolution: the term's first sitting is this count's, the latest
                // Italian count on the night): on the first sitting, or where the semestre bianco bars it (art. 88 c. 2) the day the bar lifts; then art. 61's
                // polling day - the same two functions, read only. ⚠ The projection does not carry these dates as fields: `FormationProjection.Reason` holds them
                // only inside its sentence, so the night computes them here rather than parse words. The term's first sitting is this record's, which is the
                // manager's latest Italian count only while the night is built from the latest record (the live night always is; a film staged on an older one
                // would read its own record's term)
                night.DissolutionBarred = WorldClock.ItalyDissolutionBarred(firstSitting, firstSitting, out System.DateTime opens);
                night.DissolvedOn = night.DissolutionBarred ? opens : firstSitting;
                night.PollingAfter = ConfidenceProcedure.ItalianElectionDay(night.DissolvedOn);
            }
            if (sim != null && country != null)
            {
                VoteAttribution.Ledger ledger = sim.PlayerCampaignLedger;
                if (ledger != null)
                {
                    double pp = 100.0 * ledger.Deviation;
                    night.LedgerLine = (pp >= 0 ? "+" : "−") + System.Math.Abs(pp).ToString("0.0", CultureInfo.InvariantCulture) + " pp over the campaign";
                }
            }
            if (country?.Government != null && night.PlayerParty != null) { night.PlayerInGovernment = country.Government.Cabinet.Contains(night.PlayerParty); }
            DivisionRecord standing = StandingBudgetDivision(country);
            if (standing != null) { night.BudgetLine = standing.Effects.Count.ToString(CultureInfo.InvariantCulture) + (standing.Effects.Count == 1 ? " outcome estimated" : " outcomes estimated"); }
            return night;
        }

        /// <summary>F1's rule for Italy: the night's own count at the close against the chambers the election seated - the Camera's seats (the record's) and the
        /// Senato's elected and for life (the country's second chamber). The differences, or none; the chambers stand on the count either way, and the night says so
        /// in the log.</summary>
        internal static List<string> ItalianNightDisagrees(ItalianNight.State state, ElectionRecord record, Country country)
        {
            var differ = new List<string>();
            if (state == null || record == null) { return differ; }
            foreach (KeyValuePair<string, int> kv in record.Seats)
            {
                int night = state.Camera.SeatsOf(kv.Key);
                if (night != kv.Value) { differ.Add($"Camera {kv.Key} night {night} chamber {kv.Value}"); }
            }
            if (country?.SecondChamberSeats != null && country.SecondChamberSeats.Count > 0)
            {
                var senato = new Dictionary<string, int>(state.Senato.Result.Seats);
                foreach ((string _, string _, string key) in state.Senato.Life) { senato.TryGetValue(key, out int n); senato[key] = n + 1; }
                foreach (KeyValuePair<string, int> kv in country.SecondChamberSeats)
                {
                    int night = senato.TryGetValue(kv.Key, out int s) ? s : 0;
                    if (night != kv.Value) { differ.Add($"Senato {kv.Key} night {night} chamber {kv.Value}"); }
                }
            }
            return differ;
        }

        /// <summary>
        /// D-IT: the Italian night - the count the election just held, college by college, at its final index. ⚠ F1's rule: at the close the night's seats ARE the
        /// chambers'; the night's own count is the very count the chambers were seated by (`ItalianNightDiagnostic` holds it), and where it ever were not, the
        /// log says so - the chambers stand on the count.
        /// </summary>
        private void ShowItalianNight()
        {
            ElectionRecord latest = _playerCountry.ElectionHistory.Count > 0 ? _playerCountry.ElectionHistory[_playerCountry.ElectionHistory.Count - 1] : null;
            if (latest == null || latest.Date != _simulationManager.CurrentDate || latest.Method != ElectionMethod.ItalyRosatellum)
            {
                Debug.Log($"ELECTION: no election night for Italy - no Rosatellum count was held on {_simulationManager.CurrentDate:yyyy-MM-dd}.");
                return;
            }
            try
            {
                ElectionNightScreen.ItalyNight night = ItalianNightInputs(_simulationManager, _playerCountry, latest, ItalianNight.Total);
                if (night == null) { Debug.Log("ELECTION: no election night for Italy - the count carries no shares."); return; }
                List<string> differ = ItalianNightDisagrees(night.State, latest, _playerCountry);
                if (differ.Count > 0) { Debug.LogError("ELECTION: the Italian night's own count disagrees with the chambers it seated (" + string.Join("; ", differ) + ") - the chambers stand on the count."); }
                foreach (ItalianNight.ChamberNight c in new[] { night.State.Camera, night.State.Senato })
                {
                    // K283: the count is pure and records a list seat it could not place; the night, which reads both chambers' counts, says so
                    if (c.Result.ListSeatsShort != 0) { Debug.LogError($"ELECTION: the Italian {c.Of}'s count left {c.Result.ListSeatsShort} list seat(s) unseated (K283) - {c.Result.Trace}"); }
                }
                PoliSim.Testing.CaptureIdentity.CanvasSurface = "electionnight";
                AudioDirector.Fire(AudioCue.ConstituencyDeclares);   // P4-2: the count is in - built at its final index, so this fires once
                _electionNight = ElectionNightScreen.BuildItaly(night, _pendingElectionVerdict);
                Debug.Log($"ELECTION: the Italian night - Camera {night.State.Camera.Declared} of {night.State.Camera.Colleges} colleges, Senato {night.State.Senato.Declared} of {night.State.Senato.Colleges}; called at {night.State.CalledAt} (Camera {night.State.Camera.CalledAt}, Senato {night.State.Senato.CalledAt}); "
                    + (night.Projection != null ? night.Projection.Describe() : "no projection"));
            }
            catch (System.Exception e)
            {
                Debug.LogError($"CANVAS: the Italian election-night build THREW ({e.GetType().Name}: {e.Message}) - the night is not shown.");
                _electionNight = null;
            }
        }
    }
}
