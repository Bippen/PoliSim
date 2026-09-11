using System;
using System.Collections.Generic;
using PoliSim.Data;
using PoliSim.Data.Generated;

namespace PoliSim.Simulation
{
    /// <summary>
    /// THE ENERGY MARKET, STAGE 3 - ONE CLEARING PER BLOCK PER ZONE (EN-3, 2026-09-11; POLISIM_ENERGY_SPECLET.md S5-S6 ruled §454; the
    /// data ENERGY_LAYER_SPINE.md §5 and EnergyData/dispatch_levels_2023.csv, variable_costs_2023.csv). A merit order over variable cost -
    /// fuel over efficiency, plus the ETS carbon price and the carbon tax's points above its seed on the emission factor, plus variable O&M -
    /// serves each block's residual demand after the resource-driven output (nuclear at the record's availability, hydro, wind, solar and the
    /// firm plants at their 2023 levels), constrained by each fossil category's dependable capacity; the area price is the marginal unit's
    /// cost, a scarcity term rises as the residual approaches the dependable capacity, and NO PRICE IS CLAMPED: the lowest offer on every
    /// curve is the curtailment offer, and a negative one prices a block negative the day a support scheme bids one.
    ///
    /// <para><b>Sweden's four zones</b> clear on coincident hours along the chain SE1 → SE2 → SE3 → SE4 against the dated NTCs (§455): each
    /// zone's balance is its settlement consumption plus its exogenous external export less its own supply; the flows the balances require
    /// are carried by the links up to their capacity, a link that cannot carry them BINDS and splits the price, and the congestion rent is
    /// the flow times the split. Sweden has no dispatchable fossil fleet worth the name (its fossil thermal is heat-led CHP inside the firm
    /// class), so its uncongested price is the WATER VALUE - the price of the electricity its exports displace - proxied by the block prices
    /// of the two connected markets this model clears, Germany and Poland, weighted by SE4's capacity to each (a CONVENTION, stated: Norway,
    /// Finland and Denmark are outside the six). A reservoir dispatch of the hydro is the open follow-up.</para>
    ///
    /// <para><b>The seed calibration.</b> An annual merit order in three blocks cannot reproduce a year's hourly reality unaided; the model
    /// carries, per country and fossil category, a CALIBRATION ADDER (currency per MWh) solved once at the seed so that the seed dispatch
    /// reproduces 2023's shares of coal, gas and oil in the fossil total - the shadow of the constraints the blocks do not see (must-run
    /// contracts, heat-led CHP, ramping, location). The adders are printed by the dump and held constant; what moves the dispatch afterwards
    /// is the carbon tax's points above the seed, the price level (nominal with nominal, P5-B6) and nothing else until later stages.</para>
    ///
    /// <para><b>The writer changes hands here (S4's deviation closed).</b> The environment family's power figure is written by this dispatch:
    /// the fossil generation times each category's emission factor times its main-activity share, plus the country's seed residual (heat
    /// plants, CHP heat, refineries - EnergyLayer's decomposition, fixed at the seed by method), over the population. The family's power
    /// elasticity is retired; its transport half is untouched.</para>
    /// </summary>
    public static class EnergyMarket
    {
        /// <remarks>SOURCED: ACER Decision No 04/2017 on the harmonised maximum clearing price for single day-ahead coupling - 4 000 EUR/MWh; the ceiling of the scarcity term.</remarks>
        public const float MaxClearingPrice = 4000f;
        /// <remarks>[AUTHORED-DRAFT] - the share of dependable fossil capacity in use at which the scarcity term begins to rise toward the ceiling (quadratic to the ceiling at full use).</remarks>
        public const float ScarcityOnset = 0.9f;
        /// <remarks>CONVENTION - the lowest offer on every curve at stage 3: no support scheme bids below zero yet, so no clamp exists and a negative offer prices a block negative the day one does.</remarks>
        public const float CurtailmentOffer = 0f;
        /// <remarks>SOURCED by ruling (EN-4c, 2026-09-11, COMPLETED.md §464) - the carbon tax's rate IS the country's currency per tonne of CO₂, the statutory meaning; one point of the line is one unit of that currency per tonne, and the budget's revenue reads the same rate on the same tonnes (TaxBases.RevenueAtRate). Before the ruling this was the dispatch's own convention against a line that stated no unit.</remarks>
        public const float CarbonTaxPointPerTonne = 1f;
        /// <remarks>CONVENTION - the calibration's tolerance on each fossil category's share of the fossil total: one point, §342's "within a point"; the tranche resolution below is chosen so a single tranche's flip moves a share by less.</remarks>
        public const float CalibrationTolerance = 0.01f;
        /// <remarks>CONVENTION - the calibration adder's search bound, currency per MWh, either sign; a category that needs the bound is reported by the check as uncalibrated.</remarks>
        public const float AdderBound = 300f;
        /// <remarks>CONVENTION - coordinate rounds of the calibration; each is a bisection per category.</remarks>
        public const int CalibrationRounds = 12;
        /// <remarks>[AUTHORED-DRAFT] - a fleet is not one plant: each fossil category's dependable capacity is offered in tranches whose cost runs from (1 − spread) to (1 + spread) of the category's mean, the efficiency dispersion of a real fleet (a coal fleet spans roughly 33 to 46 per cent, ± a sixth on fuel and carbon). Without it an annual merit order is winner-take-all and no calibration can reach an interior share.</remarks>
        public const float FleetSpread = 0.25f;
        /// <remarks>CONVENTION - tranches per category, the supply curve's resolution: a share moves in steps of one tranche's energy, and 200 keeps the largest fleet's step (Germany's coal, 160 MW over the mid block's hours) under the calibration's point.</remarks>
        public const int Tranches = 200;

