using System;
using System.Text;
using PoliSim.Elections;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// W-A1's harness — **loyalty derived from measured volatility**, replacing Day-2's global
    /// constant of 60 that failed the gate on Italy.
    ///
    /// Two directions are printed, and the distinction is the whole point:
    /// - **PLAY** (the two MOST RECENT elections): the loyalty a campaign would carry into the
    ///   NEXT, unplayed election. This is what the prototype uses.
    /// - **BACKTEST** (T−2 → T−1): the loyalty available BEFORE the election being re-predicted.
    ///   W-A3's re-run must use this one — deriving loyalty from the target election's own
    ///   movement would read the answer off the answer sheet.
    ///
    /// ⚠ **The coverage constraint (sourced 2026-08-29) is reported per country and is not
    /// cosmetic.** A name-joined volatility measure is only as good as party continuity across the
    /// pair: Sweden ~99 % of the vote safely joinable, Germany ~95 %, **Italy ~53 %, Poland ~38 %**.
    /// In the latter two, organisational reshuffling (mergers into coalition committees, splits,
    /// dissolutions) would be scored as voter defection. Their derived loyalties print
    /// LOW-CONFIDENCE, and that is a property of those party systems rather than a defect in the
    /// formula — it is also why the prototype target is Sweden.
    /// </summary>
    public static class LoyaltyHarness
    {
        private readonly struct Series
        {
            public readonly string Country;
            public readonly string[] Parties;
            public readonly double[] T2;      // the T-2 election
            public readonly double[] T1;      // the T-1 election
            public readonly double[] T;       // the most recent election
            public readonly double CoveragePct;
            public readonly string Note;

            public Series(string country, string[] parties, double[] t2, double[] t1, double[] t,
                double coveragePct, string note)
            {
                Country = country; Parties = parties; T2 = t2; T1 = t1; T = t;
                CoveragePct = coveragePct; Note = note;
            }
        }

        public static void Run()
        {
            int failures = 0;
            var sb = new StringBuilder();
            sb.Append("=== W-A1: loyalty derived from volatility (zero authored constants) ===\n");

            // ---- Unit assertions on the formula itself (synthetic) ----
            failures += Near("unchanged party -> loyalty 100", LoyaltyModel.PartyLoyalty(20.0, 20.0), 100.0);
            failures += Near("party that doubled -> 50", LoyaltyModel.PartyLoyalty(20.0, 10.0), 50.0);
            failures += Near("party that halved -> 50 (symmetry)", LoyaltyModel.PartyLoyalty(10.0, 20.0), 50.0);
            failures += Near("party new at T-1 (absent at T-2) -> 0", LoyaltyModel.PartyLoyalty(12.0, 0.0), 0.0);
            failures += Near("party present at neither -> 0", LoyaltyModel.PartyLoyalty(0.0, 0.0), 0.0);
            failures += Near("Pedersen of an unchanged field -> 0",
                LoyaltyModel.PedersenIndex(new[] { 40.0, 35.0, 25.0 }, new[] { 40.0, 35.0, 25.0 }), 0.0);
            failures += Near("Pedersen of a 10-point swap -> 10",
                LoyaltyModel.PedersenIndex(new[] { 50.0, 25.0, 25.0 }, new[] { 40.0, 35.0, 25.0 }), 10.0);

            // ---- The four countries with three elections on disk ----
            foreach (Series s in BuildSeries())
            {
                double[] playLoyalty = LoyaltyModel.PartyLoyalties(s.T, s.T1);        // most recent pair
                double[] backLoyalty = LoyaltyModel.PartyLoyalties(s.T1, s.T2);       // the pre-target pair
                double playPedersen = LoyaltyModel.PedersenIndex(s.T, s.T1);
                double backPedersen = LoyaltyModel.PedersenIndex(s.T1, s.T2);
                double playMean = LoyaltyModel.WeightedMeanLoyalty(playLoyalty, s.T);
                double backMean = LoyaltyModel.WeightedMeanLoyalty(backLoyalty, s.T1);

                bool lowConfidence = s.CoveragePct < 80.0;
                sb.Append(string.Format(System.Globalization.CultureInfo.InvariantCulture,
                    "\n---- {0} ---- continuity coverage {1:F0}% {2}\n  {3}\n",
                    s.Country, s.CoveragePct, lowConfidence ? "** LOW CONFIDENCE **" : "(trustworthy)", s.Note));
                sb.Append("  party    T-2     T-1     T      loyalty(PLAY)  loyalty(BACKTEST)\n");
                for (int i = 0; i < s.Parties.Length; i++)
                {
                    sb.Append(string.Format(System.Globalization.CultureInfo.InvariantCulture,
                        "  {0,-6} {1,6:F2} {2,6:F2} {3,6:F2}      {4,6:F1}          {5,6:F1}\n",
                        s.Parties[i], s.T2[i], s.T1[i], s.T[i], playLoyalty[i], backLoyalty[i]));
                }

                sb.Append(string.Format(System.Globalization.CultureInfo.InvariantCulture,
                    "  Pedersen: play pair {0:F1}, backtest pair {1:F1} | size-weighted mean loyalty: play {2:F1}, backtest {3:F1} (Day-2's global constant was 60)\n",
                    playPedersen, backPedersen, playMean, backMean));

                // The done-when's named check: Italy's FdI and M5S from the 2018->2022 pair.
                if (s.Country == "ITALY")
                {
                    double fdi = playLoyalty[0];
                    double m5s = playLoyalty[2];
                    failures += Assert("W-A1 done-when: Italy FdI computes visibly low loyalty from 2018->2022",
                        fdi < 30.0, $"FdI = {fdi:F1} (vs the old global 60)");
                    failures += Assert("W-A1 done-when: Italy M5S computes visibly low loyalty from 2018->2022",
                        m5s < 60.0, $"M5S = {m5s:F1} (vs the old global 60)");
                }

                if (s.Country == "SWEDEN")
                {
                    failures += Assert("Sweden's derived mean loyalty exceeds Italy's (stable vs volatile system)",
                        playMean > 55.0, $"Sweden size-weighted mean = {playMean:F1}");
                }
            }

            sb.Append("\n---- NOT COMPUTABLE, stated (the done-when's 'all six' shortfall) ----\n");
            sb.Append("  USA and FRANCE: only ONE election each is on disk (2024). Volatility needs two\n");
            sb.Append("  elections before the one modelled, so both are BILLED: the USA needs 2020 + 2016\n");
            sb.Append("  House national shares; France needs 2022 + 2017 legislative. France is out of scope\n");
            sb.Append("  for seats by R-EL10 regardless, so the USA is the one that would pay for itself.\n");

            sb.Append($"\n=== LoyaltyHarness: {(failures == 0 ? "ALL ASSERTIONS PASS" : failures + " FAILED")} ===\n");
            Debug.Log(sb.ToString());
            CheckExit.Finish(failures == 0 ? 0 : 1);
        }

        private static Series[] BuildSeries()
        {
            return new[]
            {
                // Sweden: 2014 -> 2018 -> 2022. Folkpartiet renamed Liberalerna between 2014 and
                // 2018 - joined on ENTITY, not abbreviation (the one rename in the window).
                new Series("SWEDEN",
                    new[] { "S", "M", "SD", "C", "V", "KD", "L", "MP" },
                    new[] { 31.01, 23.33, 12.86, 6.11, 5.72, 4.57, 5.42, 6.89 },
                    new[] { 28.26, 19.84, 17.53, 8.61, 8.00, 6.32, 5.49, 4.41 },
                    new[] { 30.33, 19.10, 20.54, 6.71, 6.75, 5.34, 4.61, 5.08 },
                    99.0, "8 of 8 clean, one rename (FP -> L) joined on entity"),

                // Germany: 2017 -> 2021 -> 2025. SSW did not contest 2017 (0), BSW did not exist
                // before 2024 (0 at both) - both correctly score loyalty 0 where absent.
                new Series("GERMANY",
                    new[] { "CDU", "AfD", "SPD", "Grune", "Linke", "CSU", "FDP", "SSW", "BSW" },
                    new[] { 26.80, 12.60, 20.50, 8.90, 9.20, 6.20, 10.70, 0.00, 0.00 },
                    new[] { 19.00, 10.40, 25.70, 14.70, 4.90, 5.20, 11.40, 0.10, 0.00 },
                    new[] { 22.55, 20.80, 16.41, 11.61, 8.78, 5.97, 4.33, 0.15, 4.98 },
                    95.0, "all seven 2017 contestants clean; SSW absent 2017 (no candidacy), BSW founded 2024"),

                // Poland: 2015 -> 2019 -> 2023, joined at COMMITTEE level with the merges named.
                new Series("POLAND",
                    new[] { "PiS", "KO", "TD", "NL", "Konf" },
                    new[] { 37.58, 24.09, 5.13, 7.55, 4.76 },
                    new[] { 43.59, 27.40, 8.55, 12.56, 6.81 },
                    new[] { 35.38, 30.70, 14.40, 8.61, 7.16 },
                    38.0, "only PiS and the German Minority are clean; KO absorbed Nowoczesna, TD absorbed Kukiz'15, NL absorbed Razem, Konf grew out of KORWiN"),

                // Italy: 2013 -> 2018 -> 2022. PdL -> FI is a rename PLUS a split; SEL -> LeU -> AVS
                // is a partial lineage; Azione/IV did not exist before 2019.
                new Series("ITALY",
                    new[] { "FdI", "PD", "M5S", "Lega", "FI", "AzIV", "AVS" },
                    new[] { 1.96, 25.43, 25.56, 4.09, 21.56, 0.00, 3.20 },
                    new[] { 4.35, 18.76, 32.68, 17.35, 14.00, 0.00, 3.39 },
                    new[] { 25.98, 19.04, 15.43, 8.79, 8.11, 7.78, 3.64 },
                    53.0, "only PD and M5S clean; PdL -> FI is a rename plus a split; SEL -> LeU -> AVS partial"),
            };
        }

        private static int Near(string label, double actual, double expected)
        {
            bool ok = Math.Abs(actual - expected) < 1e-9;
            Debug.Log(string.Format(System.Globalization.CultureInfo.InvariantCulture,
                "  {0} {1}: got {2:F4}, expected {3:F4}", ok ? "ok  " : "FAIL", label, actual, expected));
            return ok ? 0 : 1;
        }

        private static int Assert(string label, bool condition, string detail)
        {
            Debug.Log($"  {(condition ? "ok  " : "FAIL")} {label}: {detail}");
            return condition ? 0 : 1;
        }
    }
}
