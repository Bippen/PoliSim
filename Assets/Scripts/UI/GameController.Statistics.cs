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
            readings.Add(new HeadlineReading("Budget Balance", UiFormat.MoneyDelta(state.Budget, MoneyUnit.Billions), null, false, null));
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
        /// C-C8 (P-E1): **the browsable country page** — each of the other five in relation to the
        /// player's country, one at a time, prev/next.
        ///
        /// <para>⚠ <b>ONLY WHAT THE MODEL HOLDS, and the absence is drawn as loudly as the presence.</b>
        /// The pre-ruling is explicit: no relations score, no derived affinity presented as a fact. That
        /// is not a stylistic preference here — <b>this model holds no bilateral relations state at
        /// all.</b> `Country` has no relations field; `ForeignPolicyMeeting` is an event with options,
        /// not a standing relationship. Any "relations: warm/cool" reading on this page would be
        /// invented whole, and the page says so in its own words instead.</para>
        ///
        /// <para>What it CAN say, every line of it derived: both sides' headline readings; the pair's
        /// trade in both directions from the map's own `TradePartner` links; the tariff each charges the
        /// other, through the same `GetTariffRate` the simulation charges; whether they share a trade
        /// bloc and whether they share a currency; and both compass positions as the compass itself
        /// draws them.</para>
        /// </summary>
        private void DrawCountryPageContent()
        {
            var others = new List<Country>();
            foreach (Country c in _world.Countries)
            {
                if (c.Id != PlayerCountryId) { others.Add(c); }
            }

            if (others.Count == 0) { return; }

            _internationalPageIndex = ((_internationalPageIndex % others.Count) + others.Count) % others.Count;
            Country them = others[_internationalPageIndex];

            DrawStatsSectionCaption($"{_playerCountry.Name.ToUpperInvariant()} AND {them.Name.ToUpperInvariant()} — {_internationalPageIndex + 1} OF {others.Count}");
            GUILayout.Space(StatsUnit(4f));

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("< Previous", _buttonStyle, GUILayout.Width(StatsUnit(110f)))) { _internationalPageIndex--; }
            GUILayout.FlexibleSpace();
            GUILayout.Label(them.Name, _headerStyle);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Next >", _buttonStyle, GUILayout.Width(StatsUnit(110f)))) { _internationalPageIndex++; }
            GUILayout.EndHorizontal();

            GUILayout.Space(StatsUnit(6f));

            // Side by side, the player first - every figure each country's own state.
            DrawPairRow("GDP", UiFormat.Money(_playerCountry.State.GDP, MoneyUnit.Billions), UiFormat.Money(them.State.GDP, MoneyUnit.Billions));
            DrawPairRow("Unemployment", $"{_playerCountry.State.Unemployment:F2}%", $"{them.State.Unemployment:F2}%");
            DrawPairRow("Inflation", $"{_playerCountry.State.Inflation:F2}%", $"{them.State.Inflation:F2}%");
            DrawPairRow("Debt-to-GDP", $"{_playerCountry.State.DebtToGdpRatio:F1}%", $"{them.State.DebtToGdpRatio:F1}%");
            DrawPairRow("Approval", $"{_playerCountry.State.ApprovalRating:F1}", $"{them.State.ApprovalRating:F1}");

            GUILayout.Space(StatsUnit(6f));
            GUILayout.Label("Between the two", _headerStyle);

            TradePartner link = _playerCountry.TradePartners.Find(p => p.PartnerId == them.Id);
            if (link != null)
            {
                DrawPairRow("Trade (exports / imports)",
                    $"{UiFormat.Money(link.ExportVolume, MoneyUnit.Billions)} out",
                    $"{UiFormat.Money(link.ImportVolume, MoneyUnit.Billions)} in");
                DrawPairRow("Tariff charged",
                    $"{TradeSystem.GetTariffRate(_playerCountry, them, _world.TradeBlocs):F2}%",
                    $"{TradeSystem.GetTariffRate(them, _playerCountry, _world.TradeBlocs):F2}%");
            }
            else
            {
                // ⚠ Absence, drawn. Not every pair trades in this model, and a zero would read as
                // "they trade nothing" rather than "this model holds no link between them".
                GUILayout.Label("No bilateral trade link exists between these two in this model - which is not the same as trade of zero.", _labelStyle);
            }

            bool sameBloc = false;
            foreach (TradeBloc bloc in _world.TradeBlocs)
            {
                if (bloc.IsMember(PlayerCountryId) && bloc.IsMember(them.Id)) { sameBloc = true; }
            }

            GUILayout.Label(sameBloc ? "Both are members of the same trade bloc." : "They share no trade bloc.", _labelStyle);
            GUILayout.Label(_playerCountry.CurrencyZone == them.CurrencyZone
                ? "They share a currency, so neither sets a policy rate against the other."
                : "They use different currencies.", _labelStyle);

            GUILayout.Space(StatsUnit(6f));
            DrawPairRow("Compass — fiscal size",
                $"{PoliticalCompassRenderer.GetFiscalSizeAxisValue(_playerCountry):F1}",
                $"{PoliticalCompassRenderer.GetFiscalSizeAxisValue(them):F1}");
            DrawPairRow("Compass — regulation / welfare",
                $"{PoliticalCompassRenderer.GetRegulationWelfareAxisValue(_playerCountry):F1}",
                $"{PoliticalCompassRenderer.GetRegulationWelfareAxisValue(them):F1}");

            GUILayout.Space(StatsUnit(6f));

            // ⚠ THE ABSENCE BLOCK. Every gap here is a line in the Design ask (C-F1), and stating them
            // is the point of the page rather than an apology for it: a player who cannot see what the
            // model does NOT know will read the four facts above as a complete picture of a relationship.
            GUILayout.Label("What this model does not hold about this pair", _headerStyle);
            // ⚠ PLAYER-FACING PROSE, and the first cut was not. It named the type - "Country carries no
            // bilateral relations field" - with the identifier in backticks, which rendered literally on
            // screen: developer-facing text on a player surface, the exact class P-A1 cut 131 strings of.
            // The 1280 film is what showed it. This says the same true thing in the player's own terms.
            GUILayout.Label(
                "No relations score, no alliance or treaty standing, no diplomatic history, and no record of "
                + "past dealings between these two countries. This simulation does not model a relationship "
                + "between governments at all - a summit is a passing event, not a bond that lasts - so a "
                + "\"warm\" or \"cool\" reading here would be made up rather than measured. What you see above "
                + "is everything the simulation knows about this pair.",
                _labelStyle);
        }

        /// <summary>C-C8: one comparison row — the label, the player's figure, then theirs. Deliberately
        /// plain: neither side is coloured good or bad, because "their unemployment is higher" is not a
        /// thing this model has an opinion about.</summary>
        private void DrawPairRow(string label, string mine, string theirs)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, _labelStyle, GUILayout.Width(StatsUnit(200f)));
            GUILayout.Label(mine, _labelStyle, GUILayout.Width(StatsUnit(140f)));
            GUILayout.Label(theirs, _labelStyle, GUILayout.Width(StatsUnit(140f)));
            GUILayout.EndHorizontal();
        }
    }
}