        /// <summary>
        /// THE FITTED PARAMETERS, COUNTED (§461). The calibration adders are FITTED: one per non-dominant fossil category with a fleet, solved at the seed
        /// against EnergyData/dispatch_levels_2023.csv so the seed dispatch reproduces 2023's coal / gas / oil shares of the fossil total - Germany 2
        /// (gas, oil), France 2 (coal, oil), Italy 2 (coal, oil), Poland 2 (gas, oil), the USA 2 (coal, oil), Sweden 0: TEN free parameters, and no
        /// other quantity in this class is fitted. Everything else is sourced, derived, authored or a convention and says which. A model that
        /// reproduces its seed year is not yet a model that responds: EnergyLayerCheck's gate 7 holds the out-of-sample response - the adders fixed,
        /// a carbon-price step must move coal down, gas up and the peak price up by the amount the merit order gives, Poland's twenty points the
        /// reference read off the first landing.
        /// </summary>
        public const int FittedParametersPerCountryWithFleet = 2;   // FITTED, counted: the non-dominant fossil categories with a fleet (read off the calibration: 2 for each of the five, 0 for Sweden)
        /// <remarks>FITTED-REFERENCE, read off `bar334_en4` (2026-09-11, §461): Poland's response to twenty points of carbon tax at the seed - twenty ZLOTY per tonne, €4.40 - with the adders fixed: coal's share of the fossil total down 0.004, gas's up 0.004, the peak price up 2.8 €/MWh - the amount the merit order gives. (§460's first reading, 0.016 / 0.016 / 12.1, priced the points as euro; the unit error the standing check found.) A change here is a change of the model, to be explained.</remarks>
        public const float ReferenceCoalDrop = 0.004f, ReferenceGasRise = 0.004f, ReferencePeakPriceRise = 2.8f;
        /// <remarks>CONVENTION - the response reference's slack: two thousandths of a share and one currency unit per MWh; the tranche resolution's own step.</remarks>
        public const float ReferenceShareSlack = 0.002f, ReferencePriceSlack = 1.0f;
        /// <remarks>CONVENTION - the carbon-price step of the standing response check, in the line's points (currency per tonne).</remarks>
        public const float ResponseStepPoints = 20f;

        /// <remarks>CONVENTION - the indices of EnergyLayerData.DispatchCategories, asserted against the catalog at first use.</remarks>
        public const int Coal = 0, Gas = 1, Oil = 2, Nuclear = 3, Hydro = 4, Wind = 5, Solar = 6, Firm = 7;
        /// <remarks>CONVENTION - the merit order's categories are the first three.</remarks>
        public const int FossilCount = 3;

        public sealed class BlockResult
        {
            public string Zone; public int Block;
            public double DemandMw, MustRunMw, ResidualMw, CurtailedMw, UnservedMw, Price, Scarcity, DependableMw;
            public readonly double[] FossilMw = new double[FossilCount];
            public readonly double[] MarginalCost = new double[FossilCount];
        }

        public sealed class LinkResult
        {
            public string From, To;
            public readonly double[] FlowMw = new double[3], CapacityMw = new double[3], PriceNorth = new double[3], PriceSouth = new double[3];
            public readonly bool[] Binding = new bool[3];
            /// <summary>The congestion rent over the year - the carried flow times the price split, summed over the blocks' hours.</summary>
            public double RentPerYear;
        }

        public sealed class Result
        {
            public CountryId Country;
            public BlockResult[][] Zones;        // [zone][block]
            public string[] ZoneNames;
            public LinkResult[] Links;           // Sweden only
            public double[] WaterValue;          // Sweden only, per block
            /// <summary>Sweden only, per block: the chain's national unbalance, MW - the four balances' sum, which the exogenous exchange and the unmodelled losses leave a few tens of MW from zero; printed, never priced.</summary>
            public double[] ChainUnbalanceMw;
            public readonly double[] AnnualGwh = new double[8];
            public double DerivedCo2Mt;
            public double[] Adders;
            public double MaxCalibrationGap;
        }

        private static readonly Dictionary<CountryId, double[]> AdderCache = new Dictionary<CountryId, double[]>();
        private static readonly Dictionary<CountryId, double> GapCache = new Dictionary<CountryId, double>();
        private static double[] _waterValue;   // set per turn by BeginTurn; null outside a turn
        private static double[] _waterValueSeed;   // the seed's, computed once (WaterValueAtSeed)

