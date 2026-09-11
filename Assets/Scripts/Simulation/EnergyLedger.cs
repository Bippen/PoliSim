using System;
using System.Collections.Generic;
using PoliSim.Data;
using PoliSim.Data.Generated;

namespace PoliSim.Simulation
{
    /// <summary>
    /// EN-4 (2026-09-11), THE FISCAL LAYER of the energy layer: the retail stack and the two ledgers every energy policy writes.
    ///
    /// <para><b>The retail stack.</b> A kWh's price, per customer class (households, non-households), is WHOLESALE + SUPPLY MARGIN + NETWORK +
    /// POLICY LEVIES + ENVIRONMENTAL TAX + VAT, each component traceable and IN THE BOOK'S DOLLARS (the game keeps every country's book in US
    /// dollars; the catalog's ECB 2023 rates bridge the sources' euro and the tax lines' national currency): the wholesale is this year's dispatch (EnergyMarket), load-weighted over
    /// the blocks and, for Sweden, the zones; the supply margin is FITTED at the seed - Eurostat's energy-and-supply component less the seed
    /// dispatch's wholesale - and indexes with prices; network, levies and environmental tax are Eurostat's 2023 components (EnergyData/retail_2023.csv,
    /// EIA's averages for the USA with its components BILLED) carried by the price level, nominal with nominal (P5-B6); VAT is the rate the seed's
    /// components imply, levied on the stack it is levied on. Non-households bear the pre-VAT price - recoverable VAT is no cost - so their bill
    /// and their line in the ledgers exclude it, stated.</para>
    ///
    /// <para><b>The support scheme.</b> The book's energy spending line (SpendingCategory.Energy) is the budget-financed support; it indexes like any
    /// other line (P5-B2). The bill-financed support is the policy levy - revenue following its base (P5-B3): the levy per kWh times the consumption.
    /// The scheme's cost is one sum; the split is the policy: a budget line moved above its indexed path takes the levy down one for one, a line
    /// cut takes it up (the EEG's own history - levy until July 2022, the federal budget since), spread over every kWh in proportion to the seed's
    /// levies by class; a levy cannot fall below zero, and support above the whole levy is taxpayer-funded support with no retail effect. Germany's
    /// book carries no energy line (the KTF is a Sondervermögen outside the Bundeshaushalt), so its budget-financed support reads 0, stated.</para>
    ///
    /// <para><b>Congestion rent and its redistribution rule.</b> Where a zonal link binds (Sweden's snitt), the rent the dispatch computes is
    /// credited to the NEXT year's network component per kWh, both classes alike - Regulation (EU) 2019/943 Article 19(2)-(3) (the residual income
    /// reduces network tariffs) and Svenska kraftnät's practice with its capacity fees. Zero where no block binds, as at the seed.</para>
    ///
    /// <para><b>The two ledgers.</b> SYSTEM COST as far as this model states it: the fossil dispatch's variable cost at the dispatched tranches'
    /// own costs (fuel and O&amp;M, the ETS, the carbon tax, the fitted adders), the wholesale outlay above it (the inframarginal rent that pays the
    /// fleet's fixed costs and the non-fossil fleet, not modelled), the network's revenue as its cost, the support scheme's cost. INCIDENCE: who
    /// pays - households, non-households, taxpayers - and who receives - generators, suppliers, networks, the support scheme, the state's
    /// electricity taxes; the two sides close to the unit, the book's own identity. The carbon tax's payment on power is booked under EnergyMarket's
    /// point-per-tonne CONVENTION and named so; the budget's carbon revenue is the book's (TaxBases, P5-B3) - two readings of one line, reconciled
    /// by a ruling, not here.</para>
    ///
    /// <para><b>What reaches the model (the BASELINE move).</b> The energy line's BusinessConfidence proxy is retired: the industrial electricity
    /// bill's change as a share of GDP is what firms bear, and MacroSystem reads it at the proxy's own sensitivity, the sign reversed. THE SINGLE
    /// BOOK: the state's prices and bill are written by AdvanceYear and presented as written; recomputing the stack at the standing rate returns
    /// the stored figures (EnergyLedgerDiagnostic holds it).</para>
    /// </summary>
    public static class EnergyLedger
    {
        /// <remarks>CONVENTION - the indices of EnergyLayerData.RetailClasses (households, non-households), asserted against the catalog at the seed.</remarks>
        public const int Households = 0, NonHouseholds = 1, ClassCount = 2;
        /// <remarks>CONVENTION - the redistribution rule where a zonal link binds: the whole of the year's congestion rent is credited to the next year's network component, per kWh, both classes alike (Regulation (EU) 2019/943 Article 19(2)-(3): the residual income reduces network tariffs; Svenska kraftnät's capacity fees). No share retained.</remarks>
        public const float CongestionRentCreditShare = 1f;
        /// <remarks>CONVENTION - the fitted parameters of this class, counted: two supply margins per country, one per customer class - and nothing else here is fitted.</remarks>
        public const int FittedParametersPerCountry = 2;

