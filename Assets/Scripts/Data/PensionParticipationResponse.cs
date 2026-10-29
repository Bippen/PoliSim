using System.Collections.Generic;
using PoliSim.Data.Generated;
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
    /// so, since PN-3, the labour force potential reads) gains, in the ages between the statute's age for the year (K276; the seed's before it) and the age in force now, a STEP:
    /// since §599 the effect a study MEASURED in the country where one exists (France, Germany), else the country's own sourced rate for the year before
    /// the lower of the two ages × the table's median hazard (Sweden, Italy, Poland, the USA) - `Step`. Raised, those ages keep that much more of their
    /// pre-age participation; lowered, they lose it (the paper's Table B.1 carries one lowering, Vestad 2013, NOR 64→62, −33.2 pp, the same mechanism read
    /// backwards; a measured country's lowering reuses its raise's measurement - §599's review, stated). A band's rate never leaves 0-100 %.</para>
    ///
    /// <para><b>K276 (2026-10-29): the reference is the STATUTE'S age for the year, no longer its age at the seed.</b> Since K276 the rates by age follow
    /// the Ageing Report's (the EU five) and CBO's (the USA) projections (`ParticipationCohortPath`), and both project under the pension law as legislated -
    /// the statute's own steps are inside the projected path. The response is therefore ZERO wherever the country's age in force is its statute's for the
    /// year and the statute's path is the one the projection holds, and moves participation only where a passed bill's age departs from it: the player's
    /// statute is the input, and the statute's own path is not counted twice. Review 1 (M1): past an indexed statute's horizon (Sweden after 2032, Italy
    /// after 2028) the Ageing Report keeps indexing while the game carries the last published figure, so the reference there is the age the Ageing Report
    /// assumed (<see cref="ReferenceAge"/>) and the no-policy run carries a NEGATIVE response - participation under the carried law the pension line reads.
    /// Before K276 the reference was the seed year's age and the statute's later steps (Germany to 67, France
    /// to 64, Italy's 2027-2028 adjustments, the USA's 67) added the response in the no-policy run as well. The step reads the year's projected rates.</para>
    ///
    /// <para><b>What it is not.</b> The paper's figure is EMPLOYMENT; it enters here as participation, and the participation the model adds meets the
    /// country's unemployment like any other - the substitution into unemployment the studies also find (Staubli &amp; Zweimüller's +12.5 pp on men) is
    /// not added on top. No upstream effect before the old age (the paper: mixed evidence), no response of the pension line itself - its saving
    /// (the headcount at or above the age) is unchanged.</para>
    /// </summary>
    public static class PensionParticipationResponse
    {
        /// <summary>SOURCED - §599, ruled 2026-09-23: WHERE A COUNTRY'S EFFECT WAS MEASURED DIRECTLY, THE MEASUREMENT IS THE SOURCE. The employment effect
        /// the study measured in that country, as a fraction, from Atav, Jongen &amp; Rabaté 2021, Table B.1: France, Rabaté &amp; Rochut 2019 (the NRA 60→61,
        /// +20.9 pp); Germany, Geyer &amp; Welteke 2019 (+13.5 pp - women only, at an early retirement age, 60→63, on a pre-age employment rate of 62 %; carried
        /// here as the same absolute points for both sexes at the statutory age, where the sourced rate before 66 y 4 m is 41.9 % - an implied hazard of 0.32
        /// against the study's 0.19; the country's only measurement, stated). Each is carried as absolute points at the country's CURRENT ages, not the ages
        /// it was measured at (France's at 60, applied from 62 y 9 m). The model's formula would have given France 25.3 pp (its own sourced rate at 61 y 9 m,
        /// 50.7 %, × the study's hazard 0.50, +21 % over the measurement); the paper's own product for that study - its 45 % × 0.50 = 22.5 pp - overshoots by 8 %.
        /// Nothing caps a measured step at the pre-age rate; neither entry comes near it (§599's review).</summary>
        private static readonly Dictionary<CountryId, float> MeasuredEffect = new Dictionary<CountryId, float>
        {
            { CountryId.France, 0.209f },
            { CountryId.Germany, 0.135f },
        };

        /// <summary>DERIVED from the same table: the median hazard of its eight INCREASES that report one (0.19, 0.25, 0.25, 0.35, 0.50, 0.50, 0.50,
        /// 0.70 → 0.425), for the four countries no study measured - Sweden, Italy, Poland and the USA - where the step is the country's own sourced rate
        /// before the age × this. The paper's mechanism, with its known overshoot where it can be checked: on France the model's formula gives 25.3 pp
        /// against the 20.9 measured (the paper's own product for that study, 22.5 pp), and across Table B.1 the measured effect is a median 0.736 of the
        /// product (§599's review). Vestad's lowering is left out of the median.</summary>
        public const float MedianHazard = 0.425f;

        /// <summary>The effect a country MEASURED, as a fraction, when one exists.</summary>
        public static bool TryMeasured(CountryId id, out float effect) => MeasuredEffect.TryGetValue(id, out effect);

        /// <summary>The step - the share of the people in the ages crossed who stay in (or leave) the labour force: the country's measured effect where
        /// one exists, else its sourced rate at the year before the lower age × the median hazard.</summary>
        public static float Step(CountryId id, float[] rates, float lowerAge)
            => MeasuredEffect.TryGetValue(id, out float measured) ? measured : RateAtAge(rates, lowerAge - 1f) * MedianHazard;

        /// <summary>⚠ PROBE ONLY: set by `PensionParticipationProbe` to run a world with the response off beside one with it on (the attribution), and
        /// restored in its `finally`. The game never sets it.</summary>
        public static bool ProbeSuspended;

        /// <summary>K276: the age the projected rates were made under in a year. Review 1 (M1): the Ageing Report projects Sweden's and Italy's
        /// participation under the pension age linked to life expectancy for its whole horizon, while the game's indexed statute publishes only to its
        /// horizon (Sweden 2032, Italy 2028) and then carries its last figure (board 15c-r2). So: inside the horizon, and for a scheduled or fixed statute
        /// (Germany, France, Poland; the USA, whose CBO projection is made under the legislated 67), the statute's own age for the year; past an indexed
        /// statute's horizon, the age the Ageing Report assumed (<see cref="ProjectedAge"/>), never below the statute's carried figure. In the no-policy run
        /// the response is then NEGATIVE there - the labour force is the projection's less the participation of the indexation the carried statute does
        /// not enact, the law the pension line's headcount also reads - and a bill is measured from the age the projection already holds, never twice.</summary>
        public static float ReferenceAge(CountryId id, int year)
        {
            float statute = PensionAgeStatute.AgeInForce(id, year);
            if (PensionAgeStatute.IsDated(id, year)) { return statute; }
            float projected = ProjectedAge(id, year);
            return float.IsNaN(projected) ? statute : Mathf.Max(statute, projected);
        }

        /// <summary>Review 1 (M1): the pension age the Ageing Report 2024 projects an indexed statute's country under in a year - the 2024 country fiche's
        /// Table 1 (`ParticipationPath.ProjectedPensionAge`, generated by Tools/k276_participation_prep.pl; Sweden the earliest age for a guaranteed pension,
        /// the age the riktålder sets; Italy the statutory retirement age), linear between the fiche's years [AUTHORED - the fiche prints a figure a
        /// decade], held before its first and after its last; NaN for a country the fiches are not read for.</summary>
        public static float ProjectedAge(CountryId id, int year)
        {
            if (!ParticipationPath.ProjectedPensionAgeYears.TryGetValue(id, out int[] years)) { return float.NaN; }
            float[] ages = ParticipationPath.ProjectedPensionAge[id];
            if (year <= years[0]) { return ages[0]; }
            for (int k = 1; k < years.Length; k++)
            {
                if (year <= years[k]) { return ages[k - 1] + (ages[k] - ages[k - 1]) * (year - years[k - 1]) / (float)(years[k] - years[k - 1]); }
            }
            return ages[ages.Length - 1];
        }

        /// <summary>The extra active people - in the pyramid's units, negative for a lowered age - the moved age keeps in (or puts out of) the labour
        /// force: Σ over the bands between the two ages of the band's count × the share of the band in that span × the step (`Step`: measured, or the
        /// sourced rate for the year before the lower age × the median hazard), capped so no band leaves 0-100 %. Zero without a statute, a table or a pyramid.
        /// K276: between the reference for <paramref name="year"/> (<see cref="ReferenceAge"/> - the statute's age, or past an indexed statute's horizon the
        /// age the Ageing Report assumed) and the country's age in force that year, at that year's projected rates.</summary>
        public static float ExtraActive(Country country, float[] counts, int year)
        {
            if (ProbeSuspended || country == null || counts == null || !PensionAgeStatute.Has(country.Id)) { return 0f; }
            float[] rates = ParticipationRateTable.RatesAt(country.Id, year);
            if (rates == null) { return 0f; }
            float reference = ReferenceAge(country.Id, year);
            float now = PensionAgeStatute.AgeInForce(country, year);
            if (Mathf.Abs(now - reference) < 1e-4f) { return 0f; }
            float lower = Mathf.Min(reference, now), upper = Mathf.Max(reference, now);
            float step = Step(country.Id, rates, lower);
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