        // ---- the catalog, by country and category ----------------------------------------------------------
        private static int CostIndex(int category) => category;   // coal, gas, oil are the first three of both lists
        private static double NuclearAvailability(CountryId id) => id == CountryId.Germany ? 0.0 : 1.0;   // §457: CLOSED BY LAW - the record's installed stock, unavailable (Atomgesetz, 15 April 2023)

        /// <summary>The record's dependable capacity for a fossil category, MW: the record's capacity times the availability for coal and gas; for oil the record's "other" label times oil's share of that label's 2023 output - an apportionment, stated.</summary>
        public static double RecordDependableMw(CountryId id, int category)
        {
            int ci = EnergyLayer.Index(id);
            double availability = EnergyLayerData.Availability[ci][CostIndex(category)];
            if (category == Coal) { return EnergyLayer.CapacityMw(id, 0) * availability; }
            if (category == Gas) { return EnergyLayer.CapacityMw(id, 1) * availability; }
            double otherCapacity = EnergyLayer.CapacityMw(id, 6);
            double otherGeneration = EnergyLayer.GenerationGwh(id, 6);
            double oilGeneration = AnnualLevelGwh(EnergyLayer.ZoneIndex(EnergyLayer.Code(id)), Oil);
            double share = otherGeneration > 0 ? Math.Min(1.0, oilGeneration / otherGeneration) : 0.0;
            return otherCapacity * share * availability;
        }

        /// <summary>The highest block level the category reached in 2023, MW - a fleet demonstrably capable of it.</summary>
        public static double PeakLevelMw(CountryId id, int category)
        {
            int zone = EnergyLayer.ZoneIndex(EnergyLayer.Code(id)); double peak = 0;
            for (int b = 0; b < 3; b++) { peak = Math.Max(peak, EnergyLayerData.DispatchLevelMw[zone][b][category]); }
            return peak;
        }

        /// <summary>
        /// The fossil category's dependable capacity, MW: the record's, or the category's own 2023 peak level where the record's fuel grouping does not carry
        /// it - France's and Italy's coal plants sit in Eurostat's multi-fuel groups (19 and 36 MW under "coal" against 0.5 and 2.1 GW of coal output) - the
        /// definition kept, the shortfall made up from the output the plants demonstrably produced and named as such (<see cref="DependableSource"/>).
        /// </summary>
        public static double DependableMw(CountryId id, int category) => Math.Max(RecordDependableMw(id, category), PeakLevelMw(id, category));
        public static string DependableSource(CountryId id, int category) => RecordDependableMw(id, category) >= PeakLevelMw(id, category) ? "record" : "2023 output";

        /// <summary>
        /// The inflexible part of a fossil category: what it produced in the trough decile of the load runs in every block - heat-led CHP, minimum loads,
        /// contracts - and is not dispatched; the merit order runs above it. [AUTHORED-DRAFT] as a rule; the level itself is the 2023 record's.
        /// </summary>
        public static double MustRunFossilMw(CountryId id, int category) => EnergyLayerData.DispatchLevelMw[EnergyLayer.ZoneIndex(EnergyLayer.Code(id))][0][category];

        private static double AnnualLevelGwh(int zone, int category)
        {
            double gwh = 0;
            for (int b = 0; b < 3; b++) { gwh += EnergyLayerData.DispatchLevelMw[zone][b][category] * EnergyLayerData.DispatchHours[zone][b] / 1000.0; }
            return gwh;
        }

        /// <summary>The marginal cost of a fossil category, currency per MWh: (fuel / efficiency + O&M) carried by the price level, plus (the ETS price carried + the tax's points above the seed) on the emission factor, plus the calibration adder - CARRIED BY THE PRICE LEVEL TOO since EN-5 (2026-09-11): the adder is the shadow of costs the blocks do not see (contracts, heat-led CHP, ramping, location), and a cost is nominal with nominal (P5-B6); held in seed currency it eroded in real terms and drifted the fossil shares and the real wholesale a little each year, which EN-5's B6 probe measured (§465). Infinite where the country has no such plant.</summary>
        public static double MarginalCost(CountryId id, int category, double priceIndex, double taxPointsAboveSeed, double adder)
        {
            (double fuelVom, double ets, double tax) = CostParts(id, category, priceIndex, taxPointsAboveSeed);
            return double.IsInfinity(fuelVom) ? double.PositiveInfinity : fuelVom + ets + tax + adder * priceIndex;
        }

