using System.Collections.Generic;
using PoliSim.Data;
using PoliSim.Simulation;
using UnityEditor;
using UnityEngine;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// FT-5, LANDED as ruled (2026-09-08, §396; built §372, reverted §383, re-measured §392 and §395). (1) At the seed the term is exactly zero for six and the
    /// seed rate is the income-tax line's; the elasticity is 1 ÷ 8.61 = 0.116 (SELMA's posterior mean). (2) Sweden the player, eight turns, a 5-point income-tax
    /// CUT from turn 3 against an untouched run: participation HIGHER two years on (the Frisch response), UNEMPLOYMENT HIGHER two years on (the new participants
    /// arrive before the jobs - the first seat, SELMA's sign), and potential HIGHER four years on (they are employed at 0.40 a year and reach potential only then -
    /// the second seat); a 5-point RISE the mirror on all three. (3) The term itself reads 0.116 × 100 × ln((100 − t) ÷ (100 − t₀)) exactly at both rates. (4) The
    /// guard: the term stays inside ±5 at +20 points of tax.
    /// </summary>
    public static class LaborTaxParticipationDiagnostic
    {
        private const int Turns = 8;
        private const int Landing = 3;

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

            float[] untouched = Run(0f);
            float[] cut = Run(-5f);
            float[] rise = Run(5f);
            // per run: [0] participation L+2, [1] unemployment L+2, [2] potential L+4, [3] the term at the end, [4] the seed rate
            float seedRate = untouched[4];
            float expectedCut = 0.116f * 100f * Mathf.Log((100f - (seedRate - 5f)) / (100f - seedRate));
            float expectedRise = 0.116f * 100f * Mathf.Log((100f - (seedRate + 5f)) / (100f - seedRate));
            if (!(cut[0] > untouched[0]) || !(rise[0] < untouched[0])) { Debug.LogError($"LABOR TAX: participation two years on does not answer the rate both ways (cut {cut[0]:F3}, untouched {untouched[0]:F3}, rise {rise[0]:F3})."); ok = false; }
            if (!(cut[1] > untouched[1]) || !(rise[1] < untouched[1])) { Debug.LogError($"LABOR TAX: unemployment two years on does not RISE on a cut and fall on a rise (cut {cut[1]:F3}, untouched {untouched[1]:F3}, rise {rise[1]:F3}) - the jobs lag is not carrying the new participants."); ok = false; }
            if (!(cut[2] > untouched[2]) || !(rise[2] < untouched[2])) { Debug.LogError($"LABOR TAX: potential four years on does not answer the rate both ways (cut {cut[2]:F1}, untouched {untouched[2]:F1}, rise {rise[2]:F1})."); ok = false; }
            if (Mathf.Abs(cut[3] - expectedCut) > 1e-3f || Mathf.Abs(rise[3] - expectedRise) > 1e-3f) { Debug.LogError($"LABOR TAX: the term reads {cut[3]:F4} / {rise[3]:F4}, expected {expectedCut:F4} / {expectedRise:F4}."); ok = false; }
            if (Mathf.Abs(untouched[3]) > 1e-6f) { Debug.LogError($"LABOR TAX: untouched Sweden carries a term after {Turns} years ({untouched[3]:R})."); ok = false; }
            float[] far = Run(20f);
            if (Mathf.Abs(far[3]) > 5f) { Debug.LogError($"LABOR TAX: the term left its ±5 guard at +20 points ({far[3]:F3})."); ok = false; }

            Debug.Log($"LABOR TAX (LANDED, §396): Sweden, income tax {seedRate:F1} % at the seed - a 5-point CUT from turn {Landing}: participation {untouched[0]:F3} → {cut[0]:F3} % two years on, unemployment {untouched[1]:F3} → {cut[1]:F3} % (the new participants before the jobs), potential {untouched[2]:F1} → {cut[2]:F1} four years on; a 5-point RISE: {rise[0]:F3} % / {rise[1]:F3} % / {rise[2]:F1}; the term {cut[3]:F3} / {rise[3]:F3} (expected {expectedCut:F3} / {expectedRise:F3}); +20 points: {far[3]:F3} (guard ±5). The elasticity 0.116 is SELMA's 1 ÷ 8.61.");
            Debug.Log(ok ? "LABOR TAX: PASS - zero at the seed, the sourced elasticity, both directions on participation, unemployment and potential behind the jobs lag, inside the guard." : "LABOR TAX: FAILED (see above).");
            CheckExit.Finish(ok ? 0 : 1);
        }

        private static float[] Run(float deltaPoints)
        {
            SimulationRandom.Seed(777);
            World world = WorldFactory.CreateDefault();
            var go = new GameObject("LABORTAX");
            try
            {
                SimulationManager sim = go.AddComponent<SimulationManager>();
                sim.SetWorld(world);
                sim.AiFinanceMinistryEnabled = false;   // §388: this check measures its own rule alone - the ministry's levers would move the lines it reads
                sim.PlayerCountryId = CountryId.Sweden;
                Country c = world.GetCountry(CountryId.Sweden);
                float seedRate = c.LaborTaxRateSeed;
                var decisions = new Dictionary<CountryId, PolicyDecision>();
                foreach (Country k in world.Countries) { decisions[k.Id] = PolicyDecision.None(); }
                var result = new float[5]; result[4] = seedRate;
                for (int year = 1; year <= Turns; year++)
                {
                    for (int day = 0; day < SimulationManager.DaysPerTurn; day++) { sim.AdvanceDay(); }
                    PolicyDecision d = PolicyDecision.None();
                    if (deltaPoints != 0f && year >= Landing) { d.TaxRateOverrides[TaxType.IncomeTax] = seedRate + deltaPoints; }
                    decisions[CountryId.Sweden] = d;
                    sim.AdvanceTurn(decisions);
                    if (year == Landing + 2) { result[0] = c.State.LaborForceParticipationRate; result[1] = c.State.Unemployment; }
                    if (year == Landing + 4) { result[2] = c.State.PotentialGDP; }
                }
                result[3] = MacroSystem.LaborTaxParticipationTerm(c);
                return result;
            }
            finally { Object.DestroyImmediate(go); }
        }
    }
}
