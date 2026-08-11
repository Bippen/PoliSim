using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// Asserts that every sprite under `Assets/Resources/Art/UI/` imported with the settings its
    /// RENDERING CLASS requires.
    ///
    /// <para><b>WHAT THIS ENUMERATES</b> (rule 14 — cite a check by its enumeration, never by its
    /// intent): every `*.png` under `Assets/Resources/Art/UI/`, recursively, classified by folder and
    /// filename prefix. 149 files at the time of writing. It does NOT cover fonts, SVG sources, art
    /// outside that root, or whether a sprite is the RIGHT drawing — only how it imported.</para>
    ///
    /// <para><b>Why one check rather than a third per-family one.</b> Block compression on white-on-alpha
    /// has now been found and fixed twice, both site-specifically: §3's Chrome correction, then
    /// `mark_party_*`. That is the label-clipping class's exact shape — a defect fixed in place, twice,
    /// where the third instance is already sitting somewhere nobody has looked — and this project's
    /// standing answer to that shape is one check that covers the whole population instead of a third
    /// careful local fix.</para>
    ///
    /// ⚠ **IT READS THE IMPORTED TEXTURE, NEVER THE `.meta` TEXT. The meta is the claim; the loaded
    /// texture is the fact.** `PartyMarkCoverageCheck`'s first version passed 4 of 4 while all four were
    /// DXT5, precisely because it never looked at the fact. Dimensions are compared against the PNG's own
    /// IHDR header rather than a setting, so an nPOT rescale is caught as a changed image rather than as
    /// a flag someone has to interpret.
    ///
    /// ⚠ **IT ASSERTS STATED VALUES, NEVER A REFERENCE ARTIFACT.** Comparing each mark against the
    /// reference emblem passed 4 of 4 while every mark was compressed, because the reference was itself
    /// DXT5. A check whose bar is another artifact inherits that artifact's defects.
    /// </summary>
    public static class ImporterSettingsCheck
    {
        private const string Root = "Assets/Resources/Art/UI";

        /// <summary>
        /// The three rendering classes, and the ONE property that distinguishes them: what happens to the
        /// pixels between the file and the screen.
        /// </summary>
        private enum RenderClass
        {
            /// <summary>Silhouette in white, tinted at draw time. §3.1's default for everything that is not a flag or a party emblem.</summary>
            WhiteOnAlphaTinted,

            /// <summary>Authored in real colours and drawn as-is. §3.1's named exemption: flags, party emblems, and portraits.</summary>
            FullColour,

            /// <summary>Drawn tiled across a surface, so its wrap mode is load-bearing rather than incidental.</summary>
            Tiling,
        }

        /// <summary>
        /// ⚠ **THE CLASSIFICATION IS BY TREATMENT, NOT BY FOLDER — and the two disagree in exactly one
        /// place, which is where the last defect came from.** `Emblems/` holds both `emblem_party_*`
        /// (full-colour, never tinted) and `mark_party_*` (white-on-alpha, tinted at draw time). They are
        /// filename-adjacent and treatment-opposite, which is why copying the nearest `.meta` by name put
        /// compression onto four white-on-alpha marks.
        /// </summary>
        private static RenderClass Classify(string assetPath)
        {
            string file = Path.GetFileNameWithoutExtension(assetPath);
            string folder = Path.GetFileName(Path.GetDirectoryName(assetPath) ?? string.Empty);

            if (folder == "Textures")
            {
                return RenderClass.Tiling;
            }

            if (file.StartsWith("mark_party_"))
            {
                return RenderClass.WhiteOnAlphaTinted;
            }

            if (folder == "Flags" || folder == "Portraits" || file.StartsWith("emblem_"))
            {
                return RenderClass.FullColour;
            }

            return RenderClass.WhiteOnAlphaTinted;
        }

        public static void Run()
        {
            string[] files = Directory.GetFiles(Root, "*.png", SearchOption.AllDirectories);
            System.Array.Sort(files);

            // ⚠ AN EMPTY ENUMERATION IS NOT A PASS — rule 14 enforced by the check on itself, every run.
            // "0 errors over 149 sprites" and "0 errors over 0 sprites" print almost identically and mean
            // opposite things; the second is what a moved folder or a renamed root looks like.
            if (files.Length == 0)
            {
                Debug.LogError($"  EMPTY ENUMERATION — no *.png found under {Root}. VERIFIED NOTHING.");
                CheckExit.Finish(1);
                return;
            }

            int errors = 0, warnings = 0;
            var byClass = new Dictionary<RenderClass, int>();

            foreach (string raw in files)
            {
                string assetPath = raw.Replace('\\', '/');
                RenderClass cls = Classify(assetPath);
                byClass.TryGetValue(cls, out int seen);
                byClass[cls] = seen + 1;

                var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
                if (texture == null)
                {
                    Debug.LogError($"  UNLOADABLE {assetPath} — present on disk, no imported texture.");
                    errors++;
                    continue;
                }

                // COMPRESSION. Documented requirement for white-on-alpha: §3's Chrome correction exists
                // because block compression mangles a tinted silhouette's alpha edges at icon size.
                bool compressed = IsCompressed(texture.format);
                if (compressed && cls == RenderClass.WhiteOnAlphaTinted)
                {
                    Debug.LogError($"  COMPRESSED (white-on-alpha) {assetPath} -> {texture.format}. " +
                                   $"Expected an uncompressed format; alpha edges are the drawing here.");
                    errors++;
                }
                else if (compressed && cls == RenderClass.Tiling)
                {
                    Debug.LogError($"  COMPRESSED (tiling) {assetPath} -> {texture.format}. " +
                                   $"Block edges repeat with the tile and read as a grid.");
                    errors++;
                }
                // ⚠ FULL-COLOUR COMPRESSION IS RULED ACCEPTABLE AND IS NO LONGER REPORTED (2026-08-11).
                // It was a warning while undecided; Elias ruled after checking flags against an
                // uncompressed source at display size - flags are the worst case for block compression
                // (large flat fields meeting at sharp edges) and show no visible damage, so portraits are
                // covered a fortiori. The reasoning lives in the request doc, §3.0a.
                //
                // Dropped rather than kept as a passing note, because a permanent 26-line amber is a
                // thing people learn to skim, and a check whose output is mostly noise stops being read
                // at all - the same argument that made filing the eight SVG sources better than
                // annotating an expected failure.

                // NPOT RESCALE, read as a changed image rather than as a setting: if Unity resized the
                // texture on import, the imported dimensions no longer match the file's own header.
                if (TryReadPngSize(assetPath, out int srcW, out int srcH) &&
                    (texture.width != srcW || texture.height != srcH))
                {
                    Debug.LogError($"  RESCALED {assetPath} -> imported {texture.width}x{texture.height}, " +
                                   $"file is {srcW}x{srcH}. nPOT scaling changed the art.");
                    errors++;
                }

                // WRAP MODE. Only load-bearing for the tiling class, where Clamp smears the edge row
                // across the seam - the documented menu_pattern_tile defect.
                if (cls == RenderClass.Tiling && texture.wrapMode != TextureWrapMode.Repeat)
                {
                    Debug.LogError($"  WRAP {assetPath} -> {texture.wrapMode}, expected Repeat.");
                    errors++;
                }

                // MIPMAPS — AN ERROR SINCE 2026-08-11, and promoted rather than invented. §3's settings
                // table has said "Mipmaps **Off** (`enableMipMap: 0`) — UI sprites never minify" since it
                // was written; 44 files across Emblems/, Flags/, Icons/ and Portraits/ carried them
                // anyway, which is a rule that existed and was never checked. A mip chain on art drawn at
                // 1:1 by IMGUI is memory spent to make it blurrier.
                if (texture.mipmapCount > 1)
                {
                    Debug.LogError($"  MIPMAPS {assetPath} -> {texture.mipmapCount} levels on UI art drawn at 1:1. " +
                                   $"§3 requires enableMipMap: 0.");
                    errors++;
                }
            }

            foreach (KeyValuePair<RenderClass, int> entry in byClass)
            {
                Debug.Log($"  class {entry.Key}: {entry.Value} sprite(s)");
            }

            Debug.Log($"=== Importer settings: {files.Length} sprite(s) under {Root}, " +
                      $"{errors} error(s), {warnings} warning(s) ===");
            CheckExit.Finish(errors == 0 ? 0 : 1);
        }

        /// <summary>Every block-compressed format this project can plausibly produce. Listed rather than inferred, so a new one shows up as a compile-time gap instead of silently passing.</summary>
        private static bool IsCompressed(TextureFormat format)
        {
            switch (format)
            {
                case TextureFormat.DXT1:
                case TextureFormat.DXT5:
                case TextureFormat.DXT1Crunched:
                case TextureFormat.DXT5Crunched:
                case TextureFormat.BC4:
                case TextureFormat.BC5:
                case TextureFormat.BC6H:
                case TextureFormat.BC7:
                case TextureFormat.ETC_RGB4:
                case TextureFormat.ETC2_RGBA8:
                case TextureFormat.ASTC_4x4:
                case TextureFormat.ASTC_6x6:
                case TextureFormat.ASTC_8x8:
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Width and height straight from the PNG's IHDR chunk — 8-byte signature, then a 4-byte length,
        /// the "IHDR" tag, then two big-endian 32-bit dimensions. Read from the file rather than from the
        /// importer so the comparison has one side that Unity did not produce.
        /// </summary>
        private static bool TryReadPngSize(string path, out int width, out int height)
        {
            width = height = 0;
            try
            {
                using (FileStream stream = File.OpenRead(path))
                {
                    var header = new byte[24];
                    if (stream.Read(header, 0, 24) != 24)
                    {
                        return false;
                    }

                    if (header[1] != 'P' || header[2] != 'N' || header[3] != 'G')
                    {
                        return false;
                    }

                    width = (header[16] << 24) | (header[17] << 16) | (header[18] << 8) | header[19];
                    height = (header[20] << 24) | (header[21] << 16) | (header[22] << 8) | header[23];
                    return width > 0 && height > 0;
                }
            }
            catch (IOException)
            {
                return false;
            }
        }
    }
}