        /// <summary>The marginal cost's parts, currency per MWh - fuel and O&amp;M carried by the price level; the ETS carried; the tax's points above the seed on the emission factor - ONE formula the clearing and the ledger both read. Fuel is infinite where the country has no such plant.</summary>
        public static (double FuelVom, double Ets, double Tax) CostParts(CountryId id, int category, double priceIndex, double taxPointsAboveSeed)
        {
            int ci = EnergyLayer.Index(id); int k = CostIndex(category);
            double efficiency = EnergyLayerData.Efficiency[ci][k];
            if (efficiency <= 0) { return (double.PositiveInfinity, 0.0, 0.0); }
            double fuel = (EnergyLayerData.FuelPerMwhTh[ci][k] / efficiency + EnergyLayerData.VomPerMwh[ci][k]) * priceIndex;
            double ef = EnergyLayerData.EmissionFactorTPerMwh[ci][k];
            // the tax's points are the country's currency per tonne; the market's costs are in euro for the five (dollars for the USA) - the ECB rate bridges the krona's and the zloty's points (§463: the first landing added zloty points to euro costs)
            double taxInMarketCurrency = taxPointsAboveSeed * CarbonTaxPointPerTonne / Math.Max(1e-6, EnergyLayerData.NationalPerMarketCurrency[ci]);
            return (fuel, EnergyLayerData.EtsPerT[ci] * priceIndex * ef, taxInMarketCurrency * ef);
        }

        /// <summary>
        /// ONE CLEARING: the block's demand less the resource-driven output is the residual; the fossil categories serve it in merit order up
        /// to their dependable capacity; the price is the marginal unit's cost, raised by the scarcity term as the residual approaches the
        /// dependable total, the ceiling when it exceeds it; a residual at or below zero is curtailment at the lowest offer. Public and pure
        /// so a probe can hand it any offers - including a negative one - and read what it prices.
        /// </summary>
        public static BlockResult ClearBlock(double demandMw, double mustRunMw, double[] marginalCost, double[] dependableMw, double curtailmentOffer) => ClearBlock(demandMw, mustRunMw, marginalCost, dependableMw, curtailmentOffer, 0f, 1);

        /// <summary>The same clearing with each category offered in <paramref name="tranches"/> steps spread ± <paramref name="spread"/> around its mean cost - the fleet's dispersion; a probe passes 0 and 1 for one plant per category.</summary>
        public static BlockResult ClearBlock(double demandMw, double mustRunMw, double[] marginalCost, double[] dependableMw, double curtailmentOffer, float spread, int tranches)
        {
            var r = new BlockResult { DemandMw = demandMw, MustRunMw = mustRunMw };
            r.ResidualMw = demandMw - mustRunMw;
            for (int k = 0; k < FossilCount; k++) { r.MarginalCost[k] = marginalCost[k]; r.DependableMw += double.IsInfinity(marginalCost[k]) ? 0 : dependableMw[k]; }
            if (r.ResidualMw <= 0)
            {
                r.CurtailedMw = -r.ResidualMw;
                r.Price = curtailmentOffer;
                return r;
            }
            tranches = Math.Max(1, tranches);
            var offers = new List<(double Cost, double Mw, int Category)>();
            for (int k = 0; k < FossilCount; k++)
            {
                if (double.IsInfinity(marginalCost[k]) || dependableMw[k] <= 0) { continue; }
                for (int i = 0; i < tranches; i++)
                {
                    double factor = tranches == 1 ? 1.0 : 1.0 + spread * (2.0 * i + 1.0 - tranches) / tranches;
                    offers.Add((marginalCost[k] * factor, dependableMw[k] / tranches, k));
                }
            }
            offers.Sort((a, b) => a.Cost != b.Cost ? a.Cost.CompareTo(b.Cost) : a.Category.CompareTo(b.Category));
            double left = r.ResidualMw; double price = curtailmentOffer; bool any = false;
            foreach ((double cost, double mw, int k) in offers)
            {
                if (left <= 0) { break; }
                double take = Math.Min(left, mw);
                r.FossilMw[k] += take; left -= take; price = cost; any = true;
            }
            if (left > 1e-9)
            {
                r.UnservedMw = left;
                r.Scarcity = MaxClearingPrice - price;
                r.Price = MaxClearingPrice;
                return r;
            }
            if (!any) { r.Price = curtailmentOffer; return r; }
            double ratio = r.DependableMw > 0 ? r.ResidualMw / r.DependableMw : 0;
            if (ratio > ScarcityOnset)
            {
                double x = (ratio - ScarcityOnset) / (1.0 - ScarcityOnset);
                r.Scarcity = Math.Max(0, MaxClearingPrice - price) * x * x;
            }
            r.Price = price + r.Scarcity;
            return r;
        }

        private static double MustRun(CountryId id, int zone, int block)
        {
            double[] level = EnergyLayerData.DispatchLevelMw[zone][block];
            return level[Nuclear] * NuclearAvailability(id) + level[Hydro] + level[Wind] + level[Solar] + level[Firm];
        }

        private static double[] Costs(CountryId id, double priceIndex, double taxDelta, double[] adders)
        {
            var mc = new double[FossilCount];
            for (int k = 0; k < FossilCount; k++) { mc[k] = MarginalCost(id, k, priceIndex, taxDelta, adders[k]); }
            return mc;
        }

        /// <summary>The flexible capacity per fossil category - the dependable less the inflexible floor.</summary>
        private static double[] Caps(CountryId id)
        {
            var caps = new double[FossilCount];
            for (int k = 0; k < FossilCount; k++) { caps[k] = Math.Max(0.0, DependableMw(id, k) - MustRunFossilMw(id, k)); }
            return caps;
        }

