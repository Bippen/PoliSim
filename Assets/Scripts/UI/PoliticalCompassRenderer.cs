using System.Collections.Generic;
using System.Globalization;
using PoliSim.Data;
using PoliSim.Elections;
using UnityEngine;

namespace PoliSim.UI
{
    /// <summary>
    /// The political compass on the CHES scales (P2-3.2, Playtest 2, 2026-09-02): X is <c>lrecon</c>
    /// (economic left … right), Y is <c>galtan</c> (liberal / GAL … conservative / TAN), both 0–10 with the
    /// endpoints the codebook gives, fixed - not auto-scaled to the observed spread, so a point's place is
    /// its value. Three kinds of point, every one from <see cref="CompassPositions"/>: the player's
    /// chamber's parties at their published pairs (small, in the neutral ink - D9 row 5: party ink never sits beside
    /// an area accent, and the countries are drawn in theirs - tagged by abbreviation), each
    /// country at the seat-weighted mean of its chamber (in the country's ink, the player's ringed), and the
    /// player's sitting cabinet at the seat-weighted mean of its members (a hollow ring in the country's
    /// ink, tagged). With <c>withLegend</c> the plot is joined by a column that names every point with its
    /// pair and the seats it rests on - the six chamber means sit within a unit of one another, so names on
    /// the plot would pile up; without it (the Desk card, the ladder) the plot carries the six dots, the
    /// player's ring and name, and the two axis captions. The axes' old policy-data blends live on in
    /// <see cref="PoliSim.Simulation.PolicyStanceAxes"/> for the Statistics comparison; they are no longer
    /// positions on this plot.
    /// </summary>
    public class PoliticalCompassRenderer
    {
        private const float DotDiameter = 14f;
        private const float PartyDotDiameter = 8f;
        private const float PlayerRingExtraDiameter = 8f;
        private const float CabinetRingDiameter = 22f;
        private const float CaptionGap = 6f;
        private const float CaptionLineGap = 2f;
        private const float LegendGap = 14f;
        private const float LegendShareOfWidth = 0.5f;
        // P2-3.3: the trail's ink alpha and dot, and the electorate's diamond.
        private const float TrailAlpha = 0.35f;
        private const float TrailDotDiameter = 5f;
        private const float ElectorateDiamond = 14f;

        private static readonly Color BackgroundColor = PoliSimTheme.Card;
        private static readonly Color GridColor = PoliSimTheme.Hairline;
        private static readonly Color AxisLabelColor = PoliSimTheme.TextMuted;
        private static readonly Color PlayerRingColor = PoliSimTheme.TextPrimary;

        private Texture2D _backgroundTexture;
        private Texture2D _circleTexture;
        private Texture2D _ringTexture;
        private Texture2D _lineTexture;
        private Texture2D _diamondTexture;

        private readonly struct LegendLine
        {
            public readonly string Text;
            public readonly Color? Swatch;
            public readonly bool Ring;
            public readonly bool Header;
            public LegendLine(string text, Color? swatch, bool ring, bool header) { Text = text; Swatch = swatch; Ring = ring; Header = header; }
        }

        private static (string X, string Y) CaptionTexts() =>
            ("X: economic left (0) to right (10) - CHES lrecon.",
             "Y: liberal / GAL (0) to conservative / TAN (10) - CHES galtan.");

        private static GUIStyle CaptionStyle(GUIStyle labelStyle, bool wrap)
        {
            var style = new GUIStyle(labelStyle) { fontSize = Mathf.Max(9, labelStyle.fontSize - 2), wordWrap = wrap, alignment = TextAnchor.UpperLeft };
            style.normal.textColor = AxisLabelColor;
            style.hover.textColor = AxisLabelColor;
            style.active.textColor = AxisLabelColor;
            style.focused.textColor = AxisLabelColor;
            return style;
        }

        private static float CaptionBandHeight((string X, string Y) captions, GUIStyle captionStyle, float width)
        {
            return CaptionGap
                   + captionStyle.CalcHeight(new GUIContent(captions.X), width)
                   + CaptionLineGap
                   + captionStyle.CalcHeight(new GUIContent(captions.Y), width);
        }

