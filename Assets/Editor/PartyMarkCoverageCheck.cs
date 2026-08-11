using PoliSim.UI;
using UnityEditor;
using UnityEngine;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// Asks whether the delivered `mark_party_*` art RESOLVES through <see cref="IconLibrary"/>, which
    /// is the runtime half of the question and the only half a filesystem listing cannot answer.
    ///
    /// ⚠ **WRITTEN BECAUSE §1F PRESCRIBED A CHECK THAT CANNOT ANSWER THIS.** §1F closed with *"Still
    /// unverified: that the marks RESOLVE through Resources.Load. Run DeliveredAssetCheck and
    /// StatIconCoverageCheck on the next Editor open."* `StatIconCoverageCheck` enumerates
    /// `StatNodeId` plus `menu_pattern_tile` - eighteen stats and a background - and never touches
    /// `Emblems/`. It passes 19 of 19 with the marks absent, present, or corrupt, so its green result
    /// says nothing whatever about them.
    ///
    /// That is the same defect shape as the diff argument in the same section: a procedure whose scope
    /// does not contain the claim, read as evidence for it. A passing check that cannot fail for the
    /// stated reason is worse than no check, because it retires the question.
    ///
    /// The specific risk being tested is real and narrow: these four `.meta` files were HAND-WRITTEN
    /// from the `emblem_party_*` settings with fresh GUIDs. A file can sit on disk at the right path
    /// with a malformed meta and still return null from `Resources.Load`.
    /// </summary>
    public static class PartyMarkCoverageCheck
    {
        private static readonly string[] DeliveredMarks =
        {
            "mark_party_us_rep",
            "mark_party_us_dem",
            "mark_party_se_s",
            "mark_party_se_v",
        };

        public static void Run()
        {
            // SELF-TEST FIRST, matching StatIconCoverageCheck's own discipline: if a known-good emblem
            // does not load, every "missing" below is a broken probe rather than a real gap.
            Texture2D reference = IconLibrary.GetPartyEmblem(PoliSim.Data.PartyArchetype.ProgressiveAlliance);
            Debug.Log($"SELFTEST emblem_party_progressivealliance -> " +
                      $"{(reference != null ? $"{reference.width}x{reference.height} OK" : "NULL - BROKEN, results below are void")}");

            // ⚠ THE BAR IS THE CONVENTION, NOT THE NEIGHBOUR. The first version compared each mark
            // against `emblem_party_*`'s runtime format, on the reasoning that the metas were copied
            // from it. That passed 4 of 4 — and the reference itself is DXT5, so "matches the
            // reference" was green while every mark was block-compressed. A check whose bar is another
            // artifact inherits that artifact's defects, which is this session's own theme one level
            // further down.
            //
            // `emblem_party_*` is FULL-COLOUR art (§3.1: authored in real colours, never tinted).
            // `mark_party_*` is WHITE-ON-ALPHA and tinted at draw time — see IconLibrary.GetPartyMark,
            // where the naming split exists precisely to mark that difference. §3.1 separates those two
            // categories, and the separation governs importer settings too: compression on white-on-
            // alpha at icon size is the documented damage vector. So the bar is Chrome/'s corrected
            // convention (textureCompression 0), which is what the other white-on-alpha family uses.
            Debug.Log($"SELFTEST reference emblem format -> {(reference != null ? reference.format.ToString() : "n/a")} " +
                      $"(full-colour family; NOT the bar for these)");
            const TextureFormat expected = TextureFormat.RGBA32;

            int missing = 0, damaged = 0;
            foreach (string mark in DeliveredMarks)
            {
                Texture2D texture = IconLibrary.GetPartyMark(mark);
                if (texture == null)
                {
                    Debug.LogError($"  MISSING party mark -> {mark} (on disk? check the hand-written .meta)");
                    missing++;
                    continue;
                }

                // ⚠ RESOLUTION IS NOT IMPORT VERIFICATION, and the first version of this check confused
                // them. A handle coming back at 128x128 proves the GUID, the path and that the meta
                // parses. It says nothing about whether BLOCK COMPRESSION took effect - and compression
                // mangling white-on-alpha at icon sizes is the documented damage vector that produced
                // these importer settings in the first place. A compressed mark resolves at 128x128 and
                // reports green, so "resolves" was being read as "imported correctly" one level down
                // from the defect this check was written to fix.
                //
                // `format` is the runtime ground truth and needs no `isReadable` - which is just as
                // well, since these metas carry `isReadable: 0` and pixels cannot be sampled at all.
                if (texture.format != expected)
                {
                    Debug.LogError($"  DAMAGED {mark} -> {texture.width}x{texture.height} format {texture.format}, " +
                                   $"expected {expected}. Block compression on white-on-alpha at icon size.");
                    damaged++;
                    continue;
                }

                Debug.Log($"  OK {mark} -> {texture.width}x{texture.height} {texture.format}");
            }

            Debug.Log($"=== Party marks: {DeliveredMarks.Length - missing} of {DeliveredMarks.Length} resolve, " +
                      $"{DeliveredMarks.Length - missing - damaged} of {DeliveredMarks.Length - missing} at the reference format ===");
            EditorApplication.Exit(missing == 0 && damaged == 0 ? 0 : 1);
        }
    }
}
