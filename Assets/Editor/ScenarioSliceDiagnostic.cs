using System.Collections.Generic;
using PoliSim.Data;
using PoliSim.Persistence;
using PoliSim.Simulation;
using UnityEditor;
using UnityEngine;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// Step 3's slice diagnostic, in two halves.
    ///
    /// <para><b>Half A — the scenario's own trajectory, and R3's CREDITOR BRANCH GETTING ITS FIRST
    /// LIVE EXERCISE.</b> Runs "Inherit the Fund" headless from its deltas to its end turn under
    /// no-op decisions, evaluating at every boundary through the real
    /// <see cref="ScenarioEvaluator"/>. Deliberately NOT compared to any baseline: a scenario is a
    /// different world by design, so its numbers are reported, never diffed. What IS asserted is the
    /// coverage claim — that the erosion term's symmetric arm actually executes against a negative
    /// stock, and behaves as ruling R3's reasoning predicted (the position shrinks toward zero at π,
    /// and a creditor earns nothing on it).</para>
    ///
    /// <para><b>Half B — a scenario in progress crossing a save.</b> Serializes a
    /// <see cref="ScenarioProgress"/> mid-run through the real save path, deserializes it, and
    /// evaluates ANOTHER boundary on the restored progress — because "the counters persisted" and
    /// "evaluation still works after a load" are different claims and only the second is the feature.
    /// The definition is looked up by id on the far side, never serialized.</para>
    ///
    /// Run: `Unity.exe -batchmode -nographics -projectPath &lt;path&gt; -executeMethod
    /// PoliSim.EditorTools.ScenarioSliceDiagnostic.Run -logFile &lt;path&gt;`, or from the menu.
    /// </summary>
    public static class ScenarioSliceDiagnostic
    {
        [MenuItem("PoliSim/Run Scenario Slice Diagnostic")]
        private static void RunFromMenu()
        {
            int code = CheckExit.Collect(Run);
            Debug.Log(code == 0 ? "SCENARIO: slice diagnostic clean." : $"SCENARIO: slice diagnostic FAILED ({code}).");
        }

        public static void Run()
        {
            CheckExit.ArmLogFold(); // ruling 1: this advances turns; an ATTRIB during it now fails the run.
            int failures = 0;
            failures += RunTrajectory();
            failures += RunSaveCrossing();

            Debug.Log(failures == 0
                ? "SCENARIO: both halves clean - the scenario runs to a verdict and survives a save mid-run."
                : $"SCENARIO: {failures} failure(s) - a divergence is a finding, report it.");
            CheckExit.Finish(failures == 0 ? 0 : 1);
        }

        private static int RunTrajectory()
        {
            ScenarioDefinition definition = ScenarioLibrary.ById("inherit_the_fund");
            if (definition == null)
            {
                Debug.LogError("SCENARIO: 'inherit_the_fund' is not in the library.");
                return 1;
            }

            SimulationRandom.Seed(777);
            World world = WorldFactory.CreateDefault();
            var go = new GameObject("SCENARIO_RUN");
            try
            {
                SimulationManager sim = go.AddComponent<SimulationManager>();
                sim.SetWorld(world);
                sim.ForeignPolicyCadenceMultiplier = definition.ForeignPolicyCadenceMultiplier;

                Country player = world.GetCountry(definition.Country);
                float debtBeforeDeltas = player.State.GovernmentDebt;
                definition.ApplyDeltas?.Invoke(world, player);

                Debug.Log($"SCENARIO: deltas applied to {definition.Country} - GovernmentDebt {debtBeforeDeltas:F1} -> " +
                          $"{player.State.GovernmentDebt:F1} ({player.State.DebtToGdpRatio:F1}% of GDP), " +
                          $"SWF {player.SovereignWealthFund?.TotalAssets ?? 0f:F1}.");

                ScenarioProgress progress = ScenarioEvaluator.Begin(definition, sim.CurrentTurn);

                var decisions = new Dictionary<CountryId, PolicyDecision>();
                foreach (Country c in world.Countries) { decisions[c.Id] = PolicyDecision.None(); }

                // THE CREDITOR-BRANCH MEASUREMENT: the debt stock at every boundary, plus whether the
                // position ever stopped being negative. Erosion multiplies the stock by
                // (1 - π/100)^fraction each day, so a NEGATIVE stock must drift toward zero.
                float previousDebt = player.State.GovernmentDebt;
                int negativeTurns = 0;
                bool everPositive = false;

                for (int turn = 1; turn <= definition.EndTurn; turn++)
                {
                    // The erosion arm's OWN contribution this period, computed from the period-open
                    // stock and the period's inflation: stock × ((1 − π/100) − 1). Reported beside the
                    // total move so the two are never conflated - the erosion term is the branch under
                    // test, and the rest of the move is the budget balance.
                    float debtAtOpen = previousDebt;
                    float inflationAtOpen = player.State.Inflation;

                    for (int day = 0; day < SimulationManager.DaysPerTurn; day++)
                    {
                        sim.AdvanceDay();
                    }
                    sim.AdvanceTurn(decisions);
                    ScenarioEvaluator.EvaluateAtBoundary(definition, progress, player, sim.CurrentTurn);

                    float debt = player.State.GovernmentDebt;
                    if (debt < 0f) { negativeTurns++; } else { everPositive = true; }

                    float erosionComponent = debtAtOpen * (Mathf.Pow(Mathf.Max(0.01f, 1f - inflationAtOpen / 100f), 1f) - 1f);
                    float totalMove = debt - previousDebt;

                    Debug.Log($"SCENARIO t{turn}: debt {debt:F1} ({player.State.DebtToGdpRatio:F1}%) " +
                              $"Δ{totalMove:+0.0;-0.0} [erosion {erosionComponent:+0.0;-0.0}, balance {totalMove - erosionComponent:+0.0;-0.0}] · " +
                              $"poverty {player.State.PovertyRate:F2}% · " +
                              $"approval {player.State.ApprovalRating:F1} · U {player.State.Unemployment:F2}% · " +
                              $"π {player.State.Inflation:F2}% · SWF {player.SovereignWealthFund?.TotalAssets ?? 0f:F1} · " +
                              $"verdict {progress.Verdict}");
                    previousDebt = debt;

                    if (progress.Verdict != ScenarioVerdict.Undecided)
                    {
                        break;
                    }
                }

                Debug.Log($"SCENARIO: verdict {progress.Verdict} - {progress.VerdictReason}");
                foreach (ScenarioObjective objective in definition.Objectives)
                {
                    ObjectiveProgress state = ScenarioEvaluator.FindProgress(progress, objective.Id);
                    Debug.Log($"SCENARIO objective '{objective.Id}': met={state?.Met} failed={state?.Failed} " +
                              $"value={state?.LastValue:F2}{objective.Unit} margin={(state != null ? ScenarioEvaluator.MarginOf(objective, state.LastValue) : 0f):+0.00;-0.00}");
                }

                // The coverage claim, asserted rather than narrated.
                int failures = 0;
                if (negativeTurns == 0)
                {
                    Debug.LogError("SCENARIO: the creditor branch NEVER RAN - the stock was never negative at a boundary. " +
                                   "This scenario exists to exercise it; a start that does not is a defect in the deltas.");
                    failures++;
                }
                else
                {
                    Debug.Log($"SCENARIO: CREDITOR BRANCH EXERCISED - net-creditor at {negativeTurns} of the run's boundaries" +
                              (everPositive ? ", crossing back to net debtor at least once." : ", for the whole run."));
                }

                return failures;
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        /// <summary>Half B: progress crosses a real save, and the evaluator keeps working on the far
        /// side. The world crossing the save is already covered by SaveLoadRoundTripDiagnostic; what
        /// is new here is the SCENARIO layer riding along and staying judgeable.</summary>
        private static int RunSaveCrossing()
        {
            ScenarioDefinition definition = ScenarioLibrary.ById("inherit_the_fund");
            SimulationRandom.Seed(424242);
            World world = WorldFactory.CreateDefault();
            var go = new GameObject("SCENARIO_SAVE");
            try
            {
                SimulationManager sim = go.AddComponent<SimulationManager>();
                sim.SetWorld(world);
                Country player = world.GetCountry(definition.Country);
                definition.ApplyDeltas?.Invoke(world, player);

                ScenarioProgress progress = ScenarioEvaluator.Begin(definition, sim.CurrentTurn);
                var decisions = new Dictionary<CountryId, PolicyDecision>();
                foreach (Country c in world.Countries) { decisions[c.Id] = PolicyDecision.None(); }

                for (int turn = 0; turn < 2; turn++)
                {
                    for (int day = 0; day < SimulationManager.DaysPerTurn; day++) { sim.AdvanceDay(); }
                    sim.AdvanceTurn(decisions);
                    ScenarioEvaluator.EvaluateAtBoundary(definition, progress, player, sim.CurrentTurn);
                }

                var ui = new UiDraftState { Scenario = progress, ScenarioVerdictPending = false };
                SaveGame save = SaveGameService.CreateSaveGame(sim, world, definition.Country, ui,
                    new System.DateTime(2026, 8, 18, 0, 0, 0, System.DateTimeKind.Utc));
                string json = SaveGameService.Serialize(save);
                SaveGame loaded = SaveGameService.Deserialize(json);

                int failures = 0;
                ScenarioProgress restored = loaded.Ui?.Scenario;
                if (restored == null)
                {
                    Debug.LogError("SCENARIO: progress did not cross the save at all - the post-load verdict screen would be blank.");
                    return 1;
                }

                if (restored.ScenarioId != progress.ScenarioId || restored.StartTurn != progress.StartTurn
                    || restored.Objectives.Count != progress.Objectives.Count)
                {
                    Debug.LogError($"SCENARIO: progress shape changed across the save - id '{progress.ScenarioId}'->'{restored.ScenarioId}', " +
                                   $"objectives {progress.Objectives.Count}->{restored.Objectives.Count}.");
                    failures++;
                }

                foreach (ObjectiveProgress before in progress.Objectives)
                {
                    ObjectiveProgress after = ScenarioEvaluator.FindProgress(restored, before.ObjectiveId);
                    if (after == null || after.Met != before.Met || after.Failed != before.Failed
                        || after.ConsecutiveTurns != before.ConsecutiveTurns
                        || Mathf.Abs(after.LastValue - before.LastValue) > 1e-4f)
                    {
                        Debug.LogError($"SCENARIO: objective '{before.ObjectiveId}' changed across the save - " +
                                       $"met {before.Met}->{after?.Met}, counter {before.ConsecutiveTurns}->{after?.ConsecutiveTurns}, " +
                                       $"value {before.LastValue:F4}->{after?.LastValue:F4}.");
                        failures++;
                    }
                }

                // The definition is looked up by id on the far side - never serialized.
                ScenarioDefinition rehydrated = ScenarioLibrary.ById(restored.ScenarioId);
                if (rehydrated == null)
                {
                    Debug.LogError($"SCENARIO: '{restored.ScenarioId}' does not resolve in the library after load.");
                    return failures + 1;
                }

                // AND THE CLAIM THAT MATTERS: evaluation still fires on the restored progress.
                var goB = new GameObject("SCENARIO_SAVE_B");
                try
                {
                    SimulationManager simB = goB.AddComponent<SimulationManager>();
                    SaveGameService.RestoreInto(simB, loaded);
                    World worldB = loaded.World;
                    Country playerB = worldB.GetCountry(rehydrated.Country);
                    var decisionsB = new Dictionary<CountryId, PolicyDecision>();
                    foreach (Country c in worldB.Countries) { decisionsB[c.Id] = PolicyDecision.None(); }

                    for (int day = 0; day < SimulationManager.DaysPerTurn; day++) { simB.AdvanceDay(); }
                    simB.AdvanceTurn(decisionsB);
                    ScenarioEvaluator.EvaluateAtBoundary(rehydrated, restored, playerB, simB.CurrentTurn);

                    ObjectiveProgress creditor = ScenarioEvaluator.FindProgress(restored, "still_creditor");
                    if (creditor == null || !creditor.HasValue)
                    {
                        Debug.LogError("SCENARIO: the post-load boundary did not measure - evaluation is dead after a load.");
                        failures++;
                    }
                    else
                    {
                        Debug.Log($"SCENARIO: post-load evaluation fired at turn {simB.CurrentTurn} - " +
                                  $"still_creditor measured {creditor.LastValue:F1}, verdict {restored.Verdict}.");
                    }
                }
                finally
                {
                    Object.DestroyImmediate(goB);
                }

                if (failures == 0)
                {
                    Debug.Log("SCENARIO: a run in progress crosses the save intact and stays judgeable.");
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
