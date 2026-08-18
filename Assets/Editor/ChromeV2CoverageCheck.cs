using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// Asks TWO questions about the v2.0 chrome pack, in both directions:
    ///
    /// 1. **Does everything PRESENT resolve** through `Resources.Load`, the only path the game itself
    ///    uses? (Enumerated from the files on disk.)
    /// 2. **Is everything SPECIFIED present at all?** (Compared against `ChromeManifest.txt`.)
    ///
    /// **The second question is new, and it exists because the first one is not enough.** Until
    /// 2026-08-10 this check only enumerated the disk, which means it validated that what was there
    /// worked and said nothing about what was missing. Pass 3 specified four Canvas assets and shipped
    /// them as SVG sources only; the old check would have reported a clean 52/52 while all four were
    /// absent from `Resources/` entirely. A check that can only pass when art is missing is not
    /// measuring coverage.
    ///
    /// That is the delivered-versus-reachable lesson arriving from the other side. The original gap was
    /// "the file exists but nothing can load it" (`menu_pattern_tile`, the flags and emblems under
    /// `Assets/Art/UI/`). This one is "the specification names it and no file exists", and no amount of
    /// reading the folder can catch it — a folder cannot report an absence it was never told to expect.
    ///
    /// <para><b>On the hardcoded-list objection.</b> This check's previous doc comment argued against a
    /// hardcoded name list, because "a hardcoded list would pass while silently omitting whatever it
    /// forgot". That argument is still correct — for question 1, where the disk is the right source.
    /// For question 2 an external list is not a shortcut, it IS the specification, and there is no way
    /// to ask "what should be here?" without one. Two questions, two sources, and neither alone is
    /// sufficient.</para>
    ///
    /// Run: `Unity.exe -batchmode -nographics -projectPath &lt;path&gt; -executeMethod
    /// PoliSim.EditorTools.ChromeV2CoverageCheck.Run -logFile &lt;path&gt;`
    /// </summary>
    public static class ChromeV2CoverageCheck
    {
        private const string ChromeFolder = "Assets/Resources/Art/UI/Chrome";
        private const string ManifestPath = "Assets/Editor/ChromeManifest.txt";
        private const string ResourcePrefix = "Art/UI/Chrome/";

        public static void Run()
        {
            // SELF-TEST FIRST, per the standing rule: if a known-good sprite does not resolve, the whole
            // run is measuring a broken harness rather than a broken import.
            Texture2D selfTest = Resources.Load<Texture2D>(ResourcePrefix + "ui_panel_paper");
            Debug.Log($"SELFTEST ui_panel_paper -> {(selfTest != null ? selfTest.width + "x" + selfTest.height + " OK" : "BROKEN - results below are void")}");

            if (!File.Exists(ManifestPath))
            {
                Debug.Log($"FAIL manifest not found at {ManifestPath} - cannot answer 'is everything specified present?'");
                CheckExit.Finish(1);
                return;
            }

            var specified = new List<string>();
            var superseded = new HashSet<string>();
            foreach (string raw in File.ReadAllLines(ManifestPath))
            {
                string line = raw.Trim();
                if (line.Length == 0 || line.StartsWith("#"))
                {
                    continue;
                }

                if (line.StartsWith("!"))
                {
                    superseded.Add(line.Substring(1));
                }
                else
                {
                    specified.Add(line);
                }
            }

            var present = new HashSet<string>();
            foreach (string file in Directory.GetFiles(ChromeFolder, "*.png", SearchOption.TopDirectoryOnly))
            {
                present.Add(Path.GetFileNameWithoutExtension(file));
            }

            // ---- direction 1: everything present must resolve ----
            int resolved = 0;
            var unresolvable = new List<string>();
            foreach (string name in present)
            {
                Texture2D loaded = Resources.Load<Texture2D>(ResourcePrefix + name);
                if (loaded == null)
                {
                    unresolvable.Add(name);
                    Debug.Log($"  FAIL {name,-30} does not resolve through Resources.Load");
                    continue;
                }

                var importer = AssetImporter.GetAtPath($"{ChromeFolder}/{name}.png") as TextureImporter;
                string wrap = importer != null ? importer.wrapMode.ToString() : "?";
                bool alpha = importer != null && importer.alphaIsTransparency;
                bool readable = importer != null && importer.isReadable;
                string tag = specified.Contains(name) ? "ok  " : superseded.Contains(name) ? "supd" : "EXTRA";

                Debug.Log($"  {tag,-5} {name,-30} {loaded.width,4}x{loaded.height,-4} wrap={wrap,-6} alphaIsTransparency={alpha} isReadable={readable}");
                resolved++;
            }

            // ---- direction 2: everything specified must be present ----
            var missing = new List<string>();
            foreach (string name in specified)
            {
                if (!present.Contains(name))
                {
                    missing.Add(name);
                }
            }

            var unexpected = new List<string>();
            foreach (string name in present)
            {
                if (!specified.Contains(name) && !superseded.Contains(name))
                {
                    unexpected.Add(name);
                }
            }

            Debug.Log("");
            Debug.Log($"=== PRESENT      {resolved} of {present.Count} resolve through Resources.Load ===");
            Debug.Log($"=== SPECIFIED    {specified.Count - missing.Count} of {specified.Count} are present on disk ===");
            Debug.Log($"=== SUPERSEDED   {superseded.Count} known-legacy names on record, REMOVED from disk by the Track 3 ruling ===");

            foreach (string name in missing)
            {
                Debug.Log($"  MISSING  {name,-30} specified in the manifest, no file in {ChromeFolder}");
            }

            foreach (string name in unexpected)
            {
                Debug.Log($"  UNEXPECTED {name,-28} on disk but neither specified nor marked superseded - add it to the manifest or delete it");
            }

            bool ok = unresolvable.Count == 0 && missing.Count == 0 && unexpected.Count == 0;
            Debug.Log(ok
                ? "=== CHROME COVERAGE OK - both directions clean ==="
                : $"=== CHROME COVERAGE FAILED - {unresolvable.Count} unresolvable, {missing.Count} missing, {unexpected.Count} unexpected ===");

            CheckExit.Finish(ok ? 0 : 1);
        }
    }
}
