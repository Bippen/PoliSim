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
    /// PS-4 (POLITICAL_SYSTEM_SPEC.md §9, stage 4 - Germany), part one: **THE LÄNDER CATALOG**, generated. Germany's campaign regions are its
    /// sixteen Länder - the unit the Zweitstimme is counted and the Landeslisten are drawn in - and their returns are SOURCED on disk
    /// (`ElectionsData/germany/land_votes_2021.csv` and `land_votes_2025.csv`: Die Bundeswahlleiterin's kerg2.csv, rows Gebietsart=Land,
    /// Stimme=2, every column sum verified against the national figures by the research that fetched them). `ElectionsData/` sits outside
    /// `Assets/`, so a runtime reader needs a generated catalog, the way Sweden's valkretsar have one
    /// (<see cref="ElectionsDataCatalogGenerator"/>, whose discipline this copies and whose shape it cannot share: sixteen rows, not 29; the
    /// parties by the source's own header, not eight fixed; no eligible or cast column in these files).
    ///
    /// <para><b>Asserted before anything is emitted:</b> the header's first two columns and every party column mapped to a roster key
    /// (GRUENE is the roster's `Grune`); sixteen rows, each a Land of the Grundgesetz's sixteen, each named once; every count a non-negative
    /// integer; the itemised parties never above the Land's valid Zweitstimmen (the remainder is the parties the file does not itemise, and it
    /// is reported, never distributed). The structural zeros are real and kept (CSU stands only in Bayern, CDU everywhere else; SSW only in
    /// Schleswig-Holstein; in 2021 the Grüne's Saarland list was rejected) - a zero is a party that did not stand, which is what the region
    /// layer's availability reads.</para>
    /// </summary>
    public static class GermanLandCatalogGenerator
    {
        internal readonly struct Vintage
        {
            public readonly int Year;
            public readonly string SourceRelative;
            public readonly string OutputRelative;
            public readonly string ClassName;
            public Vintage(int year)
            {
                Year = year;
                SourceRelative = "ElectionsData/germany/land_votes_" + year + ".csv";
                OutputRelative = "Assets/Scripts/Elections/Generated/GermanLandReturns" + year + ".cs";
                ClassName = "GermanLandReturns" + year;
            }
        }

        internal static readonly Vintage[] Vintages = { new Vintage(2021), new Vintage(2025) };

        /// <summary>The sixteen Länder (the Grundgesetz's preamble names them; the kerg2 files' own spellings).</summary>
        internal static readonly string[] Laender =
        {
            "Schleswig-Holstein", "Mecklenburg-Vorpommern", "Hamburg", "Niedersachsen", "Bremen", "Brandenburg", "Sachsen-Anhalt", "Berlin",
            "Nordrhein-Westfalen", "Sachsen", "Hessen", "Thüringen", "Rheinland-Pfalz", "Bayern", "Baden-Württemberg", "Saarland",
        };

        /// <summary>The 2021 file writes two Länder in ASCII (the w-btw21_kerg2 file's own spelling); the catalogs carry one spelling, the 2025 file's.
        /// Named, not a general transliteration, so a third spelling fails the assertion below rather than being guessed.</summary>
        private static string CanonicalLand(string name) => name == "Thueringen" ? "Thüringen" : name == "Baden-Wuerttemberg" ? "Baden-Württemberg" : name;

        /// <summary>The source's party column to the roster's key.</summary>
        private static string RosterKey(string column) => column == "GRUENE" ? "Grune" : column;

        private static readonly HashSet<string> RosterKeys = new HashSet<string>(StringComparer.Ordinal) { "CDU", "CSU", "AfD", "SPD", "Grune", "Linke", "SSW", "BSW", "FDP" };

        [MenuItem("PoliSim/Generate German Land Catalog")]
        public static void Run()
        {
            CheckExit.ArmLogFold();
            int failed = 0;
            foreach (Vintage v in Vintages) { failed += Generate(v, emit: true); }
            AssetDatabase.Refresh();
            CheckExit.Finish(failed == 0 ? 0 : 1);
        }

        /// <summary>Reads and asserts one vintage; emits its catalog when <paramref name="emit"/>. Returns the failures. The check calls it with
        /// emit false - one reader, so the check cannot disagree with the generator about what the file says.</summary>
        internal static int Generate(Vintage v, bool emit) => emit ? EmitTo(v) : Read(v, out _, out _, out _, out _);

        internal static int Read(Vintage v, out List<string> parties, out List<string> names, out List<long[]> rows, out string digest)
        {
            parties = new List<string>(); names = new List<string>(); rows = new List<long[]>(); digest = null;
            string source = Path.Combine(Directory.GetCurrentDirectory(), v.SourceRelative.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(source)) { Debug.LogError("GERMANCATALOG: " + v.SourceRelative + " is not on disk; nothing generated."); return 1; }
            digest = ElectionsDataCatalogGenerator.Sha256Of(File.ReadAllBytes(source));
            string[] header = null;
            var broken = new List<string>();
            foreach (string raw in File.ReadAllLines(source, Encoding.UTF8))
            {
                string line = raw.Trim();
                if (line.Length == 0 || line.StartsWith("#", StringComparison.Ordinal)) { continue; }
                string[] cells = line.Split(';');
                if (header == null)
                {
                    header = cells;
                    if (header.Length < 3 || header[0] != "land" || header[1] != "valid") { Debug.LogError("GERMANCATALOG: " + v.SourceRelative + "'s header is not land;valid;<parties>."); return 1; }
                    for (int c = 2; c < header.Length; c++)
                    {
                        string key = RosterKey(header[c].Trim());
                        if (!RosterKeys.Contains(key)) { Debug.LogError("GERMANCATALOG: column '" + header[c] + "' maps to no roster key."); return 1; }
                        parties.Add(key);
                    }
                    continue;
                }
                if (cells.Length != header.Length) { broken.Add(cells[0] + ": " + cells.Length + " cells, not " + header.Length); continue; }
                var values = new long[cells.Length - 1];
                for (int c = 1; c < cells.Length; c++)
                {
                    if (!long.TryParse(cells[c], NumberStyles.None, CultureInfo.InvariantCulture, out values[c - 1])) { broken.Add(cells[0] + ": '" + cells[c] + "' is not a count"); }
                }
                names.Add(CanonicalLand(cells[0].Trim()));
                rows.Add(values);
            }
            if (header == null) { Debug.LogError("GERMANCATALOG: " + v.SourceRelative + " has no header."); return 1; }
            if (rows.Count != Laender.Length) { broken.Add("read " + rows.Count + " Länder, not 16"); }
            foreach (string land in Laender) { if (names.FindAll(n => n == land).Count != 1) { broken.Add(land + " is not named exactly once"); } }
            for (int r = 0; r < Math.Min(names.Count, Laender.Length); r++) { if (names[r] != Laender[r]) { broken.Add("row " + r + " is " + names[r] + ", not " + Laender[r] + " - both vintages keep one order, so a region index is one Land"); break; } }
            long validTotal = 0, itemised = 0;
            for (int r = 0; r < rows.Count; r++)
            {
                long sum = 0;
                for (int p = 1; p < rows[r].Length; p++) { sum += rows[r][p]; }
                if (sum > rows[r][0]) { broken.Add(names[r] + ": the itemised parties (" + sum + ") exceed valid (" + rows[r][0] + ")"); }
                validTotal += rows[r][0];
                itemised += sum;
            }
            if (broken.Count > 0) { Debug.LogError("GERMANCATALOG: " + v.SourceRelative + " - " + string.Join("; ", broken.ToArray()) + ". Nothing generated."); return 1; }
            Debug.Log($"GERMANCATALOG: {v.SourceRelative}: 16 Länder, {parties.Count} parties itemised = {100.0 * itemised / Math.Max(1, validTotal):F2} % of {validTotal:N0} valid Zweitstimmen; the rest is the parties the file does not itemise, not distributed.");
            return 0;
        }

        private static int EmitTo(Vintage v)
        {
            if (Read(v, out List<string> parties, out List<string> names, out List<long[]> rows, out string digest) != 0) { return 1; }
            var sb = new StringBuilder();
            sb.Append("// GENERATED by PoliSim.EditorTools.GermanLandCatalogGenerator. DO NOT EDIT BY HAND.\n//\n");
            sb.Append("// Source : ").Append(v.SourceRelative).Append('\n');
            sb.Append("// SHA-256: ").Append(digest).Append("\n//\n");
            sb.Append("// ⚠ GeneratedCatalogCheck re-reads the source through the generator's own reader and compares every figure.\n\n");
            sb.Append("namespace PoliSim.Elections.Generated\n{\n");
            sb.Append("    /// <summary>Germany's ").Append(v.Year).Append(" Bundestag election, per Land, Zweitstimmen - SOURCED from Die Bundeswahlleiterin's kerg2.csv.\n");
            sb.Append("    /// Absolute counts. Generated, never hand-edited.</summary>\n");
            sb.Append("    public static class ").Append(v.ClassName).Append("\n    {\n");
            sb.Append("        public const string SourceDigest = \"").Append(digest).Append("\";\n\n");
            sb.Append("        /// <summary>The party columns (roster keys), in the order every row uses.</summary>\n");
            sb.Append("        public static readonly string[] Parties = { ");
            for (int p = 0; p < parties.Count; p++) { sb.Append(p > 0 ? ", " : string.Empty).Append('"').Append(parties[p]).Append('"'); }
            sb.Append(" };\n\n        /// <summary>The sixteen Länder, in the source's order.</summary>\n        public static readonly string[] Names =\n        {\n");
            foreach (string n in names) { sb.Append("            \"").Append(n).Append("\",\n"); }
            sb.Append("        };\n\n        /// <summary>Each Land's valid Zweitstimmen - the weight the region layer aggregates by.</summary>\n        public static readonly long[] Valid =\n        {\n");
            foreach (long[] r in rows) { sb.Append("            ").Append(r[0].ToString(CultureInfo.InvariantCulture)).Append(",\n"); }
            sb.Append("        };\n\n        /// <summary>Each Land's Zweitstimmen per party, in <see cref=\"Parties\"/>' order.</summary>\n        public static readonly long[][] Votes =\n        {\n");
            foreach (long[] r in rows)
            {
                sb.Append("            new long[] { ");
                for (int p = 1; p < r.Length; p++) { sb.Append(p > 1 ? ", " : string.Empty).Append(r[p].ToString(CultureInfo.InvariantCulture)); }
                sb.Append(" },\n");
            }
            sb.Append("        };\n    }\n}\n");
            string output = Path.Combine(Directory.GetCurrentDirectory(), v.OutputRelative.Replace('/', Path.DirectorySeparatorChar));
            File.WriteAllText(output, sb.ToString(), new UTF8Encoding(false));
            Debug.Log("GERMANCATALOG: " + v.OutputRelative + " written; digest " + digest + ".");
            return 0;
        }
    }
}