        /// <summary>The book's currency - the game keeps every country's book in US dollars (Sweden's GDP seeds at 620, the USA's at 29 000), so the stack, the bills and the ledgers are in dollars, and the plate says so; the sources' euro and the tax lines' national currency reach them through the catalog's ECB 2023 rates.</summary>
        public const string BookCurrency = "USD";

        /// <summary>One class's stack this year, the book's dollars per kWh, and its bill in billions of dollars.</summary>
        public sealed class ClassStack
        {
            public string Class;
            public double ConsumptionGwh;
            public double Wholesale, Margin, Network, Policy, TaxEnv, Vat;
            public double PreVat => Wholesale + Margin + Network + Policy + TaxEnv;
            public double Total => PreVat + Vat;
            /// <summary>What the class pays, billions: the all-in price for households, the pre-VAT price for non-households (recoverable VAT is no cost).</summary>
            public double Bill;
        }

        /// <summary>The year's book: the stacks, the system-cost lines and the incidence lines, billions of the book's dollars.</summary>
        public sealed class Book
        {
            public CountryId Country;
            public double PriceIndex;
            /// <summary>The book's dollars per unit of the market's currency, and the country's currency per unit of it - the two bridges this book used.</summary>
            public double UsdPerMarket, NationalPerMarket;
            /// <summary>The load-weighted wholesale price, the book's dollars per kWh.</summary>
            public double WholesalePerKwh;
            public ClassStack[] Classes;
            // ---- system cost
            public double FuelVomCost, EtsCost, CarbonTaxCost, AdderCost, WholesaleOutlay, NetworkRevenue, LevyRevenue, BudgetSupport;
            public double FossilVariableCost => FuelVomCost + EtsCost + CarbonTaxCost + AdderCost;
            /// <summary>The wholesale outlay above the fossil variable cost - what pays the fleet's fixed costs and the non-fossil fleet; not modelled, printed.</summary>
            public double InframarginalRent => WholesaleOutlay - FossilVariableCost;
            public double SupportCost => LevyRevenue + BudgetSupport;
            // ---- incidence
            public double PaidHouseholds, PaidNonHouseholds, PaidTaxpayers;
            public double ToGenerators, ToSuppliers, ToNetworks, ToSupport, ToStateTaxes;
            public double PaidTotal => PaidHouseholds + PaidNonHouseholds + PaidTaxpayers;
            public double ReceivedTotal => ToGenerators + ToSuppliers + ToNetworks + ToSupport + ToStateTaxes;
            /// <summary>The identity's gap - zero to the unit when the book closes.</summary>
            public double Gap => PaidTotal - ReceivedTotal;
            public double StateNet => ToStateTaxes - BudgetSupport;
            // ---- the rules' readouts
            /// <summary>This year's congestion rent on the zonal links, billions (Sweden's; 0 elsewhere and where no block binds).</summary>
            public double CongestionRent;
            /// <summary>The credit applied to this year's network component, dollars per kWh - last year's rent over the consumption.</summary>
            public double NetworkCreditPerKwh;
            /// <summary>The levy's scale against its indexed seed: 1 with the budget line on its path, below 1 where the line was raised, above where it was cut, 0 at the floor.</summary>
            public double LevyScale;
            /// <summary>The budget line's deviation from its indexed path, billions - the player's (or the AI ministry's) own doing.</summary>
            public double SupportDeviation;
        }

        // ---- the seed ---------------------------------------------------------------------------------------
        /// <summary>At the seed: the two FITTED margins and the implied VAT rates by class on the seeds, and the seed's book written to the state, so the presented figures exist from turn 0 and reproduce Eurostat's components exactly.</summary>
        public static void Seed(Country country)
        {
            EnvironmentSeeds s = country.Environment;
            if (s == null || !EnergyLayer.Has(country.Id)) { return; }
            FitMargins(country);
            EnergyMarket.Result r = EnergyMarket.ClearAtSeed(country.Id);
            Book b = Compute(country, r, 1.0, EnvironmentFamily.CarbonTaxRate(country), 0.0);
            Write(country, b, first: true);
        }

