using System.Collections.Generic;
using PoliSim.Data;
using UnityEngine;

namespace PoliSim.UI
{
    /// <summary>
    /// The chart form for a categorical breakdown too long to hue-key: rows sorted largest-first, one
    /// bar each, **all of them in a single ink**, every row carrying its own inline label.
    ///
    /// **Why this exists rather than a longer palette.** `UiPalette.GetCategoricalColor` caps at eight
    /// aged inks because eight is what can be cut aged and still be told apart as 12px legend swatches.
    /// The spending breakdown has 29 categories and the tax breakdown has 13. The honest answer to
    /// "29 distinguishable aged hues" is that they do not exist, so the chart changes rather than the
    /// palette degrading - a 29-slice pie in near-identical inks is a worse chart AND a broken legend.
    ///
    /// <para><b>This satisfies behaviour 9 by construction.</b> A legend swatch must be drawn in the
    /// colour of the element it explains. Here there is no legend at all: the label sits on the row it
    /// names, so there is nothing that can drift out of agreement with the bar beside it. Single-ink
    /// bars are not a compromise on that rule - they remove the surface it applies to.</para>
    ///
    /// <para>Ranking is the other half of the trade. A pie communicates share-of-whole and 29 slices
    /// destroy that; a sorted bar ledger communicates rank and relative size, which is what a reader
    /// actually wants from a 29-way spending breakdown. The percent-of-total still prints per row, so
    /// share is not lost - it moves from an angle nobody can read to a number they can.</para>
    /// </summary>
    public class RankedBarLedgerRenderer
    {
        private const float BarHeightScale = 0.62f;
        private const float MinBarPixels = 1f;

        private GUIStyle _rowLabelStyle;
        private readonly List<(string Label, float Value)> _sorted = new List<(string, float)>();

        /// <summary>
        /// Draws the ledger. <paramref name="ink"/> is the owning area's ink - every bar prints in it.
        ///
        /// The two format parameters are exclusive and both required, exactly as
        /// <see cref="PieChartRenderer.Draw"/> requires them, and for the same reason: a call site
        /// cannot render a currency without naming its <see cref="MoneyUnit"/>. This renderer is a
        /// replacement for that one at two call sites, so it does not get to relax the rule that
        /// caught the unit bug three times.
        /// </summary>
        public void Draw(string title, IReadOnlyList<(string Label, float Value)> rows, GUIStyle labelStyle,
            Color ink, string valueFormat, MoneyUnit? moneyUnit)
        {
            EnsureStylesInitialized(labelStyle);
            GUILayout.Label(title, labelStyle);

            _sorted.Clear();
            float total = 0f;
            foreach ((string Label, float Value) row in rows)
            {
                if (row.Value <= 0f)
                {
                    continue;
                }

                _sorted.Add(row);
                total += row.Value;
            }

            if (_sorted.Count == 0 || total <= 0f)
            {
                GUILayout.Label("No data yet.", labelStyle);
                return;
            }

            _sorted.Sort((a, b) => b.Value.CompareTo(a.Value));
            float largest = _sorted[0].Value;

            foreach ((string Label, float Value) row in _sorted)
            {
                float percent = row.Value / total * 100f;
                string valueText = moneyUnit.HasValue
                    ? UiFormat.Money(row.Value, moneyUnit.Value)
                    : row.Value.ToString(valueFormat ?? "F1");

                GUILayout.Label($"{row.Label}: {valueText} ({percent:F0}%)", _rowLabelStyle);

                // Bars scale against the LARGEST row rather than the total, so the smallest categories
                // stay visible instead of collapsing to a sub-pixel sliver - with 29 rows the tail is
                // most of the chart, and a bar nobody can see is the same as no bar.
                Rect track = GUILayoutUtility.GetRect(10f, labelStyle.fontSize * BarHeightScale, GUILayout.ExpandWidth(true));
                if (Event.current.type != EventType.Repaint)
                {
                    continue;
                }

                var fill = new Rect(track.x, track.y, Mathf.Max(MinBarPixels, track.width * (row.Value / largest)), track.height);
                Color previousColor = GUI.color;
                GUI.color = ink;
                GUI.DrawTexture(fill, Texture2D.whiteTexture);
                GUI.color = previousColor;
            }
        }

        private void EnsureStylesInitialized(GUIStyle referenceStyle)
        {
            if (_rowLabelStyle != null)
            {
                return;
            }

            _rowLabelStyle = new GUIStyle(referenceStyle) { wordWrap = false };
        }
    }
}
