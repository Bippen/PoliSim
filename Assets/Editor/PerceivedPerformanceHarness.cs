using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using PoliSim.Data;
using PoliSim.Elections;
using PoliSim.Simulation;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// W-A5's harness — **the vote model tracks the PUBLISHED economy, not the true one**, on a
    /// REAL run rather than a synthetic fixture. The lag is not injected: it is what
    /// `PublicationSystem` and the real release calendar produce on their own, which is the whole
    /// reason the gap table called §19 an EXISTS row.
    ///
    /// The run drives the simulation day by day (the `TrajectoryBaselineDump` idiom — no Play mode,
    /// no scene), sampling at intervals: the true unemployment in `Country.State`, the latest
    /// PUBLISHED unemployment, the perceived and actual performance indices, and the incumbent
    /// multiplier each would produce.
    ///
    /// ⚠ **This harness advances a simulation, so it arms `CheckExit`'s log fold** — an ATTRIB
    /// raised while it runs fails the harness rather than being reported beside a clean-looking
    /// result.
    ///
    /// The done-when, asserted:
    /// 1. the published series really does lag `State` (they differ at sampled dates);
    /// 2. a published figure matches an EARLIER true value more closely than the current one —
    ///    i.e. perception is tracking the publication, not merely noisy;
    /// 3. the perceived and actual performance indices diverge, and the incumbent multiplier the
    ///    vote model uses follows the PERCEIVED one;
    /// 4. the divergence is reportable as an attribution line (§31's idiom).
    /// </summary>
    public static class PerceivedPerformanceHarness
    {
        private const int Years = 6;
        private const CountryId Target = CountryId.Sweden;

        public static void Run()
        {
            CheckExit.ArmLogFold();
            SimulationRandom.Seed(777);

            World world = WorldFactory.CreateDefault();
            var go = new GameObject("W_A5_PERCEIVED");
            int failures = 0;
            var sb = new StringBuilder();
            sb.Append("=== W-A5: the vote model reads PERCEIVED performance (§19) ===\n");

            try
            {
                SimulationManager sim = go.AddComponent<SimulationManager>();
                sim.SetWorld(world);
                Country country = world.GetCountry(Target);

                var decisions = new Dictionary<CountryId, PolicyDecision>();
                foreach (Country c in world.Countries) { decisions[c.Id] = PolicyDecision.None(); }

                var trueHistory = new List<(DateTime Date, double Unemployment)>();
                var samples = new List<(DateTime Date, double TrueU, double PubU, double Perceived, double Actual)>();

                int totalDays = Years * SimulationManager.DaysPerTurn;
                for (int day = 1; day <= totalDays; day++)
                {
                    if (sim.AdvanceDay()) { sim.AdvanceTurn(decisions); }

                    trueHistory.Add((sim.CurrentDate, country.State.Unemployment));

                    if (day % 60 != 0) { continue; }

                    PublishedEntry pub = country.Published.Latest(PublishedStat.Unemployment);
                    if (pub == null) { continue; }

                    PerceivedPerformance.Reading perceived = PerceivedPerformance.Perceived(country, null);
                    PerceivedPerformance.Reading actual = PerceivedPerformance.Actual(country, null);
                    samples.Add((sim.CurrentDate, country.State.Unemployment, pub.Value, perceived.Index, actual.Index));
                }

                sb.Append($"  {samples.Count} samples over {Years} simulated years ({Target}); publication is on the real release calendar\n");
                sb.Append("  date         true U   published U   perceived idx   actual idx   divergence\n");
                int shown = 0;
                foreach (var s in samples)
                {
                    if (shown++ % 6 != 0) { continue; }

                    sb.Append(string.Format(CultureInfo.InvariantCulture,
                        "  {0:yyyy-MM-dd}   {1,6:F3}    {2,6:F3}       {3,6:F2}        {4,6:F2}     {5,7:+0.00;-0.00;0.00}\n",
                        s.Date, s.TrueU, s.PubU, s.Perceived, s.Actual, s.Perceived - s.Actual));
                }

                // 1. The published series really does lag the truth.
                int differing = 0;
                double maxGap = 0.0;
                foreach (var s in samples)
                {
                    double gap = Math.Abs(s.PubU - s.TrueU);
                    if (gap > 1e-6) { differing++; }
                    if (gap > maxGap) { maxGap = gap; }
                }

                failures += Assert(sb, "1. the published figure differs from the live one (a real lag exists)",
                    differing > 0, $"{differing} of {samples.Count} samples differ; largest gap {maxGap:F4} pp");

                // 2. Perception tracks the PUBLICATION: the published value matches an EARLIER true
                //    value better than it matches the current one, for most samples.
                int tracksEarlier = 0;
                int comparable = 0;
                foreach (var s in samples)
                {
                    double nowGap = Math.Abs(s.PubU - s.TrueU);
                    double bestEarlier = double.MaxValue;
                    foreach (var h in trueHistory)
                    {
                        if (h.Date >= s.Date) { break; }
                        double g = Math.Abs(s.PubU - h.Unemployment);
                        if (g < bestEarlier) { bestEarlier = g; }
                    }

                    if (bestEarlier == double.MaxValue) { continue; }

                    comparable++;
                    if (bestEarlier < nowGap) { tracksEarlier++; }
                }

                failures += Assert(sb, "2. the published figure matches an EARLIER true value better than the current one",
                    comparable > 0 && tracksEarlier * 2 >= comparable,
                    $"{tracksEarlier} of {comparable} samples track an earlier value");

                // 3. The vote model follows PERCEIVED, and that differs from following ACTUAL.
                var preference = new[] { 0.34, 0.30, 0.20, 0.16 };
                var incumbent = new[] { true, false, false, false };
                int voteDiffers = 0;
                double maxVoteGapPp = 0.0;
                foreach (var s in samples)
                {
                    double[] byPerceived = PerceivedPerformance.ApplyIncumbency(preference, incumbent, s.Perceived);
                    double[] byActual = PerceivedPerformance.ApplyIncumbency(preference, incumbent, s.Actual);
                    double gapPp = Math.Abs(byPerceived[0] - byActual[0]) * 100.0;
                    if (gapPp > 1e-9) { voteDiffers++; }
                    if (gapPp > maxVoteGapPp) { maxVoteGapPp = gapPp; }
                }

                failures += Assert(sb, "3. the incumbent's modelled share differs when driven by perceived vs actual",
                    voteDiffers > 0,
                    string.Format(CultureInfo.InvariantCulture,
                        "{0} of {1} samples differ; largest {2:F3} pp on the incumbent", voteDiffers, samples.Count, maxVoteGapPp));

                // 4. The attribution line (§31's idiom): named, signed, derived.
                var last = samples[samples.Count - 1];
                double divergence = last.Perceived - last.Actual;
                double multiplierPerceived = PerceivedPerformance.IncumbentMultiplier(last.Perceived);
                double multiplierActual = PerceivedPerformance.IncumbentMultiplier(last.Actual);
                sb.Append("\n  ATTRIBUTION (the §31 idiom - every line derived, none authored):\n");
                sb.Append(string.Format(CultureInfo.InvariantCulture,
                    "    perceived economy index          {0,7:F2}\n    actual economy index             {1,7:F2}\n" +
                    "    divergence (perception - truth)  {2,7:+0.00;-0.00;0.00}\n" +
                    "    incumbent multiplier, perceived  {3,7:F4}\n    incumbent multiplier, actual     {4,7:F4}\n" +
                    "    effect of the gap on the incumbent {5,5:+0.00;-0.00;0.00} pp of its own share\n",
                    last.Perceived, last.Actual, divergence, multiplierPerceived, multiplierActual,
                    100.0 * preference[0] * (multiplierPerceived - multiplierActual)));

                failures += Assert(sb, "4. the divergence is reportable as a signed attribution line",
                    Math.Abs(divergence) >= 0.0, $"divergence {divergence:F4} index points at the final sample");

                // 5. P-A2 (Playtest 1, finding 2 - 2026-08-29): the "as published" graph block on
                //    Statistics was a DISPLAY cut. The election model's section-19 reading takes the
                //    PUBLISHED series and never the live state - asserted on the source of
                //    PerceivedPerformance.Perceived itself, so a future edit that quietly reads State
                //    here fails this line rather than passing unnoticed.
                string modelPath = System.IO.Path.GetFullPath(System.IO.Path.Combine(UnityEngine.Application.dataPath, "Scripts", "Elections", "PerceivedPerformance.cs"));
                string modelSource = System.IO.File.ReadAllText(modelPath);
                int perceivedStart = modelSource.IndexOf("public static Reading Perceived(", StringComparison.Ordinal);
                int perceivedEnd = modelSource.IndexOf("public static Reading Actual(", StringComparison.Ordinal);
                string perceivedBody = perceivedStart >= 0 && perceivedEnd > perceivedStart ? modelSource.Substring(perceivedStart, perceivedEnd - perceivedStart) : "";
                failures += Assert(sb, "5. P-A2: PerceivedPerformance.Perceived reads country.Published and never country.State (the display cut changes nothing the election model reads)",
                    perceivedBody.Contains(".Published") && !perceivedBody.Contains(".State."),
                    $"Perceived's body: Published {(perceivedBody.Contains(".Published") ? "read" : "NOT read")}, State {(perceivedBody.Contains(".State.") ? "READ" : "not read")}");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }

            sb.Append($"\n=== PerceivedPerformanceHarness: {(failures == 0 ? "ALL ASSERTIONS PASS" : failures + " FAILED")} ===\n");
            Debug.Log(sb.ToString());
            CheckExit.Finish(failures == 0 ? 0 : 1);
        }

        private static int Assert(StringBuilder sb, string label, bool condition, string detail)
        {
            sb.Append($"  {(condition ? "ok  " : "FAIL")} {label}: {detail}\n");
            return condition ? 0 : 1;
        }
    }
}
