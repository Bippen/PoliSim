using System;
using System.Collections.Generic;

namespace PoliSim.Data
{
    /// <summary>
    /// STEP 3'S DELIVERABLE: the authoring contract for a playable scenario (R-S3a).
    ///
    /// <para><b>Read the inversion first, because it is why none of the validation-scenario code is
    /// reused.</b> `SimulationTestRunner`'s 15 "scenarios" supply the DECISIONS turn by turn and hold
    /// the world at an unmodified <see cref="WorldFactory.CreateDefault"/>. A playable scenario is the
    /// mirror image: it supplies the WORLD (<see cref="ApplyDeltas"/>) and the player supplies the
    /// decisions. Nothing here extends that machinery; the two only share a discipline.</para>
    ///
    /// <para><b>The format is data, not code paths</b> - one definition per scenario in
    /// <see cref="ScenarioLibrary"/>, mirroring `EventSystem.EventPool` and
    /// `ForeignPolicySystem.MeetingPool` (both hardcoded authored pools). The save persists the
    /// scenario's ID and its objective PROGRESS, never the definition: the definition is looked up by
    /// id at load, the same "record values, not formulas" principle `FiscalTurnReport` states.</para>
    ///
    /// <para><b>The remaining five scenarios of the ruled slate are meant to be SUBSETS of this
    /// shape</b> - that is the slice's stated test. If one needs a new objective kind, that is a
    /// finding about the grammar, not a reason to special-case the scenario.</para>
    /// </summary>
    public class ScenarioDefinition
    {
        /// <summary>Stable across builds - this is what the save carries, so renaming it breaks saves.</summary>
        public string Id;

        public string Name;

        /// <summary>The premise, shown at entry and again on the verdict. Two or three sentences.</summary>
        public string Premise;

        /// <summary>Which country the player governs. A scenario IS a start, so picking one replaces
        /// country selection rather than following it.</summary>
        public CountryId Country;

        /// <summary>The scenario's own clock - the turn its terminal objectives are judged on.
        /// ⚠ NOT an election clock (the standing ruling): elections still occur in the world and
        /// losing one still ends the run through the existing game-over path, but the scenario's
        /// win/lose is its objectives, never "survived to a date".</summary>
        public int EndTurn;

        /// <summary>R-S3e: this scenario's foreign-policy interrupt pacing, as a multiplier on
        /// `ForeignPolicySystem.MeetingChancePerDay`. **1.0 by default so nothing existing moves** -
        /// the global constant is untouched and pacing becomes a per-scenario design lever (a
        /// diplomatic scenario legitimately wants more; a disinflation run wants the player watching
        /// the Phillips curve). The three-rate playtest the ruling asks for varies THIS.</summary>
        public float ForeignPolicyCadenceMultiplier = 1f;

        /// <summary>
        /// THE SEED-DELTA SEAM: mutate the freshly-built world into this scenario's start. Runs ONCE,
        /// after <see cref="WorldFactory.CreateDefault"/> and before the player's first day - the same
        /// instant `SelectPlayerCountry` commits, so no turn has advanced and nothing has been
        /// observed yet.
        ///
        /// ⚠ A delta is a STARTING STATE, never a rule change: write fields, do not install
        /// behaviour. The validation scenarios smuggled state mutation through their decision builder
        /// (swfstress creates two funds at turn 1) - that proved the capability is safe and
        /// deterministic, and this is that capability given its own seam.
        /// </summary>
        public Action<World, Country> ApplyDeltas;

        public List<ScenarioObjective> Objectives = new List<ScenarioObjective>();
    }

    /// <summary>The four objective forms (R-S3a), derived from what the model can evaluate HONESTLY
    /// under Step 2's attribution classes: boundary formulas and period stances are exact at a
    /// boundary, events are dated, and compounding feedback may be THRESHOLDED but never
    /// ATTRIBUTED - a scenario may say "debt ended above 130", never "your tax policy caused 12 of
    /// those points".</summary>
    public enum ObjectiveKind
    {
        /// <summary>Must hold at <see cref="ScenarioObjective.ByTurn"/>.</summary>
        ThresholdAtDate,

