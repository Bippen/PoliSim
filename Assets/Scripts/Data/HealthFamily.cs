using System;
using UnityEngine;

namespace PoliSim.Data
{
    /// <summary>
    /// P5-C2 (2026-09-05, evening) - THE HEALTH FAMILY, the first society-stat family, built on Design's grammar (D15 item 3,
    /// board 9c) from the spine `HEALTH_FAMILY_SPINE.md`. Per country: the seeds the OECD flows gave (coverage, treatable
    /// mortality, two waiting times, five supporting readouts) and the bases the couplings measure against (health spending
    /// per head and per age-cost unit at the seed, the minister's efficiency at the seed). The STATE the family moves lives on
    /// EconomyState (HealthCoverage, TreatableMortality, WaitCataractDays, WaitKneeDays) so the trajectory dump and the
    /// history see it; this class holds the seeds and the arithmetic.
    ///
    /// THE COUPLINGS ARE READOUTS, NOT FEEDBACK. Nothing here writes into the macro block, the ledger or approval: the family
    /// reads the health line, the age-cost index and the minister, and moves its own four figures. The spine's proposed
    /// feedbacks (quality → life expectancy, coverage → approval) are NOT built - they wait for their own pass with the
    /// trajectory suite before and after. So the trajectory suite across this commit is byte-identical on every field that
    /// existed before it; the four new fields are the family.
    ///
    /// ABSENT IS A STATE, NOT A ZERO. A waiting time the country's statistics do not publish (Germany, France, the USA) is
    /// <see cref="Absent"/> (-1) on the state and stays so; the coupling does not run on it and the instrument prints the word.
    /// </summary>
    public sealed class HealthSeeds
    {
        /// <summary>The value an absent figure carries - never printed as a number.</summary>
        public const float Absent = -1f;

        // ---- SOURCED (OECD SDMX, fetched 2026-09-05; the spine's tables) ----
        public float Coverage = Absent;            // % of population covered for core services (HEALTH_PROT · TPRIBASI)
        public float CoveragePublic = Absent;      // % of population under government / compulsory cover (HEALTH_PROT · COVGCMED)
        public float CoverageCeiling = 100f;       // 100 for the five; the USA's own (its split is structural, not a spending outcome)
        public int CoverageYear;
        public float TreatableMortality = Absent;  // deaths per 100 000, age-standardised (HEALTH_STAT · DF_AM · TRTM); lower is better
        public int TreatableMortalityYear;
        public float WaitCataract = Absent;        // mean days, specialist assessment to treatment (HEALTH_PROC · DF_WAITING · CM131_138)
        public float WaitKnee = Absent;            // mean days (CM8154)
        public int WaitYear;
        /// <summary>Supporting readouts, five of six (France absent): asthma+COPD, diabetes, CHF admissions per 100 000 aged 15+;
        /// AMI and stroke 30-day mortality per 100 admissions aged 45+ (HCQO · DF_PC, DF_AC).</summary>
        public float[] Supporting = { Absent, Absent, Absent, Absent, Absent };
        public int SupportingYear;

        // ---- BASES captured at the seed (HealthFamily.Seed) ----
        public float SpendPerHeadSeed;         // real health spending per head at the seed (the ratio is what matters, not the unit)
        public float SpendPerAgeCostSeed;      // real health spending per age-cost unit at the seed
        public float EfficiencySeed = 1f;      // the health minister's efficiency at the seed (0..1)
        public float DeathRateSeed;            // the crude death rate at the seed (per 1 000), the feedback pass's anchor
        // ---- THE TREND FAMILY (2026-09-07, ruling 2 of the overnight review) ----
        public float TreatableMortalityTrendPerYear;   // SOURCED: the continuous yearly rate of the OECD's own series since 2000 (DF_AM, TRTM), one vintage per country - negative is improvement
        public int TrendFirstYear, TrendLastYear;      // the vintage's span
        public float TrendIndex = 1f;                  // exp(rate × years since the seed), compounding at the yearly step - the anchor the quality target multiplies; 1 at the seed
        public bool Seeded;

        public bool HasWaits => WaitCataract >= 0f && WaitKnee >= 0f;
        public bool HasSupporting => Supporting != null && Supporting.Length == 5 && Supporting[0] >= 0f;
    }

