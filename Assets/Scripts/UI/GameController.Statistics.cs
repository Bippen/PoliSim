using System.Collections.Generic;
using System.Globalization;
using PoliSim.Data;
using PoliSim.Simulation;
using UnityEngine;

namespace PoliSim.UI
{
    /// <summary>
    /// UI v3.1 Phase B (2026-08-28) — Statistics › Domestic as INSTRUMENTS, built against Design's
    /// board 2a ("Statistics drawn", drawn 2026-08-28 at 1280×720 against Annex E's census). Every
    /// dataset gets the form that fits its shape: the ten headline readings as compact plates in a
    /// 5-column grid; the four fiscal shares of GDP as bars on ONE printed axis, with GDP per capita
    /// folded in as a bare level (§A.9b, E2 absorbed); the eight sector shares as one stacked
    /// distribution bar over a legend - the one true distribution, where eight gauges each to its own
    /// 100 % were eight readings of nothing; the six live graphs in a 3-column grid at D4's taller
    /// clamp; the Society rows in two columns with a gauge for a share, a row-end sparkline for an
    /// index or level that keeps a history, nothing for a level that does not; the published band with
    /// E19's sentence retired for a KEY on its rule and the poverty bulletin (E18) beside the graphs.
    /// E24 (the turn log) is dropped from International, which otherwise inherits the tokens. The
    /// sub-tabs are kept in their delivered faces (one form across the three sub-tabbed screens).
    ///
    /// Instrument type on this sheet is drawn at the board's px scaled from 720 (the Desk's law,
    /// R-B10; DeskPx / StatsUnit), not at the body clamp. Placeholders on the board - the USA's
    /// figures, its 30 % axis - are declared as such, and the build draws from its own data: the axis
    /// is the group's maximum rounded up to the next 10 %, printed; sector names are the model's.
    ///
    /// <para><b>D-ST (boards 23a-23c, 2026-09-30, `COMPLETED.md` §701): what left the page at rest went to a slip at its own anchor</b> (19b; the
    /// book is <see cref="StatsSlips"/>) - the subtitle to the title's slip, heads' tails, the fiscal axis's arithmetic, the pagers' legends, the Δ's
    /// definition, SOCIETY's unit phrases, the sentences that explained an absence - and the absences draw as 19a's glyphs (ABSENT, NIL, ◇ DATED,
    /// the verdicts). The built screen's wrong readings are fixed where Design found them: the Δ prints in the reading's own unit, GDP growth is
    /// read off the kept series, the sector bar sums to GDP, a chart's head says it is the window's last point, a held seed value is not a history.</para>
    /// </summary>
    public partial class GameController
    {
        /// <summary>D-ST (23a ⑨): the six live series share one pager; the trade chart has its own.</summary>
        private readonly GraphSection _statsSeriesSection = new GraphSection();
        private readonly GraphSection _statsTradeSection = new GraphSection();

        /// <summary>True while the Statistics sheet draws - the one page whose reading cells register slip anchors (the Desk strip and the Budget
        /// header draw the same cell and hold no slips).</summary>
        private bool _statsSlipPage;

        /// <summary>The part of the Statistics content its scroll view shows, in the content's coordinates - the map clips its rotated lines to it.</summary>
        private Rect _statsVisibleContent;

        /// <summary>A slip anchor on the Statistics sheet only.</summary>
        private void StatsAnchor(Rect r, string id) { if (_statsSlipPage) { SlipAnchor(r, id); } }

        /// <summary>A 19a state glyph in its slot, its word where the file is not held.</summary>
        private void DrawStateGlyph(Rect r, Symbol s, Color ink) => SymbolRegistry.Draw(r, s, ink, DeskCaption(6.5f, ink));

        /// <summary>One of the ten headline readings - ONE list for the Desk's chip strip and the Statistics plates, so the two can never disagree about the tenth reading (the tiles' list of old, shared).</summary>
        private readonly struct HeadlineReading
        {
            public readonly string Label;
            public readonly string Value;
            public readonly string Delta;
            public readonly bool DeltaIsGood;
            /// <summary>§566: the delta's own VALUE, where it has one - the ink is then <see cref="UiPalette.GetDeltaColor"/>'s reading of it, and a delta of zero takes
            /// the neutral ink rather than the good one. NaN where the delta is categorical (an outlook's + or -, a projection's NEXT), which the flag above answers for.</summary>
            public readonly float DeltaValue;
            public readonly IReadOnlyList<float> Series;
            /// <summary>D-ST (23a ③): the second line is a figure the model does not hold yet - ABSENT in its slot, never 0.00 and never a dash.</summary>
            public readonly bool DeltaAbsent;

            public HeadlineReading(string label, string value, string delta, bool deltaIsGood, IReadOnlyList<float> series, float deltaValue = float.NaN, bool deltaAbsent = false)
            {
                Label = label;
                Value = value;
                Delta = delta;
                DeltaIsGood = deltaIsGood;
                DeltaValue = deltaValue;
                Series = series;
                DeltaAbsent = deltaAbsent;
            }

            /// <summary>The ink a drawer gives this reading's delta: the value's own where there is one, the flag's otherwise.</summary>
            public Color DeltaInk => float.IsNaN(DeltaValue)
                ? UiPalette.GetDeltaColor(DeltaIsGood ? 1f : -1f, higherIsBetter: true)
                : UiPalette.GetDeltaColor(DeltaValue, higherIsBetter: true);
        }

        /// <summary>Item 5: every spending line's nominal amount summed, over nominal GDP, in per cent.</summary>
        private float LinesShareOfGdpPercent()
        {
            float sum = 0f;
            foreach (SpendingLine line in _playerCountry.SpendingLines) { sum += line.Amount; }
            return 100f * sum / Mathf.Max(0.0001f, _playerCountry.State.NominalGdp);
        }

        /// <summary>Item 5: the same share a year on - each line's own next-year figure (the NEXT column's, SpendingLine.ProjectNextYear) over nominal GDP grown
        /// at the country's potential growth rate and the printed inflation. A projection on stated terms, not a forecast.</summary>
        private float LinesShareOfGdpNextPercent()
        {
            EconomyState state = _playerCountry.State;
            float sum = 0f;
            foreach (SpendingLine line in _playerCountry.SpendingLines) { sum += line.ProjectNextYear(state.Inflation); }
            float nextNominalGdp = state.NominalGdp * (1f + _playerCountry.PotentialGrowthRate / 100f) * (1f + state.Inflation / 100f);
            return 100f * sum / Mathf.Max(0.0001f, nextNominalGdp);
        }

