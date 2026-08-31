using System;
using System.Collections.Generic;

namespace PoliSim.Data
{
    /// <summary>
    /// P-I2 stage 2 — **one country's aging-step rates**, all three DERIVED from that country's own
    /// published stock in two consecutive years.
    ///
    /// <para><b>Why the step is parameterised this way and not as a life table.</b> The spec-let's §2
    /// specifies deaths, then a shift, then births, then migration by a sourced age profile — four
    /// sourced families. **Eurostat publishes all four for the EU five and NONE of them for the USA**,
    /// and the search for a US general-population life table by single year of age came up empty
    /// (SSA's actuarial table refuses an ordinary request at 403; the CDC data portal carries life
    /// expectancy and state tables, not a national q(x) series). The fork was decided at **D-6** and is
    /// strikeable: **one method for six countries, derived from each country's own publisher**, beats
    /// the best data for five and something else for the sixth, because the alternative is two
    /// demographic models wearing one name.</para>
    ///
    /// <para>⚠ <b>The cost, stated rather than discovered: mortality and net migration are NOT
    /// SEPARABLE here.</b> `Survival` is the observed cohort ratio, which is what actually happened to
    /// that cohort — deaths and net migration together. That is why the immigration lever cannot hook
    /// through this array and must be an additive, age-profiled inflow on top of it (spec-let §4.4);
    /// re-pointing the two demographic levers is its own stage and this comment is the bill it
    /// owes.</para>
    ///
    /// <para>⚠ <b>`Crossing` REPLACES the spec-let's uniform 1/5 assumption with an observed
    /// fraction.</b> §2 called shifting one fifth of a five-year band per year *"the standard, and
    /// standardly wrong, approximation"* — it assumes people are spread evenly inside the band. The
    /// observed fractions are near 0.20 in the young bands and fall to **0.114–0.199** in the old ones,
    /// where the pyramid is steepest and where the dependency ratio is decided. Since the single-year
    /// data that would justify 1/5 was fetched anyway, using it costs nothing and removes an
    /// assumption.</para>
    /// </summary>
    public class CohortStepRates
    {
        /// <summary>DERIVED, per band: the share of the band's members present a year later, one year
        /// older. Deaths and net migration together — see the type's own warning.</summary>
        public readonly float[] Survival;

        /// <summary>DERIVED, per band 0..19: the share of a band's survivors who have crossed into the
        /// NEXT band. Index 20 has no successor and carries no crossing fraction.</summary>
        public readonly float[] Crossing;

        /// <summary>DERIVED: women as a share of ALL people aged 15-49, from the same publisher and year.
        /// It carries its own weight because the substrate is sex-blind and cannot supply the fertility
        /// denominator: a hard-coded 0.5 would be an invented figure, and none of the six is 0.5 - the
        /// observed range is 0.4849 (Sweden) to 0.5023 (France).</summary>
        public readonly float FemaleShareOfChildbearingAge;

        /// <summary>DERIVED: births in the year per woman aged 15-49 at its start — the general
        /// fertility rate, on the standard denominator.</summary>
        public readonly float GeneralFertilityRate;

        public CohortStepRates(float[] survival, float[] crossing, float generalFertilityRate,
            float femaleShareOfChildbearingAge)
        {
            if (survival == null || survival.Length != PopulationCohorts.CohortCount)
            {
                throw new ArgumentException($"Survival needs {PopulationCohorts.CohortCount} bands.", nameof(survival));
            }

            if (crossing == null || crossing.Length != PopulationCohorts.CohortCount - 1)
            {
                throw new ArgumentException($"Crossing needs {PopulationCohorts.CohortCount - 1} bands.", nameof(crossing));
            }

            Survival = survival;
            Crossing = crossing;
            GeneralFertilityRate = generalFertilityRate;
            FemaleShareOfChildbearingAge = femaleShareOfChildbearingAge;
        }
    }