    public static class HealthFamily
    {
        public static readonly string[] SupportingNames = { "ASTHMA + COPD", "DIABETES", "HEART FAILURE", "AMI 30-DAY", "STROKE 30-DAY" };
        public static readonly string[] SupportingUnits = { "ADM / 100 000 · 15+", "ADM / 100 000", "ADM / 100 000", "PER 100 ADM · 45+", "PER 100 ADM" };

        // ---- THE COUPLING CONSTANTS - every one [AUTHORED-DRAFT], the spine's §6 lines, stated and measured on this pass ----
        /// <remarks>[AUTHORED-DRAFT] - a yearly reversion speed toward the target; 0.3 means a third of the gap closes each year.</remarks>
        public const float ReversionPerYear = 0.3f;
        /// <remarks>[AUTHORED-DRAFT] - coverage's elasticity to real spending per head against its seed (coverage_target = seed x ratio^c, ceilinged).</remarks>
        public const float CoverageElasticity = 0.1f;
        /// <remarks>[AUTHORED-DRAFT] - treatable mortality's elasticity to real spending per age-cost unit against its seed (target = seed x (seed/now)^q).
        /// HealthFamilyDiagnostic prints the elasticity Poland's 106 against Sweden's 45 IMPLIES on the game's own seeded spending per head beside this
        /// figure, so the two can be read together - never tuned to meet.</remarks>
        public const float QualitySpendingElasticity = 0.5f;
        /// <remarks>[AUTHORED-DRAFT] - treatable mortality's elasticity to the Health ministry's EFFECTIVENESS against its seed (P5-C7: allocated / requested x efficiency).</remarks>
        public const float QualityEfficiencyElasticity = 0.3f;
        /// <remarks>[AUTHORED-DRAFT] - waiting times' elasticity to the Health ministry's effectiveness (P5-C7).</remarks>
        public const float WaitEffectivenessElasticity = 0.5f;
        /// <remarks>CONVENTION - runaway guards on the state, set outside anything the couplings reach; the instruments' bands are the family's STATED ranges (9c), not these.</remarks>
        public const float MinTreatableMortality = 10f, MaxTreatableMortality = 400f, MinWaitDays = 1f, MaxWaitDays = 1000f;

        // ---- THE FEEDBACK PASS (2026-09-06, overnight; the spine's §4 "feedbacks to the model, proposed and NOT built" - built now, one family, its own BASELINE) ----
        // The family MOVES THE MODEL through three terms, each [AUTHORED-DRAFT] in magnitude and DIRECTIONAL in the literature, each reading the
        // quality key's RATIO to its seed so a country at its seed adds exactly nothing (the suite's pre-existing fields open only where the ratio moves):
        /// <remarks>[AUTHORED-DRAFT], DIRECTIONAL - years of life expectancy per unit of ln(seed / treatable mortality): treatable mortality is by construction the mortality timely care averts
        /// (OECD/Eurostat avoidable-mortality definition; OECD Health at a Glance 2023 reads the life-expectancy gap between countries largely through it). 1.0 means halving treatable
        /// mortality adds 0.69 years - the small drift the spine asked for.</remarks>
        public const float LifeExpectancyPerLogQuality = 1.0f;
        /// <remarks>[AUTHORED-DRAFT], DIRECTIONAL - percentage points of labour-force participation per unit of ln(seed / treatable mortality): ill health is the largest single reason for
        /// inactivity in the 50–64 band (OECD Sickness, Disability and Work 2010; Eurostat lfsa_igar "own illness or disability" among the inactive). 0.5 means halving treatable mortality
        /// lifts participation a third of a point.</remarks>
        public const float ParticipationPerLogQuality = 0.5f;
        /// <summary>DERIVED, not authored: the crude death rate moves ONE FOR ONE with treatable mortality against its seed - a treatable death is a death, so
        /// (per 1 000) = seed + (treatable mortality − its seed) ÷ 100. The stated approximation: treatable mortality is age-standardised (the OECD population)
        /// and the crude rate is not, so the identity is exact only at the seed's age structure. At Sweden's seed that is an elasticity of 45 ÷ 950 = 0.047,
        /// Poland's 106 ÷ ~1 100 = 0.10 - each country's own, never one figure for six. (A first draft authored 0.15 and called treatable deaths "a sixth" of all
        /// deaths; the diagnostic's own printout - 45 against 950 - showed that wrong, and the identity replaced it before anything was dumped on it.) Retires
        /// the "HELD at its sourced seed" of F2's CohortDemographics for THIS one channel - the lever map's last unreached quantity closes here.</summary>
        public static float DeathRateFor(HealthSeeds s, float treatableMortality)
            => Mathf.Max(0.5f, s.DeathRateSeed + (treatableMortality - TrendedAnchor(s)) / 100f);   // §364: the deviation from the trended anchor - the trend's deaths are the projection's already

