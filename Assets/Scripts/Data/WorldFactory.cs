using System.Collections.Generic;

namespace PoliSim.Data
{
    /// <summary>
    /// Builds the default six-country scenario: USA, Sweden, Germany, France, Italy, Poland,
    /// with Germany/France/Italy sharing one Eurozone CurrencyZone and Germany/France/Italy/
    /// Sweden/Poland forming the EU trade bloc (Sweden and Poland are EU members but keep their
    /// own currency, same as in reality).
    ///
    /// Policy rates, inflation, and the figures the user specified (USA/Poland unemployment,
    /// USA/Eurozone/Sweden/Poland growth) are seeded to real mid-2026 data. NAIRU, unspecified
    /// unemployment rates, government-spending shares, and starting GDP levels are stylized,
    /// directionally-realistic estimates for flavor, not researched figures - see inline comments.
    /// Starting debt-to-GDP ratios are seeded to real approximate figures (USA ~124%, Italy ~138%,
    /// France ~116%, Germany ~63%, Poland ~59%, Sweden ~35%); BenefitRatePerUnemployed is a stylized
    /// estimate of welfare-state generosity, not a researched figure.
    /// </summary>
    public static class WorldFactory
    {
        public static World CreateDefault()
        {
            var eurozone = new CurrencyZone("Eurozone", 2.25f);
            var usDollarZone = new CurrencyZone("US Dollar Zone", 3.75f);
            var swedishKronaZone = new CurrencyZone("Swedish Krona Zone", 1.75f);
            var polishZlotyZone = new CurrencyZone("Polish Zloty Zone", 3.75f);

            // GDP levels are illustrative relative scale (roughly proportional to real nominal
            // GDP), not precise figures - the sim treats them as abstract currency units.
            var usa = new Country(
                CountryId.USA, "United States",
                new EconomyState(gdp: 29000f, inflation: 2.7f, unemployment: 4.5f, approvalRating: 50f, budget: 0f, taxRate: 25f,
                    governmentDebt: 29000f * 1.24f),
                usDollarZone, baseTariffRate: 3f,
                naturalUnemploymentRate: 4.0f, potentialGrowthRate: 2.0f, governmentSpendingRate: 17f, benefitRatePerUnemployed: 0.10f);

            var sweden = new Country(
                CountryId.Sweden, "Sweden",
                new EconomyState(gdp: 620f, inflation: 2.0f, unemployment: 8.0f, approvalRating: 50f, budget: 0f, taxRate: 25f,
                    governmentDebt: 620f * 0.35f),
                swedishKronaZone, baseTariffRate: 1f,
                naturalUnemploymentRate: 6.5f, potentialGrowthRate: 1.5f, governmentSpendingRate: 26f, benefitRatePerUnemployed: 0.25f);

            var germany = new Country(
                CountryId.Germany, "Germany",
                new EconomyState(gdp: 4700f, inflation: 3.0f, unemployment: 3.5f, approvalRating: 50f, budget: 0f, taxRate: 25f,
                    governmentDebt: 4700f * 0.63f),
                eurozone, baseTariffRate: 1f,
                naturalUnemploymentRate: 3.3f, potentialGrowthRate: 0.8f, governmentSpendingRate: 21f, benefitRatePerUnemployed: 0.20f);

            var france = new Country(
                CountryId.France, "France",
                new EconomyState(gdp: 3200f, inflation: 3.0f, unemployment: 7.3f, approvalRating: 50f, budget: 0f, taxRate: 25f,
                    governmentDebt: 3200f * 1.16f),
                eurozone, baseTariffRate: 1f,
                naturalUnemploymentRate: 7.5f, potentialGrowthRate: 0.8f, governmentSpendingRate: 24f, benefitRatePerUnemployed: 0.22f);

            var italy = new Country(
                CountryId.Italy, "Italy",
                new EconomyState(gdp: 2300f, inflation: 3.0f, unemployment: 7.8f, approvalRating: 50f, budget: 0f, taxRate: 25f,
                    governmentDebt: 2300f * 1.38f),
                eurozone, baseTariffRate: 1f,
                naturalUnemploymentRate: 8.0f, potentialGrowthRate: 0.8f, governmentSpendingRate: 19f, benefitRatePerUnemployed: 0.18f);

            var poland = new Country(
                CountryId.Poland, "Poland",
                new EconomyState(gdp: 840f, inflation: 2.2f, unemployment: 5.4f, approvalRating: 50f, budget: 0f, taxRate: 25f,
                    governmentDebt: 840f * 0.59f),
                polishZlotyZone, baseTariffRate: 1f,
                naturalUnemploymentRate: 5.0f, potentialGrowthRate: 3.5f, governmentSpendingRate: 18f, benefitRatePerUnemployed: 0.12f);

            var euMembers = new List<CountryId> { germany.Id, france.Id, italy.Id, sweden.Id, poland.Id };
            var europeanUnion = new TradeBloc("European Union", euMembers, externalTariffRate: 3f, internalTariffRate: 0.1f);

            AddBilateralTrade(usa, germany, aExportVolume: 120f, aImportVolume: 150f);
            AddBilateralTrade(usa, france, aExportVolume: 80f, aImportVolume: 90f);
            AddBilateralTrade(usa, sweden, aExportVolume: 30f, aImportVolume: 25f);
            AddBilateralTrade(usa, poland, aExportVolume: 20f, aImportVolume: 18f);
            AddBilateralTrade(germany, france, aExportVolume: 200f, aImportVolume: 180f);
            AddBilateralTrade(germany, italy, aExportVolume: 150f, aImportVolume: 140f);
            AddBilateralTrade(germany, poland, aExportVolume: 100f, aImportVolume: 90f);
            AddBilateralTrade(germany, sweden, aExportVolume: 70f, aImportVolume: 65f);
            AddBilateralTrade(france, italy, aExportVolume: 90f, aImportVolume: 85f);
            AddBilateralTrade(poland, sweden, aExportVolume: 40f, aImportVolume: 35f);

            var world = new World();
            world.Countries.AddRange(new[] { usa, sweden, germany, france, italy, poland });
            world.TradeBlocs.Add(europeanUnion);
            return world;
        }

        /// <summary>
        /// Wires a trade link both ways: country A's export volume is country B's import volume,
        /// and vice versa.
        /// </summary>
        private static void AddBilateralTrade(Country a, Country b, float aExportVolume, float aImportVolume)
        {
            a.TradePartners.Add(new TradePartner(b.Id, aExportVolume, aImportVolume));
            b.TradePartners.Add(new TradePartner(a.Id, aImportVolume, aExportVolume));
        }
    }
}
