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

            int total = 0, missing = 0, gaps = 0;
            foreach (StatNodeId stat in System.Enum.GetValues(typeof(StatNodeId)))
            {
                total++;
                string name = PolicyScreenStatsRenderer.GetIconName(stat);
                Texture2D icon = IconLibrary.GetStat(name);
                if (icon == null)
                {
                    // ⚠ R-CL4 (2026-08-31, C-F1): A MISSING **STAT ICON** IS A REPORTED GAP, NOT A
                    // FAILURE — `PartyMarkCoverageCheck`'s own precedent, where 52 undrawn marks are the
                    // Design ask's evidence rather than a broken build. §E4 promoted two StatNodeId
                    // members that have no delivered icon precisely so the ask has something to point
                    // at, and a check that went red on its own evidence would be turned off within a
                    // week. **Logged as an R-N1 fork: a check's severity changed.**
                    //
                    // ⚠ What still FAILS: the empty enumeration below, and a missing texture in the
                    // hard-coded literal list further down — that one is a name a draw call passes with
                    // no fallback, which is a different fact from an icon the row simply draws without.
                    Debug.Log($"  GAP {stat} -> {name} (no icon delivered; the row draws without one)");
                    gaps++;
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
                    // ⚠ THE WRAP-MODE ASSERTION MOVED OUT OF HERE 2026-08-11, to
                    // `ImporterSettingsCheck`'s Tiling class. It was the only importer-settings test in
                    // this file, and leaving it would have meant two checks asserting overlapping
                    // properties of the same texture with no rule about which is authoritative. This
                    // file asks ONE question - does a name the UI hard-codes resolve - and that is now
                    // all it asks.
                    Debug.Log($"  ok   {textureName} -> {texture.width}x{texture.height} (settings: see ImporterSettingsCheck)");
                }
            }

            // ⚠ NAME THE ENUMERATION, NOT THE INTENT (rule 14). This covers every `StatNodeId` icon plus
            // `menu_pattern_tile` - 19 names. It was cited twice as proof that four newly imported party
            // marks resolved; it never touches `Emblems/` and passed 19 of 19 while they were absent from
            // its scope entirely. `PartyMarkCoverageCheck` covers those.
            // ⚠ AN EMPTY ENUMERATION IS NOT A PASS. "19 of 19" and "0 of 0" both read as clean.
            if (total == 0)
            {
                Debug.LogError("  EMPTY ENUMERATION — no StatNodeId values. VERIFIED NOTHING.");
                CheckExit.Finish(1);
                return;
            }

            Debug.Log($"=== UI art coverage: {total - missing - gaps} of {total} names resolve, {gaps} reported GAP(s) " +
                      $"(every StatNodeId icon + menu_pattern_tile; NOT chrome, emblems, marks or portraits) ===");
            if (gaps > 0)
            {
                Debug.Log($"=== {gaps} stat icon(s) UNDELIVERED, reported as GAPs and not failures (R-CL4) — they are the Design ask's own evidence ===");
            }

            CheckExit.Finish(missing == 0 ? 0 : 1);
        }
    }
}