        private static string Pair(CompassPositions.Point p) => string.Format(CultureInfo.InvariantCulture, "{0:F1} · {1:F1}", p.LrEcon, p.Galtan);

        /// <summary>The legend column's lines: every point on the plot, named, with its pair and the seats it rests on.</summary>
        private static List<LegendLine> BuildLegend(IReadOnlyList<Country> countries, Country player)
        {
            var lines = new List<LegendLine> { new LegendLine("COUNTRIES · CHAMBER MEAN · lrecon · galtan · seats", null, false, true) };
            foreach (Country country in countries)
            {
                CompassPositions.Point? mean = CompassPositions.ChamberMean(country, out int leftOut);
                string tail = mean.HasValue
                    ? string.Format(CultureInfo.InvariantCulture, "{0} · {1} seats{2}", Pair(mean.Value), mean.Value.Seats, leftOut > 0 ? string.Format(CultureInfo.InvariantCulture, " ({0} publish no pair)", leftOut) : string.Empty)
                    : "no seated party publishes a pair";
                lines.Add(new LegendLine($"{country.Name}{(ReferenceEquals(country, player) ? " (ringed)" : string.Empty)}  {tail}", UiPalette.GetCountryColor(country.Id), false, false));
            }

            if (player != null)
            {
                lines.Add(new LegendLine($"{player.Name.ToUpperInvariant()}'S CABINET · SEAT-WEIGHTED MEAN", null, false, true));
                IReadOnlyList<string> cabinet = GovernmentFormation.Cabinet(player);
                CompassPositions.Point? cabinetMean = CompassPositions.CabinetMean(player, out int _);
                if (cabinet.Count == 0)
                {
                    lines.Add(new LegendLine("no government is formed from this chamber", null, false, false));
                }
                else
                {
                    string tail = cabinetMean.HasValue ? string.Format(CultureInfo.InvariantCulture, "{0} · {1} seats", Pair(cabinetMean.Value), cabinetMean.Value.Seats) : "no member publishes a pair";
                    lines.Add(new LegendLine($"{string.Join("+", cabinet)}  {tail}", UiPalette.GetCountryColor(player.Id), true, false));
                }

                lines.Add(new LegendLine($"{player.Name.ToUpperInvariant()}'S ELECTORATE · COMPATIBILITY-WEIGHTED MEAN", null, false, true));
                CompassPositions.Point? electorateMean = CompassPositions.ElectorateMean(player, out int electorateParties);
                lines.Add(electorateMean.HasValue
                    ? new LegendLine(string.Format(CultureInfo.InvariantCulture, "diamond  {0} · over {1} parties, the fitted electorate over the cohorts", Pair(electorateMean.Value), electorateParties), null, false, false)
                    : new LegendLine("no fitted electorate for this chamber - no point", null, false, false));
                lines.Add(new LegendLine("TRAILS · each chamber's mean at every turn close, faint", null, false, true));
                List<(System.DateTime Date, float LrEcon, float Galtan)> playerTrail = CompassPositions.Trail(player);
                lines.Add(new LegendLine(playerTrail.Count > 0
                    ? string.Format(CultureInfo.InvariantCulture, "{0} point(s) stored for {1}, {2:yyyy-MM-dd} to {3:yyyy-MM-dd}", playerTrail.Count, player.Name, playerTrail[0].Date, playerTrail[playerTrail.Count - 1].Date)
                    : $"no turn has closed yet - {player.Name}'s trail starts at the first close", null, false, false));
                lines.Add(new LegendLine($"{player.Name.ToUpperInvariant()}'S PARTIES · PUBLISHED PAIRS (CHES 2024)", null, false, true));
                foreach (PoliticalParty party in PartySystems.For(player.Id))
                {
                    CompassPositions.Point? own = CompassPositions.Party(party);
                    lines.Add(new LegendLine($"{party.Abbrev}  {(own.HasValue ? Pair(own.Value) : "no published pair")}", PoliSimTheme.TextSecondary, false, false));
                }
            }
            return lines;
        }

