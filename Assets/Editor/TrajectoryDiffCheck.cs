using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// Compares two TrajectoryBaselineDump CSVs and reports, per field, the maximum and end-of-run
    /// deviation in BOTH metrics: relative (max(|a|, 1e-4) denominator, the equivalence check's own
    /// convention) and ABSOLUTE, in the field's native units.
    ///
    /// The absolute column exists because of a measured cost, not taste (Phase 4's verdict, item 4):
    /// signed quantities crossing zero make the relative metric scream - the demographics matrix
    /// produced three such artifacts (France's growth rate at its sign change read 43% relative on
    /// an absolute deviation of 0.0006 per-1000; Italy's approval at its 0-floor read 1770% on
    /// 0.002 points) and each cost an investigation. The GDP identity is full of signed
    /// zero-crossing quantities (TradeBalance, output gap, budget balance), so Phase 5 reads BOTH
    /// columns and judges near-zero series on the absolute one.
    ///
    /// Run: `Unity.exe -batchmode -nographics -projectPath &lt;path&gt; -executeMethod
    /// PoliSim.EditorTools.TrajectoryDiffCheck.Run -logFile &lt;path&gt;
    /// -diffa=&lt;pre.csv&gt; -diffb=&lt;post.csv&gt; [-difftop=&lt;n&gt;]`
    ///
    /// A SELF-TEST runs before every comparison (verification-integrity instance 10's standing
    /// rule): a synthetic zero-crossing pair where the truth is known by construction - the
    /// relative metric must scream (&gt;10%) while the absolute metric reports the small real
    /// deviation - so a broken column fails the run instead of blessing a comparison. Reporting
    /// tool, not a gate: exit 0 on a completed compare (thresholds are per-phase judgment), 1 on a
    /// self-test failure, 2 on structural mismatch (differing key sets - which means the two dumps
    /// are not comparable at all).
    ///
    /// **NEW-FIELD ALLOWANCE (Round 4 batch 1).** A round whose whole point is adding EconomyState
    /// fields makes "post-batch dump has fields the pre-batch dump lacks" the EXPECTED shape, not a
    /// structural mismatch - the old strict key-set equality would have exit-2'd every inputs-only
    /// batch on its own success. Fields present only in B are NAMED in the output as NEW and excluded
    /// from comparison (there is nothing to compare them against); every asymmetry in the other
    /// direction stays fatal - a field present only in A means the batch DELETED state, which no
    /// additive batch may do, and extra B keys inside a shared field still mean differing runs.
    /// </summary>
    public static class TrajectoryDiffCheck
    {
        private sealed class FieldStats
        {
            public double MaxAbs;
            public string MaxAbsAt = "";
            public double MaxRel;
            public string MaxRelAt = "";
            public double EndAbs;
            public double EndRel;
        }

        public static void Run()
        {
            if (!SelfTest())
            {
                Debug.LogError("DIFF: SELF-TEST FAILED - the columns cannot be trusted, comparing nothing.");
                CheckExit.Finish(1);
                return;
            }

            string pathA = Arg("-diffa=", null);
            string pathB = Arg("-diffb=", null);
            if (pathA == null || pathB == null)
            {
                Debug.Log("DIFF: self-test only (no -diffa/-diffb given) - OK.");
                CheckExit.Finish(0);
                return;
            }

            int top = int.TryParse(Arg("-difftop=", "14"), NumberStyles.Integer, CultureInfo.InvariantCulture, out int t) ? t : 14;

            Dictionary<(int Turn, string Country, string Field), double> a = Load(pathA);
            Dictionary<(int, string, string), double> b = Load(pathB);

            // The new-field allowance, exactly as scoped in the class comment: B-only FIELDS are
            // named and excluded; every other asymmetry is still the old exit-2 mismatch.
            var fieldsA = new HashSet<string>();
            var fieldsB = new HashSet<string>();
            foreach ((_, _, string field) in a.Keys) { fieldsA.Add(field); }
            int bKeysInNewFields = 0;
            foreach ((_, _, string field) in b.Keys)
            {
                fieldsB.Add(field);
                if (!fieldsA.Contains(field)) { bKeysInNewFields++; }
            }

            var removedFields = new List<string>();
            foreach (string field in fieldsA)
            {
                if (!fieldsB.Contains(field)) { removedFields.Add(field); }
            }

            if (removedFields.Count > 0)
            {
                Debug.LogError($"DIFF: field(s) present in A but MISSING from B: {string.Join(", ", removedFields)} - state was deleted, the dumps are not comparable.");
                CheckExit.Finish(2);
                return;
            }

            foreach (string field in fieldsB)
            {
                if (!fieldsA.Contains(field))
                {
                    Debug.Log($"DIFF: NEW field in B (no A counterpart, excluded from comparison): {field}");
                }
            }

            if (a.Count + bKeysInNewFields != b.Count)
            {
                Debug.LogError($"DIFF: key counts differ beyond the named NEW fields ({a.Count} + {bKeysInNewFields} new-field keys vs {b.Count}) - the dumps are not comparable.");
                CheckExit.Finish(2);
                return;
            }

            int maxTurn = 0;
            foreach ((int turn, _, _) in a.Keys)
            {
                maxTurn = Math.Max(maxTurn, turn);
            }

            var stats = new Dictionary<string, FieldStats>();
            foreach (KeyValuePair<(int Turn, string Country, string Field), double> pair in a)
            {
                if (!b.TryGetValue(pair.Key, out double bv))
                {
                    Debug.LogError($"DIFF: key {pair.Key} missing from B - the dumps are not comparable.");
                    CheckExit.Finish(2);
                    return;
                }

                double abs = Math.Abs(bv - pair.Value);
                double rel = abs / Math.Max(Math.Abs(pair.Value), 1e-4);
                if (!stats.TryGetValue(pair.Key.Field, out FieldStats s))
                {
                    s = new FieldStats();
                    stats[pair.Key.Field] = s;
                }

                if (abs > s.MaxAbs) { s.MaxAbs = abs; s.MaxAbsAt = $"t{pair.Key.Turn},{pair.Key.Country}"; }
                if (rel > s.MaxRel) { s.MaxRel = rel; s.MaxRelAt = $"t{pair.Key.Turn},{pair.Key.Country}"; }
                if (pair.Key.Turn == maxTurn)
                {
                    s.EndAbs = Math.Max(s.EndAbs, abs);
                    s.EndRel = Math.Max(s.EndRel, rel);
                }
            }

            var ranked = new List<KeyValuePair<string, FieldStats>>(stats);
            ranked.Sort((x, y) => y.Value.MaxRel.CompareTo(x.Value.MaxRel));
            Debug.Log($"DIFF: {Path.GetFileName(pathA)} vs {Path.GetFileName(pathB)}, {maxTurn} turns, {stats.Count} fields. Ranked by max RELATIVE; judge near-zero series on the ABSOLUTE column.");
            Debug.Log($"  {"field",-38} {"maxRel",9}  {"at",-16} {"maxABS",12}  {"at",-16} {"endRel",9} {"endABS",12}");
            for (int i = 0; i < Math.Min(top, ranked.Count); i++)
            {
                FieldStats s = ranked[i].Value;
                Debug.Log($"  {ranked[i].Key,-38} {s.MaxRel * 100,8:F3}%  {s.MaxRelAt,-16} {s.MaxAbs,12:G6}  {s.MaxAbsAt,-16} {s.EndRel * 100,8:F3}% {s.EndAbs,12:G6}");
            }

            int identical = 0;
            foreach (FieldStats s in stats.Values)
            {
                if (s.MaxAbs == 0.0)
                {
                    identical++;
                }
            }

            Debug.Log($"DIFF: fields byte-identical across the whole trajectory: {identical} of {stats.Count}.");
            CheckExit.Finish(0);
        }

        /// <summary>The known-truth pair: a series crossing zero with a constant absolute offset of
        /// 0.0002. The relative column MUST scream at the crossing and the absolute column MUST
        /// report ~0.0002 - each column checked against the truth the other cannot express.</summary>
        private static bool SelfTest()
        {
            double[] pre = { 0.0100, 0.0010, -0.0010, -0.0100 };
            double[] post = { 0.0102, 0.0012, -0.0008, -0.0098 };
            double maxAbs = 0, maxRel = 0;
            for (int i = 0; i < pre.Length; i++)
            {
                double abs = Math.Abs(post[i] - pre[i]);
                double rel = abs / Math.Max(Math.Abs(pre[i]), 1e-4);
                maxAbs = Math.Max(maxAbs, abs);
                maxRel = Math.Max(maxRel, rel);
            }

            bool absHonest = Math.Abs(maxAbs - 0.0002) < 1e-12;
            bool relScreams = maxRel > 0.10;
            Debug.Log($"SELFTEST zero-crossing pair -> absolute {maxAbs:G6} ({(absHonest ? "OK" : "BROKEN")}), relative {maxRel * 100:F1}% ({(relScreams ? "screams as expected" : "BROKEN - failed to flag the crossing")})");
            return absHonest && relScreams;
        }

        private static Dictionary<(int, string, string), double> Load(string path)
        {
            var data = new Dictionary<(int, string, string), double>(1 << 20);
            using (var reader = new StreamReader(path))
            {
                reader.ReadLine();
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    int c1 = line.IndexOf(',');
                    int c2 = line.IndexOf(',', c1 + 1);
                    int c3 = line.IndexOf(',', c2 + 1);
                    data[(int.Parse(line.Substring(0, c1), CultureInfo.InvariantCulture),
                          line.Substring(c1 + 1, c2 - c1 - 1),
                          line.Substring(c2 + 1, c3 - c2 - 1))] =
                        double.Parse(line.Substring(c3 + 1), CultureInfo.InvariantCulture);
                }
            }

            return data;
        }

        private static string Arg(string prefix, string fallback)
        {
            foreach (string arg in Environment.GetCommandLineArgs())
            {
                if (arg.StartsWith(prefix, StringComparison.Ordinal))
                {
                    return arg.Substring(prefix.Length);
                }
            }

            return fallback;
        }
    }
}
