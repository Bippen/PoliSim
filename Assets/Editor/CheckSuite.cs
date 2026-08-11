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

        /// <summary>Call instead of <c>EditorApplication.Exit</c>. Identical behaviour in batch mode.</summary>
        public static void Finish(int code)
        {
            if (_collecting)
            {
                _worst = Mathf.Max(_worst, code);
                return;
            }

            EditorApplication.Exit(code);
        }

        /// <summary>Runs one check without letting it end the process, returning the code it wanted.</summary>
        public static int Collect(Action check)
        {
            _collecting = true;
            _worst = 0;
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
