using System.Collections.Generic;
using UnityEngine;

namespace PoliSim.UI
{
    public readonly struct PieSlice
    {
        public readonly string Label;
        public readonly float Value;
        public readonly Color Color;

        public PieSlice(string label, float value, Color color)
        {
            Label = label;
            Value = value;
            Color = color;
        }
    }

    /// <summary>
    /// A pie as an instrument of the paper idiom - board 8a (D14 item 1, built 2026-09-04, P6-1). The wedge: solid ink,
    /// no outline, adjacent wedges parted by a hairline in the plate's paper (the edges are the negative space, as the
    /// bars' breaks are); wedges start at 12 o'clock, largest first, clockwise, so the eye reads the rank without a legend.
    /// The label sits OUTSIDE on a 1 px leader in the hairline ink - name bold, figure beneath in TextMuted, both in the
    /// caption face. Series beyond the eighth ink fold into OTHER, hatched neutral, drawn last; the pie never draws more
    /// wedges than <see cref="UiPalette.MaxCategoricalSeries"/>. The head row (title left, the provenance stamp right) and
    /// the foot line (one caption) are the page's - the renderer draws the disc and its labels and nothing else. Before 8a
    /// the labels were a legend list beneath the disc with colour swatches; the board moved them out.
    /// </summary>
    public class PieChartRenderer
    {
        private const int Diameter = 120;
        /// <summary>8a: the paper hairline between adjacent wedges, in texture pixels at the 120 px disc.</summary>
        private const float WedgeGap = 1.5f;
        private static readonly Color BackgroundColor = PoliSimTheme.Card;
        private static readonly Color EmptyColor = PoliSimTheme.BarTrack;

        private Texture2D _texture;
        private readonly List<float> _drawnValues = new List<float>();
        private bool _neverDrawn = true;
        private GUIStyle _nameStyle;
        private GUIStyle _figureStyle;
        private readonly List<PieSlice> _ordered = new List<PieSlice>();
        private readonly List<float> _midAngles = new List<float>();

        /// <summary>The area the disc and its labels took on the last layout pass, in the page's content space - the screenshot driver reads it to scroll the pie into a frame.</summary>
        public Rect LastArea { get; private set; }

        /// <summary>Draws the disc with its outside labels; the title, when given, is drawn above in the label style (the
        /// People page passes none and draws its own head row). The labels' column takes the width to the right of the disc.</summary>
        public void Draw(string title, IReadOnlyList<PieSlice> slices, GUIStyle labelStyle, string valueFormat, MoneyUnit? moneyUnit)
        {
            EnsureStylesInitialized(labelStyle);
            if (!string.IsNullOrEmpty(title)) { GUILayout.Label(title, labelStyle); }

            float total = 0f;
            foreach (PieSlice slice in slices) { total += Mathf.Max(0f, slice.Value); }
            if (slices.Count == 0 || total <= 0f)
            {
                GUILayout.Label("No data yet.", labelStyle);
                return;
            }

            Order(slices);
            if (NeedsRedraw())
            {
                Regenerate(total);
            }

            // The disc at left, the labels' column at right: each label on a 1 px leader from its wedge's mid-angle,
            // stacked in wedge order so no two labels overlap whatever the wedge sizes are.
            float lineHeight = Mathf.Max(_nameStyle.lineHeight, _nameStyle.fontSize + 3f);
            float rowsHeight = _ordered.Count * lineHeight * 2f;
            float height = Mathf.Max(Diameter, rowsHeight);
            Rect area = GUILayoutUtility.GetRect(10f, height, GUILayout.ExpandWidth(true));
            if (Event.current.type == EventType.Repaint) { LastArea = area; }
            if (Event.current.type != EventType.Repaint || _texture == null) { return; }

            var disc = new Rect(area.x, area.y + (height - Diameter) * 0.5f, Diameter, Diameter);
            GUI.DrawTexture(disc, _texture, ScaleMode.ScaleToFit);
            float radius = Diameter * 0.5f;
            Vector2 centre = new Vector2(disc.x + radius, disc.y + radius);
            float leaderX = disc.xMax + Mathf.Round(labelStyle.fontSize * 0.9f);
            float labelX = leaderX + 6f;
            float labelWidth = Mathf.Max(20f, area.xMax - labelX);
            float y = area.y + (height - rowsHeight) * 0.5f;
            for (int i = 0; i < _ordered.Count; i++)
            {
                PieSlice slice = _ordered[i];
                float percent = slice.Value / total * 100f;
                string figure = (moneyUnit.HasValue ? UiFormat.Money(slice.Value, moneyUnit.Value) : slice.Value.ToString(valueFormat ?? "F1")) + $" · {percent:F0} %";
                float rowMid = y + lineHeight;
                // The leader: from the wedge's mid-angle at the rim, out to the label column, then a short run to the label.
                float a = _midAngles[i] * Mathf.Deg2Rad;
                Vector2 rim = centre + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * (radius - 1f);
                Vector2 elbow = new Vector2(leaderX, rowMid);
                DrawLeader(rim, elbow);
                PoliSimTheme.Rule(new Rect(leaderX, rowMid - 0.5f, labelX - leaderX, 1f), PoliSimTheme.Hairline);
                GUI.Label(new Rect(labelX, y, labelWidth, lineHeight), slice.Label, _nameStyle);
                GUI.Label(new Rect(labelX, y + lineHeight, labelWidth, lineHeight), figure, _figureStyle);
                y += lineHeight * 2f;
            }
        }

