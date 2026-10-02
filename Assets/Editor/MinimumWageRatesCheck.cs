using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using PoliSim.Data;
using UnityEngine;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// §756 (Elias's ruling A5): THE STATUTORY MINIMUM WAGES HELD TO THEIR FILE. `MinimumWageRates.Steps` against `ElectionsData/rules/minimum_wage_rates.csv`,
    /// row by row - every SOURCED row present once, with its day, rate, currency and unit, its raw page on disk, and nothing in the table the file does
    /// not hold; Sweden and Italy OFF in the file and in the world (no statutory minimum - collective agreements); the four that carry one implemented;
    /// the step in force read on a few dates, Poland's monthly figure its headline.
    /// </summary>
    public static class MinimumWageRatesCheck
    {
        public static void Run()
        {
            CheckExit.ArmLogFold();
            var sb = new StringBuilder("=== MinimumWageRatesCheck (§756, ruling A5): the statutory rates, dated and sourced by read ===\n");
            int failures = 0;
            void Check(bool ok, string what) { if (!ok) { failures++; } sb.Append(ok ? "    ok        " : "    FAIL      ").Append(what).Append('\n'); }
            try
            {
                string rules = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "ElectionsData", "rules"));
                string[] lines = File.ReadAllLines(Path.Combine(rules, "minimum_wage_rates.csv"), Encoding.UTF8);
                string[] head = SplitCsv(lines[0]);
                int Col(string name) => Array.IndexOf(head, name);
                int country = Col("country"), from = Col("effective_from"), rate = Col("rate"), currency = Col("currency"), unit = Col("unit"), raw = Col("raw_file"), status = Col("status");
                var matched = new HashSet<int>();
                var off = new List<string>();
                int sourced = 0;
                for (int i = 1; i < lines.Length; i++)
                {
                    if (lines[i].Length == 0) { continue; }
                    string[] f = SplitCsv(lines[i]);
                    if (f[status] == "OFF") { off.Add(f[country]); continue; }
                    if (f[status] != "SOURCED") { continue; }
                    sourced++;
                    CountryId id = (CountryId)Enum.Parse(typeof(CountryId), f[country]);
                    DateTime day = DateTime.ParseExact(f[from], "yyyy-MM-dd", CultureInfo.InvariantCulture);
                    float value = float.Parse(f[rate], CultureInfo.InvariantCulture);
                    MinimumWageRates.Per per = f[unit].Contains("month") ? MinimumWageRates.Per.Month : MinimumWageRates.Per.Hour;
                    int at = -1;
                    for (int k = 0; k < MinimumWageRates.Steps.Count; k++)
                    {
                        MinimumWageRates.Step s = MinimumWageRates.Steps[k];
                        if (s.Country == id && s.From == day && s.Unit == per && Mathf.Abs(s.Rate - value) < 1e-4f && s.Currency == f[currency]) { at = k; }
                    }
                    bool rawHeld = f[raw].Length > 0 && File.Exists(Path.Combine(rules, f[raw].Replace('/', Path.DirectorySeparatorChar)));
                    if (at >= 0) { matched.Add(at); }
                    if (at < 0 || !rawHeld) { Check(false, F("{0} {1} {2} {3} {4}: {5}", f[country], f[from], f[rate], f[currency], per, at < 0 ? "NOT IN THE TABLE" : "ITS RAW PAGE " + f[raw] + " IS NOT ON DISK")); }
                }
                Check(sourced == MinimumWageRates.Steps.Count && matched.Count == sourced, F("{0} SOURCED rows in the file, {1} steps in the table, {2} matched one for one - each with its raw page on disk", sourced, MinimumWageRates.Steps.Count, matched.Count));
                World world = WorldFactory.CreateDefault();
                Check(off.Contains("Sweden") && off.Contains("Italy") && off.Count == 2 && !world.GetCountry(CountryId.Sweden).MinimumWageImplemented && !world.GetCountry(CountryId.Italy).MinimumWageImplemented,
                    "Sweden and Italy OFF - in the file and in the world: no statutory minimum, collective agreements");
                foreach (CountryId id in new[] { CountryId.Germany, CountryId.France, CountryId.Poland, CountryId.USA })
                {
                    Check(world.GetCountry(id).MinimumWageImplemented && MinimumWageRates.TryInForce(id, new DateTime(2026, 10, 2), out MinimumWageRates.Step s),
                        F("{0}: a statutory minimum implemented, and in force on 2026-10-02", id));
                }
                bool de = MinimumWageRates.TryInForce(CountryId.Germany, new DateTime(2024, 11, 6), out MinimumWageRates.Step de24) && Mathf.Abs(de24.Rate - 12.41f) < 1e-4f;
                bool de27 = MinimumWageRates.TryInForce(CountryId.Germany, new DateTime(2027, 1, 1), out MinimumWageRates.Step de1) && Mathf.Abs(de1.Rate - 14.60f) < 1e-4f;
                bool fr = MinimumWageRates.TryInForce(CountryId.France, new DateTime(2026, 10, 2), out MinimumWageRates.Step fr26) && Mathf.Abs(fr26.Rate - 12.31f) < 1e-4f;
                bool pl = MinimumWageRates.TryInForce(CountryId.Poland, new DateTime(2026, 10, 2), out MinimumWageRates.Step pl26) && pl26.Unit == MinimumWageRates.Per.Month && Mathf.Abs(pl26.Rate - 4806f) < 1e-4f;
                bool us = MinimumWageRates.TryInForce(CountryId.USA, new DateTime(2026, 10, 2), out MinimumWageRates.Step us26) && Mathf.Abs(us26.Rate - 7.25f) < 1e-4f;
                bool none = !MinimumWageRates.TryInForce(CountryId.Sweden, new DateTime(2026, 10, 2), out _) && !MinimumWageRates.TryInForce(CountryId.Germany, new DateTime(2022, 9, 30), out _);
                Check(de && de27 && fr && pl && us && none, "in force: Germany EUR 12.41 on 2024-11-06 and 14.60 from 2027-01-01; France EUR 12.31, Poland PLN 4 806 a month (the headline), the USA USD 7.25 on 2026-10-02; none for Sweden, none before the first step held");
            }
            catch (Exception e) { failures++; sb.Append("    FAIL      threw: ").Append(e.GetType().Name).Append(": ").Append(e.Message).Append('\n'); }
            sb.Append(failures == 0 ? "    CLEAN\n" : F("    {0} failure(s)\n", failures));
            if (failures > 0) { Debug.LogError(sb.ToString()); CheckExit.Finish(1); return; }
            Debug.Log(sb.ToString());
            CheckExit.Finish(0);
        }

        /// <summary>Splits one CSV row; a field may be quoted, with a comma inside (a doubled quote inside a quoted field is one quote).</summary>
        private static string[] SplitCsv(string line)
        {
            var fields = new List<string>();
            var cur = new StringBuilder();
            bool quoted = false;
            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                if (c == '"') { if (quoted && i + 1 < line.Length && line[i + 1] == '"') { cur.Append('"'); i++; } else { quoted = !quoted; } continue; }
                if (c == ',' && !quoted) { fields.Add(cur.ToString()); cur.Clear(); continue; }
                cur.Append(c);
            }
            fields.Add(cur.ToString());
            return fields.ToArray();
        }

        private static string F(string format, params object[] args) => string.Format(CultureInfo.InvariantCulture, format, args);
    }
}