        private static double[] Floors(CountryId id)
        {
            var floors = new double[FossilCount];
            for (int k = 0; k < FossilCount; k++) { floors[k] = MustRunFossilMw(id, k); }
            return floors;
        }

        /// <summary>A country's block: the resource-driven output and the fossil floors are must-run; the merit order serves the rest with the flexible capacity; the floors are added back to the dispatch.</summary>
        private static BlockResult ClearCountryBlock(CountryId id, int zone, int block, double[] mc, double[] caps, double[] floors)
        {
            double floorSum = 0; for (int k = 0; k < FossilCount; k++) { floorSum += floors[k]; }
            BlockResult r = ClearBlock(EnergyLayerData.DispatchDemandMw[zone][block], MustRun(id, zone, block) + floorSum, mc, caps, CurtailmentOffer, FleetSpread, Tranches);
            for (int k = 0; k < FossilCount; k++) { r.FossilMw[k] += floors[k]; }
            // CONVENTION: a block with no flexible dispatch but a running fossil floor is priced by that floor's cheapest tranche - the inflexible fleet's own
            // lowest cost - not by the curtailment offer, which is the price only when nothing fossil runs at all.
            if (r.ResidualMw <= 0 && floorSum > 0)
            {
                double cheapest = double.PositiveInfinity;
                for (int k = 0; k < FossilCount; k++) { if (floors[k] > 0 && !double.IsInfinity(mc[k])) { cheapest = Math.Min(cheapest, mc[k] * (1.0 - FleetSpread)); } }
                if (!double.IsInfinity(cheapest)) { r.Price = cheapest; }
            }
            return r;
        }

        // ---- the country ------------------------------------------------------------------------------------
        /// <summary>Clear a country for a carbon tax rate: its single zone, or Sweden's four along the chain.</summary>
        public static Result Clear(Country country, float carbonTaxRate)
        {
            double priceIndex = Math.Max(0.0001f, country.State.PriceLevel);
            double taxDelta = carbonTaxRate - (country.Environment != null ? country.Environment.CarbonTaxRateSeed : 0f);
            return ClearAt(country.Id, priceIndex, taxDelta);
        }

        /// <summary>Clear at an explicit price index and tax delta (the calibration and the probes use the seed's: 1 and 0); Sweden at the turn's water value where a turn has set one, the seed's otherwise.</summary>
        public static Result ClearAt(CountryId id, double priceIndex, double taxDelta) => ClearAt(id, priceIndex, taxDelta, null);

        /// <summary>The SEED clearing - price index 1, the tax at its seed, and Sweden at the SEED's water value whatever turn state stands: the seed fits (the residual, the retail margins) and the seed gates read this, so a stale turn value from an earlier world in the same process cannot reach a seed figure (EN-4's first simulation bar found it: the ledger diagnostic ran after the market's ten-year worlds and fitted Sweden's margins against their last water value).</summary>
        public static Result ClearAtSeed(CountryId id) => ClearAt(id, 1.0, 0.0, WaterValueAtSeed());

        private static Result ClearAt(CountryId id, double priceIndex, double taxDelta, double[] waterValue)
        {
            var result = new Result { Country = id, Adders = Adders(id), MaxCalibrationGap = GapCache.TryGetValue(id, out double gap) ? gap : double.NaN };
            double[] mc = Costs(id, priceIndex, taxDelta, result.Adders);
            double[] caps = Caps(id);
            if (id == CountryId.Sweden) { ClearSweden(result, mc, caps, waterValue ?? _waterValue ?? WaterValueAtSeed()); }
            else
            {
                int zone = EnergyLayer.ZoneIndex(EnergyLayer.Code(id));
                double[] floors = Floors(id);
                result.ZoneNames = new[] { EnergyLayer.Code(id) };
                result.Zones = new[] { new BlockResult[3] };
                for (int b = 0; b < 3; b++)
                {
                    BlockResult r = ClearCountryBlock(id, zone, b, mc, caps, floors);
                    r.Zone = result.ZoneNames[0]; r.Block = b; result.Zones[0][b] = r;
                    Accumulate(result, id, zone, b, r);
                }
            }
            return result;
        }

        private static void Accumulate(Result result, CountryId id, int zone, int block, BlockResult r)
        {
            int ci = EnergyLayer.Index(id);
            double hours = EnergyLayerData.DispatchHours[zone][block];
            double[] level = EnergyLayerData.DispatchLevelMw[zone][block];
            for (int k = 0; k < FossilCount; k++)
            {
                double gwh = r.FossilMw[k] * hours / 1000.0;
                result.AnnualGwh[k] += gwh;
                result.DerivedCo2Mt += gwh * EnergyLayerData.EmissionFactorTPerMwh[ci][k] * EnergyLayerData.MainShare[ci][k] / 1000.0;   // GWh × t/MWh = kt; /1000 = Mt
            }
            result.AnnualGwh[Nuclear] += level[Nuclear] * NuclearAvailability(id) * hours / 1000.0;
            result.AnnualGwh[Hydro] += level[Hydro] * hours / 1000.0;
            result.AnnualGwh[Wind] += level[Wind] * hours / 1000.0;
            result.AnnualGwh[Solar] += level[Solar] * hours / 1000.0;
            result.AnnualGwh[Firm] += level[Firm] * hours / 1000.0;
        }