        private static float LegendLineHeight(LegendLine line, GUIStyle labelStyle, GUIStyle captionStyle) =>
            Mathf.Ceil((line.Header ? captionStyle : labelStyle).CalcSize(new GUIContent(line.Text)).y) + 2f;

        private static float LegendWidthNeed(List<LegendLine> lines, GUIStyle labelStyle, GUIStyle captionStyle)
        {
            float need = 0f;
            foreach (LegendLine line in lines)
            {
                float text = (line.Header ? captionStyle : labelStyle).CalcSize(new GUIContent(line.Text)).x;
                need = Mathf.Max(need, text + (line.Swatch.HasValue ? labelStyle.fontSize + 6f : 0f));
            }
            return Mathf.Ceil(need);
        }

        private static float LegendHeight(List<LegendLine> lines, GUIStyle labelStyle, GUIStyle captionStyle)
        {
            float height = 0f;
            for (int i = 0; i < lines.Count; i++)
            {
                if (lines[i].Header && i > 0) { height += captionStyle.fontSize * 0.6f; }
                height += LegendLineHeight(lines[i], labelStyle, captionStyle);
            }
            return height;
        }

        private static Country Find(IReadOnlyList<Country> countries, CountryId id)
        {
            foreach (Country c in countries) { if (c.Id == id) { return c; } }
            return null;
        }

        /// <summary>
        /// The footprint: without a legend, the plot square plus the caption band at the width the axis
        /// captions need (capped at the available width); with one, the whole available width, and the
        /// taller of the plot-with-captions and the legend column.
        /// </summary>
        public Vector2 Footprint(IReadOnlyList<Country> countries, float plotSize, float availableWidth, GUIStyle labelStyle, CountryId playerCountryId, bool withLegend)
        {
            (string X, string Y) captions = CaptionTexts();
            GUIStyle flat = CaptionStyle(labelStyle, wrap: false);
            GUIStyle wrapped = CaptionStyle(labelStyle, wrap: true);
            if (!withLegend)
            {
                float need = Mathf.Max(flat.CalcSize(new GUIContent(captions.X)).x, flat.CalcSize(new GUIContent(captions.Y)).x);
                float width = Mathf.Min(Mathf.Max(availableWidth, 1f), Mathf.Max(plotSize, need));
                return new Vector2(width, plotSize + CaptionBandHeight(captions, wrapped, width));
            }

            List<LegendLine> lines = BuildLegend(countries, Find(countries, playerCountryId));
            float legendHeight = LegendHeight(lines, labelStyle, flat);
            float plotWidth = Mathf.Max(1f, availableWidth - Mathf.Min(LegendWidthNeed(lines, labelStyle, flat), availableWidth * LegendShareOfWidth) - LegendGap);
            float side = Mathf.Min(plotSize, plotWidth);
            return new Vector2(Mathf.Max(availableWidth, 1f), Mathf.Max(side + CaptionBandHeight(captions, wrapped, side), legendHeight));
        }

