using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// P-A1 (Playtest 1, finding 1 — 2026-08-29): the guard against DEVELOPER-FACING TEXT on
    /// PLAYER SURFACES. Elias, in substance: *"COMPLETED" in the laws tab, progress markers,
    /// anything addressed to the builder rather than the player.*
    ///
    /// <para><b>WHAT THIS ENUMERATES</b> (rule 14). Every string literal — plain, interpolated
    /// (`$"…"`), verbatim (`@"…"`) — in the player-reachable UI sources:
    /// <c>Assets/Scripts/UI/*.cs</c> (every screen the game draws), plus the two catalogs whose
    /// strings the screens print verbatim: <c>Assets/Scripts/Simulation/LawCatalog.cs</c> (law
    /// names, descriptions, citations — the laws tab) and <c>Assets/Scripts/Data/*.cs</c> (display
    /// names of enums and portfolios). Comments are stripped first (<c>//</c>, <c>/* */</c>,
    /// <c>///</c>), so a doc comment naming a ruling is not a hit — only text that can reach a
    /// label is. Exit 1 on any hit outside the allowlist below; the census table (token × file ×
    /// count) is printed every run, hits first.</para>
    ///
    /// <para><b>THE BANNED TOKENS</b> — the classes Elias named, each as a pattern:
    /// completion / progress language addressed to the builder (<c>COMPLETED</c>, <c>IMPLEMENTED</c>
    /// as a tag, <c>TODO</c>, <c>WIP</c>, <c>PLACEHOLDER</c>, <c>STUB</c>); internal section and ruling
    /// references (<c>§</c>, <c>section N</c>, <c>R-XN</c>, <c>W-XN</c>, <c>board 1m</c>, <c>Annex X</c>);
    /// build vocabulary (<c>Master Sequence</c>, <c>step 5d</c>, <c>Phase A/2</c>, <c>this pass</c>,
    /// <c>harness</c>, <c>backtest</c>, <c>Design's</c>, <c>the spec</c>); data-class tags
    /// (<c>[AUTHORED-DRAFT]</c>, <c>PROVISIONAL</c>, <c>[DERIVED]</c>, <c>IS DERIVED</c>,
    /// <c>SOURCED</c> as a tag); research-status prefixes on citations (<c>CONFIRMED -</c>,
    /// <c>GENRE-IDIOM</c>, <c>UNCONFIRMED</c>).</para>
    ///
    /// <para><b>THE ALLOWLIST</b> — player-facing uses that share a word with the banned classes,
    /// each with its reason: <c>PRELIMINARY</c> / <c>REVISED</c> / <c>FINAL</c> on published figures
    /// (the statistics honesty convention — a status of the DATA, addressed to the player);
    /// <c>not implemented</c> / <c>implemented</c> as a LAW's enactment state on the laws tab and
    /// the ledger rows (a state of the world, not of the build); the Policy Web's
    /// <c>DERIVED</c> / <c>DECLARED</c> edge idiom (R-C6 — the player's own reading of where an
    /// edge comes from); <c>SCENARIO COMPLETE</c> on the scenario verdict screen (the player
    /// completed it). Anything else with these words is a hit.</para>
    /// </summary>
    public static class MetaTextCheck
    {
        private static readonly string[] Roots = { "Assets/Scripts/UI", "Assets/Scripts/Simulation/LawCatalog.cs", "Assets/Scripts/Data" };

        private static readonly (string Name, Regex Pattern)[] Banned =
        {
            ("section sign §", new Regex("§")),
            ("'section N'", new Regex(@"\bsection \d+\b", RegexOptions.IgnoreCase)),
            ("ruling ref R-XN", new Regex(@"\bR-[A-Z]{1,3}\d+[a-z]?\b")),
            ("item ref W-XN", new Regex(@"\bW-[A-H]\d+\b")),
            ("board ref", new Regex(@"\bboard \d[a-z]?(-r\d)?\b", RegexOptions.IgnoreCase)),
            ("annex ref", new Regex(@"\bAnnex [A-Z]\b")),
            ("COMPLETED", new Regex(@"\bCOMPLETED\b")),
            ("IMPLEMENTED tag", new Regex(@"\bIMPLEMENTED\b")),
            ("TODO / WIP / STUB / PLACEHOLDER", new Regex(@"\b(TODO|WIP|STUB|PLACEHOLDER)\b")),
            ("Master Sequence / step Nx", new Regex(@"Master Sequence|\bstep \d+[a-z]?\b", RegexOptions.IgnoreCase)),
            ("Phase X", new Regex(@"\bPhase [0-9A-C]\b")),
            ("'this pass'", new Regex(@"\bthis pass\b", RegexOptions.IgnoreCase)),
            ("harness / backtest", new Regex(@"\b(harness|backtest)\b", RegexOptions.IgnoreCase)),
            ("research-status vocabulary", new Regex(@"\b(GENRE-IDIOM|DIRECTIONAL)\b")),
            ("Design's / the spec", new Regex(@"\bDesign's\b|\bthe spec\b")),
            ("AUTHORED-DRAFT", new Regex(@"AUTHORED-DRAFT")),
            ("PROVISIONAL / UNCONFIRMED", new Regex(@"\b(PROVISIONAL|UNCONFIRMED)\b")),
            ("[DERIVED] / IS DERIVED / SOURCED tag", new Regex(@"\[DERIVED\]|\bIS DERIVED\b|\[SOURCED\]|\bSOURCED\b")),
            ("citation status prefix", new Regex(@"^(CONFIRMED|GENRE-IDIOM)\b")),
        };

        /// <summary>(file suffix, literal substring) pairs that are player-facing despite the word — enumerated in the class doc.</summary>
        private static readonly (string File, string Contains)[] Allow =
        {
            ("GameController.cs", "SCENARIO COMPLETE"),
            ("GameController.cs", "Scenario complete"),
        };

        public static void Run()
        {
            CheckExit.ArmLogFold();
            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            var files = new List<string>();
            foreach (string root in Roots)
            {
                string full = Path.Combine(projectRoot, root);
                if (File.Exists(full)) { files.Add(full); }
                else if (Directory.Exists(full)) { files.AddRange(Directory.GetFiles(full, "*.cs", SearchOption.TopDirectoryOnly)); }
            }

            files.Sort(StringComparer.Ordinal);

            var hits = new List<string>();
            var census = new SortedDictionary<string, SortedDictionary<string, int>>();
            int literals = 0;
            foreach (string file in files)
            {
                string rel = file.Substring(projectRoot.Length + 1).Replace('\\', '/');
                string code = StripComments(File.ReadAllText(file));
                foreach ((string literal, int line) in Literals(code))
                {
                    literals++;
                    foreach ((string name, Regex pattern) in Banned)
                    {
                        if (!pattern.IsMatch(literal)) { continue; }
                        if (IsAllowed(rel, literal)) { continue; }
                        if (!census.TryGetValue(name, out SortedDictionary<string, int> perFile)) { perFile = new SortedDictionary<string, int>(); census[name] = perFile; }
                        perFile[rel] = perFile.TryGetValue(rel, out int n) ? n + 1 : 1;
                        hits.Add($"  {rel}:{line}  [{name}]  \"{Truncate(literal, 110)}\"");
                    }
                }
            }

            var sb = new StringBuilder();
            sb.Append($"=== MetaTextCheck: {files.Count} files, {literals} string literals scanned, {hits.Count} hit(s) ===\n");
            if (hits.Count > 0)
            {
                sb.Append("  hits:\n");
                foreach (string h in hits) { sb.Append(h).Append('\n'); }
            }

            sb.Append("\n  census (token x file x count):\n");
            if (census.Count == 0) { sb.Append("  (none)\n"); }
            foreach (KeyValuePair<string, SortedDictionary<string, int>> token in census)
            {
                foreach (KeyValuePair<string, int> perFile in token.Value)
                {
                    sb.Append($"  {token.Key,-36} {perFile.Key,-56} {perFile.Value,4}\n");
                }
            }

            sb.Append($"\n=== MetaTextCheck: {(hits.Count == 0 ? "CLEAN - no developer-facing text on a player surface" : hits.Count + " hit(s) - developer text on a player surface")} ===\n");
            Debug.Log(sb.ToString());
            CheckExit.Finish(hits.Count == 0 ? 0 : 1);
        }

        private static bool IsAllowed(string file, string literal)
        {
            foreach ((string f, string contains) in Allow)
            {
                if (file.EndsWith(f, StringComparison.Ordinal) && literal.Contains(contains)) { return true; }
            }

            return false;
        }

        /// <summary>Removes // line comments, /* */ blocks and /// docs without touching string literals (a "//" inside a string survives).</summary>
        private static string StripComments(string code)
        {
            var sb = new StringBuilder(code.Length);
            int i = 0;
            while (i < code.Length)
            {
                char c = code[i];
                if (c == '"')
                {
                    bool verbatim = i > 0 && code[i - 1] == '@';
                    int j = i + 1;
                    while (j < code.Length)
                    {
                        if (verbatim && code[j] == '"' && j + 1 < code.Length && code[j + 1] == '"') { j += 2; continue; }
                        if (!verbatim && code[j] == '\\') { j += 2; continue; }
                        if (code[j] == '"') { break; }
                        j++;
                    }

                    sb.Append(code, i, Math.Min(j + 1, code.Length) - i);
                    i = j + 1;
                    continue;
                }

                if (c == '/' && i + 1 < code.Length && code[i + 1] == '/')
                {
                    while (i < code.Length && code[i] != '\n') { i++; }
                    continue;
                }

                if (c == '/' && i + 1 < code.Length && code[i + 1] == '*')
                {
                    int end = code.IndexOf("*/", i + 2, StringComparison.Ordinal);
                    int skipTo = end < 0 ? code.Length : end + 2;
                    for (int k = i; k < skipTo; k++) { if (code[k] == '\n') { sb.Append('\n'); } }   // keep line numbers
                    i = skipTo;
                    continue;
                }

                sb.Append(c);
                i++;
            }

            return sb.ToString();
        }

        private static IEnumerable<(string, int)> Literals(string code)
        {
            int line = 1;
            int i = 0;
            while (i < code.Length)
            {
                char c = code[i];
                if (c == '\n') { line++; i++; continue; }
                if (c != '"') { i++; continue; }

                bool verbatim = i > 0 && code[i - 1] == '@';
                int start = i + 1;
                int j = start;
                var sb = new StringBuilder();
                while (j < code.Length)
                {
                    if (verbatim && code[j] == '"' && j + 1 < code.Length && code[j + 1] == '"') { sb.Append('"'); j += 2; continue; }
                    if (!verbatim && code[j] == '\\' && j + 1 < code.Length) { sb.Append(code[j + 1]); j += 2; continue; }
                    if (code[j] == '"') { break; }
                    if (code[j] == '\n') { line++; }
                    sb.Append(code[j]);
                    j++;
                }

                yield return (sb.ToString(), line);
                i = j + 1;
            }
        }

        private static string Truncate(string s, int max) => s.Length <= max ? s : s.Substring(0, max) + "…";
    }
}
