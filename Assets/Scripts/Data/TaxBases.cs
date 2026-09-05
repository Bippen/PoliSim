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
        /// <summary>GDP - D-16's base as it was; corporate and capital income scale with output absent a profit-share series, carbon awaits P5-C5's CO₂, estate and wealth taxes are levied on stocks the model does not carry.</summary>
        Output,
        /// <summary>Employment times the real wage: the 20–64 cohort × participation × (1 − unemployment) × RealWageIndex - the caseload income and payroll taxes are levied on.</summary>
        WageBill,
        /// <summary>Household consumption (the C of the national accounts) - what VAT, sales and excise taxes are levied on.</summary>
        Consumption,
        /// <summary>The housing stock at its price: population × HousePriceIndex - what a property tax is levied on.</summary>
        Housing
    }

    public static class TaxBases
    {
        public const int DriverCount = 4;

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
            if (country.RevenueBaseSeedGdp <= 0f || country.RevenueBaseSeeds == null || country.RevenueBaseSeeds.Length < DriverCount)
            {
                return share * country.State.NominalGdp;   // P5-B6: nominal
            }
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
        public static float Revenue(Country country, TaxLine line) => line.Rate / 100f * Base(country, line.Type);

        /// <summary>The driver's ratio now against the seed - 1 where the driver has not moved or is not yet referenced.</summary>
        public static float DriverRatio(Country country, TaxType type)
        {
            if (country.RevenueBaseSeeds == null || country.RevenueBaseSeeds.Length < DriverCount) { return 1f; }
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
                default: return "output";
            }
        }
    }
}
