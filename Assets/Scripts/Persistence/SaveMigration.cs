using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;

namespace PoliSim.Persistence
{
    /// <summary>
    /// §693 (ruled 2026-09-30): **THE STUDIO NAME MOVED THE GAME'S FOLDER, SO ITS SAVES AND PREFERENCES ARE CARRIED ACROSS - ONCE, BY COPY.**
    /// Unity's identity is now `DWELOP Games` / `Incumbent` (the built window's title is productName's). On Windows both the save folder
    /// (`Application.persistentDataPath` = `LocalLow\<company>\<product>`) and the preferences (the registry key
    /// `HKCU\Software\<company>\<product>`, the Editor's under `Software\Unity\UnityEditor\...`) are named by that pair, so the change left every
    /// save and preference behind at `DefaultCompany\PoliSim` (measured, §651). This carries them:
    /// <list type="bullet">
    /// <item><b>Saves</b> - where the new `saves` folder is empty and the old one exists, every file in it is COPIED across; the originals are
    /// never moved or deleted. A marker beside the new folder (<see cref="MarkerName"/>, listing what was copied) makes it once: a player who
    /// later empties the new folder does not get the old saves back.</item>
    /// <item><b>Preferences</b> - where the new key holds none of the game's (no `polisim.` value) and the old key exists, every value is COPIED
    /// across raw (name, type and bytes - PlayerPrefs' own `_h` names, so each reads back exactly as written); the old key is only read. A marker
    /// value in the new key (<see cref="PrefsMarker"/>) makes it once.</item>
    /// </list>
    /// It runs before the first save or preference access: at start-up (<see cref="RuntimeInitializeLoadType.BeforeSceneLoad"/>) and from
    /// <see cref="SaveGameService.DefaultSaveDirectory"/> (the Editor's tools read saves without entering play). Windows only - the only platform the
    /// game ships on today; elsewhere it does nothing. The core (<see cref="CopySaves"/>, <see cref="CopyPreferences"/>) takes its locations as
    /// arguments, so `SaveMigrationCheck` proves it on a planted pair without touching the real ones.
    /// </summary>
    public static class SaveMigration
    {
        public const string OldCompany = "DefaultCompany";
        public const string OldProduct = "PoliSim";
        public const string MarkerName = "migrated_from_DefaultCompany_PoliSim.txt";
        public const string PrefsMarker = "PoliSimMigratedFrom";

        /// <summary>What the last run did - the smoke and the checks read it.</summary>
        public static string LastResult { get; private set; } = "not run";

        private static bool _ran;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void AtStartup() => EnsureOnce();

