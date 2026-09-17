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
    /// drawn that the layer does not hold and everything the layer lacks drawn as ABSENT with its reason. The page shipped before
    /// any board; the board was then a composition question over a built page, read figure for figure against it.
    ///
    /// <para><b>Board 15a (2026-09-13, Design's answer to D20 batch 1), composed over this page and built here.</b> The rule on
    /// the price is ONE device with two part-lists: 10c's waterfall from a hairline zero to the block's figure, the parts
    /// accumulating left to right under their own NAMES (FUEL+O&amp;M · ETS · ADDER · TRANCHE for the five; SEED × LEVEL · WATER ·
    /// DEFICIT for Sweden's zones), a negative term a dashed cut taken out of the right end, a zero term drawn as nothing and its
    /// name struck, the block's caption saying in bold what priced it. The wholesale row is the rule row's head and the head's
    /// figure is re-derived in the foot (Σ price × load ⁄ Σ load), printed so the reader can check it. The two retail stacks sit on
    /// ONE scale - the larger class fills the lane, the smaller stops short. The zones' links are drawn in the gutters they join,
    /// flow over capacity, BINDS as a mark and a word, the split prices either side in Caution. The water value is aligned under the
    /// rule row's three blocks - it is that row's water part, read by column. The two ledgers are 13b's bridge twice on one scale
    /// with one leader joining the figure they share (the wholesale outlay one closes on is the generators' receipt the other pays
    /// out). The absent rows sit where the quantity would sit: load growth under the zones' loads, investment under the fleet, the
    /// hydro fold at the water section, Italy's seven and the USA's three as the zones strip's one cell. The rail cell: no.</para>
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

        /// <summary>EN-7a: where the Energy sector's cost sentence was laid out last frame - the film driver scrolls to it (06c_policylaws_sectors_energy_sector_cost).</summary>
        private Rect _energySectorCostLastArea;

        /// <summary>EN-7b: where plate 4 (the instruments) was laid out last frame - the film driver scrolls to its foot (06c_policylaws_sectors_energy_electricity_tax_provenance).</summary>
        private Rect _energyInstrumentsLastArea;

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

        /// <summary>15a: a gap row drawn inside an extra row, where the quantity would sit - 10a's grammar through the plate's own drawer.</summary>
        private float EnergyGapRowHeight(string why)
        {
            GUIStyle reason = DeskBodyWrapped(11.5f, PoliSimTheme.TextPrimary);
            float width = Mathf.Max(10f, (PlateGrid.GapRow[2] / PlateGrid.Content) * Mathf.Max(10f, UiScreen.Width * 0.8f));
            return Mathf.Ceil(reason.CalcHeight(new GUIContent(why), width)) + StatsUnit(16f);
        }

        private void DrawEnergyGapRow(float[] x, float y, float pad, string name, string unit, string why)
        {
            var area = new Rect(x[0], y, x[x.Length - 1] - x[0], EnergyGapRowHeight(why));
            float[] gx = PlateGrid.Tracks(area, PlateGrid.GapRow);
            PoliSimTheme.Rule(new Rect(area.x, area.y, area.width, 1f), PoliSimTheme.RuleRow);
            var row = new PlateRow(name, unit, "", "absent", PlateBand.Absent, 0f, 1f, -1f, null, true, null, null, new[] { "ABSENT · STATED" }, false, why);
            DrawPlateGapRow(area, gx, row, DeskBodyWrapped(11.5f, PoliSimTheme.TextPrimary), pad);
        }

        private const string AbsentLoadGrowth = "THE LOAD DOES NOT GROW WITH GDP OR ELECTRIFICATION · STATED, NOT MODELLED";
        private const string AbsentInvestment = "NOTHING BUILDS OR CLOSES A PLANT · THE FLEET AND THE LOAD ARE STATIC UNTIL DISPATCH";

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

            // ---- plate 1: the price, its decomposition, the rule on it with the wholesale as its head ------------------------------
            EnergyLedger.ClassStack hh = book.Classes[EnergyLedger.Households];
            EnergyLedger.ClassStack nh = book.Classes[EnergyLedger.NonHouseholds];
            // the stacks in cents per kWh so the segments' figures read (a 0.27 book price is 27.0 cents of six parts); the row's caption carries the book's own figure
            float[] hhStack = { (float)hh.Wholesale * 100f, (float)hh.Margin * 100f, (float)hh.Network * 100f, (float)hh.Policy * 100f, (float)hh.TaxEnv * 100f, (float)hh.Vat * 100f };
            float[] nhStack = { (float)nh.Wholesale * 100f, (float)nh.Margin * 100f, (float)nh.Network * 100f, (float)nh.Policy * 100f, (float)nh.TaxEnv * 100f };
            string[] nhLabels = { "WHOLESALE", "MARGIN", "NETWORK", "LEVIES", "ENV. TAX" };
            // 15a: the two stacks on ONE scale - the larger class fills the lane, the smaller stops short (the band scales by the row's High where it exceeds the parts' sum)
            float stackScale = Mathf.Max(1f, Mathf.Max((float)hh.Total, (float)nh.PreVat) * 100f);
            var peers = new List<float>();
            foreach (CountryId id in PeerOrder) { if (_energyPeerWholesale.TryGetValue(id, out double w)) { peers.Add((float)w); } }
            double wholesalePerMwh = EnergyLedger.LoadWeightedPricePerMwh(r);
            var prices = new List<PlateRow>
            {
                new PlateRow("Households' price", "CENTS PER kWh · THE STACK THE LEDGER WRITES · " + PlateFigure(s.EnergyHouseholdPrice, 2) + " " + bookUnit, "EUROSTAT nrg_pc_204 · EIA · THIS YEAR'S BOOK", PlateFigure(s.EnergyHouseholdPrice * 100f, 1, " ¢"),
                    PlateBand.Distribution, 0f, stackScale, (float)hh.Wholesale * 100f, null, true, new[] { "ENERGY LINE ▸", "DISPATCH ▸" }, history?.EnergyHouseholdPrice.Quarterly, new[] { "DERIVED" }, false, null, hhStack, StackLabels, scaleToHigh: true),
                new PlateRow("Non-households' price", "CENTS PER kWh · EXCLUDING RECOVERABLE VAT · " + PlateFigure(s.EnergyIndustryPrice, 2) + " " + bookUnit, "EUROSTAT nrg_pc_205 · EIA · THIS YEAR'S BOOK", PlateFigure(s.EnergyIndustryPrice * 100f, 1, " ¢"),
                    PlateBand.Distribution, 0f, stackScale, (float)nh.Wholesale * 100f, null, true, new[] { "ENERGY LINE ▸", "BUSINESS CONFIDENCE ▸" }, history?.EnergyIndustryPrice.Quarterly, new[] { "DERIVED" }, false, null, nhStack, nhLabels, scaleToHigh: true),
                new PlateRow("Industry's electricity bill", "% OF GDP · NON-HOUSEHOLDS' CONSUMPTION × THEIR PRICE", "THE BOOK · THIS YEAR", PlateFigure(s.EnergyIndustryBillGdpShare, 2, " %"),
                    PlateBand.Open, 0f, 4f, s.EnergyIndustryBillGdpShare, null, true, new[] { "BUSINESS CONFIDENCE ▸", "PRICE LEVEL ▸" }, history?.EnergyIndustryBillGdpShare.Quarterly, new[] { "DERIVED" }, false),
                // 15a: the wholesale row is the rule row's HEAD - the figure and the five peers' ticks; the three blocks that follow are its body
                new PlateRow("Wholesale price", marketUnit + " · LOAD-WEIGHTED OVER THE BLOCKS · LOWER ◂", "OWN TICK · OTHER FIVE'S CLEARINGS · ENTSO-E · EIA · " + EnergyLayer.Year, PlateFigure((float)wholesalePerMwh, 1),
                    PlateBand.Open, 0f, 250f, (float)wholesalePerMwh, peers.ToArray(), true, new[] { "THE RULE ROW BELOW IS ITS BODY", "THE ETS PRICE, NOT THE CARBON TAX" }, null, new[] { "DERIVED" }, false),
            };
            string foot1 = "THE STACKS, LEFT TO RIGHT: WHOLESALE · MARGIN · NETWORK · LEVIES · ENV. TAX · VAT, ON ONE SCALE · THE SINGLE BOOK: EVERY MONEY FIGURE IN " + EnergyLedger.BookCurrency + " AS THE STATE CARRIES IT; THE MARKET CLEARS IN ITS OWN CURRENCY AND THE CATALOG'S 2023 RATES REACH THE BOOK · THE OTHER FIVE'S TICKS ARE THEIR OWN CLEARINGS THIS TURN";
            _energyPlateLastArea = DrawPlateRows(prices, areaInk, foot1, false, row => null,
                extraRowHeightFor: (nameH, capH, srcH, smallH) => EnergyRuleRowHeight(nameH, capH, srcH),
                drawExtraRow: (x, y, pad, styles) => DrawEnergyRuleRow(x, y, pad, styles, r, country.Id, priceIndex, sweden, marketUnit, wholesalePerMwh));

            // ---- plate 2: the fleet, investment absent under it, the zones with load growth absent under their loads ----------------
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
                // 15a: two bars, one order, one legend - capacity and generation share the technology order (the same seven, left to right) and cannot share a scale
                new PlateRow("Capacity by technology", "% OF MW · COAL·GAS·NUCLEAR·HYDRO·WIND·SOLAR·OTHER", "EMBER · EUROSTAT · EIA · " + EnergyLayer.Year + " · " + PlateFigure((float)(capacityTotal / 1000.0), 1) + " GW",
                    PlateFigure(capacityShares[4] + capacityShares[5], 0, " % WIND + SOLAR"), PlateBand.Distribution, 0f, 100f, -1f, null, true,
                    new[] { "NO INVESTMENT, NO RETIREMENT", string.Format(CultureInfo.InvariantCulture, "WIND {0:0} % · SOLAR {1:0} % UTILISED", utilisationWind * 100.0, utilisationSolar * 100.0) }, null, new[] { "SOURCED" }, false, null, capacityShares, fleetLabels),
                new PlateRow("Generation by technology", "% OF GWh · THE SAME ORDER · THIS YEAR'S DISPATCH", "THE CLEARING · SEEDED EMBER · EUROSTAT · EIA · " + EnergyLayer.Year, PlateFigure(dispatched[0] + dispatched[1], 0, " % FOSSIL"),
                    PlateBand.Distribution, 0f, 100f, -1f, null, true, new[] { "DISPATCHED YEARLY", "ENVIRONMENT ▸" }, null, new[] { "DERIVED" }, false, null, dispatched, EnvironmentFamily.MixLabels),
                new PlateRow("Investment and retirement", "MW BUILT · MW CLOSED", "NO RULE", "absent",
                    PlateBand.Absent, 0f, 1f, -1f, null, true, new[] { "THE FLEET IS THE SEED'S" }, null, new[] { "ABSENT · STATED" }, false, AbsentInvestment),
            };
            string foot2 = sweden
                ? "THE FLEET'S TWO BARS SHARE ONE ORDER, NOT ONE SCALE: A COLUMN READS SHARE OF THE FLEET, THEN SHARE OF THE POWER · FOUR BIDDING ZONES ON THE SEEDED LOADS, THE CHAIN'S THREE LINKS DRAWN IN THE GUTTERS THEY JOIN AT PEAK FLOW OVER CAPACITY; A LINK BINDS WHERE A BLOCK FILLS IT AND THE PRICES EITHER SIDE SPLIT · THE NEIGHBOURS OUTSIDE THE SIX ARE EXOGENOUS · NO QUANTITY MOVES DAILY"
                : "THE FLEET'S TWO BARS SHARE ONE ORDER, NOT ONE SCALE: A COLUMN READS SHARE OF THE FLEET, THEN SHARE OF THE POWER · ONE ZONE ON THE SEEDED LOAD BLOCKS; THE NEIGHBOURS ARE EXOGENOUS · NO QUANTITY MOVES DAILY";
            DrawPlateRows(fleet, areaInk, foot2, false, row => null,
                extraRowHeightFor: (nameH, capH, srcH, smallH) => EnergyZonesRowHeight(nameH, capH, srcH, sweden) + EnergyGapRowHeight(AbsentLoadGrowth),
                drawExtraRow: (x, y, pad, styles) => DrawEnergyZonesRow(x, y, pad, styles, r, country.Id, marketUnit, sweden));

            // ---- plate 3: the water, aligned under the rule row's blocks, and the two ledgers on one scale ----------------------------
            var water = new List<PlateRow>();
            bool waterKnown = sweden && r.WaterValue != null && r.WaterValue.Length >= 3;
            if (waterKnown)
            {
                double reservoirCap = EnergyLayer.SwedenReservoirCapacityGwh();
                water.Add(new PlateRow("Reservoir balance", "TWh AGAINST THE SEED'S CYCLE · STORE " + PlateFigure((float)(reservoirCap / 1000.0), 1) + " TWh", "SVENSKA KRAFTNÄT · SEEDED FILL UNSOURCED",
                    PlateFigure(Mathf.Abs(s.HydroReservoirBalanceGwh) / 1000f, 2, s.HydroReservoirBalanceGwh >= 0f ? " TWh SURPLUS" : " TWh DEFICIT"),
                    PlateBand.Open, -(float)(reservoirCap / 1000.0), (float)(reservoirCap / 1000.0), s.HydroReservoirBalanceGwh / 1000f, null, false, new[] { "WATER VALUE ▸", "THE HYDRO SHIFT ▸" }, history?.HydroReservoirBalanceGwh.Quarterly, new[] { "DERIVED" }, false));
                water.Add(new PlateRow("Congestion rent", EnergyLedger.BookCurrency + " BN · THE LINKS' PRICE SPLITS × THEIR FLOWS", "THE CLEARING · CREDITED TO THE NETWORK TARIFF", PlateFigure(s.EnergyCongestionRent, 2),
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
                "THE TWO LEDGERS CLOSE ON THE SAME BOOK AND ARE DRAWN ON ONE SCALE: SYSTEM COST {0:N1} BN OF FUEL, ETS AND ADDERS PLUS {1:N1} BN OF INFRAMARGINAL RENT IS THE WHOLESALE OUTLAY {2:N1} BN - {6:0} % OF WHAT IS PAID; INCIDENCE {3:N1} BN PAID AGAINST {4:N1} BN RECEIVED, THE GAP {5:0.0} · THE RENT IS PRINTED, NOT MODELLED - NOTHING READS IT",
                book.FossilVariableCost, book.InframarginalRent, book.WholesaleOutlay, book.PaidTotal, book.ReceivedTotal, book.Gap, book.PaidTotal > 0 ? 100.0 * book.WholesaleOutlay / book.PaidTotal : 0.0);
            DrawPlateRows(water, areaInk, foot3, false, row => null,
                extraRowHeightFor: (nameH, capH, srcH, smallH) => (waterKnown ? EnergyWaterRowHeight(nameH, capH, srcH) : 0f) + EnergyBridgesRowHeight(nameH, capH, srcH),
                drawExtraRow: (x, y, pad, styles) =>
                {
                    float yy = y;
                    if (waterKnown) { DrawEnergyWaterRow(x, yy, pad, styles, r, marketUnit); yy += EnergyWaterRowHeight(styles.NameH, styles.CapH, styles.SrcH); }
                    DrawEnergyBridgesRow(x, yy, pad, styles, book);
                });

            // ---- plate 4: the instruments -------------------------------------------------------------------------------------------
            int ci = EnergyLayer.Index(country.Id);
            double etsSeed = ci >= 0 ? EnergyLayerData.EtsPerT[ci] : 0.0;
            var instruments = new List<PlateRow>();
            instruments.Add(etsSeed > 0
                ? new PlateRow("ETS price", (usa ? "USD" : "EUR") + " PER TONNE · THE " + EnergyLayer.Year + " MEAN CARRIED BY THE PRICE LEVEL", "EEX · THE CATALOG · NO PATH", PlateFigure((float)((etsSeed + r.EtsRisePerT) * priceIndex), 1),
                    PlateBand.None, 0f, 0f, 0f, null, true, new[] { "NO PATH · " + EnergyLayer.Year + " MEAN CARRIED BY THE LEVEL", "THE FLEET'S CARBON COST, NOT THE TAX'S" }, null, new[] { "SOURCED" }, false, chipsWithoutBand: true)
                : new PlateRow("ETS price", "PER TONNE", "NO EMISSIONS TRADING FOR " + countryUpper, "absent",
                    PlateBand.Absent, 0f, 1f, -1f, null, true, new[] { "THE FLEET PAYS NO CARBON PRICE" }, null, new[] { "ABSENT · STATED" }, false, "THE ROW IS ZERO BY THE CATALOG · A FEDERAL CARBON PRICE DOES NOT EXIST"));
            // EN-7a (2026-09-14): the Energy sector's five dials ARE the four instruments - retail intervention (the subsidy's money side on the energy
            // line, the levy the other way), market liberalisation (the regulation gap splitting the supply margin), investment planning and state
            // ownership (absent: the fleet neither invests nor changes hands, and the rent an ownership share would read is printed, never read),
            // research grants descriptive - with the two carbon prices beside them; each row's chips say what it reaches
            if (!SectorCouplings.HasEnergyLine(country))
            {
                instruments.Add(new PlateRow("Retail intervention", "× THE SEEDED LEVIES", "NO ENERGY LINE FOR " + countryUpper, "absent",
                    PlateBand.Absent, 0f, 1f, -1f, null, true, new[] { "THE LEVIES STAND AT THEIR SEED" }, null, new[] { "ABSENT · STATED" }, false, "THE CLIMATE FUND SITS OFF THE BUDGET · THE SUBSIDY DIAL'S COST STAYS WITH THE OTHER SECTORS' SUPPORT"));
            }
            else if (!EnergyLedger.HasPolicyLevy(country.Id))
            {
                // the review's finding: the USA's line carries the subsidy but its retail components are billed - no levy, so no retail effect, and a 0.00 read as a levy displaced
                instruments.Add(new PlateRow("Retail intervention", "× THE SEEDED LEVIES", "NO POLICY LEVY IN THE STACK FOR " + countryUpper, "absent",
                    PlateBand.Absent, 0f, 1f, -1f, null, true, new[] { "SUBSIDY DIAL ▸ ENERGY LINE" }, null, new[] { "ABSENT · STATED" }, false, "THE RETAIL COMPONENTS ARE BILLED · THE SUBSIDY'S COST LANDS ON THE ENERGY LINE WITH NO RETAIL EFFECT"));
            }
            else
            {
                instruments.Add(new PlateRow("Retail intervention", "× THE SEEDED LEVIES · CUT ONE FOR ONE BY THE SUBSIDY", "THE ENERGY SECTOR'S SUBSIDY DIAL · THE BUDGET'S ENERGY LINE", PlateFigure((float)book.LevyScale, 2),
                    PlateBand.None, 0f, 0f, 0f, null, true, new[] { "SUBSIDY DIAL ▸ ENERGY LINE", "LEVY ▸ HOUSEHOLDS' PRICE" }, null, new[] { "DERIVED" }, false, chipsWithoutBand: true));
            }
            instruments.Add(new PlateRow("Market liberalisation", "POINTS BELOW THE SEED'S REGULATION · MARGIN TO HOUSEHOLDS", "THE ENERGY SECTOR'S REGULATION DIAL · STEINER, OECD 2000", PlateFigure((float)(EnergyLedger.LiberalisationGap(country) * 100.0), 0),
                PlateBand.None, 0f, 0f, 0f, null, true, new[] { "REGULATION DIAL ▸ MARGIN SPLIT", "INDUSTRY'S BILL ▸", "HOUSEHOLDS' PRICE ▸" }, null, new[] { "DECLARED" }, false, chipsWithoutBand: true));
            instruments.Add(new PlateRow("Investment planning", "THE TAX CREDITS DIAL", "THE FLEET DOES NOT INVEST OR RETIRE", "absent",
                PlateBand.Absent, 0f, 1f, -1f, null, true, new[] { "NOTHING IN THE LAYER TO REACH" }, null, new[] { "ABSENT · STATED" }, false, "NO INVESTMENT RULE - THE FLEET IS THE SEED'S · THE DIAL'S COST STAYS WITH THE OTHER SECTORS' SUPPORT"));
            instruments.Add(new PlateRow("State ownership", "THE NATIONALIZATION / DEREGULATION DIAL", "NO OWNERSHIP TERM IN THE LEDGERS", "absent",
                PlateBand.Absent, 0f, 1f, -1f, null, true, new[] { "THE SECTOR ROW STILL READS IT" }, null, new[] { "ABSENT · STATED" }, false, "A STATE SHARE OF THE GENERATORS' RECEIPTS WOULD READ THE RENT THE PAGE PRINTS AND NOTHING READS · THE DIAL STILL MOVES THE SECTOR'S OUTPUT AND EMPLOYMENT"));
            instruments.Add(new PlateRow("Research grants", "THE RESEARCH GRANTS DIAL", "NO RESEARCH MECHANIC", "—",
                PlateBand.None, 0f, 0f, 0f, null, true, new[] { "DESCRIPTIVE · THE SECTOR ROW ONLY" }, null, new[] { "ABSENT · STATED" }, false, chipsWithoutBand: true));
            instruments.Add(new PlateRow("Carbon tax", "THE NATIONAL RATE PER TONNE", "THE TAX LEDGER · " + countryUpper, PlateFigure(EnvironmentFamily.CarbonTaxRate(country), 0),
                PlateBand.None, 0f, 0f, 0f, null, true, new[] { "REACHES TRANSPORT, NOT THE FLEET", "ENVIRONMENT ▸" }, null, new[] { "SOURCED" }, false, chipsWithoutBand: true));
            // EN-7b (2026-09-15): the electricity tax - the statute per class as the laws in force leave it, against the 2023 statute; the stack's
            // environmental-tax component moves by the change within its coverage (the plate above reads this turn's cached book, the rows the live statute)
            if (EnergyLayer.HasElectricityTax(country.Id))
            {
                for (int cls = 0; cls < EnergyLedger.ClassCount; cls++)
                {
                    bool homes = cls == EnergyLedger.Households;
                    float composed = homes ? country.ElectricityTaxHouseholds : country.ElectricityTaxNonHouseholds;
                    float statute = EnergyLedger.EffectiveElectricityTaxEurPerMwh(country, cls);   // the rate the ledger moves the stack by - a business cut below the EU minimum reads the minimum
                    float statuteBase = homes ? country.ElectricityTaxHouseholdsBase : country.ElectricityTaxNonHouseholdsBase;
                    string source = string.Format(CultureInfo.InvariantCulture, "2023 STATUTE {0:0.###} · STACK CARRIES {1:0.00} OF A CHANGE", statuteBase, EnergyLayer.ElectricityTaxCoverage(country.Id, cls));
                    string caption = statute != composed ? "EUR PER MWh · HELD AT THE EU BUSINESS MINIMUM" : "EUR PER MWh · THE STATUTE AS THE LAWS IN FORCE LEAVE IT";
                    instruments.Add(new PlateRow(homes ? "Electricity tax, households" : "Electricity tax, firms", caption, source, PlateFigure(statute, 2),
                        PlateBand.None, 0f, 0f, 0f, null, true, new[] { "LAWS ▸ STATUTE", homes ? "ENV. TAX ▸ HOUSEHOLDS' PRICE" : "ENV. TAX ▸ INDUSTRY'S BILL", "BUDGET ▸" }, null, new[] { "SOURCED" }, false, chipsWithoutBand: true));
                }
            }
            else
            {
                instruments.Add(new PlateRow("Electricity tax", "EUR PER MWh", "NO FEDERAL ELECTRICITY EXCISE FOR " + countryUpper, "absent",
                    PlateBand.Absent, 0f, 1f, -1f, null, true, new[] { "THE LAWS ARE NOT OFFERED" }, null, new[] { "ABSENT · STATED" }, false, "THE STATES LEVY THEIR OWN GROSS-RECEIPTS TAXES · NO NATIONAL STATUTE FOR THIS HOUSE TO MOVE"));
            }
            string foot4 = "THE ENERGY SECTOR'S FIVE DIALS ARE THE INSTRUMENTS FROM RETAIL INTERVENTION TO RESEARCH GRANTS - SET ON THE SECTORS PAGE ABOVE, NO SIXTH CONTROL · THE ETS PRICE IS THE MARKET'S, THE CARBON TAX A BUDGET ROW, THE ELECTRICITY TAX THE LAWS' · WHAT EACH REACHES IS ITS CHIP - SO NO RAIL CELL · WHAT THE LAYER LACKS IS DRAWN AS ABSENT WHERE THE QUANTITY WOULD SIT";
            _energyInstrumentsLastArea = DrawPlateRows(instruments, areaInk, foot4, false, row => null);
        }

        // ---- the rule on the price: one device, two part-lists; the wholesale row above is its head, the derivation its foot -----------
        /// <summary>The derivation line carries Σ and ⁄, which stand taller than the caption's letters at 2560 (21 against 18): its height is
        /// measured on those glyphs, as the guard measures them.</summary>
        private float EnergyDerivationLineHeight(float capH) => Mathf.Max(capH, Mathf.Ceil(DeskCaption(7.5f, PoliSimTheme.TextMuted).CalcSize(new GUIContent("Σ PRICE × ENERGY ⁄ Σ ENERGY")).y));

        private float EnergyRuleRowHeight(float nameH, float capH, float srcH)
        {
            float nameCol = StatsUnit(2f) + nameH + capH + srcH * 2f;
            float blocks = StatsUnit(4f) + capH + StatsUnit(22f) + StatsUnit(2f) + capH * 2f;
            return Mathf.Max(nameCol, blocks) + StatsUnit(4f) + EnergyDerivationLineHeight(capH) + StatsUnit(8f);
        }

        private void DrawEnergyRuleRow(float[] x, float y, float pad, PlateStyles styles, EnergyMarket.Result r, CountryId id, double priceIndex, bool sweden, string marketUnit, double wholesalePerMwh)
        {
            // Two forms of one rule, ONE device (15a). The five clear a merit order: the marginal fossil unit's fuel and O&M, its ETS cost
            // on the emission factor, its calibration adder, and the tranche or scarcity term. Sweden's four zones are priced by the
            // reservoir dispatch: the seed's zone price carried by the price level, the water value's departure from its seed proxy on the
            // zone's own beta, the deficit slope on both - and a binding link splits the chain. The geometry is one - 10c's waterfall -
            // and the FORM is carried by the parts' names and by the caption that says what priced the block.
            bool chain = sweden && r.Links != null && r.Links.Length > 0 && r.WaterValue != null && r.WaterValue.Length >= 3;
            int zone = 0;
            if (chain && r.ZoneNames != null) { int se3 = Array.IndexOf(r.ZoneNames, "SE3"); if (se3 >= 0) { zone = se3; } }
            string zoneName = r.ZoneNames != null && zone < r.ZoneNames.Length ? r.ZoneNames[zone] : id.ToString();
            float nameTop = y + StatsUnit(2f);
            PoliSimWidgets.MeasuredLabel(new Rect(x[0] + pad, nameTop, x[1] - x[0] - pad, styles.NameH), "The rule on the price", styles.Name);
            PoliSimWidgets.MeasuredLabel(new Rect(x[0] + pad, nameTop + styles.NameH, x[1] - x[0] - pad, styles.CapH),
                zoneName + " · " + marketUnit + (chain ? " · SEED × LEVEL · WATER · DEFICIT" : " · FUEL+O&M · ETS · ADDER · TRANCHE⁄SCARCITY"), styles.Caption);
            PoliSimWidgets.MeasuredLabel(new Rect(x[0] + pad, nameTop + styles.NameH + styles.CapH, x[1] - x[0] - pad, styles.SrcH), "ONE DEVICE · THE FORM IS IN THE PARTS' NAMES", styles.Source);
            PoliSimWidgets.MeasuredLabel(new Rect(x[0] + pad, nameTop + styles.NameH + styles.CapH + styles.SrcH, x[1] - x[0] - pad, styles.SrcH), "AND IN THE CAPTION THAT SAYS WHAT PRICED IT", styles.Source);
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
                if (chain)
                {
                    double seed = EnergyLayer.SeedZonePrice(zoneName, b) * priceIndex;
                    double water = EnergyLayer.ZoneBetaToProxy(zoneName) * (r.WaterValue[b] - (seedProxy != null && b < seedProxy.Length ? seedProxy[b] : 0.0) * priceIndex);
                    double own = (seed + water) * (1.0 + EnergyMarket.ReservoirDeficitSlope * r.ReservoirDeficitShare);
                    double deficit = own - seed - water;
                    bool split = Math.Abs(block.Price - own) > 1e-6;
                    how = split ? (block.Price <= EnergyMarket.CurtailmentOffer + 1e-9 ? "A SURPLUS LOCKED NORTH OF A CUT · THE LOWEST OFFER" : block.Price >= EnergyMarket.MaxClearingPrice - 1e-6 ? "A DEFICIT A LINK CANNOT FILL · THE CEILING" : "SPLIT BY A BINDING LINK") : "THE RESERVOIRS PRICE IT";
                    terms = split
                        ? new (string Name, float Value)[] { ("SEED × LEVEL", (float)seed), ("WATER", (float)water), ("DEFICIT", (float)deficit), ("AT THE CUT", (float)(block.Price - own)) }
                        : new (string Name, float Value)[] { ("SEED × LEVEL", (float)seed), ("WATER", (float)water), ("DEFICIT", (float)deficit) };
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
                        // CURTAILED draws the block's head with no parts and the word - the one place the page's Caution hue means a constraint bound
                        how = block.ResidualMw <= 0 ? "CURTAILED" : "NO FOSSIL AT THE MARGIN";
                        PoliSimWidgets.MeasuredLabel(new Rect(cx, y + StatsUnit(2f), cellW - pad, headH), string.Format(CultureInfo.InvariantCulture, "{0} · {1:0} · {2}", BlockNames[b], block.Price, how), DeskCaption(8f, block.ResidualMw <= 0 ? PoliSimTheme.Caution : PoliSimTheme.TextSecondary, true));
                        DeskDottedBaseline(new Rect(cx, segTop + lane * 0.5f, cellW - pad, 1f));
                        PoliSimWidgets.MeasuredLabel(new Rect(cx, segTop + lane + StatsUnit(2f) + styles.CapH, cellW - pad, styles.CapH),
                            string.Format(CultureInfo.InvariantCulture, "{0:N0} MW DEMAND · {1:N0} MW MUST-RUN", block.DemandMw, block.MustRunMw), termLabel);
                        continue;
                    }
                    (double fuelVom, double ets) = EnergyMarket.CostParts(id, marginal, priceIndex, r.EtsRisePerT);
                    double adder = r.Adders != null && marginal < r.Adders.Length ? r.Adders[marginal] * priceIndex : 0.0;
                    double above = block.Price - marginalCost;   // scarcity above the mean offer, or a cheaper tranche below it
                    how = FossilNames[marginal] + " SETS IT" + (block.Price >= EnergyMarket.MaxClearingPrice - 1e-6 ? " · THE CEILING" : above < -1e-6 ? " · A CHEAPER TRANCHE" : above > 1e-6 ? " · SCARCITY" : "");
                    terms = new (string Name, float Value)[] { ("FUEL+O&M", (float)fuelVom), ("ETS", (float)ets), ("ADDER", (float)adder), (above >= 0 ? "SCARCITY" : "TRANCHE", (float)above) };
                }
                PoliSimWidgets.MeasuredLabel(new Rect(cx, y + StatsUnit(2f), cellW - pad, headH), string.Format(CultureInfo.InvariantCulture, "{0} · {1:0} · {2}", BlockNames[b], block.Price, how), blockLabel);
                float scale = (float)((cellW - pad * 2f) / maxPrice);   // the peak block fills its lane
                RuleWaterfall.Geometry geometry = RuleWaterfall.Compute(terms, (float)block.Price, scale);
                // the hairline zero the parts accumulate from
                PoliSimTheme.Rule(new Rect(cx - 0.5f, segTop - StatsUnit(2f), 1f, lane + StatsUnit(4f)), PoliSimTheme.Hairline);
                int positiveIndex = 0;
                foreach (RuleWaterfall.Segment seg in geometry.Positives)
                {
                    if (seg.Zero) { continue; }
                    // a scarcity term is a solid part in Caution ink - a constraint bound; every other part alternates the two rule tints
                    Color partInk = seg.Name == "SCARCITY" ? PoliSimTheme.Caution : positiveIndex % 2 == 0 ? PoliSimTheme.HairlineStrong : PoliSimTheme.RuleFill;
                    PoliSimTheme.Rule(new Rect(cx + seg.X, segTop, Mathf.Max(1f, seg.Width), lane), partInk);
                    positiveIndex++;
                }
                float positiveSum = cx + geometry.PositiveSum;
                float cutFrom = cx + geometry.CutFrom;
                if (geometry.CutWidth > 0.5f)
                {
                    PoliSimTheme.Rule(new Rect(cutFrom, segTop, positiveSum - cutFrom, lane), PoliSimTheme.Card);
                    DrawDashedRule(new Rect(cutFrom, segTop, positiveSum - cutFrom, 1f), PoliSimTheme.Bad, 4f, 3f);
                    DrawDashedRule(new Rect(cutFrom, segTop + lane - 1f, positiveSum - cutFrom, 1f), PoliSimTheme.Bad, 4f, 3f);
                }
                // the total tick at the price
                PoliSimTheme.Rule(new Rect(cx + geometry.TotalX - 0.5f, segTop - StatsUnit(2f), 1f, lane + StatsUnit(4f)), PoliSimTheme.Caution);
                // the parts under the bar, each under its NAME; a zero term's name struck through, drawn as nothing above
                float labelY = segTop + lane + StatsUnit(2f);
                DrawEnergyTermNames(new Rect(cx, labelY, cellW - pad, styles.CapH), terms, termLabel);
                PoliSimWidgets.MeasuredLabel(new Rect(cx, labelY + styles.CapH, cellW - pad, styles.CapH),
                    string.Format(CultureInfo.InvariantCulture, "{0:N0} MW DEMAND · {1:N0} MW MUST-RUN", block.DemandMw, block.MustRunMw), termLabel);
            }
            // the head's figure re-derived from the body with the clearing's own weights - each block's seed MW times its hours, the
            // energy it serves - printed so the reader can check it; the first film weighted by MW alone and read 73 against 58
            double weighted = 0, load = 0;
            for (int z = 0; z < r.Zones.Length; z++)
            {
                int zi = r.ZoneNames != null && z < r.ZoneNames.Length ? EnergyLayer.ZoneIndex(r.ZoneNames[z]) : -1;
                for (int b = 0; b < r.Zones[z].Length && b < 3; b++)
                {
                    double mw = zi < 0 ? r.Zones[z][b].DemandMw : sweden ? EnergyLayerData.ZoneConsumptionBlockMw[zi][b] : EnergyLayerData.DispatchDemandMw[zi][b];
                    double energy = Math.Max(0.0, mw) * (zi < 0 ? 1.0 : EnergyLayerData.DispatchHours[zi][b]);
                    weighted += r.Zones[z][b].Price * energy; load += energy;
                }
            }
            double rederived = load > 0 ? weighted / load : 0;
            string over = chain ? "TWELVE BLOCKS" : "THREE BLOCKS";
            string derivation = Math.Abs(rederived - wholesalePerMwh) < 0.05
                ? string.Format(CultureInfo.InvariantCulture, "{0:0.0} = Σ PRICE × ENERGY ⁄ Σ ENERGY, {1} · ENERGY = SEED MW × HOURS PER BLOCK · THE PEAK BLOCK FILLS ITS LANE", wholesalePerMwh, over)
                : string.Format(CultureInfo.InvariantCulture, "{0:0.0} IS THE CLEARING'S; Σ PRICE × ENERGY ⁄ Σ ENERGY, {1}, READS {2:0.0} HERE - THE TWO WEIGHTS DIFFER", wholesalePerMwh, over, rederived);
            float footY = y + Mathf.Max(StatsUnit(2f) + styles.NameH + styles.CapH + styles.SrcH * 2f, StatsUnit(4f) + styles.CapH + lane + StatsUnit(2f) + styles.CapH * 2f) + StatsUnit(4f);
            PoliSimWidgets.MeasuredLabel(new Rect(left, footY, right - left, EnergyDerivationLineHeight(styles.CapH)), derivation, termLabel);
        }

        /// <summary>The parts' names with their values, left to right; a zero term drawn as nothing above and its name struck here.</summary>
        private void DrawEnergyTermNames(Rect rect, (string Name, float Value)[] terms, GUIStyle style)
        {
            float lx = rect.x;
            float sepW = style.CalcSize(new GUIContent(" · ")).x;
            for (int i = 0; i < terms.Length; i++)
            {
                bool zero = Mathf.Abs(terms[i].Value) <= RuleWaterfall.ZeroBand;
                string text = terms[i].Name + " " + (i == 0 ? terms[i].Value.ToString("0", CultureInfo.InvariantCulture) : terms[i].Value.ToString("+0;−0;0", CultureInfo.InvariantCulture));
                float w = style.CalcSize(new GUIContent(text)).x;
                if (lx + w > rect.xMax) { break; }   // a cell too narrow for its names keeps what fits, never clips a name mid-glyph
                PoliSimWidgets.MeasuredLabel(new Rect(lx, rect.y, w, rect.height), text, style);
                if (zero) { PoliSimTheme.Rule(new Rect(lx, rect.y + rect.height * 0.55f, w, 1f), PoliSimTheme.TextMuted); }
                lx += w;
                if (i < terms.Length - 1) { PoliSimWidgets.MeasuredLabel(new Rect(lx, rect.y, sepW, rect.height), " · ", style); lx += sepW; }
            }
        }

        // ---- the zones: Sweden's four as a small multiple with the links in the gutters; any other country its one zone ----------
        private float EnergyZonesRowHeight(float nameH, float capH, float srcH, bool sweden)
        {
            GUIStyle small = DeskCaption(7.5f, PoliSimTheme.TextMuted);
            float smallH = Mathf.Max(capH, Mathf.Ceil(small.CalcSize(new GUIContent("6,704 ⁄ 7,300 · BINDS")).y));
            float nameCol = StatsUnit(2f) + nameH + capH + srcH;
            float cells = StatsUnit(4f) + capH + StatsUnit(26f) + StatsUnit(2f) + capH * 2f + StatsUnit(2f) + smallH;
            return Mathf.Max(nameCol, cells) + StatsUnit(6f);
        }

        private void DrawEnergyZonesRow(float[] x, float y, float pad, PlateStyles styles, EnergyMarket.Result r, CountryId id, string marketUnit, bool sweden)
        {
            PoliSimWidgets.MeasuredLabel(new Rect(x[0] + pad, y + StatsUnit(2f), x[1] - x[0] - pad, styles.NameH), "The zones", styles.Name);
            PoliSimWidgets.MeasuredLabel(new Rect(x[0] + pad, y + StatsUnit(2f) + styles.NameH, x[1] - x[0] - pad, styles.CapH), marketUnit + " · BASE · MID · PEAK PER ZONE", styles.Caption);
            PoliSimWidgets.MeasuredLabel(new Rect(x[0] + pad, y + StatsUnit(2f) + styles.NameH + styles.CapH, x[1] - x[0] - pad, styles.SrcH),
                sweden ? "LINKS IN THE GUTTERS THEY JOIN · PEAK FLOW ⁄ NTC" : "THE CLEARING · SEEDED LOADS", styles.Source);
            float zonesH = EnergyZonesRowHeight(styles.NameH, styles.CapH, styles.SrcH, sweden);
            if (r.Zones != null && r.Zones.Length > 0)
            {
                float left = x[1] + pad, right = x[x.Length - 2] - pad;
                int zones = r.Zones.Length;
                float cellW = (right - left) / Mathf.Max(1, zones);
                float barsTop = y + StatsUnit(4f) + styles.CapH;
                float barsH = StatsUnit(26f);
                GUIStyle zoneLabel = DeskCaption(8f, PoliSimTheme.TextSecondary, true);
                float zoneHeadH = Mathf.Max(styles.CapH, Mathf.Ceil(DeskCaptionHeight(zoneLabel)));
                GUIStyle small = DeskCaption(7.5f, PoliSimTheme.TextMuted);
                GUIStyle smallSplit = DeskCaption(7.5f, PoliSimTheme.Caution);
                GUIStyle gutter = DeskCaption(7.5f, PoliSimTheme.TextMuted, false, TextAnchor.MiddleCenter);
                GUIStyle gutterBinds = DeskCaption(7.5f, PoliSimTheme.Caution, true, TextAnchor.MiddleCenter);
                float smallH = Mathf.Max(styles.CapH, Mathf.Ceil(small.CalcSize(new GUIContent("6,704 ⁄ 7,300 · BINDS")).y));
                // one price scale across the cells, so a zone's dearness is read by height, not by digits
                double maxPrice = 1.0;
                for (int z = 0; z < zones; z++) { for (int b = 0; b < r.Zones[z].Length; b++) { maxPrice = Math.Max(maxPrice, r.Zones[z][b].Price); } }
                // which zones a binding link splits - their prices print in Caution either side of the mark
                var split = new bool[zones];
                if (r.Links != null)
                {
                    foreach (EnergyMarket.LinkResult link in r.Links)
                    {
                        bool binds = link.Binding[0] || link.Binding[1] || link.Binding[2];
                        if (!binds || r.ZoneNames == null) { continue; }
                        int a = Array.IndexOf(r.ZoneNames, link.From), c = Array.IndexOf(r.ZoneNames, link.To);
                        if (a >= 0) { split[a] = true; }
                        if (c >= 0) { split[c] = true; }
                    }
                }
                float priceY = barsTop + barsH + StatsUnit(2f);
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
                    PoliSimWidgets.MeasuredLabel(new Rect(cx, priceY, cellW - pad, styles.CapH), priceText.ToString(), split[z] ? smallSplit : small);
                    PoliSimWidgets.MeasuredLabel(new Rect(cx, priceY + styles.CapH, cellW - pad, styles.CapH), loadText + " GW", small);
                }
                float gutterY = priceY + styles.CapH * 2f + StatsUnit(2f);
                if (r.Links != null && r.Links.Length > 0 && r.ZoneNames != null)
                {
                    // each link in the gutter it joins: a hairline through the bars' lane, PEAK FLOW ⁄ NTC beneath; a binding link a Caution mark and the word
                    foreach (EnergyMarket.LinkResult link in r.Links)
                    {
                        int a = Array.IndexOf(r.ZoneNames, link.From), c = Array.IndexOf(r.ZoneNames, link.To);
                        if (a < 0 || c < 0) { continue; }
                        float bx = left + Math.Max(a, c) * cellW - pad * 0.5f;
                        bool binds = link.Binding[0] || link.Binding[1] || link.Binding[2];
                        if (binds) { PoliSimTheme.Rule(new Rect(bx - 1f, barsTop - StatsUnit(2f), 2f, barsH + StatsUnit(4f)), PoliSimTheme.Caution); }
                        else { PoliSimTheme.Rule(new Rect(bx - 0.5f, barsTop, 1f, barsH), PoliSimTheme.RuleLight); }
                        string figure = string.Format(CultureInfo.InvariantCulture, "{0:N0} ⁄ {1:N0}{2}", Math.Abs(link.FlowMw[2]), link.CapacityMw[2], binds ? " · BINDS" : "");
                        float w = Mathf.Min(cellW - pad, gutter.CalcSize(new GUIContent(figure)).x + pad);
                        PoliSimWidgets.MeasuredLabel(new Rect(bx - w * 0.5f, gutterY, w, smallH), figure, binds ? gutterBinds : gutter);
                    }
                }
                else
                {
                    string note = id == CountryId.Italy ? "ONE ZONE · THE REAL MARKET HAS SEVEN: NORD · CNOR · CSUD · SUD · CALA · SICI · SARD" : id == CountryId.USA ? "ONE ZONE · THE REAL GRID IS THREE INTERCONNECTIONS: EASTERN · WESTERN · ERCOT" : "ONE ZONE · THE NEIGHBOURS ARE EXOGENOUS";
                    PoliSimWidgets.MeasuredLabel(new Rect(left, gutterY, right - left, smallH), note, small);
                }
            }
            // 15a: load growth is absent, and its absence sits beside the load it would move
            DrawEnergyGapRow(x, y + zonesH, pad, "Load growth", "GWh PER YEAR", AbsentLoadGrowth);
        }

        // ---- the water value, aligned under the rule row's three blocks: that row's water part, read by column -------------------
        private float EnergyWaterRowHeight(float nameH, float capH, float srcH)
        {
            return Mathf.Max(StatsUnit(2f) + nameH + capH + srcH, StatsUnit(2f) + capH + StatsUnit(16f) + capH) + StatsUnit(6f);
        }

        private void DrawEnergyWaterRow(float[] x, float y, float pad, PlateStyles styles, EnergyMarket.Result r, string marketUnit)
        {
            PoliSimWidgets.MeasuredLabel(new Rect(x[0] + pad, y + StatsUnit(2f), x[1] - x[0] - pad, styles.NameH), "Water value", styles.Name);
            PoliSimWidgets.MeasuredLabel(new Rect(x[0] + pad, y + StatsUnit(2f) + styles.NameH, x[1] - x[0] - pad, styles.CapH), marketUnit + " · PER BLOCK · UNDER THE RULE ROW'S THREE BLOCKS", styles.Caption);
            PoliSimWidgets.MeasuredLabel(new Rect(x[0] + pad, y + StatsUnit(2f) + styles.NameH + styles.CapH, x[1] - x[0] - pad, styles.SrcH), "THE RESERVOIRS' OPPORTUNITY COST · SLOPE AUTHORED", styles.Source);
            float left = x[1] + pad, right = x[x.Length - 2] - pad;
            float cellW = (right - left) / 3f;
            GUIStyle figure = DeskCaption(11f, PoliSimTheme.TextPrimary, true);
            GUIStyle blockLabel = DeskCaption(7.5f, PoliSimTheme.TextMuted);
            float figH = Mathf.Ceil(DeskCaptionHeight(figure));
            for (int b = 0; b < 3; b++)
            {
                float cx = left + b * cellW;
                PoliSimWidgets.MeasuredLabel(new Rect(cx, y + StatsUnit(2f), cellW - pad, styles.CapH), BlockNames[b], blockLabel);
                PoliSimWidgets.MeasuredLabel(new Rect(cx, y + StatsUnit(2f) + styles.CapH, cellW - pad, Mathf.Max(figH, StatsUnit(16f))), r.WaterValue[b].ToString("0", CultureInfo.InvariantCulture), figure);
            }
        }

        // ---- the two ledgers as bridges: 13b's painter twice on ONE scale, one leader joining the figure they share ----------------
        private float EnergyBridgesRowHeight(float nameH, float capH, float srcH)
        {
            float bridge = nameH + capH + StatsUnit(44f) + capH + StatsUnit(6f);
            return bridge * 2f + StatsUnit(16f);   // the leader's band between the two
        }

        private void DrawEnergyBridgesRow(float[] x, float y, float pad, PlateStyles styles, EnergyLedger.Book book)
        {
            float left = x[1] + pad, right = x[x.Length - 2] - pad;
            float rowH = styles.NameH + styles.CapH + StatsUnit(44f) + styles.CapH + StatsUnit(6f);
            float leaderH = StatsUnit(16f);
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
            // one scale: the larger excursion fills the lane, the other stops at its own figure on the same pixels per unit
            AttributionBridge.Geometry g1 = null, g2 = null;
            string fail = null;
            try { g1 = AttributionBridge.BuildFrom(0.0, book.WholesaleOutlay, system); g2 = AttributionBridge.BuildFrom(0.0, book.Gap, incidence); }
            catch (InvalidOperationException e) { fail = e.Message; }
            float usable = StatsUnit(44f) - 8f;
            double excursion = 1e-9;
            if (g1 != null) { excursion = Math.Max(excursion, g1.HighPoints - g1.LowPoints); }
            if (g2 != null) { excursion = Math.Max(excursion, g2.HighPoints - g2.LowPoints); }
            float pixelsPerUnit = (float)(usable / excursion);
            Rect box1 = DrawOneEnergyBridge(x, y, pad, styles, left, right, "System cost", EnergyLedger.BookCurrency + " BN · COST + RENT = OUTLAY",
                string.Format(CultureInfo.InvariantCulture, "OUTLAY {0:N1} BN · RENT {1:N1} BN, PRINTED", book.WholesaleOutlay, book.InframarginalRent), g1, fail, pixelsPerUnit);
            float y2 = y + rowH + leaderH;
            Rect box2 = DrawOneEnergyBridge(x, y2, pad, styles, left, right, "Incidence", EnergyLedger.BookCurrency + " BN · PAID, THEN RECEIVED · ONE SCALE WITH THE ROW ABOVE",
                string.Format(CultureInfo.InvariantCulture, "{0:N1} BN PAID · {1:N1} BN RECEIVED · GAP {2:0.0}", book.PaidTotal, book.ReceivedTotal, book.Gap), g2, fail, pixelsPerUnit);
            if (g1 == null || g2 == null || box1.width <= 0f || box2.width <= 0f) { return; }
            // the leader: the outlay one ledger closes on is the generators' receipt the other pays out - one figure, joined
            int slots1 = g1.Steps.Count + 2, slots2 = g2.Steps.Count + 2;
            float closeX = box1.xMax - (box1.width / slots1) * 0.5f;
            int genIndex = 0;
            for (int i = 0; i < g2.Steps.Count; i++) { if (g2.Steps[i].Abbreviation == "GEN") { genIndex = i + 1; break; } }
            float genX = box2.x + (box2.width / slots2) * (genIndex + 0.5f);
            float fromY = box1.yMax + styles.CapH + StatsUnit(2f);
            float toY = box2.y - StatsUnit(1f);
            float midY = fromY + (toY - fromY) * 0.5f;
            Color leaderInk = PoliSimTheme.TextSecondary;
            for (float yy = fromY; yy < midY; yy += 4f) { PoliSimTheme.Rule(new Rect(closeX - 0.5f, yy, 1f, Mathf.Min(2f, midY - yy)), leaderInk); }
            for (float yy = midY; yy < toY; yy += 4f) { PoliSimTheme.Rule(new Rect(genX - 0.5f, yy, 1f, Mathf.Min(2f, toY - yy)), leaderInk); }
            float lx0 = Mathf.Min(closeX, genX), lx1 = Mathf.Max(closeX, genX);
            DrawDashedRule(new Rect(lx0, midY - 0.5f, lx1 - lx0, 1f), leaderInk, 2f, 2f);
            GUIStyle leaderLabel = DeskCaption(7f, PoliSimTheme.TextSecondary, false, TextAnchor.MiddleCenter);
            string leader = string.Format(CultureInfo.InvariantCulture, "THE SAME {0:N1} - WHAT THE MARKET COST IS WHAT THE GENERATORS RECEIVED", book.WholesaleOutlay);
            float lw = leaderLabel.CalcSize(new GUIContent(leader)).x + pad * 2f;
            float lcx = (lx0 + lx1) * 0.5f;
            var labelRect = new Rect(Mathf.Clamp(lcx - lw * 0.5f, left, right - lw), midY - styles.CapH * 0.5f, lw, styles.CapH);
            PoliSimTheme.Rule(labelRect, PoliSimTheme.Card);
            PoliSimWidgets.MeasuredLabel(labelRect, leader, leaderLabel);
        }

        private Rect DrawOneEnergyBridge(float[] x, float y, float pad, PlateStyles styles, float left, float right, string name, string caption, string source,
            AttributionBridge.Geometry g, string fail, float pixelsPerUnit)
        {
            PoliSimWidgets.MeasuredLabel(new Rect(x[0] + pad, y + StatsUnit(2f), x[1] - x[0] - pad, styles.NameH), name, styles.Name);
            PoliSimWidgets.MeasuredLabel(new Rect(x[0] + pad, y + StatsUnit(2f) + styles.NameH, x[1] - x[0] - pad, styles.CapH * 2f), caption, styles.Caption);
            PoliSimWidgets.MeasuredLabel(new Rect(x[0] + pad, y + StatsUnit(2f) + styles.NameH + styles.CapH * 2f, x[1] - x[0] - pad, styles.SrcH), source, styles.Source);
            if (g == null)
            {
                PoliSimWidgets.MeasuredLabel(new Rect(left, y + StatsUnit(4f), right - left, styles.CapH * 2f), "THE LINES DO NOT CLOSE · " + (fail ?? "").ToUpperInvariant(), DeskCaption(8f, PoliSimTheme.Bad));
                return new Rect(left, y, 0f, 0f);
            }
            var box = new Rect(left, y + StatsUnit(3f), right - left, StatsUnit(44f));
            if (Event.current.type == EventType.Repaint && box.width >= 8f)
            {
                Texture2D bridge = CanvasPaint.Bridge(Mathf.RoundToInt(box.width), Mathf.RoundToInt(box.height), g, pixelsPerUnit, PoliSimTheme.Card,
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
                    step.Abbreviation + " " + step.Points.ToString(Math.Abs(step.Points) >= 100.0 ? "+0;-0;0" : "+0.0;-0.0;0", CultureInfo.InvariantCulture), slotLabel);   // EN-7a: a hundred and up in whole billions - "SUPPLIERS -408.2" overflowed its slot at 1280
            }
            PoliSimWidgets.MeasuredLabel(new Rect(box.x + slotW * (slots - 1), labelY, slotW, styles.CapH), g.ClosePoints.ToString("0.0", CultureInfo.InvariantCulture), slotLabel);
            return box;
        }
    }
}
