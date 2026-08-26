using System.Collections.Generic;
using PoliSim.Data;
using PoliSim.Persistence;
using PoliSim.Simulation;
using UnityEditor;
using UnityEngine;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// "Italy Debt Crisis"'s own slice diagnostic - the `ScenarioSliceDiagnostic` pattern
    /// (Inherit the Fund) applied to the second real scenario, AND the `Sustained` objective
    /// form's first exercise on REAL content rather than a synthetic throwaway.
    ///
    /// Three claims, each asserted rather than narrated: (1) a no-policy line FAILS the debt
    /// objective, so the scenario has real stakes; (2) a real, measured consolidation line WINS -
    /// a winnable line exists, this is not a second unwinnable premise; (3) the Sustained streak
    /// survives a save crossing mid-run, exercising the persistence shape this scenario actually
    /// introduces to content (Inherit the Fund never used Sustained).
    ///
    /// Run: `Unity.exe -batchmode -nographics -projectPath &lt;path&gt; -executeMethod
    /// PoliSim.EditorTools.ItalyDebtCrisisSliceDiagnostic.Run -logFile &lt;path&gt;`, or from the menu.
    /// </summary>
    public static class ItalyDebtCrisisSliceDiagnostic
    {
        [MenuItem("PoliSim/Run Italy Debt Crisis Slice Diagnostic")]
        private static void RunFromMenu()
        {
            int code = CheckExit.Collect(Run);
            Debug.Log(code == 0 ? "ITALYSLICE: clean." : $"ITALYSLICE: FAILED ({code}).");
        }

        public static void Run()
        {
            CheckExit.ArmLogFold(); // ruling 1: this advances turns; an ATTRIB during it now fails the run.
            int failures = 0;
            failures += RunNoPolicyMustFail();
            failures += RunConsolidationMustWin();
            failures += RunSaveCrossingMidSustain();

            Debug.Log(failures == 0
                ? "ITALYSLICE: all three claims hold - real stakes, a winnable line exists, the Sustained streak survives a save."
                : $"ITALYSLICE: {failures} failure(s) - a divergence is a finding, report it.");
            CheckExit.Finish(failures == 0 ? 0 : 1);
        }

        private static int RunNoPolicyMustFail()
        {
            ScenarioDefinition def = ScenarioLibrary.ById("italy_debt_crisis");
            if (def == null)
            {
                Debug.LogError("ITALYSLICE: 'italy_debt_crisis' is not in the library.");
                return 1;
            }

            (ScenarioProgress progress, Country c) = RunLine(def, seed: 777, spendingCutPercent: 0f, label: "no-policy");
            LogVerdict("no-policy", def, progress, c);

            if (progress.Verdict != ScenarioVerdict.Lost)
            {
                Debug.LogError($"ITALYSLICE: no-policy should LOSE (real stakes) but resolved {progress.Verdict}.");
                return 1;
            }

            ObjectiveProgress debtState = ScenarioEvaluator.FindProgress(progress, "debt_down");
            if (debtState.Met)
            {
                Debug.LogError("ITALYSLICE: no-policy met the debt objective - the threshold is not calibrated hard enough.");
                return 1;
            }

            Debug.Log("ITALYSLICE: CONFIRMED - no-policy fails the debt objective, matching the pre-authoring measurement.");
            return 0;
        }

        private static int RunConsolidationMustWin()
        {
            ScenarioDefinition def = ScenarioLibrary.ById("italy_debt_crisis");
            // RE-PREMISED 2026-08-26 (the seed recalibration): the winning line is the measured
            // MIXED package - a -20% discretionary cut plus VAT to 25% - which lands 143.1% at
            // t30 against the ≤145 target (margin 1.9) while holding the approval streak. The old
            // cuts-only -20% line was the suppressed-revenue era's winner and now buys only 2.5
            // points; VAT28 alone also wins the debt objective but breaks keep_the_room at 39.9.
            (ScenarioProgress progress, Country c) = RunLine(def, seed: 777, spendingCutPercent: -20f, label: "consolidation-cut20-vat25", vatOverride: 25f);
            LogVerdict("consolidation-cut20-vat25", def, progress, c);

            int failures = 0;
            if (progress.Verdict != ScenarioVerdict.Won)
            {
                Debug.LogError($"ITALYSLICE: the measured cut20+VAT25 consolidation line should WIN but resolved {progress.Verdict} - " +
                               "a winnable line does not exist at the calibration as authored, which is the finding, not something to force.");
                failures++;
            }
            else
            {
                Debug.Log("ITALYSLICE: CONFIRMED - a real, measured consolidation line wins. Not a second unwinnable premise.");
            }

            // The Sustained objective's REAL exercise: report its final streak honestly, whichever
            // way it landed, rather than asserting a specific number the measurement's 5-turn
            // sampling couldn't pin down precisely.
            ObjectiveProgress roomState = ScenarioEvaluator.FindProgress(progress, "keep_the_room");
            Debug.Log($"ITALYSLICE: keep_the_room (Sustained) - Met={roomState.Met}, final ConsecutiveTurns={roomState.ConsecutiveTurns} " +
                      $"of {def.Objectives[1].RequiredTurns} required, final Approval={roomState.LastValue:F2}.");

            return failures;
        }

        private static (ScenarioProgress, Country) RunLine(ScenarioDefinition def, int seed, float spendingCutPercent, string label, float vatOverride = -1f)
        {
            SimulationRandom.Seed(seed);
            World world = WorldFactory.CreateDefault();
            var go = new GameObject($"ITALYSLICE_{label}");
            try
            {
                SimulationManager sim = go.AddComponent<SimulationManager>();
                sim.SetWorld(world);
                Country c = world.GetCountry(def.Country);
                def.ApplyDeltas?.Invoke(world, c);

                ScenarioProgress progress = ScenarioEvaluator.Begin(def, sim.CurrentTurn);
                var decisions = new Dictionary<CountryId, PolicyDecision>();
                foreach (Country x in world.Countries) { decisions[x.Id] = PolicyDecision.None(); }

                for (int turn = 1; turn <= def.EndTurn; turn++)
                {
                    PolicyDecision d = PolicyDecision.None();
                    if (turn == 1 && spendingCutPercent != 0f)
                    {
                        d.SpendingLineChanges[SpendingCategory.InfrastructureAndDevelopment] = spendingCutPercent;
                        d.SpendingLineChanges[SpendingCategory.PublicServices] = spendingCutPercent;
                        d.SpendingLineChanges[SpendingCategory.Administration] = spendingCutPercent;
                    }
                    if (turn == 1 && vatOverride >= 0f)
                    {
                        d.TaxRateOverrides[TaxType.VAT] = vatOverride;
                    }
                    decisions[def.Country] = d;

                    for (int day = 0; day < SimulationManager.DaysPerTurn; day++) { sim.AdvanceDay(); }
                    sim.AdvanceTurn(decisions);
                    ScenarioEvaluator.EvaluateAtBoundary(def, progress, c, sim.CurrentTurn);

                    if (progress.Verdict != ScenarioVerdict.Undecided) { break; }
                }

                return (progress, c);
            }
            finally
            {
                // Safe to destroy here even though the caller keeps using `c`/`progress` after
                // this returns: World/Country/ScenarioProgress are plain C# objects with no
                // dependency on the Unity GameObject - only SimulationManager needed it, and
                // neither caller reads `sim` after RunLine returns.
                Object.DestroyImmediate(go);
            }
        }

        private static void LogVerdict(string label, ScenarioDefinition def, ScenarioProgress progress, Country c)
        {
            Debug.Log($"ITALYSLICE[{label}]: verdict={progress.Verdict} - {progress.VerdictReason}");
            foreach (ScenarioObjective objective in def.Objectives)
            {
                ObjectiveProgress state = ScenarioEvaluator.FindProgress(progress, objective.Id);
                Debug.Log($"ITALYSLICE[{label}] objective '{objective.Id}': met={state.Met} failed={state.Failed} " +
                          $"value={state.LastValue:F2}{objective.Unit} margin={ScenarioEvaluator.MarginOf(objective, state.LastValue):+0.00;-0.00} " +
                          $"streak={state.ConsecutiveTurns}");
            }
        }

        /// <summary>The Sustained streak crossing a real save mid-run - the new persistence shape
        /// this scenario introduces to content (Inherit the Fund's objectives never used it).</summary>
        private static int RunSaveCrossingMidSustain()
        {
            ScenarioDefinition def = ScenarioLibrary.ById("italy_debt_crisis");
            SimulationRandom.Seed(424242);
            World world = WorldFactory.CreateDefault();
            var go = new GameObject("ITALYSLICE_SAVE");
            try
            {
                SimulationManager sim = go.AddComponent<SimulationManager>();
                sim.SetWorld(world);
                Country c = world.GetCountry(def.Country);
                def.ApplyDeltas?.Invoke(world, c);

                ScenarioProgress progress = ScenarioEvaluator.Begin(def, sim.CurrentTurn);
                var decisions = new Dictionary<CountryId, PolicyDecision>();
                foreach (Country x in world.Countries) { decisions[x.Id] = PolicyDecision.None(); }

                // A real -20% consolidation line, saved partway through, before EndTurn.
                int guard = 0;
                ObjectiveProgress roomState = null;
                while (guard < 8)
                {
                    PolicyDecision d = PolicyDecision.None();
                    if (guard == 0)
                    {
                        d.SpendingLineChanges[SpendingCategory.InfrastructureAndDevelopment] = -20f;
                        d.SpendingLineChanges[SpendingCategory.PublicServices] = -20f;
                        d.SpendingLineChanges[SpendingCategory.Administration] = -20f;
                    }
                    decisions[def.Country] = d;

                    for (int day = 0; day < SimulationManager.DaysPerTurn; day++) { sim.AdvanceDay(); }
                    sim.AdvanceTurn(decisions);
                    ScenarioEvaluator.EvaluateAtBoundary(def, progress, c, sim.CurrentTurn);
                    roomState = ScenarioEvaluator.FindProgress(progress, "keep_the_room");
                    guard++;
                }

                Debug.Log($"ITALYSLICE: saving mid-run at turn {sim.CurrentTurn}, keep_the_room streak={roomState.ConsecutiveTurns}, Met={roomState.Met}.");

                var ui = new UiDraftState { Scenario = progress, ScenarioVerdictPending = false };
                SaveGame save = SaveGameService.CreateSaveGame(sim, world, def.Country, ui,
                    new System.DateTime(2026, 8, 18, 0, 0, 0, System.DateTimeKind.Utc));
                string json = SaveGameService.Serialize(save);
                SaveGame loaded = SaveGameService.Deserialize(json);

                ScenarioProgress restored = loaded.Ui?.Scenario;
                ObjectiveProgress restoredRoom = restored != null ? ScenarioEvaluator.FindProgress(restored, "keep_the_room") : null;

                int failures = 0;
                if (restored == null || restoredRoom == null
                    || restoredRoom.ConsecutiveTurns != roomState.ConsecutiveTurns
                    || restoredRoom.Met != roomState.Met)
                {
                    Debug.LogError($"ITALYSLICE: the Sustained streak did NOT cross the save intact - " +
                                   $"streak {roomState.ConsecutiveTurns}->{restoredRoom?.ConsecutiveTurns}, met {roomState.Met}->{restoredRoom?.Met}.");
                    failures++;
                }
                else
                {
                    Debug.Log("ITALYSLICE: the Sustained streak crossed the save intact.");
                }

                // AND it keeps evaluating correctly on the restored world.
                var goB = new GameObject("ITALYSLICE_SAVE_B");
                try
                {
                    SimulationManager simB = goB.AddComponent<SimulationManager>();
                    SaveGameService.RestoreInto(simB, loaded);
                    World worldB = loaded.World;
                    Country cB = worldB.GetCountry(def.Country);
                    var decisionsB = new Dictionary<CountryId, PolicyDecision>();
                    foreach (Country x in worldB.Countries) { decisionsB[x.Id] = PolicyDecision.None(); }

                    for (int day = 0; day < SimulationManager.DaysPerTurn; day++) { simB.AdvanceDay(); }
                    simB.AdvanceTurn(decisionsB);
                    ScenarioEvaluator.EvaluateAtBoundary(def, restored, cB, simB.CurrentTurn);
                    ObjectiveProgress afterOneMore = ScenarioEvaluator.FindProgress(restored, "keep_the_room");

                    bool heldThisTurn = cB.State.ApprovalRating >= 40f;
                    int expected = heldThisTurn ? roomState.ConsecutiveTurns + 1 : 0;
                    if (afterOneMore.ConsecutiveTurns != expected)
                    {
                        Debug.LogError($"ITALYSLICE: post-load streak continuation is WRONG - expected {expected} " +
                                       $"(held-this-turn={heldThisTurn}), got {afterOneMore.ConsecutiveTurns}.");
                        failures++;
                    }
                    else
                    {
                        Debug.Log($"ITALYSLICE: post-load evaluation continues the streak correctly ({roomState.ConsecutiveTurns} -> {afterOneMore.ConsecutiveTurns}).");
                    }
                }
                finally
                {
                    Object.DestroyImmediate(goB);
                }

                return failures;
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }
    }
}
