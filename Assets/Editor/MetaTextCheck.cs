using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using PoliSim.UI;
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
    /// edge comes from); <c>PROVISIONAL</c> on the day-one government while the real one is not on record
    /// (K-1, 2026-09-24 - a standing of the world, like PRELIMINARY of a figure). Anything else with these words is a hit.</para>
    ///
    /// <para><b>THE POSSESSIVE (UI-1; J8, Elias's ruling of 2026-10-19: one possessive helper).</b> Every runtime source under <c>Assets/Scripts</c> but the
    /// film harness, read by a lexer of its own: a possessive built BY HAND on a value - an apostrophe-s (straight, curly, or the curly one escaped; either
    /// case) straight after an interpolation hole or a format placeholder, or a literal that OPENS with one and is joined to a value (after <c>+</c>,
    /// <c>+=</c>, a ternary's <c>?</c> or <c>:</c>, or as an argument of Append, Concat or Insert) - is a hit, in a hole's nested literals too; the value's
    /// possessive goes through <see cref="UiFormat.Possessive(string)"/> or its sentence-case overload, which writes "UNITED STATES'" where the hand wrote
    /// "STATES'S". Fixed prose whose noun is in the literal ("the government's") is never a hit, and neither is a literal inside a diagnostic CALL (Unity's
    /// log, an exception's constructor, an assertion) - by the call, not the line. A year's possessive is allowed by its literal, while that literal holds
    /// it alone. THE TEETH, every run: a planted form per class caught, planted clean forms not flagged, the allow-list both ways, the harness shown to
    /// hold what the walk excludes, and the helper's battery. ⚠ OUT OF REACH, by design: the apostrophe alone built by hand (<c>name + "' "</c> - the
    /// same characters close a quotation, <c>'{x}'</c>); a log's words built in a local first (judged where the literal stands, so a hit); a literal joined to a literal (<c>"the government" + "'s"</c>, flagged as built by hand); a hole's nested verbatim or interpolated
    /// string holding a brace or a quote (the hole skipper reads them as plain strings); escaped braces in a format string that is not interpolated.</para>
    /// </summary>
    public static class MetaTextCheck
    {
        private static readonly string[] Roots = { "Assets/Scripts/UI", "Assets/Scripts/Simulation/LawCatalog.cs", "Assets/Scripts/Data", "Assets/Scripts/Simulation/Congress.cs", "Assets/Scripts/Simulation/UsBudgetProcedure.cs", "Assets/Scripts/Elections/PresidentialDocket.cs", "Assets/Scripts/Simulation/UsLapse.cs", "Assets/Scripts/Elections/UsLapseDraw.cs", "Assets/Scripts/Simulation/UsMajorityReconciliation.cs", "Assets/Scripts/Simulation/UsFedConsent.cs", "Assets/Scripts/Simulation/UsDebtLimit.cs" };   // PS-6 US-24 (the review's L4): UsDebtLimit.cs the raise's division title; PS-6 US-29: UsFedConsent.cs the Senate's count on the Fed chair in a page's words; PS-6 US-23: UsMajorityReconciliation.cs the majority's reconciliation's title and words; PS-6 US-18: Congress.cs builds the words the Laws page, the cards and the slips print; PS-6 US-22: UsBudgetProcedure.cs the budget acts', the continuing resolution's and the lapse's; PS-6 US-21b: PresidentialDocket.cs the docket's period; PS-6 US-22b (the review's L3): UsLapse.cs the lapse's economics in a page's words, UsLapseDraw.cs the fitted label

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
            // Design's board 18b (2026-09-24): "(E-49) on the card and (S8) in the reason - errand and stage identifiers belong in the
            // log, not the card". The register ids the records use, each as a whole word, case-sensitive; "S 107", "SD 73" and a year never match.
            ("register number (an errand, stage or ticket id: E-49, S8, D-SP, PF-16, R-EL10 …)", new Regex(@"\b(E-\d+|S\d+|D-[A-Z]{2,3}|D\d+[a-z]?|PF-\d+|PS-\d+|SP-\d+|MM-\d+|CL-\d+|EN-\d+|FT-\d+|PN-\d+|K-1[a-z]?|R-EL\d+|T-\d+)\b")),
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
            // People's dense lines name the table a plate is drawn from, provenance last (§662, board 20a part C)
            ("identifier in prose", "GameController.cs", "CONSTITUTION · CohortVoterGroups"),
            ("identifier in prose", "GameController.cs", "BOTH THE MODEL'S · PopulationCohorts"),
            ("identifier in prose", "GameController.cs", "EMPLOYMENT · SHARE BY SECTOR"),
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
            // K-1 part (4) (2026-09-24, Elias's order: the day-one government "marked provisional"): PROVISIONAL here is the standing of
            // the WORLD's government - the Riksdag has not yet chosen a prime minister, so the formation's result stands in - addressed
            // to the player, the same class as PRELIMINARY on a published figure; not the data-class tag this check hunts. Exactly these
            // three literals: the picker's line, its row mark, the compass legend's word - and the picker line's other reason, a start whose sitting
            // cabinet's parties the record does not name (Italy's, France's), the same standing.
            ("CountrySelectorScreen.cs", "PROVISIONAL · THE RIKSDAG HAS NOT YET CHOSEN A PRIME MINISTER"),
            ("CountrySelectorScreen.cs", "PROVISIONAL · THE SITTING CABINET'S PARTIES ARE NOT IN THE RECORD"),
            ("CountrySelectorScreen.cs", " · IN THE CABINET (PROVISIONAL)"),
            ("PoliticalCompassRenderer.cs", " (PROVISIONAL)"),
            // Board 5b (D11 row 2, 2026-09-02): the People page prints its honesty class ON the instrument in the
            // coalition page's own vocabulary - DERIVED / DECLARED / SOURCED / MEASURED - by Design's ruling; the word
            // SOURCED is player-facing there, with its year, and is not the provenance tag this check hunts.
            ("GameController.cs", "SOURCED · SCB 2014"),
            // §662 (board 20a part C): the dense view's lines lead with their honesty class, in the same vocabulary
            ("GameController.cs", "VOTING AGE {votingAge} SOURCED · CONSTITUTION"),
            ("GameController.cs", "SOURCED - A PUBLISHED SERIES, WITH ITS YEAR"),
            // §662 (19a): PROVISIONAL is the honesty glyph's own word - the registry's fallback and the slip's first line
            ("SymbolRegistry.cs", "PROVISIONAL"),
            // §666 (19b, 20a part B): People's slips print the honesty class in the coalition page's vocabulary, and mark a term as [[TERM]] for its level 2
            ("PeopleSlips.cs", ""),
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
            // §756 (Elias's ruling A5): the statutory minimum wages' instruments - "MiLoG § 1 Abs. 2", "MiLoV5 § 1 Nr. 1", "Dz.U. 2025 poz. 1242, § 1" - the laws' own
            // paragraphs on the dial's slip, allowed by the authority's name beside them
            ("MinimumWageRates.cs", "MiLoG"),
            ("MinimumWageRates.cs", "MiLoV"),
            ("MinimumWageRates.cs", "Dz.U."),
            // AF-8 (ruling q8): the Bundestag's standing orders on who may move the constructive vote - "GO-BT § 97 ABS. 1" - the rule's own paragraph on the
            // Parliament row's slip, allowed by the authority's name beside it (F4-2's precedent)
            ("GameController.ParliamentRows.cs", "GO-BT"),
            ("GameController.ImmigrationPoverty.cs", "SOURCED"),   // P5-C6 (2026-09-06)
            // P4-B1 (2026-09-04): the range-caption catalog is [AUTHORED] game fiction - the desk's own deadpan on every
            // dial's ten bands - and is exempt BY NAME so its satire is not read as meta-text; RangeCaptionCheck holds it
            // to the model's effect signs instead. Nothing else in Assets/Scripts/UI is widened by this row.
            ("RangeCaptions.cs", ""),
        };

        /// <summary>UI-1: an apostrophe-s closing an interpolation hole (marked \u0001 by the possessive lexer), not followed by a letter.</summary>
        private static readonly Regex HolePossessive = new Regex(@"\u0001['’][sS](?!\p{L})");

        /// <summary>UI-1: an apostrophe-s closing a format placeholder ("{0}'s") in a literal that is not interpolated.</summary>
        private static readonly Regex PlaceholderPossessive = new Regex(@"\}['’][sS](?!\p{L})");

        /// <summary>UI-1: a literal that opens with an apostrophe-s, not followed by a letter - a hand-built possessive when it is joined to a value.</summary>
        private static readonly Regex LeadingPossessive = new Regex(@"^['’][sS](?!\p{L})");

        /// <summary>UI-1 (the review's L2): a call whose literals are developer text - Unity's log, an exception's constructor, an assertion, the console. By the
        /// CALL a literal sits in, not by its line: a player-visible call sharing a line with a log is still read, and Math.Log is not a log.</summary>
        private static readonly Regex DiagnosticCallee = new Regex(@"^(?:(?:UnityEngine\.)?Debug\.(?:Log\w*|Assert\w*)|(?:System\.)?\w*Exception|(?:\w+\.)?Assert\.\w+|(?:System\.)?Console\.Write(?:Line)?)$");

        /// <summary>UI-1: a call that joins its arguments into one string - a literal opening with 's passed to it is joined to a value.</summary>
        private static readonly Regex JoiningCallee = new Regex(@"(?:^|\.)(?:Append|Concat|Insert)$");

        /// <summary>UI-1: (file suffix, literal substring, reason) - a possessive on a value that is not a name, allowed by its LITERAL and only while that
        /// literal holds exactly one possessive (the review's L3: a second one added to an allowed literal is a hit).</summary>
        private static readonly (string File, string LiteralContains, string Reason)[] AllowPossessive =
        {
            ("EnergyConnectionQueue.cs", "'S VOLUME, BY CONVENTION)", "a year's volume - a year is not a name"),
            ("PresidentialElection.cs", "{2}'s {3:N0} valid votes", "the gate year's valid votes - a year is not a name"),
            ("UsCongressElection.cs", "{5}'s {6:0.0000}", "the base year's vote - a year is not a name"),
            ("UsPresidentialElection.cs", "{1}'s by the normalised uniform swing", "the prior election year's states - a year is not a name"),
        };

        /// <summary>UI-1: one string literal as the possessive pass reads it - its text with escapes decoded and every interpolation hole marked \u0001, its
        /// line, whether it is interpolated, whether a diagnostic call holds it, the innermost call it is an argument of, and the two code characters
        /// before it (past its $ and @).</summary>
        private struct PossessiveLiteral
        {
            public string Marked;
            public int Line;
            public bool Interpolated;
            public bool Diagnostic;
            public string Callee;
            public char Before;
            public char BeforeThat;
        }

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

            string possessiveReport = PossessivePass(projectRoot, hits);

            var sb = new StringBuilder();
            sb.Append($"=== MetaTextCheck: {files.Count} files, {literals} string literals scanned, {hits.Count} hit(s) ===\n");
            sb.Append(possessiveReport);
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

        /// <summary>UI-1: the possessive pass - the runtime sources scanned, the teeth run, every finding added to <paramref name="hits"/>; returns the report's lines.</summary>
        private static string PossessivePass(string projectRoot, List<string> hits)
        {
            var report = new StringBuilder();
            int files = 0, found = 0, allowed = 0;
            foreach (string file in Directory.GetFiles(Path.Combine(projectRoot, "Assets/Scripts"), "*.cs", SearchOption.AllDirectories))
            {
                string rel = file.Substring(projectRoot.Length + 1).Replace('\\', '/');
                if (IsHarness(rel)) { continue; }   // the film harness stages and logs; its words are not the game's
                if (rel == HelperFile) { continue; }   // the helper is where the possessive is built - once, by its rule
                files++;
                foreach (PossessiveLiteral lit in HandBuiltPossessives(StripComments(SourceText.Read(file))))
                {
                    if (IsAllowedPossessive(rel, lit.Marked)) { allowed++; continue; }
                    found++;
                    hits.Add($"  {rel}:{lit.Line}  [possessive by hand - UiFormat.Possessive]  \"{Truncate(lit.Marked.Replace('\u0001', '#'), 110)}\"");
                }
            }

            // THE TEETH: each planted form's right answer is known, both directions - one per class the pass reads.
            (string Code, bool Hit)[] planted =
            {
                ("class A { string F(string n) => $\"{n}'s cabinet\"; }", true),
                ("class A { string F(string n) => n.ToUpperInvariant() + \"'S GOODS\"; }", true),
                ("class A { string F(string n) => string.Format(\"{0}'s candidate\", n); }", true),
                ("class A { string F(string n) => n +\n    \"\u2019s\"; }", true),
                ("class A { string F(string n) => n + \"\\u2019s\"; }", true),   // the curly form written as an escape in the source
                ("class A { string F(string a, string k, bool c) => $\"{(c ? a : k + \"'s\")} frames\"; }", true),   // nested in a hole
                ("class A { void F(string n) { Debug.Log(\"x\"); Breaks.Add($\"{n}'s cabinet\"); } }", true),   // shares its line with a log
                ("class A { string F(string n) { double v = Math.Log(2.0); return $\"{n}'s\"; } }", true),   // Math.Log is not a log
                ("class A { void F(string s) { s += \"'s\"; } }", true),
                ("class A { void F(StringBuilder sb) { sb.Append(\"'s\"); } }", true),
                ("class A { string F(string n, bool c) => n + (c ? \"'S\" : \"\"); }", true),   // a ternary arm
                ("class A { string F() => \"the government's frames\"; }", false),
                ("class A { string F(string n) => UiFormat.Possessive(n) + \" cabinet\"; }", false),
                ("class A { void F(string n) { Debug.Log($\"{n}'s chamber\"); } }", false),
                ("class A { void F(string n) {\n    Debug.LogError(\n        $\"{n}'s chamber\"); } }", false),   // a log's literal on its continuation line
                ("class A { void F(string n) { throw new ArgumentException($\"{n}'s chamber\"); } }", false),
                ("class A { bool F(string n) => n.IndexOf(\"'s candidate\") >= 0; }", false),
                ("class A { string F() => $\"{{n}}'s\"; }", false),   // escaped braces are text, not a hole
                ("class A { void F(string n, bool c) { Debug.Log($\"{(c ? n + \"'s\" : n)} x\"); } }", false),   // nested in a log's hole
                ("class A { char Q() => '\"'; string F(string n) => n.IndexOf(\"'s\") > 0 ? \"x\" : \"y\"; }", false),   // a char literal holding a quote
            };
            int caught = 0, plantedHits = 0, clean = 0, cleanFlagged = 0;
            foreach ((string code, bool hit) in planted)
            {
                bool got = false;
                foreach (PossessiveLiteral _ in HandBuiltPossessives(code)) { got = true; }
                if (hit) { plantedHits++; if (got) { caught++; } else { hits.Add("  [possessive teeth] a planted hand-built form was NOT caught: " + code); } }
                else { clean++; if (got) { cleanFlagged++; hits.Add("  [possessive teeth] a planted clean form WAS flagged: " + code); } }
            }

            // THE ALLOW-LIST'S TEETH: a row allows its own literal in its own file, and nothing with a second possessive or elsewhere.
            (string File, string Marked, bool Allowed)[] allowCases =
            {
                ("Assets/Scripts/Simulation/EnergyConnectionQueue.cs", "'S VOLUME, BY CONVENTION)", true),
                ("Assets/Scripts/Simulation/EnergyConnectionQueue.cs", "'S VOLUME, BY CONVENTION) OVER \u0001'S", false),
                ("Assets/Scripts/UI/GameController.cs", "'S VOLUME, BY CONVENTION)", false),
            };
            int allowRight = 0;
            foreach ((string file, string marked, bool want) in allowCases)
            {
                if (IsAllowedPossessive(file, marked) == want) { allowRight++; }
                else { hits.Add($"  [possessive teeth] the allow-list answers {!want} for \"{marked.Replace('\u0001', '#')}\" in {file}"); }
            }

            // THE HARNESS EXCLUSION'S TEETH: the film driver does hold hand-built possessives (strings it builds before it logs them), so the exclusion is what keeps them out.
            int harnessHeld = 0;
            string driver = Path.Combine(projectRoot, "Assets/Scripts/Testing/UiScreenshotDriver.cs");
            if (File.Exists(driver)) { foreach (PossessiveLiteral _ in HandBuiltPossessives(StripComments(SourceText.Read(driver)))) { harnessHeld++; } }
            if (harnessHeld == 0) { hits.Add("  [possessive teeth] the film driver holds no hand-built possessive, so the harness exclusion is shown excluding nothing"); }

            // THE HELPER'S BATTERY: the ruling's words, the sentence's case, a party's key, an absent name.
            (string Got, string Want)[] battery =
            {
                (UiFormat.Possessive("UNITED STATES"), "UNITED STATES'"),
                (UiFormat.Possessive("United States"), "United States'"),
                (UiFormat.Possessive("SWEDEN"), "SWEDEN'S"),
                (UiFormat.Possessive("Sweden"), "Sweden's"),
                (UiFormat.Possessive("Harris", false), "Harris'"),
                (UiFormat.Possessive("KD", false, true), "KD's"),
                (UiFormat.Possessive("S", false, true), "S's"),
                (UiFormat.Possessive("S", true, true), "S'S"),
                (UiFormat.Possessive("PiS", false, true), "PiS's"),
                (UiFormat.Possessive("2025", true), "2025'S"),
                (UiFormat.Possessive(null), string.Empty),
                (UiFormat.Possessive(string.Empty, true, true), string.Empty),
            };
            int answered = 0;
            foreach ((string got, string want) in battery)
            {
                if (got == want) { answered++; } else { hits.Add($"  [possessive helper] reads \"{got}\" where \"{want}\" is owed"); }
            }

            report.Append($"  the possessive (UI-1): {files} runtime files, {found} hand-built on a value, {allowed} allowed by literal (a year); "
                + $"planted {caught} of {plantedHits} caught and {cleanFlagged} of {clean} clean flagged; the allow-list {allowRight} of {allowCases.Length}; "
                + $"the harness excluded ({harnessHeld} held there); the helper {answered} of {battery.Length}\n");
            return report.ToString();
        }

        private static bool IsHarness(string rel) => rel.StartsWith("Assets/Scripts/Testing/", StringComparison.Ordinal);

        /// <summary>UI-1: the one file that builds a possessive by hand, by the rule every other site calls.</summary>
        private const string HelperFile = "Assets/Scripts/Data/EnglishText.cs";

        /// <summary>UI-1: a possessive the allow-list holds - its file and its literal named by a row, and the literal holding that one possessive alone.</summary>
        private static bool IsAllowedPossessive(string rel, string marked)
        {
            int count = HolePossessive.Matches(marked).Count + PlaceholderPossessive.Matches(marked).Count + (LeadingPossessive.IsMatch(marked) ? 1 : 0);
            if (count != 1) { return false; }
            foreach ((string f, string contains, string _) in AllowPossessive)
            {
                if (rel.EndsWith(f, StringComparison.Ordinal) && marked.Contains(contains)) { return true; }
            }

            return false;
        }

        /// <summary>UI-1: every literal in <paramref name="code"/> (comments already stripped) that builds a possessive by hand on a value - an apostrophe-s
        /// after a hole or a placeholder, or a literal opening with one and joined to a value (after +, +=, a ternary's ? or :, or as an argument of
        /// Append, Concat or Insert) - outside a diagnostic call.</summary>
        private static IEnumerable<PossessiveLiteral> HandBuiltPossessives(string code)
        {
            var all = new List<PossessiveLiteral>();
            LexPossessive(code, 1, false, all);
            foreach (PossessiveLiteral lit in all)
            {
                if (lit.Diagnostic) { continue; }
                bool hole = lit.Interpolated ? HolePossessive.IsMatch(lit.Marked) : PlaceholderPossessive.IsMatch(lit.Marked);
                bool joined = LeadingPossessive.IsMatch(lit.Marked)
                    && (lit.Before == '+' || (lit.Before == '=' && lit.BeforeThat == '+') || lit.Before == '?' || lit.Before == ':'
                        || ((lit.Before == '(' || lit.Before == ',') && JoiningCallee.IsMatch(lit.Callee)));
                if (hole || joined) { yield return lit; }
            }
        }

        /// <summary>UI-1: the possessive pass's own lexer - string literals (plain, verbatim, interpolated, their escapes decoded), char literals skipped,
        /// and a stack of the calls open at each point (the name before each open parenthesis), so a literal knows the call it is an argument of. An
        /// interpolation hole is code: it is lexed again, inheriting whether a diagnostic call holds it.</summary>
        private static void LexPossessive(string code, int line, bool inDiagnostic, List<PossessiveLiteral> found)
        {
            var calls = new List<string>();
            int i = 0;
            while (i < code.Length)
            {
                char c = code[i];
                if (c == '\n') { line++; i++; continue; }
                if (c == '\'')
                {
                    int q = i + 1;
                    if (q < code.Length && code[q] == '\\') { q++; }
                    q++;
                    while (q < code.Length && code[q] != '\'' && code[q] != '\n') { q++; }
                    i = q + 1;
                    continue;
                }

                if (c == '(') { calls.Add(CalleeBefore(code, i)); i++; continue; }
                if (c == ')') { if (calls.Count > 0) { calls.RemoveAt(calls.Count - 1); } i++; continue; }
                if (c != '"') { i++; continue; }

                int p = i - 1;
                bool verbatim = false, interpolated = false;
                while (p >= 0 && (code[p] == '$' || code[p] == '@')) { if (code[p] == '$') { interpolated = true; } else { verbatim = true; } p--; }
                while (p >= 0 && char.IsWhiteSpace(code[p])) { p--; }
                char before = p >= 0 ? code[p] : '\0';
                char beforeThat = p >= 1 ? code[p - 1] : '\0';

                var marked = new StringBuilder();
                var holes = new List<string>();
                int start = line;
                int j = i + 1;
                while (j < code.Length)
                {
                    char d = code[j];
                    if (verbatim && d == '"' && j + 1 < code.Length && code[j + 1] == '"') { marked.Append('"'); j += 2; continue; }
                    if (!verbatim && d == '\\' && j + 1 < code.Length)
                    {
                        if (code[j + 1] == 'u' && j + 5 < code.Length && int.TryParse(code.Substring(j + 2, 4), System.Globalization.NumberStyles.HexNumber, null, out int unit))
                        {
                            marked.Append((char)unit);
                            j += 6;
                            continue;
                        }
                        marked.Append(code[j + 1]);
                        j += 2;
                        continue;
                    }
                    if (interpolated && (d == '{' || d == '}') && j + 1 < code.Length && code[j + 1] == d) { marked.Append(d); j += 2; continue; }
                    if (interpolated && d == '{')
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

                        holes.Add(code.Substring(j + 1, Math.Max(0, Math.Min(k, code.Length) - j - 1)));
                        marked.Append('\u0001');
                        j = k + 1;
                        continue;
                    }

                    if (d == '"') { break; }
                    if (d == '\n') { line++; }
                    marked.Append(d);
                    j++;
                }

                bool diagnostic = inDiagnostic;
                foreach (string call in calls) { if (DiagnosticCallee.IsMatch(call)) { diagnostic = true; break; } }
                found.Add(new PossessiveLiteral
                {
                    Marked = marked.ToString(), Line = start, Interpolated = interpolated, Diagnostic = diagnostic,
                    Callee = calls.Count > 0 ? calls[calls.Count - 1] : string.Empty, Before = before, BeforeThat = beforeThat,
                });
                foreach (string hole in holes) { LexPossessive(hole, start, diagnostic, found); }
                i = j + 1;
            }
        }

        /// <summary>UI-1: the dotted name before an open parenthesis ("sb.Append", "Debug.Log", "ArgumentException"); empty where none stands there.</summary>
        private static string CalleeBefore(string code, int open)
        {
            int e = open - 1;
            while (e >= 0 && char.IsWhiteSpace(code[e])) { e--; }
            int b = e;
            while (b >= 0 && (char.IsLetterOrDigit(code[b]) || code[b] == '_' || code[b] == '.')) { b--; }
            return e > b ? code.Substring(b + 1, e - b) : string.Empty;
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
