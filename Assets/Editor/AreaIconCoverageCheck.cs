using System;
using PoliSim.Data;
using PoliSim.UI;
using UnityEditor;
using UnityEngine;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// Asks whether every AREA ICON and every PARTY EMBLEM the UI can ask for resolves through
    /// <c>Resources.Load</c> and imported in the tintable class.
    ///
    /// <para><b>WHAT THIS ENUMERATES</b> (rule 6 / the old rule 14): the ten members of
    /// <see cref="UiPalette.SystemArea"/> other than <c>Neutral</c> (which has no icon by design),
    /// each through <see cref="IconLibrary.GetAreaIcon"/> — the ONE accessor the sub-tab rows read
    /// since 2026-08-28. W-G1 removed the archetype-emblem half: see the note at the old loop. Through
    /// `IconLibrary.GetPartyMark` (W-G1). Both are the DISPLAY enums, not the folders: an
    /// area added to the enum with no icon shows up here as MISSING the day it lands, which a folder
    /// count could never say (the `PartyMarkCoverageCheck` lesson). It does NOT enumerate the
    /// `mark_party_*` ballot marks (that is `PartyMarkCoverageCheck`'s enumeration), the nav icons,
    /// or the stat icons (`StatIconCoverageCheck`).</para>
    ///
    /// <para>Built 2026-08-28 (omnibus R-K6) to close roadmap item 5's "one coverage check that does
    /// not exist": until then the area icons' and emblems' coverage was asserted from the filesystem
    /// alone. The area icons are white-on-alpha and tinted at draw time, so block compression is the
    /// documented damage vector - their format is asserted, not just the handle. The emblems are
    /// FULL-COLOUR art (IconLibrary's party art: "authored in their own real colours … callers
    /// must NOT tint them"), the class the 2026-08-11 importer ruling allows block compression for
    /// after the flags' visual check - so for them only resolution is asserted. ⚠ The first run of
    /// this check asserted RGBA32 on the emblems too and reported all four DXT5 as DAMAGED; that was
    /// the check's error, not the import's, and is recorded here so the distinction survives.</para>
    /// </summary>
    public static class AreaIconCoverageCheck
    {
        public static void Run()
        {
            CheckExit.ArmLogFold();

            // SELF-TEST FIRST: the two icons the tab bar has drawn since Phase B. If either is null,
            // the probe is broken and every "missing" below is void.
            Texture2D fiscal = IconLibrary.GetAreaIcon(UiPalette.SystemArea.Fiscal);
            Texture2D political = IconLibrary.GetAreaIcon(UiPalette.SystemArea.Political);
            Debug.Log($"SELFTEST icon_area_fiscal -> {(fiscal != null ? "OK" : "NULL - BROKEN, results below are void")}; " +
                      $"icon_area_political -> {(political != null ? "OK" : "NULL - BROKEN")}");
            if (fiscal == null || political == null)
            {
                Debug.LogError("  SELF-TEST FAILED - the probe cannot load a known-good icon; VERIFIED NOTHING.");
                CheckExit.Finish(1);
                return;
            }

            int total = 0, missing = 0, damaged = 0;

            foreach (UiPalette.SystemArea area in Enum.GetValues(typeof(UiPalette.SystemArea)))
            {
                if (area == UiPalette.SystemArea.Neutral)
                {
                    Debug.Log("  skipped   Neutral - no icon by design (the absence of an area)");
                    continue;
                }

                total++;
                Texture2D icon = IconLibrary.GetAreaIcon(area);
                if (icon == null)
                {
                    Debug.LogError($"  MISSING   icon_area_{area.ToString().ToLowerInvariant()} does not resolve through IconLibrary.GetAreaIcon");
                    missing++;
                    continue;
                }

                if (icon.format != TextureFormat.RGBA32)
                {
                    Debug.LogError($"  DAMAGED   icon_area_{area.ToString().ToLowerInvariant()} imported {icon.format}, expected RGBA32 (white-on-alpha, tinted at draw time)");
                    damaged++;
                    continue;
                }

                Debug.Log($"  ok        {area,-16} icon_area_{area.ToString().ToLowerInvariant()} {icon.width}x{icon.height} {icon.format}");
            }

            // W-G1: the archetype-emblem half of this check RETIRED with the archetypes. It looped
            // `Enum.GetValues(typeof(PartyArchetype))` and asserted an emblem for each; there is no
            // such enum now, and real parties' art is `PartyMarkCoverageCheck`'s subject - which
            // enumerates the PARTIES (53 today) rather than the folder, and so reports the 52 gaps
            // this check could never have seen. Nothing is lost by dropping it here; the four
            // delivered emblem files stay on disk as Design's work.

            if (total == 0)
            {
                Debug.LogError("  EMPTY ENUMERATION - no areas; VERIFIED NOTHING.");
                CheckExit.Finish(1);
                return;
            }

            Debug.Log($"=== Area icons: {total - missing - damaged} of {total} resolve " +
                      $"(SystemArea members as RGBA32), {missing} missing, {damaged} damaged ===");
            CheckExit.Finish(missing == 0 && damaged == 0 ? 0 : 1);
        }
    }
}
