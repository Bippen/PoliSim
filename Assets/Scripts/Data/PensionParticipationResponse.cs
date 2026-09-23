using System.Collections.Generic;
using UnityEngine;

namespace PoliSim.Data
{
    /// <summary>
    /// PN-1's other half (2026-09-23, §596): THE PARTICIPATION RESPONSE OF THE BANDS A PENSION AGE CROSSES. The pension line's saving (§520's driver,
    /// §590's dial) counted a moved age as fewer pensioners and nothing else; the people it kept from retiring did not appear in the labour force.
    ///
    /// <para><b>The source.</b> Atav, Jongen &amp; Rabaté, <i>Increasing the Effective Retirement Age: Key Factors and Interaction Effects</i>, IZA DP
    /// 14150 (February 2021; the working paper of Rabaté, Jongen &amp; Atav, AEJ: Economic Policy 2024), read 2026-09-23. Their finding is a MECHANISM, not a
    /// single number: people who reach the old age stay in their pre-age labour-market state, so the employment effect of an increase is "the product
    /// of the two numbers - the pre-SRA employment rate and hazard rate into retirement", the bunching at the age, "in the absence of active substitution
    /// and upstream effects". Their Table B.1 sets the studies side by side (employment effect; employment rate just before the age; hazard at it):
    /// Staubli &amp; Zweimüller 2013 (AUT, ERA 60→62) +9.8 pp, 28 %, 0.50 and (ERA 55→58¼) +11.0, 57 %, 0.25; Cribb et al. 2016 (UK, ERA 60→62) +6.3,
    /// 55 %, 0.25; De Vos et al. 2018 (NLD, SRA 65→65½) +10, 43 %, 0.35; Rabaté &amp; Rochut 2019 (FRA, NRA 60→61) +20.9, 45 %, 0.50; Geyer &amp; Welteke
    /// 2019 (GER, ERA 60→63) +13.5, 62 %, 0.19; the paper's own (NLD) +4.5, 15 %, 0.50 and +20.3, 29 %, 0.70.</para>
    ///
    /// <para><b>The model.</b> The participation the pyramid implies (`ParticipationRateTable.StructuralRate`, the anchor the state's rate reverts to and
    /// so, since PN-3, the labour force potential reads) gains, in the ages between the age in force at the seed and the age in force now, the
    /// country's OWN sourced rate for the year before the lower of the two ages × the hazard at the age. Raised, those ages keep that much more of
    /// their pre-age participation; lowered, they lose it (the paper's Table B.1 carries one lowering, Vestad 2013, NOR 64→62, −33.2 pp, the same
    /// mechanism read backwards). A band's rate never leaves 0-100 %. The response is ZERO at the seed year's age - the tables are the seed's structure,
    /// as every seed table is - so the no-policy run moves only where a statute steps after the seed, and a bill's age moves it from there.</para>
    ///
    /// <para><b>What it is not.</b> The paper's figure is EMPLOYMENT; it enters here as participation, and the participation the model adds meets the
    /// country's unemployment like any other - the substitution into unemployment the studies also find (Staubli &amp; Zweimüller's +12.5 pp on men) is
    /// not added on top. No upstream effect before the old age (the paper: mixed evidence), no response of the pension line itself - its saving
    /// (the headcount at or above the age) is unchanged.</para>
    /// </summary>
    public static class PensionParticipationResponse
    {
        /// <summary>SOURCED (Atav, Jongen &amp; Rabaté 2021, Table B.1): the hazard into retirement at the age, for the countries a study in the table
        /// measured - France, Rabaté &amp; Rochut 2019 (the NRA 60→61, 0.50); Germany, Geyer &amp; Welteke 2019 (women's ERA 60→63, 0.19 - an early
        /// retirement age, not the statutory one, and the country's only entry).</summary>
        private static readonly Dictionary<CountryId, float> HazardByCountry = new Dictionary<CountryId, float>
        {
            { CountryId.France, 0.50f },
            { CountryId.Germany, 0.19f },
        };

