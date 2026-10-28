using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
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
    /// PS-6 US-8 (`docs/specs/USA_STAGE_PLAN.md`, "The 2024 presidential election held" - checkpoint 1): THE US PRESIDENTIAL COUNT, ACCEPTED. The plan's
    /// done-when, as this diagnostic holds it (each world built as a new game builds one - the epoch set before the world is created - and stepped by the
    /// game's own loop):
    /// <list type="bullet">
    /// <item>(a) the statutes and the dates: 3 U.S.C. §21(1) (the polling day), §7 (the electors' meeting), §15(a) (the count of 6 January) and the Twelfth
    /// Amendment (the House's choice) held to their saved pages (SHA-256 in SHA256SUMS.txt) by their words; the rules give NARA's key dates for 2024;</item>
    /// <item>(b) E1's acceptance for the USA: the reference world (US-6's - the epoch the eve of 5 Nov 2024, one day stepped) holds its own count on its
    /// polling day, on the poll E1's factors are fitted to, and elects the Republican nominee - its electors set against the record's 312-226, every state
    /// and district called wrong, its national vote against the FEC's, its states against US-4's (a) column (no call apart), every other candidate's share
    /// of each jurisdiction the ballots of record's (US-6c, recomputed from the catalog) and against the record's 2024 - written to the card; the stored contest
    /// re-derives the count's own states (<see cref="UsPresidentialElection.StatesOf"/>, the reason no per-state field rides the save);</item>
    /// <item>(c) a US game played from its start (12 Mar 2024) past 5 Nov 2024 to the day after the oath, in the controller's order: the polling day raised once,
    /// the run-up and the campaign staged and the count reading its final shares (PS-6 US-30), the count held once with its log line naming the winner and the electors, the controller's own election path (the
    /// warm-up's `HoldElectionWithoutTheNight`) recording that no chamber is elected, the record yielding from the polling day, the 119th House by date, the
    /// winner sworn on the oath's day; saves cut across the count and across the oath loaded INTO THE GAME (the controller's `RestoreFromSave`, §557) and
    /// stepped - the contest whole, nothing counted twice, the president kept;</item>
    /// <item>(d) R-US3 (a), planted: the record's 2024 college with Pennsylvania, Michigan and Wisconsin to the Democratic nominee and Nebraska's 2nd district
    /// to the Republican - 269-269 through the count's own path (<see cref="UsPresidentialElection.FromCollege"/>); in a world its contest is chosen on
    /// 6 January by the delegations of the House then seated, held against the catalog's districts counted here, and its winner sworn on the 20th; the
    /// Twelfth Amendment's choice on planted delegations - a tie of states decides nothing, a tied delegation casts no vote;</item>
    /// <item>(e) the count is pure: called twice it returns the same contest, writes no world and draws no random number; (f) a later day (2028): the field
    /// unnamed at E1's future factor, the record's 2024 states as the prior, the electors in force;</item>
    /// <item>(g) the default-USA paths (the plan's risk 13): before a pick the controller falls back to the USA while the manager holds no player country, so
    /// its day loop never runs and no default world holds a US count; since PS-8 (K243, R-US5 (a)) a world with no player country stepped across a US polling
    /// day holds the AI USA's count, by the idle game (the prediction, no campaign), on its day, its president in office until the oath (the shadow's, the
    /// dump's); the film driver's USA default plays the game from the USA's start, and its warm-up's window holds the polling days measured here - the count
    /// held there as play holds it, by design; (h) scope: a world whose player is another country holds the AI USA's count on its day too (PS-8's).</item>
    /// <item>(i) US-8b (R-US25, RULED: forks hold their own counts, every fork drawing the live game's election noise): the election's noise source
    /// (<see cref="SimulationRandom.ElectionNoiseOn"/>) gives the live game and a fork's generator the same draws for one polling day whatever their counted
    /// streams hold, moves no counted stream, and differs by day, country and game; at a test σ the two worlds' counts on one poll are one contest; and a
    /// fork of a US game at its start (`ShadowBaseline`, the impact ledger's), advanced a turn on the live game's decisions, elects the same president with
    /// identical shares.</item>
    /// </list>
    /// Pinned in the cheap bar and the documents bar through the US card's marked block (`docs/reference/US_ELECTIONS.md`, *The readings (US-8)*), written
    /// by <see cref="WriteReadings"/>; a block that no longer says what this measures fails here. ⚠ It depends on US-6's [FITTED] factors being patched in:
    /// with a NaN factor the count holds nothing and (b) and (c) fail by name.
    /// </summary>
    public static class UsPresidentialElectionDiagnostic
    {
        private const int Seed = 777;
        private static readonly DateTime Polled = new DateTime(2024, 11, 5);
        private static readonly DateTime HouseDay = new DateTime(2025, 1, 3);
        private static readonly DateTime Oath = new DateTime(2025, 1, 20);

        private const string Day21Page = "ElectionsData/usa/raw/apportionment/govinfo_uscode_3_21.html";
        private const string Day7Page = "ElectionsData/usa/raw/apportionment/govinfo_uscode_3_7.html";
        private const string Day15Page = "ElectionsData/usa/raw/apportionment/govinfo_uscode_3_15.html";
        private const string AmendmentsPage = "ElectionsData/usa/raw/executive/archives_amendments_11-27.html";
        private const string KeyDatesPage = "ElectionsData/usa/raw/executive/archives_electoral_college_key_dates.html";
        private const string Day21Words = "the Tuesday next after the first Monday in November, in every fourth year succeeding every election of a President and Vice President";
        private const string Day7Words = "the first Tuesday after the second Wednesday in December next following their appointment";
        private const string Day15Words = "Congress shall be in session on the sixth day of January succeeding every meeting of the electors";
        private static readonly string[] TwelfthWords =
        {
            "if such number be a majority of the whole number of Electors appointed",
            "the House of Representatives shall choose immediately, by ballot",
            "the votes shall be taken by states, the representation from each state having one vote",
            "a majority of all the states shall be necessary to a choice",
        };
        private static readonly string[] KeyDateWords = { "November 5, 2024—Election Day", "December 17, 2024—electors vote in their States", "January 6, 2025—Congress counts the electoral votes", "January 20, 2025 at Noon—Inauguration Day" };

        public static void Run()
        {
            CheckExit.ArmLogFold();
            var sb = new StringBuilder("=== UsPresidentialElectionDiagnostic (PS-6 US-8): the US presidential count - checkpoint 1 ===\n");
            int failures = 0;
            void Check(bool ok, string what) { if (!ok) { failures++; } sb.Append(ok ? "    ok        " : "    FAIL      ").Append(what).Append('\n'); }
            try
            {
                var card = new List<string>();
                string digest = Measure(sb, Check, card);
                string expected = CardBlock(card, digest);
                string onDisk = CardBlockOf(File.ReadAllText(PathOf(CardRelative), Encoding.UTF8));
                Check(onDisk == expected, onDisk == null ? F("the model card ({0}) carries no US-8 block", CardRelative)
                    : onDisk == expected ? F("the model card's US-8 block ({0}) says what this measures - {1} lines", CardRelative, card.Count)
                    : F("the model card's US-8 block ({0}) is STALE - regenerate: -executeMethod PoliSim.EditorTools.UsPresidentialElectionDiagnostic.WriteReadings", CardRelative));
            }
            catch (Exception ex) { failures++; sb.Append("    FAIL      threw: ").Append(ex).Append('\n'); }

            sb.Append(failures == 0 ? "=== UsPresidentialElectionDiagnostic: the count holds ===" : F("=== UsPresidentialElectionDiagnostic: {0} FAILED ===", failures));
            if (failures == 0) { Debug.Log(sb.ToString()); } else { Debug.LogError(sb.ToString()); }
            CheckExit.Finish(failures == 0 ? 0 : 1);
        }

        /// <summary>Writes the card's US-8 block from this reading - the stamp and its END marker placed once by hand; refuses while any check fails.</summary>
        public static void WriteReadings()
        {
            CheckExit.ArmLogFold();
            var sb = new StringBuilder("=== UsPresidentialElectionDiagnostic.WriteReadings (PS-6 US-8): the US model card's US-8 block ===\n");
            int failures = 0;
            void Check(bool ok, string what) { if (!ok) { failures++; } sb.Append(ok ? "    ok        " : "    FAIL      ").Append(what).Append('\n'); }
            try
            {
                var card = new List<string>();
                string digest = Measure(sb, Check, card);
                string path = PathOf(CardRelative);
                string text = File.ReadAllText(path, Encoding.UTF8);
                string old = CardBlockOf(text);
                if (failures > 0) { Check(false, "a check above failed - the card is not written; fix what failed, then write"); }
                else if (old == null) { Check(false, F("the model card ({0}) carries no US-8 block to write into", CardRelative)); }
                else
                {
                    string written = text.Replace("\r\n", "\n").Replace(old, CardBlock(card, digest));
                    if (text.Contains("\r\n")) { written = written.Replace("\n", "\r\n"); }
                    File.WriteAllText(path, written, new UTF8Encoding(false));
                    Check(true, F("the model card's US-8 block written ({0}) - {1} lines", CardRelative, card.Count));
                }
            }
            catch (Exception ex) { failures++; sb.Append("    FAIL      threw: ").Append(ex).Append('\n'); }

            sb.Append(failures == 0 ? "=== WriteReadings: written ===" : F("=== WriteReadings: {0} FAILED ===", failures));
            if (failures == 0) { Debug.Log(sb.ToString()); } else { Debug.LogError(sb.ToString()); }
            CheckExit.Finish(failures == 0 ? 0 : 1);
        }

        /// <summary>The reading <see cref="Run"/> checks and <see cref="WriteReadings"/> writes. The random streams, the energy state and the regional derivation
        /// a prediction leaves are put back after, the epoch by its scope, and every host destroyed.</summary>
        private static string Measure(StringBuilder sb, Action<bool, string> Check, List<string> card)
        {
            var hosts = new List<GameObject>();
            string dir = Path.Combine(Application.temporaryCachePath, "us_presidential_" + Guid.NewGuid().ToString("N"));
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
                using (SimulationManager.EpochScope()) { return MeasureAll(sb, Check, card, hosts, dir); }
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

        private static string MeasureAll(StringBuilder sb, Action<bool, string> Check, List<string> card, List<GameObject> hosts, string dir)
        {
            string Day(DateTime d) => d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            string Long(DateTime d) => d.ToString("d MMM yyyy", CultureInfo.InvariantCulture);
            var y2024 = Array.Find(UsPresidentialReturns.Years, r => r.Year == 2024);
            int recR = y2024.CastR + y2024.OthersRSlate, recD = y2024.CastD + y2024.OthersDSlate;

            // ---- (a) the statutes and the dates
            Check(PresidencyDiagnostic.Page(Day21Page, out string day21) && day21.Contains(Day21Words), F("(a) 3 U.S.C. §21(1) ({0}) held to its SHA-256, holding \"{1}\"", Path.GetFileName(Day21Page), Day21Words));
            Check(PresidencyDiagnostic.Page(Day7Page, out string day7) && day7.Contains(Day7Words), F("(a) 3 U.S.C. §7 ({0}) held to its SHA-256, holding \"{1}\"", Path.GetFileName(Day7Page), Day7Words));
            Check(PresidencyDiagnostic.Page(Day15Page, out string day15) && day15.Contains(Day15Words), F("(a) 3 U.S.C. §15(a) ({0}) held to its SHA-256, holding \"{1}\"", Path.GetFileName(Day15Page), Day15Words));
            Check(PresidencyDiagnostic.Page(AmendmentsPage, out string amendments) && TwelfthWords.All(amendments.Contains),
                F("(a) the Twelfth Amendment ([CONST-AM], {0}) held to its SHA-256, holding \"{1}\"", Path.GetFileName(AmendmentsPage), string.Join("\" ... \"", TwelfthWords)));
            Check(PresidencyDiagnostic.Page(KeyDatesPage, out string keyDates) && KeyDateWords.All(keyDates.Contains), F("(a) NARA's key dates ([EC-DATES]) held to its SHA-256 - {0}", string.Join("; ", KeyDateWords)));
            DateTime meet = UsPresidentialElection.ElectorsMeetAfter(Polled), counted = UsPresidentialElection.CountedOnAfter(Polled), oath = UsPresidentialElection.OathDayAfter(Polled);
            Check(UsPresidentialElection.PresidentialDay(2024) == Polled && meet == new DateTime(2024, 12, 17) && counted == new DateTime(2025, 1, 6) && oath == Oath
                  && WorldClock.TryNextPollingDay(CountryId.USA, WorldClock.StartDate(CountryId.USA), out DateTime fromStart) && fromStart == Polled,
                F("(a) the rules give NARA's days for 2024: polling day {0}, the electors meet {1}, Congress counts {2}, the oath {3}; the calendar from the USA's start holds {4}", Day(UsPresidentialElection.PresidentialDay(2024)), Day(meet), Day(counted), Day(oath), Day(Polled)));
            Check(UsPresidentialElection.ElectorsMeetAfter(new DateTime(2028, 11, 7)) == new DateTime(2028, 12, 19) && UsPresidentialElection.CountedOnAfter(new DateTime(2028, 11, 7)) == new DateTime(2029, 1, 6),
                "(a) and for 2028: the electors meet 2028-12-19 (the second Wednesday the 13th), Congress counts 2029-01-06");

            // ---- (b) E1's acceptance: the reference world holds its own count
            PresidentialReferenceWorld.Result world = HoldReference(hosts);
            List<UsPresidentialElection.Contest> held = world.Country?.UsPresidentialElections ?? new List<UsPresidentialElection.Contest>();
            UsPresidentialElection.Contest c = held.Count == 1 ? held[0] : null;
            Check(c != null && c.PollingDay == Polled && c.DayBasis.StartsWith(UsPresidentialElection.DayBasis, StringComparison.Ordinal),
                F("(b) the reference world (the epoch {0}, one day stepped) holds its own count on {1}, once - {2}", Day(Polled.AddDays(-1)), Day(Polled), c?.Line ?? held.Count + " contest(s)"));
            UsPresidentialElection.Contest recount = UsPresidentialElection.Count(CountryId.USA, Polled, world.PollOnFirstVote, out ElectoralCollege.StateCall[] states, out string why);
            Check(c != null && recount != null && PresidencyDiagnostic.SameContest(recount, c),
                F("(b) the count is the reference world's poll counted (the poll E1's factors are fitted on): recounted on it, the same contest field by field{0}", recount == null ? " - the recount failed: " + why : string.Empty));
            bool trump = c != null && c.Decided() && c.WinnerParty == "REP" && c.Winner == y2024.NomineeR && !c.Contingent;
            Check(trump, F("(b) E1's acceptance: it elects {0} ({1})", c?.Winner ?? "no one", c?.WinnerParty ?? "-"));
            int countR = c?.ElectorsOf("REP") ?? -1, countD = c?.ElectorsOf("DEM") ?? -1;
            // the electors set against the record: US-4's (a) on 2020's states gives the record's 312-226 at the true national shares (its pin), and this
            // world's national vote is the true one by construction (E1's factors) - so the record is expected, and a difference is a finding, not a tuning
            Check(countR == recR && countD == recD && c?.ElectorsAppointed == y2024.Electors,
                F("(b) its electors set against the record: {0} - {1} of {2}, the record as appointed {3} - {4}", countR, countD, c?.ElectorsAppointed, recR, recD));
            ElectoralCollege.Jurisdiction[] record = ElectoralCollege.FromCatalog(2024);
            var wrongStates = new List<string>();
            var wrongDistricts = new List<string>();
            if (states != null)
            {
                foreach (ElectoralCollege.StateCall call in states)
                {
                    ElectoralCollege.Jurisdiction j = record.First(x => x.Name == call.State);
                    if (call.Winner != j.StatewideWinner) { wrongStates.Add(call.State); }
                    for (int k = 0; k < call.Districts.Length; k++)
                    {
                        if (j.DistrictWinners == null || k >= j.DistrictWinners.Length || call.DistrictWinners[k] != j.DistrictWinners[k]) { wrongDistricts.Add(call.State + "-" + call.Districts[k].ToString(CultureInfo.InvariantCulture)); }
                    }
                }
            }
            string wrongStateText = wrongStates.Count == 0 ? "none" : string.Join(", ", wrongStates), wrongDistrictText = wrongDistricts.Count == 0 ? "none" : string.Join(", ", wrongDistricts);
            Check(states != null && wrongStates.Count == 0 && wrongDistricts.Count == 0, F("(b) every state and district called against the record's winners: states wrong {0}; districts wrong {1}", wrongStateText, wrongDistrictText));
            // the national vote against the FEC's, and the states against US-4's (a) column at the true national shares
            double[] fec = National2024();
            double countRShare = c?.Electors.FirstOrDefault(t => t.Party == "REP")?.Share ?? double.NaN, countDShare = c?.Electors.FirstOrDefault(t => t.Party == "DEM")?.Share ?? double.NaN;
            double countOShare = c?.Electors.FirstOrDefault(t => t.Candidate == UsPresidentialElection.OtherCandidates)?.Share ?? double.NaN;
            double nationalGap = Math.Max(Math.Abs(countRShare - 100.0 * fec[ElectoralCollege.ShareRepublican]), Math.Max(Math.Abs(countDShare - 100.0 * fec[ElectoralCollege.ShareDemocrat]), Math.Abs(countOShare - 100.0 * fec[ElectoralCollege.ShareOthers])));
            Check(nationalGap < 0.005, F("(b) its national vote, by construction the FEC's: REP {0:0.0000} %, DEM {1:0.0000} %, every other candidate {2:0.0000} % (the FEC's {3:0.0000}, {4:0.0000}, {5:0.0000}) - the largest gap {6:0.0000} pp",
                countRShare, countDShare, countOShare, 100.0 * fec[ElectoralCollege.ShareRepublican], 100.0 * fec[ElectoralCollege.ShareDemocrat], 100.0 * fec[ElectoralCollege.ShareOthers], nationalGap));
            ElectoralCollege.FromNationalShares(2020, 2024, fec, out ElectoralCollege.StateCall[] us4, out double _);
            double widest = 0.0;
            string widestAt = "-";
            int callsApart = 0;
            if (states != null)
            {
                for (int r = 0; r < states.Length; r++)
                {
                    double m = 100.0 * ((states[r].Shares[ElectoralCollege.ShareRepublican] - states[r].Shares[ElectoralCollege.ShareDemocrat]) - (us4[r].Shares[ElectoralCollege.ShareRepublican] - us4[r].Shares[ElectoralCollege.ShareDemocrat]));
                    if (Math.Abs(m) > widest) { widest = Math.Abs(m); widestAt = states[r].State; }
                    if (states[r].Winner != us4[r].Winner || !states[r].DistrictWinners.SequenceEqual(us4[r].DistrictWinners)) { callsApart++; }
                }
            }
            // US-6c: US-4's (a) swings all three columns - the two nominees and every other candidate, pooled - from 2020's states; the count stands every
            // other candidate only where on the ballot of record and swings the two nominees on their two-party split over the rest - so the margins differ
            // by that form, and no call may
            Check(states != null && callsApart == 0, F("(b) its states against US-4's (a) column at the true national shares: {0} calls apart; the largest margin difference {1:0.000000} pp ({2}) - the count's form on the ballots of record (US-6c: every other candidate where listed, the nominees on their two-party split), where (a) swings all three from 2020's", callsApart, widest, widestAt));
            // US-6c (J3): every other candidate stands only where on the ballot of record - each jurisdiction's share of every other candidate recomputed here
            // from the catalog alone (each column of 2024 but the nominees', its part of the count's pooled share by its national vote, spread over the
            // jurisdictions whose cell is roman by its share of record there; the scattered write-ins, on no ballot, where the workbook reports them), on the
            // count's own weights (2020's votes cast) - against the count's states; and against the record's 2024 shares
            double ballotGap = double.PositiveInfinity, recordWorst = 0.0, recordMean = 0.0;
            string recordWorstAt = "-";
            var standingCounts = new List<string>();
            if (states != null && !double.IsNaN(countOShare))
            {
                var prior2020 = UsPresidentialReturns.States.Where(s => s.Year == 2020).ToDictionary(s => s.State, s => (double)s.VotesTotal);
                var total2024 = UsPresidentialReturns.States.Where(s => s.Year == 2024).ToDictionary(s => s.State, s => s);
                double weightAll = prior2020.Values.Sum();
                var columns = UsPresidentialReturns.Candidates.Where(k => k.Year == 2024 && k.Ticket == "-" && k.Votes > 0).ToList();
                double votesAll = columns.Sum(k => (double)k.Votes);
                var onBallots = states.ToDictionary(s => s.State, s => 0.0);
                foreach (var k in columns)
                {
                    var cells = UsPresidentialReturns.CandidateStates.Where(b => b.Year == 2024 && b.Candidate == k.Candidate).ToList();
                    var where = cells.Any(b => b.Listed) ? cells.Where(b => b.Listed).ToList() : cells;
                    double under = where.Sum(b => prior2020[b.State] * b.Votes / (double)total2024[b.State].VotesTotal);
                    foreach (var b in where) { onBallots[b.State] += countOShare / 100.0 * k.Votes / votesAll * weightAll * (b.Votes / (double)total2024[b.State].VotesTotal) / under; }
                    if (k.Candidate == "STEIN" || k.Candidate == "KENNEDY" || k.Candidate == "OLIVER") { standingCounts.Add(F("{0} {1}", PresidencyOfRecord.NameOfRecord(CountryId.USA, 2024, k.Candidate), where.Count)); }
                }
                ballotGap = states.Max(s => Math.Abs(s.Shares[ElectoralCollege.ShareOthers] - onBallots[s.State]));
                foreach (ElectoralCollege.StateCall s in states)
                {
                    var rec = total2024[s.State];
                    double gap = 100.0 * Math.Abs(s.Shares[ElectoralCollege.ShareOthers] - (double)rec.VotesOther / rec.VotesTotal);
                    recordMean += gap / states.Length;
                    if (gap > recordWorst) { recordWorst = gap; recordWorstAt = s.State; }
                }
            }
            Check(ballotGap < 1e-12, F("(b) every other candidate stands only where on the ballot of record (J3, US-6c): each jurisdiction's share of them is the ballots', recomputed from the catalog - the largest gap {0:E1}; jurisdictions listing {1}; against the record's 2024 shares the largest gap {2:0.000} pp ({3}), the mean {4:0.000} pp",
                ballotGap, string.Join(", ", standingCounts), recordWorst, recordWorstAt, recordMean));
            ElectoralCollege.StateCall[] restated = c != null ? UsPresidentialElection.StatesOnRecord(c) : null;   // a first count: counted on the record's states (PS-6 US-26: the record form, by name)
            bool reDerived = restated != null && states != null && restated.Length == states.Length
                             && restated.Zip(states, (a, b) => a.State == b.State && a.Winner == b.Winner && a.Shares.SequenceEqual(b.Shares) && a.DistrictWinners.SequenceEqual(b.DistrictWinners)).All(same => same);
            Check(reDerived, "(b) the stored contest re-derives the count's own states exactly (UsPresidentialElection.StatesOnRecord, a first count's form) - the save carries the states through the national shares, so format 39 needs no per-state field");
            Check(c != null && c.ElectorsMeet == meet && c.CountedOn == counted && c.TakesOffice == oath, F("(b) its dates: the electors meet {0}, Congress counts {1}, the oath {2}", c != null ? Day(c.ElectorsMeet) : "-", c != null ? Day(c.CountedOn) : "-", c != null ? Day(c.TakesOffice) : "-"));

            // ---- (c) a US game played from its start past the count to the day after the oath, in the controller's order
            string saveCount = Path.Combine(dir, "after_count.json"), saveOath = Path.Combine(dir, "after_oath.json");
            var logged = new List<string>();
            void Listen(string condition, string stack, LogType type) { if (condition != null && condition.StartsWith("PRESIDENTIAL: USA", StringComparison.Ordinal)) { logged.Add(condition); } }
            UsPresidentialElection.Contest played = null;
            GovernmentRecord sworn = null;
            {
                WorldClock.ApplyStart(CountryId.USA);
                (SimulationManager sim, World w, Country usa) = Open(hosts, CountryId.USA, "REP");
                DateTime start = sim.CurrentDate;
                var controller = BindController(hosts, sim, usa);
                int raised = 0, contestsOnDay = -1;
                DateTime raisedOn = DateTime.MinValue;
                bool staged = false, campaignCounted = false, controllerPath = false, recordYields = false, houseByDate = false, oathDay = false, cutCount = false, cutOath = false;
                string controllerWhat = "not reached", lineOnDay = null;
                Application.logMessageReceived += Listen;
                try
                {
                    for (int d = 0; d < 400 && sim.CurrentDate < Oath.AddDays(1); d++)
                    {
                        Step(sim, w);
                        DateTime today = sim.CurrentDate;
                        if (sim.PlayerPreCampaign != null || sim.PlayerCampaign != null) { staged = true; }
                        if (sim.PollingDayToday)
                        {
                            raised++; raisedOn = today;
                            contestsOnDay = usa.UsPresidentialElections.Count;
                            played = contestsOnDay == 1 ? usa.UsPresidentialElections[0] : null;
                            lineOnDay = played?.Line;   // the count's own line - a House choice on 6 January would append to it
                            campaignCounted = sim.TryPlayerCampaignShares(out Dictionary<string, double> _) && logged.Any(l => l.StartsWith("PRESIDENTIAL: USA - the campaign ran to the polling day", StringComparison.Ordinal));   // PS-6 US-30: the campaign's final shares counted
                            // the controller's election path on the day - the warm-up's HoldElectionWithoutTheNight, as the film driver calls it
                            int before = usa.ElectionHistory.Count;
                            MethodInfo hold = typeof(PoliSim.UI.GameController).GetMethod("HoldElectionWithoutTheNight", BindingFlags.Instance | BindingFlags.NonPublic);
                            if (hold != null) { hold.Invoke(controller, null); }
                            ElectionRecord last = usa.ElectionHistory.Count > before ? usa.ElectionHistory[usa.ElectionHistory.Count - 1] : null;
                            bool over = PresidencyDiagnostic.ControllerField("_isGameOver")?.GetValue(controller) is bool b && b;
                            // PS-6 K74 (Elias's ruling of 2026-10-23): the record carries the US method and the House the day loop elected today, by party - no
                            // parliamentary reader reads it (NationalElection.CountsParliament)
                            UsCongressElection.Contest houseToday = usa.UsCongressElections.Find(c => c.PollingDay.Date == today.Date);
                            bool seatsAsCounted = houseToday != null && last != null && last.Seats.Count > 0 && last.Seats.Values.Sum() == UsCongressElection.HouseMembers
                                                  && houseToday.HouseSeats(CountryId.USA).All(kv => last.Seats.TryGetValue(kv.Key, out int n) && n == kv.Value);
                            controllerPath = hold != null && usa.ElectionHistory.Count == before + 1 && last.Date == today && last.Method == ElectionMethod.UsCongress && seatsAsCounted
                                             && !NationalElection.CountsParliament(last.Method) && NationalElection.CountsParliament(ElectionMethod.SwedenTwoTier)
                                             && last.NotHeldReason == NationalElection.NotHeldReason(CountryId.USA) && !over && usa.UsPresidentialElections.Count == 1;
                            controllerWhat = last == null ? "no record added" : F("{0} on {1} (seats {2}; the day loop's House {3}): \"{4}\"; game over {5}", last.Method, Day(last.Date),
                                string.Join(" ", last.Seats.Select(kv => kv.Key + " " + kv.Value)), houseToday == null ? "NONE" : houseToday.HouseRep + "/" + houseToday.HouseDem, last.NotHeldReason, over);
                        }
                        if (today == Polled.AddDays(1))
                        {
                            recordYields = !WorldClock.RecordSeatsExecutive(usa, today) && WorldClock.RecordSeatsExecutive(CountryId.USA);
                            SaveGameService.SaveToFile(saveCount, SaveGameService.CreateSaveGame(sim, w, CountryId.USA, null));
                            cutCount = true;
                        }
                        // PS-6 US-16: the House the world's own count elected on the polling day, seated on 3 January - the record seats no House in this world from then
                        if (today == HouseDay) { houseByDate = usa.UsCongressElections.Count == 1 && Same(usa.ParliamentSeats, usa.UsCongressElections[0].HouseSeats(CountryId.USA)) && !WorldClock.RecordSeatsChamber(usa, today); }
                        if (today == Oath)
                        {
                            sworn = usa.Government;
                            oathDay = played != null && played.Decided() && UsPresidentialElection.Governs(sworn, played) && sworn.FormedOn == Oath;
                        }
                        if (today == Oath.AddDays(1))
                        {
                            SaveGameService.SaveToFile(saveOath, SaveGameService.CreateSaveGame(sim, w, CountryId.USA, null));
                            cutOath = true;
                        }
                    }
                }
                finally { Application.logMessageReceived -= Listen; }
                Check(raised == 1 && raisedOn == Polled, F("(c) a game from the USA's start ({0}) raises its polling day {1} time(s) on the way to {2} - on {3}", Day(start), raised, Day(Oath.AddDays(1)), Day(raisedOn)));
                Check(staged && campaignCounted, "(c) the run-up and the campaign are staged for the USA on the way (PS-6 US-30, LiveCampaignSetup.TryUsa) - the count reads the campaign's final shares, as its log line says");
                string line = lineOnDay == null ? null : logged.FirstOrDefault(l => l.Contains(lineOnDay));
                Check(played != null && usa.UsPresidentialElections.Count == 1 && line != null && logged.Count(l => l.StartsWith("PRESIDENTIAL: USA - the count of ", StringComparison.Ordinal)) == 1
                      && line.Contains(played.Electors[0].Candidate) && line.Contains(played.Electors[0].Votes.ToString(CultureInfo.InvariantCulture)) && line.Contains(played.Electors[1].Votes.ToString(CultureInfo.InvariantCulture)),
                    F("(c) the count held once ({0} contest(s) at the end, {1} on the polling day), its log line naming the winner and the electors: \"{2}\"", usa.UsPresidentialElections.Count, contestsOnDay, line ?? "NOT LOGGED"));
                Check(controllerPath, F("(c) the controller's own election path on the polling day (HoldElectionWithoutTheNight - the warm-up's) records the day by the US method (K74) with the House the day loop elected, no parliamentary chamber counted, and ends nothing: {0}", controllerWhat));
                Check(recordYields, "(c) from the polling day the record seats no president in this world (the country's gate unchanged)");
                Check(houseByDate, F("(c) on {0} the House the world's own count elected on {1} is seated (US-16) - the record seats no House in this world from then", Day(HouseDay), Day(Polled)));
                Check(played == null || !played.Decided() || oathDay,
                    F("(c) the winner sworn on {0} - {1}", Day(Oath), sworn != null ? sworn.Executive + ", \"" + sworn.Basis + "\"" : "no government"));
                sb.Append(F("    read      (c) E1's premise: the game played from {0} on its own economy and seed vintage counts its own - {1}\n", Day(start), played?.Line ?? "no count"));

                // the saves, each loaded INTO THE GAME and stepped a day
                foreach ((string path, string label, bool cut) in new[] { (saveCount, "the day after the count", cutCount), (saveOath, "the day after the oath", cutOath) })
                {
                    if (!cut) { Check(false, F("(c) {0}: no save was cut", label)); continue; }
                    SaveGame save = SaveGameService.LoadFromFile(path);
                    Country saved = save.World.GetCountry(CountryId.USA);
                    object govSaved = saved.Government;
                    bool carried = saved.UsPresidentialElections.Count == 1 && played != null && PresidencyDiagnostic.SameContest(saved.UsPresidentialElections[0], played);
                    (SimulationManager adopted, PoliSim.UI.GameController loaded) = PresidencyDiagnostic.Adopt(save, hosts, out string error);
                    if (adopted == null) { Check(false, F("(c) the save cut {0}: {1}", label, error)); continue; }
                    try
                    {
                        Country u = adopted.World.GetCountry(CountryId.USA);
                        bool asLoaded = ReferenceEquals(u.Government, govSaved);
                        DateTime from = adopted.CurrentDate;
                        Step(adopted, adopted.World);
                        Check(carried && asLoaded && u.UsPresidentialElections.Count == 1 && ReferenceEquals(u.Government, govSaved),
                            F("(c) the save cut {0} ({1}, format {2}) loads into the game - its contest whole, its government kept ({3}); stepped to {4}: nothing counted twice, the same government",
                                label, Day(from), save.SaveVersion, u.Government?.Executive, Day(adopted.CurrentDate)));
                    }
                    finally { EnergyMarket.ResetTurnState(); }
                }
                EnergyMarket.ResetTurnState();
            }

            // ---- (d) R-US3 (a), planted: 269-269 through the count's own path, chosen by the House on the count's day
            var planted = new List<ElectoralCollege.Jurisdiction>();
            foreach (ElectoralCollege.Jurisdiction j in record)
            {
                if (j.Name == "PA" || j.Name == "MI" || j.Name == "WI") { planted.Add(new ElectoralCollege.Jurisdiction(j.Name, j.TotalEv, ElectoralCollege.Democrat)); }
                else if (j.Name == "NE") { planted.Add(new ElectoralCollege.Jurisdiction(j.Name, j.TotalEv, j.StatewideWinner, j.AtLargeElectors, j.DistrictWinners.Select(_ => ElectoralCollege.Republican).ToArray())); }
                else { planted.Add(j); }
            }
            var rep = new UsPresidentialElection.Tally { Candidate = y2024.NomineeR, Party = "REP", Share = 100.0 * fec[ElectoralCollege.ShareRepublican] };
            var dem = new UsPresidentialElection.Tally { Candidate = y2024.NomineeD, Party = "DEM", Share = 100.0 * fec[ElectoralCollege.ShareDemocrat] };
            var others = new UsPresidentialElection.Tally { Candidate = UsPresidentialElection.OtherCandidates, Share = 100.0 * fec[ElectoralCollege.ShareOthers] };
            UsPresidentialElection.Contest tie = UsPresidentialElection.FromCollege(Polled, "planted (UsPresidentialElectionDiagnostic): the record's 2024 college, PA, MI, WI to DEM and NE-2 to REP", planted.ToArray(), rep, dem, others);
            Check(tie.ElectorsOf("REP") == 269 && tie.ElectorsOf("DEM") == 269 && tie.Contingent && !tie.Decided() && tie.MajorityOfElectors() == 270 && tie.CountedOn == counted,
                F("(d) planted: the record's 2024 college with PA, MI and WI to the Democratic nominee and NE-2 to the Republican - {0} - {1}: no majority of {2} (270), contingent, undecided - \"{3}\"", tie.ElectorsOf("REP"), tie.ElectorsOf("DEM"), tie.ElectorsAppointed, tie.Line));
            // the House on the count's day, counted here from the catalog's districts (independently of the count's reader)
            int houseYear = WorldClock.ElectionDayOf(CountryId.USA, WorldClock.SeatedVintage(CountryId.USA, counted)).Year;
            var expected = UsPresidentialReturns.HouseDistricts.Where(h => h.Year == houseYear && (h.Winner == "R" || h.Winner == "D")).GroupBy(h => h.State)
                .Select(g => (State: g.Key, R: g.Count(h => h.Winner == "R"), D: g.Count(h => h.Winner == "D"))).ToList();
            int expR = expected.Count(e => e.R > e.D), expD = expected.Count(e => e.D > e.R), expTied = expected.Count - expR - expD;
            Dictionary<string, int> house119 = PartySystems.InitialSeats(CountryId.USA, ElectionVintage.Usa2024);
            Check(expected.Sum(e => e.R) == house119["REP"] && expected.Sum(e => e.D) == house119["DEM"] && expected.Count == 50,
                F("(d) the House on {0} is the one elected in {1} (seated by date): its districts sum to the seated chamber - REP {2}, DEM {3} - over {4} states; delegations REP {5}, DEM {6}, tied {7} ({8})",
                    Day(counted), houseYear, expected.Sum(e => e.R), expected.Sum(e => e.D), expected.Count, expR, expD, expTied, string.Join(", ", expected.Where(e => e.R == e.D).Select(e => e.State))));
            {
                SimulationManager.SetEpoch(HouseDay.AddDays(1));
                (SimulationManager sim, World w, Country usa) = Open(hosts, CountryId.USA, "REP");
                usa.UsPresidentialElections.Add(tie);
                GovernmentRecord eveGov = usa.Government;
                bool before = true, chose = false, elect = false, oathDay = false;
                for (int d = 0; d < 30 && sim.CurrentDate < Oath.AddDays(1); d++)
                {
                    Step(sim, w);
                    DateTime today = sim.CurrentDate;
                    if (today < counted && (tie.Decided() || tie.Delegations.Count > 0)) { before = false; }
                    if (today == counted)
                    {
                        chose = tie.Delegations.Count == 2 && tie.Delegations.First(t => t.Party == "REP").Votes == expR && tie.Delegations.First(t => t.Party == "DEM").Votes == expD
                                && tie.WinnerParty == (expR >= 26 ? "REP" : expD >= 26 ? "DEM" : null);
                        elect = !tie.Decided() || (GovernmentRecord.RoleOn(usa, tie.WinnerParty, today) == (tie.WinnerParty == "REP" ? PlayerRole.PresidentElect : PlayerRole.PrimeMinister) && ReferenceEquals(usa.Government, eveGov));
                    }
                    if (today == Oath) { oathDay = !tie.Decided() || (UsPresidentialElection.Governs(usa.Government, tie) && usa.Government.Basis.Contains("chosen by the House")); }
                }
                Check(before && chose, F("(d) on {0} the House chooses by states (the day loop's own choice) - {1}", Day(counted), tie.Line));
                Check(elect && oathDay, F("(d) its winner is the president-elect from that day and sworn on {0}: {1}", Day(Oath), usa.Government?.Basis));
                card.Add(F("**R-US3's path, planted** - the record's 2024 college with Pennsylvania, Michigan and Wisconsin to the Democratic nominee and Nebraska's 2nd district to the Republican gives {0} to {1}: no candidate has a majority of the {2} electors appointed. On {3} the House elected in {4} (seated by date) chooses by states - {5} for {6}, {7} for {8}, {9} tied, casting no vote, of {10}; a majority of all the states is {11} - {12}.",
                    tie.ElectorsOf("REP"), tie.ElectorsOf("DEM"), tie.ElectorsAppointed, Long(counted), houseYear, expR, y2024.NomineeR, expD, y2024.NomineeD, expTied, expected.Count, expected.Count / 2 + 1,
                    tie.Decided() ? tie.Winner + " elected, sworn on " + Long(Oath) : "no one elected (the Senate chooses the Vice President, who acts from the oath - R-US22)"));
                card.Add("");
                EnergyMarket.ResetTurnState();
            }
            // the Twelfth Amendment's choice on planted delegations: a tie of states decides nothing; a tied delegation casts no vote
            UsPresidentialElection.Contest Planted() => UsPresidentialElection.FromCollege(Polled, "planted", planted.ToArray(), rep, dem, others);
            int allStates = UsPresidentialElection.AllStates(Polled);
            Check(allStates == 50 && allStates == expected.Count, F("(d) all the states, from the apportionment the catalog holds for 2024's electors (DC excluded): {0} - the Twelfth Amendment's majority is reckoned on them, {1}", allStates, allStates / 2 + 1));
            var even = Enumerable.Range(0, 50).Select(i => ("S" + i.ToString(CultureInfo.InvariantCulture), i < 25 ? 2 : 1, i < 25 ? 1 : 2)).ToList();
            UsPresidentialElection.Contest stuck = Planted();
            UsPresidentialElection.ChooseByStates(stuck, even, allStates, "a planted House", counted);
            var oneTied = Enumerable.Range(0, 50).Select(i => ("S" + i.ToString(CultureInfo.InvariantCulture), i < 26 ? 2 : i < 49 ? 1 : 3, i < 26 ? 1 : i < 49 ? 2 : 3)).ToList();
            UsPresidentialElection.Contest chosen = Planted();
            UsPresidentialElection.ChooseByStates(chosen, oneTied, allStates, "a planted House", counted);
            Check(!stuck.Decided() && stuck.Line.Contains("nothing is decided") && chosen.WinnerParty == "REP" && chosen.Delegations.Sum(t => t.Votes) == 49 && chosen.Line.Contains("1 tied (no vote)"),
                F("(d) planted delegations: 25 states to 25 decide nothing - \"...{0}\"; 26 to 23 with one tied elect REP's nominee, the tied delegation casting no vote", stuck.Line.Substring(Math.Max(0, stuck.Line.Length - 90))));
            // L1 (the review): a majority of ALL the states - 25 REP, 24 DEM and one tied decide nothing, whether the tied state is listed or no delegation of
            // it is listed at all (a state with no member of either party); counted over the states listed, 25 of 49 would have elected
            var tiedListed = Enumerable.Range(0, 50).Select(i => ("S" + i.ToString(CultureInfo.InvariantCulture), i < 25 ? 2 : i < 49 ? 1 : 2, i < 25 ? 1 : 2)).ToList();
            var tiedUnlisted = tiedListed.Take(49).ToList();
            UsPresidentialElection.Contest listed = Planted(), unlisted = Planted();
            string listedLine = UsPresidentialElection.ChooseByStates(listed, tiedListed, allStates, "a planted House", counted);
            string unlistedLine = UsPresidentialElection.ChooseByStates(unlisted, tiedUnlisted, allStates, "a planted House", counted);
            Check(!listed.Decided() && !unlisted.Decided() && listed.Delegations.Sum(t => t.Votes) == 49 && unlisted.Delegations.Sum(t => t.Votes) == 49
                  && listedLine.Contains("1 tied (no vote)") && listedLine.Contains("a majority of all the states is 26") && unlistedLine.Contains("1 with no delegation listed (no vote)")
                  && unlistedLine.Contains("of 50; a majority of all the states is 26") && unlistedLine.EndsWith("nothing is decided", StringComparison.Ordinal),
                F("(d) L1: 25 REP, 24 DEM and one tied decide nothing - the tied state listed (\"...{0}\") and not listed (\"...{1}\"): the majority is 26 of ALL the states",
                    listedLine.Substring(Math.Max(0, listedLine.Length - 110)), unlistedLine.Substring(Math.Max(0, unlistedLine.Length - 130))));

            // ---- (e) the count is pure; (f) a later day
            {
                var poll = new Dictionary<string, double> { { "REP", 0.51 }, { "DEM", 0.49 } };
                Dictionary<SimulationRandom.Stream, int> drawsBefore = SimulationRandom.CaptureDrawCounts();
                UsPresidentialElection.Contest one = UsPresidentialElection.Count(CountryId.USA, Polled, poll, out ElectoralCollege.StateCall[] _, out string _);
                UsPresidentialElection.Contest two = UsPresidentialElection.Count(CountryId.USA, Polled, poll, out ElectoralCollege.StateCall[] _, out string _);
                Dictionary<SimulationRandom.Stream, int> drawsAfter = SimulationRandom.CaptureDrawCounts();
                bool noDraw = drawsBefore.Count == drawsAfter.Count && drawsBefore.All(kv => drawsAfter.TryGetValue(kv.Key, out int n) && n == kv.Value);
                Check(one != null && two != null && PresidencyDiagnostic.SameContest(one, two) && noDraw, "(e) the count is pure: on the same poll it returns the same contest twice, and draws no random number (R-US5: PS-8 needs only its gate)");
                var even2028 = new Dictionary<string, double> { { "REP", 0.5 }, { "DEM", 0.5 } };
                UsPresidentialElection.Contest later = UsPresidentialElection.Count(CountryId.USA, new DateTime(2028, 11, 7), even2028, out ElectoralCollege.StateCall[] _, out string laterWhy);
                bool laterField = later != null && later.Electors.Any(t => t.Party == "REP" && t.Candidate == "Republican Party's candidate (2028)") && later.Electors.Any(t => t.Party == "DEM" && t.Candidate == "Democratic Party's candidate (2028)")
                                  && later.Electors.First(t => t.Party == null).Share == 0.0 && later.DayBasis.Contains("the states from 2024's") && later.DayBasis.Contains("the electors in force in 2024") && !later.DayBasis.Contains("carried forward")
                                  && later.TakesOffice == new DateTime(2029, 1, 20);
                Check(laterField, F("(f) a later day (7 Nov 2028): the field unnamed at E1's future factor and no minor candidate, the record's 2024 states as the prior (a planted count with no game behind it; a game's own later count swings from its own, R-US24 (b)), the 2020 census's electors in force - {0}", later?.Line ?? laterWhy));
                // the review's L7: with no minor candidate the swing closes only approximately (no third column to absorb the floor) - the residual, printed
                double laterResidual = double.NaN;
                if (later != null)
                {
                    double R(string party) => later.Electors.First(t => t.Party == party).Share / 100.0;
                    ElectoralCollege.FromNationalShares(2024, 2024, new[] { R("DEM"), R("REP"), R(null) }, out ElectoralCollege.StateCall[] _, out laterResidual);
                }
                // M2 (the review): the electors carried forward are labelled from the year alone - not in 2024 or 2028 (the 2020 census's, by law), from 2032
                bool carried2032 = UsPresidentialElection.ElectorsCarriedForward(new DateTime(2032, 11, 2), out int carriedYear);
                Check(carried2032 && carriedYear == 2024 && !UsPresidentialElection.ElectorsCarriedForward(Polled, out int _) && !UsPresidentialElection.ElectorsCarriedForward(new DateTime(2028, 11, 7), out int _),
                    F("(f) the electors carried forward, from the year alone (the president row's label): not in 2024 or 2028, from 2032 - the {0} electors carried", carriedYear));
                card.Add(F("**A later day**, on a planted even poll: 7 Nov 2028 counts {0} - the field unnamed (\"Republican Party's candidate (2028)\") at E1's future factor and no minor candidate, the states from the record's 2024 - a planted count with no game behind it (a game's own later count swings from the game's own 2024 states, R-US24 (b), US-26) - the swing closing to within {1:E1} of the national shares (with no minor candidate to absorb the floor it closes only approximately; no call moves), the electors those in force in 2024 (the 2020 census's, by law in force for 2028; carried forward from 2032, R-US13 (a), and labelled so on the president row).",
                    later != null ? later.Electors[0].Votes.ToString(CultureInfo.InvariantCulture) + " to " + later.Electors[1].Votes.ToString(CultureInfo.InvariantCulture) : "nothing", laterResidual));
            }

            // ---- (g) the default-USA paths (risk 13); (h) scope
            {
                var pickHost = new GameObject("UsPresidentialElectionDiagnostic.Prepick"); hosts.Add(pickHost);
                var pickSim = pickHost.AddComponent<SimulationManager>();
                var pickController = pickHost.AddComponent<PoliSim.UI.GameController>();
                PresidencyDiagnostic.ControllerField("_simulationManager").SetValue(pickController, pickSim);
                object fallback = typeof(PoliSim.UI.GameController).GetProperty("PlayerCountryId", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(pickController);
                object selected = PresidencyDiagnostic.ControllerField("_selectedPlayerCountryId")?.GetValue(pickController);
                Check(fallback is CountryId f && f == CountryId.USA && selected == null && !pickSim.PlayerCountryId.HasValue,
                    F("(g) before a pick the controller falls back to {0} while nothing is selected ({1}) and the manager holds no player country ({2}) - the controller's day loop returns until a country is selected, and the count's gate is the manager's player country: no default world holds a US count",
                        fallback, selected ?? "null", pickSim.PlayerCountryId.HasValue ? pickSim.PlayerCountryId.ToString() : "none"));
                SimulationManager.SetEpoch(Polled.AddDays(-1));
                (SimulationManager none, World noneWorld, Country noneUsa) = Open(hosts, null, null);
                GovernmentRecord noneGov = noneUsa.Government;
                for (int d = 0; d < 2; d++) { Step(none, noneWorld); }
                // PS-8 (K243, R-US5 (a)): the AI USA counts its own polling day in every world, by the idle game - the contest dated the day, the president of record in office until the oath
                Check(noneUsa.UsPresidentialElections.Count == 1 && noneUsa.UsPresidentialElections[0].PollingDay.Date == Polled.Date && ReferenceEquals(noneUsa.Government, noneGov),
                    F("(g) a world with no player country (the seeded shadow's, the dump's), stepped across {0}, holds the AI USA's count on its day by the idle game (PS-8, K243) and keeps its president until the oath - {1}",
                        Day(Polled), noneUsa.UsPresidentialElections.Count > 0 ? noneUsa.UsPresidentialElections[0].Line : "NO COUNT"));
                SimulationManager.SetEpoch(Polled.AddDays(-1));
                (SimulationManager ger, World gerWorld, Country gerUsa) = Open(hosts, CountryId.Germany, "CDU");
                for (int d = 0; d < 2; d++) { Step(ger, gerWorld); }
                Check(gerUsa.UsPresidentialElections.Count == 1 && gerUsa.UsPresidentialElections[0].PollingDay.Date == Polled.Date && ger.LastPlayerCount == null,
                    F("(h) a world whose player is Germany (CDU), stepped across {0}, holds the AI USA's count on its day - PS-8's (R-US5 (a)) - and no count of the player's own - {1}",
                        Day(Polled), gerUsa.UsPresidentialElections.Count > 0 ? gerUsa.UsPresidentialElections[0].Line : "NO COUNT"));
                EnergyMarket.ResetTurnState();
                // the film driver: its default country, and the polling days its warm-up's window holds from the USA's start
                var driverHost = new GameObject("UsPresidentialElectionDiagnostic.Driver"); hosts.Add(driverHost);
                var driver = driverHost.AddComponent<PoliSim.Testing.UiScreenshotDriver>();
                const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic;
                object driverDefault = typeof(PoliSim.Testing.UiScreenshotDriver).GetField("_countryId", Hidden)?.GetValue(driver);
                object minDays = typeof(PoliSim.Testing.UiScreenshotDriver).GetField("MinWarmupDays", Hidden)?.GetRawConstantValue();
                object maxDays = typeof(PoliSim.Testing.UiScreenshotDriver).GetField("MaxWarmupDays", Hidden)?.GetRawConstantValue();
                DateTime usStart = WorldClock.StartDate(CountryId.USA);
                var inWindow = new List<string>();
                if (minDays is int lo && maxDays is int hi)
                {
                    for (DateTime from = usStart.AddDays(1); WorldClock.TryNextPollingDay(CountryId.USA, from, out DateTime p) && p <= usStart.AddDays(hi); from = p.AddDays(1))
                    {
                        int dayN = (int)(p - usStart).TotalDays;
                        inWindow.Add(F("{0} (day {1}{2})", Day(p), dayN, dayN <= lo ? ", inside every warm-up" : ", only where the warm-up runs past it"));
                    }
                }
                Check(driverDefault is CountryId dc && dc == CountryId.USA && minDays is int && maxDays is int && inWindow.Count > 0 && inWindow[0].StartsWith(Day(Polled), StringComparison.Ordinal),
                    F("(g) the film driver's default country is {0} (-shotcountry= unset), played from the USA's start {1}; its warm-up (from {2} to {3} days, stopping on a preliminary release) holds the polling days {4} - each counted as play counts it (the warm-up calls HoldElectionWithoutTheNight, measured in (c)): by design, not by accident; the USA's filmed frames after the warm-up show the game's own president",
                        driverDefault, Day(usStart), minDays, maxDays, string.Join("; ", inWindow)));
            }

            // ---- (i) US-8b (R-US25, ruled: "(a), with every fork drawing the live game's election noise (common random numbers)"): the election's noise is common
            // to every world of a game, and a fork with no policy difference counts the live game's contest
            {
                SimulationManager.SetEpoch(WorldClock.StartDate(CountryId.USA));
                (SimulationManager live, World liveWorld, Country liveUsa) = Open(hosts, CountryId.USA, "REP");
                var none = new Dictionary<CountryId, PolicyDecision>();
                foreach (Country each in liveWorld.Countries) { none[each.Id] = PolicyDecision.None(); }
                double[] Draws(CountryId id, DateTime day) { System.Random r = SimulationRandom.ElectionNoiseOn(id, day); return new[] { r.NextDouble(), r.NextDouble(), r.NextDouble(), r.NextDouble() }; }
                string Show(double[] xs) => string.Join(", ", xs.Select(x => x.ToString("0.000000", CultureInfo.InvariantCulture)));

                // the source: the live game's draws for its polling day, a fork's generator (the save's seed, its counted streams elsewhere), another day, country, game
                SimulationRandom.LiveState real = SimulationRandom.CaptureLive();
                double[] liveDraws, forkDraws, otherDay, otherCountry, otherGame;
                bool uncounted;
                var plantedPoll = new Dictionary<string, double> { { "REP", 0.50 }, { "DEM", 0.48 } };
                Dictionary<string, double> noisyLive, noisyFork;
                try
                {
                    Dictionary<SimulationRandom.Stream, int> countsBefore = SimulationRandom.CaptureDrawCounts();
                    liveDraws = Draws(CountryId.USA, Polled);
                    noisyLive = UsPresidentialElection.WithNoise(plantedPoll, SimulationRandom.ElectionNoiseOn(CountryId.USA, Polled), RegionalAggregation.RegionalNoiseSigmaPp);
                    Dictionary<SimulationRandom.Stream, int> countsAfter = SimulationRandom.CaptureDrawCounts();
                    uncounted = countsBefore.Count == countsAfter.Count && countsBefore.All(kv => countsAfter.TryGetValue(kv.Key, out int n) && n == kv.Value);
                    var forkCounts = new Dictionary<SimulationRandom.Stream, int>(countsBefore);
                    forkCounts[SimulationRandom.Stream.ElectionNoise] = 5;   // a fork that drew on the counted stream where the live game did not
                    forkCounts[SimulationRandom.Stream.Event] = (forkCounts.TryGetValue(SimulationRandom.Stream.Event, out int ev) ? ev : 0) + 7;
                    SimulationRandom.RestoreState(SimulationRandom.MasterSeed, forkCounts);
                    forkDraws = Draws(CountryId.USA, Polled);
                    noisyFork = UsPresidentialElection.WithNoise(plantedPoll, SimulationRandom.ElectionNoiseOn(CountryId.USA, Polled), RegionalAggregation.RegionalNoiseSigmaPp);
                    otherDay = Draws(CountryId.USA, UsPresidentialElection.PresidentialDay(2028));
                    otherCountry = Draws(CountryId.Poland, Polled);
                    SimulationRandom.Seed(Seed + 1);
                    otherGame = Draws(CountryId.USA, Polled);
                }
                finally { SimulationRandom.Reinstate(real); }
                Check(uncounted && liveDraws.SequenceEqual(forkDraws) && !liveDraws.SequenceEqual(otherDay) && !liveDraws.SequenceEqual(otherCountry) && !liveDraws.SequenceEqual(otherGame),
                    F("(i) the election's noise is common to every world of the game (R-US25, ruled): the live game's draws for {0} ({1}) equal a fork generator's whose counted streams stand elsewhere ({2}); another polling day, another country and another game's seed draw otherwise ({3}; {4}; {5}); no counted stream moves",
                        Day(Polled), Show(liveDraws), Show(forkDraws), Show(otherDay), Show(otherCountry), Show(otherGame)));
                UsPresidentialElection.Contest liveNoisy = UsPresidentialElection.Count(CountryId.USA, Polled, noisyLive, out ElectoralCollege.StateCall[] _, out string whyLive);
                UsPresidentialElection.Contest forkNoisy = UsPresidentialElection.Count(CountryId.USA, Polled, noisyFork, out ElectoralCollege.StateCall[] _, out string _);
                UsPresidentialElection.Contest quiet = UsPresidentialElection.Count(CountryId.USA, Polled, plantedPoll, out ElectoralCollege.StateCall[] _, out string _);
                Check(liveNoisy != null && forkNoisy != null && quiet != null && PresidencyDiagnostic.SameContest(liveNoisy, forkNoisy) && !liveNoisy.Electors[0].Share.Equals(quiet.Electors[0].Share),
                    F("(i) at §27's σ ({0:0.0} pp, a test figure - the count's σ in play is {1:0.0}, DECLARED until noise is on in play) the live game's and the fork's counts on one planted poll (REP 50 / DEM 48) are one contest - \"{2}\" - where the noiseless count reads \"{3}\"",
                        RegionalAggregation.RegionalNoiseSigmaPp, UsPresidentialElection.NoiseSigmaPp, liveNoisy?.Line ?? whyLive, quiet?.Line ?? "no count"));

                // the fork: the impact ledger's (ShadowBaseline from the game in progress, on its own copy of the streams), advanced with the live game's decisions
                ShadowBaseline fork = null;
                try
                {
                    fork = new ShadowBaseline(live, liveWorld, CountryId.USA);
                    EnergyMarket.ResetTurnState();
                    for (int d = 0; d < SimulationManager.DaysPerTurn; d++) { live.AdvanceDay(); }
                    live.AdvanceTurn(none);
                    EnergyMarket.ResetTurnState();   // the market's turn state is static: each world starts its turn from the same one
                    fork.AdvanceTurn(none);
                    Country forkUsa = fork.CountryFor(CountryId.USA);
                    UsPresidentialElection.Contest a = liveUsa.UsPresidentialElections.FirstOrDefault(k => k.PollingDay == Polled), b = forkUsa?.UsPresidentialElections.FirstOrDefault(k => k.PollingDay == Polled);
                    UsPresidentialElection.Tally aRep = a?.Electors.FirstOrDefault(t => t.Party == "REP"), bRep = b?.Electors.FirstOrDefault(t => t.Party == "REP");
                    Check(a != null && b != null && liveUsa.UsPresidentialElections.Count == 1 && forkUsa.UsPresidentialElections.Count == 1 && PresidencyDiagnostic.SameContest(a, b) && UsTermLimit.WinnerOf(a) == UsTermLimit.WinnerOf(b),
                        F("(i) a fork with no policy difference (forked at the USA's start {0}, both worlds a turn of {1} days on the same decisions) counts its own election and elects the same president with identical shares: live \"{2}\" (REP {3:0.000000} %), the fork \"{4}\" (REP {5:0.000000} %)",
                            Day(WorldClock.StartDate(CountryId.USA)), SimulationManager.DaysPerTurn, a?.Line ?? "no count", aRep?.Share ?? double.NaN, b?.Line ?? "no count", bRep?.Share ?? double.NaN));
                }
                finally { fork?.Dispose(); EnergyMarket.ResetTurnState(); }
            }

            // ---- the card's block
            card.Insert(0, "");
            card.Insert(0, F("**The dates**: the polling day {0} (3 U.S.C. §21(1)), the electors meet {1} (§7), Congress counts {2} (§15(a)), the oath {3} (the Twentieth Amendment §1) - NARA's key dates for 2024.",
                Long(Polled), Long(meet), Long(counted), Long(oath)));
            card.Insert(0, "");
            card.Insert(0, F("**Every other candidate stands only where on the ballot of record** (Elias's ruling J3, 2026-10-19; US-6c): each column of the FEC's workbook but the nominees' takes its part of the count's share of every other candidate (by its national vote) only in the jurisdictions whose ballot listed it - the workbook's roman cells, by its note that italics indicate write-in votes - spread over them in the record's own pattern there (its share of record of each one's vote, renormalised to its national share on the count's weights, 2020's votes cast); a write-in count gives no standing, and its votes go where the column stands; the scattered write-ins, on no ballot, stand where the workbook reports them. Jurisdictions listing {0}. The two nominees share the rest of each jurisdiction by the normalised uniform swing on their two-party split of 2020 (R-US14 (a)), each jurisdiction weighing its 2020 votes cast less every other candidate's part. Against the record's 2024 shares of every other candidate the largest gap is {1:0.000} pp ({2}), the mean {3:0.000} pp.",
                string.Join(", ", standingCounts), recordWorst, recordWorstAt, recordMean));
            card.Insert(0, "");
            card.Insert(0, F("Its states against US-4's (a) column at the true national shares (*The readings*, above): {1} calls apart, the largest margin difference {0:0.000000} pp ({2}) - the difference is the count's form on the ballots of record: (a) swings all three columns, every other candidate pooled, from 2020's states, where the count stands every other candidate only where listed (below) and swings the two nominees on their two-party split over the rest. The stored contest re-derives these states exactly from the national shares it keeps, so the save carries no per-state field (format 39, the step US-7 took for the pair).",
                widest, callsApart, widestAt));
            card.Insert(0, "");
            card.Insert(0, F("States called wrong: {0}. Districts called wrong: {1}.", wrongStateText, wrongDistrictText));
            card.Insert(0, "");
            card.Insert(0, F("**E1's acceptance for the USA** - the reference world (US-6's: the epoch the eve of {0}, one day stepped by the game's loop) holds its own count on the poll E1's factors are fitted to, and {1}: **{2} to {3}** electors of {4}, against the record's **{5} to {6}** as appointed. Its national vote is the FEC's by construction - REP {7:0.00} %, DEM {8:0.00} %, every other candidate {9:0.00} % (the largest gap {10:0.0000} pp).",
                Long(Polled), trump ? "elects " + y2024.NomineeR + " (REP)" : "does NOT elect the Republican nominee", countR, countD, c?.ElectorsAppointed, recR, recD, countRShare, countDShare, countOShare, nationalGap));
            card.Add("");
            card.Add("**Not modelled, stated**: faithless electors; Maine's ranked-choice count; a single minor candidate's win of a state (the states pool every other candidate - the count names a state where they lead together); the Vice President's own electoral count (the tickets are one: where the House cannot choose, the Senate chooses the Vice President, who acts from the oath - R-US22, RULED, held by PresidencyDiagnostic).; a minor candidate's own swing (every other candidate stands at the record's pattern where on the ballot of record, scaled with the field).");
            card.Add("");
            card.Add("The rulings it runs on: R-US1 (a), R-US3 (a) and R-US13 (a) (Elias's ruling F8), R-US14 (a) (G2) and J3's minor candidates on the ballot of record, each RULED; R-US11 (a) and R-US24 (b), RULED, are US-26's, built: a game's own later count swings its states from its own previous count's (HistoryRolledForwardDiagnostic (d)).");

            // the stamp: the catalog's digests (states, districts, years, candidates and their jurisdictions, House districts) and the stored factors
            var stamp = new StringBuilder(UsPresidentialReturns.YearSourceDigest + UsPresidentialReturns.StateSourceDigest + UsPresidentialReturns.DistrictSourceDigest + UsPresidentialReturns.CandidateSourceDigest + UsPresidentialReturns.CandidateStateSourceDigest + UsPresidentialReturns.HouseDistrictSourceDigest);
            foreach (PresidencyOfRecord.CandidateOfRecord candidate in PresidencyOfRecord.CandidatesOf(CountryId.USA, 2025)) { if (candidate.BackingParty != null) { stamp.Append(candidate.Surname).Append(candidate.Factor.ToString("R", CultureInfo.InvariantCulture)); } }
            return Digest(stamp.ToString());
        }

        /// <summary>The FEC's 2024 national shares as the states pool them - the Democratic nominee, the Republican, every other candidate (the catalog's state sums,
        /// US-4's National).</summary>
        private static double[] National2024()
        {
            double d = 0, r = 0, o = 0, t = 0;
            foreach (var s in UsPresidentialReturns.States) { if (s.Year == 2024) { d += s.VotesD; r += s.VotesR; o += s.VotesOther; t += s.VotesTotal; } }
            return new[] { d / t, r / t, o / t };
        }

        /// <summary>US-6's reference world for the USA, as `UsPresidentialFactorCheck` holds it: seed 777, the energy calibration reset, the host kept for disposal.</summary>
        private static PresidentialReferenceWorld.Result HoldReference(List<GameObject> hosts)
        {
            var host = new GameObject("UsPresidentialElectionDiagnostic.Reference"); hosts.Add(host);
            SimulationRandom.Seed(Seed);
            EnergyMarket.ResetCalibration();
            try { return PresidentialReferenceWorld.Hold(host, CountryId.USA, null); }
            finally { EnergyMarket.ResetTurnState(); }
        }

        /// <summary>A world on the epoch already set, as a new game builds one: the player's country and party, or none.</summary>
        private static (SimulationManager Sim, World World, Country Usa) Open(List<GameObject> hosts, CountryId? player, string party)
        {
            SimulationRandom.Seed(Seed);
            EnergyMarket.ResetCalibration();
            World w = WorldFactory.CreateDefault();
            var go = new GameObject("UsPresidentialElectionDiagnostic"); hosts.Add(go);
            var s = go.AddComponent<SimulationManager>();
            s.SetWorld(w);
            if (player.HasValue) { s.PlayerCountryId = player.Value; w.GetCountry(player.Value).PlayerPartyAbbrev = party; }
            return (s, w, w.GetCountry(CountryId.USA));
        }

        /// <summary>A controller in edit mode driving <paramref name="sim"/> on <paramref name="usa"/> - its election path reached by name (PresidencyDiagnostic's way).</summary>
        private static PoliSim.UI.GameController BindController(List<GameObject> hosts, SimulationManager sim, Country usa)
        {
            var go = new GameObject("UsPresidentialElectionDiagnostic.Controller"); hosts.Add(go);
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

        private static bool Same(IReadOnlyDictionary<string, int> a, IReadOnlyDictionary<string, int> b) => a != null && b != null && a.Count == b.Count && a.All(kv => b.TryGetValue(kv.Key, out int n) && n == kv.Value);

        // ---- the US model card's US-8 block (docs/reference/US_ELECTIONS.md) - written by WriteReadings

        private const string CardRelative = "docs/reference/US_ELECTIONS.md";
        private const string CardStamp = "<!-- GENERATED by PoliSim.EditorTools.UsPresidentialElectionDiagnostic.WriteReadings. DO NOT EDIT BY HAND. source-digest: ";
        private const string CardEnd = "<!-- END GENERATED -->";

        private static string PathOf(string relative) => Path.Combine(Path.GetFullPath(Path.Combine(Application.dataPath, "..")), relative);

        private static string Digest(string text)
        {
            using (var sha = SHA256.Create()) { return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(text))).Replace("-", string.Empty).ToLowerInvariant(); }
        }

        private static string CardBlock(List<string> lines, string digest)
        {
            var block = new StringBuilder(CardStamp).Append(digest).Append(" -->\n");
            foreach (string line in lines) { block.Append(line).Append('\n'); }
            return block.Append(CardEnd).ToString();
        }

        private static string CardBlockOf(string card)
        {
            string text = card.Replace("\r\n", "\n");
            int start = text.IndexOf(CardStamp, StringComparison.Ordinal);
            int end = start < 0 ? -1 : text.IndexOf(CardEnd, start, StringComparison.Ordinal);
            return end < 0 ? null : text.Substring(start, end + CardEnd.Length - start);
        }

        private static string F(string format, params object[] args) => string.Format(CultureInfo.InvariantCulture, format, args);
    }
}
