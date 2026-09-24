using System;
using UnityEngine;

namespace PoliSim.UI
{
    /// <summary>What a preference holds, so the harness can snapshot and restore it without guessing.</summary>
    public enum PreferenceKind { Int, Float }

    /// <summary>
    /// MM-2 (2026-09-24, `docs/specs/START_POINTS_AND_PARTY_CREATION_SPEC.md` §9.2): THE ENUMERATION OF EVERY PREFERENCE THE
    /// GAME READS. Preferences live in PlayerPrefs under `polisim.` keys, outside the save (a save never carries them). Two rules
    /// ride on this list: <b>every setting must change something the game measurably does</b> (`SettingsCheck` proves each key
    /// live through its reader), and <b>every preference is pinned by the film harness and restored when it finishes</b>
    /// (`PreferencePins` reads this list; `SettingsCheck` fails on a `polisim.` literal in the sources that is not on it, and on an
    /// entry here that no source reads). Preferences leaking into films has cost twice - the maximized Game view filming
    /// 962-pixel frames, the PROVENANCE preference flipping later films into false overflows - which is R-N5's bar for a check.
    /// </summary>
    public static class PreferenceKeys
    {
        public static readonly (string Key, PreferenceKind Kind)[] All =
        {
            (AudioDirector.VolumePref, PreferenceKind.Float),
            (AudioDirector.MutePref, PreferenceKind.Int),
            (DeskProvenance.ProvenancePref, PreferenceKind.Int),
            (DisplaySettings.ModePref, PreferenceKind.Int),
            (DisplaySettings.WidthPref, PreferenceKind.Int),
            (DisplaySettings.HeightPref, PreferenceKind.Int),
            (GameSettings.DefaultSpeedPref, PreferenceKind.Int),
            (GameSettings.HoldCampaignPref, PreferenceKind.Int),
            (GameSettings.HoldBudgetPref, PreferenceKind.Int),
            (GameSettings.AutosaveDaysPref, PreferenceKind.Int),
            (GameSettings.AutosaveSlotsPref, PreferenceKind.Int),
        };

        /// <summary>Every cached reader forgets what it read, so the next touch reads PlayerPrefs again - the harness's pin and a check's probe both need it.</summary>
        public static void ReloadAll()
        {
            AudioDirector.Reload();
            DeskProvenance.Reload();
            DisplaySettings.Reload();
            GameSettings.Reload();
        }
    }

    /// <summary>The window's mode, in the order the settings screen offers it. Persisted as its int.</summary>
    public enum WindowMode { Windowed = 0, Borderless = 1, Fullscreen = 2 }

    /// <summary>
    /// MM-2 §9.2 DISPLAY: window mode and resolution, <b>offered only at the geometries the harness films</b> - the four standards
    /// every layout guard in this project was measured at - and no free UI-scale slider, because every guard assumes the filmed
    /// type sizes. Unset (the default) leaves the window as the player launched it; a set value is applied at the game's start
    /// and the moment it is chosen, through <see cref="Apply"/>, whose request is recorded so a check can read what was asked of
    /// the screen (in the Editor `Screen.SetResolution` moves nothing; in a player it moves the window).
    /// </summary>
    public static class DisplaySettings
    {
        public const string ModePref = "polisim.display.mode";
        public const string WidthPref = "polisim.display.width";
        public const string HeightPref = "polisim.display.height";

        /// <summary>S-17's four geometries, the ones the film harness accepts; `UiScreenshotCapture.StandardGeometries` reads this table.</summary>
        public static readonly (int Width, int Height)[] Geometries =
        {
            (1280, 720), (1600, 950), (1920, 1080), (2560, 1440),
        };

        /// <summary>The last request made of the screen: (mode, width, height), or null when nothing has been applied - what `SettingsCheck` reads.</summary>
        public static (WindowMode Mode, int Width, int Height)? LastRequest { get; private set; }

        private static bool _read;
        private static int _mode = -1;
        private static int _width;
        private static int _height;

        /// <summary>True when the player has chosen a geometry; false leaves the launch window alone.</summary>
        public static bool IsSet { get { Read(); return _mode >= 0 && _width > 0 && _height > 0; } }

        public static WindowMode Mode { get { Read(); return _mode >= 0 ? (WindowMode)_mode : WindowMode.Windowed; } }
        public static int Width { get { Read(); return _width; } }
        public static int Height { get { Read(); return _height; } }

        /// <summary>Choose and apply. A geometry off the table is refused (the guard's own reason: an off-standard frame is a different test).</summary>
        public static bool Set(WindowMode mode, int width, int height)
        {
            bool known = false;
            foreach ((int Width, int Height) g in Geometries) { if (g.Width == width && g.Height == height) { known = true; break; } }
            if (!known) { return false; }
            Read();
            _mode = (int)mode; _width = width; _height = height;
            PlayerPrefs.SetInt(ModePref, _mode);
            PlayerPrefs.SetInt(WidthPref, _width);
            PlayerPrefs.SetInt(HeightPref, _height);
            Apply();
            return true;
        }