        /// <summary>Must hold for <see cref="ScenarioObjective.RequiredTurns"/> CONSECUTIVE boundaries.</summary>
        Sustained,

        /// <summary>Must hold at EVERY boundary - the fail-state form; breaching it once ends the run.</summary>
        NeverBreach,

        /// <summary>Judged once, at <see cref="ScenarioDefinition.EndTurn"/>.</summary>
        Terminal
    }

    public enum ObjectiveComparison
    {
        AtMost,
        AtLeast
    }

    /// <summary>
    /// One objective. <see cref="Read"/> is a live-model reader (R-S3a: objectives evaluate the LIVE
    /// model, per R-S2c's precedent and because the trace panel explains live values - an objective
    /// judged on a number the player cannot trace is unfair in exactly the way Step 2 exists to
    /// prevent).
    ///
    /// ⚠ **Published-judged objectives are DEFERRED, with their trigger**: `PublishedStat` carries
    /// only the 12 stats with real sourced release rules and deliberately never publishes
    /// `DebtToGdpRatio`, so the debt axis cannot be published-judged at all; and GDP's three-stage
    /// revision would let an objective flip after the fact. The trigger is a scenario that is ABOUT
    /// information asymmetry, and it must ship with its revision rule stated in writing (judge the
    /// final figure, or judge the figure as it stood on the date - pick one).
    /// </summary>
    public class ScenarioObjective
    {
        /// <summary>Stable across builds - the save keys progress by this.</summary>
        public string Id;

        /// <summary>Player-facing, and it must name the measure and the bar ("End net creditor:
        /// government debt at or below 0").</summary>
        public string Description;

        public ObjectiveKind Kind;
        public ObjectiveComparison Comparison;
        public float Target;

        /// <summary>The measured quantity, read live off the country at a turn boundary. Never
        /// recomputes a model formula - it reads what the simulation produced.</summary>
        public Func<Country, float> Read;

        /// <summary>Unit for the margin line on the verdict ("%", " pts", "" for a raw index).</summary>
        public string Unit = "";

        /// <summary><see cref="ObjectiveKind.ThresholdAtDate"/> only.</summary>
        public int ByTurn;

        /// <summary><see cref="ObjectiveKind.Sustained"/> only.</summary>
        public int RequiredTurns;
    }

    /// <summary>How a scenario ended. <see cref="Undecided"/> is the in-progress value and the one an
    /// old save deserializes to.</summary>
    public enum ScenarioVerdict
    {
        Undecided,
        Won,
        Lost
    }

    /// <summary>
    /// The persisted half (R-S3e's "one id plus counters"): everything needed to resume mid-scenario
    /// and to draw the verdict, and NOTHING that lives in the definition. Rides in
    /// `UiDraftState` beside `IsGameOver`/`GameOverReason`, which is where the analogous run-ending
    /// state already lives.
    /// </summary>
    public class ScenarioProgress
    {
        public string ScenarioId;
        public int StartTurn;
        public ScenarioVerdict Verdict = ScenarioVerdict.Undecided;

        /// <summary>Set when the verdict resolves - the one-line reason, same register as
        /// `GameOverReason`.</summary>
        public string VerdictReason;

        public List<ObjectiveProgress> Objectives = new List<ObjectiveProgress>();
    }

    /// <summary>Per-objective state. <see cref="LastValue"/> is what the verdict's MARGIN line reads
    /// (R-S3d: margins are measured numbers, unlike a composite score).</summary>
    public class ObjectiveProgress
    {
        public string ObjectiveId;
        public bool Met;
        public bool Failed;
        public int ConsecutiveTurns;
        public float LastValue;
        public bool HasValue;
    }
}
