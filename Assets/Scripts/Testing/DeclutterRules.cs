using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace PoliSim.Testing
{
    /// <summary>
    /// **K321 - THE DECLUTTER CAPS READ OFF THE LABEL TABLE** (Code's recommendation on Elias's K320 of 2026-10-30; `docs/RULINGS_2026-10-23.md`,
    /// `docs/specs/UI_V33_SPEC.md` §7). The film's judge, and the rules `Tools/declutter_scan.pl` reads the same tables by.
    ///
    /// <para><b>A breach at rest.</b> A draw of the label table (`DryGuiPass`) that is visible (not OUTSIDE its clip), not a slip's own draw, on a
    /// capture no exemption covers, is a breach when it is CUT (ends in an ellipsis), WRAP (runs past one line - the table's MULTILINE flag - with
    /// at least the cap's counted words), LONG (more counted words than the cap), STOP (ends in a sentence stop with at least the cap's counted
    /// words) or LABEL (a CONTROL draw over the label cap). A counted word is a token holding a letter, tokens split at white space, the middle dot
    /// and the slash: figures are uncounted. ⚠ A machine cannot tell a proper name from a word (K321 (b), DECLARED): a name-heavy line it flags is
    /// answered in the baseline.</para>
    ///
    /// <para><b>The slip.</b> On any capture whose exemption does not cover everything, a slip's head and lines are the draws the table flags
    /// SLIPHEAD / SLIPLINE (level 1) and SLIPHEAD2 / SLIPLINE2 (level 2), grouped in draw order: a level-1 slip with more lines than the cap, or a line
    /// over the line cap, is a SLIP breach; a level-2 slip with more lines than its own cap is one.</para>
    ///
    /// <para><b>One set of rules, read twice.</b> The exemptions and the caps live in ONE file, `Tools/declutter_rules.tsv`, read here and by the
    /// perl tool. The classification is written twice - here for the film, in perl for the baseline and the reports - and the two are held equal by
    /// one fixture both must reproduce (`Tools/declutter_fixture/`: `perl Tools/declutter_scan.pl selftest` and `DeclutterJudgeCheck`) and by
    /// `perl Tools/declutter_scan.pl parity`, which runs this file out of the engine (`Tools/textcheck`'s declutter mode) over any tables and
    /// compares every key.</para>
    ///
    /// <para><b>Pure .NET by construction</b> - no UnityEngine - so the out-of-engine runner compiles this very file.</para>
    /// </summary>
    public static class DeclutterRules
    {
        public const string RulesPath = "Tools/declutter_rules.tsv";
        public const string BaselinePath = "docs/generated/DECLUTTER_BASELINE.tsv";
        public const string MovedFolder = "docs/declutter";
        public const string GenerateCommand = "perl Tools/declutter_scan.pl baseline <the films' label tables (each film's facts beside its table)>";

        public const string Taken = "TAKEN";
        public static readonly string[] Kinds = { "CUT", "WRAP", "LONG", "STOP", "LABEL", "SLIP" };

        // ------------------------------------------------------------------ the rules file

        public sealed class Exemption
        {
            public string Glob;
            public bool All;
            public string Reason;
            public Regex Match;
        }

        public sealed class RuleSet
        {
            public readonly List<Exemption> Exempt = new List<Exemption>();
            public readonly Dictionary<string, int> Caps = new Dictionary<string, int>(StringComparer.Ordinal);
            public int Cap(string name) => Caps[name];

            /// <summary>The exemption that covers <paramref name="capture"/>, the first in the file's order; null when none does.</summary>
            public Exemption ExemptionOf(string capture)
            {
                foreach (Exemption e in Exempt) { if (e.Match.IsMatch(capture)) { return e; } }
                return null;
            }
        }

        public static readonly string[] CapNames = { "label_words", "long_words", "wrap_words", "stop_words", "slip_lines", "slip_line_words", "slip2_lines" };

        /// <summary>The rules file under <paramref name="root"/>; every fault it finds goes to <paramref name="errors"/> (a rule set with errors is not used).</summary>
        public static RuleSet Load(string root, List<string> errors)
        {
            var rules = new RuleSet();
            string path = Path.Combine(root, RulesPath);
            if (!File.Exists(path)) { errors.Add($"{RulesPath} is missing"); return rules; }
            bool header = false;
            int n = 0;
            foreach (string raw in File.ReadAllLines(path, Encoding.UTF8))
            {
                n++;
                string line = raw.TrimEnd('\r');
                if (line.Length == 0 || line.StartsWith("#", StringComparison.Ordinal)) { continue; }
                if (!header) { header = true; continue; }
                string[] c = line.Split('\t');
                if (c.Length != 4 || c[3].Trim().Length == 0) { errors.Add($"{RulesPath} line {n}: four columns, the last a reason - '{line}'"); continue; }
                if (c[0] == "exempt")
                {
                    if (c[2] != "all" && c[2] != "rest") { errors.Add($"{RulesPath} line {n}: an exemption's scope is all or rest - '{c[2]}'"); continue; }
                    rules.Exempt.Add(new Exemption { Glob = c[1], All = c[2] == "all", Reason = c[3], Match = GlobRegex(c[1]) });
                }
                else if (c[0] == "cap")
                {
                    if (!int.TryParse(c[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out int v) || v < 0) { errors.Add($"{RulesPath} line {n}: a cap's value is a whole number - '{c[2]}'"); continue; }
                    if (Array.IndexOf(CapNames, c[1]) < 0) { errors.Add($"{RulesPath} line {n}: no cap is named '{c[1]}'"); continue; }
                    rules.Caps[c[1]] = v;
                }
                else { errors.Add($"{RulesPath} line {n}: a row is exempt or cap - '{c[0]}'"); }
            }
            foreach (string cap in CapNames) { if (!rules.Caps.ContainsKey(cap)) { errors.Add($"{RulesPath}: the cap '{cap}' is not set"); } }
            if (rules.Exempt.Count == 0) { errors.Add($"{RulesPath}: no exemption read - the setup screens would be judged"); }
            return rules;
        }

        /// <summary>A capture-stem glob as an anchored pattern: * any run, ? one character, [..] a class, everything else literal.</summary>
        public static Regex GlobRegex(string glob)
        {
            var sb = new StringBuilder("^");
            for (int i = 0; i < glob.Length; i++)
            {
                char c = glob[i];
                if (c == '*') { sb.Append(".*"); }
                else if (c == '?') { sb.Append('.'); }
                else if (c == '[')
                {
                    int close = glob.IndexOf(']', i + 1);
                    if (close < 0) { sb.Append(Regex.Escape("[")); continue; }
                    sb.Append(glob, i, close - i + 1);
                    i = close;
                }
                else { sb.Append(Regex.Escape(c.ToString())); }
            }
            return new Regex(sb.Append('$').ToString(), RegexOptions.CultureInvariant);
        }

        // ------------------------------------------------------------------ the label table

        public sealed class Row
        {
            public int Index;
            public string Capture;
            public int Width;
            public double Font;
            public string Mode;
            public double NeedsHeight;
            public HashSet<string> Flags;
            /// <summary>The text as the table writes it (escaped: a backslash, a tab and the line breaks as two characters).</summary>
            public string Text;
            /// <summary>The text as drawn.</summary>
            public string Plain;
            public bool Has(string flag) => Flags.Contains(flag);
            public bool IsSlip => Flags.Contains("SLIPHEAD") || Flags.Contains("SLIPLINE") || Flags.Contains("SLIPHEAD2") || Flags.Contains("SLIPLINE2");
        }

        /// <summary>A label table (`DryGuiPass.WriteTable`), its columns found by the header's names.</summary>
        public static List<Row> ReadTable(string path)
        {
            var rows = new List<Row>();
            string[] lines = File.ReadAllLines(path, Encoding.UTF8);
            if (lines.Length == 0) { throw new InvalidDataException($"{path} is empty"); }
            string[] head = lines[0].TrimEnd('\r').Split('\t');
            var at = new Dictionary<string, int>(StringComparer.Ordinal);
            for (int i = 0; i < head.Length; i++) { at[head[i]] = i; }
            foreach (string col in new[] { "capture", "screen", "font", "mode", "needs_height", "flags", "text" })
            {
                if (!at.ContainsKey(col)) { throw new InvalidDataException($"{path}: the table has no '{col}' column"); }
            }
            for (int n = 1; n < lines.Length; n++)
            {
                string line = lines[n].TrimEnd('\r');
                if (line.Length == 0) { continue; }
                string[] f = line.Split('\t');
                string Field(string col) => at[col] < f.Length ? f[at[col]] : string.Empty;
                string screen = Field("screen");
                int x = screen.IndexOf('x');
                int.TryParse(x > 0 ? screen.Substring(0, x) : screen, NumberStyles.Integer, CultureInfo.InvariantCulture, out int width);
                double.TryParse(Field("font"), NumberStyles.Float, CultureInfo.InvariantCulture, out double font);
                double.TryParse(Field("needs_height"), NumberStyles.Float, CultureInfo.InvariantCulture, out double needsHeight);
                string text = Field("text");
                var flags = new HashSet<string>(Field("flags").Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries), StringComparer.Ordinal);
                rows.Add(new Row
                {
                    Index = n, Capture = Field("capture"), Width = width, Font = font, Mode = Field("mode"), NeedsHeight = needsHeight,
                    Flags = flags, Text = text, Plain = Unescape(text),
                });
            }
            return rows;
        }

        /// <summary>The table's escape undone (`DryGuiPass.Escape`): a backslash and the next character become that character, n t r their controls.</summary>
        public static string Unescape(string s)
        {
            if (s.IndexOf('\\') < 0) { return s; }
            var sb = new StringBuilder(s.Length);
            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];
                if (c == '\\' && i + 1 < s.Length)
                {
                    char e = s[++i];
                    sb.Append(e == 'n' ? '\n' : e == 't' ? '\t' : e == 'r' ? '\r' : e);
                }
                else { sb.Append(c); }
            }
            return sb.ToString();
        }

        // ------------------------------------------------------------------ the classification

        private static readonly Regex Separators = new Regex(@"[\s·/]+", RegexOptions.CultureInvariant);
        private static readonly Regex Letter = new Regex(@"\p{L}", RegexOptions.CultureInvariant);
        private static readonly Regex Ellipsis = new Regex(@"(…|\.\.\.)\s*$", RegexOptions.CultureInvariant);
        private static readonly Regex SentenceStop = new Regex(@"[.!?][""'’”»)\]]?\s*$", RegexOptions.CultureInvariant);
        private static readonly Regex Digits = new Regex(@"[0-9]+(?:[.,][0-9]+)*", RegexOptions.CultureInvariant);

        /// <summary>The counted words: tokens (split at white space, the middle dot and the slash) that hold a letter.</summary>
        public static int Words(string plain)
        {
            int n = 0;
            foreach (string t in Separators.Split(plain)) { if (t.Length > 0 && Letter.IsMatch(t)) { n++; } }
            return n;
        }

        /// <summary>The key's text: every run of digits (with its inner points and commas) folded to '#', so a figure that moves keeps its key.</summary>
        public static string Fold(string text) => Digits.Replace(text, "#");

        public sealed class Breach
        {
            public string Capture;
            public string Source;
            public string Kind;
            /// <summary>The table's text (a slip's line-cap breach: the head, the guillemet, the line).</summary>
            public string Text;
            public string Key;
            public int Words;
            public string Detail;
            public string KeyLine => Source + "\t" + Capture + "\t" + Kind + "\t" + Key;
        }

        public sealed class Reading
        {
            public readonly List<Breach> AtRest = new List<Breach>();
            public readonly List<Breach> Slips = new List<Breach>();
            /// <summary>Every capture the table holds a row of, in the table's order - a capture is TAKEN when the table holds a row of it (DECLARED).</summary>
            public readonly List<string> Captures = new List<string>();
            public readonly HashSet<string> ExemptCaptures = new HashSet<string>(StringComparer.Ordinal);
            /// <summary>Whether the table carries K321's flags at all - a table written before them reads WRAP only by the legacy height rule.</summary>
            public bool Flagged;
            public IEnumerable<Breach> All => AtRest.Concat(Slips);
        }

        private static readonly string[] K321Flags = { "MULTILINE", "CONTROL", "CANVAS", "SLIPHEAD", "SLIPLINE", "SLIPHEAD2", "SLIPLINE2" };

        /// <summary>
        /// Every breach in <paramref name="rows"/>. <paramref name="legacy"/> reads a table written before K321's flags: WRAP is then the scanner's
        /// height rule (a wrapped draw whose needed height over 1.25 type sizes exceeds 1.6), and there is no CONTROL, CANVAS or slip draw to read.
        /// </summary>
        public static Reading Read(List<Row> rows, RuleSet rules, bool legacy)
        {
            var reading = new Reading();
            var seenCapture = new HashSet<string>(StringComparer.Ordinal);
            var seenBreach = new HashSet<string>(StringComparer.Ordinal);
            var slipRows = new Dictionary<string, List<Row>>(StringComparer.Ordinal);
            int labelCap = rules.Cap("label_words"), longCap = rules.Cap("long_words"), wrapMin = rules.Cap("wrap_words"), stopMin = rules.Cap("stop_words");

            void Add(List<Breach> into, Breach b)
            {
                if (seenBreach.Add(b.KeyLine)) { into.Add(b); }
            }

            foreach (Row row in rows)
            {
                if (seenCapture.Add(row.Capture)) { reading.Captures.Add(row.Capture); }
                if (!reading.Flagged) { foreach (string f in K321Flags) { if (row.Has(f)) { reading.Flagged = true; break; } } }
                Exemption ex = rules.ExemptionOf(row.Capture);
                if (ex != null) { reading.ExemptCaptures.Add(row.Capture); }
                if (row.Text.Length == 0) { continue; }
                if (row.IsSlip)
                {
                    if (ex == null || !ex.All)
                    {
                        if (!slipRows.TryGetValue(row.Capture, out List<Row> list)) { slipRows[row.Capture] = list = new List<Row>(); }
                        list.Add(row);
                    }
                    continue;
                }
                if (row.Has("OUTSIDE") || ex != null) { continue; }

                int words = Words(row.Plain);
                bool cut = Ellipsis.IsMatch(row.Plain);
                bool multiline = legacy
                    ? row.Mode == "wrap" && row.Font > 0 && row.NeedsHeight / (row.Font * 1.25) > 1.6
                    : row.Has("MULTILINE");
                string source = row.Has("CANVAS") ? "canvas" : "imgui";
                string key = Fold(row.Text);
                if (cut) { Add(reading.AtRest, new Breach { Capture = row.Capture, Source = source, Kind = "CUT", Text = row.Text, Key = key, Words = words }); }
                if (multiline && words >= wrapMin) { Add(reading.AtRest, new Breach { Capture = row.Capture, Source = source, Kind = "WRAP", Text = row.Text, Key = key, Words = words }); }
                if (words > longCap) { Add(reading.AtRest, new Breach { Capture = row.Capture, Source = source, Kind = "LONG", Text = row.Text, Key = key, Words = words }); }
                if (words >= stopMin && !cut && SentenceStop.IsMatch(row.Plain)) { Add(reading.AtRest, new Breach { Capture = row.Capture, Source = source, Kind = "STOP", Text = row.Text, Key = key, Words = words }); }
                if (row.Has("CONTROL") && words > labelCap) { Add(reading.AtRest, new Breach { Capture = row.Capture, Source = source, Kind = "LABEL", Text = row.Text, Key = key, Words = words }); }
            }

            int slipLines = rules.Cap("slip_lines"), slipLineWords = rules.Cap("slip_line_words"), slip2Lines = rules.Cap("slip2_lines");
            foreach (string capture in reading.Captures)
            {
                if (!slipRows.TryGetValue(capture, out List<Row> list)) { continue; }
                foreach ((Row head, int level, List<Row> lines) in GroupSlips(list))
                {
                    int cap = level == 1 ? slipLines : slip2Lines;
                    if (lines.Count > cap)
                    {
                        Add(reading.Slips, new Breach
                        {
                            Capture = capture, Source = "imgui", Kind = "SLIP", Text = head.Text, Key = Fold(head.Text), Words = Words(head.Plain),
                            Detail = $"level {level}: {lines.Count} lines under the head, the cap {cap}",
                        });
                    }
                    if (level != 1) { continue; }
                    foreach (Row line in lines)
                    {
                        int words = Words(line.Plain);
                        if (words <= slipLineWords) { continue; }
                        string text = head.Text + " » " + line.Text;
                        Add(reading.Slips, new Breach
                        {
                            Capture = capture, Source = "imgui", Kind = "SLIP", Text = text, Key = Fold(text), Words = words,
                            Detail = $"level 1: a line of {words} counted words, the cap {slipLineWords}",
                        });
                    }
                }
            }
            return reading;
        }

        /// <summary>A capture's slip draws in table order: a head opens a slip of its level, a line of the same level joins the open one; a line with
        /// no open slip of its level belongs to none.</summary>
        public static List<(Row Head, int Level, List<Row> Lines)> GroupSlips(List<Row> slipRows)
        {
            var slips = new List<(Row Head, int Level, List<Row> Lines)>();
            int open = -1;
            foreach (Row r in slipRows)
            {
                if (r.Has("SLIPHEAD")) { slips.Add((r, 1, new List<Row>())); open = slips.Count - 1; }
                else if (r.Has("SLIPHEAD2")) { slips.Add((r, 2, new List<Row>())); open = slips.Count - 1; }
                else if (open >= 0 && ((r.Has("SLIPLINE") && slips[open].Level == 1) || (r.Has("SLIPLINE2") && slips[open].Level == 2))) { slips[open].Lines.Add(r); }
            }
            return slips;
        }

        /// <summary>One table's keys as the parity mode prints them - country, width, source, capture, kind, key - sorted ordinally.</summary>
        public static List<string> KeyLines(string country, int width, Reading reading)
        {
            var keys = reading.All.Select(b => country + "\t" + width.ToString(CultureInfo.InvariantCulture) + "\t" + b.KeyLine).ToList();
            keys.Sort(StringComparer.Ordinal);
            return keys;
        }

        // ------------------------------------------------------------------ the film's own facts

        /// <summary>
        /// What a film says about itself beside its label table (`&lt;label&gt;_film.tsv`): its country, its width, real or dry, and its MODE - the
        /// choreography that decides what a frame shows. ⚠ One capture stem can show different words in different modes (the Desk at 01c in the
        /// sweep and in the interrupts film, months apart), so the baseline is scoped by mode, and a film is judged only against rows of its own:
        /// otherwise each film would read the other's rows as stale. The perl tool reads this file, so a film's table needs no hand-named country.
        /// </summary>
        public sealed class FilmFacts
        {
            public string Country;
            public int Width;
            public bool Real;
            public string Mode;
        }

        public const string SweepMode = "sweep";

        /// <summary>The facts file beside a label table: `X_labels.tsv` beside `X_film.tsv`.</summary>
        public static string FactsPath(string table) =>
            table.EndsWith("_labels.tsv", StringComparison.Ordinal) ? table.Substring(0, table.Length - "_labels.tsv".Length) + "_film.tsv" : table + ".film.tsv";

        /// <summary>Writes the film's facts beside its table - before the judge judges, so a first run with no baseline still leaves what the baseline is
        /// generated from.</summary>
        public static void WriteFacts(string table, FilmFacts facts)
        {
            var sb = new StringBuilder("# K321: the film's own facts, written beside its label table by the declutter judge; Tools/declutter_scan.pl reads them\n");
            sb.Append("country\t").Append(facts.Country.ToLowerInvariant()).Append('\n');
            sb.Append("width\t").Append(facts.Width.ToString(CultureInfo.InvariantCulture)).Append('\n');
            sb.Append("film\t").Append(facts.Real ? "real" : "dry").Append('\n');
            sb.Append("mode\t").Append(facts.Mode).Append('\n');
            File.WriteAllText(FactsPath(table), sb.ToString(), new UTF8Encoding(false));
        }

        /// <summary>The facts a film wrote beside <paramref name="table"/>, or null when there are none (a table written before K321).</summary>
        public static FilmFacts ReadFacts(string table)
        {
            string path = FactsPath(table);
            if (!File.Exists(path)) { return null; }
            var f = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (string raw in File.ReadAllLines(path, Encoding.UTF8))
            {
                string line = raw.TrimEnd('\r');
                if (line.Length == 0 || line.StartsWith("#", StringComparison.Ordinal)) { continue; }
                int tab = line.IndexOf('\t');
                if (tab > 0) { f[line.Substring(0, tab)] = line.Substring(tab + 1); }
            }
            if (!f.TryGetValue("country", out string country) || !f.TryGetValue("film", out string film) || !f.TryGetValue("mode", out string mode)) { throw new InvalidDataException($"{path} names no country, film or mode"); }
            f.TryGetValue("width", out string w);
            int.TryParse(w, NumberStyles.Integer, CultureInfo.InvariantCulture, out int width);
            return new FilmFacts { Country = country, Width = width, Real = film == "real", Mode = mode };
        }

        // ------------------------------------------------------------------ the baseline

        public const string BaselineHeader = "country\twidth\tmode\tsource\tcapture\tkind\ttext";

        public sealed class BaselineRow
        {
            public string Country;
            public int Width;
            public string Mode;
            public string Source;
            public string Capture;
            public string Kind;
            public string Key;
            public string KeyLine => Source + "\t" + Capture + "\t" + Kind + "\t" + Key;
        }

        /// <summary>The generated baseline: its header says it is generated, its columns are country, width, mode, source, capture, kind, text.</summary>
        public static List<BaselineRow> ReadBaseline(string path, List<string> errors)
        {
            var rows = new List<BaselineRow>();
            bool generated = false, header = false;
            int n = 0;
            foreach (string raw in File.ReadAllLines(path, Encoding.UTF8))
            {
                n++;
                string line = raw.TrimEnd('\r');
                if (line.StartsWith("#", StringComparison.Ordinal)) { if (line.IndexOf("GENERATED by Tools/declutter_scan.pl", StringComparison.Ordinal) >= 0) { generated = true; } continue; }
                if (line.Length == 0) { continue; }
                if (!header)
                {
                    header = true;
                    if (line != BaselineHeader) { errors.Add($"{BaselinePath} line {n}: the header is country, width, mode, source, capture, kind, text - '{line}'"); }
                    continue;
                }
                string[] c = line.Split('\t');
                if (c.Length != 7 || !int.TryParse(c[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int width) || c[2].Length == 0
                    || (c[3] != "imgui" && c[3] != "canvas") || (c[5] != Taken && Array.IndexOf(Kinds, c[5]) < 0))
                {
                    errors.Add($"{BaselinePath} line {n}: not a baseline row - '{line}'");
                    continue;
                }
                rows.Add(new BaselineRow { Country = c[0], Width = width, Mode = c[2], Source = c[3], Capture = c[4], Kind = c[5], Key = c[6] });
            }
            if (!generated) { errors.Add($"{BaselinePath}: its header does not say it is GENERATED by Tools/declutter_scan.pl - it is never hand-edited"); }
            return rows;
        }

        // ------------------------------------------------------------------ the judge

        public sealed class Verdict
        {
            public Reading Reading;
            public readonly List<Breach> New = new List<Breach>();
            public readonly List<BaselineRow> Stale = new List<BaselineRow>();
            /// <summary>What stops the judge from judging (no baseline, an unreadable one, a scope it does not cover, a rules fault); null when it judged.</summary>
            public string Fatal;
            public readonly List<string> Failures = new List<string>();
            public readonly List<string> Notes = new List<string>();
            public int MovedJudged, MovedUnreachable, MovedStillAtRest, MovedElsewhere, MovedMalformed, MovedProse;
            public string Summary;
            public int Code;
        }

        /// <summary>
        /// K321 (d): one film's label table judged against the baseline, within the film's scope - its country, its width and its mode
        /// (<see cref="FilmFacts"/>); <paramref name="real"/> says whether it is a real film (a dry film cannot see Canvas text, so a baseline row of
        /// the Canvas is never stale in one). A breach whose key the scope does not hold is NEW; a row of the scope, of a source the film can see, on
        /// a capture the film took, that no longer breaches is STALE. Either fails, and so does a missing or unreadable baseline (the first run
        /// writes nothing: the integrator generates it) and a scope the baseline holds nothing of. <paramref name="slipBooks"/> (the film's recorded
        /// slip books, or null) carries K321 (g)'s moved words: <see cref="JudgeMoved"/>. <paramref name="baselineAt"/> names another baseline file -
        /// the cheap bar's teeth judge the fixture against a planted one; a film always reads the generated file.
        /// </summary>
        public static Verdict Judge(string root, string table, string slipBooks, string country, bool real, string mode, string baselineAt = null)
        {
            var v = new Verdict();
            country = (country ?? string.Empty).ToLowerInvariant();
            mode = string.IsNullOrEmpty(mode) ? SweepMode : mode;
            var errors = new List<string>();
            RuleSet rules = Load(root, errors);
            List<Row> rows = ReadTable(table);
            int width = rows.Count > 0 ? rows[0].Width : 0;
            if (errors.Count == 0)
            {
                v.Reading = Read(rows, rules, legacy: false);
            }
            else { v.Fatal = "the rules file is faulty - " + string.Join("; ", errors); }

            string baselineFile = baselineAt ?? Path.Combine(root, BaselinePath);
            List<BaselineRow> baseline = null;
            if (v.Fatal == null && !File.Exists(baselineFile))
            {
                v.Fatal = $"no baseline at {BaselinePath} - the judge cannot tell a new breach from a grandfathered one, so this film FAILS and writes nothing. "
                          + $"The integrator generates it once from the films' tables (this one is {table}): {GenerateCommand}";
            }
            else if (v.Fatal == null)
            {
                var baselineErrors = new List<string>();
                baseline = ReadBaseline(baselineFile, baselineErrors);
                if (baselineErrors.Count > 0) { v.Fatal = "the baseline is unreadable - " + string.Join("; ", baselineErrors.Take(5)); }
                else if (!baseline.Exists(b => b.Country == country && b.Width == width && b.Mode == mode && b.Source == "imgui"))
                {
                    v.Fatal = $"the baseline holds nothing of {country} at {width} px in the film mode '{mode}' - its first film generates those rows "
                              + $"(an existing screen measured for the first time): perl Tools/declutter_scan.pl baseline --grow {table}";
                }
            }

            if (v.Fatal == null)
            {
                var sources = new HashSet<string>(real ? new[] { "imgui", "canvas" } : new[] { "imgui" }, StringComparer.Ordinal);
                var mine = baseline.Where(b => b.Country == country && b.Width == width && b.Mode == mode).ToList();
                var grandfathered = new HashSet<string>(mine.Where(b => b.Kind != Taken).Select(b => b.KeyLine), StringComparer.Ordinal);
                var covered = new HashSet<string>(mine.Where(b => b.Kind == Taken).Select(b => b.Source + "\t" + b.Capture), StringComparer.Ordinal);
                var now = new HashSet<string>(v.Reading.All.Select(b => b.KeyLine), StringComparer.Ordinal);
                var taken = new HashSet<string>(v.Reading.Captures, StringComparer.Ordinal);
                foreach (Breach b in v.Reading.All)
                {
                    if (grandfathered.Contains(b.KeyLine)) { continue; }
                    v.New.Add(b);
                    v.Failures.Add($"DECLUTTER NEW {b.Capture} {b.Kind} ({b.Source}, {b.Words} counted words{(b.Detail != null ? "; " + b.Detail : string.Empty)}): {b.Text}"
                                   + (covered.Contains(b.Source + "\t" + b.Capture) ? string.Empty : "  [a frame the baseline never took]"));
                }
                foreach (BaselineRow b in mine)
                {
                    if (b.Kind == Taken || !sources.Contains(b.Source) || !taken.Contains(b.Capture) || now.Contains(b.KeyLine)) { continue; }
                    v.Stale.Add(b);
                    v.Failures.Add($"DECLUTTER STALE {b.Capture} {b.Kind} ({b.Source}): {b.Key} - it no longer breaches on a frame this film took; regenerate the baseline (it only shrinks)");
                }
            }

            if (v.Reading != null && slipBooks != null) { JudgeMoved(root, rows, slipBooks, v); }
            else if (v.Reading != null) { v.Notes.Add("DECLUTTER MOVED: no slip books recorded by this film - the moved words are not judged here"); }

            int atRest = v.Reading?.AtRest.Count ?? 0, slips = v.Reading?.Slips.Count ?? 0;
            v.Code = v.Fatal != null || v.New.Count > 0 || v.Stale.Count > 0 || v.MovedUnreachable > 0 || v.MovedStillAtRest > 0 || v.MovedMalformed > 0 ? 1 : 0;
            v.Summary = string.Format(CultureInfo.InvariantCulture,
                "DECLUTTER: {0} breach(es) at rest, {1} new, {2} stale; {3} slip breach(es); {4} capture(s) read, {5} exempt; moved words {6} judged, {7} unreachable, {8} still at rest{9}; {10} {11} {12} {13}{14}",
                atRest, v.New.Count, v.Stale.Count, slips, v.Reading?.Captures.Count ?? 0, v.Reading?.ExemptCaptures.Count ?? 0,
                v.MovedJudged, v.MovedUnreachable, v.MovedStillAtRest,
                (v.MovedMalformed > 0 ? ", " + v.MovedMalformed + " malformed" : string.Empty) + (v.MovedProse > 0 ? ", " + v.MovedProse + " unjudged (prose, pre-K321; K334)" : string.Empty),
                country, width, real ? "real" : "dry", mode,
                v.Fatal != null ? " - FAILED: " + v.Fatal : v.Code == 0 ? " - PASS" : " - FAILED");
            return v;
        }

        // ------------------------------------------------------------------ K321 (g): the moved words

        public sealed class MovedRow
        {
            public string File;
            public int Line;
            public string CaptureStem;
            public string TextBefore;
            public string NowIn;
            public string SlipText;
            /// <summary>slip or dense.</summary>
            public string Where;
            /// <summary>The capture stem the words now live on.</summary>
            public string Target;
            /// <summary>What must be reachable: the declared slip text, else the text before.</summary>
            public string Needed => SlipText.Length > 0 ? SlipText : TextBefore;
        }

        public const string MovedHeader = "capture_stem\ttext_before\tnow_in\tslip_text";

        /// <summary>K334 (§967): the marker a moved file written before this reader landed carries as a comment line above its header - its now_in
        /// describes the slip in prose. Its rows are read for shape (the header, four columns, a stem and the text before) and counted UNJUDGED: never
        /// reached, never failed, printed on every verdict. Each file converts to slip:/dense: rows when its screen's K322 sweep touches it.</summary>
        public const string ProseMarker = "# format: prose (pre-K321)";

        /// <summary>K334: the prose rows standing when the reader landed (eight files, §957-§966). A ratchet - lowered as files convert, never raised:
        /// DeclutterReachabilityCheck fails above it, so no new prose file and no added prose row passes.</summary>
        public const int ProseRowCeiling = 439;

        /// <summary>The <see cref="MovedRow.Where"/> of a prose file's row.</summary>
        public const string ProseWhere = "prose";

        /// <summary>Every `moved_*.tsv` under <see cref="MovedFolder"/> (one per sweep item, so parallel items never write one file), its rows checked for
        /// shape: the header, four columns, a capture stem, the text before, and where it went - `slip:&lt;capture stem&gt;` (the slip book the page draws
        /// on that capture, two levels) or `dense:&lt;capture stem&gt;` (that capture's dense view).</summary>
        public static List<MovedRow> ReadMoved(string root, List<string> errors, out int files)
        {
            var rows = new List<MovedRow>();
            files = 0;
            string folder = Path.Combine(root, MovedFolder);
            if (!Directory.Exists(folder)) { return rows; }
            foreach (string path in Directory.GetFiles(folder, "moved_*.tsv").OrderBy(p => p, StringComparer.Ordinal))
            {
                files++;
                rows.AddRange(ReadMovedFile(path, Path.GetFileName(path), errors));
            }
            return rows;
        }

        /// <summary>One moved file's rows (see <see cref="ReadMoved"/>).</summary>
        public static List<MovedRow> ReadMovedFile(string path, string name, List<string> errors) => ParseMoved(File.ReadAllLines(path, Encoding.UTF8), name, errors);

        public static List<MovedRow> ParseMoved(IEnumerable<string> lines, string name, List<string> errors)
        {
            var rows = new List<MovedRow>();
            bool header = false, prose = false;
            int n = 0;
            foreach (string raw in lines)
            {
                n++;
                string line = raw.TrimEnd('\r');
                if (!header && line.StartsWith(ProseMarker, StringComparison.Ordinal)) { prose = true; continue; }   // K334: above the header only
                if (line.Length == 0 || line.StartsWith("#", StringComparison.Ordinal)) { continue; }
                if (!header)
                {
                    header = true;
                    if (line != MovedHeader) { errors.Add($"{name} line {n}: the header is capture_stem, text_before, now_in, slip_text - '{line}'"); return rows; }
                    continue;
                }
                string[] c = line.Split('\t');
                if (c.Length != 4) { errors.Add($"{name} line {n}: four columns (the slip text may be empty) - '{line}'"); continue; }
                int colon = c[2].IndexOf(':');
                string where = colon > 0 ? c[2].Substring(0, colon) : string.Empty;
                string target = colon > 0 ? c[2].Substring(colon + 1) : string.Empty;
                if (c[0].Trim().Length == 0 || c[1].Trim().Length == 0) { errors.Add($"{name} line {n}: a capture stem and the text before are required - '{line}'"); continue; }
                if (prose) { rows.Add(new MovedRow { File = name, Line = n, CaptureStem = c[0], TextBefore = c[1], NowIn = c[2], SlipText = c[3], Where = ProseWhere, Target = string.Empty }); continue; }
                if ((where != "slip" && where != "dense") || target.Length == 0) { errors.Add($"{name} line {n}: now_in is slip:<capture stem> or dense:<capture stem> - '{c[2]}'"); continue; }
                rows.Add(new MovedRow { File = name, Line = n, CaptureStem = c[0], TextBefore = c[1], NowIn = c[2], SlipText = c[3], Where = where, Target = target });
            }
            if (!header) { errors.Add($"{name}: no header - the file is capture_stem, text_before, now_in, slip_text"); }
            return rows;
        }

        private static readonly Regex Segmenter = new Regex(@" · | - | / |: |; |\. |, ", RegexOptions.CultureInvariant);
        private static readonly Regex DataOnly = new Regex(@"^[#\s\-+kMBT%.,()·/$€£¢]*$", RegexOptions.CultureInvariant);
        private static readonly Regex Space = new Regex(@"\s+", RegexOptions.CultureInvariant);

        /// <summary>People's normalisation (`PeopleSlipReachabilityCheck`): upper case, the dashes one dash, figures '#', white space one space.</summary>
        public static string Normalise(string s)
        {
            string t = s.ToUpperInvariant().Replace('–', '-').Replace('—', '-').Replace('−', '-');
            t = Digits.Replace(t, "#");
            return Space.Replace(t, " ").Trim();
        }

        /// <summary>The word segments of <paramref name="text"/> that are NOT in <paramref name="reachable"/> (already normalised) - People's means: the
        /// text normalised and split at the middle dot, a spaced dash or slash, a colon, a semicolon, a full stop and a comma; a figure-only segment is
        /// data and needs nothing.</summary>
        public static List<string> Unreachable(string text, string reachable)
        {
            var missing = new List<string>();
            foreach (string part in Segmenter.Split(Normalise(text)))
            {
                string seg = part.Trim().TrimStart('·').Trim().TrimEnd('.', ',');
                if (seg.Length == 0 || DataOnly.IsMatch(seg)) { continue; }
                if (reachable.IndexOf(seg, StringComparison.Ordinal) < 0) { missing.Add(seg); }
            }
            return missing;
        }

        /// <summary>A slip-book table the film wrote (`DryGuiPass.WriteSlipBooks`): capture, level, key, role, text - the reachable text of each capture,
        /// normalised, one line a row (the books' two levels are applied where they are recorded).</summary>
        public static Dictionary<string, StringBuilder> ReadSlipBooks(IEnumerable<string> lines)
        {
            var books = new Dictionary<string, StringBuilder>(StringComparer.Ordinal);
            bool header = false;
            foreach (string raw in lines)
            {
                string line = raw.TrimEnd('\r');
                if (line.Length == 0 || line.StartsWith("#", StringComparison.Ordinal)) { continue; }
                if (!header) { header = true; continue; }
                string[] c = line.Split('\t');
                if (c.Length < 5) { continue; }
                if (!books.TryGetValue(c[0], out StringBuilder sb)) { books[c[0]] = sb = new StringBuilder(); }
                sb.Append(Normalise(Unescape(c[4]))).Append('\n');
            }
            return books;
        }

        /// <summary>
        /// K321 (g), in the film: every moved row whose target capture this film took is judged against the film's own data - a `slip:` row against
        /// the slip book the page drew on that capture (its anchors' slips and the level-2 slips they mark), a `dense:` row against that capture's
        /// draws (scrolled out of the clip included: the reader scrolls to them). A word segment of the slip text (else the text before) that is not
        /// there is UNREACHABLE; the text before still drawn at rest on its own capture is STILL AT REST (it was not moved). A row whose target the
        /// film did not take is left to the film that does.
        /// </summary>
        public static void JudgeMoved(string root, List<Row> rows, string slipBooks, Verdict v)
        {
            var errors = new List<string>();
            List<MovedRow> moved = ReadMoved(root, errors, out int files);
            foreach (string e in errors) { v.Failures.Add("DECLUTTER MOVED MALFORMED: " + e); v.MovedMalformed++; }
            if (files == 0) { v.Notes.Add($"DECLUTTER MOVED: no moved files under {MovedFolder} - nothing to judge"); return; }
            Dictionary<string, StringBuilder> books = File.Exists(slipBooks) ? ReadSlipBooks(File.ReadAllLines(slipBooks, Encoding.UTF8)) : new Dictionary<string, StringBuilder>();
            JudgeMovedRows(moved, rows, books, v);
        }

        /// <summary>The judgement of <see cref="JudgeMoved"/> on rows already read - the cheap bar's teeth drive it with planted data.</summary>
        public static void JudgeMovedRows(List<MovedRow> moved, List<Row> rows, Dictionary<string, StringBuilder> books, Verdict v)
        {
            var dense = new Dictionary<string, StringBuilder>(StringComparer.Ordinal);
            var atRest = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
            foreach (Row r in rows)
            {
                if (!dense.TryGetValue(r.Capture, out StringBuilder sb)) { dense[r.Capture] = sb = new StringBuilder(); }
                sb.Append(Normalise(r.Plain)).Append('\n');
                if (r.IsSlip || r.Has("OUTSIDE")) { continue; }
                if (!atRest.TryGetValue(r.Capture, out HashSet<string> set)) { atRest[r.Capture] = set = new HashSet<string>(StringComparer.Ordinal); }
                set.Add(Fold(r.Plain));
                set.Add(Fold(r.Text));   // a moved file may copy the draw as the table writes it (its breaks escaped) or as it reads
            }
            foreach (MovedRow m in moved)
            {
                if (m.Where == ProseWhere) { v.MovedProse++; continue; }   // K334: a prose file's row is counted, never judged, never reached
                string where = $"{m.File} line {m.Line} ({m.CaptureStem} -> {m.NowIn})";
                if (atRest.TryGetValue(m.CaptureStem, out HashSet<string> still) && still.Contains(Fold(m.TextBefore)))
                {
                    v.MovedStillAtRest++;
                    v.Failures.Add($"DECLUTTER MOVED STILL AT REST {where}: '{m.TextBefore}' still draws at rest on {m.CaptureStem} - a moved word leaves the page");
                }
                StringBuilder target = null;
                if (m.Where == "dense") { dense.TryGetValue(m.Target, out target); }
                else if (!books.TryGetValue(m.Target, out target) && dense.ContainsKey(m.Target))
                {
                    v.MovedJudged++;
                    v.MovedUnreachable++;
                    v.Failures.Add($"DECLUTTER MOVED UNREACHABLE {where}: the film took {m.Target} and the page drew no slip book on it");
                    continue;
                }
                if (target == null) { v.MovedElsewhere++; continue; }
                v.MovedJudged++;
                List<string> missing = Unreachable(Unescape(m.Needed), target.ToString());
                if (missing.Count == 0) { continue; }
                v.MovedUnreachable++;
                v.Failures.Add($"DECLUTTER MOVED UNREACHABLE {where}: {string.Join(" | ", missing.Select(s => "'" + s + "'"))} on no {(m.Where == "slip" ? "slip within two levels" : "line of the dense view")} of {m.Target}");
            }
            if (moved.Count > 0) { v.Notes.Add($"DECLUTTER MOVED: {moved.Count} moved row(s); {v.MovedJudged} on this film's captures, {v.MovedElsewhere} left to the film that takes their target"); }
        }
    }
}
