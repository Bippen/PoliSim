using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using PoliSim.Data;
using PoliSim.Simulation;
using PoliSim.UI;
using UnityEngine;

namespace PoliSim.Testing
{
    /// <summary>
    /// Drives <see cref="GameController"/> to a set of known screens and captures each one.
    ///
    /// **Promoted from scratch tooling into the repo on 2026-08-10**, because the v2.0 restyle converts
    /// one row type at a time and every conversion needs to be SEEN rather than described. It lived in a
    /// throwaway project copy for the font test; keeping it there meant every visual round began by
    /// rebuilding it, and meant the half-converted states nobody can judge from a diff were never
    /// looked at.
    ///
    /// It reaches into GameController's private state by reflection rather than adding test hooks to
    /// production code. That trade is deliberate: the alternative is widening a 5,000-line class's
    /// public surface for a screenshot, and a reflection failure here is LOUD - logged and skipped, with
    /// the screen named - rather than silently capturing the wrong screen, which is the only outcome
    /// that would actually mislead.
    ///
    /// <para><b>Captures happen after `WaitForEndOfFrame`, which is the only point IMGUI has actually
    /// composited</b> - and is why the runner cannot use `-batchmode`. See <c>Capture</c>.</para>
    /// </summary>
    public class UiScreenshotDriver : MonoBehaviour
    {
        /// <summary>
        /// Where captures land when `-shotdir=` is not given: a SIBLING of the project, outside the
        /// repository tree. Until 2026-08-16 this was in-repo `screenshots/`, and five days of capture
        /// passes put 2,003 PNGs (~874 MiB of blobs) into git history — see CLAUDE.md "The repository
        /// weight finding". Relative to the project root because Unity's CWD is the project root for
        /// both the Editor run and `-executeMethod` batch runs. ⚠ ONE default, THREE readers — this
        /// constant, `UiScreenshotCapture`'s `-shotdir=` fallback, and `ScreenEdgeCheck`'s folder —
        /// so a future move edits one line, not three.
        /// </summary>
        public const string DefaultOutputDirectory = "../PoliSim-captures";

        public string OutputDirectory = DefaultOutputDirectory;
        public string Label = "run";
        /// <summary>Which country to play as, set from `-shotcountry=` (default USA — the only country any set contained before 2026-08-12). Parsed against CountryId in Start and FAILS the run on a bad name: a typo must not silently capture the default country under the requested country's label.</summary>
        public string Country = "USA";

        /// <summary>The parsed <see cref="Country"/>, held so the warm-up's publication check and the held-state pass read the country actually being captured rather than a hardcoded USA.</summary>
        private CountryId _countryId = CountryId.USA;

        /// <summary>Set by `-shotstates`: run the state-pinning pass (cabinet, budget pause, decision search, pending bills) after the main sweep. Off by default so ordinary chrome runs stay fast and their sets comparable with history.</summary>
        public bool PinStates;

        /// <summary>Set by `-shotlocale=` (e.g. "en-US"): overrides the thread culture before anything draws, so number/date formatting can be captured in a locale other than the OS's. Empty = OS culture, which is what every set before 2026-08-12 rendered in (sv-SE on this machine — the decimal-comma set).</summary>
        public string Locale = "";

        /// <summary>Frames to let IMGUI settle before a capture. IMGUI lays out on the frame it draws, so a screen switched to on frame N is not fully measured until N+1; four is cheap insurance rather than a measured minimum.</summary>
        private const int SettleFrames = 4;

        private static readonly string[] Tabs =
        {
            "Statistics", "Decisions", "Demographics", "Budget", "PolicyLaws", "Politics"
        };

        /// <summary>
        /// Tabs whose sub-selector changes which row type is on screen, and the private enum field that
        /// drives each one.
        ///
        /// **This is what a tab-level capture cannot show.** The v2.0 conversion runs per ROW TYPE, and
        /// row types live behind sub-selectors - so one screenshot per tab shows whichever sub-screen
        /// happened to be selected and silently omits the rest. During a conversion that is exactly the
        /// wrong picture, because the half-converted state (one sub-screen restyled, its neighbours not)
        /// is the most useful thing a capture can produce.
        ///
        /// **A table rather than a branch per tab.** It was a hardcoded Budget branch until Policy/Laws
        /// needed the same treatment; a second special case is the point at which the shape should stop
        /// being special. Politics (Parliament/Compass/Cabinet/FederalReserve) will drop in here as one
        /// more line when its turn comes.
        /// </summary>
        private static readonly Dictionary<string, KeyValuePair<string, string[]>> SubScreens =
            new Dictionary<string, KeyValuePair<string, string[]>>
            {
                { "Budget", new KeyValuePair<string, string[]>("_budgetProcessCategory",
                    new[] { "Tax", "Spending", "Welfare", "Infrastructure", "Swf" }) },
                { "PolicyLaws", new KeyValuePair<string, string[]>("_policyLawsCategory",
                    new[] { "LaborMarket", "CrimeJustice", "Sectors", "PolicyWeb", "Trade", "Laws" }) },
                { "Statistics", new KeyValuePair<string, string[]>("_statisticsCategory",
                    new[] { "Domestic", "International" }) },
                { "Politics", new KeyValuePair<string, string[]>("_politicsCategory",
                    new[] { "Parliament", "Compass", "Cabinet", "FederalReserve" }) }
            };

        /// <summary>
        /// Guards against a double exit: set the instant <see cref="Finish"/> actually runs, checked by
        /// Start's own <c>finally</c> so a normal completion (which already calls Finish itself) is
        /// never followed by a second, redundant one.
        /// </summary>
        private bool _finishCalled;

        private IEnumerator Start()
        {
            // COUNTRY-LEAK FIX: `controller` is declared here, outside the try, so the `finally` below
            // can reach it. Everything from here through the method's normal end is now wrapped in
            // try/finally - see the finally block's own comment for why.
            GameController controller = null;
            try
            {
            if (!string.IsNullOrEmpty(Locale))
            {
                var culture = new System.Globalization.CultureInfo(Locale);
                System.Globalization.CultureInfo.DefaultThreadCurrentCulture = culture;
                System.Threading.Thread.CurrentThread.CurrentCulture = culture;
                Debug.Log($"SHOT: thread culture overridden to {Locale}.");
            }

            Directory.CreateDirectory(OutputDirectory);

            controller = FindAnyObjectByType<GameController>();
            if (controller == null)
            {
                Debug.LogError("SHOT: no GameController in scene - nothing to capture.");
                Finish(1);
                yield break;
            }

            // The canvas text guard self-tests BOTH directions before its first assert, per the
            // standing guard discipline — a broken probe must not be able to report clean. A failed
            // self-test voids every later canvas-text count, so it fails the run outright.
            if (!CanvasTextGuard.SelfTest())
            {
                _canvasTextViolations++;
            }

            // CANVAS PILOT: the selector now ENTERS through the takeover envelope (~0.4s, ~25 frames
            // at 60fps) — far past the 4-frame settle, so the capture waits on the seam's own flag
            // (seam defect class 5: the harness racing the envelope). Falls through after the bound:
            // if the Canvas build failed, the IMGUI selector is up instead and the capture is still
            // the right capture.
            yield return WaitForCanvasSettle(controller, wantActive: true);
            yield return Settle();
            yield return Capture("01_country_selector");
            RecordCanvasTextAssert("01_country_selector", controller);

            try
            {
                _countryId = (CountryId)System.Enum.Parse(typeof(CountryId), Country, ignoreCase: true);
            }
            catch (System.ArgumentException)
            {
                Debug.LogError($"SHOT: '{Country}' is not a CountryId - failing rather than capturing the wrong country under this label.");
                Finish(1);
                yield break;
            }

            Invoke(controller, "SelectPlayerCountry", _countryId);

            // The YIELDING state: two frames into CoverOut, the scrim is mid-cover over the Canvas —
            // the seam's other half on film. Alpha varies with frame rate (time-based envelope);
            // presence is what the capture pins, not a pixel value.
            yield return null;
            yield return null;
            yield return Capture("01a_selector_yielding");

            yield return WaitForCanvasSettle(controller, wantActive: false);
            yield return Settle();

            // ⚠ THE ONE GUARANTEED RUNNING-STATE CAPTURE, taken before the warm-up. The 2026-08-12 run
            // showed every post-warm-up capture in the HELD state — the preliminary-release stop lands
            // on an election eve, so the fed-chair pause is live for the whole main set (and was in
            // every earlier set too). Turn 0 is the one moment guaranteed unpaused: no election eve,
            // no pending decisions, nothing rolled yet. Without this shot the status line's RUNNING
            // form exists in no capture this harness can produce.
            yield return Capture("01b_running_strip");

            AdvanceDays(controller, _countryId);
            DivergeSwfWeights(controller);
            DraftSpendingLines(controller);
            yield return Settle();

            for (int i = 0; i < Tabs.Length; i++)
            {
                if (!SetEnumField(controller, "_consolidatedTab", Tabs[i]))
                {
                    continue;
                }

                yield return Settle();
                yield return Capture($"{i + 2:00}_{Tabs[i].ToLowerInvariant()}");

                if (!SubScreens.TryGetValue(Tabs[i], out KeyValuePair<string, string[]> sub))
                {
                    continue;
                }

                for (int c = 0; c < sub.Value.Length; c++)
                {
                    if (!SetEnumField(controller, sub.Key, sub.Value[c]))
                    {
                        continue;
                    }

                    string stem = $"{i + 2:00}{(char)('a' + c)}_{Tabs[i].ToLowerInvariant()}_{sub.Value[c].ToLowerInvariant()}";

                    ResetScrolls(controller);
                    yield return Settle();
                    yield return Capture(stem);

                    // ⚠ A SECOND CAPTURE, SCROLLED PAST THE PREAMBLE. Every sub-screen opens with a
                    // header, explanatory prose and a summary block that together fill most of the panel,
                    // so a capture at scroll zero shows one or two rows and answers nothing about
                    // density - which on Spending's 29 categories was the entire question D3 was about.
                    // Scrolling far enough to put rows on screen is the only way to see whether a
                    // treatment that reads well at 13 rows survives at 29.
                    ScrollBy(controller, 900f);
                    yield return Settle();
                    yield return Capture(stem + "_rows");

                    // ⚠ A THIRD CAPTURE, DEEPER STILL. 900px clears the preamble and shows the first few
                    // rows, which answers "does a row render". It does not answer "does this hold at
                    // depth" - and on the two screens where that is the real question it lands inside the
                    // first group. Economic Sectors is eight sectors x five dials = FORTY rows, and the
                    // whole point of the group headers is what happens at a sector BOUNDARY, which is
                    // ~1400px down. Spending's 29-row discretionary tail is the same shape of question.
                    ScrollBy(controller, 2200f);
                    yield return Settle();
                    yield return Capture(stem + "_deep");
                }

                SetEnumField(controller, sub.Key, sub.Value[0]);
            }

            yield return CaptureHeldState(controller);
            yield return CaptureSavesMenu(controller);

            if (PinStates)
            {
                yield return CaptureStatePins(controller);
            }

            Debug.Log($"SHOT: done, {_captured} captured, {_failed} failed.");

            int overflows = ReportOverflows();
            Debug.Log($"SHOT: {overflows} text overflow(s) recorded.");

            int escapes = ReportContainmentEscapes();
            Debug.Log($"SHOT: {escapes} containment escape(s) recorded.");

            Debug.Log($"SHOT: {_canvasTextViolations} canvas text violation(s) recorded across {_canvasTextAsserts} assert(s).");

            Finish(_failed == 0 && overflows == 0 && escapes == 0 && _canvasTextViolations == 0 ? 0 : 1);
            }
            finally
            {
                // COUNTRY-LEAK FIX (see ResetPlayerCountrySelection's doc comment on GameController).
                // SelectPlayerCountry is a real player's one-time, permanent commitment and correctly
                // has no restore of its own - this driver instead reuses it as a disposable per-run
                // label (-shotcountry=). Every KNOWN exit path above already tears the whole Editor
                // process down via Finish()/EditorApplication.Exit() before or after this point, which
                // is what actually undoes the leak (nothing survives process exit to reach a later
                // session) - but Start() had no guarantee covering an UNCAUGHT exception after the
                // country switch. Because UiScreenshotCapture deliberately runs WITHOUT -batchmode (a
                // real, interactive Editor window - see its own doc comment), that gap could leave Play
                // Mode stuck on the requested country with no in-game way back, since
                // SelectPlayerCountry's own selector gate is permanently one-directional once set.
                //
                // A `finally` around the whole run closes it two ways, both unconditional regardless of
                // how Start() ends: the selection is put back exactly the way it started (mirroring
                // SelectPlayerCountry's own reflective call on entry), and the run's own exit -
                // whatever it decided, or a forced failure if it never got that far - is guaranteed to
                // fire exactly once. Wrapped in its own try/catch so a reflection failure during cleanup
                // can never mask - or replace - whatever exception got the driver here in the first
                // place.
                try
                {
                    if (controller != null)
                    {
                        Invoke(controller, "ResetPlayerCountrySelection");
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"SHOT: could not reset the player-country selection during cleanup ({e.GetType().Name}) - forcing exit rather than risking a stuck session.");
                }

                if (!_finishCalled)
                {
                    Debug.LogError("SHOT: run ended without reaching its own exit (an uncaught exception, most likely) - forcing one now.");
                    Finish(1);
                }
            }
        }

