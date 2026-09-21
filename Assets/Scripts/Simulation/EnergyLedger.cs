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
    /// own costs (fuel and O&amp;M, the ETS, the fitted adders), the wholesale outlay above it (the inframarginal rent that pays the
    /// fleet's fixed costs and the non-fossil fleet, not modelled), the network's revenue as its cost, the support scheme's cost. INCIDENCE: who
    /// pays - households, non-households, taxpayers - and who receives - generators, suppliers, networks, the support scheme, the state's
    /// electricity taxes; the two sides close to the unit, the book's own identity. THE CARBON TAX'S PAYMENT ON POWER IS NONE (EN-4d, ruled
    /// 2026-09-11, §467): ETS-covered plant is exempt of the national carbon tax by statute so the two prices do not stack, applied here at sector
    /// level for want of an installation register (EnergyMarket's class doc states the deviation); before the ruling this ledger booked the statutory
    /// rate on the dispatch's own CO₂, a payment the statute does not levy, which §464 had named.</para>
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
        /// <remarks>[AUTHORED-DRAFT] MAGNITUDE, SOURCED DIRECTION OF THE RATIO - EN-7a (2026-09-14): market liberalisation's proportional move of the ratio of
        /// non-households' to households' seeded pre-tax price (energy and supply plus network) per unit of the Energy sector's regulation gap below its seeded
        /// anchor (a gap of 1 is a hundred dial points): the ratio becomes ratio × (1 − this × gap). The direction is Steiner (2000), OECD Economics Department
        /// Working Paper 238, table 9 - in the regression of the industrial-to-residential price ratio, unbundling of generation from transmission (−0.051,
        /// z −2.43), third-party access (−0.035, z −1.76) and a wholesale pool (−0.114, z −3.86) lower it, against a constant of 0.528 - "the benefits of
        /// reform are disproportionately realised by industrial consumers" (§53). The paper does NOT say who pays: residential consumers are "less likely to
        /// be affected by these reforms" (§53) and it declines to measure cross-subsidy (note 39). So the closure - suppliers' receipts unchanged, households'
        /// margin carrying what non-households' loses - is the model's, and so is the size: a hundred-point dial is not her binary reform indicators.</remarks>
        public const double LiberalisationSplitPerGap = 0.3;
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
            /// <summary>EN-7b: the electricity tax's move of TaxEnv this year, the book's dollars per kWh - already inside TaxEnv, never added again; 0 at the base.</summary>
            public double ElectricityTaxShift;
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
            public double FuelVomCost, EtsCost, AdderCost, WholesaleOutlay, NetworkRevenue, LevyRevenue, BudgetSupport;
            public double FossilVariableCost => FuelVomCost + EtsCost + AdderCost;
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
            /// <summary>EN-7b: the state's electricity-tax receipts above the statute's base, billions of the book's dollars, nominal - the classes' shifts on
            /// their consumption, the excise alone (households' VAT on it is not booked: the budget's VAT does not follow the retail price); the figure the
            /// boundary plans as a budget flow (FiscalPeriod.PlannedElectricityTaxRevenue). Already inside ToStateTaxes; never added to ReceivedTotal.</summary>
            public double ElectricityTaxRevenueChange;
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
            Book b = Compute(country, r, 1.0, 0.0);
            s.EnergyCongestionRentSeed = (float)b.CongestionRent;   // EN-3b: the seed's own rent is inside the seed's network tariff; only the rent above it is credited
            Write(country, b, first: true);
        }

        /// <summary>The network credit the coming year carries, billions: the rent the last year earned ABOVE the seed's - the seed's rent carried by the price level, nominal with nominal (P5-B6), since the seed's tariff already contains 2023's capacity fees at 2023's prices - times the rule's share. Zero at the seed and wherever the links earn no more in real terms than they did in 2023.</summary>
        public static double CreditFor(Country country)
        {
            EnvironmentSeeds s = country.Environment;
            double seedRent = s != null ? s.EnergyCongestionRentSeed : 0.0;
            return Math.Max(0.0, country.State.EnergyCongestionRent - seedRent * Math.Max(0.0001f, country.State.PriceLevel)) * CongestionRentCreditShare;
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
        /// <summary>The yearly step: this year's book at this year's dispatch, last year's congestion rent credited, written to the state; the industrial bill's share of GDP and its change for MacroSystem's channel.
        /// EN-7b: returns the year's electricity-tax receipts above the statute's base (Book.ElectricityTaxRevenueChange), billions, for the boundary to plan as a budget flow
        /// - handed back rather than stored on the state, whose every public field is the trajectory dump's; 0 where the layer does not run.</summary>
        public static double AdvanceYear(Country country)
        {
            EnvironmentSeeds s = country.Environment;
            if (s == null || !s.Seeded || !EnergyLayer.Has(country.Id)) { return 0.0; }
            if (s.RetailMargin == null || s.RetailMargin.Length != ClassCount) { FitMargins(country); }   // a save from before this layer
            EnergyMarket.Result r = EnergyMarket.Clear(country);
            double credit = CreditFor(country);   // last year's rent above the seed's, billions (EN-3b: the seed's rent is inside the seed's tariff)
            Book b = Compute(country, r, Math.Max(0.0001f, country.State.PriceLevel), credit);
            Write(country, b, first: false);
            return b.ElectricityTaxRevenueChange;
        }

        /// <summary>This year's book for a clearing already made - pure: the state is read (the price level is passed, the spending line is read), never written. The carbon tax line is not read (EN-4d).</summary>
        public static Book Compute(Country country, EnergyMarket.Result r, double priceIndex, double congestionCreditBillions)
        {
            using (EnergyFleet.For(country)) { return ComputeOn(country, r, priceIndex, congestionCreditBillions); }   // §544: the fossil floors and caps the cost lines read are THIS country's fleet's
        }

        private static Book ComputeOn(Country country, EnergyMarket.Result r, double priceIndex, double congestionCreditBillions)
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

            // EN-7a: market liberalisation - the Energy sector's regulation gap below its seeded anchor; exactly zero at the seed (the level is the anchor)
            double liberalisation = LiberalisationGap(country);
            // EN-7b: the electricity tax's statute against its base - exactly zero at the seed (value and base are one float) and for the USA (no statute)
            bool taxed = EnergyLayer.HasElectricityTax(country.Id);
            for (int c = 0; c < ClassCount; c++)
            {
                var st = new ClassStack { Class = EnergyLayerData.RetailClasses[c], ConsumptionGwh = EnergyLayerData.RetailConsumptionGwh[ci][c] };
                st.Wholesale = b.WholesalePerKwh;
                st.Margin = s.RetailMargin[c] * priceIndex;
                if (liberalisation != 0.0) { st.Margin += LiberalisationShiftPerKwh(ci, c, liberalisation, usd, priceIndex); }   // a branch: the seed's arithmetic untouched
                st.Network = Math.Max(0.0, EnergyLayerData.RetailNetwork[ci][c] * usd * priceIndex - b.NetworkCreditPerKwh);
                st.Policy = EnergyLayerData.RetailPolicy[ci][c] * usd * priceIndex * b.LevyScale;
                st.TaxEnv = EnergyLayerData.RetailTaxEnv[ci][c] * usd * priceIndex;
                if (taxed)
                {
                    double taxDelta = ElectricityTaxDeltaEurPerMwh(country, c);
                    if (taxDelta != 0.0)   // a branch: the seed's arithmetic untouched
                    {
                        double seedPath = st.TaxEnv;
                        st.TaxEnv = Math.Max(0.0, seedPath + ElectricityTaxShiftPerKwh(country.Id, c, taxDelta, usd, priceIndex));   // never below zero; a statute of zero takes the base statute out within its coverage - where the coverage is capped (Poland, France) the rest of the band stays
                        st.ElectricityTaxShift = st.TaxEnv - seedPath;
                    }
                }
                st.Vat = st.PreVat * s.RetailVatRate[c];
                st.Bill = (c == Households ? st.Total : st.PreVat) * st.ConsumptionGwh / 1000.0;   // currency per kWh × GWh × 1e6 kWh / 1e9
                b.Classes[c] = st;
                double kwhBn = st.ConsumptionGwh / 1000.0;
                b.WholesaleOutlay += st.Wholesale * kwhBn;
                b.ToSuppliers += st.Margin * kwhBn;
                b.NetworkRevenue += st.Network * kwhBn;
                b.LevyRevenue += st.Policy * kwhBn;
                b.ToStateTaxes += st.TaxEnv * kwhBn + (c == Households ? st.Vat * kwhBn : 0.0);
                if (st.ElectricityTaxShift != 0.0) { b.ElectricityTaxRevenueChange += st.ElectricityTaxShift * kwhBn; }   // EN-7b: the excise's change, the budget's flow
            }
            b.ToGenerators = b.WholesaleOutlay;
            b.ToNetworks = b.NetworkRevenue;
            b.ToSupport = b.LevyRevenue + b.BudgetSupport;
            b.PaidHouseholds = b.Classes[Households].Bill;
            b.PaidNonHouseholds = b.Classes[NonHouseholds].Bill;
            b.PaidTaxpayers = b.BudgetSupport;

            // the system cost: the fossil dispatch at the dispatched tranches' own costs; no carbon-tax line - ETS-covered plant is exempt of it (EN-4d)
            FossilCosts(country.Id, r, priceIndex, out double fuelVom, out double ets, out double adder);
            b.FuelVomCost = fuelVom * usd / 1e6; b.EtsCost = ets * usd / 1e6; b.AdderCost = adder * usd / 1e6;   // GWh × market currency per MWh = 1e3 per GWh; bn = / 1e6; into dollars

            // congestion rent this year, billions of dollars
            if (r.Links != null) { double rent = 0; foreach (EnergyMarket.LinkResult l in r.Links) { rent += l.RentPerYear; } b.CongestionRent = rent * usd / 1e9; }
            return b;
        }

        /// <summary>EN-7a: the Energy sector's regulation gap below its seeded anchor, a hundred dial points to one - positive where the market is
        /// liberalised past its seed, negative where it is regulated harder; 0 with no energy sector or at the anchor.</summary>
        public static double LiberalisationGap(Country country)
        {
            foreach (Sector sector in country.Sectors)
            {
                if (sector.Type == SectorType.Energy) { return (sector.BaselineRegulationLevel - sector.RegulationLevel) / 100.0; }
            }
            return 0.0;
        }

        /// <summary>EN-7a: one class's supply-margin shift per kWh, the book's dollars - sized so the ratio of non-households' to households' seeded pre-tax price
        /// (energy and supply plus network, at this year's prices) becomes ratio × (1 − <see cref="LiberalisationSplitPerGap"/> × gap) with suppliers' receipts
        /// unchanged: non-households lose x per kWh and households gain x × q, q their consumption ratio. From (N − x) / (H + x·q) = R·(1 − kg) with N = R·H,
        /// x = R·H·kg / (1 + R·(1 − kg)·q). The first form moved a share of non-households' price and divided the same money over households' smaller
        /// consumption - Poland's households' energy component went below zero past Regulation 83 (the review measured it); in this form both classes' seeded
        /// pre-tax prices stay positive for every |kg| below 1, and the ratio's move is the same proportion in every country.</summary>
        public static double LiberalisationShiftPerKwh(int ci, int c, double gap, double usd, double priceIndex)
        {
            double n = EnergyLayerData.RetailEnergySupply[ci][NonHouseholds] + EnergyLayerData.RetailNetwork[ci][NonHouseholds];
            double h = EnergyLayerData.RetailEnergySupply[ci][Households] + EnergyLayerData.RetailNetwork[ci][Households];
            double qh = EnergyLayerData.RetailConsumptionGwh[ci][Households], qn = EnergyLayerData.RetailConsumptionGwh[ci][NonHouseholds];
            if (!(h > 0.0) || !(qh > 0.0)) { return 0.0; }
            double ratio = n / h, q = qn / qh, kg = LiberalisationSplitPerGap * gap;
            double x = ratio * h * kg / (1.0 + ratio * (1.0 - kg) * q) * usd * priceIndex;
            return c == NonHouseholds ? -x : x * q;
        }

        /// <summary>EN-7b: a class's statute against its base, EUR per MWh - the composed value (the business rate held at the EU minimum where its base is at or
        /// above it: Directive 2003/96/EC allows business no lower; households may be exempted) less the base, float from float so the seed reads exactly 0.</summary>
        public static double ElectricityTaxDeltaEurPerMwh(Country country, int c)
        {
            float value = c == Households ? country.ElectricityTaxHouseholds : country.ElectricityTaxNonHouseholds;
            float baseValue = c == Households ? country.ElectricityTaxHouseholdsBase : country.ElectricityTaxNonHouseholdsBase;
            if (value == baseValue) { return 0.0; }
            return (double)EffectiveElectricityTaxEurPerMwh(country, c) - baseValue;
        }

        /// <summary>EN-7b: the statute a class actually pays, EUR per MWh - the laws' composed value, the business rate held at the EU minimum where its base is at or
        /// above it (the ledger's move and the energy page's figure read this one expression, so the page never prints a composed rate below the floor the ledger applies).</summary>
        public static float EffectiveElectricityTaxEurPerMwh(Country country, int c)
        {
            float value = c == Households ? country.ElectricityTaxHouseholds : country.ElectricityTaxNonHouseholds;
            if (c == NonHouseholds)
            {
                float baseValue = country.ElectricityTaxNonHouseholdsBase;
                float floor = (float)EnergyLayer.ElectricityTaxFloorEurPerMwh(country.Id, c);
                if (baseValue >= floor && value < floor) { value = floor; }
            }
            return value;
        }

        /// <summary>EN-7b: one class's move of the environmental-tax component per kWh, the book's dollars - the statute's change in EUR per MWh, per kWh, within the
        /// component's coverage of the statute (EnergyLayer.ElectricityTaxCoverage, capped at 1), at the book's dollars per euro and this year's prices (nominal with nominal).</summary>
        public static double ElectricityTaxShiftPerKwh(CountryId id, int c, double deltaEurPerMwh, double usd, double priceIndex)
            => deltaEurPerMwh / 1000.0 * EnergyLayer.ElectricityTaxCoverage(id, c) * usd * priceIndex;

        /// <summary>EN-7a: whether the country's retail stack carries a policy levy for the Energy subsidy to displace (the USA's components are billed - none).</summary>
        public static bool HasPolicyLevy(CountryId id)
        {
            int ci = EnergyLayer.Index(id);
            if (ci < 0) { return false; }
            for (int c = 0; c < ClassCount; c++) { if (EnergyLayerData.RetailPolicy[ci][c] > 0.0) { return true; } }
            return false;
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
        private static void FossilCosts(CountryId id, EnergyMarket.Result r, double priceIndex, out double fuelVom, out double ets, out double adder)
        {
            fuelVom = 0; ets = 0; adder = 0;
            if (id == CountryId.Sweden) { return; }   // no dispatched fossil fleet (EnergyMarket.ClearSweden)
            int zone = EnergyLayer.ZoneIndex(EnergyLayer.Code(id));
            for (int k = 0; k < EnergyMarket.FossilCount; k++)
            {
                (double partFuel, double partEts) = EnergyMarket.CostParts(id, k, priceIndex, r.EtsRisePerT);   // the ETS at the rise the clearing was made at (a probe's; 0 in the game)
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
                    fuelVom += gwh * partFuel; ets += gwh * partEts; adder += gwh * r.Adders[k] * priceIndex;   // EN-5: the adder carries the price level like every cost (EnergyMarket.MarginalCost)
                }
            }
        }

        private static void Write(Country country, Book b, bool first)
        {
            EconomyState st = country.State;
            double gdp = Math.Max(0.0001f, st.NominalGdp);
            float share = (float)(100.0 * b.Classes[NonHouseholds].Bill / gdp);
            float previous = first || st.EnergyIndustryBillGdpShare < 0f ? share : st.EnergyIndustryBillGdpShare;
            // EN-5: the household price at the seed's prices, and its change against last year - the part of electricity's move the general price level does not already carry
            float real = (float)(b.Classes[Households].Total / Math.Max(1e-6, b.PriceIndex));
            float previousReal = first || st.EnergyHouseholdPriceReal <= 0f ? real : st.EnergyHouseholdPriceReal;
            st.EnergyHouseholdPriceRealChange = previousReal > 0f ? (real / previousReal - 1f) * 100f : 0f;
            st.EnergyHouseholdPriceReal = real;
            st.EnergyHouseholdPrice = (float)b.Classes[Households].Total;
            st.EnergyIndustryPrice = (float)b.Classes[NonHouseholds].PreVat;
            st.EnergyIndustryBill = (float)b.Classes[NonHouseholds].Bill;
            st.EnergyIndustryBillGdpShare = share;
            st.EnergyIndustryBillShareChange = share - previous;
            st.EnergyCongestionRent = (float)b.CongestionRent;
        }
    }
}
