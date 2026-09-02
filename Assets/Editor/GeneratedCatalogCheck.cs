using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using PoliSim.Data;
using PoliSim.Elections.Generated;
using PoliSim.Data.Generated;
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

            // ⚠ THE SECOND GENERATED CATALOG (P-I2 stage 3, 2026-09-01). This is an EXTENSION of an
            // existing check rather than a new one — R-N5 governs new checks, and the drift question here
            // is identical to the one above: a generated table whose source has moved underneath it.
            // A second check would have been a second thing to keep true for no added coverage.
            failures += CheckProjections(sb);
            failures += CheckValkretsPopulation(sb);

            sb.Append(failures == 0
                ? "    ✅ every generated catalog is what its source says, and the row counts agree.\n"
                : "    ⚠ SEE THE ERRORS ABOVE.\n");

            if (failures > 0) { Debug.LogError(sb.ToString()); CheckExit.Finish(1); return; }

            Debug.Log(sb.ToString());
            CheckExit.Finish(0);
        }

        /// <summary>
        /// `PopulationProjections` against the six files it was generated from. ⚠ **The enumeration is
        /// the catalog's own `SourcePath` table**, so a country added to the projection catalog is covered
        /// here the day it lands with no edit in this file — the `PartyMarkCoverageCheck` idiom.
        /// </summary>
        /// <summary>
        /// F3 (2026-09-02): `SwedishValkretsPopulation2024` against its two sources - the aggregated
        /// valkrets × band file and the municipality map it was named from - plus the two identities the
        /// catalog exists for: 29 rows whose bands sum to their own totals, and every name joining
        /// `SwedishValkretsReturns2022` (the campaign looks the electorate up by that name).
        /// </summary>
        private static int CheckValkretsPopulation(StringBuilder sb)
        {
            int failures = 0;
            string root = Directory.GetCurrentDirectory();
            var sources = new (string Relative, string Recorded, string What)[]
            {
                ("ElectionsData/sweden/valkrets_population_by_age_2024.csv", SwedishValkretsPopulation2024.SourceDigest, "the valkrets x band file"),
                ("ElectionsData/sweden/valkrets_municipalities_2024.csv", SwedishValkretsPopulation2024.NamesDigest, "the municipality map"),
            };
            foreach ((string relative, string recorded, string what) in sources)
            {
                string path = Path.Combine(root, relative.Replace('/', Path.DirectorySeparatorChar));
                if (!File.Exists(path))
                {
                    failures++;
                    Debug.LogError($"CATALOG: {relative} is not on disk, so the valkrets population catalog cannot be verified against {what}.");
                    continue;
                }
                string onDisk = ElectionsDataCatalogGenerator.Sha256Of(File.ReadAllBytes(path));
                if (!string.Equals(onDisk, recorded, StringComparison.OrdinalIgnoreCase))
                {
                    failures++;
                    Debug.LogError($"CATALOG: {relative} changed since the valkrets population catalog was generated ({what}: on disk {onDisk}, recorded {recorded}). Re-run ValkretsPopulationCatalogGenerator.");
                }
            }
            int rows = SwedishValkretsPopulation2024.Bands.Length;
            long grand = 0;
            var unjoined = new List<string>();
            for (int v = 0; v < rows; v++)
            {
                long sum = 0;
                foreach (long b in SwedishValkretsPopulation2024.Bands[v]) { sum += b; }
                if (sum != SwedishValkretsPopulation2024.Total[v])
                {
                    failures++;
                    Debug.LogError($"CATALOG: valkrets {v + 1} ({SwedishValkretsPopulation2024.Names[v]}) bands sum to {sum} against its total {SwedishValkretsPopulation2024.Total[v]}.");
                }
                grand += SwedishValkretsPopulation2024.Total[v];
                if (Array.IndexOf(SwedishValkretsReturns2022.Names, SwedishValkretsPopulation2024.Names[v]) < 0) { unjoined.Add(SwedishValkretsPopulation2024.Names[v]); }
            }
            if (rows != 29 || unjoined.Count > 0)
            {
                failures++;
                Debug.LogError($"CATALOG: the valkrets population catalog has {rows} rows (29 expected) and {unjoined.Count} name(s) that do not join the returns catalog: {string.Join(", ", unjoined.ToArray())}.");
            }
            sb.Append(string.Format(CultureInfo.InvariantCulture,
                "    valkrets population 2024: {0} rows x {1} bands, national total {2:N0}, {3} name(s) unjoined - {4}\n",
                rows, rows > 0 ? SwedishValkretsPopulation2024.Bands[0].Length : 0, grand, unjoined.Count, failures == 0 ? "ok" : "FAIL"));
            return failures;
        }

        private static int CheckProjections(StringBuilder sb)
        {
            int failures = 0;
            sb.Append("\n=== The projection catalog against its sources ===\n");

            if (PopulationProjections.SourcePath.Count == 0)
            {
                Debug.LogError("CATALOGCHECK: the projection catalog names no sources, so every comparison "
                               + "below is vacuous and this run verified NOTHING about it.");
                return 1;
            }

            foreach (KeyValuePair<CountryId, string> entry in PopulationProjections.SourcePath)
            {
                string path = Path.Combine(Directory.GetCurrentDirectory(),
                    entry.Value.Replace('/', Path.DirectorySeparatorChar));

                if (!File.Exists(path))
                {
                    failures++;
                    Debug.LogError($"CATALOGCHECK: {entry.Value} is not on disk, so {entry.Key}'s projection "
                                   + "digest cannot be compared against anything.");
                    continue;
                }

                string onDisk = ElectionsDataCatalogGenerator.Sha256Of(File.ReadAllBytes(path));
                if (!PopulationProjections.SourceDigest.TryGetValue(entry.Key, out string recorded))
                {
                    failures++;
                    Debug.LogError($"CATALOGCHECK: {entry.Key} has a source path but no recorded digest — "
                                   + "the catalog was emitted by something that did not record what it read.");
                    continue;
                }

                bool ok = string.Equals(onDisk, recorded, StringComparison.OrdinalIgnoreCase);
                sb.Append(F("    {0,-8} {1} {2}\n", entry.Key, ok ? "ok  " : "DRIFT", entry.Value));

                if (ok) { continue; }

                failures++;
                Debug.LogError($"CATALOGCHECK: {entry.Key}'s projection source has changed since the catalog was "
                               + $"generated. On disk {onDisk}, recorded {recorded}. ⚠ Re-run "
                               + "`PoliSim.EditorTools.PopulationProjectionCatalogGenerator.Generate` — and read the "
                               + "diff first, because a publisher revising a projection is an event somebody "
                               + "explains, not a rebuild. ⚠ It also moves a BASELINE family.");
            }

            int years = PopulationProjections.LastYear - PopulationProjections.FirstYear + 1;
            foreach (KeyValuePair<CountryId, float[][]> entry in PopulationProjections.Bands)
            {
                if (entry.Value.Length == years) { continue; }
                failures++;
                Debug.LogError($"CATALOGCHECK: {entry.Key}'s projection holds {entry.Value.Length} year(s) against the "
                               + $"{years} its own FirstYear..LastYear range declares. A digest that matched while the "
                               + "lengths did not would mean the recorded digest is not the digest of what was read.");
                break;
            }

            return failures;
        }

        private static string F(string format, params object[] args)
            => string.Format(CultureInfo.InvariantCulture, format, args);
    }
}
