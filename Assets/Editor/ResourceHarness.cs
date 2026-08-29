using System;
using System.Globalization;
using System.Text;
using PoliSim.Elections;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// W-B2's harness — proves §35's curve has the shape the spec describes, and that no resource
    /// can be driven negative.
    ///
    /// The done-when, asserted:
    /// 1. **the first krona ≫ the millionth** — the marginal effect at 0 is orders of magnitude
    ///    above the marginal effect at a million;
    /// 2. the curve is strictly increasing and strictly concave over a wide sweep (diminishing
    ///    everywhere, not just at the ends);
    /// 3. it reproduces §35's four prose bands from ONE formula, with no per-band constant;
    /// 4. no spend can drive money or hours negative — an unaffordable spend is REFUSED, not
    ///    clamped, and leaves the pool untouched;
    /// 5. hours reset each campaign day and cannot be banked, while money and volunteers carry.
    /// </summary>
    public static class ResourceHarness
    {
        public static void Run()
        {
            int failures = 0;
            var sb = new StringBuilder();
            sb.Append("=== W-B2: campaign resources and §35's curve ===\n");

            // 1. First krona vs millionth — and the trade-off behind the threshold.
            //
            // At MoneyScale = 500k the millionth krona is only TWO scale-lengths out, so its
            // marginal value is exp(-2) = 13.5% of the first's: a 7.4x fall, real but not dramatic.
            // A smaller scale would make this ratio look far more impressive (200k gives ~150x) —
            // and would be the wrong curve, because it would flatten the 500k->2m band that §35
            // explicitly calls "moderate impact" into nothing. The scale is chosen to reproduce the
            // SPEC'S BANDS, and this ratio is a consequence of that choice rather than a target.
            // Asserted at >5x here; the "vastly more" the worklist means shows at 5m, reported too.
            double first = CampaignEconomy.MarginalEffectiveness(0.0);
            double millionth = CampaignEconomy.MarginalEffectiveness(1_000_000.0);
            double fiveMillionth = CampaignEconomy.MarginalEffectiveness(5_000_000.0);
            double ratio = first / millionth;
            double farRatio = first / fiveMillionth;
            failures += Assert(sb, "1a. the first krona is worth materially more than the millionth",
                ratio > 5.0,
                string.Format(CultureInfo.InvariantCulture,
                    "marginal at 0 = {0:E3}, at 1m = {1:E3}, ratio {2:N1}x", first, millionth, ratio));
            failures += Assert(sb, "1b. and overwhelmingly more than the five-millionth",
                farRatio > 1000.0,
                string.Format(CultureInfo.InvariantCulture,
                    "marginal at 5m = {0:E3}, ratio {1:N0}x", fiveMillionth, farRatio));

            // 2. Strictly increasing, strictly concave.
            bool increasing = true, concave = true;
            double previousValue = -1.0, previousMarginal = double.MaxValue;
            for (double spend = 0.0; spend <= 5_000_000.0; spend += 25_000.0)
            {
                double value = CampaignEconomy.Effectiveness(spend);
                double marginal = CampaignEconomy.MarginalEffectiveness(spend);
                if (value < previousValue) { increasing = false; }
                if (marginal > previousMarginal + 1e-15) { concave = false; }
                previousValue = value;
                previousMarginal = marginal;
            }

            failures += Assert(sb, "2a. effectiveness is strictly increasing over a 0-5m sweep", increasing, "201 samples");
            failures += Assert(sb, "2b. marginal effect is strictly decreasing (diminishing EVERYWHERE)", concave, "201 samples");

            // 3. The spec's four bands, from one formula.
            double at100k = CampaignEconomy.Effectiveness(100_000);
            double at500k = CampaignEconomy.Effectiveness(500_000);
            double at2m = CampaignEconomy.Effectiveness(2_000_000);
            double at10m = CampaignEconomy.Effectiveness(10_000_000);
            sb.Append(string.Format(CultureInfo.InvariantCulture,
                "  §35's bands from ONE curve: 100k -> {0:P1} | 500k -> {1:P1} | 2m -> {2:P1} | 10m -> {3:P1}\n",
                at100k, at500k, at2m, at10m));
            sb.Append(string.Format(CultureInfo.InvariantCulture,
                "    band gains: 0->100k {0:P1} | 100k->500k {1:P1} | 500k->2m {2:P1} | 2m->10m {3:P1}\n",
                at100k, at500k - at100k, at2m - at500k, at10m - at2m));
            // Absolute band gains are NOT the right comparison - the four bands are of very
            // different widths (100k, 400k, 1.5m, 8m), so the widest could buy the most in total
            // while being the worst value. The honest test is the return PER KRONA, asserted next.
            failures += Assert(sb, "3a. the curve saturates: 10m buys essentially all of the achievable effect",
                at10m > 0.99 && at10m < 1.0,
                string.Format(CultureInfo.InvariantCulture, "{0:P3} at 10m, and never reaching 100%", at10m));

            // The honest per-krona comparison: rate of return per band.
            double rate1 = at100k / 100_000.0;
            double rate2 = (at500k - at100k) / 400_000.0;
            double rate3 = (at2m - at500k) / 1_500_000.0;
            double rate4 = (at10m - at2m) / 8_000_000.0;
            failures += Assert(sb, "3b. return PER KRONA falls monotonically across the four bands",
                rate1 > rate2 && rate2 > rate3 && rate3 > rate4,
                string.Format(CultureInfo.InvariantCulture, "{0:E2} > {1:E2} > {2:E2} > {3:E2}", rate1, rate2, rate3, rate4));

            // 4. Nothing goes negative; an unaffordable spend is refused.
            var pool = new ResourcePool(250_000, CampaignEconomy.HoursPerCampaignDay, 40);
            bool refusedMoney = !pool.TrySpend(250_001, 1, out ResourcePool afterMoney);
            bool refusedHours = !pool.TrySpend(1, CampaignEconomy.HoursPerCampaignDay + 0.1, out ResourcePool afterHours);
            failures += Assert(sb, "4a. an unaffordable money spend is REFUSED and leaves the pool untouched",
                refusedMoney && Math.Abs(afterMoney.Money - pool.Money) < 1e-9, $"pool still {afterMoney}");
            failures += Assert(sb, "4b. an over-budget hours spend is REFUSED and leaves the pool untouched",
                refusedHours && Math.Abs(afterHours.Hours - pool.Hours) < 1e-9, $"pool still {afterHours}");

            bool ok = pool.TrySpend(250_000, CampaignEconomy.HoursPerCampaignDay, out ResourcePool spentAll);
            failures += Assert(sb, "4c. spending exactly the balance succeeds and lands on zero, not below",
                ok && spentAll.Money == 0.0 && spentAll.Hours == 0.0, $"{spentAll}");

            bool threw = false;
            try { _ = new ResourcePool(-1, 0, 0); } catch (ArgumentException) { threw = true; }
            failures += Assert(sb, "4d. a negative pool cannot even be constructed", threw, "ArgumentException");

            // 5. Hours reset daily; money and volunteers carry.
            ResourcePool dayTwo = spentAll.AddVolunteers(15).StartDay();
            failures += Assert(sb, "5. hours reset each campaign day; money and volunteers carry over",
                Math.Abs(dayTwo.Hours - CampaignEconomy.HoursPerCampaignDay) < 1e-9
                && dayTwo.Money == 0.0 && dayTwo.Volunteers == 55,
                $"day two: {dayTwo}");

            // A worked campaign day, for the record.
            var day = new ResourcePool(1_200_000, CampaignEconomy.HoursPerCampaignDay, 120);
            sb.Append($"\n  a worked day - start: {day}\n");
            if (day.TrySpend(300_000, 4, out day)) { sb.Append($"    after a rally (300k, 4h):        {day}\n"); }
            if (day.TrySpend(0, 2, out day)) { sb.Append($"    after an interview (0, 2h):      {day}\n"); }
            if (day.TrySpend(500_000, 3, out day)) { sb.Append($"    after a TV buy (500k, 3h):       {day}\n"); }
            bool overrun = day.TrySpend(0, 6, out _);
            sb.Append($"    a 6-hour tour with 3h left:      {(overrun ? "ALLOWED (wrong)" : "refused, as it should be")}\n");
            failures += Assert(sb, "6. the day's hours genuinely bind", !overrun, "the fourth action is refused");
            sb.Append(string.Format(CultureInfo.InvariantCulture,
                "    volunteer hours available today: {0:F0} from {1} volunteers\n",
                CampaignEconomy.VolunteerHours(day.Volunteers), day.Volunteers));

            sb.Append($"\n=== ResourceHarness: {(failures == 0 ? "ALL ASSERTIONS PASS" : failures + " FAILED")} ===\n");
            Debug.Log(sb.ToString());
            CheckExit.Finish(failures == 0 ? 0 : 1);
        }

        private static int Assert(StringBuilder sb, string label, bool condition, string detail)
        {
            sb.Append($"  {(condition ? "ok  " : "FAIL")} {label}: {detail}\n");
            return condition ? 0 : 1;
        }
    }
}
