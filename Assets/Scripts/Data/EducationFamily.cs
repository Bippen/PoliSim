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

        // ---- THE FEEDBACK PASS (2026-09-07): the sourced gaps the two terms read - each country's own, none authored ----
        public float ActivityGapBelowToUpper = Absent;   // activity rate ISCED 3-4 minus ISCED 0-2, 25–64, points (Eurostat lfsa_argaed 2024; the USA: BLS LNS11327660 − LNS11327659, 25+, 2024 mean)
        public float RelEarnBelow = Absent, RelEarnUpper = Absent, RelEarnTertiary = Absent;   // earnings relative to upper secondary = 100, 25–64 (OECD EAG DF_LSO_EARN_REL_UPPER 2023/24; France: Eurostat earn_ses22_16 hourly, stated)
        public float WageIndexLastYear;                  // the attainment-weighted relative-earnings index at the last yearly step (the seed's at the seed)
        public float ProductivityTermPoints;             // points of trend productivity growth the last yearly step produced - zero at the seed, read by MacroSystem.ApplySectorGrowthEffect the next turn
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
            // The feedback pass's gaps (EDUCATION_FAMILY_SPINE.md §8): activity by attainment, earnings by attainment - the country's own figures, read at the source.
            switch (country.Id)
            {
                case CountryId.Sweden:  Gaps(s, 88.7f - 78.2f, 77.9f, 100.5f, 124.3f); break;
                case CountryId.Germany: Gaps(s, 85.7f - 69.9f, 79.2f, 105.2f, 155.9f); break;
                case CountryId.France:  Gaps(s, 80.0f - 62.0f, 100f * 14.6f / 16.3f, 100f, 100f * 25.4f / 16.3f); break;   // not in the OECD flow: Eurostat SES 2022 hourly earnings, ISCED 0-2 / 3-4 / 5-8 (14.60 / 16.30 / 25.40 EUR)
                case CountryId.Italy:   Gaps(s, 78.2f - 60.5f, 76.7f, 100.0f, 139.2f); break;
                case CountryId.Poland:  Gaps(s, 77.9f - 53.6f, 88.2f, 100.2f, 150.9f); break;
                case CountryId.USA:     Gaps(s, 56.91f - 47.44f, 72.5f, 100.0f, 174.4f); break;
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
            s.WageIndexLastYear = WageIndex(st, s);
            s.ProductivityTermPoints = 0f;
            s.Seeded = true;
        }

        private static void Gaps(EducationSeeds s, float activityGap, float relBelow, float relUpper, float relTertiary)
        {
            s.ActivityGapBelowToUpper = activityGap; s.RelEarnBelow = relBelow; s.RelEarnUpper = relUpper; s.RelEarnTertiary = relTertiary;
        }

        // ---- THE FEEDBACK PASS (2026-09-07, overnight; the spine's §5 "proposed and NOT built" - built now, one family, its own BASELINE) ----
        // Two terms, both DERIVED from the country's own sourced gaps - no authored magnitude - and both zero at the seed, so the suite's pre-existing
        // fields open only where the attainment stock moves (which is only where the leavers move: the USA's stock holds, its terms stay zero, stated).

        /// <summary>The attainment-weighted relative-earnings index (upper secondary = 100): Σ share × relative earnings ÷ 100. The wage gap between attainment
        /// levels is read as the marginal-product gap (the Mincer reading - the stated approximation, as every earnings-as-productivity reading is).</summary>
        public static float WageIndex(EconomyState st, EducationSeeds s)
        {
            if (s.RelEarnBelow < 0f || s.RelEarnUpper < 0f || s.RelEarnTertiary < 0f) { return 1f; }
            return (st.AttainmentBelowUpperSecondary * s.RelEarnBelow + st.AttainmentUpperSecondary * s.RelEarnUpper + st.AttainmentTertiary * s.RelEarnTertiary) / 10000f;
        }

        /// <summary>Points of trend productivity growth the last yearly attainment step is worth: 100 × ln(index now ÷ index a year ago), clamped to ±0.5 as a runaway
        /// guard. Enters MacroSystem.ApplySectorGrowthEffect's ledger under its existing all-sources ceiling - lagged one turn by construction (the ledger runs before
        /// the family's yearly step), which is the "small, lagged term" the spine proposed.</summary>
        public static float ProductivityTrendTerm(Country country)
        {
            EducationSeeds s = country.Education;
            return s != null && s.Seeded ? s.ProductivityTermPoints : 0f;
        }

        /// <summary>The 25–64 population's share of the 15+ population from the pyramid (bands 25–29 … 60–64 over 15–19 … 100+); 0 without a pyramid.</summary>
        public static float Share25To64Of15Plus(Country country)
        {
            if (country.Cohorts == null) { return 0f; }
            float[] c = country.Cohorts.Counts;
            float mid = 0f, all = 0f;
            for (int i = 3; i < PopulationCohorts.CohortCount; i++) { all += Mathf.Max(0f, c[i]); if (i >= 5 && i <= 12) { mid += Mathf.Max(0f, c[i]); } }
            return all > 0f ? mid / all : 0f;
        }

        /// <summary>The participation term, points on the 15+ rate: −(below-upper-secondary share now − seed) ÷ 100 × the country's own activity gap between ISCED 0-2
        /// and 3-4 (a person who finishes upper secondary participates at the upper-secondary rate) × the 25–64 share of the 15+ population. Zero at the seed.</summary>
        public static float ParticipationTerm(Country country)
        {
            EducationSeeds s = country.Education;
            if (s == null || !s.Seeded || s.ActivityGapBelowToUpper < 0f) { return 0f; }
            float deltaBelow = country.State.AttainmentBelowUpperSecondary - s.BelowUpperSecondary;
            return Mathf.Clamp(-deltaBelow / 100f * s.ActivityGapBelowToUpper * Share25To64Of15Plus(country), -5f, 5f);
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
            // The feedback pass: the attainment-weighted wage index steps with the stock; the trend term is what the year's step is worth (zero when the stock holds).
            float index = WageIndex(st, s);
            s.ProductivityTermPoints = s.WageIndexLastYear > 0f ? Mathf.Clamp(100f * Mathf.Log(index / s.WageIndexLastYear), -0.5f, 0.5f) : 0f;
            s.WageIndexLastYear = index;
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
