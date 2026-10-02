using System;
using System.Collections.Generic;
using System.Globalization;
using PoliSim.Data;
using PoliSim.Data.Generated;
using PoliSim.Simulation;
using UnityEngine;

namespace PoliSim.UI
{
    /// <summary>
    /// §747 (UI v3.5, Design's V35 composition): THE ENERGY PAGE'S FRAME AND ITS OVERVIEW. The title *Energy* with its two tabs as words - *Overview*
    /// and *Policy* - and the †. The Overview as the composition lays it: <b>Prices</b> - the households' and the businesses' price each with the
    /// stack the ledger writes as one bar, industry's bill on its track, the wholesale price with the other five's clearings as ticks; <b>Power plants</b>
    /// - each technology's fleet as a tile with its order (− retires, + builds, one step a click, the connection queue's refusal on the slip), the
    /// capital cost BILLED; <b>Grid and target</b> - the operator's connection queue line by line, and the country's mandate. Under them, kept as built
    /// (asked): the plates that say why the price is what it is (the rule, the fleet and its zones, the water and the two ledgers). The Policy tab
    /// carries the four instrument dials and what else reaches the layer, as built, until its own item. The page is its own tab with its own rail cell
    /// (P6-F1, §536: Elias played it under Sectors and overruled DS-4b's *"Rail cell: NO"*).
    /// </summary>
    public partial class GameController
    {
        private static readonly string[] EnergyTabs = { "Overview", "Policy" };

        /// <summary>§747: which of the kept plates `DrawEnergyPlate` draws - why the price is what it is (the Overview), or what else reaches the layer (the Policy tab).</summary>
        private enum EnergyPlatePart { Why, Reaches }

        /// <summary>§747: the Energy page's tab - 0 the Overview, 1 the Policy (UI state, not saved).</summary>
        private int _energyTab;

        /// <summary>§747: the Energy page's slips, built each frame as the page draws.</summary>
        private PeopleSlips.Book _energySlipBook = new PeopleSlips.Book();

        private void DrawEnergyTab(float availableHeight, float availableWidth)
        {
            GUILayout.BeginVertical(_frameSheetStyle, GUILayout.Width(availableWidth), GUILayout.ExpandHeight(true));
            BeginSlipAnchors();
            _energySlipBook = new PeopleSlips.Book();
            float titleHeight = V35.Px(44f);
            Rect titleRow = GUILayoutUtility.GetRect(10f, titleHeight, GUILayout.ExpandWidth(true), GUILayout.Height(titleHeight));
            int clicked = DrawV35TitleTabs(titleRow, "Energy", EnergyTabs, _energyTab, UiPalette.GetAreaColor(UiPalette.SystemArea.Energy));
            if (clicked >= 0 && clicked != _energyTab) { _energyTab = clicked; _energyScrollPosition = Vector2.zero; }
            GUILayout.Space(V35.Px(4f));
            float bodyHeight = Mathf.Max(0f, availableHeight - titleHeight - V35.Px(4f));
            float contentWidth = StatsContentWidth(availableWidth);
            int scrolledFrom = _slipAnchors.Count;
            _energyScrollPosition = GUILayout.BeginScrollView(_energyScrollPosition, GUILayout.Height(Mathf.Max(0f, bodyHeight - _labelStyle.fontSize * 2f)));
            using (EnergyFleet.For(_playerCountry))   // §544: every fleet figure on the page is THIS country's - the record plus its own landed orders
            {
                if (_energyTab == 0) { DrawEnergyOverviewV35(contentWidth); }
                else
                {
                    // the Policy tab, as built until its own item: the four instruments as dials with their one bill, then what else reaches the layer
                    if (_playerCountry != null && EnergyLayer.Has(_playerCountry.Id))
                    {
                        EnsureEnergyCache(_playerCountry);
                        DrawEnergyInstrumentDials(_playerCountry);
                        DrawEnergyPlate(EnergyPlatePart.Reaches);
                    }
                }
            }
            GUILayout.EndScrollView();
            MoveScrolledAnchors(scrolledFrom, GUILayoutUtility.GetLastRect(), _energyScrollPosition);
            GUILayout.EndVertical();
            if (!DeskProvenance.On) { DrawSlips(_energySlipBook, GUILayoutUtility.GetLastRect()); }
        }

