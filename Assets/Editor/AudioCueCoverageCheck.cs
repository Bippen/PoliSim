using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using PoliSim.UI;
using Debug = UnityEngine.Debug;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// P4-2 (2026-09-03): the cue catalog against the files, both directions, the way sprites are covered.
    /// (1) Every <see cref="AudioCue"/> is in the catalog exactly once. (2) Every cue that names a file finds an
    /// importable AudioClip at <c>Assets/Resources/Audio/Cues/&lt;file&gt;.*</c>. (3) Every audio file under that
    /// folder is named by at least one cue - an unnamed file is a sound nothing can play. (4) A cue without a
    /// file, and a cue on a stand-in, is PRINTED with its note (they are honest gaps, recorded in the catalog,
    /// not failures). (5) No file under the folder has a format the importer rejects (an AudioImporter that
    /// yields no clip). Exit 1 on any failure; the whole table is printed either way.
    /// </summary>
    public static class AudioCueCoverageCheck
    {
        public const string Folder = "Assets/Resources/Audio/Cues";

        public static void Run()
        {
            CheckExit.ArmLogFold();
            var sb = new StringBuilder();
            var failures = new List<string>();
            sb.Append("=== AudioCueCoverageCheck (P4-2): the cue catalog against the files, both directions ===\n");

            var seen = new Dictionary<AudioCue, int>();
            foreach (AudioCueEntry e in AudioDirector.Catalog) { seen[e.Cue] = seen.TryGetValue(e.Cue, out int n) ? n + 1 : 1; }
            foreach (AudioCue cue in Enum.GetValues(typeof(AudioCue)))
            {
                if (!seen.TryGetValue(cue, out int n)) { failures.Add($"cue {cue} is not in the catalog"); }
                else if (n != 1) { failures.Add($"cue {cue} appears {n} times in the catalog"); }
            }

            var files = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (Directory.Exists(Folder))
            {
                foreach (string path in Directory.GetFiles(Folder))
                {
                    if (path.EndsWith(".meta", StringComparison.OrdinalIgnoreCase)) { continue; }
                    files[Path.GetFileNameWithoutExtension(path)] = path.Replace('\\', '/');
                }
            }
            else { failures.Add($"the cue folder {Folder} does not exist"); }

            var named = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var standIns = new List<string>();
            var silent = new List<string>();
            sb.Append(string.Format("    {0,-22} {1,-16} {2,-12} {3}\n", "cue", "file", "state", "note"));
            foreach (AudioCueEntry e in AudioDirector.Catalog)
            {
                string state;
                if (string.IsNullOrEmpty(e.File)) { state = "NO FILE"; silent.Add(e.Cue.ToString()); }
                else
                {
                    named.Add(e.File);
                    if (!files.TryGetValue(e.File, out string path)) { state = "MISSING"; failures.Add($"cue {e.Cue} names '{e.File}', and no such file is under {Folder}"); }
                    else
                    {
                        AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                        if (clip == null) { state = "UNREADABLE"; failures.Add($"cue {e.Cue} names '{e.File}' at {path}, which the importer yields no clip for"); }
                        else { state = e.Provisional ? "stand-in" : "ok"; if (e.Provisional) { standIns.Add(e.Cue + " (" + e.File + ")"); } }
                    }
                }
                sb.Append(string.Format("    {0,-22} {1,-16} {2,-12} {3}\n", e.Cue, e.File ?? "-", state, e.Note));
            }

            foreach (KeyValuePair<string, string> f in files)
            {
                AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(f.Value);
                if (clip == null) { failures.Add($"file {f.Value} under the cue folder yields no clip - a format the importer rejects; it should not have been imported"); }
                if (!named.Contains(f.Key)) { failures.Add($"file {f.Value} is named by no cue - a sound nothing can play"); }
            }
            sb.Append(string.Format("    {0} cue(s), {1} file(s) under {2}, {3} named by a cue.\n", AudioDirector.Catalog.Length, files.Count, Folder, named.Count));
            // E-10, closed by ruling (Elias, 2026-09-03; COMPLETED.md §278): the stand-ins and the silent cue STAY, and
            // this line keeps saying so on every run - counted and named, never folded into "covered" and never a failure.
            sb.Append(string.Format("    {0} stand-in(s) [{1}] and {2} cue(s) without a file [{3}] - reported as such by ruling (E-10, 2026-09-03), not accepted as covered.\n",
                standIns.Count, string.Join(", ", standIns), silent.Count, string.Join(", ", silent)));

            if (failures.Count == 0)
            {
                sb.Append("\n=== AudioCueCoverageCheck: ALL ASSERTIONS PASS ===\n");
                Debug.Log(sb.ToString());
                CheckExit.Finish(0);
            }
            else
            {
                sb.Append($"\n=== AudioCueCoverageCheck: {failures.Count} FAILURE(S) ===\n");
                foreach (string f in failures) { sb.Append("    ").Append(f).Append('\n'); }
                Debug.LogError(sb.ToString());
                CheckExit.Finish(1);
            }
        }
    }
}
