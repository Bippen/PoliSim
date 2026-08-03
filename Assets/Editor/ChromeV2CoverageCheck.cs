using System.IO;
using UnityEditor;
using UnityEngine;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// Asks the RUNTIME question about the v2.0 chrome pack: does every delivered sprite actually resolve
    /// through <see cref="PoliSim.UI.IconLibrary.GetChrome"/>, which is `Resources.Load`, which is the only
    /// path the game itself uses?
    ///
    /// **This exists because "delivered" and "reachable" are two different states**, and this project has
    /// now been caught by the gap twice: `menu_pattern_tile` sat imported-but-unreferenced for weeks, and
    /// the six country flags plus four party emblems sat under `Assets/Art/UI/` — outside any `Resources/`
    /// folder — where `Resources.Load` could never have found them. `DeliveredAssetCheck` passed on all of
    /// them and was right to; it asks whether a delivered file landed under `Assets/`. The v2.0 pack's own
    /// manifest opens with the same warning ("Delivered ≠ reachable: verify a load in-play"), so this is
    /// that verification rather than a reading of the folder.
    ///
    /// Names are enumerated from the FILES ON DISK rather than from a hardcoded list. A hardcoded list
    /// would pass while silently omitting whatever it forgot, which is the failure it exists to catch —
    /// and `StatIconCoverageCheck`'s own 19-name list has exactly that limitation by design, since it
    /// checks the names the UI hard-codes rather than the art that exists.
    ///
    /// Run: `Unity.exe -batchmode -nographics -projectPath &lt;path&gt; -executeMethod
    /// PoliSim.EditorTools.ChromeV2CoverageCheck.Run -logFile &lt;path&gt;`
    /// </summary>
    public static class ChromeV2CoverageCheck
    {
        private const string ChromeFolder = "Assets/Resources/Art/UI/Chrome";

        public static void Run()
        {
            // SELF-TEST FIRST, per the standing rule: if a known-good sprite does not resolve, the whole
            // run is measuring a broken harness rather than a broken import.
            Texture2D selfTest = Resources.Load<Texture2D>("Art/UI/Chrome/ui_button_normal");
            Debug.Log($"SELFTEST ui_button_normal -> {(selfTest != null ? selfTest.width + "x" + selfTest.height + " OK" : "BROKEN - results below are void")}");

            string[] files = Directory.GetFiles(ChromeFolder, "*.png", SearchOption.TopDirectoryOnly);
            int ok = 0;

            foreach (string file in files)
            {
                string name = Path.GetFileNameWithoutExtension(file);
                Texture2D loaded = Resources.Load<Texture2D>("Art/UI/Chrome/" + name);

                if (loaded == null)
                {
                    Debug.Log($"  FAIL {name,-26} does not resolve through Resources.Load");
                    continue;
                }

                var importer = AssetImporter.GetAtPath(file.Replace('\\', '/')) as TextureImporter;
                string wrap = importer != null ? importer.wrapMode.ToString() : "?";
                bool alpha = importer != null && importer.alphaIsTransparency;
                bool npot = importer != null && importer.npotScale == TextureImporterNPOTScale.None;

                Debug.Log($"  ok   {name,-26} {loaded.width,4}x{loaded.height,-4} wrap={wrap,-6} alphaIsTransparency={alpha} nPOTScale=None:{npot}");
                ok++;
            }

            Debug.Log($"=== v2.0 chrome pack: {ok} of {files.Length} resolve through Resources.Load ===");
            EditorApplication.Exit(ok == files.Length ? 0 : 1);
        }
    }
}
