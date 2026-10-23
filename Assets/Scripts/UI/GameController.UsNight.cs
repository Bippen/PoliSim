using System.Collections.Generic;
using System.Globalization;
using PoliSim.Data;
using PoliSim.Elections;
using UnityEngine;

namespace PoliSim.UI
{
    /// <summary>
    /// PS-6 US-34 (Elias, 2026-10-23: build US-34 from the D-US boards): THE USA'S NIGHT, opened from the running game on its polling day from the contests
    /// the day loop held that day (`SimulationManager.HoldUsPresidentialCount` - the presidential count and the Congress count, or the midterm's Congress
    /// count alone), built where the count ends (51 of 51), as every night is; and the House's own moment on the count's day where the College had no
    /// majority. Both are the takeover every night is: the clock holds on it and CONTINUE is its one exit.
    /// </summary>
    public partial class GameController
    {
        /// <summary>
        /// The inputs of the US night beside its count, read from the game: the sitting President, Vice President and Congress on the polling day; the
        /// persons on the two tickets and the national vote the count read; the dates to the oath; the House sitting on the day by state (each slip's ±);
        /// the comparison base (the record's states on the first night, the game's own previous count after); and history as the reference where the record
        /// holds the election. Shared with the film's staged night.
        /// </summary>
        internal static ElectionNightScreen.UsNightBoard UsNightInputs(Country country, UsNight.State state, UsPresidentialElection.Contest president, UsCongressElection.Contest congress)
        {
            System.DateTime day = state.PollingDay;
            var board = new ElectionNightScreen.UsNightBoard { State = state, Convenes = congress.Convenes, ElectorsMeet = System.DateTime.MinValue, CountedOn = System.DateTime.MinValue, TakesOffice = System.DateTime.MinValue };
            if (UsTermLimit.TryPresidentOn(country, day, out string person, out string party, out string _)) { board.President = person; board.PresidentParty = party; }
            if (UsTermLimit.TryVicePresidentOn(country, day, out string vice, out string viceParty, out string _)) { board.VicePresident = vice; board.VicePresidentParty = viceParty; }
            board.SittingCongress = UsSenate.CongressOn(day);
            if (board.SittingCongress == 0)
            {
                // beyond the record's numbering: the Congress that convened on the 3rd of January of the latest odd year - the 1st's in 1789 (CONVENTION, the
                // Twentieth Amendment's day and the Congress's own count)
                int convened = day.Year % 2 == 1 && day >= new System.DateTime(day.Year, 1, 3) ? day.Year : day.Year % 2 == 1 ? day.Year - 2 : day.Year - 1;
                board.SittingCongress = (convened - 1789) / 2 + 1;
            }
            board.AllStates = UsPresidentialElection.AllStates(day);
            List<(string State, int Republicans, int Democrats)> sitting = UsPresidentialElection.DelegationsOn(country, day, out string _);
            if (sitting != null)
            {
                board.SittingHouse = new Dictionary<string, (int, int)>();
                foreach ((string st, int r, int d) in sitting) { board.SittingHouse[st] = (r, d); }
            }
            if (!state.Presidential)
            {
                board.HouseV = congress.V;
                // the review's M1: the sitting term runs to the oath after the next presidential count (3 U.S.C. §21(1)'s day, the Twentieth Amendment's 20 January)
                board.SittingUntil = UsPresidentialElection.TryNextPresidentialDay(CountryId.USA, day.AddDays(1), out System.DateTime next) ? UsPresidentialElection.OathDayAfter(next) : System.DateTime.MinValue;
            }
            if (president != null)
            {
                board.ElectorsMeet = president.ElectorsMeet;
                board.CountedOn = president.CountedOn;
                board.TakesOffice = president.TakesOffice;
                board.SittingUntil = president.TakesOffice;
                foreach (UsPresidentialElection.Tally t in president.Electors)
                {
                    if (t.Party == "REP") { board.RepNominee = UsTermLimit.PersonOf(president, t); board.RepRunningMate = UsTermLimit.RunningMateOf(country, president, "REP"); board.RepShare = t.Share; }
                    else if (t.Party == "DEM") { board.DemNominee = UsTermLimit.PersonOf(president, t); board.DemRunningMate = UsTermLimit.RunningMateOf(country, president, "DEM"); board.DemShare = t.Share; }
                    else if (t.Candidate == UsPresidentialElection.OtherCandidates) { board.OtherShare = t.Share; }
                }
                // the base each slip compares with at 51 of 51: the game's own previous count, else the record's election of the same year
                UsPresidentialElection.Contest previous = UsPresidentialElection.PreviousCount(country, day);
                ElectoralCollege.StateCall[] before = previous != null ? UsPresidentialElection.StatesOf(country, previous) : null;
                if (before != null)
                {
                    board.BaseRepTwoParty = new Dictionary<string, double>();
                    foreach (ElectoralCollege.StateCall c in before)
                    {
                        double two = c.Shares[ElectoralCollege.ShareRepublican] + c.Shares[ElectoralCollege.ShareDemocrat];
                        if (two > 0.0) { board.BaseRepTwoParty[c.State] = c.Shares[ElectoralCollege.ShareRepublican] / two; }
                    }
                    board.BaseLabel = "The game's " + previous.PollingDay.Year.ToString(CultureInfo.InvariantCulture);
                }
                else
                {
                    var record = new Dictionary<string, double>();
                    foreach (var s in PoliSim.Elections.Generated.UsPresidentialReturns.States)
                    {
                        if (s.Year == day.Year && s.VotesR + s.VotesD > 0) { record[s.State] = (double)s.VotesR / (s.VotesR + s.VotesD); }
                    }
                    if (record.Count > 0) { board.BaseRepTwoParty = record; board.BaseLabel = "Record " + day.Year.ToString(CultureInfo.InvariantCulture); }
                }
            }
            if (WorldClock.TryReference(CountryId.USA, day, out WorldClock.Reference history))
            {
                board.HasReference = true;
                board.ReferenceLabel = history.Label;
                if (history.Electors != null)
                {
                    history.Electors.TryGetValue("REP", out board.RealElectorsRep);
                    history.Electors.TryGetValue("DEM", out board.RealElectorsDem);
                }
                if (history.Seats != null)
                {
                    history.Seats.TryGetValue("REP", out board.RealHouseRep);
                    history.Seats.TryGetValue("DEM", out board.RealHouseDem);
                }
                if (history.SecondChamber != null && history.SecondChamber.TryGetValue("REP", out int senateRep))
                {
                    board.RealSenateRep = senateRep;
                    board.RealSenateDem = history.SecondChamber.TryGetValue("DEM", out int senateDem) ? senateDem : 0;
                }
                board.RecordWinners = new Dictionary<string, string>();
                foreach (var s in PoliSim.Elections.Generated.UsPresidentialReturns.States) { if (s.Year == day.Year) { board.RecordWinners[s.State] = s.VotesR > s.VotesD ? "REP" : "DEM"; } }
                foreach (var d in PoliSim.Elections.Generated.UsPresidentialReturns.Districts)
                {
                    if (d.Year == day.Year) { board.RecordWinners[d.State + "-" + d.District.ToString(CultureInfo.InvariantCulture)] = d.VotesR > d.VotesD ? "REP" : "DEM"; }
                }
            }
            return board;
        }

