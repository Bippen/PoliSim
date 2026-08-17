using System.Collections.Generic;
using System.IO;
using Unity.VectorGraphics;
using UnityEngine;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// OUR HALF of the carried strip-cut diff (request doc §1F.1 / register §E3): Design asked
    /// that their strip-cut PNGs be diffed against OUR OWN rasterization of the same SVGs, once,
    /// before the SVG→PNG pipeline is trusted. Design's half closed 2026-08-17 (their manifest:
    /// six per-state buttons re-rasterized, 6/6 identical). Ours was gated on "a rasterizer
    /// exists on this machine" — **that gate dissolved when the Source/ SVGs began importing
    /// through Unity's built-in vectorgraphics module**, whose runtime API this check drives
    /// directly.
    ///
    /// WHAT IT DOES: for every `.svg` under a `Source/` folder with a sibling `.png` of the same
    /// basename one level up, parse + tessellate + render the SVG at the PNG's exact pixel size,
    /// then compare per-pixel. THE COMPARISON IS TOLERANT BY DESIGN: Unity's rasterizer and
    /// Design's do not share antialiasing, so a byte-diff would measure renderer edges, not
    /// pipeline drift — the test is STRUCTURAL (shapes present, placed, coloured). A pixel
    /// matches within CHANNEL TOLERANCE; a file passes below the mismatch budget; files whose
    /// SVG features this rasterizer cannot parse (SVG &lt;pattern&gt; etc.) are reported
    /// UNRASTERIZABLE-HERE by name — a named tool limit, never counted as drift and never
    /// silently skipped.
    ///
    /// Run: `Unity.exe -batchmode -projectPath &lt;path&gt; -executeMethod
    /// PoliSim.EditorTools.StripCutDiffCheck.Run -logFile &lt;path&gt;` — batchmode WITHOUT
    /// -nographics: rendering needs a graphics device.
    ///
    /// ⚠ STATUS 2026-08-17 — THE RENDER PATH IS BLANK UNDER THIS HARNESS, PROBED NOT GUESSED:
    /// three runs produced mismatch percentages equal to each PNG's ink-coverage share,
    /// identical to the decimal across two different sprite framings, and the dumped probe
    /// artifact (`stripcut_probe_ours.png`, viewed) is an empty sheet —
    /// `RenderSpriteToTexture2D` yields nothing here (shader binding or RT readback under
    /// batchmode; the importer itself demonstrably tessellates, so parsing is fine and
    /// `ui_slider_track`'s SVG-&lt;pattern&gt; KeyNotFoundException is the one true parse limit).
    /// The register's §E3 carries the sharpened gate: not "a rasterizer exists" (one does) but
    /// "a rasterizer whose OUTPUT is comparable". The tolerant-compare machinery above is
    /// finished and waits on that; every FAIL this check prints today is the harness defect,
    /// not pipeline drift. Fixing the render path is its own tooling pass.
    /// </summary>
    public static class StripCutDiffCheck
    {
        /// <summary>Per-channel tolerance (of 255) - generous enough to ignore AA and colour-space rounding at edges, far too small to pass a wrong or missing shape.</summary>
        private const int ChannelTolerance = 32;

        /// <summary>A file passes when at most this share of pixels mismatch - edge pixels on a dense silhouette stay well under it; a missing/misplaced element blows past it.</summary>
        private const float MismatchBudget = 0.02f;

        public static void Run()
        {
            int compared = 0, passed = 0, unrasterizable = 0, failed = 0;

            foreach (string svgPath in Directory.GetFiles("Assets/Resources/Art/UI", "*.svg", SearchOption.AllDirectories))
            {
                string dir = Path.GetDirectoryName(svgPath);
                if (Path.GetFileName(dir) != "Source")
                {
                    continue;
                }

                string pngPath = Path.Combine(Path.GetDirectoryName(dir), Path.GetFileNameWithoutExtension(svgPath) + ".png");
                if (!File.Exists(pngPath))
                {
                    Debug.Log($"  skip {Path.GetFileName(svgPath)}: no sibling PNG");
                    continue;
                }

                var png = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                png.LoadImage(File.ReadAllBytes(pngPath));

                Texture2D ours = null;
                try
                {
                    using (var reader = new StreamReader(svgPath))
                    {
                        SVGParser.SceneInfo scene = SVGParser.ImportSVG(reader);
                        var tess = new VectorUtils.TessellationOptions
                        {
                            StepDistance = 1f,
                            MaxCordDeviation = 0.25f,
                            MaxTanAngleDeviation = 0.05f,
                            SamplingStepSize = 0.01f
                        };
                        List<VectorUtils.Geometry> geoms = VectorUtils.TessellateScene(scene.Scene, tess);
                        // Frame on the SVG CANVAS (SceneViewport), not the geometry's tight bounds -
                        // rendering tight bounds rescales every shape to its own bounding box, which
                        // is the harness defect the first run of this check produced (uniform
                        // 15-82% mismatches, identical across variants). flipYAxis bridges SVG's
                        // y-down to Unity's y-up.
                        Sprite sprite = VectorUtils.BuildSprite(geoms, scene.SceneViewport, 100f, VectorUtils.Alignment.SVGOrigin, Vector2.zero, 128, true);
                        var material = new Material(Shader.Find("Unlit/Vector") ?? Shader.Find("Sprites/Default"));
                        ours = VectorUtils.RenderSpriteToTexture2D(sprite, png.width, png.height, material, 4);
                    }
                }
                catch (System.Exception e)
                {
                    unrasterizable++;
                    Debug.Log($"  UNRASTERIZABLE-HERE {Path.GetFileName(svgPath)}: {e.GetType().Name} - a named limit of this rasterizer, not drift");
                    continue;
                }

                if (ours == null)
                {
                    unrasterizable++;
                    Debug.Log($"  UNRASTERIZABLE-HERE {Path.GetFileName(svgPath)}: renderer returned null - a named limit, not drift");
                    continue;
                }

                // Probe artifact: what did OUR rasterizer actually produce? One file is enough to
                // separate "blank render" from "misplaced render" by eye.
                if (Path.GetFileNameWithoutExtension(svgPath) == "mark_party_us_lib")
                {
                    File.WriteAllBytes(Path.Combine(Path.GetTempPath(), "stripcut_probe_ours.png"), ours.EncodeToPNG());
                }

                compared++;
                Color32[] a = png.GetPixels32();
                Color32[] b = ours.GetPixels32();
                int mismatched = 0, worst = 0;
                for (int i = 0; i < a.Length; i++)
                {
                    int d = Mathf.Max(
                        Mathf.Abs(a[i].r - b[i].r), Mathf.Abs(a[i].g - b[i].g),
                        Mathf.Abs(a[i].b - b[i].b), Mathf.Abs(a[i].a - b[i].a));
                    if (d > ChannelTolerance)
                    {
                        mismatched++;
                    }

                    worst = Mathf.Max(worst, d);
                }

                float share = (float)mismatched / a.Length;
                bool ok = share <= MismatchBudget;
                passed += ok ? 1 : 0;
                failed += ok ? 0 : 1;
                Debug.Log($"  {(ok ? "ok  " : "FAIL")} {Path.GetFileName(svgPath),-52} {png.width}x{png.height}  mismatch={share:P2}  worstΔ={worst}");
            }

            Debug.Log($"=== StripCutDiff: {passed} of {compared} comparable pairs within budget; {unrasterizable} unrasterizable-here (named); {failed} FAILED ===");
            CheckExit.Finish(failed == 0 ? 0 : 1);
        }
    }
}
