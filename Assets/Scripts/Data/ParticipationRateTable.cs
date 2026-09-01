using System.Collections.Generic;

namespace PoliSim.Data
{
    /// <summary>
    /// F2 step 5 (2026-09-02) — labour-force participation BY AGE, the last clause of F2's done-when
    /// (cohort spec-let §3: participation "derived: Σ(cohort × sourced participation rate for that
    /// cohort) / working-age population"). One rate per five-year band of <see cref="PopulationCohorts"/>,
    /// as a fraction, SOURCED per country:
    ///
    /// <para><b>EU five — Eurostat `lfsa_argan`</b> (labour force participation rates by citizenship),
    /// `sex=T`, `citizen=TOTAL`, `unit=PC`, `time=2024`, twelve age classes `Y15-19` … `Y70-74`, fetched
    /// 2026-09-02 through the dissemination API into `ElectionsData/participation/lfsa_argan_&lt;CC&gt;.json`
    /// (digests in that folder's README). ⚠ The LFS covers ages 15–74; the bands 75+ carry 0 here, which is
    /// a statement about the survey's frame, not about people over 75 — and it is stated rather than
    /// extrapolated from the 70–74 figure.</para>
    ///
    /// <para><b>USA — BLS Current Population Survey, LNU series (not seasonally adjusted), 2024 annual
    /// averages (period M13)</b>, fetched 2026-09-02 through the public API v1 into
    /// `ElectionsData/participation/bls/&lt;id&gt;.json`; every series id was VERIFIED against its own
    /// BLS series-title page before use (a recalled id is an invented figure wearing a costume - and the
    /// first recall, `LNS11324887` for "55–64", was in fact "16–24"). The published bands are wider than
    /// five years: `LNU01300012` 16–19 (used for the 15–19 band), `LNU01300036` 20–24, `LNU01300089` 25–34
    /// (both bands), `LNU01300091` 35–44 (both), `LNU01300093` 45–54 (both), `LNU01300094` 55–59,
    /// `LNU01300096` 60–64, `LNU01300097` 65 years and over (every band from 65–69 up, including 100+).
    /// ⚠ Two asymmetries with the EU five follow from the sources and are stated, not smoothed: the US
    /// youngest band starts at 16, and the US carries one rate for all of 65+ where the LFS carries two
    /// (65–69, 70–74) and then nothing.</para>
    ///
    /// <para><b>What is derived from it.</b> <see cref="StructuralRate"/>: the participation rate a
    /// pyramid IMPLIES at these rates — Σ(band × rate) over the population aged 15 and over, as a
    /// percentage. It replaces the typed `BaselineLaborForceParticipationRate` as the anchor
    /// `MacroSystem.ApplyLaborForceParticipationRate` reverts toward, so aging moves participation through
    /// the pyramid itself instead of through the retired dependency-ratio proxy, and the immigration
    /// lever moves it through the migrants' sourced age profile instead of through a second coupling.
    /// The 15+ base matches the "total population ages 15+" definition the retired seeds used.</para>
    /// </summary>
    public static class ParticipationRateTable
    {
        private static readonly float[] Sweden = Bands(41.3f, 73.0f, 86.2f, 90.8f, 93.3f, 93.9f, 94.1f, 93.8f, 90.6f, 74.4f, 31.2f, 11.8f, 0f);
        private static readonly float[] Germany = Bands(31.6f, 75.1f, 86.7f, 87.3f, 87.9f, 89.2f, 89.6f, 87.9f, 85.0f, 68.4f, 21.6f, 9.8f, 0f);
        private static readonly float[] France = Bands(18.9f, 67.9f, 87.1f, 87.9f, 89.3f, 88.9f, 89.7f, 87.9f, 81.6f, 45.2f, 11.6f, 3.3f, 0f);
        private static readonly float[] Italy = Bands(6.5f, 42.8f, 70.8f, 80.0f, 80.9f, 81.9f, 82.6f, 79.5f, 72.2f, 48.8f, 16.5f, 4.1f, 0f);
        private static readonly float[] Poland = Bands(6.4f, 59.4f, 86.6f, 89.4f, 90.0f, 90.6f, 89.0f, 85.7f, 77.6f, 43.9f, 12.3f, 5.7f, 0f);
        private static readonly float[] Usa = Bands(36.9f, 71.5f, 83.7f, 83.7f, 84.7f, 84.7f, 82.3f, 82.3f, 74.0f, 58.3f, 19.5f, 19.5f, 19.5f);

        /// <summary>Participation per band as fractions, index-aligned with <see cref="PopulationCohorts.Counts"/>; null for a country without a sourced table.</summary>
        public static readonly Dictionary<CountryId, float[]> Rates = new Dictionary<CountryId, float[]>
        {
            { CountryId.Sweden, Sweden }, { CountryId.Germany, Germany }, { CountryId.France, France },
            { CountryId.Italy, Italy }, { CountryId.Poland, Poland }, { CountryId.USA, Usa },
        };

        public static float[] For(CountryId id) => Rates.TryGetValue(id, out float[] r) ? r : null;

        /// <summary>
        /// The participation rate (percent) the pyramid implies at the country's sourced rates by age:
        /// Σ(band × rate) / population aged 15 and over. NaN when the country has no table or no pyramid,
        /// so a caller cannot mistake "unknown" for zero.
        /// </summary>
        public static float StructuralRate(CountryId id, float[] counts)
        {
            float[] rates = For(id);
            if (rates == null || counts == null) { return float.NaN; }
            float active = 0f, base15Plus = 0f;
            for (int k = 3; k < PopulationCohorts.CohortCount && k < counts.Length; k++)   // band 3 = 15–19
            {
                active += counts[k] * rates[k];
                base15Plus += counts[k];
            }
            return base15Plus > 0f ? 100f * active / base15Plus : float.NaN;
        }

        /// <summary>Twelve published percentages for 15–19 … 70–74 and one for everything from 75 up, laid onto the 21 bands as fractions (0–14 = 0).</summary>
        private static float[] Bands(float b15, float b20, float b25, float b30, float b35, float b40, float b45, float b50,
            float b55, float b60, float b65, float b70, float b75Plus)
        {
            var r = new float[PopulationCohorts.CohortCount];
            float[] twelve = { b15, b20, b25, b30, b35, b40, b45, b50, b55, b60, b65, b70 };
            for (int i = 0; i < twelve.Length; i++) { r[3 + i] = twelve[i] / 100f; }
            for (int k = 15; k < PopulationCohorts.CohortCount; k++) { r[k] = b75Plus / 100f; }
            return r;
        }
    }
}
