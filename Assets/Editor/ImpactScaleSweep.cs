using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using PoliSim.Data;
using PoliSim.Simulation;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// §512 (2026-09-15) - <b>THE IMPACT MULTIPLIER SWEPT ACROSS THE G-SHARE SCALE.</b> Elias's ruling on T-3's impact reading (§507: the block lowered the
    /// harness's landing-year multiplier from 0.5079 to 0.4984 on its weakest dial, below the ratchet 0.5074): the ratchet is not re-based on one reading; the
    /// multiplier is swept across k (<c>Country.GovernmentConsumptionScale</c>) from 1, where the code is the tree before the block, to the seeded value. A
    /// continuous curve means the mechanism is real and the ratchet re-bases with the curve recorded; a step means a seam to fix. Ramey's band holds at L+1 and
    /// L+4, so this is the impact horizon's question alone.
    ///
    /// <para><b>The experiment is the harness's own</b> (<see cref="ResponsivenessAuditHarness.RunCase"/>: Sweden, seed 777, the year's dice off, each spending
    /// dial set once as a permanent level shift, purchases on the identity's G): only k differs, every country's at once, set before the first day by
    /// <see cref="ResponsivenessAuditHarness.ScaleBlend"/>. At a blend of 1 the seeded k is left untouched (no arithmetic on it), at 0 it is exactly 1.</para>
    ///
    /// <para><b>The continuity test.</b> On an even grid, each dial's first differences; an interval whose change exceeds three times the median change (and a
    /// ten-thousandth) is bisected eight times toward the half that carries the change. A continuous curve's change shrinks with the interval; a step's does not,
    /// so a final change above half the interval's first one is named a STEP at the k where it sits. It measures and changes nothing.</para>
    ///
    /// Run: <c>-executeMethod PoliSim.EditorTools.ImpactScaleSweep.Run [-sweepsteps=20]</c>
    /// </summary>
    public static class ImpactScaleSweep
    {
        private sealed class Point
        {
            public float T;
            public readonly float[] Impact = new float[3];
            public readonly float[] L1 = new float[3];
            public readonly float[] L4 = new float[3];
            public readonly float[] PurchaseImpulse = new float[3];
            public int Landing;
        }

        private static readonly ResponsivenessAuditHarness.Dial[] Dials =
        {
            new ResponsivenessAuditHarness.Dial { Name = "Spending +2%", Kind = ResponsivenessAuditHarness.Kind.Spending, Step = 2f },
            new ResponsivenessAuditHarness.Dial { Name = "Spending +10%", Kind = ResponsivenessAuditHarness.Kind.Spending, Step = 10f },
            new ResponsivenessAuditHarness.Dial { Name = "Spending -10%", Kind = ResponsivenessAuditHarness.Kind.Spending, Step = -10f },
        };

        private const int Bisections = 8;
        private static int _measured;

        public static void Run()
        {
            CheckExit.ArmLogFold();
            EventSystem.Enabled = false;   // the harness's own condition (§404)
            int steps = 20;
            foreach (string arg in Environment.GetCommandLineArgs())
            {
                if (arg.StartsWith("-sweepsteps=", StringComparison.Ordinal)) { int.TryParse(arg.Substring("-sweepsteps=".Length), NumberStyles.Integer, CultureInfo.InvariantCulture, out steps); }
            }
            steps = Math.Max(2, steps);

            // the seeded k, read off a throwaway world the way the harness reads its seeded rates
            var ks = new Dictionary<CountryId, float>();
            var probeGo = new GameObject("§512 SEED PROBE");
            try { foreach (Country c in WorldFactory.CreateDefault().Countries) { ks[c.Id] = c.GovernmentConsumptionScale; } }
            finally { UnityEngine.Object.DestroyImmediate(probeGo); }

            var sb = new StringBuilder();
            sb.Append("=== §512: THE IMPACT MULTIPLIER ACROSS THE G-SHARE SCALE - k blended from 1 to each country's seed; the harness's experiment, only k different ===\n");
            sb.Append("    the seeded k: ");
            foreach (KeyValuePair<CountryId, float> p in ks) { sb.Append(F("{0} {1:F4}  ", p.Key, p.Value)); }
            sb.Append('\n');

            var grid = new List<Point>();
            for (int i = 0; i <= steps; i++) { grid.Add(Measure(i == steps ? 1f : (float)i / steps)); }

            sb.Append("    blend   k(Sweden) |  impact: +2%     +10%     -10%  |  cum L+1: +2%   +10%   -10% |  cum L+4: +2%   +10%   -10% | purchases impulse +2%\n");
            float kSe = ks[CountryId.Sweden];
            foreach (Point p in grid)
            {
                sb.Append(F("    {0,5:F3}  {1,9:F4} | {2,8:F4} {3,8:F4} {4,8:F4} | {5,6:F3} {6,6:F3} {7,6:F3} | {8,6:F3} {9,6:F3} {10,6:F3} | {11,8:F4}\n",
                    p.T, KAt(p.T, kSe), p.Impact[0], p.Impact[1], p.Impact[2], p.L1[0], p.L1[1], p.L1[2], p.L4[0], p.L4[1], p.L4[2], p.PurchaseImpulse[0]));
                Debug.Log(F("SWEEP|{0:R}|{1:R}|{2:R}|{3:R}|{4:R}|{5:R}|{6:R}|{7:R}|{8:R}|{9:R}|{10:R}|{11:R}|{12}", p.T, KAt(p.T, kSe),
                    p.Impact[0], p.Impact[1], p.Impact[2], p.L1[0], p.L1[1], p.L1[2], p.L4[0], p.L4[1], p.L4[2], p.PurchaseImpulse[0], p.Landing));
            }

            // the continuity test, dial by dial
            bool anyStep = false;
            for (int j = 0; j < Dials.Length; j++)
            {
                var deltas = new List<float>();
                for (int i = 0; i + 1 < grid.Count; i++) { deltas.Add(grid[i + 1].Impact[j] - grid[i].Impact[j]); }
                var sorted = new List<float>(); foreach (float d in deltas) { sorted.Add(Math.Abs(d)); }
                sorted.Sort();
                float median = sorted[sorted.Count / 2];
                int rising = 0, falling = 0;
                foreach (float d in deltas) { if (d > 0f) { rising++; } else if (d < 0f) { falling++; } }
                float maxRatio = 0f;
                int candidates = 0;
                for (int i = 0; i < deltas.Count; i++)
                {
                    float ratio = median > 0f ? Math.Abs(deltas[i]) / median : 0f;
                    maxRatio = Math.Max(maxRatio, ratio);
                    if (Math.Abs(deltas[i]) <= 3f * median || Math.Abs(deltas[i]) <= 1e-4f) { continue; }
                    candidates++;
                    float a = grid[i].T, b = grid[i + 1].T, va = grid[i].Impact[j], vb = grid[i + 1].Impact[j];
                    float first = Math.Abs(vb - va);
                    for (int s = 0; s < Bisections; s++)
                    {
                        float m = 0.5f * (a + b);
                        float vm = Measure(m).Impact[j];
                        if (Math.Abs(vm - va) >= Math.Abs(vb - vm)) { b = m; vb = vm; } else { a = m; va = vm; }
                    }
                    float last = Math.Abs(vb - va);
                    bool step = last > 0.5f * first;
                    anyStep |= step;
                    string line = F("SWEEP CANDIDATE|{0}|{1:R}|{2:R}|{3:R}|{4:R}|{5:R}|{6}", Dials[j].Name, grid[i].T, grid[i + 1].T, first, last, b - a, step ? "STEP" : "CONTINUOUS");
                    Debug.Log(line);
                    sb.Append(F("    {0}: the change {1:F5} between blends {2:F3} and {3:F3} (k {4:F4} to {5:F4}) - bisected {6} times to a width of {7:F6}, the change {8:F6}: {9}\n",
                        Dials[j].Name, first, grid[i].T, grid[i + 1].T, KAt(grid[i].T, kSe), KAt(grid[i + 1].T, kSe), Bisections, b - a, last,
                        step ? F("a STEP at k {0:F5}", KAt(0.5f * (a + b), kSe)) : "it shrinks with the interval - continuous"));
                }
                string verdict = candidates == 0 ? "CONTINUOUS (no interval's change above three times the median)" : "see the candidates above";
                Debug.Log(F("SWEEP DIAL|{0}|{1:R}|{2:R}|{3}|{4}|{5}|{6}", Dials[j].Name, median, maxRatio, rising, falling, candidates, verdict));
                sb.Append(F("    {0}: median change per grid step {1:F5}, the largest {2:F2} times it, {3} rising and {4} falling steps, {5} interval(s) bisected - {6}\n",
                    Dials[j].Name, median, maxRatio, rising, falling, candidates, verdict));
            }

            sb.Append(F("    cases run: {0} points (grid and bisections), four runs each.\n", _measured));
            sb.Append(anyStep ? "    VERDICT: A STEP - a seam, not a mechanism; the ratchet is not re-based.\n" : "    VERDICT: CONTINUOUS on every dial - the mechanism is real.\n");
            Debug.Log(sb.ToString());
            Debug.Log(anyStep ? "=== ImpactScaleSweep: STEP FOUND ===" : "=== ImpactScaleSweep: CONTINUOUS ===");
            CheckExit.Finish(0);
        }

        private static float KAt(float t, float kSeed) => t >= 1f ? kSeed : t <= 0f ? 1f : 1f + t * (kSeed - 1f);

        /// <summary>One point: the harness's baseline and its three spending dials at a blend, read the harness's way - the landing year the first whose
        /// balance moves, impact and the cumulative L+1 and L+4 on the identity's purchases.</summary>
        private static Point Measure(float t)
        {
            _measured++;
            int years = ResponsivenessAuditHarness.Years;
            ResponsivenessAuditHarness.ScaleBlend = t >= 1f ? (float?)null : t;
            var point = new Point { T = t };
            try
            {
                float[] baseGdp = new float[years + 1], baseBudget = new float[years + 1], baseU = new float[years + 1], baseInfl = new float[years + 1], basePurchases = new float[years + 1];
                ResponsivenessAuditHarness.RunCase(null, baseGdp, baseBudget, baseU, baseInfl, basePurchases);
                for (int j = 0; j < Dials.Length; j++)
                {
                    float[] gdp = new float[years + 1], budget = new float[years + 1], u = new float[years + 1], infl = new float[years + 1], purchases = new float[years + 1];
                    ResponsivenessAuditHarness.RunCase(Dials[j], gdp, budget, u, infl, purchases);
                    int landing = 0;
                    for (int y = 1; y <= years; y++) { if (Mathf.Abs(budget[y] - baseBudget[y]) > 1e-3f) { landing = y; break; } }
                    if (landing == 0) { landing = 1; }
                    int h2 = Mathf.Min(years, landing + 1), h5 = Mathf.Min(years, landing + 4);
                    point.Landing = landing;
                    point.PurchaseImpulse[j] = purchases[landing] - basePurchases[landing];
                    if (Mathf.Abs(point.PurchaseImpulse[j]) <= 1e-3f) { Debug.LogError(F("§512: {0} at blend {1:R} moved purchases by nothing at landing - no multiplier.", Dials[j].Name, t)); }
                    point.Impact[j] = ResponsivenessAuditHarness.Cumulative(gdp, baseGdp, purchases, basePurchases, landing, landing);
                    point.L1[j] = ResponsivenessAuditHarness.Cumulative(gdp, baseGdp, purchases, basePurchases, landing, h2);
                    point.L4[j] = ResponsivenessAuditHarness.Cumulative(gdp, baseGdp, purchases, basePurchases, landing, h5);
                }
            }
            finally
            {
                ResponsivenessAuditHarness.ScaleBlend = null;
            }
            return point;
        }

        private static string F(string format, params object[] args) => string.Format(CultureInfo.InvariantCulture, format, args);
    }
}
