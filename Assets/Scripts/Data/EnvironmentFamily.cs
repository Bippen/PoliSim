using UnityEngine;

namespace PoliSim.Data
{
    /// <summary>
    /// P5-C5 (2026-09-06) - THE ENVIRONMENT FAMILY, the fourth society-stat family, on 9c's grammar inherited by shape ("total CO₂/head → KEY·OPEN,
    /// lower ◂ · power / transport split → DISTRIBUTION of the key (stacked bar under it) · generation by source → DISTRIBUTION - the family picks
    /// one"), from the spine `ENVIRONMENT_FAMILY_SPINE.md` (the EDGAR 2024 booklet, verified by content; World Bank populations for the per-head
    /// division). Per country: greenhouse gases per capita 2023 (the headline, all gases), CO₂ from the power industry and from transport per capita
    /// 2023. The electricity mix is a FETCH with no figure (Ember refused the fetch; Eurostat nrg_bal_c and the EIA are named).
    ///
    /// THE COUPLINGS ARE READOUTS, NOT FEEDBACK: the family reads the carbon tax's rate against its seed and the energy and infrastructure lines per
    /// head against their seeds, and moves the two sector figures; the headline is derived from them with the other sectors held at their seed
    /// share. The carbon tax's BASE stays on output (P5-B3) - moving it to these metrics is SHEETED on the row, not built (a revenue change is
    /// BASELINE and its own pass).
    /// </summary>
    public sealed class EnvironmentSeeds
    {
        public const float Absent = -1f;
        public float GhgPerCapita = Absent;        // t CO₂-eq per person, 2023 (EDGAR GHG_per_capita_by_country)
        public float PowerCo2PerCapita = Absent;   // t CO₂ per person, 2023 (EDGAR Power Industry ÷ WB population)
        public float TransportCo2PerCapita = Absent; // t CO₂ per person, 2023 (EDGAR Transport ÷ WB population)
        public const int Year = 2023;
        public float CarbonTaxRateSeed;            // the carbon tax's rate at the seed, % (0 when the line is not implemented)
        public float EnergyPerHeadSeed;
        public float InfrastructurePerHeadSeed;
        public bool Seeded;
    }