        /// <summary>
        /// SAVE/LOAD UI (item 8's menu pass): pins the saves screen with real rows on it. Two saves
        /// are written through the REAL service into the REAL saves directory (names prefixed
        /// `zz_driver_capture_` so they read as synthetic and sort last), the menu is opened through
        /// the controller's own OpenSavesMenu, one capture is taken, and the driver's saves are
        /// deleted again through the same service. A crash mid-coroutine can strand the two files;
        /// their names are the cleanup instruction. ⚠ This pins the SCREEN, not the round trip -
        /// layer 3's live checklist stays in the OPEN VERIFICATION GAP block regardless.
        /// </summary>
        private IEnumerator CaptureSavesMenu(GameController controller)
        {
            FieldInfo simField = controller.GetType().GetField("_simulationManager", BindingFlags.Instance | BindingFlags.NonPublic);
            FieldInfo worldField = controller.GetType().GetField("_world", BindingFlags.Instance | BindingFlags.NonPublic);
            MethodInfo open = controller.GetType().GetMethod("OpenSavesMenu", BindingFlags.Instance | BindingFlags.NonPublic);
            var sim = simField?.GetValue(controller) as PoliSim.Simulation.SimulationManager;
            var world = worldField?.GetValue(controller) as World;
            if (sim == null || world == null || open == null)
            {
                Debug.LogError("SHOT: saves-menu reflection failed - the 92 capture is MISSING, not clean.");
                yield break;
            }

            string dir = PoliSim.Persistence.SaveGameService.DefaultSaveDirectory;
            string pathA = System.IO.Path.Combine(dir, "zz_driver_capture_a.json");
            string pathB = System.IO.Path.Combine(dir, "zz_driver_capture_b.json");
            PoliSim.Persistence.SaveGameService.SaveToFile(pathA,
                PoliSim.Persistence.SaveGameService.CreateSaveGame(sim, world, _countryId, controller.CaptureUiDrafts()));
            PoliSim.Persistence.SaveGameService.SaveToFile(pathB,
                PoliSim.Persistence.SaveGameService.CreateSaveGame(sim, world, _countryId, controller.CaptureUiDrafts()));

            open.Invoke(controller, null);
            yield return Settle();
            yield return Capture("92_saves_menu");

            SetPrivateField(controller, "_savesMenuOpen", false);
            PoliSim.Persistence.SaveGameService.DeleteSave(pathA);
            PoliSim.Persistence.SaveGameService.DeleteSave(pathB);
            yield return Settle();
        }

        private IEnumerator Settle()
        {
            for (int i = 0; i < SettleFrames; i++)
            {
                yield return null;
            }
        }

        private int _canvasTextViolations;
        private int _canvasTextAsserts;

        /// <summary>One canvas-text assert per pinned Canvas capture. A Canvas that failed to build asserts nothing and says so — the guard's own "verified nothing" path counts as a violation here, so a silent degradation can't launder a clean count.</summary>
        private void RecordCanvasTextAssert(string context, GameController controller)
        {
            if (!controller.CanvasSelectorActive)
            {
                Debug.LogWarning($"CANVAS TEXT [{context}]: no canvas surface live - nothing asserted (the IMGUI fallback is up).");
                return;
            }

            _canvasTextAsserts++;
            int violations = CanvasTextGuard.Assert(context);
            _canvasTextViolations += violations < 0 ? 1 : violations;
        }

        /// <summary>Bound on the canvas-settle wait — generous against a ~25-frame envelope, but a bound, per the standing rule that an unbounded wait is a hang with no log line.</summary>
        private const int MaxCanvasSettleFrames = 600;

        /// <summary>
        /// Waits until the takeover seam is settled in the REQUESTED state: <paramref name="wantActive"/>
        /// true = the Canvas surface live and its envelope finished; false = handed back to IMGUI.
        /// Falls through after the bound (or immediately when the Canvas build failed and the IMGUI
        /// selector is the live path), logging which — a capture of the wrong state must be a named
        /// event, never a silent one.
        /// </summary>
        private IEnumerator WaitForCanvasSettle(GameController controller, bool wantActive)
        {
            FieldInfo failedField = controller.GetType().GetField("_canvasSelectorFailed", BindingFlags.Instance | BindingFlags.NonPublic);
            for (int i = 0; i < MaxCanvasSettleFrames; i++)
            {
                if (failedField?.GetValue(controller) is bool failed && failed)
                {
                    Debug.Log("SHOT: canvas selector build FAILED - the IMGUI fallback is the live path; capturing that.");
                    yield break;
                }

                if (controller.CanvasTransitionSettled && controller.CanvasSelectorActive == wantActive)
                {
                    yield break;
                }

                yield return null;
            }

            Debug.LogWarning($"SHOT: canvas seam never settled to active={wantActive} within {MaxCanvasSettleFrames} frames - capturing whatever is up.");

            // ⚠ THE FALLBACK'S OWN TRACE (2026-08-25 hang investigation). The warning above is the
            // TRIGGER for "capture whatever is up," not proof the coroutine actually got there - a
            // hang between this line and the next Capture() call would look, in the log, identical to
            // one that never entered this method at all. Both read as "warning, then silence." This
            // line is the one fact that tells them apart: present, and the fallthrough genuinely
            // completed (the hang, if any, is downstream - see Capture's own entry trace); absent
            // despite the warning appearing, and this method itself is where execution stopped, which
            // given the loop above already exited would be a stranger finding than anything named so
            // far. Per the standing rule this investigation itself restates: a fallback that leaves no
            // trace is unfalsifiable, and that is its own defect independent of whatever the seam's
            // actual root cause turns out to be.
            Debug.Log("SHOT: seam-settle fallback complete, handing control back to the caller.");
        }

        private int _captured;
        private int _failed;

        private IEnumerator Capture(string name)
        {
            // ⚠ THIS IS WHY THE RUNNER CANNOT USE -batchmode.
            //
            // `WaitForEndOfFrame` NEVER RESUMES in batchmode - the coroutine simply stops, the capture
            // never happens, and Unity sits there until something kills it. That is exactly how the
            // first attempt at this failed: it attached, logged "driver attached, label=none, 640x480",
            // and then hung with no error. A silent hang, not a stack trace.
            //
            // Running with a real Editor window fixes both halves at once: the coroutine resumes, and
            // the Game View is a real size instead of batchmode's 640x480 default - which matters more
            // than usual here, because every style in this UI derives its font size from Screen.height,
            // so a 640x480 capture would be showing font sizes no player ever sees.
            // ⚠ THE SCREENSHOT IS NOT THE VERDICT. Every one of this project's eleven clipping instances
            // was found by a person looking at a screen, and twelve screens were captured and APPROVED
            // by eye with overflows still in them. So each shot is CHECKED as well as taken.
            //
            // Labelled here rather than after the capture because MeasuredLabel records during the OnGUI
            // of the frame being captured - naming the screen afterwards would file this screen's
            // overflows under the next one.
            UiGuardContext.CurrentScreen = name;

            // ⚠ ENTRY TRACE (2026-08-25 hang investigation), paired with WaitForCanvasSettle's own
            // fallback trace. This project's own history names TWO independent, already-diagnosed
            // causes for a hang that stalls exactly here with no further log output ever: (1) the
            // driver run with -batchmode/-nographics, which stops WaitForEndOfFrame from ever
            // resuming (root-caused 2026-08-18, this class's own doc comment above), and (2) an
            // unresolved, intermittent Unity Editor "Start Indexing on Editor startup" hang recorded
            // recurring on this environment across five consecutive automated windowed capture
            // attempts previously, independent of workload. Both produce identical log silence, so
            // this line is what tells "never reached WaitForEndOfFrame" apart from "reached it and
            // Unity itself stopped rendering" - if this line prints but SHOT: wrote/SHOT: capture
            // returned null never follows, the cause is (1) or (2) above, not this driver's own code.
            Debug.Log($"SHOT: entering WaitForEndOfFrame for {name}.");
            yield return new WaitForEndOfFrame();
            Debug.Log($"SHOT: WaitForEndOfFrame resumed for {name}.");

            Texture2D shot = ScreenCapture.CaptureScreenshotAsTexture();
            if (shot == null)
            {
                Debug.LogError($"SHOT: capture returned null for {name}.");
                _failed++;
                yield break;
            }

            string path = Path.Combine(OutputDirectory, $"{Label}_{name}.png");
            File.WriteAllBytes(path, shot.EncodeToPNG());
            Debug.Log($"SHOT: wrote {path} at {shot.width}x{shot.height}");
            _captured++;
            Destroy(shot);
        }

