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

            int missing = 0;
            foreach (string mark in DeliveredMarks)
            {
                Texture2D texture = IconLibrary.GetPartyMark(mark);
                if (texture == null)
                {
                    Debug.LogError($"  MISSING party mark -> {mark} (on disk? check the hand-written .meta)");
                    missing++;
                    continue;
                }

                Debug.Log($"  OK {mark} -> {texture.width}x{texture.height}");
            }

            Debug.Log($"=== Party mark coverage: {DeliveredMarks.Length - missing} of {DeliveredMarks.Length} resolve ===");
            EditorApplication.Exit(missing == 0 ? 0 : 1);
        }
    }
}
