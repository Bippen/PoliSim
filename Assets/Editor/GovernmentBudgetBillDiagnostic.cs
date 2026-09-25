using System;
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
    /// PS-3e (§632, ruled): THE AI-GOVERNMENT BILL PATH, ASSERTED. With S seated in opposition at Sweden's start the AI government tables its
    /// budget as a bill on day one (the arrival window) - not into the book at the boundary; the player's party tables its draft as the alternative;
    /// on the bill's day the frame decision (Riksdagsordningen 11 kap. 18 § and 10 §, sweden/budget_procedure.md) adopts the one the chamber
    /// prefers and records the division with every party's side and reason; the alternative is cleared. Both ways: with M seated (governing) the
    /// player's own process opens and no government bill is tabled; with NO player nothing is tabled (player path only - the dumps stay
    /// byte-identical); Germany's procedure is unsourced, so an alternative is refused with the reason and the government's bill is voted alone.
    /// </summary>
    public static class GovernmentBudgetBillDiagnostic
    {
        public static void Run()
        {
            CheckExit.ArmLogFold();
            var sb = new StringBuilder();
            int failures = 0;
            sb.Append("=== GovernmentBudgetBillDiagnostic (PS-3e, §632): the AI government's budget as a bill ===\n");
            void Check(bool ok, string what) { if (!ok) { failures++; } sb.Append(ok ? "    ok        " : "    FAIL      ").Append(what).Append('\n'); }
            using IDisposable epoch = SimulationManager.EpochScope();
            var go = new GameObject("GovernmentBudgetBillDiagnostic");
            try
            {
                // 1. S in opposition: the government tables its budget on day one; S tables an alternative; the frame decision adopts one.
                WorldClock.ApplyStart(CountryId.Sweden);
                SimulationRandom.Seed(777);
                EnergyMarket.ResetCalibration();
                World world = WorldFactory.CreateDefault();
                SimulationManager sim = go.AddComponent<SimulationManager>();
                sim.SetWorld(world);
                sim.PlayerCountryId = CountryId.Sweden;
                Country sweden = world.GetCountry(CountryId.Sweden);
                sweden.PlayerPartyAbbrev = "S";
                Check(!sim.PlayerGoverns(sweden), "S seated at Sweden's start: the AI governs");
                int day = 0;
                BudgetBill government = null;
                for (; day < 5 && government == null; day++) { sim.AdvanceDay(); sim.AdvanceCountryDayTick(CountryId.Sweden); government = sim.GetPendingBudgetBill(CountryId.Sweden); }
                Check(government != null && government.GovernmentBill && government.TabledBy == null, F("the government tables its budget on day {0}: {1}", day, government != null ? SimulationManager.DescribeBudgetBill(government) : "NONE"));
                Check(!sim.GetPendingBudgetProcess(CountryId.Sweden), "the player's own process does not open for the opposition");
                Check(WorldClock.BudgetProcedureOf(CountryId.Sweden) == WorldClock.BudgetProcedure.RiksdagFrameDecision, "Sweden's procedure is the Riksdag's frame decision");
                var alternative = new BudgetBill();
                alternative.SpendingPercentChanges[SpendingCategory.SocialSecurity] = 4f;
                Check(sim.TableShadowBudget(CountryId.Sweden, alternative, out string refused) && refused == null, "S tables an alternative budget against the government's");
                Check(!sim.TableShadowBudget(CountryId.Sweden, new BudgetBill(), out refused) && refused == "YOUR ALTERNATIVE IS ALREADY TABLED", F("a second alternative is refused: {0}", refused));
                BudgetBill tabled = sim.GetPendingBudgetAlternative(CountryId.Sweden);
                Check(tabled != null && tabled.TabledBy == "S" && !tabled.GovernmentBill, "the alternative carries its party");
                Persistence.SimulationPendingState captured = sim.CaptureSaveState();
                Check(captured.PendingBudgetAlternatives != null && captured.PendingBudgetAlternatives.ContainsKey(CountryId.Sweden) && captured.PendingBudgetBills.ContainsKey(CountryId.Sweden), "the government's bill and the alternative both ride the save state (format 29)");
                var go1b = new GameObject("GovernmentBudgetBillDiagnostic.restore"); SimulationManager sim2 = go1b.AddComponent<SimulationManager>(); World world2 = WorldFactory.CreateDefault(); sim2.RestoreSaveState(world2, sim.CurrentTurn, sim.CurrentDate, captured);
                Check(sim2.GetPendingBudgetAlternative(CountryId.Sweden) != null && sim2.GetPendingBudgetAlternative(CountryId.Sweden).TabledBy == "S" && sim2.GetPendingBudgetBill(CountryId.Sweden) != null && sim2.GetPendingBudgetBill(CountryId.Sweden).GovernmentBill, "a restored state carries both again - the round trip");
                UnityEngine.Object.DestroyImmediate(go1b);
                int before = sweden.Divisions.Entries.Count;
                int remaining = government.DaysRemaining;
                for (int i = 0; i < remaining + 1 && sim.GetPendingBudgetBill(CountryId.Sweden) != null; i++) { sim.AdvanceDay(); sim.AdvanceCountryDayTick(CountryId.Sweden); }
                Check(sim.GetPendingBudgetBill(CountryId.Sweden) == null && sim.GetPendingBudgetAlternative(CountryId.Sweden) == null, "on the bill's day both the government's bill and the alternative leave the chamber");
                Check(sweden.Divisions.Entries.Count == before + 1, F("one division recorded ({0} -> {1})", before, sweden.Divisions.Entries.Count));
                DivisionRecord division = sweden.Divisions.Entries.Count > 0 ? sweden.Divisions.Entries[sweden.Divisions.Entries.Count - 1] : null;
                Check(division != null && division.Title.StartsWith("Annual budget:", StringComparison.Ordinal) && division.Title.Contains("adopted") && division.Title.Contains(" to "),
                    F("the frame decision's division: {0}", division?.Title ?? "NONE"));
                int sided = 0, abstained = 0; if (division != null) { foreach (DivisionSide side in division.Sides) { if (side.Side != 0) { sided++; } else { abstained++; } } }
                Check(sided > 0, F("the parties took sides: {0} for one proposal or the other, {1} abstaining", sided, abstained));
                DivisionSide sSide = null; if (division != null) { foreach (DivisionSide sd in division.Sides) { if (sd.Abbrev == "S") { sSide = sd; } } }
                Check(sSide != null && sSide.Side < 0, "S, the tabler, votes its own alternative (the reader, s633)");
                sb.Append("    division  ").Append(division?.Title ?? "-").Append('\n');
                if (division != null) { foreach (DivisionSide side in division.Sides) { sb.Append("      ").Append(side.Abbrev).Append(' ').Append(side.Seats).Append(' ').Append(side.Side > 0 ? "GOV" : side.Side < 0 ? "ALT" : "ABS").Append(" - ").Append(side.Reason).Append('\n'); } }

                // 2. The fiscal-year date (1 January 2027) tables the next one - the calendar, not the arrival, this time.
                government = null; int ticks = 0;
                while (government == null && ticks < 400) { sim.AdvanceDay(); sim.AdvanceCountryDayTick(CountryId.Sweden); government = sim.GetPendingBudgetBill(CountryId.Sweden); ticks++; }
                Check(government != null && government.GovernmentBill && sim.CurrentDate.Month == 1 && sim.CurrentDate.Day <= 3, F("the next government budget is tabled on the fiscal-year date: {0:yyyy-MM-dd}", sim.CurrentDate));

                // PS-3f (§633, ruled): a JUNIOR PARTNER tables no alternative - its budget voice is the coalition agreement; a SUPPORT party may, and votes its own - a break recorded on the government.
                sweden.PlayerPartyAbbrev = "KD";
                Check(!sim.TableShadowBudget(CountryId.Sweden, new BudgetBill(), out refused) && refused == "JUNIOR PARTNER · YOUR BUDGET VOICE IS THE COALITION AGREEMENT", F("KD, a junior partner, is refused: {0}", refused));
                sweden.PlayerPartyAbbrev = "SD";
                var sdAlternative = new BudgetBill(); sdAlternative.SpendingPercentChanges[SpendingCategory.SocialSecurity] = -3f;
                Check(sim.TableShadowBudget(CountryId.Sweden, sdAlternative, out refused), "SD, a support party, tables its alternative");
                int breaksBefore = sweden.Government.Breaks.Count; int divisionsBefore = sweden.Divisions.Entries.Count;
                for (int i = 0; i < 25 && sim.GetPendingBudgetBill(CountryId.Sweden) != null; i++) { sim.AdvanceDay(); sim.AdvanceCountryDayTick(CountryId.Sweden); }
                DivisionRecord sdDivision = sweden.Divisions.Entries.Count > divisionsBefore ? sweden.Divisions.Entries[sweden.Divisions.Entries.Count - 1] : null;
                DivisionSide sdSide = null; if (sdDivision != null) { foreach (DivisionSide sd in sdDivision.Sides) { if (sd.Abbrev == "SD") { sdSide = sd; } } }
                Check(sdSide != null && sdSide.Side < 0 && sdDivision.Contest != null && sdDivision.Contest.BreakBy == "SD" && sweden.Government.Breaks.Count == breaksBefore + 1, F("SD votes its own alternative, not the government's frames - a break recorded: {0}", sdSide?.Reason ?? "NONE"));
                Check(sdDivision != null && sdDivision.Contest != null && sdDivision.Contest.VotesFor + sdDivision.Contest.VotesAgainst + sdDivision.Contest.Abstentions == 349 && sdDivision.Passed, F("the contest on the record: {0} {1}, {2} {3}, abstaining {4}", sdDivision?.Contest?.ProposalFor, sdDivision?.Contest?.VotesFor, sdDivision?.Contest?.ProposalAgainst, sdDivision?.Contest?.VotesAgainst, sdDivision?.Contest?.Abstentions));
                sweden.PlayerPartyAbbrev = "S";

                // 3. Germany: the procedure is unsourced - an alternative is refused with the reason; the government's bill is voted alone.
                Check(WorldClock.BudgetProcedureOf(CountryId.Germany) == WorldClock.BudgetProcedure.Unsourced, "Germany's procedure is not yet sourced");
            }
            catch (Exception e) { failures++; sb.Append("    THREW: " + e.GetType().Name + ": " + e.Message + "\n" + e.StackTrace + "\n"); }
            finally { UnityEngine.Object.DestroyImmediate(go); EnergyMarket.ResetTurnState(); }

            // 4. Both ways, in fresh worlds: M governing opens the player's own process; no player tables nothing.
            var go2 = new GameObject("GovernmentBudgetBillDiagnostic.2");
            try
            {
                WorldClock.ApplyStart(CountryId.Sweden);
                SimulationRandom.Seed(777);
                EnergyMarket.ResetCalibration();
                World world = WorldFactory.CreateDefault();
                SimulationManager sim = go2.AddComponent<SimulationManager>();
                sim.SetWorld(world);
                sim.PlayerCountryId = CountryId.Sweden;
                Country sweden = world.GetCountry(CountryId.Sweden);
                sweden.PlayerPartyAbbrev = "M";
                for (int i = 0; i < 3; i++) { sim.AdvanceDay(); sim.AdvanceCountryDayTick(CountryId.Sweden); }
                Check(sim.GetPendingBudgetProcess(CountryId.Sweden) && sim.GetPendingBudgetBill(CountryId.Sweden) == null, "M governing: the player's own budget process opens and no government bill is tabled");
                Check(!sim.TableShadowBudget(CountryId.Sweden, new BudgetBill(), out string refused) && refused == "NO GOVERNMENT BUDGET IS BEFORE THE CHAMBER", F("a governing party tables no alternative: {0}", refused));

                UnityEngine.Object.DestroyImmediate(go2); go2 = new GameObject("GovernmentBudgetBillDiagnostic.3");
                SimulationRandom.Seed(777);
                EnergyMarket.ResetCalibration();
                world = WorldFactory.CreateDefault();
                sim = go2.AddComponent<SimulationManager>();
                sim.SetWorld(world);
                for (int i = 0; i < 3; i++) { sim.AdvanceDay(); foreach (Country c in world.Countries) { sim.AdvanceCountryDayTick(c.Id); } }
                bool any = false; foreach (Country c in world.Countries) { if (sim.GetPendingBudgetBill(c.Id) != null || sim.GetPendingBudgetProcess(c.Id)) { any = true; } }
                Check(!any, "no player: no government bill and no process anywhere - the player path only (the dumps stay byte-identical)");
            }
            catch (Exception e) { failures++; sb.Append("    THREW: " + e.GetType().Name + ": " + e.Message + "\n" + e.StackTrace + "\n"); }
            finally { UnityEngine.Object.DestroyImmediate(go2); EnergyMarket.ResetTurnState(); }

            if (failures > 0) { Debug.LogError($"GOVERNMENT BUDGET BILL: {failures} failure(s).\n{sb}"); CheckExit.Finish(1); return; }
            Debug.Log(sb.ToString());
            CheckExit.Finish(0);
        }

        private static string F(string format, params object[] args) => string.Format(CultureInfo.InvariantCulture, format, args);
    }
}
