using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using PoliSim.Data;
using PoliSim.Elections;
using PoliSim.Simulation;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// §706 (Elias's rulings of 2026-10-01): THE TREASURY LOCK LIFTED AND FINANCE WEIGHED - the Gamson share counts each post at Druckman &amp;
    /// Warwick's published salience (`PortfolioSalience`), the head of government's weight credited to its party, the heaviest posts first to the
    /// party with the most entitlement outstanding. The ruling's tests: the 2021 chamber's SPD+Grüne+FDP puts Finance with the FDP, the 2025
    /// chamber's CDU+CSU+SPD with the SPD - each the record's. Sweden 2022 keeps Finance with M on the weights alone; Sweden 2026 by §711's near-tie
    /// rule (KD's lead under a tenth of the post - the larger party, M). The lock's removal is `SpeakerRoundDiagnostic` (3)'s: a sheet giving a partner Finance is answered on its merits.
    /// </summary>
    public static class PortfolioSalienceDiagnostic
    {
        private static string F(string format, params object[] args) => string.Format(CultureInfo.InvariantCulture, format, args);

        public static void Run()
        {
            CheckExit.ArmLogFold();
            var sb = new StringBuilder("=== PortfolioSalienceDiagnostic (§706): Finance weighed, the Treasury lock lifted ===\n");
            int failures = 0;
            void Check(bool ok, string what) { if (!ok) { failures++; } sb.Append(ok ? "    ok        " : "    FAIL      ").Append(what).Append('\n'); }
            using IDisposable epoch = SimulationManager.EpochScope();
            try
            {
                WorldClock.ApplyStart(CountryId.Germany);
                SimulationRandom.Seed(777);
                EnergyMarket.ResetCalibration();
                World world = WorldFactory.CreateDefault();
                Country germany = world.GetCountry(CountryId.Germany);
                string Holder(Dictionary<string, List<CabinetPortfolio>> posts, CabinetPortfolio p) { foreach (KeyValuePair<string, List<CabinetPortfolio>> kv in posts) { if (kv.Value.Contains(p)) { return kv.Key; } } return "none"; }
                string Describe(Dictionary<string, List<CabinetPortfolio>> posts)
                {
                    var parts = new List<string>();
                    foreach (KeyValuePair<string, List<CabinetPortfolio>> kv in posts) { parts.Add(kv.Key + " " + string.Join("/", kv.Value.ConvertAll(p => p.ToString()))); }
                    return string.Join("; ", parts);
                }

                germany.ParliamentSeats = PartySystems.InitialSeats(CountryId.Germany, ElectionVintage.Germany2021);
                Dictionary<string, List<CabinetPortfolio>> p21 = GovernmentRecord.GamsonPosts(germany, new[] { "SPD", "Grune", "FDP" }, "SPD");
                Check(Holder(p21, CabinetPortfolio.FinanceTreasury) == "FDP",
                    F("the 2021 chamber (SPD 206 · Grüne 118 · FDP 92), Scholz's SPD+Grüne+FDP: Finance with {0} - the record's Lindner (FDP) [{1}]", Holder(p21, CabinetPortfolio.FinanceTreasury), Describe(p21)));
                germany.ParliamentSeats = PartySystems.InitialSeats(CountryId.Germany, ElectionVintage.Germany2025);
                Dictionary<string, List<CabinetPortfolio>> p25 = GovernmentRecord.GamsonPosts(germany, new[] { "CDU", "CSU", "SPD" }, "CDU");
                Check(Holder(p25, CabinetPortfolio.FinanceTreasury) == "SPD",
                    F("the 2025 chamber (CDU 164 · SPD 120 · CSU 44), Merz's CDU+CSU+SPD: Finance with {0} - the record's Klingbeil (SPD) [{1}]", Holder(p25, CabinetPortfolio.FinanceTreasury), Describe(p25)));
                Check(Holder(p21, CabinetPortfolio.Education) == "FDP",
                    F("2021: Education with {0} - the record's Stark-Watzinger (FDP), a second post the method lands", Holder(p21, CabinetPortfolio.Education)));

                // Every post allocated once, the shares near the salience-weighted Gamson entitlement, the head's party holding at least one post.
                bool allOnce = true;
                foreach (CabinetPortfolio p in (CabinetPortfolio[])Enum.GetValues(typeof(CabinetPortfolio)))
                {
                    int n = 0; foreach (List<CabinetPortfolio> held in p25.Values) { if (held.Contains(p)) { n++; } }
                    if (n != 1) { allOnce = false; }
                }
                double total = PortfolioSalience.HeadWeight(CountryId.Germany);
                foreach (CabinetPortfolio p in (CabinetPortfolio[])Enum.GetValues(typeof(CabinetPortfolio))) { total += PortfolioSalience.Weight(CountryId.Germany, p); }
                var shares = new List<string>();
                foreach (KeyValuePair<string, List<CabinetPortfolio>> kv in p25)
                {
                    double got = PortfolioSalience.Of(CountryId.Germany, kv.Value, head: kv.Key == "CDU");
                    double due = total * germany.ParliamentSeats[kv.Key] / 328.0;
                    shares.Add(F("{0} {1:0.00} of {2:0.00} due", kv.Key, got, due));
                }
                Check(allOnce && p25["CDU"].Count >= 1, F("2025: every post held once, the chancellor's party holding at least one; the salience received against the Gamson share - {0}", string.Join(", ", shares)));

                // Sweden 2022 (M 68 · KD 19 · L 16): the prime minister's party keeps Finance on the weights alone - no lock needed.
                WorldClock.ApplyStart(CountryId.Sweden);
                World sworld = WorldFactory.CreateDefault();
                Country sweden = sworld.GetCountry(CountryId.Sweden);
                sweden.ParliamentSeats = PartySystems.InitialSeats(CountryId.Sweden, ElectionVintage.Sweden2022);
                Dictionary<string, List<CabinetPortfolio>> se = GovernmentRecord.GamsonPosts(sweden, new[] { "M", "KD", "L" }, "M");
                Check(Holder(se, CabinetPortfolio.FinanceTreasury) == "M",
                    F("Sweden 2022, Kristersson's M+KD+L: Finance with {0} on the weights alone - the record's Svantesson (M) [{1}]", Holder(se, CabinetPortfolio.FinanceTreasury), Describe(se)));

                // §711 (Elias's ruling of 2026-10-01, item 2: near-ties go to the larger party): on the Riksdag elected 2026-09-13 M+KD+L had given
                // Finance to KD by 0.056 of entitlement (§706's pin, the review's note B) - under a tenth of Finance's 1.68, a near-tie, so it goes to M,
                // the larger party. The 2026 government is not yet on record; a change to the weights or the method that moves it shows here.
                sweden.ParliamentSeats = PartySystems.InitialSeats(CountryId.Sweden, ElectionVintage.Sweden2026);
                Dictionary<string, List<CabinetPortfolio>> se26 = GovernmentRecord.GamsonPosts(sweden, new[] { "M", "KD", "L" }, "M");
                Check(sweden.ParliamentSeats["M"] == 70 && sweden.ParliamentSeats["KD"] == 22 && sweden.ParliamentSeats["L"] == 19 && Holder(se26, CabinetPortfolio.FinanceTreasury) == "M",
                    F("Sweden 2026 (M {0} · KD {1} · L {2}), M+KD+L: Finance with {3} - KD's 0.056 lead in entitlement is a near-tie (under a tenth of the post's 1.68), so the larger party takes it (§711) [{4}]",
                        sweden.ParliamentSeats["M"], sweden.ParliamentSeats["KD"], sweden.ParliamentSeats["L"], Holder(se26, CabinetPortfolio.FinanceTreasury), Describe(se26)));

                // §717 (Elias's ruling of 2026-10-01, item 8): Italy's MEF sums its three rated posts as the paper does (1.64 + 1.32 + 0.98 = 3.94), the
                // derived mean Poland and the USA take moves with it (1.705 -> 2.28); the resulting MISS against the record is recorded, the method unadjusted.
                Check(Math.Abs(PortfolioSalience.Weight(CountryId.Italy, CabinetPortfolio.FinanceTreasury) - 3.94) < 1e-9 && Math.Abs(PortfolioSalience.Weight(CountryId.Poland, CabinetPortfolio.FinanceTreasury) - 2.28) < 1e-9,
                    F("Italy's MEF weighs {0:0.00} (Treasury 1.64 + Finance 1.32 + Budget 0.98, the paper's three posts summed); the derived mean {1:0.00}",
                      PortfolioSalience.Weight(CountryId.Italy, CabinetPortfolio.FinanceTreasury), PortfolioSalience.Weight(CountryId.Poland, CabinetPortfolio.FinanceTreasury)));
                Country italy = world.GetCountry(CountryId.Italy);
                italy.ParliamentSeats = PartySystems.InitialSeats(CountryId.Italy, ElectionVintage.Italy2022);
                Dictionary<string, List<CabinetPortfolio>> it22 = GovernmentRecord.GamsonPosts(italy, new[] { "FdI", "Lega", "FI", "NM" }, "FdI");
                Check(Holder(it22, CabinetPortfolio.FinanceTreasury) == "FdI",
                    F("Italy 2022, Meloni's FdI+Lega+FI+NM: Finance with {0} - a MISS against the record's Giorgetti (Lega), recorded and not adjusted (§717) [{1}]", Holder(it22, CabinetPortfolio.FinanceTreasury), Describe(it22)));
                Country poland = world.GetCountry(CountryId.Poland);
                poland.ParliamentSeats = PartySystems.InitialSeats(CountryId.Poland, ElectionVintage.Poland2023);
                Dictionary<string, List<CabinetPortfolio>> pl23 = GovernmentRecord.GamsonPosts(poland, new[] { "KO", "TD", "NL" }, "KO");
                Check(Holder(pl23, CabinetPortfolio.FinanceTreasury) == "KO", F("Poland 2023, Tusk's KO+TD+NL on the derived weights: Finance with {0} - the record's Domański (KO); TD held it on the old mean (1.705), §716's owed decision B, which the summed MEF's move in the mean closes [{1}]", Holder(pl23, CabinetPortfolio.FinanceTreasury), Describe(pl23)));

            }
            catch (Exception e) { failures++; sb.Append("    THREW: " + e.GetType().Name + ": " + e.Message + "\n" + e.StackTrace + "\n"); }
            finally { EnergyMarket.ResetTurnState(); }
            if (failures > 0) { Debug.LogError($"PORTFOLIO SALIENCE: {failures} failure(s).\n{sb}"); CheckExit.Finish(1); return; }
            Debug.Log(sb.ToString());
            CheckExit.Finish(0);
        }
    }
}
