using System.Collections.Generic;
using PoliSim.Data;
using PoliSim.Simulation;
using UnityEngine;

namespace PoliSim.UI
{
    /// <summary>
    /// §728 (UI v3.5, Design's V35 composition): STATISTICS › DOMESTIC AS THE COMPOSITION LAYS IT - Public finances (the four shares on one axis, each
    /// row with its icon) beside the GDP-per-person tile; Economy by sector (the share the eight cover, then the bar with the names inside the
    /// segments where they fit at the floor and the rest on one key line - V35 rule 7, never tooltip-only); the six live series as cards, each with
    /// a v3.5 tile head over its chart (<see cref="GraphRenderer.V35Head"/>); Society as eight tiles. Two sections the composition does not draw are
    /// KEPT in the same grammar and asked: the readings no other section carries (the debt stock, the balance, the rating, the currency, GDP at
    /// current prices) and YOUR POLICIES (the impact ledger). Every anchor keeps its id, so the slips the sheet hung on the board-2a parts hang on
    /// their v3.5 parts.
    /// </summary>
    public partial class GameController
    {
        /// <summary>§728: the inside width of the card the impact ledger's rows draw in - set before the card opens, so a wrapped line is measured at the
        /// width it will wrap at on the layout event too.</summary>
        private float _statsCardInnerWidth;

        /// <summary>A section head in the flow: the muted capitals at the floor (the composition's small caps), its slip anchor over the words, and
        /// <paramref name="reserveRight"/> kept for a control the caller draws at its right. Returns the row.</summary>
        private Rect DrawStatsV35Head(string text, string anchor, float width, float reserveRight = 0f)
        {
            float h = V35.Px(26f);
            Rect row = width > 0f ? GUILayoutUtility.GetRect(width, h, GUILayout.Width(width), GUILayout.Height(h)) : GUILayoutUtility.GetRect(10f, h, GUILayout.ExpandWidth(true), GUILayout.Height(h));
            DrawV35SectionHead(new Rect(row.x, row.y, Mathf.Max(1f, row.width - reserveRight), row.height), text);
            GUIStyle face = V35Serif(V35.Floor, PoliSimTheme.TextMuted);
            StatsAnchor(new Rect(row.x, row.y, Mathf.Min(Mathf.Max(1f, row.width - reserveRight), face.CalcSize(new GUIContent(text.ToUpperInvariant())).x + 4f), row.height), anchor);
            return row;
        }

        /// <summary>The page's title, its two tabs and the † control on one row (the composition's: the title, then the tabs' words, the active one
        /// underlined in the area's ink). Returns the tab clicked, or -1.</summary>
        private int DrawStatsV35Title(Rect row, int selected)
        {
            string[] tabs = { "Domestic", "International" };
            GUIStyle titleFace = new GUIStyle(_headerStyle) { fontSize = V35.FontPx(V35.PageTitle), alignment = TextAnchor.MiddleLeft, wordWrap = false };
            titleFace.padding = new RectOffset(0, 0, 0, 0);
            float titleWidth = Mathf.Ceil(titleFace.CalcSize(new GUIContent("Statistics")).x);
            StatsAnchor(new Rect(row.x, row.y, titleWidth, row.height), "title");
            DrawV35PageTitle(row, "Statistics");
            int clicked = -1;
            float x = row.x + titleWidth + V35.Px(32f);
            Color area = UiPalette.GetAreaColor(UiPalette.SystemArea.Global);
            for (int i = 0; i < tabs.Length; i++)
            {
                GUIStyle face = V35Serif(V35.Name, i == selected ? PoliSimTheme.TextPrimary : PoliSimTheme.TextSecondary, TextAnchor.MiddleLeft);
                float w = Mathf.Ceil(face.CalcSize(new GUIContent(tabs[i])).x);
                var tab = new Rect(x, row.y, w, row.height);
                if (Event.current.type == EventType.Repaint)
                {
                    PoliSimWidgets.MeasuredLabel(tab, tabs[i], face);
                    if (i == selected) { PoliSimTheme.Rule(new Rect(tab.x, tab.yMax - V35.Px(8f), tab.width, 2f), area); }
                }
                if (PoliSimWidgets.Button(tab, GUIContent.none, GUIStyle.none)) { clicked = i; }
                x += w + V35.Px(24f);
            }
            return clicked;
        }

