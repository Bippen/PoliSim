using System.Collections.Generic;
using System.Globalization;
using PoliSim.Data;
using UnityEngine;

namespace PoliSim.UI
{
    /// <summary>
    /// P5-C2 (2026-09-05, evening) - THE HEALTH FAMILY'S PLATE, drawn on Design's grammar (D15 item 3, board 9c: "one instrument
    /// grammar for the society stats - drawn on health, inherited by C3-C6"). One row, six cells, in this order: (1) name · unit ·
    /// source line, (2) figure, (3) band - the family's STATED range with the country's own tick and the other five at seed as dots,
    /// (4) reached by - bordered chips naming the dial or term, or 5c's arrow with its signed figure while a draft is live, (5) history -
    /// a row-end sparkline, Year 0 the dotted baseline, (6) honesty chips. Band forms: KEY·BOUNDED (a share of a whole - the bar
    /// fills to the tick, the ceiling dashed), KEY·OPEN (a level - hairline and ticks, the better end marked), ABSENT (the word on a
    /// dashed hairline with the reason), DISTRIBUTION (2a's stacked bar, its segments labelled - P5-C3 drew it first); a DERIVED row has no
    /// band. The plate is one drawing on the People page's society block (9c PLACE) and never split. Cell widths 210·120·319·250·56·96
    /// against the board's 1149, scaled to the width in force.
    ///
    /// P5-C3 (2026-09-06): the plate's core is shared - <see cref="DrawPlateRows"/> lays out any family's rows; a family is a list of
    /// <see cref="PlateRow"/>s and, where it has one, an extra row (health's supporting readouts) and an arrow rule for its drafted key.
    /// </summary>
    public partial class GameController
    {
        private enum PlateBand { Bounded, Open, Absent, None, Distribution }

        private readonly struct PlateRow
        {
            public readonly string Name, Unit, Source, Figure, AbsentReason;
            public readonly PlateBand Band;
            public readonly float Low, High, Own;
            public readonly float[] Peers;
            public readonly bool LowerIsBetter;
            public readonly string[] ReachedBy;
            public readonly IReadOnlyList<float> Series;
            public readonly string[] Honesty;
            public readonly bool CouplingDraft;
            public readonly float[] Segments;        // DISTRIBUTION: the stacked bar's parts, in order, summing to High
            public readonly string[] SegmentLabels;

            public PlateRow(string name, string unit, string source, string figure, PlateBand band, float low, float high, float own, float[] peers,
                bool lowerIsBetter, string[] reachedBy, IReadOnlyList<float> series, string[] honesty, bool couplingDraft, string absentReason = null,
                float[] segments = null, string[] segmentLabels = null)
            {
                Name = name; Unit = unit; Source = source; Figure = figure; Band = band; Low = low; High = high; Own = own; Peers = peers;
                LowerIsBetter = lowerIsBetter; ReachedBy = reachedBy; Series = series; Honesty = honesty; CouplingDraft = couplingDraft; AbsentReason = absentReason;
                Segments = segments; SegmentLabels = segmentLabels;
            }
        }

        /// <summary>Where the plate was laid out last frame - the film driver scrolls to it (UiScreenshotDriver, 04b_people_health_plate).</summary>
        private Rect _healthPlateLastArea;

        private static readonly CountryId[] PeerOrder = { CountryId.Sweden, CountryId.Germany, CountryId.France, CountryId.Italy, CountryId.Poland, CountryId.USA };

        private float[] HealthPeers(System.Func<HealthSeeds, float> read)
        {
            var peers = new List<float>();
            World world = _simulationManager.World;
            if (world == null) { return peers.ToArray(); }
            foreach (CountryId id in PeerOrder)
            {
                if (id == PlayerCountryId) { continue; }
                Country c = world.GetCountry(id);
                if (c?.Health == null || !c.Health.Seeded) { continue; }
                float v = read(c.Health);
                if (v >= 0f) { peers.Add(v); }
            }
            return peers.ToArray();
        }

        private static string PlateFigure(float value, int decimals, string symbol = "")
            => value < 0f ? "absent" : value.ToString(decimals == 0 ? "0" : "0." + new string('0', decimals), CultureInfo.InvariantCulture) + symbol;