        /// <summary>The ten headline readings in the tiles' order: the figure with its own unit, the GDP delta and the credit outlook where they exist, and the kept history for every reading that has one (the four that keep none - currency, the debt stock, the rating, the balance - carry null and draw no line rather than an invented one).</summary>
        private List<HeadlineReading> BuildHeadlineReadings()
        {
            EconomyState state = _playerCountry.State;
            StatHistory history = _playerCountry.History;
            var readings = new List<HeadlineReading>
            {
                // The GDP figure carries its own unit ("$29.0T"); a suffix would render "$29.0T B" - the
                // tiles' old lesson. Billions is a fact about EconomyState.GDP, stated here rather than
                // read from a StatNodeId (GetStatUnit(...).Value would throw inside OnGUI were the entry
                // ever cleared - the sparkline crash is what an exception in a draw call costs).
                // §566 (2026-09-22, Design's sitting part A item 10): NO DELTA UNTIL ONE EXISTS. The growth figure is computed at a turn's boundary and at no other
                // time, so before the first boundary there is no reading - and the chip printed the format's zero section, "0%", in the GOOD ink, on the desk and on
                // the Statistics tile, for the whole of turn 0. A figure that is only a placeholder is drawn as nothing; once a year has closed, the value's own ink
                // reads it, and a genuine zero takes the neutral one (GetDeltaColor's own threshold), not the green.
                // D-ST (23a ③, Design's question 4): the growth is READ OFF THE KEPT REAL SERIES - four quarters on four quarters - not the controller's
                // figure, which only play's own year-close set (the film's warm-up closes its years through the manager, and the card printed that
                // figure's default, 0.00 %, under a series rising 620 → 671). Until the series holds a year the line is ABSENT, never 0.00, never a dash.
                GdpGrowthReading(state, history),
                new HeadlineReading("Unemployment", StatsReadings.Rate(state.Unemployment), null, false, history?.Unemployment.Quarterly),   // 23a ⑤: one form - one decimal, its unit
                new HeadlineReading("Inflation", StatsReadings.Rate(state.Inflation), null, false, history?.Inflation.Quarterly),
                new HeadlineReading("Approval Rating", UiFormat.Number(state.ApprovalRating, 1), null, false, history?.ApprovalRating.Quarterly)
            };

            if (PlayerHasIndependentCurrency())
            {
                readings.Add(new HeadlineReading("Currency Strength", UiFormat.Number(state.CurrencyStrength, 1), null, false, null));
            }

            readings.Add(new HeadlineReading("Poverty Rate", StatsReadings.Rate(state.PovertyRate), null, false, history?.PovertyRate.Quarterly));
            readings.Add(new HeadlineReading("Government Debt", UiFormat.Money(state.GovernmentDebt, MoneyUnit.Billions), null, false, null));
            readings.Add(new HeadlineReading("Debt-to-GDP", StatsReadings.Rate(state.DebtToGdpRatio), null, false, history?.DebtToGdpRatio.Quarterly));

            // The STANDING rating (Elias's A1 ruling, 2026-08-02: set by scheduled review, unchanged
            // between reviews - recomputing per frame would reintroduce the thrash the cadence removes).
            // An em dash until the first review runs: an unrated sovereign is not a top-rated one. A
            // pill only for a Positive or Negative outlook - Stable is genuinely neither, and an absent
            // pill is the grid's norm, so absence reads as "nothing to telegraph".
            SovereignRatingState rating = _playerCountry.Rating;
            bool hasOutlookSignal = rating.HasBeenReviewed && rating.Outlook != RatingOutlook.Stable;
            readings.Add(new HeadlineReading("Credit Rating",
                rating.HasBeenReviewed ? CreditRatingSystem.Format(rating.Rating) : "-",
                hasOutlookSignal ? (rating.Outlook == RatingOutlook.Positive ? "OUTLOOK +" : "OUTLOOK -") : null,
                rating.Outlook == RatingOutlook.Positive,
                null));
            // Signed on purpose: a balance's direction is the whole reading.
            // P2-0.4 (2026-09-02): THE YEAR, not the accumulator - the last closed fiscal period's balance from the
            // report, the annual series as its line, and a dash before any year has closed (a figure no year has
            // computed is stated, never drawn).
            FiscalTurnReport lastYear = _simulationManager.GetLastFiscalReport(PlayerCountryId);
            readings.Add(new HeadlineReading("Budget Balance", lastYear != null ? StatsReadings.TrueMinus(UiFormat.MoneyDelta(lastYear.BudgetBalance, MoneyUnit.Billions)) : "-", null, false, history?.BudgetBalanceAnnual));   // 23a ⑤: the true minus
            return readings;
        }

        /// <summary>D-ST (23a ③): the GDP card - the nominal level, and real growth over the kept series' last four quarters as its second line, or
        /// ABSENT until the series holds a year.</summary>
        private HeadlineReading GdpGrowthReading(EconomyState state, StatHistory history)
        {
            float? growth = StatsReadings.YearOnYearGrowthPercent(history?.Gdp.Quarterly);
            string line = growth.HasValue ? StatsReadings.TrueMinus(growth.Value.ToString("+0.0;-0.0;0.0", CultureInfo.InvariantCulture)) + "%" : null;
            return new HeadlineReading("GDP", UiFormat.Money(state.NominalGdp, MoneyUnit.Billions), line, (growth ?? 0f) >= 0f, history?.Gdp.Quarterly,
                growth ?? float.NaN, deltaAbsent: !growth.HasValue);
        }

        // ------------------------------------------------------------------------------------------
        // The sheet's measures: the board's px at 720, scaled by the window height - DeskPx for type
        // (floored at D4's 9), StatsUnit for lengths - the same law the Desk draws by (R-B10).
        // ------------------------------------------------------------------------------------------
        private static float StatsUnit(float boardPx) => Mathf.Round(boardPx * UiScreen.Height / DeskBoardHeight);

        /// <summary>The width the Statistics content has inside its scroll view - the sheet's inner width less the scrollbar - so the grids can lay their columns out on the Layout event rather than on a rect measured a frame late.</summary>
        private float StatsContentWidth(float availableWidth)
        {
            float scrollbar = Mathf.Max(16f, GUI.skin.verticalScrollbar.fixedWidth);
            return Mathf.Max(1f, PoliSimWidgets.InnerWidth(availableWidth, _boxStyle) - scrollbar - 4f);
        }

        /// <summary>
        /// C-C4 (P-G4): each law this government ENACTED, as a 0–1 position on the quarterly axis the
        /// six live graphs share — *"what did I do and when"*, on every series the player reads.
        ///
        /// <para><b>The markers derive from the enactment record and nothing else.</b> The source is
        /// `Country.Divisions`, the same log the Parliament screen's DIVISION RECORDS panel prints, and
        /// only entries with <c>Passed</c> — a bill that failed changed nothing, so a tick for it would
        /// mark a date on which nothing happened.</para>
        ///
        /// <para>⚠ <b>The mapping is anchored on the series' OWN append date, not on today.</b>
        /// `MultiResolutionSeries` appends a quarterly point every
        /// <see cref="MultiResolutionSeries.QuarterlyPeriodDays"/> days, so the last point is
        /// `LastQuarterlyDate` — which is up to 90 days in the past. Anchoring on `CurrentDate` instead
        /// would be right on exactly one day per quarter and drift the markers along the axis for the
        /// other ninety.</para>
        ///
        /// <para>⚠ <b>An enactment older than the window is DROPPED, never clamped.</b> The series keeps
        /// a bounded number of points; a marker pinned to the left edge would assert that a law was
        /// enacted at the start of the visible window when it was really enacted before it.</para>
        /// </summary>
        private List<float> BuildEnactmentPositions(MultiResolutionSeries series)
        {
            var positions = new List<float>();
            if (series == null || _playerCountry?.Divisions?.Entries == null) { return positions; }

            int points = series.Quarterly.Count;
            if (points < 2 || !series.LastQuarterlyDate.HasValue) { return positions; }

            System.DateTime last = series.LastQuarterlyDate.Value;
            float span = (points - 1) * (float)MultiResolutionSeries.QuarterlyPeriodDays;
            System.DateTime first = last.AddDays(-span);

            foreach (DivisionRecord division in _playerCountry.Divisions.Entries)
            {
                if (!division.Passed || division.Motion) { continue; }   // §761: a motion enacts nothing - nor a statute's passage the President returned

                float daysFromStart = (float)(division.Date - first).TotalDays;
                float t = daysFromStart / span;
                if (t < 0f || t > 1f) { continue; }

                positions.Add(t);
            }

            return positions;
        }

        /// <summary>The graphs' label style on this sheet: the board's 12 px bold title. The renderer derives its axis, change and pager styles from the first style it is handed, once; every graph on this sheet is handed this one.</summary>
        private GUIStyle StatsGraphLabelStyle()
        {
            GUIStyle style = DeskBody(12f, PoliSimTheme.TextPrimary);
            style.fontStyle = FontStyle.Bold;
            return style;
        }

        /// <summary>A section's caption on its rule (board 2a): mono 8.5 upper-case in TextSecondary with a hairline-strong rule beneath; <paramref name="reserveRight"/> keeps room at the rule's right for a key the caller draws into the returned row.</summary>
        private Rect DrawStatsSectionCaption(string caption, float reserveRight = 0f)
        {
            GUIStyle style = DeskCaption(8.5f, PoliSimTheme.TextSecondary);
            float height = Mathf.Ceil(DeskCaptionHeight(style)) + StatsUnit(4f);
            Rect row = GUILayoutUtility.GetRect(10f, height + 1f, GUILayout.ExpandWidth(true));
            if (Event.current.type == EventType.Repaint)
            {
                PoliSimWidgets.MeasuredLabel(new Rect(row.x, row.y, Mathf.Max(1f, row.width - reserveRight), height), caption, style);
                if (_riksbankRecording) { _riksbankCaptions.Add((caption, row.y)); }   // P5-7: the Riksbank page's captions, for the fold's peek
                PoliSimTheme.Rule(new Rect(row.x, row.yMax - 1f, row.width, 1f), PoliSimTheme.HairlineStrong);
            }

            return row;
        }

        private static void StatsSectionGap()
        {
            GUILayout.Space(StatsUnit(14f));
        }

