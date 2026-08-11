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
            driver.OutputDirectory = Arg("-shotdir=", "screenshots");
            driver.Label = label;
            Debug.Log($"SHOT: driver attached, label={label}, {Screen.width}x{Screen.height}");
        }

        private static string Arg(string prefix, string fallback)
        {
            string match = Environment.GetCommandLineArgs().FirstOrDefault(a => a.StartsWith(prefix));
            return match == null ? fallback : match.Substring(prefix.Length);
        }
    }
}
