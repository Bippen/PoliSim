using System;
using System.Collections.Generic;
using System.Globalization;
using PoliSim.Data;
using PoliSim.Data.Generated;
using PoliSim.Elections;
using PoliSim.Simulation;
using UnityEngine;

namespace PoliSim.UI
{
    /// <summary>
    /// EN-6 (2026-09-12, the backlog plan's S-E1; DS-4 and DS-4b ruled): **the energy page, structural, in the v3 plate grammar,
    /// under the Economic Sectors page** - instruments first, every figure the state's own or the market's own clearing, nothing
    /// drawn that the layer does not hold and everything the layer lacks drawn as ABSENT with its reason. The page ships before
    /// any board: the board is then a composition question over a built page, read figure for figure against it.
    ///
    /// <para><b>Four plates, one family.</b> (1) The price with its decomposition - the two retail stacks the ledger writes each
    /// year (wholesale, margin, network, levies, environmental tax, VAT), the industry bill's share of GDP, the wholesale price
    /// against the other five - and, as the plate's extra row, THE RULE ON THE PRICE: each block's clearing price as a waterfall
    /// of the marginal unit's fuel and O&amp;M, its ETS cost on the emission factor, its calibration adder and the scarcity term
    /// (the market's own <c>CostParts</c>, drawn, not restated). (2) The fleet: capacity by technology beside this year's
    /// dispatched generation, and the zones as a small multiple - Sweden's four with the six links' flows against their
    /// capacities and where a block BINDS; every other country its one zone, Italy's seven and the USA's three interconnections
    /// stated as the deviation. (3) The water value and the reservoirs for Sweden, absent with the reason elsewhere, and the two
    /// ledgers as BRIDGES - system cost (fuel and O&amp;M, ETS, adders, then the inframarginal rent, closing on the wholesale
    /// outlay) and incidence (households, non-households, taxpayers paid; generators, suppliers, networks, support, the state
    /// received; the close the book's own gap, refused above the ledger's tolerance) - on the election-night bridge's own
    /// geometry and painter, generalised beneath. (4) The instruments and what is absent: the ETS price with no path, the energy
    /// line's levy scale, the carbon tax that reaches transport and not the fleet, the sector dials descriptive until their stage,
    /// and the ABSENT rows - investment and retirement, load growth, the neighbours, the hydro folds not fetched.</para>
    ///
    /// <para><b>Recomputed per turn, not per frame.</b> The market clears on demand (`EnergyMarket.Clear`) and the book is
    /// computed from the clearing; both are cached against the turn and the country so a frame costs a lookup, not a merit
    /// order. The figures written to the state (the two prices, the bill, the rent, the balance) are read from the state - the
    /// single book - and the clearing's detail (zones, blocks, links, water values) from the cache.</para>
    /// </summary>
    public partial class GameController
    {
        /// <summary>Where the energy plate was laid out last frame - the film driver scrolls to it (06c_policylaws_sectors_energy).</summary>
        private Rect _energyPlateLastArea;

        private int _energyCacheTurn = -1;
        private CountryId _energyCacheCountry;
        private EnergyMarket.Result _energyResult;
        private EnergyLedger.Book _energyBook;
        private readonly Dictionary<CountryId, double> _energyPeerWholesale = new Dictionary<CountryId, double>();

        private static readonly string[] StackLabels = { "WHOLESALE", "MARGIN", "NETWORK", "LEVIES", "ENV. TAX", "VAT" };
        private static readonly string[] BlockNames = { "BASE", "MID", "PEAK" };
        private static readonly string[] FossilNames = { "COAL", "GAS", "OIL" };

        /// <summary>The clearing and the book for the player's country this turn; the other five's wholesale price for the peer ticks.</summary>
        private void EnsureEnergyCache(Country country)
        {
            int turn = _simulationManager != null ? _simulationManager.CurrentTurn : 0;
            if (_energyResult != null && _energyCacheTurn == turn && _energyCacheCountry == country.Id) { return; }
            _energyCacheTurn = turn; _energyCacheCountry = country.Id;
            _energyResult = EnergyMarket.Clear(country);
            _energyBook = EnergyLedger.Compute(country, _energyResult, Math.Max(0.0001f, country.State.PriceLevel), EnergyLedger.CreditFor(country));
            _energyPeerWholesale.Clear();
            World world = _simulationManager?.World;
            if (world == null) { return; }
            foreach (CountryId id in PeerOrder)
            {
                if (id == country.Id) { continue; }
                Country peer = world.GetCountry(id);
                if (peer == null || !EnergyLayer.Has(id)) { continue; }
                EnergyMarket.Result pr = EnergyMarket.Clear(peer);
                _energyPeerWholesale[id] = EnergyLedger.LoadWeightedPricePerMwh(pr);
            }
        }

