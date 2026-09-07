using System.Collections.Generic;
using PoliSim.Data;
using PoliSim.Simulation;
using UnityEditor;
using UnityEngine;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// FT-5 (2026-09-07, §372): participation's response to the labour tax rate. (1) At the seed the term is exactly zero for six and the seed rate is the
    /// income-tax line's. (2) The elasticity is the sourced one (1 ÷ 8.61 = 0.116, SELMA's posterior mean). (3) Both directions on Sweden over twenty years
    /// through the decision: the income tax +5 points → participation LOWER than untouched by about 0.116 × 100 × ln(43/48) = −1.28 points at the target
    /// (the reversion closes most of it in twenty years); −5 points → HIGHER by about +1.19; the term itself reads those figures exactly. (4) The guard: the term
    /// stays inside ±5 at ±20 points of tax.
    /// </summary>
    public static class LaborTaxParticipationDiagnostic
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
                if (Mathf.Abs(MacroSystem.LaborTaxParticipationTerm(c)) > 1e-6f) { Debug.LogError($"LABOR TAX: {c.Id} carries a nonzero term at the seed ({MacroSystem.LaborTaxParticipationTerm(c):R})."); ok = false; }
                float rate = 0f; foreach (TaxLine line in c.TaxLines) { if (line.Type == TaxType.IncomeTax && line.IsImplemented) { rate = line.Rate; } }
                if (Mathf.Abs(c.LaborTaxRateSeed - rate) > 1e-6f) { Debug.LogError($"LABOR TAX: {c.Id}'s seed rate {c.LaborTaxRateSeed} is not its income-tax line's {rate}."); ok = false; }
            }
            if (Mathf.Abs(MacroSystem.ParticipationElasticityToAfterTaxWage - 1f / 8.61f) > 1e-3f) { Debug.LogError($"LABOR TAX: the elasticity {MacroSystem.ParticipationElasticityToAfterTaxWage:R} is not 1 ÷ 8.61."); ok = false; }

            float[] untouched = Run(CountryId.Sweden, Years, 0f);
            float[] up = Run(CountryId.Sweden, Years, 5f);
            float[] down = Run(CountryId.Sweden, Years, -5f);
            // [0] participation, [1] the term, [2] the income tax rate, [3] potential
            float seedRate = untouched[2];
            float expectedUp = 0.116f * 100f * Mathf.Log((100f - (seedRate + 5f)) / (100f - seedRate));
            float expectedDown = 0.116f * 100f * Mathf.Log((100f - (seedRate - 5f)) / (100f - seedRate));
            if (!(up[0] < untouched[0]) || !(down[0] > untouched[0])) { Debug.LogError($"LABOR TAX: +5 points does not lower participation ({up[0]:F3} vs {untouched[0]:F3}) or −5 does not raise it ({down[0]:F3})."); ok = false; }
            if (Mathf.Abs(up[1] - expectedUp) > 1e-3f || Mathf.Abs(down[1] - expectedDown) > 1e-3f) { Debug.LogError($"LABOR TAX: the term reads {up[1]:F4} / {down[1]:F4}, expected {expectedUp:F4} / {expectedDown:F4}."); ok = false; }
            if (Mathf.Abs(untouched[1]) > 1e-6f) { Debug.LogError($"LABOR TAX: untouched Sweden carries a term after {Years} years ({untouched[1]:R}) - the seed rate moved without a decision."); ok = false; }
            float[] far = Run(CountryId.Sweden, 2, 20f);
            if (Mathf.Abs(far[1]) > 5f) { Debug.LogError($"LABOR TAX: the term left its ±5 guard at +20 points ({far[1]:F3})."); ok = false; }

            Debug.Log($"LABOR TAX: Sweden after {Years} years - untouched participation {untouched[0]:F3} % at an income tax of {seedRate:F1} %; +5 points: {up[0]:F3} % (term {up[1]:F3}); −5 points: {down[0]:F3} % (term {down[1]:F3}); +20 points for two years: term {far[1]:F3} (guard ±5). "
                + $"Potential {untouched[3]:F1} / {up[3]:F1} / {down[3]:F1}. The elasticity 0.116 is SELMA's 1 ÷ 8.61.");
            Debug.Log(ok ? "LABOR TAX: PASS - zero at the seed, both directions, the sourced elasticity, inside the guard." : "LABOR TAX: FAILED (see above).");
            CheckExit.Finish(ok ? 0 : 1);
        }

        private static float[] Run(CountryId player, int years, float deltaPoints)
        {
            SimulationRandom.Seed(777);
            World world = WorldFactory.CreateDefault();
            var go = new GameObject("LABORTAX");
            try
            {
                SimulationManager sim = go.AddComponent<SimulationManager>();
                sim.SetWorld(world);
                sim.PlayerCountryId = player;
                Country c = world.GetCountry(player);
                float seedRate = c.LaborTaxRateSeed;
                var decisions = new Dictionary<CountryId, PolicyDecision>();
                foreach (Country k in world.Countries) { decisions[k.Id] = PolicyDecision.None(); }
                for (int year = 1; year <= years; year++)
                {
                    for (int day = 0; day < SimulationManager.DaysPerTurn; day++) { sim.AdvanceDay(); }
                    PolicyDecision d = PolicyDecision.None();
                    if (deltaPoints != 0f) { d.TaxRateOverrides[TaxType.IncomeTax] = seedRate + deltaPoints; }
                    decisions[player] = d;
                    sim.AdvanceTurn(decisions);
                }
                float rate = 0f; foreach (TaxLine line in c.TaxLines) { if (line.Type == TaxType.IncomeTax) { rate = line.Rate; } }
                return new[] { c.State.LaborForceParticipationRate, MacroSystem.LaborTaxParticipationTerm(c), deltaPoints == 0f ? rate : seedRate, c.State.PotentialGDP };
            }
            finally { Object.DestroyImmediate(go); }
        }
    }
}
