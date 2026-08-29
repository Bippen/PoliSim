using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using PoliSim.Elections;
using PoliSim.Simulation;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// W-D1's harness — §27's election day on Sweden's 29 valkretsar.
    ///
    /// The done-when, asserted:
    /// 1. **the same seed reproduces a result exactly** — seed 777 twice on `ElectionNoise` gives
    ///    the same digest of every regional vote count, and a different seed does not;
    /// 2. **400 replays show the noise distribution matching its declared σ** — per region, the
    ///    standard deviation of each party's share across replays is the declared 1.2 pp (after
    ///    re-normalisation, within a stated tolerance), and nationally it is 1.2 / √N_eff, where
    ///    N_eff = 1 / Σ w² over the valkretsar's eligible weights — the 1/√n behaviour Day-1
    ///    measured on eight equal regions, now on the real, unequal 29;
    /// 3. the count is a count — votes per region sum to the region's votes cast, the nation's to
    ///    the sum of regions, turnout is what W-B11 says it is, and with σ = 0 the expected result
    ///    is reproduced to the vote.
    /// Staging as W-B11's: the 2022 vector as every region's preference (one group per region
    /// until W-F4), base turnout 84.21 %, neutral attributes, S's three worked valkretsar.
    /// </summary>
    public static class ElectionDayHarness
    {
        private static readonly string[] Parties = { "S", "SD", "M", "V", "C", "KD", "MP", "L" };
        private static readonly double[] Shares2022 = { 0.3033, 0.2054, 0.1910, 0.0675, 0.0671, 0.0534, 0.0508, 0.0461 };
        private const double BaseTurnout2022 = 0.8421;
        private const double Turnout2018 = 0.8718;

        public static void Run()
        {
            CheckExit.ArmLogFold();
            int failures = 0;
            var sb = new StringBuilder();
            sb.Append("=== W-D1: election day (§27) on the 29 valkretsar, noise on the ElectionNoise stream ===\n");

            double[] prior = Normalised(Shares2022);
            double[] eligible = ReadEligible(out string[] names);
            var preference = new double[eligible.Length][];
            for (int r = 0; r < eligible.Length; r++) { preference[r] = prior; }
            const double neutral = 50.0;

            RegionalMobilization Staged()
            {
                var g = new RegionalMobilization(eligible, prior.Length);
                foreach (string v in new[] { "Stockholms län", "Skåne läns södra", "Gotlands län" })
                {
                    g.Operate(Array.IndexOf(names, v), 0, GotvOperation.DoorKnocking, 400_000, 20_000, out _, out _);
                }

                return g;
            }

            // ---------- 1. determinism ----------
            ElectionDay.Result a = Count(Staged(), preference, names, 777);
            ElectionDay.Result b = Count(Staged(), preference, names, 777);
            ElectionDay.Result c = Count(Staged(), preference, names, 778);
            failures += Assert(sb, "1a. the same seed reproduces every regional vote count exactly", a.Digest == b.Digest, $"{a.Digest} twice");
            failures += Assert(sb, "1b. a different seed gives a different count", a.Digest != c.Digest, $"{a.Digest} vs {c.Digest}");

            sb.Append(string.Format(CultureInfo.InvariantCulture, "\n  seed 777: {0:N0} votes cast of {1:N0} eligible, turnout {2:P2}; national shares ",
                a.VotesCast, a.Eligible, a.NationalTurnout));
            for (int p = 0; p < Parties.Length; p++) { sb.Append(string.Format(CultureInfo.InvariantCulture, "{0} {1:P2}  ", Parties[p], a.NationalShares[p])); }
            sb.Append('\n');

            // ---------- 3. a count is a count ----------
            bool regionsSum = true;
            double summed = 0.0;
            foreach (RegionalAggregation.RegionResult r in a.Regions)
            {
                double s = 0.0;
                foreach (double v in r.Votes) { s += v; }
                if (Math.Abs(s - r.VotesCast) > 0.5 * r.Votes.Length) { regionsSum = false; }   // rounding per party
                summed += s;
            }

            failures += Assert(sb, "3a. every region's party votes sum to its votes cast (to rounding)", regionsSum, $"{a.Regions.Length} regions");
            failures += Assert(sb, "3b. the nation's votes are the sum of the regions'", Math.Abs(Sum(a.NationalVotes) - summed) < 1e-6, string.Format(CultureInfo.InvariantCulture, "{0:N0}", Sum(a.NationalVotes)));

            RegionalMobilization staged = Staged();
            double expectedTurnout = staged.NationalTurnout(prior, BaseTurnout2022, neutral, neutral, neutral);
            failures += Assert(sb, "3c. turnout is W-B11's (the ground game's three valkretsar included)",
                Math.Abs(a.NationalTurnout - expectedTurnout) < 1e-9, string.Format(CultureInfo.InvariantCulture, "{0:P4}", a.NationalTurnout));

            ElectionDay.Result exact = ElectionDay.Count(names, preference, Staged(), BaseTurnout2022, neutral, neutral, neutral, new System.Random(1), noiseSigmaPp: 0.0);
            bool matchesExpected = true;
            for (int r = 0; r < eligible.Length; r++)
            {
                double[] expectedVotes = staged.RegionVotes(r, prior, BaseTurnout2022, neutral, neutral, neutral);
                for (int p = 0; p < prior.Length; p++) { if (Math.Abs(exact.Regions[r].Votes[p] - Math.Round(expectedVotes[p])) > 0.5) { matchesExpected = false; } }
            }

            failures += Assert(sb, "3d. with sigma = 0 the count IS the expected result, region by region, to the vote", matchesExpected, "29 x 8");

            // ---------- 2. the noise distribution over 400 replays ----------
            const int replays = 400;
            var regionalShares = new double[replays][][];   // [replay][region][party]
            var nationalShares = new double[replays][];
            for (int i = 0; i < replays; i++)
            {
                ElectionDay.Result res = Count(Staged(), preference, names, 10_000 + i);
                regionalShares[i] = new double[eligible.Length][];
                for (int r = 0; r < eligible.Length; r++)
                {
                    var s = new double[prior.Length];
                    for (int p = 0; p < prior.Length; p++) { s[p] = res.Regions[r].VotesCast > 0 ? res.Regions[r].Votes[p] / res.Regions[r].VotesCast : 0.0; }
                    regionalShares[i][r] = s;
                }

                nationalShares[i] = res.NationalShares;
            }

            // Regional σ: mean over regions and parties of the across-replay standard deviation, in pp.
            double regionalSigmaSum = 0.0;
            int cells = 0;
            for (int r = 0; r < eligible.Length; r++)
            {
                for (int p = 0; p < prior.Length; p++)
                {
                    var series = new double[replays];
                    for (int i = 0; i < replays; i++) { series[i] = 100.0 * regionalShares[i][r][p]; }
                    regionalSigmaSum += StdDev(series);
                    cells++;
                }
            }

            double regionalSigma = regionalSigmaSum / cells;

            double nationalSigmaSum = 0.0;
            for (int p = 0; p < prior.Length; p++)
            {
                var series = new double[replays];
                for (int i = 0; i < replays; i++) { series[i] = 100.0 * nationalShares[i][p]; }
                nationalSigmaSum += StdDev(series);
            }

            double nationalSigma = nationalSigmaSum / prior.Length;
            double nEff = ElectionDay.EffectiveRegions(eligible);
            double predictedNational = regionalSigma / Math.Sqrt(nEff);

            sb.Append(string.Format(CultureInfo.InvariantCulture,
                "\n  400 replays: regional share sigma {0:F3} pp (declared {1:F1} before re-normalisation); national {2:F3} pp; N_eff of the 29 valkretsar by eligible weight {3:F2}, so 1/sqrt(N_eff) predicts {4:F3}\n",
                regionalSigma, RegionalAggregation.RegionalNoiseSigmaPp, nationalSigma, nEff, predictedNational));

            failures += Assert(sb, "2a. the regional noise is the declared sigma within re-normalisation's shrink (0.8 to 1.0 of 1.2 pp)",
                regionalSigma >= 0.8 * RegionalAggregation.RegionalNoiseSigmaPp && regionalSigma <= 1.0 * RegionalAggregation.RegionalNoiseSigmaPp + 1e-9,
                string.Format(CultureInfo.InvariantCulture, "{0:F3} pp", regionalSigma));
            failures += Assert(sb, "2b. the national noise is the regional sigma / sqrt(N_eff) within 15 % (the 1/sqrt(n) behaviour on the real, unequal valkretsar)",
                Math.Abs(nationalSigma - predictedNational) <= 0.15 * predictedNational,
                string.Format(CultureInfo.InvariantCulture, "measured {0:F3} vs predicted {1:F3} pp", nationalSigma, predictedNational));
            failures += Assert(sb, "2c. the noise is small enough that strategy matters and large enough that the result is not certain (national sigma between 0.1 and 1 pp)",
                nationalSigma > 0.1 && nationalSigma < 1.0, string.Format(CultureInfo.InvariantCulture, "{0:F3} pp", nationalSigma));

            sb.Append($"\n=== ElectionDayHarness: {(failures == 0 ? "ALL ASSERTIONS PASS" : failures + " FAILED")} ===\n");
            Debug.Log(sb.ToString());
            CheckExit.Finish(failures == 0 ? 0 : 1);
        }

        private static ElectionDay.Result Count(RegionalMobilization gotv, double[][] preference, string[] names, int seed)
        {
            SimulationRandom.Seed(seed);
            return ElectionDay.Count(names, preference, gotv, BaseTurnout2022, 50.0, 50.0, 50.0, SimulationRandom.For(SimulationRandom.Stream.ElectionNoise));
        }

        private static double[] ReadEligible(out string[] names)
        {
            string path = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "ElectionsData", "sweden", "valkrets_votes_2018.csv"));
            var eligible = new List<double>();
            var nameList = new List<string>();
            foreach (string raw in File.ReadAllLines(path))
            {
                string line = raw.Trim();
                if (line.Length == 0 || line.StartsWith("#") || line.StartsWith("valkrets;")) { continue; }
                string[] cells = line.Split(';');
                nameList.Add(cells[0]);
                eligible.Add(double.Parse(cells[1], CultureInfo.InvariantCulture) / Turnout2018);
            }

            names = nameList.ToArray();
            return eligible.ToArray();
        }

        private static double StdDev(double[] v)
        {
            double mean = 0.0;
            foreach (double x in v) { mean += x; }
            mean /= v.Length;
            double ss = 0.0;
            foreach (double x in v) { ss += (x - mean) * (x - mean); }
            return Math.Sqrt(ss / (v.Length - 1));
        }

        private static double Sum(double[] v) { double s = 0; foreach (double x in v) { s += x; } return s; }

        private static double[] Normalised(double[] v)
        {
            double sum = Sum(v);
            var r = new double[v.Length];
            for (int i = 0; i < r.Length; i++) { r[i] = v[i] / sum; }
            return r;
        }

        private static int Assert(StringBuilder sb, string label, bool condition, string detail)
        {
            sb.Append($"  {(condition ? "ok  " : "FAIL")} {label}: {detail}\n");
            return condition ? 0 : 1;
        }
    }
}
