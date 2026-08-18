using PoliSim.Data;

namespace PoliSim.Simulation
{
    /// <summary>
    /// Step 3's evaluator (R-S3c): reads the same live state the checks read and decides whether an
    /// authored scenario has been won, lost, or is still running.
    ///
    /// <para><b>BOUNDARY-RESIDENT, deliberately.</b> Called once per turn boundary from the same
    /// post-turn hook `CheckElection` already occupies - after `AdvanceTurn`, where the period's
    /// numbers have settled. A day-resident evaluator was rejected in scoping: a threshold that can
    /// trip mid-period would claim a precision the compounding (Class D) quantities do not have, and
    /// the sustained form counts in TURNS because that is the unit the model's own political layer
    /// resolves in.</para>
    ///
    /// <para><b>Reads, never recomputes.</b> Every objective's <see cref="ScenarioObjective.Read"/>
    /// pulls a value the simulation produced - the `FiscalTurnReport` principle applied to
    /// evaluation, so the verdict can never disagree with the ledger the trace panel shows.</para>
    ///
    /// <para><b>Elections still exist.</b> The standing ruling is that a scenario STARTS with win/lose
    /// conditions rather than an election clock - not that the political layer is suspended. Losing
    /// re-election still ends the run through the existing game-over path; that is an additional loss
    /// route, not this evaluator's business.</para>
    /// </summary>
    public static class ScenarioEvaluator
    {
        /// <summary>Creates the progress record a fresh scenario starts with - one entry per
        /// objective, in the definition's own order (the verdict screen renders that order).</summary>
        public static ScenarioProgress Begin(ScenarioDefinition definition, int currentTurn)
        {
            var progress = new ScenarioProgress
            {
                ScenarioId = definition.Id,
                StartTurn = currentTurn,
                Verdict = ScenarioVerdict.Undecided
            };

            foreach (ScenarioObjective objective in definition.Objectives)
            {
                progress.Objectives.Add(new ObjectiveProgress { ObjectiveId = objective.Id });
            }

            return progress;
        }

        /// <summary>
        /// One boundary's evaluation. Updates every objective's measured value and state, then
        /// resolves the verdict if this boundary decided it. Idempotent once decided - a resolved
        /// scenario is never re-judged.
        /// </summary>
        public static void EvaluateAtBoundary(ScenarioDefinition definition, ScenarioProgress progress, Country country, int currentTurn)
        {
            if (definition == null || progress == null || country == null || progress.Verdict != ScenarioVerdict.Undecided)
            {
                return;
            }

            bool failedThisBoundary = false;
            string failureReason = null;

            for (int i = 0; i < definition.Objectives.Count; i++)
            {
                ScenarioObjective objective = definition.Objectives[i];
                ObjectiveProgress state = FindProgress(progress, objective.Id);
                if (state == null)
                {
                    // A definition that gained an objective after this save was written: record it as
                    // present-but-unmeasured rather than crashing or silently dropping it.
                    state = new ObjectiveProgress { ObjectiveId = objective.Id };
                    progress.Objectives.Add(state);
                }

                float value = objective.Read(country);
                state.LastValue = value;
                state.HasValue = true;
                bool holds = Holds(objective, value);

                switch (objective.Kind)
                {
                    case ObjectiveKind.NeverBreach:
                        if (!holds)
                        {
                            state.Failed = true;
                            failedThisBoundary = true;
                            failureReason = objective.Description;
                        }
                        else
                        {
                            state.Met = true;
                        }
                        break;

                    case ObjectiveKind.Sustained:
                        // STICKY, found and fixed authoring Italy Debt Crisis's real content: once
                        // the streak first reaches RequiredTurns, Met stays true even if a LATER
                        // turn breaks the streak - an achievement, not a live status check. The
                        // non-sticky form (recomputing Met from the CURRENT streak every boundary)
                        // is what shipped originally and is a real fairness defect, not a
                        // preference: it would fail a player who genuinely held the condition for
                        // the required stretch and then had one unrelated bad turn near EndTurn.
                        // It also contradicted this class's own already-confirmed design intent
                        // (satisfying the streak early does NOT end the scenario early) - that only
                        // makes sense if the achievement is meant to persist to be judged at
                        // EndTurn, not to be re-litigated every turn afterward. ConsecutiveTurns
                        // itself keeps counting/resetting normally - it is still the honest streak
                        // length the verdict screen and epilogue read - only Met's STICKINESS
                        // changed.
                        state.ConsecutiveTurns = holds ? state.ConsecutiveTurns + 1 : 0;
                        state.Met = state.Met || state.ConsecutiveTurns >= objective.RequiredTurns;
                        break;

                    case ObjectiveKind.ThresholdAtDate:
                        if (currentTurn >= objective.ByTurn)
                        {
                            state.Met = holds;
                            if (!holds)
                            {
                                state.Failed = true;
                            }
                        }
                        break;

                    case ObjectiveKind.Terminal:
                        // Tracked every boundary so the panel and the margin line have a live figure,
                        // but only JUDGED at the end turn below.
                        state.Met = holds;
                        break;
                }
            }

            if (failedThisBoundary)
            {
                progress.Verdict = ScenarioVerdict.Lost;
                progress.VerdictReason = $"Failed: {failureReason}.";
                return;
            }

            if (currentTurn < definition.EndTurn)
            {
                return;
            }

            // The end turn: every objective is judged, terminal ones for the first and only time.
            int met = 0;
            foreach (ScenarioObjective objective in definition.Objectives)
            {
                ObjectiveProgress state = FindProgress(progress, objective.Id);
                if (state == null)
                {
                    continue;
                }

                if (objective.Kind == ObjectiveKind.Terminal || objective.Kind == ObjectiveKind.ThresholdAtDate)
                {
                    state.Met = state.HasValue && Holds(objective, state.LastValue);
                    state.Failed = !state.Met;
                }

                if (state.Met)
                {
                    met++;
                }
            }

            bool won = met == definition.Objectives.Count;
            progress.Verdict = won ? ScenarioVerdict.Won : ScenarioVerdict.Lost;
            progress.VerdictReason = won
                ? $"All {definition.Objectives.Count} objectives met at turn {definition.EndTurn}."
                : $"{met} of {definition.Objectives.Count} objectives met at turn {definition.EndTurn}.";
        }

        /// <summary>The measured margin for the verdict's per-objective line - signed so that
        /// positive is slack and negative is the shortfall, whichever direction the comparison runs
        /// (R-S3d: a measured number, unlike the composite score the ruling excluded).</summary>
        public static float MarginOf(ScenarioObjective objective, float value)
        {
            return objective.Comparison == ObjectiveComparison.AtMost
                ? objective.Target - value
                : value - objective.Target;
        }

        public static ObjectiveProgress FindProgress(ScenarioProgress progress, string objectiveId)
        {
            foreach (ObjectiveProgress state in progress.Objectives)
            {
                if (state.ObjectiveId == objectiveId)
                {
                    return state;
                }
            }

            return null;
        }

        private static bool Holds(ScenarioObjective objective, float value)
        {
            return objective.Comparison == ObjectiveComparison.AtMost
                ? value <= objective.Target
                : value >= objective.Target;
        }
    }
}
