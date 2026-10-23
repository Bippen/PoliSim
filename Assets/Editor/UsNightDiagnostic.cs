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
using PoliSim.Simulation;
using PoliSim.UI;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// PS-6 US-34 (boards 29a-29b, a PROPOSAL built on Elias's go-ahead of 2026-10-23): THE USA'S NIGHT, ITS ARITHMETIC - the night declares the count the
    /// game already holds, so at its last declaration it must BE that count, and a call must never land before its line is crossed.
    /// <list type="bullet">
    /// <item>(a) Board 29a's cartogram as generated (`UsTileCartogram`): 51 jurisdictions, 538 cells inside the grid and none twice, each one block, its code
    /// in one of its own cells and its name the board's; each state's cells its 2024 electors of the catalog and its House seats in force plus two (the
    /// District three and no seat); Maine's and Nebraska's numbered cells the catalog's districts.</item>
    /// <item>(b) The game's own count: a world on the eve of 5 Nov 2024 (seed 777, the USA the player's, REP), stepped one day as the controller steps it -
    /// the night of the contests the day loop held, at 51 of 51, gives the presidential contest's electors, the Congress count's House, and the Senate that
    /// sits on 3 January (`UsCongressElection.SenateOn`), each exactly; and every call lands on the first declaration at which its line is crossed.</item>
    /// <item>(c) The record's vote (each nominee's poll at the national share of record over the nominee's fitted factor), then a sweep of the national vote
    /// in 2024 and 2028 and three midterms of 2026: the same, on every count (the Congress contest added to the world for its Senate and removed).</item>
    /// <item>(d) A staged College with no majority (29a's 269-269 pick, the closest states called the other way): NO MAJORITY, landing where neither side
    /// can reach 270; the delegations tonight's House gives, by state.</item>
    /// <item>(e) The inks: REP and DEM drawn in board 29b's hexes, unmoved by the nudge; and the night is available for the USA and is never the regional
    /// readout's.</item>
    /// <item>(f) K38: the Docket's dates to the oath (`UsNight.DatesToTheOath`) on a planted 2028 night and a 2026 midterm - none on the eve, the four from the
    /// night in date order, each until its day has passed, the new Congress alone at a midterm.</item>
    /// </list>
    /// The random streams are restored after, the epoch by its scope, and every host destroyed.
    /// </summary>
    public static class UsNightDiagnostic
    {
        private static readonly DateTime Eve = new DateTime(2024, 11, 4);
        private static readonly DateTime Polled = new DateTime(2024, 11, 5);
        private const int Seed = 777;
        private const string PlayerParty = "REP";

        /// <summary>[AUTHORED-REFERENCE] board 29b's legend, as installed in `PoliSimTheme`.</summary>
        private static readonly Dictionary<string, string> BoardInks = new Dictionary<string, string> { { "REP", "75383B" }, { "DEM", "383875" } };

        /// <summary>The cartogram's source, out of tree (`Tools/dus_cartogram_prep.pl`'s default), compared with the table's digest where it is on disk.</summary>
        private const string TilesSource = "../PoliSim-captures/design/dus/tiles.json";

        private static string F(string format, params object[] args) => string.Format(CultureInfo.InvariantCulture, format, args);

        public static void Run()
        {
            CheckExit.ArmLogFold();
            var sb = new StringBuilder("=== UsNightDiagnostic (PS-6 US-34): the USA's election night, its arithmetic ===\n");
            int failures = 0;
            void Check(bool ok, string what) { if (!ok) { failures++; } sb.Append(ok ? "    ok        " : "    FAIL      ").Append(what).Append('\n'); }
            var hosts = new List<GameObject>();
            int seedWas = SimulationRandom.MasterSeed;
            Dictionary<SimulationRandom.Stream, int> drawsWas = SimulationRandom.CaptureDrawCounts();
            try
            {
                Cartogram(Check);
                CallTable(Check);

                // (e) availability and the inks
                Check(ElectionNightFromModel.Available(CountryId.USA) && ElectionNightFromModel.At(ElectionNightFromModel.FinalMinute, CountryId.USA, new[] { "REP", "DEM" }, 435, 0.0) == null,
                    "(e) the night is available for the USA, and the regional readout never draws it (ElectionNightFromModel.At is null for it)");
                bool inks = true;
                var drawn = new List<string>();
                foreach (KeyValuePair<string, string> kv in BoardInks)
                {
                    string hex = ColorUtility.ToHtmlStringRGB(PoliSimTheme.PartyLaddered(CountryId.USA, kv.Key));
                    drawn.Add(kv.Key + " #" + hex);
                    if (!PoliSimTheme.HasPartyInk(CountryId.USA, kv.Key) || !string.Equals(hex, kv.Value, StringComparison.OrdinalIgnoreCase)) { inks = false; }
                }
                Check(inks, F("(e) REP and DEM drawn in board 29b's hexes, unmoved by the nudge ({0}); the independents given none (they sit with DEM; the Neutral)", string.Join(" · ", drawn)));

                using (SimulationManager.EpochScope())
                {
                    try
                    {
                        (SimulationManager sim, World world, Country usa) = Open(hosts, Eve, PlayerParty);
                        Step(sim, world);
                        UsPresidentialElection.Contest held = usa.UsPresidentialElections.Find(c => c.PollingDay.Date == Polled);
                        UsCongressElection.Contest heldCongress = usa.UsCongressElections.Find(c => c.PollingDay.Date == Polled);
                        Check(held != null && heldCongress != null, F("(b) the eve world held its own count on {0}: {1}", Day(Polled), held?.Line ?? "NO PRESIDENTIAL COUNT"));
                        if (held != null && heldCongress != null)
                        {
                            UsNight.State night = UsNight.At(int.MaxValue, usa, held, heldCongress);
                            Check(Agrees(usa, held, heldCongress, night, out string why), "(b) the game's own count of 5 Nov 2024 at 51 of 51 - " + Summary(night) + why);
                            Check(CallsHold(usa, held, heldCongress, out string callsWhy), "(b) through the night, every call on the first declaration at which its line is crossed, and never before - " + callsWhy);
                            ElectionNightScreen.UsNightBoard board = GameControllerInputs(usa, night, held, heldCongress);
                            int castR = -1, castD = -1;   // the review's L5: NARA's electors as the catalog holds them, never typed
                            foreach (var y in UsPresidentialReturns.Years) { if (y.Year == 2024) { castR = y.CastR + y.OthersRSlate; castD = y.CastD + y.OthersDSlate; } }
                            Check(board != null && board.HasReference && castR > 0 && board.RealElectorsRep == castR && board.RealElectorsDem == castD,
                                F("(b) the night carries the record as its reference (\"{0}\": the College REP {1} DEM {2}, the House REP {3} DEM {4}, the Senate {5})",
                                    board?.ReferenceLabel, board?.RealElectorsRep, board?.RealElectorsDem, board?.RealHouseRep, board?.RealHouseDem,
                                    board?.RealSenateRep.HasValue == true ? F("REP {0} DEM {1}", board.RealSenateRep, board.RealSenateDem) : "owed"));
                        }

                        // (c) the record's vote, then the sweep
                        Dictionary<string, double> recordPoll = RecordPoll();
                        int counted = 0, disagree = 0, early = 0, noMajority = 0;
                        string firstWhy = null;
                        var lines = new List<string>();
                        void Try(DateTime day, Dictionary<string, double> poll, string label)
                        {
                            UsPresidentialElection.Contest p = UsPresidentialElection.Count(CountryId.USA, day, poll, null, UsPresidentialElection.TieSeedOf(Seed, day.Year), out ElectoralCollege.StateCall[] _, out string whyP);
                            if (p == null) { disagree++; firstWhy = firstWhy ?? F(" - {0}: no presidential count ({1})", label, whyP); return; }
                            UsCongressElection.Contest c = UsCongressElection.Count(usa, day, poll, p, out string whyC);
                            if (c == null) { disagree++; firstWhy = firstWhy ?? F(" - {0}: no Congress count ({1})", label, whyC); return; }
                            // the world's own count of the same day stands aside while a planted one is read - two contests seated on one 3 January are one too many
                            bool aside = heldCongress != null && heldCongress.PollingDay.Date == day.Date && usa.UsCongressElections.Remove(heldCongress);
                            usa.UsCongressElections.Add(c);
                            try
                            {
                                counted++;
                                UsNight.State n = UsNight.At(int.MaxValue, usa, p, c);
                                if (!Agrees(usa, p, c, n, out string why)) { disagree++; firstWhy = firstWhy ?? " - " + label + why; }
                                if (!CallsHold(usa, p, c, out string cw)) { early++; firstWhy = firstWhy ?? " - " + label + ": " + cw; }
                                if (n != null && n.College == UsNight.Call.NoMajority) { noMajority++; }
                                if (label.StartsWith("the record", StringComparison.Ordinal)) { lines.Add(label + ": " + Summary(n)); }
                            }
                            finally { usa.UsCongressElections.Remove(c); if (aside) { usa.UsCongressElections.Add(heldCongress); } }
                        }
                        if (recordPoll != null) { Try(Polled, recordPoll, "the record's vote of 2024"); }
                        else { disagree++; firstWhy = " - the record's vote could not be planted (a nominee's factor is not fitted)"; }
                        DateTime day2028 = UsPresidentialElection.PresidentialDay(2028);
                        for (int i = 0; i <= 80; i++)
                        {
                            double v = 0.40 + 0.0025 * i;
                            Try(Polled, new Dictionary<string, double> { { "REP", v }, { "DEM", 1.0 - v } }, F("2024 at REP {0:0.0000}", v));
                            if (i % 4 == 0) { Try(day2028, new Dictionary<string, double> { { "REP", v }, { "DEM", 1.0 - v } }, F("2028 at REP {0:0.0000}", v)); }
                        }
                        Check(disagree == 0 && early == 0 && counted > 0,
                            F("(c) {0}; and a sweep - 2024 at REP 0.40 to 0.60 of the two-party poll by 0.0025, 2028 by 0.01: {1} counts, the night at 51 of 51 equal to the count in every one ({2} disagree), no call before its line ({3} early); {4} with no College majority{5}",
                                lines.Count > 0 ? lines[0] : "the record's vote not counted", counted, disagree, early, noMajority, firstWhy ?? string.Empty));

                        // the midterms of 2026: no College; the House and the Senate class
                        DateTime midterm = UsPresidentialElection.PresidentialDay(2026);
                        int midterms = 0, midtermBad = 0;
                        string midtermWhy = null, midtermLine = null;
                        foreach (double v in new[] { 0.45, 0.50, 0.55 })
                        {
                            UsCongressElection.Contest c = UsCongressElection.CountMidterm(usa, midterm, new Dictionary<string, double> { { "REP", v }, { "DEM", 1.0 - v } }, UsPresidentialElection.TieSeedOf(Seed, 2026), out string whyM);
                            if (c == null) { midtermBad++; midtermWhy = midtermWhy ?? " - no midterm count: " + whyM; continue; }
                            usa.UsCongressElections.Add(c);
                            try
                            {
                                midterms++;
                                UsNight.State n = UsNight.At(int.MaxValue, usa, null, c);
                                if (!Agrees(usa, null, c, n, out string why) || n.Presidential || n.College != UsNight.Call.Open) { midtermBad++; midtermWhy = midtermWhy ?? why; }
                                if (!CallsHold(usa, null, c, out string cw)) { midtermBad++; midtermWhy = midtermWhy ?? " - " + cw; }
                                int want = IndependentsNotUp(usa, c);
                                if (n.NotUpIndependents != want) { midtermBad++; midtermWhy = midtermWhy ?? F(" - the independents not up: the night {0}, counted here {1}", n.NotUpIndependents, want); }
                                midtermLine = midtermLine ?? Summary(n);
                            }
                            finally { usa.UsCongressElections.Remove(c); }
                        }
                        Check(midterms == 3 && midtermBad == 0, F("(c) the midterm of {0} at REP 0.45, 0.50 and 0.55: no College; the night at its last declaration the count's House and the Senate that sits ({1}){2}", Day(midterm), midtermLine, midtermWhy ?? string.Empty));

                        // the review's L5: the independents not up, with the world's 2024 count set aside - the record's own class I (King, Sanders) stands, not up in 2026
                        if (heldCongress != null)
                        {
                            bool aside = usa.UsCongressElections.Remove(heldCongress);
                            try
                            {
                                UsCongressElection.Contest c = UsCongressElection.CountMidterm(usa, midterm, new Dictionary<string, double> { { "REP", 0.5 }, { "DEM", 0.5 } }, UsPresidentialElection.TieSeedOf(Seed, 2026), out string whyI);
                                if (c == null) { Check(false, "(c) the independents: no midterm count on the record's seats - " + whyI); }
                                else
                                {
                                    usa.UsCongressElections.Add(c);
                                    try
                                    {
                                        UsNight.State n = UsNight.At(int.MaxValue, usa, null, c);
                                        int want = IndependentsNotUp(usa, c);
                                        Check(want > 0 && n.NotUpIndependents == want && Agrees(usa, null, c, n, out string whyA),
                                            F("(c) the independents: the 2026 midterm on the record's class I - {0} independent(s) not up, counted here from the record's seats, the night {1}, inside DEM's {2} not up", want, n.NotUpIndependents, n.NotUpDem));
                                    }
                                    finally { usa.UsCongressElections.Remove(c); }
                                }
                            }
                            finally { if (aside) { usa.UsCongressElections.Add(heldCongress); } }
                        }

                        // (d) a staged College with no majority
                        if (held != null && heldCongress != null)
                        {
                            ElectoralCollege.StateCall[] states = UsPresidentialElection.StatesOf(usa, held);
                            string[] flips = UsNight.FlipsToTie(states);
                            if (flips == null) { Check(false, "(d) no set of states called the other way leaves the College equal"); }
                            else
                            {
                                ElectoralCollege.StateCall[] flipped = UsNight.Flipped(states, flips);
                                Dictionary<(string, int), string> notUp = UsNight.NotUp(usa, heldCongress, out int independents);
                                UsNight.State tie = UsNight.Of(int.MaxValue, Polled, flipped, heldCongress, notUp, independents);
                                int dr = heldCongress.House.Count(h => h.Rep > h.Dem), dd = heldCongress.House.Count(h => h.Dem > h.Rep), dt = heldCongress.House.Count - dr - dd;
                                bool landed = true;
                                for (int k = 0; k <= tie.Total; k++)
                                {
                                    UsNight.State at = UsNight.Of(k, Polled, flipped, heldCongress, notUp, independents);
                                    bool certain = at.ElectorsRep + at.ElectorsToCome < at.ElectorsMajority && at.ElectorsDem + at.ElectorsToCome < at.ElectorsMajority;
                                    if ((at.College == UsNight.Call.NoMajority) != certain) { landed = false; }
                                }
                                Check(tie.College == UsNight.Call.NoMajority && tie.ElectorsRep == tie.ElectorsDem && landed && tie.DelegationsRep == dr && tie.DelegationsDem == dd && tie.DelegationsTied == dt,
                                    F("(d) staged with {0} called the other way: the College {1}-{2}, NO MAJORITY at {3} of {4} and never before; tonight's delegations REP {5} DEM {6} tied {7}, the House count's",
                                        string.Join(", ", flips), tie.ElectorsRep, tie.ElectorsDem, tie.CollegeAt, tie.Total, tie.DelegationsRep, tie.DelegationsDem, tie.DelegationsTied));
                            }
                        }
                    }
                    finally
                    {
                        foreach (GameObject go in hosts) { if (go != null) { UnityEngine.Object.DestroyImmediate(go); } }
                        EnergyMarket.ResetTurnState();
                    }
                }
            }
            catch (Exception e) { failures++; sb.Append("    THREW: " + e.GetType().Name + ": " + e.Message + "\n" + e.StackTrace + "\n"); }
            finally
            {
                SimulationRandom.RestoreState(seedWas, drawsWas);
            }
            if (failures > 0) { Debug.LogError($"US NIGHT: {failures} failure(s).\n{sb}"); CheckExit.Finish(1); return; }
            Debug.Log(sb.ToString());
            CheckExit.Finish(0);
        }

        /// <summary>(a) The generated table against the catalog and its own invariants.</summary>
        private static void Cartogram(Action<bool, string> Check)
        {
            UsTileCartogram.Block[] blocks = UsTileCartogram.States;
            var electors = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (var s in UsPresidentialReturns.States) { if (s.Year == 2024) { electors[s.State] = s.Electors; } }
            SortedDictionary<string, int> house = UsCongressElection.SeatsInForce(2024);
            var districts = new Dictionary<string, List<int>>(StringComparer.Ordinal);
            foreach (var d in UsPresidentialReturns.Districts)
            {
                if (d.Year != 2024) { continue; }
                if (!districts.TryGetValue(d.State, out List<int> list)) { districts[d.State] = list = new List<int>(); }
                list.Add(d.District);
            }
            var seen = new HashSet<long>();
            int cells = 0;
            string why = null;
            foreach (UsTileCartogram.Block b in blocks)
            {
                int n = b.Cells.GetLength(0);
                cells += n;
                var own = new HashSet<long>();
                var numbered = new List<int>();
                for (int r = 0; r < n; r++)
                {
                    int x = b.Cells[r, 0], y = b.Cells[r, 1];
                    long id = y * 1000L + x;
                    if (x < 0 || y < 0 || x >= UsTileCartogram.Columns || y >= UsTileCartogram.Rows || !seen.Add(id)) { why = why ?? F(" - {0}: cell {1},{2} outside the grid or taken twice", b.Code, x, y); }
                    own.Add(id);
                    if (b.Cells[r, 2] > 0) { numbered.Add(b.Cells[r, 2]); }
                }
                if (!OneBlock(own)) { why = why ?? F(" - {0} is not one block", b.Code); }
                if (!own.Contains(b.LabelRow * 1000L + b.LabelColumn)) { why = why ?? F(" - {0}: its code is not in one of its cells", b.Code); }
                if (string.IsNullOrEmpty(b.Name)) { why = why ?? F(" - {0} has no name", b.Code); }
                if (!electors.TryGetValue(b.Code, out int e) || e != n) { why = why ?? F(" - {0}: {1} cells, the catalog's 2024 electors {2}", b.Code, n, electors.TryGetValue(b.Code, out int e2) ? e2.ToString(CultureInfo.InvariantCulture) : "none"); }
                int seats = house.TryGetValue(b.Code, out int hs) ? hs : 0;
                if (b.Code == "DC" ? seats != 0 || n != 3 : seats + 2 != n) { why = why ?? F(" - {0}: {1} cells, its House seats in force {2} plus two", b.Code, n, seats); }
                numbered.Sort();
                List<int> want = districts.TryGetValue(b.Code, out List<int> dl) ? dl.OrderBy(v => v).ToList() : new List<int>();
                if (!numbered.SequenceEqual(want)) { why = why ?? F(" - {0}: numbered cells {1}, the catalog's districts {2}", b.Code, string.Join(",", numbered), string.Join(",", want)); }
            }
            Check(blocks.Length == 51 && cells == 538 && electors.Count == 51 && why == null,
                F("(a) board 29a's cartogram as generated: {0} jurisdictions, {1} cells on a {2} x {3} grid, each one block of its 2024 electors (the House seats in force plus two; the District three), its code in its own cell, ME's and NE's districts numbered as the catalog's; {4} border segments; source {5}{6}",
                    blocks.Length, cells, UsTileCartogram.Columns, UsTileCartogram.Rows, UsTileCartogram.Borders.GetLength(0), UsTileCartogram.SourceDigest.Substring(0, 12), why ?? string.Empty));
            string path = Path.GetFullPath(Path.Combine(Application.dataPath, "..", TilesSource));
            if (File.Exists(path))
            {
                string digest;
                using (SHA256 sha = SHA256.Create()) { digest = BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-", string.Empty).ToLowerInvariant(); }
                Check(digest == UsTileCartogram.SourceDigest, F("(a) the table's source on disk ({0}) at the digest the table was generated from ({1})", TilesSource, digest.Substring(0, 12)));
            }
            else { Check(true, F("(a) the table's source ({0}) is out of tree and not on this disk - not compared", TilesSource)); }
        }

        /// <summary>The night at its last declaration against the count: the College the contest's electors, the House its seats, the Senate the one that sits on
        /// its 3 January (the Congress contest in the world's list), every jurisdiction declared.</summary>
        private static bool Agrees(Country usa, UsPresidentialElection.Contest president, UsCongressElection.Contest congress, UsNight.State night, out string why)
        {
            why = string.Empty;
            if (night == null) { why = " - the night did not build"; return false; }
            int wantTotal = president != null ? 51 : congress.House.Count;
            if (!night.Complete || night.Total != wantTotal) { why = F(" - {0} of {1} declared, {2} expected", night.Declared, night.Total, wantTotal); return false; }
            if (president != null && (night.ElectorsRep != president.ElectorsOf("REP") || night.ElectorsDem != president.ElectorsOf("DEM") || night.Electors != president.ElectorsAppointed))
            {
                why = F(" - the College night {0}-{1} of {2}, the count {3}-{4} of {5}", night.ElectorsRep, night.ElectorsDem, night.Electors, president.ElectorsOf("REP"), president.ElectorsOf("DEM"), president.ElectorsAppointed);
                return false;
            }
            if (night.HouseRep != congress.HouseRep || night.HouseDem != congress.HouseDem || night.HouseToCome != 0)
            {
                why = F(" - the House night {0}-{1}, the count {2}-{3}", night.HouseRep, night.HouseDem, congress.HouseRep, congress.HouseDem);
                return false;
            }
            Dictionary<string, int> senate = UsCongressElection.SenateOn(usa, congress.Convenes);
            int rep = senate.TryGetValue("REP", out int r) ? r : 0, dem = senate.TryGetValue("DEM", out int d) ? d : 0;
            if (night.SenateRep != rep || night.SenateDem != dem || night.SenateSeats != senate.Values.Sum() || night.UpRep != congress.SenateOf("REP") || night.UpDem != congress.SenateOf("DEM"))
            {
                why = F(" - the Senate night {0}-{1} of {2} (tonight {3}-{4}), the Senate that sits {5}-{6} of {7} (tonight {8}-{9})", night.SenateRep, night.SenateDem, night.SenateSeats, night.UpRep, night.UpDem,
                    rep, dem, senate.Values.Sum(), congress.SenateOf("REP"), congress.SenateOf("DEM"));
                return false;
            }
            return true;
        }

        /// <summary>Every call through the night, declaration by declaration: the sums recounted here from the night's order, each call's line judged here, and
        /// the night's call at each instant the first crossing's - never earlier, never later, never reversed.</summary>
        private static bool CallsHold(Country usa, UsPresidentialElection.Contest president, UsCongressElection.Contest congress, out string why)
        {
            why = "none early, none late";
            // the night's own inputs read once (`UsNight.At` re-derives the states each time it is asked); every instant is the same function of them
            ElectoralCollege.StateCall[] states = president != null ? UsPresidentialElection.StatesOf(usa, president) : null;
            Dictionary<(string, int), string> notUp = UsNight.NotUp(usa, congress, out int independents);
            UsNight.State full = UsNight.Of(int.MaxValue, congress.PollingDay, states, congress, notUp, independents);
            if (full == null || (president != null && states == null)) { why = "the night did not build"; return false; }
            int er = 0, ed = 0, eTo = full.Electors, hr = 0, hd = 0, hTo = full.HouseSeats, sr = full.NotUpRep, sd = full.NotUpDem, sTo = 0;
            foreach (UsNight.Jurisdiction j in full.InOrder) { sTo += j.SenateUp.Count; }   // the seats on tonight's ballot, counted here (the complete night's UpToCome is 0 - n904's false Tied at 0)
            int collegeAt = -1, houseAt = -1, senateAt = -1;
            UsNight.Call college = UsNight.Call.Open, houseCall = UsNight.Call.Open, senateCall = UsNight.Call.Open;
            for (int k = 0; k <= full.Total; k++)
            {
                if (k > 0)
                {
                    UsNight.Jurisdiction j = full.InOrder[k - 1];
                    er += j.ElectorsRep; ed += j.ElectorsDem; eTo -= j.Electors;
                    hr += j.HouseRep; hd += j.HouseDem; hTo -= j.HouseSeats;
                    foreach (UsNight.SenateSeat s in j.SenateUp) { if (s.Side == "REP") { sr++; } else { sd++; } sTo--; }
                }
                // the lines, judged here
                if (college == UsNight.Call.Open && full.Presidential)
                {
                    int m = full.Electors / 2 + 1;
                    college = er >= m ? UsNight.Call.Republican : ed >= m ? UsNight.Call.Democrat : er + eTo < m && ed + eTo < m ? UsNight.Call.NoMajority : UsNight.Call.Open;
                    if (college != UsNight.Call.Open) { collegeAt = k; }
                }
                if (houseCall == UsNight.Call.Open)
                {
                    int m = full.HouseSeats / 2 + 1;
                    houseCall = hr >= m ? UsNight.Call.Republican : hd >= m ? UsNight.Call.Democrat : UsNight.Call.Open;
                    if (houseCall != UsNight.Call.Open) { houseAt = k; }
                }
                if (senateCall == UsNight.Call.Open)
                {
                    int m = full.SenateSeats / 2 + 1;
                    senateCall = sr >= m ? UsNight.Call.Republican : sd >= m ? UsNight.Call.Democrat : full.SenateSeats % 2 == 0 && sr + sTo < m && sd + sTo < m ? UsNight.Call.Tied : UsNight.Call.Open;
                    if (senateCall != UsNight.Call.Open) { senateAt = k; }
                }
                UsNight.State at = UsNight.Of(k, congress.PollingDay, states, congress, notUp, independents);
                if (at.College != college || (college != UsNight.Call.Open && at.CollegeAt != collegeAt)) { why = F("the College at {0} of {1}: the night {2} at {3}, the line {4} at {5}", k, full.Total, at.College, at.CollegeAt, college, collegeAt); return false; }
                if (at.House != houseCall || (houseCall != UsNight.Call.Open && at.HouseAt != houseAt)) { why = F("the House at {0} of {1}: the night {2} at {3}, the line {4} at {5}", k, full.Total, at.House, at.HouseAt, houseCall, houseAt); return false; }
                if (at.Senate != senateCall || (senateCall != UsNight.Call.Open && at.SenateAt != senateAt)) { why = F("the Senate at {0} of {1}: the night {2} at {3}, the line {4} at {5}", k, full.Total, at.Senate, at.SenateAt, senateCall, senateAt); return false; }
            }
            why = F("the College {0} at {1}, the House {2} at {3}, the Senate {4} at {5} of {6}", college, collegeAt, houseCall, houseAt, senateCall, senateAt, full.Total);
            return true;
        }

        /// <summary>The independents among the seats not up in the Senate that sits from <paramref name="congress"/>'s 3 January, counted here from the record: a
        /// seat of record held by an independent with DEM, not on the day's ballot (`UsCongressElection.SeatsUp`), and not one the game has elected and seated.</summary>
        private static int IndependentsNotUp(Country usa, UsCongressElection.Contest congress)
        {
            var up = new HashSet<string>(UsCongressElection.SeatsUp(congress.PollingDay).Select(s => s.State + "/" + s.Class));
            var game = new HashSet<string>();
            foreach (UsCongressElection.Contest c in usa.UsCongressElections) { if (c.Convenes <= congress.Convenes) { foreach (UsCongressElection.SenateSeat s in c.Senate) { game.Add(s.State + "/" + s.Class); } } }
            UsSenate.Seating record = UsSenate.On(congress.Convenes);
            return record.Seats.Count(s => s.Party == "I" && s.Side == "DEM" && !up.Contains(s.State + "/" + s.Class) && !game.Contains(s.State + "/" + s.Class));
        }

        /// <summary>The review's L5: the call rules on cases worked by hand - each the answer a reader of the rule reaches, written here, not computed.</summary>
        private static void CallTable(Action<bool, string> Check)
        {
            var college = new (int Rep, int Dem, int ToCome, int Majority, UsNight.Call Want)[]
            {
                (270, 0, 268, 270, UsNight.Call.Republican), (0, 270, 268, 270, UsNight.Call.Democrat), (269, 269, 0, 270, UsNight.Call.NoMajority),
                (268, 0, 270, 270, UsNight.Call.Open), (269, 200, 69, 270, UsNight.Call.Open), (260, 200, 9, 270, UsNight.Call.NoMajority),
            };
            var chamber = new (int Rep, int Dem, int ToCome, int Seats, UsNight.Call Want)[]
            {
                (51, 40, 9, 100, UsNight.Call.Republican), (40, 51, 9, 100, UsNight.Call.Democrat), (50, 50, 0, 100, UsNight.Call.Tied), (50, 49, 1, 100, UsNight.Call.Open),
                (50, 49, 0, 99, UsNight.Call.Republican), (49, 49, 1, 99, UsNight.Call.Open), (218, 0, 217, 435, UsNight.Call.Republican), (217, 217, 1, 435, UsNight.Call.Open),
            };
            var bad = new List<string>();
            foreach (var c in college) { UsNight.Call got = UsNight.CollegeCallOf(c.Rep, c.Dem, c.ToCome, c.Majority); if (got != c.Want) { bad.Add(F("College {0}-{1}, {2} to come, {3} to elect: {4} not {5}", c.Rep, c.Dem, c.ToCome, c.Majority, got, c.Want)); } }
            foreach (var c in chamber) { UsNight.Call got = UsNight.MajorityCallOf(c.Rep, c.Dem, c.ToCome, c.Seats); if (got != c.Want) { bad.Add(F("{0}-{1}, {2} to come, of {3}: {4} not {5}", c.Rep, c.Dem, c.ToCome, c.Seats, got, c.Want)); } }
            Check(bad.Count == 0, F("(b) the call rules on {0} cases worked by hand (a majority, no majority at 269-269, a 50-50 Senate tied, an odd Senate with a vacancy, the House at 218){1}",
                college.Length + chamber.Length, bad.Count == 0 ? string.Empty : " - " + string.Join("; ", bad)));

            // K38 (Elias, 2026-10-23): the Docket's dates to the oath, read from planted contests by the statute days the contests carry - from the night on, each
            // until it has passed; a midterm's the new Congress alone; none before the night or after the oath
            var polling = new DateTime(2028, 11, 7);
            var planted = new Country { Id = CountryId.USA };
            planted.UsPresidentialElections.Add(new UsPresidentialElection.Contest { PollingDay = polling, ElectorsMeet = UsPresidentialElection.ElectorsMeetAfter(polling), CountedOn = UsPresidentialElection.CountedOnAfter(polling), TakesOffice = UsPresidentialElection.OathDayAfter(polling) });
            planted.UsCongressElections.Add(new UsCongressElection.Contest { PollingDay = polling, Convenes = UsCongressElection.ConvenesAfter(polling) });
            string Keys(DateTime on) => string.Join(",", UsNight.DatesToTheOath(planted, on).Select(d => d.Key + "@" + d.Day.ToString("MM-dd", CultureInfo.InvariantCulture)));
            string eve = Keys(polling.AddDays(-1)), night = Keys(polling), afterElectors = Keys(UsPresidentialElection.ElectorsMeetAfter(polling).AddDays(1)), onOath = Keys(new DateTime(2029, 1, 20)), afterOath = Keys(new DateTime(2029, 1, 21));
            string meet = UsPresidentialElection.ElectorsMeetAfter(polling).ToString("MM-dd", CultureInfo.InvariantCulture);
            var midterm = new Country { Id = CountryId.USA };
            midterm.UsCongressElections.Add(new UsCongressElection.Contest { PollingDay = new DateTime(2026, 11, 3), Convenes = UsCongressElection.ConvenesAfter(new DateTime(2026, 11, 3)) });
            string mid = string.Join(",", UsNight.DatesToTheOath(midterm, new DateTime(2026, 11, 3)).Select(d => d.Key + "@" + d.Day.ToString("MM-dd", CultureInfo.InvariantCulture)));
            Check(eve.Length == 0 && night == "electors@" + meet + ",congress@01-03,count@01-06,oath@01-20" && afterElectors == "congress@01-03,count@01-06,oath@01-20" && onOath == "oath@01-20" && afterOath.Length == 0 && mid == "congress@01-03",
                F("(f) K38 the Docket's dates to the oath (a planted 2028 night): the eve \"{0}\"; the night \"{1}\"; after the electors \"{2}\"; on the oath \"{3}\"; after it \"{4}\"; a 2026 midterm's night \"{5}\"",
                    eve, night, afterElectors, onOath, afterOath, mid));
        }

        private static string Summary(UsNight.State n) => n == null ? "no night" : F("{0}the House REP {1} DEM {2}, the Senate REP {3} DEM {4} of {5} (tonight {6}-{7})",
            n.Presidential ? F("the College REP {0} DEM {1}, ", n.ElectorsRep, n.ElectorsDem) : string.Empty, n.HouseRep, n.HouseDem, n.SenateRep, n.SenateDem, n.SenateSeats, n.UpRep, n.UpDem);

        /// <summary>The poll that plants the record's national vote of 2024 in the count: each nominee's party at the national share of record over the
        /// nominee's fitted factor (the count multiplies it back), every other candidate standing at the share of record. Null where a factor is not fitted.</summary>
        private static Dictionary<string, double> RecordPoll()
        {
            long d = 0, r = 0, t = 0;
            foreach (var s in UsPresidentialReturns.States) { if (s.Year == 2024) { d += s.VotesD; r += s.VotesR; t += s.VotesTotal; } }
            var poll = new Dictionary<string, double>();
            foreach (PresidencyOfRecord.CandidateOfRecord c in PresidencyOfRecord.CandidatesOf(CountryId.USA, 2025))
            {
                if (c.BackingParty == null) { continue; }
                if (double.IsNaN(c.Factor) || c.Factor <= 0.0) { return null; }
                poll[c.BackingParty] = (c.BackingParty == "REP" ? (double)r / t : (double)d / t) / c.Factor;
            }
            return poll.ContainsKey("REP") && poll.ContainsKey("DEM") ? poll : null;
        }

        /// <summary>The controller's own inputs for the night (`GameController.UsNightInputs`), through reflection - the reference it carries.</summary>
        private static ElectionNightScreen.UsNightBoard GameControllerInputs(Country usa, UsNight.State night, UsPresidentialElection.Contest president, UsCongressElection.Contest congress)
        {
            MethodInfo inputs = typeof(GameController).GetMethod("UsNightInputs", BindingFlags.Static | BindingFlags.NonPublic);
            return inputs?.Invoke(null, new object[] { usa, night, president, congress }) as ElectionNightScreen.UsNightBoard;
        }

        /// <summary>A world on <paramref name="epoch"/> as a new game builds one: the USA the player's country with <paramref name="party"/>; the Senate of record
        /// seated as the game's selection seats it before any day is stepped (`CongressElectionDiagnostic`'s way).</summary>
        private static (SimulationManager Sim, World World, Country Usa) Open(List<GameObject> hosts, DateTime epoch, string party)
        {
            SimulationRandom.Seed(Seed);
            EnergyMarket.ResetCalibration();
            SimulationManager.SetEpoch(epoch);
            World w = WorldFactory.CreateDefault();
            var go = new GameObject("UsNightDiagnostic"); hosts.Add(go);
            var s = go.AddComponent<SimulationManager>();
            s.SetWorld(w);
            s.PlayerCountryId = CountryId.USA;
            w.GetCountry(CountryId.USA).PlayerPartyAbbrev = party;
            typeof(SimulationManager).GetMethod("SeatTheSecondChamberOfRecord", BindingFlags.Instance | BindingFlags.NonPublic)?.Invoke(s, null);
            return (s, w, w.GetCountry(CountryId.USA));
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

        /// <summary>Whether a set of grid cells (row × 1000 + column) is one block by its edges.</summary>
        private static bool OneBlock(HashSet<long> cells)
        {
            if (cells.Count == 0) { return false; }
            var reached = new HashSet<long>();
            var queue = new Queue<long>();
            foreach (long c in cells) { queue.Enqueue(c); reached.Add(c); break; }
            while (queue.Count > 0)
            {
                long c = queue.Dequeue();
                foreach (long n in new[] { c + 1, c - 1, c + 1000L, c - 1000L })
                {
                    if (cells.Contains(n) && reached.Add(n)) { queue.Enqueue(n); }
                }
            }
            return reached.Count == cells.Count;
        }

        private static string Day(DateTime d) => d.ToString("d MMM yyyy", CultureInfo.InvariantCulture);
    }
}