        /// <summary>The fit itself - a function of the catalog and the seed dispatch alone, so a save from before this layer refits to the same figures.</summary>
        public static void FitMargins(Country country)
        {
            EnvironmentSeeds s = country.Environment;
            int ci = EnergyLayer.Index(country.Id);
            if (EnergyLayerData.RetailClasses.Length != ClassCount || EnergyLayerData.RetailClasses[Households] != "households" || EnergyLayerData.RetailClasses[NonHouseholds] != "nonhousehold")
            { throw new InvalidOperationException("EnergyLedger: the catalog's retail classes are not households, nonhousehold in that order."); }
            double usd = EnergyLayerData.UsdPerMarketCurrency[ci];
            EnergyMarket.Result r = EnergyMarket.ClearAtSeed(country.Id);   // the SEED's water value for Sweden, whatever turn state stands
            double wholesaleSeed = LoadWeightedPricePerMwh(r) / 1000.0 * usd;
            s.RetailWholesaleSeed = (float)wholesaleSeed;
            s.RetailMargin = new float[ClassCount];
            s.RetailVatRate = new float[ClassCount];
            for (int c = 0; c < ClassCount; c++)
            {
                s.RetailMargin[c] = (float)(EnergyLayerData.RetailEnergySupply[ci][c] * usd - wholesaleSeed);
                // the rate over the pre-VAT COMPONENTS' sum, not Eurostat's stated total less VAT: the published components sum to the stated total within 0.0003 (the source's rounding), and the stack's total is its parts' sum
                double preVat = SeedPreVatPerKwh(ci, c);
                s.RetailVatRate[c] = preVat > 0 ? (float)(EnergyLayerData.RetailVat[ci][c] / preVat) : 0f;
            }
        }

        /// <summary>The seed's pre-VAT stack per kWh in the market's currency - the sum of Eurostat's published components (energy and supply, network, policy levies, environmental tax).</summary>
        public static double SeedPreVatPerKwh(int ci, int c) => EnergyLayerData.RetailEnergySupply[ci][c] + EnergyLayerData.RetailNetwork[ci][c] + EnergyLayerData.RetailPolicy[ci][c] + EnergyLayerData.RetailTaxEnv[ci][c];
        /// <summary>The seed's total per kWh in the market's currency as the stack states it - the components' sum with VAT; Eurostat's stated total differs from it by the source's rounding (within 0.0003), printed beside it, never substituted.</summary>
        public static double SeedTotalPerKwh(int ci, int c) => SeedPreVatPerKwh(ci, c) + EnergyLayerData.RetailVat[ci][c];

        /// <summary>The fitted parameters of this class, named - the check counts them.</summary>
        public static List<string> FittedParameters(CountryId id)
        {
            var names = new List<string>();
            if (!EnergyLayer.Has(id)) { return names; }
            for (int c = 0; c < ClassCount; c++) { names.Add(EnergyLayerData.RetailClasses[c] + " supply margin"); }
            return names;
        }

        /// <summary>The country's own currency, by ISO code - the unit of its tax lines' points and of Eurostat's krona and zloty rows; the dump names it beside the book's dollars.</summary>
        public static string NationalCurrencyCode(CountryId id)
        {
            switch (id)
            {
                case CountryId.Sweden: return "SEK";
                case CountryId.Poland: return "PLN";
                case CountryId.USA: return "USD";
                default: return "EUR";
            }
        }

        // ---- the year ---------------------------------------------------------------------------------------
        /// <summary>The yearly step: this year's book at the standing rate, last year's congestion rent credited, written to the state; the industrial bill's share of GDP and its change for MacroSystem's channel.</summary>
        public static void AdvanceYear(Country country)
        {
            EnvironmentSeeds s = country.Environment;
            if (s == null || !s.Seeded || !EnergyLayer.Has(country.Id)) { return; }
            if (s.RetailMargin == null || s.RetailMargin.Length != ClassCount) { FitMargins(country); }   // a save from before this layer
            float rate = EnvironmentFamily.CarbonTaxRate(country);
            EnergyMarket.Result r = EnergyMarket.Clear(country, rate);
            double credit = Math.Max(0.0, country.State.EnergyCongestionRent) * CongestionRentCreditShare;   // last year's rent, billions
            Book b = Compute(country, r, Math.Max(0.0001f, country.State.PriceLevel), rate, credit);
            Write(country, b, first: false);
        }

