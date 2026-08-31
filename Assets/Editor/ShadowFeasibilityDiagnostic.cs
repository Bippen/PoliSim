using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using PoliSim.Data;
using PoliSim.Simulation;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// C-C9 (P-G1) — **the two questions that decide how a shadow baseline can be built, measured before
    /// anything is built.**
    ///
    /// <para>The shadow run is a second simulation advancing beside the real one so every graph can show
    /// *"with your policies"* against *"without"*. Two things decide its shape, and neither is safe to
    /// assume:</para>
    ///
    /// <list type="number">
    /// <item><description>⚠ <b>DOES ADVANCING A SHADOW CONSUME THE REAL GAME'S RANDOMNESS?</b>
    /// `SimulationRandom` is a global static with one `CountingRandom` per stream, and the save layer
    /// persists those draw counts precisely because their position is load-bearing. If a shadow turn
    /// draws, then merely LOOKING at the counterfactual changes the real game's future — a determinism
    /// break of the class W-G2 measured, introduced by a display feature. If it draws nothing, the
    /// shadow is free of interference and the design is simple.</description></item>
    /// <item><description><b>WHAT DOES A SHADOW TURN COST?</b> The pre-ruling is explicit: if the
    /// per-turn cost exceeds a stated budget, report the cost and ship behind a flag rather than
    /// optimising blind. <b>A measured cost is the deliverable, not a fast one.</b></description></item>
    /// </list>
    ///
    /// <para>This answers both and asserts nothing about what the answer should be.</para>
    /// </summary>
    public static class ShadowFeasibilityDiagnostic
    {
        private const int MeasuredTurns = 10;

        public static void Run()
        {
            CheckExit.ArmLogFold();

            var sb = new StringBuilder();
            sb.Append("=== C-C9 (P-G1): can a shadow run advance without disturbing the real one, and what does it cost? ===\n");

            var go = new GameObject("C-C9 SHADOW");
            int failures = 0;
            try
            {
                SimulationRandom.Seed(777);
                World real = WorldFactory.CreateDefault();
                SimulationManager sim = go.AddComponent<SimulationManager>();
                sim.SetWorld(real);

                var none = new Dictionary<CountryId, PolicyDecision>();
                foreach (Country c in real.Countries) { none[c.Id] = PolicyDecision.None(); }

                // Settle one real turn so the streams are in a working position rather than at seed.
                for (int d = 0; d < SimulationManager.DaysPerTurn; d++) { sim.AdvanceDay(); }
                sim.AdvanceTurn(none);

                // ---- Question 1: does a shadow turn draw? ----
                Dictionary<SimulationRandom.Stream, int> before = SimulationRandom.CaptureDrawCounts();

                var shadowGo = new GameObject("C-C9 SHADOW WORLD");
                World shadow = WorldFactory.CreateDefault();
                SimulationManager shadowSim = shadowGo.AddComponent<SimulationManager>();
                shadowSim.SetWorld(shadow);

                var shadowNone = new Dictionary<CountryId, PolicyDecision>();
                foreach (Country c in shadow.Countries) { shadowNone[c.Id] = PolicyDecision.None(); }

                for (int d = 0; d < SimulationManager.DaysPerTurn; d++) { shadowSim.AdvanceDay(); }
                shadowSim.AdvanceTurn(shadowNone);

                Dictionary<SimulationRandom.Stream, int> after = SimulationRandom.CaptureDrawCounts();

                sb.Append("\n--- 1. Draws consumed by ONE shadow turn, per stream ---\n");
                int totalDrawn = 0;
                foreach (KeyValuePair<SimulationRandom.Stream, int> kv in after)
                {
                    before.TryGetValue(kv.Key, out int was);
                    int delta = kv.Value - was;
                    if (delta == 0) { continue; }

                    totalDrawn += delta;
                    sb.Append(F("    {0,-24} {1,8} draw(s)\n", kv.Key, delta));
                }

                if (totalDrawn == 0)
                {
                    sb.Append("    NONE. A shadow turn consumes no randomness at all, so running one cannot\n");
                    sb.Append("    move the real game's streams and the counterfactual is free of interference.\n");
                }
                else
                {
                    sb.Append(F("    ⚠ {0} DRAW(S) TOTAL. A shadow turn DOES consume the real game's randomness, so\n", totalDrawn));
                    sb.Append("    a naive shadow would make merely LOOKING at the counterfactual change the real\n");
                    sb.Append("    game's future. The shadow must therefore run inside a capture/restore of the\n");
                    sb.Append("    draw counts (SimulationRandom.CaptureDrawCounts / RestoreState, the pair the\n");
                    sb.Append("    save layer already relies on), and that restore is part of the per-turn cost.\n");
                }

                // ---- Question 2: what does it cost? ----
                var watch = new Stopwatch();
                watch.Start();
                for (int t = 0; t < MeasuredTurns; t++)
                {
                    for (int d = 0; d < SimulationManager.DaysPerTurn; d++) { shadowSim.AdvanceDay(); }
                    shadowSim.AdvanceTurn(shadowNone);
                }

                watch.Stop();
                double perTurnMs = watch.Elapsed.TotalMilliseconds / MeasuredTurns;

                var realWatch = new Stopwatch();
                realWatch.Start();
                for (int t = 0; t < MeasuredTurns; t++)
                {
                    for (int d = 0; d < SimulationManager.DaysPerTurn; d++) { sim.AdvanceDay(); }
                    sim.AdvanceTurn(none);
                }

                realWatch.Stop();
                double realPerTurnMs = realWatch.Elapsed.TotalMilliseconds / MeasuredTurns;

                sb.Append(F("\n--- 2. Cost, {0} turns each, same machine, same process ---\n", MeasuredTurns));
                sb.Append(F("    a real turn   {0,8:F1} ms\n", realPerTurnMs));
                sb.Append(F("    a shadow turn {0,8:F1} ms\n", perTurnMs));
                sb.Append(F("    ⚠ A shadow doubles the per-turn cost to about {0:F1} ms, which is what running two\n", realPerTurnMs + perTurnMs));
                sb.Append("      simulations means. Whether that is affordable is a budget question for the\n");
                sb.Append("      caller, and this states the figure rather than assuming an answer.\n");

                UnityEngine.Object.DestroyImmediate(shadowGo);

                sb.Append("\n--- 3. What this settles for the build ---\n");
                sb.Append(totalDrawn == 0
                    ? "    The shadow can advance in-process with no RNG protection. Its remaining work is\n      wiring, history and the graph overlay.\n"
                    : "    The shadow MUST be wrapped in a draw-count capture/restore, and that wrapper is the\n      first thing to build and the first thing to assert - a shadow that silently shifts the\n      real game's streams would be a determinism break shipped as a feature.\n");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }

            sb.Append(F("\n=== ShadowFeasibilityDiagnostic: {0} ===\n",
                failures == 0 ? "MEASURED" : failures + " FAILURE(S)"));

            Debug.Log(sb.ToString());
            CheckExit.Finish(failures == 0 ? 0 : 1);
        }

        private static string F(string format, params object[] args)
            => string.Format(CultureInfo.InvariantCulture, format, args);
    }
}
