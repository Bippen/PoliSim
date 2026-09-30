using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using PoliSim.Data;
using PoliSim.Elections;
using PoliSim.Elections.Generated;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// PS-4, part one (§688): **GERMANY'S SIXTEEN LÄNDER, READ AS THE ELECTION'S REGIONS.** Asserted against the sources on disk, not against
    /// the catalogs themselves: (a) the 2025 catalog's sixteen Länder sum, party by party, to the Bundeswahlleiterin's exact national
    /// Zweitstimmen (`ElectionsData/germany/national_counts_2025.csv`, the Bund rows of the same kerg2.csv) and to its valid total; (b) the
    /// candidacy facts the returns carry - the CSU stands only in Bayern and the CDU in the other fifteen, the SSW only in Schleswig-Holstein,
    /// the Grüne nowhere in Saarland in 2021, the BSW (no 2021 column) standing everywhere in 2021 with no record of its own; (c) the seated
    /// vintage by date - the snap start of 6 November 2024 reads 2021's Länder, 25 March 2025 on reads 2025's; (d) the breakdown the national
    /// election derives for Germany (`NationalElection.TryPredictShares` with a date) is sixteen Länder whose vote-weighted total reproduces the
    /// national result, and the national result is the same with the date and without it - the regional layer is a readout, never an input; a party standing in one Land (the CSU) reproduces its national share too, its swing spread over its Land alone and scaled (§689).
    /// </summary>
    public static class GermanRegionsDiagnostic
    {
        private static string F(string f, params object[] a) => string.Format(CultureInfo.InvariantCulture, f, a);

        public static void Run()
        {
            CheckExit.ArmLogFold();
            var sb = new StringBuilder("=== GermanRegionsDiagnostic (PS-4, §688): the sixteen Länder as Germany's regions ===\n");
            int failures = 0;
            void Check(bool ok, string what) { if (!ok) { failures++; } sb.Append(ok ? "    ok        " : "    FAIL      ").Append(what).Append('\n'); }
            try
            {
                // (a) the 2025 columns against the national Bund rows
                string national = Path.Combine(Directory.GetCurrentDirectory(), "ElectionsData", "germany", "national_counts_2025.csv");
                var bund = new Dictionary<string, long>(StringComparer.Ordinal);
                foreach (string raw in File.ReadAllLines(national, Encoding.UTF8))
                {
                    string line = raw.Trim();
                    if (line.Length == 0 || line.StartsWith("#", StringComparison.Ordinal) || line.StartsWith("party;", StringComparison.Ordinal)) { continue; }
                    string[] c = line.Split(';');
                    string key = c[0] == "GRUENE" ? "Grune" : c[0] == "Die Linke" ? "Linke" : c[0];
                    bund[key] = long.Parse(c[1], CultureInfo.InvariantCulture);
                }
                Check(GermanLandReturns2025.Names.Length == 16 && GermanLandReturns2021.Names.Length == 16, "sixteen Länder in both catalogs");
                bool sameOrder = true;
                for (int r = 0; r < 16; r++) { sameOrder &= GermanLandReturns2025.Names[r] == GermanLandReturns2021.Names[r]; }
                Check(sameOrder, "the two vintages list the Länder in one order - a region index means the same Land in both");
                long valid = 0;
                foreach (long v in GermanLandReturns2025.Valid) { valid += v; }
                Check(bund.TryGetValue("Gueltige", out long gueltige) && valid == gueltige, F("2025: the Länder's valid Zweitstimmen sum to the Bund's, {0:N0}", valid));
                for (int p = 0; p < GermanLandReturns2025.Parties.Length; p++)
                {
                    string key = GermanLandReturns2025.Parties[p];
                    long sum = 0;
                    for (int r = 0; r < 16; r++) { sum += GermanLandReturns2025.Votes[r][p]; }
                    Check(bund.TryGetValue(key, out long n) && n == sum, F("2025: {0}'s sixteen Länder sum to its national count, {1:N0}", key, sum));
                }

                // (b) the candidacy facts
                string[] keys = { "CDU", "CSU", "AfD", "SPD", "Grune", "Linke", "SSW", "BSW", "FDP" };
                RegionalVoteModel.RegionInput[] r2025 = GermanRegions.Regions(keys, new DateTime(2025, 3, 25));
                RegionalVoteModel.RegionInput[] r2021 = GermanRegions.Regions(keys, new DateTime(2024, 11, 6));
                int Where(RegionalVoteModel.RegionInput[] regions, int party, out string names)
                {
                    var list = new List<string>();
                    foreach (RegionalVoteModel.RegionInput x in regions) { if (x.PartyAvailable[party]) { list.Add(x.Name); } }
                    names = string.Join(", ", list.ToArray());
                    return list.Count;
                }
                Check(Where(r2025, 1, out string csu) == 1 && csu == "Bayern", "2025: the CSU stands in Bayern alone (" + csu + ")");
                Check(Where(r2025, 0, out _) == 15 && !Array.Exists(r2025, x => x.Name == "Bayern" && x.PartyAvailable[0]), "2025: the CDU stands in the fifteen other Länder, not in Bayern");
                Check(Where(r2025, 6, out string ssw) == 1 && ssw == "Schleswig-Holstein", "2025: the SSW stands in Schleswig-Holstein alone");
                Check(Where(r2021, 4, out _) == 15 && !Array.Exists(r2021, x => x.Name == "Saarland" && x.PartyAvailable[4]), "2021: the Grüne stand nowhere in Saarland (their Landesliste was rejected)");
                double[][] prior2021 = GermanRegions.PriorShares(keys, new DateTime(2024, 11, 6));
                bool bswStands = Where(r2021, 7, out _) == 16, bswNoRecord = true;
                foreach (double[] row in prior2021) { bswNoRecord &= row[7] == 0.0; }
                Check(bswStands && bswNoRecord, "2021: the BSW (founded 2024, no 2021 column) stands everywhere with a prior of 0 - no record, not absence");

                // (c) the seated vintage by date
                Check(WorldClock.SeatedVintage(CountryId.Germany, new DateTime(2024, 11, 6)) == ElectionVintage.Germany2021
                      && WorldClock.SeatedVintage(CountryId.Germany, new DateTime(2025, 3, 25)) == ElectionVintage.Germany2025,
                    "the snap start (6 Nov 2024) sits on the 2021 chamber; 25 Mar 2025 on, the 2025 chamber");

                // (d) the national election's derived breakdown for Germany
                bool withoutDate = NationalElection.TryPredictShares(CountryId.Germany, out Dictionary<string, double> plain);
                double[][] undated = NationalElection.LastRegionalShares;
                bool withDate = NationalElection.TryPredictShares(CountryId.Germany, out Dictionary<string, double> dated, on: new DateTime(2024, 11, 6));
                double[][] regional = NationalElection.LastRegionalShares;
                double[] weights = NationalElection.LastRegionalWeights;
                Check(withoutDate && withDate && undated == null, "without a date the prediction derives no regions (the caller gives the day it is held)");
                bool same = plain != null && dated != null && plain.Count == dated.Count;
                if (same) { foreach (KeyValuePair<string, double> kv in plain) { same &= dated.TryGetValue(kv.Key, out double d) && d == kv.Value; } }
                Check(same, "the national shares are the same with the date and without it - the regions are a readout, never an input");
                bool sixteen = regional != null && regional.Length == 16 && weights != null && weights.Length == 16;
                Check(sixteen, "with the date: sixteen Länder derived");
                if (sixteen && dated != null)
                {
                    IReadOnlyList<string> order = NationalElection.LastRegionalKeys;
                    double worst = 0.0, total = 0.0;
                    string worstParty = "none";
                    foreach (double w in weights) { total += w; }
                    for (int p = 0; order != null && p < order.Count; p++)
                    {
                        double agg = 0.0;
                        for (int r = 0; r < 16; r++) { agg += regional[r][p] * weights[r] / total; }
                        double e = Math.Abs(agg - dated[order[p]]);
                        if (e > worst) { worst = e; worstParty = order[p] + F(" ({0:F2} % national, {1:F2} % rebuilt)", dated[order[p]] * 100.0, agg * 100.0); }
                    }
                    double reported = NationalElection.LastRegionalWorstAbsError;
                    Check(order != null && worst < 1e-9 && Math.Abs(worst - reported) < 1e-9, F("the Länder's vote-weighted total reproduces the national shares: worst {0:E1} of a share ({2}), the swing's own residual {1:E1} - a one-Land party's swing spread over its Land alone, scaled (§689)", worst, reported, worstParty));
                }

                // (e) §696: THE SNAP START'S HISTORY - 2021 against 2017, the pair a German game's 2025 election reads on the 20th chamber
                bool pair = PartySystems.TryHistory(CountryId.Germany, out double[] l21, out double[] p17, ElectionVintage.Germany2021);
                bool later = PartySystems.TryHistory(CountryId.Germany, out double[] l25, out double[] p21, ElectionVintage.Germany2025);
                IReadOnlyList<PoliticalParty> roster = PartySystems.For(CountryId.Germany);
                int atBsw = -1, atSsw = -1;
                for (int i = 0; i < roster.Count; i++) { if (roster[i].Abbrev == "BSW") { atBsw = i; } else if (roster[i].Abbrev == "SSW") { atSsw = i; } }
                bool same2021 = pair && later && l21.Length == p21.Length;
                for (int i = 0; same2021 && i < l21.Length; i++) { same2021 &= l21[i] == p21[i]; }
                double sum17 = 0.0;
                if (pair) { foreach (double share17 in p17) { sum17 += share17; } }
                Check(pair && same2021 && p17.Length == roster.Count && atBsw >= 0 && atSsw >= 0 && p17[atBsw] == 0.0 && p17[atSsw] == 0.0 && sum17 > 90.0 && sum17 < 100.0,
                    F("the 20th chamber's pair: 2021 the same figures the 21st chamber's pair reads as its previous; 2017 the Bundeswahlleiterin's seven, summing {0:F1} %, with the SSW (no 2017 candidacy) and the BSW (not yet founded) at true zeros", sum17));
                using (PoliSim.Simulation.SimulationManager.EpochScope())
                {
                    WorldClock.ApplyStart(CountryId.Germany);   // the snap start, 6 Nov 2024 - the seated vintage the 2021 chamber
                    bool predicts = NationalElection.TryPredictShares(CountryId.Germany, out Dictionary<string, double> snap);
                    Check(predicts && snap != null && snap.Count > 0, "on the snap start the vote model predicts the 2025 election - a German game's own polling day had no prior and no loyalty to read before this pair");
                    if (predicts && later)
                    {
                        // printed, not asserted: the 2025 count the game plays toward, beside what the model predicts from 2021 and 2017 - out of sample
                        var readLine = new StringBuilder("    read      the 2025 prediction from 2021 and 2017 against the 2025 count (pp):");
                        double absDev = 0.0; int counted = 0;
                        for (int k = 0; k < roster.Count && k < l25.Length; k++)
                        {
                            if (!snap.TryGetValue(roster[k].Abbrev, out double predicted)) { continue; }
                            readLine.Append(F(" {0} {1:F1}/{2:F1}", roster[k].Abbrev, predicted * 100.0, l25[k]));
                            absDev += Math.Abs(predicted * 100.0 - l25[k]); counted++;
                        }
                        sb.Append(readLine).Append(F(" - mean absolute deviation {0:F2} pp over {1}\n", counted > 0 ? absDev / counted : double.NaN, counted));
                    }
                }

                // (f) §697: THE REGISTERED ELECTORATE - the runtime table against the sourced file, every figure, both years; each year's sixteen sum to its Bund
                string eligiblePath = Path.Combine(Path.GetDirectoryName(Application.dataPath), "ElectionsData/germany/land_eligible.csv");
                var fromFile = new Dictionary<string, (long E21, long E25)>();
                foreach (string raw in File.ReadAllLines(eligiblePath))
                {
                    if (raw.StartsWith("#", StringComparison.Ordinal) || raw.StartsWith("land;", StringComparison.Ordinal) || raw.Trim().Length == 0) { continue; }
                    string[] cells = raw.Split(';');
                    fromFile[cells[0]] = (long.Parse(cells[1], CultureInfo.InvariantCulture), long.Parse(cells[2], CultureInfo.InvariantCulture));
                }
                int eligibleMisses = 0; long sum21 = 0, sum25 = 0;
                DateTime on21 = new DateTime(2024, 11, 6), on25 = new DateTime(2025, 3, 25);
                for (int r = 0; r < GermanRegions.Count; r++)
                {
                    long e21 = GermanRegions.EligibleAt(r, on21), e25 = GermanRegions.EligibleAt(r, on25);
                    sum21 += e21; sum25 += e25;
                    if (!fromFile.TryGetValue(GermanRegions.NameAt(r), out (long E21, long E25) row) || row.E21 != e21 || row.E25 != e25) { eligibleMisses++; }
                }
                Check(fromFile.Count == 16 && eligibleMisses == 0 && sum21 == 61_172_771L && sum25 == 60_510_631L,
                    F("the registered electorate: sixteen Länder, every figure the sourced file's (land_eligible.csv), summing to the Bund's own rows - 2021 {0:N0}, 2025 {1:N0}", sum21, sum25));
            }
            catch (Exception e) { failures++; sb.Append("    FAIL      threw: ").Append(e.Message).Append('\n'); }
            sb.Append(failures == 0 ? "    CLEAN\n" : F("    {0} failure(s)\n", failures));
            if (failures > 0) { Debug.LogError(sb.ToString()); CheckExit.Finish(1); return; }
            Debug.Log(sb.ToString());
            CheckExit.Finish(0);
        }
    }
}