        /// <summary>
        /// Public finances (board 2a's fiscal position in the composition's card): the tax burden, spending, the deficit and the primary balance as bars
        /// on ONE axis (the group's maximum rounded up to the next 10 %, never below 30 %), each with its icon and figure; a row no closed year has
        /// computed draws ABSENT, never a track at zero. The bars are the neutral slate (V35 rule 5: a share of GDP has no consensus direction); the
        /// deficit takes the warning only past the country's statutory rule (§725), its slip naming it. GDP per person is its own tile beside.
        /// </summary>
        private void DrawStatsPublicFinances(float contentWidth)
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
                if (v.HasValue && Mathf.Abs(v.Value) > axisMax) { axisMax = Mathf.Ceil(Mathf.Abs(v.Value) / 10f) * 10f; }
            }

            string deficitRule = null;
            Color deficitInk = deficit.HasValue ? V35.FiscalInk(PlayerCountryId, FiscalRules.Measure.Deficit, deficit.Value, PoliSimTheme.Neutral, out deficitRule) : PoliSimTheme.Neutral;
            var rows = new List<(string icon, string name, float? value, Color ink, string anchor)>
            {
                ("receipt", "Tax burden", tax, PoliSimTheme.Neutral, null),
                ("out", "Government spending", spending, PoliSimTheme.Neutral, null),
                ("scales", deficit.HasValue && deficit.Value < 0f ? "Surplus" : "Deficit", deficit.HasValue ? Mathf.Abs(deficit.Value) : (float?)null, deficitInk, deficitRule != null ? "fiscal:rule" : null),
                ("scales", primary.HasValue && primary.Value < 0f ? "Primary surplus" : "Primary deficit", primary.HasValue ? Mathf.Abs(primary.Value) : (float?)null, PoliSimTheme.Neutral, null),
            };

            DrawStatsV35Head("Public finances", "fiscal:head", contentWidth);
            GUILayout.Space(V35.Px(6f));
            float gutter = V35.Px(V35.Gutter);
            float left = V35Span(contentWidth, 9);
            float right = contentWidth - left - gutter;
            float rowHeight = V35.Px(31f);
            float axisHeight = V35.Px(24f);
            float cardHeight = V35.Px(V35.CardPadY) * 2f + rows.Count * rowHeight + axisHeight;
            Rect band = GUILayoutUtility.GetRect(contentWidth, cardHeight, GUILayout.Width(contentWidth), GUILayout.Height(cardHeight));

