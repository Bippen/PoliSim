using System.Collections.Generic;
using UnityEngine;

namespace PoliSim.UI
{
    /// <summary>One labeled, colored wedge of a PieChartRenderer's pie - value is an absolute magnitude (e.g. a dollar amount or a percentage share), not pre-normalized; the renderer divides by the slice total itself.</summary>
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
    /// Political Systems Overhaul Part C ("Demographic pie charts"): a small, generic, reusable pie
    /// chart widget - one instance per on-screen chart, following the exact same "regenerate a cached
    /// Texture2D only when the underlying data changed" idiom GraphRenderer already established.
    /// Wedges are filled by a per-pixel angle test against the circle's center (every pixel within
    /// the circle's radius gets colored by whichever slice's angular range its angle-from-center
    /// falls into) - no polygon-fill logic needed, and it's the same "test every pixel" spirit
    /// PolicyWebRenderer's own BuildCircleTexture already uses for a plain filled disc. Procedurally
    /// drawn per Master Roadmap working-discipline rule 10 - no imported sprite art.
    /// </summary>
    public class PieChartRenderer
    {
        private const int Diameter = 120;

        /// <summary>Paper, not the dark-dashboard near-black - see GraphRenderer's own note. Rule 10: the plate is the pack's business, the wedges are not.</summary>
        private static readonly Color BackgroundColor = PoliSimTheme.Card;

        /// <summary>The "no data" wedge. Was mid-grey against black; on paper it needs to read as absence without reading as a category, so it is the palette's own bar track.</summary>
        private static readonly Color EmptyColor = PoliSimTheme.BarTrack;

        private Texture2D _texture;
        private readonly List<float> _drawnValues = new List<float>();
        private bool _neverDrawn = true;

        private GUIStyle _legendLabelStyle;

        /// <summary>
        /// Draws the pie (fixed square size, letter-boxed into whatever width GUILayout gives it)
        /// plus a legend row per slice below it (color swatch, label, value, and percent share).
        ///
        /// The two format parameters are exclusive, and both are required so a caller has to answer
        /// the question that the P2 unit bug was caused by nobody asking: is this money? A currency
        /// chart passes <paramref name="moneyUnit"/> (and <c>null</c> for
        /// <paramref name="valueFormat"/>, which is then unused) and its legend renders through
        /// <see cref="UiFormat.Money"/>. Everything else - percentage points, population counts,
        /// employment shares - passes a standard .NET numeric format string and no unit.
        /// </summary>
        public void Draw(string title, IReadOnlyList<PieSlice> slices, GUIStyle labelStyle, string valueFormat, MoneyUnit? moneyUnit)
        {
            EnsureStylesInitialized(labelStyle);
            GUILayout.Label(title, labelStyle);

            float total = 0f;
            foreach (PieSlice slice in slices)
            {
                total += Mathf.Max(0f, slice.Value);
            }

            if (slices.Count == 0 || total <= 0f)
            {
                GUILayout.Label("No data yet.", labelStyle);
                return;
            }

            if (NeedsRedraw(slices))
            {
                Regenerate(slices, total);
            }

            Rect rect = GUILayoutUtility.GetRect(Diameter, Diameter, GUILayout.ExpandWidth(false));
            if (_texture != null)
            {
                GUI.DrawTexture(rect, _texture, ScaleMode.ScaleToFit);
            }

            GUILayout.Space(4f);
            foreach (PieSlice slice in slices)
            {
                if (slice.Value <= 0f) continue;
                float percent = slice.Value / total * 100f;

                GUILayout.BeginHorizontal();
                Rect swatchRect = GUILayoutUtility.GetRect(labelStyle.fontSize, labelStyle.fontSize, GUILayout.ExpandWidth(false));
                Color previousColor = GUI.color;
                GUI.color = slice.Color;
                GUI.DrawTexture(swatchRect, Texture2D.whiteTexture);
                GUI.color = previousColor;
                string valueText = moneyUnit.HasValue
                    ? UiFormat.Money(slice.Value, moneyUnit.Value)
                    : slice.Value.ToString(valueFormat ?? "F1");
                GUILayout.Label($"  {slice.Label}: {valueText} ({percent:F0}%)", _legendLabelStyle);
                GUILayout.EndHorizontal();
            }
        }

        private void EnsureStylesInitialized(GUIStyle referenceStyle)
        {
            if (_legendLabelStyle != null)
            {
                return;
            }
            _legendLabelStyle = new GUIStyle(referenceStyle) { wordWrap = false };
        }

        private bool NeedsRedraw(IReadOnlyList<PieSlice> slices)
        {
            if (_neverDrawn || _texture == null || slices.Count != _drawnValues.Count)
            {
                return true;
            }
            for (int i = 0; i < slices.Count; i++)
            {
                if (!Mathf.Approximately(slices[i].Value, _drawnValues[i]))
                {
                    return true;
                }
            }
            return false;
        }

        private void Regenerate(IReadOnlyList<PieSlice> slices, float total)
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

            // Start/end angle per slice, in the same "-90 (top), sweep clockwise" convention
            // PolicyWebRenderer's own wedge allocation already uses.
            var startAngles = new float[slices.Count];
            var endAngles = new float[slices.Count];
            float currentAngle = -90f;
            for (int i = 0; i < slices.Count; i++)
            {
                startAngles[i] = currentAngle;
                float span = Mathf.Max(0f, slices[i].Value) / total * 360f;
                currentAngle += span;
                endAngles[i] = currentAngle;
            }

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
                        // Normalize into the same continuous range startAngles/endAngles were built
                        // in (can exceed 360 total via currentAngle's running sum), trying the raw
                        // angle and a +360 wrap so a slice that crosses the -180/180 atan2 seam still
                        // matches correctly.
                        for (int i = 0; i < slices.Count; i++)
                        {
                            if (IsAngleInRange(angle, startAngles[i], endAngles[i]) || IsAngleInRange(angle + 360f, startAngles[i], endAngles[i]))
                            {
                                pixelColor = slices[i].Color;
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
            foreach (PieSlice slice in slices)
            {
                _drawnValues.Add(slice.Value);
            }
            _neverDrawn = false;
        }

        private static bool IsAngleInRange(float angle, float start, float end)
        {
            return angle >= start && angle < end;
        }
    }
}