        // ---- Sweden -------------------------------------------------------------------------------------------
        /// <summary>Set once per turn by the boundary: the water value Sweden's uncongested zones clear at - Germany's and Poland's block prices at their current rates, weighted by SE4's capacity to each (615 and 600 MW). Null outside a turn: the SEED's water value then stands (the same two markets at price index 1 and their seed rates), so a seed fit (EN-4's margins) reads the seed's price and not the curtailment offer.</summary>
        public static void BeginTurn(World world)
        {
            Country de = world.GetCountry(CountryId.Germany), pl = world.GetCountry(CountryId.Poland);
            if (de == null || pl == null) { _waterValue = null; return; }
            _waterValue = WaterValueOf(Clear(de, EnvironmentFamily.CarbonTaxRate(de)), Clear(pl, EnvironmentFamily.CarbonTaxRate(pl)));
        }

        private static double[] WaterValueOf(Result rde, Result rpl)
        {
            double wde = 615.0, wpl = 600.0;   // Svenska kraftnät's capacity-map text: SE4 → Germany 615 MW, SE4 → Poland 600 MW (EnergyData/external_links_se.csv)
            var water = new double[3];
            for (int b = 0; b < 3; b++) { water[b] = (rde.Zones[0][b].Price * wde + rpl.Zones[0][b].Price * wpl) / (wde + wpl); }
            return water;
        }

        /// <summary>The seed's water value - Germany's and Poland's seed clearings weighted as BeginTurn weights them; what Sweden clears at outside a turn.</summary>
        public static double[] WaterValueAtSeed()
        {
            if (_waterValueSeed == null) { _waterValueSeed = WaterValueOf(ClearAt(CountryId.Germany, 1.0, 0.0), ClearAt(CountryId.Poland, 1.0, 0.0)); }
            return _waterValueSeed;
        }

        public static void EndTurn() { _waterValue = null; }
        public static bool HasWaterValue => _waterValue != null;
        /// <summary>A new world begins with no turn state: WorldFactory calls it before the families seed, so nothing of an earlier world's last turn (a diagnostic's, a finished game's) stands when the next one is built. The calibration is the catalog's and stays.</summary>
        public static void ResetTurnState() { _waterValue = null; ProbeLinkCapacityScale = 1.0; }

        /// <summary>A probe's knob on the Swedish links' capacities (1 = the dated NTCs): EnergyLedgerDiagnostic scales them down to make a snitt bind and read the rent and its credit. Never set by the game.</summary>
        public static double ProbeLinkCapacityScale = 1.0;

        private static void ClearSweden(Result result, double[] mc, double[] caps, double[] waterValue)
        {
            string[] chain = EnergyLayer.SwedenZones;
            int n = chain.Length;
            result.ZoneNames = chain;
            result.Zones = new BlockResult[n][];
            result.WaterValue = new double[3];
            result.ChainUnbalanceMw = new double[3];
            result.Links = new LinkResult[n - 1];
            for (int k = 0; k < n - 1; k++) { result.Links[k] = new LinkResult { From = chain[k], To = chain[k + 1] }; }
            for (int z = 0; z < n; z++) { result.Zones[z] = new BlockResult[3]; }
            for (int b = 0; b < 3; b++)
            {
                double water = waterValue[b];
                result.WaterValue[b] = water;
                // each zone's own balance: consumption + external export served by its own supply; Sweden's fossil caps are its national tiny fleet, put in SE3's zone (Stockholm) - the only zone with thermal capacity of note
                var surplus = new double[n];
                for (int z = 0; z < n; z++)
                {
                    int zone = EnergyLayer.ZoneIndex(chain[z]);
                    double demand = EnergyLayerData.ZoneConsumptionBlockMw[zone][b] + EnergyLayerData.ZoneExternalExportMw[zone][b];
                    double mustRun = MustRun(CountryId.Sweden, zone, b);
                    var r = new BlockResult { Zone = chain[z], Block = b, DemandMw = demand, MustRunMw = mustRun, ResidualMw = demand - mustRun };
                    result.Zones[z][b] = r;
                    surplus[z] = mustRun - demand;
                    Accumulate(result, CountryId.Sweden, zone, b, r);
                }
                // the chain: cumulative surplus from the north is the flow each link must carry; a link that cannot BINDS and splits the price
                double cumulative = 0;
                var price = new double[n]; for (int z = 0; z < n; z++) { price[z] = water; }
                for (int k = 0; k < n - 1; k++)
                {
                    cumulative += surplus[k];
                    LinkResult link = result.Links[k];
                    double cap = ProbeLinkCapacityScale * (cumulative >= 0 ? EnergyLayer.LinkCapacityMw(chain[k], chain[k + 1]) : EnergyLayer.LinkCapacityMw(chain[k + 1], chain[k]));
                    link.FlowMw[b] = cumulative; link.CapacityMw[b] = cap;
                    link.Binding[b] = Math.Abs(cumulative) > cap;
                    if (link.Binding[b])
                    {
                        if (cumulative > 0) { for (int z = 0; z <= k; z++) { price[z] = CurtailmentOffer; } cumulative = cap; }   // a surplus locked north of the cut sells at the lowest offer
                        else { for (int z = 0; z <= k; z++) { price[z] = MaxClearingPrice; } cumulative = -cap; }                 // a deficit the link cannot fill is scarcity north of the cut
                    }
                }
                // the chain's end: the four balances' sum is the national unbalance the exogenous exchange and the unmodelled losses leave (tens of MW) - recorded, never priced
                result.ChainUnbalanceMw[b] = cumulative + surplus[n - 1];
                for (int z = 0; z < n; z++) { result.Zones[z][b].Price = price[z]; result.Zones[z][b].CurtailedMw = Math.Max(0, surplus[z]); }
                for (int k = 0; k < n - 1; k++)
                {
                    LinkResult link = result.Links[k];
                    link.PriceNorth[b] = price[k]; link.PriceSouth[b] = price[k + 1];
                    double carried = Math.Min(Math.Abs(link.FlowMw[b]), link.CapacityMw[b]);
                    link.RentPerYear += carried * Math.Abs(price[k + 1] - price[k]) * EnergyLayerData.DispatchHours[EnergyLayer.ZoneIndex(chain[k])][b];
                }
            }
            // Sweden's own fossil fleet (69 MW of coal, 17 of gas, the oil inside "other") is heat-led CHP and is not dispatched: the merit order's
            // costs and caps are computed for it like any country's and left unused here, stated - its fossil categories read zero in the annual figures.
            _ = mc; _ = caps;
        }

