using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using PoliSim.Data;
using PoliSim.Elections;
using PoliSim.UI;
using Debug = UnityEngine.Debug;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// The review's M4 (Reviews/2026-10-29_portfolioname_review.md), the sibling of <see cref="PortfolioDisplayNameCheck"/>: no player-visible text prints
    /// a tax's, a welfare programme's, a spending line's, a formation outcome's, a system area's or a portfolio's raw identifier ("IncomeTax at 25.0 %", "Implement
    /// NegativeIncomeTax", "(NewElection)", "ISSUE · CrimeJustice"). Each of those enums has one scoped lookup (<see cref="EnumNames"/>, and
    /// <see cref="UiPalette.AreaName"/> for the UI's own area enum, <see cref="Effectiveness.DisplayName"/> for the portfolios, whose own check reads only
    /// interpolations - this one sees the portfolio joined to prose by +, review M3's case), and this check holds the runtime to it.
    ///
    /// <para><b>What it decides.</b> (1) THE NAMES: every member of every guarded enum displays as words (no lower-case letter followed by a capital).
    /// (2) ONE SET OF NAMES: the UI's <c>DisplayName.Of</c> returns the model's name for every tax, welfare and spending member, and every node of the
    /// Policy Web that shares a member's name carries the same name - the two read one table. (3) THE KEY: a programme bill's division title is the
    /// identifier it always was, byte for byte against the old interpolation (it is the veto's and the US chambers' draw key), and
    /// <see cref="EnumNames.ShownTitle"/> prints the programme's words in its place, inside a veto's title too, and leaves any other title as it stands.
    /// (4) THE SCAN: every runtime file under Assets/Scripts but the film harness (Testing/), comments stripped (<see cref="SourceText.ReadWithoutComments"/>),
    /// read for a guarded enum written raw into text - an interpolation hole, a <c>string.Format</c> argument, a prose literal joined to it with
    /// <c>+</c>, or its <c>ToString()</c> handed straight to a text call - outside a diagnostic call (a log, an assertion, an exception; judged by the
    /// call that holds the literal, not by the line). An expression's type is read without a compiler: the nearest declaration above it in the file
    /// names the identifier's type, and the runtime assembly's own fields and properties (by reflection) type each member after it. (5) THE TEETH:
    /// planted sources pushed through the same scan, every run - each raw form caught, each fixed form, key and log passed. (6) THE PARTY KEYS (§575's
    /// rule, the review's follow-up): where a player reads a party - the round's log, a proposal's, answer's, formation view's or projection's reason,
    /// a formation's no-government reason, the night's government line - no party KEY is printed raw (a hole, a + join, string.Join, a possessive or a
    /// format); a key is typed by the members that hold one, each proved a string or a collection of strings by reflection, and by a string local
    /// initialised from one; its own teeth every run.</para>
    /// </summary>
    public static class EnumDisplayNameCheck
    {
        private static readonly Type[] Guarded = { typeof(TaxType), typeof(WelfareProgramType), typeof(SpendingCategory), typeof(CoalitionOutcomeKind), typeof(UiPalette.SystemArea), typeof(CabinetPortfolio) };

        /// <summary>A guarded value's one lookup.</summary>
        private static string NameOf(object value)
        {
            switch (value)
            {
                case TaxType t: return EnumNames.Of(t);
                case WelfareProgramType w: return EnumNames.Of(w);
                case SpendingCategory s: return EnumNames.Of(s);
                case CoalitionOutcomeKind k: return EnumNames.Of(k);
                case UiPalette.SystemArea a: return UiPalette.AreaName(a);
                case CabinetPortfolio p: return Effectiveness.DisplayName(p);
                default: return null;
            }
        }

        /// <summary>A raw write the scan reports that is not one, each with its reason: (the file's path ending, a substring of the line, the reason).</summary>
        private static readonly (string File, string Contains, string Reason)[] Allowed = new (string, string, string)[0];

        private static readonly Regex Joined = new Regex(@"[a-z][A-Z]", RegexOptions.Compiled);
        private static readonly Regex Declaration = new Regex(@"\b([A-Z]\w*)(?:<[^<>;()=]*(?:<[^<>;()=]*>)?[^<>;()=]*>)?(?:\[\])?\??\s+(@?[a-z_]\w*)\s*(?=[=;,)]|\bin\b)", RegexOptions.Compiled);
        private static readonly Regex KeyValue = new Regex(@"KeyValuePair<\s*(?:\w+\.)*(\w+)\??\s*,\s*(?:\w+\.)*(\w+)(?:<[^<>]*>)?\??\s*>\s+(@?[a-z_]\w*)", RegexOptions.Compiled);
        private static readonly Regex Chain = new Regex(@"^@?[A-Za-z_]\w*(?:\.[A-Za-z_]\w*)*$", RegexOptions.Compiled);
        private static readonly Regex Diagnostic = new Regex(@"(?:\bDebug\.Log(?:Warning|Error|Format|Exception)?|\bSpeakerLog|\bAssert\.\w+|\bnew\s+\w*Exception)\s*\(", RegexOptions.Compiled);
        private static readonly Regex Throw = new Regex(@"\bthrow\b", RegexOptions.Compiled);
        private static readonly Regex FormatCall = new Regex(@"\b(?:string\.Format|Pf)\(", RegexOptions.Compiled);
        private static readonly Regex ProseThenValue = new Regex("\"((?:[^\"\\\\]|\\\\.)*)\"\\s*\\+\\s*(@?[A-Za-z_][\\w.]*(?:\\.ToString\\(\\))?)(?![\\w.(])", RegexOptions.Compiled);
        private static readonly Regex ValueThenProse = new Regex("(?<![\\w.)\\]])(@?[A-Za-z_][\\w.]*(?:\\.ToString\\(\\))?)\\s*\\+\\s*\"((?:[^\"\\\\]|\\\\.)*)\"", RegexOptions.Compiled);
        private static readonly Regex TextCall = new Regex(@"\.(?:Add|Append|Label|MeasuredLabel|MakeText|MakeTextRealWeight)\(\s*(@?[A-Za-z_][\w.]*)\.ToString\(\)\s*[,)]", RegexOptions.Compiled);

        private static readonly string[] Caught =
        {
            "foreach (TaxProgramBill bill in bills) { pending.Add(($\"{(bill.IsAdd ? \"Implement\" : \"Remove\")} {bill.Type} - resolves.\", 0)); }",
            "foreach (KeyValuePair<TaxType, float> kv in bill.TaxLines) { parts.Add($\"{kv.Key} at {kv.Value:0.0} %\"); }",
            "CoalitionResult result = Run();\nstring reason = $\"no government could be formed from this chamber ({result.Outcome})\";",
            "foreach (KeyValuePair<SpendingCategory, float> kv in bill.SpendingNominalTargets) { return string.Format(CultureInfo.InvariantCulture, \"{0} SET AT {1:0.###}\", kv.Key, kv.Value); }",
            "UiPalette.SystemArea area = UiPalette.SystemArea.CrimeJustice;\nvar slip = new SlipContent(\"ISSUE\").Add(area.ToString());",
            "WelfareProgramBill bill = b;\nApprovalLedgerRecorder.RecordEvent(country, date, $\"Implement {bill.Type} bill passed\", d);",
            "GovernmentFormation.View view = v;\nstring s = $\"outcome {view.Outcome}, {view.Outcome.ToString()}\";",
            "string s = $\"the {TaxType.IncomeTax} line\";",
            "CoalitionResult cabinetResult = r;\nDebug.Log(\"formed\"); label = $\"{cabinetResult.Outcome}\";",
            "SpendingCategory? category = c;\nstring line = \"cuts \" + category.Value + \" by half\";", "foreach (CabinetPortfolio post in posts) { if (!seen.Add(post)) { verdict.Reason = \"a portfolio is offered twice: \" + post; } }",
        };
        private static readonly string[] Passed =
        {
            "foreach (KeyValuePair<TaxType, float> kv in bill.TaxLines) { parts.Add($\"{EnumNames.Of(kv.Key)} at {kv.Value:0.0} %\"); }",
            "TaxLine taxLine = t;\nstring id = \"tax:\" + taxLine.Type;",
            "TaxProgramBill bill = b;\nDebug.Log($\"BUDGET: {bill.Type} tabled\");",
            "UiPalette.SystemArea area = a;\nvar slip = new SlipContent(\"ISSUE\").Add(UiPalette.AreaName(area));",
            "Sector sector = s;\nstring label = $\"the {sector.Type} sector\";",
            "TaxProgramBill bill = b;\nvetoable = ParliamentSystem.Divide(country, EnumNames.ProgrammeBillKey(bill.Type, bill.IsAdd), concern, date, out passed);",
            "CoalitionResult result = r;\nthrow new InvalidOperationException($\"no outcome {result.Outcome}\");", "foreach (CabinetPortfolio post in posts) { verdict.Reason = \"a portfolio is offered twice: \" + Effectiveness.DisplayName(post); string key = \"cabinet:\" + post; }",
        };

        public static void Run()
        {
            CheckExit.ArmLogFold();
            List<string> failures = Evaluate(out string report);
            if (failures.Count == 0) { Debug.Log(report); } else { Debug.LogError(report); }
            CheckExit.Finish(failures.Count == 0 ? 0 : 1);
        }

        /// <summary>The whole verdict without the engine (no log, no exit): the failures, and the report the run prints.</summary>
        public static List<string> Evaluate(out string report)
        {
            var failures = new List<string>();
            var sb = new StringBuilder();
            sb.Append("=== EnumDisplayNameCheck: no player-visible text prints a tax's, programme's, spending line's, formation outcome's, area's or portfolio's raw identifier ===\n");
            TypeMap map = TypeMap.Of(typeof(EnumNames).Assembly);

            // (5) THE TEETH, first: a scan that cannot see a planted raw write proves nothing about the tree
            int caught = 0, passed = 0;
            foreach (string plant in Caught) { if (Scan(plant, map).Count > 0) { caught++; } else { failures.Add($"the teeth: the scan missed the planted raw write `{plant.Replace('\n', ' ')}`"); } }
            foreach (string plant in Passed)
            {
                List<string> found = Scan(plant, map);
                if (found.Count == 0) { passed++; } else { failures.Add($"the teeth: the scan flagged the planted clean form `{plant.Replace('\n', ' ')}` ({found[0]})"); }
            }
            sb.Append($"  the teeth: {caught} of {Caught.Length} planted raw write(s) caught, {passed} of {Passed.Length} planted clean form(s) passed\n");

            // (1) THE NAMES
            int names = 0;
            foreach (Type e in Guarded)
            {
                foreach (object v in Enum.GetValues(e))
                {
                    string name = NameOf(v);
                    if (string.IsNullOrEmpty(name) || Joined.IsMatch(name)) { failures.Add($"the names: {e.Name}.{v} displays as '{name}', a joined identifier"); } else { names++; }
                }
            }
            sb.Append($"  the names: {names} member(s) of {Guarded.Length} enum(s) display as words\n");

            // (2) ONE SET OF NAMES
            int agree = 0, nodes = 0;
            foreach (Type e in new[] { typeof(TaxType), typeof(WelfareProgramType), typeof(SpendingCategory) })
            {
                foreach (object v in Enum.GetValues(e))
                {
                    string model = NameOf(v), ui = DisplayName.Of(v.ToString());
                    if (model != ui) { failures.Add($"one set of names: {e.Name}.{v} reads '{model}' in the model and '{ui}' through DisplayName.Of"); } else { agree++; }
                    if (Enum.TryParse(v.ToString(), out PolicyNodeId node) && PolicyWebRenderer.TryGetPolicyName(node, out string curated))
                    {
                        nodes++;
                        if (curated != model) { failures.Add($"one set of names: the Policy Web's node {node} reads '{curated}', the model's {e.Name}.{v} '{model}'"); }
                    }
                }
            }
            sb.Append($"  one set of names: {agree} member(s) read alike in the model and the UI; {nodes} Policy Web node(s) checked against the model's name\n");

            // (3) THE KEY
            int keys = 0;
            foreach (bool add in new[] { true, false })
            {
                string verb = add ? "Implement" : "Remove";
                foreach (TaxType t in Enum.GetValues(typeof(TaxType))) { keys += KeyHolds(EnumNames.ProgrammeBillKey(t, add), $"{verb} {t}", EnumNames.ProgrammeBillWords(t, add), failures); }
                foreach (WelfareProgramType w in Enum.GetValues(typeof(WelfareProgramType))) { keys += KeyHolds(EnumNames.ProgrammeBillKey(w, add), $"{verb} {w}", EnumNames.ProgrammeBillWords(w, add), failures); }
            }
            foreach (string other in new[] { "Annual budget bill", "Tax act: VAT 23 % to 22 %", "Enact: Cash Bail Reform Act", "Implementation of the budget rules" })
            {
                if (EnumNames.ShownTitle(other) != other) { failures.Add($"the key: ShownTitle rewrote a title that holds no programme key - '{other}' read '{EnumNames.ShownTitle(other)}'"); }
            }
            sb.Append($"  the key: {keys} programme bill title(s) byte-identical to the old interpolation and shown in words, alone and inside a veto's title; titles without a key left as they stand\n");

            // (6) THE PARTY KEYS: the members the scan types a key by, each proved a string or a collection of strings, and its teeth
            int keyMembers = 0, keyCaught = 0, keyPassed = 0;
            const BindingFlags anyMember = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
            foreach ((Type owner, string member, int kind) in KeyMembers)
            {
                Type t = owner.GetField(member, anyMember)?.FieldType ?? owner.GetProperty(member, anyMember)?.PropertyType;
                bool typed = t != null && (kind == Scalar ? t == typeof(string)
                    : kind == KeyList ? typeof(IEnumerable<string>).IsAssignableFrom(t)
                    : typeof(System.Collections.IDictionary).IsAssignableFrom(t) && t.IsGenericType && t.GetGenericArguments()[0] == typeof(string));
                string want = kind == Scalar ? "string" : kind == KeyList ? "collection of strings" : "dictionary keyed by strings";
                if (typed) { keyMembers++; } else { failures.Add($"the party keys: {owner.Name}.{member} is not a {want} on its type - the scan would read nothing through it"); }
            }
            // K312 (2026-10-29): a list of short names joins with "+", or " · " where a name holds a '+'
            int joins = 0;
            foreach ((string[] joined, string join, string expect) in new[]
            {
                (new[] { "PD", "AVS", "+E", "IC" }, "+", "PD · AVS · +E · IC"), (new[] { "M", "KD", "L" }, "+", "M+KD+L"),
                (new[] { "S", "MP" }, " + ", "S + MP"), (new[] { "Az-IV", "+E" }, " + ", "Az-IV · +E"),
            })
            {
                string got = PartySystems.JoinShortNames(joined, join);
                if (got == expect) { joins++; } else { failures.Add($"K312: [{string.Join(", ", joined)}] joined '{got}', not '{expect}'"); }
            }
            string italy = FormationProjection.Join(CountryId.Italy, new List<string> { "PD", "AVS", "PlusE", "IC" });
            if (italy == "PD · AVS · +E · IC") { joins++; } else { failures.Add($"K312: Italy's centre-left by its keys joins '{italy}', not 'PD · AVS · +E · IC' - the short names or the join"); }
            int joinChecks = 5;
            // S10 (Design's return of 2026-10-30, 10a's table): the groups as the boards print them, by their keys through the sites that name them - the
            // formation's join, and the Sejm night's (`SejmNightSteps.Names`: the foot, the steps' † lines, the Docket's titles and slips)
            foreach ((string what, string got, string expect) in new[]
            {
                ("Italy's centre-right", FormationProjection.Join(CountryId.Italy, new List<string> { "FdI", "Lega", "FI", "NM" }), "FdI+Lega+FI+NM"),
                ("Italy's example pair", FormationProjection.Join(CountryId.Italy, new List<string> { "AzIV", "M5S" }), "Az-IV+M5S"),
                ("Poland's cabinet on the Sejm night", SejmNightSteps.Names(new List<string> { "KO", "TD", "NL" }), "KO+TD+NL"),
            })
            {
                joinChecks++;
                if (got == expect) { joins++; } else { failures.Add($"K312 (10a): {what} joins '{got}', not '{expect}' as Design's table prints it"); }
            }
            foreach (string plant in KeyCaught) { if (ScanKeys(plant, map).Count > 0) { keyCaught++; } else { failures.Add($"the party keys' teeth: the scan missed the planted raw key `{plant.Replace('\n', ' ')}`"); } }
            foreach (string plant in KeyPassed)
            {
                List<string> found = ScanKeys(plant, map);
                if (found.Count == 0) { keyPassed++; } else { failures.Add($"the party keys' teeth: the scan flagged the planted clean form `{plant.Replace('\n', ' ')}` ({found[0]})"); }
            }

            // (4) THE SCAN
            int files = 0, hits = 0, allowed = 0, keyHits = 0;
            if (!Directory.Exists("Assets/Scripts")) { failures.Add("the scan: no Assets/Scripts below the working directory - nothing was read"); }
            else
            {
                foreach (string path in Directory.GetFiles("Assets/Scripts", "*.cs", SearchOption.AllDirectories))
                {
                    string rel = path.Replace('\\', '/');
                    if (rel.Contains("/Testing/")) { continue; }   // the film and test harness: its strings are the log's, never the player's
                    files++;
                    string text = SourceText.ReadWithoutComments(path);
                    foreach (string hit in Scan(text, map))
                    {
                        string line = hit.Substring(hit.IndexOf(' ') + 1);
                        bool allow = false;
                        foreach ((string file, string contains, string _) in Allowed) { if (rel.EndsWith(file, StringComparison.Ordinal) && line.Contains(contains)) { allow = true; } }
                        if (allow) { allowed++; continue; }
                        hits++;
                        failures.Add($"the scan: {rel}:{hit}");
                    }
                    foreach (string hit in ScanKeys(text, map)) { keyHits++; failures.Add($"the party keys: {rel}:{hit}"); }
                }
                if (files == 0) { failures.Add("the scan: Assets/Scripts holds no runtime source - nothing was read"); }
            }
            sb.Append($"  the scan: {files} runtime file(s), {hits} raw write(s), {allowed} allowed - its limits: a hole, a Format argument, a prose literal joined by +, or ToString() into Add/Append/Label/MeasuredLabel/MakeText; ")
              .Append("an identifier typed by its nearest declaration above (a `var`, an untyped lambda parameter or a field declared in another file is not typed), a member typed by reflection (a simple type name shared by two types reads as either); ")
              .Append("a value carried through a string local first, a method's return, a concatenation inside a hole, a literal spanning lines and the film harness are not seen; DisplayName.Of on a tax, welfare or spending identifier is the UI's route to the same table and is not judged; ")
              .Append("single-word enums written raw (the minister's and the Fed chair's philosophy, IssueId, ScandalSeverity, AgreementState, RevisionStatus, LawVerb, the Italian chamber) read as words and are not guarded - a joined member added to one would print raw\n");
            sb.Append($"  the party keys: {keyMembers} of {KeyMembers.Length} key member(s) typed by reflection; K312's join {joins} of {joinChecks}; teeth {keyCaught} of {KeyCaught.Length} planted raw key(s) caught, {keyPassed} of {KeyPassed.Length} clean form(s) passed; {keyHits} raw key(s) or hand join(s) in player text - ")
              .Append("its limits: the sinks named (the round's log, a proposal's, answer's, formation view's or projection's reason, a formation's no-government reason, the night's government line); a key typed by a member of the table above, a string local initialised from one, ")
              .Append("a foreach variable over a key list, or an entry's Key over a dictionary the table keys by party; a key handed to another helper (CandidateOf, a caller's own) is not followed, a foreach over a list built in a local and a sink text built in a local first are not typed; ")
              .Append("a hand join (\"+\", \" + \" or, since S10, \" · \") is seen only where its list visibly is short names (Shorts, ShortName, ConvertAll(Name), ConvertAll(Short)) or is a local list every Add of which is one short-name call (ShortName, Short); ")
              .Append("a list of name-and-figure pairs, a caption's fragments and a comma list in prose are not groups and are not judged; the helper handed a separator other than its own \"+\" is a hand join (S10: the Sejm night forced \" · \"); ")
              .Append("a log a player never sees (the Sejm formation's log, a government's breaks) is no sink and keeps the key\n");

            foreach (string f in failures) { sb.Append("  ⚠ FAULT  ").Append(f).Append('\n'); }
            sb.Append($"\n=== EnumDisplayNameCheck: {(failures.Count == 0 ? "CLEAN" : failures.Count + " fault(s)")} ===\n");
            report = sb.ToString();
            return failures;
        }

        private static int KeyHolds(string key, string old, string words, List<string> failures)
        {
            if (key != old) { failures.Add($"the key: ProgrammeBillKey reads '{key}' where the division's title was '{old}' - every draw keyed on it would move"); return 0; }
            string vetoed = "Vetoed by the President (Andrzej Duda): " + key + " - the veto stands, 248 for, 276 needed";
            if (EnumNames.ShownTitle(key) != words) { failures.Add($"the key: ShownTitle('{key}') reads '{EnumNames.ShownTitle(key)}', not '{words}'"); return 0; }
            if (EnumNames.ShownTitle(vetoed) != vetoed.Replace(key, words)) { failures.Add($"the key: ShownTitle left the key inside a veto's title - '{EnumNames.ShownTitle(vetoed)}'"); return 0; }
            return 1;
        }

        /// <summary>The scan of one source text (comments already stripped): each raw write as "line: what - the line".</summary>
        internal static List<string> Scan(string text, TypeMap map)
        {
            var hits = new List<string>();
            List<(int Pos, string Ident, string Type)> decls = Decls(CodeView(text), map);

            string[] lines = text.Split('\n');
            int offset = 0;
            for (int i = 0; i < lines.Length; offset += lines[i].Length + 1, i++)
            {
                string line = lines[i];
                if (line.IndexOf('"') < 0) { continue; }
                int at = offset;
                string Hit(string what) => $"{i + 1}: {what} - {line.Trim()}";

                foreach ((int start, List<string> holes) in Interpolated(line))
                {
                    if (InsideDiagnostic(line, start)) { continue; }
                    foreach (string hole in holes)
                    {
                        Type e = Resolve(Clip(hole), at + start, decls, map);
                        if (e != null) { hits.Add(Hit($"a {e.Name} interpolated raw ({{{hole.Trim()}}})")); }
                    }
                }
                foreach (Match m in FormatCall.Matches(line))
                {
                    if (InsideDiagnostic(line, m.Index)) { continue; }
                    List<string> args = Arguments(line, m.Index + m.Length);
                    int first = 0;
                    while (first < args.Count && !IsLiteral(args[first])) { first++; }
                    for (int a = first + 1; a < args.Count; a++)
                    {
                        Type e = Resolve(Clip(args[a]), at + m.Index, decls, map);
                        if (e != null) { hits.Add(Hit($"a {e.Name} handed raw to a format ({args[a].Trim()})")); }
                    }
                }
                foreach (Match m in ProseThenValue.Matches(line))
                {
                    if (m.Groups[1].Value.IndexOf(' ') < 0 || InsideDiagnostic(line, m.Index)) { continue; }
                    Type e = Resolve(Strip(m.Groups[2].Value), at + m.Index, decls, map);
                    if (e != null) { hits.Add(Hit($"a {e.Name} joined raw to prose ({m.Groups[2].Value})")); }
                }
                foreach (Match m in ValueThenProse.Matches(line))
                {
                    if (m.Groups[2].Value.IndexOf(' ') < 0 || InsideDiagnostic(line, m.Index)) { continue; }
                    Type e = Resolve(Strip(m.Groups[1].Value), at + m.Index, decls, map);
                    if (e != null) { hits.Add(Hit($"a {e.Name} joined raw to prose ({m.Groups[1].Value})")); }
                }
                foreach (Match m in TextCall.Matches(line))
                {
                    if (InsideDiagnostic(line, m.Index)) { continue; }
                    Type e = Resolve(m.Groups[1].Value, at + m.Index, decls, map);
                    if (e != null) { hits.Add(Hit($"a {e.Name}'s ToString() handed to a text call ({m.Groups[1].Value})")); }
                }
            }
            return hits;
        }

        /// <summary>The guarded enum an expression is, or null: an identifier typed by its nearest declaration above <paramref name="pos"/>, each member
        /// after it typed by reflection; a guarded enum's own member literal (<c>TaxType.IncomeTax</c>).</summary>
        private static Type Resolve(string expr, int pos, List<(int Pos, string Ident, string Type)> decls, TypeMap map)
        {
            if (string.IsNullOrEmpty(expr) || !Chain.IsMatch(expr)) { return null; }
            string[] parts = expr.TrimStart('@').Split('.');
            if (parts.Length >= 2)
            {
                foreach (Type g in Guarded) { if (parts[parts.Length - 2] == g.Name && Enum.IsDefined(g, parts[parts.Length - 1])) { return g; } }
            }
            string type = TypeOfChain(parts, parts.Length, pos, decls, map);
            return type == null ? null : GuardedNamed(type);
        }

        /// <summary>The simple type name of the chain's first <paramref name="count"/> parts, or null: the identifier by its nearest declaration above
        /// <paramref name="pos"/>, each member after it by reflection; a KeyValuePair's Key and Value by its declaration, a nullable's Value as itself.</summary>
        private static string TypeOfChain(string[] parts, int count, int pos, List<(int Pos, string Ident, string Type)> decls, TypeMap map)
        {
            string type = null;
            string first = parts[0].TrimStart('@');
            for (int d = decls.Count - 1; d >= 0; d--) { if (decls[d].Pos < pos && decls[d].Ident.TrimStart('@') == first) { type = decls[d].Type; break; } }
            for (int k = 1; k < count && type != null; k++)
            {
                if (type.StartsWith("KVP:", StringComparison.Ordinal))
                {
                    string[] kv = type.Substring(4).Split(',');
                    type = parts[k] == "Key" ? kv[0] : parts[k] == "Value" ? kv[1] : null;
                }
                else if (parts[k] == "Value" && !map.Declares(type, "Value")) { }   // a nullable's .Value is the value itself
                else { type = map.MemberType(type, parts[k]); }
            }
            return type;
        }

        /// <summary>The declarations a scan types identifiers by, in the order they stand (read off the literal-blanked text): a typed local, parameter,
        /// field or foreach variable; a KeyValuePair's two types; <c>var x = new T(</c>; and a string local initialised from a party key, which is a key itself.</summary>
        private static List<(int Pos, string Ident, string Type)> Decls(string code, TypeMap map)
        {
            var decls = new List<(int Pos, string Ident, string Type)>();
            foreach (Match m in KeyValue.Matches(code)) { decls.Add((m.Index, m.Groups[3].Value, "KVP:" + m.Groups[1].Value + "," + m.Groups[2].Value)); }
            foreach (Match m in Declaration.Matches(code)) { if (m.Groups[1].Value != "KeyValuePair") { decls.Add((m.Index, m.Groups[2].Value, m.Groups[1].Value)); } }
            foreach (Match m in VarNew.Matches(code)) { decls.Add((m.Index, m.Groups[1].Value, m.Groups[2].Value)); }
            decls.Sort((a, b) => a.Pos.CompareTo(b.Pos));
            var keyLocals = new List<(int Pos, string Ident, string Type)>();
            foreach (Match m in KeyLocal.Matches(code)) { if (KeyKind(m.Groups[2].Value, m.Index, decls, map) == 1) { keyLocals.Add((m.Index, m.Groups[1].Value, KeyType)); } }
            // the review's L2: a foreach variable over a key list is a key, and one over a dictionary keyed by party is an entry whose Key is a key - typed
            // at the variable itself, after the loop's own declaration of it, so it wins the nearest-declaration lookup
            foreach (Match m in ForeachKey.Matches(code)) { if (KeyKind(m.Groups[2].Value, m.Index, decls, map) == 2) { keyLocals.Add((m.Groups[1].Index, m.Groups[1].Value, KeyType)); } }
            foreach (Match m in ForeachEntry.Matches(code)) { if (KeyKind(m.Groups[2].Value, m.Index, decls, map) == 3) { keyLocals.Add((m.Groups[1].Index, m.Groups[1].Value, KeyEntryType)); } }
            if (keyLocals.Count > 0) { decls.AddRange(keyLocals); decls.Sort((a, b) => a.Pos.CompareTo(b.Pos)); }
            return decls;
        }

        // ------------------------------------------------------------ (6) THE PARTY KEYS: §575's rule where the scan can type it
        private const string KeyType = "<party key>";
        private const string KeyEntryType = "<party-keyed entry>";
        private const int Scalar = 1, KeyList = 2, KeyedDictionary = 3;

        /// <summary>The members that hold a party's KEY (the persisted ASCII abbreviation; a player reads the short name), each proved by reflection, every
        /// run, to be a string, a collection of strings, or a dictionary keyed by strings on its type - a renamed member fails the check rather than
        /// blinding the scan.</summary>
        private static readonly (Type Owner, string Member, int Kind)[] KeyMembers =
        {
            (typeof(Country), "PlayerPartyAbbrev", Scalar), (typeof(PoliticalParty), "Abbrev", Scalar), (typeof(DivisionSide), "Abbrev", Scalar),
            (typeof(GovernmentFormation.VoteSide), "Abbrev", Scalar), (typeof(PartyAnswer), "Party", Scalar),
            (typeof(FormationProposal), "Formateur", Scalar), (typeof(FormationProposal), "CabinetParties", KeyList), (typeof(FormationProposal), "Supporters", KeyList),
            (typeof(SpeakerRound), "Asked", Scalar), (typeof(SpeakerRound), "Order", KeyList),
            (typeof(GovernmentRecord), "PmParty", Scalar), (typeof(GovernmentRecord), "Cabinet", KeyList), (typeof(GovernmentRecord), "Support", KeyList),
            (typeof(WorldClock.GovernmentOfRecord), "Cabinet", KeyList), (typeof(WorldClock.GovernmentOfRecord), "Support", KeyList),
            (typeof(FormationProposal), "Posts", KeyedDictionary), (typeof(Country), "ParliamentSeats", KeyedDictionary),
        };
        private static readonly Regex ForeachKey = new Regex(@"\bforeach\s*\(\s*(?:string|var)\s+(@?[a-z_]\w*)\s+in\s+(@?[A-Za-z_][\w.]*)\s*\)", RegexOptions.Compiled);
        private static readonly Regex ForeachEntry = new Regex(@"\bforeach\s*\(\s*(?:KeyValuePair<[^()]*?>|var)\s+(@?[a-z_]\w*)\s+in\s+(@?[A-Za-z_][\w.]*)\s*\)", RegexOptions.Compiled);
        private static readonly Regex HandJoin = new Regex(@"\bstring\.Join\(", RegexOptions.Compiled);

        /// <summary>Where a player reads a party - the texts the scan holds to short names: the Speaker's and the chancellor's round's log (the Parliament
        /// page's slip), a proposal's, an answer's, a formation view's and a projection's reason (the sheet, the night, the verdicts), a formation's
        /// no-government reason, and the night's government line. A log a player never sees (the Sejm formation's log, a government's breaks, Debug.Log)
        /// keeps the key and is no sink.</summary>
        private static readonly Regex LogSink = new Regex(@"(@?[A-Za-z_][\w.]*)\.Log\.Add\(", RegexOptions.Compiled);
        private static readonly Regex ReasonSink = new Regex(@"(@?[A-Za-z_][\w.]*)\.Reason\s*=(?!=)", RegexOptions.Compiled);
        private static readonly Regex LineSink = new Regex(@"\.GovernmentLine\s*=(?!=)", RegexOptions.Compiled);
        private static readonly Regex NoneSink = new Regex(@"\bFormed\.None\(", RegexOptions.Compiled);
        private static readonly string[] ReasonOwners = { "ProposalVerdict", "PartyAnswer", "View", "FormationProjection" };
        private static readonly Regex ChainToken = new Regex(@"(?<![\w.@])@?[A-Za-z_]\w*(?:\s*\??\.\s*[A-Za-z_]\w*)*", RegexOptions.Compiled);
        private static readonly Regex VarNew = new Regex(@"\bvar\s+(@?[a-z_]\w*)\s*=\s*new\s+(?:\w+\.)*([A-Z]\w*)\b", RegexOptions.Compiled);
        private static readonly Regex KeyLocal = new Regex(@"\bstring\s+(@?[a-z_]\w*)\s*=\s*(@?[A-Za-z_][\w.]*)\s*;", RegexOptions.Compiled);
        private static readonly string[] Grouping = { "if", "while", "for", "foreach", "switch", "return", "catch", "when", "using", "lock", "in", "else", "case" };

        private static readonly string[] KeyCaught =
        {
            "Elections.SpeakerRound round = r;\nFormationProposal proposal = p;\nround.Log.Add($\"{d}: {proposal.Formateur} forms {string.Join(\"+\", proposal.CabinetParties)}\");",
            "SpeakerRound round = r;\nCountry country = c;\nround.Log.Add($\"{d}: {country.PlayerPartyAbbrev} does not form a government\");",
            "SpeakerRound round = r;\nround.Log.Add(flag\n    ? $\"{d}: the Speaker asks {round.Asked} to form a government\"\n    : \"none\");",
            "Country country = c;\nstring player = country.PlayerPartyAbbrev;\nSpeakerRound round = r;\nround.Log.Add($\"{d}: {player}, the player's party, sits in it\");",
            "GovernmentRecord g = x;\ng.Round.Log.Add($\"{d}: the round ends - {g.PmParty} resigns\");",
            "WorldClock.GovernmentOfRecord? after = a;\nr.GovernmentLine = head + \" · IN CABINET \" + string.Join(\"+\", after.Value.Cabinet);",
            "SpeakerRound round = r;\nround.Log.Add($\"{d}: Sweden accepts {PoliSim.Wording.EnglishText.Possessive(round.Asked, false, true)} offer\");",
            "var verdict = new ProposalVerdict();\nFormationProposal proposal = p;\nverdict.Reason = \"a post is offered to a party outside the cabinet: \" + proposal.Formateur;",
            "Country country = c;\nreturn Formed.None($\"the player's party '{country.PlayerPartyAbbrev}' is not in this chamber\");",
            "var verdict = new ProposalVerdict();\nFormationProposal proposal = p;\nforeach (string key in proposal.CabinetParties) { if (bad) { verdict.Reason = \"the cabinet names an unknown party or one twice: \" + key; } }",
            "var verdict = new ProposalVerdict();\nFormationProposal proposal = p;\nforeach (KeyValuePair<string, List<CabinetPortfolio>> kv in proposal.Posts) { verdict.Reason = \"a post is offered to a party outside the cabinet: \" + kv.Key; }",
            "SpeakerRound round = r;\nround.Log.Add($\"{d}: the cabinet {string.Join(\"+\", Shorts(country, cabinet))} takes office\");",
            // S10 (Design's 10a): a group of short names joined with " · " by hand, a local list of short names, and the helper forced to a separator
            "chip.Slip.Add(string.Join(\" · \", g.Cabinet.ConvertAll(Short)) + \" in cabinet, \" + N(cabinet));",
            "var names = new List<string>();\nforeach (string k in g.Cabinet) { names.Add(PartySystems.ShortName(country.Id, k) ?? k); }\nreturn string.Join(\" · \", names) + \" govern on \" + n(held);",
            "var shorts = new List<string>(); foreach (string m in members) { shorts.Add(Short(CountryId.Italy, m)); }\nlines.Add(string.Join(\"+\", shorts));",
            "return PartySystems.JoinShortNames(names, \" · \");",
        };
        private static readonly string[] KeyPassed =
        {
            "SpeakerRound round = r;\nFormationProposal proposal = p;\nround.Log.Add($\"{d}: {Short(country, round.Asked)} forms {PartySystems.JoinShortNames(Shorts(country, proposal.CabinetParties))}\");",
            "SejmFormation f = s;\nf.Log.Add($\"{d}: the President appoints {string.Join(\"+\", f.Cabinet)}\");",
            "GovernmentRecord g = x;\nCountry country = c;\ng.Breaks.Add($\"{d}: {country.PlayerPartyAbbrev} left the government\");",
            "SpeakerRound round = r;\nCountry country = c;\nFormationProposal proposal = p;\nround.Log.Add($\"{d}: {Short(country, round.Asked)} offers {(proposal.CabinetParties.Contains(country.PlayerPartyAbbrev) ? proposal.PostsOf(country.PlayerPartyAbbrev) + \" post(s)\" : \"support\")}\");",
            "SpeakerRound round = r;\nDebug.Log($\"SPEAKER: {round.Asked}\");",
            "WorldClock.GovernmentOfRecord? after = a;\nr.GovernmentLine = head + \" · IN CABINET \" + FormationProjection.Join(id, after.Value.Cabinet) + (after.Value.Support != null ? \"\" : \"\");",
            "var verdict = new ProposalVerdict();\nFormationProposal proposal = p;\nforeach (string key in proposal.CabinetParties) { verdict.Reason = \"an unknown party: \" + PartySystems.ShortName(country.Id, key); }\nforeach (KeyValuePair<string, List<CabinetPortfolio>> kv in proposal.Posts) { verdict.Reason = \"outside the cabinet: \" + PartySystems.ShortName(country.Id, kv.Key); }",
            "SpeakerRound round = r;\nList<string> lines = l;\nforeach (string line in lines) { round.Log.Add(\"the step: \" + line); }",
            "SpeakerRound round = r;\nround.Log.Add($\"{d}: the cabinet {PartySystems.JoinShortNames(Shorts(country, cabinet))} takes office\");\nDebug.Log(\"FORMED \" + string.Join(\"+\", Shorts(country, cabinet)));",
            // S10 (Design's 10a, "where ' · ' stays"): name-and-figure pairs, a caption's fragments, a comma list in prose, the helper itself, a log
            "var parts = new List<string>();\nforeach (string key in keys) { parts.Add(PartySystems.ShortName(CountryId.Poland, key) + \" \" + SeatsOn(s, key).ToString(CultureInfo.InvariantCulture)); }\nreturn string.Join(\" · \", parts);",
            "lines.Add(\"In cabinet · \" + string.Join(\" · \", foot.Clauses));",
            "var names = new List<string>(keys.Count);\nforeach (string k in keys) { names.Add(Short(c, k)); }\nreturn string.Join(\", \", names) + \" declined to join\";",
            "string cabinetWords = PartySystems.JoinShortNames(g.Cabinet.ConvertAll(Short)) + \" in cabinet\";\nstring support = PartySystems.JoinShortNames(names);",
            "Debug.Log(\"FORMED \" + string.Join(\" · \", g.Cabinet.ConvertAll(Short)));",
        };

        /// <summary>0 where the chain holds no party key, 1 a key, 2 a list of keys: the last member a <see cref="KeyMembers"/> member of the type the
        /// rest of the chain is, or a string local initialised from a key.</summary>
        private static int KeyKind(string expr, int pos, List<(int Pos, string Ident, string Type)> decls, TypeMap map)
        {
            if (string.IsNullOrEmpty(expr)) { return 0; }
            string[] parts = expr.Replace("?.", ".").TrimStart('@').Split('.');
            if (parts.Length == 1) { return TypeOfChain(parts, 1, pos, decls, map) == KeyType ? Scalar : 0; }
            if (parts.Length == 2 && parts[1] == "Key" && TypeOfChain(parts, 1, pos, decls, map) == KeyEntryType) { return Scalar; }
            string owner = TypeOfChain(parts, parts.Length - 1, pos, decls, map);
            if (owner == null) { return 0; }
            foreach ((Type t, string member, int kind) in KeyMembers) { if (t.Name == owner && member == parts[parts.Length - 1]) { return kind; } }
            return 0;
        }

        /// <summary>The scan of one source text (comments stripped) for a party key printed raw where a player reads it: each as "line: what - the line".</summary>
        internal static List<string> ScanKeys(string text, TypeMap map)
        {
            var hits = new List<string>();
            string code = CodeView(text);
            List<(int Pos, string Ident, string Type)> decls = Decls(code, map);
            var sinks = new List<(int Start, int End, string What)>();
            foreach (Match m in LogSink.Matches(code))
            {
                if (TypeOfChain(m.Groups[1].Value.Split('.'), m.Groups[1].Value.Split('.').Length, m.Index, decls, map) != "SpeakerRound") { continue; }
                int open = m.Index + m.Length - 1;
                sinks.Add((open + 1, MatchParen(code, open), "the round's log"));
            }
            foreach (Match m in ReasonSink.Matches(code))
            {
                string owner = TypeOfChain(m.Groups[1].Value.Split('.'), m.Groups[1].Value.Split('.').Length, m.Index, decls, map);
                if (Array.IndexOf(ReasonOwners, owner) < 0) { continue; }
                sinks.Add((m.Index + m.Length, StatementEnd(code, m.Index + m.Length), "a " + owner + "'s reason"));
            }
            foreach (Match m in LineSink.Matches(code)) { sinks.Add((m.Index + m.Length, StatementEnd(code, m.Index + m.Length), "the night's government line")); }
            foreach (Match m in NoneSink.Matches(code)) { int open = m.Index + m.Length - 1; sinks.Add((open + 1, MatchParen(code, open), "a formation's no-government reason")); }

            foreach ((int start, int end, string what) in sinks)
            {
                string body = code.Substring(start, Math.Max(0, end - start));
                foreach (Match c in ChainToken.Matches(body))
                {
                    string token = Regex.Replace(c.Value, @"\s+", string.Empty);
                    int kind = KeyKind(token, start + c.Index, decls, map);
                    if (kind == 0 || kind == KeyedDictionary || !Printed(body, c.Index, c.Index + c.Length)) { continue; }
                    int line = 1;
                    for (int k = 0; k < start + c.Index; k++) { if (text[k] == '\n') { line++; } }
                    string shown = text.Split('\n')[line - 1].Trim();
                    hits.Add($"{line}: a party {(kind == KeyList ? "key list" : "key")} printed raw into {what} ({token}) - {shown}");
                }
            }

            // K312: a list of SHORT NAMES - a group of parties named as one - joined by hand, anywhere in the runtime outside a diagnostic: it goes through
            // PartySystems.JoinShortNames, which joins with "+", or " · " where a name holds a '+'. Seen where the list visibly is short names (Shorts(..),
            // ShortName, ConvertAll(Name), ConvertAll(Short)) or is a local list whose every Add since its declaration is one short-name call; joined with "+",
            // " + " or (S10, Design's 10a: S11's night printed "KO · TD · NL") " · ". A list of name-and-figure pairs ("PiS 193 · KO 158") and a caption's
            // fragments are built otherwise and are no group; a comma list is prose ("C, KD declined to join").
            string[] lines = text.Split('\n');
            foreach (Match m in HandJoin.Matches(code))
            {
                int open = m.Index + m.Length - 1, close = MatchParen(code, open);
                int comma = TopLevelComma(code, open, close);
                if (comma < 0) { continue; }
                string separator = text.Substring(open + 1, comma - open - 1).Trim();
                string list = code.Substring(comma + 1, close - comma - 1);
                bool dot = separator == GroupDot;
                if (separator != "\"+\"" && separator != "\" + \"" && !dot) { continue; }
                bool visible = list.Contains("Shorts(") || list.Contains("ConvertAll(Name)") || list.Contains("ConvertAll(Short)")
                    || (list.Contains("ShortName") && !(dot && list.Contains("+")));   // under " · " a ShortName with a figure joined on is a pair, not a name
                if (!visible && !IsShortNameList(code, list, m.Index)) { continue; }
                int line = 1, lineStart = 0;
                for (int k = 0; k < m.Index; k++) { if (text[k] == '\n') { line++; lineStart = k + 1; } }
                if (InsideDiagnostic(lines[line - 1], m.Index - lineStart)) { continue; }
                hits.Add($"{line}: a list of short names joined with {separator} by hand (K312: PartySystems.JoinShortNames) - {lines[line - 1].Trim()}");
            }
            // S10 (10a): the helper handed a separator of its own - S11's night forced " · " on every cabinet (`JoinShortNames(names, " · ")`), so the rule never ran
            foreach (Match m in ForcedJoin.Matches(code))
            {
                if (Regex.IsMatch(code.Substring(Math.Max(0, m.Index - 40), Math.Min(40, m.Index)), @"\bstring\s*$")) { continue; }   // the helper's own declaration
                int open = m.Index + m.Length - 1, close = MatchParen(code, open);
                int comma = TopLevelComma(code, open, close);
                if (comma < 0) { continue; }
                string separator = text.Substring(comma + 1, close - comma - 1).Trim();
                if (separator == "\"+\"") { continue; }
                int line = 1, lineStart = 0;
                for (int k = 0; k < m.Index; k++) { if (text[k] == '\n') { line++; lineStart = k + 1; } }
                if (InsideDiagnostic(lines[line - 1], m.Index - lineStart)) { continue; }
                hits.Add($"{line}: a group of short names forced to the separator {separator} (K312, S10: the helper's own join is \"+\", \" · \" only where a name holds a '+') - {lines[line - 1].Trim()}");
            }
            return hits;
        }

        /// <summary>S10: " · " as a join's separator literal.</summary>
        private const string GroupDot = "\" · \"";
        private static readonly Regex ForcedJoin = new Regex(@"\bJoinShortNames\(", RegexOptions.Compiled);
        private static readonly Regex ListHead = new Regex(@"^\s*(@?[A-Za-z_]\w*)\s*(?:$|\.\s*(?:ToArray|GetRange)\b)", RegexOptions.Compiled);

        /// <summary>The first comma at depth zero inside the call whose parenthesis opens at <paramref name="open"/>, or -1.</summary>
        private static int TopLevelComma(string code, int open, int close)
        {
            int depth = 0;
            for (int k = open + 1; k < close; k++)
            {
                char ch = code[k];
                if (ch == '(' || ch == '[' || ch == '{') { depth++; } else if (ch == ')' || ch == ']' || ch == '}') { depth--; } else if (ch == ',' && depth == 0) { return k; }
            }
            return -1;
        }

        /// <summary>S10: whether a join's list is a LOCAL list of short names - a plain identifier (or its ToArray / GetRange) whose last declaration before the
        /// join (`var x =`, `List&lt;string&gt; x =`) is followed by at least one Add and every Add between them is one short-name call and nothing else
        /// (`x.Add(PartySystems.ShortName(id, k))`, `x.Add(Short(c, k))`, a `?? k` fallback allowed). A name with a figure joined on is a pair and disqualifies.</summary>
        private static bool IsShortNameList(string code, string list, int at)
        {
            Match head = ListHead.Match(list);
            if (!head.Success) { return false; }
            string name = Regex.Escape(head.Groups[1].Value.TrimStart('@'));
            int declared = -1;
            foreach (Match d in Regex.Matches(code.Substring(0, at), @"\b(?:var|List<string>)\s+@?" + name + @"\s*=")) { declared = d.Index; }
            if (declared < 0) { return false; }
            int adds = 0;
            foreach (Match a in Regex.Matches(code.Substring(declared, at - declared), @"\b" + name + @"\s*\.\s*Add\("))
            {
                int open = declared + a.Index + a.Length - 1, close = MatchParen(code, open);
                string arg = code.Substring(open + 1, Math.Max(0, close - open - 1)).Trim();
                Match call = Regex.Match(arg, @"^(?:PartySystems\s*\.\s*)?(?:ShortName|Short)\s*\(");
                if (!call.Success) { return false; }
                int callClose = MatchParen(arg, call.Index + call.Length - 1);
                string rest = callClose >= arg.Length ? "?" : arg.Substring(callClose + 1).Trim();
                if (rest.Length > 0 && !Regex.IsMatch(rest, @"^\?\?\s*@?[A-Za-z_]\w*$")) { return false; }
                adds++;
            }
            return adds > 0;
        }

        /// <summary>Whether the chain at [start, end) of a sink's (literal-blanked) text is PRINTED: straight in an interpolation hole, joined into the
        /// sink's text with +, or handed to string.Join, a possessive or a format - and not when it is compared, named through a short-name helper
        /// (ShortName, Short, Shorts, FormationProjection.Join) or handed to any other call, which reads it as a key.</summary>
        private static bool Printed(string body, int start, int end)
        {
            string after = body.Substring(end).TrimStart(), before = body.Substring(0, start).TrimEnd();
            if (after.StartsWith("==", StringComparison.Ordinal) || after.StartsWith("!=", StringComparison.Ordinal) || before.EndsWith("==", StringComparison.Ordinal) || before.EndsWith("!=", StringComparison.Ordinal)) { return false; }
            int parens = 0, braces = 0;
            for (int j = start - 1; j >= 0; j--)
            {
                char ch = body[j];
                if (ch == ')') { parens++; }
                else if (ch == '}') { braces++; }
                else if (ch == '{') { if (braces > 0) { braces--; } else { return true; } }
                else if (ch == '(')
                {
                    if (parens > 0) { parens--; continue; }
                    string name = CallName(body, j);
                    if (name == null) { continue; }   // a grouping parenthesis, a cast or a keyword's: look further out
                    string last = name.Substring(name.LastIndexOf('.') + 1);
                    if (name == "string.Join" || name == "String.Join" || last == "Possessive" || last == "Format" || last == "Pf" || last == "Concat") { return true; }
                    return false;   // ShortName, Short, Shorts, FormationProjection.Join name it; every other call reads the key
                }
            }
            return true;
        }

        /// <summary>The dotted name of the call whose parenthesis opens at <paramref name="open"/>, or null for a grouping parenthesis or a keyword's.</summary>
        private static string CallName(string code, int open)
        {
            int k = open - 1;
            while (k >= 0 && char.IsWhiteSpace(code[k])) { k--; }
            int e = k;
            while (k >= 0 && (char.IsLetterOrDigit(code[k]) || code[k] == '_' || code[k] == '.' || code[k] == '@')) { k--; }
            if (e < 0 || k == e) { return null; }
            string name = code.Substring(k + 1, e - k).Trim('.');
            return name.Length == 0 || Array.IndexOf(Grouping, name) >= 0 ? null : name;
        }

        /// <summary>The index of the parenthesis closing the one at <paramref name="open"/> in literal-blanked text (the text's end where none closes it).</summary>
        private static int MatchParen(string code, int open)
        {
            int depth = 0;
            for (int k = open; k < code.Length; k++)
            {
                if (code[k] == '(') { depth++; }
                else if (code[k] == ')') { depth--; if (depth == 0) { return k; } }
            }
            return code.Length;
        }

        /// <summary>The index of the statement's own semicolon after <paramref name="from"/> in literal-blanked text (nested parentheses and braces skipped).</summary>
        private static int StatementEnd(string code, int from)
        {
            int depth = 0;
            for (int k = from; k < code.Length; k++)
            {
                char c = code[k];
                if (c == '(' || c == '{' || c == '[') { depth++; }
                else if (c == ')' || c == '}' || c == ']') { if (depth == 0) { return k; } depth--; }
                else if (c == ';' && depth == 0) { return k; }
            }
            return code.Length;
        }

        /// <summary>The text with every literal's own characters blanked to spaces - an interpolation hole's code and its two braces kept, a hole's
        /// nested literals blanked too - so a scan reads code, never prose. Lengths and positions are the text's.</summary>
        internal static string CodeView(string s)
        {
            char[] v = s.ToCharArray();
            int i = 0;
            while (i < s.Length) { i = CodeStep(s, v, i); }
            return new string(v);
        }

        private static int CodeStep(string s, char[] v, int i)
        {
            int n = s.Length;
            char c = s[i];
            // a comment the stripper kept (it keeps a // on a line that holds a quote) is blanked, so its apostrophes and quotes never open a literal
            if (c == '/' && i + 1 < n && s[i + 1] == '/') { int e = i; while (e < n && s[e] != '\n') { if (s[e] != '\r') { v[e] = ' '; } e++; } return e; }
            if (c == '/' && i + 1 < n && s[i + 1] == '*')
            {
                int e = i + 2;
                while (e + 1 < n && !(s[e] == '*' && s[e + 1] == '/')) { e++; }
                for (int k = i; k <= Math.Min(e + 1, n - 1); k++) { if (v[k] != '\n' && v[k] != '\r') { v[k] = ' '; } }
                return e + 2;
            }
            if (c == '\'')
            {
                // a char literal is a few characters on one line; an apostrophe that closes none is no literal
                int e = i + 1;
                while (e < n && e <= i + 8 && s[e] != '\'' && s[e] != '\n') { if (s[e] == '\\') { e++; } e++; }
                if (e >= n || s[e] != '\'') { return i + 1; }
                for (int k = i; k <= e; k++) { v[k] = ' '; }
                return e + 1;
            }
            bool interpolated = false, verbatim = false;
            int quote = -1;
            if (c == '$' && i + 1 < n && s[i + 1] == '"') { interpolated = true; quote = i + 1; }
            else if (c == '$' && i + 2 < n && s[i + 1] == '@' && s[i + 2] == '"') { interpolated = verbatim = true; quote = i + 2; }
            else if (c == '@' && i + 2 < n && s[i + 1] == '$' && s[i + 2] == '"') { interpolated = verbatim = true; quote = i + 2; }
            else if (c == '@' && i + 1 < n && s[i + 1] == '"') { verbatim = true; quote = i + 1; }
            else if (c == '"') { quote = i; }
            if (quote < 0) { return i + 1; }
            for (int k = i; k <= quote; k++) { v[k] = ' '; }
            int j = quote + 1;
            while (j < n)
            {
                char d = s[j];
                if (!verbatim && d == '\\') { v[j] = ' '; if (j + 1 < n && s[j + 1] != '\n') { v[j + 1] = ' '; } j += 2; continue; }
                if (d == '"')
                {
                    if (verbatim && j + 1 < n && s[j + 1] == '"') { v[j] = v[j + 1] = ' '; j += 2; continue; }
                    v[j] = ' ';
                    return j + 1;
                }
                if (interpolated && d == '{')
                {
                    if (j + 1 < n && s[j + 1] == '{') { v[j] = v[j + 1] = ' '; j += 2; continue; }
                    int depth = 0, k = j + 1;
                    while (k < n)
                    {
                        char h = s[k];
                        if (h == '"' || h == '\'' || ((h == '$' || h == '@') && k + 1 < n && (s[k + 1] == '"' || s[k + 1] == '$' || s[k + 1] == '@'))) { k = CodeStep(s, v, k); continue; }
                        if (h == '{') { depth++; }
                        else if (h == '}') { if (depth == 0) { break; } depth--; }
                        k++;
                    }
                    j = k + 1;
                    continue;
                }
                if (interpolated && d == '}' && j + 1 < n && s[j + 1] == '}') { v[j] = v[j + 1] = ' '; j += 2; continue; }
                if (d != '\n' && d != '\r') { v[j] = ' '; }
                j++;
            }
            return j;
        }

        private static Type GuardedNamed(string simpleName)
        {
            foreach (Type g in Guarded) { if (g.Name == simpleName) { return g; } }
            return null;
        }

        /// <summary>A hole or argument cut to its expression: the format and alignment after a top-level ':' or ',' dropped, a trailing ToString() and
        /// a null-conditional dot read plainly.</summary>
        private static string Clip(string hole)
        {
            string s = hole.Trim();
            int depth = 0;
            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];
                if (c == '(' || c == '[' || c == '{') { depth++; }
                else if (c == ')' || c == ']' || c == '}') { depth--; }
                else if (depth == 0 && (c == ':' || c == ',') && !(c == ':' && i + 1 < s.Length && s[i + 1] == ':')) { s = s.Substring(0, i); break; }
            }
            return Strip(s.Trim());
        }

        private static string Strip(string expr)
        {
            string s = expr.Trim().Replace("?.", ".");
            if (s.EndsWith(".ToString()", StringComparison.Ordinal)) { s = s.Substring(0, s.Length - ".ToString()".Length); }
            return s;
        }

        private static bool IsLiteral(string arg)
        {
            string a = arg.TrimStart();
            return a.StartsWith("\"", StringComparison.Ordinal) || a.StartsWith("$\"", StringComparison.Ordinal) || a.StartsWith("@\"", StringComparison.Ordinal) || a.StartsWith("$@\"", StringComparison.Ordinal) || a.StartsWith("@$\"", StringComparison.Ordinal);
        }

        /// <summary>Whether the text at <paramref name="pos"/> sits inside a diagnostic call opened earlier on the line (a log, an assertion, an
        /// exception's constructor), or after a <c>throw</c> with no statement end between - by the call, so a write after a closed log is still read.</summary>
        private static bool InsideDiagnostic(string line, int pos)
        {
            foreach (Match m in Diagnostic.Matches(line))
            {
                if (m.Index >= pos) { break; }
                int depth = 0;
                bool inString = false;
                for (int k = m.Index + m.Length - 1; k < pos && k < line.Length; k++)
                {
                    char c = line[k];
                    if (c == '"' && (k == 0 || line[k - 1] != '\\')) { inString = !inString; continue; }
                    if (inString) { continue; }
                    if (c == '(') { depth++; } else if (c == ')') { depth--; }
                }
                if (depth > 0) { return true; }
            }
            Match t = Throw.Match(line);
            if (!t.Success || t.Index >= pos) { return false; }
            int semi = line.IndexOf(';', t.Index);
            return semi < 0 || semi > pos;
        }

        /// <summary>The top-level arguments of a call whose opening parenthesis ends at <paramref name="from"/> (on this line; a call that runs on
        /// returns what the line holds).</summary>
        private static List<string> Arguments(string line, int from)
        {
            var args = new List<string>();
            var cur = new StringBuilder();
            int depth = 0;
            for (int k = from; k < line.Length; k++)
            {
                char c = line[k];
                if (c == '"')
                {
                    int e = k + 1;
                    while (e < line.Length && line[e] != '"') { if (line[e] == '\\') { e++; } e++; }
                    cur.Append(line, k, Math.Min(e + 1, line.Length) - k);
                    k = e;
                    continue;
                }
                if (c == '(' || c == '[' || c == '{') { depth++; }
                else if (c == ')' || c == ']' || c == '}')
                {
                    if (depth == 0) { args.Add(cur.ToString()); return args; }
                    depth--;
                }
                else if (c == ',' && depth == 0) { args.Add(cur.ToString()); cur.Clear(); continue; }
                cur.Append(c);
            }
            args.Add(cur.ToString());
            return args;
        }

        /// <summary>Every interpolated literal on a line ($"..", $@"..", @$".."): where it opens and its holes' expressions. Plain strings and char
        /// literals are stepped over; a hole's nested strings are kept inside the hole.</summary>
        private static IEnumerable<(int Start, List<string> Holes)> Interpolated(string line)
        {
            int n = line.Length;
            for (int i = 0; i < n; i++)
            {
                char c = line[i];
                bool verbatim;
                int quote;
                if (c == '\'')
                {
                    int e = i + 1;
                    while (e < n && line[e] != '\'') { if (line[e] == '\\') { e++; } e++; }
                    i = e;
                    continue;
                }
                if (c == '$' && i + 1 < n && line[i + 1] == '"') { verbatim = false; quote = i + 1; }
                else if (c == '$' && i + 2 < n && line[i + 1] == '@' && line[i + 2] == '"') { verbatim = true; quote = i + 2; }
                else if (c == '@' && i + 2 < n && line[i + 1] == '$' && line[i + 2] == '"') { verbatim = true; quote = i + 2; }
                else if (c == '"' || (c == '@' && i + 1 < n && line[i + 1] == '"'))
                {
                    bool plainVerbatim = c == '@';
                    int e = plainVerbatim ? i + 2 : i + 1;
                    while (e < n)
                    {
                        if (!plainVerbatim && line[e] == '\\') { e += 2; continue; }
                        if (line[e] == '"') { if (plainVerbatim && e + 1 < n && line[e + 1] == '"') { e += 2; continue; } break; }
                        e++;
                    }
                    i = e;
                    continue;
                }
                else { continue; }

                var holes = new List<string>();
                int j = quote + 1;
                while (j < n)
                {
                    char d = line[j];
                    if (!verbatim && d == '\\') { j += 2; continue; }
                    if (d == '"') { if (verbatim && j + 1 < n && line[j + 1] == '"') { j += 2; continue; } break; }
                    if (d == '{')
                    {
                        if (j + 1 < n && line[j + 1] == '{') { j += 2; continue; }
                        int depth = 0, k = j + 1;
                        var hole = new StringBuilder();
                        while (k < n)
                        {
                            char h = line[k];
                            if (h == '"')
                            {
                                int e = k + 1;
                                while (e < n && line[e] != '"') { if (line[e] == '\\') { e++; } e++; }
                                hole.Append(line, k, Math.Min(e + 1, n) - k);
                                k = e + 1;
                                continue;
                            }
                            if (h == '(' || h == '[' || h == '{') { depth++; }
                            else if (h == ')' || h == ']') { depth--; }
                            else if (h == '}') { if (depth == 0) { break; } depth--; }
                            hole.Append(h);
                            k++;
                        }
                        holes.Add(hole.ToString());
                        j = k + 1;
                        continue;
                    }
                    if (d == '}' && j + 1 < n && line[j + 1] == '}') { j += 2; continue; }
                    j++;
                }
                yield return (i, holes);
                i = j;
            }
        }

        /// <summary>The runtime assembly's fields and properties by their declaring type's simple name: a member's type, by its simple name (a
        /// nullable read as its value's type, a generic by its name without the arity).</summary>
        internal sealed class TypeMap
        {
            private readonly Dictionary<string, Dictionary<string, HashSet<string>>> _members = new Dictionary<string, Dictionary<string, HashSet<string>>>();

            public static TypeMap Of(Assembly assembly)
            {
                var map = new TypeMap();
                Type[] types;
                try { types = assembly.GetTypes(); } catch (ReflectionTypeLoadException e) { types = e.Types; }
                const BindingFlags all = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
                foreach (Type t in types)
                {
                    if (t == null) { continue; }
                    string owner = Simple(t);
                    foreach (FieldInfo f in t.GetFields(all)) { map.Add(owner, f.Name, f.FieldType); }
                    foreach (PropertyInfo p in t.GetProperties(all)) { map.Add(owner, p.Name, p.PropertyType); }
                }
                return map;
            }

            private void Add(string owner, string member, Type type)
            {
                if (!_members.TryGetValue(owner, out Dictionary<string, HashSet<string>> byMember)) { byMember = new Dictionary<string, HashSet<string>>(); _members[owner] = byMember; }
                if (!byMember.TryGetValue(member, out HashSet<string> set)) { set = new HashSet<string>(); byMember[member] = set; }
                set.Add(Simple(Nullable.GetUnderlyingType(type) ?? type));
            }

            private static string Simple(Type t)
            {
                string name = t.Name;
                int tick = name.IndexOf('`');
                return tick >= 0 ? name.Substring(0, tick) : name;
            }

            /// <summary>The type of <paramref name="owner"/>'s member: a guarded enum where any type of that simple name declares it one, the one type
            /// where all agree, otherwise null.</summary>
            public bool Declares(string owner, string member) => _members.TryGetValue(owner, out Dictionary<string, HashSet<string>> byMember) && byMember.ContainsKey(member);

            public string MemberType(string owner, string member)
            {
                if (!_members.TryGetValue(owner, out Dictionary<string, HashSet<string>> byMember) || !byMember.TryGetValue(member, out HashSet<string> set)) { return null; }
                foreach (string s in set) { if (GuardedNamed(s) != null) { return s; } }
                if (set.Count == 1) { foreach (string s in set) { return s; } }
                return null;
            }
        }
    }
}
