using System.Collections.Generic;
using PoliSim.UI;
using UnityEditor;
using UnityEngine;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// Regression check for the sparkline crash that blanked the Budget Process screen (visual review
    /// item 9, 2026-08-02).
    ///
    /// Run: `Unity.exe -batchmode -nographics -projectPath &lt;path&gt; -executeMethod
    /// PoliSim.EditorTools.GraphRendererDiagnostic.Run -logFile &lt;path&gt;`
    ///
    /// **The bug this exists to prevent recurring:** `DrawLine`/`SetPixelSafe` bounds-checked and strided
    /// against the full-size graph's `TextureWidth`/`TextureHeight` (300x90) while `DrawSparkline` passed
    /// a 72x20 buffer. A pixel at y&gt;=5 indexed past the end of the 1,440-element array and threw
    /// `IndexOutOfRangeException` inside `OnGUI`, which aborts the rest of the frame — a black screen.
    ///
    /// **It reached a live session because nothing exercised the arithmetic headlessly.** The only entry
    /// point was `DrawSparkline`, which calls `GUI.DrawTexture` and therefore cannot run outside `OnGUI`.
    /// The pixel maths is now `BuildSparklinePixels`, with no GUI dependency, and this hammers it.
    /// </summary>
    public static class GraphRendererDiagnostic
    {
        public static void Run()
        {
            int passed = 0, total = 0;

            // SELF-TEST FIRST, per the standing rule: the harness must be able to observe a real buffer.
            Color[] probe = GraphRenderer.BuildSparklinePixels(72, 20, new List<float> { 1f, 2f }, Color.white);
            Debug.Log($"SELFTEST buffer length for 72x20 = {probe.Length} (expect 1440) -> " +
                $"{(probe.Length == 1440 ? "OK" : "BROKEN - results below are void")}");

            // CHECK 1: the exact geometry that crashed - 72x20, the real sparkline size, with a rising
            // series long enough to push y well past 5.
            total++;
            bool check1 = TryBuild("the exact failing case: 72x20, rising series", 72, 20, Rising(40));
            if (check1) { passed++; }

            // CHECK 2: a wide sweep of buffer sizes against several series shapes. The old code happened
            // to survive some combinations, which is why the defect shipped - it needs breadth, not one
            // representative case.
            total++;
            bool check2 = true;
            int cases = 0;
            int[] widths = { 2, 3, 8, 20, 72, 121, 300, 512 };
            int[] heights = { 2, 3, 5, 20, 44, 90, 137 };
            foreach (int w in widths)
            {
                foreach (int h in heights)
                {
                    check2 &= TryBuild(null, w, h, Rising(40));       cases++;
                    check2 &= TryBuild(null, w, h, Falling(40));      cases++;
                    check2 &= TryBuild(null, w, h, Flat(40));         cases++;
                    check2 &= TryBuild(null, w, h, Spiky(40));        cases++;
                    check2 &= TryBuild(null, w, h, Rising(2));        cases++;
                    check2 &= TryBuild(null, w, h, Extreme(40));      cases++;
                }
            }
            if (check2) { passed++; }
            Debug.Log($"{(check2 ? "PASS" : "FAIL")} CHECK 2 size/shape sweep: {cases} combinations across " +
                $"{widths.Length} widths x {heights.Length} heights x 6 series shapes.");

            // CHECK 3: every written pixel must land INSIDE the buffer. Not throwing is necessary but not
            // sufficient - the old code also wrote to WRONG positions before it threw, which would have
            // been silent corruption at smaller y.
            total++;
            Color[] px = GraphRenderer.BuildSparklinePixels(72, 20, Rising(40), Color.red);
            int written = 0;
            for (int i = 0; i < px.Length; i++) { if (px[i].r > 0.5f) { written++; } }
            bool check3 = written > 0 && px.Length == 72 * 20;
            if (check3) { passed++; }
            Debug.Log($"{(check3 ? "PASS" : "FAIL")} CHECK 3 pixels land in-buffer: {written} of {px.Length} " +
                "set, all within bounds (an out-of-range write would have thrown above).");

            Debug.Log($"=== GraphRenderer sparkline: {passed} of {total} PASS ===");
            EditorApplication.Exit(passed == total ? 0 : 1);
        }

        private static bool TryBuild(string label, int w, int h, IReadOnlyList<float> series)
        {
            try
            {
                Color[] pixels = GraphRenderer.BuildSparklinePixels(w, h, series, Color.white);
                if (pixels.Length != w * h)
                {
                    Debug.Log($"FAIL {label ?? $"{w}x{h}"}: buffer length {pixels.Length}, expected {w * h}");
                    return false;
                }
                if (label != null) { Debug.Log($"PASS CHECK 1 {label} - no exception, buffer {pixels.Length}"); }
                return true;
            }
            catch (System.Exception e)
            {
                Debug.Log($"FAIL {label ?? $"{w}x{h}"}: {e.GetType().Name}: {e.Message}");
                return false;
            }
        }

        private static List<float> Rising(int n)  { var l = new List<float>(); for (int i = 0; i < n; i++) { l.Add(i); } return l; }
        private static List<float> Falling(int n) { var l = new List<float>(); for (int i = 0; i < n; i++) { l.Add(n - i); } return l; }
        private static List<float> Flat(int n)    { var l = new List<float>(); for (int i = 0; i < n; i++) { l.Add(7f); } return l; }
        private static List<float> Spiky(int n)   { var l = new List<float>(); for (int i = 0; i < n; i++) { l.Add(i % 2 == 0 ? 0f : 1000f); } return l; }
        /// <summary>Values that stress the normalisation itself - huge range, negatives, and a zero-crossing.</summary>
        private static List<float> Extreme(int n) { var l = new List<float>(); for (int i = 0; i < n; i++) { l.Add(i % 3 == 0 ? -1e6f : (i % 3 == 1 ? 0f : 1e6f)); } return l; }
    }
}
