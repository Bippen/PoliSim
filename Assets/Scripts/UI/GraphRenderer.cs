using System.Collections.Generic;
using UnityEngine;

namespace PoliSim.UI
{
    /// <summary>
    /// Reusable line-graph widget: renders a stat's rolling history (see StatHistory) into a
    /// Texture2D, regenerating it only when the underlying data actually changed since the last
    /// draw - GUI.DrawTexture then blits the cached texture for free every other frame, avoiding
    /// needless per-frame churn. One instance per on-screen graph; each instance auto-scales its
    /// own Y-axis to its own historical min/max, so e.g. a GDP graph (tens of thousands) and an
    /// Unemployment graph (single digits) never share a scale. Axis value labels and gridlines are
    /// drawn as an IMGUI overlay on top of the texture rect (not baked into the pixel buffer) -
    /// text-in-a-Texture2D would mean hand-rolled font rendering for no benefit, since GUI.Label
    /// already composites correctly over GUI.DrawTexture in the same layout rect.
    /// </summary>
    public class GraphRenderer
    {
        private const int TextureWidth = 300;
        private const int TextureHeight = 90;

        private static readonly Color BackgroundColor = new Color(0.10f, 0.10f, 0.10f, 1f);
        private static readonly Color GridColor = new Color(0.28f, 0.28f, 0.28f, 1f);
        private static readonly Color HistoryLineColor = new Color(0.35f, 0.85f, 0.45f, 1f);

        /// <summary>Lighter/translucent, drawn dashed - the projected segment must read as "estimate, not committed" the same way the existing live policy preview text already does, not as a real recorded data point.</summary>
        private static readonly Color ProjectedLineColor = new Color(0.35f, 0.85f, 0.45f, 0.45f);

        /// <summary>Same green/red convention as the rest of GameController's directional coloring (e.g. approval up = green, debt up = red) - which raw sign counts as "good" is caller-supplied per stat via Draw's higherIsBetter, since a rising line means opposite things for GDP versus Unemployment.</summary>
        private static readonly Color PositiveChangeColor = new Color(0.35f, 0.85f, 0.45f, 1f);
        private static readonly Color NegativeChangeColor = new Color(0.9f, 0.35f, 0.35f, 1f);
        private static readonly Color NeutralChangeColor = new Color(0.75f, 0.75f, 0.75f, 1f);
        private static readonly Color AxisLabelColor = new Color(0.65f, 0.65f, 0.65f, 1f);

        /// <summary>Below this absolute percent, a first-to-last change reads as "no real change" (neutral gray) rather than an arbitrarily-signed green/red on essentially noise.</summary>
        private const float NeutralChangeThresholdPercent = 0.05f;

        private Texture2D _texture;
        private readonly List<float> _drawnHistory = new List<float>();
        private bool _drawnHasProjection;
        private float _drawnProjectedValue;
        private bool _neverDrawn = true;
        private float _lastMin;
        private float _lastMax;

        private GUIStyle _axisLabelStyle;
        private GUIStyle _changeLabelStyle;

        /// <summary>
        /// Draws this graph via GUILayout, stretching to whatever width the current layout group
        /// gives it. <paramref name="projectedValue"/>, when non-null, extends the line one point
        /// further as a lighter, dashed segment - the same "PreviewTurn estimate, not a guarantee"
        /// spirit as GameController's existing live policy preview. <paramref name="higherIsBetter"/>
        /// picks the green/red direction for the title-row change summary (true for GDP/Approval,
        /// false for Unemployment - a rising line is bad there).
        /// </summary>
        public void Draw(string title, IReadOnlyList<float> history, float? projectedValue, GUIStyle labelStyle, bool higherIsBetter)
        {
            EnsureOverlayStylesInitialized(labelStyle);
            DrawTitleRow(title, history, higherIsBetter, labelStyle);

            if (history == null || history.Count == 0)
            {
                GUILayout.Label("No data yet - advance a turn.", labelStyle);
                return;
            }

            if (NeedsRedraw(history, projectedValue))
            {
                Regenerate(history, projectedValue);
            }

            Rect rect = GUILayoutUtility.GetRect(TextureWidth, TextureHeight, GUILayout.ExpandWidth(true));
            if (_texture != null)
            {
                GUI.DrawTexture(rect, _texture, ScaleMode.StretchToFill);
                DrawAxisLabelOverlay(rect);
            }
        }