        /// <summary>This year's book for a clearing already made - pure: the state is read (the price level is passed, the spending line is read), never written.</summary>
        public static Book Compute(Country country, EnergyMarket.Result r, double priceIndex, float carbonTaxRate, double congestionCreditBillions)
        {
            EnvironmentSeeds s = country.Environment;
            int ci = EnergyLayer.Index(country.Id);
            double nat = EnergyLayerData.NationalPerMarketCurrency[ci], usd = EnergyLayerData.UsdPerMarketCurrency[ci];
            var b = new Book { Country = country.Id, PriceIndex = priceIndex, UsdPerMarket = usd, NationalPerMarket = nat, Classes = new ClassStack[ClassCount] };
            b.WholesalePerKwh = LoadWeightedPricePerMwh(r) / 1000.0 * usd;

            double totalCons = 0; for (int c = 0; c < ClassCount; c++) { totalCons += EnergyLayerData.RetailConsumptionGwh[ci][c]; }
            b.NetworkCreditPerKwh = totalCons > 0 ? congestionCreditBillions * 1000.0 / totalCons : 0.0;   // bn × 1e9 over GWh × 1e6

            // the support scheme: the budget line and its deviation from the indexed path; the levy scaled the other way
            SpendingLine line = null;
            foreach (SpendingLine l in country.SpendingLines) { if (l.Category == SpendingCategory.Energy) { line = l; break; } }
            b.BudgetSupport = line != null ? Math.Max(0f, line.Amount) : 0.0;
            b.SupportDeviation = line != null ? line.Amount - line.SeedAmount : 0.0;
            double seedLevy = 0; for (int c = 0; c < ClassCount; c++) { seedLevy += EnergyLayerData.RetailPolicy[ci][c] * usd * EnergyLayerData.RetailConsumptionGwh[ci][c] / 1000.0; }
            double levyNominal = seedLevy * priceIndex;
            b.LevyScale = levyNominal > 0 ? Math.Max(0.0, 1.0 - b.SupportDeviation / levyNominal) : 0.0;

            for (int c = 0; c < ClassCount; c++)
            {
                var st = new ClassStack { Class = EnergyLayerData.RetailClasses[c], ConsumptionGwh = EnergyLayerData.RetailConsumptionGwh[ci][c] };
                st.Wholesale = b.WholesalePerKwh;
                st.Margin = s.RetailMargin[c] * priceIndex;
                st.Network = Math.Max(0.0, EnergyLayerData.RetailNetwork[ci][c] * usd * priceIndex - b.NetworkCreditPerKwh);
                st.Policy = EnergyLayerData.RetailPolicy[ci][c] * usd * priceIndex * b.LevyScale;
                st.TaxEnv = EnergyLayerData.RetailTaxEnv[ci][c] * usd * priceIndex;
                st.Vat = st.PreVat * s.RetailVatRate[c];
                st.Bill = (c == Households ? st.Total : st.PreVat) * st.ConsumptionGwh / 1000.0;   // currency per kWh × GWh × 1e6 kWh / 1e9
                b.Classes[c] = st;
                double kwhBn = st.ConsumptionGwh / 1000.0;
                b.WholesaleOutlay += st.Wholesale * kwhBn;
                b.ToSuppliers += st.Margin * kwhBn;
                b.NetworkRevenue += st.Network * kwhBn;
                b.LevyRevenue += st.Policy * kwhBn;
                b.ToStateTaxes += st.TaxEnv * kwhBn + (c == Households ? st.Vat * kwhBn : 0.0);
            }
            b.ToGenerators = b.WholesaleOutlay;
            b.ToNetworks = b.NetworkRevenue;
            b.ToSupport = b.LevyRevenue + b.BudgetSupport;
            b.PaidHouseholds = b.Classes[Households].Bill;
            b.PaidNonHouseholds = b.Classes[NonHouseholds].Bill;
            b.PaidTaxpayers = b.BudgetSupport;

            // the system cost: the fossil dispatch at the dispatched tranches' own costs
            double taxDelta = carbonTaxRate - s.CarbonTaxRateSeed;
            FossilCosts(country.Id, r, priceIndex, taxDelta, out double fuelVom, out double ets, out double adder);
            b.FuelVomCost = fuelVom * usd / 1e6; b.EtsCost = ets * usd / 1e6; b.AdderCost = adder * usd / 1e6;   // GWh × market currency per MWh = 1e3 per GWh; bn = / 1e6; into dollars
            b.CarbonTaxCost = Math.Max(0f, carbonTaxRate) * EnergyMarket.CarbonTaxPointPerTonne * r.DerivedCo2Mt / 1000.0 / nat * usd;   // points (national per tonne) × Mt = millions; bn = / 1000; national → market → dollars

            // congestion rent this year, billions of dollars
            if (r.Links != null) { double rent = 0; foreach (EnergyMarket.LinkResult l in r.Links) { rent += l.RentPerYear; } b.CongestionRent = rent * usd / 1e9; }
            return b;
        }