        /// <summary>
        /// US-34: the USA's night - the contests the day loop held today, the night at its last declaration. ⚠ F1's rule: at 51 of 51 the night's College,
        /// House and Senate ARE the count's (`UsNightDiagnostic` holds the night to them); where they ever differed the log says so loudly and the night
        /// shows the night's own sums - the counts the slips add up to.
        /// </summary>
        private void ShowUsNight()
        {
            System.DateTime today = _simulationManager.CurrentDate;
            UsCongressElection.Contest congress = _playerCountry?.UsCongressElections?.Find(c => c.PollingDay.Date == today.Date);
            if (congress == null)
            {
                Debug.Log($"ELECTION: no election night for the USA - no Congress count was held on {today:yyyy-MM-dd} (the CONGRESS: line says why).");
                return;
            }
            UsPresidentialElection.Contest president = _playerCountry.UsPresidentialElections?.Find(c => c.PollingDay.Date == today.Date);
            if (president == null && UsPresidentialElection.IsPresidentialYear(today.Year))
            {
                Debug.Log($"ELECTION: no election night for the USA - no presidential count was held on {today:yyyy-MM-dd} (the PRESIDENTIAL: line says why).");
                return;
            }
            try
            {
                UsNight.State state = UsNight.At(int.MaxValue, _playerCountry, president, congress);
                if (state == null) { Debug.Log("ELECTION: no election night for the USA - the count's states could not be re-derived."); return; }
                var differ = new List<string>();
                if (president != null && (state.ElectorsRep != president.ElectorsOf("REP") || state.ElectorsDem != president.ElectorsOf("DEM"))) { differ.Add($"the College night {state.ElectorsRep}-{state.ElectorsDem} count {president.ElectorsOf("REP")}-{president.ElectorsOf("DEM")}"); }
                if (state.HouseRep != congress.HouseRep || state.HouseDem != congress.HouseDem) { differ.Add($"the House night {state.HouseRep}-{state.HouseDem} count {congress.HouseRep}-{congress.HouseDem}"); }
                Dictionary<string, int> sits = UsCongressElection.SenateOn(_playerCountry, congress.Convenes);   // the review's L2: the Senate too, against the one that sits on 3 January
                int sitsRep = sits.TryGetValue("REP", out int sr) ? sr : 0, sitsDem = sits.TryGetValue("DEM", out int sd) ? sd : 0;
                if (state.SenateRep != sitsRep || state.SenateDem != sitsDem) { differ.Add($"the Senate night {state.SenateRep}-{state.SenateDem} the Senate that sits {sitsRep}-{sitsDem}"); }
                if (differ.Count > 0) { Debug.LogError("ELECTION: the US night's own sums disagree with the count the day loop held (" + string.Join("; ", differ) + ")."); }
                ElectionNightScreen.UsNightBoard board = UsNightInputs(_playerCountry, state, president, congress);
                PoliSim.Testing.CaptureIdentity.CanvasSurface = "electionnight";
                AudioDirector.Fire(AudioCue.ConstituencyDeclares);   // P4-2: the count is in - built at its last declaration, so this fires once
                _electionNight = ElectionNightScreen.BuildUsa(board, _pendingElectionVerdict);
                Debug.Log(string.Format(CultureInfo.InvariantCulture,
                    "ELECTION: the US night - {0} of {1} declared; {2}the House REP {3} DEM {4} ({5} at {6}); the Senate REP {7} DEM {8} of {9} ({10} at {11}){12}",
                    state.Declared, state.Total,
                    state.Presidential ? string.Format(CultureInfo.InvariantCulture, "the College REP {0} DEM {1} ({2} at {3}); ", state.ElectorsRep, state.ElectorsDem, state.College, state.CollegeAt) : "a midterm, no College; ",
                    state.HouseRep, state.HouseDem, state.House, state.HouseAt, state.SenateRep, state.SenateDem, state.SenateSeats, state.Senate, state.SenateAt,
                    board.HasReference ? "; the reference " + board.ReferenceLabel : "; no reference"));
            }
            catch (System.Exception e)
            {
                Debug.LogError($"CANVAS: the US election-night build THREW ({e.GetType().Name}: {e.Message}) - the night is not shown.");
                _electionNight = null;
            }
        }

