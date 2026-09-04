using System;
using System.Collections.Generic;
using PoliSim.Data;
using PoliSim.Simulation;
using UnityEngine;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// P5-B7 (2026-09-05): potential output IS its factors, asserted on the real turn path for all six. Five untouched
    /// years, no player: EconomyState.PotentialGDP must equal the seed's potential × (the labour input now ÷ the seed's,
    /// computed HERE from the cohorts, participation and the natural rate) × the compound of the trend the ledger held
    /// each turn (read after each turn, compounded by the diagnostic) within 1e-3 - the daily slices of one turn sum to
    /// one; Country.PotentialGrowthRate must equal the derived rate ((1 + trend/100) × labour growth − 1) within 1e-3
    /// pp. Then the workforce probe: a country whose 20–64 cohort shrinks over the five years (the substrate says
    /// which) must show potential BELOW the productivity index alone - the labour input speaking - and one whose cohort
    /// grows, above. Sits on the simulation bar.
    /// </summary>
    public static class PotentialOutputDiagnostic
    {
        private const int Years = 5;
        private const float Tolerance = 1e-3f;

        public static void Run()
        {
            CheckExit.ArmLogFold();
            bool ok = true;
            int shrinking = 0, growing = 0;
            SimulationRandom.Seed(777);
            World world = WorldFactory.CreateDefault();
            var seedPotential = new Dictionary<CountryId, float>();
            var seedLabour = new Dictionary<CountryId, float>();
            var trendCompound = new Dictionary<CountryId, float>();
            foreach (Country c in world.Countries)
            {
                seedPotential[c.Id] = c.State.PotentialGDP;
                seedLabour[c.Id] = PotentialOutput.LabourInput(c);
                trendCompound[c.Id] = 1f;
                if (!PotentialOutput.HasSeeds(c)) { Debug.LogError($"POTENTIAL: {c.Id} carries no seeds after WorldFactory.CreateDefault - CaptureStructuralBases did not run."); ok = false; }
            }
            var go = new GameObject("POTENTIAL");
            try
            {
                SimulationManager sim = go.AddComponent<SimulationManager>();
                sim.SetWorld(world);
                var decisions = new Dictionary<CountryId, PolicyDecision>();
                foreach (Country c in world.Countries) { decisions[c.Id] = PolicyDecision.None(); }
                var labourBefore = new Dictionary<CountryId, float>();
                for (int year = 1; year <= Years; year++)
                {
                    foreach (Country c in world.Countries) { labourBefore[c.Id] = PotentialOutput.LabourInput(c); }
                    for (int day = 0; day < SimulationManager.DaysPerTurn; day++) { sim.AdvanceDay(); }
                    // The trend the days compounded at is the one the ledger held through them - the previous turn's finalizer
                    // wrote it, and this turn's finalizer rewrites it after the days. Read it before AdvanceTurn.
                    foreach (Country c in world.Countries) { trendCompound[c.Id] *= 1f + c.ProductivityTrendGrowth / 100f; }
                    sim.AdvanceTurn(decisions);
                }
                foreach (Country c in world.Countries)
                {
                    float labour = PotentialOutput.LabourInput(c);
                    float labourRatio = labour / seedLabour[c.Id];
                    float expected = seedPotential[c.Id] * labourRatio * trendCompound[c.Id];
                    float actual = c.State.PotentialGDP;
                    if (Mathf.Abs(actual / expected - 1f) > Tolerance)
                    {
                        Debug.LogError($"POTENTIAL: {c.Id} potential {actual:F2} after {Years} years; seed {seedPotential[c.Id]:F2} x labour {labourRatio:F5} x trend compound {trendCompound[c.Id]:F5} = {expected:F2}.");
                        ok = false;
                    }
                    float derived = ((1f + c.ProductivityTrendGrowth / 100f) * (labour / labourBefore[c.Id]) - 1f) * 100f;
                    if (Mathf.Abs(c.PotentialGrowthRate - derived) > Tolerance)
                    {
                        Debug.LogError($"POTENTIAL: {c.Id} PotentialGrowthRate {c.PotentialGrowthRate:F4} against the derived {derived:F4} (trend {c.ProductivityTrendGrowth:F3}, labour growth x{labour / labourBefore[c.Id]:F5}).");
                        ok = false;
                    }
                    if (labourRatio < 1f - 1e-4f) { shrinking++; if (actual >= seedPotential[c.Id] * trendCompound[c.Id]) { Debug.LogError($"POTENTIAL: {c.Id}'s labour input shrank x{labourRatio:F4} yet potential is not below the productivity path."); ok = false; } }
                    else if (labourRatio > 1f + 1e-4f) { growing++; if (actual <= seedPotential[c.Id] * trendCompound[c.Id]) { Debug.LogError($"POTENTIAL: {c.Id}'s labour input grew x{labourRatio:F4} yet potential is not above the productivity path."); ok = false; } }
                    Debug.Log($"POTENTIAL: {c.Id} - potential x{actual / seedPotential[c.Id]:F4} = labour x{labourRatio:F4} (participation {c.State.LaborForceParticipationRate:F2} %) x productivity x{trendCompound[c.Id]:F4} (trend {c.ProductivityTrendGrowth:F2} % a year); derived potential growth {c.PotentialGrowthRate:F3} % this year.");
                }
            }
            finally { UnityEngine.Object.DestroyImmediate(go); }
            if (shrinking == 0 && growing == 0) { Debug.LogError("POTENTIAL: no country's labour input moved in five years - the probe proves nothing."); ok = false; }
            Debug.Log(ok ? $"=== PotentialOutputDiagnostic: ALL ASSERTIONS PASS (six potentials at seed x labour x productivity; {shrinking} shrinking and {growing} growing labour inputs read in the sign of the potential) ===" : "=== PotentialOutputDiagnostic: FAILED ===");
            CheckExit.Finish(ok ? 0 : 1);
        }
    }
}