        /// <summary>DERIVED from the same table: the median hazard of its eight INCREASES that report one (0.19, 0.25, 0.25, 0.35, 0.50, 0.50, 0.50,
        /// 0.70 → 0.425), for Sweden, Italy, Poland and the USA, which no study in the table measured. Vestad's lowering is left out of the median.</summary>
        public const float MedianHazard = 0.425f;

        /// <summary>⚠ PROBE ONLY: set by `PensionParticipationProbe` to run a world with the response off beside one with it on (the attribution), and
        /// restored in its `finally`. The game never sets it.</summary>
        public static bool ProbeSuspended;

        public static float Hazard(CountryId id) => HazardByCountry.TryGetValue(id, out float h) ? h : MedianHazard;

        public static bool HazardIsCountrys(CountryId id) => HazardByCountry.ContainsKey(id);

        /// <summary>The age the tables were read under: the statute's age at the seed year.</summary>
        public static float ReferenceAge(CountryId id) => PensionAgeStatute.AgeInForce(id, PensionAgeStatute.SeedYear);

        /// <summary>The extra active people - in the pyramid's units, negative for a lowered age - the moved age keeps in (or puts out of) the labour
        /// force: Σ over the bands between the two ages of the band's count × the share of the band in that span × the step, where the step is the
        /// sourced rate for the year before the lower age × the hazard, capped so no band leaves 0-100 %. Zero without a statute, a table or a pyramid.</summary>
        public static float ExtraActive(Country country, float[] counts)
        {
            if (ProbeSuspended || country == null || counts == null || !PensionAgeStatute.Has(country.Id)) { return 0f; }
            float[] rates = ParticipationRateTable.For(country.Id);
            if (rates == null) { return 0f; }
            float reference = ReferenceAge(country.Id);
            float now = PensionAgeStatute.AgeInForce(country, country.CalendarYear);
            if (Mathf.Abs(now - reference) < 1e-4f) { return 0f; }
            float lower = Mathf.Min(reference, now), upper = Mathf.Max(reference, now);
            float step = RateAtAge(rates, lower - 1f) * Hazard(country.Id);
            float sign = now > reference ? 1f : -1f;
            float extra = 0f;
            for (int b = 3; b < PopulationCohorts.CohortCount && b < counts.Length; b++)   // band 3 = 15-19, the table's first
            {
                float start = b * PopulationCohorts.CohortWidth;
                float end = b == PopulationCohorts.OpenBandIndex ? float.MaxValue : start + PopulationCohorts.CohortWidth;
                float overlap = Mathf.Min(end, upper) - Mathf.Max(start, lower);
                if (overlap <= 0f) { continue; }
                float share = b == PopulationCohorts.OpenBandIndex ? 0f : overlap / PopulationCohorts.CohortWidth;
                float room = sign > 0f ? 1f - rates[b] : rates[b];
                extra += sign * counts[b] * share * Mathf.Min(step, Mathf.Max(0f, room));
            }
            return extra;
        }

        /// <summary>The sourced rate AT a single year of age: linear between the five-year bands' midpoints (band b's rate at b × 5 + 2.5), held flat
        /// beyond the first and last midpoints. §596's review (D2): the band holding the age was read whole, so a LOWERED age - whose "year before" is
        /// the dial's own value - jumped as it crossed a band edge (France 61 → 60 y 11 m nearly doubled the step, some 290 000 people for one month on
        /// the dial); between midpoints the step is continuous in the age, and the rate just before an age is read nearer that age than a band average.</summary>
        public static float RateAtAge(float[] rates, float age)
        {
            int n = Mathf.Min(rates.Length, PopulationCohorts.CohortCount);
            float half = PopulationCohorts.CohortWidth * 0.5f;
            float position = (age - half) / PopulationCohorts.CohortWidth;   // band index at whose midpoint the age sits
            if (position <= 0f) { return rates[0]; }
            if (position >= n - 1) { return rates[n - 1]; }
            int b = Mathf.FloorToInt(position);
            float t = position - b;
            return rates[b] + (rates[b + 1] - rates[b]) * t;
        }
    }
}