        public void Draw(Rect rect, IReadOnlyList<Country> countries, CountryId playerCountryId, GUIStyle labelStyle, bool withLegend)
        {
            EnsureTexturesInitialized();
            GUI.DrawTexture(rect, _backgroundTexture, ScaleMode.StretchToFill);

            (string X, string Y) captions = CaptionTexts();
            GUIStyle captionStyle = CaptionStyle(labelStyle, wrap: true);
            GUIStyle tagStyle = CaptionStyle(labelStyle, wrap: false);
            Country player = Find(countries, playerCountryId);

            // The plot square: the rect less the legend column (if any) and the caption band beneath.
            List<LegendLine> lines = withLegend ? BuildLegend(countries, player) : null;
            float legendWidth = withLegend ? Mathf.Min(LegendWidthNeed(lines, labelStyle, tagStyle), rect.width * LegendShareOfWidth) : 0f;
            float plotWidthAvailable = withLegend ? rect.width - legendWidth - LegendGap : rect.width;
            float bandHeight = CaptionBandHeight(captions, captionStyle, Mathf.Max(1f, plotWidthAvailable));
            float plotSide = Mathf.Max(1f, Mathf.Min(plotWidthAvailable, rect.height - bandHeight));
            var plotSquare = new Rect(rect.x, rect.y, plotSide, plotSide);

            // The margin holds the end words (full mode) or a little air (compact).
            float margin = withLegend
                ? Mathf.Max(labelStyle.fontSize * 1.5f, tagStyle.CalcSize(new GUIContent("RIGHT")).x + 4f)
                : Mathf.Max(4f, DotDiameter * 0.5f + 2f);
            var plotRect = new Rect(plotSquare.x + margin, plotSquare.y + margin, plotSquare.width - margin * 2f, plotSquare.height - margin * 2f);

            DrawGridlines(plotRect);
            if (withLegend) { DrawEndWords(plotSquare, plotRect, tagStyle); }

            // P2-3.3 (2026-09-02): each country's trail - its chamber mean at every turn close, faint, in the
            // country's ink, drawn as the stored list is stored (CompassPositions.Trail) - and, for the player's
            // chamber, the electorate at the compatibility-weighted mean of the parties election night predicts from.
            foreach (Country country in countries)
            {
                List<(System.DateTime Date, float LrEcon, float Galtan)> trail = CompassPositions.Trail(country);
                if (trail.Count < 2) { continue; }
                Color faint = UiPalette.GetCountryColor(country.Id);
                faint.a = TrailAlpha;
                Vector2 previousPoint = ToPlotPixel(plotRect, trail[0].LrEcon, trail[0].Galtan);
                for (int i = 1; i < trail.Count; i++)
                {
                    Vector2 next = ToPlotPixel(plotRect, trail[i].LrEcon, trail[i].Galtan);
                    DrawLineSegment(previousPoint, next, 1f, faint);
                    DrawCircle(new Rect(previousPoint.x - TrailDotDiameter * 0.5f, previousPoint.y - TrailDotDiameter * 0.5f, TrailDotDiameter, TrailDotDiameter), _circleTexture, faint);
                    previousPoint = next;
                }
            }
            if (withLegend && player != null)
            {
                CompassPositions.Point? electorate = CompassPositions.ElectorateMean(player, out int _);
                if (electorate.HasValue)
                {
                    Vector2 pixel = ToPlotPixel(plotRect, electorate.Value.LrEcon, electorate.Value.Galtan);
                    DrawCircle(new Rect(pixel.x - ElectorateDiamond * 0.5f, pixel.y - ElectorateDiamond * 0.5f, ElectorateDiamond, ElectorateDiamond), _diamondTexture, PoliSimTheme.TextPrimary);
                    const string tag = "electorate";
                    Vector2 tagSize = tagStyle.CalcSize(new GUIContent(tag));
                    float tagX = Mathf.Clamp(pixel.x - tagSize.x * 0.5f, plotRect.x, plotRect.xMax - tagSize.x);
                    float tagY = Mathf.Clamp(pixel.y - ElectorateDiamond * 0.5f - tagSize.y, plotRect.y, plotRect.yMax - tagSize.y);
                    GUI.Label(new Rect(tagX, tagY, tagSize.x, tagSize.y), tag, tagStyle);
                }
            }

            if (withLegend && player != null)
            {
                // 1. The player's parties, small, in the neutral ink (D9 row 5), tagged by abbreviation.
                foreach (PoliticalParty party in PartySystems.For(player.Id))
                {
                    CompassPositions.Point? p = CompassPositions.Party(party);
                    if (!p.HasValue) { continue; }
                    Vector2 pixel = ToPlotPixel(plotRect, p.Value.LrEcon, p.Value.Galtan);
                    DrawCircle(new Rect(pixel.x - PartyDotDiameter * 0.5f, pixel.y - PartyDotDiameter * 0.5f, PartyDotDiameter, PartyDotDiameter),
                        _circleTexture, PoliSimTheme.TextSecondary);
                    Vector2 tagSize = tagStyle.CalcSize(new GUIContent(party.Abbrev));
                    float tagX = Mathf.Clamp(pixel.x + PartyDotDiameter * 0.5f + 2f, plotRect.x, plotRect.xMax - tagSize.x);
                    float tagY = Mathf.Clamp(pixel.y - tagSize.y * 0.5f, plotRect.y, plotRect.yMax - tagSize.y);
                    GUI.Label(new Rect(tagX, tagY, tagSize.x, tagSize.y), party.Abbrev, tagStyle);
                }

                // 2. The sitting cabinet, a hollow ring in the country's ink.
                CompassPositions.Point? cabinet = CompassPositions.CabinetMean(player, out int _);
                if (cabinet.HasValue)
                {
                    Vector2 pixel = ToPlotPixel(plotRect, cabinet.Value.LrEcon, cabinet.Value.Galtan);
                    DrawCircle(new Rect(pixel.x - CabinetRingDiameter * 0.5f, pixel.y - CabinetRingDiameter * 0.5f, CabinetRingDiameter, CabinetRingDiameter),
                        _ringTexture, UiPalette.GetCountryColor(player.Id));
                    const string tag = "cabinet";
                    Vector2 tagSize = tagStyle.CalcSize(new GUIContent(tag));
                    float tagX = Mathf.Clamp(pixel.x - tagSize.x * 0.5f, plotRect.x, plotRect.xMax - tagSize.x);
                    float tagY = Mathf.Clamp(pixel.y + CabinetRingDiameter * 0.5f + 1f, plotRect.y, plotRect.yMax - tagSize.y);
                    GUI.Label(new Rect(tagX, tagY, tagSize.x, tagSize.y), tag, tagStyle);
                }
            }

            // 3. Each country at its chamber's seat-weighted mean, the player's ringed.
            Vector2? playerPixel = null;
            foreach (Country country in countries)
            {
                CompassPositions.Point? mean = CompassPositions.ChamberMean(country, out int _);
                if (!mean.HasValue) { continue; }
                Vector2 point = ToPlotPixel(plotRect, mean.Value.LrEcon, mean.Value.Galtan);
                if (country.Id == playerCountryId)
                {
                    playerPixel = point;
                    float ringDiameter = DotDiameter + PlayerRingExtraDiameter;
                    DrawCircle(new Rect(point.x - ringDiameter * 0.5f, point.y - ringDiameter * 0.5f, ringDiameter, ringDiameter), _ringTexture, PlayerRingColor);
                }
                DrawCircle(new Rect(point.x - DotDiameter * 0.5f, point.y - DotDiameter * 0.5f, DotDiameter, DotDiameter), _circleTexture, UiPalette.GetCountryColor(country.Id));
            }

            // Compact: the player's name beside its ring - the one label the card has room for.
            if (!withLegend && player != null && playerPixel.HasValue)
            {
                Vector2 labelSize = labelStyle.CalcSize(new GUIContent(player.Name));
                float half = (DotDiameter + PlayerRingExtraDiameter) * 0.5f;
                bool placeLeft = playerPixel.Value.x + half + 3f + labelSize.x > plotRect.xMax;
                float labelX = placeLeft ? playerPixel.Value.x - half - 3f - labelSize.x : playerPixel.Value.x + half + 3f;
                labelX = Mathf.Clamp(labelX, plotRect.x, plotRect.xMax - labelSize.x);
                float labelY = Mathf.Clamp(playerPixel.Value.y - labelSize.y * 0.5f, plotRect.y, plotRect.yMax - labelSize.y);
                GUI.Label(new Rect(labelX, labelY, labelSize.x, labelSize.y), player.Name, labelStyle);
            }

            // The captions wrap at the width the footprint measured them at: the plot's beside a legend, the rect's without one.
            float captionWidth = withLegend ? plotSide : rect.width;
            float xHeight = captionStyle.CalcHeight(new GUIContent(captions.X), captionWidth);
            float yHeight = captionStyle.CalcHeight(new GUIContent(captions.Y), captionWidth);
            var xRect = new Rect(rect.x, plotSquare.yMax + CaptionGap, captionWidth, xHeight);
            var yRect = new Rect(rect.x, xRect.yMax + CaptionLineGap, captionWidth, yHeight);
            GUI.Label(xRect, captions.X, captionStyle);
            GUI.Label(yRect, captions.Y, captionStyle);
            UiContainmentGuard.Check("Compass plot", plotSquare, rect);
            UiContainmentGuard.Check("Compass caption X", xRect, rect);
            UiContainmentGuard.Check("Compass caption Y", yRect, rect);

            if (withLegend)
            {
                DrawLegend(new Rect(plotSquare.xMax + LegendGap, rect.y, rect.xMax - plotSquare.xMax - LegendGap, rect.height), lines, labelStyle, tagStyle, rect);
            }
        }