            Rect inner = DrawV35Card(new Rect(band.x, band.y, left, cardHeight));
            bool repaint = Event.current.type == EventType.Repaint;
            GUIStyle nameFace = V35Serif(V35.Name, PoliSimTheme.TextPrimary);
            GUIStyle valueFace = V35Mono(V35.Floor, PoliSimTheme.TextPrimary, bold: true, TextAnchor.MiddleRight);
            GUIStyle axisFace = V35Serif(V35.Floor, PoliSimTheme.TextMuted);
            float iconSide = V35.Px(22f);
            float nameWidth = V35.Px(190f);
            float valueWidth = V35.Px(72f);
            float barX = inner.x + iconSide + V35.Px(10f) + nameWidth;
            float barWidth = Mathf.Max(1f, inner.xMax - valueWidth - V35.Px(12f) - barX);
            float barHeight = V35.Px(13f);
            float y = inner.y;
            foreach (var row in rows)
            {
                DrawV35Icon(new Rect(inner.x, y + Mathf.Round((rowHeight - iconSide) * 0.5f), iconSide, iconSide), row.icon, UiPalette.GetAreaColor(UiPalette.SystemArea.Global));
                if (repaint) { PoliSimWidgets.MeasuredLabel(new Rect(inner.x + iconSide + V35.Px(10f), y, nameWidth, rowHeight), row.name, nameFace); }
                if (row.value.HasValue)
                {
                    var track = new Rect(barX, y + Mathf.Round((rowHeight - barHeight) * 0.5f), barWidth, barHeight);
                    var figure = new Rect(inner.xMax - valueWidth, y, valueWidth, rowHeight);
                    if (repaint)
                    {
                        PoliSimTheme.Rule(track, PoliSimTheme.BarTrack);
                        PoliSimTheme.Rule(new Rect(track.x, track.y, track.width * Mathf.Clamp01(row.value.Value / axisMax), track.height), row.ink);
                        PoliSimWidgets.MeasuredLabel(figure, UiFormat.Number(row.value.Value, 1) + "%", row.anchor != null ? Inked(new GUIStyle(valueFace), V35.Warning) : valueFace);
                    }
                    if (row.anchor != null) { StatsAnchor(new Rect(track.x, y, figure.xMax - track.x, rowHeight), row.anchor); }   // §725: the bar and its figure open the rule's slip
                }
                else
                {
                    // no closed year has computed it: ABSENT in the figure's slot (19a), the sentence on its slip
                    float side = Mathf.Min(rowHeight, V35.Px(16f));
                    var slot = new Rect(inner.xMax - side, y + (rowHeight - side) * 0.5f, side, side);
                    if (repaint) { DrawStateGlyph(slot, Symbol.Absent, PoliSimTheme.TextMuted); }
                    StatsAnchor(slot, "fiscal:notyet");
                }
                y += rowHeight;
            }
            // 23a ⑥: the axis - its two ends, the scale's end with its unit
            if (repaint) { PoliSimWidgets.MeasuredLabel(new Rect(barX, y, V35.Px(40f), axisHeight), "0", axisFace); }
            string end = axisMax.ToString("0", System.Globalization.CultureInfo.InvariantCulture) + "% of GDP";
            float endWidth = Mathf.Ceil(axisFace.CalcSize(new GUIContent(end)).x) + 2f;
            var endRect = new Rect(barX + barWidth - endWidth, y, endWidth, axisHeight);
            if (repaint) { PoliSimWidgets.MeasuredLabel(endRect, end, axisFace); }
            StatsAnchor(endRect, "fiscal:axis");

