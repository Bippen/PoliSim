using PoliSim.UI;
using UnityEditor;
using UnityEngine;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// Confirms every stat the B2 contextual row can draw resolves to a real sprite, through the same
    /// `Resources.Load` path the game uses rather than by checking the filesystem.
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

            Debug.Log($"=== Stat icon coverage: {total - missing} of {total} present ===");
            EditorApplication.Exit(missing == 0 ? 0 : 1);
        }
    }
}
