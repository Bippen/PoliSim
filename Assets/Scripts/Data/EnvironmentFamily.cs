using PoliSim.Simulation;
using UnityEngine;

namespace PoliSim.Data
{
    /// <summary>
    /// P5-C5 (2026-09-06) - THE ENVIRONMENT FAMILY, the fourth society-stat family, on 9c's grammar inherited by shape ("total CO₂/head → KEY·OPEN,
    /// lower ◂ · power / transport split → DISTRIBUTION of the key (stacked bar under it) · generation by source → DISTRIBUTION - the family picks
    /// one"), from the spine `ENVIRONMENT_FAMILY_SPINE.md` (the EDGAR 2024 booklet, verified by content; World Bank populations for the per-head
    /// division). Per country: greenhouse gases per capita 2023 (the headline, all gases), CO₂ from the power industry and from transport per capita
    /// 2023. The electricity mix seeded 2023 (Ember, cross-checked §342) is the energy layer's calibration target; the plate shows the dispatched one.
    ///
    /// THE WRITERS, SINCE EN-3 (2026-09-11): the POWER figure is written by the energy market's dispatch (EnergyMarket - the fossil generation's CO₂
    /// at each category's factor and main-activity share, plus the seed residual of heat plants, CHP heat and refineries, over the population); the
    /// family's power elasticity is retired. The TRANSPORT figure keeps its readout couplings - the carbon tax's rate against its seed and the
    /// infrastructure line per head against its seed. The headline is derived from the two with the other sectors held at their seed share. The
    /// carbon tax's BASE moved to these metrics on 2026-09-07 (TaxBases.Emissions: power and transport CO₂ per head × population; COMPLETED.md §349).
    /// </summary>
    public sealed class EnvironmentSeeds
    {
        public const float Absent = -1f;
        public float GhgPerCapita = Absent;        // t CO₂-eq per person, 2023 (EDGAR GHG_per_capita_by_country)
        public float PowerCo2PerCapita = Absent;   // t CO₂ per person, 2023 (EDGAR Power Industry ÷ WB population)
        public float TransportCo2PerCapita = Absent; // t CO₂ per person, 2023 (EDGAR Transport ÷ WB population)
        public const int Year = 2023;
        /// <summary>The electricity mix 2023, % of generation - coal, gas, nuclear, hydro, wind, solar, other (the remainder to 100): Ember via Our World in Data,
        /// cross-checked against Eurostat nrg_bal_peh (the five) and the EIA's Table 1.1 (the USA) within a point (§342, 2026-09-06). The seed the dispatch is calibrated to (EnergyLayerCheck); the plate draws the dispatched mix.</summary>
        public float[] MixShares = null;
        public bool HasMix => MixShares != null && MixShares.Length == 7;
        public float CarbonTaxRateSeed;            // the carbon tax's rate at the seed, % (0 when the line is not implemented)
        public float InfrastructurePerHeadSeed;
        /// <summary>EN-3: the power figure's residual by method at the seed, Mt - the seed per head × the population less the seed dispatch's own CO₂ (heat plants, CHP heat, refineries, the factor gap); held constant.</summary>
        public float PowerResidualMt;
        /// <summary>EN-3: the population the seed dispatch was divided by, millions - the per-head figure is the 2023 SYSTEM's per head until a stage grows the fleet and the load with the country (the system scales with the population meanwhile; the base per head × population follows it).</summary>
        public float PowerPopulationSeedM;
        /// <summary>EN-3: the power figure is written by the dispatch (true for the six the energy layer covers).</summary>
        public bool PowerFromDispatch;
        public bool Seeded;
    }

    public static class EnvironmentFamily
    {
        /// <remarks>[AUTHORED-DRAFT] - the TRANSPORT intensity's fall per point of carbon tax above the seed's rate (a tenth of a percent of the intensity per point); the power half retired at EN-3, dispatch being the tax's mechanism there.</remarks>
        public const float CarbonTaxElasticityPerPoint = 0.004f;
        /// <remarks>[AUTHORED-DRAFT] - the transport intensity's elasticity to real infrastructure spending per head against its seed (rail and public transport).</remarks>
        public const float TransportInfrastructureElasticity = 0.1f;
        /// <remarks>[AUTHORED-DRAFT] - a yearly reversion toward the targets; the spine notes Poland's power emissions fell a fifth in one year, so the band holds that speed.</remarks>
        public const float ReversionPerYear = 0.25f;
        /// <remarks>CONVENTION - runaway guards outside anything the couplings reach; the instruments' bands are the family's stated ranges.</remarks>
        public const float MinIntensity = 0.02f, MaxIntensity = 40f;

