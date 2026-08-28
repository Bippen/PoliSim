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

        private const string PublishedKeyPreliminary = "PRELIMINARY";
        private const string PublishedKeyFinal = "FINAL";
        private const string PublishedKeyFirst = "= FIRST ESTIMATE · ";
        private const string PublishedKeyRevised = "= REVISED LATER · ";
        private const string PublishedKeySettled = "= SETTLED · LAG SHOWN PER GRAPH";

        /// <summary>Board 2a: the published band - E19's sentence retired for a KEY on the rule (the badge chips and the dashed frame the graphs themselves draw, named once), the three monthly / quarterly series as graphs in the 3-column grid, and the annual poverty figure as its bulletin (E18) beneath the third: eleven points beside a daily series read as a broken graph, so an annual figure renders as this number, for this period, released on this date.</summary>
        private void DrawStatsPublishedBand(float contentWidth)
        {
            GUIStyle keyStyle = DeskCaption(8f, PoliSimTheme.TextSecondary);
            float keyWidth = StatsPublishedKeyWidth(keyStyle);
            Rect row = DrawStatsSectionCaption("AS PUBLISHED — LAGGED, REVISED AS LATER ESTIMATES ARRIVE", keyWidth + StatsUnit(12f));
            if (Event.current.type == EventType.Repaint)
            {
                DrawStatsPublishedKey(new Rect(row.xMax - keyWidth, row.y, keyWidth, row.height - 1f), keyStyle);
            }

            GUILayout.Space(StatsUnit(6f));
            GUIStyle graphLabel = StatsGraphLabelStyle();
            GUIStyle bulletinLabel = DeskBody(12f, PoliSimTheme.TextPrimary);
            System.DateTime now = _simulationManager.CurrentDate;
            DrawStatsGraphGrid(contentWidth, new List<System.Action>
            {
                () => _gdpPublishedGraph.DrawPublished("GDP as published", PublishedSeriesFor(PublishedStat.Gdp), graphLabel, higherIsBetter: true, now, moneyUnit: PolicyWebRenderer.GetStatUnit(StatNodeId.Gdp)),
                () => _unemploymentPublishedGraph.DrawPublished("Unemployment as published", PublishedSeriesFor(PublishedStat.Unemployment), graphLabel, higherIsBetter: false, now, moneyUnit: null),
                () =>
                {
                    // Inflation joins the two graphs because it SHARES THEIR CADENCE - monthly, 143
                    // releases over twelve years; the poverty rate does not (eleven), so it is a bulletin.
                    _inflationPublishedGraph.DrawPublished("Inflation as published", PublishedSeriesFor(PublishedStat.Inflation), graphLabel, higherIsBetter: false, now, moneyUnit: null);
                    GUILayout.Space(StatsUnit(6f));
                    PublishedFigure.Draw("Poverty rate as published", PublishedSeriesFor(PublishedStat.PovertyRate), bulletinLabel, moneyUnit: null);
                }
            });
        }

        private PublishedSeries PublishedSeriesFor(PublishedStat stat)
        {
            return _playerCountry.Published.Series.TryGetValue(stat, out PublishedSeries series) ? series : null;
        }

        /// <summary>The key's measured width, so the section caption can leave it room on the rule.</summary>
        private float StatsPublishedKeyWidth(GUIStyle style)
        {
            float chipPad = StatsUnit(5f);
            float gap = StatsUnit(4f);
            float width = 0f;
            width += Mathf.Ceil(style.CalcSize(new GUIContent(PublishedKeyPreliminary)).x) + chipPad * 2f + gap;
            width += Mathf.Ceil(style.CalcSize(new GUIContent(PublishedKeyFirst)).x);
            width += StatsUnit(26f) + gap;
            width += Mathf.Ceil(style.CalcSize(new GUIContent(PublishedKeyRevised)).x);
            width += Mathf.Ceil(style.CalcSize(new GUIContent(PublishedKeyFinal)).x) + chipPad * 2f + gap;
            width += Mathf.Ceil(style.CalcSize(new GUIContent(PublishedKeySettled)).x);
            return width;
        }

        /// <summary>The KEY (E19 returned as an instrument): the PRELIMINARY chip in the caution ink, the dashed frame in the fill amber, the FINAL chip in the secondary ink - the same three marks the published graphs draw, in the same inks, so the key cannot drift from what it explains. Repaint-gated by its caller.</summary>
        private void DrawStatsPublishedKey(Rect rect, GUIStyle style)
        {
            float chipPad = StatsUnit(5f);
            float gap = StatsUnit(4f);
            float chipHeight = Mathf.Min(rect.height, Mathf.Ceil(DeskCaptionHeight(style)) + StatsUnit(2f));
            float cy = rect.y + (rect.height - chipHeight) * 0.5f;
            float x = rect.x;

            void Chip(string text, Color ink)
            {
                float w = Mathf.Ceil(style.CalcSize(new GUIContent(text)).x) + chipPad * 2f;
                var chip = new Rect(x, cy, w, chipHeight);
                PoliSimTheme.RoundedCard(chip, PoliSimTheme.Card, ink, 0f);
                PoliSimWidgets.MeasuredLabel(chip, text, DeskCaption(8f, ink, false, TextAnchor.MiddleCenter));
                x += w + gap;
            }

            void Text(string text)
            {
                float w = Mathf.Ceil(style.CalcSize(new GUIContent(text)).x);
                PoliSimWidgets.MeasuredLabel(new Rect(x, rect.y, w, rect.height), text, style);
                x += w;
            }

            Chip(PublishedKeyPreliminary, PoliSimTheme.Caution);
            Text(PublishedKeyFirst);
            var dashed = new Rect(x, cy + (chipHeight - StatsUnit(10f)) * 0.5f, StatsUnit(26f), StatsUnit(10f));
            DeskDashedFrame(dashed, PoliSimTheme.Draft, 3f, 2f);
            x += dashed.width + gap;
            Text(PublishedKeyRevised);
            Chip(PublishedKeyFinal, PoliSimTheme.TextSecondary);
            Text(PublishedKeySettled);
        }

        /// <summary>
        /// Statistics › Domestic as board 2a draws it, top to bottom: the headline plates, the fiscal
        /// position on one axis, the sector distribution, the six live graphs, the Society rows, the
        /// published band. The "Domestic" header is gone - the sub-tab already says it (a (b)-class
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
            DrawStatsSectionCaption("THE LIVE SERIES — DASHED = NEXT-YEAR ESTIMATE WHERE ONE EXISTS");
            GUILayout.Space(StatsUnit(6f));
            // The unit comes from the stat's own metadata rather than a MoneyUnit literal here: a
            // literal would be a second place that knows GDP is in billions, which is how the P2 unit
            // bug spread across 21 sites in the first place.
            DrawStatsGraphGrid(contentWidth, new List<System.Action>
            {
                () => _gdpGraph.Draw("GDP", history.Gdp.Quarterly, projectedGdp, graphLabel, higherIsBetter: true, moneyUnit: PolicyWebRenderer.GetStatUnit(StatNodeId.Gdp)),
                () => _unemploymentGraph.Draw("Unemployment", history.Unemployment.Quarterly, projectedUnemployment, graphLabel, higherIsBetter: false, moneyUnit: null,
                    thresholdValue: _playerCountry.NaturalUnemploymentRate, thresholdLabel: "NAIRU"),
                () => _inflationGraph.Draw("Inflation", history.Inflation.Quarterly, null, graphLabel, higherIsBetter: false, moneyUnit: null),
                () => _approvalGraph.Draw("Approval rating", history.ApprovalRating.Quarterly, projectedApproval, graphLabel, higherIsBetter: true, moneyUnit: null),
                () => _povertyGraph.Draw("Poverty rate", history.PovertyRate.Quarterly, null, graphLabel, higherIsBetter: false, moneyUnit: null),
                () => _debtGraph.Draw("Debt-to-GDP", history.DebtToGdpRatio.Quarterly, null, graphLabel, higherIsBetter: false, moneyUnit: null,
                    thresholdValue: _playerCountry.ComfortableDebtToGdpPercent, thresholdLabel: "comfortable")
            });
            StatsSectionGap();
            DrawStatsSocietyRows(contentWidth);
            StatsSectionGap();
            DrawStatsPublishedBand(contentWidth);
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
            DrawTradeStatsContent();
        }
    }
}