        /// <summary>Lazily builds the two small overlay styles from the caller's own label style (font/skin already resolved by GameController's RescaleStylesToScreen) rather than GUI.skin directly, so they stay proportionate to the rest of the panel without GraphRenderer needing its own screen-size-aware rescaling logic.</summary>
        private void EnsureOverlayStylesInitialized(GUIStyle referenceStyle)
        {
            if (_axisLabelStyle != null)
            {
                return;
            }

            int axisFontSize = Mathf.Max(9, Mathf.RoundToInt(referenceStyle.fontSize * 0.65f));
            _axisLabelStyle = new GUIStyle(referenceStyle) { fontSize = axisFontSize, wordWrap = false, fontStyle = FontStyle.Normal };
            _axisLabelStyle.normal.textColor = AxisLabelColor;

            _changeLabelStyle = new GUIStyle(referenceStyle) { wordWrap = false, fontStyle = FontStyle.Bold };
        }

        /// <summary>Title plus a "first-to-last visible value" percentage change, computed straight from the same history buffer the graph itself plots (not a separate calculation) - matches GameController's existing signed-delta number format (see FormatEstimate) rather than inventing a new one.</summary>
        private void DrawTitleRow(string title, IReadOnlyList<float> history, bool higherIsBetter, GUIStyle labelStyle)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(title, labelStyle);

            if (history != null && history.Count >= 2)
            {
                float first = history[0];
                float last = history[history.Count - 1];
                float percentChange = Mathf.Approximately(first, 0f)
                    ? (Mathf.Approximately(last, 0f) ? 0f : 100f * Mathf.Sign(last))
                    : (last - first) / Mathf.Abs(first) * 100f;

                Color color;
                if (Mathf.Abs(percentChange) < NeutralChangeThresholdPercent)
                {
                    color = NeutralChangeColor;
                }
                else
                {
                    bool isPositiveChange = percentChange > 0f;
                    bool isGoodChange = higherIsBetter ? isPositiveChange : !isPositiveChange;
                    color = isGoodChange ? PositiveChangeColor : NegativeChangeColor;
                }

                _changeLabelStyle.normal.textColor = color;
                GUILayout.Label($"{percentChange:+0.0;-0.0;0}%", _changeLabelStyle, GUILayout.ExpandWidth(false));
            }

            GUILayout.EndHorizontal();
        }

        /// <summary>Min/max at top-left/bottom-left, plus the midpoint value at the existing midline gridline - all read straight from the same auto-scaled range Regenerate just computed (cached in _lastMin/_lastMax), so labels never drift out of sync with what the line is actually plotted against.</summary>
        private void DrawAxisLabelOverlay(Rect rect)
        {
            float labelHeight = _axisLabelStyle.fontSize + 4f;
            float mid = (_lastMin + _lastMax) * 0.5f;

            GUI.Label(new Rect(rect.x + 2f, rect.y, rect.width - 4f, labelHeight), _lastMax.ToString("F1"), _axisLabelStyle);
            GUI.Label(new Rect(rect.x + 2f, rect.y + rect.height * 0.5f - labelHeight * 0.5f, rect.width - 4f, labelHeight), mid.ToString("F1"), _axisLabelStyle);
            GUI.Label(new Rect(rect.x + 2f, rect.y + rect.height - labelHeight, rect.width - 4f, labelHeight), _lastMin.ToString("F1"), _axisLabelStyle);
        }

        private bool NeedsRedraw(IReadOnlyList<float> history, float? projectedValue)
        {
            if (_neverDrawn || _texture == null || history.Count != _drawnHistory.Count)
            {
                return true;
            }

            for (int i = 0; i < history.Count; i++)
            {
                if (!Mathf.Approximately(history[i], _drawnHistory[i]))
                {
                    return true;
                }
            }

            bool hasProjection = projectedValue.HasValue;
            if (hasProjection != _drawnHasProjection)
            {
                return true;
            }

            return hasProjection && !Mathf.Approximately(projectedValue.Value, _drawnProjectedValue);
        }

        private void Regenerate(IReadOnlyList<float> history, float? projectedValue)
        {
            if (_texture == null)
            {
                _texture = new Texture2D(TextureWidth, TextureHeight, TextureFormat.RGBA32, false)
                {
                    filterMode = FilterMode.Bilinear,
                    wrapMode = TextureWrapMode.Clamp
                };
            }

            var pixels = new Color[TextureWidth * TextureHeight];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = BackgroundColor;
            }