        private void DrawHealthFamilyPlate()
        {
            Country country = _playerCountry;
            HealthSeeds h = country.Health;
            string countryName = DisplayName.Of(country.Id.ToString()).ToUpperInvariant();
            DrawCohortCaption($"HEALTH · SOCIETY · FAMILY 1 OF 6 · {countryName} · {_simulationManager.CurrentDate.Year}", "SOURCED · OECD 2022–25");
            if (h == null || !h.Seeded)
            {
                GUILayout.Label("This country carries no health family - the spine covers six, and this is not one of them.", _labelStyle);
                return;
            }

            EconomyState s = country.State;
            StatHistory history = country.History;
            bool draftLive = false;
            float draftHealthSpending = 0f, standingHealthSpending = 0f;
            foreach (SpendingLine line in country.SpendingLines)
            {
                if (!HealthFamily.IsHealthLine(line.Category)) { continue; }
                standingHealthSpending += line.Amount;
                if (_spendingLineInputs.TryGetValue(line.Category, out float drafted)) { draftHealthSpending += drafted; draftLive = true; }
                else { draftHealthSpending += line.Amount; }
            }

            string effectivenessChip = "EFFECTIVENESS (C7) ▸";
            string coverageYear = h.CoverageYear > 0 ? " · " + h.CoverageYear : "";
            string tmYear = h.TreatableMortalityYear > 0 ? " · " + h.TreatableMortalityYear : "";
            string waitYear = h.WaitYear > 0 ? " · " + h.WaitYear : "";
            bool waits = h.HasWaits;
            var rows = new List<PlateRow>
            {
                new PlateRow("Coverage", "% OF POPULATION · CORE SERVICES", "OECD HEALTH_PROT · TPRIBASI" + coverageYear, PlateFigure(s.HealthCoverage, 1, "%"),
                    PlateBand.Bounded, 0f, h.CoverageCeiling, s.HealthCoverage, HealthPeers(x => x.Coverage), false, new[] { "HEALTH LINE — HEAD ▸" }, history?.HealthCoverage.Quarterly, new[] { "SOURCED" }, true),
                new PlateRow("… of which public", "% OF POPULATION · GOVERNMENT / COMPULSORY", "OECD HEALTH_PROT · COVGCMED ÷ TPRIBASI", PlateFigure(HealthFamily.PublicCoverageNow(country), 1, "%"),
                    PlateBand.Bounded, 0f, 100f, HealthFamily.PublicCoverageNow(country), HealthPeers(x => x.CoveragePublic), false, new[] { "READOUT · NOTHING REACHES IT" }, null, new[] { "DERIVED" }, false),
                new PlateRow("Retiree coverage", "% OF THE 65+ COHORT", "DERIVED · F2 SUBSTRATE × COVERAGE", PlateFigure(HealthFamily.RetireeCoverage(country), 1, "%"),
                    PlateBand.None, 0f, 0f, 0f, null, false, new[] { "READOUT" }, null, new[] { "DERIVED" }, false),
                new PlateRow("Quality · treatable mortality", "DEATHS / 100 000 · AGE-STD · LOWER IS BETTER", "OECD HEALTH_STAT · TRTM" + tmYear, PlateFigure(s.TreatableMortality, 0),
                    PlateBand.Open, 40f, 120f, s.TreatableMortality, HealthPeers(x => x.TreatableMortality), true, new[] { "HEALTH LINE — AGE-COST ▸", "EFFICIENCY ▸", effectivenessChip }, history?.TreatableMortality.Quarterly, new[] { "SOURCED" }, true),
                waits
                    ? new PlateRow("Waiting · cataract", "MEAN DAYS · SPECIALIST TO TREATMENT", "OECD DF_WAITING · CM131_138" + waitYear, PlateFigure(s.WaitCataractDays, 0),
                        PlateBand.Open, 0f, 180f, s.WaitCataractDays, HealthPeers(x => x.WaitCataract), true, new[] { effectivenessChip }, null, new[] { "SOURCED" }, true)
                    : new PlateRow("Waiting · cataract", "MEAN DAYS · SPECIALIST TO TREATMENT", "OECD DF_WAITING · NO ROWS FOR " + country.Id.ToString().ToUpperInvariant(), "absent",
                        PlateBand.Absent, 0f, 180f, -1f, null, true, new[] { "NOT SIMULATED — EFFECTIVENESS REACHES QUALITY DIRECTLY" }, null, new[] { "ABSENT · STATED" }, false, "NO COMPARABLE SERIES PUBLISHED · SE IT PL REPORT"),
                waits
                    ? new PlateRow("Waiting · knee replacement", "MEAN DAYS", "OECD DF_WAITING · CM8154" + waitYear, PlateFigure(s.WaitKneeDays, 0),
                        PlateBand.Open, 0f, 400f, s.WaitKneeDays, HealthPeers(x => x.WaitKnee), true, new[] { effectivenessChip }, null, new[] { "SOURCED" }, true)
                    : new PlateRow("Waiting · knee replacement", "MEAN DAYS", "OECD DF_WAITING · NO ROWS FOR " + country.Id.ToString().ToUpperInvariant(), "absent",
                        PlateBand.Absent, 0f, 400f, -1f, null, true, new[] { "NOT SIMULATED — EFFECTIVENESS REACHES QUALITY DIRECTLY" }, null, new[] { "ABSENT · STATED" }, false, "NO COMPARABLE SERIES PUBLISHED · SE IT PL REPORT"),
            };

            Color areaInk = UiPalette.GetAreaColor(UiPalette.SystemArea.Welfare);
            string footText = "SEEDS: OECD SDMX, LATEST OBSERVATION PER COUNTRY, SEX TOTAL · THE OWN TICK IS THIS COUNTRY, THE DOTS ARE THE OTHER FIVE AT SEED · A BAND'S ENDS ARE THE FAMILY'S STATED RANGE, NOT THE DATA'S · ◂ MARKS THE BETTER END · COUPLINGS: THE HEALTH SPINE'S TABLES, DRAFT UNTIL MEASURED";
            _healthPlateLastArea = DrawPlateRows(rows, areaInk, footText, draftLive, row =>
            {
                if (!draftLive || !row.Name.StartsWith("Quality")) { return null; }
                float with = HealthFamily.ProjectTreatableMortality(country, draftHealthSpending);
                float without = HealthFamily.ProjectTreatableMortality(country, standingHealthSpending);
                return (with - without, true, "PER 100 000");
            }, extraRowHeightFor: (nameH, capH, srcH, smallH) => Mathf.Max(capH + smallH + srcH + StatsUnit(8f), nameH + capH + srcH + StatsUnit(8f)),
            drawExtraRow: (x, y, pad, styles) =>
            {
                // Supporting readouts: one row of small figures, borderless between them - moved by the quality key, never coupled.
                PoliSimWidgets.MeasuredLabel(new Rect(x[0] + pad, y + StatsUnit(2f), x[1] - x[0] - pad, styles.NameH), "Supporting readouts", h.HasSupporting ? styles.Name : styles.NameAbsent);
                PoliSimWidgets.MeasuredLabel(new Rect(x[0] + pad, y + StatsUnit(2f) + styles.NameH, x[1] - x[0] - pad, styles.CapH), "MOVED BY THE QUALITY KEY · NEVER COUPLED", styles.Caption);
                PoliSimWidgets.MeasuredLabel(new Rect(x[0] + pad, y + StatsUnit(2f) + styles.NameH + styles.CapH, x[1] - x[0] - pad, styles.SrcH), "OECD HCQO · DF_PC · DF_AC" + (h.SupportingYear > 0 ? " · " + h.SupportingYear : ""), styles.Source);
                if (h.HasSupporting)
                {
                    float cellW = (x[5] - x[1]) / 5f;
                    for (int i = 0; i < 5; i++)
                    {
                        float sx = x[1] + i * cellW + pad;
                        PoliSimWidgets.MeasuredLabel(new Rect(sx, y + StatsUnit(3f), cellW - pad, styles.CapH), HealthFamily.SupportingNames[i], styles.Caption);
                        PoliSimWidgets.MeasuredLabel(new Rect(sx, y + StatsUnit(3f) + styles.CapH, cellW - pad, styles.SmallH), PlateFigure(HealthFamily.SupportingNow(country, i), 1), styles.Small);
                        PoliSimWidgets.MeasuredLabel(new Rect(sx, y + StatsUnit(3f) + styles.CapH + styles.SmallH, cellW - pad, styles.SrcH), HealthFamily.SupportingUnits[i], styles.Source);
                    }
                    DrawPlateChips(new Rect(x[5] + pad, y + StatsUnit(4f), x[6] - x[5] - pad * 2f, styles.ExtraH - StatsUnit(8f)), new[] { "SUPPORTING · 5 OF 6" }, styles.Chip, PoliSimTheme.Hairline, bordered: true);
                }
                else
                {
                    PoliSimWidgets.MeasuredLabel(new Rect(x[1] + pad, y + StatsUnit(8f), x[5] - x[1] - pad, Mathf.Max(StatsUnit(12f), Mathf.Ceil(DeskCaptionHeight(styles.AbsentWord)))), "absent · THE HCQO FLOWS HOLD NO ROWS FOR THIS COUNTRY", styles.AbsentWord);
                    DrawPlateChips(new Rect(x[5] + pad, y + StatsUnit(4f), x[6] - x[5] - pad * 2f, styles.ExtraH - StatsUnit(8f)), new[] { "ABSENT · STATED" }, styles.Chip, PoliSimTheme.Hairline, bordered: true);
                }
            });
        }

