using System.Collections.Generic;
using PoliSim.Data;
using UnityEngine;

namespace PoliSim.UI
{
    /// <summary>
    /// Political Systems Overhaul Part B PILOT (Master Sequence step 4): a small, generic hemicycle
    /// seat visualization - one dot per seat, arranged across a fixed number of concentric half-circle
    /// rows, reusing PolicyWebRenderer's own "point on a circle via cos/sin" node-placement math (see
    /// PointOnArc below) per the Master Roadmap's own explicit "same node-placement math as the Policy
    /// Web's circular layout" instruction, just swept across 180 degrees instead of the full 360.
    /// Seats-per-row is a simple radius-proportional approximation (more seats fit at a larger radius,
    /// same arc-length reasoning any real hemicycle packing uses), not a rigorous packing algorithm -
    /// good enough for a clearly-legible generic visualization, not a claim of exact real-world seating
    /// geometry. Parties are laid out left-to-right by descending fiscal stance (W-G1: DERIVED from
    /// each party's published CHES `lrecon`, replacing four hand-set archetype constants),
    /// matching the real-world convention of high-tax/left on the left, low-tax/right on the right.
    /// </summary>
    public class HemicycleRenderer
    {
        private const float AreaWidth = 340f;
        private const float AreaHeight = 190f;
        private const int RowCount = 5;
        private const float InnerRadius = 34f;
        private const float RowSpacing = 20f;
        private const float DotDiameter = 10f;

        private Texture2D _dotTexture;

        /// <summary>
        /// W-G1: the chamber's seat-holders, left to right by DESCENDING fiscal stance - which is
        /// ASCENDING CHES `lrecon` - so high-tax/left sits on the left and low-tax/right on the
        /// right, the convention the class comment already claimed while ordering four fictional
        /// archetypes by four hand-set constants.
        ///
        /// A party with no published position has no place on that axis, so it is placed at the
        /// right-hand END rather than in the middle. Putting an unmeasured party at the centre would
        /// draw it as centrist, which is a claim; putting it last draws it as "outside the measured
        /// order", which is what it is. France's UG bloc is 178 of 577 seats, so this is not a
        /// corner case there.
        /// </summary>
        private static List<PoliticalParty> LeftToRight(CountryId country)
        {
            var ordered = new List<PoliticalParty>(PartySystems.For(country));
            ordered.Sort((a, b) =>
            {
                if (a.HasPosition != b.HasPosition) { return a.HasPosition ? -1 : 1; }
                if (!a.HasPosition) { return string.CompareOrdinal(a.Abbrev, b.Abbrev); }
                int byPosition = a.LrEcon.CompareTo(b.LrEcon);
                return byPosition != 0 ? byPosition : string.CompareOrdinal(a.Abbrev, b.Abbrev);
            });
            return ordered;
        }

