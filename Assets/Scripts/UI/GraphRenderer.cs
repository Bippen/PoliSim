using PoliSim.Data;
using System.Collections.Generic;
using System.Globalization;
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
        /// <summary>P6-3 (board 8c, 2026-09-04): the height the four captions P4-E2 cut held on the Riksbank page at 1280 - measured on the films (§297: the readings caption rose 41 px; the two lane sentences beneath were 12 px each) - given to the projected path rather than closed up, which would have re-opened dead paper at the foot. Scales with the label font.</summary>
        private const float CutCaptionsHeightAt1280 = 66f;   // MEASURED on p4c_1280 / p4g_1280 (§297)

        /// <summary>
        /// The plate a procedural chart is drawn ON - paper, not the dark-dashboard near-black this was
        /// until 2026-08-10.
        ///
        /// ⚠ **Three renderers carried this identical value and all three were missed**, because a chart
        /// with no data yet draws no plate: at turn 0 the graphs say "No data yet" and the map is empty,
        /// so every v2.0 capture to date showed paper where real play would show black. PolicyWeb was
        /// only found first because its ring renders immediately.
        ///
        /// Rule 10 draws the line exactly here: the plate and frame AROUND a procedural chart are the
        /// v2.0 pack's business, the marks inside are not. Node inks, edge good/bad and area accents all
        /// stay exactly as they were - they are already on the aged palette.
        private static readonly Color BackgroundColor = PoliSimTheme.Card;
        private static readonly Color GridColor = PoliSimTheme.Hairline;
        // Was a bright screen green that meant nothing - the series is not direction-keyed, the TITLE ROW
        // carries good/bad. A plain dark ink is the honest reading and the one that works on paper.
        private static readonly Color HistoryLineColor = PoliSimTheme.TextPrimary;

        /// <summary>Lighter/translucent, drawn dashed - the projected segment must read as "estimate, not committed" the same way the existing live policy preview text already does, not as a real recorded data point.</summary>
        private static readonly Color ProjectedLineColor = new Color(PoliSimTheme.TextPrimary.r, PoliSimTheme.TextPrimary.g, PoliSimTheme.TextPrimary.b, 0.45f);

        private static readonly Color AxisLabelColor = PoliSimTheme.TextSecondary;

        /// <summary>Distinct from GridColor (the plain midline) and from HistoryLineColor/ProjectedLineColor, so a threshold/target reference line is never confused with either - a warm amber reads as "reference marker," not "recorded data."</summary>
        /// <summary>The threshold LINE is a fill: the draft amber (D6, 2026-08-28 — the text/fill split; its label draws in the darkened <see cref="PoliSimTheme.Caution"/>).</summary>
        private static readonly Color ThresholdLineColor = PoliSimTheme.Draft;

        private Texture2D _texture;
        private readonly List<float> _drawnHistory = new List<float>();
        private bool _drawnHasProjection;
        private float _drawnProjectedValue;
        private bool _drawnHasThreshold;
        private float _drawnThresholdValue;
        private bool _neverDrawn = true;
        private float _lastMin;
        private float _lastMax;

        /// <summary>
        /// The unit this graph's series is money in, or null if it is not money. Set by whichever Draw
        /// entry point the caller used, and read by every label this class writes - the axis overlay, the
        /// single-point empty state and the "latest:" overlay - so all three of them agree by
        /// construction rather than by three call sites remembering to.
        ///
        /// Held as state rather than threaded through the private draw helpers because the alternative
        /// is a parameter on each of them, and one helper forgetting it is precisely the shape of the
        /// bug this exists to fix.
        /// </summary>
        private MoneyUnit? _moneyUnit;

        /// <summary>0 = most recent window (the only page that can show a next-turn projection); increases going further back in time. Clamped to the valid range fresh every Draw call against the CURRENT history length, so a page index that's now out of range (e.g. right after a fresh game/country switch with less history) never gets stuck showing a blank page.</summary>
        private int _pageFromEnd;

        private GUIStyle _axisLabelStyle;
        private GUIStyle _changeLabelStyle;
        private GUIStyle _pageLabelStyle;
        private GUIStyle _pageButtonStyle;
        private GUIStyle _footStyle;   // 8b: the one caption line under the plot

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
        ///
        /// <paramref name="moneyUnit"/> states whether this series is currency and in which unit -
        /// null for a rate, percentage or index. It is REQUIRED rather than defaulted, which is the
        /// whole point: the unit bug (review item 3) rendered $29T as "29k" because an axis had no way
        /// to know its series was money, and a defaulted parameter would let the next currency graph be
        /// added with the same silence. Prefer passing <c>PolicyWebRenderer.GetStatUnit(stat)</c> where
        /// the call site has a StatNodeId, so the answer comes from the stat's own metadata.
        /// </summary>
        public void Draw(string title, IReadOnlyList<float> history, float? projectedValue, GUIStyle labelStyle, bool? higherIsBetter, MoneyUnit? moneyUnit, float? thresholdValue = null, string thresholdLabel = null, IReadOnlyList<float> enactmentPositions = null, IReadOnlyList<float> shadowHistory = null, bool deltaInPoints = false)
        {
            EnsureOverlayStylesInitialized(labelStyle);
            _moneyUnit = moneyUnit;

            if (history == null || history.Count == 0)
            {
                DrawHeadRow(title, null, higherIsBetter, labelStyle, deltaInPoints, 1);   // 8b: the head row carries the pager now
                GUILayout.Label("No data yet - advance a year.", labelStyle);

                // ⚠ THE PAGE ROW IS STILL DRAWN, and this is the same behaviour-5 defect as DrawPageRow's
                // own, one call level up. Returning here emitted ZERO controls on an empty history and
                // TWO the moment the first turn advanced - a control-count change driven by background
                // state, on screens that carry sliders below the graph.
                //
                // Worth recording HOW this was missed: the 2026-08-10 sweep scanned for methods that
                // both emit a control and early-return, and this method emits none DIRECTLY - it calls
                // DrawPageRow, which does. **A sweep one call level deep cannot see a guard that sits
                // above the emitter rather than beside it.** Found by asking what the fix below did NOT
                // cover, which is the same "what does this check not assert" question the verification
                // note in CLAUDE.md is about.
                // 8b: the pager lives in the head row (DrawHeadRow above) - the disabled arrows still draw, as the note above requires.
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

            DrawHeadRow(title, visibleWindow, higherIsBetter, labelStyle, deltaInPoints, totalPages);

            if (NeedsRedraw(visibleWindow, visibleProjectedValue, thresholdValue))
            {
                Regenerate(visibleWindow, visibleProjectedValue, thresholdValue);
            }

            // Display height is decoupled from the texture's own pixel resolution (StretchToFill below
            // handles that) - a Screen.height fraction, clamped, so three stacked graphs cost
            // meaningfully less of the dashboard's vertical budget on a typical window than the old
            // fixed TextureHeight (90px) did, without losing plot resolution.
            float displayHeight = Mathf.Clamp(Screen.height * 0.085f, 56f, 110f);   // D4 (2026-08-28): clamp(0.075h, 50, 90) → clamp(0.085h, 56, 110); the 300×90 buffer stands (R-G5) and stretches
            Rect rect = GUILayoutUtility.GetRect(TextureWidth, displayHeight, GUILayout.ExpandWidth(true));
            if (_texture != null)
            {
                Rect plot = PlotRect(rect, labelStyle);   // 8b: the y-labels leave the plot for the gutter
                GUI.DrawTexture(plot, _texture, ScaleMode.StretchToFill);
                DrawAxisLabelOverlay(rect, plot);
                if (thresholdValue.HasValue && !string.IsNullOrEmpty(thresholdLabel))
                {
                    DrawThresholdLabelOverlay(plot, thresholdValue.Value, thresholdLabel);
                }

                DrawShadowSeries(plot, shadowHistory, history);
                DrawEnactmentMarkers(plot, enactmentPositions);
            }
            DrawFootRow(totalPages, deltaInPoints, _lastMin < 0f && _lastMax > 0f);
        }

        /// <summary>
        /// C-C9 (P-G1): the no-policy counterfactual drawn against the live series — *"with your
        /// policies"* against *"without"*.
        ///
        /// <para><b>An OVERLAY on the live plot's own scale, deliberately.</b> It reuses `_lastMin` /
        /// `_lastMax`, the range `Regenerate` computed for the real series, so the two lines are read
        /// against one axis. ⚠ Rescaling to fit both would make the counterfactual look like a different
        /// quantity and, worse, would move the real line every time the shadow diverged — the player's
        /// own series must not shift because of something they did not do.</para>
        ///
        /// <para>⚠ <b>The shadow is drawn to the LIVE series' length, never past it.</b> If the two are
        /// different lengths the shorter governs: a counterfactual extending beyond the history it is
        /// being compared with would be drawing a claim about turns that have not happened.</para>
        ///
        /// <para>Dashed, at the projection ink — the sheet's existing idiom for "not the measured line"
        /// — so no new hue enters the palette for it.</para>
        /// </summary>
        private void DrawShadowSeries(Rect rect, IReadOnlyList<float> shadowHistory, IReadOnlyList<float> history)
        {
            if (Event.current.type != EventType.Repaint || shadowHistory == null || history == null) { return; }

            int points = Mathf.Min(shadowHistory.Count, history.Count);
            if (points < 2 || _lastMax <= _lastMin) { return; }

            // The live series is drawn from its most recent `points` entries; the shadow is aligned to
            // the same window from its own tail, so turn N is compared with turn N.
            int shadowStart = shadowHistory.Count - points;

            Vector2 previous = Vector2.zero;
            for (int i = 0; i < points; i++)
            {
                float value = shadowHistory[shadowStart + i];
                float t = i / (float)(points - 1);
                float y = 1f - Mathf.InverseLerp(_lastMin, _lastMax, value);
                var here = new Vector2(rect.x + t * rect.width, rect.y + Mathf.Clamp01(y) * rect.height);

                if (i > 0) { DrawDashedOverlaySegment(previous, here, ProjectedLineColor); }

                previous = here;
            }
        }

        /// <summary>A dashed segment drawn as an overlay rather than into the plot texture, so the
        /// counterfactual costs no regeneration — the same technique the release and enactment markers
        /// use.</summary>
        private static void DrawDashedOverlaySegment(Vector2 from, Vector2 to, Color color)
        {
            const float Dash = 4f;
            const float Gap = 3f;
            Vector2 delta = to - from;
            float length = delta.magnitude;
            if (length < 0.01f) { return; }

            Vector2 dir = delta / length;
            Color previous = GUI.color;
            GUI.color = color;

            for (float start = 0f; start < length; start += Dash + Gap)
            {
                float run = Mathf.Min(Dash, length - start);
                Vector2 a = from + dir * start;
                Vector2 b = from + dir * (start + run);
                var segment = new Rect(Mathf.Min(a.x, b.x), Mathf.Min(a.y, b.y),
                    Mathf.Max(1f, Mathf.Abs(b.x - a.x)), Mathf.Max(1f, Mathf.Abs(b.y - a.y)));
                GUI.DrawTexture(segment, Texture2D.whiteTexture);
            }

            GUI.color = previous;
        }

        /// <summary>
        /// C-C4 (P-G4): a tick at the TOP of the plot for each law this government enacted, so
        /// *"what did I do and when"* is visible on every series the player reads.
        ///
        /// <para><b>The release-tick idiom, reused rather than restated:</b> same brass ink, same
        /// weight-derived width (`HistoryWeight + 2`), same overlay-not-texture drawing so it costs no
        /// regeneration. ⚠ **Drawn at the TOP where the release markers sit at the bottom** — two tick
        /// classes on one axis have to be distinguishable, and the distinction is position rather than
        /// a new colour, which would need a costed case under the palette rules.</para>
        ///
        /// <para>⚠ <b>Positions arrive pre-computed, and that is deliberate.</b> Mapping a date onto
        /// this index-based axis needs the series' own append anchor and cadence
        /// (`MultiResolutionSeries.LastQuarterlyDate` / `QuarterlyPeriodDays`); doing it here would
        /// mean this renderer guessing at a series it is only handed the values of. The caller owns the
        /// mapping and this draws what it is given — anything outside [0,1] is DROPPED rather than
        /// clamped, because a marker clamped to the edge would assert an enactment on a date the
        /// window does not cover.</para>
        /// </summary>
        private void DrawEnactmentMarkers(Rect rect, IReadOnlyList<float> positions)
        {
            if (Event.current.type != EventType.Repaint || positions == null || positions.Count == 0)
            {
                return;
            }

            float markerHeight = Mathf.Max(3f, rect.height * 0.10f);
            float markerWidth = HistoryWeight + 2f;
            Color previous = GUI.color;
            GUI.color = ReleaseMarkerColor;

            for (int i = 0; i < positions.Count; i++)
            {
                float t = positions[i];
                if (float.IsNaN(t) || t < 0f || t > 1f) { continue; }

                float x = rect.x + t * rect.width;
                GUI.DrawTexture(new Rect(x - markerWidth * 0.5f, rect.y, markerWidth, markerHeight),
                    Texture2D.whiteTexture);
            }

            GUI.color = previous;
        }

        /// <summary>A release marker on the timeline - furniture rather than data, so it takes the brass the pack uses for furniture instead of the screen yellow it was.</summary>
        private static readonly Color ReleaseMarkerColor = PoliSimTheme.Brass;








        /// <summary>Convenience wrapper for a stat with no clear "good direction" - see Draw's higherIsBetter remarks. <paramref name="moneyUnit"/> stays required here too: "no clear good direction" says nothing about whether the series is money, and the one caller that draws an arbitrary StatNodeId through this overload can genuinely be handed GDP or Trade Balance.</summary>
        public void DrawNeutral(string title, IReadOnlyList<float> history, float? projectedValue, GUIStyle labelStyle, MoneyUnit? moneyUnit, float? thresholdValue = null, string thresholdLabel = null, bool deltaInPoints = false)
        {
            Draw(title, history, projectedValue, labelStyle, higherIsBetter: null, moneyUnit: moneyUnit, thresholdValue: thresholdValue, thresholdLabel: thresholdLabel, deltaInPoints: deltaInPoints);
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
            _pageLabelStyle.alignment = TextAnchor.MiddleLeft;   // 8b: the head's title cell
            _footStyle = new GUIStyle(referenceStyle) { fontSize = Mathf.Max(8, axisFontSize - 1), wordWrap = false, fontStyle = FontStyle.Normal, clipping = TextClipping.Overflow };
            _footStyle.normal.textColor = PoliSimTheme.TextMuted;
            if (PoliSimTheme.Document != null) { _footStyle.font = PoliSimTheme.Document; _pageLabelStyle.font = PoliSimTheme.Document; }
            // P5-3 (board 6b row 2, 2026-09-03): the pager in the idiom's paper face, not the skin's grey; the glyphs at the axis face
            // height (24 @1x - here the axis size + 10, which is 24 at the 1280 face) in the body serif, never the mono.
            _pageButtonStyle = UiPalette.BuildButtonStyle(new GUIStyle(referenceStyle) { fontSize = axisFontSize + 4, wordWrap = false, alignment = TextAnchor.MiddleCenter }, UiPalette.ButtonKind.Neutral);
            _pageButtonStyle.fixedHeight = axisFontSize + 10f;
            _pageButtonStyle.fixedWidth = 0f;
        }

        /// <summary>Title plus a "first-to-last visible value" percentage change, computed straight from the CURRENT PAGE's own visible window (not the full retained history) - matches GameController's existing signed-delta number format (see FormatEstimate) rather than inventing a new one.</summary>
        // ------------------------------------------------------------------------------------------
        // P6-2 (board 8b, 2026-09-04): the graph composed ONCE as one instrument - head, gutter, foot - for every
        // graph on the sheet. The head row is one line, three cells: the title at left in the caption face
        // (WHAT · UNIT · SCOPE); the window's last value as the hero numeral with the window's delta beside it,
        // direction-aware; the pager at the far right of the same line, the disabled arrow in the hairline ink. The
        // head never shares a line with the plot. The y-labels leave the plot for a gutter at its left, right-aligned
        // with a 4 px tick each; the plot narrows by the gutter and never shortens. The foot is one caption line: the
        // window and pager legend at left, the delta's definition at right - so nothing beneath restates the head.
        // ------------------------------------------------------------------------------------------
        /// <summary>8b: the gutter at 1280 that carries the y-labels; scales with the label font.</summary>
        private const float GutterAt1280 = 46f;
        private const float TickWidth = 4f;

        private Rect PlotRect(Rect rect, GUIStyle labelStyle)
        {
            float gutter = Mathf.Round(GutterAt1280 * labelStyle.fontSize / 14f);
            return new Rect(rect.x + gutter, rect.y, Mathf.Max(10f, rect.width - gutter), rect.height);
        }

        private void DrawHeadRow(string title, IReadOnlyList<float> visibleWindow, bool? higherIsBetter, GUIStyle labelStyle, bool deltaInPoints, int totalPages)
        {
            GUILayout.BeginHorizontal();
            if (!string.IsNullOrEmpty(title)) { GUILayout.Label(title, _pageLabelStyle, GUILayout.ExpandWidth(false)); }   // P4-E2: an empty title draws nothing
            GUILayout.FlexibleSpace();
            if (visibleWindow != null && visibleWindow.Count >= 1)
            {
                float last = visibleWindow[visibleWindow.Count - 1];
                if (!string.IsNullOrEmpty(title))
                {
                    // The hero numeral: the window's last value, in the instrument's own unit. (The Riksbank page passes no
                    // title and prints its own lead figure above the graph, so it takes the delta only.)
                    _changeLabelStyle.normal.textColor = PoliSimTheme.TextPrimary;
                    GUILayout.Label(deltaInPoints ? last.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture) : FormatValue(last), _changeLabelStyle, GUILayout.ExpandWidth(false));
                }
                if (visibleWindow.Count >= 2)
                {
                    float first = visibleWindow[0];
                    // P3-C4 (2026-09-03): a percentage from a ZERO base is not a percentage - from a zero base the delta is the
                    // absolute change in the series' own unit, marked Δ; the percentage stays where the base is real.
                    // P4-E2: a rate's delta reads in points, and a flat window prints nothing at all.
                    bool flatPoints = deltaInPoints && Mathf.Abs(last - first) < 0.005f;
                    if (!flatPoints)
                    {
                        bool zeroBase = Mathf.Approximately(first, 0f);
                        float change = zeroBase ? last - first : (last - first) / Mathf.Abs(first) * 100f;
                        string deltaText = deltaInPoints ? "Δ " + (last - first).ToString("+0.00;-0.00", System.Globalization.CultureInfo.InvariantCulture) + " pts"
                            : zeroBase
                                ? "Δ " + (_moneyUnit.HasValue ? UiFormat.MoneyDelta(change, _moneyUnit.Value) : (change >= 0f ? "+" : "") + FormatAxisValue(change))
                                : $"Δ {change:+0.0;-0.0;0}%";
                        _changeLabelStyle.normal.textColor = higherIsBetter.HasValue
                            ? UiPalette.GetDeltaColor(change, higherIsBetter.Value)
                            : UiPalette.NeutralChangeColor;
                        GUILayout.Space(6f);
                        GUILayout.Label(deltaText, _changeLabelStyle, GUILayout.ExpandWidth(false));
                    }
                }
            }
            GUILayout.Space(8f);
            DrawPager(totalPages);
            GUILayout.EndHorizontal();
        }

        private void DrawPager(int totalPages)
        {
            bool paged = totalPages > 1;
            GUI.enabled = paged && _pageFromEnd < totalPages - 1;
            if (PoliSimWidgets.Button("◀", _pageButtonStyle, GUILayout.Width(_pageButtonStyle.fixedHeight * 1.6f)))
            {
                _pageFromEnd++;
            }
            GUI.enabled = paged && _pageFromEnd > 0;
            if (PoliSimWidgets.Button("▶", _pageButtonStyle, GUILayout.Width(_pageButtonStyle.fixedHeight * 1.6f)))
            {
                _pageFromEnd--;
            }
            GUI.enabled = true;
        }

        /// <summary>8b: one caption line under the plot - the window and the pager's legend at left, the delta's definition at right.</summary>
        private void DrawFootRow(int totalPages, bool deltaInPoints, bool spansZero)
        {
            string window = totalPages <= 1
                ? "THE WHOLE SERIES"
                : _pageFromEnd == 0 ? $"LAST {WindowSize} YEARS" : $"{_pageFromEnd * WindowSize + 1}–{(_pageFromEnd + 1) * WindowSize} YEARS AGO";
            string left = $"OLDER ◀ ▶ NEWER · {window}" + (spansZero ? " · DOTTED = ZERO" : "");
            string unit = deltaInPoints ? "POINTS" : _moneyUnit.HasValue ? "MONEY, NEVER %" : "% OF THE FIRST, MONEY FROM A ZERO BASE";
            // P5-B5 (2026-09-05): THE FOOT NEVER WIDENS THE PAGE. As two ExpandWidth(false) labels in a horizontal, the foot's
            // minimum width was the sum of its texts - wider than the Budget centre column at 1280, so the whole scroll view
            // grew a horizontal scrollbar and every spending row's figure cell slid off the panel (the B4 film caught it).
            // The foot takes the row it is given and the right piece steps down a ladder until it fits beside the left one.
            Rect foot = GUILayoutUtility.GetRect(10f, _footStyle.CalcHeight(new GUIContent(left), 4000f), GUILayout.ExpandWidth(true));
            float gap = 8f;
            float leftWidth = Mathf.Min(_footStyle.CalcSize(new GUIContent(left)).x, foot.width * 0.6f);
            float rightWidth = Mathf.Max(0f, foot.width - leftWidth - gap);
            string right = null;
            foreach (string candidate in new[] { "Δ = LAST − FIRST IN WINDOW · " + unit, "Δ = LAST − FIRST · " + unit, "Δ = LAST − FIRST" })
            {
                if (_footStyle.CalcSize(new GUIContent(candidate)).x <= rightWidth) { right = candidate; break; }
            }
            PoliSimWidgets.MeasuredLabel(new Rect(foot.x, foot.y, leftWidth, foot.height), left, _footStyle);
            if (right != null)
            {
                GUIStyle rightStyle = new GUIStyle(_footStyle) { alignment = TextAnchor.UpperRight };
                PoliSimWidgets.MeasuredLabel(new Rect(foot.xMax - rightWidth, foot.y, rightWidth, foot.height), right, rightStyle);
            }
        }


        /// <summary>
        /// Prev/Next page buttons plus a "how far back" label.
        ///
        /// ⚠ **ALWAYS EMITTED, and this is a behaviour-5 fix (2026-08-10).** It used to `return` early
        /// when there was only one page, so a graph emitted ZERO controls on a fresh game and TWO once
        /// history passed <see cref="WindowSize"/> turns - and `totalPages` derives from
        /// `history.Count`, which grows every turn. So the control count changed spontaneously, driven by
        /// background state rather than by anything the player did.
        ///
        /// That is the hazard `GameController.DrawTaxPolicyContent` documents, and this was the only
        /// site in the codebase where it could genuinely fire: GraphRenderer is drawn on screens that
        /// also carry sliders (Labor Market's participation graph, Welfare's poverty graph), so a graph
        /// crossing the pagination threshold mid-drag would shift the control ID of every slider below
        /// it. Found by sweeping for the pattern after two hand-found instances, not by hitting it.
        ///
        /// **The discipline was already here, one level too shallow** - the buttons inside were correctly
        /// disabled at the ends rather than omitted. The same treatment now covers the row itself.
        /// </summary>

        /// <summary>Min/max at top-left/bottom-left, plus the midpoint value at the existing midline gridline - all read straight from the same auto-scaled range Regenerate just computed (cached in _lastMin/_lastMax), so labels never drift out of sync with what the line is actually plotted against.</summary>
        private void DrawAxisLabelOverlay(Rect rect, Rect plot)
        {
            // 8b: the labels sit in the gutter at the plot's left, right-aligned, a 4 px tick each; the plot's left edge is a hairline.
            float labelHeight = _axisLabelStyle.fontSize + 4f;
            float mid = (_lastMin + _lastMax) * 0.5f;
            float labelWidth = Mathf.Max(8f, plot.x - TickWidth - 2f - rect.x);
            _axisLabelStyle.alignment = TextAnchor.MiddleRight;
            foreach ((float value, float y) in new[] { (_lastMax, plot.y), (mid, plot.y + plot.height * 0.5f - labelHeight * 0.5f), (_lastMin, plot.y + plot.height - labelHeight) })
            {
                GUI.Label(new Rect(rect.x, y, labelWidth, labelHeight), FormatValue(value), _axisLabelStyle);
                PoliSimTheme.Rule(new Rect(plot.x - TickWidth, y + labelHeight * 0.5f - 0.5f, TickWidth, 1f), PoliSimTheme.Hairline);
            }
            PoliSimTheme.Rule(new Rect(plot.x - 0.5f, plot.y, 1f, plot.height), PoliSimTheme.Hairline);
        }

        /// <summary>
        /// Every value this class turns into text goes through here, so the axis, the single-point
        /// empty state and the "latest:" overlay cannot disagree about the same series.
        ///
        /// Currency routes to <see cref="UiFormat.Money"/> with the unit the caller declared; everything
        /// else keeps <see cref="FormatAxisValue"/>, which is correct for the rates, percentages and
        /// indices that make up every other graph in the game.
        /// </summary>
        private string FormatValue(float value)
        {
            return _moneyUnit.HasValue ? UiFormat.Money(value, _moneyUnit.Value) : FormatAxisValue(value);
        }

        /// <summary>
        /// Master Sequence step 9, Step B: axis labels for values of any magnitude, in a gutter only a
        /// few characters wide.
        ///
        /// ⚠ NON-CURRENCY ONLY, as of the P2 fix (2026-08-02). This is where the unit bug lived: the
        /// k/M/B ladder below is correct arithmetic on a base unit of 1, and every money value in this
        /// project is stored in BILLIONS, so it reported $29T as "29k". Money goes through
        /// <see cref="UiFormat.Money"/> instead - reach it via <see cref="FormatValue"/>, never by
        /// calling this directly with an amount.
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

            // ⚠ InvariantCulture on every branch, 2026-08-11. These four were the last unpinned numeric
            // sites in the UI: on this sv-SE machine an axis read "18,2" directly beneath a tile reading
            // "$29.9T", in the same capture. Mixed separators are worse than either convention, because
            // neither reading is available - see UiFormat.Number, which carries the full reasoning.
            if (magnitude >= 1_000_000_000f)
            {
                return (value / 1_000_000_000f).ToString("0.#", CultureInfo.InvariantCulture) + "B";
            }

            if (magnitude >= 1_000_000f)
            {
                return (value / 1_000_000f).ToString("0.#", CultureInfo.InvariantCulture) + "M";
            }

            if (magnitude >= 1_000f)
            {
                return (value / 1_000f).ToString("0.#", CultureInfo.InvariantCulture) + "k";
            }

            return value.ToString("F1", CultureInfo.InvariantCulture);
        }

        /// <summary>Right-aligned label at the threshold line's own Y position, in ThresholdLineColor so it visually pairs with the line it describes rather than blending into the plain axis labels on the left.</summary>
        private void DrawThresholdLabelOverlay(Rect rect, float thresholdValue, string thresholdLabel)
        {
            float labelHeight = _axisLabelStyle.fontSize + 4f;
            float normalized = _lastMax > _lastMin ? Mathf.InverseLerp(_lastMin, _lastMax, thresholdValue) : 0.5f;
            float y = rect.y + rect.height * (1f - normalized);

            var style = new GUIStyle(_axisLabelStyle) { alignment = TextAnchor.MiddleRight };
            // D6: the label is TEXT at 10-16 px and takes the darkened Caution ink; the line it
            // describes keeps the fill amber - the same idea at the two weights the palette split.
            style.normal.textColor = PoliSimTheme.Caution;
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
            // P5-4 (board 6b row 3, 2026-09-03): THE ZERO RULE. Where the scale spans zero (a balance, a change) the rule at 0 is the
            // graph's only axis and it is drawn - dotted, in the text ink, the Year-0 sparkline's own baseline idiom - so a series that
            // starts at zero reads as distance from the rule, which is what a balance is. The delta from a zero base is the absolute
            // change (P3-C4), and it stays in the title row: the board places it at the projection's head, and a history graph has no
            // projection to head - stated as the deviation in COMPLETED.md section 270.
            if (min < 0f && max > 0f)
            {
                int zeroY = Mathf.RoundToInt(Mathf.InverseLerp(min, max, 0f) * (TextureHeight - 1));
                DrawDottedHorizontalLine(pixels, zeroY, PoliSimTheme.TextPrimary);
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
                    DrawLine(pixels, TextureWidth, TextureHeight, prevPixel.Value, pixel,
                        isProjectedSegment ? ProjectedLineColor : HistoryLineColor, isProjectedSegment,
                        isProjectedSegment ? ProjectionWeight : HistoryWeight);
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
        /// <summary>P5-4: a dotted rule - one pixel in three - for the zero axis.</summary>
        private static void DrawDottedHorizontalLine(Color[] pixels, int y, Color color)
        {
            if (y < 0 || y >= TextureHeight) { return; }
            for (int x = 0; x < TextureWidth; x++) { if (x % 3 == 0) { pixels[y * TextureWidth + x] = color; } }
        }

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

        /// <summary>
        /// Master Sequence step 9, Step B2: a compact sparkline for the contextual stat rows on policy
        /// screens - no axes, no labels, no title, just the shape of the series.
        ///
        /// **Deliberately part of GraphRenderer rather than a new widget**, per the directive's "extend
        /// GraphRenderer, do NOT build a parallel system". It reuses the same Bresenham
        /// <see cref="DrawLine"/> the full graphs use, so a sparkline and its full-size counterpart
        /// cannot render the same data differently.
        ///
        /// Static and self-contained because these are drawn many-per-frame across a policy screen and
        /// must not each carry a GraphRenderer's cached texture state. Returns silently on a series too
        /// short to have a shape - one point is not a trend, and drawing a flat line would imply one.
        /// </summary>
        public static void DrawSparkline(Rect rect, IReadOnlyList<float> history, Color color, int maxPoints = 40)
        {
            if (history == null || history.Count < 2 || rect.width < 2f || rect.height < 2f)
            {
                return;
            }

            int width = Mathf.Max(2, Mathf.RoundToInt(rect.width));
            int height = Mathf.Max(2, Mathf.RoundToInt(rect.height));

            Color[] pixels = BuildSparklinePixels(width, height, history, color, maxPoints);

            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false) { hideFlags = HideFlags.HideAndDontSave };
            texture.SetPixels(pixels);
            texture.Apply();
            GUI.DrawTexture(rect, texture);
            Object.DestroyImmediate(texture);
        }

        /// <summary>
        /// The sparkline's pixel buffer, with no GUI or texture involvement.
        ///
        /// **Split out so it can be tested.** The whole of this drawing path shipped a crash that only
        /// surfaced in a live session - an IndexOutOfRangeException mid-OnGUI that blanked the screen -
        /// and it could not be caught headlessly because DrawSparkline calls GUI.DrawTexture, which
        /// throws outside OnGUI. The arithmetic that actually had the bug has no such dependency, so it
        /// lives here and `GraphRendererDiagnostic` hammers it directly.
        /// </summary>
        public static Color[] BuildSparklinePixels(int width, int height, IReadOnlyList<float> history, Color color, int maxPoints = 40)
        {
            var pixels = new Color[width * height];
            if (history == null || history.Count < 2)
            {
                return pixels;
            }

            int start = Mathf.Max(0, history.Count - maxPoints);
            int count = history.Count - start;

            float min = float.MaxValue, max = float.MinValue;
            for (int i = start; i < history.Count; i++)
            {
                min = Mathf.Min(min, history[i]);
                max = Mathf.Max(max, history[i]);
            }

            // A perfectly flat series has no range to normalize against; centre it rather than dividing
            // by zero and producing a line pinned to an edge.
            float range = max - min;
            bool flat = range < Mathf.Epsilon;

            // Board 1l, R-G4 (2026-08-28): sparkline thickness = max(2, round(rectHeight / 34)) device
            // px - 2 at the small chip rects, 3 at a 90px 2560 rect. Native-resolution buffers, so
            // the rule speaks in the buffer's own pixels.
            int thickness = Mathf.Max(2, Mathf.RoundToInt(height / 34f));

            Vector2Int? previous = null;
            for (int i = 0; i < count; i++)
            {
                float value = history[start + i];
                int x = count > 1 ? Mathf.RoundToInt(i / (float)(count - 1) * (width - 1)) : 0;
                int y = flat
                    ? height / 2
                    : Mathf.RoundToInt((value - min) / range * (height - 3)) + 1;

                var point = new Vector2Int(Mathf.Clamp(x, 0, width - 1), Mathf.Clamp(y, 0, height - 1));
                if (previous.HasValue)
                {
                    DrawLine(pixels, width, height, previous.Value, point, color, dashed: false, thickness);
                }
                previous = point;
            }

            return pixels;
        }

        /// <summary>
        /// Board 1l's weight order (§A.16, built 2026-08-28, omnibus R-K3), in BUFFER px once with no
        /// per-resolution branch: the 300×90 buffer's vertical stretch is ×1.0 at 2560 and ≈ ×0.74 at
        /// 1600 (antialiased by the bilinear stretch), so 3 buffer px reads 3 device px at 2560 and
        /// ≈ 2.2 at 1600. R-G1 history 3 (from 2), solid, full ink. R-G2 projection 2, lighter alpha,
        /// dashed 3 on / 2 off (from "skip every 3rd step") so the gaps stay visible beside a heavier
        /// history - it must read "estimate", never "second series". R-G3 threshold stays 1
        /// (DrawDashedHorizontalLine), warm amber: a reference IS a hairline; differentiation comes
        /// from the 3 / 2 / 1 order. R-G5: the 300×90 buffer stands. The finding these answer: all
        /// three landed within a device pixel of each other at 2560, so the recorded data could not
        /// outrank its own reference marker.
        /// </summary>
        private const int HistoryWeight = 3;
        private const int ProjectionWeight = 2;
        private const int ProjectionDashOn = 3;
        private const int ProjectionDashPeriod = 5;

        /// <summary>
        /// Bresenham line, <paramref name="thickness"/> px thick (board 1l's weights: history 3,
        /// projection 2, sparklines by rect height - see HistoryWeight), optionally dashed at 3 on /
        /// 2 off so the projected segment reads as "estimate" even before its lighter alpha is
        /// accounted for. Thickness is laid down as extra rows BELOW the plotted pixel, which is what
        /// the old 2px form did - the series line is mostly horizontal, so its read weight is its
        /// vertical thickness.
        ///
        /// **The buffer's dimensions are PARAMETERS, not the TextureWidth/TextureHeight constants, and
        /// that is the fix for a real crash.** This helper was written for the full-size graph and
        /// hardcoded those constants. DrawSparkline then reused it - deliberately, so a sparkline could
        /// not disagree with its full-size counterpart - against a 72x20 buffer. The bounds check
        /// therefore validated against 300x90 while the index used a stride of 300, so a sparkline pixel
        /// at y>=5 indexed past the end of a 1,440-element array and threw IndexOutOfRangeException
        /// mid-OnGUI, blanking the entire screen; below that it silently wrote to the wrong pixels.
        /// **Sharing the algorithm was right; sharing the constants was not.**
        /// </summary>
        private static void DrawLine(Color[] pixels, int bufferWidth, int bufferHeight, Vector2Int from, Vector2Int to, Color color, bool dashed, int thickness = 2)
        {
            int x0 = from.x, y0 = from.y, x1 = to.x, y1 = to.y;
            int dx = Mathf.Abs(x1 - x0), sx = x0 < x1 ? 1 : -1;
            int dy = -Mathf.Abs(y1 - y0), sy = y0 < y1 ? 1 : -1;
            int err = dx + dy;
            int step = 0;
            int rows = Mathf.Max(1, thickness);

            while (true)
            {
                if (!dashed || step % ProjectionDashPeriod < ProjectionDashOn)
                {
                    for (int row = 0; row < rows; row++)
                    {
                        SetPixelSafe(pixels, bufferWidth, bufferHeight, x0, y0 + row, color);
                    }
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

        /// <summary>
        /// Writes one pixel, ignoring anything outside the buffer. **Bounds-checks and strides against the
        /// CALLER'S buffer dimensions**, which is what makes it genuinely safe for any buffer size rather
        /// than only for the full-size graph - see DrawLine's comment for the crash the old version caused.
        /// </summary>
        private static void SetPixelSafe(Color[] pixels, int bufferWidth, int bufferHeight, int x, int y, Color color)
        {
            if (x < 0 || x >= bufferWidth || y < 0 || y >= bufferHeight)
            {
                return;
            }
            pixels[y * bufferWidth + x] = color;
        }

        // ------------------------------------------------------------------------------------------
        // Board 5f (D11 row 6, 2026-09-02): the rate graph with a PATH - the history at HistoryWeight
        // solid, then exactly as many dashed segments as the projection holds (RatePathProjection: two),
        // a tinted band under the LAST projected segment (the preview's year - the band stops where
        // the preview stops), and a reference riding the plot as a DOTTED Caution line (the rule's
        // reading today), distinct from the dashed threshold and from the projection's dashes. The
        // buffer, the weights and the Bresenham are the graph's own; nothing is restated.
        // ------------------------------------------------------------------------------------------
        private readonly List<float> _drawnPath = new List<float>();
        private bool _drawnHasReference;
        private float _drawnReference;
        private static readonly Color ProjectionBandColor = new Color(PoliSimTheme.TextPrimary.r, PoliSimTheme.TextPrimary.g, PoliSimTheme.TextPrimary.b, 0.07f);

        public void DrawRatePath(string title, IReadOnlyList<float> history, IReadOnlyList<float> projectedPath, float? referenceValue, string referenceLabel, GUIStyle labelStyle)
        {
            EnsureOverlayStylesInitialized(labelStyle);
            _moneyUnit = null;
            if (history == null || history.Count == 0)
            {
                DrawHeadRow(title, null, null, labelStyle, true, 1);
                GUILayout.Label("No data yet - advance a year.", labelStyle);
                return;
            }

            int totalPages = Mathf.Max(1, Mathf.CeilToInt(history.Count / (float)WindowSize));
            _pageFromEnd = Mathf.Clamp(_pageFromEnd, 0, totalPages - 1);
            bool isMostRecentPage = _pageFromEnd == 0;
            int endExclusive = history.Count - _pageFromEnd * WindowSize;
            int startInclusive = Mathf.Max(0, endExclusive - WindowSize);
            var visibleWindow = new List<float>(endExclusive - startInclusive);
            for (int i = startInclusive; i < endExclusive; i++) { visibleWindow.Add(history[i]); }
            IReadOnlyList<float> path = isMostRecentPage && projectedPath != null ? projectedPath : System.Array.Empty<float>();

            DrawHeadRow(title, visibleWindow, null, labelStyle, true, totalPages);

            if (NeedsPathRedraw(visibleWindow, path, referenceValue))
            {
                RegeneratePath(visibleWindow, path, referenceValue);
            }

            float cutCaptions = Mathf.Round(CutCaptionsHeightAt1280 * labelStyle.fontSize / 14f);   // P6-3 (board 8c): the room the four cut captions held goes to the path, not to the instruments beneath
            float displayHeight = Mathf.Clamp(Screen.height * 0.11f, 64f, 140f) + cutCaptions;   // the page's one graph takes a little more of the sheet than a dashboard's three
            Rect rect = GUILayoutUtility.GetRect(TextureWidth, displayHeight, GUILayout.ExpandWidth(true));
            if (_texture != null)
            {
                Rect plot = PlotRect(rect, labelStyle);   // 8b
                GUI.DrawTexture(plot, _texture, ScaleMode.StretchToFill);
                DrawAxisLabelOverlay(rect, plot);
                if (referenceValue.HasValue && !string.IsNullOrEmpty(referenceLabel))
                {
                    DrawThresholdLabelOverlay(plot, referenceValue.Value, referenceLabel);
                }
            }
            DrawFootRow(totalPages, true, _lastMin < 0f && _lastMax > 0f);
        }

        private bool NeedsPathRedraw(IReadOnlyList<float> history, IReadOnlyList<float> path, float? reference)
        {
            if (_neverDrawn || _texture == null || history.Count != _drawnHistory.Count || path.Count != _drawnPath.Count) { return true; }
            for (int i = 0; i < history.Count; i++) { if (!Mathf.Approximately(history[i], _drawnHistory[i])) { return true; } }
            for (int i = 0; i < path.Count; i++) { if (!Mathf.Approximately(path[i], _drawnPath[i])) { return true; } }
            if (reference.HasValue != _drawnHasReference) { return true; }
            return reference.HasValue && !Mathf.Approximately(reference.Value, _drawnReference);
        }

        private void RegeneratePath(IReadOnlyList<float> history, IReadOnlyList<float> path, float? reference)
        {
            if (_texture == null)
            {
                _texture = new Texture2D(TextureWidth, TextureHeight, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            }
            var pixels = new Color[TextureWidth * TextureHeight];
            for (int i = 0; i < pixels.Length; i++) { pixels[i] = BackgroundColor; }

            // The scale folds the path and the reference in, so both are always on the plot.
            var all = new List<float>(history);
            all.AddRange(path);
            GetScaleRange(all, null, reference, out float min, out float max);
            _lastMin = min;
            _lastMax = max;

            int totalPoints = history.Count + path.Count;
            // P3-B2: the path gets its own span - the right 28 % of the plot, one equal step per projected point - so two
            // moves are two visible segments rather than the last two pixels of a fifty-point window (the first film).
            int historyEnd = path.Count > 0 ? Mathf.RoundToInt((TextureWidth - 1) * 0.72f) : TextureWidth - 1;
            int X(int index) => index < history.Count
                ? (history.Count == 1 ? historyEnd : Mathf.RoundToInt((float)index / (history.Count - 1) * historyEnd))
                : historyEnd + Mathf.RoundToInt((float)(index - history.Count + 1) / path.Count * (TextureWidth - 1 - historyEnd));
            int Y(float value) => Mathf.RoundToInt((value - min) / (max - min) * (TextureHeight - 1));

            // The band under the last projected segment - the preview's year - before anything draws over it.
            if (path.Count > 0)
            {
                int x0 = X(totalPoints - 2), x1 = X(totalPoints - 1);
                for (int x = Mathf.Max(0, x0); x <= Mathf.Min(TextureWidth - 1, x1); x++)
                {
                    for (int y = 0; y < TextureHeight; y++)
                    {
                        Color under = pixels[y * TextureWidth + x];
                        pixels[y * TextureWidth + x] = Color.Lerp(under, new Color(ProjectionBandColor.r, ProjectionBandColor.g, ProjectionBandColor.b, 1f), ProjectionBandColor.a);
                    }
                }
            }

            DrawHorizontalLine(pixels, TextureHeight / 2, GridColor);
            if (reference.HasValue)
            {
                int y = Mathf.Clamp(Y(reference.Value), 0, TextureHeight - 1);
                for (int x = 0; x < TextureWidth; x++) { if (x % 3 == 0) { pixels[y * TextureWidth + x] = PoliSimTheme.Caution; } }   // dotted: one on, two off
            }

            Vector2Int? previous = null;
            for (int i = 0; i < totalPoints; i++)
            {
                float value = i < history.Count ? history[i] : path[i - history.Count];
                var pixel = new Vector2Int(X(i), Y(value));
                if (previous.HasValue)
                {
                    bool projected = i >= history.Count;
                    DrawLine(pixels, TextureWidth, TextureHeight, previous.Value, pixel, projected ? ProjectedLineColor : HistoryLineColor, projected, projected ? ProjectionWeight : HistoryWeight);
                }
                previous = pixel;
            }

            _texture.SetPixels(pixels);
            _texture.Apply(false);
            _drawnHistory.Clear();
            _drawnHistory.AddRange(history);
            _drawnPath.Clear();
            _drawnPath.AddRange(path);
            _drawnHasProjection = false;
            _drawnHasThreshold = reference.HasValue;
            _drawnThresholdValue = reference ?? 0f;
            _drawnHasReference = reference.HasValue;
            _drawnReference = reference ?? 0f;
            _neverDrawn = false;
        }
    }
}