        /// <summary>§747: a v3.5 section head with its slip anchor on the Energy page's book, and an optional note at its right.</summary>
        private Rect DrawEnergySectionHead(string title, string anchor, float width, string right = null)
        {
            float h = V35.Px(26f);
            Rect row = GUILayoutUtility.GetRect(width, h, GUILayout.Width(width), GUILayout.Height(h));
            DrawV35SectionHead(row, title);
            if (!string.IsNullOrEmpty(right) && Event.current.type == EventType.Repaint)
            {
                PoliSimWidgets.MeasuredLabel(new Rect(row.x + row.width * 0.5f, row.y, row.width * 0.5f, row.height), right, V35Serif(V35.Floor, PoliSimTheme.TextSecondary, TextAnchor.MiddleRight));
            }
            GUIStyle face = V35Serif(V35.Floor, PoliSimTheme.TextMuted);
            SlipAnchor(new Rect(row.x, row.y, Mathf.Min(row.width * 0.5f, face.CalcSize(new GUIContent(title.ToUpperInvariant())).x + 4f), row.height), anchor);
            GUILayout.Space(V35.Px(6f));
            return row;
        }

        private void DrawEnergyOverviewV35(float width)
        {
            Country c = _playerCountry;
            if (c == null) { return; }
            if (!EnergyLayer.Has(c.Id))
            {
                GUILayout.Label("This country carries no energy layer - the six do, and this is not one of them.", _labelStyle);
                return;
            }
            EnsureEnergyCache(c);
            EnergyMarket.Result r = _energyResult;
            EnergyLedger.Book book = _energyBook;
            EconomyState s = c.State;
            StatHistory history = c.History;
            Color area = UiPalette.GetAreaColor(UiPalette.SystemArea.Energy);
            float gutter = V35.Px(V35.Gutter), half = V35Span(width, 6);
            bool repaint = Event.current.type == EventType.Repaint;
            V35.FloorGuarded = true;

            // ---- prices ----
            Rect pricesHead = DrawEnergySectionHead("Prices", "en:prices", width);
            _energySlipBook.Anchors["en:prices"] = new SlipContent("PRICES")
                .Add("THE TWO CLASSES' PRICES AS THE LEDGER WRITES THEM, IN " + EnergyLedger.BookCurrency + " CENTS PER kWh - EACH ITS STACK: WHOLESALE, MARGIN, NETWORK, LEVIES, ENVIRONMENTAL TAX, VAT")
                .Add("THE WHOLESALE PRICE CLEARS IN THE MARKET'S OWN CURRENCY PER MWh; THE OTHER FIVE'S CLEARINGS THIS TURN ARE ITS TICKS")
                .Add("A PRICE'S CHANGE IS IN THE NEUTRAL INK: A PRICE IS NOT A GOOD OR A BAD BY ITSELF");
            EnergyLedger.ClassStack hh = book.Classes[EnergyLedger.Households];
            EnergyLedger.ClassStack nh = book.Classes[EnergyLedger.NonHouseholds];
            float stackScale = Mathf.Max(0.0001f, Mathf.Max((float)hh.Total, (float)nh.PreVat) * 100f);
            var homes = new[] { ("Wholesale", (float)hh.Wholesale), ("Margin", (float)hh.Margin), ("Network", (float)hh.Network), ("Levies", (float)hh.Policy), ("Env. tax", (float)hh.TaxEnv), ("VAT", (float)hh.Vat) };
            var firms = new[] { ("Wholesale", (float)nh.Wholesale), ("Margin", (float)nh.Margin), ("Network", (float)nh.Network), ("Levies", (float)nh.Policy), ("Env. tax", (float)nh.TaxEnv) };
            float homesH = EnergyPriceCardHeight(homes, half, stackScale, area);
            float firmsH = EnergyPriceCardHeight(firms, half, stackScale, area);
            float priceRowH = Mathf.Max(homesH, firmsH);
            Rect priceRow = GUILayoutUtility.GetRect(width, priceRowH, GUILayout.Width(width), GUILayout.Height(priceRowH));
            DrawEnergyPriceCard(new Rect(priceRow.x, priceRow.y, half, priceRowH), "home", "Homes", s.EnergyHouseholdPrice, history?.EnergyHouseholdPrice.Quarterly, homes, stackScale, area, "en:homes",
                "THE HOUSEHOLDS' PRICE · " + PlateFigure(s.EnergyHouseholdPrice, 2) + " " + EnergyLedger.BookCurrency + " PER kWh, VAT INCLUDED · EUROSTAT nrg_pc_204 · EIA");
            DrawEnergyPriceCard(new Rect(priceRow.x + half + gutter, priceRow.y, priceRow.xMax - (priceRow.x + half + gutter), priceRowH), "office", "Businesses", s.EnergyIndustryPrice, history?.EnergyIndustryPrice.Quarterly, firms, stackScale, area, "en:firms",
                "THE NON-HOUSEHOLDS' PRICE · " + PlateFigure(s.EnergyIndustryPrice, 2) + " " + EnergyLedger.BookCurrency + " PER kWh, EXCLUDING RECOVERABLE VAT · EUROSTAT nrg_pc_205 · EIA");
            GUILayout.Space(gutter);

            // industry's bill on its track, the wholesale with the others' ticks
            var billTile = new V35TileData { Icon = "factory", IconInk = area, Figure = PlateFigure(s.EnergyIndustryBillGdpShare, 2, " %"), Name = "Industry's power bill", FigurePx = V35.FigureSmall, Spark = history?.EnergyIndustryBillGdpShare.Quarterly };
            EnergyNeutralChange(billTile, history?.EnergyIndustryBillGdpShare.Quarterly, 2, " pts");
            double wholesale = EnergyLedger.LoadWeightedPricePerMwh(r);
            string marketUnit = (c.Id == CountryId.USA ? "USD" : "EUR") + "/MWh";
            var wholesaleTile = new V35TileData { Icon = "pylon", IconInk = area, Figure = PlateFigure((float)wholesale, 1) + " " + marketUnit, Name = "Wholesale", FigurePx = V35.FigureSmall };
            float trackBand = V35.Px(44f);
            float tileH = Mathf.Max(V35TileHeight(billTile), V35TileHeight(wholesaleTile)) + trackBand;
            Rect row2 = GUILayoutUtility.GetRect(width, tileH, GUILayout.Width(width), GUILayout.Height(tileH));
            var billRect = new Rect(row2.x, row2.y, half, tileH);
            var wholesaleRect = new Rect(row2.x + half + gutter, row2.y, row2.xMax - (row2.x + half + gutter), tileH);
            DrawV35Tile(billRect, billTile);
            DrawV35Tile(wholesaleRect, wholesaleTile);
            float pad = V35.Px(V35.CardPadX);
            if (repaint)
            {
                // the bill's track, 0-4 % of GDP, its tick at the figure
                var track = new Rect(billRect.x + pad, billRect.yMax - trackBand + V35.Px(8f), billRect.width - pad * 2f, 1f);
                PoliSimTheme.Rule(track, V35.PyramidThreshold);
                float bx = track.x + track.width * Mathf.Clamp01(s.EnergyIndustryBillGdpShare / 4f);
                PoliSimTheme.Rule(new Rect(Mathf.Round(bx) - 1f, track.y - V35.Px(7f), 2f, V35.Px(14f)), PoliSimTheme.TextPrimary);
                GUIStyle ends = V35Serif(V35.Floor, PoliSimTheme.TextSecondary);
                GUIStyle endsRight = V35Serif(V35.Floor, PoliSimTheme.TextSecondary, TextAnchor.MiddleRight);
                GUI.Label(new Rect(track.x, track.y + V35.Px(8f), track.width * 0.5f, V35.Px(20f)), "0 %", ends);
                GUI.Label(new Rect(track.x + track.width * 0.5f, track.y + V35.Px(8f), track.width * 0.5f, V35.Px(20f)), "4 % of GDP", endsRight);

                // the wholesale's track, 0-250 per MWh, the others' ticks with their tags, its own heavier
                var wt = new Rect(wholesaleRect.x + pad, wholesaleRect.yMax - trackBand + V35.Px(8f), wholesaleRect.width - pad * 2f, 1f);
                PoliSimTheme.Rule(wt, V35.PyramidThreshold);
                GUIStyle tag = V35Mono(V35.Floor, PoliSimTheme.TextSecondary, bold: true, TextAnchor.UpperCenter);
                // the others' tags: under the track, or over it where one would print on another's (the first film's PL, FR and DE ran together); where both
                // rows are taken the tag is the slip's alone
                var tagged = new List<Rect>();
                foreach (CountryId id in PeerOrder)
                {
                    if (!_energyPeerWholesale.TryGetValue(id, out double w)) { continue; }
                    bool own = id == c.Id;
                    float x = wt.x + wt.width * Mathf.Clamp01((float)(w / 250.0));
                    PoliSimTheme.Rule(new Rect(Mathf.Round(x) - (own ? 1f : 0f), wt.y - V35.Px(own ? 8f : 5f), own ? 2f : 1f, V35.Px(own ? 16f : 10f)), own ? PoliSimTheme.TextPrimary : PoliSimTheme.TextMuted);
                    if (own) { continue; }
                    float tw = Mathf.Ceil(tag.CalcSize(new GUIContent(MapRenderer.TagOf(id))).x) + V35.Px(4f);
                    var below = new Rect(x - tw * 0.5f, wt.y + V35.Px(6f), tw, V35.Px(18f));
                    var above = new Rect(x - tw * 0.5f, wt.y - V35.Px(24f), tw, V35.Px(18f));
                    Rect? at = !tagged.Exists(t => t.Overlaps(below)) ? below : !tagged.Exists(t => t.Overlaps(above)) ? above : (Rect?)null;
                    if (at.HasValue) { tagged.Add(at.Value); GUI.Label(at.Value, MapRenderer.TagOf(id), tag); }
                }
                GUI.Label(new Rect(wt.x, wt.y + V35.Px(20f), wt.width * 0.5f, V35.Px(18f)), "0", ends);
                GUI.Label(new Rect(wt.x + wt.width * 0.5f, wt.y + V35.Px(20f), wt.width * 0.5f, V35.Px(18f)), "250 " + marketUnit, endsRight);
            }
            SlipAnchor(billRect, "en:bill"); SlipAnchor(wholesaleRect, "en:wholesale");
            _energySlipBook.Anchors["en:bill"] = new SlipContent("INDUSTRY'S POWER BILL · " + billTile.Figure + " OF GDP")
                .Add("NON-HOUSEHOLDS' CONSUMPTION × THEIR PRICE, AS A SHARE OF GDP · THE BOOK, THIS YEAR")
                .Add("IT REACHES BUSINESS CONFIDENCE AND THE PRICE LEVEL");
            var wholesaleSlip = new SlipContent("WHOLESALE · " + wholesaleTile.Figure).Add("LOAD-WEIGHTED OVER THE BLOCKS · THE ETS PRICE, NOT THE CARBON TAX · ENTSO-E · EIA · " + EnergyLayer.Year);
            foreach (CountryId id in PeerOrder) { if (_energyPeerWholesale.TryGetValue(id, out double w) && id != c.Id) { wholesaleSlip.Add(MapRenderer.TagOf(id) + " " + PlateFigure((float)w, 1) + " - ITS OWN CLEARING THIS TURN, IN ITS MARKET'S CURRENCY"); } }
            wholesaleSlip.Add("WHY IT IS WHAT IT IS - THE RULE, THE FLEET AND ITS ZONES - IS BELOW THE PLANTS");
            _energySlipBook.Anchors["en:wholesale"] = wholesaleSlip;
            if (repaint) { _energyPlateLastArea = new Rect(pricesHead.x, pricesHead.y, width, row2.yMax - pricesHead.y); }
            GUILayout.Space(gutter);

            // ---- power plants ----
            int year = c.CalendarYear;
            double step = EnergyFleet.StepMw(c.Id);
            int pending = EnergyFleet.PendingCount(c);
            Rect plantsHead = DrawEnergySectionHead("Power plants", "en:plants", width, pending == 0 ? "nothing queued" : pending == 1 ? "1 order" : pending + " orders");
            bool fleetLocked = !_simulationManager.PlayerMayIntroduce(c.Id, out string fleetLockedWhy);   // PS-3c (§630): a fleet order is the government's
            _energySlipBook.Anchors["en:plants"] = new SlipContent("POWER PLANTS")
                .Add("EACH TECHNOLOGY'S FLEET · − RETIRES, + BUILDS, ONE STEP OF " + EnergyConnectionQueue.Mw(step) + " MW A CLICK, LANDING AFTER THE LEAD TIME")
                .Add("A STEP THE CONNECTION QUEUE HAS NO ROOM FOR IS REFUSED, AND ITS TILE SAYS WHY · HYDRO AND OTHER ARE NOT ORDERABLE")
                .Add(fleetLocked ? fleetLockedWhy : "THE ORDERS ARE YOURS · ON LANDING AN ORDER MOVES THE FLEET THE PRICE READS");
            float plantW = V35Span(width, 3);
            // the head as the tile head lays it out - the figure over the name (the first film's buttons sat on the name: the head had been sized at the icon)
            float plantHead = Mathf.Max(V35.Px(V35.ListIcon), Mathf.Ceil(V35Mono(V35.FigureSmall, PoliSimTheme.TextPrimary, bold: true).CalcSize(new GUIContent("0")).y) + 2f
                + Mathf.Ceil(V35Serif(V35.Name, PoliSimTheme.TextPrimary).CalcSize(new GUIContent("Ag")).y));
            float plantH = V35.Px(V35.CardPadY) * 2f + plantHead + V35.Px(8f) + V35.Px(24f);
            int labels = EnergyLayerData.Labels.Length;
            Rect plantRow = default;
            for (int k = 0; k <= labels; k++)
            {
                if (k % 4 == 0) { plantRow = GUILayoutUtility.GetRect(width, plantH, GUILayout.Width(width), GUILayout.Height(plantH)); }
                var tile = new Rect(plantRow.x + (k % 4) * (plantW + gutter), plantRow.y, plantW, plantH);
                if (k < labels) { DrawEnergyPlantTile(tile, c, k, year, step, fleetLocked, fleetLockedWhy, area); }
                else
                {
                    // the capital cost: BILLED, and says so
                    var bill = new V35TileData { Icon = "coins", IconInk = PoliSimTheme.TextMuted, Glyph = Symbol.Billed, Name = "Build costs", FigurePx = V35.FigureSmall };
                    DrawV35Tile(tile, bill);
                    SlipAnchor(tile, "en:capex");
                    _energySlipBook.Anchors["en:capex"] = new SlipContent("BUILD COSTS · BILLED").Add(EnergyFleet.CapexBill);
                }
                if (k % 4 == 3 || k == labels) { GUILayout.Space(gutter); }
            }
            if (repaint) { _energyDecisionsLastArea = new Rect(plantsHead.x, plantsHead.y, width, plantRow.yMax - plantsHead.y); }

            // ---- grid and target ----
            DrawEnergySectionHead("Grid and target", "en:grid", width);
            _energySlipBook.Anchors["en:grid"] = new SlipContent("GRID AND TARGET")
                .Add(EnergyConnectionQueue.SourceText(c).ToUpperInvariant())
                .Add("THE MANDATE IS THE COUNTRY'S OWN STATUTE, READ FOR YOU - AN AI STATE'S MINISTRY ANSWERS ITS OWN WITH ORDERS LIKE THESE, HELD UNTIL ITS FAMILY IS RULED");
            EnergyConnectionQueue.Published published = EnergyConnectionQueue.Of(c.Id);
            float lineH = V35.Px(V35.ListRow + 2f);
            int lines = published != null ? published.Lines.Length : 1;
            float gridH = V35.Px(V35.CardPadY) * 2f + V35.Px(V35.CardIcon) + V35.Px(8f) + lines * lineH + V35.Px(20f);
            Rect gridRow = GUILayoutUtility.GetRect(width, gridH, GUILayout.Width(width), GUILayout.Height(gridH));
            float gridW = V35Span(width, 8);
            var gridCard = new Rect(gridRow.x, gridRow.y, gridW, gridH);
            Rect gridInner = DrawV35Card(gridCard);
            Rect gridHead = DrawV35CardHead(gridInner, "queue", "Grid queue", area);
            SlipAnchor(gridHead, "en:queue");
            var queueSlip = new SlipContent("GRID QUEUE").Add(EnergyConnectionQueue.CapacityText(c, year).ToUpperInvariant());
            bool anyOrder = false;
            foreach (EnergyFleet.Order o in EnergyFleet.Queue(c))
            {
                anyOrder = true;
                queueSlip.Add(EnergyLayerData.Labels[o.Technology].ToUpperInvariant() + " " + (o.Mw > 0 ? "+" : "−") + EnergyConnectionQueue.Mw(Math.Abs(o.Mw)) + " MW · PLACED " + o.OrderedYear + " · "
                    + (o.Landed ? (o.Mw > 0 ? "CONNECTED " : "LEFT ") : (o.Mw > 0 ? "CONNECTS " : "LEAVES ")) + o.OnlineYear);
            }
            if (!anyOrder) { queueSlip.Add("NO ORDER · THE FLEET IS THE SEED'S UNTIL AN ORDER LANDS"); }
            _energySlipBook.Anchors["en:queue"] = queueSlip;
            if (repaint)
            {
                GUIStyle nameFace = V35Serif(V35.Floor, PoliSimTheme.TextPrimary);
                GUIStyle figFace = V35Mono(V35.Floor, PoliSimTheme.TextPrimary, bold: true, TextAnchor.MiddleRight);
                float ly = gridHead.yMax + V35.Px(8f);
                if (published == null)
                {
                    float side = V35.Px(18f);
                    DrawStateGlyph(new Rect(gridInner.x, ly + Mathf.Round((lineH - side) * 0.5f), side, side), Symbol.Billed, PoliSimTheme.TextMuted);
                    PoliSimWidgets.MeasuredLabel(new Rect(gridInner.x + side + V35.Px(8f), ly, gridInner.width - side - V35.Px(8f), lineH), "No connection queue is published for this country", V35Serif(V35.Floor, PoliSimTheme.TextMuted));
                }
                else
                {
                    float nameW = V35.Px(150f), figW = V35.Px(150f);
                    float barX = gridInner.x + nameW, barW = Mathf.Max(10f, gridInner.width - nameW - figW - V35.Px(10f));
                    foreach (EnergyConnectionQueue.Line line in published.Lines)
                    {
                        double taken = EnergyConnectionQueue.TakenMw(c, line, year);
                        PoliSimWidgets.MeasuredLabel(new Rect(gridInner.x, ly, nameW - V35.Px(6f), lineH), V35Fit(CapitalWord(line.Name), nameFace, nameW - V35.Px(6f), out _), nameFace);
                        var bar = new Rect(barX, ly + Mathf.Round((lineH - V35.Px(10f)) * 0.5f), barW, V35.Px(10f));
                        PoliSimTheme.Rule(bar, PoliSimTheme.BarTrack);
                        PoliSimTheme.Rule(new Rect(bar.x, bar.y, bar.width * Mathf.Clamp01((float)(taken / Math.Max(1.0, line.CapMwIn(year)))), bar.height), area);
                        GUI.Label(new Rect(gridInner.xMax - figW, ly, figW, lineH), EnergyConnectionQueue.Mw(taken) + " / " + EnergyConnectionQueue.Mw(line.CapMwIn(year)) + " MW", figFace);
                        ly += lineH;
                    }
                    GUIStyle axis = V35Serif(V35.Floor, PoliSimTheme.TextMuted);
                    GUI.Label(new Rect(barX, ly, barW * 0.5f, V35.Px(18f)), "0", axis);
                    GUI.Label(new Rect(barX + barW * 0.5f, ly, barW * 0.5f, V35.Px(18f)), "full", V35Serif(V35.Floor, PoliSimTheme.TextMuted, TextAnchor.MiddleRight));
                }
            }

            // the mandate
            var targetRect = new Rect(gridRow.x + gridW + gutter, gridRow.y, gridRow.xMax - (gridRow.x + gridW + gutter), gridH);
            AiEnergyMinistry.Mandate mandate = AiEnergyMinistry.MandateOf(c.Id);
            var target = new V35TileData { Icon = "flag", IconInk = area, FigurePx = V35.FigureSmall };
            var targetSlip = new SlipContent("THE MANDATE");
            if (mandate == null) { target.Glyph = Symbol.Absent; target.Name = "No energy mandate"; targetSlip.Add("THE COUNTRY HOLDS NO STATUTORY ENERGY TARGET THE GAME CARRIES"); }
            else
            {
                target.Name = V35Fit(CapitalWord(mandate.Headline), V35Serif(V35.Name, PoliSimTheme.TextPrimary), targetRect.width - V35.Px(V35.CardPadX) * 2f - V35.Px(V35.CardIcon) - V35.Px(12f), out _);
                switch (mandate.Form)
                {
                    case AiEnergyMinistry.MandateForm.FossilFree:
                        target.Figure = EnergyConnectionQueue.Mw(EnergyLayer.CapacityMw(c.Id, 0) + EnergyFleet.QueuedMw(c, 0) + EnergyLayer.CapacityMw(c.Id, 1) + EnergyFleet.QueuedMw(c, 1)) + " MW";
                        targetSlip.Add("THE FOSSIL FLEET WITH THE QUEUE - COAL AND GAS - AGAINST A TARGET OF NONE");
                        break;
                    case AiEnergyMinistry.MandateForm.CapacityPath:
                        target.Figure = PlateFigure((float)((EnergyLayer.CapacityMw(c.Id, 4) + EnergyFleet.QueuedMw(c, 4) + EnergyLayer.CapacityMw(c.Id, 5) + EnergyFleet.QueuedMw(c, 5)) / 1000.0), 1) + " GW";
                        targetSlip.Add("WIND AND SOLAR WITH THE QUEUE, AGAINST THE PATH THE STATUTE SETS");
                        break;
                    default:
                        target.Figure = PlateFigure((float)AiEnergyMinistry.RenewableSharePercent(c), 1) + " %";
                        targetSlip.Add("THE RENEWABLE SHARE WITH THE QUEUE · THE RECORD " + PlateFigure((float)AiEnergyMinistry.RecordRenewableSharePercent(c.Id), 1) + " %");
                        break;
                }
                targetSlip.Add(mandate.Statute.ToUpperInvariant()).Add(mandate.Headline.ToUpperInvariant());
            }
            DrawV35Tile(targetRect, target);
            SlipAnchor(targetRect, "en:target");
            _energySlipBook.Anchors["en:target"] = targetSlip;
            GUILayout.Space(gutter);
            V35.FloorGuarded = false;

            // ---- kept as built (not in the composition; asked): why the price is what it is ----
            DrawEnergySectionHead("Why the price is what it is", "en:why", width);
            _energySlipBook.Anchors["en:why"] = new SlipContent("WHY THE PRICE IS WHAT IT IS")
                .Add("THE RULE ON THE PRICE, THE FLEET AND ITS ZONES, THE WATER AND THE TWO LEDGERS - KEPT AS BUILT UNDER THE NEW PAGE");
            DrawEnergyPlate(EnergyPlatePart.Why);
        }

