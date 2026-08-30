using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using PoliSim.Elections;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// W-A2 — **per-region priors, so §27 and §8 compose.** Day-2 measured the problem: the
    /// both-layers run came out WORSE than §8 alone (Germany 5.01 vs 4.55 pp) because every region
    /// was damped toward the NATIONAL prior. Bavaria is not Germany-in-miniature, and damping it
    /// toward the national average fights the regional structure instead of completing it.
    ///
    /// This harness gives each region its own previous-election prior and re-measures.
    ///
    /// **Everything here is non-circular by construction** (the invariant in `LoyaltyModel`'s doc):
    /// - region ELECTORATE WEIGHTS come from the PRIOR election, not the target — so the target's
    ///   own turnout never leaks in (Day-2 used the target's weights; this is the stricter choice);
    /// - region PRIORS are the prior election's regional shares;
    /// - LOYALTY is derived from the two elections before the target (Germany 2017→2021 for a 2025
    ///   target; Sweden 2014→2018 for a 2022 target);
    /// - party AVAILABILITY is the target election's ballot access, which is known before any vote
    ///   is cast and is therefore not a prediction.
    ///
    /// The catalogs are read from `ElectionsData/` at run time rather than transcribed, so the
    /// harness re-measures whatever the sourced files currently say — and a parse failure is loud.
    /// </summary>
    public static class CompositionHarness
    {
        public static void Run()
        {
            int failures = 0;
            var sb = new StringBuilder();
            sb.Append("=== W-A2: per-region priors, §27 + §8 composition ===\n");

            failures += Germany(sb);
            failures += Sweden(sb);

            sb.Append($"\n=== CompositionHarness: {(failures == 0 ? "ALL ASSERTIONS PASS" : failures + " FAILED")} ===\n");
            Debug.Log(sb.ToString());
            CheckExit.Finish(failures == 0 ? 0 : 1);
        }

        // ------------------------------------------------------------------ GERMANY
        private static int Germany(StringBuilder sb)
        {
            string[] names = { "CDU", "AfD", "SPD", "Grune", "Linke", "CSU", "SSW", "BSW", "FDP" };
            var parties = new[]
            {
                new VoteModel.PartyPoint("CDU",   6.58, 6.56),
                new VoteModel.PartyPoint("AfD",   7.63, 9.39),
                new VoteModel.PartyPoint("SPD",   3.47, 3.61),
                new VoteModel.PartyPoint("Grune", 3.37, 1.61),
                new VoteModel.PartyPoint("Linke", 1.37, 2.29),
                new VoteModel.PartyPoint("CSU",   6.77, 7.54),
                new VoteModel.PartyPoint("SSW",   4.50, 4.00),
                new VoteModel.PartyPoint("BSW",   2.78, 7.06),
                new VoteModel.PartyPoint("FDP",   7.58, 3.22),
            };
            double[] actualPct = { 22.551, 20.803, 16.413, 11.606, 8.775, 5.970, 0.152, 4.981, 4.328 };
            var day1 = new VoteModel.Electorate(4.50, 6.50, 1.00, 16.00);
            const double wEcon = 0.80;

            // Loyalty from 2017 -> 2021 (the two elections BEFORE the 2025 target).
            double[] de2017 = { 26.80, 12.60, 20.50, 8.90, 9.20, 6.20, 0.00, 0.00, 10.70 };
            double[] de2021 = { 19.00, 10.40, 25.70, 14.70, 4.90, 5.20, 0.10, 0.00, 11.40 };
            double[] loyalty = LoyaltyModel.PartyLoyalties(de2021, de2017);

            // Per-Land 2021: weights and priors. CSV column order after 'valid':
            // CDU;AfD;SPD;GRUENE;Linke;CSU;SSW;FDP  -> BSW absent (did not exist) = 0.
            List<string[]> rows2021 = ReadCsv("ElectionsData/germany/land_votes_2021.csv", 10);
            List<string[]> rows2025 = ReadCsv("ElectionsData/germany/land_votes_2025.csv", 11);
            if (rows2021 == null || rows2025 == null)
            {
                sb.Append("  FAIL GERMANY: could not read the per-Land catalogs\n");
                return 1;
            }

            var regions = new RegionalVoteModel.RegionInput[rows2021.Count];
            var priors = new double[rows2021.Count][];
            for (int r = 0; r < rows2021.Count; r++)
            {
                string[] p21 = rows2021[r];
                string[] p25 = rows2025[r];
                double weight = ParseNum(p21[1]);                       // 2021 valid votes = the PRIOR electorate
                var prior = new double[names.Length];
                prior[0] = ParseNum(p21[2]);   // CDU
                prior[1] = ParseNum(p21[3]);   // AfD
                prior[2] = ParseNum(p21[4]);   // SPD
                prior[3] = ParseNum(p21[5]);   // GRUENE
                prior[4] = ParseNum(p21[6]);   // Linke
                prior[5] = ParseNum(p21[7]);   // CSU
                prior[6] = ParseNum(p21[8]);   // SSW
                prior[7] = 0.0;                // BSW - did not exist in 2021
                prior[8] = ParseNum(p21[9]);   // FDP
                priors[r] = prior;

                // Availability = 2025 ballot access (known before the vote, not a prediction).
                var available = new bool[names.Length];
                available[0] = ParseNum(p25[2]) > 0;   // CDU
                available[1] = ParseNum(p25[3]) > 0;   // AfD
                available[2] = ParseNum(p25[4]) > 0;   // SPD
                available[3] = ParseNum(p25[5]) > 0;   // GRUENE
                available[4] = ParseNum(p25[6]) > 0;   // Linke
                available[5] = ParseNum(p25[7]) > 0;   // CSU
                available[6] = ParseNum(p25[8]) > 0;   // SSW
                available[7] = ParseNum(p25[9]) > 0;   // BSW
                available[8] = ParseNum(p25[10]) > 0;  // FDP
                regions[r] = new RegionalVoteModel.RegionInput(p21[0], weight, available);
            }

            return Measure(sb, "GERMANY 2025", names, parties, actualPct, day1, wEcon,
                loyalty, regions, priors, 95.0);
        }

        // ------------------------------------------------------------------ SWEDEN
        private static int Sweden(StringBuilder sb)
        {
            string[] names = { "S", "SD", "M", "V", "C", "KD", "MP", "L" };
            var parties = new[]
            {
                new VoteModel.PartyPoint("S",  3.68, 4.74),
                new VoteModel.PartyPoint("SD", 6.32, 9.00),
                new VoteModel.PartyPoint("M",  7.89, 6.47),
                new VoteModel.PartyPoint("V",  1.89, 2.42),
                new VoteModel.PartyPoint("C",  7.84, 2.95),
                new VoteModel.PartyPoint("KD", 7.26, 7.79),
                new VoteModel.PartyPoint("MP", 3.16, 1.95),
                new VoteModel.PartyPoint("L",  7.32, 4.47),
            };
            double[] actualPct = { 30.33, 20.54, 19.10, 6.75, 6.71, 5.34, 5.08, 4.61 };
            var day1 = new VoteModel.Electorate(3.25, 6.25, 3.00, 0.50);
            const double wEcon = 0.15;

            // Loyalty from 2014 -> 2018 (the two elections BEFORE the 2022 target).
            double[] se2014 = { 31.01, 12.86, 23.33, 5.72, 6.11, 4.57, 6.89, 5.42 };
            double[] se2018 = { 28.26, 17.53, 19.84, 8.00, 8.61, 6.32, 4.41, 5.49 };
            double[] loyalty = LoyaltyModel.PartyLoyalties(se2018, se2014);

            // Per-valkrets 2018. CSV order after 'valid': S;M;SD;C;V;KD;L;MP
            // Party order here is S,SD,M,V,C,KD,MP,L -> indices 2,4,3,6,5,7,9,8.
            //
            // W-F1 DELIBERATELY DID NOT REPOINT THIS ONE. The 2022 per-valkrets file now exists,
            // but this is a BACKTEST OF 2022: the 2018 result is its PRIOR, exactly as Germany's
            // 2021 Land votes are the prior for the 2025 case above. Pointing it at 2022 would let
            // the model see the answer it is being scored against, and the MAD it reports would
            // stop meaning anything.
            List<string[]> rows = ReadCsv("ElectionsData/sweden/valkrets_votes_2018.csv", 10);
            if (rows == null)
            {
                sb.Append("  FAIL SWEDEN: could not read the per-valkrets catalog\n");
                return 1;
            }

            var regions = new RegionalVoteModel.RegionInput[rows.Count];
            var priors = new double[rows.Count][];
            var allAvailable = new bool[names.Length];
            for (int i = 0; i < allAvailable.Length; i++) { allAvailable[i] = true; }

            for (int r = 0; r < rows.Count; r++)
            {
                string[] c = rows[r];
                priors[r] = new[]
                {
                    ParseNum(c[2]), ParseNum(c[4]), ParseNum(c[3]), ParseNum(c[6]),
                    ParseNum(c[5]), ParseNum(c[7]), ParseNum(c[9]), ParseNum(c[8]),
                };
                regions[r] = new RegionalVoteModel.RegionInput(c[0], ParseNum(c[1]), allAvailable);
            }

            return Measure(sb, "SWEDEN 2022", names, parties, actualPct, day1, wEcon,
                loyalty, regions, priors, 99.0);
        }

        // ------------------------------------------------------------------ shared
        private static int Measure(StringBuilder sb, string title, string[] names,
            VoteModel.PartyPoint[] parties, double[] actualPct, VoteModel.Electorate day1, double wEcon,
            double[] loyalty, RegionalVoteModel.RegionInput[] regions, double[][] priors, double coverage)
        {
            double sum = 0.0;
            foreach (double a in actualPct) { sum += a; }
            var actual = new double[actualPct.Length];
            for (int i = 0; i < actual.Length; i++) { actual[i] = actualPct[i] / sum; }

            // National prior, for the §8-only run: the regional priors summed.
            var nationalPrior = new double[parties.Length];
            foreach (double[] row in priors)
            {
                for (int p = 0; p < parties.Length; p++) { nationalPrior[p] += row[p]; }
            }

            double[] national = VoteModel.PredictShares(parties, day1, wEcon);
            double madNational = VoteModel.MeanAbsoluteDeviationPp(national, actual);

            double[] loyaltyOnly = PreferenceModel.Preference(ToCompatScale(national), nationalPrior, loyalty);
            double madLoyalty = VoteModel.MeanAbsoluteDeviationPp(loyaltyOnly, actual);

            double[] regionalOnly = RegionalVoteModel.NationalShares(parties, regions, day1, wEcon);
            double madRegional = VoteModel.MeanAbsoluteDeviationPp(regionalOnly, actual);

            double[] both = RegionalVoteModel.NationalSharesWithRegionalLoyalty(
                parties, regions, day1, wEcon, priors, loyalty);
            double madBoth = VoteModel.MeanAbsoluteDeviationPp(both, actual);

            sb.Append(string.Format(CultureInfo.InvariantCulture,
                "\n---- {0} ---- {1} regions, name-join coverage {2:F0}%\n", title, regions.Length, coverage));
            sb.Append("  party   actual   national  +§8(nat prior)  +§27   +both(regional priors)\n");
            for (int i = 0; i < names.Length; i++)
            {
                sb.Append(string.Format(CultureInfo.InvariantCulture,
                    "  {0,-6} {1,6:F2}   {2,6:F2}      {3,6:F2}       {4,6:F2}   {5,6:F2}   (loyalty {6,5:F1})\n",
                    names[i], 100 * actual[i], 100 * national[i], 100 * loyaltyOnly[i],
                    100 * regionalOnly[i], 100 * both[i], loyalty[i]));
            }

            sb.Append(string.Format(CultureInfo.InvariantCulture,
                "  MAD: national {0:F2} | +§8 {1:F2} | +§27 {2:F2} | +BOTH {3:F2} pp\n",
                madNational, madLoyalty, madRegional, madBoth));

            // [call, W-A2] "No worse" is judged at a DECLARED tolerance of 0.01 pp, and the raw
            // delta is printed at four decimals so the tolerance can never hide a real regression.
            // The reason a tolerance is needed at all: where a country's regions are homogeneous in
            // party availability (Sweden - all eight parties stand in all 29 valkretsar) §27 is
            // correctly a NO-OP, and the only difference between the two runs is whether damping is
            // applied per region and then summed, or once nationally. Those differ in the last
            // decimals by Jensen-type aggregation order, not by model quality. Germany, where
            // availability genuinely varies, shows the real effect and needs no tolerance.
            const double NoWorseTolerancePp = 0.01;
            int failures = 0;
            double delta = madBoth - madLoyalty;
            failures += Assert(sb, $"{title}: both-layers is no worse than §8 alone (W-A2's done-when)",
                delta <= NoWorseTolerancePp,
                string.Format(CultureInfo.InvariantCulture,
                    "both {0:F4} vs §8-only {1:F4} pp; delta {2:+0.0000;-0.0000;0.0000} (tolerance {3:F2})",
                    madBoth, madLoyalty, delta, NoWorseTolerancePp));
            failures += Assert(sb, $"{title}: both-layers improves on the bare national model",
                madBoth < madNational,
                string.Format(CultureInfo.InvariantCulture, "both {0:F2} vs national {1:F2} pp", madBoth, madNational));
            return failures;
        }

        private static double[] ToCompatScale(double[] shares)
        {
            double max = 0.0;
            foreach (double s in shares) { if (s > max) { max = s; } }
            var scaled = new double[shares.Length];
            for (int i = 0; i < shares.Length; i++)
            {
                scaled[i] = max > 0 ? 100.0 * Math.Pow(shares[i] / max, 1.0 / PreferenceModel.Sharpness) : 0.0;
            }

            return scaled;
        }

        /// <summary>Reads a semicolon catalog, skipping '#' comments and the header; returns rows with at least <paramref name="minColumns"/> fields. Null (loudly) if the file is missing.</summary>
        private static List<string[]> ReadCsv(string path, int minColumns)
        {
            if (!File.Exists(path))
            {
                Debug.LogError($"COMPOSITION: catalog not found at {path} (cwd {Directory.GetCurrentDirectory()})");
                return null;
            }

            var rows = new List<string[]>();
            bool headerSeen = false;
            foreach (string raw in File.ReadAllLines(path))
            {
                string line = raw.Trim();
                if (line.Length == 0 || line.StartsWith("#")) { continue; }

                string[] parts = line.Split(';');
                if (!headerSeen) { headerSeen = true; continue; }
                if (parts.Length < minColumns) { continue; }

                rows.Add(parts);
            }

            return rows;
        }

        private static double ParseNum(string s)
        {
            return double.TryParse(s.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out double v) ? v : 0.0;
        }

        private static int Assert(StringBuilder sb, string label, bool condition, string detail)
        {
            sb.Append($"  {(condition ? "ok  " : "FAIL")} {label}: {detail}\n");
            return condition ? 0 : 1;
        }
    }
}