        private void DrawEnergyPlate()
        {
            Country country = _playerCountry;
            if (country == null) { return; }
            EconomyState s = country.State;
            StatHistory history = country.History;
            string countryUpper = country.Id.ToString().ToUpperInvariant();
            PlateFamily("Energy", EnergyLayer.Year.ToString(CultureInfo.InvariantCulture), "SOURCED · EMBER · ENTSO-E · EUROSTAT · EIA");
            if (!EnergyLayer.Has(country.Id))
            {
                DrawPlateFamilyHeader("Energy", "", "");
                GUILayout.Label("This country carries no energy layer - the six do, and this is not one of them.", _labelStyle);
                return;
            }
            EnsureEnergyCache(country);
            EnergyMarket.Result r = _energyResult;
            EnergyLedger.Book book = _energyBook;
            Color areaInk = UiPalette.GetAreaColor(UiPalette.SystemArea.Sectors);
            bool sweden = country.Id == CountryId.Sweden;
            bool usa = country.Id == CountryId.USA;
            string marketUnit = (usa ? "USD" : "EUR") + " PER MWh";
            string bookUnit = EnergyLedger.BookCurrency + " PER kWh";
            double priceIndex = Math.Max(0.0001f, s.PriceLevel);

            // ---- plate 1: the price, its decomposition, the rule on it ------------------------------------------------------
            EnergyLedger.ClassStack hh = book.Classes[EnergyLedger.Households];
            EnergyLedger.ClassStack nh = book.Classes[EnergyLedger.NonHouseholds];
            // the stacks in cents per kWh so the segments' figures read (a 0.27 book price is 27.0 cents of six parts); the row's caption carries the book's own figure
            float[] hhStack = { (float)hh.Wholesale * 100f, (float)hh.Margin * 100f, (float)hh.Network * 100f, (float)hh.Policy * 100f, (float)hh.TaxEnv * 100f, (float)hh.Vat * 100f };
            float[] nhStack = { (float)nh.Wholesale * 100f, (float)nh.Margin * 100f, (float)nh.Network * 100f, (float)nh.Policy * 100f, (float)nh.TaxEnv * 100f };
            string[] nhLabels = { "WHOLESALE", "MARGIN", "NETWORK", "LEVIES", "ENV. TAX" };
            var peers = new List<float>();
            foreach (CountryId id in PeerOrder) { if (_energyPeerWholesale.TryGetValue(id, out double w)) { peers.Add((float)w); } }
            double wholesalePerMwh = EnergyLedger.LoadWeightedPricePerMwh(r);
            var prices = new List<PlateRow>
            {
                new PlateRow("Households' price", "CENTS PER kWh · THE STACK THE LEDGER WRITES · " + PlateFigure(s.EnergyHouseholdPrice, 2) + " " + bookUnit, "EUROSTAT nrg_pc_204 · EIA · THIS YEAR'S BOOK", PlateFigure(s.EnergyHouseholdPrice * 100f, 1, " ¢"),
                    PlateBand.Distribution, 0f, Mathf.Max(1f, (float)hh.Total * 100f), (float)hh.Wholesale * 100f, null, true, new[] { "ENERGY LINE ▸", "DISPATCH ▸" }, history?.EnergyHouseholdPrice.Quarterly, new[] { "DERIVED" }, false, null, hhStack, StackLabels),
                new PlateRow("Non-households' price", "CENTS PER kWh · EXCLUDING RECOVERABLE VAT · " + PlateFigure(s.EnergyIndustryPrice, 2) + " " + bookUnit, "EUROSTAT nrg_pc_205 · EIA · THIS YEAR'S BOOK", PlateFigure(s.EnergyIndustryPrice * 100f, 1, " ¢"),
                    PlateBand.Distribution, 0f, Mathf.Max(1f, (float)nh.PreVat * 100f), (float)nh.Wholesale * 100f, null, true, new[] { "ENERGY LINE ▸", "BUSINESS CONFIDENCE ▸" }, history?.EnergyIndustryPrice.Quarterly, new[] { "DERIVED" }, false, null, nhStack, nhLabels),
                new PlateRow("Industry's electricity bill", "% OF GDP · NON-HOUSEHOLDS' CONSUMPTION × THEIR PRICE", "THE BOOK · THIS YEAR", PlateFigure(s.EnergyIndustryBillGdpShare, 2, " %"),
                    PlateBand.Open, 0f, 4f, s.EnergyIndustryBillGdpShare, null, true, new[] { "BUSINESS CONFIDENCE ▸", "PRICE LEVEL ▸" }, history?.EnergyIndustryBillGdpShare.Quarterly, new[] { "DERIVED" }, false),
                new PlateRow("Wholesale price", marketUnit + " · LOAD-WEIGHTED OVER THE BLOCKS · LOWER ◂", "THE CLEARING · SEEDED ENTSO-E · EIA · " + EnergyLayer.Year, PlateFigure((float)wholesalePerMwh, 1),
                    PlateBand.Open, 0f, 250f, (float)wholesalePerMwh, peers.ToArray(), true, new[] { "THE MERIT ORDER", "THE ETS PRICE, NOT THE CARBON TAX" }, null, new[] { "DERIVED" }, false),
            };
            string foot1 = "THE SINGLE BOOK: EVERY MONEY FIGURE IN " + EnergyLedger.BookCurrency + " AS THE STATE CARRIES IT; THE MARKET CLEARS IN ITS OWN CURRENCY AND THE CATALOG'S 2023 RATES REACH THE BOOK · THE FLEET AND THE LOAD ARE STATIC UNTIL DISPATCH · THE OTHER FIVE'S TICKS ARE THEIR OWN CLEARINGS THIS TURN";
            _energyPlateLastArea = DrawPlateRows(prices, areaInk, foot1, false, row => null,
                extraRowHeightFor: (nameH, capH, srcH, smallH) => nameH + capH + srcH + StatsUnit(22f) + capH * 2f + StatsUnit(14f),
                drawExtraRow: (x, y, pad, styles) => DrawEnergyRuleRow(x, y, pad, styles, r, country.Id, priceIndex, sweden, marketUnit));

            // ---- plate 2: the fleet and the zones ---------------------------------------------------------------------------
            float[] capacityShares = new float[EnergyLayerData.Labels.Length];
            double capacityTotal = 0;
            for (int k = 0; k < capacityShares.Length; k++) { capacityShares[k] = (float)EnergyLayer.CapacityMw(country.Id, k); capacityTotal += capacityShares[k]; }
            for (int k = 0; k < capacityShares.Length; k++) { capacityShares[k] = capacityTotal > 0 ? (float)(100.0 * capacityShares[k] / capacityTotal) : 0f; }
            string[] fleetLabels = new string[EnergyLayerData.Labels.Length];
            for (int k = 0; k < fleetLabels.Length; k++) { fleetLabels[k] = EnergyLayerData.Labels[k].ToUpperInvariant(); }
            float[] dispatched = EnergyMarket.MixSharesNow(country);
            double utilisationWind = EnergyLayer.Utilisation(country.Id, 4), utilisationSolar = EnergyLayer.Utilisation(country.Id, 5);
            var fleet = new List<PlateRow>
            {
                new PlateRow("Capacity by technology", "% OF MW · COAL·GAS·NUCLEAR·HYDRO·WIND·SOLAR·OTHER", "EMBER · EUROSTAT · EIA · " + EnergyLayer.Year + " · " + PlateFigure((float)(capacityTotal / 1000.0), 1) + " GW",
                    PlateFigure(capacityShares[4] + capacityShares[5], 0, " % WIND + SOLAR"), PlateBand.Distribution, 0f, 100f, -1f, null, true,
                    new[] { "NO INVESTMENT, NO RETIREMENT", string.Format(CultureInfo.InvariantCulture, "WIND {0:0} % · SOLAR {1:0} % UTILISED", utilisationWind * 100.0, utilisationSolar * 100.0) }, null, new[] { "SOURCED" }, false, null, capacityShares, fleetLabels),
                new PlateRow("Generation by technology", "% OF GWh · THIS YEAR'S DISPATCH", "THE CLEARING · SEEDED EMBER · EUROSTAT · EIA · " + EnergyLayer.Year, PlateFigure(dispatched[0] + dispatched[1], 0, " % FOSSIL"),
                    PlateBand.Distribution, 0f, 100f, -1f, null, true, new[] { "DISPATCHED YEARLY", "ENVIRONMENT ▸" }, null, new[] { "DERIVED" }, false, null, dispatched, EnvironmentFamily.MixLabels),
            };
            string foot2 = sweden
                ? "FOUR BIDDING ZONES ON THE SEEDED LOADS, THE CHAIN'S THREE LINKS AT THEIR CAPACITIES; A LINK BINDS WHERE A BLOCK FILLS IT · THE NEIGHBOURS OUTSIDE THE SIX ARE EXOGENOUS · NO QUANTITY MOVES DAILY"
                : "ONE ZONE ON THE SEEDED LOAD BLOCKS; THE NEIGHBOURS ARE EXOGENOUS · NO QUANTITY MOVES DAILY";
            DrawPlateRows(fleet, areaInk, foot2, false, row => null,
                extraRowHeightFor: (nameH, capH, srcH, smallH) => nameH + capH * 5f + StatsUnit(30f) + (sweden ? capH * 2f : 0f) + StatsUnit(4f),
                drawExtraRow: (x, y, pad, styles) => DrawEnergyZonesRow(x, y, pad, styles, r, country.Id, marketUnit));

            // ---- plate 3: the water and the two ledgers ---------------------------------------------------------------------
            var water = new List<PlateRow>();
            if (sweden && r.WaterValue != null && r.WaterValue.Length >= 3)
            {
                double reservoirCap = EnergyLayer.SwedenReservoirCapacityGwh();
                water.Add(new PlateRow("Water value", marketUnit + " · BASE · MID · PEAK", "THE RESERVOIRS' OWN OPPORTUNITY COST · THIS TURN",
                    string.Format(CultureInfo.InvariantCulture, "{0:0} · {1:0} · {2:0}", r.WaterValue[0], r.WaterValue[1], r.WaterValue[2]),
                    PlateBand.None, 0f, 0f, 0f, null, true, new[] { "A DEFICIT RAISES IT", "THE SLOPE IS AUTHORED" }, null, new[] { "DERIVED" }, false));
                water.Add(new PlateRow("Reservoir balance", "TWh AGAINST THE SEED'S CYCLE · STORE " + PlateFigure((float)(reservoirCap / 1000.0), 1) + " TWh", "SVENSKA KRAFTNÄT · SEEDED FILL UNSOURCED",
                    PlateFigure(Mathf.Abs(s.HydroReservoirBalanceGwh) / 1000f, 2, s.HydroReservoirBalanceGwh >= 0f ? " TWh SURPLUS" : " TWh DEFICIT"),
                    PlateBand.Open, -(float)(reservoirCap / 1000.0), (float)(reservoirCap / 1000.0), s.HydroReservoirBalanceGwh / 1000f, null, false, new[] { "WATER VALUE ▸", "THE HYDRO SHIFT ▸" }, history?.HydroReservoirBalanceGwh.Quarterly, new[] { "DERIVED" }, false));
                water.Add(new PlateRow("Congestion rent", EnergyLedger.BookCurrency + " BN · THE SIX LINKS' PRICE SPLITS × THEIR FLOWS", "THE CLEARING · CREDITED TO THE NETWORK TARIFF", PlateFigure(s.EnergyCongestionRent, 2),
                    PlateBand.Open, 0f, 2f, s.EnergyCongestionRent, null, false, new[] { "NETWORK ▸ HOUSEHOLDS' PRICE" }, history?.EnergyCongestionRent.Quarterly, new[] { "DERIVED" }, false));
            }
            else
            {
                string why = sweden ? "NO WATER VALUE THIS TURN" : (usa || country.Id == CountryId.France || country.Id == CountryId.Poland)
                    ? "THE HYDRO SHIFT'S FOLD IS BILLED FOR " + countryUpper + " · NO RESERVOIR MODELLED" : "NO RESERVOIR MODELLED FOR " + countryUpper + " · SWEDEN'S IS THE ONE";
                water.Add(new PlateRow("Water value and reservoirs", marketUnit, "RESERVOIR DISPATCH · SWEDEN ONLY", "absent",
                    PlateBand.Absent, 0f, 1f, -1f, null, true, new[] { "SWEDEN'S STORE IS THE ONE THIS GAME KEEPS" }, null, new[] { "ABSENT · STATED" }, false, why));
            }
            string foot3 = string.Format(CultureInfo.InvariantCulture,
                "THE TWO LEDGERS CLOSE ON THE SAME BOOK: SYSTEM COST {0:N1} BN OF FUEL, ETS AND ADDERS PLUS {1:N1} BN OF INFRAMARGINAL RENT IS THE WHOLESALE OUTLAY {2:N1} BN; INCIDENCE {3:N1} BN PAID AGAINST {4:N1} BN RECEIVED, THE GAP {5:E1} · THE RENT IS PRINTED, NOT MODELLED - NOTHING READS IT",
                book.FossilVariableCost, book.InframarginalRent, book.WholesaleOutlay, book.PaidTotal, book.ReceivedTotal, book.Gap);
            DrawPlateRows(water, areaInk, foot3, false, row => null,
                extraRowHeightFor: (nameH, capH, srcH, smallH) => (nameH + capH + StatsUnit(44f) + capH + StatsUnit(6f)) * 2f,
                drawExtraRow: (x, y, pad, styles) => DrawEnergyBridgesRow(x, y, pad, styles, book));

            // ---- plate 4: the instruments, and what is absent ---------------------------------------------------------------
            int ci = EnergyLayer.Index(country.Id);
            double etsSeed = ci >= 0 ? EnergyLayerData.EtsPerT[ci] : 0.0;
            var instruments = new List<PlateRow>();
            instruments.Add(etsSeed > 0
                ? new PlateRow("ETS price", (usa ? "USD" : "EUR") + " PER TONNE · THE " + EnergyLayer.Year + " MEAN CARRIED BY THE PRICE LEVEL", "EEX · THE CATALOG · NO PATH", PlateFigure((float)((etsSeed + r.EtsRisePerT) * priceIndex), 1),
                    PlateBand.None, 0f, 0f, 0f, null, true, new[] { "NO PATH · EXOGENOUS", "THE FLEET'S CARBON COST, NOT THE TAX'S" }, null, new[] { "SOURCED" }, false)
                : new PlateRow("ETS price", "PER TONNE", "NO EMISSIONS TRADING FOR " + countryUpper, "absent",
                    PlateBand.Absent, 0f, 1f, -1f, null, true, new[] { "THE FLEET PAYS NO CARBON PRICE" }, null, new[] { "ABSENT · STATED" }, false, "THE ROW IS ZERO BY THE CATALOG · A FEDERAL CARBON PRICE DOES NOT EXIST"));
            instruments.Add(book.LevyScale > 0 || !(country.Id == CountryId.Germany)
                ? new PlateRow("The energy line's levy", "× THE SEEDED LEVIES · THE LINE'S SCALE THIS YEAR", "THE BUDGET'S ENERGY LINE · THE LEDGER", PlateFigure((float)book.LevyScale, 2),
                    PlateBand.None, 0f, 0f, 0f, null, true, new[] { "ENERGY LINE ▸", "HOUSEHOLDS' PRICE ▸" }, null, new[] { "DERIVED" }, false)
                : new PlateRow("The energy line's levy", "× THE SEEDED LEVIES", "NO ENERGY LINE FOR " + countryUpper, "absent",
                    PlateBand.Absent, 0f, 1f, -1f, null, true, new[] { "THE LEVIES STAND AT THEIR SEED" }, null, new[] { "ABSENT · STATED" }, false, "THE CLIMATE FUND SITS OFF THE BUDGET · NO LINE TO SCALE"));
            instruments.Add(new PlateRow("Carbon tax", "THE NATIONAL RATE PER TONNE", "THE TAX LEDGER · " + countryUpper, PlateFigure(EnvironmentFamily.CarbonTaxRate(country), 0),
                PlateBand.None, 0f, 0f, 0f, null, true, new[] { "REACHES TRANSPORT, NOT THE FLEET", "ENVIRONMENT ▸" }, null, new[] { "SOURCED" }, false));
            instruments.Add(new PlateRow("The sector dials", "SUBSIDY · REGULATION · THE ENERGY SECTOR'S FIVE", "THE ROWS ABOVE THIS PLATE", "—",
                PlateBand.None, 0f, 0f, 0f, null, true, new[] { "DESCRIPTIVE UNTIL THEIR STAGE MAPS THEM ONTO THE INSTRUMENTS" }, null, new[] { "ABSENT · STATED" }, false));
            instruments.Add(new PlateRow("Investment and retirement", "MW BUILT · MW CLOSED", "NO RULE", "absent",
                PlateBand.Absent, 0f, 1f, -1f, null, true, new[] { "THE FLEET IS THE SEED'S" }, null, new[] { "ABSENT · STATED" }, false, "NOTHING BUILDS OR CLOSES A PLANT · THE FLEET AND THE LOAD ARE STATIC UNTIL DISPATCH"));
            instruments.Add(new PlateRow("Load growth", "GWh PER YEAR", "NO RULE", "absent",
                PlateBand.Absent, 0f, 1f, -1f, null, true, new[] { "THE LOAD IS THE SEED'S" }, null, new[] { "ABSENT · STATED" }, false, "THE LOAD DOES NOT GROW WITH GDP OR ELECTRIFICATION · STATED, NOT MODELLED"));
            if (country.Id == CountryId.Italy)
            {
                instruments.Add(new PlateRow("The seven zones", "NORD · CNOR · CSUD · SUD · CALA · SICI · SARD", "GME", "absent",
                    PlateBand.Absent, 0f, 1f, -1f, null, true, new[] { "ONE ZONE ON THIS PAGE" }, null, new[] { "ABSENT · STATED" }, false, "ONE ZONE · THE REAL MARKET HAS SEVEN"));
            }
            if (usa)
            {
                instruments.Add(new PlateRow("The three interconnections", "EASTERN · WESTERN · ERCOT", "EIA", "absent",
                    PlateBand.Absent, 0f, 1f, -1f, null, true, new[] { "ONE ZONE ON THIS PAGE" }, null, new[] { "ABSENT · STATED" }, false, "ONE ZONE · THE REAL GRID IS THREE"));
            }
            string foot4 = "WHAT THE LAYER HOLDS IS DRAWN; WHAT IT LACKS IS DRAWN AS ABSENT WITH ITS REASON · THE INSTRUMENTS REACH THE PRICE THROUGH THE MERIT ORDER AND THE LEDGER, NOTHING ELSE";
            DrawPlateRows(instruments, areaInk, foot4, false, row => null);
        }