        private void DrawLegend(Rect column, List<LegendLine> lines, GUIStyle labelStyle, GUIStyle captionStyle, Rect container)
        {
            float y = column.y;
            Color previous = GUI.color;
            for (int i = 0; i < lines.Count; i++)
            {
                LegendLine line = lines[i];
                GUIStyle style = line.Header ? captionStyle : labelStyle;
                if (line.Header && i > 0) { y += captionStyle.fontSize * 0.6f; }
                float height = LegendLineHeight(line, labelStyle, captionStyle);
                float x = column.x;
                if (line.Swatch.HasValue)
                {
                    float size = labelStyle.fontSize;
                    var swatch = new Rect(x, y + (height - 2f - size) * 0.5f, size, size);
                    DrawCircle(swatch, line.Ring ? _ringTexture : _circleTexture, line.Swatch.Value);
                    x += size + 6f;
                }
                var textRect = new Rect(x, y, column.xMax - x, height - 2f);
                GUI.Label(textRect, line.Text, style);
                UiOverflowGuard.Check(line.Text, style.CalcSize(new GUIContent(line.Text)), new Vector2(textRect.width, textRect.height), style.fontSize);
                UiContainmentGuard.Check("Compass legend line", textRect, container);
                y += height;
            }
            GUI.color = previous;
        }