        /// <summary>The load-weighted price over the blocks (and Sweden's zones), the market's currency per MWh.</summary>
        public static double LoadWeightedPricePerMwh(EnergyMarket.Result r)
        {
            double sum = 0, weight = 0;
            for (int z = 0; z < r.Zones.Length; z++)
            {
                int zone = EnergyLayer.ZoneIndex(r.ZoneNames[z]);
                for (int bl = 0; bl < 3; bl++)
                {
                    double mw = r.Country == CountryId.Sweden ? EnergyLayerData.ZoneConsumptionBlockMw[zone][bl] : EnergyLayerData.DispatchDemandMw[zone][bl];
                    double w = Math.Max(0.0, mw) * EnergyLayerData.DispatchHours[zone][bl];
                    sum += r.Zones[z][bl].Price * w; weight += w;
                }
            }
            return weight > 0 ? sum / weight : 0.0;
        }

        /// <summary>The fossil dispatch's variable cost by part, GWh × currency per MWh (the market's currency), at the dispatched tranches' own costs: a category dispatched to a fraction f of its flexible capacity ran its cheapest tranches, whose mean is (1 − spread) + spread × f of the category's mean; the floors run at the mean.</summary>
        private static void FossilCosts(CountryId id, EnergyMarket.Result r, double priceIndex, double taxDelta, out double fuelVom, out double etsAndTax, out double adder)
        {
            fuelVom = 0; etsAndTax = 0; adder = 0;
            if (id == CountryId.Sweden) { return; }   // no dispatched fossil fleet (EnergyMarket.ClearSweden)
            int zone = EnergyLayer.ZoneIndex(EnergyLayer.Code(id));
            for (int k = 0; k < EnergyMarket.FossilCount; k++)
            {
                (double partFuel, double partEts, double partTax) = EnergyMarket.CostParts(id, k, priceIndex, taxDelta);
                if (double.IsInfinity(partFuel)) { continue; }
                double floor = EnergyMarket.MustRunFossilMw(id, k), cap = Math.Max(0.0, EnergyMarket.DependableMw(id, k) - floor);
                for (int bl = 0; bl < 3; bl++)
                {
                    double hours = EnergyLayerData.DispatchHours[zone][bl];
                    double mw = r.Zones[0][bl].FossilMw[k], flexible = Math.Max(0.0, mw - floor);
                    double f = cap > 0 ? Math.Min(1.0, flexible / cap) : 0.0;
                    double multiplier = 1.0 - EnergyMarket.FleetSpread + EnergyMarket.FleetSpread * f;
                    double weightedMw = Math.Min(mw, floor) + flexible * multiplier;   // the floor at the mean, the flexible part at its tranches' mean
                    double gwh = weightedMw * hours / 1000.0;
                    fuelVom += gwh * partFuel; etsAndTax += gwh * partEts; adder += gwh * r.Adders[k];
                    _ = partTax;   // the tax above the seed is inside the merit order; the ledger books the whole tax at the standing rate from the CO₂ (Compute), not the delta twice
                }
            }
        }

        private static void Write(Country country, Book b, bool first)
        {
            EconomyState st = country.State;
            double gdp = Math.Max(0.0001f, st.NominalGdp);
            float share = (float)(100.0 * b.Classes[NonHouseholds].Bill / gdp);
            float previous = first || st.EnergyIndustryBillGdpShare < 0f ? share : st.EnergyIndustryBillGdpShare;
            st.EnergyHouseholdPrice = (float)b.Classes[Households].Total;
            st.EnergyIndustryPrice = (float)b.Classes[NonHouseholds].PreVat;
            st.EnergyIndustryBill = (float)b.Classes[NonHouseholds].Bill;
            st.EnergyIndustryBillGdpShare = share;
            st.EnergyIndustryBillShareChange = share - previous;
            st.EnergyCongestionRent = (float)b.CongestionRent;
        }
    }
}
