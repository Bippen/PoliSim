using System;
using System.Linq;
using PoliSim.Testing;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// The Editor half of <see cref="UiScreenshotDriver"/> - opens the scene, sizes the Game View,
    /// enters Play mode and attaches the driver.
    ///
    /// <code>
    /// Unity.exe -projectPath &lt;path&gt; -executeMethod PoliSim.EditorTools.UiScreenshotCapture.Run
    ///           -logFile &lt;path&gt; -skipsimulationtestrunner -shotlabel=&lt;label&gt; [-shotdir=&lt;dir&gt;]
    /// </code>
    ///
    /// ⚠ **NO `-batchmode` AND NO `-nographics`, both deliberately.** There is no frame to capture
    /// without a graphics device, and `WaitForEndOfFrame` never resumes under `-batchmode` - the
    /// coroutine stops, the capture never happens, and Unity hangs with no error line. This is the one
    /// entry point in the project that must run with a real Editor window; every other batch tool here
    /// takes `-batchmode -nographics`, so the omission looks like a mistake and is not.
    ///
    /// ⚠ **`-skipsimulationtestrunner` matters.** SampleScene carries BOTH GameController and
    /// SimulationTestRunner, and without that flag entering Play mode runs 100 turns of simulation
    /// synchronously before the first frame ever renders.
    ///
    /// State lives in `SessionState` rather than static fields for the reason
    /// <see cref="BatchSimulationRunner"/> documents at length: entering Play mode triggers a domain
    /// reload, which wipes statics and unsubscribes delegates. That was the cause of this project's
    /// long-running "batch run hangs forever" bug, and the trap applies verbatim here.
    /// </summary>
    public static class UiScreenshotCapture
    {
        private const string ActiveKey = "PoliSim.UiScreenshotCapture.Active";

        /// <summary>
        /// Game View size for captures. Deliberately a real play size rather than a default.
        ///
        /// Every style in this UI derives its font size from `Screen.height` (see
        /// `GameController.RescaleStylesToScreen`), so a small Game View does not merely produce a small
        /// screenshot - it produces a screenshot of DIFFERENT FONT SIZES. For a capture whose purpose is
        /// judging layout and type, that answers the wrong question. It is also why
        /// `LedgerRow.Height` derives from the font metric: at this size two wrapped lines fit, and at
        /// 1440p they would not have under a fixed row height.
        /// </summary>
        /// ⚠ **OVERRIDABLE SINCE 2026-08-11 — `-shotwidth=` / `-shotheight=`, ruled by Elias.** Everything
        /// this project has ever measured about layout came from ONE window size, and that turned out to
        /// scope three separate conclusions without any of them saying so: the `LedgerRow` squeeze floor
        /// ("never engages" — at 1600×929), instance #12's closure ("0 of 55 clipped" — at 1600×929), and
        /// `ScreenEdgeCheck` itself, which can only ever answer for the captures it is given. A second
        /// resolution is a capture-config change rather than a code change, and it converts those three
        /// from resolution-scoped claims into real ones.
        private static float ViewWidth => ArgFloat("-shotwidth=", 1600f);
        private static float ViewHeight => ArgFloat("-shotheight=", 950f);

        /// <summary>The Game View window's toolbar, in pixels: the difference between the WINDOW height
        /// `view.position` takes and the FRAME height a capture gets back. **Measured 2026-09-01 at Unity
        /// 6000.5.6f1: a request of 950 captured 929.** [AUTHORED-DRAFT]
        ///
        /// <para>⚠ <b>Its defence clause (R-T1).</b> This is a number about Unity's chrome, not about this
        /// project, so it can go stale without anything here changing. **The guard it feeds fails on the
        /// FIRST capture of a run, not silently across all of them**, and its message says outright that a
        /// mismatch means either the argument was ignored OR this constant has gone stale — and that the
        /// answer is to re-measure and change the number, never to widen the guard. A run cannot proceed on
        /// a stale value: it stops at capture one.</para></summary>
        private const int GameViewChromeHeight = 21;

        private static float ArgFloat(string prefix, float fallback)
        {
            string raw = Arg(prefix, null);
            return raw != null && float.TryParse(raw, System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out float value) && value > 0f
                ? value
                : fallback;
        }

        [InitializeOnLoadMethod]
        private static void ReattachAfterDomainReload()
        {
            if (!SessionState.GetBool(ActiveKey, false))
            {
                return;
            }

            SessionState.SetBool(ActiveKey, false);
            EditorApplication.delayCall += AttachDriver;
        }

        public static void Run()
        {
            // ⚠ TRAP 1, ARMED 2026-08-31 (the clearance list's process correction). This file's own doc
            // has said "NO -batchmode AND NO -nographics, both deliberately" since it was written, and
            // that documentation cost a full run this week: a capture pass launched with both flags did
            // not fail - it HUNG, logging "canvas seam never settled" until a ten-minute timeout killed
            // it, because WaitForEndOfFrame never resumes under -batchmode and there is no frame to
            // capture without a graphics device.
            //
            // A comment cannot stop that; this can. The refusal names the flag and the reason, so the
            // next person reaching for the idiom every OTHER entry point in this project uses gets a
            // sentence instead of a hang.
            if (Application.isBatchMode)
            {
                Debug.LogError("SHOT: REFUSING TO RUN under -batchmode. There is no frame to capture without a graphics "
                               + "device, and WaitForEndOfFrame never resumes under -batchmode, so this would HANG rather "
                               + "than fail. Every OTHER -executeMethod in this project takes -batchmode -nographics; this "
                               + "one is the exception. Re-run as: Unity.exe -projectPath <path> -executeMethod "
                               + "PoliSim.EditorTools.UiScreenshotCapture.Run -shotlabel=<label> -shotwidth=<w> -shotheight=<h>");
                EditorApplication.Exit(2);
                return;
            }

            EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
            ResizeGameView();
            SessionState.SetBool(ActiveKey, true);
            EditorApplication.isPlaying = true;
        }

        /// <summary>Reflection against `UnityEditor.GameView`, which is internal - so this is best-effort by construction. On failure it logs the size it actually got rather than pretending, and the capture still runs.</summary>
        private static void ResizeGameView()
        {
            try
            {
                Type gameViewType = Type.GetType("UnityEditor.GameView,UnityEditor");
                if (gameViewType == null)
                {
                    Debug.LogWarning("SHOT: GameView type not found - capturing at whatever size the Editor gives.");
                    return;
                }

                EditorWindow view = EditorWindow.GetWindow(gameViewType, false, "Game", true);
                view.position = new Rect(0f, 0f, ViewWidth, ViewHeight);
                view.Repaint();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"SHOT: could not resize Game View ({e.GetType().Name}) - capturing at default size.");
            }
        }

        private static int _attachAttempts;

        private static void AttachDriver()
        {
            if (!EditorApplication.isPlaying)
            {
                // Play mode has not finished coming up - retry next tick rather than attaching to a
                // scene about to be reloaded out from under us. BOUNDED, because an unbounded retry is a
                // hang with no log line, which is the exact failure shape this project has already hit
                // twice (see BatchSimulationRunner's own comment).
                if (++_attachAttempts > 600)
                {
                    Debug.LogError("SHOT: play mode never started - giving up rather than hanging.");
                    EditorApplication.Exit(1);
                    return;
                }

                EditorApplication.delayCall += AttachDriver;
                return;
            }

            string label = Arg("-shotlabel=", "run");
            var go = new GameObject("UiScreenshotDriver");
            UiScreenshotDriver driver = go.AddComponent<UiScreenshotDriver>();
            driver.OutputDirectory = Arg("-shotdir=", UiScreenshotDriver.DefaultOutputDirectory);
            driver.Label = label;
            // Country coverage (2026-08-12, Elias's ruling: coverage before features). Same idiom as
            // -shotwidth=: a capture-config argument, defaulting to the only country ever captured
            // before it existed. The driver validates the name and fails LOUDLY on a bad one.
            driver.Country = Arg("-shotcountry=", "USA");
            // State pinning and locale override (2026-08-12): flags of the same capture-config family.
            driver.PinStates = Environment.GetCommandLineArgs().Contains("-shotstates");
            // R-D4 (the clear-out kickoff, 2026-08-28): stage the three playtest saves instead of the sweep.
            driver.StageSaves = Environment.GetCommandLineArgs().Contains("-shotsaves");
            // UI v3.0 Phase A, Phase 3 (2026-08-28): film the instrument ladder instead of the sweep.
            driver.Ladder = Environment.GetCommandLineArgs().Contains("-shotladder");
            // W-E1 (2026-08-29): film Campaign HQ instead of the sweep. Stages SOURCED Swedish
            // returns, so it demands -shotcountry=Sweden and fails loudly under any other.
            driver.CampaignHq = Environment.GetCommandLineArgs().Contains("-shotcampaign");
            // W-E6 (2026-08-30): film board 1h, election night, instead of the sweep. Stages the
            // SOURCED Swedish 2022 returns like -shotcampaign, and demands the same country.
            driver.ElectionNightBoard = Environment.GetCommandLineArgs().Contains("-shotelectionnight");
            // C-C10 (2026-08-31): film the impact ledger POPULATED instead of the sweep. The sweep's
            // warm-up is deliberately no-policy, so it films the ledger's empty state and only this
            // films the state where there is a divergence to attribute.
            driver.ImpactLedger = Environment.GetCommandLineArgs().Contains("-shotledger");
            driver.Locale = Arg("-shotlocale=", "");
            // Trap 2: hand the driver the width we asked for so every capture can assert it got it.
            driver.ExpectedWidth = Mathf.RoundToInt(ViewWidth);
            // R-T3 (2026-09-01): the other half of the same argument pair, unguarded for a month.
            // ⚠ AND IT IS NOT SYMMETRIC, WHICH IS WHY IT WAS NEVER NOTICED. `view.position` is a WINDOW
            // rect and the Game View window carries a toolbar, so a run asking for 950 has always captured
            // 929 - measured today, and this project's own records say "1600x929" while every capture
            // command in them says 950. Width has no such chrome and matches exactly, so the naive
            // assertion worked on one axis and would have been false on the other for a stable reason.
            driver.ExpectedHeight = Mathf.RoundToInt(ViewHeight) - GameViewChromeHeight;
            Debug.Log($"SHOT: driver attached, label={label}, country={driver.Country}, states={driver.PinStates}, saves={driver.StageSaves}, ladder={driver.Ladder}, campaign={driver.CampaignHq}, locale={(driver.Locale.Length == 0 ? "OS" : driver.Locale)}, {Screen.width}x{Screen.height}");
        }

        private static string Arg(string prefix, string fallback)
        {
            string match = Environment.GetCommandLineArgs().FirstOrDefault(a => a.StartsWith(prefix));
            return match == null ? fallback : match.Substring(prefix.Length);
        }
    }
}
