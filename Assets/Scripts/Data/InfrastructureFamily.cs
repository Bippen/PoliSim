using UnityEngine;

namespace PoliSim.Data
{
    /// <summary>
    /// P5-C4 (2026-09-06) - THE INFRASTRUCTURE FAMILY, the third society-stat family, on 9c's grammar inherited by shape ("road quality 1–7 →
    /// KEY·BOUNDED with a DATED chip (WEF 2019) · congestion % → KEY·OPEN · road km/1 000 km² → SUPPORTING"), from the spine
    /// `INFRASTRUCTURE_FAMILY_SPINE.md` (the WEF Global Competitiveness Report 2019 dataset, verified by content). Per country: road quality
    /// (ROADINF, the 2019 normalised score - the survey ended with that edition, so the figure is DATED and the instrument prints the year) and
    /// road connectivity (ROADQUALIDX, 0–100 from travel speeds between the ten largest cities). Road length is ABSENT (the IRF World Road
    /// Statistics is paid) and congestion is a FETCH with no figure (the TomTom Traffic Index is a web report).
    ///
    /// THE COUPLINGS ARE READOUTS, NOT FEEDBACK: the family reads the infrastructure lines per head against their seed and the population against
    /// its seed, and moves its own two figures. The spine's proposed feedback (quality → Country.InfrastructureSpendingGrowthAdjustment's visible
    /// face) is NOT built.
    /// </summary>
    public sealed class InfrastructureSeeds
    {
        public const float Absent = -1f;
        public float RoadQuality = Absent;       // WEF GCI 2019 ROADINF score 0–100 (the 1–7 survey normalised), DATED 2019
        public int RoadQualityRank;
        public float RoadConnectivity = Absent;  // WEF GCI 2019 ROADQUALIDX 0–100
        public const int Year = 2019;
        public float SpendPerHeadSeed;
        public float PopulationSeed;
        public bool Seeded;
    }

    public static class InfrastructureFamily
    {
        /// <remarks>[AUTHORED-DRAFT] - the share of road quality that decays a year without spending; the seed's spending rebuilds exactly this much, so the seed holds its score (the spine's claim to check).</remarks>
        public const float QualityDecayPerYear = 0.04f;
        /// <remarks>[AUTHORED-DRAFT] - the rebuild's elasticity to real infrastructure spending per head against its seed.</remarks>
        public const float RebuildElasticity = 0.7f;
        /// <remarks>[AUTHORED-DRAFT] - connectivity follows quality with a lag: a fifth of the gap to its target closes a year.</remarks>
        public const float ConnectivityReversionPerYear = 0.2f;
        /// <remarks>[AUTHORED-DRAFT] - connectivity falls as the population outgrows the network: the elasticity to population against its seed.</remarks>
        public const float ConnectivityPopulationElasticity = 0.3f;
        /// <remarks>CONVENTION - runaway guards on the state; the instruments' bands (50–100, 70–100) are the family's stated ranges.</remarks>
        public const float MinScore = 1f, MaxScore = 100f;

        public static void SeedAll(World world) { foreach (Country c in world.Countries) { Seed(c); } }

        public static void Seed(Country country)
        {
            InfrastructureSeeds s = country.Infrastructure;
            switch (country.Id)
            {
                // WEF_GCI_4.0_2019_Dataset.xlsx - ROADINF 2019 score and rank of 141; ROADQUALIDX 2019 (INFRASTRUCTURE_FAMILY_SPINE.md §1-§2).
                case CountryId.Sweden:  s.RoadQuality = 83.9f; s.RoadQualityRank = 13; s.RoadConnectivity = 95.9f; break;
                case CountryId.Germany: s.RoadQuality = 83.4f; s.RoadQualityRank = 15; s.RoadConnectivity = 95.1f; break;
                case CountryId.France:  s.RoadQuality = 85.3f; s.RoadQualityRank = 10; s.RoadConnectivity = 96.6f; break;
                case CountryId.Italy:   s.RoadQuality = 71.3f; s.RoadQualityRank = 40; s.RoadConnectivity = 85.9f; break;
                case CountryId.Poland:  s.RoadQuality = 71.6f; s.RoadQualityRank = 39; s.RoadConnectivity = 88.0f; break;
                case CountryId.USA:     s.RoadQuality = 87.2f; s.RoadQualityRank = 5;  s.RoadConnectivity = 100f; break;
                default: return;
            }
            country.State.RoadQuality = s.RoadQuality;
            country.State.RoadConnectivity = s.RoadConnectivity;
            s.SpendPerHeadSeed = SpendPerHead(country);
            s.PopulationSeed = Mathf.Max(0.0001f, SpendingDrivers.Level(SpendingDriver.Population, country));
            s.Seeded = true;
        }