        /// <summary>The conventional words at the plot's four edges, in the margin the plot square keeps around the plot.</summary>
        private static void DrawEndWords(Rect plotSquare, Rect plotRect, GUIStyle tagStyle)
        {
            GUIStyle centred = new GUIStyle(tagStyle) { alignment = TextAnchor.MiddleCenter };
            float line = centred.CalcSize(new GUIContent("LIBERAL")).y;
            GUI.Label(new Rect(plotRect.x, plotSquare.y + (plotRect.y - plotSquare.y - line) * 0.5f, plotRect.width, line), "LIBERAL (GAL)", centred);
            GUI.Label(new Rect(plotRect.x, plotRect.yMax + (plotSquare.yMax - plotRect.yMax - line) * 0.5f, plotRect.width, line), "CONSERVATIVE (TAN)", centred);
            GUIStyle left = new GUIStyle(tagStyle) { alignment = TextAnchor.MiddleRight };
            GUIStyle right = new GUIStyle(tagStyle) { alignment = TextAnchor.MiddleLeft };
            GUI.Label(new Rect(plotSquare.x, plotRect.y, plotRect.x - plotSquare.x - 3f, plotRect.height), "LEFT", left);
            GUI.Label(new Rect(plotRect.xMax + 3f, plotRect.y, plotSquare.xMax - plotRect.xMax - 3f, plotRect.height), "RIGHT", right);
        }

        /// <summary>The fixed 0–10 scales: x left to right, y with GAL (0) at the top and TAN (10) at the foot.</summary>
        private static Vector2 ToPlotPixel(Rect plotRect, float lrEcon, float galtan)
        {
            float tx = Mathf.InverseLerp(CompassPositions.ScaleMin, CompassPositions.ScaleMax, lrEcon);
            float ty = Mathf.InverseLerp(CompassPositions.ScaleMin, CompassPositions.ScaleMax, galtan);
            return new Vector2(plotRect.x + tx * plotRect.width, plotRect.y + ty * plotRect.height);
        }

