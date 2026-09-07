using System.Collections.Generic;
using PoliSim.Data;
using PoliSim.Simulation;
using UnityEditor;
using UnityEngine;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// §385 (2026-09-07): the decaying-expectations form. (1) The unit: with the print equal to the expectation, an expectation 5 points above the target
    /// closes exactly ExpectationsAnchoringRate of the distance each step, both directions (−5 too), to 1e-4 over ten steps; at the target it does not move;
    /// a caller that passes no anchor keeps the old form (unchanged to 1e-6). (2) The constant is 0.25, with its ten-year memory 0.5 × 0.75^10 = 2.8 % as the
    /// header derives. (3) The world: no player, seed 777, 300 turns - the mean inflation over turns 100-300 sits within 1.5 points of the zone's target for
    /// six (§379 measured 5-8 % at year 300 before), and the print is stated per country.
    /// </summary>
    public static class ExpectationsAnchoringDiagnostic
    {
        private const int Turns = 300;

        public static void Run()
        {
            CheckExit.ArmLogFold();
            bool ok = true;
            // (1) the unit
            float rate = MacroSystem.ExpectationsAnchoringRate;
            foreach (float start in new[] { 7f, -3f })
            {
                var s = new EconomyState { Inflation = 2f, InflationExpectations = start };
                float distance = start - 2f;
                for (int i = 0; i < 10; i++)
                {
                    s.Inflation = s.InflationExpectations;   // the adaptive step is zero: only the anchor moves it
                    MacroSystem.ApplyInflationExpectations(s, anchorPercent: 2f);
                    distance *= 1f - rate;
                    if (Mathf.Abs(s.InflationExpectations - (2f + distance)) > 1e-4f) { Debug.LogError($"ANCHORING: from {start} the expectation reads {s.InflationExpectations:F5} after step {i + 1}, expected {2f + distance:F5}."); ok = false; break; }
                }
            }
            var atTarget = new EconomyState { Inflation = 2f, InflationExpectations = 2f };
            MacroSystem.ApplyInflationExpectations(atTarget, anchorPercent: 2f);
            if (Mathf.Abs(atTarget.InflationExpectations - 2f) > 1e-6f) { Debug.LogError($"ANCHORING: at the target the expectation moved to {atTarget.InflationExpectations:R}."); ok = false; }
            var oldForm = new EconomyState { Inflation = 4f, InflationExpectations = 6f };
            MacroSystem.ApplyInflationExpectations(oldForm);
            if (Mathf.Abs(oldForm.InflationExpectations - 5f) > 1e-6f) { Debug.LogError($"ANCHORING: a caller without an anchor no longer gets the adaptive form (read {oldForm.InflationExpectations:R}, expected 5)."); ok = false; }
            // (2) the constant
            float memory = 0.5f * Mathf.Pow(1f - rate, 10f);
            if (Mathf.Abs(rate - 0.25f) > 1e-6f) { Debug.LogError($"ANCHORING: the rate is {rate:R}, not the derived 0.25."); ok = false; }
            // (3) the world
            SimulationRandom.Seed(777);
            World world = WorldFactory.CreateDefault();
            var go = new GameObject("ANCHORING");
            var report = new List<string>();
            try
            {
                SimulationManager sim = go.AddComponent<SimulationManager>();
                sim.SetWorld(world);
                var decisions = new Dictionary<CountryId, PolicyDecision>();
                foreach (Country k in world.Countries) { decisions[k.Id] = PolicyDecision.None(); }
                var sum = new Dictionary<CountryId, double>(); var n = 0;
                foreach (Country k in world.Countries) { sum[k.Id] = 0; }
                for (int year = 1; year <= Turns; year++)
                {
                    for (int day = 0; day < SimulationManager.DaysPerTurn; day++) { sim.AdvanceDay(); }
                    sim.AdvanceTurn(decisions);
                    if (year >= 100) { n++; foreach (Country k in world.Countries) { sum[k.Id] += k.State.Inflation; } }
                }
                foreach (Country c in world.Countries)
                {
                    float target = c.CurrencyZone != null ? c.CurrencyZone.InflationTarget : TaylorRule.DefaultInflationTarget;
                    float mean = (float)(sum[c.Id] / n);
                    if (Mathf.Abs(mean - target) > 1.5f) { Debug.LogError($"ANCHORING: {c.Id} mean inflation over turns 100-{Turns} is {mean:F2} against a target of {target:F2} - the anchor does not hold."); ok = false; }
                    report.Add($"{c.Id} {mean:F2} (target {target:F1}, year {Turns} {c.State.Inflation:F2}, expectations {c.State.InflationExpectations:F2})");
                }
            }
            finally { Object.DestroyImmediate(go); }
            Debug.Log($"ANCHORING: rate {rate:F2}, ten-year memory of a surprise {memory * 100f:F1} %; the unit holds both ways; mean inflation over turns 100-{Turns}: {string.Join("; ", report)}.");
            Debug.Log(ok ? "ANCHORING: PASS - the target holds exactly, a shock decays at the rate both ways, the old form is untouched for a caller without an anchor, the long horizon reads near target." : "ANCHORING: FAILED (see above).");
            CheckExit.Finish(ok ? 0 : 1);
        }
    }
}
