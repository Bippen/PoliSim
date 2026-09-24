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
    /// <para><b>WIDENED 2026-09-22 (§565, the sitting pass's Track 2 - Design's whole-game reading found five classes the
    /// patterns above let through):</b> (1) <b>an identifier in prose</b> - a CamelCase token that is a name DECLARED in
    /// <c>Assets/Scripts</c> (a type, a member, an enum value) standing inside a literal that has a space in it (*"Seats shift
    /// gradually with your ApprovalRating"*, *"that's CompetenceBias"*, *"NO TradePartner LINK"*); (2) a <c>HUE:</c> token (the
    /// selector printed the country card's area ink by name); (3) the ASCII arrow (the desk's arrow is the glyph - the scenario
    /// verdict and a Labor trailing printed the typed one); (4) a central bank named by literal - <c>FEDERAL RESERVE</c> as a
    /// title, or <c>Federal Reserve</c> in prose - outside <c>GetCentralBankName</c>'s table (the Docket's dossier said FEDERAL
    /// RESERVE for the Riksbank); (5) instruction prose addressed to the hand - <c>Hover a</c>, <c>Click to</c>, <c>Drag the</c>.
    /// For these five an INTERPOLATION HOLE is code, not text: an interpolated literal is read with its holes blanked (nested
    /// braces and nested strings included), so a member named inside a hole is never a hit. A literal on a line that logs, throws
    /// or asserts is a diagnostic, not a label, and is skipped for the five (the nineteen patterns above keep reading every
    /// literal, as they always have). Each of the five was PROVED FAILING on the tree before its fix.</para>
    ///
    /// <para><b>THE ALLOWLIST</b> — player-facing uses that share a word with the banned classes,
    /// each with its reason: <c>PRELIMINARY</c> / <c>REVISED</c> / <c>FINAL</c> on published figures
    /// (the statistics honesty convention — a status of the DATA, addressed to the player);
    /// <c>not implemented</c> / <c>implemented</c> as a LAW's enactment state on the laws tab and
    /// the ledger rows (a state of the world, not of the build); the Policy Web's
    /// <c>DERIVED</c> / <c>DECLARED</c> edge idiom (R-C6 — the player's own reading of where an
    /// edge comes from); <c>SCENARIO COMPLETE</c> on the scenario verdict screen (the player
    /// completed it); <c>PROVISIONAL</c> on the day-one government while the real one is not on record
    /// (K-1, 2026-09-24 - a standing of the world, like PRELIMINARY of a figure). Anything else with these words is a hit.</para>
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
            // ⚠ S-16, ARMED AT C-E3 (2026-08-31). A BACKTICK IN A PLAYER-FACING STRING is a reliable tell
            // for a leaked identifier: C-C8's first cut shipped "`Country` carries no bilateral relations
            // field" to a screen, where the backticks rendered literally — and this check PASSED it,
            // because a backtick was in none of the nineteen patterns above. Markdown on a game screen is
            // developer text wearing punctuation, and it is exactly the class P-A1 cut 131 strings of.
            ("backtick (a leaked identifier)", new Regex("`")),
        };

        /// <summary>§565: the five widened patterns - read with interpolation holes blanked, prose literals only where <c>ProseOnly</c>, never on a diagnostic line.</summary>
        private static readonly (string Name, Regex Pattern, bool ProseOnly)[] Widened =
        {
            ("HUE: token", new Regex(@"\bHUE:"), false),
            ("ASCII arrow", new Regex(@"->"), false),
            ("a central bank named by literal", new Regex(@"\bFEDERAL RESERVE\b|\bFederal Reserve\b"), false),
            ("instruction prose (Hover / Click / Drag)", new Regex(@"\b(Hover|Click|Drag|Scroll|Press|Tap) (a|an|the|on|to|over|here|and)\b|\b(hover|click|drag|scroll|press|tap) (to|on|over|here)\b"), true),
        };

        private static readonly Regex CamelToken = new Regex(@"\b[A-Z][a-z]+(?:[A-Z][a-z]+)+\b");
        private static readonly Regex DiagnosticLine = new Regex(@"Debug\.Log|\bthrow\b|Exception\(|Assert\.|LogError|LogWarning|\.Log\(");
        private static readonly Regex Declaration = new Regex(@"\b(?:class|struct|enum|interface)\s+([A-Z]\w+)|\b(?:public|private|internal|protected)\s+(?:static\s+|readonly\s+|const\s+|override\s+|virtual\s+|abstract\s+|new\s+)*[\w<>\[\],.?()]+\s+([A-Z]\w+)\s*[;={(<]|^\s+([A-Z]\w+)\s*[,=]", RegexOptions.Multiline);

        /// <summary>§565: (pattern name, file suffix, literal substring) - a widened pattern's allowed uses, each with its reason.</summary>
        private static readonly (string Pattern, string File, string Contains)[] AllowWidened =
        {
            // the Policy Web names the model's own fields as an edge's provenance by ruling (R-C6), and the page is the sitting pass's leave-alone set
            ("identifier in prose", "PolicyWebRenderer.cs", ""),
            // People's captions name the table a plate is drawn from, in the honesty idiom's own words; People is the sitting pass's leave-alone set
            ("identifier in prose", "GameController.cs", "THE ELECTORATE"),
            ("identifier in prose", "GameController.cs", "POPULATION BY BAND"),
            ("identifier in prose", "GameController.cs", "EMPLOYMENT · SHARE BY SECTOR"),
            ("identifier in prose", "GameController.cs", "VOTER GROUPS · POPULATION SHARE"),
            // a guard's own context names - what the overflow and containment guards measured, for the film log, never a label
            ("identifier in prose", "LedgerRow.cs", "LedgerRow "),
            ("identifier in prose", "PoliSimWidgets.cs", "StatTile "),
            ("identifier in prose", "PolicyScreenStatsRenderer.cs", "StatChip "),
            // the audio pack's own file names in the importer's notes, and the mixer's name
            ("identifier in prose", "AudioDirector.cs", ""),
            // a theme dump and three data-integrity messages: the font report, and the tables that must grow with their enum
            ("identifier in prose", "PoliSimTheme.cs", "fonts:"),
            ("identifier in prose", "PublishedData.cs", "counterpart"),
            ("identifier in prose", "StructuralParameters.cs", "must grow with the enum"),
            ("identifier in prose", "TradeMatrixTable.cs", "carries no flow"),
            // a log line assembled before the Debug.Log that prints it (the map's west-push report)
            ("ASCII arrow", "MapRenderer.cs", "one cell west"),
            // a United States statute's own TITLE, cited on the law's card - the institution is in the act's name, and a citation is not a hard-coded institution
            ("a central bank named by literal", "LawCatalog.cs", "Reform Act of 1977"),
        };

        /// <summary>§565: (pattern name, file suffix, SOURCE-LINE substring) - where the literal is allowed by what the line around it is. One use so far: the country
        /// table this check exists to push every caller toward (<c>GetCentralBankName</c>), whose USA row is the string itself.</summary>
        private static readonly (string Pattern, string File, string LineContains)[] AllowWidenedLine =
        {
            ("a central bank named by literal", "GameController.cs", "case CountryId.USA:"),
        };

        /// <summary>(file suffix, literal substring) pairs that are player-facing despite the word — enumerated in the class doc.</summary>
        private static readonly (string File, string Contains)[] Allow =
        {
            ("GameController.cs", "SCENARIO COMPLETE"),
            ("GameController.cs", "Scenario complete"),
            // K-1 part (4) (2026-09-24, Elias's order: the day-one government "marked provisional"): PROVISIONAL here is the standing of
            // the WORLD's government - the Riksdag has not yet chosen a prime minister, so the formation's result stands in - addressed
            // to the player, the same class as PRELIMINARY on a published figure; not the data-class tag this check hunts. Exactly these
            // three literals: the picker's line, its row mark, the compass legend's word.
            ("CountrySelectorScreen.cs", "PROVISIONAL · THE RIKSDAG HAS NOT YET CHOSEN A PRIME MINISTER"),
            ("CountrySelectorScreen.cs", " · IN THE CABINET (PROVISIONAL)"),
            ("PoliticalCompassRenderer.cs", " (PROVISIONAL)"),
            // Board 5b (D11 row 2, 2026-09-02): the People page prints its honesty class ON the instrument in the
            // coalition page's own vocabulary - DERIVED / DECLARED / SOURCED / MEASURED - by Design's ruling; the word
            // SOURCED is player-facing there, with its year, and is not the provenance tag this check hunts.
            ("GameController.cs", "SOURCED · SCB 2014"),
            ("GameController.cs", "VOTING AGE · SOURCED · CONSTITUTION"),
            ("GameController.cs", "A DERIVATION FROM TWO SOURCED SERIES, NOT A FORECAST"),
            ("GameController.cs", "SOURCED — A PUBLISHED SERIES, WITH ITS YEAR"),
            // P5-C2 (2026-09-05, board 9c): the society-stat plate's HONESTY chips are Design's own vocabulary - SOURCED / DERIVED / ABSENT · STATED / SUPPORTING,
            // player-facing with the source line's dataflow id and year beside them; not the provenance tag this check hunts.
            ("GameController.Health.cs", "SOURCED"),
            ("GameController.Education.cs", "SOURCED"),   // P5-C3 (2026-09-06): the same honesty chips on the education plate
            ("GameController.Infrastructure.cs", "SOURCED"),   // P5-C4 (2026-09-06)
            ("GameController.Environment.cs", "SOURCED"),   // P5-C5 (2026-09-06)
            ("GameController.Energy.cs", "SOURCED"),   // EN-6 (2026-09-12): the energy page's honesty chips
            // F4-2 (2026-09-13): a statute's own citation carries the section sign as part of the law's name - "§ 32a EStG", "§ 1(j)(2)" - and is
            // the source line the schedule row prints (board 15b); it is the law's paragraph, not a record's section, and is allowed by the
            // authority's name beside it
            ("TaxSchedule.cs", "EStG"),
            ("TaxSchedule.cs", "Rev. Proc."),
            ("TaxSchedule.cs", "Skatteverket"),
            // PN-1 (2026-09-13): the pension statutes' paragraphs - "2 kap. 10 a–10 c §§", "SGB VI § 35", "§ 216(l)" - the laws' own names on the node's sentence
            ("PensionAgeStatute.cs", "Socialförsäkringsbalken"),
            ("PensionAgeStatute.cs", "SGB VI"),
            ("PensionAgeStatute.cs", "Social Security Act"),
            ("GameController.ImmigrationPoverty.cs", "SOURCED"),   // P5-C6 (2026-09-06)
            // P4-B1 (2026-09-04): the range-caption catalog is [AUTHORED] game fiction - the desk's own deadpan on every
            // dial's ten bands - and is exempt BY NAME so its satire is not read as meta-text; RangeCaptionCheck holds it
            // to the model's effect signs instead. Nothing else in Assets/Scripts/UI is widened by this row.
            ("RangeCaptions.cs", ""),
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

            HashSet<string> declared = DeclaredIdentifiers(projectRoot);

            var hits = new List<string>();
            var census = new SortedDictionary<string, SortedDictionary<string, int>>();
            int literals = 0;
            foreach (string file in files)
            {
                string rel = file.Substring(projectRoot.Length + 1).Replace('\\', '/');
                string code = StripComments(SourceText.Read(file));
                foreach ((string literal, int line, string text, string lineText) in Literals(code))
                {
                    literals++;
                    string at = rel;
                    int where = line;
                    void Hit(string name, string shown)
                    {
                        if (!census.TryGetValue(name, out SortedDictionary<string, int> perFile)) { perFile = new SortedDictionary<string, int>(); census[name] = perFile; }
                        perFile[at] = perFile.TryGetValue(at, out int n) ? n + 1 : 1;
                        hits.Add($"  {at}:{where}  [{name}]  \"{Truncate(shown, 110)}\"");
                    }

                    foreach ((string name, Regex pattern) in Banned)
                    {
                        if (!pattern.IsMatch(literal)) { continue; }
                        if (IsAllowed(rel, literal)) { continue; }
                        Hit(name, literal);
                    }

                    // §565: the widened five read the literal's TEXT (interpolation holes blanked), and never a diagnostic's line
                    if (DiagnosticLine.IsMatch(lineText)) { continue; }
                    bool prose = text.Contains(" ");
                    foreach ((string name, Regex pattern, bool proseOnly) in Widened)
                    {
                        if (proseOnly && !prose) { continue; }
                        if (!pattern.IsMatch(text)) { continue; }
                        if (IsAllowed(rel, literal) || IsAllowedWidened(name, rel, text) || IsAllowedWidenedLine(name, rel, lineText)) { continue; }
                        Hit(name, text);
                    }

                    if (prose && !IsAllowedWidened("identifier in prose", rel, text))
                    {
                        foreach (Match m in CamelToken.Matches(text))
                        {
                            if (m.Value == "PoliSim" || !declared.Contains(m.Value)) { continue; }
                            Hit("identifier in prose", m.Value + "  in  " + text);
                        }
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

        private static bool IsAllowedWidenedLine(string pattern, string file, string lineText)
        {
            foreach ((string p, string f, string contains) in AllowWidenedLine)
            {
                if (p == pattern && file.EndsWith(f, StringComparison.Ordinal) && lineText.Contains(contains)) { return true; }
            }

            return false;
        }

        private static bool IsAllowedWidened(string pattern, string file, string text)
        {
            foreach ((string p, string f, string contains) in AllowWidened)
            {
                if (p == pattern && file.EndsWith(f, StringComparison.Ordinal) && text.Contains(contains)) { return true; }
            }

            return false;
        }

        /// <summary>§565: every CamelCase name declared under Assets/Scripts - types, members, enum values - so a token in prose is judged against the code, not a word list.</summary>
        private static HashSet<string> DeclaredIdentifiers(string projectRoot)
        {
            var set = new HashSet<string>(StringComparer.Ordinal);
            foreach (string file in Directory.GetFiles(Path.Combine(projectRoot, "Assets/Scripts"), "*.cs", SearchOption.AllDirectories))
            {
                foreach (Match m in Declaration.Matches(SourceText.Read(file)))
                {
                    for (int g = 1; g <= 3; g++) { if (m.Groups[g].Success) { set.Add(m.Groups[g].Value); } }
                }
            }

            return set;
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

        /// <summary>Every literal: (the literal as the nineteen patterns have always read it, its line, the literal's TEXT with interpolation holes blanked, the source line).
        /// §565: an interpolated literal's hole is CODE - it is skipped to its matching brace, nested braces and nested string literals included - and the nineteen
        /// patterns keep reading the raw literal as before (a hole that leaks an identifier through a format is still their business).</summary>
        private static IEnumerable<(string, int, string, string)> Literals(string code)
        {
            int line = 1;
            int i = 0;
            while (i < code.Length)
            {
                char c = code[i];
                if (c == '\n') { line++; i++; continue; }
                if (c != '"') { i++; continue; }

                bool verbatim = (i > 0 && code[i - 1] == '@') || (i > 1 && code[i - 2] == '@' && code[i - 1] == '$');
                bool interpolated = (i > 0 && code[i - 1] == '$') || (i > 1 && code[i - 2] == '$' && code[i - 1] == '@');
                int lineStart = code.LastIndexOf('\n', Math.Max(0, i - 1)) + 1;
                int lineEnd = code.IndexOf('\n', i);
                if (lineEnd < 0) { lineEnd = code.Length; }
                string lineText = code.Substring(lineStart, lineEnd - lineStart);

                int start = i + 1;
                int j = start;
                var sb = new StringBuilder();
                var text = new StringBuilder();
                while (j < code.Length)
                {
                    if (verbatim && code[j] == '"' && j + 1 < code.Length && code[j + 1] == '"') { sb.Append('"'); text.Append('"'); j += 2; continue; }
                    if (!verbatim && code[j] == '\\' && j + 1 < code.Length) { sb.Append(code[j + 1]); text.Append(code[j + 1]); j += 2; continue; }
                    if (interpolated && code[j] == '{' && j + 1 < code.Length && code[j + 1] == '{') { sb.Append('{'); text.Append('{'); j += 2; continue; }
                    if (interpolated && code[j] == '}' && j + 1 < code.Length && code[j + 1] == '}') { sb.Append('}'); text.Append('}'); j += 2; continue; }
                    if (interpolated && code[j] == '{')
                    {
                        int depth = 0;
                        int k = j;
                        while (k < code.Length)
                        {
                            char h = code[k];
                            if (h == '"') { k++; while (k < code.Length && code[k] != '"') { if (code[k] == '\\') { k++; } k++; } k++; continue; }
                            if (h == '{') { depth++; }
                            else if (h == '}') { depth--; if (depth == 0) { break; } }
                            else if (h == '\n') { line++; }
                            k++;
                        }

                        sb.Append(code, j, Math.Min(k + 1, code.Length) - j);
                        text.Append(' ');
                        j = k + 1;
                        continue;
                    }

                    if (code[j] == '"') { break; }
                    if (code[j] == '\n') { line++; }
                    sb.Append(code[j]);
                    text.Append(code[j]);
                    j++;
                }

                yield return (sb.ToString(), line, text.ToString(), lineText);
                i = j + 1;
            }
        }

        private static string Truncate(string s, int max) => s.Length <= max ? s : s.Substring(0, max) + "…";
    }
}
