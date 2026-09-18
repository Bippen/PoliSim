using System.Collections.Generic;
using System.Globalization;
using System.Text;
using PoliSim.Data;
using UnityEngine;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// **P6-E1 (2026-09-17, `COMPLETED.md` §534): the credit's formula against the statute's own worked examples.** Skatteverket's
    /// *Teknisk beskrivning SKV 433 utgåva 35* (income year 2025) prints three worked computations of the credit at tax table 34,
    /// i.e. a municipal rate of 34 − 1.16 = 32.84 %, and two of the basic allowance; each is reproduced here to the krona, which is
    /// the rounding the statute itself prescribes. Beyond the examples: the credit never exceeds the municipal tax it is set off
    /// against, it is continuous where the intervals meet (the statute's own 0.001 PBB step at 5.24 for the 66-plus schedule is
    /// asserted as the statute writes it), and it is inert in the yield until its family is ruled - the seed's average effective
    /// rate is the same number with the hold on that it was before the credit existed.
    /// </summary>
    public static class EarnedIncomeCreditDiagnostic
    {
        private const double Table34 = 34 - 1.16;

        public static void Run()
        {
            CheckExit.ArmLogFold();
            var failures = new List<string>();
            var sb = new StringBuilder("=== EarnedIncomeCreditDiagnostic: jobbskatteavdraget against SKV 433's own examples ===\n");

            // The three worked examples of section 7.5.2. ⚠ The third is a seafarer's 250 000 with a 36 000 sea-income deduction: the
            // credit's schedule phases on the 214 000 that remains while the allowance is computed on the assessed 250 000 - the
            // document does exactly that, and the first form of this diagnostic computed both on 214 000 and missed it by 1 182 kr.
            Expect(failures, sb, "Exempel 1, 90 000 kr at table 34", EarnedIncomeCredit.Credit(90000, Table34, false), 11935);
            Expect(failures, sb, "Exempel 2, 240 000 kr at table 34", EarnedIncomeCredit.Credit(240000, Table34, false), 25238);
            Expect(failures, sb, "Exempel 3, 214 000 kr earned of 250 000 assessed, at table 34", EarnedIncomeCredit.Credit(214000, 250000, Table34, false), 23867);

            // The basic allowance the examples compute on the way (rounded up to a whole hundred, as the statute says).
            Expect(failures, sb, "grundavdrag at 90 000", EarnedIncomeCredit.BasicAllowance(90000), 31300);
            Expect(failures, sb, "grundavdrag at 240 000", EarnedIncomeCredit.BasicAllowance(240000), 39600);
            Expect(failures, sb, "grundavdrag at 250 000", EarnedIncomeCredit.BasicAllowance(250000), 38600);

            // The 66-plus schedule at its own interval ends, from the constants as written: 22 % of 1.75 PBB, then 0.2635 PBB + 7 % of it.
            double pbb = EarnedIncomeCredit.PriceBaseAmount;
            double at175 = EarnedIncomeCredit.Credit(1.75 * pbb, Table34, true);
            double at524 = EarnedIncomeCredit.Credit(5.24 * pbb, Table34, true);
            double above = EarnedIncomeCredit.Credit(20 * pbb, Table34, true);
            if (System.Math.Abs(at175 - System.Math.Floor(0.22 * 1.75 * pbb)) > 1) { failures.Add(F("66+: at 1.75 PBB the credit is {0}, not 22 % of the income", at175)); }
            if (System.Math.Abs(above - System.Math.Floor(0.6293 * pbb)) > 1) { failures.Add(F("66+: above 5.24 PBB the credit is {0}, not 0.6293 PBB", above)); }
            sb.Append(F("    66-plus: {0} kr at 1.75 PBB, {1} kr at 5.24 PBB, {2} kr above it (0.6293 PBB = {3})\n", at175, at524, above, System.Math.Floor(0.6293 * pbb)));

            // Never more than the municipal tax it is set off against, and never negative.
            foreach (double income in new[] { 1000.0, 20000.0, 53500.0, 100000.0, 400000.0, 1000000.0 })
            {
                foreach (bool turned66 in new[] { false, true })
                {
                    double c = EarnedIncomeCredit.Credit(income, Table34, turned66);
                    double municipalTax = Table34 / 100.0 * System.Math.Floor(income / 100.0) * 100.0;
                    if (c < 0 || c > municipalTax + 1e-6) { failures.Add(F("the credit at {0} kr ({1}) is {2}, outside 0..the municipal tax {3}", income, turned66 ? "66+" : "under 66", c, municipalTax)); }
                }
            }

            // Continuity at the under-66 interval ends: the two incomes are two hundred kronor apart, and below 0.91 PBB the base moves
            // krona for krona, so the credit may move by at most two hundred kronor's worth at the rate (plus the whole-krona rounding).
            foreach (double edge in new[] { 0.91 * pbb, 3.24 * pbb, 8.08 * pbb })
            {
                double below = EarnedIncomeCredit.Credit(edge - 100, Table34, false), atEdge = EarnedIncomeCredit.Credit(edge + 100, Table34, false);
                if (System.Math.Abs(atEdge - below) > 200 * Table34 / 100.0 + 2) { failures.Add(F("the under-66 schedule jumps by {0} kr across {1:F0} kr", atEdge - below, edge)); }
            }

            // Inert until ruled: the yield's rate is the rate without the credit, so the seed's average effective rate is what it was.
            World world = WorldFactory.CreateDefault();
            Country sweden = world.GetCountry(CountryId.Sweden);
            double without = TaxSchedule.AverageEffectiveRateWithoutCredit(sweden, 0.0, 1.0, 1.0);   // P6-E1's yield (§538): the yield path reads the credit now, so "without" is read past the hold
            double with = TaxSchedule.AverageEffectiveRateWithCredit(sweden, 0.0, 1.0, 1.0);
            double seedAer = sweden.IncomeTaxSeedAer;
            if (!EarnedIncomeCredit.Live) { failures.Add("the credit is held off the yield - it landed by ruling as the pass's family (§538, traj_p6e1)"); }
            if (System.Math.Abs((EarnedIncomeCredit.Live ? with : without) - seedAer) > 1e-4) { failures.Add(F("the seed's average effective rate moved: {0:F4} against the captured {1:F4}", EarnedIncomeCredit.Live ? with : without, seedAer)); }   // §538: the captured seed is the credited rate while the flag is on
            if (!(with < without)) { failures.Add(F("the credited rate ({0:F2}) is not below the rate without ({1:F2})", with, without)); }
            sb.Append(F("    Sweden at the seed: {0:F2} % without the credit, {2:F2} % with it (the yield's since §538, and the captured seed's {1:F2}) - the credit is worth {3:F2} points of the average effective rate\n", without, seedAer, with, without - with));

            foreach (string f in failures) { sb.Append("    ⚠ ").Append(f).Append('\n'); }
            sb.Append(failures.Count == 0 ? "    CLEAN - the statute's three examples to the krona, the allowance's rounding, the 66-plus ends, the cap, the continuity, the hold." : F("    {0} FAILURE(S).", failures.Count));
            if (failures.Count == 0) { Debug.Log(sb.ToString()); } else { Debug.LogError(sb.ToString()); }
            CheckExit.Finish(failures.Count == 0 ? 0 : 1);
        }

        private static void Expect(List<string> failures, StringBuilder sb, string what, double got, double expected)
        {
            bool ok = System.Math.Abs(got - expected) < 0.5;
            sb.Append(F("    {0}: {1} kr (the document's {2} kr){3}\n", what, got, expected, ok ? string.Empty : " - NOT THE DOCUMENT'S"));
            if (!ok) { failures.Add(F("{0}: {1} against the document's {2}", what, got, expected)); }
        }

        private static string F(string format, params object[] args) => string.Format(CultureInfo.InvariantCulture, format, args);
    }
}