        private static void DrawLeader(Vector2 from, Vector2 to)
        {
            // A 1 px line in the hairline ink, stepped in whole pixels (the same idiom as the map's links, no sprite).
            float length = Vector2.Distance(from, to);
            int steps = Mathf.Max(1, Mathf.CeilToInt(length));
            for (int s = 0; s <= steps; s++)
            {
                Vector2 p = Vector2.Lerp(from, to, s / (float)steps);
                PoliSimTheme.Rule(new Rect(Mathf.Round(p.x), Mathf.Round(p.y), 1f, 1f), PoliSimTheme.Hairline);
            }
        }

        private void EnsureStylesInitialized(GUIStyle referenceStyle)
        {
            if (_nameStyle != null && _nameStyle.fontSize == CaptionSize(referenceStyle)) { return; }
            _nameStyle = new GUIStyle(referenceStyle) { wordWrap = false, fontSize = CaptionSize(referenceStyle), fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleLeft, clipping = TextClipping.Overflow };
            _nameStyle.normal.textColor = PoliSimTheme.TextPrimary;
            _figureStyle = new GUIStyle(_nameStyle) { fontStyle = FontStyle.Normal };
            _figureStyle.normal.textColor = PoliSimTheme.TextMuted;
            if (PoliSimTheme.Document != null) { _nameStyle.font = PoliSimTheme.Document; _figureStyle.font = PoliSimTheme.Document; }
        }

        private static int CaptionSize(GUIStyle referenceStyle) => Mathf.Max(8, Mathf.RoundToInt(referenceStyle.fontSize * 0.68f));

        /// <summary>8a: largest first; series past the eighth ink fold into one hatched OTHER, drawn last.</summary>
        private void Order(IReadOnlyList<PieSlice> slices)
        {
            _ordered.Clear();
            foreach (PieSlice s in slices) { if (s.Value > 0f) { _ordered.Add(s); } }
            _ordered.Sort((a, b) => b.Value.CompareTo(a.Value));
            int cap = UiPalette.MaxCategoricalSeries;
            if (_ordered.Count > cap)
            {
                float other = 0f;
                for (int i = cap - 1; i < _ordered.Count; i++) { other += _ordered[i].Value; }
                _ordered.RemoveRange(cap - 1, _ordered.Count - (cap - 1));
                _ordered.Add(new PieSlice("Other", other, PoliSimTheme.TextMuted));
            }
        }