        /// <summary>Asks the screen for the chosen geometry. Nothing happens while unset.</summary>
        public static void Apply()
        {
            if (!IsSet) { return; }
            FullScreenMode screenMode = Mode == WindowMode.Fullscreen ? FullScreenMode.ExclusiveFullScreen
                : Mode == WindowMode.Borderless ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;
            LastRequest = (Mode, _width, _height);
            Screen.SetResolution(_width, _height, screenMode);
        }

        public static void Reload() { _read = false; LastRequest = null; }

        private static void Read()
        {
            if (_read) { return; }
            _read = true;
            _mode = PlayerPrefs.GetInt(ModePref, -1);
            _width = PlayerPrefs.GetInt(WidthPref, 0);
            _height = PlayerPrefs.GetInt(HeightPref, 0);
        }
    }

    /// <summary>
    /// MM-2 §9.2 GAME: the default speed a new game starts at; which of the two interrupts that CAN release the clock hold it (the
    /// campaign's opening and the budget window - election night is a takeover and always holds, so it is not offered); the
    /// provenance (†) default is `DeskProvenance.On` itself, one value for the desk; autosave every N days into rotating slots.
    /// Read on first touch (the AudioDirector idiom), so a harness that never asks never touches PlayerPrefs.
    /// </summary>
    public static class GameSettings
    {
        public const string DefaultSpeedPref = "polisim.game.speed";
        public const string HoldCampaignPref = "polisim.game.hold.campaign";
        public const string HoldBudgetPref = "polisim.game.hold.budget";
        public const string AutosaveDaysPref = "polisim.game.autosave.days";
        public const string AutosaveSlotsPref = "polisim.game.autosave.slots";

        /// <summary>The speeds the desk offers, as the settings screen names them: 0 = 1×, 1 = 2×, 2 = 3× (the desk's own chips).</summary>
        public static readonly string[] SpeedLabels = { "1×", "2×", "3×" };

        /// <summary>The autosave cadences offered, in days; 0 is off.</summary>
        public static readonly int[] AutosaveDayChoices = { 0, 7, 30, 90 };

        public const int MinSlots = 1;
        public const int MaxSlots = 5;

        private static bool _read;
        private static int _speed;
        private static bool _holdCampaign;
        private static bool _holdBudget;
        private static int _autosaveDays;
        private static int _autosaveSlots;

        /// <summary>0, 1 or 2 - the index into <see cref="SpeedLabels"/> and the desk's running speeds.</summary>
        public static int DefaultSpeed
        {
            get { Read(); return _speed; }
            set { Read(); _speed = Mathf.Clamp(value, 0, SpeedLabels.Length - 1); PlayerPrefs.SetInt(DefaultSpeedPref, _speed); }
        }

        public static bool HoldOnCampaignOpening
        {
            get { Read(); return _holdCampaign; }
            set { Read(); _holdCampaign = value; PlayerPrefs.SetInt(HoldCampaignPref, value ? 1 : 0); }
        }

        public static bool HoldOnBudgetWindow
        {
            get { Read(); return _holdBudget; }
            set { Read(); _holdBudget = value; PlayerPrefs.SetInt(HoldBudgetPref, value ? 1 : 0); }
        }

        /// <summary>Days between autosaves; 0 = off.</summary>
        public static int AutosaveDays
        {
            get { Read(); return _autosaveDays; }
            set { Read(); _autosaveDays = Mathf.Max(0, value); PlayerPrefs.SetInt(AutosaveDaysPref, _autosaveDays); }
        }

        public static int AutosaveSlots
        {
            get { Read(); return _autosaveSlots; }
            set { Read(); _autosaveSlots = Mathf.Clamp(value, MinSlots, MaxSlots); PlayerPrefs.SetInt(AutosaveSlotsPref, _autosaveSlots); }
        }

        /// <summary>The rule the day loop asks: an autosave is due when the cadence is on and that many days have passed since the last.</summary>
        public static bool AutosaveDue(int daysSinceLast, int autosaveDays) => autosaveDays > 0 && daysSinceLast >= autosaveDays;

        /// <summary>The slot an autosave lands in, rotating 1..slots: the n-th autosave of a game goes to slot ((n-1) mod slots) + 1.</summary>
        public static int AutosaveSlot(int autosaveCount, int slots) => slots <= 0 ? 1 : ((Math.Max(0, autosaveCount)) % slots) + 1;

        public static string AutosaveName(int slot) => $"autosave_{slot}";

        public static void Reload() { _read = false; }

        private static void Read()
        {
            if (_read) { return; }
            _read = true;
            _speed = Mathf.Clamp(PlayerPrefs.GetInt(DefaultSpeedPref, 0), 0, SpeedLabels.Length - 1);
            _holdCampaign = PlayerPrefs.GetInt(HoldCampaignPref, 1) == 1;
            _holdBudget = PlayerPrefs.GetInt(HoldBudgetPref, 1) == 1;
            _autosaveDays = Mathf.Max(0, PlayerPrefs.GetInt(AutosaveDaysPref, 0));
            _autosaveSlots = Mathf.Clamp(PlayerPrefs.GetInt(AutosaveSlotsPref, 3), MinSlots, MaxSlots);
        }
    }
}