        /// <summary>The plate's styles and measured heights, handed to a family's extra row.</summary>
        private readonly struct PlateStyles
        {
            public readonly GUIStyle Name, NameAbsent, Caption, Source, Small, Chip, AbsentWord;
            public readonly float NameH, CapH, SrcH, SmallH, ExtraH;
            public PlateStyles(GUIStyle name, GUIStyle nameAbsent, GUIStyle caption, GUIStyle source, GUIStyle small, GUIStyle chip, GUIStyle absentWord, float nameH, float capH, float srcH, float smallH, float extraH)
            {
                Name = name; NameAbsent = nameAbsent; Caption = caption; Source = source; Small = small; Chip = chip; AbsentWord = absentWord;
                NameH = nameH; CapH = capH; SrcH = srcH; SmallH = smallH; ExtraH = extraH;
            }
        }

        /// <summary>The plate's core (9c): the column head, one six-cell row per <see cref="PlateRow"/>, an optional extra row, the foot. Returns the
        /// area laid out (the film driver scrolls to it). <paramref name="arrowFor"/> returns, for a row whose key a live draft moves, the delta, whether
        /// lower is better and the unit - the 5c arrow in the row's own compact form replaces the chips.</summary>
        private Rect DrawPlateRows(List<PlateRow> rows, Color areaInk, string footText, bool draftLive, System.Func<PlateRow, (float Delta, bool LowerIsBetter, string Unit)?> arrowFor,
            System.Func<float, float, float, float, float> extraRowHeightFor = null, System.Action<float[], float, float, PlateStyles> drawExtraRow = null)
        {
            // Geometry: the board's six cells against 1149, scaled to the width in force; the column head is one caption line, a row three.
            float[] share = { 210f / 1149f, 120f / 1149f, 319f / 1149f, 250f / 1149f, 56f / 1149f, 96f / 1149f };
            GUIStyle head = DeskCaption(7.5f, PoliSimTheme.TextMuted);
            GUIStyle name = DeskBody(12f, PoliSimTheme.TextPrimary);
            GUIStyle nameAbsent = DeskBody(12f, PoliSimTheme.TextMuted);
            GUIStyle caption = DeskCaption(7.5f, PoliSimTheme.TextMuted);
            GUIStyle source = DeskCaption(6.5f, PoliSimTheme.TextMuted);
            GUIStyle figure = DeskCaption(17f, PoliSimTheme.TextPrimary, true, TextAnchor.UpperLeft);
            GUIStyle figureAbsent = DeskCaption(10.5f, PoliSimTheme.TextMuted, false, TextAnchor.UpperLeft);
            GUIStyle chip = DeskCaption(7f, PoliSimTheme.TextSecondary, false, TextAnchor.MiddleCenter);
            GUIStyle absentWord = DeskCaption(9f, PoliSimTheme.TextMuted, false, TextAnchor.MiddleLeft);
            GUIStyle draftChip = DeskCaption(7f, PoliSimTheme.Caution, false, TextAnchor.MiddleCenter);
            GUIStyle small = DeskCaption(11f, PoliSimTheme.TextPrimary, true, TextAnchor.UpperLeft);
            // Heights are the styles' own measure (DeskCaptionHeight), never a guessed unit: the first films' OVERFLOW guard found fixed heights 1-3 px short at both widths.
            float nameH = Mathf.Ceil(DeskCaptionHeight(name));
            float capH = Mathf.Ceil(DeskCaptionHeight(caption));
            float srcH = Mathf.Ceil(DeskCaptionHeight(source));
            float figH = Mathf.Ceil(DeskCaptionHeight(figure));
            float smallH = Mathf.Ceil(DeskCaptionHeight(small));
            GUIStyle foot = DeskCaptionWrapped(6.5f, PoliSimTheme.TextMuted);
            float headHeight = Mathf.Max(StatsUnit(12f), capH + StatsUnit(3f));
            float rowHeight = Mathf.Max(nameH + capH + srcH + StatsUnit(8f), figH + StatsUnit(12f), StatsUnit(38f));
            float extraHeight = extraRowHeightFor != null ? extraRowHeightFor(nameH, capH, srcH, smallH) : 0f;
            float pad = StatsUnit(4f);
            float footHeight = Mathf.Ceil(foot.CalcHeight(new GUIContent(footText), Mathf.Max(10f, Screen.width * 0.8f - pad * 2f))) + StatsUnit(4f);
            float total = headHeight + rows.Count * rowHeight + extraHeight + footHeight;
            Rect area = GUILayoutUtility.GetRect(10f, total, GUILayout.ExpandWidth(true));
            if (Event.current.type != EventType.Repaint) { return area; }

            float[] x = new float[7];
            x[0] = area.x;
            for (int i = 0; i < 6; i++) { x[i + 1] = x[i] + area.width * share[i]; }

            string[] heads = { "STAT · UNIT · SOURCE LINE", "FIGURE", "BAND · OWN TICK · FIVE PEERS", "REACHED BY · 5c ARROW WHEN A DRAFT IS LIVE", "HISTORY", "HONESTY" };
            for (int i = 0; i < 6; i++) { PoliSimWidgets.MeasuredLabel(new Rect(x[i] + pad, area.y, x[i + 1] - x[i] - pad, headHeight), heads[i], head); }
            PoliSimTheme.Rule(new Rect(area.x, area.y + headHeight - 1f, area.width, 1f), PoliSimTheme.Hairline);

            float y = area.y + headHeight;
            for (int r = 0; r < rows.Count; r++, y += rowHeight)
            {
                PlateRow row = rows[r];
                bool absent = row.Band == PlateBand.Absent;
                PoliSimWidgets.MeasuredLabel(new Rect(x[0] + pad, y + StatsUnit(2f), x[1] - x[0] - pad, nameH), row.Name, absent ? nameAbsent : name);
                PoliSimWidgets.MeasuredLabel(new Rect(x[0] + pad, y + StatsUnit(2f) + nameH, x[1] - x[0] - pad, capH), row.Unit, caption);
                PoliSimWidgets.MeasuredLabel(new Rect(x[0] + pad, y + StatsUnit(2f) + nameH + capH, x[1] - x[0] - pad, srcH), row.Source, source);
                PoliSimWidgets.MeasuredLabel(new Rect(x[1] + pad, y + StatsUnit(4f), x[2] - x[1] - pad, Mathf.Max(figH, StatsUnit(22f))), row.Figure, absent ? figureAbsent : figure);
                var band = new Rect(x[2] + pad, y + StatsUnit(6f), x[3] - x[2] - pad * 2f, rowHeight - StatsUnit(12f));
                DrawPlateBand(band, row, areaInk, caption);
                var reach = new Rect(x[3] + pad, y + StatsUnit(4f), x[4] - x[3] - pad * 2f, rowHeight - StatsUnit(8f));
                var arrow = arrowFor?.Invoke(row);
                if (arrow.HasValue) { DrawPlateArrow(reach, arrow.Value.Delta, arrow.Value.LowerIsBetter, arrow.Value.Unit, caption, srcH, row); }
                else { DrawPlateChips(reach, row.ReachedBy, chip, PoliSimTheme.Hairline, bordered: !absent && row.Band != PlateBand.None); }
                var spark = new Rect(x[4] + pad, y + (rowHeight - StatsUnit(10f)) * 0.5f, Mathf.Max(8f, x[5] - x[4] - pad * 2f), StatsUnit(10f));
                if (row.Series != null && row.Series.Count >= 2) { GraphRenderer.DrawSparkline(spark, row.Series, areaInk); }
                else if (row.Band != PlateBand.None && !absent) { DeskDottedBaseline(spark); }
                var honesty = new Rect(x[5] + pad, y + StatsUnit(4f), x[6] - x[5] - pad * 2f, rowHeight - StatsUnit(8f));
                var stamps = new List<string>(row.Honesty);
                if (row.CouplingDraft) { stamps.Add("COUPLING DRAFT"); }
                DrawPlateChips(honesty, stamps.ToArray(), chip, PoliSimTheme.Hairline, bordered: true, lastInCaution: row.CouplingDraft, cautionStyle: draftChip);
                PoliSimTheme.Rule(new Rect(area.x, y + rowHeight - 1f, area.width, 1f), PoliSimTheme.RuleRow);
            }

            if (drawExtraRow != null && extraHeight > 0f)
            {
                drawExtraRow(x, y, pad, new PlateStyles(name, nameAbsent, caption, source, small, chip, absentWord, nameH, capH, srcH, smallH, extraHeight));
                y += extraHeight;
            }
            PoliSimTheme.Rule(new Rect(area.x, y - 1f, area.width, 1f), PoliSimTheme.Hairline);
            GUI.Label(new Rect(area.x + pad, y + StatsUnit(2f), area.width - pad * 2f, footHeight - StatsUnit(2f)), footText, foot);
            return area;
        }

