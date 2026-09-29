using System;
using System.IO;
using System.Security.Cryptography;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// §664 (the Incumbent brand set, Code's side): **THE FIVE FILES DESIGN SENT ARE HELD BYTE-EXACT, AND THE PLAYER'S ICON IS THE BRAND'S.**
    /// Design's `send/incumbent_install.zip` carried the logotype and logomark SVGs, the .ico and the 256 and 512 icons with a MANIFEST
    /// (`Assets/Art/Brand/Incumbent/`, kept `-text` in `.gitattributes` so a checkout never rewrites a line ending under a digest).
    /// <see cref="Run"/> (cheap bar): every file listed in the MANIFEST is present with its digest, and the default player icon is the 512 PNG.
    /// <see cref="Install"/> sets that icon through Unity's own `PlayerSettings`, once.
    /// </summary>
    public static class IncumbentBrandCheck
    {
        private const string Folder = "Assets/Art/Brand/Incumbent";
        private const string Icon = Folder + "/incumbent_icon_512.png";

        public static void Install()
        {
            Texture2D icon = AssetDatabase.LoadAssetAtPath<Texture2D>(Icon);
            if (icon == null) { Debug.LogError($"BRAND: {Icon} does not load - nothing set"); CheckExit.Finish(1); return; }
            PlayerSettings.SetIcons(NamedBuildTarget.Unknown, new[] { icon }, IconKind.Any);
            AssetDatabase.SaveAssets();
            Debug.Log($"BRAND: the default player icon is {Icon} ({icon.width}x{icon.height})");
            CheckExit.Finish(0);
        }

        public static void Run()
        {
            string root = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string manifest = Path.Combine(root, Folder, "MANIFEST.sha256");
            int files = 0, bad = 0;
            if (!File.Exists(manifest)) { Debug.LogError($"  MISSING {Folder}/MANIFEST.sha256 - VERIFIED NOTHING"); CheckExit.Finish(1); return; }
            foreach (string line in File.ReadAllLines(manifest))
            {
                string[] f = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (f.Length < 2) { continue; }
                files++;
                string path = Path.Combine(root, Folder, f[1]);
                if (!File.Exists(path)) { Debug.LogError($"  MISSING {f[1]}"); bad++; continue; }
                string digest;
                using (var sha = SHA256.Create()) { digest = BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-", string.Empty).ToLowerInvariant(); }
                if (digest != f[0]) { Debug.LogError($"  DIGEST {f[1]} is {digest}, Design's MANIFEST says {f[0]}"); bad++; }
                else { Debug.Log($"  ok   {f[1]}"); }
            }
            if (files == 0) { Debug.LogError("  EMPTY MANIFEST - VERIFIED NOTHING"); CheckExit.Finish(1); return; }
            Texture2D[] icons = PlayerSettings.GetIcons(NamedBuildTarget.Unknown, IconKind.Any);
            bool iconSet = icons != null && icons.Length > 0 && icons[0] != null && AssetDatabase.GetAssetPath(icons[0]) == Icon;
            if (!iconSet) { Debug.LogError($"  the default player icon is {(icons != null && icons.Length > 0 && icons[0] != null ? AssetDatabase.GetAssetPath(icons[0]) : "unset")}, not {Icon}"); bad++; }
            Debug.Log($"=== Incumbent brand: {files} file(s) against Design's MANIFEST, the default icon {(iconSet ? "the brand's" : "NOT the brand's")} - {(bad == 0 ? "clean" : bad + " problem(s)")} ===");
            CheckExit.Finish(bad == 0 ? 0 : 1);
        }
    }
}
