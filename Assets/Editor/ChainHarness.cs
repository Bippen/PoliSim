using System;
using PoliSim.Elections;
using PoliSim.Simulation;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// SPEC §8 / §26 / §27 unit harness — the chain layers, on SYNTHETIC vectors. Deterministic
    /// throughout: every random draw comes from an explicitly seeded `System.Random`, so the
    /// whole harness replays identically.
    ///
    /// THE ENUMERATION (rule 14 — the coverage claim, stated so it can be checked):
    ///
    /// §8 loyalty damping
    ///  1. loyalty 100 → preference IS the prior (nothing persuades a wall)
    ///  2. loyalty 0 → preference IS the compatibility-implied share
    ///  3. loyalty 50 → the exact midpoint of the two
    ///  4. monotone: raising loyalty moves preference monotonically toward the prior
    ///  5. **the measured defect, fixed** — a party with strong compatibility but NO prior support
    ///     (the empty-quadrant case that over-predicted BSW by +10.2 pp and TD by +15.9 pp on
    ///     Day-1) receives strictly less at high loyalty than at low, and a large incumbent party
    ///     keeps more; this is the assertion that §8 addresses what the measurement found
    ///  6. shares always sum to 1
    ///
    /// §26 turnout
    ///  7. all attributes neutral (50) → turnout is exactly the base rate
    ///  8. all attributes maximal → turnout is base × the product of the four spans
    ///  9. monotone in campaign mobilisation
    /// 10. clamped into [0,1] even with an absurd base
    ///
    /// §27 election day
    /// 11. one region, one group → votes equal population × eligible × turnout × preference
    /// 12. two regions aggregate additively, and national shares are vote-weighted (NOT the mean
    ///     of regional shares — the trap that would let a tiny region outvote a huge one)
    /// 13. noise is deterministic under a seed: same seed, identical output
    /// 14. noise preserves the simplex (non-negative, sums to 1)
    /// 15. **regional noise partially cancels nationally** — across 400 replays the national
    ///     standard deviation is strictly smaller than the regional one, which is the property
    ///     that makes national polling more accurate than constituency forecasting
    ///
    /// Run: `Unity.exe -batchmode -nographics -projectPath &lt;path&gt; -executeMethod
    /// PoliSim.EditorTools.ChainHarness.Run -logFile &lt;path&gt;`.
    /// </summary>
    public static class ChainHarness
    {
        public static void Run()
        {
            int failures = 0;

            // ---------------- §8 loyalty damping ----------------
            double[] compatibility = { 80.0, 60.0, 40.0 };
            double[] prior = { 0.20, 0.50, 0.30 };
            double[] persuaded = PreferenceModel.PersuadedShares(compatibility);

            double[] atFull = PreferenceModel.Preference(compatibility, prior, 100.0);
            failures += NearVector("1. loyalty 100 -> preference is the prior", atFull, prior);

            double[] atZero = PreferenceModel.Preference(compatibility, prior, 0.0);
            failures += NearVector("2. loyalty 0 -> preference is the persuaded share", atZero, persuaded);

            double[] atHalf = PreferenceModel.Preference(compatibility, prior, 50.0);
            var midpoint = new double[3];
            for (int i = 0; i < 3; i++) { midpoint[i] = 0.5 * prior[i] + 0.5 * persuaded[i]; }
            failures += NearVector("3. loyalty 50 -> the midpoint", atHalf, midpoint);

            bool monotone = true;
            double previousGap = double.PositiveInfinity;
            for (int step = 0; step <= 10; step++)
            {
                double[] p = PreferenceModel.Preference(compatibility, prior, step * 10.0);
                double gap = 0.0;
                for (int i = 0; i < 3; i++) { gap += Math.Abs(p[i] - prior[i]); }
                if (gap > previousGap + 1e-9) { monotone = false; }
                previousGap = gap;
            }

            failures += Assert("4. monotone: more loyalty -> closer to the prior", monotone, "11-step sweep");

            // 5. The Day-1 defect, as an assertion.
            //    Party 0 = a newcomer in an empty quadrant: high compatibility, no prior support.
            //    Party 1 = a large incumbent: moderate compatibility, most of the prior vote.
            double[] newcomerCase = { 85.0, 55.0, 45.0 };
            double[] incumbentPrior = { 0.02, 0.60, 0.38 };
            double[] lowLoyalty = PreferenceModel.Preference(newcomerCase, incumbentPrior, 15.0);
            double[] highLoyalty = PreferenceModel.Preference(newcomerCase, incumbentPrior, 85.0);
            failures += Assert("5a. empty-quadrant newcomer is damped by loyalty (the BSW/TD over-prediction)",
                highLoyalty[0] < lowLoyalty[0] - 1e-9,
                $"loyalty 85 -> {100.0 * highLoyalty[0]:F2}% vs loyalty 15 -> {100.0 * lowLoyalty[0]:F2}%");
            failures += Assert("5b. large incumbent is held up by loyalty (the CDU/KO under-prediction)",
                highLoyalty[1] > lowLoyalty[1] + 1e-9,
                $"loyalty 85 -> {100.0 * highLoyalty[1]:F2}% vs loyalty 15 -> {100.0 * lowLoyalty[1]:F2}%");

            failures += Near("6. shares sum to 1", Sum(atHalf), 1.0);

            // ---------------- §26 turnout ----------------
            failures += Near("7. neutral attributes -> the base rate", TurnoutModel.Turnout(0.70, 50, 50, 50, 50), 0.70);

            double expectedMax = 0.70 * (1 + TurnoutModel.EngagementSpan) * (1 + TurnoutModel.MobilizationSpan)
                                      * (1 + TurnoutModel.EnthusiasmSpan) * (1 + TurnoutModel.SalienceSpan);
            failures += Near("8. maximal attributes -> base x the four spans",
                TurnoutModel.Turnout(0.70, 100, 100, 100, 100), Math.Min(1.0, expectedMax));

            bool turnoutMonotone = true;
            double previousRate = -1.0;
            for (int step = 0; step <= 10; step++)
            {
                double rate = TurnoutModel.Turnout(0.60, 50, step * 10.0, 50, 50);
                if (rate < previousRate - 1e-12) { turnoutMonotone = false; }
                previousRate = rate;
            }

            failures += Assert("9. monotone in campaign mobilisation", turnoutMonotone, "11-step sweep");
            failures += Near("10. clamped at 1 with an absurd base", TurnoutModel.Turnout(0.99, 100, 100, 100, 100), 1.0);

            // ---------------- §27 election day ----------------
            var groups = new[]
            {
                new VoterGroupProfile("only group", 1.0, 70, IssueVector.Uniform(50), IssueVector.Uniform(50),
                    IdeologyVector.Uniform(50), partyLoyalty: 50, politicalEngagement: 50),
            };
            var regionA = new RegionProfile("A", 1_000_000, 0.75, 10, new[] { 1.0 }, IssueVector.Uniform(50));
            var preferences = new[] { new[] { 0.5, 0.3, 0.2 } };
            var turnout = new[] { 0.60 };

            RegionalAggregation.RegionResult resultA =
                RegionalAggregation.Region(regionA, groups, preferences, turnout, 3);
            double expectedCast = 1_000_000 * 0.75 * 1.0 * 0.60;
            failures += Near("11a. votes cast = population x eligible x turnout", resultA.VotesCast, expectedCast);
            failures += Near("11b. party 0 votes = cast x preference", resultA.Votes[0], expectedCast * 0.5);

            var regionB = new RegionProfile("B", 200_000, 0.75, 2, new[] { 1.0 }, IssueVector.Uniform(50));
            RegionalAggregation.RegionResult resultB =
                RegionalAggregation.Region(regionB, groups, new[] { new[] { 0.1, 0.1, 0.8 } }, turnout, 3);
            double[] national = RegionalAggregation.NationalShares(new[] { resultA, resultB }, 3);
            double totalVotes = resultA.VotesCast + resultB.VotesCast;
            double expectedParty0 = (resultA.Votes[0] + resultB.Votes[0]) / totalVotes;
            failures += Near("12a. national shares are vote-weighted, not a mean of regions", national[0], expectedParty0);
            failures += Assert("12b. the big region dominates (0.5/0.1 split -> national well above 0.3)",
                national[0] > 0.40, $"national party 0 = {100.0 * national[0]:F2}%");

            double[] shares = { 0.40, 0.35, 0.25 };
            double[] noisy1 = RegionalAggregation.ApplyNoise(shares, new System.Random(4242));
            double[] noisy2 = RegionalAggregation.ApplyNoise(shares, new System.Random(4242));
            failures += NearVector("13. noise is deterministic under a seed", noisy1, noisy2);
            failures += Near("14a. noisy shares still sum to 1", Sum(noisy1), 1.0);
            failures += Assert("14b. noisy shares stay non-negative", noisy1[0] >= 0 && noisy1[1] >= 0 && noisy1[2] >= 0, "3 parties");

            // 15. Regional noise partially cancels nationally.
            const int trials = 400;
            var rng = new System.Random(20260829);
            double regionalSum = 0, regionalSumSq = 0, nationalSum = 0, nationalSumSq = 0;
            for (int t = 0; t < trials; t++)
            {
                double regionTotal = 0.0;
                double[] first = null;
                var perRegion = new double[8][];
                for (int r = 0; r < 8; r++)
                {
                    perRegion[r] = RegionalAggregation.ApplyNoise(shares, rng);
                    if (first == null) { first = perRegion[r]; }
                    regionTotal += perRegion[r][0];
                }

                regionalSum += first[0];
                regionalSumSq += first[0] * first[0];
                double nationalShare = regionTotal / 8.0;   // equal-sized regions
                nationalSum += nationalShare;
                nationalSumSq += nationalShare * nationalShare;
            }

            double regionalSd = Math.Sqrt(regionalSumSq / trials - Math.Pow(regionalSum / trials, 2));
            double nationalSd = Math.Sqrt(nationalSumSq / trials - Math.Pow(nationalSum / trials, 2));
            failures += Assert("15. regional noise partially cancels nationally",
                nationalSd < regionalSd,
                string.Format(System.Globalization.CultureInfo.InvariantCulture,
                    "regional sd {0:F4} pp vs national sd {1:F4} pp over {2} replays of 8 regions",
                    100.0 * regionalSd, 100.0 * nationalSd, trials));

            // The named stream exists and is isolated (R-N2: nothing in the live game draws from it).
            SimulationRandom.Seed(777);
            System.Random electionStream = SimulationRandom.For(SimulationRandom.Stream.ElectionNoise);
            failures += Assert("16. the ElectionNoise stream resolves and is its own sequence",
                electionStream != null && !ReferenceEquals(electionStream, SimulationRandom.For(SimulationRandom.Stream.Event)),
                "SimulationRandom.Stream.ElectionNoise");

            Debug.Log($"=== ChainHarness (spec §8 / §26 / §27): {(failures == 0 ? "ALL 20 ASSERTIONS PASS" : failures + " FAILED")} ===");
            CheckExit.Finish(failures == 0 ? 0 : 1);
        }

        private static double Sum(double[] values)
        {
            double total = 0.0;
            foreach (double v in values) { total += v; }
            return total;
        }

        private static int Near(string label, double actual, double expected)
        {
            bool ok = Math.Abs(actual - expected) < 1e-6 * Math.Max(1.0, Math.Abs(expected));
            Debug.Log(string.Format(System.Globalization.CultureInfo.InvariantCulture,
                "  {0} {1}: got {2:F6}, expected {3:F6}", ok ? "ok  " : "FAIL", label, actual, expected));
            return ok ? 0 : 1;
        }

        private static int NearVector(string label, double[] actual, double[] expected)
        {
            bool ok = actual.Length == expected.Length;
            for (int i = 0; ok && i < actual.Length; i++)
            {
                if (Math.Abs(actual[i] - expected[i]) > 1e-9) { ok = false; }
            }

            Debug.Log($"  {(ok ? "ok  " : "FAIL")} {label}: [{Format(actual)}] vs [{Format(expected)}]");
            return ok ? 0 : 1;
        }

        private static int Assert(string label, bool condition, string detail)
        {
            Debug.Log($"  {(condition ? "ok  " : "FAIL")} {label}: {detail}");
            return condition ? 0 : 1;
        }

        private static string Format(double[] values)
        {
            var parts = new string[values.Length];
            for (int i = 0; i < values.Length; i++)
            {
                parts[i] = values[i].ToString("F4", System.Globalization.CultureInfo.InvariantCulture);
            }

            return string.Join(",", parts);
        }
    }
}
