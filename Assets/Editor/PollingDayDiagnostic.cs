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
    /// any date is the right one; the other five countries offer none (their chambers hold as of record, §618's ruling 4); an election on a polling
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
                foreach (CountryId other in new[] { CountryId.Germany, CountryId.Poland, CountryId.Italy, CountryId.USA, CountryId.France })
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
                    for (int day = 1; day <= 365; day++)
                    {
                        if (sim.AdvanceDay()) { sim.AdvanceTurn(none); }
                        if (day == 1 && sim.PlayerPreCampaign != null) { runUpBegun = true; }
                        if (sim.PollingDayToday)
                        {
                            flagged++; flaggedOn = day; flaggedDate = sim.CurrentDate;
                            campaignResultOnPollingDay = sim.PlayerCampaignResult != null && sim.CampaignRecord != null && sim.CampaignRecord.ElectionDate == sim.CurrentDate;
                            recordDate = sim.CampaignRecord != null ? sim.CampaignRecord.ElectionDate : DateTime.MinValue;
                        }
                    }
                    Check(runUpBegun, "the run-up begins on the first advanced day (the pre-campaign is live)");
                    Check(flagged == 1 && flaggedDate == new DateTime(2026, 9, 13) && flaggedOn == 238, F("PollingDayToday was raised {0} time(s) in the first year - on day {1}, {2:yyyy-MM-dd}", flagged, flaggedOn, flaggedDate));
                    Check(campaignResultOnPollingDay, F("on polling day the campaign that ran up to it has a result, its record's election {0:yyyy-MM-dd}", recordDate));
                    Check(sim.TryPlayerPollingDay(out DateTime next) && next == new DateTime(2030, 9, 8), F("after the year the manager offers {0:yyyy-MM-dd}", next));
                    Check(sim.PlayerCampaign == null, "the 2026 campaign's running state was dropped once its polling day had passed");
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(go);
                    EnergyMarket.ResetTurnState();   // the review (§619): the year crossed one boundary, whose BeginTurn set the process's water value and deficit share - put back, as PlayProtocolCheck does
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