        /// <summary>
        /// §568 (2026-09-22, Design's drift rows D6 and D13): **ONE CELL FOR A HEADLINE READING**, wherever it stands - the desk's foot, the Budget's header, the
        /// Statistics head. Design: *"ten readouts, two faces … the strip as an integrated band with sparklines, Stats as bordered tiles without. D1-the-split allows
        /// two documents; it does not require two faces for one row of figures."* The cell is a TILE (Elias's ruling: *"the ten readouts as tiles like the rest"*) -
        /// the plate, the caption, the numeral, the delta in the value's own ink, and the kept history as a sparkline at the numeral's right in ONE NEUTRAL INK
        /// (D13: the Laws chips drew a falling debt series in the red of its status, the desk drew every series neutral; a trend is history, and the verdict is the
        /// delta's). A reading with no kept history draws the dotted baseline the strip already drew: the line will start here, and a flat line would imply a trend.
        /// ⚠ This supersedes board 1m-r2's *"the strip is part of the sheet - no plates, a hairline divider between neighbours"* for the desk's foot, by the
        /// 2026-09-22 ruling; what the board decided about pitch, type and the sparkline's place is unchanged and is drawn here.
        /// </summary>
        private void DrawReadingCell(Rect plate, HeadlineReading reading, float captionPx, float numeralPx, float deltaPx, float padX, float padY, float sparkWidth, float sparkHeight)
        {
            if (Event.current.type != EventType.Repaint) { return; }

            GUIStyle caption = DeskCaption(captionPx, PoliSimTheme.TextMuted);
            GUIStyle numeral = DeskNumeral(numeralPx, PoliSimTheme.TextPrimary, TextAnchor.MiddleLeft);
            PoliSimTheme.RoundedCard(plate, PoliSimTheme.Tile, PoliSimTheme.Hairline, 0f);
            float captionHeight = Mathf.Ceil(DeskCaptionHeight(caption));
            // PF-15 (§600): THE PADDING GIVES WAY, NEVER THE FIGURE. The desk's board is scaled to the height the frame leaves it (uy = inner height / 680) and its
            // type to the screen (DeskPx), so a frame that leaves the desk less height shrinks the tile and not the figure: at 88 % of the board height the strip's
            // tile was 46.6 px, and caption 12 + delta 12 + pads 8 left the credit rating's 17 px "AAA" a 14.6 px row (GDP's figure hid the same row by shrinking
            // to its width). Where the figure's measured height does not fit, the vertical padding is taken back first, down to none; past that the guard reports.
            float figureNeed = Mathf.Ceil(numeral.CalcSize(new GUIContent(string.IsNullOrEmpty(reading.Value) ? "0" : reading.Value)).y);
            bool deltaRow = !string.IsNullOrEmpty(reading.Delta) || reading.DeltaAbsent;
            float deltaNeed = !deltaRow ? 0f : Mathf.Ceil(DeskCaptionHeight(DeskCaption(deltaPx, reading.DeltaInk, bold: true)));
            float shortfall = figureNeed - (plate.height - padY * 2f - captionHeight - deltaNeed);
            if (shortfall > 0f) { padY = Mathf.Max(0f, padY - Mathf.Ceil(shortfall * 0.5f)); }
            var inner = new Rect(plate.x + padX, plate.y + padY, plate.width - padX * 2f, plate.height - padY * 2f);
            PoliSimWidgets.MeasuredLabel(new Rect(inner.x, inner.y, inner.width, captionHeight), reading.Label.ToUpperInvariant(), caption);

            GUIStyle delta = !deltaRow ? null : DeskCaption(deltaPx, reading.DeltaInk, bold: true);
            float deltaHeight = delta == null ? 0f : Mathf.Ceil(DeskCaptionHeight(delta));
            float sparkX = inner.xMax - sparkWidth;
            var spark = new Rect(sparkX, inner.yMax - sparkHeight, sparkWidth, sparkHeight);
            if (reading.Series != null && reading.Series.Count >= 2) { GraphRenderer.DrawSparkline(spark, reading.Series, PoliSimTheme.TextSecondary); }
            else if (reading.Series == null && StatsSlips.KeepsNoHistory(reading.Label))
            {
                // D-ST (23a ④, the D24 carried note): a reading whose history is NOT KEPT takes ABSENT in the sparkline's slot - the dotted rule said
                // "the line starts here", which for a series never kept is a promise nothing keeps (debt cannot stay flat under a deficit)
                float side = Mathf.Min(inner.height - captionHeight, Mathf.Max(sparkHeight, StatsUnit(12f)));
                var slot = new Rect(inner.xMax - side, inner.yMax - side, side, side);
                DrawStateGlyph(slot, Symbol.Absent, PoliSimTheme.TextMuted);
                StatsAnchor(slot, "card:history");
                UiContainmentGuard.Check("Reading cell history glyph", slot, plate);
            }
            else { DeskDottedBaseline(spark); }
            UiContainmentGuard.Check("Reading cell sparkline", spark, plate);

            var numeralRect = new Rect(inner.x, inner.y + captionHeight, Mathf.Max(1f, sparkX - inner.x - padX), Mathf.Max(1f, inner.yMax - deltaHeight - inner.y - captionHeight));
            PoliSimWidgets.MeasuredLabel(numeralRect, reading.Value, numeral);
            if (reading.DeltaAbsent)
            {
                // 23a ③: a growth the model does not hold yet is not a zero - ABSENT in the line's slot, its slip naming it
                var slot = new Rect(inner.x, inner.yMax - deltaHeight, deltaHeight, deltaHeight);
                DrawStateGlyph(slot, Symbol.Absent, PoliSimTheme.TextMuted);
                StatsAnchor(slot, "card:gdp/growth");
            }
            else if (delta != null)
            {
                Rect line = new Rect(inner.x, inner.yMax - deltaHeight, Mathf.Max(1f, sparkX - inner.x - padX), deltaHeight);
                PoliSimWidgets.MeasuredLabel(line, reading.Delta, delta);
                if (reading.Label == "GDP") { StatsAnchor(new Rect(line.x, line.y, Mathf.Min(line.width, delta.CalcSize(new GUIContent(reading.Delta)).x), line.height), "card:gdp/growth"); }
            }
        }

        /// <summary>Board 2a's 3-column graph grid: each cell is a GUILayout column one third of the content width wide; the renderer's own title row, pager and plot stack inside it. Three per row, the last row's remainder left open.</summary>
        private void DrawStatsGraphGrid(float contentWidth, List<System.Action> cells)
        {
            // §728 (UI v3.5): each chart on its card - four of the twelve columns, the composition's three across - laid out in the flow inside the card's
            // own box, so the renderer's head and plot keep the flow's coordinates (the sheet's anchors read them).
            float gap = V35.Px(V35.Gutter);
            float column = V35Span(contentWidth, 4);
            for (int start = 0; start < cells.Count; start += 3)
            {
                GUILayout.BeginHorizontal();
                for (int i = start; i < start + 3 && i < cells.Count; i++)
                {
                    if (i > start)
                    {
                        GUILayout.Space(gap);
                    }

                    GUILayout.BeginVertical(V35CardStyle(), GUILayout.Width(column));
                    cells[i]();
                    GUILayout.EndVertical();
                }

                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
                GUILayout.Space(gap);
            }
        }

        // P-A2 (Playtest 1, finding 2 - 2026-08-29): the "as published" graph block that closed this
        // sheet is gone. It was a DISPLAY cut: the PublicationSystem mechanism is untouched (the
        // election model's section-19 reading takes Published, never State - PerceivedPerformanceHarness
        // asserts it), and the PRELIMINARY / revision honesty conventions stay on the main graphs,
        // which already carry them.

