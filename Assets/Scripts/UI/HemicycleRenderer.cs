using System.Collections.Generic;
using System.Globalization;
using PoliSim.Data;
using PoliSim.Elections;
using UnityEngine;

namespace PoliSim.UI
{
    /// <summary>
    /// The chamber as seats: one dot per mandate on concentric half-rings, the ring count chosen for the
    /// width it is given so that every dot is distinct. P2-3.1 (Playtest 2, 2026-09-02): five fixed rows
    /// of ten-pixel dots had merged Sweden's mandates into curved bands - the rows are now found by
    /// capacity (the arc length each ring offers at a dot-and-a-third pitch) and the dots sized from the
    /// ring gap. Parties run left to right by bloc - the left bloc, then the unaffiliated, then the right
    /// bloc, from <see cref="NationalElection.BlocOf"/>, sourced for Sweden's 2022 blocs and unknown
    /// elsewhere - and within a bloc by mandates; the legend lists them in the same order with a header per
    /// bloc where blocs are known (the earlier order was each party's published CHES lrecon, W-G1). The
    /// dot count of the last Repaint is recorded (<see cref="LastDotsDrawn"/> against
    /// <see cref="LastChamberSeats"/>) so the screenshot driver holds it against the chamber. Point
    /// placement is the Policy Web's cos/sin on a circle swept across 180 degrees.
    /// </summary>
    public class HemicycleRenderer
    {
        private const int MinRows = 4;
        private const int MaxRows = 24;
        /// <summary>Dot diameter as a fraction of the ring gap - the rest is air between rings.</summary>
        private const float DotFill = 0.62f;
        /// <summary>Centre-to-centre pitch along a ring, in dot diameters.</summary>
        private const float DotPitch = 1.35f;
        private const float InnerRadiusFraction = 0.38f;
        /// <summary>The outer radius cap, in label font sizes - the arc grows with the type, not the sheet.</summary>
        private const float RadiusInFontSizes = 14f;

        /// <summary>Dots drawn on the last Repaint (the harness's tally).</summary>
        public static int LastDotsDrawn { get; private set; }
        /// <summary>The seats the drawn dictionary summed to on the last Repaint.</summary>
        public static int LastChamberSeats { get; private set; }
        /// <summary>The chamber the party system declares for that country, on the last Repaint.</summary>
        public static int LastDeclaredSeats { get; private set; }
        /// <summary>Rings used on the last Repaint.</summary>
        public static int LastRows { get; private set; }

        private Texture2D _dotTexture;

        /// <summary>Left bloc first, then the unaffiliated, then the right bloc.</summary>
        private static int BlocRank(int bloc) => bloc == 0 ? 0 : bloc < 0 ? 1 : 2;

        private static List<PoliticalParty> ByBlocThenMandates(CountryId country, IReadOnlyDictionary<string, int> seats)
        {
            var ordered = new List<PoliticalParty>(PartySystems.For(country));
            ordered.Sort((a, b) =>
            {
                int rankA = BlocRank(NationalElection.BlocOf(country, a.Abbrev));
                int rankB = BlocRank(NationalElection.BlocOf(country, b.Abbrev));
                if (rankA != rankB) { return rankA.CompareTo(rankB); }
                int seatsA = seats.TryGetValue(a.Abbrev, out int sa) ? sa : 0;
                int seatsB = seats.TryGetValue(b.Abbrev, out int sb) ? sb : 0;
                if (seatsA != seatsB) { return seatsB.CompareTo(seatsA); }
                return string.CompareOrdinal(a.Abbrev, b.Abbrev);
            });
            return ordered;
        }

        /// <summary>The height the arc reserves for a label style - half a disc at the capped radius plus a margin.</summary>
        private static float ArcHeight(GUIStyle labelStyle) => Mathf.Round(labelStyle.fontSize * RadiusInFontSizes) + 8f;

