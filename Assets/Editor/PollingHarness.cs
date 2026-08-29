using System;
using System.Globalization;
using System.Reflection;
using System.Text;
using PoliSim.Elections;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// W-B10's harness — §20–§22.
    ///
    /// The done-when, asserted:
    /// 1. **a poll never exactly equals the truth** — over many replays, exact agreement is
    ///    vanishingly rare rather than the common case;
    /// 2. **the margin of error is HONEST** — tested by coverage, the only test that means
    ///    anything: over 2 000 replays of an unbiased pollster, the true share must fall inside the
    ///    reported ± about 95 % of the time. Too low and the poll lies about its precision; too
    ///    high and it is uselessly wide;
    /// 3. **a house effect does NOT wash out with sample size** — the systematic lean survives a
    ///    tenfold larger sample, while the sampling error shrinks by √10;
    /// 4. **the Poll object cannot carry the truth** — reflection finds no member that could;
    /// 5. **internal polling buys precision** (§21) — a larger paid sample has a materially
    ///    narrower MoE, so the purchase decision is real;
    /// 6. **§22's momentum decays on its half-life** and reproduces the spec's own worked example
    ///    (+2.0 → ~+1.4 after several days → ~+0.4 after two weeks → ~0 after a month).
    /// </summary>
    public static class PollingHarness
    {
        public static void Run()
        {
            int failures = 0;
            var sb = new StringBuilder();
            sb.Append("=== W-B10: polling (§20-§22) ===\n");

            double[] truth = { 0.3080, 0.2086, 0.1940, 0.0686, 0.0681, 0.0542, 0.0516, 0.0468 };
            var unbiased = new PollingHouse("Neutral Research", 1000, 120_000, new double[truth.Length]);

            // ---------- 4. structural: no truth in a Poll ----------
            var forbidden = new[] { "truth", "true", "actual", "underlying", "real" };
            var offenders = new StringBuilder();
            foreach (MemberInfo m in typeof(Poll).GetMembers(BindingFlags.Public | BindingFlags.Instance))
            {
                string lower = m.Name.ToLowerInvariant();
                foreach (string bad in forbidden)
                {
                    if (lower.Contains(bad) && m.Name != "GetType") { offenders.Append(m.Name).Append(' '); }
                }
            }

            failures += Assert(sb, "4. a Poll cannot carry the truth (no truth/actual/underlying member)",
                offenders.Length == 0, offenders.Length == 0 ? "clean" : $"offenders: {offenders}");

            // ---------- 1 + 2. exactness and coverage ----------
            const int replays = 2000;
            var rng = new System.Random(20260829);
            int exact = 0;
            var covered = new int[truth.Length];
            double moeSum = 0.0;

            for (int r = 0; r < replays; r++)
            {
                Poll poll = PollingSystem.Conduct(truth, unbiased, new DateTime(2026, 8, 1), rng);
                bool allExact = true;
                for (int i = 0; i < truth.Length; i++)
                {
                    if (Math.Abs(poll.Share(i) - truth[i]) > 1e-12) { allExact = false; }
                    if (poll.Covers(i, truth[i])) { covered[i]++; }
                }

                if (allExact) { exact++; }
                moeSum += poll.MarginOfErrorPp(0);
            }

            failures += Assert(sb, "1. a poll essentially never equals the truth exactly",
                exact == 0, $"{exact} of {replays} replays matched exactly");

            double totalCoverage = 0.0;
            sb.Append("  coverage of the reported 95% interval, per party, over 2 000 replays:\n");
            for (int i = 0; i < truth.Length; i++)
            {
                double pct = 100.0 * covered[i] / replays;
                totalCoverage += pct;
                sb.Append(string.Format(CultureInfo.InvariantCulture,
                    "    party {0}: true {1,6:P2}  covered {2,6:F2}% of replays\n", i, truth[i], pct));
            }

            double meanCoverage = totalCoverage / truth.Length;
            failures += Assert(sb, "2. the margin of error is HONEST (mean coverage near the nominal 95%)",
                meanCoverage >= 93.0 && meanCoverage <= 97.0,
                string.Format(CultureInfo.InvariantCulture, "mean coverage {0:F2}%, mean MoE on party 0 = ±{1:F2} pp",
                    meanCoverage, moeSum / replays));

            // ---------- 3. house effects do not wash out ----------
            var leaning = new PollingHouse("Leaning Institute", 1000, 120_000,
                new[] { 2.5, -2.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0 });
            var leaningBig = new PollingHouse("Leaning Institute XL", 10_000, 400_000,
                new[] { 2.5, -2.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0 });

            double biasSmall = MeanBias(truth, leaning, 400, 11);
            double biasBig = MeanBias(truth, leaningBig, 400, 12);
            double spreadSmall = MeanMoe(truth, leaning, 50, 13);
            double spreadBig = MeanMoe(truth, leaningBig, 50, 14);

            failures += Assert(sb, "3a. a house's systematic lean survives a 10x larger sample",
                Math.Abs(biasBig - biasSmall) < 0.5 && biasBig > 1.5,
                string.Format(CultureInfo.InvariantCulture,
                    "bias on party 0: {0:F2} pp at n=1000, {1:F2} pp at n=10000", biasSmall, biasBig));
            failures += Assert(sb, "3b. while sampling precision improves by about √10",
                spreadSmall / spreadBig > 2.5 && spreadSmall / spreadBig < 4.0,
                string.Format(CultureInfo.InvariantCulture,
                    "MoE ±{0:F2} pp vs ±{1:F2} pp, ratio {2:F2} (√10 = 3.16)", spreadSmall, spreadBig, spreadSmall / spreadBig));

            // ---------- 5. §21 internal polling buys precision ----------
            var cheap = new PollingHouse("Public tracker", 600, 40_000, new double[truth.Length]);
            var premium = new PollingHouse("Internal, full sample", 4000, 350_000, new double[truth.Length], isInternal: true);
            double moeCheap = PollingSystem.MarginOfErrorPp(0.30, cheap.SampleSize);
            double moePremium = PollingSystem.MarginOfErrorPp(0.30, premium.SampleSize);
            failures += Assert(sb, "5. §21 - paid internal polling is materially more precise",
                moePremium < moeCheap * 0.5,
                string.Format(CultureInfo.InvariantCulture,
                    "±{0:F2} pp for {1:N0} kr vs ±{2:F2} pp for {3:N0} kr",
                    moeCheap, cheap.Cost, moePremium, premium.Cost));

            // ---------- 6. §22 momentum and its decay ----------
            var momentum = new MomentumTracker(truth.Length);
            momentum.AddShock(0, 2.0);
            double atStart = momentum.MomentumPp(0);
            momentum.Advance(5);
            double afterFiveDays = momentum.MomentumPp(0);
            momentum.Advance(9);
            double afterTwoWeeks = momentum.MomentumPp(0);
            momentum.Advance(16);
            double afterAMonth = momentum.MomentumPp(0);

            sb.Append(string.Format(CultureInfo.InvariantCulture,
                "  §22's worked example: shock +{0:F1} -> {1:F2} after 5 days -> {2:F2} after two weeks -> {3:F2} after a month\n",
                atStart, afterFiveDays, afterTwoWeeks, afterAMonth));
            // §22's three illustrative points imply THREE DIFFERENT half-lives (~9.7, ~6.0, ~5.3
            // days) and so cannot all be hit by one exponential - see MomentumHalfLifeDays' doc.
            // What is asserted is the SHAPE the spec actually requires: monotone decay, materially
            // reduced within a fortnight, substantially gone within a month.
            failures += Assert(sb, "6. momentum decays monotonically and is substantially gone within a month",
                afterFiveDays < atStart && afterTwoWeeks < afterFiveDays && afterAMonth < afterTwoWeeks
                && afterFiveDays > 1.0 && afterFiveDays < 1.5
                && afterTwoWeeks < 0.6
                && afterAMonth < 0.15,
                string.Format(CultureInfo.InvariantCulture,
                    "+{0:F1} -> {1:F2} -> {2:F2} -> {3:F2} (spec's illustrative ~1.4 / ~0.4 / ~0.0 are mutually inconsistent)",
                    atStart, afterFiveDays, afterTwoWeeks, afterAMonth));

            // Momentum moves the APPEARANCE without touching the underlying preference.
            var fresh = new MomentumTracker(truth.Length);
            fresh.AddShock(0, 3.0);
            double[] shown = fresh.Apply(truth);
            failures += Assert(sb, "6b. momentum shifts the visible race while the underlying vector is untouched",
                shown[0] > truth[0] && Math.Abs(truth[0] - 0.3080) < 1e-12,
                string.Format(CultureInfo.InvariantCulture, "shown {0:P2} vs underlying {1:P2}", shown[0], truth[0]));

            sb.Append($"\n=== PollingHarness: {(failures == 0 ? "ALL ASSERTIONS PASS" : failures + " FAILED")} ===\n");
            Debug.Log(sb.ToString());
            CheckExit.Finish(failures == 0 ? 0 : 1);
        }

        private static double MeanBias(double[] truth, PollingHouse house, int replays, int seed)
        {
            var rng = new System.Random(seed);
            double sum = 0.0;
            for (int r = 0; r < replays; r++)
            {
                Poll poll = PollingSystem.Conduct(truth, house, new DateTime(2026, 8, 1), rng);
                sum += (poll.Share(0) - truth[0]) * 100.0;
            }

            return sum / replays;
        }

        private static double MeanMoe(double[] truth, PollingHouse house, int replays, int seed)
        {
            var rng = new System.Random(seed);
            double sum = 0.0;
            for (int r = 0; r < replays; r++)
            {
                Poll poll = PollingSystem.Conduct(truth, house, new DateTime(2026, 8, 1), rng);
                sum += poll.MarginOfErrorPp(0);
            }

            return sum / replays;
        }

        private static int Assert(StringBuilder sb, string label, bool condition, string detail)
        {
            sb.Append($"  {(condition ? "ok  " : "FAIL")} {label}: {detail}\n");
            return condition ? 0 : 1;
        }
    }
}