        /// <summary>The band cell: BOUNDED fills 2a's one-axis bar to the own tick with the ceiling dashed; OPEN is a hairline with the own tick and the peers as
        /// dots, the better end marked; ABSENT is the word on a dashed hairline with the reason; DISTRIBUTION is 2a's stacked bar, the segments in three
        /// tints of the area ink with their labels beneath; a DERIVED row prints the no-band line.</summary>
        private void DrawPlateBand(Rect cell, PlateRow row, Color ink, GUIStyle caption)
        {
            float lineY = cell.y + cell.height * 0.42f;
            float labelY = lineY + StatsUnit(5f);
            float labelH = Mathf.Ceil(DeskCaptionHeight(caption));
            GUIStyle left = DeskCaption(7f, PoliSimTheme.TextMuted, false, TextAnchor.MiddleLeft);
            GUIStyle right = DeskCaption(7f, PoliSimTheme.TextMuted, false, TextAnchor.MiddleRight);
            switch (row.Band)
            {
                case PlateBand.None:
                    PoliSimWidgets.MeasuredLabel(new Rect(cell.x, cell.y, cell.width, cell.height), "NO BAND — A DERIVATION PRINTS ITS FIGURE ONLY", left);
                    return;
                case PlateBand.Absent:
                    DrawDashedRule(new Rect(cell.x, lineY, cell.width, 1f), PoliSimTheme.Hairline, 4f, 3f);
                    PoliSimWidgets.MeasuredLabel(new Rect(cell.x, cell.y, cell.width * 0.4f, lineY - cell.y), row.Figure == "to fetch" ? "TO FETCH" : "ABSENT", DeskCaption(9f, PoliSimTheme.TextMuted));
                    PoliSimWidgets.MeasuredLabel(new Rect(cell.x, labelY, cell.width, labelH), row.AbsentReason ?? "", left);
                    return;
                case PlateBand.Distribution:
                    {
                        // 2a's stacked bar: the segments fill the whole range in three tints of the area ink, hairlines between; the labels beneath in wedge order.
                        var bar = new Rect(cell.x, lineY - StatsUnit(4f), cell.width, StatsUnit(9f));
                        float sum = 0f;
                        if (row.Segments != null) { foreach (float v in row.Segments) { sum += Mathf.Max(0f, v); } }
                        sum = Mathf.Max(0.0001f, sum);
                        float sx = bar.x;
                        for (int i = 0; row.Segments != null && i < row.Segments.Length; i++)
                        {
                            float w = bar.width * Mathf.Max(0f, row.Segments[i]) / sum;
                            Color tint = i == 0 ? PoliSimTheme.Tint(ink, 0.35f) : i == 1 ? PoliSimTheme.Tint(ink, 0.65f) : ink;
                            PoliSimTheme.Rule(new Rect(sx, bar.y, w, bar.height), tint);
                            if (i > 0) { PoliSimTheme.Rule(new Rect(sx, bar.y - 1f, 1f, bar.height + 2f), PoliSimTheme.Card); }
                            sx += w;
                        }
                        if (row.Segments != null && row.SegmentLabels != null)
                        {
                            float cellW = cell.width / row.Segments.Length;
                            for (int i = 0; i < row.Segments.Length && i < row.SegmentLabels.Length; i++)
                            {
                                string text = row.Segments[i].ToString(row.High < 20f ? "0.0" : "0", CultureInfo.InvariantCulture) + " · " + row.SegmentLabels[i];   // whole points on a percentage bar (the third of a 319-px band at 1280 holds no decimals); one decimal where the range is small (the environment's tonnes: 0.56 is not "1")
                                GUIStyle style = i == row.Segments.Length - 1 ? right : left;
                                PoliSimWidgets.MeasuredLabel(new Rect(cell.x + i * cellW, labelY + StatsUnit(2f), cellW, labelH), text, style);
                            }
                        }
                        return;
                    }
            }

            float span = Mathf.Max(0.0001f, row.High - row.Low);
            float Px(float v) => cell.x + Mathf.Clamp01((v - row.Low) / span) * cell.width;
            var track = new Rect(cell.x, lineY, cell.width, 1f);
            if (row.Band == PlateBand.Bounded)
            {
                var bar = new Rect(cell.x, lineY - StatsUnit(3f), cell.width, StatsUnit(7f));
                PoliSimTheme.Rule(bar, PoliSimTheme.BarTrack);
                PoliSimTheme.Rule(new Rect(bar.x, bar.y, Mathf.Max(0f, Px(row.Own) - bar.x), bar.height), ink);
                DrawDashedRule(new Rect(cell.xMax - 1f, bar.y - StatsUnit(2f), 1f, bar.height + StatsUnit(4f)), PoliSimTheme.TextMuted, 2f, 2f);
                string lowText = row.Low.ToString("0", CultureInfo.InvariantCulture) + (row.LowerIsBetter ? " ◂ BETTER" : "");
                string highText = row.High.ToString("0", CultureInfo.InvariantCulture) + (row.LowerIsBetter ? "" : " · CEILING");
                PoliSimWidgets.MeasuredLabel(new Rect(cell.x, labelY + StatsUnit(2f), cell.width * 0.5f, labelH), lowText, left);
                PoliSimWidgets.MeasuredLabel(new Rect(cell.x + cell.width * 0.5f, labelY + StatsUnit(2f), cell.width * 0.5f, labelH), highText, right);
            }
            else
            {
                PoliSimTheme.Rule(track, PoliSimTheme.HairlineStrong);
                string lowText = row.Low.ToString("0", CultureInfo.InvariantCulture) + (row.LowerIsBetter ? " ◂ BETTER" : "");
                string highText = (row.LowerIsBetter ? "" : "BETTER ▸ ") + row.High.ToString("0", CultureInfo.InvariantCulture);
                PoliSimWidgets.MeasuredLabel(new Rect(cell.x, labelY, cell.width * 0.5f, labelH), lowText, left);
                PoliSimWidgets.MeasuredLabel(new Rect(cell.x + cell.width * 0.5f, labelY, cell.width * 0.5f, labelH), highText, right);
                if (row.Peers != null && row.Peers.Length < 5)
                {
                    PoliSimWidgets.MeasuredLabel(new Rect(cell.x + cell.width * 0.3f, labelY, cell.width * 0.4f, labelH), (row.Peers.Length + 1) + " OF 6 REPORT", DeskCaption(7f, PoliSimTheme.TextMuted, false, TextAnchor.MiddleCenter));
                }
            }
            if (row.Peers != null)
            {
                foreach (float p in row.Peers)
                {
                    float px = Px(p);
                    PoliSimTheme.Rule(new Rect(px - 1.5f, lineY - 1.5f, 3f, 3f), PoliSimTheme.TextMuted);
                }
            }
            if (row.Own >= 0f)
            {
                float ox = Px(row.Own);
                PoliSimTheme.Rule(new Rect(ox - 1f, lineY - StatsUnit(5f), 2f, StatsUnit(10f)), PoliSimTheme.TextPrimary);
            }
        }