        public void Draw(string title, CountryId country, IReadOnlyDictionary<string, int> seats, GUIStyle labelStyle)
        {
            EnsureTexture();
            if (!string.IsNullOrEmpty(title)) { GUILayout.Label(title, labelStyle); }

            int totalSeats = 0;
            foreach (KeyValuePair<string, int> kvp in seats) { totalSeats += kvp.Value; }
            if (totalSeats <= 0)
            {
                GUILayout.Label("No data yet.", labelStyle);
                return;
            }

            List<PoliticalParty> order = ByBlocThenMandates(country, seats);
            var seatColors = new List<Color>(totalSeats);
            bool blocsKnown = false;
            var seatsByRank = new int[3];
            foreach (PoliticalParty party in order)
            {
                int count = seats.TryGetValue(party.Abbrev, out int s) ? s : 0;
                int bloc = NationalElection.BlocOf(country, party.Abbrev);
                blocsKnown |= bloc >= 0;
                seatsByRank[BlocRank(bloc)] += count;
                Color color = PoliSimTheme.PartyLaddered(country, party.Abbrev);
                for (int j = 0; j < count; j++) { seatColors.Add(color); }
            }

            Rect area = GUILayoutUtility.GetRect(10f, ArcHeight(labelStyle), GUILayout.ExpandWidth(true));
            if (Event.current.type == EventType.Repaint)
            {
                DrawArc(area, seatColors, labelStyle);
                LastDotsDrawn = seatColors.Count;
                LastChamberSeats = totalSeats;
                LastDeclaredSeats = PartySystems.ChamberSeats(country);
            }

            GUILayout.Space(4f);
            DrawLegend(country, seats, order, totalSeats, blocsKnown, seatsByRank, labelStyle);
        }

        /// <summary>
        /// The rings: the fewest (from four) whose combined arc length seats every mandate at the pitch,
        /// dots sized from the ring gap; seats per ring in proportion to its radius, the outermost taking
        /// the rounding remainder. Centred in the area, baseline at its foot.
        /// </summary>
        private void DrawArc(Rect area, IReadOnlyList<Color> seatColors, GUIStyle labelStyle)
        {
            int total = seatColors.Count;
            float outer = Mathf.Min(Mathf.Round(labelStyle.fontSize * RadiusInFontSizes), area.width * 0.5f - 2f);
            if (outer < 8f) { return; }
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
            LastRows = rows;

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

            var baseline = new Vector2(Mathf.Round(area.x + area.width * 0.5f), area.yMax - 4f);
            Color previousColor = GUI.color;
            int seat = 0;
            for (int r = 0; r < rows && seat < total; r++)
            {
                int rowSeats = Mathf.Min(perRow[r], total - seat);
                float radius = inner + r * gap;
                for (int i = 0; i < rowSeats; i++)
                {
                    float angle = rowSeats == 1 ? 90f : 180f - (180f / (rowSeats - 1)) * i;
                    Vector2 point = PointOnArc(baseline, radius, angle);
                    GUI.color = seatColors[seat];
                    GUI.DrawTexture(new Rect(point.x - dot * 0.5f, point.y - dot * 0.5f, dot, dot), _dotTexture);
                    seat++;
                }
            }
            GUI.color = previousColor;
        }

