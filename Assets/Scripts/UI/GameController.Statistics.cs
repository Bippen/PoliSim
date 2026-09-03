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
    /// </summary>
    public partial class GameController
    {
        /// <summary>One of the ten headline readings - ONE list for the Desk's chip strip and the Statistics plates, so the two can never disagree about the tenth reading (the tiles' list of old, shared).</summary>
        private readonly struct HeadlineReading
        {
            public readonly string Label;
            public readonly string Value;
            public readonly string Delta;
            public readonly bool DeltaIsGood;
            public readonly IReadOnlyList<float> Series;

            public HeadlineReading(string label, string value, string delta, bool deltaIsGood, IReadOnlyList<float> series)
            {
                Label = label;
                Value = value;
                Delta = delta;
                DeltaIsGood = deltaIsGood;
                Series = series;
            }
        }

        /// <summary>P2-1.4 (2026-09-02): the Budget screen's persistent header - the five fiscal readings a draft is
        /// judged against, every one derived: the closed year's balance, revenue and spending from the fiscal report
        /// (a dash before the first close - a figure no year has computed is stated, never drawn), the debt stock and
        /// its ratio from the live state. Drawn in the chip-strip idiom above the Budget's columns, outside their
        /// scroll, so it is on screen whatever the columns show.</summary>
        private List<HeadlineReading> BuildFiscalReadings()
        {
            EconomyState state = _playerCountry.State;
            StatHistory history = _playerCountry.History;
            FiscalTurnReport lastYear = _simulationManager.GetLastFiscalReport(PlayerCountryId);
            return new List<HeadlineReading>
            {
                // P3-C2 (2026-09-03): the year's balance beside last year's - the preview's own figure (revenue minus spending on the previewed
                // year, WITH the draft; a turn is a year), and at year one last year is the seed and says so: the seeded accumulator is
                // the seed's standing balance, and no year has closed to replace it.
                lastYear != null
                    ? new HeadlineReading(_simulationManager.CurrentTurn == 0 ? "Balance · last year · the seed" : "Balance · last year", UiFormat.MoneyDelta(lastYear.BudgetBalance, MoneyUnit.Billions), null, false, history?.BudgetBalanceAnnual)
                    : new HeadlineReading("Balance · the seed (no year closed)", UiFormat.MoneyDelta(state.Budget, MoneyUnit.Billions), null, false, null),
                new HeadlineReading("Balance · this year · projected", _cachedPreview != null ? UiFormat.MoneyDelta(_cachedPreview.RevenueEstimate - _cachedPreview.SpendingEstimate, MoneyUnit.Billions) : "-",
                    _cachedPreview != null && _cachedPreviewWithoutDraft != null && !Mathf.Approximately(_cachedPreview.RevenueEstimate - _cachedPreview.SpendingEstimate, _cachedPreviewWithoutDraft.RevenueEstimate - _cachedPreviewWithoutDraft.SpendingEstimate)
                        ? "WITH THE DRAFT" : null, true, null),
                new HeadlineReading("Government debt", UiFormat.Money(state.GovernmentDebt, MoneyUnit.Billions), null, false, null),
                new HeadlineReading("Debt-to-GDP", UiFormat.Number(state.DebtToGdpRatio, 1) + "%", null, false, history?.DebtToGdpRatio.Quarterly),
                new HeadlineReading("Revenue · last year", lastYear != null ? UiFormat.Money(lastYear.Revenue, MoneyUnit.Billions) : "-", null, false, null),
                new HeadlineReading("Spending · last year", lastYear != null ? UiFormat.Money(lastYear.TotalSpending, MoneyUnit.Billions) : "-", null, false, null),
            };
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
                new HeadlineReading("GDP", UiFormat.Money(state.GDP, MoneyUnit.Billions), _lastGrowthPercent.ToString("+0.00;-0.00;0", CultureInfo.InvariantCulture) + "%", _lastGrowthPercent >= 0f, history?.Gdp.Quarterly),
                new HeadlineReading("Unemployment", UiFormat.Number(state.Unemployment, 2) + "%", null, false, history?.Unemployment.Quarterly),
                new HeadlineReading("Inflation", UiFormat.Number(state.Inflation, 2) + "%", null, false, history?.Inflation.Quarterly),
                new HeadlineReading("Approval Rating", UiFormat.Number(state.ApprovalRating, 1), null, false, history?.ApprovalRating.Quarterly)
            };

            if (PlayerHasIndependentCurrency())
            {
                readings.Add(new HeadlineReading("Currency Strength", UiFormat.Number(state.CurrencyStrength, 1), null, false, null));
            }

            readings.Add(new HeadlineReading("Poverty Rate", UiFormat.Number(state.PovertyRate, 1) + "%", null, false, history?.PovertyRate.Quarterly));
            readings.Add(new HeadlineReading("Government Debt", UiFormat.Money(state.GovernmentDebt, MoneyUnit.Billions), null, false, null));
            readings.Add(new HeadlineReading("Debt-to-GDP", UiFormat.Number(state.DebtToGdpRatio, 1) + "%", null, false, history?.DebtToGdpRatio.Quarterly));

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
            readings.Add(new HeadlineReading("Budget Balance", lastYear != null ? UiFormat.MoneyDelta(lastYear.BudgetBalance, MoneyUnit.Billions) : "-", null, false, history?.BudgetBalanceAnnual));
            return readings;
        }

        // ------------------------------------------------------------------------------------------
        // The sheet's measures: the board's px at 720, scaled by the window height - DeskPx for type
        // (floored at D4's 9), StatsUnit for lengths - the same law the Desk draws by (R-B10).
        // ------------------------------------------------------------------------------------------
        private static float StatsUnit(float boardPx) => Mathf.Round(boardPx * Screen.height / DeskBoardHeight);

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
                if (!division.Passed) { continue; }

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
                PoliSimTheme.Rule(new Rect(row.x, row.yMax - 1f, row.width, 1f), PoliSimTheme.HairlineStrong);
            }

            return row;
        }

        private static void StatsSectionGap()
        {
            GUILayout.Space(StatsUnit(14f));
        }

        /// <summary>Board 2a's ten headline readings as compact plates in a 5-column grid: caption 7.5 in the muted ink, numeral 19 bold, the GDP delta or the outlook as mono 8 beneath; no keyline (the area key is the ledger rows' and the graphs', not a plate's - R-B5's reasoning), no 9-slice plate sprite (at this height its baked shadow ate the caption).</summary>
        private void DrawStatsHeadlinePlates(float contentWidth)
        {
            List<HeadlineReading> readings = BuildHeadlineReadings();
            const int columns = 5;
            float gap = StatsUnit(6f);
            float padX = StatsUnit(8f);
            float padY = StatsUnit(5f);
            GUIStyle caption = DeskCaption(7.5f, PoliSimTheme.TextMuted);
            GUIStyle numeral = DeskNumeral(19f, PoliSimTheme.TextPrimary, TextAnchor.MiddleLeft);
            float captionHeight = Mathf.Ceil(DeskCaptionHeight(caption));
            float numeralHeight = Mathf.Ceil(numeral.CalcSize(new GUIContent("0")).y);
            bool anyDelta = false;
            for (int i = 0; i < readings.Count; i++)
            {
                anyDelta |= !string.IsNullOrEmpty(readings[i].Delta);
            }

            float deltaHeight = anyDelta ? Mathf.Ceil(DeskCaptionHeight(DeskCaption(8f, PoliSimTheme.Neutral, bold: true))) : 0f;
            float plateHeight = padY * 2f + captionHeight + StatsUnit(2f) + numeralHeight + deltaHeight;
            int rows = Mathf.CeilToInt(readings.Count / (float)columns);
            float totalHeight = rows * plateHeight + (rows - 1) * gap;
            Rect grid = GUILayoutUtility.GetRect(contentWidth, totalHeight, GUILayout.Width(contentWidth), GUILayout.Height(totalHeight));
            if (Event.current.type != EventType.Repaint)
            {
                return;
            }

            float plateWidth = (grid.width - gap * (columns - 1)) / columns;
            for (int i = 0; i < readings.Count; i++)
            {
                HeadlineReading reading = readings[i];
                var plate = new Rect(grid.x + (i % columns) * (plateWidth + gap), grid.y + (i / columns) * (plateHeight + gap), plateWidth, plateHeight);
                PoliSimTheme.RoundedCard(plate, PoliSimTheme.Tile, PoliSimTheme.Hairline, 0f);
                var inner = new Rect(plate.x + padX, plate.y + padY, plate.width - padX * 2f, plate.height - padY * 2f);
                PoliSimWidgets.MeasuredLabel(new Rect(inner.x, inner.y, inner.width, captionHeight), reading.Label.ToUpperInvariant(), caption);
                PoliSimWidgets.MeasuredLabel(new Rect(inner.x, inner.y + captionHeight + StatsUnit(2f), inner.width, numeralHeight), reading.Value, numeral);
                if (!string.IsNullOrEmpty(reading.Delta))
                {
                    GUIStyle delta = DeskCaption(8f, UiPalette.GetDeltaColor(reading.DeltaIsGood ? 1f : -1f, higherIsBetter: true), bold: true);
                    PoliSimWidgets.MeasuredLabel(new Rect(inner.x, inner.yMax - deltaHeight, inner.width, deltaHeight), reading.Delta, delta);
                }
            }
        }

        /// <summary>
        /// Board 2a: the fiscal shares of GDP on ONE axis - tax burden, spending, the deficit and the
        /// primary balance as bars to a printed axis (the group's maximum rounded up to the next 10 %,
        /// never below the board's 30 %), each with its figure; a row no closed year has computed yet
        /// says so instead of drawing a track at zero (an empty track IS the wrong number); GDP per
        /// capita beneath as a bare level - currency per person, no denominator, no gauge (§A.9b, E2
        /// absorbed). The deficit rows are NAMED by their sign ("Surplus" at 4.8 %, never "Deficit:
        /// −4.8 %"), and a positive deficit reads in the bad ink - the sign convention is the opposite
        /// of the budget balance's, so the ink follows the meaning.
        /// </summary>
        private void DrawStatsFiscalPosition(float contentWidth)
        {
            FiscalTurnReport report = _simulationManager.GetLastFiscalReport(PlayerCountryId);
            float? tax = DerivedStats.TaxBurdenPercentOfGdp(_playerCountry, report);
            float? spending = DerivedStats.SpendingPercentOfGdp(_playerCountry, report);
            float? deficit = DerivedStats.DeficitPercentOfGdp(_playerCountry, report);
            float? primary = DerivedStats.PrimaryDeficitPercentOfGdp(_playerCountry, report);
            float? perCapita = DerivedStats.GdpPerCapita(_playerCountry);

            float axisMax = 30f;
            foreach (float? v in new[] { tax, spending, deficit, primary })
            {
                if (v.HasValue && Mathf.Abs(v.Value) > axisMax)
                {
                    axisMax = Mathf.Ceil(Mathf.Abs(v.Value) / 10f) * 10f;
                }
            }

            Color fiscalInk = UiPalette.GetAreaColor(UiPalette.SystemArea.Fiscal);
            var rows = new List<(string name, float? value, Color ink)>
            {
                ("Tax burden", tax, fiscalInk),
                ("Government spending", spending, fiscalInk),
                (deficit.HasValue && deficit.Value < 0f ? "Surplus" : "Deficit",
                    deficit.HasValue ? Mathf.Abs(deficit.Value) : (float?)null,
                    deficit.HasValue ? UiPalette.GetDeltaColor(deficit.Value, higherIsBetter: false) : fiscalInk),
                (primary.HasValue && primary.Value < 0f ? "Primary surplus" : "Primary deficit",
                    primary.HasValue ? Mathf.Abs(primary.Value) : (float?)null,
                    primary.HasValue ? UiPalette.GetDeltaColor(primary.Value, higherIsBetter: false) : fiscalInk)
            };

            DrawStatsSectionCaption($"FISCAL POSITION — SHARES OF GDP · ONE AXIS TO {axisMax.ToString("0", CultureInfo.InvariantCulture)}%");

            float rowHeight = StatsUnit(22f);
            float labelWidth = StatsUnit(120f);
            float valueWidth = StatsUnit(44f);
            float gap = StatsUnit(8f);
            float barHeight = StatsUnit(11f);
            GUIStyle label = DeskBody(12f, PoliSimTheme.TextPrimary);
            GUIStyle value = DeskCaption(10f, PoliSimTheme.TextPrimary, false, TextAnchor.MiddleRight);
            GUIStyle note = DeskCaption(7.5f, PoliSimTheme.TextMuted);
            float axisRowHeight = Mathf.Ceil(DeskCaptionHeight(note)) + StatsUnit(2f);
            float totalHeight = rows.Count * rowHeight + axisRowHeight + rowHeight;
            Rect block = GUILayoutUtility.GetRect(contentWidth, totalHeight, GUILayout.Width(contentWidth), GUILayout.Height(totalHeight));
            if (Event.current.type != EventType.Repaint)
            {
                return;
            }

            float barX = block.x + labelWidth + gap;
            float barWidth = Mathf.Max(1f, block.width - labelWidth - valueWidth - gap * 2f);
            float y = block.y;
            for (int i = 0; i < rows.Count; i++)
            {
                var row = rows[i];
                PoliSimWidgets.MeasuredLabel(new Rect(block.x, y, labelWidth, rowHeight), row.name, label);
                if (row.value.HasValue)
                {
                    var track = new Rect(barX, y + (rowHeight - barHeight) * 0.5f, barWidth, barHeight);
                    PoliSimTheme.Rule(track, PoliSimTheme.BarTrack);
                    PoliSimTheme.Rule(new Rect(track.x, track.y, track.width * Mathf.Clamp01(row.value.Value / axisMax), track.height), row.ink);
                    PoliSimWidgets.MeasuredLabel(new Rect(block.xMax - valueWidth, y, valueWidth, rowHeight), UiFormat.Number(row.value.Value, 1) + "%", value);
                }
                else
                {
                    PoliSimWidgets.MeasuredLabel(new Rect(barX, y, barWidth + gap + valueWidth, rowHeight), "NOT YET COMPUTED — ADVANCE A YEAR", note);
                }

                y += rowHeight;
            }

            var ticks = new System.Text.StringBuilder("0");
            for (float t = 10f; t <= axisMax + 0.01f; t += 10f)
            {
                ticks.Append(" · ").Append(t.ToString("0", CultureInfo.InvariantCulture)).Append('%');
            }

            ticks.Append(" OF GDP");
            PoliSimWidgets.MeasuredLabel(new Rect(barX, y, barWidth + gap + valueWidth, axisRowHeight), ticks.ToString(), note);
            y += axisRowHeight;

            PoliSimWidgets.MeasuredLabel(new Rect(block.x, y, labelWidth, rowHeight), "GDP per capita", label);
            PoliSimWidgets.MeasuredLabel(new Rect(barX, y, barWidth, rowHeight), perCapita.HasValue ? "LEVEL · NO GAUGE" : "NO POPULATION", note);
            PoliSimWidgets.MeasuredLabel(new Rect(block.xMax - valueWidth, y, valueWidth, rowHeight), perCapita.HasValue ? UiFormat.Money(perCapita.Value, MoneyUnit.Thousands) : "n/a", value);
        }

        /// <summary>Board 2a: the sector shares of GDP as ONE stacked distribution bar in the categorical eight (the shares normalised to their own sum, so the bar is always the whole) over a two-column legend - swatch, name, share - replacing eight gauges each to its own 100 %.</summary>
        private void DrawStatsSectorShares(float contentWidth)
        {
            List<(SectorType Type, float SharePercent)> shares = DerivedStats.SectorSharesOfGdp(_playerCountry);
            DrawStatsSectionCaption("SECTOR SHARES OF GDP — ONE DISTRIBUTION");
            GUIStyle note = DeskCaption(7.5f, PoliSimTheme.TextMuted);
            if (shares.Count == 0)
            {
                Rect empty = GUILayoutUtility.GetRect(contentWidth, StatsUnit(22f), GUILayout.Width(contentWidth));
                if (Event.current.type == EventType.Repaint)
                {
                    PoliSimWidgets.MeasuredLabel(empty, "NOT TRACKED FOR THIS COUNTRY", note);
                }

                return;
            }

            float barHeight = StatsUnit(22f);
            float legendGap = StatsUnit(8f);
            float legendPitch = StatsUnit(16f);
            int legendRows = Mathf.CeilToInt(shares.Count / 2f);
            float totalHeight = barHeight + legendGap + legendRows * legendPitch;
            Rect block = GUILayoutUtility.GetRect(contentWidth, totalHeight, GUILayout.Width(contentWidth), GUILayout.Height(totalHeight));
            if (Event.current.type != EventType.Repaint)
            {
                return;
            }

            float total = 0f;
            for (int i = 0; i < shares.Count; i++)
            {
                total += Mathf.Max(0f, shares[i].SharePercent);
            }

            var bar = new Rect(block.x, block.y, block.width, barHeight);
            PoliSimTheme.Rule(bar, PoliSimTheme.BarTrack);
            float x = bar.x;
            for (int i = 0; i < shares.Count && total > 0f; i++)
            {
                float w = bar.width * Mathf.Max(0f, shares[i].SharePercent) / total;
                PoliSimTheme.Rule(new Rect(x, bar.y, w, bar.height), UiPalette.GetCategoricalColor(i));
                if (i > 0)
                {
                    PoliSimTheme.Rule(new Rect(Mathf.Round(x), bar.y, 1f, bar.height), PoliSimTheme.Card);
                }

                x += w;
            }

            GUIStyle name = DeskBody(11f, PoliSimTheme.TextPrimary);
            GUIStyle share = DeskCaption(9.5f, PoliSimTheme.TextSecondary, false, TextAnchor.MiddleRight);
            float swatch = StatsUnit(7f);
            float columnGap = StatsUnit(13f);
            float columnWidth = (block.width - columnGap) / 2f;
            float shareWidth = StatsUnit(44f);
            float swatchGap = StatsUnit(6f);
            for (int i = 0; i < shares.Count; i++)
            {
                float cx = block.x + (i % 2) * (columnWidth + columnGap);
                float cy = bar.yMax + legendGap + (i / 2) * legendPitch;
                PoliSimTheme.Rule(new Rect(cx, cy + (legendPitch - swatch) * 0.5f, swatch, swatch), UiPalette.GetCategoricalColor(i));
                float nameX = cx + swatch + swatchGap;
                // Spaced, NOT Of: SectorType.Energy resolves through the curated policy table to "Energy
                // (Spending)", a discretionary spending line rather than an economic sector.
                PoliSimWidgets.MeasuredLabel(new Rect(nameX, cy, Mathf.Max(1f, columnWidth - swatch - swatchGap - shareWidth), legendPitch), DisplayName.Spaced(shares[i].Type.ToString()), name);
                PoliSimWidgets.MeasuredLabel(new Rect(cx + columnWidth - shareWidth, cy, shareWidth, legendPitch), UiFormat.Number(shares[i].SharePercent, 1) + "%", share);
            }
        }

        /// <summary>Board 2a's 3-column graph grid: each cell is a GUILayout column one third of the content width wide; the renderer's own title row, pager and plot stack inside it. Three per row, the last row's remainder left open.</summary>
        private void DrawStatsGraphGrid(float contentWidth, List<System.Action> cells)
        {
            float gap = StatsUnit(13f);
            float column = Mathf.Max(1f, (contentWidth - gap * 2f) / 3f);
            for (int start = 0; start < cells.Count; start += 3)
            {
                GUILayout.BeginHorizontal();
                for (int i = start; i < start + 3 && i < cells.Count; i++)
                {
                    if (i > start)
                    {
                        GUILayout.Space(gap);
                    }

                    GUILayout.BeginVertical(GUILayout.Width(column));
                    cells[i]();
                    GUILayout.EndVertical();
                }

                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
                GUILayout.Space(StatsUnit(8f));
            }
        }

        /// <summary>One Society row's reading: the name, a 0..1 gauge fill or −1, the kept history for a row-end sparkline or null, the figure, the unit caption, the ink; Absent marks a row that is drawn as absent by ruling (no figure, never a zero).</summary>
        private readonly struct SocietyReading
        {
            public readonly string Name;
            public readonly float Fill;
            public readonly IReadOnlyList<float> Series;
            public readonly string Value;
            public readonly string Unit;
            public readonly Color Ink;
            public readonly bool Absent;

            public SocietyReading(string name, float fill, IReadOnlyList<float> series, string value, string unit, Color ink, bool absent = false)
            {
                Name = name;
                Fill = fill;
                Series = series;
                Value = value;
                Unit = unit;
                Ink = ink;
                Absent = absent;
            }
        }

        /// <summary>
        /// Board 2a: the Society rows in two columns - name 12, then a 70×8 gauge for a share (youth
        /// unemployment, Gini on its 0–100 scale, homeownership), a 44×13 row-end sparkline for an
        /// index or level that keeps a history (real wages, house prices, productivity - a base-100
        /// index is unbounded by construction and any fill denominator an invented ceiling, §A.9b, so
        /// its own history is the honest instrument), nothing for a level that keeps none (life
        /// expectancy); the figure in mono 10.5; the unit as a muted caption. The housing three keep
        /// their asymmetry (overburden first for the EU five, homeownership primary for the USA) and
        /// the USA's overburden row is ABSENT by ruling, drawn as absent - a name and the ruling, no
        /// figure, never a zero: drawing "0.0%" would fabricate a figure no source publishes.
        /// </summary>
        private void DrawStatsSocietyRows(float contentWidth)
        {
            EconomyState state = _playerCountry.State;
            StatHistory history = _playerCountry.History;
            Color labor = UiPalette.GetAreaColor(UiPalette.SystemArea.Labor);
            Color welfare = UiPalette.GetAreaColor(UiPalette.SystemArea.Welfare);
            bool tracksOverburden = _playerCountry.TracksHousingOverburden;
            var rows = new List<SocietyReading>
            {
                new SocietyReading("Youth unemployment", state.YouthUnemployment / 100f, null, UiFormat.Number(state.YouthUnemployment, 1) + "%", "OF YOUTH LABOR FORCE", labor),
                new SocietyReading("Life expectancy", -1f, null, UiFormat.Number(state.LifeExpectancy, 1), "YEARS AT BIRTH", welfare),
                new SocietyReading("Income inequality (Gini)", state.Gini / 100f, null, UiFormat.Number(state.Gini, 1), "0–100 SCALE", welfare),
                new SocietyReading("Real wages", -1f, history?.RealWageIndex.Quarterly, UiFormat.Number(state.RealWageIndex, 1), "INDEX · 100 = TERM START", labor),
                new SocietyReading("Productivity", -1f, history?.Productivity.Quarterly, UiFormat.Number(state.Productivity, 1), "$/HOUR (PPP) · OWN PAST", labor),
                tracksOverburden
                    ? new SocietyReading("Housing overburden", state.HousingOverburden / 100f, null, UiFormat.Number(state.HousingOverburden, 1) + "%", ">40% OF INCOME ON HOUSING", welfare)
                    : new SocietyReading("Housing overburden", -1f, null, null, "ABSENT BY RULING · NOT ZERO", welfare, absent: true),
                new SocietyReading("Homeownership", state.Homeownership / 100f, null, UiFormat.Number(state.Homeownership, 1) + "%", tracksOverburden ? "OF HOUSEHOLDS" : "OF HOUSEHOLDS · PRIMARY", welfare),
                new SocietyReading("House prices", -1f, history?.HousePriceIndex.Quarterly, UiFormat.Number(state.HousePriceIndex, 1), "INDEX · 100 = TERM START", welfare)
            };

            DrawStatsSectionCaption("SOCIETY — SHARES AS GAUGES · INDICES AND LEVELS WITH THEIR KEPT HISTORIES");
            float rowHeight = StatsUnit(22f);
            int gridRows = Mathf.CeilToInt(rows.Count / 2f);
            float totalHeight = gridRows * rowHeight;
            Rect block = GUILayoutUtility.GetRect(contentWidth, totalHeight, GUILayout.Width(contentWidth), GUILayout.Height(totalHeight));
            if (Event.current.type != EventType.Repaint)
            {
                return;
            }

            float columnGap = StatsUnit(13f);
            float columnWidth = (block.width - columnGap) / 2f;
            float nameWidth = StatsUnit(170f);
            float instrumentWidth = StatsUnit(70f);
            float valueWidth = StatsUnit(60f);
            float gap = StatsUnit(8f);
            GUIStyle name = DeskBody(12f, PoliSimTheme.TextPrimary);
            GUIStyle absentName = DeskBody(12f, PoliSimTheme.TextMuted);
            GUIStyle value = DeskCaption(10.5f, PoliSimTheme.TextPrimary, false, TextAnchor.MiddleRight);
            GUIStyle unit = DeskCaption(7.5f, PoliSimTheme.TextMuted);
            float gaugeHeight = StatsUnit(8f);
            float sparkWidth = StatsUnit(44f);
            float sparkHeight = StatsUnit(13f);

            for (int i = 0; i < rows.Count; i++)
            {
                SocietyReading row = rows[i];
                float x = block.x + (i % 2) * (columnWidth + columnGap);
                float y = block.y + (i / 2) * rowHeight;
                PoliSimWidgets.MeasuredLabel(new Rect(x, y, nameWidth, rowHeight), row.Name, row.Absent ? absentName : name);
                float ix = x + nameWidth + gap;
                if (row.Fill >= 0f)
                {
                    var track = new Rect(ix, y + (rowHeight - gaugeHeight) * 0.5f, instrumentWidth, gaugeHeight);
                    PoliSimTheme.Rule(track, PoliSimTheme.BarTrack);
                    PoliSimTheme.Rule(new Rect(track.x, track.y, track.width * Mathf.Clamp01(row.Fill), track.height), row.Ink);
                }
                else if (row.Series != null)
                {
                    var spark = new Rect(ix + instrumentWidth - sparkWidth, y + (rowHeight - sparkHeight) * 0.5f, sparkWidth, sparkHeight);
                    if (row.Series.Count >= 2)
                    {
                        GraphRenderer.DrawSparkline(spark, row.Series, row.Ink);
                    }
                    else
                    {
                        DeskDottedBaseline(spark);
                    }
                }

                float vx = ix + instrumentWidth + gap;
                if (!row.Absent)
                {
                    PoliSimWidgets.MeasuredLabel(new Rect(vx, y, valueWidth, rowHeight), row.Value, value);
                }

                float unitX = vx + valueWidth + gap;
                PoliSimWidgets.MeasuredLabel(new Rect(unitX, y, Mathf.Max(1f, x + columnWidth - unitX), rowHeight), row.Unit, unit);
            }
        }

        // P-A2 (Playtest 1, finding 2 - 2026-08-29): the "as published" graph block that closed this
        // sheet is gone. It was a DISPLAY cut: the PublicationSystem mechanism is untouched (the
        // election model's section-19 reading takes Published, never State - PerceivedPerformanceHarness
        // asserts it), and the PRELIMINARY / revision honesty conventions stay on the main graphs,
        // which already carry them.

        /// <summary>
        /// Statistics › Domestic as board 2a draws it, top to bottom: the headline plates, the fiscal
        /// position on one axis, the sector distribution, the six live graphs, the Society rows (the "as published" band that
        /// followed them was cut at P-A2, 2026-08-29 - a display cut; the mechanism stands). The "Domestic" header is gone - the sub-tab already says it (a (b)-class
        /// duplicate, the census's own category). Next-year projections ride the three graphs that
        /// have them, from the same cached PreviewTurn the Desk's effects card reads (the dashed
        /// segment is a real feature and the section caption says what it is, once).
        /// </summary>
        private void DrawDomesticStatisticsContent(float contentWidth)
        {
            EconomyState state = _playerCountry.State;
            DrawStatsHeadlinePlates(contentWidth);
            StatsSectionGap();
            DrawStatsFiscalPosition(contentWidth);
            StatsSectionGap();
            DrawStatsSectorShares(contentWidth);
            StatsSectionGap();

            float? projectedGdp = null;
            float? projectedUnemployment = null;
            float? projectedApproval = null;
            if (_hasCachedPreview)
            {
                projectedGdp = state.GDP * (1f + _cachedGdpGrowthPercentRaw / 100f);
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

            DrawStatsSectionCaption("THE LIVE SERIES — DASHED = NEXT-YEAR ESTIMATE WHERE ONE EXISTS · TICKS ABOVE = LAWS ENACTED");
            GUILayout.Space(StatsUnit(6f));
            // The unit comes from the stat's own metadata rather than a MoneyUnit literal here: a
            // literal would be a second place that knows GDP is in billions, which is how the P2 unit
            // bug spread across 21 sites in the first place.
            DrawStatsGraphGrid(contentWidth, new List<System.Action>
            {
                () => _gdpGraph.Draw("GDP", history.Gdp.Quarterly, projectedGdp, graphLabel, higherIsBetter: true, moneyUnit: PolicyWebRenderer.GetStatUnit(StatNodeId.Gdp), enactmentPositions: enactments, shadowHistory: shadowHistory?.Gdp.Quarterly),
                () => _unemploymentGraph.Draw("Unemployment", history.Unemployment.Quarterly, projectedUnemployment, graphLabel, higherIsBetter: false, moneyUnit: null,
                    thresholdValue: _playerCountry.NaturalUnemploymentRate, thresholdLabel: "NAIRU", enactmentPositions: enactments, shadowHistory: shadowHistory?.Unemployment.Quarterly),
                () => _inflationGraph.Draw("Inflation", history.Inflation.Quarterly, null, graphLabel, higherIsBetter: false, moneyUnit: null, enactmentPositions: enactments, shadowHistory: shadowHistory?.Inflation.Quarterly),
                () => _approvalGraph.Draw("Approval rating", history.ApprovalRating.Quarterly, projectedApproval, graphLabel, higherIsBetter: true, moneyUnit: null, enactmentPositions: enactments, shadowHistory: shadowHistory?.ApprovalRating.Quarterly),
                () => _povertyGraph.Draw("Poverty rate", history.PovertyRate.Quarterly, null, graphLabel, higherIsBetter: false, moneyUnit: null, enactmentPositions: enactments, shadowHistory: shadowHistory?.PovertyRate.Quarterly),
                () => _debtGraph.Draw("Debt-to-GDP", history.DebtToGdpRatio.Quarterly, null, graphLabel, higherIsBetter: false, moneyUnit: null,
                    thresholdValue: _playerCountry.ComfortableDebtToGdpPercent, thresholdLabel: "comfortable", enactmentPositions: enactments, shadowHistory: shadowHistory?.Gdp.Quarterly)
            });
            StatsSectionGap();
            DrawImpactLedgerContent();
            StatsSectionGap();
            DrawStatsSocietyRows(contentWidth);
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
        /// no divergence to explain, and the panel says that in a sentence rather than printing six rows
        /// of zero.</para>
        /// </summary>
        private void DrawImpactLedgerContent()
        {
            if (_impactLedger == null) { return; }

            DrawStatsSectionCaption("YOUR POLICIES — THE GAP FROM THE NO-POLICY COUNTERFACTUAL, AND WHAT OPENED IT");
            GUILayout.Space(StatsUnit(6f));

            if (!_impactLedger.HasAnything)
            {
                GUILayout.Label("You have not moved a dial yet, so the live series and the counterfactual are the same run. "
                                + "The moment you do, the gap appears here with the reason beside it.", _labelStyle);
                return;
            }

            DrawImpactRow("GDP", "GDP", PolicyWebRenderer.GetStatUnit(StatNodeId.Gdp));
            DrawImpactRow("Unemployment", "Unemployment", null);
            DrawImpactRow("Inflation", "Inflation", null);
            DrawImpactRow("Approval rating", "ApprovalRating", null);
            DrawImpactRow("Poverty rate", "PovertyRate", null);
            // Debt is carried in the same money as GDP, so it takes GDP's declared unit rather than a
            // MoneyUnit literal here - a literal would be a second place that knows what the seed's
            // money is, which is how the P2 unit bug spread across 21 sites.
            DrawImpactRow("Government debt", "GovernmentDebt", PolicyWebRenderer.GetStatUnit(StatNodeId.Gdp));
        }

        /// <summary>One stat's line: the divergence, then each family's share of it largest first, then
        /// the interaction. ⚠ A family whose share rounds away is dropped from the sentence rather than
        /// printed as a zero it is not - but the interaction is printed whatever its size, because its
        /// smallness is the reader's business as much as its largeness.</summary>
        private void DrawImpactRow(string label, string statField, MoneyUnit? unit)
        {
            List<ImpactLine> lines = _impactLedger.LinesFor(_playerCountry, statField, out float divergence);

            string headline = FormatImpact(divergence, unit);
            var reasons = new System.Text.StringBuilder();
            for (int i = 0; i < lines.Count; i++)
            {
                bool isInteraction = i == lines.Count - 1;
                if (!isInteraction && Mathf.Abs(lines[i].Contribution) < ImpactRoundsAway(unit)) { continue; }

                if (reasons.Length > 0) { reasons.Append("  ·  "); }
                reasons.Append(lines[i].Family).Append(' ').Append(FormatImpact(lines[i].Contribution, unit));
            }

            GUILayout.BeginHorizontal();
            GUILayout.Label(label, _labelStyle, GUILayout.Width(StatsUnit(140f)));
            GUILayout.Label(headline, _labelStyle, GUILayout.Width(StatsUnit(120f)));
            GUILayout.Label(reasons.ToString(), _labelStyle);
            GUILayout.EndHorizontal();
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
        /// the event card's now), and the "International" header with it - the sub-tab says it. No
        /// board of its own: it inherits D4's tokens and E22's pass-through label unchanged.
        /// </summary>
        private void DrawInternationalStatisticsContent()
        {
            DrawWorldMapContent();
            GUILayout.Space(StatsUnit(10f));
            DrawCountryPageContent();
            GUILayout.Space(StatsUnit(10f));
            DrawTradeStatsContent();
        }

        /// <summary>
        /// Board 5a (D11 row 1, 2026-09-02): **the pair as ONE PAGE rather than a stack.** P-E1's two
        /// side-by-side blocks become one instrument with a spine - a mirrored ledger, the eight
        /// readings as one column of labels down the centre with the home side reading right-to-left
        /// on the left and the partner left-to-right on the right, so a label is read once and the eye
        /// compares across it. Both identities are the masthead; the pair (trade both ways as two
        /// arrows, both tariffs, bloc, currency) leads the right column because it is the only content
        /// that belongs to the pair - everything else is two countries' own readings.
        ///
        /// <para>⚠ <b>ONLY WHAT THE MODEL HOLDS, and absence drawn as its own fact - three states, three
        /// drawings.</b> This model holds no bilateral relations state at all (`Country` has no relations
        /// field; a summit is an event, not a bond), so every pair page carries the dashed collar saying
        /// so and nothing reads warm or cool. <i>No trade link</i> replaces the trade plate's arrows with
        /// the collar while the tariffs still read (each side's rate is a fact about that side); <i>trade
        /// of zero</i> draws the arrows at their minimum with the figure 0 - a different fact from "no
        /// link", never the same pixels. A row with one side is not drawn: currency strength exists only
        /// for independent-currency countries, so against a euro partner the row is omitted and the footer
        /// says why. Partner order is the CountryId enum's.</para>
        /// </summary>
        /// <summary>The pair column's width on the board (380 of the 1280 board's px), scaled with the sheet.</summary>
        private float PairColumnWidth => StatsUnit(380f);

        private void DrawCountryPageContent()
        {
            var others = new List<Country>();
            foreach (CountryId id in (CountryId[])System.Enum.GetValues(typeof(CountryId)))
            {
                if (id == PlayerCountryId) { continue; }
                Country c = _world.GetCountry(id);
                if (c != null) { others.Add(c); }
            }
            if (others.Count == 0) { return; }

            _internationalPageIndex = ((_internationalPageIndex % others.Count) + others.Count) % others.Count;
            Country them = others[_internationalPageIndex];

            DrawStatsSectionCaption("PAIR PAGE · THE MODEL'S OWN LINKS");
            GUILayout.Space(StatsUnit(4f));

            // The pager: prev · the partners in enum order, the current one in the primary ink · next.
            GUILayout.BeginHorizontal();
            if (PoliSimWidgets.Button("\u2039 PREV", _neutralActionButtonStyle, GUILayout.Width(StatsUnit(90f)))) { _internationalPageIndex--; }
            GUILayout.FlexibleSpace();
            for (int i = 0; i < others.Count; i++)
            {
                bool current = i == _internationalPageIndex;
                GUILayout.Label(others[i].Name.ToUpperInvariant(), DeskCaption(current ? 10f : 8.5f, current ? PoliSimTheme.TextPrimary : PoliSimTheme.TextMuted, current, TextAnchor.MiddleCenter));
                if (i < others.Count - 1) { GUILayout.Label("·", DeskCaption(8.5f, PoliSimTheme.TextMuted, false, TextAnchor.MiddleCenter)); }
            }
            GUILayout.FlexibleSpace();
            if (PoliSimWidgets.Button("NEXT \u203A", _neutralActionButtonStyle, GUILayout.Width(StatsUnit(90f)))) { _internationalPageIndex++; }
            GUILayout.EndHorizontal();
            GUILayout.Space(StatsUnit(6f));

            // The masthead: both identities, the home side left, the partner right.
            GUILayout.BeginHorizontal();
            DrawPairIdentity(_playerCountry, "HOME", left: true);
            GUILayout.FlexibleSpace();
            GUILayout.Label("BOTH SIDES · LIVE", DeskCaption(8.5f, PoliSimTheme.TextMuted, false, TextAnchor.MiddleCenter));
            GUILayout.FlexibleSpace();
            DrawPairIdentity(them, "PARTNER", left: false);
            GUILayout.EndHorizontal();
            GUILayout.Space(StatsUnit(8f));

            GUILayout.BeginHorizontal();
            GUILayout.BeginVertical(GUILayout.ExpandWidth(true));
            DrawPairMirroredLedger(them);
            GUILayout.EndVertical();
            GUILayout.Space(StatsUnit(16f));
            GUILayout.BeginVertical(GUILayout.Width(PairColumnWidth));
            DrawPairTradePlate(them);
            GUILayout.Space(StatsUnit(8f));
            DrawPairStancePlate(them);
            GUILayout.Space(StatsUnit(8f));
            DrawPairRelationsCollar();
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
        }

        /// <summary>One identity of the masthead: the flag, the name as a numeral, and its role · currency zone · bloc (· the year for the home side).</summary>
        private void DrawPairIdentity(Country country, string role, bool left)
        {
            TextAnchor anchor = left ? TextAnchor.MiddleLeft : TextAnchor.MiddleRight;
            string bloc = "NO BLOC";
            foreach (TradeBloc b in _world.TradeBlocs) { if (b.IsMember(country.Id)) { bloc = b.Name.ToUpperInvariant(); break; } }
            string line = role + " · " + country.CurrencyZone.Name.ToUpperInvariant() + " · " + bloc
                + (left ? " · YEAR " + _simulationManager.CurrentDate.Year.ToString(CultureInfo.InvariantCulture) : "");
            GUILayout.BeginHorizontal();
            if (left) { DrawPairFlag(country.Id); }
            GUILayout.BeginVertical();
            GUILayout.Label(country.Name, DeskNumeral(16f, PoliSimTheme.TextPrimary, left ? TextAnchor.LowerLeft : TextAnchor.LowerRight));
            GUILayout.Label(line, DeskCaption(8.5f, PoliSimTheme.TextSecondary, false, anchor));
            GUILayout.EndVertical();
            if (!left) { DrawPairFlag(country.Id); }
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

        /// <summary>The mirrored ledger: one label column down the centre, the home figure reading right-to-left on the left, the partner's left-to-right on the right. Eight readings each side; a row with one side is omitted and the footer says why.</summary>
        private void DrawPairMirroredLedger(Country them)
        {
            DrawStatsSectionCaption("EIGHT HEADLINE READINGS · THE SAME EIGHT EACH SIDE");
            GUILayout.Space(StatsUnit(3f));
            DrawPairMirrorRow("GDP", UiFormat.Money(_playerCountry.State.GDP, MoneyUnit.Billions), UiFormat.Money(them.State.GDP, MoneyUnit.Billions));
            DrawPairMirrorRow("UNEMPLOYMENT", UiFormat.Number(_playerCountry.State.Unemployment, 1) + "%", UiFormat.Number(them.State.Unemployment, 1) + "%");
            DrawPairMirrorRow("INFLATION", UiFormat.Number(_playerCountry.State.Inflation, 1) + "%", UiFormat.Number(them.State.Inflation, 1) + "%");
            DrawPairMirrorRow("APPROVAL", UiFormat.Number(_playerCountry.State.ApprovalRating, 1), UiFormat.Number(them.State.ApprovalRating, 1));
            DrawPairMirrorRow("DEBT-TO-GDP", UiFormat.Number(_playerCountry.State.DebtToGdpRatio, 1) + "%", UiFormat.Number(them.State.DebtToGdpRatio, 1) + "%");
            DrawPairMirrorRow("BUDGET BALANCE", PairBudgetBalance(_playerCountry), PairBudgetBalance(them));
            DrawPairMirrorRow("CREDIT RATING", PairCreditRating(_playerCountry), PairCreditRating(them));
            DrawPairMirrorRow("POVERTY RATE", UiFormat.Number(_playerCountry.State.PovertyRate, 1) + "%", UiFormat.Number(them.State.PovertyRate, 1) + "%");

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
            return (last.BudgetBalance / country.State.GDP * 100f).ToString("+0.0;-0.0;0.0", CultureInfo.InvariantCulture) + "% GDP";
        }

        /// <summary>The standing rating (set by scheduled review); a dash until the first review - an unrated sovereign is not a top-rated one.</summary>
        private static string PairCreditRating(Country country) =>
            country.Rating != null && country.Rating.HasBeenReviewed ? CreditRatingSystem.Format(country.Rating.Rating) : "—";

        private void DrawPairMirrorRow(string label, string mine, string theirs)
        {
            GUIStyle numeral = DeskNumeral(13f, PoliSimTheme.TextPrimary, TextAnchor.MiddleRight);
            GUIStyle numeralRight = DeskNumeral(13f, PoliSimTheme.TextPrimary, TextAnchor.MiddleLeft);
            GUIStyle caption = DeskCaption(8.5f, PoliSimTheme.TextMuted, false, TextAnchor.MiddleCenter);
            float height = Mathf.Ceil(numeral.CalcSize(new GUIContent("0")).y) + StatsUnit(4f);
            Rect row = GUILayoutUtility.GetRect(10f, height, GUILayout.ExpandWidth(true));
            if (Event.current.type != EventType.Repaint) { return; }
            float labelWidth = StatsUnit(150f);
            float side = Mathf.Max(1f, (row.width - labelWidth) * 0.5f);
            PoliSimWidgets.MeasuredLabel(new Rect(row.x, row.y, side, row.height), mine, numeral);
            PoliSimWidgets.MeasuredLabel(new Rect(row.x + side, row.y, labelWidth, row.height), label, caption);
            PoliSimWidgets.MeasuredLabel(new Rect(row.x + side + labelWidth, row.y, side, row.height), theirs, numeralRight);
            PoliSimTheme.Rule(new Rect(row.x, row.yMax - 1f, row.width, 1f), PoliSimTheme.RuleRow);
        }

        /// <summary>The pair plate: trade from the map's own links as two arrows (direction the fact, length relative to the larger), the tariff each charges, shared bloc, shared currency - and the two absence states drawn apart.</summary>
        private void DrawPairTradePlate(Country them)
        {
            DrawStatsSectionCaption("THE PAIR · TRADE FROM THE MAP'S OWN LINKS");
            GUILayout.Space(StatsUnit(4f));
            string mine = _playerCountry.Name.ToUpperInvariant();
            string theirs = them.Name.ToUpperInvariant();
            TradePartner link = _playerCountry.TradePartners.Find(p => p.PartnerId == them.Id);
            if (link == null)
            {
                DrawPairCollar("NO TRADE LINK", "THE MAP HOLDS NO TradePartner LINK BETWEEN THESE TWO. THIS IS NOT TRADE OF ZERO — NO VOLUME EXISTS TO BE ZERO. TARIFFS STILL READ: EACH SIDE'S RATE IS A FACT ABOUT THAT SIDE.");
            }
            else
            {
                float max = Mathf.Max(link.ExportVolume, link.ImportVolume);
                bool zero = max <= 0f;
                if (zero) { GUILayout.Label("(TRADE OF ZERO, THIS PERIOD) — A LINK EXISTS AND CARRIED NOTHING: THE ARROWS DRAW AT THEIR MINIMUM, THE FIGURE READS 0", DeskCaption(7.5f, PoliSimTheme.TextMuted)); }
                DrawPairTradeArrow(mine + " → " + theirs, link.ExportVolume, max);
                DrawPairTradeArrow(theirs + " → " + mine, link.ImportVolume, max);
            }
            GUILayout.Space(StatsUnit(4f));
            DrawPairFactRow("TARIFF " + mine + " CHARGES", UiFormat.Number(TradeSystem.GetTariffRate(_playerCountry, them, _world.TradeBlocs), 1) + "%");
            DrawPairFactRow("TARIFF " + theirs + " CHARGES", UiFormat.Number(TradeSystem.GetTariffRate(them, _playerCountry, _world.TradeBlocs), 1) + "%");
            string sharedBloc = null;
            foreach (TradeBloc bloc in _world.TradeBlocs) { if (bloc.IsMember(PlayerCountryId) && bloc.IsMember(them.Id)) { sharedBloc = bloc.Name.ToUpperInvariant(); break; } }
            DrawPairFactRow("SHARED BLOC", sharedBloc ?? "NONE");
            bool sameCurrency = _playerCountry.CurrencyZone == them.CurrencyZone;
            DrawPairFactRow("SHARED CURRENCY", sameCurrency
                ? "YES — " + _playerCountry.CurrencyZone.Name.ToUpperInvariant()
                : "NO — " + _playerCountry.CurrencyZone.Name.ToUpperInvariant() + " / " + them.CurrencyZone.Name.ToUpperInvariant());
        }

        /// <summary>One trade arrow: the label above, the shaft from the left edge with its length relative to the larger of the pair (a minimum for zero), the head, and the figure at the head - in the Trade area's ink.</summary>
        private void DrawPairTradeArrow(string label, float volume, float max)
        {
            GUIStyle caption = DeskCaption(8f, PoliSimTheme.TextSecondary);
            GUIStyle figure = DeskNumeral(12f, PoliSimTheme.TextPrimary, TextAnchor.MiddleLeft);
            float captionHeight = Mathf.Ceil(Mathf.Max(DeskCaptionHeight(caption), caption.CalcSize(new GUIContent(label)).y));   // the arrow glyph's line is taller than the caption face's (2.7 px at 2560), so the row is measured on the label itself
            float lane = Mathf.Ceil(figure.CalcSize(new GUIContent("0")).y) + StatsUnit(2f);
            Rect r = GUILayoutUtility.GetRect(10f, captionHeight + lane + StatsUnit(3f), GUILayout.ExpandWidth(true));
            if (Event.current.type != EventType.Repaint) { return; }
            PoliSimWidgets.MeasuredLabel(new Rect(r.x, r.y, r.width, captionHeight), label, caption);
            string text = UiFormat.Money(volume, MoneyUnit.Billions);
            float figureWidth = figure.CalcSize(new GUIContent(text)).x + StatsUnit(6f);
            float track = Mathf.Max(1f, r.width - figureWidth);
            float fraction = max > 0f ? volume / max : 0f;
            float length = Mathf.Max(track * 0.12f, track * fraction);
            float y = r.y + captionHeight + lane * 0.5f;
            float shaft = Mathf.Max(2f, StatsUnit(3f));
            float head = Mathf.Max(5f, StatsUnit(7f));
            Color ink = UiPalette.GetAreaColor(UiPalette.SystemArea.Trade);
            PoliSimTheme.Rule(new Rect(r.x, y - shaft * 0.5f, Mathf.Max(1f, length - head), shaft), ink);
            Color previous = GUI.color;
            GUI.color = ink;
            const int Steps = 5;
            for (int s = 0; s < Steps; s++)
            {
                float t = (s + 0.5f) / Steps;
                float half = head * 0.8f * (1f - t);
                GUI.DrawTexture(new Rect(r.x + length - head + head * t - head / Steps * 0.5f, y - half, head / Steps + 0.6f, half * 2f), Texture2D.whiteTexture);
            }
            GUI.color = previous;
            PoliSimWidgets.MeasuredLabel(new Rect(r.x + length + StatsUnit(4f), r.y + captionHeight, figureWidth, lane), text, figure);
        }

        private void DrawPairFactRow(string label, string value)
        {
            GUIStyle caption = DeskCaption(8f, PoliSimTheme.TextMuted);
            GUIStyle figure = DeskNumeral(12f, PoliSimTheme.TextPrimary, TextAnchor.MiddleRight);
            float height = Mathf.Ceil(figure.CalcSize(new GUIContent("0")).y) + StatsUnit(3f);
            Rect r = GUILayoutUtility.GetRect(10f, height, GUILayout.ExpandWidth(true));
            if (Event.current.type != EventType.Repaint) { return; }
            float valueWidth = figure.CalcSize(new GUIContent(value)).x + StatsUnit(4f);
            PoliSimWidgets.MeasuredLabel(new Rect(r.x, r.y, Mathf.Max(1f, r.width - valueWidth), r.height), label, caption);
            PoliSimWidgets.MeasuredLabel(new Rect(r.xMax - valueWidth, r.y, valueWidth, r.height), value, figure);
            PoliSimTheme.Rule(new Rect(r.x, r.yMax - 1f, r.width, 1f), PoliSimTheme.RuleRow);
        }

        /// <summary>The stance plate: the two blends both sides sit on (PolicyStanceAxes, the pair the compass plotted until P2-3.2 - not the CHES positions, which are not on this page), each a centred lane with the two markers tagged.</summary>
        private void DrawPairStancePlate(Country them)
        {
            DrawStatsSectionCaption("POLICY STANCE · TWO BLENDS, BOTH SIDES");
            GUILayout.Space(StatsUnit(4f));
            DrawPairStanceLane("FISCAL SIZE", PolicyStanceAxes.GetFiscalSizeAxisValue(_playerCountry), PolicyStanceAxes.GetFiscalSizeAxisValue(them), them);
            DrawPairStanceLane("REGULATION / WELFARE", PolicyStanceAxes.GetRegulationWelfareAxisValue(_playerCountry), PolicyStanceAxes.GetRegulationWelfareAxisValue(them), them);
            GUILayout.Label("THE BLENDS THE COMPASS PLOTTED UNTIL P2-3.2 — NOT THE CHES POSITIONS, WHICH ARE NOT ON THIS PAGE. 0–100, THE CODE'S OWN SCALE.", DeskCaption(7.5f, PoliSimTheme.TextMuted));
        }

        private void DrawPairStanceLane(string axis, float mine, float theirs, Country them)
        {
            GUIStyle caption = DeskCaption(8f, PoliSimTheme.TextSecondary);
            GUIStyle tag = DeskCaption(8f, PoliSimTheme.TextPrimary, true, TextAnchor.MiddleCenter);
            float captionHeight = Mathf.Ceil(DeskCaptionHeight(caption));
            float lane = StatsUnit(18f);
            float tagHeight = captionHeight;   // the markers' tags get a full caption row above the track (the first film squeezed them into half a lane)
            Rect r = GUILayoutUtility.GetRect(10f, captionHeight + tagHeight + lane + StatsUnit(4f), GUILayout.ExpandWidth(true));
            if (Event.current.type != EventType.Repaint) { return; }
            PoliSimWidgets.MeasuredLabel(new Rect(r.x, r.y, r.width, captionHeight), axis, caption);
            float trackY = r.y + captionHeight + tagHeight + lane * 0.5f;
            float tagWidth = StatsUnit(22f);
            float x0 = r.x + tagWidth * 0.5f;
            float span = Mathf.Max(1f, r.width - tagWidth);
            PoliSimTheme.Rule(new Rect(x0, trackY - 0.5f, span, 1f), PoliSimTheme.Hairline);
            PoliSimTheme.Rule(new Rect(x0 + span * 0.5f, trackY - lane * 0.25f, 1f, lane * 0.5f), PoliSimTheme.HairlineStrong);
            DrawPairStanceMarker(x0 + span * Mathf.Clamp01(mine / 100f), trackY, PairCountryTag(PlayerCountryId), tagWidth, lane, tag, PoliSimTheme.TextPrimary);
            DrawPairStanceMarker(x0 + span * Mathf.Clamp01(theirs / 100f), trackY, PairCountryTag(them.Id), tagWidth, lane, tag, PoliSimTheme.TextSecondary);
        }

        private void DrawPairStanceMarker(float x, float y, string tagText, float tagWidth, float lane, GUIStyle tag, Color ink)
        {
            float dot = Mathf.Max(4f, StatsUnit(6f));
            PoliSimTheme.Rule(new Rect(x - dot * 0.5f, y - dot * 0.5f, dot, dot), ink);
            GUIStyle inked = new GUIStyle(tag);
            inked.normal.textColor = ink;
            float tagHeight = Mathf.Ceil(DeskCaptionHeight(tag));
            PoliSimWidgets.MeasuredLabel(new Rect(x - tagWidth * 0.5f, y - lane * 0.5f - tagHeight, tagWidth, tagHeight), tagText, inked);
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

        /// <summary>The collar every pair page carries: the model holds no bilateral relations state, and nothing on the page reads warm or cool - in a dashed frame, always.</summary>
        private void DrawPairRelationsCollar()
        {
            DrawPairCollar("NO BILATERAL RELATIONS STATE", "THE MODEL HOLDS NONE — NO RELATIONS SCORE, NO ALLIANCE OR TREATY STANDING, NO DIPLOMATIC HISTORY. NOTHING ON THIS PAGE READS WARM OR COOL, AND NO SCORE IS DRAWN IN ITS PLACE.");
        }

        /// <summary>A dashed collar with a title and a sentence - board 5a's drawing of an absence.</summary>
        private void DrawPairCollar(string title, string sentence)
        {
            GUIStyle head = DeskCaption(8.5f, PoliSimTheme.TextSecondary, true);
            GUIStyle body = DeskCaption(7.5f, PoliSimTheme.TextMuted);
            body.wordWrap = true;
            float pad = StatsUnit(6f);
            float width = PairColumnWidth;   // the pair column's own width - the collar is measured against it, not against a layout probe
            float headHeight = Mathf.Ceil(DeskCaptionHeight(head));
            float bodyHeight = Mathf.Ceil(body.CalcHeight(new GUIContent(sentence), Mathf.Max(1f, width - pad * 2f)));
            Rect r = GUILayoutUtility.GetRect(10f, headHeight + bodyHeight + pad * 2f + StatsUnit(2f), GUILayout.ExpandWidth(true));
            if (Event.current.type != EventType.Repaint) { return; }
            DeskDashedFrame(r, PoliSimTheme.HairlineStrong, 4f, 3f);
            PoliSimWidgets.MeasuredLabel(new Rect(r.x + pad, r.y + pad, r.width - pad * 2f, headHeight), title, head);
            GUI.Label(new Rect(r.x + pad, r.y + pad + headHeight + StatsUnit(2f), r.width - pad * 2f, bodyHeight), sentence, body);
        }
    }
}