        public void Draw(string title, CountryId country, IReadOnlyDictionary<string, int> seats, GUIStyle labelStyle)
        {
            EnsureTexture();
            GUILayout.Label(title, labelStyle);

            int totalSeats = 0;
            foreach (KeyValuePair<string, int> kvp in seats)
            {
                totalSeats += kvp.Value;
            }

            if (totalSeats <= 0)
            {
                GUILayout.Label("No data yet.", labelStyle);
                return;
            }

            // Flatten into one ordered list of colors, one entry per seat, left to right.
            List<PoliticalParty> order = LeftToRight(country);
            var seatColors = new List<Color>(totalSeats);
            for (int i = 0; i < order.Count; i++)
            {
                PoliticalParty party = order[i];
                int count = seats.TryGetValue(party.Abbrev, out int s) ? s : 0;
                Color color = PoliSimTheme.Party(country, party.Abbrev);
                for (int j = 0; j < count; j++)
                {
                    seatColors.Add(color);
                }
            }

            Rect area = GUILayoutUtility.GetRect(AreaWidth, AreaHeight, GUILayout.ExpandWidth(false));
            Vector2 baseline = new Vector2(area.x + area.width * 0.5f, area.y + area.height - 8f);

            float[] radii = new float[RowCount];
            float radiusSum = 0f;
            for (int r = 0; r < RowCount; r++)
            {
                radii[r] = InnerRadius + r * RowSpacing;
                radiusSum += radii[r];
            }

            int[] seatsPerRow = new int[RowCount];
            int assigned = 0;
            for (int r = 0; r < RowCount; r++)
            {
                seatsPerRow[r] = Mathf.RoundToInt(totalSeats * (radii[r] / radiusSum));
                assigned += seatsPerRow[r];
            }
            seatsPerRow[RowCount - 1] += totalSeats - assigned;

            Color previousColor = GUI.color;
            int seatIndex = 0;
            for (int r = 0; r < RowCount && seatIndex < seatColors.Count; r++)
            {
                int rowSeats = Mathf.Min(seatsPerRow[r], seatColors.Count - seatIndex);
                for (int i = 0; i < rowSeats; i++)
                {
                    float angle = rowSeats == 1 ? 90f : 180f - (180f / (rowSeats - 1)) * i;
                    Vector2 point = PointOnArc(baseline, radii[r], angle);
                    Rect dotRect = new Rect(point.x - DotDiameter * 0.5f, point.y - DotDiameter * 0.5f, DotDiameter, DotDiameter);
                    GUI.color = seatColors[seatIndex];
                    GUI.DrawTexture(dotRect, _dotTexture);
                    seatIndex++;
                }
            }
            GUI.color = previousColor;

            GUILayout.Space(4f);
            for (int i = 0; i < order.Count; i++)
            {
                PoliticalParty party = order[i];
                int count = seats.TryGetValue(party.Abbrev, out int s) ? s : 0;
                float percent = totalSeats > 0 ? count / (float)totalSeats * 100f : 0f;

                GUILayout.BeginHorizontal();

                // ⚠ THE MARKER LANE TAKES THE ROW'S FULL HEIGHT, and the swatch is centred inside it.
                // Requesting a `fontSize`-tall rect left the swatch and emblem pinned to the TOP of a row
                // that is now two lines tall, while the name beside them sits vertically centred — which
                // read as a misalignment in the first capture after this conversion. The lane is sized to
                // the row; only the mark inside it is small.
                float markerSize = labelStyle.fontSize;
                float rowHeight = LedgerRow.Height(labelStyle);
                Rect markerLane = GUILayoutUtility.GetRect(markerSize, rowHeight, GUILayout.ExpandWidth(false));
                var swatchRect = new Rect(markerLane.x, markerLane.y + (rowHeight - markerSize) * 0.5f, markerSize, markerSize);

                // The party's own emblem, where a flat colour swatch used to be. These four sprites were
                // delivered and imported weeks ago and had never once been drawn - see IconLibrary.GetFlag
                // for the delivered-but-unreachable story they share with the country flags.
                //
                // **Worth noting for the v2.0 eleven-hue question**: this row now carries party identity as
                // a MARK as well as a colour, which is exactly the substitution the redesign has to decide
                // about. It is a live example rather than a proposal.
                //
                // ⚠ **THE SWATCH STAYS, AND THAT IS THE WHOLE POINT OF THIS BLOCK.** The emblem was first
                // drawn INSTEAD of the swatch, which looked better and broke something real: the legend's
                // colour is what keys each row to its own arc of seats in the chart above, and the emblems
                // are authored in their own palette (gold, red, blue) that has no relationship to
                // GetCategoricalColor's golden-angle hues. A legend whose colour does not match the chart
                // it explains is worse than no emblem. Caught in the screenshot, not in review.
                //
                // So: swatch first (correspondence preserved), emblem beside it (identity added).
                //
                // **This is a small live answer to v2.0's eleven-hue question** - a mark and a colour can
                // coexist and carry identity together. Whether the redesign keeps both, or drops the
                // colour and lets the mark carry it alone, is exactly the open decision; what this proves
                // is that dropping the colour is not free wherever a colour is also keying a chart.
                //
                // Untinted (the emblems are already coloured) and null-safe: a missing file simply leaves
                // the swatch, which is what this legend has always drawn.
                GUI.color = PoliSimTheme.Party(country, party.Abbrev);
                GUI.DrawTexture(swatchRect, Texture2D.whiteTexture);
                GUI.color = previousColor;

                // W-G1: a real party's MARK, not a fictional archetype's emblem. Null for 52 of the
                // 53 today - the legend falls back to the swatch, exactly as it always did for a
                // missing file, and `PartyMarkCoverageCheck` counts the gap rather than the folder.
                Texture2D emblem = IconLibrary.GetPartyMark(party.MarkName);
                if (emblem != null)
                {
                    Rect emblemLane = GUILayoutUtility.GetRect(markerSize, rowHeight, GUILayout.ExpandWidth(false));
                    GUI.DrawTexture(
                        new Rect(emblemLane.x, emblemLane.y + (rowHeight - markerSize) * 0.5f, markerSize, markerSize),
                        emblem, ScaleMode.ScaleToFit);
                }

                // ⚠ v2.0 CONVERSION, 2026-08-11 — the legend row becomes a READ-ONLY LEDGER ROW, and the
                // swatch's second job MOVES TO THE GAUGE rather than being dropped. The block above
                // spells out why that colour cannot simply go: it is what keys each row to its own arc in
                // the chart, and an emblem drawn INSTEAD of it broke that correspondence once already.
                //
                // `DrawReadOnly` draws its gauge in `barInk`, so passing the party's own hue keeps the
                // correspondence exactly — and improves on a swatch, because a bar in that hue is
                // PROPORTIONAL to the seat share as well as keyed to it. One mark carrying two readings
                // where a solid block carried one. The swatch stays as well, since it is what the emblem
                // is drawn over and what a missing emblem falls back to.
                //
                // Per Elias's ruling: seat COUNT is the figure, seat PERCENTAGE the trailing column — the
                // same split the Statistics sector rows took. D2 deleted the only competing occupant of
                // that column (the per-row VOTES figure), so nothing contends for it.
                Rect rowRect = GUILayoutUtility.GetRect(10f, LedgerRow.Height(labelStyle), GUILayout.ExpandWidth(true));
                LedgerRow.DrawReadOnly(
                    rowRect,
                    party.Name,
                    totalSeats > 0 ? count / (float)totalSeats : -1f,
                    count.ToString(System.Globalization.CultureInfo.InvariantCulture) + " seats",
                    percent.ToString("F0", System.Globalization.CultureInfo.InvariantCulture) + "%",
                    PoliSimTheme.Party(country, party.Abbrev),
                    labelStyle,
                    labelStyle);
                GUILayout.EndHorizontal();
            }
        }

