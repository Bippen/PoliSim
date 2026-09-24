using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using PoliSim.Testing;
using PoliSim.UI;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// MM-2 (2026-09-24, the spec's §9.2 as a check): THE PREFERENCES ARE PINNED AND LIVE.
    ///
    /// <para><b>(a) The pin.</b> Every `polisim.` literal under `Assets/Scripts` is a preference key the game reads; every one of
    /// them must be in <see cref="PreferenceKeys.All"/>, which is the list the film harness pins (`PreferencePins`), and every
    /// entry on that list must be read somewhere - a pinned key nothing reads is a stale entry. The driver must call `Pin` and
    /// `Restore` by name. Proved failing first: the scanner runs over a synthetic source carrying a key the list does not hold,
    /// and the run fails if it does not report it.</para>
    ///
    /// <para><b>(b) Liveness</b> - the lever-liveness rule applied to preferences: a setting that changes nothing is not shipped.
    /// Each key is written, the readers made to forget, and the value read back through the accessor the game consumes; the
    /// display's `Apply` must record the request it made of the screen; the autosave rule must be due exactly when its cadence
    /// says. The preferences are snapshotted before and restored after, so the check leaves the desk as it found it.</para>
    /// </summary>
    public static class SettingsCheck
    {
        private static readonly Regex KeyLiteral = new Regex("\"(polisim\\.[a-z0-9_.]+)\"");

        public static void Run()
        {
            CheckExit.ArmLogFold();
            string root = Directory.GetCurrentDirectory();
            string scripts = Path.Combine(root, "Assets", "Scripts");
            if (!Directory.Exists(scripts))
            {
                Debug.LogError("SETTINGS: no Assets/Scripts, so no key can be read and this run verified NOTHING.");
                CheckExit.Finish(1);
                return;
            }

            int failures = 0;
            var sb = new StringBuilder();
            sb.Append("=== SettingsCheck (MM-2): every preference pinned by the harness, every setting live ===\n");

            // (a) the pin - the scanner, self-tested first
            var synthetic = ScanText("PlayerPrefs.GetInt(\"polisim.synthetic.unpinned\", 0)");
            if (!synthetic.Contains("polisim.synthetic.unpinned"))
            {
                Debug.LogError("SETTINGS: the key scanner did not find a key planted in a synthetic source - it would report a clean tree while reading nothing.");
                failures++;
            }

            var inSources = new Dictionary<string, List<string>>(StringComparer.Ordinal);
            foreach (string path in Directory.GetFiles(scripts, "*.cs", SearchOption.AllDirectories))
            {
                foreach (string key in ScanText(SourceText.ReadWithoutComments(path)))
                {
                    if (!inSources.TryGetValue(key, out List<string> files)) { files = new List<string>(); inSources[key] = files; }
                    string name = Path.GetFileName(path);
                    if (!files.Contains(name)) { files.Add(name); }
                }
            }

            var pinned = new HashSet<string>(PreferencePins.Keys, StringComparer.Ordinal);
            sb.Append($"    THE ENUMERATION: {inSources.Count} key(s) read under Assets/Scripts, {pinned.Count} pinned by the harness.\n");
            foreach (KeyValuePair<string, List<string>> k in inSources)
            {
                bool ok = pinned.Contains(k.Key);
                sb.Append($"    {(ok ? "pinned " : "UNPINNED")} {k.Key,-34} read by {string.Join(", ", k.Value)}\n");
                if (!ok) { failures++; }
            }

            foreach (string key in pinned)
            {
                if (!inSources.ContainsKey(key)) { sb.Append($"    STALE    {key,-34} pinned but read by no source\n"); failures++; }
            }

            string driver = Path.Combine(scripts, "Testing", "UiScreenshotDriver.cs");
            string driverText = File.Exists(driver) ? SourceText.ReadWithoutComments(driver) : string.Empty;
            bool pins = driverText.Contains("PreferencePins.Pin(") && driverText.Contains("PreferencePins.Restore(");
            sb.Append($"    the film driver {(pins ? "pins and restores" : "DOES NOT pin and restore")} the preferences by name\n");
            if (!pins) { failures++; }

            // (b) liveness - under a snapshot, restored whatever happens
            int before = PreferencePins.Pin();
            try
            {
                failures += ProbeLiveness(sb);
            }
            finally
            {
                PreferencePins.Restore();
            }

            sb.Append($"    {before} preference(s) snapshotted before the probes and restored after.\n");

            if (failures > 0)
            {
                Debug.LogError($"SETTINGS: {failures} failure(s).\n{sb}");
                CheckExit.Finish(1);
                return;
            }

            Debug.Log(sb.ToString());
            CheckExit.Finish(0);
        }

        private static HashSet<string> ScanText(string text)
        {
            var keys = new HashSet<string>(StringComparer.Ordinal);
            foreach (Match m in KeyLiteral.Matches(text)) { keys.Add(m.Groups[1].Value); }
            return keys;
        }

        private static int ProbeLiveness(StringBuilder sb)
        {
            int failures = 0;

            // sound: the volume a cue plays at, and the mute that silences it
            AudioDirector.Volume = 0.35f;
            PreferenceKeys.ReloadAll();
            failures += Expect(sb, "audio.volume", Mathf.Approximately(AudioDirector.Volume, 0.35f), $"read back {AudioDirector.Volume:0.00}");
            AudioDirector.Mute = true;
            PreferenceKeys.ReloadAll();
            failures += Expect(sb, "audio.mute", AudioDirector.Mute, "read back off");

            // the † default: the desk's one provenance state
            DeskProvenance.On = true;
            PreferenceKeys.ReloadAll();
            failures += Expect(sb, "desk.provenance", DeskProvenance.On, "read back READING");

            // display: a chosen geometry is applied - the request the screen was given is the record; an off-table one is refused
            bool refused = !DisplaySettings.Set(WindowMode.Windowed, 1234, 567);
            failures += Expect(sb, "display (off the table)", refused, "an off-standard geometry was accepted");
            bool taken = DisplaySettings.Set(WindowMode.Borderless, 1600, 950);
            PreferenceKeys.ReloadAll();
            DisplaySettings.Apply();
            (WindowMode Mode, int Width, int Height)? req = DisplaySettings.LastRequest;
            failures += Expect(sb, "display.mode/width/height", taken && DisplaySettings.IsSet && req.HasValue && req.Value.Mode == WindowMode.Borderless && req.Value.Width == 1600 && req.Value.Height == 950,
                req.HasValue ? $"the screen was asked for {req.Value.Mode} {req.Value.Width}x{req.Value.Height}" : "no request reached the screen");

            // game: the default speed, the two holds, the autosave cadence and slots
            GameSettings.DefaultSpeed = 2;
            GameSettings.HoldOnCampaignOpening = false;
            GameSettings.HoldOnBudgetWindow = false;
            GameSettings.AutosaveDays = 30;
            GameSettings.AutosaveSlots = 5;
            PreferenceKeys.ReloadAll();
            failures += Expect(sb, "game.speed", GameSettings.DefaultSpeed == 2, $"read back {GameSettings.DefaultSpeed}");
            failures += Expect(sb, "game.hold.campaign", !GameSettings.HoldOnCampaignOpening, "read back holding");
            failures += Expect(sb, "game.hold.budget", !GameSettings.HoldOnBudgetWindow, "read back holding");
            failures += Expect(sb, "game.autosave.days", GameSettings.AutosaveDays == 30 && GameSettings.AutosaveDue(30, 30) && !GameSettings.AutosaveDue(29, 30) && !GameSettings.AutosaveDue(400, 0),
                $"read back {GameSettings.AutosaveDays}; due(30,30)={GameSettings.AutosaveDue(30, 30)} due(29,30)={GameSettings.AutosaveDue(29, 30)} due(400,0)={GameSettings.AutosaveDue(400, 0)}");
            failures += Expect(sb, "game.autosave.slots", GameSettings.AutosaveSlots == 5 && GameSettings.AutosaveSlot(0, 5) == 1 && GameSettings.AutosaveSlot(4, 5) == 5 && GameSettings.AutosaveSlot(5, 5) == 1,
                $"read back {GameSettings.AutosaveSlots}; slots for the 1st, 5th and 6th autosave {GameSettings.AutosaveSlot(0, 5)}, {GameSettings.AutosaveSlot(4, 5)}, {GameSettings.AutosaveSlot(5, 5)}");

            // the defaults, once the pins clear the keys: a fresh player sees these
            PreferencePins.Restore();
            PreferencePins.Pin();
            failures += Expect(sb, "the defaults", Mathf.Approximately(AudioDirector.Volume, 0.8f) && !AudioDirector.Mute && !DeskProvenance.On && !DisplaySettings.IsSet
                && GameSettings.DefaultSpeed == 0 && GameSettings.HoldOnCampaignOpening && GameSettings.HoldOnBudgetWindow && GameSettings.AutosaveDays == 0 && GameSettings.AutosaveSlots == 3,
                "a cleared preference set did not read as the documented defaults");
            return failures;
        }

        private static int Expect(StringBuilder sb, string what, bool ok, string detail)
        {
            sb.Append($"    {(ok ? "live  " : "DEAD  ")} {what,-28} {(ok ? "" : detail)}\n");
            return ok ? 0 : 1;
        }
    }
}