        // ---- the rule on the price: each block's clearing price as a waterfall of the marginal unit's parts -------------------
        private void DrawEnergyRuleRow(float[] x, float y, float pad, PlateStyles styles, EnergyMarket.Result r, CountryId id, double priceIndex, bool sweden, string marketUnit)
        {
            // Two forms of one rule. The five clear a merit order: the marginal fossil unit's fuel and O&M, its ETS cost on the emission
            // factor, its calibration adder, and the scarcity term above it. Sweden's four zones are priced by the reservoir dispatch:
            // the seed's zone price carried by the price level, the water value's departure from its seed proxy on the zone's own beta,
            // the deficit slope on both - and a binding link splits the chain (the lowest offer north of a surplus cut, the ceiling
            // north of a deficit cut). The row draws whichever form priced the block, and says so.
            bool chain = sweden && r.Links != null && r.Links.Length > 0 && r.WaterValue != null && r.WaterValue.Length >= 3;
            int zone = 0;
            if (chain && r.ZoneNames != null) { int se3 = Array.IndexOf(r.ZoneNames, "SE3"); if (se3 >= 0) { zone = se3; } }
            string zoneName = r.ZoneNames != null && zone < r.ZoneNames.Length ? r.ZoneNames[zone] : id.ToString();
            PoliSimWidgets.MeasuredLabel(new Rect(x[0] + pad, y + StatsUnit(2f), x[1] - x[0] - pad, styles.NameH), "The rule on the price", styles.Name);
            PoliSimWidgets.MeasuredLabel(new Rect(x[0] + pad, y + StatsUnit(2f) + styles.NameH, x[1] - x[0] - pad, styles.CapH),
                zoneName + " · " + marketUnit + (chain ? " · SEED × LEVEL · WATER · DEFICIT" : " · FUEL + O&M · ETS · ADDER · SCARCITY"), styles.Caption);
            PoliSimWidgets.MeasuredLabel(new Rect(x[0] + pad, y + StatsUnit(2f) + styles.NameH + styles.CapH, x[1] - x[0] - pad, styles.SrcH),
                chain ? "THE RESERVOIR DISPATCH'S OWN PARTS" : string.Format(CultureInfo.InvariantCulture, "THE MARKET'S OWN PARTS · OFFERS SPREAD ±{0:0} % AROUND THE MEAN", EnergyMarket.FleetSpread * 100f), styles.Source);
            if (r.Zones == null || zone >= r.Zones.Length) { return; }
            float left = x[1] + pad, right = x[x.Length - 2] - pad;
            float cellW = (right - left) / 3f;
            float lane = StatsUnit(22f);
            float segTop = y + StatsUnit(4f) + styles.CapH;
            GUIStyle termLabel = DeskCaption(7.5f, PoliSimTheme.TextMuted);
            GUIStyle blockLabel = DeskCaption(8f, PoliSimTheme.TextSecondary, true);
            // the bold head is taller than the plate's caption at 2560 (20 against 18): its rect is its own measure, never the caption's
            float headH = Mathf.Max(styles.CapH, Mathf.Ceil(DeskCaptionHeight(blockLabel)));
            double[] seedProxy = chain ? EnergyMarket.WaterValueAtSeed() : null;
            double maxPrice = 1.0;
            for (int b = 0; b < 3 && b < r.Zones[zone].Length; b++) { maxPrice = Math.Max(maxPrice, r.Zones[zone][b].Price); }
            for (int b = 0; b < 3 && b < r.Zones[zone].Length; b++)
            {
                EnergyMarket.BlockResult block = r.Zones[zone][b];
                float cx = left + b * cellW;
                (string Name, float Value)[] terms;
                string how;
                string parts;
                if (chain)
                {
                    double seed = EnergyLayer.SeedZonePrice(zoneName, b) * priceIndex;
                    double water = EnergyLayer.ZoneBetaToProxy(zoneName) * (r.WaterValue[b] - (seedProxy != null && b < seedProxy.Length ? seedProxy[b] : 0.0) * priceIndex);
                    double own = (seed + water) * (1.0 + EnergyMarket.ReservoirDeficitSlope * r.ReservoirDeficitShare);
                    double deficit = own - seed - water;
                    bool split = Math.Abs(block.Price - own) > 1e-6;
                    how = split ? (block.Price <= EnergyMarket.CurtailmentOffer + 1e-9 ? "A SURPLUS LOCKED NORTH OF A CUT · THE LOWEST OFFER" : block.Price >= EnergyMarket.MaxClearingPrice - 1e-6 ? "A DEFICIT A LINK CANNOT FILL · THE CEILING" : "SPLIT BY A BINDING LINK") : "THE RESERVOIRS PRICE IT";
                    terms = new (string Name, float Value)[] { ("SEED × LEVEL", (float)seed), ("WATER", (float)water), ("DEFICIT", (float)deficit) };
                    parts = string.Format(CultureInfo.InvariantCulture, "{0:0} {1:+0;-0;+0} {2:+0;-0;+0}{3}", seed, water, deficit, split ? " → " + block.Price.ToString("0", CultureInfo.InvariantCulture) + " AT THE CUT" : "");
                }
                else
                {
                    // the marginal unit: the fossil category whose mean cost sits nearest the price - the fleet's offers are tranches spread
                    // ± FleetSpread around that mean, so the clearing tranche may sit under the mean (a cheaper tranche set it: the last term
                    // reads TRANCHE, negative) or above it (scarcity, positive); none when no category has a real offer
                    int marginal = -1; double marginalCost = double.NaN;
                    for (int k = 0; k < EnergyMarket.FossilCount && k < block.MarginalCost.Length; k++)
                    {
                        double c = block.MarginalCost[k];
                        if (double.IsInfinity(c) || double.IsNaN(c) || c <= 0) { continue; }
                        if (marginal < 0 || Math.Abs(c - block.Price) < Math.Abs(marginalCost - block.Price)) { marginal = k; marginalCost = c; }
                    }
                    if (block.ResidualMw <= 0 || marginal < 0)
                    {
                        how = block.ResidualMw <= 0 ? "CURTAILED" : "NO FOSSIL AT THE MARGIN";
                        PoliSimWidgets.MeasuredLabel(new Rect(cx, y + StatsUnit(2f), cellW - pad, headH), string.Format(CultureInfo.InvariantCulture, "{0} · {1:0} · {2}", BlockNames[b], block.Price, how), blockLabel);
                        DeskDottedBaseline(new Rect(cx, segTop + lane * 0.5f, cellW - pad, 1f));
                        PoliSimWidgets.MeasuredLabel(new Rect(cx, segTop + lane + StatsUnit(2f) + styles.CapH, cellW - pad, styles.CapH),
                            string.Format(CultureInfo.InvariantCulture, "{0:N0} MW DEMAND · {1:N0} MW MUST-RUN", block.DemandMw, block.MustRunMw), termLabel);
                        continue;
                    }
                    (double fuelVom, double ets) = EnergyMarket.CostParts(id, marginal, priceIndex, r.EtsRisePerT);
                    double adder = r.Adders != null && marginal < r.Adders.Length ? r.Adders[marginal] * priceIndex : 0.0;
                    double above = block.Price - marginalCost;   // scarcity above the mean offer, or a cheaper tranche below it
                    how = FossilNames[marginal] + " SETS IT" + (block.Price >= EnergyMarket.MaxClearingPrice - 1e-6 ? " · THE CEILING" : above < -1e-6 ? " · A CHEAPER TRANCHE" : "");
                    terms = new (string Name, float Value)[] { ("FUEL + O&M", (float)fuelVom), ("ETS", (float)ets), ("ADDER", (float)adder), (above >= 0 ? "SCARCITY" : "TRANCHE", (float)above) };
                    parts = string.Format(CultureInfo.InvariantCulture, "{0:0} {1:+0;-0;+0} {2:+0;-0;+0} {3:+0;-0;+0}", fuelVom, ets, adder, above);
                }
                PoliSimWidgets.MeasuredLabel(new Rect(cx, y + StatsUnit(2f), cellW - pad, headH), string.Format(CultureInfo.InvariantCulture, "{0} · {1:0} · {2}", BlockNames[b], block.Price, how), blockLabel);
                float scale = (float)((cellW - pad * 2f) / maxPrice);
                RuleWaterfall.Geometry geometry = RuleWaterfall.Compute(terms, (float)block.Price, scale);
                int positiveIndex = 0;
                float labelY = segTop + lane + StatsUnit(2f);
                foreach (RuleWaterfall.Segment seg in geometry.Positives)
                {
                    if (seg.Zero) { continue; }
                    PoliSimTheme.Rule(new Rect(cx + seg.X, segTop, Mathf.Max(1f, seg.Width), lane), positiveIndex % 2 == 0 ? PoliSimTheme.HairlineStrong : PoliSimTheme.RuleFill);
                    positiveIndex++;
                }
                float positiveSum = cx + geometry.PositiveSum;
                float cutFrom = cx + geometry.CutFrom;
                if (geometry.CutWidth > 0.5f)
                {
                    PoliSimTheme.Rule(new Rect(cutFrom, segTop, positiveSum - cutFrom, lane), PoliSimTheme.Card);
                    DrawDashedRule(new Rect(cutFrom, segTop, positiveSum - cutFrom, 1f), PoliSimTheme.Bad, 4f, 3f);
                }
                // the total tick at the price
                PoliSimTheme.Rule(new Rect(cx + geometry.TotalX - 0.5f, segTop - StatsUnit(2f), 1f, lane + StatsUnit(4f)), PoliSimTheme.Caution);
                PoliSimWidgets.MeasuredLabel(new Rect(cx, labelY, cellW - pad, styles.CapH), parts, termLabel);
                PoliSimWidgets.MeasuredLabel(new Rect(cx, labelY + styles.CapH, cellW - pad, styles.CapH),
                    string.Format(CultureInfo.InvariantCulture, "{0:N0} MW DEMAND · {1:N0} MW MUST-RUN{2}", block.DemandMw, block.MustRunMw, block.Scarcity > 0 ? " · SCARCITY " + block.Scarcity.ToString("0.00", CultureInfo.InvariantCulture) : ""), termLabel);
            }
        }

