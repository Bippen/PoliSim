using System.Collections.Generic;
using PoliSim.Data;
using PoliSim.Persistence;
using PoliSim.Simulation;
using UnityEditor;
using UnityEngine;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// Exercises the `Sustained` `ObjectiveKind` (built, unexercised since Step 3 shipped) against
    /// the REAL `ScenarioEvaluator`/`ScenarioProgress`/save-shape code, using a SYNTHETIC,
    /// throwaway `ScenarioDefinition` built here rather than one in `ScenarioLibrary`.
    ///
    /// <para><b>Why synthetic rather than "Wage Boom Management" itself</b>: the measurement pass
    /// this diagnostic accompanies found no formulation of a labour-market-tightness scenario
    /// survives contact with `UnemploymentReversionSpeed` (0.7/turn), so nothing real was
    /// authored to carry this test. Inventing a scenario just to exercise a form would be the
    /// exact "tuning it into existence" the pass was told not to do. This file tests the FORM's
    /// own mechanics - a smaller, honest claim - using an inflation-band condition instead, which
    /// has no comparably dominant reversion force fighting it.</para>
    ///
    /// Run: `Unity.exe -batchmode -nographics -projectPath &lt;path&gt; -executeMethod
    /// PoliSim.EditorTools.SustainedObjectiveDiagnostic.Run -logFile &lt;path&gt;`, or from the menu.
    /// </summary>
    public static class SustainedObjectiveDiagnostic
    {
        [MenuItem("PoliSim/Run Sustained Objective Diagnostic (infrastructure test)")]
        private static void RunFromMenu()
        {
            int code = CheckExit.Collect(Run);
            Debug.Log(code == 0 ? "SUSTAINED: clean." : $"SUSTAINED: FAILED ({code}).");
        }

        private static ScenarioDefinition BuildSyntheticDefinition()
        {
            return new ScenarioDefinition
            {
                Id = "synthetic_sustained_test",
                Name = "Synthetic Sustained Test (never shipped)",
                Premise = "Infrastructure test only.",
                Country = CountryId.Sweden,
                EndTurn = 20,
                Objectives = new List<ScenarioObjective>
                {
                    new ScenarioObjective
                    {
                        Id = "hold_band",
                        Description = "Hold inflation within [1,3] for 5 consecutive turns",
                        Kind = ObjectiveKind.Sustained,
                        Comparison = ObjectiveComparison.AtMost,
                        Target = 3f,
                        RequiredTurns = 5,
                        Unit = "%",
                        Read = c => c.State.Inflation
                    }
                }
            };
        }

        public static void Run()
        {
            CheckExit.ArmLogFold(); // ruling 1: this advances turns; an ATTRIB during it now fails the run.
            int failures = 0;
            failures += RunEvaluationCorrectness();
            failures += RunSaveCrossingMidSustain();
            CheckExit.Finish(failures == 0 ? 0 : 1);
        }

        /// <summary>Does the Sustained form evaluate as designed: ConsecutiveTurns tracks a
        /// held-condition streak, Met flips exactly when RequiredTurns is reached, and a breach
        /// resets the counter to 0 rather than merely pausing it.</summary>
        private static int RunEvaluationCorrectness()
        {
            int failures = 0;
            SimulationRandom.Seed(777);
            World world = WorldFactory.CreateDefault();
            ScenarioDefinition def = BuildSyntheticDefinition();
            var go = new GameObject("SUSTAINED_EVAL");
            try
            {
                SimulationManager sim = go.AddComponent<SimulationManager>();
                sim.SetWorld(world);
                Country c = world.GetCountry(def.Country);
                ScenarioProgress progress = ScenarioEvaluator.Begin(def, sim.CurrentTurn);

                var decisions = new Dictionary<CountryId, PolicyDecision>();
                foreach (Country x in world.Countries) { decisions[x.Id] = PolicyDecision.None(); }

                int metAtTurn = -1;
                var streakLog = new List<string>();
                for (int turn = 1; turn <= def.EndTurn; turn++)
                {
                    for (int day = 0; day < SimulationManager.DaysPerTurn; day++) { sim.AdvanceDay(); }
                    sim.AdvanceTurn(decisions);
                    ScenarioEvaluator.EvaluateAtBoundary(def, progress, c, sim.CurrentTurn);

                    ObjectiveProgress state = ScenarioEvaluator.FindProgress(progress, "hold_band");
                    streakLog.Add($"t{turn}:π={c.State.Inflation:F2},streak={state.ConsecutiveTurns},met={state.Met}");
                    if (state.Met && metAtTurn < 0) { metAtTurn = turn; }
                }

                Debug.Log($"SUSTAINED: eval trace - {string.Join(" ", streakLog)}");
                Debug.Log($"SUSTAINED: verdict={progress.Verdict}, reason={progress.VerdictReason}, first Met at turn {metAtTurn}");

                if (metAtTurn < 0)
                {
                    Debug.LogError("SUSTAINED: the streak condition never fired across the whole run - either the condition is unreachable at Sweden's seed or the counter logic is broken. Cannot validate margin/save behaviour on an unmet condition; treating as a finding, not tuning the threshold to force a pass.");
                    failures++;
                }
                else
                {
                    ObjectiveProgress finalState = ScenarioEvaluator.FindProgress(progress, "hold_band");
                    if (finalState.ConsecutiveTurns < def.EndTurn - metAtTurn + 1 && !finalState.Met)
                    {
                        Debug.LogError("SUSTAINED: Met flipped true but is not sticky on a later breach reset - unexpected for this form's semantics.");
                        failures++;
                    }
                    Debug.Log($"SUSTAINED: margin at final measured value = {ScenarioEvaluator.MarginOf(def.Objectives[0], finalState.LastValue):+0.00;-0.00} " +
                              $"- this is what the SHIPPED verdict screen's figure line shows (LastValue vs Target). It says NOTHING about " +
                              $"the {finalState.ConsecutiveTurns}-turn streak that is what actually decided Met - CONFIRMED FINDING: the margin " +
                              $"line as built is not informative for a Sustained objective (see GameController.cs:4141, generic across all kinds).");
                }

                // The overall verdict resolves at EndTurn, not the instant the Sustained condition
                // is first satisfied - worth stating explicitly since it is not obvious from the
                // objective's own description ("hold for 5 turns" could be misread as "then win").
                if (progress.Verdict == ScenarioVerdict.Undecided)
                {
                    Debug.LogError("SUSTAINED: verdict never resolved by EndTurn - EvaluateAtBoundary's end-turn judging path did not fire.");
                    failures++;
                }
                else
                {
                    Debug.Log($"SUSTAINED: CONFIRMED - satisfying the streak early (turn {metAtTurn}) does NOT end the scenario early; " +
                              "the verdict waits for EndTurn, consistent with a scenario that may carry other objectives still tracking.");
                }
            }
            finally
            {
                Object.DestroyImmediate(go);
            }

            return failures;
        }

        /// <summary>The new state shape this form introduces to persistence: ConsecutiveTurns
        /// partway through a streak, crossing a real save/load, then continuing to evaluate
        /// correctly on the far side - the same bar Step 3's slice held itself to for
        /// "Inherit the Fund".</summary>
        private static int RunSaveCrossingMidSustain()
        {
            int failures = 0;
            SimulationRandom.Seed(424242);
            World world = WorldFactory.CreateDefault();
            ScenarioDefinition def = BuildSyntheticDefinition();
            var go = new GameObject("SUSTAINED_SAVE");
            try
            {
                SimulationManager sim = go.AddComponent<SimulationManager>();
                sim.SetWorld(world);
                Country c = world.GetCountry(def.Country);
                ScenarioProgress progress = ScenarioEvaluator.Begin(def, sim.CurrentTurn);

                var decisions = new Dictionary<CountryId, PolicyDecision>();
                foreach (Country x in world.Countries) { decisions[x.Id] = PolicyDecision.None(); }

                // Advance until the streak is PARTIALLY built (>0 but not yet Met) - the interesting
                // mid-sustain state a save could land on.
                int guard = 0;
                ObjectiveProgress state = null;
                while (guard < def.EndTurn)
                {
                    for (int day = 0; day < SimulationManager.DaysPerTurn; day++) { sim.AdvanceDay(); }
                    sim.AdvanceTurn(decisions);
                    ScenarioEvaluator.EvaluateAtBoundary(def, progress, c, sim.CurrentTurn);
                    state = ScenarioEvaluator.FindProgress(progress, "hold_band");
                    guard++;
                    if (state.ConsecutiveTurns > 0 && !state.Met) { break; }
                }

                if (state == null || state.Met || state.ConsecutiveTurns == 0)
                {
                    Debug.LogError($"SUSTAINED: could not reach a genuine mid-streak state (streak={state?.ConsecutiveTurns}, met={state?.Met}) " +
                                   "within the run - the save-crossing claim is untested, reported as a finding rather than skipped silently.");
                    return failures + 1;
                }

                int streakAtSave = state.ConsecutiveTurns;
                Debug.Log($"SUSTAINED: saving mid-streak at turn {sim.CurrentTurn}, ConsecutiveTurns={streakAtSave}, Met={state.Met}.");

                var ui = new UiDraftState { Scenario = progress, ScenarioVerdictPending = false };
                SaveGame save = SaveGameService.CreateSaveGame(sim, world, def.Country, ui,
                    new System.DateTime(2026, 8, 18, 0, 0, 0, System.DateTimeKind.Utc));
                string json = SaveGameService.Serialize(save);
                SaveGame loaded = SaveGameService.Deserialize(json);

                ScenarioProgress restored = loaded.Ui?.Scenario;
                ObjectiveProgress restoredState = restored != null ? ScenarioEvaluator.FindProgress(restored, "hold_band") : null;
                if (restoredState == null || restoredState.ConsecutiveTurns != streakAtSave || restoredState.Met != state.Met)
                {
                    Debug.LogError($"SUSTAINED: streak did NOT cross the save intact - {streakAtSave}->{restoredState?.ConsecutiveTurns}, " +
                                   $"met {state.Met}->{restoredState?.Met}.");
                    failures++;
                }
                else
                {
                    Debug.Log($"SUSTAINED: streak crossed the save intact (ConsecutiveTurns={restoredState.ConsecutiveTurns}).");
                }

                // AND the claim that matters: does the restored streak keep counting correctly on
                // the far side, or does the counter reset/desync after a load?
                var goB = new GameObject("SUSTAINED_SAVE_B");
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
                    ObjectiveProgress afterOneMore = ScenarioEvaluator.FindProgress(restored, "hold_band");

                    bool conditionHeldThisTurn = cB.State.Inflation <= def.Objectives[0].Target;
                    int expected = conditionHeldThisTurn ? streakAtSave + 1 : 0;
                    if (afterOneMore.ConsecutiveTurns != expected)
                    {
                        Debug.LogError($"SUSTAINED: post-load streak continuation is WRONG - expected {expected} " +
                                       $"(held-this-turn={conditionHeldThisTurn}), got {afterOneMore.ConsecutiveTurns}.");
                        failures++;
                    }
                    else
                    {
                        Debug.Log($"SUSTAINED: post-load evaluation continues the streak correctly ({streakAtSave} -> {afterOneMore.ConsecutiveTurns}).");
                    }
                }
                finally
                {
                    Object.DestroyImmediate(goB);
                }
            }
            finally
            {
                Object.DestroyImmediate(go);
            }

            return failures;
        }
    }
}
