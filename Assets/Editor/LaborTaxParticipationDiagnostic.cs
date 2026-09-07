using System.Collections.Generic;
using PoliSim.Data;
using PoliSim.Simulation;
using UnityEditor;
using UnityEngine;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// FT-5, REVERTED as ruled (2026-09-07, §383; built §372). The sourced term stays in the code and this check guards its status: (1) the term itself
    /// still reads its figures - zero at the seed for six, the elasticity 1 / 8.61, both signs on Sweden at +/-5 points of income tax - so FT-7 (the jobs
    /// lag) finds it whole; (2) the term is INERT - participation after twenty years is identical to 1e-6 whether the income tax is moved +/-5 points or
    /// not (potential within 1e-4 relative against the 2 % the term moved it; participation within a hundredth, the tax's disposable-income channel), because the target
    /// does not read it. A build that wires it back without FT-7 fails here, which is the sheet's fence.
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
            }
            if (Mathf.Abs(MacroSystem.ParticipationElasticityToAfterTaxWage - 1f / 8.61f) > 1e-3f) { Debug.LogError($"LABOR TAX: the elasticity {MacroSystem.ParticipationElasticityToAfterTaxWage:R} is not 1 / 8.61."); ok = false; }
            float[] untouched = Run(CountryId.Sweden, Years, 0f);
            float[] up = Run(CountryId.Sweden, Years, 5f);
            float[] down = Run(CountryId.Sweden, Years, -5f);
            // [0] participation, [1] the term's own value, [2] potential
            if (!(up[1] < 0f) || !(down[1] > 0f)) { Debug.LogError($"LABOR TAX: the term does not read both signs ({up[1]:F4} / {down[1]:F4}) - FT-7 would not find it whole."); ok = false; }
            // the term's channel is the labour input: with the term in the target potential moved 2 % at ±5 points (§372); without it the tax still reaches potential through
            // disposable income → output → unemployment → the discouraged-worker term → participation → the labour input, measured at 3e-5 relative (§383) - so the fence
            // is 1e-4 relative on potential (two hundred times below the term's effect, three times above the other channel) and a hundredth on participation
            if (Mathf.Abs(up[2] - untouched[2]) > 1e-4f * untouched[2] || Mathf.Abs(down[2] - untouched[2]) > 1e-4f * untouched[2]) { Debug.LogError($"LABOR TAX: potential MOVED with the income tax ({untouched[2]:F3} / {up[2]:F3} / {down[2]:F3}) - the term is in the target; FT-5 is REVERTED and sheeted behind FT-7 (§383)."); ok = false; }
            if (Mathf.Abs(up[0] - untouched[0]) > 0.01f || Mathf.Abs(down[0] - untouched[0]) > 0.01f) { Debug.LogError($"LABOR TAX: participation moved more than a hundredth with the income tax ({untouched[0]:F5} / {up[0]:F5} / {down[0]:F5}) - more than the tax's other channel explains."); ok = false; }
            Debug.Log($"LABOR TAX (REVERTED, §383): Sweden after {Years} years - participation {untouched[0]:F4} % untouched, {up[0]:F4} % at +5 points, {down[0]:F4} % at -5 points (the term is inert: potential within 1e-4); the term itself reads {up[1]:F3} / {down[1]:F3} (kept whole for FT-7); potential {untouched[2]:F1} / {up[2]:F1} / {down[2]:F1}.");
            Debug.Log(ok ? "LABOR TAX: PASS - the term is whole and inert; the target does not read it." : "LABOR TAX: FAILED (see above).");
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
                return new[] { c.State.LaborForceParticipationRate, MacroSystem.LaborTaxParticipationTerm(c), c.State.PotentialGDP };
            }
            finally { Object.DestroyImmediate(go); }
        }
    }
}