        public static void SeedAll(World world) { foreach (Country c in world.Countries) { Seed(c); } }

        public static void Seed(Country country)
        {
            EnvironmentSeeds s = country.Environment;
            switch (country.Id)
            {
                // EDGAR_2024_GHG_booklet_2024.xlsx: GHG per capita 2023; Power Industry and Transport CO₂ 2023 over WB SP.POP.TOTL 2023 (ENVIRONMENT_FAMILY_SPINE.md §1-§2).
                case CountryId.Sweden:  s.GhgPerCapita = 4.76f;  s.PowerCo2PerCapita = 0.56f; s.TransportCo2PerCapita = 1.26f; s.MixShares = Mix(0.0f, 0.1f, 29.2f, 39.9f, 20.6f, 1.9f); break;
                case CountryId.Germany: s.GhgPerCapita = 8.26f;  s.PowerCo2PerCapita = 2.13f; s.TransportCo2PerCapita = 1.68f; s.MixShares = Mix(24.6f, 15.1f, 1.4f, 4.2f, 27.7f, 12.6f); break;
                case CountryId.France:  s.GhgPerCapita = 5.81f;  s.PowerCo2PerCapita = 0.35f; s.TransportCo2PerCapita = 1.79f; s.MixShares = Mix(0.3f, 5.8f, 65.2f, 10.8f, 9.7f, 4.4f); break;
                case CountryId.Italy:   s.GhgPerCapita = 6.36f;  s.PowerCo2PerCapita = 1.43f; s.TransportCo2PerCapita = 1.74f; s.MixShares = Mix(5.1f, 45.5f, 0f, 15.5f, 9.0f, 11.7f); break;
                case CountryId.Poland:  s.GhgPerCapita = 9.67f;  s.PowerCo2PerCapita = 3.21f; s.TransportCo2PerCapita = 1.85f; s.MixShares = Mix(59.7f, 10.0f, 0f, 1.5f, 14.6f, 6.7f); break;
                case CountryId.USA:     s.GhgPerCapita = 17.61f; s.PowerCo2PerCapita = 4.35f; s.TransportCo2PerCapita = 5.08f; s.MixShares = Mix(15.9f, 42.5f, 18.2f, 5.6f, 9.9f, 5.6f); break;
                default: return;
            }
            EconomyState st = country.State;
            st.PowerCo2PerCapita = s.PowerCo2PerCapita;
            st.TransportCo2PerCapita = s.TransportCo2PerCapita;
            s.CarbonTaxRateSeed = CarbonTaxRate(country);
            s.InfrastructurePerHeadSeed = PerHead(country, SpendingCategory.InfrastructureAndDevelopment, SpendingCategory.Transportation);
            // The feedback pass (2026-09-07): the family seeds AFTER Country.CaptureStructuralBases, so the emissions reference the carbon base follows is written here, at the
            // seed's own level (otherwise the existing first-read rule would anchor it a year late).
            if (country.RevenueBaseSeeds != null && country.RevenueBaseSeeds.Length > (int)TaxBaseDriver.Emissions) { country.RevenueBaseSeeds[(int)TaxBaseDriver.Emissions] = TaxBases.Level(TaxBaseDriver.Emissions, country); }
            // EN-3 (2026-09-11): the power figure's residual by method, so the dispatch reproduces the seed exactly at year 0 and writes the figure from then on
            EnergyMarket.SeedResidual(country);
            s.Seeded = true;
        }

        /// <summary>The mix as seven shares - the six named fuels and the remainder to 100 (oil, biomass, other), from Ember's 2023 shares (Our World in Data), cross-checked (§342).</summary>
        public static float[] Mix(float coal, float gas, float nuclear, float hydro, float wind, float solar)
        {
            float other = Mathf.Max(0f, 100f - coal - gas - nuclear - hydro - wind - solar);
            return new[] { coal, gas, nuclear, hydro, wind, solar, other };
        }
        public static readonly string[] MixLabels = { "COAL", "GAS", "NUCLEAR", "HYDRO", "WIND", "SOLAR", "OTHER" };

