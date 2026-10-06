using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// PS-6 US-10 (`COMPLETED.md` §802): THE US VETO DOCUMENT IS CURRENT. `docs/generated/US_VETO_BACKTEST.md` is what `Tools/us_veto_backtest.pl`
    /// writes from `ElectionsData/usa/us_veto_measures.csv` (`Tools/us_veto_prep.pl`); this check fails the cheap bar when it goes stale - the
    /// document's stamp must carry the CSV's digest, and its record and rule tables are recomputed here from the CSV, rule by rule and president by
    /// president, every count compared (the percentages follow from the counts and are not re-rounded here). It also recomputes the bloc Congress under
    /// both readings and chooses the verdict again from the record (B1's yardstick read from the Sejm's CSV).
    /// </summary>
    public static class UsVetoBacktestCheck
    {
        private const string CsvRelative = "ElectionsData/usa/us_veto_measures.csv";
        private const string DocRelative = "docs/generated/US_VETO_BACKTEST.md";
        private static readonly string[] Presidents = { "Donald J. Trump (first term)", "Joseph R. Biden Jr.", "Donald J. Trump (second term)" };

        public static void Run()
        {
            CheckExit.ArmLogFold();
            var sb = new StringBuilder("=== UsVetoBacktestCheck (PS-6 US-10, §802): the US veto document against the record ===\n");
            int failures = 0;
            void Check(bool ok, string what) { if (!ok) { failures++; } sb.Append(ok ? "    ok        " : "    FAIL      ").Append(what).Append('\n'); }
            try { Measure(Check); }
            catch (Exception ex) { failures++; sb.Append("    FAIL      threw: ").Append(ex.Message).Append('\n').Append(ex.StackTrace).Append('\n'); }
            sb.Append(failures == 0 ? "=== UsVetoBacktestCheck: the document is current ===" : F("=== UsVetoBacktestCheck: {0} FAILED ===", failures));
            if (failures == 0) { Debug.Log(sb.ToString()); } else { Debug.LogError(sb.ToString()); }
            CheckExit.Finish(failures == 0 ? 0 : 1);
        }

        private static void Measure(Action<bool, string> Check)
        {
            string root = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            byte[] csvBytes = File.ReadAllBytes(Path.Combine(root, CsvRelative));
            string digest = ElectionsDataCatalogGenerator.Sha256Of(csvBytes);
            List<Dictionary<string, string>> rows = ReadCsv(Encoding.UTF8.GetString(csvBytes));
            string[] doc = File.ReadAllLines(Path.Combine(root, DocRelative), Encoding.UTF8);
            Check(doc.Length > 0 && doc[0].Contains("sha256 " + digest), F("the document's stamp carries the CSV's digest {0} - a changed CSV is re-run through Tools/us_veto_backtest.pl", digest.Substring(0, 8)));
            Check(rows.Count > 0 && rows.All(r => Presidents.Contains(r["president"])), F("the CSV's {0} measures each name one of the three terms", rows.Count));

            // the record table
            foreach (string k in Presidents.Concat(new[] { "the three terms" }))
            {
                var r = rows.Where(x => k == "the three terms" || x["president"] == k).ToList();
                int[] want = { r.Count, r.Count(Vetoed), r.Count(x => x["outcome"] == "vetoed, overridden"), r.Count(x => x["house_final"].StartsWith("roll ", StringComparison.Ordinal)), r.Count(x => x["senate_final"].StartsWith("roll ", StringComparison.Ordinal)) };
                int[] got = Ints(Row(doc, "## The record", k), 1);
                Check(got != null && got.SequenceEqual(want), F("the record, {0}: {1} (the document: {2})", k, string.Join(" / ", want), got == null ? "no row" : string.Join(" / ", got)));
            }

            // the rules, recomputed
            var rules = new (string Name, Func<Dictionary<string, string>, bool> Named)[]
            {
                ("(a) B1 transposed", x => { int[] s = Split(x, "house"); return s != null && 2 * s[1] > s[0] + s[1] + s[2] + s[3]; }),
                ("F3 transposed", x => OpposedF3(x, "house")),
                ("(b) either chamber", x => OpposedF3(x, "house") || OpposedF3(x, "senate")),
                ("both chambers", x => OpposedF3(x, "house") && OpposedF3(x, "senate")),
            };
            foreach (var rule in rules)
            {
                foreach (string k in Presidents.Concat(new[] { "the three terms" }))
                {
                    var r = rows.Where(x => k == "the three terms" || x["president"] == k).ToList();
                    int named = r.Count(rule.Named), namedVetoed = r.Count(x => rule.Named(x) && Vetoed(x)), vetoes = r.Count(Vetoed);
                    int[] want = { named, namedVetoed, vetoes - namedVetoed, named - namedVetoed };
                    string line = doc.SkipWhile(l => !l.StartsWith("## Each rule", StringComparison.Ordinal)).FirstOrDefault(l => l.StartsWith("| " + rule.Name, StringComparison.Ordinal) && Cells(l).Length > 2 && Cells(l)[1] == k);
                    int[] got = line == null ? null : new[] { 2, 3, 4, 5 }.Select(i => LeadingInt(Cells(line)[i])).ToArray();
                    Check(got != null && got.SequenceEqual(want), F("{0}, {1}: named {2}, vetoed among them {3}, outside {4}, named not vetoed {5}{6}", rule.Name, k, want[0], want[1], want[2], want[3], got == null ? " - NO ROW" : got.SequenceEqual(want) ? "" : " - the document: " + string.Join("/", got)));
                }
            }

            // the party-bloc Congress, both readings, recomputed (the plan's premise - one stance in both chambers - and chamber by chamber)
            var expedited = new HashSet<string>(StringComparer.Ordinal) { "CRA disapproval", "arms-sale disapproval", "War Powers", "national emergency", "D.C. disapproval" };
            var seats = new Dictionary<string, (int R, int D, int Sworn, string Date)>(StringComparer.Ordinal);
            foreach (var x in rows.OrderBy(r => r["house_final_date"], StringComparer.Ordinal))
            {
                foreach (string ch in new[] { "house", "senate" })
                {
                    if (!x[ch + "_final"].StartsWith("roll ", StringComparison.Ordinal)) { continue; }
                    int Members(string p) { int[] s = SplitOf(x, ch, p); return s == null ? 0 : s.Sum(); }
                    int r = Members("R"), d = Members("D"), sworn = r + d + Members("I");
                    if (ch == "senate" && x["senate_I_caucus"] != "-")
                    {
                        foreach (string kv in x["senate_I_caucus"].Split(' ')) { string[] p = kv.Split(':'); if (p[0] == "R") { r += int.Parse(p[1], CultureInfo.InvariantCulture); } else if (p[0] == "D") { d += int.Parse(p[1], CultureInfo.InvariantCulture); } }
                    }
                    string key = x["congress"] + " " + ch, date = x[ch + "_final_date"];
                    if (!seats.ContainsKey(key) || string.CompareOrdinal(date, seats[key].Date) >= 0) { seats[key] = (r, d, sworn, date); }
                }
            }
            int opposed = 0, oneStance = 0;
            foreach (var x in rows.Where(x => OpposedF3(x, "house") || OpposedF3(x, "senate")))
            {
                opposed++;
                string other = x["president_party"] == "R" ? "D" : "R";
                var h = seats[x["congress"] + " house"]; var s = seats[x["congress"] + " senate"];
                int ho = other == "R" ? h.R : h.D, so = other == "R" ? s.R : s.D;
                bool senate = expedited.Contains(x["kind"]) ? 2 * so > s.Sworn : so >= (3 * s.Sworn + 4) / 5;
                if (2 * ho > h.Sworn && senate) { oneStance++; }
            }
            string bloc = doc.FirstOrDefault(l => l.StartsWith(F("- Of the {0} measures the president's party opposed in a chamber", opposed), StringComparison.Ordinal));
            Check(bloc != null && bloc.Contains(F("sends **{0}**", oneStance)), F("the plan's premise, one stance in both chambers: of {0} measures opposed, a bloc Congress sends {1}{2}", opposed, oneStance, bloc == null ? " - NO LINE" : ""));
            int vetoable = rows.Count(x => BlocSends(x, expedited) && (BlocOpposed(x, "house") || BlocOpposed(x, "senate")));
            int sendableVetoes = rows.Count(x => Vetoed(x) && BlocSends(x, expedited));
            Check(doc.Any(l => l.Contains(F("it could have sent **{0}** to the president", sendableVetoes))) && doc.Any(l => l.Contains(F("of those, **{0}** are VETOABLE", vetoable))),
                F("chamber by chamber: {0} of the vetoed measures sent, {1} vetoable", sendableVetoes, vetoable));

            // the verdict, chosen again: B1's yardstick from the Sejm's CSV, the rules' pooled precision, the bloc Congress
            int b1Named = 0, b1Vetoed = 0;
            foreach (var p in ReadCsv(File.ReadAllText(Path.Combine(root, "ElectionsData/poland/third_readings_term10.csv"), Encoding.UTF8)))
            {
                if (p["veto_exempt"].Length > 0) { continue; }
                string o = p["outcome"];
                if (!(o == "vetoed" || o.StartsWith("signed", StringComparison.Ordinal) || o.StartsWith("referred to Tribunal", StringComparison.Ordinal))) { continue; }
                if (2 * int.Parse(p["pis_no"], CultureInfo.InvariantCulture) <= int.Parse(p["pis_members"], CultureInfo.InvariantCulture)) { continue; }
                b1Named++; if (o == "vetoed") { b1Vetoed++; }
            }
            bool allFar = rules.All(rule => { int nm = rows.Count(rule.Named); return nm == 0 || (double)rows.Count(x => rule.Named(x) && Vetoed(x)) / nm < (double)b1Vetoed / b1Named; });
            int aNamed = rows.Count(rules[0].Named), aVetoed = rows.Count(x => rules[0].Named(x) && Vetoed(x));
            string wantVerdict = allFar || oneStance == 0 ? "**STOPPED" : (double)aVetoed / Math.Max(1, aNamed) >= (double)b1Vetoed / b1Named ? "**Code recommends (a)" : "**No recommendation";
            string verdict = doc.SkipWhile(l => !l.StartsWith("## The verdict (R-US17)", StringComparison.Ordinal)).FirstOrDefault(l => l.StartsWith("**", StringComparison.Ordinal));
            Check(b1Named == 166 && b1Vetoed == 45, F("B1's yardstick from the Sejm's CSV: {0} of {1}", b1Vetoed, b1Named));
            Check(verdict != null && verdict.StartsWith(wantVerdict, StringComparison.Ordinal), F("the verdict is the one the record gives: {0} ({1})", wantVerdict.Trim('*'), verdict == null ? "none" : verdict.Substring(0, Math.Min(60, verdict.Length))));
        }

        private static int[] SplitOf(Dictionary<string, string> x, string ch, string p)
        {
            if (x[ch + "_" + p + "_yea"] == "-") { return null; }
            return new[] { "yea", "nay", "present", "not_voting" }.Select(v => int.Parse(x[ch + "_" + p + "_" + v], CultureInfo.InvariantCulture)).ToArray();
        }

        /// <summary>Chamber by chamber: each bloc votes as its members' majority voted there (yea above nay and present); no roll call passes.</summary>
        private static bool BlocPasses(Dictionary<string, string> x, string ch, HashSet<string> expedited)
        {
            if (!x[ch + "_final"].StartsWith("roll ", StringComparison.Ordinal)) { return true; }
            int yes = 0, no = 0, sworn = 0;
            var dir = new Dictionary<string, bool>();
            foreach (string p in new[] { "R", "D" }) { int[] s = SplitOf(x, ch, p); dir[p] = s != null && s[0] > s[1] + s[2]; int seats = s == null ? 0 : s.Sum(); sworn += seats; if (dir[p]) { yes += seats; } else { no += seats; } }
            int[] i = SplitOf(x, ch, "I"); sworn += i == null ? 0 : i.Sum();
            if (ch == "senate" && x["senate_I_caucus"] != "-")
            {
                foreach (string kv in x["senate_I_caucus"].Split(' ')) { string[] p = kv.Split(':'); if (p[0] != "R" && p[0] != "D") { continue; } int n = int.Parse(p[1], CultureInfo.InvariantCulture); if (dir[p[0]]) { yes += n; } else { no += n; } }
            }
            if (ch == "senate" && !expedited.Contains(x["kind"])) { return yes >= (3 * sworn + 4) / 5; }
            return yes > no;
        }
        private static bool BlocSends(Dictionary<string, string> x, HashSet<string> expedited) => BlocPasses(x, "house", expedited) && BlocPasses(x, "senate", expedited);
        private static bool BlocOpposed(Dictionary<string, string> x, string ch) { int[] s = SplitOf(x, ch, x["president_party"]); return s != null && s[0] <= s[1] + s[2]; }

        private static bool Vetoed(Dictionary<string, string> x) => x["outcome"].Contains("veto");
        private static int[] Split(Dictionary<string, string> x, string ch)
        {
            string p = x["president_party"];
            if (x[ch + "_" + p + "_yea"] == "-") { return null; }
            return new[] { "yea", "nay", "present", "not_voting" }.Select(v => int.Parse(x[ch + "_" + p + "_" + v], CultureInfo.InvariantCulture)).ToArray();
        }
        private static bool OpposedF3(Dictionary<string, string> x, string ch) { int[] s = Split(x, ch); return s != null && s[1] + s[2] > s[0]; }

        private static string Row(string[] doc, string section, string key) =>
            doc.SkipWhile(l => !l.StartsWith(section, StringComparison.Ordinal)).Skip(1).TakeWhile(l => !l.StartsWith("## ", StringComparison.Ordinal)).FirstOrDefault(l => l.StartsWith("| " + key + " |", StringComparison.Ordinal));
        private static int[] Ints(string line, int from) => line == null ? null : Cells(line).Skip(from).Select(LeadingInt).ToArray();
        private static string[] Cells(string line) => line.Trim().Trim('|').Split('|').Select(c => c.Trim()).ToArray();
        private static int LeadingInt(string cell) { int i = 0; while (i < cell.Length && char.IsDigit(cell[i])) { i++; } return i == 0 ? -1 : int.Parse(cell.Substring(0, i), CultureInfo.InvariantCulture); }

        /// <summary>RFC 4180: quoted fields, doubled quotes, '#' comment lines before the header.</summary>
        private static List<Dictionary<string, string>> ReadCsv(string text)
        {
            var records = new List<List<string>>();
            var row = new List<string>();
            var field = new StringBuilder();
            bool inq = false;
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (inq)
                {
                    if (c == '"') { if (i + 1 < text.Length && text[i + 1] == '"') { field.Append('"'); i++; } else { inq = false; } }
                    else { field.Append(c); }
                    continue;
                }
                if (c == '"') { inq = true; }
                else if (c == ',') { row.Add(field.ToString()); field.Clear(); }
                else if (c == '\r') { }
                else if (c == '\n') { row.Add(field.ToString()); field.Clear(); records.Add(row); row = new List<string>(); }
                else { field.Append(c); }
            }
            if (field.Length > 0 || row.Count > 0) { row.Add(field.ToString()); records.Add(row); }
            records = records.Where(r => !(r.Count == 1 && r[0].Length == 0) && !r[0].StartsWith("#", StringComparison.Ordinal)).ToList();
            List<string> head = records[0];
            var rows = new List<Dictionary<string, string>>();
            foreach (List<string> r in records.Skip(1))
            {
                if (r.Count != head.Count) { throw new InvalidDataException(F("{0}: a row of {1} fields against {2}", CsvRelative, r.Count, head.Count)); }
                var d = new Dictionary<string, string>(StringComparer.Ordinal);
                for (int c = 0; c < head.Count; c++) { d[head[c]] = r[c]; }
                rows.Add(d);
            }
            return rows;
        }

        private static string F(string format, params object[] args) => string.Format(CultureInfo.InvariantCulture, format, args);
    }
}
