using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Debug = UnityEngine.Debug;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// **A money path gets its adversarial review, and the bar fails where it did not** (ruled by Elias 2026-09-21: *"a commit whose tier
    /// owed an adversarial review and got none fails the bar - §539 shipped a static-table-across-worlds defect because the rule was written
    /// and not enforced"*).
    ///
    /// <para><b>What happened.</b> §524's rule says a commit that touches a money path is reviewed adversarially, and `Tools/bar_tier.ps1`
    /// prints `review : REQUIRED` when it is owed. `c544620` (§539, build and retire) touched `EnergyMarket.cs`, the tool said REQUIRED, no
    /// review ran, and nothing noticed: the landed orders sat in one static table shared by every world the game holds, and the shadow
    /// baseline zeroed the player's fleet after every turn. §544's review found it three days later, in a commit already pushed. A rule that
    /// only prints is a rule nobody has to keep.</para>
    ///
    /// <para><b>WHAT THIS ENUMERATES</b> (rule 14). Three passes over `Tools/review_ledger.tsv`, the ledger of reviews that ran:
    /// (1) THE STATE - every money-path source file as it stands in the working tree (the tier tool's own definition, read out of
    /// `bar_tier.ps1` itself: a `.cs` under the money roots whose name matches the money pattern) hashed with its carriage returns
    /// stripped, must have a row for THAT state - `reviewed`, with a record and an evidence file that exists, is a report's length and
    /// names the file; or `grandfathered`, the state the guard found when it landed, which is a count under a ratchet and only falls.
    /// So the tree being committed cannot carry an unreviewed state of a money path past its bar.
    /// (2) THE HISTORY - every commit after <see cref="GuardedSince"/> that changed a money-path file must have a `reviewed` row for the
    /// blob it left behind, so a state committed without its bar and overwritten later is still found. `-reviewsince=&lt;commit&gt;` points
    /// the pass further back (an ANCESTOR of the constant only - it can widen the audit, never narrow it); pointed before `c544620` it
    /// fails on that commit by name, which is the proof it would have held the day.
    /// (3) THE BASELINE - both of `TrajectorySentinelCheck`'s digests must have a `reviewed` row: a family moves every figure the game
    /// books, whatever files it touched (the tier tool's second clause, *"REQUIRED if the sentinel moves"*).</para>
    ///
    /// <para><b>What it does not see, stated.</b> It cannot tell a real review from a written one: it turns a silent omission into a false
    /// statement in the record, with a report committed beside it, and that is all a check can do. The money pattern is names, so a file
    /// that books money under another name is outside it (`SimulationManager.cs` WAS, and was named into the pattern by ruling on 2026-09-21, §549: *the fiscal path cannot sit
    /// outside a money-path audit because of its name*; `MacroSystem.cs` was too, named in by ruling on 2026-09-21 and built at §575; the baseline pass reaches whatever still books under another name). Data files and
    /// editor diagnostics the tier tool also calls money paths are left to the tool's printed line and the baseline pass. Deleted files
    /// need no row.</para>
    /// </summary>
    public static class ReviewLedgerCheck
    {
        /// <summary>CONVENTION: where the ledger lives - beside the tier tool that says when a review is owed.</summary>
        public const string LedgerPath = "Tools/review_ledger.tsv";

        /// <summary>CONVENTION: the tier tool, whose `$money` and `$moneyRoots` lines ARE the definition (one source: the tool reads this suite's tables, this check reads the tool's).</summary>
        public const string TierToolPath = "Tools/bar_tier.ps1";

        /// <summary>CONVENTION: the sentinel's source, whose digest table IS the declared baseline.</summary>
        public const string SentinelPath = "Assets/Editor/TrajectorySentinelCheck.cs";

        /// <summary>CONVENTION: the history pass audits every commit AFTER this one - `3d92516`, the last commit pushed before the guard was ruled, so P-A (§545) is the first commit it holds.
        /// Never moved forward: a later anchor would un-audit the commits between.</summary>
        public const string GuardedSince = "3d92516";

        /// <summary>CONVENTION: an evidence file shorter than this is not a reviewer's report, bytes - the two reports of §544 run past ten thousand.</summary>
        public const int MinEvidenceBytes = 2000;

        /// <summary>
        /// The money-path states the guard FOUND when it landed (2026-09-21, §546) and did not review - the files' states as of that day, each a `grandfathered` row. LOWER IT AS THEY ARE
        /// EDITED AND REVIEWED, NEVER RAISE IT: a grandfathered row whose file has moved on is stale and fails the check until it is removed, so the count only falls.
        /// </summary>
        public const int GrandfatheredCeiling = 27;

        private struct Row { public string Kind, Key, Sha, Status, Date, Record, Evidence, Note; public int Line; }

        public static void Run()
        {
            var sb = new StringBuilder();
            sb.Append("=== REVIEW LEDGER: a money path's state, a commit's blob and a baseline's digest each need the review that ran ===\n");
            bool ok = true;

            // ---- the definition, read off the tier tool
            string tool = File.Exists(TierToolPath) ? File.ReadAllText(TierToolPath) : null;
            Match money = tool != null ? Regex.Match(tool, @"^\s*\$money\s*=\s*'([^']+)'", RegexOptions.Multiline) : Match.Empty;
            Match roots = tool != null ? Regex.Match(tool, @"^\s*\$moneyRoots\s*=\s*'([^']+)'", RegexOptions.Multiline) : Match.Empty;
            if (!money.Success || !roots.Success)
            {
                Debug.LogError("REVIEW LEDGER: could not read $money and $moneyRoots out of " + TierToolPath + " - VERIFIED NOTHING. The tool's two lines are this check's definition of a money path.");
                CheckExit.Finish(1); return;
            }
            var moneyName = new Regex(money.Groups[1].Value, RegexOptions.IgnoreCase);   // PowerShell's -match is case-insensitive; so is this
            var moneyRoot = new Regex(roots.Groups[1].Value, RegexOptions.IgnoreCase);

            // ---- the ledger
            List<Row> rows = ReadLedger(sb, ref ok);
            if (rows == null) { Debug.LogError(sb.ToString() + "REVIEW LEDGER: " + LedgerPath + " is missing or unreadable - VERIFIED NOTHING."); CheckExit.Finish(1); return; }
            var reviewed = new HashSet<string>(StringComparer.Ordinal);   // kind|key|sha
            var grandfathered = new Dictionary<string, Row>(StringComparer.Ordinal);
            foreach (Row r in rows)
            {
                string id = r.Kind + "|" + r.Key + "|" + r.Sha;
                if (reviewed.Contains(id) || grandfathered.ContainsKey(id)) { ok = false; sb.Append($"    ⚠ line {r.Line}: a second row for {r.Kind} {r.Key} at {Short(r.Sha)} - one state, one row.\n"); continue; }
                if (r.Status == "reviewed") { if (EvidenceHolds(r, sb)) { reviewed.Add(id); } else { ok = false; } }
                else if (r.Status == "grandfathered")
                {
                    if (r.Kind != "file") { ok = false; sb.Append($"    ⚠ line {r.Line}: only a file's state can be grandfathered - a baseline is reviewed or it is not the baseline.\n"); continue; }
                    grandfathered[id] = r;
                }
                else { ok = false; sb.Append($"    ⚠ line {r.Line}: status '{r.Status}' - a row is `reviewed` or `grandfathered`, nothing else (no waiver exists).\n"); }
            }

            // ---- (1) the state
            var files = new List<string>();
            foreach (string path in Directory.GetFiles("Assets/Scripts", "*.cs", SearchOption.AllDirectories))
            {
                string p = path.Replace('\\', '/');
                if (IsMoneyPath(p, moneyRoot, moneyName)) { files.Add(p); }
            }
            files.Sort(string.CompareOrdinal);
            if (files.Count == 0) { ok = false; sb.Append("    ⚠ NO MONEY-PATH FILE FOUND - the pattern matched nothing, so the state pass verified nothing.\n"); }
            int held = 0, old = 0; var liveGrandfathered = new HashSet<string>(StringComparer.Ordinal);
            foreach (string p in files)
            {
                string sha = Sha256NoCr(File.ReadAllBytes(p));
                string id = "file|" + p + "|" + sha;
                if (reviewed.Contains(id)) { held++; continue; }
                if (grandfathered.ContainsKey(id)) { old++; liveGrandfathered.Add(id); continue; }
                ok = false;
                sb.Append($"    ⚠ UNREVIEWED {p} at {Short(sha)} - a money path stands in a state no review covers. Run the adversarial review on the change (§524's rule), commit its report under Reviews/, and add the row: `Tools/review_row.ps1 -Path {p} -Record <§> -Evidence <Reviews/…>`.\n");
            }
            foreach (KeyValuePair<string, Row> g in grandfathered)
            {
                if (!liveGrandfathered.Contains(g.Key)) { ok = false; sb.Append($"    ⚠ line {g.Value.Line}: STALE grandfathered row for {g.Value.Key} - the file has moved on from the state the guard found; remove the row and lower GrandfatheredCeiling (the new state needs its own `reviewed` row).\n"); }
            }
            sb.Append($"    (1) the state: {files.Count} money-path file(s) - {held} in a reviewed state, {old} as the guard found them (grandfathered, ceiling {GrandfatheredCeiling}).\n");
            RatchetLedger.Report("ReviewLedgerCheck.GRANDFATHERED", grandfathered.Count, GrandfatheredCeiling);
            if (grandfathered.Count > GrandfatheredCeiling) { ok = false; sb.Append($"    ⚠ {grandfathered.Count} grandfathered rows against a ceiling of {GrandfatheredCeiling} - the count only falls.\n"); }

            // ---- (2) the history
            string since = GuardedSince;
            foreach (string arg in Environment.GetCommandLineArgs())
            {
                if (!arg.StartsWith("-reviewsince=", StringComparison.Ordinal)) { continue; }
                string asked = arg.Substring("-reviewsince=".Length);
                if (TryGit("merge-base --is-ancestor " + asked + " " + GuardedSince, out _)) { since = asked; sb.Append($"    -reviewsince={asked}: the history pass widened (an ancestor of {GuardedSince}).\n"); }
                else { ok = false; sb.Append($"    ⚠ -reviewsince={asked} is not an ancestor of {GuardedSince} - the argument may widen the audit, never narrow it. Ignored.\n"); }
            }
            if (!TryGit("log --reverse --format=@%H%x09%s --name-only " + since + "..HEAD -- Assets/Scripts", out string log))
            {
                ok = false; sb.Append("    ⚠ (2) the history: git could not list the commits since " + since + " - VERIFIED NOTHING there.\n");
            }
            else
            {
                int commits = 0, blobs = 0; string commit = null, subject = null; bool counted = false;
                foreach (string raw in log.Split('\n'))
                {
                    string line = raw.TrimEnd('\r');
                    if (line.Length == 0) { continue; }
                    if (line[0] == '@') { int tab = line.IndexOf('\t'); commit = tab > 0 ? line.Substring(1, tab - 1) : line.Substring(1); subject = tab > 0 ? line.Substring(tab + 1) : ""; counted = false; continue; }
                    string p = line.Replace('\\', '/');
                    if (commit == null || !IsMoneyPath(p, moneyRoot, moneyName)) { continue; }
                    if (!TryGitBytes("cat-file blob " + commit + ":" + p, out byte[] blob)) { continue; }   // the commit deleted it: nothing left to review
                    if (!counted) { commits++; counted = true; }
                    blobs++;
                    string sha = Sha256NoCr(blob);
                    if (reviewed.Contains("file|" + p + "|" + sha)) { continue; }
                    ok = false;
                    sb.Append($"    ⚠ NO REVIEW ON RECORD: commit {commit.Substring(0, 7)} left {p} at {Short(sha)} and the ledger holds no `reviewed` row for it - \"{Trim(subject, 90)}\". Its tier owed an adversarial review.\n");
                }
                sb.Append($"    (2) the history: {commits} commit(s) after {since.Substring(0, Math.Min(7, since.Length))} changed a money path, {blobs} blob(s) looked up.\n");
            }

            // ---- (3) the baseline: the sentinel's own table, read off its source (the sentinel is a simulation-group check; this one runs in the cheap bar and reads it as text)
            int digests = 0;
            // Read WITHOUT COMMENTS: a digest left in a comment (the old baseline's, kept as history) is not the declared baseline, and would otherwise ask for a row it has no right to.
            // The STATE pass above is the opposite by design and reads bytes: a file's state is all of it, comments included - a review reads those too.
            string sentinel = File.Exists(SentinelPath) ? SourceText.ReadWithoutComments(SentinelPath) : "";
            Match label = Regex.Match(sentinel, @"public const string BaselineLabel = ""([A-Za-z0-9_]+)"";");
            foreach (Match m in Regex.Matches(sentinel, @"\(\s*(\d+)\s*,\s*""([0-9a-f]{64})""\s*\)"))
            {
                digests++;
                string seed = m.Groups[1].Value, sha = m.Groups[2].Value;
                if (reviewed.Contains("baseline|" + seed + "|" + sha)) { continue; }
                ok = false;
                sb.Append($"    ⚠ UNREVIEWED BASELINE: seed {seed}'s digest {Short(sha)} ('{label.Groups[1].Value}') has no `reviewed` row - a family moves every figure the game books; its review is owed whatever files it touched.\n");
            }
            if (digests == 0) { ok = false; sb.Append("    ⚠ no digest read out of " + SentinelPath + " - the baseline pass verified nothing.\n"); }
            sb.Append($"    (3) the baseline: '{label.Groups[1].Value}', {digests} digest(s) looked up.\n");

            if (ok) { Debug.Log(sb.ToString() + "REVIEW LEDGER: every money-path state, every commit since the guard and the baseline carry the review that ran."); CheckExit.Finish(0); }
            else { Debug.LogError(sb.ToString() + "REVIEW LEDGER: FAILED - a review that was owed is not on record. The lines above name the file, the commit or the digest."); CheckExit.Finish(1); }
        }

        private static bool IsMoneyPath(string path, Regex moneyRoot, Regex moneyName)
        {
            if (!path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase) || !moneyRoot.IsMatch(path)) { return false; }
            return moneyName.IsMatch(Path.GetFileNameWithoutExtension(path));
        }

        /// <summary>A `reviewed` row stands on a record and a report: the evidence file exists, is a report's length, and names what it reviewed (the file's name, or the digest's first eight characters).</summary>
        private static bool EvidenceHolds(Row r, StringBuilder sb)
        {
            if (string.IsNullOrWhiteSpace(r.Record) || !DateTime.TryParseExact(r.Date, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
            { sb.Append($"    ⚠ line {r.Line}: a reviewed row needs its date (yyyy-MM-dd) and its record (the § that tells it).\n"); return false; }
            if (string.IsNullOrWhiteSpace(r.Evidence) || !File.Exists(r.Evidence)) { sb.Append($"    ⚠ line {r.Line}: the evidence file '{r.Evidence}' does not exist - the report is committed beside the row.\n"); return false; }
            byte[] bytes = File.ReadAllBytes(r.Evidence);
            if (bytes.Length < MinEvidenceBytes) { sb.Append($"    ⚠ line {r.Line}: '{r.Evidence}' is {bytes.Length} bytes - not a reviewer's report (at least {MinEvidenceBytes}).\n"); return false; }
            string text = Encoding.UTF8.GetString(bytes);
            string names = r.Kind == "file" ? Path.GetFileName(r.Key) : Short(r.Sha);
            if (text.IndexOf(names, StringComparison.Ordinal) < 0) { sb.Append($"    ⚠ line {r.Line}: '{r.Evidence}' never names {names} - a report names what it read.\n"); return false; }
            return true;
        }

        private static List<Row> ReadLedger(StringBuilder sb, ref bool ok)
        {
            if (!File.Exists(LedgerPath)) { return null; }
            var rows = new List<Row>(); int n = 0;
            foreach (string raw in File.ReadAllLines(LedgerPath))
            {
                n++;
                string line = raw.TrimEnd('\r');
                if (line.Length == 0 || line[0] == '#') { continue; }
                string[] c = line.Split('\t');
                if (c.Length < 8) { ok = false; sb.Append($"    ⚠ line {n}: {c.Length} column(s), eight are needed (kind, key, sha256, status, date, record, evidence, note).\n"); continue; }
                if ((c[0] != "file" && c[0] != "baseline") || c[2].Length != 64) { ok = false; sb.Append($"    ⚠ line {n}: kind is `file` or `baseline` and the third column a SHA-256.\n"); continue; }
                rows.Add(new Row { Kind = c[0], Key = c[1].Replace('\\', '/'), Sha = c[2].ToLowerInvariant(), Status = c[3], Date = c[4], Record = c[5], Evidence = c[6], Note = c[7], Line = n });
            }
            return rows;
        }

        /// <summary>The state's name: SHA-256 of the bytes with every carriage return removed, so the working tree (CRLF) and the repository's blob (LF) are one state.</summary>
        public static string Sha256NoCr(byte[] bytes)
        {
            var lf = new byte[bytes.Length]; int n = 0;
            foreach (byte b in bytes) { if (b != 0x0D) { lf[n++] = b; } }
            using (SHA256 sha = SHA256.Create())
            {
                byte[] hash = sha.ComputeHash(lf, 0, n);
                var sb = new StringBuilder(64);
                foreach (byte b in hash) { sb.Append(b.ToString("x2", CultureInfo.InvariantCulture)); }
                return sb.ToString();
            }
        }

        private static string Short(string sha) => sha != null && sha.Length >= 8 ? sha.Substring(0, 8) : sha;
        private static string Trim(string s, int max) => s == null ? "" : s.Length <= max ? s : s.Substring(0, max) + "…";

        private static bool TryGit(string arguments, out string output)
        {
            output = null;
            if (!TryGitBytes(arguments, out byte[] bytes)) { return false; }
            output = Encoding.UTF8.GetString(bytes);
            return true;
        }

        /// <summary>git's standard output as bytes (a blob is hashed as it is, never decoded). False when git is absent, fails, or the object does not exist - reported by the caller, never passed quietly.</summary>
        private static bool TryGitBytes(string arguments, out byte[] output)
        {
            output = null;
            try
            {
                var info = new ProcessStartInfo("git", arguments)
                {
                    WorkingDirectory = Directory.GetCurrentDirectory(),
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                };
                using (Process process = Process.Start(info))
                {
                    if (process == null) { return false; }
                    process.ErrorDataReceived += (s, e) => { };
                    process.BeginErrorReadLine();   // drained, so a long stderr cannot block the pipe
                    using (var ms = new MemoryStream())
                    {
                        process.StandardOutput.BaseStream.CopyTo(ms);
                        process.WaitForExit(30000);
                        output = ms.ToArray();
                    }
                    return process.ExitCode == 0;
                }
            }
            catch (Exception) { return false; }
        }
    }
}
