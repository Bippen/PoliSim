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
    /// any date is the right one; Germany's snap (§697) is on record and after it, since PS-8, the first Sunday of Art. 39 GG's window (Elias's ruling K159), Poland's 2023 day then each term's by Art. 98 ust. 2's
    /// window (§767, Elias's ruling D3 - the rule held against the record's 2015, 2019 and 2023 from the Sejm's own term dates), the USA's presidential day
    /// of 3 U.S.C. §21(1) every fourth year from 5 November 2024 (PS-6 US-8 - the assertion inverted as §697's was for Germany; the US count's own path is
    /// `UsPresidentialElectionDiagnostic`'s) and, since PS-6 US-27, the midterm days of 2 U.S.C. §7 between them - every even year (the midterm's own path is
    /// `UsMidtermDiagnostic`'s), and the countries before their stages offer none (their chambers hold as of record, §618's ruling 4); an election on a polling
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
                // §697 (PS-4): Germany's polling day on record - the snap's, 23 Feb 2025 - and PS-8 (Elias's ruling K159) after it the first Sunday of Art. 39 GG's window:
                // forty-six months from the 21st Bundestag's first sitting of record (25 Mar 2025) run to Thursday 25 Jan 2029 (BGB § 187 Abs. 1, § 188 Abs. 2), the
                // window opening on the 26th, so Sunday 28 Jan 2029; the term that election gives first sits on the 30th day (27 Feb 2029), its period running to
                // Monday 27 Dec 2032, so Sunday 2 Jan 2033
                Next(CountryId.Germany, new DateTime(2024, 11, 6), new DateTime(2025, 2, 23), "Germany's snap start");
                Next(CountryId.Germany, new DateTime(2025, 2, 23), new DateTime(2025, 2, 23), "Germany's polling day itself");
                Next(CountryId.Germany, new DateTime(2025, 2, 24), new DateTime(2029, 1, 28), "the day after - the first Sunday of the 21st Bundestag's Art. 39 window (K159)");
                Next(CountryId.Germany, new DateTime(2029, 1, 28), new DateTime(2029, 1, 28), "the K159 day itself");
                Next(CountryId.Germany, new DateTime(2029, 1, 29), new DateTime(2033, 1, 2), "a term the game elects - its first sitting the 30th day after 28 Jan 2029");
                Check(WorldClock.FirstSundayOfBundestagWindow(new DateTime(2025, 3, 25)) == new DateTime(2029, 1, 28) && WorldClock.FirstSundayOfBundestagWindow(new DateTime(2029, 1, 28)) == new DateTime(2032, 12, 5)
                      && WorldClock.FirstSundayOfBundestagWindow(new DateTime(2029, 4, 30)) == new DateTime(2033, 3, 6),
                    "K159's rule read by the BGB (L2): 46 months from a first sitting end on the day of its number (a period ending on Sunday 28 Nov 2032 opens the window on Monday the 29th, so Sunday 5 Dec 2032), "
                    + "a missing day the month's last (from 30 Apr 2029 to 28 Feb 2033, BGB § 188 Abs. 3), the window opening the day after (Tuesday 1 Mar 2033, so Sunday 6 Mar 2033)");
                Check(WorldClock.PollingDayBasis(CountryId.Germany) != null && WorldClock.PollingDayBasis(CountryId.Germany).Contains("[BWL-WT25]") && WorldClock.PollingDayBasis(CountryId.Germany).Contains("[GG-39]") && WorldClock.PollingDayBasis(CountryId.Germany).Contains("K159"),
                    "Germany's basis cites the snap's notice [BWL-WT25], Art. 39 GG [GG-39] and the ruling K159");
                // §762, §767 (PS-5; Elias's ruling D3): Poland's start's own day, 15 Oct 2023, then each term's - the first Sunday of Art. 98 ust. 2's window
                // before the fourth anniversary of the term's first sitting; the rule held first against the record, from the Sejm's own term dates
                Next(CountryId.Poland, new DateTime(2023, 2, 19), new DateTime(2023, 10, 15), "Poland's start");
                Next(CountryId.Poland, new DateTime(2023, 10, 15), new DateTime(2023, 10, 15), "Poland's polling day itself");
                Next(CountryId.Poland, new DateTime(2023, 10, 16), new DateTime(2027, 10, 17), "the day after - the 10th term's window (its first sitting 13 Nov 2023)");
                Next(CountryId.Poland, new DateTime(2027, 10, 18), new DateTime(2031, 10, 19), "a term the game elects - its first sitting the 30th day after 17 Oct 2027");
                var terms = new System.Collections.Generic.Dictionary<int, DateTime>();
                string termsJson = System.IO.File.ReadAllText(System.IO.Path.Combine(Application.dataPath, "..", "ElectionsData", "poland", "raw", "records", "api_sejm_term.json"));
                foreach (System.Text.RegularExpressions.Match m in System.Text.RegularExpressions.Regex.Matches(termsJson, "\"from\":\"(\\d{4}-\\d{2}-\\d{2})\",\"num\":(\\d+)"))
                {
                    terms[int.Parse(m.Groups[2].Value, CultureInfo.InvariantCulture)] = DateTime.ParseExact(m.Groups[1].Value, "yyyy-MM-dd", CultureInfo.InvariantCulture);
                }
                bool termsRead = terms.ContainsKey(7) && terms.ContainsKey(8) && terms.ContainsKey(9) && terms.ContainsKey(10) && terms[10] == new DateTime(2023, 11, 13);
                DateTime rule2015 = termsRead ? WorldClock.FirstSundayOfTermWindow(terms[7]) : DateTime.MinValue, rule2019 = termsRead ? WorldClock.FirstSundayOfTermWindow(terms[8]) : DateTime.MinValue,
                    rule2023 = termsRead ? WorldClock.FirstSundayOfTermWindow(terms[9]) : DateTime.MinValue;
                Check(termsRead && rule2019 == WorldClock.ElectionDayOf(CountryId.Poland, ElectionVintage.Poland2019) && rule2023 == WorldClock.LatestElectionDay(CountryId.Poland)
                      && rule2015 == new DateTime(2015, 10, 11),
                    F("D3's rule on the Sejm's own term dates (api_sejm_term.json): from the 8th term's first sitting {0:yyyy-MM-dd} it gives {1:yyyy-MM-dd} (2019's), from the 9th's {2:yyyy-MM-dd} {3:yyyy-MM-dd} (2023's); from the 7th's {4:yyyy-MM-dd} {5:yyyy-MM-dd} - the record's 2015 election was on 25 Oct, the exception the ruling names",
                        termsRead ? terms[8] : DateTime.MinValue, rule2019, termsRead ? terms[9] : DateTime.MinValue, rule2023, termsRead ? terms[7] : DateTime.MinValue, rule2015));
                Check(WorldClock.PollingDayBasis(CountryId.Poland) != null && WorldClock.PollingDayBasis(CountryId.Poland).Contains("[PKW-SEN-2023]") && WorldClock.PollingDayBasis(CountryId.Poland).Contains("Art. 98 ust. 2")
                      && WorldClock.PollingDayBasis(CountryId.Poland).Contains("[API-TERM]") && WorldClock.PollingDayBasis(CountryId.Poland).Contains("ruled, E2") && !WorldClock.PollingDayBasis(CountryId.Poland).Contains("DECLARED"),
                    "Poland's basis cites the PKW's notice [PKW-SEN-2023], Art. 98 ust. 2's window, the 10th term's sitting [API-TERM] and the first-sitting day, ruled (E2, §773)");
                // PS-6 US-8 (inverted, as §697 did for Germany): the USA's presidential day of 3 U.S.C. §21(1) - the Tuesday next after the first Monday in
                // November, every fourth year from the election of record - held against the record's own two days (2024's, NARA's; 2020's, the House's
                // row, the same Tuesday by 2 U.S.C. §7). PS-6 US-27 MOVED THESE EXPECTATIONS (legitimately: the plan's US-27 adds every even year, 2 U.S.C. §7
                // [USC-7]): the day after 2024's is now the 2026 midterm, and after 2028 the 2030 midterm; the presidential days alone are held by
                // UsPresidentialElection.TryNextPresidentialDay just below
                Next(CountryId.USA, WorldClock.StartDate(CountryId.USA), new DateTime(2024, 11, 5), "the USA's start");
                Next(CountryId.USA, new DateTime(2024, 11, 5), new DateTime(2024, 11, 5), "the USA's polling day itself");
                Next(CountryId.USA, new DateTime(2024, 11, 6), new DateTime(2026, 11, 3), "the day after - the 2026 midterm (US-27; 1 Nov 2026 is a Sunday, so the first Monday is the 2nd)");
                Next(CountryId.USA, new DateTime(2026, 1, 18), new DateTime(2026, 11, 3), "2026 - its midterm is the game's polling day (US-27)");
                Next(CountryId.USA, new DateTime(2026, 11, 4), new DateTime(2028, 11, 7), "after the midterm - 1 Nov 2028 is a Wednesday, so the first Monday is the 6th");
                Next(CountryId.USA, new DateTime(2028, 11, 8), new DateTime(2030, 11, 5), "after 2028 - the 2030 midterm (US-27)");
                Next(CountryId.USA, new DateTime(2020, 1, 1), new DateTime(2020, 11, 3), "an earlier date reads the same cycle");
                Next(CountryId.USA, new DateTime(2021, 1, 1), new DateTime(2022, 11, 8), "an earlier midterm reads the same cycle (2022's, the House's day of record)");
                bool hasNext = UsPresidentialElection.TryNextPresidentialDay(CountryId.USA, new DateTime(2024, 11, 6), out DateTime nextPresident);
                bool hasAfter = UsPresidentialElection.TryNextPresidentialDay(CountryId.USA, new DateTime(2028, 11, 8), out DateTime afterThat);   // both read before the message, which prints them
                Check(hasNext && nextPresident == new DateTime(2028, 11, 7)
                      && hasAfter && afterThat == new DateTime(2032, 11, 2)
                      && UsCongressElection.IsMidterm(new DateTime(2026, 11, 3)) && !UsCongressElection.IsMidterm(new DateTime(2028, 11, 7)),
                    F("the USA's presidential days alone: after 5 Nov 2024 {0:yyyy-MM-dd}, after 2028 {1:yyyy-MM-dd} (1 Nov 2032 is a Monday, so the Tuesday after it is the 2nd); 2026 a midterm, 2028 not", nextPresident, afterThat));
                Check(WorldClock.ElectionDayOf(CountryId.USA, ElectionVintage.Usa2022) == new DateTime(2022, 11, 8) && WorldClock.PollingDayBasis(CountryId.USA).Contains("[USC-7]"),
                    "the midterm rule against the record: 2022's House election of record fell on 8 Nov 2022, the rule's day; the basis quotes 2 U.S.C. §7 [USC-7]");
                Check(UsPresidentialElection.PresidentialDay(2024) == WorldClock.LatestElectionDay(CountryId.USA) && UsPresidentialElection.PresidentialDay(2020) == WorldClock.ElectionDayOf(CountryId.USA, ElectionVintage.Usa2020),
                    F("the USA's rule against the record: {0:yyyy-MM-dd} (NARA's [EC-DATES] {1:yyyy-MM-dd}) and {2:yyyy-MM-dd} (the 2020 election of record, {3:yyyy-MM-dd})", UsPresidentialElection.PresidentialDay(2024),
                        WorldClock.LatestElectionDay(CountryId.USA), UsPresidentialElection.PresidentialDay(2020), WorldClock.ElectionDayOf(CountryId.USA, ElectionVintage.Usa2020)));
                Check(WorldClock.PollingDayBasis(CountryId.USA) != null && WorldClock.PollingDayBasis(CountryId.USA).Contains("3 U.S.C. §21(1)") && WorldClock.PollingDayBasis(CountryId.USA).Contains("[EC-DATES]")
                      && WorldClock.PollingDayBasis(CountryId.USA).Contains("the Tuesday next after the first Monday in November"),
                    "the USA's basis cites 3 U.S.C. §21(1) in its words and NARA's day of record [EC-DATES]");
                Check(!WorldClock.TryNextPollingDay(CountryId.France, new DateTime(2026, 1, 18), out _) && WorldClock.PollingDayBasis(CountryId.France) == null, "France: no polling day and no basis - its calendar is not modelled, its chamber holds as of record");
                // PS-7 (K156): Italy's - the snap election of record's day from the start, then the last Sunday before each term's fifth anniversary: 13 Oct 2027 is a
                // Wednesday, so 10 Oct 2027; that term first sits 20 days on (30 Oct 2027), its fifth anniversary 30 Oct 2032 is a Saturday, so 24 Oct 2032
                bool itStart = WorldClock.TryNextPollingDay(CountryId.Italy, WorldClock.StartDate(CountryId.Italy), out DateTime it22);
                bool itNext = WorldClock.TryNextPollingDay(CountryId.Italy, new DateTime(2022, 9, 26), out DateTime it27);
                bool itAfter = WorldClock.TryNextPollingDay(CountryId.Italy, it27.AddDays(1), out DateTime it32);
                Check(itStart && it22 == new DateTime(2022, 9, 25) && itNext && it27 == new DateTime(2027, 10, 10) && itAfter && it32 == new DateTime(2032, 10, 24)
                      && WorldClock.ItalyFirstSittingAfter(it22) == new DateTime(2022, 10, 13) && WorldClock.LastSundayBeforeFifthAnniversary(new DateTime(2021, 10, 17)) == new DateTime(2026, 10, 11)
                      && WorldClock.PollingDayBasis(CountryId.Italy) != null && WorldClock.PollingDayBasis(CountryId.Italy).Contains("[DPR-97]") && WorldClock.PollingDayBasis(CountryId.Italy).Contains("K156"),
                    F("PS-7 (K156): Italy's polling days - {0:yyyy-MM-dd} from the start (DPR 97/2022), then {1:yyyy-MM-dd} and {2:yyyy-MM-dd} (the last Sunday before each term's fifth anniversary; a Sunday anniversary is not before itself)", it22, it27, it32));

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
                    Check(sim.TryPlayerPollingDay(out DateTime plNext) && plNext == new DateTime(2027, 10, 17), F("after 15 Oct 2023 the manager offers {0:yyyy-MM-dd} - the 10th term's window, its first Sunday (D3)", plNext));
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