        /// <summary>5c's arrow in the plate's reached-by cell: the signed figure in direction-aware ink, an arrow rule whose length is the move against the row's
        /// stated range (a tenth of the range fills the cell), pointing left for a fall and right for a rise; zero is a figure and prints with the minimum arrow.</summary>
        private void DrawPlateArrow(Rect cell, float delta, bool lowerIsBetter, string unit, GUIStyle captionStyle, float srcHeight, PlateRow band)
        {
            bool good = lowerIsBetter ? delta < 0f : delta > 0f;
            Color ink = Mathf.Abs(delta) < 0.05f ? PoliSimTheme.TextPrimary : good ? PoliSimTheme.Good : PoliSimTheme.Bad;
            GUIStyle figureStyle = DeskCaption(10.5f, ink, true, TextAnchor.MiddleLeft);
            float figH = Mathf.Ceil(DeskCaptionHeight(figureStyle));
            string figure = delta.ToString("+0.0;-0.0;0.0", CultureInfo.InvariantCulture) + " " + unit;
            float figW = Mathf.Min(cell.width * 0.55f, figureStyle.CalcSize(new GUIContent(figure)).x + StatsUnit(4f));
            PoliSimWidgets.MeasuredLabel(new Rect(cell.x, cell.y, figW, figH), figure, figureStyle);
            float span = Mathf.Max(0.0001f, band.High - band.Low);
            float laneX = cell.x + figW + StatsUnit(4f);
            float laneW = Mathf.Max(8f, cell.xMax - laneX);
            float length = Mathf.Clamp(Mathf.Abs(delta) / (span * 0.1f), 0.06f, 1f) * laneW;
            float midY = cell.y + figH * 0.5f;
            float startX = delta < 0f ? laneX + laneW : laneX;
            float endX = delta < 0f ? startX - length : startX + length;
            PoliSimTheme.Rule(new Rect(Mathf.Min(startX, endX), midY - 0.5f, Mathf.Abs(endX - startX), 1.5f), ink);
            float head = StatsUnit(3f);
            for (int i = 0; i < 3; i++) { float t = (i + 1) / 3f; PoliSimTheme.Rule(new Rect(endX + (delta < 0f ? t * head : -t * head) - 0.5f, midY - head * (1f - t), 1f, head * 2f * (1f - t) + 1f), ink); }
            PoliSimWidgets.MeasuredLabel(new Rect(cell.x, cell.yMax - srcHeight, cell.width, srcHeight), "NEXT YEAR · WITH vs WITHOUT THIS DRAFT", DeskCaption(6.5f, PoliSimTheme.TextMuted));
        }

