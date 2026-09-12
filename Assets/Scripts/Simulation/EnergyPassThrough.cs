using System;
using PoliSim.Data;
using PoliSim.Data.Generated;

namespace PoliSim.Simulation
{
    /// <summary>
    /// EN-5 (2026-09-11), the energy track's S8: THE RETAIL ELECTRICITY PRICE REACHES INFLATION THROUGH A SOURCED INDEX WEIGHT. The household
    /// price the fiscal layer writes each year (EnergyLedger) is nominal - its network, levies, tax and margin carry the price level, so a
    /// price that merely rose with everything else is already inside the Phillips print. What the print does not carry is electricity's move
    /// RELATIVE to the general level: the change in the household price at the seed's prices (EconomyState.EnergyHouseholdPriceRealChange) times
    /// electricity's weight in the consumer price index (EnergyLayerData.RetailPriceIndexWeightPerMille - Eurostat prc_hicp_inw CP0451 for the
    /// five, the BLS CPI-U relative importance for the USA, 2023) is the year's pass-through in inflation points. It rides the Phillips level
    /// map beside the tariff term (SimulationManager's FiscalPeriod, MacroSystem.ApplyPhillipsCurveInflation) and, like it, is a price-LEVEL
    /// term the expectations step looks through - a one-year change in electricity's relative price must not become permanent inflation.
    /// Zero where no ledger covers the country, and zero to the noise at no policy: the stack indexes with the level it is measured against.
    /// </summary>
    public static class EnergyPassThrough
    {
        /// <remarks>CONVENTION - the catalog's weights are per mille of the all-items basket; the fraction multiplies a percentage change into inflation points.</remarks>
        public const float PerMille = 1000f;

        /// <summary>Electricity's weight in the country's consumer price index as a fraction of the basket; 0 where the layer does not cover the country.</summary>
        public static float WeightFraction(CountryId id)
        {
            int i = EnergyLayer.Index(id);
            return i < 0 ? 0f : (float)(EnergyLayerData.RetailPriceIndexWeightPerMille[i] / PerMille);
        }

        /// <summary>The pass-through the boundary plans for the coming period, inflation points: the weight times the household price's change relative to the general price level over the year just written.</summary>
        public static float Planned(Country country)
        {
            if (country.Environment == null || !country.Environment.Seeded || !EnergyLayer.Has(country.Id)) { return 0f; }
            return WeightFraction(country.Id) * country.State.EnergyHouseholdPriceRealChange;
        }

        /// <summary>The same term for a PREVIEW clone: the household price the clone's own state would produce this year (the ledger's pure book on the clone - its price level, its energy line, the dispatch; the carbon tax line no longer reaches the stack, EN-4d) against the standing real price - the boundary's own expression, so the preview and the turn read one form.</summary>
        public static float PlannedForPreview(Country preview)
        {
            if (preview.Environment == null || !preview.Environment.Seeded || !EnergyLayer.Has(preview.Id) || preview.State.EnergyHouseholdPriceReal <= 0f) { return 0f; }
            double priceIndex = Math.Max(0.0001f, preview.State.PriceLevel);
            EnergyLedger.Book book = EnergyLedger.Compute(preview, EnergyMarket.Clear(preview), priceIndex, EnergyLedger.CreditFor(preview));
            double real = book.Classes[EnergyLedger.Households].Total / priceIndex;
            float change = (float)((real / preview.State.EnergyHouseholdPriceReal - 1.0) * 100.0);
            return WeightFraction(preview.Id) * change;
        }
    }
}
