using System.Collections.Generic;
using PoliSim.Data.Generated;

namespace PoliSim.Data
{
    /// <summary>
    /// K276 (Elias, 2026-10-29, answering K245): LABOUR-FORCE PARTICIPATION RISES BY COHORT AND WITH STATUTORY PENSION AGES. Before this item the rates by
    /// age (`ParticipationRateTable`, Eurostat `lfsa_argan` 2024 and BLS CPS 2024) were held at the seed's for the whole run, so the labour input aged
    /// with the pyramid and with nothing else - the cause K245 named for Italy's real growth 2024-2031 (the no-policy run's -0.05 % a year against the
    /// WEO's 0.65 %). The publishers project those rates to rise - each new cohort of women more active than the one before, and the older bands held
    /// in work by the pension reforms already legislated - and this class carries that projection onto the model's five-year bands.
    ///
    /// <para><b>The sources</b> (generated, `ParticipationPath`, by `Tools/k276_participation_prep.pl`): the EU five, the EC Ageing Report 2024's
    /// statistical annex, the projected participation rate by age group, both sexes, a figure a year 2022-2070 (tables II.1.29 young 20-24, II.1.30
    /// prime-age 25-54, II.1.31 older 55-64, II.1.32 oldest 65-74); the USA, CBO's March 2025 Long-Term Budget Outlook, the rates by age group and sex,
    /// 2025-2099.</para>
    ///
    /// <para><b>The model.</b> A band's rate in a year is the seed's sourced rate plus the CHANGE, in points, its source projects for the band's group
    /// from the source's base year to that year - the seed table stays the level (a seed is held against a source in the source's own year and
    /// perimeter, §519), the projection supplies the path. The EU five's base is 2024, the seed table's own year; the USA's is 2025, CBO's first year
    /// (the file carries no 2024 row), and before it the change is zero. A year before the source's first holds the first year's change (Italy's run
    /// opens in 2022 and reads 2022's, below the seed); after the last it holds the last (2070; the USA 2099). A band's rate never leaves 0-100 %.</para>
    ///
    /// <para><b>The groups onto the bands, DECLARED.</b> The EU five: 20-24 the young group, the six bands 25-54 the prime-age group, 55-59 and 60-64 the
    /// older, 65-69 and 70-74 the oldest; 15-19 and 75+ carry no projection (the annex projects neither) and hold the seed's. The USA: CBO's five-year
    /// groups band for band; 15-19 the two groups 16-17 and 18-19 equally; 60-64 the groups 60-61 and 62-64 by their years (two fifths, three fifths);
    /// 70-74 and 75-79 the group 70-79, 80-84 and 85-89 the group 80-89; 90+ holds the seed's. CBO publishes no rate by age for the two sexes together:
    /// the change is the mean of the men's and the women's [AUTHORED - equal weights; the tool prints the widest gap between the sexes' changes, which
    /// bounds the miss against a population-weighted mean when multiplied by the weight's distance from one half].</para>
    ///
    /// <para><b>The pension age.</b> The Ageing Report's and CBO's projections are made under the pension law as legislated, so the statute's own
    /// path (`PensionAgeStatute.AgeInForce(id, year)`) is INSIDE the projected rates; the response of the bands a pension age crosses
    /// (`PensionParticipationResponse`) now measures from the statute's age for the year, not from its age at the seed, and so moves participation
    /// only where the country's age in force departs from its statute's - a player's pension-age bill. Counting the statute's own steps on top of a
    /// path that already holds them was the double count this item removes. Review 1 (M1): the Ageing Report keeps Sweden's and Italy's indexation
    /// going past the dates their statutes publish, so past those horizons the response measures from the age the Ageing Report assumed (its country
    /// fiches, `PensionParticipationResponse.ReferenceAge`) and the no-policy run carries the carried statute's shortfall as a negative response.</para>
    /// </summary>
    public static class ParticipationCohortPath
    {
        /// <summary>The EU five's base year: the seed table's own (Eurostat `lfsa_argan` 2024).</summary>
        public const int EuBaseYear = 2024;