        // ---- the zones: Sweden's four as a small multiple with the six links; any other country its one zone -------------------
        private void DrawEnergyZonesRow(float[] x, float y, float pad, PlateStyles styles, EnergyMarket.Result r, CountryId id, string marketUnit)
        {
            PoliSimWidgets.MeasuredLabel(new Rect(x[0] + pad, y + StatsUnit(2f), x[1] - x[0] - pad, styles.NameH), "The zones", styles.Name);
            PoliSimWidgets.MeasuredLabel(new Rect(x[0] + pad, y + StatsUnit(2f) + styles.NameH, x[1] - x[0] - pad, styles.CapH), marketUnit + " · BASE · MID · PEAK PER ZONE", styles.Caption);
            PoliSimWidgets.MeasuredLabel(new Rect(x[0] + pad, y + StatsUnit(2f) + styles.NameH + styles.CapH, x[1] - x[0] - pad, styles.SrcH), "THE CLEARING · SEEDED LOADS AND LINKS", styles.Source);
            if (r.Zones == null || r.Zones.Length == 0) { return; }
            float left = x[1] + pad, right = x[x.Length - 2] - pad;
            int zones = r.Zones.Length;
            float cellW = (right - left) / Mathf.Max(1, zones);
            float barsTop = y + StatsUnit(4f) + styles.CapH;
            float barsH = StatsUnit(26f);
            GUIStyle zoneLabel = DeskCaption(8f, PoliSimTheme.TextSecondary, true);
            float zoneHeadH = Mathf.Max(styles.CapH, Mathf.Ceil(DeskCaptionHeight(zoneLabel)));
            GUIStyle small = DeskCaption(7.5f, PoliSimTheme.TextMuted);
            // the link lines carry a fraction slash and an arrow, taller than the caption's letters at 2560 (21 against 18): measured on those glyphs, as the guard measures them
            float smallH = Mathf.Max(styles.CapH, Mathf.Ceil(small.CalcSize(new GUIContent("SE1→SE2 1 ⁄ 1 MW")).y));
            double maxPrice = 1.0;
            for (int z = 0; z < zones; z++) { for (int b = 0; b < r.Zones[z].Length; b++) { maxPrice = Math.Max(maxPrice, r.Zones[z][b].Price); } }
            for (int z = 0; z < zones; z++)
            {
                float cx = left + z * cellW;
                string name = r.ZoneNames != null && z < r.ZoneNames.Length ? r.ZoneNames[z] : id.ToString();
                PoliSimWidgets.MeasuredLabel(new Rect(cx, y + StatsUnit(2f), cellW - pad, zoneHeadH), name, zoneLabel);
                float barW = (cellW - pad * 2f) / 3f;
                var priceText = new System.Text.StringBuilder();
                var loadText = new System.Text.StringBuilder();
                for (int b = 0; b < 3 && b < r.Zones[z].Length; b++)
                {
                    EnergyMarket.BlockResult block = r.Zones[z][b];
                    float h = (float)(barsH * Math.Min(1.0, block.Price / maxPrice));
                    PoliSimTheme.Rule(new Rect(cx + b * barW + 1f, barsTop + barsH - h, Mathf.Max(1f, barW - 3f), Mathf.Max(1f, h)), b == 2 ? PoliSimTheme.HairlineStrong : PoliSimTheme.RuleFill);
                    if (block.Scarcity > 0) { PoliSimTheme.Rule(new Rect(cx + b * barW + 1f, barsTop + barsH - h - 2f, Mathf.Max(1f, barW - 3f), 1f), PoliSimTheme.Caution); }
                    priceText.Append(b > 0 ? " · " : "").Append(block.Price.ToString("0", CultureInfo.InvariantCulture));
                    loadText.Append(b > 0 ? " · " : "").Append((block.DemandMw / 1000.0).ToString("0.0", CultureInfo.InvariantCulture));
                }
                PoliSimWidgets.MeasuredLabel(new Rect(cx, barsTop + barsH + StatsUnit(2f), cellW - pad, styles.CapH), priceText.ToString(), small);
                PoliSimWidgets.MeasuredLabel(new Rect(cx, barsTop + barsH + StatsUnit(2f) + styles.CapH, cellW - pad, styles.CapH), loadText + " GW", small);
            }
            float linksY = barsTop + barsH + StatsUnit(2f) + styles.CapH * 2f + StatsUnit(2f);
            if (r.Links != null && r.Links.Length > 0)
            {
                // the six links, two per line: flow against capacity per block, BINDS where a block fills the link
                var lines = new List<string>();
                var current = new System.Text.StringBuilder();
                for (int l = 0; l < r.Links.Length; l++)
                {
                    EnergyMarket.LinkResult link = r.Links[l];
                    var binds = new List<string>();
                    for (int b = 0; b < 3; b++) { if (link.Binding[b]) { binds.Add(BlockNames[b]); } }
                    string one = string.Format(CultureInfo.InvariantCulture, "{0}→{1} {2:N0} ⁄ {3:N0} MW PEAK{4}", link.From, link.To, Math.Abs(link.FlowMw[2]), link.CapacityMw[2],
                        binds.Count > 0 ? " · BINDS " + string.Join(", ", binds) : "");
                    if (current.Length > 0) { current.Append("   ·   "); }
                    current.Append(one);
                    if (l % 2 == 1 || l == r.Links.Length - 1) { lines.Add(current.ToString()); current.Clear(); }
                }
                for (int i = 0; i < lines.Count && i < 3; i++)
                {
                    PoliSimWidgets.MeasuredLabel(new Rect(left, linksY + i * smallH, right - left, smallH), lines[i], small);
                }
            }
            else
            {
                string note = id == CountryId.Italy ? "ONE ZONE · THE REAL MARKET HAS SEVEN" : id == CountryId.USA ? "ONE ZONE · THE REAL GRID IS THREE INTERCONNECTIONS" : "ONE ZONE · THE NEIGHBOURS ARE EXOGENOUS";
                PoliSimWidgets.MeasuredLabel(new Rect(left, linksY, right - left, styles.CapH), note, small);
            }
        }

