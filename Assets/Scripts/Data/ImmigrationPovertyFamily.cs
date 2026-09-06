using UnityEngine;

namespace PoliSim.Data
{
    /// <summary>
    /// P5-C6 (2026-09-06) - THE IMMIGRATION-AND-POVERTY-DEPTH FAMILY, the fifth society-stat family, on 9c's grammar inherited by shape ("irregular
    /// migration → KEY·OPEN with TWO-DEFINITION chip (Eurostat / DHS) and ABSENT where a year has no estimate · poverty gap % → KEY·BOUNDED ·
    /// underemployment % → KEY·BOUNDED · homelessness → KEY·OPEN with a DEFINITION chip per country"), from the spine
    /// `IMMIGRATION_POVERTY_FAMILY_SPINE.md`. TWO DEFINITIONS WHERE THE SOURCES HAVE TWO, STATED: the five carry Eurostat's FLOW of third-country
    /// nationals found illegally present in the year, the USA the DHS STOCK of unauthorized residents - never one axis; the five's poverty gap is
    /// Eurostat's median gap at the 60 % line, the USA's the OECD's (which reads ten points higher for the same countries), each captioned with its
    /// vintage. Homelessness carries each country's own definition and year (the OECD's table of who counts what).
    ///
    /// THE COUPLINGS ARE READOUTS, NOT FEEDBACK: the family reads the Immigration Policy and Border Enforcement dials, the welfare programmes'
    /// generosity and the minimum wage, the unemployment gap, the housing lines and housing overburden - and moves its own four figures.
    /// </summary>
    public sealed class MigrationPovertySeeds
    {
        public const float Absent = -1f;
        public bool MigrationIsStock;               // false: Eurostat flow per 10 000 in the year; true: the DHS resident stock per 10 000
        public float IrregularMigrationPer10k = Absent;
        public int MigrationYear;
        public float PovertyGap = Absent;           // % below the 60 % line - Eurostat's median gap (five) or the OECD's (USA)
        public bool PovertyGapIsOecd;
        public float PovertyGapOecd = Absent;       // the OECD's reading for the five, the second reading beside Eurostat's
        public int PovertyGapYear;
        public float Underemployment = Absent;      // % of employment
        public int UnderemploymentYear;
        public float HomelessPer10k = Absent;
        public int HomelessYear;
        public string HomelessDefinition = "";
        // bases
        public float ImmigrationPolicySeed = 50f, BorderEnforcementSeed = 50f, GenerositySeed, MinimumWageSeed, UnemploymentSeed, HousingPerHeadSeed, OverburdenSeed;
        public bool Seeded;
    }

    public static class MigrationPovertyFamily
    {
        /// <remarks>[AUTHORED-DRAFT] - the flow's response per 50 dial points: openness lowers irregular entry (legal channels widen), enforcement raises apprehensions; for the STOCK the signs turn (openness raises residence, enforcement lowers it).</remarks>
        public const float MigrationDialElasticity = 0.3f;
        /// <remarks>[AUTHORED-DRAFT] - the poverty gap's fall per 100 points of mean welfare generosity above the seed (generosity closes the GAP before it moves the rate - the catalog's line).</remarks>
        public const float GapGenerosityElasticity = 0.3f;
        /// <remarks>[AUTHORED-DRAFT] - the poverty gap's elasticity to the minimum wage's Kaitz share against its seed.</remarks>
        public const float GapMinimumWageElasticity = 0.2f;
        /// <remarks>[AUTHORED-DRAFT] - underemployment's rise per point of unemployment above its seed (the cycle's slack).</remarks>
        public const float UnderemploymentPerPointOfUnemployment = 0.3f;
        /// <remarks>[AUTHORED-DRAFT] - homelessness' elasticity to housing overburden against its seed, and to real housing spending per head against its seed.</remarks>
        public const float HomelessOverburdenElasticity = 0.5f, HomelessHousingElasticity = 0.3f;
        /// <remarks>[AUTHORED-DRAFT] - yearly reversions.</remarks>
        public const float ReversionPerYear = 0.3f, HomelessReversionPerYear = 0.2f;
        /// <remarks>CONVENTION - runaway guards outside anything the couplings reach; the instruments' bands are the family's stated ranges.</remarks>
        public const float MinFigure = 0.01f, MaxMigration = 2000f, MaxGap = 90f, MaxUnderemployment = 40f, MaxHomeless = 500f;