            GetScaleRange(history, projectedValue, out float min, out float max);
            _lastMin = min;
            _lastMax = max;

            DrawHorizontalLine(pixels, TextureHeight / 2, GridColor);
            PlotSeries(pixels, history, projectedValue, min, max);

            _texture.SetPixels(pixels);
            _texture.Apply(false);

            _drawnHistory.Clear();
            _drawnHistory.AddRange(history);
            _drawnHasProjection = projectedValue.HasValue;
            _drawnProjectedValue = projectedValue ?? 0f;
            _neverDrawn = false;
        }

        /// <summary>This graph's own Y-axis range: its historical min/max (plus the projected point, if any), padded 10% so the series doesn't hug the top/bottom edge, with a flat-line fallback so a constant series doesn't divide by a zero range.</summary>
        private static void GetScaleRange(IReadOnlyList<float> history, float? projectedValue, out float min, out float max)
        {
            min = history[0];
            max = history[0];
            for (int i = 1; i < history.Count; i++)
            {
                min = Mathf.Min(min, history[i]);
                max = Mathf.Max(max, history[i]);
            }
            if (projectedValue.HasValue)
            {
                min = Mathf.Min(min, projectedValue.Value);
                max = Mathf.Max(max, projectedValue.Value);
            }

            float range = max - min;
            float pad = range < 0.0001f ? Mathf.Max(Mathf.Abs(max) * 0.05f, 0.5f) : range * 0.1f;
            min -= pad;
            max += pad;
        }

        private static void PlotSeries(Color[] pixels, IReadOnlyList<float> history, float? projectedValue, float min, float max)
        {
            int totalPoints = history.Count + (projectedValue.HasValue ? 1 : 0);
            int lastRealIndex = history.Count - 1;

            Vector2Int? prevPixel = null;
            for (int i = 0; i < totalPoints; i++)
            {
                float value = i <= lastRealIndex ? history[i] : projectedValue.Value;
                int x = totalPoints == 1 ? TextureWidth - 1 : Mathf.RoundToInt((float)i / (totalPoints - 1) * (TextureWidth - 1));
                float normalized = (value - min) / (max - min);
                int y = Mathf.RoundToInt(normalized * (TextureHeight - 1));

                var pixel = new Vector2Int(x, y);
                if (prevPixel.HasValue)
                {
                    bool isProjectedSegment = i > lastRealIndex;
                    DrawLine(pixels, prevPixel.Value, pixel, isProjectedSegment ? ProjectedLineColor : HistoryLineColor, isProjectedSegment);
                }
                prevPixel = pixel;
            }
        }

        private static void DrawHorizontalLine(Color[] pixels, int y, Color color)
        {
            y = Mathf.Clamp(y, 0, TextureHeight - 1);
            for (int x = 0; x < TextureWidth; x++)
            {
                pixels[y * TextureWidth + x] = color;
            }
        }

        /// <summary>Bresenham line, 2px thick for legibility at typical panel widths, optionally dashed (every 3rd step skipped) so the projected segment reads as "estimate" even before its lighter alpha is accounted for.</summary>
        private static void DrawLine(Color[] pixels, Vector2Int from, Vector2Int to, Color color, bool dashed)
        {
            int x0 = from.x, y0 = from.y, x1 = to.x, y1 = to.y;
            int dx = Mathf.Abs(x1 - x0), sx = x0 < x1 ? 1 : -1;
            int dy = -Mathf.Abs(y1 - y0), sy = y0 < y1 ? 1 : -1;
            int err = dx + dy;
            int step = 0;

            while (true)
            {
                if (!dashed || step % 3 != 0)
                {
                    SetPixelSafe(pixels, x0, y0, color);
                    SetPixelSafe(pixels, x0, y0 + 1, color);
                }
                step++;

                if (x0 == x1 && y0 == y1)
                {
                    break;
                }

                int e2 = 2 * err;
                if (e2 >= dy) { err += dy; x0 += sx; }
                if (e2 <= dx) { err += dx; y0 += sy; }
            }
        }

        private static void SetPixelSafe(Color[] pixels, int x, int y, Color color)
        {
            if (x < 0 || x >= TextureWidth || y < 0 || y >= TextureHeight)
            {
                return;
            }
            pixels[y * TextureWidth + x] = color;
        }
    }
}
