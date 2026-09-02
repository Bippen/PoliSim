using System.Collections.Generic;
using System.Globalization;
using PoliSim.Data;
using PoliSim.Simulation;
using UnityEngine;

namespace PoliSim.UI
{
    /// <summary>
    /// P2-2.2 (Playtest 2, 2026-09-02) — **parliamentary support as seats.** A compact hemicycle, one dot
    /// per mandate, each coloured FOR, AGAINST or UNDECIDED for the bill in front of it, laid out for on
    /// the left, undecided in the middle, against on the right, with the three counts beneath (the chamber total is the sum). It replaces
    /// the lean bar on every law-support preview: a bar said how far the seat-weighted alignment leaned;
    /// the map says which mandates lean, and how many. The sides come from
    /// <see cref="ParliamentSystem.SeatSides"/> — the same enumeration the seat-weighted alignment and the
    /// verdict read — so the counts equal the stance arithmetic to the seat by construction. The rings are
    /// the fewest whose arc length seats every mandate at a dot-and-a-third pitch (the first film's four
    /// fixed rings merged 349 seats into bands), and the arc is capped at a few font sizes of radius so a
    /// preview column keeps its other lines in a 720 frame.
    /// </summary>
    public static class SeatMapRenderer
    {
        private const int MinRows = 3;
        private const int MaxRows = 24;
        private const float DotFill = 0.62f;
        private const float DotPitch = 1.35f;
        private const float InnerRadiusFraction = 0.38f;
        private const float RadiusInFontSizes = 4.2f;

        private static float Radius(float width, GUIStyle captionStyle) =>
            Mathf.Min(width * 0.5f - 2f, Mathf.Round(captionStyle.fontSize * RadiusInFontSizes));

        private static float CaptionHeight(GUIStyle captionStyle) =>
            Mathf.Ceil(captionStyle.CalcSize(new GUIContent("FOR 0 · OF 0")).y);

        /// <summary>The height the map wants for a given width: the half-disc plus the measured caption line.</summary>
        public static float MeasureHeight(float width, GUIStyle captionStyle)
        {
            return Radius(width, captionStyle) + 1f + CaptionHeight(captionStyle) + 1f;
        }

        public static void Draw(Rect area, Country country, float direction, BillAxis axis, GUIStyle captionStyle)
            => Draw(area, country, BillConcern.FromLegacy(direction, axis), captionStyle);

        /// <summary>P3-A3 (2026-09-03): the map over what the bill CONCERNS (the stance model's enumeration), so a support preview and the vote that follows it colour the same seats the same way.</summary>
        public static void Draw(Rect area, Country country, BillConcern concern, GUIStyle captionStyle)
        {
            int forSeats = 0, againstSeats = 0, undecidedSeats = 0;
            foreach ((PoliticalParty _, int seats, int side, float _, bool _) in ParliamentSystem.SeatSides(country, concern))
            {
                if (side > 0) { forSeats += seats; } else if (side < 0) { againstSeats += seats; } else { undecidedSeats += seats; }
            }

            var seatInks = new List<Color>(forSeats + undecidedSeats + againstSeats);
            for (int i = 0; i < forSeats; i++) { seatInks.Add(PoliSimTheme.Good); }
            for (int i = 0; i < undecidedSeats; i++) { seatInks.Add(PoliSimTheme.TextMuted); }
            for (int i = 0; i < againstSeats; i++) { seatInks.Add(PoliSimTheme.Bad); }

            float captionHeight = CaptionHeight(captionStyle);
            var arc = new Rect(area.x, area.y, area.width, Mathf.Max(8f, area.height - captionHeight - 1f));
            if (Event.current.type == EventType.Repaint && seatInks.Count > 0)
            {
                DrawSeats(arc, seatInks, Radius(area.width, captionStyle));
            }

            string caption = string.Format(CultureInfo.InvariantCulture, "FOR {0} · UNDECIDED {1} · AGAINST {2}",
                forSeats, undecidedSeats, againstSeats);
            GUIStyle centred = new GUIStyle(captionStyle) { alignment = TextAnchor.MiddleCenter, wordWrap = false };
            var captionRect = new Rect(area.x, area.yMax - captionHeight, area.width, captionHeight);
            GUI.Label(captionRect, caption, centred);
            UiOverflowGuard.Check(caption, centred.CalcSize(new GUIContent(caption)), new Vector2(area.width, captionHeight), centred.fontSize);
        }

        /// <summary>
        /// The dots on concentric half-rings, left to right in list order: the fewest rings (from three)
        /// whose combined arc length seats every mandate at the pitch, dots sized from the ring gap, seats
        /// per ring in proportion to its radius with the outermost taking the rounding remainder.
        /// </summary>
        private static void DrawSeats(Rect arc, IReadOnlyList<Color> seatInks, float outer)
        {
            int total = seatInks.Count;
            outer = Mathf.Min(outer, arc.height - 2f);
            if (outer < 6f) { return; }
            float inner = outer * InnerRadiusFraction;
            int rows = MinRows;
            float gap, dot;
            while (true)
            {
                gap = (outer - inner) / (rows - 1);
                dot = gap * DotFill;
                int capacity = 0;
                for (int r = 0; r < rows; r++) { capacity += Mathf.FloorToInt(Mathf.PI * (inner + r * gap) / (dot * DotPitch)); }
                if (capacity >= total || rows >= MaxRows) { break; }
                rows++;
            }

            float radiusSum = 0f;
            for (int r = 0; r < rows; r++) { radiusSum += inner + r * gap; }
            var perRow = new int[rows];
            int assigned = 0;
            for (int r = 0; r < rows; r++)
            {
                perRow[r] = Mathf.RoundToInt(total * ((inner + r * gap) / radiusSum));
                assigned += perRow[r];
            }
            perRow[rows - 1] += total - assigned;

            var baseline = new Vector2(Mathf.Round(arc.x + arc.width * 0.5f), arc.yMax - 1f);
            Color previous = GUI.color;
            int seat = 0;
            for (int r = 0; r < rows && seat < total; r++)
            {
                int rowSeats = Mathf.Min(perRow[r], total - seat);
                float radius = inner + r * gap;
                for (int i = 0; i < rowSeats; i++)
                {
                    float angle = rowSeats == 1 ? 90f : 180f - (180f / (rowSeats - 1)) * i;
                    float rad = angle * Mathf.Deg2Rad;
                    Vector2 p = baseline + new Vector2(Mathf.Cos(rad), -Mathf.Sin(rad)) * radius;
                    GUI.color = seatInks[seat];
                    GUI.DrawTexture(new Rect(p.x - dot * 0.5f, p.y - dot * 0.5f, dot, dot), Texture2D.whiteTexture);
                    seat++;
                }
            }

            GUI.color = previous;
        }
    }
}
