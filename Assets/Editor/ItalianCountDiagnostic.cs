using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using PoliSim.Data;
using PoliSim.Elections;
using PoliSim.Elections.Generated;
using UnityEngine;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// PS-7 (rulings K150-K158): ITALY'S COUNT HELD TO THE 2022 RECORD WHERE THE RECORD IS THE INPUT. The catalog (`Tools/it_count_prep.pl`) is held to its
    /// sources by digest; then, fed the record's own 2022 figures, the count must give the record's result:
    /// <list type="bullet">
    /// <item><description>(a) every college that voted on lists, both chambers, elects the group of the candidate the candidates file has first (art. 77 lett. a/b on
    /// the cifre of lett. c) - TOLERANCE: none, every college;</description></item>
    /// <item><description>(b) the Camera's national stage (art. 83 lett. b-g) on Eligendo's published national cifre gives its published proportional seats, list for
    /// list - TOLERANCE: none;</description></item>
    /// <item><description>(c) the whole proportional count on the colleges' derived cifre (the open data's comune returns - which differ from the verified cifre by
    /// up to 0.3 % for a standalone list, `ps7_sources.md`) gives the published proportional seats in both chambers - TOLERANCE: none, as measured on 2026-10-28;
    /// a list one seat off fails, so a later regeneration that moves a seat is seen;</description></item>
    /// <item><description>(d) the circoscrizione stage (lett. h/i, K151) gives every list in the circoscrizioni exactly its national seats, and fills every
    /// circoscrizione's seats - the published per-circoscrizione cifre are BILLED (`ps7_sources.md`), so its seats there are not compared;</description></item>
    /// <item><description>(e) the Camera's Estero on the 2022 votes gives Eligendo's Area Estero seats, list for list - TOLERANCE: none;</description></item>
    /// <item><description>(f) the swing's identity (K150): handed 2018's own national shares, every college's swung votes are its 2018 votes; and a live count on a
    /// moved vote fills the Camera's 400 and the Senato's 200.</description></item>
    /// <item><description>(g) K153, K154: the Senato the count seats holds the senators for life by record, in their groups, beside the 200 elected;</description></item>
    /// <item><description>(h) K157: Draghi's cabinet and support at the start are the parties the Camera's vote of 18 Feb 2021 gives, re-derived from every deputy's vote.</description></item>
    /// <item><description>(i) K155: art. 94's vote in each chamber (the government and its support for, every other member against), the mandates on the chambers the 2022 vote
    /// seats and on a hostile Senato (three at most; a granted one holds both), and art. 61's day after a dissolution - the record's 25 Sep 2022 from 21 Jul.</description></item>
    /// </list>
    /// </summary>
    public static class ItalianCountDiagnostic
    {
        public static void Run()
        {
            CheckExit.ArmLogFold();
            var sb = new StringBuilder("=== ItalianCountDiagnostic (PS-7, K150-K158): Italy's count on the 2022 record ===\n");
            int failures = 0;
            void Check(bool ok, string what) { if (!ok) { failures++; } sb.Append(ok ? "    ok        " : "    FAIL      ").Append(what).Append('\n'); }
            try
            {
                string root = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
                foreach ((string path, string digest) in ItalianCount2022.SourceDigests)
                {
                    string full = Path.Combine(root, path);
                    Check(File.Exists(full) && Sha256(full) == digest, F("the catalog's source {0} is the file on disk (SHA-256 {1})", path, digest.Substring(0, 12)));
                }

                string[] keys = ItalianCount2022.Keys;
                foreach (ItalianCount.Chamber chamber in new[] { ItalianCount.Chamber.Camera, ItalianCount.Chamber.Senato })
                {
                    bool camera = chamber == ItalianCount.Chamber.Camera;
                    long[][] c22 = camera ? ItalianCount2022.Camera2022 : ItalianCount2022.Senato2022;
                    long[] other = camera ? ItalianCount2022.Camera2022Other : ItalianCount2022.Senato2022Other;
                    bool[] listless = camera ? ItalianCount2022.CameraCollegeCandidatesOnly : ItalianCount2022.SenatoCollegeCandidatesOnly;
                    string[] winners = camera ? ItalianCount2022.CameraWinner2022 : ItalianCount2022.SenatoWinner2022;
                    long[] pubVotes = camera ? ItalianCount2022.CameraPublished2022Votes : ItalianCount2022.SenatoPublished2022Votes;
                    int[] pubSeats = camera ? ItalianCount2022.CameraPublished2022Seats : ItalianCount2022.SenatoPublished2022Seats;
                    long pubValid = camera ? ItalianCount2022.CameraPublished2022Valid : ItalianCount2022.SenatoPublished2022Valid;
                    var shares = new Dictionary<string, double>();
                    for (int k = 0; k < keys.Length; k++) { shares[keys[k]] = (double)pubVotes[k] / pubValid; }
                    double[][] votes = c22.Select(row => row.Select(v => (double)v).ToArray()).ToArray();
                    ItalianCount.Result r = ItalianCount.Count(chamber, keys, votes, other, shares, ItalianCount.EsteroRecord(chamber));

                    // (a) the colleges
                    int agree = 0, counted = 0;
                    var misses = new List<string>();
                    for (int c = 0; c < winners.Length; c++)
                    {
                        if (listless[c]) { continue; }
                        counted++;
                        if (r.CollegeWinner[c] == winners[c]) { agree++; } else { misses.Add((camera ? ItalianCount2022.CameraColleges : ItalianCount2022.SenatoColleges)[c] + " " + r.CollegeWinner[c] + " for " + winners[c]); }
                    }
                    Check(counted > 0 && agree == counted, F("(a) {0}: {1} of {2} list-voting colleges elect the record's winner{3}", chamber, agree, counted, misses.Count > 0 ? " - " + string.Join("; ", misses.Take(5)) : string.Empty));

                    // (a') the 2022 candidacy agreement carried: on the record's cifre every college whose winner's own party the generator resolved is counted for that
                    // party (the list candidacy or parliamentary group of record, reading (6))
                    string[] recordParty = camera ? ItalianCount2022.CameraWinnerParty2022 : ItalianCount2022.SenatoWinnerParty2022;
                    int resolved = 0, partyAgrees = 0;
                    for (int c = 0; c < recordParty.Length; c++) { if (string.IsNullOrEmpty(recordParty[c]) || listless[c]) { continue; } resolved++; if (r.CollegeSeatKey[c] == recordParty[c]) { partyAgrees++; } }
                    Check(resolved > 0 && partyAgrees == resolved, F("(a') {0}: {1} of {2} colleges with the winner's party of record are counted for it", chamber, partyAgrees, resolved));
                    // (c) the proportional count on the colleges' derived cifre
                    var off = new List<string>();
                    for (int k = 0; k < keys.Length; k++) { if (r.ProportionalSeats[keys[k]] != pubSeats[k]) { off.Add(F("{0} {1} for {2}", keys[k], r.ProportionalSeats[keys[k]], pubSeats[k])); } }
                    Check(off.Count == 0, F("(c) {0}: the proportional seats on the colleges' derived cifre are Eligendo's published ones, list for list ({1} seats){2}", chamber, pubSeats.Sum(), off.Count > 0 ? " - " + string.Join(", ", off) : string.Empty));

                    // (d) the circoscrizione stage: each list's seats there are its national seats; each circoscrizione filled
                    int[] circSeats = camera ? ItalianCount2022.CameraCircoscrizioneSeats : ItalianCount2022.SenatoCircoscrizioneSeats;
                    int[] collegeCirc = camera ? ItalianCount2022.CameraCollegeCircoscrizione : ItalianCount2022.SenatoCollegeCircoscrizione;
                    bool filled = true;
                    for (int i = 0; i < circSeats.Length; i++)
                    {
                        int colleges = collegeCirc.Count(x => x == i);
                        int got = r.ByCircoscrizione[i].Sum();
                        bool listVotes = Enumerable.Range(0, c22.Length).Any(c => collegeCirc[c] == i && !listless[c]);
                        if (listVotes && got != circSeats[i] - colleges) { filled = false; sb.Append(F("      {0}: {1} proportional seat(s) of {2}\n", ItalianCount.Circoscrizioni(chamber)[i], got, circSeats[i] - colleges)); }
                    }
                    Check(filled, F("(d) {0}: every circoscrizione's proportional seats (its Tabella A seats less its colleges) are filled, and the lists' circoscrizione seats sum to their national ones", chamber));

                    // the chamber's whole size
                    Check(r.Seats.Values.Sum() + r.EsteroUnkeyed == (camera ? 400 : 200), F("{0}: the record's count fills {1} seats ({2})", chamber, camera ? 400 : 200, r.Seats.Values.Sum() + r.EsteroUnkeyed));

                    // (f) the swing's identity on 2018's own national shares
                    long[][] v18 = camera ? ItalianCount2022.Camera2018 : ItalianCount2022.Senato2018;
                    var n18 = new double[keys.Length]; double sum18 = 0.0;
                    foreach (long[] row in v18) { for (int k = 0; k < keys.Length; k++) { n18[k] += row[k]; sum18 += row[k]; } }
                    var shares18 = new Dictionary<string, double>();
                    for (int k = 0; k < keys.Length; k++) { if (n18[k] > 0) { shares18[keys[k]] = n18[k] / sum18; } }
                    double[][] swung = ItalianCount.SwungVotes(chamber, shares18, out string[] _);
                    double worst = 0.0;
                    for (int c = 0; c < v18.Length; c++) { for (int k = 0; k < keys.Length; k++) { worst = Math.Max(worst, Math.Abs(swung[c][k] - v18[c][k])); } }
                    Check(worst < 1e-6, F("(f) {0}: handed 2018's national shares, the swing returns every college's 2018 votes (worst {1:0.0e0} votes)", chamber, worst));

                    // (f) a live count on a moved vote: the published 2022 shares, normalised to the game's sum of one, with ten points moved from FdI to PD
                    var moved = new Dictionary<string, double>();
                    double s = shares.Values.Sum();
                    foreach (KeyValuePair<string, double> kv in shares) { moved[kv.Key] = kv.Value / s; }
                    moved["FdI"] -= 0.10; moved["PD"] += 0.10;
                    ItalianCount.Result live = camera ? ItalianCount.Camera(moved) : ItalianCount.Senato(moved);
                    Check(live.Seats.Values.Sum() + live.EsteroUnkeyed == (camera ? 400 : 200) && live.Seats.Values.All(v => v >= 0),
                        F("(f) {0}: a live count with ten points moved from FdI to PD fills {1} ({2}: {3})", chamber, camera ? 400 : 200, live.Seats.Values.Sum() + live.EsteroUnkeyed,
                            string.Join(" ", live.Seats.Where(kv => kv.Value > 0).Select(kv => kv.Key + " " + kv.Value))));
                    // (f) a key the catalog does not carry (a created party at 15 %, the rest scaled to 85 %) is counted: it wins proportional seats, the chamber is full
                    var created = new Dictionary<string, double>();
                    foreach (KeyValuePair<string, double> kv in shares) { created[kv.Key] = 0.85 * kv.Value / s; }
                    created["NEW"] = 0.15;
                    ItalianCount.Result withNew = camera ? ItalianCount.Camera(created) : ItalianCount.Senato(created);
                    Check(withNew.Seats.TryGetValue("NEW", out int newSeats) && newSeats > 0 && withNew.Seats.Values.Sum() == (camera ? 400 : 200),
                        F("(f) {0}: a created party at 15 % (no lineage, no link) is counted - {1} seat(s), the chamber {2}", chamber, newSeats, withNew.Seats.Values.Sum()));
                    // (e) M2: the live path - the game's own Estero swing on the 2022 shares over the roster (summing to one) changes no list's share, so every
                    // ripartizione elects what the statute elects on the record's own votes: the Camera's 8, the Senato's 4
                    var rosterShares = new Dictionary<string, double>();
                    double rosterSum = shares.Values.Sum();
                    foreach (KeyValuePair<string, double> kv in shares) { rosterShares[kv.Key] = kv.Value / rosterSum; }
                    int[][] estRecord = ItalianCount.EsteroSeatsByList(chamber, ItalianCount.EsteroRecord(chamber));
                    int[][] estLive = ItalianCount.EsteroSeatsByList(chamber, ItalianCount.EsteroVotes(chamber, rosterShares));
                    bool estSame = true; int estSeats = 0;
                    for (int rip = 0; rip < estRecord.Length; rip++) { for (int l = 0; l < estRecord[rip].Length; l++) { estSeats += estLive[rip][l]; if (estLive[rip][l] != estRecord[rip][l]) { estSame = false; } } }
                    double rho = rosterSum;   // the roster's share of the valid vote on the record's day (the published shares sum to it), K205's base
                    ItalianCount.Result liveRecord = camera ? ItalianCount.Camera(rosterShares, rho) : ItalianCount.Senato(rosterShares, rho);
                    Check(estSame && estSeats == (camera ? 8 : 4) && liveRecord.EsteroSeats.Values.Sum() == (camera ? 8 : 4),
                        F("(e) {0}: the live Estero on the 2022 shares over the roster elects the statute's {1} on the record's own votes, list for list (live {2}: {3})", chamber, camera ? 8 : 4, estSeats,
                            string.Join(" ", liveRecord.EsteroSeats.Where(kv => kv.Value > 0).Select(kv => kv.Key + " " + kv.Value))));
                    // (c') M3 / K205, the live path: the count a game runs - K150's swing scaled to each list's national share, the lines read on the roster's share of
                    // the valid vote - on the record's own shares gives the published proportional seats: the Camera list for list (exact, as measured 2026-10-28); the
                    // Senato within two seats per list (its regions read the 2018 geography, not 2022's - measured 2026-10-28: at most 2)
                    int liveWorst = 0; string liveWorstKey = null;
                    for (int k = 0; k < keys.Length; k++) { int d = Math.Abs(liveRecord.ProportionalSeats[keys[k]] - pubSeats[k]); if (d > liveWorst) { liveWorst = d; liveWorstKey = keys[k]; } }
                    Check(liveWorst <= (camera ? 0 : 2), F("(c') {0}: the live count on the record's shares (scaled swing, the lines on the roster's share {1:0.0000}) gives the published proportional seats within {2} per list - the largest miss {3} ({4}): {5}",
                        chamber, rho, camera ? 0 : 2, liveWorst, liveWorstKey ?? "none", string.Join(" ", liveRecord.ProportionalSeats.Where(kv => kv.Value > 0).Select(kv => kv.Key + " " + kv.Value))));
                    // L3 (review 4), REPORTED - not asserted: each party's seats on the record's figures against the chamber of record's own table (the Camera's
                    // per-list totals, flagged unconfirmed in the roster; the Senato's elected senators by the groups bearing a list's name, SEAT-1's table)
                    var ofRecord = new Dictionary<string, int>();
                    if (camera) { foreach (KeyValuePair<string, int> kv in PartySystems.InitialSeats(CountryId.Italy, ElectionVintage.Italy2022)) { ofRecord[kv.Key] = kv.Value; } }
                    else { foreach (var g in PartySystems.ItalySenato2022) { if (g.List != null) { ofRecord[g.List] = g.Elected; } } }
                    sb.Append("      REPORTED (L3): seats on the record's figures / the chamber of record's table - ")
                        .Append(string.Join(" ", keys.Where(k => r.Seats[k] > 0 || (ofRecord.TryGetValue(k, out int o) && o > 0)).Select(k => k + " " + r.Seats[k] + "/" + (ofRecord.TryGetValue(k, out int o2) ? o2.ToString(CultureInfo.InvariantCulture) : "-"))))
                        .Append(camera ? " (the Camera's per-list totals are the roster's unconfirmed ones)\n" : " (groups bearing no list's name - Civici d'Italia, Per le Autonomie, Misto - not compared)\n");
                    sb.Append("      the record's count: ").Append(string.Join(" ", r.Seats.Where(kv => kv.Value > 0).Select(kv => kv.Key + " " + kv.Value))).Append(" (colleges ")
                        .Append(string.Join(" ", r.CollegeSeats.Where(kv => kv.Value > 0).Select(kv => kv.Key + " " + kv.Value))).Append(")\n");

                    if (camera)
                    {
                        // (b) the national stage on the published cifre
                        List<string[]> coalitions = ItalianCount.Coalitions(chamber);
                        var entries = keys.Select((k, i) => new Rosatellum.ListEntry(k, pubVotes[i], coalitions.FindIndex(m => m.Contains(k)), k == ItalianCount.MinorityKey)).ToArray();
                        int[] national = Rosatellum.AllocateNational(entries, pubValid, 245, out string _);
                        var nOff = new List<string>();
                        for (int k = 0; k < keys.Length; k++) { if (national[k] != pubSeats[k]) { nOff.Add(F("{0} {1} for {2}", keys[k], national[k], pubSeats[k])); } }
                        Check(nOff.Count == 0, "(b) Camera: the national stage on Eligendo's published cifre gives its 245 proportional seats, list for list" + (nOff.Count > 0 ? " - " + string.Join(", ", nOff) : string.Empty));

                        // (e) the Estero against Eligendo's Area Estero page
                        Dictionary<string, int> record = EsteroSeatsOnPage(Path.Combine(root, "ElectionsData", "italy", "raw", "reseat_2022", "eligendo_camera_2022_area-estero.html"));
                        var counted22 = new Dictionary<string, int>();
                        int[][] byList = ItalianCount.EsteroSeatsByList(chamber, ItalianCount.EsteroRecord(chamber));   // the production count, on the record's votes
                        for (int rip = 0; rip < byList.Length; rip++)
                        {
                            for (int l = 0; l < byList[rip].Length; l++) { if (byList[rip][l] > 0) { counted22.TryGetValue(ItalianCount2022.EsteroLists[l], out int n); counted22[ItalianCount2022.EsteroLists[l]] = n + byList[rip][l]; } }
                        }
                        bool same = record.Count > 0 && record.Where(kv => kv.Value > 0).All(kv => counted22.TryGetValue(kv.Key, out int n) && n == kv.Value) && counted22.All(kv => record.TryGetValue(kv.Key, out int n) && n == kv.Value);
                        Check(same, "(e) Camera: the Estero on the 2022 votes (L. 459/2001 art. 15, EsteroSeatsByList) gives Eligendo's Area Estero seats - " + string.Join(", ", counted22.Select(kv => kv.Key + " " + kv.Value)));
                    }
                }
                // (g) K153, K154: the Senato the count seats carries the senators for life beside the 200 elected - by record, in their groups (Aut as SVP,
                // the Misto as Misto): six on the first sitting of 13 Oct 2022 (Rubbia in the group he joined on 9 Nov 2022), five from Napolitano's death
                var even = new Dictionary<string, double>();
                foreach (string key in ItalianCount2022.Keys) { even[key] = (double)ItalianCount2022.CameraPublished2022Votes[Array.IndexOf(ItalianCount2022.Keys, key)] / ItalianCount2022.CameraPublished2022Valid; }
                Dictionary<string, int> life22 = ItalianCount.SenatorsForLifeOn(WorldClock.ItalyFirstSitting2022);
                Dictionary<string, int> life27 = ItalianCount.SenatorsForLifeOn(new DateTime(2027, 10, 30));
                Dictionary<string, int> seated = ItalianCount.SenatoSeated(even, WorldClock.ItalyFirstSitting2022);
                Check(life22.Values.Sum() == 6 && life22.TryGetValue("SVP", out int aut22) && aut22 == 3 && life22.TryGetValue("Misto", out int mis22) && mis22 == 3
                      && life27.Values.Sum() == 5 && seated.Values.Sum() == 206,
                    F("(g) the senators for life by record: {0} on 13 Oct 2022 ({1}), {2} on 30 Oct 2027; the Senato seated on the 2022 vote holds {3} (200 elected and the six)",
                        life22.Values.Sum(), string.Join(" ", life22.Select(kv => kv.Key + " " + kv.Value)), life27.Values.Sum(), seated.Values.Sum()));
                try
                {
                    // (h) K157: Draghi's cabinet and support re-derived from the Camera's vote of 18 Feb 2021 - every deputy's vote by group sigla, each deputy once
                    // (the file serves a placeholder deputy's votes four times, ps7_sources.md); a group or component with more votes for than against and abstaining
                    // carries its key (the declared names below, WorldClock.ItalyDraghiCabinet's doc); Italia Viva's deputies sit in PD's seats at the start
                    string path = Path.Combine(Application.dataPath, "..", "ElectionsData", "italy", "raw", "confidence_2021", "dati_camera_vs18_459_001_voti.csv");
                    var groupKey = new Dictionary<string, string> { { "M5S", "M5S" }, { "LEGA", "Lega" }, { "PD", "PD" }, { "FI", "FI" }, { "LEU", "LeU" }, { "IV", "PD" }, { "FDI", "FdI" } };
                    var componentKey = new Dictionary<string, string> { { "M-MIN", "SVP" }, { "M-NCI", "NcI" }, { "M-CDIE", "PlusE" }, { "M-EUMP", "MAIE" } };
                    var votesBy = new Dictionary<string, Dictionary<string, HashSet<string>>>();
                    foreach (string line in File.ReadAllLines(path).Skip(1))
                    {
                        MatchCollection f = Regex.Matches(line, "\"((?:[^\"]|\"\")*)\"");
                        if (f.Count < 5) { continue; }
                        string label = f[2].Groups[1].Value, type = f[3].Groups[1].Value, sigla = f[4].Groups[1].Value;
                        if (!votesBy.TryGetValue(sigla, out Dictionary<string, HashSet<string>> byType)) { votesBy[sigla] = byType = new Dictionary<string, HashSet<string>>(); }
                        if (!byType.TryGetValue(type, out HashSet<string> who)) { byType[type] = who = new HashSet<string>(); }
                        who.Add(label);
                    }
                    int Count(string sigla, string type) => votesBy.TryGetValue(sigla, out Dictionary<string, HashSet<string>> t) && t.TryGetValue(type, out HashSet<string> w) ? w.Count : 0;
                    bool For(string sigla) => Count(sigla, "Favorevole") > Count(sigla, "Contrario") + Count(sigla, "Astensione");
                    var cabinet = new SortedSet<string>(StringComparer.Ordinal);
                    var support = new SortedSet<string>(StringComparer.Ordinal);
                    foreach (string sigla in votesBy.Keys)
                    {
                        if (!For(sigla)) { continue; }
                        if (groupKey.TryGetValue(sigla, out string g)) { cabinet.Add(g); }
                        else if (componentKey.TryGetValue(sigla, out string c)) { support.Add(c); }
                    }
                    bool same = cabinet.SetEquals(WorldClock.ItalyDraghiCabinet) && support.SetEquals(WorldClock.ItalyDraghiSupport) && !For("FDI")
                        && Count("M5S", "Favorevole") + Count("LEGA", "Favorevole") + Count("PD", "Favorevole") + Count("FI", "Favorevole") + Count("IV", "Favorevole") + Count("LEU", "Favorevole") > 315;
                    Check(same, F("(h) K157: Draghi's parties re-derived from the vote of 18 Feb 2021 - cabinet {0}, support {1} (the record's rows: {2} / {3}); FdI against",
                        string.Join("+", cabinet), string.Join("+", support), string.Join("+", WorldClock.ItalyDraghiCabinet), string.Join("+", WorldClock.ItalyDraghiSupport)));
                }
                catch (Exception ex) { failures++; sb.Append("    FAIL      (h) threw: ").Append(ex.Message).Append('\n'); }

                // (i) K155: art. 94 in both chambers, three mandates, art. 88 and art. 61's day
                Check(ConfidenceProcedure.RulesOf(CountryId.Italy) == ConfidenceProcedure.Rules.Parlamento && !ConfidenceProcedure.TakesMotions(CountryId.Italy)
                      && ConfidenceProcedure.ItalianElectionDay(new DateTime(2022, 7, 21)) == new DateTime(2022, 9, 25),
                    F("(i) Italy's rules are art. 94's (no mid-term motion taken up yet); a dissolution of 21 Jul 2022 polls on {0:yyyy-MM-dd} - the record's 25 Sep 2022 (art. 61, the last Sunday in seventy days)",
                        ConfidenceProcedure.ItalianElectionDay(new DateTime(2022, 7, 21))));
                // (j) K206: a dissolution starts a new term - the calendar re-anchors on the new chambers' first sitting (art. 60); the semestre bianco (art. 88 c. 2)
                bool barredLate = WorldClock.ItalyDissolutionBarred(new DateTime(2028, 9, 1), WorldClock.ItalyFirstSitting2022, out DateTime opensLate);
                bool barredCoinciding = WorldClock.ItalyDissolutionBarred(new DateTime(2028, 9, 1), new DateTime(2023, 12, 24), out DateTime _);
                bool barredEarly = WorldClock.ItalyDissolutionBarred(new DateTime(2026, 3, 1), WorldClock.ItalyFirstSitting2022, out DateTime _);
                Check(WorldClock.ItalyOrdinaryAfter(new DateTime(2022, 12, 4), new DateTime(2022, 12, 5)) == new DateTime(2027, 12, 19)
                      && WorldClock.ItalyOrdinaryAfter(new DateTime(2022, 9, 25), new DateTime(2022, 9, 26)) == new DateTime(2027, 10, 10)
                      && barredLate && opensLate == new DateTime(2029, 2, 3) && !barredCoinciding && !barredEarly,
                    F("(j) K206: after a dissolution election on 4 Dec 2022 the next ordinary day is {0:yyyy-MM-dd} (its own term, first sitting 24 Dec 2022); a dissolution on 1 Sep 2028 is barred for the term that sat on 13 Oct 2022 (the President's last six months, the bar lifting {1:yyyy-MM-dd}), allowed where they coincide with the legislature's last six months, and allowed in March 2026",
                        WorldClock.ItalyOrdinaryAfter(new DateTime(2022, 12, 4), new DateTime(2022, 12, 5)), opensLate));
                // (k) L2, planted: lett. h's compensation - a unit taking two decimal seats against one of entitlement gives up the one of its smaller decimal (the
                // first circoscrizione at a tie) to the unit short of its seat with an unused decimal there; every circoscrizione keeps its seat
                int[][] comp = ItalianCount.Distribute(new[] { new long[] { 150, 150 }, new long[] { 50, 50 } }, new[] { 1, 1 }, new[] { 1, 1 }, new long[] { 300, 100 }, new StringBuilder(), "planted");
                Check(comp[0][0] == 0 && comp[0][1] == 1 && comp[1][0] == 1 && comp[1][1] == 0,
                    F("(k) the compensation, planted: A {0}+{1}, B {2}+{3} - A's excess seat (decimal 0.75, the first circoscrizione at a tie) to B's unused decimal (0.25) there; each circoscrizione one seat", comp[0][0], comp[0][1], comp[1][0], comp[1][1]));
                var planted = new Country { Id = CountryId.Italy, Name = "Italy" };
                ConfidenceProcedure.MotionVote granted94 = ConfidenceProcedure.Article94(planted, "Camera", new Dictionary<string, int> { { "FdI", 201 }, { "PD", 199 } }, new[] { "FdI" }, null, "FdI");
                ConfidenceProcedure.MotionVote refused94 = ConfidenceProcedure.Article94(planted, "Senato", new Dictionary<string, int> { { "FdI", 100 }, { "PD", 99 }, { "M5S", 7 } }, new[] { "FdI" }, new[] { "M5S" }, "FdI");
                ConfidenceProcedure.MotionVote tied94 = ConfidenceProcedure.Article94(planted, "Camera", new Dictionary<string, int> { { "FdI", 200 }, { "PD", 200 } }, new[] { "FdI" }, null, "FdI");
                Check(granted94.Carried && granted94.For == 201 && refused94.Carried && refused94.For == 107 && !tied94.Carried && granted94.Title().Contains("Art. 94"),
                    F("(i) art. 94's vote: the government and its support for, every other member against, granted on more for than against - {0}; {1}; {2}", granted94.Title(), refused94.Title(), tied94.Title()));
                // the mandates on the chambers the count seats on the 2022 vote (normalised to the game's sum of one), and on a Senato a single other party holds whole
                var game = new Dictionary<string, double>();
                double total = even.Values.Sum();
                foreach (KeyValuePair<string, double> kv in even) { game[kv.Key] = kv.Value / total; }
                planted.PlayerPartyAbbrev = "PD";
                planted.ParliamentSeats = new Dictionary<string, int>(ItalianCount.Camera(game).Seats);
                planted.SecondChamberSeats = ItalianCount.SenatoSeated(game, WorldClock.ItalyFirstSitting2022);
                DeclarationReading reading22 = DeclarationReading.OfElection(CountryId.Italy, new DateTime(2022, 9, 25));
                List<ItalianFormation.Mandate> onRecord = ItalianFormation.Run(planted, reading22, out ItalianFormation.Mandate grantedOnRecord);
                Check(grantedOnRecord != null && grantedOnRecord.Camera.Carried && grantedOnRecord.Senato.Carried && onRecord.Count <= ConfidenceProcedure.ItalianMandates,
                    "(i) on the chambers the 2022 vote seats, a mandate is granted by both chambers - " + string.Join("; ", onRecord.Select(ItalianFormation.Line)));
                // K216: Italy's pre-election coalitions enter a cabinet as one bloc (the formation's joint masks) - no granted cabinet splits one, and on the chambers
                // the 2022 vote seats the government formed is the centre-right coalition whole, led by FdI (the record's Meloni cabinet)
                IReadOnlyList<string[]> blocs = ChamberRules.CabinetBlocs(CountryId.Italy);
                bool whole = grantedOnRecord != null && blocs.All(b => { int inside = b.Count(k => grantedOnRecord.View.Cabinet.Exists(c => c.Abbrev == k) && planted.ParliamentSeats.TryGetValue(k, out int s) && s > 0); int seated = b.Count(k => planted.ParliamentSeats.TryGetValue(k, out int s) && s > 0); return inside == 0 || inside == seated; });
                bool meloni = grantedOnRecord != null && new HashSet<string>(grantedOnRecord.View.Cabinet.Select(c => c.Abbrev)).SetEquals(new[] { "FdI", "Lega", "FI", "NM" }) && grantedOnRecord.Camera.PmParty == "FdI";
                Check(blocs.Count == 2 && whole && meloni, F("(i) K216: the coalitions are cabinet blocs ({0}); the government on the 2022 chambers is {1} led by {2} - the record's FdI+Lega+FI+NM led by FdI",
                    string.Join(" | ", blocs.Select(b => string.Join("+", b))), grantedOnRecord != null ? string.Join("+", grantedOnRecord.View.Cabinet.Select(c => c.Abbrev)) : "none", grantedOnRecord?.Camera.PmParty ?? "-"));
                string notInFirst = null;
                if (grantedOnRecord != null && onRecord[0].View != null) { foreach (string key in new[] { "M5S", "AzIV", "PD", "FdI" }) { if (!onRecord[0].View.Cabinet.Exists(c => c.Abbrev == key) && !onRecord[0].View.Support.Exists(c => c.Abbrev == key)) { notInFirst = key; break; } } }
                if (notInFirst != null)
                {
                    planted.SecondChamberSeats = new Dictionary<string, int> { { notInFirst, 206 } };
                    List<ItalianFormation.Mandate> split = ItalianFormation.Run(planted, reading22, out ItalianFormation.Mandate grantedSplit);
                    Check(split.Count >= 2 && split[0].Senato != null && !split[0].Senato.Carried && split.Count <= ConfidenceProcedure.ItalianMandates
                          && (grantedSplit == null ? split.Count == ConfidenceProcedure.ItalianMandates : grantedSplit.Camera.Carried && grantedSplit.Senato.Carried),
                        F("(i) a Senato {0} holds whole refuses the first mandate's government, and the next mandates are tried - three at most, a granted one holding both chambers: {1}", notInFirst, string.Join("; ", split.Select(ItalianFormation.Line))));
                }
                else { Check(false, "(i) no party stands outside the first mandate's government to plant a hostile Senato with"); }
            }
            catch (Exception ex) { failures++; sb.Append("    FAIL      threw: ").Append(ex.Message).Append('\n'); }

            sb.Append(failures == 0 ? "=== ItalianCountDiagnostic: the 2022 record reproduced where it is the input ===" : F("=== ItalianCountDiagnostic: {0} FAILED ===", failures));
            if (failures == 0) { Debug.Log(sb.ToString()); } else { Debug.LogError(sb.ToString()); }
            CheckExit.Finish(failures == 0 ? 0 : 1);
        }

        /// <summary>Eligendo's Area Estero page: each list's seats ("name|votes|pct|seats").</summary>
        private static Dictionary<string, int> EsteroSeatsOnPage(string path)
        {
            string t = File.ReadAllText(path);
            t = Regex.Replace(t, "<script.*?</script>", string.Empty, RegexOptions.Singleline);
            t = Regex.Replace(t, "<[^>]+>", "|");
            t = Regex.Replace(t, @"\s+", " ");
            t = Regex.Replace(t, @"(\| ?)+", "|");
            var seats = new Dictionary<string, int>();
            Match body = Regex.Match(t, @"Seggi\|(.*?)\|TOTALI\|");
            if (!body.Success) { return seats; }
            string[] cells = body.Groups[1].Value.Split('|');
            for (int i = 0; i + 3 < cells.Length; i += 4) { seats[cells[i].Trim().Replace("&#39;", "'")] = int.Parse(cells[i + 3].Trim(), CultureInfo.InvariantCulture); }
            return seats;
        }

        private static string Sha256(string path)
        {
            using (var sha = System.Security.Cryptography.SHA256.Create()) { return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-", string.Empty).ToLowerInvariant(); }
        }

        private static string F(string format, params object[] args) => string.Format(CultureInfo.InvariantCulture, format, args);
    }
}
