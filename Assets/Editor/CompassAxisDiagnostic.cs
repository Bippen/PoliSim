using System.Collections.Generic;
using PoliSim.Data;
using PoliSim.UI;
using UnityEditor;
using UnityEngine;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// Playtest 3, finding 1 (2026-08-27): the Political Compass "only appears to operate on the
    /// x-axis". Two candidate causes that must be separated BEFORE anything is touched: a MODEL
    /// cause (the Y term blends average sector RegulationLevel with average IMPLEMENTED welfare
    /// generosity - if no seed implements a welfare program, half of the blend is a constant 0 and
    /// the other half may be a constant seed value) or a PLOT cause (the renderer auto-scales each
    /// axis to the observed min/max, padded, so a real spread would fill the plot - unless a clamp
    /// or the padding rule collapses it).
    ///
    /// <para><b>WHAT THIS ENUMERATES</b> (rule 14): the six countries of <c>WorldFactory.CreateDefault()</c>
    /// at turn 0 - the state the Editor session opened on. For each: the per-sector RegulationLevel
    /// values, the implemented-welfare count and generosity mean, the raw Y and X axis values from
    /// <see cref="PoliticalCompassRenderer.GetRegulationWelfareAxisValue"/> /
    /// <see cref="PoliticalCompassRenderer.GetFiscalSizeAxisValue"/>, then the plotted position
    /// each value lands at under the renderer's own padding and InverseLerp rule (replicated here
    /// verbatim from its private helpers, with a reference 600px-tall plot) - the y the dot is drawn at.</para>
    ///
    /// Read-only: builds a world in memory, draws nothing, changes nothing.
    /// </summary>
    public static class CompassAxisDiagnostic
    {
        private const float ReferencePlotHeight = 600f;

        public static void Run()
        {
            CheckExit.ArmLogFold();
            World world = WorldFactory.CreateDefault();
            var countries = world.Countries;
            if (countries == null || countries.Count == 0)
            {
                Debug.LogError("  EMPTY ENUMERATION - WorldFactory.CreateDefault() returned no countries. VERIFIED NOTHING.");
                CheckExit.Finish(1);
                return;
            }

            Debug.Log("=== COMPASS: raw axis inputs at turn 0 (WorldFactory.CreateDefault) ===");
            var xs = new float[countries.Count];
            var ys = new float[countries.Count];
            for (int i = 0; i < countries.Count; i++)
            {
                Country c = countries[i];
                var regs = new List<string>();
                float regSum = 0f;
                foreach (Sector s in c.Sectors) { regs.Add($"{s.Type}={s.RegulationLevel:F1}"); regSum += s.RegulationLevel; }
                float avgReg = c.Sectors.Count > 0 ? regSum / c.Sectors.Count : 50f;

                int implemented = 0, total = 0;
                float genSum = 0f;
                var welfare = new List<string>();
                foreach (WelfareProgram p in c.WelfarePrograms)
                {
                    total++;
                    welfare.Add($"{p.Type}={(p.IsImplemented ? "ON" : "off")}@{p.GenerosityLevel:F0}");
                    if (!p.IsImplemented) continue;
                    implemented++;
                    genSum += p.GenerosityLevel;
                }
                float avgGen = implemented > 0 ? genSum / implemented : 0f;

                xs[i] = PoliticalCompassRenderer.GetFiscalSizeAxisValue(c);
                ys[i] = PoliticalCompassRenderer.GetRegulationWelfareAxisValue(c);

                Debug.Log($"COMPASS {c.Id,-8} sectors={c.Sectors.Count} avgRegulation={avgReg:F2} [{string.Join(" ", regs)}]");
                Debug.Log($"COMPASS {c.Id,-8} welfare implemented={implemented}/{total} avgGenerosity(implemented)={avgGen:F2} [{string.Join(" ", welfare)}]");
                Debug.Log($"COMPASS {c.Id,-8} Y raw = ({avgReg:F2} + {avgGen:F2}) / 2 = {ys[i]:F3}   X raw = {xs[i]:F3}");
            }

            float minX = float.MaxValue, maxX = float.MinValue, minY = float.MaxValue, maxY = float.MinValue;
            for (int i = 0; i < countries.Count; i++)
            {
                minX = Mathf.Min(minX, xs[i]); maxX = Mathf.Max(maxX, xs[i]);
                minY = Mathf.Min(minY, ys[i]); maxY = Mathf.Max(maxY, ys[i]);
            }
            float rawRangeX = maxX - minX, rawRangeY = maxY - minY;
            PadRange(ref minX, ref maxX);
            PadRange(ref minY, ref maxY);

            Debug.Log($"=== COMPASS: observed spread - X raw range {rawRangeX:F3} (padded {minX:F2}..{maxX:F2}), " +
                      $"Y raw range {rawRangeY:F3} (padded {minY:F2}..{maxY:F2}); the renderer pads 15% of the range, or a flat ±5 when the range is under 1 ===");

            Debug.Log($"=== COMPASS: plotted positions on a reference {ReferencePlotHeight:F0}px plot (ty = InverseLerp over the padded Y range; pixel y from the TOP) ===");
            for (int i = 0; i < countries.Count; i++)
            {
                float tx = maxX > minX ? Mathf.InverseLerp(minX, maxX, xs[i]) : 0.5f;
                float ty = maxY > minY ? Mathf.InverseLerp(minY, maxY, ys[i]) : 0.5f;
                float py = (1f - ty) * ReferencePlotHeight;
                Debug.Log($"COMPASS {countries[i].Id,-8} tx={tx:F3} ty={ty:F3} -> pixel y {py:F1} of {ReferencePlotHeight:F0}");
            }

            float minPy = float.MaxValue, maxPy = float.MinValue;
            for (int i = 0; i < countries.Count; i++)
            {
                float ty = maxY > minY ? Mathf.InverseLerp(minY, maxY, ys[i]) : 0.5f;
                float py = (1f - ty) * ReferencePlotHeight;
                minPy = Mathf.Min(minPy, py); maxPy = Mathf.Max(maxPy, py);
            }
            string verdict = rawRangeY < 1f
                ? "MODEL: the six Y values sit within one axis unit, so the ±5 flat pad places every dot in a band of " +
                  $"{(maxPy - minPy):F1}px on a {ReferencePlotHeight:F0}px plot - a horizontal line by construction, not a plot defect"
                : $"PLOT-SIDE QUESTION: Y varies by {rawRangeY:F2} units and the auto-scale spreads that over {(maxPy - minPy):F1}px of {ReferencePlotHeight:F0} - a real spread that reaches the screen";
            Debug.Log($"=== COMPASS VERDICT (turn 0): {verdict} ===");
            CheckExit.Finish(0);
        }

        // Replicated verbatim from PoliticalCompassRenderer.PadRange (private) - read-only diagnosis
        // must not touch the renderer. If the renderer's rule changes, this copy is the stale one.
        private static void PadRange(ref float min, ref float max)
        {
            float range = max - min;
            float pad = range < 1f ? 5f : range * 0.15f;
            min -= pad;
            max += pad;
        }
    }
}