        /// <summary>The deaths per 1 000 above (or below) the seed's crude rate that the family has written - what the cohort substrate removes from the
        /// pyramid each year (CohortDemographics.Step), so that the identity Δpopulation = births − deaths + migration closes on DEATHS and the migration
        /// readings stay the publisher's. Zero at the seed and for a country without the family.</summary>
        public static float ExcessDeathsPerThousand(Country country)
        {
            HealthSeeds s = country.Health;
            if (s == null || !s.Seeded || s.DeathRateSeed <= 0f) { return 0f; }
            return country.State.DeathRate - s.DeathRateSeed;
        }

        /// <summary>ln(the TRENDED anchor ÷ treatable mortality now) - positive when care is better than the world's own trend would have it (the deviation form,
        /// §364: the trend is what the publisher's projection already carries, so only the deviation from it is a term); 0 at the seed and 0 on the trend; clamped to
        /// ±ln 4 as a runaway guard.</summary>
        public static float QualityLog(Country country)
        {
            HealthSeeds s = country.Health;
            if (s == null || !s.Seeded || s.TreatableMortality <= 0f || country.State.TreatableMortality <= 0f) { return 0f; }
            return Mathf.Clamp(Mathf.Log(TrendedAnchor(s) / country.State.TreatableMortality), -1.386f, 1.386f);
        }

        /// <summary>The life-expectancy term, years, added to MacroSystem.ApplyLifeExpectancy's target.</summary>
        public static float LifeExpectancyTerm(Country country) => LifeExpectancyPerLogQuality * QualityLog(country);

        /// <summary>The participation term, percentage points, added to MacroSystem.ApplyLaborForceParticipationRate's target.</summary>
        public static float ParticipationTerm(Country country) => ParticipationPerLogQuality * QualityLog(country);

        /// <summary>Seeds every country from the spine's tables and captures its bases. Called once, at the end of WorldFactory.CreateDefault,
        /// after the spending lines and the cabinet exist.</summary>
        public static void SeedAll(World world)
        {
            foreach (Country c in world.Countries) { Seed(c); }
        }

        public static void Seed(Country country)
        {
            HealthSeeds s = country.Health;
            switch (country.Id)
            {
                // OECD DF_HEALTH_PROT (TPRIBASI, COVGCMED), DF_AM (TRTM), DF_WAITING (WAIT_MEAN WTSP), DF_PC / DF_AC - HEALTH_FAMILY_SPINE.md §1-§3.
                case CountryId.Sweden:  Set(s, 100f, 100f, 100f, 2024, 45f, 2024, 60.2f, 141.3f, 2025, new[] { 123.1f, 62.0f, 206.0f, 3.4f, 4.9f }, 2023); break;
                case CountryId.Germany: Set(s, 99.9f, 99.9f, 100f, 2024, 63f, 2022, HealthSeeds.Absent, HealthSeeds.Absent, 0, new[] { 251.5f, 180.5f, 381.5f, 7.9f, 7.0f }, 2023); break;
                case CountryId.France:  Set(s, 99.9f, 99.9f, 100f, 2025, 46f, 2023, HealthSeeds.Absent, HealthSeeds.Absent, 0, null, 0); break;
                case CountryId.Italy:   Set(s, 100f, 100f, 100f, 2025, 51f, 2023, 69f, 90f, 2025, new[] { 31.9f, 31.2f, 163.2f, 4.7f, 6.9f }, 2023); break;
                case CountryId.Poland:  Set(s, 92.1f, 92.1f, 100f, 2025, 106f, 2024, 47f, 281.5f, 2025, new[] { 129.3f, 161.4f, 523.4f, 6.7f, 10.5f }, 2023); break;
                case CountryId.USA:     Set(s, 91.8f, 39.2f, 91.8f, 2024, 92f, 2023, HealthSeeds.Absent, HealthSeeds.Absent, 0, new[] { 123.3f, 224.0f, 387.2f, 5.2f, 4.5f }, 2022); break;
                default: return;   // a country the spine does not cover carries no family
            }
            EconomyState st = country.State;
            st.HealthCoverage = s.Coverage;
            st.TreatableMortality = s.TreatableMortality;
            st.WaitCataractDays = s.WaitCataract;
            st.WaitKneeDays = s.WaitKnee;
            s.SpendPerHeadSeed = SpendPerHead(country);
            s.SpendPerAgeCostSeed = SpendPerAgeCost(country);
            s.DeathRateSeed = st.DeathRate;
            s.EfficiencySeed = Efficiency(country);
            // HEALTH_FAMILY_SPINE.md §9: ln(last ÷ first) ÷ years of the OECD series (deaths per 100 000, age-standardised, total), 2000 (Italy 2003) to the latest published year.
            switch (country.Id)
            {
                case CountryId.Sweden:  Trend(s, -0.02980f, 2000, 2024); break;   // 92 → 45
                case CountryId.Germany: Trend(s, -0.02492f, 2000, 2022); break;   // 109 → 63
                case CountryId.France:  Trend(s, -0.02296f, 2000, 2023); break;   // 78 → 46
                case CountryId.Italy:   Trend(s, -0.02188f, 2003, 2023); break;   // 79 → 51
                case CountryId.Poland:  Trend(s, -0.01944f, 2000, 2024); break;   // 169 → 106
                case CountryId.USA:     Trend(s, -0.01298f, 2000, 2023); break;   // 124 → 92
            }
            s.TrendIndex = 1f;
            s.Seeded = true;
        }