        // ---- the two ledgers as bridges: the election-night painter over a geometry built beneath its ledger --------------------
        private void DrawEnergyBridgesRow(float[] x, float y, float pad, PlateStyles styles, EnergyLedger.Book book)
        {
            float left = x[1] + pad, right = x[x.Length - 2] - pad;
            float rowH = styles.NameH + styles.CapH + StatsUnit(44f) + styles.CapH + StatsUnit(6f);
            VoteAttributionSource none = default(VoteAttributionSource);
            var system = new List<(VoteAttributionSource Source, string Abbreviation, double Points)>
            {
                (none, "FUEL+O&M", book.FuelVomCost), (none, "ETS", book.EtsCost), (none, "ADDERS", book.AdderCost), (none, "RENT", book.InframarginalRent),
            };
            var incidence = new List<(VoteAttributionSource Source, string Abbreviation, double Points)>
            {
                (none, "HH", book.PaidHouseholds), (none, "NON-HH", book.PaidNonHouseholds), (none, "TAXPAYERS", book.PaidTaxpayers),
                (none, "GEN", -book.ToGenerators), (none, "SUPPLIERS", -book.ToSuppliers), (none, "NETWORKS", -book.ToNetworks), (none, "SUPPORT", -book.ToSupport), (none, "STATE", -book.ToStateTaxes),
            };
            DrawOneEnergyBridge(x, y, pad, styles, left, right, "System cost", EnergyLedger.BookCurrency + " BN · COST + RENT = OUTLAY",
                string.Format(CultureInfo.InvariantCulture, "OUTLAY {0:N1} BN · RENT {1:N1} BN, PRINTED", book.WholesaleOutlay, book.InframarginalRent), 0.0, book.WholesaleOutlay, system);
            DrawOneEnergyBridge(x, y + rowH, pad, styles, left, right, "Incidence", EnergyLedger.BookCurrency + " BN · PAID, THEN RECEIVED",
                string.Format(CultureInfo.InvariantCulture, "{0:N1} BN PAID · {1:N1} BN RECEIVED · GAP {2:E1}", book.PaidTotal, book.ReceivedTotal, book.Gap), 0.0, book.Gap, incidence);
        }

