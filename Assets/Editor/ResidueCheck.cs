using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// THE RESIDUE COUNT, re-armed 2026-09-07 (COMPLETED.md §369) - the open-work marker scan across the LIVE documents and the code.
    /// ⚠ A ZERO MEANS NO CODE ROW IS STARTABLE BY A SESSION, NOT THAT THE WORK IS FINISHED: rows owned by Elias, by Design or by the
    /// calendar are excluded BY NAME and every exclusion is printed with its reason, so the number is "what a session could start now".
    ///
    /// <para>What counts as open: a feature-list row (`**ID — `) whose last status marker is ⏳ or ◐ or absent (✅ and ⏹ close a row);
    /// a fix-track row (`| FT-n |`, `| RF-n |`) whose status cell says OPEN and not CLOSED / STOPPED / APPLIED / HELD; the words
    /// "to fetch" in a live document or a UI string (a row that still owes a figure); TODO / WIP / STUB in code outside the check that
    /// scans for them; and an `[AUTHORED-DRAFT]` tag with nothing after it on its line (a magnitude with no stated reason).</para>
    ///
    /// <para>The check REPORTS and exits 0; it fails only when it has nothing to scan (a run that scanned nothing would print like a
    /// clean one). It is the successor of the retired `InstructionResidueCheck` (§181), re-armed on the sheet of 2026-09-07 with the
    /// exclusion list the sheet named: OWNER ELIAS, DESIGN, CALENDAR - by name, each with its reason.</para>
    /// </summary>
    public static class ResidueCheck
    {
        private static readonly HashSet<string> Historical = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "COMPLETED.md", "CLAUDE.md" };

        /// <summary>The exclusions, by name, each with its reason - the sheet's three owners and nothing else.</summary>
        private static readonly (string Name, string Owner, string Reason)[] Excluded =
        {
            ("E-1", "OWNER ELIAS", "the itanes.it account and the one paste - sending and registering are Elias's by the §E2 convention"),
            ("E-2", "OWNER ELIAS", "the sitting - a capture is a harness film, not Elias's eyes"),
            ("E-3", "OWNER ELIAS", "felt verdict 1 - a staged save, loaded and played"),
            ("E-4", "OWNER ELIAS", "felt verdict 3 - a staged save, loaded and played"),
            ("E-5..E-24", "OWNER ELIAS", "the twenty play-calibration verdicts - numbers awaiting a loop to judge them against"),
            ("E-25", "OWNER ELIAS", "C-C6's basis ruling - executes as written unless struck"),
            ("E-26", "OWNER ELIAS", "C-C11's recalibration recommendations - strike or bless, per line"),
            ("E-27", "OWNER ELIAS", "the two spec-lets (POLISIM_TAX_SPECLET.md among them) - ruled before any code"),
            ("E-28", "OWNER ELIAS", "C-D2's pool resolution - a design question, not a measurement"),
            ("E-29", "OWNER ELIAS", "C-D3's språkrör answer, if he wants the call rather than the record's"),
            ("D-7", "DESIGN", "board 2b, the Policy Web drawn to be read - never pasted, Design's to draw"),
            ("D-8.1", "DESIGN", "the party identity marks, 52 of 53 undrawn - original art by silhouette"),
            ("D-1 portraits", "DESIGN", "appointed ministers render the procedural placeholder until the portrait delivery"),
            ("K-1", "CALENDAR", "the seed refresh from Sweden's real result - 13 September 2026"),
        };

        /// <summary>Documents that are an excluded owner's in their entirety, with the row that owns them.</summary>
        private static readonly Dictionary<string, string> ExcludedFiles = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "POLISIM_TAX_SPECLET.md", "E-27 (OWNER ELIAS) - the spec-let is ruled before any code; its fetch and its bills wait on that ruling" },
        };

        private static readonly Regex RowHead = new Regex(@"^\*\*([A-Z][A-Z0-9]*-[A-Z0-9]+) — ", RegexOptions.Multiline);
        private static readonly Regex FixRow = new Regex(@"^\| ((?:FT|RF)-\d+) \|(.*)$", RegexOptions.Multiline);

        public static void Run()
        {
            CheckExit.ArmLogFold();
            string root = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            var sb = new StringBuilder();
            sb.Append("=== THE RESIDUE COUNT - open work a SESSION could start now (zero is not \"finished\": Elias's, Design's and the calendar's rows are excluded by name below) ===\n");
            var open = new List<string>();
            var excludedFiles = new List<string>();
            int scanned = 0;

            // (a) and (b): the governing document's rows and fix-track rows
            string featurePath = Path.Combine(root, "POLISIM_FEATURE_LIST.md");
            if (File.Exists(featurePath))
            {
                string text = File.ReadAllText(featurePath).Replace("\r\n", "\n");
                scanned++;
                MatchCollection heads = RowHead.Matches(text);
                for (int i = 0; i < heads.Count; i++)
                {
                    int start = heads[i].Index;
                    int end = i + 1 < heads.Count ? heads[i + 1].Index : text.Length;
                    // a row block ends at the next row head or the next markdown heading, whichever is first
                    int heading = text.IndexOf("\n#", start + 1, StringComparison.Ordinal);
                    if (heading > 0 && heading < end) { end = heading; }
                    string block = text.Substring(start, end - start);
                    string id = heads[i].Groups[1].Value;
                    int lastClosed = Math.Max(block.LastIndexOf("✅", StringComparison.Ordinal), block.LastIndexOf("⏹", StringComparison.Ordinal));
                    int lastOpen = Math.Max(block.LastIndexOf("⏳", StringComparison.Ordinal), block.LastIndexOf("◐", StringComparison.Ordinal));
                    if (lastOpen > lastClosed) { open.Add($"{id}: the row's last marker is open ({(lastOpen == block.LastIndexOf("⏳", StringComparison.Ordinal) ? "⏳" : "◐")})"); }
                    else if (lastClosed < 0) { open.Add($"{id}: the row carries no status marker at all"); }
                }
                foreach (Match m in FixRow.Matches(text))
                {
                    string cells = m.Groups[2].Value;
                    int lastBar = cells.LastIndexOf('|');
                    int prevBar = lastBar > 0 ? cells.LastIndexOf('|', lastBar - 1) : -1;
                    string status = prevBar >= 0 ? cells.Substring(prevBar + 1, lastBar - prevBar - 1) : cells;
                    bool closed = status.Contains("CLOSED") || status.Contains("STOPPED") || status.Contains("APPLIED") || status.Contains("HELD") || status.Contains("LANDED") || status.Contains("FIXED");
                    if (!closed) { open.Add($"{m.Groups[1].Value}: the fix-track row's status is not closed ({Trim(status, 90)})"); }
                }
            }

            // (c): "to fetch" in the live documents and the UI strings
            foreach (string path in Directory.GetFiles(root, "*.md", SearchOption.TopDirectoryOnly))
            {
                string name = Path.GetFileName(path);
                if (Historical.Contains(name)) { continue; }
                if (ExcludedFiles.ContainsKey(name)) { excludedFiles.Add($"{name}: {ExcludedFiles[name]}"); continue; }
                scanned++;
                string[] lines = File.ReadAllLines(path);
                for (int i = 0; i < lines.Length; i++)
                {
                    if (lines[i].IndexOf("to fetch", StringComparison.OrdinalIgnoreCase) >= 0 && lines[i].IndexOf("closed as billed", StringComparison.OrdinalIgnoreCase) < 0 && lines[i].IndexOf("no longer", StringComparison.OrdinalIgnoreCase) < 0)
                    { open.Add($"{name}:{i + 1}: still says \"to fetch\""); }
                }
            }

            // (d): code markers - TODO / WIP / STUB (outside the check that scans for them), and an AUTHORED-DRAFT tag with no reason after it
            var codeMarker = new Regex(@"\b(TODO|WIP|STUB)\b");
            var bareDraft = new Regex(@"\[AUTHORED-DRAFT\]\s*(\*/|</remarks>|</summary>)?\s*$");
            foreach (string path in Directory.GetFiles(Path.Combine(root, "Assets"), "*.cs", SearchOption.AllDirectories))
            {
                string file = Path.GetFileName(path);
                if (file == "MetaTextCheck.cs" || file == "ResidueCheck.cs") { continue; }
                scanned++;
                string[] lines = File.ReadAllLines(path);
                for (int i = 0; i < lines.Length; i++)
                {
                    if (codeMarker.IsMatch(lines[i])) { open.Add($"{file}:{i + 1}: code marker ({codeMarker.Match(lines[i]).Value})"); }
                    // a tag at the end of a line whose NEXT line continues the comment is not bare - the reason follows on the next line
                    bool continues = i + 1 < lines.Length && (lines[i + 1].TrimStart().StartsWith("///") || lines[i + 1].TrimStart().StartsWith("//")) && lines[i + 1].Trim().Length > 4;
                    if (bareDraft.IsMatch(lines[i]) && !continues) { open.Add($"{file}:{i + 1}: an [AUTHORED-DRAFT] with no reason after it"); }
                    if (lines[i].IndexOf("to fetch", StringComparison.OrdinalIgnoreCase) >= 0 && lines[i].Contains("\"")) { open.Add($"{file}:{i + 1}: a UI string still says \"to fetch\""); }
                }
            }

            sb.Append($"    THE EXCLUSIONS, by name ({Excluded.Length}):\n");
            foreach (var e in Excluded) { sb.Append($"      {e.Name,-14} {e.Owner,-12} {e.Reason}\n"); }
            sb.Append($"    THE RESIDUE: {open.Count} open item(s) a session could start ({scanned} file(s) scanned).\n");
            foreach (string o in open) { sb.Append($"      - {o}\n"); }
            if (open.Count == 0) { sb.Append("    ZERO - no CODE row is startable. This is not \"finished\": the excluded rows above are still open, and they are Elias's, Design's or the calendar's.\n"); }
            Debug.Log(sb.ToString());
            bool ok = scanned > 0;
            if (!ok) { Debug.LogError("RESIDUE: nothing was scanned - this run verified NOTHING rather than finding nothing."); }
            Debug.Log(ok ? $"=== ResidueCheck: REPORTED - residue {open.Count} ===" : "=== ResidueCheck: FAILED (nothing scanned) ===");
            CheckExit.Finish(ok ? 0 : 1);
        }

        private static string Trim(string s, int n) { s = s.Trim(); return s.Length <= n ? s : s.Substring(0, n) + "…"; }
    }
}
