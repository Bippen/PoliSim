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
    /// chamber's CDU+CSU+SPD with the SPD - each the record's, until §753 (Elias's ruling A3) summed Germany's BMBF as the paper sums a merged post:
    /// dated (the 2025 chamber's government split it): 2021's Finance landed with the SPD, a miss recorded and pinned; 2025's with the SPD, the record's. AF-6 (q6, 2026-10-18) added
    /// the economy portfolio - PROVISIONALLY weighed by the economy ministry alone (reading (b), pending Elias; (a), its three ministries summed, is the
    /// alternative put to him) - and the small-party bonus as read (the residual to the partners, each post to the one left least above its due in proportion): Sweden's 2022 and 2026 Finance stay with M and
    /// 2021's with the SPD (a miss, as before); 2025's moves to the CDU, a MISS against the record's Klingbeil, recorded and pinned (the ask of AF-6's
    /// record); Italy's stays a miss, Poland's the record's. The lock's removal is `SpeakerRoundDiagnostic` (3)'s: a sheet giving a partner Finance is answered on its merits.
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
                Dictionary<string, List<CabinetPortfolio>> p21 = GovernmentRecord.GamsonPosts(germany, new[] { "SPD", "Grune", "FDP" }, "SPD", new DateTime(2021, 12, 8));
                // §753 (Elias's ruling A3: "merged ministries coded as Druckman & Warwick code them, Germany included"): the BMBF weighs its two rated
                // posts summed (0.82 + 0.93 = 1.75), as the paper sums a merged post (its own example: Ireland's justice and communications), for the
                // 2021 chamber's cabinets; the 2025 chamber's government split it, its education post the BMBFSFJ (0.82 + 0.68). 2021's Finance then lands
                // with the SPD - a MISS against the record's Lindner, recorded and not adjusted (as §717's Italy); 2025's with the SPD, the record's.
                Check(Holder(p21, CabinetPortfolio.FinanceTreasury) == "SPD",
                    F("the 2021 chamber (SPD 206 · Grüne 118 · FDP 92), Scholz's SPD+Grüne+FDP: Finance with {0} - a MISS against the record's Lindner (FDP), recorded and not adjusted (§753) [{1}]", Holder(p21, CabinetPortfolio.FinanceTreasury), Describe(p21)));
                germany.ParliamentSeats = PartySystems.InitialSeats(CountryId.Germany, ElectionVintage.Germany2025);
                Dictionary<string, List<CabinetPortfolio>> p25 = GovernmentRecord.GamsonPosts(germany, new[] { "CDU", "CSU", "SPD" }, "CDU", new DateTime(2025, 5, 6));
                Check(Holder(p25, CabinetPortfolio.FinanceTreasury) == "CDU",
                    F("the 2025 chamber (CDU 164 · SPD 120 · CSU 44), Merz's CDU+CSU+SPD: Finance with {0} - a MISS against the record's Klingbeil (SPD) since AF-6: with a seventh post the CDU's entitlement covers Finance and the SPD's goes to Interior, Education and Defence (§753's split BMBF kept) [{1}]", Holder(p25, CabinetPortfolio.FinanceTreasury), Describe(p25)));
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
                foreach (CabinetPortfolio p in (CabinetPortfolio[])Enum.GetValues(typeof(CabinetPortfolio))) { total += PortfolioSalience.Weight(CountryId.Germany, p, new DateTime(2025, 5, 6)); }
                var shares = new List<string>();
                foreach (KeyValuePair<string, List<CabinetPortfolio>> kv in p25)
                {
                    double got = PortfolioSalience.Of(CountryId.Germany, kv.Value, new DateTime(2025, 5, 6), head: kv.Key == "CDU");
                    double due = total * germany.ParliamentSeats[kv.Key] / 328.0;
                    shares.Add(F("{0} {1:0.00} of {2:0.00} due", kv.Key, got, due));
                }
                Check(allOnce && p25["CDU"].Count >= 1, F("2025: every post held once, the chancellor's party holding at least one; the salience received against the Gamson share - {0}", string.Join(", ", shares)));

                // Sweden 2022 (M 68 · KD 19 · L 16): the prime minister's party keeps Finance on the weights alone - no lock needed.
                WorldClock.ApplyStart(CountryId.Sweden);
                World sworld = WorldFactory.CreateDefault();
                Country sweden = sworld.GetCountry(CountryId.Sweden);
                sweden.ParliamentSeats = PartySystems.InitialSeats(CountryId.Sweden, ElectionVintage.Sweden2022);
                Dictionary<string, List<CabinetPortfolio>> se = GovernmentRecord.GamsonPosts(sweden, new[] { "M", "KD", "L" }, "M", new DateTime(2022, 10, 18));
                Check(Holder(se, CabinetPortfolio.FinanceTreasury) == "M",
                    F("Sweden 2022, Kristersson's M+KD+L: Finance with {0} on the weights - the record's Svantesson (M) [{1}]", Holder(se, CabinetPortfolio.FinanceTreasury), Describe(se)));

                // §711 (Elias's ruling of 2026-10-01, item 2: near-ties go to the larger party): on the Riksdag elected 2026-09-13 M+KD+L had given
                // Finance to KD by 0.056 of entitlement (§706's pin, the review's note B) - under a tenth of Finance's 1.68, a near-tie, so it goes to M,
                // the larger party. The 2026 government is not yet on record; a change to the weights or the method that moves it shows here.
                sweden.ParliamentSeats = PartySystems.InitialSeats(CountryId.Sweden, ElectionVintage.Sweden2026);
                Dictionary<string, List<CabinetPortfolio>> se26 = GovernmentRecord.GamsonPosts(sweden, new[] { "M", "KD", "L" }, "M", new DateTime(2026, 9, 13));
                Check(sweden.ParliamentSeats["M"] == 70 && sweden.ParliamentSeats["KD"] == 22 && sweden.ParliamentSeats["L"] == 19 && Holder(se26, CabinetPortfolio.FinanceTreasury) == "M",
                    F("Sweden 2026 (M {0} · KD {1} · L {2}), M+KD+L: Finance with {3} - M has the most entitlement outstanding when Finance comes up (AF-6, whole entitlements first) [{4}]",
                        sweden.ParliamentSeats["M"], sweden.ParliamentSeats["KD"], sweden.ParliamentSeats["L"], Holder(se26, CabinetPortfolio.FinanceTreasury), Describe(se26)));

                // §717 (Elias's ruling of 2026-10-01, item 8): Italy's MEF sums its three rated posts as the paper does (1.64 + 1.32 + 0.98 = 3.94), the
                // derived mean Poland and the USA take moves with it (1.705 -> 2.28); the resulting MISS against the record is recorded, the method unadjusted.
                Check(Math.Abs(PortfolioSalience.Weight(CountryId.Italy, CabinetPortfolio.FinanceTreasury, new DateTime(2023, 12, 13)) - 3.94) < 1e-9 && Math.Abs(PortfolioSalience.Weight(CountryId.Poland, CabinetPortfolio.FinanceTreasury, new DateTime(2023, 12, 13)) - 2.545) < 1e-9,
                    F("Italy's MEF weighs {0:0.00} (Treasury 1.64 + Finance 1.32 + Budget 0.98, the paper's three posts summed); the derived mean {1:0.000} on Tusk's day - with France's Bercy and its Industry summed since §753 (1.92 + 1.06)",
                      PortfolioSalience.Weight(CountryId.Italy, CabinetPortfolio.FinanceTreasury, new DateTime(2023, 12, 13)), PortfolioSalience.Weight(CountryId.Poland, CabinetPortfolio.FinanceTreasury, new DateTime(2023, 12, 13))));
                // AF-6 (q6), PROVISIONAL reading (b): the economy portfolio weighs the economy ministry that holds energy, alone (`ElectionsData/portfolios/portfolio_salience.md`) -
                // France's Industry until Bercy carries it (a ministry counts once), then its unrated energy ministry at the survey's average 1.00; the derived mean follows
                double eco = PortfolioSalience.Weight(CountryId.Germany, CabinetPortfolio.Economy, new DateTime(2025, 5, 6));
                double ecoFrBefore = PortfolioSalience.Weight(CountryId.France, CabinetPortfolio.Economy, new DateTime(2022, 5, 19)), ecoFrAfter = PortfolioSalience.Weight(CountryId.France, CabinetPortfolio.Economy, new DateTime(2022, 5, 20));
                double ecoMean = PortfolioSalience.Weight(CountryId.Poland, CabinetPortfolio.Economy, new DateTime(2023, 12, 13));
                Check(Math.Abs(eco - 0.99) < 1e-9 && Math.Abs(ecoFrBefore - 1.06) < 1e-9 && Math.Abs(ecoFrAfter - 1.00) < 1e-9 && Math.Abs(ecoMean - (0.99 + 1.06 + 1.00 + 0.87) / 4.0) < 1e-9,
                    F("the economy post (AF-6, provisional (b)): Germany {0:0.00} (Economic Affairs), France {1:0.00} before Bercy carried Industry and {2:0.00} after (unrated, the survey's average), the derived mean {3:0.0000} on Tusk's day", eco, ecoFrBefore, ecoFrAfter, ecoMean));
                Country italy = world.GetCountry(CountryId.Italy);
                italy.ParliamentSeats = PartySystems.InitialSeats(CountryId.Italy, ElectionVintage.Italy2022);
                Dictionary<string, List<CabinetPortfolio>> it22 = GovernmentRecord.GamsonPosts(italy, new[] { "FdI", "Lega", "FI", "NM" }, "FdI", new DateTime(2022, 10, 22));
                Check(Holder(it22, CabinetPortfolio.FinanceTreasury) == "FdI",
                    F("Italy 2022, Meloni's FdI+Lega+FI+NM: Finance with {0} - a MISS against the record's Giorgetti (Lega), recorded and not adjusted (§717) [{1}]", Holder(it22, CabinetPortfolio.FinanceTreasury), Describe(it22)));
                Country poland = world.GetCountry(CountryId.Poland);
                poland.ParliamentSeats = PartySystems.InitialSeats(CountryId.Poland, ElectionVintage.Poland2023);
                Dictionary<string, List<CabinetPortfolio>> pl23 = GovernmentRecord.GamsonPosts(poland, new[] { "KO", "TD", "NL" }, "KO", new DateTime(2023, 12, 13));
                Check(Holder(pl23, CabinetPortfolio.FinanceTreasury) == "KO", F("Poland 2023, Tusk's KO+TD+NL on the derived weights: Finance with {0} - the record's Domański (KO); TD held it on the old mean (1.705), §716's owed decision B, which the summed MEF's move in the mean closes [{1}]", Holder(pl23, CabinetPortfolio.FinanceTreasury), Describe(pl23)));
                // AF-6 (the review's M4): THE SHARES, PINNED - each party's posts, and the salience it receives against its Gamson due. The residual goes to the
                // partner left least above its due in proportion, so no partner is paid far past its share (the first cut paid the 7-seat NM 2.2 times its due).
                string Shares(Country c, Dictionary<string, List<CabinetPortfolio>> posts, string headParty, DateTime on)
                {
                    double whole = PortfolioSalience.HeadWeight(c.Id);
                    foreach (CabinetPortfolio p in (CabinetPortfolio[])Enum.GetValues(typeof(CabinetPortfolio))) { whole += PortfolioSalience.Weight(c.Id, p, on); }
                    int seatsAll = 0;
                    foreach (string party in posts.Keys) { seatsAll += c.ParliamentSeats[party]; }
                    var parts = new List<string>();
                    foreach (KeyValuePair<string, List<CabinetPortfolio>> kv in posts) { parts.Add(F("{0} {1:0.00} of {2:0.00} due", kv.Key, PortfolioSalience.Of(c.Id, kv.Value, on, head: kv.Key == headParty), whole * c.ParliamentSeats[kv.Key] / seatsAll)); }
                    return string.Join(", ", parts);
                }
                bool Holds(Dictionary<string, List<CabinetPortfolio>> posts, string party, params CabinetPortfolio[] want) =>
                    posts.TryGetValue(party, out List<CabinetPortfolio> got) && got.Count == want.Length && Array.TrueForAll(want, got.Contains);
                Check(Holds(it22, "FdI", CabinetPortfolio.FinanceTreasury, CabinetPortfolio.ForeignAffairs) && Holds(it22, "Lega", CabinetPortfolio.InteriorJustice, CabinetPortfolio.Defense, CabinetPortfolio.Education)
                      && Holds(it22, "FI", CabinetPortfolio.HealthSocialAffairs, CabinetPortfolio.Economy) && Holds(it22, "NM"),
                    F("Italy 2022's shares (AF-6): {0} - the 7-seat NM holds no post rather than one worth twice its due [{1}]", Shares(italy, it22, "FdI", new DateTime(2022, 10, 22)), Describe(it22)));
                Check(Holds(pl23, "KO", CabinetPortfolio.FinanceTreasury, CabinetPortfolio.InteriorJustice, CabinetPortfolio.ForeignAffairs) && Holds(pl23, "TD", CabinetPortfolio.HealthSocialAffairs, CabinetPortfolio.Education, CabinetPortfolio.Economy)
                      && Holds(pl23, "NL", CabinetPortfolio.Defense),
                    F("Poland 2023's shares (AF-6): {0} [{1}]", Shares(poland, pl23, "KO", new DateTime(2023, 12, 13)), Describe(pl23)));

            }
            catch (Exception e) { failures++; sb.Append("    THREW: " + e.GetType().Name + ": " + e.Message + "\n" + e.StackTrace + "\n"); }
            finally { EnergyMarket.ResetTurnState(); }
            if (failures > 0) { Debug.LogError($"PORTFOLIO SALIENCE: {failures} failure(s).\n{sb}"); CheckExit.Finish(1); return; }
            Debug.Log(sb.ToString());
            CheckExit.Finish(0);
        }
    }
}