        private static void Trend(HealthSeeds s, float ratePerYear, int firstYear, int lastYear) { s.TreatableMortalityTrendPerYear = ratePerYear; s.TrendFirstYear = firstYear; s.TrendLastYear = lastYear; }

        /// <summary>The quality anchor the target multiplies: the seed's treatable mortality carried along the sourced trend (seed × TrendIndex). The elasticities
        /// then read spending and effectiveness against THIS anchor, so the world's improvement and the player's are separate terms, as B7 separates the
        /// productivity trend from the cycle.</summary>
        public static float TrendedAnchor(HealthSeeds s) => Mathf.Max(MinTreatableMortality, s.TreatableMortality * Mathf.Max(0.0001f, s.TrendIndex));   // §364: the anchor is bounded by the same floor as the metric, so once both sit on the guard the deviation is zero rather than the anchor's distance below it

        private static void Set(HealthSeeds s, float coverage, float coveragePublic, float ceiling, int coverageYear, float tm, int tmYear,
            float waitCataract, float waitKnee, int waitYear, float[] supporting, int supportingYear)
        {
            s.Coverage = coverage; s.CoveragePublic = coveragePublic; s.CoverageCeiling = ceiling; s.CoverageYear = coverageYear;
            s.TreatableMortality = tm; s.TreatableMortalityYear = tmYear;
            s.WaitCataract = waitCataract; s.WaitKnee = waitKnee; s.WaitYear = waitYear;
            s.Supporting = supporting ?? new[] { HealthSeeds.Absent, HealthSeeds.Absent, HealthSeeds.Absent, HealthSeeds.Absent, HealthSeeds.Absent };
            s.SupportingYear = supportingYear;
        }

        /// <summary>True for a health line: the five's HealthcareAndSocialCare and the USA's Medicare and Medicaid.</summary>
        public static bool IsHealthLine(SpendingCategory category)
            => category == SpendingCategory.HealthcareAndSocialCare || category == SpendingCategory.Medicare || category == SpendingCategory.Medicaid;

        /// <summary>The country's health spending this year, NOMINAL (the book's figures, P5-B6).</summary>
        public static float HealthSpendingNominal(Country country)
        {
            float sum = 0f;
            foreach (SpendingLine line in country.SpendingLines) { if (IsHealthLine(line.Category)) { sum += line.Amount; } }
            return sum;
        }

        /// <summary>Real health spending: the nominal figure over the price level (the couplings read quantities, not prices).</summary>
        public static float HealthSpendingReal(Country country) => HealthSpendingNominal(country) / Mathf.Max(0.0001f, country.State.PriceLevel);

