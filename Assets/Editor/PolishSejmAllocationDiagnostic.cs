using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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
    /// <para><b>Not live yet.</b> Poland's chamber is NotImplemented on the live path (NationalElection): wiring this in needs each district's share
    /// from the national vote on the polling day (a uniform swing from 2023's district results is the standard reading) and moves Poland's
    /// trajectory, so it lands as its own sentinel family. This proves the law first.</para>
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

        private static string F(string format, params object[] args) => string.Format(CultureInfo.InvariantCulture, format, args);
    }
}