    public static class EnvironmentFamily
    {
        /// <remarks>[AUTHORED-DRAFT] - the sector intensities' fall per point of carbon tax above the seed's rate (a tenth of a percent of the intensity per point); checked against the six's own spread - Poland's 3.21 t against France's 0.35 is the mix, not the tax.</remarks>
        public const float CarbonTaxElasticityPerPoint = 0.004f;
        /// <remarks>[AUTHORED-DRAFT] - the power intensity's elasticity to real energy spending per head against its seed (the energy line buys the transition).</remarks>
        public const float PowerEnergyLineElasticity = 0.15f;
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
                case CountryId.Sweden:  s.GhgPerCapita = 4.76f;  s.PowerCo2PerCapita = 0.56f; s.TransportCo2PerCapita = 1.26f; break;
                case CountryId.Germany: s.GhgPerCapita = 8.26f;  s.PowerCo2PerCapita = 2.13f; s.TransportCo2PerCapita = 1.68f; break;
                case CountryId.France:  s.GhgPerCapita = 5.81f;  s.PowerCo2PerCapita = 0.35f; s.TransportCo2PerCapita = 1.79f; break;
                case CountryId.Italy:   s.GhgPerCapita = 6.36f;  s.PowerCo2PerCapita = 1.43f; s.TransportCo2PerCapita = 1.74f; break;
                case CountryId.Poland:  s.GhgPerCapita = 9.67f;  s.PowerCo2PerCapita = 3.21f; s.TransportCo2PerCapita = 1.85f; break;
                case CountryId.USA:     s.GhgPerCapita = 17.61f; s.PowerCo2PerCapita = 4.35f; s.TransportCo2PerCapita = 5.08f; break;
                default: return;
            }
            EconomyState st = country.State;
            st.PowerCo2PerCapita = s.PowerCo2PerCapita;
            st.TransportCo2PerCapita = s.TransportCo2PerCapita;
            s.CarbonTaxRateSeed = CarbonTaxRate(country);
            s.EnergyPerHeadSeed = PerHead(country, SpendingCategory.Energy, SpendingCategory.ClimateAndEnvironment);
            s.InfrastructurePerHeadSeed = PerHead(country, SpendingCategory.InfrastructureAndDevelopment, SpendingCategory.Transportation);
            s.Seeded = true;
        }

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

        public readonly struct Targets
        {
            public readonly float Power, Transport;
            public Targets(float power, float transport) { Power = power; Transport = transport; }
        }

        /// <summary>The intensities' targets: the seed times (1 − e × the carbon tax's points above its seed), floored, times the spending terms.</summary>
        public static Targets TargetsFor(Country country, float carbonTaxRate, float energyPerHead, float infrastructurePerHead)
        {
            EnvironmentSeeds s = country.Environment;
            float taxFactor = Mathf.Max(0.1f, 1f - CarbonTaxElasticityPerPoint * (carbonTaxRate - s.CarbonTaxRateSeed));
            float energyRatio = s.EnergyPerHeadSeed > 0f ? Mathf.Max(0.01f, energyPerHead / s.EnergyPerHeadSeed) : 1f;
            float infraRatio = s.InfrastructurePerHeadSeed > 0f ? Mathf.Max(0.01f, infrastructurePerHead / s.InfrastructurePerHeadSeed) : 1f;
            float power = Mathf.Clamp(s.PowerCo2PerCapita * taxFactor * Mathf.Pow(1f / energyRatio, PowerEnergyLineElasticity), MinIntensity, MaxIntensity);
            float transport = Mathf.Clamp(s.TransportCo2PerCapita * taxFactor * Mathf.Pow(1f / infraRatio, TransportInfrastructureElasticity), MinIntensity, MaxIntensity);
            return new Targets(power, transport);
        }

        public static void AdvanceYear(Country country)
        {
            EnvironmentSeeds s = country.Environment;
            if (s == null || !s.Seeded) { return; }
            EconomyState st = country.State;
            Targets t = TargetsFor(country, CarbonTaxRate(country), PerHead(country, SpendingCategory.Energy, SpendingCategory.ClimateAndEnvironment), PerHead(country, SpendingCategory.InfrastructureAndDevelopment, SpendingCategory.Transportation));
            st.PowerCo2PerCapita = Mathf.Clamp(st.PowerCo2PerCapita + (t.Power - st.PowerCo2PerCapita) * ReversionPerYear, MinIntensity, MaxIntensity);
            st.TransportCo2PerCapita = Mathf.Clamp(st.TransportCo2PerCapita + (t.Transport - st.TransportCo2PerCapita) * ReversionPerYear, MinIntensity, MaxIntensity);
        }

        /// <summary>The headline, derived: the seed's greenhouse gases per capita with the two sector figures' moves carried through and the other sectors held at their seed share.</summary>
        public static float GhgPerCapitaNow(Country country)
        {
            EnvironmentSeeds s = country.Environment;
            if (s == null || !s.Seeded) { return EnvironmentSeeds.Absent; }
            EconomyState st = country.State;
            return Mathf.Max(0f, s.GhgPerCapita - (s.PowerCo2PerCapita - st.PowerCo2PerCapita) - (s.TransportCo2PerCapita - st.TransportCo2PerCapita));
        }

        /// <summary>Next year's power intensity for a given carbon tax rate - the 5c arrow while a tax draft is live.</summary>
        public static float ProjectPowerCo2(Country country, float carbonTaxRate)
        {
            Targets t = TargetsFor(country, carbonTaxRate, PerHead(country, SpendingCategory.Energy, SpendingCategory.ClimateAndEnvironment), PerHead(country, SpendingCategory.InfrastructureAndDevelopment, SpendingCategory.Transportation));
            float now = country.State.PowerCo2PerCapita;
            return Mathf.Clamp(now + (t.Power - now) * ReversionPerYear, MinIntensity, MaxIntensity);
        }
    }
}
