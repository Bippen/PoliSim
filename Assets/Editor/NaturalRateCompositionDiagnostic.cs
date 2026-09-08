using System.Collections.Generic;
using PoliSim.Data;
using PoliSim.Simulation;
using UnityEditor;
using UnityEngine;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// FT-8 (2026-09-08, §398): the natural rate reads the labour force. (1) At the seed the demographic shift is zero for six and the effective natural rate
    /// is the seeded one; the composition base is the hand computation Σ(band × participation × rate) ÷ Σ(band × participation) over the two sourced tables,
    /// to 1e-4. (2) After one turn with no player the shift equals composition-now minus the base to 1e-5 for six, and is small (a composition effect is
    /// basis points a year, not points). (3) The three readers read the shift: with a shift of 0.5 set by hand on Sweden the Taylor gap rises by exactly 0.5,
    /// the Phillips curve's print RISES by slope × 0.5 (a higher natural rate is a tighter market at the same unemployment), and Okun's reversion moves unemployment linearly in the shift (twice the move for a shift of 1.0).
    /// (4) The direction: moving a million 20–24-year-olds into 60–64 on Sweden's pyramid lowers the composition rate, by the hand computation to 1e-5 - the
    /// young run at three to four times the prime-age rate in every source (Shimer 1998, the Eurostat and BLS tables).
    /// </summary>
    public static class NaturalRateCompositionDiagnostic
    {
        public static void Run()
        {
            CheckExit.ArmLogFold();
            bool ok = true;
            SimulationRandom.Seed(777);
            World world = WorldFactory.CreateDefault();
            var report = new List<string>();
            foreach (Country c in world.Countries)
            {
                if (Mathf.Abs(c.State.NaturalRateDemographicShift) > 1e-6f) { Debug.LogError($"NATURAL RATE: {c.Id} carries a demographic shift at the seed ({c.State.NaturalRateDemographicShift:R})."); ok = false; }
                if (Mathf.Abs(c.EffectiveNaturalUnemploymentRate - c.NaturalUnemploymentRate) > 1e-6f) { Debug.LogError($"NATURAL RATE: {c.Id}'s effective natural rate is not the seeded one at the seed."); ok = false; }
                float hand = Hand(c.Id, c.Cohorts.Counts);
                if (float.IsNaN(hand) || Mathf.Abs(c.CompositionNaturalRateAtSeed - hand) > 1e-4f) { Debug.LogError($"NATURAL RATE: {c.Id}'s composition base {c.CompositionNaturalRateAtSeed:F5} is not the hand computation {hand:F5}."); ok = false; }
                report.Add($"{c.Id} NAIRU {c.NaturalUnemploymentRate:F2}, composition rate at the seed {c.CompositionNaturalRateAtSeed:F3} (the 2024 structure at the 2024 cycle; only its change is read)");
            }
            var go = new GameObject("NATURALRATE");
            try
            {
                SimulationManager sim = go.AddComponent<SimulationManager>();
                sim.SetWorld(world);
                var decisions = new Dictionary<CountryId, PolicyDecision>();
                foreach (Country k in world.Countries) { decisions[k.Id] = PolicyDecision.None(); }
                for (int day = 0; day < SimulationManager.DaysPerTurn; day++) { sim.AdvanceDay(); }
                sim.AdvanceTurn(decisions);
                foreach (Country c in world.Countries)
                {
                    float expected = UnemploymentRateByAgeTable.CompositionRate(c.Id, c.Cohorts.Counts) - c.CompositionNaturalRateAtSeed;
                    if (Mathf.Abs(c.State.NaturalRateDemographicShift - expected) > 1e-5f) { Debug.LogError($"NATURAL RATE: {c.Id}'s shift after one turn {c.State.NaturalRateDemographicShift:R} is not composition-now minus the base {expected:R}."); ok = false; }
                    if (Mathf.Abs(c.State.NaturalRateDemographicShift) > 0.1f) { Debug.LogError($"NATURAL RATE: {c.Id}'s shift after ONE year is {c.State.NaturalRateDemographicShift:F4} points - a composition effect is basis points a year."); ok = false; }
                    report.Add($"{c.Id} shift after one year {c.State.NaturalRateDemographicShift:+0.0000} points");
                }
            }
            finally { Object.DestroyImmediate(go); }

            // (3) the readers, on a fresh Sweden
            SimulationRandom.Seed(777);
            World fresh = WorldFactory.CreateDefault();
            Country s = fresh.GetCountry(CountryId.Sweden);
            float gap0 = TaylorRule.GetUnemploymentGapPercent(s);
            s.State.NaturalRateDemographicShift = 0.5f;
            float gap1 = TaylorRule.GetUnemploymentGapPercent(s);
            if (Mathf.Abs((gap1 - gap0) - 0.5f) > 1e-5f) { Debug.LogError($"NATURAL RATE: the Taylor gap moved {gap1 - gap0:F5} for a shift of 0.5 - the rule does not read the effective natural rate."); ok = false; }
            EconomyState before = s.State.Clone();
            s.State.NaturalRateDemographicShift = 0f; MacroSystem.ApplyPhillipsCurveInflation(s); float pi0 = s.State.Inflation; s.State = before.Clone();
            s.State.NaturalRateDemographicShift = 0.5f; MacroSystem.ApplyPhillipsCurveInflation(s); float pi1 = s.State.Inflation; s.State = before.Clone();
            if (Mathf.Abs((pi1 - pi0) - 0.3f * 0.5f) > 1e-4f) { Debug.LogError($"NATURAL RATE: the Phillips print moved {pi1 - pi0:F5} for a shift of 0.5 (expected +slope 0.3 × 0.5 = +0.15: a higher natural rate is a tighter market at the same unemployment) - the curve does not read the effective natural rate."); ok = false; }
            float u0 = Okun(s, before, 0f), u05 = Okun(s, before, 0.5f), u1 = Okun(s, before, 1.0f);
            if (!(u05 > u0) || Mathf.Abs((u1 - u0) - 2f * (u05 - u0)) > 1e-4f) { Debug.LogError($"NATURAL RATE: Okun's reversion reads the shift as {u05 - u0:F5} at 0.5 and {u1 - u0:F5} at 1.0 - not linear in the effective natural rate."); ok = false; }

            // (4) the direction on Sweden's pyramid
            float[] counts = (float[])s.Cohorts.Counts.Clone();
            float baseRate = UnemploymentRateByAgeTable.CompositionRate(CountryId.Sweden, counts);
            counts[4] -= 1f; counts[12] += 1f;   // a million 20–24-year-olds become 60–64-year-olds
            float aged = UnemploymentRateByAgeTable.CompositionRate(CountryId.Sweden, counts);
            float agedHand = Hand(CountryId.Sweden, counts);
            if (!(aged < baseRate)) { Debug.LogError($"NATURAL RATE: an older labour force did not lower the composition rate ({baseRate:F4} → {aged:F4})."); ok = false; }
            if (Mathf.Abs(aged - agedHand) > 1e-5f) { Debug.LogError($"NATURAL RATE: the aged composition rate {aged:F6} is not the hand computation {agedHand:F6}."); ok = false; }

            foreach (string line in report) { Debug.Log("NATURAL RATE: " + line); }
            Debug.Log($"NATURAL RATE: the readers on Sweden - Taylor gap {gap0:F3} → {gap1:F3} at a shift of 0.5; Phillips print {pi0:F4} → {pi1:F4}; Okun's reversion Δ {u05 - u0:+0.00000} at 0.5 and {u1 - u0:+0.00000} at 1.0; Sweden's composition rate {baseRate:F4} → {aged:F4} when a million 20–24-year-olds are 60–64.");
            Debug.Log(ok ? "NATURAL RATE: PASS - zero at the seed, the base is the hand computation, the shift is composition-now minus the base, the three readers read it, an older labour force lowers it." : "NATURAL RATE: FAILED (see above).");
            CheckExit.Finish(ok ? 0 : 1);
        }

        /// <summary>Σ(band × participation × rate) ÷ Σ(band × participation) over bands 15–19 and up, computed here from the two tables independently of the class under test.</summary>
        private static float Hand(CountryId id, float[] counts)
        {
            float[] p = ParticipationRateTable.For(id); float[] u = UnemploymentRateByAgeTable.For(id);
            if (p == null || u == null) { return float.NaN; }
            double w = 0, lf = 0;
            for (int k = 3; k < PopulationCohorts.CohortCount; k++) { double f = counts[k] * p[k]; w += f * u[k]; lf += f; }
            return lf > 0 ? (float)(w / lf) : float.NaN;
        }

        /// <summary>One turn of Okun's law at potential growth on a copy of the state with the shift set; returns unemployment after.</summary>
        private static float Okun(Country c, EconomyState template, float shift)
        {
            c.State = template.Clone();
            c.State.NaturalRateDemographicShift = shift;
            MacroSystem.ApplyOkunsLaw(c, c.PotentialGrowthRate);
            float u = c.State.Unemployment;
            c.State = template.Clone();
            return u;
        }
    }
}