    /// <summary>
    /// P-I2 stage 2 — **the sourced aging-step rates for all six countries.**
    ///
    /// <para><b>SOURCED, per country, from the same two publishers stage 1 used</b>, so that the rates
    /// and the pyramid they step cannot come from different worlds:</para>
    /// <list type="bullet">
    /// <item><description><b>EU five</b> — Eurostat <c>demo_pjan</c>, single years of age,
    /// <c>time=2023</c> and <c>time=2024</c> (both 1 January), <c>sex=T</c> for the stock and
    /// <c>sex=F</c> for the fertility denominator. Fetched 2026-08-31.</description></item>
    /// <item><description><b>USA</b> — US Census Bureau PEP vintage 2024,
    /// <c>nc-est2024-agesex-res.csv</c>, columns <c>POPESTIMATE2023</c> and <c>POPESTIMATE2024</c>
    /// (both 1 July), <c>SEX=0</c> and <c>SEX=2</c>.</description></item>
    /// </list>
    ///
    /// <para>The three derivations, each a ratio of two published counts and nothing else:</para>
    /// <list type="number">
    /// <item><description><b>Survival</b>, band k: Σ(ages 5k+1…5k+5 in t+1) / Σ(ages 5k…5k+4 in
    /// t).</description></item>
    /// <item><description><b>Crossing</b>, band k: (age 5k+5 in t+1) / Σ(ages 5k+1…5k+5 in
    /// t+1).</description></item>
    /// <item><description><b>General fertility rate</b>: (age 0 in t+1) / Σ(women 15–49 in
    /// t).</description></item>
    /// </list>
    ///
    /// <para>⚠ <b>ONE ASSUMPTION, and it is named because it cannot be derived.</b> The open 100+ band
    /// receives survivors from the 95–99 band, and the published stock cannot say how many. Its survival
    /// is computed as <c>(100+ in t+1 − s₁₉ × (age 99 in t)) / (100+ in t)</c> — **the age-99 cohort is
    /// assumed to survive at its own band's rate.** It reaches 0.50–0.68, which is the right order for
    /// centenarians, and it governs **under 0.03 %** of every one of the six populations.</para>
    /// </summary>
    public static class CohortStepRateTable
    {
        /// <summary>SOURCED / DERIVED — see the type's own comment for both publishers and all three
        /// derivations.</summary>
        public static readonly Dictionary<CountryId, CohortStepRates> Rates = new Dictionary<CountryId, CohortStepRates>
        {
            { CountryId.Sweden, new CohortStepRates(
                survival: new float[] { 1.001919f, 0.996932f, 1.000071f, 1.005313f, 1.013778f, 1.007094f, 1.003349f, 1.001112f, 1.000786f, 0.999894f, 0.998983f, 0.996890f, 0.994330f, 0.990203f, 0.983272f, 0.970802f, 0.947222f, 0.899740f, 0.818248f, 0.716649f, 0.551637f },
                crossing: new float[] { 0.208592f, 0.200958f, 0.198559f, 0.195999f, 0.202970f, 0.222248f, 0.194566f, 0.189546f, 0.192543f, 0.204040f, 0.200674f, 0.186983f, 0.194370f, 0.195100f, 0.205242f, 0.174115f, 0.160694f, 0.142328f, 0.114422f, 0.084282f },
                generalFertilityRate: 0.044823f,
                femaleShareOfChildbearingAge: 0.484906f) },
            { CountryId.Germany, new CohortStepRates(
                survival: new float[] { 1.010546f, 1.008426f, 1.009865f, 1.028858f, 1.033903f, 1.023208f, 1.012292f, 1.008483f, 1.005287f, 1.002105f, 0.998288f, 0.994925f, 0.990950f, 0.985222f, 0.977710f, 0.963322f, 0.938439f, 0.887912f, 0.794938f, 0.695297f, 0.544036f },
                crossing: new float[] { 0.205858f, 0.192050f, 0.203834f, 0.204479f, 0.213616f, 0.203672f, 0.210210f, 0.196159f, 0.190850f, 0.198146f, 0.220789f, 0.199571f, 0.181284f, 0.184450f, 0.174312f, 0.208555f, 0.174412f, 0.111042f, 0.111746f, 0.075686f },
                generalFertilityRate: 0.041320f,
                femaleShareOfChildbearingAge: 0.489252f) },
            { CountryId.France, new CohortStepRates(
                survival: new float[] { 1.009216f, 1.008043f, 1.005485f, 0.984684f, 0.999780f, 1.017502f, 1.014970f, 1.010323f, 1.005747f, 1.003440f, 1.000544f, 1.000504f, 0.995647f, 0.991265f, 0.985939f, 0.978516f, 0.959664f, 0.929851f, 0.870298f, 0.780728f, 0.625256f },
                crossing: new float[] { 0.208204f, 0.208098f, 0.202805f, 0.190000f, 0.196820f, 0.200375f, 0.205029f, 0.196453f, 0.190802f, 0.214261f, 0.194047f, 0.199280f, 0.193087f, 0.192442f, 0.194572f, 0.160652f, 0.183775f, 0.158120f, 0.118550f, 0.093216f },
                generalFertilityRate: 0.043332f,
                femaleShareOfChildbearingAge: 0.502275f) },
            { CountryId.Italy, new CohortStepRates(
                survival: new float[] { 1.007693f, 1.005117f, 1.004949f, 1.010084f, 1.010486f, 1.010613f, 1.009593f, 1.007125f, 1.004004f, 1.001728f, 1.000010f, 0.998085f, 0.995319f, 0.991277f, 0.983919f, 0.972021f, 0.947564f, 0.900224f, 0.820406f, 0.725616f, 0.561779f },
                crossing: new float[] { 0.214364f, 0.209049f, 0.206872f, 0.199701f, 0.203015f, 0.204143f, 0.205150f, 0.207452f, 0.214116f, 0.208817f, 0.201006f, 0.190679f, 0.184799f, 0.185880f, 0.205092f, 0.173146f, 0.179938f, 0.145563f, 0.109440f, 0.084432f },
                generalFertilityRate: 0.032679f,
                femaleShareOfChildbearingAge: 0.491297f) },
            { CountryId.Poland, new CohortStepRates(
                survival: new float[] { 1.003908f, 1.000901f, 0.999811f, 0.998858f, 0.999549f, 0.999452f, 0.999210f, 0.998624f, 0.997870f, 0.996682f, 0.994811f, 0.991786f, 0.986910f, 0.980520f, 0.972326f, 0.959122f, 0.932646f, 0.887114f, 0.812587f, 0.736871f, 0.741184f },
                crossing: new float[] { 0.222029f, 0.192912f, 0.202910f, 0.188580f, 0.209270f, 0.216076f, 0.208908f, 0.214134f, 0.195171f, 0.186340f, 0.187217f, 0.204979f, 0.215021f, 0.188335f, 0.175837f, 0.148393f, 0.176659f, 0.138801f, 0.104447f, 0.085435f },
                generalFertilityRate: 0.032155f,
                femaleShareOfChildbearingAge: 0.491196f) },
            { CountryId.USA, new CohortStepRates(
                survival: new float[] { 1.012444f, 1.009395f, 1.007816f, 1.012147f, 1.016295f, 1.016841f, 1.013059f, 1.008920f, 1.004615f, 1.001873f, 0.998443f, 0.995248f, 0.990997f, 0.986301f, 0.978641f, 0.965126f, 0.940106f, 0.900323f, 0.842941f, 0.773118f, 0.632736f },
                crossing: new float[] { 0.205708f, 0.203055f, 0.206738f, 0.199393f, 0.199381f, 0.205717f, 0.197129f, 0.195503f, 0.191021f, 0.194290f, 0.197766f, 0.208780f, 0.193620f, 0.185700f, 0.181629f, 0.159458f, 0.152862f, 0.141192f, 0.124474f, 0.094662f },
                generalFertilityRate: 0.047242f,
                femaleShareOfChildbearingAge: 0.493955f) },
        };

