using System.Collections.Generic;
using System.Globalization;
using PoliSim.Data;
using UnityEngine;

namespace PoliSim.UI
{
    /// <summary>
    /// P5-C2 (2026-09-05, evening) - THE HEALTH FAMILY'S PLATE, drawn on Design's grammar (D15 item 3, board 9c: "one instrument
    /// grammar for the society stats - drawn on health, inherited by C3-C6"). P5-C3 (2026-09-06): the plate's core is shared -
    /// <see cref="DrawPlateRows"/> lays out any family's rows; a family is a list of <see cref="PlateRow"/>s and, where it has one, an
    /// extra row (health's supporting readouts) and an arrow rule for its drafted key.
    ///
    /// <para><b>D16-1, board 10a (2026-09-09, §410) - THE LEGIBILITY PASS.</b> The grammar is 9c's; what changed is what is on at rest.
    /// A row is now <b>the name, and then four graphics</b> - figure · band · pips · sparkline - because the name is the only thing in
    /// the row that is language. The census (unit, source line, the reached-by chips, SOURCED/DERIVED, the rank numeral, the publisher)
    /// is behind the desk's one `†` tab (<see cref="DeskProvenance"/>), which adds LINES and never COLUMNS: three grids of the board,
    /// READING `296 · 132 · 543 · 40 · 44`, PROVENANCE `296 · 132 · 453 · 40 · 44 · 74` and gap rows `296 · 132 · 659`, all inside the
    /// 1149 sheet's 1119 of content at gap 16, and the band is the only track that flexes. The `n ⁄ 6` numeral and the `3 OF 6 REPORT`
    /// chip are one mark now - six pips, own tall and solid at its rank, a peer that reports short and solid, a country with no series an
    /// open slot. A gap row keeps its word and its reason at reading size: a mark may replace a word only where the word was a label on
    /// something already drawn, and it may not replace a gap.</para>
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
            /// <summary>D16 §3.3: the compact unit beside the figure - `%`, `d`, `⁄100k`. Null draws no glyph.</summary>
            public readonly string UnitGlyph;
            /// <summary>D16 §3.6: `◇` DATED (this row's vintage is older than its family's) or `‡` TWO DEFINITIONS, after the name. Null draws none.</summary>
            public readonly string Flag;

