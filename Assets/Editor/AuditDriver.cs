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
    /// <item>`scenario` - a `ScenarioLibrary` id: the selector's scenario strip is clicked instead of the country's folder.</item>
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
        internal string Scenario;
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

        private static readonly string[] Styles = { "idle", "historical", "fuzz", "exploit", "extreme" };

        internal string Describe() =>
            $"{Id}: {Country}{(string.IsNullOrEmpty(Scenario) ? string.Empty : " scenario " + Scenario)} as {(string.IsNullOrEmpty(Create) ? Party : "a created party (" + Create + ")")}, style {Style}, seed {Seed}, {Days} day(s) at {Speed}x, delta {Delta.ToString("R", CultureInfo.InvariantCulture)} s"
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
                        case "scenario": s.Scenario = value; break;
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
                        default: error = $"unknown key '{key}'"; return null;
                    }
                }
                catch (FormatException) { error = $"'{token}' does not parse"; return null; }
            }

            if (string.IsNullOrEmpty(s.Id) || s.Id.Any(ch => !(char.IsLetterOrDigit(ch) || ch == '_' || ch == '-' || ch == '.'))) { error = "id= is missing or holds a character outside letters, digits, _ . -"; return null; }
            if (string.IsNullOrEmpty(s.Country) || !Enum.TryParse(s.Country, false, out CountryId _)) { error = $"country= '{s.Country}' is not a CountryId"; return null; }
            if (string.IsNullOrEmpty(s.Party) && string.IsNullOrEmpty(s.Create)) { error = "party= (or create=) is required"; return null; }
            if (!Styles.Contains(s.Style)) { error = $"style= '{s.Style}' is not one of {string.Join(", ", Styles)}"; return null; }
            if (s.Speed < 1 || s.Speed > 3) { error = "speed= is 1, 2 or 3"; return null; }
            if (s.Delta <= 0f || s.Delta > 0.333f) { error = "delta= is in (0, 0.333] - Unity caps a frame's delta at the project's maximum timestep"; return null; }
            if (s.Days < 0) { error = "days= is not negative"; return null; }
            return s;
        }
    }

    /// <summary>
    /// The audit's driver for one run: it starts a game the way a player does (the main menu's NEW GAME, the country's folder, the start card
    /// and SELECT, the party's row - Canvas buttons clicked through their own `onClick`), lets the controller's `Update` advance the days, answers
    /// every hold through the method its control calls, acts for the play style, interrupts as the plan asks, checks the invariants every day,
    /// and writes the trajectory. See `AuditHarness` for the route and its reasons.
    /// </summary>
    internal sealed class AuditDriver
    {
        private const BindingFlags Any = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

        private enum Phase { Boot, Menu, Selector, StartSheet, PartyPanel, CreateFlow, Opening, Playing, Done }

        private readonly AuditSpec _spec;
        private readonly string _dir;
        private readonly Action<int, string, string> _onEnd;
        private readonly System.Random _choices;
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
            _choices = new System.Random(spec.Seed * 31 + 7);
            _events = new StreamWriter(Path.Combine(dir, spec.Id + ".events.log"), false, new UTF8Encoding(false)) { NewLine = "\n" };
            _invariants = new AuditInvariants(Event);
            Application.logMessageReceived += OnLog;
            Event("RUN " + spec.Describe());
        }

        // ------------------------------------------------------------------ the frame

        internal void Tick()
        {
            if (_ended) { return; }
            _frame++;
            _phaseFrames++;
            try
            {
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
            UiScreen.OverrideWidth = 1280;
            UiScreen.OverrideHeight = 699;

            // The seed first: the game's world is built when the party row is clicked, from this seed.
            SimulationRandom.Seed(_spec.Seed);
            AuditPrefs.Save(_dir);
            PreferencePins.Pin();
            GameSettings.DefaultSpeed = _spec.Speed - 1;
            GameSettings.HoldOnCampaignOpening = _spec.HoldCampaign;
            GameSettings.HoldOnBudgetWindow = _spec.HoldBudget;
            GameSettings.AutosaveDays = 0;
            Time.captureDeltaTime = _spec.Delta;
            Event($"boot: seed {_spec.Seed}; preferences pinned (speed {GameSettings.DefaultSpeed + 1}x, holds campaign {GameSettings.HoldOnCampaignOpening} budget {GameSettings.HoldOnBudgetWindow}, autosave off); captureDeltaTime {Time.captureDeltaTime.ToString("R", CultureInfo.InvariantCulture)} s; OnGUI at {UiScreen.Width}x{UiScreen.Height}");
            Go(Phase.Menu);
        }

        private void Menu()
        {
            if (Click("MainMenu", "NewGame")) { Go(Phase.Selector); return; }
            // The menu is the first takeover; where it failed to build the controller goes to the selector itself.
            if (FindButton("CountrySelector", FolderName()) != null || FindButton("CountrySelector", ScenarioName()) != null) { Event("the main menu never came up - the selector is live"); Go(Phase.Selector); return; }
            if (PhaseStuck(600)) { End(1, "SOFTLOCK at the main menu - NEW GAME never became clickable"); }
        }

        private string FolderName() => "Folder_" + _spec.Country;
        private string ScenarioName() => "Scenario_" + _spec.Scenario;

        private void Selector()
        {
            if (!string.IsNullOrEmpty(_spec.Scenario))
            {
                if (Click("CountrySelector", ScenarioName())) { Go(Phase.PartyPanel); return; }
            }
            else if (Click("CountrySelector", FolderName())) { Go(Phase.StartSheet); return; }

            if (_phaseFrames % 120 == 1) { Event("waiting for the selector: " + SeamState()); }
            if (PhaseStuck(600)) { End(1, $"SOFTLOCK at the selector - {(string.IsNullOrEmpty(_spec.Scenario) ? FolderName() : ScenarioName())} never became clickable"); }
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
            else if (Click("PartyPanel", "Party_" + _spec.Party)) { Go(Phase.Opening); return; }

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
            Go(Phase.Opening);
        }

        private void Opening()
        {
            CountryId? selected = (CountryId?)Get("_selectedPlayerCountryId");
            if (selected.HasValue && !(bool)Get("_canvasLive") && Get("_canvasPhase").ToString() == "None")
            {
                Country c = PlayerCountry();
                _lastDate = _sm.CurrentDate;
                _lastTurn = _sm.CurrentTurn;
                _lastElectionCount = c.ElectionHistory.Count;
                _lastPresidentialCount = c.PresidentialElections.Count;
                _playWall = DateTime.UtcNow;
                _trajectory = new AuditTrajectory(_dir, _spec.Id);
                _trajectory.Day(0, _lastDate, _lastTurn, c, false);
                _trajectory.Turn(0, _lastDate, _lastTurn, World());
                Event($"the game opened on {_lastDate:yyyy-MM-dd}: {c.Id}, the player's {c.PlayerPartyAbbrev} is {(c.Government?.RoleOf(c.PlayerPartyAbbrev) ?? PlayerRole.None)} (government {c.Government?.PmParty} [{(c.Government == null ? string.Empty : string.Join("+", c.Government.Cabinet))}] '{c.Government?.Outcome}'), speed {Get("_gameSpeed")}");
                CheckDay(c);
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
            bool answered = AnswerHolds(c);
            Act(c);
            if (_framesSinceDay > _spec.Stall && !answered && _pauseLeft == 0)
            {
                End(1, $"SOFTLOCK - no day passed in {_framesSinceDay} frames at day {_day} ({now:yyyy-MM-dd}) and nothing the harness answers holds the clock: {HoldState(c)}");
            }
        }

        private void CheckDay(Country c)
        {
            _invariants.NoteDay(_sm);
            _invariants.Finite(World(), _day, _lastDate);
            _invariants.Bounds(World(), _day, _lastDate);
            _invariants.Debt(World(), _day, _lastDate);
            _invariants.Seats(c, _day, _lastDate);
            _invariants.Government(c, _sm, _day, _lastDate);
            while (_lastElectionCount < c.ElectionHistory.Count)
            {
                ElectionRecord r = c.ElectionHistory[_lastElectionCount++];
                Event($"ELECTION {r.Date:yyyy-MM-dd} ({r.Method}): seats {string.Join(" ", r.Seats.OrderByDescending(p => p.Value).Select(p => p.Key + " " + p.Value))}");
                _invariants.Election(c, r, _day, _lastDate);
            }

            if (_lastPresidentialCount != c.PresidentialElections.Count)
            {
                Event($"PRESIDENTIAL contests now {c.PresidentialElections.Count}");
                _lastPresidentialCount = c.PresidentialElections.Count;
            }
        }

        /// <summary>The protocol's interruptions: save then load at a day (the saves screen's own write, the menu's own load route), and the speed
        /// chips. Returns true when this frame was spent interrupting.</summary>
        private bool Interrupt(Country c)
        {
            if (_pauseLeft > 0)
            {
                if (--_pauseLeft == 0) { SetSpeed(_resumeSpeed); Event($"PAUSE ends at day {_day}: speed {_resumeSpeed}"); _trajectory.Mark(_day, _lastDate, "resume " + _resumeSpeed); }
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

            if (_pendingResume && Get("_pendingLoadPath") == null)
            {
                _pendingResume = false;
                SetSpeed(_resumeSpeed);
                _sm = _gc.GetComponent<SimulationManager>();
                _invariants.RestartDebt();
                Event($"LOADED - the game resumed at {_sm.CurrentDate:yyyy-MM-dd}; speed {_resumeSpeed} restored (the chips' write)");
                return true;
            }

            return false;
        }

        private bool _pendingResume;
        private object _clickedNight;
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
                // CONTINUE once per night, when the board offers it; the night then covers out over frames.
                if (!ReferenceEquals(night, _clickedNight) && Click(null, "ContinueButton")) { _clickedNight = night; }
                return true;
            }

            if ((bool)Get("_scenarioVerdictPending")) { Call("DismissScenarioVerdict"); Count("call DismissScenarioVerdict"); Event($"scenario verdict dismissed at day {_day}"); return true; }

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
                    int pick = Pick(candidates.Count, "chair");
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
                int pick = Pick(meeting.Options.Count, "meeting");
                _sm.ResolveForeignPolicyMeeting(id, meeting.Options[pick]);
                Count("answer meeting");
                Event($"HOLD meeting '{meeting.Name}': '{meeting.Options[pick].Label}' (ResolveForeignPolicyMeeting, the option's call)");
                return true;
            }

            if (_sm.GetPendingBudgetProcess(id) && GameSettings.HoldOnBudgetWindow)
            {
                if (_sm.PlayerMayIntroduce(id, out string why))
                {
                    if (_sm.GetPendingBudgetBill(id) == null)
                    {
                        BudgetBill bill = (BudgetBill)Call("BuildBudgetBillFromDrafts");
                        bool ok = _sm.IntroduceBudgetBill(id, bill);
                        Count("answer budget window");
                        Event($"HOLD budget window: Introduce budget bill {(ok ? "tabled" : "REFUSED")} (IntroduceBudgetBill(BuildBudgetBillFromDrafts()), the button's body; drafts as the style left them)");
                        return true;
                    }
                }
                else
                {
                    Event($"HOLD budget window up for a player who may not introduce ({why}) - nothing the desk offers answers it");
                }
            }

            if ((bool)Call("DeclarationHolds"))
            {
                bool extra = _spec.Style == "fuzz" ? _choices.Next(2) == 0 : false;
                string refused;
                bool ok = extra ? _sm.OrderExtraElection(id, out refused) : _sm.AskToBeDischarged(id, out refused);
                Count("answer declaration");
                Event($"HOLD no-confidence declared: {(extra ? "EXTRA ELECTION (OrderExtraElection)" : "ASK TO BE DISCHARGED (AskToBeDischarged)")} {(ok ? "done" : "REFUSED - " + refused)}");
                return true;
            }

            if ((bool)Call("FormationHolds"))
            {
                SpeakerRound round = _sm.RoundOf(id);
                if (round.Stage == RoundStage.PlayerAsked)
                {
                    FormationProposal draft = (FormationProposal)Call("FormationDraft");
                    ProposalVerdict preview = _sm.PreviewFormation(id, draft);
                    if (preview != null && preview.AllAccept && !(_spec.Style == "fuzz" && _choices.Next(3) == 0))
                    {
                        bool ok = _sm.SubmitFormation(id, draft, out _, out string refused);
                        Count("answer formation table");
                        Event($"HOLD formation: TABLE IT {(ok ? "tabled" : "REFUSED - " + refused)} (SubmitFormation with the sheet's draft)");
                    }
                    else
                    {
                        bool ok = _sm.PassFormation(id, out string refused);
                        Count("answer formation pass");
                        Event($"HOLD formation: PASS {(ok ? "passed" : "REFUSED - " + refused)} (PassFormation){(preview != null && !preview.AllAccept ? " - the draft does not carry every invited party" : string.Empty)}");
                    }

                    return true;
                }

                if (round.Stage == RoundStage.OfferToPlayer)
                {
                    bool accept = _spec.Style == "fuzz" ? _choices.Next(2) == 0 : true;
                    bool ok = _sm.AnswerOffer(id, accept, out string refused);
                    Count("answer offer");
                    Event($"HOLD offer: {(accept ? "ACCEPT" : "DECLINE")} {(ok ? "answered" : "REFUSED - " + refused)} (AnswerOffer)");
                    return true;
                }
            }

            return false;
        }

        /// <summary>The style's pick among a hold's options: idle and historical take the first (the control drawn first), fuzz draws from the run's own generator.</summary>
        private int Pick(int options, string what)
        {
            if (options <= 1) { return 0; }
            return _spec.Style == "fuzz" ? _choices.Next(options) : 0;
        }

        /// <summary>The scandal chip: idle and historical IGNORE (the no-input answer), fuzz draws among the chips the desk draws open (never Resign, and
        /// SacrificeStaffMember only with staff on the roster - the chip's own gate).</summary>
        private ScandalResponse ScandalAnswer()
        {
            if (_spec.Style != "fuzz") { return ScandalResponse.Ignore; }
            ScandalResponse[] open = { ScandalResponse.Deny, ScandalResponse.Apologize, ScandalResponse.Explain, ScandalResponse.AttackSource, ScandalResponse.Ignore };
            return open[_choices.Next(open.Length)];
        }

        // ------------------------------------------------------------------ the play styles

        /// <summary>The style's own actions, on the days it chooses. Idle acts on nothing; the other styles' levers are added area by area as the
        /// inventory maps them (the protocol's Phase 1 builds the harness, Phase 2 its matrix).</summary>
        private void Act(Country c)
        {
        }

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
            int restored = PreferencePins.Restore();
            AuditPrefs.Done(_dir);
            Debug.Log($"AUDIT: RUN {_spec.Id} - {summary} (preferences restored: {restored})");
            _onEnd(code, summary, digest);
        }

        private void OnLog(string message, string stackTrace, LogType type)
        {
            if (type != LogType.Error && type != LogType.Exception && type != LogType.Assert) { return; }
            if (message != null && message.StartsWith("AUDIT:", StringComparison.Ordinal)) { return; }
            _errors++;
            string first = (message ?? string.Empty).Split('\n')[0];
            if (_firstErrors.Count < 5 && !_firstErrors.Contains(first)) { _firstErrors.Add(first.Length > 200 ? first.Substring(0, 200) : first); }
            Event($"LOGGED {type} at day {_day} ({_lastDate:yyyy-MM-dd}), frame {_frame}: {first}");
        }

        // ------------------------------------------------------------------ helpers

        private void PumpGui()
        {
            var clock = System.Diagnostics.Stopwatch.StartNew();
            if (!DryGuiPass.Pump(_gc, _onGui)) { End(1, $"the IMGUI pass failed ({DryGuiPass.Failure})"); return; }
            _guiMs += clock.Elapsed.TotalMilliseconds;
            _guiPasses++;
        }

        private World World() => (World)Get("_world");
        private Country PlayerCountry() => (Country)Get("_playerCountry");

        private string HoldState(Country c) =>
            $"night {(Get("_electionNight") != null ? 1 : 0)}, canvasLive {Get("_canvasLive")}, phase {Get("_canvasPhase")}, signing {((ICollection)Get("_signingQueue")).Count}, speed {Get("_gameSpeed")}, verdict {Get("_scenarioVerdictPending")}, "
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
