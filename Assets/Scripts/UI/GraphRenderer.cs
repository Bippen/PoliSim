using PoliSim.Data;
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
    ///
    /// Political Systems Overhaul Part C ("Graph restyling"): two additions on top of the above,
    /// both opt-in per call site. (1) An optional threshold/target reference line (debt
    /// comfortable-level, NAIRU) - a second, distinctly-colored horizontal line alongside the
    /// existing plain-gray midline gridline, folded into the auto-scale range so it's always
    /// visible even when the data itself is far from it. (2) "Last N changes" pagination - the
    /// caller can now pass up to StatHistory.MaxEntries (250) worth of history; this widget slices
    /// its own 50-turn display window internally and exposes Prev/Next buttons to page back through
    /// older data, rather than only ever being able to show the most recent 50 turns.
    /// </summary>
    public class GraphRenderer
    {
        private const int TextureWidth = 300;
        private const int TextureHeight = 90;

        /// <summary>How many turns one page shows - unchanged from the graph's original fixed display window, just now one page of potentially several rather than the only page.</summary>
        private const int WindowSize = 50;

        private static readonly Color BackgroundColor = new Color(0.10f, 0.10f, 0.10f, 1f);
        private static readonly Color GridColor = new Color(0.28f, 0.28f, 0.28f, 1f);
        private static readonly Color HistoryLineColor = new Color(0.35f, 0.85f, 0.45f, 1f);

        /// <summary>Lighter/translucent, drawn dashed - the projected segment must read as "estimate, not committed" the same way the existing live policy preview text already does, not as a real recorded data point.</summary>
        private static readonly Color ProjectedLineColor = new Color(0.35f, 0.85f, 0.45f, 0.45f);

        private static readonly Color AxisLabelColor = new Color(0.65f, 0.65f, 0.65f, 1f);

        /// <summary>Distinct from GridColor (the plain midline) and from HistoryLineColor/ProjectedLineColor, so a threshold/target reference line is never confused with either - a warm amber reads as "reference marker," not "recorded data."</summary>
        private static readonly Color ThresholdLineColor = new Color(0.90f, 0.70f, 0.25f, 0.9f);

        private Texture2D _texture;
        private readonly List<float> _drawnHistory = new List<float>();
        private bool _drawnHasProjection;
        private float _drawnProjectedValue;
        private bool _drawnHasThreshold;
        private float _drawnThresholdValue;
        private bool _neverDrawn = true;
        private float _lastMin;
        private float _lastMax;

        /// <summary>0 = most recent window (the only page that can show a next-turn projection); increases going further back in time. Clamped to the valid range fresh every Draw call against the CURRENT history length, so a page index that's now out of range (e.g. right after a fresh game/country switch with less history) never gets stuck showing a blank page.</summary>
        private int _pageFromEnd;

        private GUIStyle _axisLabelStyle;
        private GUIStyle _changeLabelStyle;
        private GUIStyle _pageLabelStyle;
        private GUIStyle _pageButtonStyle;

        /// <summary>
        /// Draws this graph via GUILayout, stretching to whatever width the current layout group
        /// gives it. <paramref name="history"/> may hold up to StatHistory.MaxEntries worth of
        /// turns - only the current page's WindowSize-turn slice is actually plotted; Prev/Next
        /// buttons let the player page back through the rest (see the class doc comment).
        /// <paramref name="projectedValue"/>, when non-null, extends the line one point further as
        /// a lighter, dashed segment on the MOST RECENT page only (paging back to older turns hides
        /// it - a projection for "next turn" makes no sense appended to a window that isn't the most
        /// recent one). <paramref name="higherIsBetter"/> picks the green/red direction for the
        /// title-row change summary (true for GDP/Approval, false for Unemployment - a rising line
        /// is bad there) - null for a stat where "good direction" is genuinely ambiguous/contested
        /// (e.g. an interest rate, or incarceration rate per PrisonPopulationRate's own honestly-
        /// contested framing elsewhere in this codebase), which always shows the neutral gray rather
        /// than inventing a judgment call. Prefer the DrawNeutral convenience overload below at call
        /// sites for that null case. <paramref name="thresholdValue"/>/<paramref
        /// name="thresholdLabel"/> draw an optional reference line (e.g. a country's own
        /// ComfortableDebtToGdpPercent, or NaturalUnemploymentRate/NAIRU) - omit both (leave
        /// thresholdValue null) for a stat with no natural single reference point.
        /// </summary>
        public void Draw(string title, IReadOnlyList<float> history, float? projectedValue, GUIStyle labelStyle, bool? higherIsBetter, float? thresholdValue = null, string thresholdLabel = null)
        {
            EnsureOverlayStylesInitialized(labelStyle);

            if (history == null || history.Count == 0)
            {
                DrawTitleRow(title, null, higherIsBetter, labelStyle);
                GUILayout.Label("No data yet - advance a turn.", labelStyle);
                return;
            }

            int totalPages = Mathf.Max(1, Mathf.CeilToInt(history.Count / (float)WindowSize));
            _pageFromEnd = Mathf.Clamp(_pageFromEnd, 0, totalPages - 1);
            bool isMostRecentPage = _pageFromEnd == 0;

            int endExclusive = history.Count - _pageFromEnd * WindowSize;
            int startInclusive = Mathf.Max(0, endExclusive - WindowSize);
            var visibleWindow = new List<float>(endExclusive - startInclusive);
            for (int i = startInclusive; i < endExclusive; i++)
            {
                visibleWindow.Add(history[i]);
            }

            float? visibleProjectedValue = isMostRecentPage ? projectedValue : null;

            DrawTitleRow(title, visibleWindow, higherIsBetter, labelStyle);
            DrawPageRow(totalPages);

            if (NeedsRedraw(visibleWindow, visibleProjectedValue, thresholdValue))
            {
                Regenerate(visibleWindow, visibleProjectedValue, thresholdValue);
            }

            // Display height is decoupled from the texture's own pixel resolution (StretchToFill below
            // handles that) - a Screen.height fraction, clamped, so three stacked graphs cost
            // meaningfully less of the dashboard's vertical budget on a typical window than the old
            // fixed TextureHeight (90px) did, without losing plot resolution.
            float displayHeight = Mathf.Clamp(Screen.height * 0.075f, 50f, TextureHeight);
            Rect rect = GUILayoutUtility.GetRect(TextureWidth, displayHeight, GUILayout.ExpandWidth(true));
            if (_texture != null)
            {
                GUI.DrawTexture(rect, _texture, ScaleMode.StretchToFill);
                DrawAxisLabelOverlay(rect);
                if (thresholdValue.HasValue && !string.IsNullOrEmpty(thresholdLabel))
                {
                    DrawThresholdLabelOverlay(rect, thresholdValue.Value, thresholdLabel);
                }
            }
        }

        /// <summary>Selectable window for a published-series graph. "All" keeps the existing paging behaviour; the two bounded ranges filter by real elapsed calendar time rather than by entry count, since publication cadences differ per stat (monthly unemployment against quarterly GDP) and a fixed entry count would cover very different spans for each.</summary>
        public enum TimeRange
        {
            OneYear,
            FiveYears,
            All
        }

        private TimeRange _timeRange = TimeRange.All;

        private static readonly Color ReleaseMarkerColor = new Color(0.95f, 0.80f, 0.30f, 1f);
        private static readonly Color PreliminaryLineColor = new Color(0.95f, 0.65f, 0.25f, 1f);

        /// <summary>
        /// Master Sequence step 9, Step B: draws a PUBLISHED series - lagged, revisable figures as the
        /// player actually saw them - with a calendar date axis, release-point markers, and preliminary
        /// values visually distinguished from settled ones.
        ///
        /// A separate overload rather than a change to the float-list signature, and the reason is
        /// SEMANTIC rather than structural. A float-list graph shows live simulation values with no
        /// release timing at all; this shows data that arrived on a schedule and may later be revised.
        /// Only 6 of the ~29 tracked stats have real release schedules (see PublishedStat) - the other 23
        /// legitimately keep reading live, and reshaping their call sites to carry publication semantics
        /// that do not apply to them would make every one of those graphs express something untrue.
        /// Every existing call site is therefore untouched.
        ///
        /// The plotted value for a reference period is its LATEST entry, so a revised figure supersedes
        /// the preliminary one on the line itself - matching what a player looking at the chart today
        /// would see - while the preliminary remains in the series and is what the marker colour reports.
        /// </summary>
        public void DrawPublished(string title, PublishedSeries series, GUIStyle labelStyle, bool? higherIsBetter, System.DateTime currentDate, float? thresholdValue = null, string thresholdLabel = null)
        {
            EnsureOverlayStylesInitialized(labelStyle);

            if (series == null || series.Entries.Count == 0)
            {
                DrawTitleRow(title, null, higherIsBetter, labelStyle);
                GUILayout.Label("Not yet published - the first release is still ahead.", labelStyle);
                return;
            }

            // One point per REFERENCE PERIOD, not per entry: a revised figure replaces its preliminary on
            // the line rather than appearing as a second point at the same date, which would read as
            // volatility that never happened.
            var periods = new List<System.DateTime>();
            var latestForPeriod = new Dictionary<System.DateTime, PublishedEntry>();
            foreach (PublishedEntry entry in series.Entries)
            {
                if (!latestForPeriod.TryGetValue(entry.ReferencePeriodStart, out PublishedEntry existing))
                {
                    periods.Add(entry.ReferencePeriodStart);
                    latestForPeriod[entry.ReferencePeriodStart] = entry;
                }
                else if (entry.PublicationDate > existing.PublicationDate)
                {
                    latestForPeriod[entry.ReferencePeriodStart] = entry;
                }
            }

            periods.Sort();

            System.DateTime cutoff = _timeRange == TimeRange.OneYear ? currentDate.AddYears(-1)
                : _timeRange == TimeRange.FiveYears ? currentDate.AddYears(-5)
                : System.DateTime.MinValue;

            var values = new List<float>();
            var visiblePeriods = new List<System.DateTime>();
            bool anyPreliminary = false;
            foreach (System.DateTime period in periods)
            {
                if (period < cutoff)
                {
                    continue;
                }

                PublishedEntry entry = latestForPeriod[period];
                values.Add(entry.Value);
                visiblePeriods.Add(period);
                anyPreliminary |= entry.Status == RevisionStatus.Preliminary;
            }

            DrawTitleRow(title, values, higherIsBetter, labelStyle);
            DrawTimeRangeRow();

            if (values.Count == 0)
            {
                GUILayout.Label($"No releases in the selected range - {periods.Count} older entries exist.", labelStyle);
                return;
            }

            // A single point is the NORMAL early-game state, not an edge case: a new government starts
            // with one inherited quarter and waits until roughly day 120 for its own first release. A
            // one-point line has no slope to plot and would render as a degenerate full-width segment, so
            // it is reported as a value instead - which is also more honest, since one figure is not yet
            // a trend.
            if (values.Count == 1)
            {
                PublishedEntry only = latestForPeriod[visiblePeriods[0]];
                GUILayout.Label($"{FormatAxisValue(only.Value)} for {only.ReferencePeriodStart:MMM yyyy} - {only.ReferencePeriodEnd:MMM yyyy} ({only.Status}). Next release builds the trend.", labelStyle);
                return;
            }

            if (NeedsRedraw(values, null, thresholdValue))
            {
                Regenerate(values, null, thresholdValue);
            }

            float displayHeight = Mathf.Clamp(Screen.height * 0.075f, 50f, TextureHeight);
            Rect rect = GUILayoutUtility.GetRect(TextureWidth, displayHeight, GUILayout.ExpandWidth(true));
            if (_texture == null)
            {
                return;
            }

            GUI.DrawTexture(rect, _texture, ScaleMode.StretchToFill);
            DrawAxisLabelOverlay(rect);
            if (thresholdValue.HasValue && !string.IsNullOrEmpty(thresholdLabel))
            {
                DrawThresholdLabelOverlay(rect, thresholdValue.Value, thresholdLabel);
            }

            DrawReleaseMarkers(rect, visiblePeriods, latestForPeriod);
            DrawDateAxisOverlay(rect, visiblePeriods);

            PublishedEntry newest = latestForPeriod[visiblePeriods[visiblePeriods.Count - 1]];
            string lag = $"{(newest.PublicationDate - newest.ReferencePeriodEnd).Days}d lag";
            string status = newest.Status == RevisionStatus.Preliminary ? "PRELIMINARY" : newest.Status.ToString().ToUpperInvariant();
            DrawColoredOverlayLabel(rect, $"latest: {FormatAxisValue(newest.Value)} ({status}, {lag})",
                newest.Status == RevisionStatus.Preliminary ? PreliminaryLineColor : Color.white, anyPreliminary);
        }

        /// <summary>Range selector. Bounded ranges filter on real elapsed time, so a monthly stat and a quarterly one both show the same calendar span rather than the same number of points.</summary>
        private void DrawTimeRangeRow()
        {
            GUILayout.BeginHorizontal();
            foreach (TimeRange range in new[] { TimeRange.OneYear, TimeRange.FiveYears, TimeRange.All })
            {
                bool selected = _timeRange == range;
                GUIStyle style = UiPalette.BuildButtonStyle(_pageButtonStyle, selected ? UiPalette.ButtonKind.Primary : UiPalette.ButtonKind.Neutral);
                string label = range == TimeRange.OneYear ? "1yr" : range == TimeRange.FiveYears ? "5yr" : "All";
                if (GUILayout.Button(label, style, GUILayout.ExpandWidth(true)))
                {
                    _timeRange = range;
                }
            }
            GUILayout.EndHorizontal();
        }

        /// <summary>
        /// A tick under each release point, coloured by revision status - amber for a figure still
        /// preliminary, pale for one that has settled. This is the payoff of Step A's revision mechanic:
        /// the player can see WHEN a number arrived, and whether the one they are looking at might still
        /// move. Drawn as an overlay rather than into the plot texture so it costs no regeneration.
        /// </summary>
        private void DrawReleaseMarkers(Rect rect, List<System.DateTime> periods, Dictionary<System.DateTime, PublishedEntry> latestForPeriod)
        {
            if (Event.current.type != EventType.Repaint || periods.Count < 2)
            {
                return;
            }

            float markerHeight = Mathf.Max(3f, rect.height * 0.10f);
            for (int i = 0; i < periods.Count; i++)
            {
                float t = i / (float)(periods.Count - 1);
                float x = rect.x + t * rect.width;
                bool preliminary = latestForPeriod[periods[i]].Status == RevisionStatus.Preliminary;
                var marker = new Rect(x - 1f, rect.yMax - markerHeight, 2f, markerHeight);
                Color previousMarkerColor = GUI.color;
                GUI.color = preliminary ? PreliminaryLineColor : ReleaseMarkerColor;
                GUI.DrawTexture(marker, Texture2D.whiteTexture);
                GUI.color = previousMarkerColor;
            }
        }

        /// <summary>Calendar dates at each end of the plotted span, replacing the turn-number framing entirely - the reference PERIOD each figure describes, not when it was published.</summary>
        private void DrawDateAxisOverlay(Rect rect, List<System.DateTime> periods)
        {
            if (periods.Count == 0)
            {
                return;
            }

            float labelHeight = _axisLabelStyle.fontSize + 4f;
            var left = new Rect(rect.x + 2f, rect.yMax - labelHeight, rect.width * 0.5f, labelHeight);
            var right = new Rect(rect.x + rect.width * 0.5f - 2f, rect.yMax - labelHeight, rect.width * 0.5f, labelHeight);

            GUI.Label(left, periods[0].ToString("MMM yyyy"), _axisLabelStyle);
            var rightStyle = new GUIStyle(_axisLabelStyle) { alignment = TextAnchor.UpperRight };
            GUI.Label(right, periods[periods.Count - 1].ToString("MMM yyyy"), rightStyle);
        }

        private void DrawColoredOverlayLabel(Rect rect, string text, Color color, bool anyPreliminary)
        {
            var style = new GUIStyle(_axisLabelStyle) { alignment = TextAnchor.UpperRight };
            Color previous = GUI.color;
            GUI.color = color;
            GUI.Label(new Rect(rect.x, rect.y, rect.width - 4f, _axisLabelStyle.fontSize + 4f), text, style);
            GUI.color = previous;
        }

        /// <summary>Convenience wrapper for a stat with no clear "good direction" - see Draw's higherIsBetter remarks.</summary>
        public void DrawNeutral(string title, IReadOnlyList<float> history, float? projectedValue, GUIStyle labelStyle, float? thresholdValue = null, string thresholdLabel = null)
        {
            Draw(title, history, projectedValue, labelStyle, higherIsBetter: null, thresholdValue: thresholdValue, thresholdLabel: thresholdLabel);
        }

        /// <summary>Lazily builds the overlay styles from the caller's own label style (font/skin already resolved by GameController's RescaleStylesToScreen) rather than GUI.skin directly, so they stay proportionate to the rest of the panel without GraphRenderer needing its own screen-size-aware rescaling logic.</summary>
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

            _pageLabelStyle = new GUIStyle(referenceStyle) { fontSize = axisFontSize, wordWrap = false, fontStyle = FontStyle.Normal, alignment = TextAnchor.MiddleCenter };
            _pageButtonStyle = new GUIStyle(GUI.skin.button) { fontSize = axisFontSize, fixedHeight = axisFontSize + 10f };
        }

        /// <summary>Title plus a "first-to-last visible value" percentage change, computed straight from the CURRENT PAGE's own visible window (not the full retained history) - matches GameController's existing signed-delta number format (see FormatEstimate) rather than inventing a new one.</summary>
        private void DrawTitleRow(string title, IReadOnlyList<float> visibleWindow, bool? higherIsBetter, GUIStyle labelStyle)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(title, labelStyle);

            if (visibleWindow != null && visibleWindow.Count >= 2)
            {
                float first = visibleWindow[0];
                float last = visibleWindow[visibleWindow.Count - 1];
                float percentChange = Mathf.Approximately(first, 0f)
                    ? (Mathf.Approximately(last, 0f) ? 0f : 100f * Mathf.Sign(last))
                    : (last - first) / Mathf.Abs(first) * 100f;

                _changeLabelStyle.normal.textColor = higherIsBetter.HasValue
                    ? UiPalette.GetDeltaColor(percentChange, higherIsBetter.Value)
                    : UiPalette.NeutralChangeColor;
                GUILayout.Label($"{percentChange:+0.0;-0.0;0}%", _changeLabelStyle, GUILayout.ExpandWidth(false));
            }

            GUILayout.EndHorizontal();
        }

        /// <summary>Prev/Next page buttons plus a "how far back" label - only drawn when there's more than one page, so a graph with <=50 turns of history (most of a fresh game) looks exactly as it did before pagination existed.</summary>
        private void DrawPageRow(int totalPages)
        {
            if (totalPages <= 1)
            {
                return;
            }

            GUILayout.BeginHorizontal();
            GUI.enabled = _pageFromEnd < totalPages - 1;
            if (GUILayout.Button("< Older", _pageButtonStyle, GUILayout.ExpandWidth(false)))
            {
                _pageFromEnd++;
            }
            GUI.enabled = true;

            string rangeLabel = _pageFromEnd == 0
                ? $"Last {WindowSize} turns"
                : $"{_pageFromEnd * WindowSize + 1}-{(_pageFromEnd + 1) * WindowSize} turns ago";
            GUILayout.Label(rangeLabel, _pageLabelStyle, GUILayout.ExpandWidth(true));

            GUI.enabled = _pageFromEnd > 0;
            if (GUILayout.Button("Newer >", _pageButtonStyle, GUILayout.ExpandWidth(false)))
            {
                _pageFromEnd--;
            }
            GUI.enabled = true;
            GUILayout.EndHorizontal();
        }

        /// <summary>Min/max at top-left/bottom-left, plus the midpoint value at the existing midline gridline - all read straight from the same auto-scaled range Regenerate just computed (cached in _lastMin/_lastMax), so labels never drift out of sync with what the line is actually plotted against.</summary>
        private void DrawAxisLabelOverlay(Rect rect)
        {
            float labelHeight = _axisLabelStyle.fontSize + 4f;
            float mid = (_lastMin + _lastMax) * 0.5f;

            GUI.Label(new Rect(rect.x + 2f, rect.y, rect.width - 4f, labelHeight), FormatAxisValue(_lastMax), _axisLabelStyle);
            GUI.Label(new Rect(rect.x + 2f, rect.y + rect.height * 0.5f - labelHeight * 0.5f, rect.width - 4f, labelHeight), FormatAxisValue(mid), _axisLabelStyle);
            GUI.Label(new Rect(rect.x + 2f, rect.y + rect.height - labelHeight, rect.width - 4f, labelHeight), FormatAxisValue(_lastMin), _axisLabelStyle);
        }

        /// <summary>
        /// Master Sequence step 9, Step B: axis labels for values of any magnitude, in a gutter only a
        /// few characters wide.
        ///
        /// The previous `ToString("F1")` produced "42358,1" for GovernmentDebt and "30555,1" for GDP -
        /// seven characters in `rect.width - 4f`. That is the same shape as the StatTile bug the
        /// directive warns about: a number too wide for its space, silently mangled by the UI.
        ///
        /// Abbreviation is both the fix and the hazard here, so this is written so that losing magnitude
        /// is IMPOSSIBLE rather than merely unlikely. Every abbreviated result carries an explicit unit
        /// suffix (k/M/B), so a truncated or misread value cannot masquerade as a smaller plain number -
        /// which is exactly how "29689,3" became a plausible-looking "9,3". If the suffix is absent, no
        /// scaling was applied and the digits are literal.
        ///
        /// Verified against real values taken from an actual baseline run rather than invented ones -
        /// see the table in the Step B commit message. Sub-1000 values keep one decimal, matching the
        /// old behaviour exactly, so percentages and rates are unchanged.
        /// </summary>
        internal static string FormatAxisValue(float value)
        {
            float magnitude = Mathf.Abs(value);

            if (magnitude >= 1_000_000_000f)
            {
                return (value / 1_000_000_000f).ToString("0.#") + "B";
            }

            if (magnitude >= 1_000_000f)
            {
                return (value / 1_000_000f).ToString("0.#") + "M";
            }

            if (magnitude >= 1_000f)
            {
                return (value / 1_000f).ToString("0.#") + "k";
            }

            return value.ToString("F1");
        }

        /// <summary>Right-aligned label at the threshold line's own Y position, in ThresholdLineColor so it visually pairs with the line it describes rather than blending into the plain axis labels on the left.</summary>
        private void DrawThresholdLabelOverlay(Rect rect, float thresholdValue, string thresholdLabel)
        {
            float labelHeight = _axisLabelStyle.fontSize + 4f;
            float normalized = _lastMax > _lastMin ? Mathf.InverseLerp(_lastMin, _lastMax, thresholdValue) : 0.5f;
            float y = rect.y + rect.height * (1f - normalized);

            var style = new GUIStyle(_axisLabelStyle) { alignment = TextAnchor.MiddleRight };
            style.normal.textColor = ThresholdLineColor;
            GUI.Label(new Rect(rect.x + 2f, y - labelHeight * 0.5f, rect.width - 4f, labelHeight), thresholdLabel, style);
        }

        private bool NeedsRedraw(IReadOnlyList<float> history, float? projectedValue, float? thresholdValue)
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
            if (hasProjection && !Mathf.Approximately(projectedValue.Value, _drawnProjectedValue))
            {
                return true;
            }

            bool hasThreshold = thresholdValue.HasValue;
            if (hasThreshold != _drawnHasThreshold)
            {
                return true;
            }

            return hasThreshold && !Mathf.Approximately(thresholdValue.Value, _drawnThresholdValue);
        }

        private void Regenerate(IReadOnlyList<float> history, float? projectedValue, float? thresholdValue)
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

            GetScaleRange(history, projectedValue, thresholdValue, out float min, out float max);
            _lastMin = min;
            _lastMax = max;

            DrawHorizontalLine(pixels, TextureHeight / 2, GridColor);
            if (thresholdValue.HasValue)
            {
                int thresholdY = Mathf.RoundToInt(Mathf.InverseLerp(min, max, thresholdValue.Value) * (TextureHeight - 1));
                DrawDashedHorizontalLine(pixels, thresholdY, ThresholdLineColor);
            }
            PlotSeries(pixels, history, projectedValue, min, max);

            _texture.SetPixels(pixels);
            _texture.Apply(false);

            _drawnHistory.Clear();
            _drawnHistory.AddRange(history);
            _drawnHasProjection = projectedValue.HasValue;
            _drawnProjectedValue = projectedValue ?? 0f;
            _drawnHasThreshold = thresholdValue.HasValue;
            _drawnThresholdValue = thresholdValue ?? 0f;
            _neverDrawn = false;
        }

        /// <summary>This graph's own Y-axis range: its historical min/max (plus the projected point and/or threshold value, if given), padded 10% so the series doesn't hug the top/bottom edge, with a flat-line fallback so a constant series doesn't divide by a zero range. Folding the threshold into the range (not just clamping it into whatever range the data alone produces) is what keeps a reference line ALWAYS visible, even on a page where the data sits far from it - the whole point of a "how far from target are we" reference.</summary>
        private static void GetScaleRange(IReadOnlyList<float> history, float? projectedValue, float? thresholdValue, out float min, out float max)
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
            if (thresholdValue.HasValue)
            {
                min = Mathf.Min(min, thresholdValue.Value);
                max = Mathf.Max(max, thresholdValue.Value);
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

        /// <summary>Same as DrawHorizontalLine but dashed (every 4th pixel skipped) - visually distinguishes the threshold reference line from the plain solid midline gridline at a glance, without needing a different color alone to carry that distinction.</summary>
        private static void DrawDashedHorizontalLine(Color[] pixels, int y, Color color)
        {
            y = Mathf.Clamp(y, 0, TextureHeight - 1);
            for (int x = 0; x < TextureWidth; x++)
            {
                if (x % 4 < 2)
                {
                    pixels[y * TextureWidth + x] = color;
                }
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
