using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using PoliSim.Persistence;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// §693 (ruled 2026-09-30): **THE MIGRATION, PROVED ON A PLANTED PAIR.** A save and a preference are planted at an old location - a temporary
    /// folder and a temporary registry key of this check's own under `HKCU\Software\` - the migration's own core runs
    /// (<see cref="SaveMigration.CopySaves"/>, <see cref="SaveMigration.CopyPreferences"/>), and it is asserted that **both appear intact at the new
    /// location** (the save's bytes and write time; the preference's name, type and bytes) **while the originals are untouched** (the same bytes,
    /// the same write time, the same values). Then that it runs **once** (a second run, after the old save changed, copies nothing), and that
    /// **a new location that already holds the game's data is left alone** (nothing copied over it). Every temporary folder and key is removed at
    /// the end, whatever happened. The real locations are proved by the player's own smoke (`Tools/build_player.ps1`: the migrated play save loads
    /// from the new folder).
    /// </summary>
    public static class SaveMigrationCheck
    {
        public static void Run()
        {
            CheckExit.ArmLogFold();
            var sb = new StringBuilder("=== SaveMigrationCheck (§693): a planted save and preference carried to the new location, the originals untouched ===\n");
            int failures = 0;
            void Check(bool ok, string what) { if (!ok) { failures++; } sb.Append(ok ? "    ok        " : "    FAIL      ").Append(what).Append('\n'); }
            string tag = Guid.NewGuid().ToString("N").Substring(0, 12);
            string root = Path.Combine(Path.GetTempPath(), "polisim_migration_" + tag);
            string keyRoot = @"Software\PoliSimMigrationCheck\" + tag;
            try
            {
                // ---- The saves.
                string oldSaves = Path.Combine(root, "old", "saves"), newRoot = Path.Combine(root, "new"), newSaves = Path.Combine(newRoot, "saves");
                string marker = Path.Combine(newRoot, SaveMigration.MarkerName);
                Directory.CreateDirectory(oldSaves);
                string planted = Path.Combine(oldSaves, "planted_save.json");
                byte[] save = Encoding.UTF8.GetBytes("{\"planted\":\"§693\",\"turn\":3,\"åäö\":true}");
                File.WriteAllBytes(planted, save);
                DateTime written = new DateTime(2026, 9, 1, 12, 0, 0, DateTimeKind.Utc);
                File.SetLastWriteTimeUtc(planted, written);

                string first = SaveMigration.CopySaves(oldSaves, newSaves, marker);
                string copy = Path.Combine(newSaves, "planted_save.json");
                Check(first.StartsWith("copied 1", StringComparison.Ordinal), "the planted save is copied: " + first);
                Check(File.Exists(copy) && File.ReadAllBytes(copy).SequenceEqual(save) && File.GetLastWriteTimeUtc(copy) == written, "it appears intact at the new location - the same bytes and write time");
                Check(File.Exists(planted) && File.ReadAllBytes(planted).SequenceEqual(save) && File.GetLastWriteTimeUtc(planted) == written, "the original is untouched - still there, the same bytes and write time");
                Check(File.Exists(marker) && File.ReadAllText(marker).Contains("planted_save.json"), "the marker names what was copied");

                File.WriteAllText(planted, "{\"changed\":true}");
                File.Delete(copy);
                string second = SaveMigration.CopySaves(oldSaves, newSaves, marker);
                Check(!File.Exists(copy) && second.StartsWith("already migrated", StringComparison.Ordinal), "it runs once - a second run, the old save changed and the new folder emptied, copies nothing: " + second);

                string fresh = Path.Combine(root, "fresh"), freshSaves = Path.Combine(fresh, "saves");
                Directory.CreateDirectory(freshSaves);
                File.WriteAllText(Path.Combine(freshSaves, "own.json"), "{\"own\":true}");
                string occupied = SaveMigration.CopySaves(oldSaves, freshSaves, Path.Combine(fresh, SaveMigration.MarkerName));
                Check(!File.Exists(Path.Combine(freshSaves, "planted_save.json")) && File.ReadAllText(Path.Combine(freshSaves, "own.json")) == "{\"own\":true}",
                    "a new folder that already holds saves is left alone: " + occupied);

                // ---- The preferences (raw registry values: PlayerPrefs' own `_h` names, an int as REG_DWORD, a string as REG_BINARY).
                string oldKey = keyRoot + @"\Old", newKey = keyRoot + @"\New";
                if (SaveMigration.RegCreateKeyEx(SaveMigration.HKCU, oldKey, 0, null, 0, SaveMigration.KeyWrite | SaveMigration.KeyRead, IntPtr.Zero, out UIntPtr oldHandle, out _) != 0)
                {
                    Check(false, "the old key could be created");
                }
                else
                {
                    byte[] dword = BitConverter.GetBytes(42);
                    byte[] text = Encoding.UTF8.GetBytes("DWELOP\0");
                    SaveMigration.RegSetValueEx(oldHandle, "polisim.planted_h1234567", 0, SaveMigration.RegDword, dword, dword.Length);
                    SaveMigration.RegSetValueEx(oldHandle, "polisim.planted.name_h7654321", 0, SaveMigration.RegBinary, text, text.Length);
                    SaveMigration.RegCloseKey(oldHandle);

                    string prefs = SaveMigration.CopyPreferences(oldKey, newKey);
                    Check(prefs == "copied 2 value(s)", "the planted preferences are copied: " + prefs);
                    Dictionary<string, (int Type, byte[] Data)> Read(string key)
                    {
                        var map = new Dictionary<string, (int, byte[])>();
                        if (SaveMigration.RegOpenKeyEx(SaveMigration.HKCU, key, 0, SaveMigration.KeyRead, out UIntPtr h) != 0) { return map; }
                        try { foreach (var v in SaveMigration.Values(h)) { map[v.Name] = (v.Type, v.Data); } }
                        finally { SaveMigration.RegCloseKey(h); }
                        return map;
                    }
                    var after = Read(newKey);
                    var before = Read(oldKey);
                    bool Same(Dictionary<string, (int Type, byte[] Data)> m, string name, int type, byte[] data) => m.TryGetValue(name, out var v) && v.Type == type && v.Data.SequenceEqual(data);
                    Check(Same(after, "polisim.planted_h1234567", SaveMigration.RegDword, dword) && Same(after, "polisim.planted.name_h7654321", SaveMigration.RegBinary, text),
                        "they appear intact at the new key - the same names, types and bytes");
                    Check(before.Count == 2 && Same(before, "polisim.planted_h1234567", SaveMigration.RegDword, dword) && Same(before, "polisim.planted.name_h7654321", SaveMigration.RegBinary, text),
                        "the originals are untouched - the old key holds exactly its two values, as planted");
                    Check(after.ContainsKey(SaveMigration.PrefsMarker), "the new key carries the marker");
                    string again = SaveMigration.CopyPreferences(oldKey, newKey);
                    Check(again == "already migrated", "it runs once: " + again);
                }
            }
            catch (Exception e) { failures++; sb.Append("    FAIL      threw: ").Append(e.Message).Append('\n'); }
            finally
            {
                try { if (Directory.Exists(root)) { Directory.Delete(root, recursive: true); } } catch (Exception e) { sb.Append("    note      the temporary folder stays: ").Append(e.Message).Append('\n'); }
                SaveMigration.RegDeleteTree(SaveMigration.HKCU, keyRoot);
                SaveMigration.RegDeleteTree(SaveMigration.HKCU, @"Software\PoliSimMigrationCheck");
            }
            sb.Append(failures == 0 ? "    CLEAN\n" : $"    {failures} failure(s)\n");
            if (failures > 0) { Debug.LogError(sb.ToString()); CheckExit.Finish(1); return; }
            Debug.Log(sb.ToString());
            CheckExit.Finish(0);
        }
    }
}