        /// <summary>A change over four quarters on a tile, in the neutral ink - a price or a share of GDP has no direction most agree on.</summary>
        private static void EnergyNeutralChange(V35TileData tile, IReadOnlyList<float> series, int decimals, string unit)
        {
            if (series == null || series.Count < 5) { return; }
            float d = series[series.Count - 1] - series[series.Count - 5];
            if (Mathf.Abs(d) < Mathf.Pow(10f, -decimals) * 0.5f) { return; }
            tile.Change = (d > 0f ? "▲ " : "▼ ") + UiFormat.Number(Mathf.Abs(d), decimals) + unit;
            tile.ChangeInk = V35.DirectionNeutral;
        }

        /// <summary>§747: a price card's parts as the share bar takes them - each component in cents per kWh, inked along the area's own ladder.</summary>
        private List<V35Part> EnergyStackParts((string Name, float Value)[] stack, Color area, string anchor)
        {
            var parts = new List<V35Part>();
            for (int i = 0; i < stack.Length; i++)
            {
                Color ink = Color.Lerp(V35.CardPaper, area, 0.30f + 0.70f * i / Mathf.Max(1, stack.Length - 1));
                float luminance = 0.299f * ink.r + 0.587f * ink.g + 0.114f * ink.b;
                parts.Add(new V35Part(stack[i].Name, PlateFigure(stack[i].Value * 100f, 0, " ¢"), ink, luminance < 0.55f ? V35.OnDataDark : V35.OnDataLight, stack[i].Value * 100f, anchor + ":" + stack[i].Name));
            }
            return parts;
        }