        /// <summary>Prints every overflow the guard recorded and returns the count. Already deduped by the guard itself - see the note there on why that has to happen at record time.</summary>
        private static int ReportOverflows()
        {
#if UNITY_EDITOR
            foreach (UiOverflowGuard.Violation v in UiOverflowGuard.Violations)
            {
                Debug.LogError($"OVERFLOW: {v}");
            }

            // ⚠ THE TOTAL, NOT THE LIST LENGTH. The list is capped at 200 to bound memory; the first
            // pass reported exactly 200 and that was read as the count when it was the ceiling - the
            // real figure was 608. Print both, and fail on the total.
            int total = UiOverflowGuard.TotalViolations;
            if (total > UiOverflowGuard.Violations.Count)
            {
                Debug.LogError($"OVERFLOW: {total - UiOverflowGuard.Violations.Count} further violation(s) " +
                               $"beyond the {UiOverflowGuard.Violations.Count} printed above.");
            }

            return total;
#else
            return 0;
#endif
        }

        /// <summary>
        /// Prints every rect that escaped its container. Separate from <see cref="ReportOverflows"/>
        /// because it is a separate defect: a label can fit its rect perfectly while the rect sits
        /// outside the tile, which is exactly how the stat tile's delta came to be drawn on its
        /// neighbour's keyline.
        /// </summary>
        private static int ReportContainmentEscapes()
        {
#if UNITY_EDITOR
            foreach (UiContainmentGuard.Violation v in UiContainmentGuard.Violations)
            {
                Debug.LogError($"ESCAPE: {v}");
            }

            int total = UiContainmentGuard.TotalViolations;
            if (total > UiContainmentGuard.Violations.Count)
            {
                Debug.LogError($"ESCAPE: {total - UiContainmentGuard.Violations.Count} further escape(s) " +
                               $"beyond the {UiContainmentGuard.Violations.Count} printed above.");
            }

            return total;
#else
            return 0;
#endif
        }

        /// <summary>Instance rather than static (was static until the country-leak fix) so it can set <see cref="_finishCalled"/> - Start's own <c>finally</c> reads that guard to tell a normal exit from one it had to force itself.</summary>
        private void Finish(int exitCode)
        {
            _finishCalled = true;
#if UNITY_EDITOR
            UnityEditor.EditorApplication.Exit(exitCode);
#endif
        }

        /// <summary>Params rather than a single `object arg` (widened for the country-leak fix) so this can also invoke a zero-argument method like ResetPlayerCountrySelection, not only SelectPlayerCountry's one-argument shape.</summary>
        private static void Invoke(object target, string method, params object[] args)
        {
            MethodInfo m = target.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic);
            if (m == null)
            {
                Debug.LogError($"SHOT: method {method} not found - the screen will be WRONG, not missing.");
                return;
            }

            m.Invoke(target, args);
        }

        /// <summary>
        /// The left column's own scroll view, deliberately left alone so it stays identical between
        /// captures and cannot be mistaken for part of what changed.
        /// </summary>
        private const string LeftColumnScrollField = "_leftColumnScrollPosition";

        private static FieldInfo[] _scrollFields;

        /// <summary>
        /// Every `Vector2 *ScrollPosition` field on GameController, DISCOVERED rather than listed.
        ///
        /// ⚠ **A hardcoded list of six shipped and was silently wrong.** Policy/Laws does not scroll
        /// through `_policyLawsContentScrollPosition` - each of its sub-screens owns its own
        /// (`_laborMarketScrollPosition`, `_crimeJusticeScrollPosition`, `_sectorPolicyScrollPosition`),
        /// none of which was in the list. So every "scrolled" capture of that tab was a no-op that
        /// happened to look plausible, because the preamble there is short enough that rows appear
        /// anyway. The deep pass is what exposed it: identical to the shallow one, pixel for pixel.
        ///
        /// There are eighteen such fields. Enumerating them is the same discipline
        /// `ChromeV2CoverageCheck` applies to sprites - a list omits whatever it forgot, and cannot tell
        /// you that it did. Only one view is on screen at a time, so setting them all is harmless.
        /// </summary>
        private static FieldInfo[] ScrollFields(object target)
        {
            if (_scrollFields != null)
            {
                return _scrollFields;
            }

            var found = new List<FieldInfo>();
            foreach (FieldInfo f in target.GetType().GetFields(BindingFlags.Instance | BindingFlags.NonPublic))
            {
                if (f.FieldType == typeof(Vector2) && f.Name.EndsWith("ScrollPosition") && f.Name != LeftColumnScrollField)
                {
                    found.Add(f);
                }
            }

            _scrollFields = found.ToArray();
            Debug.Log($"SHOT: driving {_scrollFields.Length} scroll views.");
            return _scrollFields;
        }

        /// <summary>
        /// Days to advance before capturing. Long enough that every history-dependent surface is
        /// populated: graphs plot, the map has trade lines, published bulletins exist, and
        /// `GraphRenderer`'s page row has more than one page to page through.
        ///
        /// ⚠ **The reason this exists is that turn-0 captures systematically under-tested the UI**, and it
        /// took three separate discoveries to see it. A chart with no data draws no plate, so
        /// `GraphRenderer` and `MapRenderer` sat on near-black grounds through every v2.0 capture and
        /// looked like paper. The page row emitted no controls, so a behaviour-5 defect was invisible.
        /// Anything whose appearance depends on history existing was, in effect, never captured at all.
        ///
        /// 3 turns x 365 days clears `WindowSize` (50) history points on the quarterly series, which is
        /// what makes the pagination row real rather than merely present.
        /// </summary>
        private const int MinWarmupDays = 365 * 3;

        /// <summary>Ceiling on the search for a preliminary release, so a schedule change can never turn the warm-up into an unbounded loop - the same bounded-retry discipline UiScreenshotCapture applies to waiting for play mode.</summary>
        private const int MaxWarmupDays = 365 * 5;

        /// <summary>
        /// Drives the simulation forward through the real `AdvanceDay` / `AdvanceTurn` pair, so warmed-up
        /// state is produced by the same path play produces it.
        ///
        /// ⚠ **`AdvanceDay` ALONE IS NOT ENOUGH, and the first attempt at this proved it.** `AdvanceDay`
        /// runs the daily systems and returns true on a turn boundary, but it is the CALLER that then
        /// calls `AdvanceTurn` - which is what appends `StatHistory`. Driving days alone advanced the
        /// calendar three years and moved debt from $36.0T to $41.9T, yet left the UI reading "Turn 0"
        /// and every graph saying "No data yet". A warm-up that moves the economy without producing
        /// history is precisely the failure this whole pass exists to correct, in miniature.
        /// </summary>
        private static void AdvanceDays(object controller, CountryId playerCountry)
        {
            FieldInfo simField = controller.GetType().GetField("_simulationManager", BindingFlags.Instance | BindingFlags.NonPublic);
            if (!(simField?.GetValue(controller) is SimulationManager sim))
            {
                Debug.LogError("SHOT: could not reach SimulationManager - capturing at turn 0, which under-tests every history-dependent surface.");
                return;
            }

            // Empty decisions: the warm-up is "time passed with no policy changes", which is the neutral
            // baseline a capture wants. Anything else would bake one playthrough's choices into what is
            // meant to be a picture of the UI.
            var noDecisions = new Dictionary<CountryId, PolicyDecision>();
            int turns = 0;
            int days = 0;
            for (int i = 0; i < MaxWarmupDays; i++)
            {
                if (sim.AdvanceDay())
                {
                    sim.AdvanceTurn(noDecisions);
                    turns++;
                }

                days++;

                // ⚠ STOP ON A PRELIMINARY RELEASE, not on a day count.
                //
                // Behaviour 6's whole point is the state where a figure is published but not yet
                // revised - badged, dated AND dashed at once. A fixed warm-up cannot capture it except
                // by luck: three years of history left every series reading FINAL, because everything
                // old enough to plot is also old enough to have been revised. The window between a
                // release and its revision is narrow and moves, so the driver has to WATCH for it
                // rather than guess a date.
                //
                // Minimum days first, so the graphs still have a trend to draw - a preliminary figure on
                // an empty chart would prove B6 and nothing else.
                if (days >= MinWarmupDays && AnyPreliminary(sim, playerCountry))
                {
                    Debug.Log($"SHOT: stopped on a PRELIMINARY release at day {days} / turn {turns} - behaviour 6's provisional state is on screen.");
                    return;
                }
            }

            Debug.LogWarning($"SHOT: advanced {days} days / {turns} turns without catching a preliminary release - B6's provisional case will NOT be in this capture.");
        }

        /// <summary>
        /// Drafts a Sovereign Wealth Fund and pushes ONE asset-class weight off its default.
        ///
        /// ⚠ **Without this the SWF trailing column cannot be judged, and was verified by argument only
        /// for two sessions.** The four weights default to 40/30/15/15, which sums to exactly 100 - so
        /// raw weight and normalised "% of fund" print identical numbers and the column looks like a
        /// restatement of the figure beside it. Its actual purpose is that dragging ONE weight moves the
        /// other three, and that is invisible until the four stop summing to 100.
        ///
        /// Equities to 80 makes the sum 140, so every share diverges from its raw weight at once
        /// (80 -> 57%, 30 -> 21%, 15 -> 11%) and the three untouched rows visibly move. Drafting the fund
        /// as well so the rows render interactive rather than disabled, which is also what puts the amber
        /// draft cue on screen - the row differs from standing, which is exactly what behaviour 1 marks.
        /// </summary>
        private static void DivergeSwfWeights(object controller)
        {
            if (!SetPrivateField(controller, "_swfExistsDraft", true)
                || !SetPrivateField(controller, "_swfEquitiesWeightInput", 80f))
            {
                Debug.LogWarning("SHOT: could not diverge SWF weights - the normalised column will read identical to raw and prove nothing.");
            }
        }