        private void DrawOneEnergyBridge(float[] x, float y, float pad, PlateStyles styles, float left, float right, string name, string caption, string source,
            double baseline, double close, List<(VoteAttributionSource Source, string Abbreviation, double Points)> steps)
        {
            PoliSimWidgets.MeasuredLabel(new Rect(x[0] + pad, y + StatsUnit(2f), x[1] - x[0] - pad, styles.NameH), name, styles.Name);
            PoliSimWidgets.MeasuredLabel(new Rect(x[0] + pad, y + StatsUnit(2f) + styles.NameH, x[1] - x[0] - pad, styles.CapH * 2f), caption, styles.Caption);
            PoliSimWidgets.MeasuredLabel(new Rect(x[0] + pad, y + StatsUnit(2f) + styles.NameH + styles.CapH * 2f, x[1] - x[0] - pad, styles.SrcH), source, styles.Source);
            AttributionBridge.Geometry g;
            try { g = AttributionBridge.BuildFrom(baseline, close, steps); }
            catch (InvalidOperationException e)
            {
                PoliSimWidgets.MeasuredLabel(new Rect(left, y + StatsUnit(4f), right - left, styles.CapH * 2f), "THE LINES DO NOT CLOSE · " + e.Message.ToUpperInvariant(), DeskCaption(8f, PoliSimTheme.Bad));
                return;
            }
            var box = new Rect(left, y + StatsUnit(3f), right - left, StatsUnit(44f));
            if (Event.current.type == EventType.Repaint && box.width >= 8f)
            {
                Texture2D bridge = CanvasPaint.Bridge(Mathf.RoundToInt(box.width), Mathf.RoundToInt(box.height), g, 1e6f, PoliSimTheme.Card,
                    PoliSimTheme.TextPrimary, PoliSimTheme.Caution, PoliSimTheme.Hairline, PoliSimTheme.TextPrimary);
                GUI.DrawTexture(box, bridge);
                UnityEngine.Object.DestroyImmediate(bridge);
            }
            // the abbreviations under their slots, the baseline and the close at the ends
            GUIStyle slotLabel = DeskCaption(7.5f, PoliSimTheme.TextMuted, false, TextAnchor.MiddleCenter);
            int slots = g.Steps.Count + 2;
            float slotW = box.width / slots;
            float labelY = box.yMax + StatsUnit(1f);
            PoliSimWidgets.MeasuredLabel(new Rect(box.x, labelY, slotW, styles.CapH), g.BaselinePoints.ToString("0.0", CultureInfo.InvariantCulture), slotLabel);
            for (int i = 0; i < g.Steps.Count; i++)
            {
                AttributionBridge.Step step = g.Steps[i];
                PoliSimWidgets.MeasuredLabel(new Rect(box.x + slotW * (i + 1), labelY, slotW, styles.CapH),
                    step.Abbreviation + " " + step.Points.ToString("+0.0;-0.0;0", CultureInfo.InvariantCulture), slotLabel);
            }
            PoliSimWidgets.MeasuredLabel(new Rect(box.x + slotW * (slots - 1), labelY, slotW, styles.CapH), g.ClosePoints.ToString("0.0", CultureInfo.InvariantCulture), slotLabel);
        }
    }
}
