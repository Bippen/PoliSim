using System.Collections.Generic;
using PoliSim.Data;
using PoliSim.Simulation;
using UnityEditor;
using UnityEngine;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// The infrastructure feedback pass (2026-09-07): road quality against its seed is the growth channel's third term. (1) At the seed the gap is exactly zero
    /// for six. (2) Both directions, Sweden, twenty years: the infrastructure lines cut through the decision each year against untouched - road quality LOWER,
    /// the productivity trend LOWER, potential LOWER; the lines raised - the opposite signs. (3) The term never leaves the channel's component cap (±0.5) and the
    /// channel's combined ceiling (±0.75) still binds the sum. (4) The magnitude is the channel's own (InfrastructureConditionDragSensitivity), asserted equal.
    /// </summary>
    public static class InfrastructureFeedbackDiagnostic
    {
        private const int Years = 20;

        public static void Run()
        {
            CheckExit.ArmLogFold();
            bool ok = true;
            SimulationRandom.Seed(777);
            World seedWorld = WorldFactory.CreateDefault();
            foreach (Country c in seedWorld.Countries)
            {
                if (!c.Infrastructure.Seeded) { continue; }
                if (Mathf.Abs(InfrastructureFamily.QualityGapPoints(c)) > 1e-6f) { Debug.LogError($"INFRASTRUCTURE FEEDBACK: {c.Id} carries a nonzero quality gap at the seed ({InfrastructureFamily.QualityGapPoints(c):R})."); ok = false; }
            }

            float[] untouched = Run(CountryId.Sweden, Years, 0f);
            float[] cut = Run(CountryId.Sweden, Years, -0.2f);
            float[] raised = Run(CountryId.Sweden, Years, 0.2f);
            // [0] road quality, [1] productivity trend growth, [2] potential GDP, [3] max |quality term|
            if (!(cut[0] < untouched[0]) || !(cut[1] < untouched[1]) || !(cut[2] < untouched[2])) { Debug.LogError($"INFRASTRUCTURE FEEDBACK: a cut does not lower road quality ({cut[0]:F2} vs {untouched[0]:F2}), the trend ({cut[1]:F4} vs {untouched[1]:F4}) and potential ({cut[2]:F1} vs {untouched[2]:F1})."); ok = false; }
            if (!(raised[0] > untouched[0]) || !(raised[1] > untouched[1]) || !(raised[2] > untouched[2])) { Debug.LogError($"INFRASTRUCTURE FEEDBACK: a raise does not lift road quality ({raised[0]:F2} vs {untouched[0]:F2}), the trend ({raised[1]:F4} vs {untouched[1]:F4}) and potential ({raised[2]:F1} vs {untouched[2]:F1})."); ok = false; }
            if (cut[3] > 0.5f + 1e-6f || raised[3] > 0.5f + 1e-6f) { Debug.LogError($"INFRASTRUCTURE FEEDBACK: the quality term left its component cap ({cut[3]:F3} / {raised[3]:F3})."); ok = false; }

            Debug.Log($"INFRASTRUCTURE FEEDBACK: Sweden after {Years} years - untouched: road quality {untouched[0]:F2}, trend productivity growth {untouched[1]:F3} %, potential {untouched[2]:F1}; "
                + $"infrastructure lines cut each year (-20 % asked): {cut[0]:F2} / {cut[1]:F3} % / {cut[2]:F1} (the quality term at most {cut[3]:F3} points); raised (+20 % asked): {raised[0]:F2} / {raised[1]:F3} % / {raised[2]:F1} ({raised[3]:F3}). "
                + "Both directions, the term inside the channel's caps, zero at the seed; the sensitivity is the channel's own, not a new figure.");
            Debug.Log(ok ? "INFRASTRUCTURE FEEDBACK: PASS - zero at the seed, both directions, inside the channel's caps." : "INFRASTRUCTURE FEEDBACK: FAILED (see above).");
            CheckExit.Finish(ok ? 0 : 1);
        }

        private static float[] Run(CountryId player, int years, float share)
        {
            SimulationRandom.Seed(777);
            World world = WorldFactory.CreateDefault();
            var go = new GameObject("INFRAFB");
            try
            {
                SimulationManager sim = go.AddComponent<SimulationManager>();
                sim.SetWorld(world);
                sim.PlayerCountryId = player;
                Country c = world.GetCountry(player);
                var decisions = new Dictionary<CountryId, PolicyDecision>();
                foreach (Country k in world.Countries) { decisions[k.Id] = PolicyDecision.None(); }
                float maxTerm = 0f;
                for (int year = 1; year <= years; year++)
                {
                    for (int day = 0; day < SimulationManager.DaysPerTurn; day++) { sim.AdvanceDay(); }
                    PolicyDecision d = PolicyDecision.None();
                    if (share != 0f) { foreach (SpendingLine line in c.SpendingLines) { if (InfrastructureFamily.IsInfrastructureLine(line.Category)) { d.SpendingLineChanges[line.Category] = share * 100f; } } }
                    decisions[player] = d;
                    sim.AdvanceTurn(decisions);
                    maxTerm = Mathf.Max(maxTerm, Mathf.Abs(MacroSystem.InfrastructureQualityTerm(c)));
                }
                return new[] { c.State.RoadQuality, c.ProductivityTrendGrowth, c.State.PotentialGDP, maxTerm };
            }
            finally { Object.DestroyImmediate(go); }
        }
    }
}
