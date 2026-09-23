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
    ///
    /// <para><b>A COMMENT-ONLY CHANGE INHERITS THE REVIEW OF THE STATE IT CHANGED</b> (ruled by Elias 2026-09-23, §584: §579 left two money
    /// paths citing moved documents because a comment edit owed a full review). A state with no row of its own is accepted when its code is the
    /// code of the state before it - walked back through git, one comment-only step at a time, to a state with a `reviewed` row (never a grandfathered
    /// one: a grandfathered file's first change of any kind owes its review). **"Its code" is the COMPILER'S reading (RL-1, ruled 2026-09-23, §588):**
    /// the text runner (`Tools/textcheck`) parses both versions with Microsoft.CodeAnalysis.CSharp, drops comment and whitespace trivia, and compares the
    /// rest token for token - a preprocessor directive and a disabled `#if` branch are kept as code, since a branch disabled here is live under another
    /// define. It replaced a regex stripper and a list of the shapes it mis-stripped, which two adversarial reads broke twice (§584,
    /// `Reviews/2026-09-23_s584_rt3_ledger.md`). No line carrying a provenance mark may change or move either, because a constant's claimed provenance
    /// lives in its comment. A verdict the runner cannot give (dotnet absent, the build failing) is never an acceptance.
    /// <see cref="ProbeCommentOnly"/> proves both directions on every run: comment and whitespace edits accepted, and a literal, a commented-out
    /// statement, a provenance relabel, an edited disabled branch and every shape the regex once mis-stripped owed.</para>
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
        public const int GrandfatheredCeiling = 26;

        /// <summary>CONVENTION: how many comment-only steps a state may be walked back through git to reach one with a row - a bound on the walk, not a policy.</summary>
        public const int MaxCommentOnlySteps = 40;

        /// <summary>CONVENTION (RL-1, §588): the text runner's project, which carries the Roslyn reference, and the assembly its build writes - OUTSIDE the
        /// repository, as `Tools/textcheck/Directory.Build.props` places every build output of that project.</summary>
        private const string RunnerProject = "Tools/textcheck/TextCheck.csproj";
        private const string RunnerDll = "../PoliSim-captures/textcheck/bin/Release/net8.0/textcheck.dll";

        private struct Row { public string Kind, Key, Sha, Status, Date, Record, Evidence, Note; public int Line; }

        public static void Run()
        {
            var sb = new StringBuilder();
            sb.Append("=== REVIEW LEDGER: a money path's state, a commit's blob and a baseline's digest each need the review that ran ===\n");
            bool ok = true;
            _callerInfo = null; _runnerBuilt = false; _verdictCache.Clear();   // RL-1: the runner's build, the caller-info scan and the verdict cache are per run

            // ---- the comment-only exemption, proved both ways before it is used (§584)
            if (!ProbeCommentOnly(sb)) { ok = false; }

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
            int held = 0, old = 0, inherited = 0; var liveGrandfathered = new HashSet<string>(StringComparer.Ordinal);
            foreach (string p in files)
            {
                byte[] bytes = File.ReadAllBytes(p);
                string sha = Sha256NoCr(bytes);
                string id = "file|" + p + "|" + sha;
                // The second read's P1-5, older than §584: the state's digest drops EVERY carriage return, so a lone CR - a line break to C#, ending a comment -
                // would let a file with live code where a comment was hash like its reviewed neighbour. No money path holds one; the bar keeps it that way.
                if (HasForeignLineBreak(Encoding.UTF8.GetString(bytes))) { ok = false; sb.Append($"    ⚠ {p} holds a lone CR or a Unicode line break - the digest cannot name its state; normalise the file's line breaks.\n"); continue; }
                if (reviewed.Contains(id)) { held++; continue; }
                if (grandfathered.ContainsKey(id)) { old++; liveGrandfathered.Add(id); continue; }
                string via = CoveredThroughComments(p, bytes, "HEAD", reviewed, out int steps);
                if (via != null)
                {
                    inherited++;
                    sb.Append($"    comment-only {p} at {Short(sha)} - its code is {Short(via.Substring(via.LastIndexOf('|') + 1))}'s, {steps} comment-only step(s) back, and that state's reviewed row stands.\n");
                    continue;
                }
                ok = false;
                sb.Append($"    ⚠ UNREVIEWED {p} at {Short(sha)} - a money path stands in a state no review covers. Run the adversarial review on the change (§524's rule), commit its report under Reviews/, and add the row: `Tools/review_row.ps1 -Path {p} -Record <§> -Evidence <Reviews/…>`.\n");
            }
            foreach (KeyValuePair<string, Row> g in grandfathered)
            {
                if (!liveGrandfathered.Contains(g.Key)) { ok = false; sb.Append($"    ⚠ line {g.Value.Line}: STALE grandfathered row for {g.Value.Key} - the file has moved on from the state the guard found; remove the row and lower GrandfatheredCeiling (the new state needs its own `reviewed` row).\n"); }
            }
            sb.Append($"    (1) the state: {files.Count} money-path file(s) - {held} in a reviewed state, {old} as the guard found them (grandfathered, ceiling {GrandfatheredCeiling}), {inherited} comment-only from a state with a row.\n");
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
                int commits = 0, blobs = 0, commentOnly = 0; string commit = null, subject = null; bool counted = false;
                foreach (string raw in log.Split('\n'))
                {
                    string line = raw.TrimEnd('\r');
                    if (line.Length == 0) { continue; }
                    if (line[0] == '@') { int tab = line.IndexOf('\t'); commit = tab > 0 ? line.Substring(1, tab - 1) : line.Substring(1); subject = tab > 0 ? line.Substring(tab + 1) : ""; counted = false; continue; }
                    string p = line.Replace('\\', '/');
                    if (commit == null || !IsMoneyPath(p, moneyRoot, moneyName)) { continue; }
                    if (!TryGitBytes("cat-file blob \"" + commit + ":" + p + "\"", out byte[] blob)) { continue; }   // the commit deleted it: nothing left to review
                    if (!counted) { commits++; counted = true; }
                    blobs++;
                    string sha = Sha256NoCr(blob);
                    if (HasForeignLineBreak(Encoding.UTF8.GetString(blob))) { ok = false; sb.Append($"    ⚠ commit {commit.Substring(0, 7)} left {p} with a lone CR or a Unicode line break - the digest drops every CR, so it cannot name that state apart from its neighbour; normalise the file's line breaks.\n"); continue; }
                    if (reviewed.Contains("file|" + p + "|" + sha)) { continue; }
                    string via = CoveredThroughComments(p, blob, commit + "^", reviewed, out int steps);
                    if (via != null) { commentOnly++; sb.Append($"    comment-only: commit {commit.Substring(0, 7)} left {p} at {Short(sha)} with {Short(via.Substring(via.LastIndexOf('|') + 1))}'s code, {steps} step(s) back - that state's row stands.\n"); continue; }
                    ok = false;
                    sb.Append($"    ⚠ NO REVIEW ON RECORD: commit {commit.Substring(0, 7)} left {p} at {Short(sha)} and the ledger holds no `reviewed` row for it - \"{Trim(subject, 90)}\". Its tier owed an adversarial review.\n");
                }
                sb.Append($"    (2) the history: {commits} commit(s) after {since.Substring(0, Math.Min(7, since.Length))} changed a money path, {blobs} blob(s) looked up, {commentOnly} accepted as comment-only.\n");
            }

            // ---- (3) the baseline: the sentinel's own table, read off its source (the sentinel is a simulation-group check; this one runs in the cheap bar and reads it as text)
            int digests = 0;
            // Read WITHOUT COMMENTS: a digest left in a comment (the old baseline's, kept as history) is not the declared baseline, and would otherwise ask for a row it has no right to.
            // The STATE pass above keys a state on its BYTES, comments included - a review reads those too - and a state with no row of its own is accepted
            // only when a comment-only walk reaches a REVIEWED state (§584): the comments may differ, the code may not, and a provenance line may not.
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

        /// <summary>§584: the ledger id of the state this one inherits its review from, walking back through git from <paramref name="rev"/> one distinct state
        /// at a time while each step is comment-only - null when a step changes code, the stripper cannot be trusted on either side, the history runs out,
        /// or the walk passes <see cref="MaxCommentOnlySteps"/>. A step that leaves the bytes unchanged is not a step (the same state, an earlier commit).
        /// ⚠ ONLY A `reviewed` ROW ANCHORS (the s584 review's P1-2): a grandfathered state as the anchor would leave a comment-only commit in the history
        /// hanging on a row the state pass later calls stale - keep it and the bar says STALE, remove it and the history says NO REVIEW. A grandfathered
        /// file's first change of any kind owes its review, which is what the ratchet is for.</summary>
        private static string CoveredThroughComments(string path, byte[] bytes, string rev, HashSet<string> reviewed, out int steps)
        {
            steps = 0;
            byte[] current = bytes;
            string cursor = rev;
            for (int walked = 0; walked < MaxCommentOnlySteps; walked++)
            {
                if (!TryGit("log -1 --format=%H " + cursor + " -- \"" + path + "\"", out string found)) { return null; }
                string commit = found.Trim();
                if (commit.Length == 0 || !TryGitBytes("cat-file blob \"" + commit + ":" + path + "\"", out byte[] previous)) { return null; }
                cursor = commit + "^";
                string previousSha = Sha256NoCr(previous);
                if (previousSha != Sha256NoCr(current))
                {
                    if (CommentOnlyRefusal(previous, current) != null) { return null; }
                    steps++;
                    current = previous;
                }
                string id = "file|" + path + "|" + previousSha;
                if (steps > 0 && reviewed.Contains(id)) { return id; }
            }
            return null;
        }

        /// <summary>§584, re-cut by RL-1 (§588): null when <paramref name="after"/> differs from <paramref name="before"/> in comments and whitespace only - the
        /// compiler's own parser decides it (<see cref="RoslynVerdicts"/>), on the files' own BYTES - and no provenance claim moved; else the reason it is not,
        /// or why it could not be decided. Refused outright while the project declares a caller-info parameter (<see cref="CallerInfoDeclared"/>).</summary>
        public static string CommentOnlyRefusal(byte[] before, byte[] after)
        {
            string callerInfo = CallerInfoDeclared();
            if (callerInfo != null) { return callerInfo; }
            Dictionary<string, string> verdicts = RoslynVerdicts(new List<(string, byte[], byte[])> { ("step", before, after) }, out string failure);
            if (verdicts == null) { return failure; }
            if (verdicts["step"] != null) { return verdicts["step"]; }
            // The s584 review's P1-3 and the RL-1 review's F1: a constant's provenance is claimed in its COMMENT (`ConstantProvenanceCheck` reads it there),
            // so a comment edit that rewrites, moves or re-attaches a mark - [AUTHORED-DRAFT] relabelled SOURCED - is a change a review must read.
            return ProvenanceBlocks(Encoding.UTF8.GetString(before)) == ProvenanceBlocks(Encoding.UTF8.GetString(after)) ? null : "a provenance claim changed, moved or re-attached - what a constant claims to be is reviewed";
        }

        /// <summary>Every line carrying one of `ConstantProvenanceCheck`'s marks, IN ORDER, each with EVERY line after it down to and including the first code
        /// line - blank and comment lines between kept, not skipped (the RL-1 review's F1: skipping them let a blank line removed between a mark and a
        /// declaration re-attach the mark to it, which the check reads as that declaration's provenance). The parser has already ruled the code identical.</summary>
        private static string ProvenanceBlocks(string text)
        {
            string[] raw = text.Replace("\r\n", "\n").Split('\n');
            var sb = new StringBuilder();
            for (int i = 0; i < raw.Length; i++)
            {
                bool marked = false;
                foreach (string mark in ConstantProvenanceCheck.ProvenanceMarks) { if (raw[i].IndexOf(mark, StringComparison.OrdinalIgnoreCase) >= 0) { marked = true; break; } }
                if (!marked) { continue; }
                sb.Append(raw[i].Trim());
                for (int j = i + 1; j < raw.Length; j++)
                {
                    string t = raw[j].Trim();
                    sb.Append(" | ").Append(t);
                    // A line opening with '/' then '*' is a block comment's; tested by characters, because the two-character string would open a
                    // "comment" to the source-reading checks' regex stripper and hide the code after it from them (found by DeadStateCheck, §588).
                    bool opensBlock = t.Length >= 2 && t[0] == '/' && t[1] == '*';
                    if (t.Length > 0 && !t.StartsWith("//", StringComparison.Ordinal) && !opensBlock && !t.StartsWith("*", StringComparison.Ordinal)) { break; }
                }
                sb.Append('\n');
            }
            return sb.ToString();
        }

        /// <summary>The RL-1 review's F4, the parser's known residue: a caller-info parameter compiles a call site's LINE or its argument's TEXT into the program,
        /// so a comment line added above such a call, or spacing inside its argument, changes the build. Nothing under Assets/ declares one; while anything does,
        /// no change is comment-only. Read with comments stripped, so a mention in a comment is not a declaration. Null when none; else the reason.</summary>
        private static string CallerInfoDeclared()
        {
            if (_callerInfo != null) { return _callerInfo.Length == 0 ? null : _callerInfo; }
            _callerInfo = "";
            foreach (string path in Directory.GetFiles("Assets", "*.cs", SearchOption.AllDirectories))
            {
                Match m = CallerInfoAttribute.Match(SourceText.ReadWithoutComments(path));
                if (m.Success) { _callerInfo = path.Replace('\\', '/') + " declares a " + m.Groups[1].Value + " parameter - a comment or spacing edit can change what compiles, so no change is comment-only while it does"; return _callerInfo; }
            }
            return null;
        }

        /// <summary>Per run (reset in <see cref="Run"/>): the caller-info scan's answer ("" = none), whether the runner was built, and the verdicts already given,
        /// keyed by the two texts' digests - so a walk that revisits a pair, or two passes that meet the same step, run the parser once (the RL-1 review's F5).</summary>
        private static string _callerInfo;

        /// <summary>A caller-info attribute AS AN ATTRIBUTE - after '[', ',' or a namespace's '.', with or without its Attribute suffix - so the names in this
        /// file's own string literals and messages do not read as declarations.</summary>
        private static readonly Regex CallerInfoAttribute = new Regex(@"[\[,.]\s*(CallerLineNumber|CallerArgumentExpression|CallerFilePath|CallerMemberName)(?:Attribute)?\b");
        private static bool _runnerBuilt;
        private static readonly Dictionary<string, string> _verdictCache = new Dictionary<string, string>(StringComparer.Ordinal);

        /// <summary>RL-1 (ruled 2026-09-23, §588): THE VERDICT IS THE COMPILER'S. The pairs' own BYTES are written to a directory the text runner reads
        /// (`Tools/textcheck`, `textcheck commentonly &lt;dir&gt;`), which decodes them as strict UTF-8, parses both with Microsoft.CodeAnalysis.CSharp, drops
        /// comment and whitespace trivia, and compares the rest token for token - directives and disabled `#if` text kept as code. The runner is built ONCE a run
        /// (`dotnet build`, incremental) and refused if its assembly is older than its sources; null with <paramref name="failure"/> set when any step fails, and
        /// the caller then treats the change as OWED - a verdict that could not be reached is never an acceptance. Values: null = comment-only, else the reason.</summary>
        private static Dictionary<string, string> RoslynVerdicts(List<(string Name, byte[] Before, byte[] After)> pairs, out string failure)
        {
            failure = null;
            var verdicts = new Dictionary<string, string>(StringComparer.Ordinal);
            var asked = new List<(string Name, byte[] Before, byte[] After, string Key)>();
            foreach (var p in pairs)
            {
                string key = Sha256NoCr(p.Before) + ":" + Sha256NoCr(p.After) + ":" + p.Before.Length + ":" + p.After.Length;
                if (_verdictCache.TryGetValue(key, out string cached)) { verdicts[p.Name] = cached; } else { asked.Add((p.Name, p.Before, p.After, key)); }
            }
            if (asked.Count == 0) { return verdicts; }

            string root = Directory.GetCurrentDirectory();
            string project = Path.Combine(root, RunnerProject);
            if (!File.Exists(project)) { failure = "the comment-only runner's project (" + RunnerProject + ") is not on disk - no verdict"; return null; }
            string dll = Path.GetFullPath(Path.Combine(root, RunnerDll));
            if (!_runnerBuilt)
            {
                if (!TryProcess("dotnet", "build \"" + project + "\" -c Release -nologo -v q", root, 300000, out string buildOut))
                { failure = "the comment-only runner did not build (dotnet build " + RunnerProject + ") - no verdict: " + Trim(buildOut, 200); return null; }
                // The RL-1 review's F7: an assembly older than the runner's own sources is a stale build from somewhere else - refused, never run.
                DateTime newestSource = DateTime.MinValue;
                foreach (string source in Directory.GetFiles(Path.GetDirectoryName(project), "*.cs")) { DateTime t = File.GetLastWriteTimeUtc(source); if (t > newestSource) { newestSource = t; } }
                if (!File.Exists(dll) || File.GetLastWriteTimeUtc(dll) < newestSource) { failure = "the comment-only runner's assembly is missing or older than its sources at " + RunnerDll + " - no verdict"; return null; }
                _runnerBuilt = true;
            }
            string dir = Path.Combine(Path.GetTempPath(), "polisim_commentonly_" + Guid.NewGuid().ToString("N"));
            try
            {
                Directory.CreateDirectory(dir);
                for (int i = 0; i < asked.Count; i++)
                {
                    File.WriteAllBytes(Path.Combine(dir, i.ToString("D3", CultureInfo.InvariantCulture) + ".before.txt"), asked[i].Before);
                    File.WriteAllBytes(Path.Combine(dir, i.ToString("D3", CultureInfo.InvariantCulture) + ".after.txt"), asked[i].After);
                }
                if (!TryProcess("dotnet", "\"" + dll + "\" commentonly \"" + dir + "\"", root, 120000, out string output))
                { failure = "the comment-only runner failed - no verdict: " + Trim(output, 200); return null; }
                var byIndex = new Dictionary<int, string>();
                foreach (string rawLine in output.Split('\n'))
                {
                    string[] c = rawLine.TrimEnd('\r').Split('\t');
                    if (c.Length < 3 || c[0] != "VERDICT" || !int.TryParse(c[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int index)) { continue; }
                    byIndex[index] = c[2] == "ACCEPT" ? null : (c.Length > 3 ? c[3] : "owed, no reason given");
                }
                for (int i = 0; i < asked.Count; i++)
                {
                    if (!byIndex.ContainsKey(i)) { failure = "the comment-only runner gave no verdict for pair " + i + " - no verdict"; return null; }
                    verdicts[asked[i].Name] = byIndex[i];
                    _verdictCache[asked[i].Key] = byIndex[i];
                }
                return verdicts;
            }
            finally { try { Directory.Delete(dir, true); } catch (Exception) { } }
        }

        /// <summary>A line break C# honours that the digest does not name: a lone CR (the state's digest drops EVERY carriage return), NEL, LS, PS. The
        /// state and history passes fail on one (the s584 review's P1-5).</summary>
        public static bool HasForeignLineBreak(string text)
        {
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (c == '\u0085' || c == '\u2028' || c == '\u2029') { return true; }
                if (c == '\r' && (i + 1 >= text.Length || text[i + 1] != '\n')) { return true; }
            }
            return false;
        }

        private static byte[] U(string s) => new UTF8Encoding(false).GetBytes(s);

        /// <summary>§584, kept by RL-1 (§588) as the proof it now stands on: the exemption proved both directions on every run - each case's right answer is
        /// known. The thirty-five cases §584 wrote (one re-answered: spacing inside a code line is whitespace trivia, which the parser drops by the ruling),
        /// three shapes only a parser can see, and the RL-1 review's: a provenance mark re-attached three ways, and two texts that differ only in bytes that
        /// are not UTF-8. ONE runner call answers them all.</summary>
        private static bool ProbeCommentOnly(StringBuilder sb)
        {
            var cases = new (string Name, byte[] Before, byte[] After, bool Accept)[]
            {
                ("a comment's words edited", U("int a = 1; // old words\n"), U("int a = 1; // new words\n"), true),
                ("a doc comment line added", U("class A {\n    int x;\n}\n"), U("class A {\n    /// <summary>x</summary>\n    int x;\n}\n"), true),
                ("a block comment edited inside a statement", U("int a = /* one */ 1;\n"), U("int a = /* two */ 1;\n"), true),
                ("a comment line removed", U("// history\nint a = 1;\n"), U("int a = 1;\n"), true),
                ("a section sign repaired in a comment (the s579 artefact)", U("// COMPLETED.md " + (char)92 + "x{a7}580\nint a;\n"), U("// COMPLETED.md §580\nint a;\n"), true),
                ("a numeric literal changed", U("float r = 0.5f;\n"), U("float r = 0.6f;\n"), false),
                ("a string literal changed", U("string s = \"a\";\n"), U("string s = \"b\";\n"), false),
                ("a statement commented out", U("Pay(x);\nint a;\n"), U("// Pay(x);\nint a;\n"), false),
                ("a commented statement restored", U("// Pay(x);\nint a;\n"), U("Pay(x);\nint a;\n"), false),
                ("spacing inside a code line (whitespace trivia: accepted since RL-1)", U("int a=1;\n"), U("int a = 1;\n"), true),
                ("code hidden between a block opener and closer in strings", U("string g = \"Assets/*.cs\"; int a = 1; string e = \"*/\";\n"), U("string g = \"Assets/*.cs\"; int a = 2; string e = \"*/\";\n"), false),
                ("code hidden after a block opener in a line comment", U("// Assets/*\nint a = 1;\n// end */\n"), U("// Assets/*\nint a = 2;\n// end */\n"), false),
                ("a verbatim string's line that looks like a comment", U("string s = @\"one\n// two\nend\";\n"), U("string s = @\"one\n// three\nend\";\n"), false),
                ("a block inside a string with an escaped quote", U("string s = \"a\\\"b /* one */ c\";\n"), U("string s = \"a\\\"b /* two */ c\";\n"), false),
                ("a block in a string after a quote character", U("char q = '\"'; string s = \"x /* one */ y\";\n"), U("char q = '\"'; string s = \"x /* two */ y\";\n"), false),
                ("a block in a string after an escaped quote character", U("char q = '\\\"'; string s = \"x /* one */ y\";\n"), U("char q = '\\\"'; string s = \"x /* two */ y\";\n"), false),
                ("a block in a string after an escaped-quote string", U("string s = \"\\\"\" + \"a /* one */ b\";\n"), U("string s = \"\\\"\" + \"a /* two */ b\";\n"), false),
                ("a block in an interpolated string after a quote character", U("string s = $\"{'\"'} /* one */\";\n"), U("string s = $\"{'\"'} /* two */\";\n"), false),
                ("an interpolated verbatim string's line that looks like a comment", U("var q = $@\"{Col(\"rate\")}\n// rate = 0.25\n\";\n"), U("var q = $@\"{Col(\"rate\")}\n// rate = 0.35\n\";\n"), false),
                ("the other interpolated verbatim order", U("string s = @$\"{'\"'}\n// one\n\";\n"), U("string s = @$\"{'\"'}\n// two\n\";\n"), false),
                ("a block across an interpolated verbatim string", U("string s = $@\"{F(\"a\")}\nx /* one */ y\n\";\n"), U("string s = $@\"{F(\"a\")}\nx /* two */ y\n\";\n"), false),
                ("a blank line inside an interpolated verbatim string", U("string s = $@\"{F(\"a\")}\nx\n\";\n"), U("string s = $@\"{F(\"a\")}\n\nx\n\";\n"), false),
                ("live code after a block opened in a skipped #if branch", U("#if false\n/* disabled\n#endif\npaid = 1;\n// */\n"), U("#if false\n/* disabled\n#endif\npaid = 2;\n// */\n"), false),
                ("a constant's provenance relabelled in its comment (P1-3)", U("// [AUTHORED-DRAFT] a game figure\nconst float K = 0.5f;\n"), U("// SOURCED: a paper\nconst float K = 0.5f;\n"), false),
                ("live code after a lone CR ends a comment", U("int paid = 0; // note\rpaid = 1;\n"), U("int paid = 0; // note\rpaid = 2;\n"), false),
                ("live code after a NEL ends a comment", U("int paid = 0; // note\u0085paid = 1;\n"), U("int paid = 0; // note\u0085paid = 2;\n"), false),
                ("live code after a LINE SEPARATOR ends a comment", U("int paid = 0; // note\u2028paid = 1;\n"), U("int paid = 0; // note\u2028paid = 2;\n"), false),
                ("live code after a PARAGRAPH SEPARATOR ends a comment", U("int paid = 0; // note\u2029paid = 1;\n"), U("int paid = 0; // note\u2029paid = 2;\n"), false),
                ("live code after a block opened in a #region name", U("#region Rates /* note\npaid = 1;\n// */\n#endregion\n"), U("#region Rates /* note\npaid = 2;\n// */\n#endregion\n"), false),
                ("live code after a block opened in a #warning message", U("#warning check /* x\npaid = 1;\n// */\n"), U("#warning check /* x\npaid = 2;\n// */\n"), false),
                ("a skipped #if after a no-break space", U("\u00a0#if false\n/* d\n#endif\npaid = 1;\n// */\n"), U("\u00a0#if false\n/* d\n#endif\npaid = 2;\n// */\n"), false),
                ("a skipped #if after a vertical tab", U("\u000b#if false\n/* d\n#endif\npaid = 1;\n// */\n"), U("\u000b#if false\n/* d\n#endif\npaid = 2;\n// */\n"), false),
                ("a skipped #if after a byte-order mark", U("\ufeff#if false\n/* d\n#endif\npaid = 1;\n// */\n"), U("\ufeff#if false\n/* d\n#endif\npaid = 2;\n// */\n"), false),
                ("two constants' provenance marks swapped", U("// SOURCED: OECD\nconst float A = 1f;\n// [AUTHORED-DRAFT] a game figure\nconst float B = 2f;\n"), U("// [AUTHORED-DRAFT] a game figure\nconst float A = 1f;\n// SOURCED: OECD\nconst float B = 2f;\n"), false),
                ("a comment edited beside a #region (a directive alone refuses nothing)", U("#region Rates\nint a = 1; // old words\n#endregion\n"), U("#region Rates\nint a = 1; // new words\n#endregion\n"), true),
                // RL-1: three shapes only a parser can see
                ("an edited branch under a define (disabled here, live under UNITY_EDITOR)", U("#if UNITY_EDITOR\nint a = 1;\n#endif\n"), U("#if UNITY_EDITOR\nint a = 2;\n#endif\n"), false),
                ("a raw string literal's content", U("string s = \"\"\"\n/* one */\n\"\"\";\n"), U("string s = \"\"\"\n/* two */\n\"\"\";\n"), false),
                ("a comment after a directive (a directive's line is kept whole)", U("#if X\n#endif // old\nint a;\n"), U("#if X\n#endif // new\nint a;\n"), false),
                // the RL-1 review's: a mark re-attached, and bytes that are not UTF-8
                ("a mark re-attached by a blank line removed (F1)", U("// SOURCED: OECD\n\nconst float B = 2f;\n"), U("// SOURCED: OECD\nconst float B = 2f;\n"), false),
                ("a mark re-attached by a block-comment line removed (F1)", U("// SOURCED: OECD\n/* see note */\nconst float B = 2f;\n"), U("// SOURCED: OECD\nconst float B = 2f;\n"), false),
                ("a mark moved from one declaration to the next (F1)", U("// SOURCED: OECD\n/* a */ const float A = 1f;\nconst float B = 2f;\n"), U("/* a */ const float A = 1f;\n// SOURCED: OECD\nconst float B = 2f;\n"), false),
                ("two texts that differ only in bytes that are not UTF-8 (F2)", new byte[] { 0x73, 0x3D, 0x22, 0xE9, 0x22, 0x3B, 0x0A }, new byte[] { 0x73, 0x3D, 0x22, 0xE8, 0x22, 0x3B, 0x0A }, false),
                ("a CRLF text against its LF commit (F3: the same code)", U("string s = @\"one\r\ntwo\";\r\n"), U("string s = @\"one\ntwo\";\n"), true),
            };
            var pairs = new List<(string, byte[], byte[])>();
            foreach (var c in cases) { pairs.Add((c.Name, c.Before, c.After)); }
            Dictionary<string, string> verdicts = RoslynVerdicts(pairs, out string failure);
            if (verdicts == null)
            {
                sb.Append($"    ⚠ (0) the comment-only exemption: NO VERDICT - {failure}. The probe verified nothing, so the check fails rather than trust an exemption it cannot prove.\n");
                return false;
            }
            int wrong = 0;
            foreach (var c in cases)
            {
                string refusal = verdicts[c.Name];
                if (refusal == null && ProvenanceBlocks(Encoding.UTF8.GetString(c.Before)) != ProvenanceBlocks(Encoding.UTF8.GetString(c.After))) { refusal = "a provenance claim changed, moved or re-attached"; }
                bool accepted = refusal == null;
                if (accepted != c.Accept) { wrong++; sb.Append($"    ⚠ COMMENT-ONLY PROBE WRONG: {c.Name} - {(accepted ? "accepted and must owe its review" : "owed (" + refusal + ") and must be accepted")}.\n"); }
            }
            string callerInfo = CallerInfoDeclared();
            sb.Append($"    (0) the comment-only exemption (Roslyn, RL-1): {cases.Length} probe case(s), {cases.Length - wrong} right - comment and whitespace edits accepted, every code change owed{(callerInfo == null ? "" : "; ⚠ AND NO CHANGE IS COMMENT-ONLY WHILE " + callerInfo)}.\n");
            return wrong == 0 && cases.Length > 0;
        }

        /// <summary>A process's standard output and error together, and whether it exited 0 within <paramref name="timeoutMs"/> - both streams read asynchronously,
        /// so the timeout can fire (the RL-1 review's F8). False when it cannot start.</summary>
        private static bool TryProcess(string file, string arguments, string workingDirectory, int timeoutMs, out string output)
        {
            output = "";
            try
            {
                var info = new ProcessStartInfo(file, arguments)
                {
                    WorkingDirectory = workingDirectory,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    StandardOutputEncoding = Encoding.UTF8,
                };
                using (Process process = Process.Start(info))
                {
                    if (process == null) { output = "could not start " + file; return false; }
                    var outText = new StringBuilder();
                    var errText = new StringBuilder();
                    process.OutputDataReceived += (s, e) => { if (e.Data != null) { lock (outText) { outText.Append(e.Data).Append('\n'); } } };
                    process.ErrorDataReceived += (s, e) => { if (e.Data != null) { lock (errText) { errText.Append(e.Data).Append('\n'); } } };
                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();
                    if (!process.WaitForExit(timeoutMs)) { try { process.Kill(); } catch (Exception) { } output = outText + errText.ToString() + " (timed out)"; return false; }
                    process.WaitForExit();   // drains the asynchronous readers
                    output = outText.ToString() + errText;
                    return process.ExitCode == 0;
                }
            }
            catch (Exception e) { output = file + ": " + e.Message; return false; }
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
