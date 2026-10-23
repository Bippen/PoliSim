using System.Collections.Generic;
using PoliSim.Data;
using PoliSim.Elections;
using UnityEngine;

namespace PoliSim.UI
{
    /// <summary>
    /// D-PL (Elias, 2026-10-23: "Build the Polish election night"): POLAND'S TWO NIGHTS, opened from the running game. The Sejm's on its polling day,
    /// built from the election the manager just held (the record's shares - the count `PolishSejmAllocation` seated the chamber from); the President's
    /// on each round's day, from the contest the manager's day held (`PresidentialElection.HoldRounds`). Both are the takeover every night is: the clock
    /// holds on it and CONTINUE is its one exit.
    /// </summary>
    public partial class GameController
    {
        /// <summary>A President's night held back because another night was on the board that day (a round on a Sejm polling day): the day it is owed
        /// on, opened once that night is dismissed and the clock would move again (`PresidentialNightOwed`). Cleared by a load.</summary>
        private System.DateTime _presidentialNightOwed = System.DateTime.MinValue;

        /// <summary>D-PL: a President's night owed on the day the board stands at - opened now; true when it opened, so the clock stays held.</summary>
        private bool PresidentialNightOwed()
        {
            if (_presidentialNightOwed == System.DateTime.MinValue) { return false; }
            if (_presidentialNightOwed != _simulationManager.CurrentDate) { _presidentialNightOwed = System.DateTime.MinValue; return false; }
            _presidentialNightOwed = System.DateTime.MinValue;
            CheckPresidentialRound();
            return _electionNight != null;
        }

        /// <summary>
        /// The inputs of Poland's Sejm night beside its count, read from the game: the swing's base (the previous election the game held, else the
        /// chamber the world seated - 2019's by lineage on the 2023 night, LOW CONFIDENCE, §767), the President on the day, the outgoing cabinet's party,
        /// and the record's result where it holds the election (its seats national and per okreg). Shared with the film's staged night.
        /// </summary>
        internal static ElectionNightScreen.SejmNight PolishSejmNightInputs(Country country, PolishSejmNight.State state, System.DateTime pollingDay)
        {
            var night = new ElectionNightScreen.SejmNight { State = state };
            string[] keys = state.Keys;
            ElectionRecord earlier = null;
            if (country?.ElectionHistory != null)
            {
                for (int i = country.ElectionHistory.Count - 1; i >= 0; i--)
                {
                    ElectionRecord e = country.ElectionHistory[i];
                    if (e.Method != ElectionMethod.NotImplemented && e.Date < pollingDay.Date) { earlier = e; break; }
                }
            }
            if (earlier != null)
            {
                night.PreviousShares = new double[keys.Length];
                for (int k = 0; k < keys.Length; k++) { earlier.Shares.TryGetValue(keys[k], out night.PreviousShares[k]); }
                night.PreviousLabel = "VS " + earlier.Date.Year.ToString(System.Globalization.CultureInfo.InvariantCulture);
            }
            else
            {
                // the chamber the world seated - its election's shares as the vote history reads them (the lineage, where the lists changed)
                ElectionVintage seated = WorldClock.SeatedVintage(CountryId.Poland, pollingDay);
                if (PartySystems.TryHistory(CountryId.Poland, out double[] latest, out double[] _, seated))
                {
                    IReadOnlyList<PoliticalParty> roster = PartySystems.For(CountryId.Poland);
                    night.PreviousShares = new double[keys.Length];
                    for (int k = 0; k < keys.Length; k++)
                    {
                        for (int p = 0; p < roster.Count && p < latest.Length; p++) { if (roster[p].Abbrev == keys[k]) { night.PreviousShares[k] = latest[p] / 100.0; } }
                    }
                    night.PreviousLabel = "VS " + WorldClock.ElectionDayOf(CountryId.Poland, seated).Year.ToString(System.Globalization.CultureInfo.InvariantCulture);
                    if (seated == ElectionVintage.Poland2019) { night.LineageNote = PartySystems.HistoryNote(CountryId.Poland); }   // §767 (D1): 2019's lists read by lineage
                }
            }

            if (country != null && PresidentialElection.PresidentAt(CountryId.Poland, country.PresidentialElections, pollingDay, out PresidencyOfRecord.President president))
            {
                night.PresidentName = president.Name;
                night.PresidentParty = president.BackingParty;
            }
            night.GovernmentParty = country?.Government?.PmParty;

            if (WorldClock.TryReference(CountryId.Poland, pollingDay, out WorldClock.Reference history))
            {
                var realSeats = new int[keys.Length];
                for (int k = 0; k < keys.Length; k++) { history.Seats.TryGetValue(keys[k], out realSeats[k]); }
                night.Reference = new ElectionNightScreen.Reference
                {
                    Label = history.Label, Seats = realSeats, GovernmentLine = history.GovernmentLine,
                    HeadParty = history.HeadParty, HeadSurname = history.HeadSurname, HeadQualifier = history.HeadQualifier, HeadFrom = history.HeadFrom,
                    HeadIsCaretaker = history.HeadIsCaretaker, RecordDate = WorldClock.RecordDate, HistoryNote = history.HistoryNote,
                };
                if (history.Vintage == ElectionVintage.Poland2023)
                {
                    // the record's own count per okreg: 2023's district votes through the night's own division - the chamber it returns is the PKW's (§730)
                    PolishSejmNight.State record = PolishSejmNight.At(PolishSejmNight.FinalMinute, PolishSejmNight.Record2023Shares());
                    night.ReferenceByOkreg = new int[record.Okregi.Length][];
                    for (int d = 0; d < record.Okregi.Length; d++)
                    {
                        night.ReferenceByOkreg[d] = new int[keys.Length];
                        for (int k = 0; k < keys.Length; k++)
                        {
                            int r = System.Array.IndexOf(record.Keys, keys[k]);
                            if (r >= 0) { night.ReferenceByOkreg[d][k] = record.Okregi[d].Certain[r]; }
                        }
                    }
                }
            }
            return night;
        }