        /// <summary>
        /// Statistics › Domestic as board 2a draws it, top to bottom: the headline plates, the fiscal
        /// position on one axis, the sector distribution, the six live graphs, YOUR POLICIES, the Society rows. Next-year projections ride the three
        /// graphs that have them, from the same cached PreviewTurn the Desk's effects card reads.
        /// <para>D-ST (23a ⑨-⑭): the six charts share ONE pager on the LIVE SERIES head and draw no feet - their words are slips; each chart's Δ prints
        /// in its reading's own unit; a verdict glyph leads a name where lower is better; each head's figure is ◇ DATED - the window's last point,
        /// beside the live card - and the GDP chart names itself REAL (the card is nominal; the chart and its estimate are real, B6's rule); approval's
        /// seed value, held until the first year's close, is empty paper.</para>
        /// </summary>
        private void DrawDomesticStatisticsContent(float contentWidth)
        {
            // §728 (UI v3.5): the composition's order - Public finances, Economy by sector, the live series, Society - then the two kept sections
            // (More readings, Your policies). The floor is guarded on the page (V35.FloorGuarded, raised by the tab).
            EconomyState state = _playerCountry.State;
            DrawStatsPublicFinances(contentWidth);
            StatsSectionGap();
            DrawStatsEconomyBySector(contentWidth);
            StatsSectionGap();

            float? projectedGdp = null;
            float? projectedUnemployment = null;
            float? projectedApproval = null;
            if (_hasCachedPreview)
            {
                projectedGdp = state.GDP * (1f + _cachedGdpGrowthPercentRaw / 100f);   // D-ST (23a ⑫): the chart is REAL GDP, so its estimate is the real level at the real growth - never the nominal level on a real line (B6: real with real)
                projectedUnemployment = state.Unemployment + _cachedUnemploymentChangeRaw;
                projectedApproval = state.ApprovalRating + _cachedApprovalChangeRaw;
            }

            StatHistory history = _playerCountry.History;
            GUIStyle graphLabel = StatsGraphLabelStyle();

            // C-C4 (P-G4): where this government's enacted laws fall on the axis every series shares.
            // Computed ONCE for the whole grid - the six graphs plot the same quarterly cadence, so six
            // separate mappings would be six chances to disagree with each other.
            List<float> enactments = BuildEnactmentPositions(history.Gdp);

            // C-C9 (P-G1): the counterfactual's own history, read from the shadow world's matching
            // country. Null until a shadow exists or before it has two points, and each graph draws
            // nothing extra in that case rather than a flat line at zero.
            StatHistory shadowHistory = _shadowBaseline?.CountryFor(PlayerCountryId)?.History;

            int pages = 1;
            foreach (StatsChart c in StatsSlips.Domestic) { pages = Mathf.Max(pages, GraphRenderer.PagesFor(c.Series(history).Quarterly)); }
            DrawStatsPagedHead("Live series", "series:head", "series:pager", _statsSeriesSection, pages, contentWidth);
            GUILayout.Space(V35.Px(6f));
            var projections = new Dictionary<string, float?> { { "gdp", projectedGdp }, { "unemployment", projectedUnemployment }, { "approval", projectedApproval } };
            var cells = new List<System.Action>();
            foreach (StatsChart chart in StatsSlips.Domestic)
            {
                StatsChart c = chart;
                projections.TryGetValue(c.Id, out float? projected);
                float? threshold = c.Id == "unemployment" ? _playerCountry.EffectiveNaturalUnemploymentRate : c.Id == "debt" ? _playerCountry.ComfortableDebtToGdpPercent : (float?)null;
                string thresholdLabel = c.Id == "unemployment" ? "NAIRU" : c.Id == "debt" ? "comfortable" : null;
                // C-C9: each chart's own counterfactual series - the debt chart had been handed the shadow's GDP (billions on a ratio's axis, pinned to
                // its top edge); it reads the shadow's debt ratio now
                IReadOnlyList<float> shadow = shadowHistory == null ? null : c.Series(shadowHistory).Quarterly;
                cells.Add(() => DrawStatsChart(c, history, projected, graphLabel, threshold, thresholdLabel, enactments, shadow, _statsSeriesSection));
            }
            DrawStatsGraphGrid(contentWidth, cells);
            StatsSectionGap();
            DrawStatsSocietyTiles(contentWidth);
            StatsSectionGap();
            DrawStatsMoreReadings(contentWidth);
            StatsSectionGap();
            DrawImpactLedgerContent(contentWidth);
        }

        /// <summary>The graph instances by chart id - the fields the sheet has always drawn through (their caches and textures are per chart).</summary>
        private GraphRenderer StatsGraphFor(string id)
        {
            switch (id)
            {
                case "gdp": return _gdpGraph;
                case "unemployment": return _unemploymentGraph;
                case "inflation": return _inflationGraph;
                case "approval": return _approvalGraph;
                case "poverty": return _povertyGraph;
                case "debt": return _debtGraph;
                default: return _tradeBalanceGraph;
            }
        }

        /// <summary>One chart of the sheet (23a ⑩-⑭): drawn by the renderer in its reading's unit, and its head's parts anchored for their slips.</summary>
        private void DrawStatsChart(StatsChart c, StatHistory history, float? projected, GUIStyle graphLabel, float? threshold, string thresholdLabel,
            List<float> enactments, IReadOnlyList<float> shadow, GraphSection section)
        {
            MultiResolutionSeries series = c.Series(history);
            bool dated = series.LastQuarterlyDate.HasValue && series.LastQuarterlyDate.Value < _simulationManager.CurrentDate;
            GraphRenderer graph = StatsGraphFor(c.Id);
            graph.V35Head = StatsChartFaces(c.Id);   // §728: the head as a v3.5 tile (the icon, the figure, ▲▼, the name); put back after
            graph.Draw(c.Title, series.Quarterly, projected, graphLabel, c.HigherIsBetter, c.Money, threshold, thresholdLabel, enactments, shadow,
                reading: c.Unit, heldSeed: c.HeldSeed, section: section, datedHead: dated,
                breachOf: c.Rule.HasValue ? level => c.BreachOf(PlayerCountryId, level) : (System.Func<float, string>)null);   // §725: the head's warning, the slip's test
            graph.V35Head = null;
            string id = "chart:" + c.Id;
            StatsAnchor(graph.HeadDeltaRect, id + "/delta");
            StatsAnchor(graph.HeadVerdictRect, id + "/verdict");
            StatsAnchor(graph.HeadDatedRect, id + "/dated");
            if (c.Id == "gdp") { StatsAnchor(graph.HeadTitleRect, id + "/name"); }
            // §733: the threshold line's label opens the slip the book has always held for it (NAIRU, comfortable) - no anchor had registered it
            if (c.Id == "unemployment") { StatsAnchor(graph.ThresholdLabelRect, id + "/nairu"); }
            if (c.Id == "debt") { StatsAnchor(graph.ThresholdLabelRect, id + "/comfortable"); }
            if (c.HeldSeed.HasValue && graph.PlotAreaRect.width > 0f)
            {
                // 23a ⑭: the empty paper before the first live point opens the held seed's slip
                (int start, int end) = GraphRenderer.WindowOf(series.Quarterly.Count, section?.PageFromEnd ?? 0);
                int live = Mathf.Clamp(StatsReadings.FirstLiveIndex(series.Quarterly, c.HeldSeed) - start, 0, end - start);
                if (live > 0 && end - start > 1)
                {
                    Rect plot = graph.PlotAreaRect;
                    float w = live >= end - start ? plot.width : plot.width * (live - 0.5f) / (end - start - 1);
                    StatsAnchor(new Rect(plot.x, plot.y, Mathf.Max(1f, w), plot.height), id + "/held");
                }
            }
        }

        /// <summary>D-ST (23a ⑨): a section head carrying the section's ONE pager at its right - ◀ OLDER, ▶ NEWER, the disabled arrow in the hairline
        /// ink (THE WHOLE SERIES is the pager's disabled face, since the window already holds the series); its legend is the pager's slip.</summary>
        private void DrawStatsPagedHead(string caption, string headAnchor, string pagerAnchor, GraphSection section, int pages, float width)
        {
            // §728 (UI v3.5): the section head in the composition's capitals at the floor; the pager's arrows at the floor too.
            GUIStyle arrow = V35Serif(V35.Name, PoliSimTheme.TextPrimary, TextAnchor.MiddleCenter);
            float button = Mathf.Ceil(arrow.CalcSize(new GUIContent("◀")).x) + V35.Px(12f);
            Rect row = DrawStatsV35Head(caption, headAnchor, width, button * 2f + V35.Px(4f));
            section.PageFromEnd = Mathf.Clamp(section.PageFromEnd, 0, Mathf.Max(0, pages - 1));
            var older = new Rect(row.xMax - button * 2f - V35.Px(2f), row.y, button, row.height - 1f);
            var newer = new Rect(row.xMax - button, row.y, button, row.height - 1f);
            bool canOlder = section.PageFromEnd < pages - 1, canNewer = section.PageFromEnd > 0;
            if (Event.current.type == EventType.Repaint)
            {
                GUIStyle off = V35Serif(V35.Name, PoliSimTheme.Hairline, TextAnchor.MiddleCenter);
                PoliSimWidgets.MeasuredLabel(older, "◀", canOlder ? arrow : off);
                PoliSimWidgets.MeasuredLabel(newer, "▶", canNewer ? arrow : off);
            }
            StatsAnchor(new Rect(older.x, older.y, newer.xMax - older.x, older.height), pagerAnchor);
            if (canOlder && PoliSimWidgets.Button(older, GUIContent.none, GUIStyle.none)) { section.PageFromEnd++; }
            if (canNewer && PoliSimWidgets.Button(newer, GUIContent.none, GUIStyle.none)) { section.PageFromEnd--; }
        }

