using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// The coherence audit, sweep (c) — **artifact identity, generalised past captures: every produced
    /// evidence artifact must prove the thing it claims.**
    ///
    /// <para><b>S-20 established the class on the capture side</b>: every `-shotelectionnight` film ever
    /// taken photographed the Desk under board 1h's name, at 0 failed and exit 0, because nothing checked
    /// that the screen under test was the screen on screen. That is now guarded by a token in the frame.
    /// ⚠ **The other artifact family had no such guard at all.**</para>
    ///
    /// <para><b>The trajectory CSVs carry NO identity.</b> Their filename —
    /// <c>traj_&lt;label&gt;_s&lt;seed&gt;_t&lt;horizon&gt;.csv</c> — is the entire claim, and nothing
    /// inside the file verifies any part of it. This matters concretely: this project asserts *"6 of 6
    /// byte-identical"* against these files constantly, and **a mislabelled dump — a `-trajlabel` typo, a
    /// horizon that silently fell back — would compare cleanly against the wrong twin and read as
    /// proof.**</para>
    ///
    /// <para><b>THE ENUMERATION, and what it can prove without changing the format.</b> For every
    /// <c>traj_*.csv</c> in the capture directory: the row count must equal
    /// <c>horizon × countries × fields</c> plus the header; the turn column must run 1…horizon with no
    /// gaps; and the country count must be the six the model has. **A file claiming `t100` with 126 000
    /// data rows is mislabelled, and this says so.**</para>
    ///
    /// <para>⚠ <b>Why not a header line inside the file, which would be stronger.</b> It would change
    /// every CSV's bytes, and the reference family every comparison in this project measures against is
    /// exactly those bytes. The format change is worth making — it is recorded as the next step — but it
    /// must be made deliberately, with the reference family re-dumped in the same commit, not as a side
    /// effect of adding a check.</para>
    /// </summary>
    public static class ArtifactIdentityCheck
    {
        /// <summary>⚠ The label may contain underscores — `clear_p1`, `omni_final` — so it is greedy and
        /// the anchor is the `_s&lt;digits&gt;_t&lt;digits&gt;.csv` suffix. The first run required an
        /// alphanumeric label and called 354 correctly-named files "unnameable", which is a check
        /// inventing a contract the project never had.</summary>
        private static readonly Regex Name = new Regex(@"^traj_(?<label>.+)_s(?<seed>\d+)_t(?<horizon>\d+)\.csv$");

        /// <summary>The six countries every dump covers — asserted, not assumed, because a dump that
        /// silently lost a country would otherwise still divide evenly.</summary>
        private const int Countries = 6;

        public static void Run()
        {
            CheckExit.ArmLogFold();

            string dir = Arg("-artifactdir=", "../PoliSim-captures/trajectories");
            var sb = new StringBuilder();
            sb.Append("=== The coherence audit (c): artifact identity ===\n");

            if (!Directory.Exists(dir))
            {
                Debug.LogError($"ARTIFACT: no trajectory directory at '{dir}'. Reporting nothing rather than reporting clean.");
                CheckExit.Finish(1);
                return;
            }

            string[] files = Directory.GetFiles(dir, "traj_*.csv");
            int checkedCount = 0, unparseable = 0;
            var failures = new List<string>();

            foreach (string file in files)
            {
                string name = Path.GetFileName(file);
                Match match = Name.Match(name);
                if (!match.Success)
                {
                    unparseable++;
                    Debug.LogError($"ARTIFACT: '{name}' does not match the trajectory naming contract "
                                   + "traj_<label>_s<seed>_t<horizon>.csv. An artifact whose NAME cannot be read is an artifact "
                                   + "whose claim cannot be checked.");
                    failures.Add(name);
                    continue;
                }

                int horizon = int.Parse(match.Groups["horizon"].Value, CultureInfo.InvariantCulture);

                var turns = new HashSet<int>();
                var countries = new HashSet<string>();
                var fields = new HashSet<string>();
                int rows = 0;

                using (var reader = new StreamReader(file))
                {
                    reader.ReadLine();   // the column header
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        if (line.Length == 0) { continue; }
                        rows++;
                        string[] parts = line.Split(',');
                        if (parts.Length < 3) { continue; }
                        if (int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out int turn)) { turns.Add(turn); }
                        countries.Add(parts[1]);
                        fields.Add(parts[2]);
                    }
                }

                checkedCount++;
                int expected = horizon * Countries * fields.Count;
                bool rowsOk = rows == expected;
                bool turnsOk = turns.Count == horizon;
                bool countriesOk = countries.Count == Countries;

                if (rowsOk && turnsOk && countriesOk)
                {
                    sb.Append(F("    ok    {0,-34} {1} turns x {2} countries x {3} fields = {4} rows\n",
                        name, horizon, Countries, fields.Count, rows));
                    continue;
                }

                failures.Add(name);
                Debug.LogError($"ARTIFACT: '{name}' does not contain what its name claims — {rows} data rows against an expected "
                               + $"{expected} ({horizon} turns x {Countries} countries x {fields.Count} fields); {turns.Count} distinct "
                               + $"turn(s) against {horizon}; {countries.Count} country/countries against {Countries}. ⚠ A comparison "
                               + "against a mislabelled artifact passes cleanly and proves nothing.");
                sb.Append(F("    ⚠ FAIL {0,-34} rows {1} vs {2}, turns {3} vs {4}, countries {5} vs {6}\n",
                    name, rows, expected, turns.Count, horizon, countries.Count, Countries));
            }

            sb.Append(F("\n    THE ENUMERATION: {0} traj_*.csv artifact(s) in '{1}'; {2} checked, {3} unnameable, {4} FAILED.\n",
                files.Length, dir, checkedCount, unparseable, failures.Count));
            sb.Append("    ⚠ NEXT STEP, recorded not done: an identity HEADER inside each file (label, seed, horizon, vintage)\n");
            sb.Append("    is strictly stronger than deriving identity from the row count. It changes every CSV's bytes, and the\n");
            sb.Append("    reference family every comparison here measures against is exactly those bytes - so it must be made\n");
            sb.Append("    deliberately, with the family re-dumped in the same commit, not as a side effect of adding a check.\n");

            if (failures.Count == 0)
            {
                sb.Append("    CLEAN - every trajectory artifact contains what its name claims.\n");
                Debug.Log(sb.ToString());
                CheckExit.Finish(0);
                return;
            }

            Debug.LogError(sb.ToString());
            CheckExit.Finish(1);
        }

        private static string Arg(string prefix, string fallback)
        {
            foreach (string arg in Environment.GetCommandLineArgs())
            {
                if (arg.StartsWith(prefix, StringComparison.Ordinal)) { return arg.Substring(prefix.Length); }
            }

            return fallback;
        }

        private static string F(string format, params object[] args)
            => string.Format(CultureInfo.InvariantCulture, format, args);
    }
}
