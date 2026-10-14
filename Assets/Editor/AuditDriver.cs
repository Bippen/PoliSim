using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using PoliSim.Data;
using PoliSim.Elections;
using PoliSim.Persistence;
using PoliSim.Simulation;
using PoliSim.Testing;
using PoliSim.UI;
using UnityEngine;
using UnityEngine.UI;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// One audit run, as a plan line names it (`key=value` pairs separated by spaces or tabs).
    /// <list type="bullet">
    /// <item>`id` - the run's name (letters, digits, `_ . -`); required, unique in the plan.</item>
    /// <item>`country` - a `CountryId` (Sweden, Germany, Poland, Italy, USA, France); required.</item>
    /// <item>`party` - the party row the player clicks (its key, `PoliticalParty.Abbrev`); required unless `create=` is given.</item>
    /// <item>`create` - a created party's origin (Sweden): CREATE A PARTY is clicked; the draft is filled and committed through the flow's own commit.</item>
    /// <item>`style` - idle (default), historical, fuzz, exploit, extreme.</item>
    /// <item>`seed` - the master seed (default 777); the style's own choices draw from a generator seeded with it, never from the game's streams.</item>
    /// <item>`days` - in-game days to play after the start (default 30); the run also ends at game over.</item>
    /// <item>`speed` - 1, 2 or 3, the new game's speed through the settings (default 3).</item>
    /// <item>`delta` - `Time.captureDeltaTime`, seconds a frame (default 0.25: one day a frame at 3x).</item>
    /// <item>`saveload` - comma-separated days: at each, save through the saves screen's method to a scratch file and load it back through the menu's route, then continue.</item>
    /// <item>`speeds` - comma-separated `day:speed` changes, speed P, 1, 2 or 3; P pauses for `pauseframes` frames, then the speed before it returns.</item>
    /// <item>`pauseframes` - frames a P holds (default 30).</item>
    /// <item>`holdcampaign`, `holdbudget` - the two hold settings, 1 or 0 (default 1, a fresh player's).</item>
    /// <item>`gui` - deliver OnGUI every Nth frame (default 1: every frame, as a Game View does).</item>
    /// <item>`compare` - another run's id, run earlier in the plan: this run must reach the same trajectory digest.</item>
    /// <item>`stall` - frames with no day passed and nothing the harness can answer before the run is called a softlock (default 2000).</item>
    /// <item>`wall` - wall-clock seconds before the run is stopped as overrun (default 3600).</item>
    /// </list>
    /// </summary>
    internal sealed class AuditSpec
    {
        internal string Id;
        internal string Country;
        internal string Party;
        internal string Create;
        internal string Style = "idle";
        internal int Seed = 777;
        internal int Days = 30;
        internal int Speed = 3;
        internal float Delta = 0.25f;
        internal readonly List<int> SaveLoad = new List<int>();
        internal readonly List<(int Day, string Speed)> Speeds = new List<(int, string)>();
        internal int PauseFrames = 30;
        internal bool HoldCampaign = true;
        internal bool HoldBudget = true;
        internal int GuiEvery = 1;
        internal string Compare;
        internal int Stall = 2000;
        internal int Wall = 3600;
        /// <summary>extreme: deficit (default), surplus, big or small - see the styles' file.</summary>
        internal string Pole = "deficit";
        /// <summary>exploit: the inventory's lead the run plays (X-01, X-02, X-02S, X-04, X-16, and the sweep's X-05, X-15, X-17, X-07S).</summary>
        internal string Exploit;
        /// <summary>fuzz: days between its own actions (default 21, a bill's passage).</summary>
        internal int Every = 21;
        /// <summary>Days at which to save through the saves screen's write and play on, loading nothing - a later run resumes the file.</summary>
        internal readonly List<int> Save = new List<int>();
        /// <summary>A save file in the plan's output folder to resume in this run's fresh session, through the saves screen's load, in place of NEW GAME.</summary>
        internal string Resume;
        /// <summary>The day count a resumed run starts at (the day the resumed file was saved on).</summary>
        internal int StartDay;
        /// <summary>A day on which the cabinet's Search is pressed and the first candidate of each shortlisted portfolio appointed (the cards' own bodies) - a resumed game's Search finds the shortlists its save carries and draws none (GA-1-2).</summary>
        internal int Cabinet = -1;
        /// <summary>A day on which Search is pressed, the game saved, and only then the candidates appointed - the save carries the shortlist since GA-1-2 (inventory A S-24 found it dropped).</summary>
        internal int SearchSave = -1;
        /// <summary>The autosave setting, in days (0 off - the default; 7, 30 or 90 as the settings screen offers). Autosave writes the PLAYER'S saves
        /// folder, so a plan that sets it is refused unless that folder is empty or absent (moved aside first, and restored after).</summary>
        internal int Autosave;
        /// <summary>The autosave slots (1-5, the setting's range; default 3).</summary>
        internal int Slots = 3;
        /// <summary>Days on which the Parliament tab's motion is moved - MOVE NO CONFIDENCE in the Riksdag, ELECT A SUCCESSOR in the Bundestag (one
        /// verb, `MoveNoConfidence`, the rows' own call).</summary>
        internal readonly List<int> Motion = new List<int>();
        /// <summary>A day on which WITHDRAW SUPPORT is pressed (a support party's agreement block).</summary>
        internal int Withdraw = -1;
        /// <summary>A day on which LEAVE THE GOVERNMENT is pressed (a junior partner's row).</summary>
        internal int Leave = -1;
        /// <summary>The central bank's head the chair hold appoints, in place of the style's own pick: dove (the lowest RateBias), hawk (the
        /// highest) or first (the card the list opens on) - a controlled pair is one game with only the head changed.</summary>
        internal string Chair;
        /// <summary>The live campaign's chips the run presses (`CampaignActs`): runup (the run-up's priced verbs and the five hires, once), ground (rally,
        /// town hall, doors), air (TV, digital, post), all (the eight) or paced (the eight within what is left over the days left and the day's hours) -
        /// the campaign's mixes after the run-up, each legal act once a day.</summary>
        internal string Campaign;
        /// <summary>The Energy page's plant buttons the run presses (`FleetActs`): retirefossil, retirenuclear or buildgreen.</summary>
        internal string Energy;
        /// <summary>events=off: the random economic events switched off for the run (`EventSystem.Enabled`, a harness switch), restored at its end.</summary>
        internal bool EventsOff;
        /// <summary>The foreign-policy meeting's option the hold answers with (`MeetingPick`): first, last or approval.</summary>
        internal string Meeting;
        /// <summary>The screen the IMGUI pass lays out for (`geometry=WxH`, one of the four the film takes: 1280x720, 1600x950, 1920x1080, 2560x1440).</summary>
        internal int GeometryWidth = 1280, GeometryHeight = 720;
        /// <summary>Pages to capture, `day:Page` (Desk, Statistics, Decisions, Demographics, Budget, PolicyLaws, Politics, Energy): on that day the
        /// page is opened through the rail's own body, one frame of OnGUI is recorded by the dry film's label table, and the desk returns.</summary>
        internal readonly List<(int Day, string Page)> Captures = new List<(int, string)>();

        /// <summary>True when the run writes or reads the player's real saves folder (an autosave, or a resume from it - `resume=@file`).</summary>
        internal bool UsesRealSaves => Autosave > 0 || (Resume != null && Resume.StartsWith("@", StringComparison.Ordinal));

        private static readonly string[] Styles = { "idle", "historical", "fuzz", "exploit", "extreme" };
        private static readonly string[] Poles = { "deficit", "surplus", "big", "small", "spendup", "spenddown" };
        private static readonly string[] Exploits = { "X-01", "X-02", "X-02S", "X-04", "X-16", "X-05", "X-15", "X-17", "X-07S" };

        internal string Describe() =>
            $"{Id}: {Country} as {(string.IsNullOrEmpty(Create) ? Party : "a created party (" + Create + ")")}, style {Style}{(Style == "extreme" ? " " + Pole : Style == "exploit" ? " " + Exploit : Style == "fuzz" ? " every " + Every + " d" : string.Empty)}, seed {Seed}, {Days} day(s) at {Speed}x, delta {Delta.ToString("R", CultureInfo.InvariantCulture)} s"
            + (SaveLoad.Count > 0 ? ", save/load at day(s) " + string.Join(",", SaveLoad) : string.Empty)
            + (Speeds.Count > 0 ? ", speeds " + string.Join(",", Speeds.Select(s => s.Day + ":" + s.Speed)) : string.Empty)
            + $", holds campaign {(HoldCampaign ? 1 : 0)} budget {(HoldBudget ? 1 : 0)}, OnGUI every {GuiEvery} frame(s)"
            + (string.IsNullOrEmpty(Compare) ? string.Empty : ", compared with " + Compare);

        internal static AuditSpec Parse(string line, out string error)
        {
            error = null;
            var s = new AuditSpec();
            foreach (string token in line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries))
            {
                int eq = token.IndexOf('=');
                if (eq <= 0) { error = $"'{token}' is not key=value"; return null; }
                string key = token.Substring(0, eq).ToLowerInvariant();
                string value = token.Substring(eq + 1);
                try
                {
                    switch (key)
                    {
                        case "id": s.Id = value; break;
                        case "country": s.Country = value; break;
                        case "party": s.Party = value; break;
                        case "create": s.Create = value; break;
                        case "style": s.Style = value.ToLowerInvariant(); break;
                        case "seed": s.Seed = int.Parse(value, CultureInfo.InvariantCulture); break;
                        case "days": s.Days = int.Parse(value, CultureInfo.InvariantCulture); break;
                        case "speed": s.Speed = int.Parse(value, CultureInfo.InvariantCulture); break;
                        case "delta": s.Delta = float.Parse(value, CultureInfo.InvariantCulture); break;
                        case "saveload": s.SaveLoad.AddRange(value.Split(',').Where(v => v.Length > 0).Select(v => int.Parse(v, CultureInfo.InvariantCulture))); break;
                        case "speeds":
                            foreach (string pair in value.Split(',').Where(v => v.Length > 0))
                            {
                                string[] dv = pair.Split(':');
                                if (dv.Length != 2 || !(dv[1] == "P" || dv[1] == "1" || dv[1] == "2" || dv[1] == "3")) { error = $"speeds '{pair}' is not day:P|1|2|3"; return null; }
                                s.Speeds.Add((int.Parse(dv[0], CultureInfo.InvariantCulture), dv[1]));
                            }
                            break;
                        case "pauseframes": s.PauseFrames = int.Parse(value, CultureInfo.InvariantCulture); break;
                        case "holdcampaign": s.HoldCampaign = value == "1"; break;
                        case "holdbudget": s.HoldBudget = value == "1"; break;
                        case "gui": s.GuiEvery = Math.Max(1, int.Parse(value, CultureInfo.InvariantCulture)); break;
                        case "compare": s.Compare = value; break;
                        case "stall": s.Stall = int.Parse(value, CultureInfo.InvariantCulture); break;
                        case "wall": s.Wall = int.Parse(value, CultureInfo.InvariantCulture); break;
                        case "pole": s.Pole = value.ToLowerInvariant(); break;
                        case "exploit": s.Exploit = value.ToUpperInvariant(); break;
                        case "every": s.Every = int.Parse(value, CultureInfo.InvariantCulture); break;
                        case "save": s.Save.AddRange(value.Split(',').Where(v => v.Length > 0).Select(v => int.Parse(v, CultureInfo.InvariantCulture))); break;
                        case "resume": s.Resume = value; break;
                        case "startday": s.StartDay = int.Parse(value, CultureInfo.InvariantCulture); break;
                        case "cabinet": s.Cabinet = int.Parse(value, CultureInfo.InvariantCulture); break;
                        case "searchsave": s.SearchSave = int.Parse(value, CultureInfo.InvariantCulture); break;
                        case "autosave": s.Autosave = int.Parse(value, CultureInfo.InvariantCulture); break;
                        case "slots": s.Slots = int.Parse(value, CultureInfo.InvariantCulture); break;
                        case "motion": s.Motion.AddRange(value.Split(',').Where(v => v.Length > 0).Select(v => int.Parse(v, CultureInfo.InvariantCulture))); break;
                        case "withdraw": s.Withdraw = int.Parse(value, CultureInfo.InvariantCulture); break;
                        case "leave": s.Leave = int.Parse(value, CultureInfo.InvariantCulture); break;
                        case "chair": s.Chair = value.ToLowerInvariant(); break;
                        case "campaign": s.Campaign = value.ToLowerInvariant(); break;
                        case "energy": s.Energy = value.ToLowerInvariant(); break;
                        case "events": s.EventsOff = value.ToLowerInvariant() == "off"; break;
                        case "meeting": s.Meeting = value.ToLowerInvariant(); break;
                        case "geometry":
                            {
                                string[] wh = value.ToLowerInvariant().Split('x');
                                if (wh.Length != 2) { error = $"geometry= '{value}' is not WxH"; return null; }
                                s.GeometryWidth = int.Parse(wh[0], CultureInfo.InvariantCulture); s.GeometryHeight = int.Parse(wh[1], CultureInfo.InvariantCulture);
                                break;
                            }
                        case "capture":
                            foreach (string pair in value.Split(',').Where(v => v.Length > 0))
                            {
                                string[] dp = pair.Split(':');
                                if (dp.Length != 2) { error = $"capture '{pair}' is not day:Page"; return null; }
                                s.Captures.Add((int.Parse(dp[0], CultureInfo.InvariantCulture), dp[1]));
                            }
                            break;
                        default: error = $"unknown key '{key}'"; return null;
                    }
                }
                catch (FormatException) { error = $"'{token}' does not parse"; return null; }
            }

            if (string.IsNullOrEmpty(s.Id) || s.Id.Any(ch => !(char.IsLetterOrDigit(ch) || ch == '_' || ch == '-' || ch == '.'))) { error = "id= is missing or holds a character outside letters, digits, _ . -"; return null; }
            if (string.IsNullOrEmpty(s.Country) || !Enum.TryParse(s.Country, false, out CountryId _)) { error = $"country= '{s.Country}' is not a CountryId"; return null; }
            if (string.IsNullOrEmpty(s.Party) && string.IsNullOrEmpty(s.Create)) { error = "party= (or create=) is required"; return null; }
            if (!Styles.Contains(s.Style)) { error = $"style= '{s.Style}' is not one of {string.Join(", ", Styles)}"; return null; }
            if (!Poles.Contains(s.Pole)) { error = $"pole= '{s.Pole}' is not one of {string.Join(", ", Poles)}"; return null; }
            if (s.Style == "exploit" && !Exploits.Contains(s.Exploit ?? string.Empty)) { error = $"style=exploit needs exploit= one of {string.Join(", ", Exploits)}"; return null; }
            if (s.Speed < 1 || s.Speed > 3) { error = "speed= is 1, 2 or 3"; return null; }
            if (s.Delta <= 0f || s.Delta > 0.333f) { error = "delta= is in (0, 0.333] - Unity caps a frame's delta at the project's maximum timestep"; return null; }
            if (s.Days < 0) { error = "days= is not negative"; return null; }
            if (!GameSettings.AutosaveDayChoices.Contains(s.Autosave)) { error = $"autosave= is one of the settings screen's choices ({string.Join(", ", GameSettings.AutosaveDayChoices)})"; return null; }
            if (s.Slots < GameSettings.MinSlots || s.Slots > GameSettings.MaxSlots) { error = $"slots= is {GameSettings.MinSlots}..{GameSettings.MaxSlots}"; return null; }
            if (s.Chair != null && s.Chair != "dove" && s.Chair != "hawk" && s.Chair != "first") { error = "chair= is dove, hawk or first"; return null; }
            if (s.Campaign != null && s.Campaign != "runup" && s.Campaign != "ground" && s.Campaign != "air" && s.Campaign != "all" && s.Campaign != "paced") { error = "campaign= is runup, ground, air, all or paced"; return null; }
            if (s.Energy != null && s.Energy != "retirefossil" && s.Energy != "retirenuclear" && s.Energy != "buildgreen") { error = "energy= is retirefossil, retirenuclear or buildgreen"; return null; }
            if (s.Meeting != null && s.Meeting != "first" && s.Meeting != "last" && s.Meeting != "approval") { error = "meeting= is first, last or approval"; return null; }
            if (!(s.GeometryWidth == 1280 && s.GeometryHeight == 720 || s.GeometryWidth == 1600 && s.GeometryHeight == 950 || s.GeometryWidth == 1920 && s.GeometryHeight == 1080 || s.GeometryWidth == 2560 && s.GeometryHeight == 1440)) { error = "geometry= is one of the film's four: 1280x720, 1600x950, 1920x1080, 2560x1440"; return null; }
            return s;
        }
    }

    /// <summary>
    /// The audit's driver for one run: it starts a game the way a player does (the main menu's NEW GAME, the country's folder, the start card
    /// and SELECT, the party's row - Canvas buttons clicked through their own `onClick`), lets the controller's `Update` advance the days, answers
    /// every hold through the method its control calls, acts for the play style, interrupts as the plan asks, checks the invariants every day,
    /// and writes the trajectory. See `AuditHarness` for the route and its reasons.
    /// </summary>
    internal sealed partial class AuditDriver
    {
        private const BindingFlags Any = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

        private enum Phase { Boot, Menu, Selector, StartSheet, PartyPanel, CreateFlow, Opening, Playing, Done }

        private readonly AuditSpec _spec;
        private readonly string _dir;
        private readonly Action<int, string, string> _onEnd;
        private readonly StreamWriter _events;
        private readonly AuditInvariants _invariants;
        private AuditTrajectory _trajectory;

        private Phase _phase = Phase.Boot;
        private GameController _gc;
        private SimulationManager _sm;
        private MethodInfo _onGui;
        private int _frame;
        private int _phaseFrames;
        private int _framesSinceDay;
        private int _day;
        private DateTime _lastDate;
        private int _lastTurn;
        private int _lastElectionCount;
        private int _lastPresidentialCount;
        private readonly DateTime _wallStart = DateTime.UtcNow;
        private int _errors;
        private readonly List<string> _firstErrors = new List<string>();
        private readonly Dictionary<string, int> _answers = new Dictionary<string, int>(StringComparer.Ordinal);
        private readonly HashSet<int> _saveLoadDone = new HashSet<int>();
        private readonly HashSet<int> _speedsDone = new HashSet<int>();
        private int _pauseLeft;
        private string _resumeSpeed;
        private int _guiPasses;
        private double _guiMs;
        private bool _ended;

        internal AuditDriver(AuditSpec spec, string dir, Action<int, string, string> onEnd)
        {
            _spec = spec;
            _dir = dir;
            _onEnd = onEnd;
            _events = new StreamWriter(Path.Combine(dir, spec.Id + ".events.log"), false, new UTF8Encoding(false)) { NewLine = "\n" };
            _invariants = new AuditInvariants(Event);
            Application.logMessageReceived += OnLog;
            Event("RUN " + spec.Describe());
        }

        // ------------------------------------------------------------------ the frame

        /// <summary>§820 (GA-1-1): the live HQ as the harness last saw it, so an HQ the game closed itself - its run dropped, settled in the controller's
        /// LateUpdate (before a pump, or Unity's after the tick) - is logged with where the player then stands; the fix returns them to the desk (the
        /// finding: they landed on Statistics), and a self-close anywhere else ends the run. The harness's own closes (a page capture) re-read it, so
        /// they are not counted as the game's.</summary>
        private bool _hqOpenLastTick;

        private void WatchHq()
        {
            bool open = (bool)Get("_liveCampaignOpen");
            if (_hqOpenLastTick && !open)
            {
                bool desk = (bool)Get("_onDesk");
                Event($"SCREEN day {_day} ({_lastDate:yyyy-MM-dd}): the live HQ closed itself - the player on {(desk ? "the desk" : "the " + Get("_consolidatedTab") + " tab")}");
                if (!desk) { End(1, $"the live HQ closed itself on day {_day} and left the player off the desk (GA-1-1)"); }
            }
            _hqOpenLastTick = open;
        }

        internal void Tick()
        {
            if (_ended) { return; }
            _frame++;
            _phaseFrames++;
            try
            {
                if (_phase == Phase.Playing && _gc != null) { WatchHq(); }
                Step();
                if (!_ended && _gc != null && _onGui != null && _frame % _spec.GuiEvery == 0) { PumpGui(); }
            }
            catch (Exception e)
            {
                Event($"HARNESS FAILURE in phase {_phase} at frame {_frame}: {e.GetType().Name}: {e.Message}\n{e.StackTrace}");
                End(1, "harness failure: " + e.GetType().Name + ": " + e.Message);
            }
        }

        private void Step()
        {
            if ((DateTime.UtcNow - _wallStart).TotalSeconds > _spec.Wall) { End(1, $"OVERRUN - {_spec.Wall} s of wall clock passed at day {_day}"); return; }
            switch (_phase)
            {
                case Phase.Boot: Boot(); break;
                case Phase.Menu: Menu(); break;
                case Phase.Selector: Selector(); break;
                case Phase.StartSheet: StartSheet(); break;
                case Phase.PartyPanel: PartyPanel(); break;
                case Phase.CreateFlow: CreateFlow(); break;
                case Phase.Opening: Opening(); break;
                case Phase.Playing: Playing(); break;
            }
        }

        private void Go(Phase next)
        {
            Event($"phase {_phase} -> {next} at frame {_frame}");
            _phase = next;
            _phaseFrames = 0;
            _phaseWall = DateTime.UtcNow;
        }

        private DateTime _phaseWall = DateTime.UtcNow;

        /// <summary>A start phase is stuck when it has had both its frames and twenty seconds of wall clock: the Canvas seam's envelopes are timed in
        /// real seconds (`Time.unscaledTime`), so a frame count alone says nothing about whether a screen had the time it asks for.</summary>
        private bool PhaseStuck(int frames) => _phaseFrames > frames && (DateTime.UtcNow - _phaseWall).TotalSeconds > 20.0;

        /// <summary>The seam's state, for the log while a start phase waits on a screen.</summary>
        private string SeamState() =>
            $"phase {Get("_canvasPhase")}, screen {Get("_canvasScreenKind")}, live {Get("_canvasLive")}, menu passed {Get("_mainMenuPassed")} failed {Get("_mainMenuFailed")} choice {Get("_mainMenuChoice")}, selector failed {Get("_canvasSelectorFailed")}, "
            + $"selected {Get("_selectedPlayerCountryId") ?? "none"}; unscaledTime {Time.unscaledTime.ToString("F3", CultureInfo.InvariantCulture)} time {Time.time.ToString("F3", CultureInfo.InvariantCulture)} deltaTime {Time.deltaTime.ToString("F3", CultureInfo.InvariantCulture)}";

        // ------------------------------------------------------------------ the start, as a player makes it

        private void Boot()
        {
            _gc = Object.FindAnyObjectByType<GameController>();
            if (_gc == null)
            {
                if (PhaseStuck(300)) { End(1, "no GameController in the scene"); }
                return;
            }

            _sm = _gc.GetComponent<SimulationManager>();
            _onGui = typeof(GameController).GetMethod("OnGUI", Any);
            if (!DryGuiPass.Prepare() || _onGui == null) { End(1, $"the IMGUI pass cannot be driven ({DryGuiPass.Failure ?? "GameController.OnGUI"})"); return; }
            UiScreen.OverrideWidth = _spec.GeometryWidth;
            UiScreen.OverrideHeight = _spec.GeometryHeight - 21;   // the Game View's chrome, as the film takes it (UiScreenshotCapture's GameViewChromeHeight)

            // The seed first: the game's world is built when the party row is clicked, from this seed.
            SimulationRandom.Seed(_spec.Seed);
            AuditPrefs.Save(_dir);
            PreferencePins.Pin();
            AudioListener.volume = 0f;   // Elias (2026-10-14): the run is silent - the listener's volume, never a preference; every cue is still recorded
            _eventsWere = EventSystem.Enabled;
            if (_spec.EventsOff) { EventSystem.Enabled = false; }   // the harness switch (inventory D D-43), restored at the run's end
            GameSettings.DefaultSpeed = _spec.Speed - 1;
            GameSettings.HoldOnCampaignOpening = _spec.HoldCampaign;
            GameSettings.HoldOnBudgetWindow = _spec.HoldBudget;
            GameSettings.AutosaveDays = _spec.Autosave;
            GameSettings.AutosaveSlots = _spec.Slots;
            Time.captureDeltaTime = _spec.Delta;
            Event($"boot: seed {_spec.Seed}; preferences pinned (speed {GameSettings.DefaultSpeed + 1}x, holds campaign {GameSettings.HoldOnCampaignOpening} budget {GameSettings.HoldOnBudgetWindow}, autosave {(GameSettings.AutosaveDays == 0 ? "off" : GameSettings.AutosaveDays + " days, " + GameSettings.AutosaveSlots + " slots - the player's saves folder")}); captureDeltaTime {Time.captureDeltaTime.ToString("R", CultureInfo.InvariantCulture)} s; OnGUI at {UiScreen.Width}x{UiScreen.Height}");
            Go(Phase.Menu);
        }

        private void Menu()
        {
            if (!string.IsNullOrEmpty(_spec.Resume))
            {
                // A resumed run: the saves screen's confirmed Load writes _pendingLoadPath, and Update's safe point loads it - in this run's fresh
                // session, as a player who quit and came back does. The menu waits until it is the live surface, as a player's click would.
                if (FindButton("MainMenu", "NewGame") == null) { if (PhaseStuck(600)) { End(1, "SOFTLOCK at the main menu - it never came up to load from"); } return; }
                string path = _spec.Resume.StartsWith("@", StringComparison.Ordinal) ? Path.Combine(SaveGameService.DefaultSaveDirectory, _spec.Resume.Substring(1)) : Path.Combine(_dir, _spec.Resume);
                if (!File.Exists(path)) { End(1, $"resume= names {path}, which no earlier run wrote"); return; }
                Set("_pendingLoadPath", path);
                Count("load from the menu");
                Event($"LOAD from the menu: {_spec.Resume} through _pendingLoadPath (the saves screen's Load), in a fresh session");
                Go(Phase.Opening);
                return;
            }

            if (Click("MainMenu", "NewGame")) { Go(Phase.Selector); return; }
            // The menu is the first takeover; where it failed to build the controller goes to the selector itself.
            if (FindButton("CountrySelector", FolderName()) != null) { Event("the main menu never came up - the selector is live"); Go(Phase.Selector); return; }
            if (PhaseStuck(600)) { End(1, "SOFTLOCK at the main menu - NEW GAME never became clickable"); }
        }

        private string FolderName() => "Folder_" + _spec.Country;

        /// <summary>The country's folder on the selector. (A `scenario=` key clicked the scenario strip instead, until the challenge scenarios were
        /// retired, §825 - a plan that still names one is refused at parse, as any unknown key is.)</summary>
        private void Selector()
        {
            if (Click("CountrySelector", FolderName())) { Go(Phase.StartSheet); return; }

            if (_phaseFrames % 120 == 1) { Event("waiting for the selector: " + SeamState()); }
            if (PhaseStuck(600)) { End(1, $"SOFTLOCK at the selector - {FolderName()} never became clickable"); }
        }

        private void StartSheet()
        {
            Button select = FindButton("StartSheet", "Select");
            if (select != null && select.interactable)
            {
                select.onClick.Invoke();
                Count("click StartSheet/Select");
                Event("clicked StartSheet/Select");
                Go(Phase.PartyPanel);
                return;
            }

            // No card selected yet: the playable card is clicked (its button selects it, as a player's click does).
            if (select != null && StartPoints.TryPlayable((CountryId)Enum.Parse(typeof(CountryId), _spec.Country), out StartPoints.StartPoint start))
            {
                Click("StartSheet", "Start_" + start.Kind.Replace(' ', '_'));
            }

            if (PhaseStuck(600)) { End(1, "SOFTLOCK at the start sheet - SELECT never became clickable"); }
        }

        private void PartyPanel()
        {
            if (!string.IsNullOrEmpty(_spec.Create))
            {
                if (Click("PartyPanel", "CreateParty")) { Go(Phase.CreateFlow); return; }
            }
            else if (Click("PartyPanel", "Party_" + _spec.Party)) { RecordOpening(); Go(Phase.Opening); return; }

            if (PhaseStuck(600))
            {
                string rows = string.Join(" ", Object.FindObjectsByType<Button>().Where(b => b.isActiveAndEnabled && b.name.StartsWith("Party_", StringComparison.Ordinal)).Select(b => b.name).OrderBy(n => n, StringComparer.Ordinal));
                End(1, $"the party row {(string.IsNullOrEmpty(_spec.Create) ? "Party_" + _spec.Party : "CreateParty")} is not on the panel (rows: {rows})");
            }
        }

        /// <summary>The creation flow's five steps are IMGUI controls that only edit a draft; the draft is filled from the flow's own NewDraft and
        /// committed through the flow's START (`CommitPartyCreation`) - a declared shortcut past the steps' controls, never past the commit.</summary>
        private void CreateFlow()
        {
            CreatedParty draft = (CreatedParty)Get("_pcDraft");
            if (draft == null)
            {
                if (PhaseStuck(600)) { End(1, "the creation flow never opened its draft"); }
                return;
            }

            if (!Enum.TryParse(_spec.Create, true, out PartyOrigin origin)) { End(1, $"create= '{_spec.Create}' is not a PartyOrigin"); return; }
            PartyCreationFlow.ChooseOrigin(draft, origin);   // the origin card's own call
            string shortName = string.IsNullOrEmpty(_spec.Party) ? "AUD" : _spec.Party;
            draft.Name = "Audit party " + shortName;   // the name field's write
            draft.ShortName = shortName;
            draft.Key = PartyCreationFlow.KeyFrom(draft.Country, shortName);   // the short-name field's body
            draft.LeaderName = "Audit Leader";   // the leader field's write
            if (origin == PartyOrigin.Splinter && string.IsNullOrEmpty(draft.ParentKey))
            {
                Country home = World().GetCountry(draft.Country);
                string parent = home.ParliamentSeats.OrderByDescending(p => p.Value).ThenBy(p => p.Key, StringComparer.Ordinal).First().Key;
                PartyCreationFlow.ChooseParent(draft, parent);   // the parent row's own call: the largest party, a declared choice
            }

            string missing = PartyCreationFlow.Missing(draft);
            if (missing != null) { End(1, $"the created party's draft still lacks its {missing}"); return; }
            Call("CommitPartyCreation");
            Count("call CommitPartyCreation");
            Event($"created party {draft.Key} ({origin}) committed through CommitPartyCreation - the flow's START; its steps' controls are IMGUI and were filled through their own writes");
            if ((CountryId?)Get("_selectedPlayerCountryId") != null) { RecordOpening(); }
            Go(Phase.Opening);
        }

        /// <summary>The game's first state, recorded the moment the world opens (the party's click and the creation's commit open it in the same call),
        /// before the controller's Update can run a day - so day 0 is the start's own date whatever a frame's delta.</summary>
        private void RecordOpening()
        {
            Country c = PlayerCountry();
            _day = _spec.StartDay;
            _lastDate = _sm.CurrentDate;
            _lastTurn = _sm.CurrentTurn;
            _lastElectionCount = c.ElectionHistory.Count;
            _lastPresidentialCount = c.PresidentialElections.Count;
            _lastDivision = c.Divisions.Entries.Count > 0 ? c.Divisions.Entries[c.Divisions.Entries.Count - 1].Number : 0;
            _playWall = DateTime.UtcNow;
            _trajectory = new AuditTrajectory(_dir, _spec.Id);
            _trajectory.Day(_day, _lastDate, _lastTurn, c, false);
            _trajectory.Turn(_day, _lastDate, _lastTurn, World());
            Event($"the game {(string.IsNullOrEmpty(_spec.Resume) ? "opened" : "resumed")} on {_lastDate:yyyy-MM-dd} (day {_day}): {c.Id}, the player's {c.PlayerPartyAbbrev} is {(c.Government?.RoleOf(c.PlayerPartyAbbrev) ?? PlayerRole.None)} (government {c.Government?.PmParty} [{(c.Government == null ? string.Empty : string.Join("+", c.Government.Cabinet))}] '{c.Government?.Outcome}'), speed {Get("_gameSpeed")}");
            CheckDay(c);
        }

        private void Opening()
        {
            CountryId? selected = (CountryId?)Get("_selectedPlayerCountryId");
            if (selected.HasValue && !(bool)Get("_canvasLive") && Get("_canvasPhase").ToString() == "None")
            {
                if (_trajectory == null)
                {
                    // A resumed game: the load landed under the menu; the game resumes PAUSED, and the speed is set again as the chips set it -
                    // once the start day has had its acts (§821: `_speedAfterAct`).
                    RecordOpening();
                    _speedAfterAct = _spec.Speed == 1 ? "Normal" : _spec.Speed == 2 ? "Fast" : "VeryFast";
                    _invariants.RestartDebt();
                }

                Go(Phase.Playing);
                return;
            }

            if (PhaseStuck(900)) { End(1, "SOFTLOCK opening the game - the selector never covered out onto the desk"); }
        }

        // ------------------------------------------------------------------ play

        private void Playing()
        {
            Country c = PlayerCountry();
            DateTime now = _sm.CurrentDate;
            if (now != _lastDate)
            {
                int passed = (now - _lastDate).Days;
                _day += passed;
                _framesSinceDay = 0;
                if (passed > 1) { Event($"{passed} days passed in frame {_frame} (to {now:yyyy-MM-dd})"); }
                _lastDate = now;
                bool gameOver = (bool)Get("_isGameOver");
                _trajectory.Day(_day, now, _sm.CurrentTurn, c, gameOver);
                if (_sm.CurrentTurn != _lastTurn)
                {
                    _lastTurn = _sm.CurrentTurn;
                    _trajectory.Turn(_day, now, _lastTurn, World());
                    _invariants.Energy(c, _day, now);
                }

                TaxReadout(c);
                TickReadout();
                CheckDay(c);
            }
            else
            {
                _framesSinceDay++;
            }

            if ((bool)Get("_isGameOver"))
            {
                End(0, "GAME OVER: " + (Get("_gameOverReason") as string ?? "(no reason)"));
                return;
            }

            if (_day >= _spec.Days) { End(0, $"{_day} day(s) played"); return; }

            if (Interrupt(c)) { return; }
            if (CapturePages()) { return; }
            bool answered = AnswerHolds(c);
            Act(c);
            if (_speedAfterAct != null)
            {
                // §821: the day an interruption stopped on has had its acts - only now does the clock run again
                SetSpeed(_speedAfterAct);
                Event($"SPEED {_speedAfterAct} restored at day {_day}, after the day's acts");
                _speedAfterAct = null;
            }
            if (_framesSinceDay > _spec.Stall && !answered && _pauseLeft == 0)
            {
                End(1, $"SOFTLOCK - no day passed in {_framesSinceDay} frames at day {_day} ({now:yyyy-MM-dd}) and nothing the harness answers holds the clock: {HoldState(c)}");
            }
        }

        /// <summary>The count's per-valkrets shares as it left them (`NationalElection.LastRegional*`, read on the frame its record lands): every party
        /// under 4 % of the nation's votes that reaches 12 % in a valkrets - vallagen 14 kap. 3 § seats such a party there (inventory C D-11, GA-0-21) -
        /// and the player's party's best valkrets. Sweden alone (the 12 % route is Sweden's); a reading, not an invariant - GA-0-21 holds the ruling.</summary>
        private void RegionalReadout(Country c, ElectionRecord r)
        {
            if (c.Id != CountryId.Sweden) { return; }
            double[][] shares = NationalElection.LastRegionalShares;
            IReadOnlyList<string> keys = NationalElection.LastRegionalKeys;
            string[] names = NationalElection.LastRegionalNames;
            if (shares == null || keys == null || names == null || shares.Length != names.Length) { Event("REGIONAL: no per-valkrets shares on record for this count"); return; }
            for (int k = 0; k < keys.Count; k++)
            {
                double best = -1.0;
                int at = -1;
                for (int v = 0; v < shares.Length; v++) { if (k < shares[v].Length && shares[v][k] > best) { best = shares[v][k]; at = v; } }
                if (at < 0) { continue; }
                double national = r.Shares.TryGetValue(keys[k], out double n) ? n : double.NaN;
                bool route = national < 0.04 && best >= 0.12;
                if (!route && keys[k] != c.PlayerPartyAbbrev) { continue; }
                int seated = r.Seats.TryGetValue(keys[k], out int s) ? s : 0;
                Event(string.Format(CultureInfo.InvariantCulture, "REGIONAL {0}: {1:F2} % of the nation's votes, best {2:F2} % in {3}{4}", keys[k], national * 100.0, best * 100.0, names[at],
                    route ? $" - UNDER 4 % WITH 12 % IN A VALKRETS (vallagen 14 kap. 3 §), seated {seated}" : string.Empty));
            }
        }

        private string _taxSignature;
        private DateTime _tickMark;
        private int _tickDay;

        /// <summary>Area 11: each year's wall clock per day of play (a night's takeover included), so a thirty-year run shows whether the tick grows
        /// with the game's age - a list that never shrinks, a cache that never clears - where one mean over the run would hide it.</summary>
        private void TickReadout()
        {
            if (_tickMark == default) { _tickMark = DateTime.UtcNow; _tickDay = _day; return; }
            if (_day - _tickDay < 365) { return; }
            Event(string.Format(CultureInfo.InvariantCulture, "TICK days {0}-{1}: {2:F1} ms a day", _tickDay, _day, (DateTime.UtcNow - _tickMark).TotalMilliseconds / (_day - _tickDay)));
            _tickMark = DateTime.UtcNow;
            _tickDay = _day;
        }

        /// <summary>The player's tax lines as they stand (`TaxLine.Rate`, the rate in force - not the Budget page's draft) and the income tax's own
        /// sub-row rates where a bill has set them (`TaxLine.BracketRates` in brackets, -1 where the statute's figure stands), logged whenever one
        /// moves: a passed bill on its day, a statute's indexation at a boundary (inventory D GA-0-28: the carbon statute against a passed rate). With
        /// them the household burden as a share of GDP (`MacroSystem.HouseholdTaxesCollectedShare`) and its gap from the seed's statute
        /// (`MacroSystem.HouseholdBurdenGap`, the figure the consumption term multiplies) - GA-2-11: a bill that reached only the sub-rows moved the
        /// revenue and never the gap. Both are read the way the turn reads them; their one lazy step (a driver's reference taken on its first read
        /// above zero) is the turn's own by then.</summary>
        private void TaxReadout(Country c)
        {
            string signature = string.Join(" ", c.TaxLines.Where(t => t.IsImplemented).Select(t => t.Type + " " + t.Rate.ToString("0.###", CultureInfo.InvariantCulture)
                + (t.BracketRates == null || t.BracketRates.Length == 0 ? string.Empty : " [" + string.Join(";", t.BracketRates.Select(r => r.ToString("0.###", CultureInfo.InvariantCulture))) + "]")));
            if (signature == _taxSignature) { return; }
            _taxSignature = signature;
            Event($"TAXES day {_day} ({_lastDate:yyyy-MM-dd}, turn {_sm.CurrentTurn}): {signature}");
            Event(string.Format(CultureInfo.InvariantCulture, "BURDEN day {0}: households hand over {1:0.0000} % of GDP, {2:+0.0000;-0.0000;0.0000} points from the seed's statute",
                _day, MacroSystem.HouseholdTaxesCollectedShare(c) * 100f, MacroSystem.HouseholdBurdenGap(c) * 100f));
        }

        private void CheckDay(Country c)
        {
            _invariants.NoteDay(_sm);
            _invariants.Finite(World(), _day, _lastDate);
            _invariants.Bounds(World(), _day, _lastDate);
            _invariants.Debt(World(), _sm, _day, _lastDate);
            _invariants.Seats(c, _day, _lastDate);
            _invariants.Government(c, _sm, _day, _lastDate);
            while (_lastElectionCount < c.ElectionHistory.Count)
            {
                ElectionRecord r = c.ElectionHistory[_lastElectionCount++];
                Event($"ELECTION {r.Date:yyyy-MM-dd} ({r.Method}): seats {string.Join(" ", r.Seats.OrderByDescending(p => p.Value).Select(p => p.Key + " " + p.Value))}");
                _invariants.Election(c, r, _day, _lastDate);
                RegionalReadout(c, r);
            }

            if (_lastPresidentialCount != c.PresidentialElections.Count)
            {
                Event($"PRESIDENTIAL contests now {c.PresidentialElections.Count}");
                _lastPresidentialCount = c.PresidentialElections.Count;
            }

            // Every vote the chamber takes in the player's country, with its result - a bill's fate is what most of a style's levers turn on.
            foreach (DivisionRecord d in c.Divisions.Entries)
            {
                if (d.Number <= _lastDivision) { continue; }
                _lastDivision = d.Number;
                string contest = d.Contest == null ? string.Empty : $" ({d.Contest.VotesFor} for, {d.Contest.VotesAgainst} against, {d.Contest.Abstentions} abstaining{(d.Contest.AlternativeAdopted ? ", the alternative adopted" : string.Empty)})";
                Event($"DIVISION #{d.Number} {d.Date:yyyy-MM-dd}{(d.Motion ? " motion" : string.Empty)}: '{d.Title}' {(d.Passed ? "PASSED" : "FAILED")}{contest}");
            }
        }

        private int _lastDivision = int.MaxValue;

        /// <summary>The protocol's interruptions: save then load at a day (the saves screen's own write, the menu's own load route), and the speed
        /// chips. Returns true when this frame was spent interrupting. §821 (GA-1-2's review): AN INTERRUPTION NEVER COSTS ITS DAY THE DAY'S ACTS - the
        /// speed a pause's end, a load or a resume restores waits in `_speedAfterAct` until the day has had them, and a save does not take the frame.
        /// Before, the clock ran on at once and the next frame's Update played the day after first: a resumed cabinet run appointed on day 41 where
        /// the run that wrote its file had on day 40 - the whole of GA-1-2's measured Budget gap.</summary>
        private bool Interrupt(Country c)
        {
            if (_pauseLeft > 0)
            {
                if (--_pauseLeft == 0)
                {
                    _speedAfterAct = _resumeSpeed;
                    Event($"PAUSE ends at day {_day}: speed {_resumeSpeed}, after the day's acts");
                    _trajectory.Mark(_day, _lastDate, "resume " + _resumeSpeed);
                    return false;
                }

                return true;
            }

            foreach ((int day, string speed) in _spec.Speeds)
            {
                if (day > _day || _speedsDone.Contains(day)) { continue; }
                _speedsDone.Add(day);
                if (speed == "P")
                {
                    _resumeSpeed = Get("_gameSpeed").ToString();
                    SetSpeed("Paused");
                    _pauseLeft = Math.Max(1, _spec.PauseFrames);
                    Event($"PAUSE at day {_day} for {_pauseLeft} frame(s)");
                    _trajectory.Mark(_day, _lastDate, "pause");
                    return true;
                }

                string name = speed == "1" ? "Normal" : speed == "2" ? "Fast" : "VeryFast";
                SetSpeed(name);
                Event($"SPEED {speed}x at day {_day}");
                _trajectory.Mark(_day, _lastDate, "speed " + speed);
            }

            foreach (int day in _spec.SaveLoad)
            {
                if (day > _day || _saveLoadDone.Contains(day)) { continue; }
                if (!AtSafePoint()) { return false; }   // mid-ceremony is not a save point (the controller's own rule): wait for the queue to drain
                _saveLoadDone.Add(day);
                string path = Path.Combine(_dir, _spec.Id + ".day" + _day.ToString(CultureInfo.InvariantCulture) + ".save.json");
                string speed = Get("_gameSpeed").ToString();
                bool saved = (bool)Call("SaveToPath", path);
                if (!saved) { End(1, $"SAVE FAILED at day {_day} through SaveToPath({path})"); return true; }
                Set("_pendingLoadPath", path);   // the saves menu's confirmed load: Update's safe point runs LoadFromPath next frame
                _resumeSpeed = speed;
                _pendingResume = true;
                Event($"SAVE at day {_day} ({_lastDate:yyyy-MM-dd}) to {Path.GetFileName(path)} via SaveToPath, LOAD queued via _pendingLoadPath; speed {speed} to be restored (a load resumes paused)");
                _trajectory.Mark(_day, _lastDate, "save+load");
                return true;
            }

            foreach (int day in _spec.Save)
            {
                if (day > _day || _saveDone.Contains(day)) { continue; }
                if (!AtSafePoint()) { return false; }
                _saveDone.Add(day);
                SaveFile(day);   // §821: synchronous, and the day's acts follow in this frame as in the run without the save
            }

            if (_pendingResume && Get("_pendingLoadPath") == null)
            {
                _pendingResume = false;
                _speedAfterAct = _resumeSpeed;   // §821: the chips' write, once the day the load landed on has had its acts
                _sm = _gc.GetComponent<SimulationManager>();
                _invariants.RestartDebt();
                Event($"LOADED - the game resumed at {_sm.CurrentDate:yyyy-MM-dd}; speed {_resumeSpeed} to be restored after the day's acts (the chips' write)");
                return false;
            }

            return false;
        }

        private bool _pendingResume;
        /// <summary>§821: the speed an interruption restores, held until the day it stopped on has had its acts (`Playing`, after `Act`); null when none waits.</summary>
        private string _speedAfterAct;
        private readonly HashSet<int> _saveDone = new HashSet<int>();

        private int _rngDay = int.MinValue;
        private readonly Dictionary<string, System.Random> _rngs = new Dictionary<string, System.Random>(StringComparer.Ordinal);

        /// <summary>A style's choices are the run's own, never the game's streams - and keyed by the seed, the DAY and the kind of choice, so the choices
        /// made on a day are the same whatever the history before it: a run resumed from a file (resume=) chooses on its days exactly as the run that
        /// wrote the file did, and a difference between the two is the game's, not the harness's.</summary>
        private System.Random Rng(string kind)
        {
            if (_rngDay != _day) { _rngs.Clear(); _rngDay = _day; }
            if (_rngs.TryGetValue(kind, out System.Random r)) { return r; }
            uint hash = 2166136261;
            foreach (char ch in kind) { hash = (hash ^ ch) * 16777619; }
            r = new System.Random(unchecked(_spec.Seed * 7919 + _day * 104729 + (int)hash));
            _rngs[kind] = r;
            return r;
        }

        /// <summary>The saves screen's Save, to this run's own folder (never the player's): `SaveToPath`, the method the screen's row calls. A later
        /// run resumes the file in a fresh session (`resume=`).</summary>
        private string SaveFile(int day)
        {
            string name = _spec.Id + ".day" + day.ToString(CultureInfo.InvariantCulture) + ".save.json";
            string path = Path.Combine(_dir, name);
            bool saved = (bool)Call("SaveToPath", path);
            Event($"SAVE at day {_day} ({_lastDate:yyyy-MM-dd}) to {name} via SaveToPath - played on, nothing loaded{(saved ? string.Empty : " - THE SAVE FAILED")}");
            _trajectory.Mark(_day, _lastDate, "save " + name);
            if (!saved) { End(1, $"SAVE FAILED at day {_day} through SaveToPath({path})"); }
            return path;
        }

        private object _clickedNight;
        private int _nights;
        private object _clickedSigning;
        private DateTime _playWall;

        private bool AtSafePoint() =>
            !(bool)Get("_canvasLive") && Get("_canvasPhase").ToString() == "None" && ((ICollection)Get("_signingQueue")).Count == 0 && Get("_electionNight") == null;

        private void SetSpeed(string name)
        {
            Type t = typeof(GameController).GetNestedType("GameSpeed", Any);
            Set("_gameSpeed", Enum.Parse(t, name));
        }

        // ------------------------------------------------------------------ the holds, answered through their controls

        /// <summary>What holds the clock, in Update's own order, answered as the style answers it. True when something was answered or a takeover
        /// is mid-transition (the clock is held for a reason the harness is serving).</summary>
        private bool AnswerHolds(Country c)
        {
            object night = Get("_electionNight");
            if (night != null)
            {
                // CONTINUE once per night, when the board offers it; the night then covers out over frames. Before the click, the board's own Canvas
                // text is kept (<id>.night<n>.txt): what the player is shown, to be read against the record the count wrote (the elections inventory's
                // lead D-01: Sweden's night allocates its seats apart from the chamber's).
                if (!ReferenceEquals(night, _clickedNight))
                {
                    Button cont = FindButton(null, "ContinueButton");
                    if (cont != null && cont.interactable && night is ElectionNightScreen screen && screen.Root != null)
                    {
                        _nights++;
                        string file = Path.Combine(_dir, _spec.Id + ".night" + _nights.ToString(CultureInfo.InvariantCulture) + ".txt");
                        var texts = new StringBuilder();
                        foreach (Text t in screen.Root.GetComponentsInChildren<Text>(true))
                        {
                            if (string.IsNullOrEmpty(t.text)) { continue; }
                            texts.Append(t.gameObject.name).Append('\t').Append(t.text.Replace('\n', ' ').Replace('\t', ' ')).Append('\n');
                        }

                        File.WriteAllText(file, texts.ToString(), new UTF8Encoding(false));
                        Event($"NIGHT {_nights} on {_lastDate:yyyy-MM-dd}: the board's text kept in {Path.GetFileName(file)}");
                    }

                    if (Click(null, "ContinueButton")) { _clickedNight = night; }
                }

                return true;
            }

            if ((bool)Get("_canvasLive") || Get("_canvasPhase").ToString() != "None" || ((ICollection)Get("_signingQueue")).Count > 0)
            {
                // SIGN once per ceremony; the seal settles and the seam covers out on its own clock.
                object signing = Get("_signingScreen");
                if (signing != null && !ReferenceEquals(signing, _clickedSigning) && Click(null, "SignButton")) { _clickedSigning = signing; }
                return true;
            }

            if (Get("_gameSpeed").ToString() == "Paused") { return _pauseLeft > 0 || _pendingResume; }

            CountryId id = c.Id;
            if ((bool)Call("UpdateFedChairSelectionState"))
            {
                IList candidates = (IList)Get("_fedChairCandidates");
                if (candidates != null && candidates.Count > 0)
                {
                    int pick = PickChair(candidates);
                    object chair = candidates[pick];
                    // The Appoint button's own body (GameController.cs, the chair's candidates): the model written inline, as the control writes it.
                    c.CurrentFedChair = (FedChair)chair;
                    Set("_fedChairCandidates", null);
                    Call("RecomputePolicyPreview");
                    Count("answer chair");
                    Event($"HOLD chair: appointed {c.CurrentFedChair.Name} (candidate {pick + 1} of {candidates.Count}) - the Appoint button's body");
                    return true;
                }
            }

            if ((bool)Call("HasPendingCampaignOpening") && GameSettings.HoldOnCampaignOpening)
            {
                Call("AcknowledgeCampaignOpening");
                Count("answer campaign opening");
                Event($"HOLD campaign opening acknowledged (AcknowledgeCampaignOpening) at day {_day}");
                return true;
            }

            if ((bool)Call("HasPendingScandalAnswer"))
            {
                ScandalResponse response = ScandalAnswer();
                bool ok = _sm.QueueScandalResponse(response, out string refusal);
                Count("answer scandal");
                Event($"HOLD scandal: {response} {(ok ? "queued" : "REFUSED - " + refusal)} (QueueScandalResponse, the chip's call)");
                return true;
            }

            List<(CabinetPortfolio Portfolio, CabinetDecision Decision)> decisions = _sm.GetPendingCabinetDecisions(id);
            if (decisions.Count > 0)
            {
                (CabinetPortfolio portfolio, CabinetDecision decision) = decisions[0];
                int pick = Pick(decision.Options.Count, "cabinet");
                _sm.ResolveCabinetDecision(id, portfolio, decision, decision.Options[pick]);
                Count("answer cabinet");
                Event($"HOLD cabinet {portfolio}: option {(char)('A' + pick)} '{decision.Options[pick].Label}' (ResolveCabinetDecision, CHOOSE's call)");
                return true;
            }

            ForeignPolicyMeeting meeting = _sm.GetPendingForeignPolicyMeeting(id);
            if (meeting != null)
            {
                int pick = MeetingPick(meeting);
                _sm.ResolveForeignPolicyMeeting(id, meeting.Options[pick]);
                Count("answer meeting");
                Event($"HOLD meeting '{meeting.Name}': '{meeting.Options[pick].Label}' (ResolveForeignPolicyMeeting, the option's call)");
                return true;
            }

            if (_sm.GetPendingBudgetProcess(id) && GameSettings.HoldOnBudgetWindow)
            {
                if (TableBudget(c, "HOLD")) { return true; }
            }

            if ((bool)Call("DeclarationHolds"))
            {
                bool extra = _spec.Style == "fuzz" ? Rng("hold").Next(2) == 0 : false;
                string refused;
                bool ok = extra ? _sm.OrderExtraElection(id, out refused) : _sm.AskToBeDischarged(id, out refused);
                Count("answer declaration");
                Event($"HOLD no-confidence declared: {(extra ? "EXTRA ELECTION (OrderExtraElection)" : "ASK TO BE DISCHARGED (AskToBeDischarged)")} {(ok ? "done" : "REFUSED - " + refused)}");
                return true;
            }

            if ((bool)Call("FormationHolds"))
            {
                SpeakerRound round = _sm.RoundOf(id);
                bool? record = _spec.Style == "historical" ? RecordSeats(c) : null;
                if (round.Stage == RoundStage.PlayerAsked)
                {
                    FormationProposal draft = (FormationProposal)Call("FormationDraft");
                    ProposalVerdict preview = _sm.PreviewFormation(id, draft);
                    bool table = preview != null && preview.AllAccept && !(_spec.Style == "fuzz" && Rng("hold").Next(3) == 0) && record != false;
                    if (record.HasValue) { Event($"HOLD formation (historical): the record's government a season on {(record.Value ? "seats" : "does not seat")} {c.PlayerPartyAbbrev}"); }
                    if (table)
                    {
                        // TABLE IT's whole body (GameController.FormationSheet.cs): the sheet closes on success, the refusal is shown on failure.
                        bool ok = _sm.SubmitFormation(id, draft, out _, out string refused);
                        if (ok) { Set("_formationSheetOpen", false); } else { Set("_formationRefusal", refused); }
                        Count("answer formation table");
                        Event($"HOLD formation: TABLE IT {(ok ? "tabled" : "REFUSED - " + refused)} (SubmitFormation with the sheet's draft)");
                    }
                    else
                    {
                        // PASS's whole body, as TABLE IT's.
                        bool ok = _sm.PassFormation(id, out string refused);
                        if (ok) { Set("_formationSheetOpen", false); } else { Set("_formationRefusal", refused); }
                        Count("answer formation pass");
                        Event($"HOLD formation: PASS {(ok ? "passed" : "REFUSED - " + refused)} (PassFormation){(preview != null && !preview.AllAccept ? " - the draft does not carry every invited party" : string.Empty)}");
                    }

                    return true;
                }

                if (round.Stage == RoundStage.OfferToPlayer)
                {
                    bool accept = _spec.Style == "fuzz" ? Rng("hold").Next(2) == 0 : record ?? true;
                    bool ok = _sm.AnswerOffer(id, accept, out string refused);
                    Count("answer offer");
                    Event($"HOLD offer: {(accept ? "ACCEPT" : "DECLINE")} {(ok ? "answered" : "REFUSED - " + refused)} (AnswerOffer)");
                    return true;
                }
            }

            return false;
        }

        /// <summary>The style's pick among a hold's options: idle and historical take the first (the control drawn first), fuzz draws from the run's own generator.</summary>
        private bool _eventsWere = true;

        /// <summary>The meeting card's option, as the plan's `meeting=` chooses it: first (the card's first, the default for every style but fuzz), last
        /// (the card's last - the decline or the arm's length), approval (the option with the most approval, the first of equals); else the style's pick.</summary>
        private int MeetingPick(ForeignPolicyMeeting meeting)
        {
            int n = meeting.Options.Count;
            if (n <= 1 || _spec.Meeting == null) { return Pick(n, "meeting"); }
            if (_spec.Meeting == "last") { return n - 1; }
            if (_spec.Meeting != "approval") { return 0; }
            int best = 0;
            for (int i = 1; i < n; i++) { if (meeting.Options[i].ApprovalEffect > meeting.Options[best].ApprovalEffect) { best = i; } }
            return best;
        }

        private int Pick(int options, string what)
        {
            if (options <= 1) { return 0; }
            return _spec.Style == "fuzz" ? Rng("hold").Next(options) : 0;
        }

        /// <summary>The scandal chip: idle and historical IGNORE (the no-input answer), fuzz draws among the chips the desk draws open (never Resign, and
        /// SacrificeStaffMember only with staff on the roster - the chip's own gate).</summary>
        private ScandalResponse ScandalAnswer()
        {
            if (_spec.Style != "fuzz") { return ScandalResponse.Ignore; }
            ScandalResponse[] open = { ScandalResponse.Deny, ScandalResponse.Apologize, ScandalResponse.Explain, ScandalResponse.AttackSource, ScandalResponse.Ignore };
            return open[Rng("hold").Next(open.Length)];
        }

        /// <summary>The budget window's bill: the style drafts its dials, then "Introduce budget bill" runs its body. False when the player may not
        /// introduce or a bill is already before the chamber.</summary>
        private bool TableBudget(Country c, string why)
        {
            CountryId id = c.Id;
            if (!_sm.PlayerMayIntroduce(id, out string locked))
            {
                if (_lockedWindowLogged != _sm.CurrentDate) { Event($"{why} budget window up for a player who may not introduce ({locked}) - nothing the desk offers answers it"); _lockedWindowLogged = _sm.CurrentDate; }
                return false;
            }

            if (_sm.GetPendingBudgetBill(id) != null) { return false; }
            DraftBudget(c);
            BudgetBill bill = (BudgetBill)Call("BuildBudgetBillFromDrafts");
            bool ok = _sm.IntroduceBudgetBill(id, bill);
            Count("answer budget window");
            Event($"{why} budget window: Introduce budget bill {(ok ? "tabled" : "REFUSED")} (IntroduceBudgetBill(BuildBudgetBillFromDrafts()), the button's body; {bill.TaxLines.Count} tax line(s), {bill.SpendingNominalTargets.Count} spending target(s), {bill.WelfarePrograms.Count} programme(s){(bill.PensionAgeSet ? ", pension age " + bill.PensionAge.ToString("R", CultureInfo.InvariantCulture) : string.Empty)})");
            return true;
        }

        private DateTime _lockedWindowLogged;

        // ------------------------------------------------------------------ the end

        private void End(int code, string why)
        {
            if (_ended) { return; }
            _ended = true;
            _phase = Phase.Done;
            Application.logMessageReceived -= OnLog;
            string digest = _trajectory != null ? _trajectory.Finish() : string.Empty;
            double seconds = (DateTime.UtcNow - _wallStart).TotalSeconds;
            double play = _playWall == default ? 0.0 : (DateTime.UtcNow - _playWall).TotalSeconds;
            if (_errors > 0) { code = Math.Max(code, 1); }
            if (_invariants.Breaches > 0) { code = Math.Max(code, 1); }
            string summary = string.Format(CultureInfo.InvariantCulture,
                "{0}; day {1} ({2:yyyy-MM-dd}), {3} frame(s), {4:F1} s in all, {14:F1} s of play ({5:F1} ms a day); {6}; logged errors {7}{8}; answers {9}; OnGUI {10} pass(es) {11:F1} ms mean; trajectory {12} row(s), digest {13}",
                why, _day, _lastDate, _frame, seconds, _day > 0 ? play * 1000.0 / _day : 0.0, _invariants.Summary(), _errors,
                _firstErrors.Count > 0 ? " (first: " + string.Join(" | ", _firstErrors) + ")" : string.Empty,
                _answers.Count == 0 ? "none" : string.Join(", ", _answers.OrderBy(p => p.Key, StringComparer.Ordinal).Select(p => p.Key + " x" + p.Value)),
                _guiPasses, _guiPasses > 0 ? _guiMs / _guiPasses : 0.0, _trajectory != null ? _trajectory.Rows : 0, string.IsNullOrEmpty(digest) ? "none" : digest, play);
            Event("END " + summary);
            _events.Flush();
            _events.Dispose();
            _trajectory?.Dispose();
            if (_captureCount > 0)
            {
                string table = DryGuiPass.WriteTable(_dir, _spec.Id);
                Debug.Log($"AUDIT: RUN {_spec.Id} - {_captureCount} page capture(s) written to {table ?? "(no rows recorded)"}");
            }
            int restored = PreferencePins.Restore();
            AuditPrefs.Done(_dir);
            EventSystem.Enabled = _eventsWere;
            Debug.Log($"AUDIT: RUN {_spec.Id} - {summary} (preferences restored: {restored})");
            _onEnd(code, summary, digest);
        }

        private void OnLog(string message, string stackTrace, LogType type)
        {
            // The game's own save and load lines are kept as events (an autosave's day is the evidence of its cadence); nothing else at Log level is.
            if (type == LogType.Log && message != null && (message.StartsWith("SAVE: ", StringComparison.Ordinal) || message.StartsWith("LOAD: ", StringComparison.Ordinal)))
            {
                Event($"GAME at day {_day} ({_lastDate:yyyy-MM-dd}), frame {_frame}: {message.Split('\n')[0]}");
                return;
            }

            if (type != LogType.Error && type != LogType.Exception && type != LogType.Assert) { return; }
            if (message != null && message.StartsWith("AUDIT:", StringComparison.Ordinal)) { return; }
            _errors++;
            string first = (message ?? string.Empty).Split('\n')[0];
            if (_firstErrors.Count < 5 && !_firstErrors.Contains(first)) { _firstErrors.Add(first.Length > 200 ? first.Substring(0, 200) : first); }
            Event($"LOGGED {type} at day {_day} ({_lastDate:yyyy-MM-dd}), frame {_frame}: {first}");
        }

        // ------------------------------------------------------------------ helpers

        private readonly HashSet<string> _capturesDone = new HashSet<string>(StringComparer.Ordinal);
        private int _captureCount;

        /// <summary>A page's drawn text on a day: the rail cell's own body opens it (`_onDesk`, `_consolidatedTab`, the live HQ closed), one OnGUI
        /// frame runs under the dry film's label table, and the rail's body returns to the desk. The page `HQ` is the live campaign HQ, opened by
        /// the rail cell's own `OpenLiveCampaign` (the run-up's screen before the campaign opens) and left as it was found. The table goes to
        /// `&lt;id&gt;_labels.tsv` at the end of the run. True while a capture takes this frame.</summary>
        private bool CapturePages()
        {
            foreach ((int day, string page) in _spec.Captures)
            {
                string key = day.ToString(CultureInfo.InvariantCulture) + ":" + page;
                if (day > _day || _capturesDone.Contains(key) || !AtSafePoint()) { continue; }
                _capturesDone.Add(key);
                string name = _spec.Id + "_day" + _day.ToString(CultureInfo.InvariantCulture) + "_" + page;
                DryGuiPass.InstallHook();
                Call("LateUpdate");   // §820: the frame settled before the page is opened or read (an HQ whose run dropped closes here, as in play)
                WatchHq();            // ...and a close it made is the game's, logged before the harness's own opens and closes below
                if (_ended) { return true; }
                bool hq = page == "HQ", hqWasOpen = (bool)Get("_liveCampaignOpen");
                if (hq)
                {
                    if (!hqWasOpen) { Call("OpenLiveCampaign"); }
                    if (!(bool)Get("_liveCampaignOpen")) { Event($"CAPTURE {name}: no campaign and no run-up to open on {_lastDate:yyyy-MM-dd} - nothing drawn"); continue; }
                }
                else
                {
                    Call("CloseLiveCampaign");
                    if (page == "Desk") { Set("_onDesk", true); }
                    else
                    {
                        Type tabs = typeof(GameController).GetNestedType("ConsolidatedTab", Any);
                        Set("_onDesk", false);
                        Set("_consolidatedTab", Enum.Parse(tabs, page));
                    }
                }

                PumpGui();   // a Layout and Repaint to settle the page
                DryGuiPass.BeginCapture(name);
                PumpGui();
                DryGuiPass.EndCapture();
                _captureCount++;
                if (hq && !hqWasOpen) { Call("CloseLiveCampaign"); }
                if (!hq || !hqWasOpen) { Set("_onDesk", true); }
                _hqOpenLastTick = (bool)Get("_liveCampaignOpen");   // §820: the harness's own close is not the game's
                Event($"CAPTURE {name}: the {page} page drawn on {_lastDate:yyyy-MM-dd} ({(hq ? (hqWasOpen ? "the HQ was open; left open" : "the rail's OpenLiveCampaign opened it; closed to the desk") : "the rail's body opened it; the desk restored")})");
                return true;
            }

            return false;
        }

        private void PumpGui()
        {
            var clock = System.Diagnostics.Stopwatch.StartNew();
            // §820 (GA-1-1's review): the game's frame runs LateUpdate before OnGUI, and the live HQ is settled there now - the harness's tick
            // runs in the Update phase, before Unity's LateUpdate, so it settles the frame through the controller's own LateUpdate first (it is
            // idempotent): a pump draws the frame the player would see, never the snapshot of the day before
            Call("LateUpdate");
            if (!DryGuiPass.Pump(_gc, _onGui)) { End(1, $"the IMGUI pass failed ({DryGuiPass.Failure})"); return; }
            _guiMs += clock.Elapsed.TotalMilliseconds;
            _guiPasses++;
        }

        private World World() => (World)Get("_world");
        private Country PlayerCountry() => (Country)Get("_playerCountry");

        private string HoldState(Country c) =>
            $"night {(Get("_electionNight") != null ? 1 : 0)}, canvasLive {Get("_canvasLive")}, phase {Get("_canvasPhase")}, signing {((ICollection)Get("_signingQueue")).Count}, speed {Get("_gameSpeed")}, "
            + $"budget process {_sm.GetPendingBudgetProcess(c.Id)} (bill {(_sm.GetPendingBudgetBill(c.Id) != null ? 1 : 0)}, player may introduce {_sm.PlayerMayIntroduce(c.Id, out _)}), round {_sm.RoundOf(c.Id)?.Stage.ToString() ?? "none"}, "
            + $"cabinet decisions {_sm.GetPendingCabinetDecisions(c.Id).Count}, meeting {(_sm.GetPendingForeignPolicyMeeting(c.Id) != null ? 1 : 0)}, declaration {Call("DeclarationHolds")}, campaign opening {Call("HasPendingCampaignOpening")}, scandal {Call("HasPendingScandalAnswer")}";

        private void Count(string what) => _answers[what] = (_answers.TryGetValue(what, out int n) ? n : 0) + 1;

        private void Event(string line)
        {
            _events.Write(line);
            _events.Write('\n');
            if (line.StartsWith("INVARIANT", StringComparison.Ordinal) || line.StartsWith("HARNESS", StringComparison.Ordinal) || line.StartsWith("SOFTLOCK", StringComparison.Ordinal))
            {
                Debug.Log("AUDIT: " + _spec.Id + " " + line);
            }
        }

        /// <summary>An active Button named <paramref name="name"/> under a parent named <paramref name="root"/> (any parent when null). Where several
        /// match, the one first by its hierarchy path - an order that depends on the screen, never on object ids.</summary>
        private static Button FindButton(string root, string name)
        {
            Button best = null;
            string bestPath = null;
            foreach (Button b in Object.FindObjectsByType<Button>())
            {
                if (b == null || !b.isActiveAndEnabled || b.name != name) { continue; }
                bool under = root == null;
                var path = new StringBuilder(b.name);
                for (Transform t = b.transform.parent; t != null; t = t.parent)
                {
                    if (t.name == root) { under = true; }
                    path.Insert(0, t.name + "/");
                }

                if (!under) { continue; }
                string p = path.ToString();
                if (bestPath == null || string.CompareOrdinal(p, bestPath) < 0) { best = b; bestPath = p; }
            }

            return best;
        }

        /// <summary>A Canvas control clicked: its own onClick, which is what a pointer's click invokes.</summary>
        private bool Click(string root, string name)
        {
            Button b = FindButton(root, name);
            if (b == null || !b.interactable) { return false; }
            b.onClick.Invoke();
            Count("click " + (root == null ? string.Empty : root + "/") + name);
            Event($"clicked {(root == null ? string.Empty : root + "/")}{name} at frame {_frame}{(_phase == Phase.Playing ? $", day {_day}" : string.Empty)}");
            return true;
        }

        // The controller's private members, each looked up once: a frame reads a dozen of them, and a fresh reflection lookup per read cost more
        // than the day it watched (measured in the calibration plan, cal1).
        private static readonly Dictionary<string, FieldInfo> Fields = new Dictionary<string, FieldInfo>(StringComparer.Ordinal);
        private static readonly Dictionary<string, MethodInfo> Methods = new Dictionary<string, MethodInfo>(StringComparer.Ordinal);

        private static FieldInfo FieldOf(string field)
        {
            if (Fields.TryGetValue(field, out FieldInfo f)) { return f; }
            f = typeof(GameController).GetField(field, Any);
            if (f == null) { throw new MissingFieldException(nameof(GameController), field); }
            Fields[field] = f;
            return f;
        }

        private object Get(string field) => FieldOf(field).GetValue(_gc);

        private void Set(string field, object value) => FieldOf(field).SetValue(_gc, value);

        private object Call(string method, params object[] args)
        {
            string key = method + "/" + args.Length.ToString(CultureInfo.InvariantCulture);
            if (!Methods.TryGetValue(key, out MethodInfo m))
            {
                m = typeof(GameController).GetMethods(Any).FirstOrDefault(x => x.Name == method && x.GetParameters().Length == args.Length);
                if (m == null) { throw new MissingMethodException(nameof(GameController), method); }
                Methods[key] = m;
            }

            try { return m.Invoke(_gc, args); }
            catch (TargetInvocationException e) { throw e.InnerException ?? e; }
        }
    }
}