        /// <summary>
        /// C-C10 (P-G2): **the impact ledger — the gap between the live series and the counterfactual,
        /// attributed to the families of dials that opened it.**
        ///
        /// <para>⚠ <b>The interaction line is not a rounding term and is never hidden.</b> Measured
        /// before this was built (`COMPLETED.md` §106): over twelve turns of four dials the part of the
        /// divergence that belongs to no single family reaches <b>17.4 % on government debt</b>. A tax
        /// rise and a spending rise meet in the same GDP, so lines that appeared to sum exactly would be
        /// a false identity. Elias's ruling for this item is the wording of the last line here: an
        /// honest residual beats a false identity.</para>
        ///
        /// <para>Nothing is shown until the player has actually moved something — before that there is
        /// no divergence to explain. D-ST (23a ⑯): that held nothing is NIL, the em dash (19a) - the sentence that said it is the dash's slip.</para>
        /// </summary>
        private void DrawImpactLedgerContent(float contentWidth)
        {
            if (_impactLedger == null) { return; }

            // §728 (UI v3.5): KEPT (the composition draws no impact ledger; asked) - in the grammar: the section head, the rows on a card, the floor.
            DrawStatsV35Head("Your policies", "policies:head", contentWidth);
            GUILayout.Space(V35.Px(6f));
            _statsCardInnerWidth = contentWidth - V35.Px(V35.CardPadX) * 2f;
            GUILayout.BeginVertical(V35CardStyle(), GUILayout.Width(contentWidth));

            if (!_impactLedger.HasAnything)
            {
                GUIStyle dash = V35Mono(V35.Name, PoliSimTheme.TextMuted);
                Rect nil = GUILayoutUtility.GetRect(10f, V35.Px(V35.ListRow), GUILayout.ExpandWidth(true));
                if (Event.current.type == EventType.Repaint)
                {
                    var mark = new Rect(nil.x, nil.y, Mathf.Ceil(dash.CalcSize(new GUIContent("—")).x) + V35.Px(4f), nil.height);
                    PoliSimWidgets.MeasuredLabel(mark, "—", dash);
                    StatsAnchor(mark, "policies:nil");
                }
                GUILayout.EndVertical();
                return;
            }

            DrawImpactRow("GDP", "GDP", PolicyWebRenderer.GetStatUnit(StatNodeId.Gdp), true);
            DrawImpactRow("Unemployment", "Unemployment", null, false);
            DrawImpactRow("Inflation", "Inflation", null, false);
            DrawImpactRow("Approval rating", "ApprovalRating", null, true);
            DrawImpactRow("Poverty rate", "PovertyRate", null, false);
            // Debt is carried in the same money as GDP, so it takes GDP's declared unit rather than a
            // MoneyUnit literal here - a literal would be a second place that knows what the seed's
            // money is, which is how the P2 unit bug spread across 21 sites.
            DrawImpactRow("Government debt", "GovernmentDebt", PolicyWebRenderer.GetStatUnit(StatNodeId.Gdp), null);   // §725 (Elias's ruling): debt's move has no consensus direction
            GUILayout.EndVertical();
        }

        /// <summary>One stat's line: the divergence, then each family's share of it largest first, then
        /// the interaction. ⚠ A family whose share rounds away is dropped from the sentence rather than
        /// printed as a zero it is not - but the interaction is printed whatever its size, because its
        /// smallness is the reader's business as much as its largeness.</summary>
        private void DrawImpactRow(string label, string statField, MoneyUnit? unit, bool? higherIsBetter)
        {
            List<ImpactLine> lines = _impactLedger.LinesFor(_playerCountry, statField, out float divergence);

            string headline = FormatImpact(divergence, unit);
            var reasons = new System.Text.StringBuilder();
            for (int i = 0; i < lines.Count; i++)
            {
                bool isInteraction = i == lines.Count - 1;
                if (!isInteraction && Mathf.Abs(lines[i].Contribution) < ImpactRoundsAway(unit)) { continue; }

                if (reasons.Length > 0) { reasons.Append(" · "); }
                reasons.Append(lines[i].Family.ToUpperInvariant()).Append(' ').Append(FormatImpact(lines[i].Contribution, unit));
            }

            // §564 (2026-09-22): the family's row, not three body-serif labels in a line (Design's sitting, part A item 9) - the stat's name in the desk's body face, the gap
            // as a numeral in its own delta ink at the right, and the reasons as ONE caption line beneath in TextMuted, a row rule under each.
            // §728 (UI v3.5): the faces at the floor - the name in the serif at 16, the gap in the mono, the reasons wrapping under them (the floor is
            // never traded for one line); measured at the card's inside, which the layout event does not know.
            GUIStyle nameFace = V35Serif(V35.Name, PoliSimTheme.TextPrimary);
            GUIStyle figureFace = V35Mono(V35.Name, UiPalette.GetDeltaColor(divergence, higherIsBetter), bold: true, TextAnchor.MiddleRight);
            GUIStyle reasonFace = V35SerifWrapped(V35.Floor, PoliSimTheme.TextMuted);
            float width = Mathf.Max(1f, _statsCardInnerWidth);
            float line = Mathf.Ceil(nameFace.CalcSize(new GUIContent("Ag")).y);
            string reasonText = reasons.ToString();
            float reasonHeight = string.IsNullOrEmpty(reasonText) ? 0f : Mathf.Ceil(reasonFace.CalcHeight(new GUIContent(reasonText), width));
            Rect r = GUILayoutUtility.GetRect(10f, line + reasonHeight + V35.Px(8f), GUILayout.ExpandWidth(true));
            if (Event.current.type != EventType.Repaint) { return; }
            float figureWidth = figureFace.CalcSize(new GUIContent(headline)).x + V35.Px(4f);
            PoliSimWidgets.MeasuredLabel(new Rect(r.x, r.y, Mathf.Max(1f, r.width - figureWidth), line), label, nameFace);
            PoliSimWidgets.MeasuredLabel(new Rect(r.xMax - figureWidth, r.y, figureWidth, line), headline, figureFace);
            if (reasonHeight > 0f) { GUI.Label(new Rect(r.x, r.y + line + 1f, r.width, reasonHeight), reasonText, reasonFace); }
            PoliSimTheme.Rule(new Rect(r.x, r.yMax - 1f, r.width, 1f), V35.ListRule);
        }

        /// <summary>The threshold below which a contribution would print as a zero it is not. Money is
        /// carried in the seed's own billions, so a tenth of one is genuinely nothing; a rate's tenth of
        /// a point is not.</summary>
        private static float ImpactRoundsAway(MoneyUnit? unit) => unit.HasValue ? 0.1f : 0.005f;

        private static string FormatImpact(float value, MoneyUnit? unit)
        {
            if (unit.HasValue) { return UiFormat.Money(value, unit.Value, explicitPlus: true); }

            return value.ToString("+0.00;-0.00;0.00", CultureInfo.InvariantCulture) + " pts";
        }

        /// <summary>
        /// International statistics: the world map plus everything cross-country, including Trade -
        /// which absorbed the old peer sub-tab because trade IS international relations. Board 2a
        /// (2026-08-28) drops E24, the turn log that lived here (its content is the calendar's and
        /// the event card's now), and the "International" header with it - the sub-tab says it.
        /// <para>D-ST (23b, 2026-09-30): one heading face on the page - WORLD MAP, PAIR and TRADE take the section rule, as Domestic's heads do.</para>
        /// <para>§729 (UI v3.5): the composition's page (<see cref="DrawInternationalStatisticsV35"/>) - the map and trade cards, partners and peers -
        /// then the pair page, kept as built.</para>
        /// </summary>
        private void DrawInternationalStatisticsContent(float contentWidth)
        {
            DrawInternationalStatisticsV35(contentWidth);
        }

        /// <summary>
        /// Board 5a (D11 row 1, 2026-09-02): **the pair as ONE PAGE rather than a stack.** The home side reads right-to-left on the left, the partner
        /// left-to-right on the right, one label column down the centre, so a label is read once and the eye compares across it.
        ///
        /// <para>⚠ <b>ONLY WHAT THE MODEL HOLDS, and absence drawn as its own fact.</b> This model holds no bilateral relations state at all (`Country`
        /// has no relations field; a summit is an event, not a bond), so every pair page carries RELATIONS with ABSENT. <i>No trade link</i> draws
        /// ABSENT where the arrows would be while the tariffs still read; <i>trade of zero</i> draws the arrows at their minimum with the figure 0 - a
        /// different fact from "no link", never the same pixels. A row with one side is not drawn. Partner order is the CountryId enum's.</para>
        ///
        /// <para>D-ST (23b ④-⑬): the partner control is ONE line of the partners' codes on the PAIR head, the active one boxed (10b) - a click on a code
        /// is the step; the heads keep name and flag (HOME and PARTNER are the side each stands on, so their words are the heads' slips); zone and bloc
        /// print ONCE, as mirrored rows in LINKS; the head over the nine rows reads READINGS; each trade arrow starts on its own side; the tariffs are
        /// one mirrored row; the stance lanes name their ends; RELATIONS takes ABSENT. The chip's name beside each map marker is R-SP5's, Elias's
        /// ruling of 2026-08-28 - Design's Q7 would retire it on this map, and that is Elias's to confirm: the map is drawn as ruled.</para>
        /// </summary>
        /// <summary>The pair column's width on the board (380 of the 1280 board's px), scaled with the sheet.</summary>
        private float PairColumnWidth => StatsUnit(380f);

