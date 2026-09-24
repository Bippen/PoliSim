using System.Collections.Generic;
using PoliSim.UI;
using UnityEngine;

namespace PoliSim.Testing
{
    /// <summary>
    /// MM-2 (2026-09-24): THE HARNESS PINS EVERY PREFERENCE AND RESTORES IT WHEN IT FINISHES. <see cref="Pin"/> snapshots every
    /// key of <see cref="PreferenceKeys.All"/> (present or absent, and its value), deletes them so every reader sees its default,
    /// and makes the readers forget what they had read; <see cref="Restore"/> writes the snapshot back. The film driver calls the
    /// first before its first frame and the second in `Finish`, so a film sees the game as a fresh player does and leaves the
    /// desk's preferences as it found them. `SettingsCheck` holds the list to the sources: a preference the harness does not pin
    /// fails the cheap bar.
    /// </summary>
    public static class PreferencePins
    {
        private struct Saved { public bool Present; public int Int; public float Float; }

        private static readonly Dictionary<string, Saved> Snapshot = new Dictionary<string, Saved>();
        private static bool _pinned;

        /// <summary>The keys the harness pins - the game's own enumeration, never a second list.</summary>
        public static IEnumerable<string> Keys
        {
            get { foreach ((string Key, PreferenceKind Kind) p in PreferenceKeys.All) { yield return p.Key; } }
        }

        /// <summary>Snapshot and clear. Idempotent: a second call while pinned changes nothing, so a nested harness cannot lose the first snapshot.</summary>
        public static int Pin()
        {
            if (_pinned) { return Snapshot.Count; }
            Snapshot.Clear();
            foreach ((string Key, PreferenceKind Kind) p in PreferenceKeys.All)
            {
                var saved = new Saved { Present = PlayerPrefs.HasKey(p.Key) };
                if (saved.Present)
                {
                    if (p.Kind == PreferenceKind.Float) { saved.Float = PlayerPrefs.GetFloat(p.Key); }
                    else { saved.Int = PlayerPrefs.GetInt(p.Key); }
                }

                Snapshot[p.Key] = saved;
                PlayerPrefs.DeleteKey(p.Key);
            }

            PreferenceKeys.ReloadAll();
            _pinned = true;
            return Snapshot.Count;
        }

        /// <summary>Write the snapshot back and forget it. Safe to call unpinned (nothing happens).</summary>
        public static int Restore()
        {
            if (!_pinned) { return 0; }
            int restored = 0;
            foreach ((string Key, PreferenceKind Kind) p in PreferenceKeys.All)
            {
                if (!Snapshot.TryGetValue(p.Key, out Saved saved)) { continue; }
                if (saved.Present)
                {
                    if (p.Kind == PreferenceKind.Float) { PlayerPrefs.SetFloat(p.Key, saved.Float); }
                    else { PlayerPrefs.SetInt(p.Key, saved.Int); }
                }
                else
                {
                    PlayerPrefs.DeleteKey(p.Key);
                }

                restored++;
            }

            PlayerPrefs.Save();
            PreferenceKeys.ReloadAll();
            Snapshot.Clear();
            _pinned = false;
            return restored;
        }
    }
}
