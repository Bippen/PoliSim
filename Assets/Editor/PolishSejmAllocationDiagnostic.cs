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
    /// PS-5, PART THREE (§730): POLAND'S SEJM BY ITS OWN LAW, PROVEN ON THE RECORD BEFORE IT GOES LIVE. The Kodeks wyborczy as the PKW's notice and the
    /// returns file cite it (`ElectionsData/poland/returns_2023.md`, Rules): <b>41 multi-member districts</b> (art. 201), <b>d'Hondt in each district
    /// with no national tier</b> (art. 232 §1), and the national thresholds - <b>5 % of valid votes for a party's committee, 8 % for a coalition
    /// committee</b> (art. 196 §1-2), a national minority's committee exempt on declaration (art. 197; MN 2023). Run on the KBW's district votes
    /// (`district_votes_2023.csv`, every committee's national sum cross-checked against the notice) and the districts' magnitudes (the returns file's
    /// table, summing to 460), it must give the 10th-term Sejm the PKW announced - <b>PiS 194, KO 157, TD 65, NL 26, Konf 18, MN 0</b> (Dz.U. 2023
    /// poz. 2234) - seat for seat. The thresholds' edges are planted: a coalition at 7.9 % is out where a party at 7.9 % is in, and the minority's
    /// exemption seats nothing it does not win.
    ///
    /// <para><b>LIVE since §762</b> (`PolishSejmAllocation`, `NationalElection`'s Poland case): the game's national vote spread over the districts by
    /// a uniform swing from 2023's district results, the standard reading this instrument named. The live block below holds the generated table
    /// (`Tools/sejm_districts_prep.pl`) against both files, reproduces the record through the game's own path, and moves it by a swing.</para>
    /// </summary>
    public static class PolishSejmAllocationDiagnostic
    {
        private enum Kind { Party, Coalition, Minority }

        /// <summary>The 2023 committees' kinds, by their names in the notice: KO and TD were KOALICYJNY committees (8 %), PiS, NL and Konf party
        /// committees (5 %), MN a minority's voters' committee that declared the exemption (art. 197).</summary>
        private static readonly Dictionary<string, Kind> Kinds2023 = new Dictionary<string, Kind>
        {
            { "PiS", Kind.Party }, { "KO", Kind.Coalition }, { "TD", Kind.Coalition }, { "NL", Kind.Party }, { "Konf", Kind.Party }, { "MN", Kind.Minority },
        };

        /// <summary>SOURCED: the 10th-term Sejm as the PKW's notice announced it (Dz.U. 2023 poz. 2234; returns_2023.md's national table).</summary>
        private static readonly Dictionary<string, int> Record2023 = new Dictionary<string, int> { { "PiS", 194 }, { "KO", 157 }, { "TD", 65 }, { "NL", 26 }, { "Konf", 18 }, { "MN", 0 } };

        /// <summary>SOURCED: the 2023 valid votes nationwide, every committee's (returns_2023.md: "Valid votes total 21,596,674").</summary>
        private const long Valid2023 = 21596674L;

        public static void Run()
        {
            CheckExit.ArmLogFold();
            var sb = new StringBuilder("=== PolishSejmAllocationDiagnostic (PS-5 part three, §730): the Sejm by its own law, on the 2023 record ===\n");
            int failures = 0;
            void Check(bool ok, string what) { if (!ok) { failures++; } sb.Append(ok ? "    ok        " : "    FAIL      ").Append(what).Append('\n'); }
            try
            {
                string root = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "ElectionsData", "poland"));
                (string[] committees, List<long[]> votes) = ReadDistrictVotes(Path.Combine(root, "district_votes_2023.csv"));
                int[] magnitudes = ReadMagnitudes(Path.Combine(root, "returns_2023.md"));
                Check(votes.Count == 41 && magnitudes.Length == 41, F("41 districts read - votes {0}, magnitudes {1}", votes.Count, magnitudes.Length));
                Check(magnitudes.Sum() == 460, F("the magnitudes sum to the Sejm's 460 ({0})", magnitudes.Sum()));

                var national = new long[committees.Length];
                foreach (long[] d in votes) { for (int c = 0; c < committees.Length; c++) { national[c] += d[c]; } }
                sb.Append("    national votes (the districts summed):");
                for (int c = 0; c < committees.Length; c++) { sb.Append(F(" {0} {1:N0} ({2:0.00} %)", committees[c], national[c], 100.0 * national[c] / Valid2023)); }
                sb.Append('\n');

                Dictionary<string, int> seats = Allocate(committees, national, votes, magnitudes, Valid2023, Kinds2023, out List<string> eligible);
                Check(eligible.SequenceEqual(new[] { "PiS", "KO", "TD", "NL", "Konf", "MN" }), "the thresholds admit " + string.Join(", ", eligible) + " (PiS, NL, Konf over 5 %; KO, TD over 8 %; MN exempt)");
                foreach (KeyValuePair<string, int> r in Record2023)
                {
                    seats.TryGetValue(r.Key, out int got);
                    Check(got == r.Value, F("{0}: {1} seat(s) - the PKW's {2}", r.Key, got, r.Value));
                }
                Check(seats.Values.Sum() == 460, F("460 seats allocated ({0})", seats.Values.Sum()));

                // ---- §762: THE COUNT LIVE - the generated table, the record through the game's own path, a swing ----
                bool tableMatches = PolishSejmDistricts2023.Committees.SequenceEqual(committees) && PolishSejmDistricts2023.Votes.Length == votes.Count
                    && PolishSejmDistricts2023.Magnitudes.SequenceEqual(magnitudes);
                for (int d = 0; tableMatches && d < votes.Count; d++) { tableMatches = PolishSejmDistricts2023.Votes[d].SequenceEqual(votes[d]); }
                string csvDigest = Sha256(Path.Combine(root, "district_votes_2023.csv")), mdDigest = Sha256(Path.Combine(root, "returns_2023.md"));
                Check(tableMatches && PolishSejmDistricts2023.SourceDigest == csvDigest && PolishSejmDistricts2023.MagnitudesDigest == mdDigest,
                    "§762: the generated table (Tools/sejm_districts_prep.pl) holds both files figure for figure, their digests current");
                long six = national.Sum();
                var shares2023 = new Dictionary<string, double>();
                for (int c = 0; c < committees.Length; c++) { shares2023[committees[c]] = (double)national[c] / six; }
                ElectionRecord live = NationalElection.Run(CountryId.Poland, 0, shares2023, new DateTime(2023, 10, 15));
                bool record = live.Method == ElectionMethod.PolandDistricts;
                foreach (KeyValuePair<string, int> r in Record2023) { record &= live.Seats.TryGetValue(r.Key, out int got) && got == r.Value; }
                Check(record && live.Seats.Values.Sum() == 460, F("§762: NationalElection.Run on 2023's national shares (on the committees' own sum - no swing) returns the record seat for seat: {0}",
                    string.Join(", ", live.Seats.Where(kv => kv.Value > 0).Select(kv => kv.Key + " " + kv.Value))));
                var swung = new Dictionary<string, double>(shares2023);
                swung["PiS"] += 0.05; swung["KO"] -= 0.05;
                Dictionary<string, int> moved = PolishSejmAllocation.Allocate(swung, out List<Dictionary<string, int>> perDistrict);
                Check(moved["PiS"] > Record2023["PiS"] && moved["KO"] < Record2023["KO"] && moved.Values.Sum() == 460 && perDistrict.Count == 41,
                    F("§762: five points from KO to PiS, swung uniformly over the 41 okregi - PiS {0} (from 194), KO {1} (from 157), 460 in all", moved["PiS"], moved["KO"]));
                // the live thresholds, seen as which lists take part in the okregi's divisions (Allocate's per-district result)
                var edge = new Dictionary<string, double>(shares2023);
                double toPiS = (edge["Konf"] - 0.049) + (edge["TD"] - 0.079) + (edge["NL"] - 0.05) + (edge["MN"] - 0.0001);
                edge["Konf"] = 0.049; edge["TD"] = 0.079; edge["NL"] = 0.05; edge["MN"] = 0.0001; edge["PiS"] += toPiS;
                Dictionary<string, int> edgeSeats = PolishSejmAllocation.Allocate(edge, out List<Dictionary<string, int>> edgeDistricts);
                bool konfOut = edgeDistricts.All(d => !d.ContainsKey("Konf")) && edgeSeats["Konf"] == 0, tdOut = edgeDistricts.All(d => !d.ContainsKey("TD")) && edgeSeats["TD"] == 0;
                bool nlIn = edgeDistricts.Any(d => d.ContainsKey("NL")), minorityIn = edgeDistricts.Any(d => d.ContainsKey("MN"));
                Check(konfOut && tdOut && nlIn && minorityIn && NationalElection.ExemptFromThreshold(CountryId.Poland, "MN"),
                    F("§762: the live thresholds - Konfederacja (a party) at 4.9 % out {0}, TD (a coalition) at 7.9 % out {1}, NL (a party) at 5.0 % in {2}, the minority at 0.01 % in {3} (NationalElection.ExemptFromThreshold)",
                        konfOut, tdOut, nlIn, minorityIn));
                var withSld = new Dictionary<string, double>(shares2023);
                withSld["SLD"] = 0.06; withSld["NL"] -= 0.06;
                Dictionary<string, int> sld = PolishSejmAllocation.Allocate(withSld, out _);
                Check(sld["SLD"] > 0 && sld.Values.Sum() == 460, F("§762: a list with no 2023 committee (SLD at 6 %) takes its national share in every okreg - {0} seat(s)", sld["SLD"]));

                // ---- the rule's edges, planted ----
                var edgeKinds = new Dictionary<string, Kind> { { "A", Kind.Party }, { "Coalition", Kind.Coalition }, { "Party", Kind.Party } };
                string[] edgeNames = { "A", "Coalition", "Party" };
                Allocate(edgeNames, new long[] { 842_000, 79_000, 79_000 }, new List<long[]> { new long[] { 842_000, 79_000, 79_000 } }, new[] { 20 }, 1_000_000L, edgeKinds, out List<string> edgeIn);
                Check(!edgeIn.Contains("Coalition") && edgeIn.Contains("Party"), "art. 196: a coalition committee at 7.9 % is out (8 %), a party's at 7.9 % is in (5 %)");
                var minorityKinds = new Dictionary<string, Kind> { { "A", Kind.Party }, { "B", Kind.Party }, { "MN", Kind.Minority } };
                Dictionary<string, int> mn = Allocate(new[] { "A", "B", "MN" }, new long[] { 600_000, 397_000, 3_000 }, new List<long[]> { new long[] { 600_000, 397_000, 3_000 } },
                    new[] { 10 }, 1_000_000L, minorityKinds, out List<string> mnIn);
                Check(mnIn.Contains("MN") && mn["MN"] == 0, "art. 197: the minority's committee at 0.3 % takes part in the division and wins nothing its quotients do not reach");
            }
            catch (Exception ex) { failures++; sb.Append("    FAIL      threw: ").Append(ex.Message).Append('\n'); }

            sb.Append(failures == 0 ? "=== PolishSejmAllocationDiagnostic: the 2023 Sejm, seat for seat ===" : F("=== PolishSejmAllocationDiagnostic: {0} FAILED ===", failures));
            if (failures == 0) { Debug.Log(sb.ToString()); } else { Debug.LogError(sb.ToString()); }
            CheckExit.Finish(failures == 0 ? 0 : 1);
        }

        /// <summary>The law: the thresholds on the national valid votes (every committee's, the denominator the PKW uses), then d'Hondt in each district
        /// among the committees admitted. Ties at the last seat go to the committee with more votes in the district (art. 232 §2's order is the
        /// district's votes; not reached on the record).</summary>
        private static Dictionary<string, int> Allocate(string[] committees, long[] national, List<long[]> districts, int[] magnitudes, long validTotal, Dictionary<string, Kind> kinds, out List<string> eligible)
        {
            var admitted = new bool[committees.Length];
            eligible = new List<string>();
            for (int c = 0; c < committees.Length; c++)
            {
                double share = 100.0 * national[c] / validTotal;
                Kind kind = kinds.TryGetValue(committees[c], out Kind k) ? k : Kind.Party;
                admitted[c] = kind == Kind.Minority || share >= (kind == Kind.Coalition ? 8.0 : 5.0);
                if (admitted[c]) { eligible.Add(committees[c]); }
            }
            var seats = committees.ToDictionary(c => c, c => 0);
            for (int d = 0; d < districts.Count; d++)
            {
                var won = new int[committees.Length];
                for (int s = 0; s < magnitudes[d]; s++)
                {
                    int best = -1;
                    double bestQuotient = -1.0;
                    for (int c = 0; c < committees.Length; c++)
                    {
                        if (!admitted[c] || districts[d][c] <= 0) { continue; }
                        double q = districts[d][c] / (double)(won[c] + 1);
                        if (q > bestQuotient || (q == bestQuotient && best >= 0 && districts[d][c] > districts[d][best])) { best = c; bestQuotient = q; }
                    }
                    if (best < 0) { break; }
                    won[best]++;
                }
                for (int c = 0; c < committees.Length; c++) { seats[committees[c]] += won[c]; }
            }
            return seats;
        }

        private static (string[] Committees, List<long[]> Votes) ReadDistrictVotes(string path)
        {
            string[] header = null;
            var rows = new List<long[]>();
            foreach (string raw in File.ReadAllLines(path, Encoding.UTF8))
            {
                if (raw.Length == 0 || raw[0] == '#') { continue; }
                string[] f = raw.Split(';');
                if (header == null) { header = f.Skip(1).ToArray(); continue; }
                rows.Add(f.Skip(1).Select(v => long.Parse(v, CultureInfo.InvariantCulture)).ToArray());
            }
            return (header, rows);
        }

        /// <summary>The magnitudes from returns_2023.md's district table: "| 1 Legnica (12) | ...".</summary>
        private static int[] ReadMagnitudes(string path)
        {
            var byDistrict = new SortedDictionary<int, int>();
            var row = new Regex(@"^\| (\d+) [^|]*\((\d+)\) \|");
            foreach (string line in File.ReadAllLines(path, Encoding.UTF8))
            {
                Match m = row.Match(line);
                if (m.Success) { byDistrict[int.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture)] = int.Parse(m.Groups[2].Value, CultureInfo.InvariantCulture); }
            }
            return byDistrict.Values.ToArray();
        }

        /// <summary>§762: a file's SHA-256, lower-case hex - the generated table's recorded digests are checked against the files they were read from.</summary>
        private static string Sha256(string path)
        {
            using (var sha = System.Security.Cryptography.SHA256.Create()) { return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-", string.Empty).ToLowerInvariant(); }
        }

        private static string F(string format, params object[] args) => string.Format(CultureInfo.InvariantCulture, format, args);
    }
}