        /// <summary>The pair page's partners, in the CountryId enum's order.</summary>
        private List<Country> PairPartners()
        {
            var others = new List<Country>();
            foreach (CountryId id in (CountryId[])System.Enum.GetValues(typeof(CountryId)))
            {
                if (id == PlayerCountryId) { continue; }
                Country c = _world.GetCountry(id);
                if (c != null) { others.Add(c); }
            }
            return others;
        }

        /// <summary>The pair page's partner as it stands, or null with no other country.</summary>
        private Country PairPartner()
        {
            List<Country> others = PairPartners();
            if (others.Count == 0) { return null; }
            _internationalPageIndex = ((_internationalPageIndex % others.Count) + others.Count) % others.Count;
            return others[_internationalPageIndex];
        }

        private void DrawCountryPageContent()
        {
            List<Country> others = PairPartners();
            Country them = PairPartner();
            if (them == null) { return; }

            // 23b ⑥: PAIR on the section rule, the partners' codes on its right - one control line, the active code boxed, a click the step
            GUIStyle code = DeskCaption(9f, PoliSimTheme.TextSecondary, false, TextAnchor.MiddleCenter);
            GUIStyle active = DeskCaption(9f, PoliSimTheme.TextPrimary, true, TextAnchor.MiddleCenter);
            float codeWidth = Mathf.Ceil(active.CalcSize(new GUIContent("WW")).x) + StatsUnit(10f);
            float codeGap = StatsUnit(4f);
            float controlWidth = others.Count * codeWidth + (others.Count - 1) * codeGap;
            Rect head = DrawStatsSectionCaption("PAIR", controlWidth + StatsUnit(8f));
            StatsAnchor(new Rect(head.x, head.y, Mathf.Min(head.width - controlWidth, StatsUnit(160f)), head.height), "pair:head");
            for (int i = 0; i < others.Count; i++)
            {
                var r = new Rect(head.xMax - controlWidth + i * (codeWidth + codeGap), head.y, codeWidth, head.height - 2f);
                bool current = i == _internationalPageIndex;
                if (Event.current.type == EventType.Repaint)
                {
                    if (current)
                    {
                        PoliSimTheme.Rule(new Rect(r.x, r.y, r.width, 1f), PoliSimTheme.TextPrimary);
                        PoliSimTheme.Rule(new Rect(r.x, r.yMax - 1f, r.width, 1f), PoliSimTheme.TextPrimary);
                        PoliSimTheme.Rule(new Rect(r.x, r.y, 1f, r.height), PoliSimTheme.TextPrimary);
                        PoliSimTheme.Rule(new Rect(r.xMax - 1f, r.y, 1f, r.height), PoliSimTheme.TextPrimary);
                    }
                    PoliSimWidgets.MeasuredLabel(r, PairCountryTag(others[i].Id), current ? active : code);
                }
                StatsAnchor(r, "pair:code:" + others[i].Id);
                if (!current && PoliSimWidgets.Button(r, GUIContent.none, GUIStyle.none)) { _internationalPageIndex = i; }
            }
            GUILayout.Space(StatsUnit(6f));

            // 23b ⑦: the heads - name and flag, each on its own side
            GUILayout.BeginHorizontal();
            DrawPairIdentity(_playerCountry, left: true);
            GUILayout.FlexibleSpace();
            DrawPairIdentity(them, left: false);
            GUILayout.EndHorizontal();
            GUILayout.Space(StatsUnit(8f));

            GUILayout.BeginHorizontal();
            GUILayout.BeginVertical(GUILayout.ExpandWidth(true));
            DrawPairMirroredLedger(them);
            GUILayout.EndVertical();
            GUILayout.Space(StatsUnit(16f));
            GUILayout.BeginVertical(GUILayout.Width(PairColumnWidth));
            DrawPairLinks(them);
            GUILayout.Space(StatsUnit(8f));
            DrawPairStancePlate(them);
            GUILayout.Space(StatsUnit(8f));
            DrawPairRelations();
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
        }

        /// <summary>One head (23b ⑦): the flag and the name as a numeral, each on its own side; its role, zone and bloc are its slip's and LINKS'.</summary>
        private void DrawPairIdentity(Country country, bool left)
        {
            GUILayout.BeginHorizontal();
            if (left) { DrawPairFlag(country.Id); GUILayout.Space(StatsUnit(6f)); }
            GUILayout.Label(country.Name, DeskNumeral(16f, PoliSimTheme.TextPrimary, left ? TextAnchor.MiddleLeft : TextAnchor.MiddleRight));
            StatsAnchor(GUILayoutUtility.GetLastRect(), left ? "pair:home" : "pair:partner");
            if (!left) { GUILayout.Space(StatsUnit(6f)); DrawPairFlag(country.Id); }
            GUILayout.EndHorizontal();
        }

        private void DrawPairFlag(CountryId id)
        {
            float w = StatsUnit(30f);
            float h = Mathf.Round(w * 2f / 3f);
            Rect r = GUILayoutUtility.GetRect(w, h, GUILayout.Width(w), GUILayout.Height(h));
            Texture2D flag = IconLibrary.GetFlag(id);
            if (Event.current.type == EventType.Repaint && flag != null) { GUI.DrawTexture(new Rect(r.x, r.y + (r.height - h) * 0.5f, w, h), flag, ScaleMode.StretchToFill, true); }
        }

        /// <summary>The mirrored ledger (23b ⑧): one label column down the centre, the home figure on the left, the partner's on the right, under the head
        /// READINGS. A row with one side is omitted and a line says why.</summary>
        private void DrawPairMirroredLedger(Country them)
        {
            Rect head = DrawStatsSectionCaption("READINGS");
            StatsAnchor(new Rect(head.x, head.y, Mathf.Min(head.width, StatsUnit(160f)), head.height), "readings:head");
            GUILayout.Space(StatsUnit(3f));
            DrawPairMirrorRow("GDP", UiFormat.Money(_playerCountry.State.NominalGdp, MoneyUnit.Billions), UiFormat.Money(them.State.NominalGdp, MoneyUnit.Billions));
            DrawPairMirrorRow("UNEMPLOYMENT", StatsReadings.Rate(_playerCountry.State.Unemployment), StatsReadings.Rate(them.State.Unemployment));
            DrawPairMirrorRow("INFLATION", StatsReadings.Rate(_playerCountry.State.Inflation), StatsReadings.Rate(them.State.Inflation));
            DrawPairMirrorRow("APPROVAL RATING", UiFormat.Number(_playerCountry.State.ApprovalRating, 1), UiFormat.Number(them.State.ApprovalRating, 1));
            DrawPairMirrorRow("DEBT-TO-GDP", StatsReadings.Rate(_playerCountry.State.DebtToGdpRatio), StatsReadings.Rate(them.State.DebtToGdpRatio));
            DrawPairMirrorRow("BUDGET BALANCE", PairBudgetBalance(_playerCountry), PairBudgetBalance(them));
            DrawPairMirrorRow("CREDIT RATING", PairCreditRating(_playerCountry), PairCreditRating(them));
            DrawPairMirrorRow("POVERTY RATE", StatsReadings.Rate(_playerCountry.State.PovertyRate), StatsReadings.Rate(them.State.PovertyRate));

            bool mineIndependent = !CurrencySystem.SharesCurrencyZoneWithOthers(_playerCountry, _world);
            bool theirsIndependent = !CurrencySystem.SharesCurrencyZoneWithOthers(them, _world);
            if (mineIndependent && theirsIndependent)
            {
                DrawPairMirrorRow("CURRENCY STRENGTH", UiFormat.Number(_playerCountry.State.CurrencyStrength, 1), UiFormat.Number(them.State.CurrencyStrength, 1));
            }
            else
            {
                Country shared = mineIndependent ? them : _playerCountry;
                GUILayout.Space(StatsUnit(3f));
                GUILayout.Label("CURRENCY STRENGTH OMITTED: " + shared.Name.ToUpperInvariant() + " HAS NO INDEPENDENT CURRENCY, SO THE ROW HAS ONE SIDE AND IS NOT DRAWN",
                    DeskCaption(7.5f, PoliSimTheme.TextMuted, false, TextAnchor.MiddleCenter));
            }
        }

