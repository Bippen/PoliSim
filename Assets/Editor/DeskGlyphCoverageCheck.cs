using PoliSim.UI;
using UnityEditor;
using UnityEngine;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// D16 §3.6 (2026-09-09, §410): the three glyphs the legibility pass draws instead of words - `†` the PROVENANCE tab, `◇` DATED and
    /// `‡` TWO DEFINITIONS - must exist in the faces that draw them, or the desk prints a box where a mark should be and the whole
    /// device fails silently on film. The check asks each font for the character (Font.HasCharacter) and names the ones that answer no,
    /// so a missing glyph is a stated deviation with a drawn fallback rather than a surprise in a capture.
    /// </summary>
    public static class DeskGlyphCoverageCheck
    {
        private static readonly string[] Faces = { "CourierPrime-Regular", "TeXGyrePagella-Regular", "TeXGyrePagella-Bold" };
        private static readonly (string Glyph, string Name)[] Glyphs =
        {
            (DeskProvenance.Glyph, "† PROVENANCE tab"),
            (DeskProvenance.DatedGlyph, "◇ DATED"),
            (DeskProvenance.TwoDefinitionGlyph, "‡ TWO DEFINITIONS"),
        };

        public static void Run()
        {
            CheckExit.ArmLogFold();
            bool ok = true;
            foreach (string face in Faces)
            {
                var font = Resources.Load<Font>("Art/UI/Fonts/" + face);
                if (font == null) { Debug.LogError($"GLYPHS: the face {face} is not loadable from Resources/Art/UI/Fonts - the desk draws with it."); ok = false; continue; }
                foreach ((string glyph, string name) in Glyphs)
                {
                    bool has = font.HasCharacter(glyph[0]);
                    Debug.Log($"GLYPHS: {face,-24} {name,-20} {(has ? "present" : "ABSENT")}");
                    // Courier Prime is the face that draws all three (they are mono captions); a serif face without them is not a fault.
                    if (!has && face == "CourierPrime-Regular") { Debug.LogError($"GLYPHS: {face} has no {name} - D16 §3.6 draws it as a glyph; a primitive is needed instead, stated as a deviation."); ok = false; }
                }
            }
            Debug.Log(ok ? "GLYPHS: PASS - the mono face carries †, ◇ and ‡." : "GLYPHS: FAILED (see above).");
            CheckExit.Finish(ok ? 0 : 1);
        }
    }
}
