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
    /// SP-2 (§623): THE BRIEF TRACES. For every start point of every country: four clauses, each with a non-empty basis naming its record;
    /// no clause carries a figure the record does not hold (a gap says so in the sentence); Sweden's brief reads the record's own figures
    /// (Kristersson's M+KD+L since 18 October 2022 with 103 of 349 seats and SD's support; polling day 13 September 2026; 8 parties); the
    /// tagline slot is empty until one is reviewed.
    /// </summary>
    public static class StartBriefDiagnostic
    {
        public static void Run()
        {
            CheckExit.ArmLogFold();
            var sb = new StringBuilder();
            int failures = 0;
            sb.Append("=== StartBriefDiagnostic (SP-2, §623): the derived brief ===\n");
            void Check(bool ok, string what) { if (!ok) { failures++; } sb.Append(ok ? "    ok        " : "    FAIL      ").Append(what).Append('\n'); }
            try
            {
                foreach (CountryId id in new[] { CountryId.Sweden, CountryId.Germany, CountryId.Poland, CountryId.Italy, CountryId.USA, CountryId.France })
                {
                    foreach (StartPoints.StartPoint start in StartPoints.For(id))
                    {
                        if (!start.Playable) { continue; }   // a locked contest opens no world; its brief waits for its model
                        List<StartBrief.Clause> clauses = StartBrief.Clauses(start);
                        Check(clauses.Count == 4, F("{0} {1}: four clauses ({2})", id, start.Kind, clauses.Count));
                        foreach (StartBrief.Clause c in clauses)
                        {
                            Check(!string.IsNullOrEmpty(c.Text) && !string.IsNullOrEmpty(c.Basis) && c.Basis.Contains("WorldClock") | c.Basis.Contains("StartPoints") | c.Basis.Contains("PartySystems"),
                                F("{0}: \"{1}\" <- {2}", id, c.Text, c.Basis));
                        }
                        Check(StartBrief.Tagline(start) == null, F("{0}: the tagline slot is empty until one is reviewed", id));
                        sb.Append("    brief     ").Append(StartBrief.Text(start)).Append('\n');
                    }
                }
                StartPoints.TryPlayable(CountryId.Sweden, out StartPoints.StartPoint sweden);
                string text = StartBrief.Text(sweden);
                Check(text.StartsWith("Sweden, 18 January 2026.", StringComparison.Ordinal) && text.Contains("since 18 October 2022, with 103 of 349 seats and the support of SD.")
                    && text.Contains("Polling day is 13 September 2026.") && text.Contains("8 parties sit in the Riksdag (349 seats, the election of 11 September 2022)."),
                    "Sweden's brief reads the record: Kristersson's M+KD+L since 18 Oct 2022, 103 of 349, SD's support; polling day 13 Sep 2026; 8 parties");
            }
            catch (Exception e) { failures++; sb.Append("    THREW: " + e.GetType().Name + ": " + e.Message + "\n" + e.StackTrace + "\n"); }
            if (failures > 0) { Debug.LogError($"START BRIEF: {failures} failure(s).\n{sb}"); CheckExit.Finish(1); return; }
            Debug.Log(sb.ToString());
            CheckExit.Finish(0);
        }

        private static string F(string format, params object[] args) => string.Format(CultureInfo.InvariantCulture, format, args);
    }
}