        private static void DrawLegend(CountryId country, IReadOnlyDictionary<string, int> seats, List<PoliticalParty> order,
            int totalSeats, bool blocsKnown, int[] seatsByRank, GUIStyle labelStyle)
        {
            GUIStyle caption = CaptionStyle(labelStyle);
            Color previousColor = GUI.color;
            bool anyUnsourced = false;
            int lastRank = -1;
            for (int i = 0; i < order.Count; i++)
            {
                PoliticalParty party = order[i];
                int count = seats.TryGetValue(party.Abbrev, out int s) ? s : 0;
                float percent = totalSeats > 0 ? count / (float)totalSeats * 100f : 0f;
                int bloc = NationalElection.BlocOf(country, party.Abbrev);
                int rank = BlocRank(bloc);
                if (blocsKnown && rank != lastRank)
                {
                    // A header per group: the bloc's name and its seats, from the same dictionary the dots read.
                    if (lastRank >= 0) { GUILayout.Space(4f); }
                    string header = string.Format(CultureInfo.InvariantCulture, "{0} · {1} SEATS",
                        NationalElection.BlocName(bloc).ToUpperInvariant(), seatsByRank[rank]);
                    GUILayout.Label(header, caption);
                    if (Event.current.type == EventType.Repaint)
                    {
                        Rect drawn = GUILayoutUtility.GetLastRect();
                        UiOverflowGuard.Check(header, caption.CalcSize(new GUIContent(header)), new Vector2(drawn.width, drawn.height), caption.fontSize);
                    }
                    lastRank = rank;
                }

                GUILayout.BeginHorizontal();
                float markerSize = labelStyle.fontSize;
                float rowHeight = LedgerRow.Height(labelStyle);
                Rect markerLane = GUILayoutUtility.GetRect(markerSize, rowHeight, GUILayout.ExpandWidth(false));
                var swatchRect = new Rect(markerLane.x, markerLane.y + (rowHeight - markerSize) * 0.5f, markerSize, markerSize);
                bool inkIsSourced = PoliSimTheme.HasPartyInk(country, party.Abbrev);
                anyUnsourced |= !inkIsSourced;
                GUI.color = PoliSimTheme.PartyLaddered(country, party.Abbrev);
                if (inkIsSourced)
                {
                    GUI.DrawTexture(swatchRect, Texture2D.whiteTexture);
                }
                else
                {
                    DrawHairlineBox(swatchRect);
                }
                GUI.color = previousColor;

                Texture2D emblem = IconLibrary.GetPartyMark(party.MarkName);
                if (emblem != null)
                {
                    Rect emblemLane = GUILayoutUtility.GetRect(markerSize, rowHeight, GUILayout.ExpandWidth(false));
                    GUI.DrawTexture(
                        new Rect(emblemLane.x, emblemLane.y + (rowHeight - markerSize) * 0.5f, markerSize, markerSize),
                        emblem, ScaleMode.ScaleToFit);
                }

                Rect rowRect = GUILayoutUtility.GetRect(10f, LedgerRow.Height(labelStyle), GUILayout.ExpandWidth(true));
                LedgerRow.DrawReadOnly(
                    rowRect,
                    party.Name,
                    totalSeats > 0 ? count / (float)totalSeats : -1f,
                    count.ToString(CultureInfo.InvariantCulture) + " seats",
                    percent.ToString("F0", CultureInfo.InvariantCulture) + "%",
                    PoliSimTheme.PartyLaddered(country, party.Abbrev),
                    labelStyle,
                    labelStyle);
                GUILayout.EndHorizontal();
            }

            if (anyUnsourced)
            {
                GUILayout.Space(2f);
                GUILayout.Label("Outlined swatch: no published colour for this party", caption);
            }
        }

        private static GUIStyle CaptionStyle(GUIStyle labelStyle)
        {
            var caption = new GUIStyle(labelStyle)
            {
                fontSize = Mathf.Max(9, Mathf.RoundToInt(labelStyle.fontSize * 0.72f)),
                wordWrap = false
            };
            caption.normal.textColor = PoliSimTheme.TextMuted;
            caption.hover.textColor = PoliSimTheme.TextMuted;
            caption.active.textColor = PoliSimTheme.TextMuted;
            caption.focused.textColor = PoliSimTheme.TextMuted;
            return caption;
        }

        private static void DrawHairlineBox(Rect r)
        {
            const float t = 1f;
            GUI.DrawTexture(new Rect(r.x, r.y, r.width, t), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(r.x, r.yMax - t, r.width, t), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(r.x, r.y, t, r.height), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(r.xMax - t, r.y, t, r.height), Texture2D.whiteTexture);
        }

        private static Vector2 PointOnArc(Vector2 baseline, float radius, float angleDegrees)
        {
            float rad = angleDegrees * Mathf.Deg2Rad;
            return baseline + new Vector2(Mathf.Cos(rad), -Mathf.Sin(rad)) * radius;
        }

        private void EnsureTexture()
        {
            if (_dotTexture != null) { return; }
            const int diameter = 32;
            _dotTexture = new Texture2D(diameter, diameter, TextureFormat.RGBA32, false) { hideFlags = HideFlags.HideAndDontSave };
            float radius = diameter / 2f;
            var pixels = new Color[diameter * diameter];
            for (int y = 0; y < diameter; y++)
            {
                for (int x = 0; x < diameter; x++)
                {
                    float dx = x + 0.5f - radius;
                    float dy = y + 0.5f - radius;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    // A one-pixel soft edge so a downscaled dot keeps a round rim instead of a jagged one.
                    pixels[y * diameter + x] = new Color(1f, 1f, 1f, Mathf.Clamp01(radius - d + 0.5f));
                }
            }
            _dotTexture.SetPixels(pixels);
            _dotTexture.Apply(false);
        }
    }
}