        /// <summary>The carbon tax's rate, %, or 0 when the country has no implemented carbon tax line.</summary>
        public static float CarbonTaxRate(Country country)
        {
            foreach (TaxLine line in country.TaxLines) { if (line.Type == TaxType.CarbonTax && line.IsImplemented) { return Mathf.Max(0f, line.Rate); } }
            return 0f;
        }

        /// <summary>Real spending on the two categories over the population - the per-head term the intensities read.</summary>
        public static float PerHead(Country country, SpendingCategory a, SpendingCategory b)
        {
            float sum = 0f;
            foreach (SpendingLine line in country.SpendingLines) { if (line.Category == a || line.Category == b) { sum += line.Amount; } }
            float real = sum / Mathf.Max(0.0001f, country.State.PriceLevel);
            return real / Mathf.Max(0.0001f, SpendingDrivers.Level(SpendingDriver.Population, country));
        }

        /// <summary>The transport intensity's target: the seed times (1 − e × the carbon tax's points above its seed), floored, times the infrastructure term.</summary>
        public static float TransportTargetFor(Country country, float carbonTaxRate, float infrastructurePerHead)
        {
            EnvironmentSeeds s = country.Environment;
            float taxFactor = Mathf.Max(0.1f, 1f - CarbonTaxElasticityPerPoint * (carbonTaxRate - s.CarbonTaxRateSeed));
            float infraRatio = s.InfrastructurePerHeadSeed > 0f ? Mathf.Max(0.01f, infrastructurePerHead / s.InfrastructurePerHeadSeed) : 1f;
            return Mathf.Clamp(s.TransportCo2PerCapita * taxFactor * Mathf.Pow(1f / infraRatio, TransportInfrastructureElasticity), MinIntensity, MaxIntensity);
        }

        /// <summary>The yearly step. POWER: the dispatch writes it (EN-3) - no reversion, the year's clearing is the year's figure. TRANSPORT: its target under the readout couplings, reverted toward at the family's rate.</summary>
        public static void AdvanceYear(Country country)
        {
            EnvironmentSeeds s = country.Environment;
            if (s == null || !s.Seeded) { return; }
            EconomyState st = country.State;
            float rate = CarbonTaxRate(country);
            if (s.PowerFromDispatch) { st.PowerCo2PerCapita = Mathf.Clamp(EnergyMarket.PowerCo2PerHead(country, rate), MinIntensity, MaxIntensity); }
            float transportTarget = TransportTargetFor(country, rate, PerHead(country, SpendingCategory.InfrastructureAndDevelopment, SpendingCategory.Transportation));
            st.TransportCo2PerCapita = Mathf.Clamp(st.TransportCo2PerCapita + (transportTarget - st.TransportCo2PerCapita) * ReversionPerYear, MinIntensity, MaxIntensity);
        }

        /// <summary>The headline, derived: the seed's greenhouse gases per capita with the two sector figures' moves carried through and the other sectors held at their seed share.</summary>
        public static float GhgPerCapitaNow(Country country)
        {
            EnvironmentSeeds s = country.Environment;
            if (s == null || !s.Seeded) { return EnvironmentSeeds.Absent; }
            EconomyState st = country.State;
            return Mathf.Max(0f, s.GhgPerCapita - (s.PowerCo2PerCapita - st.PowerCo2PerCapita) - (s.TransportCo2PerCapita - st.TransportCo2PerCapita));
        }

        /// <summary>Next year's power intensity for a given carbon tax rate - the 5c arrow while a tax draft is live: the dispatch at that rate (EN-3), the standing figure where no dispatch covers the country.</summary>
        public static float ProjectPowerCo2(Country country, float carbonTaxRate)
        {
            EnvironmentSeeds s = country.Environment;
            if (s == null || !s.PowerFromDispatch) { return country.State.PowerCo2PerCapita; }
            return Mathf.Clamp(EnergyMarket.PowerCo2PerHead(country, carbonTaxRate), MinIntensity, MaxIntensity);
        }
    }
}
