using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using PoliSim.Data;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// PN-1 (2026-09-13, DS-3 ruled §474): the pension age's statute layer, verified against the CSV it was read from. (1) THE RULES AGAINST THE
    /// CSV: `ElectionsData/pensions/statutory_ages.csv` is parsed here (quoted fields, commas inside), and per country the rule's kind and its
    /// headline figure must be the CSV's. (2) THE INDEXED CLASS IS TWO COUNTRIES - Sweden's riktålder and Italy's ISTAT adjustment - and
    /// nothing else. (3) THE DATED PATH: an indexed rule is known to a horizon and no further - Sweden's six years out (SFB 2 kap. 10 c §), Italy's
    /// to 2028 - and every scheduled rule reaches its end on the year the statute names (Germany 67 in 2031, France 64 in 2033, the USA 67 in
    /// 2027); Poland never moves. (4) THE PATH PRINTED, 2026 to 2036, per country, each year's figure marked KNOWN or "?" - what board 15c's
    /// third mark draws. The driver is deferred (the BASELINE half): this diagnostic reads no world and moves nothing.
    /// </summary>
    public static class PensionAgeDiagnostic
    {
        private const string CsvPath = "ElectionsData/pensions/statutory_ages.csv";

        public static void Run()
        {
            CheckExit.ArmLogFold();
            bool ok = true;
            var sb = new StringBuilder();
            sb.Append("=== PENSION AGE (PN-1): the statute layer against its CSV - the rule kinds, the headline figures, the indexed class of two, the dated paths ===\n");

            Dictionary<string, Dictionary<string, string>> csv = ReadCsv(Path.Combine(Directory.GetCurrentDirectory(), CsvPath));
            if (csv == null) { Debug.LogError($"PENSION AGE: {CsvPath} could not be read."); CheckExit.Finish(1); return; }
            var names = new Dictionary<CountryId, string> { { CountryId.Sweden, "Sweden" }, { CountryId.Germany, "Germany" }, { CountryId.France, "France" }, { CountryId.Italy, "Italy" }, { CountryId.Poland, "Poland" }, { CountryId.USA, "United States" } };
            CountryId[] order = { CountryId.Sweden, CountryId.Germany, CountryId.France, CountryId.Italy, CountryId.Poland, CountryId.USA };

            // (1) the rules against the CSV
            sb.Append("\n    1. THE RULES AGAINST THE CSV - kind and headline figure per country\n");
            foreach (CountryId id in order)
            {
                PensionAgeStatute.Rule r = PensionAgeStatute.Of(id);
                if (r == null) { ok = false; Debug.LogError($"PENSION AGE: {id} has no rule."); continue; }
                if (!csv.TryGetValue(names[id], out Dictionary<string, string> row)) { ok = false; Debug.LogError($"PENSION AGE: the CSV has no row for {names[id]}."); continue; }
                string csvRule = row["rule"];
                string modelRule = r.Kind == PensionAgeRule.LifeExpectancyIndexed ? "LifeExpectancyIndexed" : r.Kind == PensionAgeRule.Scheduled ? "Scheduled" : "Fixed";
                float csvAge = float.Parse(row["current_age_years"], CultureInfo.InvariantCulture);
                sb.Append(F("    {0,-8} {1,-22} headline {2,-8} CSV: {3,-22} {4,-4} · {5} · {6}\n", id, modelRule, PensionAgeStatute.Format(r.Headline), csvRule, csvAge, r.Paragraph, r.SourceFile));
                if (csvRule != modelRule) { ok = false; Debug.LogError($"PENSION AGE: {id}'s rule is {modelRule} in the model and {csvRule} in the CSV."); }
                if (Math.Abs(csvAge - r.Headline) > 1e-4f) { ok = false; Debug.LogError($"PENSION AGE: {id}'s headline is {r.Headline} in the model and {csvAge} in the CSV."); }
                // the CSV names a rule's files separated by semicolons; every one of them must be named by the rule
                foreach (string file in row["source_file"].Split(';'))
                {
                    string name = file.Trim();
                    if (name.Length > 0 && !r.SourceFile.Contains(name)) { ok = false; Debug.LogError($"PENSION AGE: {id}'s source file {name} is not named by the rule ({r.SourceFile})."); }
                }
            }

            // (2) the indexed class is two countries
            sb.Append("\n    2. THE INDEXED CLASS - the countries whose statute recalculates the age from life expectancy\n");
            var indexed = new List<CountryId>();
            foreach (CountryId id in order) { if (PensionAgeStatute.Of(id)?.Kind == PensionAgeRule.LifeExpectancyIndexed) { indexed.Add(id); } }
            sb.Append(F("    {0} of 6: {1}\n", indexed.Count, string.Join(", ", indexed)));
            if (indexed.Count != 2 || !indexed.Contains(CountryId.Sweden) || !indexed.Contains(CountryId.Italy)) { ok = false; Debug.LogError("PENSION AGE: the indexed class is not Sweden and Italy alone."); }

            // (3) the dated paths
            sb.Append("\n    3. THE DATED PATHS - an indexed rule's horizon, a schedule's end year\n");
            var expectations = new Dictionary<CountryId, (int Year, float Age, string Why)>
            {
                { CountryId.Sweden, (PensionAgeStatute.SeedYear + PensionAgeStatute.RiktalderYearsAhead, 67f, "SFB 2 kap. 10 c §: in force the sixth year after its calculation - dated six years out") },
                { CountryId.Italy, (2028, 67f + 3f / 12f, "art. 24 DL 201/2011: 67 y 3 m in 2028, the last figure INPS has published") },
                { CountryId.Germany, (2031, 67f, "§ 235 Abs. 2: the 1964 cohort reaches 67 in 2031") },
                { CountryId.France, (2033, 64f, "L161-17-2: the 1969 cohort reaches 64 in 2033") },
                { CountryId.USA, (2027, 67f, "416(l)(1): those born 1960 attain 67 in 2027") },
                { CountryId.Poland, (2026, 65f, "art. 24 ust. 1: fixed") },
            };
            foreach (CountryId id in order)
            {
                PensionAgeStatute.Rule r = PensionAgeStatute.Of(id);
                (int year, float age, string why) = expectations[id];
                int last = r.Path[r.Path.Length - 1].Year;
                float lastAge = r.Path[r.Path.Length - 1].Age;
                string horizon = r.DatedTo == int.MaxValue ? "known throughout" : "dated to " + r.DatedTo.ToString(CultureInfo.InvariantCulture);
                sb.Append(F("    {0,-8} path ends {1} at {2} · {3} · {4}\n", id, last, PensionAgeStatute.Format(lastAge), horizon, why));
                if (r.Kind == PensionAgeRule.LifeExpectancyIndexed)
                {
                    if (r.DatedTo != year) { ok = false; Debug.LogError($"PENSION AGE: {id}'s dated horizon is {r.DatedTo}, expected {year}."); }
                    if (PensionAgeStatute.IsDated(id, year + 1)) { ok = false; Debug.LogError($"PENSION AGE: {id} reads dated beyond its horizon."); }
                }
                else if (last != year || Math.Abs(lastAge - age) > 1e-4f) { ok = false; Debug.LogError($"PENSION AGE: {id}'s path ends {last} at {lastAge}, expected {year} at {age}."); }
                if (Math.Abs(PensionAgeStatute.AgeInForce(id, 2100) - lastAge) > 1e-6f) { ok = false; Debug.LogError($"PENSION AGE: {id}'s age in force after the path is not its last figure."); }
            }

            // (4) the path printed
            sb.Append("\n    4. THE PATH, 2026 TO 2036 - the age in force each year; ? where the statute has published no figure for the year\n");
            sb.Append("    year    ");
            foreach (CountryId id in order) { sb.Append(F("{0,-12}", id)); }
            sb.Append('\n');
            for (int year = PensionAgeStatute.SeedYear; year <= PensionAgeStatute.SeedYear + 10; year++)
            {
                sb.Append(F("    {0}    ", year));
                foreach (CountryId id in order)
                {
                    string cell = PensionAgeStatute.Format(PensionAgeStatute.AgeInForce(id, year)) + (PensionAgeStatute.IsDated(id, year) ? "" : " ?");
                    sb.Append(F("{0,-12}", cell));
                }
                sb.Append('\n');
            }
            if (PensionAgeStatute.IsDated(CountryId.Sweden, 2033)) { ok = false; Debug.LogError("PENSION AGE: Sweden's 2033 reads dated - the six-year horizon is not enforced."); }

            // (5) board 15c's undated mark: the first year without a published figure, never a year already past - the row printed the horizon's
            // year as NEXT once the calendar had passed it (Italy in 2029, on the films' own calendar) until this was asserted
            sb.Append("\n    5. THE UNDATED MARK - the year the row's \"?\" names: the first year without a published figure, never one already past\n");
            int marksChecked = 0;
            for (int year = PensionAgeStatute.SeedYear; year <= PensionAgeStatute.SeedYear + 20; year++)
            {
                foreach (CountryId id in order)
                {
                    PensionAgeStatute.Rule r = PensionAgeStatute.Of(id);
                    int? mark = PensionAgeStatute.UndatedMarkYear(id, year);
                    bool isIndexed = r.Kind == PensionAgeRule.LifeExpectancyIndexed;
                    if (isIndexed != mark.HasValue) { ok = false; Debug.LogError($"PENSION AGE: {id} in {year} - an undated mark exactly when the rule is indexed, and {(isIndexed ? "none was given" : "one was given")}."); continue; }
                    if (!mark.HasValue) { continue; }
                    marksChecked++;
                    if (mark.Value < year) { ok = false; Debug.LogError($"PENSION AGE: {id} in {year} marks {mark.Value} as its undated year - a year already past."); }
                    if (mark.Value > year && !PensionAgeStatute.IsDated(id, year)) { ok = false; Debug.LogError($"PENSION AGE: {id} in {year} has no published figure for the year itself, but the mark names {mark.Value}."); }
                    if (mark.Value == year && PensionAgeStatute.IsDated(id, year)) { ok = false; Debug.LogError($"PENSION AGE: {id} in {year} marks the year undated though its figure is published."); }
                    if (!PensionAgeStatute.IsDated(id, year) && Math.Abs(PensionAgeStatute.AgeInForce(id, year) - PensionAgeStatute.AgeInForce(id, r.DatedTo)) > 1e-6f) { ok = false; Debug.LogError($"PENSION AGE: {id} in {year} - an undated year's age in force is not the last published figure, carried."); }
                }
            }
            sb.Append(F("    {0} marks checked, {1} to {2}: Sweden 2030 ? {3}, 2040 ? {4} · Italy 2027 ? {5}, 2029 ? {6}, 2040 ? {7} (the knob carrying {8} and {9})\n",
                marksChecked, PensionAgeStatute.SeedYear, PensionAgeStatute.SeedYear + 20,
                PensionAgeStatute.UndatedMarkYear(CountryId.Sweden, 2030), PensionAgeStatute.UndatedMarkYear(CountryId.Sweden, 2040),
                PensionAgeStatute.UndatedMarkYear(CountryId.Italy, 2027), PensionAgeStatute.UndatedMarkYear(CountryId.Italy, 2029), PensionAgeStatute.UndatedMarkYear(CountryId.Italy, 2040),
                PensionAgeStatute.Format(PensionAgeStatute.AgeInForce(CountryId.Sweden, 2040)), PensionAgeStatute.Format(PensionAgeStatute.AgeInForce(CountryId.Italy, 2040))));
            if (Math.Abs(PensionAgeStatute.AgeInForce(CountryId.Germany, 2026) - (66f + 4f / 12f)) > 1e-6f) { ok = false; Debug.LogError("PENSION AGE: Germany's age in force in 2026 is not 66 y 4 m (the 1960 cohort's)."); }
            if (Math.Abs(PensionAgeStatute.AgeInForce(CountryId.France, 2026) - (62f + 9f / 12f)) > 1e-6f) { ok = false; Debug.LogError("PENSION AGE: France's age in force in 2026 is not 62 y 9 m."); }

            Debug.Log(sb.ToString());
            Debug.Log(ok ? "=== PensionAgeDiagnostic: ALL ASSERTIONS PASS ===" : "=== PensionAgeDiagnostic: FAILED ===");
            CheckExit.Finish(ok ? 0 : 1);
        }

        /// <summary>The CSV as rows keyed by the country column: RFC-4180 quoting (a quoted field may hold commas and doubled quotes).</summary>
        private static Dictionary<string, Dictionary<string, string>> ReadCsv(string path)
        {
            if (!File.Exists(path)) { return null; }
            string text = File.ReadAllText(path, Encoding.UTF8);
            var records = new List<List<string>>();
            var field = new StringBuilder(); var record = new List<string>(); bool quoted = false;
            for (int i = 0; i < text.Length; i++)
            {
                char ch = text[i];
                if (quoted)
                {
                    if (ch == '"') { if (i + 1 < text.Length && text[i + 1] == '"') { field.Append('"'); i++; } else { quoted = false; } }
                    else { field.Append(ch); }
                }
                else if (ch == '"') { quoted = true; }
                else if (ch == ',') { record.Add(field.ToString()); field.Clear(); }
                else if (ch == '\n' || ch == '\r')
                {
                    if (ch == '\r' && i + 1 < text.Length && text[i + 1] == '\n') { i++; }
                    record.Add(field.ToString()); field.Clear();
                    if (record.Count > 1 || record[0].Length > 0) { records.Add(record); }
                    record = new List<string>();
                }
                else { field.Append(ch); }
            }
            if (field.Length > 0 || record.Count > 0) { record.Add(field.ToString()); records.Add(record); }
            if (records.Count < 2) { return null; }
            List<string> header = records[0];
            var rows = new Dictionary<string, Dictionary<string, string>>();
            for (int r = 1; r < records.Count; r++)
            {
                var row = new Dictionary<string, string>();
                for (int c = 0; c < header.Count && c < records[r].Count; c++) { row[header[c]] = records[r][c]; }
                if (row.TryGetValue("country", out string country)) { rows[country] = row; }
            }
            return rows;
        }

        private static string F(string format, params object[] args) => string.Format(CultureInfo.InvariantCulture, format, args);
    }
}
