using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using PoliSim.UI;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// P2-1.2 (Playtest 2, 2026-09-02) — **every ink pair at its real use, against the floor its use sets.**
    ///
    /// <para><b>The class, twice.</b> D6 (2026-08-28) darkened the faint inks toward 4.5:1 and re-measured
    /// by hand; two targets were missed and nobody noticed until Elias read the sheet at a sitting. A
    /// contrast figure is a DERIVED fact about two tokens, and a derived fact that lives only in a
    /// record goes stale the day a token moves. This check derives it from the theme on every run
    /// (WCAG 2.x relative luminance, the same arithmetic the D6 table was checked with), so the ratio
    /// can never disagree with the tokens.</para>
    ///
    /// <para><b>The enumeration</b> (rule 14). The pairs below are the inks the UI draws as TEXT, on the
    /// grounds they are drawn on, each with the floor the row set for its use — <b>body</b> text (the
    /// `DeskBody` and `DeskNumeral` ladders, `_labelStyle`, ledger names and headers) must clear 4.5,
    /// <b>caption</b> text (the `DeskCaption` ladders, chip captions, plate captions) 3.5. A ground an ink
    /// is never drawn on is not listed: the table is a census of uses, not a matrix, and adding a pair
    /// means adding a use. The area inks are listed at body floor on the paper because `DrawColoredLabel`
    /// and the ledger rows set them in body sizes; their FILL uses are not text and are not judged here.</para>
    ///
    /// <para><b>Directions.</b> Every floor is a floor; a ratio may only rise. The table is printed every
    /// run so the record can point at it instead of copying it.</para>
    /// </summary>
    public static class InkContrastCheck
    {
        private const float BodyFloor = 4.5f;
        private const float CaptionFloor = 3.5f;

        private readonly struct Pair
        {
            public readonly string Ink;
            public readonly Color InkColor;
            public readonly string Ground;
            public readonly Color GroundColor;
            public readonly float Floor;
            public readonly string Use;

            public Pair(string ink, Color inkColor, string ground, Color groundColor, float floor, string use)
            {
                Ink = ink; InkColor = inkColor; Ground = ground; GroundColor = groundColor; Floor = floor; Use = use;
            }
        }

        private static IEnumerable<Pair> Pairs()
        {
            // The paper grounds text is set on: the sheet (Card), the plate inset, the tile, the modal.
            (string Name, Color Color)[] papers =
            {
                ("Card", PoliSimTheme.Card), ("CardInset", PoliSimTheme.CardInset), ("Tile", PoliSimTheme.Tile), ("ModalBackground", PoliSimTheme.ModalBackground),
            };

            // Body inks on every paper ground (DeskBody 9-13, DeskNumeral, _labelStyle, ledger figures).
            foreach ((string ground, Color g) in papers)
            {
                yield return new Pair("TextPrimary", PoliSimTheme.TextPrimary, ground, g, BodyFloor, "body");
                yield return new Pair("TextSecondary", PoliSimTheme.TextSecondary, ground, g, BodyFloor, "body");
                yield return new Pair("TextMuted", PoliSimTheme.TextMuted, ground, g, BodyFloor, "body (DeskBody 12, the impact notes)");
                yield return new Pair("Bad", PoliSimTheme.Bad, ground, g, BodyFloor, "body (DeskNumeral 17, deltas in _labelStyle)");
                yield return new Pair("Good", PoliSimTheme.Good, ground, g, BodyFloor, "body (deltas in _labelStyle)");
                // The delta inks GetDeltaColor prints with - bound to the verdict tokens at P2-1.2, listed so a
                // future copy cannot slip below the floor unseen (the pre-D6 green did exactly that).
                yield return new Pair("PositiveChange", UiPalette.PositiveChangeColor, ground, g, BodyFloor, "body (every delta GetDeltaColor colours)");
                yield return new Pair("NegativeChange", UiPalette.NegativeChangeColor, ground, g, BodyFloor, "body (every delta GetDeltaColor colours)");
                yield return new Pair("NeutralChange", UiPalette.NeutralChangeColor, ground, g, BodyFloor, "body (a zero delta)");
                // Caption inks on every paper ground (DeskCaption 7.5-11, chip and plate captions).
                yield return new Pair("Caution", PoliSimTheme.Caution, ground, g, CaptionFloor, "caption (DeskCaption 8.5-9: BREAKING, thresholds)");
                yield return new Pair("Neutral", PoliSimTheme.Neutral, ground, g, CaptionFloor, "caption (DeskCaption 8: zero deltas)");
            }

            // The area inks as text: section headers (DrawColoredLabel, _headerStyle), ledger names and
            // the DeskBody/DeskCaption ladders that take an area ink - body sizes on the sheet.
            foreach (UiPalette.SystemArea area in Enum.GetValues(typeof(UiPalette.SystemArea)))
            {
                yield return new Pair("Area." + area, UiPalette.GetAreaColor(area), "Card", PoliSimTheme.Card, BodyFloor, "body (headers, ledger names)");
            }

            // The desk register and the chips.
            yield return new Pair("TextOnDesk", PoliSimTheme.TextOnDesk, "Desk", PoliSimTheme.Desk, BodyFloor, "body (the hold banner, desk labels)");
            yield return new Pair("TextPrimary", PoliSimTheme.TextPrimary, "Brass", PoliSimTheme.Brass, CaptionFloor, "caption (the selected chip, D6's assignment flip)");
            yield return new Pair("InkOnStock", PoliSimTheme.InkOnStock, "StockOff", PoliSimTheme.StockOff, CaptionFloor, "caption (the unselected chip)");
        }

        public static void Run()
        {
            CheckExit.ArmLogFold();
            var sb = new StringBuilder();
            sb.Append("=== P2-1.2: ink contrast at its real use - WCAG ratio derived from the theme tokens ===\n");
            sb.Append(F("    body floor {0:0.0}, caption floor {1:0.0}; a ratio may only rise\n\n", BodyFloor, CaptionFloor));
            sb.Append("    ink                     ground            ratio   floor  use\n");
            int failures = 0;
            int pairs = 0;
            foreach (Pair p in Pairs())
            {
                pairs++;
                float ratio = Contrast(p.InkColor, p.GroundColor);
                bool ok = ratio >= p.Floor;
                if (!ok) { failures++; }
                sb.Append(F("    {0} {1,-23} {2,-16} {3,6:0.00}  {4,5:0.0}  {5}\n", ok ? "ok  " : "FAIL", p.Ink + " " + Hex(p.InkColor), p.Ground, ratio, p.Floor, p.Use));
            }

            sb.Append(F("\n=== P2-1.2: {0} pair(s), {1} below floor ===\n", pairs, failures));
            if (failures == 0)
            {
                Debug.Log(sb.ToString());
                CheckExit.Finish(0);
                return;
            }

            Debug.LogError(sb.ToString() + "INK CONTRAST: a pair sits below the floor its use sets. Raise the ink (hue held) or, if no value of this hue clears it, send the pair to Design - never lower the floor.");
            CheckExit.Finish(1);
        }

        /// <summary>WCAG 2.x contrast ratio between two opaque sRGB colours.</summary>
        public static float Contrast(Color a, Color b)
        {
            float la = Luminance(a);
            float lb = Luminance(b);
            return la >= lb ? (la + 0.05f) / (lb + 0.05f) : (lb + 0.05f) / (la + 0.05f);
        }

        private static float Luminance(Color c)
        {
            return 0.2126f * Linear(c.r) + 0.7152f * Linear(c.g) + 0.0722f * Linear(c.b);
        }

        private static float Linear(float channel)
        {
            return channel <= 0.03928f ? channel / 12.92f : Mathf.Pow((channel + 0.055f) / 1.055f, 2.4f);
        }

        private static string Hex(Color c) =>
            "#" + ((int)Mathf.Round(c.r * 255f)).ToString("X2") + ((int)Mathf.Round(c.g * 255f)).ToString("X2") + ((int)Mathf.Round(c.b * 255f)).ToString("X2");

        private static string F(string format, params object[] args) => string.Format(CultureInfo.InvariantCulture, format, args);
    }
}