        /// <summary>
        /// US-34 (29b's answer 6): on the count's day, where the College had no majority, the House chose by states on the day loop
        /// (`UsPresidentialElection.HoldHouseChoice`) - its own moment opens now, a takeover; where another night is on the board that day it is OWED and opens
        /// once that night is dismissed and the clock would move again (`UsHouseChoiceOwed`, the review's L3 - Poland's President's night is the precedent).
        /// </summary>
        /// <summary>The day the House's moment was owed because another night held the board; cleared by a load.</summary>
        private System.DateTime _usHouseChoiceOwed = System.DateTime.MinValue;

        /// <summary>The House's moment owed on the day the board stands at - opened now; true when it opened, so the clock stays held.</summary>
        private bool UsHouseChoiceOwed()
        {
            if (_usHouseChoiceOwed == System.DateTime.MinValue) { return false; }
            System.DateTime owed = _usHouseChoiceOwed;
            _usHouseChoiceOwed = System.DateTime.MinValue;
            if (owed != _simulationManager.CurrentDate || _electionNight != null) { if (_electionNight != null) { _usHouseChoiceOwed = owed; } return false; }
            CheckUsHouseChoice();
            return _electionNight != null;
        }

        private void CheckUsHouseChoice()
        {
            if (PlayerCountryId != CountryId.USA || _playerCountry?.UsPresidentialElections == null) { return; }
            System.DateTime today = _simulationManager.CurrentDate;
            UsPresidentialElection.Contest chosen = _playerCountry.UsPresidentialElections.Find(c => c.Contingent && c.CountedOn.Date == today.Date && c.Delegations.Count > 0);
            if (chosen == null) { return; }
            if (_electionNight != null) { _usHouseChoiceOwed = today; Debug.Log($"ELECTION: the House's choice of {today:yyyy-MM-dd} waits for the night on the board"); return; }
            try
            {
                PoliSim.Testing.CaptureIdentity.CanvasSurface = "electionnight";
                _electionNight = ElectionNightScreen.BuildUsHouseChoice(chosen, UsPresidentialElection.AllStates(chosen.PollingDay), null);
                Debug.Log($"ELECTION: the House's choice of {today:yyyy-MM-dd} - {(chosen.Decided() ? chosen.Winner + " chosen" : "no one chosen")}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"CANVAS: the House's choice build THREW ({e.GetType().Name}: {e.Message}) - it is not shown.");
                _electionNight = null;
            }
        }
    }
}