        /// <summary>
        /// SOURCED — the PRIOR-YEAR pyramids (Eurostat 1 January 2023; US Census 1 July 2023), from the
        /// same publishers, the same fetch and the same fold as `PopulationPyramids`.
        ///
        /// <para>⚠ <b>They exist for exactly one purpose and are not part of the seed.</b> The model
        /// starts in 2024. 2023 is here so `CohortAgingStepDiagnostic` can HINDCAST — step the earlier
        /// pyramid one year and check the result against the later one, band by band. The rates were
        /// derived from these two stocks, so the step must reproduce the second; **it is the only
        /// assertion in the stage that can catch an arithmetic error**, and it needs a year the step did
        /// not choose to be checked against.</para>
        /// </summary>
        private static readonly Dictionary<CountryId, float[]> PriorYear = new Dictionary<CountryId, float[]>
        {
            { CountryId.Sweden, new float[] { 0.576367f, 0.621155f, 0.631571f, 0.602847f, 0.585490f, 0.666652f, 0.780805f, 0.694511f, 0.647850f, 0.652876f, 0.660484f, 0.678719f, 0.575092f, 0.542740f, 0.524383f, 0.497667f, 0.308331f, 0.171953f, 0.078387f, 0.020989f, 0.002687f } },
            { CountryId.Germany, new float[] { 3.864748f, 3.952215f, 3.802005f, 3.864600f, 4.406873f, 4.826247f, 5.449752f, 5.458911f, 5.284200f, 4.832604f, 5.932063f, 6.796205f, 6.157874f, 5.070899f, 4.295215f, 3.103205f, 3.321449f, 1.873598f, 0.655537f, 0.153543f, 0.016758f } },
            { CountryId.France, new float[] { 3.522456f, 3.955233f, 4.279577f, 4.211370f, 3.957159f, 3.796389f, 4.123229f, 4.242623f, 4.338634f, 4.241845f, 4.523772f, 4.456072f, 4.202080f, 3.904313f, 3.715557f, 2.745800f, 1.791483f, 1.338512f, 0.702118f, 0.199032f, 0.029956f } },
            { CountryId.Italy, new float[] { 2.085244f, 2.464447f, 2.794408f, 2.895108f, 2.938344f, 2.995670f, 3.234123f, 3.366663f, 3.773564f, 4.503977f, 4.791465f, 4.810601f, 4.162290f, 3.601512f, 3.312180f, 2.760935f, 2.263224f, 1.424476f, 0.639782f, 0.158743f, 0.020445f } },
            { CountryId.Poland, new float[] { 1.745504f, 1.923273f, 1.999562f, 1.748395f, 1.812973f, 2.128895f, 2.545843f, 2.945612f, 2.967375f, 2.763666f, 2.299688f, 2.157846f, 2.404218f, 2.490145f, 2.043138f, 1.182384f, 0.799155f, 0.523005f, 0.220118f, 0.047077f, 0.005864f } },
            { CountryId.USA, new float[] { 18.632397f, 20.284053f, 20.943661f, 22.228073f, 22.043447f, 22.278222f, 23.770395f, 22.697636f, 22.015321f, 19.906918f, 20.738630f, 20.654912f, 21.285859f, 19.181126f, 15.535630f, 11.380104f, 6.975294f, 3.811482f, 1.790447f, 0.562781f, 0.089843f } },
        };

        /// <summary>The prior-year pyramid for a country, or null. A copy, for the same reason
        /// `PopulationPyramids.For` returns one.</summary>
        public static float[] PriorYearBands(CountryId id) =>
            PriorYear.TryGetValue(id, out float[] bands) ? (float[])bands.Clone() : null;

        public static CohortStepRates For(CountryId id) =>
            Rates.TryGetValue(id, out CohortStepRates rates) ? rates : null;
    }
}
