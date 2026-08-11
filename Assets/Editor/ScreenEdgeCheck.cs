using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// Fails when a captured screen has content sitting on the last drawable pixel of `OnGUI`'s clip
    /// rect — the FRAME question, which neither text guard asks.
    ///
    /// <para><b>WHAT THIS ENUMERATES</b> (rule 14): for each PNG matching the pattern it is given, exactly
    /// FOUR lines of pixels — the margin column and row on each side. Not the interior, not any other
    /// screen, and not any resolution other than the one captured. It reports FLUSHNESS, never overrun
    /// magnitude: clipped content stops at the boundary, so the pixels past it do not exist in the
    /// capture and cannot be measured from it. **A clean run says nothing about how much slack a screen
    /// has left.**</para>
    ///
    /// <para><b>Why it exists.</b> Instance #12 was five `GUILayout` groups laid out wider and taller
    /// than `OnGUI`'s own `BeginArea`, so the area clipped them. `UiOverflowGuard` asks whether text fits
    /// its rect (it did) and `UiContainmentGuard` asks whether a child rect sits inside its container
    /// (scoped to three composite widgets). Both reported zero, correctly. This asks the question the
    /// player actually experiences, and needs no engine access beyond reading the PNGs the capture pass
    /// already writes.</para>
    ///
    /// <para><b>Ported from `screenshot_edge_check.py` (2026-08-11), which cannot run here.</b> That
    /// script is cited as this class's detector in `CLAUDE.md`, `COMPLETED.md` and the master roadmap —
    /// and **Python is not installed on this machine**; the `py` launcher's registration points at a
    /// directory that does not exist. A detector that cannot be executed in the environment that ships is
    /// not a detector. Same algorithm, same thresholds, running where every other check in this project
    /// runs.</para>
    ///
    /// ⚠ **IT ONLY FLAGS RIGHT AND BOTTOM, and that is an assumption rather than a symmetry.** GUILayout
    /// grows rightward and downward, so an over-wide group runs off those edges; the Python original made
    /// the same choice silently. Content clipped at the LEFT or TOP would pass. Stated here so it is a
    /// known limit rather than a surprise.
    /// </summary>
    public static class ScreenEdgeCheck
    {
        /// <summary>
        /// Manhattan distance in RGB from the desk colour before a pixel counts as content. Low enough to
        /// catch the paper's own drop shadow, high enough to ignore PNG quantisation on the flat desk.
        /// ⚠ Carried over from the Python original as an asserted constant, not a measured one.
        /// </summary>
        private const int ContentThreshold = 30;

        /// <summary>Sub-pixel seams and antialiasing leave a handful of stray pixels on any edge; a real clipped panel leaves hundreds. Same "a guard that cries wolf gets switched off" reasoning as the two C# guards. ⚠ Also asserted rather than measured.</summary>
        private const int FlushMinPixels = 20;

        private const string ShotFolder = "screenshots";

        public static void Run()
        {
            string pattern = Arg("-edgepattern=", "clipfix2_*.png");
            string[] paths = Directory.Exists(ShotFolder)
                ? Directory.GetFiles(ShotFolder, pattern).OrderBy(p => p).ToArray()
                : Array.Empty<string>();

            if (paths.Length == 0)
            {
                Debug.LogError($"EDGE: no captures matched '{pattern}' in {ShotFolder}/ - " +
                               "this verified NOTHING rather than finding nothing.");
                EditorApplication.Exit(2);
                return;
            }

            float marginFraction = ScreenMarginFraction();
            Debug.Log($"EDGE: margin fraction {marginFraction:F3}, read from GameController rather than " +
                      $"duplicated; {paths.Length} capture(s) matching '{pattern}'.");

            int flagged = 0;
            foreach (string path in paths)
            {
                if (!TryAnalyse(path, marginFraction, out int left, out int top, out int right, out int bottom))
                {
                    Debug.LogError($"EDGE: could not decode {Path.GetFileName(path)}");
                    flagged++;
                    continue;
                }

                // Full-bleed on an axis (both sides flush) is a BACKGROUND, not a clip - the menu screen
                // fills the whole screen on purpose. The asymmetry is the diagnosis.
                bool clipped = (Flush(right) && !Flush(left)) || (Flush(bottom) && !Flush(top));
                string line = $"  {Path.GetFileNameWithoutExtension(path),-46} " +
                              $"L{left,5} T{top,5} R{right,5} B{bottom,5}";

                if (clipped)
                {
                    Debug.LogError(line + "   <-- CLIPPED");
                    flagged++;
                }
                else
                {
                    Debug.Log(line);
                }
            }

            Debug.Log($"=== Screen edges: {paths.Length} capture(s), {flagged} clipped " +
                      $"(4 pixel lines per screen; right/bottom only; flushness, not overrun) ===");
            EditorApplication.Exit(flagged == 0 ? 0 : 1);
        }

        private static bool Flush(int count) => count > FlushMinPixels;

        /// <summary>
        /// ⚠ READ FROM `GameController`, NOT COPIED. The Python original hardcoded 0.02 with a comment
        /// saying *"if that constant changes, this must change with it"* - which is two statements of one
        /// fact, the failure this project has now recorded three times. Reflection because the field is
        /// private; a wrong answer here silently moves every edge sampled, so it falls back loudly.
        /// </summary>
        private static float ScreenMarginFraction()
        {
            var field = typeof(PoliSim.UI.GameController).GetField("ScreenMarginFraction",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

            if (field?.GetRawConstantValue() is float value)
            {
                return value;
            }

            Debug.LogWarning("EDGE: GameController.ScreenMarginFraction not found - falling back to 0.02. " +
                             "If the constant was renamed, every edge below is sampled in the wrong place.");
            return 0.02f;
        }

        private static bool TryAnalyse(string path, float marginFraction,
            out int left, out int top, out int right, out int bottom)
        {
            left = top = right = bottom = 0;

            var texture = new Texture2D(2, 2);
            try
            {
                if (!texture.LoadImage(File.ReadAllBytes(path)))
                {
                    return false;
                }

                int width = texture.width;
                int height = texture.height;
                Color32[] pixels = texture.GetPixels32();

                // The desk colour, sampled outside the margin by construction. Texture2D is
                // bottom-up, so row 1 here is the capture's second-from-bottom row - still desk.
                Color32 desk = pixels[1 * width + 1];

                int marginX = Mathf.RoundToInt(width * marginFraction);
                int marginY = Mathf.RoundToInt(height * marginFraction);
                int rightX = width - marginX - 1;
                int bottomY = marginY;                  // bottom-up: the capture's bottom edge is low y
                int topY = height - marginY - 1;

                for (int y = 0; y < height; y++)
                {
                    if (IsContent(pixels[y * width + marginX], desk)) { left++; }
                    if (IsContent(pixels[y * width + rightX], desk)) { right++; }
                }

                for (int x = 0; x < width; x++)
                {
                    if (IsContent(pixels[topY * width + x], desk)) { top++; }
                    if (IsContent(pixels[bottomY * width + x], desk)) { bottom++; }
                }

                return true;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(texture);
            }
        }

        private static bool IsContent(Color32 pixel, Color32 desk)
        {
            return Mathf.Abs(pixel.r - desk.r) + Mathf.Abs(pixel.g - desk.g) + Mathf.Abs(pixel.b - desk.b)
                   > ContentThreshold;
        }

        private static string Arg(string prefix, string fallback)
        {
            string match = Environment.GetCommandLineArgs().FirstOrDefault(a => a.StartsWith(prefix));
            return match == null ? fallback : match.Substring(prefix.Length);
        }
    }
}