        public static void SeedAll(World world) { foreach (Country c in world.Countries) { Seed(c); } }

        public static void Seed(Country country)
        {
            MigrationPovertySeeds s = country.MigrationPoverty;
            switch (country.Id)
            {
                // IMMIGRATION_POVERTY_FAMILY_SPINE.md §1 (Eurostat migr_eipre 2024 flow per 10 000; DHS OHSS stock 1 Jan 2022), §2 (Eurostat ilc_li11 2025 MED_EI B_60; OECD IDD PG_INC_DISP PL_60),
                // §3 (Eurostat lfsi_sup_a / lfsi_emp_a 2024; BLS 2024), §4 (OECD AHD HC3.1.A1).
                case CountryId.Sweden:  Set(s, false, 2.8f, 2024, 23.1f, false, 22.7f, 2025, 3.60f, 2024, 33f, 2017, "SURVEY 2017 · PIT · NO CHILDREN"); break;
                case CountryId.Germany: Set(s, false, 29.9f, 2024, 21.7f, false, 31.6f, 2025, 1.18f, 2024, 31f, 2022, "REPORTING ACT 2022 · PIT · CHILDREN"); break;
                case CountryId.France:  Set(s, false, 20.8f, 2024, 20.6f, false, 25.7f, 2025, 4.11f, 2024, 49f, 2022, "DIHAL 2022 · PIT · ASYLUM COUNTED"); break;
                case CountryId.Italy:   Set(s, false, 18.5f, 2024, 24.6f, false, 32.6f, 2025, 2.41f, 2024, 16f, 2021, "ISTAT 2021 · A FLOW, NOT A COUNT"); break;
                case CountryId.Poland:  Set(s, false, 4.4f, 2024, 19.7f, false, 27.0f, 2025, 0.83f, 2024, 8f, 2019, "COUNT 2019 · PIT · CHILDREN"); break;
                case CountryId.USA:     Set(s, true, 326f, 2022, 37.2f, true, 37.2f, 2023, 2.77f, 2024, 19f, 2023, "HUD PIT 2023 · CHILDREN"); break;
                default: return;
            }
            EconomyState st = country.State;
            st.IrregularMigrationPer10k = s.IrregularMigrationPer10k;
            st.PovertyGap = s.PovertyGap;
            st.Underemployment = s.Underemployment;
            st.HomelessPer10k = s.HomelessPer10k;
            s.ImmigrationPolicySeed = country.ImmigrationPolicyLevel;
            s.BorderEnforcementSeed = country.BorderEnforcementLevel;
            s.GenerositySeed = MeanGenerosity(country);
            s.MinimumWageSeed = country.MinimumWageImplemented ? country.MinimumWagePercentOfMedian : 0f;
            s.UnemploymentSeed = st.Unemployment;
            s.HousingPerHeadSeed = HousingPerHead(country);
            s.OverburdenSeed = country.TracksHousingOverburden ? Mathf.Max(0.1f, st.HousingOverburden) : 0f;
            s.Seeded = true;
        }

        private static void Set(MigrationPovertySeeds s, bool stock, float migration, int migrationYear, float gap, bool gapOecd, float gapOecdReading, int gapYear, float underemployment, int underYear, float homeless, int homelessYear, string definition)
        {
            s.MigrationIsStock = stock; s.IrregularMigrationPer10k = migration; s.MigrationYear = migrationYear;
            s.PovertyGap = gap; s.PovertyGapIsOecd = gapOecd; s.PovertyGapOecd = gapOecdReading; s.PovertyGapYear = gapYear;
            s.Underemployment = underemployment; s.UnderemploymentYear = underYear;
            s.HomelessPer10k = homeless; s.HomelessYear = homelessYear; s.HomelessDefinition = definition;
        }

        /// <summary>The mean generosity of the implemented welfare programmes (0..100), 0 when none.</summary>
        public static float MeanGenerosity(Country country)
        {
            float sum = 0f; int n = 0;
            foreach (WelfareProgram p in country.WelfarePrograms) { if (p.IsImplemented) { sum += p.GenerosityLevel; n++; } }
            return n > 0 ? sum / n : 0f;
        }

