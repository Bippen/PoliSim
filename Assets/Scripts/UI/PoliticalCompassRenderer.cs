using System.Collections.Generic;
using PoliSim.Data;
using UnityEngine;

namespace PoliSim.UI
{
    /// <summary>
    /// Political Systems Overhaul Part C: a 2D scatter plot, one dot per country, grounded entirely
    /// in this game's OWN real, already-tracked policy data - not invented ideology labels. Reuses
    /// the same hand-drawn Texture2D circle/line technique MapRenderer/PolicyWebRenderer already
    /// established (BuildCircleTexture, rotated-stretched-rect line segments), procedurally drawn
    /// per Master Roadmap working-discipline rule 10.
    ///
    /// X axis ("fiscal size"): average implemented TaxLine.Rate blended with total government
    /// spending as a percent of GDP (whichever mechanism the country actually uses - detailed
    /// SpendingLines if present, else the legacy GovernmentSpendingRate baseline) - higher means
    /// more tax collected and more spent, a bigger fiscal footprint either way.
    /// Y axis ("regulatory/social intervention"): average Sector.RegulationLevel blended with
    /// average implemented WelfareProgram.GenerosityLevel - higher means more regulated markets and
    /// a more generous welfare state.
    /// Both axes land on the SAME already-existing 0-100 dial scale every policy lever in this game
    /// already uses (50 = neutral), so no new normalization scheme was invented for this widget.
    /// </summary>
    public class PoliticalCompassRenderer
    {
        private const float MinAxisValue = 0f;
        private const float MaxAxisValue = 100f;
        private const float DotDiameter = 14f;
        private const float PlayerRingExtraDiameter = 8f;

        /// <summary>Paper, not near-black - the FIFTH renderer carrying this literal, found by sweeping rather than by looking at the screen it breaks.</summary>
        private static readonly Color BackgroundColor = PoliSimTheme.Card;
        private static readonly Color GridColor = PoliSimTheme.Hairline;
        private static readonly Color AxisLabelColor = new Color(0.65f, 0.65f, 0.65f, 1f);
        private static readonly Color PlayerRingColor = Color.white;

        private Texture2D _backgroundTexture;
        private Texture2D _circleTexture;
        private Texture2D _ringTexture;
        private Texture2D _lineTexture;

        /// <summary>This country's X-axis position (0-100): the average of its implemented tax rates and its total government spending as a percent of GDP, whichever mechanism (detailed SpendingLines or the legacy GovernmentSpendingRate baseline) it actually uses.</summary>
        public static float GetFiscalSizeAxisValue(Country country)
        {
            float taxSum = 0f;
            int taxCount = 0;
            foreach (TaxLine taxLine in country.TaxLines)
            {
                if (!taxLine.IsImplemented) continue;
                taxSum += taxLine.Rate;
                taxCount++;
            }
            float avgTaxRate = taxCount > 0 ? taxSum / taxCount : 0f;

            float spendingPercentOfGdp;
            if (country.SpendingLines.Count > 0)
            {
                float total = 0f;
                foreach (SpendingLine line in country.SpendingLines)
                {
                    total += line.Amount;
                }
                spendingPercentOfGdp = country.State.GDP > 0f ? total / country.State.GDP * 100f : 0f;
            }
            else
            {
                spendingPercentOfGdp = country.GovernmentSpendingRate;
            }

            return Mathf.Clamp((avgTaxRate + spendingPercentOfGdp) * 0.5f, MinAxisValue, MaxAxisValue);
        }

        /// <summary>This country's Y-axis position (0-100): the average of its eight sectors' RegulationLevel and its implemented welfare programs' GenerosityLevel (0 if none implemented).</summary>
        public static float GetRegulationWelfareAxisValue(Country country)
        {
            float regulationSum = 0f;
            foreach (Sector sector in country.Sectors)
            {
                regulationSum += sector.RegulationLevel;
            }
            float avgRegulation = country.Sectors.Count > 0 ? regulationSum / country.Sectors.Count : 50f;

            float generositySum = 0f;
            int welfareCount = 0;
            foreach (WelfareProgram program in country.WelfarePrograms)
            {
                if (!program.IsImplemented) continue;
                generositySum += program.GenerosityLevel;
                welfareCount++;
            }
            float avgGenerosity = welfareCount > 0 ? generositySum / welfareCount : 0f;

            return Mathf.Clamp((avgRegulation + avgGenerosity) * 0.5f, MinAxisValue, MaxAxisValue);
        }

