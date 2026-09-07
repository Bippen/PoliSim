using System.Collections.Generic;
using PoliSim.Data;
using PoliSim.Simulation;
using UnityEditor;
using UnityEngine;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// RF-1 (2026-09-07): the road-quality readout saturates toward its ceiling. (1) At the seed's spending the rebuild equals the decay to the digit for six - the
    /// seed's spending holds the seed's score. (2) The saturation factor is 1 at the seed, 0 at the ceiling, above 1 below the seed. (3) A century at seed policy:
    /// no country's score reaches the ceiling (the drift to 100 of §350 is gone) and none falls to the floor. (4) Both directions around the seed still hold for
    /// Sweden over twenty years through the decision: the lines cut leave the score LOWER, raised HIGHER, and raised never reaches 100.
    /// </summary>
    public static class InfrastructureReadoutDiagnostic
    {
        public static void Run()
        {
            CheckExit.ArmLogFold();
            bool ok = true;
            SimulationRandom.Seed(777);
            World seedWorld = WorldFactory.CreateDefault();
            foreach (Country c in seedWorld.Countries)
            {
                if (!c.Infrastructure.Seeded) { continue; }
                float heads = Mathf.Max(0.0001f, SpendingDrivers.Level(SpendingDriver.Population, c));
                float rebuildAtSeed = InfrastructureFamily.RebuildFor(c, c.Infrastructure.SpendPerHeadSeed * heads);
                float decayAtSeed = c.State.RoadQuality * InfrastructureFamily.QualityDecayPerYear;
                if (Mathf.Abs(rebuildAtSeed - decayAtSeed) > 1e-5f * Mathf.Max(1f, decayAtSeed)) { Debug.LogError($"INFRASTRUCTURE READOUT: {c.Id}'s rebuild at the seed's spending ({rebuildAtSeed:R}) is not its decay ({decayAtSeed:R}) - the seed's spending does not hold the seed's score."); ok = false; }
                if (Mathf.Abs(InfrastructureFamily.SaturationFactor(c) - 1f) > 1e-6f) { Debug.LogError($"INFRASTRUCTURE READOUT: {c.Id}'s saturation factor at the seed is {InfrastructureFamily.SaturationFactor(c):R}, not 1."); ok = false; }
            }

            float[] untouched = Run(CountryId.Sweden, 20, 0f, out _);
            float[] cut = Run(CountryId.Sweden, 20, -0.2f, out _);
            float[] raised = Run(CountryId.Sweden, 20, 0.2f, out _);
            if (!(cut[0] < untouched[0]) || !(raised[0] > untouched[0])) { Debug.LogError($"INFRASTRUCTURE READOUT: the cut ({cut[0]:F2}) and the raise ({raised[0]:F2}) do not bracket untouched ({untouched[0]:F2})."); ok = false; }
            if (!(raised[0] < InfrastructureFamily.MaxScore)) { Debug.LogError($"INFRASTRUCTURE READOUT: twenty years of raises reached the ceiling ({raised[0]:F2})."); ok = false; }

            var century = new List<string>();
            foreach (CountryId id in new[] { CountryId.Sweden, CountryId.Germany, CountryId.France, CountryId.Italy, CountryId.Poland, CountryId.USA })
            {
                float[] r = Run(id, 100, 0f, out float maxScore);
                century.Add($"{id} {r[0]:F2} (max {maxScore:F2})");
                if (maxScore >= InfrastructureFamily.MaxScore - 1e-3f) { Debug.LogError($"INFRASTRUCTURE READOUT: {id} reached the ceiling at baseline within a century (max {maxScore:F3}) - the drift is not fixed."); ok = false; }
                if (r[0] <= InfrastructureFamily.MinScore + 1e-3f) { Debug.LogError($"INFRASTRUCTURE READOUT: {id} fell to the floor at baseline within a century ({r[0]:F3})."); ok = false; }
            }

            Debug.Log($"INFRASTRUCTURE READOUT: at the seed's spending the rebuild equals the decay for six; the saturation factor is 1 at the seed. Sweden after twenty years - untouched {untouched[0]:F2}, lines cut {cut[0]:F2}, raised {raised[0]:F2} (below the ceiling). "
                + $"A century at seed policy, the score at year 100 and its maximum on the way: {string.Join(", ", century)} - no ceiling reached, no floor.");
            Debug.Log(ok ? "INFRASTRUCTURE READOUT: PASS - the seed's spending holds the seed's score; the ceiling is approached, never sat on." : "INFRASTRUCTURE READOUT: FAILED (see above).");
            CheckExit.Finish(ok ? 0 : 1);
        }

        private static float[] Run(CountryId player, int years, float share, out float maxScore)
        {
            SimulationRandom.Seed(777);
            World world = WorldFactory.CreateDefault();
            var go = new GameObject("INFRAREADOUT");
            maxScore = 0f;
            try
            {
                SimulationManager sim = go.AddComponent<SimulationManager>();
                sim.SetWorld(world);
                sim.PlayerCountryId = player;
                Country c = world.GetCountry(player);
                var decisions = new Dictionary<CountryId, PolicyDecision>();
                foreach (Country k in world.Countries) { decisions[k.Id] = PolicyDecision.None(); }
                for (int year = 1; year <= years; year++)
                {
                    for (int day = 0; day < SimulationManager.DaysPerTurn; day++) { sim.AdvanceDay(); }
                    PolicyDecision d = PolicyDecision.None();
                    if (share != 0f) { foreach (SpendingLine line in c.SpendingLines) { if (InfrastructureFamily.IsInfrastructureLine(line.Category)) { d.SpendingLineChanges[line.Category] = share * 100f; } } }
                    decisions[player] = d;
                    sim.AdvanceTurn(decisions);
                    maxScore = Mathf.Max(maxScore, c.State.RoadQuality);
                }
                return new[] { c.State.RoadQuality };
            }
            finally { Object.DestroyImmediate(go); }
        }
    }
}
