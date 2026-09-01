using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// How a check ends, so the same check can run under `-executeMethod` (where it must set a process
    /// exit code) and from inside the Editor (where calling <c>EditorApplication.Exit</c> would close
    /// Unity).
    ///
    /// ⚠ **That difference is why nine checks existed and none of them ever ran outside a command line.**
    /// Every one ended in <c>EditorApplication.Exit</c>, which made a menu item impossible to add: the
    /// first person to click it would have quit the Editor. So the only way to invoke any of them was a
    /// command line someone had to remember to type, and remembering is exactly what this project has
    /// already recorded twice as a failing mechanism.
    /// </summary>
    public static class CheckExit
    {
        private static bool _collecting;
        private static int _worst;

        // The log-error fold (ruling 1, 2026-08-25). A harness's summary line and exit code must be
        // the CONJUNCTION of everything the run asserted - not only what its own code counted. The
        // instance: SaveLoadRoundTripDiagnostic printed "RT: PASS" and exited 0 while 24 ATTRIB:
        // approval-audit LogErrors, raised INSIDE the simulation (ApprovalLedgerRecorder/
        // DebtLedgerRecorder.CloseAtBoundary during AdvanceTurn), went unnoticed for a day - the
        // harness's own `failures` counter could not see a red line raised by code it merely drove.
        // ArmLogFold() subscribes to the whole log stream for the run's duration; any Error or
        // Exception - the harness's own, or one from deep in the simulation it advanced - is folded
        // into Finish's code and named on the way out. Off until armed, so a harness that has a
        // legitimate expected-error path (none does today) is unaffected until it opts in.
        private static bool _foldArmed;
        private static int _observedErrors;
        private static string _firstError;

        /// <summary>Arm the log-error fold for the rest of this run (call once at the top of a
        /// harness's Run). Every subsequent Error/Exception log - from this harness OR from the
        /// simulation it drives - raises the code Finish will exit with, so a red line nothing
        /// counted can no longer hide under a green summary. Idempotent; the process exit clears it.</summary>
        public static void ArmLogFold()
        {
            if (_foldArmed)
            {
                _observedErrors = 0;
                _firstError = null;
                return;
            }

            _foldArmed = true;
            _observedErrors = 0;
            _firstError = null;
            Application.logMessageReceived += OnLog;
        }

        private static void OnLog(string condition, string stackTrace, LogType type)
        {
            if (type != LogType.Error && type != LogType.Exception && type != LogType.Assert)
            {
                return;
            }

            _observedErrors++;
            if (_firstError == null)
            {
                _firstError = condition;
            }
        }

        /// <summary>Call instead of <c>EditorApplication.Exit</c>. Identical behaviour in batch mode.
        /// When the log fold is armed, a run that logged ANY error exits nonzero even if
        /// <paramref name="code"/> was 0 - and says so, naming the count and the first line, so the
        /// flip is never silent.</summary>
        public static void Finish(int code)
        {
            if (_foldArmed && _observedErrors > 0)
            {
                int folded = Mathf.Max(code, 1);
                if (folded != code)
                {
                    Debug.LogError($"CHECKEXIT: the run reported code {code} but logged {_observedErrors} error(s) during it - " +
                                   $"exiting {folded}. First: \"{Truncate(_firstError, 200)}\". A red line nothing counted is still a failure (ruling 1, 2026-08-25).");
                }
                else
                {
                    Debug.Log($"CHECKEXIT: {_observedErrors} error(s) logged during the run; the reported code {code} already reflects failure.");
                }

                code = folded;
            }

            if (_collecting)
            {
                _worst = Mathf.Max(_worst, code);
                return;
            }

            EditorApplication.Exit(code);
        }

        private static string Truncate(string s, int max)
            => string.IsNullOrEmpty(s) || s.Length <= max ? s : s.Substring(0, max) + "…";

        /// <summary>Runs one check without letting it end the process, returning the code it wanted.</summary>
        public static int Collect(Action check)
        {
            _collecting = true;
            _worst = 0;
            // Reset the fold counter so a check that DOESN'T arm cannot inherit an error count from
            // a prior armed check in the same session (CheckSuite runs checks in sequence via
            // Collect). A check that arms resets it again itself; this covers the ones that don't.
            _observedErrors = 0;
            _firstError = null;
            try
            {
                check();
                return _worst;
            }
            finally
            {
                _collecting = false;
            }
        }
    }

    /// <summary>
    /// Runs the project's asset and settings checks together, from a menu item and once per Editor
    /// session.
    ///
    /// <para><b>WHAT THIS ENUMERATES</b> (rule 14): the checks named in <see cref="Suite"/> (EIGHTEEN since the SEVENTH sweep - a written claim about the code, checked against the code - and S-32's player-reachability check, both 2026-09-01; sixteen since the SIXTH sweep - evidence that would pass regardless - the same day; fifteen since its fifth; fourteen since's four sweeps joined 2026-08-31 — comment claims, dead state, artifact identity and constant provenance; ten since `PhantomGuardCheck`; nine since `MetaTextCheck` joined 2026-08-29; eight since
    /// `AreaIconCoverageCheck` joined 2026-08-28), each
    /// with its own enumeration — see their doc comments. It does NOT run the simulation diagnostics
    /// (`AggregationEquivalenceCheck`, `CreditRatingAnchorCheck`, `PublicationCadenceCheck`), which need a
    /// seeded world rather than a project scan, and it does NOT run
    /// <see cref="ScreenEdgeCheck"/> — see below.</para>
    ///
    /// ⚠ **`ScreenEdgeCheck` IS DELIBERATELY EXCLUDED FROM THE AUTOMATIC RUN.** It reads whatever PNGs
    /// happen to be on disk, so on Editor open it would report on a capture set of unknown age — and a
    /// green result from stale captures is worse than no result, because it answers a question about a
    /// build nobody is looking at. It belongs immediately after a capture pass, and has its own menu item
    /// for that.
    ///
    /// <para><b>Once per session, not once per domain reload.</b> A script edit reloads the domain many
    /// times an hour; re-scanning 149 textures each time would make this the thing someone disables. The
    /// <c>SessionState</c> flag survives reloads and clears when the Editor closes, which is the cadence
    /// "on Editor open" actually means.</para>
    /// </summary>
    public static class CheckSuite
    {
        private const string RanThisSessionKey = "PoliSim.CheckSuite.Ran";

        private static readonly (string Name, Action Run)[] Suite =
        {
            ("DeliveredAssetCheck", DeliveredAssetCheck.Run),
            ("ImporterSettingsCheck", ImporterSettingsCheck.Run),
            ("StatIconCoverageCheck", StatIconCoverageCheck.Run),
            ("PartyMarkCoverageCheck", PartyMarkCoverageCheck.Run),
            ("PortraitCoverageCheck", PortraitCoverageCheck.Run),
            ("AreaIconCoverageCheck", AreaIconCoverageCheck.Run),
            ("ChromeV2CoverageCheck", ChromeV2CoverageCheck.Run),
            ("UpstreamCheck", UpstreamCheck.Run),
            ("MetaTextCheck", MetaTextCheck.Run),   // P-A1 (2026-08-29): no developer-facing text on a player surface - the ninth
            // C-E3 (2026-08-31, S-11): a doc comment naming a guard must name one that EXISTS. Cheap - it
            // reads text and reflects over loaded types, builds no World - so it belongs in the nine.
            ("PhantomGuardCheck", PhantomGuardCheck.Run),
            // The coherence audit (2026-08-31), four sweeps that each end in a check rather than a list.
            // All four read text or artifacts and build no World, so they belong in the cheap nine.
            ("CommentClaimCheck", CommentClaimCheck.Run),                 // (a) a comment naming code names code that exists
            ("DeadStateCheck", DeadStateCheck.Run),                       // (b) state and code nothing reaches - a ratchet at 39
            ("ArtifactIdentityCheck", ArtifactIdentityCheck.Run),         // (c) an artifact contains what its name claims
            ("ConstantProvenanceCheck", ConstantProvenanceCheck.Run),     // (d) a simulation constant says where it came from - a ratchet at 212

            // The coherence audit's FIFTH sweep (2026-09-01): a subsystem the game does not call.
            // ⚠ It exists because the other four were green while `TacticalVoting` sat built,
            // harness-proven and wired to nothing - `DeadStateCheck` scans PRIVATE declarations and
            // could not see it. Judged at the FILE, not the method: cut per method the first run
            // reported 58 findings, nearly all public helpers inside wired subsystems.
            ("UnwiredSubsystemCheck", UnwiredSubsystemCheck.Run),

            // The coherence audit's SIXTH sweep (2026-09-01): evidence that would pass regardless — the
            // class with FIVE recorded instances, which makes it this project's dominant failure mode
            // rather than a coincidence. ⚠ Its first run found `PublicationCadenceCheck`, registered in
            // the simulation group below, whose only exit was `Finish(0)`: it reported clean BY
            // CONSTRUCTION and every run of the bar had counted it. Cheap — it reads text and builds no
            // World — so it belongs with the others here.
            ("EvidenceDiscriminationCheck", EvidenceDiscriminationCheck.Run),

            // S-32 (2026-09-01): a delivered screen the player cannot reach is a FAILURE, not a
            // curiosity. ⚠ `ElectionNightScreen` — board 1h — was built, filmed at four widths and
            // recorded as delivered while the only thing naming it was the capture driver. Every guard
            // stayed green throughout: they check what was DRAWN, never whether a player could have got
            // there.
            ("PlayerReachabilityCheck", PlayerReachabilityCheck.Run),

            // The coherence audit's SEVENTH sweep (2026-09-01): a written claim about the code, checked
            // against the code. ⚠ `PhantomGuardCheck` and `CommentClaimCheck` scan CODE COMMENTS; nothing
            // checked a markdown claim, and this project's documents make far more claims about the code
            // than its comments do. Binds on the LIVE documents only - the historical records are correct
            // to name members that have since been deleted.
            ("DocumentClaimCheck", DocumentClaimCheck.Run),
        };

        [InitializeOnLoadMethod]
        private static void ScheduleOnEditorOpen()
        {
            if (Application.isBatchMode || SessionState.GetBool(RanThisSessionKey, false))
            {
                return;
            }

            SessionState.SetBool(RanThisSessionKey, true);

            // delayCall, so the first import/compile settles before 149 textures are asked for.
            EditorApplication.delayCall += () => RunAll(announceClean: false);
        }

        [MenuItem("PoliSim/Run Asset Checks")]
        private static void RunFromMenu()
        {
            SessionState.SetBool(RanThisSessionKey, true);
            RunAll(announceClean: true);
        }

        [MenuItem("PoliSim/Run Screen Edge Check (needs a fresh capture)")]
        private static void RunEdgeFromMenu()
        {
            int code = CheckExit.Collect(ScreenEdgeCheck.Run);
            Debug.Log(code == 0
                ? "CHECKS: ScreenEdgeCheck clean — but only for the captures currently on disk."
                : $"CHECKS: ScreenEdgeCheck FAILED ({code}).");
        }

        /// <summary>
        /// The three simulation checks, on demand.
        ///
        /// ⚠ **THEIR EXCLUSION WAS RECORDED AS "they need a seeded world rather than a project scan",
        /// AND THAT REASON WAS WRONG.** All three run headless: `WorldFactory.CreateDefault()` is a plain
        /// static call building a `World` in memory — no Play mode, no scene, no `GameObject`.
        /// `CreditRatingAnchorCheck` does not build one at all. There was never a capability barrier; the
        /// real one was the same `EditorApplication.Exit` that kept every other check off a menu, and it
        /// is fixed for these three now too.
        ///
        /// <para><b>What genuinely separates them is COST, not capability.</b>
        /// `AggregationEquivalenceCheck` alone constructs four Worlds and advances them — a real per-open
        /// expense where scanning 149 textures is not. So they get their own menu item and stay out of the
        /// once-per-session run: a stated trade rather than an inherited assumption. ⚠ **The cost has not
        /// been measured**; if it turns out small they belong in the automatic suite, and this note is the
        /// reason to go and check.</para>
        /// </summary>
        [MenuItem("PoliSim/Run Simulation Checks (slower — builds Worlds)")]
        private static void RunSimulationChecksFromMenu()
        {
            RunSimulation();
        }

        /// <summary>
        /// C-N3 (2026-08-31): **a batchmode entry for the simulation group, so it can actually be part of
        /// the bar.**
        ///
        /// <para>⚠ The group had a menu item and nothing else, which means a check in it could not be run
        /// by a session or by CI — it was armed for a human who remembered to click it. That is the same
        /// failure mode <see cref="RunAllBatch"/> fixed for the nine, and adding `LeverLivenessCheck` to a
        /// menu-only group would have been arming a guard that never fires. The two entries stay
        /// separate: `RunAllBatch` is the cheap once-per-session suite, this one is paid for on
        /// purpose.</para>
        /// </summary>
        public static void RunSimulationBatch()
        {
            int worst = RunSimulation();
            Debug.Log($"CHECKS: simulation group exiting {worst}.");
            EditorApplication.Exit(worst);
        }

        /// <summary>The simulation group, run once, returning the WORST code any of them wanted. A check
        /// that throws counts as 1 — an exception is not a pass. The enumeration is printed BEFORE the run
        /// (the enumeration rule): a group that silently ran three of four would read like a clean run.</summary>
        private static int RunSimulation()
        {
            var simulation = new (string Name, Action Run)[]
            {
                ("AggregationEquivalenceCheck", AggregationEquivalenceCheck.Run),
                ("CreditRatingAnchorCheck", CreditRatingAnchorCheck.Run),
                ("PublicationCadenceCheck", PublicationCadenceCheck.Run),
                // C-N3 (2026-08-31): every player-facing lever must measurably move the model, or be named
                // as retired. It belongs HERE rather than in the nine for this group's own stated reason -
                // COST: it builds and advances a World per PolicyDecision field, and there are 37.
                ("LeverLivenessCheck", LeverLivenessCheck.Run),

                // P-I2 (2026-08-31): the cohort substrate. Both belong to this group and not to the nine
                // for the same cost reason - each builds a World, and the aging step then runs 25 years of
                // it per country. CohortSubstrateDiagnostic asserts the 21 bands reconcile against their
                // own publishers' separately-transcribed totals; CohortAgingStepDiagnostic HINDCASTS the
                // step against a published year it was not fitted to, which is the assertion that caught a
                // ~50% double-count in the open 100+ band on its first run.
                ("CohortSubstrateDiagnostic", CohortSubstrateDiagnostic.Run),
                ("CohortAgingStepDiagnostic", CohortAgingStepDiagnostic.Run),

                // D-5 (a) (2026-08-31): the office test. It builds a World and forms a government for
                // every party of every country as the player's, so it belongs to this group on the same
                // cost argument as the rest. ⚠ Its Sweden 2022 assertion is the one in the suite whose
                // answer is PUBLIC RECORD rather than the model's own opinion.
                ("OfficeTestDiagnostic", OfficeTestDiagnostic.Run),

                // C-D1 (2026-08-31): the voter groups as a view over the cohort substrate. ⚠ Its Sweden
                // clause weights the 2024 pyramid by SCB's 2014 band rates and checks the result against
                // SCB's separately-published all-ages figure - two independently sourced things agreeing,
                // which is the strongest form of check this suite has.
                ("VoterGroupViewDiagnostic", VoterGroupViewDiagnostic.Run),
            };

            var names = new string[simulation.Length];
            for (int i = 0; i < simulation.Length; i++) { names[i] = simulation[i].Name; }
            Debug.Log($"CHECKS: running the {simulation.Length} simulation checks — {string.Join(", ", names)}.");

            var failed = new List<string>();
            int worstCode = 0;
            foreach ((string name, Action run) in simulation)
            {
                try
                {
                    int code = CheckExit.Collect(run);
                    if (code != 0) { failed.Add(name); }
                    if (code > worstCode) { worstCode = code; }
                }
                catch (Exception e)
                {
                    Debug.LogError($"CHECKS: {name} THREW {e.GetType().Name}: {e.Message}");
                    failed.Add(name);
                    worstCode = Mathf.Max(worstCode, 1);
                }
            }

            Debug.Log(failed.Count == 0
                ? $"CHECKS: {simulation.Length} of {simulation.Length} simulation checks clean."
                : $"CHECKS: {failed.Count} of {simulation.Length} FAILED — {string.Join(", ", failed)}.");

            return worstCode;
        }

        /// <summary>
        /// The whole suite under ONE `-executeMethod`, for batch and CI:
        /// <code>
        /// Unity.exe -batchmode -nographics -projectPath &lt;path&gt; \
        ///   -executeMethod PoliSim.EditorTools.CheckSuite.RunAllBatch -logFile &lt;path&gt;
        /// </code>
        ///
        /// ⚠ **Why this did not exist until 2026-08-31 (C-0.4), and what it cost.** Every check ends in
        /// <see cref="CheckExit.Finish"/>, which calls <c>EditorApplication.Exit</c> outside a
        /// <see cref="CheckExit.Collect"/> — so the suite could only ever be driven from the Editor, and
        /// <see cref="ScheduleOnEditorOpen"/> early-returns under <c>Application.isBatchMode</c>.
        /// Batch runs therefore invoked the nine checks as **nine separate Unity launches**, each paying
        /// the ~40 s domain warm-up, and **two spurious exit-1s in this project's history were traced to
        /// invoking the wrong entry point** — a failure mode that exists only because the caller has to
        /// remember nine names. Remembering is what this repo has already recorded twice as a failing
        /// mechanism.
        ///
        /// <para><b>The enumeration is printed before the run, not just after</b> (the enumeration rule):
        /// a suite that silently ran eight of nine would otherwise read exactly like a clean run of nine.
        /// The exit code is the WORST any check wanted, so a green summary line cannot outrank a red
        /// check.</para>
        ///
        /// <para>⚠ It deliberately carries no <c>[MenuItem]</c>: it ends in
        /// <c>EditorApplication.Exit</c>, so the first person to click it would quit the Editor. The
        /// menu equivalent is <b>PoliSim/Run Asset Checks</b>, which runs the identical suite and
        /// returns. The three simulation checks are NOT in it, for the cost reason recorded on
        /// <see cref="RunSimulationChecksFromMenu"/>.</para>
        /// </summary>
        public static void RunAllBatch()
        {
            var names = new string[Suite.Length];
            for (int i = 0; i < Suite.Length; i++) { names[i] = Suite[i].Name; }
            Debug.Log($"CHECKS: running all {Suite.Length} in one pass — {string.Join(", ", names)}.");

            int worst = RunAll(announceClean: true);
            Debug.Log($"CHECKS: suite exiting {worst}.");
            EditorApplication.Exit(worst);
        }

        /// <summary>Runs the nine and returns the WORST code any of them wanted (0 = all clean). A check
        /// that throws counts as 1 — an exception is not a pass.</summary>
        private static int RunAll(bool announceClean)
        {
            var failed = new List<string>();
            int worst = 0;

            foreach ((string name, Action run) in Suite)
            {
                int code;
                try
                {
                    code = CheckExit.Collect(run);
                }
                catch (Exception e)
                {
                    // A check that throws has not passed. Saying so is the whole point - a silent
                    // exception here would read exactly like a clean run.
                    Debug.LogError($"CHECKS: {name} THREW {e.GetType().Name}: {e.Message}");
                    failed.Add(name);
                    worst = Mathf.Max(worst, 1);
                    continue;
                }

                if (code != 0)
                {
                    failed.Add(name);
                }

                worst = Mathf.Max(worst, code);
            }

            if (failed.Count > 0)
            {
                Debug.LogError($"CHECKS: {failed.Count} of {Suite.Length} FAILED — {string.Join(", ", failed)}. " +
                               "Scroll up for the per-check detail.");
            }
            else if (announceClean)
            {
                Debug.Log($"CHECKS: {Suite.Length} of {Suite.Length} clean.");
            }

            return worst;
        }
    }
}
