using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// CL-3 (2026-09-16, §523; the plan's S-C3): **THE PLAY SHEET, GENERATED.** `PLAY_SHEET.md` at the repo root is the twenty entries of the
    /// play-calibration list (`COMPLETED.md` §346) with, beside each, the constant AS THE CODE HOLDS IT TODAY (read by reflection off the
    /// `Type.Member = value` tokens the entry's owner cell names - never typed here) and §346's "one thing to look for in play" - and a line for
    /// Elias to fill after the play. A token that names a member reflection cannot find FAILS the generation by name, so nothing on the sheet
    /// is filled from memory; a constant that has moved since §346 says so beside its figure.
    ///
    /// <para><see cref="PlaySheetCheck"/> (the cheap bar) regenerates the sheet in memory and compares it with the file on disk: a constant
    /// that moves, or a §346 row that is edited, makes the sheet stale by name until it is regenerated - the generated-catalog pattern
    /// (`GeneratedCatalogCheck`), applied to a document a person fills in. Elias's lines live in the last column and are preserved across a
    /// regeneration: the check compares every column but that one.</para>
    ///
    /// Run: `Unity.exe -batchmode -nographics -projectPath &lt;path&gt; -executeMethod PoliSim.EditorTools.PlaySheetGenerator.Run -logFile &lt;path&gt;`.
    /// </summary>
    public static class PlaySheetGenerator
    {
        public const string SheetRelative = "PLAY_SHEET.md";
        public const string SourceRelative = "COMPLETED.md";
        private const string SectionStart = "## 346.";
        private const string SectionEnd = "## 347.";
        private static readonly Regex Row = new Regex(@"^\| (\d+) \| ([^|]+) \| (.+?) \| ([^|]+) \| ([^|]+) \| (.+?) \|\s*$", RegexOptions.Compiled);
        private static readonly Regex Token = new Regex(@"`([A-Za-z]+)\.([A-Za-z0-9]+) = ([0-9_][0-9_.]*)`", RegexOptions.Compiled);

        private static string F(string f, params object[] a) => string.Format(CultureInfo.InvariantCulture, f, a);
        private static string RootPath(string relative) => Path.Combine(Directory.GetCurrentDirectory(), relative.Replace('/', Path.DirectorySeparatorChar));

        /// <summary>The sheet's rows as generated: entry number, name, the owner cell with every token's figure read from the code, the thing to look for.</summary>
        public sealed class SheetRow { public int Number; public string Entry; public string Owner; public string Today; public string LookFor; public string EliasLine = ""; }

        /// <summary>Reads §346's table off COMPLETED.md and resolves every constant it names. Throws with the name of the first token reflection cannot find.</summary>
        public static List<SheetRow> Rows()
        {
            string text = File.ReadAllText(RootPath(SourceRelative)).Replace("\r\n", "\n");
            int start = text.IndexOf("\n" + SectionStart, StringComparison.Ordinal);
            if (start < 0) { throw new InvalidOperationException("COMPLETED.md carries no §346"); }
            int end = text.IndexOf("\n" + SectionEnd, start + 1, StringComparison.Ordinal);
            string section = end < 0 ? text.Substring(start) : text.Substring(start, end - start);
            var rows = new List<SheetRow>();
            foreach (string line in section.Split('\n'))
            {
                Match m = Row.Match(line);
                if (!m.Success) { continue; }
                var row = new SheetRow
                {
                    Number = int.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture),
                    Entry = m.Groups[2].Value.Trim(),
                    Owner = m.Groups[3].Value.Trim(),
                    LookFor = m.Groups[6].Value.Trim(),
                };
                row.Today = Resolve(row.Owner);
                rows.Add(row);
            }
            if (rows.Count != 20) { throw new InvalidOperationException(F("§346's table reads {0} rows, not twenty", rows.Count)); }
            return rows;
        }

        /// <summary>Every `Type.Member = value` token in the owner cell, read off the code: "Type.Member = today" - with "(was value at §346)" where it moved.
        /// An owner cell with no token is returned as it stands (a formula, a table, a catalogue - the entry names its mechanism, not a number).</summary>
        private static string Resolve(string owner)
        {
            var parts = new List<string>();
            foreach (Match t in Token.Matches(owner))
            {
                string type = t.Groups[1].Value, member = t.Groups[2].Value, quoted = t.Groups[3].Value;
                object value = ReadStatic(type, member);
                if (value == null) { throw new InvalidOperationException(F("reflection finds no static {0}.{1} - the sheet is not filled from memory", type, member)); }
                string today = Format(value);
                double q = double.Parse(quoted.Replace("_", ""), CultureInfo.InvariantCulture);
                bool moved = !(value is IConvertible) || Math.Abs(Convert.ToDouble(value, CultureInfo.InvariantCulture) - q) > 1e-9 * Math.Max(1.0, Math.Abs(q));
                parts.Add(F("`{0}.{1} = {2}`{3}", type, member, today, moved ? F(" **(was {0} at §346)**", quoted) : ""));
            }
            return parts.Count == 0 ? "as the entry states it" : string.Join(" · ", parts);
        }

        private static string Format(object v)
        {
            switch (v)
            {
                case float f: return f.ToString("0.###", CultureInfo.InvariantCulture);
                case double d: return d.ToString("0.###", CultureInfo.InvariantCulture);
                case int i: return i.ToString(CultureInfo.InvariantCulture);
                case long l: return l.ToString(CultureInfo.InvariantCulture);
                default: return Convert.ToString(v, CultureInfo.InvariantCulture);
            }
        }

        /// <summary>A static field (const or readonly) or static property named <paramref name="member"/> on a type named <paramref name="type"/> in the PoliSim assemblies.</summary>
        private static object ReadStatic(string type, string member)
        {
            foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (!asm.FullName.StartsWith("Assembly-CSharp", StringComparison.Ordinal)) { continue; }
                Type[] types;
                try { types = asm.GetTypes(); } catch (ReflectionTypeLoadException e) { types = e.Types; }
                foreach (Type t in types)
                {
                    if (t == null || t.Name != type) { continue; }
                    const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.FlattenHierarchy;
                    FieldInfo f = t.GetField(member, flags);
                    if (f != null) { return f.GetValue(null); }
                    PropertyInfo p = t.GetProperty(member, flags);
                    if (p != null && p.GetIndexParameters().Length == 0) { return p.GetValue(null); }
                }
            }
            return null;
        }

        /// <summary>The sheet's text. Elias's lines are taken from <paramref name="existing"/> by entry number so a regeneration never erases them.</summary>
        public static string Render(List<SheetRow> rows, Dictionary<int, string> existing)
        {
            var sb = new StringBuilder();
            sb.Append("# The play sheet - twenty lines, one each (CL-3, `COMPLETED.md` §523)\n\n");
            sb.Append("GENERATED by `PlaySheetGenerator` from `COMPLETED.md` §346 (the play-calibration list) and the code as it stands; `PlaySheetCheck` on the cheap bar fails the day a constant moves or a §346 row is edited until the sheet is regenerated. **Only the last column is yours** - one line per entry after the play (`PLAY_PROTOCOL.md` says how); a regeneration keeps what you wrote. A figure in the third column is read off the code by reflection at generation, never typed; `(was N at §346)` marks a constant that has moved since the list was made.\n\n");
            sb.Append("| # | entry | the constant today | one thing to look for | Elias's line |\n|---|---|---|---|---|\n");
            foreach (SheetRow r in rows)
            {
                string line = existing != null && existing.TryGetValue(r.Number, out string kept) ? kept : "";
                sb.Append(F("| {0} | {1} | {2} | {3} | {4} |\n", r.Number, Cell(r.Entry), Cell(r.Today), Cell(r.LookFor), Cell(line)));
            }
            return sb.ToString();
        }

        private static string Cell(string s) => (s ?? "").Replace("|", "\\|").Replace("\n", " ");

        /// <summary>Elias's lines as the file on disk holds them, by entry number - the one column the check ignores and the generator keeps.</summary>
        public static Dictionary<int, string> ExistingLines(string path)
        {
            var lines = new Dictionary<int, string>();
            if (!File.Exists(path)) { return lines; }
            foreach (string line in File.ReadAllText(path).Replace("\r\n", "\n").Split('\n'))
            {
                Match m = Regex.Match(line, @"^\| (\d+) \| .+ \| ([^|]*)\|\s*$");
                if (m.Success) { lines[int.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture)] = m.Groups[2].Value.Trim(); }
            }
            return lines;
        }

        /// <summary>The sheet with its last column blanked - what the check compares, so Elias's lines are never a reason to fail.</summary>
        public static string WithoutLastColumn(string sheet)
        {
            var sb = new StringBuilder();
            foreach (string line in sheet.Replace("\r\n", "\n").Split('\n'))
            {
                Match m = Regex.Match(line, @"^(\| \d+ \| .+ \| )[^|]*\|\s*$");
                sb.Append(m.Success ? m.Groups[1].Value + "|" : line).Append('\n');
            }
            return sb.ToString().TrimEnd('\n');
        }

        public static void Run()
        {
            CheckExit.ArmLogFold();
            try
            {
                string path = RootPath(SheetRelative);
                List<SheetRow> rows = Rows();
                string sheet = Render(rows, ExistingLines(path));
                File.WriteAllText(path, sheet.Replace("\n", "\r\n"), new UTF8Encoding(false));
                int moved = 0; foreach (SheetRow r in rows) { if (r.Today.Contains("(was ")) { moved++; } }
                Debug.Log(F("PLAY SHEET: wrote {0} - {1} entries, {2} with a constant moved since §346.", SheetRelative, rows.Count, moved));
                CheckExit.Finish(0);
            }
            catch (Exception e)
            {
                Debug.LogError("PLAY SHEET: not written - " + e.Message);
                CheckExit.Finish(1);
            }
        }
    }

}