        // ---- the calibration ----------------------------------------------------------------------------------
        /// <summary>The country's calibration adders, solved once: the seed dispatch (price index 1, tax at its seed) reproduces 2023's coal / gas / oil shares of the fossil total within the tolerance, or as near as the bound allows - the gap is kept for the check.</summary>
        public static double[] Adders(CountryId id)
        {
            if (AdderCache.TryGetValue(id, out double[] cached)) { return cached; }
            var adders = new double[FossilCount];
            int zone = EnergyLayer.ZoneIndex(EnergyLayer.Code(id));
            var target = new double[FossilCount]; double total = 0;
            for (int k = 0; k < FossilCount; k++) { target[k] = AnnualLevelGwh(zone, k); total += target[k]; }
            double gap = 0;
            if (total > 0 && id != CountryId.Sweden)
            {
                for (int k = 0; k < FossilCount; k++) { target[k] /= total; }
                double[] caps = Caps(id); double[] floors = Floors(id);
                // the adders are relative: the DOMINANT category (2023's largest share) keeps an adder of zero, so the level of the price stays that fuel's own cost
                int dominant = 0; for (int k = 1; k < FossilCount; k++) { if (target[k] > target[dominant]) { dominant = k; } }
                for (int round = 0; round < CalibrationRounds; round++)
                {
                    for (int k = 0; k < FossilCount; k++)
                    {
                        if (k == dominant || double.IsInfinity(MarginalCost(id, k, 1.0, 0.0, 0.0)) || caps[k] <= 0) { continue; }
                        double lo = -AdderBound, hi = AdderBound;
                        for (int step = 0; step < 40; step++)
                        {
                            double mid = 0.5 * (lo + hi); adders[k] = mid;
                            double share = SeedShare(id, zone, k, adders, caps, floors);
                            if (share > target[k]) { lo = mid; } else { hi = mid; }   // a higher adder dispatches less
                        }
                        adders[k] = 0.5 * (lo + hi);
                    }
                }
                double[] shares = SeedShares(id, zone, adders, caps, floors);
                for (int k = 0; k < FossilCount; k++) { gap = Math.Max(gap, Math.Abs(shares[k] - target[k])); }
            }
            AdderCache[id] = adders; GapCache[id] = gap;
            return adders;
        }

        private static double[] SeedShares(CountryId id, int zone, double[] adders, double[] caps, double[] floors)
        {
            double[] mc = Costs(id, 1.0, 0.0, adders);
            var gwh = new double[FossilCount]; double total = 0;
            for (int b = 0; b < 3; b++)
            {
                BlockResult r = ClearCountryBlock(id, zone, b, mc, caps, floors);
                for (int k = 0; k < FossilCount; k++) { double g = r.FossilMw[k] * EnergyLayerData.DispatchHours[zone][b]; gwh[k] += g; total += g; }
            }
            if (total > 0) { for (int k = 0; k < FossilCount; k++) { gwh[k] /= total; } }
            return gwh;
        }

        private static double SeedShare(CountryId id, int zone, int category, double[] adders, double[] caps, double[] floors) => SeedShares(id, zone, adders, caps, floors)[category];

