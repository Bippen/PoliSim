using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using PoliSim.Data;
using PoliSim.Elections;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// §714 (Elias's ruling of 2026-10-01, item 5: "Formation time comes from each country's recent formations, election date to installation,
    /// derived from the record's own dates. The caretaker governs meanwhile"): every country's formation time as <see cref="WorldClock.FormationDays"/>
    /// derives it from the record - each formation it averages printed, the result asserted - so a government added to the record, or a change to
    /// the rule, shows here. (The round's gate is `GermanFormationDiagnostic` (d)'s: the 2025 Bundestag elects on 6 May, the record's day.)
    /// </summary>
    public static class FormationTimeDiagnostic
    {
        private static string F(string format, params object[] args) => string.Format(CultureInfo.InvariantCulture, format, args);

        public static void Run()
        {
            CheckExit.ArmLogFold();
            var sb = new StringBuilder("=== FormationTimeDiagnostic (§714): the formation time each country's record gives ===\n");
            int failures = 0;
            void Check(bool ok, string what) { if (!ok) { failures++; } sb.Append(ok ? "    ok        " : "    FAIL      ").Append(what).Append('\n'); }
            var expected = new (CountryId Id, int? Days)[] { (CountryId.Germany, 72), (CountryId.Sweden, 37), (CountryId.Poland, 46), (CountryId.Italy, 27), (CountryId.France, 60), (CountryId.USA, null) };
            foreach ((CountryId id, int? days) in expected)
            {
                int? derived = WorldClock.FormationDays(id, out List<string> formations);
                Check(derived == days, F("{0}: {1} - {2}", id, derived.HasValue ? derived.Value + " days, the median of " + formations.Count + " formation(s) on record" : "none on record",
                    formations.Count > 0 ? string.Join("; ", formations) : "no cabinet of record took office within a year of an election of record"));
            }
            if (failures > 0) { Debug.LogError($"FORMATION TIME: {failures} failure(s).\n{sb}"); CheckExit.Finish(1); return; }
            Debug.Log(sb.ToString());
            CheckExit.Finish(0);
        }
    }
}