        /// <summary>A housing line: the USA's Housing and the five's regional planning and housing line. [AUTHORED-DRAFT] by the line's subject.</summary>
        public static bool IsHousingLine(SpendingCategory category) => category == SpendingCategory.Housing || category == SpendingCategory.RegionalPlanningAndDevelopment;

        public static float HousingPerHead(Country country)
        {
            float sum = 0f;
            foreach (SpendingLine line in country.SpendingLines) { if (IsHousingLine(line.Category)) { sum += line.Amount; } }
            return sum / Mathf.Max(0.0001f, country.State.PriceLevel) / Mathf.Max(0.0001f, SpendingDrivers.Level(SpendingDriver.Population, country));
        }

        public readonly struct Targets
        {
            public readonly float Migration, Gap, Underemployment, Homeless;
            public Targets(float migration, float gap, float underemployment, float homeless) { Migration = migration; Gap = gap; Underemployment = underemployment; Homeless = homeless; }
        }

        public static Targets TargetsFor(Country country)
        {
            MigrationPovertySeeds s = country.MigrationPoverty;
            EconomyState st = country.State;
            float openness = (country.ImmigrationPolicyLevel - s.ImmigrationPolicySeed) / 50f;
            float enforcement = (country.BorderEnforcementLevel - s.BorderEnforcementSeed) / 50f;
            float migration = s.MigrationIsStock
                ? s.IrregularMigrationPer10k * (1f + MigrationDialElasticity * openness) * (1f - MigrationDialElasticity * enforcement)
                : s.IrregularMigrationPer10k * (1f - MigrationDialElasticity * openness) * (1f + MigrationDialElasticity * enforcement);
            float generosityTerm = 1f - GapGenerosityElasticity * (MeanGenerosity(country) - s.GenerositySeed) / 100f;
            float wageRatio = s.MinimumWageSeed > 0f && country.MinimumWageImplemented ? Mathf.Max(0.1f, country.MinimumWagePercentOfMedian / s.MinimumWageSeed) : 1f;
            float gap = s.PovertyGap * Mathf.Max(0.1f, generosityTerm) * Mathf.Pow(1f / wageRatio, GapMinimumWageElasticity);
            float underemployment = s.Underemployment + UnderemploymentPerPointOfUnemployment * (st.Unemployment - s.UnemploymentSeed);
            float overburdenTerm = s.OverburdenSeed > 0f && country.TracksHousingOverburden ? 1f + HomelessOverburdenElasticity * (st.HousingOverburden - s.OverburdenSeed) / s.OverburdenSeed : 1f;
            float housingRatio = s.HousingPerHeadSeed > 0f ? Mathf.Max(0.01f, HousingPerHead(country) / s.HousingPerHeadSeed) : 1f;
            float homeless = s.HomelessPer10k * Mathf.Max(0.1f, overburdenTerm) * Mathf.Pow(1f / housingRatio, HomelessHousingElasticity);
            return new Targets(Mathf.Clamp(migration, MinFigure, MaxMigration), Mathf.Clamp(gap, MinFigure, MaxGap), Mathf.Clamp(underemployment, MinFigure, MaxUnderemployment), Mathf.Clamp(homeless, MinFigure, MaxHomeless));
        }

        public static void AdvanceYear(Country country)
        {
            MigrationPovertySeeds s = country.MigrationPoverty;
            if (s == null || !s.Seeded) { return; }
            EconomyState st = country.State;
            Targets t = TargetsFor(country);
            st.IrregularMigrationPer10k = Mathf.Clamp(st.IrregularMigrationPer10k + (t.Migration - st.IrregularMigrationPer10k) * ReversionPerYear, MinFigure, MaxMigration);
            st.PovertyGap = Mathf.Clamp(st.PovertyGap + (t.Gap - st.PovertyGap) * ReversionPerYear, MinFigure, MaxGap);
            st.Underemployment = Mathf.Clamp(st.Underemployment + (t.Underemployment - st.Underemployment) * ReversionPerYear, MinFigure, MaxUnderemployment);
            st.HomelessPer10k = Mathf.Clamp(st.HomelessPer10k + (t.Homeless - st.HomelessPer10k) * HomelessReversionPerYear, MinFigure, MaxHomeless);
        }
    }
}
