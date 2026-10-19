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
    /// PS-3g (§634): PORTFOLIOS BY GAMSON'S LAW, ASSERTED. On Sweden's government of record (M+KD+L, the 2026 chamber's seats M 70, KD 22, L 19 =
    /// 111 - the start's chamber of record, the 2022 Riksdag, as the line prints) every portfolio apportions by salience-weighted Gamson (§706: each post at Druckman and Warwick's
    /// weight; AF-6: the economy portfolio's too, provisionally the economy ministry's alone, and the rounding's residual to the partners, least above due in proportion) - M 3, KD 2, L 2,
    /// Finance with M, the record's Svantesson; every cabinet party holds at least one; every portfolio is held once. The gate's portfolio leg: a junior partner introduces the bills of its own portfolios and is
    /// refused the others with the reason naming the minister's lever and its own portfolios; the prime minister's party introduces everything;
    /// the opposition nothing. The what-if (France) hands every portfolio to the player's party. The record rides the save (format 31).
    /// </summary>
    public static class PortfolioAllocationDiagnostic
    {
        public static void Run()
        {
            CheckExit.ArmLogFold();
            var sb = new StringBuilder();
            int failures = 0;
            sb.Append("=== PortfolioAllocationDiagnostic (PS-3g, §634): portfolios by seat share ===\n");
            void Check(bool ok, string what) { if (!ok) { failures++; } sb.Append(ok ? "    ok        " : "    FAIL      ").Append(what).Append('\n'); }
            using IDisposable epoch = SimulationManager.EpochScope();
            var go = new GameObject("PortfolioAllocationDiagnostic");
            try
            {
                WorldClock.ApplyStart(CountryId.Sweden);
                SimulationRandom.Seed(777);
                EnergyMarket.ResetCalibration();
                World world = WorldFactory.CreateDefault();
                SimulationManager sim = go.AddComponent<SimulationManager>();
                sim.SetWorld(world);
                sim.PlayerCountryId = CountryId.Sweden;
                Country sweden = world.GetCountry(CountryId.Sweden);
                GovernmentRecord g = sweden.Government;
                int m = sweden.ParliamentSeats["M"], kd = sweden.ParliamentSeats["KD"], l = sweden.ParliamentSeats["L"];
                sb.Append(F("    seats     M {0}, KD {1}, L {2} of the cabinet's {3}\n", m, kd, l, m + kd + l));
                foreach (string party in g.Cabinet) { sb.Append("    holds     ").Append(party).Append(": ").Append(g.PortfoliosOf(party)).Append('\n'); }
                // §706 (Elias's ruling of 2026-10-01): Gamson's law with salience - each post at Druckman & Warwick's Swedish weight, the prime
                // minister's credited to M; the heaviest posts first to the party with the most entitlement outstanding
                Check(g.Portfolios.Count == 3 && g.Portfolios["M"].Count == 3 && g.Portfolios["KD"].Count == 2 && g.Portfolios["L"].Count == 2,
                    F("every portfolio apportions by salience-weighted Gamson (§706), the residual to the partners (AF-6): M {0} · KD {1} · L {2}", g.PortfoliosOf("M"), g.PortfoliosOf("KD"), g.PortfoliosOf("L")));
                Check(g.HoldsPortfolio("M", CabinetPortfolio.FinanceTreasury), "M holds Finance on the weights alone - the Treasury lock is lifted (§706), and the record has Svantesson (M)");
                var seen = new HashSet<CabinetPortfolio>(); int held = 0;
                foreach (KeyValuePair<string, List<CabinetPortfolio>> kv in g.Portfolios) { foreach (CabinetPortfolio p in kv.Value) { seen.Add(p); held++; } }
                int portfolios = Enum.GetValues(typeof(CabinetPortfolio)).Length;
                Check(seen.Count == portfolios && held == portfolios, F("every portfolio held once ({0} distinct of {1} held, of {2})", seen.Count, held, portfolios));
                Check(!g.HoldsPortfolio("SD", CabinetPortfolio.FinanceTreasury) && g.PortfoliosOf("SD") == "NONE" && g.PortfoliosOf("S") == "NONE", "a support party and the opposition hold none");

                // The gate's portfolio leg: a junior partner's own portfolio passes, the others are refused with the reason, the prime minister's lever too.
                // AF-6: the leg is read on a post of KD's other than Finance (a partner holding Finance acts through the stance only, §755) and on the first post it does not hold
                CabinetPortfolio kdHolds = g.Portfolios["KD"].Find(p => p != CabinetPortfolio.FinanceTreasury);
                CabinetPortfolio notKd = Array.Find((CabinetPortfolio[])Enum.GetValues(typeof(CabinetPortfolio)), p => !g.HoldsPortfolio("KD", p));
                sweden.PlayerPartyAbbrev = "KD";
                Check(sim.PlayerMayIntroduce(CountryId.Sweden, kdHolds, out string why) && why == null, F("KD introduces the bills of its own portfolio ({0})", Effectiveness.ShortName(kdHolds)));
                Check(!sim.PlayerMayIntroduce(CountryId.Sweden, notKd, out why) && why == "JUNIOR PARTNER · THE " + Effectiveness.ShortName(notKd).ToUpperInvariant() + " MINISTER'S LEVER · YOURS: " + g.PortfoliosOf("KD"), F("KD is refused {0} with the reason: {1}", Effectiveness.ShortName(notKd), why));
                Check(!sim.PlayerMayIntroduce(CountryId.Sweden, null, out why) && why == "JUNIOR PARTNER · THE PRIME MINISTER'S LEVER · YOURS: " + g.PortfoliosOf("KD") && !sim.PlayerMayIntroduce(CountryId.Sweden, out string whyOne) && whyOne == why, F("KD is refused a lever with no portfolio, by either gate: {0}", why));
                Check(!sim.PlayerGoverns(sweden), "a junior partner does not govern the book (the budget is the coalition agreement's, §5.2)");
                sweden.PlayerPartyAbbrev = "M";
                Check(sim.PlayerMayIntroduce(CountryId.Sweden, notKd, out _) && sim.PlayerMayIntroduce(CountryId.Sweden, null, out _), "the prime minister's party introduces everything");
                sweden.PlayerPartyAbbrev = "S";
                Check(!sim.PlayerMayIntroduce(CountryId.Sweden, kdHolds, out why) && why.StartsWith("IN OPPOSITION", StringComparison.Ordinal), "the opposition introduces nothing, portfolio or not");
                Check(SimulationManager.PortfolioOfLaw(new LawBill { LawId = "cash_bail_reform_act" }) == CabinetPortfolio.InteriorJustice, "a crime-and-justice law belongs to Interior");

                // The what-if: the player's party alone holds every portfolio.
                Country france = world.GetCountry(CountryId.France);
                GovernmentRecord whatIf = GovernmentRecord.WhatIfGoverning(france, "UG", WorldClock.StartDate(CountryId.France), world);
                Check(whatIf.Portfolios.TryGetValue("UG", out List<CabinetPortfolio> ug) && ug.Count == Enum.GetValues(typeof(CabinetPortfolio)).Length, "France's what-if: the player's party holds every portfolio");

                // The record rides the save.
                Persistence.SaveGame save = Persistence.SaveGameService.CreateSaveGame(sim, world, CountryId.Sweden, null);
                Persistence.SaveGame back = Persistence.SaveGameService.Deserialize(Persistence.SaveGameService.Serialize(save));
                GovernmentRecord loaded = back.World.GetCountry(CountryId.Sweden).Government;
                Check(loaded != null && loaded.Portfolios.Count == 3 && loaded.HoldsPortfolio("M", CabinetPortfolio.FinanceTreasury) && loaded.HoldsPortfolio("KD", kdHolds), "the portfolios ride the save (format 31)");
            }
            catch (Exception e) { failures++; sb.Append("    THREW: " + e.GetType().Name + ": " + e.Message + "\n" + e.StackTrace + "\n"); }
            finally { UnityEngine.Object.DestroyImmediate(go); EnergyMarket.ResetTurnState(); }
            if (failures > 0) { Debug.LogError($"PORTFOLIOS: {failures} failure(s).\n{sb}"); CheckExit.Finish(1); return; }
            Debug.Log(sb.ToString());
            CheckExit.Finish(0);
        }

        private static string F(string format, params object[] args) => string.Format(CultureInfo.InvariantCulture, format, args);
    }
}