        /// <summary>The last closed year's balance as a share of that country's GDP - the same report the desk strip reads; a dash before any year has closed.</summary>
        private string PairBudgetBalance(Country country)
        {
            FiscalTurnReport last = _simulationManager.GetLastFiscalReport(country.Id);
            if (last == null || country.State.GDP <= 0f) { return "—"; }
            return StatsReadings.TrueMinus((last.BudgetBalance / country.State.NominalGdp * 100f).ToString("+0.0;-0.0;0.0", CultureInfo.InvariantCulture)) + "% GDP";
        }

        /// <summary>The standing rating (set by scheduled review); a dash until the first review - an unrated sovereign is not a top-rated one.</summary>
        private static string PairCreditRating(Country country) =>
            country.Rating != null && country.Rating.HasBeenReviewed ? CreditRatingSystem.Format(country.Rating.Rating) : "—";

        /// <summary>One mirrored row; returns its rect for a slip's anchor.</summary>
        private Rect DrawPairMirrorRow(string label, string mine, string theirs)
        {
            GUIStyle numeral = DeskNumeral(13f, PoliSimTheme.TextPrimary, TextAnchor.MiddleRight);
            GUIStyle numeralRight = DeskNumeral(13f, PoliSimTheme.TextPrimary, TextAnchor.MiddleLeft);
            GUIStyle caption = DeskCaption(8.5f, PoliSimTheme.TextMuted, false, TextAnchor.MiddleCenter);
            float height = Mathf.Ceil(numeral.CalcSize(new GUIContent("0")).y) + StatsUnit(4f);
            Rect row = GUILayoutUtility.GetRect(10f, height, GUILayout.ExpandWidth(true));
            if (Event.current.type != EventType.Repaint) { return row; }
            float labelWidth = Mathf.Min(StatsUnit(150f), row.width * 0.44f);
            float side = Mathf.Max(1f, (row.width - labelWidth) * 0.5f);
            PoliSimWidgets.MeasuredLabel(new Rect(row.x, row.y, side, row.height), mine, numeral);
            PoliSimWidgets.MeasuredLabel(new Rect(row.x + side, row.y, labelWidth, row.height), label, caption);
            PoliSimWidgets.MeasuredLabel(new Rect(row.x + side + labelWidth, row.y, side, row.height), theirs, numeralRight);
            PoliSimTheme.Rule(new Rect(row.x, row.yMax - 1f, row.width, 1f), PoliSimTheme.RuleRow);
            return row;
        }

        /// <summary>A bloc's short name - its words' initials (the European Union reads EU), or NIL's em dash for none.</summary>
        private string PairBloc(CountryId id)
        {
            foreach (TradeBloc b in _world.TradeBlocs)
            {
                if (!b.IsMember(id)) { continue; }
                var initials = new System.Text.StringBuilder();
                foreach (string w in b.Name.Split(' ')) { if (w.Length > 0 && char.IsUpper(w[0])) { initials.Append(w[0]); } }
                return initials.Length > 1 ? initials.ToString() : b.Name.ToUpperInvariant();
            }
            return "—";
        }

        /// <summary>LINKS (23b ⑦ ⑨ ⑩): trade from the map's own links as two arrows, each starting on its own side (home left, the partner right); the
        /// tariff each side charges the other as one mirrored row; bloc and currency as mirrored rows, each side's fact once - and the two absence states
        /// drawn apart.</summary>
        private void DrawPairLinks(Country them)
        {
            Rect head = DrawStatsSectionCaption("LINKS");
            StatsAnchor(new Rect(head.x, head.y, Mathf.Min(head.width, StatsUnit(160f)), head.height), "links:head");
            GUILayout.Space(StatsUnit(4f));
            TradePartner link = _playerCountry.TradePartners.Find(p => p.PartnerId == them.Id);
            if (link == null)
            {
                // no link: ABSENT where the arrows would be - not trade of zero; the tariffs still read
                float side = StatsUnit(14f);
                Rect r = GUILayoutUtility.GetRect(10f, side + StatsUnit(4f), GUILayout.ExpandWidth(true));
                var slot = new Rect(r.x + (r.width - side) * 0.5f, r.y + StatsUnit(2f), side, side);
                if (Event.current.type == EventType.Repaint) { DrawStateGlyph(slot, Symbol.Absent, PoliSimTheme.TextMuted); }
                StatsAnchor(slot, "links:none");
            }
            else
            {
                float max = Mathf.Max(link.ExportVolume, link.ImportVolume);
                Rect a = DrawPairTradeArrow(link.ExportVolume, max, fromLeft: true);
                StatsAnchor(a, max <= 0f ? "links:zero" : "links:out");
                Rect b = DrawPairTradeArrow(link.ImportVolume, max, fromLeft: false);
                StatsAnchor(b, max <= 0f ? "links:zero" : "links:in");
            }
            GUILayout.Space(StatsUnit(4f));
            Rect tariff = DrawPairMirrorRow("TARIFF", UiFormat.Number(TradeSystem.GetTariffRate(_playerCountry, them, _world.TradeBlocs), 1) + "%",
                UiFormat.Number(TradeSystem.GetTariffRate(them, _playerCountry, _world.TradeBlocs), 1) + "%");
            StatsAnchor(tariff, "links:tariff");
            StatsAnchor(DrawPairMirrorRow("BLOC", PairBloc(PlayerCountryId), PairBloc(them.Id)), "links:bloc");
            StatsAnchor(DrawPairMirrorRow("CURRENCY", EnergyLayer.CurrencyCode(PlayerCountryId), EnergyLayer.CurrencyCode(them.Id)), "links:currency");
        }

        /// <summary>One trade arrow (23b ⑨): the shaft from its own side - home's from the left edge pointing right, the partner's from the right edge
        /// pointing left - its length relative to the larger of the pair (a minimum for zero), the head, and the figure at the head; in the Trade area's
        /// ink. The direction's words are the arrow's slip. Returns the row's rect.</summary>
        private Rect DrawPairTradeArrow(float volume, float max, bool fromLeft)
        {
            GUIStyle figure = DeskNumeral(12f, PoliSimTheme.TextPrimary, fromLeft ? TextAnchor.MiddleLeft : TextAnchor.MiddleRight);
            float lane = Mathf.Ceil(figure.CalcSize(new GUIContent("0")).y) + StatsUnit(2f);
            Rect r = GUILayoutUtility.GetRect(10f, lane + StatsUnit(3f), GUILayout.ExpandWidth(true));
            if (Event.current.type != EventType.Repaint) { return r; }
            string text = UiFormat.Money(volume, MoneyUnit.Billions);
            float figureWidth = figure.CalcSize(new GUIContent(text)).x + StatsUnit(6f);
            float track = Mathf.Max(1f, r.width - figureWidth);
            float fraction = max > 0f ? volume / max : 0f;
            float length = Mathf.Max(track * 0.12f, track * fraction);
            float y = r.y + lane * 0.5f;
            float shaft = Mathf.Max(2f, StatsUnit(3f));
            float head = Mathf.Max(5f, StatsUnit(7f));
            Color ink = UiPalette.GetAreaColor(UiPalette.SystemArea.Trade);
            float x0 = fromLeft ? r.x : r.xMax - length;   // the shaft's left end
            PoliSimTheme.Rule(new Rect(fromLeft ? x0 : x0 + head, y - shaft * 0.5f, Mathf.Max(1f, length - head), shaft), ink);
            Color previous = GUI.color;
            GUI.color = ink;
            const int Steps = 5;
            for (int s = 0; s < Steps; s++)
            {
                float t = (s + 0.5f) / Steps;
                float half = head * 0.8f * (1f - t);
                float hx = fromLeft ? x0 + length - head + head * t : x0 + head - head * t;
                GUI.DrawTexture(new Rect(hx - head / Steps * 0.5f, y - half, head / Steps + 0.6f, half * 2f), Texture2D.whiteTexture);
            }
            GUI.color = previous;
            var figureRect = fromLeft
                ? new Rect(x0 + length + StatsUnit(4f), r.y, figureWidth, lane)
                : new Rect(x0 - StatsUnit(4f) - figureWidth, r.y, figureWidth, lane);
            PoliSimWidgets.MeasuredLabel(figureRect, text, figure);
            return r;
        }