        private bool NeedsRedraw()
        {
            if (_neverDrawn || _texture == null || _ordered.Count != _drawnValues.Count) { return true; }
            for (int i = 0; i < _ordered.Count; i++)
            {
                if (!Mathf.Approximately(_ordered[i].Value, _drawnValues[i])) { return true; }
            }
            return false;
        }

        private void Regenerate(float total)
        {
            if (_texture == null)
            {
                _texture = new Texture2D(Diameter, Diameter, TextureFormat.RGBA32, false)
                {
                    filterMode = FilterMode.Bilinear,
                    wrapMode = TextureWrapMode.Clamp,
                    hideFlags = HideFlags.HideAndDontSave
                };
            }

            int count = _ordered.Count;
            var startAngles = new float[count];
            var endAngles = new float[count];
            _midAngles.Clear();
            float currentAngle = -90f;   // 12 o'clock, clockwise (y grows downward, so +angle is clockwise on screen)
            for (int i = 0; i < count; i++)
            {
                startAngles[i] = currentAngle;
                float span = Mathf.Max(0f, _ordered[i].Value) / total * 360f;
                currentAngle += span;
                endAngles[i] = currentAngle;
                _midAngles.Add(startAngles[i] + span * 0.5f);
            }
            bool lastIsOther = count > 0 && _ordered[count - 1].Label == "Other" && count == UiPalette.MaxCategoricalSeries;

            var pixels = new Color[Diameter * Diameter];
            float radius = Diameter / 2f;
            for (int y = 0; y < Diameter; y++)
            {
                for (int x = 0; x < Diameter; x++)
                {
                    float dx = x + 0.5f - radius;
                    float dy = y + 0.5f - radius;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    Color pixelColor = BackgroundColor;
                    if (dist <= radius)
                    {
                        pixelColor = EmptyColor;
                        float angle = Mathf.Atan2(dy, dx) * Mathf.Rad2Deg;
                        for (int i = 0; i < count; i++)
                        {
                            if (IsAngleInRange(angle, startAngles[i], endAngles[i]) || IsAngleInRange(angle + 360f, startAngles[i], endAngles[i]))
                            {
                                pixelColor = _ordered[i].Color;
                                // The hatched OTHER: the neutral ink on every fourth diagonal, paper between (the draft hatch's pitch).
                                if (lastIsOther && i == count - 1 && ((x + y) & 3) != 0) { pixelColor = BackgroundColor; }
                                // The paper hairline between adjacent wedges: a pixel within the gap's half-width of a wedge edge.
                                if (count > 1 && dist > 2f && DistanceToEdge(dx, dy, dist, startAngles[i], endAngles[i]) <= WedgeGap * 0.5f) { pixelColor = BackgroundColor; }
                                break;
                            }
                        }
                    }
                    pixels[y * Diameter + x] = pixelColor;
                }
            }

            _texture.SetPixels(pixels);
            _texture.Apply(false);
            _drawnValues.Clear();
            foreach (PieSlice slice in _ordered) { _drawnValues.Add(slice.Value); }
            _neverDrawn = false;
        }

        /// <summary>The pixel's distance to the nearer of the wedge's two radial edges (a point's distance to a ray from the centre).</summary>
        private static float DistanceToEdge(float dx, float dy, float dist, float startDeg, float endDeg)
        {
            return Mathf.Min(DistanceToRay(dx, dy, dist, startDeg), DistanceToRay(dx, dy, dist, endDeg));
        }

        private static float DistanceToRay(float dx, float dy, float dist, float angleDeg)
        {
            float a = angleDeg * Mathf.Deg2Rad;
            float ux = Mathf.Cos(a), uy = Mathf.Sin(a);
            float along = dx * ux + dy * uy;
            if (along < 0f) { return dist; }   // behind the centre: not near this ray
            return Mathf.Abs(dx * uy - dy * ux);
        }

        private static bool IsAngleInRange(float angle, float start, float end)
        {
            return angle >= start && angle < end;
        }
    }
}
