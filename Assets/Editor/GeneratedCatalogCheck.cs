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
            failures += CheckReturns2026(sb);
            failures += CheckProjections(sb);
            failures += CheckValkretsPopulation(sb);
            failures += CheckItanes(sb);
            failures += CheckEnergy(sb);
            failures += CheckCohortIncome(sb);
            failures += CheckGovernmentConsumption(sb);
            failures += CheckGermanLaender(sb);   // PS-4 (§688): the Länder catalogs
            failures += CheckUsPresidentialReturns(sb);   // PS-6 US-3 (§786): the US presidential returns
            failures += CheckUsHouseDistricts(sb);   // PS-6 US-11 (§790): the House by district, its maps, its record

            sb.Append(failures == 0
                ? "    ✅ every generated catalog is what its source says, and the row counts agree.\n"
                : "    ⚠ SEE THE ERRORS ABOVE.\n");

            if (failures > 0) { Debug.LogError(sb.ToString()); CheckExit.Finish(1); return; }

            Debug.Log(sb.ToString());
            CheckExit.Finish(0);
        }

        /// <summary>
        /// K-1 (2026-09-23): the 2026 returns catalog, the live game's since K-1 - the same digest and length questions as the
        /// 2022 one above, plus the join the live readers depend on: the 29 names in the 2022 catalog's ORDER, because the
        /// cartogram's bands, the 2024 population catalog and every harness index the valkretsar by position.
        /// </summary>
        private static int CheckReturns2026(StringBuilder sb)
        {
            const string sourceRelative = "ElectionsData/sweden/2026/valkrets_votes_2026.csv";
            string source = Path.Combine(Directory.GetCurrentDirectory(), sourceRelative.Replace('/', Path.DirectorySeparatorChar));
            sb.Append("\n=== The 2026 returns catalog against its source ===\n");
            if (!File.Exists(source))
            {
                Debug.LogError("CATALOGCHECK: " + sourceRelative + " is not on disk, so the 2026 catalog's digest cannot be compared against anything.");
                return 1;
            }

            int failures = 0;
            string onDisk = ElectionsDataCatalogGenerator.Sha256Of(File.ReadAllBytes(source));
            if (!string.Equals(onDisk, SwedishValkretsReturns2026.SourceDigest, StringComparison.OrdinalIgnoreCase))
            {
                failures++;
                Debug.LogError("CATALOGCHECK: the 2026 source has changed since its catalog was generated. On disk " + onDisk
                               + ", recorded " + SwedishValkretsReturns2026.SourceDigest + ". Re-run the generator after reading the diff.");
            }

            int rows = 0;
            foreach (string raw in File.ReadAllLines(source))
            {
                string line = raw.Trim();
                if (line.Length == 0 || line.StartsWith("#", StringComparison.Ordinal) || line.StartsWith("valkrets;", StringComparison.Ordinal)) { continue; }
                rows++;
            }
            int[] lengths =
            {
                SwedishValkretsReturns2026.Names.Length, SwedishValkretsReturns2026.Votes.Length, SwedishValkretsReturns2026.Valid.Length,
                SwedishValkretsReturns2026.Eligible.Length, SwedishValkretsReturns2026.Cast.Length,
            };
            foreach (int length in lengths)
            {
                if (length == rows && rows == 29) { continue; }
                failures++;
                Debug.LogError($"CATALOGCHECK: the 2026 source holds {rows} data row(s) and one of its catalog's arrays holds {length}; both must be 29.");
                break;
            }

            var order = new List<string>();
            for (int r = 0; r < SwedishValkretsReturns2026.Names.Length; r++)
            {
                if (r >= SwedishValkretsReturns2022.Names.Length || SwedishValkretsReturns2026.Names[r] != SwedishValkretsReturns2022.Names[r]) { order.Add($"row {r + 1} '{SwedishValkretsReturns2026.Names[r]}'"); }
            }
            bool partiesJoin = string.Join(",", SwedishValkretsReturns2026.Parties) == string.Join(",", SwedishValkretsReturns2022.Parties);
            if (order.Count > 0 || !partiesJoin)
            {
                failures++;
                Debug.LogError("CATALOGCHECK: the 2026 catalog does not keep the 2022 catalog's order - " + (partiesJoin ? "" : "the party columns differ; ")
                               + string.Join(", ", order.ToArray()) + ". Every reader that indexes a valkrets by position would read the wrong one.");
            }
            sb.Append(F("    source {0}: digest {1}, {2} rows, names and party columns in the 2022 order: {3} - {4}\n",
                sourceRelative, onDisk.Substring(0, 12), rows, order.Count == 0 && partiesJoin ? "yes" : "NO", failures == 0 ? "ok" : "FAIL"));
            return failures;
        }

        /// <summary>
        /// E-1 (2026-09-10): `ItanesVoteByAge` against the two cross-tab files it was generated from, plus the identity
        /// the catalog exists for - six bands in §137's order, each band's party columns within its weight sum.
        /// </summary>
        private static int CheckItanes(StringBuilder sb)
        {
            int failures = 0;
            string root = Directory.GetCurrentDirectory();
            var sources = new (string Relative, string Recorded)[]
            {
                ("ElectionsData/italy/itanes_vote_by_age_2013.csv", ItanesVoteByAge.SourceDigest2013),
                ("ElectionsData/italy/itanes_vote_by_age_2018.csv", ItanesVoteByAge.SourceDigest2018),
            };
            foreach ((string relative, string recorded) in sources)
            {
                string path = Path.Combine(root, relative.Replace('/', Path.DirectorySeparatorChar));
                if (!File.Exists(path)) { failures++; Debug.LogError($"CATALOG: {relative} is not on disk, so the ITANES catalog cannot be verified."); continue; }
                string onDisk = ElectionsDataCatalogGenerator.Sha256Of(File.ReadAllBytes(path));
                if (!string.Equals(onDisk, recorded, StringComparison.OrdinalIgnoreCase))
                {
                    failures++;
                    Debug.LogError($"CATALOG: {relative} changed since the ITANES catalog was generated (on disk {onDisk}, recorded {recorded}). Re-run ItanesCatalogGenerator.");
                }
            }

            double[][][] waves = { ItanesVoteByAge.Counts2013, ItanesVoteByAge.Counts2018 };
            double[][] sums = { ItanesVoteByAge.WeightSum2013, ItanesVoteByAge.WeightSum2018 };
            for (int w = 0; w < 2; w++)
            {
                if (waves[w].Length != 6 || sums[w].Length != 6) { failures++; Debug.LogError($"CATALOG: ITANES wave {(w == 0 ? 2013 : 2018)} does not carry six bands."); continue; }
                for (int g = 0; g < 6; g++)
                {
                    double s = 0; foreach (double c in waves[w][g]) { s += c; }
                    if (s > sums[w][g] * 1.0001 || sums[w][g] <= 0) { failures++; Debug.LogError($"CATALOG: ITANES {(w == 0 ? 2013 : 2018)} band {ItanesVoteByAge.Bands[g]}: party columns {s} against weight sum {sums[w][g]}."); }
                }
            }
            sb.Append($"    ItanesVoteByAge: two sources at their recorded digests, 6 bands x {ItanesVoteByAge.Parties.Length} parties per wave, every band's columns within its weight sum ({failures} fault(s)).\n");
            return failures;
        }

        /// <summary>
        /// F4-1 (2026-09-12): `CohortIncomeSeeds` against the one file it was generated from, plus the catalog's own shape - six
        /// countries, 21 entries each, a positive median and sigma on every cohort from 15 up and zeros below, the anchor present.
        /// </summary>
        private static int CheckCohortIncome(StringBuilder sb)
        {
            int failures = 0;
            string path = Path.Combine(Directory.GetCurrentDirectory(), CohortIncomeSeeds.SourcePath.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(path)) { Debug.LogError($"CATALOG: {CohortIncomeSeeds.SourcePath} is not on disk, so the income catalog cannot be verified."); return 1; }
            string onDisk = ElectionsDataCatalogGenerator.Sha256Of(File.ReadAllBytes(path));
            if (!string.Equals(onDisk, CohortIncomeSeeds.SourceDigest, StringComparison.OrdinalIgnoreCase))
            {
                failures++;
                Debug.LogError($"CATALOG: {CohortIncomeSeeds.SourcePath} changed since the income catalog was generated (on disk {onDisk}, recorded {CohortIncomeSeeds.SourceDigest}). Re-run CohortIncomeCatalogGenerator - and read the diff first: a sourced file changing is an event somebody explains.");
            }
            int countries = 0;
            foreach (KeyValuePair<CountryId, float[]> e in CohortIncomeSeeds.Median)
            {
                countries++;
                if (!CohortIncomeSeeds.Sigma.TryGetValue(e.Key, out float[] sigma) || e.Value.Length != PopulationCohorts.CohortCount || sigma.Length != PopulationCohorts.CohortCount)
                {
                    failures++; Debug.LogError($"CATALOG: the income catalog's {e.Key} does not carry {PopulationCohorts.CohortCount} medians and sigmas."); continue;
                }
                for (int i = 0; i < PopulationCohorts.CohortCount; i++)
                {
                    bool below15 = i * PopulationCohorts.CohortWidth < 15;
                    bool ok = below15 ? (e.Value[i] == 0f && sigma[i] == 0f) : (e.Value[i] > 0f && sigma[i] > 0f && !float.IsInfinity(sigma[i]));
                    if (ok) { continue; }
                    failures++;
                    Debug.LogError($"CATALOG: the income catalog's {e.Key} cohort {PopulationCohorts.Label(i)} reads median {e.Value[i]} sigma {sigma[i]} - {(below15 ? "a cohort below 15 must carry no dimension" : "a cohort from 15 up must carry a positive median and sigma")}.");
                }
                if (!CohortIncomeSeeds.AnchorMean.ContainsKey(e.Key) || !CohortIncomeSeeds.Unit.ContainsKey(e.Key)) { failures++; Debug.LogError($"CATALOG: the income catalog's {e.Key} has no anchor or unit."); }
            }
            if (countries != 6) { failures++; Debug.LogError($"CATALOG: the income catalog carries {countries} countries, not six."); }
            sb.Append($"    CohortIncomeSeeds: the source at its recorded digest, {countries} countries × {PopulationCohorts.CohortCount} cohorts, medians and sigmas positive from 15 up and zero below.\n");
            return failures;
        }

        /// <summary>
        /// T-3, form A (2026-09-15): `GovernmentConsumptionData` against the one file it was generated from, plus the table's own shape -
        /// six countries, each share a number strictly between 0 and 100 with its source and flag, the federal-only share on the USA alone and
        /// below its general-government share, and as many data rows in the file as the table holds.
        /// </summary>
        private static int CheckGovernmentConsumption(StringBuilder sb)
        {
            int failures = 0;
            string path = Path.Combine(Directory.GetCurrentDirectory(), GovernmentConsumptionData.SourcePath.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(path)) { Debug.LogError($"CATALOG: {GovernmentConsumptionData.SourcePath} is not on disk, so the government-consumption table cannot be verified."); return 1; }
            string onDisk = ElectionsDataCatalogGenerator.Sha256Of(File.ReadAllBytes(path));
            if (!string.Equals(onDisk, GovernmentConsumptionData.SourceDigest, StringComparison.OrdinalIgnoreCase))
            {
                failures++;
                Debug.LogError($"CATALOG: {GovernmentConsumptionData.SourcePath} changed since the government-consumption table was generated (on disk {onDisk}, recorded {GovernmentConsumptionData.SourceDigest}). Re-run GovernmentConsumptionCatalogGenerator - and read the diff first: a derived data file changing means a source or the prep script changed, which is an event somebody explains.");
            }
            int dataRows = -1;
            foreach (string raw in File.ReadAllLines(path)) { if (raw.Trim().Length > 0) { dataRows++; } }
            if (dataRows != GovernmentConsumptionData.SharePct.Count) { failures++; Debug.LogError($"CATALOG: {GovernmentConsumptionData.SourcePath} holds {dataRows} data row(s) and the government-consumption table holds {GovernmentConsumptionData.SharePct.Count}."); }
            foreach (CountryId id in (CountryId[])Enum.GetValues(typeof(CountryId)))
            {
                if (!GovernmentConsumptionData.SharePct.TryGetValue(id, out float share)) { failures++; Debug.LogError($"CATALOG: the government-consumption table carries no share for {id}."); continue; }
                if (!(share > 0f) || !(share < 100f)) { failures++; Debug.LogError($"CATALOG: the government-consumption table's {id} share reads {share} - not a number strictly between 0 and 100."); }
                if (!GovernmentConsumptionData.Source.ContainsKey(id) || !GovernmentConsumptionData.Flag.ContainsKey(id)) { failures++; Debug.LogError($"CATALOG: the government-consumption table's {id} has no source or flag."); }
                bool federal = GovernmentConsumptionData.FederalSharePct.TryGetValue(id, out float fed);
                if (federal != (id == CountryId.USA)) { failures++; Debug.LogError($"CATALOG: the government-consumption table's {id} {(federal ? "carries" : "lacks")} a federal-only share - the USA alone carries one (its lines are federal, the share general government)."); }
                else if (federal && !(fed > 0f && fed < share)) { failures++; Debug.LogError($"CATALOG: the USA's federal-only share {fed} is not positive and below its general-government share {share}."); }
            }
            sb.Append($"    GovernmentConsumptionData: the source at its recorded digest, {GovernmentConsumptionData.SharePct.Count} shares of GDP ({GovernmentConsumptionData.Year}), the USA's federal-only share beside its general one ({failures} fault(s)).\n");
            return failures;
        }

        /// <summary>
        /// Stage 2 of the energy track (2026-09-10, §457): `EnergyLayerData` against the six files under `EnergyData/` it was generated
        /// from - each at its recorded digest, each with as many data rows as the catalog's arrays hold.
        /// </summary>
        private static int CheckEnergy(StringBuilder sb)
        {
            int failures = 0;
            string root = Directory.GetCurrentDirectory();
            var sources = new (string Relative, string Recorded, int Rows)[]
            {
                (EnergyCatalogGenerator.FleetSource, EnergyLayerData.FleetDigest, EnergyLayerData.Countries.Length * EnergyLayerData.Labels.Length),
                (EnergyCatalogGenerator.BlocksSource, EnergyLayerData.LoadBlocksDigest, EnergyLayerData.Zones.Length),
                (EnergyCatalogGenerator.ZonesSource, EnergyLayerData.ZonesDigest, EnergyLayerData.SwedishZones.Length),
                (EnergyCatalogGenerator.LinksSource, EnergyLayerData.LinksDigest, EnergyLayerData.LinkFrom.Length),
                (EnergyCatalogGenerator.CombustionSource, EnergyLayerData.CombustionDigest, EnergyLayerData.Countries.Length * EnergyLayerData.CombustionLabels.Length * EnergyLayerData.CombustionClasses.Length),
                (EnergyCatalogGenerator.CountrySource, EnergyLayerData.CountryDigest, EnergyLayerData.PopulationM.Length),
                // EN-3 (2026-09-11): the market layer's three
                (EnergyCatalogGenerator.DispatchSource, EnergyLayerData.DispatchDigest, EnergyLayerData.Zones.Length * EnergyLayerData.DispatchBlocks.Length),
                (EnergyCatalogGenerator.CostsSource, EnergyLayerData.CostsDigest, EnergyLayerData.Countries.Length * EnergyLayerData.CostCategories.Length),
                (EnergyCatalogGenerator.ExternalLinksSource, EnergyLayerData.ExternalLinksDigest, EnergyLayerData.ExternalLinkZone.Length),
                // EN-4 (2026-09-11): the retail seed
                (EnergyCatalogGenerator.RetailSource, EnergyLayerData.RetailDigest, EnergyLayerData.Countries.Length * EnergyLayerData.RetailClasses.Length),
                // EN-5 (2026-09-11): the price-index weights
                (EnergyCatalogGenerator.PriceIndexWeightSource, EnergyLayerData.PriceIndexWeightDigest, EnergyLayerData.Countries.Length),
                // EN-3b (2026-09-11): the reservoir dispatch's two files
                (EnergyCatalogGenerator.HydroSource, EnergyLayerData.HydroDigest, EnergyLayerData.HydroZones.Length),
                (EnergyCatalogGenerator.HydroFleetSource, EnergyLayerData.HydroFleetDigest, EnergyLayerData.Countries.Length),
                // EN-7b (2026-09-15): the electricity tax's statute - a row per class for every covered country but the USA (no federal excise)
                (EnergyCatalogGenerator.ElectricityTaxSource, EnergyLayerData.ElectricityTaxDigest, (EnergyLayerData.Countries.Length - 1) * EnergyLayerData.RetailClasses.Length),
            };
            foreach ((string relative, string recorded, int rows) in sources)
            {
                string path = Path.Combine(root, relative.Replace('/', Path.DirectorySeparatorChar));
                if (!File.Exists(path)) { failures++; Debug.LogError($"CATALOG: {relative} is not on disk, so the energy catalog cannot be verified."); continue; }
                string onDisk = ElectionsDataCatalogGenerator.Sha256Of(File.ReadAllBytes(path));
                if (!string.Equals(onDisk, recorded, StringComparison.OrdinalIgnoreCase))
                {
                    failures++;
                    Debug.LogError($"CATALOG: {relative} changed since the energy catalog was generated (on disk {onDisk}, recorded {recorded}). Re-run EnergyCatalogGenerator - and read the diff first: a derived data file changing means a source or the prep script changed, which is an event somebody explains.");
                }
                int dataRows = EnergyCatalogGenerator.DataRows(path);
                if (dataRows != rows) { failures++; Debug.LogError($"CATALOG: {relative} holds {dataRows} data row(s) and the energy catalog's array holds {rows}."); }
            }
            sb.Append($"    EnergyLayerData: nine sources at their recorded digests, {EnergyLayerData.Countries.Length} countries × {EnergyLayerData.Labels.Length} labels, {EnergyLayerData.Zones.Length} load zones × {EnergyLayerData.DispatchBlocks.Length} dispatch blocks, {EnergyLayerData.LinkFrom.Length} links, {EnergyLayerData.ExternalLinkZone.Length} interconnectors, {EnergyLayerData.CombustionClasses.Length} combustion classes, {EnergyLayerData.CostCategories.Length} cost categories ({failures} fault(s)).\n");
            return failures;
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

        /// <summary>
        /// PS-4 (§688): the two Länder catalogs against their sources, through the GENERATOR'S OWN READER (<see cref="GermanLandCatalogGenerator.Read"/>) -
        /// so the check and the generator cannot disagree about what a file says: the digest, the party columns, and every figure, row by row.
        /// </summary>
        private static int CheckGermanLaender(StringBuilder sb)
        {
            sb.Append("\n=== The Länder catalogs against their sources ===\n");
            int failures = 0;
            foreach (GermanLandCatalogGenerator.Vintage v in GermanLandCatalogGenerator.Vintages)
            {
                if (GermanLandCatalogGenerator.Read(v, out List<string> parties, out List<string> names, out List<long[]> rows, out string digest) != 0) { failures++; continue; }
                string recorded = v.Year == 2021 ? GermanLandReturns2021.SourceDigest : GermanLandReturns2025.SourceDigest;
                string[] catParties = v.Year == 2021 ? GermanLandReturns2021.Parties : GermanLandReturns2025.Parties;
                string[] catNames = v.Year == 2021 ? GermanLandReturns2021.Names : GermanLandReturns2025.Names;
                long[] catValid = v.Year == 2021 ? GermanLandReturns2021.Valid : GermanLandReturns2025.Valid;
                long[][] catVotes = v.Year == 2021 ? GermanLandReturns2021.Votes : GermanLandReturns2025.Votes;
                var wrong = new List<string>();
                if (!string.Equals(digest, recorded, StringComparison.OrdinalIgnoreCase)) { wrong.Add("the source changed since generation (on disk " + digest + ", recorded " + recorded + ")"); }
                if (parties.Count != catParties.Length) { wrong.Add("party columns " + parties.Count + " vs " + catParties.Length); }
                else { for (int p = 0; p < parties.Count; p++) { if (parties[p] != catParties[p]) { wrong.Add("column " + p + " is " + parties[p] + " in the source, " + catParties[p] + " in the catalog"); } } }
                if (rows.Count != catNames.Length) { wrong.Add("rows " + rows.Count + " vs " + catNames.Length); }
                for (int r = 0; r < Math.Min(rows.Count, catNames.Length) && wrong.Count < 10; r++)
                {
                    if (names[r] != catNames[r] || rows[r][0] != catValid[r]) { wrong.Add(names[r] + ": name or valid differs"); continue; }
                    for (int p = 0; p < catParties.Length && p + 1 < rows[r].Length; p++) { if (rows[r][p + 1] != catVotes[r][p]) { wrong.Add(names[r] + " " + catParties[p] + ": " + rows[r][p + 1] + " vs " + catVotes[r][p]); } }
                }
                if (wrong.Count > 0) { failures++; Debug.LogError("CATALOGCHECK: " + v.OutputRelative + " - " + string.Join("; ", wrong.ToArray()) + ". Re-run the generator after reading the diff."); }
                sb.Append(wrong.Count == 0
                    ? $"    {v.ClassName}: {rows.Count} Länder × {parties.Count} parties, every figure the source's; digest {digest.Substring(0, 12)}…\n"
                    : $"    ⚠ {v.ClassName}: {wrong.Count} disagreement(s)\n");
            }
            return failures;
        }

        /// <summary>
        /// PS-6 US-3 (§786): the US presidential returns - its CSVs and one catalog (US-5, §788, added 2024's candidates and the House by
        /// state), all written by one run of `Tools/us_returns_prep.pl` from saved pages. The drift questions of the blocks above, asked of each
        /// part: every page the run read is still the bytes it read (re-hashed against <see cref="UsPresidentialReturns.RawSources"/>), and the
        /// READ transcription too; each CSV is still the bytes the catalog was written with, under the heading the comparison reads; every figure
        /// of every CSV row is the catalog's, row for row and in order. ⚠ Plus the one question a catalog read by an allocator owes:
        /// <see cref="PoliSim.Elections.ElectoralCollege.FromCatalog"/> over each year, through the statute's allocator, gives the record's split
        /// exactly - NARA's table, each nominee's electoral votes plus those the nominee's own electors cast for other persons. And what US-5's
        /// instrument leans on: a year's candidates sum to its jurisdictions' votes, each nominee's row to its ticket's; each House year holds the
        /// 50 states once, and no House election is missing between the first and the last.
        /// </summary>
        private static int CheckUsPresidentialReturns(StringBuilder sb)
        {
            sb.Append("\n=== The US presidential returns catalog against its sources ===\n");
            string usa = Path.Combine(Directory.GetCurrentDirectory(), "ElectionsData", "usa");
            var wrong = new List<string>();
            string HashOf(string relative)
            {
                string path = Path.Combine(usa, relative.Replace('/', Path.DirectorySeparatorChar));
                return File.Exists(path) ? ElectionsDataCatalogGenerator.Sha256Of(File.ReadAllBytes(path)) : null;
            }

            foreach ((string path, string sha) in UsPresidentialReturns.RawSources)
            {
                string onDisk = HashOf(path);
                if (onDisk == null) { wrong.Add(path + " is not on disk"); }
                else if (!string.Equals(onDisk, sha, StringComparison.OrdinalIgnoreCase)) { wrong.Add(path + " changed since generation (on disk " + onDisk + ")"); }
            }

            if (UsPresidentialReturns.RawSources.Length == 0) { wrong.Add("the catalog lists no pages, so no page was compared"); }
            if (!string.Equals(HashOf("maine_districts_read_2012_2016.tsv"), UsPresidentialReturns.ReadTranscriptionDigest, StringComparison.OrdinalIgnoreCase))
            {
                wrong.Add("the READ transcription changed since generation");
            }

            List<string[]> years = ReadUsCsv(usa, "president_by_year.csv", UsPresidentialReturns.YearSourceDigest, "year,nominee_d,nominee_r,electors,cast_d,cast_r,others_d_slate,others_r_slate", wrong);
            List<string[]> states = ReadUsCsv(usa, "president_by_state.csv", UsPresidentialReturns.StateSourceDigest, "year,state,electors,votes_d,votes_r,votes_other,votes_total,cast_d,cast_r,cast_other,cast_other_to", wrong);
            List<string[]> districts = ReadUsCsv(usa, "president_by_district.csv", UsPresidentialReturns.DistrictSourceDigest, "year,state,district,votes_d,votes_r,basis", wrong);

            if (years.Count != UsPresidentialReturns.Years.Length) { wrong.Add("years: " + years.Count + " CSV rows, " + UsPresidentialReturns.Years.Length + " in the catalog"); }
            for (int i = 0; i < Math.Min(years.Count, UsPresidentialReturns.Years.Length); i++)
            {
                var y = UsPresidentialReturns.Years[i];
                if (!SameRow(years[i], y.Year, y.NomineeD, y.NomineeR, y.Electors, y.CastD, y.CastR, y.OthersDSlate, y.OthersRSlate)) { wrong.Add("year row " + i + " (" + y.Year + ") differs"); }
            }

            if (states.Count != UsPresidentialReturns.States.Length) { wrong.Add("states: " + states.Count + " CSV rows, " + UsPresidentialReturns.States.Length + " in the catalog"); }
            for (int i = 0; i < Math.Min(states.Count, UsPresidentialReturns.States.Length) && wrong.Count < 12; i++)
            {
                var s = UsPresidentialReturns.States[i];
                if (!SameRow(states[i], s.Year, s.State, s.Electors, s.VotesD, s.VotesR, s.VotesOther, s.VotesTotal, s.CastD, s.CastR, s.CastOther, s.CastOtherTo)) { wrong.Add("state row " + i + " (" + s.Year + " " + s.State + ") differs"); }
            }

            if (districts.Count != UsPresidentialReturns.Districts.Length) { wrong.Add("districts: " + districts.Count + " CSV rows, " + UsPresidentialReturns.Districts.Length + " in the catalog"); }
            for (int i = 0; i < Math.Min(districts.Count, UsPresidentialReturns.Districts.Length); i++)
            {
                var d = UsPresidentialReturns.Districts[i];
                if (!SameRow(districts[i], d.Year, d.State, d.District, d.VotesD, d.VotesR, d.Read ? "READ" : "canvass")) { wrong.Add("district row " + i + " (" + d.Year + " " + d.State + "-" + d.District + ") differs"); }
            }

            // US-5 (§788): 2024's candidates and the House by state - row for row, and the sums a reader of them leans on
            List<string[]> candidates = ReadUsCsv(usa, "president_by_candidate.csv", UsPresidentialReturns.CandidateSourceDigest, "year,candidate,ticket,votes", wrong);
            List<string[]> house = ReadUsCsv(usa, "house_by_state.csv", UsPresidentialReturns.HouseSourceDigest, "year,state,votes_r,votes_d,votes_other,votes_total", wrong);
            if (candidates.Count != UsPresidentialReturns.Candidates.Length) { wrong.Add("candidates: " + candidates.Count + " CSV rows, " + UsPresidentialReturns.Candidates.Length + " in the catalog"); }
            for (int i = 0; i < Math.Min(candidates.Count, UsPresidentialReturns.Candidates.Length); i++)
            {
                var c = UsPresidentialReturns.Candidates[i];
                if (!SameRow(candidates[i], c.Year, c.Candidate, c.Ticket, c.Votes)) { wrong.Add("candidate row " + i + " (" + c.Year + " " + c.Candidate + ") differs"); }
            }

            if (house.Count != UsPresidentialReturns.House.Length) { wrong.Add("House: " + house.Count + " CSV rows, " + UsPresidentialReturns.House.Length + " in the catalog"); }
            for (int i = 0; i < Math.Min(house.Count, UsPresidentialReturns.House.Length) && wrong.Count < 12; i++)
            {
                var h = UsPresidentialReturns.House[i];
                if (!SameRow(house[i], h.Year, h.State, h.VotesR, h.VotesD, h.VotesOther, h.VotesTotal)) { wrong.Add("House row " + i + " (" + h.Year + " " + h.State + ") differs"); }
                // the generator writes the other columns as the total's rest, so this holds on every row it writes - a guard on a hand edit only
                if (h.VotesR + h.VotesD + h.VotesOther != h.VotesTotal) { wrong.Add("House " + h.Year + " " + h.State + ": its parts do not sum to its total"); }
            }

            // each House year the 50 states once - the presidential catalog's jurisdictions less the District - and the years every House election,
            // no gap between the first and the last
            var fifty = new SortedSet<string>(StringComparer.Ordinal);
            foreach (var s in UsPresidentialReturns.States) { if (s.State != "DC") { fifty.Add(s.State); } }
            var houseYears = new SortedDictionary<int, SortedSet<string>>();
            foreach (var h in UsPresidentialReturns.House)
            {
                if (!houseYears.TryGetValue(h.Year, out SortedSet<string> seen)) { houseYears[h.Year] = seen = new SortedSet<string>(StringComparer.Ordinal); }
                if (!seen.Add(h.State)) { wrong.Add("House " + h.Year + ": " + h.State + " twice"); }
            }

            int previous = 0;
            foreach (KeyValuePair<int, SortedSet<string>> kv in houseYears)
            {
                if (fifty.Count != 50 || !kv.Value.SetEquals(fifty)) { wrong.Add("House " + kv.Key + ": " + kv.Value.Count + " states, not the 50 of the presidential catalog"); }
                if (previous != 0 && kv.Key != previous + 2) { wrong.Add("House " + previous + " then " + kv.Key + ": a House election missing between them"); }
                previous = kv.Key;
            }

            if (houseYears.Count == 0) { wrong.Add("the catalog holds no House year"); }

            var candidateYears = new SortedSet<int>();
            foreach (var c in UsPresidentialReturns.Candidates) { candidateYears.Add(c.Year); }
            foreach (int year in candidateYears)
            {
                long field = 0, ticketR = 0, ticketD = 0, total = 0, votesR = 0, votesD = 0;
                int nomineesR = 0, nomineesD = 0;
                foreach (var c in UsPresidentialReturns.Candidates)
                {
                    if (c.Year != year) { continue; }
                    field += c.Votes;
                    if (c.Ticket == "R") { ticketR += c.Votes; nomineesR++; }
                    if (c.Ticket == "D") { ticketD += c.Votes; nomineesD++; }
                }

                foreach (var s in UsPresidentialReturns.States) { if (s.Year == year) { total += s.VotesTotal; votesR += s.VotesR; votesD += s.VotesD; } }
                if (nomineesR != 1 || nomineesD != 1) { wrong.Add(year + ": " + nomineesR + " R and " + nomineesD + " D nominee row(s) among the candidates, not one each"); }
                if (field != total || ticketR != votesR || ticketD != votesD)
                {
                    wrong.Add(F("{0}: the candidates sum to {1} (R {2}, D {3}); the jurisdictions to {4} (R {5}, D {6})", year, field, ticketR, ticketD, total, votesR, votesD));
                }
            }

            var splits = new List<string>();
            foreach (var y in UsPresidentialReturns.Years)
            {
                int[] college;
                try { college = PoliSim.Elections.ElectoralCollege.Allocate(PoliSim.Elections.ElectoralCollege.FromCatalog(y.Year), 2); }
                catch (ArgumentException e) { wrong.Add(y.Year + ": " + e.Message); continue; }
                int r = y.CastR + y.OthersRSlate, d = y.CastD + y.OthersDSlate;
                int gotR = college[PoliSim.Elections.ElectoralCollege.Republican], gotD = college[PoliSim.Elections.ElectoralCollege.Democrat];
                if (gotR != r || gotD != d) { wrong.Add(y.Year + ": the allocator over the catalog gives R " + gotR + " D " + gotD + ", the record R " + r + " D " + d); }
                else { splits.Add(F("{0} R {1} D {2}", y.Year, gotR, gotD)); }
            }

            if (UsPresidentialReturns.Years.Length == 0) { wrong.Add("the catalog holds no year, so the allocator was never run"); }

            if (wrong.Count > 0)
            {
                Debug.LogError("CATALOGCHECK: UsPresidentialReturns - " + string.Join("; ", wrong.ToArray()) + ". Re-run Tools/us_returns_prep.pl after reading the diff.");
                sb.Append(F("    ⚠ UsPresidentialReturns: {0} disagreement(s)\n", wrong.Count));
                return 1;
            }

            sb.Append(F("    UsPresidentialReturns: {0} page(s) and the READ transcription the bytes the run read; {1} year(s), {2} jurisdiction row(s), "
                        + "{3} district row(s), {4} candidate row(s) (their sum the jurisdictions'), {5} House row(s) (50 a year, {6} years), every figure the CSVs'; "
                        + "the allocator over the catalog gives the record's split: {7}\n",
                UsPresidentialReturns.RawSources.Length, years.Count, states.Count, districts.Count, candidates.Count, house.Count, houseYears.Count, string.Join(", ", splits.ToArray())));
            return 0;
        }

        /// <summary>
        /// PS-6 US-11 (§790): the House by district - the catalog's other part (<c>UsHouseDistricts.cs</c>, written by
        /// <c>Tools/us_house_prep.pl</c>), its own pages and three CSVs. Every page <c>HouseRawSources</c> lists is the bytes the run read; each
        /// CSV the bytes the catalog was written with, under the heading the comparison reads, every figure the catalog's, row for row. ⚠ And what
        /// a reader of the districts leans on: the races' years are the record's, and each maps year holds the 50 states once with 435 seats;
        /// each election's races are 435, each state's numbered 1 to its seats that year (0 for one seat at large) by the maps' rows; no race's
        /// parts exceed its total, nor its own lines its party's votes; every winner leads its race's deciding count (the vacant seat's
        /// included), and a race without votes has none; the winners count to the House Historian's division, the seat its footnote leaves
        /// vacant being the one race without a winner or the one winner it does not count; and the districts' own lines and totals sum, state
        /// by state, to the 50 House rows of each election (US-5).
        /// </summary>
        private static int CheckUsHouseDistricts(StringBuilder sb)
        {
            sb.Append("\n=== The US House by district against its sources ===\n");
            string usa = Path.Combine(Directory.GetCurrentDirectory(), "ElectionsData", "usa");
            var wrong = new List<string>();
            foreach ((string path, string sha) in UsPresidentialReturns.HouseRawSources)
            {
                string full = Path.Combine(usa, path.Replace('/', Path.DirectorySeparatorChar));
                string onDisk = File.Exists(full) ? ElectionsDataCatalogGenerator.Sha256Of(File.ReadAllBytes(full)) : null;
                if (onDisk == null) { wrong.Add(path + " is not on disk"); }
                else if (!string.Equals(onDisk, sha, StringComparison.OrdinalIgnoreCase)) { wrong.Add(path + " changed since generation (on disk " + onDisk + ")"); }
            }

            if (UsPresidentialReturns.HouseRawSources.Length == 0) { wrong.Add("the House part lists no pages, so no page was compared"); }

            List<string[]> races = ReadUsCsv(usa, "house_districts.csv", UsPresidentialReturns.HouseDistrictSourceDigest, "year,state,district,votes_r,votes_d,votes_other,votes_total,own_r,own_d,cands_r,cands_d,final_r,final_d,winner,flags", wrong);
            List<string[]> maps = ReadUsCsv(usa, "house_maps.csv", UsPresidentialReturns.HouseMapSourceDigest, "year,state,seats,lines_changed,source", wrong);
            List<string[]> record = ReadUsCsv(usa, "house_years.csv", UsPresidentialReturns.HouseYearSourceDigest, "year,congress,seats_r,seats_d,seats_other,vacant", wrong);
            if (races.Count != UsPresidentialReturns.HouseDistricts.Length) { wrong.Add("races: " + races.Count + " CSV rows, " + UsPresidentialReturns.HouseDistricts.Length + " in the catalog"); }
            for (int i = 0; i < Math.Min(races.Count, UsPresidentialReturns.HouseDistricts.Length) && wrong.Count < 12; i++)
            {
                var r = UsPresidentialReturns.HouseDistricts[i];
                if (!SameRow(races[i], r.Year, r.State, r.District, r.VotesR, r.VotesD, r.VotesOther, r.VotesTotal, r.OwnR, r.OwnD, r.CandsR, r.CandsD, r.FinalR, r.FinalD, r.Winner, r.Flags)) { wrong.Add("race row " + i + " (" + r.Year + " " + r.State + "-" + r.District + ") differs"); }
            }

            if (maps.Count != UsPresidentialReturns.HouseMaps.Length) { wrong.Add("maps: " + maps.Count + " CSV rows, " + UsPresidentialReturns.HouseMaps.Length + " in the catalog"); }
            for (int i = 0; i < Math.Min(maps.Count, UsPresidentialReturns.HouseMaps.Length) && wrong.Count < 12; i++)
            {
                var m = UsPresidentialReturns.HouseMaps[i];
                if (!SameRow(maps[i], m.Year, m.State, m.Seats, m.LinesChanged ? 1 : 0, m.Source)) { wrong.Add("map row " + i + " (" + m.Year + " " + m.State + ") differs"); }
            }

            if (record.Count != UsPresidentialReturns.HouseRecord.Length) { wrong.Add("record: " + record.Count + " CSV rows, " + UsPresidentialReturns.HouseRecord.Length + " in the catalog"); }
            for (int i = 0; i < Math.Min(record.Count, UsPresidentialReturns.HouseRecord.Length); i++)
            {
                var y = UsPresidentialReturns.HouseRecord[i];
                if (!SameRow(record[i], y.Year, y.Congress, y.SeatsR, y.SeatsD, y.SeatsOther, y.Vacant)) { wrong.Add("record row " + i + " (" + y.Year + ") differs"); }
            }

            // the seats each state holds each year, by the maps' rows - each maps year the 50 states once, 435 seats
            var seats = new Dictionary<(int, string), int>();
            var mapYears = new SortedDictionary<int, (int States, int Seats)>();
            foreach (var m in UsPresidentialReturns.HouseMaps)
            {
                if (seats.ContainsKey((m.Year, m.State))) { wrong.Add("maps " + m.Year + ": " + m.State + " twice"); }
                seats[(m.Year, m.State)] = m.Seats;
                mapYears.TryGetValue(m.Year, out (int States, int Seats) t);
                mapYears[m.Year] = (t.States + 1, t.Seats + m.Seats);
            }

            foreach (KeyValuePair<int, (int States, int Seats)> kv in mapYears)
            {
                if (kv.Value.States != 50 || kv.Value.Seats != 435) { wrong.Add(F("maps {0}: {1} states, {2} seats - not 50 and 435", kv.Key, kv.Value.States, kv.Value.Seats)); }
            }

            // the races' years are the record's: no election counted without its division, no division without its races
            var raceYears = new SortedSet<int>();
            var recordYears = new SortedSet<int>();
            foreach (var r in UsPresidentialReturns.HouseDistricts) { raceYears.Add(r.Year); }
            foreach (var y in UsPresidentialReturns.HouseRecord) { recordYears.Add(y.Year); }
            if (!raceYears.SetEquals(recordYears)) { wrong.Add("the races' years " + string.Join(",", raceYears) + ", the record's " + string.Join(",", recordYears)); }

            var lines = new List<string>();
            foreach (var y in UsPresidentialReturns.HouseRecord)
            {
                int count = 0, winnersR = 0, winnersD = 0, winnersO = 0, none = 0;
                string unwon = null, vacantWinner = null;
                var numbers = new Dictionary<string, List<int>>(StringComparer.Ordinal);
                var ownR = new Dictionary<string, long>(StringComparer.Ordinal);
                var ownD = new Dictionary<string, long>(StringComparer.Ordinal);
                var totals = new Dictionary<string, long>(StringComparer.Ordinal);
                foreach (var r in UsPresidentialReturns.HouseDistricts)
                {
                    if (r.Year != y.Year) { continue; }
                    count++;
                    string name = r.State + "-" + (r.District == 0 ? "AL" : r.District.ToString(CultureInfo.InvariantCulture));
                    if (!numbers.TryGetValue(r.State, out List<int> list)) { numbers[r.State] = list = new List<int>(); }
                    list.Add(r.District);
                    ownR[r.State] = (ownR.TryGetValue(r.State, out long a) ? a : 0) + r.OwnR;
                    ownD[r.State] = (ownD.TryGetValue(r.State, out long b) ? b : 0) + r.OwnD;
                    totals[r.State] = (totals.TryGetValue(r.State, out long c) ? c : 0) + r.VotesTotal;
                    if (r.VotesR + r.VotesD + r.VotesOther > r.VotesTotal || r.OwnR > r.VotesR || r.OwnD > r.VotesD) { wrong.Add(r.Year + " " + name + ": its parts exceed its total, or its own lines its party's votes"); }
                    // the winner leads the deciding count (Louisiana's runoff finalists, Alaska 2022's last round); a race printed without votes
                    // (unopposed, or uncertified) carries none, its winner the unopposed name's or none
                    string lead = r.FinalR > r.FinalD ? "R" : r.FinalD > r.FinalR ? "D" : "-";
                    bool unopposed = r.Flags.IndexOf('U') >= 0;
                    if (unopposed ? r.FinalR + r.FinalD != 0 || r.VotesTotal != 0 : lead != r.Winner)
                    {
                        wrong.Add(F("{0} {1}: the deciding count R {2} D {3}, the winner {4}", r.Year, name, r.FinalR, r.FinalD, r.Winner));
                    }

                    if (r.Winner == "R") { winnersR++; } else if (r.Winner == "D") { winnersD++; } else if (r.Winner == "O") { winnersO++; } else { none++; unwon = name; }
                    if (name == y.Vacant) { vacantWinner = r.Winner; }
                }

                if (count != 435) { wrong.Add(y.Year + ": " + count + " races, not 435"); }
                foreach (KeyValuePair<string, List<int>> kv in numbers)
                {
                    int n = seats.TryGetValue((y.Year, kv.Key), out int s) ? s : -1;
                    kv.Value.Sort();
                    var want = new List<int>();
                    if (n == 1) { want.Add(0); } else { for (int k = 1; k <= n; k++) { want.Add(k); } }
                    if (string.Join(",", kv.Value) != string.Join(",", want)) { wrong.Add(y.Year + " " + kv.Key + ": districts " + string.Join(",", kv.Value) + ", the maps give " + n + " seat(s)"); }
                }

                // the division the House Historian prints: the winners, less the seat its footnote leaves vacant where the Clerk names a winner there
                int r2 = winnersR - (vacantWinner == "R" ? 1 : 0), d2 = winnersD - (vacantWinner == "D" ? 1 : 0), o2 = winnersO - (vacantWinner == "O" ? 1 : 0);
                bool vacancyRight = y.Vacant == "-" ? none == 0 : (vacantWinner != null && (none == 0 || (none == 1 && unwon == y.Vacant)));
                if (r2 != y.SeatsR || d2 != y.SeatsD || o2 != y.SeatsOther || !vacancyRight)
                {
                    wrong.Add(F("{0}: the winners R {1} D {2} other {3} (none {4}), the House Historian R {5} D {6} other {7}, vacant {8}", y.Year, winnersR, winnersD, winnersO, none, y.SeatsR, y.SeatsD, y.SeatsOther, y.Vacant));
                }

                int houseRows = 0;
                foreach (var h in UsPresidentialReturns.House)
                {
                    if (h.Year != y.Year) { continue; }
                    houseRows++;
                    long gotR = ownR.TryGetValue(h.State, out long a) ? a : -1, gotD = ownD.TryGetValue(h.State, out long b) ? b : -1, gotT = totals.TryGetValue(h.State, out long c) ? c : -1;
                    if (gotR != h.VotesR || gotD != h.VotesD || gotT != h.VotesTotal) { wrong.Add(F("{0} {1}: the districts' own lines R {2} D {3}, total {4}; the House row R {5} D {6}, total {7}", y.Year, h.State, gotR, gotD, gotT, h.VotesR, h.VotesD, h.VotesTotal)); }
                }

                if (houseRows != 50) { wrong.Add(F("{0}: {1} House rows by state to sum the districts to, not 50", y.Year, houseRows)); }

                lines.Add(F("{0} R {1} D {2}{3}", y.Year, y.SeatsR, y.SeatsD, y.Vacant == "-" ? "" : " (" + y.Vacant + " vacant)"));
            }

            if (UsPresidentialReturns.HouseRecord.Length == 0) { wrong.Add("the House part holds no election, so nothing was counted"); }
            if (wrong.Count > 0)
            {
                Debug.LogError("CATALOGCHECK: UsPresidentialReturns (the House by district) - " + string.Join("; ", wrong.ToArray()) + ". Re-run Tools/us_house_prep.pl after reading the diff.");
                sb.Append(F("    ⚠ UsPresidentialReturns, the House by district: {0} disagreement(s)\n", wrong.Count));
                return 1;
            }

            sb.Append(F("    UsPresidentialReturns, the House by district: {0} page(s) the bytes the run read; {1} race row(s), {2} map row(s), {3} election(s), every figure "
                        + "the CSVs'; each election 435 races, each state numbered to its seats; the winners count to the House Historian's division: {4}; the "
                        + "districts sum to the House rows by state\n",
                UsPresidentialReturns.HouseRawSources.Length, races.Count, maps.Count, record.Count, string.Join(", ", lines.ToArray())));
            return 0;
        }

        /// <summary>A generated US CSV's data rows (comments skipped), its digest held to the catalog's, its heading to <paramref name="heading"/> -
        /// the columns in the order the catalog's tuple and the comparison read them (the House CSV lists R before D, the presidential ones D
        /// before R) - and each row's field count to the heading's.</summary>
        private static List<string[]> ReadUsCsv(string usa, string file, string recorded, string heading, List<string> wrong)
        {
            var rows = new List<string[]>();
            int fields = heading.Split(',').Length;
            string path = Path.Combine(usa, file);
            if (!File.Exists(path)) { wrong.Add(file + " is not on disk"); return rows; }
            byte[] bytes = File.ReadAllBytes(path);
            string digest = ElectionsDataCatalogGenerator.Sha256Of(bytes);
            if (!string.Equals(digest, recorded, StringComparison.OrdinalIgnoreCase)) { wrong.Add(file + " changed since generation (on disk " + digest + ", recorded " + recorded + ")"); }
            bool atHeading = true;
            foreach (string raw in Encoding.ASCII.GetString(bytes).Split('\n'))
            {
                string line = raw.TrimEnd('\r');
                if (line.Length == 0 || line.StartsWith("#", StringComparison.Ordinal)) { continue; }
                if (atHeading)
                {
                    atHeading = false;
                    if (line != heading) { wrong.Add(file + ": its heading '" + line + "', the comparison reads '" + heading + "'"); }
                    continue;
                }
                string[] cells = line.Split(',');
                if (cells.Length != fields) { wrong.Add(file + ": a row of " + cells.Length + " field(s), not " + fields); continue; }
                rows.Add(cells);
            }

            if (rows.Count == 0) { wrong.Add(file + " holds no data rows, so every comparison of it is vacuous"); }
            return rows;
        }

        private static bool SameRow(string[] cells, params object[] values)
        {
            if (cells.Length != values.Length) { return false; }
            for (int i = 0; i < values.Length; i++)
            {
                if (cells[i] != Convert.ToString(values[i], CultureInfo.InvariantCulture)) { return false; }
            }

            return true;
        }
    }
}
