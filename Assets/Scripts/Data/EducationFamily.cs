using UnityEngine;

namespace PoliSim.Data
{
    /// <summary>
    /// P5-C3 (2026-09-06) - THE EDUCATION FAMILY, the second society-stat family, on Design's grammar (board 9c, inherited by shape:
    /// "PISA mean → KEY·OPEN (band 350–550, higher ◂) · graduation % → KEY·BOUNDED · early leavers % → KEY·BOUNDED, lower ◂ · attainment by
    /// level → DISTRIBUTION (2a's stacked bar, one row) · students/teacher → CARD-ONLY key") from the spine `EDUCATION_FAMILY_SPINE.md`.
    /// Per country the seeds the OECD and Eurostat flows gave (attainment by level 25–64, early leavers 18–24, students per teacher) and the
    /// bases the couplings measure against (real education spending per pupil at the seed). PISA and the graduation rate are FETCHES with no
    /// score (ruled 2026-09-06): their rows print the word and the source, and nothing here carries a figure for them.
    ///
    /// THE COUPLINGS ARE READOUTS, NOT FEEDBACK (as the health family's): the family reads the education lines over the 0–19 cohort and
    /// youth unemployment, and moves its own figures. The spine's proposed feedback (attainment → the productivity trend) is NOT built.
    /// ABSENT IS A STATE: the USA's early leavers are Eurostat's series and the USA is not in it - -1, the word on the instrument.
    /// </summary>
    public sealed class EducationSeeds
    {
        public const float Absent = -1f;

        // ---- SOURCED (the spine's tables; OECD EAG DF_LSO_NEAC_DISTR_EA 25–64, Eurostat edat_lfse_14 18–24, OECD EAG DF_UOE_NF_PERS_STR) ----
        public float BelowUpperSecondary = Absent;   // ISCED 0–2, % of 25–64
        public float UpperSecondary = Absent;        // ISCED 3–4
        public float Tertiary = Absent;              // ISCED 5–8
        public int AttainmentYear;
        public float EarlyLeavers = Absent;          // % of 18–24 (Eurostat); the USA absent
        public int EarlyLeaversYear;
        public float StudentsPerTeacherPrimary = Absent;         // ISCED 1
        public float StudentsPerTeacherLowerSecondary = Absent;  // ISCED 2
        public int StudentsPerTeacherYear;

        // ---- BASES captured at the seed ----
        public float SpendPerPupilSeed;
        public float YouthUnemploymentSeed;
        public bool Seeded;

        public bool HasEarlyLeavers => EarlyLeavers >= 0f;
    }

    public static class EducationFamily
    {
        // ---- THE COUPLING CONSTANTS - every one [AUTHORED-DRAFT], the spine's §5 lines ----
        /// <remarks>[AUTHORED-DRAFT] - the students-per-teacher ratio's elasticity to real spending per pupil against its seed (ratio_target = seed x (seed/now)^s); fast reversion.</remarks>
        public const float TeacherRatioElasticity = 0.6f;
        /// <remarks>[AUTHORED-DRAFT] - a fast yearly reversion for the ratio (teachers are hired within a budget year).</remarks>
        public const float TeacherRatioReversionPerYear = 0.6f;
        /// <remarks>[AUTHORED-DRAFT] - early leavers' elasticity to youth unemployment against its seed (the pull out of school).</remarks>
        public const float LeaversYouthUnemploymentElasticity = 0.3f;
        /// <remarks>[AUTHORED-DRAFT] - early leavers' elasticity to real spending per pupil against its seed (the push to stay).</remarks>
        public const float LeaversSpendingElasticity = 0.3f;
        /// <remarks>[AUTHORED-DRAFT] - early leavers' yearly reversion.</remarks>
        public const float LeaversReversionPerYear = 0.3f;
        /// <remarks>[AUTHORED-DRAFT] - the attainment stock turns over one cohort a year: a fortieth of the 25–64 population; the below-upper-secondary share drifts toward
        /// the leavers' level against its seed at that pace, and the two shares above it keep their proportion. The one metric here that cannot move fast.</remarks>
        public const float AttainmentCohortShare = 1f / 40f;
        /// <remarks>CONVENTION - runaway guards outside anything the couplings reach; the instruments' bands are the family's stated ranges (9c).</remarks>
        public const float MinRatio = 3f, MaxRatio = 60f, MinLeavers = 0.5f, MaxLeavers = 60f;

