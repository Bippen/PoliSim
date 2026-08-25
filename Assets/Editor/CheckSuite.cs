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
    /// <para><b>WHAT THIS ENUMERATES</b> (rule 14): the four checks named in <see cref="Suite"/>, each
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
            ("ChromeV2CoverageCheck", ChromeV2CoverageCheck.Run),
            ("UpstreamCheck", UpstreamCheck.Run),
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
            var simulation = new (string Name, Action Run)[]
            {
                ("AggregationEquivalenceCheck", AggregationEquivalenceCheck.Run),
                ("CreditRatingAnchorCheck", CreditRatingAnchorCheck.Run),
                ("PublicationCadenceCheck", PublicationCadenceCheck.Run),
            };

            var failed = new List<string>();
            foreach ((string name, Action run) in simulation)
            {
                try
                {
                    if (CheckExit.Collect(run) != 0) { failed.Add(name); }
                }
                catch (Exception e)
                {
                    Debug.LogError($"CHECKS: {name} THREW {e.GetType().Name}: {e.Message}");
                    failed.Add(name);
                }
            }

            Debug.Log(failed.Count == 0
                ? $"CHECKS: {simulation.Length} of {simulation.Length} simulation checks clean."
                : $"CHECKS: {failed.Count} of {simulation.Length} FAILED — {string.Join(", ", failed)}.");
        }

        private static void RunAll(bool announceClean)
        {
            var failed = new List<string>();

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
                    continue;
                }

                if (code != 0)
                {
                    failed.Add(name);
                }
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
        }
    }
}