        /// <summary>The USA's base year: CBO's first projected year (the seed table is BLS 2024; the file carries no 2024 row).</summary>
        public const int UsBaseYear = ParticipationPath.UsFirstYear;

        /// <summary>The band's change in a year is a pure function of the country and the year: computed once and kept (no world state).</summary>
        private static readonly Dictionary<long, float[]> Cache = new Dictionary<long, float[]>();
        private static readonly object CacheLock = new object();

        /// <summary>Whether a sourced path exists for the country.</summary>
        public static bool Has(CountryId id) => id == CountryId.USA || ParticipationPath.Prime.ContainsKey(id);

        /// <summary>The year the source's change is measured from.</summary>
        public static int BaseYear(CountryId id) => id == CountryId.USA ? UsBaseYear : EuBaseYear;

        /// <summary>The change in points, per band (index-aligned with <see cref="PopulationCohorts.Counts"/>), from the source's base year to
        /// <paramref name="year"/>; null without a path. The array is shared: read it, never write it.</summary>
        public static float[] DeltaPoints(CountryId id, int year)
        {
            if (!Has(id)) { return null; }
            long key = ((long)id << 32) | (uint)year;
            lock (CacheLock)
            {
                if (Cache.TryGetValue(key, out float[] hit)) { return hit; }
                float[] d = id == CountryId.USA ? Usa(year) : Eu(id, year);
                Cache[key] = d;
                return d;
            }
        }

        /// <summary>A source series' value at a year, held before its first and after its last.</summary>
        private static float At(float[] series, int first, int year)
        {
            int i = year - first;
            if (i < 0) { i = 0; }
            if (i > series.Length - 1) { i = series.Length - 1; }
            return series[i];
        }

        private static float[] Eu(CountryId id, int year)
        {
            var d = new float[PopulationCohorts.CohortCount];
            int first = ParticipationPath.EuFirstYear;
            float Change(Dictionary<CountryId, float[]> g) => At(g[id], first, year) - At(g[id], first, EuBaseYear);
            float young = Change(ParticipationPath.Young), prime = Change(ParticipationPath.Prime), older = Change(ParticipationPath.Older), oldest = Change(ParticipationPath.Oldest);
            d[4] = young;                                   // 20-24
            for (int b = 5; b <= 10; b++) { d[b] = prime; }  // 25-29 ... 50-54
            d[11] = older; d[12] = older;                   // 55-59, 60-64
            d[13] = oldest; d[14] = oldest;                 // 65-69, 70-74
            return d;
        }

        private static float[] Usa(int year)
        {
            var d = new float[PopulationCohorts.CohortCount];
            if (year < UsBaseYear) { return d; }
            int first = ParticipationPath.UsFirstYear;
            float G(int lo)
            {
                for (int k = 0; k < ParticipationPath.UsGroups.Length; k++)
                {
                    if (ParticipationPath.UsGroups[k][0] != lo) { continue; }
                    float m = At(ParticipationPath.UsMale[k], first, year) - At(ParticipationPath.UsMale[k], first, UsBaseYear);
                    float f = At(ParticipationPath.UsFemale[k], first, year) - At(ParticipationPath.UsFemale[k], first, UsBaseYear);
                    return 0.5f * (m + f);
                }
                throw new KeyNotFoundException("CBO carries no age group from " + lo);
            }
            d[3] = 0.5f * (G(16) + G(18));                  // 15-19
            for (int b = 4; b <= 11; b++) { d[b] = G(b * PopulationCohorts.CohortWidth); }   // 20-24 ... 55-59
            d[12] = 0.4f * G(60) + 0.6f * G(62);            // 60-64: 60-61 and 62-64 by their years
            d[13] = G(65);                                  // 65-69
            d[14] = G(70); d[15] = d[14];                   // 70-74, 75-79
            d[16] = G(80); d[17] = d[16];                   // 80-84, 85-89
            return d;
        }
    }
}
