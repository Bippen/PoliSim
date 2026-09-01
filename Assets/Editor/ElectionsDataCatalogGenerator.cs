using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// **S-33's root, addressed: the sourced elections data made readable from RUNTIME code.**
    ///
    /// <para><b>The fact underneath three unreachable subsystems.</b> `ElectionsData/` sits at the repo
    /// ROOT, outside `Assets/`. Only editor-side code can read it, and a built player would not have it at
    /// all. That single fact is why board 1h has no per-constituency count, why `RegionalVoteModel` has no
    /// per-region input, and why no amount of wiring above them helps.</para>
    ///
    /// <para><b>THE FORK, MEASURED BEFORE IT WAS CHOSEN.</b> `ElectionsData/` is **197 KB across 24
    /// files, of which the CSVs are 29 KB.** At that size the options separate cleanly:</para>
    /// <list type="bullet">
    /// <item><b>`Resources/`</b> — works, but adds a runtime CSV parser and a SECOND COPY of the data
    /// that can drift from the first with nothing watching.</item>
    /// <item><b>`StreamingAssets/`</b> — loose files in the build; platform quirks (Android and WebGL need
    /// `UnityWebRequest`) bought for no benefit at 29 KB.</item>
    /// <item><b>A GENERATED CATALOG</b> — an editor step turns the CSV into C# constants. No runtime
    /// parsing, no second format, no platform question. ⚠ **And its one risk, drift, is exactly the risk
    /// this project already knows how to kill**: the generated file records the SHA-256 of the source it
    /// was generated from, and <see cref="GeneratedCatalogCheck"/> re-hashes the source every run.</item>
    /// <item><b>Hand transcription</b> — the same result as generation, done by a person, unrepeatably.
    /// ⚠ It is also the project's existing pattern (`PartySystem`'s 53 parties, `DeclaredRedLines`), and
    /// **the generator is that pattern made mechanical and checkable rather than a departure from it.**</item>
    /// </list>
    ///
    /// <para><b>CHOSEN: the generated catalog</b>, and the fork is logged here rather than in a commit
    /// message so the next person meets the reasoning where the code is.</para>
    ///
    /// <para>⚠ <b>THIS GENERATOR IS NOT PART OF THE BUILD.</b> It is run by hand when a source file
    /// changes, exactly like the seed transcriptions before it. Making it an automatic import step would
    /// hide a data change inside a compile, and this project's whole discipline is that a data change is
    /// an event somebody explains.</para>
    ///
    /// <para>⚠ <b>AND THE CATALOG EMITS TO `Assets/Editor/Generated/` FOR NOW, NOT `Assets/Scripts/`.
    /// That is the repo's own guard talking, and it is right.</b> Emitted into the runtime assembly with
    /// no consumer, the file is a delivered artifact nothing reads — `UnwiredSubsystemCheck`'s UNREACHABLE
    /// class caught it on the first run, and a ceiling may not be raised to admit it. **A data layer that
    /// lands before anything consumes it is queued art in another costume.** The mechanism is chosen,
    /// built and proven here — header assertion, definitional reconciliation, digest check, both
    /// directions — and the ONE remaining step, moving the emitted file into `Assets/Scripts`, belongs to
    /// the item that wires `RegionalVoteModel`, because that is when the runtime-readability claim is
    /// exercised rather than asserted.</para>
    /// </summary>
    public static class ElectionsDataCatalogGenerator
    {
        private const string SourceRelative = "ElectionsData/sweden/valkrets_votes_2022.csv";
        private const string OutputRelative = "Assets/Editor/Generated/SwedishValkretsReturns2022.cs";

        /// <summary>The party columns, in the CSV's own order — read from its header rather than assumed,
        /// and asserted below so a re-ordered file cannot silently re-label every column.</summary>
        private static readonly string[] ExpectedHeader =
        {
            "valkrets", "valid", "S", "M", "SD", "C", "V", "KD", "L", "MP", "eligible", "cast",
        };

        [MenuItem("PoliSim/Generate Elections Data Catalog")]
        public static void Run()
        {
            CheckExit.ArmLogFold();

            string root = Directory.GetCurrentDirectory();
            string source = Path.Combine(root, SourceRelative.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(source))
            {
                Debug.LogError("CATALOG: " + SourceRelative + " is not on disk. Nothing was generated, and an empty "
                               + "catalog would have compiled and said nothing.");
                CheckExit.Finish(1);
                return;
            }

            string[] lines = File.ReadAllLines(source);
            string digest = Sha256Of(File.ReadAllBytes(source));

            var names = new List<string>();
            var rows = new List<long[]>();
            string[] header = null;

            foreach (string raw in lines)
            {
                string line = raw.Trim();
                if (line.Length == 0 || line.StartsWith("#", StringComparison.Ordinal)) { continue; }

                string[] cells = line.Split(';');
                if (header == null)
                {
                    header = cells;
                    continue;
                }

                if (cells.Length != ExpectedHeader.Length)
                {
                    Debug.LogError($"CATALOG: row '{cells[0]}' has {cells.Length} cells, not {ExpectedHeader.Length}. "
                                   + "The file's shape changed and generating from it would produce a catalog whose "
                                   + "columns mean something else.");
                    CheckExit.Finish(1);
                    return;
                }

                names.Add(cells[0]);
                var values = new long[ExpectedHeader.Length - 1];
                for (int c = 1; c < cells.Length; c++)
                {
                    values[c - 1] = long.Parse(cells[c], CultureInfo.InvariantCulture);
                }

                rows.Add(values);
            }

            // ⚠ THE HEADER IS ASSERTED, NOT ASSUMED. A re-ordered source would otherwise generate a
            // catalog in which every party's votes belong to a different party, and it would compile.
            if (header == null || header.Length != ExpectedHeader.Length)
            {
                Debug.LogError("CATALOG: no header row was found, so the columns cannot be identified.");
                CheckExit.Finish(1);
                return;
            }

            for (int i = 0; i < ExpectedHeader.Length; i++)
            {
                if (!string.Equals(header[i].Trim(), ExpectedHeader[i], StringComparison.Ordinal))
                {
                    Debug.LogError($"CATALOG: column {i} is '{header[i].Trim()}', expected '{ExpectedHeader[i]}'. "
                                   + "The source's column order changed; generating would relabel every figure.");
                    CheckExit.Finish(1);
                    return;
                }
            }

            // The enumeration rule, and the statute's own number: 29 valkretsar.
            if (rows.Count != 29)
            {
                Debug.LogError($"CATALOG: read {rows.Count} valkretsar, not 29. Either the file is a different "
                               + "country's or it is truncated - and a catalog generated from it would be neither.");
                CheckExit.Finish(1);
                return;
            }

            // ⚠ RECONCILED AT GENERATION, so a bad row cannot reach the catalog — and the FIRST version of
            // this assertion was WRONG, which is worth leaving written down. It required the eight party
            // columns to sum EXACTLY to `valid`, and all 29 rows failed by 1.3–3 %. The file is right and
            // the assertion was: **`valid` counts every valid ballot, including the småpartier below the
            // 4 % threshold, which this file does not itemise.** The generator refused to emit and said
            // why, which is the discipline working rather than the data being bad.
            //
            // What is asserted instead needs no invented tolerance, because it holds by DEFINITION:
            // sum(the eight) <= valid <= cast <= eligible. The remainder is reported per row so the
            // small-party share is visible rather than absorbed.
            var broken = new List<string>();
            long remainderTotal = 0, validTotal = 0;
            for (int r = 0; r < rows.Count; r++)
            {
                long sum = 0;
                for (int p = 1; p <= 8; p++) { sum += rows[r][p]; }

                long valid = rows[r][0], eligible = rows[r][9], cast = rows[r][10];
                remainderTotal += valid - sum;
                validTotal += valid;

                if (sum > valid) { broken.Add($"{names[r]}: the eight parties ({sum}) exceed valid ({valid})"); }
                if (valid > cast) { broken.Add($"{names[r]}: valid ({valid}) exceeds cast ({cast})"); }
                if (cast > eligible) { broken.Add($"{names[r]}: cast ({cast}) exceeds eligible ({eligible})"); }
            }

            if (broken.Count > 0)
            {
                Debug.LogError("CATALOG: " + broken.Count + " row(s) break an identity that holds by definition - "
                               + string.Join("; ", broken.ToArray()) + ". Nothing generated.");
                CheckExit.Finish(1);
                return;
            }

            Debug.Log($"CATALOG: all 29 rows satisfy parties <= valid <= cast <= eligible. The eight itemised parties "
                      + $"account for {100.0 * (validTotal - remainderTotal) / validTotal:F2} % of valid votes; the "
                      + $"remaining {100.0 * remainderTotal / validTotal:F2} % is the småpartier the source does not "
                      + "itemise, and it is NOT distributed anywhere.");

            string output = Path.Combine(root, OutputRelative.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            File.WriteAllText(output, Emit(names, rows, digest), new UTF8Encoding(false));

            Debug.Log($"CATALOG: {rows.Count} valkretsar generated into {OutputRelative}; source digest {digest}. "
                      + "All 29 rows satisfy parties <= valid <= cast <= eligible.");
            AssetDatabase.Refresh();
            CheckExit.Finish(0);
        }

        private static string Emit(List<string> names, List<long[]> rows, string digest)
        {
            var sb = new StringBuilder();
            sb.Append("// GENERATED by PoliSim.EditorTools.ElectionsDataCatalogGenerator. DO NOT EDIT BY HAND.\n");
            sb.Append("//\n");
            sb.Append("// Source : ").Append(SourceRelative).Append('\n');
            sb.Append("// SHA-256: ").Append(digest).Append('\n');
            sb.Append("//\n");
            sb.Append("// ⚠ The digest above is what `GeneratedCatalogCheck` re-derives from the source every run.\n");
            sb.Append("// If they disagree the source changed and this file did not - which is the one failure mode a\n");
            sb.Append("// generated catalog has, and the reason it is generated rather than transcribed by hand.\n");
            sb.Append("//\n");
            sb.Append("// WHY THIS EXISTS: `ElectionsData/` sits outside `Assets/`, so runtime code cannot read it and a\n");
            sb.Append("// built player would not have it. That is the root under S-33: board 1h, the regional vote model\n");
            sb.Append("// and the tactical layer are all unreachable because their data is.\n");
            sb.Append("//\n");
            sb.Append("// The type names are deliberately NOT written above. A prose mention in a file under Assets used\n");
            sb.Append("// to count as a reference and made two subsystems stop being reported as unreachable - this very\n");
            sb.Append("// header did it on 2026-09-01. UnwiredSubsystemCheck strips comments now, so the mention would be\n");
            sb.Append("// harmless; the names stay out anyway, because a guard should not have to be right twice.\n");
            sb.Append("\nnamespace PoliSim.Elections.Generated\n{\n");
            sb.Append("    /// <summary>Sweden's 2022 Riksdag election, per valkrets, SOURCED from Valmyndigheten's own\n");
            sb.Append("    /// machine-readable results. Absolute counts. Generated, never hand-edited.</summary>\n");
            sb.Append("    public static class SwedishValkretsReturns2022\n    {\n");
            sb.Append("        /// <summary>The source file's SHA-256 at generation time.</summary>\n");
            sb.Append("        public const string SourceDigest = \"").Append(digest).Append("\";\n\n");
            sb.Append("        /// <summary>The party columns, in the order every row below uses.</summary>\n");
            sb.Append("        public static readonly string[] Parties = { \"S\", \"M\", \"SD\", \"C\", \"V\", \"KD\", \"L\", \"MP\" };\n\n");
            sb.Append("        /// <summary>The 29 valkretsar, in the source file's own order — Valmyndigheten's numbering 01–29.</summary>\n");
            sb.Append("        public static readonly string[] Names =\n        {\n");
            foreach (string n in names) { sb.Append("            \"").Append(n).Append("\",\n"); }
            sb.Append("        };\n\n");

            Column(sb, "Valid", names, rows, 0, "Valid votes cast in the valkrets.");
            Column(sb, "Eligible", names, rows, 9, "Rostberattigade — the sourced electorate, not a derived one.");
            Column(sb, "Cast", names, rows, 10, "Ballots cast, valid and invalid together.");

            sb.Append("        /// <summary>Votes per party per valkrets, indexed [valkrets][party] against <see cref=\"Parties\"/>.\n");
            sb.Append("        /// ⚠ Every row reconciles against its own <see cref=\"Valid\"/> total at generation.</summary>\n");
            sb.Append("        public static readonly long[][] Votes =\n        {\n");
            for (int r = 0; r < rows.Count; r++)
            {
                sb.Append("            new long[] { ");
                for (int p = 1; p <= 8; p++)
                {
                    sb.Append(rows[r][p].ToString(CultureInfo.InvariantCulture));
                    if (p < 8) { sb.Append(", "); }
                }

                sb.Append(" },   // ").Append(names[r]).Append('\n');
            }

            sb.Append("        };\n    }\n}\n");
            return sb.ToString();
        }

        private static void Column(StringBuilder sb, string name, List<string> names, List<long[]> rows, int index, string doc)
        {
            sb.Append("        /// <summary>").Append(doc).Append("</summary>\n");
            sb.Append("        public static readonly long[] ").Append(name).Append(" =\n        {\n");
            for (int r = 0; r < rows.Count; r++)
            {
                sb.Append("            ").Append(rows[r][index].ToString(CultureInfo.InvariantCulture))
                  .Append(",   // ").Append(names[r]).Append('\n');
            }

            sb.Append("        };\n\n");
        }

        internal static string Sha256Of(byte[] bytes)
        {
            using (var sha = SHA256.Create())
            {
                byte[] hash = sha.ComputeHash(bytes);
                var sb = new StringBuilder(hash.Length * 2);
                foreach (byte b in hash) { sb.Append(b.ToString("x2", CultureInfo.InvariantCulture)); }
                return sb.ToString();
            }
        }
    }
}
