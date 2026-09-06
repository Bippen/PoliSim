using System.Collections.Generic;
using PoliSim.Data;
using PoliSim.Simulation;
using UnityEditor;
using UnityEngine;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// P5-C4 (2026-09-06): the infrastructure family's proof. (1) The seeds are the spine's WEF 2019 figures for six (road quality and connectivity),
    /// every country carrying a spending base; no figure exists for congestion or road length anywhere in the model. (2) Sweden at no policy for
    /// twenty years: quality and connectivity inside their guards, quality holding within a few points of its seed (the seed's spending rebuilds the
    /// decay - the spine's claim to check, measured here). (3) Sweden with its infrastructure lines cut through the decision each year: road quality
    /// LOWER and connectivity LOWER than untouched - the couplings move the stated way.
    /// </summary>
    public static class InfrastructureFamilyDiagnostic
    {
        private const int Years = 20;

        public static void Run()
        {
            CheckExit.ArmLogFold();
            bool ok = true;
            SimulationRandom.Seed(777);
            World seedWorld = WorldFactory.CreateDefault();
            var expectedQuality = new Dictionary<CountryId, float> { { CountryId.Sweden, 83.9f }, { CountryId.Germany, 83.4f }, { CountryId.France, 85.3f }, { CountryId.Italy, 71.3f }, { CountryId.Poland, 71.6f }, { CountryId.USA, 87.2f } };
            var expectedConnectivity = new Dictionary<CountryId, float> { { CountryId.Sweden, 95.9f }, { CountryId.Germany, 95.1f }, { CountryId.France, 96.6f }, { CountryId.Italy, 85.9f }, { CountryId.Poland, 88.0f }, { CountryId.USA, 100f } };
            foreach (Country c in seedWorld.Countries)
            {
                if (!expectedQuality.ContainsKey(c.Id)) { continue; }
                if (!c.Infrastructure.Seeded) { Debug.LogError($"INFRASTRUCTURE: {c.Id} carries no seeded family."); ok = false; continue; }
                if (Mathf.Abs(c.State.RoadQuality - expectedQuality[c.Id]) > 1e-4f) { Debug.LogError($"INFRASTRUCTURE: {c.Id} road quality seeds {c.State.RoadQuality}, the spine says {expectedQuality[c.Id]}."); ok = false; }
                if (Mathf.Abs(c.State.RoadConnectivity - expectedConnectivity[c.Id]) > 1e-4f) { Debug.LogError($"INFRASTRUCTURE: {c.Id} connectivity seeds {c.State.RoadConnectivity}, the spine says {expectedConnectivity[c.Id]}."); ok = false; }
                if (c.Infrastructure.SpendPerHeadSeed <= 0f) { Debug.LogError($"INFRASTRUCTURE: {c.Id} has no spending base - the infrastructure lines were not found."); ok = false; }
            }

            float[] untouched = RunSweden(Years, cutShare: 0f, out bool guardsHeld);
            if (!guardsHeld) { Debug.LogError("INFRASTRUCTURE: a figure left its runaway guard in twenty years at no policy."); ok = false; }
            if (Mathf.Abs(untouched[0] - 83.9f) > 6f) { Debug.LogError($"INFRASTRUCTURE: Sweden's road quality drifted to {untouched[0]:F1} from 83.9 at no policy - the seed's spending should hold the seed's score within a few points."); ok = false; }
            float[] cut = RunSweden(Years, cutShare: -0.2f, out _);
            if (!(cut[0] < untouched[0])) { Debug.LogError($"INFRASTRUCTURE: with the infrastructure lines cut, road quality {cut[0]:F2} is not below the untouched run's {untouched[0]:F2}."); ok = false; }
            if (!(cut[1] < untouched[1])) { Debug.LogError($"INFRASTRUCTURE: with the infrastructure lines cut, connectivity {cut[1]:F2} is not below the untouched run's {untouched[1]:F2}."); ok = false; }

            Debug.Log($"INFRASTRUCTURE: seeds - road quality and connectivity for six (WEF GCR 2019, dated), congestion a fetch, road length absent. Sweden after {Years} years - untouched: quality {untouched[0]:F1} (seed 83.9), connectivity {untouched[1]:F1} (seed 95.9); "
                + $"infrastructure lines cut through the decision each year (-20 % asked, the seed range deciding): quality {cut[0]:F1}, connectivity {cut[1]:F1} - both lower: the couplings move the stated way.");
            Debug.Log(ok ? "INFRASTRUCTURE: PASS - the seeds are the spine's, the seed's spending holds the seed's score, the couplings move the stated way." : "INFRASTRUCTURE: FAILED (see above).");
            CheckExit.Finish(ok ? 0 : 1);
        }

        private static float[] RunSweden(int years, float cutShare, out bool guardsHeld)
        {
            SimulationRandom.Seed(777);
            World world = WorldFactory.CreateDefault();
            var go = new GameObject("INFRASTRUCTURE");
            guardsHeld = true;
            try
            {
                SimulationManager sim = go.AddComponent<SimulationManager>();
                sim.SetWorld(world);
                sim.PlayerCountryId = CountryId.Sweden;
                Country se = world.GetCountry(CountryId.Sweden);
                var decisions = new Dictionary<CountryId, PolicyDecision>();
                foreach (Country k in world.Countries) { decisions[k.Id] = PolicyDecision.None(); }
                for (int year = 1; year <= years; year++)
                {
                    for (int day = 0; day < SimulationManager.DaysPerTurn; day++) { sim.AdvanceDay(); }
                    PolicyDecision d = PolicyDecision.None();
                    if (cutShare != 0f) { foreach (SpendingLine line in se.SpendingLines) { if (InfrastructureFamily.IsInfrastructureLine(line.Category)) { d.SpendingLineChanges[line.Category] = cutShare * 100f; } } }
                    decisions[CountryId.Sweden] = d;
                    sim.AdvanceTurn(decisions);
                    if (se.State.RoadQuality < InfrastructureFamily.MinScore || se.State.RoadQuality > InfrastructureFamily.MaxScore || se.State.RoadConnectivity > InfrastructureFamily.MaxScore) { guardsHeld = false; }
                }
                return new[] { se.State.RoadQuality, se.State.RoadConnectivity };
            }
            finally { Object.DestroyImmediate(go); }
        }
    }
}