        /// <summary>The fitted parameters of a country, by name - the categories whose adder the calibration solved (the dominant category and any without a fleet are pinned at zero and are not parameters).</summary>
        public static List<string> FittedParameters(CountryId id)
        {
            var names = new List<string>();
            if (id == CountryId.Sweden) { return names; }
            double[] target = SeedTargets(id); double[] caps = Caps(id);
            int dominant = 0; for (int k = 1; k < FossilCount; k++) { if (target[k] > target[dominant]) { dominant = k; } }
            for (int k = 0; k < FossilCount; k++)
            {
                if (k == dominant || double.IsInfinity(MarginalCost(id, k, 1.0, 0.0, 0.0)) || caps[k] <= 0) { continue; }
                names.Add(EnergyLayerData.CostCategories[k] + " adder");
            }
            return names;
        }

        /// <summary>The out-of-sample response at the seed: the adders fixed, the carbon price stepped - coal's and gas's shares of the fossil total and the peak price, before and after.</summary>
        public static (double CoalBefore, double CoalAfter, double GasBefore, double GasAfter, double PeakBefore, double PeakAfter) Response(CountryId id, double stepPoints)
        {
            Result a = ClearAtSeed(id), b = ClearAt(id, 1.0, stepPoints, WaterValueAtSeed());
            double fa = a.AnnualGwh[Coal] + a.AnnualGwh[Gas] + a.AnnualGwh[Oil], fb = b.AnnualGwh[Coal] + b.AnnualGwh[Gas] + b.AnnualGwh[Oil];
            return (fa > 0 ? a.AnnualGwh[Coal] / fa : 0, fb > 0 ? b.AnnualGwh[Coal] / fb : 0, fa > 0 ? a.AnnualGwh[Gas] / fa : 0, fb > 0 ? b.AnnualGwh[Gas] / fb : 0, a.Zones[0][2].Price, b.Zones[0][2].Price);
        }

        /// <summary>2023's shares of the fossil total, coal / gas / oil, from the levels - the calibration's targets.</summary>
        public static double[] SeedTargets(CountryId id)
        {
            int zone = EnergyLayer.ZoneIndex(EnergyLayer.Code(id));
            var t = new double[FossilCount]; double total = 0;
            for (int k = 0; k < FossilCount; k++) { t[k] = AnnualLevelGwh(zone, k); total += t[k]; }
            if (total > 0) { for (int k = 0; k < FossilCount; k++) { t[k] /= total; } }
            return t;
        }

        // ---- the writer ---------------------------------------------------------------------------------------
        /// <summary>At the seed: the residual of the family's power figure by method - the seed per head times the population, less the seed dispatch's own CO₂ - stored on the seeds so the identity holds at year 0 exactly and the writer can change hands.</summary>
        public static void SeedResidual(Country country)
        {
            EnvironmentSeeds s = country.Environment;
            if (s == null || !EnergyLayer.Has(country.Id)) { return; }
            Result seed = ClearAtSeed(country.Id);
            s.PowerPopulationSeedM = Math.Max(0.0001f, country.State.Population);   // millions
            s.PowerResidualMt = (float)(s.PowerCo2PerCapita * s.PowerPopulationSeedM - seed.DerivedCo2Mt);   // per head × million people = Mt
            s.PowerFromDispatch = true;
        }

        /// <summary>
        /// The family's power figure for a carbon tax rate: the dispatch's CO₂ plus the seed residual, over the SEED population, t per head. The
        /// fleet and the load are the 2023 system's until a stage grows them, so the figure is that system's per head - the system scales with the
        /// country meanwhile (a total that stayed fixed while a population halved would read as a doubling nobody built), and the tax base, per
        /// head × population, follows the population as the family's coupling always had it.
        /// </summary>
        public static float PowerCo2PerHead(Country country, float carbonTaxRate)
        {
            EnvironmentSeeds s = country.Environment;
            Result r = Clear(country, carbonTaxRate);
            double population = Math.Max(0.0001f, s.PowerPopulationSeedM);
            return (float)Math.Max(0.0, (r.DerivedCo2Mt + s.PowerResidualMt) / population);
        }

        /// <summary>The mix as the dispatch makes it this year - the seven labels' shares, %, for the plate's distribution row.</summary>
        public static float[] MixSharesNow(Country country)
        {
            Result r = Clear(country, EnvironmentFamily.CarbonTaxRate(country));
            double total = 0; foreach (double g in r.AnnualGwh) { total += g; }
            var shares = new float[7];
            if (total <= 0) { return shares; }
            shares[0] = (float)(100 * r.AnnualGwh[Coal] / total); shares[1] = (float)(100 * r.AnnualGwh[Gas] / total); shares[2] = (float)(100 * r.AnnualGwh[Nuclear] / total);
            shares[3] = (float)(100 * r.AnnualGwh[Hydro] / total); shares[4] = (float)(100 * r.AnnualGwh[Wind] / total); shares[5] = (float)(100 * r.AnnualGwh[Solar] / total);
            shares[6] = (float)(100 * (r.AnnualGwh[Oil] + r.AnnualGwh[Firm]) / total);
            return shares;
        }

        /// <summary>Forget the calibration (a diagnostic that re-seeds the world calls it).</summary>
        public static void ResetCalibration() { AdderCache.Clear(); GapCache.Clear(); _waterValueSeed = null; }
    }
}