            var perPerson = new V35TileData
            {
                Icon = "person",
                IconInk = UiPalette.GetAreaColor(UiPalette.SystemArea.Global),
                Figure = perCapita.HasValue ? UiFormat.Money(perCapita.Value, MoneyUnit.Thousands) : null,
                Glyph = perCapita.HasValue ? (Symbol?)null : Symbol.Absent,
                Name = "GDP per person",
            };
            Rect reading = DrawV35Tile(new Rect(band.x + left + gutter, band.y, right, cardHeight), perPerson);
            StatsAnchor(reading, "fiscal:percapita");
        }

        /// <summary>
        /// Economy by sector (D-ST 23a ⑧'s bar, in the composition's card): the head says how much of GDP the eight cover; the bar sums to its whole -
        /// each segment its share of GDP, the rest OTHER in the neutral. V35 rule 7: a part's name and figure INSIDE its segment where they fit at the
        /// floor, then the name alone, else the part goes to ONE key line under the bar, in bar order - never tooltip-only.
        /// </summary>
        private void DrawStatsEconomyBySector(float contentWidth)
        {
            List<(SectorType Type, float SharePercent)> shares = DerivedStats.SectorSharesOfGdp(_playerCountry);
            DrawStatsV35Head("Economy by sector", "sector:head", contentWidth);
            GUILayout.Space(V35.Px(6f));
            Color area = UiPalette.GetAreaColor(UiPalette.SystemArea.Global);
            if (shares.Count == 0)
            {
                float h = V35.Px(V35.CardPadY) * 2f + V35.Px(V35.ListRow);
                Rect empty = GUILayoutUtility.GetRect(contentWidth, h, GUILayout.Width(contentWidth), GUILayout.Height(h));
                Rect emptyInside = DrawV35Card(empty);
                if (Event.current.type == EventType.Repaint) { PoliSimWidgets.MeasuredLabel(emptyInside, "Not tracked for this country", V35Serif(V35.Name, PoliSimTheme.TextMuted)); }
                return;
            }

            var percents = new List<float>(shares.Count);
            foreach ((SectorType _, float p) in shares) { percents.Add(p); }
            float other = StatsReadings.SectorRemainderPercent(percents);
            var entries = new List<(string Name, string Figure, Color Ink, float Part, bool Other)>();
            for (int i = 0; i < shares.Count; i++)
            {
                // Spaced, NOT Of: SectorType.Energy resolves through the curated policy table to "Energy (Spending)", a discretionary spending line.
                entries.Add((DisplayName.Spaced(shares[i].Type.ToString()), UiFormat.Number(shares[i].SharePercent, 1) + "%", UiPalette.GetCategoricalColor(i), Mathf.Max(0f, shares[i].SharePercent), false));
            }
            entries.Add(("Other", UiFormat.Number(other, 1) + "%", PoliSimTheme.Neutral, other, true));

            var head = new V35TileData { Icon = "sectors", IconInk = area, Figure = UiFormat.Number(100f - other, 1) + "%", Name = "Share of the economy" };
            float innerWidth = contentWidth - V35.Px(V35.CardPadX) * 2f;
            GUIStyle inside = V35Serif(V35.Floor, PoliSimTheme.TextOnDesk, TextAnchor.MiddleCenter);
            GUIStyle keyName = V35Serif(V35.Floor, PoliSimTheme.TextPrimary);
            GUIStyle keyFigure = V35Mono(V35.Floor, PoliSimTheme.TextSecondary);
            float pad = V35.Px(6f);
            // Each part's label: name and figure where they fit inside its segment, then the name alone, else the key line.
            var label = new string[entries.Count];
            var keyed = new List<int>();
            for (int i = 0; i < entries.Count; i++)
            {
                float w = innerWidth * Mathf.Clamp01(entries[i].Part / 100f);
                string both = entries[i].Name + " " + entries[i].Figure;
                if (inside.CalcSize(new GUIContent(both)).x + pad * 2f <= w) { label[i] = both; }
                else if (inside.CalcSize(new GUIContent(entries[i].Name)).x + pad * 2f <= w) { label[i] = entries[i].Name; }
                else { keyed.Add(i); }
            }
            float swatch = V35.Px(10f), entryGap = V35.Px(16f), keyPitch = V35.Px(22f);
            var widths = new float[entries.Count];
            foreach (int i in keyed) { widths[i] = swatch + V35.Px(5f) + keyName.CalcSize(new GUIContent(entries[i].Name)).x + V35.Px(4f) + keyFigure.CalcSize(new GUIContent(entries[i].Figure)).x; }
            int keyRows = keyed.Count == 0 ? 0 : 1;
            float run = 0f;
            foreach (int i in keyed)
            {
                if (run > 0f && run + entryGap + widths[i] > innerWidth) { keyRows++; run = 0f; }
                run += (run > 0f ? entryGap : 0f) + widths[i];
            }

            float headHeight = V35TileHeight(head) - V35.Px(V35.CardPadY) * 2f;
            float barHeight = V35.Px(26f);
            float cardHeight = V35.Px(V35.CardPadY) * 2f + headHeight + V35.Px(10f) + barHeight + (keyRows > 0 ? V35.Px(8f) + keyRows * keyPitch : 0f);
            Rect card = GUILayoutUtility.GetRect(contentWidth, cardHeight, GUILayout.Width(contentWidth), GUILayout.Height(cardHeight));
            Rect inner = DrawV35Card(card);
            // the head is the tile's, drawn borderless inside this card (the card is the section's)
            DrawStatsSectorHead(new Rect(inner.x, inner.y, inner.width, headHeight), head);
            if (Event.current.type != EventType.Repaint) { return; }

            var bar = new Rect(inner.x, inner.y + headHeight + V35.Px(10f), inner.width, barHeight);
            PoliSimTheme.Rule(bar, PoliSimTheme.BarTrack);
            float x = bar.x;
            for (int i = 0; i < entries.Count; i++)
            {
                float w = bar.width * Mathf.Clamp01(entries[i].Part / 100f);   // 23a ⑧: a segment's length is its share of GDP
                var segment = new Rect(x, bar.y, w, bar.height);
                PoliSimTheme.Rule(segment, entries[i].Ink);
                if (i > 0) { PoliSimTheme.Rule(new Rect(Mathf.Round(x), bar.y, 1f, bar.height), V35.CardPaper); }
                if (label[i] != null) { PoliSimWidgets.MeasuredLabel(segment, label[i], inside); }
                if (entries[i].Other) { StatsAnchor(segment, "sector:other"); }
                x += w;
            }
            float kx = inner.x, ky = bar.yMax + V35.Px(8f);
            foreach (int i in keyed)
            {
                if (kx > inner.x && kx + widths[i] > inner.xMax + 0.5f) { kx = inner.x; ky += keyPitch; }
                PoliSimTheme.Rule(new Rect(kx, ky + (keyPitch - swatch) * 0.5f, swatch, swatch), entries[i].Ink);
                float nx = kx + swatch + V35.Px(5f);
                float nw = Mathf.Ceil(keyName.CalcSize(new GUIContent(entries[i].Name)).x);
                PoliSimWidgets.MeasuredLabel(new Rect(nx, ky, nw, keyPitch), entries[i].Name, keyName);
                float fw = Mathf.Ceil(keyFigure.CalcSize(new GUIContent(entries[i].Figure)).x);
                PoliSimWidgets.MeasuredLabel(new Rect(nx + nw + V35.Px(4f), ky, fw, keyPitch), entries[i].Figure, keyFigure);
                if (entries[i].Other) { StatsAnchor(new Rect(kx, ky, widths[i], keyPitch), "sector:other"); }
                kx += widths[i] + entryGap;
            }
        }

        /// <summary>The sector card's head: the tile's icon, figure and name, without a card of its own.</summary>
        private void DrawStatsSectorHead(Rect r, V35TileData t)
        {
            float iconSide = V35.Px(V35.CardIcon);
            DrawV35Icon(new Rect(r.x, r.y + Mathf.Round((r.height - iconSide) * 0.5f), iconSide, iconSide), t.Icon, t.IconInk);
            if (Event.current.type != EventType.Repaint) { return; }
            GUIStyle figure = V35Mono(t.FigurePx, PoliSimTheme.TextPrimary, bold: true);
            GUIStyle name = V35Serif(V35.Name, PoliSimTheme.TextPrimary);
            float fh = Mathf.Ceil(figure.CalcSize(new GUIContent("0")).y), nh = Mathf.Ceil(name.CalcSize(new GUIContent("Ag")).y);
            float x = r.x + iconSide + V35.Px(12f), y = r.y + Mathf.Round((r.height - fh - 2f - nh) * 0.5f);
            GUI.Label(new Rect(x, y, r.xMax - x, fh), t.Figure, figure);
            PoliSimWidgets.MeasuredLabel(new Rect(x, y + fh + 2f, r.xMax - x, nh), t.Name, name);
        }

        /// <summary>The live series' icons, by chart id (the manifest's rows).</summary>
        private static string StatsChartIcon(string id)
        {
            switch (id)
            {
                case "gdp": return "chart";
                case "unemployment": return "jobs";
                case "inflation": return "infl";
                case "approval": return "check";
                case "poverty": return "bowl";
                case "debt": return "ratio";
                default: return "trade";
            }
        }

        /// <summary>The v3.5 head's faces for a chart of this sheet: the icon in the area's ink, the figure at 24, the change at 15, the name at 16,
        /// the axis at the floor.</summary>
        private GraphRenderer.V35HeadFaces StatsChartFaces(string id) => new GraphRenderer.V35HeadFaces
        {
            Icon = IconLibrary.V35(StatsChartIcon(id)),
            IconInk = UiPalette.GetAreaColor(UiPalette.SystemArea.Global),
            IconSide = V35.Px(V35.CardIcon),
            Figure = V35Mono(V35.Figure, PoliSimTheme.TextPrimary, bold: true),
            Delta = V35Mono(15f, PoliSimTheme.TextPrimary, bold: true),
            Name = V35Serif(V35.Name, PoliSimTheme.TextPrimary),
            Axis = V35Serif(V35.Floor, PoliSimTheme.TextMuted, TextAnchor.MiddleRight),
        };

        /// <summary>
        /// Society (board 2a's eight rows as the composition's eight tiles): a share draws its gauge under the head (youth unemployment, the Gini on its
        /// 0-100 scale, the housing two), a level or index with a kept history its sparkline at the head's right (an index's base drawn at 100, 23a
        /// ⑰); the figure carries its true unit; the USA's overburden is ABSENT by ruling - never a zero.
        /// </summary>
        private void DrawStatsSocietyTiles(float contentWidth)
        {
            EconomyState state = _playerCountry.State;
            StatHistory history = _playerCountry.History;
            Color area = UiPalette.GetAreaColor(UiPalette.SystemArea.Global);
            var tiles = new List<(V35TileData Tile, string Anchor, bool Index)>
            {
                (new V35TileData { Icon = "young", Figure = StatsReadings.Rate(state.YouthUnemployment), Name = "Youth unemployment", Fill = state.YouthUnemployment / 100f }, "Youth unemployment", false),
                (new V35TileData { Icon = "gini", Figure = UiFormat.Number(state.Gini, 1), Name = "Income inequality (Gini)", Fill = state.Gini / 100f }, "Income inequality (Gini)", false),
                (new V35TileData { Icon = "gear", Figure = UiFormat.Number(state.Productivity, 1) + " $/H", Name = "Productivity", Spark = history?.Productivity.Quarterly }, "Productivity", false),
                (new V35TileData { Icon = "home", Figure = StatsReadings.Rate(state.Homeownership), Name = "Homeownership", Fill = state.Homeownership / 100f }, "Homeownership", false),
                (new V35TileData { Icon = "heart", Figure = UiFormat.Number(state.LifeExpectancy, 1) + " Y", Name = "Life expectancy", Spark = history?.LifeExpectancy.Quarterly }, "Life expectancy", false),
                (new V35TileData { Icon = "coins", Figure = UiFormat.Number(state.RealWageIndex, 1), Name = "Real wages", Spark = history?.RealWageIndex.Quarterly, SparkReference = 100f }, "Real wages", true),
                (_playerCountry.TracksHousingOverburden
                    ? new V35TileData { Icon = "burden", Figure = StatsReadings.Rate(state.HousingOverburden), Name = "Housing overburden", Fill = state.HousingOverburden / 100f }
                    : new V35TileData { Icon = "burden", Glyph = Symbol.Absent, Name = "Housing overburden" }, "Housing overburden", false),
                (new V35TileData { Icon = "hprice", Figure = UiFormat.Number(state.HousePriceIndex, 1), Name = "House prices", Spark = history?.HousePriceIndex.Quarterly, SparkReference = 100f }, "House prices", true),
            };
            foreach (var t in tiles) { t.Tile.IconInk = area; t.Tile.FigurePx = V35.FigureSmall; t.Tile.SparkBelow = true; }

            DrawStatsV35Head("Society", "society:head", contentWidth);
            GUILayout.Space(V35.Px(6f));
            DrawStatsTileRows(contentWidth, tiles, 4);
        }

        /// <summary>Tiles in rows of <paramref name="perRow"/> across the twelve columns, each row as tall as its tallest; each tile's reading anchored
        /// "society:NAME" (the Society book's ids) - and an index's sparkline its base's slip.</summary>
        private void DrawStatsTileRows(float contentWidth, List<(V35TileData Tile, string Anchor, bool Index)> tiles, int perRow)
        {
            float gutter = V35.Px(V35.Gutter);
            float width = (contentWidth - gutter * (perRow - 1)) / perRow;
            for (int start = 0; start < tiles.Count; start += perRow)
            {
                float rowHeight = 0f;
                for (int i = start; i < Mathf.Min(tiles.Count, start + perRow); i++) { rowHeight = Mathf.Max(rowHeight, V35TileHeight(tiles[i].Tile)); }
                Rect row = GUILayoutUtility.GetRect(contentWidth, rowHeight, GUILayout.Width(contentWidth), GUILayout.Height(rowHeight));
                for (int i = start; i < Mathf.Min(tiles.Count, start + perRow); i++)
                {
                    var r = new Rect(row.x + (i - start) * (width + gutter), row.y, width, rowHeight);
                    Rect reading = DrawV35Tile(r, tiles[i].Tile);
                    if (tiles[i].Anchor != null) { StatsAnchor(reading, tiles[i].Anchor.StartsWith("card:") ? tiles[i].Anchor : "society:" + tiles[i].Anchor); }
                    if (tiles[i].Index)
                    {
                        // the index's sparkline - its base drawn at 100 - opens the base's slip: the band under the head, or the head's right
                        float padX = V35.Px(V35.CardPadX), band = V35.Px(24f);
                        StatsAnchor(tiles[i].Tile.SparkBelow
                            ? new Rect(r.x + padX, r.y + V35TileHeight(tiles[i].Tile) - V35.Px(V35.CardPadY) - band, r.width - padX * 2f, band)
                            : new Rect(r.xMax - padX - V35.Px(100f), r.y, V35.Px(100f), rowHeight), "society:index");
                    }
                }
                GUILayout.Space(gutter);
            }
        }

        /// <summary>
        /// KEPT (not in the composition; asked): the readings the composition's sections do not carry - GDP at current prices with its growth, the
        /// debt stock, the closed year's balance, the rating with its outlook, the currency - from the ten headline readings (ONE list with the
        /// desk's strip), as tiles. A reading with no figure yet prints its dash, as the strip does.
        /// </summary>
        private void DrawStatsMoreReadings(float contentWidth)
        {
            List<HeadlineReading> readings = BuildHeadlineReadings();
            var wanted = new (string Label, string Icon)[] { ("GDP", "chart"), ("Government Debt", "debt"), ("Budget Balance", "scales"), ("Credit Rating", "shield"), ("Currency Strength", "bank") };
            Color area = UiPalette.GetAreaColor(UiPalette.SystemArea.Global);
            var tiles = new List<(V35TileData Tile, string Anchor, bool Index)>();
            foreach ((string label, string icon) in wanted)
            {
                foreach (HeadlineReading r in readings)
                {
                    if (r.Label != label) { continue; }
                    var t = new V35TileData { Icon = icon, IconInk = area, Figure = r.Value, Name = label == "GDP" ? "GDP, current prices" : label.Substring(0, 1) + label.Substring(1).ToLowerInvariant(), FigurePx = V35.FigureSmall };
                    if (!string.IsNullOrEmpty(r.Delta)) { t.Change = r.Delta; t.ChangeInk = r.DeltaInk; }
                    tiles.Add((t, label == "GDP" ? "card:gdp/growth" : null, false));
                }
            }
            if (tiles.Count == 0) { return; }
            DrawStatsV35Head("More readings", "readings:head", contentWidth);
            GUILayout.Space(V35.Px(6f));
            DrawStatsTileRows(contentWidth, tiles, 4);
        }
    }
}
