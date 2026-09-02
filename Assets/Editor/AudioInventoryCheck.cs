using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// P4-2 (2026-09-03): the asset discipline for the sound pipeline, run against the imported cue files. For every
    /// file under <c>Assets/Resources/Audio/Cues</c>: the magic bytes (MP3 = "ID3" or a frame sync, WAV = "RIFF…WAVE",
    /// OGG = "OggS", AIFF = "FORM…AIFF") must match the extension; the sha256 is printed so the record can be checked
    /// against the pack's inventory (COMPLETED.md §261, taken from the pack BEFORE the copy - origin stream, magic,
    /// digest); the importer must yield a clip, whose channels, frequency and length are the inventory. A file whose
    /// magic bytes disagree with its extension fails - it was imported without passing the check.
    /// </summary>
    public static class AudioInventoryCheck
    {
        public static void Run()
        {
            CheckExit.ArmLogFold();
            var sb = new StringBuilder();
            var failures = new List<string>();
            string folder = AudioCueCoverageCheck.Folder;
            sb.Append("=== AudioInventoryCheck (P4-2): every imported cue file - magic bytes, digest, the clip's own measure ===\n");
            sb.Append(string.Format("    {0,-20} {1,-5} {2,8} {3,-10} {4,-18} {5,5} {6,7} {7,9}\n", "file", "ext", "bytes", "magic", "sha256 (16)", "ch", "Hz", "seconds"));
            int count = 0;
            if (Directory.Exists(folder))
            {
                foreach (string path in Directory.GetFiles(folder))
                {
                    if (path.EndsWith(".meta", StringComparison.OrdinalIgnoreCase)) { continue; }
                    count++;
                    byte[] bytes = File.ReadAllBytes(path);
                    string ext = Path.GetExtension(path).TrimStart('.').ToLowerInvariant();
                    string magic = Magic(bytes);
                    bool agrees = (ext == "mp3" && (magic == "ID3" || magic == "MPEG")) || (ext == "wav" && magic == "WAVE") || (ext == "ogg" && magic == "OGG") || ((ext == "aif" || ext == "aiff") && magic == "AIFF");
                    string sha;
                    using (var sha256 = SHA256.Create()) { sha = BitConverter.ToString(sha256.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant().Substring(0, 16); }
                    AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path.Replace('\\', '/'));
                    sb.Append(string.Format(CultureInfo.InvariantCulture, "    {0,-20} {1,-5} {2,8} {3,-10} {4,-18} {5,5} {6,7} {7,9:0.000}{8}\n",
                        Path.GetFileNameWithoutExtension(path), ext, bytes.Length, magic, sha,
                        clip != null ? clip.channels.ToString(CultureInfo.InvariantCulture) : "-", clip != null ? clip.frequency.ToString(CultureInfo.InvariantCulture) : "-", clip != null ? clip.length : 0f,
                        agrees ? "" : "   <-- magic bytes do not match the extension"));
                    if (!agrees) { failures.Add($"{Path.GetFileName(path)}: extension .{ext} but the bytes say {magic}"); }
                    if (clip == null) { failures.Add($"{Path.GetFileName(path)}: the importer yields no clip"); }
                }
            }
            else { failures.Add($"{folder} does not exist"); }
            if (count == 0) { failures.Add("no cue file under the folder - this verified nothing"); }
            sb.Append(string.Format("    {0} file(s). The pack's licence: none shipped in the folder; the origin of every file is its Zone.Identifier stream, recorded in COMPLETED.md §261.\n", count));

            if (failures.Count == 0)
            {
                sb.Append("\n=== AudioInventoryCheck: ALL ASSERTIONS PASS ===\n");
                Debug.Log(sb.ToString());
                CheckExit.Finish(0);
            }
            else
            {
                sb.Append($"\n=== AudioInventoryCheck: {failures.Count} FAILURE(S) ===\n");
                foreach (string f in failures) { sb.Append("    ").Append(f).Append('\n'); }
                Debug.LogError(sb.ToString());
                CheckExit.Finish(1);
            }
        }

        private static string Magic(byte[] b)
        {
            if (b.Length < 12) { return "short"; }
            if (b[0] == 0x49 && b[1] == 0x44 && b[2] == 0x33) { return "ID3"; }
            if (b[0] == 0xFF && (b[1] & 0xE0) == 0xE0) { return "MPEG"; }
            if (b[0] == 'R' && b[1] == 'I' && b[2] == 'F' && b[3] == 'F' && b[8] == 'W' && b[9] == 'A' && b[10] == 'V' && b[11] == 'E') { return "WAVE"; }
            if (b[0] == 'O' && b[1] == 'g' && b[2] == 'g' && b[3] == 'S') { return "OGG"; }
            if (b[0] == 'F' && b[1] == 'O' && b[2] == 'R' && b[3] == 'M' && b[8] == 'A' && b[9] == 'I' && b[10] == 'F') { return "AIFF"; }
            if (b[4] == 'f' && b[5] == 't' && b[6] == 'y' && b[7] == 'p') { return "MP4/ftyp"; }
            return "unknown";
        }
    }
}
