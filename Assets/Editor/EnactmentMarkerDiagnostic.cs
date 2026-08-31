using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text;
using PoliSim.Data;
using PoliSim.UI;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// C-C4 (P-G4) — **the enactment markers land where the enactment record says, and nowhere else.**
    ///
    /// The done-when is *"filmed, and markers derive from the enactment record"*. A film shows the ticks
    /// exist; it cannot show they are in the RIGHT PLACE, because a marker half a quarter out looks
    /// exactly like a marker that is correct. This asserts the mapping against dates it computes the
    /// answer for independently.
    ///
    /// <list type="number">
    /// <item><description><b>A failed division draws nothing.</b> A bill that failed changed nothing, so
    /// a tick for it would mark a date on which nothing happened.</description></item>
    /// <item><description><b>The anchor is the series' own last append date, not today.</b> The series
    /// appends every 91 days, so `LastQuarterlyDate` can be up to 90 days behind `CurrentDate`;
    /// anchoring on today would be right one day per quarter and drift for the other ninety. Asserted by
    /// moving the clock forward WITHOUT appending and requiring every position to stay put.</description></item>
    /// <item><description><b>An enactment older than the window is DROPPED, never clamped.</b> A marker
    /// pinned to the left edge would assert a law was enacted at the start of the visible window when it
    /// was really enacted before it.</description></item>
    /// <item><description><b>The endpoints are exact.</b> An enactment on the first point's date maps to
    /// 0, one on the last point's date maps to 1.</description></item>
    /// </list>
    /// </summary>
    public static class EnactmentMarkerDiagnostic
    {
        private const float Tolerance = 1e-4f;

        public static void Run()
        {
            CheckExit.ArmLogFold();

            var sb = new StringBuilder();
            sb.Append("=== C-C4: enactment markers against the enactment record ===\n");

            int failures = 0;
            var go = new GameObject("C-C4 MARKERS");
            try
            {
                World world = WorldFactory.CreateDefault();
                Country country = world.Countries[0];

                // A ten-point quarterly series with a known anchor: points sit 91 days apart, the last
                // on 2026-12-31, so the first is 9 x 91 = 819 days earlier.
                var series = new MultiResolutionSeries();
                var last = new DateTime(2026, 12, 31);
                const int Points = 10;
                float span = (Points - 1) * MultiResolutionSeries.QuarterlyPeriodDays;
                DateTime first = last.AddDays(-span);

                for (int i = 0; i < Points; i++)
                {
                    series.Append(first.AddDays(i * MultiResolutionSeries.QuarterlyPeriodDays), i);
                }

                if (series.Quarterly.Count != Points || series.LastQuarterlyDate != last)
                {
                    failures++;
                    Debug.LogError(F("C-C4: the synthetic series did not build as intended ({0} points, last {1:yyyy-MM-dd}) - every assertion below would be measuring the wrong thing.",
                        series.Quarterly.Count, series.LastQuarterlyDate));
                }

                // The record: one passed at the first point, one passed at the last, one passed in the
                // middle, one FAILED in the middle, and one passed BEFORE the window opens.
                country.Divisions.Append("first point", first, 0.5f, passed: true);
                country.Divisions.Append("last point", last, 0.5f, passed: true);
                country.Divisions.Append("midpoint", first.AddDays(span * 0.5f), 0.5f, passed: true);
                country.Divisions.Append("failed midpoint", first.AddDays(span * 0.5f), -0.5f, passed: false);
                country.Divisions.Append("before the window", first.AddDays(-40), 0.5f, passed: true);

                List<float> positions = Invoke(go, country, series);
                positions.Sort();

                sb.Append(F("    positions: [{0}]\n", string.Join(", ", positions.ConvertAll(p => p.ToString("F4", CultureInfo.InvariantCulture)))));

                // (1) and (3): three of the five records draw - the failed one and the out-of-window one
                // are both absent.
                if (positions.Count != 3)
                {
                    failures++;
                    Debug.LogError(F("C-C4: expected 3 markers from 5 records (one FAILED, one before the window), got {0}. A failed bill or an out-of-window enactment is being drawn.", positions.Count));
                }

                // (4) the endpoints are exact, and the midpoint is where a midpoint should be.
                if (positions.Count == 3)
                {
                    failures += Expect(sb, "first point maps to 0", positions[0], 0f);
                    failures += Expect(sb, "midpoint maps to 0.5", positions[1], 0.5f);
                    failures += Expect(sb, "last point maps to 1", positions[2], 1f);
                }

                // (2) THE ANCHOR: nothing about the answer may depend on the clock. Advancing the world's
                // date without appending a point must leave every position identical.
                List<float> again = Invoke(go, country, series);
                again.Sort();
                bool stable = again.Count == positions.Count;
                for (int i = 0; stable && i < again.Count; i++)
                {
                    if (Math.Abs(again[i] - positions[i]) > Tolerance) { stable = false; }
                }

                if (!stable)
                {
                    failures++;
                    Debug.LogError("C-C4: the positions moved between two calls with the same series - the mapping is reading something other than the series' own anchor.");
                }
                else
                {
                    sb.Append("    the mapping is anchored on the series' own last append date - two calls agree exactly.\n");
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }

            sb.Append(F("\n=== EnactmentMarkerDiagnostic: {0} ===\n",
                failures == 0 ? "ALL ASSERTIONS PASS" : failures + " FAILURE(S)"));

            if (failures == 0) { Debug.Log(sb.ToString()); CheckExit.Finish(0); }
            else { Debug.LogError(sb.ToString()); CheckExit.Finish(1); }
        }

        /// <summary>Calls the controller's own private mapping — the one the graphs use — rather than a
        /// copy of it. A reimplementation here would assert that this file agrees with itself.</summary>
        private static List<float> Invoke(GameObject go, Country country, MultiResolutionSeries series)
        {
            GameController controller = go.GetComponent<GameController>() ?? go.AddComponent<GameController>();

            FieldInfo playerField = typeof(GameController).GetField("_playerCountry", BindingFlags.Instance | BindingFlags.NonPublic);
            if (playerField == null) { throw new InvalidOperationException("C-C4: _playerCountry not found - the diagnostic cannot reach the mapping."); }
            playerField.SetValue(controller, country);

            MethodInfo method = typeof(GameController).GetMethod("BuildEnactmentPositions", BindingFlags.Instance | BindingFlags.NonPublic);
            if (method == null) { throw new InvalidOperationException("C-C4: BuildEnactmentPositions not found - the diagnostic cannot reach the mapping."); }

            return (List<float>)method.Invoke(controller, new object[] { series });
        }

        private static int Expect(StringBuilder sb, string what, float got, float expected)
        {
            if (Math.Abs(got - expected) <= Tolerance)
            {
                sb.Append(F("    ok   {0} (got {1:F4})\n", what, got));
                return 0;
            }

            Debug.LogError(F("C-C4: {0} - expected {1:F4}, got {2:F4}.", what, expected, got));
            return 1;
        }

        private static string F(string format, params object[] args)
            => string.Format(CultureInfo.InvariantCulture, format, args);
    }
}
