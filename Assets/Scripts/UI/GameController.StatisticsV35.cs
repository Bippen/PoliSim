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
            var parts = new List<V35Part>();
            for (int i = 0; i < shares.Count; i++)
            {
                // Spaced, NOT Of: SectorType.Energy resolves through the curated policy table to "Energy (Spending)", a discretionary spending line.
                parts.Add(new V35Part(DisplayName.Spaced(shares[i].Type.ToString()), UiFormat.Number(shares[i].SharePercent, 1) + "%", UiPalette.GetCategoricalColor(i), PoliSimTheme.TextOnDesk, shares[i].SharePercent));
            }
            parts.Add(new V35Part("Other", UiFormat.Number(other, 1) + "%", PoliSimTheme.Neutral, PoliSimTheme.TextOnDesk, other, "sector:other"));

            // 23a ⑧: a segment's length is its share of GDP - the bar's whole is 100 % of it; the card's head is the tile's, drawn borderless inside it
            var head = new V35TileData { Icon = "sectors", IconInk = area, Figure = UiFormat.Number(100f - other, 1) + "%", Name = "Share of the economy" };
            float cardHeight = V35ShareCardHeight(contentWidth, head, parts, 100f);
            Rect card = GUILayoutUtility.GetRect(contentWidth, cardHeight, GUILayout.Width(contentWidth), GUILayout.Height(cardHeight));
            DrawV35ShareCard(card, head, parts, 100f, StatsAnchor);
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
        /// §729 (UI v3.5): STATISTICS › INTERNATIONAL as the composition lays it - the World trade card (the map, its chips the composition's, one form
        /// with the desk's - Elias's "one form per map"); the trade balance's card beside the credit rating's tile; PARTNERS AND PEERS - the share of the
        /// trade each partner takes and the six states' growth, both the model's own (the composition's figures were illustrative; these are read off
        /// the trade links and the six GDP histories). The PAIR page the composition does not draw is KEPT as built, after them, and asked.
        /// </summary>
        private void DrawInternationalStatisticsV35(float contentWidth)
        {
            V35.FloorGuarded = true;
            Color area = UiPalette.GetAreaColor(UiPalette.SystemArea.Global);

            // ---- World trade ----
            GUILayout.BeginVertical(V35CardStyle(), GUILayout.Width(contentWidth));
            float headSide = V35.Px(V35.CardIcon);
            Rect head = GUILayoutUtility.GetRect(10f, headSide, GUILayout.ExpandWidth(true));
            DrawV35CardHead(head, "globe", "World trade", area);
            StatsAnchor(new Rect(head.x, head.y, Mathf.Min(head.width, V35.Px(220f)), head.height), "map");
            GUILayout.Space(V35.Px(V35.CardHeadGap));
            Rect mapRect = GUILayoutUtility.GetRect(10f, WorldMapHeight, GUILayout.ExpandWidth(true));
            _mapRenderer.VisibleClip = _statsVisibleContent;   // D-ST: this map scrolls - its rotated lines clip to what the view shows
            _mapRenderer.OwnTooltip = false;   // §710: this sheet hangs its own slips on the chips - the name the first line
            _mapRenderer.V35Chips = true;      // §729: the composition's chip, the desk's form (one form per map)
            _mapRenderer.Draw(mapRect, _world.Countries, PlayerCountryId, _mapEventMarkers, _simulationManager.CurrentTurn, EventMarkerFadeTurns,
                V35Mono(V35.Floor, PoliSimTheme.TextPrimary, bold: true), out CountryId? clickedCountry, out MapEventMarker? clickedEvent);
            _mapRenderer.V35Chips = false;
            _mapRenderer.VisibleClip = null;
            _mapRenderer.OwnTooltip = true;
            foreach (KeyValuePair<CountryId, Rect> chip in _mapRenderer.LastChipRects) { StatsAnchor(chip.Value, "map:chip:" + chip.Key); }
            if (clickedCountry.HasValue) { _selectedMapCountry = clickedCountry; _selectedMapEvent = null; }
            else if (clickedEvent.HasValue) { _selectedMapEvent = clickedEvent; _selectedMapCountry = null; }
            if (_selectedMapEvent.HasValue) { GUILayout.Space(V35.Px(8f)); DrawSelectedMapEventPanel(_selectedMapEvent.Value); }
            else if (_selectedMapCountry.HasValue) { GUILayout.Space(V35.Px(8f)); DrawSelectedMapCountryPanel(_selectedMapCountry.Value); }
            GUILayout.EndVertical();
            StatsSectionGap();

            // ---- the trade balance's card beside the credit rating's tile ----
            StatHistory history = _playerCountry.History;
            DrawStatsPagedHead("Trade", "trade:head", "trade:pager", _statsTradeSection, GraphRenderer.PagesFor(history.TradeBalance.Quarterly), contentWidth);
            GUILayout.Space(V35.Px(6f));
            float gutter = V35.Px(V35.Gutter);
            float half = V35Span(contentWidth, 6);
            GUILayout.BeginHorizontal(GUILayout.Width(contentWidth));
            GUILayout.BeginVertical(V35CardStyle(), GUILayout.Width(half));
            DrawStatsChart(StatsSlips.Trade, history, null, StatsGraphLabelStyle(), null, null, null, null, _statsTradeSection);
            DrawTradePassThroughRow();
            GUILayout.EndVertical();
            GUILayout.Space(gutter);
            GUILayout.BeginVertical(GUILayout.Width(contentWidth - half - gutter));
            HeadlineReading rating = default;
            bool hasRating = false;
            foreach (HeadlineReading r in BuildHeadlineReadings()) { if (r.Label == "Credit Rating") { rating = r; hasRating = true; } }
            if (hasRating)
            {
                var tile = new V35TileData { Icon = "shield", IconInk = area, Figure = rating.Value, Name = "Credit rating", FigurePx = V35.FigureSmall };
                if (!string.IsNullOrEmpty(rating.Delta)) { tile.Change = rating.Delta; tile.ChangeInk = rating.DeltaInk; }
                float h = V35TileHeight(tile);
                Rect r = GUILayoutUtility.GetRect(contentWidth - half - gutter, h, GUILayout.ExpandWidth(true), GUILayout.Height(h));
                DrawV35Tile(r, tile);
            }
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
            StatsSectionGap();

            // ---- partners and peers ----
            DrawStatsV35Head("Partners and peers", "partners:head", contentWidth);
            GUILayout.Space(V35.Px(6f));
            var partners = new List<(string Name, string Code, float Value, bool Absent, bool Own)>();
            float tradeSum = 0f;
            foreach (TradePartner p in _playerCountry.TradePartners) { tradeSum += Mathf.Max(0f, p.ExportVolume) + Mathf.Max(0f, p.ImportVolume); }
            foreach (TradePartner p in _playerCountry.TradePartners)
            {
                Country c = _world.GetCountry(p.PartnerId);
                if (c == null) { continue; }
                float share = tradeSum > 0f ? 100f * (Mathf.Max(0f, p.ExportVolume) + Mathf.Max(0f, p.ImportVolume)) / tradeSum : 0f;
                partners.Add((c.Name, MapRenderer.TagOf(c.Id), share, false, false));
            }
            partners.Sort((a, b) => b.Value.CompareTo(a.Value));
            var peers = new List<(string Name, string Code, float Value, bool Absent, bool Own)>();
            foreach (Country c in _world.Countries)
            {
                float? g = StatsReadings.YearOnYearGrowthPercent(c.History?.Gdp.Quarterly);
                var row = (c.Name, MapRenderer.TagOf(c.Id), g ?? 0f, !g.HasValue, c.Id == PlayerCountryId);
                if (row.Item5) { peers.Insert(0, row); } else { peers.Add(row); }   // the home country first, as the composition sets it
            }
            float cardHeight = Mathf.Max(StatsBarCardHeight(partners.Count), StatsBarCardHeight(peers.Count));
            Rect band = GUILayoutUtility.GetRect(contentWidth, cardHeight, GUILayout.Width(contentWidth), GUILayout.Height(cardHeight));
            DrawStatsBarCard(new Rect(band.x, band.y, half, cardHeight), "trade", "Trade partners", partners, "% of trade", "partners:card");
            DrawStatsBarCard(new Rect(band.x + half + gutter, band.y, band.width - half - gutter, cardHeight), "chart", "GDP growth, six states", peers, "%", "peers:card");
            V35.FloorGuarded = false;

            // ---- KEPT (not in the composition; asked): the pair page, as built ----
            StatsSectionGap();
            DrawCountryPageContent();
        }

        /// <summary>The tariff pass-through row inside the trade card, at the floor: the last closed period's applied term, ABSENT before one closed.</summary>
        private void DrawTradePassThroughRow()
        {
            FiscalTurnReport last = _simulationManager.GetLastFiscalReport(PlayerCountryId);
            GUIStyle name = V35Serif(V35.Floor, PoliSimTheme.TextPrimary);
            GUIStyle figure = V35Mono(V35.Floor, PoliSimTheme.TextPrimary, bold: true, TextAnchor.MiddleRight);
            float rowHeight = V35.Px(V35.ListRow);
            GUILayout.Space(V35.Px(6f));
            Rect row = GUILayoutUtility.GetRect(10f, rowHeight, GUILayout.ExpandWidth(true));
            if (Event.current.type == EventType.Repaint)
            {
                PoliSimWidgets.MeasuredLabel(new Rect(row.x, row.y, row.width * 0.6f, row.height), "Tariff pass-through", name);
                PoliSimTheme.Rule(new Rect(row.x, row.y - 1f, row.width, 1f), V35.ListRule);
            }
            if (last != null)
            {
                float pp = last.TariffPassThroughPp;
                string text = (StatsReadings.IsFlat(pp, 2) ? 0f.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)
                    : StatsReadings.TrueMinus(pp.ToString("+0.00;-0.00", System.Globalization.CultureInfo.InvariantCulture))) + " pp";   // 23b ⑬: a zero has no sign
                float w = Mathf.Ceil(figure.CalcSize(new GUIContent(text)).x) + V35.Px(4f);
                var cell = new Rect(row.xMax - w, row.y, w, row.height);
                if (Event.current.type == EventType.Repaint) { PoliSimWidgets.MeasuredLabel(cell, text, figure); }
                StatsAnchor(cell, "trade:passthrough");
            }
            else
            {
                float side = Mathf.Min(rowHeight, V35.Px(16f));
                var slot = new Rect(row.xMax - side, row.y + (row.height - side) * 0.5f, side, side);
                if (Event.current.type == EventType.Repaint) { DrawStateGlyph(slot, Symbol.Absent, PoliSimTheme.TextMuted); }
                StatsAnchor(slot, "trade:passthrough");
            }
        }

        /// <summary>A bar card's height for <paramref name="rows"/> rows: the head, the rows, the axis line.</summary>
        private static float StatsBarCardHeight(int rows) =>
            V35.Px(V35.CardPadY) * 2f + V35.Px(V35.CardIcon) + V35.Px(V35.CardHeadGap) + rows * V35.Px(31f) + V35.Px(24f);

        /// <summary>
        /// §729: a card of rows on one axis (the composition's Trade partners and GDP growth cards) - each row the globe, NAME · CODE, the bar, the
        /// figure; the axis's two ends under them. The bars are the neutral slate (a share and a growth figure ranked side by side say no verdict);
        /// the player's own row takes the area's ink, as the composition marks it. A row with no figure yet draws ABSENT, never a zero; a negative
        /// figure draws its magnitude and prints its sign.
        /// </summary>
        private void DrawStatsBarCard(Rect card, string icon, string title, List<(string Name, string Code, float Value, bool Absent, bool Own)> rows, string unit, string anchor)
        {
            Rect inner = DrawV35Card(card);
            Color area = UiPalette.GetAreaColor(UiPalette.SystemArea.Global);
            Rect head = DrawV35CardHead(inner, icon, title, area);
            StatsAnchor(head, anchor);
            if (Event.current.type != EventType.Repaint) { return; }
            float max = 0f;
            bool anyNegative = false;
            foreach (var r in rows) { if (!r.Absent) { max = Mathf.Max(max, Mathf.Abs(r.Value)); anyNegative |= r.Value < 0f; } }
            float axisMax = max <= 0f ? 1f : max <= 5f ? Mathf.Ceil(max) : Mathf.Ceil(max / 10f) * 10f;
            // a negative figure is not a short positive one: where any row is below zero the axis diverges - zero at the centre, each bar toward its sign
            GUIStyle nameFace = V35Serif(V35.Name, PoliSimTheme.TextPrimary);
            GUIStyle valueFace = V35Mono(V35.Floor, PoliSimTheme.TextPrimary, bold: true, TextAnchor.MiddleRight);
            GUIStyle axisFace = V35Serif(V35.Floor, PoliSimTheme.TextMuted);
            float rowHeight = V35.Px(31f), iconSide = V35.Px(20f);
            float nameWidth = V35.Px(170f), valueWidth = V35.Px(64f);
            float barX = inner.x + iconSide + V35.Px(8f) + nameWidth;
            float barWidth = Mathf.Max(1f, inner.xMax - valueWidth - V35.Px(10f) - barX);
            float barHeight = V35.Px(13f);
            float y = head.yMax + V35.Px(V35.CardHeadGap);
            foreach (var r in rows)
            {
                DrawV35Icon(new Rect(inner.x, y + Mathf.Round((rowHeight - iconSide) * 0.5f), iconSide, iconSide), "globe", area);
                string label = r.Name + " · " + r.Code;
                PoliSimWidgets.MeasuredLabel(new Rect(inner.x + iconSide + V35.Px(8f), y, nameWidth, rowHeight), V35Fit(label, nameFace, nameWidth, out _), nameFace);
                if (r.Absent)
                {
                    float side = V35.Px(16f);
                    DrawStateGlyph(new Rect(inner.xMax - side, y + Mathf.Round((rowHeight - side) * 0.5f), side, side), Symbol.Absent, PoliSimTheme.TextMuted);
                }
                else
                {
                    var track = new Rect(barX, y + Mathf.Round((rowHeight - barHeight) * 0.5f), barWidth, barHeight);
                    PoliSimTheme.Rule(track, PoliSimTheme.BarTrack);
                    Color ink = r.Own ? area : PoliSimTheme.Neutral;
                    if (anyNegative)
                    {
                        float centre = Mathf.Round(track.x + track.width * 0.5f);
                        float w = track.width * 0.5f * Mathf.Clamp01(Mathf.Abs(r.Value) / axisMax);
                        PoliSimTheme.Rule(r.Value >= 0f ? new Rect(centre, track.y, w, track.height) : new Rect(centre - w, track.y, w, track.height), ink);
                        PoliSimTheme.Rule(new Rect(centre, track.y - 2f, 1f, track.height + 4f), PoliSimTheme.HairlineStrong);
                    }
                    else
                    {
                        PoliSimTheme.Rule(new Rect(track.x, track.y, track.width * Mathf.Clamp01(Mathf.Abs(r.Value) / axisMax), track.height), ink);
                    }
                    string figure = StatsReadings.TrueMinus(UiFormat.Number(r.Value, 1)) + "%";
                    PoliSimWidgets.MeasuredLabel(new Rect(inner.xMax - valueWidth, y, valueWidth, rowHeight), figure, valueFace);
                }
                y += rowHeight;
            }
            string zero = anyNegative ? StatsReadings.Minus + axisMax.ToString("0", System.Globalization.CultureInfo.InvariantCulture) : "0";
            PoliSimWidgets.MeasuredLabel(new Rect(barX, y, V35.Px(48f), V35.Px(24f)), zero, axisFace);
            if (anyNegative)
            {
                GUIStyle mid = V35Serif(V35.Floor, PoliSimTheme.TextMuted, TextAnchor.UpperCenter);
                PoliSimWidgets.MeasuredLabel(new Rect(barX + barWidth * 0.5f - V35.Px(20f), y, V35.Px(40f), V35.Px(24f)), "0", mid);
            }
            string end = axisMax.ToString("0", System.Globalization.CultureInfo.InvariantCulture) + unit;
            float endWidth = Mathf.Ceil(axisFace.CalcSize(new GUIContent(end)).x) + 2f;
            PoliSimWidgets.MeasuredLabel(new Rect(barX + barWidth - endWidth, y, endWidth, V35.Px(24f)), end, axisFace);
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
            DrawStatsV35Head("More readings", "more:head", contentWidth);
            GUILayout.Space(V35.Px(6f));
            DrawStatsTileRows(contentWidth, tiles, 4);
        }
    }
}
