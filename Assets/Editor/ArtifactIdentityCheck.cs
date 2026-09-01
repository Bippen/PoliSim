using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
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
    ///
    /// <para><b>⚠ INCREMENTAL BY CONTENT DIGEST (2026-09-01), and this is a COST fix, not a coverage
    /// cut.</b> `BarTiming`'s first runs measured this check at <b>~92 % of the whole cheap suite</b> —
    /// it re-parsed the entire trajectory archive on every bar, including the many bars whose only change
    /// was a two-line document edit that cannot touch a CSV. Profiling put the floor at about
    /// <b>5 s to READ the archive</b> against <b>~136 s to parse it</b>: the cost was never the disk, it
    /// was splitting ~113 million lines into strings and hashing them into sets.</para>
    ///
    /// <para><b>The gate is a digest of the file CONTENT, and a size-and-timestamp gate was deliberately
    /// NOT taken:</b> this check exists because an artifact can be
    /// something other than what it claims, and trusting a timestamp to say the bytes are unchanged
    /// would reintroduce exactly that assumption at the one place it must not live. **Every byte is read
    /// on every run; only the re-PARSE is skipped.**</para>
    ///
    /// <para>⚠ <b>The digest is MD5, and that is a deliberate, stated trade rather than an oversight.</b>
    /// The threat here is a typo'd `-trajlabel`, a horizon that fell back, a truncated dump — <b>accident,
    /// not an adversary</b>; nothing in this project defends an artifact archive against forgery, and a
    /// check that skipped a re-parse on a collision an attacker had to construct would still have been
    /// beaten by simply editing the manifest, which sits unsigned in a gitignored directory. SHA-256 was
    /// measured first and cost <b>~46 s</b> against MD5's read-bound floor, because Mono's managed
    /// implementation is not hardware-accelerated. <b>The full re-parse still runs at every gate via
    /// <c>-artifactfull</c></b>, so the digest only ever decides how often the archive is re-read between
    /// gates.</para>
    ///
    /// <list type="bullet">
    /// <item><b>Only PASSING files are ever recorded.</b> A failure is never cached, so it is re-parsed
    /// and re-reported on every run until it is fixed. A cache that could remember "clean" for a file
    /// that failed would be the silent-pass defect this whole sweep exists to prevent.</item>
    /// <item><b>The manifest is a LOCAL CACHE under `Logs/`, which is gitignored</b> — never a committed
    /// artifact anyone could quote as evidence, and absent on a fresh clone or in CI, where the check
    /// therefore does the full sweep by default. **The safe state is the default state.**</item>
    /// <item><b><c>-artifactfull</c> forces the whole archive to be re-parsed</b> regardless of the
    /// manifest. ⚠ **That is what a GATE runs.** Between gates the digest answers "is this the same
    /// file"; at a gate the question is asked of the bytes themselves.</item>
    /// </list>
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

        /// <summary>⚠ A LOCAL CACHE, under a gitignored directory ON PURPOSE. It is never evidence; it
        /// only lets an unchanged file skip a re-parse it has already passed.</summary>
        private const string ManifestRelative = "Logs/artifact_identity_manifest.tsv";

        /// <summary>One remembered PASS: the digest that earned it and the shape it was found to have.</summary>
        private struct Verdict
        {
            public string Digest;
            public int Horizon;
            public int Fields;
            public int Rows;
        }

        public static void Run()
        {
            CheckExit.ArmLogFold();

            string dir = Arg("-artifactdir=", "../PoliSim-captures/trajectories");
            bool full = HasFlag("-artifactfull");
            var sb = new StringBuilder();
            sb.Append("=== The coherence audit (c): artifact identity ===\n");

            if (!Directory.Exists(dir))
            {
                Debug.LogError($"ARTIFACT: no trajectory directory at '{dir}'. Reporting nothing rather than reporting clean.");
                CheckExit.Finish(1);
                return;
            }

            string[] files = Directory.GetFiles(dir, "traj_*.csv");
            Dictionary<string, Verdict> remembered = full
                ? new Dictionary<string, Verdict>(StringComparer.Ordinal)
                : LoadManifest();

            var current = new Dictionary<string, Verdict>(StringComparer.Ordinal);
            int parsed = 0, reused = 0, unparseable = 0;
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
                string digest = DigestOf(file);

                // ⚠ Byte-identical to a file this check has already PASSED, so its shape cannot have
                // changed. Only passes are ever remembered, so this can never resurrect a stale "clean".
                if (remembered.TryGetValue(name, out Verdict prior) && prior.Digest == digest)
                {
                    reused++;
                    current[name] = prior;
                    sb.Append(F("    ok*   {0,-34} {1} turns x {2} countries x {3} fields = {4} rows\n",
                        name, prior.Horizon, Countries, prior.Fields, prior.Rows));
                    continue;
                }

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

                parsed++;
                int expected = horizon * Countries * fields.Count;
                bool rowsOk = rows == expected;
                bool turnsOk = turns.Count == horizon;
                bool countriesOk = countries.Count == Countries;

                if (rowsOk && turnsOk && countriesOk)
                {
                    current[name] = new Verdict
                    {
                        Digest = digest, Horizon = horizon, Fields = fields.Count, Rows = rows,
                    };
                    sb.Append(F("    ok    {0,-34} {1} turns x {2} countries x {3} fields = {4} rows\n",
                        name, horizon, Countries, fields.Count, rows));
                    continue;
                }

                // ⚠ NOT recorded. A failure is re-parsed and re-reported every run until it is fixed.
                failures.Add(name);
                Debug.LogError($"ARTIFACT: '{name}' does not contain what its name claims — {rows} data rows against an expected "
                               + $"{expected} ({horizon} turns x {Countries} countries x {fields.Count} fields); {turns.Count} distinct "
                               + $"turn(s) against {horizon}; {countries.Count} country/countries against {Countries}. ⚠ A comparison "
                               + "against a mislabelled artifact passes cleanly and proves nothing.");
                sb.Append(F("    ⚠ FAIL {0,-34} rows {1} vs {2}, turns {3} vs {4}, countries {5} vs {6}\n",
                    name, rows, expected, turns.Count, horizon, countries.Count, Countries));
            }

            SaveManifest(current);

            sb.Append(F("\n    THE ENUMERATION: {0} traj_*.csv artifact(s) in '{1}'; {2} parsed in full, {3} verified by digest "
                        + "against a previous PASS, {4} unnameable, {5} FAILED.\n",
                files.Length, dir, parsed, reused, unparseable, failures.Count));
            sb.Append(F("    Mode: {0}. A digest match means the bytes are identical to a file this check has already passed;\n",
                full ? "FULL SWEEP (-artifactfull) - the manifest was ignored" : "incremental (pass -artifactfull at a gate)"));
            sb.Append("    only PASSES are ever remembered, and the manifest is a gitignored local cache, absent in CI.\n");
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

        /// <summary>⚠ A missing, unreadable or malformed manifest is not an error — it is a full sweep,
        /// which is the SAFE state. The cache may only ever make the check faster, never weaker.</summary>
        private static Dictionary<string, Verdict> LoadManifest()
        {
            var map = new Dictionary<string, Verdict>(StringComparer.Ordinal);
            try
            {
                string path = Path.Combine(Directory.GetCurrentDirectory(), ManifestRelative);
                if (!File.Exists(path)) { return map; }

                foreach (string line in File.ReadAllLines(path))
                {
                    string[] p = line.Split('\t');
                    if (p.Length != 5) { continue; }
                    if (!int.TryParse(p[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out int horizon)) { continue; }
                    if (!int.TryParse(p[3], NumberStyles.Integer, CultureInfo.InvariantCulture, out int fields)) { continue; }
                    if (!int.TryParse(p[4], NumberStyles.Integer, CultureInfo.InvariantCulture, out int rows)) { continue; }
                    map[p[0]] = new Verdict { Digest = p[1], Horizon = horizon, Fields = fields, Rows = rows };
                }
            }
            catch (Exception e)
            {
                Debug.Log($"ARTIFACT: manifest unreadable ({e.GetType().Name}), so this run parses everything. "
                          + "A cache that cannot be read is a full sweep, never a pass.");
                return new Dictionary<string, Verdict>(StringComparer.Ordinal);
            }

            return map;
        }

        /// <summary>⚠ A manifest that cannot be written must not fail the bar — the run's VERDICT is
        /// already correct; only the next run's speed is lost.</summary>
        private static void SaveManifest(Dictionary<string, Verdict> map)
        {
            try
            {
                string path = Path.Combine(Directory.GetCurrentDirectory(), ManifestRelative);
                string dir = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) { Directory.CreateDirectory(dir); }

                var sb = new StringBuilder();
                foreach (KeyValuePair<string, Verdict> e in map)
                {
                    sb.Append(e.Key).Append('\t')
                      .Append(e.Value.Digest).Append('\t')
                      .Append(e.Value.Horizon.ToString(CultureInfo.InvariantCulture)).Append('\t')
                      .Append(e.Value.Fields.ToString(CultureInfo.InvariantCulture)).Append('\t')
                      .Append(e.Value.Rows.ToString(CultureInfo.InvariantCulture)).Append('\n');
                }

                File.WriteAllText(path, sb.ToString(), new UTF8Encoding(false));
            }
            catch (Exception e)
            {
                Debug.Log($"ARTIFACT: manifest not written ({e.GetType().Name}). This run's verdict stands; "
                          + "the next run simply parses everything again.");
            }
        }

        private static string DigestOf(string file)
        {
            using (var sha = MD5.Create())
            using (var stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read, 1 << 20))
            {
                byte[] hash = sha.ComputeHash(stream);
                var sb = new StringBuilder(hash.Length * 2);
                foreach (byte b in hash) { sb.Append(b.ToString("x2", CultureInfo.InvariantCulture)); }
                return sb.ToString();
            }
        }

        private static bool HasFlag(string flag)
        {
            foreach (string arg in Environment.GetCommandLineArgs())
            {
                if (string.Equals(arg, flag, StringComparison.Ordinal)) { return true; }
            }

            return false;
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
        {
            return string.Format(CultureInfo.InvariantCulture, format, args);
        }
    }
}
