using System;
using System.Text;
using PoliSim.Elections;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// E-3, the measure-first anchor (overnight 2026-08-28→29): SOURCED real returns through the
    /// vote-to-seat layer, compared against the REAL chambers — campaigns OFF, no tuning, zero
    /// free parameters (every input below is a cited figure from `ElectionsData/`). A deviation
    /// is a FINDING, not a failure: the run exits 0 whenever the harness itself ran; the tables
    /// are the result. This run IS the "port to C# and reproduce, re-derive from scratch" that
    /// `POLISIM_SEED_DATA_MACRO_OVERHAUL.md` Part 5 requires before the allocator claims are
    /// relied on.
    ///
    /// WHAT RUNS, and what deliberately does NOT (the morning report's scope calls):
    /// - SWEDEN 2022: exact national counts (val.se RD_S.json), 4 % threshold, modified
    ///   Sainte-Laguë 1.2, 349 — the recorded expectation is EXACT (the adjustment seats exist
    ///   to make the national result proportional).
    /// - GERMANY 2025: three-decimal official shares as scaled integer votes (allocation is
    ///   ratio-invariant), 5 % threshold with SSW exempt (BSW 4.981 and FDP 4.328 out), 630
    ///   Sainte-Laguë/Schepers — the recorded band is off-by-≈1 at published-share precision
    ///   (kerg2.csv exact counts are billed; the share-precision caveat prints with the table).
    /// - POLAND 2023 SIGNATURE: national d'Hondt over the exact national counts — DELIBERATELY
    ///   the wrong system (the real Sejm is 41 districts), run to re-derive the recorded
    ///   ~70-seat national-vs-district signature from scratch. Agreement with the recorded
    ///   figures (PiS 169, Konfederacja 34) confirms both this allocator and the branch-side
    ///   claim without inspecting the branch.
    /// - ITALY: NOT run — the Rosatellum's proportional allocation FORMULA was not sourced
    ///   tonight (thresholds and structure were; the formula is billed), and this harness does
    ///   not run un-sourced arithmetic. FRANCE: NOT run — two-round SMD has no national model
    ///   by construction. USA: NOT run — the full 51-state table was not fetched (12 states
    ///   landed). Each stated here so silence cannot read as coverage.
    /// - SYNTHETIC vectors first: the divisor-decisive case (A=100, B=22, 3 seats: pure
    ///   Sainte-Laguë gives B a seat, the 1.2 modification takes it away) and a
    ///   d'Hondt-vs-Sainte-Laguë divergence case (100/80/30 × 4) — the first-divisor coverage
    ///   the python original built synthetically because no real Swedish election exercises it.
    ///
    /// Run: `Unity.exe -batchmode -nographics -projectPath &lt;path&gt; -executeMethod
    /// PoliSim.EditorTools.SeatAllocationBacktest.Run -logFile &lt;path&gt;`.
    /// InvariantCulture on every printed number (B3).
    /// </summary>
    public static class SeatAllocationBacktest
    {
        public static void Run()
        {
            int failures = 0;

            // --- Synthetic vectors (stated enumeration: 2 cases x every method involved) ---
            int[] pure = SeatAllocation.HighestAverages(new long[] { 100, 22 }, 3, SeatAllocation.SainteLagueDivisor);
            int[] modified = SeatAllocation.HighestAverages(new long[] { 100, 22 }, 3, SeatAllocation.ModifiedSainteLagueDivisor);
            failures += Expect("synthetic divisor-decisive, pure S-L", pure, new[] { 2, 1 });
            failures += Expect("synthetic divisor-decisive, modified 1.2", modified, new[] { 3, 0 });

            int[] dh = SeatAllocation.HighestAverages(new long[] { 100, 80, 30 }, 4, SeatAllocation.DHondtDivisor);
            int[] sl = SeatAllocation.HighestAverages(new long[] { 100, 80, 30 }, 4, SeatAllocation.SainteLagueDivisor);
            failures += Expect("synthetic divergence, d'Hondt", dh, new[] { 2, 2, 0 });
            failures += Expect("synthetic divergence, S-L", sl, new[] { 2, 1, 1 });

            // Per-district vs national divergence, synthetic: 2 districts x 3 seats each vs 1x6.
            var districts = new[] { new long[] { 60, 25, 15 }, new long[] { 55, 30, 15 } };
            int[] perDistrict = SeatAllocation.PerDistrictSum(districts, new[] { 3, 3 }, new[] { true, true, true }, SeatAllocation.DHondtDivisor);
            int[] national = SeatAllocation.HighestAverages(new long[] { 115, 55, 30 }, 6, SeatAllocation.DHondtDivisor);
            Debug.Log($"BACKTEST: synthetic per-district [{Join(perDistrict)}] vs national [{Join(national)}] - the structural gap the Poland signature rides");

            // --- SWEDEN 2022 (ElectionsData/sweden/returns_2022.md: exact counts, slutlig) ---
            string[] seNames = { "S", "SD", "M", "V", "C", "KD", "MP", "L" };
            long[] seVotes = { 1964474, 1330325, 1237428, 437050, 434945, 345712, 329242, 298542 };
            int[] seReal = { 107, 73, 68, 24, 24, 19, 18, 16 };
            const long seValid = 6477970;
            int[] seModel = SeatAllocation.AllocateWithThreshold(seVotes, seValid, 0.04, 349, SeatAllocation.ModifiedSainteLagueDivisor);
            failures += Report("SWEDEN 2022 - national modified Sainte-Lague 1.2, 349 seats, 4% threshold (expected EXACT)", seNames, seVotes, seValid, seModel, seReal);

            // --- GERMANY 2025 (ElectionsData/germany/returns_2025.md: official 3-decimal shares
            // as scaled votes x1000; SSW exempt; BSW 4.981 / FDP 4.328 below 5%) ---
            string[] deNames = { "CDU", "AfD", "SPD", "Grune", "Linke", "CSU", "SSW", "BSW", "FDP" };
            long[] deVotes = { 22551, 20803, 16413, 11606, 8775, 5970, 152, 4981, 4328 }; // shares x1000; SSW = 0.152% (0.2 published, 0.152 implied by seat 1 of 630 - see caveat print below)
            bool[] deExempt = { false, false, false, false, false, false, true, false, false };
            int[] deReal = { 164, 152, 120, 85, 64, 44, 1, 0, 0 };
            const long deValid = 100000; // shares are per-mille of 100%; threshold arithmetic on the same scale
            int[] deModel = SeatAllocation.AllocateWithThreshold(deVotes, deValid, 0.05, 630, SeatAllocation.SainteLagueDivisor, deExempt);
            Debug.Log("BACKTEST: Germany caveat - inputs are the official THREE-DECIMAL shares (kerg2.csv exact counts billed); SSW's share entered at its seat-implied 0.152% because the published 0.2% is one-decimal rounding of 0.152 (its exact count is in the billed CSV); the recorded precision band expects off-by-about-1.");
            failures += Report("GERMANY 2025 - national Sainte-Lague/Schepers, 630 seats, 5% threshold + SSW exemption (expected off-by-~1 at share precision)", deNames, deVotes, deValid, deModel, deReal);

            // --- POLAND 2023 SIGNATURE (ElectionsData/poland/returns_2023.md: exact counts;
            // NATIONAL d'Hondt = deliberately the wrong system, re-deriving the recorded gap) ---
            string[] plNames = { "PiS", "KO", "TD", "NL", "Konf", "MN" };
            long[] plVotes = { 7640854, 6629402, 3110670, 1859018, 1547364, 25778 };
            bool[] plExempt = { false, false, false, false, false, true };
            int[] plReal = { 194, 157, 65, 26, 18, 0 };
            const long plValid = 21596674;
            // Thresholds: PiS/NL/Konf party 5%; KO/TD registered as coalition committees, 8%
            // (both clear it); MN exempt (art. 197 s1). Party-vs-coalition is per committee:
            long[] plMasked = (long[])plVotes.Clone();
            for (int i = 0; i < plVotes.Length; i++)
            {
                double share = (double)plVotes[i] / plValid;
                bool coalition = i == 1 || i == 2; // KO, TD
                bool eligible = plExempt[i] || share >= (coalition ? 0.08 : 0.05);
                if (!eligible) { plMasked[i] = 0; }
            }

            int[] plModel = SeatAllocation.HighestAverages(plMasked, 460, SeatAllocation.DHondtDivisor);
            failures += Report("POLAND 2023 SIGNATURE - NATIONAL d'Hondt (the WRONG system on purpose; the real Sejm is 41-district d'Hondt - recorded signature PiS 169, Konf 34)", plNames, plVotes, plValid, plModel, plReal);

            Debug.Log("BACKTEST: NOT RUN, stated: Italy (allocation formula unsourced tonight - billed), France (two-round SMD, no national model exists), USA (full state table not fetched). Poland's REAL 41-district allocation awaits the billed per-district absolute counts; PerDistrictSum is ready for them.");
            Debug.Log($"=== SeatAllocationBacktest: synthetic {(failures == 0 ? "ALL PASS" : failures + " FAILED")}; the country tables above are FINDINGS (deviations reported, not asserted) ===");
            CheckExit.Finish(failures == 0 ? 0 : 1);
        }

        private static int Expect(string name, int[] got, int[] want)
        {
            bool ok = true;
            for (int i = 0; i < want.Length; i++) { ok &= got[i] == want[i]; }
            Debug.Log($"  {(ok ? "ok  " : "FAIL")} {name}: got [{Join(got)}], expected [{Join(want)}]");
            return ok ? 0 : 1;
        }

        private static int Report(string title, string[] names, long[] votes, long valid, int[] model, int[] real)
        {
            var sb = new StringBuilder();
            sb.Append("BACKTEST: ").Append(title).Append('\n');
            int totalAbsDev = 0;
            for (int i = 0; i < names.Length; i++)
            {
                int dev = model[i] - real[i];
                totalAbsDev += Math.Abs(dev);
                double share = 100.0 * votes[i] / valid;
                sb.Append(string.Format(System.Globalization.CultureInfo.InvariantCulture,
                    "  {0,-6} share {1,6:F2}%  real {2,3}  model {3,3}  dev {4,4:+0;-0;0}\n", names[i], share, real[i], model[i], dev));
            }

            sb.Append(string.Format(System.Globalization.CultureInfo.InvariantCulture,
                "  total absolute seat deviation: {0}", totalAbsDev));
            Debug.Log(sb.ToString());
            return 0; // findings, not failures
        }

        private static string Join(int[] a) => string.Join(",", a);
    }
}
