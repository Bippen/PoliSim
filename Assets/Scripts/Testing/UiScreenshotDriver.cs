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

        /// <summary>Run just before the process exits, given the exit code the run wants, returning the
        /// code it should actually use. ⚠ **M-S16 (2026-09-01): this exists so a guard over the films
        /// cannot be forgotten.** `ScreenEdgeCheck` refuses to run without film - correctly - and was
        /// therefore in neither batch, firing only when somebody remembered it after a capture pass. That
        /// is the failure mode `C-N3` fixed for the simulation group. This driver lives in the runtime
        /// assembly and cannot reference Editor code, so the Editor side installs the hook and this side
        /// only promises to call it. **The returned code is the WORSE of the two**: a clean capture whose
        /// films are clipped is not a clean run.</summary>
        public System.Func<int, int> BeforeExit;
        /// <summary>Which country to play as, set from `-shotcountry=` (default USA — the only country any set contained before 2026-08-12). Parsed against CountryId in Start and FAILS the run on a bad name: a typo must not silently capture the default country under the requested country's label.</summary>
        public string Country = "USA";

        /// <summary>The parsed <see cref="Country"/>, held so the warm-up's publication check and the held-state pass read the country actually being captured rather than a hardcoded USA.</summary>
        private CountryId _countryId = CountryId.USA;

        /// <summary>Set by `-shotstates`: run the state-pinning pass (cabinet, budget pause, decision search, pending bills) after the main sweep. Off by default so ordinary chrome runs stay fast and their sets comparable with history.</summary>
        public bool PinStates;

        /// <summary>§676 (SP-4), set by `-shotcreatestart`: the creation flow ends in START (the draft registered, the player seated as it) instead of BACK.</summary>
        public bool CreateAndStart;

        /// <summary>Set by `-shotsaves` (R-D4, the clear-out kickoff of 2026-08-28): instead of the sweep, stage the
        /// playtest saves - one per felt verdict in COMPLETED.md §198 §P - through the REAL save service
        /// into the real saves directory, each state filmed once as proof, so §P is load-play-judge rather than
        /// set-up-first. USA stages the Trade-bill and dense-mid-game saves; Sweden the Riksbank-B one.</summary>
        public bool StageSaves;
        /// <summary>P4-2: `-cuesweep` fires every cue in the catalog by name after the sweep's first capture and asserts each cue with a file loaded its clip - the harness's own trigger, beside the Editor cue board.</summary>
        public bool CueSweep;
        /// <summary>Set by `-shotstop=&lt;capture name&gt;` (2026-09-04, the 1920 wedge): the sweep ends the moment that
        /// capture is written, judged by the same fold as a whole sweep over what it filmed. A bisect at one width
        /// then costs the frames up to the one in question rather than the set. ⚠ The log names the stop and the
        /// count, so a partial set can never pass for a sweep; a name no capture carries stops nothing and the
        /// sweep runs out as usual - the film's own count says which happened.</summary>
        public string StopAfter = "";

        /// <summary>Set by `-shotload=&lt;save name&gt;` (2026-09-21, §560, the sitting package): once the game has landed on the Desk the named save is loaded through the
        /// controller's ONE load path (the saves menu's own), so every frame after it is of that game and not of the harness's fresh world. The save's digest, date and
        /// player are logged, and a save that is missing, refused, or another country's ends the run: a film of the wrong game under this flag's name is the one outcome it
        /// exists to prevent. The sweep's warm-up then plays the LOADED game forward; the modes that drive the campaign's own day path (`-shotinterrupts`) or stage a
        /// dated reading over the game (`-shotcampaign`, `-shotelectionnight`) skip it, so the run-up they open on is the one the save was cut in.</summary>
        public string LoadSave = "";
        /// <summary>§785: -shotparty=&lt;key&gt; - the film's player seated through the picker's own commit (`SelectPlayerCountryAndParty`, CL-2) instead of
        /// the largest party, so a film can play a party the largest is not: Poland's KO, in government after the warm-up's count of 2023, whose drafts
        /// the President's backing party votes against - the veto's at-risk faces, owed on film since §778. Empty: the largest party, as before.</summary>
        public string Party = "";
        private DateTime _loadedDate = DateTime.MinValue;

        /// <summary>Set by `-shotlocale=` (e.g. "en-US"): overrides the thread culture before anything draws, so number/date formatting can be captured in a locale other than the OS's. Empty = OS culture, which is what every set before 2026-08-12 rendered in (sv-SE on this machine — the decimal-comma set).</summary>
        public string Locale = "";

        /// <summary>C-C7 follow-up / trap 2: the width the caller ASKED for, so every capture can assert
        /// it got it. Zero means no size was requested and nothing is checked - see the mismatch guard in
        /// Capture, and the run it was armed for (a 1280 pass that silently filmed at the 1600 default
        /// and reported 77 captured, 0 failed).</summary>
        public int ExpectedWidth;

        /// <summary>The HEIGHT the caller asked for. ⚠ **Added 2026-09-01 by R-T3, and it was missing for
        /// a month.** Trap 2 was armed for width alone; `-shotheight=` is set on the SAME line of
        /// `UiScreenshotCapture`, from the SAME ruling, with its own silent default of 950 — and nothing
        /// ever compared it with what came back. **A capture silently the wrong height is wrong in exactly
        /// the way the run that cost this project a week was wrong**, and the only reason width was
        /// guarded and height was not is that the failure happened to arrive on the width axis.
        /// R-T3's whole point is that a fix to one consumer is a claim about one call site until the
        /// others are enumerated. Zero means no size was requested and nothing is claimed.</summary>
        public int ExpectedHeight;

        /// <summary>**THE DRY FILM (2026-09-17)** - `-shotdry`: the same sweep under `-batchmode`, where no Game View
        /// delivers OnGUI. The driver delivers it (<see cref="DryGuiPass"/>) once a frame and once more at each capture,
        /// and a capture measures instead of photographing: the guards, the ledger reach and the label table run as in
        /// a film; nothing is written but the table. The pixel checks (the identity token, the frame size, the edge
        /// guard) need pixels and stay the film's.</summary>
        public bool Dry;

        /// <summary>Set by the Editor side's dry loop: called with the sweep's exit code INSTEAD of exiting, so one Unity
        /// process can run the next country or width.</summary>
        public System.Action<int> DryExit;

        /// <summary>The last sweep's counters, read by the dry loop's summary before the next play session reloads the domain.</summary>
        public static string LastSweepSummary = string.Empty;

        private GameController _dryController;
        private MethodInfo _dryOnGui;
        /// <summary>§705: the page tour's controller, set where the tour begins (the special modes have branched off by then) - a sheet the game
        /// opened by itself is closed before each of the tour's frames (<see cref="Capture"/>), unless 07a is staging it.</summary>
        private GameController _tourController;
        private bool _stagingFormationSheet;

        /// <summary>Frames to let IMGUI settle before a capture. IMGUI lays out on the frame it draws, so a screen switched to on frame N is not fully measured until N+1; four is cheap insurance rather than a measured minimum.</summary>
        private const int SettleFrames = 4;

        private static readonly string[] Tabs =
        {
            "Statistics", "Decisions", "Demographics", "Budget", "PolicyLaws", "Politics",
            "Energy",   // P6-F1 (2026-09-17): the ninth rail cell; last, so the stems of the tabs before it do not move
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

            // MM-2 (2026-09-24): EVERY PREFERENCE PINNED before the first frame and restored in Finish, so a film sees the game as a
            // fresh player does and leaves the desk's preferences as it found them (SettingsCheck holds the list to the sources).
            Debug.Log($"SHOT: preferences pinned - {PreferencePins.Pin()} key(s) snapshotted and cleared for the film; restored when it finishes.");
            // Elias (2026-10-14): A FILM IS SILENT - the listener's volume, never a preference: nothing drawn changes (the settings sheet reads the
            // preferences) and AudioDirector still records every cue it is asked to fire; the game never writes the listener's volume.
            AudioListener.volume = 0f;
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
                // §718: the game's one fixed English culture replaces the requested one, exactly as it does over a machine's - so -shotlocale= plays a foreign
                // machine and proves NOTHING moves with it, dates or numbers (§568 had kept the dates in the requested culture).
                UiCulture.Install();
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

            // THE CURSOR, PARKED (the candidate's second half) - see ParkCursor's own doc. A dry film has no
            // window for a cursor to hover over, and moving the OS cursor would only disturb whoever is at the desk.
            if (!Dry) { ParkCursor(); }

            Directory.CreateDirectory(OutputDirectory);

            controller = FindAnyObjectByType<GameController>();
            if (controller == null)
            {
                Debug.LogError("SHOT: no GameController in scene - nothing to capture.");
                Finish(1);
                yield break;
            }

#if UNITY_EDITOR
            // The label table's hook, in both kinds of film, so a film and a dry film of one tree can be diffed row by row.
            DryGuiPass.InstallHook();
            if (Dry)
            {
                _dryOnGui = typeof(GameController).GetMethod("OnGUI", BindingFlags.Instance | BindingFlags.NonPublic);
                if (!DryGuiPass.Prepare() || _dryOnGui == null)
                {
                    Debug.LogError($"SHOT: DRY - the IMGUI pass cannot be driven on this Unity ({DryGuiPass.Failure ?? "GameController.OnGUI"} not found). A dry film that cannot lay out measures nothing, so it stops here.");
                    Finish(1);
                    yield break;
                }

                _dryController = controller;
                Debug.Log($"SHOT: DRY - the sweep runs without a window at {UiScreen.Width}x{UiScreen.Height}; OnGUI is delivered by the driver once a frame and at every capture.");
            }
#endif

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

            // MM-1 (2026-09-24): THE MAIN MENU is the first screen now. Filmed as the player meets it, then NEW GAME chosen
            // through the controller's own entry (the click's method), and the seam waited through the menu's exit and the
            // selector's entrance before the selector's own frame - so every frame after this one is what it always was.
            Expect("00_main_menu", "00a_settings");   // TL-1 (§645): the menu is every film's first screen
            if (controller.MainMenuLive)
            {
                Claim("menu");
                yield return Capture("00_main_menu");
                RecordCanvasTextAssert("00_main_menu", controller);

                // MM-2: the settings screen as the menu opens it - no game behind it - then back to the menu through its own Close.
                Invoke(controller, "ChooseMainMenu", MainMenuChoice.Settings);
                yield return WaitForCanvasSettle(controller, wantActive: false);
                yield return Settle();
                Claim("imgui");
                yield return Capture("00a_settings");
                Invoke(controller, "CloseSettings");
                yield return WaitForCanvasSettle(controller, wantActive: true);
                yield return Settle();
                Debug.Log("SHOT: MM-2 - the settings screen filmed from the menu and closed back to it.");

                Invoke(controller, "ChooseMainMenu", MainMenuChoice.NewGame);
                yield return WaitForCanvasSettle(controller, wantActive: false);
                yield return WaitForCanvasSettle(controller, wantActive: true);
                yield return Settle();
                Debug.Log("SHOT: MM-1 - the main menu filmed, NEW GAME chosen, the selector entered.");
            }
            else
            {
                Debug.LogWarning("SHOT: MM-1 - the main menu was not the live surface at the first settle; 00_main_menu NOT captured.");
            }

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

            // SP-1 (§622): the start panel - the selector's step between the folder and the party - filmed for the country, then hidden; the
            // party panel below is still opened directly, so no frame after this one moves.
            Expect("01f2_start_points");   // TL-1 (§645): filmed only on the live Canvas selector - its absence fails the film
            if (controller.CanvasSelectorActive)
            {
                object startSelector = controller.GetType().GetField("_countrySelector", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(controller);
                object startWorld = controller.GetType().GetField("_world", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(controller);
                Country startCountry = (startWorld as World)?.GetCountry(_countryId);
                if (startSelector is CountrySelectorScreen startScreen && startCountry != null)
                {
                    startScreen.ShowStartPanel(startCountry, null);
                    yield return Settle();
                    Claim("selector");
                    yield return Capture("01f2_start_points");
                    RecordCanvasTextAssert("01f2_start_points", controller);
                    startScreen.HidePartyPanel();
                    yield return Settle();
                    Debug.Log($"SHOT: SP-1 - the start panel filmed for {_countryId}: {StartPoints.For(_countryId).Count} start point(s).");

                    // SP-7b (Elias's ruling I14): Poland's presidential start - the sheet with its card selected (the Marshal's order, no campaign, the brief's
                    // contest rows), then the candidate picker its SELECT opens (the five of record a Sejm party backs, the eight shown with their reason);
                    // both filmed, then hidden, so no frame after them moves. Canvas surfaces: a REAL film reads them.
                    if (_countryId == CountryId.Poland)
                    {
                        Expect("01f3_start_presidential", "01g3_candidate_picker");
                        startScreen.ShowStartPanel(startCountry, null, "PRESIDENTIAL ELECTION");
                        yield return Settle();
                        Claim("selector");
                        yield return Capture("01f3_start_presidential");
                        RecordCanvasTextAssert("01f3_start_presidential", controller);
                        startScreen.ShowCandidatePanel(startCountry, StartPoints.For(CountryId.Poland)[1]);
                        yield return Settle();
                        Claim("selector");
                        yield return Capture("01g3_candidate_picker");
                        RecordCanvasTextAssert("01g3_candidate_picker", controller);
                        startScreen.HidePartyPanel();
                        yield return Settle();
                        Debug.Log($"SHOT: SP-7b - the presidential card and the candidate picker filmed: {PresidentialStart.Offers(StartPoints.For(CountryId.Poland)[1]).Count} candidates of record.");
                    }
                }
            }

            // CL-2 (2026-09-13, DS-6): the party picker over the selector - the country's seeded chamber, largest first, the cabinet the
            // chamber forms marked - filmed on the Canvas selector, then hidden; the run then seats the party the one-argument shape
            // seats (the largest), as every film before the picker did, so no frame after this one moves.
            Expect("01g_party_picker");   // TL-1 (§645) - 01h, the scenario's party pick, left with the scenarios (§825)
            if (controller.CanvasSelectorActive)
            {
                object selector = controller.GetType().GetField("_countrySelector", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(controller);
                object world = controller.GetType().GetField("_world", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(controller);
                Country pickCountry = (world as World)?.GetCountry(_countryId);
                if (selector is CountrySelectorScreen canvasSelector && pickCountry != null)
                {
                    canvasSelector.ShowPartyPanel(pickCountry, null);
                    yield return Settle();
                    Claim("selector");   // S-20: the panel is the selector's own Canvas surface
                    yield return Capture("01g_party_picker");
                    RecordCanvasTextAssert("01g_party_picker", controller);
                    canvasSelector.HidePartyPanel();
                    yield return Settle();
                    Debug.Log($"SHOT: CL-2 - the party picker filmed for {_countryId}: {CountrySelectorScreen.PartiesBySeats(_countryId).Count} parties, largest first.");

                }
                else
                {
                    Debug.LogWarning("SHOT: CL-2 - the Canvas selector was not reachable; the party picker is NOT filmed.");
                }
            }

            // §676 (SP-4): THE CREATION FLOW - the party panel's CREATE A PARTY, then the five steps on one filled draft (the profile with an
            // origin card's slip pinned), then BACK to the selector through the flow's own close. Nothing is registered, so no frame after these moves.
            if (controller.CanvasSelectorActive && PartyCreationFlow.Offered(_countryId))
            {
                string[] steps = { "01k1_create_profile", "01k2_create_placement", "01k3_create_declarations", "01k4_create_leader", "01k5_create_review" };
                Expect(steps);
                Invoke(controller, "RequestPartyCreation", _countryId);
                yield return WaitForCanvasSettle(controller, wantActive: false);
                yield return Settle();
                if (controller.GetType().GetField("_pcDraft", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(controller) is CreatedParty draft)
                {
                    draft.Name = "Framtidspartiet";
                    draft.ShortName = "Framtid";
                    draft.Key = PartyCreationFlow.KeyFrom(_countryId, draft.ShortName);
                    draft.LeaderName = "Maja Ek";
                    draft.RedLinesAgainst.Add("SD");
                    draft.OneWayAgainst.Add("V");
                    draft.BacksCandidateOf = draft.Key;
                    draft.LrEcon = 4.5f;
                    draft.Galtan = 3.0f;
                }
                for (int s = 0; s < steps.Length; s++)
                {
                    SetPrivateField(controller, "_pcStep", s);
                    if (s == 0) { Invoke(controller, "PinSlipForFilm", "origin/Grassroots", null, new Vector2(UiScreen.Width * 0.58f, UiScreen.Height * 0.40f)); }
                    yield return Settle();
                    Claim("imgui");
                    yield return Capture(steps[s]);
                }
                if (CreateAndStart)
                {
                    // -shotcreatestart: START through the flow's own commit - the draft registered, the world rebuilt on the roster that carries it,
                    // the player seated as it - and the desk filmed with the created party seated. The film runs on as that party (cut it with -shotstop=).
                    Expect("01k6_created_seated");
                    Invoke(controller, "CommitPartyCreation");
                    SetPrivateField(controller, "_daySpeedTimer", -ClockHoldSeconds);
                    yield return Settle();
                    Country seatedIn = controller.GetType().GetField("_playerCountry", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(controller) as Country;
                    Debug.Log($"SHOT: SP-4 - START: the cast {string.Join(" ", LiveCampaignSetup.Keys(_countryId))}; the seated party {seatedIn?.PlayerPartyAbbrev}; campaign capital for it: {seatedIn?.PartyCapital.Exists(c => c.PartyAbbrev == seatedIn.PlayerPartyAbbrev)}.");
                    Claim("imgui");
                    yield return Capture("01k6_created_seated");
                }
                else
                {
                    Invoke(controller, "ClosePartyCreation");
                    yield return WaitForCanvasSettle(controller, wantActive: true);
                    yield return Settle();
                    Debug.Log($"SHOT: SP-4 - the creation flow filmed for {_countryId}: five steps, closed back to the selector, nothing registered.");
                }
            }

            // PS-6 US-33 (SP-6a): THE CREATION FLOW'S US FORM - the party panel's YOUR OWN NOMINEE (01g_party_picker shows it), then the four steps on one filled
            // draft (the ticket's slip pinned on the first; the nominee placed off the party's own point, at its range's economic floor and a quarter up its GAL-TAN
            // span, so the placement and the review show a moved point), then BACK to the selector through the flow's own close. Nothing is registered, so no frame
            // after these moves. -shotcreatestart ends it in START instead: the nominee registered, the world rebuilt, the player seated as its party - and the desk
            // filmed. The ticket is -shotparty's where it is one, else the first offered.
            if (controller.CanvasSelectorActive && OwnNomineeFlow.Offered(_countryId))
            {
                string[] steps = { "01n1_nominee_ticket", "01n2_nominee_placement", "01n3_nominee_name", "01n4_nominee_review" };
                Expect(steps);
                Invoke(controller, "RequestPartyCreation", _countryId);
                yield return WaitForCanvasSettle(controller, wantActive: false);
                yield return Settle();
                List<string> tickets = OwnNominees.Tickets(_countryId);
                string ticket = !string.IsNullOrEmpty(Party) && tickets.Contains(Party) ? Party : tickets.Count > 0 ? tickets[0] : null;
                OwnNominee draft = controller.GetType().GetField("_onDraft", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(controller) as OwnNominee;
                if (draft != null && ticket != null)
                {
                    OwnNomineeFlow.ChooseTicket(draft, ticket);
                    if (OwnNominees.TryRangeOf(_countryId, ticket, out OwnNominees.Range range)) { OwnNomineeFlow.Place(draft, range.EconLo, range.SocLo + 0.25f * (range.SocHi - range.SocLo)); }
                    draft.Name = "Jordan Hale";
                }
                else { Debug.LogWarning($"SHOT: US-33 - the own-nominee flow opened no draft for {_countryId} (ticket {ticket ?? "none"}); its frames show what the controller drew."); }
                for (int s = 0; s < steps.Length; s++)
                {
                    SetPrivateField(controller, "_onStep", s);
                    if (s == 0 && ticket != null) { Invoke(controller, "PinSlipForFilm", "ticket/" + ticket, null, new Vector2(UiScreen.Width * 0.58f, UiScreen.Height * 0.40f)); }
                    yield return Settle();
                    Claim("imgui");
                    yield return Capture(steps[s]);
                }
                if (CreateAndStart)
                {
                    Expect("01n5_nominee_seated", "01n6_nominee_parliament_rows");
                    Invoke(controller, "CommitOwnNominee");
                    SetPrivateField(controller, "_daySpeedTimer", -ClockHoldSeconds);
                    yield return Settle();
                    Country seatedIn = controller.GetType().GetField("_playerCountry", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(controller) as Country;
                    bool registered = OwnNominees.TryOf(_countryId, out OwnNominee own);
                    Debug.Log($"SHOT: US-33 - START: the own nominee {(registered ? own.Name + " at (" + own.LrEcon.ToString("0.0", CultureInfo.InvariantCulture) + ", " + own.Galtan.ToString("0.0", CultureInfo.InvariantCulture) + ") heads the " + own.Party + " ticket" : "NOT registered")}; the seated party {seatedIn?.PlayerPartyAbbrev}.");
                    Claim("imgui");
                    yield return Capture("01n5_nominee_seated");

                    // the US-33 review's L6: the Parliament tab's nominee row in its own-nominee form - drawn only before the 2024 count, which every film's
                    // warm-up crosses before 07a - filmed here, on the start's day, at the rows' scroll; the tab, its page and the Desk then put back as they were
                    FieldInfo onDesk = controller.GetType().GetField("_onDesk", BindingFlags.Instance | BindingFlags.NonPublic);
                    FieldInfo tab = controller.GetType().GetField("_consolidatedTab", BindingFlags.Instance | BindingFlags.NonPublic);
                    FieldInfo page = controller.GetType().GetField("_politicsCategory", BindingFlags.Instance | BindingFlags.NonPublic);
                    object deskWas = onDesk?.GetValue(controller), tabWas = tab?.GetValue(controller), pageWas = page?.GetValue(controller);
                    SetPrivateField(controller, "_onDesk", false);
                    bool staged = SetEnumField(controller, "_consolidatedTab", "Politics") && SetEnumField(controller, "_politicsCategory", "Parliament");
                    ResetScrolls(controller);
                    yield return Settle();
                    ScrollBy(controller, CongressRowsScroll(controller, 900f));   // PS-6 US-35: the Congress view stands taller - scrolled to the rows by their top
                    yield return Settle();
                    Claim("imgui");
                    yield return Capture("01n6_nominee_parliament_rows");
                    Debug.Log($"SHOT: US-33 - the Parliament tab before the count ({(staged ? "staged" : "NOT staged")}): the nominee row reads the own nominee {(registered ? own.Name : "-")} for {seatedIn?.PlayerPartyAbbrev}, polling day 2024-11-05.");
                    ResetScrolls(controller);
                    if (tab != null && tabWas != null) { tab.SetValue(controller, tabWas); }
                    if (page != null && pageWas != null) { page.SetValue(controller, pageWas); }
                    if (onDesk != null && deskWas != null) { onDesk.SetValue(controller, deskWas); }
                    yield return Settle();
                }
                else
                {
                    Invoke(controller, "ClosePartyCreation");
                    yield return WaitForCanvasSettle(controller, wantActive: true);
                    yield return Settle();
                    Debug.Log($"SHOT: US-33 - the own-nominee flow filmed for {_countryId}: four steps on the {ticket} ticket, closed back to the selector, nothing registered ({OwnNominees.Any(_countryId)}).");
                }
            }

            if (string.IsNullOrEmpty(Party)) { Invoke(controller, "SelectPlayerCountry", _countryId); }
            else
            {
                Invoke(controller, "SelectPlayerCountryAndParty", _countryId, Party);   // §785: the picker's own commit - an unknown key is the picker's error, and the largest party
                Debug.Log($"SHOT: -shotparty={Party} - seated through the picker's own commit, not the largest party.");
            }
            // §510 (2026-09-15): PLAY'S CLOCK IS HELD FROM HERE THROUGH THE WARM-UP, invisibly - its accumulator is primed an hour short
            // of a day, so the speed, the lamp and the running captures stay what they are and no real-time day can pass before the
            // warm-up (a slow frame used to pass one - see ReportClockBeforeWarmup). Released, to zero, the moment the warm-up ends.
            SetPrivateField(controller, "_daySpeedTimer", -ClockHoldSeconds);

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

            // §560: the save, loaded the way a player loads it - after the game has landed, through the saves menu's own path - then the Desk AS LOADED (a load always
            // resumes PAUSED), then play pressed so the two frames below are the RUNNING state their names promise. The clock stays held: no real-time day passes.
            if (!string.IsNullOrEmpty(LoadSave))
            {
                if (!LoadNamedSave(controller)) { Finish(1); yield break; }
                SetPrivateField(controller, "_daySpeedTimer", -ClockHoldSeconds);
                yield return Settle();
                yield return Capture("01l_desk_as_loaded");
                AssertDeskState(controller, "01l_desk_as_loaded");
                SetEnumField(controller, "_gameSpeed", "Normal");
                yield return Settle();
            }

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
            AssertMapChips(controller, "01c_desk");   // §750: the Desk's World trade is the same renderer and the v3.5 chip - its plates met at 1280 (FR under DE) with no assert to say so

            if (CueSweep) { RunCueSweep(); }

            // P3-C2 (2026-09-03): the Budget at TURN 0 - before the warm-up closes a year - so the fiscal header is filmed
            // with the seed named (no year closed) beside the year's projected balance.
            SetPrivateField(controller, "_onDesk", false);
            SetEnumField(controller, "_consolidatedTab", "Budget");
            yield return Settle();
            yield return Capture("05t_budget_turn0");
            SetPrivateField(controller, "_onDesk", true);
            yield return Settle();

            ReportClockBeforeWarmup(controller);
            bool opensOnTheSave = !string.IsNullOrEmpty(LoadSave) && (Interrupts || CampaignHq || ElectionNightBoard);
            if (opensOnTheSave) { Debug.Log($"SHOT: -shotload - no warm-up in this mode: it opens on the save's own day, {_loadedDate:yyyy-MM-dd}."); }
            // PS-2 / CL-4 (§619): the interrupts film plays ONE country's game from its own start - the run-up, the campaign and the election on the
            // country's real polling day (Sweden's 13 September 2026, 238 days in). A warm-up would walk past that polling day with no election held
            // (the warm-up drives the manager, not the controller) and film the NEXT cycle's night against a chamber play never voted in - the
            // 2030 night the first film of §619 showed, with no history beside it. So no warm-up here: the film opens on day 0.
            else if (Interrupts) { Debug.Log("SHOT: -shotinterrupts - no warm-up: the film plays the country's game from its start to its first polling day (PS-2, §619)."); }
            // PS-6 US-36: the US journey plays its game from the start too - the run-up, the campaign and the 2024 count are what it films
            else if (UsJourney) { Debug.Log("SHOT: -shotusjourney - no warm-up: the film plays the US game from its start through the 2024 count to the 2026 midterm (US-36)."); }
            else if (Abroad) { Debug.Log("SHOT: -shotabroad - no warm-up: the film plays the game from its start to the day after the AI USA's first count (PS-8, K163)."); }
            else
            {
                AdvanceDays(controller, _countryId);
                // AF-30b (J5): a governing player's start now seats the average cabinet, and its ministers roll cabinet decisions at every boundary the
                // warm-up crosses - which the warm-up, driving the manager, never answers. Left pending they hold the controller's clock (GameController.Update),
                // so the day the round opens after the warm-up's election never ticks (drys875: Germany's formation sheet never filmed). The warm-up is
                // "time passed with no policy changes": its unanswered decisions are dropped, unanswered, as the clean Docket the sweep stages its own on.
                if (controller.GetType().GetField("_simulationManager", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(controller) is SimulationManager warmed)
                {
                    List<(CabinetPortfolio Portfolio, CabinetDecision Decision)> warmupDecisions = warmed.GetPendingCabinetDecisions(_countryId);
                    if (warmupDecisions.Count > 0) { Debug.Log($"SHOT: the warm-up's {warmupDecisions.Count} unanswered cabinet decision(s) dropped - they would hold the clock (AF-30b's seated cabinet)."); }
                    warmupDecisions.Clear();
                }
            }
            // §510: the hold released - EXCEPT, since AF-7 (the s875 film), for a country with a bank of its own. Its main set was always filmed on a clock held
            // by the chair's hold: the warm-up's stop is a turn-3 eve, and the chair's term ended on the election turns' cycle (the note above 01b says so). AF-7
            // moved the term to the statute's date, so nothing holds that clock now, and real time ticked the controller's day: in the USA the first tick after a
            // warm-up that ran no day ticks lapses the funding (no appropriation since 2024-09-30), presents the continuing resolution to the President, and every
            // page after it went undrawn (drys875: 16 failed). The driver now holds that clock itself, as the hold did; a country without one (a euro member)
            // keeps the released clock its films were made on - Germany's formation sheet opens on that first tick.
            // s875's second film: the hold is the USA's alone. Sweden's held main set (drys875b) never ticked the day that opens the Speaker's round after the
            // warm-up's election, so its formation sheet went unfilmed, while its released clock filmed clean (drys875, realse875: 155 of 155). The USA is the
            // country whose released clock breaks: its appropriation is a day tick the warm-up never runs.
            bool ownBank = _countryId == CountryId.USA;
            SetPrivateField(controller, "_daySpeedTimer", ownBank && !opensOnTheSave && !Interrupts && !UsJourney ? -ClockHoldSeconds : 0f);   // US-36: the journey holds its own clock
            if (ownBank && !opensOnTheSave && !Interrupts && !UsJourney) { Debug.Log("SHOT: the clock stays HELD after the warm-up - the USA, whose appropriation is a day tick the warm-up never runs (the Fed chair's hold held it until AF-7)."); }

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

            // PS-8 (K163): the election abroad on the Docket instead of the sweep - the run ends here.
            if (Abroad)
            {
                yield return CaptureAbroad(controller);
                yield break;
            }

            // PS-6 US-36: the US journey instead of the sweep - the run ends here.
            if (UsJourney)
            {
                yield return CaptureUsJourney(controller);
                yield break;
            }

            if (Interrupts)
            {
                yield return CaptureInterrupts(controller);
                yield break;
            }

            if (CampaignHq)
            {
                // §961: the campaign film is a tour too - a sheet the game opens by itself (the Speaker asked the player's party after the staged count)
                // is closed before its frames, or every staged overlay after it (the e2 maps) is filmed under the sheet (§705's rule)
                _tourController = controller;
                yield return CaptureCampaignHq(controller);
                yield break;
            }

            // C-C10 (P-G2): the impact ledger instead of the sweep - the run ends here.
            if (ImpactLedger)
            {
                yield return CaptureImpactLedger(controller);
                yield break;
            }

            _tourController = controller;   // §705: the page tour begins - a sheet the game opens by itself is closed before its frames

            // P2-2.1 (2026-09-02): the Budget at REST - before any line is drafted - so the effects panel is filmed with
            // nothing to show (its idle sentence) as well as, below, mid-draft with its arrows.
            SetPrivateField(controller, "_onDesk", false);
            SetEnumField(controller, "_consolidatedTab", "Budget");
            yield return Settle();
            yield return Capture("05r_budget_rest");
            // PS-6 US-24: the debt tile's slip pinned in a US game - the federal debt limit as the game holds it on the day (the law in force, the debt subject
            // to it, the measures' days left or the cut, a raise before Congress), then let go
            if (_countryId == CountryId.USA)
            {
                MethodInfo pinLimit = controller.GetType().GetMethod("PinSlipOnAnchorForFilm", BindingFlags.Instance | BindingFlags.NonPublic);
                FieldInfo limitPins = controller.GetType().GetField("_slipPins", BindingFlags.Instance | BindingFlags.NonPublic);
                Country usLimit = (controller.GetType().GetField("_simulationManager", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(controller) as SimulationManager)?.World?.GetCountry(CountryId.USA);
                if (pinLimit != null && limitPins != null && usLimit != null)
                {
                    pinLimit.Invoke(controller, new object[] { "budget:debt", new Vector2(UiScreen.Width * 0.58f, UiScreen.Height * 0.40f) });
                    yield return Settle();
                    yield return Settle();
                    yield return Capture("05r_budget_us_debt_limit_slip");
                    Debug.Log($"SHOT: 05r_budget_us_debt_limit_slip - the debt tile's slip pinned: the limit {(UsDebtLimit.IsRead(usLimit) ? UsDebtLimit.LawWords(usLimit) : "not yet read")}, reached {(usLimit.DebtLimitReachedOn == DateTime.MinValue ? "no" : usLimit.DebtLimitReachedOn.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture))}");
                    (limitPins.GetValue(controller) as IList)?.Clear();
                    yield return Settle();
                }
                else { Debug.LogError("SHOT: PS-6 US-24 - the controller has no PinSlipOnAnchorForFilm / _slipPins / manager; 05r_budget_us_debt_limit_slip NOT written."); _failed++; }
                // PS-6 K22/K23/K75 (the review's M3): the US Congress's verbs row under the budget's action, STAGED - the limit reached ten days ago with the
                // debt over it, so a player-president is offered the raises and the suspensions and a party holding a chamber against the presidency the withhold;
                // the page scrolled to its foot so the row is in frame; every planted field put back after
                SimulationManager verbSim = controller.GetType().GetField("_simulationManager", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(controller) as SimulationManager;
                if (usLimit != null && verbSim != null && UsDebtLimit.IsRead(usLimit) && !UsDebtLimit.Suspended(usLimit, verbSim.CurrentDate))
                {
                    double limitWas = usLimit.DebtLimitUsd;
                    DateTime reachedWas = usLimit.DebtLimitReachedOn;
                    object scrollWas = controller.GetType().GetField("_budgetProcessCenterScrollPosition", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(controller);
                    try
                    {
                        usLimit.DebtLimitUsd = UsDebtLimit.SubjectToLimitUsd(usLimit) - 1e9;
                        usLimit.DebtLimitReachedOn = verbSim.CurrentDate.AddDays(-10);
                        SetPrivateField(controller, "_budgetProcessCenterScrollPosition", new Vector2(0f, 100000f));
                        yield return Settle();
                        yield return Settle();
                        yield return Capture("05s_budget_us_congress_verbs");
                        bool president = verbSim.PlayerGoverns(usLimit) && usLimit.Government != null && !string.IsNullOrEmpty(usLimit.PlayerPartyAbbrev) && usLimit.Government.RoleOf(usLimit.PlayerPartyAbbrev) == PoliSim.Elections.PlayerRole.PrimeMinister;
                        bool raises = UsDebtLimit.MayPropose(usLimit, president, UsBudgetProcedure.Applies(usLimit), verbSim.GetPendingDebtLimitBill(CountryId.USA), verbSim.CurrentDate, UsDebtLimit.RaiseUsd, DateTime.MinValue, out string raiseWhy);
                        bool withhold = UsDebtLimit.MayWithhold(usLimit, UsBudgetProcedure.Applies(usLimit), verbSim.CurrentDate, out string withholdWhy);
                        Debug.Log($"SHOT: 05s_budget_us_congress_verbs - the verbs row staged (the limit reached {usLimit.DebtLimitReachedOn:yyyy-MM-dd}, the debt over it): the raises and suspensions {(raises ? "offered" : "not offered - " + raiseWhy)}; the withhold {(withhold ? "offered" : "not offered - " + withholdWhy)}");
                    }
                    finally
                    {
                        usLimit.DebtLimitUsd = limitWas;
                        usLimit.DebtLimitReachedOn = reachedWas;
                        if (scrollWas != null) { SetPrivateField(controller, "_budgetProcessCenterScrollPosition", scrollWas); }
                    }
                    yield return Settle();
                }
                else { Debug.LogError("SHOT: 05s_budget_us_congress_verbs NOT staged - the limit is not read, or is suspended, on the film's day"); _failed++; }   // the second review's L6: a skipped frame fails
            }
            SetPrivateField(controller, "_onDesk", true);
            yield return Settle();

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
            yield return CaptureDeskUsRoles(controller);   // PS-6 US-19: the US role chip's other words (USA only)
            yield return CaptureDeskEventCard(controller);
            yield return CaptureDeskGameOver(controller);
            yield return CaptureDeskCrisis(controller);   // MF q33 (D-WORKLIST §1): the debt crisis staged - the moment, the Desk's card, the withheld year, the Budget's target, the reopening
            SetPrivateField(controller, "_onDesk", false);
            yield return Settle();

            for (int i = 0; i < Tabs.Length; i++)
            {
                if (!SetEnumField(controller, "_consolidatedTab", Tabs[i]))
                {
                    continue;
                }

                yield return Settle();
                if (Tabs[i] == "Decisions")
                {
                    // P2-4.3: the Docket's alerts and option arrows need a seated minister with a pending decision; the seed has neither.
                    var dockSim = controller.GetType().GetField("_simulationManager", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(controller) as SimulationManager;
                    StageCabinetDecisionForTheDocket(dockSim?.World?.GetCountry(_countryId), dockSim);
                    yield return Settle();
                }
                ResetScrolls(controller);   // board 5b (2026-09-02): the whole-tab capture at scroll ZERO - the previous tab's deep scroll had been carried into it by SetScrolls (every scroll field at once), so People filmed at its tail
                yield return Settle();
                yield return Capture($"{i + 2:00}_{Tabs[i].ToLowerInvariant()}");
                // EN-6 (2026-09-12): the energy page sits under the forty sector rows on the Sectors page - the plate's own
                // laid-out area, scrolled to the way the People plates are, four plates filmed in one frame where they fit.
                if (Tabs[i] == "Energy")
                {
                    string energyStem = $"{i + 2:00}_energy";
                    // EN-7a (2026-09-14) filmed the Energy sector's cost sentence here as `_sector_cost`, scrolled by the rect the Sectors page laid it out at.
                    // §739: the sentence is the Energy sector's Subsidy slip on the v3.5 Sectors page (where its subsidy lands, what the line carries) - no
                    // rect to scroll to, and the capture retires.
                    var energyField = controller.GetType().GetField("_energyPlateLastArea", BindingFlags.Instance | BindingFlags.NonPublic);
                    float energyY = energyField != null ? ((Rect)energyField.GetValue(controller)).y : 3600f;
                    ScrollBy(controller, Mathf.Max(0f, energyY - UiScreen.Height * 0.06f));
                    yield return Settle();
                    yield return Settle();
                    yield return Capture(energyStem + "_plate");   // PF-12 (§598): was the tab's own name, which overwrote the whole-tab frame at scroll zero (495)
                    ScrollBy(controller, Mathf.Max(0f, energyY + UiScreen.Height * 0.50f));
                    yield return Settle();
                    yield return Capture(energyStem + "_mid");
                    ScrollBy(controller, Mathf.Max(0f, energyY + UiScreen.Height * 1.06f));
                    yield return Settle();
                    yield return Capture(energyStem + "_lower");
                    // P6-F2 (2026-09-18, §539): the decisions plate at rest, then with one order placed by the driver - a wind step, the
                    // technology every fleet runs - and withdrawn after the frame, so the sweep's later captures read the seed's fleet
                    var decisionsField = controller.GetType().GetField("_energyDecisionsLastArea", BindingFlags.Instance | BindingFlags.NonPublic);
                    Rect decisions = decisionsField != null ? (Rect)decisionsField.GetValue(controller) : Rect.zero;
                    if (decisions.height > 0f)
                    {
                        ResetScrolls(controller);
                        yield return Settle();
                        ScrollBy(controller, Mathf.Max(0f, decisions.y - UiScreen.Height * 0.08f));
                        yield return Settle();
                        yield return Settle();
                        yield return Capture(energyStem + "_decisions");
                        var playerField = controller.GetType().GetField("_playerCountry", BindingFlags.Instance | BindingFlags.NonPublic);
                        var player = playerField?.GetValue(controller) as PoliSim.Data.Country;
                        var fleetSim = controller.GetType().GetField("_simulationManager", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(controller) as SimulationManager;
                        EnergyFleet.Order placed = player != null && fleetSim != null ? EnergyFleet.Place(player, 4, EnergyFleet.StepMw(player.Id), player.CalendarYear, fleetSim.CurrentTurn) : null;
                        if (placed != null)
                        {
                            yield return Settle();
                            yield return Settle();
                            yield return Capture(energyStem + "_decisions_ordered");
                            EnergyFleet.Withdraw(player, placed);
                            yield return Settle();
                        }
                        else { Debug.LogError($"SHOT: no wind order could be placed - {energyStem}_decisions_ordered not filmed."); }
                        // P6-F2e (2026-09-21, §551): the queue FULL - solar steps placed until the queue refuses one, filmed with the step up disabled and the sentence on its line, and
                        // every step withdrawn after the frame. §575: Germany's line is the EEG's annual volume, so the steps fill THAT YEAR's volume and the refusal is the statute's sentence - the same shot, in the source's own words. Where a technology's capacity is billed (no line at all) no step is ever refused: nothing to film, and the rest frame says BILLED.
                        if (player != null && fleetSim != null && EnergyConnectionQueue.LineOf(player.Id, 5) != null && EnergyFleet.CanOrder(player.Id, 5))
                        {
                            var steps = new System.Collections.Generic.List<EnergyFleet.Order>();
                            for (int guard = 0; guard < 4000; guard++)
                            {
                                double up = EnergyConnectionQueue.StepUpMw(player, 5, EnergyFleet.StepMw(player.Id), player.CalendarYear);   // the page's own step: the last one is the room
                                EnergyFleet.Order step = up >= 1.0 ? EnergyFleet.Place(player, 5, up, player.CalendarYear, fleetSim.CurrentTurn) : null;
                                if (step == null) { break; }
                                steps.Add(step);
                            }
                            bool refused = EnergyConnectionQueue.FullText(player, 5, player.CalendarYear) != null && EnergyFleet.CannotPlaceWhy(player, 5, 1.0, player.CalendarYear) != null;
                            if (!refused) { Debug.LogError($"SHOT: the solar queue never refused a step - {energyStem}_decisions_queue_full not filmed."); }
                            // the queue's lines are one per order: a full queue is many steps, so its plate is scrolled to the technology lines, where the refusal stands
                            yield return Settle();
                            yield return Settle();
                            if (refused) { yield return Capture(energyStem + "_decisions_queue_full"); }
                            foreach (EnergyFleet.Order step in steps) { EnergyFleet.Withdraw(player, step); }
                            yield return Settle();
                        }
                    }
                    else { Debug.LogError($"SHOT: the decisions plate was never laid out - {energyStem}_decisions not filmed."); }
                    // §747: the dials, the laws link and the instruments plate are the Policy tab's now - the frames that follow are filmed on it, and the Overview restored after
                    SetPrivateField(controller, "_energyTab", 1);
                    ResetScrolls(controller);
                    yield return Settle();
                    yield return Settle();
                    // P6-F2b (2026-09-21, §542): the four instruments as dials - at rest, then with the liberalisation dial's DRAFT moved fifteen points down
                    // (the sector's regulation draft, the same dictionary the Sectors page writes) so its range caption and its hatch are on film, and taken back
                    var dialsField = controller.GetType().GetField("_energyDialsLastArea", BindingFlags.Instance | BindingFlags.NonPublic);
                    Rect dials = dialsField != null ? (Rect)dialsField.GetValue(controller) : Rect.zero;
                    if (dials.height > 0f)
                    {
                        ResetScrolls(controller);
                        yield return Settle();
                        ScrollBy(controller, Mathf.Max(0f, dials.y - UiScreen.Height * 0.30f));
                        yield return Settle();
                        yield return Settle();
                        yield return Capture(energyStem + "_instrument_dials");
                        var regulation = controller.GetType().GetField("_sectorRegulationInputs", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(controller) as Dictionary<SectorType, float>;
                        if (regulation != null && regulation.TryGetValue(SectorType.Energy, out float standingRegulation))
                        {
                            RangeCaptionPresenter.Reset();
                            RangeCaptionPresenter.ClockOverride = 0f;   // on-drag: the caption at full ink, as the Sectors page's own caption captures hold it (P4-B2)
                            regulation[SectorType.Energy] = Mathf.Max(0f, standingRegulation - 15f);
                            // PF-4 (§554): the Subsidy dial dragged in the same frame - *Retail intervention*'s caption (the sector's own *Subsidy* where no levy is reached) had never been
                            // on a film; sixty-five is the band whose line admits a levy already gone, the longest of the ten
                            var subsidy = controller.GetType().GetField("_sectorSubsidyInputs", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(controller) as Dictionary<SectorType, float>;
                            float standingSubsidy = 0f; bool subsidyDragged = subsidy != null && subsidy.TryGetValue(SectorType.Energy, out standingSubsidy);
                            if (subsidyDragged) { subsidy[SectorType.Energy] = 65f; } else { Debug.LogError($"SHOT: the Energy sector's subsidy draft was never written - {energyStem}_instrument_dials_dragged films one caption, not two."); }
                            yield return Settle();
                            yield return Settle();
                            yield return Capture(energyStem + "_instrument_dials_dragged");
                            regulation[SectorType.Energy] = standingRegulation;
                            if (subsidyDragged) { subsidy[SectorType.Energy] = standingSubsidy; }
                            RangeCaptionPresenter.ClockOverride = null;
                            RangeCaptionPresenter.Reset();
                            yield return Settle();
                        }
                        else { Debug.LogError($"SHOT: the Energy sector's regulation draft was never written - {energyStem}_instrument_dials_dragged not filmed."); }
                    }
                    else { Debug.LogError($"SHOT: the instruments' dials were never laid out - {energyStem}_instrument_dials not filmed."); }
                    // P6-F2c (2026-09-21, §543): the laws link on the tab, then the jump it makes - the three fields a click writes, written here, so the frame
                    // is the Laws board on the electricity-tax filter as a player arriving from the Energy tab finds it; then back to the tab for the captures that follow
                    var lawsLinkField = controller.GetType().GetField("_energyLawsLinkLastArea", BindingFlags.Instance | BindingFlags.NonPublic);
                    Rect lawsLink = lawsLinkField != null ? (Rect)lawsLinkField.GetValue(controller) : Rect.zero;
                    if (lawsLink.height > 0f)
                    {
                        ResetScrolls(controller);
                        yield return Settle();
                        ScrollBy(controller, Mathf.Max(0f, lawsLink.y - UiScreen.Height * 0.55f));
                        yield return Settle();
                        yield return Settle();
                        yield return Capture(energyStem + "_laws_link");
                        SetEnumField(controller, "_consolidatedTab", "PolicyLaws");
                        SetEnumField(controller, "_policyLawsCategory", "Laws");
                        SetEnumField(controller, "_lawBrowserFilter", "ElectricityTax");
                        ResetScrolls(controller);
                        yield return Settle();
                        yield return Settle();
                        yield return Capture(energyStem + "_laws_link_arrived");
                        SetEnumField(controller, "_lawBrowserFilter", "All");
                        SetEnumField(controller, "_policyLawsCategory", "LaborMarket");
                        SetEnumField(controller, "_consolidatedTab", "Energy");
                        SetPrivateField(controller, "_energyTab", 1);
                        ResetScrolls(controller);
                        yield return Settle();
                    }
                    else { Debug.LogError($"SHOT: the laws link was never laid out - {energyStem}_laws_link not filmed."); }
                    // EN-7a (2026-09-14): the instruments plate in PROVENANCE, where the chips that say what each instrument reaches draw
                    // the setting is the viewer's saved preference: kept and restored - after the capture, or by Finish when a -shotstop ends the run on it
                    _provenanceToRestore = DeskProvenance.On;
                    DeskProvenance.On = true;
                    // §747: the instruments plate is on the Policy tab - scrolled to its own recorded rect, not by the Overview's plate
                    var instrumentsHead = controller.GetType().GetField("_energyInstrumentsLastArea", BindingFlags.Instance | BindingFlags.NonPublic);
                    Rect instrumentsAt = instrumentsHead != null ? (Rect)instrumentsHead.GetValue(controller) : Rect.zero;
                    ResetScrolls(controller);
                    yield return Settle();
                    ScrollBy(controller, Mathf.Max(0f, instrumentsAt.y - UiScreen.Height * 0.12f));
                    yield return Settle();
                    yield return Settle();
                    yield return Capture(energyStem + "_instruments_provenance");
                    // EN-7b (2026-09-15): plate 4's foot, where the electricity tax's rows sit, still in PROVENANCE - scrolled to the plate's recorded rect
                    var instrumentsField = controller.GetType().GetField("_energyInstrumentsLastArea", BindingFlags.Instance | BindingFlags.NonPublic);
                    Rect instruments = instrumentsField != null ? (Rect)instrumentsField.GetValue(controller) : Rect.zero;
                    if (instruments.height > 0f)
                    {
                        ResetScrolls(controller);
                        yield return Settle();
                        ScrollBy(controller, Mathf.Max(0f, instruments.yMax - UiScreen.Height * 0.52f));
                        yield return Settle();
                        yield return Settle();
                        yield return Capture(energyStem + "_electricity_tax_provenance");
                    }
                    else { Debug.LogError($"SHOT: plate 4 was never laid out - {energyStem}_electricity_tax_provenance not filmed."); }
                    DeskProvenance.On = _provenanceToRestore.Value;
                    _provenanceToRestore = null;
                    SetPrivateField(controller, "_energyTab", 0);
                    yield return Settle();
                    ResetScrolls(controller);
                    yield return Settle();
                }
                if (Tabs[i] == "Decisions")
                {
                    // P4-E3 (2026-09-04): the staged Cabinet decision's options scrolled into view - two options, each with its cost line, plate and scope line.
                    // §731 (UI v3.5): the docket is a list - the staged decision's item is OPENED (its Decide), its options under it, then scrolled into view.
                    SetPrivateField(controller, "_docketOpenItem", "cabinet:" + CabinetPortfolio.InteriorJustice);
                    yield return Settle();
                    ScrollBy(controller, UiScreen.Height * 0.3f);
                    yield return Settle();
                    yield return Capture("03a_decisions_options");
                    SetPrivateField(controller, "_docketOpenItem", null);
                    ResetScrolls(controller);
                    yield return Settle();

                    // P6-A3 (2026-09-17): THE INTERRUPT STATE, which no film had ever carried. The budget
                    // process is the fourth thing that holds the clock, and when it is open the docket
                    // draws its own area card with the urgency stamp - the panel playtest 6's finding 4
                    // reports painting over the tab strip. The flag is the whole of the state the card
                    // reads (a country in the pending set), so it is staged here and taken back straight
                    // after: the sweep's later captures must see the game they would have seen.
                    yield return CaptureBudgetProcessInterrupt(controller);
                }
                if (Tabs[i] == "Demographics")
                {
                    // §732 (UI v3.5): People is one scrolled page of sections - a frame below the fold scrolls the page to its section's head, by the
                    // page's own record (_peopleSectionTops, ScrollPeopleTo), and a slip is filmed pinned through the controller's own hook (a pointer's
                    // slip cannot be filmed; a pinned one is drawn whatever the pointer does).
                    MethodInfo pinSlip = controller.GetType().GetMethod("PinSlipForFilm", BindingFlags.Instance | BindingFlags.NonPublic);
                    FieldInfo pins = controller.GetType().GetField("_slipPins", BindingFlags.Instance | BindingFlags.NonPublic);
                    if (pinSlip != null && pins != null)
                    {
                        // §666 (19b): two pinned chains - the 35-39 band's slip with WORKING AGE's level 2 open, and the turnout head's
                        pinSlip.Invoke(controller, new object[] { "band:7", "WORKING AGE", new Vector2(UiScreen.Width * 0.30f, UiScreen.Height * 0.24f) });
                        pinSlip.Invoke(controller, new object[] { "head:turnout", null, new Vector2(UiScreen.Width * 0.58f, UiScreen.Height * 0.06f) });
                        yield return Settle();
                        yield return Settle();
                        yield return Capture("04i_people_slips_pinned");
                        (pins.GetValue(controller) as System.Collections.IList)?.Clear();
                        yield return Settle();
                    }
                    else { Debug.LogError("SHOT: §666 - the controller has no PinSlipForFilm / _slipPins; 04i_people_slips_pinned NOT written."); _failed++; }

                    foreach ((string section, string stem, string pinned, string pinnedStem) in new[]
                    {
                        ("dependency", "04a_people_dependency_electorate", null, null),
                        // the cataract wait's slip carries the knee replacement the composition leaves off the page
                        ("health", "04b_people_health", "health:cataract", "04b2_people_health_wait_pinned"),
                        // 20b rule 5 kept: BILLED is the glyph in the figure's slot, its sentence the slip's
                        ("education", "04c_people_education", "education:pisa", "04c2_people_education_billed_pinned"),
                        ("infrastructure", "04d_people_infrastructure", null, null),
                        ("environment", "04e_people_environment", null, null),
                        ("migration", "04f_people_migration_employment", null, null),
                    })
                    {
                        ScrollPeopleTo(controller, section);
                        yield return Settle();
                        yield return Settle();
                        yield return Capture(stem);
                        if (pinned != null && pinSlip != null && pins != null)
                        {
                            pinSlip.Invoke(controller, new object[] { pinned, null, new Vector2(UiScreen.Width * 0.45f, UiScreen.Height * 0.22f) });
                            yield return Settle();
                            yield return Settle();
                            yield return Capture(pinnedStem);
                            (pins.GetValue(controller) as System.Collections.IList)?.Clear();
                            yield return Settle();
                        }
                    }

                    // P6-B1 / §686, in v3.5 (rule 3): THE SAME PAGE BEHIND THE †. Every slip prints as its tile's dotted line under the section - the
                    // census the tiles took off at rest, filmed at four sections and at the head.
                    _provenanceToRestore = DeskProvenance.On;
                    DeskProvenance.On = true;
                    yield return Settle();
                    foreach ((string section, string stem) in new[] { ("health", "04g_people_health_dense"), ("education", "04g2_people_education_dense"),
                        ("infrastructure", "04g3_people_infrastructure_dense"), ("environment", "04g4_people_environment_dense") })
                    {
                        yield return Settle();   // the dense lines lengthen the page: the tops are re-recorded before the scroll reads them
                        ScrollPeopleTo(controller, section);
                        yield return Settle();
                        yield return Settle();
                        yield return Capture(stem);
                    }
                    ScrollPeopleTo(controller, null);
                    yield return Settle();
                    yield return Settle();
                    yield return Capture("04h_people_dense");
                    DeskProvenance.On = _provenanceToRestore.Value;
                    _provenanceToRestore = null;
                    ResetScrolls(controller);
                    yield return Settle();
                }

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

                    // §710 (R-SP5 retired): the map's chips, measured by the renderer on the capture frame.
                    if (stem == "02b_statistics_international")
                    {
                        AssertMapChips(controller, stem);
                    }

                    // D-ST (§701, 19b): the sheet's slips on film - three pinned chains per tab through the controller's own pin, filmed, then let go.
                    if (stem == "02a_statistics_domestic" || stem == "02b_statistics_international")
                    {
                        MethodInfo pinStats = controller.GetType().GetMethod("PinSlipForFilm", BindingFlags.Instance | BindingFlags.NonPublic);
                        FieldInfo statsPins = controller.GetType().GetField("_slipPins", BindingFlags.Instance | BindingFlags.NonPublic);
                        if (pinStats != null && statsPins != null)
                        {
                            bool domestic = stem == "02a_statistics_domestic";
                            string[] ids = domestic ? new[] { "card:history", "sector:other", "chart:unemployment/nairu" } : new[] { "map:chip:Germany", "links:tariff", "relations" };   // §710: a chip's slip, the name first
                            Vector2[] at = domestic
                                ? new[] { new Vector2(UiScreen.Width * 0.62f, UiScreen.Height * 0.22f), new Vector2(UiScreen.Width * 0.30f, UiScreen.Height * 0.52f), new Vector2(UiScreen.Width * 0.40f, UiScreen.Height * 0.78f) }
                                : new[] { new Vector2(UiScreen.Width * 0.20f, UiScreen.Height * 0.46f), new Vector2(UiScreen.Width * 0.62f, UiScreen.Height * 0.62f), new Vector2(UiScreen.Width * 0.62f, UiScreen.Height * 0.80f) };
                            // §708 (D-ST's return, flag 3): each pin is made on its anchor nearest the film's point, as a player's click makes it, so the
                            // slip opens there and the 19b addition places it clear of the figure it qualifies
                            MethodInfo pinOnAnchor = controller.GetType().GetMethod("PinSlipOnAnchorForFilm", BindingFlags.Instance | BindingFlags.NonPublic);
                            for (int p = 0; p < ids.Length; p++)
                            {
                                if (pinOnAnchor != null) { pinOnAnchor.Invoke(controller, new object[] { ids[p], at[p] }); }
                                else { pinStats.Invoke(controller, new object[] { ids[p], null, at[p] }); }
                            }
                            yield return Settle();
                            yield return Settle();
                            yield return Capture(stem + "_slips_pinned");
                            (statsPins.GetValue(controller) as System.Collections.IList)?.Clear();
                            yield return Settle();
                        }
                        else { Debug.LogError($"SHOT: D-ST - the controller has no PinSlipForFilm / _slipPins; {stem}_slips_pinned NOT written."); _failed++; }
                    }

                    // S10 (Design's return of 2026-10-30, 10b: "Yes, please film it"): the debt chart's framework line in Sweden (below its anchor) and Italy (above the
                    // Treaty's line), and the USA's NO RATIO IN LAW stamp - the chart scrolled into frame, then its gutter's slip pinned
                    if (stem == "02a_statistics_domestic" && Array.IndexOf(DebtLineCountries, _countryId) >= 0)
                    {
                        yield return FilmDebtLine(controller);
                    }

                    // §647 (POLITICAL_SYSTEM_SPEC.md §5.3, premise 1): THE FORMATION SHEET. The warm-up election opens the Speaker's round with the film's
                    // party in opposition, so the Speaker never asks it; the sheet is staged the §630 way - the party re-seated as the first in the round's
                    // order, a round opened on the same chamber and declarations, filmed, then the film's round and party put back.
                    if (stem == "07a_politics_parliament")
                    {
                        SimulationManager roundSim = controller.GetType().GetField("_simulationManager", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(controller) as SimulationManager;
                        Country roundCountry = roundSim?.World?.GetCountry(_countryId);
                        if (roundSim != null && roundCountry != null && roundSim.RoundsApply(_countryId))
                        {
                            Expect(stem + "_formation_sheet", stem + "_formation_sheet_rows", stem + "_formation_sheet_revised", stem + "_speaker_asks");   // TL-1: a country with rounds reaches these
                            SpeakerRound filmRound = roundSim.RoundOf(_countryId);
                            if (filmRound == null || filmRound.Order.Count == 0)
                            {
                                Debug.LogError($"SHOT: {stem} - no Speaker's round is open after the warm-up election; the formation sheet is not filmed.");
                                _failed++;
                            }
                            else
                            {
                                GovernmentRecord roundRecord = roundCountry.Government;
                                string partyBefore = roundCountry.PlayerPartyAbbrev;
                                roundCountry.PlayerPartyAbbrev = filmRound.Order[0];
                                _stagingFormationSheet = true;   // §705: the sheet is this staging's to film
                                roundSim.OpenSpeakerRound(roundCountry, filmRound.Vintage, filmRound.Occasion,   // §685 (21a): the film round's own occasion - "for the film" was harness prose on a player screen
                                    electionDay: filmRound.ReadsOn == DateTime.MinValue ? (DateTime?)null : filmRound.ReadsOn, midTerm: filmRound.MidTerm);   // the film's round's own reading (§653)
                                SpeakerRound staged = roundSim.RoundOf(_countryId);
                                Debug.Log($"SHOT: §647 - {partyBefore} re-seated as {roundCountry.PlayerPartyAbbrev}, first in the order ({string.Join(", ", filmRound.Order)}); the staged round's stage {staged?.Stage}");
                                FieldInfo sheetOpen = controller.GetType().GetField("_formationSheetOpen", BindingFlags.Instance | BindingFlags.NonPublic);
                                ResetScrolls(controller);
                                yield return Settle();
                                if (!(sheetOpen?.GetValue(controller) is bool opened) || !opened)
                                {
                                    Debug.LogError($"SHOT: {stem}_formation_sheet - the Speaker asked the player's party and the sheet did not open (premise 1).");
                                    _failed++;
                                }
                                yield return Capture(stem + "_formation_sheet");

                                // §685 (board 21a): the sheet no longer scrolls, so the second frame stages the state the first cannot show - a party outside
                                // the cabinet ASKED to support, its tabled demands as rows under it, the first accepted - then puts the draft back.
                                FieldInfo draftField = controller.GetType().GetField("_formationDraft", BindingFlags.Instance | BindingFlags.NonPublic);
                                FieldInfo versionField = controller.GetType().GetField("_formationDraftVersion", BindingFlags.Instance | BindingFlags.NonPublic);
                                string askedSupporter = null;
                                if (draftField?.GetValue(controller) is FormationProposal rowsDraft && versionField != null)
                                {
                                    foreach (PoliticalParty party in PartySystems.For(_countryId))
                                    {
                                        if (rowsDraft.CabinetParties.Contains(party.Abbrev) || rowsDraft.Supporters.Contains(party.Abbrev)
                                            || !roundCountry.ParliamentSeats.TryGetValue(party.Abbrev, out int rowsSeats) || rowsSeats <= 0) { continue; }
                                        askedSupporter = party.Abbrev;
                                        break;
                                    }
                                    if (askedSupporter != null)
                                    {
                                        rowsDraft.Supporters.Add(askedSupporter);
                                        rowsDraft.AcceptedDemands[askedSupporter] = new List<string>();
                                        rowsDraft.FreezeTabled(roundCountry, roundSim.CurrentDate, roundSim.World);
                                        if (rowsDraft.Tabled.TryGetValue(askedSupporter, out List<AgreementItem> rowsTabled) && rowsTabled.Count > 0)
                                        {
                                            rowsDraft.AcceptedDemands[askedSupporter].Add(SupportAgreement.KeyOf(rowsTabled[0]));
                                        }
                                        versionField.SetValue(controller, (int)versionField.GetValue(controller) + 1);
                                    }
                                }
                                yield return Settle();
                                if (askedSupporter == null) { Debug.LogError($"SHOT: {stem}_formation_sheet_rows - no seated party outside the cabinet to ask; the support rows are not staged."); _failed++; }
                                else { Debug.Log($"SHOT: §685 - {askedSupporter} asked to support on the sheet, its demands as rows, the first accepted."); }
                                yield return Capture(stem + "_formation_sheet_rows");
                                if (askedSupporter != null && draftField?.GetValue(controller) is FormationProposal rowsBack)
                                {
                                    rowsBack.Supporters.Remove(askedSupporter);
                                    rowsBack.AcceptedDemands.Remove(askedSupporter);
                                    versionField.SetValue(controller, (int)versionField.GetValue(controller) + 1);
                                }

                                // Premise 3, revise and re-offer: a partner's posts taken back by the formateur - the partner's answer turns, and the sheet says why.
                                string stinted = null;
                                FormationProposal stintDraft = versionField != null ? draftField?.GetValue(controller) as FormationProposal : null;
                                if (stintDraft != null)
                                {
                                    foreach (string partner in stintDraft.CabinetParties)
                                    {
                                        if (partner == stintDraft.Formateur || stintDraft.PostsOf(partner) == 0) { continue; }
                                        if (!stintDraft.Posts.ContainsKey(stintDraft.Formateur)) { stintDraft.Posts[stintDraft.Formateur] = new List<CabinetPortfolio>(); }
                                        stintDraft.Posts[stintDraft.Formateur].AddRange(stintDraft.Posts[partner]);
                                        stintDraft.Posts[partner] = new List<CabinetPortfolio>();
                                        stinted = partner;
                                        break;
                                    }
                                    versionField.SetValue(controller, (int)versionField.GetValue(controller) + 1);
                                }
                                // s918: a formateur drafting alone (Sweden's round S-led alone since s917's count) has no partner to stint - the harness seats a seated
                                // party outside the cabinet with no post, largest first, and keeps the first that refuses FOR THE POSTS (not on a red line), the premise's own case
                                bool premiseUnstageable = false;   // every candidate explained: refused on a rule (a red line, K-1f, no seat) or accepted with nowhere else to go
                                if (stintDraft != null && stinted == null && roundCountry.ParliamentSeats != null)
                                {
                                    premiseUnstageable = true;
                                    var candidates = new List<string>();
                                    foreach (KeyValuePair<string, int> kv in roundCountry.ParliamentSeats)
                                    {
                                        if (kv.Value > 0 && kv.Key != stintDraft.Formateur && kv.Key != roundCountry.PlayerPartyAbbrev && !stintDraft.CabinetParties.Contains(kv.Key)) { candidates.Add(kv.Key); }
                                    }
                                    if (candidates.Count == 0) { premiseUnstageable = false; }
                                    candidates.Sort((a, b) => roundCountry.ParliamentSeats[b] != roundCountry.ParliamentSeats[a] ? roundCountry.ParliamentSeats[b].CompareTo(roundCountry.ParliamentSeats[a]) : string.CompareOrdinal(a, b));
                                    foreach (string candidate in candidates)
                                    {
                                        stintDraft.CabinetParties.Add(candidate);
                                        stintDraft.Posts[candidate] = new List<CabinetPortfolio>();
                                        versionField.SetValue(controller, (int)versionField.GetValue(controller) + 1);
                                        yield return Settle();
                                        ProposalVerdict trial = controller.GetType().GetField("_formationVerdict", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(controller) as ProposalVerdict;
                                        PartyAnswer tried = trial?.Answers.Find(a => a.Party == candidate);
                                        if (RefusesForThePosts(tried)) { stinted = candidate; Debug.Log($"SHOT: §647 - {stintDraft.Formateur} drafts alone; {candidate} seated in its cabinet with no post for the premise."); break; }
                                        Debug.Log($"SHOT: §647 - {candidate} seated with no post does not refuse for the posts: {(tried == null ? "no answer" : tried.Reason)}");
                                        if (tried == null || tried.Alternative > 0.0) { premiseUnstageable = false; }   // an answer the rule does not explain: a partner with somewhere better that took no post
                                        stintDraft.CabinetParties.Remove(candidate);
                                        stintDraft.Posts.Remove(candidate);
                                    }
                                    versionField.SetValue(controller, (int)versionField.GetValue(controller) + 1);
                                }
                                ResetScrolls(controller);
                                yield return Settle();
                                ProposalVerdict revised = controller.GetType().GetField("_formationVerdict", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(controller) as ProposalVerdict;
                                PartyAnswer stintedAnswer = revised?.Answers.Find(a => a.Party == stinted);
                                if (stinted == null && premiseUnstageable)
                                {
                                    Debug.Log($"SHOT: §647 - PREMISE 2 NOT STAGEABLE in this world (named, not a failure; Germany's sheet films it): {stintDraft?.Formateur} drafts alone and every seated party outside its cabinet refuses on a rule or has no formation elsewhere - the lines above.");
                                }
                                else if (stinted == null || !RefusesForThePosts(stintedAnswer))
                                {
                                    Debug.LogError($"SHOT: {stem}_formation_sheet_revised - a partner offered no post should refuse (premise 2); stinted {stinted ?? "none"}, answer {(stintedAnswer == null ? "none" : stintedAnswer.Accepts ? "accepts" : "refuses")}.");
                                    _failed++;
                                }
                                else { Debug.Log($"SHOT: §647 - {stinted} offered no post refuses: {stintedAnswer.Reason}"); }
                                yield return Capture(stem + "_formation_sheet_revised");

                                // Closed, the Parliament tab's round block offers the sheet again.
                                sheetOpen?.SetValue(controller, false);
                                ScrollBy(controller, 900f);
                                yield return Settle();
                                yield return Capture(stem + "_speaker_asks");

                                roundRecord.Round = filmRound;
                                roundCountry.PlayerPartyAbbrev = partyBefore;
                                sheetOpen?.SetValue(controller, false);
                                ResetScrolls(controller);
                                yield return Settle();
                                _stagingFormationSheet = false;
                                Debug.Log($"SHOT: §647 - the film's round ({filmRound.Stage}, {filmRound.Asked} asked) and {partyBefore} put back.");
                            }
                        }
                    }

                    // P4-B2 (2026-09-04): the range caption at its three moments on two dials - the Minimum Wage on the
                    // labour page and the first sector's Subsidy on the sectors page. The draft moves into a new band
                    // with the presenter's clock at 0 (on-drag), then the clock alone advances to the hold's end (held)
                    // and past the fade (faded); the row's geometry is compared at rest and with the caption showing.
                    if (stem == "06a_policylaws_labormarket" || stem == "06c_policylaws_sectors")
                    {
                        var restGeometry = new Dictionary<string, (Rect Name, Rect Track, Rect Figure, Rect Trailing)>(LedgerRow.GeometryByRow);
                        RangeCaptionPresenter.Reset();
                        RangeCaptionPresenter.ClockOverride = 0f;
                        object before = null;
                        SectorType firstSector = SectorType.Manufacturing;
                        if (stem == "06a_policylaws_labormarket")
                        {
                            // Paid Family Leave, not the minimum wage: Sweden's wage floor is collective bargaining ("none"), so
                            // that dial is drawn disabled and a caption never shows on it - the first film of this block proved it.
                            var field = controller.GetType().GetField("_paidFamilyLeaveWeeksInput", BindingFlags.Instance | BindingFlags.NonPublic);
                            before = field?.GetValue(controller);
                            field?.SetValue(controller, (float?)60f);
                            // §564 (2026-09-22): the dials lead the tab now - scrolled to the row's own laid-out rect (the card that stood above them is under them)
                            float leaveY = RowTop(" / Paid Family Leave");
                            if (leaveY < 0f) { Debug.LogError($"SHOT: {stem}_caption_drag - the Paid Family Leave row's geometry was not recorded; the frame is filmed at the page's head."); _failed++; }
                            ScrollBy(controller, Mathf.Max(0f, leaveY - UiScreen.Height * 0.14f));
                        }
                        else
                        {
                            var inputs = controller.GetType().GetField("_sectorSubsidyInputs", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(controller) as Dictionary<SectorType, float>;
                            if (inputs != null) { before = inputs.TryGetValue(firstSector, out float b0) ? (object)b0 : null; inputs[firstSector] = 80f; }
                            // §739: the sector's dials are tiles under the eight sector tiles, Manufacturing's by default - scrolled to the Subsidy tile's own laid-out rect
                            float subsidyY = RowTop(" / Subsidy");
                            if (subsidyY < 0f) { Debug.LogError($"SHOT: {stem}_caption_drag - the Subsidy tile's geometry was not recorded; the frame is filmed at the page's head."); _failed++; }
                            ScrollBy(controller, Mathf.Max(0f, subsidyY - UiScreen.Height * 0.14f));
                        }
                        yield return Settle();
                        yield return Settle();
                        yield return Capture(stem + "_caption_drag");
                        RangeCaptionPresenter.ClockOverride = RangeCaptionPresenter.HoldSeconds * 0.9f;
                        yield return Settle();
                        yield return Capture(stem + "_caption_held");
                        RangeCaptionPresenter.ClockOverride = RangeCaptionPresenter.HoldSeconds + RangeCaptionPresenter.FadeSeconds + 1f;
                        yield return Settle();
                        yield return Capture(stem + "_caption_faded");
                        AssertLedgerGeometryStable(restGeometry, stem);
                        Debug.Log($"SHOT: P4-B2 - the range caption filmed at 0 s (on-drag), {RangeCaptionPresenter.HoldSeconds * 0.9f:0.0} s (held) and {RangeCaptionPresenter.HoldSeconds + RangeCaptionPresenter.FadeSeconds + 1f:0.0} s (faded) on {stem}; the row's geometry compared at rest and showing.");
                        if (stem == "06a_policylaws_labormarket")
                        {
                            controller.GetType().GetField("_paidFamilyLeaveWeeksInput", BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(controller, before);
                        }
                        else
                        {
                            var inputs = controller.GetType().GetField("_sectorSubsidyInputs", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(controller) as Dictionary<SectorType, float>;
                            if (inputs != null) { if (before is float b1) { inputs[firstSector] = b1; } else { inputs.Remove(firstSector); } }
                        }
                        RangeCaptionPresenter.ClockOverride = null;
                        RangeCaptionPresenter.Reset();
                        yield return Settle();
                    }


                    // P4-1 (2026-09-03): the readout pair on the TAX ledger, where the rows sit above the fold - the Income Tax
                    // draft moved +5 points, the same frame again, the geometry compared, the draft put back.
                    if (stem == "05a_budget_tax")
                    {
                        var taxInputs = controller.GetType().GetField("_taxRateInputs", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(controller) as Dictionary<TaxType, float>;
                        if (taxInputs != null)
                        {
                            var restGeometry = new Dictionary<string, (Rect Name, Rect Track, Rect Figure, Rect Trailing)>(LedgerRow.GeometryByRow);
                            bool had = taxInputs.TryGetValue(TaxType.IncomeTax, out float before);
                            float standing = (controller.GetType().GetField("_playerCountry", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(controller) as Country)?.TaxLines.Find(t => t.Type == TaxType.IncomeTax)?.Rate ?? before;
                            taxInputs[TaxType.IncomeTax] = Mathf.Min(standing + 5f, 90f);
                            yield return Settle();
                            yield return Settle();
                            yield return Capture(stem + "_dragged");
                            Debug.Log("SHOT: P4-1 - the Income Tax draft moved +5 for the mid-drag frame; the readout cell shows the draft in the draft cue, the track unmoved.");
                            AssertLedgerGeometryStable(restGeometry, stem);
                            // §768 (Elias's ruling D4): where the procedure is the Sejm's budget act and statutes (Poland, US-20) the drafted rate is the budget's separate tax act - the
                            // If passed count's slip pinned on the same draft, so its tax act line (and his answer) is on film, then let go.
                            if (WorldClock.StatutePartsAreActs(_countryId))   // US-20: Poland's procedure, not its veto
                            {
                                MethodInfo pinTaxAct = controller.GetType().GetMethod("PinSlipOnAnchorForFilm", BindingFlags.Instance | BindingFlags.NonPublic);
                                FieldInfo taxActPins = controller.GetType().GetField("_slipPins", BindingFlags.Instance | BindingFlags.NonPublic);
                                if (pinTaxAct != null && taxActPins != null)
                                {
                                    pinTaxAct.Invoke(controller, new object[] { "ifpassed:count", new Vector2(UiScreen.Width * 0.85f, UiScreen.Height * 0.30f) });
                                    yield return Settle();
                                    yield return Settle();
                                    yield return Capture(stem + "_taxact_slip");
                                    (taxActPins.GetValue(controller) as IList)?.Clear();
                                    yield return Settle();
                                }
                                else { Debug.LogError($"SHOT: §768 - the controller has no PinSlipOnAnchorForFilm / _slipPins; {stem}_taxact_slip NOT written."); _failed++; }
                            }
                            // PS-6 US-22: where the US Congress votes the draft as its acts, the same slip on the same draft - the acts, their procedures and verdicts, the
                            // president's answer, the continuing resolution and the lapse - on film beside Poland's, then let go
                            else if (WorldClock.CongressActs(_countryId))
                            {
                                MethodInfo pinActs = controller.GetType().GetMethod("PinSlipOnAnchorForFilm", BindingFlags.Instance | BindingFlags.NonPublic);
                                FieldInfo actsPins = controller.GetType().GetField("_slipPins", BindingFlags.Instance | BindingFlags.NonPublic);
                                if (pinActs != null && actsPins != null)
                                {
                                    pinActs.Invoke(controller, new object[] { "ifpassed:count", new Vector2(UiScreen.Width * 0.85f, UiScreen.Height * 0.30f) });
                                    yield return Settle();
                                    yield return Settle();
                                    yield return Capture(stem + "_us_acts_slip");
                                    Debug.Log("SHOT: 05a_budget_tax_us_acts_slip - the If passed count's slip pinned on the +5 income-tax draft: the acts as Congress votes them (the appropriation, the reconciliation), the continuing resolution, the lapse");
                                    (actsPins.GetValue(controller) as IList)?.Clear();
                                    yield return Settle();
                                }
                                else { Debug.LogError($"SHOT: 05a US acts - the controller has no PinSlipOnAnchorForFilm / _slipPins; {stem}_us_acts_slip NOT written."); _failed++; }
                            }
                            if (had) { taxInputs[TaxType.IncomeTax] = before; } else { taxInputs.Remove(TaxType.IncomeTax); }
                            yield return Settle();
                            // EN-8 (2026-09-13): the same tab scrolled to its last rows, so the Carbon Tax row - the one row whose step is
                            // coarser than a point - is on film with its step printed under its name (the 9d second line; "BY 30 SEK/t"
                            // on Sweden's row at 1280). The 05b precedent (P5-B5): scrolled, filmed, the scroll put back.
                            ScrollBy(controller, 2000f);
                            yield return Settle();
                            yield return Capture(stem + "_rows_carbon");   // PF-12 (§598): was "_rows", which the sweep's own 900 px frame overwrote (1000) - the Carbon Tax row never survived
                            // AF-36 (Elias's ruling q36, 2026-10-18: "the slip and the Desk show the date the rate reverts, and a bill may carry a passed figure into
                            // the next year"): on the scrolled carbon row a figure passed this year is planted standing and the carry drafted, the tile's slip pinned
                            // (the day it reverts, the carry), then the Desk's calendar slip pinned (the same day) - and everything put back. Where the statute moves
                            // no figure (France; the three with no carbon tax) there is no date and no carry, and nothing is filmed.
                            if (CarbonRateStatute.StatuteMoves(_countryId))
                            {
                                Country carbonPlayer = controller.GetType().GetField("_playerCountry", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(controller) as Country;
                                SimulationManager carbonSim = controller.GetType().GetField("_simulationManager", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(controller) as SimulationManager;
                                var carryInputs = controller.GetType().GetField("_taxCarryInputs", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(controller) as Dictionary<TaxType, TaxCarryDraft>;
                                MethodInfo pinCarbon = controller.GetType().GetMethod("PinSlipOnAnchorForFilm", BindingFlags.Instance | BindingFlags.NonPublic);
                                FieldInfo carbonPins = controller.GetType().GetField("_slipPins", BindingFlags.Instance | BindingFlags.NonPublic);
                                TaxLine carbonLine = carbonPlayer?.TaxLines.Find(t => t.IsPerTonne);
                                if (carbonSim != null && carryInputs != null && carbonLine != null && pinCarbon != null && carbonPins != null)
                                {
                                    int stampBefore = carbonLine.PassedForYear;
                                    carryInputs.TryGetValue(carbonLine.Type, out TaxCarryDraft carryBefore);
                                    carbonLine.PassedForYear = carbonSim.CurrentDate.Year;   // a figure passed this year, standing
                                    carryInputs[carbonLine.Type] = TaxCarryDraft.Of(carbonLine);   // drafted against the planted stamp
                                    pinCarbon.Invoke(controller, new object[] { "tax:" + carbonLine.Type, new Vector2(UiScreen.Width * 0.30f, UiScreen.Height * 0.30f) });
                                    yield return Settle();
                                    yield return Settle();
                                    yield return Capture(stem + "_carbon_passed");
                                    bool shown = carbonSim.TryPassedCarbonFigure(_countryId, out TaxLine _, out DateTime reverts);
                                    Debug.Log($"SHOT: AF-36 - {stem}_carbon_passed: the carbon line passed for {carbonLine.PassedForYear}, its tile's slip pinned with the day it reverts ({reverts:yyyy-MM-dd}) and the carry drafted.");
                                    (carbonPins.GetValue(controller) as IList)?.Clear();
                                    SetPrivateField(controller, "_onDesk", true);
                                    yield return Settle();
                                    pinCarbon.Invoke(controller, new object[] { "card:calendar", new Vector2(UiScreen.Width * 0.70f, UiScreen.Height * 0.25f) });
                                    yield return Settle();
                                    yield return Settle();
                                    yield return Capture("01d_desk_carbon_passed");
                                    Debug.Log($"SHOT: AF-36 - 01d_desk_carbon_passed: the Desk's calendar slip pinned with the same day ({reverts:yyyy-MM-dd}).");
                                    (carbonPins.GetValue(controller) as IList)?.Clear();
                                    SetPrivateField(controller, "_onDesk", false);
                                    if (carryBefore == null) { carryInputs.Remove(carbonLine.Type); } else { carryInputs[carbonLine.Type] = carryBefore; }
                                    carbonLine.PassedForYear = stampBefore;
                                    yield return Settle();
                                    if (!shown) { Debug.LogError($"SHOT: AF-36 - a figure planted as passed for {carbonSim.CurrentDate.Year} reads as no passed figure on {_countryId}; the date is NOT on film."); _failed++; }
                                }
                                else { Debug.LogError("SHOT: AF-36 - the controller has no player, manager, carry drafts or slip pins reachable; the carbon carry is NOT filmed."); _failed++; }
                            }
                            ScrollBy(controller, 0f);
                            yield return Settle();
                        }
                        else
                        {
                            Debug.LogError("SHOT: P4-1 - the tax inputs dictionary was not found; the readout pair is NOT filmed.");
                            _failed++;
                        }
                    }

                    // P3-C1 (2026-09-03): the film PAIR mid-drag - one spending dial moved as a slider would move it,
                    // then the same frame again after the preview's own debounce has passed, so the arrows' response
                    // to the draft is on film; the dial is put back afterwards.
                    if (stem == "05b_budget_spending")
                    {
                        var spendingInputs = controller.GetType().GetField("_spendingLineInputs", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(controller) as Dictionary<SpendingCategory, float>;
                        if (spendingInputs != null)
                        {
                            // P4-1 (2026-09-03): STABLE CONTROL LAYOUT - every ledger row's four column rects at rest, to be compared
                            // against the same rows mid-drag after the capture below.
                            var restGeometry = new Dictionary<string, (Rect Name, Rect Track, Rect Figure, Rect Trailing)>(LedgerRow.GeometryByRow);
                            // P5-B5: the drafts are figures - Education drafted at 1.15x its standing amount (the +15 % the frame always showed).
                            bool hadBefore = spendingInputs.TryGetValue(SpendingCategory.Education, out float before);
                            var dragPlayer = controller.GetType().GetField("_playerCountry", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(controller) as Country;
                            float educationStanding = dragPlayer?.SpendingLines.Find(l => l.Category == SpendingCategory.Education)?.Amount ?? 0f;
                            spendingInputs[SpendingCategory.Education] = educationStanding * 1.15f;
                            yield return Settle();
                            yield return Settle();
                            yield return Capture(stem + "_dragged");
                            Debug.Log("SHOT: P3-C1 - the Education spending dial moved +15 for the mid-drag frame; the arrows above are with vs without that draft.");
                            AssertLedgerGeometryStable(restGeometry, stem);
                            // P5-B5 (2026-09-05): the same drag, scrolled to the rows - the figure, the band's instruments and a caption under
                            // drafted rows on film; the scroll is put back to the top before the drag is undone.
                            // §564 (2026-09-22): to the DRAGGED row itself by its recorded geometry - the fixed 900 px reached the discretionary group only while the prose
                            // summary block stood above the rows; it now follows them, and the frame is named for the drafted row.
                            float educationY = RowTop(" / Education");
                            if (educationY < 0f) { Debug.LogError($"SHOT: {stem}_rows_dragged - the Education row's geometry was not recorded; the frame is filmed at the page's head."); _failed++; }
                            ScrollBy(controller, Mathf.Max(0f, educationY - UiScreen.Height * 0.14f));
                            yield return Settle();
                            yield return Capture(stem + "_rows_dragged");
                            ScrollBy(controller, 0f);
                            if (hadBefore) { spendingInputs[SpendingCategory.Education] = before; } else { spendingInputs.Remove(SpendingCategory.Education); }
                            yield return Settle();
                            // Board 15c (2026-09-13): the same tab scrolled past the summary block to its FIRST row - Social Security, the pension
                            // line - so the statutory mark beneath it (PN-1's path on the track and its sentence in the caption band) is
                            // on film with the row it belongs to. §564 (2026-09-22): the row's OWN laid-out rect, read off LedgerRow.GeometryByRow at scroll zero - the fixed
                            // offset was the prose summary block's height, and that block now follows the dials.
                            // by the Social Security line the pension row sits under: the pension row itself is read-only, and only interactive rows are recorded
                            float pensionY = RowTop(" / Pension age");   // §735: the pension row sits in its own card after the mandatory tiles - scroll to the row itself
                            if (pensionY < 0f) { pensionY = RowTop(" / Social Security"); }
                            if (pensionY < 0f) { Debug.LogError($"SHOT: {stem}_pension - the pension row's geometry was not recorded; the frame is filmed at the page's head, not at the row."); _failed++; }
                            ScrollBy(controller, Mathf.Max(0f, pensionY - UiScreen.Height * 0.14f));
                            yield return Settle();
                            yield return Capture(stem + "_pension");
                            // Board 15c-r2 (2026-09-15): the same row in 2026 and in 2034 - the game's calendar stands at 2029 on film, so the RISING and HELD
                            // windows (Italy 2026, Sweden to 2032) and the COMPLETE and CARRIED states (Germany from 2031, Sweden from 2033) need the row's
                            // own year; the field is the film's alone and is cleared before the next frame.
                            var pensionYear = controller.GetType().GetField("_pensionRowYearForFilm", BindingFlags.Instance | BindingFlags.NonPublic);
                            if (pensionYear != null)
                            {
                                foreach (int filmYear in new[] { 2026, 2034 })
                                {
                                    pensionYear.SetValue(controller, (int?)filmYear);
                                    yield return Settle();
                                    yield return Capture(stem + "_pension_" + filmYear);
                                }
                                pensionYear.SetValue(controller, null);
                            }
                            else { Debug.LogError("SHOT: board 15c-r2 - GameController._pensionRowYearForFilm was not found; the 2026 and 2034 pension frames are NOT filmed."); }
                            // §590: PN-1's DIAL - the row drafted two years past the age standing, its range caption held (the clock pinned on-drag), then the age SET BY
                            // A PASSED BILL a year under the law with no draft, so the law's ghost mark and its label are on film. Both put back before the next frame.
                            var pensionInput = controller.GetType().GetField("_pensionAgeInput", BindingFlags.Instance | BindingFlags.NonPublic);
                            var pensionPlayer = controller.GetType().GetField("_playerCountry", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(controller) as Country;
                            if (pensionInput != null && pensionPlayer != null && PensionAgeStatute.Has(pensionPlayer.Id))
                            {
                                float lawAge = PensionAgeStatute.AgeInForce(pensionPlayer.Id, pensionPlayer.CalendarYear);
                                RangeCaptionPresenter.Reset();
                                RangeCaptionPresenter.ClockOverride = 0f;
                                pensionInput.SetValue(controller, (float?)Mathf.Min(BudgetBill.PensionAgeMax, Mathf.Round(lawAge) + 2f));
                                yield return Settle();
                                yield return Capture(stem + "_pension_dial_drafted");
                                // §597: THE DRAFT SAVES WITH A GAME - through the controller's own capture, the real serializer and its own restore: the drafted age
                                // is captured, the save written to JSON and read back, the draft cleared, and the UI layer restored from the save; the age must return.
                                // Then a save of the old shape (the field removed from the JSON) must restore no draft at all.
                                var restoreDrafts = controller.GetType().GetMethod("RestoreUiDrafts", BindingFlags.Instance | BindingFlags.NonPublic);
                                var roundSim = controller.GetType().GetField("_simulationManager", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(controller) as SimulationManager;
                                var roundWorld = controller.GetType().GetField("_world", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(controller) as World;
                                float? drafted = (float?)pensionInput.GetValue(controller);
                                if (restoreDrafts != null && roundSim != null && roundWorld != null && drafted.HasValue)
                                {
                                    string json = PoliSim.Persistence.SaveGameService.Serialize(PoliSim.Persistence.SaveGameService.CreateSaveGame(roundSim, roundWorld, _countryId, controller.CaptureUiDrafts()));
                                    pensionInput.SetValue(controller, null);
                                    restoreDrafts.Invoke(controller, new object[] { PoliSim.Persistence.SaveGameService.Deserialize(json).Ui });
                                    float? back = (float?)pensionInput.GetValue(controller);
                                    if (back.HasValue && Mathf.Abs(back.Value - drafted.Value) < 1e-5f) { Debug.Log($"SHOT: §597 - the drafted pension age {drafted.Value:0.###} survived the save and load ({back.Value:0.###})."); }
                                    else { Debug.LogError($"SHOT: §597 - the drafted pension age {drafted.Value:0.###} did NOT survive the save and load (read back {(back.HasValue ? back.Value.ToString("0.###") : "none")})."); _failed++; }
                                    yield return Settle();
                                    yield return Capture(stem + "_pension_dial_reloaded");
                                    string oldShape = System.Text.RegularExpressions.Regex.Replace(json, "\"PensionAgeDraft\"\\s*:\\s*[^,}]+,?", string.Empty);
                                    restoreDrafts.Invoke(controller, new object[] { PoliSim.Persistence.SaveGameService.Deserialize(oldShape).Ui });
                                    float? none = (float?)pensionInput.GetValue(controller);
                                    if (!none.HasValue && !oldShape.Contains("PensionAgeDraft")) { Debug.Log("SHOT: §597 - a save without the field restores no pension draft."); }
                                    else { Debug.LogError($"SHOT: §597 - a save without the field restored a pension draft ({(none.HasValue ? none.Value.ToString("0.###") : "none")}), or the field was not removed."); _failed++; }
                                }
                                else { Debug.LogError("SHOT: §597 - RestoreUiDrafts, the simulation, the world or the draft was not found; the save round trip is NOT proved."); _failed++; }
                                pensionInput.SetValue(controller, null);
                                RangeCaptionPresenter.ClockOverride = null;
                                RangeCaptionPresenter.Reset();
                                float keptOverride = pensionPlayer.PensionAgeOverride;
                                pensionPlayer.PensionAgeOverride = Mathf.Max(BudgetBill.PensionAgeMin, Mathf.Round(lawAge) - 1f);
                                yield return Settle();
                                yield return Capture(stem + "_pension_dial_set_by_bill");
                                pensionPlayer.PensionAgeOverride = keptOverride;
                                yield return Settle();
                            }
                            else { Debug.LogError("SHOT: §590 - GameController._pensionAgeInput or the player's country was not found, or the country has no pension statute; the dial frames are NOT filmed."); _failed++; }
                            ScrollBy(controller, 0f);
                            yield return Settle();
                        }
                        else
                        {
                            Debug.LogError("SHOT: P3-C1 - the spending inputs dictionary was not found; the mid-drag frame is NOT filmed.");
                            _failed++;
                        }
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
                    ScrollBy(controller, stem == "07a_politics_parliament" ? CongressRowsScroll(controller, 900f) : 900f);   // PS-6 US-35: the USA's rows by their top
                    yield return Settle();
                    yield return Capture(stem + "_rows");

                    // §770 (PS-5 item C4): the President row with an election the game held - its own next rounds held on the live country through the
                    // runtime's path (the first vote on the live prediction, the run-off by B4), their days then moved before the film's day so the row reads
                    // the president-elect and the pinned slip both rounds; filmed at the rows' scroll, then the contest taken away.
                    if (stem == "07a_politics_parliament" && TwoRoundElection.RuleOf(_countryId) != null)
                    {
                        SimulationManager presSim = controller.GetType().GetField("_simulationManager", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(controller) as SimulationManager;
                        Country presCountry = presSim?.World?.GetCountry(_countryId);
                        MethodInfo pinPresident = controller.GetType().GetMethod("PinSlipOnAnchorForFilm", BindingFlags.Instance | BindingFlags.NonPublic);
                        FieldInfo presPins = controller.GetType().GetField("_slipPins", BindingFlags.Instance | BindingFlags.NonPublic);
                        PresidentialElection.Contest held = null;
                        int heldBefore = presCountry?.PresidentialElections.Count ?? 0;
                        try
                        {
                            if (presCountry != null && PresidentialElection.TryNextFirstVote(presCountry, presSim.CurrentDate, out DateTime presFirst, out _, out _)
                                && NationalElection.TryPredictShares(_countryId, out Dictionary<string, double> presShares, EconomicVote.RecordOverTerm(presCountry, presFirst, out _), on: presFirst))
                            {
                                held = PresidentialElection.HoldRounds(presCountry, presFirst, presShares);
                                if (held != null && held.RunOffPending()) { PresidentialElection.HoldRounds(presCountry, held.RunOffOn, null); }
                            }
                            if (held == null || !held.Decided() || pinPresident == null || presPins == null)
                            {
                                Debug.LogError($"SHOT: §770 - the game's presidential rounds were not held for the film ({held?.Line ?? "no first vote"}); {stem}_president_elect NOT written.");
                                _failed++;
                            }
                            else
                            {
                                // PLANTED: the rounds are the game's own, their days moved - the first vote 20 days before the film's day, a run-off its 14 after,
                                // the oath 50 days ahead - so the row reads a president-elect; an outright winner has no run-off to move
                                TimeSpan back = presSim.CurrentDate.AddDays(-20) - held.FirstVote;
                                held.FirstVote += back;
                                if (held.RunOffA != null) { held.RunOffOn += back; }
                                held.TakesOffice = presSim.CurrentDate.AddDays(50);
                                Debug.Log($"SHOT: §770 - PLANTED: the game's own presidential rounds held for the film ({held.Line}); their days moved to {held.FirstVote:yyyy-MM-dd}"
                                    + (held.RunOffA != null ? $" and {held.RunOffOn:yyyy-MM-dd}" : string.Empty) + $", the oath {held.TakesOffice:yyyy-MM-dd}");
                                yield return Settle();
                                pinPresident.Invoke(controller, new object[] { "president", new Vector2(UiScreen.Width * 0.30f, UiScreen.Height * 0.50f) });
                                yield return Settle();
                                yield return Settle();
                                yield return Capture(stem + "_president_elect");

                                // SP-7b: the same row with the winner the player's candidate - PLANTED (the contest marked, the country's candidate set), so the
                                // row and its pinned slip read the verdict for the player (YOU WON, the oath's day, the party the AI's while he serves); undone below
                                held.PlayerCandidate = held.Elected;
                                presCountry.PlayerCandidate = held.Elected;
                                Debug.Log($"SHOT: SP-7b - PLANTED: {held.Elected} marked the player's candidate for the President row's verdict");
                                yield return Settle();
                                yield return Capture(stem + "_president_you_won");
                            }
                        }
                        finally
                        {
                            if (presCountry != null) { presCountry.PlayerCandidate = null; }   // SP-7b: the film's session is a Sejm start - no candidate
                            (presPins?.GetValue(controller) as IList)?.Clear();
                            if (presCountry != null && presCountry.PresidentialElections.Count > heldBefore) { presCountry.PresidentialElections.RemoveRange(heldBefore, presCountry.PresidentialElections.Count - heldBefore); }
                        }
                        yield return Settle();
                    }

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
                            detailScroll.SetValue(controller, new Vector2(0f, UiScreen.Height * 0.3f));
                            yield return Settle();
                            yield return Capture("06g_laws_expected_effects");
                            ResetScrolls(controller);
                        }

                        // P4-C2 (2026-09-04): a law in force shows ENACTED · REPEALABLE. One law is enacted through the real
                        // effects path (SimulationManager's private ApplyLawBillEffects, LawCompositionDiagnostic's idiom),
                        // the browser filmed at rest with it in force, the law repealed the same way, and every public float
                        // on the country, its state and its sectors asserted BIT-IDENTICAL to before the enactment - the
                        // approval excepted (enactment's price, which a repeal does not refund) and restored by hand so the
                        // captures after this one see the untouched country.
                        var lawSim = controller.GetType().GetField("_simulationManager", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(controller) as SimulationManager;
                        Country lawCountry = lawSim?.World?.GetCountry(_countryId);
                        MethodInfo applyLaw = typeof(SimulationManager).GetMethod("ApplyLawBillEffects", BindingFlags.Instance | BindingFlags.NonPublic);
                        if (lawSim == null || lawCountry == null || applyLaw == null)
                        {
                            Debug.LogError("SHOT: P4-C2 - the law effects path could not be reached by reflection; the repealable capture is MISSING, not clean.");
                        }
                        else
                        {
                            const string stagedLawId = "truth_in_sentencing_act";
                            float approvalUntouched = lawCountry.State.ApprovalRating;
                            List<(string Name, int Bits)> untouched = SnapshotFloats(lawCountry);
                            FieldInfo selectedLaw = controller.GetType().GetField("_selectedLawId", BindingFlags.Instance | BindingFlags.NonPublic);
                            object selectedBefore = selectedLaw?.GetValue(controller);
                            applyLaw.Invoke(lawSim, new object[] { lawCountry, new LawBill { LawId = stagedLawId, IsRepeal = false } });
                            ResetScrolls(controller);
                            selectedLaw?.SetValue(controller, stagedLawId);   // the detail pane carries the status cell; select the law in force so the mark is on the frame
                            yield return Settle();
                            yield return Capture("06g_laws_repealable");
                            // §662 (Design's D24 ask of Code, 3): a law row carrying the interest rate's icon - the monetary regime's emblem - under the All
                            // chip, so the category cell draws the pair; found by the browser's own search, then the search cleared.
                            FieldInfo lawSearch = controller.GetType().GetField("_lawSearchText", BindingFlags.Instance | BindingFlags.NonPublic);
                            if (lawSearch != null)
                            {
                                object searchBefore = lawSearch.GetValue(controller);
                                lawSearch.SetValue(controller, "Inflation Target");
                                ResetScrolls(controller);
                                yield return Settle();
                                yield return Settle();
                                yield return Capture("06i_laws_monetary_row");
                                lawSearch.SetValue(controller, searchBefore ?? string.Empty);
                                yield return Settle();
                            }
                            else { Debug.LogError("SHOT: §662 - the controller has no _lawSearchText; 06i_laws_monetary_row NOT written."); _failed++; }
                            applyLaw.Invoke(lawSim, new object[] { lawCountry, new LawBill { LawId = stagedLawId, IsRepeal = true } });
                            List<(string Name, int Bits)> restored = SnapshotFloats(lawCountry);
                            int drifted = 0;
                            for (int k = 0; k < untouched.Count && k < restored.Count; k++)
                            {
                                if (untouched[k].Name == "State.ApprovalRating") { continue; }
                                if (untouched[k].Bits != restored[k].Bits)
                                {
                                    drifted++;
                                    Debug.LogError($"SHOT: P4-C2 - {untouched[k].Name} is not byte-identical after enact-then-repeal of {stagedLawId}.");
                                }
                            }
                            float price = LawCatalog.GetById(stagedLawId)?.EnactmentApprovalCost ?? 0f;
                            Debug.Log(drifted == 0
                                ? $"SHOT: P4-C2 [{stem}] - {untouched.Count - 1} quantities byte-identical after enact-then-repeal of {stagedLawId}; approval sat {price:F1} below untouched (the price paid to pass it), restored for the captures that follow."
                                : $"SHOT: P4-C2 [{stem}] - {drifted} quantit(ies) drifted after enact-then-repeal of {stagedLawId}.");
                            lawCountry.State.ApprovalRating = approvalUntouched;
                            selectedLaw?.SetValue(controller, selectedBefore);
                            // P4-C3 (2026-09-04): one card per structural category on film - the law selected, its EXPECTED EFFECTS line in the parameter's own unit.
                            foreach ((string lawId, string capture) in new[] { ("hartz_benefit_reform_act", "06h_laws_institutions_card"), ("constitutional_debt_brake_act", "06h_laws_fiscal_card"), ("full_employment_mandate_act", "06h_laws_monetary_card"), ("industrial_electricity_tax_relief_act", "06h_laws_electricity_card") })
                            {
                                selectedLaw?.SetValue(controller, lawId);
                                ResetScrolls(controller);
                                detailScroll.SetValue(controller, new Vector2(0f, UiScreen.Height * 0.3f));   // the effects and the citation sit under the name and the magnitude; the same scroll 06g uses
                                yield return Settle();
                                yield return Capture(capture);
                            }
                            selectedLaw?.SetValue(controller, selectedBefore);
                            yield return Settle();
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
            yield return CaptureDeskByRole(controller);   // §694: last, because it tables bills - no frame before it moves
            if (_countryId == CountryId.Poland) { yield return CapturePolishDeskVerbs(controller); }   // K197: after it - the verbs change who governs

            EndSweep();
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

                // P6-B1 (2026-09-17): the same pinned node in PROVENANCE. The pane's DERIVED / DECLARED
                // count moved behind the `†` this item, and a line that moved has to be readable in the
                // state it moved to - otherwise "nothing is deleted" is a claim with no frame behind it.
                _provenanceToRestore = DeskProvenance.On;
                DeskProvenance.On = true;
                ResetScrolls(controller);
                yield return Settle();
                yield return Settle();
                yield return Capture("06l_policylaws_policyweb_node_provenance");
                DeskProvenance.On = _provenanceToRestore.Value;
                _provenanceToRestore = null;
                yield return Settle();

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
            StageDivisionWithContent(player, sim, "Harness: staged division for the entrance capture (R-C6)", 30f, passed: true);   // P2-4.3: with sides and an estimate
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

            // (c2) §761 (PS-5): THE SEJM'S VOTE ON THE PRESIDENT'S VETO - the plate's line at 3/5 of those voting, its stamp VETO STANDS. Staged through the
            // game's own gate (a planted passage on the 2023 Sejm's lists, PiS and Konfederacja against, handed to SimulationManager.PresidentialVetoGate),
            // so the frame shows the record play writes; filmed real: the surface is Canvas.
            if (player.Id == CountryId.Poland)
            {
                StageVetoDivision(player, sim);
                InvokeNoArg(controller, "TriggerSigningForNewestDivision");
                yield return WaitForCanvasSettle(controller, wantActive: true);
                yield return Settle();
                Claim("signing");
                yield return Capture("89g_signing_veto");
                RecordCanvasTextAssert("89g_signing_veto", controller);
                InvokeNoArg(controller, "SignPendingDivision");
                yield return WaitForCanvasSettle(controller, wantActive: false);
                yield return Settle();

                // (c2b) SP-7b: THE PLATE'S TWO ANSWERS - a player-president's statute on his docket: the line (on your desk, the day it is signed unanswered),
                // VETO on the paper face and SIGN on the brass. Staged on the newest division by the controller's hook (no statute is held, so the answer
                // writes nothing; the beat closes it); filmed real: the surface is Canvas.
                StageDivisionWithContent(player, sim, "Harness: staged statute for the answer plate (SP-7b)", 30f, passed: true);   // a statute the Sejm passed, with sides and an estimate
                InvokeNoArg(controller, "TriggerAnswerPlateForNewestDivision");
                yield return WaitForCanvasSettle(controller, wantActive: true);
                yield return Settle();
                Claim("signing");
                yield return Capture("89j_signing_answer");
                RecordCanvasTextAssert("89j_signing_answer", controller);
                InvokeNoArg(controller, "SignPendingDivision");
                yield return WaitForCanvasSettle(controller, wantActive: false);
                yield return Settle();
            }

            // (c3) PS-6 US-21: THE US VETO PLATE - a bill both chambers sent, vetoed by the president of the day (rule (a)) and put to the override, each chamber's
            // vote on the veto its own division. Staged through the game's own gate on the film's own world (Congress.Divide draws the defectors, then
            // SimulationManager.UsVetoGate answers and records), the other major party's bill against the president's party; the draw's indices PLANTED
            // (UsVeto.PlantIndex) so that Congress sends it: 89h the bill's first chamber sustaining the veto (the other never voting on it), 89i an override
            // carrying in both chambers. Filmed real: the surface is Canvas.
            if (player.Id == CountryId.USA)
            {
                foreach ((string shot, bool overridden) in new[] { ("89h_signing_us_veto", false), ("89i_signing_us_override", true) })
                {
                    if (!StageUsVetoDivision(player, sim, overridden)) { Debug.LogError($"SHOT: {shot} could not be staged through the gate - MISSING, not clean."); continue; }
                    InvokeNoArg(controller, "TriggerSigningForNewestDivision");
                    yield return WaitForCanvasSettle(controller, wantActive: true);
                    yield return Settle();
                    Claim("signing");
                    yield return Capture(shot);
                    RecordCanvasTextAssert(shot, controller);
                    InvokeNoArg(controller, "SignPendingDivision");
                    yield return WaitForCanvasSettle(controller, wantActive: false);
                    yield return Settle();
                }

                // (c3b) PS-6 US-23: THE PRESIDENT'S PEN - the out-party majority's reconciliation on the player-president's desk, and his veto put to the
                // override. Staged on the film's own world through the game's own path (the majority's book, both chambers, the US veto gate's docket, the
                // verb): 89k the answer plate (SIGN, VETO, LATER) on the bill the desk holds, 89l the House's vote on his veto. Filmed real: the surface is Canvas.
                DivisionRecord deskPassage = null;
                Action restoreDesk = null;
                try
                {
                    if (!StageUsDeskReconciliation(player, sim, out deskPassage, out restoreDesk)) { Debug.LogError("SHOT: 89k_signing_us_desk could not be staged - 89k and 89l are MISSING, not clean."); }
                    else
                    {
                        InvokeNoArg(controller, "TriggerSigningForNewestDivision");
                        yield return WaitForCanvasSettle(controller, wantActive: true);
                        yield return Settle();
                        Claim("signing");
                        yield return Capture("89k_signing_us_desk");
                        RecordCanvasTextAssert("89k_signing_us_desk", controller);
                        InvokeNoArg(controller, "AnswerLater");   // LATER: nothing recorded, the bill stays on the desk - the veto below is the verb's own
                        yield return WaitForCanvasSettle(controller, wantActive: false);
                        yield return Settle();
                        if (!sim.VetoDocketBill(player.Id, deskPassage, out string vetoWhy)) { Debug.LogError($"SHOT: 89l_signing_us_desk_veto - the veto was refused ({vetoWhy}) - MISSING, not clean."); }
                        else
                        {
                            DivisionRecord vote = player.Divisions.Entries[player.Divisions.Entries.Count - 1];
                            Debug.Log($"SHOT: 89l - the player-president's veto put to the override: No. {vote.Number}: {vote.Title} ({(vote.Passed ? "overridden" : "the veto stands")})");
                            if (vote.Passed) { Debug.LogError("SHOT: 89l_signing_us_desk_veto - the staged veto was OVERRIDDEN, not the asked frame (the veto standing); the rates the law moved are restored after."); }
                            InvokeNoArg(controller, "TriggerSigningForNewestDivision");
                            yield return WaitForCanvasSettle(controller, wantActive: true);
                            yield return Settle();
                            Claim("signing");
                            yield return Capture("89l_signing_us_desk_veto");
                            RecordCanvasTextAssert("89l_signing_us_desk_veto", controller);
                            InvokeNoArg(controller, "SignPendingDivision");
                            yield return WaitForCanvasSettle(controller, wantActive: false);
                            yield return Settle();
                        }
                    }
                }
                finally { restoreDesk?.Invoke(); }
            }

            // (d) PS-3f (§633, ruled): a BUDGET division is a contest of two proposals - the signing screen names each with its votes, the abstentions,
            // and stamps the adopted one. Staged as the frame decision the model records (S's alternative against the government's frames, the
            // government's own parties carrying them), and filmed real: the surface is Canvas.
            if (player.Id != CountryId.Sweden) { Debug.Log("SHOT: 89f_signing_budget_contest is Sweden's (the staged sides name its parties) - skipped for this country, not missing."); yield break; }
            StageBudgetContestDivision(player, sim);
            InvokeNoArg(controller, "TriggerSigningForNewestDivision");
            yield return WaitForCanvasSettle(controller, wantActive: true);
            yield return Settle();
            Claim("signing");
            yield return Capture("89f_signing_budget_contest");
            RecordCanvasTextAssert("89f_signing_budget_contest", controller);
            InvokeNoArg(controller, "SignPendingDivision");
            yield return WaitForCanvasSettle(controller, wantActive: false);
            yield return Settle();
        }

        /// <summary>§761: a statute's passage on the 2023 Sejm's lists - PiS and Konfederacja against, the governing lists for - handed to the game's own veto
        /// gate by reflection, which marks the passage a motion and records the vote on the veto as play would. F3: the statute is at risk and the gate
        /// draws; the frame is the veto's, so the draw is PLANTED to veto (`PresidentialVeto.PlantDraw`) - the rest of the record is the gate's own.</summary>
        private static void StageVetoDivision(Country player, SimulationManager sim)
        {
            var seats = new (string Abbrev, int Seats, int Side)[] { ("PiS", 194, -1), ("KO", 157, 1), ("TD", 65, 1), ("NL", 26, 1), ("Konf", 18, -1) };
            var sides = new List<DivisionSide>();
            foreach ((string abbrev, int n, int side) in seats)
            {
                sides.Add(new DivisionSide { Abbrev = abbrev, ShortName = PartySystems.ShortName(player.Id, abbrev), Seats = n, Side = side, Alignment = side * 0.3f,
                    Reason = side > 0 ? "for the statute" : "against the statute", ReasonShort = side > 0 ? "for" : "against" });
            }
            DivisionRecord passage = player.Divisions.Append("Enact: Cash Bail Reform Act", sim.CurrentDate, 0.08f, true, 1f, (int)BillAxis.Fiscal, sides);
            MethodInfo gate = typeof(SimulationManager).GetMethod("PresidentialVetoGate", BindingFlags.Instance | BindingFlags.NonPublic);
            if (gate == null) { Debug.LogError("SHOT: SimulationManager.PresidentialVetoGate not found - 89g_signing_veto is MISSING, not clean."); return; }
            bool stands;
            using (PresidentialVeto.PlantDraw(0.0)) { stands = (bool)gate.Invoke(sim, new object[] { player, passage, true, PresidentialVeto.Act.OrdinaryStatute }); }
            DivisionRecord newest = player.Divisions.Entries[player.Divisions.Entries.Count - 1];
            Debug.Log($"SHOT: staged the vote on the President's veto through the game's own gate - stands {stands}, line {newest.Required}: {newest.Title}");
        }

        /// <summary>
        /// PS-6 US-21: a US bill both chambers send and the president of the day vetoes, through the game's own gate. The president is read as the gate reads him
        /// (`Congress.PresidentOn`); for the staging the player's party is set to the OTHER major party - so the president is not the player's and answers by rule
        /// (a) - and restored after; the bill is that party's own, the first of a few dial moves toward its end that the president's party stands against in both
        /// chambers. The draw's indices are PLANTED (`UsVeto.PlantIndex`): the other party's held at its lists' first entry, the president's party's searched
        /// chamber by chamber for the first pair that sends the bill and gives the asked override - sustained in the bill's first chamber, or carried in both. The
        /// gate then runs whole (`ParliamentSystem.Divide`, `SimulationManager.UsVetoGate`) and its records are the frame's.
        /// </summary>
        private static bool StageUsVetoDivision(Country player, SimulationManager sim, bool overridden)
        {
            DateTime day = sim.CurrentDate;
            if (!Congress.Applies(player) || !Congress.PresidentOn(player, day, out string president, out string pp, out _) || (pp != "REP" && pp != "DEM"))
            {
                Debug.Log($"SHOT: the US veto is not staged on {day:yyyy-MM-dd} - no Congress seated, or no president of REP or DEM found on the day.");
                return false;
            }
            string op = pp == "REP" ? "DEM" : "REP";
            string mine = player.PlayerPartyAbbrev;
            Dictionary<string, int> house0 = player.ParliamentSeats, senate0 = player.SecondChamberSeats;
            player.PlayerPartyAbbrev = op;
            try
            {
                if (StageUsVetoOn(player, sim, overridden, day, president, pp, op)) { return true; }
                if (!overridden) { return false; }
                // An override needs two thirds on the passage's sides, and the crossers enter a chamber only where they change its verdict - so where the other
                // party holds the House its bill passes there on the blocs, no crosser enters, and its bloc alone is short of two thirds: no plant overrides.
                // STAGED, said here and in the log: the two parties' seats swapped in both chambers for this frame alone (the president's party holding them),
                // restored the moment the gate has written its records - the plate reads the records, never the seats
                player.ParliamentSeats = SwappedSeats(house0, pp, op);
                player.SecondChamberSeats = SwappedSeats(senate0, pp, op);
                Debug.Log($"SHOT: 89i - no plant overrides on the film's chambers; {pp}'s and {op}'s seats swapped in both chambers for this staging alone, restored after.");
                return StageUsVetoOn(player, sim, overridden, day, president, pp, op);
            }
            finally { player.PlayerPartyAbbrev = mine; player.ParliamentSeats = house0; player.SecondChamberSeats = senate0; }
        }

        /// <summary>
        /// PS-6 US-23: THE MAJORITY'S RECONCILIATION ON THE PLAYER-PRESIDENT'S DESK, staged through the game's own path. The player is made the president of the day
        /// as the oath makes one (a contest of the game's own won by the player's party, sworn on the day before the film's day - `Congress.PresidentOn` reads the
        /// game's latest sworn contest - and its government installed); the other major party holds both chambers - as seated, or, where it does not, the two
        /// parties' seats swapped for this staging alone (said in the log); its book is tabled in the manager's slot on its last day of course and the manager's
        /// own day step votes and presents it (`SimulationManager.ResolveMajorityReconciliationDay`): both chambers send it and the player-president's desk holds it.
        /// <paramref name="restore"/> puts back the contests, the government, the seats, the tax rates (an override that carried would have moved them - the review's L4), the slot and the year's mark (the divisions stay, as every staging's do).
        /// </summary>
        private static bool StageUsDeskReconciliation(Country player, SimulationManager sim, out DivisionRecord passage, out Action restore)
        {
            passage = null;
            restore = null;
            DateTime day = sim.CurrentDate;
            string pp = player.PlayerPartyAbbrev;
            if (!Congress.Applies(player) || (pp != "REP" && pp != "DEM")) { Debug.Log($"SHOT: 89k - no Congress seated, or the player's party ({pp}) is neither REP nor DEM - not staged."); return false; }
            string op = pp == "REP" ? "DEM" : "REP";
            FieldInfo slotField = typeof(SimulationManager).GetField("_pendingMajorityReconciliationByCountry", BindingFlags.Instance | BindingFlags.NonPublic);
            MethodInfo resolve = typeof(SimulationManager).GetMethod("ResolveMajorityReconciliationDay", BindingFlags.Instance | BindingFlags.NonPublic, null, new[] { typeof(Country), typeof(BudgetBill) }, null);
            if (slotField == null || resolve == null || !(slotField.GetValue(sim) is Dictionary<CountryId, BudgetBill> slot)) { Debug.LogError("SHOT: SimulationManager's majority reconciliation slot or day step not found - 89k is MISSING, not clean."); return false; }
            GovernmentRecord government0 = player.Government;
            var rates0 = new List<(TaxLine Line, float Rate)>();
            foreach (TaxLine line in player.TaxLines) { rates0.Add((line, line.Rate)); }   // the review's L4: an override that carries writes the book's rates - put back in every outcome
            Dictionary<string, int> house0 = player.ParliamentSeats, senate0 = player.SecondChamberSeats;
            int year0 = player.MajorityReconciliationYear;
            slot.TryGetValue(player.Id, out BudgetBill slot0);
            var contest = new UsPresidentialElection.Contest { PollingDay = day.AddDays(-76), TakesOffice = day.AddDays(-1), Winner = pp == "DEM" ? "Kamala D. Harris" : "Donald J. Trump", WinnerParty = pp, ElectorsAppointed = 538, Line = "staged (the film's desk, US-23)" };
            contest.Electors.Add(new UsPresidentialElection.Tally { Candidate = contest.Winner, Party = pp, Votes = 270 });
            contest.Electors.Add(new UsPresidentialElection.Tally { Candidate = op == "DEM" ? "Kamala D. Harris" : "Donald J. Trump", Party = op, Votes = 268 });
            player.UsPresidentialElections.Add(contest);
            player.Government = UsPresidentialElection.GovernmentOf(contest, contest.TakesOffice);
            restore = () =>
            {
                player.UsPresidentialElections.Remove(contest);
                player.Government = government0;
                player.ParliamentSeats = house0;
                player.SecondChamberSeats = senate0;
                player.MajorityReconciliationYear = year0;
                foreach ((TaxLine line, float rate) in rates0) { line.Rate = rate; }
                if (slot0 != null) { slot[player.Id] = slot0; } else { slot.Remove(player.Id); }
            };
            if (!UsMajorityReconciliation.TryOutPartyMajority(player, day, out string majority, out _) || majority != op)
            {
                // s930: swap only a chamber the opposition does not lead - since MF q29 the world's own count can split the Congress (one chamber each), and
                // swapping both would hand the player the chamber the opposition already held
                int Seats(Dictionary<string, int> chamber, string key) => chamber != null && chamber.TryGetValue(key, out int s) ? s : 0;
                // by HOLDING, not by seats: a chamber equally divided goes to the vice president's party (Trump's), so a tie is broken by one seat moved to the opposition
                if (Congress.HouseMajority(player) != op) { player.ParliamentSeats = Seats(house0, op) < Seats(house0, pp) ? SwappedSeats(house0, pp, op) : new Dictionary<string, int>(house0); if (Congress.HouseMajority(player) != op) { player.ParliamentSeats = MovedSeat(player.ParliamentSeats, pp, op); } }
                if (Congress.SenateMajority(player, day) != op) { player.SecondChamberSeats = Seats(senate0, op) < Seats(senate0, pp) ? SwappedSeats(senate0, pp, op) : new Dictionary<string, int>(senate0); if (Congress.SenateMajority(player, day) != op) { player.SecondChamberSeats = MovedSeat(player.SecondChamberSeats, pp, op); } }
                Debug.Log($"SHOT: 89k - {op} does not hold both chambers on the film's world; {pp}'s and {op}'s seats swapped in each chamber {op} did not lead, for this staging alone, restored after.");
                Debug.Log($"SHOT: 89k - after the staging the House is held by {Congress.HouseMajority(player) ?? "no one"}, the Senate by {Congress.SenateMajority(player, day) ?? "no one"}.");
            }
            if (!Congress.PresidentOn(player, day, out string president, out _, out bool playerPresident) || !playerPresident || !UsMajorityReconciliation.TryOutPartyMajority(player, day, out majority, out _) || majority != op)
            {
                Debug.LogError($"SHOT: 89k - the staging did not give a player-president ({president}) facing a {op} Congress - MISSING, not clean.");
                return false;
            }
            BudgetBill bill = UsMajorityReconciliation.Book(player, op, out string why);
            if (bill == null) { Debug.LogError($"SHOT: 89k - {op}'s book moves nothing ({why}) - MISSING, not clean."); return false; }
            bill.DaysRemaining = 1;   // its last day of course: the day step below votes it
            slot[player.Id] = bill;
            resolve.Invoke(sim, new object[] { player, bill });
            passage = slot.TryGetValue(player.Id, out BudgetBill held) ? held.Docket?.Passage : null;   // the day step builds the book again on the vote day (the review's M1): the slot holds the bill it voted
            Debug.Log($"SHOT: staged {op}'s reconciliation on the desk of the player-president ({president}, {pp}) through the manager's own day step: {(passage != null ? "held - No. " + passage.Number + ": " + passage.Title : "NOT HELD")}");
            return passage != null && sim.TryUnansweredDocketItem(player.Id, passage, out _);
        }

        /// <summary>PS-6 US-21: two parties' seats swapped in a copy of a chamber's seating.</summary>
        private static Dictionary<string, int> MovedSeat(Dictionary<string, int> seats, string from, string to)
        {
            var moved = new Dictionary<string, int>(seats);
            moved.TryGetValue(from, out int nf);
            moved.TryGetValue(to, out int nt);
            if (nf > 0) { moved[from] = nf - 1; moved[to] = nt + 1; }
            return moved;
        }

        private static Dictionary<string, int> SwappedSeats(Dictionary<string, int> seats, string a, string b)
        {
            var swapped = new Dictionary<string, int>(seats);
            seats.TryGetValue(a, out int na);
            seats.TryGetValue(b, out int nb);
            swapped[a] = nb;
            swapped[b] = na;
            return swapped;
        }

        /// <summary>PS-6 US-21: the staging on the chambers as they stand - the first candidate bill with a plant giving the asked shape, through the gate.</summary>
        private static bool StageUsVetoOn(Country player, SimulationManager sim, bool overridden, DateTime day, string president, string pp, string op)
        {
            {
                foreach ((string title, BillConcern concern) in UsVetoCandidates(player, op))
                {
                    int origin = Congress.OriginOf(player, concern, day);
                    Congress.Verdict v = Congress.Project(player, concern, day, origin);
                    if (!SideIs(v.House, pp, -1) || !SideIs(v.Senate, pp, -1) || !SideIs(v.House, op, 1) || !SideIs(v.Senate, op, 1)) { continue; }
                    if (!UsVetoPlant(v, pp, op, overridden, out int house, out int senate)) { continue; }
                    MethodInfo gate = typeof(SimulationManager).GetMethod("UsVetoGate", BindingFlags.Instance | BindingFlags.NonPublic, null, new[] { typeof(Country), typeof(DivisionRecord), typeof(BillConcern), typeof(bool), typeof(DocketEntry).MakeByRefType(), typeof(bool) }, null);   // by its full parameter list: an arity change fails here by name
                    if (gate == null) { Debug.LogError("SHOT: SimulationManager.UsVetoGate not found - the US veto plate is MISSING, not clean."); return false; }
                    bool sent, law;
                    using (UsVeto.PlantIndex((chamber, party, n) => party == pp ? (chamber == UsVeto.Chamber.House ? house : senate) : 0))
                    {
                        DivisionRecord passage = ParliamentSystem.Divide(player, title, concern, day, out sent);
                        law = gate.Invoke(sim, new object[] { player, passage, concern, sent, null, false }) is bool decided && decided;   // every parameter given (no docket held, not an amendment): MethodInfo.Invoke fills no optional one; a bill held on a player-president's docket is not law on the day
                    }
                    DivisionRecord newest = player.Divisions.Entries[player.Divisions.Entries.Count - 1];
                    bool shaped = sent && newest.Required > 0 && !newest.Cloture && newest.Passed == overridden && law == overridden && (newest.OtherChamber != null) == overridden;
                    Debug.Log($"SHOT: staged the US veto through the game's own gate - the president ({president}, {pp}) answering {op}'s '{title}', the draw planted at the president's party's index {house} in the House and {senate} in the Senate, the other party's at 0: sent {sent}, law {law}; the ceremony No. {newest.Number}: {newest.Title}{(shaped ? string.Empty : " - NOT THE ASKED SHAPE")}");
                    return shaped;
                }
                Debug.Log($"SHOT: no staged bill of {op}'s has {pp} against and {op} for in both chambers with a plant giving the asked override - the US veto plate is not staged on these chambers.");
                return false;
            }
        }

        /// <summary>PS-6 US-21: the staged bills, in order - a crime-and-justice dial and a labour dial moved toward <paramref name="author"/>'s end (REP's up the
        /// sentencing and down the minimum wage, DEM's the other way), each with the title play gives its kind.</summary>
        private static IEnumerable<(string Title, BillConcern Concern)> UsVetoCandidates(Country u, string author)
        {
            float toward = author == "REP" ? 1f : -1f;
            yield return ("Crime & Justice bill", ParliamentSystem.GetCrimeJusticeBillConcern(u, new CrimeJusticePolicyBill { PoliceFunding = u.PoliceFundingLevel, SentencingSeverity = Mathf.Clamp(u.SentencingSeverity + 15f * toward, 0f, 100f),
                BailReform = u.BailReformLevel, DrugPolicy = u.DrugPolicyLevel, JudicialFunding = u.JudicialFundingLevel, BorderEnforcement = u.BorderEnforcementLevel }));
            yield return ("Crime & Justice bill", ParliamentSystem.GetCrimeJusticeBillConcern(u, new CrimeJusticePolicyBill { PoliceFunding = Mathf.Clamp(u.PoliceFundingLevel + 15f * toward, 0f, 100f), SentencingSeverity = u.SentencingSeverity,
                BailReform = u.BailReformLevel, DrugPolicy = u.DrugPolicyLevel, JudicialFunding = u.JudicialFundingLevel, BorderEnforcement = Mathf.Clamp(u.BorderEnforcementLevel + 15f * toward, 0f, 100f) }));
            yield return ("Labor Market bill", ParliamentSystem.GetLaborBillConcern(u, new LaborPolicyBill { MinimumWage = u.MinimumWagePercentOfMedianBase - 10f * toward, PaidFamilyLeaveWeeks = u.PaidFamilyLeaveWeeksBase - 10f * toward,
                OvertimeRegulation = u.OvertimeRegulationBase, RetrainingProgram = u.RetrainingProgramBase, FamilyPolicy = u.FamilyPolicyBase, ImmigrationPolicy = u.ImmigrationPolicyBase }));
        }

        private static bool SideIs(Congress.Division d, string party, int side)
        {
            foreach (PartyStance s in d.Stances) { if (s.Party.Abbrev == party) { return s.Measured && s.Seats > 0 && Math.Sign(s.Side) == side; } }
            return false;
        }

        /// <summary>PS-6 US-21: the president's party's planted index in each chamber (the other party's at 0) for the first pair under which both chambers pass the
        /// bill and the override comes out as asked - the gate's own rule restated over the projection: crossers entering a division only where they change it,
        /// the override on the passage's sides, the bill's first chamber first.</summary>
        private static bool UsVetoPlant(Congress.Verdict v, string pp, string op, bool overridden, out int house, out int senate)
        {
            house = senate = -1;
            IReadOnlyList<(int Defectors, int Voting)> hp = Congress.CellOf(Congress.House, true), ho = Congress.CellOf(Congress.House, false), sp = Congress.CellOf(Congress.Senate, true), so = Congress.CellOf(Congress.Senate, false);
            if (hp == null || ho == null || sp == null || so == null) { return false; }
            int Seats(Congress.Division d, string party) { foreach (PartyStance s in d.Stances) { if (s.Party.Abbrev == party) { return s.Seats; } } return 0; }
            int Members(Congress.Division d) { int n = 0; foreach (PartyStance s in d.Stances) { n += s.Seats; } return d.Chamber == Congress.Senate ? Math.Max(n, d.Sworn) : n; }
            (bool Pass, int Yea) Chamber(Congress.Division d, int kp, int ko)
            {
                int moved = kp - ko, yea = d.For + moved, nay = d.Against - moved;
                bool pass = d.Cloture ? yea >= d.Required : yea > nay;
                return (pass, pass != d.Passed ? yea : d.For);   // the crossers enter the division only where they change its verdict
            }
            int koH = UsVeto.Crossers(Seats(v.House, op), ho[0].Defectors, ho[0].Voting), koS = UsVeto.Crossers(Seats(v.Senate, op), so[0].Defectors, so[0].Voting);
            for (int jh = 0; jh < hp.Count; jh++)
            {
                (bool hPass, int hYea) = Chamber(v.House, UsVeto.Crossers(Seats(v.House, pp), hp[jh].Defectors, hp[jh].Voting), koH);
                if (!hPass) { continue; }
                bool hCarries = UsVeto.OverrideCarries(hYea, Members(v.House) - hYea);
                for (int js = 0; js < sp.Count; js++)
                {
                    (bool sPass, int sYea) = Chamber(v.Senate, UsVeto.Crossers(Seats(v.Senate, pp), sp[js].Defectors, sp[js].Voting), koS);
                    if (!sPass) { continue; }
                    bool sCarries = UsVeto.OverrideCarries(sYea, Members(v.Senate) - sYea);
                    bool firstCarries = v.Origin == Congress.Senate ? sCarries : hCarries, secondCarries = v.Origin == Congress.Senate ? hCarries : sCarries;
                    if (overridden ? firstCarries && secondCarries : !firstCarries) { house = jh; senate = js; return true; }
                }
            }
            return false;
        }

        /// <summary>PS-3f (§633): the frame decision as the model records it - the government's parties carrying its frames, the rest by alignment toward S's alternative or abstaining - with the contest on the record.</summary>
        private static void StageBudgetContestDivision(Country player, SimulationManager sim)
        {
            var sides = new List<DivisionSide>();
            int forG = 0, forA = 0, abst = 0;
            string alternativeBy = "S";
            foreach (PoliticalParty party in PartySystems.For(player.Id))
            {
                int seats = player.ParliamentSeats.TryGetValue(party.Abbrev, out int n) ? n : 0;
                if (seats <= 0) { continue; }
                PoliSim.Elections.PlayerRole role = player.Government != null ? player.Government.RoleOf(party.Abbrev) : PoliSim.Elections.PlayerRole.None;
                bool governmentParty = role == PoliSim.Elections.PlayerRole.PrimeMinister || role == PoliSim.Elections.PlayerRole.JuniorPartner || role == PoliSim.Elections.PlayerRole.Support;
                int side = governmentParty ? 1 : party.Abbrev == alternativeBy || party.Abbrev == "V" || party.Abbrev == "MP" ? -1 : 0;
                if (side > 0) { forG += seats; } else if (side < 0) { forA += seats; } else { abst += seats; }
                string why = governmentParty ? "the government's own party - carries its frames" : side < 0 ? PoliSim.Wording.EnglishText.Possessive(alternativeBy, false, true) + " frames (+0.40) over the government's (+0.00)" : "abstains - aligned with neither";
                sides.Add(new DivisionSide { Abbrev = party.Abbrev, ShortName = party.ShortName, Seats = seats, Side = side, Alignment = side < 0 ? 0.4f : 0f, Reason = why });
            }
            bool governmentAdopted = forG >= forA;
            string title = governmentAdopted ? $"Annual budget: the government's frames adopted, {forG} to {forA}, over {PoliSim.Wording.EnglishText.Possessive(alternativeBy, false, true)} alternative" : $"Annual budget: {PoliSim.Wording.EnglishText.Possessive(alternativeBy, false, true)} alternative frames adopted, {forA} to {forG}, over the government's";
            player.Divisions.Append(title, sim.CurrentDate, 0f, true, 0f, (int)BillAxis.Fiscal, sides);
            player.Divisions.Entries[player.Divisions.Entries.Count - 1].Contest = new DivisionContest { ProposalFor = "THE GOVERNMENT'S FRAMES", ProposalAgainst = PoliSim.Wording.EnglishText.Possessive(alternativeBy, true, true) + " ALTERNATIVE", VotesFor = forG, VotesAgainst = forA, Abstentions = abst, AlternativeAdopted = !governmentAdopted };
            Debug.Log($"SHOT: staged the budget contest division - {title}");
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
        ///     COMPLETED.md §198 §D0), so the first cut of this save - named for a pending selection
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

            // PS-3c (§630): the bills staged here are the GOVERNMENT'S - seat the record's prime-minister party for them, else the role gate refuses every one (the sweep seats the largest party, which may be in opposition).
            string seatedBefore = player.PlayerPartyAbbrev;
            PolicyImpactLedger stagingLedger = sim.ImpactLedger;   // K51 (the merged film real911): the staged bills are the re-seated government's, not the player's - the impact ledger does not read them, or each kind forks a world whose campaign replays under the borrowed party and fails its draw check
            sim.ImpactLedger = null;
            if (player.Government != null && player.Government.PmParty != null && player.PlayerPartyAbbrev != player.Government.PmParty) { Debug.Log($"SHOT: bills staged - re-seating {player.Government.PmParty}, the prime minister's party, in place of {player.PlayerPartyAbbrev} so the bills are the government's (§630)"); player.PlayerPartyAbbrev = player.Government.PmParty; }
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
            if (player.PlayerPartyAbbrev != seatedBefore) { Debug.Log($"SHOT: bills staged - {seatedBefore} re-seated; the pending bills stay before the chamber (their countdown is the chamber's, §630)"); player.PlayerPartyAbbrev = seatedBefore; }
            sim.ImpactLedger = stagingLedger;   // K51: the ledger watches the player's own verbs again

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
        /// <summary>§564: the top of the FIRST ledger row whose geometry key ends with <paramref name="keySuffix"/> (scroll-content coordinates, as LedgerRow records them on Repaint), or -1 when no such row was drawn.</summary>
        private static float RowTop(string keySuffix)
        {
            float y = -1f;
            foreach (KeyValuePair<string, (Rect Name, Rect Track, Rect Figure, Rect Trailing)> g in LedgerRow.GeometryByRow) { if (g.Key.EndsWith(keySuffix) && (y < 0f || g.Value.Name.y < y)) { y = g.Value.Name.y; } }
            return y;
        }

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
            // §564 (2026-09-22): to the first partner's own override row (the two preambles are gone, so a fixed offset would overshoot) - the header, the arrows and
            // the sentence-and-action row stand above it within the margin.
            yield return Settle();
            float overrideY = RowTop(" / Override rate");
            if (overrideY < 0f) { Debug.LogError("SHOT: 06m_policylaws_trade_draft_moved - the first override row's geometry was not recorded; the frame is filmed at the page's head."); _failed++; }
            ScrollBy(controller, Mathf.Max(0f, overrideY - UiScreen.Height * 0.36f));
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
        /// <summary>
        /// §694 (ruled): THE DESK'S EFFECTS CARD BY ROLE, filmed where the ruling bites - the player in opposition with the government's budget before the
        /// chamber (93a), then with the player's alternative tabled against it (93b). Staged through the public simulation API as play stages them (the AI
        /// government's bill path, `TableGovernmentBudget`, §632; the opposition's motion, `TableShadowBudget`), after every other frame, so no frame before
        /// them moves. A player who governs at this point has no government's budget to be shown: the shots are skipped and said.
        /// </summary>
        private IEnumerator CaptureDeskByRole(GameController controller)
        {
            var sim = controller.GetType().GetField("_simulationManager", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(controller) as PoliSim.Simulation.SimulationManager;
            var world = controller.GetType().GetField("_world", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(controller) as World;
            MethodInfo draft = controller.GetType().GetMethod("BuildBudgetBillFromDrafts", BindingFlags.Instance | BindingFlags.NonPublic);
            Country player = world?.GetCountry(_countryId);
            if (sim == null || player == null || draft == null)
            {
                Debug.LogError("SHOT: desk-by-role reflection failed - the 93 captures are MISSING, not clean.");
                yield break;
            }
            if (UsBudgetProcedure.Applies(player))
            {
                yield return CaptureDeskUsBudget(controller, sim, player);   // PS-6 US-22: the president's budget before Congress, its acts decided, and a shutdown's day
                yield break;
            }
            if (sim.PlayerGoverns(player))
            {
                Debug.Log($"SHOT: 93a/93b are the opposition's Desk - {player.PlayerPartyAbbrev} governs {player.Name} at this frame; skipped, not missing.");
                yield break;
            }
            sim.TableGovernmentBudget(player);
            BudgetBill pending = sim.GetPendingBudgetBill(player.Id);
            if (pending == null || !pending.GovernmentBill)
            {
                Debug.LogError("SHOT: 93a - the government's budget could not be tabled (a bill of the player's already pending?) - the 93 captures are MISSING, not clean.");
                yield break;
            }
            SetPrivateField(controller, "_onDesk", true);
            yield return Settle();
            ScrollDeskTo(controller, "effects");   // §726: the effects card is the v3.5 desk's third row, below the fold
            yield return Settle();
            Debug.Log($"SHOT: 93a_desk_government_budget - {player.PlayerPartyAbbrev} in opposition, the government's budget before the chamber, decided in {pending.DaysRemaining} day(s)");
            yield return Capture("93a_desk_government_budget");
            if (!sim.TableShadowBudget(player.Id, draft.Invoke(controller, null) as BudgetBill, out string refused))
            {
                Debug.Log($"SHOT: 93b_desk_alternative_tabled - the alternative was refused ({refused}); skipped, not missing.");
                ScrollDeskTo(controller, null);
                yield break;
            }
            yield return Settle();
            Debug.Log($"SHOT: 93b_desk_alternative_tabled - {player.PlayerPartyAbbrev}'s alternative tabled against the government's budget");
            yield return Capture("93b_desk_alternative_tabled");
            ScrollDeskTo(controller, null);
        }

        /// <summary>
        /// K197: THE PLAYER'S POLISH DESK VERBS, filmed on Poland's main sweep after every other frame. STAGED (the review's H2: Poland's default start is
        /// Morawiecki's PiS cabinet alone, with no partner to stage), said in the log: the chamber planted as `PolishDeskVerbsDiagnostic` plants it (PiS 200,
        /// KO 165, TD 65, NL 10, Konf 20 - KO+TD+NL holding 240, KO+TD 230, no opposition candidate able to reach 231) and the KO+TD+NL cabinet named through
        /// the record's own method (`GovernmentRecord.OfNamedCabinet`); the player NL, the smallest partner, for pl1 the Parliament page's rows with LEAVE THE
        /// GOVERNMENT and - the verb taken through the manager (`SimulationManager.LeaveGovernment`) - pl2 the Docket with the leaving and the standing KO+TD
        /// kept; then the player PiS, in opposition, for pl3 the Parliament page's rows with MOVE ART. 158 and - the motion moved through the manager - pl4 the
        /// Docket with the vote and the strip's place 4 on its day. The clock is held for the frames. The four frames are expected: a refusal fails the film.
        /// The review's L3: the impact ledger does not watch the staged verbs (K51's precedent - the parties are borrowed), and the government, the seats, the
        /// division log, the player's party and the presidency flag are put back after, so nothing staged reaches a later frame or a forked world.
        /// </summary>
        private IEnumerator CapturePolishDeskVerbs(GameController controller)
        {
            Expect("pl1_politics_leave", "pl2_docket_left", "pl3_politics_art158", "pl4_docket_art158");
            var sim = controller.GetType().GetField("_simulationManager", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(controller) as SimulationManager;
            Country player = sim?.World?.GetCountry(CountryId.Poland);
            GovernmentRecord government0 = player?.Government;
            if (sim == null || government0 == null || player.ParliamentSeats == null)
            {
                Debug.LogError("SHOT: K197 - the manager, Poland's government or its seats could not be reached; the pl1-pl4 frames are MISSING, not clean.");
                _failed++;
                yield break;
            }
            string own = player.PlayerPartyAbbrev;
            bool president0 = player.PlayerHoldsPresidency;
            Dictionary<string, int> seats0 = player.ParliamentSeats;
            var divisions0 = new List<DivisionRecord>(player.Divisions.Entries);
            PolicyImpactLedger ledger0 = sim.ImpactLedger;
            void Fail(string what) { Debug.LogError("SHOT: K197 - " + what + "; the remaining pl frames are MISSING, not clean."); _failed++; }
            try
            {
                var staged = new Dictionary<string, int>(seats0);
                foreach (string k in new List<string>(staged.Keys)) { staged[k] = 0; }
                staged["PiS"] = 200; staged["KO"] = 165; staged["TD"] = 65; staged["NL"] = 10; staged["Konf"] = 20;
                player.ParliamentSeats = staged;
                GovernmentRecord g = GovernmentRecord.OfNamedCabinet("KO", new[] { "KO", "TD", "NL" }, new string[0], sim.CurrentDate.AddDays(-1), "staged (UiScreenshotDriver): the KO+TD+NL cabinet for the Polish desk verbs' frames");
                g.Kind = government0.Kind;
                g.Executive = government0.Executive;
                g.Outcome = CoalitionOutcomeKind.MajorityCoalition.ToString();
                g.AllocatePortfolios(player);
                player.Government = g;
                player.PlayerHoldsPresidency = false;
                player.PlayerPartyAbbrev = "NL";
                sim.ImpactLedger = null;
                IEnumerator Film(string stem, string tab)
                {
                    SetPrivateField(controller, "_daySpeedTimer", -ClockHoldSeconds);
                    SetPrivateField(controller, "_onDesk", false);
                    SetEnumField(controller, "_consolidatedTab", tab);
                    if (tab == "Politics") { SetEnumField(controller, "_politicsCategory", "Parliament"); }
                    ResetScrolls(controller);
                    if (tab == "Politics") { ScrollBy(controller, 900f); }
                    yield return Settle();
                    Claim("imgui");
                    yield return Capture(stem);
                }
                bool mayLeave = sim.CanLeaveGovernment(CountryId.Poland, out string leaveWhy);
                Debug.Log($"SHOT: pl1_politics_leave - STAGED: the KO+TD+NL cabinet on a planted Sejm (PiS 200, KO 165, TD 65, NL 10, Konf 20), the player NL, on {sim.CurrentDate:yyyy-MM-dd}: LEAVE THE GOVERNMENT {(mayLeave ? "offered" : "disabled - " + leaveWhy)}");
                if (!mayLeave) { Fail("NL may not leave (" + leaveWhy + ")"); yield break; }
                yield return Film("pl1_politics_leave", "Politics");
                if (!sim.LeaveGovernment(CountryId.Poland, out string refused)) { Fail("NL could not leave (" + refused + ")"); yield break; }
                Debug.Log($"SHOT: pl2_docket_left - NL left; {PolishDeskRecord.StandingWords(player)}; governing {string.Join("+", player.Government.Cabinet)} led by {player.Government.PmParty}");
                yield return Film("pl2_docket_left", "Decisions");
                player.PlayerPartyAbbrev = "PiS";
                bool mayMove = sim.CanMoveNoConfidence(CountryId.Poland, out string moveWhy);
                ConfidenceProcedure.MotionVote projected = sim.ProjectConstructiveVote(CountryId.Poland);
                Debug.Log($"SHOT: pl3_politics_art158 - the player PiS, in opposition: {(projected != null ? projected.For + " of " + projected.Members + " for, " + projected.Needed + " needed" : "no projection")}; MOVE ART. 158 {(mayMove ? "offered" : "disabled - " + moveWhy)}");
                if (!mayMove) { Fail("PiS may not move Art. 158 (" + moveWhy + ")"); yield break; }
                yield return Film("pl3_politics_art158", "Politics");
                if (!sim.MoveNoConfidence(CountryId.Poland, out string moveRefused, out ConfidenceProcedure.MotionVote vote)) { Fail("the motion was refused (" + moveRefused + ")"); yield break; }
                Debug.Log($"SHOT: pl4_docket_art158 - {vote.Title()}; the strip's place 4: {TopStrip.Art158Chip(player, sim.CurrentDate)?.Words ?? "NONE"}");
                yield return Film("pl4_docket_art158", "Decisions");
            }
            finally
            {
                player.Government = government0;
                player.ParliamentSeats = seats0;
                player.Divisions.Entries.Clear();
                player.Divisions.Entries.AddRange(divisions0);
                player.PlayerPartyAbbrev = own;
                player.PlayerHoldsPresidency = president0;
                sim.ImpactLedger = ledger0;
                Debug.Log("SHOT: K197 - the staged government, seats, divisions, party and presidency flag restored; the impact ledger watches the player's verbs again.");
            }
        }

        /// <summary>
        /// PS-6 US-22 (`docs/specs/USA_STAGE_PLAN.md` US-22's done-when: "the US film's budget frame (93b) ... is filmed"): THE DESK WHERE CONGRESS VOTES THE
        /// BUDGET AS ITS ACTS, the effects card by role as §694's frames film it - 93a the president's budget before Congress (the government's bill,
        /// `TableGovernmentBudget`); 93b its acts decided through the game's own day call (`SimulationManager.AdvanceBudgetBillDay`, the date held: each act a
        /// division through both chambers and the president, each logged by title), the continuing resolution before Congress where the appropriation fell;
        /// 93c a shutdown's day on the masthead, a lapse planted for the one frame and restored. The card is the opposition's: where the player's party holds the
        /// presidency at this frame (the warm-up's count seats the game's president), the other major party is staged in the player's place for the three frames
        /// and restored. US-19's 01d2-01d4 precedent: the Fed chair's selection, its turn, the speed and the clock are snapshotted and restored, play's clock is
        /// held for the staged frames (no day passes, so no signing is queued), and a hold that does not come back fails the run. The bills the frames table
        /// stay before Congress after them, as §694's do (the frames run last).
        /// </summary>
        private IEnumerator CaptureDeskUsBudget(GameController controller, SimulationManager sim, Country player)
        {
            const BindingFlags Own = BindingFlags.Instance | BindingFlags.NonPublic;
            FieldInfo candidatesField = controller.GetType().GetField("_fedChairCandidates", Own), forTurnField = controller.GetType().GetField("_fedChairCandidatesForTurn", Own);
            FieldInfo speedField = controller.GetType().GetField("_gameSpeed", Own), timerField = controller.GetType().GetField("_daySpeedTimer", Own);
            if (candidatesField == null || forTurnField == null || speedField == null || timerField == null)
            {
                Debug.LogError("SHOT: the US budget frames could not snapshot the Fed chair's selection or the clock (_fedChairCandidates/_fedChairCandidatesForTurn/_gameSpeed/_daySpeedTimer not found) - 93a-93c are MISSING, not clean.");
                _failed++;
                yield break;
            }
            if (sim.GetPendingBudgetBill(player.Id) != null)
            {
                Debug.LogError("SHOT: 93a - a budget bill is already before Congress at this frame; the US budget frames are MISSING, not clean.");
                _failed++;
                yield break;
            }
            object candidatesWas = candidatesField.GetValue(controller), forTurnWas = forTurnField.GetValue(controller), speedWas = speedField.GetValue(controller), timerWas = timerField.GetValue(controller);
            int candidatesBefore = (candidatesWas as ICollection)?.Count ?? 0;
            string mine = player.PlayerPartyAbbrev, withheldWas = player.ContinuingResolutionWithheldBy;
            DateTime lapseWas = player.LapseSince, fundedWas = player.AppropriatedThrough, dayWas = sim.CurrentDate; bool holdWas = sim.CentralBankHeadAwaitsPlayer(player.Id);   // the second review's L1: the date and the hold, asserted after
            timerField.SetValue(controller, -ClockHoldSeconds);
            try
            {
                if (sim.PlayerGoverns(player))
                {
                    string other = null;
                    foreach (KeyValuePair<string, int> seat in player.ParliamentSeats) { if (seat.Key != mine && (other == null || seat.Value > player.ParliamentSeats[other])) { other = seat.Key; } }
                    if (other == null) { Debug.LogError("SHOT: 93a - no other party seated to stage as the opposition; the US budget frames are MISSING, not clean."); _failed++; yield break; }
                    player.PlayerPartyAbbrev = other;
                    Debug.Log($"SHOT: 93a-93c staged - {mine} holds the presidency at this frame; {other}, the other major party, seated in its place for the opposition's Desk (restored after).");
                }
                sim.TableGovernmentBudget(player);
                BudgetBill pending = sim.GetPendingBudgetBill(player.Id);
                if (pending == null || !pending.GovernmentBill)
                {
                    Debug.LogError("SHOT: 93a - the president's budget could not be tabled - the US budget frames are MISSING, not clean.");
                    _failed++;
                    yield break;
                }
                SetPrivateField(controller, "_onDesk", true);
                yield return Settle();
                ScrollDeskTo(controller, "effects");
                yield return Settle();
                Debug.Log($"SHOT: 93a_desk_government_budget - {player.PlayerPartyAbbrev} out of the presidency, the president's budget before Congress ({SimulationManager.DescribeBudgetBill(pending)}), decided in {pending.DaysRemaining} day(s)");
                yield return Capture("93a_desk_government_budget");
                int seen = player.Divisions.Entries.Count > 0 ? player.Divisions.Entries[player.Divisions.Entries.Count - 1].Number : 0;
                DateTime horizonBefore = player.AppropriatedThrough;
                for (int i = 0; i <= ParliamentSystem.BillDurationDays && ReferenceEquals(sim.GetPendingBudgetBill(player.Id), pending); i++) { sim.AdvanceBudgetBillDay(player.Id); }
                foreach (DivisionRecord record in player.Divisions.Entries)
                {
                    if (record.Number > seen) { Debug.Log($"SHOT: 93b - No. {record.Number} the {Congress.ChamberName(player.Id, record.Chamber).ToLowerInvariant()} {(record.Passed ? "passed" : "failed")}{(record.Motion ? " (a motion)" : string.Empty)}: {record.Title}"); }
                }
                BudgetBill after = sim.GetPendingBudgetBill(player.Id);
                Debug.Log("SHOT: 93b_desk_us_budget_acts - the president's budget decided as its acts on " + sim.CurrentDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) + "; "
                    + (after != null && after.ContinuingResolution ? $"the appropriation fell: a continuing resolution at the standing figures before Congress, decided in {after.DaysRemaining} day(s)"
                        : after == null && player.AppropriatedThrough != horizonBefore ? $"the appropriation stood: funded through {player.AppropriatedThrough:yyyy-MM-dd}"
                        : after == null ? $"the appropriation fell: nothing before Congress - a continuing resolution is decided on {UsBudgetProcedure.HorizonEve(player.AppropriatedThrough):yyyy-MM-dd}, the day before the horizon (US-22b)"
                        : "a bill other than a continuing resolution is before Congress (A DEFECT)"));
                if (after != null && !after.ContinuingResolution) { _failed++; }
                yield return Settle();
                yield return Capture("93b_desk_us_budget_acts");
                // 93c: a lapse planted for one frame - the masthead says the shutdown's day; the horizon and the lapse are put back after
                player.LapseSince = sim.CurrentDate.AddDays(-11);
                player.AppropriatedThrough = player.LapseSince.AddDays(-1);
                ScrollDeskTo(controller, null);
                yield return Settle();
                Debug.Log($"SHOT: 93c_desk_us_shutdown - staged: funding lapsed on {player.LapseSince:yyyy-MM-dd}, the masthead on its day {UsBudgetProcedure.LapseDays(player.LapseSince, sim.CurrentDate)}; restored after");
                yield return Capture("93c_desk_us_shutdown");
            }
            finally
            {
                player.LapseSince = lapseWas;
                player.AppropriatedThrough = fundedWas;
                player.ContinuingResolutionWithheldBy = withheldWas;
                player.PlayerPartyAbbrev = mine;
                candidatesField.SetValue(controller, candidatesWas);
                forTurnField.SetValue(controller, forTurnWas);
                speedField.SetValue(controller, speedWas);
                timerField.SetValue(controller, timerWas);
                ScrollDeskTo(controller, null);
            }
            yield return Settle();
            int candidatesAfter = (candidatesField.GetValue(controller) as ICollection)?.Count ?? 0;
            object speedAfter = speedField.GetValue(controller);
            string restored = $"SHOT: 93a-93c restored - {mine} seated again, the funding horizon {player.AppropriatedThrough:yyyy-MM-dd} and the lapse {(player.LapseSince == DateTime.MinValue ? "none" : player.LapseSince.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture))} as found; the Fed chair's candidates {candidatesAfter} (before {candidatesBefore}), the speed {speedAfter} (before {speedWas})";
            if (candidatesAfter != candidatesBefore || !Equals(speedAfter, speedWas) || player.PlayerPartyAbbrev != mine || sim.CurrentDate != dayWas || sim.CentralBankHeadAwaitsPlayer(player.Id) != holdWas) { Debug.LogError(restored + $" - THE HOLD DID NOT RETURN (the day {sim.CurrentDate:yyyy-MM-dd}, before {dayWas:yyyy-MM-dd}; the hold {sim.CentralBankHeadAwaitsPlayer(player.Id)}, before {holdWas})."); _failed++; }
            else { Debug.Log(restored + "."); }
        }

        /// <summary>
        /// §726 (UI v3.5): the desk is a scrolled page - a frame that films a card below the fold scrolls the desk to that card's top (the controller
        /// records each card's top by name, <c>_deskCardTops</c>, on every pass of the page); <paramref name="card"/> null puts it back at the top. A card
        /// with no recorded top is an error: the frame would film the fold under the name of the card.
        /// </summary>
        private static void ScrollDeskTo(GameController controller, string card)
        {
            FieldInfo scroll = controller.GetType().GetField("_deskScrollPosition", BindingFlags.Instance | BindingFlags.NonPublic);
            FieldInfo tops = controller.GetType().GetField("_deskCardTops", BindingFlags.Instance | BindingFlags.NonPublic);
            if (scroll == null || tops == null)
            {
                Debug.LogError("SHOT: the desk's scroll (_deskScrollPosition/_deskCardTops) not found - a desk frame below the fold films the fold.");
                return;
            }
            if (card == null) { scroll.SetValue(controller, Vector2.zero); return; }
            if (!(tops.GetValue(controller) is Dictionary<string, float> map) || !map.TryGetValue(card, out float top))
            {
                Debug.LogError($"SHOT: the desk's '{card}' card has no recorded top - the frame would film the fold, not the card.");
                return;
            }
            scroll.SetValue(controller, new Vector2(0f, top));
        }

        /// <summary>
        /// §732 (UI v3.5): People is a scrolled page of sections - a frame below the fold scrolls the page to its section's head, a little above it so
        /// the head is on film (the controller records each section's top by name, <c>_peopleSectionTops</c>); <paramref name="section"/> null puts it
        /// back at the top. A section with no recorded top is an error, as on the desk.
        /// </summary>
        private void ScrollPeopleTo(GameController controller, string section)
        {
            FieldInfo scroll = controller.GetType().GetField("_demographicsScrollPosition", BindingFlags.Instance | BindingFlags.NonPublic);
            FieldInfo tops = controller.GetType().GetField("_peopleSectionTops", BindingFlags.Instance | BindingFlags.NonPublic);
            if (scroll == null || tops == null)
            {
                Debug.LogError("SHOT: People's scroll (_demographicsScrollPosition/_peopleSectionTops) not found - a People frame below the fold films the fold.");
                _failed++;
                return;
            }
            if (section == null) { scroll.SetValue(controller, Vector2.zero); return; }
            if (!(tops.GetValue(controller) is Dictionary<string, float> map) || !map.TryGetValue(section, out float top))
            {
                Debug.LogError($"SHOT: People's '{section}' section has no recorded top - the frame would film the fold, not the section.");
                _failed++;
                return;
            }
            scroll.SetValue(controller, new Vector2(0f, Mathf.Max(0f, top - V35.Px(8f))));
        }

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
            // ⚠ A DRY FILM CANNOT REPRESENT A CANVAS SCREEN, and P6-A1 (2026-09-17) is where that showed.
            // The dry film's size seam is `UiScreen`, which the IMGUI styles read; a Canvas is scaled by
            // the REAL backbuffer, and a windowless Editor reports 640x480 whatever geometry the run names.
            // So a Canvas screen in a dry pass is laid out at a frame the run does not claim, and asserting
            // on it reports the wrong frame's clipping - 124 of them the first time the Canvas type carried
            // a device-pixel floor, against none in the film of the same tree at the same geometry. The
            // assert stays the film's, beside the edge guard and the frame-size traps.
            if (Dry)
            {
                return;
            }

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

        /// <summary>TL-1 (§645): the envelope is TIMED, so the wait is bounded in real seconds too - it gives up only when both bounds have passed. A
        /// frame count alone lost the race in a fast batch run (frames uncapped): the bound ran out before the cover had come in.</summary>
        public const float DefaultCanvasSettleSeconds = 10f;
        public float CanvasSettleSeconds = DefaultCanvasSettleSeconds;
        /// <summary>The frame bound, a field so the race flag (-shotcanvasrace) can shorten it.</summary>
        public int CanvasSettleFrames = MaxCanvasSettleFrames;

        // TL-1 (§645): THE FRAMES A SWEEP DECLARES IT WILL CAPTURE - a guarded block names its frames before it runs, and EndSweep fails the film on
        // any that was never captured. A skipped frame is a failure, never a silence.
        private readonly HashSet<string> _expectedFrames = new HashSet<string>();
        private readonly HashSet<string> _capturedFrames = new HashSet<string>();
        private void Expect(params string[] frames) { foreach (string f in frames) { _expectedFrames.Add(f); } }

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
            float began = Time.realtimeSinceStartup;
            for (int i = 0; i < CanvasSettleFrames || Time.realtimeSinceStartup - began < CanvasSettleSeconds; i++)
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

            // TL-1 (§645): an ERROR, counted - a wait that gave up is a film that may capture the wrong surface or skip a frame.
            Debug.LogError($"SHOT: canvas seam never settled to active={wantActive} within {CanvasSettleFrames} frames and {CanvasSettleSeconds:0.#} s - capturing whatever is up, and the film fails.");

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

        /// <summary>The sweep's own verdict over what was filmed, then the exit. One site, so a sweep that
        /// stops early (<see cref="StopAfter"/>) is judged by the same fold as one that runs out.</summary>
        private void EndSweep()
        {
            ReportIdentity();
            // TL-1 (§645): every frame the sweep declared and never captured fails the film.
            int missing = 0;
            foreach (string frame in _expectedFrames)
            {
                if (_capturedFrames.Contains(frame)) { continue; }
                missing++;
                _failed++;
                Debug.LogError($"SHOT: EXPECTED FRAME MISSING - '{frame}' was declared and never captured (TL-1): a skipped frame fails the film.");
            }
            Debug.Log($"SHOT: {_expectedFrames.Count} declared frame(s), {missing} missing.");
            Debug.Log($"SHOT: done, {_captured} captured, {_failed} failed, {_realCrisisPlateFrames} frame(s) under a real crisis plate.");   // the review 2, N7: the plate frames reach the run's summary

            int overflows = ReportOverflows();
            Debug.Log($"SHOT: {overflows} text overflow(s) recorded.");

            int escapes = ReportContainmentEscapes();
            Debug.Log($"SHOT: {escapes} containment escape(s) recorded.");

            // ⚠ In a dry pass this line would otherwise read "0 violations across 0 asserts", which is the
            // guard's own shape for "verified nothing" and would be mistaken for a clean Canvas (P6-A1).
            Debug.Log(Dry
                ? "SHOT: DRY - canvas text NOT CLAIMED: a Canvas is scaled by the real backbuffer, which is 640x480 with no window, so this run asserted nothing about it."
                : $"SHOT: {_canvasTextViolations} canvas text violation(s) recorded across {_canvasTextAsserts} assert(s).");

            // Ruling 1's fold: a red line nothing counted is still a failure. The four counters
            // above are the driver's own asserts; _loggedErrors catches everything else - the ~18
            // uncounted LogError sites and any ATTRIB the simulation raised while this drove it.
            bool clean = _failed == 0 && overflows == 0 && escapes == 0 && _canvasTextViolations == 0;
            LastSweepSummary = $"{_captured} measured, {_failed} failed, {overflows} overflow(s), {escapes} escape(s), {_canvasTextViolations} canvas text, {_loggedErrors} error(s) logged, {_realCrisisPlateFrames} crisis-plate frame(s), ";
            if (_loggedErrors > 0)
            {
                Debug.Log(clean
                    ? $"SHOT: FOLD - the run's own counters were clean but {_loggedErrors} error(s) were logged during it; exiting nonzero. First: \"{(_firstLoggedError != null && _firstLoggedError.Length > 200 ? _firstLoggedError.Substring(0, 200) + "…" : _firstLoggedError)}\"."
                    : $"SHOT: {_loggedErrors} error(s) logged during the run; the failure is already reflected.");
            }

            Finish(clean && _loggedErrors == 0 ? 0 : 1);
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
        private bool AssertCaptureIdentity(string name, Texture2D shot, string claimed)
        {
            if (!CaptureIdentity.Armed || string.IsNullOrEmpty(claimed)) { return true; }

            if (!CaptureIdentity.TryColorFor(claimed, out Color32 want))
            {
                Debug.LogError($"SHOT: IDENTITY - '{claimed}' is not a known surface, so {name} claims a "
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

            Debug.LogError($"SHOT: IDENTITY MISMATCH on {name} - it claims '{claimed}' and the written "
                           + $"frame carries the token of '{blamed}' (rgb {found.r},{found.g},{found.b}). This is S-20's "
                           + "defect: the capture wrote, the guards were silent, and the screen under test is not the "
                           + "screen in the file. Failing loudly rather than filing a picture of something else.");
            return false;
        }

        private int _identityAsserts;

        /// <summary>P2-1.3: the ledger rows drawn in each CAPTURED frame - cleared as a capture begins, collected after its
        /// frame is written, so a row drawn while the driver scrolls or settles toward the next screen is never filed
        /// under the previous stem (the first film keyed a Policy Laws dial under a Budget stem exactly that way).</summary>
        private readonly Dictionary<string, float> _reachByCapture = new Dictionary<string, float>();

        /// <summary>
        /// P4-2 (2026-09-03): `-cuesweep` - every cue in the catalog fired by NAME through the director, in order, and
        /// the clip each resolves to asserted: a cue with a file must load it, a cue without one must say so in the
        /// catalog. Batch mode has no audio device, so nothing is heard here; what is proved is that every wired cue
        /// resolves to a verified file. The Editor's cue board (PoliSim ▸ Audio ▸ Cue Board) is the same trigger with
        /// sound, for Elias's own test in Play mode.
        /// </summary>
        private void RunCueSweep()
        {
            int fired = 0, loaded = 0, silent = 0;
            foreach (AudioCueEntry e in AudioDirector.Catalog)
            {
                bool ok = AudioDirector.Fire(e.Cue.ToString());
                if (!ok) { Debug.LogError($"SHOT: -cuesweep - '{e.Cue}' is not a cue the director knows by name."); _failed++; continue; }
                fired++;
                AudioClip clip = AudioDirector.Clip(e.Cue);
                if (string.IsNullOrEmpty(e.File))
                {
                    silent++;
                    Debug.Log($"SHOT: -cuesweep - {e.Cue}: no file in the catalog ({e.Note}); fired silent, as recorded.");
                }
                else if (clip == null)
                {
                    Debug.LogError($"SHOT: -cuesweep - {e.Cue} names '{e.File}' and the clip did not load.");
                    _failed++;
                }
                else
                {
                    loaded++;
                    Debug.Log($"SHOT: -cuesweep - {e.Cue}: '{e.File}' {clip.channels} ch, {clip.frequency} Hz, {clip.length:0.000} s{(e.Provisional ? " (stand-in)" : "")}.");
                }
            }
            Debug.Log($"SHOT: -cuesweep - {fired} cue(s) fired by name, {loaded} resolved to a loaded clip, {silent} without a file (recorded), {AudioDirector.Fired.Count} in the director's log.");
        }

        /// <summary>
        /// P4-1 (2026-09-03): STABLE CONTROL LAYOUT, asserted on film. Every ledger row recorded at rest (the capture
        /// before the drag) is looked up again mid-drag (after the drag capture) and its four column rects must be
        /// identical to the pixel: the track a pointer is moving must not move. A row missing from either side is
        /// reported, never silently skipped; a run that compares no rows fails, because it verified nothing.
        /// </summary>
        private void AssertLedgerGeometryStable(Dictionary<string, (Rect Name, Rect Track, Rect Figure, Rect Trailing)> rest, string stem)
        {
            int compared = 0, moved = 0;
            var detail = new System.Text.StringBuilder();
            foreach (KeyValuePair<string, (Rect Name, Rect Track, Rect Figure, Rect Trailing)> row in rest)
            {
                if (!LedgerRow.GeometryByRow.TryGetValue(row.Key, out (Rect Name, Rect Track, Rect Figure, Rect Trailing) now)) { continue; }
                compared++;
                bool same = row.Value.Name == now.Name && row.Value.Track == now.Track && row.Value.Figure == now.Figure && row.Value.Trailing == now.Trailing;
                if (!same)
                {
                    moved++;
                    detail.AppendLine($"    {row.Key}: track {row.Value.Track} -> {now.Track}; figure {row.Value.Figure} -> {now.Figure}; name {row.Value.Name} -> {now.Name}; trailing {row.Value.Trailing} -> {now.Trailing}");
                }
            }
            if (compared == 0)
            {
                Debug.LogError($"SHOT: P4-1 [{stem}] - no ledger row was recorded on both sides of the drag; the geometry guard verified NOTHING.");
                _failed++;
            }
            else if (moved > 0)
            {
                Debug.LogError($"SHOT: P4-1 [{stem}] - {moved} of {compared} ledger row(s) MOVED between rest and mid-drag (stable control layout broken):\n{detail}");
                _failed++;
            }
            else
            {
                Debug.Log($"SHOT: P4-1 [{stem}] - ledger geometry stable: {compared} row(s) compared at rest and mid-drag, every column rect identical.");
            }
        }

        /// <summary>
        /// P2-4.3 (2026-09-02): a staged division carries what a real one carries - its sides from
        /// ParliamentSystem.RecordDivision (the same enumeration the verdict reads) and an estimate: the standing
        /// preview's arrows, mapped as RecomputePolicyPreview maps them, because the harness has no draft of its
        /// own and says so here rather than leaving the ceremony's panels to state absence on every film.
        /// </summary>
        private static void StageDivisionWithContent(Country player, SimulationManager sim, string title, float direction, bool passed)
        {
            DivisionRecord record;
            if (Congress.Applies(player))
            {
                // PS-6 US-18: where a bill goes through both chambers, the staged division is what play writes - the player's own bill (its author, as every
                // player bill carries since §654) voted through the game's own gate (ParliamentSystem.Divide): both chambers' divisions where the first passes
                // it, the second carrying the first, its ceremony the second's. The harness's ask picks the direction as below; where the chambers decide the
                // same either way, theirs stands and the log says so.
                BillConcern Staged(float d) { BillConcern c = BillConcern.FromLegacy(d, BillAxis.Fiscal); c.Author = player.PlayerPartyAbbrev; return c; }
                bool both = ParliamentSystem.WouldBillPass(player, Staged(direction), sim.CurrentDate);
                if (both != passed)
                {
                    if (ParliamentSystem.WouldBillPass(player, Staged(-direction), sim.CurrentDate) == passed) { direction = -direction; Debug.Log($"SHOT: staged division '{title}' takes direction {direction:+0;-0} so Congress's own verdict is {(passed ? "CARRIED" : "LOST")}, as the capture asks."); }
                    else { Debug.Log($"SHOT: staged division '{title}': Congress {(both ? "carries" : "loses")} it in either direction; the records keep Congress's verdict, not the harness's ask."); }
                }
                record = ParliamentSystem.Divide(player, title, Staged(direction), sim.CurrentDate, out bool carried);
                Debug.Log($"SHOT: staged '{title}' through both chambers - {(record.OtherChamber != null ? "No. " + record.OtherChamber.Number + " then No. " + record.Number : "No. " + record.Number + " alone")}: {Congress.StopWords(player.Id, Congress.Project(player, Staged(direction), sim.CurrentDate))} ({(carried ? "carried" : "lost")})");
            }
            else
            {
                // P3 close (2026-09-03): a record never contradicts its own sides. The verdict is the chamber's (§257, the count
                // decides); when the harness asks for the other outcome the direction is flipped and re-judged, and when the
                // chamber decides the same either way the record keeps the chamber's verdict and the log says so.
                bool verdict = ParliamentSystem.WouldBillPass(player, direction);
                if (verdict != passed)
                {
                    if (ParliamentSystem.WouldBillPass(player, -direction) == passed) { direction = -direction; verdict = passed; Debug.Log($"SHOT: staged division '{title}' takes direction {direction:+0;-0} so the chamber's own verdict is {(passed ? "CARRIED" : "LOST")}, as the capture asks."); }
                    else { Debug.Log($"SHOT: staged division '{title}': the chamber {(verdict ? "carries" : "loses")} it in either direction; the record keeps the chamber's verdict, not the harness's ask."); }
                }
                ParliamentSystem.RecordDivision(player, title, direction, verdict, sim.CurrentDate);
                record = player.Divisions.Entries[player.Divisions.Entries.Count - 1];
            }
            PolicyPreview preview = sim.PreviewTurn(player.Id, PolicyDecision.None());
            void Add(string name, float value, bool higherIsBetter, string unit)
            {
                if (Mathf.Abs(value) < 0.005f) { return; }
                record.Effects.Add(new DivisionEffect { Name = name, Value = value, HigherIsBetter = higherIsBetter, Figure = value.ToString("+0.##;-0.##", CultureInfo.CurrentCulture) + unit });
            }
            Add("GDP growth", preview.GdpGrowthPercent, true, "%");
            Add("Unemployment", preview.UnemploymentChange, false, " pts");
            Add("Inflation", preview.InflationChange, false, " pts");
            Add("Approval", preview.ApprovalChange, true, " pts");
            Add("Poverty rate", preview.PovertyRateChange, false, " pts");
            Add("Labor force participation", preview.LaborForceParticipationRateChange, true, " pts");
            Add("Crime index", preview.CrimeIndexChange, false, " pts");
            Debug.Log($"SHOT: staged division '{title}' with {record.Sides.Count} side(s) and {record.Effects.Count} effect(s) (the standing preview's, the harness holds no draft).");
        }

        /// <summary>
        /// P2-4.3: the Docket at the seed has no minister and no pending decision, so the alerts region and the
        /// option arrows would never be on film. This seats the first candidate in Interior & Justice and stages one
        /// synthetic decision with two options whose figures are authored here for the film - the harness says so in
        /// its log line; nothing here reaches a real game.
        /// </summary>
        private static void StageCabinetDecisionForTheDocket(Country player, SimulationManager sim)
        {
            if (player == null || sim == null) { return; }
            if (!player.CabinetMinisters.ContainsKey(CabinetPortfolio.InteriorJustice))
            {
                player.CabinetMinisters[CabinetPortfolio.InteriorJustice] = CabinetSystem.GenerateCandidates(CabinetPortfolio.InteriorJustice)[0];
            }
            var decision = new CabinetDecision
            {
                Name = "Harness: a staged decision for the Docket film",
                Description = "Staged by the screenshot harness so the minister alerts and an option's cost and impact are on film; the figures are the harness's own.",
            };
            // PF-14 (§591): a cost is NEGATIVE - the model adds the figure to the budget; until §591 the pilot was staged at +4 and filmed as a cost, one inversion hiding the other. A revenue option beside it puts both inks on film.
            decision.Options.Add(new CabinetDecisionOption("Fund the visible-policing pilot", crimeIndexShock: -1.5f, budgetImpact: -4f, approvalEffect: 1f));
            decision.Options.Add(new CabinetDecisionOption("Pay for it by selling the old patrol fleet", crimeIndexShock: -1f, budgetImpact: 2f, approvalEffect: -0.5f));
            decision.Options.Add(new CabinetDecisionOption("Decline the pilot", approvalEffect: -0.5f));
            sim.StagePendingCabinetDecision(player.Id, CabinetPortfolio.InteriorJustice, decision);
            Debug.Log($"SHOT: staged a cabinet decision on {CabinetPortfolio.InteriorJustice} with {player.CabinetMinisters[CabinetPortfolio.InteriorJustice].Name} seated (the harness's own figures, for the Docket film).");
        }

        private IEnumerator Capture(string name)
        {
            _capturedFrames.Add(name);   // TL-1 (§645): attempted - a capture that fails is counted by its own trap, not as missing
            // §705: THE TOUR NEVER FILMS A PAGE UNDER A SHEET THE GAME OPENED BY ITSELF. Where the player's party is asked to form a government,
            // the controller opens the formation sheet over every page (premise 1) - the German session's round opens at the tour's first day tick
            // (the warm-up drives the manager, not the controller's day), the SPD asked, and every frame after 01f was the sheet, five plate traps
            // firing. A player closes it (CLOSE THE SHEET - the clock still waits on the answer); so does the tour, and 07a stages the sheet itself.
            if (_tourController != null && !_stagingFormationSheet)
            {
                FieldInfo sheetField = typeof(GameController).GetField("_formationSheetOpen", BindingFlags.Instance | BindingFlags.NonPublic);
                if (sheetField?.GetValue(_tourController) is bool sheetUp && sheetUp)
                {
                    sheetField.SetValue(_tourController, false);
                    Debug.Log($"SHOT: {name} - the formation sheet the game opened by itself (the player's party asked) is closed as a player would, CLOSE THE SHEET; the clock still waits on the answer (07a films the sheet).");
                }
            }
            // The review of wave/dcrisis, L6: a frame filmed under a REAL debt crisis's plate (the world's own trajectory read CCC on the film's day) is said and
            // counted - the plate lies over the page the name promises. The staged crisis (CaptureDeskCrisis) films its plate on purpose and is not counted.
            if (!_stagingCrisis)
            {
                GameController plateHost = _tourController != null ? _tourController : UnityEngine.Object.FindFirstObjectByType<GameController>();
                MethodInfo plateHolds = plateHost != null ? typeof(GameController).GetMethod("CrisisPlateHolds", BindingFlags.Instance | BindingFlags.NonPublic) : null;
                if (plateHolds != null && plateHolds.Invoke(plateHost, null) is bool plateUp && plateUp)
                {
                    _realCrisisPlateFrames++;
                    Debug.LogWarning($"SHOT: CRISIS PLATE ON FILM - {name} is filmed under a real debt crisis's plate (market access lost on the film's day); {_realCrisisPlateFrames} frame(s) so far.");
                }
            }
            // The review 2, N2: every Desk frame says its calendar card's margin - the need, the room and the outcome the card chose (CalendarFit)
            GameController fitHost = _tourController != null ? _tourController : UnityEngine.Object.FindFirstObjectByType<GameController>();
            if (fitHost != null && fitHost.OnDesk && !string.IsNullOrEmpty(fitHost.LastCalendarFitLine)) { Debug.Log($"SHOT: {name} - {fitHost.LastCalendarFitLine}"); }
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
            PoliSim.UI.LedgerRow.ReachByRow.Clear();

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
#if UNITY_EDITOR
            DryGuiPass.BeginCapture(name);
            // PF-1: every chamber answer the captured frame reuses is recomputed uncached and compared - a drift fails the film.
            ChamberVerdicts.VerifyHits = true;
            double verifiedBefore = ChamberVerdicts.VerifyMilliseconds;
#endif
            if (Dry)
            {
#if UNITY_EDITOR
                // The dry film's frame: the IMGUI pass a Game View would have delivered before WaitForEndOfFrame resumes.
                if (!DryGuiPass.Pump(_dryController, _dryOnGui)) { DryStop(); yield break; }
                Debug.Log($"SHOT: dry pass for {name} - the IMGUI pass took {DryGuiPass.LastPassMilliseconds} ms, its Layout event {DryGuiPass.LastLayoutMilliseconds} ms, the chamber cache's drift verification {ChamberVerdicts.VerifyMilliseconds - verifiedBefore:F0} ms of it.");
#endif
            }
            else
            {
                Debug.Log($"SHOT: entering WaitForEndOfFrame for {name}.");
                yield return new WaitForEndOfFrame();
                Debug.Log($"SHOT: WaitForEndOfFrame resumed for {name}.");
            }
#if UNITY_EDITOR
            DryGuiPass.EndCapture();
            ChamberVerdicts.VerifyHits = false;
#endif
            if (name.Contains("parliament"))
            {
                // P2-3.1 (2026-09-02): the hemicycle's dot count against the chamber, read from the frame just
                // captured - the row's own "done when".
                int dots = PoliSim.UI.HemicycleRenderer.LastDotsDrawn;
                int chamber = PoliSim.UI.HemicycleRenderer.LastChamberSeats;
                int declared = PoliSim.UI.HemicycleRenderer.LastDeclaredSeats;
                Debug.Log($"SHOT: the chamber drew {dots} seat(s) on {PoliSim.UI.HemicycleRenderer.LastRows} row(s) - the arc's rings, or the v3.5 seat bar's one (§743) - for a chamber of {chamber} seat(s) (declared {declared}).");
                if (dots != chamber || chamber != declared)
                {
                    _failed++;
                    Debug.LogError($"SHOT: the chamber's seat count {dots} != chamber {chamber} (declared {declared}) in {name}.");
                }
            }
            foreach (KeyValuePair<string, float> reach in PoliSim.UI.LedgerRow.ReachByRow)
            {
                if (!_reachByCapture.TryGetValue(reach.Key, out float worstSoFar) || reach.Value > worstSoFar) { _reachByCapture[reach.Key] = reach.Value; }
            }

            if (Dry)
            {
                // Measured, not photographed: the guards recorded during the pass, the reach is merged above, and the
                // pixel traps (identity token, frame size) have no pixels to read.
                _captured++;
                CaptureIdentity.Expected = "imgui";
                if (!string.IsNullOrEmpty(StopAfter) && name == StopAfter)
                {
                    Debug.Log($"SHOT: -shotstop={StopAfter} - the sweep ends at this capture by request: a PARTIAL set of {_captured}, not a sweep.");
                    EndSweep();
                }

                yield break;
            }

            // ⚠ PF-13 (2026-09-22, §578): THE CLAIM IS CONSUMED HERE, BEFORE ANYTHING CAN FAIL. S-20 reset it after a shot was written and on the identity
            // failure itself, which left THREE exits that fail before writing - a null capture, a HEIGHT MISMATCH and a WIDTH MISMATCH - carrying the claim
            // forward to the next capture that DID write. Measured on the film of 2026-09-22: 127 captures all reached the assert (126 proved, 1 mismatched) while
            // only 88 wrote, so 38 failed on geometry after the assert and one of them handed its 'signing' claim to 92_saves_menu - the guard then named an
            // innocent capture. A claim read once and cleared cannot be inherited, whatever happens next.
            string claimed = CaptureIdentity.Expected;
            CaptureIdentity.Expected = "imgui";

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
            if (!AssertCaptureIdentity(name, shot, claimed))
            {
                _failed++;
                UnityEngine.Object.Destroy(shot);
                // The claim was already consumed above, so no later shot can inherit it - the cascade this reset once prevented (a
                // mismatched capture making every later shot fail with the same claim) is now impossible on EVERY exit, not two of them.
                yield break;
            }

            // ⚠ Height is checked FIRST and separately, so a run wrong on both axes says so on both
            // rather than reporting the width and leaving the reader to assume the height was fine.
            if (ExpectedHeight > 0 && shot.height != ExpectedHeight)
            {
                Debug.LogError($"SHOT: HEIGHT MISMATCH on {name} - expected a {ExpectedHeight}px frame, captured "
                               + $"{shot.height}. ⚠ TWO CAUSES, AND THEY NEED DIFFERENT FIXES: either -shotheight= was "
                               + "missing or ignored and this film is not evidence for the geometry it is named after, "
                               + "OR UiScreenshotCapture.GameViewChromeHeight has gone stale because Unity changed the "
                               + "Game View toolbar. ⚠ If it is the second, RE-MEASURE AND CHANGE THE CONSTANT - do not "
                               + "widen this guard, which would be moving a bar to pass. This fires on the FIRST capture "
                               + "of a run precisely so a stale number cannot film a whole set at the wrong size. "
                               + "Fold position is a function of height, and this project has already recorded a finding "
                               + "that turned on where the fold fell: a silently-short capture does not merely mislabel a "
                               + "film, it moves the thing the film was taken to look at.");
                _failed++;
                Destroy(shot);
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

            // S-20, as PF-13 (§578) re-cut it: the claim is consumed where the capture BEGINS, not here - a caller that means a board
            // says so once, each time, and no exit of this method can carry that claim into the next capture.

            if (!string.IsNullOrEmpty(StopAfter) && name == StopAfter)
            {
                Debug.Log($"SHOT: -shotstop={StopAfter} - the sweep ends at this capture by request: a PARTIAL set of {_captured}, not a sweep.");
                EndSweep();
                yield break;
            }
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
        /// <summary>
        /// The docket with the budget process open - the interrupt state (P6-A3, 2026-09-17).
        ///
        /// <para>The card the docket draws for it is `BeginAreaCard("BUDGET PROCESS", …, blocksTime: true)`,
        /// whose header carries the `HOLDS TIME` stamp. ⚠ **The state is staged, not driven**: the flag is a
        /// membership in `SimulationManager`'s pending set and nothing else, and driving a year of days to
        /// reach a fiscal-year start would move every figure the rest of the sweep films. It is taken back
        /// before the method returns, in a `finally`-shaped path, so a failure cannot leave the clock held
        /// for the captures that follow.</para>
        /// </summary>
        private IEnumerator CaptureBudgetProcessInterrupt(GameController controller)
        {
            FieldInfo simField = controller.GetType().GetField("_simulationManager", BindingFlags.Instance | BindingFlags.NonPublic);
            if (!(simField?.GetValue(controller) is SimulationManager sim))
            {
                Debug.LogError("SHOT: P6-A3 - no SimulationManager on the controller; the interrupt state is NOT filmed.");
                _failed++;
                yield break;
            }

            FieldInfo pendingField = typeof(SimulationManager).GetField("_pendingBudgetProcessByCountry", BindingFlags.Instance | BindingFlags.NonPublic);
            if (!(pendingField?.GetValue(sim) is HashSet<CountryId> pending))
            {
                Debug.LogError("SHOT: P6-A3 - the pending budget-process set could not be reached; the interrupt state is NOT filmed.");
                _failed++;
                yield break;
            }

            bool held = pending.Contains(_countryId);
            if (!held) { pending.Add(_countryId); }
            if (!sim.GetPendingBudgetProcess(_countryId))
            {
                Debug.LogError("SHOT: P6-A3 - the staged flag did not take; the interrupt state is NOT filmed.");
                _failed++;
                if (!held) { pending.Remove(_countryId); }
                yield break;
            }

            yield return Settle();
            yield return Capture("03b_decisions_budget_process");
            SetPrivateField(controller, "_docketOpenItem", "budget");   // §731: the budget process's item opened - its panel under it
            yield return Settle();
            ScrollBy(controller, UiScreen.Height * 0.5f);
            yield return Settle();
            yield return Capture("03c_decisions_budget_process_scrolled");
            SetPrivateField(controller, "_docketOpenItem", null);
            ResetScrolls(controller);

            // The Budget tab in the same state: the screen whose own rows sit above the process panel, and
            // the one screen where the banner deliberately does NOT repeat the hold.
            SetEnumField(controller, "_consolidatedTab", "Budget");
            ResetScrolls(controller);
            yield return Settle();
            yield return Capture("03d_budget_process_open");
            SetEnumField(controller, "_consolidatedTab", "Decisions");
            ResetScrolls(controller);

            if (!held) { pending.Remove(_countryId); }
            yield return Settle();
            if (sim.GetPendingBudgetProcess(_countryId) != held)
            {
                Debug.LogError("SHOT: P6-A3 - the staged interrupt was not taken back; every capture after this one is of a held clock.");
                _failed++;
            }
        }

        /// <summary>
        /// PS-8 (K163; M4 and N1 of the s938 reviews): THE ELECTION ABROAD ON THE DOCKET, filmed from a game played as play reaches it - never staged. The game is driven
        /// day by day from its start through the controller-shaped path (the day, the player's day tick, the boundary with no decision), every wait on the player
        /// answered first by the AI's rule for the player's party (K49, `AnswerWaitsAsTheAi`), a budget window answered by the player's own verb with the page's current draft (`IntroduceBudgetBill`; left open where it refuses), and the
        /// player's own election held on its polling day as the warm-up holds it (`HoldElectionWithoutTheNight`) - so from Sweden's start the 2026 Riksdag sits
        /// and its round has formed a government (or is still running on the day, said in the log) - every country other than the player's holding its own elections on the way, to the day after the AI USA's
        /// first count (3 Nov 2026 from Sweden's start, a midterm). A player's election not held, or no government formed after it, fails the film. Then the Docket: `abroad01_docket` with the USA's item listed (the Information stamp, the
        /// headline under it), `abroad02_docket_results` with its results card open (the field the item's own action writes). A missing item is a FAILURE here.
        /// S6 (Design's return of 2026-10-30): the card in each state the walk reaches, each frame once on the first day it holds (<see cref="AbroadStateDue"/>) -
        /// `abroad00_docket_upcoming` (an election ahead, listed from its campaign's start: every walk reaches the AI USA's House), and where a chamber abroad votes
        /// inside the walk (from Italy's start, Sweden's 11 Sep 2022 Riksdag) `abroad03_docket_forming` (counted, its formation owed) and `abroad04_docket_settled`
        /// (formed, or a caretaker where none was); then `abroad02p_docket_results_slip` (the count's slip pinned, 6a), `abroad05_desk_month` (the Desk's This month
        /// with the result's row) and `abroad06_stats_polling_days` (Statistics › International scrolled to the polling days). Nothing is staged: each state is the
        /// walk's own.
        /// </summary>
        private IEnumerator CaptureAbroad(GameController controller)
        {
            SimulationManager sim = controller.GetType().GetField("_simulationManager", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(controller) as SimulationManager;
            DateTime usDay = DateTime.MinValue;
            if (sim == null || _countryId == CountryId.USA || !string.IsNullOrEmpty(LoadSave) || !WorldClock.TryNextPollingDay(CountryId.USA, sim.CurrentDate.AddDays(1), out usDay))
            {
                Debug.LogError($"SHOT: -shotabroad plays a game whose player is not the USA from its start (this run plays {_countryId}, load '{LoadSave}', manager {(sim != null ? "found" : "NOT found")}); NOTHING filmed.");
                _failed++;
                EndSweep();
                yield break;
            }
            Expect("abroad01_docket", "abroad02_docket_results");
            // S6: the states the walk reaches - an election ahead in every walk; a chamber counted and its formation ended where one votes inside the walk
            Expect("abroad00_docket_upcoming", "abroad02p_docket_results_slip", "abroad05_desk_month", "abroad06_stats_polling_days");
            _abroadShotUpcoming = _abroadShotForming = _abroadShotSettled = false;
            string chamberInWalk = null;
            foreach (Country other in sim.World.Countries)
            {
                if (other.Id == _countryId || !WorldCalendar.TryNextChamberDay(other, sim.CurrentDate, out DateTime chamberDay, out bool _) || chamberDay >= usDay.Date) { continue; }
                chamberInWalk = $"{other.Id} on {chamberDay:yyyy-MM-dd}, first sitting {WorldCalendar.FirstSittingAfter(other, chamberDay):yyyy-MM-dd}";
                Expect("abroad03_docket_forming");
                if (WorldCalendar.FirstSittingAfter(other, chamberDay) < usDay.Date) { Expect("abroad04_docket_settled"); }
                break;
            }
            Debug.Log($"SHOT: ABROAD (S6) - the chamber abroad voting inside this walk: {chamberInWalk ?? "NONE - abroad03 and abroad04 belong to a session whose walk holds one (Italy's start: Sweden's 11 Sep 2022 Riksdag)"}");
            var noDecisions = new Dictionary<CountryId, PolicyDecision>();
            int walked = 0, answered = 0, budgets = 0;
            bool budgetLeftOpen = false;
            DateTime playerPolled = DateTime.MinValue;
            bool playerPollingInWalk = sim.TryPlayerPollingDay(out DateTime playerPolling) && playerPolling <= usDay;
            if (AbroadStateDue(sim, out string firstFrame, out WorldCalendar.Result firstOf)) { yield return AbroadStateFrame(controller, sim, firstFrame, firstOf); }   // the start's own day: an election ahead may be listed already
            while (sim.CurrentDate.Date <= usDay.Date && walked < MaxStateSearchDays)
            {
                // N1 (the s938 review): the walk is play's own day - every wait on the player answered first by the AI's rule for the player's party (K49, as GATE-18's
                // idle game and the forks answer it: the round's asks and offers, the meeting, the cabinet's decisions), and a budget window the player's government
                // holds the clock on answered by the player's own verb (N3 of review 3): the page's current draft introduced through IntroduceBudgetBill, its role gate
                // and range check the game's - where the verb refuses it the window is left open, as GATE-18's idle game leaves it - each said in the log
                sim.IsCounterfactual = true;
                try { if (sim.AnswerWaitsAsTheAi(_countryId)) { answered++; } }
                finally { sim.IsCounterfactual = false; }
                if (sim.GetPendingBudgetProcess(_countryId) && !budgetLeftOpen)
                {
                    BudgetBill draft = controller.GetType().GetMethod("BuildBudgetBillFromDrafts", BindingFlags.Instance | BindingFlags.NonPublic)?.Invoke(controller, null) as BudgetBill;
                    if (draft != null && sim.IntroduceBudgetBill(_countryId, draft))
                    {
                        budgets++;
                        Debug.Log($"SHOT: ABROAD - {sim.CurrentDate:yyyy-MM-dd}: the budget window answered by the player's own verb - the page's current draft introduced (IntroduceBudgetBill)");
                    }
                    else
                    {
                        budgetLeftOpen = true;   // said once; the window stays open, as GATE-18's idle game leaves it
                        Debug.Log($"SHOT: ABROAD - {sim.CurrentDate:yyyy-MM-dd}: the budget window LEFT OPEN - {(draft == null ? "the page's draft could not be built (BuildBudgetBillFromDrafts not reached)" : "the player's verb refused the page's draft (the LEVERS: line above says why)")}, as GATE-18's idle game leaves it");
                    }
                }
                if (!sim.GetPendingBudgetProcess(_countryId)) { budgetLeftOpen = false; }
                bool boundary = sim.AdvanceDay();
                MemTick(sim);
                sim.AdvanceCountryDayTick(_countryId);
                if (boundary) { sim.AdvanceTurn(noDecisions); EnergyMarket.BeginTurn(sim.World); }
                if (sim.PollingDayToday)
                {
                    // as the warm-up holds it (AdvanceDays): the player's own election held on its day without the night - play cannot walk past it
                    InvokeNoArg(controller, "HoldElectionWithoutTheNight");
                    playerPolled = sim.CurrentDate.Date;
                    bool over = controller.GetType().GetField("_isGameOver", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(controller) is bool o && o;
                    Debug.Log($"SHOT: ABROAD - {_countryId}'s own election held on {sim.CurrentDate:yyyy-MM-dd} as play holds it, without the night{(over ? " - and the run ENDED on its verdict" : string.Empty)}");
                    if (over) { Debug.LogError("SHOT: ABROAD - the game ended on the player's election verdict; abroad01 and abroad02 are MISSING, not clean."); _failed++; EndSweep(); yield break; }
                }
                walked++;
                if (walked % 30 == 0) { SetPrivateField(controller, "_daySpeedTimer", -ClockHoldSeconds); yield return null; }
                if (AbroadStateDue(sim, out string stateFrame, out WorldCalendar.Result stateOf)) { yield return AbroadStateFrame(controller, sim, stateFrame, stateOf); }
            }
            Country played = sim.World.GetCountry(_countryId);
            Debug.Log($"SHOT: ABROAD - the walk answered {answered} wait(s) by the AI's rule and {budgets} budget window(s); {_countryId} votes {(playerPollingInWalk ? playerPolling.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) : "after the walk")}, held {(playerPolled != DateTime.MinValue ? playerPolled.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) : "NOT")}; "
                + $"its government {string.Join("+", played.Government?.Cabinet ?? new List<string>())} led by {played.Government?.PmParty ?? "-"} from {played.Government?.FormedOn:yyyy-MM-dd}{(played.Government?.Round != null && played.Government.Round.Open ? " (its round still open)" : string.Empty)}");
            bool formedAfter = played.Government != null && played.Government.FormedOn > playerPolling.Date;
            bool roundGoesOn = played.Government?.Round != null && played.Government.Round.Open;   // the procedure still running on the day is a state play reaches too, and the log says so
            if (playerPollingInWalk && (playerPolled != playerPolling.Date || !(formedAfter || roundGoesOn)))
            {
                Debug.LogError($"SHOT: ABROAD - {_countryId}'s own {playerPolling:yyyy-MM-dd} election was not held, or no government was formed after it and no round runs, by {sim.CurrentDate:yyyy-MM-dd}; the Docket would show a state play cannot reach - abroad01 and abroad02 are MISSING, not clean.");
                _failed++;
                EndSweep();
                yield break;
            }
            SetPrivateField(controller, "_daySpeedTimer", -ClockHoldSeconds);
            List<WorldCalendar.Result> abroad = WorldCalendar.RecentAbroad(sim.World, _countryId, sim.CurrentDate);
            WorldCalendar.Result us = abroad.Find(r => r.Country == CountryId.USA && r.Day == usDay.Date && r.Rows.Count > 0);
            Debug.Log($"SHOT: ABROAD - walked {walked} day(s) to {sim.CurrentDate:yyyy-MM-dd}; the elections abroad listed: " + (abroad.Count > 0 ? string.Join("; ", abroad.ConvertAll(r => r.Country + " " + r.Elected + " " + r.Day.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) + " - " + r.Headline)) : "NONE"));
            if (us == null)
            {
                Debug.LogError($"SHOT: ABROAD - no count of the USA on {usDay:yyyy-MM-dd} is listed on the Docket (the CALENDAR: lines above say why); abroad01 and abroad02 are MISSING, not clean.");
                _failed++;
                EndSweep();
                yield break;
            }
            SetPrivateField(controller, "_onDesk", false);
            SetEnumField(controller, "_consolidatedTab", "Decisions");
            SetPrivateField(controller, "_docketOpenItem", null);
            ResetScrolls(controller);
            yield return Settle();
            Claim("imgui");
            yield return Capture("abroad01_docket");
            SetPrivateField(controller, "_docketOpenItem", "abroad:" + us.Key);   // the item's own action: its results card open under it
            yield return Settle();
            Claim("imgui");
            yield return Capture("abroad02_docket_results");
            LogAbroadCard("abroad02_docket_results", sim, us);

            // S6 (6a): the count's slip pinned on the open card, then let go
            MethodInfo pinCount = controller.GetType().GetMethod("PinSlipOnAnchorForFilm", BindingFlags.Instance | BindingFlags.NonPublic);
            FieldInfo countPins = controller.GetType().GetField("_slipPins", BindingFlags.Instance | BindingFlags.NonPublic);
            if (pinCount != null && countPins != null)
            {
                pinCount.Invoke(controller, new object[] { "docket:abroadcount:abroad:" + us.Key, new Vector2(UiScreen.Width * 0.45f, UiScreen.Height * 0.40f) });
                yield return Settle();
                yield return Settle();
                Claim("imgui");
                yield return Capture("abroad02p_docket_results_slip");
                (countPins.GetValue(controller) as IList)?.Clear();
                yield return Settle();
            }
            else { Debug.LogError("SHOT: S6 - the controller has no PinSlipOnAnchorForFilm / _slipPins; abroad02p_docket_results_slip NOT written."); _failed++; }
            SetPrivateField(controller, "_docketOpenItem", null);

            // S6 (6c, left): the Desk's This month - the result's row (the globe, the country and chamber, the first list and its seats) - scrolled to its card
            SetPrivateField(controller, "_onDesk", true);
            ResetScrolls(controller);
            yield return Settle();
            ScrollDeskTo(controller, "approval");
            yield return Settle();
            Claim("imgui");
            yield return Capture("abroad05_desk_month");
            ScrollDeskTo(controller, null);

            // S6 (6c, right): Statistics › International scrolled to the polling days card (its top as the page last drew it)
            SetPrivateField(controller, "_onDesk", false);
            SetEnumField(controller, "_consolidatedTab", "Statistics");
            SetEnumField(controller, "_statisticsCategory", "International");
            ResetScrolls(controller);
            yield return Settle();
            float pollingTop = controller.GetType().GetField("_pollingDaysTop", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(controller) is float t ? t : -1f;
            FieldInfo statsScroll = controller.GetType().GetField("_statisticsContentScrollPosition", BindingFlags.Instance | BindingFlags.NonPublic);
            if (pollingTop >= 0f && statsScroll != null)
            {
                statsScroll.SetValue(controller, new Vector2(0f, Mathf.Max(0f, pollingTop - 8f)));
                yield return Settle();
                Claim("imgui");
                yield return Capture("abroad06_stats_polling_days");
                if (controller.GetType().GetField("_pollingRows", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(controller) is List<ForeignElectionCard.PollingRow> rows)
                {
                    Debug.Log("SHOT: abroad06_stats_polling_days - the polling days on " + sim.CurrentDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) + ": "
                        + string.Join("; ", rows.ConvertAll(p => p.Code + " " + p.Chamber + " " + (p.Day == DateTime.MinValue ? "ABSENT" : p.Day.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) + " (" + p.In + ")") + " " + (p.Stamp ?? "-"))));
                }
            }
            else { Debug.LogError($"SHOT: S6 - the polling days card drew no top ({pollingTop}) or the Statistics scroll was not found; abroad06_stats_polling_days NOT written."); _failed++; }
            SetEnumField(controller, "_statisticsCategory", "Domestic");
            ResetScrolls(controller);
            EndSweep();
        }

        /// <summary>S6: the walk's state frames, each once a run (<see cref="CaptureAbroad"/> resets them).</summary>
        private bool _abroadShotUpcoming, _abroadShotForming, _abroadShotSettled;

        /// <summary>S6: the state frame the walk's day holds, if one is still owed - an election ahead with a card (a chamber whose government a procedure forms first,
        /// else the US House); else a chamber abroad counted and forming, from the day after its polling day; else one whose formation ended (formed, or a caretaker).
        /// Read off the same records the Docket reads (`WorldCalendar`, `ForeignElectionCard.Read`); marks the frame taken.</summary>
        private bool AbroadStateDue(SimulationManager sim, out string frame, out WorldCalendar.Result of)
        {
            frame = null;
            of = null;
            DateTime today = sim.CurrentDate;
            if (!_abroadShotUpcoming)
            {
                foreach (WorldCalendar.Result r in WorldCalendar.UpcomingAbroad(sim.World, _countryId, today))
                {
                    if (r.Rows.Count > 0 && (of == null || (r.Formation && !of.Formation))) { of = r; }
                }
                if (of != null) { frame = "abroad00_docket_upcoming"; _abroadShotUpcoming = true; return true; }
            }
            if (_abroadShotForming && _abroadShotSettled) { return false; }
            foreach (WorldCalendar.Result r in WorldCalendar.RecentAbroad(sim.World, _countryId, today))
            {
                if (!r.Formation || r.Rows.Count == 0) { continue; }
                ForeignElectionCard card = ForeignElectionCard.Read(r, sim.World.GetCountry(r.Country), today, sim.LastCalendarFormation(r.Country));
                if (!_abroadShotForming && card.Is == ForeignElectionCard.State.Forming && today.Date > r.Day.Date) { frame = "abroad03_docket_forming"; _abroadShotForming = true; of = r; return true; }
                if (!_abroadShotSettled && (card.Is == ForeignElectionCard.State.Formed || card.Is == ForeignElectionCard.State.Caretaker)) { frame = "abroad04_docket_settled"; _abroadShotSettled = true; of = r; return true; }
            }
            return false;
        }

        /// <summary>S6: one state frame - the Docket with the election's card open (the field its own action writes), the clock held, then closed again.</summary>
        private IEnumerator AbroadStateFrame(GameController controller, SimulationManager sim, string frame, WorldCalendar.Result of)
        {
            SetPrivateField(controller, "_daySpeedTimer", -ClockHoldSeconds);
            SetPrivateField(controller, "_onDesk", false);
            SetEnumField(controller, "_consolidatedTab", "Decisions");
            SetPrivateField(controller, "_docketOpenItem", "abroad:" + of.Key);
            ResetScrolls(controller);
            UiGuardContext.CurrentScreen = frame;   // §962: the settle frames under their own name - the guard filed Italy's first card under 05t_budget_turn0
            yield return Settle();
            Claim("imgui");
            yield return Capture(frame);
            LogAbroadCard(frame, sim, of);
            SetPrivateField(controller, "_docketOpenItem", null);
        }

        /// <summary>S6: what a card frame shows, as the card reads it - the state, the entry's state words, who governs, the steps, how it ended, the dates.</summary>
        private void LogAbroadCard(string frame, SimulationManager sim, WorldCalendar.Result of)
        {
            ForeignElectionCard card = ForeignElectionCard.Read(of, sim.World.GetCountry(of.Country), sim.CurrentDate, sim.LastCalendarFormation(of.Country));
            string steps = card.StepLabel == null ? "none (no formation)" : card.StepsAbsent ? "ABSENT" : string.Join(" ", card.Cells.ConvertAll(c => c.State + (c.Asked != null ? ":" + c.Asked : string.Empty)));
            Debug.Log($"SHOT: {frame} - {of.Country} {of.Chamber} {of.Day:yyyy-MM-dd} read on {sim.CurrentDate:yyyy-MM-dd}: {card.Stamp} ('{card.When}'); {of.Rows.Count} list(s) of {card.Total}, majority {card.Majority}; "
                + $"who governs '{card.Standing}' {(card.CabinetSeats >= 0 ? card.CabinetSeats.ToString(CultureInfo.InvariantCulture) : "-")} [{string.Join("+", card.CabinetParties)}]; {card.StepLabel ?? "-"} {steps} '{card.StepFigure}'; ended '{card.Ended}'; "
                + $"dates {string.Join(", ", card.Dates.ConvertAll(d => d.Label + " " + d.Value))}");
        }

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
        /// <summary>EN-7a: the viewer's PROVENANCE setting a frame switched on, while that frame is out - Finish restores it, so a run that ends on the frame leaves the preference as it found it.</summary>
        private bool? _provenanceToRestore;

        /// <summary>The identity trap's count, once a run (the sitting package, 2026-09-21): the sweep said it at its end and the four modes that end through their own Finish said
        /// nothing, so a campaign or an election-night film proved every frame and printed no line to show for it. A dry film reads no pixels and claims none.</summary>
        private void ReportIdentity()
        {
            if (_identityReported || Dry) { return; }
            _identityReported = true;
            Debug.Log($"SHOT: capture-identity - {_identityAsserts} capture(s) proved they show the surface they claim.");
        }
        private bool _identityReported;

        private void Finish(int exitCode)
        {
            _finishCalled = true;
            ReportIdentity();
            if (_provenanceToRestore.HasValue) { DeskProvenance.On = _provenanceToRestore.Value; _provenanceToRestore = null; }
            Debug.Log($"SHOT: preferences restored - {PreferencePins.Restore()} key(s) written back as the film found them.");   // MM-2: after the frame-level provenance restore, so the snapshot wins
            // P2-1.3 (2026-09-02): every ledger row the film drew recorded the range a pixel of its track covers;
            // a whole point is reachable without overshoot only when that does not exceed the row's snap.
            // EN-8 (2026-09-12): the range is recorded in the row's GRAIN (a point; a per-tonne row's ceiling's hundredth,
            // TaxLine.DialGrain), so the Carbon Tax row in kronor per tonne is judged on the resolution its own row prints.
            if (_reachByCapture.Count > 0)
            {
                string worstName = null;
                float worst = 0f;
                // Keyed "capture stem / row": the verdict is the BUDGET screen's (stems 05*), which is the row P2-1.3
                // set; every other ledger row (the Policy Web's node panel draws the same dial in a side box) is
                // reported for the record, never judged here.
                string worstAnyName = null;
                float worstAny = 0f;
                foreach (KeyValuePair<string, float> r in _reachByCapture)
                {
                    if (worstAnyName == null || r.Value > worstAny) { worstAnyName = r.Key; worstAny = r.Value; }
                    if (!r.Key.StartsWith("05", System.StringComparison.Ordinal)) { continue; }
                    if (worstName == null || r.Value > worst) { worstName = r.Key; worst = r.Value; }
                }

                string worstGeometry = worstAnyName != null && PoliSim.UI.LedgerRow.GeometryByRow.TryGetValue(worstAnyName, out (Rect Name, Rect Track, Rect Figure, Rect Trailing) g)
                    ? $" Track {g.Track.width:0} px, name {g.Name.width:0}, figure {g.Figure.width:0}, trailing {g.Trailing.width:0}, row {g.Name.x:0}..{g.Trailing.xMax:0}."
                    : string.Empty;   // P5-1: the worst row's widths, so a floor is measured rather than guessed
                Debug.Log($"SHOT: slider reach, every ledger row - worst {worstAny:0.000} grains per pixel ({worstAnyName}; a grain is a point, a per-tonne row's ceiling's hundredth, or a spending row's range's hundredth on a 1-2-5 step).{worstGeometry}");
                bool reachable = worstName == null || worst <= PoliSim.UI.LedgerRow.WholeUnit;
                string reach = $"SHOT: slider reach on the Budget - {_reachByCapture.Count} ledger row(s) drawn in the captured frames, worst Budget row {worst:0.000} grains per pixel ({worstName}) against the whole-grain bound of {PoliSim.UI.LedgerRow.WholeUnit:0.###} (the step adapts to the track: {PoliSim.UI.LedgerRow.StepFor(worst):0.###} grain on that row; a grain is a point, a per-tonne row's ceiling's hundredth, or a spending row's range's hundredth on a 1-2-5 step).";
                if (reachable) { Debug.Log(reach); }
                else
                {
                    Debug.LogError(reach + " A whole grain is NOT reachable without overshoot on that row - lengthen the track or coarsen the row's grain.");
                    exitCode = System.Math.Max(exitCode, 1);
                }
            }

            // ⚠ The hook runs BEFORE the exit and can only make the code worse, never better. A hook that
            // throws must not swallow the run's own verdict, so it is caught and counted as a failure -
            // an exception in a guard is not a pass, which is this suite's standing rule for checks.
#if UNITY_EDITOR
            string table = DryGuiPass.WriteTable(Path.Combine(OutputDirectory, "labels"), Label);
            Debug.Log(table == null
                ? "SHOT: label table - nothing recorded (no captured frame drew text through a style)."
                : $"SHOT: label table - {DryGuiPass.Summary}; written to {table}.");
#endif

            if (Dry)
            {
                Debug.Log("SHOT: DRY - the edge guard, the identity token and the frame-size traps read pixels, and the canvas-text guard reads a Canvas scaled by the real backbuffer (640x480 with no window, whatever geometry this run names); they stay the film's, and this run claims none of them.");
                LastSweepSummary = $"{LastSweepSummary}exit {exitCode}";
                if (DryExit != null)
                {
                    // The sweep is over for this session: nothing after the stop frame may run while play mode winds down.
                    StopAllCoroutines();
                    DryExit(exitCode);
                    return;
                }
            }
            else if (BeforeExit != null)
            {
                try { exitCode = System.Math.Max(exitCode, BeforeExit(exitCode)); }
                catch (System.Exception e)
                {
                    Debug.LogError("SHOT: the before-exit guard threw and is counted as a FAILURE - " + e.Message);
                    exitCode = System.Math.Max(exitCode, 1);
                }
            }

#if UNITY_EDITOR
            Debug.Log($"SHOT: exiting {exitCode}.");   // the result line the launcher's hang watchdog reads (Tools/unity_run.ps1): the film's work and its before-exit guards are done
            UnityEditor.EditorApplication.Exit(exitCode);
#endif
        }

        /// <summary>The dry film's frame: OnGUI delivered once a frame, as a Game View would, so everything the controller
        /// advances inside OnGUI (the Canvas seam's envelope, the per-frame style rescale) advances between captures.</summary>
        private void LateUpdate()
        {
#if UNITY_EDITOR
            if (Dry && _dryController != null && !_finishCalled && !DryGuiPass.Pump(_dryController, _dryOnGui))
            {
                DryStop();
            }
#endif
        }

#if UNITY_EDITOR
        /// <summary>The dry pass's own machinery failed: named once, and the run ends failed - never a frame-by-frame repeat.</summary>
        private void DryStop()
        {
            if (_finishCalled) { return; }
            Debug.LogError($"SHOT: DRY - the IMGUI pass failed ({DryGuiPass.Failure}); the dry film stops here, measuring nothing further.");
            StopAllCoroutines();
            Finish(1);
        }
#endif

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
        /// <summary>§510: how far below a day play's clock is primed while held - an hour of real time, against a stage that reaches the warm-up in seconds.</summary>
        private const float ClockHoldSeconds = 3600f;

        /// <summary>Ceiling on the search for a preliminary release, so a schedule change can never turn the warm-up into an unbounded loop - the same bounded-retry discipline UiScreenshotCapture applies to waiting for play mode.</summary>
        private const int MaxWarmupDays = 365 * 5;

        /// <summary>
        /// §510 (2026-09-15): the days PLAY'S OWN CLOCK ran before the warm-up - read off the date against the epoch, and a failure when
        /// any. The controller's clock is real time (a second a day at Normal), so a slow frame between the running captures and the
        /// warm-up let it tick a day. That day is not a figure: only the controller's day tick opens the incoming government's budget
        /// window (the warm-up's `AdvanceDay` does not), so every frame after the warm-up carried an open window, the warm-up stopped a
        /// day earlier, and Poland's hold banner wrapped under the edge guard - frames decided by the machine's speed (§509).
        /// </summary>
        private void ReportClockBeforeWarmup(object controller)
        {
            FieldInfo simField = controller.GetType().GetField("_simulationManager", BindingFlags.Instance | BindingFlags.NonPublic);
            if (!(simField?.GetValue(controller) is SimulationManager sim)) { return; }   // AdvanceDays reports the unreachable manager
            int ran = (sim.CurrentDate - (_loadedDate != DateTime.MinValue ? _loadedDate : SimulationManager.EpochDate)).Days;   // §560: a loaded game's clock is held from the save's own date
            if (ran > 0)
            {
                Debug.LogError($"SHOT: play's clock RAN {ran} day(s) in real time before the warm-up (the date {sim.CurrentDate:yyyy-MM-dd}) - the budget window and the warm-up's stop now depend on the machine's speed.");
                _failed++;
            }
            else { Debug.Log($"SHOT: play's clock HELD before the warm-up - the date {sim.CurrentDate:yyyy-MM-dd}, 0 days run."); }
        }

        /// <summary>§560: loads <see cref="LoadSave"/> from the game's saves directory through `GameController.LoadFromPath` - the path F9 and the menu's Load share - and
        /// holds the result to what the flag promises: the file exists, the load was not refused, the player is this run's country. Logs the file's SHA-256.</summary>
        private bool LoadNamedSave(GameController controller)
        {
            string path = Path.Combine(PoliSim.Persistence.SaveGameService.DefaultSaveDirectory, LoadSave + ".json");
            if (!File.Exists(path)) { Debug.LogError($"SHOT: -shotload={LoadSave} - no such save at {path}. NOTHING filmed: a fresh world under a loaded game's name is what this flag exists to prevent."); return false; }
            string digest;
            using (var sha = System.Security.Cryptography.SHA256.Create()) using (FileStream stream = File.OpenRead(path)) { digest = BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "").ToLowerInvariant(); }
            MethodInfo load = controller.GetType().GetMethod("LoadFromPath", BindingFlags.Instance | BindingFlags.NonPublic);
            FieldInfo simField = controller.GetType().GetField("_simulationManager", BindingFlags.Instance | BindingFlags.NonPublic);
            FieldInfo statusField = controller.GetType().GetField("_savesMenuStatus", BindingFlags.Instance | BindingFlags.NonPublic);
            if (load == null || simField == null || statusField == null) { Debug.LogError("SHOT: -shotload - the controller's load path could not be reached (LoadFromPath / _simulationManager / _savesMenuStatus). NOTHING filmed."); return false; }
            load.Invoke(controller, new object[] { path });
            string status = statusField.GetValue(controller) as string;
            var sim = simField.GetValue(controller) as SimulationManager;
            if (!string.IsNullOrEmpty(status) || sim == null) { Debug.LogError($"SHOT: -shotload={LoadSave} - the game did not take the save: {status ?? "no manager"}. NOTHING filmed."); return false; }
            if (sim.PlayerCountryId != _countryId) { Debug.LogError($"SHOT: -shotload={LoadSave} - the save's player is {sim.PlayerCountryId} and this run films {_countryId}. NOTHING filmed."); return false; }
            _loadedDate = sim.CurrentDate;
            Debug.Log($"SHOT: -shotload={LoadSave} - LOADED through the game's own load path: sha256 {digest}, {new FileInfo(path).Length} bytes, {sim.CurrentDate:yyyy-MM-dd}, turn {sim.CurrentTurn}, player {sim.PlayerCountryId}. Every frame from here is of this game.");
            return true;
        }

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

                // PS-2 / CL-4 (§619): A WARM-UP HOLDS THE ELECTION PLAY WOULD HOLD. Play votes on the player's country's polling day (the
                // controller's Update); this loop drives the manager alone, so walking past that day would leave a game past an election never
                // held - a state play cannot produce, on a chamber the game never voted in (the first film of §619 showed a 2030 night with no
                // history beside it). The controller holds it here without the night's board, and the warm-up goes on; a first try that
                // stopped on the eve instead starved every history-dependent capture at turn 0 (the dry film `drycal`: 4 failed).
                if (sim.PollingDayToday)
                {
                    InvokeNoArg(controller, "HoldElectionWithoutTheNight");
                    FieldInfo overField = controller.GetType().GetField("_isGameOver", BindingFlags.Instance | BindingFlags.NonPublic);
                    bool over = overField?.GetValue(controller) is bool b && b;
                    Debug.Log($"SHOT: warm-up held {playerCountry}'s election on {sim.CurrentDate:yyyy-MM-dd} (day {days} / turn {turns}) as play would, without the night{(over ? " - and the run ENDED on its verdict; the warm-up stops here" : string.Empty)}.");
                    if (over) { return; }
                }

                // PS-6 K85/K90 (the second review's L2): A WARM-UP ANSWERS THE BINDING LIMIT'S HOLD AS PLAY WOULD - play's clock holds on it, so a REP
                // player-president never runs months under a binding limit nobody answered. The President proposes the standard raise and signs it (voted at once
                // past the deadline, K43); where an AI majority withholds it (K89) he lets the limit bind. Said in the log.
                if (sim.DebtLimitAwaitsPresident(playerCountry))
                {
                    Country limitCountry = sim.World.GetCountry(playerCountry);
                    bool aiWithholds = limitCountry.DebtLimitWithheldBy != null && limitCountry.DebtLimitWithheldBy != limitCountry.PlayerPartyAbbrev;
                    string limitRefused;
                    bool limitOk;
                    if (aiWithholds) { limitOk = sim.LetDebtLimitBind(playerCountry, out limitRefused); }
                    else
                    {
                        System.Func<DivisionRecord, bool> answerWas = sim.PlayerPresidentVetoes;
                        sim.PlayerPresidentVetoes = _ => false;   // the President signs his own raise on the day, as play's desk would
                        try { limitOk = sim.ProposeDebtLimitRaise(playerCountry, UsDebtLimit.RaiseUsd, DateTime.MinValue, out limitRefused); }
                        finally { sim.PlayerPresidentVetoes = answerWas; }
                    }
                    Debug.Log($"SHOT: warm-up answered the binding debt limit on {sim.CurrentDate:yyyy-MM-dd} as play would: {(aiWithholds ? "LET IT BIND" : "the standard raise proposed and signed")} - {(limitOk ? "done" : "REFUSED: " + limitRefused)}");
                }


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
                // P5-B5: the drafts are FIGURES - alternate 1.06x and 0.96x of the line's standing amount (the +6 % / -4 % the frame always showed).
                object amountObj = line.GetType().GetProperty("Amount")?.GetValue(line) ?? line.GetType().GetField("Amount")?.GetValue(line);
                float standingAmount = amountObj is float fa ? fa : 0f;
                inputs[category] = standingAmount * ((drafted % 2 == 0) ? 1.06f : 0.96f);
                drafted++;
            }

            Debug.Log($"SHOT: drafted {drafted} spending lines (x1.06, x0.96 of their standing amounts) so B1's hatch, draft figure and pencil render.");
        }

        private static bool SetPrivateField(object target, string field, object value)
        {
            FieldInfo f = target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic);
            // §731: null is a value too, for a field of a reference type (the docket's open item closed) - IsInstanceOfType says false for it
            bool fits = f != null && (value == null ? !f.FieldType.IsValueType || Nullable.GetUnderlyingType(f.FieldType) != null : f.FieldType.IsInstanceOfType(value));
            if (!fits)
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

        /// <summary>Ceiling on the search for a head's term's end — bounded like every other wait in this harness. AF-7: the longest own-bank term the statutes set
        /// (`CentralBankTerms`) is six years, and a six-year span holds at most two leap days.</summary>
        private const int MaxHeldSearchDays = 365 * 6 + 2;

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

            if (sim.World?.GetCountry(_countryId)?.CurrentFedChair == null)
            {
                Debug.LogWarning($"SHOT: {_countryId} has no head of its own bank (the euro members' is the ECB's, the AI's to appoint) - HELD captures skipped rather than mislabelled.");
                yield break;
            }

            var noDecisions = new Dictionary<CountryId, PolicyDecision>();
            int guard = 0;
            // AF-7 (q7): the head's term ends on its STATUTE's date (`CentralBankTerms`), where the turn cadence stood until AF-7 removed it - so
            // the HELD state is reached on the day the player's bank's head awaits the player's pick.
            // K16: the Fed's hold rises at the term's end under any Senate; a term that ends where the player does not appoint (the role gate) raises none
            // and the Vice Chair acts, so the search stops there too rather than stepping on for years
            Country held = sim.World.GetCountry(_countryId);
            while (!sim.CentralBankHeadAwaitsPlayer(_countryId) && UsFedConsent.HoldoverWords(held, held.CurrentFedChair, sim.CurrentDate) == null && guard < MaxHeldSearchDays)
            {
                if (sim.AdvanceDay())
                {
                    sim.AdvanceTurn(noDecisions);
                }

                guard++;
            }

            string heldOver = UsFedConsent.HoldoverWords(held, held.CurrentFedChair, sim.CurrentDate);
            if (!sim.CentralBankHeadAwaitsPlayer(_countryId) && heldOver != null)
            {
                Debug.LogWarning($"SHOT: {_countryId} - {heldOver} - no hold is raised, so the HELD banner will NOT be in this capture set.");
                yield break;
            }

            if (!sim.CentralBankHeadAwaitsPlayer(_countryId))
            {
                Debug.LogWarning($"SHOT: no head's term ended within {MaxHeldSearchDays} days (or the player does not appoint {_countryId}'s) - the HELD banner will NOT be in this capture set.");
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

            // PS-6 US-29 (the review of 2026-10-22, M3): the Fed's hold rose, so the holdover is staged from here - the seated Senate turned over
            if (UsFedConsent.Applies(held)) { yield return CaptureFedHoldover(controller, sim, held); }
        }

        /// <summary>
        /// K15-K16 (Elias's rulings of 2026-10-23; US-29's 90h/90i re-staged): <b>THE FED CHAIR'S TERM ENDED UNFILLED</b> - reached from the HELD state, where
        /// the chair's term has ended and the player-president's choice is open. Two states, each verified before its frames are named: <b>90h</b> a nominee
        /// before the Senate - the first candidate LODGED (`UsFedConsent.Lodge`, as the verb lodges it, without the lever so no world but the played one is
        /// touched), the Vice Chair acting - the bank tab's governor block (`90h_bank_acting`), the lever's column at the page's foot (`90h_bank_acting_lever`),
        /// the Docket's item that does not hold the clock (`90h_docket_before_senate`) and the Congress view's "Before the Senate" card
        /// (`90h_congress_before_senate`); <b>90i</b> the Senate's refusal as an event - a refused vote PLANTED through `UsFedConsent.Resolve` with a refusing
        /// count (the crossing rule refuses in no Senate the game seats - `FedChairConsentDiagnostic` measures it - so the state is planted, never reached),
        /// the Docket's refusal item (`90i_docket_refused`) and the bank tab (`90i_bank_refused`). No day passes: play's clock is held, the candidates are set
        /// aside for the frames, and the nomination, the votes, the acting flag, the candidates, the tab and the clock are put back after; a hold that does not
        /// come back fails the run.
        /// </summary>
        private IEnumerator CaptureFedHoldover(GameController controller, SimulationManager sim, Country player)
        {
            const BindingFlags Own = BindingFlags.Instance | BindingFlags.NonPublic;
            FieldInfo candidatesField = controller.GetType().GetField("_fedChairCandidates", Own), forTurnField = controller.GetType().GetField("_fedChairCandidatesForTurn", Own);
            FieldInfo speedField = controller.GetType().GetField("_gameSpeed", Own), timerField = controller.GetType().GetField("_daySpeedTimer", Own);
            FieldInfo tabField = controller.GetType().GetField("_consolidatedTab", Own), categoryField = controller.GetType().GetField("_politicsCategory", Own);
            FieldInfo onDeskField = controller.GetType().GetField("_onDesk", Own), openItemField = controller.GetType().GetField("_docketOpenItem", Own);
            if (candidatesField == null || forTurnField == null || speedField == null || timerField == null || tabField == null || categoryField == null || onDeskField == null || openItemField == null)
            {
                Debug.LogError("SHOT: the Fed frames could not snapshot the selection, the clock or the tab (_fedChairCandidates/_fedChairCandidatesForTurn/_gameSpeed/_daySpeedTimer/_consolidatedTab/_politicsCategory/_onDesk/_docketOpenItem not found) - 90h/90i are MISSING, not clean.");
                _failed++;
                yield break;
            }
            DateTime day = sim.CurrentDate;
            var drawn = candidatesField.GetValue(controller) as List<FedChair>;
            if (!CentralBankTerms.Ended(player.CurrentFedChair, day) || !Congress.PresidentOn(player, day, out string president, out string pp, out _) || string.IsNullOrEmpty(pp)
                || drawn == null || drawn.Count == 0 || player.FedNominationPending != null || player.FedViceChair == null)
            {
                Debug.LogError($"SHOT: 90h/90i - on {day:yyyy-MM-dd} the Fed chair's term has not ended, no president is found, no candidate is drawn, a nomination is already pending or no Vice Chair is seated - MISSING, not clean.");
                _failed++;
                yield break;
            }
            object candidatesWas = candidatesField.GetValue(controller), forTurnWas = forTurnField.GetValue(controller), speedWas = speedField.GetValue(controller), timerWas = timerField.GetValue(controller);
            object tabWas = tabField.GetValue(controller), categoryWas = categoryField.GetValue(controller), onDeskWas = onDeskField.GetValue(controller), openItemWas = openItemField.GetValue(controller);
            int candidatesBefore = drawn.Count, votesBefore = player.FedSenateVotes?.Count ?? 0;
            bool actingWas = player.FedViceChairActing;
            FedChair chairWas = player.CurrentFedChair;
            bool holdWas = sim.CentralBankHeadAwaitsPlayer(player.Id);
            timerField.SetValue(controller, -ClockHoldSeconds);
            try
            {
                SetPrivateField(controller, "_onDesk", false);

                // ---- 90h: a nominee before the Senate, the Vice Chair acting ----
                FedNomination lodged = UsFedConsent.Lodge(player, drawn[0].Copy(), pp, day);
                player.FedViceChairActing = UsFedConsent.ViceChairActs(player, day);
                candidatesField.SetValue(controller, null);
                string pending = UsFedConsent.PendingWords(player), acting = UsFedConsent.ActingWords(player, day);
                if (pending == null || acting == null || !player.FedViceChairActing || sim.CentralBankHeadAwaitsPlayer(player.Id))
                {
                    Debug.LogError($"SHOT: 90h - the staging did not give a nominee before the Senate with the Vice Chair acting (pending: {pending ?? "none"}; acting: {acting ?? "none"}; hold {sim.CentralBankHeadAwaitsPlayer(player.Id)}) - MISSING, not clean.");
                    _failed++;
                }
                else
                {
                    SetEnumField(controller, "_consolidatedTab", "Politics");
                    SetEnumField(controller, "_politicsCategory", "FederalReserve");
                    ResetScrolls(controller);
                    yield return Settle();
                    Debug.Log($"SHOT: 90h_bank_acting - staged: {president} ({pp}) lodged {lodged.Nominee.Name} on {day:yyyy-MM-dd}; {pending}; {acting}");
                    yield return Capture("90h_bank_acting");
                    SetScrolls(controller, UiScreen.Height * 20f);   // past the page's foot - the scroll view clamps there, and the political half (the lever's column) is the page's last block
                    yield return Settle();
                    yield return Capture("90h_bank_acting_lever");
                    SetEnumField(controller, "_consolidatedTab", "Decisions");
                    SetPrivateField(controller, "_docketOpenItem", null);
                    ResetScrolls(controller);
                    yield return Settle();
                    yield return Capture("90h_docket_before_senate");
                    SetEnumField(controller, "_consolidatedTab", "Politics");
                    SetEnumField(controller, "_politicsCategory", "Parliament");
                    ResetScrolls(controller);
                    yield return Settle();
                    yield return Capture("90h_congress_before_senate");
                }

                // ---- 90i: the Senate's refusal as an event (planted: the rule refuses in no seated Senate) ----
                var refusing = new UsFedConsent.Count { President = president, PresidentsParty = pp, For = 48, Against = 52, OwnSeats = 48, OtherSeats = 52, Confirms = false };
                FedSenateVote vote = UsFedConsent.Resolve(player, lodged, refusing, day);
                candidatesField.SetValue(controller, null);
                string refusedWords = UsFedConsent.VoteWords(UsFedConsent.StandingRefusal(player, day));
                if (vote.Confirmed || refusedWords == null || player.FedNominationPending != null || !ReferenceEquals(player.CurrentFedChair, chairWas) || !player.FedViceChairActing)
                {
                    Debug.LogError($"SHOT: 90i - the planted refusal did not stand ({refusedWords ?? "no standing refusal"}; pending {player.FedNominationPending != null}; acting {player.FedViceChairActing}) - MISSING, not clean.");
                    _failed++;
                }
                else
                {
                    SetEnumField(controller, "_consolidatedTab", "Decisions");
                    SetPrivateField(controller, "_docketOpenItem", null);
                    ResetScrolls(controller);
                    yield return Settle();
                    Debug.Log($"SHOT: 90i_docket_refused - planted: {refusedWords}");
                    yield return Capture("90i_docket_refused");
                    SetEnumField(controller, "_consolidatedTab", "Politics");
                    SetEnumField(controller, "_politicsCategory", "FederalReserve");
                    ResetScrolls(controller);
                    yield return Settle();
                    yield return Capture("90i_bank_refused");
                }
            }
            finally
            {
                player.FedNominationPending = null;
                if (player.FedSenateVotes != null && player.FedSenateVotes.Count > votesBefore) { player.FedSenateVotes.RemoveRange(votesBefore, player.FedSenateVotes.Count - votesBefore); }
                player.CurrentFedChair = chairWas;
                player.FedViceChairActing = actingWas;
                candidatesField.SetValue(controller, candidatesWas);
                forTurnField.SetValue(controller, forTurnWas);
                speedField.SetValue(controller, speedWas);
                timerField.SetValue(controller, timerWas);
                tabField.SetValue(controller, tabWas);
                categoryField.SetValue(controller, categoryWas);
                onDeskField.SetValue(controller, onDeskWas);
                openItemField.SetValue(controller, openItemWas);
                ResetScrolls(controller);
            }
            yield return Settle();
            int candidatesAfter = (candidatesField.GetValue(controller) as ICollection)?.Count ?? 0;
            object speedAfter = speedField.GetValue(controller);
            bool holdAfter = sim.CentralBankHeadAwaitsPlayer(player.Id);
            int votesAfter = player.FedSenateVotes?.Count ?? 0;
            string restored = $"SHOT: 90h/90i restored - the nomination cleared, the Senate's votes {votesAfter} (before {votesBefore}), the acting flag {player.FedViceChairActing} (before {actingWas}); the Fed chair's candidates {candidatesAfter} (before {candidatesBefore}), the speed {speedAfter} (before {speedWas}), the day {sim.CurrentDate:yyyy-MM-dd} (before {day:yyyy-MM-dd}), the hold {holdAfter} (before {holdWas})";
            if (candidatesAfter != candidatesBefore || !Equals(speedAfter, speedWas) || votesAfter != votesBefore || player.FedNominationPending != null || player.FedViceChairActing != actingWas
                || sim.CurrentDate != day || holdAfter != holdWas || !ReferenceEquals(player.CurrentFedChair, chairWas)) { Debug.LogError(restored + " - THE HOLD DID NOT RETURN."); _failed++; }
            else { Debug.Log(restored + "."); }
        }

        /// <summary>Bound on each state search (budget pause, decision/meeting rolls). Generous — four sim years — but a bound, per this harness's standing rule that an unbounded wait is a hang with no log line.</summary>
        private const int MaxStateSearchDays = 365 * 4 + 7;   // PS-2 (§619, the review): consecutive second Sundays of September can be 1 462 days apart (a leap year between), one more than four years

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

            // PS-3c (§630): the bills staged here are the GOVERNMENT'S - seat the record's prime-minister party for them, else the role gate refuses every one (the sweep seats the largest party, which may be in opposition).
            string seatedBefore = player.PlayerPartyAbbrev;
            PolicyImpactLedger stagingLedger = sim.ImpactLedger;   // K51 (the merged film real911): the staged bills are the re-seated government's, not the player's - the impact ledger does not read them, or each kind forks a world whose campaign replays under the borrowed party and fails its draw check
            sim.ImpactLedger = null;
            if (player.Government != null && player.Government.PmParty != null && player.PlayerPartyAbbrev != player.Government.PmParty) { Debug.Log($"SHOT: bills staged - re-seating {player.Government.PmParty}, the prime minister's party, in place of {player.PlayerPartyAbbrev} so the bills are the government's (§630)"); player.PlayerPartyAbbrev = player.Government.PmParty; }
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
            if (player.PlayerPartyAbbrev != seatedBefore) { Debug.Log($"SHOT: bills staged - {seatedBefore} re-seated; the pending bills stay before the chamber (their countdown is the chamber's, §630)"); player.PlayerPartyAbbrev = seatedBefore; }
            sim.ImpactLedger = stagingLedger;   // K51: the ledger watches the player's own verbs again

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

            // --- E1c / E1d (94*, 95*): the two challenge scenarios' frames left with the scenarios (GA-0-14 / GA-0-40, §825).

            // PF-12 (§598): THE FILM'S OWN COUNTRY, BACK. The Italy scenario that stood above seated Italy (until the scenarios were retired, §825), and Italy
            // has no regional count, so the night's pin looked for Sweden's night in Italy's game and logged "Sweden showed no election night" - the name was the
            // film's, the country was not. The pin puts the film's country back if anything has seated another (it cleared the finished scenarios too, so no
            // verdict screen stood over what follows). §718 (Elias's ruling of 2026-10-01, item 9: the signing screen's seam lines): it stood AFTER the signing ceremony, so
            // since §598 the ceremony was asked of Italy's game - whose division log held nothing to sign - and every real film logged two "canvas seam never
            // settled" lines at 89_signing_document and 89c_signing_rejected and captured the budget page under the ceremony's name. It now stands before it.
            var seatedField = controller.GetType().GetField("_playerCountry", BindingFlags.Instance | BindingFlags.NonPublic);
            if (seatedField?.GetValue(controller) is Country seated && seated.Id != _countryId)
            {
                Debug.Log($"SHOT: PF-12 - {seated.Id} is seated; the signing and election pins seat {_countryId} again, the selection's own way.");
                InvokeOneArg(controller, "SelectPlayerCountry", _countryId);
            }
            // (§825: the scenarios that seated another country and left a verdict standing are retired - the re-seat above stays as a guard.)
            // The signing and election pins stand on the desk, as they did when the re-seat put it back (SelectPlayerCountry seats the desk);
            // with nothing to re-seat, the sweep's last page (93b's Budget) would stand under them instead (§825's review, finding 1).
            SetPrivateField(controller, "_onDesk", true);
            // §718: the country seated again, read again - a seat re-taken may open its own book, and `player` above is the one the film began with
            Country filmed = seatedField?.GetValue(controller) as Country ?? player;

            // --- E2. THE SIGNING CEREMONY (Canvas screen 2) — pinned via the controller's own queue
            // method (ceremonies fire only from play's day tick, never from harness sim-advances, so
            // this pass stays clean; TriggerSigningForNewestDivision fills the same queue the day
            // tick fills). SignPendingDivision is the SIGN button's own method — the reflection call
            // exits the screen exactly like a click, the selector's idiom. §718: the passed division is
            // staged with content, as 89c's rejected one is - the film's own country's book may hold none.
            StageDivisionWithContent(filmed, sim, "Harness Test Bill (staged for the signing capture)", 30f, passed: true);
            InvokeNoArg(controller, "TriggerSigningForNewestDivision");
            yield return WaitForCanvasSettle(controller, wantActive: true);
            yield return Settle();
            Claim("signing");   // §718: the ceremony is filmed now - before it, these frames showed the budget page and claimed nothing
            yield return Capture("89_signing_document");
            RecordCanvasTextAssert("89_signing_document", controller);

            InvokeNoArg(controller, "SignPendingDivision");
            yield return null;
            yield return null;
            Claim("signing");   // §718: the seal mid-beat, still the signing board
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
            StageDivisionWithContent(filmed, sim, "Harness Test Bill (injected for rejected-signing coverage)", -30f, passed: false);   // P2-4.3: with sides and an estimate; §718 the seated country's
            InvokeNoArg(controller, "TriggerSigningForNewestDivision");
            yield return WaitForCanvasSettle(controller, wantActive: true);
            yield return Settle();
            Claim("signing");   // §718
            yield return Capture("89c_signing_rejected");
            RecordCanvasTextAssert("89c_signing_rejected", controller);

            InvokeNoArg(controller, "SignPendingDivision");
            yield return WaitForCanvasSettle(controller, wantActive: false);
            yield return Settle();

            // --- F. ELECTION NIGHT AS THE TAKEOVER, AND GAME OVER - through the controller's own path.
            // P2-0.2 (2026-09-02): the approval-threshold reveal this block used to force (a WIN and a LOSS
            // frame by writing approval) is retired with the rule. The election is election night's count
            // and the office verdict on its foot; a country without a live vote model shows no screen, and
            // this says so rather than filming the desk under the night's name (S-20). The film's own country was seated again before the signing (PF-12, §718).
            if (AdvanceToElectionTurn(sim, noDecisions))
            {
                InvokeNoArg(controller, "CheckElection");
                FieldInfo nightField = controller.GetType().GetField("_electionNight", BindingFlags.Instance | BindingFlags.NonPublic);
                if (nightField?.GetValue(controller) != null)
                {
                    yield return WaitForCanvasSettle(controller, wantActive: true);
                    yield return Settle();
                    Claim("electionnight");
                    yield return Capture("88n_election_night_takeover");
                    RecordCanvasTextAssert("88n_election_night_takeover", controller);

                    InvokeNoArg(controller, "DismissElectionNight");
                    yield return WaitForCanvasSettle(controller, wantActive: false);
                    yield return Settle();
                    FieldInfo overAfterNight = controller.GetType().GetField("_isGameOver", BindingFlags.Instance | BindingFlags.NonPublic);
                    bool over = overAfterNight?.GetValue(controller) is bool o && o;
                    yield return Capture(over ? "88b_game_over" : "88c_after_election_night");
                }
                else
                {
                    Debug.LogWarning($"SHOT: {_countryId} showed no election night after CheckElection - the takeover is NOT filmed for it (the controller logs why: ELECTION: / CANVAS: lines); -shotinterrupts is the film that requires one.");
                }
            }
            else
            {
                Debug.LogWarning($"SHOT: no election turn within {MaxStateSearchDays} days - election night stays unpinned for {_countryId}.");
            }
        }

        /// <summary>
        /// Advances sim days (turns through the driver's own no-decisions path, each day ticked via
        /// AdvanceCountryDayTick like play does) until CurrentTurn is an election turn, bounded by
        /// MaxStateSearchDays. False when the bound ran out — the caller states the unpinned state.
        /// </summary>
        private bool AdvanceToElectionTurn(SimulationManager sim, Dictionary<CountryId, PolicyDecision> noDecisions)
        {
            // PS-2 / CL-4 (§619): the election is the player's country's own POLLING DAY, flagged by the manager's AdvanceDay, no longer a turn
            // multiple - the day is ticked as play ticks it, then the controller's CheckElection is the caller's to invoke, as Update does.
            for (int days = 0; days < MaxStateSearchDays; days++)
            {
                if (sim.AdvanceDay())
                {
                    sim.AdvanceTurn(noDecisions);
                }

                sim.AdvanceCountryDayTick(_countryId);
                if (sim.PollingDayToday) { return true; }
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
            ScrollDeskTo(controller, "event");   // §726: the event card is the v3.5 desk's fourth row, below the fold
            yield return Settle();
            yield return Capture("01e_desk_event");
            ScrollDeskTo(controller, null);
            Debug.Log($"SHOT: 01e_desk_event - the card filled with the pool's own \"{pool[0].Name}\" (GDP {pool[0].GdpShockPercent:+0.0;-0.0}%, inflation {pool[0].InflationShockPoints:+0.0;-0.0} pts, approval {pool[0].ApprovalEffect:+0.0;-0.0}).");
            if (had) { lastEvents[_countryId] = previous; } else { lastEvents.Remove(_countryId); }
            yield return Settle();
        }

        /// <summary>
        /// PS-6 US-19: THE US ROLE CHIP'S OTHER WORDS, each staged for one capture and restored. The warm-up holds the 2024 count and seats the game's president,
        /// so the held desk (01d) films that president's party as PRESIDENT; these three film the words a US player meets out of it: 01d2 the same president
        /// read as the record's (THE PRESIDENT'S PARTY), 01d3 the other major party seated in the player's place (where it holds neither the presidency nor a
        /// chamber, MINORITY), 01d4 a president of the other party staged over the same Congress (where the player's party holds the House, HOUSE MAJORITY).
        /// Each prints the role it staged as `GovernmentRecord.RoleOn` reads it. The government record and the player's party are touched, and restored; the
        /// Fed chair's selection (which a staged party out of the government lifts) and the speed are snapshotted and restored with them, play's clock is held for
        /// the staged frames, and a hold that does not come back fails the run (the review's M1); another country, or a US world whose stage does not hold,
        /// captures nothing and says so.
        /// </summary>
        private IEnumerator CaptureDeskUsRoles(GameController controller)
        {
            var country = controller.GetType().GetField("_playerCountry", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(controller) as Country;
            var sim = controller.GetType().GetField("_simulationManager", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(controller) as SimulationManager;
            if (country == null || sim == null || country.Id != CountryId.USA) { yield break; }
            GovernmentRecord held = country.Government;
            string mine = country.PlayerPartyAbbrev;
            if (held == null || held.Kind != WorldClock.ExecutiveKind.Presidency || !Congress.Applies(country) || string.IsNullOrEmpty(mine))
            {
                Debug.LogWarning("SHOT: the US role chip could not be staged (no presidency, no Congress seated or no party) - 01d2-01d4 NOT captured.");
                yield break;
            }
            string other = null;
            foreach (KeyValuePair<string, int> seat in country.ParliamentSeats) { if (seat.Key != mine && (other == null || seat.Value > country.ParliamentSeats[other])) { other = seat.Key; } }
            string Role() => GovernmentRecord.RoleOn(country, country.PlayerPartyAbbrev, sim.CurrentDate).ToString();
            // the review's M1: a staged party out of the government lifts the Fed chair's selection - the chair is the government's to appoint, so the controller
            // nulls the candidates, and the hold the film rides on (an election eve) goes with them and nothing draws them again for this turn. So the selection,
            // its turn and the speed are snapshotted and restored, and play's clock is held for the staged frames (§510's idiom: primed an hour short of a day) so
            // no real-time day can pass under a staged government.
            const BindingFlags Own = BindingFlags.Instance | BindingFlags.NonPublic;
            FieldInfo candidatesField = controller.GetType().GetField("_fedChairCandidates", Own), forTurnField = controller.GetType().GetField("_fedChairCandidatesForTurn", Own);
            FieldInfo speedField = controller.GetType().GetField("_gameSpeed", Own), timerField = controller.GetType().GetField("_daySpeedTimer", Own);
            if (candidatesField == null || forTurnField == null || speedField == null || timerField == null)
            {
                Debug.LogError("SHOT: the US role chip's stage could not snapshot the Fed chair's selection or the clock (_fedChairCandidates/_fedChairCandidatesForTurn/_gameSpeed/_daySpeedTimer not found) - 01d2-01d4 NOT captured.");
                yield break;
            }
            object candidatesWas = candidatesField.GetValue(controller), forTurnWas = forTurnField.GetValue(controller), speedWas = speedField.GetValue(controller), timerWas = timerField.GetValue(controller);
            int candidatesBefore = (candidatesWas as ICollection)?.Count ?? 0;
            timerField.SetValue(controller, -ClockHoldSeconds);
            try
            {
                if (held.PmParty == mine)
                {
                    country.Government = GovernmentRecord.OfPresidency(held.Executive, held.PmParty, held.FormedOn, "of record", "staged (UiScreenshotDriver): the president in office read as the record's");
                    yield return Settle();
                    yield return Capture("01d2_desk_role_record");
                    Debug.Log($"SHOT: 01d2_desk_role_record - staged: {held.Executive} read as the record's president; {mine} reads {Role()}.");
                    country.Government = held;
                }
                if (other != null)
                {
                    country.PlayerPartyAbbrev = other;
                    yield return Settle();
                    yield return Capture("01d3_desk_role_other");
                    Debug.Log($"SHOT: 01d3_desk_role_other - staged: {other} seated in {mine}'s place under {held.Executive}; {other} reads {Role()} (House {Congress.HouseMajority(country) ?? "no majority"}, Senate {Congress.SenateMajority(country, sim.CurrentDate) ?? "no majority"}).");
                    country.PlayerPartyAbbrev = mine;
                    country.Government = GovernmentRecord.OfPresidency("a staged president (" + other + ")", other, held.FormedOn, "of record", "staged (UiScreenshotDriver): a president of the other party over the same Congress");
                    yield return Settle();
                    yield return Capture("01d4_desk_role_house");
                    Debug.Log($"SHOT: 01d4_desk_role_house - staged: a president of {other} over the same Congress; {mine} reads {Role()}.");
                }
            }
            finally
            {
                country.Government = held;
                country.PlayerPartyAbbrev = mine;
                candidatesField.SetValue(controller, candidatesWas);
                forTurnField.SetValue(controller, forTurnWas);
                speedField.SetValue(controller, speedWas);
                timerField.SetValue(controller, timerWas);
            }
            yield return Settle();
            int candidatesAfter = (candidatesField.GetValue(controller) as ICollection)?.Count ?? 0;
            object speedAfter = speedField.GetValue(controller);
            string restored = $"SHOT: 01d2-01d4 restored - {held.Executive}'s government and {mine} seated again; the Fed chair's candidates {candidatesAfter} (before the stage {candidatesBefore}), the speed {speedAfter} (before {speedWas})";
            if (candidatesAfter != candidatesBefore || !Equals(speedAfter, speedWas)) { Debug.LogError(restored + " - THE HOLD DID NOT RETURN."); }
            else { Debug.Log(restored + "."); }
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
            reasonField.SetValue(controller, $"Out of office at year {sim.CurrentTurn}: the chamber formed a government without {country.PlayerPartyAbbrev ?? "your party"}.");
            yield return Settle();
            yield return Capture("01f_desk_gameover");
            overField.SetValue(controller, wasOver);
            reasonField.SetValue(controller, wasReason);
            yield return Settle();
        }

        /// <summary>The review of wave/dcrisis, L6: true while CaptureDeskCrisis films its staged plate; the frames a REAL crisis's plate lay over, counted
        /// (and, review 2's N7, reported in the run's summary).</summary>
        private bool _stagingCrisis;
        private int _realCrisisPlateFrames;

        /// <summary>
        /// MF q33 on screen (D-WORKLIST §1, boards 32a-32e): THE DEBT CRISIS, STAGED. The player's country's crisis state is replaced, for these frames alone,
        /// by the one the model's entry into a crisis (SovereignCrisis.AdvanceDay) would write on today's review - the country's own branch (the USA defaults, every EU country takes a programme),
        /// the model's constants, the haircut and the output cost computed from today's debt and GDP, the entry deficit the rating's settled reading (for a
        /// default the stock IS written down by the haircut for the staged frames, as the model writes it, and put back after - the review's L5) - and then
        /// moved through its after-states by its own fields: (1) <c>01g_crisis_moment</c>, the plate on its day (32a / 32b), and <c>01g1_crisis_moment_sources</c>,
        /// the head's slip opened through the plate's film hook; Continue through the controller's own AcknowledgeCrisisPlate; (2)
        /// <c>01g2_crisis_desk</c>, 220 days on (the default's output cost with 145 days left, the cut so far at the last closed year's rate - 32c; the
        /// programme's first year running - 32d); for a programme (3) <c>01g3_crisis_desk_withheld</c>, year 2 with the loan withheld (32d's second state) and
        /// (4) <c>05u_budget_programme_target</c>, the Budget's target row under the sweep's own drafts (32e); then (5) <c>01g4_crisis_restored</c>, the day the
        /// market reopens (the strip's GOOD chip). Play's clock is held for the staged frames (§510's idiom), and the crisis state, the plate's
        /// acknowledgement, the page and the clock are put back after, whatever happens.
        /// </summary>
        private IEnumerator CaptureDeskCrisis(GameController controller)
        {
            const BindingFlags Own = BindingFlags.Instance | BindingFlags.NonPublic;
            var country = controller.GetType().GetField("_playerCountry", Own)?.GetValue(controller) as Country;
            var sim = controller.GetType().GetField("_simulationManager", Own)?.GetValue(controller) as SimulationManager;
            FieldInfo timerField = controller.GetType().GetField("_daySpeedTimer", Own);
            FieldInfo seenCountry = controller.GetType().GetField("_crisisPlateSeenCountry", Own), seenCrises = controller.GetType().GetField("_crisisPlateSeenCrises", Own);
            FieldInfo tabField = controller.GetType().GetField("_consolidatedTab", Own);
            MethodInfo holds = controller.GetType().GetMethod("CrisisPlateHolds", Own), acknowledge = controller.GetType().GetMethod("AcknowledgeCrisisPlate", Own);
            if (country == null || sim == null || timerField == null || seenCountry == null || seenCrises == null || tabField == null || holds == null || acknowledge == null)
            {
                Debug.LogError("SHOT: the debt crisis could not be staged (_playerCountry/_simulationManager/_daySpeedTimer/_crisisPlateSeen*/_consolidatedTab/CrisisPlateHolds/AcknowledgeCrisisPlate not found) - 01g-01g4 and 05u NOT captured.");
                _failed++;
                yield break;
            }
            SovereignCrisisState was = country.Crisis;
            object seenCountryWas = seenCountry.GetValue(controller), seenCrisesWas = seenCrises.GetValue(controller), timerWas = timerField.GetValue(controller), tabWas = tabField.GetValue(controller);
            DateTime today = sim.CurrentDate.Date;
            CrisisPhase branch = SovereignCrisis.BranchOf(country.Id);
            SovereignCrisisState staged = was != null ? was.Copy() : new SovereignCrisisState();
            staged.Phase = branch;
            staged.StartedOn = today;
            staged.Crises = (was?.Crises ?? 0) + 1;
            staged.TriggerRating = CreditRating.CCC;
            staged.TriggerReviewedOn = today;
            float cutPerDay = 0f;
            FiscalTurnReport year = sim.GetLastFiscalReport(country.Id);
            if (year != null)
            {
                float rest = Mathf.Max(0f, year.TotalSpending - year.InterestOnDebt);
                cutPerDay = Mathf.Clamp(rest - Mathf.Max(0f, year.Revenue - year.InterestOnDebt), 0f, rest) / 365f;
            }
            if (branch == CrisisPhase.Default)
            {
                float writtenOff = Mathf.Max(0f, country.State.GovernmentDebt) * SovereignCrisis.HaircutFraction;
                staged.HaircutBillions += writtenOff;
                staged.Haircuts = new List<DatedAmount>(staged.Haircuts ?? new List<DatedAmount>()) { new DatedAmount { Date = today, Billions = writtenOff } };
                staged.OutputCostPerDay = country.State.GDP * (SovereignCrisis.OutputCostPercent / 100f) / SovereignCrisis.OutputCostDays;
                staged.OutputCostDaysLeft = SovereignCrisis.OutputCostDays;
                staged.EndsOn = today.AddDays(Math.Round(SovereignCrisis.ExclusionYears * 365.25) - 1);
            }
            else
            {
                CreditRatingSystem.GetSettledPosition(country, today, out _, out float? deficit, out _);
                staged.EntryDeficitPercent = deficit ?? country.Rating.ReviewedDeficitPercent ?? AiFinanceMinistry.EuDeficitReferencePercent;
                staged.ProgrammeYear = 1;
                staged.NextReviewOn = today.AddYears(1);
                staged.WithheldThrough = DateTime.MinValue;
                staged.EndsOn = today.AddYears(SovereignCrisis.ProgrammeYears).AddDays(-1);
            }
            float debtWas = country.State.GovernmentDebt;
            object budgetScrollWas = controller.GetType().GetField("_budgetProcessCenterScrollPosition", Own)?.GetValue(controller);
            timerField.SetValue(controller, -ClockHoldSeconds);
            try
            {
                _stagingCrisis = true;
                country.Crisis = staged;
                // the review of wave/dcrisis, L5: the haircut is written off the stock as the model writes it on the day (put back after), so the plate's
                // debt-to-GDP and its stock before and after are play's
                if (branch == CrisisPhase.Default) { country.State.GovernmentDebt = debtWas - staged.Haircuts[staged.Haircuts.Count - 1].Billions; }
                SetPrivateField(controller, "_onDesk", true);
                ScrollDeskTo(controller, null);   // the crisis's card is the Desk's first row
                UiGuardContext.CurrentScreen = "01g_crisis_moment";   // the films of e15a0eb4: the staged frames settle under their own name, never the frame before (01f)
                yield return Settle();
                yield return Settle();
                bool owed = (bool)holds.Invoke(controller, null);
                yield return Capture("01g_crisis_moment");
                if (owed) { Debug.Log($"SHOT: 01g_crisis_moment - staged: {country.Id} loses market access today ({today:yyyy-MM-dd}), {branch} - the plate owed and holding the clock."); }
                else { Debug.LogError("SHOT: 01g_crisis_moment - the staged crisis owes NO plate (CrisisPlateHolds false): the frame is not the moment."); _failed++; }
                // L2: the head's slip with its sources, opened through the plate's own film hook
                if (SetPrivateField(controller, "_crisisSlipForFilm", "plate:head"))
                {
                    UiGuardContext.CurrentScreen = "01g1_crisis_moment_sources";   // the films of e15a0eb4: the staged frames settle under their own name, never the frame before (01f)
                    yield return Settle();
                    yield return Capture("01g1_crisis_moment_sources");
                    SetPrivateField(controller, "_crisisSlipForFilm", null);
                }
                else { Debug.LogError("SHOT: 01g1_crisis_moment_sources - the plate has no _crisisSlipForFilm hook; NOT captured."); _failed++; }
                acknowledge.Invoke(controller, null);
                if ((bool)holds.Invoke(controller, null)) { Debug.LogError("SHOT: 01g_crisis_moment - Continue did not release the plate."); _failed++; }

                // the after-state 220 days on: the crisis's own fields moved as the model's day would move them
                const int on = 220;
                staged.StartedOn = today.AddDays(-on);
                if (branch == CrisisPhase.Default)
                {
                    staged.EndsOn = staged.StartedOn.AddDays(Math.Round(SovereignCrisis.ExclusionYears * 365.25) - 1);
                    staged.OutputCostDaysLeft = SovereignCrisis.OutputCostDays - on;
                    staged.Haircuts[staged.Haircuts.Count - 1] = new DatedAmount { Date = staged.StartedOn, Billions = staged.Haircuts[staged.Haircuts.Count - 1].Billions };
                    staged.CutBillions = (was?.CutBillions ?? 0f) + cutPerDay * on;
                }
                else
                {
                    staged.NextReviewOn = staged.StartedOn.AddYears(1);
                    staged.EndsOn = staged.StartedOn.AddYears(SovereignCrisis.ProgrammeYears).AddDays(-1);
                }
                UiGuardContext.CurrentScreen = "01g2_crisis_desk";   // the films of e15a0eb4: the staged frames settle under their own name, never the frame before (01f)
                yield return Settle();
                yield return Capture("01g2_crisis_desk");
                Debug.Log($"SHOT: 01g2_crisis_desk - staged {on} days into the crisis: {(branch == CrisisPhase.Default ? $"the exclusion through {staged.EndsOn:yyyy-MM-dd}, the output cost {staged.OutputCostDaysLeft} days left, the cut so far {staged.CutBillions:0.#} bn at the last closed year's rate" : $"programme year 1, the entry deficit {staged.EntryDeficitPercent:0.0} %, the review {staged.NextReviewOn:yyyy-MM-dd}")}.");

                if (branch == CrisisPhase.Programme)
                {
                    // 32d's second state: year 1 missed at its review, year 2's loan withheld
                    staged.StartedOn = today.AddDays(-(365 + 40));
                    staged.ProgrammeYear = 2;
                    staged.TargetsMissed = (was?.TargetsMissed ?? 0) + 1;
                    staged.WithheldThrough = staged.StartedOn.AddYears(2).AddDays(-1);
                    staged.NextReviewOn = staged.StartedOn.AddYears(2);
                    staged.EndsOn = staged.StartedOn.AddYears(SovereignCrisis.ProgrammeYears).AddDays(-1);
                    staged.CutBillions = (was?.CutBillions ?? 0f) + cutPerDay * 40;
                    UiGuardContext.CurrentScreen = "01g3_crisis_desk_withheld";   // the films of e15a0eb4: the staged frames settle under their own name, never the frame before (01f)
                    yield return Settle();
                    yield return Capture("01g3_crisis_desk_withheld");
                    Debug.Log($"SHOT: 01g3_crisis_desk_withheld - staged: year 1 missed, the loan withheld through {staged.WithheldThrough:yyyy-MM-dd}, the cut so far {staged.CutBillions:0.#} bn.");

                    // 32e: the Budget's target row, year 1, under the sweep's own drafts
                    staged.StartedOn = today.AddDays(-on);
                    staged.ProgrammeYear = 1;
                    staged.TargetsMissed = was?.TargetsMissed ?? 0;
                    staged.WithheldThrough = DateTime.MinValue;
                    staged.NextReviewOn = staged.StartedOn.AddYears(1);
                    staged.EndsOn = staged.StartedOn.AddYears(SovereignCrisis.ProgrammeYears).AddDays(-1);
                    SetPrivateField(controller, "_onDesk", false);
                    SetEnumField(controller, "_consolidatedTab", "Budget");
                    SetPrivateField(controller, "_budgetProcessCenterScrollPosition", Vector2.zero);   // L5: the strip and its target row are the page's head
                    UiGuardContext.CurrentScreen = "05u_budget_programme_target";   // the films of e15a0eb4: the staged frames settle under their own name, never the frame before (01f)
                    yield return Settle();
                    yield return Settle();
                    yield return Capture("05u_budget_programme_target");
                    Debug.Log($"SHOT: 05u_budget_programme_target - staged: programme year 1, the target {SovereignCrisis.TargetFor(staged.EntryDeficitPercent, 1):0.0} % of GDP on the balance strip.");
                    SetPrivateField(controller, "_onDesk", true);
                }

                // the market reopens: the crisis ended yesterday (the model ends it on the first day past EndsOn)
                staged.Phase = CrisisPhase.None;
                staged.WithheldThrough = DateTime.MinValue;
                staged.EndsOn = today.AddDays(-1);
                UiGuardContext.CurrentScreen = "01g4_crisis_restored";   // the films of e15a0eb4: the staged frames settle under their own name, never the frame before (01f)
                yield return Settle();
                yield return Capture("01g4_crisis_restored");
                Debug.Log($"SHOT: 01g4_crisis_restored - staged: the {(branch == CrisisPhase.Default ? "exclusion" : "programme")} ended {staged.EndsOn:yyyy-MM-dd}; the strip reads Market access restored today.");
            }
            finally
            {
                country.Crisis = was;
                country.State.GovernmentDebt = debtWas;
                if (budgetScrollWas != null) { SetPrivateField(controller, "_budgetProcessCenterScrollPosition", budgetScrollWas); }
                SetPrivateField(controller, "_hasCachedPreview", false);   // L5: no preview computed over the staged crisis survives it
                SetPrivateField(controller, "_crisisSlipForFilm", null);
                _stagingCrisis = false;
                seenCountry.SetValue(controller, seenCountryWas);
                seenCrises.SetValue(controller, seenCrisesWas);
                tabField.SetValue(controller, tabWas);
                SetPrivateField(controller, "_onDesk", true);
                timerField.SetValue(controller, timerWas);
            }
            yield return Settle();
            Debug.Log($"SHOT: 01g-01g4 restored - {country.Id}'s crisis state put back ({(country.Crisis == null ? "none" : country.Crisis.Phase.ToString())}), the plate's acknowledgement and the clock as they were.");
        }

        /// <summary>
        /// §710 (R-SP5 retired, Elias's ruling of 2026-10-01): after the map's capture, what its renderer laid down - the six chips, the tag
        /// each one's label, no two overlapping - is asserted (R-SP5's label-separation assert retired with the names it measured). A miss is an
        /// error, so the run's fold turns red on it.
        /// </summary>
        private static void AssertMapChips(GameController controller, string screen)
        {
            FieldInfo field = controller.GetType().GetField("_mapRenderer", BindingFlags.Instance | BindingFlags.NonPublic);
            if (!(field?.GetValue(controller) is MapRenderer map))
            {
                Debug.LogError("SHOT: _mapRenderer not found - the map's chip assert VERIFIED NOTHING.");
                return;
            }

            var rects = new List<Rect>(map.LastChipRects.Values);
            int overlapping = 0;
            for (int i = 0; i < rects.Count; i++) { for (int j = i + 1; j < rects.Count; j++) { if (rects[i].Overlaps(rects[j])) { overlapping++; } } }
            Debug.Log($"SHOT: map chips on {screen} - {rects.Count} chip(s), the tag each one's label, smallest gap {map.LastMinChipSeparation:F1} px, {overlapping} overlapping pair(s).");
            if (rects.Count != 6 || overlapping > 0)
            {
                Debug.LogError($"SHOT: MAP CHIPS on {screen}: {rects.Count} drawn, {overlapping} overlapping pair(s) - six chips, none overlapping, is the map.");
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
        /// <see cref="CampaignLegality.LegalActions"/>; the economy reading (§752: over the term, what the election scores) is
        /// <see cref="EconomicVote.RecordTermIndex"/> read off the LIVE warmed-up country. The war
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
        /// <summary>
        /// **F1's last proof: board 1h drawn from the MODEL'S OWN COUNT, filmed.**
        ///
        /// <para>⚠ Every election-night film before this one was of `ElectionNightFilm`'s staged fixture.
        /// A fixture exercises the SCREEN and says nothing about the model - it would render identically
        /// if the simulation produced nothing at all, which for board 1h was the literal situation.</para>
        ///
        /// <para>It predicts through the live path and builds through `ElectionNightFromModel`, the same
        /// two calls `GameController.ShowElectionNight` makes, so the frame is of the thing a player
        /// reaches rather than of a rehearsal of it.</para>
        /// </summary>
        /// <summary>Election night items 1-3 (2026-09-10): what a filmed night SAID, one line - each call with the count it landed
        /// at, the map's tiles by ladder level, and the government formed - so a film is read against its log, not only its frame.</summary>
        private static void LogNight(string stem, NightState state, string[] parties, ElectionNightScreen screen, GovernmentFormation.View government)
        {
            var calls = new System.Text.StringBuilder();
            foreach (ElectionCall call in state.Calls)
            {
                calls.Append(call.Kind).Append(call.Party >= 0 ? ":" + parties[call.Party] : ":" + call.Bloc)
                    .Append("@").Append(call.DeclaredAt.ToString(CultureInfo.InvariantCulture)).Append(' ');
            }
            string map = screen?.Map == null ? "no map"
                : string.Format(CultureInfo.InvariantCulture, "map {0} full / {1} compact / {2} minimal", screen.Map.LastFull, screen.Map.LastCompact, screen.Map.LastMinimal);
            string governs = government == null ? "no formation"
                : government.HasGovernment
                    ? string.Format(CultureInfo.InvariantCulture, "{0}: cabinet {1} ({2} seats), support {3}, opposed {4} of majority {5}",
                        government.Outcome, string.Join("+", government.Cabinet.ConvertAll(c => c.Abbrev)), government.CabinetSeats,
                        string.Join("+", government.Support.ConvertAll(c => c.Abbrev)), government.OpposedSeats, government.Majority)
                    : "no government: " + government.Reason;
            Debug.Log($"SHOT: NIGHT {stem} - {state.DeclaredCount} of {state.TotalConstituencies} declared; calls {calls}; {map}; {governs}");
        }

        private IEnumerator CaptureElectionNightFromModel(GameController controller)
        {
            // PS-3k (§638): the model's night reads the government's record as play's polling day does with no campaign - the loaded or warmed game's
            // Sweden, its record over the term today (§752); a harness with no game in hand predicts without it, and says so.
            var modelSim = controller.GetType().GetField("_simulationManager", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(controller) as SimulationManager;
            Country modelCountry = modelSim?.World?.GetCountry(CountryId.Sweden);
            Dictionary<string, double> modelRecord = modelCountry != null ? EconomicVote.RecordOverTerm(modelCountry, modelSim.CurrentDate, out _) : null;   // §752 (A1): over the term
            Debug.Log(modelRecord == null ? "SHOT: F1 - no game in hand, the model's night is predicted WITHOUT the government's record." : "SHOT: F1 - the model's night reads the government's record: " + string.Join(", ", System.Linq.Enumerable.Select(modelRecord, kv => kv.Key + " " + (kv.Value * 100.0).ToString("+0.00;-0.00", CultureInfo.InvariantCulture) + " pp")));
            if (!NationalElection.TryPredictShares(CountryId.Sweden, out _, modelRecord))
            {
                Debug.LogError("SHOT: F1 - Sweden could not be predicted, so the model frame is NOT filmed. A missing frame is reported; a fixture standing in for it would not be.");
                _failed++;
                yield break;
            }

            var keys = new List<string>();
            foreach (PoliticalParty party in PartySystems.For(CountryId.Sweden))
            {
                if (party.HasPosition) { keys.Add(party.Abbrev); }
            }

            NightState modelState = ElectionNightFromModel.At(
                ElectionNightFromModel.FinalMinute, CountryId.Sweden, keys, 349, 0.04);
            if (modelState == null)
            {
                Debug.LogError("SHOT: F1 - the model produced no night state; the frame is NOT filmed.");
                _failed++;
                yield break;
            }

            // Election night items 1-3 (2026-09-10): the same inputs the played game gives the board - the seated election's
            // count per valkrets as the previous election (2026's since K-1), its seats, and the formation run on this count's
            // chamber; dated the game's first election, the boundary the live campaign runs to (CL-5). No player's party: the
            // harness predicts the country, it does not play a party in it.
            var seeded = PartySystems.For(CountryId.Sweden);
            var previousSeats = new int[keys.Count];
            for (int k = 0; k < keys.Count; k++)
            {
                foreach (PoliticalParty party in seeded) { if (party.Abbrev == keys[k]) { previousSeats[k] = party.SeedSeats; break; } }
            }
            GovernmentFormation.View modelGovernment = GovernmentFormation.ViewOf(CountryId.Sweden, keys, modelState.SeatsOnCounted, null);

            PoliSim.Testing.CaptureIdentity.CanvasSurface = "electionnight";
            ElectionNightScreen modelScreen = ElectionNightScreen.Build(
                modelState, keys.ToArray(), "SWEDEN", PoliSim.Elections.WorldClock.LatestElectionDay(CountryId.Sweden).AddHours(20), 349, previousLabel: "SWEDEN 2026",   // PS-2 (§619): the fixture's night is dated the real polling day
                previousByConstituency: SwedishRegions.PreviousVotes(keys), previousSeats: previousSeats,
                government: modelGovernment, inkCountry: CountryId.Sweden);
            if (modelScreen == null)
            {
                Debug.LogError("SHOT: F1 - the board did not build for the model frame; nothing filmed.");
                _failed++;
                yield break;
            }

            yield return Settle();
            Claim("electionnight");
            yield return Capture("e6_election_night_model");
            RecordCanvasTextAssert("e6_election_night_model", controller);   // §648: the staged nights carry the Canvas guard too (the synthetic weight among it)

            Debug.Log(string.Format(CultureInfo.InvariantCulture,
                "SHOT: F1 - board 1h filmed from the MODEL, {0} constituencies declared of {1}.",
                modelState.DeclaredCount, modelState.TotalConstituencies));
            LogNight("model", modelState, keys.ToArray(), modelScreen, modelGovernment);

            if (modelScreen.Root != null) { UnityEngine.Object.Destroy(modelScreen.Root); }
            yield return null;

            yield return CaptureNightColumn(controller, modelSim, modelCountry, modelState, keys, previousSeats);
        }

        /// <summary>
        /// D-NIGHT (the review's M3, THE CANVAS FILM RULE): the redrawn column on film where the fixtures above cannot show it -
        /// (a) the model's Swedish night with the column the PROCEDURE projects (`SimulationManager.ProjectFormation` on the model night's chamber: the
        /// round's cells, each party's side), not `GovernmentFormation.ViewOf`'s aggregate fallback - the live country's seats swapped for the model night's
        /// for the projection's one call and put back at once (a projection moves no state, FormationProjectionDiagnostic); (b) that night with the † view open
        /// (`SetDense(true)`, restored); (c) Italy's column STAGED twice - a government expected with support, and none expected - on the seated chambers'
        /// seats, a one-region count, the sides by a stated staging rule (a layout fixture: no Italian count is filmed here, and no Italian night is reachable
        /// in this film's game).
        /// </summary>
        private IEnumerator CaptureNightColumn(GameController controller, SimulationManager sim, Country sweden, NightState state, List<string> keys, int[] previousSeats)
        {
            if (sim == null || sweden == null)
            {
                Debug.LogError("SHOT: D-NIGHT - no game in hand, so the projected column (e6_election_night_projected, e6_election_night_dagger) is NOT filmed.");
                _failed++;
                yield break;
            }
            FormationProjection projected = null;
            var savedSeats = new Dictionary<string, int>(sweden.ParliamentSeats);
            try
            {
                sweden.ParliamentSeats.Clear();
                for (int k = 0; k < keys.Count; k++) { sweden.ParliamentSeats[keys[k]] = state.SeatsOnCounted[k]; }
                projected = sim.ProjectFormation(sweden, WorldClock.LatestElectionDay(CountryId.Sweden));
            }
            catch (Exception e) { Debug.LogError($"SHOT: D-NIGHT - the projection THREW ({e.GetType().Name}: {e.Message}); the projected column is NOT filmed."); }
            finally
            {
                sweden.ParliamentSeats.Clear();
                foreach (KeyValuePair<string, int> kv in savedSeats) { sweden.ParliamentSeats[kv.Key] = kv.Value; }
            }
            GovernmentFormation.View view = projected?.View;
            if (view == null || view.Votes.Count == 0 || view.Round.Count == 0)
            {
                Debug.LogError("SHOT: D-NIGHT - the projection carried no vote or no round" + (projected != null ? " (" + projected.Describe() + ")" : string.Empty) + "; the projected column is NOT filmed.");
                _failed++;
                yield break;
            }
            Debug.Log("SHOT: D-NIGHT - the projected column: " + projected.Describe());

            PoliSim.Testing.CaptureIdentity.CanvasSurface = "electionnight";
            ElectionNightScreen screen = ElectionNightScreen.Build(state, keys.ToArray(), "SWEDEN", WorldClock.LatestElectionDay(CountryId.Sweden).AddHours(20), 349, previousLabel: "SWEDEN 2026",
                previousByConstituency: SwedishRegions.PreviousVotes(keys), previousSeats: previousSeats, government: view, inkCountry: CountryId.Sweden,
                ledger: sim.PlayerCampaignLedger, ledgerParty: sim.PlayerCampaignLedgerParty);
            if (screen == null) { Debug.LogError("SHOT: D-NIGHT - the board did not build for the projected column; nothing filmed."); _failed++; yield break; }
            yield return Settle();
            Claim("electionnight");
            yield return Capture("e6_election_night_projected");
            RecordCanvasTextAssert("e6_election_night_projected", controller);
            LogNight("projected", state, keys.ToArray(), screen, view);

            screen.SetDense(true);
            yield return Settle();
            Claim("electionnight");
            yield return Capture("e6_election_night_dagger");
            RecordCanvasTextAssert("e6_election_night_dagger", controller);
            if (!screen.DenseShown) { Debug.LogError("SHOT: e6_election_night_dagger - the † view did not open."); _failed++; }
            screen.SetDense(false);
            if (screen.Root != null) { UnityEngine.Object.Destroy(screen.Root); }
            yield return Settle();

            Country italy = sim.World?.GetCountry(CountryId.Italy);
            if (italy == null || italy.ParliamentSeats == null || italy.ParliamentSeats.Count == 0)
            {
                Debug.LogError("SHOT: D-NIGHT - the game holds no seated Italian chamber; Italy's staged column is NOT filmed.");
                _failed++;
                yield break;
            }
            foreach (bool expected in new[] { true, false })
            {
                string stem = expected ? "e6_election_night_italy_support_staged" : "e6_election_night_italy_none_staged";
                GovernmentFormation.View staged = StagedItalianView(italy, expected, out List<string> italianKeys);
                NightState italianState = StagedItalianCount(italy, italianKeys);
                PoliSim.Testing.CaptureIdentity.CanvasSurface = "electionnight";
                ElectionNightScreen it = ElectionNightScreen.Build(italianState, italianKeys.ToArray(), "ITALY  ·  STAGED", WorldClock.LatestElectionDay(CountryId.Italy).AddHours(23), StagedSeats(italy),
                    government: staged, inkCountry: CountryId.Italy);   // review 2's N7: STAGED on the masthead; no ledger (the game holds no Italian campaign), so Why says so rather than drawing another country's
                if (it == null) { Debug.LogError("SHOT: " + stem + " - the board did not build; nothing filmed."); _failed++; continue; }
                yield return Settle();
                Claim("electionnight");
                yield return Capture(stem);
                RecordCanvasTextAssert(stem, controller);
                LogNight(stem, italianState, italianKeys.ToArray(), it, staged);
                if (it.Root != null) { UnityEngine.Object.Destroy(it.Root); }
                yield return Settle();
            }
        }

        private static int StagedSeats(Country italy) { int n = 0; foreach (int s in italy.ParliamentSeats.Values) { n += s; } return n; }

        /// <summary>D-NIGHT: Italy's seated Camera as a one-region count - each party's seats as its votes (x 1 000), declared at once. A layout fixture.</summary>
        private static NightState StagedItalianCount(Country italy, List<string> keys)
        {
            var votes = new long[keys.Count];
            long valid = 0;
            for (int k = 0; k < keys.Count; k++) { votes[k] = 1000L * italy.ParliamentSeats[keys[k]]; valid += votes[k]; }
            int seats = 0;
            foreach (int s in italy.ParliamentSeats.Values) { seats += s; }
            return ElectionNight.At(0, new[] { "ITALIA" }, new[] { votes }, new[] { valid }, new[] { valid + valid / 3 }, new[] { 0 }, seats, 0.03, keys.ToArray());
        }

        /// <summary>
        /// D-NIGHT: Italy's column STAGED (the staging rule, stated - a layout fixture, never a projection): the seated chambers' parties by Camera seats, largest
        /// first. EXPECTED WITH SUPPORT - the cabinet the largest parties while under two fifths of the Camera, the next party from outside, then more from outside
        /// until the for side carries in both chambers; every other party against; mandate 1 current. NONE EXPECTED - three mandates refused, the last the
        /// third-largest party alone, every other party against it. Each chamber's line is art. 64's: against + 1. The player's party is the largest's, read by
        /// the AI's rule (K119), so the tallest rows are drawn.
        /// </summary>
        private static GovernmentFormation.View StagedItalianView(Country italy, bool expected, out List<string> keys)
        {
            IReadOnlyDictionary<string, int> camera = italy.ParliamentSeats;
            IReadOnlyDictionary<string, int> senato = italy.SecondChamberSeats != null && italy.SecondChamberSeats.Count > 0 ? italy.SecondChamberSeats : italy.ParliamentSeats;
            keys = new List<string>();
            foreach (KeyValuePair<string, int> kv in camera) { if (kv.Value > 0) { keys.Add(kv.Key); } }
            keys.Sort((a, b) => camera[b].CompareTo(camera[a]));
            int total = 0;
            foreach (string k in keys) { total += camera[k]; }
            var cabinet = new List<string>();
            var support = new List<string>();
            if (expected)
            {
                int cum = 0;
                foreach (string k in keys) { if (cabinet.Count == 0 || cum + camera[k] < total * 2 / 5) { cabinet.Add(k); cum += camera[k]; } else { break; } }
                bool Carries(IReadOnlyDictionary<string, int> chamber)
                {
                    int f = 0, a = 0;
                    foreach (KeyValuePair<string, int> kv in chamber) { if (cabinet.Contains(kv.Key) || support.Contains(kv.Key)) { f += kv.Value; } else { a += kv.Value; } }
                    return f > a;
                }
                foreach (string k in keys)
                {
                    if (cabinet.Contains(k)) { continue; }
                    if (support.Count > 0 && Carries(camera) && Carries(senato)) { break; }
                    support.Add(k);
                }
            }
            else { cabinet.Add(keys[Math.Min(2, keys.Count - 1)]); }

            var view = new GovernmentFormation.View
            {
                HasGovernment = expected, Outcome = expected ? CoalitionOutcomeKind.ConfidenceAndSupply : CoalitionOutcomeKind.NewElection, TotalSeats = total, Majority = total / 2 + 1,
                NegativeRule = false, DeclarationsSourced = DeclaredRedLines.IsSourced(CountryId.Italy), PlayerParty = keys[0], ReadsPlayersAnswer = true, ProjectionVote = "BOTH CHAMBERS VOTE CONFIDENCE",
                ProjectionStep = expected ? "MANDATE 1 OF AT MOST 3 · CONFIDENCE OF BOTH CHAMBERS (STAGED)" : null, RoundLimit = 3,
                NoneWords = expected ? null : "THE MANDATES FAIL - DISSOLUTION", Reason = expected ? null : "three mandates fail - the chambers are dissolved (art. 88) - staged",
            };
            foreach (string k in cabinet) { view.Cabinet.Add((k, camera[k])); view.CabinetSeats += camera[k]; }
            foreach (string k in support) { view.Support.Add((k, camera[k])); }
            view.PlayerInCabinet = cabinet.Contains(keys[0]);
            view.PlayerSupports = support.Contains(keys[0]);
            if (expected)
            {
                view.Round.Add(new GovernmentFormation.RoundCell(cabinet[0], GovernmentFormation.RoundCellState.Current));
                view.Round.Add(new GovernmentFormation.RoundCell(null, GovernmentFormation.RoundCellState.NotReached));
                view.Round.Add(new GovernmentFormation.RoundCell(null, GovernmentFormation.RoundCellState.NotReached));
            }
            else
            {
                view.Round.Add(new GovernmentFormation.RoundCell(keys[0], GovernmentFormation.RoundCellState.Refused));
                view.Round.Add(new GovernmentFormation.RoundCell(keys.Count > 1 ? keys[1] : keys[0], GovernmentFormation.RoundCellState.Refused));
                view.Round.Add(new GovernmentFormation.RoundCell(cabinet[0], GovernmentFormation.RoundCellState.LastRefused));
            }
            foreach ((string name, IReadOnlyDictionary<string, int> chamber) in new[] { ("Camera", camera), ("Senato", senato) })
            {
                var vote = new GovernmentFormation.NightVote { Chamber = name };
                foreach (KeyValuePair<string, int> kv in chamber)
                {
                    if (kv.Value <= 0) { continue; }
                    CoalitionFormation.InvestitureSide side = cabinet.Contains(kv.Key) ? CoalitionFormation.InvestitureSide.InCabinet : support.Contains(kv.Key) ? CoalitionFormation.InvestitureSide.Supports : CoalitionFormation.InvestitureSide.Against;
                    vote.Sides.Add(new GovernmentFormation.VoteSide { Abbrev = kv.Key, Seats = kv.Value, Side = side, Reason = "staged" });
                    vote.Members += kv.Value;
                    if (side == CoalitionFormation.InvestitureSide.Against) { vote.Against += kv.Value; } else { vote.For += kv.Value; }
                }
                vote.Needed = vote.Against + 1;
                vote.Carried = vote.For >= vote.Needed;
                view.Votes.Add(vote);
                if (name == "Camera") { view.SupportedSeats = vote.For; view.OpposedSeats = vote.Against; }
            }
            return view;
        }

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
            // ⚠ F1 (2026-09-01): THE FIFTH FRAME IS THE MODEL'S OWN COUNT, not the staged fixture.
            // The four below are `ElectionNightFilm`'s staged night - a fixture, chosen to exercise the
            // board at four instants. **A fixture proves the SCREEN and says nothing about the MODEL**,
            // and every election-night film ever taken was of a fixture. This one predicts Sweden's
            // election through the live path and films what the model actually produced, so "board 1h
            // filmed" stops being a claim about furniture.
            yield return CaptureElectionNightFromModel(controller);

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
                // Items 1-3 (2026-09-10): the staged fixture IS Sweden 2022, so its comparison stays 2018 (sourced, national, on
                // the complete count only); the formation is run on the fixture's chamber once it is complete - with 2022's
                // declarations (K-1: the seated ones are 2026's, and 2022's count on them forms a government 2022 never saw).
                GovernmentFormation.View stagedGovernment = state.Complete
                    ? GovernmentFormation.ViewOf(CountryId.Sweden, parties, state.SeatsOnCounted, null, ElectionVintage.Sweden2022)
                    : null;
                PoliSim.Testing.CaptureIdentity.CanvasSurface = "electionnight";
                ElectionNightScreen screen = ElectionNightScreen.Build(state, parties, "SWEDEN",
                    new DateTime(2026, 9, 13, 20, 0, 0), 349,
                    previousVotes: ElectionNightFilm.Votes2018, previousLabel: "SWEDEN 2018",
                    government: stagedGovernment, inkCountry: CountryId.Sweden);
                if (screen == null)
                {
                    Debug.LogError("SHOT: W-E6 - the board did not build (furniture missing); nothing filmed.");
                    yield break;
                }

                yield return Settle();
                Claim("electionnight");
                yield return Capture("e6_election_night_" + stems[i]);
                RecordCanvasTextAssert("e6_election_night_" + stems[i], controller);

                Debug.Log(string.Format(CultureInfo.InvariantCulture,
                    "SHOT: W-E6 {0} - minute {1}, {2} of {3} declared, {4:N0} votes counted, {5} call(s) safe.",
                    stems[i], minute, state.DeclaredCount, state.TotalConstituencies, state.CountedValid, state.Calls.Count));
                LogNight(stems[i], state, parties, screen, stagedGovernment);

                // Destroy, not DestroyImmediate: this runs in PLAY mode, where the immediate form tears a
                // Canvas child out mid-frame and the run ends without reaching its own exit.
                if (screen.Root != null) { UnityEngine.Object.Destroy(screen.Root); }
                yield return Settle();

                // Premise 9 (§656): the final count once more with the player's party asked first - the Speaker's line the game writes and the
                // control to the formation sheet, filmed where the Canvas draws them.
                if (stems[i] == "final")
                {
                    PoliSim.Testing.CaptureIdentity.CanvasSurface = "electionnight";
                    // §685 (board 21d): the foot as the game builds it - the first-asked party's mark (the staged count's largest), YOUR PARTY, the
                    // caretaker stamp and the brass control; the sentence on its slip.
                    int largest = 0;
                    for (int p = 1; p < parties.Length; p++) { if (state.SeatsOnCounted[p] > state.SeatsOnCounted[largest]) { largest = p; } }
                    ElectionNightScreen first = ElectionNightScreen.Build(state, parties, "SWEDEN", new DateTime(2026, 9, 13, 20, 0, 0), 349,
                        previousVotes: ElectionNightFilm.Votes2018, previousLabel: "SWEDEN 2018", verdict: ElectionNightScreen.SpeakerLine(true, null),
                        government: stagedGovernment, inkCountry: CountryId.Sweden, continueLabel: ElectionNightScreen.ContinueToSheet,
                        speaker: new ElectionNightScreen.SpeakerFoot { Country = CountryId.Sweden, FirstAsked = parties[largest], PlayerFirst = true, Sentence = ElectionNightScreen.SpeakerLine(true, null) });
                    yield return Settle();
                    Claim("electionnight");
                    yield return Capture("e6_election_night_player_first");
                    RecordCanvasTextAssert("e6_election_night_player_first", controller);
                    bool control = first?.Root != null && Array.Exists(first.Root.GetComponentsInChildren<UnityEngine.UI.Text>(), x => x.text == ElectionNightScreen.ContinueToSheet);
                    if (!control) { Debug.LogError("SHOT: e6_election_night_player_first - the control to the formation sheet is not on the board (premise 9)."); _failed++; }
                    else { Debug.Log("SHOT: premise 9 - the board ends with the Speaker's line and the control '" + ElectionNightScreen.ContinueToSheet + "'."); }
                    if (first?.Root != null) { UnityEngine.Object.Destroy(first.Root); }
                    yield return Settle();
                }
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

        /// <summary>P2-0.2 / P2-0.3 (2026-09-02): `-shotinterrupts` plays ONE country's game through the
        /// controller's own day path to its interrupts and films each with its held clock proven - the
        /// campaign's opening (P2-0.3) and election night's takeover (P2-0.2). Set from the command line;
        /// the run ends here.</summary>
        public bool Interrupts;
        /// <summary>§707 (`-shotformation`, with `-shotinterrupts`): after the night, the game played on to its formation - the round opened, the
        /// sheet where the player's party is asked, the ballot's eve, the government installed.</summary>
        public bool Formation;
        /// <summary>PS-6 US-36 (`-shotusjourney`): a US game filmed from its start through the 2024 count, the oath, the bills, the first budget, the debt
        /// limit's episode and the 2026 midterm (`CaptureUsJourney`). The run ends there.</summary>
        public bool UsJourney;
        /// <summary>PS-8 (K163; M4 of the s938 review; `-shotabroad`): a game played from its start, day by day, to the day after the AI USA's first count (3 Nov
        /// 2026 from Sweden's start), the Docket filmed with the election abroad listed and then with its results card open (`CaptureAbroad`). The run ends there.</summary>
        public bool Abroad;
        /// <summary>PS-6 US-36 (`-shotwinner=DEM|REP`, with `-shotusjourney`): the party the journey's 2024 count is staged to elect.</summary>
        public string UsJourneyWinner = "";

        /// <summary>Frames the clock is watched for at VeryFast (0.25 s per day): long enough that a running
        /// clock moves several days, so "no day passed" is a held clock and not a slow frame.</summary>
        private const int HeldWatchFrames = 120;

        /// <summary>
        /// The interrupts, filmed from a played game - never staged. The sim is driven day by day through the
        /// controller-shaped path (AdvanceDay, the player's AdvanceCountryDayTick, AdvanceTurn on the boundary),
        /// so the campaign begins, runs and is counted exactly as play makes it; the controller's OWN Update is
        /// then let run at VeryFast and the date watched: an interrupt is proven by a date that does not move
        /// with the clock set to run, and its dismissal by a date that moves again. ⚠ A missing interrupt is a
        /// FAILURE here, not a skipped frame - the done-when is that a Sweden game cannot pass these points.
        /// </summary>
        private IEnumerator CaptureInterrupts(GameController controller)
        {
            FieldInfo simField = controller.GetType().GetField("_simulationManager", BindingFlags.Instance | BindingFlags.NonPublic);
            if (!(simField?.GetValue(controller) is SimulationManager sim))
            {
                Debug.LogError("SHOT: -shotinterrupts could not reach SimulationManager - nothing filmed.");
                _failed++;
                yield break;
            }

            var noDecisions = new Dictionary<CountryId, PolicyDecision>();
            if (_countryId == CountryId.Poland)
            {
                // D-PL: a Polish game's film walks the day path to its polling day and its nights - PL-C: filming its Sejm campaign's HQ on the way and its presidential campaign's after
                yield return CapturePolishInterrupts(controller, sim, noDecisions);
                yield break;
            }
            if (_countryId == CountryId.Italy)
            {
                // D-IT: an Italian game holds no campaign (§941's note: its -shotinterrupts film reached no night), so its film walks the day path straight to
                // the polling day, films the night the controller opens, then the night's states staged from the same count
                yield return CaptureItalianInterrupts(controller, sim, noDecisions);
                yield break;
            }

            // CL-1 (2026-09-12): the run-up is reachable. Drive the day path to the pre-campaign's first day, open
            //    the HQ through the branch the rail's cell takes, film it, queue an office and a pollster live, step
            //    one day and film what they came to. Filmed here because this mode drives the controller-shaped day
            //    path; the staged -shotcampaign film stays the film of the screen's three readings.
            int daysToRunUp = 0;
            while (sim.PlayerPreCampaign == null && sim.PlayerCampaign == null && daysToRunUp < MaxStateSearchDays)
            {
                bool boundaryBefore = sim.AdvanceDay();
                MemTick(sim);   // the 2560 out-of-memory: the working set and the heaps every 30 game days
                sim.AdvanceCountryDayTick(_countryId);
                daysToRunUp++;
                if (boundaryBefore) { sim.AdvanceTurn(noDecisions); }
            }
            if (sim.PlayerPreCampaign != null && sim.PlayerCampaign == null)
            {
                UiGuardContext.CurrentScreen = "cl1_runup_hq_opened";   // S4: the HQ's first settle frames under their own name - the guard filed them under 05t_budget_turn0
                InvokeNoArg(controller, "OpenLiveCampaign");
                yield return Settle();
                Claim("imgui");
                yield return Capture("cl1_runup_hq_opened");
                if (_countryId == CountryId.USA) { yield return FilmS7BallotHq(controller, sim); }   // S7 (SP-6b): an independent's ballot drive on this HQ, STAGED and put back
                // A role the party's staging does not already hold - the stand-in's party (the largest seeded) stages a
                // manager and a pollster, so a pollster would be refused "already on the roster" and the film would show
                // a refusal where it means to show a hire.
                StaffRole hireRole = StaffRole.Pollster;
                foreach (StaffRole candidate in CampaignStaff.TheFive) { if (!sim.PlayerPreCampaign.HasRole(candidate)) { hireRole = candidate; break; } }
                // A-LOOP q24 5b: the office is back in the run-up now that the count reads its doors (5a had refused it), so the film queues the
                // television buy (charged, its week on the polls noted on the HQ once it has aired), a hire and an office - every one taken.
                sim.QueueCampaignDecision(CampaignActionKind.PrepareAdvertising, -1, null, PreCampaignRun.Price(sim.PlayerPreCampaign, CampaignActionKind.PrepareAdvertising), out string televisionRefusal);
                sim.QueueCampaignDecision(CampaignActionKind.RecruitStaff, -1, null, CampaignStaff.SalaryPerDay, out string hireRefusal, role: (int)hireRole);
                sim.QueueCampaignDecision(CampaignActionKind.EstablishOffice, -1, null, CampaignOffices.OpenCost, out string officeRefusal);
                if (televisionRefusal != null || hireRefusal != null || officeRefusal != null)
                {
                    Debug.LogError($"SHOT: CL-1 - the run-up refused a priced verb: {televisionRefusal ?? hireRefusal ?? officeRefusal}");
                    _failed++;
                }
                yield return Settle();
                yield return Capture("cl1_runup_hq_queued");
                {
                    bool boundaryBefore = sim.AdvanceDay();
                    MemTick(sim);   // the 2560 out-of-memory: the working set and the heaps every 30 game days
                    sim.AdvanceCountryDayTick(_countryId);
                    if (boundaryBefore) { sim.AdvanceTurn(noDecisions); }
                }
                yield return Settle();
                yield return Capture("cl1_runup_hq_stepped");
                // §657: the run-up's declarations, opened from the HQ's DECLARED chip - the DECLARED block only, on today's date.
                yield return FilmDeclared(controller, sim, "ps3d_runup_declared");
                Debug.Log($"SHOT: CL-1 - the run-up began {(sim.CampaignRecord != null ? sim.CampaignRecord.StartDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) : "?")} after {daysToRunUp} day(s); HQ filmed on run-up day {sim.PlayerPreCampaign.Day} with a television buy and a {hireRole} queued and stepped, an office taken; chest {sim.PlayerPreCampaign.Money:F0}, {sim.PlayerPreCampaign.TelevisionAired.Count} buy(s) aired, {sim.PlayerPreCampaign.Hired.Count} hired");
                InvokeNoArg(controller, "CloseLiveCampaign");
                SetPrivateField(controller, "_onDesk", true);
                yield return Settle();
            }
            else
            {
                Debug.LogError($"SHOT: CL-1 - no run-up began for {_countryId} within {MaxStateSearchDays} days; the pre-campaign HQ is NOT filmed.");
                _failed++;
            }

            // 0. P2-0.3: the campaign's opening is an interrupt. Drive the day path until the sim has begun
            //    the player's campaign, then let the controller's own Update meet it: the banner must name
            //    it, HQ must have opened itself, and the clock must not move until it is acknowledged.
            int daysToOpening = 0;
            while (sim.PlayerCampaign == null && daysToOpening < MaxStateSearchDays)
            {
                bool boundaryBefore = sim.AdvanceDay();
                MemTick(sim);   // the 2560 out-of-memory: the working set and the heaps every 30 game days
                sim.AdvanceCountryDayTick(_countryId);
                daysToOpening++;
                if (boundaryBefore) { sim.AdvanceTurn(noDecisions); }
            }

            if (sim.PlayerCampaign == null)
            {
                Debug.LogError($"SHOT: -shotinterrupts - no campaign began for {_countryId} within {MaxStateSearchDays} days; the opening interrupt is NOT filmed. A Sweden game must have one.");
                _failed++;
                yield break;
            }

            yield return Settle();
            yield return Settle();
            string interruptText = controller.GetType().GetMethod("BuildFoldedInterruptText", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.Invoke(controller, new object[] { true }) as string;   // §648: one parameter (the speed hint went with the sentence)
            FieldInfo hqOpenField = controller.GetType().GetField("_liveCampaignOpen", BindingFlags.Instance | BindingFlags.NonPublic);
            bool hqOpen = hqOpenField?.GetValue(controller) is bool h && h;
            if (interruptText == null || interruptText.IndexOf("campaign", System.StringComparison.OrdinalIgnoreCase) < 0)
            {
                Debug.LogError($"SHOT: -shotinterrupts - the campaign began ({sim.CurrentDate:yyyy-MM-dd}) and the banner does not name it: \"{interruptText ?? "(no banner)"}\".");
                _failed++;
            }

            if (!hqOpen)
            {
                Debug.LogError("SHOT: -shotinterrupts - the campaign began and Campaign HQ did not open itself.");
                _failed++;
            }

            Claim("imgui");   // the HQ is an IMGUI sheet - it stamps the desk token, as every folded screen does
            yield return Capture("e7a_campaign_opening_hq");
            yield return AssertClockHeld(controller, sim, "the campaign's opening");
            yield return FilmDeclared(controller, sim, "ps3d_campaign_declared");   // §657: the declarations the campaign opens on

            InvokeNoArg(controller, "AcknowledgeCampaignOpening");
            yield return Settle();
            yield return Capture("e7b_campaign_opening_acknowledged");
            yield return ReportClockAfterDismissal(controller, sim, "the campaign's opening");
            // PS-3e (§632): the government's budget is tabled and adopted in the run-up, and its division opens the signing takeover as
            // any passed act does (§633); the film seals it as a player would, or every frame after it photographs the signing (§648's finding).
            yield return SealLiveSigning(controller, "cl1_signing_government_budget");

            // CL-2 (2026-09-13): the loop's closures on film, on the live campaign the opening just began. (1) The map as the picker:
            //    opened from the HQ's masthead, a tile picked (the catalog's second valkrets), the HQ's local-acts line naming it and a
            //    rally queued there. (2) A story for the player's party: STAGED into the run's pending list for the film (the live rate is
            //    a coin the film cannot wait on), the HQ's seven chips over the legality panel, an answer queued, the day stepped and the
            //    note it leaves. The film stages the story; the harness (CampaignClockHarness 8) proves the rate, the answer, the replay.
            {
                int me = sim.PlayerPartyIndexForCampaign();
                InvokeNoArg(controller, "OpenLiveCampaignMap");
                yield return Settle();
                yield return Capture("cl2_campaign_map_live");
                controller.GetType().GetMethod("PickCampaignRegion", BindingFlags.Instance | BindingFlags.NonPublic)?.Invoke(controller, new object[] { 1 });
                yield return Settle();
                yield return Capture("cl2_campaign_map_picked");
                InvokeNoArg(controller, "CloseLiveCampaignMap");
                yield return Settle();
                object pickedObject = controller.GetType().GetMethod("LocalActRegion", BindingFlags.Instance | BindingFlags.NonPublic)?.Invoke(controller, null);
                int pickedRegion = pickedObject is int pr ? pr : -1;
                CampaignActions.ActionSpec rally = CampaignActions.Spec(CampaignActionKind.Rally);
                sim.QueueCampaignDecision(CampaignActionKind.Rally, pickedRegion, null, rally.MoneyCost, out string rallyRefusal);
                if (rallyRefusal != null) { Debug.LogError($"SHOT: CL-2 - the rally was refused: {rallyRefusal}"); _failed++; }
                yield return Settle();
                yield return Capture("cl2_campaign_hq_rally_picked");
                Debug.Log($"SHOT: CL-2 - region {pickedRegion} ({(pickedRegion >= 0 && sim.PlayerCampaign != null ? sim.PlayerCampaign.Setup.Regions[pickedRegion].Name : "none")}) picked on the map; a rally queued there for campaign day {sim.PlayerCampaign?.Day}; the debates fall on days {(sim.PlayerCampaign != null ? string.Join(" and ", sim.PlayerCampaign.Setup.DebateDays) : "?")}.");

                if (me >= 0 && sim.PlayerCampaign != null && !sim.PlayerCampaign.Finished)
                {
                    var staged = new Scandal(ScandalKind.Corruption, ScandalSeverity.Major, 0.5);
                    sim.PlayerCampaign.PendingScandals.Add((sim.PlayerCampaign.Day - 1, me, staged, 0.62));
                    yield return Settle();
                    yield return Settle();
                    yield return Capture("cl2_campaign_hq_scandal");
                    yield return AssertClockHeld(controller, sim, "a story that waits for its answer");
                    sim.QueueScandalResponse(ScandalResponse.Apologize, out string answerRefusal);
                    if (answerRefusal != null) { Debug.LogError($"SHOT: CL-2 - the answer was refused: {answerRefusal}"); _failed++; }
                    yield return Settle();
                    yield return Capture("cl2_campaign_hq_scandal_answered");
                    {
                        bool boundaryBefore = sim.AdvanceDay();
                        MemTick(sim);   // the 2560 out-of-memory: the working set and the heaps every 30 game days
                        sim.AdvanceCountryDayTick(_countryId);
                        if (boundaryBefore) { sim.AdvanceTurn(noDecisions); }
                    }
                    yield return Settle();
                    yield return Capture("cl2_campaign_hq_scandal_stepped");
                    int answered = 0;
                    foreach ((int sDay, int sParty, ScandalResponse sResponse, ScandalOutcome sOutcome) in sim.PlayerCampaign.Scandals) { if (sParty == me && sResponse == ScandalResponse.Apologize) { answered++; } }
                    if (answered != 1) { Debug.LogError($"SHOT: CL-2 - the staged story was not answered by the queued response ({answered} apologies on the party's record)."); _failed++; }
                    Debug.Log($"SHOT: CL-2 - the staged story (corruption, major, seen 0.62) answered APOLOGIZE and resolved on campaign day {sim.PlayerCampaign.Day - 1}; {sim.PlayerCampaign.Scandals.Count} story(ies) on the run's record; pending {sim.PlayerCampaign.PendingScandals.Count}.");
                }
                else
                {
                    Debug.LogError("SHOT: CL-2 - no player party in the live campaign; the story frames are NOT filmed.");
                    _failed++;
                }
            }

            // Back to the Desk for the rest of the run, as a player who has read the HQ would go.
            InvokeNoArg(controller, "CloseLiveCampaign");
            SetPrivateField(controller, "_onDesk", true);
            yield return Settle();

            // §657: play's own clock is PAUSED while the harness walks the days itself - at Normal the controller's Update ticked a day off polling
            // day between the harness's frames (a slow frame at 1280 real), queued the staged act's ceremony and left CheckElection no polling day.
            // Restored once the election is called; the night holds the clock by itself (AssertClockHeld sets and restores its own speed).
            FieldInfo walkSpeedField = controller.GetType().GetField("_gameSpeed", BindingFlags.Instance | BindingFlags.NonPublic);
            object walkSpeed = walkSpeedField?.GetValue(controller);
            SetEnumField(controller, "_gameSpeed", "Paused");

            // 1. To the first POLLING DAY (PS-2 / CL-4, §619: the country's own calendar - Sweden's 13 September 2026 from its 18 January start),
            //    through the controller-shaped day path (the campaign runs inside the sim's own day; the polling day is where Update calls CheckElection).
            int days = 0;
            while (days < MaxStateSearchDays)
            {
                bool boundary = sim.AdvanceDay();
                MemTick(sim);   // the 2560 out-of-memory: the working set and the heaps every 30 game days
                sim.AdvanceCountryDayTick(_countryId);
                days++;
                if (boundary)
                {
                    sim.AdvanceTurn(noDecisions);
                }
                if (sim.PollingDayToday) { break; }
            }

            if (!sim.PollingDayToday)
            {
                Debug.LogError($"SHOT: -shotinterrupts found no polling day within {MaxStateSearchDays} days for {_countryId} - the night is NOT filmed (a country whose election calendar is not modelled has none).");
                _failed++;
                yield break;
            }

            Debug.Log($"SHOT: -shotinterrupts reached election turn {sim.CurrentTurn} on {sim.CurrentDate:yyyy-MM-dd} after {days} days; campaign result {(sim.PlayerCampaignResult != null ? "present" : "ABSENT")}.");

            // Board 5c (D11 row 3): the night's plate draws the estimate that travelled with the STANDING budget
            // act - the newest passed fiscal division carrying effects - so one is staged here with the standing
            // preview's arrows (the same staging the ceremony films use, P2-4.3), and the harness says so.
            StageDivisionWithContent(sim.World.GetCountry(_countryId), sim, "Harness: the standing budget act (staged for the effects plate, 5c)", 30f, passed: true);

            // 2. Election night is entered by the controller's own election call, and it must be a takeover
            //    the clock cannot pass.
            yield return SealLiveSigning(controller, "e7_signing_before_the_night");
            InvokeNoArg(controller, "CheckElection");
            if (walkSpeed != null) { walkSpeedField.SetValue(controller, walkSpeed); }   // §657: the walk's pause lifted once the election is called
            FieldInfo nightField = controller.GetType().GetField("_electionNight", BindingFlags.Instance | BindingFlags.NonPublic);
            // PS-6 US-34: the USA's night is built (D-US) - its campaign staged and its HQ filmed above, the night is filmed here as every country's; one not
            // shown is the failure below, as anywhere
            if (nightField?.GetValue(controller) == null)
            {
                Debug.LogError($"SHOT: -shotinterrupts - {_countryId} reached its election and NO election night was shown. The controller logs why (ELECTION: / CANVAS: lines above); a Sweden game must show one.");
                _failed++;
                yield break;
            }

            yield return WaitForCanvasSettle(controller, wantActive: true);
            yield return Settle();
            Claim("electionnight");
            yield return Capture("e7c_election_night_takeover");
            RecordCanvasTextAssert("e7c_election_night_takeover", controller);

            yield return AssertClockHeld(controller, sim, "election night");

            InvokeNoArg(controller, "DismissElectionNight");
            yield return WaitForCanvasSettle(controller, wantActive: false);
            yield return Settle();
            Claim("imgui");
            FieldInfo overField = controller.GetType().GetField("_isGameOver", BindingFlags.Instance | BindingFlags.NonPublic);
            bool over = overField?.GetValue(controller) is bool o && o;
            yield return Capture(over ? "e7d_after_election_night_gameover" : "e7d_after_election_night");
            if (!over)
            {
                yield return ReportClockAfterDismissal(controller, sim, "election night");
            }
            if (_countryId == CountryId.Germany)
            {
                yield return CaptureGermanNightPartial(controller, sim);
            }
            if (_countryId == CountryId.USA)
            {
                yield return CaptureUsNightStaged(controller, sim);   // US-34: the night part-way, the Congress lens, a College with no majority and the House's choice - each STAGED
            }

            if (Formation && !over && sim.RoundsApply(_countryId))
            {
                yield return CaptureFormationTail(controller, sim, noDecisions);
            }
            else if (Formation)
            {
                Debug.LogError($"SHOT: -shotformation - {_countryId}'s game {(over ? "ended on its verdict" : "runs no round")}; the formation is NOT filmed.");
                _failed++;
            }

            Debug.Log($"SHOT: interrupts done, {_captured} captured, {_failed} failed.");
            Debug.Log($"SHOT: {ReportOverflows()} text overflow(s) recorded.");
            Debug.Log($"SHOT: {ReportContainmentEscapes()} containment escape(s) recorded.");
            Finish(_failed == 0 && _loggedErrors == 0 ? 0 : 1);
        }

        /// <summary>
        /// PS-6 US-34 (D-US, boards 29a-29b): THE US NIGHT STAGED, after the night the film just played (e7c) - built from that night's own contests, each
        /// in a screen of its own, filmed with the Canvas guard and destroyed; the game is not touched. (1) Part-way, at 30 of 51 declared (e7e): the states
        /// not yet in dashed, the bars' middles to come, the calls where they stand. (2) The same instant under the Congress lens (e7f): the senators not up
        /// hatched, the District dotted. (3) A College with no majority (e7g, 29a's 269-269 pick): the closest states called the other way
        /// (`UsNight.FlipsToTie`), NO MAJORITY · TO THE HOUSE, the Delegations lens, the 6 JAN chip amber. (4) The House's choice on 6 January (e7h) on that
        /// staged College, the votes by tonight's delegations (the House that sits from 3 January) - and the Senate's Vice President where it cannot choose.
        /// Each is STAGED and named so: the live night opens at its last declaration, and a College with no majority is not this game's count.
        /// </summary>
        private IEnumerator CaptureUsNightStaged(GameController controller, SimulationManager sim)
        {
            Country usa = sim.World?.GetCountry(CountryId.USA);
            // real904: the night is dismissed and the day has moved on - the count is read by the polling day its contest carries (the latest presidential count held)
            UsPresidentialElection.Contest latest = null;
            if (usa?.UsPresidentialElections != null) { foreach (UsPresidentialElection.Contest c in usa.UsPresidentialElections) { if (c.PollingDay.Date <= sim.CurrentDate.Date && (latest == null || c.PollingDay > latest.PollingDay)) { latest = c; } } }
            DateTime polled = latest != null ? latest.PollingDay.Date : sim.CurrentDate.Date;
            UsCongressElection.Contest congress = usa?.UsCongressElections?.Find(c => c.PollingDay.Date == polled);
            UsPresidentialElection.Contest president = usa?.UsPresidentialElections?.Find(c => c.PollingDay.Date == polled);
            if (congress == null || president == null)
            {
                Debug.LogError($"SHOT: US-34 - no count of {polled:yyyy-MM-dd} in hand, so the night part-way is NOT filmed.");
                _failed++;
                yield break;
            }
            SetPrivateField(controller, "_canvasLive", true);
            PoliSim.Testing.CaptureIdentity.CanvasSurface = "electionnight";

            // (1) and (2): part-way, then the Congress lens
            UsNight.State partial = UsNight.At(30, usa, president, congress);
            ElectionNightScreen staged = partial != null ? ElectionNightScreen.BuildUsa(GameController.UsNightInputs(usa, partial, president, congress), null) : null;
            if (staged == null) { Debug.LogError("SHOT: US-34 - the board did not build for the night part-way; nothing filmed."); _failed++; }
            else
            {
                yield return Settle();
                Claim("electionnight");
                yield return Capture("e7e_election_night_partial_staged");
                RecordCanvasTextAssert("e7e_election_night_partial_staged", controller);
                Debug.Log(string.Format(CultureInfo.InvariantCulture, "SHOT: US-34 - the night staged part-way: {0} of {1} declared; the College REP {2} DEM {3} ({4} to come, {5}); the House REP {6} DEM {7} ({8}); the Senate REP {9} DEM {10} ({11} to come, {12}); cells {13} inked / {14} not yet.",
                    partial.Declared, partial.Total, partial.ElectorsRep, partial.ElectorsDem, partial.ElectorsToCome, partial.College, partial.HouseRep, partial.HouseDem, partial.House,
                    partial.SenateRep, partial.SenateDem, partial.UpToCome, partial.Senate, staged.UsTiles?.LastInked ?? -1, staged.UsTiles?.LastNotYet ?? -1));
                if (staged.SetUsLens(UsTileView.Lens.Congress))
                {
                    yield return Settle();
                    Claim("electionnight");
                    yield return Capture("e7f_election_night_congress_lens_staged");
                    RecordCanvasTextAssert("e7f_election_night_congress_lens_staged", controller);
                    Debug.Log(string.Format(CultureInfo.InvariantCulture, "SHOT: US-34 - the Congress lens on the night part-way: cells {0} inked / {1} hatched / {2} dotted / {3} not yet.",
                        staged.UsTiles.LastInked, staged.UsTiles.LastHatched, staged.UsTiles.LastDotted, staged.UsTiles.LastNotYet));
                }
                else { Debug.LogError("SHOT: US-34 - the night part-way would not take the Congress lens; e7f is NOT filmed."); _failed++; }
                if (staged.Root != null) { UnityEngine.Object.Destroy(staged.Root); }
                yield return Settle();
            }

            // (3): a College with no majority, staged
            ElectoralCollege.StateCall[] states = UsPresidentialElection.StatesOf(usa, president);
            string[] flips = UsNight.FlipsToTie(states);
            UsPresidentialElection.Contest tied = null;
            if (flips == null) { Debug.LogError("SHOT: US-34 - no set of states called the other way leaves the College equal; e7g and e7h are NOT filmed."); _failed++; }
            else
            {
                Dictionary<(string, int), string> notUp = UsNight.NotUp(usa, congress, out int independents);
                UsNight.State tie = UsNight.Of(int.MaxValue, polled, UsNight.Flipped(states, flips), congress, notUp, independents);
                ElectionNightScreen.UsNightBoard tieBoard = GameController.UsNightInputs(usa, tie, president, congress);
                tieBoard.HasReference = false;   // the review's L4: a staged College is no played count to set beside the record
                ElectionNightScreen none = ElectionNightScreen.BuildUsa(tieBoard, null, UsTileView.Lens.Delegations);
                if (none == null) { Debug.LogError("SHOT: US-34 - the board did not build for the College with no majority; nothing filmed."); _failed++; }
                else
                {
                    yield return Settle();
                    Claim("electionnight");
                    yield return Capture("e7g_election_night_no_majority_staged");
                    RecordCanvasTextAssert("e7g_election_night_no_majority_staged", controller);
                    Debug.Log(string.Format(CultureInfo.InvariantCulture, "SHOT: US-34 - the College staged with no majority ({0} called the other way): REP {1} DEM {2}, {3} at {4} of {5}; tonight's delegations REP {6} DEM {7} tied {8}; the lens {9}.",
                        string.Join(", ", flips), tie.ElectorsRep, tie.ElectorsDem, tie.College, tie.CollegeAt, tie.Total, tie.DelegationsRep, tie.DelegationsDem, tie.DelegationsTied, none.UsTiles?.Current));
                    if (none.Root != null) { UnityEngine.Object.Destroy(none.Root); }
                    yield return Settle();
                }

                // (4): the House's choice on that staged College - a copy of the contest, never the game's
                tied = new UsPresidentialElection.Contest
                {
                    PollingDay = president.PollingDay, DayBasis = president.DayBasis, ElectorsMeet = president.ElectorsMeet, CountedOn = president.CountedOn, TakesOffice = president.TakesOffice,
                    ElectorsAppointed = president.ElectorsAppointed, Contingent = true, TieSeed = president.TieSeed, Line = "staged",
                };
                foreach (UsPresidentialElection.Tally t in president.Electors)
                {
                    int votes = t.Party == "REP" ? tie.ElectorsRep : t.Party == "DEM" ? tie.ElectorsDem : 0;
                    tied.Electors.Add(new UsPresidentialElection.Tally { Candidate = t.Candidate, Party = t.Party, Votes = votes, Share = t.Share, OwnNominee = t.OwnNominee });
                }
                int allStates = UsPresidentialElection.AllStates(polled);
                string choice = UsPresidentialElection.ChooseByStates(tied, congress.Delegations(), allStates, "the House elected tonight", tied.CountedOn);
                if (!tied.Decided())
                {
                    // the review's L4: the Senate that chooses on 6 January is the one seated on 3 January (tonight's seats over the record's) - the game's
                    // own choice reads the Senate seated on its day; the polling day's country still seats the outgoing one, so the staged tallies are made here
                    Dictionary<string, int> sits = UsCongressElection.SenateOn(usa, congress.Convenes);
                    int needed = UsSenate.Members / 2 + 1;
                    foreach (UsPresidentialElection.Tally t in tied.Electors)
                    {
                        if (t.Party == null || t.Votes <= 0) { continue; }
                        tied.SenateChoice.Add(new UsPresidentialElection.Tally { Candidate = UsTermLimit.RunningMateOf(usa, tied, t.Party), Party = t.Party, Votes = sits.TryGetValue(t.Party, out int n) ? n : 0 });
                    }
                    tied.SenateChoice.Sort((a, b) => b.Votes.CompareTo(a.Votes));
                    UsPresidentialElection.Tally vp = tied.SenateChoice.Find(x => x.Votes >= needed);
                    choice += string.Format(CultureInfo.InvariantCulture, "; the Senate seated {0:yyyy-MM-dd} chooses the Vice President - {1}; {2}", congress.Convenes,
                        string.Join(", ", tied.SenateChoice.ConvertAll(x => x.Party + " " + x.Votes.ToString(CultureInfo.InvariantCulture))), vp != null ? vp.Candidate + " chosen" : "no one has " + needed.ToString(CultureInfo.InvariantCulture));
                }
                ElectionNightScreen house = ElectionNightScreen.BuildUsHouseChoice(tied, allStates, null);
                if (house == null) { Debug.LogError("SHOT: US-34 - the board did not build for the House's choice; nothing filmed."); _failed++; }
                else
                {
                    yield return Settle();
                    Claim("electionnight");
                    yield return Capture("e7h_house_choice_staged");
                    RecordCanvasTextAssert("e7h_house_choice_staged", controller);
                    Debug.Log("SHOT: US-34 - the House's choice staged on 6 January: " + choice);
                    if (house.Root != null) { UnityEngine.Object.Destroy(house.Root); }
                }
            }
            yield return FilmS7Night(controller, usa, president, congress, polled);   // S7 (SP-6b): the College with a third ink, STAGED
            PoliSim.Testing.CaptureIdentity.CanvasSurface = null;
            SetPrivateField(controller, "_canvasLive", false);
            yield return Settle();
        }

        /// <summary>
        /// S7 (Design's return of 2026-10-30, 7a and 7c; SP-6b): AN INDEPENDENT'S BALLOT DRIVE ON THE RUN-UP'S HQ, STAGED. SP-6b's run is not built, so a drive is
        /// handed to the HQ (`GameController.StageUsBallotForFilm`) on the sourced rules (`UsBallotAccess`): filed by the fee wherever a fee may be paid instead of
        /// the petition (the one route the rules allow without signatures); the target Ohio collecting at Design's illustrative 3,410 signatures; the next three
        /// open deadlines' states collecting at Design's illustrative shares of their requirement (85, 72 and 30 %); every other jurisdiction as the rules leave it
        /// on the day - off, its deadline passed, or BILLED. Filmed whole, then one slip of each face pinned; the drive put back, nothing of the game touched.
        /// </summary>
        private IEnumerator FilmS7BallotHq(GameController controller, SimulationManager sim)
        {
            string[] slipFrames = { "s7c_us_ballot_on_slip", "s7c_us_ballot_collecting_slip", "s7c_us_ballot_off_slip", "s7c_us_ballot_lost_slip", "s7c_us_ballot_billed_slip" };
            Expect("s7a_us_ballot_hq_staged");
            Expect(slipFrames);
            DateTime poll = sim.PlayerPreCampaign.Calendar.ElectionDate, today = sim.CurrentDate.Date;
            UsBallotAccess.Drive drive = UsBallotAccess.NewDrive(poll);
            if (drive.Petitions.Count == 0)
            {
                Debug.LogError($"SHOT: S7 s7a_us_ballot_hq_staged - MISSING: no ballot rules for the polling day {poll:yyyy-MM-dd} (the summary is 2024's)");
                _failed += 1 + slipFrames.Length;
                yield break;
            }
            var filed = new List<string>();
            foreach (UsBallotAccess.Rule r in UsBallotAccess.Rules(poll)) { if (r.FeeInstead && UsBallotAccess.File(r, drive.Of(r.State), today, out string _)) { filed.Add(r.State); } }
            UsBallotAccess.Petition ohio = drive.Of("OH");
            if (ohio != null) { ohio.Gathered = 3410; }
            double[] shares = { 0.85, 0.72, 0.30 };
            var collecting = new List<string> { "OH 3410" };
            int k = 0;
            foreach (UsBallotAccess.Rule r in UsBallotAccess.NextDeadlines(drive, today, 8))
            {
                if (k >= shares.Length) { break; }
                if (r.State == "OH" || r.Signatures <= 0) { continue; }
                drive.Of(r.State).Gathered = (int)Math.Round(r.Signatures * shares[k]);
                collecting.Add(r.State + " " + drive.Of(r.State).Gathered.ToString(CultureInfo.InvariantCulture) + "/" + r.Signatures.ToString(CultureInfo.InvariantCulture));
                k++;
            }
            controller.StageUsBallotForFilm(drive, "OH");
            yield return Settle();
            yield return Settle();
            UsBallotAccess.Tally(drive, today, out int on, out int col, out int lost, out int onCount);
            Claim("imgui");
            Debug.Log(string.Format(CultureInfo.InvariantCulture, "SHOT: S7 s7a_us_ballot_hq_staged - STAGED: the run-up's HQ on {0:yyyy-MM-dd} with an independent's drive - filed by the fee {1}; collecting {2}; the rest as the rules leave them; electors on {3}, collecting {4}, lost {5}; {6} of {7} on the ballot; target OH",
                today, filed.Count > 0 ? string.Join(" ", filed.ToArray()) : "nowhere", string.Join(", ", collecting.ToArray()), on, col, lost, onCount, drive.Petitions.Count));
            yield return Capture("s7a_us_ballot_hq_staged");

            // one slip of each face, pinned on its tile
            MethodInfo pin = controller.GetType().GetMethod("PinSlipOnAnchorForFilm", BindingFlags.Instance | BindingFlags.NonPublic);
            FieldInfo pins = controller.GetType().GetField("_slipPins", BindingFlags.Instance | BindingFlags.NonPublic);
            string FirstOf(UsBallotAccess.Standing st)
            {
                foreach (UsBallotAccess.Rule r in UsBallotAccess.Rules(poll)) { if (UsBallotAccess.StandingOf(r, drive.Of(r.State), today) == st) { return r.State; } }
                return null;
            }
            string[] states = { FirstOf(UsBallotAccess.Standing.OnBallot), "OH", FirstOf(UsBallotAccess.Standing.Off), FirstOf(UsBallotAccess.Standing.Lost), FirstOf(UsBallotAccess.Standing.Billed) };
            for (int i = 0; i < slipFrames.Length; i++)
            {
                if (pin == null || pins == null || states[i] == null)
                {
                    Debug.LogError("SHOT: S7 " + slipFrames[i] + " - MISSING: " + (pin == null || pins == null ? "the controller has no PinSlipOnAnchorForFilm / _slipPins" : "no jurisdiction stands so on " + today.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)));
                    _failed++;
                    continue;
                }
                pin.Invoke(controller, new object[] { "hq:ballot:" + states[i], Vector2.zero });
                yield return Settle();
                yield return Settle();
                Debug.Log("SHOT: S7 " + slipFrames[i] + " - STAGED: the slip of " + states[i] + " pinned on its tile (the drive above)");
                yield return Capture(slipFrames[i]);
                (pins.GetValue(controller) as IList)?.Clear();
                yield return Settle();
            }
            controller.StageUsBallotForFilm(null, null);
            yield return Settle();
        }

        /// <summary>
        /// S7 (Design's 7b; SP-6b): THE COLLEGE NIGHT WITH A THIRD INK, STAGED on the count the night just played (its states as `UsPresidentialElection.StatesOf`
        /// re-derives them), the night at 51 of 51. On the ballot: every jurisdiction whose rule is not BILLED and whose deadline falls on or after the run-up's
        /// first day - the drive filed wherever its sourced rule could be met. The independent's share where on the ballot (`UsBallotAccess.Night`): (a) every other
        /// candidate's national share as the count read it - no elector; (b) the least share that wins one (`UsBallotAccess.LeastShareForOneElector`) - and with
        /// no one at a majority, the House decides. Each a screen of its own, filmed with the Canvas guard and destroyed; the game is not touched.
        /// </summary>
        private IEnumerator FilmS7Night(GameController controller, Country usa, UsPresidentialElection.Contest president, UsCongressElection.Contest congress, DateTime polled)
        {
            Expect("s7b_us_night_ind_zero_staged", "s7b_us_night_ind_one_staged");
            ElectoralCollege.StateCall[] states = UsPresidentialElection.StatesOf(usa, president);
            UsNight.State full = UsNight.At(int.MaxValue, usa, president, congress);
            var onBallot = new HashSet<string>();
            DateTime opened = WorldClock.StartDate(CountryId.USA).Date;
            foreach (UsBallotAccess.Rule r in UsBallotAccess.Rules(polled)) { if (!r.Billed && r.Deadline.Value.Date >= opened) { onBallot.Add(r.State); } }
            double others = double.NaN;
            foreach (UsPresidentialElection.Tally t in president.Electors) { if (t.Candidate == UsPresidentialElection.OtherCandidates) { others = t.Popular() / 100.0; } }
            if (states == null || full == null || onBallot.Count == 0 || double.IsNaN(others) || !ElectoralCollege.TryPriorYear(polled.Year, out int prior))
            {
                Debug.LogError($"SHOT: S7 s7b_us_night_ind_zero_staged / s7b_us_night_ind_one_staged - MISSING: {(states == null || full == null ? "the count's states could not be re-derived" : onBallot.Count == 0 ? "no ballot rules for " + polled.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) : double.IsNaN(others) ? "the count carries no other candidates' share" : "no prior election")}");
                _failed += 2;
                yield break;
            }
            // (b) on the College e7g stages with no majority (the fewest states called the other way to an equal College, `UsNight.FlipsToTie`): on the count's own
            // states the place the least share wins can be taken from the larger side and leave its majority standing, so the one elector that sends the count to
            // the House is staged from an equal College - as Design's 7b drew it
            string[] flips = UsNight.FlipsToTie(states);
            ElectoralCollege.StateCall[] tiedStates = flips != null ? UsNight.Flipped(states, flips) : null;
            UsNight.State tied = null;
            if (tiedStates != null)
            {
                Dictionary<(string, int), string> notUp = UsNight.NotUp(usa, congress, out int independents);
                tied = UsNight.Of(int.MaxValue, polled, tiedStates, congress, notUp, independents);
            }
            if (tied == null) { Debug.LogError("SHOT: S7 s7b_us_night_ind_one_staged - MISSING: no set of states called the other way leaves the College equal"); _failed++; }
            double least = tiedStates != null ? UsBallotAccess.LeastShareForOneElector(tiedStates, onBallot, prior) : double.NaN;
            var frames = new List<(string Frame, double Share, string Words, ElectoralCollege.StateCall[] States, UsNight.State Night)>
            {
                ("s7b_us_night_ind_zero_staged", others, "every other candidate's national share as the count read it, on the count's own states", states, full),
            };
            if (tied != null) { frames.Add(("s7b_us_night_ind_one_staged", least, "the least share that wins an elector, on the College staged equal (" + string.Join(", ", flips) + " called the other way)", tiedStates, tied)); }
            foreach ((string frame, double share, string words, ElectoralCollege.StateCall[] on, UsNight.State nightState) in frames)
            {
                UsBallotAccess.NightReading reading = UsBallotAccess.Night(on, share, onBallot, prior);
                ElectionNightScreen.UsNightBoard board = GameController.UsNightInputs(usa, nightState, president, congress);
                board.HasReference = false;   // a staged College is no played count to set beside the record
                board.Independent = reading;
                board.IndependentInk = GameController.IndependentInk();
                ElectionNightScreen screen = ElectionNightScreen.BuildUsa(board, null);
                if (screen == null) { Debug.LogError("SHOT: S7 " + frame + " - MISSING: the board did not build"); _failed++; continue; }
                yield return Settle();
                Claim("electionnight");
                UsBallotAccess.Place nearest = reading.Nearest.Count > 0 ? reading.Nearest[0] : null;
                Debug.Log(string.Format(CultureInfo.InvariantCulture, "SHOT: S7 {0} - STAGED: the independent on {1} ballots at {2:0.00} % where on them ({3}), {4:0.00} % nationally; electors IND {5} REP {6} DEM {7}; nearest {8}{9}",
                    frame, onBallot.Count, 100.0 * share, words, 100.0 * reading.NationalShare, reading.Electors, reading.ElectorsRep, reading.ElectorsDem,
                    nearest != null ? nearest.Code + " " + nearest.GapPp.ToString("0.0", CultureInfo.InvariantCulture) + " pp" : "none", reading.NoMajority ? "; no one at 270 - the House decides" : string.Empty));
                yield return Capture(frame);
                RecordCanvasTextAssert(frame, controller);
                if (screen.Root != null) { UnityEngine.Object.Destroy(screen.Root); }
                yield return Settle();
            }
        }

        /// <summary>
        /// §707 (D-DE, board 24b B): THE GERMAN NIGHT PART-WAY - the night the film just played, staged at 11 of 16 Länder declared (the state
        /// 24b measured): the undeclared Länder dashed at full size, the seats OWED, the swing like for like against the same Länder. Built from
        /// that election's own Land count (NationalElection's last), in a screen of its own, filmed with the Canvas guard and destroyed - the
        /// game is not touched. A staged frame, named so: the live night opens at its final minute.
        /// </summary>
        private IEnumerator CaptureGermanNightPartial(GameController controller, SimulationManager sim)
        {
            Country germany = sim.World?.GetCountry(CountryId.Germany);
            if (NationalElection.LastRegionalKeys == null || germany == null)
            {
                Debug.LogError("SHOT: §707 - no German count in hand, so the night part-way is NOT filmed.");
                _failed++;
                yield break;
            }
            var keys = new List<string>(NationalElection.LastRegionalKeys);
            int seats = 0;
            foreach (int s in germany.ParliamentSeats.Values) { seats += s; }
            DateTime polled = sim.CurrentDate;
            int minute = (int)Math.Round(ElectionNightFromModel.FinalMinute * 11.0 / 16.0);
            NightState partial = ElectionNightFromModel.At(minute, CountryId.Germany, keys, seats, 0.05, polled);
            if (partial == null || partial.Complete)
            {
                Debug.LogError($"SHOT: §707 - the German night at minute {minute} is {(partial == null ? "not built" : "already complete")}; the night part-way is NOT filmed.");
                _failed++;
                yield break;
            }
            ElectionVintage seatedDe = WorldClock.SeatedVintage(CountryId.Germany, polled);
            Dictionary<string, int> seatedTable = PartySystems.InitialSeats(CountryId.Germany, seatedDe);
            var previousSeats = new int[keys.Count];
            for (int k = 0; k < keys.Count; k++) { seatedTable.TryGetValue(keys[k], out previousSeats[k]); }

            SetPrivateField(controller, "_canvasLive", true);
            PoliSim.Testing.CaptureIdentity.CanvasSurface = "electionnight";
            ElectionNightScreen screen = ElectionNightScreen.Build(partial, keys.ToArray(), "GERMANY", polled.Date.AddHours(18), seats,
                previousLabel: "GERMANY " + WorldClock.ElectionDayOf(CountryId.Germany, seatedDe).Year.ToString(CultureInfo.InvariantCulture),
                previousByConstituency: GermanRegions.PreviousVotes(keys, polled), previousSeats: previousSeats, inkCountry: CountryId.Germany,
                ledger: sim.PlayerCampaignLedger, ledgerParty: sim.PlayerCampaignLedgerParty);   // the game's own campaign, as the live night carries it
            if (screen == null)
            {
                Debug.LogError("SHOT: §707 - the board did not build for the German night part-way; nothing filmed.");
                _failed++;
            }
            else
            {
                yield return Settle();
                Claim("electionnight");
                yield return Capture("e7e_election_night_partial_staged");
                RecordCanvasTextAssert("e7e_election_night_partial_staged", controller);
                Debug.Log(string.Format(CultureInfo.InvariantCulture, "SHOT: §707 - the German night staged part-way: minute {0}, {1} of {2} Länder declared, seats owed; map {3} full / {4} compact / {5} minimal, {6} with VS 2021.",
                    minute, partial.DeclaredCount, partial.TotalConstituencies, screen.Laender?.LastFull ?? -1, screen.Laender?.LastCompact ?? -1, screen.Laender?.LastMinimal ?? -1, screen.Laender?.LastWithSwing ?? -1));
                if (screen.Root != null) { UnityEngine.Object.Destroy(screen.Root); }
            }
            PoliSim.Testing.CaptureIdentity.CanvasSurface = null;
            SetPrivateField(controller, "_canvasLive", false);
            yield return Settle();
        }

        /// <summary>
        /// D-IT (boards 34a-34d): THE ITALIAN INTERRUPTS FILM - an Italian game played from its start as play steps it (the day path, the country's day tick,
        /// the clock paused while the harness walks) to its polling day, the night opened by the controller's own election call and held as a takeover the
        /// clock cannot pass (e7c, the night at its close as play opens it), dismissed by its Continue; then `CaptureItalianNights`.
        /// </summary>
        private IEnumerator CaptureItalianInterrupts(GameController controller, SimulationManager sim, Dictionary<CountryId, PolicyDecision> noDecisions)
        {
            Debug.Log("SHOT: -shotinterrupts - Italy (D-IT): the film walks to the polling day, films the night, then its states staged from the same count.");
            Expect("e7c_election_night_takeover", "e7i_italy_night_before_staged", "e7i_italy_night_counting_staged", "e7i_italy_night_counting_senato_staged",
                "e7i_italy_night_called_staged", "e7i_italy_night_called_senato_staged", "e7i_italy_night_final_senato_staged", "e7i_italy_night_dagger_staged",
                "e7i_italy_night_dissolution_staged");
            SetPrivateField(controller, "_onDesk", true);
            FieldInfo walkSpeedField = controller.GetType().GetField("_gameSpeed", BindingFlags.Instance | BindingFlags.NonPublic);
            object walkSpeed = walkSpeedField?.GetValue(controller);
            SetEnumField(controller, "_gameSpeed", "Paused");
            int days = 0;
            while (days < MaxStateSearchDays)
            {
                if (sim.PollingDayToday) { break; }
                bool boundary = sim.AdvanceDay();
                MemTick(sim);
                sim.AdvanceCountryDayTick(_countryId);
                days++;
                if (boundary) { sim.AdvanceTurn(noDecisions); }
            }
            if (!sim.PollingDayToday)
            {
                Debug.LogError($"SHOT: -shotinterrupts found no polling day within {MaxStateSearchDays} days for Italy - the night is NOT filmed.");
                _failed++;
            }
            else
            {
                Debug.Log($"SHOT: -shotinterrupts reached Italy's polling day {sim.CurrentDate:yyyy-MM-dd} after {days} days.");
                InvokeNoArg(controller, "CheckElection");
                if (walkSpeed != null) { walkSpeedField.SetValue(controller, walkSpeed); }
                FieldInfo nightField = controller.GetType().GetField("_electionNight", BindingFlags.Instance | BindingFlags.NonPublic);
                if (!(nightField?.GetValue(controller) is ElectionNightScreen live))
                {
                    Debug.LogError("SHOT: -shotinterrupts - Italy reached its election and NO election night was shown. The controller logs why (ELECTION: / CANVAS: lines above).");
                    _failed++;
                }
                else
                {
                    yield return WaitForCanvasSettle(controller, wantActive: true);
                    yield return Settle();
                    Claim("electionnight");
                    yield return Capture("e7c_election_night_takeover");
                    RecordCanvasTextAssert("e7c_election_night_takeover", controller);
                    LogItalianNight("e7c_election_night_takeover", live);
                    yield return AssertClockHeld(controller, sim, "election night");
                    InvokeNoArg(controller, "DismissElectionNight");
                    yield return WaitForCanvasSettle(controller, wantActive: false);
                    yield return Settle();
                    Claim("imgui");
                    FieldInfo overField = controller.GetType().GetField("_isGameOver", BindingFlags.Instance | BindingFlags.NonPublic);
                    bool over = overField?.GetValue(controller) is bool o && o;
                    yield return Capture(over ? "e7d_after_election_night_gameover" : "e7d_after_election_night");
                    if (!over) { yield return ReportClockAfterDismissal(controller, sim, "election night"); }
                    yield return CaptureItalianNights(controller, sim);
                }
            }
            Debug.Log($"SHOT: interrupts done, {_captured} captured, {_failed} failed.");
            Debug.Log($"SHOT: {ReportOverflows()} text overflow(s) recorded.");
            Debug.Log($"SHOT: {ReportContainmentEscapes()} containment escape(s) recorded.");
            Finish(_failed == 0 && _loggedErrors == 0 ? 0 : 1);
        }

        /// <summary>
        /// D-IT (34a's picks, 34b's C): ITALY'S NIGHT, ITS STATES STAGED from the election the film just held - before (0 of 147), counting (61, Camera and
        /// Senato), called (where both chambers' calls landed, Camera and Senato), final on the Senato, the † view, and the dissolution: the column read on a
        /// planted hostile Senato (each centre-right list cut in proportion to seventy seats, the seats taken given to PD - a fixture, never a count), so the
        /// formation's own code tries and fails three mandates. Each in a screen of its own, filmed with the Canvas guard and destroyed; STAGED on the masthead;
        /// the game is not touched (the planted Senato is restored before the frame).
        /// </summary>
        private IEnumerator CaptureItalianNights(GameController controller, SimulationManager sim)
        {
            Country italy = sim.World?.GetCountry(CountryId.Italy);
            ElectionRecord latest = italy != null && italy.ElectionHistory.Count > 0 ? italy.ElectionHistory[italy.ElectionHistory.Count - 1] : null;
            if (latest == null || latest.Method != ElectionMethod.ItalyRosatellum)
            {
                Debug.LogError("SHOT: D-IT - no Italian count in hand, so the night's states are NOT filmed.");
                _failed++;
                yield break;
            }
            ElectionNightScreen.ItalyNight final = GameController.ItalianNightInputs(sim, italy, latest, ItalianNight.Total);
            int called = final?.State.CalledAt ?? 0;
            var frames = new List<(string Frame, int Index, ItalianCount.Chamber Chamber, bool Dagger)>
            {
                ("e7i_italy_night_before_staged", 0, ItalianCount.Chamber.Camera, false),
                ("e7i_italy_night_counting_staged", 61, ItalianCount.Chamber.Camera, false),
                ("e7i_italy_night_counting_senato_staged", 61, ItalianCount.Chamber.Senato, false),
                ("e7i_italy_night_called_staged", called, ItalianCount.Chamber.Camera, false),
                ("e7i_italy_night_called_senato_staged", called, ItalianCount.Chamber.Senato, false),
                ("e7i_italy_night_final_senato_staged", ItalianNight.Total, ItalianCount.Chamber.Senato, false),
                ("e7i_italy_night_dagger_staged", ItalianNight.Total, ItalianCount.Chamber.Camera, true),
            };
            SetPrivateField(controller, "_canvasLive", true);
            foreach ((string frame, int index, ItalianCount.Chamber chamber, bool dagger) in frames)
            {
                if (frame.Contains("_called") && (called <= 0 || called >= ItalianNight.Total))
                {
                    Debug.Log($"SHOT: D-IT {frame} - NOT IN THIS OUTCOME: the chambers are {(called <= 0 ? "never both called before the close" : "called only at the close")} on this count, so there is no called state to film.");
                    _expectedFrames.Remove(frame);
                    continue;
                }
                ElectionNightScreen.ItalyNight night = GameController.ItalianNightInputs(sim, italy, latest, index, chamber);
                if (night == null) { Debug.LogError($"SHOT: D-IT {frame} - the inputs did not build; nothing filmed."); _failed++; continue; }
                night.Staged = "STAGED";
                yield return FilmItalianNight(controller, frame, night, dagger);
            }

            // the dissolution: the column on a planted hostile Senato, restored before the frame
            Dictionary<string, int> saved = italy.SecondChamberSeats != null ? new Dictionary<string, int>(italy.SecondChamberSeats) : null;
            ElectionNightScreen.ItalyNight dissolved = null;
            try
            {
                if (saved != null && saved.Count > 0)
                {
                    string[] cdx = null;
                    foreach (string[] co in ItalianCount.Coalitions(ItalianCount.Chamber.Senato)) { if (Array.IndexOf(co, "FdI") >= 0) { cdx = co; } }
                    int cdxSeats = 0;
                    foreach (string k in cdx ?? new string[0]) { cdxSeats += saved.TryGetValue(k, out int s) ? s : 0; }
                    var planted = new Dictionary<string, int>(saved);
                    int moved = 0;
                    foreach (string k in cdx ?? new string[0])
                    {
                        if (!saved.TryGetValue(k, out int s) || cdxSeats <= 0) { continue; }
                        int keep = (int)Math.Floor(s * 70.0 / cdxSeats);
                        planted[k] = keep;
                        moved += s - keep;
                    }
                    planted.TryGetValue("PD", out int pd);
                    planted["PD"] = pd + moved;
                    italy.SecondChamberSeats.Clear();
                    foreach (KeyValuePair<string, int> kv in planted) { italy.SecondChamberSeats[kv.Key] = kv.Value; }
                    dissolved = GameController.ItalianNightInputs(sim, italy, latest, ItalianNight.Total);
                    Debug.Log("SHOT: D-IT - the planted Senato for the dissolution: " + string.Join(" · ", System.Linq.Enumerable.Select(System.Linq.Enumerable.Where(planted, kv => kv.Value > 0), kv => kv.Key + " " + kv.Value.ToString(CultureInfo.InvariantCulture))) + " | " + (dissolved?.Projection != null ? dissolved.Projection.Describe() + " | " + string.Join(" | ", dissolved.Projection.Steps) : "no projection"));
                }
            }
            catch (Exception e) { Debug.LogError($"SHOT: D-IT - the dissolution's staging THREW ({e.GetType().Name}: {e.Message})"); _failed++; }
            finally
            {
                if (saved != null) { italy.SecondChamberSeats.Clear(); foreach (KeyValuePair<string, int> kv in saved) { italy.SecondChamberSeats[kv.Key] = kv.Value; } }
            }
            if (dissolved == null || dissolved.Projection?.View == null || dissolved.Projection.View.HasGovernment)
            {
                Debug.LogError("SHOT: D-IT e7i_italy_night_dissolution_staged - the planted Senato did not stage three failed mandates" + (dissolved?.Projection != null ? " (" + dissolved.Projection.Describe() + ")" : string.Empty) + "; the dissolution is NOT filmed.");
                _failed++;
            }
            else
            {
                dissolved.Staged = "STAGED";
                yield return FilmItalianNight(controller, "e7i_italy_night_dissolution_staged", dissolved, false);
            }
            PoliSim.Testing.CaptureIdentity.CanvasSurface = null;
            SetPrivateField(controller, "_canvasLive", false);
            yield return Settle();
        }

        /// <summary>D-IT: one staged state of the Italian night, built, filmed with the Canvas guard, logged and destroyed.</summary>
        private IEnumerator FilmItalianNight(GameController controller, string frame, ElectionNightScreen.ItalyNight night, bool dagger)
        {
            PoliSim.Testing.CaptureIdentity.CanvasSurface = "electionnight";
            ElectionNightScreen screen = ElectionNightScreen.BuildItaly(night, null);
            if (screen == null) { Debug.LogError($"SHOT: D-IT {frame} - the board did not build; nothing filmed."); _failed++; yield break; }
            if (dagger) { screen.SetDense(true); }
            yield return Settle();
            Claim("electionnight");
            yield return Capture(frame);
            RecordCanvasTextAssert(frame, controller);
            if (dagger && !screen.DenseShown) { Debug.LogError($"SHOT: {frame} - the † view did not open."); _failed++; }
            LogItalianNight(frame, screen);
            if (screen.Root != null) { UnityEngine.Object.Destroy(screen.Root); }
            yield return Settle();
        }

        /// <summary>D-IT: the film's log line for an Italian night frame - the chamber shown, the cells by state on both maps, the smallest map label in canvas units.</summary>
        private static void LogItalianNight(string frame, ElectionNightScreen screen)
        {
            ItalianCellView cam = screen.ItalyCameraMap, sen = screen.ItalySenatoMap;
            int smallest = int.MaxValue;
            foreach (UnityEngine.UI.Text t in screen.ItalyTexts) { if (t != null) { smallest = Math.Min(smallest, t.fontSize); } }
            Debug.Log(string.Format(CultureInfo.InvariantCulture, "SHOT: D-IT {0} - showing the {1}; Camera cells {2} inked / {3} to come, Senato {4} / {5}; smallest sheet text {6} units, smallest label on the map shown {7} units (14 px at 1280 is 21; the hidden map lays itself when shown){8}",
                frame, screen.ItalyChamber, cam?.LastInked ?? -1, cam?.LastToCome ?? -1, sen?.LastInked ?? -1, sen?.LastToCome ?? -1, smallest == int.MaxValue ? 0 : smallest,
                (screen.ItalyChamber == ItalianCount.Chamber.Camera ? cam : sen)?.LastSmallestLabel ?? 0f, screen.DenseShown ? "; the † view open" : string.Empty));
        }

        /// <summary>
        /// D-PL (Elias, 2026-10-23: "Build the Polish election night"; boards 28a-28d): POLAND'S NIGHTS ON FILM, after the Sejm's own night (e7c).
        /// (1) The Sejm's night part-way - the election the film just held, staged at 30 of 41 okręgi (board 28a's own state): the undeclared okręgi
        /// dashed, the seats owed until their calls land, the seats waiting on an open call in paper - built from that election's own count in a screen
        /// of its own, filmed with the Canvas guard and destroyed; STAGED, named so. (2) The President's nights: the game played on as play steps it (the
        /// day path, the country's day tick, the clock paused while the harness walks) to the first vote the game holds - the night the controller opens
        /// on the day (`CheckPresidentialRound`), filmed; the state between the rounds through the Docket the day after (D-PL2: its item, then the night
        /// its OPEN raises), filmed and dismissed; then on to the run-off and its night. A first vote that elects outright has no run-off, and the film says so.
        /// </summary>
        private IEnumerator CapturePolishNights(GameController controller, SimulationManager sim, Dictionary<CountryId, PolicyDecision> noDecisions)
        {
            Country poland = sim.World?.GetCountry(CountryId.Poland);
            ElectionRecord latest = poland != null && poland.ElectionHistory.Count > 0 ? poland.ElectionHistory[poland.ElectionHistory.Count - 1] : null;
            if (latest == null || latest.Method != ElectionMethod.PolandDistricts)
            {
                Debug.LogError("SHOT: D-PL - no Polish Sejm count in hand, so the night part-way is NOT filmed.");
                _failed++;
                yield break;
            }
            int minute = PolishSejmNight.ArrivalMinute(30, PolishOkregiCartogramCount);
            PolishSejmNight.State partial = PolishSejmNight.At(minute, latest.Shares, latest.OkregShift);   // PL-C: the okregi as counted
            if (partial == null || partial.Complete)
            {
                Debug.LogError($"SHOT: D-PL - the Polish night at minute {minute} is {(partial == null ? "not built" : "already complete")}; the night part-way is NOT filmed.");
                _failed++;
            }
            else
            {
                SetPrivateField(controller, "_canvasLive", true);
                PoliSim.Testing.CaptureIdentity.CanvasSurface = "electionnight";
                ElectionNightScreen staged = ElectionNightScreen.BuildPoland(GameController.PolishSejmNightInputs(poland, partial, latest.Date), latest.Date.Date.AddHours(21), null);
                if (staged == null) { Debug.LogError("SHOT: D-PL - the board did not build for the Polish night part-way; nothing filmed."); _failed++; }
                else
                {
                    yield return Settle();
                    Claim("electionnight");
                    yield return Capture("e7e_election_night_partial_staged");
                    RecordCanvasTextAssert("e7e_election_night_partial_staged", controller);
                    var calls = new List<string>();
                    for (int k = 0; k < partial.Keys.Length; k++) { calls.Add(partial.Keys[k] + " " + partial.Calls[k] + (partial.CalledAt[k] > 0 ? "@" + partial.CalledAt[k].ToString(CultureInfo.InvariantCulture) : string.Empty) + " " + partial.Seats[k].ToString(CultureInfo.InvariantCulture)); }
                    Debug.Log(string.Format(CultureInfo.InvariantCulture, "SHOT: D-PL - the Polish night staged part-way: minute {0}, {1} of {2} okręgi declared; {3}; in play {4}, not yet {5}; cells {6} inked / {7} in play / {8} not yet.",
                        minute, partial.Declared, partial.Total, string.Join(" · ", calls), partial.InPlay, partial.NotYet, staged.Okregi?.LastInked ?? -1, staged.Okregi?.LastInPlay ?? -1, staged.Okregi?.LastNotYet ?? -1));
                    if (staged.Root != null) { UnityEngine.Object.Destroy(staged.Root); }
                }
                PoliSim.Testing.CaptureIdentity.CanvasSurface = null;
                SetPrivateField(controller, "_canvasLive", false);
                yield return Settle();
            }

            // (1b) K302 (the art155 review's R2-M1) and S11 (boards 11a-11c, 11e): the Sejm night in each state the steps can end in, and the announcement plate - staged, see FilmPolishStagedNights
            yield return FilmPolishStagedNights(controller, poland, latest);

            // (2) the President's nights, on their days
            FieldInfo speedField = controller.GetType().GetField("_gameSpeed", BindingFlags.Instance | BindingFlags.NonPublic);
            object speed = speedField?.GetValue(controller);
            SetEnumField(controller, "_gameSpeed", "Paused");
            PresidentialElection.Contest contest = null;
            for (int round = 0; round < 2; round++)
            {
                bool wantRunOff = round == 1;
                if (wantRunOff && (contest == null || !contest.RunOffPending()))
                {
                    if (contest != null && contest.Decided()) { Debug.Log($"SHOT: D-PL - the first vote of {contest.FirstVote:yyyy-MM-dd} elected outright ({contest.Line}); there is no run-off night to film."); }
                    break;
                }
                int days = 0;
                bool found = false;
                while (days < MaxStateSearchDays && !found)
                {
                    bool boundary = sim.AdvanceDay();
                    MemTick(sim);
                    sim.AdvanceCountryDayTick(_countryId);
                    days++;
                    if (boundary) { sim.AdvanceTurn(noDecisions); }
                    // S11 (board 11d): the Sejm's steps on the Docket, live - on the new Sejm's first sitting and on the day its steps conclude
                    SejmFormation sejm = SejmDesk.Record(poland);
                    if (!_sejmSittingFilmed && sejm != null && sim.CurrentDate.Date == sejm.FirstSitting.Date) { _sejmSittingFilmed = true; yield return FilmSejmDocket(controller, sim, poland, "e7s_docket_sejm_first_sitting"); }
                    if (_sejmSittingFilmed && !_sejmConcludedFilmed && sejm != null && !sejm.Open && sejm.Log.Count > 0 && sejm.Log[sejm.Log.Count - 1].StartsWith(sim.CurrentDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), StringComparison.Ordinal))
                    {
                        _sejmConcludedFilmed = true;
                        yield return FilmSejmDocket(controller, sim, poland, "e7t_docket_sejm_steps_concluded");
                    }
                    // PL-C: the presidential campaign's HQ and the run-off's, filmed on this walk - it is the one that crosses 2025 (the walk after the nights
                    // starts past the run-off, and the next first vote is beyond any search)
                    if (!_plcFirstFilmed && sim.PlayerCampaign != null && sim.PlayerCampaign.Setup.Contest == CampaignContest.PresidentialFirstVote && sim.PlayerCampaign.Day >= 1)
                    {
                        _plcFirstFilmed = true;
                        yield return FilmPolishHq(controller, sim, "plc03_presidential_campaign_hq");
                    }
                    if (!_plcRunOffFilmed && sim.PlayerCampaign != null && sim.PlayerCampaign.Setup.Contest == CampaignContest.PresidentialRunOff && sim.PlayerCampaign.Day >= 2)
                    {
                        _plcRunOffFilmed = true;
                        yield return FilmPolishHq(controller, sim, "plc04_runoff_campaign_hq");
                    }
                    foreach (PresidentialElection.Contest c in poland.PresidentialElections)
                    {
                        if (wantRunOff ? c == contest && c.RunOffHeld && c.RunOffOn == sim.CurrentDate : c.FirstVote == sim.CurrentDate) { contest = c; found = true; }
                    }
                }
                if (!found)
                {
                    Debug.LogError($"SHOT: D-PL - no presidential {(wantRunOff ? "run-off" : "first vote")} within {MaxStateSearchDays} days of the Sejm's night; the President's night is NOT filmed.");
                    _failed++;
                    break;
                }
                InvokeNoArg(controller, "CheckPresidentialRound");
                FieldInfo nightField = controller.GetType().GetField("_electionNight", BindingFlags.Instance | BindingFlags.NonPublic);
                if (nightField?.GetValue(controller) == null)
                {
                    Debug.LogError($"SHOT: D-PL - the {(wantRunOff ? "run-off" : "first vote")} of {sim.CurrentDate:yyyy-MM-dd} was held ({contest.Line}) and NO President's night was shown; the controller logs why.");
                    _failed++;
                    break;
                }
                string frame = !wantRunOff ? (contest.Decided() ? "e7p_presidential_election_night_elected_outright" : "e7p_presidential_election_night_first_round")
                    : contest.Decided() ? "e7r_presidential_election_night_elected" : "e7r_presidential_election_night_runoff_undecided";
                yield return WaitForCanvasSettle(controller, wantActive: true);
                yield return Settle();
                Claim("electionnight");
                yield return Capture(frame);
                RecordCanvasTextAssert(frame, controller);
                Debug.Log($"SHOT: D-PL - the President's night of {sim.CurrentDate:yyyy-MM-dd} after {days} days: {contest.Line}");
                InvokeNoArg(controller, "DismissElectionNight");
                yield return WaitForCanvasSettle(controller, wantActive: false);
                yield return Settle();

                if (!wantRunOff && contest.RunOffPending())
                {
                    // D-PL2: the state between the rounds (28c frame 2) through its REAL entry - the day after the first vote, played as play steps it, the
                    // Docket's item between the rounds filmed, its OPEN taken (the field the docket's own action writes), the night the controller's loop
                    // opens filmed with the run-off counted down from that day, then dismissed by its CONTINUE
                    bool next = sim.AdvanceDay();
                    MemTick(sim);
                    sim.AdvanceCountryDayTick(_countryId);
                    if (next) { sim.AdvanceTurn(noDecisions); }
                    FieldInfo tabField = controller.GetType().GetField("_consolidatedTab", BindingFlags.Instance | BindingFlags.NonPublic);
                    FieldInfo deskField = controller.GetType().GetField("_onDesk", BindingFlags.Instance | BindingFlags.NonPublic);
                    object tabWas = tabField?.GetValue(controller), deskWas = deskField?.GetValue(controller);
                    // the review's H1: a tab set by field alone while _onDesk is true films the DESK - the Docket is a tab, so the desk is left first
                    SetPrivateField(controller, "_onDesk", false);
                    SetEnumField(controller, "_consolidatedTab", "Decisions");
                    ResetScrolls(controller);
                    yield return Settle();
                    int toRunOff = (int)Math.Round((contest.RunOffOn.Date - sim.CurrentDate.Date).TotalDays);
                    // the review's M1: the item is asserted on the docket before its frame is taken under its name - the anchor its draw registers
                    object slipBook = controller.GetType().GetField("_docketSlipBook", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(controller);
                    bool itemDrawn = slipBook is PeopleSlips.Book drawnBook && drawnBook.Anchors.ContainsKey("docket:president:between");
                    object nightOnBoard = null;
                    if (!itemDrawn)
                    {
                        Debug.LogError($"SHOT: D-PL2 - the Docket on {sim.CurrentDate:yyyy-MM-dd} drew NO item between the rounds (the contest: first vote {contest.FirstVote:yyyy-MM-dd}, run-off {contest.RunOffOn:yyyy-MM-dd}, pending {contest.RunOffPending()}, held {contest.RunOffHeld}, decided {contest.Decided()}; on the desk {deskField?.GetValue(controller)}, tab {tabField?.GetValue(controller)}); the docket frame and the night are NOT filmed.");
                        _failed++;
                    }
                    else
                    {
                        Claim("imgui");
                        yield return Capture("e7q_docket_between_the_rounds");
                        SetPrivateField(controller, "_docketOpenItem", "president:between");
                        yield return Settle();
                        nightOnBoard = controller.GetType().GetField("_electionNight", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(controller);
                        if (nightOnBoard == null)
                        {
                            Debug.LogError($"SHOT: D-PL2 - the Docket's OPEN between the rounds on {sim.CurrentDate:yyyy-MM-dd} raised NO night (the item open {controller.GetType().GetField("_docketOpenItem", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(controller) ?? "no"}, the request {controller.GetType().GetField("_presidentBetweenAsked", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(controller)}); an ELECTION: line above names a refusal.");
                            _failed++;
                            SetPrivateField(controller, "_docketOpenItem", null);
                        }
                    }
                    if (nightOnBoard != null)
                    {
                        yield return WaitForCanvasSettle(controller, wantActive: true);
                        yield return Settle();
                        Claim("electionnight");
                        yield return Capture("e7q_presidential_election_night_between");
                        RecordCanvasTextAssert("e7q_presidential_election_night_between", controller);
                        Debug.Log($"SHOT: D-PL2 - the night between the rounds opened from the Docket on {sim.CurrentDate:yyyy-MM-dd}: the run-off {contest.RunOffOn:yyyy-MM-dd}, in {toRunOff} days");
                        InvokeNoArg(controller, "DismissElectionNight");
                        yield return WaitForCanvasSettle(controller, wantActive: false);
                        yield return Settle();
                    }
                    if (tabWas != null) { tabField.SetValue(controller, tabWas); }
                    if (deskWas != null) { deskField.SetValue(controller, deskWas); }
                    ResetScrolls(controller);
                }
            }
            if (!_sejmSittingFilmed) { Debug.LogError("SHOT: S11 e7s_docket_sejm_first_sitting - MISSING: the walk after the night never reached the new Sejm's first sitting"); _failed++; }
            if (!_sejmConcludedFilmed) { Debug.LogError("SHOT: S11 e7t_docket_sejm_steps_concluded - MISSING: the walk after the night never reached the day the Sejm's steps concluded"); _failed++; }
            if (speed != null) { speedField.SetValue(controller, speed); }
        }

        /// <summary>K302 (the art155 review's R2-M1): the frame <see cref="FilmPolishStagedNight"/> films for the refusal - S11 keeps its name.</summary>
        private const string PolishRefusalFrame = "e7e2_election_night_art155_refusal_staged";

        /// <summary>
        /// S11 (Design's return of 2026-10-30, boards 11a-11c, 11e): THE SEJM NIGHT'S STATES, STAGED - each of the steps the formation can end at, and the none
        /// case, on the board of the election the film just held: (e7e2) the refusal, PiS 230 / KO 230 (no cabinet carries even a simple majority, the President's
        /// cabinet refused, the term shortened - K302's frame), and after it the announcement plate (e7e6) built from that night's steps; (e7e3) the President's step,
        /// PiS 240 (the record's designee carries); (e7e4) the Sejm's step on the record's own seats (`WorldClock.TryReference`: the designee refused, the record's
        /// cabinet elected); (e7e5) the President's second step on `SejmFormationDiagnostic` (h)'s simple-majority chamber (KO 83, Konf 10, NL 35, PiS 220, TD 112).
        /// </summary>
        private IEnumerator FilmPolishStagedNights(GameController controller, Country poland, ElectionRecord latest)
        {
            yield return FilmPolishStagedNight(controller, latest, PolishRefusalFrame, Plant(("PiS", 230), ("KO", 230)), 3, false, "e7e6_snap_election_plate_staged");
            yield return FilmPolishStagedNight(controller, latest, "e7e3_election_night_president_step_staged", Plant(("PiS", 240), ("KO", 120), ("TD", 50), ("NL", 30), ("Konf", 20)), 1, true, null);
            Dictionary<string, int> record = WorldClock.TryReference(CountryId.Poland, latest.Date, out WorldClock.Reference history) && history.Seats != null ? new Dictionary<string, int>(history.Seats) : null;
            if (record == null) { Debug.LogError("SHOT: S11 e7e4_election_night_sejm_step_staged - the record holds no seats for this election; NOT filmed."); _failed++; }
            else { yield return FilmPolishStagedNight(controller, latest, "e7e4_election_night_sejm_step_staged", record, 2, true, null); }
            yield return FilmPolishStagedNight(controller, latest, "e7e5_election_night_second_step_staged", Plant(("KO", 83), ("Konf", 10), ("NL", 35), ("PiS", 220), ("TD", 112)), 3, true, null);
        }

        private static Dictionary<string, int> Plant(params (string Key, int Seats)[] seats)
        {
            var d = new Dictionary<string, int>();
            foreach ((string key, int n) in seats) { d[key] = n; }
            return d;
        }

        /// <summary>
        /// K302 (the art155 review's R2-M1) and S11: ONE STAGED SEJM NIGHT - the verdict the controller itself resolves (`GameController.ResolveSejmVerdict`: the
        /// projection, the steps read against it, the foot's clauses) for a <paramref name="plant"/>ed chamber (every other list zero), drawn on the board of the
        /// election the film just held at its final minute, its seats the plant's so the frame reads as one chamber. The chamber is planted on the seated country for
        /// the verdict's own projection and put back, with every field the verdict writes, in a finally before anything yields - no day, screen or check reads the
        /// plant. A night whose steps do not end where the frame's name says (<paramref name="wantStep"/>, <paramref name="wantExpected"/>), or that drew only the
        /// projection's end, is NOT filmed under that name and fails the film. Where <paramref name="plateFrame"/> is named and none is expected, the announcement
        /// plate (`GameController.SejmPlate.cs`) is staged from the same steps over the Docket and filmed, then cleared. Silent: nothing here fires a cue.
        /// </summary>
        private IEnumerator FilmPolishStagedNight(GameController controller, ElectionRecord latest, string frame, Dictionary<string, int> plant, int wantStep, bool wantExpected, string plateFrame)
        {
            const BindingFlags Own = BindingFlags.Instance | BindingFlags.NonPublic;
            FieldInfo verdictField = typeof(GameController).GetField("_pendingElectionVerdict", Own), endsField = typeof(GameController).GetField("_pendingElectionVerdictEndsGame", Own);
            FieldInfo stepsField = typeof(GameController).GetField("_pendingSejmSteps", Own), footField = typeof(GameController).GetField("_pendingSejmFoot", Own);
            FieldInfo seatedField = typeof(GameController).GetField("_playerCountry", Own);
            MethodInfo resolve = typeof(GameController).GetMethod("ResolveSejmVerdict", Own);
            Country seated = seatedField?.GetValue(controller) as Country;
            if (verdictField == null || endsField == null || stepsField == null || footField == null || resolve == null || seated == null || seated.Id != CountryId.Poland)
            {
                Debug.LogError($"SHOT: S11 {frame} - the controller's verdict hooks or its seated Poland were not found; NOT filmed.");
                _failed++;
                yield break;
            }
            Dictionary<string, int> seatsWas = seated.ParliamentSeats;
            object verdictWas = verdictField.GetValue(controller), endsWas = endsField.GetValue(controller), stepsWas = stepsField.GetValue(controller), footWas = footField.GetValue(controller);
            string verdict = null;
            SejmNightSteps steps = null;
            SejmFoot foot = null;
            var planted = new Dictionary<string, int>();
            try
            {
                foreach (string key in seatsWas.Keys) { planted[key] = 0; }
                foreach (KeyValuePair<string, int> kv in plant) { planted[kv.Key] = kv.Value; }
                seated.ParliamentSeats = planted;
                resolve.Invoke(controller, new object[] { latest, GovernmentFormation.Formed.None("staged for the S11 frame"), SejmFormation.OpenAfter(seated, latest.Date) });
                verdict = verdictField.GetValue(controller) as string;
                steps = stepsField.GetValue(controller) as SejmNightSteps;
                foot = footField.GetValue(controller) as SejmFoot;
            }
            catch (Exception e)
            {
                Exception inner = e.InnerException ?? e;
                Debug.LogError($"SHOT: S11 {frame} - the verdict's resolution THREW ({inner.GetType().Name}: {inner.Message}); NOT filmed.");
                _failed++;
            }
            finally
            {
                seated.ParliamentSeats = seatsWas;
                verdictField.SetValue(controller, verdictWas);
                endsField.SetValue(controller, endsWas);
                stepsField.SetValue(controller, stepsWas);
                footField.SetValue(controller, footWas);
            }
            if (verdict == null || steps == null || foot == null) { if (verdict != null) { Debug.LogError($"SHOT: S11 {frame} - the verdict resolved with no steps or no foot; NOT filmed."); _failed++; } yield break; }
            string plantLine = string.Join(" · ", new List<string>(PlantWords(plant)));
            if (steps.Partial || steps.EndStep != wantStep || steps.Expected != wantExpected)
            {
                Debug.LogError($"SHOT: S11 {frame} - the planted chamber ({plantLine}) ends at step {steps.EndStep} (expected {steps.Expected}, partial {steps.Partial}), not step {wantStep} (expected {wantExpected}) - \"{verdict}\"; NOT filmed under this name.");
                _failed++;
                yield break;
            }
            if (!wantExpected && !verdict.Contains("Art. 155 ust. 2"))
            {
                Debug.LogError($"SHOT: S11 {frame} - the planted chamber's verdict names no shortened term (\"{verdict}\"); NOT filmed under the refusal's name.");
                _failed++;
                yield break;
            }
            PolishSejmNight.State final = PolishSejmNight.At(PolishSejmNight.FinalMinute, latest.Shares, latest.OkregShift);
            if (final == null)
            {
                Debug.LogError($"SHOT: S11 {frame} - the election's night did not build at its final minute; NOT filmed.");
                _failed++;
                yield break;
            }
            for (int k = 0; k < final.Keys.Length; k++) { final.Seats[k] = planted.TryGetValue(final.Keys[k], out int s) ? s : 0; }   // the plant's seats, so the board reads as the chamber the steps ran on
            ElectionNightScreen.SejmNight night = GameController.PolishSejmNightInputs(seated, final, latest.Date);
            night.Steps = steps;
            night.Foot = foot;
            SetPrivateField(controller, "_canvasLive", true);
            PoliSim.Testing.CaptureIdentity.CanvasSurface = "electionnight";
            ElectionNightScreen staged = ElectionNightScreen.BuildPoland(night, latest.Date.Date.AddHours(21), verdict);
            if (staged == null) { Debug.LogError($"SHOT: S11 {frame} - the board did not build; nothing filmed."); _failed++; }
            else
            {
                yield return Settle();
                Claim("electionnight");
                yield return Capture(frame);
                RecordCanvasTextAssert(frame, controller);
                string shown = null;
                foreach (UnityEngine.UI.Text t in staged.Root.GetComponentsInChildren<UnityEngine.UI.Text>(true)) { if (t.name == "Verdict") { shown = t.text; } }
                SejmNightSteps.Step d = steps.Deciding;
                var cells = new List<string>();
                for (int i = 0; i < 3; i++) { SejmNightSteps.Step st = steps.Steps[i]; cells.Add((i + 1).ToString(CultureInfo.InvariantCulture) + ":" + (st.Voted ? (st.Asked ?? "?") + (st.Carried ? " carried " : " refused ") + st.For.ToString(CultureInfo.InvariantCulture) + "/" + st.Members.ToString(CultureInfo.InvariantCulture) : "not put")); }
                Debug.Log(string.Format(CultureInfo.InvariantCulture, "SHOT: S11 {0} - STAGED: the chamber {1} (planted for its projection, put back) on the board of the {2:yyyy-MM-dd} count; ends at step {3} of 3 ({4}); cells {5}; the vote {6}; the snap election {7}; the caretaker {8}; the foot shows \"{9}\" ({10} counted words), the whole verdict in its slip.",
                    frame, plantLine, latest.Date, steps.EndStep, steps.Expected ? "expected: " + string.Join("+", d.CabinetParties) : "none expected", string.Join(" | ", cells),
                    d == null ? "none" : string.Format(CultureInfo.InvariantCulture, "{0} for, {1} against, {2} abstaining of {3}", d.For, d.Against, d.Abstaining, d.Members),
                    steps.SnapPollingDay == DateTime.MinValue ? "-" : steps.SnapPollingDay.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), steps.CaretakerParty ?? "-", shown ?? "NO VERDICT LINE", SejmNightSteps.CountedWords(shown)));
                if (shown == null) { Debug.LogError($"SHOT: S11 {frame} - the board drew no Verdict line."); _failed++; }
                else if (shown.Contains("…")) { Debug.LogError($"SHOT: S11 {frame} - the foot was CUT (\"{shown}\") - a clause drops whole, never cut (11b)."); _failed++; }
                if (staged.Root != null) { UnityEngine.Object.Destroy(staged.Root); }
            }
            PoliSim.Testing.CaptureIdentity.CanvasSurface = null;
            SetPrivateField(controller, "_canvasLive", false);
            yield return Settle();

            if (plateFrame == null) { yield break; }
            SejmDesk.Plate plate = SejmDesk.PlateOf(steps);
            if (plate == null) { Debug.LogError($"SHOT: S11 {plateFrame} - the night's steps give no plate (none expected {!steps.Expected}, the snap day {steps.SnapPollingDay:yyyy-MM-dd}); NOT filmed."); _failed++; yield break; }
            FieldInfo tabField = controller.GetType().GetField("_consolidatedTab", Own), deskField = controller.GetType().GetField("_onDesk", Own);
            object tabWas = tabField?.GetValue(controller), deskWas = deskField?.GetValue(controller);
            SetPrivateField(controller, "_onDesk", false);
            SetEnumField(controller, "_consolidatedTab", "Decisions");
            ResetScrolls(controller);
            SetPrivateField(controller, "_sejmPlateStaged", plate);
            yield return Settle();
            Claim("imgui");
            yield return Capture(plateFrame);
            Debug.Log(string.Format(CultureInfo.InvariantCulture, "SHOT: S11 {0} - STAGED from {1}'s steps over the Docket: the term shortened {2:yyyy-MM-dd}, polling day {3:yyyy-MM-dd} ({4} days on), the caretaker {5}; the steps {6}; the campaign opens {7:yyyy-MM-dd}.",
                plateFrame, frame, plate.OrderedOn, plate.PollingDay, (int)(plate.PollingDay - plate.OrderedOn).TotalDays, plate.Caretaker ?? "-",
                string.Join(" | ", Array.ConvertAll(plate.Steps, st => st.Name + " " + (st.Article ?? "-") + " " + (st.Result ?? "-") + " " + (st.Day == DateTime.MinValue ? "-" : st.Day.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)))),
                SejmDesk.SnapCampaign(plate.OrderedOn, plate.PollingDay).CampaignStart));
            SetPrivateField(controller, "_sejmPlateStaged", null);
            if (tabWas != null) { tabField.SetValue(controller, tabWas); }
            if (deskWas != null) { deskField.SetValue(controller, deskWas); }
            ResetScrolls(controller);
            yield return Settle();
        }

        private static IEnumerable<string> PlantWords(Dictionary<string, int> plant)
        {
            foreach (KeyValuePair<string, int> kv in plant) { yield return kv.Key + " " + kv.Value.ToString(CultureInfo.InvariantCulture); }
        }

        /// <summary>S11 (board 11d): whether the walk after the night filmed the Docket on the Sejm's first sitting and on the day its steps concluded.</summary>
        private bool _sejmSittingFilmed, _sejmConcludedFilmed;

        /// <summary>
        /// S11 (board 11d): THE SEJM'S STEPS ON THE DOCKET, LIVE - on the walk after the night (the day path, as play steps it), the Docket filmed on the new
        /// Sejm's first sitting (e7s: the first entry, place 4's next step, place 6's Caretaker) and on the day its steps conclude (e7t: every step's entry, newest on
        /// top, and the government that formed - or the shortened term). The game's own record, nothing staged. The docket is asserted to hold the steps' entries
        /// before the frame is taken under its name.
        /// </summary>
        private IEnumerator FilmSejmDocket(GameController controller, SimulationManager sim, Country poland, string frame)
        {
            const BindingFlags Own = BindingFlags.Instance | BindingFlags.NonPublic;
            FieldInfo tabField = controller.GetType().GetField("_consolidatedTab", Own), deskField = controller.GetType().GetField("_onDesk", Own);
            object tabWas = tabField?.GetValue(controller), deskWas = deskField?.GetValue(controller);
            SetPrivateField(controller, "_onDesk", false);
            SetEnumField(controller, "_consolidatedTab", "Decisions");
            ResetScrolls(controller);
            yield return Settle();
            object slipBook = controller.GetType().GetField("_docketSlipBook", Own)?.GetValue(controller);
            bool drawn = false;
            if (slipBook is PeopleSlips.Book book) { foreach (string key in book.Anchors.Keys) { if (key.StartsWith("docket:sejm:", StringComparison.Ordinal)) { drawn = true; break; } } }
            SejmFormation f = SejmDesk.Record(poland);
            List<SejmDesk.Entry> entries = SejmDesk.Entries(f);
            if (!drawn)
            {
                Debug.LogError($"SHOT: S11 {frame} - the Docket on {sim.CurrentDate:yyyy-MM-dd} drew NO entry of the Sejm's steps (the record: {(f == null ? "none" : f.Stage + ", " + f.Log.Count + " line(s)")}); NOT filmed.");
                _failed++;
            }
            else
            {
                Claim("imgui");
                yield return Capture(frame);
                var lines = new List<string>();
                for (int i = entries.Count - 1; i >= 0; i--) { lines.Add(entries[i].Day.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) + " " + entries[i].Label + (entries[i].Line != null ? " / " + entries[i].Line : string.Empty)); }
                TopStrip.Chip chip = TopStrip.SejmStepChip(poland, sim.CurrentDate), standing = TopStrip.StandingChip(poland, sim.CurrentDate);
                Debug.Log($"SHOT: S11 {frame} - the Docket on {sim.CurrentDate:yyyy-MM-dd}, the procedure {f?.Stage.ToString() ?? "none"}: {entries.Count} step entr(ies), newest first: {string.Join(" | ", lines)}; place 4 \"{chip?.Words ?? "-"}\", place 6 \"{standing?.Words ?? "-"}\"; the government {string.Join("+", poland.Government?.Cabinet ?? new List<string>())} led by {poland.Government?.PmParty ?? "-"}{(poland.Government != null && poland.Government.Caretaker ? " (caretaker)" : string.Empty)}.");
            }
            if (tabWas != null) { tabField.SetValue(controller, tabWas); }
            if (deskWas != null) { deskField.SetValue(controller, deskWas); }
            ResetScrolls(controller);
            yield return Settle();
        }

        /// <summary>
        /// D-PL: THE POLISH INTERRUPTS FILM - a Polish game played from its start as play steps it (the day path, the country's day tick, the clock paused
        /// while the harness walks) to its polling day, the Sejm's night opened by the controller's own election call and held as a takeover the clock
        /// cannot pass, dismissed by its CONTINUE; then `CapturePolishNights`. PL-C: on the way the Sejm campaign's HQ is filmed - the run-up's first day
        /// (`plc01_sejm_runup_hq`) and the campaign's second (`plc02_sejm_campaign_hq`), the cap stated beside the chest - and on the nights' walk through 2025
        /// the presidential campaign's HQ (`plc03_presidential_campaign_hq`, on the wojewodztwa) and the run-off's (`plc04_runoff_campaign_hq`, the two
        /// finalists alone; out of reach where the film's party backs neither).
        /// </summary>
        private IEnumerator CapturePolishInterrupts(GameController controller, SimulationManager sim, Dictionary<CountryId, PolicyDecision> noDecisions)
        {
            Debug.Log("SHOT: -shotinterrupts - Poland's campaign (PL-C): the film walks to the polling day, filming the Sejm campaign's HQ on the way, then the nights, then the presidential campaign's HQ and the run-off's.");
            Expect("plc01_sejm_runup_hq", "plc02_sejm_campaign_hq", "plc03_presidential_campaign_hq", "plc04_runoff_campaign_hq");
            // S4 (Design's return of 2026-10-30): the re-laid HQ's new states, staged on the live campaign and put back after each frame
            Expect("plc01b_sejm_office_picker_staged", "plc01c_sejm_office_moved_staged", "plc02b_sejm_regional_poll_staged", "plc02c_sejm_against_composer_staged",
                "plc02d_sejm_final_week_phones_staged");
            bool runUpFilmed = false, campaignFilmed = false, finalWeekFilmed = false;
            SetPrivateField(controller, "_onDesk", true);
            FieldInfo walkSpeedField = controller.GetType().GetField("_gameSpeed", BindingFlags.Instance | BindingFlags.NonPublic);
            object walkSpeed = walkSpeedField?.GetValue(controller);
            SetEnumField(controller, "_gameSpeed", "Paused");
            int days = 0;
            while (days < MaxStateSearchDays)
            {
                bool boundary = sim.AdvanceDay();
                MemTick(sim);
                sim.AdvanceCountryDayTick(_countryId);
                days++;
                if (boundary) { sim.AdvanceTurn(noDecisions); }
                // PL-C: the Sejm campaign's HQ, once each - the run-up's first day and the campaign's second
                if (!runUpFilmed && sim.PlayerPreCampaign != null && sim.PlayerCampaign == null) { runUpFilmed = true; yield return FilmPolishHq(controller, sim, "plc01_sejm_runup_hq"); yield return FilmHqOfficePicker(controller, sim); }
                if (!campaignFilmed && sim.PlayerCampaign != null && sim.PlayerCampaign.Day >= 1) { campaignFilmed = true; yield return FilmPolishHq(controller, sim, "plc02_sejm_campaign_hq"); yield return FilmHqRegionalPollAndAgainst(controller, sim); }
                // S4: PHONES opens with the final week - the day the queue steps on is inside it (the run's own gate)
                if (!finalWeekFilmed && sim.PlayerCampaign != null && !sim.PlayerCampaign.Finished && sim.PlayerCampaign.Setup.Contest == CampaignContest.Chamber
                    && sim.PlayerCampaign.Setup.Calendar.DaysUntilElection(sim.PlayerCampaign.Today) <= GotvTurnout.PhoneFinalWeekDays)
                {
                    finalWeekFilmed = true;
                    yield return FilmHqStaged(controller, "plc02d_sejm_final_week_phones_staged", () =>
                    {
                        controller.StageHqComposerForFilm(CampaignActionKind.PhoneBank, false, 1, 0);
                        return string.Format(CultureInfo.InvariantCulture, "the final week - the queue steps on {0:yyyy-MM-dd}, {1} day(s) before polling day; PHONES open in the composer", sim.PlayerCampaign.Today,
                            sim.PlayerCampaign.Setup.Calendar.DaysUntilElection(sim.PlayerCampaign.Today));
                    }, () => controller.StageHqComposerForFilm(null, false, 1, 0));
                }
                if (sim.PollingDayToday) { break; }
            }
            if (!runUpFilmed) { Debug.LogError("SHOT: PL-C plc01_sejm_runup_hq - MISSING: no run-up began before the polling day (the player's party is in no Polish cast?)"); _failed++; }
            if (!campaignFilmed) { Debug.LogError("SHOT: PL-C plc02_sejm_campaign_hq - MISSING: no Sejm campaign ran before the polling day"); _failed++; }
            if (!finalWeekFilmed) { Debug.LogError("SHOT: S4 plc02d_sejm_final_week_phones_staged - MISSING: the Sejm campaign never reached its final week on the walk"); _failed++; }
            bool over = false;
            if (!sim.PollingDayToday)
            {
                Debug.LogError($"SHOT: -shotinterrupts found no polling day within {MaxStateSearchDays} days for Poland - the night is NOT filmed.");
                _failed++;
            }
            else
            {
                Debug.Log($"SHOT: -shotinterrupts reached Poland's polling day {sim.CurrentDate:yyyy-MM-dd} after {days} days.");
                InvokeNoArg(controller, "CheckElection");
                if (walkSpeed != null) { walkSpeedField.SetValue(controller, walkSpeed); }
                FieldInfo nightField = controller.GetType().GetField("_electionNight", BindingFlags.Instance | BindingFlags.NonPublic);
                if (nightField?.GetValue(controller) == null)
                {
                    Debug.LogError("SHOT: -shotinterrupts - Poland reached its election and NO election night was shown. The controller logs why (ELECTION: / CANVAS: lines above).");
                    _failed++;
                }
                else
                {
                    yield return WaitForCanvasSettle(controller, wantActive: true);
                    yield return Settle();
                    Claim("electionnight");
                    yield return Capture("e7c_election_night_takeover");
                    RecordCanvasTextAssert("e7c_election_night_takeover", controller);
                    yield return AssertClockHeld(controller, sim, "election night");
                    InvokeNoArg(controller, "DismissElectionNight");
                    yield return WaitForCanvasSettle(controller, wantActive: false);
                    yield return Settle();
                    Claim("imgui");
                    FieldInfo overField = controller.GetType().GetField("_isGameOver", BindingFlags.Instance | BindingFlags.NonPublic);
                    over = overField?.GetValue(controller) is bool o && o;
                    yield return Capture(over ? "e7d_after_election_night_gameover" : "e7d_after_election_night");
                    if (!over)
                    {
                        yield return ReportClockAfterDismissal(controller, sim, "election night");
                        yield return CapturePolishNights(controller, sim, noDecisions);
                        yield return CapturePolishPresidentialHq(controller, sim, noDecisions);   // PL-C: the presidential campaign and its run-off
                    }
                }
            }
            Debug.Log($"SHOT: interrupts done, {_captured} captured, {_failed} failed.");
            Debug.Log($"SHOT: {ReportOverflows()} text overflow(s) recorded.");
            Debug.Log($"SHOT: {ReportContainmentEscapes()} containment escape(s) recorded.");
            Debug.Log($"SHOT: {_canvasTextViolations} canvas text violation(s) recorded across {_canvasTextAsserts} assert(s).");   // S11: the Canvas guard's count on the summary, and it fails the film
            Finish(_failed == 0 && _loggedErrors == 0 && _canvasTextViolations == 0 ? 0 : 1);
        }

        /// <summary>PL-C: whether the Polish nights' walk filmed the presidential campaign's HQ and the run-off's.</summary>
        private bool _plcFirstFilmed, _plcRunOffFilmed;

        /// <summary>PL-C: the live campaign's HQ opened through the rail's own branch, filmed as <paramref name="frame"/> with the staging's cap in the log, then closed
        /// back to the Desk.</summary>
        private IEnumerator FilmPolishHq(GameController controller, SimulationManager sim, string frame)
        {
            CampaignRun.Setup? staged = sim.PlayerCampaign?.Setup ?? sim.PlayerPreCampaign?.Staged;
            Debug.Log(string.Format(CultureInfo.InvariantCulture, "SHOT: PL-C {0} on {1:yyyy-MM-dd} - the {2}, {3} regions, the player's cap: {4}", frame, sim.CurrentDate,
                staged.HasValue ? staged.Value.Contest.ToString() : "(no staging)", staged.HasValue ? staged.Value.Regions.Length : 0, LiveCampaignSetup.SpendingLimitLine(staged, sim.PlayerPartyIndexForCampaign()) ?? "none"));
            UiGuardContext.CurrentScreen = frame;   // S4: the settle frames under their own name, never the frame before
            InvokeNoArg(controller, "OpenLiveCampaign");
            yield return Settle();
            Claim("imgui");
            yield return Capture(frame);
            InvokeNoArg(controller, "CloseLiveCampaign");
            SetPrivateField(controller, "_onDesk", true);
            yield return Settle();
        }

        /// <summary>
        /// S4 (Design's return of 2026-10-30): the live HQ opened through the rail's own branch, STAGED by <paramref name="stage"/> (which says what it staged, for
        /// the log's `SHOT: S4 ... STAGED:` line), filmed as <paramref name="frame"/>, put back by <paramref name="restore"/> and closed to the Desk - the way the
        /// `_staged` shots stage a state and leave the game as they found it.
        /// </summary>
        private IEnumerator FilmHqStaged(GameController controller, string frame, Func<string> stage, Action restore)
        {
            UiGuardContext.CurrentScreen = frame;   // the settle frames under their own name, never the frame before (the guard filed the HQ's first frames under 05t)
            InvokeNoArg(controller, "OpenLiveCampaign");
            yield return Settle();
            string what = stage != null ? stage() : "nothing";
            yield return Settle();
            Claim("imgui");
            Debug.Log("SHOT: S4 " + frame + " - STAGED: " + what);
            yield return Capture(frame);
            restore?.Invoke();
            InvokeNoArg(controller, "CloseLiveCampaign");
            SetPrivateField(controller, "_onDesk", true);
            yield return Settle();
        }

        /// <summary>S4 item 5: OFFICE turns the map into the picker - the composer opened on OFFICE with the rule's site (NEXT on the largest electorate without
        /// one), then a click on another tile moving it; the pick and the composer put back.</summary>
        private IEnumerator FilmHqOfficePicker(GameController controller, SimulationManager sim)
        {
            PreCampaignRun.State pre = sim.PlayerPreCampaign;
            if (pre == null) { Debug.LogError("SHOT: S4 plc01b_sejm_office_picker_staged - MISSING: no run-up to site an office in"); _failed++; yield break; }
            int site = LiveCampaignSnapshot.NextOfficeSite(pre, -1, out _);
            yield return FilmHqStaged(controller, "plc01b_sejm_office_picker_staged", () =>
            {
                controller.ComposeForFilm(CampaignActionKind.EstablishOffice);
                return "OFFICE open in the composer - the map is the picker, NEXT on " + (site >= 0 ? pre.Staged.Regions[site].Name : "none") + " (the largest electorate without an office)";
            }, () => controller.ComposeForFilm(null));
            int other = -1;
            for (int r = 0; r < pre.Staged.Regions.Length; r++) { if (r != site && pre.Staged.StandsIn(pre.Party, r) && !pre.HasOffice(r)) { other = r; break; } }
            MethodInfo pick = controller.GetType().GetMethod("PickCampaignRegion", BindingFlags.Instance | BindingFlags.NonPublic);
            yield return FilmHqStaged(controller, "plc01c_sejm_office_moved_staged", () =>
            {
                controller.ComposeForFilm(CampaignActionKind.EstablishOffice);
                if (other >= 0) { pick?.Invoke(controller, new object[] { other }); }
                return "a click on another tile moved the next office to " + (other >= 0 ? pre.Staged.Regions[other].Name : "none");
            }, () => { controller.ComposeForFilm(null); SetPrivateField(controller, "_campaignPickedRegion", -1); });
        }

        /// <summary>
        /// S4 items 1 and 7 and the regional surfaces: regional polls PLANTED in the run's record for the film - the target region, the region whose top two are
        /// closest (a too-close tile beside a polled one) and the one with the widest lead - each the region's truth at the prior's offset (`RegionalPolling.TruthIn`
        /// on the run's true preference, no campaign shift) read WITHOUT sampling noise, its ± the regional poll's at its sample: DECLARED a staging (no random
        /// stream is drawn, no vote moves), removed after the frame. Then the composer on a rally with AGAINST picked, ×2, the topic stepped once - put back.
        /// </summary>
        private IEnumerator FilmHqRegionalPollAndAgainst(GameController controller, SimulationManager sim)
        {
            CampaignRun.State run = sim.PlayerCampaign;
            int me = sim.PlayerPartyIndexForCampaign();
            if (run == null || me < 0 || run.RegionalPolls == null || run.Setup.RegionalOffset == null)
            {
                Debug.LogError("SHOT: S4 plc02b_sejm_regional_poll_staged - MISSING: " + (run == null ? "no campaign" : me < 0 ? "no player party in it" : "no regional poll is on sale in this staging"));
                _failed++;
                yield break;
            }
            object targetObject = controller.GetType().GetMethod("LocalActRegion", BindingFlags.Instance | BindingFlags.NonPublic)?.Invoke(controller, null);
            int target = targetObject is int t ? t : -1;
            var truths = new Dictionary<int, double[]>();
            int closest = -1, widest = -1;
            double closestGap = double.MaxValue, widestGap = -1.0;
            for (int r = 0; r < run.Setup.Regions.Length; r++)
            {
                double[] truth = RegionalPolling.TruthIn(run.Setup, run.TruePreference, null, null, r);
                if (truth == null) { continue; }
                truths[r] = truth;
                double first = 0.0, second = 0.0;
                foreach (double v in truth) { if (v > first) { second = first; first = v; } else if (v > second) { second = v; } }
                if (r == target) { continue; }
                if (first - second < closestGap) { closestGap = first - second; closest = r; }
                if (first - second > widestGap) { widestGap = first - second; widest = r; }
            }
            var planted = new List<int>();
            foreach (int r in new[] { target, closest, widest })
            {
                if (r < 0 || planted.Contains(r) || !truths.ContainsKey(r)) { continue; }
                double[] shares = truths[r];
                var moe = new double[shares.Length];
                for (int i = 0; i < moe.Length; i++) { moe[i] = RegionalPolling.MarginOfErrorPp(shares[i], RegionalPolling.Sample); }
                System.DateTime fielded = run.Setup.Calendar.CampaignStart.AddDays(System.Math.Max(0, run.Day - 1));
                run.RegionalPolls[me].Add((System.Math.Max(0, run.Day - 1), r, new Poll(fielded, RegionalPolling.Sample, "Regional poll · " + run.Setup.Regions[r].Name + " (staged)", shares, moe)));
                planted.Add(r);
            }
            void Unplant() { int n = planted.Count; List<(int Day, int Region, Poll Poll)> filed = run.RegionalPolls[me]; filed.RemoveRange(filed.Count - n, n); }
            yield return FilmHqStaged(controller, "plc02b_sejm_regional_poll_staged",
                () => "regional polls planted in " + string.Join(", ", planted.ConvertAll(r => run.Setup.Regions[r].Name).ToArray()) + " (the target " + (target >= 0 ? run.Setup.Regions[target].Name : "none")
                    + "; the closest top two, gap " + (closest >= 0 ? (100.0 * closestGap).ToString("0.0", CultureInfo.InvariantCulture) : "-") + " pts; the widest lead) - the regions' truths read without sampling noise",
                null);
            yield return FilmHqStaged(controller, "plc02c_sejm_against_composer_staged", () =>
            {
                int rival = controller.StageHqComposerForFilm(CampaignActionKind.Rally, true, 2, 1);
                return "a rally ×2 in the composer, AGAINST " + (rival >= 0 ? run.Setup.Parties[rival].Name : "none (no rival stands in the target region)") + ", the topic stepped once; the regional polls still planted";
            }, () => controller.StageHqComposerForFilm(null, false, 1, 0));
            Unplant();
            Debug.Log("SHOT: S4 - the planted regional polls removed: " + run.RegionalPolls[me].Count.ToString(CultureInfo.InvariantCulture) + " bought poll(s) left on the party's record");
        }

        /// <summary>PL-C (K168, K169): after the Sejm's nights the Polish game walks on by the day path to the presidential campaign - its HQ on its second day
        /// (the wojewodztwa) - and to the run-off's, the two finalists alone, on its second day.</summary>
        private IEnumerator CapturePolishPresidentialHq(GameController controller, SimulationManager sim, Dictionary<CountryId, PolicyDecision> noDecisions)
        {
            // PL-C: the nights' walk (CapturePolishNights) crosses 2025 and films both HQs on its way; this says what it filmed, or why not - no second walk
            if (!_plcFirstFilmed) { Debug.LogError("SHOT: PL-C plc03_presidential_campaign_hq - MISSING: no PresidentialFirstVote campaign ran on the walk to the 2025 first vote (the film's party, " + (sim.World.GetCountry(CountryId.Poland)?.PlayerPartyAbbrev ?? "none") + ", in no cast?)"); _failed++; }
            if (!_plcRunOffFilmed)
            {
                Country poland = sim.World.GetCountry(CountryId.Poland);
                PresidentialElection.Contest held = poland?.PresidentialElections.Find(c => c.RunOffA != null);
                string a = held?.Field.Find(c => c.Name == held.RunOffA)?.Party, b = held?.Field.Find(c => c.Name == held.RunOffB)?.Party;
                if (held == null || (poland.PlayerPartyAbbrev != a && poland.PlayerPartyAbbrev != b))
                {
                    // the review's L6: a run-off is campaigned only by a player whose party backs a finalist - out of reach otherwise, said and not failed
                    Debug.Log("SHOT: PL-C plc04_runoff_campaign_hq - out of reach: " + (held == null ? "no run-off was called (the first vote decided it)" : "the player's party, " + poland.PlayerPartyAbbrev + ", backs neither finalist (" + a + ", " + b + ")"));
                    _expectedFrames.Remove("plc04_runoff_campaign_hq");
                }
                else { Debug.LogError("SHOT: PL-C plc04_runoff_campaign_hq - MISSING: the player's party backs a finalist and no run-off campaign reached its second day on the walk"); _failed++; }
            }
            yield break;
        }

        /// <summary>The okręgi the Polish night counts - the generated cartogram's, so the staged state's place in the order is the night's own.</summary>
        private static int PolishOkregiCartogramCount => PoliSim.Elections.Generated.PolishOkregiCartogram.Okregi.Length;

        /// <summary>
        /// §707 (Elias's round-4 follow-up 5, "a German game filmed from its start to its formation"): after the night the game is played on as
        /// play plays it (the day path, the country's day tick) until the round concludes - the chancellor's election under Art. 63 GG in
        /// Germany (§705). Four frames of the Parliament tab: the round opened, the sheet the game opens where the player's party is asked
        /// (premise 1), the eve of the ballot, the government installed. Where the round waits on the player the film answers as the least choice
        /// there is: the sheet's own opening draft tabled as it stands, a pass where a partner refuses it; an offer the formation made accepted.
        /// </summary>
        private IEnumerator CaptureFormationTail(GameController controller, SimulationManager sim, Dictionary<CountryId, PolicyDecision> noDecisions)
        {
            Expect("e8a_formation_round_opened", "e8d_formation_government");
            Country country = sim.World.GetCountry(_countryId);
            FieldInfo sheetField = typeof(GameController).GetField("_formationSheetOpen", BindingFlags.Instance | BindingFlags.NonPublic);
            void Step()
            {
                bool boundary = sim.AdvanceDay();
                MemTick(sim);
                sim.AdvanceCountryDayTick(_countryId);
                if (boundary) { sim.AdvanceTurn(noDecisions); }
            }
            IEnumerator Film(string stem, bool sheet)
            {
                SetPrivateField(controller, "_onDesk", false);
                SetEnumField(controller, "_consolidatedTab", "Politics");
                SetEnumField(controller, "_politicsCategory", "Parliament");
                sheetField?.SetValue(controller, sheet);
                ResetScrolls(controller);
                if (!sheet) { ScrollBy(controller, 900f); }
                yield return Settle();
                sheetField?.SetValue(controller, sheet);   // the controller's Update may have opened it again on the player's turn
                yield return Settle();
                Claim("imgui");
                yield return Capture(stem);
            }
            for (int d = 0; d < 3 && sim.RoundOf(_countryId) == null; d++) { Step(); }
            SpeakerRound round = sim.RoundOf(_countryId);
            if (round == null)
            {
                Debug.LogError($"SHOT: -shotformation - no round opened for {_countryId} in the three days after the night; the formation is NOT filmed.");
                _failed++;
                yield break;
            }
            yield return Film("e8a_formation_round_opened", false);
            Debug.Log($"SHOT: -shotformation - the round opened {sim.CurrentDate:yyyy-MM-dd}: {round.Log[round.Log.Count - 1]}");
            bool sheetFilmed = false, eveFilmed = false;
            for (int d = 0; d < 240 && round.Open; d++)
            {
                if (round.Stage == RoundStage.PlayerAsked)
                {
                    if (!sheetFilmed) { yield return Film("e8b_formation_sheet", true); sheetFilmed = true; }
                    FormationProposal opening = sim.DraftProposal(country, round, country.PlayerPartyAbbrev);
                    bool tabled = sim.SubmitFormation(_countryId, opening, out ProposalVerdict _, out string refused);
                    if (!tabled) { sim.PassFormation(_countryId, out string _); }
                    sheetField?.SetValue(controller, false);
                    Debug.Log($"SHOT: -shotformation - {sim.CurrentDate:yyyy-MM-dd} the player's party asked: {(tabled ? "tabled the sheet's opening draft, " + string.Join("+", opening.CabinetParties) : "passed (" + refused + ")")}.");
                    continue;
                }
                if (round.Stage == RoundStage.OfferToPlayer)
                {
                    sim.AnswerOffer(_countryId, true, out string _);
                    Debug.Log($"SHOT: -shotformation - {sim.CurrentDate:yyyy-MM-dd} the player's party accepted the formation's offer.");
                    continue;
                }
                if (!eveFilmed && round.Stage == RoundStage.VotePending && sim.CurrentDate.AddDays(1) >= round.VoteOn)
                {
                    yield return Film("e8c_formation_ballot_eve", false);
                    eveFilmed = true;
                }
                Step();
            }
            if (round.Open)
            {
                Debug.LogError($"SHOT: -shotformation - the round is still open on {sim.CurrentDate:yyyy-MM-dd}, 240 days on; the government is NOT filmed.");
                _failed++;
                yield break;
            }
            yield return Film("e8d_formation_government", false);
            GovernmentRecord formed = country.Government;
            Debug.Log($"SHOT: -shotformation - {sim.CurrentDate:yyyy-MM-dd} the round concluded: {string.Join("+", formed.Cabinet)} led by {formed.PmParty} ({formed.Basis}).");
        }

        /// <summary>
        /// PS-6 US-36 (`docs/specs/USA_STAGE_PLAN.md` US-36: "A US game is filmed from 12 Mar 2024 through the 2024 count, the oath, a bill through both
        /// chambers, a veto, the first budget, the debt limit's episode and the 2026 midterm; the driver stages each outcome (a DEM win, a REP win) as
        /// `-shotformation` stages a formation"): THE US JOURNEY. One US game played from its start as the interrupts film plays it - the day path, the
        /// country's day tick, the turn on its boundary, play's own clock held while the harness walks - and every milestone VERIFIED on the game before its
        /// frame is named (a `SHOT: USJ frame - STAGED:` line with the evidence). A milestone that cannot be reached prints `MISSING` and fails the film; one
        /// the rules put out of reach for the role the count left the player in prints `NOT IN THIS OUTCOME` - never a silence. The series is `usj`; the
        /// start card is the opening's 01f2_start_points (every film's).
        /// <para>The outcome is STAGED (`StageUsWinner`): on the polling day the campaign's final shares are re-split between REP and DEM by the least margin
        /// that elects `UsJourneyWinner`, and the day's count is held again through the manager's own count - no random stream is read (the count's state
        /// noise is keyed by the seed, the country and the day). Everything after it is the game's: the oath, the Congress it seats, the first budget, the
        /// debt limit's episode and the midterm are played, and the player answers only where play cannot go on without an answer, with the least answer
        /// there is (`-shotformation`'s rule): a player-president tables the standing figures as the first budget, and proposes the record's last raise when
        /// the limit waits on the President; every bill on the desk is left to its period. The bill frames are staged through the game's own gate as the
        /// sweep's 89h-89l are, each put back after. Silent: the listener is muted at the film's start (the harness rule).</para>
        /// </summary>
        private IEnumerator CaptureUsJourney(GameController controller)
        {
            SimulationManager sim = controller.GetType().GetField("_simulationManager", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(controller) as SimulationManager;
            Country usa = sim?.World?.GetCountry(CountryId.USA);
            string winner = (UsJourneyWinner ?? string.Empty).Trim().ToUpperInvariant();
            if (_countryId != CountryId.USA || usa == null || (winner != "DEM" && winner != "REP") || !string.IsNullOrEmpty(LoadSave))
            {
                Debug.LogError($"SHOT: -shotusjourney plays a US game from its start and stages its 2024 count for -shotwinner=DEM or -shotwinner=REP - this run plays {_countryId}, winner '{UsJourneyWinner}', load '{LoadSave}', manager {(sim != null ? "found" : "NOT found")}; NOTHING filmed.");
                _failed++;
                EndSweep();
                yield break;
            }
            var noDecisions = new Dictionary<CountryId, PolicyDecision>();
            void Step()
            {
                bool boundary = sim.AdvanceDay();
                MemTick(sim);
                sim.AdvanceCountryDayTick(_countryId);
                if (boundary) { sim.AdvanceTurn(noDecisions); }
            }
            int WalkUntil(Func<bool> reached, int maxDays)
            {
                int d = 0;
                while (!reached() && d < maxDays) { Step(); d++; }
                return d;
            }
            FieldInfo hqField = controller.GetType().GetField("_liveCampaignOpen", BindingFlags.Instance | BindingFlags.NonPublic);
            FieldInfo nightField = controller.GetType().GetField("_electionNight", BindingFlags.Instance | BindingFlags.NonPublic);
            bool HqOpen() => hqField?.GetValue(controller) is bool open && open;

            Expect("usj01_runup_hq", "usj02_campaign_opening_hq", "usj03_campaign_hq_late", "usj04_night_2024", "usj05_after_night_2024", "usj06_congress_seated",
                "usj07_oath_desk", "usj08_oath_congress", "usj09_signing_bill_signed", "usj10_signing_veto_sustained", "usj11_signing_veto_overridden",
                "usj15_budget_before_congress", "usj16_budget_decided", "usj17_debt_limit_reached",
                "usj19_debt_limit_raise_before_congress", "usj20_debt_limit_resolved", "usj21_midterm_night_2026", "usj22_after_midterm_congress");
            Debug.Log($"SHOT: USJ - the US journey from {sim.CurrentDate:yyyy-MM-dd}: the player's party {usa.PlayerPartyAbbrev}, the 2024 count staged for {winner}; the start card is 01f2_start_points, filmed in the opening.");

            // play's clock: Normal, and held an hour short of a day for the whole journey - the controller's own Update meets the campaign's opening (it opens
            // the HQ before any gate), and no real-time day passes between the harness's days
            FieldInfo speedField = controller.GetType().GetField("_gameSpeed", BindingFlags.Instance | BindingFlags.NonPublic);
            object speedWas = speedField?.GetValue(controller);
            SetEnumField(controller, "_gameSpeed", "Normal");
            SetPrivateField(controller, "_daySpeedTimer", -ClockHoldSeconds);
            SetPrivateField(controller, "_onDesk", true);

            // (1) the run-up's HQ, on the run-up's first day
            int days = WalkUntil(() => sim.PlayerPreCampaign != null || sim.PlayerCampaign != null, MaxStateSearchDays);
            if (sim.PlayerPreCampaign != null && sim.PlayerCampaign == null)
            {
                InvokeNoArg(controller, "OpenLiveCampaign");
                yield return Settle();
                if (!HqOpen()) { UsjMissing("usj01_runup_hq", $"the run-up began {sim.CurrentDate:yyyy-MM-dd} and the HQ did not open from its own branch"); }
                else
                {
                    UsjStaged("usj01_runup_hq", string.Format(CultureInfo.InvariantCulture, "the run-up's HQ on {0:yyyy-MM-dd}, {1} day(s) in: run-up day {2}, chest {3:F0}, nothing queued", sim.CurrentDate, days, sim.PlayerPreCampaign.Day, sim.PlayerPreCampaign.Money));
                    Claim("imgui");
                    yield return Capture("usj01_runup_hq");
                }
                InvokeNoArg(controller, "CloseLiveCampaign");
                SetPrivateField(controller, "_onDesk", true);
                yield return Settle();
            }
            else { UsjMissing("usj01_runup_hq", $"no run-up began within {days} day(s) (the campaign {(sim.PlayerCampaign != null ? "began first" : "never began")})"); }

            // (2) the campaign's opening - an interrupt the controller meets by itself, its HQ open and the clock held until it is acknowledged
            days = WalkUntil(() => sim.PlayerCampaign != null, MaxStateSearchDays);
            if (sim.PlayerCampaign == null) { UsjMissing("usj02_campaign_opening_hq", $"no campaign began within {days} day(s)"); }
            else
            {
                SetPrivateField(controller, "_daySpeedTimer", -ClockHoldSeconds);
                yield return Settle();
                yield return Settle();
                string banner = controller.GetType().GetMethod("BuildFoldedInterruptText", BindingFlags.Instance | BindingFlags.NonPublic)?.Invoke(controller, new object[] { true }) as string;
                if (!HqOpen() || banner == null || banner.IndexOf("campaign", StringComparison.OrdinalIgnoreCase) < 0) { UsjMissing("usj02_campaign_opening_hq", $"the campaign began {sim.CurrentDate:yyyy-MM-dd} and the HQ {(HqOpen() ? "opened" : "did NOT open")} with the banner \"{banner ?? "(none)"}\""); }
                else
                {
                    UsjStaged("usj02_campaign_opening_hq", string.Format(CultureInfo.InvariantCulture, "the campaign opened {0:yyyy-MM-dd} ({1} day(s) after the run-up), the HQ open by itself, the banner \"{2}\"; {3} campaign day(s) to the polling day", sim.CurrentDate, days, banner, sim.PlayerCampaign.TotalDays));
                    Claim("imgui");
                    yield return Capture("usj02_campaign_opening_hq");
                    SetPrivateField(controller, "_daySpeedTimer", 0f);   // released for the watch, so a held date is the interrupt's and not the harness's
                    yield return AssertClockHeld(controller, sim, "the US campaign's opening");
                    SetPrivateField(controller, "_daySpeedTimer", -ClockHoldSeconds);
                }
                InvokeNoArg(controller, "AcknowledgeCampaignOpening");
                InvokeNoArg(controller, "CloseLiveCampaign");
                SetPrivateField(controller, "_onDesk", true);
                yield return Settle();
            }

            // (3) the campaign's HQ a week before the polling day
            if (sim.PlayerCampaign != null)
            {
                days = WalkUntil(() => sim.PlayerCampaign == null || sim.PlayerCampaign.Finished || sim.PollingDayToday || sim.PlayerCampaign.Day >= sim.PlayerCampaign.TotalDays - 7, MaxStateSearchDays);
                if (sim.PlayerCampaign == null || sim.PlayerCampaign.Finished || sim.PollingDayToday) { UsjMissing("usj03_campaign_hq_late", $"the campaign {(sim.PlayerCampaign == null ? "ended" : "finished")} before a late day was reached ({sim.CurrentDate:yyyy-MM-dd})"); }
                else
                {
                    InvokeNoArg(controller, "OpenLiveCampaign");
                    yield return Settle();
                    if (!HqOpen()) { UsjMissing("usj03_campaign_hq_late", $"the HQ did not open on campaign day {sim.PlayerCampaign.Day}"); }
                    else
                    {
                        UsjStaged("usj03_campaign_hq_late", string.Format(CultureInfo.InvariantCulture, "the campaign's HQ on {0:yyyy-MM-dd}, campaign day {1} of {2}", sim.CurrentDate, sim.PlayerCampaign.Day, sim.PlayerCampaign.TotalDays));
                        Claim("imgui");
                        yield return Capture("usj03_campaign_hq_late");
                    }
                    InvokeNoArg(controller, "CloseLiveCampaign");
                    SetPrivateField(controller, "_onDesk", true);
                    yield return Settle();
                }
            }
            else { UsjMissing("usj03_campaign_hq_late", "no campaign ran"); }

            // (4) the polling day: the count staged for the winner asked, then the night the controller's own election call opens
            days = WalkUntil(() => sim.PollingDayToday, MaxStateSearchDays);
            if (!sim.PollingDayToday)
            {
                UsjMissing("usj04_night_2024", $"no polling day within {days} day(s); the journey ends here");
                SetPrivateField(controller, "_onDesk", true);
                if (speedWas != null) { speedField.SetValue(controller, speedWas); }
                EndSweep();
                yield break;
            }
            DateTime polled = sim.CurrentDate.Date;
            bool staged = StageUsWinner(sim, usa, winner, out string stagedLine);
            Debug.Log("SHOT: USJ - the 2024 count " + (staged ? "STAGED: " : "NOT STAGED: ") + stagedLine);
            UsPresidentialElection.Contest counted = usa.UsPresidentialElections.Find(c => c.PollingDay.Date == polled);
            yield return SealLiveSigning(controller, "usj04a_signing_before_the_night");
            HideLiveCampaign(controller);
            InvokeNoArg(controller, "CheckElection");
            object night = nightField?.GetValue(controller);
            if (night != null)
            {
                yield return WaitForCanvasSettle(controller, wantActive: true);
                yield return Settle();
                if (!staged || counted == null || counted.WinnerParty != winner) { UsjMissing("usj04_night_2024", $"the night is up but the count elects {counted?.WinnerParty ?? "no one"}, not {winner} ({stagedLine})"); }
                else
                {
                    UsjStaged("usj04_night_2024", string.Format(CultureInfo.InvariantCulture, "the night of {0:yyyy-MM-dd} opened by the controller's own election call: {1} ({2}) elected, the College REP {3} DEM {4}, takes office {5:yyyy-MM-dd}",
                        polled, counted.Winner, counted.WinnerParty, counted.ElectorsOf("REP"), counted.ElectorsOf("DEM"), counted.TakesOffice));
                    Claim("electionnight");
                    yield return Capture("usj04_night_2024");
                    RecordCanvasTextAssert("usj04_night_2024", controller);
                    SetPrivateField(controller, "_daySpeedTimer", 0f);
                    yield return AssertClockHeld(controller, sim, "the 2024 night");
                    SetPrivateField(controller, "_daySpeedTimer", -ClockHoldSeconds);
                }
                InvokeNoArg(controller, "DismissElectionNight");
                yield return WaitForCanvasSettle(controller, wantActive: false);
                yield return Settle();
            }
            else { UsjMissing("usj04_night_2024", $"the polling day {polled:yyyy-MM-dd} showed NO night (the ELECTION: line above says why)"); }
            bool over = controller.GetType().GetField("_isGameOver", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(controller) is bool o && o;
            if (over || nightField?.GetValue(controller) != null)
            {
                UsjMissing("usj05_after_night_2024", over ? "the game ended on the night's verdict; the journey ends here" : "the night would not dismiss; the journey ends here");
                if (speedWas != null) { speedField.SetValue(controller, speedWas); }
                EndSweep();
                yield break;
            }
            SetPrivateField(controller, "_onDesk", true);
            yield return Settle();
            UsjStaged("usj05_after_night_2024", $"the night dismissed on {sim.CurrentDate:yyyy-MM-dd}, the game running on (not over), the Desk");
            Claim("imgui");
            yield return Capture("usj05_after_night_2024");

            // (5) to the midterm: each milestone watched for day by day, filmed the first day the game reaches it
            DateTime oathDay = counted != null ? counted.TakesOffice.Date : UsPresidentialElection.OathDayAfter(polled);
            bool congressFilmed = false, oathSeen = false, billsFilmed = false, playerPresident = false, windowSeen = false, decidedFilmed = false, reachedFilmed = false, holdSeen = false, raiseFilmed = false, resolvedFilmed = false;
            BudgetBill budgetAtOath = null, budgetFilmed = null;
            int divisionsSeen = 0, raisesBefore = usa.DebtLimitRaises, proposals = 0;
            int walked = 0;
            while (walked < MaxStateSearchDays)
            {
                Step();
                walked++;
                DateTime today = sim.CurrentDate.Date;
                if (sim.PollingDayToday) { break; }
                if (walked % 30 == 0) { SetPrivateField(controller, "_daySpeedTimer", -ClockHoldSeconds); yield return null; }

                if (!congressFilmed && UsCongressElection.TryInOffice(usa, today, out UsCongressElection.Contest sitting) && sitting.PollingDay.Date == polled)
                {
                    congressFilmed = true;
                    UsjStaged("usj06_congress_seated", string.Format(CultureInfo.InvariantCulture, "{0:yyyy-MM-dd}: the Congress the game elected on {1:yyyy-MM-dd} sits (convened {2:yyyy-MM-dd}): the House REP {3} DEM {4}, the Senate REP {5} DEM {6}",
                        today, sitting.PollingDay, sitting.Convenes, sitting.HouseRep, sitting.HouseDem, sitting.SenateOf("REP"), sitting.SenateOf("DEM")));
                    yield return UsjFilmPage(controller, "usj06_congress_seated", "Politics", "Parliament", false, null, null);
                }

                if (!oathSeen && today >= oathDay)
                {
                    oathSeen = true;
                    bool sworn = UsPresidentialElection.TryInOffice(usa, today, out UsPresidentialElection.Contest inOffice) && inOffice.PollingDay.Date == polled && inOffice.WinnerParty == winner && UsBudgetProcedure.HoldsPresidency(usa, winner);
                    playerPresident = Congress.PresidentOn(usa, today, out string shownAs, out string presidentsParty, out bool isPlayer) && isPlayer;
                    budgetAtOath = sim.GetPendingBudgetBill(usa.Id);
                    if (!sworn) { UsjMissing("usj07_oath_desk", $"on {today:yyyy-MM-dd} the president in office is not the staged count's {winner} ({shownAs ?? "none"}, {presidentsParty ?? "-"})"); UsjMissing("usj08_oath_congress", "no oath of the staged count"); }
                    else
                    {
                        UsjStaged("usj07_oath_desk", $"{today:yyyy-MM-dd}: the oath - {inOffice.Winner} ({inOffice.WinnerParty}) in office, the government {usa.Government?.Executive}; the player's {usa.PlayerPartyAbbrev} {(playerPresident ? "holds the presidency (a player-president)" : "is out of the White House")}");
                        yield return UsjFilmPage(controller, "usj07_oath_desk", null, null, false, null, null);
                        UsjStaged("usj08_oath_congress", $"{today:yyyy-MM-dd}: the Congress view on the oath's day, {inOffice.WinnerParty} in the White House");
                        yield return UsjFilmPage(controller, "usj08_oath_congress", "Politics", "Parliament", false, null, null);
                    }
                    if (playerPresident) { Expect("usj12_signing_desk_answer", "usj13_signing_desk_veto", "usj14_budget_window", "usj18_debt_limit_hold"); }
                    else
                    {
                        UsjNotInOutcome("usj14_budget_window", "the president is not the player's party, so the AI administration tables the budget itself and no window opens for the player (PS-3e)");
                        UsjNotInOutcome("usj18_debt_limit_hold", "the limit waits on a player-president only (K85); the AI administration tables the raise itself (K23)");
                    }
                }

                // the bills, two days after the oath: the staged contest of the desk's frames is sworn the day before the film's day, after the game's own
                if (oathSeen && !billsFilmed && today >= oathDay.AddDays(2))
                {
                    billsFilmed = true;
                    yield return UsjFilmBills(controller, sim, usa);
                }

                // the debt limit reached - from the suspension's end, before or after the oath
                if (!reachedFilmed && UsDebtLimit.IsRead(usa) && usa.DebtLimitReachedOn != DateTime.MinValue && !UsDebtLimit.Suspended(usa, today))
                {
                    reachedFilmed = true;
                    UsjStaged("usj17_debt_limit_reached", $"{today:yyyy-MM-dd}: the limit reached on {usa.DebtLimitReachedOn:yyyy-MM-dd} - {UsDebtLimit.LawWords(usa)}; the measures run to {UsDebtLimit.BindsOn(usa):yyyy-MM-dd} ({UsDebtLimit.MeasuresDaysLeft(usa, today)} day(s) left)");
                    yield return UsjFilmPage(controller, "usj17_debt_limit_reached", "Budget", null, false, null, "budget:debt");
                }

                if (!oathSeen) { continue; }

                // the first budget after the oath: a player-president's window, the bill before Congress, its acts decided
                BudgetBill pending = sim.GetPendingBudgetBill(usa.Id);
                if (budgetAtOath != null && !ReferenceEquals(pending, budgetAtOath)) { budgetAtOath = null; }   // what stood before Congress at the oath is decided: the first budget is the next
                if (playerPresident && !windowSeen && budgetFilmed == null && pending == null && sim.GetPendingBudgetProcess(usa.Id))
                {
                    windowSeen = true;
                    UsjStaged("usj14_budget_window", $"{today:yyyy-MM-dd}: the budget window open for the player-president ({usa.PlayerPartyAbbrev}), nothing before Congress");
                    yield return UsjFilmPage(controller, "usj14_budget_window", "Budget", null, false, null, null);
                    BudgetBill draft = controller.GetType().GetMethod("BuildBudgetBillFromDrafts", BindingFlags.Instance | BindingFlags.NonPublic)?.Invoke(controller, null) as BudgetBill;
                    bool tabled = draft != null && sim.IntroduceBudgetBill(usa.Id, draft);
                    Debug.Log($"SHOT: USJ - the player-president tables the drafts as they stand (the least answer, -shotformation's rule): {(tabled ? "tabled - " + SimulationManager.DescribeBudgetBill(draft) : "REFUSED (the LEVERS: line above says why)")}");
                    pending = sim.GetPendingBudgetBill(usa.Id);
                }
                if (budgetFilmed == null && budgetAtOath == null && pending != null && !pending.ContinuingResolution)
                {
                    budgetFilmed = pending;
                    divisionsSeen = usa.Divisions.Entries.Count > 0 ? usa.Divisions.Entries[usa.Divisions.Entries.Count - 1].Number : 0;
                    UsjStaged("usj15_budget_before_congress", $"{today:yyyy-MM-dd}: the first budget after the oath before Congress ({SimulationManager.DescribeBudgetBill(pending)}, {(pending.GovernmentBill ? "the administration's" : "the player-president's")}), decided in {pending.DaysRemaining} day(s)");
                    yield return UsjFilmPage(controller, "usj15_budget_before_congress", null, null, false, "effects", null);
                }
                if (budgetFilmed != null && !decidedFilmed && !ReferenceEquals(sim.GetPendingBudgetBill(usa.Id), budgetFilmed))
                {
                    decidedFilmed = true;
                    var acts = new List<string>();
                    bool appropriation = false;
                    foreach (DivisionRecord record in usa.Divisions.Entries)
                    {
                        if (record.Number <= divisionsSeen) { continue; }
                        acts.Add($"No. {record.Number} {(record.Passed ? "passed" : "failed")}: {record.Title}");
                        if (record.Title != null && record.Title.StartsWith(UsBudgetProcedure.ActWords(UsBudgetProcedure.Act.Appropriation), StringComparison.Ordinal)) { appropriation = true; }
                    }
                    BudgetBill after = sim.GetPendingBudgetBill(usa.Id);
                    if (!appropriation) { UsjMissing("usj16_budget_decided", $"the first budget left the slot on {today:yyyy-MM-dd} with no appropriation voted ({acts.Count} division(s): {string.Join("; ", acts)})"); }
                    else
                    {
                        UsjStaged("usj16_budget_decided", $"{today:yyyy-MM-dd}: the first budget decided as its acts - {string.Join("; ", acts)}; funded through {usa.AppropriatedThrough:yyyy-MM-dd}{(after != null ? "; now before Congress: " + SimulationManager.DescribeBudgetBill(after) : string.Empty)}");
                        yield return UsjFilmPage(controller, "usj16_budget_decided", "Politics", "Parliament", true, null, null);
                    }
                }

                // the debt limit's hold (a player-president's: the binding limit with nothing proposed holds play's clock) and its resolution
                if (playerPresident && sim.DebtLimitAwaitsPresident(usa.Id) && proposals < 8)
                {
                    bool held = controller.GetType().GetMethod("DebtLimitHolds", BindingFlags.Instance | BindingFlags.NonPublic)?.Invoke(controller, null) is bool h && h;
                    if (!holdSeen)
                    {
                        holdSeen = true;
                        if (!held) { UsjMissing("usj18_debt_limit_hold", $"{today:yyyy-MM-dd}: the limit waits on the President and play's clock does NOT hold on it"); }
                        else
                        {
                            string holdBanner = controller.GetType().GetMethod("BuildFoldedInterruptText", BindingFlags.Instance | BindingFlags.NonPublic)?.Invoke(controller, new object[] { true }) as string;
                            UsjStaged("usj18_debt_limit_hold", $"{today:yyyy-MM-dd}: the measures ran out on {UsDebtLimit.BindsOn(usa):yyyy-MM-dd} with nothing proposed - the limit waits on the President and holds play's clock (K85); {UsDebtLimit.LawWords(usa)}; the banner \"{holdBanner ?? "(none)"}\"");
                            yield return UsjFilmPage(controller, "usj18_debt_limit_hold", null, null, false, null, null);
                        }
                    }
                    if (usa.DebtLimitWithheldBy != null)   // the review's M2: a standing withhold - let it bind first (K89/K90), as the warm-up does; never a raise voted down at once
                    {
                        // a raise fell to a standing withhold: the President lets the limit bind until the episode changes (K90), then is asked again
                        bool letBind = sim.LetDebtLimitBind(usa.Id, out string bindRefused);
                        Debug.Log($"SHOT: USJ - {today:yyyy-MM-dd}: {usa.DebtLimitWithheldBy} withholds the raise; the President lets the limit bind until that changes: {(letBind ? "done" : "refused (" + bindRefused + ")")}");
                    }
                    else
                    {
                        proposals++;
                        bool proposed = sim.ProposeDebtLimitRaise(usa.Id, UsDebtLimit.Pl119IncreaseUsd, DateTime.MinValue, out string proposeRefused);
                        Debug.Log($"SHOT: USJ - {today:yyyy-MM-dd}: the President proposes the record's last raise, {UsDebtLimit.TrillionsWords(UsDebtLimit.Pl119IncreaseUsd)} over the excess (proposal {proposals}): {(proposed ? "tabled" : "REFUSED (" + proposeRefused + ")")}");
                    }
                }
                BudgetBill raise = sim.GetPendingDebtLimitBill(usa.Id);
                if (!raiseFilmed && raise != null)
                {
                    raiseFilmed = true;
                    UsjStaged("usj19_debt_limit_raise_before_congress", $"{today:yyyy-MM-dd}: {UsDebtLimit.BillTitle(raise)} - {(raise.Docket != null ? "on the President's desk since " + raise.Docket.PresentedOn.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) : "before Congress, voted in " + raise.DaysRemaining.ToString(CultureInfo.InvariantCulture) + " day(s)")}; {(raise.GovernmentBill ? "the administration's" : "the President's own")}{(usa.DebtLimitWithheldBy != null ? "; withheld by " + usa.DebtLimitWithheldBy : string.Empty)}");
                    yield return UsjFilmPage(controller, "usj19_debt_limit_raise_before_congress", "Budget", null, false, null, "budget:debt");
                }
                if (raiseFilmed && !resolvedFilmed && usa.DebtLimitRaises > raisesBefore)
                {
                    resolvedFilmed = true;
                    UsjStaged("usj20_debt_limit_resolved", $"{today:yyyy-MM-dd}: the episode closed by law - the game's act no. {usa.DebtLimitRaises}: {UsDebtLimit.LawWords(usa)}");
                    yield return UsjFilmPage(controller, "usj20_debt_limit_resolved", "Budget", null, false, null, "budget:debt");
                }
            }

            // what the walk never reached, each named with the state it stood in
            DateTime stopped = sim.CurrentDate.Date;
            if (!congressFilmed) { UsjMissing("usj06_congress_seated", $"no Congress of the game's count sat by {stopped:yyyy-MM-dd}"); }
            if (!oathSeen) { UsjMissing("usj07_oath_desk", $"the oath day {oathDay:yyyy-MM-dd} was not reached by {stopped:yyyy-MM-dd}"); UsjMissing("usj08_oath_congress", "no oath"); }
            if (!billsFilmed) { foreach (string bill in new[] { "usj09_signing_bill_signed", "usj10_signing_veto_sustained", "usj11_signing_veto_overridden", "usj12_signing_desk_answer", "usj13_signing_desk_veto" }) { UsjMissing(bill, $"the bills are staged two days after the oath ({oathDay:yyyy-MM-dd}), never reached by {stopped:yyyy-MM-dd}"); } }
            if (playerPresident && !windowSeen && budgetFilmed == null) { UsjMissing("usj14_budget_window", $"no budget window opened for the player-president by {stopped:yyyy-MM-dd}"); }
            if (budgetFilmed == null) { UsjMissing("usj15_budget_before_congress", $"no budget (other than a continuing resolution) came before Congress after the oath by {stopped:yyyy-MM-dd}"); }
            else if (!decidedFilmed) { UsjMissing("usj16_budget_decided", $"the first budget was still before Congress on {stopped:yyyy-MM-dd}"); }
            if (!reachedFilmed) { UsjMissing("usj17_debt_limit_reached", $"the debt never reached the limit by {stopped:yyyy-MM-dd} ({(UsDebtLimit.IsRead(usa) ? UsDebtLimit.LawWords(usa) : "the law not read")})"); }
            if (playerPresident && !holdSeen) { UsjMissing("usj18_debt_limit_hold", $"the limit never waited on the President by {stopped:yyyy-MM-dd} (reached {(usa.DebtLimitReachedOn == DateTime.MinValue ? "no" : usa.DebtLimitReachedOn.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture))}, raises {usa.DebtLimitRaises - raisesBefore})"); }
            if (!raiseFilmed) { UsjMissing("usj19_debt_limit_raise_before_congress", $"no raise came before Congress by {stopped:yyyy-MM-dd}"); }
            else if (!resolvedFilmed) { UsjMissing("usj20_debt_limit_resolved", $"no raise or suspension became law by {stopped:yyyy-MM-dd} ({UsDebtLimit.LawWords(usa)})"); }
            if (proposals >= 8) { Debug.Log($"SHOT: USJ - the President's raises reached the walk's cap of 8 proposals; from then the walk ran on days play's clock would hold (the review's L1){(sim.DebtLimitAwaitsPresident(usa.Id) ? " - the limit still waited on the President on " + stopped.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) : string.Empty)}"); }
            if (!oathSeen) { Debug.Log("SHOT: USJ usj12-usj14, usj18 - no oath reached, so the player's role is unknown and the player-president's frames were never declared (the review's L5)"); }

            // (6) the 2026 midterm: the House, the class and the two specials of record (K14), the night the controller's own election call opens
            if (!sim.PollingDayToday || !UsCongressElection.IsMidterm(stopped))
            {
                UsjMissing("usj21_midterm_night_2026", $"the walk stopped on {stopped:yyyy-MM-dd} with no midterm polling day");
                UsjMissing("usj22_after_midterm_congress", "no midterm");
            }
            else
            {
                UsCongressElection.Contest midterm = usa.UsCongressElections.Find(c => c.PollingDay.Date == stopped);
                var specials = new List<string>();
                if (midterm != null) { foreach (UsCongressElection.SenateSeat seat in midterm.Senate) { if (seat.Term == "unexpired") { specials.Add(seat.State + "/" + seat.Class.ToString(CultureInfo.InvariantCulture) + " " + seat.Side); } } }
                bool fl = specials.Exists(s => s.StartsWith("FL/3", StringComparison.Ordinal)), oh = specials.Exists(s => s.StartsWith("OH/3", StringComparison.Ordinal));
                yield return SealLiveSigning(controller, "usj21a_signing_before_the_midterm");
                HideLiveCampaign(controller);
                InvokeNoArg(controller, "CheckElection");
                if (nightField?.GetValue(controller) == null) { UsjMissing("usj21_midterm_night_2026", $"the midterm {stopped:yyyy-MM-dd} showed NO night (counted: {(midterm != null ? midterm.Line : "no Congress count")})"); }
                else
                {
                    yield return WaitForCanvasSettle(controller, wantActive: true);
                    yield return Settle();
                    if (midterm == null || !fl || !oh) { UsjMissing("usj21_midterm_night_2026", $"the night is up but the count {(midterm == null ? "is absent" : "carries the specials [" + string.Join(", ", specials) + "], not FL/3 and OH/3")}"); }
                    else
                    {
                        UsjStaged("usj21_midterm_night_2026", string.Format(CultureInfo.InvariantCulture, "the midterm night of {0:yyyy-MM-dd}: the House REP {1} DEM {2}, class {3} and the specials {4}; the president's party {5}, its term {6:0.00} point(s)",
                            stopped, midterm.HouseRep, midterm.HouseDem, midterm.SenateClass, string.Join(", ", specials), midterm.PresidentsParty, midterm.TermPoints));
                        Claim("electionnight");
                        yield return Capture("usj21_midterm_night_2026");
                        RecordCanvasTextAssert("usj21_midterm_night_2026", controller);
                        SetPrivateField(controller, "_daySpeedTimer", 0f);
                        yield return AssertClockHeld(controller, sim, "the 2026 midterm night");
                        SetPrivateField(controller, "_daySpeedTimer", -ClockHoldSeconds);
                    }
                    InvokeNoArg(controller, "DismissElectionNight");
                    yield return WaitForCanvasSettle(controller, wantActive: false);
                    yield return Settle();
                }
                if (midterm == null || nightField?.GetValue(controller) != null) { UsjMissing("usj22_after_midterm_congress", midterm == null ? "no midterm count" : "the night would not dismiss"); }
                else
                {
                    UsjStaged("usj22_after_midterm_congress", $"{sim.CurrentDate:yyyy-MM-dd}: the Congress view after the midterm - the 120th elected, seated on {midterm.Convenes:yyyy-MM-dd}");
                    yield return UsjFilmPage(controller, "usj22_after_midterm_congress", "Politics", "Parliament", false, null, null);
                }
            }

            if (speedWas != null) { speedField.SetValue(controller, speedWas); }
            SetPrivateField(controller, "_daySpeedTimer", -ClockHoldSeconds);
            Debug.Log($"SHOT: USJ done on {sim.CurrentDate:yyyy-MM-dd} - the count staged for {winner}, the player's {usa.PlayerPartyAbbrev} {(playerPresident ? "a player-president" : "out of the White House")}; {_captured} captured, {_failed} failed.");
            EndSweep();
        }

        /// <summary>PS-6 US-36: a journey frame's evidence, printed before the frame is named.</summary>
        private static void UsjStaged(string frame, string evidence) => Debug.Log("SHOT: USJ " + frame + " - STAGED: " + evidence);

        /// <summary>PS-6 US-36: a journey frame the game did not reach - loud, and a failure of the film.</summary>
        private void UsjMissing(string frame, string why)
        {
            Debug.LogError("SHOT: USJ " + frame + " - MISSING: " + why);
            _failed++;
        }

        /// <summary>PS-6 US-36: a journey frame the rules put out of reach for the role the count left the player in - said, never silent, and no failure.</summary>
        private static void UsjNotInOutcome(string frame, string why) => Debug.Log("SHOT: USJ " + frame + " - NOT IN THIS OUTCOME: " + why);

        /// <summary>PS-6 US-36: the live HQ closed before a night or a page, and the Desk put back - an HQ the game opened by itself would sit over the frame.</summary>
        private static void HideLiveCampaign(GameController controller)
        {
            if (controller.GetType().GetField("_liveCampaignOpen", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(controller) is bool open && open) { InvokeNoArg(controller, "CloseLiveCampaign"); }
            SetPrivateField(controller, "_onDesk", true);
        }

        /// <summary>
        /// PS-6 US-36: one journey frame of an IMGUI page - the Desk (<paramref name="tab"/> null; scrolled to <paramref name="deskCard"/> where named), or a tab
        /// and its page (the Parliament tab's rows by their top where <paramref name="rows"/>), with a slip pinned on <paramref name="slipAnchor"/> where named;
        /// the frame's evidence was printed by the caller. The Desk, its scroll and the pins are put back after.
        /// </summary>
        private IEnumerator UsjFilmPage(GameController controller, string frame, string tab, string category, bool rows, string deskCard, string slipAnchor)
        {
            HideLiveCampaign(controller);
            SetPrivateField(controller, "_daySpeedTimer", -ClockHoldSeconds);
            if (tab != null)
            {
                SetPrivateField(controller, "_onDesk", false);
                SetEnumField(controller, "_consolidatedTab", tab);
                if (category != null) { SetEnumField(controller, "_politicsCategory", category); }
            }
            ResetScrolls(controller);
            ScrollDeskTo(controller, null);
            yield return Settle();
            if (rows) { ScrollBy(controller, CongressRowsScroll(controller, 900f)); yield return Settle(); }
            if (deskCard != null) { ScrollDeskTo(controller, deskCard); yield return Settle(); }
            IList pins = controller.GetType().GetField("_slipPins", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(controller) as IList;
            if (slipAnchor != null)
            {
                MethodInfo pin = controller.GetType().GetMethod("PinSlipOnAnchorForFilm", BindingFlags.Instance | BindingFlags.NonPublic);
                if (pin == null || pins == null) { Debug.LogError($"SHOT: USJ {frame} - the controller has no PinSlipOnAnchorForFilm / _slipPins; the frame is filmed with no slip."); _failed++; }
                else { pin.Invoke(controller, new object[] { slipAnchor, new Vector2(UiScreen.Width * 0.58f, UiScreen.Height * 0.40f) }); yield return Settle(); yield return Settle(); }
            }
            Claim("imgui");
            yield return Capture(frame);
            pins?.Clear();
            ResetScrolls(controller);
            ScrollDeskTo(controller, null);
            SetPrivateField(controller, "_onDesk", true);
            yield return Settle();
        }

        /// <summary>PS-6 US-36: the signing takeover for the newest division, VERIFIED up before its frame is named, filmed with the Canvas guard and closed by
        /// <paramref name="closeVerb"/> (SIGN on the plate, or LATER on a desk's answer plate).</summary>
        private IEnumerator UsjFilmSigning(GameController controller, string frame, string closeVerb)
        {
            HideLiveCampaign(controller);
            InvokeNoArg(controller, "TriggerSigningForNewestDivision");
            yield return WaitForCanvasSettle(controller, wantActive: true);
            yield return Settle();
            string kind = controller.GetType().GetField("_canvasScreenKind", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(controller)?.ToString();
            if (kind != "Signing") { UsjMissing(frame, $"the signing takeover did not come up (the Canvas screen is {kind ?? "none"})"); yield break; }
            Claim("signing");
            yield return Capture(frame);
            RecordCanvasTextAssert(frame, controller);
            InvokeNoArg(controller, closeVerb);
            yield return WaitForCanvasSettle(controller, wantActive: false);
            yield return Settle();
            Claim("imgui");
        }

        /// <summary>
        /// PS-6 US-36: THE BILLS ON THE JOURNEY'S WORLD, after the oath - each staged through the game's own gate as the sweep's are, each put back: (9) a bill of
        /// the president's party sent by both chambers and signed by the president (rule (a), never the player's docket - the player's party seated as the other
        /// party's for the staging, `StageUsSignedBill`); (10) the other party's bill vetoed and the veto sustained in its first chamber, and (11) a veto
        /// overridden in both - the override vote (`StageUsVetoDivision`; where no bill of the other party reaches the desk on the game's chambers, the two
        /// parties' seats are swapped for that staging alone and put back, said in the log); (12) the majority's reconciliation on a player-president's desk -
        /// the answer plate - and (13) the House's vote on that veto (`StageUsDeskReconciliation`, its restore run in every outcome). A restore line closes it.
        /// </summary>
        private IEnumerator UsjFilmBills(GameController controller, SimulationManager sim, Country usa)
        {
            string mine = usa.PlayerPartyAbbrev;
            GovernmentRecord government0 = usa.Government;
            Dictionary<string, int> house0 = usa.ParliamentSeats, senate0 = usa.SecondChamberSeats;
            int contests0 = usa.UsPresidentialElections.Count;

            if (StageUsSignedBill(usa, sim, out string signedLine)) { UsjStaged("usj09_signing_bill_signed", signedLine); yield return UsjFilmSigning(controller, "usj09_signing_bill_signed", "SignPendingDivision"); }
            else { UsjMissing("usj09_signing_bill_signed", signedLine); }

            foreach ((string frame, bool overridden) in new[] { ("usj10_signing_veto_sustained", false), ("usj11_signing_veto_overridden", true) })
            {
                bool vetoStaged = StageUsVetoDivision(usa, sim, overridden);
                if (!vetoStaged && !overridden && Congress.PresidentOn(usa, sim.CurrentDate, out _, out string pp, out _) && (pp == "REP" || pp == "DEM"))
                {
                    string op = pp == "REP" ? "DEM" : "REP";
                    Debug.Log($"SHOT: USJ {frame} - no bill of {op}'s is sent and vetoed on the game's chambers; {pp}'s and {op}'s seats swapped in both chambers for this staging alone, restored after.");
                    usa.ParliamentSeats = SwappedSeats(house0, pp, op);
                    usa.SecondChamberSeats = SwappedSeats(senate0, pp, op);
                    try { vetoStaged = StageUsVetoDivision(usa, sim, false); }
                    finally { usa.ParliamentSeats = house0; usa.SecondChamberSeats = senate0; }
                }
                if (!vetoStaged) { UsjMissing(frame, "no bill could be staged through the gate in the asked shape (the SHOT: staged the US veto line above says how it fell)"); continue; }
                DivisionRecord ceremony = usa.Divisions.Entries[usa.Divisions.Entries.Count - 1];
                UsjStaged(frame, $"No. {ceremony.Number}: {ceremony.Title} - the line {ceremony.Required}, {(ceremony.Passed ? "overridden" : "the veto stands")}{(ceremony.OtherChamber != null ? ", both chambers voted on it" : ", its first chamber alone")}");
                yield return UsjFilmSigning(controller, frame, "SignPendingDivision");
            }

            DivisionRecord deskPassage = null;
            Action restoreDesk = null;
            try
            {
                if (!(Congress.PresidentOn(usa, sim.CurrentDate, out _, out _, out bool deskIsPlayers) && deskIsPlayers))
                {
                    const string why = "the answer plate is a player-president's desk, and the count left the player out of the White House";
                    UsjNotInOutcome("usj12_signing_desk_answer", why);
                    UsjNotInOutcome("usj13_signing_desk_veto", why);
                }
                else if (!StageUsDeskReconciliation(usa, sim, out deskPassage, out restoreDesk)) { UsjMissing("usj12_signing_desk_answer", "the majority's reconciliation could not be put on a player-president's desk (the SHOT: 89k line above says why)"); UsjMissing("usj13_signing_desk_veto", "no bill on the desk"); }
                else
                {
                    UsjStaged("usj12_signing_desk_answer", $"No. {deskPassage.Number}: {deskPassage.Title} - held on the player-president's desk, unanswered");
                    yield return UsjFilmSigning(controller, "usj12_signing_desk_answer", "AnswerLater");
                    if (!sim.VetoDocketBill(usa.Id, deskPassage, out string vetoWhy)) { UsjMissing("usj13_signing_desk_veto", $"the veto was refused ({vetoWhy})"); }
                    else
                    {
                        DivisionRecord vote = usa.Divisions.Entries[usa.Divisions.Entries.Count - 1];
                        if (vote.Passed) { UsjMissing("usj13_signing_desk_veto", $"the veto put to the override was OVERRIDDEN (No. {vote.Number}: {vote.Title}), not the asked frame (the veto standing)"); }
                        else
                        {
                            UsjStaged("usj13_signing_desk_veto", $"No. {vote.Number}: {vote.Title} - the player-president's veto put to the override, the veto stands");
                            yield return UsjFilmSigning(controller, "usj13_signing_desk_veto", "SignPendingDivision");
                        }
                    }
                }
            }
            finally { restoreDesk?.Invoke(); }

            bool putBack = usa.PlayerPartyAbbrev == mine && ReferenceEquals(usa.Government, government0) && ReferenceEquals(usa.ParliamentSeats, house0) && ReferenceEquals(usa.SecondChamberSeats, senate0) && usa.UsPresidentialElections.Count == contests0;
            string restored = $"SHOT: USJ usj09-usj13 restored - the player's {usa.PlayerPartyAbbrev} (before {mine}), the government {usa.Government?.Executive} (the game's {(ReferenceEquals(usa.Government, government0) ? "as found" : "NOT as found")}), the chambers' seats {(ReferenceEquals(usa.ParliamentSeats, house0) && ReferenceEquals(usa.SecondChamberSeats, senate0) ? "as found" : "NOT as found")}, {usa.UsPresidentialElections.Count} presidential contest(s) (before {contests0}); the staged divisions stay on the record, as every staging's do";
            if (putBack) { Debug.Log(restored + "."); }
            else { Debug.LogError(restored + " - NOT PUT BACK."); _failed++; }
        }

        /// <summary>
        /// PS-6 US-36: A BILL BOTH CHAMBERS SEND AND THE PRESIDENT SIGNS, through the game's own gate on the journey's world - the president's party's own bill (the
        /// first of the veto staging's dial moves, toward its end, that the projection passes with that party for in both chambers), the draw's indices planted
        /// at each list's first entry (`UsVeto.PlantIndex`); the player's party seated as the other party's for the staging alone, so the president answers by
        /// rule (a) and never on the player's docket, and put back after. The gate's records are the frame's; the bill's policy is not applied (the site that
        /// would apply it is not run), as the veto staging's.
        /// </summary>
        private static bool StageUsSignedBill(Country player, SimulationManager sim, out string line)
        {
            DateTime day = sim.CurrentDate;
            if (!Congress.Applies(player) || !Congress.PresidentOn(player, day, out string president, out string pp, out _) || (pp != "REP" && pp != "DEM"))
            {
                line = $"no Congress seated, or no president of REP or DEM on {day:yyyy-MM-dd}";
                return false;
            }
            MethodInfo gate = typeof(SimulationManager).GetMethod("UsVetoGate", BindingFlags.Instance | BindingFlags.NonPublic, null, new[] { typeof(Country), typeof(DivisionRecord), typeof(BillConcern), typeof(bool), typeof(DocketEntry).MakeByRefType(), typeof(bool) }, null);
            if (gate == null) { line = "SimulationManager.UsVetoGate not found"; return false; }
            string op = pp == "REP" ? "DEM" : "REP";
            string mine = player.PlayerPartyAbbrev;
            player.PlayerPartyAbbrev = op;
            try
            {
                var tried = new List<string>();
                foreach ((string title, BillConcern concern) in UsVetoCandidates(player, pp))
                {
                    int origin = Congress.OriginOf(player, concern, day);
                    Congress.Verdict v = Congress.Project(player, concern, day, origin);
                    if (!v.Passed || !SideIs(v.House, pp, 1) || !SideIs(v.Senate, pp, 1)) { tried.Add(title + " (projected " + (v.Passed ? "passed" : "stopped") + ")"); continue; }
                    bool sent, law;
                    DivisionRecord passage;
                    using (UsVeto.PlantIndex(0))
                    {
                        passage = ParliamentSystem.Divide(player, title, concern, day, out sent);
                        law = gate.Invoke(sim, new object[] { player, passage, concern, sent, null, false }) is bool decided && decided;   // every parameter given, as the veto staging's
                    }
                    DivisionRecord newest = player.Divisions.Entries[player.Divisions.Entries.Count - 1];
                    if (sent && law && passage != null && passage.OtherChamber != null && newest.Title != null && !newest.Title.StartsWith("Vetoed by the President (", StringComparison.Ordinal))   // signed: no veto record after the passage (the Senate's own passage carries cloture's 60, so Required is no test)
                    {
                        line = $"the president ({president}, {pp}) signs {pp}'s '{title}', sent by both chambers (the draw planted at each list's first entry): No. {passage.Number}, {passage.Title}";
                        return true;
                    }
                    tried.Add(title + " (sent " + sent + ", law " + law + ", the other chamber's " + (passage?.OtherChamber != null ? "record" : "NONE") + ", the newest No. " + newest.Number + " '" + newest.Title + "' required " + newest.Required + ")");
                }
                line = $"no bill of {pp}'s was sent by both chambers and signed on {day:yyyy-MM-dd}: {string.Join("; ", tried)}";
                return false;
            }
            finally { player.PlayerPartyAbbrev = mine; }
        }

        /// <summary>
        /// PS-6 US-36: THE 2024 COUNT STAGED FOR <paramref name="winner"/> - as `-shotformation` stages a formation, by the least intervention that gives the asked
        /// outcome. Where the count as played already elects it, nothing is staged. Otherwise the campaign's final shares are re-split between REP and DEM,
        /// their sum kept, by the played margin turned the other way and then wider in quarter-point steps until the College elects <paramref name="winner"/>
        /// outright; at each step the day's presidential and Congress contests are taken off the country and the manager's own count (`HoldUsPresidentialCount`,
        /// the campaign's shares, its ground game, the House and the Senate class on the same poll) holds the day again. Nothing else the count wrote exists to
        /// undo (`UsPresidentialElection.Hold` and `UsCongressElection.Hold` add their contest and nothing more). No random stream is read: the state noise is
        /// keyed by the seed, the country and the day, and the election's own sigma is zero in play. Where no step elects the winner, the played shares and
        /// count are put back and false is returned.
        /// </summary>
        private static bool StageUsWinner(SimulationManager sim, Country usa, string winner, out string line)
        {
            DateTime day = sim.CurrentDate.Date;
            UsPresidentialElection.Contest played = usa.UsPresidentialElections.Find(c => c.PollingDay.Date == day);
            if (played == null) { line = $"the day loop held no presidential count on {day:yyyy-MM-dd} (the PRESIDENTIAL: line above says why)"; return false; }
            string playedLine = string.Format(CultureInfo.InvariantCulture, "the count as played: {0} ({1}), the College REP {2} DEM {3}", played.Winner ?? "no one", played.WinnerParty ?? "-", played.ElectorsOf("REP"), played.ElectorsOf("DEM"));
            if (played.Decided() && played.WinnerParty == winner) { line = playedLine + " - it elects " + winner + " as played; nothing staged"; return true; }
            CampaignRun.Result result = sim.PlayerCampaignResult;
            if (!sim.TryPlayerCampaignShares(out Dictionary<string, double> _) || result?.FinalShares == null || sim.PlayerCampaign == null || sim.PlayerCampaign.Setup.Parties == null)
            {
                line = playedLine + " - no campaign ran up to the polling day, so the count read the prediction and there are no campaign shares to stage; the played count stands";
                return false;
            }
            CampaignRun.PartySetup[] parties = sim.PlayerCampaign.Setup.Parties;
            int iw = -1, il = -1;
            string loser = winner == "REP" ? "DEM" : "REP";
            for (int i = 0; i < parties.Length; i++) { if (parties[i].Name == winner) { iw = i; } else if (parties[i].Name == loser) { il = i; } }
            MethodInfo hold = typeof(SimulationManager).GetMethod("HoldUsPresidentialCount", BindingFlags.Instance | BindingFlags.NonPublic, null, Type.EmptyTypes, null);
            if (iw < 0 || il < 0 || hold == null) { line = playedLine + $" - the campaign stages no {winner} or {loser}, or SimulationManager.HoldUsPresidentialCount is not found; the played count stands"; return false; }
            double[] was = (double[])result.FinalShares.Clone();
            double sum = was[iw] + was[il];
            void Recount(double w, double l)
            {
                result.FinalShares[iw] = w;
                result.FinalShares[il] = l;
                usa.UsPresidentialElections.RemoveAll(c => c.PollingDay.Date == day);
                usa.UsCongressElections.RemoveAll(c => c.PollingDay.Date == day);
                hold.Invoke(sim, null);
            }
            for (int step = 1; step <= 160; step++)   // the review's M1: the least margin - upward from a tie in quarter points (to 40), never the played margin turned
            {
                double m = 0.0025 * step;
                if (m <= 0.0) { continue; }
                Recount(sum / 2.0 + m / 2.0, sum / 2.0 - m / 2.0);
                UsPresidentialElection.Contest recount = usa.UsPresidentialElections.Find(c => c.PollingDay.Date == day);
                if (recount != null && recount.Decided() && recount.WinnerParty == winner)
                {
                    line = string.Format(CultureInfo.InvariantCulture, "{0}; the campaign's final shares re-split {1} {2:0.00} % / {3} {4:0.00} % (played {1} {5:0.00} % / {3} {6:0.00} %, a {7:0.00}-point margin for {1}), the count held again: {8} ({9}), the College REP {10} DEM {11}",
                        playedLine, winner, 100.0 * result.FinalShares[iw], loser, 100.0 * result.FinalShares[il], 100.0 * was[iw], 100.0 * was[il], 100.0 * m, recount.Winner, recount.WinnerParty, recount.ElectorsOf("REP"), recount.ElectorsOf("DEM"));
                    return true;
                }
            }
            Recount(was[iw], was[il]);
            line = playedLine + $" - no re-split up to a 40.00-point margin elects {winner} outright; the played shares and count are put back";
            return false;
        }

        /// <summary>
        /// A signing takeover the game opened by itself (a division the player's country recorded - the government's budget in the run-up,
        /// PS-3e): filmed under its own stem and sealed through the controller's own verb, as a player's click would. Nothing is filmed where
        /// no signing is up - the frame is the game's to show, not the film's to stage, so it is never declared.
        /// </summary>
        private IEnumerator SealLiveSigning(GameController controller, string stem)
        {
            FieldInfo kindField = controller.GetType().GetField("_canvasScreenKind", BindingFlags.Instance | BindingFlags.NonPublic);
            // Every signing the game opened, one after another (a second division queues behind the first): the first filmed under the stem,
            // each sealed; bounded, and loud if the queue never empties.
            for (int n = 0; n < 8; n++)
            {
                for (int w = 0; w < SettleFrames * 4 && kindField?.GetValue(controller)?.ToString() != "Signing"; w++) { yield return null; }   // a queued one is built on a later frame
                if (kindField == null || kindField.GetValue(controller)?.ToString() != "Signing") { yield break; }   // the kind is set when the takeover is built, before its cover is in
                yield return WaitForCanvasSettle(controller, wantActive: true);
                yield return Settle();
                string frame = n == 0 ? stem : stem + "_" + (n + 1).ToString(CultureInfo.InvariantCulture);
                Claim("signing");
                yield return Capture(frame);
                RecordCanvasTextAssert(frame, controller);
                InvokeNoArg(controller, "SignPendingDivision");
                yield return WaitForCanvasSettle(controller, wantActive: false);
                yield return Settle();
                Claim("imgui");
                Debug.Log($"SHOT: {frame} - a signing the game opened by itself, filmed and sealed.");
            }
            Debug.LogError($"SHOT: {stem} - eight signings sealed and another still up; the queue does not empty.");
            _failed++;
        }

        private int _memDays;

        /// <summary>
        /// §648 (the 2560 interrupts film died of Unity's "System out of memory" in its day loop): every 30 game days, the Editor's
        /// working set, the managed heap and Unity's own allocator - the growth curve, read off the log's MEM lines.
        /// </summary>
        private void MemTick(SimulationManager sim)
        {
            _memDays++;
            if (_memDays % 30 != 0) { return; }
            System.Diagnostics.Process self = System.Diagnostics.Process.GetCurrentProcess();
            self.Refresh();
            Debug.Log(string.Format(CultureInfo.InvariantCulture, "MEM: day {0} {1:yyyy-MM-dd} ws {2} MB managed {3} MB unity-allocated {4} MB unity-reserved {5} MB mono-used {6} MB",
                _memDays, sim.CurrentDate, self.WorkingSet64 / 1048576, GC.GetTotalMemory(false) / 1048576,
                UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong() / 1048576, UnityEngine.Profiling.Profiler.GetTotalReservedMemoryLong() / 1048576,
                UnityEngine.Profiling.Profiler.GetMonoUsedSizeLong() / 1048576));
        }

        /// <summary>With the clock set to run, the date must not move: that is what an interrupt IS. Restores the speed it found.</summary>
        private IEnumerator AssertClockHeld(GameController controller, SimulationManager sim, string what)
        {
            FieldInfo speedField = controller.GetType().GetField("_gameSpeed", BindingFlags.Instance | BindingFlags.NonPublic);
            object wasSpeed = speedField?.GetValue(controller);
            SetEnumField(controller, "_gameSpeed", "VeryFast");
            DateTime before = sim.CurrentDate;
            for (int i = 0; i < HeldWatchFrames; i++) { yield return null; }
            DateTime after = sim.CurrentDate;
            if (after != before)
            {
                Debug.LogError($"SHOT: the clock PASSED {what} - {before:yyyy-MM-dd} -> {after:yyyy-MM-dd} over {HeldWatchFrames} frames at VeryFast with the interrupt up. The player can miss it.");
                _failed++;
            }
            else
            {
                Debug.Log($"SHOT: the clock is HELD on {what} - {before:yyyy-MM-dd} unchanged over {HeldWatchFrames} frames at VeryFast.");
            }

            if (wasSpeed != null) { speedField.SetValue(controller, wasSpeed); }
        }

        /// <summary>After a dismissal the clock should run again unless something else holds it; reported, not asserted, because another interrupt (a cabinet decision, a budget window) can legitimately be waiting behind this one.</summary>
        private IEnumerator ReportClockAfterDismissal(GameController controller, SimulationManager sim, string what)
        {
            FieldInfo speedField = controller.GetType().GetField("_gameSpeed", BindingFlags.Instance | BindingFlags.NonPublic);
            object wasSpeed = speedField?.GetValue(controller);
            SetEnumField(controller, "_gameSpeed", "VeryFast");
            DateTime before = sim.CurrentDate;
            for (int i = 0; i < HeldWatchFrames; i++) { yield return null; }
            DateTime after = sim.CurrentDate;
            Debug.Log(after != before
                ? $"SHOT: after dismissing {what} the clock runs again - {before:yyyy-MM-dd} -> {after:yyyy-MM-dd}."
                : $"SHOT: after dismissing {what} the clock is still held ({before:yyyy-MM-dd}) - another interrupt is waiting behind it, or the game is over; read the banner in the frame.");
            if (wasSpeed != null) { speedField.SetValue(controller, wasSpeed); }
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
                      $"(seed {CampaignFilmSeed}); economy over the term {perceived:F1}/100 read off the live country; " +
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
        /// same `Debates.Resolve` the model uses, on the `Debate` stream at a fixed seed - so the
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
                    Pressure = null,   // no campaign: every line zero (A-LOOP q24 5a's form)
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

                var snapshot = new CampaignMapSnapshot(campaign, regions, Sweden2022Parties, 0, bought, perRegion,
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
        /// comes from the running game rather than from staging. §752 (A1): the term's reading, the row's own figure now.</summary>
        private double ReadPerceivedEconomy(GameController controller)
        {
            FieldInfo simField = controller.GetType().GetField("_simulationManager", BindingFlags.Instance | BindingFlags.NonPublic);
            var sim = simField?.GetValue(controller) as SimulationManager;
            Country player = sim?.World?.GetCountry(_countryId);
            if (player == null)
            {
                Debug.LogError("SHOT: the live country could not be read - the economy-over-the-term figure would be invented, so the pass fails rather than guessing.");
                return double.NaN;
            }

            return EconomicVote.RecordTermIndex(player, sim.CurrentDate);
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

        /// <summary>§657: the run-up's declarations page, opened as the HQ's DECLARED chip opens it, filmed, and closed back to the HQ - the page's
        /// rows are the timeline's facts standing on the day (`DeclaredRedLines.StandingOn`), logged beside the frame so the film is read against them.</summary>
        private IEnumerator FilmDeclared(GameController controller, SimulationManager sim, string frame)
        {
            if (!DeclaredRedLines.HasTimeline(_countryId))
            {
                // PS-6 US-30: a staged campaign with no dated declarations (the USA's) offers no DECLARED chip - nothing to film, and nothing failed
                Debug.Log($"SHOT: {frame} - {_countryId} carries no dated declarations; the HQ offers no DECLARED page, and the frame is not filmed.");
                yield break;
            }
            InvokeNoArg(controller, "OpenLiveDeclared");
            FieldInfo open = controller.GetType().GetField("_liveDeclaredOpen", BindingFlags.Instance | BindingFlags.NonPublic);
            if (!(open?.GetValue(controller) is bool isOpen && isOpen))
            {
                Debug.LogError($"SHOT: {frame} - the declarations page did not open (the HQ's DECLARED chip is offered only over a live campaign with a dated timeline).");
                _failed++;
                yield break;
            }
            yield return Settle();
            Claim("imgui");
            yield return Capture(frame);
            var standing = DeclaredRedLines.StandingOn(_countryId, sim.CurrentDate);
            var lifted = DeclaredRedLines.LiftedSince(_countryId, WorldClock.ElectionDayOf(_countryId, WorldClock.SeatedVintage(_countryId, sim.CurrentDate)), sim.CurrentDate);
            var rows = new List<string>();
            foreach (DeclaredRedLines.DatedFact f in standing) { rows.Add(f.Kind + " " + f.Party + (f.Other != null ? ">" + f.Other : string.Empty)); }
            var liftedRows = new List<string>();
            foreach (DeclaredRedLines.DatedFact f in lifted) { liftedRows.Add(f.Party + ">" + f.Other); }
            Debug.Log($"SHOT: {frame} - {sim.CurrentDate:yyyy-MM-dd}: {standing.Count} declared standing ({string.Join(", ", rows.ToArray())}); lifted {lifted.Count} ({string.Join(", ", liftedRows.ToArray())}).");
            InvokeNoArg(controller, "CloseLiveDeclared");
            yield return Settle();
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

        /// <summary>§647's premise 2, exactly (s918): a partner refuses FOR THE POSTS - offered none of those Gamson's law allocates it, K124's refusal
        /// (`Formateur.NoPostRefusal`), not a red line's.</summary>
        private static bool RefusesForThePosts(PartyAnswer answer) =>
            answer != null && !answer.Accepts && answer.PostsOffered == 0 && answer.PostsExpected > 0
            && answer.Reason != null && answer.Reason.StartsWith(Formateur.NoPostRefusal, StringComparison.Ordinal);

        private static void ResetScrolls(object target) => SetScrolls(target, 0f);

        private static void ScrollBy(object target, float y) => SetScrolls(target, y);

        /// <summary>S10 (10b): the sessions that film the debt chart's framework line - Design's two (Sweden below its anchor, Italy above the Treaty's line) and the
        /// USA, whose chart draws the NO RATIO IN LAW stamp instead of a line.</summary>
        private static readonly CountryId[] DebtLineCountries = { CountryId.Sweden, CountryId.Italy, CountryId.USA };

        /// <summary>S10 (10b): DECLARED - how far past the line a staged series is set, in points of GDP, so the frame reads plainly on its side of it.</summary>
        private const float DebtLineStageMarginPoints = 3f;

        /// <summary>S10 (10b): how far above the plot's own top the frame is scrolled (px at 1280, scaled) - the chart's head and its neighbours in frame.</summary>
        private const float DebtLineHeadroom = 120f;

        /// <summary>
        /// S10 (Design's return of 2026-10-30, 10b, "Yes, please film it: Sweden below the anchor and Italy above the Treaty line, at 1280 and 2560, so Design can
        /// check the label against a real series"): THE DEBT CHART'S FRAMEWORK LINE ON FILM. The Statistics sheet scrolled to the debt chart - its top read off the
        /// chart's own plot as the last repaint laid it out (`_debtGraph.PlotAreaRect`, content coordinates of the sheet's scroll) - filmed at rest
        /// (`02a2_statistics_debt_line`), then with its gutter's slip pinned (`02a2_statistics_debt_line_slips_pinned`). <b>Staged where the run's own series does
        /// not stand on the side Design asked for</b> (the `_staged` frames' rule: the state put back after): the kept quarterly series is shifted, whole, so its
        /// window clears the line by <see cref="DebtLineStageMarginPoints"/>, its shape the run's own; the SHOT line says whether it was staged and by how much.
        /// The USA is filmed as it stands.
        /// </summary>
        private IEnumerator FilmDebtLine(GameController controller)
        {
            const string rest = "02a2_statistics_debt_line", pinned = rest + "_slips_pinned";
            Expect(rest, pinned);
            BindingFlags any = BindingFlags.Instance | BindingFlags.NonPublic;
            var player = controller.GetType().GetField("_playerCountry", any)?.GetValue(controller) as Country;
            var graph = controller.GetType().GetField("_debtGraph", any)?.GetValue(controller) as GraphRenderer;
            var section = controller.GetType().GetField("_statsSeriesSection", any)?.GetValue(controller) as GraphSection;
            MethodInfo pinOnAnchor = controller.GetType().GetMethod("PinSlipOnAnchorForFilm", any);
            FieldInfo pins = controller.GetType().GetField("_slipPins", any);
            List<float> points = player?.History?.DebtToGdpRatio?.Quarterly;
            if (player == null || graph == null || section == null || pinOnAnchor == null || pins == null || points == null)
            {
                Debug.LogError($"SHOT: S10 10b - the controller has no _playerCountry / _debtGraph / _statsSeriesSection / PinSlipOnAnchorForFilm / _slipPins (or no kept debt series); {rest} and {pinned} NOT written.");
                _failed += 2;
                yield break;
            }

            float? limit = FiscalRules.DebtReferencePercent(_countryId);
            var kept = new List<float>(points);
            string staged = "the run's own series";
            (int start, int end) = GraphRenderer.WindowOf(points.Count, section.PageFromEnd);
            if (limit.HasValue && end > start)
            {
                float lo = float.MaxValue, hi = float.MinValue;
                for (int i = start; i < end; i++) { lo = Mathf.Min(lo, points[i]); hi = Mathf.Max(hi, points[i]); }
                bool below = _countryId == CountryId.Sweden;   // Design's ask: Sweden below its anchor, Italy above the Treaty's line
                float shift = below ? Mathf.Min(0f, limit.Value - DebtLineStageMarginPoints - hi) : Mathf.Max(0f, limit.Value + DebtLineStageMarginPoints - lo);
                if (shift != 0f)
                {
                    for (int i = 0; i < points.Count; i++) { points[i] += shift; }
                    staged = $"STAGED - the kept series shifted {shift:+0.00;-0.00} points of GDP (its window {lo:0.00}-{hi:0.00} % against the line at {limit.Value:0} %), its shape the run's own";
                }
                else { staged = $"the run's own series, its window {lo:0.00}-{hi:0.00} % {(below ? "below" : "above")} the line at {limit.Value:0} %"; }
            }
            try
            {
                ResetScrolls(controller);
                yield return Settle();
                yield return Settle();
                Rect plot = graph.PlotAreaRect;
                if (plot.width <= 0f || plot.height <= 0f)
                {
                    Debug.LogError($"SHOT: S10 10b - the debt chart was not laid out on the Statistics sheet (no plot rect); {rest} and {pinned} NOT written.");
                    _failed += 2;
                    yield break;
                }
                float scroll = Mathf.Max(0f, plot.y - V35.Px(DebtLineHeadroom));
                SetScrolls(controller, scroll);
                yield return Settle();
                yield return Settle();
                Rect label = graph.ThresholdLabelRect;
                if (label.width <= 0f || label.height <= 0f) { Debug.LogError($"SHOT: S10 10b - the debt chart drew no {(limit.HasValue ? "framework label" : "NO RATIO IN LAW stamp")} in its gutter ({_countryId})"); _failed++; }
                Debug.Log($"SHOT: {rest} - {_countryId}: {(limit.HasValue ? FiscalRules.DebtReferenceLabel(_countryId) + " line" : FiscalRules.NoDebtRatioStamp(_countryId) + ", no line")}; {staged}; the plot at {plot.y:0} in the sheet's scroll, filmed from {scroll:0}; the gutter's words at {label.x:0},{label.y:0} {label.width:0}x{label.height:0}");
                yield return Capture(rest);
                pinOnAnchor.Invoke(controller, new object[] { "chart:debt/comfortable", new Vector2(label.center.x, label.center.y) });
                yield return Settle();
                yield return Settle();
                yield return Capture(pinned);
                (pins.GetValue(controller) as System.Collections.IList)?.Clear();
            }
            finally
            {
                for (int i = 0; i < kept.Count && i < points.Count; i++) { points[i] = kept[i]; }   // the run's series put back, staged or not
                ResetScrolls(controller);
            }
            yield return Settle();
        }

        /// <summary>PS-6 US-35: where the Parliament tab's rows are filmed - for a country whose Congress view draws (the USA), a little above the rows' own top
        /// as the last repaint measured it (`_parliamentRowsTop`), so the bills, the dated rows and the divisions are in frame however tall the view stands;
        /// every other country keeps <paramref name="fallback"/>, so no other film moves. Logged as a SHOT line.</summary>
        private float CongressRowsScroll(object controller, float fallback)
        {
            if (_countryId != CountryId.USA) { return fallback; }
            object top = controller.GetType().GetField("_parliamentRowsTop", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(controller);
            if (!(top is float rowsTop) || rowsTop <= 0f)
            {
                Debug.LogError("SHOT: PS-6 US-35 - the controller has no _parliamentRowsTop (or it is unmeasured); the rows frame scrolls to the old fixed 900.");
                _failed++;
                return fallback;
            }
            float y = Mathf.Max(0f, rowsTop - 60f);
            Debug.Log($"SHOT: PS-6 US-35 - the Parliament tab's rows at {rowsTop:0} in the scroll; filmed from {y:0} (the Congress view above them)");
            return y;
        }

        /// <summary>P4-C2: every public float on the country, its state and its sectors, as raw bits - the snapshot the
        /// repeal assertion compares. Order is reflection order, the same on both sides of the comparison.</summary>
        private static List<(string Name, int Bits)> SnapshotFloats(Country country)
        {
            var list = new List<(string Name, int Bits)>();
            foreach (FieldInfo f in typeof(Country).GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                if (f.FieldType == typeof(float)) { list.Add(("Country." + f.Name, BitConverter.SingleToInt32Bits((float)f.GetValue(country)))); }
            }
            foreach (FieldInfo f in typeof(EconomyState).GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                if (f.FieldType == typeof(float)) { list.Add(("State." + f.Name, BitConverter.SingleToInt32Bits((float)f.GetValue(country.State)))); }
            }
            foreach (Sector sector in country.Sectors)
            {
                foreach (FieldInfo f in typeof(Sector).GetFields(BindingFlags.Public | BindingFlags.Instance))
                {
                    if (f.FieldType == typeof(float)) { list.Add(($"Sector.{sector.Type}.{f.Name}", BitConverter.SingleToInt32Bits((float)f.GetValue(sector)))); }
                }
            }
            return list;
        }

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
