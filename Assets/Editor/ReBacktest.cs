using System;
using System.Text;
using PoliSim.Elections;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// DAY-2 PART 3 — the re-backtest, and R-EL13's gate. Same sourced spine, **the same declared
    /// parameters as Day-1**, no new calibration pass: every difference measured here is the added
    /// LAYERS, not the tuning.
    ///
    /// Four runs per country, as far as each country's sourced data supports:
    /// - **A. Day-1 national** — the four-parameter spatial model, recomputed here rather than
    ///   quoted, so the comparison is arithmetic and not memory.
    /// - **B. + §27 regional** — the national vote as a weighted sum of regions, each with its real
    ///   electorate size and its real party availability. Germany only: it is the country whose
    ///   per-region ABSOLUTE counts are on file (`land_votes_2025.csv`, from the official
    ///   `kerg2.csv`), and it is also the country whose deviation named this layer.
    /// - **C. + §8 loyalty** — damping toward the PREVIOUS election's national shares.
    /// - **D. + both**, where both are available.
    ///
    /// **The loyalty constant is declared, not fitted.** `Loyalty = 60` is the spec's own "Lean"
    /// — the middle rung of §8's five-point scale (Strong Loyalist 90 / Loyal 75 / Lean 60 /
    /// Independent 40 / Swing 20) — chosen a priori, applied uniformly to all four countries, and
    /// NOT varied to improve any result. A single value across four very different party systems
    /// is certainly wrong in detail; it is the honest way to measure whether the LAYER helps
    /// without smuggling in a fitted parameter (kickoff: "no new calibration pass").
    ///
    /// **The priors are real previous elections, never the election being predicted** — Germany
    /// 2021, Sweden 2018, Poland 2019, Italy 2018, all from the official sources recorded in
    /// `ElectionsData/*/priors_*.md`. Where a 2022/2023/2025 party had NO predecessor, its prior is
    /// **zero, and that is the model's correct statement of newness**, not a hack: nobody had voted
    /// for it before, so it must win its entire vote from the persuadable fraction. Where a mapping
    /// is defensible but imperfect, the bias direction is stated per country below and repeated in
    /// the report.
    /// </summary>
    public static class ReBacktest
    {
        /// <summary>[AUTHORED-DRAFT] §8's "Lean" rung, chosen a priori and never tuned — see the class doc.</summary>
        public const double Loyalty = 60.0;

        private readonly struct Country
        {
            public readonly string Name;
            public readonly string[] PartyNames;
            public readonly VoteModel.PartyPoint[] Parties;
            public readonly double[] ActualPct;
            public readonly double[] PriorPct;
            public readonly VoteModel.Electorate Day1;
            public readonly double WEcon;
            public readonly double Day1Mad;
            public readonly string PriorNote;

            public Country(string name, string[] partyNames, VoteModel.PartyPoint[] parties, double[] actualPct,
                double[] priorPct, VoteModel.Electorate day1, double wEcon, double day1Mad, string priorNote)
            {
                Name = name;
                PartyNames = partyNames;
                Parties = parties;
                ActualPct = actualPct;
                PriorPct = priorPct;
                Day1 = day1;
                WEcon = wEcon;
                Day1Mad = day1Mad;
                PriorNote = priorNote;
            }
        }

        public static void Run()
        {
            var report = new StringBuilder();
            report.Append("=== ReBacktest (Day-2 Part 3) - same spine, SAME Day-1 parameters, loyalty declared a priori at ");
            report.Append(Loyalty.ToString("F0", System.Globalization.CultureInfo.InvariantCulture));
            report.Append(" ===\n");

            Country[] countries = BuildCountries();
            var gateRows = new StringBuilder();
            bool gatePass = true;

            foreach (Country c in countries)
            {
                double[] actual = Normalise(c.ActualPct);
                double[] runA = VoteModel.PredictShares(c.Parties, c.Day1, c.WEcon);
                double madA = VoteModel.MeanAbsoluteDeviationPp(runA, actual);

                double[] prior = Normalise(c.PriorPct);
                double[] runC = PreferenceModel.Preference(ToCompatibilityScale(runA), prior, Loyalty);
                double madC = VoteModel.MeanAbsoluteDeviationPp(runC, actual);

                report.Append($"\n---- {c.Name} ----\n  prior basis: {c.PriorNote}\n");
                report.Append("  party    actual   A:nat   devA    C:+loyalty  devC\n");
                for (int i = 0; i < c.PartyNames.Length; i++)
                {
                    report.Append(string.Format(System.Globalization.CultureInfo.InvariantCulture,
                        "  {0,-7} {1,6:F2}  {2,6:F2} {3,7:+0.00;-0.00;0.00}   {4,6:F2}  {5,7:+0.00;-0.00;0.00}\n",
                        c.PartyNames[i], 100 * actual[i], 100 * runA[i], 100 * (runA[i] - actual[i]),
                        100 * runC[i], 100 * (runC[i] - actual[i])));
                }

                report.Append(string.Format(System.Globalization.CultureInfo.InvariantCulture,
                    "  MAD: Day-1 recorded {0:F2} -> A recomputed {1:F2} -> C with §8 loyalty {2:F2} pp\n",
                    c.Day1Mad, madA, madC));

                double best = madC;
                string bestLabel = "C (+§8)";

                // Germany alone has the sourced per-region weights and availability.
                if (c.Name == "GERMANY")
                {
                    RegionalVoteModel.RegionInput[] regions = BuildGermanRegions(c.Parties.Length);
                    double[] runB = RegionalVoteModel.NationalShares(c.Parties, regions, c.Day1, c.WEcon);
                    double madB = VoteModel.MeanAbsoluteDeviationPp(runB, actual);

                    var regionPriors = new double[regions.Length][];
                    for (int r = 0; r < regions.Length; r++) { regionPriors[r] = prior; }
                    double[] runD = RegionalVoteModel.NationalSharesWithLoyalty(
                        c.Parties, regions, c.Day1, c.WEcon, regionPriors, Loyalty);
                    double madD = VoteModel.MeanAbsoluteDeviationPp(runD, actual);

                    report.Append("  party    actual   B:+§27  devB    D:+both  devD\n");
                    for (int i = 0; i < c.PartyNames.Length; i++)
                    {
                        report.Append(string.Format(System.Globalization.CultureInfo.InvariantCulture,
                            "  {0,-7} {1,6:F2}  {2,6:F2} {3,7:+0.00;-0.00;0.00}   {4,6:F2}  {5,7:+0.00;-0.00;0.00}\n",
                            c.PartyNames[i], 100 * actual[i], 100 * runB[i], 100 * (runB[i] - actual[i]),
                            100 * runD[i], 100 * (runD[i] - actual[i])));
                    }

                    report.Append(string.Format(System.Globalization.CultureInfo.InvariantCulture,
                        "  MAD: B with §27 regional {0:F2} pp; D with both {1:F2} pp\n", madB, madD));
                    report.Append("  the named deviations:\n");
                    report.Append(Deviation("CSU (+7.4 Day-1, named as regional structure)", 5, actual, runA, runB, "§27"));
                    report.Append(Deviation("CDU (-9.3 Day-1)", 0, actual, runA, runB, "§27"));
                    report.Append(Deviation("BSW (+10.2 Day-1, named as loyalty)", 7, actual, runA, runC, "§8"));
                    report.Append(Deviation("AfD (-11.7 Day-1)", 1, actual, runA, runD, "both"));

                    if (madB < best) { best = madB; bestLabel = "B (+§27)"; }
                    if (madD < best) { best = madD; bestLabel = "D (+both)"; }
                }

                // GERMANY (9 parties, with SSW) is a §27 demonstration, not a like-for-like
                // comparison against Day-1's eight-party 5.78 — it is excluded from the gate and
                // GERMANY-8 carries Germany's gate row instead. Stated rather than silently dropped.
                bool countsForGate = c.Name != "GERMANY";
                bool improved = best < c.Day1Mad;
                if (countsForGate && !improved) { gatePass = false; }

                gateRows.Append(string.Format(System.Globalization.CultureInfo.InvariantCulture,
                    "  {0,-10} Day-1 {1,5:F2} -> best Day-2 {2,5:F2} pp  [{3,-9}]  {4}{5}\n",
                    c.Name, c.Day1Mad, best, bestLabel, improved ? "IMPROVED" : "REGRESSED",
                    countsForGate ? "" : "  (NOT IN GATE - 9-party set, §27 demo only)"));
            }

            report.Append("\n=== R-EL13 GATE ===\n");
            report.Append(gateRows);
            report.Append(gatePass
                ? "  VERDICT: every country improved on its Day-1 figure. The vote-share half of the gate PASSES;\n" +
                  "  the seat half is SeatAllocationBacktest at this same boundary (must stay 5/6 exact).\n"
                : "  VERDICT: at least one country did NOT improve. The vote-share half of the gate FAILS,\n" +
                  "  and per R-EL13 Part 4 does NOT run. The table above is the finding.\n");

            Debug.Log(report.ToString());
            CheckExit.Finish(0);
        }

        /// <summary>Spatial shares are a distribution; §8 wants compatibility-like magnitudes. Rescaling to 0-100 by the max preserves the ordering and ratios PreferenceModel then exponentiates.</summary>
        private static double[] ToCompatibilityScale(double[] shares)
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

        private static double[] Normalise(double[] pct)
        {
            double sum = 0.0;
            foreach (double p in pct) { sum += p; }
            var result = new double[pct.Length];
            for (int i = 0; i < pct.Length; i++) { result[i] = sum > 0 ? pct[i] / sum : 0.0; }
            return result;
        }

        private static string Deviation(string label, int index, double[] actual, double[] before, double[] after, string layer)
        {
            double b = 100 * (before[index] - actual[index]);
            double a = 100 * (after[index] - actual[index]);
            return string.Format(System.Globalization.CultureInfo.InvariantCulture,
                "    {0,-48} {1,7:+0.0;-0.0;0.0} -> {2,7:+0.0;-0.0;0.0} pp via {3,-5} {4}\n",
                label, b, a, layer, Math.Abs(a) < Math.Abs(b) ? "improved" : "NOT improved");
        }

        private static RegionalVoteModel.RegionInput[] BuildGermanRegions(int partyCount)
        {
            var regions = new RegionalVoteModel.RegionInput[LandNames.Length];
            for (int l = 0; l < LandNames.Length; l++)
            {
                var available = new bool[partyCount];
                for (int p = 0; p < partyCount; p++) { available[p] = LandVotes[l][p] > 0; }
                regions[l] = new RegionalVoteModel.RegionInput(LandNames[l], LandValid[l], available);
            }

            return regions;
        }

        private static Country[] BuildCountries()
        {
            return new[]
            {
                new Country("GERMANY",
                    new[] { "CDU", "AfD", "SPD", "Grune", "Linke", "CSU", "SSW", "BSW", "FDP" },
                    new[]
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
                    },
                    new[] { 22.551, 20.803, 16.413, 11.606, 8.775, 5.970, 0.152, 4.981, 4.328 },
                    // 2021 Zweitstimmen shares (Bundeswahlleiterin, post-Berlin-re-run official version).
                    // BSW = 0: it did not exist in 2021 - the model's correct statement of newness.
                    new[] { 19.0, 10.4, 25.7, 14.7, 4.9, 5.2, 0.1, 0.0, 11.4 },
                    new VoteModel.Electorate(4.50, 6.50, 1.00, 16.00), 0.80, 5.78,
                    "Germany 2021 Zweitstimmen; BSW has NO prior (founded 2024 as a Linke split) - zero, stated; Linke's 4.9 is not a clean baseline for the 2025 Linke for the same reason"),

                // The LIKE-FOR-LIKE Germany: Day-1's exact eight-party set (no SSW). Day-1's 5.78
                // was computed over these eight, so this is the only valid comparison against it;
                // the nine-party entry above adds SSW because §27's availability case needs it
                // (SSW stands in one Land), and its run A is therefore NOT comparable to 5.78.
                new Country("GERMANY-8",
                    new[] { "CDU", "AfD", "SPD", "Grune", "Linke", "CSU", "BSW", "FDP" },
                    new[]
                    {
                        new VoteModel.PartyPoint("CDU",   6.58, 6.56),
                        new VoteModel.PartyPoint("AfD",   7.63, 9.39),
                        new VoteModel.PartyPoint("SPD",   3.47, 3.61),
                        new VoteModel.PartyPoint("Grune", 3.37, 1.61),
                        new VoteModel.PartyPoint("Linke", 1.37, 2.29),
                        new VoteModel.PartyPoint("CSU",   6.77, 7.54),
                        new VoteModel.PartyPoint("BSW",   2.78, 7.06),
                        new VoteModel.PartyPoint("FDP",   7.58, 3.22),
                    },
                    new[] { 22.551, 20.803, 16.413, 11.606, 8.775, 5.970, 4.981, 4.328 },
                    new[] { 19.0, 10.4, 25.7, 14.7, 4.9, 5.2, 0.0, 11.4 },
                    new VoteModel.Electorate(4.50, 6.50, 1.00, 16.00), 0.80, 5.78,
                    "Germany 2021, Day-1's eight-party set - the ONLY like-for-like comparison against Day-1's 5.78"),

                new Country("SWEDEN",
                    new[] { "S", "SD", "M", "V", "C", "KD", "MP", "L" },
                    new[]
                    {
                        new VoteModel.PartyPoint("S",  3.68, 4.74),
                        new VoteModel.PartyPoint("SD", 6.32, 9.00),
                        new VoteModel.PartyPoint("M",  7.89, 6.47),
                        new VoteModel.PartyPoint("V",  1.89, 2.42),
                        new VoteModel.PartyPoint("C",  7.84, 2.95),
                        new VoteModel.PartyPoint("KD", 7.26, 7.79),
                        new VoteModel.PartyPoint("MP", 3.16, 1.95),
                        new VoteModel.PartyPoint("L",  7.32, 4.47),
                    },
                    new[] { 30.33, 20.54, 19.10, 6.75, 6.71, 5.34, 5.08, 4.61 },
                    // 2018 Riksdag (Valmyndigheten final) - the ONE clean party-for-party join of the four.
                    new[] { 28.26, 17.53, 19.84, 8.00, 8.61, 6.32, 4.41, 5.49 },
                    new VoteModel.Electorate(3.25, 6.25, 3.00, 0.50), 0.15, 3.25,
                    "Sweden 2018 final result - all eight parties contested both elections as the same entities; no mapping needed"),

                new Country("POLAND",
                    new[] { "PiS", "KO", "TD", "NL", "Konf" },
                    new[]
                    {
                        new VoteModel.PartyPoint("PiS",  2.52, 8.45),
                        new VoteModel.PartyPoint("KO",   6.17, 3.66),
                        new VoteModel.PartyPoint("TD",   5.25, 5.93),
                        new VoteModel.PartyPoint("NL",   2.32, 1.75),
                        new VoteModel.PartyPoint("Konf", 8.96, 8.41),
                    },
                    new[] { 35.38, 30.70, 14.40, 8.61, 7.16 },
                    // 2019 Sejm (PKW obwieszczenie). TD's prior is PSL-Koalicja Polska's 8.55 and
                    // UNDERSTATES it: Polska 2050, TD's other half, did not exist in 2019.
                    new[] { 43.59, 27.40, 8.55, 12.56, 6.81 },
                    new VoteModel.Electorate(3.50, 7.00, 1.50, 8.00), 0.54, 6.99,
                    "Poland 2019; TD's prior = PSL-Koalicja Polska 8.55 which UNDERSTATES it (Polska 2050 did not exist); NL's = SLD 12.56 with composition drift"),

                new Country("ITALY",
                    new[] { "FdI", "PD", "M5S", "Lega", "FI", "AzIV", "AVS" },
                    new[]
                    {
                        new VoteModel.PartyPoint("FdI",   6.40, 9.13),
                        new VoteModel.PartyPoint("PD",    2.93, 2.33),
                        new VoteModel.PartyPoint("M5S",   2.87, 3.27),
                        new VoteModel.PartyPoint("Lega",  6.80, 8.87),
                        new VoteModel.PartyPoint("FI",    7.40, 6.07),
                        new VoteModel.PartyPoint("AzIV",  5.21, 3.46),
                        new VoteModel.PartyPoint("AVS",   1.80, 1.70),
                    },
                    new[] { 25.98, 19.04, 15.43, 8.79, 8.11, 7.78, 3.64 },
                    // 2018 Camera proportional (Eligendo). AzIV = 0: neither Azione nor Italia Viva
                    // existed in 2018 (both 2019 splits from the PD), so the PD's 18.76 prior
                    // OVERSTATES the 2022 PD by the material those two were built from.
                    new[] { 4.35, 18.76, 32.68, 17.35, 14.00, 0.0, 3.39 },
                    new VoteModel.Electorate(4.25, 7.00, 1.00, 4.00), 0.79, 5.61,
                    "Italy 2018 proportional; AzIV has NO prior (both halves are 2019 PD splits) - zero, stated; PD's 18.76 therefore OVERSTATES the 2022 PD; AVS's 3.39 is LeU, a partial lineage only"),
            };
        }

        private static readonly string[] LandNames =
        {
            "Schleswig-Holstein", "Mecklenburg-Vorpommern", "Hamburg", "Niedersachsen", "Bremen",
            "Brandenburg", "Sachsen-Anhalt", "Berlin", "Nordrhein-Westfalen", "Sachsen", "Hessen",
            "Thueringen", "Rheinland-Pfalz", "Bayern", "Baden-Wuerttemberg", "Saarland",
        };

        private static readonly double[] LandValid =
        {
            1880005, 1021242, 1045620, 5015336, 348256, 1647338, 1337349, 1949533,
            10526134, 2569572, 3580834, 1324160, 2482358, 7972054, 6349998, 599723,
        };

        // ElectionsData/germany/land_votes_2025.csv - official kerg2.csv, Gebietsart=Land, Stimme=2.
        // Column order: CDU, AfD, SPD, GRUENE, Linke, CSU, SSW, BSW, FDP. The zeros are the
        // candidacy facts: CDU does not stand in Bayern, CSU stands nowhere else, SSW only in
        // Schleswig-Holstein.
        private static readonly double[][] LandVotes =
        {
            new double[] { 518424, 306165, 352546, 279923, 146428, 0, 76138, 64777, 88147 },
            new double[] { 181956, 357361, 126687, 54719, 123059, 0, 0, 107872, 32678 },
            new double[] { 216935, 113608, 237740, 201713, 151115, 0, 0, 41919, 47115 },
            new double[] { 1410418, 894540, 1153523, 576845, 405519, 0, 0, 189376, 205163 },
            new double[] { 71573, 52496, 80604, 54280, 51461, 0, 0, 15114, 12295 },
            new double[] { 298048, 535275, 244010, 108598, 176224, 0, 0, 176405, 53467 },
            new double[] { 256538, 496110, 146535, 59077, 143807, 0, 0, 150411, 41251 },
            new double[] { 356099, 296990, 295182, 328035, 387222, 0, 0, 129651, 74076 },
            new double[] { 3170627, 1770379, 2108434, 1300901, 877123, 0, 0, 432911, 462446 },
            new double[] { 507247, 958401, 217144, 167269, 290462, 0, 0, 232257, 83436 },
            new double[] { 1033842, 636778, 657510, 451510, 311058, 0, 0, 158653, 180823 },
            new double[] { 246065, 510527, 115915, 56097, 200688, 0, 0, 124760, 37292 },
            new double[] { 760623, 498695, 462705, 256869, 161867, 0, 0, 105103, 114047 },
            new double[] { 0, 1515731, 920675, 957435, 456935, 2964028, 0, 246518, 333257 },
            new double[] { 2006866, 1256430, 898778, 865738, 429484, 0, 0, 260219, 357539 },
            new double[] { 161113, 129294, 131136, 43371, 44080, 0, 0, 37001, 25725 },
        };
    }
}
