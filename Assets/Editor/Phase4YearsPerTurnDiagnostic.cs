using System.Reflection;
using PoliSim.Data;
using PoliSim.Simulation;
using UnityEngine;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// Continuous Time Phase 4, step 1: THE THROWAWAY DIAGNOSTIC BEFORE ANYTHING - the handoff
    /// notes' own gate, named there because this exact discipline caught demographics' two prior
    /// structural bugs (the 3x over-compounding, twice). Run BEFORE any conversion exists; its job
    /// is to prove `YearsPerTurn` threads through the 365-day turn cleanly, or stop the pass.
    ///
    /// Run: `Unity.exe -batchmode -nographics -projectPath &lt;path&gt; -executeMethod
    /// PoliSim.EditorTools.Phase4YearsPerTurnDiagnostic.Run -logFile &lt;path&gt;`
    ///
    /// Four checks: (1) the project's only two statements of turn length agree exactly
    /// (`DaysPerTurn`/365 == `YearsPerTurn` == 4/`ElectionCycle`); (2) the MEASURED population step
    /// applies exactly one year of an annual-per-1000 rate per turn, at positive and negative
    /// growth, with the reversion pinned to a no-op so the measurement isolates the YearsPerTurn
    /// factor; (3) the measured secular BirthRate decline is exactly its per-turn constant; (4) a
    /// dependency-gap drift step matches its constants to float precision. `YearsPerTurn` is
    /// private by design - read here by reflection rather than widened for a diagnostic.
    /// </summary>
    public static class Phase4YearsPerTurnDiagnostic
    {
        public static void Run()
        {
            int passed = 0, total = 0;

            // (1) The two turn-length statements.
            FieldInfo yearsField = typeof(MacroSystem).GetField("YearsPerTurn", BindingFlags.Static | BindingFlags.NonPublic);
            if (yearsField == null)
            {
                Debug.LogError("P4DIAG: MacroSystem.YearsPerTurn not found by reflection - VERIFIED NOTHING.");
                CheckExit.Finish(2);
                return;
            }

            float yearsPerTurn = (float)yearsField.GetRawConstantValue();
            total += 3;
            passed += Check("ElectionCycle == 4 (a presidential term of years)", ElectionSystem.ElectionCycle == 4,
                $"ElectionCycle={ElectionSystem.ElectionCycle}") ? 1 : 0;
            passed += Check("YearsPerTurn == 1.0 exactly", yearsPerTurn == 1f, $"YearsPerTurn={yearsPerTurn:R}") ? 1 : 0;
            passed += Check("DaysPerTurn/365 == YearsPerTurn (the two statements agree)",
                SimulationManager.DaysPerTurn / 365f == yearsPerTurn,
                $"DaysPerTurn={SimulationManager.DaysPerTurn}") ? 1 : 0;

            // (2) The measured population application, reversion pinned to a no-op.
            foreach (float g in new[] { 5f, -3f })
            {
                World world = WorldFactory.CreateDefault();
                Country sweden = world.GetCountry(CountryId.Sweden);
                EconomyState s = sweden.State;
                s.BirthRate = 10f;
                s.DeathRate = 9f;
                s.NetMigrationRate = g - 1f; // implied = 10 - 9 + (g-1) = g
                sweden.SteadyStateGrowthRate = g;
                s.PopulationGrowthRate = g;  // target == g == current -> reversion no-op
                float populationBefore = s.Population;

                MacroSystem.ApplyPopulationGrowth(sweden);

                float expected = populationBefore * (1f + g / 1000f * yearsPerTurn);
                total += 2;
                passed += Check($"g={g:R}: PopulationGrowthRate unmoved by pinned reversion",
                    Mathf.Approximately(s.PopulationGrowthRate, g), $"rate={s.PopulationGrowthRate:R}") ? 1 : 0;
                passed += Check($"g={g:R}: one turn applies exactly one year of the annual rate",
                    Mathf.Approximately(s.Population, expected),
                    $"pop {populationBefore:R} -> {s.Population:R}, expected {expected:R}") ? 1 : 0;
            }

            // (3) The secular decline's measured per-turn magnitude.
            {
                World world = WorldFactory.CreateDefault();
                Country sweden = world.GetCountry(CountryId.Sweden);
                float naturalBefore = sweden.State.NaturalBirthRate;
                MacroSystem.ApplyDemographicRates(sweden);
                float decline = naturalBefore - sweden.State.NaturalBirthRate;
                total++;
                // 1e-5 absolute, not Mathf.Approximately: the measurement subtracts two ~10.6
                // floats, whose representation granularity (~1e-6) survives the subtraction - the
                // first run FAILED at 0.0100002289 measured, which is the float-honest value of
                // exactly 0.01 applied at this magnitude, not a defect.
                passed += Check("secular BirthRate decline == 0.01/turn as documented",
                    Mathf.Abs(decline - 0.01f) < 1e-5f, $"measured {decline:R}") ? 1 : 0;
            }

            // (4) One dependency-drift step at a controlled gap.
            {
                World world = WorldFactory.CreateDefault();
                Country sweden = world.GetCountry(CountryId.Sweden);
                EconomyState s = sweden.State;
                s.DeathRate = s.BirthRate + 2f;      // natural decrease of exactly 2
                float dependencyBefore = s.DependencyRatio;
                MacroSystem.ApplyDemographicRates(sweden);
                // BirthRate moves first (secular decline widens the gap by that step's own 0.01
                // when NaturalBirthRate is above its floor and family policy is neutral).
                float expectedGap = 2f + (dependencyBefore >= 15f ? 0.01f : 0f);
                float drift = s.DependencyRatio - dependencyBefore;
                total++;
                passed += Check("DependencyRatio drift == 0.0015 x natural-decrease gap",
                    Mathf.Abs(drift - 0.0015f * expectedGap) < 1e-5f,
                    $"measured {drift:R}, expected {0.0015f * expectedGap:R}") ? 1 : 0;
            }

            Debug.Log($"P4DIAG: {passed} of {total} checks passed.");
            CheckExit.Finish(passed == total ? 0 : 1);
        }

        private static bool Check(string name, bool ok, string detail)
        {
            Debug.Log($"  {(ok ? "ok  " : "FAIL")} {name}  ({detail})");
            return ok;
        }
    }
}