        /// <summary>Bordered chips laid left to right, wrapping to a second line; the last chip in Caution ink when the row's coupling is a draft.</summary>
        private void DrawPlateChips(Rect cell, string[] texts, GUIStyle chip, Color border, bool bordered, bool lastInCaution = false, GUIStyle cautionStyle = null)
        {
            if (texts == null || texts.Length == 0) { return; }
            float chipH = StatsUnit(11f);
            float gap = StatsUnit(3f);
            float cx = cell.x, cy = cell.y;
            for (int i = 0; i < texts.Length; i++)
            {
                GUIStyle style = lastInCaution && i == texts.Length - 1 && cautionStyle != null ? cautionStyle : chip;
                float w = Mathf.Min(cell.width, style.CalcSize(new GUIContent(texts[i])).x + StatsUnit(8f));
                if (cx + w > cell.xMax + 0.5f && cx > cell.x) { cx = cell.x; cy += chipH + gap; }
                if (cy + chipH > cell.yMax + 0.5f) { return; }
                var r = new Rect(cx, cy, w, chipH);
                if (bordered)
                {
                    Color ink = style == cautionStyle ? PoliSimTheme.Caution : border;
                    PoliSimTheme.Rule(new Rect(r.x, r.y, r.width, 1f), ink);
                    PoliSimTheme.Rule(new Rect(r.x, r.yMax - 1f, r.width, 1f), ink);
                    PoliSimTheme.Rule(new Rect(r.x, r.y, 1f, r.height), ink);
                    PoliSimTheme.Rule(new Rect(r.xMax - 1f, r.y, 1f, r.height), ink);
                }
                PoliSimWidgets.MeasuredLabel(r, texts[i], style);
                cx += w + gap;
            }
        }
    }
}
