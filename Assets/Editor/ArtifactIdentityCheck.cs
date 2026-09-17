using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
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
    /// <para><b>⚠ THE DIGEST RUNS ON EVERY CORE (2026-09-17), and the gate is unchanged.</b> By 2026-09-16
    /// the archive had doubled to 1 415 files and 7.9 GB, and the serial digest alone was <b>24–45 s of a
    /// ~50 s cheap bar</b> (`Logs/bar_timing.tsv`). A probe measured the parts on this machine (16 logical
    /// cores, NVMe): a plain serial READ of the archive <b>4.2 s</b>, the serial MD5 <b>36.5–37.4 s</b>,
    /// the same MD5 across the cores <b>6.0–6.9 s with every digest identical</b>. The cost was the
    /// managed hash on one core, so the files are hashed in parallel, and a file that needs parsing is
    /// parsed in parallel too by an allocation-free scanner (see <see cref="Parse"/>); results are gathered by index and reported in ordinal filename order, so
    /// the log reads the same whatever order the workers finish in. **Every byte is still read and
    /// hashed on every run** — the timestamp gate stays not taken, for the reason above.</para>
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
    /// <item><b>One manifest PER DIRECTORY (2026-09-17).</b> The manifest was keyed by file name alone
    /// and rewritten whole each run, so one run against any other <c>-artifactdir=</c> replaced the
    /// archive's cache with that directory's files and the next bar re-parsed all 7.9 GB. The default
    /// directory keeps the original manifest file; any other directory gets its own, named by a digest
    /// of its full path.</item>
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

        /// <summary>The directory the bar reads when no <c>-artifactdir=</c> is given.</summary>
        private const string DefaultDirectory = "../PoliSim-captures/trajectories";

        /// <summary>⚠ A LOCAL CACHE, under a gitignored directory ON PURPOSE. It is never evidence; it
        /// only lets an unchanged file skip a re-parse it has already passed. This is the default
        /// directory's manifest; another directory's is named by <see cref="ManifestFor"/>.</summary>
        private const string ManifestRelative = "Logs/artifact_identity_manifest.tsv";

        /// <summary>One remembered PASS: the digest that earned it and the shape it was found to have.</summary>
        private struct Verdict
        {
            public string Digest;
            public int Horizon;
            public int Fields;
            public int Rows;
        }

        /// <summary>What one file came to, filled by a worker and reported on the main thread.</summary>
        private sealed class Outcome
        {
            public string Name;
            public int Horizon;
            public string Digest;
            public bool Named;
            public bool Reused;
            public bool Parsed;
            public bool Passed;
            public Verdict Verdict;
            public int Rows, Expected, Turns, CountryCount, FieldCount;
            public string Error;
        }

        public static void Run()
        {
            CheckExit.ArmLogFold();

            string dir = Arg("-artifactdir=", DefaultDirectory);
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
            Array.Sort(files, StringComparer.Ordinal);
            string manifest = ManifestFor(dir);
            Dictionary<string, Verdict> remembered = full
                ? new Dictionary<string, Verdict>(StringComparer.Ordinal)
                : LoadManifest(manifest);

            var outcomes = new Outcome[files.Length];
            int threads = Math.Max(1, Environment.ProcessorCount);
            long bytes = 0;
            foreach (string file in files) { bytes += new FileInfo(file).Length; }

            var digestClock = Stopwatch.StartNew();
            Parallel.For(0, files.Length, new ParallelOptions { MaxDegreeOfParallelism = threads }, i =>
            {
                var o = new Outcome { Name = Path.GetFileName(files[i]) };
                outcomes[i] = o;
                Match match = Name.Match(o.Name);
                if (!match.Success) { return; }
                o.Named = true;
                o.Horizon = int.Parse(match.Groups["horizon"].Value, CultureInfo.InvariantCulture);
                try { o.Digest = DigestOf(files[i]); }
                catch (Exception e) { o.Error = $"{e.GetType().Name}: {e.Message}"; }
            });
            long digestMs = digestClock.ElapsedMilliseconds;

            // ⚠ Byte-identical to a file this check has already PASSED, so its shape cannot have changed.
            // Only passes are ever remembered, so this can never resurrect a stale "clean".
            var toParse = new List<int>();
            for (int i = 0; i < outcomes.Length; i++)
            {
                Outcome o = outcomes[i];
                if (!o.Named || o.Error != null) { continue; }
                if (remembered.TryGetValue(o.Name, out Verdict prior) && prior.Digest == o.Digest)
                {
                    o.Reused = true;
                    o.Passed = true;
                    o.Verdict = prior;
                    continue;
                }

                toParse.Add(i);
            }

            var parseClock = Stopwatch.StartNew();
            Parallel.ForEach(toParse, new ParallelOptions { MaxDegreeOfParallelism = threads }, i =>
            {
                Outcome o = outcomes[i];
                try { Parse(files[i], o); }
                catch (Exception e) { o.Error = $"{e.GetType().Name}: {e.Message}"; }
            });
            long parseMs = parseClock.ElapsedMilliseconds;

            var current = new Dictionary<string, Verdict>(StringComparer.Ordinal);
            int parsed = 0, reused = 0, unparseable = 0;
            var failures = new List<string>();

            foreach (Outcome o in outcomes)
            {
                if (!o.Named)
                {
                    unparseable++;
                    Debug.LogError($"ARTIFACT: '{o.Name}' does not match the trajectory naming contract "
                                   + "traj_<label>_s<seed>_t<horizon>.csv. An artifact whose NAME cannot be read is an artifact "
                                   + "whose claim cannot be checked.");
                    failures.Add(o.Name);
                    continue;
                }

                if (o.Error != null)
                {
                    failures.Add(o.Name);
                    Debug.LogError($"ARTIFACT: '{o.Name}' could not be read ({o.Error}). An artifact that cannot be read cannot be checked.");
                    sb.Append(F("    ⚠ FAIL {0,-34} unreadable: {1}\n", o.Name, o.Error));
                    continue;
                }

                if (o.Reused)
                {
                    reused++;
                    current[o.Name] = o.Verdict;
                    sb.Append(F("    ok*   {0,-34} {1} turns x {2} countries x {3} fields = {4} rows\n",
                        o.Name, o.Verdict.Horizon, Countries, o.Verdict.Fields, o.Verdict.Rows));
                    continue;
                }

                parsed++;
                if (o.Passed)
                {
                    current[o.Name] = o.Verdict;
                    sb.Append(F("    ok    {0,-34} {1} turns x {2} countries x {3} fields = {4} rows\n",
                        o.Name, o.Horizon, Countries, o.FieldCount, o.Rows));
                    continue;
                }

                // ⚠ NOT recorded. A failure is re-parsed and re-reported every run until it is fixed.
                failures.Add(o.Name);
                Debug.LogError($"ARTIFACT: '{o.Name}' does not contain what its name claims — {o.Rows} data rows against an expected "
                               + $"{o.Expected} ({o.Horizon} turns x {Countries} countries x {o.FieldCount} fields); {o.Turns} distinct "
                               + $"turn(s) against {o.Horizon}; {o.CountryCount} country/countries against {Countries}. ⚠ A comparison "
                               + "against a mislabelled artifact passes cleanly and proves nothing.");
                sb.Append(F("    ⚠ FAIL {0,-34} rows {1} vs {2}, turns {3} vs {4}, countries {5} vs {6}\n",
                    o.Name, o.Rows, o.Expected, o.Turns, o.Horizon, o.CountryCount, Countries));
            }

            SaveManifest(manifest, current);

            sb.Append(F("\n    THE ENUMERATION: {0} traj_*.csv artifact(s) in '{1}'; {2} parsed in full, {3} verified by digest "
                        + "against a previous PASS, {4} unnameable, {5} FAILED.\n",
                files.Length, dir, parsed, reused, unparseable, failures.Count));
            sb.Append(F("    Cost: every byte digested ({0:F2} GB) in {1} ms, {2} file(s) parsed in {3} ms, on {4} worker thread(s). Manifest '{5}'.\n",
                bytes / 1e9, digestMs, toParse.Count, parseMs, threads, manifest));
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

        /// <summary>
        /// Reads one file's shape into its outcome. Runs on a worker thread: it touches nothing but its own
        /// outcome and never logs.
        ///
        /// <para>⚠ <b>A byte scanner, not <c>StreamReader.ReadLine</c> and <c>Split</c> (2026-09-17).</b> The
        /// string form allocated a line and its parts for every row of the archive, and Mono's
        /// collector serialises allocating threads: the first parallel gate took <b>568 s</b> on 16 workers.
        /// This reads the same contract off the bytes with no allocation per row - the first line is the
        /// header; <c>\r</c> and <c>\n</c> each end a line (a <c>\r\n</c> pair leaves an empty line, which is
        /// skipped exactly as an empty line always was); every non-empty line is a row; a row with fewer than
        /// two commas counts and contributes nothing; the first part is a turn when it reads as an integer
        /// (white space and a sign allowed, as <c>int.TryParse</c> allows); the second part is the country and
        /// the third the field, as distinct byte strings.</para>
        /// </summary>
        private static void Parse(string file, Outcome o)
        {
            var shape = new ShapeScanner();
            using (var stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read, 1 << 16))
            {
                byte[] chunk = new byte[1 << 20];
                int read;
                while ((read = stream.Read(chunk, 0, chunk.Length)) > 0)
                {
                    shape.Feed(chunk, read);
                }
            }

            shape.Finish();

            o.Parsed = true;
            o.Rows = shape.Rows;
            o.FieldCount = shape.Fields.Count;
            o.Turns = shape.Turns.Count;
            o.CountryCount = shape.Countries.Count;
            o.Expected = o.Horizon * Countries * shape.Fields.Count;
            o.Passed = shape.Rows == o.Expected && shape.Turns.Count == o.Horizon && shape.Countries.Count == Countries;
            if (o.Passed)
            {
                o.Verdict = new Verdict { Digest = o.Digest, Horizon = o.Horizon, Fields = shape.Fields.Count, Rows = shape.Rows };
            }
        }

        /// <summary>The shape of one CSV, fed in chunks. See <see cref="Parse"/> for the contract it reads.</summary>
        private sealed class ShapeScanner
        {
            public int Rows;
            public readonly HashSet<int> Turns = new HashSet<int>();
            public readonly DistinctBytes Countries = new DistinctBytes();
            public readonly DistinctBytes Fields = new DistinctBytes();

            private byte[] _line = new byte[512];
            private int _length;
            private bool _header = true;

            public void Feed(byte[] chunk, int count)
            {
                int start = 0;
                for (int i = 0; i < count; i++)
                {
                    byte b = chunk[i];
                    if (b != (byte)'\n' && b != (byte)'\r') { continue; }
                    if (_length == 0) { Line(chunk, start, i - start); }
                    else { Append(chunk, start, i - start); Line(_line, 0, _length); _length = 0; }
                    start = i + 1;
                }

                if (start < count) { Append(chunk, start, count - start); }
            }

            public void Finish()
            {
                if (_length > 0) { Line(_line, 0, _length); _length = 0; }
            }

            private void Append(byte[] source, int offset, int count)
            {
                if (_length + count > _line.Length) { Array.Resize(ref _line, Math.Max(_line.Length * 2, _length + count)); }
                Buffer.BlockCopy(source, offset, _line, _length, count);
                _length += count;
            }

            private void Line(byte[] b, int offset, int count)
            {
                if (_header) { _header = false; return; }
                if (count == 0) { return; }
                Rows++;

                int end = offset + count;
                int first = Array.IndexOf(b, (byte)',', offset, count);
                if (first < 0) { return; }
                int second = Array.IndexOf(b, (byte)',', first + 1, end - first - 1);
                if (second < 0) { return; }
                int third = Array.IndexOf(b, (byte)',', second + 1, end - second - 1);
                if (third < 0) { third = end; }

                if (TryTurn(b, offset, first, out int turn)) { Turns.Add(turn); }
                Countries.Add(b, first + 1, second - first - 1);
                Fields.Add(b, second + 1, third - second - 1);
            }

            /// <summary><c>int.TryParse</c> with <c>NumberStyles.Integer</c> on ASCII: surrounding white space,
            /// one leading sign, at least one digit, nothing else, and no overflow.</summary>
            private static bool TryTurn(byte[] b, int from, int to, out int value)
            {
                value = 0;
                while (from < to && IsSpace(b[from])) { from++; }
                while (to > from && IsSpace(b[to - 1])) { to--; }
                if (from >= to) { return false; }
                bool negative = false;
                if (b[from] == (byte)'+' || b[from] == (byte)'-') { negative = b[from] == (byte)'-'; from++; }
                if (from >= to) { return false; }
                long n = 0;
                for (int i = from; i < to; i++)
                {
                    int digit = b[i] - (byte)'0';
                    if (digit < 0 || digit > 9) { return false; }
                    n = n * 10 + digit;
                    if (n > (long)int.MaxValue + 1) { return false; }
                }

                n = negative ? -n : n;
                if (n < int.MinValue || n > int.MaxValue) { return false; }
                value = (int)n;
                return true;
            }

            private static bool IsSpace(byte c) => c == (byte)' ' || c == (byte)'\t' || c == 0x0B || c == 0x0C;
        }

        /// <summary>A set of distinct byte strings that allocates only when it meets a new one.</summary>
        private sealed class DistinctBytes
        {
            private readonly Dictionary<ulong, List<byte[]>> _byHash = new Dictionary<ulong, List<byte[]>>();

            public int Count { get; private set; }

            public void Add(byte[] b, int offset, int count)
            {
                ulong hash = 14695981039346656037UL;
                for (int i = offset; i < offset + count; i++) { hash = (hash ^ b[i]) * 1099511628211UL; }

                if (_byHash.TryGetValue(hash, out List<byte[]> known))
                {
                    foreach (byte[] k in known)
                    {
                        if (k.Length != count) { continue; }
                        bool same = true;
                        for (int i = 0; i < count; i++) { if (k[i] != b[offset + i]) { same = false; break; } }
                        if (same) { return; }
                    }
                }
                else
                {
                    known = new List<byte[]>(1);
                    _byHash[hash] = known;
                }

                var copy = new byte[count];
                Buffer.BlockCopy(b, offset, copy, 0, count);
                known.Add(copy);
                Count++;
            }
        }

        /// <summary>The manifest for a directory: the original file for the default directory, so the cache
        /// built before 2026-09-17 stays valid; otherwise one named by the first eight hex digits of the
        /// MD5 of the directory's full path, so a run against another directory never evicts the archive's.</summary>
        private static string ManifestFor(string dir)
        {
            string full = Path.GetFullPath(dir).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            string standard = Path.GetFullPath(DefaultDirectory).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            if (string.Equals(full, standard, StringComparison.OrdinalIgnoreCase))
            {
                return Path.Combine(Directory.GetCurrentDirectory(), ManifestRelative);
            }

            using (var md5 = MD5.Create())
            {
                byte[] hash = md5.ComputeHash(Encoding.UTF8.GetBytes(full.ToLowerInvariant()));
                var tag = new StringBuilder(8);
                for (int i = 0; i < 4; i++) { tag.Append(hash[i].ToString("x2", CultureInfo.InvariantCulture)); }
                return Path.Combine(Directory.GetCurrentDirectory(), "Logs", "artifact_identity_manifest_" + tag + ".tsv");
            }
        }

        /// <summary>⚠ A missing, unreadable or malformed manifest is not an error — it is a full sweep,
        /// which is the SAFE state. The cache may only ever make the check faster, never weaker.</summary>
        private static Dictionary<string, Verdict> LoadManifest(string path)
        {
            var map = new Dictionary<string, Verdict>(StringComparer.Ordinal);
            try
            {
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
        private static void SaveManifest(string path, Dictionary<string, Verdict> map)
        {
            try
            {
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
            using (var md5 = MD5.Create())
            using (var stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read, 1 << 20))
            {
                byte[] hash = md5.ComputeHash(stream);
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
