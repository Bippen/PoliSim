using UnityEngine;
using PoliSim.Data;
using PoliSim.Simulation;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// RECALIBRATION PASS (build-order item 1) — the Sweden PotentialGDP solve, ruled 2026-08-26:
    /// the ruled UO10/11/12 mandatory flip removed 5.6pp of GDP from the national-accounts G term,
    /// opening a persistent ~2–3% output gap at seed (measured: t1 GDP growth +0.5% against 1.5%
    /// potential growth). This is the USA's own turn-1-consistency problem (WorldFactory's
    /// PotentialGDP 33260 note) arriving on Sweden, and it takes the USA's own answer: an
    /// EMPIRICALLY SOLVED PotentialGDP seed at which GDP=620 is already at its turn-1-consistent
    /// fixed point — found by sweep, not closed form, because the turn-1 interest-rate/output-gap/
    /// reversion chain has none.
    ///
    /// Two-stage sweep: coarse over a wide bracket, then fine inside the best coarse pair. The
    /// criterion is |t1 GDP − 620| (the seed headline must hold at its own fixed point), with t2/t3
    /// printed so stability is visible rather than assumed. Each candidate runs on a FRESH world
    /// and a re-seeded RNG so candidates are comparable.
    ///
    /// Run: `Unity.exe -batchmode -nographics -projectPath &lt;path&gt; -executeMethod
    /// PoliSim.EditorTools.SwedenPotentialSolveDiagnostic.Run -logFile &lt;path&gt;`
    /// </summary>
    public static class SwedenPotentialSolveDiagnostic
    {
        private static readonly System.Globalization.CultureInfo Inv = System.Globalization.CultureInfo.InvariantCulture;
        private const float SeedGdp = 620f;

        public static void Run()
        {
            CheckExit.ArmLogFold();

            // Coarse: bracket wide on both sides (the USA's solution sat ABOVE its GDP; Sweden's
            // identity now sums BELOW potential, so above is expected — but the sweep, not the
            // expectation, decides).
            float bestCoarse = Sweep(600f, 700f, 10f);
            float best = Sweep(bestCoarse - 10f, bestCoarse + 10f, 1f);
            float bestFine = Sweep(best - 1f, best + 1f, 0.25f);

            Debug.Log($"SWEPOT: SOLVED PotentialGDP seed = {bestFine.ToString("F2", Inv)} (coarse {bestCoarse.ToString("F0", Inv)}, mid {best.ToString("F0", Inv)}).");
            CheckExit.Finish(0);
        }

        private static float Sweep(float from, float to, float step)
        {
            float bestCandidate = from;
            float bestError = float.MaxValue;
            for (float candidate = from; candidate <= to + 0.001f; candidate += step)
            {
                float err = Probe(candidate, out float t1, out float t2, out float t3);
                Debug.Log($"SWEPOT cand={candidate.ToString("F2", Inv)} t1={t1.ToString("F2", Inv)} t2={t2.ToString("F2", Inv)} t3={t3.ToString("F2", Inv)} |t1-620|={err.ToString("F3", Inv)}");
                if (err < bestError)
                {
                    bestError = err;
                    bestCandidate = candidate;
                }
            }

            Debug.Log($"SWEPOT: best in [{from.ToString("F2", Inv)}, {to.ToString("F2", Inv)}] step {step.ToString("F2", Inv)} -> {bestCandidate.ToString("F2", Inv)} (err {bestError.ToString("F3", Inv)})");
            return bestCandidate;
        }

        private static float Probe(float potentialCandidate, out float t1, out float t2, out float t3)
        {
            SimulationRandom.Seed(777);
            World world = WorldFactory.CreateDefault();
            Country sweden = world.Countries.Find(c => c.Id == CountryId.Sweden);
            sweden.State.PotentialGDP = potentialCandidate;

            var go = new GameObject($"SWEPOT_{potentialCandidate:F0}");
            try
            {
                SimulationManager sim = go.AddComponent<SimulationManager>();
                sim.SetWorld(world);
                var decisions = new System.Collections.Generic.Dictionary<CountryId, PolicyDecision>();
                foreach (Country c in world.Countries) { decisions[c.Id] = PolicyDecision.None(); }

                t1 = AdvanceOneTurn(sim, sweden, decisions);
                t2 = AdvanceOneTurn(sim, sweden, decisions);
                t3 = AdvanceOneTurn(sim, sweden, decisions);
                return Mathf.Abs(t1 - SeedGdp);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        private static float AdvanceOneTurn(SimulationManager sim, Country sweden, System.Collections.Generic.Dictionary<CountryId, PolicyDecision> decisions)
        {
            for (int d = 0; d < SimulationManager.DaysPerTurn; d++) { sim.AdvanceDay(); }
            sim.AdvanceTurn(decisions);
            return sweden.State.GDP;
        }
    }
}