        public static void SeedAll(World world) { foreach (Country c in world.Countries) { Seed(c); } }

        public static void Seed(Country country)
        {
            EducationSeeds s = country.Education;
            switch (country.Id)
            {
                // EDUCATION_FAMILY_SPINE.md §1 (attainment, OECD EAG), §2 (early leavers, Eurostat edat_lfse_14, 2025), §3 (students per teacher, OECD EAG).
                case CountryId.Sweden:  Set(s, 14.0f, 35.2f, 50.8f, 2025, 6.7f, 2025, 12.4f, 11.2f, 2024); break;
                case CountryId.Germany: Set(s, 14.1f, 50.5f, 35.5f, 2025, 13.1f, 2025, 15.2f, 12.9f, 2024); break;
                case CountryId.France:  Set(s, 16.1f, 40.6f, 43.4f, 2024, 7.2f, 2025, 18.1f, 14.7f, 2023); break;
                case CountryId.Italy:   Set(s, 33.0f, 44.7f, 22.3f, 2025, 8.2f, 2025, 10.5f, 10.4f, 2024); break;
                case CountryId.Poland:  Set(s, 5.1f, 54.9f, 40.0f, 2025, 4.0f, 2025, 13.0f, 9.5f, 2024); break;
                case CountryId.USA:     Set(s, 7.7f, 40.1f, 52.2f, 2025, EducationSeeds.Absent, 0, 13.7f, 14.3f, 2024); break;
                default: return;
            }
            EconomyState st = country.State;
            st.AttainmentBelowUpperSecondary = s.BelowUpperSecondary;
            st.AttainmentUpperSecondary = s.UpperSecondary;
            st.AttainmentTertiary = s.Tertiary;
            st.EarlyLeavers = s.EarlyLeavers;
            st.StudentsPerTeacherPrimary = s.StudentsPerTeacherPrimary;
            st.StudentsPerTeacherLowerSecondary = s.StudentsPerTeacherLowerSecondary;
            s.SpendPerPupilSeed = SpendPerPupil(country);
            s.YouthUnemploymentSeed = Mathf.Max(0.1f, st.YouthUnemployment);
            s.Seeded = true;
        }

        private static void Set(EducationSeeds s, float below, float upper, float tertiary, int attainmentYear, float leavers, int leaversYear, float primary, float lowerSecondary, int ratioYear)
        {
            s.BelowUpperSecondary = below; s.UpperSecondary = upper; s.Tertiary = tertiary; s.AttainmentYear = attainmentYear;
            s.EarlyLeavers = leavers; s.EarlyLeaversYear = leaversYear;
            s.StudentsPerTeacherPrimary = primary; s.StudentsPerTeacherLowerSecondary = lowerSecondary; s.StudentsPerTeacherYear = ratioYear;
        }

        /// <summary>An education line: the Education ministry's lines (Effectiveness.PortfolioOf) - the five's Education line and the USA's Education and student aid.</summary>
        public static bool IsEducationLine(SpendingCategory category) => Effectiveness.PortfolioOf(category) == CabinetPortfolio.Education;

        public static float EducationSpendingNominal(Country country)
        {
            float sum = 0f;
            foreach (SpendingLine line in country.SpendingLines) { if (IsEducationLine(line.Category)) { sum += line.Amount; } }
            return sum;
        }

        public static float EducationSpendingReal(Country country) => EducationSpendingNominal(country) / Mathf.Max(0.0001f, country.State.PriceLevel);

        /// <summary>Real education spending over the 0–19 cohort (P5-B2's driver for the line) - money per pupil; a smaller cohort with the same line is more per pupil.</summary>
        public static float SpendPerPupil(Country country)
            => EducationSpendingReal(country) / Mathf.Max(0.0001f, SpendingDrivers.Level(SpendingDriver.Youth0To19, country));

        public readonly struct Targets
        {
            public readonly float RatioFactor, Leavers;
            public Targets(float ratioFactor, float leavers) { RatioFactor = ratioFactor; Leavers = leavers; }
        }

