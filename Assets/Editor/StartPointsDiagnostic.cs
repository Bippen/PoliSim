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
    /// SP-1 (§622): THE START POINTS, ASSERTED. Every country offers at least one; exactly one is playable today and it is the ruled start
    /// of §618 (its polling day the country's latest election of record, its opening `WorldClock.StartDate`, its line the selector's);
    /// every locked card carries a reason; Poland and France offer two contests, the presidential one locked; the cards are in date order;
    /// a date the record does not hold is billed on the card, never typed.
    /// </summary>
    public static class StartPointsDiagnostic
    {
        public static void Run()
        {
            CheckExit.ArmLogFold();
            var sb = new StringBuilder();
            int failures = 0;
            sb.Append("=== StartPointsDiagnostic (SP-1, §622): the start points as data ===\n");
            void Check(bool ok, string what) { if (!ok) { failures++; } sb.Append(ok ? "    ok        " : "    FAIL      ").Append(what).Append('\n'); }
            try
            {
                foreach (CountryId id in new[] { CountryId.Sweden, CountryId.Germany, CountryId.Poland, CountryId.Italy, CountryId.USA, CountryId.France })
                {
                    IReadOnlyList<StartPoints.StartPoint> points = StartPoints.For(id);
                    int playable = 0;
                    foreach (StartPoints.StartPoint p in points) { if (p.Playable) { playable++; } }
                    Check(points.Count >= 1 && playable == 1, F("{0}: {1} start point(s), {2} playable", id, points.Count, playable));
                    Check(StartPoints.TryPlayable(id, out StartPoints.StartPoint start) && start.PollingDay == WorldClock.LatestElectionDay(id) && start.Opens == WorldClock.StartDate(id) && start.Line == WorldClock.StartLine(id),
                        F("{0}: the playable start is the ruled one - {1}, polling day {2:yyyy-MM-dd}, opens {3:yyyy-MM-dd}: {4}", id, start.Kind, start.PollingDay, start.Opens, start.Line));
                    for (int i = 0; i < points.Count; i++)
                    {
                        StartPoints.StartPoint p = points[i];
                        Check(!string.IsNullOrEmpty(p.Line) && !string.IsNullOrEmpty(p.Basis) && !string.IsNullOrEmpty(p.Kind), F("{0} #{1} {2}: a line, a basis and a kind", id, i, p.Kind));
                        Check(p.Playable || p.Line.StartsWith("LOCKED", StringComparison.Ordinal), F("{0} #{1} {2}: a locked card says LOCKED and why - {3}", id, i, p.Kind, p.Line));
                        Check(p.PollingDay != DateTime.MinValue || (p.DateNote != null && p.DateNote.Contains("BILLED")), F("{0} #{1} {2}: the date is the record's or billed on the card ({3})", id, i, p.Kind, StartPoints.DateLine(p)));
                        if (i > 0) { Check(StartPoints.DateLine(points[i - 1]) != StartPoints.DateLine(p) || points[i - 1].Kind != p.Kind, F("{0}: #{1} and #{2} are distinct", id, i - 1, i)); }
                    }
                    Check((id == CountryId.Poland || id == CountryId.France) == (points.Count == 2), F("{0}: two contests offered only where a president is popularly elected beside the chamber (the USA's House is a later option, the spec's §1.1)", id));
                }
                Check(StartPoints.For(CountryId.Poland)[1].Kind.StartsWith("PRESIDENTIAL", StringComparison.Ordinal) && StartPoints.For(CountryId.Poland)[1].PollingDay == new DateTime(2025, 5, 18), "Poland's presidential card: 18 May 2025 (the record's first round), after the Sejm's");
                Check(StartPoints.For(CountryId.France)[0].Kind.StartsWith("PRESIDENTIAL", StringComparison.Ordinal) && StartPoints.For(CountryId.France)[0].DateNote.Contains("E-49"), "France's presidential card: 2022, its rounds billed (E-49), before the legislative");
            }
            catch (Exception e) { failures++; sb.Append("    THREW: " + e.GetType().Name + ": " + e.Message + "\n" + e.StackTrace + "\n"); }
            if (failures > 0) { Debug.LogError($"START POINTS: {failures} failure(s).\n{sb}"); CheckExit.Finish(1); return; }
            Debug.Log(sb.ToString());
            CheckExit.Finish(0);
        }

        private static string F(string format, params object[] args) => string.Format(CultureInfo.InvariantCulture, format, args);
    }
}
