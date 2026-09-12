using UnityEngine;

namespace PoliSim.Data
{
    /// <summary>
    /// P5-B3 (2026-09-05): what a tax base FOLLOWS. Before this pass every base was a fixed share of GDP
    /// (TaxBaseTable, D-16), so revenue moved one-for-one with GDP and with nothing else - §312 measured the
    /// elasticity at exactly 1 and named the two missing channels: employment (a recession that cuts jobs more than
    /// output did not cut payroll revenue more) and the distribution (F4's income dimension, which the substrate does
    /// not carry yet). The base is now the sourced share of GDP AT THE SEED, carried forward by the ratio of its own
    /// driver: the wage bill for income and payroll taxes, consumption for VAT and the sales and excise lines, the
    /// housing stock at its price for property tax, and output itself for the rest - where output IS the base's
    /// driver the arithmetic is D-16's unchanged. P5-B6 (2026-09-05): the base is NOMINAL - the real base times
    /// EconomyState.PriceLevel - because the book is in current prices from that pass on (§314 measured what a price term
    /// did in a constant-price book; B6 gave the book the prices first).
    /// </summary>
    public enum TaxBaseDriver
    {
        /// <summary>GDP - D-16's base as it was; corporate and capital income scale with output absent a profit-share series, carbon moved to the taxed CO₂ on 2026-09-07 (Emissions), estate and wealth taxes are levied on stocks the model does not carry.</summary>
        Output,
        /// <summary>Employment times the real wage: the 20–64 cohort × participation × (1 − unemployment) × RealWageIndex - the caseload income and payroll taxes are levied on.</summary>
        WageBill,
        /// <summary>Household consumption (the C of the national accounts) - what VAT, sales and excise taxes are levied on.</summary>
        Consumption,
        /// <summary>The housing stock at its price: population × HousePriceIndex - what a property tax is levied on.</summary>
        Housing,
        /// <summary>The taxed emissions (the environment feedback pass, 2026-09-07): CO₂ per head (EnvironmentFamily, EDGAR-seeded) times the population - tonnes, what a
        /// carbon tax is levied on. A tax that works erodes its own base, as a Pigouvian tax does. Since EN-4c (2026-09-11) the carbon line's revenue is its rate per
        /// tonne times this level directly (TaxBases.RevenueAtRate); the driver ratio still reads it. SINCE EN-4d (2026-09-11, §467) THE LEVEL IS TRANSPORT'S TONNES:
        /// the national carbon tax covers transport and non-ETS combustion, and the power fleet - ETS-covered - is exempt of it by statute (Sweden's LSE 6 a kap. 1 §,
        /// Germany's BEHG § 7 Abs. 5, France's composante carbone; the reason in each: the ETS prices those tonnes and the two do not stack). No installation register
        /// exists here, so the exemption is applied at SECTOR level - the deviation, stated in EnergyMarket's class doc. Heating fuels the model does not carry stay outside.</summary>
        Emissions
    }

    public static class TaxBases
    {
        public const int DriverCount = 5;

        /// <summary>The driver per instrument, stated once. A type not listed follows output (D-16's base unchanged).</summary>
        public static TaxBaseDriver Of(TaxType type)
        {
            switch (type)
            {
                case TaxType.IncomeTax:
                case TaxType.PayrollTax:
                    return TaxBaseDriver.WageBill;
                case TaxType.VAT:
                case TaxType.SalesTax:
                case TaxType.ExciseTax:
                    return TaxBaseDriver.Consumption;
                case TaxType.PropertyTax:
                    return TaxBaseDriver.Housing;
                case TaxType.CarbonTax:
                    return TaxBaseDriver.Emissions;   // the environment feedback pass (2026-09-07): off output, onto the taxed CO₂
                default:
                    return TaxBaseDriver.Output;
            }
        }

        /// <summary>The driver's current level for a country, read off the state and the cohort substrate - never authored. Units are irrelevant: only the RATIO to the seed's level is ever used.</summary>
        public static float Level(TaxBaseDriver driver, Country country)
        {
            EconomyState s = country.State;
            switch (driver)
            {
                case TaxBaseDriver.WageBill:
                    return SpendingDrivers.Level(SpendingDriver.WorkingAge20To64, country)
                           * Mathf.Clamp(s.LaborForceParticipationRate, 0f, 100f) / 100f
                           * Mathf.Clamp(100f - s.Unemployment, 0f, 100f) / 100f
                           * Mathf.Max(0f, s.RealWageIndex) / 100f;
                case TaxBaseDriver.Consumption:
                    return Mathf.Max(0f, s.Consumption);
                case TaxBaseDriver.Housing:
                    return Mathf.Max(0f, s.Population) * Mathf.Max(0f, s.HousePriceIndex) / 100f;
                case TaxBaseDriver.Emissions:
                    // Absent intensities (no family, or a save from before it) read 0, so the base holds at the seed's real level - stated, not invented.
                    // EN-4d: transport's tonnes alone - the power fleet pays the ETS and is exempt of the national carbon tax (sector level; the deviation stated above).
                    return s.TransportCo2PerCapita > 0f ? s.TransportCo2PerCapita * Mathf.Max(0f, s.Population) : 0f;
                default:
                    return Mathf.Max(0f, s.GDP);
            }
        }

