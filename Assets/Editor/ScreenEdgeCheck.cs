using System.Collections.Generic;
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

        /// <summary>
        /// The LONGEST CONTIGUOUS RUN of content pixels along a line before the line counts as flush
        /// (a count of stray pixels until 2026-08-28). A real element at the margin line is a contiguous
        /// edge - the narrowest that can sit there is the v3 rail's 39 px cell at 720p, a panel is
        /// hundreds - while the desk's own grain never runs. ⚠ MEASURED, 2026-08-28 (UI v3.0 Phase A):
        /// at 1280×720 every capture's bottom margin line carries 36 grain speckles above
        /// <see cref="ContentThreshold"/> (max Manhattan distance 39; real content measures 60+), in
        /// runs of one or two pixels at the tile's 128 px seams. As a COUNT they exceeded 20 on every
        /// frame and were hidden only because the tab tongues made the TOP line flush too (the
        /// asymmetry rule below); the folded, running landing frame has no tongues, and the grain
        /// surfaced as a false CLIPPED (`v3a_1280_01b_running_strip`). A run length asks the question
        /// the check exists for - does an EDGE reach the line - and the grain cannot answer it.
        /// </summary>
        private const int FlushMinPixels = 20;

        public static void Run()
        {
            RunOver(Arg("-edgepattern=", "clipfix2_*.png"));
        }

        /// <summary>The check over an EXPLICIT pattern, so the capture path can run it on the films it
        /// has just written. ⚠ **M-S16 (2026-09-01): until now this guard fired only when somebody
        /// remembered to invoke it after a capture pass.** It is registered in neither batch, because it
        /// correctly refuses to run without film — and a guard that depends on being remembered is the
        /// failure mode `C-N3` fixed for the simulation group. It was sitting inside a master-list row
        /// that called it a standing guard. **The capture driver now calls it before it exits, over its
        /// own label**, so the guard fires at the one moment its input is guaranteed to exist.</summary>
        public static void RunOver(string pattern)
        {
            // Same `-shotdir=` argument and same out-of-tree default as the capture side, so the
            // check reads where the driver writes without a second path to drift (2026-08-16, the
            // repository-weight pass — captures no longer live inside the repo).
            string shotFolder = Arg("-shotdir=", PoliSim.Testing.UiScreenshotDriver.DefaultOutputDirectory);
            string[] paths = Directory.Exists(shotFolder)
                ? Directory.GetFiles(shotFolder, pattern).OrderBy(p => p).ToArray()
                : Array.Empty<string>();

            if (paths.Length == 0)
            {
                Debug.LogError($"EDGE: no captures matched '{pattern}' in {shotFolder}/ - " +
                               "this verified NOTHING rather than finding nothing.");
                CheckExit.Finish(2);
                return;
            }

            float marginFraction = ScreenMarginFraction();
            Debug.Log($"EDGE: margin fraction {marginFraction:F3}, read from GameController rather than " +
                      $"duplicated; {paths.Length} capture(s) matching '{pattern}'.");

            int flagged = 0;
            var deadShares = new List<(string Name, float Share)>();
            foreach (string path in paths)
            {
                if (!TryAnalyse(path, marginFraction, out int left, out int top, out int right, out int bottom, out int width, out int height, out float deadShare))
                {
                    Debug.LogError($"EDGE: could not decode {Path.GetFileName(path)}");
                    flagged++;
                    continue;
                }

                // Full-bleed on an axis (both sides flush) is a BACKGROUND, not a clip - the menu screen
                // fills the whole screen on purpose. The asymmetry is the diagnosis.
                bool clipped = (Flush(right) && !Flush(left)) || (Flush(bottom) && !Flush(top));
                // P2-1.1 (2026-09-02): at a ZERO margin the frame is the sheet, and the rule is the row's own
                // done-when - flush on all four sides. Some sides flush and others not is a gap that crept back
                // (or a screen that never filled its frame); NO side flush is a takeover on the ground (the
                // country selector, the saves menu), symmetric, and a background by the rule above. The
                // asymmetry test above still names a clip.
                // At zero margin "flush" is stricter than the >20 px run the clip test uses: the sheet must COVER
                // the line (FullLineFraction of its length). The rail is content down the whole left column and
                // along the bottom row's first cells whatever the sheet does, so a 20 px run would have called a
                // 33 px band under the sheet flush - the first zero-margin film did exactly that.
                // Board 1n-r3 (D11 row 4, 2026-09-02): the LEFT line is the tongue column - paper tabs with 4 u of
                // desk ground between them by ruling, and ground under the utility block - so it can never be
                // covered 90 % and is not asked to be. What the left line must still prove is that the tongues
                // REACH the frame: a run at least one tongue tall (the narrowest is the 39 px cell at 720p, and
                // the grain never runs 20), which a band of ground along the edge fails at zero.
                int flushSides = (Flush(left) ? 1 : 0) + (Covers(top, width) ? 1 : 0) + (Covers(right, height) ? 1 : 0) + (Covers(bottom, width) ? 1 : 0);
                bool gap = marginFraction <= 0f && flushSides > 0 && flushSides < 4;
                string line = $"  {Path.GetFileNameWithoutExtension(path),-46} " +
                              $"L{left,5} T{top,5} R{right,5} B{bottom,5}   dead {deadShare * 100f,5:F1}%";
                deadShares.Add((Path.GetFileNameWithoutExtension(path), deadShare));

                if (clipped || gap)
                {
                    Debug.LogError(line + (clipped ? "   <-- CLIPPED" : "   <-- NOT FLUSH (a gap at zero margin)"));
                    flagged++;
                }
                else
                {
                    Debug.Log(line);
                }
            }

            Debug.Log($"=== Screen edges: {paths.Length} capture(s), {flagged} clipped " +
                      $"(4 pixel lines per screen; right/bottom only; flushness as the longest content run, not overrun) ===");
            // P2-4.1: the dead-space annex line - the mean over the pass and the five emptiest captures, ground-coloured share of the frame.
            if (deadShares.Count > 0)
            {
                deadShares.Sort((a, b) => b.Share.CompareTo(a.Share));
                float mean = 0f;
                foreach ((string _, float share) in deadShares) { mean += share; }
                mean /= deadShares.Count;
                var emptiest = new List<string>();
                for (int i = 0; i < Mathf.Min(5, deadShares.Count); i++) { emptiest.Add($"{deadShares[i].Name} {deadShares[i].Share * 100f:F1}%"); }
                Debug.Log($"=== Dead space: mean {mean * 100f:F1}% of the frame ground-coloured over {deadShares.Count} capture(s); emptiest: {string.Join(", ", emptiest)} ===");
            }
            CheckExit.Finish(flagged == 0 ? 0 : 1);
        }

        private static bool Flush(int count) => count > FlushMinPixels;
        /// <summary>P2-1.1: the share of a margin line a frame edge must cover to count as flush at zero margin. The
        /// hold banner's plate and the paper both read as content against the desk; a band of desk under the sheet
        /// breaks the bottom row's run at the rail's edge, far below this.</summary>
        private const float FullLineFraction = 0.9f;
        private static bool Covers(int run, int lineLength) => run >= lineLength * FullLineFraction;

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
            out int left, out int top, out int right, out int bottom, out int width, out int height, out float deadShare)
        {
            left = top = right = bottom = width = height = 0;
            deadShare = 0f;

            var texture = new Texture2D(2, 2);
            try
            {
                if (!texture.LoadImage(File.ReadAllBytes(path)))
                {
                    return false;
                }

                width = texture.width;
                height = texture.height;
                Color32[] pixels = texture.GetPixels32();

                // The desk colour, READ FROM THE THEME (P2-1.1, 2026-09-02). It used to be sampled at pixel
                // (1,1), "outside the margin by construction" - and at a zero margin that pixel is the rail,
                // so every verdict flipped. The theme is the one statement of what the ground is.
                Color32 desk = PoliSim.UI.PoliSimTheme.Desk;
                // P2-4.1 (2026-09-02): the dead-space measure - the share of the frame within the content threshold of a
                // ground (the desk, the paper, the tile): what is drawn nothing on. A measure for the annex, not a verdict.
                Color32 paper = PoliSim.UI.PoliSimTheme.Card;
                Color32 tile = PoliSim.UI.PoliSimTheme.Tile;
                long dead = 0;
                for (int i = 0; i < pixels.Length; i++)
                {
                    if (!IsContent(pixels[i], desk) || !IsContent(pixels[i], paper) || !IsContent(pixels[i], tile)) { dead++; }
                }
                deadShare = pixels.Length > 0 ? dead / (float)pixels.Length : 0f;

                int marginX = Mathf.RoundToInt(width * marginFraction);
                int marginY = Mathf.RoundToInt(height * marginFraction);
                int rightX = width - marginX - 1;
                int bottomY = marginY;                  // bottom-up: the capture's bottom edge is low y
                int topY = height - marginY - 1;

                // Longest contiguous run per line (see FlushMinPixels): a run resets on the first
                // desk pixel, so isolated grain speckles never accumulate into a verdict.
                int leftRun = 0, rightRun = 0, topRun = 0, bottomRun = 0;
                for (int y = 0; y < height; y++)
                {
                    leftRun = IsContent(pixels[y * width + marginX], desk) ? leftRun + 1 : 0;
                    rightRun = IsContent(pixels[y * width + rightX], desk) ? rightRun + 1 : 0;
                    left = Mathf.Max(left, leftRun);
                    right = Mathf.Max(right, rightRun);
                }

                for (int x = 0; x < width; x++)
                {
                    topRun = IsContent(pixels[topY * width + x], desk) ? topRun + 1 : 0;
                    bottomRun = IsContent(pixels[bottomY * width + x], desk) ? bottomRun + 1 : 0;
                    top = Mathf.Max(top, topRun);
                    bottom = Mathf.Max(bottom, bottomRun);
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
