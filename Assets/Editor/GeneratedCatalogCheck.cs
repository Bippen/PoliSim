using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using PoliSim.Elections.Generated;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// **The one failure mode a generated catalog has: the source moved and the catalog did not.**
    ///
    /// <para>`ElectionsData/` sits outside `Assets/`, so runtime code cannot read it — the root under
    /// S-33. The answer chosen at the fork (see <see cref="ElectionsDataCatalogGenerator"/>) is to
    /// generate C# from it, which removes runtime parsing, the second data format and the platform
    /// question, and leaves exactly one risk: **drift.** This check is that risk's guard.</para>
    ///
    /// <para><b>What it asserts.</b> The generated file records the SHA-256 of the source it was
    /// generated from. This re-hashes the source **on disk, now**, and requires the two to agree. ⚠ It
    /// also re-reads the source's row count and the catalog's array lengths, because a digest match with
    /// mismatched lengths would mean the recorded digest is not the digest of what was actually read.</para>
    ///
    /// <para>⚠ <b>Why a digest and not a re-parse.</b> Re-parsing the CSV here and comparing values would
    /// be a SECOND implementation of the generator — a second thing to keep true, and the first place a
    /// disagreement between the two would be resolved by whichever was edited last. **The digest compares
    /// the input, not the interpretation**, which is the only comparison that cannot itself drift.</para>
    /// </summary>
    public static class GeneratedCatalogCheck
    {
        private const string SourceRelative = "ElectionsData/sweden/valkrets_votes_2022.csv";

        public static void Run()
        {
            CheckExit.ArmLogFold();

            string source = Path.Combine(Directory.GetCurrentDirectory(),
                SourceRelative.Replace('/', Path.DirectorySeparatorChar));

            if (!File.Exists(source))
            {
                Debug.LogError("CATALOGCHECK: " + SourceRelative + " is not on disk, so the catalog's digest cannot be "
                               + "compared against anything and this run verified NOTHING.");
                CheckExit.Finish(1);
                return;
            }

            string onDisk = ElectionsDataCatalogGenerator.Sha256Of(File.ReadAllBytes(source));
            string recorded = SwedishValkretsReturns2022.SourceDigest;

            int rows = 0;
            foreach (string raw in File.ReadAllLines(source))
            {
                string line = raw.Trim();
                if (line.Length == 0 || line.StartsWith("#", StringComparison.Ordinal)) { continue; }
                if (line.StartsWith("valkrets;", StringComparison.Ordinal)) { continue; }
                rows++;
            }

            var sb = new StringBuilder();
            sb.Append("=== The generated catalog against its source ===\n");
            sb.Append(F("    source   : {0}\n", SourceRelative));
            sb.Append(F("    on disk  : {0}\n", onDisk));
            sb.Append(F("    recorded : {0}\n", recorded));
            sb.Append(F("    rows     : {0} in the source; catalog holds {1} name(s), {2} vote row(s), "
                        + "{3} valid, {4} eligible, {5} cast\n",
                rows, SwedishValkretsReturns2022.Names.Length, SwedishValkretsReturns2022.Votes.Length,
                SwedishValkretsReturns2022.Valid.Length, SwedishValkretsReturns2022.Eligible.Length,
                SwedishValkretsReturns2022.Cast.Length));

            int failures = 0;

            if (!string.Equals(onDisk, recorded, StringComparison.OrdinalIgnoreCase))
            {
                failures++;
                Debug.LogError("CATALOGCHECK: the source has changed since the catalog was generated. On disk "
                               + onDisk + ", recorded " + recorded + ". ⚠ Re-run "
                               + "`PoliSim.EditorTools.ElectionsDataCatalogGenerator.Run` - and read the diff first, "
                               + "because a sourced data file changing is an event somebody explains, not a rebuild.");
            }

            // ⚠ A digest match with mismatched lengths would mean the recorded digest is not the digest of
            // what was actually read - the one way a drift check can pass while being wrong.
            int[] lengths =
            {
                SwedishValkretsReturns2022.Names.Length, SwedishValkretsReturns2022.Votes.Length,
                SwedishValkretsReturns2022.Valid.Length, SwedishValkretsReturns2022.Eligible.Length,
                SwedishValkretsReturns2022.Cast.Length,
            };

            foreach (int length in lengths)
            {
                if (length == rows) { continue; }
                failures++;
                Debug.LogError($"CATALOGCHECK: the source holds {rows} data row(s) and one of the catalog's arrays "
                               + $"holds {length}. A digest that matched while the lengths did not would mean the "
                               + "recorded digest is not the digest of what was read.");
                break;
            }

            // The enumeration rule: a source with no data rows would make every length comparison vacuous.
            if (rows == 0)
            {
                failures++;
                Debug.LogError("CATALOGCHECK: the source holds no data rows, so every comparison above is vacuous and "
                               + "this run verified NOTHING.");
            }

            sb.Append(failures == 0
                ? "    ✅ the catalog is what the source says, and both hold the same number of rows.\n"
                : "    ⚠ SEE THE ERRORS ABOVE.\n");

            if (failures > 0) { Debug.LogError(sb.ToString()); CheckExit.Finish(1); return; }

            Debug.Log(sb.ToString());
            CheckExit.Finish(0);
        }

        private static string F(string format, params object[] args)
            => string.Format(CultureInfo.InvariantCulture, format, args);
    }
}
