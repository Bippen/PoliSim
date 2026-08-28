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
        public void Draw(string title, IReadOnlyList<float> history, float? projectedValue, GUIStyle labelStyle, bool? higherIsBetter, MoneyUnit? moneyUnit, float? thresholdValue = null, string thresholdLabel = null)
        {
            EnsureOverlayStylesInitialized(labelStyle);
            _moneyUnit = moneyUnit;

            if (history == null || history.Count == 0)
            {
                DrawTitleRow(title, null, higherIsBetter, labelStyle);
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
                DrawPageRow(1);
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

        /// <summary>A release marker on the timeline - furniture rather than data, so it takes the brass the pack uses for furniture instead of the screen yellow it was.</summary>
        private static readonly Color ReleaseMarkerColor = PoliSimTheme.Brass;
        /// <summary>The aged draft/caution amber, not the screen orange it was - a preliminary release is the published-data cousin of a draft, and they should read as the same idea.</summary>
        private static readonly Color PreliminaryLineColor = PoliSimTheme.Draft;   // a fill (D6's split, 2026-08-28)

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
        public void DrawPublished(string title, PublishedSeries series, GUIStyle labelStyle, bool? higherIsBetter, System.DateTime currentDate, MoneyUnit? moneyUnit, float? thresholdValue = null, string thresholdLabel = null)
        {
            EnsureOverlayStylesInitialized(labelStyle);
            _moneyUnit = moneyUnit;

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
                GUILayout.Label($"{FormatValue(only.Value)} for {only.ReferencePeriodStart:MMM yyyy} - {only.ReferencePeriodEnd:MMM yyyy} ({only.Status}). Next release builds the trend.", labelStyle);
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
            DrawPublishedPointOverlay(rect, visiblePeriods, latestForPeriod, series);
            DrawDateAxisOverlay(rect, visiblePeriods);

            PublishedEntry newest = latestForPeriod[visiblePeriods[visiblePeriods.Count - 1]];
            string lag = $"{(newest.PublicationDate - newest.ReferencePeriodEnd).Days}d lag";
            // ⚠ BEHAVIOUR 6, AS AMENDED BY D8 - TWO INDEPENDENT CHANNELS, drawn here for the first time.
            //
            // The old §1C.2 rule collapsed them into one ("published = solid + badge; live = dashed,
            // unbadged"), which made the commonest state in this game inexpressible: a PRELIMINARY
            // PUBLISHED figure is published AND provisional at once. D8 struck that sentence. The rule
            // now is:
            //
            //   badge chip + reference period + publication date  ->  PUBLISHED-NESS
            //   frame style: dashed = provisional, solid = final  ->  REVISION STATUS
            //
            // So a preliminary release reads badged, dated AND dashed simultaneously, and a revised one
            // keeps its badge and date while its frame goes solid. Two facts, two carriers, neither
            // inferable from the other.
            bool preliminary = newest.Status == RevisionStatus.Preliminary;
            string status = preliminary ? "PRELIMINARY" : newest.Status.ToString().ToUpperInvariant();

            // Channel 2 first, so the frame sits under the badge rather than over it.
            DrawRevisionFrame(rect, preliminary);

            // Channel 1: the badge carries the status; the line beneath carries the reference period and
            // publication date, which are what make it a PUBLICATION rather than a desk reading.
            DrawPublicationBadge(rect, status, preliminary);
            DrawColoredOverlayLabel(rect, $"latest: {FormatValue(newest.Value)} ({lag})",
                PoliSimTheme.TextPrimary, anyPreliminary);
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
            // Board 1l (2026-08-28): release-point markers scale to weight + 2 so they stay proud of
            // the heavier history line - a 5px tick at the 3px history weight.
            float markerWidth = HistoryWeight + 2f;
            for (int i = 0; i < periods.Count; i++)
            {
                float t = i / (float)(periods.Count - 1);
                float x = rect.x + t * rect.width;
                bool preliminary = latestForPeriod[periods[i]].Status == RevisionStatus.Preliminary;
                var marker = new Rect(x - markerWidth * 0.5f, rect.yMax - markerHeight, markerWidth, markerHeight);
                Color previousMarkerColor = GUI.color;
                GUI.color = preliminary ? PreliminaryLineColor : ReleaseMarkerColor;
                GUI.DrawTexture(marker, Texture2D.whiteTexture);
                GUI.color = previousMarkerColor;
            }
        }

        /// <summary>
        /// Per-point provenance overlay - the part that makes a published series readable AS published
        /// rather than as just another line.
        ///
        /// Designed rather than iterated, because two earlier attempts failed for reasons more width
        /// alone could not fix. Four distinct signals, none of which requires reading text:
        ///
        /// 1. **Every release is a filled marker.** A published series is a sequence of discrete events,
        ///    not a continuous measurement, and the markers say so - the line between them is
        ///    interpolation the player should not read as data.
        /// 2. **Preliminary points are HOLLOW, settled points FILLED.** Shape rather than colour alone,
        ///    so it survives being tinted and does not depend on hue discrimination. A hollow marker
        ///    reads as "not yet solid", which is what preliminary means.
        /// 3. **A revised point shows its ghost.** Where a figure was revised, the superseded value is
        ///    drawn as a faint marker at its old height with a connector to the new one, so the
        ///    correction is visible as a movement rather than inferred from a number that silently
        ///    changed. This is the payoff of Step A's revision mechanic and the thing no amount of extra
        ///    width was ever going to convey on its own.
        /// 4. **Larger markers than the plot line is thick**, so they read as deliberate marks rather
        ///    than as rendering artifacts - the failure mode of the previous attempt's 2px ticks.
        /// </summary>
        private void DrawPublishedPointOverlay(Rect rect, List<System.DateTime> periods, Dictionary<System.DateTime, PublishedEntry> latestForPeriod, PublishedSeries series)
        {
            if (Event.current.type != EventType.Repaint || periods.Count < 2)
            {
                return;
            }

            float range = Mathf.Max(0.0001f, _lastMax - _lastMin);
            float markerSize = Mathf.Clamp(rect.height * 0.09f, 5f, 9f);

            for (int i = 0; i < periods.Count; i++)
            {
                PublishedEntry entry = latestForPeriod[periods[i]];
                float x = rect.x + (i / (float)(periods.Count - 1)) * rect.width;
                float y = rect.yMax - ((entry.Value - _lastMin) / range) * rect.height;

                // A superseded value for this same period, if one exists - the ghost.
                PublishedEntry superseded = null;
                foreach (PublishedEntry candidate in series.Entries)
                {
                    if (candidate.ReferencePeriodStart == entry.ReferencePeriodStart
                        && candidate.PublicationDate < entry.PublicationDate
                        && (superseded == null || candidate.PublicationDate > superseded.PublicationDate))
                    {
                        superseded = candidate;
                    }
                }

                if (superseded != null && !Mathf.Approximately(superseded.Value, entry.Value))
                {
                    float ghostY = rect.yMax - ((superseded.Value - _lastMin) / range) * rect.height;
                    DrawMarker(new Vector2(x, ghostY), markerSize * 0.8f, new Color(PreliminaryLineColor.r, PreliminaryLineColor.g, PreliminaryLineColor.b, 0.35f), hollow: true);
                    DrawConnector(x, ghostY, y, new Color(PreliminaryLineColor.r, PreliminaryLineColor.g, PreliminaryLineColor.b, 0.5f));
                }

                bool preliminary = entry.Status == RevisionStatus.Preliminary;
                DrawMarker(new Vector2(x, y), markerSize, preliminary ? PreliminaryLineColor : ReleaseMarkerColor, hollow: preliminary);
            }
        }

        /// <summary>Filled or hollow square marker. Hollow is drawn as four edges rather than a ring because IMGUI has no primitive circle, and a hollow SQUARE still reads unambiguously as "not filled" at 5-9px.</summary>
        private static void DrawMarker(Vector2 centre, float size, Color color, bool hollow)
        {
            Color previous = GUI.color;
            GUI.color = color;
            float half = size * 0.5f;

            if (!hollow)
            {
                GUI.DrawTexture(new Rect(centre.x - half, centre.y - half, size, size), Texture2D.whiteTexture);
            }
            else
            {
                const float edge = 1.5f;
                GUI.DrawTexture(new Rect(centre.x - half, centre.y - half, size, edge), Texture2D.whiteTexture);
                GUI.DrawTexture(new Rect(centre.x - half, centre.y + half - edge, size, edge), Texture2D.whiteTexture);
                GUI.DrawTexture(new Rect(centre.x - half, centre.y - half, edge, size), Texture2D.whiteTexture);
                GUI.DrawTexture(new Rect(centre.x + half - edge, centre.y - half, edge, size), Texture2D.whiteTexture);
            }

            GUI.color = previous;
        }

        /// <summary>Vertical connector from a superseded value to its revision - the visible "this number moved" cue.</summary>
        private static void DrawConnector(float x, float fromY, float toY, Color color)
        {
            Color previous = GUI.color;
            GUI.color = color;
            float top = Mathf.Min(fromY, toY);
            GUI.DrawTexture(new Rect(x - 0.75f, top, 1.5f, Mathf.Abs(toY - fromY)), Texture2D.whiteTexture);
            GUI.color = previous;
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

        /// <summary>
        /// Behaviour 6, channel 2: REVISION STATUS as frame style. Dashed while a figure is still
        /// provisional, solid once revised - a hairline rule drawn as dashes along the plate's own edge,
        /// so the "this may still move" fact is carried by the frame rather than by a word someone has
        /// to read.
        /// </summary>
        private static void DrawRevisionFrame(Rect rect, bool preliminary)
        {
            if (Event.current.type != EventType.Repaint)
            {
                return;
            }

            Color previous = GUI.color;
            GUI.color = preliminary ? PoliSimTheme.Draft : PoliSimTheme.HairlineStrong;   // the frame is a rule, not text (D6's split)

            const float thickness = 1f;
            if (!preliminary)
            {
                GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, thickness), Texture2D.whiteTexture);
                GUI.DrawTexture(new Rect(rect.x, rect.yMax - thickness, rect.width, thickness), Texture2D.whiteTexture);
            }
            else
            {
                // Dashes rather than a tinted solid: a dashed rule reads as provisional at any size,
                // where a colour alone would be one more hue competing with the eleven that key areas.
                const float dash = 6f;
                const float gap = 4f;
                for (float x = rect.x; x < rect.xMax; x += dash + gap)
                {
                    float w = Mathf.Min(dash, rect.xMax - x);
                    GUI.DrawTexture(new Rect(x, rect.y, w, thickness), Texture2D.whiteTexture);
                    GUI.DrawTexture(new Rect(x, rect.yMax - thickness, w, thickness), Texture2D.whiteTexture);
                }
            }

            GUI.color = previous;
        }

        /// <summary>Behaviour 6, channel 1: a printed chip saying what KIND of figure this is. `ui_chip_outline` is the pack's outlined chip, specified in §1C.5 for exactly this - PRELIMINARY and ACTION REQUIRED - with `ui_chip` reserved for solid ones.</summary>
        private void DrawPublicationBadge(Rect rect, string status, bool preliminary)
        {
            if (Event.current.type != EventType.Repaint)
            {
                return;
            }

            var style = new GUIStyle(_axisLabelStyle) { alignment = TextAnchor.MiddleCenter, fontSize = Mathf.Max(9, _axisLabelStyle.fontSize - 1) };
            float width = PoliSimWidgets.MeasuredWidth(status, style, style.fontSize * 1.6f);
            float height = style.fontSize + 6f;
            var badge = new Rect(rect.x + 4f, rect.y + 4f, width, height);

            Color ink = preliminary ? PoliSimTheme.Caution : PoliSimTheme.TextSecondary;
            Texture2D chip = IconLibrary.GetChrome("ui_chip_outline");
            Color previous = GUI.color;
            GUI.color = ink;
            if (chip != null)
            {
                GUI.DrawTexture(badge, chip, ScaleMode.StretchToFill);
            }
            GUI.color = previous;

            style.normal.textColor = ink;
            GUI.Label(badge, status, style);
        }

        private void DrawColoredOverlayLabel(Rect rect, string text, Color color, bool anyPreliminary)
        {
            var style = new GUIStyle(_axisLabelStyle) { alignment = TextAnchor.UpperRight };
            Color previous = GUI.color;
            GUI.color = color;
            GUI.Label(new Rect(rect.x, rect.y, rect.width - 4f, _axisLabelStyle.fontSize + 4f), text, style);
            GUI.color = previous;
        }

        /// <summary>Convenience wrapper for a stat with no clear "good direction" - see Draw's higherIsBetter remarks. <paramref name="moneyUnit"/> stays required here too: "no clear good direction" says nothing about whether the series is money, and the one caller that draws an arbitrary StatNodeId through this overload can genuinely be handed GDP or Trade Balance.</summary>
        public void DrawNeutral(string title, IReadOnlyList<float> history, float? projectedValue, GUIStyle labelStyle, MoneyUnit? moneyUnit, float? thresholdValue = null, string thresholdLabel = null)
        {
            Draw(title, history, projectedValue, labelStyle, higherIsBetter: null, moneyUnit: moneyUnit, thresholdValue: thresholdValue, thresholdLabel: thresholdLabel);
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
        private void DrawPageRow(int totalPages)
        {
            bool paged = totalPages > 1;

            GUILayout.BeginHorizontal();
            GUI.enabled = paged && _pageFromEnd < totalPages - 1;
            if (GUILayout.Button("< Older", _pageButtonStyle, GUILayout.ExpandWidth(false)))
            {
                _pageFromEnd++;
            }
            GUI.enabled = true;

            // Blank rather than "Last 50 years" on a single-page graph: the row is present for control
            // stability, not to announce a pagination the player has no use for yet.
            string rangeLabel = !paged
                ? string.Empty
                : _pageFromEnd == 0
                    ? $"Last {WindowSize} years"
                    : $"{_pageFromEnd * WindowSize + 1}-{(_pageFromEnd + 1) * WindowSize} years ago";
            GUILayout.Label(rangeLabel, _pageLabelStyle, GUILayout.ExpandWidth(true));

            GUI.enabled = paged && _pageFromEnd > 0;
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

            GUI.Label(new Rect(rect.x + 2f, rect.y, rect.width - 4f, labelHeight), FormatValue(_lastMax), _axisLabelStyle);
            GUI.Label(new Rect(rect.x + 2f, rect.y + rect.height * 0.5f - labelHeight * 0.5f, rect.width - 4f, labelHeight), FormatValue(mid), _axisLabelStyle);
            GUI.Label(new Rect(rect.x + 2f, rect.y + rect.height - labelHeight, rect.width - 4f, labelHeight), FormatValue(_lastMin), _axisLabelStyle);
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
    }
}