        /// <summary>
        /// Drafts two spending lines - one up, one down - so behaviour 1's carriers actually RENDER.
        ///
        /// Without this every spending row captures at zero change, which means the hatch band has zero
        /// width, the draft figure is null, and icon_pencil_draft never draws. The screen would look
        /// correct and prove nothing, exactly the way the SWF weight column did before
        /// <see cref="DivergeSwfWeights"/>. Two directions rather than one because the hatch is
        /// explicitly bidirectional (see LedgerRow.DrawTrackFurniture) and a single raise would leave
        /// the cut case unexercised.
        /// </summary>
        private static void DraftSpendingLines(object controller)
        {
            FieldInfo f = controller.GetType().GetField("_spendingLineInputs", BindingFlags.Instance | BindingFlags.NonPublic);
            if (f == null || !(f.GetValue(controller) is System.Collections.IDictionary inputs))
            {
                Debug.LogWarning("SHOT: could not draft spending lines - B1's hatch and pencil will not render and the capture proves nothing.");
                return;
            }

            // ⚠ SEEDED FROM THE COUNTRY, NOT FROM THE DICTIONARY'S EXISTING KEYS. `_spendingLineInputs`
            // is filled lazily as each row draws, so before the first Budget frame it is EMPTY - an
            // earlier version of this iterated its keys, drafted nothing, and reported success.
            object country = controller.GetType()
                .GetField("_playerCountry", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.GetValue(controller);
            object lines = country?.GetType().GetProperty("SpendingLines")?.GetValue(country)
                ?? country?.GetType().GetField("SpendingLines")?.GetValue(country);

            if (!(lines is System.Collections.IEnumerable spendingLines))
            {
                Debug.LogWarning("SHOT: could not reach SpendingLines - B1's hatch and pencil will not render.");
                return;
            }

            int drafted = 0;
            foreach (object line in spendingLines)
            {
                object category = line.GetType().GetProperty("Category")?.GetValue(line)
                    ?? line.GetType().GetField("Category")?.GetValue(line);
                if (category == null)
                {
                    continue;
                }

                // EVERY line, alternating direction - not the first two. Drafting only a couple left the
                // carriers real but off-screen: the bill direction moved, proving the draft was live,
                // while every row in frame still rendered at zero change. A capture that depends on
                // which rows happen to be scrolled into view is not evidence.
                inputs[category] = (drafted % 2 == 0) ? 6f : -4f;
                drafted++;
            }

            Debug.Log($"SHOT: drafted {drafted} spending lines (+6%, -4%) so B1's hatch, draft figure and pencil render.");
        }

        private static bool SetPrivateField(object target, string field, object value)
        {
            FieldInfo f = target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic);
            if (f == null || !f.FieldType.IsInstanceOfType(value))
            {
                Debug.LogError($"SHOT: field {field} not found or wrong type.");
                return false;
            }

            f.SetValue(target, value);
            return true;
        }