        private float EnergyPriceCardHeight((string Name, float Value)[] stack, float width, float scale, Color area)
        {
            var probe = new V35TileData { Icon = "home", Figure = "0", Name = "Ag", FigurePx = V35.FigureSmall, Spark = new List<float> { 0f, 1f } };
            V35BarLayout l = LayOutV35Bar(width - V35.Px(V35.CardPadX) * 2f, EnergyStackParts(stack, area, "probe"), scale);
            return V35TileHeight(probe) + V35.Px(4f) + V35BarHeight(l) + V35.Px(V35.CardPadY);
        }

        /// <summary>§747: a price as the composition's card - the tile's head (the figure in cents, its change in the neutral ink, its history) and the
        /// stack as one bar on the two classes' common scale, so the smaller class stops short (15a's rule); every component on the slip.</summary>
        private void DrawEnergyPriceCard(Rect r, string icon, string name, float pricePerKwh, IReadOnlyList<float> series, (string Name, float Value)[] stack, float scale, Color area, string anchor, string census)
        {
            var tile = new V35TileData { Icon = icon, IconInk = area, Figure = PlateFigure(pricePerKwh * 100f, 1, " ¢"), Name = name, FigurePx = V35.FigureSmall, Spark = series };
            EnergyNeutralChange(tile, series != null ? Hundredfold(series) : null, 1, " ¢");
            DrawV35Tile(r, tile);
            float pad = V35.Px(V35.CardPadX);
            List<V35Part> parts = EnergyStackParts(stack, area, anchor);
            float barW = r.width - pad * 2f;
            V35BarLayout l = LayOutV35Bar(barW, parts, scale);
            var barArea = new Rect(r.x + pad, r.y + V35TileHeight(tile) + V35.Px(4f) - V35.Px(V35.CardPadY), barW, V35BarHeight(l));
            DrawV35Bar(barArea, parts, scale, l, (rect, id) => SlipAnchor(rect, id));
            SlipAnchor(new Rect(r.x, r.y, r.width, V35TileHeight(tile)), anchor);
            var slip = new SlipContent(name.ToUpperInvariant() + " · " + tile.Figure).Add(census);
            foreach ((string part, float value) in stack)
            {
                slip.Add(part.ToUpperInvariant() + " " + PlateFigure(value * 100f, 2, " ¢"));
                _energySlipBook.Anchors[anchor + ":" + part] = new SlipContent(part.ToUpperInvariant() + " · " + PlateFigure(value * 100f, 2, " ¢") + " PER kWh").Add("ONE COMPONENT OF " + name.ToUpperInvariant() + "' PRICE AS THE LEDGER WRITES IT");
            }
            slip.Add("THE BAR'S SCALE IS THE LARGER CLASS'S PRICE - THE SMALLER STOPS SHORT");
            _energySlipBook.Anchors[anchor] = slip;
        }

