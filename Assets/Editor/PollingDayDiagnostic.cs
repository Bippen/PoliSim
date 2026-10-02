using System;
using System.Globalization;
using System.Text;
using PoliSim.Data;
using PoliSim.Elections;
using PoliSim.Simulation;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// PS-2 / CL-4 (2026-09-25, §619): POLLING DAY ON THE CALENDAR'S DATE, ASSERTED. Sweden's ordinary election falls on the statute's day
    /// (every fourth year, the second Sunday of September - regeringsformen 3 kap. 3 §, vallagen 1 kap. 3 §; `ElectionsData/sweden/election_calendar.md`):
    /// the rule reproduces the record's own polling days (2018-09-09, 2022-09-11, 2026-09-13) and gives 2030-09-08 next; the next polling day from
    /// any date is the right one; Germany's snap (§697) and Poland's 2023 day (§762) are on record with nothing after them, and the countries before
    /// their stages offer none (their chambers hold as of record, §618's ruling 4); an election on a polling
    /// day reads that election's declarations; the reference for 13 September 2026 is K-1's sourced result and for 2030 there is none. Then the
    /// game's own path: a manager opened on Sweden's start reads the run-up's first day at once, its run-up begins, its campaign runs up to polling
    /// day, and `PollingDayToday` is raised on 2026-09-13 - the 238th day - and on no other day of the first year. The epoch is restored afterwards.
    /// </summary>
    public static class PollingDayDiagnostic
    {
        private const int Seed = 777;

        public static void Run()
        {
            CheckExit.ArmLogFold();
            var sb = new StringBuilder();
            int failures = 0;
            sb.Append("=== PollingDayDiagnostic (PS-2 / CL-4, §619): the election on the calendar's date ===\n");
            void Check(bool ok, string what)
            {
                if (!ok) { failures++; }
                sb.Append(ok ? "    ok        " : "    FAIL      ").Append(what).Append('\n');
            }

            using System.IDisposable epoch = SimulationManager.EpochScope();
            try
            {
                // 1. The statute's rule against the record's own polling days.
                Check(WorldClock.SecondSundayOfSeptember(2018) == WorldClock.ElectionDayOf(CountryId.Sweden, ElectionVintage.Sweden2018), F("2018: the rule gives {0:yyyy-MM-dd}, the record {1:yyyy-MM-dd}", WorldClock.SecondSundayOfSeptember(2018), WorldClock.ElectionDayOf(CountryId.Sweden, ElectionVintage.Sweden2018)));
                Check(WorldClock.SecondSundayOfSeptember(2022) == WorldClock.ElectionDayOf(CountryId.Sweden, ElectionVintage.Sweden2022), F("2022: the rule gives {0:yyyy-MM-dd}, the record {1:yyyy-MM-dd}", WorldClock.SecondSundayOfSeptember(2022), WorldClock.ElectionDayOf(CountryId.Sweden, ElectionVintage.Sweden2022)));
                Check(WorldClock.SecondSundayOfSeptember(2026) == WorldClock.LatestElectionDay(CountryId.Sweden), F("2026: the rule gives {0:yyyy-MM-dd}, the record {1:yyyy-MM-dd}", WorldClock.SecondSundayOfSeptember(2026), WorldClock.LatestElectionDay(CountryId.Sweden)));
                Check(WorldClock.SecondSundayOfSeptember(2030) == new DateTime(2030, 9, 8), F("2030: the rule gives {0:yyyy-MM-dd} (1 September 2030 is a Sunday, so the second is the 8th)", WorldClock.SecondSundayOfSeptember(2030)));
                Check(WorldClock.PollingDayBasis(CountryId.Sweden) != null && WorldClock.PollingDayBasis(CountryId.Sweden).Contains("[RF-3-3]") && WorldClock.PollingDayBasis(CountryId.Sweden).Contains("[VL-1-3]"), "the basis cites the two statutes by their record ids");

                // 2. The next polling day from a date.
                Next(CountryId.Sweden, new DateTime(2026, 1, 18), new DateTime(2026, 9, 13), "Sweden's start");
                Next(CountryId.Sweden, new DateTime(2026, 9, 13), new DateTime(2026, 9, 13), "polling day itself");
                Next(CountryId.Sweden, new DateTime(2026, 9, 14), new DateTime(2030, 9, 8), "the day after");
                Next(CountryId.Sweden, SimulationManager.DefaultEpoch, new DateTime(2030, 9, 8), "the default epoch");
                Next(CountryId.Sweden, new DateTime(2022, 7, 21), new DateTime(2022, 9, 11), "an earlier date reads the same cycle");
                // §697 (PS-4): Germany's one polling day on record - the snap's, 23 Feb 2025 - and nothing after it (the next regular day's rule is not on disk)
                Next(CountryId.Germany, new DateTime(2024, 11, 6), new DateTime(2025, 2, 23), "Germany's snap start");
                Next(CountryId.Germany, new DateTime(2025, 2, 23), new DateTime(2025, 2, 23), "Germany's polling day itself");
                Check(!WorldClock.TryNextPollingDay(CountryId.Germany, new DateTime(2025, 2, 24), out _) && WorldClock.PollingDayBasis(CountryId.Germany) != null && WorldClock.PollingDayBasis(CountryId.Germany).Contains("[BWL-WT25]"),
                    "Germany after its snap: no polling day on record; the basis cites the snap's notice [BWL-WT25]");
                // §762 (PS-5): Poland's one polling day on record - the start's, 15 Oct 2023 - and nothing after it (Art. 98 ust. 2 gives the President a window, no rule for the day)
                Next(CountryId.Poland, new DateTime(2023, 2, 19), new DateTime(2023, 10, 15), "Poland's start");
                Next(CountryId.Poland, new DateTime(2023, 10, 15), new DateTime(2023, 10, 15), "Poland's polling day itself");
                Check(!WorldClock.TryNextPollingDay(CountryId.Poland, new DateTime(2023, 10, 16), out _) && WorldClock.PollingDayBasis(CountryId.Poland) != null
                      && WorldClock.PollingDayBasis(CountryId.Poland).Contains("[PKW-SEN-2023]") && WorldClock.PollingDayBasis(CountryId.Poland).Contains("Art. 98 ust. 2"),
                    "Poland after 15 Oct 2023: no polling day on record; the basis cites the PKW's notice [PKW-SEN-2023] and Art. 98 ust. 2's window");
                foreach (CountryId other in new[] { CountryId.Italy, CountryId.USA, CountryId.France })
                {
                    Check(!WorldClock.TryNextPollingDay(other, new DateTime(2026, 1, 18), out _) && WorldClock.PollingDayBasis(other) == null, other + ": no polling day and no basis - its calendar is not modelled, its chamber holds as of record");
                }

                // 3. The declarations an election reads, and the reference.
                Check(WorldClock.VintageOfElection(CountryId.Sweden, new DateTime(2026, 9, 13)) == ElectionVintage.Sweden2026, "13 Sep 2026 reads Sweden2026's declarations");
                Check(WorldClock.VintageOfElection(CountryId.Sweden, new DateTime(2030, 9, 8)) == ElectionVintage.Sweden2026, "8 Sep 2030 reads the latest dated declarations, Sweden2026's");
                Check(WorldClock.VintageOfElection(CountryId.Sweden, new DateTime(2022, 9, 11)) == ElectionVintage.Sweden2022, "11 Sep 2022 reads Sweden2022's");
                bool hasReference = WorldClock.TryReference(CountryId.Sweden, new DateTime(2026, 9, 13), out WorldClock.Reference reference);
                int sum = 0; if (hasReference) { foreach (int n in reference.Seats.Values) { sum += n; } }
                Check(hasReference && reference.Vintage == ElectionVintage.Sweden2026 && sum == 349 && !string.IsNullOrEmpty(reference.GovernmentLine) && reference.Label.Contains("2026"),
                    hasReference ? F("the reference for 13 Sep 2026: {0}, {1} seats; {2}", reference.Label, sum, reference.GovernmentLine) : "the reference for 13 Sep 2026 is MISSING");
                Check(!WorldClock.TryReference(CountryId.Sweden, new DateTime(2030, 9, 8), out _), "no reference for 8 Sep 2030 - the record holds no such election");

                // 4. The game's own path from Sweden's start.
                WorldClock.ApplyStart(CountryId.Sweden);
                var go = new GameObject("PollingDayDiagnostic");
                try
                {
                    SimulationRandom.Seed(Seed);
                    EnergyMarket.ResetCalibration();
                    World world = WorldFactory.CreateDefault();
                    SimulationManager sim = go.AddComponent<SimulationManager>();
                    sim.SetWorld(world);
                    sim.PlayerCountryId = CountryId.Sweden;
                    Country player = world.GetCountry(CountryId.Sweden);
                    PoliticalParty largest = default; int largestSeats = -1;
                    foreach (PoliticalParty party in PartySystems.For(CountryId.Sweden)) { int held = player.ParliamentSeats.TryGetValue(party.Abbrev, out int n) ? n : 0; if (largest.Abbrev == null || held > largestSeats) { largest = party; largestSeats = held; } }
                    player.PlayerPartyAbbrev = largest.Abbrev;
                    Check(sim.TryPlayerPollingDay(out DateTime first) && first == new DateTime(2026, 9, 13), F("the manager on Sweden's start offers {0:yyyy-MM-dd} as its polling day", first));
                    Check(new CampaignCalendar(first).PreCampaignStart == sim.CurrentDate, F("the run-up's first day is the start itself, {0:yyyy-MM-dd}", sim.CurrentDate));
                    Check(!sim.PollingDayToday, "day 0 is not polling day");
                    var none = new System.Collections.Generic.Dictionary<CountryId, PolicyDecision>();
                    foreach (Country c in world.Countries) { none[c.Id] = PolicyDecision.None(); }
                    int flagged = 0, flaggedOn = -1; DateTime flaggedDate = DateTime.MinValue; bool runUpBegun = false, campaignResultOnPollingDay = false; DateTime recordDate = DateTime.MinValue;
                    int countJudged = -1, openingJudged = -1;   // §752: the record judged at the opening and again for the count, both stored on the record
                    for (int day = 1; day <= 365; day++)
                    {
                        if (sim.AdvanceDay()) { sim.AdvanceTurn(none); }
                        if (day == 1 && sim.PlayerPreCampaign != null) { runUpBegun = true; }
                        if (sim.PollingDayToday)
                        {
                            flagged++; flaggedOn = day; flaggedDate = sim.CurrentDate;
                            campaignResultOnPollingDay = sim.PlayerCampaignResult != null && sim.CampaignRecord != null && sim.CampaignRecord.ElectionDate == sim.CurrentDate;
                            recordDate = sim.CampaignRecord != null ? sim.CampaignRecord.ElectionDate : DateTime.MinValue;
                            countJudged = sim.CampaignRecord?.CountRecordShift?.Count ?? -1;
                            openingJudged = sim.CampaignRecord?.RecordShift?.Count ?? -1;
                        }
                    }
                    Check(runUpBegun, "the run-up begins on the first advanced day (the pre-campaign is live)");
                    Check(flagged == 1 && flaggedDate == new DateTime(2026, 9, 13) && flaggedOn == 238, F("PollingDayToday was raised {0} time(s) in the first year - on day {1}, {2:yyyy-MM-dd}", flagged, flaggedOn, flaggedDate));
                    Check(campaignResultOnPollingDay, F("on polling day the campaign that ran up to it has a result, its record's election {0:yyyy-MM-dd}", recordDate));
                    Check(openingJudged > 0 && countJudged > 0, F("§752: the government's record judged at the campaign's opening ({0} part(y/ies)) and again for the count, stored for a replay ({1})", openingJudged, countJudged));
                    Check(sim.TryPlayerPollingDay(out DateTime next) && next == new DateTime(2030, 9, 8), F("after the year the manager offers {0:yyyy-MM-dd}", next));
                    Check(sim.PlayerCampaign == null, "the 2026 campaign's running state was dropped once its polling day had passed");
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(go);
                    EnergyMarket.ResetTurnState();   // the review (§619): the year crossed one boundary, whose BeginTurn set the process's water value and deficit share - put back, as PlayProtocolCheck does
                }

                // 5. §762 (PS-5): A POLISH GAME'S OWN PATH - from Poland's start (the run-up's first day) through its polling day, 15 Oct 2023. No campaign
                // is staged for Poland (LiveCampaignSetup stages Sweden's and Germany's), so on the day the controller counts the live prediction
                // (GameController.RunNationalElection's no-campaign path, done here as it does it) - through the 41 districts.
                WorldClock.ApplyStart(CountryId.Poland);
                var goPl = new GameObject("PollingDayDiagnostic Poland");
                try
                {
                    SimulationRandom.Seed(Seed);
                    EnergyMarket.ResetCalibration();
                    World world = WorldFactory.CreateDefault();
                    SimulationManager sim = goPl.AddComponent<SimulationManager>();
                    sim.SetWorld(world);
                    sim.PlayerCountryId = CountryId.Poland;
                    Country player = world.GetCountry(CountryId.Poland);
                    player.PlayerPartyAbbrev = "PiS";
                    DateTime start = sim.CurrentDate;
                    Check(sim.TryPlayerPollingDay(out DateTime plFirst) && plFirst == new DateTime(2023, 10, 15), F("the manager on Poland's start ({0:yyyy-MM-dd}) offers {1:yyyy-MM-dd} as its polling day", start, plFirst));
                    var none = new System.Collections.Generic.Dictionary<CountryId, PolicyDecision>();
                    foreach (Country c in world.Countries) { none[c.Id] = PolicyDecision.None(); }
                    int flagged = 0; DateTime flaggedDate = DateTime.MinValue; bool staged = false;
                    ElectionRecord counted = null; System.Collections.Generic.Dictionary<string, double> predicted = null;
                    for (int day = 1; day <= 400 && sim.CurrentDate < new DateTime(2023, 10, 20); day++)
                    {
                        if (sim.AdvanceDay()) { sim.AdvanceTurn(none); }
                        if (sim.PlayerPreCampaign != null || sim.PlayerCampaign != null) { staged = true; }
                        if (sim.PollingDayToday)
                        {
                            flagged++; flaggedDate = sim.CurrentDate;
                            if (NationalElection.TryPredictShares(CountryId.Poland, out predicted, EconomicVote.RecordOverTerm(player, sim.CurrentDate, out _), on: sim.CurrentDate))
                            {
                                counted = NationalElection.Run(CountryId.Poland, sim.CurrentTurn, predicted, sim.CurrentDate);
                            }
                        }
                    }
                    Check(flagged == 1 && flaggedDate == new DateTime(2023, 10, 15), F("a Polish game raised PollingDayToday {0} time(s) between {1:yyyy-MM-dd} and 20 Oct 2023 - on {2:yyyy-MM-dd}", flagged, start, flaggedDate));
                    Check(!staged, "no run-up or campaign was staged for Poland on the way (LiveCampaignSetup stages none) - the day is counted by the prediction");
                    int seated = 0; var shareLine = new StringBuilder(); var seatLine = new StringBuilder();
                    if (counted != null)
                    {
                        foreach (System.Collections.Generic.KeyValuePair<string, int> kv in counted.Seats) { seated += kv.Value; if (kv.Value > 0) { seatLine.Append(kv.Key).Append(' ').Append(kv.Value).Append(", "); } }
                        foreach (System.Collections.Generic.KeyValuePair<string, double> kv in counted.Shares) { if (kv.Value > 0.0005) { shareLine.Append(kv.Key).Append(' ').Append((100.0 * kv.Value).ToString("0.0", CultureInfo.InvariantCulture)).Append(" %, "); } }
                    }
                    Check(counted != null && counted.Method == ElectionMethod.PolandDistricts && seated == 460,
                        F("on 15 Oct 2023 the prediction ({0}) counted through the 41 districts - {1}: {2}", shareLine.ToString().TrimEnd(',', ' '), counted?.Method.ToString() ?? "NOT COUNTED", seatLine.ToString().TrimEnd(',', ' ')));
                    Check(!sim.TryPlayerPollingDay(out _), "after 15 Oct 2023 the manager offers no Polish polling day - none on record (Art. 98 ust. 2's window, no rule for the day)");
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(goPl);
                    EnergyMarket.ResetTurnState();
                }
            }
            catch (Exception e)
            {
                failures++;
                sb.Append("    THREW: " + e.GetType().Name + ": " + e.Message + "\n" + e.StackTrace + "\n");
            }

            if (failures > 0)
            {
                Debug.LogError($"POLLING DAY: {failures} failure(s).\n{sb}");
                CheckExit.Finish(1);
                return;
            }
            Debug.Log(sb.ToString());
            CheckExit.Finish(0);

            void Next(CountryId id, DateTime from, DateTime expected, string what)
            {
                bool ok = WorldClock.TryNextPollingDay(id, from, out DateTime got) && got == expected;
                Check(ok, F("from {0:yyyy-MM-dd} ({1}): {2:yyyy-MM-dd}, expected {3:yyyy-MM-dd}", from, what, got, expected));
            }
        }

        private static string F(string format, params object[] args) => string.Format(CultureInfo.InvariantCulture, format, args);
    }
}
