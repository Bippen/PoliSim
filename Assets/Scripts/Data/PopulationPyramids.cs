using System.Collections.Generic;

namespace PoliSim.Data
{
    /// <summary>
    /// P-I2 stage 1 — **the sourced age pyramids**, six countries, 21 five-year bands each.
    ///
    /// <para><b>SOURCED, and the sources are named per country because they are not the same source and
    /// do not share a reference date.</b></para>
    ///
    /// <list type="bullet">
    /// <item><description><b>Sweden, Germany, France, Italy, Poland</b> — Eurostat <c>demo_pjan</c>,
    /// *"Population on 1 January by age and sex"*, DOI 10.2908/demo_pjan, dimension <c>sex=T</c>,
    /// <c>time=2024</c>. **Reference date: 1 January 2024.** Fetched 2026-08-31 from the Eurostat
    /// dissemination API. The dataset publishes SINGLE years of age (Y_LT1 … Y99, Y_OPEN); the bands
    /// below are those single years folded into fives, which is a sum and not an estimate.</description></item>
    /// <item><description><b>The USA</b> — US Census Bureau, Population Estimates Program, Vintage 2024
    /// national characteristics file <c>nc-est2024-agesex-res.csv</c>, column <c>POPESTIMATE2024</c>,
    /// <c>SEX=0</c> (both sexes), single years of age 0…100+. ⚠ **Reference date: 1 JULY 2024, not 1
    /// January** — the Census estimates programme publishes mid-year, and no 1 January series exists to
    /// match Eurostat's. The six-month offset is stated rather than hidden, and it is smaller than the
    /// difference between any two of the vintages these seeds replaced.</description></item>
    /// </list>
    ///
    /// <para>⚠ <b>The US source was BILLED at C-C13 and is now discharged</b> — the spec-let's §6 marked
    /// it *"not identified precisely"*, and naming a file that was not opened would have been an
    /// invented figure in a technical costume. It was opened.</para>
    ///
    /// <para>⚠ <b>Why the published total is carried SEPARATELY.</b> `PublishedTotal` is each source's
    /// OWN total — Eurostat's <c>TOTAL</c> age class, the Census file's <c>AGE=999</c> row — transcribed
    /// independently of the 21 bands. It is not the sum of them. **That is what makes
    /// `CohortSubstrateDiagnostic`'s reconciliation a real check rather than a tautology**: a slip in any
    /// one of 126 transcribed band figures breaks the sum against a number that came from somewhere
    /// else. All six reconciled to the person at the fetch, and the check asserts they still do.</para>
    ///
    /// <para>Units: **millions of persons**, matching `EconomyState.Population`.</para>
    /// </summary>
    public static class PopulationPyramids
    {
        /// <summary>SOURCED — see the type's own comment. Index 0 = ages 0-4, index 20 = 100+.</summary>
        public static readonly Dictionary<CountryId, float[]> Bands = new Dictionary<CountryId, float[]>
        {
            { CountryId.Sweden, new float[] { 0.557673f, 0.615262f, 0.630646f, 0.612678f, 0.591868f, 0.642642f, 0.780206f, 0.715922f, 0.655310f, 0.644445f, 0.660604f, 0.682501f, 0.587198f, 0.543719f, 0.514637f, 0.504840f, 0.329247f, 0.179625f, 0.078821f, 0.021113f, 0.002750f } },
            { CountryId.Germany, new float[] { 3.791359f, 4.024080f, 3.822307f, 3.945710f, 4.396018f, 4.905765f, 5.362851f, 5.584994f, 5.378213f, 4.897022f, 5.573992f, 6.719765f, 6.345372f, 5.180675f, 4.388965f, 3.097954f, 3.196790f, 2.022499f, 0.647607f, 0.156910f, 0.017197f } },
            { CountryId.France, new float[] { 3.444015f, 3.897495f, 4.260069f, 4.231642f, 3.965519f, 3.867494f, 4.100931f, 4.302374f, 4.373072f, 4.177024f, 4.559920f, 4.448166f, 4.264407f, 3.933254f, 3.695325f, 2.967945f, 1.834912f, 1.363768f, 0.735411f, 0.213345f, 0.033215f } },
            { CountryId.Italy, new float[] { 2.031476f, 2.409672f, 2.745119f, 2.921261f, 2.950359f, 3.012211f, 3.213337f, 3.357101f, 3.680853f, 4.380844f, 4.770527f, 4.848988f, 4.292746f, 3.672071f, 3.254150f, 2.887393f, 2.223335f, 1.481572f, 0.654100f, 0.162904f, 0.021211f } },
            { CountryId.Poland, new float[] { 1.630387f, 1.942717f, 1.964886f, 1.822716f, 1.762263f, 2.047207f, 2.472155f, 2.843098f, 3.013031f, 2.819135f, 2.372723f, 2.129748f, 2.301235f, 2.491981f, 2.097126f, 1.315082f, 0.781945f, 0.531235f, 0.224582f, 0.050408f, 0.007310f } },
            { CountryId.USA, new float[] { 18.599314f, 20.197672f, 20.901154f, 22.375825f, 22.421936f, 22.459876f, 23.993988f, 23.170072f, 22.369152f, 20.294049f, 20.486307f, 20.359928f, 21.301797f, 19.489471f, 15.955491f, 11.993326f, 7.306487f, 3.949451f, 1.805887f, 0.581771f, 0.098034f } },
        };

        /// <summary>SOURCED — each publisher's own total, transcribed independently of the bands above so
        /// that the two can disagree. Millions of persons.
        /// Eurostat <c>demo_pjan</c> age class TOTAL, 1 January 2024: Sweden 10 551 707 · Germany
        /// 83 456 045 · France 68 669 303 · Italy 58 971 230 · Poland 36 620 970.
        /// Census <c>nc-est2024-agesex-res.csv</c>, SEX=0, AGE=999, POPESTIMATE2024: 340 110 988.</summary>
        public static readonly Dictionary<CountryId, float> PublishedTotal = new Dictionary<CountryId, float>
        {
            { CountryId.Sweden,  10.551707f },
            { CountryId.Germany, 83.456045f },
            { CountryId.France,  68.669303f },
            { CountryId.Italy,   58.971230f },
            { CountryId.Poland,  36.620970f },
            { CountryId.USA,    340.110988f },
        };

        /// <summary>A fresh pyramid for a country, or null if none is seeded. The copy matters: the
        /// substrate becomes mutable state the moment the aging step lands, and a country holding a
        /// reference into this static table would age every other game with it.</summary>
        public static PopulationCohorts For(CountryId id) =>
            Bands.TryGetValue(id, out float[] bands) ? new PopulationCohorts(bands) : null;
    }
}