        /// <summary>True when ANY published series on the player country is currently sitting on a preliminary release - the state behaviour 6 exists to distinguish, and the one a fixed-length warm-up reliably misses. Reads the country actually being captured — this hardcoded USA until 2026-08-12, which was invisible only because USA was the only country ever captured.</summary>
        private static bool AnyPreliminary(SimulationManager sim, CountryId playerCountry)
        {
            Country country = sim.World?.GetCountry(playerCountry);
            if (country?.Published == null)
            {
                return false;
            }

            foreach (KeyValuePair<PublishedStat, PublishedSeries> kv in country.Published.Series)
            {
                PublishedEntry latest = kv.Value.Latest();
                if (latest != null && latest.Status == RevisionStatus.Preliminary)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>Ceiling on the search for an election eve — bounded like every other wait in this harness. Election cycles are measured in (365-day) turns, so six years covers any cycle this game has ever configured.</summary>
        private const int MaxHeldSearchDays = 365 * 6;

        /// <summary>
        /// Drives the calendar to the eve of an election turn so the Fed-chair selection fires through
        /// its own real path (GameController.UpdateFedChairSelectionState), then captures the HELD
        /// state twice — on a standard tab (the calendar strip's banner) and on Budget full-screen
        /// (DrawFullScreenPendingInterruptBanner's re-surfaced copy). Those are the two sites of B8's
        /// always-visible interrupt indicator, and the `ui_banner_hold` plate dressed onto them.
        ///
        /// ⚠ **Without this pass the hold banner's presence in the set is LUCK, not a guarantee.**
        /// This method's first draft claimed the banner would otherwise exist in NO capture — the
        /// 2026-08-12 run disproved that in the other direction: the warm-up's preliminary-release
        /// stop happened to land on an election eve (turn 3), so the fed-chair pause was live in
        /// EVERY main capture. That is a coincidence of two unrelated constants — the election cycle
        /// and the publication cadence — and either moving would silently drop the HELD state from
        /// the set with nothing reporting it. This pass pins it; `01b_running_strip` pins the
        /// opposite state, which that same coincidence currently excludes from the main set.
        ///
        /// The state is produced by the REAL trigger — an election eve reached through the same
        /// AdvanceDay/AdvanceTurn pair play uses — not by injecting a candidate list. An injected list
        /// would light the Budget site (which reads `_fedChairCandidates` directly) while leaving the
        /// calendar site dark (its flag is recomputed per frame from election timing), which is
        /// exactly the half-real evidence rule 14 warns about: a capture that proves one of the two
        /// sites while appearing to prove both.
        /// </summary>
        private IEnumerator CaptureHeldState(GameController controller)
        {
            FieldInfo simField = controller.GetType().GetField("_simulationManager", BindingFlags.Instance | BindingFlags.NonPublic);
            if (!(simField?.GetValue(controller) is SimulationManager sim))
            {
                Debug.LogError("SHOT: could not reach SimulationManager - the HELD banner will not be captured.");
                yield break;
            }

            var noDecisions = new Dictionary<CountryId, PolicyDecision>();
            int guard = 0;
            while (!ElectionSystem.IsElectionTurn(sim.CurrentTurn + 1) && guard < MaxHeldSearchDays)
            {
                if (sim.AdvanceDay())
                {
                    sim.AdvanceTurn(noDecisions);
                }

                guard++;
            }

            if (!ElectionSystem.IsElectionTurn(sim.CurrentTurn + 1))
            {
                Debug.LogWarning($"SHOT: no election eve within {MaxHeldSearchDays} days - the HELD banner will NOT be in this capture set.");
                yield break;
            }

            SetEnumField(controller, "_consolidatedTab", "Statistics");
            yield return Settle();

            // ⚠ VERIFY THE PAUSE ACTUALLY ENGAGED before writing captures whose NAMES claim it did.
            // The fed-chair selection is a no-op for a country without an independent chair (the
            // Eurozone three share the ECB), so on those countries an election eve pauses nothing —
            // and a capture named "interrupt_held" showing the RUNNING strip would be the harness
            // lying in the filename. Skipped with a log instead: HELD is pinned only where the
            // fed-chair path exists, and the per-country coverage report says so.
            FieldInfo candidatesField = controller.GetType().GetField("_fedChairCandidates", BindingFlags.Instance | BindingFlags.NonPublic);
            if (!(candidatesField?.GetValue(controller) is System.Collections.ICollection candidates) || candidates.Count == 0)
            {
                Debug.LogWarning($"SHOT: election eve reached but no fed-chair selection engaged for {_countryId} " +
                                 "(no independent chair) - HELD captures skipped rather than mislabelled.");
                yield break;
            }

            yield return Capture("90_interrupt_held");

            SetEnumField(controller, "_consolidatedTab", "Budget");
            ResetScrolls(controller);
            yield return Settle();
            yield return Capture("91_interrupt_held_budget");
        }

        /// <summary>Bound on each state search (budget pause, decision/meeting rolls). Generous — four sim years — but a bound, per this harness's standing rule that an unbounded wait is a hang with no log line.</summary>
        private const int MaxStateSearchDays = 365 * 4;

        /// <summary>
        /// The state-pinning pass (2026-08-12, Elias: pin the reachable axes the way the interrupt
        /// state was pinned). Every state here is produced through the REAL path — the public sim API
        /// and the same day-advance play uses — with two reflective touches into GameController's
        /// private draft state (`_cabinetCandidatesByPortfolio`, the sub-screen selectors), the same
        /// idiom the rest of this driver already runs on.
        ///
        /// ⚠ ORDER IS LOAD-BEARING: pending BILLS are introduced LAST, after every day-advancing
        /// search, so their countdowns never tick and their (arbitrary) dial values can never resolve
        /// into the economy and mangle the state other captures show.
        ///
        /// What is deliberately NOT here, reported rather than half-pinned: an ELECTION resolving
        /// leaves no observable UI state (no record, no results screen — the Canvas election night is
        /// its future home), so there is nothing a capture could pin; same shape as the stamps, and
        /// the DivisionLog precedent now exists for the record it would need.
        /// </summary>
        private IEnumerator CaptureStatePins(GameController controller)
        {
            FieldInfo simField = controller.GetType().GetField("_simulationManager", BindingFlags.Instance | BindingFlags.NonPublic);
            if (!(simField?.GetValue(controller) is SimulationManager sim))
            {
                Debug.LogError("SHOT: could not reach SimulationManager - state pins skipped.");
                yield break;
            }

            Country player = sim.World?.GetCountry(_countryId);
            if (player == null)
            {
                Debug.LogError("SHOT: could not reach the player country - state pins skipped.");
                yield break;
            }

            // --- A0. CALENDAR PANEL: the "month-boundary flip" pin (Calendar Panel, see CLAUDE.md).
            // An empty/static calendar validates nothing - this drives to the LAST day of whichever
            // month CurrentDate currently sits in, captures, advances exactly one more real day
            // (crossing into the next month), and captures again, so the two frames differ by one day
            // and should show the panel's displayed month genuinely changing, not a stale carryover.
            var noDecisionsForCalendar = new Dictionary<CountryId, PolicyDecision>();
            int daysInCurrentMonth = System.DateTime.DaysInMonth(sim.CurrentDate.Year, sim.CurrentDate.Month);
            int calendarDays = 0;
            while (sim.CurrentDate.Day != daysInCurrentMonth && calendarDays < MaxStateSearchDays)
            {
                if (sim.AdvanceDay()) { sim.AdvanceTurn(noDecisionsForCalendar); }
                sim.AdvanceCountryDayTick(_countryId);
                calendarDays++;
            }

            SetEnumField(controller, "_consolidatedTab", "Decisions");
            ResetScrolls(controller);
            yield return Settle();
            yield return Capture("80a_calendar_month_end");

            if (sim.AdvanceDay()) { sim.AdvanceTurn(noDecisionsForCalendar); }
            sim.AdvanceCountryDayTick(_countryId);
            yield return Settle();
            yield return Capture("80b_calendar_month_flip");

            // --- A. CABINET: the search state, then the appointed roster. ---
            FieldInfo candField = controller.GetType().GetField("_cabinetCandidatesByPortfolio", BindingFlags.Instance | BindingFlags.NonPublic);
            var searchResults = new Dictionary<CabinetPortfolio, List<CabinetMinister>>();
            foreach (CabinetPortfolio portfolio in System.Enum.GetValues(typeof(CabinetPortfolio)))
            {
                // Guarded because only 3 of the 6 portfolios have authored candidate pools — an
                // unimplemented portfolio throwing must cost one portfolio, not the whole pass.
                try
                {
                    List<CabinetMinister> candidates = CabinetSystem.GenerateCandidates(portfolio);
                    if (candidates != null && candidates.Count > 0)
                    {
                        searchResults[portfolio] = candidates;
                    }
                }
                catch (System.Exception e)
                {
                    Debug.Log($"SHOT: no candidate pool for {portfolio} ({e.GetType().Name}) - skipped.");
                }
            }

            if (candField?.GetValue(controller) is System.Collections.IDictionary candDict)
            {
                foreach (var kv in searchResults)
                {
                    candDict[kv.Key] = kv.Value;
                }
            }

            SetEnumField(controller, "_consolidatedTab", "Politics");
            SetEnumField(controller, "_politicsCategory", "Cabinet");
            ResetScrolls(controller);
            yield return Settle();
            yield return Capture("82a_cabinet_search");

            foreach (var kv in searchResults)
            {
                player.CabinetMinisters[kv.Key] = kv.Value[0];
                (candField?.GetValue(controller) as System.Collections.IDictionary)?.Remove(kv.Key);
            }

            Debug.Log($"SHOT: appointed {searchResults.Count} minister(s) through the public candidate pool.");
            yield return Settle();
            yield return Capture("82b_cabinet_appointed");

            // R4-4: the roster's NEW bottom half - Defense/ForeignAffairs/Education panels, whose
            // appointed ministers render the PROCEDURAL PLACEHOLDER until the D1 portrait delivery
            // lands. The batch bar names this state explicitly, and the top-anchored 82b capture
            // cannot reach it (six panels no longer fit one screen at either size).
            ScrollBy(controller, 1500f);
            yield return Settle();
            yield return Capture("82c_cabinet_new_portfolios");

            // R4-4: pin one REAL new-portfolio decision - rolled from the live pools, never
            // fabricated - so the new content's decision card is on camera too. The warm-up's
            // natural search keeps hitting 0 cabinet decisions (the known day-0 variance), and
            // waiting on a 12%/minister/turn roll to land organically would make this capture a
            // coin flip per run. Draws consumed on the Cabinet stream are a capture-run concern
            // only. The pin is REMOVED after the shot so the pending-decision Advance-Turn gate
            // cannot wedge the later day-driven states.
            {
                FieldInfo pendingField = typeof(SimulationManager).GetField("_pendingCabinetDecisionsByCountry", BindingFlags.Instance | BindingFlags.NonPublic);
                if (pendingField?.GetValue(sim) is Dictionary<CountryId, List<(CabinetPortfolio Portfolio, CabinetDecision Decision)>> pendingByCountry)
                {
                    var pinned = new List<(CabinetPortfolio Portfolio, CabinetDecision Decision)>();
                    for (int i = 0; i < 1000 && pinned.Count == 0; i++)
                    {
                        foreach ((CabinetPortfolio portfolio, CabinetDecision decision) in CabinetSystem.TryRollDecisions(player))
                        {
                            if (portfolio == CabinetPortfolio.Defense || portfolio == CabinetPortfolio.ForeignAffairs || portfolio == CabinetPortfolio.Education)
                            {
                                pinned.Add((portfolio, decision));
                                break;
                            }
                        }
                    }

                    if (pinned.Count > 0)
                    {
                        pendingByCountry[_countryId] = pinned;
                        SetEnumField(controller, "_consolidatedTab", "Decisions");
                        ResetScrolls(controller);
                        yield return Settle();
                        yield return Capture("82d_cabinet_new_decision");
                        pendingByCountry.Remove(_countryId);
                        Debug.Log($"SHOT: pinned a {pinned[0].Portfolio} decision ({pinned[0].Decision.Name}), then cleared it.");
                    }
                    else
                    {
                        Debug.Log("SHOT: no new-portfolio decision rolled in 1000 tries - card capture skipped.");
                    }
                }
            }

            // --- B. THE BUDGET-PROCESS PAUSE: advance to the country's own fiscal-year date. ---
            //
            // ⚠ THE DAILY ARM CALLS ARE THE CONTROLLER'S, NOT AdvanceDay's — measured, not assumed:
            // the first version of this search advanced 1460 days on three countries and pinned
            // NOTHING, because TryOpenBudgetProcess and TryRollForeignPolicyMeeting are called once
            // per day from GameController.Update's day loop, which a sim-side advance never runs.
            // The driver now makes the same daily calls the controller makes — the real path,
            // invoked by the harness the way play invokes it.
            // ⚠ ONE DAY MODEL, SHARED WITH PLAY (2026-08-12, ruled): every search below advances via
            // AdvanceCountryDayTick — the extracted controller day tick — instead of a hand-copied
            // subset of its calls. The copies are what produced three "never captured" findings in one
            // day; a call added to the controller's day now reaches these searches automatically.
            var noDecisions = new Dictionary<CountryId, PolicyDecision>();
            int days = 0;
            while (!sim.GetPendingBudgetProcess(_countryId) && days < MaxStateSearchDays)
            {
                if (sim.AdvanceDay()) { sim.AdvanceTurn(noDecisions); }
                sim.AdvanceCountryDayTick(_countryId);
                days++;
            }

            if (sim.GetPendingBudgetProcess(_countryId))
            {
                Debug.Log($"SHOT: budget-process pause reached after {days} day(s).");
                SetEnumField(controller, "_consolidatedTab", "Decisions");
                ResetScrolls(controller);
                yield return Settle();
                yield return Capture("83a_budget_pause_decisions");

                SetEnumField(controller, "_consolidatedTab", "Budget");
                ResetScrolls(controller);
                yield return Settle();
                yield return Capture("83b_budget_pause_budget");
            }
            else
            {
                Debug.LogWarning($"SHOT: no budget-process pause within {MaxStateSearchDays} days - NOT pinned for {_countryId}.");
            }

            // --- C. A CABINET DECISION and/or FOREIGN POLICY MEETING: real rolls, bounded search. ---
            days = 0;
            while (sim.GetPendingCabinetDecisions(_countryId).Count == 0
                   && sim.GetPendingForeignPolicyMeeting(_countryId) == null
                   && days < MaxStateSearchDays)
            {
                if (sim.AdvanceDay()) { sim.AdvanceTurn(noDecisions); }
                sim.AdvanceCountryDayTick(_countryId);
                days++;
            }

            int cabinetPending = sim.GetPendingCabinetDecisions(_countryId).Count;
            bool meetingPending = sim.GetPendingForeignPolicyMeeting(_countryId) != null;
            if (cabinetPending > 0 || meetingPending)
            {
                Debug.Log($"SHOT: decision search after {days} day(s): {cabinetPending} cabinet decision(s), foreign-policy meeting: {meetingPending}.");
                SetEnumField(controller, "_consolidatedTab", "Decisions");
                ResetScrolls(controller);
                yield return Settle();
                yield return Capture("84_decisions_pending");
            }
            else
            {
                Debug.LogWarning($"SHOT: neither a cabinet decision nor a foreign-policy meeting within {MaxStateSearchDays} days - the CABINET and FOREIGN POLICY dossiers stay unpinned for {_countryId}.");
            }

            // --- C2. THE FOREIGN-POLICY MEETING specifically (fold-in, Elias 2026-08-12): the search
            // above stops on whichever fires first, and cabinet decisions always won. This one keeps
            // rolling PAST pending cabinet decisions until a meeting lands.
            if (!meetingPending)
            {
                days = 0;
                while (sim.GetPendingForeignPolicyMeeting(_countryId) == null && days < MaxStateSearchDays)
                {
                    if (sim.AdvanceDay()) { sim.AdvanceTurn(noDecisions); }
                    sim.AdvanceCountryDayTick(_countryId);
                    days++;
                }

                if (sim.GetPendingForeignPolicyMeeting(_countryId) != null)
                {
                    Debug.Log($"SHOT: foreign-policy meeting landed after {days} further day(s).");
                    SetEnumField(controller, "_consolidatedTab", "Decisions");
                    ResetScrolls(controller);
                    yield return Settle();
                    yield return Capture("84b_meeting_decisions");
                }
                else
                {
                    Debug.LogWarning($"SHOT: no foreign-policy meeting within {MaxStateSearchDays} further days - the FOREIGN POLICY dossier stays unpinned for {_countryId}.");
                }
            }

            // --- D. PENDING BILLS, one of every type — LAST, so no day ever ticks their countdowns. ---
            TaxType? taxPick = null;
            foreach (TaxLine line in player.TaxLines)
            {
                if (!line.IsImplemented) { taxPick = line.Type; break; }
            }
            if (taxPick == null && player.TaxLines.Count > 0) { taxPick = player.TaxLines[0].Type; }
            bool taxOk = taxPick != null && sim.IntroduceTaxProgramBill(_countryId, taxPick.Value,
                isAdd: !FindTaxLine(player, taxPick.Value).IsImplemented);

            WelfareProgramType? welfarePick = null;
            bool welfareAdd = true;
            foreach (WelfareProgram program in player.WelfarePrograms)
            {
                if (!program.IsImplemented) { welfarePick = program.Type; break; }
            }
            if (welfarePick == null && player.WelfarePrograms.Count > 0)
            {
                welfarePick = player.WelfarePrograms[0].Type;
                welfareAdd = false;
            }
            bool welfareOk = welfarePick != null && sim.IntroduceWelfareProgramBill(_countryId, welfarePick.Value, welfareAdd);

            bool laborOk = sim.IntroduceLaborBill(_countryId, new LaborPolicyBill { MinimumWage = 12f });
            bool crimeOk = sim.IntroduceCrimeJusticeBill(_countryId, new CrimeJusticePolicyBill { PoliceFunding = 55f });

            // Content marathon end-of-run bar: "full captures pinned on a populated browser AND a
            // populated enacted list" - direct real-API assignment (the same idiom section A's
            // CabinetMinisters pin uses) rather than a real vote, guaranteeing the enacted state
            // deterministically. A representative set spanning every dial, not a repeat of the
            // original MVP pin's single law - the Laws browser now needs to show a genuinely
            // populated enacted list, not just prove the mechanism works once. Two further laws get
            // a real pending bill in the same "pending bills, LAST" batch, so the browser shows
            // available/enacted/pending together in one capture.
            string[] lawsToEnactForCapture =
            {
                "three_strikes_law", "cash_bail_abolition_act", "drug_decriminalization_act",
                "public_defender_funding_act", "body_worn_camera_program", "court_backlog_reduction_program",
                "frontex_border_cooperation_agreement", "restorative_justice_program"
            };
            foreach (string enactId in lawsToEnactForCapture)
            {
                player.EnactedLaws.Add(new EnactedLaw { LawId = enactId, EnactedOn = sim.CurrentDate });
            }
            bool lawOk = sim.IntroduceLawBill(_countryId, new LawBill { LawId = "cash_bail_reform_act", IsRepeal = false });
            bool lawOk2 = sim.IntroduceLawBill(_countryId, new LawBill { LawId = "sanctuary_city_policy", IsRepeal = false });

            var sectorBill = new SectorPolicyBill();
            foreach (Sector sector in player.Sectors)
            {
                sectorBill.SubsidyLevels[sector.Type] = 10f;
                sectorBill.RegulationLevels[sector.Type] = 50f;
                sectorBill.TaxCreditLevels[sector.Type] = 10f;
                sectorBill.ResearchGrantsLevels[sector.Type] = 10f;
                sectorBill.DeregulationLevels[sector.Type] = 0f;
            }
            bool sectorOk = sim.IntroduceSectorBill(_countryId, sectorBill);

            bool tradeOk = sim.IntroduceTradeBill(_countryId, new TradePolicyBill { NewBaseTariffRate = player.BaseTariffRate + 2f });
            bool swfOk = sim.IntroduceSwfDrawdownBill(_countryId, new SwfDrawdownBill { WithdrawalPercentOfGdp = 1f });

            Debug.Log($"SHOT: bills introduced - tax:{taxOk} welfare:{welfareOk} labor:{laborOk} crime:{crimeOk} " +
                      $"sector:{sectorOk} trade:{tradeOk} swfDrawdown:{swfOk} law:{lawOk} law2:{lawOk2} (a false is a finding to read, not an error).");

            SetEnumField(controller, "_consolidatedTab", "Politics");
            SetEnumField(controller, "_politicsCategory", "Parliament");
            ResetScrolls(controller);
            yield return Settle();
            yield return Capture("85a_bills_parliament");

            SetEnumField(controller, "_consolidatedTab", "PolicyLaws");
            foreach ((string sub, string stem) in new[]
            {
                ("LaborMarket", "85b_bill_labormarket"), ("CrimeJustice", "85c_bill_crimejustice"),
                ("Sectors", "85d_bill_sectors"), ("Trade", "85e_bill_trade"),
                // Law system MVP slice: pinned specifically on "laws both available and enacted" -
                // truth_in_sentencing_act shows ENACTED, cash_bail_reform_act shows its pending bill
                // and live PASS/FAIL estimate, and the other two catalog laws show plain "available".
                ("Laws", "85g_bill_laws")
            })
            {
                SetEnumField(controller, "_policyLawsCategory", sub);
                ResetScrolls(controller);
                yield return Settle();
                yield return Capture(stem);
            }

            SetEnumField(controller, "_consolidatedTab", "Budget");
            SetEnumField(controller, "_budgetProcessCategory", "Tax");
            ResetScrolls(controller);
            yield return Settle();
            yield return Capture("85f_bill_tax_rows");

            // --- E. DIVISION RECORDS: let the introduced bills RESOLVE, which is what writes the log
            // (ParliamentSystem.RecordDivision, eight sites) — then capture item 1a's panel with real
            // divisions on it. This deliberately breaks the "bills never resolve" invariant the
            // introduction order above protects, which is why it runs after every bill capture: the
            // arbitrary dial values now DO resolve into the economy, and everything captured from here
            // on shows the post-resolution state.
            // The countdowns tick through the same shared day model as every search above — this loop's
            // first version hand-copied the eight countdown calls and its PREVIOUS version omitted
            // them entirely (23 days, zero divisions, measured). AdvanceCountryDayTick is the fix's
            // structural form: the controller's day, made by the harness the way play makes it.
            int resolveDays = ParliamentSystem.BillDurationDays + 2;
            for (int i = 0; i < resolveDays; i++)
            {
                if (sim.AdvanceDay()) { sim.AdvanceTurn(noDecisions); }
                sim.AdvanceCountryDayTick(_countryId);
            }

            Debug.Log($"SHOT: advanced {resolveDays} day(s) to resolve the introduced bills - divisions recorded: {player.Divisions.Entries.Count}.");
            SetEnumField(controller, "_consolidatedTab", "Politics");
            SetEnumField(controller, "_politicsCategory", "Parliament");
            ResetScrolls(controller);
            yield return Settle();
            yield return Capture("87_divisions_parliament");

            // The Division Records panel sits below the hemicycle and the pending list, so the scrolled
            // capture is the one that actually shows it — the same lesson the _rows captures encode.
            ScrollBy(controller, 900f);
            yield return Settle();
            yield return Capture("87b_divisions_parliament_rows");

            // --- E1b (Step 2): THE TRACE PANEL, on a rich ledger — the resolved bills above
            // recorded real dated events; two more are PINNED here as real writes with real
            // records (the election idiom: pin by doing, so the panel's audit footer still
            // reconciles), then the period is closed so the capture shows a full term list
            // WITH events. A boring ledger validates nothing.
            {
                float pinBefore = player.State.ApprovalRating;
                player.State.ApprovalRating = Mathf.Clamp(player.State.ApprovalRating - 2f, 0f, 100f);
                ApprovalLedgerRecorder.RecordEvent(player, sim.CurrentDate, "Budget bill failed", player.State.ApprovalRating - pinBefore);
                pinBefore = player.State.ApprovalRating;
                player.State.ApprovalRating = Mathf.Clamp(player.State.ApprovalRating - 3f, 0f, 100f);
                ApprovalLedgerRecorder.RecordEvent(player, sim.CurrentDate, "Scandal: ferry contract", player.State.ApprovalRating - pinBefore);
            }

            for (int i = 0; i < SimulationManager.DaysPerTurn + 1; i++)
            {
                bool boundaryReached = sim.AdvanceDay();
                sim.AdvanceCountryDayTick(_countryId);
                if (boundaryReached) { sim.AdvanceTurn(noDecisions); break; }
            }

            SetEnumField(controller, "_consolidatedTab", "PolicyLaws");
            SetEnumField(controller, "_policyLawsCategory", "LaborMarket");
            ResetScrolls(controller);
            // RequestSelection, not the chip toggle (2026-08-25): a toggle queued under an
            // exclusive screen stays pending and composes with the next one - see the panel's own
            // note on the 95b collapse. The driver states what it wants, absolutely.
            StatTracePanel.RequestSelection(StatNodeId.Approval);
            yield return Settle();
            if (StatTracePanel.SelectedStat != StatNodeId.Approval)
            {
                Debug.LogError("SHOT: 93_trace_approval - the trace panel is NOT open on Approval; this capture would be misnamed.");
            }
            yield return Capture("93_trace_approval");

            StatTracePanel.RequestSelection(StatNodeId.ConsumerConfidence);
            SetEnumField(controller, "_consolidatedTab", "Budget");
            SetEnumField(controller, "_budgetProcessCategory", "Tax");
            ResetScrolls(controller);
            yield return Settle();
            if (StatTracePanel.SelectedStat != StatNodeId.ConsumerConfidence)
            {
                Debug.LogError("SHOT: 93b_trace_confidence - the trace panel is NOT open on ConsumerConfidence; this capture would be misnamed.");
            }
            yield return Capture("93b_trace_confidence");

            StatTracePanel.RequestSelection(null);
            yield return Settle();

            // Step 2's third section (2026-08-25): the debt trace, on the Budget tab's own chip row
            // (Fiscal's ranking puts Debt-to-GDP first - every tax and spending lever targets it).
            // The warm-up's periods are closed and rich (the FRF's first-turn reaction, interest,
            // erosion all non-trivial on the USA's stock), so this is a real period, not an empty
            // one. Assert-own-name: the panel must be OPEN ON DEBT before the shutter, else the
            // capture would be a lie under its own filename.
            StatTracePanel.RequestSelection(StatNodeId.DebtToGdp);
            yield return Settle();
            if (StatTracePanel.SelectedStat != StatNodeId.DebtToGdp)
            {
                Debug.LogError("SHOT: 93c_trace_debt - the trace panel is NOT open on Debt-to-GDP; this capture would be misnamed.");
            }
            DebtAttribution debtLedgerPinned = player.FiscalLedgerLastPeriod;
            Debug.Log($"SHOT: 93c debt trace pinned - closed={(debtLedgerPinned?.Closed ?? false)}, days={debtLedgerPinned?.DaysRecorded ?? -1}, " +
                      $"events={debtLedgerPinned?.Events.Count ?? -1}, terms={debtLedgerPinned?.TermSum ?? float.NaN:F2}, clamp={debtLedgerPinned?.ClampLoss ?? float.NaN:F4}.");
            yield return Capture("93c_trace_debt");
            StatTracePanel.RequestSelection(null);
            yield return Settle();

            // --- E1c (Step 3): THE SCENARIO SLICE — entry, an objective in progress, and the
            // verdict. All three through the controller's OWN methods (StartScenario /
            // CheckScenarioObjectives / the verdict screen's own gate), so the driver exercises the
            // real path minus only the click — the election-reveal idiom exactly. The verdict needs a
            // REACHED end state, which is why it is pinned by DOING: the scenario's own end turn is
            // forced, then the real evaluator decides the outcome.
            {
                ScenarioDefinition slice = ScenarioLibrary.All.Count > 0 ? ScenarioLibrary.All[0] : null;
                if (slice == null)
                {
                    Debug.LogWarning("SHOT: no scenario in the library - the Step 3 captures stay unpinned.");
                }
                else
                {
                    // Entry: the scenario's own start, applied to THIS run's world. The player country
                    // is already selected, so this is the deltas-plus-progress half of StartScenario,
                    // reached through the controller rather than reimplemented.
                    InvokeOneArg(controller, "StartScenario", slice);
                    SetEnumField(controller, "_consolidatedTab", "Statistics");
                    ResetScrolls(controller);
                    yield return Settle();
                    yield return Capture("94_scenario_entry");

                    // An objective in progress: advance a few real turns so the evaluator has run at
                    // real boundaries and the ledger/history behind the epilogue is populated.
                    for (int t = 0; t < 3; t++)
                    {
                        for (int d = 0; d < SimulationManager.DaysPerTurn; d++)
                        {
                            if (sim.AdvanceDay()) { sim.AdvanceTurn(noDecisions); }
                            sim.AdvanceCountryDayTick(_countryId);
                        }
                        InvokeNoArg(controller, "CheckScenarioObjectives");
                    }

                    SetEnumField(controller, "_consolidatedTab", "PolicyLaws");
                    SetEnumField(controller, "_policyLawsCategory", "LaborMarket");
                    ResetScrolls(controller);
                    StatTracePanel.RequestSelection(StatNodeId.Approval);
                    yield return Settle();
                    if (StatTracePanel.SelectedStat != StatNodeId.Approval)
                    {
                        Debug.LogError("SHOT: 94b_scenario_in_progress - the trace panel is NOT open on Approval; this capture would be misnamed.");
                    }
                    yield return Capture("94b_scenario_in_progress");
                    StatTracePanel.RequestSelection(null);

                    // The verdict: run to the scenario's own end turn so the REAL evaluator resolves
                    // it. ⚠ BOUNDED BY THE TURNS ACTUALLY NEEDED, not by MaxStateSearchDays - the
                    // shared 4-year bound is sized for a state SEARCH, and this loop is a known
                    // distance: the scenario starts mid-run here, so the flat bound expired at turn 11
                    // of 12 and the first capture pass pinned the DASHBOARD under a name that promised
                    // a verdict. A capture whose name asserts a state it does not show is the failure
                    // this project's capture discipline exists to catch; the bound is still finite.
                    int turnsNeeded = Mathf.Max(0, slice.EndTurn - sim.CurrentTurn) + 2;
                    int guard = 0;
                    int dayBound = turnsNeeded * SimulationManager.DaysPerTurn;
                    while (sim.CurrentTurn < slice.EndTurn && guard < dayBound)
                    {
                        if (sim.AdvanceDay()) { sim.AdvanceTurn(noDecisions); }
                        sim.AdvanceCountryDayTick(_countryId);
                        guard++;
                    }

                    InvokeNoArg(controller, "CheckScenarioObjectives");
                    yield return Settle();
                    yield return Capture("94c_scenario_verdict");

                    bool verdictOnScreen = (bool)controller.GetType()
                        .GetField("_scenarioVerdictPending", BindingFlags.Instance | BindingFlags.NonPublic)
                        .GetValue(controller);
                    if (verdictOnScreen)
                    {
                        Debug.Log($"SHOT: scenario verdict pinned at turn {sim.CurrentTurn} (end turn {slice.EndTurn}).");
                    }
                    else
                    {
                        Debug.LogError($"SHOT: 94c shows NO verdict - reached turn {sim.CurrentTurn} of {slice.EndTurn}. " +
                                       "The capture is named for a state it does not show; fix the bound, do not accept the image.");
                    }
                }
            }

            // --- E1d (Italy Debt Crisis): the SECOND shipped scenario's own captures -
            // ScenarioLibrary.All[1], since [0] is "Inherit the Fund" above and StartScenario
            // takes an explicit definition rather than an index, so both get their own real
            // entry through the controller. The in-progress capture is deliberately NOT pinned
            // near turn 1: a Sustained objective (keep_the_room, RequiredTurns=10) shows nothing
            // meaningful that early - the streak needs real turns to build, so this advances into
            // it before capturing, the same "a boring ledger validates nothing" reasoning the
            // trace-panel captures already established.
            {
                ScenarioDefinition italySlice = ScenarioLibrary.All.Count > 1 ? ScenarioLibrary.All[1] : null;
                if (italySlice == null)
                {
                    Debug.LogWarning("SHOT: no second scenario in the library - the Italy Debt Crisis captures stay unpinned.");
                }
                else
                {
                    InvokeOneArg(controller, "StartScenario", italySlice);
                    SetEnumField(controller, "_consolidatedTab", "Statistics");
                    ResetScrolls(controller);
                    yield return Settle();
                    yield return Capture("95_italydebt_entry");

                    // A REAL consolidation line (the measured -20% package, per the pre-authoring
                    // report) so the in-progress and verdict captures show the scenario actually
                    // being played, not a no-policy line drifting - the "post-decision,
                    // post-parliament-cost" pinned-capture pattern applied to fiscal policy.
                    // Applied on the FIRST turn boundary only (matching ItalyDebtCrisisSliceDiagnostic's
                    // own line), then every later boundary reverts to noDecisions - the SpendingLine
                    // change is persistent once applied (ApplySpendingLineChanges mutates the line's
                    // Amount directly), so it needs no re-application.
                    var italyConsolidation = new PolicyDecision();
                    italyConsolidation.SpendingLineChanges[SpendingCategory.InfrastructureAndDevelopment] = -20f;
                    italyConsolidation.SpendingLineChanges[SpendingCategory.PublicServices] = -20f;
                    italyConsolidation.SpendingLineChanges[SpendingCategory.Administration] = -20f;
                    var italyDecisions = new Dictionary<CountryId, PolicyDecision> { [_countryId] = italyConsolidation };
                    bool italyConsolidationApplied = false;

                    // BOUNDED (2026-08-25), not the fixed eleven turns this block shipped with: the
                    // recorded EndTurn-as-absolute-turn artifact (ScenarioEvaluator compares EndTurn
                    // to the shared session clock, and this block starts wherever the blocks before
                    // it left that clock) had drifted far enough that eleven turns ran PAST Italy's
                    // EndTurn - the verdict screen was up, exclusive, for both "in-progress" captures,
                    // and the debt trace's own assert-own-name is what caught it (the panel could not
                    // open under an exclusive screen). Stopping two turns short of EndTurn wherever
                    // the clock stands keeps the in-progress state genuinely in progress without
                    // reordering any block. The verdict capture below still runs to EndTurn itself.
                    int italyInProgressTurns = Mathf.Max(0, italySlice.EndTurn - sim.CurrentTurn - 2);
                    if (italyInProgressTurns == 0)
                    {
                        Debug.LogWarning($"SHOT: the session clock (turn {sim.CurrentTurn}) is already within two turns of Italy's EndTurn ({italySlice.EndTurn}) - the in-progress state is unreachable from here; 95b/95d will not show it.");
                    }
                    for (int t = 0; t < italyInProgressTurns; t++)
                    {
                        for (int d = 0; d < SimulationManager.DaysPerTurn; d++)
                        {
                            if (sim.AdvanceDay())
                            {
                                sim.AdvanceTurn(italyConsolidationApplied ? noDecisions : italyDecisions);
                                italyConsolidationApplied = true;
                            }
                            sim.AdvanceCountryDayTick(_countryId);
                        }
                        InvokeNoArg(controller, "CheckScenarioObjectives");
                    }

                    SetEnumField(controller, "_consolidatedTab", "PolicyLaws");
                    SetEnumField(controller, "_policyLawsCategory", "LaborMarket");
                    ResetScrolls(controller);
                    StatTracePanel.RequestSelection(StatNodeId.Approval);
                    yield return Settle();
                    // Assert-own-name for the in-progress capture, added with the bound above: the
                    // verdict must NOT be pending, or "in progress" is a lie under its own filename
                    // - and the approval trace it clicks for must actually be OPEN (the toggle
                    // collapse this block carried since Step 3; see StatTracePanel.RequestSelection).
                    FieldInfo verdictPendingProbe = controller.GetType().GetField("_scenarioVerdictPending", BindingFlags.Instance | BindingFlags.NonPublic);
                    if (verdictPendingProbe?.GetValue(controller) is bool verdictUp && verdictUp)
                    {
                        Debug.LogError($"SHOT: 95b_italydebt_in_progress - the scenario verdict is already pending at turn {sim.CurrentTurn} (EndTurn {italySlice.EndTurn}); this capture shows the verdict, not the scenario in progress.");
                    }
                    if (StatTracePanel.SelectedStat != StatNodeId.Approval)
                    {
                        Debug.LogError("SHOT: 95b_italydebt_in_progress - the trace panel is NOT open on Approval; this capture would be misnamed.");
                    }
                    yield return Capture("95b_italydebt_in_progress");
                    StatTracePanel.RequestSelection(null);

                    // Step 2's third section (2026-08-25): the debt trace pinned on the state that
                    // FIRED its trigger - Italy mid-scenario, eleven periods in, the −20% package
                    // applied, the stock still well past the comfort anchor: erosion, the lag, the
                    // reaction's give-back and the primary balance all live at once. On the Budget
                    // tab's chip row, where Debt-to-GDP ranks first. Assert-own-name on the panel
                    // AND on the ledger: a closed period with 365 observed days, or the capture is
                    // not showing what its name says.
                    SetEnumField(controller, "_consolidatedTab", "Budget");
                    SetEnumField(controller, "_budgetProcessCategory", "Tax");
                    ResetScrolls(controller);
                    StatTracePanel.RequestSelection(StatNodeId.DebtToGdp);
                    yield return Settle();
                    if (StatTracePanel.SelectedStat != StatNodeId.DebtToGdp)
                    {
                        Debug.LogError("SHOT: 95d_italydebt_trace_debt - the trace panel is NOT open on Debt-to-GDP; this capture would be misnamed.");
                    }
                    DebtAttribution italyDebtLedger = sim.World?.GetCountry(italySlice.Country)?.FiscalLedgerLastPeriod;
                    if (italyDebtLedger == null || !italyDebtLedger.Closed || italyDebtLedger.DaysRecorded != SimulationManager.DaysPerTurn)
                    {
                        Debug.LogError($"SHOT: 95d_italydebt_trace_debt - Italy's closed debt ledger is not a full period (closed={(italyDebtLedger?.Closed ?? false)}, days={italyDebtLedger?.DaysRecorded ?? -1}); the capture would not show the state it claims.");
                    }
                    else
                    {
                        Debug.Log($"SHOT: 95d Italy debt trace pinned - {italyDebtLedger.DebtAtPeriodOpen:F1}->{italyDebtLedger.DebtAtClose:F1} (ratio {italyDebtLedger.RatioAtPeriodOpen:F1}->{italyDebtLedger.RatioAtClose:F1}%), " +
                                  $"primary={italyDebtLedger.PrimaryBalanceEffect:F1} frf={italyDebtLedger.FiscalReactionEffect:F1} intIss={italyDebtLedger.InterestAtIssuance:F1} lag={italyDebtLedger.RateLagEffect:F1} erosion={italyDebtLedger.Erosion:F1} clamp={italyDebtLedger.ClampLoss:F4} events={italyDebtLedger.Events.Count}, stance x{italyDebtLedger.FiscalReactionMultiplier:F3}.");
                    }
                    yield return Capture("95d_italydebt_trace_debt");
                    StatTracePanel.RequestSelection(null);
                    yield return Settle();

                    int turnsNeeded = Mathf.Max(0, italySlice.EndTurn - sim.CurrentTurn) + 2;
                    int guard = 0;
                    int dayBound = turnsNeeded * SimulationManager.DaysPerTurn;
                    while (sim.CurrentTurn < italySlice.EndTurn && guard < dayBound)
                    {
                        if (sim.AdvanceDay()) { sim.AdvanceTurn(noDecisions); }
                        sim.AdvanceCountryDayTick(_countryId);
                        guard++;
                    }

                    InvokeNoArg(controller, "CheckScenarioObjectives");
                    yield return Settle();
                    yield return Capture("95c_italydebt_verdict");

                    FieldInfo verdictPendingField = controller.GetType()
                        .GetField("_scenarioVerdictPending", BindingFlags.Instance | BindingFlags.NonPublic);
                    bool italyVerdictOnScreen = (bool)verdictPendingField.GetValue(controller);
                    if (italyVerdictOnScreen)
                    {
                        Debug.Log($"SHOT: Italy Debt Crisis verdict pinned at turn {sim.CurrentTurn} (end turn {italySlice.EndTurn}).");
                    }
                    else
                    {
                        Debug.LogError($"SHOT: 95c shows NO verdict - reached turn {sim.CurrentTurn} of {italySlice.EndTurn}. " +
                                       "The capture is named for a state it does not show; fix the bound, do not accept the image.");
                    }

                    // ⚠ PLAYTEST FIX (2026-08-18): Italy is the LAST scenario in ScenarioLibrary.All, so
                    // unlike "Inherit the Fund" - whose own dangling verdict gets silently cleared by
                    // StartScenario's `_scenarioVerdictPending = false` the moment Italy's block starts -
                    // nothing clears Italy's. Left set, it pinned every later section under the WRONG
                    // name: found by actually looking at 89_signing_document post-fix and seeing the
                    // Italy verdict screen again, not a signing ceremony. Not GameController.
                    // DismissScenarioVerdict() - that method sets _isGameOver = true permanently (the
                    // real player's only dismissal path, correctly, since a real ceremony ending a
                    // scenario ends the run) - which would corrupt every capture after it (bills would
                    // stop resolving, the election/game-over sections would run against an
                    // already-game-over state). Cleared directly instead, the same "reach into private
                    // state by reflection rather than widen production's public surface" idiom this
                    // whole file already uses - this is driver cleanup, not a dismissal a player took.
                    verdictPendingField.SetValue(controller, false);
                }
            }

            // --- E2. THE SIGNING CEREMONY (Canvas screen 2) — pinned via the controller's own queue
            // method (ceremonies fire only from play's day tick, never from harness sim-advances, so
            // this pass stays clean; TriggerSigningForNewestDivision fills the same queue the day
            // tick fills). SignPendingDivision is the SIGN button's own method — the reflection call
            // exits the screen exactly like a click, the selector's idiom.
            InvokeNoArg(controller, "TriggerSigningForNewestDivision");
            yield return WaitForCanvasSettle(controller, wantActive: true);
            yield return Settle();
            yield return Capture("89_signing_document");
            RecordCanvasTextAssert("89_signing_document", controller);

            InvokeNoArg(controller, "SignPendingDivision");
            yield return null;
            yield return null;
            yield return Capture("89a_signing_yielding");

            yield return WaitForCanvasSettle(controller, wantActive: false);
            yield return Settle();
            yield return Capture("89b_signing_restored");

            // --- E2b. THE SIGNING CEREMONY, REJECTED FORM (playtest fix, 2026-08-18) — the seal
            // used to drop and the button read "SIGN" for a rejected division too, a false
            // player-facing claim (nothing was enacted; there is nothing to sign). TriggerSigningFor-
            // NewestDivision only ever grabs WHATEVER bill happened to resolve last, with no
            // guarantee either way, so this injects one guaranteed-rejected DivisionRecord directly
            // (the same DivisionLog.Append every real resolution goes through) to pin the branch the
            // fix actually changed — the election win/loss pair's own precedent for a binary outcome
            // that must not go unpinned by chance.
            player.Divisions.Append("Harness Test Bill (injected for rejected-signing coverage)", sim.CurrentDate, -0.35f, passed: false);
            InvokeNoArg(controller, "TriggerSigningForNewestDivision");
            yield return WaitForCanvasSettle(controller, wantActive: true);
            yield return Settle();
            yield return Capture("89c_signing_rejected");
            RecordCanvasTextAssert("89c_signing_rejected", controller);

            InvokeNoArg(controller, "SignPendingDivision");
            yield return WaitForCanvasSettle(controller, wantActive: false);
            yield return Settle();

            // --- F. THE ELECTION REVEAL, BOTH FORMS, AND GAME OVER — through the controller's own
            // path. The sim never shows a reveal because CheckElection is the CONTROLLER's post-turn
            // call (the same driver-artifact class as the daily arm calls: the first state pass
            // recorded "an election resolving leaves no observable UI state", and the truth was that
            // the DRIVER's turn path never ran the method that shows it). The WIN form first: a win's
            // dismissal sets no state and returns to the dashboard, so the same run can then search
            // to the NEXT election and pin the loss chain — reveal in its loss form, then game over
            // on dismissal. This closes "the WIN form stays unpinned, stated rather than implied".
            if (AdvanceToElectionTurn(sim, noDecisions))
            {
                player.State.ApprovalRating = 60f;
                InvokeNoArg(controller, "CheckElection");
                yield return Settle();
                yield return Capture("88w_election_reveal_win");

                InvokeNoArg(controller, "DismissElectionResult");
                yield return Settle();
            }
            else
            {
                Debug.LogWarning($"SHOT: no election turn within {MaxStateSearchDays} days - the WIN reveal stays unpinned for {_countryId}.");
            }

            if (AdvanceToElectionTurn(sim, noDecisions))
            {
                player.State.ApprovalRating = 5f;
                InvokeNoArg(controller, "CheckElection");
                yield return Settle();
                yield return Capture("88a_election_reveal_loss");

                InvokeNoArg(controller, "DismissElectionResult");
                yield return Settle();
                yield return Capture("88b_game_over");
            }
            else
            {
                Debug.LogWarning($"SHOT: no election turn within {MaxStateSearchDays} days - the loss reveal and game-over states stay unpinned for {_countryId}.");
            }
        }

        /// <summary>
        /// Advances sim days (turns through the driver's own no-decisions path, each day ticked via
        /// AdvanceCountryDayTick like play does) until CurrentTurn is an election turn, bounded by
        /// MaxStateSearchDays. False when the bound ran out — the caller states the unpinned state.
        /// </summary>
        private bool AdvanceToElectionTurn(SimulationManager sim, Dictionary<CountryId, PolicyDecision> noDecisions)
        {
            for (int days = 0; days < MaxStateSearchDays; days++)
            {
                if (sim.AdvanceDay())
                {
                    sim.AdvanceTurn(noDecisions);
                    if (ElectionSystem.IsElectionTurn(sim.CurrentTurn))
                    {
                        return true;
                    }
                }

                sim.AdvanceCountryDayTick(_countryId);
            }

            return false;
        }

        private static void InvokeNoArg(object target, string method)
        {
            MethodInfo m = target.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic);
            if (m == null)
            {
                Debug.LogError($"SHOT: method {method} not found - the state its caller pins will be WRONG, not missing.");
                return;
            }

            m.Invoke(target, null);
        }

        /// <summary>The one-argument twin, same contract: a missing method is an ERROR, because the
        /// state its caller meant to pin would otherwise be silently wrong rather than absent.</summary>
        private static void InvokeOneArg(object target, string method, object argument)
        {
            MethodInfo m = target.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic);
            if (m == null)
            {
                Debug.LogError($"SHOT: method {method} not found - the state its caller pins will be WRONG, not missing.");
                return;
            }

            m.Invoke(target, new[] { argument });
        }