        /// <summary>
        /// D-PL: the Sejm's night - the count the election just held, okreg by okreg, at its final minute (the night is built where the count ends, as every
        /// night is). ⚠ F1's rule: at 41 of 41 the night's seats ARE the chamber's; the night's own division reproduces it (`PolishNightDiagnostic`), and
        /// where it ever did not, the chamber's seats stand on the count and the log says so - the screen and the result never disagree.
        /// </summary>
        private void ShowPolishSejmNight()
        {
            ElectionRecord latest = _playerCountry.ElectionHistory.Count > 0 ? _playerCountry.ElectionHistory[_playerCountry.ElectionHistory.Count - 1] : null;
            if (latest == null || latest.Date != _simulationManager.CurrentDate || latest.Method != ElectionMethod.PolandDistricts)
            {
                Debug.Log($"ELECTION: no election night for Poland - no Sejm count was held on {_simulationManager.CurrentDate:yyyy-MM-dd}.");
                return;
            }
            try
            {
                PolishSejmNight.State state = PolishSejmNight.At(PolishSejmNight.FinalMinute, latest.Shares);
                if (state == null) { Debug.Log("ELECTION: no election night for Poland - the count carries no shares."); return; }
                var differ = new List<string>();
                for (int k = 0; k < state.Keys.Length; k++)
                {
                    int seated = latest.Seats.TryGetValue(state.Keys[k], out int s) ? s : 0;
                    if (seated != state.Seats[k]) { differ.Add($"{state.Keys[k]} night {state.Seats[k]} chamber {seated}"); state.Seats[k] = seated; }
                }
                if (differ.Count > 0) { Debug.LogError("ELECTION: the Polish night's own count disagrees with the chamber it seated (" + string.Join("; ", differ) + ") - the chamber's seats stand on the count."); }
                System.DateTime pollingDay = _simulationManager.CurrentDate;
                ElectionNightScreen.SejmNight night = PolishSejmNightInputs(_playerCountry, state, pollingDay);
                PoliSim.Testing.CaptureIdentity.CanvasSurface = "electionnight";
                AudioDirector.Fire(AudioCue.ConstituencyDeclares);   // P4-2: the count is in - built at its final minute, so this fires once
                _electionNight = ElectionNightScreen.BuildPoland(night, pollingDay.AddHours(21), _pendingElectionVerdict);   // board 28a's clock: the polls close at 21:00
                Debug.Log($"ELECTION: the Polish night - {state.Declared} of {state.Total} okręgi, " + string.Join(" · ", SeatsLine(state)) + (night.Reference != null ? $"; the reference {night.Reference.Label}" : "; no reference"));
            }
            catch (System.Exception e)
            {
                Debug.LogError($"CANVAS: the Polish election-night build THREW ({e.GetType().Name}: {e.Message}) - the night is not shown.");
                _electionNight = null;
            }
        }

        private static List<string> SeatsLine(PolishSejmNight.State state)
        {
            var parts = new List<string>();
            for (int k = 0; k < state.Keys.Length; k++) { parts.Add(state.Keys[k] + " " + state.Seats[k].ToString(System.Globalization.CultureInfo.InvariantCulture)); }
            return parts;
        }

        /// <summary>
        /// D-PL (28c-28d): the President's night, on the day a round of the game's presidential election was held (the manager's day held it before
        /// this runs): the first vote's night, or the run-off's - elected where the run-off decided it. The day loop visits a day once, so a round's night
        /// opens once; where another night holds the board that day it is OWED and opens when that one is dismissed - never dropped.
        /// </summary>
        private void CheckPresidentialRound()
        {
            if (PlayerCountryId != CountryId.Poland || _playerCountry?.PresidentialElections == null) { return; }
            System.DateTime today = _simulationManager.CurrentDate;
            PresidentialElection.Contest held = null;
            ElectionNightScreen.PresidentStage stage = ElectionNightScreen.PresidentStage.FirstRound;
            foreach (PresidentialElection.Contest c in _playerCountry.PresidentialElections)
            {
                if (c.RunOffHeld && c.RunOffOn == today) { held = c; stage = c.Decided() ? ElectionNightScreen.PresidentStage.Elected : ElectionNightScreen.PresidentStage.RunOff; }
                else if (c.FirstVote == today) { held = c; stage = c.Decided() ? ElectionNightScreen.PresidentStage.Elected : ElectionNightScreen.PresidentStage.FirstRound; }
            }
            if (held == null) { return; }
            if (_electionNight != null) { _presidentialNightOwed = today; Debug.Log($"ELECTION: the President's night of {today:yyyy-MM-dd} waits for the night on the board"); return; }
            try
            {
                string sitting = PresidentialElection.PresidentAt(CountryId.Poland, _playerCountry.PresidentialElections, today, out PresidencyOfRecord.President president) ? president.Name : null;
                PoliSim.Testing.CaptureIdentity.CanvasSurface = "electionnight";
                _electionNight = ElectionNightScreen.BuildPresident(held, stage, sitting, today.AddHours(21), null);   // board 28c's clock: the polls close at 21:00
                Debug.Log($"ELECTION: the President's night of {today:yyyy-MM-dd} ({stage}) - {held.Line}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"CANVAS: the presidential night build THREW ({e.GetType().Name}: {e.Message}) - the night is not shown.");
                _electionNight = null;
            }
        }
    }
}