        public static float SpendPerHead(Country country)
            => HealthSpendingReal(country) / Mathf.Max(0.0001f, SpendingDrivers.Level(SpendingDriver.Population, country));

        /// <summary>Real health spending over the age-cost index (P5-B2's driver): the money per unit of the demand an ageing cohort makes.</summary>
        public static float SpendPerAgeCost(Country country)
            => HealthSpendingReal(country) / Mathf.Max(0.0001f, SpendingDrivers.Level(SpendingDriver.AgeCostIndex, country));

        /// <summary>The health minister's efficiency as a 0..1 factor (CabinetMinister.Efficiency is 0..100); 1 when no minister sits.</summary>
        public static float Efficiency(Country country)
        {
            if (country.CabinetMinisters != null && country.CabinetMinisters.TryGetValue(CabinetPortfolio.HealthSocialAffairs, out CabinetMinister m) && m != null)
            {
                return Mathf.Clamp(m.Efficiency, 1f, 100f) / 100f;
            }
            return 1f;
        }

        /// <summary>P5-C7 (board 9d): the Health ministry's effectiveness - allocated / requested x the minister's efficiency (Effectiveness.RatioOf);
        /// before the first turn records one it is the efficiency alone (the seed: allocated = requested). The spending-per-head stand-in is retired.</summary>
        public static float HealthEffectiveness(Country country) => Effectiveness.RatioOf(country, CabinetPortfolio.HealthSocialAffairs);

        public readonly struct Targets
        {
            public readonly float Coverage, TreatableMortality, WaitFactor;
            public Targets(float coverage, float treatableMortality, float waitFactor) { Coverage = coverage; TreatableMortality = treatableMortality; WaitFactor = waitFactor; }
        }

        /// <summary>The targets the state drifts toward, from the spine's §6 lines, for a given real health spending (so a draft can be projected).</summary>
        public static Targets TargetsFor(Country country, float healthSpendingReal)
        {
            HealthSeeds s = country.Health;
            float heads = Mathf.Max(0.0001f, SpendingDrivers.Level(SpendingDriver.Population, country));
            float ageCost = Mathf.Max(0.0001f, SpendingDrivers.Level(SpendingDriver.AgeCostIndex, country));
            float perHeadRatio = s.SpendPerHeadSeed > 0f ? (healthSpendingReal / heads) / s.SpendPerHeadSeed : 1f;
            float perAgeCostRatio = s.SpendPerAgeCostSeed > 0f ? (healthSpendingReal / ageCost) / s.SpendPerAgeCostSeed : 1f;
            // P5-C7: effectiveness against its seed (at the seed allocated = requested, so the seed's effectiveness is the seed's efficiency).
            float effectivenessRatio = s.EfficiencySeed > 0f ? HealthEffectiveness(country) / s.EfficiencySeed : 1f;
            perHeadRatio = Mathf.Max(0.01f, perHeadRatio); perAgeCostRatio = Mathf.Max(0.01f, perAgeCostRatio); effectivenessRatio = Mathf.Max(0.01f, effectivenessRatio);

            float coverage = Mathf.Min(s.CoverageCeiling, s.Coverage * Mathf.Pow(perHeadRatio, CoverageElasticity));
            float tm = TrendedAnchor(s) * Mathf.Pow(1f / perAgeCostRatio, QualitySpendingElasticity) * Mathf.Pow(1f / effectivenessRatio, QualityEfficiencyElasticity);   // the trend family: the anchor carries the sourced trend
            float waitFactor = Mathf.Pow(1f / effectivenessRatio, WaitEffectivenessElasticity);   // P5-C7: the waits read the ministry's effectiveness
            return new Targets(coverage, Mathf.Clamp(tm, MinTreatableMortality, MaxTreatableMortality), waitFactor);
        }

