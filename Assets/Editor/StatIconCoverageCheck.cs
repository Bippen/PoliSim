using PoliSim.UI;
using UnityEditor;
using UnityEngine;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// Confirms every stat the B2 contextual row can draw resolves to a real sprite - plus the handful
    /// of other UI art loaded by a hard-coded literal name - through the same `Resources.Load` path the
    /// game uses rather than by checking the filesystem.
    ///
    /// Run: `Unity.exe -batchmode -nographics -projectPath &lt;path&gt; -executeMethod
    /// PoliSim.EditorTools.StatIconCoverageCheck.Run -logFile &lt;path&gt;`
    ///
    /// **Why this exists.** `icon_stat_interestrate` was missing for weeks and nothing said so: the icon
    /// lookup returns null for a missing sprite by design, and the row simply shifts its label left. That
    /// is the right runtime behaviour - a placeholder would imply the wrong stat - but it means a gap is
    /// invisible in play and invisible in the log. It was eventually found by cross-referencing every
    /// requested icon name against the files on disk BY HAND. This is that cross-reference, made
    /// runnable, and it is the standing rule from the sparkline crash applied to a lookup instead of to
    /// maths: where the only entry point is a draw call, extract the part a batch-mode method can reach.
    ///
    /// It also covers the *cause*, which was not carelessness but a reasonable derivation done against
    /// the wrong list. The macro icon pack derived its stats from the 29 fields on `EconomyState`;
    /// `InterestRate` lives on `CurrencyZone` instead, because a rate belongs to a currency zone rather
    /// than to one country (the Eurozone five share one). Enumerating `StatNodeId` - what the UI can
    /// actually show - is the check that would have caught it, so that is what this enumerates.
    /// </summary>
    public static class StatIconCoverageCheck
    {
        public static void Run()
        {
            // SELF-TEST FIRST: a known-present icon must load, or every "missing" below is a false
            // positive from a broken probe rather than a real gap.
            Texture2D reference = IconLibrary.GetStat("icon_stat_inflation");
            Debug.Log($"SELFTEST icon_stat_inflation -> " +
                $"{(reference != null ? $"{reference.width}x{reference.height} OK" : "NULL - BROKEN, results below are void")}");

            int total = 0, missing = 0;
            foreach (StatNodeId stat in System.Enum.GetValues(typeof(StatNodeId)))
            {
                total++;
                string name = PolicyScreenStatsRenderer.GetIconName(stat);
                Texture2D icon = IconLibrary.GetStat(name);
                if (icon == null)
                {
                    Debug.Log($"  MISSING {stat} -> {name}");
                    missing++;
                }
                else if (icon.width != 256 || icon.height != 256)
                {
                    // Not fatal - the row draws at 22px and scales - but a size that does not match the
                    // delivered convention means an asset arrived outside the established pipeline.
                    Debug.Log($"  ODD SIZE {stat} -> {name} is {icon.width}x{icon.height}, expected 256x256");
                }
            }

            // The other art the UI loads by a hard-coded literal name, which is the same question this
            // check exists for one category over: does the name a draw call passes actually resolve?
            // Kept here rather than in DeliveredAssetCheck because that one asks whether a delivery
            // arrived on disk, and a file can be present and correct while a malformed .meta leaves it
            // unloadable - which is exactly the risk when a .meta is hand-written, as this one's was.
            foreach (string textureName in new[] { "menu_pattern_tile" })
            {
                total++;
                Texture2D texture = IconLibrary.GetTexture(textureName);
                if (texture == null)
                {
                    Debug.Log($"  MISSING background texture -> {textureName}");
                    missing++;
                }
                else
                {
                    // A seamless tile is drawn with DrawTextureWithTexCoords and MUST be imported with
                    // Wrap Mode Repeat - the one place its .meta deliberately departs from the icon
                    // convention. Clamp would not fail; it would stretch the edge pixel across the
                    // screen, which reads as a design choice rather than as a broken import.
                    string wrap = texture.wrapMode.ToString();
                    bool repeats = texture.wrapMode == TextureWrapMode.Repeat;
                    if (!repeats) { missing++; }
                    Debug.Log($"  {(repeats ? "ok  " : "FAIL")} {textureName} -> {texture.width}x{texture.height}, " +
                        $"wrap {wrap}{(repeats ? string.Empty : " - expected Repeat for a seamless tile")}");
                }
            }

            Debug.Log($"=== UI art coverage: {total - missing} of {total} resolve ===");
            EditorApplication.Exit(missing == 0 ? 0 : 1);
        }
    }
}
