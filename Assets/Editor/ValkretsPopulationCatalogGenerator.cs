using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// F3 (2026-09-02) — generates `Assets/Scripts/Elections/Generated/SwedishValkretsPopulation2024.cs`
    /// from `ElectionsData/sweden/valkrets_population_by_age_2024.csv` (SCB's 31 December 2024 population
    /// by single year of age for all 290 municipalities, mapped to the 29 Riksdag valkretsar by Vallagen
    /// 4 kap. 2 §'s own municipality lists and summed into the cohort substrate's 21 five-year bands —
    /// `Tools/build_valkrets_population.sh` is that method) and the valkrets names from
    /// `valkrets_municipalities_2024.csv`. Same idiom as `ElectionsDataCatalogGenerator`: the source's
    /// SHA-256 is written into the catalog and `GeneratedCatalogCheck` re-derives it every run.
    ///
    /// ⚠ The valkrets NAMES are emitted in Valmyndigheten's form so the catalog joins
    /// `SwedishValkretsReturns2022` by name: the statute writes "Skåne läns västra valkrets" and
    /// "Stockholms läns valkrets (Stockholms län med undantag av …)", the returns "Skåne läns västra"
    /// and "Stockholms län". The normalisation is three rules and the check asserts every name joins.
    /// </summary>
    public static class ValkretsPopulationCatalogGenerator
    {
        private const string SourceRelative = "ElectionsData/sweden/valkrets_population_by_age_2024.csv";
        private const string NamesRelative = "ElectionsData/sweden/valkrets_municipalities_2024.csv";
        private const string OutputRelative = "Assets/Scripts/Elections/Generated/SwedishValkretsPopulation2024.cs";
        private const int Bands = 21;
        private const int Valkretsar = 29;

        [MenuItem("PoliSim/Generate Valkrets Population Catalog")]
        public static void Run()
        {
            CheckExit.ArmLogFold();
            string root = Directory.GetCurrentDirectory();
            string source = Path.Combine(root, SourceRelative.Replace('/', Path.DirectorySeparatorChar));
            string namesPath = Path.Combine(root, NamesRelative.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(source) || !File.Exists(namesPath))
            {
                Debug.LogError($"CATALOG: {SourceRelative} or {NamesRelative} is not on disk. Nothing generated.");
                CheckExit.Finish(1);
                return;
            }
            string digest = ElectionsDataCatalogGenerator.Sha256Of(File.ReadAllBytes(source));
            string namesDigest = ElectionsDataCatalogGenerator.Sha256Of(File.ReadAllBytes(namesPath));

            var names = new string[Valkretsar];
            foreach (string raw in File.ReadAllLines(namesPath))
            {
                string line = raw.Trim();
                if (line.Length == 0 || line.StartsWith("#", StringComparison.Ordinal) || line.StartsWith("kommun_code", StringComparison.Ordinal)) { continue; }
                string[] cells = line.Split(',');
                if (cells.Length < 4) { continue; }
                int v = int.Parse(cells[2], CultureInfo.InvariantCulture);
                if (v >= 1 && v <= Valkretsar) { names[v - 1] = Normalise(cells[3]); }
            }

            var rows = new long[Valkretsar][];
            var totals = new long[Valkretsar];
            long grand = 0;
            foreach (string raw in File.ReadAllLines(source))
            {
                string line = raw.Trim();
                if (line.Length == 0 || line.StartsWith("#", StringComparison.Ordinal) || line.StartsWith("valkrets_no", StringComparison.Ordinal)) { continue; }
                string[] cells = line.Split(',');
                if (cells.Length != Bands + 2)
                {
                    Debug.LogError($"CATALOG: row '{cells[0]}' has {cells.Length} cells, not {Bands + 2}. The file's shape changed; nothing generated.");
                    CheckExit.Finish(1);
                    return;
                }
                int v = int.Parse(cells[0], CultureInfo.InvariantCulture);
                var bands = new long[Bands];
                long sum = 0;
                for (int b = 0; b < Bands; b++) { bands[b] = long.Parse(cells[1 + b], CultureInfo.InvariantCulture); sum += bands[b]; }
                long total = long.Parse(cells[Bands + 1], CultureInfo.InvariantCulture);
                if (sum != total)
                {
                    Debug.LogError($"CATALOG: valkrets {v}'s bands sum to {sum} against its own total {total}. Nothing generated.");
                    CheckExit.Finish(1);
                    return;
                }
                rows[v - 1] = bands; totals[v - 1] = total; grand += total;
            }
            for (int v = 0; v < Valkretsar; v++)
            {
                if (rows[v] == null || string.IsNullOrEmpty(names[v]))
                {
                    Debug.LogError($"CATALOG: valkrets {v + 1} is missing a row or a name. Nothing generated.");
                    CheckExit.Finish(1);
                    return;
                }
            }

            var sb = new StringBuilder();
            sb.Append("// GENERATED by PoliSim.EditorTools.ValkretsPopulationCatalogGenerator. DO NOT EDIT BY HAND.\n//\n");
            sb.Append("// Source : ").Append(SourceRelative).Append('\n');
            sb.Append("// SHA-256: ").Append(digest).Append('\n');
            sb.Append("// Names  : ").Append(NamesRelative).Append('\n');
            sb.Append("// SHA-256: ").Append(namesDigest).Append("\n//\n");
            sb.Append("// SCB BE0101N1, population at 31 December 2024, both sexes, by Riksdag valkrets (Vallagen 4 kap. 2 §,\n");
            sb.Append("// the statute's own municipality lists) and the cohort substrate's 21 five-year bands (0-4 … 100+).\n");
            sb.Append("// The 29 totals sum to SCB's own national figure for 2024. The digests above are what\n");
            sb.Append("// GeneratedCatalogCheck re-derives from the sources every run.\n");
            sb.Append("\nnamespace PoliSim.Elections.Generated\n{\n");
            sb.Append("    /// <summary>Sweden's population by Riksdag valkrets and five-year age band, 31 December 2024, SOURCED\n");
            sb.Append("    /// from SCB and mapped by Vallagen's own lists. Absolute counts. Generated, never hand-edited.</summary>\n");
            sb.Append("    public static class SwedishValkretsPopulation2024\n    {\n");
            sb.Append("        public const string SourceDigest = \"").Append(digest).Append("\";\n");
            sb.Append("        public const string NamesDigest = \"").Append(namesDigest).Append("\";\n");
            sb.Append("        /// <summary>The 29 valkretsar in the STATUTE's order (Vallagen 4 kap. 2 §, item 1 first), named in Valmyndigheten's form so they join the returns catalog by name.</summary>\n");
            sb.Append("        public static readonly string[] Names =\n        {\n");
            foreach (string n in names) { sb.Append("            \"").Append(n).Append("\",\n"); }
            sb.Append("        };\n\n");
            sb.Append("        /// <summary>[valkrets][band], persons, band 0 = ages 0-4 … band 20 = 100+.</summary>\n");
            sb.Append("        public static readonly long[][] Bands =\n        {\n");
            for (int v = 0; v < Valkretsar; v++)
            {
                sb.Append("            new long[] { ");
                for (int b = 0; b < Bands; b++) { sb.Append(rows[v][b].ToString(CultureInfo.InvariantCulture)); if (b < Bands - 1) { sb.Append(", "); } }
                sb.Append(" },\n");
            }
            sb.Append("        };\n\n");
            sb.Append("        /// <summary>Each valkrets's total, persons - its bands summed; the 29 sum to ").Append(grand.ToString(CultureInfo.InvariantCulture)).Append(".</summary>\n");
            sb.Append("        public static readonly long[] Total = { ");
            for (int v = 0; v < Valkretsar; v++) { sb.Append(totals[v].ToString(CultureInfo.InvariantCulture)); if (v < Valkretsar - 1) { sb.Append(", "); } }
            sb.Append(" };\n");
            sb.Append("    }\n}\n");

            string output = Path.Combine(root, OutputRelative.Replace('/', Path.DirectorySeparatorChar));
            File.WriteAllText(output, sb.ToString(), new UTF8Encoding(false));
            Debug.Log($"CATALOG: {Valkretsar} valkretsar x {Bands} bands generated into {OutputRelative}; national total {grand}; source digest {digest}.");
            AssetDatabase.Refresh();
            CheckExit.Finish(0);
        }

        /// <summary>The statute's name in Valmyndigheten's form: no parenthetical, no trailing " valkrets", and a trailing "läns" becomes "län".</summary>
        public static string Normalise(string statuteName)
        {
            string n = statuteName.Trim();
            int paren = n.IndexOf('(');
            if (paren >= 0) { n = n.Substring(0, paren).Trim(); }
            if (n.EndsWith(" valkrets", StringComparison.Ordinal)) { n = n.Substring(0, n.Length - " valkrets".Length).Trim(); }
            if (n.EndsWith(" läns", StringComparison.Ordinal)) { n = n.Substring(0, n.Length - 1); }
            return n;
        }
    }
}