        private static TaxLine FindTaxLine(Country country, TaxType type)
        {
            foreach (TaxLine line in country.TaxLines)
            {
                if (line.Type == type) { return line; }
            }

            return country.TaxLines[0];
        }

        private static void ResetScrolls(object target) => SetScrolls(target, 0f);

        private static void ScrollBy(object target, float y) => SetScrolls(target, y);

        private static void SetScrolls(object target, float y)
        {
            foreach (FieldInfo f in ScrollFields(target))
            {
                f.SetValue(target, new Vector2(0f, y));
            }
        }

        private static bool SetEnumField(object target, string field, string value)
        {
            FieldInfo f = target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic);
            if (f == null)
            {
                Debug.LogError($"SHOT: field {field} not found - skipping rather than capturing the wrong screen.");
                return false;
            }

            // Enum.Parse throws on a name the enum does not carry, which is what should happen: a
            // renamed sub-category must fail loudly here rather than quietly capturing whichever screen
            // was already up. Every caller checks the return and skips.
            try
            {
                f.SetValue(target, System.Enum.Parse(f.FieldType, value));
                return true;
            }
            catch (System.ArgumentException)
            {
                Debug.LogError($"SHOT: '{value}' is not a member of {f.FieldType.Name} - skipping.");
                return false;
            }
        }
    }
}
