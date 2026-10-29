using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using PoliSim.Data;
using PoliSim.Elections;
using PoliSim.Elections.Generated;
using PoliSim.UI;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// D-IT (Design's boards 34a-34d): ITALY'S ELECTION NIGHT, its arithmetic and its sheet's rules - fails while any of these is false.
    /// (a) The cells: the Camera's 392 in Italy and 8 abroad (400), the Senato's 196, 4 and the senators for life of the 2022 first sitting (206); every block's
    /// cells its legal magnitude as the game's count reports it (`ItalianCount2022`'s Tabella A seats), its colleges the count's own colleges of that
    /// circoscrizione (region) in U order, its indices once each; the generated header's digest the table's recorded one, and the file's where the file is on disk.
    /// (b) The order inside each block: the colleges first, then the list seats in the night's fixed order (`ItalianNight.Order`), never re-sorted - and at the
    /// close each block's list cells are exactly the count's list seats there (no seat over, no cell empty), on the 2022 vote and a seeded sweep.
    /// (c) No ink on a list seat or an abroad seat before the close; a college inked exactly when it has declared; the senators for life inked from the start.
    /// (d) The lines: half the chamber plus one, read off the chamber's size - 201 of 400 and 104 of 206 on 2022's chambers, 103 of 205 on a Senato without
    /// Napolitano.
    /// (e) The call (K281): at EVERY declared count, both chambers, the leader's certain seats - its colleges won plus the floor of its list seats - never
    /// exceed the seats it ends with in Italy (a floor that did would call what the count can take back), on the 2022 vote, a seeded sweep and the reviews'
    /// plants (lists just over 3 %, a coalition at 10.03 %, three near-line lists that made an allocation set's call wrong).
    /// (f) F1: at the close the night's seats are the chambers' - the Camera's as `NationalElection.Run` seats it, the Senato's as `ItalianCount.SenatoSeated`.
    /// (g) Every text the Italian sheet draws is at the 14 px floor at 1280 (a board pixel is 1.5 canvas units) and every fixed text cell is sized from its
    /// words' measure (`Measure`), never a size or a Px plus a constant; the map's labels at 14 board px in a view drawn at 1.5 units a board pixel.
    /// (h) The inks: Italy's ten drawn in board 34d's hexes, unmoved by the nudge; IC, ScN, MAIE and UV given none.
    /// </summary>
    public static class ItalianNightDiagnostic
    {
        private static string F(string format, params object[] args) => string.Format(CultureInfo.InvariantCulture, format, args);

        /// <summary>[AUTHORED-REFERENCE] board 34d's legend, as installed in `PoliSimTheme`.</summary>
        private static readonly Dictionary<string, string> BoardInks = new Dictionary<string, string>
        {
            { "FdI", "385575" }, { "PD", "75383B" }, { "Lega", "38753D" }, { "M5S", "756B38" }, { "FI", "538499" },
            { "AzIV", "6E3F75" }, { "AVS", "2D5E4E" }, { "NM", "2A3054" }, { "SVP", "54302A" }, { "PlusE", "57482B" },
        };

        private static readonly DateTime FirstSitting2022 = new DateTime(2022, 10, 13);

        public static void Run()
        {
            CheckExit.ArmLogFold();
            var sb = new StringBuilder("=== ItalianNightDiagnostic (D-IT): Italy's election night, its arithmetic and its sheet ===\n");
            int failures = 0;
            void Check(bool ok, string what) { if (!ok) { failures++; } sb.Append(ok ? "    ok        " : "    FAIL      ").Append(what).Append('\n'); }
            try
            {
                string root = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
                Dictionary<string, double> record = RecordShares(out double cameraRoster);
                double senatoRoster = ItalianCount.SenatoRosterShare2022;

                // (a) the cells
                foreach (ItalianCount.Chamber chamber in new[] { ItalianCount.Chamber.Camera, ItalianCount.Chamber.Senato })
                {
                    bool camera = chamber == ItalianCount.Chamber.Camera;
                    ItalianNightCartogram.Chamber g = camera ? ItalianNightCartogram.Camera : ItalianNightCartogram.Senato;
                    int[] magnitude = camera ? ItalianCount2022.CameraCircoscrizioneSeats : ItalianCount2022.SenatoCircoscrizioneSeats;
                    int[] collegeCirc = camera ? ItalianCount2022.CameraCollegeCircoscrizione : ItalianCount2022.SenatoCollegeCircoscrizione;
                    string[] colleges = camera ? ItalianCount2022.CameraColleges : ItalianCount2022.SenatoColleges;
                    int blocks = g.BlockCode.Length;
                    var cells = new int[blocks];
                    var seen = new HashSet<string>();
                    bool indices = true;
                    for (int i = 0; i < g.Cells.GetLength(0); i++)
                    {
                        int b = g.Cells[i, 2], j = g.Cells[i, 3];
                        if (b < 0 || b >= blocks || j < 0 || j >= magnitude[b] || !seen.Add(b + ":" + j)) { indices = false; continue; }
                        cells[b]++;
                    }
                    var wrong = new List<string>();
                    for (int b = 0; b < blocks; b++)
                    {
                        int[] cols = g.BlockColleges[b];
                        int inCirc = collegeCirc.Count(x => x == b);
                        bool uOrder = true;
                        for (int k = 1; k < cols.Length; k++) { if (UNumber(colleges[cols[k]]) <= UNumber(colleges[cols[k - 1]])) { uOrder = false; } }
                        if (cells[b] != magnitude[b] || cols.Length != inCirc || cols.Any(ci => collegeCirc[ci] != b) || !uOrder) { wrong.Add(F("{0} cells {1} of magnitude {2}, colleges {3} of {4}{5}", g.BlockCode[b], cells[b], magnitude[b], cols.Length, inCirc, uOrder ? string.Empty : ", not in U order")); }
                    }
                    int italy = magnitude.Sum(), abroad = (camera ? ItalianCount2022.CameraEsteroSeats : ItalianCount2022.SenatoEsteroSeats).Sum();
                    int life = camera ? 0 : ItalianCount.SenatorsForLifeSeatedOn(FirstSitting2022).Count;
                    int wantItaly = camera ? 392 : 196, wantAbroad = camera ? 8 : 4, wantLife = camera ? 0 : 6;
                    Check(indices && wrong.Count == 0 && g.Cells.GetLength(0) == italy && italy == wantItaly && abroad == wantAbroad && life == wantLife,
                        F("(a) {0}: {1} cells in Italy in {2} blocks + {3} abroad{4} = {5}; every block's cells its magnitude as the count reports it, its colleges the count's own in U order, its indices once each{6}",
                            chamber, g.Cells.GetLength(0), blocks, abroad, camera ? string.Empty : F(" + {0} for life", life), g.Cells.GetLength(0) + abroad + life, wrong.Count > 0 ? " - " + string.Join("; ", wrong) : string.Empty));
                }
                string generated = SourceText.Read(Path.Combine(Application.dataPath, "Scripts", "Elections", "Generated", "ItalianNightCartogram.cs"));
                Match header = Regex.Match(generated, @"// SHA-256: ([0-9a-f]{64})");
                Check(header.Success && header.Groups[1].Value == ItalianNightCartogram.SourceDigest, F("(a) the generated header's source digest is the table's recorded one ({0})", ItalianNightCartogram.SourceDigest.Substring(0, 12)));
                string source = null;
                foreach (string up in new[] { "..", Path.Combine("..", "..") })   // the main checkout's sibling, or a worktree's (PoliSim-wt/<name>)
                {
                    string candidate = Path.GetFullPath(Path.Combine(root, up, "PoliSim-captures", "design", "RETURN_2026-10-29_DIT", "night.json"));
                    if (source == null && File.Exists(candidate)) { source = candidate; }
                }
                if (source != null) { Check(Sha256(source) == ItalianNightCartogram.SourceDigest, F("(a) the table's source is the file out of tree ({0})", source)); }
                else { sb.Append("    note      (a) the table's source is out of tree and not on this disk; the header's digest is asserted above\n"); }

                // (b) the order inside each block and its fill at the close; (c) the inks by the count's stage; (d) the lines; (e) the call; (f) F1 - on the 2022 vote
                ItalianNight.State final = ItalianNight.At(ItalianNight.Total, record, cameraRoster, senatoRoster, FirstSitting2022);
                Check(BlocksFill(final, out string fillWhy), "(b) on the 2022 vote: every block's list cells are the count's list seats there, in the night's fixed order, none over and none empty" + fillWhy);
                foreach (int index in new[] { 0, 61, ItalianNight.Total - 1, ItalianNight.Total })
                {
                    ItalianNight.State s = index == ItalianNight.Total ? final : ItalianNight.At(index, record, cameraRoster, senatoRoster, FirstSitting2022);
                    foreach (ItalianCount.Chamber chamber in new[] { ItalianCount.Chamber.Camera, ItalianCount.Chamber.Senato })
                    {
                        Check(InksByStage(s, chamber, out string why), F("(c) {0} at {1} of {2}: list seats and abroad seats {3}; a college inked exactly when declared; the senators for life inked from the start{4}",
                            chamber, index, ItalianNight.Total, index == ItalianNight.Total ? "inked at the close" : "paper (to come)", why));
                    }
                }
                Check(final.Camera.Seats == 400 && final.Camera.Line == 201 && final.Senato.Seats == 206 && final.Senato.Line == 104,
                    F("(d) the lines on 2022's chambers: Camera {0} of {1}, Senato {2} of {3} - half the chamber plus one", final.Camera.Line, final.Camera.Seats, final.Senato.Line, final.Senato.Seats));
                ItalianNight.State later = ItalianNight.At(0, record, cameraRoster, senatoRoster, new DateTime(2023, 10, 13));
                Check(later.Senato.LifeSeats == 5 && later.Senato.Seats == 205 && later.Senato.Line == 103,
                    F("(d) the line read off the chamber's size: a Senato first sitting on 13 Oct 2023 (Napolitano's mandate closed by the record) has {0} for life, {1} seats, the line {2}", later.Senato.LifeSeats, later.Senato.Seats, later.Senato.Line));

                // (e) the call's floor (K281), on the 2022 vote and the reviews' plants, at every declared count
                Check(BoundHolds(final, out string boundWhy, out int boundMargin, out int boundReach), F("(e) on the 2022 vote, at every declared count of both chambers: the leader's certain seats never exceed the seats it ends with in Italy (least margin before the close {0}){1}", boundMargin, boundWhy));
                Check(boundReach > 0 && final.Camera.Called && final.Camera.CalledAt < ItalianNight.Total && final.Senato.Called && final.Senato.CalledAt < ItalianNight.Total,
                    F("(e) on the 2022 vote the floor reaches: list seats before the close at {0} count(s), and both chambers called before the close (Camera at {1}, Senato at {2} of {3})", boundReach, final.Camera.CalledAt, final.Senato.CalledAt, ItalianNight.Total));

                foreach ((string name, Dictionary<string, double> fixedShares) in Plants)
                {
                    Dictionary<string, double> planted = Plant(record, fixedShares);
                    ItalianNight.State near = ItalianNight.At(ItalianNight.Total, planted, 1.0, 1.0, FirstSitting2022);
                    bool holds = BoundHolds(near, out string nearWhy, out int nearMargin, out int _) & BlocksFill(near, out string fillNear);
                    nearWhy += fillNear;
                    Check(holds, F("(e) planted - {0}: at every declared count the certain seats never exceed the end, every list seat placed at the close (least margin before the close {1}; Camera called at {2}, Senato at {3}){4}", name, nearMargin, near.Camera.CalledAt, near.Senato.CalledAt, nearWhy));
                }
                sb.Append(F("    info      the 2022 vote: Camera called at {0}, Senato at {1}, both at {2} (of {3}; the board's order, the game's count)\n", final.Camera.CalledAt, final.Senato.CalledAt, final.CalledAt, ItalianNight.Total));

                // the sweep: each list's share moved by a seeded factor, the vote renormalised
                var random = new System.Random(20261029);
                int sweep = 6, sweepBad = 0;
                string sweepWhy = null;
                for (int t = 0; t < sweep; t++)
                {
                    var moved = new Dictionary<string, double>();
                    foreach (KeyValuePair<string, double> kv in record) { moved[kv.Key] = kv.Value * (0.5 + random.NextDouble()); }
                    double sum = moved.Values.Sum();
                    foreach (string k in moved.Keys.ToList()) { moved[k] /= sum; }
                    ItalianNight.State s = ItalianNight.At(ItalianNight.Total, moved, 1.0, 1.0, FirstSitting2022);
                    if (!BlocksFill(s, out string w1)) { sweepBad++; sweepWhy = sweepWhy ?? w1; }
                    if (!BoundHolds(s, out string w2, out int _, out int _)) { sweepBad++; sweepWhy = sweepWhy ?? w2; }
                }
                Check(sweepBad == 0, F("(b, e) a seeded sweep of {0} national votes (each list 0.5 to 1.5 times its 2022 share): every block filled in order at the close, the floor never over the end{1}", sweep, sweepWhy ?? string.Empty));

                // (f) F1: the chambers as the game seats them
                ElectionRecord held = NationalElection.Run(CountryId.Italy, 0, record, WorldClock.LatestElectionDay(CountryId.Italy), cameraRoster);
                var off = new List<string>();
                foreach (KeyValuePair<string, int> kv in held.Seats) { if (final.Camera.SeatsOf(kv.Key) != kv.Value) { off.Add(F("Camera {0} {1} for {2}", kv.Key, final.Camera.SeatsOf(kv.Key), kv.Value)); } }
                Dictionary<string, int> senato = ItalianCount.SenatoSeated(record, FirstSitting2022, senatoRoster);
                var nightSenato = new Dictionary<string, int>(final.Senato.Result.Seats);
                foreach ((string _, string _, string key) in final.Senato.Life) { nightSenato.TryGetValue(key, out int n); nightSenato[key] = n + 1; }
                foreach (KeyValuePair<string, int> kv in senato) { int n = nightSenato.TryGetValue(kv.Key, out int x) ? x : 0; if (n != kv.Value) { off.Add(F("Senato {0} {1} for {2}", kv.Key, n, kv.Value)); } }
                Check(off.Count == 0, F("(f) F1: at the close the night's seats are the chambers' - the Camera as NationalElection.Run seats it, the Senato as SenatoSeated seats it (with the {0} for life){1}", final.Senato.LifeSeats, off.Count > 0 ? " - " + string.Join("; ", off) : string.Empty));

                // art. 84 (information: the night draws what the count says)
                int m5s = Array.IndexOf(ItalianCount2022.Keys, "M5S");
                string[] circs = ItalianCount2022.CameraCircoscrizioni;
                var where = new List<string>();
                foreach (string name in new[] { "CAMPANIA 1", "VENETO 1", "LOMBARDIA 2", "LOMBARDIA 4", "LAZIO 2" }) { int i = Array.IndexOf(circs, name); if (i >= 0) { where.Add(name + " " + final.Camera.Result.ByCircoscrizione[i][m5s]); } }
                sb.Append("    info      the Camera counts at lett. h/i, no art. 84 cascade (K151) - M5S's list seats on the 2022 vote: " + string.Join(" · ", where) + "\n");

                // (g) the 14 px floor and measured widths, read off the sheet's source; the map's labels off its plan
                Check(SizesHold(out string sizeWhy), "(g) every text the Italian sheet creates is at 14 board px or more (1.5 canvas units each: 14 px at 1280), every fixed text cell's width from its words' measure, no size plus a pad" + sizeWhy);
                Check(MapLabelsHold(final, out string labelWhy), "(g) the map's labels and captions at 14 board px, in a view drawn at 1.5 canvas units a board pixel" + labelWhy);

                // (h) the inks
                bool inks = true;
                var drawn = new List<string>();
                foreach (KeyValuePair<string, string> kv in BoardInks)
                {
                    string hex = ColorUtility.ToHtmlStringRGB(PoliSimTheme.PartyLaddered(CountryId.Italy, kv.Key));
                    drawn.Add(kv.Key + " #" + hex);
                    if (!PoliSimTheme.HasPartyInk(CountryId.Italy, kv.Key) || !string.Equals(hex, kv.Value, StringComparison.OrdinalIgnoreCase)) { inks = false; }
                }
                bool none = new[] { "IC", "ScN", "MAIE", "UV" }.All(k => !PoliSimTheme.HasPartyInk(CountryId.Italy, k));
                Check(inks && none, F("(h) Italy's ten lists drawn in board 34d's hexes, unmoved by the nudge ({0}); IC, ScN, MAIE and UV given none (the Neutral)", string.Join(" · ", drawn)));
            }
            catch (Exception e) { failures++; sb.Append("    THREW: " + e.GetType().Name + ": " + e.Message + "\n" + e.StackTrace + "\n"); }
            if (failures > 0) { Debug.LogError($"ITALIAN NIGHT: {failures} failure(s).\n{sb}"); CheckExit.Finish(1); return; }
            Debug.Log(sb.ToString());
            CheckExit.Finish(0);
        }

        /// <summary>Eligendo's 2022 Camera shares over the roster's own sum (the game's shares sum to one) and the roster's share of the valid vote - the 2022 polling
        /// day's count as the game holds it.</summary>
        private static Dictionary<string, double> RecordShares(out double roster)
        {
            string[] keys = ItalianCount2022.Keys;
            long[] pub = ItalianCount2022.CameraPublished2022Votes;
            long sum = pub.Sum();
            var shares = new Dictionary<string, double>();
            for (int k = 0; k < keys.Length; k++) { shares[keys[k]] = sum > 0 ? (double)pub[k] / sum : 0.0; }
            roster = ItalianCount2022.CameraPublished2022Valid > 0 ? (double)sum / ItalianCount2022.CameraPublished2022Valid : 1.0;
            return shares;
        }

        private static int UNumber(string college)
        {
            Match m = Regex.Match(college, @"U(\d+)$");
            return m.Success ? int.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture) : -1;
        }

        /// <summary>(b): each block's list cells at the close hold exactly the count's list seats there, in the night's fixed order.</summary>
        private static bool BlocksFill(ItalianNight.State s, out string why)
        {
            var bad = new List<string>();
            foreach (ItalianNight.ChamberNight c in new[] { s.Camera, s.Senato })
            {
                int pool = c.ItalySeats - c.Colleges, placed = c.Result.ProportionalSeats.Values.Sum();
                if (placed != pool || c.Result.ListSeatsShort != 0) { bad.Add(F("{0}: {1} list seats placed of its pool {2} (K283)", c.Of, placed, pool)); }
                List<string> order = c.OrderedKeys();
                for (int b = 0; b < c.Geometry.BlockCode.Length; b++)
                {
                    string[] list = c.BlockListKeys(b, out int overflow);
                    int empty = list.Count(k => k == null);
                    bool ordered = true;
                    for (int i = 1; i < list.Length; i++) { if (list[i] != null && list[i - 1] != null && order.IndexOf(list[i]) < order.IndexOf(list[i - 1])) { ordered = false; } }
                    int seats = c.Result.ByCircoscrizione[b].Sum();
                    if (overflow > 0 || empty > 0 || !ordered || seats != list.Length) { bad.Add(F("{0} {1}: {2} list cells, {3} seats, {4} over, {5} empty{6}", c.Of, c.Geometry.BlockCode[b], list.Length, seats, overflow, empty, ordered ? string.Empty : ", out of order")); }
                }
            }
            why = bad.Count > 0 ? " - " + string.Join("; ", bad.Take(6)) : string.Empty;
            return bad.Count == 0;
        }

        /// <summary>(c): the map's plan by the night's stage - the sheet's own plan (`ElectionNightScreen.MapPlan`), read by reflection.</summary>
        private static bool InksByStage(ItalianNight.State s, ItalianCount.Chamber chamber, out string why)
        {
            ItalianNight.ChamberNight c = s.Of(chamber);
            ItalianCellView.Plan plan = Plan(s, c);
            var bad = new List<string>();
            ItalianNightCartogram.Chamber g = c.Geometry;
            int italy = g.Cells.GetLength(0);
            for (int i = 0; i < italy; i++)
            {
                int b = g.Cells[i, 2], j = g.Cells[i, 3];
                bool inked = plan.Cells[i].Ink.HasValue;
                if (j < g.BlockColleges[b].Length)
                {
                    bool declared = c.CollegeDeclared[g.BlockColleges[b][j]];
                    if (inked != declared) { bad.Add(F("{0} college {1} {2}", g.BlockCode[b], j, declared ? "declared, not inked" : "inked before it declared")); }
                }
                else if (inked != s.Final) { bad.Add(F("{0} list cell {1} {2}", g.BlockCode[b], j, s.Final ? "not inked at the close" : "inked before the close")); }
            }
            int abroad = c.AbroadSeats;
            for (int i = italy; i < italy + abroad; i++) { if (plan.Cells[i].Ink.HasValue != s.Final) { bad.Add("an abroad seat " + (s.Final ? "not inked at the close" : "inked before the close")); break; } }
            for (int i = italy + abroad; i < plan.Cells.Count; i++) { if (!plan.Cells[i].Ink.HasValue) { bad.Add("a senator for life not inked"); break; } }
            if (plan.Cells.Count != italy + abroad + c.LifeSeats) { bad.Add(F("{0} cells planned for {1}", plan.Cells.Count, italy + abroad + c.LifeSeats)); }
            why = bad.Count > 0 ? " - " + string.Join("; ", bad.Take(5)) : string.Empty;
            return bad.Count == 0;
        }

        private static ItalianCellView.Plan Plan(ItalianNight.State s, ItalianNight.ChamberNight c)
        {
            MethodInfo mapPlan = typeof(ElectionNightScreen).GetMethod("MapPlan", BindingFlags.NonPublic | BindingFlags.Static);
            if (mapPlan == null) { throw new InvalidOperationException("ElectionNightScreen.MapPlan is not found - the check reads the sheet's own plan"); }
            var night = new ElectionNightScreen.ItalyNight { State = s, Chamber = c.Of, PollingDay = WorldClock.LatestElectionDay(CountryId.Italy), FirstSitting = FirstSitting2022 };
            object[] args = { night, c, null };
            return (ItalianCellView.Plan)mapPlan.Invoke(null, args);
        }

        /// <summary>(e): at EVERY declared count - the Camera 1 to 147, the Senato at each count the night's pairing reaches - the leader's certain seats (K281's
        /// floor) against the seats it ends with in Italy (its colleges and its list seats). <paramref name="tightest"/>: the least margin end − certain before the close;
        /// <paramref name="nonzeroBefore"/>: the counts before the close where the floor holds list seats (a floor of nothing would pass the first test and never call).</summary>
        private static bool BoundHolds(ItalianNight.State s, out string why, out int tightest, out int nonzeroBefore)
        {
            var bad = new List<string>();
            tightest = int.MaxValue;
            nonzeroBefore = 0;
            int lastSenato = -1;
            for (int i = 1; i <= ItalianNight.Total; i++)
            {
                foreach (ItalianNight.ChamberNight c in new[] { s.Camera, s.Senato })
                {
                    int declared = c.Of == ItalianCount.Chamber.Camera ? i : ItalianNight.SenatoDeclaredAt(i);
                    if (c.Of == ItalianCount.Chamber.Senato) { if (declared == lastSenato) { continue; } lastSenato = declared; }
                    ItalianNight.Certain(c, declared, out string[] leader, out int certain, out int _, out int listFloor);
                    if (leader == null) { continue; }
                    int end = 0;
                    foreach (string key in leader) { c.Result.CollegeSeats.TryGetValue(key, out int a); c.Result.ProportionalSeats.TryGetValue(key, out int b); end += a + b; }
                    if (declared < c.Colleges)
                    {
                        tightest = Math.Min(tightest, end - certain);   // before the close: at the close the certain seats are the count's own by construction
                        if (listFloor > 0) { nonzeroBefore++; }
                    }
                    if (certain > end) { bad.Add(F("{0} at {1}: {2} certain for {3}, which ends with {4} in Italy", c.Of, declared, certain, string.Join("+", leader), end)); }
                }
            }
            why = bad.Count > 0 ? " - " + string.Join("; ", bad.Take(5)) : string.Empty;
            return bad.Count == 0;
        }

        /// <summary>The 2022 vote with <paramref name="fixedShares"/> set as given and every other list scaled to the rest - the review's plants.</summary>
        private static Dictionary<string, double> Plant(Dictionary<string, double> record, Dictionary<string, double> fixedShares)
        {
            var planted = new Dictionary<string, double>();
            double fixedSum = fixedShares.Values.Sum();
            double others = record.Where(kv => !fixedShares.ContainsKey(kv.Key)).Sum(kv => kv.Value);
            foreach (KeyValuePair<string, double> kv in record) { planted[kv.Key] = fixedShares.ContainsKey(kv.Key) ? fixedShares[kv.Key] : kv.Value * (1.0 - fixedSum) / others; }
            foreach (KeyValuePair<string, double> kv in fixedShares) { planted[kv.Key] = kv.Value; }
            return planted;
        }

        /// <summary>[AUTHORED] the plants the reviews built against the call's earlier forms (2026-10-29): lists just over 3 % (one, two, three - a created party
        /// "NEW" among them), the centre-left's coalition at 10.03 %, FdI swept where three near-line lists turned the set-minimum's call wrong.</summary>
        private static readonly (string Name, Dictionary<string, double> Shares)[] Plants =
        {
            ("Az-IV alone at 3.02 %", new Dictionary<string, double> { { "AzIV", 0.0302 } }),
            ("Az-IV and NEW at 3.01 %", new Dictionary<string, double> { { "AzIV", 0.0301 }, { "NEW", 0.0301 } }),
            ("Az-IV at 3.01 %, CSX at 10.03 % (PD 5.00, AVS 2.95, +E 2.08)", new Dictionary<string, double> { { "AzIV", 0.0301 }, { "PD", 0.0500 }, { "AVS", 0.0295 }, { "PlusE", 0.0208 } }),
            ("Az-IV, ScN and NEW at 3.01 %, FdI 19.05 %", new Dictionary<string, double> { { "AzIV", 0.0301 }, { "ScN", 0.0301 }, { "NEW", 0.0301 }, { "FdI", 0.1905 } }),
            ("Az-IV, ScN and NEW at 3.01 %, FdI 19.30 %", new Dictionary<string, double> { { "AzIV", 0.0301 }, { "ScN", 0.0301 }, { "NEW", 0.0301 }, { "FdI", 0.1930 } }),
            ("Az-IV, ScN and NEW at 3.01 %, FdI 19.55 %", new Dictionary<string, double> { { "AzIV", 0.0301 }, { "ScN", 0.0301 }, { "NEW", 0.0301 }, { "FdI", 0.1955 } }),
            ("review 3's vote: Lega 11.56 % with FdI and FI under 3 %, four lists near 17 % (K283)", new Dictionary<string, double>
            {
                { "Lega", 0.1156 }, { "FdI", 0.0269 }, { "FI", 0.0296 }, { "NM", 0.0172 }, { "PD", 0.0475 }, { "AVS", 0.0215 }, { "PlusE", 0.0241 }, { "IC", 0.0280 },
                { "M5S", 0.1829 }, { "AzIV", 0.1673 }, { "ScN", 0.1673 }, { "NEW", 0.1720 },
            }),
        };

        /// <summary>(g): the text-creating calls in the sheet's files, their size argument parsed - every `Px(N)` in it at least 14; every fixed cell's width a
        /// measured value; no "size +" arithmetic.</summary>
        private static bool SizesHold(out string why)
        {
            var bad = new List<string>();
            int calls = 0;
            // the size argument's position (0-based) in each text-creating call
            var sizeAt = new Dictionary<string, int> { { "ItText", 4 }, { "PlText", 3 }, { "FixedCell", 3 }, { "MakeTextRealWeight", 4 }, { "Stamp", 2 }, { "FacedButton", 4 } };
            foreach (string file in new[] { "ElectionNightScreen.Italy.cs", "ItalianCellView.cs" })
            {
                string text = SourceText.ReadWithoutComments(Path.Combine(Application.dataPath, "Scripts", "UI", file));
                if (Regex.IsMatch(text, @"(\b(fontSize|size|Size)|Px\([^)]*\))\s*\+\s*\d")) { bad.Add(file + ": a size or a Px plus a constant"); }
                foreach (KeyValuePair<string, int> kv in sizeAt)
                {
                    foreach (Match m in Regex.Matches(text, @"\b" + kv.Key + @"\("))
                    {
                        if (m.Index > 0 && Regex.IsMatch(text.Substring(Math.Max(0, m.Index - 30), Math.Min(30, m.Index)), @"(static|private|public)\s+\w+\s*$")) { continue; }   // a declaration, not a call
                        List<string> args = Args(text, m.Index + m.Length - 1);
                        if (args.Count <= kv.Value) { continue; }
                        calls++;
                        string size = args[kv.Value];
                        if (!SizeOk(text, size, out string sizeWhy)) { bad.Add(F("{0}: {1}(… {2} …) {3}", file, kv.Key, size.Trim(), sizeWhy)); }
                        if (kv.Key == "FixedCell" && !Measured(text, args[2])) { bad.Add(F("{0}: FixedCell width {1} is not measured", file, args[2].Trim())); }
                    }
                }
            }
            why = F(" ({0} calls read)", calls) + (bad.Count > 0 ? " - " + string.Join("; ", bad.Take(6)) : string.Empty);
            return bad.Count == 0 && calls > 40;
        }

        private static bool SizeOk(string text, string size, out string why)
        {
            why = null;
            MatchCollection px = Regex.Matches(size, @"Px\((\d+(?:\.\d+)?)f?\)");
            if (px.Count > 0)
            {
                foreach (Match p in px) { if (double.Parse(p.Groups[1].Value, CultureInfo.InvariantCulture) < 14.0) { why = "under 14 board px"; return false; } }
                return true;
            }
            string id = size.Trim();
            if (Regex.IsMatch(id, @"^\w+$"))
            {
                if (Regex.IsMatch(text, @"\([^)]*\bint\s+" + id + @"\s*,")) { return true; }   // a wrapper's own size parameter, passed through - its callers are read
                Match decl = Regex.Match(text, @"\b(?:int|float|var)\s+" + id + @"\s*=\s*([^;]+);");
                if (decl.Success)
                {
                    if (decl.Groups[1].Value.Contains(".Size * scale")) { return true; }   // the map's labels: their board size times the view's scale, held by MapLabelsHold
                    return SizeOk(text, decl.Groups[1].Value, out why);
                }
            }
            why = "no Px(N) size";
            return false;
        }

        private static bool Measured(string text, string width)
        {
            if (width.Contains("Measure(")) { return true; }
            string id = width.Trim();
            return Regex.IsMatch(id, @"^\w+$") && Regex.IsMatch(text, @"\b" + id + @"\s*=\s*[^;]*Measure\(");
        }

        /// <summary>A call's arguments from its opening parenthesis, split at top-level commas (strings and nested brackets respected).</summary>
        private static List<string> Args(string text, int open)
        {
            var args = new List<string>();
            int depth = 0;
            var current = new StringBuilder();
            for (int i = open; i < text.Length; i++)
            {
                char ch = text[i];
                if (ch == '"')
                {
                    int j = i + 1;
                    while (j < text.Length && text[j] != '"') { if (text[j] == '\\') { j++; } j++; }
                    if (depth >= 1) { current.Append(text, i, Math.Min(text.Length, j + 1) - i); }
                    i = j;
                    continue;
                }
                if (ch == '(' || ch == '[' || ch == '{') { depth++; if (depth == 1) { continue; } }
                else if (ch == ')' || ch == ']' || ch == '}') { depth--; if (depth == 0) { args.Add(current.ToString()); return args; } }
                else if (ch == ',' && depth == 1) { args.Add(current.ToString()); current.Clear(); continue; }
                current.Append(ch);
            }
            return args;
        }

        /// <summary>(g): the map's plan labels at 14 board px or more, and the sheet creating the view at 1.5 canvas units a board pixel (`BoardPx`).</summary>
        private static bool MapLabelsHold(ItalianNight.State s, out string why)
        {
            var bad = new List<string>();
            foreach (ItalianNight.ChamberNight c in new[] { s.Camera, s.Senato })
            {
                ItalianCellView.Plan plan = Plan(s, c);
                foreach (ItalianCellView.Label l in plan.Labels) { if (l.Size < 14f) { bad.Add(F("{0} label {1} at {2} board px", c.Of, l.Text, l.Size)); } }
            }
            string sheet = SourceText.ReadWithoutComments(Path.Combine(Application.dataPath, "Scripts", "UI", "ElectionNightScreen.Italy.cs"));
            if (!Regex.IsMatch(sheet, @"ItalianCellView\.Create\([^;]*,\s*BoardPx\)")) { bad.Add("the view is not created at BoardPx units a board pixel"); }
            why = bad.Count > 0 ? " - " + string.Join("; ", bad.Take(5)) : string.Empty;
            return bad.Count == 0;
        }

        private static string Sha256(string path)
        {
            using (SHA256 sha = SHA256.Create()) { return string.Concat(sha.ComputeHash(File.ReadAllBytes(path)).Select(b => b.ToString("x2"))); }
        }
    }
}