        /// <summary>The yearly step, at the turn boundary after the lines are indexed and resolved: each figure closes ReversionPerYear of its gap
        /// to its target. An absent wait stays absent. A country the spine does not cover is untouched.</summary>
        public static void AdvanceYear(Country country)
        {
            HealthSeeds s = country.Health;
            if (s == null || !s.Seeded) { return; }
            EconomyState st = country.State;
            // The trend family: the anchor compounds one year of the sourced rate before the year's target is read (the seed's year is index 1).
            s.TrendIndex *= Mathf.Exp(s.TreatableMortalityTrendPerYear);
            Targets t = TargetsFor(country, HealthSpendingReal(country));
            st.HealthCoverage = Mathf.Clamp(st.HealthCoverage + (t.Coverage - st.HealthCoverage) * ReversionPerYear, 0f, s.CoverageCeiling);
            st.TreatableMortality = Mathf.Clamp(st.TreatableMortality + (t.TreatableMortality - st.TreatableMortality) * ReversionPerYear, MinTreatableMortality, MaxTreatableMortality);
            // The feedback pass: the crude death rate follows treatable mortality against its seed one for one (the one channel that moves F2's held figure).
            if (s.DeathRateSeed > 0f && s.TreatableMortality > 0f) { st.DeathRate = DeathRateFor(s, st.TreatableMortality); }
            if (s.HasWaits)
            {
                float cataractTarget = Mathf.Clamp(s.WaitCataract * t.WaitFactor, MinWaitDays, MaxWaitDays);
                float kneeTarget = Mathf.Clamp(s.WaitKnee * t.WaitFactor, MinWaitDays, MaxWaitDays);
                st.WaitCataractDays = Mathf.Clamp(st.WaitCataractDays + (cataractTarget - st.WaitCataractDays) * ReversionPerYear, MinWaitDays, MaxWaitDays);
                st.WaitKneeDays = Mathf.Clamp(st.WaitKneeDays + (kneeTarget - st.WaitKneeDays) * ReversionPerYear, MinWaitDays, MaxWaitDays);
            }
        }

        /// <summary>Next year's treatable mortality if this year's health spending were <paramref name="healthSpendingNominal"/> - the figure the
        /// 5c arrow prints on the plate while a draft is live (WITH vs WITHOUT: the caller subtracts the standing projection).</summary>
        public static float ProjectTreatableMortality(Country country, float healthSpendingNominal)
        {
            Targets t = TargetsFor(country, healthSpendingNominal / Mathf.Max(0.0001f, country.State.PriceLevel));
            float now = country.State.TreatableMortality;
            return Mathf.Clamp(now + (t.TreatableMortality - now) * ReversionPerYear, MinTreatableMortality, MaxTreatableMortality);
        }

        /// <summary>A supporting readout today: its seed moved by the quality key's own ratio (the spine: "moved by the quality key, never coupled").</summary>
        public static float SupportingNow(Country country, int index)
        {
            HealthSeeds s = country.Health;
            if (!s.HasSupporting || index < 0 || index >= s.Supporting.Length || s.Supporting[index] < 0f || s.TreatableMortality <= 0f) { return HealthSeeds.Absent; }
            return s.Supporting[index] * country.State.TreatableMortality / s.TreatableMortality;
        }

        /// <summary>Retiree coverage, derived: coverage on the 65+ cohort - the USA's Medicare rule (the 65+ are publicly covered) is the one
        /// authored exception, [AUTHORED-DRAFT] as the spine marks it.</summary>
        public static float RetireeCoverage(Country country) => country.Id == CountryId.USA ? 100f : country.State.HealthCoverage;

        /// <summary>The public share of coverage, derived (COVGCMED ÷ TPRIBASI at the seed, applied to today's coverage).</summary>
        public static float PublicCoverageNow(Country country)
        {
            HealthSeeds s = country.Health;
            return s.Coverage > 0f ? country.State.HealthCoverage * (s.CoveragePublic / s.Coverage) : HealthSeeds.Absent;
        }

        /// <summary>The elasticity two countries' seeds IMPLY between them: ln(tm_a / tm_b) / ln(spendPerHead_b / spendPerHead_a) - the diagnostic's
        /// yardstick beside <see cref="QualitySpendingElasticity"/>. NaN when a ratio is not positive.</summary>
        public static float ImpliedQualityElasticity(Country a, Country b)
        {
            float tmA = a.Health.TreatableMortality, tmB = b.Health.TreatableMortality;
            float sA = a.Health.SpendPerHeadSeed, sB = b.Health.SpendPerHeadSeed;
            if (tmA <= 0f || tmB <= 0f || sA <= 0f || sB <= 0f || Mathf.Approximately(sA, sB)) { return float.NaN; }
            return (float)(Math.Log(tmA / tmB) / Math.Log(sB / sA));
        }
    }
}
