using UnityEngine;

namespace PoliSim.Data
{
    /// <summary>
    /// P5-B2 (2026-09-05): what a spending line follows between the player's own changes. A line with a
    /// demographic or cyclical driver indexes to that driver's level - pensions to the 65+ cohort, unemployment benefits to the unemployment rate,
    /// education to the 0–19 cohort, health to the age structure - all read off F2's cohort substrate and the
    /// state, never authored. A line with no driver holds its figure - there is NO price term, because the model keeps its book in
    /// constant prices (SimulationManager.IndexSpendingLines says why, with the measurement). (Before P5-B2 every line grew at the
    /// seed's PotentialGrowthRate and nothing else - §312 measured it: no line indexed to prices, none followed
    /// the actual economy; the pension and health "pressures" were bounded nudges of at most 0.5 % a year.)
    /// </summary>
    public enum SpendingDriver
    {
        None,
        Population,
        Elderly65Plus,
        Youth0To19,
        WorkingAge20To64,
        Students15To29,
        AgeCostIndex,
        UnemploymentRate
    }

    public static class SpendingDrivers
    {
        /// <summary>The line's driver, by category. The mapping is stated once here; a category not listed has no driver.
        /// Sources for the pairing: pensions to the 65+ cohort (the pension line IS a headcount times a benefit); unemployment
        /// benefits and labour-market programmes to the unemployment rate (the caseload); education and student aid to the
        /// cohorts in school and in study; health and social care to the age-cost index (OECD age-cost profiles: spending per
        /// head rises steeply past 65); sickness and disability to the working-age cohort (the insured population); family
        /// and children to the 0–19 cohort; municipal grants and justice to the population. Defence, aid, culture, the EU fee,
        /// administration: no driver.</summary>
        public static SpendingDriver Of(SpendingCategory category)
        {
            switch (category)
            {
                case SpendingCategory.SocialSecurity:
                case SpendingCategory.FederalRetirement:
                case SpendingCategory.VeteransBenefitsMandatory:
                    return SpendingDriver.Elderly65Plus;
                case SpendingCategory.Medicare:
                case SpendingCategory.Medicaid:
                case SpendingCategory.HHSDiscretionary:
                case SpendingCategory.HealthcareAndSocialCare:
                    return SpendingDriver.AgeCostIndex;
                case SpendingCategory.IncomeSecurity:
                case SpendingCategory.Labor:
                case SpendingCategory.LaborMarket:
                case SpendingCategory.SocialPrograms:
                    return SpendingDriver.UnemploymentRate;
                case SpendingCategory.Education:
                case SpendingCategory.FamilyAndChildren:
                    return SpendingDriver.Youth0To19;
                case SpendingCategory.StudentAid:
                    return SpendingDriver.Students15To29;
                case SpendingCategory.SicknessAndDisability:
                    return SpendingDriver.WorkingAge20To64;
                case SpendingCategory.MunicipalGrants:
                case SpendingCategory.Justice:
                case SpendingCategory.PublicServices:
                case SpendingCategory.Housing:
                    return SpendingDriver.Population;
                default:
                    return SpendingDriver.None;
            }
        }

        /// <summary>The driver's current level for a country - a headcount, an index or a rate. The indexation applies the RATIO of
        /// the level now to the level at the last index, so the unit does not matter and a driver that does not move is a factor of 1.</summary>
        public static float Level(SpendingDriver driver, Country country)
        {
            PopulationCohorts cohorts = country.Cohorts;
            switch (driver)
            {
                case SpendingDriver.Population: return cohorts != null ? cohorts.Total : country.State.Population;
                case SpendingDriver.Elderly65Plus: return cohorts != null ? cohorts.InAgeRange(65, 999) : country.State.Population * country.State.DependencyRatio / 100f;
                case SpendingDriver.Youth0To19: return cohorts != null ? cohorts.InAgeRange(0, 19) : country.State.Population;
                case SpendingDriver.WorkingAge20To64: return cohorts != null ? cohorts.InAgeRange(20, 64) : country.State.Population;
                case SpendingDriver.Students15To29: return cohorts != null ? cohorts.InAgeRange(15, 29) : country.State.Population;
                case SpendingDriver.AgeCostIndex: return cohorts != null ? AgeCostIndex(cohorts) : country.State.Population;
                case SpendingDriver.UnemploymentRate: return Mathf.Max(0.5f, country.State.Unemployment);   // a rate; the floor keeps the ratio finite at full employment
                default: return 1f;
            }
        }

        /// <summary>[AUTHORED-DRAFT] age-cost weights, DIRECTIONAL to the OECD's age-cost profiles for health and long-term care
        /// (spending per head near 0.7 of the working-age figure for the young, 1 for 20–64, about 2.5 for 65–79 and 4 and more past 80).
        /// The index is the weighted headcount; only its RATIO over time enters the line.</summary>
        public const float YoungCostWeight = 0.7f;
        public const float WorkingCostWeight = 1f;
        public const float OldCostWeight = 2.5f;
        public const float OldestCostWeight = 4f;

        public static float AgeCostIndex(PopulationCohorts cohorts)
        {
            return cohorts.InAgeRange(0, 19) * YoungCostWeight
                 + cohorts.InAgeRange(20, 64) * WorkingCostWeight
                 + cohorts.InAgeRange(65, 79) * OldCostWeight
                 + cohorts.InAgeRange(80, 999) * OldestCostWeight;
        }

        /// <summary>P5-B5: the driver's short name for the row's instrument band (caption mono, a fifth of the track at 1280).</summary>
        public static string Short(SpendingDriver driver)
        {
            switch (driver)
            {
                case SpendingDriver.Population: return "POPULATION";
                case SpendingDriver.Elderly65Plus: return "65+";
                case SpendingDriver.Youth0To19: return "0–19";
                case SpendingDriver.WorkingAge20To64: return "20–64";
                case SpendingDriver.Students15To29: return "15–29";
                case SpendingDriver.AgeCostIndex: return "AGE COST";
                case SpendingDriver.UnemploymentRate: return "JOBLESS RATE";
                default: return "NO DRIVER";
            }
        }

        public static string Name(SpendingDriver driver)
        {
            switch (driver)
            {
                case SpendingDriver.Population: return "population";
                case SpendingDriver.Elderly65Plus: return "the 65+ cohort";
                case SpendingDriver.Youth0To19: return "the 0–19 cohort";
                case SpendingDriver.WorkingAge20To64: return "the 20–64 cohort";
                case SpendingDriver.Students15To29: return "the 15–29 cohort";
                case SpendingDriver.AgeCostIndex: return "the age-cost index";
                case SpendingDriver.UnemploymentRate: return "the unemployment rate";
                default: return "no driver";
            }
        }
    }
}