        /// <summary>
        /// The base an instrument is levied on, in the country's unit: the sourced share of GDP at the seed
        /// (TaxBaseTable.BaseShareOfGdp × the seed's GDP) times the ratio of the driver's level now to its level at
        /// the seed (Country.RevenueBaseSeeds, captured by Country.CaptureStructuralBases). A driver whose seed level
        /// was not yet defined when the seed was captured (consumption is computed by the first day's national
        /// accounts) takes its reference the first time it is read above zero and applies a ratio of 1 that once.
        /// A country without captured seeds (a save from before this pass) reads D-16's base: share × GDP today.
        /// </summary>
        public static float Base(Country country, TaxType type)
        {
            float share = TaxBaseTable.BaseShareOfGdp(country.Id, type);
            if (country.RevenueBaseSeedGdp <= 0f || country.RevenueBaseSeeds == null)
            {
                return share * country.State.NominalGdp;   // P5-B6: nominal
            }
            if (country.RevenueBaseSeeds.Length < DriverCount) { System.Array.Resize(ref country.RevenueBaseSeeds, DriverCount); }   // a save from before a driver was added: its reference is captured on first read
            TaxBaseDriver driver = Of(type);
            float level = Level(driver, country);
            float reference = country.RevenueBaseSeeds[(int)driver];
            if (reference <= 0f)
            {
                if (level <= 0f) { return share * country.RevenueBaseSeedGdp * country.State.PriceLevel; }
                country.RevenueBaseSeeds[(int)driver] = reference = level;
            }
            return share * country.RevenueBaseSeedGdp * (level / reference) * country.State.PriceLevel;   // P5-B6: the base is NOMINAL - the real base at the seed's prices times the price level
        }

        /// <summary>The instrument's revenue before the coverage bridge: rate × base. ONE ACCESSOR, READ BY EVERY REVENUE SITE (the turn, the household burden, the Budget's estimates, the diagnostics), as D-16's was.</summary>
        public static float Revenue(Country country, TaxLine line) => RevenueAtRate(country, line.Type, line.Rate);

        /// <summary>
        /// The revenue an instrument yields at a given rate, the book's dollars. A percentage tax: rate ÷ 100 × its base. THE CARBON TAX (EN-4c, ruled
        /// 2026-09-11): the rate is the country's currency per tonne of CO₂ and the revenue is RATE × THE TAXED TONNES - the emissions level, Mt - brought
        /// into the book's dollars by the ECB rate; the share-of-GDP reading the line carried before the model had priceable emissions is retired. The
        /// taxed tonnes are TRANSPORT's since EN-4d (2026-09-11, §467): the power fleet is ETS-covered and exempt of the tax by statute, applied at sector
        /// level (TaxBaseDriver.Emissions states the sources and the deviation); heating fuels the model does not carry stay outside.
        /// </summary>
        public static float RevenueAtRate(Country country, TaxType type, float rate)
        {
            if (type == TaxType.CarbonTax)
            {
                float taxedMt = Level(TaxBaseDriver.Emissions, country);   // per head × millions of people = Mt
                double nationalBillions = rate * taxedMt / 1000.0;         // currency per tonne × Mt = millions; billions = ÷ 1000
                return (float)(nationalBillions / EnergyLayer.NationalPerUsd(country.Id));
            }
            return rate / 100f * Base(country, type);
        }

        /// <summary>The driver's ratio now against the seed - 1 where the driver has not moved or is not yet referenced.</summary>
        public static float DriverRatio(Country country, TaxType type)
        {
            if (country.RevenueBaseSeeds == null) { return 1f; }
            if (country.RevenueBaseSeeds.Length < DriverCount) { System.Array.Resize(ref country.RevenueBaseSeeds, DriverCount); }
            TaxBaseDriver driver = Of(type);
            float reference = country.RevenueBaseSeeds[(int)driver];
            float level = Level(driver, country);
            return reference > 0f && level > 0f ? level / reference : 1f;
        }

        public static string Name(TaxBaseDriver driver)
        {
            switch (driver)
            {
                case TaxBaseDriver.WageBill: return "the wage bill";
                case TaxBaseDriver.Consumption: return "consumption";
                case TaxBaseDriver.Housing: return "the housing stock at its price";
                case TaxBaseDriver.Emissions: return "the taxed emissions (transport CO₂; the power fleet pays the ETS and is exempt)";
                default: return "output";
            }
        }
    }
}
