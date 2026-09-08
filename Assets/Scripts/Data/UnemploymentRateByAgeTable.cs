using System.Collections.Generic;

namespace PoliSim.Data
{
    /// <summary>
    /// FT-8 (2026-09-08, §398): the unemployment rate by age band, SOURCED per country and laid onto the 21 cohort bands, so the natural rate can read the
    /// labour force's composition the way the CBO and Shimer (1998) read it - every band at its own sourced rate, the shares moving with the pyramid.
    ///
    /// <para><b>The five, Eurostat</b> `une_rt_a` (Unemployment by sex and age - annual data; unit PC_ACT, percentage of the population in the labour force;
    /// sex T), reference year 2024, dataset updated 2026-06-11, read through the dissemination API on 2026-09-08. The dataset carries three working-age groups and
    /// nothing finer: Y15-24, Y25-54, Y55-74. Bands 15–19 and 20–24 take Y15-24; 25–29 … 50–54 take Y25-54; 55–59 … 70–74 take Y55-74, and so do 75+ where the
    /// LFS carries nothing (weightless for the five: their sourced participation above 74 is zero). France's 2024 figures are flagged `bd` by Eurostat (a
    /// break in the series and a definition that differs) - stated, not smoothed.</para>
    ///
    /// <para><b>The USA, BLS</b> Current Population Survey, 2024 annual averages, not seasonally adjusted, read from the public API (series LNU04000012 16–19,
    /// LNU04000036 20–24, LNU04000089 25–34, LNU04000091 35–44, LNU04000093 45–54, LNU04000095 55–64, LNU04000097 65 years and over) on 2026-09-08. The
    /// youngest band is 16–19 where the five carry 15–19, the same asymmetry ParticipationRateTable states.</para>
    ///
    /// <para><b>What is derived from it.</b> <see cref="CompositionRate"/>: Σ(band × participation rate × unemployment rate) ÷ Σ(band × participation rate)
    /// over the population aged 15 and over - the unemployment rate the pyramid's labour force would show with every age at its sourced rate. Only its CHANGE
    /// against the seed is used (Country.CompositionNaturalRateAtSeed → EconomyState.NaturalRateDemographicShift): the level is the 2024 structure at the 2024
    /// cycle, not a natural rate, and cancels in the difference. The group rates are held at their source year while the shares move - the CBO's construction
    /// as Aaronson, Hu, Seifoddini and Sullivan (Chicago Fed Letter 338, 2015) describe it, and the FRBSF (Bok and Petrosky-Nadeau, Economic Letter 2022-14)
    /// restate it.</para>
    /// </summary>
    public static class UnemploymentRateByAgeTable
    {
        // Eurostat une_rt_a, 2024, % of the labour force: Y15-24 / Y25-54 / Y55-74
        private static readonly float[] Sweden = Groups(24.3f, 6.4f, 5.7f);
        private static readonly float[] Germany = Groups(6.6f, 3.4f, 2.3f);
        private static readonly float[] France = Groups(18.8f, 6.4f, 5.1f);   // flagged bd by Eurostat (break; definition differs)
        private static readonly float[] Italy = Groups(20.3f, 6.4f, 3.6f);
        private static readonly float[] Poland = Groups(10.8f, 2.4f, 1.9f);
        // BLS CPS 2024 annual averages, unadjusted: 16–19 / 20–24 / 25–34 / 35–44 / 45–54 / 55–64 / 65+
        private static readonly float[] Usa = UsBands(12.7f, 7.3f, 4.3f, 3.2f, 2.7f, 2.8f, 3.1f);

        /// <summary>Unemployment rate per band in percent, index-aligned with <see cref="PopulationCohorts.Counts"/>; null for a country without a sourced table.</summary>
        public static readonly Dictionary<CountryId, float[]> Rates = new Dictionary<CountryId, float[]>
        {
            { CountryId.Sweden, Sweden }, { CountryId.Germany, Germany }, { CountryId.France, France },
            { CountryId.Italy, Italy }, { CountryId.Poland, Poland }, { CountryId.USA, Usa },
        };

        public static float[] For(CountryId id) => Rates.TryGetValue(id, out float[] r) ? r : null;

        /// <summary>
        /// The unemployment rate (percent) the pyramid's labour force would show with every band at its sourced rate: Σ(band × participation × rate) ÷
        /// Σ(band × participation) over ages 15 and over, the participation weights from ParticipationRateTable. NaN when the country has no table, no
        /// participation table or no pyramid, so a caller cannot mistake "unknown" for zero.
        /// </summary>
        public static float CompositionRate(CountryId id, float[] counts)
        {
            float[] rates = For(id);
            float[] participation = ParticipationRateTable.For(id);
            if (rates == null || participation == null || counts == null) { return float.NaN; }
            float weighted = 0f, labourForce = 0f;
            for (int k = 3; k < PopulationCohorts.CohortCount && k < counts.Length; k++)   // band 3 = 15–19
            {
                float lf = counts[k] * participation[k];
                weighted += lf * rates[k];
                labourForce += lf;
            }
            return labourForce > 0f ? weighted / labourForce : float.NaN;
        }

        /// <summary>The three Eurostat groups laid onto the 21 bands: 0–14 zero (no labour force), 15–24 young, 25–54 prime, 55 and over older.</summary>
        private static float[] Groups(float young, float prime, float older)
        {
            var bands = new float[PopulationCohorts.CohortCount];
            for (int k = 3; k < PopulationCohorts.CohortCount; k++) { bands[k] = k <= 4 ? young : k <= 10 ? prime : older; }
            return bands;
        }

        /// <summary>The seven BLS groups laid onto the 21 bands: 15–19 takes 16–19, 20–24, 25–34 (two bands), 35–44, 45–54, 55–64, 65 and over (the rest).</summary>
        private static float[] UsBands(float b16To19, float b20To24, float b25To34, float b35To44, float b45To54, float b55To64, float b65Plus)
        {
            var bands = new float[PopulationCohorts.CohortCount];
            for (int k = 3; k < PopulationCohorts.CohortCount; k++)
            {
                bands[k] = k == 3 ? b16To19 : k == 4 ? b20To24 : k <= 6 ? b25To34 : k <= 8 ? b35To44 : k <= 10 ? b45To54 : k <= 12 ? b55To64 : b65Plus;
            }
            return bands;
        }
    }
}