            public PlateRow(string name, string unit, string source, string figure, PlateBand band, float low, float high, float own, float[] peers,
                bool lowerIsBetter, string[] reachedBy, IReadOnlyList<float> series, string[] honesty, bool couplingDraft, string absentReason = null,
                float[] segments = null, string[] segmentLabels = null, string unitGlyph = null, string flag = null)
            {
                Name = name; Unit = unit; Source = source; Figure = figure; Band = band; Low = low; High = high; Own = own; Peers = peers;
                LowerIsBetter = lowerIsBetter; ReachedBy = reachedBy; Series = series; Honesty = honesty; CouplingDraft = couplingDraft; AbsentReason = absentReason;
                Segments = segments; SegmentLabels = segmentLabels; UnitGlyph = unitGlyph; Flag = flag;
            }
        }

        private string _plateFamilyName, _plateFamilyVintage, _plateFamilyPublisher;

        /// <summary>D16 §3.4: the family whose plate is drawn next - its name, its vintage (the years alone at rest) and its publisher (behind the tab).</summary>
        private void PlateFamily(string name, string vintage, string publisher)
        {
            _plateFamilyName = name; _plateFamilyVintage = vintage; _plateFamilyPublisher = publisher;
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
            if (h == null || !h.Seeded)
            {
                PlateFamily("Health", "", "");
                DrawPlateFamilyHeader("Health", "", "");
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
                new PlateRow("Coverage", "% OF POPULATION · CORE SERVICES", "OECD HEALTH_PROT · TPRIBASI" + coverageYear, PlateFigure(s.HealthCoverage, 1),
                    PlateBand.Bounded, 0f, h.CoverageCeiling, s.HealthCoverage, HealthPeers(x => x.Coverage), false, new[] { "HEALTH LINE — HEAD ▸" }, history?.HealthCoverage.Quarterly, new[] { "SOURCED" }, true, unitGlyph: "%"),
                new PlateRow("… of which public", "% OF POPULATION · GOVERNMENT / COMPULSORY", "OECD HEALTH_PROT · COVGCMED ÷ TPRIBASI", PlateFigure(HealthFamily.PublicCoverageNow(country), 1),
                    PlateBand.Bounded, 0f, 100f, HealthFamily.PublicCoverageNow(country), HealthPeers(x => x.CoveragePublic), false, new[] { "READOUT · NOTHING REACHES IT" }, null, new[] { "DERIVED" }, false, unitGlyph: "%"),
                new PlateRow("Retiree coverage", "% OF THE 65+ COHORT", "DERIVED · F2 SUBSTRATE × COVERAGE", PlateFigure(HealthFamily.RetireeCoverage(country), 1),
                    PlateBand.None, 0f, 0f, 0f, null, false, new[] { "READOUT" }, null, new[] { "DERIVED" }, false, unitGlyph: "%"),
                new PlateRow("Quality · treatable mortality", "DEATHS / 100 000 · AGE-STD · LOWER IS BETTER", "OECD HEALTH_STAT · TRTM" + tmYear, PlateFigure(s.TreatableMortality, 0),
                    PlateBand.Open, 40f, 120f, s.TreatableMortality, HealthPeers(x => x.TreatableMortality), true, new[] { "HEALTH LINE — AGE-COST ▸", "EFFICIENCY ▸", effectivenessChip }, history?.TreatableMortality.Quarterly, new[] { "SOURCED" }, true, unitGlyph: "⁄100k"),
                waits
                    ? new PlateRow("Waiting · cataract", "MEAN DAYS · SPECIALIST TO TREATMENT", "OECD DF_WAITING · CM131_138" + waitYear, PlateFigure(s.WaitCataractDays, 0),
                        PlateBand.Open, 0f, 180f, s.WaitCataractDays, HealthPeers(x => x.WaitCataract), true, new[] { effectivenessChip }, null, new[] { "SOURCED" }, true, unitGlyph: "d")
                    : new PlateRow("Waiting · cataract", "MEAN DAYS · SPECIALIST TO TREATMENT", "OECD DF_WAITING · NO ROWS FOR " + country.Id.ToString().ToUpperInvariant(), "absent",
                        PlateBand.Absent, 0f, 180f, -1f, null, true, new[] { "NOT SIMULATED — EFFECTIVENESS REACHES QUALITY DIRECTLY" }, null, new[] { "ABSENT · STATED" }, false, "NO COMPARABLE SERIES PUBLISHED · SE IT PL REPORT"),
                waits
                    ? new PlateRow("Waiting · knee replacement", "MEAN DAYS", "OECD DF_WAITING · CM8154" + waitYear, PlateFigure(s.WaitKneeDays, 0),
                        PlateBand.Open, 0f, 400f, s.WaitKneeDays, HealthPeers(x => x.WaitKnee), true, new[] { effectivenessChip }, null, new[] { "SOURCED" }, true, unitGlyph: "d")
                    : new PlateRow("Waiting · knee replacement", "MEAN DAYS", "OECD DF_WAITING · NO ROWS FOR " + country.Id.ToString().ToUpperInvariant(), "absent",
                        PlateBand.Absent, 0f, 400f, -1f, null, true, new[] { "NOT SIMULATED — EFFECTIVENESS REACHES QUALITY DIRECTLY" }, null, new[] { "ABSENT · STATED" }, false, "NO COMPARABLE SERIES PUBLISHED · SE IT PL REPORT"),
            };

            Color areaInk = UiPalette.GetAreaColor(UiPalette.SystemArea.Welfare);
            PlateFamily("Health", h.CoverageYear > 0 ? h.CoverageYear + "–25" : "2022–25", "SOURCED · OECD");
            string footText = "SEEDS: OECD SDMX, LATEST OBSERVATION PER COUNTRY, SEX TOTAL · THE OWN TICK IS THIS COUNTRY, THE SHORT TICKS ARE THE OTHER FIVE AT SEED · A BAND'S ENDS ARE THE FAMILY'S STATED RANGE, NOT THE DATA'S · COUPLINGS: THE HEALTH SPINE'S TABLES, DRAFT UNTIL MEASURED";
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
                if (DeskProvenance.On)
                {
                    PoliSimWidgets.MeasuredLabel(new Rect(x[0] + pad, y + StatsUnit(2f) + styles.NameH, x[1] - x[0] - pad, styles.CapH), "MOVED BY THE QUALITY KEY · NEVER COUPLED", styles.Caption);
                    PoliSimWidgets.MeasuredLabel(new Rect(x[0] + pad, y + StatsUnit(2f) + styles.NameH + styles.CapH, x[1] - x[0] - pad, styles.SrcH), "OECD HCQO · DF_PC · DF_AC" + (h.SupportingYear > 0 ? " · " + h.SupportingYear : ""), styles.Source);
                }
                if (h.HasSupporting)
                {
                    float cellW = (x[x.Length - 2] - x[1]) / 5f;
                    for (int i = 0; i < 5; i++)
                    {
                        float sx = x[1] + i * cellW + pad;
                        PoliSimWidgets.MeasuredLabel(new Rect(sx, y + StatsUnit(3f), cellW - pad, styles.CapH), HealthFamily.SupportingNames[i], styles.Caption);
                        PoliSimWidgets.MeasuredLabel(new Rect(sx, y + StatsUnit(3f) + styles.CapH, cellW - pad, styles.SmallH), PlateFigure(HealthFamily.SupportingNow(country, i), 1), styles.Small);
                        PoliSimWidgets.MeasuredLabel(new Rect(sx, y + StatsUnit(3f) + styles.CapH + styles.SmallH, cellW - pad, styles.SrcH), HealthFamily.SupportingUnits[i], styles.Source);
                    }
                }
                else
                {
                    PoliSimWidgets.MeasuredLabel(new Rect(x[1] + pad, y + StatsUnit(8f), x[x.Length - 2] - x[1] - pad, Mathf.Max(StatsUnit(12f), Mathf.Ceil(DeskCaptionHeight(styles.AbsentWord)))), "absent · THE HCQO FLOWS HOLD NO ROWS FOR THIS COUNTRY", styles.AbsentWord);
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

        /// <summary>
        /// D16 §3.4: a family's header is a section rule, not a card and not a tab - the name in serif 15 bold, a hairline to the right
        /// edge, and the vintage at mono 7 on the far right. The publisher joins it only in PROVENANCE (at rest the vintage is the years
        /// alone). Drawn by the plate itself so the same call can repeat it at the top of the scroll while the family is under the eye.
        /// </summary>
        private float DrawPlateFamilyHeader(Rect row, string name, string vintage, string publisher)
        {
            GUIStyle nameStyle = DeskBody(15f, PoliSimTheme.TextPrimary);
            GUIStyle vintageStyle = DeskCaption(7f, PoliSimTheme.TextMuted, false, TextAnchor.MiddleRight);
            string right = DeskProvenance.On && !string.IsNullOrEmpty(publisher) ? publisher + " · " + vintage : vintage;
            float nameW = nameStyle.CalcSize(new GUIContent(name)).x;
            float rightW = string.IsNullOrEmpty(right) ? 0f : vintageStyle.CalcSize(new GUIContent(right)).x + StatsUnit(6f);
            PoliSimTheme.Rule(new Rect(row.x, row.y, row.width, 1f), PoliSimTheme.Hairline);
            PoliSimWidgets.MeasuredLabel(new Rect(row.x + StatsUnit(14f), row.y + StatsUnit(3f), nameW, row.height - StatsUnit(8f)), name, nameStyle);
            float hairX = row.x + StatsUnit(14f) + nameW + StatsUnit(8f);
            float hairW = Mathf.Max(0f, row.xMax - rightW - StatsUnit(6f) - hairX);
            if (hairW > 0f) { PoliSimTheme.Rule(new Rect(hairX, row.y + row.height * 0.55f, hairW, 1f), PoliSimTheme.RuleLight); }
            if (rightW > 0f) { PoliSimWidgets.MeasuredLabel(new Rect(row.xMax - rightW, row.y + StatsUnit(3f), rightW, row.height - StatsUnit(8f)), right, vintageStyle); }
            return row.height;
        }

        /// <summary>The layout call: reserves the family header's row and draws it. Kept separate so a family with no rows still carries its name.</summary>
        private void DrawPlateFamilyHeader(string name, string vintage, string publisher)
        {
            float h = Mathf.Ceil(DeskCaptionHeight(DeskBody(15f, PoliSimTheme.TextPrimary))) + StatsUnit(8f);
            Rect row = GUILayoutUtility.GetRect(10f, h, GUILayout.ExpandWidth(true));
            if (Event.current.type == EventType.Repaint) { DrawPlateFamilyHeader(row, name, vintage, publisher); }
        }

        /// <summary>
        /// The plate's core, re-composed for D16-1 (board 10a). One family: the sticky family header, one row per <see cref="PlateRow"/>,
        /// an optional extra row, the foot. Returns the area laid out (the film driver scrolls to it). <paramref name="arrowFor"/> returns,
        /// for a row whose key a live draft moves, the delta, whether lower is better and the unit - 5c's arrow paints in the band cell in
        /// place of everything else, which is where its subject is.
        /// </summary>
        private Rect DrawPlateRows(List<PlateRow> rows, Color areaInk, string footText, bool draftLive, System.Func<PlateRow, (float Delta, bool LowerIsBetter, string Unit)?> arrowFor,
            System.Func<float, float, float, float, float> extraRowHeightFor = null, System.Action<float[], float, float, PlateStyles> drawExtraRow = null)
        {
            bool prov = DeskProvenance.On;
            string familyName = _plateFamilyName, familyVintage = _plateFamilyVintage, familyPublisher = _plateFamilyPublisher;
            _plateFamilyName = _plateFamilyVintage = _plateFamilyPublisher = null;   // one header per plate: a family that did not set one draws none
            // D16 §3.2: the board's three grids live in PlateGrid, which is also what §8.4's acceptance bar reads - one set of numbers,
            // drawn and asserted from the same place. The band (track 3) is the only track that flexes.
            float[] tracks = PlateGrid.For(prov);
            float[] gapTracks = PlateGrid.GapRow;

            GUIStyle name = DeskBody(12.5f, PoliSimTheme.TextPrimary);
            GUIStyle nameAbsent = DeskBody(12.5f, PoliSimTheme.TextMuted);
            GUIStyle flagStyle = DeskCaption(9f, PoliSimTheme.TextMuted);
            GUIStyle caption = DeskCaption(7f, PoliSimTheme.TextMuted);
            GUIStyle source = DeskCaption(6.5f, PoliSimTheme.TextMuted);
            GUIStyle figure = DeskCaption(18f, PoliSimTheme.TextPrimary, true, TextAnchor.MiddleRight);
            GUIStyle figureUnit = DeskCaption(8f, PoliSimTheme.TextMuted, false, TextAnchor.MiddleLeft);
            GUIStyle figureAbsent = DeskCaption(11f, PoliSimTheme.TextMuted, true, TextAnchor.MiddleRight);
            GUIStyle chip = DeskCaption(6.5f, PoliSimTheme.TextSecondary, false, TextAnchor.MiddleCenter);
            GUIStyle draftChip = DeskCaption(6.5f, PoliSimTheme.Caution, false, TextAnchor.MiddleCenter);
            GUIStyle rankStyle = DeskCaption(7f, PoliSimTheme.TextMuted, true, TextAnchor.MiddleCenter);
            GUIStyle reason = DeskBodyWrapped(11.5f, PoliSimTheme.TextPrimary);
            GUIStyle small = DeskCaption(11f, PoliSimTheme.TextPrimary, true, TextAnchor.UpperLeft);
            GUIStyle absentWord = DeskCaption(9f, PoliSimTheme.TextMuted, false, TextAnchor.MiddleLeft);
            // Heights are the styles' own measure (DeskCaptionHeight), never a guessed unit: the first films' OVERFLOW guard found fixed heights 1-3 px short at both widths.
            float nameH = Mathf.Ceil(DeskCaptionHeight(name));
            float capH = Mathf.Ceil(DeskCaptionHeight(caption));
            float srcH = Mathf.Ceil(DeskCaptionHeight(source));
            float figH = Mathf.Ceil(DeskCaptionHeight(figure));
            float smallH = Mathf.Ceil(DeskCaptionHeight(small));
            float rankH = Mathf.Ceil(DeskCaptionHeight(rankStyle));
            float lane = StatsUnit(15f);          // §3.3 the band's lane
            float pipsLane = StatsUnit(11f);      // §3.3 the pips' lane
            float chipH = StatsUnit(11f);
            GUIStyle foot = DeskCaptionWrapped(6.5f, PoliSimTheme.TextMuted);

            GUIStyle segment = DeskCaption(6.5f, PoliSimTheme.TextMuted, false, TextAnchor.MiddleCenter);
            float segH = Mathf.Ceil(DeskCaptionHeight(segment));
            bool anyDistribution = false;
            foreach (PlateRow r in rows) { if (r.Band == PlateBand.Distribution) { anyDistribution = true; break; } }
            float nameBlock = prov ? nameH + capH + srcH : nameH;
            float bandBlock = lane + (anyDistribution ? segH : 0f) + (prov ? chipH + StatsUnit(3f) : 0f);
            float pipsBlock = pipsLane + (prov ? rankH : 0f);
            float rowHeight = Mathf.Max(Mathf.Max(nameBlock, figH), Mathf.Max(bandBlock, pipsBlock)) + StatsUnit(12f);
            float extraHeight = extraRowHeightFor != null ? extraRowHeightFor(nameH, capH, srcH, smallH) : 0f;
            float pad = StatsUnit(4f);
            float headerHeight = string.IsNullOrEmpty(familyName) ? 0f : Mathf.Ceil(DeskCaptionHeight(DeskBody(15f, PoliSimTheme.TextPrimary))) + StatsUnit(8f);
            float footHeight = Mathf.Ceil(foot.CalcHeight(new GUIContent(footText), Mathf.Max(10f, Screen.width * 0.8f - pad * 2f))) + StatsUnit(4f);

            // A gap row's reason is the only prose on the page and it earns its reading size, so the row grows to hold it.
            float gapReasonWidth = 0f, gapRowHeight = rowHeight;
            {
                gapReasonWidth = Mathf.Max(10f, (gapTracks[2] / PlateGrid.Content) * Mathf.Max(10f, Screen.width * 0.8f));
                foreach (PlateRow r in rows)
                {
                    if (r.Band != PlateBand.Absent || string.IsNullOrEmpty(r.AbsentReason)) { continue; }
                    gapRowHeight = Mathf.Max(gapRowHeight, Mathf.Ceil(reason.CalcHeight(new GUIContent(r.AbsentReason), gapReasonWidth)) + StatsUnit(16f));
                }
            }
            int gapRows = 0;
            foreach (PlateRow r in rows) { if (r.Band == PlateBand.Absent) { gapRows++; } }
            float total = headerHeight + (rows.Count - gapRows) * rowHeight + gapRows * gapRowHeight + extraHeight + footHeight;
            Rect area = GUILayoutUtility.GetRect(10f, total, GUILayout.ExpandWidth(true));
            if (Event.current.type != EventType.Repaint) { return area; }

            float[] x = PlateGrid.Tracks(area, tracks);
            float[] gx = PlateGrid.Tracks(area, gapTracks);

            float y = area.y;
            if (headerHeight > 0f)
            {
                DrawPlateFamilyHeader(new Rect(area.x, y, area.width, headerHeight), familyName, familyVintage, familyPublisher);
                // §3.4 sticky: while the family is under the eye and its header has scrolled off, the header repeats at the top of the
                // scroll on its own piece of paper - the same device the 1920 wedge used (a sheet piece drawn above the window).
                float visibleTop = _demographicsScrollPosition.y;
                if (area.y < visibleTop && area.yMax > visibleTop + headerHeight * 2f)
                {
                    var stuck = new Rect(area.x, visibleTop, area.width, headerHeight);
                    PoliSimTheme.Rule(stuck, PoliSimTheme.Card);
                    DrawPlateFamilyHeader(stuck, familyName, familyVintage, familyPublisher);
                }
                y += headerHeight;
            }

            for (int r = 0; r < rows.Count; r++)
            {
                PlateRow row = rows[r];
                bool gap = row.Band == PlateBand.Absent;
                float h = gap ? gapRowHeight : rowHeight;
                PoliSimTheme.Rule(new Rect(area.x, y, area.width, 1f), PoliSimTheme.RuleRow);
                if (gap) { DrawPlateGapRow(new Rect(area.x, y, area.width, h), gx, row, reason, pad); }
                else { DrawPlateRow(new Rect(area.x, y, area.width, h), x, row, areaInk, prov, arrowFor, name, nameAbsent, flagStyle, caption, source, figure, figureUnit, figureAbsent, chip, draftChip, rankStyle, nameH, capH, srcH, figH, rankH, lane, pipsLane, chipH, pad, anyDistribution ? segH : 0f); }
                y += h;
            }

            if (drawExtraRow != null && extraHeight > 0f)
            {
                PoliSimTheme.Rule(new Rect(area.x, y, area.width, 1f), PoliSimTheme.RuleRow);
                drawExtraRow(x, y, pad, new PlateStyles(name, nameAbsent, caption, source, small, chip, absentWord, nameH, capH, srcH, smallH, extraHeight));
                y += extraHeight;
            }
            PoliSimTheme.Rule(new Rect(area.x, y - 1f, area.width, 1f), PoliSimTheme.Hairline);
            GUI.Label(new Rect(area.x + pad, y + StatsUnit(2f), area.width - pad * 2f, footHeight - StatsUnit(2f)), footText, foot);
            return area;
        }

        /// <summary>One row of the glance layer: the name, and then four graphics - figure · band · pips · sparkline (D16 §3.3).</summary>
        private void DrawPlateRow(Rect row, float[] x, PlateRow data, Color areaInk, bool prov,
            System.Func<PlateRow, (float Delta, bool LowerIsBetter, string Unit)?> arrowFor,
            GUIStyle name, GUIStyle nameAbsent, GUIStyle flagStyle, GUIStyle caption, GUIStyle source, GUIStyle figure, GUIStyle figureUnit, GUIStyle figureAbsent,
            GUIStyle chip, GUIStyle draftChip, GUIStyle rankStyle,
            float nameH, float capH, float srcH, float figH, float rankH, float lane, float pipsLane, float chipH, float pad, float distributionExtra)
        {
            float top = row.y + StatsUnit(6f);
            // 1 · Name, with its qualification glyph. The unit and the source line are lines the tab adds beneath it.
            float nameW = x[1] - x[0] - pad * 2f;
            PoliSimWidgets.MeasuredLabel(new Rect(x[0] + pad, top, nameW, nameH), data.Name, name);
            if (!string.IsNullOrEmpty(data.Flag))
            {
                float w = name.CalcSize(new GUIContent(data.Name)).x;
                PoliSimWidgets.MeasuredLabel(new Rect(x[0] + pad + w + StatsUnit(3f), top, StatsUnit(12f), nameH), data.Flag, flagStyle);
            }
            if (prov)
            {
                PoliSimWidgets.MeasuredLabel(new Rect(x[0] + pad, top + nameH, nameW, capH), data.Unit, caption);
                PoliSimWidgets.MeasuredLabel(new Rect(x[0] + pad, top + nameH + capH, nameW, srcH), data.Source, source);
            }

            // 2 · Figure, right-aligned, with the compact unit glyph after it.
            float unitW = string.IsNullOrEmpty(data.UnitGlyph) ? 0f : figureUnit.CalcSize(new GUIContent(data.UnitGlyph)).x;
            var figureCell = new Rect(x[1] + pad, top, x[2] - x[1] - pad * 2f - unitW - (unitW > 0f ? StatsUnit(3f) : 0f), Mathf.Max(figH, StatsUnit(20f)));
            PoliSimWidgets.MeasuredLabel(figureCell, data.Figure, data.Own < 0f ? figureAbsent : figure);
            if (unitW > 0f) { PoliSimWidgets.MeasuredLabel(new Rect(figureCell.xMax + StatsUnit(3f), top, unitW + StatsUnit(2f), figureCell.height), data.UnitGlyph, figureUnit); }

            // 3 · Band. At rest the cell holds only the band; the tab adds the reached-by chips beneath the axis; a live draft paints
            // 5c's arrow in the cell in place of everything else, and COUPLING DRAFT follows it there.
            var bandCell = new Rect(x[2] + pad, top, x[3] - x[2] - pad * 2f, data.Band == PlateBand.Distribution ? lane + distributionExtra : lane);
            var arrow = arrowFor?.Invoke(data);
            if (arrow.HasValue)
            {
                DrawPlateArrow(new Rect(bandCell.x, bandCell.y, bandCell.width, row.height - StatsUnit(12f)), arrow.Value.Delta, arrow.Value.LowerIsBetter, arrow.Value.Unit, caption, srcH, data);
                if (DeskProvenance.ShowsCouplingDraft(data.CouplingDraft, true)) { DrawPlateChips(new Rect(bandCell.x, bandCell.yMax + StatsUnit(1f), bandCell.width, chipH), new[] { "COUPLING DRAFT" }, draftChip, PoliSimTheme.Caution, bordered: true); }
            }
            else
            {
                DrawPlateBand(bandCell, data, areaInk, caption);
                if (prov && data.ReachedBy != null && data.Band != PlateBand.None)
                {
                    DrawPlateChips(new Rect(bandCell.x, bandCell.yMax + StatsUnit(3f), bandCell.width, chipH), data.ReachedBy, chip, PoliSimTheme.Hairline, bordered: true);
                }
            }

            // 4 · Pips - rank and coverage in one mark, replacing both the numeral and the report chip.
            var pipsCell = new Rect(x[3] + pad, top + Mathf.Max(0f, (lane - pipsLane) * 0.5f), x[4] - x[3] - pad, pipsLane);
            int rank = PlateRank(data, out int reporting);
            if (data.Band != PlateBand.None && data.Own >= 0f && reporting > 0)
            {
                DrawPlatePips(pipsCell, rank, reporting);
                if (prov) { PoliSimWidgets.MeasuredLabel(new Rect(pipsCell.x, pipsCell.yMax + StatsUnit(1f), pipsCell.width, rankH), rank + " ⁄ " + reporting, rankStyle); }
            }

            // 5 · Sparkline, 36 x 10, unchanged from 9c.
            var spark = new Rect(x[4] + pad, top + Mathf.Max(0f, (lane - StatsUnit(10f)) * 0.5f), Mathf.Max(8f, x[5] - x[4] - pad * 2f), StatsUnit(10f));
            if (data.Series != null && data.Series.Count >= 2) { GraphRenderer.DrawSparkline(spark, data.Series, areaInk); }
            else if (data.Band != PlateBand.None) { DeskDottedBaseline(spark); }

            // 6 · Honesty - PROVENANCE only, because SOURCED and DERIVED qualify nothing you can already see.
            if (DeskProvenance.ShowsHonestyColumn(prov) && x.Length > 6)
            {
                DrawPlateChips(new Rect(x[5] + pad, top, x[6] - x[5] - pad, row.height - StatsUnit(12f)), data.Honesty, chip, PoliSimTheme.Hairline, bordered: true);
            }
        }

        /// <summary>A gap row (§3.5): full height, a dashed left edge, the word once in the figure cell, and the reason at reading size.</summary>
        private void DrawPlateGapRow(Rect row, float[] gx, PlateRow data, GUIStyle reason, float pad)
        {
            GUIStyle word = DeskCaption(11f, PoliSimTheme.TextMuted, true, TextAnchor.MiddleRight);
            GUIStyle nameStyle = DeskBody(12.5f, PoliSimTheme.TextMuted);
            for (float yy = row.y + StatsUnit(3f); yy < row.yMax - StatsUnit(3f); yy += StatsUnit(7f))
            {
                PoliSimTheme.Rule(new Rect(row.x, yy, StatsUnit(3f), Mathf.Min(StatsUnit(4f), row.yMax - StatsUnit(3f) - yy)), PoliSimTheme.EdgeDashed);
            }
            PoliSimWidgets.MeasuredLabel(new Rect(gx[0] + StatsUnit(11f), row.y + StatsUnit(8f), gx[1] - gx[0] - StatsUnit(11f) - pad, Mathf.Ceil(DeskCaptionHeight(nameStyle))), data.Name, nameStyle);
            PoliSimWidgets.MeasuredLabel(new Rect(gx[1] + pad, row.y + StatsUnit(8f), gx[2] - gx[1] - pad * 2f, Mathf.Ceil(DeskCaptionHeight(word))), data.Figure == "billed" ? "BILLED" : "ABSENT", word);
            GUI.Label(new Rect(gx[2] + pad, row.y + StatsUnit(6f), gx[3] - gx[2] - pad * 2f, row.height - StatsUnit(12f)), data.AbsentReason ?? "", reason);
        }

        /// <summary>Where the country stands among those that report: 1 = best. <paramref name="reporting"/> counts own plus the peers that carry a series.</summary>
        private static int PlateRank(PlateRow row, out int reporting)
        {
            reporting = 1 + (row.Peers?.Length ?? 0);
            if (row.Own < 0f) { reporting = row.Peers?.Length ?? 0; return 0; }
            int better = 0;
            if (row.Peers != null)
            {
                foreach (float p in row.Peers) { if (row.LowerIsBetter ? p < row.Own : p > row.Own) { better++; } }
            }
            return better + 1;
        }

        /// <summary>
        /// D16 §3.3, the pips: six cells 4 px wide at gap 2, bottom-aligned in an 11 px lane, in rank order best → worst. The own cell is
        /// 11 px and solid, a peer that reports is 6 px and solid, a country with no series is a 6 px open slot. "Second of three
        /// reporting" reads with no words at all, and it replaces both the `n ⁄ 6` numeral and the `3 OF 6 REPORT` chip.
        /// </summary>
        private static void DrawPlatePips(Rect cell, int rank, int reporting)
        {
            float w = StatsUnit(4f), gap = StatsUnit(2f);
            float shortH = StatsUnit(6f);
            for (int i = 0; i < 6; i++)
            {
                float px = cell.x + i * (w + gap);
                if (px + w > cell.xMax + 0.5f) { return; }
                bool reports = i < reporting;
                bool own = i == rank - 1;
                if (own)
                {
                    PoliSimTheme.Rule(new Rect(px, cell.y, w, cell.height), PoliSimTheme.TextPrimary);
                }
                else if (reports)
                {
                    PoliSimTheme.Rule(new Rect(px, cell.yMax - shortH, w, shortH), PoliSimTheme.Peer);
                }
                else
                {
                    var slot = new Rect(px, cell.yMax - shortH, w, shortH);
                    PoliSimTheme.Rule(new Rect(slot.x, slot.y, slot.width, 1f), PoliSimTheme.EdgeDashed);
                    PoliSimTheme.Rule(new Rect(slot.x, slot.yMax - 1f, slot.width, 1f), PoliSimTheme.EdgeDashed);
                    PoliSimTheme.Rule(new Rect(slot.x, slot.y, 1f, slot.height), PoliSimTheme.EdgeDashed);
                    PoliSimTheme.Rule(new Rect(slot.xMax - 1f, slot.y, 1f, slot.height), PoliSimTheme.EdgeDashed);
                }
            }
        }

        /// <summary>
        /// The band cell (§3.3), a 15 px lane: [7 px gutter] [axis, flex] [7 px gutter] at gap 5. The axis is the family's stated range;
        /// a share-of-a-whole row fills to its own tick; the five peers are short ticks on the axis; the reach runs from the peer median
        /// to the own tick and is Good when the own tick is on the better side and TextMuted when it is not - <b>never Bad</b>, because the
        /// reach describes an inherited level and being behind the peer median is not a verdict. The own tick is the only black mark in
        /// the cell. A DERIVED row draws an empty lane: the empty lane is the statement, and there is no sentence.
        /// </summary>
        private void DrawPlateBand(Rect cell, PlateRow row, Color ink, GUIStyle caption)
        {
            if (row.Band == PlateBand.None) { return; }
            float gutter = StatsUnit(7f), gapPx = StatsUnit(5f);
            float axisX = cell.x + gutter + gapPx;
            float axisW = Mathf.Max(4f, cell.width - (gutter + gapPx) * 2f);
            float laneH = Mathf.Min(cell.height, StatsUnit(15f));
            float u = laneH / 15f;   // the lane's own unit, so the board's y offsets hold at any scale
            float axisY = cell.y + 6f * u;

            if (row.Band == PlateBand.Distribution)
            {
                // 2a's stacked bar, kept: the board's three forms do not cover a distribution row, and dropping it would delete content
                // D16 never asked to remove (§410 states the deviation).
                float sum = 0f;
                if (row.Segments != null) { foreach (float v in row.Segments) { sum += Mathf.Max(0f, v); } }
                sum = Mathf.Max(0.0001f, sum);
                float sx = axisX;
                for (int i = 0; row.Segments != null && i < row.Segments.Length; i++)
                {
                    float w = axisW * Mathf.Max(0f, row.Segments[i]) / sum;
                    int n = row.Segments.Length;
                    Color tint = n <= 3 ? (i == 0 ? PoliSimTheme.Tint(ink, 0.35f) : i == 1 ? PoliSimTheme.Tint(ink, 0.65f) : ink) : PoliSimTheme.Tint(ink, 0.3f + 0.7f * i / Mathf.Max(1, n - 1));
                    PoliSimTheme.Rule(new Rect(sx, cell.y + 4f * u, w, 5f * u), tint);
                    if (i > 0) { PoliSimTheme.Rule(new Rect(sx, cell.y + 3f * u, 1f, 7f * u), PoliSimTheme.Card); }
                    sx += w;
                }
                // The parts' own figures stay at rest: they are the row's only reading of the parts, and a figure under its own extent
                // is a label on something drawn - §3.7 lets a mark replace such a word, and there is no mark for a share.
                GUIStyle segment = DeskCaption(6.5f, PoliSimTheme.TextMuted, false, TextAnchor.MiddleCenter);
                float segH = Mathf.Ceil(DeskCaptionHeight(segment));
                float segY = cell.y + laneH;
                float lx = axisX;
                for (int i = 0; row.Segments != null && i < row.Segments.Length; i++)
                {
                    float w = axisW * Mathf.Max(0f, row.Segments[i]) / sum;
                    string figureText = row.Segments[i].ToString(row.High < 20f ? "0.0" : "0", CultureInfo.InvariantCulture);
                    if (segment.CalcSize(new GUIContent(figureText)).x + 2f <= w) { PoliSimWidgets.MeasuredLabel(new Rect(lx, segY, w, segH), figureText, segment); }
                    lx += w;
                }
                return;
            }

            float span = Mathf.Max(0.0001f, row.High - row.Low);
            float Px(float v) => axisX + Mathf.Clamp01((v - row.Low) / span) * axisW;

            PoliSimTheme.Rule(new Rect(axisX, axisY, axisW, 1f), PoliSimTheme.Hairline);
            if (row.Band == PlateBand.Bounded && row.Own >= 0f)
            {
                PoliSimTheme.Rule(new Rect(axisX, cell.y + 4f * u, Mathf.Max(0f, Px(row.Own) - axisX), 5f * u), PoliSimTheme.RuleLight);
            }
            if (row.Peers != null)
            {
                foreach (float p in row.Peers) { PoliSimTheme.Rule(new Rect(Px(p) - 0.75f, cell.y + 2f * u, 1.5f, 9f * u), PoliSimTheme.Peer); }
            }
            if (row.Own >= 0f && row.Peers != null && row.Peers.Length > 0)
            {
                float median = PlateMedian(row.Peers);
                float a = Mathf.Min(Px(row.Own), Px(median)), b = Mathf.Max(Px(row.Own), Px(median));
                bool better = row.LowerIsBetter ? row.Own < median : row.Own > median;
                PoliSimTheme.Rule(new Rect(a, cell.y + 11f * u, Mathf.Max(1f, b - a), 3f * u), better ? PoliSimTheme.Good : PoliSimTheme.TextMuted);
            }
            if (row.Own >= 0f)
            {
                PoliSimTheme.Rule(new Rect(Px(row.Own) - 1f, cell.y, 2f, cell.height), PoliSimTheme.TextPrimary);
            }
            // The direction triangle sits in the gutter at the BETTER end - the row's only direction mark.
            DrawPlateTriangle(row.LowerIsBetter ? new Rect(cell.x, axisY - 3f * u, gutter, 6f * u) : new Rect(cell.xMax - gutter, axisY - 3f * u, gutter, 6f * u), row.LowerIsBetter);
        }

        private static float PlateMedian(float[] values)
        {
            var sorted = new List<float>(values);
            sorted.Sort();
            int n = sorted.Count;
            return n == 0 ? 0f : (n % 2 == 1 ? sorted[n / 2] : (sorted[n / 2 - 1] + sorted[n / 2]) * 0.5f);
        }

        /// <summary>A 6 px triangle in TextMuted, pointing left when lower is better and right when higher is (§3.3).</summary>
        private static void DrawPlateTriangle(Rect r, bool pointsLeft)
        {
            int steps = Mathf.Max(3, Mathf.RoundToInt(r.height * 0.5f));
            for (int i = 0; i < steps; i++)
            {
                float t = (i + 1f) / steps;
                float h = r.height * t;
                float px = pointsLeft ? r.x + r.width * t : r.xMax - r.width * t;
                PoliSimTheme.Rule(new Rect(px - 0.5f, r.center.y - h * 0.5f, 1f, Mathf.Max(1f, h)), PoliSimTheme.TextMuted);
            }
        }

        /// <summary>5c's arrow in the band cell: the signed figure in direction-aware ink, an arrow rule whose length is the move against the row's
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
            if (DeskProvenance.On) { PoliSimWidgets.MeasuredLabel(new Rect(cell.x, cell.y + figH, cell.width, srcHeight), "NEXT YEAR · WITH vs WITHOUT THIS DRAFT", DeskCaption(6.5f, PoliSimTheme.TextMuted)); }
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
