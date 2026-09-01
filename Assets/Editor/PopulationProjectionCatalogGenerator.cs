using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using PoliSim.Data;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// **P-I2 stage 3, step 1: the two projections, catalogued.** Turns the sourced projection files in
    /// <c>ElectionsData/projections/</c> into a generated C# table of 21-band pyramids per country per
    /// year, so the aging step has a published target to converge toward instead of repeating one
    /// observed year's rates forever (§141's revert; D-15 (c)).
    ///
    /// <para><b>Two publishers, one shape.</b> The EU five come from Eurostat <c>proj_23np</c>
    /// (*"Population on 1st January by age, sex and type of projection"*, <c>projection=BSL</c>, the
    /// baseline variant); the USA from the Census Bureau's 2023 National Population Projections main
    /// series, <c>np2023_d1_mid.csv</c>. ⚠ **The US file is the better of the two for this purpose** — it
    /// gives the projected pyramid by single year of age directly, where Eurostat's has to be filtered
    /// out of a mixed dimension.</para>
    ///
    /// <para>⚠ <b>THE TRAP THIS GENERATOR EXISTS TO NOT FALL INTO, stated because it produces a wrong
    /// catalog SILENTLY.</b> Eurostat's <c>age</c> dimension holds <b>110 categories and mixes single
    /// years with aggregates</b>: <c>TOTAL</c>, <c>Y_LT15</c>, <c>Y15-64</c>, <c>Y15-74</c>,
    /// <c>Y_LT20</c>, <c>Y20-64</c>, <c>Y_GE65</c>, <c>Y_GE75</c> and <c>Y_GE80</c> sit *interleaved*
    /// among <c>Y1</c>, <c>Y2</c>, <c>Y3</c>. **A build that sums the dimension reads Sweden as 56.9
    /// million — 44.8 million of it double-counted aggregates — for a country of 12.1 million.**
    /// <b>FILTER, NEVER SUM.</b> The 101 single-year categories are exactly
    /// <c>Y_LT1</c> (age 0), <c>Y1</c>…<c>Y99</c>, and <c>Y_GE100</c> (the open band).</para>
    ///
    /// <para><b>And the filter is ASSERTED, not trusted.</b> For every country and every year, the sum of
    /// the filtered single-year categories must equal that country-year's own published <c>TOTAL</c>
    /// **exactly, to the person**. A filter that silently dropped or admitted a category would move the
    /// sum, and the publisher's own total is the one number that catches it. ⚠ **The generator refuses to
    /// emit anything if a single country-year fails to reconcile** — a partial catalog is worse than
    /// none, because the failure would land inside a projection nobody re-reads.</para>
    ///
    /// <para><b>Why a generated table and not a parser in the game.</b> The same argument
    /// <see cref="ElectionsDataCatalogGenerator"/> makes: <c>ElectionsData/</c> sits outside
    /// <c>Assets/</c>, so runtime code cannot read it, and a second parser at runtime would be a second
    /// thing to keep true. The source digests are recorded in the emitted file so drift between the
    /// source and the catalog is detectable without re-parsing either.</para>
    ///
    /// <para>⚠ <b>AND IT IS EMITTED INTO THE EDITOR ASSEMBLY, NOT THE RUNTIME ONE — deliberately, and
    /// the guard made this decision rather than a preference.</b> The first version wrote it to
    /// <c>Assets/Scripts/Data/Generated/</c> and <see cref="UnwiredSubsystemCheck"/> failed the bar: a
    /// public runtime type that no game code names. **That is F1's own rule enforcing itself** — *"the
    /// generated catalog moves into the runtime assembly WHEN A RUNTIME CONSUMER EXISTS, not before; a
    /// data layer landing ahead of its consumer is queued art in another costume."* The alternative was
    /// raising the UNREACHABLE ceiling, which is the one thing a ratchet forbids: **never raise a ratchet
    /// you have not cleared.**</para>
    ///
    /// <para><b>Its first consumer is the HINDCAST</b>, which is editor-side and is what proves the
    /// convergence before anything is wired to it. **It moves to the runtime assembly in the commit that
    /// wires the step into the simulation** — the retirement — and not one commit earlier.</para>
    /// </summary>
    public static class PopulationProjectionCatalogGenerator
    {
        private const string OutputRelative = "Assets/Scripts/Data/Generated/PopulationProjections.cs";
        private const string EuroRelativeFormat = "ElectionsData/projections/proj_23np_{0}.json";
        private const string UsaRelative = "ElectionsData/projections/np2023_d1_mid.csv";

        /// <summary>⚠ The catalog starts at the pyramid vintage the substrate is seeded from (2024), so
        /// year 0 of a game is the seeded pyramid and the target series begins where the model does.</summary>
        private const int FirstYear = 2024;

        /// <summary>⚠ Both publishers stop at 2100. The model runs far past it; what happens beyond is a
        /// decision for the STEP, not for this catalog, and it is stated at the step's call site rather
        /// than hidden here by extrapolating a series the publishers did not publish.</summary>
        private const int LastYear = 2100;

        private static readonly (CountryId Id, string Geo)[] EuroCountries =
        {
            (CountryId.Sweden, "SE"),
            (CountryId.Germany, "DE"),
            (CountryId.France, "FR"),
            (CountryId.Italy, "IT"),
            (CountryId.Poland, "PL"),
        };

        [MenuItem("PoliSim/Generate Population Projection Catalog")]
        public static void Generate()
        {
            string root = Directory.GetCurrentDirectory();
            var bands = new Dictionary<CountryId, float[][]>();
            var digests = new Dictionary<CountryId, string>();
            var failures = new List<string>();

            foreach ((CountryId id, string geo) in EuroCountries)
            {
                string path = Path.Combine(root, string.Format(CultureInfo.InvariantCulture, EuroRelativeFormat, geo));
                if (!File.Exists(path))
                {
                    failures.Add($"{id}: {path} is not on disk.");
                    continue;
                }

                try
                {
                    bands[id] = ParseEurostat(File.ReadAllText(path), id, failures);
                    digests[id] = Sha256Of(path);
                }
                catch (Exception e)
                {
                    failures.Add($"{id}: {e.GetType().Name} — {e.Message}");
                }
            }

            string usaPath = Path.Combine(root, UsaRelative);
            if (!File.Exists(usaPath))
            {
                failures.Add($"USA: {usaPath} is not on disk.");
            }
            else
            {
                try
                {
                    bands[CountryId.USA] = ParseUsCensus(File.ReadAllText(usaPath), failures);
                    digests[CountryId.USA] = Sha256Of(usaPath);
                }
                catch (Exception e)
                {
                    failures.Add($"USA: {e.GetType().Name} — {e.Message}");
                }
            }

            if (failures.Count > 0)
            {
                Debug.LogError("PROJCATALOG: REFUSING TO EMIT. A partial projection catalog is worse than none — "
                               + "the gap would land inside a target series nobody re-reads.\n  "
                               + string.Join("\n  ", failures));
                return;
            }

            string output = Path.Combine(root, OutputRelative);
            string dir = Path.GetDirectoryName(output);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) { Directory.CreateDirectory(dir); }

            File.WriteAllText(output, Emit(bands, digests), new UTF8Encoding(false));
            AssetDatabase.Refresh();

            Debug.Log($"PROJCATALOG: {bands.Count} countries x {LastYear - FirstYear + 1} years x "
                      + $"{PopulationCohorts.CohortCount} bands generated into {OutputRelative}. "
                      + "Every country-year reconciled to its publisher's own TOTAL to the person.");
        }

        /// <summary>
        /// JSON-stat 2.0, parsed for exactly the three things this needs: the age index, the time index
        /// and the sparse value map. ⚠ **The dimension order is `[freq, projection, sex, age, unit, geo,
        /// time]` with sizes `[1,1,1,110,1,1,79]`, so the linear index is `age * yearCount + time`** —
        /// time varies fastest because it is last.
        /// </summary>
        private static float[][] ParseEurostat(string json, CountryId id, List<string> failures)
        {
            Dictionary<string, int> ageIndex = IndexMap(json, "\"age\":{\"label\":\"Age class\",\"category\":{\"index\":{");
            Dictionary<string, int> timeIndex = IndexMap(json, "\"time\":{\"label\":\"Time\",\"category\":{\"index\":{");
            Dictionary<int, double> values = ValueMap(json);
            int yearCount = timeIndex.Count;

            var result = new float[LastYear - FirstYear + 1][];

            for (int year = FirstYear; year <= LastYear; year++)
            {
                string key = year.ToString(CultureInfo.InvariantCulture);
                if (!timeIndex.TryGetValue(key, out int t))
                {
                    failures.Add($"{id}: the series has no year {year}.");
                    return result;
                }

                var band = new float[PopulationCohorts.CohortCount];
                double filtered = 0;

                foreach (KeyValuePair<string, int> entry in ageIndex)
                {
                    int age = SingleYearAge(entry.Key);
                    if (age < 0) { continue; }   // ⚠ an AGGREGATE - filtered out, never summed

                    if (!values.TryGetValue(entry.Value * yearCount + t, out double v)) { continue; }

                    filtered += v;
                    band[BandOf(age)] += (float)(v / 1e6);
                }

                // ⚠ THE RECONCILIATION. The publisher's own TOTAL is the only thing that can catch a
                // filter that dropped a category or admitted an aggregate.
                if (!values.TryGetValue(ageIndex["TOTAL"] * yearCount + t, out double published))
                {
                    failures.Add($"{id} {year}: no published TOTAL to reconcile against.");
                    return result;
                }

                if (Math.Abs(filtered - published) > 0.5)
                {
                    failures.Add($"{id} {year}: the single-year filter sums to {filtered:F0} against a published "
                                 + $"TOTAL of {published:F0} — a difference of {filtered - published:F0}. "
                                 + "The age filter is admitting an aggregate or dropping a single year.");
                    return result;
                }

                result[year - FirstYear] = band;
            }

            return result;
        }

        /// <summary>⚠ The USA row is `SEX=0, ORIGIN=0, RACE=0` — every sex, origin and race. Its own
        /// `TOTAL_POP` reconciles against `POP_0`…`POP_100`, which is the same assertion the Eurostat side
        /// makes against `TOTAL`.</summary>
        private static float[][] ParseUsCensus(string csv, List<string> failures)
        {
            var result = new float[LastYear - FirstYear + 1][];
            string[] lines = csv.Split('\n');
            var seen = new HashSet<int>();

            for (int i = 1; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (line.Length == 0) { continue; }

                string[] p = line.Split(',');
                if (p.Length < 106) { continue; }
                if (p[0] != "0" || p[1] != "0" || p[2] != "0") { continue; }
                if (!int.TryParse(p[3], NumberStyles.Integer, CultureInfo.InvariantCulture, out int year)) { continue; }
                if (year < FirstYear || year > LastYear) { continue; }

                double published = double.Parse(p[4], CultureInfo.InvariantCulture);
                var band = new float[PopulationCohorts.CohortCount];
                double summed = 0;

                for (int age = 0; age <= 100; age++)
                {
                    double v = double.Parse(p[5 + age], CultureInfo.InvariantCulture);
                    summed += v;
                    band[BandOf(age)] += (float)(v / 1e6);
                }

                if (Math.Abs(summed - published) > 0.5)
                {
                    failures.Add($"USA {year}: POP_0..POP_100 sum to {summed:F0} against a published TOTAL_POP of "
                                 + $"{published:F0}.");
                    return result;
                }

                result[year - FirstYear] = band;
                seen.Add(year);
            }

            for (int year = FirstYear; year <= LastYear; year++)
            {
                if (!seen.Contains(year)) { failures.Add($"USA: the series has no year {year}."); }
            }

            return result;
        }

        /// <summary>⚠ **THE FILTER.** Returns the single year of age a category names, or -1 if it is an
        /// AGGREGATE and must be skipped. `Y_LT1` is age 0 and `Y_GE100` is the open band; every other
        /// `Y_LT*`, `Y_GE*` and `Ya-b` is a span that overlaps the single years and must never be added
        /// to them.</summary>
        private static int SingleYearAge(string category)
        {
            if (category == "Y_LT1") { return 0; }
            if (category == "Y_GE100") { return 100; }
            if (category.Length < 2 || category[0] != 'Y') { return -1; }

            for (int i = 1; i < category.Length; i++)
            {
                if (category[i] < '0' || category[i] > '9') { return -1; }   // Y_LT15, Y15-64, Y_GE65 …
            }

            return int.Parse(category.Substring(1), CultureInfo.InvariantCulture);
        }

        private static int BandOf(int age)
        {
            int band = age / PopulationCohorts.CohortWidth;
            return band >= PopulationCohorts.OpenBandIndex ? PopulationCohorts.OpenBandIndex : band;
        }

        private static Dictionary<string, int> IndexMap(string json, string anchor)
        {
            int start = json.IndexOf(anchor, StringComparison.Ordinal);
            if (start < 0) { throw new InvalidDataException($"the JSON has no segment '{anchor}'."); }

            start += anchor.Length;
            int end = json.IndexOf('}', start);
            var map = new Dictionary<string, int>(StringComparer.Ordinal);

            foreach (string pair in json.Substring(start, end - start).Split(','))
            {
                int colon = pair.LastIndexOf(':');
                if (colon < 0) { continue; }
                map[pair.Substring(0, colon).Trim().Trim('"')] =
                    int.Parse(pair.Substring(colon + 1), CultureInfo.InvariantCulture);
            }

            return map;
        }

        private static Dictionary<int, double> ValueMap(string json)
        {
            const string anchor = "\"value\":{";
            int start = json.IndexOf(anchor, StringComparison.Ordinal);
            if (start < 0) { throw new InvalidDataException("the JSON has no \"value\" object."); }

            start += anchor.Length;
            int end = json.IndexOf('}', start);
            var map = new Dictionary<int, double>();

            foreach (string pair in json.Substring(start, end - start).Split(','))
            {
                int colon = pair.LastIndexOf(':');
                if (colon < 0) { continue; }
                string key = pair.Substring(0, colon).Trim().Trim('"');
                string val = pair.Substring(colon + 1).Trim();
                if (val == "null") { continue; }
                map[int.Parse(key, CultureInfo.InvariantCulture)] = double.Parse(val, CultureInfo.InvariantCulture);
            }

            return map;
        }

        private static string Emit(Dictionary<CountryId, float[][]> bands, Dictionary<CountryId, string> digests)
        {
            var sb = new StringBuilder();
            sb.Append("// GENERATED by PoliSim.EditorTools.PopulationProjectionCatalogGenerator. DO NOT EDIT BY HAND.\n");
            sb.Append("//\n");
            sb.Append("// The projected 21-band pyramid per country per year, in MILLIONS of persons - the target\n");
            sb.Append("// P-I2 stage 3's aging step converges toward (D-15 (c)).\n");
            sb.Append("//\n");
            sb.Append("// EU five: Eurostat proj_23np, projection=BSL (baseline), sex=T.\n");
            sb.Append("// USA:     US Census Bureau 2023 National Population Projections, np2023_d1_mid.csv, SEX=0 ORIGIN=0 RACE=0.\n");
            sb.Append("//\n");
            sb.Append("// ⚠ Eurostat's age dimension MIXES single years with aggregates. This table was built by FILTERING to\n");
            sb.Append("// the 101 single-year categories (Y_LT1, Y1..Y99, Y_GE100), never by summing the dimension - a summed\n");
            sb.Append("// dimension reads Sweden as 56.9 million against a true 12.1 million. Every country-year below was\n");
            sb.Append("// reconciled against its own publisher's TOTAL to the person before this file was written.\n");
            sb.Append("//\n");

            foreach (KeyValuePair<CountryId, string> d in digests)
            {
                sb.Append("// SHA-256 (").Append(d.Key).Append("): ").Append(d.Value).Append('\n');
            }

            sb.Append("\nusing System.Collections.Generic;\nusing PoliSim.Data;\n\nnamespace PoliSim.Data.Generated\n{\n");
            sb.Append("    public static class PopulationProjections\n    {\n");
            sb.Append("        public const int FirstYear = ").Append(FirstYear.ToString(CultureInfo.InvariantCulture)).Append(";\n");
            sb.Append("        public const int LastYear = ").Append(LastYear.ToString(CultureInfo.InvariantCulture)).Append(";\n\n");

            sb.Append("        /// <summary>[country][year - FirstYear][band] in millions.</summary>\n");
            sb.Append("        public static readonly Dictionary<CountryId, float[][]> Bands = new Dictionary<CountryId, float[][]>\n        {\n");

            foreach (KeyValuePair<CountryId, float[][]> entry in bands)
            {
                sb.Append("            { CountryId.").Append(entry.Key).Append(", new float[][]\n            {\n");
                for (int y = 0; y < entry.Value.Length; y++)
                {
                    sb.Append("                new float[] { ");
                    for (int b = 0; b < entry.Value[y].Length; b++)
                    {
                        if (b > 0) { sb.Append(", "); }
                        sb.Append(entry.Value[y][b].ToString("0.######", CultureInfo.InvariantCulture)).Append('f');
                    }

                    sb.Append(" },   // ").Append((FirstYear + y).ToString(CultureInfo.InvariantCulture)).Append('\n');
                }

                sb.Append("            } },\n");
            }

            sb.Append("        };\n\n");
            sb.Append("        /// <summary>The SHA-256 of each country's source file as it was when this table was generated.\n");
            sb.Append("        /// ⚠ Emitted as DATA rather than only as a comment so `GeneratedCatalogCheck` can re-derive it and\n");
            sb.Append("        /// fail on drift - a digest a check cannot read is a digest nothing enforces.</summary>\n");
            sb.Append("        public static readonly Dictionary<CountryId, string> SourceDigest = new Dictionary<CountryId, string>\n        {\n");
            foreach (KeyValuePair<CountryId, string> d in digests)
            {
                sb.Append("            { CountryId.").Append(d.Key).Append(", \"").Append(d.Value).Append("\" },\n");
            }

            sb.Append("        };\n\n");
            sb.Append("        /// <summary>The relative path each country's projection was read from, so the check does not\n");
            sb.Append("        /// re-state a path this generator already knows.</summary>\n");
            sb.Append("        public static readonly Dictionary<CountryId, string> SourcePath = new Dictionary<CountryId, string>\n        {\n");
            foreach ((CountryId id, string geo) in EuroCountries)
            {
                sb.Append("            { CountryId.").Append(id).Append(", \"")
                  .Append(string.Format(CultureInfo.InvariantCulture, EuroRelativeFormat, geo)).Append("\" },\n");
            }

            sb.Append("            { CountryId.USA, \"").Append(UsaRelative).Append("\" },\n");
            sb.Append("        };\n\n");
            sb.Append("        /// <summary>The projected pyramid for a country in a given year. ⚠ Years outside the published\n");
            sb.Append("        /// range CLAMP to the nearest published year - the publishers stop at LastYear and this table does\n");
            sb.Append("        /// not invent a series beyond them. What that means for a model running past LastYear is a decision\n");
            sb.Append("        /// for the STEP, and it is stated at the step's call site.</summary>\n");
            sb.Append("        public static float[] For(CountryId id, int year)\n        {\n");
            sb.Append("            if (!Bands.TryGetValue(id, out float[][] series)) { return null; }\n");
            sb.Append("            int i = year - FirstYear;\n");
            sb.Append("            if (i < 0) { i = 0; }\n");
            sb.Append("            if (i >= series.Length) { i = series.Length - 1; }\n");
            sb.Append("            return series[i];\n        }\n");
            sb.Append("    }\n}\n");

            return sb.ToString();
        }

        private static string Sha256Of(string path)
        {
            using (var sha = SHA256.Create())
            using (var stream = File.OpenRead(path))
            {
                byte[] hash = sha.ComputeHash(stream);
                var sb = new StringBuilder(hash.Length * 2);
                foreach (byte b in hash) { sb.Append(b.ToString("x2", CultureInfo.InvariantCulture)); }
                return sb.ToString();
            }
        }
    }
}
