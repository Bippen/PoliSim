using PoliSim.Data;
using PoliSim.Simulation;
using UnityEditor;
using UnityEngine;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// THE VALIDATION BAR for every Continuous Time phase, run BEFORE the scenario matrix:
    /// *"simulate 121 consecutive days and confirm the result is within ±3-5% of what the existing,
    /// already-validated single turn-level step produces for the same inputs."*
    ///
    /// Run: `Unity.exe -batchmode -nographics -projectPath &lt;path&gt; -executeMethod
    /// PoliSim.EditorTools.AggregationEquivalenceCheck.Run -logFile &lt;path&gt;`
    ///
    /// Phase 1 covers Sectors and Infrastructure. Both should come out EXACT rather than merely inside
    /// tolerance, because both translations were chosen to be algebraically equivalent rather than
    /// approximately so — a residual above float noise means a constant took the wrong shape.
    /// </summary>
    public static class AggregationEquivalenceCheck
    {
        private const float TolerancePercent = 3f;

        public static void Run()
        {
            int passed = 0, total = 0;

            // SELF-TEST FIRST: two independently-built worlds must start identical, or every comparison
            // below is measuring world-construction noise rather than the translation.
            World a = WorldFactory.CreateDefault();
            World b = WorldFactory.CreateDefault();
            Country ca = a.GetCountry(CountryId.Sweden);
            Country cb = b.GetCountry(CountryId.Sweden);
            bool sameStart = Mathf.Approximately(ca.Sectors[0].OutputShareOfGdp, cb.Sectors[0].OutputShareOfGdp)
                && Mathf.Approximately(ca.InfrastructureAssets[0].ConditionIndex, cb.InfrastructureAssets[0].ConditionIndex);
            Debug.Log($"SELFTEST two fresh worlds identical at start -> {(sameStart ? "OK" : "BROKEN - results below are void")}");

            // --- SECTORS -----------------------------------------------------------------------------
            // Driven OFF baseline so there is a real gap to close: without a policy offset every stat
            // already sits at its target and both paths trivially agree, which would prove nothing.
            foreach (Sector s in ca.Sectors) { s.SubsidyLevel = 90f; }
            foreach (Sector s in cb.Sectors) { s.SubsidyLevel = 90f; }

            MacroSystem.ApplySectorEffects(ca);                                        // one turn step
            for (int d = 0; d < SimulationManager.DaysPerTurn; d++)                     // 121 daily steps
            {
                MacroSystem.ApplySectorEffectsDaily(cb);
            }

            for (int i = 0; i < ca.Sectors.Count; i++)
            {
                total += 3;
                passed += Compare($"Sector[{ca.Sectors[i].Type}].Output", ca.Sectors[i].OutputShareOfGdp, cb.Sectors[i].OutputShareOfGdp) ? 1 : 0;
                passed += Compare($"Sector[{ca.Sectors[i].Type}].Employment", ca.Sectors[i].EmploymentShare, cb.Sectors[i].EmploymentShare) ? 1 : 0;
                passed += Compare($"Sector[{ca.Sectors[i].Type}].Metric", ca.Sectors[i].SectorMetric, cb.Sectors[i].SectorMetric) ? 1 : 0;
            }

            // --- INFRASTRUCTURE ----------------------------------------------------------------------
            // Decay only, with no investment: that is the part that moved to daily granularity, and
            // isolating it is what makes a failure attributable to the decay translation specifically.
            World c = WorldFactory.CreateDefault();
            World d2 = WorldFactory.CreateDefault();
            Country cc = c.GetCountry(CountryId.Sweden);
            Country cd = d2.GetCountry(CountryId.Sweden);
            var noSpending = PolicyDecision.None();

            MacroSystem.ApplyInfrastructureCondition(cc, noSpending);                   // one turn step
            for (int i = 0; i < SimulationManager.DaysPerTurn; i++)                     // 121 daily steps
            {
                MacroSystem.ApplyInfrastructureConditionDaily(cd);
            }

            for (int i = 0; i < cc.InfrastructureAssets.Count; i++)
            {
                total++;
                passed += Compare($"Infrastructure[{cc.InfrastructureAssets[i].Type}].Condition",
                    cc.InfrastructureAssets[i].ConditionIndex, cd.InfrastructureAssets[i].ConditionIndex) ? 1 : 0;
            }

            // --- PHASE 2: Labor Market and Crime & Justice -------------------------------------------
            // Driven off baseline the same way, and via the same policy dials a player would move, so the
            // targets differ from current state and there is a real gap for both paths to close.
            World e = WorldFactory.CreateDefault();
            World f = WorldFactory.CreateDefault();
            Country ce = e.GetCountry(CountryId.Sweden);
            Country cf = f.GetCountry(CountryId.Sweden);
            foreach (Country x in new[] { ce, cf })
            {
                x.PoliceFundingLevel = 80f;
                x.SentencingSeverity = 20f;
                x.BailReformLevel = 75f;
                x.DrugPolicyLevel = 25f;
                x.RetrainingProgramLevel = 85f;
            }

            // Turn path: one step each, in AdvanceTurn's documented order.
            MacroSystem.ApplyLaborForceParticipationRate(ce);
            MacroSystem.ApplyOrganizedCrimeIndex(ce);
            MacroSystem.ApplyCorruptionIndex(ce);
            MacroSystem.ApplyCrimeIndex(ce);
            MacroSystem.ApplyCrimeEffects(ce);
            MacroSystem.ApplyPrisonPopulationRate(ce);

            // Daily path: 121 steps, same order preserved.
            for (int i = 0; i < SimulationManager.DaysPerTurn; i++)
            {
                MacroSystem.ApplyLaborForceParticipationRateDaily(cf);
                MacroSystem.ApplyOrganizedCrimeIndexDaily(cf);
                MacroSystem.ApplyCorruptionIndexDaily(cf);
                MacroSystem.ApplyCrimeIndexDaily(cf);
                MacroSystem.ApplyCrimeEffectsDaily(cf);
                MacroSystem.ApplyPrisonPopulationRateDaily(cf);
            }

            total += 6;
            passed += Compare("LaborForceParticipationRate", ce.State.LaborForceParticipationRate, cf.State.LaborForceParticipationRate) ? 1 : 0;
            passed += Compare("OrganizedCrimeIndex", ce.State.OrganizedCrimeIndex, cf.State.OrganizedCrimeIndex) ? 1 : 0;
            passed += Compare("CorruptionIndex", ce.State.CorruptionIndex, cf.State.CorruptionIndex) ? 1 : 0;
            passed += Compare("CrimeIndex", ce.State.CrimeIndex, cf.State.CrimeIndex) ? 1 : 0;
            passed += Compare("PrisonPopulationRate", ce.State.PrisonPopulationRate, cf.State.PrisonPopulationRate) ? 1 : 0;
            passed += Compare("BusinessConfidence (crime drift)", ce.State.BusinessConfidence, cf.State.BusinessConfidence) ? 1 : 0;

            Debug.Log($"=== Phases 1-2 aggregation-equivalence: {passed} of {total} within {TolerancePercent}% ===");
            EditorApplication.Exit(passed == total ? 0 : 1);
        }

        private static bool Compare(string label, float turnValue, float dailyValue)
        {
            float denominator = Mathf.Max(Mathf.Abs(turnValue), 0.0001f);
            float driftPercent = Mathf.Abs(dailyValue - turnValue) / denominator * 100f;
            bool ok = driftPercent <= TolerancePercent;
            Debug.Log($"  {(ok ? "ok  " : "FAIL")} {label,-42} turn={turnValue,9:F5}  daily={dailyValue,9:F5}  drift={driftPercent,7:F4}%");
            return ok;
        }
    }
}