        /// <summary>Point on a half-circle arc above <paramref name="baseline"/> - angleDegrees 180 = far left, 90 = top, 0 = far right. Same cos/sin-around-a-center math PolicyWebRenderer.PointOnCircle uses for its own full-circle node placement, just negated on Y (IMGUI's Y grows downward, so seats need to arc UPWARD from the baseline) and restricted to a 180-degree sweep instead of 360.</summary>
        private static Vector2 PointOnArc(Vector2 baseline, float radius, float angleDegrees)
        {
            float rad = angleDegrees * Mathf.Deg2Rad;
            return baseline + new Vector2(Mathf.Cos(rad), -Mathf.Sin(rad)) * radius;
        }

        private void EnsureTexture()
        {
            if (_dotTexture != null)
            {
                return;
            }

            const int diameter = 12;
            _dotTexture = new Texture2D(diameter, diameter, TextureFormat.RGBA32, false) { hideFlags = HideFlags.HideAndDontSave };
            float radius = diameter / 2f;
            var pixels = new Color[diameter * diameter];
            for (int y = 0; y < diameter; y++)
            {
                for (int x = 0; x < diameter; x++)
                {
                    float dx = x + 0.5f - radius;
                    float dy = y + 0.5f - radius;
                    pixels[y * diameter + x] = Mathf.Sqrt(dx * dx + dy * dy) <= radius ? Color.white : Color.clear;
                }
            }
            _dotTexture.SetPixels(pixels);
            _dotTexture.Apply(false);
        }
    }
}
