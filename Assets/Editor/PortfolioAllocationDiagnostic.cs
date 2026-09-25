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
    /// 111) the six portfolios apportion by largest remainder to M 4, KD 1, L 1 with Finance to M (the prime minister's party); every cabinet party
    /// holds at least one; the six are held once each. The gate's portfolio leg: a junior partner introduces the bills of its own portfolios and is
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
                Check(g.Portfolios.Count == 3 && g.Portfolios["M"].Count == 4 && g.Portfolios["KD"].Count == 1 && g.Portfolios["L"].Count == 1, "the six portfolios apportion M 4, KD 1, L 1 by largest remainder on the cabinet's seats");
                Check(g.HoldsPortfolio("M", CabinetPortfolio.FinanceTreasury), "the prime minister's party takes Finance first");
                var seen = new HashSet<CabinetPortfolio>(); int held = 0;
                foreach (KeyValuePair<string, List<CabinetPortfolio>> kv in g.Portfolios) { foreach (CabinetPortfolio p in kv.Value) { seen.Add(p); held++; } }
                Check(seen.Count == 6 && held == 6, F("every portfolio held once ({0} distinct of {1} held)", seen.Count, held));
                Check(!g.HoldsPortfolio("SD", CabinetPortfolio.FinanceTreasury) && g.PortfoliosOf("SD") == "NONE" && g.PortfoliosOf("S") == "NONE", "a support party and the opposition hold none");

                // The gate's portfolio leg: a junior partner's own portfolio passes, the others are refused with the reason, the prime minister's lever too.
                CabinetPortfolio kdHolds = g.Portfolios["KD"][0];
                CabinetPortfolio notKd = CabinetPortfolio.FinanceTreasury;
                sweden.PlayerPartyAbbrev = "KD";
                Check(sim.PlayerMayIntroduce(CountryId.Sweden, kdHolds, out string why) && why == null, F("KD introduces the bills of its own portfolio ({0})", Effectiveness.ShortName(kdHolds)));
                Check(!sim.PlayerMayIntroduce(CountryId.Sweden, notKd, out why) && why == "JUNIOR PARTNER · THE " + Effectiveness.ShortName(notKd).ToUpperInvariant() + " MINISTER'S LEVER · YOURS: " + g.PortfoliosOf("KD"), F("KD is refused the Treasury with the reason: {0}", why));
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
                Check(whatIf.Portfolios.TryGetValue("UG", out List<CabinetPortfolio> ug) && ug.Count == 6, "France's what-if: the player's party holds all six");

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
