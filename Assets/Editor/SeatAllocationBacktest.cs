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
    /// `COMPLETED.md §188` Part 5 requires before the allocator claims are
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
    /// - USA 2024 ELECTORAL COLLEGE (added Day-1, R-EL8): the REAL rule — 49 winner-take-all
    ///   jurisdictions plus Maine's and Nebraska's congressional-district method, computed from
    ///   the statutes via <see cref="PoliSim.Elections.ElectoralCollege"/>, with the
    ///   forced-winner-take-all counterfactual printed beside it because the two district
    ///   effects cancel in 2024's national total.
    /// - ITALY: NOT run — the Rosatellum's proportional allocation FORMULA is not yet sourced
    ///   (thresholds and structure are; the formula is R-EL9's sourcing task), and this harness
    ///   does not run un-sourced arithmetic. FRANCE: NOT run — two-round SMD has no national
    ///   model by construction. The USA House stays national-totals-only. Each stated here so
    ///   silence cannot read as coverage.
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

            // --- GERMANY 2025 ON EXACT COUNTS (ElectionsData/germany/national_counts_2025.csv:
            // kerg2.csv Bund rows, the second fetch of the night - the Part 5 constraint
            // honoured in full for Germany too) ---
            long[] deCounts = { 11196374, 10328780, 8149124, 5762380, 4356532, 2964028, 76138, 2472947, 2148757 };
            const long deCountsValid = 49649512;
            int[] deModel2 = SeatAllocation.AllocateWithThreshold(deCounts, deCountsValid, 0.05, 630, SeatAllocation.SainteLagueDivisor, deExempt);
            failures += Report("GERMANY 2025 ON EXACT COUNTS - the same regime over kerg2.csv's own integers (the definitive run; the shares run above stays as the precision result)", deNames, deCounts, deCountsValid, deModel2, deReal);

            // --- POLAND 2023 REAL: d'Hondt in each of the 41 okregi over the absolute counts
            // (ElectionsData/poland/district_votes_2023.csv - the KBW file, national sums
            // verified exactly), magnitudes from okregi_sejm (sum 460), eligibility national
            // (art. 196/197: PiS/NL/Konf 5%, KO/TD 8%, MN exempt) ---
            int[] plMagnitudes = { 12, 8, 14, 12, 13, 15, 12, 12, 10, 9, 12, 8, 14, 10, 9, 10, 9, 12, 20, 12, 12, 11, 15, 14, 12, 14, 9, 7, 9, 9, 12, 9, 16, 8, 10, 12, 9, 9, 10, 8, 12 };
            long[][] plDistricts =
            {
                new long[] { 174643, 169540, 53958, 47715, 31770, 0 },
                new long[] { 107797, 120188, 39215, 25806, 19478, 0 },
                new long[] { 206899, 286713, 106624, 88089, 54132, 0 },
                new long[] { 162603, 186914, 80426, 52959, 34266, 0 },
                new long[] { 183131, 158719, 84308, 60473, 34232, 0 },
                new long[] { 294847, 131712, 102894, 37083, 54325, 0 },
                new long[] { 231882, 79501, 59577, 25691, 35594, 0 },
                new long[] { 143530, 195091, 77933, 47911, 33672, 0 },
                new long[] { 122433, 187527, 54283, 55770, 25428, 0 },
                new long[] { 184929, 86083, 54479, 25340, 30247, 0 },
                new long[] { 221031, 138038, 77313, 41188, 36383, 0 },
                new long[] { 156308, 88408, 54585, 22036, 28754, 0 },
                new long[] { 232430, 232799, 127693, 83633, 58435, 0 },
                new long[] { 229587, 68804, 49487, 13594, 37301, 0 },
                new long[] { 196433, 68690, 75229, 16152, 32241, 0 },
                new long[] { 195218, 99146, 75526, 28848, 28877, 0 },
                new long[] { 190418, 82003, 54690, 20874, 28593, 0 },
                new long[] { 262236, 100902, 83681, 26149, 44299, 0 },
                new long[] { 345380, 741286, 227127, 230648, 124220, 0 },
                new long[] { 231905, 257470, 110086, 51556, 51573, 0 },
                new long[] { 150022, 161241, 61155, 34763, 31150, 25778 },
                new long[] { 241790, 70054, 60938, 19750, 38080, 0 },
                new long[] { 347688, 119259, 83676, 32828, 63854, 0 },
                new long[] { 258277, 126971, 114898, 29478, 59648, 0 },
                new long[] { 155318, 257009, 90599, 57967, 38406, 0 },
                new long[] { 199709, 258909, 92793, 56887, 49203, 0 },
                new long[] { 163506, 127677, 64778, 34601, 34909, 0 },
                new long[] { 117756, 94313, 47698, 30497, 21256, 0 },
                new long[] { 116827, 139711, 51681, 35673, 26934, 0 },
                new long[] { 145230, 114404, 47525, 26117, 30527, 0 },
                new long[] { 162458, 193596, 69825, 44509, 35240, 0 },
                new long[] { 112389, 114519, 37221, 81646, 21512, 0 },
                new long[] { 310266, 137941, 90975, 45048, 43197, 0 },
                new long[] { 105373, 95410, 46101, 24269, 19590, 0 },
                new long[] { 126432, 129339, 63007, 31631, 27119, 0 },
                new long[] { 194416, 154990, 87628, 46222, 37838, 0 },
                new long[] { 162192, 100580, 69740, 39761, 29208, 0 },
                new long[] { 120301, 144114, 72996, 32378, 28370, 0 },
                new long[] { 116666, 262779, 98589, 73345, 35182, 0 },
                new long[] { 101023, 124625, 39776, 28101, 19379, 0 },
                new long[] { 159575, 222427, 69957, 52032, 32942, 0 },
            };
            int magSum = 0;
            foreach (int m in plMagnitudes) { magSum += m; }
            Debug.Log($"BACKTEST: Poland real-system inputs - 41 districts, magnitudes sum {magSum} (must be 460); eligibility national, MN exempt.");
            var plEligible = new bool[6];
            for (int i = 0; i < 6; i++) { plEligible[i] = plMasked[i] > 0 || plExempt[i]; }

            int[] plRealModel = SeatAllocation.PerDistrictSum(plDistricts, plMagnitudes, plEligible, SeatAllocation.DHondtDivisor);
            failures += Report("POLAND 2023 REAL - d'Hondt per okreg over the KBW absolute counts (the actual Sejm system; the definitive run)", plNames, plVotes, plValid, plRealModel, plReal);

            // --- USA 2024 ELECTORAL COLLEGE, BY THE REAL RULE (R-EL8, ruled 2026-08-29).
            // Winner-take-all is a STATE CHOICE that 48 states and DC direct; Maine and Nebraska
            // direct the congressional-district method instead (Me. 21-A s802; Neb. s32-710 with
            // s32-1038(1) - the statutes and the per-district results are
            // ElectionsData/usa/district_method_2024.md). The model computes the split FROM the
            // district winners by the statute; it does not read a pre-split answer.
            // Candidate index 0 = Trump (R), 1 = Harris (D). Jurisdiction EVs from
            // ElectionsData/usa/state_ev_2024.csv (FEC); ME/NE district winners from the state
            // canvasses.
            bool[] usIsR = { true, true, true, true, false, false, false, false, false, true, true, false, true, false, true, true, true, true, true, false, false, false, true, false, true, true, true, true, true, false, false, false, false, true, true, true, true, false, true, false, true, true, true, true, true, false, false, false, true, true, true };
            string[] usNames = { "AL", "AK", "AZ", "AR", "CA", "CO", "CT", "DE", "DC", "FL", "GA", "HI", "ID", "IL", "IN", "IA", "KS", "KY", "LA", "ME", "MD", "MA", "MI", "MN", "MS", "MO", "MT", "NE", "NV", "NH", "NJ", "NM", "NY", "NC", "ND", "OH", "OK", "OR", "PA", "RI", "SC", "SD", "TN", "TX", "UT", "VT", "VA", "WA", "WV", "WI", "WY" };
            int[] usTotalEv = { 9, 3, 11, 6, 54, 10, 7, 3, 3, 30, 16, 4, 4, 19, 11, 6, 6, 8, 8, 4, 10, 11, 15, 10, 6, 10, 4, 5, 6, 4, 14, 5, 28, 16, 3, 17, 7, 8, 19, 4, 9, 3, 11, 40, 6, 3, 13, 12, 4, 10, 3 };
            var usJurisdictions = new ElectoralCollege.Jurisdiction[usNames.Length];
            for (int i = 0; i < usNames.Length; i++)
            {
                int statewide = usIsR[i] ? 0 : 1;
                if (usNames[i] == "ME")
                {
                    // 2 at-large (statewide Harris) + ME-1 Harris + ME-2 Trump.
                    usJurisdictions[i] = new ElectoralCollege.Jurisdiction("ME", 4, statewide, 2, new[] { 1, 0 });
                }
                else if (usNames[i] == "NE")
                {
                    // 2 at-large (statewide Trump) + NE-1 Trump + NE-2 Harris + NE-3 Trump.
                    usJurisdictions[i] = new ElectoralCollege.Jurisdiction("NE", 5, statewide, 2, new[] { 0, 1, 0 });
                }
                else
                {
                    usJurisdictions[i] = new ElectoralCollege.Jurisdiction(usNames[i], usTotalEv[i], statewide);
                }
            }

            int[] usReal = ElectoralCollege.Allocate(usJurisdictions, 2);
            int[] usWtaOnly = ElectoralCollege.AllocateAsIfWinnerTakeAll(usJurisdictions, 2);
            int usWinner = ElectoralCollege.Winner(usReal);
            Debug.Log($"BACKTEST: USA 2024 ELECTORAL COLLEGE by the real rule (49 winner-take-all jurisdictions + ME and NE by congressional district): Trump {usReal[0]} / Harris {usReal[1]} vs real 312/226 - {(usReal[0] == 312 && usReal[1] == 226 ? "EXACT" : "DEVIATES, a finding")}; majority {ElectoralCollege.MajorityToElect} reached by {(usWinner < 0 ? "nobody - contingent election, not modelled" : usWinner == 0 ? "Trump" : "Harris")}.");
            Debug.Log($"BACKTEST: USA counterfactual - every jurisdiction forced winner-take-all: Trump {usWtaOnly[0]} / Harris {usWtaOnly[1]}. The district method moved {Math.Abs(usReal[0] - usWtaOnly[0])} elector(s) in each direction this cycle, so the two effects CANCEL in the national total: a model that dropped the district rule would have matched 312/226 by luck. That cancellation is why R-EL8 required the rule to be built from the statutes rather than inferred from a matching total.");
            failures += Expect("USA EC by the real rule", usReal, new[] { 312, 226 });

            // --- ITALY 2022, CAMERA, THE PROPORTIONAL STAGE (R-EL9, ruled 2026-08-29; the
            // allocation arithmetic sourced in ElectionsData/italy/rosatellum_allocation.md).
            // Floored-Hare twice: lett. f) between coalitions/standalone lists, lett. g) inside
            // each coalition over its ADMITTED lists only. Coalition 0 = centre-right,
            // 1 = centre-left, -1 = standalone. SVP-PATT enters by the minority route at 0.42%.
            // The 146 college seats are already out of the pool: 245 proportional seats.
            // Compared against Eligendo's PROPORTIONAL seat column (not total seats - the two
            // tiers are parallel, so a party's college wins are not part of this test).
            var itLists = new[]
            {
                new Rosatellum.ListEntry("FdI",   7301303, 0),
                new Rosatellum.ListEntry("Lega",  2470318, 0),
                new Rosatellum.ListEntry("FI",    2279266, 0),
                new Rosatellum.ListEntry("NM",     254127, 0),
                new Rosatellum.ListEntry("PD",    5348676, 1),
                new Rosatellum.ListEntry("AVS",   1021808, 1),
                new Rosatellum.ListEntry("+Eur",   796057, 1),
                new Rosatellum.ListEntry("IC",     173555, 1),
                new Rosatellum.ListEntry("M5S",   4335494, -1),
                new Rosatellum.ListEntry("AzIV",  2186505, -1),
                new Rosatellum.ListEntry("SVP",    117032, -1, true),
            };
            const long itValid = 28098196;
            int[] itReal = { 69, 23, 22, 0, 57, 11, 0, 0, 41, 21, 1 };
            int[] itModel = Rosatellum.AllocateNational(itLists, itValid, 245, out string itTrace);
            Debug.Log("BACKTEST: Italy working (art. 83 stage by stage) -\n" + itTrace);
            var itNames = new string[itLists.Length];
            var itVotes = new long[itLists.Length];
            for (int i = 0; i < itLists.Length; i++) { itNames[i] = itLists[i].Name; itVotes[i] = itLists[i].Votes; }
            failures += Report("ITALY 2022 CAMERA - Rosatellum national proportional stage, 245 seats, floored Hare twice (expected EXACT vs Eligendo's proportional seat column)", itNames, itVotes, itValid, itModel, itReal);

            Debug.Log("BACKTEST: NOT RUN, stated: Italy's SUB-NATIONAL stages (lett. h/i into the 28 circoscrizioni, art. 83-bis into the 49 collegi, and the art. 84 incapienza cascade - they need per-circoscrizione and per-collegio cifre elettorali that exist only as HTML on Eligendo; they change WHICH deputies sit, not the per-list national totals above). FRANCE: NOT run - two-round SMD has no national model by construction. The USA House stays national-totals-only (no district model claimed).");
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
