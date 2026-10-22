using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using PoliSim.Data;
using PoliSim.Elections;
using PoliSim.Elections.Generated;
using PoliSim.Simulation;
using UnityEngine;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// PS-6 US-26 (`docs/specs/USA_STAGE_PLAN.md`, "The history rolled forward (cross-country)"; R-US11 (a), RULED - F8; R-US24 (b), RULED - I8): THE
    /// ROLLED-HISTORY DIAGNOSTIC. The plan's done-when: "a diagnostic shows a US world past 5 Nov 2024 predicting 2026 from the game's own 2024, and Sweden's,
    /// Germany's and Poland's second elections reading their own" - and R-US24's line: "a US game's second presidential count swings its states from the game's
    /// own previous count, its states printed beside the record's 2024 map".
    /// <list type="bullet">
    /// <item>(a) inertness: a world on each of the four starts holds no count of its own, and its history, its prediction and (Sweden, Germany, the USA) its staged
    /// campaign handed the played country are bit for bit what they are without it - the reason the dump cannot move (it holds no election; the integrator's
    /// dump diff proves it);</item>
    /// <item>(b) Sweden, Germany and Poland, each on its own start: the game's first count (the prediction on its polling day counted by the country's own
    /// procedure, as the controller counts it with no campaign) is not read on its own day, and is read after it - the second election's prior the count at the
    /// record's roster total, its loyalty pair the count against the record's last election; a second count makes the third election read the game's own two;
    /// the prediction moves; Sweden's and Germany's staged campaigns stage the same prior. Germany's calendar holds no election after its snap (WorldClock), so its
    /// second election is read on a probe date four years on - the reader, not a polling day;</item>
    /// <item>(c) the USA: the eve world (epoch 4 Nov 2024, seed 777, the USA the player's country, REP) stepped through 5 Nov 2024 holds its own count; on 3 Nov
    /// 2026 (2 U.S.C. §7's day - the midterm itself is US-27's) the poll reads the game's own 2024 House against the record's last, and moves off the epoch's;</item>
    /// <item>(d) R-US24 (b): the game's 2028 count (the day loop's own hold, then removed) swings its states from the game's own 2024 states - each state's prior
    /// the game's 2024 call, re-derived from the stored contest - printed state by state beside the record's 2024 map and the count the record's states would give; the form a stored contest re-derives in is its own basis's, whatever order the list holds the contests in, and a 2028 count made on the record's states (as a build before US-26 made one) re-derives on them.</item>
    /// </list>
    /// Registered in the cheap bar (CheckSuite); it writes nothing.
    /// </summary>
    public static class HistoryRolledForwardDiagnostic
    {
        /// <summary>CONVENTION: the eve world's epoch, the day of the count and the seed - CongressElectionDiagnostic's, so the two read one world.</summary>
        private static readonly DateTime Eve = new DateTime(2024, 11, 4);
        private static readonly DateTime Polled = new DateTime(2024, 11, 5);
        /// <summary>2 U.S.C. §7: the Tuesday next after the first Monday in November of every even year - 3 Nov 2026, the day the reader is asked about.</summary>
        private static readonly DateTime Midterm2026 = new DateTime(2026, 11, 3);
        /// <summary>CONVENTION: the master seed of the eve world.</summary>
        private static readonly int Seed = 777;

        public static void Run()
        {
            CheckExit.ArmLogFold();
            var sb = new StringBuilder("=== HistoryRolledForwardDiagnostic (PS-6 US-26): the game's own counts become the next election's prior and loyalty pair ===\n");
            int failures = 0;
            void Check(bool ok, string what) { if (!ok) { failures++; } sb.Append(ok ? "    ok        " : "    FAIL      ").Append(what).Append('\n'); }
            var hosts = new List<GameObject>();
            int seedWas = SimulationRandom.MasterSeed;
            Dictionary<SimulationRandom.Stream, int> drawsWas = SimulationRandom.CaptureDrawCounts();
            double[][] regionalShares = NationalElection.LastRegionalShares;
            string[] regionalNames = NationalElection.LastRegionalNames;
            double[] regionalWeights = NationalElection.LastRegionalWeights;
            double regionalWorst = NationalElection.LastRegionalWorstAbsError;
            IReadOnlyList<string> regionalKeys = NationalElection.LastRegionalKeys;
            try
            {
                using (SimulationManager.EpochScope())
                {
                    Inertness(sb, Check);
                    foreach (CountryId id in new[] { CountryId.Sweden, CountryId.Germany, CountryId.Poland }) { SecondElection(id, sb, Check); }
                    UnitedStates(sb, Check, hosts);
                }
            }
            catch (Exception e)
            {
                failures++;
                sb.Append("    THREW: " + e.GetType().Name + ": " + e.Message + "\n" + e.StackTrace + "\n");
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
            }
            if (failures > 0)
            {
                Debug.LogError($"HISTORY ROLLED FORWARD: {failures} failure(s).\n{sb}");
                CheckExit.Finish(1);
                return;
            }
            sb.Append("=== HistoryRolledForwardDiagnostic: all checks passed ===");
            Debug.Log(sb.ToString());
            CheckExit.Finish(0);
        }

        // ------------------------------------------------------------------ (a) a world with no count of its own reads what it read before

        private static void Inertness(StringBuilder sb, Action<bool, string> Check)
        {
            foreach (CountryId id in new[] { CountryId.Sweden, CountryId.Germany, CountryId.Poland, CountryId.USA })
            {
                WorldClock.ApplyStart(id);
                Country country = Fresh(id);
                DateTime start = WorldClock.StartDate(id);
                DateTime probe = start.AddYears(4);
                bool plain = PartySystems.TryHistory(id, out double[] l0, out double[] p0);
                bool rolled = PartySystems.TryHistory(id, country, probe, out double[] l1, out double[] p1, out int own);
                bool predicted0 = NationalElection.TryPredictShares(id, out Dictionary<string, double> s0, null, probe);
                bool predicted1 = NationalElection.TryPredictShares(id, out Dictionary<string, double> s1, null, probe, country);
                bool sameStaging = true;
                string staging = string.Empty;
                if (id == CountryId.Sweden || id == CountryId.Germany || id == CountryId.USA)   // PS-6 US-30: the USA's campaign is staged too (LiveCampaignSetup.TryUsa), on the same calendar
                {
                    CampaignCalendar cal = CampaignCalendar.FromWorldStart(NextAfter(id, start, probe), start);
                    bool a = LiveCampaignSetup.TryFor(id, new (int, int, Scandal)[0], cal, out CampaignRun.Setup u0, out string n0, onVoteModelCompatibility: true);
                    bool b = LiveCampaignSetup.TryFor(id, new (int, int, Scandal)[0], cal, out CampaignRun.Setup u1, out string n1, onVoteModelCompatibility: true, played: country);
                    sameStaging = a && b && Equal(u0.PriorShares, u1.PriorShares) && Equal(u0.LoyaltyPerParty, u1.LoyaltyPerParty) && n0 == n1;
                    staging = ", and its campaign staged for " + Day(cal.ElectionDate) + " the same, its note included";
                }
                Check(plain && rolled && own == 0 && Equal(l0, l1) && Equal(p0, p1) && predicted0 && predicted1 && Same(s0, s1) && sameStaging,
                    F("(a) {0}: a world from its start ({1}) holds no count of its own, and handed to the reader on {2} its history, its prediction{3} are bit for bit the record's pair's",
                        id, Day(start), Day(probe), staging));
            }
        }

        // ------------------------------------------------------------------ (b) Sweden's, Germany's and Poland's second elections

        private static void SecondElection(CountryId id, StringBuilder sb, Action<bool, string> Check)
        {
            WorldClock.ApplyStart(id);
            Country country = Fresh(id);
            DateTime start = WorldClock.StartDate(id);
            if (!WorldClock.TryNextPollingDay(id, start, out DateTime first)) { Check(false, F("(b) {0}: no polling day after its start {1}", id, Day(start))); return; }
            DateTime second = NextAfter(id, first.AddDays(1), first.AddYears(4));
            DateTime third = NextAfter(id, second.AddDays(1), second.AddYears(4));
            bool secondOnCalendar = WorldClock.TryNextPollingDay(id, first.AddDays(1), out DateTime onCalendar) && onCalendar == second;
            string[] keys = PartySystems.For(id).Select(p => p.Abbrev).ToArray();
            PartySystems.TryHistory(id, out double[] recordLatest, out double[] recordPrevious);
            double scale = recordLatest.Sum();

            // the game's first count: the prediction on its polling day, counted by the country's own procedure (the controller's no-campaign path)
            bool firstPredicted = NationalElection.TryPredictShares(id, out Dictionary<string, double> firstShares, null, first, country);
            ElectionRecord firstCount = firstPredicted ? NationalElection.Run(id, 0, firstShares, first) : null;
            if (firstCount == null || firstCount.Method == ElectionMethod.NotImplemented) { Check(false, F("(b) {0}: the first count on {1} could not be made", id, Day(first))); return; }
            country.ElectionHistory.Add(firstCount);
            double[] firstVector = keys.Select(k => firstCount.Shares.TryGetValue(k, out double s) ? s : 0.0).ToArray();

            PartySystems.TryHistory(id, country, first, out double[] onDayLatest, out double[] onDayPrevious, out int onDayOwn);
            Check(onDayOwn == 0 && Equal(onDayLatest, recordLatest) && Equal(onDayPrevious, recordPrevious),
                F("(b) {0}: the game's count of {1} is not read on its own day - the election held that day read the record's pair", id, Day(first)));

            bool read = PartySystems.TryHistory(id, country, second, out double[] latest, out double[] previous, out int own);
            bool exact = read && own == 1 && Equal(previous, recordLatest) && latest.Length == firstVector.Length && latest.Select((v, i) => v == firstVector[i] * scale).All(x => x);
            NationalElection.TryPredictShares(id, out Dictionary<string, double> epochPrediction, null, second);
            NationalElection.TryPredictShares(id, out Dictionary<string, double> ownPrediction, null, second, country);
            double moved = keys.Max(k => Math.Abs(ownPrediction[k] - epochPrediction[k]));
            Check(exact && moved > 0.0,
                F("(b) {0}: the second election ({1}{2}) reads the game's own count of {3} as its prior - {4} - at the record's roster total {5:0.00}, its loyalty pair that count against the record's last election; the prediction moves off the epoch's history by up to {6:0.00} pp: {7}",
                    id, Day(second), secondOnCalendar ? ", on the calendar" : ", a probe date - the calendar holds no election after the first", Day(first), Shares(keys, firstVector),
                    scale, 100.0 * moved, string.Join(", ", keys.Select(k => k + " " + Pct(epochPrediction[k]) + " to " + Pct(ownPrediction[k])))));
            if (id == CountryId.Sweden || id == CountryId.Germany)
            {
                CampaignCalendar cal = CampaignCalendar.FromWorldStart(second, start);
                bool staged = LiveCampaignSetup.TryFor(id, new (int, int, Scandal)[0], cal, out CampaignRun.Setup setup, out string note, onVoteModelCompatibility: true, played: country);
                double sum = latest.Sum();
                double[] cast = setup.PriorShares;
                string[] castKeys = staged ? setup.Parties.Select(p => p.Name).ToArray() : new string[0];
                bool samePrior = staged && castKeys.Length == keys.Length && castKeys.Select((k, i) => Math.Abs(cast[i] - latest[Array.IndexOf(keys, k)] / sum) < 1e-12).All(x => x)
                                 && note.Contains("(the history rolled forward)");
                Check(samePrior, F("(b) {0}: the campaign staged for {1} stages the same prior - the game's own count, normalised - and its note says so", id, Day(second)));
            }

            // the game's second count: the third election reads the game's own two
            ElectionRecord secondCount = NationalElection.Run(id, 0, ownPrediction, second);
            country.ElectionHistory.Add(secondCount);
            double[] secondVector = keys.Select(k => secondCount.Shares.TryGetValue(k, out double s) ? s : 0.0).ToArray();
            bool readThird = PartySystems.TryHistory(id, country, third, out double[] latest3, out double[] previous3, out int own3);
            bool both = readThird && own3 == 2 && latest3.Select((v, i) => v == secondVector[i] * scale).All(x => x) && previous3.Select((v, i) => v == firstVector[i] * scale).All(x => x);
            Check(both, F("(b) {0}: after a second count (on {1}) the next election ({2}) reads the game's own two - {3} against {4}", id, Day(second), Day(third), Day(second), Day(first)));
        }

        // ------------------------------------------------------------------ (c) and (d) the USA

        private static void UnitedStates(StringBuilder sb, Action<bool, string> Check, List<GameObject> hosts)
        {
            SimulationRandom.Seed(Seed);
            EnergyMarket.ResetCalibration();
            SimulationManager.SetEpoch(Eve);
            World w = WorldFactory.CreateDefault();
            var go = new GameObject("HistoryRolledForwardDiagnostic"); hosts.Add(go);
            var sim = go.AddComponent<SimulationManager>();
            sim.SetWorld(w);
            sim.PlayerCountryId = CountryId.USA;
            Country usa = w.GetCountry(CountryId.USA);
            usa.PlayerPartyAbbrev = "REP";
            typeof(SimulationManager).GetMethod("SeatTheSecondChamberOfRecord", BindingFlags.Instance | BindingFlags.NonPublic)?.Invoke(sim, null);
            for (int d = 0; d < 10 && sim.CurrentDate <= Polled; d++) { Step(sim, w); }
            UsCongressElection.Contest house = usa.UsCongressElections.FirstOrDefault(c => c.PollingDay == Polled);
            UsPresidentialElection.Contest president = usa.UsPresidentialElections.FirstOrDefault(c => c.PollingDay == Polled);
            Check(house != null && president != null && sim.CurrentDate > Polled,
                F("(c) the eve world ({0}, seed {1}, the USA the player's country, REP) stepped to {2} holds its own count of {3}: {4}", Day(Eve), Seed, Day(sim.CurrentDate), Day(Polled), house?.Line ?? "NO CONGRESS COUNT"));
            if (house == null || president == null) { return; }

            string[] keys = PartySystems.For(CountryId.USA).Select(p => p.Abbrev).ToArray();
            int rep = Array.IndexOf(keys, "REP"), dem = Array.IndexOf(keys, "DEM");
            PartySystems.TryHistory(CountryId.USA, out double[] recordLatest, out double[] recordPrevious);
            PartySystems.TryUsHistoryYears(WorldClock.Resolve(CountryId.USA, ElectionVintage.Seated), out int t1, out int t2);   // the years of the pair the epoch seats
            double scale = recordLatest.Sum();
            bool read = PartySystems.TryHistory(CountryId.USA, usa, Midterm2026, out double[] latest, out double[] previous, out int own);
            bool exact = read && own == 1 && Equal(previous, recordLatest) && latest[rep] == house.V * scale && latest[dem] == (1.0 - house.V) * scale;
            NationalElection.TryPredictShares(CountryId.USA, out Dictionary<string, double> epochPoll, null, Midterm2026);
            NationalElection.TryPredictShares(CountryId.USA, out Dictionary<string, double> ownPoll, null, Midterm2026, usa);
            double vEpoch = UsCongressElection.TwoPartyV(epochPoll), vOwn = UsCongressElection.TwoPartyV(ownPoll);
            Check(exact && vOwn != vEpoch,
                F("(c) on {0} the poll reads the game's own 2024 House (V {1:0.0000}, REP {2:0.0000} and DEM {3:0.0000} of all votes at the record's REP-and-DEM total {4:0.0000}) as its prior, its loyalty that count against the record's {5} (REP {6:0.0000}, DEM {7:0.0000}); the epoch's history read {5} against {8} - the 2026 poll's V {9:0.0000} on the epoch's history, {10:0.0000} on the game's own",
                    Day(Midterm2026), house.V, latest[rep], latest[dem], scale, t1, recordLatest[rep], recordLatest[dem], t2, vEpoch, vOwn));

            // (d) R-US24 (b): the game's 2028 count on its own 2024 states, held by the day loop's own hold and removed after
            DateTime day2028 = UsPresidentialElection.PresidentialDay(2028);
            var poll = new Dictionary<string, double>(ownPoll);
            ElectoralCollege.StateCall[] game2024 = UsPresidentialElection.StatesOf(usa, president);
            ElectoralCollege.StateCall[] record2024 = UsPresidentialElection.StatesOnRecord(president);
            bool firstOnRecord = game2024 != null && record2024 != null && game2024.Length == record2024.Length
                                 && game2024.Zip(record2024, (a, b) => a.State == b.State && Equal(a.Shares, b.Shares)).All(x => x);
            Check(firstOnRecord, "(d) the game's first presidential count (2024) re-derives on the record's states - the chain starts from the record");
            IReadOnlyList<UsTermLimit.Nominee> nominees = UsTermLimit.NomineesFor(usa, day2028);   // read before the hold adds its contest, as the hold reads them
            UsPresidentialElection.Contest held = UsPresidentialElection.Hold(usa, day2028, poll, out string why);
            UsPresidentialElection.Contest plantedOnRecord = null;
            try
            {
                UsPresidentialElection.Contest onRecord = UsPresidentialElection.Count(CountryId.USA, day2028, poll, nominees,
                    UsPresidentialElection.TieSeedOf(SimulationRandom.MasterSeed, 2028), out ElectoralCollege.StateCall[] fromRecord, out string _);
                ElectoralCollege.StateCall[] fromGame = held == null ? null : UsPresidentialElection.StatesOf(usa, held);
                bool swungFromGame = held != null && fromGame != null && game2024 != null && fromGame.Length == game2024.Length
                                     && fromGame.All(c => Equal(c.PriorShares, game2024.First(g => g.State == c.State).Shares))
                                     && held.DayBasis.Contains(UsPresidentialElection.GameStatesBasis + Day(Polled));
                var map = new StringBuilder();
                int differ = 0;
                if (fromGame != null && fromRecord != null && game2024 != null)
                {
                    var truth = UsPresidentialReturns.States.Where(s => s.Year == 2024).ToDictionary(s => s.State, s => (double)s.VotesR / (s.VotesR + s.VotesD), StringComparer.Ordinal);
                    foreach (ElectoralCollege.StateCall c in fromGame)
                    {
                        ElectoralCollege.StateCall r = fromRecord.First(x => x.State == c.State);
                        ElectoralCollege.StateCall g = game2024.First(x => x.State == c.State);
                        if (c.Winner != r.Winner) { differ++; }
                        map.Append(F("        {0,-3} record 2024 {1}  game 2024 {2}  | 2028 from the game's {3} {4}  from the record's {5} {6}{7}\n",
                            c.State, Pct(truth.TryGetValue(c.State, out double t) ? t : double.NaN), Pct(TwoParty(g)), Pct(TwoParty(c)), Side(c.Winner), Pct(TwoParty(r)), Side(r.Winner), c.Winner != r.Winner ? "  <- called apart" : string.Empty));
                    }
                }
                Check(swungFromGame,
                    F("(d) the game's 2028 count ({0}) swings every state from the game's own 2024 states - each state's prior the 2024 call re-derived from the stored contest - its basis saying so; the count on the record's 2024 states would read {1}; {2} state(s) called apart. REP share of REP and DEM, state by state:\n{3}",
                        held?.Line ?? "NOT HELD: " + why, onRecord?.Line ?? "no count", differ, map));
                if (held != null)
                {
                    UsCongressElection.Contest congress = UsCongressElection.Count(usa, day2028, poll, held, out string whyCongress);
                    bool nightRead = congress != null && congress.Senate.All(s => Math.Abs(s.Parent - TwoParty(fromGame.First(c => c.State == s.State))) < 1e-15);
                    Check(nightRead, F("(d) the 2028 Senate count reads the same night - each seat's parent the state's share on the game's own states ({0} seats){1}", congress?.Senate.Count ?? 0, congress == null ? ": " + whyCongress : string.Empty));
                }
                if (held != null && fromGame != null)
                {
                    // the order the list holds the contests in reads nothing: the 2028 count moved to the list's head re-derives the same states (its basis names the day it swung from)
                    usa.UsPresidentialElections.Remove(held);
                    usa.UsPresidentialElections.Insert(0, held);
                    ElectoralCollege.StateCall[] reordered = UsPresidentialElection.StatesOf(usa, held);
                    bool named = UsPresidentialElection.TryCountedOnGameStates(held, out DateTime namedDay);
                    bool sameReordered = reordered != null && reordered.Length == fromGame.Length && reordered.Zip(fromGame, (a, b) => a.State == b.State && a.Winner == b.Winner && Equal(a.Shares, b.Shares)).All(x => x);
                    Check(named && namedDay == Polled && sameReordered,
                        F("(d) the form is the contest's own: the 2028 count's basis names {0} as the count it swung from, and moved to the head of the list it re-derives the same states", Day(namedDay)));
                }
                if (onRecord != null && fromRecord != null)
                {
                    // a 2028 count made on the record's states - as a build before US-26 counted one - kept in the list beside the game's own 2024: re-derived in the record form it was counted in
                    usa.UsPresidentialElections.Add(onRecord);
                    plantedOnRecord = onRecord;
                    ElectoralCollege.StateCall[] restated = UsPresidentialElection.StatesOf(usa, onRecord);
                    bool recordForm = !UsPresidentialElection.TryCountedOnGameStates(onRecord, out DateTime _) && restated != null && restated.Length == fromRecord.Length
                                      && restated.Zip(fromRecord, (a, b) => a.State == b.State && a.Winner == b.Winner && Equal(a.Shares, b.Shares) && Equal(a.PriorShares, b.PriorShares)).All(x => x);
                    Check(recordForm, "(d) a 2028 count made on the record's states (its basis the record form, as a build before US-26 counted it), planted in the list beside the game's own 2024, re-derives on the record's states it was counted on - not the game's");
                }
            }
            finally { if (held != null) { usa.UsPresidentialElections.Remove(held); } if (plantedOnRecord != null) { usa.UsPresidentialElections.Remove(plantedOnRecord); } }
        }

        // ------------------------------------------------------------------ helpers

        /// <summary>A world on the epoch already set, as a new game builds one, its country returned.</summary>
        private static Country Fresh(CountryId id)
        {
            SimulationRandom.Seed(Seed);
            EnergyMarket.ResetCalibration();
            World world = WorldFactory.CreateDefault();
            return world.GetCountry(id);
        }

        /// <summary>The country's next polling day on or after <paramref name="from"/>, or <paramref name="probe"/> where its calendar holds none (Germany after its snap).</summary>
        private static DateTime NextAfter(CountryId id, DateTime from, DateTime probe) => WorldClock.TryNextPollingDay(id, from, out DateTime day) ? day : probe;

        /// <summary>One day in the controller's order as it stood before FORK-2 (§890) moved the player's election into the manager; part (b) copies the count by hand,
        /// so the manager's own election path is not exercised here.</summary>
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

        private static double TwoParty(ElectoralCollege.StateCall c) => c.Shares[ElectoralCollege.ShareRepublican] / (c.Shares[ElectoralCollege.ShareRepublican] + c.Shares[ElectoralCollege.ShareDemocrat]);
        private static string Side(int winner) => winner == ElectoralCollege.Republican ? "R" : "D";
        private static bool Equal(double[] a, double[] b) => a != null && b != null && a.Length == b.Length && a.Select((v, i) => v.Equals(b[i])).All(x => x);
        private static bool Same(Dictionary<string, double> a, Dictionary<string, double> b) => a != null && b != null && a.Count == b.Count && a.All(kv => b.TryGetValue(kv.Key, out double v) && v.Equals(kv.Value));
        private static string Shares(string[] keys, double[] v) => string.Join(" ", keys.Select((k, i) => k + " " + Pct(v[i])));
        private static string Pct(double v) => double.IsNaN(v) ? "n/a" : (100.0 * v).ToString("0.00", CultureInfo.InvariantCulture);
        private static string Day(DateTime d) => d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        private static string F(string format, params object[] args) => string.Format(CultureInfo.InvariantCulture, format, args);
    }
}