        /// <summary>An infrastructure line: the five's InfrastructureAndDevelopment and the USA's Transportation. [AUTHORED-DRAFT] by the line's subject.</summary>
        public static bool IsInfrastructureLine(SpendingCategory category) => category == SpendingCategory.InfrastructureAndDevelopment || category == SpendingCategory.Transportation;

        public static float SpendingNominal(Country country)
        {
            float sum = 0f;
            foreach (SpendingLine line in country.SpendingLines) { if (IsInfrastructureLine(line.Category)) { sum += line.Amount; } }
            return sum;
        }

        public static float SpendingReal(Country country) => SpendingNominal(country) / Mathf.Max(0.0001f, country.State.PriceLevel);

        public static float SpendPerHead(Country country) => SpendingReal(country) / Mathf.Max(0.0001f, SpendingDrivers.Level(SpendingDriver.Population, country));

        /// <summary>The year's rebuild of road quality for a given real infrastructure spending: the decay at the seed's spending, more or less with the ratio,
        /// SATURATING toward the score's ceiling (RF-1, 2026-09-07): scaled by (100 − score now) ÷ (100 − seed), which is exactly 1 at the seed's score - so the
        /// seed's spending holds the seed's score to the digit - and falls toward 0 as the score approaches 100, so more money approaches the ceiling and never sits
        /// on it (a WEF survey score is bounded; a 7 of 7 is not made better by money). Below the seed the factor exceeds 1: a worse network is cheaper to rebuild,
        /// which is the same diminishing-returns statement read the other way. RF-1's probe (COMPLETED.md §354) showed the drift to the cap came from the lines'
        /// own real growth per head (the USA ×4.2, Poland ×13.7 in a century), not from the seed's spending; the form here is the readout's answer to a bounded score.</summary>
        public static float RebuildFor(Country country, float spendingReal)
        {
            InfrastructureSeeds s = country.Infrastructure;
            float heads = Mathf.Max(0.0001f, SpendingDrivers.Level(SpendingDriver.Population, country));
            float ratio = s.SpendPerHeadSeed > 0f ? Mathf.Max(0.01f, (spendingReal / heads) / s.SpendPerHeadSeed) : 1f;
            return s.RoadQuality * QualityDecayPerYear * Mathf.Pow(ratio, RebuildElasticity) * SaturationFactor(country);
        }

        /// <summary>RF-1: (MaxScore − score now) ÷ (MaxScore − the seed's score), 1 at the seed, 0 at the ceiling; a seed at the ceiling reads 1 (nothing to saturate).</summary>
        public static float SaturationFactor(Country country)
        {
            InfrastructureSeeds s = country.Infrastructure;
            float room = MaxScore - s.RoadQuality;
            if (room <= 0.01f) { return 1f; }
            return Mathf.Max(0f, MaxScore - country.State.RoadQuality) / room;
        }

        /// <summary>The yearly step: quality decays by its share and is rebuilt by spending per head against the seed (the seed's spending holds the seed's score);
        /// connectivity drifts toward a target set by quality's ratio to its seed and by the population against its seed.</summary>
        public static void AdvanceYear(Country country)
        {
            InfrastructureSeeds s = country.Infrastructure;
            if (s == null || !s.Seeded) { return; }
            EconomyState st = country.State;
            float decay = st.RoadQuality * QualityDecayPerYear;
            st.RoadQuality = Mathf.Clamp(st.RoadQuality - decay + RebuildFor(country, SpendingReal(country)), MinScore, MaxScore);
            float popRatio = s.PopulationSeed > 0f ? Mathf.Max(0.01f, SpendingDrivers.Level(SpendingDriver.Population, country) / s.PopulationSeed) : 1f;
            float qualityRatio = s.RoadQuality > 0f ? st.RoadQuality / s.RoadQuality : 1f;
            float target = Mathf.Clamp(s.RoadConnectivity * qualityRatio * Mathf.Pow(1f / popRatio, ConnectivityPopulationElasticity), MinScore, MaxScore);
            st.RoadConnectivity = Mathf.Clamp(st.RoadConnectivity + (target - st.RoadConnectivity) * ConnectivityReversionPerYear, MinScore, MaxScore);
        }

        /// <summary>Next year's road quality for a given nominal infrastructure spending - the 5c arrow while a draft is live.</summary>
        public static float ProjectRoadQuality(Country country, float spendingNominal)
        {
            float real = spendingNominal / Mathf.Max(0.0001f, country.State.PriceLevel);
            float now = country.State.RoadQuality;
            return Mathf.Clamp(now - now * QualityDecayPerYear + RebuildFor(country, real), MinScore, MaxScore);
        }
    }
}