        private void DrawGridlines(Rect plotRect)
        {
            for (int i = 0; i <= 4; i++)
            {
                float t = i * 0.25f;
                Color color = i == 2 ? Color.Lerp(GridColor, PoliSimTheme.TextMuted, 0.35f) : GridColor;
                float gx = plotRect.x + t * plotRect.width;
                DrawLineSegment(new Vector2(gx, plotRect.y), new Vector2(gx, plotRect.y + plotRect.height), 1f, color);
                float gy = plotRect.y + t * plotRect.height;
                DrawLineSegment(new Vector2(plotRect.x, gy), new Vector2(plotRect.x + plotRect.width, gy), 1f, color);
            }
        }

        private static void DrawCircle(Rect rect, Texture2D texture, Color color)
        {
            Color previous = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, texture, ScaleMode.StretchToFill);
            GUI.color = previous;
        }

        private void DrawLineSegment(Vector2 from, Vector2 to, float thickness, Color color)
        {
            Vector2 delta = to - from;
            float length = delta.magnitude;
            if (length < 1f) return;
            float angleDegrees = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
            Matrix4x4 previousMatrix = GUI.matrix;
            Color previousColor = GUI.color;
            GUIUtility.RotateAroundPivot(angleDegrees, from);
            GUI.color = color;
            GUI.DrawTexture(new Rect(from.x, from.y - thickness * 0.5f, length, thickness), _lineTexture, ScaleMode.StretchToFill);
            GUI.matrix = previousMatrix;
            GUI.color = previousColor;
        }

        private void EnsureTexturesInitialized()
        {
            if (_backgroundTexture == null)
            {
                _backgroundTexture = new Texture2D(2, 2, TextureFormat.RGBA32, false) { hideFlags = HideFlags.HideAndDontSave };
                _backgroundTexture.SetPixels(new[] { BackgroundColor, BackgroundColor, BackgroundColor, BackgroundColor });
                _backgroundTexture.Apply(false);
            }
            if (_circleTexture == null) { _circleTexture = BuildCircleTexture(16, filled: true); }
            if (_ringTexture == null) { _ringTexture = BuildCircleTexture(24, filled: false); }
            if (_diamondTexture == null) { _diamondTexture = BuildDiamondTexture(24); }
            if (_lineTexture == null)
            {
                _lineTexture = new Texture2D(1, 1, TextureFormat.RGBA32, false) { hideFlags = HideFlags.HideAndDontSave };
                _lineTexture.SetPixel(0, 0, Color.white);
                _lineTexture.Apply(false);
            }
        }


        /// <summary>P2-3.3: a filled diamond for the electorate's point - a shape no other point uses.</summary>
        private static Texture2D BuildDiamondTexture(int size)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { hideFlags = HideFlags.HideAndDontSave };
            float half = size / 2f;
            var pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = Mathf.Abs(x + 0.5f - half);
                    float dy = Mathf.Abs(y + 0.5f - half);
                    pixels[y * size + x] = dx + dy <= half ? Color.white : Color.clear;
                }
            }
            texture.SetPixels(pixels);
            texture.Apply(false);
            return texture;
        }
        private static Texture2D BuildCircleTexture(int diameter, bool filled)
        {
            var texture = new Texture2D(diameter, diameter, TextureFormat.RGBA32, false) { hideFlags = HideFlags.HideAndDontSave };
            float radius = diameter / 2f;
            float innerRadius = radius - 2f;
            var pixels = new Color[diameter * diameter];
            for (int y = 0; y < diameter; y++)
            {
                for (int x = 0; x < diameter; x++)
                {
                    float dx = x + 0.5f - radius;
                    float dy = y + 0.5f - radius;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    bool inside = filled ? dist <= radius : (dist <= radius && dist >= innerRadius);
                    pixels[y * diameter + x] = inside ? Color.white : Color.clear;
                }
            }
            texture.SetPixels(pixels);
            texture.Apply(false);
            return texture;
        }
    }
}
