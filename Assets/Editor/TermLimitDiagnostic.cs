using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using PoliSim.Data;
using PoliSim.Elections;
using PoliSim.Elections.Generated;
using PoliSim.Persistence;
using PoliSim.Simulation;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// PS-6 US-28 (`docs/specs/USA_STAGE_PLAN.md`, "The two-term limit and the next nominee"; R-US6 (b) as Elias ruled it, F8 §781): THE TWENTY-SECOND
    /// AMENDMENT AS DATA, AND THE TERM-LIMITED PARTY'S NOMINEE (<see cref="UsTermLimit"/>). The plan's done-when, as this diagnostic holds it - each world built
    /// as a new game builds one (the epoch set before the world is created) and stepped by the game's own loop:
    /// <list type="bullet">
    /// <item>(a) the amendment's two clauses held to NARA's saved amendments page, and the record's running mates held to NARA's Electoral College results pages
    /// (each page to its SHA-256 in SHA256SUMS.txt) - the Vice President of record by date read from them;</item>
    /// <item>(b) each person's elections derived, never typed: the record's winners from the catalog, the game's own 2024 count (the count's own path on a planted
    /// poll) replacing the record's 2024, a contest that names its winner otherwise still the same person, an undecided contest no one's election;</item>
    /// <item>(c) THE RECORD'S BY DATE: a world on the eve of 7 Nov 2028 with no contest of the game's - Trump, elected 2016 and 2024 by the record, cannot stand; the
    /// REP player's nominee defaults to the Vice President of record (Vance); the count holds him at the future factor; the 2028 count reads the outgoing term's
    /// record on the president's party; the controller's own election path ends nothing; the 2028 winner sworn on 20 Jan 2029 and the world stepped on - the run
    /// continues; the next field's nominee of the winner's party is the winner, within the limit;</item>
    /// <item>(d) THE GAME'S OWN TICKET and (e) THE PICK: worlds on the same eve carrying the game's own 2024 count for REP - the default the running mate on the 2024
    /// ticket of record (Vance); the player picks another of the party's people who may be elected, and the count fields the pick with the same shares and electors
    /// as the default (the pick changes who stands, not the vote); the refusals (a barred person, another party, a person not of the party, the field of record,
    /// a day that is not a presidential polling day, the counted day); an AI party takes
    /// the default whatever pick is kept; a save cut after the pick loads INTO THE GAME (§557) and its count fields the pick;</item>
    /// <item>(f) A HARRIS WIN: the game's own 2024 count for DEM leaves Harris eligible - she stands again in 2028 as the president within the limit, no party is
    /// term-limited, and no pick is offered;</item>
    /// <item>(g) a later chain on the game's own contests: Vance elected 2028 and 2032 cannot stand in 2036, and his party's default is the running mate on the
    /// game's 2028 ticket, kept on his re-elected 2032 ticket (US-8b: R-US27 (ii) as ruled - a re-elected president keeps the sitting Vice President);
    /// (l) the same rule for a Harris re-elected in 2028: Walz kept, and DEM's 2032 default;</item>
    /// <item>(h) the field of record untouched: no nominee of the game's on 5 Nov 2024 and the count the same contest with or without nominees; the no-country
    /// count's later field still unnamed (US-8's (f)); (i) the record's completeness for the people the pick offers: NARA's pages from 1952 to 2008 (US-13's)
    /// name none of them a winner, so the catalog's window holds every election of theirs; (j) the second clause DECLARED not reachable; (k) scope.</item>
    /// <item>(m) US-28b (R-US26 (a) as ruled): the player's out-party picks from its people, the defeated president among them, the default unnamed - a Harris
    /// elected in 2024 and beaten in 2028 fielded by the DEM player in 2032; the AI out-party's default; the vote unchanged; the pick in the save and the count.</item>
    /// </list>
    /// No card block: the readings are this check's lines.
    /// </summary>
    public static class TermLimitDiagnostic
    {
        private const int Seed = 777;
        private static readonly DateTime Polled2024 = new DateTime(2024, 11, 5);
        private static readonly DateTime Polled2028 = new DateTime(2028, 11, 7);
        private static readonly DateTime Polled2032 = new DateTime(2032, 11, 2);
        private static readonly DateTime Polled2036 = new DateTime(2036, 11, 4);
        private static readonly DateTime Oath2025 = new DateTime(2025, 1, 20);
        private static readonly DateTime Oath2029 = new DateTime(2029, 1, 20);

        private const string AmendmentsPage = "ElectionsData/usa/raw/executive/archives_amendments_11-27.html";
        private static string ResultsPage(int year) => year >= 2020
            ? F("ElectionsData/usa/raw/executive/archives_electoral_college_{0}.html", year)
            : year >= 2012 ? F("ElectionsData/usa/raw/returns/archives_electoral_college_{0}.html", year)
            : F("ElectionsData/usa/raw/midterms/archives_electoral_college_{0}.html", year);

        private const string Trump = "Donald J. Trump", Harris = "Kamala D. Harris", Vance = "JD Vance", Walz = "Tim Walz", Biden = "Joseph R. Biden Jr.";
        private const string RepUnnamed2028 = "Republican Party's candidate (2028)", DemUnnamed2028 = "Democratic Party's candidate (2028)";

        public static void Run()
        {
            CheckExit.ArmLogFold();
            var sb = new StringBuilder("=== TermLimitDiagnostic (PS-6 US-28): the Twenty-second Amendment as data, and the term-limited party's nominee ===\n");
            int failures = 0;
            void Check(bool ok, string what) { if (!ok) { failures++; } sb.Append(ok ? "    ok        " : "    FAIL      ").Append(what).Append('\n'); }
            try { Measure(sb, Check); }
            catch (Exception ex) { failures++; sb.Append("    FAIL      threw: ").Append(ex).Append('\n'); }
            sb.Append(failures == 0 ? "=== TermLimitDiagnostic: the limit binds the person, not the run ===" : F("=== TermLimitDiagnostic: {0} FAILED ===", failures));
            if (failures == 0) { Debug.Log(sb.ToString()); } else { Debug.LogError(sb.ToString()); }
            CheckExit.Finish(failures == 0 ? 0 : 1);
        }

        /// <summary>The random streams, the energy state and the regional derivation a prediction leaves are put back after, the epoch by its scope, and every
        /// host destroyed (UsPresidentialElectionDiagnostic's discipline).</summary>
        private static void Measure(StringBuilder sb, Action<bool, string> Check)
        {
            var hosts = new List<GameObject>();
            string dir = Path.Combine(Application.temporaryCachePath, "term_limit_" + Guid.NewGuid().ToString("N"));
            int seedWas = SimulationRandom.MasterSeed;
            Dictionary<SimulationRandom.Stream, int> drawsWas = SimulationRandom.CaptureDrawCounts();
            double[][] regionalShares = NationalElection.LastRegionalShares;
            string[] regionalNames = NationalElection.LastRegionalNames;
            double[] regionalWeights = NationalElection.LastRegionalWeights;
            double regionalWorst = NationalElection.LastRegionalWorstAbsError;
            IReadOnlyList<string> regionalKeys = NationalElection.LastRegionalKeys;
            try
            {
                Directory.CreateDirectory(dir);
                using (SimulationManager.EpochScope()) { MeasureAll(sb, Check, hosts, dir); }
            }
            finally
            {
                foreach (GameObject go in hosts) { if (go != null) { UnityEngine.Object.DestroyImmediate(go); } }
                EnergyMarket.ResetTurnState();
                SimulationRandom.RestoreState(seedWas, drawsWas);
                void Set(string property, object value) =>
                    typeof(NationalElection).GetProperty(property, BindingFlags.Public | BindingFlags.Static).GetSetMethod(true).Invoke(null, new[] { value });
                Set(nameof(NationalElection.LastRegionalShares), regionalShares);
                Set(nameof(NationalElection.LastRegionalNames), regionalNames);
                Set(nameof(NationalElection.LastRegionalWeights), regionalWeights);
                Set(nameof(NationalElection.LastRegionalWorstAbsError), regionalWorst);
                Set(nameof(NationalElection.LastRegionalKeys), regionalKeys);
                try { if (Directory.Exists(dir)) { Directory.Delete(dir, true); } } catch (Exception e) { sb.Append("    note      the temporary folder stays: ").Append(e.Message).Append('\n'); }
            }
        }

        private static void MeasureAll(StringBuilder sb, Action<bool, string> Check, List<GameObject> hosts, string dir)
        {
            string Day(DateTime d) => d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            string Won(IEnumerable<UsTermLimit.Election> es) => string.Join(", ", es.Select(e => F("{0} {1} ({2}, {3})", e.PollingDay.Year, e.Person, e.Party, e.InGame ? "the game's" : "the record's")));
            string People(IEnumerable<string> ps) => "[" + string.Join("; ", ps) + "]";

            // ---- (a) the amendment and the running mates of record, held to NARA's pages
            Check(PresidencyDiagnostic.Page(AmendmentsPage, out string amendments) && amendments.Contains(UsTermLimit.FirstClause) && amendments.Contains(UsTermLimit.SecondClause),
                F("(a) the Twenty-second Amendment, Section 1 ([CONST-AM], {0}) held to its SHA-256, holding both clauses - \"{1}\" and \"{2}\"", Path.GetFileName(AmendmentsPage), UsTermLimit.FirstClause, UsTermLimit.SecondClause));
            foreach ((int year, string party, string person) in UsTermLimit.RunningMatesOfRecord)
            {
                string tag = party == "REP" ? "R" : "D";
                bool page = PresidencyDiagnostic.Page(ResultsPage(year), out string html);
                string text = Words(html);
                var row = Array.Find(UsPresidentialReturns.Years, r => r.Year == year);
                string winner = row.CastR >= row.Electors / 2 + 1 ? "REP" : row.CastD >= row.Electors / 2 + 1 ? "DEM" : null;
                string asWinner = "Vice President " + person + " [" + tag + "]", asOpponent = "V.P. Opponent " + person + " [" + tag + "]", asOpponentColon = "V.P. Opponent: " + person + " [" + tag + "]";
                bool onTicket = winner == party ? text.Contains(asWinner) : text.Contains(asOpponent) || text.Contains(asOpponentColon);
                Check(page && onTicket, F("(a) the {0} {1} ticket's running mate, {2}, on NARA's results page ({3}, held to its SHA-256) as \"{4}\" - the {5} ticket", year, party, person, Path.GetFileName(ResultsPage(year)), winner == party ? asWinner : asOpponent, winner == party ? "winning" : "losing"));
            }

            // a world's USA to read the record on (no contest of the game's), and the game's own 2024 counts on planted polls
            SimulationManager.SetEpoch(Polled2028.AddDays(-1));
            (SimulationManager _, World _, Country probe) = Open(hosts, CountryId.USA, "REP");
            var repPoll = new Dictionary<string, double> { { "REP", 0.52 }, { "DEM", 0.48 } };
            var demPoll = new Dictionary<string, double> { { "REP", 0.45 }, { "DEM", 0.55 } };
            UsPresidentialElection.Contest countR = UsPresidentialElection.Count(CountryId.USA, Polled2024, repPoll, out ElectoralCollege.StateCall[] _, out string whyR);
            UsPresidentialElection.Contest countD = UsPresidentialElection.Count(CountryId.USA, Polled2024, demPoll, out ElectoralCollege.StateCall[] _, out string whyD);
            Check(countR != null && countR.Decided() && countR.WinnerParty == "REP" && countR.Winner == Trump && countD != null && countD.Decided() && countD.WinnerParty == "DEM" && countD.Winner == Harris,
                F("(b) the game's own 2024 counts through the count's own path on planted polls (REP 52 / DEM 48 and REP 45 / DEM 55): \"{0}\" and \"{1}\"", countR?.Line ?? whyR, countD?.Line ?? whyD));
            if (countR == null || countD == null) { return; }

            // ---- (a, cont.) the Vice President of record by date
            {
                bool before = UsTermLimit.TryVicePresidentOn(probe, new DateTime(2024, 3, 12), out string vpStart, out string vpStartParty, out string _);
                bool after = UsTermLimit.TryVicePresidentOn(probe, Polled2028, out string vpLate, out string vpLateParty, out string vpLateBasis);
                Check(before && vpStart == Harris && vpStartParty == "DEM" && after && vpLate == Vance && vpLateParty == "REP",
                    F("(a) the Vice President of record by date (records §5, the running mate on the ticket that elected the president of record): on 2024-03-12 {0} ({1}); on {2} {3} ({4}) - {5}", vpStart, vpStartParty, Day(Polled2028), vpLate, vpLateParty, vpLateBasis));
            }

            // ---- (b) each person's elections, derived
            List<UsPresidentialElection.Contest> probeContests = probe.UsPresidentialElections;
            try
            {
                probeContests.Clear();
                List<UsTermLimit.Election> record = UsTermLimit.ElectionsBefore(probe, Polled2028);
                var expected = UsPresidentialReturns.Years.Where(y => UsPresidentialElection.PresidentialDay(y.Year) < Polled2028)
                    .Select(y => (y.Year, Person: y.CastR >= y.Electors / 2 + 1 ? y.NomineeR : y.NomineeD)).ToList();
                Check(record.Count == expected.Count && record.Zip(expected, (e, x) => e.PollingDay.Year == x.Year && e.Person == x.Person && !e.InGame).All(same => same),
                    F("(b) the record's elections, read from the catalog (NARA's winners - the nominee with a majority of the electors), with no contest of the game's: {0}", Won(record)));
                List<UsTermLimit.Election> trumpRecord = UsTermLimit.ElectionsOf(probe, Trump, Polled2028);
                Check(trumpRecord.Count == 2 && !UsTermLimit.Eligible(probe, Trump, Polled2028) && UsTermLimit.Eligible(probe, Trump, Polled2024)
                      && UsTermLimit.Eligible(probe, Harris, Polled2024) && UsTermLimit.ElectionsOf(probe, Harris, Polled2024).Count == 0
                      && UsTermLimit.ElectionsOf(probe, Biden, Polled2028).Count == 1 && UsTermLimit.Eligible(probe, Vance, Polled2028),
                    F("(b) by the record alone {0} was elected {1} - he cannot stand on {2}; on {3} the field of record's two may both stand ({0} elected once, {4} never); {5} elected once; {6} never",
                        Trump, UsTermLimit.YearsOf(trumpRecord), Day(Polled2028), Day(Polled2024), Harris, Biden, Vance));

                probeContests.Add(Copy(countR));
                List<UsTermLimit.Election> withR = UsTermLimit.ElectionsBefore(probe, Polled2028);
                List<UsTermLimit.Election> trumpR = UsTermLimit.ElectionsOf(probe, Trump, Polled2028);
                Check(trumpR.Count == 2 && trumpR[0].PollingDay.Year == 2016 && !trumpR[0].InGame && trumpR[1].PollingDay == Polled2024 && trumpR[1].InGame
                      && withR.Count(e => e.PollingDay.Year == 2024) == 1 && !UsTermLimit.Eligible(probe, Trump, Polled2028),
                    F("(b) with the game's own 2024 count for REP: {0} - the record's 2024 yields to the game's, never counted twice; {1} elected {2}, barred on {3}", Won(withR), Trump, UsTermLimit.YearsOf(trumpR), Day(Polled2028)));

                UsPresidentialElection.Contest relabelled = Copy(countR);
                foreach (UsPresidentialElection.Tally t in relabelled.Electors) { if (t.Party == "REP") { t.Candidate = "the Republican nominee"; } }
                relabelled.Winner = "the Republican nominee";
                probeContests.Clear();
                probeContests.Add(relabelled);
                Check(UsTermLimit.WinnerOf(relabelled) == Trump && UsTermLimit.ElectionsOf(probe, Trump, Polled2028).Count == 2,
                    F("(b) the person, not the label: the same count with its winner printed \"the Republican nominee\" (the count's naming is asked of Elias) still elects {0} - his second election, barred on {1}", UsTermLimit.WinnerOf(relabelled), Day(Polled2028)));

                UsPresidentialElection.Contest undecided = Copy(countR);
                undecided.Winner = null; undecided.WinnerParty = null; undecided.Contingent = true;
                probeContests.Clear();
                probeContests.Add(undecided);
                Check(UsTermLimit.ElectionsOf(probe, Trump, Polled2028).Count == 1 && UsTermLimit.ElectionsBefore(probe, Polled2028).All(e => e.PollingDay.Year != 2024),
                    "(b) a contest the game held and no one won (a House that could not choose) is no one's election, and the record's 2024 still yields to it: Trump elected once (2016)");

                probeContests.Clear();
                probeContests.Add(Copy(countD));
                Check(UsTermLimit.ElectionsOf(probe, Harris, Polled2028).Count == 1 && UsTermLimit.Eligible(probe, Harris, Polled2028) && UsTermLimit.ElectionsOf(probe, Trump, Polled2028).Count == 1 && UsTermLimit.Eligible(probe, Trump, Polled2028),
                    F("(b) with the game's own 2024 count for DEM: {0} - {1} elected once (2024), eligible on {2}; {3} elected once (2016), eligible", Won(UsTermLimit.ElectionsBefore(probe, Polled2028)), Harris, Day(Polled2028), Trump));

                // ---- (h) the field of record untouched
                List<UsTermLimit.Nominee> on2024 = UsTermLimit.NomineesFor(probe, Polled2024);
                UsPresidentialElection.Contest plain = UsPresidentialElection.Count(CountryId.USA, Polled2024, repPoll, out ElectoralCollege.StateCall[] _, out string _);
                var planted = new List<UsTermLimit.Nominee> { new UsTermLimit.Nominee { Party = "REP", Person = "a planted name", TermLimited = true, Barred = "a planted president", BarredBecause = "planted" } };
                UsPresidentialElection.Contest withNominees = UsPresidentialElection.Count(CountryId.USA, Polled2024, repPoll, planted, out ElectoralCollege.StateCall[] _, out string _);
                Check(on2024.Count == 0 && UsTermLimit.EligibleFor(probe, "REP", Polled2024).Count == 0 && plain != null && withNominees != null && PresidencyDiagnostic.SameContest(plain, withNominees) && plain.Line == withNominees.Line,
                    "(h) the field of record untouched (R-US2 (a)): no nominee of the game's on 2024-11-05, no pick, and the 2024 count is the same contest, line and all, whatever nominees it is handed");
                UsPresidentialElection.Contest unnamed = UsPresidentialElection.Count(CountryId.USA, Polled2028, new Dictionary<string, double> { { "REP", 0.5 }, { "DEM", 0.5 } }, out ElectoralCollege.StateCall[] _, out string _);
                Check(unnamed != null && unnamed.Electors.Any(t => t.Party == "REP" && t.Candidate == RepUnnamed2028) && unnamed.Electors.Any(t => t.Party == "DEM" && t.Candidate == DemUnnamed2028),
                    "(h) the count with no country names no one: the later field unnamed as US-8 built it (its diagnostic's (f) unchanged)");

                // ---- (g) a later chain on the game's own contests
                probeContests.Clear();
                probeContests.Add(Copy(countR));
                var chainNominees2028 = new List<UsTermLimit.Nominee> { new UsTermLimit.Nominee { Party = "REP", Person = Vance }, new UsTermLimit.Nominee { Party = "DEM", Person = DemUnnamed2028 } };
                UsPresidentialElection.Contest vance2028 = UsPresidentialElection.Count(CountryId.USA, Polled2028, repPoll, chainNominees2028, out ElectoralCollege.StateCall[] _, out string _);
                if (vance2028 != null) { probeContests.Add(vance2028); }
                UsTermLimit.Nominee rep2032 = UsTermLimit.NomineeOf(probe, Polled2032, "REP");
                var chainNominees2032 = new List<UsTermLimit.Nominee> { new UsTermLimit.Nominee { Party = "REP", Person = rep2032?.Person } };
                UsPresidentialElection.Contest vance2032 = UsPresidentialElection.Count(CountryId.USA, Polled2032, repPoll, chainNominees2032, out ElectoralCollege.StateCall[] _, out string _);
                if (vance2032 != null) { probeContests.Add(vance2032); }
                UsTermLimit.Nominee rep2036 = UsTermLimit.NomineeOf(probe, Polled2036, "REP");
                List<string> choices2036 = UsTermLimit.EligibleFor(probe, "REP", Polled2036);
                string mate2032 = "Republican Party's candidate for Vice President (2028)";   // US-8b (R-US27 (ii) as ruled): the 2028 mate, kept on the re-elected 2032 ticket
                bool vp2033 = UsTermLimit.TryVicePresidentOn(probe, new DateTime(2033, 2, 1), out string vpAfter2032, out string vpAfter2032Party, out string vpAfter2032Basis);
                Check(vp2033 && vpAfter2032 == mate2032 && vpAfter2032Party == "REP" && vpAfter2032Basis.Contains("kept on the re-elected president's 2032 ticket"),
                    F("(l) a re-elected president keeps the sitting Vice President (R-US27 (ii), ruled): after {0}'s 2032 re-election the Vice President is {1} - {2}", Vance, vpAfter2032, vpAfter2032Basis));
                Check(vance2028?.Winner == Vance && rep2032 != null && rep2032.Person == Vance && !rep2032.TermLimited && vance2032?.Winner == Vance
                      && rep2036 != null && rep2036.TermLimited && rep2036.Barred == Vance && rep2036.BarredBecause == "elected 2028 and 2032" && rep2036.Default == mate2032 && rep2036.ViceDefault
                      && !choices2036.Contains(Vance) && !choices2036.Contains(Trump) && choices2036.FirstOrDefault() == mate2032 && choices2036.Contains("Republican Party's candidate (2036)"),
                    F("(g) a chain on the game's own contests: {0} elected 2028 stands again in 2032 as the president within the limit ({1}); elected 2028 and 2032 he cannot stand in 2036, and REP's default is {2} ({3}); the choices {4}",
                        Vance, rep2032?.Basis, rep2036?.Default, rep2036?.Basis, People(choices2036)));

                // ---- (l) R-US27 (ii), ruled: a Harris re-elected in 2028 keeps Walz, and DEM's 2032 default is Walz
                probeContests.Clear();
                probeContests.Add(Copy(countD));
                UsPresidentialElection.Contest harris2028 = UsPresidentialElection.Count(CountryId.USA, Polled2028, demPoll, UsTermLimit.NomineesFor(probe, Polled2028), out ElectoralCollege.StateCall[] _, out string whyHarris);
                if (harris2028 != null) { probeContests.Add(harris2028); }
                bool vp2029 = UsTermLimit.TryVicePresidentOn(probe, new DateTime(2029, 2, 1), out string vpHarris2, out string vpHarris2Party, out string vpHarris2Basis);
                UsTermLimit.Nominee dem2032 = UsTermLimit.NomineeOf(probe, Polled2032, "DEM");
                Check(harris2028 != null && UsTermLimit.WinnerOf(harris2028) == Harris && UsTermLimit.RunningMateOf(probe, harris2028, "DEM") == Walz
                      && vp2029 && vpHarris2 == Walz && vpHarris2Party == "DEM" && vpHarris2Basis.Contains("kept on the re-elected president's 2028 ticket")
                      && dem2032 != null && dem2032.TermLimited && dem2032.Barred == Harris && dem2032.BarredBecause == "elected 2024 and 2028" && dem2032.ViceDefault && dem2032.Default == Walz,
                    F("(l) {0} elected 2024 (the ticket of record, Walz) and re-elected 2028 ({1}) keeps the sitting Vice President: from 20 Jan 2029 {2} - {3}; in 2032 she cannot stand, and DEM's default is {4} ({5})",
                        Harris, harris2028?.Line ?? whyHarris, vpHarris2, vpHarris2Basis, dem2032?.Default, dem2032?.Basis));
            }
            finally { probeContests.Clear(); }
            EnergyMarket.ResetTurnState();

            // ---- (c) the record's by date: a world on the eve of 2028 with no contest of the game's, played past the oath
            var logged = new List<string>();
            void Listen(string condition, string stack, LogType type) { if (condition != null && condition.StartsWith("PRESIDENTIAL: USA", StringComparison.Ordinal)) { logged.Add(condition); } }
            {
                SimulationManager.SetEpoch(Polled2028.AddDays(-1));
                (SimulationManager sim, World w, Country usa) = Open(hosts, CountryId.USA, "REP");
                List<UsTermLimit.Nominee> field = UsTermLimit.NomineesFor(usa, Polled2028);
                UsTermLimit.Nominee rep = field.FirstOrDefault(n => n.Party == "REP"), dem = field.FirstOrDefault(n => n.Party == "DEM");
                List<string> choices = UsTermLimit.EligibleFor(usa, "REP", Polled2028);
                Check(rep != null && rep.TermLimited && rep.Barred == Trump && rep.BarredBecause == "elected 2016 and 2024" && rep.Person == Vance && rep.ViceDefault && rep.Basis.Contains("of record by date")
                      && dem != null && !dem.TermLimited && dem.Person == DemUnnamed2028,
                    F("(c) the record's by date ({0}, no contest of the game's): {1}, {2} by the record, cannot stand; REP fields {3} - {4}; DEM fields \"{5}\" ({6})", Day(sim.CurrentDate), rep?.Barred, rep?.BarredBecause, rep?.Person, rep?.Basis, dem?.Person, dem?.Basis));
                Check(choices.Count >= 2 && choices[0] == Vance && choices.Contains(RepUnnamed2028) && !choices.Contains(Trump),
                    F("(c) the REP player's choices, the default first: {0} - {1} is not among them", People(choices), Trump));

                var controller = BindController(hosts, sim, usa);
                // the review's L6: the game-over flag reached by name - a rename fails here rather than reading as "not over"
                FieldInfo gameOver = PresidencyDiagnostic.ControllerField("_isGameOver");
                Check(gameOver != null && gameOver.FieldType == typeof(bool), "(c) the controller's game-over flag (_isGameOver) is reached by name, so \"the game not over\" below is measured");
                UsPresidentialElection.Contest held = null;
                Dictionary<string, double> shift = null;
                PerceivedPerformance.TermReading reading = default;
                bool controllerPath = false, oathDay = false, over = false;
                string controllerWhat = "not reached";
                GovernmentRecord sworn = null;
                Application.logMessageReceived += Listen;
                try
                {
                    for (int d = 0; d < 120 && sim.CurrentDate < Oath2029.AddDays(1); d++)
                    {
                        Step(sim, w);
                        DateTime today = sim.CurrentDate;
                        if (sim.PollingDayToday && today == Polled2028)
                        {
                            held = usa.UsPresidentialElections.FirstOrDefault(c => c.PollingDay == Polled2028);
                            shift = EconomicVote.RecordOverTerm(usa, today, out reading);
                            int before = usa.ElectionHistory.Count;
                            MethodInfo hold = typeof(PoliSim.UI.GameController).GetMethod("HoldElectionWithoutTheNight", BindingFlags.Instance | BindingFlags.NonPublic);
                            if (hold != null) { hold.Invoke(controller, null); }
                            bool overNow = gameOver?.GetValue(controller) is bool b && b;
                            controllerPath = hold != null && gameOver != null && !overNow && usa.UsPresidentialElections.Count == 1;
                            controllerWhat = F("{0} record(s) added; game over {1}", usa.ElectionHistory.Count - before, overNow);
                        }
                        if (today == Oath2029)
                        {
                            sworn = usa.Government;
                            oathDay = held != null && held.Decided() && UsPresidentialElection.Governs(sworn, held) && sworn.FormedOn == Oath2029;
                        }
                    }
                    over = gameOver == null || (gameOver.GetValue(controller) is bool b2 && b2);
                }
                finally { Application.logMessageReceived -= Listen; }
                UsPresidentialElection.Tally heldRep = held?.Electors.FirstOrDefault(t => t.Party == "REP"), heldDem = held?.Electors.FirstOrDefault(t => t.Party == "DEM");
                string line = held == null ? null : logged.FirstOrDefault(l => l.Contains("the count of 2028-11-07"));
                Check(held != null && heldRep?.Candidate == Vance && heldDem?.Candidate == DemUnnamed2028 && line != null && line.Contains(Vance) && line.Contains(Trump + ", elected 2016 and 2024, cannot be elected again"),
                    F("(c) the count of {0} fields {1} for REP at the future factor, logged: \"{2}\"", Day(Polled2028), heldRep?.Candidate ?? "no one", line ?? "NOT LOGGED"));
                Check(shift != null && shift.ContainsKey("REP") && !shift.ContainsKey("DEM") && reading.TookOffice == Oath2025,
                    F("(c) the 2028 count reads the outgoing term's record on the president's party: the shift on {0}, the term from {1} ({2})",
                        shift == null ? "nothing" : string.Join(", ", shift.Select(kv => F("{0} {1:+0.0000;-0.0000}", kv.Key, kv.Value))), Day(reading.TookOffice), EconomicVote.Describe(reading)));
                Check(controllerPath, F("(c) the controller's own election path on the polling day (HoldElectionWithoutTheNight) ends nothing: {0}", controllerWhat));
                Check(oathDay && !over && sim.CurrentDate > Oath2029,
                    F("(c) the run continues past the limit: the 2028 winner sworn on {0} - {1} - and the world stepped on to {2}, the game not over", Day(Oath2029), sworn?.Executive ?? "no government", Day(sim.CurrentDate)));
                string winnerParty = held?.WinnerParty;
                UsTermLimit.Nominee next = winnerParty == null ? null : UsTermLimit.NomineeOf(usa, Polled2032, winnerParty);
                Check(next != null && !next.TermLimited && next.Person == UsTermLimit.WinnerOf(held),
                    F("(c) the next field ({0}): {1} fields its president, {2} - {3}", Day(Polled2032), winnerParty, next?.Person, next?.Basis));
                EnergyMarket.ResetTurnState();
            }

            // ---- (d) the game's own ticket and (e) the pick: two worlds on the same eve, the game's own 2024 count for REP in each
            string savePath = Path.Combine(dir, "after_pick.json");
            UsPresidentialElection.Contest byDefault = null, byPick = null;
            {
                SimulationManager.SetEpoch(Polled2028.AddDays(-2));
                (SimulationManager sim, World w, Country usa) = Open(hosts, CountryId.USA, "REP");
                usa.UsPresidentialElections.Add(Copy(countR));
                UsTermLimit.Nominee rep = UsTermLimit.NomineeOf(usa, Polled2028, "REP");
                Check(rep != null && rep.TermLimited && rep.Barred == Trump && rep.BarredBecause == "elected 2016 and 2024" && rep.Person == Vance && rep.ViceDefault && rep.Basis.Contains("ticket of record, which the game elected"),
                    F("(d) the game's own ticket: with the game's 2024 count for REP, {0} ({1}) cannot stand, and REP fields {2} - {3}", rep?.Barred, rep?.BarredBecause, rep?.Person, rep?.Basis));
                for (int d = 0; d < 3 && sim.CurrentDate < Polled2028; d++) { Step(sim, w); }
                byDefault = usa.UsPresidentialElections.FirstOrDefault(c => c.PollingDay == Polled2028);
                Check(byDefault != null && byDefault.Electors.Any(t => t.Party == "REP" && t.Candidate == Vance),
                    F("(d) the count of {0} fields the default - {1}", Day(Polled2028), byDefault?.Line ?? "no count"));
                EnergyMarket.ResetTurnState();
            }
            {
                SimulationManager.SetEpoch(Polled2028.AddDays(-2));
                (SimulationManager sim, World w, Country usa) = Open(hosts, CountryId.USA, "REP");
                // the review's L2: the field of record refuses a pick (no contest held yet, so the refusal is the field's, not the counted day's)
                bool ofRecord = !UsTermLimit.Pick(usa, Polled2024, "REP", Vance, out string ofRecordWhy);
                Check(ofRecord && ofRecordWhy == "the field of that day is the record's" && usa.UsNomineePicks.Count == 0,
                    F("(e) the field of record refuses a pick: {0} - \"{1}\", nothing kept", Day(Polled2024), ofRecordWhy));
                usa.UsPresidentialElections.Add(Copy(countR));
                List<string> choices = UsTermLimit.EligibleFor(usa, "REP", Polled2028);
                bool barred = !UsTermLimit.Pick(usa, Polled2028, "REP", Trump, out string barredWhy);
                bool other = !UsTermLimit.Pick(usa, Polled2028, "DEM", DemUnnamed2028, out string otherWhy);
                bool notOfParty = !UsTermLimit.Pick(usa, Polled2028, "REP", Harris, out string notOfPartyWhy);
                Check(barred && other && notOfParty && usa.UsNomineePicks.Count == 0,
                    F("(e) the refusals, nothing kept: {0} - \"{1}\"; DEM's nominee - \"{2}\"; {3} - \"{4}\"", Trump, barredWhy, otherWhy, Harris, notOfPartyWhy));
                // the review's L1: a day that is not a presidential polling day - the day after one, and a midterm's - is refused, nothing kept
                DateTime dayAfter = Polled2028.AddDays(1), midterm = new DateTime(2030, 11, 5);
                bool notDayAfter = !UsTermLimit.Pick(usa, dayAfter, "REP", RepUnnamed2028, out string dayAfterWhy);
                bool notMidterm = !UsTermLimit.Pick(usa, midterm, "REP", RepUnnamed2028, out string midtermWhy);
                Check(notDayAfter && notMidterm && dayAfterWhy == "that day is not a presidential polling day" && midtermWhy == dayAfterWhy && usa.UsNomineePicks.Count == 0,
                    F("(e) a day that is not a presidential polling day refuses a pick: {0} - \"{1}\"; {2} - \"{3}\"; nothing kept", Day(dayAfter), dayAfterWhy, Day(midterm), midtermWhy));
                bool picked = UsTermLimit.Pick(usa, Polled2028, "REP", RepUnnamed2028, out string pickWhy);
                UsTermLimit.Nominee afterPick = UsTermLimit.NomineeOf(usa, Polled2028, "REP");
                bool back = UsTermLimit.Pick(usa, Polled2028, "REP", Vance, out string _);
                int keptAfterBack = usa.UsNomineePicks.Count;
                UsTermLimit.Pick(usa, Polled2028, "REP", RepUnnamed2028, out string _);
                Check(picked && afterPick != null && afterPick.Picked && afterPick.Person == RepUnnamed2028 && afterPick.Default == Vance && back && keptAfterBack == 0 && usa.UsNomineePicks.Count == 1,
                    F("(e) the REP player picks from {0}: \"{1}\" stands - {2}; picking the default again keeps no pick{3}", People(choices), afterPick?.Person, afterPick?.Basis, picked ? string.Empty : " - refused: " + pickWhy));
                usa.PlayerPartyAbbrev = "DEM";
                UsTermLimit.Nominee ai = UsTermLimit.NomineeOf(usa, Polled2028, "REP");
                usa.PlayerPartyAbbrev = "REP";
                Check(ai != null && ai.Person == Vance && !ai.Picked,
                    F("(e) an AI party takes the default: with the player seated in DEM, REP fields {0} whatever pick is kept", ai?.Person));
                Step(sim, w);
                SaveGameService.SaveToFile(savePath, SaveGameService.CreateSaveGame(sim, w, CountryId.USA, null));
                Step(sim, w);
                byPick = usa.UsPresidentialElections.FirstOrDefault(c => c.PollingDay == Polled2028);
                UsPresidentialElection.Tally pickTally = byPick?.Electors.FirstOrDefault(t => t.Party == "REP"), defaultTally = byDefault?.Electors.FirstOrDefault(t => t.Party == "REP");
                Check(byPick != null && pickTally?.Candidate == RepUnnamed2028 && byPick.Line.Contains("the player's pick"),
                    F("(e) the count of {0} fields the pick - {1}", Day(Polled2028), byPick?.Line ?? "no count"));
                // the review's L2: the counted day refuses a pick, and the pick kept is left as it was
                int keptBefore = usa.UsNomineePicks.Count;
                bool counted = !UsTermLimit.Pick(usa, Polled2028, "REP", Vance, out string countedWhy);
                Check(byPick != null && counted && countedWhy == "that polling day is counted" && usa.UsNomineePicks.Count == keptBefore && keptBefore == 1 && usa.UsNomineePicks[0].Person == RepUnnamed2028,
                    F("(e) after the count a pick for {0} is refused - \"{1}\" - and the pick kept stays ({2} kept)", Day(Polled2028), countedWhy, usa.UsNomineePicks.Count));
                // the vote with the pick and with the default, on one poll: the count's own path, the nominees the country gives each way
                List<UsTermLimit.Nominee> withPick = UsTermLimit.NomineesFor(usa, Polled2028);
                usa.PlayerPartyAbbrev = "DEM";
                List<UsTermLimit.Nominee> withDefault = UsTermLimit.NomineesFor(usa, Polled2028);
                usa.PlayerPartyAbbrev = "REP";
                UsPresidentialElection.Contest onePick = UsPresidentialElection.Count(CountryId.USA, Polled2028, repPoll, withPick, out ElectoralCollege.StateCall[] _, out string _);
                UsPresidentialElection.Contest oneDefault = UsPresidentialElection.Count(CountryId.USA, Polled2028, repPoll, withDefault, out ElectoralCollege.StateCall[] _, out string _);
                UsPresidentialElection.Tally onePickRep = onePick?.Electors.FirstOrDefault(t => t.Party == "REP"), oneDefaultRep = oneDefault?.Electors.FirstOrDefault(t => t.Party == "REP");
                bool sameVote = onePickRep != null && oneDefaultRep != null && onePickRep.Candidate == RepUnnamed2028 && oneDefaultRep.Candidate == Vance
                                && onePick.Electors.Zip(oneDefault.Electors, (a, b) => a.Party == b.Party && a.Votes == b.Votes && a.Share == b.Share).All(same => same);
                Check(sameVote, F("(e) the pick changes who stands, not the vote: on one poll (REP 52 / DEM 48) REP's \"{0}\" takes {1:0.000000} % and {2} electors, the default {3} {4:0.000000} % and {5} - every tally equal (every nominee at the future factor)",
                    onePickRep?.Candidate, onePickRep?.Share ?? double.NaN, onePickRep?.Votes ?? -1, oneDefaultRep?.Candidate, oneDefaultRep?.Share ?? double.NaN, oneDefaultRep?.Votes ?? -1));
                sb.Append(F("    read      (e) the two worlds' own counts (the same eve, the same seed - the pick's and the default's): REP {0:0.000000} % and {1} electors with the pick, {2:0.000000} % and {3} with the default\n",
                    pickTally?.Share ?? double.NaN, pickTally?.Votes ?? -1, defaultTally?.Share ?? double.NaN, defaultTally?.Votes ?? -1));
                EnergyMarket.ResetTurnState();

                // the save cut after the pick, loaded INTO THE GAME and stepped to the polling day
                SaveGame save = SaveGameService.LoadFromFile(savePath);
                Country saved = save.World.GetCountry(CountryId.USA);
                bool carried = saved.UsNomineePicks.Count == 1 && saved.UsNomineePicks[0].Person == RepUnnamed2028 && saved.UsNomineePicks[0].PollingDay == Polled2028 && saved.UsNomineePicks[0].Party == "REP";
                (SimulationManager adopted, PoliSim.UI.GameController _) = PresidencyDiagnostic.Adopt(save, hosts, out string error);
                if (adopted == null) { Check(false, F("(e) the save cut after the pick: {0}", error)); }
                else
                {
                    try
                    {
                        Country u = adopted.World.GetCountry(CountryId.USA);
                        DateTime from = adopted.CurrentDate;
                        Step(adopted, adopted.World);
                        UsPresidentialElection.Contest loaded = u.UsPresidentialElections.FirstOrDefault(c => c.PollingDay == Polled2028);
                        Check(carried && loaded != null && loaded.Electors.Any(t => t.Party == "REP" && t.Candidate == RepUnnamed2028),
                            F("(e) the save cut after the pick ({0}, format {1}) carries it and loads into the game; stepped to {2} its count fields the pick - {3}", Day(from), save.SaveVersion, Day(adopted.CurrentDate), loaded?.Line ?? "no count"));
                    }
                    finally { EnergyMarket.ResetTurnState(); }
                }
                // a save from before the field (the key absent) loads with no pick - the default
                string json = SaveGameService.Serialize(SaveGameService.CreateSaveGame(sim, w, CountryId.USA, null));
                string stripped = Regex.Replace(json, "\"UsNomineePicks\"\\s*:\\s*\\[[^\\]]*\\]\\s*,?", string.Empty);
                SaveGame older = SaveGameService.Deserialize(stripped);
                Country olderUsa = older.World.GetCountry(CountryId.USA);
                Check(!stripped.Contains("UsNomineePicks") && olderUsa.UsNomineePicks != null && olderUsa.UsNomineePicks.Count == 0,
                    "(e) a save with no pick field - as every save before this field - loads with none, which is the default (additive under ruling A: no format step)");
            }

            // ---- (f) a Harris win: eligible, standing again, no party term-limited
            {
                SimulationManager.SetEpoch(Polled2028.AddDays(-1));
                (SimulationManager sim, World w, Country usa) = Open(hosts, CountryId.USA, "DEM");
                usa.UsPresidentialElections.Add(Copy(countD));
                List<UsTermLimit.Nominee> field = UsTermLimit.NomineesFor(usa, Polled2028);
                UsTermLimit.Nominee dem = field.FirstOrDefault(n => n.Party == "DEM"), rep = field.FirstOrDefault(n => n.Party == "REP");
                bool refused = !UsTermLimit.Pick(usa, Polled2028, "DEM", DemUnnamed2028, out string refusedWhy);
                Check(dem != null && dem.Person == Harris && !dem.TermLimited && rep != null && rep.Person == RepUnnamed2028 && !rep.TermLimited && field.All(n => !n.TermLimited)
                      && UsTermLimit.EligibleFor(usa, "DEM", Polled2028).Count == 0 && refused,
                    F("(f) a Harris win: {0} may stand again - {1}; REP fields \"{2}\"; no party is term-limited and no pick is offered (\"{3}\")", Harris, dem?.Basis, rep?.Person, refusedWhy));
                for (int d = 0; d < 2 && sim.CurrentDate < Polled2028; d++) { Step(sim, w); }
                UsPresidentialElection.Contest held = usa.UsPresidentialElections.FirstOrDefault(c => c.PollingDay == Polled2028);
                Check(held != null && held.Electors.Any(t => t.Party == "DEM" && t.Candidate == Harris) && !held.Line.Contains("cannot be elected again"),
                    F("(f) the count of {0} fields {1} for DEM - {2}", Day(Polled2028), Harris, held?.Line ?? "no count"));
                EnergyMarket.ResetTurnState();
            }

            // ---- (m) US-28b (R-US26 (a), ruled): the player's out-party picks from its people, the defeated president among them, the default unnamed - a Harris
            // elected in 2024 and beaten in 2028 fielded in 2032
            {
                const string DemUnnamed2032 = "Democratic Party's candidate (2032)";
                SimulationManager.SetEpoch(Polled2032.AddDays(-2));
                (SimulationManager sim, World w, Country usa) = Open(hosts, CountryId.USA, "DEM");
                usa.UsPresidentialElections.Add(Copy(countD));
                UsPresidentialElection.Contest beaten2028 = UsPresidentialElection.Count(CountryId.USA, Polled2028, repPoll, UsTermLimit.NomineesFor(usa, Polled2028), out ElectoralCollege.StateCall[] _, out string whyBeaten);
                if (beaten2028 != null) { usa.UsPresidentialElections.Add(beaten2028); }
                UsTermLimit.Nominee before = UsTermLimit.NomineeOf(usa, Polled2032, "DEM");
                List<string> choices = UsTermLimit.EligibleFor(usa, "DEM", Polled2032);
                Check(beaten2028 != null && beaten2028.WinnerParty == "REP" && beaten2028.Electors.Any(t => t.Party == "DEM" && t.Candidate == Harris)
                      && before != null && before.OutParty && !before.TermLimited && !before.Picked && before.Person == DemUnnamed2032 && before.Default == DemUnnamed2032
                      && choices.FirstOrDefault() == DemUnnamed2032 && choices.Contains(Harris) && UsTermLimit.Eligible(usa, Harris, Polled2032) && UsTermLimit.ElectionsOf(usa, Harris, Polled2032).Count == 1,
                    F("(m) the out-party (R-US26 (a), ruled): {0} elected 2024 and beaten 2028 ({1}); on {2} DEM is out of the White House and fields \"{3}\" by default - {4}; the DEM player's choices, the default first: {5}",
                        Harris, beaten2028?.Line ?? whyBeaten, Day(Polled2032), before?.Person, before?.Basis, People(choices)));
                bool picked = UsTermLimit.Pick(usa, Polled2032, "DEM", Harris, out string pickWhy);
                UsTermLimit.Nominee after = UsTermLimit.NomineeOf(usa, Polled2032, "DEM");
                bool notOfParty = !UsTermLimit.Pick(usa, Polled2032, "DEM", Vance, out string notOfPartyWhy);
                usa.PlayerPartyAbbrev = "REP";
                UsTermLimit.Nominee ai = UsTermLimit.NomineeOf(usa, Polled2032, "DEM");
                bool aiRefused = !UsTermLimit.Pick(usa, Polled2032, "DEM", Harris, out string aiWhy);
                usa.PlayerPartyAbbrev = "DEM";
                Check(picked && after != null && after.Picked && after.OutParty && after.Person == Harris && after.Default == DemUnnamed2032 && notOfParty
                      && ai != null && !ai.Picked && ai.Person == DemUnnamed2032 && aiRefused && usa.UsNomineePicks.Count == 1 && usa.UsNomineePicks[0].Person == Harris,
                    F("(m) the DEM player picks {0}: \"{1}\" stands - {2}{3}; {4} is refused - \"{5}\"; an AI out-party takes the default (\"{6}\"; a pick refused - \"{7}\")",
                        Harris, after?.Person, after?.Basis, picked ? string.Empty : " - refused: " + pickWhy, Vance, notOfPartyWhy, ai?.Person, aiWhy));
                // the vote with the pick and with the default, on one poll
                List<UsTermLimit.Nominee> withPick = UsTermLimit.NomineesFor(usa, Polled2032);
                usa.PlayerPartyAbbrev = "REP";
                List<UsTermLimit.Nominee> withDefault = UsTermLimit.NomineesFor(usa, Polled2032);
                usa.PlayerPartyAbbrev = "DEM";
                UsPresidentialElection.Contest onePick = UsPresidentialElection.Count(CountryId.USA, Polled2032, demPoll, withPick, out ElectoralCollege.StateCall[] _, out string _);
                UsPresidentialElection.Contest oneDefault = UsPresidentialElection.Count(CountryId.USA, Polled2032, demPoll, withDefault, out ElectoralCollege.StateCall[] _, out string _);
                bool sameVote = onePick != null && oneDefault != null && onePick.Electors.Any(t => t.Party == "DEM" && t.Candidate == Harris) && oneDefault.Electors.Any(t => t.Party == "DEM" && t.Candidate == DemUnnamed2032)
                                && onePick.Electors.Zip(oneDefault.Electors, (a, b) => a.Party == b.Party && a.Votes == b.Votes && a.Share == b.Share).All(same => same);
                Check(sameVote && onePick.Line.Contains("DEM, out of the White House, fields " + Harris),
                    F("(m) the out-party's pick changes who stands, not the vote: on one poll (REP 45 / DEM 55) every tally equal with {0} and with the default; the line - \"{1}\"", Harris, onePick?.Line ?? "no count"));
                // the pick rides the save (the list US-28 added - no format step)
                SaveGame copy = SaveGameService.Deserialize(SaveGameService.Serialize(SaveGameService.CreateSaveGame(sim, w, CountryId.USA, null)));
                Country copied = copy.World.GetCountry(CountryId.USA);
                bool carried = copied.UsNomineePicks.Count == 1 && copied.UsNomineePicks[0].Person == Harris && copied.UsNomineePicks[0].Party == "DEM" && copied.UsNomineePicks[0].PollingDay == Polled2032;
                for (int d = 0; d < 3 && sim.CurrentDate < Polled2032; d++) { Step(sim, w); }
                UsPresidentialElection.Contest held = usa.UsPresidentialElections.FirstOrDefault(c => c.PollingDay == Polled2032);
                Check(carried && held != null && held.Electors.Any(t => t.Party == "DEM" && t.Candidate == Harris),
                    F("(m) the pick rides the save (format {0}, no step: the list US-28 added) and the world's own count of {1} fields it - {2}", copy.SaveVersion, Day(Polled2032), held?.Line ?? "no count"));
                EnergyMarket.ResetTurnState();
            }

            // ---- (i) the record's completeness for the people the pick offers: NARA's earlier pages name none of them a winner
            {
                var people = new HashSet<string>(StringComparer.Ordinal) { Trump, Vance, Harris, Walz, Biden };
                foreach ((int _, string _, string mate) in UsTermLimit.RunningMatesOfRecord) { people.Add(mate); }
                int first = UsPresidentialReturns.Years.Min(y => y.Year);
                var winners = new List<string>();
                var unread = new List<int>();
                var clash = new List<string>();
                var winnerLine = new Regex(@"\bPresident (\S.{1,60}?) \[([A-Z]+)\] Main Opponent");
                for (int year = 1952; year < first; year += 4)
                {
                    if (!PresidencyDiagnostic.Page(ResultsPage(year), out string html)) { unread.Add(year); continue; }
                    Match m = winnerLine.Match(Words(html));
                    if (!m.Success) { unread.Add(year); continue; }
                    string winner = m.Groups[1].Value.Trim();
                    winners.Add(F("{0} {1} [{2}]", year, winner, m.Groups[2].Value));
                    if (people.Contains(winner)) { clash.Add(F("{0} {1}", year, winner)); }
                }
                Check(unread.Count == 0 && clash.Count == 0,
                    F("(i) the record's completeness for the people the pick can offer ({0}): NARA's pages from 1952 to {1} (held to their SHA-256) name none of them a winner{2} - so the catalog's window from {3} holds every election of theirs. Read: {4}",
                        string.Join(", ", people.OrderBy(p => p, StringComparer.Ordinal)), first - 4, unread.Count > 0 ? "; UNREAD " + string.Join(", ", unread) : clash.Count > 0 ? "; WINNERS " + string.Join(", ", clash) : string.Empty, first, string.Join("; ", winners)));
            }

            // ---- (j) the second clause; (k) scope
            sb.Append("    declared  (j) the amendment's second clause (more than two years of a term to which another was elected) arises only through succession, which the game does not model (the plan's §8: no Twenty-fifth Amendment) - not reachable, never counted\n");
            {
                SimulationManager.SetEpoch(Polled2028.AddDays(-1));
                (SimulationManager _, World w, Country _) = Open(hosts, CountryId.Germany, "CDU");
                Country germany = w.GetCountry(CountryId.Germany);
                Check(UsTermLimit.NomineesFor(germany, Polled2028).Count == 0 && !UsTermLimit.Pick(germany, Polled2028, "CDU", "anyone", out string _) && !UsTermLimit.TryPresidentOn(germany, Polled2028, out string _, out string _, out string _),
                    "(k) scope: a country that chooses no president by electors has no nominee, no pick and no president here");
                EnergyMarket.ResetTurnState();
            }
        }

        /// <summary>A page's words: scripts and styles dropped, every tag a space, the entities NARA's pages carry decoded, white space collapsed
        /// (`Tools/us_midterm_record.pl`'s html_text, the same rule).</summary>
        private static string Words(string html)
        {
            string s = Regex.Replace(html ?? string.Empty, @"<script\b.*?</script>", string.Empty, RegexOptions.Singleline | RegexOptions.IgnoreCase);
            s = Regex.Replace(s, @"<style\b.*?</style>", string.Empty, RegexOptions.Singleline | RegexOptions.IgnoreCase);
            s = Regex.Replace(s, "<[^>]*>", " ");
            s = s.Replace("&nbsp;", " ").Replace("&#160;", " ").Replace("\u00A0", " ").Replace("&amp;", "&");
            return Regex.Replace(s, @"\s+", " ");
        }

        /// <summary>A contest copied, so each world holds its own (a contest is state: the House's choice writes into one).</summary>
        private static UsPresidentialElection.Contest Copy(UsPresidentialElection.Contest c) =>
            JsonConvert.DeserializeObject<UsPresidentialElection.Contest>(JsonConvert.SerializeObject(c, SaveGameService.BuildSettings()), SaveGameService.BuildSettings());

        /// <summary>A world on the epoch already set, as a new game builds one: the player's country and party.</summary>
        private static (SimulationManager Sim, World World, Country Usa) Open(List<GameObject> hosts, CountryId player, string party)
        {
            SimulationRandom.Seed(Seed);
            EnergyMarket.ResetCalibration();
            World w = WorldFactory.CreateDefault();
            var go = new GameObject("TermLimitDiagnostic"); hosts.Add(go);
            var s = go.AddComponent<SimulationManager>();
            s.SetWorld(w);
            s.PlayerCountryId = player;
            w.GetCountry(player).PlayerPartyAbbrev = party;
            return (s, w, w.GetCountry(CountryId.USA));
        }

        /// <summary>A controller in edit mode driving <paramref name="sim"/> on <paramref name="usa"/> - its election path reached by name (PresidencyDiagnostic's way).</summary>
        private static PoliSim.UI.GameController BindController(List<GameObject> hosts, SimulationManager sim, Country usa)
        {
            var go = new GameObject("TermLimitDiagnostic.Controller"); hosts.Add(go);
            go.AddComponent<SimulationManager>();
            var controller = go.AddComponent<PoliSim.UI.GameController>();
            PresidencyDiagnostic.ControllerField("_simulationManager").SetValue(controller, sim);
            PresidencyDiagnostic.ControllerField("_playerCountry").SetValue(controller, usa);
            PresidencyDiagnostic.ControllerField("_selectedPlayerCountryId").SetValue(controller, (CountryId?)CountryId.USA);
            return controller;
        }

        /// <summary>One day in the controller's order (GameController: AdvanceDay, the player's day tick, the boundary's turn).</summary>
        private static void Step(SimulationManager s, World w)
        {
            bool boundary = s.AdvanceDay();
            if (s.PlayerCountryId.HasValue) { s.AdvanceCountryDayTick(s.PlayerCountryId.Value); }
            if (boundary)
            {
                var none = new Dictionary<CountryId, PolicyDecision>();
                foreach (Country c in w.Countries) { none[c.Id] = PolicyDecision.None(); }
                s.AdvanceTurn(none);
            }
        }

        private static string F(string format, params object[] args) => string.Format(CultureInfo.InvariantCulture, format, args);
    }
}
