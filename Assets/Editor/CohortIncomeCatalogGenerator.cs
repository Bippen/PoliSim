using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using PoliSim.Data;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// **F4-1 (2026-09-12, the backlog plan's S2; DS-2 ruled (c)): the cohort substrate's INCOME dimension,
    /// catalogued.** Turns the sourced income-by-age file S1 put on disk (`ElectionsData/income/income_by_age_2024.csv`,
    /// derived by `Tools/income_prep.pl` from Eurostat `ilc_di03` and the Census CPS ASEC table PINC-01) into a
    /// generated C# table of one log-normal per five-year cohort per country - DERIVED, no authored constant.
    ///
    /// <para><b>The form, as ruled.</b> Per source age band the publisher gives a MEAN and a MEDIAN. For a
    /// log-normal, mean ÷ median = exp(σ²/2), so <c>σ² = 2 ln(mean ÷ median)</c> and the scale is the median
    /// (μ = ln median). Nothing is fitted: two published figures pin the two parameters, and the national
    /// deciles (`ilc_di01`) are printed beside the result as a cross-check and never fitted to (DS-2 (c)).</para>
    ///
    /// <para>⚠ <b>The sub-band assumption, stated here because the projections generator's comment demands it
    /// of every step that makes it.</b> The publishers' bands are wider than the substrate's: Eurostat reports
    /// 16–24, 25–49, 50–64, 65+ and 75+; the CPS reports five-year bands from 25 to 74 and 15–24, 75+. **A cohort
    /// inside a wider source band carries the band's shape** - the 25–29 cohort and the 45–49 cohort share
    /// Eurostat's 25–49 parameters. That is the resolution the source has, and pretending to a finer one would
    /// be an authored figure. The mapping is by the cohort's midpoint, and the source band each cohort took is
    /// emitted beside its parameters so the diagnostic can print it.</para>
    ///
    /// <para>⚠ <b>Two concepts, carried as two and never compared across.</b> Eurostat's figure is EQUIVALISED
    /// NET household income per person (every member of a household carries its equivalised income - which is
    /// why a child has one); the CPS's is TOTAL MONEY INCOME per person 15+ WITH income, before tax and not
    /// equivalised. The catalog carries each country's concept and unit as data. What F4-1 reads from each is the
    /// band's SHAPE (mean ÷ median); the levels are in the source's own unit and year (EUR or USD, income year
    /// 2023) and are not converted here - a conversion is a decision for the reader that needs one.</para>
    ///
    /// <para><b>Below 15 there is no income dimension.</b> The 0–4, 5–9 and 10–14 cohorts carry 0 and 0 - not a
    /// NaN, which a JSON save would carry as a string - and `PopulationCohorts.HasIncome` reads a zero median as
    /// "no dimension". Eurostat publishes a figure for children (the household's, equivalised) but the substrate's
    /// income dimension is a PERSON's, for the tax and pension readers that will consume it (F4-2, PN-2), and a
    /// child's household income is not a child's tax base.</para>
    ///
    /// <para><b>Refuses to emit</b> if any mapped band lacks a mean or a median, if a mean is not above its
    /// median (the log-normal would have no σ), or if the anchor band (16+ for the five, 15+ for the USA) is
    /// missing - a partial catalog is worse than none, as the projections generator says. The source's SHA-256 is
    /// emitted as data so `GeneratedCatalogCheck` holds the catalog to the file.</para>
    ///
    /// <para><b>Emitted into the RUNTIME assembly</b>, because its consumer exists in the same commit:
    /// `WorldFactory` applies the seeds to every country's `PopulationCohorts` (the runtime reader), and the
    /// substrate diagnostic reads them back. Nothing else reads the dimension yet, and nothing in `EconomyState`
    /// derives from it - which is what makes F4-1 a READOUT (the trajectory family byte-identical, DS-2b).</para>
    /// </summary>
    public static class CohortIncomeCatalogGenerator
    {
        public const string SourceRelative = "ElectionsData/income/income_by_age_2024.csv";
        private const string OutputRelative = "Assets/Scripts/Data/Generated/CohortIncomeSeeds.cs";
        public const int IncomeYear = 2023;

        /// <summary>The source band each of the 21 cohorts takes, by the cohort's midpoint; null = no income dimension (below 15).</summary>
        public static readonly string[] EuroBandByCohort =
        {
            null, null, null,                              // 0-4, 5-9, 10-14
            "Y16-24", "Y16-24",                            // 15-19, 20-24
            "Y25-49", "Y25-49", "Y25-49", "Y25-49", "Y25-49",   // 25-29 .. 45-49
            "Y50-64", "Y50-64", "Y50-64",                  // 50-54 .. 60-64
            "Y_GE65", "Y_GE65",                            // 65-69, 70-74 (the 65+ aggregate's shape; no 65-74 band is published)
            "Y_GE75", "Y_GE75", "Y_GE75", "Y_GE75", "Y_GE75", "Y_GE75",   // 75-79 .. 100+
        };
        public static readonly string[] UsaBandByCohort =
        {
            null, null, null,
            "Y15-24", "Y15-24",
            "Y25-29", "Y30-34", "Y35-39", "Y40-44", "Y45-49",
            "Y50-54", "Y55-59", "Y60-64",
            "Y65-69", "Y70-74",
            "Y_GE75", "Y_GE75", "Y_GE75", "Y_GE75", "Y_GE75", "Y_GE75",
        };
        public const string EuroAnchorBand = "Y_GE16";
        public const string UsaAnchorBand = "Y_GE15";

        private static readonly (CountryId Id, string Geo)[] Countries =
        {
            (CountryId.Sweden, "SE"), (CountryId.Germany, "DE"), (CountryId.France, "FR"),
            (CountryId.Italy, "IT"), (CountryId.Poland, "PL"), (CountryId.USA, "US"),
        };

        public sealed class BandFigures
        {
            public double Mean, Median;
            public string Unit, Concept;
        }

        /// <summary>The source file parsed: [geo][age_class] → (mean, median, unit, concept). Public so the diagnostic reads the same parse.</summary>
        public static Dictionary<string, Dictionary<string, BandFigures>> Parse(string csv, List<string> failures)
        {
            var table = new Dictionary<string, Dictionary<string, BandFigures>>(StringComparer.Ordinal);
            string[] lines = csv.Split('\n');
            for (int i = 1; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (line.Length == 0) { continue; }
                string[] p = line.Split(',');
                if (p.Length < 8) { failures.Add($"line {i + 1}: {p.Length} column(s), 8 expected"); continue; }
                if (!table.TryGetValue(p[0], out Dictionary<string, BandFigures> bands)) { bands = new Dictionary<string, BandFigures>(StringComparer.Ordinal); table[p[0]] = bands; }
                var f = new BandFigures { Unit = p[5], Concept = p[6] };
                if (p[3].Length > 0 && !double.TryParse(p[3], NumberStyles.Float, CultureInfo.InvariantCulture, out f.Mean)) { failures.Add($"line {i + 1}: mean '{p[3]}' is not a number"); }
                if (p[4].Length > 0 && !double.TryParse(p[4], NumberStyles.Float, CultureInfo.InvariantCulture, out f.Median)) { failures.Add($"line {i + 1}: median '{p[4]}' is not a number"); }
                bands[p[2]] = f;
            }
            return table;
        }

        /// <summary>σ² = 2 ln(mean ÷ median); NaN when the pair cannot pin a log-normal (mean not above median).</summary>
        public static double SigmaOf(double mean, double median) => mean > median && median > 0 ? Math.Sqrt(2.0 * Math.Log(mean / median)) : double.NaN;

        [MenuItem("PoliSim/Generate Cohort Income Catalog")]
        public static void Generate()
        {
            string root = Directory.GetCurrentDirectory();
            string path = Path.Combine(root, SourceRelative.Replace('/', Path.DirectorySeparatorChar));
            var failures = new List<string>();
            if (!File.Exists(path)) { Debug.LogError($"INCOMECATALOG: {SourceRelative} is not on disk - run Tools/income_prep.pl first."); return; }
            Dictionary<string, Dictionary<string, BandFigures>> table = Parse(File.ReadAllText(path), failures);

            var median = new Dictionary<CountryId, float[]>();
            var sigma = new Dictionary<CountryId, float[]>();
            var bandOf = new Dictionary<CountryId, string[]>();
            var unit = new Dictionary<CountryId, string>();
            var concept = new Dictionary<CountryId, string>();
            var anchorMean = new Dictionary<CountryId, float>();
            var anchorMedian = new Dictionary<CountryId, float>();
            var anchorBand = new Dictionary<CountryId, string>();

            foreach ((CountryId id, string geo) in Countries)
            {
                if (!table.TryGetValue(geo, out Dictionary<string, BandFigures> bands)) { failures.Add($"{id}: no rows for geo {geo}"); continue; }
                string[] map = id == CountryId.USA ? UsaBandByCohort : EuroBandByCohort;
                string anchor = id == CountryId.USA ? UsaAnchorBand : EuroAnchorBand;
                var med = new float[PopulationCohorts.CohortCount];
                var sig = new float[PopulationCohorts.CohortCount];
                string u = null, c = null;
                for (int i = 0; i < PopulationCohorts.CohortCount; i++)
                {
                    if (map[i] == null) { med[i] = 0f; sig[i] = 0f; continue; }
                    if (!bands.TryGetValue(map[i], out BandFigures f) || f.Mean <= 0 || f.Median <= 0)
                    {
                        failures.Add($"{id}: cohort {PopulationCohorts.Label(i)} maps to {map[i]}, which has no mean or median in the source");
                        continue;
                    }
                    double s = SigmaOf(f.Mean, f.Median);
                    if (double.IsNaN(s)) { failures.Add($"{id}: {map[i]} has mean {f.Mean} not above median {f.Median} - no log-normal has that shape"); continue; }
                    med[i] = (float)f.Median; sig[i] = (float)s;
                    u = u ?? f.Unit; c = c ?? f.Concept;
                    if (u != f.Unit) { failures.Add($"{id}: two units in one country's rows ({u}, {f.Unit})"); }
                }
                if (!bands.TryGetValue(anchor, out BandFigures a) || a.Mean <= 0 || a.Median <= 0) { failures.Add($"{id}: the anchor band {anchor} has no mean or median"); continue; }
                median[id] = med; sigma[id] = sig; bandOf[id] = map; unit[id] = u ?? ""; concept[id] = c ?? "";
                anchorMean[id] = (float)a.Mean; anchorMedian[id] = (float)a.Median; anchorBand[id] = anchor;
            }

            if (failures.Count > 0)
            {
                Debug.LogError("INCOMECATALOG: REFUSING TO EMIT. A partial income catalog is worse than none - the gap would land inside a "
                               + "distribution nobody re-reads.\n  " + string.Join("\n  ", failures));
                return;
            }

            string digest = ElectionsDataCatalogGenerator.Sha256Of(File.ReadAllBytes(path));
            string output = Path.Combine(root, OutputRelative);
            File.WriteAllText(output, Emit(median, sigma, bandOf, unit, concept, anchorMean, anchorMedian, anchorBand, digest), new UTF8Encoding(false));
            AssetDatabase.Refresh();
            Debug.Log($"INCOMECATALOG: {median.Count} countries x {PopulationCohorts.CohortCount} cohorts generated into {OutputRelative} from {SourceRelative} ({digest}).");
        }

        private static string F(float v) => v.ToString("0.######", CultureInfo.InvariantCulture) + "f";

        private static string Emit(Dictionary<CountryId, float[]> median, Dictionary<CountryId, float[]> sigma, Dictionary<CountryId, string[]> bandOf,
            Dictionary<CountryId, string> unit, Dictionary<CountryId, string> concept, Dictionary<CountryId, float> anchorMean,
            Dictionary<CountryId, float> anchorMedian, Dictionary<CountryId, string> anchorBand, string digest)
        {
            var sb = new StringBuilder();
            sb.Append("// GENERATED by PoliSim.EditorTools.CohortIncomeCatalogGenerator. DO NOT EDIT BY HAND.\n//\n");
            sb.Append("// F4-1: the cohort substrate's income dimension - one log-normal per five-year cohort per country, DERIVED from the\n");
            sb.Append("// publisher's mean and median per age band (sigma^2 = 2 ln(mean / median), scale = median); no authored constant.\n");
            sb.Append("// EU five: Eurostat ilc_di03 2024 (income year 2023), sex T, EUR - EQUIVALISED NET household income per person.\n");
            sb.Append("// USA:     Census CPS ASEC 2024 PINC-01 (income year 2023), USD - TOTAL MONEY INCOME per person 15+ with income.\n");
            sb.Append("// Two concepts, carried as two; the SHAPE is what is read from each. Below 15: 0 and 0 (no income dimension).\n");
            sb.Append("// A cohort inside a wider source band carries the band's shape (the sub-band assumption, stated in the generator).\n//\n");
            sb.Append("// SHA-256 (").Append(SourceRelative).Append("): ").Append(digest).Append('\n');
            sb.Append("\nusing System.Collections.Generic;\nusing PoliSim.Data;\n\nnamespace PoliSim.Data.Generated\n{\n    public static class CohortIncomeSeeds\n    {\n");
            sb.Append("        public const string SourcePath = \"").Append(SourceRelative).Append("\";\n");
            sb.Append("        public const string SourceDigest = \"").Append(digest).Append("\";\n");
            sb.Append("        public const int IncomeYear = ").Append(IncomeYear.ToString(CultureInfo.InvariantCulture)).Append(";\n\n");

            EmitFloatTable(sb, "Median", "the median income per cohort in the source's unit and year; 0 below 15 (no income dimension)", median);
            EmitFloatTable(sb, "Sigma", "the log-normal's sigma per cohort, sqrt(2 ln(mean / median)); 0 below 15", sigma);

            sb.Append("        /// <summary>The source age band each cohort took (by the cohort's midpoint); null below 15.</summary>\n");
            sb.Append("        public static readonly Dictionary<CountryId, string[]> SourceBand = new Dictionary<CountryId, string[]>\n        {\n");
            foreach (KeyValuePair<CountryId, string[]> e in bandOf)
            {
                sb.Append("            { CountryId.").Append(e.Key).Append(", new string[] { ");
                for (int i = 0; i < e.Value.Length; i++) { if (i > 0) { sb.Append(", "); } sb.Append(e.Value[i] == null ? "null" : "\"" + e.Value[i] + "\""); }
                sb.Append(" } },\n");
            }
            sb.Append("        };\n\n");
            EmitStringTable(sb, "Unit", "the source's currency unit", unit);
            EmitStringTable(sb, "Concept", "the source's income concept, verbatim from the file", concept);
            EmitStringTable(sb, "AnchorBand", "the all-ages-with-income band the diagnostic reconciles the cohort mixture against", anchorBand);
            EmitScalarTable(sb, "AnchorMean", "the anchor band's published mean", anchorMean);
            EmitScalarTable(sb, "AnchorMedian", "the anchor band's published median", anchorMedian);

            sb.Append("        /// <summary>Seeds a country's pyramid with its income dimension - COPIES, never references into the tables. False when the country has none.</summary>\n");
            sb.Append("        public static bool Apply(PopulationCohorts cohorts, CountryId id)\n        {\n");
            sb.Append("            if (cohorts == null || !Median.TryGetValue(id, out float[] median) || !Sigma.TryGetValue(id, out float[] sigma)) { return false; }\n");
            sb.Append("            cohorts.IncomeMedian = (float[])median.Clone();\n            cohorts.IncomeSigma = (float[])sigma.Clone();\n");
            sb.Append("            cohorts.IncomeUnit = Unit[id];\n            cohorts.IncomeConcept = Concept[id];\n            return true;\n        }\n");
            sb.Append("    }\n}\n");
            return sb.ToString();
        }

        private static void EmitFloatTable(StringBuilder sb, string name, string doc, Dictionary<CountryId, float[]> table)
        {
            sb.Append("        /// <summary>").Append(doc).Append("</summary>\n");
            sb.Append("        public static readonly Dictionary<CountryId, float[]> ").Append(name).Append(" = new Dictionary<CountryId, float[]>\n        {\n");
            foreach (KeyValuePair<CountryId, float[]> e in table)
            {
                sb.Append("            { CountryId.").Append(e.Key).Append(", new float[] { ");
                for (int i = 0; i < e.Value.Length; i++) { if (i > 0) { sb.Append(", "); } sb.Append(F(e.Value[i])); }
                sb.Append(" } },\n");
            }
            sb.Append("        };\n\n");
        }

        private static void EmitStringTable(StringBuilder sb, string name, string doc, Dictionary<CountryId, string> table)
        {
            sb.Append("        /// <summary>").Append(doc).Append("</summary>\n");
            sb.Append("        public static readonly Dictionary<CountryId, string> ").Append(name).Append(" = new Dictionary<CountryId, string>\n        {\n");
            foreach (KeyValuePair<CountryId, string> e in table) { sb.Append("            { CountryId.").Append(e.Key).Append(", \"").Append(e.Value.Replace("\"", "\\\"")).Append("\" },\n"); }
            sb.Append("        };\n\n");
        }

        private static void EmitScalarTable(StringBuilder sb, string name, string doc, Dictionary<CountryId, float> table)
        {
            sb.Append("        /// <summary>").Append(doc).Append("</summary>\n");
            sb.Append("        public static readonly Dictionary<CountryId, float> ").Append(name).Append(" = new Dictionary<CountryId, float>\n        {\n");
            foreach (KeyValuePair<CountryId, float> e in table) { sb.Append("            { CountryId.").Append(e.Key).Append(", ").Append(F(e.Value)).Append(" },\n"); }
            sb.Append("        };\n\n");
        }
    }
}