        private static List<float> Hundredfold(IReadOnlyList<float> series)
        {
            var list = new List<float>(series.Count);
            foreach (float v in series) { list.Add(v * 100f); }
            return list;
        }

        private static string EnergyPlantIcon(int k)
        {
            switch (k)
            {
                case 0: return "coal";
                case 1: return "gas";
                case 2: return "atom";
                case 3: return "drop";
                case 4: return "wind";
                case 5: return "sun";
                default: return "other";
            }
        }

        /// <summary>
        /// §747: one technology's fleet as a tile - its capacity, its name, and its order: − retires and + builds one step a click (the decisions plate's
        /// own logic: a step the connection queue has no room for is refused, the + drawn disabled; a step larger than the room is the room); while an
        /// order stands, its net MW as a chip in the area's ink and the tile edged in it. The lead time, what stands queued, the refusal and the orders
        /// of this technology are the slip's. Two controls a tile, always - disabled where the technology cannot be ordered or the role locks the fleet.
        /// </summary>
        private void DrawEnergyPlantTile(Rect tile, Country c, int k, int year, double step, bool fleetLocked, string fleetLockedWhy, Color area)
        {
            double mw = EnergyLayer.CapacityMw(c.Id, k);
            string name = CapitalWord(EnergyLayerData.Labels[k]);
            bool can = EnergyFleet.CanOrder(c.Id, k);
            double queued = EnergyFleet.QueuedMw(c, k);
            bool ordered = Math.Abs(queued) >= 0.5;
            Rect inner = DrawV35Card(tile);
            if (ordered && Event.current.type == EventType.Repaint)
            {
                float line = Mathf.Max(1f, V35.Px(2f));
                PoliSimTheme.Rule(new Rect(tile.x, tile.y, tile.width, line), area);
                PoliSimTheme.Rule(new Rect(tile.x, tile.yMax - line, tile.width, line), area);
                PoliSimTheme.Rule(new Rect(tile.x, tile.y, line, tile.height), area);
                PoliSimTheme.Rule(new Rect(tile.xMax - line, tile.y, line, tile.height), area);
            }
            string figure = mw >= 1000.0 ? PlateFigure((float)(mw / 1000.0), 1) + " GW" : EnergyConnectionQueue.Mw(mw) + " MW";
            Rect chipRect = new Rect(inner.xMax, inner.y, 0f, 0f);
            if (ordered)
            {
                string chipText = (queued > 0 ? "+" : "−") + (Math.Abs(queued) >= 1000.0 ? PlateFigure((float)(Math.Abs(queued) / 1000.0), 1) : EnergyConnectionQueue.Mw(Math.Abs(queued)) + " MW");
                GUIStyle chipFace = V35Mono(V35.Floor, V35.OnDataDark, bold: true, TextAnchor.MiddleCenter);
                float cw = Mathf.Ceil(chipFace.CalcSize(new GUIContent(chipText)).x) + V35.Px(12f);
                chipRect = new Rect(inner.xMax - cw, inner.y, cw, V35.Px(20f));
                if (Event.current.type == EventType.Repaint) { PoliSimTheme.Rule(chipRect, area); GUI.Label(chipRect, chipText, chipFace); }
            }
            DrawBudgetTileHead(inner, EnergyPlantIcon(k), area, figure, PoliSimTheme.TextPrimary, name, PoliSimTheme.TextPrimary, chipRect);
            float bh = V35.Px(24f), bw = V35.Px(26f);
            var minus = new Rect(inner.x, inner.yMax - bh, bw, bh);
            var plus = new Rect(inner.x + bw + V35.Px(6f), inner.yMax - bh, bw, bh);
            GUIStyle sign = V35Mono(V35.Floor, PoliSimTheme.TextPrimary, bold: true, TextAnchor.MiddleCenter);
            if (DrawDeskChipButton(minus, "−", sign, false, !can || fleetLocked)) { EnergyFleet.Place(c, k, -step, year, _simulationManager.CurrentTurn); _hasCachedPreview = false; }   // §544: a retirement lands at the coming boundary
            string full = can ? EnergyConnectionQueue.FullText(c, k, year) : null;
            double up = can ? EnergyConnectionQueue.StepUpMw(c, k, step, year) : step;   // a step larger than the line's room is the room
            if (DrawDeskChipButton(plus, "+", sign, false, !can || fleetLocked || full != null)) { EnergyFleet.Place(c, k, up, year, _simulationManager.CurrentTurn); _hasCachedPreview = false; }
            string anchor = "en:plant:" + k;
            SlipAnchor(new Rect(tile.x, tile.y, tile.width, tile.height - bh - V35.Px(V35.CardPadY)), anchor);
            var slip = new SlipContent(name.ToUpperInvariant() + " · " + figure.ToUpperInvariant());
            slip.Add(can ? "LEAD TIME " + EnergyFleet.LeadTimeYears[k] + " YEARS · ONE STEP " + EnergyConnectionQueue.Mw(step) + " MW" : EnergyFleet.CannotOrderWhy(c.Id, k));
            slip.Add(ordered ? (queued > 0 ? "+" : "−") + EnergyConnectionQueue.Mw(Math.Abs(queued)) + " MW QUEUED" : "NOTHING QUEUED");
            if (full != null) { slip.Add(full); }
            else if (can && up < step) { slip.Add("THE NEXT STEP IS THE QUEUE'S ROOM: " + EnergyConnectionQueue.Mw(up) + " MW"); }
            else if (can && EnergyConnectionQueue.NoLineText(c.Id, k) != null) { slip.Add(EnergyConnectionQueue.NoLineText(c.Id, k)); }
            foreach (EnergyFleet.Order o in EnergyFleet.Queue(c))
            {
                if (o.Technology != k) { continue; }
                slip.Add((o.Mw > 0 ? "+" : "−") + EnergyConnectionQueue.Mw(Math.Abs(o.Mw)) + " MW · PLACED " + o.OrderedYear + " · " + (o.Landed ? (o.Mw > 0 ? "CONNECTED " : "LEFT ") : (o.Mw > 0 ? "CONNECTS " : "LEAVES ")) + o.OnlineYear);
            }
            if (fleetLocked) { slip.Add(fleetLockedWhy); }
            _energySlipBook.Anchors[anchor] = slip;
        }
    }
}
