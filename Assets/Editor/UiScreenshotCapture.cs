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
        /// 6000.5.6f1: a request of 950 captured 929.** [AUTHORED-DRAFT] ⚠ **And FOUR independent corroborations
        /// arrived from S-17's record** — its filmed view heights 699/929/1059/1419 sit exactly 21 below
        /// requests of 720/950/1080/1440, written down weeks earlier by somebody not looking for it. A
        /// fifth was taken 2026-09-01: a 1280x720 run filmed at 1280x699. See <see cref="StandardGeometries"/>.
        ///
        /// <para>⚠ <b>Its defence clause (R-T1).</b> This is a number about Unity's chrome, not about this
        /// project, so it can go stale without anything here changing. **The guard it feeds fails on the
        /// FIRST capture of a run, not silently across all of them**, and its message says outright that a
        /// mismatch means either the argument was ignored OR this constant has gone stale — and that the
        /// answer is to re-measure and change the number, never to widen the guard. A run cannot proceed on
        /// a stale value: it stops at capture one.</para></summary>
        private const int GameViewChromeHeight = 21;

        /// <summary>**S-17's four geometries**, read off the project's own filmed record at the Track C
        /// close rather than assumed. ⚠ **An off-standard height is a DIFFERENT TEST whose verdict is not
        /// comparable**: a 1280 film at `-shotheight=800` reports **13 text overflows** on a tree that
        /// films **0** at the standard 720, reproduced on clean `HEAD` — so it is the aspect ratio and not
        /// any change to the code.
        ///
        /// <para>⚠ <b>And these four independently confirm <see cref="GameViewChromeHeight"/>.</b> The
        /// record's filmed view heights are 699 / 929 / 1059 / 1419 against requests of 720 / 950 / 1080 /
        /// 1440 — **exactly 21 below, four times over**, written down weeks before the constant was
        /// measured for M-S2 and by somebody not looking for it. That is four corroborations of a number
        /// this file otherwise states on one measurement.</para></summary>
        private static readonly (int Width, int Height)[] StandardGeometries =
        {
            (1280, 720), (1600, 950), (1920, 1080), (2560, 1440),
        };

        /// <summary>Opt out of the geometry guard, loudly. ⚠ It exists because refusing outright would
        /// make a legitimate experiment impossible, and a guard that blocks legitimate work gets deleted.
        /// **It does not silence anything** — the run announces itself as off-standard and not comparable,
        /// which is the whole content of the finding.</summary>
        private static bool OffStandardAllowed =>
            Environment.GetCommandLineArgs().Contains("-shotoffstandard");

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

            // ⚠ S-17, ARMED 2026-09-01. THE GEOMETRY MUST BE ONE THE RECORD CAN BE COMPARED WITH.
            // A 1280 film at -shotheight=800 reports 13 text overflows on a tree that films 0 at the
            // standard 720 - reproduced on clean HEAD, so it is the aspect ratio and nothing else. An
            // off-standard film is not a worse film, it is a DIFFERENT TEST, and its verdict read beside
            // the record's is a comparison of two things that were never the same measurement.
            //
            // Trap 2 (and its height half) guard "the size asked for is the size captured". This guards
            // the question before it: "is the size asked for a size anything can be compared against".
            int reqW = Mathf.RoundToInt(ViewWidth);
            int reqH = Mathf.RoundToInt(ViewHeight);
            bool standard = false;
            var known = new System.Text.StringBuilder();
            foreach ((int w, int h) in StandardGeometries)
            {
                known.Append(w).Append('x').Append(h).Append(' ');
                if (reqW == w && reqH == h) { standard = true; }
            }

            if (!standard && !OffStandardAllowed)
            {
                Debug.LogError($"SHOT: REFUSING a non-standard geometry {reqW}x{reqH}. S-17's four are: {known}"
                               + "- read off this project's own filmed record, not chosen. ⚠ An off-standard height is "
                               + "a DIFFERENT TEST whose verdict is NOT comparable with the record: a 1280 film at "
                               + "height 800 reports 13 text overflows where the standard 720 reports 0, on identical "
                               + "code. If that is what you want, pass -shotoffstandard and the run will say so in "
                               + "every line it writes - but do not read the result beside a standard film.");
                EditorApplication.Exit(2);
                return;
            }

            if (!standard)
            {
                Debug.LogWarning($"SHOT: OFF-STANDARD GEOMETRY {reqW}x{reqH}, allowed by -shotoffstandard. ⚠ This film "
                                 + "is a DIFFERENT TEST from the record's four and its verdict is not comparable with "
                                 + "them. Nothing here is wrong; it is simply not the same measurement.");
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
                SelectFreeAspect(view, gameViewType);
                view.Repaint();

                // P2-0.2 (2026-09-02): a DOCKED Game View ignores the position assignment - its size is the
                // layout's - and the run then films at the dock's size while claiming the requested one
                // (every frame reports a HEIGHT MISMATCH, which is the guard working; this is the cure).
                // When the assignment did not take, the docked view is closed and a fresh one is shown
                // floating, which honours its position. Said in the log either way.
                // 2026-09-03: the docked view ECHOES the assignment (it printed "resized to 1280x720") while its real size stayed
                // the layout's - a sitting in the Editor had left the layout with the Game View filling the window, and four films
                // read 962 px tall with the check fooled. So the docked view is ALWAYS closed and a floating one shown at the
                // requested size; the comparison below is kept only as the log's witness of what the docked view claimed.
                const bool alwaysFloat = false;   // the floating path stalled the driver (no capture in four minutes, 2026-09-03); the cure for a maximized layout is to reset the layout file, not to float
                if (alwaysFloat || Mathf.Abs(view.position.width - ViewWidth) > 2f || Mathf.Abs(view.position.height - ViewHeight) > 2f)
                {
                    Rect docked = view.position;
                    view.Close();
                    EditorWindow floating = ScriptableObject.CreateInstance(gameViewType) as EditorWindow;
                    floating.Show();
                    floating.position = new Rect(0f, 0f, ViewWidth, ViewHeight);
                    SelectFreeAspect(floating, gameViewType);
                    floating.Repaint();
                    Debug.Log($"SHOT: the docked Game View ignored the resize ({docked.width}x{docked.height}); re-created it floating at {floating.position.width}x{floating.position.height}.");
                }
                else
                {
                    Debug.Log($"SHOT: Game View resized to {view.position.width}x{view.position.height}.");
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"SHOT: could not resize Game View ({e.GetType().Name}) - capturing at default size.");
            }
        }

        /// <summary>
        /// 2026-09-03: the Game View's SIZE PRESET is Editor state the harness never set - a sitting in the Editor that left
        /// a fixed preset selected (the Library changed at 11:56 that day) made every frame of the next film 962 px tall
        /// while the window read 1280x720 and the guard failed all 85. The harness now selects the first preset - Free
        /// Aspect, the one whose render size IS the window's - by reflection, and says what it read back.
        /// </summary>
        private static void SelectFreeAspect(EditorWindow view, Type gameViewType)
        {
            try
            {
                System.Reflection.PropertyInfo index = gameViewType.GetProperty("selectedSizeIndex",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
                if (index == null) { Debug.LogWarning("SHOT: GameView.selectedSizeIndex not found - the size preset is whatever the Editor kept."); return; }
                object before = index.GetValue(view, null);
                index.SetValue(view, 0, null);
                Debug.Log($"SHOT: Game View size preset {before} -> {index.GetValue(view, null)} (0 = Free Aspect, the window's own size).");

                // The same sitting can leave PLAY MAXIMIZED on (Unity 6: enterPlayModeBehavior; older Editors: maximizeOnPlay) -
                // then the view fills the Editor window on play (1920 x 962 on this machine) whatever its docked size says, and
                // every frame reads 962 px tall. Play Focused is the behaviour whose render size is the window's.
                System.Reflection.PropertyInfo behaviour = gameViewType.GetProperty("enterPlayModeBehavior",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
                if (behaviour != null)
                {
                    object was = behaviour.GetValue(view, null);
                    behaviour.SetValue(view, Enum.ToObject(behaviour.PropertyType, 0), null);
                    Debug.Log($"SHOT: Game View enter-play behaviour {was} -> {behaviour.GetValue(view, null)} (0 = Play Focused, not maximized).");
                }
                System.Reflection.PropertyInfo maximize = gameViewType.GetProperty("maximizeOnPlay",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
                if (maximize != null && maximize.PropertyType == typeof(bool))
                {
                    object was = maximize.GetValue(view, null);
                    maximize.SetValue(view, false, null);
                    Debug.Log($"SHOT: Game View maximizeOnPlay {was} -> false.");
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"SHOT: could not select the Game View's size preset ({e.GetType().Name}).");
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
            driver.Interrupts = Environment.GetCommandLineArgs().Contains("-shotinterrupts");
            driver.CueSweep = Environment.GetCommandLineArgs().Contains("-cuesweep");
            // W-E6 (2026-08-30): film board 1h, election night, instead of the sweep. Stages the
            // SOURCED Swedish 2022 returns like -shotcampaign, and demands the same country.
            driver.ElectionNightBoard = Environment.GetCommandLineArgs().Contains("-shotelectionnight");
            // 2026-09-04 (the 1920 wedge): end the sweep at a named capture, so a bisect at one width films
            // the frames in question rather than the set. The film says it stopped, and where.
            driver.StopAfter = Arg("-shotstop=", "");
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

            // M-S16 (2026-09-01): the label-clipping guard, armed where its input is created rather than
            // where somebody remembers it. ⚠ It reads the films THIS run just wrote, by this run's own
            // label, so the pattern cannot drift from the pass it is meant to judge.
            string edgePattern = label + "_*.png";
            driver.BeforeExit = code =>
            {
                Debug.Log("SHOT: running the edge guard over '" + edgePattern + "' before exit (M-S16).");
                return CheckExit.Collect(() => ScreenEdgeCheck.RunOver(edgePattern));
            };
            Debug.Log($"SHOT: driver attached, label={label}, country={driver.Country}, states={driver.PinStates}, saves={driver.StageSaves}, ladder={driver.Ladder}, campaign={driver.CampaignHq}, locale={(driver.Locale.Length == 0 ? "OS" : driver.Locale)}, {Screen.width}x{Screen.height}");
        }

        private static string Arg(string prefix, string fallback)
        {
            string match = Environment.GetCommandLineArgs().FirstOrDefault(a => a.StartsWith(prefix));
            return match == null ? fallback : match.Substring(prefix.Length);
        }
    }
}
