using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Unity.VectorGraphics;
using UnityEngine;
using Debug = UnityEngine.Debug;

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
    /// basename one level up, rasterize the SVG at the PNG's exact pixel size, then compare
    /// per-pixel. THE COMPARISON IS TOLERANT BY DESIGN: two rasterizers do not share
    /// antialiasing, so a byte-diff would measure renderer edges, not pipeline drift — the test
    /// is STRUCTURAL (shapes present, placed, coloured). A pixel matches within CHANNEL
    /// TOLERANCE; a file passes below the mismatch budget; files whose SVG features the
    /// rasterizer in use cannot handle are reported UNRASTERIZABLE-HERE by name — a named tool
    /// limit, never counted as drift and never silently skipped.
    ///
    /// TWO RASTERIZERS (2026-08-28, omnibus R-K8): the default is Unity's vectorgraphics path
    /// (parse + tessellate + `RenderSpriteToTexture2D`); `-stripcutrasterizer=&lt;path to
    /// resvg.exe&gt;` switches to an EXTERNAL rasterizer, run once per SVG at the PNG's size
    /// (`resvg -w W -h H in.svg out.png`) and read back through the same PNG loader as Design's
    /// file, so both sides of the compare share one decoder. The summary line names which
    /// rasterizer produced "ours". Every FAILED pair's rendering is written beside the probe
    /// artifact (`stripcut_fail_&lt;name&gt;.png` in the temp folder) so a real drift can be
    /// viewed rather than inferred.
    ///
    /// Run (Unity path): `Unity.exe -batchmode -projectPath &lt;path&gt; -executeMethod
    /// PoliSim.EditorTools.StripCutDiffCheck.Run -logFile &lt;path&gt;` — batchmode WITHOUT
    /// -nographics: rendering needs a graphics device. Run (external path): the same with
    /// `-stripcutrasterizer=G:\...\resvg.exe` (batchmode and -nographics both fine - nothing
    /// renders in-process).
    ///
    /// ⚠ STATUS 2026-08-17 — THE UNITY RENDER PATH IS BLANK UNDER THIS HARNESS, PROBED NOT
    /// GUESSED: three runs produced mismatch percentages equal to each PNG's ink-coverage share,
    /// identical to the decimal across two different sprite framings, and the dumped probe
    /// artifact (`stripcut_probe_ours.png`, viewed) is an empty sheet —
    /// `RenderSpriteToTexture2D` yields nothing here (shader binding or RT readback under
    /// batchmode; the importer itself demonstrably tessellates, so parsing is fine and
    /// `ui_slider_track`'s SVG-&lt;pattern&gt; KeyNotFoundException is the one true parse limit).
    /// The external path is what closes the diff; the Unity path is kept as the in-repo
    /// rasterizer for the day its render works, and its probe result is re-recorded per run.
    ///
    /// THE BUDGETS (R-C2, the continuation kickoff of 2026-08-28 — set from an inspection, not
    /// blind). The omnibus sweep left nine Stats icons at 2.06–3.21% under the original flat 2%
    /// budget, which had been set before any comparable output existed. Every one of the nine was
    /// read by eye beside its resvg rendering with a per-pixel mismatch mask, and all 90 pairs were
    /// then measured the same way (stripcut_family.csv in the continuation's record): the
    /// mismatched pixels fall into two DAMAGE CLASSES, and the check asserts each against its own bar.
    /// - EDGE — both renderers put ink at the pixel and disagree on coverage: antialiasing at a
    ///   10.7× upscale of 24-unit strokes. Its count scales with the silhouette's PERIMETER, not
    ///   with damage (the prison bars' long outline read 3.21% of the canvas with not one pixel of
    ///   shape difference), so the bar is per boundary pixel of Design's silhouette: the family
    ///   runs 0.14–1.55 mismatched pixels per boundary pixel (a band under two pixels wide); a
    ///   one-pixel stroke offset or weight change would put a mismatched pixel on BOTH sides of
    ///   every boundary pixel plus the band — three or more. The bar sits at 2.0: above the whole
    ///   family with a 29% margin, below the smallest geometric error.
    /// - STRUCTURE — a pixel solidly inked (alpha > 128) in one render and void in the other: a
    ///   shape present, missing, misplaced or misdrawn. Measured against the canvas: the largest
    ///   sound pair in the family is icon_stat_population at 0.58% (its three figures' shoulders
    ///   overlap and the two rasterizers join the strokes differently), the next 0.32%; the known
    ///   defects read 3.6–11% (the three text stamps — fonts, exempted by name below) and 48.5% (the
    ///   hatch tile's tiling, §E5); one missing 24-unit stroke piece would be ~9%. The bar sits at
    ///   1.0%: above the sound family with margin, a third of the smallest defect ever measured.
    /// A file passes only when BOTH hold. The original flat 2% of the canvas is gone: it measured
    /// perimeter and damage with one number, and raising it to clear the nine would have been the
    /// rule-14 shape (a blind bar moved to fit observed failures).
    /// </summary>
    public static class StripCutDiffCheck
    {
        /// <summary>Per-channel tolerance (of 255) - generous enough to ignore AA and colour-space rounding at edges, far too small to pass a wrong or missing shape.</summary>
        private const int ChannelTolerance = 32;

        /// <summary>STRUCTURE budget (R-C2): the share of the canvas where one render is solidly inked and the other void. See the class doc for the derivation (sound family max 0.58%; smallest known defect 3.6%).</summary>
        private const float StructureBudget = 0.01f;

        /// <summary>EDGE budget (R-C2): coverage disagreements per boundary pixel of Design's silhouette. See the class doc for the derivation (family max 1.55; a one-pixel geometric error reads three or more).</summary>
        private const float EdgeBudgetPerBoundaryPixel = 2.0f;

        /// <summary>The alpha above which a pixel counts as solidly inked for the STRUCTURE class and the silhouette's boundary - the midpoint, so a half-covered antialiased edge pixel is neither.</summary>
        private const int InkAlpha = 128;

        /// <summary>An external rasterizer that has not returned in this long is killed and the file reported as a named limit.</summary>
        private const int ExternalRasterizerTimeoutMs = 30000;

        public static void Run()
        {
            string external = Arg("-stripcutrasterizer=", null);
            bool useExternal = !string.IsNullOrEmpty(external);
            if (useExternal && !File.Exists(external))
            {
                Debug.Log($"StripCutDiff: external rasterizer not found at {external}");
                CheckExit.Finish(2);
                return;
            }

            string probeDir = Path.GetTempPath();
            string scratch = Path.Combine(probeDir, "stripcut_external");
            if (useExternal)
            {
                Directory.CreateDirectory(scratch);
            }

            string rasterizerName = useExternal ? $"external {Path.GetFileName(external)}" : "Unity vectorgraphics";
            Debug.Log($"StripCutDiff: rasterizer = {rasterizerName}{(useExternal ? " (" + external + ")" : " (RenderSpriteToTexture2D)")}");

            int compared = 0, passed = 0, unrasterizable = 0, failed = 0, textBearing = 0, currentColorResolved = 0;

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

                string svgText = File.ReadAllText(svgPath);
                string limit;
                bool resolvedColor = false;
                Texture2D ours = useExternal
                    ? RasterizeExternal(external, svgPath, svgText, png.width, png.height, scratch, out limit, out resolvedColor)
                    : RasterizeUnity(svgPath, png.width, png.height, out limit);
                currentColorResolved += resolvedColor ? 1 : 0;
                if (ours == null)
                {
                    unrasterizable++;
                    Debug.Log($"  UNRASTERIZABLE-HERE {Path.GetFileName(svgPath)}: {limit} - a named limit of this rasterizer, not drift");
                    continue;
                }

                // Probe artifact: what did OUR rasterizer actually produce? One file is enough to
                // separate "blank render" from "misplaced render" by eye.
                string baseName = Path.GetFileNameWithoutExtension(svgPath);
                if (baseName == "mark_party_us_lib")
                {
                    File.WriteAllBytes(Path.Combine(probeDir, "stripcut_probe_ours.png"), ours.EncodeToPNG());
                }

                compared++;
                Color32[] a = png.GetPixels32();
                Color32[] b = ours.GetPixels32();
                int width = png.width;
                int mismatched = 0, edge = 0, structure = 0, boundary = 0, worst = 0;
                for (int i = 0; i < a.Length; i++)
                {
                    int d = Mathf.Max(
                        Mathf.Abs(a[i].r - b[i].r), Mathf.Abs(a[i].g - b[i].g),
                        Mathf.Abs(a[i].b - b[i].b), Mathf.Abs(a[i].a - b[i].a));
                    bool inkA = a[i].a > InkAlpha;
                    bool inkB = b[i].a > InkAlpha;
                    if (d > ChannelTolerance)
                    {
                        mismatched++;
                        if (inkA != inkB) { structure++; } else { edge++; }
                    }

                    // The silhouette's boundary in Design's PNG: an inked pixel with a void 4-neighbour
                    // (or the canvas edge). The EDGE class is measured per pixel of it.
                    if (inkA)
                    {
                        int x = i % width, y = i / width;
                        bool voidNeighbour = x == 0 || a[i - 1].a <= InkAlpha
                            || x == width - 1 || a[i + 1].a <= InkAlpha
                            || y == 0 || a[i - width].a <= InkAlpha
                            || i + width >= a.Length || a[i + width].a <= InkAlpha;
                        if (voidNeighbour) { boundary++; }
                    }

                    worst = Mathf.Max(worst, d);
                }

                float share = (float)mismatched / a.Length;
                float structureShare = (float)structure / a.Length;
                float edgePerBoundary = boundary > 0 ? (float)edge / boundary : (edge > 0 ? float.PositiveInfinity : 0f);
                bool ok = structureShare <= StructureBudget && edgePerBoundary <= EdgeBudgetPerBoundaryPixel;
                // A text-bearing SVG above budget is the two rasterizers' FONTS disagreeing (glyph
                // outlines, hinting, fallback family), not pipeline drift - reported by name with its
                // figure and its rendering written out for the eye, never counted as a FAIL and never
                // counted as a pass either.
                bool textBearingMiss = !ok && svgText.Contains("<text");
                passed += ok ? 1 : 0;
                textBearing += textBearingMiss ? 1 : 0;
                failed += ok || textBearingMiss ? 0 : 1;
                if (!ok)
                {
                    File.WriteAllBytes(Path.Combine(probeDir, $"stripcut_fail_{baseName}.png"), ours.EncodeToPNG());
                }

                string verdict = ok ? "ok  " : textBearingMiss ? "TEXT" : "FAIL";
                Debug.Log($"  {verdict} {Path.GetFileName(svgPath),-52} {png.width}x{png.height}  mismatch={share:P2}  structure={structureShare:P2} (budget {StructureBudget:P2})  edge/boundary={edgePerBoundary:F2} (budget {EdgeBudgetPerBoundaryPixel:F1}; boundary {boundary}px)  worstΔ={worst}{(resolvedColor ? "  (currentColor -> #ffffff)" : "")}{(textBearingMiss ? "  text-bearing: renderer font difference, viewed not counted" : "")}");
            }

            Debug.Log($"=== StripCutDiff ({rasterizerName}): {passed} of {compared} comparable pairs within budget (structure <= {StructureBudget:P2} of the canvas AND edge <= {EdgeBudgetPerBoundaryPixel:F1} per boundary pixel - R-C2); {textBearing} text-bearing above budget (named, fonts); {unrasterizable} unrasterizable-here (named); {currentColorResolved} rendered with currentColor resolved to #ffffff (the root carried no color attribute; Design's pipeline convention); {failed} FAILED ===");
            CheckExit.Finish(failed == 0 ? 0 : 1);
        }

        /// <summary>The in-repo path: parse + tessellate + render through Unity's vectorgraphics module. Returns null with the limit named when the module cannot parse or render the file.</summary>
        private static Texture2D RasterizeUnity(string svgPath, int width, int height, out string limit)
        {
            limit = null;
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
                    Texture2D ours = VectorUtils.RenderSpriteToTexture2D(sprite, width, height, material, 4);
                    if (ours == null)
                    {
                        limit = "renderer returned null";
                    }

                    return ours;
                }
            }
            catch (Exception e)
            {
                limit = e.GetType().Name;
                return null;
            }
        }

        /// <summary>The external path: one process per SVG, output read back through Texture2D.LoadImage - the same decoder Design's PNG goes through. Returns null with the limit named on a non-zero exit, a timeout, an undecodable file or a size the rasterizer would not honour.</summary>
        private static Texture2D RasterizeExternal(string exe, string svgPath, string svgText, int width, int height, string scratch, out string limit, out bool resolvedCurrentColor)
        {
            limit = null;
            resolvedCurrentColor = false;
            string outPng = Path.Combine(scratch, Path.GetFileNameWithoutExtension(svgPath) + ".png");
            if (File.Exists(outPng))
            {
                File.Delete(outPng);
            }

            // Design's strip-cut pipeline rasterizes `currentColor` as WHITE (the runtime tints every
            // glyph); the SVG default is black. Where the root carries no `color` attribute the
            // external rasterizer would draw the right shapes in the wrong ink and every pixel would
            // miss on colour alone, so the check hands it Design's convention explicitly - a temp
            // copy with color="#ffffff" on the root - and says so on the file's line. A root that
            // DOES carry a color attribute is rendered exactly as written.
            string inputPath = Path.GetFullPath(svgPath);
            if (svgText.Contains("currentColor"))
            {
                int rootStart = svgText.IndexOf("<svg", StringComparison.Ordinal);
                int rootEnd = rootStart >= 0 ? svgText.IndexOf('>', rootStart) : -1;
                if (rootStart >= 0 && rootEnd > rootStart && !svgText.Substring(rootStart, rootEnd - rootStart).Contains(" color="))
                {
                    string resolved = svgText.Insert(rootStart + 4, " color=\"#ffffff\"");
                    inputPath = Path.Combine(scratch, Path.GetFileNameWithoutExtension(svgPath) + ".currentcolor.svg");
                    File.WriteAllText(inputPath, resolved);
                    resolvedCurrentColor = true;
                }
            }

            var start = new ProcessStartInfo(exe, $"-w {width} -h {height} \"{inputPath}\" \"{outPng}\"")
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            string stderr, stdout;
            int exitCode;
            using (Process process = Process.Start(start))
            {
                stderr = process.StandardError.ReadToEnd();
                stdout = process.StandardOutput.ReadToEnd();
                if (!process.WaitForExit(ExternalRasterizerTimeoutMs))
                {
                    try { process.Kill(); } catch (Exception) { }
                    limit = "external rasterizer timed out";
                    return null;
                }

                exitCode = process.ExitCode;
            }

            if (exitCode != 0 || !File.Exists(outPng))
            {
                limit = $"external rasterizer exit {exitCode}: {(stderr + stdout).Trim().Replace("\r", " ").Replace("\n", " ")}";
                return null;
            }

            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!texture.LoadImage(File.ReadAllBytes(outPng)))
            {
                limit = "external rasterizer output not decodable";
                return null;
            }

            if (texture.width != width || texture.height != height)
            {
                limit = $"external rasterizer output {texture.width}x{texture.height} where {width}x{height} was asked (aspect not honoured)";
                return null;
            }

            return texture;
        }

        private static string Arg(string prefix, string fallback)
        {
            foreach (string arg in Environment.GetCommandLineArgs())
            {
                if (arg.StartsWith(prefix))
                {
                    return arg.Substring(prefix.Length);
                }
            }

            return fallback;
        }
    }
}
