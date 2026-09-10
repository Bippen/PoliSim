using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using UnityEditor;
using UnityEngine;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// Flags any asset delivery whose contents are not fully present under `Assets/`.
    ///
    /// Run: `Unity.exe -batchmode -nographics -projectPath &lt;path&gt; -executeMethod
    /// PoliSim.EditorTools.DeliveredAssetCheck.Run -logFile &lt;path&gt;`
    ///
    /// **The rule this enforces: "awaiting delivery" is a status that must be re-derived from the
    /// filesystem, never trusted from a document.** It was recorded against two different assets that had
    /// already been delivered and were sitting in zips at the project root -
    /// `icon_stat_interestrate` (registered as *"REQUEST SENT, awaiting delivery"* on the same day it
    /// arrived) and `menu_pattern_tile.png` (delivered, then unimported for weeks). Neither document was
    /// wrong when written. Nothing watches the project root, and a delivery does not announce itself, so
    /// the status simply outlived the fact.
    ///
    /// **Why the check is on the zip rather than on the register.** A register can only be as current as
    /// its last edit; the zip is the delivery. Comparing what was delivered against what exists under
    /// `Assets/` is the one comparison that cannot go stale, and it is the check nobody could run before
    /// - the reconciliation that eventually found both gaps was done by hand, twice.
    ///
    /// Sibling to <see cref="StatIconCoverageCheck"/>, which asks the same question one layer down: that
    /// one verifies every icon a screen can ask for resolves, this one verifies every asset that was
    /// delivered arrived.
    /// </summary>
    public static class DeliveredAssetCheck
    {
        /// <summary>
        /// Extensions worth tracking. A delivery's `README.md` is documentation about the pack rather
        /// than a deliverable, so it is not a gap when absent - `files.zip` in the archive is nothing but
        /// documents, which is why it correctly reports zero asset entries rather than five misses.
        /// </summary>
        private static readonly HashSet<string> AssetExtensions = new HashSet<string>
        {
            ".png", ".jpg", ".jpeg", ".svg", ".cs", ".ttf", ".otf", ".shader", ".wav", ".mp3", ".asset",
        };

        /// <summary>
        /// Deliveries whose files were imported under a different name, verified against the files on
        /// disk rather than taken from the archive README's "and so on". Without this the GUI redesign
        /// pack reports 16 permanent false misses and the check becomes noise that gets ignored - which
        /// is the failure mode of every check that cries wolf.
        ///
        /// The renames are all the same edit: the pack shipped `icon_&lt;area&gt;`, the project namespaces
        /// area icons as `icon_area_&lt;SystemArea&gt;`, and two areas have longer enum names than the
        /// pack's shorthand (`crime` is `CrimeJustice`, `sovereign` is `SovereignWealth`).
        /// </summary>
        private static readonly Dictionary<string, string> ImportedAs = new Dictionary<string, string>
        {
            { "icon_crime", "icon_area_crimejustice" },
            { "icon_fiscal", "icon_area_fiscal" },
            { "icon_labor", "icon_area_labor" },
            { "icon_political", "icon_area_political" },
            { "icon_sectors", "icon_area_sectors" },
            { "icon_sovereign", "icon_area_sovereignwealth" },
            { "icon_trade", "icon_area_trade" },
            { "icon_welfare", "icon_area_welfare" },
        };

        /// <summary>
        /// Entries that are REFERENCE MATERIAL by their pack's own manifest, never deliverables -
        /// the consolidation pass (2026-08-26) caught the check disagreeing with a manifest here:
        /// Progress4's MANIFEST.md states, verbatim, "a DeliveredAssetCheck run against this pack
        /// should expect 0 PNG and 0 SVG deliverables, and a report of 'nothing found' is a PASS,
        /// not a miss. This is the pack's whole point - the board asks for a build, not for art" -
        /// yet the zip carries the board's 1920x1080 reference render, which this check counted
        /// from the zip contents and reported as a REGRESSION. The extension filter cannot tell a
        /// reference render from game art, so the exemption is per (pack, entry), doc'd against the
        /// manifest's own words, and each skip is logged (like 'supd') so it never becomes a
        /// silent hole. If a future pack ships reference imagery, it gets a row here WITH its
        /// manifest's declaration quoted - no quote, no exemption.
        /// </summary>
        private static readonly Dictionary<string, HashSet<string>> ReferenceMaterial = new Dictionary<string, HashSet<string>>
        {
            { "PoliSim v2 Design Progress4.zip", new HashSet<string> { "board_1i_law_browser.png" } },
        };

        /// <summary>
        /// Entries removed from Assets/ on DESIGN'S OWN ANSWER, by exact file name with the dated
        /// reason - the one class the '!' rows cannot carry, because the base name stays live for a
        /// sibling of another extension (the PNG strip is canonical; its namesake SVG was never its
        /// source). An archived pack that shipped one of these is not a regression; each skip is
        /// logged so the allowance stays visible.
        /// </summary>
        private static readonly Dictionary<string, string> RemovedOnDesignAnswer = new Dictionary<string, string>
        {
            { "ui_slider_track.svg", "removed 2026-08-28 on Design's §E5 answer - the 24×24 pill was the OLD chrome pack's leftover under a colliding name, not the 256×28 strip's parent; the strip is authored raster with no SVG source (StripCutDiffCheck.SourcelessByDesign)" }
        };

        /// <summary>
        /// ⚠ **A SOURCE THAT LANDS UNDER OTHER NAMES.** D18's bundle (2026-09-09) ships fifteen neutral
        /// cells whose whole purpose is to be copied to 43 destination names - `mark_party_&lt;iso&gt;_&lt;slug&gt;` -
        /// so not one of them is ever in `Assets/` under its own. A pack like that is not a regression and
        /// it is not "reference material" either: the bytes ARE deliverables, they simply arrive wearing a
        /// different name.
        ///
        /// <para>The allowance is therefore not a word. Each exempted entry is hashed out of the zip and
        /// a byte-identical file is REQUIRED to exist under `Assets/`; the count of matches is logged. An
        /// entry that landed nowhere still reports MISSING, so a bundle that was verified and then not
        /// copied cannot pass as "consumed elsewhere". Per (pack, entry), with the pack's own words.</para>
        /// </summary>
        private static readonly Dictionary<string, Dictionary<string, string>> ConsumedUnderOtherNames =
            new Dictionary<string, Dictionary<string, string>>
        {
            {
                "PoliSim v2 Design Progressd17.zip", new Dictionary<string, string>
                {
                    { "mark_cell_square_none_solid.png", "D18 cell - copied to its destination stems (§429)" },
                    { "mark_cell_disc_none_solid.png", "D18 cell - copied to its destination stems (§429)" },
                    { "mark_cell_hex_none_solid.png", "D18 cell - copied to its destination stems (§429)" },
                    { "mark_cell_wedge_none_solid.png", "D18 cell - copied to its destination stems (§429)" },
                    { "mark_cell_keystone_none_solid.png", "D18 cell - copied to its destination stems (§429)" },
                    { "mark_cell_square_bar_solid.png", "D18 cell - copied to its destination stems (§429)" },
                    { "mark_cell_disc_bar_solid.png", "D18 cell - copied to its destination stems (§429)" },
                    { "mark_cell_hex_bar_solid.png", "D18 cell - copied to its destination stems (§429)" },
                    { "mark_cell_wedge_bar_solid.png", "D18 cell - copied to its destination stems (§429)" },
                    { "mark_cell_keystone_bar_solid.png", "D18 cell - copied to its destination stems (§429)" },
                    { "mark_cell_square_notch_solid.png", "D18 cell - copied to its destination stems (§429)" },
                    { "mark_cell_disc_notch_solid.png", "D18 cell - copied to its destination stems (§429)" },
                    { "mark_cell_hex_notch_solid.png", "D18 cell - copied to its destination stems (§429)" },
                    { "mark_cell_wedge_notch_solid.png", "D18 cell - copied to its destination stems (§429)" },
                    { "mark_cell_keystone_notch_solid.png", "D18 cell - copied to its destination stems (§429)" },
                }
            },
        };

        /// <summary>
        /// Names ChromeManifest.txt rules superseded (its '!'-prefixed rows): delivered once, later
        /// replaced by the v2.0 chrome set, and REMOVED from Assets/ by the Track 3 ruling. Read from
        /// the manifest itself rather than duplicated here, so this allowance cannot drift from the
        /// ruling that grants it. An archived pack that shipped one of these is not a regression —
        /// the deletion was deliberate — but each skip is still logged, so the allowance stays
        /// visible rather than becoming a silent hole in the check.
        /// </summary>
        private static HashSet<string> LoadSuperseded()
        {
            var set = new HashSet<string>();
            string manifest = Path.Combine(Application.dataPath, "Editor", "ChromeManifest.txt");
            if (!File.Exists(manifest))
            {
                return set;
            }

            foreach (string raw in File.ReadAllLines(manifest))
            {
                string line = raw.Trim();
                if (line.StartsWith("!"))
                {
                    set.Add(line.Substring(1));
                }
            }

            return set;
        }

        public static void Run()
        {
            string projectRoot = Path.GetDirectoryName(Application.dataPath);
            Dictionary<string, string> assetsByName = IndexAssets();
            HashSet<string> superseded = LoadSuperseded();

            // SELF-TEST FIRST: the index must be able to see a file known to exist, or every "missing"
            // below is an artefact of a broken index rather than a real gap.
            bool indexOk = assetsByName.ContainsKey("icon_stat_gdp.png");
            Debug.Log($"SELFTEST asset index holds {assetsByName.Count} files, icon_stat_gdp.png found = " +
                $"{indexOk} -> {(indexOk ? "OK" : "BROKEN - results below are void")}");

            int rootGaps = 0, rootZips = 0;

            foreach (string zipPath in Directory.GetFiles(projectRoot, "*.zip", SearchOption.TopDirectoryOnly))
            {
                rootZips++;
                rootGaps += Report(zipPath, assetsByName, superseded, isAtRoot: true);
            }

            if (rootZips == 0)
            {
                Debug.Log("No zips at the project root - every delivery has been imported and archived.");
            }

            // The archive is gitignored, so a fresh clone will not have it. Its absence is not a failure.
            // Re-checking it catches the other direction: an asset deleted AFTER its pack was archived.
            string archive = Path.Combine(projectRoot, "AssetPackArchive");
            int archiveGaps = 0;
            if (Directory.Exists(archive))
            {
                foreach (string zipPath in Directory.GetFiles(archive, "*.zip", SearchOption.TopDirectoryOnly))
                {
                    archiveGaps += Report(zipPath, assetsByName, superseded, isAtRoot: false);
                }
            }
            else
            {
                Debug.Log("No AssetPackArchive/ present (it is gitignored) - skipping the archived packs.");
            }

            Debug.Log($"=== Delivered assets: {rootGaps} missing from {rootZips} root zip(s), " +
                $"{archiveGaps} missing from archived packs ===");
            CheckExit.Finish(rootGaps + archiveGaps == 0 ? 0 : 1);
        }

        /// <summary>How many files under `Assets/` are byte-identical to this zip entry. ⚠ Compares the
        /// BYTES, not the name and not the size: the whole point of the exemption above is that the name
        /// is different, and §427's rule is that a file which merely looks right is not a file that is
        /// right. Reads the entry once into memory - these are 8 KB PNGs - and walks the same asset index
        /// the check already built.</summary>
        private static int CountIdenticalCopies(ZipArchiveEntry entry)
        {
            byte[] wanted;
            using (Stream source = entry.Open())
            using (var buffer = new MemoryStream())
            {
                source.CopyTo(buffer);
                wanted = buffer.ToArray();
            }

            int copies = 0;
            string root = Path.Combine(Directory.GetCurrentDirectory(), "Assets");
            if (!Directory.Exists(root)) { return 0; }

            foreach (string path in Directory.GetFiles(root, "*" + Path.GetExtension(entry.Name), SearchOption.AllDirectories))
            {
                var info = new FileInfo(path);
                if (info.Length != wanted.Length) { continue; }

                byte[] have = File.ReadAllBytes(path);
                bool same = true;
                for (int i = 0; i < have.Length; i++) { if (have[i] != wanted[i]) { same = false; break; } }
                if (same) { copies++; }
            }

            return copies;
        }

        /// <summary>Reports one zip, returning how many of its asset entries are absent under Assets/.</summary>
        private static int Report(string zipPath, Dictionary<string, string> assetsByName, HashSet<string> superseded, bool isAtRoot)
        {
            string label = Path.GetFileName(zipPath);
            int entries = 0, missing = 0, supd = 0;

            using (FileStream stream = File.OpenRead(zipPath))
            using (var archive = new ZipArchive(stream, ZipArchiveMode.Read))
            {
                foreach (ZipArchiveEntry entry in archive.Entries)
                {
                    // A directory entry has an empty Name. Extension filter drops READMEs and the like.
                    if (string.IsNullOrEmpty(entry.Name)) { continue; }
                    if (!AssetExtensions.Contains(Path.GetExtension(entry.Name).ToLowerInvariant())) { continue; }

                    entries++;
                    if (Resolve(entry.Name, assetsByName) == null)
                    {
                        if (ReferenceMaterial.TryGetValue(label, out HashSet<string> refs) && refs.Contains(entry.Name))
                        {
                            Debug.Log($"  ref  {label}: {entry.Name} - reference material per the pack's own manifest, not a deliverable");
                            supd++;
                            continue;
                        }

                        if (superseded.Contains(Path.GetFileNameWithoutExtension(entry.Name)))
                        {
                            Debug.Log($"  supd {label}: {entry.Name} - removed by ruling (ChromeManifest '!'), not a regression");
                            supd++;
                            continue;
                        }

                        if (RemovedOnDesignAnswer.TryGetValue(entry.Name, out string answer))
                        {
                            Debug.Log($"  rmvd {label}: {entry.Name} - {answer}");
                            supd++;
                            continue;
                        }

                        if (ConsumedUnderOtherNames.TryGetValue(label, out Dictionary<string, string> consumed)
                            && consumed.TryGetValue(entry.Name, out string why))
                        {
                            int copies = CountIdenticalCopies(entry);
                            if (copies > 0)
                            {
                                Debug.Log($"  used {label}: {entry.Name} - {why}; {copies} byte-identical file(s) under Assets/");
                                supd++;
                                continue;
                            }

                            Debug.Log($"  MISSING {label}: {entry.Name} - claimed as consumed under other names "
                                      + "and NO byte-identical file exists under Assets/. The bundle was read and not copied.");
                            missing++;
                            continue;
                        }

                        Debug.Log($"  MISSING {label}: {entry.Name}");
                        missing++;
                    }
                }
            }

            string supdNote = supd > 0 ? $" ({supd} superseded-by-ruling)" : "";
            if (missing > 0)
            {
                Debug.Log($"{(isAtRoot ? "GAP" : "REGRESSION")} {label}: {entries - missing - supd} of {entries} " +
                    $"asset entries present{supdNote}. {missing} NOT imported.");
            }
            else if (isAtRoot && entries > 0)
            {
                // Not a failure, but it violates the standing convention: a pack whose contents are all
                // present belongs in /AssetPackArchive/, because a zip left at the root IS the reminder
                // that something in it is unfinished.
                Debug.Log($"ARCHIVE ME {label}: all {entries} asset entries are accounted for under " +
                    $"Assets/{supdNote}, so this zip should be moved to /AssetPackArchive/.");
            }
            else
            {
                Debug.Log($"ok   {label}: {entries - supd} of {entries} asset entries present{supdNote}.");
            }

            return missing;
        }

        /// <summary>The delivered filename, the name it was imported under, or null if genuinely absent.</summary>
        private static string Resolve(string deliveredName, Dictionary<string, string> assetsByName)
        {
            if (assetsByName.TryGetValue(deliveredName, out string direct))
            {
                return direct;
            }

            string stem = Path.GetFileNameWithoutExtension(deliveredName);
            if (ImportedAs.TryGetValue(stem, out string renamed) &&
                assetsByName.TryGetValue(renamed + Path.GetExtension(deliveredName), out string aliased))
            {
                return aliased;
            }

            return null;
        }

        /// <summary>
        /// Every file under Assets/ by bare filename. Non-zero length is required: a zero-byte file is
        /// the shape a failed copy leaves behind, and it would otherwise read as a successful import.
        /// </summary>
        private static Dictionary<string, string> IndexAssets()
        {
            var index = new Dictionary<string, string>();
            foreach (string path in Directory.GetFiles(Application.dataPath, "*", SearchOption.AllDirectories))
            {
                if (path.EndsWith(".meta")) { continue; }
                if (new FileInfo(path).Length == 0) { continue; }
                index[Path.GetFileName(path)] = path;
            }
            return index;
        }
    }
}