        /// <summary>The labelled arrow the Trade policy tab's partner rows draw (its label above, the shaft from the left) - the pair page's arrow
        /// with its caption kept, since those rows carry no slips.</summary>
        private void DrawPairTradeArrow(string label, float volume, float max)
        {
            GUIStyle caption = DeskCaption(8f, PoliSimTheme.TextSecondary);
            float captionHeight = Mathf.Ceil(Mathf.Max(DeskCaptionHeight(caption), caption.CalcSize(new GUIContent(label)).y));
            Rect r = GUILayoutUtility.GetRect(10f, captionHeight, GUILayout.ExpandWidth(true));
            if (Event.current.type == EventType.Repaint) { PoliSimWidgets.MeasuredLabel(r, label, caption); }
            DrawPairTradeArrow(volume, max, fromLeft: true);
        }

        /// <summary>STANCE (23b ⑪): the two blends both sides sit on (PolicyStanceAxes - not the CHES positions, which are not on this page), each a
        /// centred lane with the two markers tagged and its ENDS NAMED, in the model's own words for what grows along it (22c's rule: a compass names
        /// its ends). 0–100 is the head's slip; the provenance is the dense line's.</summary>
        private void DrawPairStancePlate(Country them)
        {
            Rect head = DrawStatsSectionCaption("STANCE");
            StatsAnchor(new Rect(head.x, head.y, Mathf.Min(head.width, StatsUnit(160f)), head.height), "stance:head");
            GUILayout.Space(StatsUnit(4f));
            DrawPairStanceLane("FISCAL SIZE", "stance:fiscal", "SMALLER STATE", "LARGER STATE", PolicyStanceAxes.GetFiscalSizeAxisValue(_playerCountry), PolicyStanceAxes.GetFiscalSizeAxisValue(them), them);
            DrawPairStanceLane("REGULATION / WELFARE", "stance:regulation", "LESS REACH", "MORE REACH", PolicyStanceAxes.GetRegulationWelfareAxisValue(_playerCountry), PolicyStanceAxes.GetRegulationWelfareAxisValue(them), them);
        }

        private void DrawPairStanceLane(string axis, string anchor, string lowEnd, string highEnd, float mine, float theirs, Country them)
        {
            GUIStyle caption = DeskCaption(8f, PoliSimTheme.TextSecondary);
            GUIStyle endFace = DeskCaption(7.5f, PoliSimTheme.TextMuted);
            GUIStyle endFaceRight = DeskCaption(7.5f, PoliSimTheme.TextMuted, false, TextAnchor.MiddleRight);
            GUIStyle tag = DeskCaption(8f, PoliSimTheme.TextPrimary, true, TextAnchor.MiddleCenter);
            float captionHeight = Mathf.Ceil(DeskCaptionHeight(caption));
            float endHeight = Mathf.Ceil(DeskCaptionHeight(endFace));
            float lane = StatsUnit(18f);
            float tagHeight = captionHeight;   // the markers' tags get a full caption row above the track (the first film squeezed them into half a lane)
            // P5-8 (board 6b row 7, 2026-09-03): when the two sides read within a point the tags STACK above the lane - two tag rows,
            // reserved always so the lane's geometry does not change with the readings.
            Rect r = GUILayoutUtility.GetRect(10f, captionHeight + tagHeight * 2f + lane + endHeight + StatsUnit(4f), GUILayout.ExpandWidth(true));
            if (Event.current.type != EventType.Repaint) { return; }
            PoliSimWidgets.MeasuredLabel(new Rect(r.x, r.y, r.width, captionHeight), axis, caption);
            StatsAnchor(new Rect(r.x, r.y, Mathf.Min(r.width, caption.CalcSize(new GUIContent(axis)).x), captionHeight), anchor);
            float trackY = r.y + captionHeight + tagHeight * 2f + lane * 0.5f;
            float tagWidth = StatsUnit(22f);
            float x0 = r.x + tagWidth * 0.5f;
            float span = Mathf.Max(1f, r.width - tagWidth);
            PoliSimTheme.Rule(new Rect(x0, trackY - 0.5f, span, 1f), PoliSimTheme.Hairline);
            PoliSimTheme.Rule(new Rect(x0 + span * 0.5f, trackY - lane * 0.25f, 1f, lane * 0.5f), PoliSimTheme.HairlineStrong);
            // 23b ⑪: the ends named, under the lane's two ends
            float endY = trackY + lane * 0.5f;
            PoliSimWidgets.MeasuredLabel(new Rect(x0, endY, span * 0.5f, endHeight), lowEnd, endFace);
            PoliSimWidgets.MeasuredLabel(new Rect(x0 + span * 0.5f, endY, span * 0.5f, endHeight), highEnd, endFaceRight);
            float mineX = x0 + span * Mathf.Clamp01(mine / 100f);
            float theirsX = x0 + span * Mathf.Clamp01(theirs / 100f);
            bool stacked = Mathf.Abs(mine - theirs) < 1f;   // the threshold: 1.0 point on the axis's own scale (board 6b row 7)
            float midX = (mineX + theirsX) * 0.5f;
            // Own side nearest the rule, the partner above it; a 1 px leader in each tag's ink at 50 % joins tag to marker. The markers do not move.
            DrawPairStanceMarker(mineX, trackY, PairCountryTag(PlayerCountryId), tagWidth, lane, tag, PoliSimTheme.TextPrimary, stacked ? midX : mineX, 0, stacked);
            DrawPairStanceMarker(theirsX, trackY, PairCountryTag(them.Id), tagWidth, lane, tag, PoliSimTheme.TextSecondary, stacked ? midX : theirsX, stacked ? 1 : 0, stacked);
        }

        private void DrawPairStanceMarker(float x, float y, string tagText, float tagWidth, float lane, GUIStyle tag, Color ink, float tagX, int tagRow, bool leader)
        {
            float dot = Mathf.Max(4f, StatsUnit(6f));
            PoliSimTheme.Rule(new Rect(x - dot * 0.5f, y - dot * 0.5f, dot, dot), ink);
            GUIStyle inked = new GUIStyle(tag);
            inked.normal.textColor = ink;
            float tagHeight = Mathf.Ceil(DeskCaptionHeight(tag));
            float tagTop = y - lane * 0.5f - tagHeight * (tagRow + 1);
            if (leader)
            {
                // The leader: 1 px, the tag's ink at 50 %, from the marker's top to the tag's foot at the tag's own x.
                Color half = new Color(ink.r, ink.g, ink.b, ink.a * 0.5f);
                PoliSimTheme.Rule(new Rect(x - 0.5f, tagTop + tagHeight, 1f, Mathf.Max(1f, y - dot * 0.5f - (tagTop + tagHeight))), half);
                if (Mathf.Abs(tagX - x) > 1f) { PoliSimTheme.Rule(new Rect(Mathf.Min(x, tagX), tagTop + tagHeight - 0.5f, Mathf.Abs(tagX - x), 1f), half); }
            }
            PoliSimWidgets.MeasuredLabel(new Rect(tagX - tagWidth * 0.5f, tagTop, tagWidth, tagHeight), tagText, inked);
        }

        /// <summary>Two-letter country tags for a marker (the ISO forms of the six).</summary>
        private static string PairCountryTag(CountryId id)
        {
            switch (id)
            {
                case CountryId.Sweden: return "SE";
                case CountryId.Germany: return "DE";
                case CountryId.France: return "FR";
                case CountryId.Italy: return "IT";
                case CountryId.Poland: return "PL";
                case CountryId.USA: return "US";
                default: return id.ToString().Substring(0, 2).ToUpperInvariant();
            }
        }

        /// <summary>RELATIONS (23b ⑫): the model holds no bilateral relations state - the word and ABSENT (19a), its sentence the glyph's slip.</summary>
        private void DrawPairRelations()
        {
            GUIStyle face = DeskCaption(8.5f, PoliSimTheme.TextSecondary);
            float height = Mathf.Ceil(DeskCaptionHeight(face)) + StatsUnit(6f);
            Rect r = GUILayoutUtility.GetRect(10f, height, GUILayout.ExpandWidth(true));
            float side = Mathf.Min(height, StatsUnit(14f));
            var slot = new Rect(r.xMax - side, r.y + (r.height - side) * 0.5f, side, side);
            if (Event.current.type == EventType.Repaint)
            {
                PoliSimWidgets.MeasuredLabel(new Rect(r.x, r.y, r.width - side - StatsUnit(6f), r.height), "RELATIONS", face);
                PoliSimTheme.Rule(new Rect(r.x, r.yMax - 1f, r.width, 1f), PoliSimTheme.RuleRow);
                DrawStateGlyph(slot, Symbol.Absent, PoliSimTheme.TextMuted);
            }
            StatsAnchor(slot, "relations");
        }
    }
}
