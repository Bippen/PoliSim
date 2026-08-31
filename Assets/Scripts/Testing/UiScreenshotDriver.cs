using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using PoliSim.Data;
using PoliSim.Elections;
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

        /// <summary>The film seed (2026-08-28, the ratified candidate): every <see cref="SimulationRandom"/> stream made reproducible for a capture run, so two films of one code are byte-stable run-to-run. 777 is the trajectory baselines' own standing seed.</summary>
        private const int FilmSeed = 777;

        public string OutputDirectory = DefaultOutputDirectory;
        public string Label = "run";
        /// <summary>Which country to play as, set from `-shotcountry=` (default USA — the only country any set contained before 2026-08-12). Parsed against CountryId in Start and FAILS the run on a bad name: a typo must not silently capture the default country under the requested country's label.</summary>
        public string Country = "USA";

        /// <summary>The parsed <see cref="Country"/>, held so the warm-up's publication check and the held-state pass read the country actually being captured rather than a hardcoded USA.</summary>
        private CountryId _countryId = CountryId.USA;

        /// <summary>Set by `-shotstates`: run the state-pinning pass (cabinet, budget pause, decision search, pending bills) after the main sweep. Off by default so ordinary chrome runs stay fast and their sets comparable with history.</summary>
        public bool PinStates;

        /// <summary>Set by `-shotsaves` (R-D4, the clear-out kickoff of 2026-08-28): instead of the sweep, stage the
        /// playtest saves - one per felt verdict in MISSING_PREREQUISITES.md §P - through the REAL save service
        /// into the real saves directory, each state filmed once as proof, so §P is load-play-judge rather than
        /// set-up-first. USA stages the Trade-bill and dense-mid-game saves; Sweden the Riksbank-B one.</summary>
        public bool StageSaves;

        /// <summary>Set by `-shotlocale=` (e.g. "en-US"): overrides the thread culture before anything draws, so number/date formatting can be captured in a locale other than the OS's. Empty = OS culture, which is what every set before 2026-08-12 rendered in (sv-SE on this machine — the decimal-comma set).</summary>
        public string Locale = "";

        /// <summary>C-C7 follow-up / trap 2: the width the caller ASKED for, so every capture can assert
        /// it got it. Zero means no size was requested and nothing is checked - see the mismatch guard in
        /// Capture, and the run it was armed for (a 1280 pass that silently filmed at the 1600 default
        /// and reported 77 captured, 0 failed).</summary>
        public int ExpectedWidth;

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

        // The log-error fold (ruling 1, 2026-08-25), the driver's own copy of CheckExit's - the
        // driver lives in the runtime assembly and cannot reference the Editor-only CheckExit, so it
        // subscribes itself. The sweep this ruling prompted found the driver exits 0 with any of ~18
        // uncounted LogError lines in the log (assert-own-name misses, reflection failures) AND with
        // an ATTRIB raised by the simulation it advances - "clean 1600 runs" carried the 2050-12-26
        // approval audit for exactly this reason (CLAUDE.md). Now every Error/Exception/Assert during
        // the run, its own or the simulation's, folds into the exit code. The two election-forcing
        // raw approval writes that caused that specific ATTRIB are recorded below, so a clean run has
        // nothing to fold.
        private int _loggedErrors;
        private string _firstLoggedError;

        private void OnRunLog(string condition, string stackTrace, LogType type)
        {
            if (type != LogType.Error && type != LogType.Exception && type != LogType.Assert)
            {
                return;
            }

            _loggedErrors++;
            if (_firstLoggedError == null)
            {
                _firstLoggedError = condition;
            }
        }

        private IEnumerator Start()
        {
            Application.logMessageReceived += OnRunLog;

            // S-20: arm the capture-identity token for the whole run, and PRINT THE ENUMERATION - a trap
            // that silently knew fewer surfaces than it claims reads exactly like a clean run.
            CaptureIdentity.Armed = true;
            CaptureIdentity.Expected = "imgui";
            Debug.Log("SHOT: capture-identity armed. Surfaces the token palette knows - "
                      + string.Join(", ", CaptureIdentity.Surfaces)
                      + ". Every capture claims one, and the written frame must carry its token.");

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

            // THE FILM SEED (2026-08-28, the flagged candidate ratified - COMPLETED.md §45's gate
            // paragraph: two films of one code differed run to run only by the Fed-chair candidate
            // draw and the cursor). Every stream is seeded here, before the game advances a single
            // day, so the chair draw - and every other SimulationRandom consumer the warm-up
            // touches - replays the same sequence every run: films of one code are byte-stable, and
            // a rule-15 diff measures the code, not the clock. Family-to-family this is one
            // deliberate discontinuity: the first seeded family's random-dependent surfaces
            // (candidate names, event timing, publication noise) differ once from every unseeded
            // family before it, and never again between themselves.
            SimulationRandom.Seed(FilmSeed);
            Debug.Log($"SHOT: SimulationRandom seeded ({FilmSeed}) - the film is deterministic run-to-run.");

            // THE CURSOR, PARKED (the candidate's second half) - see ParkCursor's own doc.
            ParkCursor();

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
            Claim("selector");
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
            // v3.0 Phase B (consolidation rider, 2026-08-28): the surface beneath the scrim on this
            // frame and the next is SCREEN 0 (the Desk) - the game lands there now - so the guard
            // results filed under 01a_selector_yielding and 01b_running_strip are the Desk's, not the
            // selector's or the strip's. Attribute a text overflow or containment escape read under
            // these names to the Desk on first reading (the Desk's own frames are 01c-01f).
            // S-20: still the SELECTOR - IMGUI is veiling it with a scrim, not replacing it.
            Claim("selector");
            yield return Capture("01a_selector_yielding");

            yield return WaitForCanvasSettle(controller, wantActive: false);
            yield return Settle();

            // ⚠ THE ONE GUARANTEED RUNNING-STATE CAPTURE, taken before the warm-up. The 2026-08-12 run
            // showed every post-warm-up capture in the HELD state — the preliminary-release stop lands
            // on an election eve, so the fed-chair pause is live for the whole main set (and was in
            // every earlier set too). Turn 0 is the one moment guaranteed unpaused: no election eve,
            // no pending decisions, nothing rolled yet. Without this shot the status line's RUNNING
            // form exists in no capture this harness can produce. (Since v3.0 Phase B the frame under
            // this name is Screen 0 in its RUNNING state - see the note above 01a - and its guard
            // results are the Desk's.)
            yield return Capture("01b_running_strip");

            // UI v3.0 Phase B: the game lands on Screen 0 (The Desk) - the one guaranteed RUNNING
            // capture of the stage, before the warm-up: the lamp green, the speed faces live, the
            // ledger without a closed period, the rail without a spine (board 1m, D2).
            yield return Capture("01c_desk");
            AssertDeskState(controller, "01c_desk");

            AdvanceDays(controller, _countryId);

            // R-D4: the playtest saves are staged on the warmed-up game BEFORE the sweep's own drafts
            // (diverged SWF weights, drafted spending lines) go in - a playtester should open a clean
            // book, not the harness's amber. The run ends here in that mode.
            if (StageSaves)
            {
                yield return StagePlaytestSaves(controller);
                Debug.Log($"SHOT: playtest saves staged, {_captured} proof capture(s), {_failed} failed.");
                Finish(_failed == 0 && _loggedErrors == 0 ? 0 : 1);
                yield break;
            }

            // UI v3.0 Phase A, Phase 3: the instrument ladder instead of the sweep - the run ends here.
            if (Ladder)
            {
                yield return CaptureInstrumentLadder(controller);
                yield break;
            }

            // W-E1: Campaign HQ instead of the sweep - the run ends here.
            if (ElectionNightBoard)
            {
                yield return CaptureElectionNight(controller);
                yield break;
            }

            if (CampaignHq)
            {
                yield return CaptureCampaignHq(controller);
                yield break;
            }

            // C-C10 (P-G2): the impact ledger instead of the sweep - the run ends here.
            if (ImpactLedger)
            {
                yield return CaptureImpactLedger(controller);
                yield break;
            }

            DivergeSwfWeights(controller);
            DraftSpendingLines(controller);
            yield return Settle();

            // UI v3.0 Phase B: Screen 0 in the warmed-up game (HELD - the banner above the masthead,
            // the lamp amber, the speed faces disabled), then the two conditional states the board
            // draws beside it: the event card filled with an authored event from the pool, and the
            // game-over stamp with the reason string the game itself prints. Each staged state is
            // restored before the next capture, and the Desk is left before the tab sweep - a tab
            // set by field alone would otherwise film the stage under a document's name.
            SetPrivateField(controller, "_onDesk", true);
            yield return Settle();
            yield return Capture("01d_desk_held");
            AssertDeskState(controller, "01d_desk_held");
            yield return CaptureDeskEventCard(controller);
            yield return CaptureDeskGameOver(controller);
            SetPrivateField(controller, "_onDesk", false);
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

                    // R-SP5: the map's label separation, measured by the renderer on the capture frame.
                    if (stem == "02b_statistics_international")
                    {
                        AssertMapLabelSeparation(controller, stem);
                    }

                    // The v3.0 fold-pair sweep (V3-R4: the other fold state's guards on every screen,
                    // three screens on film as `_folded` / `_open`) lived here until v3.1 R-E1 made ONE
                    // FRAME the only state; it was deleted with the OPEN branch in v3.1 Phase B.

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

                    // Item 6 (2026-08-25): the law detail pane's EXPECTED EFFECTS band sits between
                    // the generic stops - the pane's own scroll CLAMPS to its max under both 900px
                    // and 2200px (its content is short), and max scroll shows only the tail
                    // (citation/cost/direction/button), cutting the effects lines just above the
                    // fold at 2560 and entirely at 1600. One bespoke stop (the 85f_bill_tax_rows
                    // idiom) scrolls ONLY the detail pane to a mid position - 0.3 x screen height,
                    // calibrated by eyes at both capture sizes against the default-selected law -
                    // so the derived-effects deliverable exists in a capture.
                    if (stem.EndsWith("_policylaws_laws"))
                    {
                        ResetScrolls(controller);
                        FieldInfo detailScroll = controller.GetType().GetField("_lawDetailScrollPosition", BindingFlags.Instance | BindingFlags.NonPublic);
                        if (detailScroll == null)
                        {
                            Debug.LogError("SHOT: _lawDetailScrollPosition not found - the 06g expected-effects capture is MISSING, not clean.");
                        }
                        else
                        {
                            detailScroll.SetValue(controller, new Vector2(0f, Screen.height * 0.3f));
                            yield return Settle();
                            yield return Capture("06g_laws_expected_effects");
                            ResetScrolls(controller);
                        }
                    }
                }

                SetEnumField(controller, sub.Key, sub.Value[0]);
            }

            yield return CaptureFilmGaps(controller);
            yield return CaptureHeldState(controller);
            yield return CaptureSavesMenu(controller);

            if (PinStates)
            {
                yield return CaptureStatePins(controller);
            }

            Debug.Log($"SHOT: capture-identity - {_identityAsserts} capture(s) proved they show the surface they claim.");
            Debug.Log($"SHOT: done, {_captured} captured, {_failed} failed.");

            int overflows = ReportOverflows();
            Debug.Log($"SHOT: {overflows} text overflow(s) recorded.");

            int escapes = ReportContainmentEscapes();
            Debug.Log($"SHOT: {escapes} containment escape(s) recorded.");

            Debug.Log($"SHOT: {_canvasTextViolations} canvas text violation(s) recorded across {_canvasTextAsserts} assert(s).");

            // Ruling 1's fold: a red line nothing counted is still a failure. The four counters
            // above are the driver's own asserts; _loggedErrors catches everything else - the ~18
            // uncounted LogError sites and any ATTRIB the simulation raised while this drove it.
            bool clean = _failed == 0 && overflows == 0 && escapes == 0 && _canvasTextViolations == 0;
            if (_loggedErrors > 0)
            {
                Debug.Log(clean
                    ? $"SHOT: FOLD - the run's own counters were clean but {_loggedErrors} error(s) were logged during it; exiting nonzero. First: \"{(_firstLoggedError != null && _firstLoggedError.Length > 200 ? _firstLoggedError.Substring(0, 200) + "…" : _firstLoggedError)}\"."
                    : $"SHOT: {_loggedErrors} error(s) logged during the run; the failure is already reflected.");
            }

            Finish(clean && _loggedErrors == 0 ? 0 : 1);
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

                Application.logMessageReceived -= OnRunLog;
            }
        }

        /// <summary>
        /// R-C6 (the continuation kickoff, 2026-08-28): the surfaces §V listed as verified by code alone,
        /// put on film in the MAIN sweep so every omni set carries them. Every pin here is UI-STATE ONLY
        /// - a capture state may pose the UI, never move the model: no day advances, no sim write.
        /// (a) The trace panel open on Policy/Laws for each of its three sections (approval, confidence,
        ///     the fiscal chain) through the driver's absolute RequestSelection, assert-own-name before
        ///     the shutter (the 93-series idiom, which lives behind -shotstates and so was in no omni set).
        /// (b) A Policy Web node selected - one policy node, one stat node - because the derived/declared
        ///     idiom and the stat chords draw only on a click; the two private selection fields are set
        ///     the way a click sets them (one pinned, the other cleared).
        /// (c) The signing ceremony's ENTRANCE (§A.13 rows 4 and 6): staged the way the rejected-form
        ///     capture stages a ceremony - one DivisionRecord appended through DivisionLog.Append, the
        ///     queue the day tick fills, then TriggerSigningForNewestDivision - and filmed twice: two
        ///     frames after the canvas goes live (the document rising, the SIGN button still invisible)
        ///     and again once the seam reports the entrance settled (the button faded in). Presence is
        ///     what the mid-entrance frame pins, not a pixel value (the yielding-state idiom). Runs LAST
        ///     in this pass so the appended record is not on the Parliament tab's division list in the
        ///     tab captures; the record stays in the run's log, named as the harness's.
        /// </summary>
        private IEnumerator CaptureFilmGaps(GameController controller)
        {
            // (a) the trace panel, three sections
            SetEnumField(controller, "_consolidatedTab", "PolicyLaws");
            SetEnumField(controller, "_policyLawsCategory", "LaborMarket");
            foreach ((StatNodeId stat, string stem) in new[]
            {
                (StatNodeId.Approval, "06h_policylaws_trace_approval"),
                (StatNodeId.ConsumerConfidence, "06i_policylaws_trace_confidence"),
                (StatNodeId.DebtToGdp, "06j_policylaws_trace_debt")
            })
            {
                ResetScrolls(controller);
                StatTracePanel.RequestSelection(stat);
                yield return Settle();
                if (StatTracePanel.SelectedStat != stat)
                {
                    Debug.LogError($"SHOT: {stem} - the trace panel is NOT open on {stat}; this capture would be misnamed.");
                }
                yield return Capture(stem);
            }
            StatTracePanel.RequestSelection(null);
            yield return Settle();

            // (b) a Policy Web node selected - a policy node, then a stat node
            FieldInfo policyNodeField = controller.GetType().GetField("_selectedPolicyWebPolicyNode", BindingFlags.Instance | BindingFlags.NonPublic);
            FieldInfo statNodeField = controller.GetType().GetField("_selectedPolicyWebStatNode", BindingFlags.Instance | BindingFlags.NonPublic);
            if (policyNodeField == null || statNodeField == null)
            {
                Debug.LogError("SHOT: the Policy Web selection fields were not found - the 06k/06l node captures are MISSING, not clean.");
            }
            else
            {
                SetEnumField(controller, "_policyLawsCategory", "PolicyWeb");

                // C-C3 (P-F1): the finding names THREE states - rest, focused, restored - so all three
                // are filmed under their own names. Rest and restored reach the same code path two
                // different ways (never focused / focused then released), and filming both is the
                // point: a focus mode that cannot get back is not a focus mode.
                policyNodeField.SetValue(controller, null);
                statNodeField.SetValue(controller, null);
                ResetScrolls(controller);
                yield return Settle();
                yield return Capture("06j_policylaws_policyweb_rest");

                // Each node state twice: the diagram with the pinned node's chords, then scrolled past it
                // to the readout the click pins below - the derived / declared idiom and the ledger-term
                // chords in text, which is what the first capture of this state left below the fold.
                policyNodeField.SetValue(controller, PolicyNodeId.IncomeTax);
                statNodeField.SetValue(controller, null);
                ResetScrolls(controller);
                yield return Settle();
                yield return Capture("06k_policylaws_policyweb_node_policy");
                ScrollBy(controller, 900f);
                yield return Settle();
                yield return Capture("06k_policylaws_policyweb_node_policy_rows");

                policyNodeField.SetValue(controller, null);
                statNodeField.SetValue(controller, StatNodeId.Approval);
                ResetScrolls(controller);
                yield return Settle();
                yield return Capture("06l_policylaws_policyweb_node_stat");
                ScrollBy(controller, 900f);
                yield return Settle();
                yield return Capture("06l_policylaws_policyweb_node_stat_rows");

                // C-C3: RESTORED - focus released, the whole web back at full ink. Filmed from the
                // focused state above rather than from a fresh screen, because "it looks like rest
                // again" is the claim being tested.
                statNodeField.SetValue(controller, null);
                policyNodeField.SetValue(controller, null);
                ResetScrolls(controller);
                yield return Settle();
                yield return Capture("06m_policylaws_policyweb_restored");

                SetEnumField(controller, "_policyLawsCategory", "LaborMarket");
                ResetScrolls(controller);
                yield return Settle();
            }

            // (d) R-D2 (the clear-out kickoff, 2026-08-28): the Reset click edits the DRAFT, never the live
            // state - shown as a pair on the Trade tab. The first partner's override flag is turned on
            // at today's effective rate (what the Set Override button does; economically inert - the same
            // tariff, no retaliation excess, so no model movement even if days later advance), the draft
            // dial is moved +10 points through the same dictionary the slider writes, and the row is
            // captured; then the controller's own ResetPartnerTariffDraft runs by reflection (the
            // button's path minus the click) and the row is captured again: the draft back at the
            // standing rate beside an override still active. Both writes are undone afterwards.
            yield return CaptureTradeDraftReset(controller);

            // (c) the ceremony's entrance, staged without a day advanced
            FieldInfo simField = controller.GetType().GetField("_simulationManager", BindingFlags.Instance | BindingFlags.NonPublic);
            Country player = (simField?.GetValue(controller) as SimulationManager)?.World?.GetCountry(_countryId);
            var sim = simField?.GetValue(controller) as SimulationManager;
            if (player == null || sim == null)
            {
                Debug.LogError("SHOT: could not reach the player country - the 89d/89e entrance captures are MISSING, not clean.");
                yield break;
            }

            SetEnumField(controller, "_consolidatedTab", "Statistics");
            ResetScrolls(controller);
            yield return Settle();
            player.Divisions.Append("Harness: staged division for the entrance capture (R-C6)", sim.CurrentDate, 0.25f, passed: true);
            InvokeNoArg(controller, "TriggerSigningForNewestDivision");
            for (int i = 0; i < MaxCanvasSettleFrames && !controller.CanvasSelectorActive; i++)
            {
                yield return null;
            }
            if (!controller.CanvasSelectorActive)
            {
                Debug.LogError("SHOT: the signing canvas never went live - 89d_signing_entrance would show the dashboard; MISSING, not clean.");
            }
            yield return null;
            yield return null;
            // S-20: the entrance is mid-envelope, but the surface being photographed is still the SIGNING
            // board - IMGUI is veiling it with a scrim, not replacing it.
            Claim("signing");
            yield return Capture("89d_signing_entrance");
            RecordCanvasTextAssert("89d_signing_entrance", controller);

            yield return WaitForCanvasSettle(controller, wantActive: true);
            yield return Settle();
            Claim("signing");
            yield return Capture("89e_signing_settled");
            RecordCanvasTextAssert("89e_signing_settled", controller);

            InvokeNoArg(controller, "SignPendingDivision");
            yield return WaitForCanvasSettle(controller, wantActive: false);
            yield return Settle();
        }

        /// <summary>
        /// R-D4: the three playtest saves, through the real service into the real saves directory
        /// (`SaveGameService.DefaultSaveDirectory`), each state filmed once under this run's label.
        /// Every state is produced the way play produces it - the shared day model, the public sim API,
        /// the controller's own draft dictionaries - the state-pin idiom of CaptureStatePins.
        /// (1) `playtest_1_trade_bill_costs` (USA): a partner override drafted so the Trade bill card
        ///     shows its costs (the draft rides the save's UiDraftState.PartnerTariffInputs; the override
        ///     flag on at the effective rate, inert until a bill moves it). Navigation is not saved, so the
        ///     player opens Policy/Laws › Trade - the file name says so.
        /// (2) `playtest_2_riksbank_rate_decision` (Sweden): a rate decision drafted on the Riksbank tab
        ///     (the draft rides UiDraftState.InterestRateChangeInput), so the load opens on the surface the
        ///     felt verdict is about - option C's naming of the player-set rate. NOT a pending appointment:
        ///     on main no chair is seeded for Sweden (Riksbank-B's appointment machinery ships with item 10,
        ///     MISSING_PREREQUISITES.md §D0), so the first cut of this save - named for a pending selection
        ///     and asserted on one - correctly refused to write.
        /// (3) `playtest_3_dense_midgame` (USA): the budget-process pause, pending cabinet decisions and
        ///     a foreign-policy meeting reached by the bounded searches, then one bill of every type and
        ///     twelve enacted laws - the decision density the verdict is about.
        /// A save that cannot be staged is logged and skipped, never written half-made.
        /// </summary>
        private IEnumerator StagePlaytestSaves(GameController controller)
        {
            FieldInfo simField = controller.GetType().GetField("_simulationManager", BindingFlags.Instance | BindingFlags.NonPublic);
            FieldInfo worldField = controller.GetType().GetField("_world", BindingFlags.Instance | BindingFlags.NonPublic);
            FieldInfo inputsField = controller.GetType().GetField("_partnerTariffInputs", BindingFlags.Instance | BindingFlags.NonPublic);
            var sim = simField?.GetValue(controller) as SimulationManager;
            var world = worldField?.GetValue(controller) as World;
            var inputs = inputsField?.GetValue(controller) as Dictionary<CountryId, float>;
            Country player = sim?.World?.GetCountry(_countryId);
            if (sim == null || world == null || inputs == null || player == null)
            {
                Debug.LogError("SHOT: playtest staging failed to reach the simulation - no save written.");
                yield break;
            }

            string dir = PoliSim.Persistence.SaveGameService.DefaultSaveDirectory;
            Directory.CreateDirectory(dir);
            var noDecisions = new Dictionary<CountryId, PolicyDecision>();

            if (_countryId == CountryId.Sweden)
            {
                // (2) the Riksbank rate decision, drafted: +0.25 on the tab's own slider draft, the tab
                // open on the Riksbank, so the load lands on option C's naming - the verdict's surface.
                if (!SetPrivateField(controller, "_interestRateChangeInput", 0.25f))
                {
                    Debug.LogError("SHOT: could not draft the Riksbank rate - playtest_2 NOT written.");
                    yield break;
                }
                SetEnumField(controller, "_consolidatedTab", "Politics");
                SetEnumField(controller, "_politicsCategory", "FederalReserve");
                ResetScrolls(controller);
                yield return Settle();
                WritePlaytestSave(controller, sim, world, dir, "playtest_2_riksbank_rate_decision");
                yield return Capture("p2_riksbank_rate_decision");
                yield break;
            }

            // (1) the Trade bill open with its costs on screen.
            if (player.TradePartners.Count > 0)
            {
                TradePartner link = player.TradePartners[0];
                Country partner = world.GetCountry(link.PartnerId);
                float effective = TradeSystem.GetTariffRate(player, partner, world.TradeBlocs);
                float previousOverride = link.PlayerTariffOverride;
                link.PlayerTariffOverride = Mathf.Clamp(effective, 0f, 50f);
                inputs[link.PartnerId] = Mathf.Clamp(link.PlayerTariffOverride + 10f, 0f, 50f);
                SetEnumField(controller, "_consolidatedTab", "PolicyLaws");
                SetEnumField(controller, "_policyLawsCategory", "Trade");
                ResetScrolls(controller);
                yield return Settle();
                WritePlaytestSave(controller, sim, world, dir, "playtest_1_trade_bill_costs");
                yield return Capture("p1_trade_bill_costs");
                // the draft stays in the save; the running game is put back so the dense state starts clean
                link.PlayerTariffOverride = previousOverride;
                inputs.Remove(link.PartnerId);
            }
            else
            {
                Debug.LogError("SHOT: no trade partner - playtest_1 NOT written.");
            }

            // (3) the dense mid-game: the pause, the decisions, the meeting, then the bills - the
            // CaptureStatePins searches, in their load-bearing order (bills last, so no countdown ticks).
            int days = 0;
            while (!sim.GetPendingBudgetProcess(_countryId) && days < MaxStateSearchDays)
            {
                if (sim.AdvanceDay()) { sim.AdvanceTurn(noDecisions); }
                sim.AdvanceCountryDayTick(_countryId);
                days++;
            }
            // A cabinet can only roll decisions for portfolios that HAVE a minister, and the warmed-up
            // game has none (the player appoints) - so the authored portfolios are filled first, the
            // first candidate of each from the public pool (the CaptureStatePins idiom), and the search
            // then waits for a decision AND a meeting, two years at most so the save does not drift.
            int appointed = 0;
            foreach (CabinetPortfolio portfolio in System.Enum.GetValues(typeof(CabinetPortfolio)))
            {
                try
                {
                    List<CabinetMinister> candidates = CabinetSystem.GenerateCandidates(portfolio);
                    if (candidates != null && candidates.Count > 0) { player.CabinetMinisters[portfolio] = candidates[0]; appointed++; }
                }
                catch (System.Exception e) { Debug.Log($"SHOT: no candidate pool for {portfolio} ({e.GetType().Name}) - not appointed."); }
            }
            days = 0;
            while ((sim.GetPendingCabinetDecisions(_countryId).Count == 0 || sim.GetPendingForeignPolicyMeeting(_countryId) == null) && days < 365 * 2)
            {
                if (sim.AdvanceDay()) { sim.AdvanceTurn(noDecisions); }
                sim.AdvanceCountryDayTick(_countryId);
                days++;
            }
            Debug.Log($"SHOT: dense staging - {appointed} minister(s) appointed, decision/meeting search {days} day(s).");
            if (sim.GetPendingCabinetDecisions(_countryId).Count == 0)
            {
                // The roll is 12%/minister/turn and the window two boundaries - a miss is likely. Pin one
                // rolled from the live pools through the real roller (CaptureStatePins' own R4-4 idiom:
                // rolled, never fabricated), so the dense book carries every decision surface.
                FieldInfo pendingField = typeof(SimulationManager).GetField("_pendingCabinetDecisionsByCountry", BindingFlags.Instance | BindingFlags.NonPublic);
                if (pendingField?.GetValue(sim) is Dictionary<CountryId, List<(CabinetPortfolio Portfolio, CabinetDecision Decision)>> pendingByCountry)
                {
                    var pinned = new List<(CabinetPortfolio Portfolio, CabinetDecision Decision)>();
                    for (int i = 0; i < 1000 && pinned.Count == 0; i++)
                    {
                        foreach ((CabinetPortfolio portfolio, CabinetDecision decision) in CabinetSystem.TryRollDecisions(player)) { pinned.Add((portfolio, decision)); break; }
                    }
                    if (pinned.Count > 0) { pendingByCountry[_countryId] = pinned; Debug.Log($"SHOT: dense staging - pinned a rolled {pinned[0].Portfolio} decision ({pinned[0].Decision.Name})."); }
                    else { Debug.Log("SHOT: dense staging - no cabinet decision rolled in 1000 tries; the save carries none."); }
                }
            }

            TaxType? taxPick = null;
            foreach (TaxLine line in player.TaxLines) { if (!line.IsImplemented) { taxPick = line.Type; break; } }
            if (taxPick == null && player.TaxLines.Count > 0) { taxPick = player.TaxLines[0].Type; }
            if (taxPick != null) { sim.IntroduceTaxProgramBill(_countryId, taxPick.Value, isAdd: !FindTaxLine(player, taxPick.Value).IsImplemented); }
            WelfareProgramType? welfarePick = null;
            foreach (WelfareProgram program in player.WelfarePrograms) { if (!program.IsImplemented) { welfarePick = program.Type; break; } }
            if (welfarePick != null) { sim.IntroduceWelfareProgramBill(_countryId, welfarePick.Value, true); }
            sim.IntroduceLaborBill(_countryId, new LaborPolicyBill { MinimumWage = 12f });
            sim.IntroduceCrimeJusticeBill(_countryId, new CrimeJusticePolicyBill { PoliceFunding = 55f });
            foreach (string enactId in new[] { "three_strikes_law", "cash_bail_abolition_act", "drug_decriminalization_act", "public_defender_funding_act", "body_worn_camera_program", "court_backlog_reduction_program", "frontex_border_cooperation_agreement", "restorative_justice_program", "parental_leave_expansion_act", "raise_the_wage_act", "working_time_regulation_act", "active_labor_market_programs_act" })
            {
                player.EnactedLaws.Add(new EnactedLaw { LawId = enactId, EnactedOn = sim.CurrentDate });
            }
            foreach (string recompute in new[] { "RecomputeCrimeJusticeDialsFromEnactedLaws", "RecomputeLaborDialsFromEnactedLaws" })
            {
                sim.GetType().GetMethod(recompute, BindingFlags.Instance | BindingFlags.NonPublic)?.Invoke(sim, new object[] { player });
            }
            sim.IntroduceLawBill(_countryId, new LawBill { LawId = "cash_bail_reform_act", IsRepeal = false });
            sim.IntroduceLawBill(_countryId, new LawBill { LawId = "skilled_worker_immigration_act", IsRepeal = false });
            sim.IntroduceTradeBill(_countryId, new TradePolicyBill { NewBaseTariffRate = player.BaseTariffRate + 2f });

            Debug.Log($"SHOT: dense state - budget pause {sim.GetPendingBudgetProcess(_countryId)}, cabinet decisions {sim.GetPendingCabinetDecisions(_countryId).Count}, meeting {(sim.GetPendingForeignPolicyMeeting(_countryId) != null)}, enacted laws {player.EnactedLaws.Count}, turn {sim.CurrentTurn}.");
            SetEnumField(controller, "_consolidatedTab", "Decisions");
            ResetScrolls(controller);
            yield return Settle();
            WritePlaytestSave(controller, sim, world, dir, "playtest_3_dense_midgame");
            yield return Capture("p3_dense_midgame");
        }

        /// <summary>One playtest save through the real service - the same call the saves-menu capture makes.</summary>
        private void WritePlaytestSave(GameController controller, SimulationManager sim, World world, string dir, string name)
        {
            string path = System.IO.Path.Combine(dir, name + ".json");
            PoliSim.Persistence.SaveGameService.SaveToFile(path,
                PoliSim.Persistence.SaveGameService.CreateSaveGame(sim, world, _countryId, controller.CaptureUiDrafts()));
            Debug.Log($"SHOT: wrote playtest save {path} ({new FileInfo(path).Length} bytes).");
        }

        /// <summary>R-D2's pair - see CaptureFilmGaps (d). Skips with a logged error, never a wrong
        /// capture, if the player has no trade partner or the reflection misses.</summary>
        private IEnumerator CaptureTradeDraftReset(GameController controller)
        {
            FieldInfo simField = controller.GetType().GetField("_simulationManager", BindingFlags.Instance | BindingFlags.NonPublic);
            FieldInfo worldField = controller.GetType().GetField("_world", BindingFlags.Instance | BindingFlags.NonPublic);
            FieldInfo inputsField = controller.GetType().GetField("_partnerTariffInputs", BindingFlags.Instance | BindingFlags.NonPublic);
            MethodInfo reset = controller.GetType().GetMethod("ResetPartnerTariffDraft", BindingFlags.Instance | BindingFlags.NonPublic);
            var sim = simField?.GetValue(controller) as SimulationManager;
            var world = worldField?.GetValue(controller) as World;
            var inputs = inputsField?.GetValue(controller) as Dictionary<CountryId, float>;
            Country player = sim?.World?.GetCountry(_countryId);
            if (sim == null || world == null || inputs == null || reset == null || player == null || player.TradePartners.Count == 0)
            {
                Debug.LogError("SHOT: trade draft-reset staging failed (reflection or no trade partner) - the 06m/06n captures are MISSING, not clean.");
                yield break;
            }

            TradePartner link = player.TradePartners[0];
            Country partner = world.GetCountry(link.PartnerId);
            float effective = TradeSystem.GetTariffRate(player, partner, world.TradeBlocs);
            float previousOverride = link.PlayerTariffOverride;
            link.PlayerTariffOverride = Mathf.Clamp(effective, 0f, 50f);
            inputs[link.PartnerId] = Mathf.Clamp(link.PlayerTariffOverride + 10f, 0f, 50f);

            SetEnumField(controller, "_consolidatedTab", "PolicyLaws");
            SetEnumField(controller, "_policyLawsCategory", "Trade");
            ResetScrolls(controller);
            // 700px, not the sweep's 900: the first partner's header row stays in frame above its
            // controls at 1600 (the 900px stop put the name just above the fold).
            ScrollBy(controller, 700f);
            yield return Settle();
            yield return Capture("06m_policylaws_trade_draft_moved");

            reset.Invoke(controller, new object[] { link.PartnerId });
            yield return Settle();
            // Assert-own-name: the draft is back at the standing override (the dial row rewrites its
            // dictionary entry every frame with the slider's current value, so "key absent" is not the
            // test - "value equals the live override" is) and the live override itself has not moved.
            bool draftAtStanding = !inputs.TryGetValue(link.PartnerId, out float draftNow) || Mathf.Abs(draftNow - link.PlayerTariffOverride) < 0.001f;
            if (!draftAtStanding || !link.HasPlayerTariffOverride || Mathf.Abs(link.PlayerTariffOverride - Mathf.Clamp(effective, 0f, 50f)) > 0.001f)
            {
                Debug.LogError("SHOT: 06n_policylaws_trade_draft_reset - the draft did not reset or the live override moved; this capture would be misnamed.");
            }
            yield return Capture("06n_policylaws_trade_draft_reset");

            link.PlayerTariffOverride = previousOverride;
            inputs.Remove(link.PartnerId);
            SetEnumField(controller, "_policyLawsCategory", "LaborMarket");
            ResetScrolls(controller);
            yield return Settle();
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

        /// <summary>
        /// S-20's trap: does the written frame carry the token of the surface this capture CLAIMS?
        ///
        /// <para>The token is a 4×4 block in the top-left corner. ⚠ Unity textures are bottom-left
        /// origin and GUI is top-left, so the pixel read is `(1, height - 2)` — one in from each edge, so
        /// a filtering artefact on the very boundary cannot decide the assertion.</para>
        ///
        /// <para>⚠ <b>An UNKNOWN claim is not a pass.</b> If nothing set an expectation for this capture,
        /// the assertion is skipped and SAID to be skipped, rather than counting as evidence — C-C9's
        /// assertion 4 is the precedent for reporting an untested thing as untested.</para>
        /// </summary>
        private bool AssertCaptureIdentity(string name, Texture2D shot)
        {
            if (!CaptureIdentity.Armed || string.IsNullOrEmpty(CaptureIdentity.Expected)) { return true; }

            if (!CaptureIdentity.TryColorFor(CaptureIdentity.Expected, out Color32 want))
            {
                Debug.LogError($"SHOT: IDENTITY - '{CaptureIdentity.Expected}' is not a known surface, so {name} claims a "
                               + "screen the token palette cannot express. Add it to CaptureIdentity.Palette rather than "
                               + "letting the capture pass unchecked.");
                return false;
            }

            Color32 found = shot.GetPixel(1, shot.height - 2);
            const int Tolerance = 40;
            bool match = Mathf.Abs(found.r - want.r) <= Tolerance
                         && Mathf.Abs(found.g - want.g) <= Tolerance
                         && Mathf.Abs(found.b - want.b) <= Tolerance;

            if (match) { _identityAsserts++; return true; }

            string blamed = "no known surface";
            foreach (string surface in CaptureIdentity.Surfaces)
            {
                if (!CaptureIdentity.TryColorFor(surface, out Color32 candidate)) { continue; }
                if (Mathf.Abs(found.r - candidate.r) <= Tolerance
                    && Mathf.Abs(found.g - candidate.g) <= Tolerance
                    && Mathf.Abs(found.b - candidate.b) <= Tolerance)
                {
                    blamed = surface;
                    break;
                }
            }

            Debug.LogError($"SHOT: IDENTITY MISMATCH on {name} - it claims '{CaptureIdentity.Expected}' and the written "
                           + $"frame carries the token of '{blamed}' (rgb {found.r},{found.g},{found.b}). This is S-20's "
                           + "defect: the capture wrote, the guards were silent, and the screen under test is not the "
                           + "screen in the file. Failing loudly rather than filing a picture of something else.");
            return false;
        }

        private int _identityAsserts;

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

            // ⚠ TRAP 2, ARMED 2026-08-31. THE WIDTH ASKED FOR MUST BE THE WIDTH CAPTURED. This cost a
            // full run this week: a four-width pass omitted `-shotwidth` for its 1280 case, the Game
            // View silently fell back to the 1600 default, and the run reported "77 captured, 0 failed"
            // for a set that contained 1600 twice and no 1280 at all - the tightest width, and the one
            // this project's own record says is where an over-long caption appears, went unfilmed while
            // every guard stayed green. A capture that is silently the wrong size is worse than a
            // missing one, because it is indistinguishable from evidence.
            //
            // ExpectedWidth is 0 when the caller did not ask for a size, in which case there is nothing
            // to check and nothing is claimed.
            // ⚠ TRAP 3, ARMED 2026-08-31 (S-20). THE SCREEN CLAIMED MUST BE THE SCREEN WRITTEN.
            //
            // C-D5 found that every `-shotelectionnight` film ever taken had photographed the DESK under
            // board 1h's name - W-E6's own films included - because an overlay Canvas draws before IMGUI.
            // Through all of it: 8 captured, 0 failed, 0 overflows, 0 escapes, exit 0. Traps 1 and 2 guard
            // HOW a capture was taken; every other guard measures WITHIN whatever was drawn. Nothing asked
            // whether the thing under test was the thing on screen.
            //
            // The surface that ends up on top stamps a 4x4 token in the corner (`CaptureIdentity`), and
            // this reads it out of the texture just written. Claimed vs found, in the pixels.
            if (!AssertCaptureIdentity(name, shot))
            {
                _failed++;
                UnityEngine.Object.Destroy(shot);
                // ⚠ The claim resets even on the FAILURE path. The first run of this trap did not, and one
                // mismatched capture made every later shot in the run inherit the same claim and fail with
                // it - a cascade that hides which capture was actually wrong.
                CaptureIdentity.Expected = "imgui";
                yield break;
            }

            if (ExpectedWidth > 0 && shot.width != ExpectedWidth)
            {
                Debug.LogError($"SHOT: WIDTH MISMATCH on {name} - asked for {ExpectedWidth}, captured {shot.width}. "
                               + "The Game View did not take the requested size (a missing or ignored -shotwidth=), so "
                               + "this capture is not evidence for the width it is named after. Failing loudly rather "
                               + "than writing a file that looks correct.");
                _failed++;
                Destroy(shot);
                yield break;
            }

            string path = Path.Combine(OutputDirectory, $"{Label}_{name}.png");
            File.WriteAllBytes(path, shot.EncodeToPNG());
            Debug.Log($"SHOT: wrote {path} at {shot.width}x{shot.height}");
            _captured++;
            Destroy(shot);

            // S-20: the claim resets to IMGUI after every shot, so a Canvas claim can never leak onto the
            // next capture and quietly pass it. A caller that means a board says so, once, each time.
            CaptureIdentity.Expected = "imgui";
        }

        /// <summary>S-20: the next capture claims this surface. Resets to `imgui` the moment the shot is
        /// written, so the claim is per-capture and never sticky.</summary>
        private static void Claim(string surface)
        {
            CaptureIdentity.Expected = surface;
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

        /// <summary>
        /// THE CURSOR, PARKED (2026-08-28, the ratified candidate's second half). The OS cursor is an
        /// input to the film: wherever it rests over the Game View, IMGUI draws that control's hover
        /// state - the v31b/v31bf compare found one 66x24 box on the 2560 Desk differing by 842 px
        /// for exactly this reason, and a difference between two runs of the same code can be the
        /// mouse. Parked once, before the first capture, at the primary screen's top-left: a fixed
        /// spot on the Editor window's chrome ABOVE the Game View's content (the view is laid out at
        /// the screen origin by UiScreenshotCapture.ResizeGameView, so (0,0) is its tab strip, never
        /// a game pixel) - and even if a future layout put content there, it is the SAME spot every
        /// run, so the film stays deterministic either way. Win32-only by P/Invoke; anywhere else the
        /// method logs and does nothing, and a run-to-run diff can still be the mouse.
        /// </summary>
        private static void ParkCursor()
        {
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
            bool parked = SetCursorPos(0, 0);
            Debug.Log(parked
                ? "SHOT: cursor parked at (0,0) - no hover state can vary between two films of one code."
                : "SHOT: cursor park FAILED (SetCursorPos returned false) - the film may carry a hover state; a run-to-run diff can be the mouse.");
#else
            Debug.Log("SHOT: cursor park skipped - not a Windows platform; a run-to-run diff can be the mouse.");
#endif
        }

#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
        /// <summary>Win32: moves the OS cursor. The one P/Invoke this harness carries - see <see cref="ParkCursor"/>.</summary>
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool SetCursorPos(int x, int y);
#endif

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
                // (The OPEN column's own scroll field was excluded here until v3.1 Phase B deleted the column.)
                if (f.FieldType == typeof(Vector2) && f.Name.EndsWith("ScrollPosition"))
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
        /// (DrawFoldedInterruptBanner's re-surfaced copy, v3.0). Those are the two sites of B8's
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
                "frontex_border_cooperation_agreement", "restorative_justice_program",
                // Pass 3 (2026-08-26): four LABOR laws join the pin so the two-category browser
                // state is actually photographable - the returned category chips carry two real
                // counts, both category tokens appear in the rows, and the ENACTED status colors
                // split by area. Without these, every stop pins a one-category state by
                // construction and the returned chrome is unreachable (the driver's own recorded
                // silent-gap lesson).
                "parental_leave_expansion_act", "raise_the_wage_act",
                "working_time_regulation_act", "active_labor_market_programs_act"
            };
            foreach (string enactId in lawsToEnactForCapture)
            {
                player.EnactedLaws.Add(new EnactedLaw { LawId = enactId, EnactedOn = sim.CurrentDate });
            }

            // Pass 3: run BOTH category recomputes after the direct adds, by reflection (the
            // driver's established private-member idiom) - direct EnactedLaws.Add bypasses
            // ApplyLawBillEffects, so without this the pinned state would hold enacted laws whose
            // dials never moved: internally inconsistent, and the Labor tab's two-books
            // annotation ("laws +N -> M in effect") would exist in no capture. With it, the
            // pinned dials match what genuine enactment produces - which also means the
            // Crime & Justice read-only tab now photographs law-driven values rather than the
            // untouched 50s earlier capture sets showed (a deliberate, stated baseline change).
            foreach (string recompute in new[] { "RecomputeCrimeJusticeDialsFromEnactedLaws", "RecomputeLaborDialsFromEnactedLaws" })
            {
                MethodInfo recomputeMethod = sim.GetType().GetMethod(recompute, BindingFlags.Instance | BindingFlags.NonPublic);
                if (recomputeMethod == null)
                {
                    Debug.LogError($"SHOT: SimulationManager.{recompute} not found by reflection - the pinned law state is INCONSISTENT, not clean.");
                }
                else
                {
                    recomputeMethod.Invoke(sim, new object[] { player });
                }
            }

            bool lawOk = sim.IntroduceLawBill(_countryId, new LawBill { LawId = "cash_bail_reform_act", IsRepeal = false });
            bool lawOk2 = sim.IntroduceLawBill(_countryId, new LawBill { LawId = "sanctuary_city_policy", IsRepeal = false });
            // Pass 3: a labor law's pending bill alongside the two C&J ones - available/enacted/
            // pending now all exist in BOTH categories in one capture.
            bool lawOk3 = sim.IntroduceLawBill(_countryId, new LawBill { LawId = "skilled_worker_immigration_act", IsRepeal = false });

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
                // Observed, not raw (2026-08-25): forcing approval to guarantee a WIN capture is a
                // harness write like the ledger-pin ones above, and leaving it un-recorded is what
                // tripped the approval self-audit at the next boundary - the 2050-12-26 ATTRIB that
                // rode every "clean" run before ruling 1's fold would have failed the capture on it.
                float winForceBefore = player.State.ApprovalRating;
                player.State.ApprovalRating = 60f;
                ApprovalLedgerRecorder.RecordEvent(player, sim.CurrentDate, "Harness: forced approval for WIN reveal capture", player.State.ApprovalRating - winForceBefore);
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
                // Observed, same as the WIN force above - the LOSS reveal's own approval override.
                float lossForceBefore = player.State.ApprovalRating;
                player.State.ApprovalRating = 5f;
                ApprovalLedgerRecorder.RecordEvent(player, sim.CurrentDate, "Harness: forced approval for LOSS reveal capture", player.State.ApprovalRating - lossForceBefore);
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

        /// <summary>Screen 0's invariants on the frame just filmed (UI v3.0 Phase B): the controller is
        /// on the Desk, the fold is locked there (R-B3) and the frame is FOLDED. A miss is an error -
        /// the capture would otherwise be filed under a name that lies about its state.</summary>
        private static void AssertDeskState(GameController controller, string stem)
        {
            if (!controller.OnDesk)
            {
                Debug.LogError($"SHOT: {stem} - the controller is NOT on the Desk; the capture is not Screen 0.");
            }

            // R-B3's "locked FOLDED" half of this assert retired with the fold itself (v3.1 R-E1, ONE
            // FRAME; the deletion in v3.1 Phase B): there is one frame, so there is nothing to assert
            // about it - the Desk's own state is the whole claim.
            Debug.Log($"SHOT: {stem} - Screen 0 on film (ONE FRAME).");
        }

        /// <summary>
        /// The event card filled (C1/C2/C3): the country's last event is set to the FIRST entry of
        /// EventSystem's own pool - an authored event, never one written for the film - for one
        /// capture, then the previous state is restored (the sweep advances no turn after this point,
        /// and the trajectory dumps run in their own process, so nothing downstream reads the staged
        /// value). A missing hook is an error: the film would otherwise show the empty reservation
        /// under a name that promises the card.
        /// </summary>
        private IEnumerator CaptureDeskEventCard(GameController controller)
        {
            FieldInfo simField = controller.GetType().GetField("_simulationManager", BindingFlags.Instance | BindingFlags.NonPublic);
            object sim = simField?.GetValue(controller);
            FieldInfo dictField = sim?.GetType().GetField("_lastEventsByCountry", BindingFlags.Instance | BindingFlags.NonPublic);
            var lastEvents = dictField?.GetValue(sim) as Dictionary<CountryId, EconomicEvent>;
            FieldInfo poolField = typeof(EventSystem).GetField("EventPool", BindingFlags.Static | BindingFlags.NonPublic);
            var pool = poolField?.GetValue(null) as List<EconomicEvent>;
            if (lastEvents == null || pool == null || pool.Count == 0)
            {
                Debug.LogError("SHOT: the event card could not be staged (_simulationManager/_lastEventsByCountry/EventPool not found) - 01e_desk_event NOT captured.");
                yield break;
            }

            bool had = lastEvents.TryGetValue(_countryId, out EconomicEvent previous);
            lastEvents[_countryId] = pool[0];
            yield return Settle();
            yield return Capture("01e_desk_event");
            Debug.Log($"SHOT: 01e_desk_event - the card filled with the pool's own \"{pool[0].Name}\" (GDP {pool[0].GdpShockPercent:+0.0;-0.0}%, inflation {pool[0].InflationShockPoints:+0.0;-0.0} pts, approval {pool[0].ApprovalEffect:+0.0;-0.0}).");
            if (had) { lastEvents[_countryId] = previous; } else { lastEvents.Remove(_countryId); }
            yield return Settle();
        }

        /// <summary>
        /// The game-over stamp (C4/C5): _isGameOver raised with the election-loss reason in the exact
        /// form CheckElection prints it, from the live turn and approval - for one capture, then both
        /// fields restored. The stage beneath stays what it was; only the frame's gating changes.
        /// </summary>
        private IEnumerator CaptureDeskGameOver(GameController controller)
        {
            FieldInfo overField = controller.GetType().GetField("_isGameOver", BindingFlags.Instance | BindingFlags.NonPublic);
            FieldInfo reasonField = controller.GetType().GetField("_gameOverReason", BindingFlags.Instance | BindingFlags.NonPublic);
            FieldInfo countryField = controller.GetType().GetField("_playerCountry", BindingFlags.Instance | BindingFlags.NonPublic);
            FieldInfo simField = controller.GetType().GetField("_simulationManager", BindingFlags.Instance | BindingFlags.NonPublic);
            var country = countryField?.GetValue(controller) as Country;
            var sim = simField?.GetValue(controller) as SimulationManager;
            if (overField == null || reasonField == null || country == null || sim == null)
            {
                Debug.LogError("SHOT: the game-over stamp could not be staged (_isGameOver/_gameOverReason/_playerCountry/_simulationManager not found) - 01f_desk_gameover NOT captured.");
                yield break;
            }

            object wasOver = overField.GetValue(controller);
            object wasReason = reasonField.GetValue(controller);
            overField.SetValue(controller, true);
            reasonField.SetValue(controller, $"Lost re-election at year {sim.CurrentTurn} with {country.State.ApprovalRating:F1} approval (needed at least {ElectionSystem.LosingThreshold:F0}).");
            yield return Settle();
            yield return Capture("01f_desk_gameover");
            overField.SetValue(controller, wasOver);
            reasonField.SetValue(controller, wasReason);
            yield return Settle();
        }

        /// <summary>
        /// R-SP5 (2026-08-28): after the map's capture, the separation its renderer measured - every
        /// label at least <see cref="MapRenderer.MinLabelSeparationPx"/> from every other label and
        /// node, after §A.9a's ladder - is asserted. A miss is an error: the ladder could not clear the
        /// floor at this size, which is the measurement the ruling wants reported, and the run's fold
        /// turns red on it.
        /// </summary>
        private static void AssertMapLabelSeparation(GameController controller, string screen)
        {
            FieldInfo field = controller.GetType().GetField("_mapRenderer", BindingFlags.Instance | BindingFlags.NonPublic);
            if (!(field?.GetValue(controller) is MapRenderer map))
            {
                Debug.LogError("SHOT: _mapRenderer not found - the map-label separation assert VERIFIED NOTHING.");
                return;
            }

            float gap = map.LastMinLabelSeparation;
            Debug.Log($"SHOT: map labels on {screen} - {map.LastLabelRects.Count} label(s), smallest gap {gap:F1} px against the {MapRenderer.MinLabelSeparationPx} px floor, ladder rung {map.LastLabelRung}.");
            if (gap < MapRenderer.MinLabelSeparationPx)
            {
                Debug.LogError($"SHOT: MAP LABELS {gap:F1} px apart on {screen} ({map.LastLabelViolation}) - the resort ladder could not clear the floor at this size.");
            }
        }

        /// <summary>UI v3.0 Phase A, Phase 3: `-shotladder` films each candidate stage instrument at a
        /// descending run of sizes (GameController.DrawInstrumentLadder) instead of the tab sweep, so
        /// the inventory's minimum legible sizes are measured on film. Set from the command line.</summary>
        public bool Ladder;

        /// <summary>W-E1: `-shotcampaign` films Campaign HQ (GameController.Campaign.cs) instead of
        /// the tab sweep — three staged days, so the empty states, the full ledgers and the
        /// over-budget caution are all on film rather than only the flattering one. Set from the
        /// command line; R-N2 holds, so this is the ONLY path that reaches the screen.</summary>
        public bool CampaignHq;

        /// <summary>W-E6: film board 1h, election night, in its four states.</summary>
        public bool ElectionNightBoard;

        /// <summary>
        /// C-C10 (P-G2): `-shotledger` films the impact ledger instead of the tab sweep.
        ///
        /// <para>⚠ <b>It needs its own flag because the sweep is deliberately a NO-POLICY warm-up</b> —
        /// *"anything else would bake one playthrough's choices into what is meant to be a picture of the
        /// UI"* — and a ledger with no policy behind it correctly has nothing to attribute. Filming its
        /// populated state therefore requires a game where the player actually moved dials, which is a
        /// different run, not a stricter one. Both states end up on film: the sweep carries the empty
        /// one, this carries the populated one.</para>
        /// </summary>
        public bool ImpactLedger;

        /// <summary>
        /// Sweden's 2022 Riksdag result — the SOURCED vector every polled figure on the campaign
        /// screen descends from (ElectionsData/sweden/returns_2022.md; Valmyndigheten's RD_S.json
        /// final count, 6 477 970 valid votes). It is used here as the campaign's TRUE preference,
        /// which the real `PollingSystem.Conduct` then samples: the screen therefore shows a genuine
        /// poll of genuine data, never a hand-written percentage. Order matches
        /// <see cref="Sweden2022Parties"/>.
        /// </summary>
        private static readonly double[] Sweden2022Shares =
        {
            0.3033, 0.2054, 0.1910, 0.0675, 0.0671, 0.0534, 0.0508, 0.0461
        };

        private static readonly string[] Sweden2022Parties =
        {
            "S", "SD", "M", "V", "C", "KD", "MP", "L"
        };

        /// <summary>
        /// Four of Sweden's 29 valkretsar, spelled as Valmyndigheten spells them
        /// (ElectionsData/sweden/valkrets_votes_2022.csv). Real constituency names, so the office
        /// ledger names places that exist; the volunteer and upkeep figures beside them are
        /// [AUTHORED-DRAFT] staging and are logged as such by the pass below.
        /// </summary>
        private static readonly string[] SwedenValkretsar =
        {
            "Blekinge län", "Dalarnas län", "Gotlands län", "Gävleborgs län"
        };

        /// <summary>
        /// W-E1's pass. Three staged days of one Swedish campaign, each filmed once.
        ///
        /// **What is derived and what is staged, stated rather than blurred.** The poll is a real
        /// <see cref="PollingSystem.Conduct"/> draw against the sourced 2022 vector; the margins of
        /// error come out of that draw; momentum is a real <see cref="MomentumTracker"/> shock
        /// decayed on §22's half-life; every queued action's cost is read from
        /// <see cref="CampaignActions.Spec"/>; the legality list is
        /// <see cref="CampaignLegality.LegalActions"/>; the perceived-economy index is
        /// <see cref="PerceivedPerformance.Perceived"/> read off the LIVE warmed-up country. The war
        /// chest, the volunteer counts and the office upkeep are [AUTHORED-DRAFT] staging (W-F5 will
        /// source real party finances) and no figure here is a spec illustration.
        ///
        /// **Staff carry no personal names.** Inventing people would be inventing data; the rows
        /// show the post, whether it is filled, and the draft bonus. Names are W-B5's business, with
        /// their own sourcing.
        ///
        /// The pass is SEEDED (`CampaignFilmSeed`) so the poll draw is byte-stable run to run — the
        /// same rule-15 reasoning that seeded the main film.
        /// </summary>
        private const int CampaignFilmSeed = 20260913;

        /// <summary>
        /// W-E6 — board 1h in its four states. The night is RUN, not staged: one seeded schedule
        /// over Sweden 2022's own regionalised returns, sampled at four moments of it, so the four
        /// films are four instants of ONE night and cannot disagree with each other. The states are
        /// chosen by how much has DECLARED rather than by clock time, because that is what the
        /// screen is about.
        /// </summary>
        private IEnumerator CaptureElectionNight(GameController controller)
        {
            // ⚠ C-D5 (2026-08-31): PUT THE DESK AWAY FIRST — a defect this item found in W-E6's own film
            // path, not one C-D5 introduced.
            //
            // The board is a ScreenSpaceOverlay Canvas, and `GameController.OnGUI` draws AFTER overlay
            // canvases in the built-in pipeline, so the desk paints straight over it. **Every
            // `-shotelectionnight` film ever taken shows the DESK under the board's name** — checked
            // against `we6_night_1280_e6_election_night_final.png` from W-E6 itself, which shows the desk
            // exactly as this item's first run did. The board built, the captures wrote, the run exited 0,
            // and nobody was looking at board 1h.
            //
            // `_canvasLive` is the controller's own takeover flag: with it set, IMGUI draws only its hold
            // banner and the Canvas is what a capture sees. Set by reflection, the way this file reaches
            // every other piece of private state, and restored at the end.
            SetPrivateField(controller, "_canvasLive", true);
            yield return Settle();

            ElectionNightFilm.Stage(out string[] names, out long[][] votes, out long[] valid,
                out long[] eligible, out int[] arrivals, out string[] parties, out var blocs);

            // Four instants of one night, picked by declared count: the first returns, the middle
            // of the count, the moment the last threshold call lands, and the completed night.
            int[] wanted = { 4, 16, 28, 29 };
            var stems = new[] { "early", "partial", "called", "final" };

            for (int i = 0; i < wanted.Length; i++)
            {
                int minute = ElectionNightFilm.MinuteFor(wanted[i], arrivals);
                NightState state = ElectionNight.At(minute, names, votes, valid, eligible, arrivals,
                    349, 0.04, parties, null, blocs);

                // C-D5 (V-N3): the comparison is Sweden 2018, SOURCED - the same
                // `ElectionNightFilm.Votes2018` the results screen already compares against, so the two
                // screens cannot disagree about the swing any more than they can about who won.
                PoliSim.Testing.CaptureIdentity.CanvasSurface = "electionnight";
                ElectionNightScreen screen = ElectionNightScreen.Build(state, parties, "SWEDEN",
                    new DateTime(2026, 9, 13, 20, 0, 0), 349,
                    previousVotes: ElectionNightFilm.Votes2018, previousLabel: "SWEDEN 2018");
                if (screen == null)
                {
                    Debug.LogError("SHOT: W-E6 - the board did not build (furniture missing); nothing filmed.");
                    yield break;
                }

                yield return Settle();
                Claim("electionnight");
                yield return Capture("e6_election_night_" + stems[i]);

                Debug.Log(string.Format(CultureInfo.InvariantCulture,
                    "SHOT: W-E6 {0} - minute {1}, {2} of {3} declared, {4:N0} votes counted, {5} call(s) safe.",
                    stems[i], minute, state.DeclaredCount, state.TotalConstituencies, state.CountedValid, state.Calls.Count));

                // Destroy, not DestroyImmediate: this runs in PLAY mode, where the immediate form tears a
                // Canvas child out mid-frame and the run ends without reaching its own exit.
                if (screen.Root != null) { UnityEngine.Object.Destroy(screen.Root); }
                yield return Settle();
            }

            // The run ends HERE, and it must end through Finish - a `yield break` alone leaves
            // the driver's own exit unreached and the process is forced out with a 1 (which is
            // what the first film of this board did, four widths over).
            PoliSim.Testing.CaptureIdentity.CanvasSurface = null;
            SetPrivateField(controller, "_canvasLive", false);
            yield return Settle();

            Debug.Log($"SHOT: election night done, {_captured} captured, {_failed} failed.");
            Debug.Log($"SHOT: {ReportOverflows()} text overflow(s) recorded.");
            Debug.Log($"SHOT: {ReportContainmentEscapes()} containment escape(s) recorded.");
            Finish(_failed == 0 && _loggedErrors == 0 ? 0 : 1);
        }

        private IEnumerator CaptureCampaignHq(GameController controller)

        {
            if (_countryId != CountryId.Sweden)
            {
                // The staged data is Swedish and sourced as Swedish. Filming it under another
                // country's frame would put real Swedish returns beside the wrong flag, which is
                // exactly the quiet wrongness the data classes exist to prevent.
                Debug.LogError($"SHOT: -shotcampaign stages SOURCED Swedish returns but the run is playing {_countryId} " +
                               "- pass -shotcountry=Sweden. NOTHING captured.");
                Finish(1);
                yield break;
            }

            double perceived = ReadPerceivedEconomy(controller);

            var states = new[]
            {
                // Three days chosen so the film shows the screen's THREE distinct readings, not the
                // flattering one three times: nothing yet, a full but affordable day, and a day the
                // resource system would refuse.
                //  - precampaign: every empty state at once (no staff, no offices, nothing queued)
                //    and §3's preparation verbs as the only ones open.
                //  - campaign: a fully booked, affordable day - Rally 4 h + Town hall 3 h + Door to
                //    door 5 h is exactly the 12 h `StartDay` grants, so the queue reads 12 of 12
                //    with no caution. A film in which every day is over budget never shows the
                //    normal case.
                //  - overcommitted: over on BOTH money and hours, so the caution line and
                //    `TrySpend`'s refusal are on film rather than merely implemented.
                new CampaignFilmState("precampaign", new DateTime(2026, 6, 1), 2_400_000.0, 12.0, 180, 0, 0, 0.0),
                new CampaignFilmState("campaign", new DateTime(2026, 8, 21), 1_120_000.0, 12.0, 1_460, 4, 3, 2.2),
                new CampaignFilmState("overcommitted", new DateTime(2026, 9, 4), 210_000.0, 3.0, 1_820, 4, 7, -1.4),
            };

            foreach (CampaignFilmState state in states)
            {
                controller.SetCampaignScreen(BuildCampaignSnapshot(state, perceived));
                yield return Settle();
                yield return Capture($"e1_campaign_hq_{state.Stem}");
            }

            controller.SetCampaignScreen(null);
            yield return Settle();

            // W-E3's three states, filmed from the SAME staged campaign day so the two screens agree
            // about the war chest and the hours - two campaign screens disagreeing about the money
            // would be worse than either being wrong alone. R-N1: `-shotcampaign` films the whole
            // Track E set rather than gaining a flag per screen, because each Unity launch costs
            // ~40 s of warm-up and the screens share their staging; the items stay separable by
            // capture NAME (`e1_*`, `e3_*`, `e4_*`), which is what a reviewer actually reads.
            yield return CaptureActionScreen(controller, perceived);
            yield return CapturePollingScreen(controller, perceived);
            yield return CaptureCampaignMap(controller, perceived);
            yield return CaptureDebateScreen(controller, perceived);
            yield return CaptureResultsScreen(controller);
            yield return CaptureCoalitionScreen(controller);

            Debug.Log($"SHOT: Campaign HQ - poll drawn by PollingSystem.Conduct against the SOURCED Sweden 2022 vector " +
                      $"(seed {CampaignFilmSeed}); perceived economy {perceived:F1}/100 read off the live country; " +
                      "war chest, volunteers and office upkeep are [AUTHORED-DRAFT] staging (W-F5 sources party finances).");
            Debug.Log($"SHOT: campaign done, {_captured} captured, {_failed} failed.");
            Debug.Log($"SHOT: {ReportOverflows()} text overflow(s) recorded.");
            Debug.Log($"SHOT: {ReportContainmentEscapes()} containment escape(s) recorded.");
            Finish(_failed == 0 && _loggedErrors == 0 ? 0 : 1);
        }

        /// <summary>
        /// W-E3's pass — the action screen at three readings that must look DIFFERENT from one
        /// another, because the item's bar is about what the estimate communicates:
        ///
        /// - **`tight`** — a big, recent poll. The band is narrow, and acting on the number is
        ///   reasonable.
        /// - **`loose`** — a small, stale poll. The band is wide *for the same action and the same
        ///   spend*, because the player knows less. Nothing about the world changed; only the
        ///   measurement did, and the screen must show that as width.
        /// - **`unmeasured`** — §36's gate. No poll of this audience at all, so the screen prints
        ///   **no number**: an unbought fact is an absent estimate, not a wide one.
        ///
        /// The ± figures are the measurement, and the band's width is that measurement propagated
        /// through §42's chain by `CampaignActions.ResolveBand` — proven by sweep in
        /// `ChainBandHarness` to bound the whole uncertainty box. Nothing here is an authored ±.
        /// <summary>
        /// W-E4's pass — the polling screen at three readings.
        ///
        /// §21's four offers are **[AUTHORED-DRAFT]** in sample size and price (real Swedish polling
        /// prices are W-F5's to source, and the pass says so in its log line), but every ± beside
        /// them is DERIVED by `PollingSystem.MarginOfErrorPp` from that sample size — the same
        /// function a conducted poll reports with, so the price list cannot promise a precision the
        /// polls then fail to deliver.
        ///
        /// The three readings exist to show the decision from both ends:
        /// - **`rich`** — the whole ladder affordable, so the question is genuinely "is the extra
        ///   information worth it" rather than "what can I have".
        /// - **`poor`** — late in the campaign with the chest nearly gone, where most of the ladder
        ///   greys out and the question answers itself.
        /// - **`nomomentum`** — §22's stock at rest, so the centre column's empty state is on film.
        /// </summary>
        /// <summary>
        /// W-E5's three states, from the SAME staged campaign day as its siblings so the strip
        /// agrees about the money and the days left. The debate itself is RUN, not mocked - the
        /// same `Debates.Hold` the model uses, on the `Debate` stream at a fixed seed - so the
        /// film shows what the model produces and the mid-debate state is a genuine prefix of the
        /// finished one rather than a separately invented picture.
        /// </summary>
        private IEnumerator CaptureDebateScreen(GameController controller, double perceived)
        {
            var day = new CampaignFilmState("debate", new DateTime(2026, 8, 21), 1_120_000.0, 12.0, 1_460, 4, 3, 2.2);
            CampaignSnapshot campaign = BuildCampaignSnapshot(day, perceived);

            // W-F6: the NAMES are SOURCED - Magdalena Andersson (S) and Ulf Kristersson (M) led
            // their parties at the 2022 election, each cited to that party's OWN website as it
            // stood that week (ElectionsData/sweden/party_leaders_2022.md).
            //
            // THE NINE ATTRIBUTES BESIDE THEM ARE STILL [AUTHORED-DRAFT] GAME FICTION, and the
            // screen says so. Sourcing a real person's NAME does not license inventing their
            // CHARACTER; the ban on invented data is not suspended because someone is famous.
            // Deliberately UNEQUAL, so the verdict has a margin worth reading, not a draw.
            var a = new CandidateProfile("Magdalena Andersson", 72, 78, 74, 70, 68, 76, 70, 66, 62);
            var b = new CandidateProfile("Ulf Kristersson", 64, 66, 70, 72, 70, 71, 68, 61, 66);
            var prepA = new DebatePreparation(14.0, new[] { IssueId.Economy, IssueId.Healthcare },
                new[] { DebateMove.PresentStatistics, DebateMove.DefendPolicy, DebateMove.AttackOpponent, DebateMove.AppealEmotionally, DebateMove.Counterattack, DebateMove.DefendPolicy });
            var prepB = new DebatePreparation(6.0, new[] { IssueId.Immigration },
                new[] { DebateMove.AttackOpponent, DebateMove.ChangeSubject, DebateMove.IgnoreAttack, DebateMove.PresentStatistics, DebateMove.AttackOpponent, DebateMove.DefendPolicy });

            SimulationRandom.Seed(FilmSeed);
            DebateResult result = Debates.Resolve(a, prepA, issue => issue == IssueId.Economy ? 0.8 : 0.3,
                b, prepB, issue => issue == IssueId.Immigration ? 0.85 : 0.25,
                6, SimulationRandom.For(SimulationRandom.Stream.Debate));

            var midway = new DebateExchange[result.Exchanges.Length / 2];
            Array.Copy(result.Exchanges, midway, midway.Length);

            var states = new[]
            {
                new DebateScreenSnapshot(campaign, DebateStage.Preparation, a.Name, b.Name, a, b, prepA, prepB,
                    new DebateExchange[0], result.Exchanges.Length, default),
                new DebateScreenSnapshot(campaign, DebateStage.InProgress, a.Name, b.Name, a, b, prepA, prepB,
                    midway, result.Exchanges.Length, default),
                new DebateScreenSnapshot(campaign, DebateStage.Verdict, a.Name, b.Name, a, b, prepA, prepB,
                    result.Exchanges, result.Exchanges.Length, result),
            };
            var stems = new[] { "prep", "midway", "verdict" };

            for (int i = 0; i < states.Length; i++)
            {
                controller.SetCampaignDebateScreen(states[i]);
                yield return Settle();
                yield return Capture("e5_campaign_debate_" + stems[i]);
            }

            controller.SetCampaignDebateScreen(null);
            yield return Settle();

            Debug.Log(string.Format(CultureInfo.InvariantCulture,
                "SHOT: W-E5 - {0} exchanges held on the Debate stream at FilmSeed {1}; performance {2:F1} / {3:F1}, "
                + "margin {4:F1} pts, coverage shock {5:F2}, momentum shock {6:F2} pp. The midway film is a genuine "
                + "PREFIX of the finished debate ({7} of {0} exchanges), not a separately invented picture; the "
                + "unresolved rows are em dashes, never zeroes. Candidate attributes are [AUTHORED-DRAFT] (W-F6).",
                result.Exchanges.Length, FilmSeed, result.PerformanceA, result.PerformanceB, result.Margin,
                result.CoverageShock, result.MomentumShockPp, midway.Length));
        }

        /// <summary>
        /// W-E7 - section 30's results, over the SAME election W-E6's night counted, so the two
        /// screens cannot disagree about who won. The comparison is Sweden 2018, SOURCED
        /// (ElectionsData/priors/previous_elections.md), and its seats are DERIVED by the same
        /// allocation that reproduces 2022 seat-for-seat - never a second arithmetic.
        ///
        /// Filmed in two states: the player as the largest party, and the player as a party that
        /// LOST ground, because a results screen that has only ever been seen on a win is a screen
        /// nobody has checked the signs on.
        /// </summary>
        private IEnumerator CaptureResultsScreen(GameController controller)
        {
            ElectionNightFilm.Stage(out string[] names, out long[][] votes, out long[] valid,
                out long[] eligible, out int[] arrivals, out string[] parties, out _);

            long totalValid = 0; foreach (long v in valid) { totalValid += v; }
            long totalEligible = 0; foreach (long e in eligible) { totalEligible += e; }
            var national = new long[parties.Length];
            foreach (long[] row in votes) { for (int p = 0; p < parties.Length; p++) { national[p] += row[p]; } }

            int[] seats = SeatAllocation.AllocateWithThreshold(national, totalValid, 0.04, 349,
                SeatAllocation.ModifiedSainteLagueDivisor);

            // Sweden 2018, SOURCED. Seats DERIVED by the same allocation, so the comparison column
            // is not a second arithmetic pretending to agree with the first.
            long[] previous = ElectionNightFilm.Votes2018;
            long previousValid = 0; foreach (long v in previous) { previousValid += v; }
            int[] previousSeats = SeatAllocation.AllocateWithThreshold(previous, previousValid, 0.04, 349,
                SeatAllocation.ModifiedSainteLagueDivisor);

            foreach (int player in new[] { 0, 7 })   // S, the largest; L, which lost ground
            {
                var input = new VoteAttribution.Inputs
                {
                    OwnPersuasionByAction = new double[8],
                    AttacksReceived = 0.0,
                    TotalPersuasionPerParty = new double[parties.Length],
                    BaseCompatibility = BaselineCompatibility(national, totalValid),
                    PriorShares = BaselineShares(previous, previousValid),
                    LoyaltyPerParty = FlatLoyalty(parties.Length),
                };

                // Turnout and the electorate are the SOURCED ones, not a ratio re-derived from
                // the eight parties' votes over a derived electorate - that arithmetic reads
                // 85.88 %, and a results screen is exactly where a derived figure gets mistaken
                // for the published one.
                var snapshot = new ResultsScreenSnapshot("SWEDEN", new DateTime(2022, 9, 11), parties, player,
                    national, seats, totalValid, ElectionNightFilm.Turnout2022, ElectionNightFilm.Eligible2022, 349,
                    "2018", previous, previousSeats, previousValid,
                    names, votes, valid, VoteAttribution.Explain(input, player));

                controller.SetCampaignResultsScreen(snapshot);
                yield return Settle();
                yield return Capture("e7_results_" + (player == 0 ? "largest" : "lost_ground"));

                Debug.Log(string.Format(CultureInfo.InvariantCulture,
                    "SHOT: W-E7 {0} - {1} seats on {2:N0} votes for the eight parties, turnout {3:P2} (SOURCED, not re-derived); {4} swing {5:+0.00;-0.00} pp, {6:+0;-0;0} seats against 2018. "
                    + "Section 30's voter-group block is drawn ABSENT: the electorate is one group until W-F4 and rule 0.4 forbids inventing it.",
                    parties[player], snapshot.Seats[player], totalValid, snapshot.Turnout,
                    parties[player], snapshot.SwingPp(player), snapshot.SeatChange(player), arrivals.Length));
            }

            controller.SetCampaignResultsScreen(null);
            yield return Settle();
        }

        /// <summary>A flat compatibility baseline that reproduces the prior shares - the attribution needs a base to move FROM, and the count itself is what moved.</summary>
        private static double[] BaselineCompatibility(long[] national, long valid)
        {
            var c = new double[national.Length];
            for (int p = 0; p < national.Length; p++) { c[p] = 50.0; }
            return c;
        }

        private static double[] BaselineShares(long[] previous, long previousValid)
        {
            var s = new double[previous.Length];
            for (int p = 0; p < previous.Length; p++) { s[p] = (double)previous[p] / previousValid; }
            return s;
        }

        private static double[] FlatLoyalty(int parties)
        {
            var l = new double[parties];
            for (int p = 0; p < parties; p++) { l[p] = 50.0; }
            return l;
        }

        /// <summary>
        /// W-E8 - the coalition screen in three outcome states, and all three FALL OUT OF THE MODEL
        /// rather than being staged: the same `CoalitionFormation.Form` the harness proves, on the
        /// same Sweden 2022 seats W-E6 counted and W-E7 tabulated.
        ///
        /// - `confidence_and_supply` - 2022 as it happened: M+KD+L in cabinet, SD carrying it.
        /// - `new_election` - the 150/100/99 chamber where every pair refuses every other, which is
        ///   the harness's own reachability proof drawn.
        /// - `majority` - the same 2022 seats with the DECLARED lines dropped, which is the
        ///   counterfactual that shows what those declarations are actually doing.
        /// </summary>
        private IEnumerator CaptureCoalitionScreen(GameController controller)
        {
            var stems = new[] { "confidence_and_supply", "new_election", "majority" };
            for (int i = 0; i < stems.Length; i++)
            {
                CoalitionScreenSnapshot snapshot = BuildCoalitionState(i);
                controller.SetCampaignCoalitionScreen(snapshot);
                yield return Settle();
                yield return Capture("e8_coalition_" + stems[i]);

                Debug.Log(string.Format(CultureInfo.InvariantCulture,
                    "SHOT: W-E8 {0} - {1}; cabinet {2} ({3} seats), carried by {4}; {5} viable, {6} arithmetic majorities refused.",
                    stems[i], snapshot.Result.Outcome, snapshot.Name(snapshot.Result.Government.Cabinet),
                    snapshot.Result.Government.CabinetSeats, snapshot.Name(snapshot.Result.Government.Support),
                    snapshot.Result.Viable.Count, snapshot.Result.BlockedByRedLine.Count));
            }

            controller.SetCampaignCoalitionScreen(null);
            yield return Settle();
        }

        private static CoalitionScreenSnapshot BuildCoalitionState(int which)
        {
            if (which == 1)
            {
                // The reachability chamber: three blocs, none able to govern alone, each refusing
                // both others. This is the harness's own proof that a new election is REACHABLE.
                var seats = new[] { 150, 100, 99 };
                var compat = new double[3, 3];
                for (int a = 0; a < 3; a++) { for (int b = 0; b < 3; b++) { compat[a, b] = a == b ? 100.0 : 20.0; } }
                var mutual = new System.Collections.Generic.List<RedLine>
                {
                    new RedLine(0, 1, RedLineKind.Declared, true, "DECLARED: a mutual refusal"),
                    new RedLine(0, 2, RedLineKind.Declared, true, "DECLARED: a mutual refusal"),
                    new RedLine(1, 2, RedLineKind.Declared, true, "DECLARED: a mutual refusal"),
                };

                CoalitionResult deadlocked = CoalitionFormation.Form(seats, compat, mutual);
                return new CoalitionScreenSnapshot("A HUNG CHAMBER", new[] { "A", "B", "C" }, seats, 349,
                    deadlocked.Majority, 0, deadlocked, mutual);
            }

            int[] real = ElectionNightFilm.Seats2022;
            double[,] compatibility = CoalitionFilm.Compatibility();
            System.Collections.Generic.List<RedLine> lines = which == 2
                ? CoalitionFilm.DerivedOnly()      // the declarations dropped - the counterfactual
                : CoalitionFilm.AllLines();

            CoalitionResult result = CoalitionFormation.Form(real, compatibility, lines);
            return new CoalitionScreenSnapshot("SWEDEN", ElectionNightFilm.Parties, real, 349,
                result.Majority, 0, result, lines);
        }

        private IEnumerator CapturePollingScreen(GameController controller, double perceived)
        {
            var states = new[]
            {
                new PollingFilmState("rich", new DateTime(2026, 8, 21), 1_120_000.0, 2.2),
                new PollingFilmState("poor", new DateTime(2026, 9, 4), 96_000.0, -1.4),
                new PollingFilmState("nomomentum", new DateTime(2026, 8, 21), 1_120_000.0, 0.0),
            };

            foreach (PollingFilmState state in states)
            {
                var day = new CampaignFilmState(state.Stem, state.Today, state.Money, 12.0, 1_460, 4, 3,
                    state.MomentumShockPp);
                CampaignSnapshot campaign = BuildCampaignSnapshot(day, perceived);

                // The ± is quoted at the PLAYER's own polled share, because a margin of error depends
                // on the proportion measured - quoting one number for a whole poll would be wrong,
                // and the screen says which share it used.
                int player = campaign.PlayerPartyIndex;
                double quotedShare = campaign.LatestPoll.Share(player);

                // §21's ladder: cheap/low-sample/large-uncertainty through to
                // expensive/large-sample/regional/demographic/turnout, exactly as the spec lists it.
                var offers = new[]
                {
                    MakeOffer("Public tracker", 600, 40_000, false, false, false, campaign),
                    MakeOffer("Standard commission", 1_200, 120_000, false, false, false, campaign),
                    MakeOffer("Regional breakdown", 2_400, 260_000, true, false, false, campaign),
                    MakeOffer("Full internal programme", 6_000, 620_000, true, true, true, campaign),
                };

                controller.SetCampaignPollingScreen(new PollingScreenSnapshot(
                    campaign, offers, 3, quotedShare, campaign.PartyNames[player]));
                yield return Settle();
                yield return Capture($"e4_campaign_polling_{state.Stem}");

                Debug.Log(string.Format(CultureInfo.InvariantCulture,
                    "SHOT: W-E4 {0} - +/-{1:F2}pp at n=600 down to +/-{2:F2}pp at n=6000 on {3}'s {4:P1}; "
                    + "sample sizes and prices are [AUTHORED-DRAFT] (W-F5 sources them), every +/- is DERIVED "
                    + "by PollingSystem.MarginOfErrorPp.",
                    state.Stem, offers[0].MarginOfErrorPp(quotedShare), offers[3].MarginOfErrorPp(quotedShare),
                    campaign.PartyNames[player], quotedShare));
            }

            controller.SetCampaignPollingScreen(null);
            yield return Settle();
        }

        private static PollOffer MakeOffer(string name, int sampleSize, double cost, bool regional,
            bool demographic, bool turnout, CampaignSnapshot campaign)
        {
            return new PollOffer(name, sampleSize, cost, regional, demographic, turnout,
                cost <= campaign.Resources.Money);
        }

        /// <summary>One filmed polling day: the money and the momentum are what vary.</summary>
        private readonly struct PollingFilmState
        {
            public readonly string Stem;
            public readonly DateTime Today;
            public readonly double Money;
            public readonly double MomentumShockPp;

            public PollingFilmState(string stem, DateTime today, double money, double momentumShockPp)
            {
                Stem = stem; Today = today; Money = money; MomentumShockPp = momentumShockPp;
            }
        }

        /// </summary>
        private IEnumerator CaptureActionScreen(GameController controller, double perceived)
        {
            var day = new CampaignFilmState("campaign", new DateTime(2026, 8, 21), 1_120_000.0, 12.0, 1_460, 4, 3, 2.2);
            CampaignSnapshot campaign = BuildCampaignSnapshot(day, perceived);

            // The audience is STRUCTURAL, not polled: Sweden's electorate at the 2022 election
            // (7,775,390 eligible, Valmyndigheten) scaled to the one valkrets this action targets.
            // It carries no measurement error, which is why the band's width comes from the polled
            // quantities alone.
            const double audience = 7_775_390.0 * 0.035;
            const double salience = 0.55;
            const double issueMatch = 0.60;
            const double credibility = 0.70;

            var readings = new[]
            {
                new ActionFilmReading("tight", 0.03, 0.04, true, "Novus", 4_000, new DateTime(2026, 8, 19), true),
                new ActionFilmReading("wide", 0.14, 0.18, true, "Novus", 600, new DateTime(2026, 7, 28), false),
                new ActionFilmReading("unmeasured", 0.0, 0.0, false, "—", 0, new DateTime(2026, 8, 19), false),
            };

            foreach (ActionFilmReading reading in readings)
            {
                CampaignActionKind[] legal = CampaignLegality.LegalActions(campaign.Phase);
                var options = new System.Collections.Generic.List<ActionOption>();
                foreach (CampaignActionKind kind in CampaignActions.TheEight)
                {
                    if (!CampaignLegality.IsLegal(kind, campaign.Phase)) { continue; }

                    CampaignActions.ActionSpec spec = CampaignActions.Spec(kind);
                    bool affordable = spec.MoneyCost <= campaign.Resources.Money
                                      && spec.Hours <= campaign.Resources.Hours;

                    // EVERY option is priced, not only the selected one: the decision this screen
                    // serves is which action to run, and that is a comparison. Each band is the same
                    // measurement carried through that action's own spec, so the rows differ by what
                    // the action IS rather than by how well it happens to have been measured.
                    CampaignActions.ChainBand optionBand = CampaignActions.ResolveBand(spec, audience,
                        salience, reading.SalienceError, issueMatch, reading.MatchError,
                        credibility, spec.MoneyCost, reading.Measured);

                    options.Add(new ActionOption(kind,
                        spec.IsLocal ? SwedenValkretsar[0] : "National",
                        spec.MoneyCost, spec.Hours, affordable, optionBand));
                }

                // Television ad: national, expensive, and the one whose estimate a player would most
                // want to price before spending half a million kronor on it.
                int selected = options.FindIndex(o => o.Kind == CampaignActionKind.TelevisionAd);

                var provenance = new EstimateProvenance(reading.House, reading.SampleSize,
                    reading.FieldDate, reading.SalienceError, reading.MatchError, reading.RegionalDetail);

                var snapshot = new ActionScreenSnapshot(campaign, options.ToArray(), selected, provenance);
                CampaignActions.ChainBand band = snapshot.Estimate;

                controller.SetCampaignActionScreen(snapshot);
                yield return Settle();
                yield return Capture($"e3_campaign_action_{reading.Stem}");

                if (reading.Measured)
                {
                    Debug.Log(string.Format(CultureInfo.InvariantCulture,
                        "SHOT: W-E3 {0} - persuasion {1:N0}-{2:N0} (mid {3:N0}) from +/-{4:F0}pp salience and +/-{5:F0}pp match, "
                        + "n={6:N0}; the span is measurement carried through the chain, not an authored margin.",
                        reading.Stem, band.Low.Persuasion, band.High.Persuasion, band.Mid.Persuasion,
                        reading.SalienceError * 100.0, reading.MatchError * 100.0, reading.SampleSize));
                }
                else
                {
                    Debug.Log("SHOT: W-E3 unmeasured - NO estimate printed (§36); an unbought fact is absent, not wide.");
                }

                // The legality list is derived; naming it here keeps the log honest about what the
                // options column was built from rather than implying a hand-written eight.
                if (legal.Length == 0) { Debug.LogError("SHOT: no legal actions in the staged phase - the options column would be empty."); }
            }

            controller.SetCampaignActionScreen(null);
            yield return Settle();
        }


        /// <summary>
        /// W-E2's pass: the campaign map in three readings of the SAME campaign day — nothing
        /// bought, the regional breakdown bought, the full programme bought — so the film shows
        /// §36's gate, the uncertainty it lifts, and the sharpening the bigger sample buys.
        ///
        /// The truth polled is SOURCED: each valkrets's 2018 Riksdag result
        /// (`ElectionsData/sweden/valkrets_votes_2022.csv`, Valmyndigheten's absolute counts, all
        /// eight parties) as that valkrets's preference vector — W-F1 sourced it from the official
        /// per-constituency backend, so the map now polls the 2022 election rather than the 2018
        /// one. Each bought valkrets is
        /// polled by `PollingSystem.Conduct` at the offer's per-valkrets sample (the national n
        /// divided over 29 — what a "regional breakdown" of that size actually affords), seeded per
        /// valkrets so the film is byte-stable. The offer line's ± figures are `MarginOfErrorPp` at
        /// the same per-valkrets samples, quoted at the player's national share.
        /// </summary>
        private IEnumerator CaptureCampaignMap(GameController controller, double perceived)
        {
            var day = new CampaignFilmState("campaign", new DateTime(2026, 8, 21), 1_120_000.0, 12.0, 1_460, 4, 3, 2.2);
            CampaignSnapshot campaign = BuildCampaignSnapshot(day, perceived);

            string[] names;
            double[] weights;
            double[][] truth = ReadValkretsVectors(out names, out weights);
            if (truth == null)
            {
                Debug.LogError("SHOT: the 2018 valkrets file could not be read - the map pass captured NOTHING.");
                yield break;
            }

            MapTile[] layout = SwedenCartogram.Layout();
            var readings = new[]
            {
                ("unbought", "", 0),
                ("regional", "Regional breakdown", 2_400 / 29),
                ("full", "Full internal programme", 6_000 / 29),
            };

            double playerShare = Sweden2022Shares[0];
            string offerLine = string.Format(CultureInfo.InvariantCulture,
                "THE REGIONAL BREAKDOWN (n = 2,400 · 260,000 kr) READS EACH VALKRETS AT ABOUT ±{0:F0} ON YOUR SHARE; " +
                "THE FULL INTERNAL PROGRAMME (n = 6,000 · 620,000 kr) AT ABOUT ±{1:F0}. THE ± IS SAMPLING ERROR ONLY.",
                PollingSystem.MarginOfErrorPp(playerShare, 2_400 / 29), PollingSystem.MarginOfErrorPp(playerShare, 6_000 / 29));

            foreach ((string stem, string bought, int perRegion) in readings)
            {
                var regions = new MapRegionReading[names.Length];
                for (int r = 0; r < names.Length; r++)
                {
                    if (perRegion <= 0)
                    {
                        regions[r] = SwingRegions.Unknown(names[r], weights[r]);
                        continue;
                    }

                    var house = new PollingHouse("Novus", perRegion, 0, new double[truth[r].Length]);
                    Poll poll = PollingSystem.Conduct(truth[r], house, day.Today.AddDays(-2), new System.Random(CampaignFilmSeed + 1000 * perRegion + r));
                    regions[r] = SwingRegions.FromPoll(names[r], weights[r], poll);
                }

                var snapshot = new CampaignMapSnapshot(campaign, regions, layout, Sweden2022Parties, 0, bought, perRegion,
                    day.Today.AddDays(-2), offerLine);
                controller.SetCampaignMapScreen(snapshot);
                yield return Settle();
                yield return Capture($"e2_campaign_map_{stem}");

                int measured = snapshot.MeasuredCount;
                int swing = 0, close = 0;
                foreach (MapRegionReading reading in regions) { if (reading.Measured && reading.SwingIndex >= 60.0) { swing++; } if (reading.TooCloseToCall) { close++; } }
                Debug.Log($"SHOT: map '{stem}' - {measured} of {names.Length} valkretsar read at n = {perRegion} each; {swing} swing (index >= 60), {close} too close to call.");
                if (names.Length != 29) { Debug.LogError($"SHOT: the map staged {names.Length} valkretsar, not 29."); }
            }

            controller.SetCampaignMapScreen(null);
            yield return Settle();
        }

        /// <summary>The 2022 per-valkrets result (W-F1, SOURCED) as a preference vector per valkrets, in the driver's party order (S, SD, M, V, C, KD, MP, L), with each valkrets's share of the national valid vote as its weight.</summary>
        private static double[][] ReadValkretsVectors(out string[] names, out double[] weights)
        {
            names = null; weights = null;
            string path = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "ElectionsData", "sweden", "valkrets_votes_2022.csv"));
            if (!File.Exists(path)) { return null; }

            // csv order: valkrets;valid;S;M;SD;C;V;KD;L;MP -> S, SD, M, V, C, KD, MP, L
            int[] map = { 2, 4, 3, 6, 5, 7, 9, 8 };
            var rows = new List<double[]>();
            var nameList = new List<string>();
            var validList = new List<double>();
            foreach (string raw in File.ReadAllLines(path))
            {
                string line = raw.Trim();
                if (line.Length == 0 || line.StartsWith("#") || line.StartsWith("valkrets;")) { continue; }
                string[] cells = line.Split(';');
                double valid = double.Parse(cells[1], CultureInfo.InvariantCulture);
                var shares = new double[8];
                double partySum = 0.0;
                for (int p = 0; p < 8; p++) { shares[p] = double.Parse(cells[map[p]], CultureInfo.InvariantCulture); partySum += shares[p]; }
                for (int p = 0; p < 8; p++) { shares[p] /= partySum; }   // the eight parties' shares of the eight-party vote
                rows.Add(shares);
                nameList.Add(cells[0]);
                validList.Add(valid);
            }

            double total = 0.0;
            foreach (double v in validList) { total += v; }
            names = nameList.ToArray();
            weights = new double[validList.Count];
            for (int i = 0; i < weights.Length; i++) { weights[i] = validList[i] / total; }
            return rows.ToArray();
        }

        /// <summary>One filmed reading of the SAME action: only the measurement changes between them.</summary>
        private readonly struct ActionFilmReading
        {
            public readonly string Stem;
            public readonly double SalienceError;
            public readonly double MatchError;
            public readonly bool Measured;
            public readonly string House;
            public readonly int SampleSize;
            public readonly DateTime FieldDate;
            public readonly bool RegionalDetail;

            public ActionFilmReading(string stem, double salienceError, double matchError, bool measured,
                string house, int sampleSize, DateTime fieldDate, bool regionalDetail)
            {
                Stem = stem; SalienceError = salienceError; MatchError = matchError; Measured = measured;
                House = house; SampleSize = sampleSize; FieldDate = fieldDate; RegionalDetail = regionalDetail;
            }
        }

        /// <summary>One filmed day: what varies between the three captures.</summary>
        private readonly struct CampaignFilmState
        {
            public readonly string Stem;
            public readonly DateTime Today;
            public readonly double Money;
            public readonly double Hours;
            public readonly int Volunteers;
            public readonly int Offices;
            public readonly int QueuedActions;
            public readonly double MomentumShockPp;

            public CampaignFilmState(string stem, DateTime today, double money, double hours,
                int volunteers, int offices, int queuedActions, double momentumShockPp)
            {
                Stem = stem; Today = today; Money = money; Hours = hours; Volunteers = volunteers;
                Offices = offices; QueuedActions = queuedActions; MomentumShockPp = momentumShockPp;
            }
        }

        private CampaignSnapshot BuildCampaignSnapshot(CampaignFilmState state, double perceivedEconomy)
        {
            CampaignCalendar calendar = CampaignCalendar.Sweden2026;
            CampaignPhase phase = calendar.PhaseOn(state.Today);

            // The poll: a real draw against the sourced vector, by a house with no lean, so what the
            // screen shows differs from the truth only by honest sampling error.
            var house = new PollingHouse("Novus", 1_200, 120_000, new double[Sweden2022Shares.Length]);
            Poll poll = PollingSystem.Conduct(Sweden2022Shares, house, state.Today.AddDays(-2),
                new System.Random(CampaignFilmSeed + state.Stem.Length));

            // Momentum: a real shock decayed on §22's half-life over the days since it landed.
            var momentum = new MomentumTracker(Sweden2022Shares.Length);
            if (Math.Abs(state.MomentumShockPp) > 0.0)
            {
                momentum.AddShock(0, state.MomentumShockPp);
                momentum.Advance(3);
            }

            var momentumPp = new double[Sweden2022Shares.Length];
            for (int i = 0; i < momentumPp.Length; i++) { momentumPp[i] = momentum.MomentumPp(i); }

            // The queue: every cost read from the action's own spec, never typed here.
            //
            // ⚠ Queued from `CampaignActions.TheEight` INTERSECTED with the phase's legality, not
            // from the legality list alone. Only §12's eight have specs - `Spec` throws
            // "RecruitStaff is not one of §12's eight campaign actions" for the §3 preparation
            // verbs, which `LegalActions(Campaign)` legitimately includes. The first film caught it
            // as an ArgumentException mid-capture. The queue is a queue of §12 actions, which is
            // what §12's queue is; a pre-campaign day therefore shows the empty state, correctly.
            var specced = new System.Collections.Generic.List<CampaignActionKind>();
            foreach (CampaignActionKind kind in CampaignActions.TheEight)
            {
                if (CampaignLegality.IsLegal(kind, phase)) { specced.Add(kind); }
            }

            var queue = new QueuedAction[Mathf.Min(state.QueuedActions, specced.Count)];
            for (int i = 0; i < queue.Length; i++)
            {
                CampaignActions.ActionSpec spec = CampaignActions.Spec(specced[i]);
                queue[i] = new QueuedAction(specced[i],
                    spec.IsLocal ? SwedenValkretsar[i % SwedenValkretsar.Length] : "National",
                    spec.MoneyCost, spec.Hours);
            }

            // Staff: the post, whether it is filled, and the draft bonus. No invented people.
            var staff = state.Offices == 0
                ? new StaffMember[0]
                : new[]
                {
                    new StaffMember("Campaign manager", "Filled", "[AUTHORED-DRAFT] +10 % action effect", CampaignStaff.SalaryPerDay),
                    new StaffMember("Press secretary", "Filled", "[AUTHORED-DRAFT] +15 % earned media", CampaignStaff.SalaryPerDay),
                    new StaffMember("Field director", "Filled", "[AUTHORED-DRAFT] +20 % volunteer hours", CampaignStaff.SalaryPerDay),
                    new StaffMember("Pollster", "Vacant", "—"),
                };

            var offices = new RegionalOffice[Mathf.Min(state.Offices, SwedenValkretsar.Length)];
            for (int i = 0; i < offices.Length; i++)
            {
                offices[i] = new RegionalOffice(SwedenValkretsar[i], 120 + i * 85, 4_000 + i * 1_500);
            }

            return new CampaignSnapshot(
                "Socialdemokraterna", "mark_party_se_s", "Sweden", phase, state.Today, calendar,
                new ResourcePool(state.Money, state.Hours, state.Volunteers),
                2_400_000.0, poll, Sweden2022Parties, 0, momentumPp,
                queue, staff, offices, perceivedEconomy);
        }

        /// <summary>§19's index off the LIVE warmed-up country — the one figure on the screen that
        /// comes from the running game rather than from staging.</summary>
        private double ReadPerceivedEconomy(GameController controller)
        {
            FieldInfo simField = controller.GetType().GetField("_simulationManager", BindingFlags.Instance | BindingFlags.NonPublic);
            Country player = (simField?.GetValue(controller) as SimulationManager)?.World?.GetCountry(_countryId);
            if (player == null)
            {
                Debug.LogError("SHOT: the live country could not be read - the perceived-economy figure would be invented, so the pass fails rather than guessing.");
                return double.NaN;
            }

            return PerceivedPerformance.Perceived(player, null).Index;
        }

        /// <summary>The ladder's kinds, in the order they are filmed - one capture each, named `ladder_{kind}`.</summary>
        private static readonly string[] LadderKinds =
        {
            "map", "compass", "web", "sparkline", "chipstrip", "tile", "graph", "trace", "sheet", "chip",
            "stamp", "stampchip", "steps", "hemicycle", "pie", "flag", "icon", "row", "bars", "dot"
        };

        /// <summary>
        /// C-C10 (P-G2): a game where the player actually governed, so the impact ledger has something
        /// to attribute and the Statistics sheet can be filmed with it populated.
        ///
        /// <para>Three families are held down for the whole run — taxes, spending and crime-and-justice —
        /// chosen because they are the three that the feasibility harness measured as having live,
        /// separable effects. ⚠ <b>The dials are read off the country's own seeded values</b> (its income
        /// tax rate, its police funding level) rather than set to authored numbers, so nothing on this
        /// film is a figure this harness invented.</para>
        ///
        /// <para>⚠ The shadow and the ledger are advanced by reflection through the controller's own
        /// fields rather than through a hook added to `GameController` for the harness's benefit — the
        /// discipline this file states at the top and the reason it reaches private state everywhere
        /// else. Advancing the ledger BEFORE the real turn is not incidental: a family first touched on a
        /// turn forks from that turn's opening state, which is what makes the fork exact.</para>
        /// </summary>
        private IEnumerator CaptureImpactLedger(GameController controller)
        {
            FieldInfo simField = controller.GetType().GetField("_simulationManager", BindingFlags.Instance | BindingFlags.NonPublic);
            FieldInfo worldField = controller.GetType().GetField("_world", BindingFlags.Instance | BindingFlags.NonPublic);
            FieldInfo shadowField = controller.GetType().GetField("_shadowBaseline", BindingFlags.Instance | BindingFlags.NonPublic);
            FieldInfo ledgerField = controller.GetType().GetField("_impactLedger", BindingFlags.Instance | BindingFlags.NonPublic);

            var sim = simField?.GetValue(controller) as SimulationManager;
            var world = worldField?.GetValue(controller) as World;
            var shadow = shadowField?.GetValue(controller) as ShadowBaseline;
            var ledger = ledgerField?.GetValue(controller) as PolicyImpactLedger;

            if (sim == null || world == null || shadow == null || ledger == null)
            {
                Debug.LogError("SHOT: could not reach the simulation, the shadow baseline or the impact ledger - "
                               + "the ledger film would show the EMPTY state while claiming to show the populated one.");
                Finish(1);
                yield break;
            }

            var player = controller.GetType()
                .GetField("_playerCountry", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.GetValue(controller) as Country;

            if (player == null)
            {
                Debug.LogError("SHOT: could not reach the player country - the ledger film would have no dials to move.");
                Finish(1);
                yield break;
            }

            for (int turn = 0; turn < LedgerTurns; turn++)
            {
                var decisions = new Dictionary<CountryId, PolicyDecision>();
                foreach (Country c in world.Countries) { decisions[c.Id] = PolicyDecision.None(); }

                var acting = new PolicyDecision();
                foreach (TaxLine line in player.TaxLines)
                {
                    if (line.Type == TaxType.IncomeTax && line.IsImplemented)
                    {
                        acting.TaxRateOverrides[TaxType.IncomeTax] = line.Rate + 8f;
                    }
                }

                acting.SpendingLineChanges[SpendingCategory.Defense] = 6f;
                acting.PoliceFundingOverride = Mathf.Min(100f, player.PoliceFundingLevel + 20f);
                decisions[player.Id] = acting;

                ledger.AdvanceTurn(sim, world, player.Id, decisions);
                for (int d = 0; d < SimulationManager.DaysPerTurn; d++) { sim.AdvanceDay(); }
                sim.AdvanceTurn(decisions);
                shadow.AdvanceTurn();
            }

            // ⚠ Leave the Desk first. A tab set by field alone while `_onDesk` is true films the DESK
            // under the tab's name - the sweep's own note, and the first ledger film hit exactly that.
            SetPrivateField(controller, "_onDesk", false);
            SetEnumField(controller, "_consolidatedTab", "Statistics");
            SetEnumField(controller, "_statisticsCategory", "Domestic");
            ResetScrolls(controller);
            yield return Settle();
            yield return Capture("c10_ledger_top");

            SetScrolls(controller, 900f);
            yield return Settle();
            yield return Capture("c10_ledger_scrolled");

            ReportOverflows();
            Debug.Log($"SHOT: impact ledger done, {_captured} captured, {_failed} failed.");
            Finish(_failed == 0 && _loggedErrors == 0 ? 0 : 1);
        }

        /// <summary>Enough turns for the divergence to be visible rather than a rounding difference, and
        /// few enough that the film run stays short.</summary>
        private const int LedgerTurns = 10;

        private IEnumerator CaptureInstrumentLadder(GameController controller)
        {
            foreach (string kind in LadderKinds)
            {
                controller.SetInstrumentLadder(kind);
                yield return Settle();
                yield return Capture($"ladder_{kind}");
            }

            controller.SetInstrumentLadder(null);
            yield return Settle();

            // The guards' counts on a ladder run are the ladder's own breaks - it shrinks instruments
            // until they fail, and a break the guard records is a measurement, not a defect. Reported,
            // not gated: the run is clean when every capture wrote and nothing else errored. The
            // error fold is read BEFORE the reports print (their OVERFLOW lines are LogErrors, and
            // counting them made the first ladder runs exit 1 on their own measurement).
            int errorsDuringCaptures = _loggedErrors;
            Debug.Log($"SHOT: ladder done, {_captured} captured, {_failed} failed.");
            Debug.Log($"SHOT: {ReportOverflows()} text overflow(s) recorded - on a ladder run these are the rungs that broke, reported not gated.");
            Debug.Log($"SHOT: {ReportContainmentEscapes()} containment escape(s) recorded - on a ladder run these are the rungs that broke, reported not gated.");
            Finish(_failed == 0 && errorsDuringCaptures == 0 ? 0 : 1);
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