        /// <summary>
        /// Draws the whole compass into <paramref name="rect"/> - one dot per country in
        /// <paramref name="countries"/>, colored via UiPalette.GetCountryColor, the player's own
        /// ringed in white so it's never ambiguous which dot is "mine" among six similarly-sized
        /// ones. Both axes auto-scale to the OBSERVED min/max across the given countries (padded),
        /// the same "zoom into whatever real variance exists" philosophy GraphRenderer's own Y-axis
        /// auto-scaling already uses - six countries' real policy differences are often modest
        /// relative to the dials' full 0-100 range, and a fixed 0-100 plot left them clustered into
        /// an unreadably tight, overlapping clump. Country-name labels get a light vertical
        /// decluttering pass (pushed down just enough to clear the previous label, top to bottom) so
        /// they never overlap each other even when two dots land close together.
        /// </summary>
        public void Draw(Rect rect, IReadOnlyList<Country> countries, CountryId playerCountryId, GUIStyle labelStyle)
        {
            EnsureTexturesInitialized();
            GUI.DrawTexture(rect, _backgroundTexture, ScaleMode.StretchToFill);

            float margin = labelStyle.fontSize * 1.5f;
            var plotRect = new Rect(rect.x + margin, rect.y + margin, rect.width - margin * 2f, rect.height - margin * 2f);

            int count = countries.Count;
            var xValues = new float[count];
            var yValues = new float[count];
            float minX = float.MaxValue, maxX = float.MinValue, minY = float.MaxValue, maxY = float.MinValue;
            for (int i = 0; i < count; i++)
            {
                xValues[i] = GetFiscalSizeAxisValue(countries[i]);
                yValues[i] = GetRegulationWelfareAxisValue(countries[i]);
                minX = Mathf.Min(minX, xValues[i]);
                maxX = Mathf.Max(maxX, xValues[i]);
                minY = Mathf.Min(minY, yValues[i]);
                maxY = Mathf.Max(maxY, yValues[i]);
            }
            PadRange(ref minX, ref maxX);
            PadRange(ref minY, ref maxY);

            DrawGridlines(plotRect);

            var points = new List<(Vector2 Pixel, Country Country)>(count);
            for (int i = 0; i < count; i++)
            {
                Vector2 point = ToPlotPixel(plotRect, xValues[i], yValues[i], minX, maxX, minY, maxY);
                points.Add((point, countries[i]));

                bool isPlayer = countries[i].Id == playerCountryId;
                if (isPlayer)
                {
                    float ringDiameter = DotDiameter + PlayerRingExtraDiameter;
                    var ringRect = new Rect(point.x - ringDiameter * 0.5f, point.y - ringDiameter * 0.5f, ringDiameter, ringDiameter);
                    DrawCircle(ringRect, _ringTexture, PlayerRingColor);
                }

                var dotRect = new Rect(point.x - DotDiameter * 0.5f, point.y - DotDiameter * 0.5f, DotDiameter, DotDiameter);
                DrawCircle(dotRect, _circleTexture, UiPalette.GetCountryColor(countries[i].Id));
            }

            points.Sort((a, b) => a.Pixel.y.CompareTo(b.Pixel.y));
            float minLabelGap = labelStyle.fontSize + 4f;
            float? previousLabelY = null;
            foreach ((Vector2 point, Country country) in points)
            {
                Vector2 labelSize = labelStyle.CalcSize(new GUIContent(country.Name));
                float labelY = point.y - labelSize.y * 0.5f;
                if (previousLabelY.HasValue && labelY < previousLabelY.Value + minLabelGap)
                {
                    labelY = previousLabelY.Value + minLabelGap;
                }
                previousLabelY = labelY;
                GUI.Label(new Rect(point.x + DotDiameter * 0.5f + 3f, labelY, labelSize.x, labelSize.y), country.Name, labelStyle);
            }

            // Plain GUILayout rows below the plot rect (not more absolute-positioned GUI.Label
            // overlays) for the observed axis range - simpler and can't collide with anything else,
            // unlike the corner/edge overlay labels this replaced.
            var rangeStyle = new GUIStyle(labelStyle) { fontSize = Mathf.Max(9, labelStyle.fontSize - 2) };
            rangeStyle.normal.textColor = AxisLabelColor;
            GUILayout.Label($"X: fiscal size, {minX:F0} (smaller govt) to {maxX:F0} (bigger govt)", rangeStyle);
            GUILayout.Label($"Y: regulation & welfare generosity, {minY:F0} (less) to {maxY:F0} (more)", rangeStyle);
        }

        /// <summary>Pads an observed [min, max] range by 15% on each side (or a flat +-5 for an unreachably narrow/zero range) so dots never sit flush against the plot's own edge.</summary>
        private static void PadRange(ref float min, ref float max)
        {
            float range = max - min;
            float pad = range < 1f ? 5f : range * 0.15f;
            min -= pad;
            max += pad;
        }

        private static Vector2 ToPlotPixel(Rect plotRect, float x, float y, float minX, float maxX, float minY, float maxY)
        {
            float tx = maxX > minX ? Mathf.InverseLerp(minX, maxX, x) : 0.5f;
            float ty = maxY > minY ? Mathf.InverseLerp(minY, maxY, y) : 0.5f;
            return new Vector2(plotRect.x + tx * plotRect.width, plotRect.y + (1f - ty) * plotRect.height);
        }

        /// <summary>Light gridlines at the plot's own quarter marks, plus a slightly brighter one at the midpoint (each axis' own observed-range center, not a universal "50") so "which half" reads at a glance even though the range itself is per-draw.</summary>
        private void DrawGridlines(Rect plotRect)
        {
            for (int i = 0; i <= 4; i++)
            {
                float t = i * 0.25f;
                Color color = i == 2 ? Color.Lerp(GridColor, Color.white, 0.25f) : GridColor;

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

        /// <summary>Same rotated-stretched-rect technique as MapRenderer/PolicyWebRenderer's own DrawLineSegment.</summary>
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
            if (_circleTexture == null)
            {
                _circleTexture = BuildCircleTexture(16, filled: true);
            }
            if (_ringTexture == null)
            {
                _ringTexture = BuildCircleTexture(24, filled: false);
            }
            if (_lineTexture == null)
            {
                _lineTexture = new Texture2D(2, 2, TextureFormat.RGBA32, false) { hideFlags = HideFlags.HideAndDontSave };
                var white = Color.white;
                _lineTexture.SetPixels(new[] { white, white, white, white });
                _lineTexture.Apply(false);
            }
        }

        /// <summary>Same filled-disc technique as PolicyWebRenderer.BuildCircleTexture, extended with an optional hollow-ring mode (a thin annulus instead of a solid disc) for the player-country highlight ring.</summary>
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