        public static Targets TargetsFor(Country country, float educationSpendingReal)
        {
            EducationSeeds s = country.Education;
            float pupils = Mathf.Max(0.0001f, SpendingDrivers.Level(SpendingDriver.Youth0To19, country));
            float perPupilRatio = s.SpendPerPupilSeed > 0f ? (educationSpendingReal / pupils) / s.SpendPerPupilSeed : 1f;
            perPupilRatio = Mathf.Max(0.01f, perPupilRatio);
            float youthRatio = s.YouthUnemploymentSeed > 0f ? Mathf.Max(0.1f, country.State.YouthUnemployment) / s.YouthUnemploymentSeed : 1f;
            float ratioFactor = Mathf.Pow(1f / perPupilRatio, TeacherRatioElasticity);
            float leavers = s.EarlyLeavers >= 0f ? s.EarlyLeavers * Mathf.Pow(youthRatio, LeaversYouthUnemploymentElasticity) * Mathf.Pow(1f / perPupilRatio, LeaversSpendingElasticity) : EducationSeeds.Absent;
            return new Targets(ratioFactor, leavers);
        }

        /// <summary>The yearly step at the turn boundary: the teacher ratios revert fast to their spending target; leavers revert to youth unemployment and spending;
        /// the attainment stock turns over one cohort a year, the below-upper-secondary share following the leavers against their seed, the shares above keeping their
        /// proportion so the three always sum to 100. An absent leavers figure stays absent and the stock then holds.</summary>
        public static void AdvanceYear(Country country)
        {
            EducationSeeds s = country.Education;
            if (s == null || !s.Seeded) { return; }
            EconomyState st = country.State;
            Targets t = TargetsFor(country, EducationSpendingReal(country));
            float primaryTarget = Mathf.Clamp(s.StudentsPerTeacherPrimary * t.RatioFactor, MinRatio, MaxRatio);
            float lowerTarget = Mathf.Clamp(s.StudentsPerTeacherLowerSecondary * t.RatioFactor, MinRatio, MaxRatio);
            st.StudentsPerTeacherPrimary = Mathf.Clamp(st.StudentsPerTeacherPrimary + (primaryTarget - st.StudentsPerTeacherPrimary) * TeacherRatioReversionPerYear, MinRatio, MaxRatio);
            st.StudentsPerTeacherLowerSecondary = Mathf.Clamp(st.StudentsPerTeacherLowerSecondary + (lowerTarget - st.StudentsPerTeacherLowerSecondary) * TeacherRatioReversionPerYear, MinRatio, MaxRatio);
            if (s.HasEarlyLeavers && st.EarlyLeavers >= 0f)
            {
                float leaversTarget = Mathf.Clamp(t.Leavers, MinLeavers, MaxLeavers);
                st.EarlyLeavers = Mathf.Clamp(st.EarlyLeavers + (leaversTarget - st.EarlyLeavers) * LeaversReversionPerYear, MinLeavers, MaxLeavers);
                // The stock: one cohort a year enters at the leavers' level against the seed's, one leaves at the stock's own share.
                float belowTarget = Mathf.Clamp(s.BelowUpperSecondary * (st.EarlyLeavers / Mathf.Max(0.1f, s.EarlyLeavers)), 0.5f, 90f);
                float below = st.AttainmentBelowUpperSecondary + (belowTarget - st.AttainmentBelowUpperSecondary) * AttainmentCohortShare;
                float aboveSeed = Mathf.Max(0.0001f, st.AttainmentUpperSecondary + st.AttainmentTertiary);
                float tertiaryShare = st.AttainmentTertiary / aboveSeed;
                float above = Mathf.Max(0f, 100f - below);
                st.AttainmentBelowUpperSecondary = below;
                st.AttainmentTertiary = above * tertiaryShare;
                st.AttainmentUpperSecondary = above * (1f - tertiaryShare);
            }
        }

        /// <summary>Next year's primary students-per-teacher ratio for a given nominal education spending - the 5c arrow while a draft is live.</summary>
        public static float ProjectStudentsPerTeacher(Country country, float educationSpendingNominal)
        {
            Targets t = TargetsFor(country, educationSpendingNominal / Mathf.Max(0.0001f, country.State.PriceLevel));
            float target = Mathf.Clamp(country.Education.StudentsPerTeacherPrimary * t.RatioFactor, MinRatio, MaxRatio);
            float now = country.State.StudentsPerTeacherPrimary;
            return Mathf.Clamp(now + (target - now) * TeacherRatioReversionPerYear, MinRatio, MaxRatio);
        }

        /// <summary>At least upper secondary, 25–64 - the general attainment key, derived from the distribution (100 − ISCED 0–2).</summary>
        public static float AtLeastUpperSecondary(Country country) => 100f - country.State.AttainmentBelowUpperSecondary;
    }
}
