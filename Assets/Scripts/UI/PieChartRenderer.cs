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
    /// A pie as an instrument of the paper idiom - board 8a (D14 item 1, built 2026-09-04, P6-1), amended by
    /// P5-A1 (2026-09-05): the LEADERS RETIRE. The wedge: solid ink, no outline, adjacent wedges parted by a
    /// hairline in the plate's paper; wedges start at 12 o'clock, largest first, clockwise, so the eye reads the
    /// rank without a legend. The list beside the disc IS the legend - a swatch in the wedge's ink, the name bold,
    /// the figure and share beneath - and a wedge large enough to hold its own share prints it inside, in the
    /// paper's ink; the rest are carried by the list alone. Nothing crosses. Series beyond the eighth ink fold into
    /// OTHER, hatched neutral, drawn last; the pie never draws more wedges than <see cref="UiPalette.MaxCategoricalSeries"/>.
    /// The head row and the foot line are the page's. (8a's leaders were built and filmed on 2026-09-04; with eight
    /// wedges they converged on one column and two crossed - the playtest's screenshot - so the board's outside
    /// label is a deviation stated to Design, D15 item 1.)
    /// </summary>
    public class PieChartRenderer
    {
        private const int Diameter = 120;
        /// <summary>8a: the paper hairline between adjacent wedges, in texture pixels at the 120 px disc.</summary>
        private const float WedgeGap = 1.5f;
        /// <summary>P5-A1: a wedge prints its share inside when its span is at least this many degrees - the label's width fits the chord at 0.62 r.</summary>
        private const float InsideLabelMinDegrees = 40f;
        private static readonly Color BackgroundColor = PoliSimTheme.Card;
        private static readonly Color EmptyColor = PoliSimTheme.BarTrack;

        private Texture2D _texture;
        private readonly List<float> _drawnValues = new List<float>();
        private bool _neverDrawn = true;
        private GUIStyle _nameStyle;
        private GUIStyle _figureStyle;
        private GUIStyle _insideStyle;
        private readonly List<PieSlice> _ordered = new List<PieSlice>();
        private readonly List<float> _midAngles = new List<float>();
        private readonly List<float> _spans = new List<float>();

        /// <summary>The area the disc and its legend took on the last layout pass, in the page's content space - the screenshot driver reads it to scroll the pie into a frame.</summary>
        public Rect LastArea { get; private set; }

        /// <summary>Draws the disc with its legend beside it; the title, when given, is drawn above in the label style (the
        /// People page passes none and draws its own head row).</summary>
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

            // The legend: a swatch in the wedge's ink keyed to its row, the name bold, the figure beneath.
            float swatch = Mathf.Round(_nameStyle.fontSize * 0.8f);
            float legendX = disc.xMax + Mathf.Round(labelStyle.fontSize * 1.2f);
            float textX = legendX + swatch + 6f;
            float labelWidth = Mathf.Max(20f, area.xMax - textX);
            float y = area.y + (height - rowsHeight) * 0.5f;
            Color previous = GUI.color;
            for (int i = 0; i < _ordered.Count; i++)
            {
                PieSlice slice = _ordered[i];
                float percent = slice.Value / total * 100f;
                string figure = (moneyUnit.HasValue ? UiFormat.Money(slice.Value, moneyUnit.Value) : slice.Value.ToString(valueFormat ?? "F1")) + $" · {percent:F0} %";
                GUI.color = slice.Color;
                GUI.DrawTexture(new Rect(legendX, y + (lineHeight - swatch) * 0.5f, swatch, swatch), Texture2D.whiteTexture);
                GUI.color = previous;
                GUI.Label(new Rect(textX, y, labelWidth, lineHeight), slice.Label, _nameStyle);
                GUI.Label(new Rect(textX, y + lineHeight, labelWidth, lineHeight), figure, _figureStyle);
                y += lineHeight * 2f;

                // The large wedge carries its own share inside, at the wedge's centroid, in the paper's ink.
                if (_spans[i] >= InsideLabelMinDegrees)
                {
                    float a = _midAngles[i] * Mathf.Deg2Rad;
                    Vector2 at = centre + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * (radius * 0.62f);
                    string share = $"{percent:F0} %";
                    Vector2 size = _insideStyle.CalcSize(new GUIContent(share));
                    GUI.Label(new Rect(Mathf.Round(at.x - size.x * 0.5f), Mathf.Round(at.y - size.y * 0.5f), size.x, size.y), share, _insideStyle);
                }
            }
        }

        private void EnsureStylesInitialized(GUIStyle referenceStyle)
        {
            if (_nameStyle != null && _nameStyle.fontSize == CaptionSize(referenceStyle)) { return; }
            _nameStyle = new GUIStyle(referenceStyle) { wordWrap = false, fontSize = CaptionSize(referenceStyle), fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleLeft, clipping = TextClipping.Overflow };
            _nameStyle.normal.textColor = PoliSimTheme.TextPrimary;
            _figureStyle = new GUIStyle(_nameStyle) { fontStyle = FontStyle.Normal };
            _figureStyle.normal.textColor = PoliSimTheme.TextMuted;
            _insideStyle = new GUIStyle(_nameStyle) { alignment = TextAnchor.MiddleCenter };
            _insideStyle.normal.textColor = PoliSimTheme.Card;
            if (PoliSimTheme.Document != null) { _nameStyle.font = PoliSimTheme.Document; _figureStyle.font = PoliSimTheme.Document; _insideStyle.font = PoliSimTheme.Document; }
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
            _spans.Clear();
            float currentAngle = -90f;   // 12 o'clock, clockwise (y grows downward, so +angle is clockwise on screen)
            for (int i = 0; i < count; i++)
            {
                startAngles[i] = currentAngle;
                float span = Mathf.Max(0f, _ordered[i].Value) / total * 360f;
                currentAngle += span;
                endAngles[i] = currentAngle;
                _midAngles.Add(startAngles[i] + span * 0.5f);
                _spans.Add(span);
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
                                if (lastIsOther && i == count - 1 && ((x + y) & 3) != 0) { pixelColor = BackgroundColor; }
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

        private static float DistanceToEdge(float dx, float dy, float dist, float startDeg, float endDeg)
        {
            return Mathf.Min(DistanceToRay(dx, dy, dist, startDeg), DistanceToRay(dx, dy, dist, endDeg));
        }

        private static float DistanceToRay(float dx, float dy, float dist, float angleDeg)
        {
            float a = angleDeg * Mathf.Deg2Rad;
            float ux = Mathf.Cos(a), uy = Mathf.Sin(a);
            float along = dx * ux + dy * uy;
            if (along < 0f) { return dist; }
            return Mathf.Abs(dx * uy - dy * ux);
        }

        private static bool IsAngleInRange(float angle, float start, float end)
        {
            return angle >= start && angle < end;
        }
    }
}