        /// <summary>Once per process: the real locations, derived from Unity's own path and the pair the old identity used.</summary>
        public static void EnsureOnce()
        {
            if (_ran) { return; }
            _ran = true;
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            try
            {
                string newRoot = Application.persistentDataPath;
                DirectoryInfo company = Directory.GetParent(newRoot);
                DirectoryInfo localLow = company?.Parent;
                if (localLow == null) { LastResult = "no LocalLow above " + newRoot; return; }
                string oldRoot = Path.Combine(localLow.FullName, OldCompany, OldProduct);
                if (string.Equals(Path.GetFullPath(oldRoot).TrimEnd('\\'), Path.GetFullPath(newRoot).TrimEnd('\\'), StringComparison.OrdinalIgnoreCase))
                {
                    LastResult = "the identity is still the old one; nothing to migrate";
                    return;
                }
                string saves = CopySaves(Path.Combine(oldRoot, "saves"), Path.Combine(newRoot, "saves"), Path.Combine(newRoot, MarkerName));
                string prefix = Application.isEditor ? @"Software\Unity\UnityEditor\" : @"Software\";
                string prefs = CopyPreferences(prefix + OldCompany + @"\" + OldProduct, prefix + Application.companyName + @"\" + Application.productName);
                LastResult = "saves: " + saves + "; preferences: " + prefs;
                Debug.Log("MIGRATION: " + LastResult);
            }
            catch (Exception e)
            {
                LastResult = "failed: " + e.Message;
                Debug.LogWarning("MIGRATION: " + LastResult + " - the originals are untouched (the migration only reads them).");
            }
#else
            LastResult = "not a Windows build; nothing to migrate";
#endif
        }

        /// <summary>
        /// Copies every file of <paramref name="oldDir"/> into <paramref name="newDir"/> when the new one holds no file and no
        /// <paramref name="marker"/> stands; writes the marker (the list of what was copied). Never moves, overwrites or deletes. Returns what it did.
        /// </summary>
        public static string CopySaves(string oldDir, string newDir, string marker)
        {
            if (File.Exists(marker)) { return "already migrated (" + Path.GetFileName(marker) + ")"; }
            if (!Directory.Exists(oldDir))
            {
                WriteMarker(marker, "no old saves folder at " + oldDir);
                return "no old saves folder";
            }
            if (Directory.Exists(newDir) && Directory.GetFiles(newDir).Length > 0)
            {
                WriteMarker(marker, "the new saves folder already held files; nothing copied from " + oldDir);
                return "the new folder is not empty; nothing copied";
            }
            Directory.CreateDirectory(newDir);
            var copied = new List<string>();
            foreach (string file in Directory.GetFiles(oldDir))
            {
                string to = Path.Combine(newDir, Path.GetFileName(file));
                File.Copy(file, to, overwrite: false);
                File.SetLastWriteTimeUtc(to, File.GetLastWriteTimeUtc(file));
                copied.Add(Path.GetFileName(file));
            }
            WriteMarker(marker, "copied from " + oldDir + ":\n" + string.Join("\n", copied.ToArray()));
            return "copied " + copied.Count + " save file(s)";
        }

        private static void WriteMarker(string marker, string text)
        {
            string dir = Path.GetDirectoryName(marker);
            if (!string.IsNullOrEmpty(dir)) { Directory.CreateDirectory(dir); }
            File.WriteAllText(marker, "Incumbent's save migration, " + DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ") + "\n" + text + "\n");
        }

        /// <summary>
        /// Copies every value of the HKCU key <paramref name="oldKey"/> into <paramref name="newKey"/> - raw, name, type and bytes - when the new
        /// key holds no `polisim.` value and no <see cref="PrefsMarker"/>; sets the marker. The old key is opened read-only. Returns what it did.
        /// </summary>
        public static string CopyPreferences(string oldKey, string newKey)
        {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            if (RegOpenKeyEx(HKCU, newKey, 0, KeyRead, out UIntPtr existing) == 0)
            {
                // The marker is looked for across EVERY value before the game's own are: the registry enumerates in no promised order, and a
                // key the migration wrote lists its copied values ahead of the marker it wrote last (found by SaveMigrationCheck, §693).
                List<(string Name, int Type, byte[] Data)> present;
                try { present = Values(existing); }
                finally { RegCloseKey(existing); }
                if (present.Exists(v => v.Name == PrefsMarker)) { return "already migrated"; }
                if (present.Exists(v => v.Name.StartsWith("polisim.", StringComparison.Ordinal))) { return "the new key already holds the game's preferences; nothing copied"; }
            }
            if (RegOpenKeyEx(HKCU, oldKey, 0, KeyRead, out UIntPtr from) != 0) { return "no old preferences"; }
            var values = new List<(string Name, int Type, byte[] Data)>();
            try { values.AddRange(Values(from)); }
            finally { RegCloseKey(from); }
            if (RegCreateKeyEx(HKCU, newKey, 0, null, 0, KeyWrite | KeyRead, IntPtr.Zero, out UIntPtr to, out _) != 0) { throw new IOException("cannot open " + newKey); }
            try
            {
                foreach ((string name, int type, byte[] data) in values)
                {
                    int rc = RegSetValueEx(to, name, 0, type, data, data.Length);
                    if (rc != 0) { throw new IOException("cannot write " + name + " (" + rc + ")"); }
                }
                byte[] marker = Encoding.Unicode.GetBytes(oldKey + "\0");
                RegSetValueEx(to, PrefsMarker, 0, RegSz, marker, marker.Length);
            }
            finally { RegCloseKey(to); }
            return "copied " + values.Count + " value(s)";
#else
            return "not Windows";
#endif
        }

        /// <summary>Every value under an open key: name, registry type, raw bytes.</summary>
        public static List<(string Name, int Type, byte[] Data)> Values(UIntPtr key)
        {
            var list = new List<(string Name, int Type, byte[] Data)>();
            for (int i = 0; ; i++)
            {
                var name = new StringBuilder(16384);
                int nameLen = name.Capacity, dataLen = 0;
                int rc = RegEnumValue(key, i, name, ref nameLen, IntPtr.Zero, out int type, null, ref dataLen);
                if (rc == NoMoreItems) { break; }
                if (rc != 0 && rc != MoreData) { throw new IOException("cannot read value " + i + " (" + rc + ")"); }
                var data = new byte[Math.Max(0, dataLen)];
                name = new StringBuilder(16384);
                nameLen = name.Capacity;
                rc = RegEnumValue(key, i, name, ref nameLen, IntPtr.Zero, out type, data, ref dataLen);
                if (rc != 0) { throw new IOException("cannot read value " + i + " (" + rc + ")"); }
                if (dataLen != data.Length) { Array.Resize(ref data, dataLen); }
                list.Add((name.ToString(), type, data));
            }
            return list;
        }

        // advapi32 directly: this project's API level (.NET Standard 2.1) does not expose Microsoft.Win32.Registry.
        public static readonly UIntPtr HKCU = new UIntPtr(0x80000001u);
        public const int KeyRead = 0x20019, KeyWrite = 0x20006, RegSz = 1, RegDword = 4, RegBinary = 3;
        private const int NoMoreItems = 259, MoreData = 234;

        [DllImport("advapi32.dll", CharSet = CharSet.Unicode)] public static extern int RegOpenKeyEx(UIntPtr hKey, string subKey, int options, int samDesired, out UIntPtr result);
        [DllImport("advapi32.dll", CharSet = CharSet.Unicode)] public static extern int RegCreateKeyEx(UIntPtr hKey, string subKey, int reserved, string cls, int options, int samDesired, IntPtr security, out UIntPtr result, out int disposition);
        [DllImport("advapi32.dll", CharSet = CharSet.Unicode)] private static extern int RegEnumValue(UIntPtr hKey, int index, StringBuilder name, ref int nameLen, IntPtr reserved, out int type, byte[] data, ref int dataLen);
        [DllImport("advapi32.dll", CharSet = CharSet.Unicode)] public static extern int RegSetValueEx(UIntPtr hKey, string name, int reserved, int type, byte[] data, int dataLen);
        [DllImport("advapi32.dll", CharSet = CharSet.Unicode)] public static extern int RegDeleteTree(UIntPtr hKey, string subKey);
        [DllImport("advapi32.dll")] public static extern int RegCloseKey(UIntPtr hKey);
    }
}
