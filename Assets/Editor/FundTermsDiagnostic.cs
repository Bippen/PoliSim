using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using PoliSim.Data;
using PoliSim.Elections;
using PoliSim.Simulation;
using UnityEngine;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// AF-9 and AF-35 (Elias's rulings of 2026-10-18, `docs/RULINGS_2026-10-18.md`: q9 "fund terms go to the vote; a dissolved fund's assets are
    /// credited; the budget gates move into the model", q35 "a fund earns on what it holds"): the fund and the budget's gates, each through the game's own
    /// path, each written to fail while the old behaviour holds.
    /// (a) THE VOTE WEIGHS THE FUND'S MONEY TERMS (`ParliamentSystem.FundTermsOf`): a bill restating the fund asks nothing; a contribution a point higher
    /// weighs a point contractionary; a fund created at −10 % of GDP a year (Germany, which holds none at the seed - the premise asserted) weighs ten points expansionary and is asked; a
    /// dissolution weighs the assets it takes out; a part built new for a vote (the tax act) and a continuing resolution weigh no fund term. Until AF-9
    /// every one of these read zero and auto-passed.
    /// (b) A DISSOLVED FUND'S ASSETS ARE CREDITED (GA-12-2): Sweden's fund dissolved by a budget the chamber passes (the house PLANTED to a party the
    /// dissolution's concern puts for it) - the fund gone, the debt lower and the Budget accumulator higher by its assets, the event on the debt ledger;
    /// then a period to the boundary, so the ledger's audit runs on the stock that moved (a red line folds the exit). Until AF-9 the debt did not move.
    /// (c) THE BUDGET GATES ARE THE MODEL'S: the manager refuses a budget off the window (until AF-9 any day was accepted), refuses a figure outside the
    /// line's yearly range (`SimulationManager.YearlyRangeOf`) and accepts one on its end, and refuses an alternative carrying such a figure.
    /// (d) AN EMPTIED FUND EARNS NOTHING (GA-2-10): a PLANTED small Swedish fund at a standing −10 % runs dry within its first period; from the day it is
    /// empty it holds exactly nothing and earns exactly nothing, to the boundary and across it. Until AF-35 it earned the period's flat slice every day
    /// (measured by §814's review). And the rule itself, read off its own method: the whole slice while the PRINCIPAL is whole (the balance less the
    /// period's own returns - the review's M2), none when empty, the principal's share between, an older save's zero read as whole; and a PLANTED loss year
    /// on a fund keeping its principal realises exactly the drawn loss.
    /// Run: `-executeMethod PoliSim.EditorTools.FundTermsDiagnostic.Run`, or by name in the simulation group.
    /// </summary>
    public static class FundTermsDiagnostic
    {
        private const int Seed = 777;

        public static void Run()
        {
            CheckExit.ArmLogFold();
            var sb = new StringBuilder("=== FundTermsDiagnostic (AF-9, AF-35 - rulings q9 and q35): fund terms to the vote, a dissolution credited, the gates in the model, a fund earns on what it holds ===\n");
            int failures = 0;
            void Check(bool ok, string what) { if (!ok) { failures++; } sb.Append(ok ? "    ok        " : "    FAIL      ").Append(what).Append('\n'); }
            var hosts = new List<GameObject>();
            try
            {
                using (SimulationManager.EpochScope())
                {
                    WorldClock.ApplyStart(CountryId.Sweden);   // every world here on Sweden's start (GovernmentBudgetBillDiagnostic's), the process epoch restored after
                    VoteWeighsFund(Check, hosts);
                    DissolutionCredited(Check, hosts);
                    GatesInTheModel(Check, hosts);
                    EmptiedFundEarnsNothing(Check, hosts);
                }
            }
            catch (Exception e) { failures++; sb.Append("    FAIL      threw: ").Append((e.InnerException ?? e).GetType().Name).Append(": ").Append((e.InnerException ?? e).Message).Append('\n'); }
            finally { foreach (GameObject go in hosts) { UnityEngine.Object.DestroyImmediate(go); } }

            sb.Append(failures == 0 ? "=== FundTermsDiagnostic: PASS ===" : F("=== FundTermsDiagnostic: {0} FAILED ===", failures));
            if (failures == 0) { Debug.Log(sb.ToString()); } else { Debug.LogError(sb.ToString()); }
            CheckExit.Finish(failures == 0 ? 0 : 1);
        }

        private static (SimulationManager Sim, World World) Open(List<GameObject> hosts, string name, CountryId player, string party)
        {
            SimulationRandom.Seed(Seed);
            World world = WorldFactory.CreateDefault();
            var go = new GameObject(name);
            hosts.Add(go);
            SimulationManager sim = go.AddComponent<SimulationManager>();
            sim.SetWorld(world);
            sim.PlayerCountryId = player;
            world.GetCountry(player).PlayerPartyAbbrev = party;   // null: the instrument's hand (§629) - the player governs with no party seated
            return (sim, world);
        }

        private static float SpendVsTax(BillConcern concern) => concern.Moves.TryGetValue(StanceAxis.SpendVsTax, out float v) ? v : 0f;

        // (a)
        private static void VoteWeighsFund(Action<bool, string> Check, List<GameObject> hosts)
        {
            (SimulationManager _, World world) = Open(hosts, "FundTermsDiagnostic.vote", CountryId.Sweden, null);
            Country se = world.GetCountry(CountryId.Sweden);
            Country de = world.GetCountry(CountryId.Germany);
            SovereignWealthFund fund = se.SovereignWealthFund;
            Check(fund != null && fund.TotalAssets > 0f && de.SovereignWealthFund == null, F("(a) the premise: Sweden holds a fund ({0:0.###}), Germany none", fund?.TotalAssets ?? 0f));
            if (fund == null) { return; }

            BudgetBill restating = BudgetBillHarness.RestatingFund(se, new BudgetBill());
            BillConcern restatingConcern = ParliamentSystem.GetBudgetBillConcern(se, restating);
            Check(ParliamentSystem.FundTermsOf(se, restating) == (0f, 0f) && restatingConcern.IsEmpty && restatingConcern.Direction == 0f,
                "(a) a bill restating Sweden's fund weighs no fund term - nothing asked, direction 0");

            BudgetBill raise = BudgetBillHarness.RestatingFund(se, new BudgetBill());
            raise.SwfContributionRatePercent = fund.ContributionRatePercent + 1f;
            BillConcern raiseConcern = ParliamentSystem.GetBudgetBillConcern(se, raise);
            Check(Math.Abs(raiseConcern.Direction + 1f) < 1e-5f && Math.Abs(SpendVsTax(raiseConcern) - 1f) < 1e-5f,
                F("(a) Sweden's contribution a point higher ({0:0.##} -> {1:0.##} % of GDP a year) weighs a point contractionary - direction {2:0.#####}, spendvtax {3:+0.#####;-0.#####} (toward the tax end)",
                    fund.ContributionRatePercent, raise.SwfContributionRatePercent, raiseConcern.Direction, SpendVsTax(raiseConcern)));

            var create = new BudgetBill { SwfShouldExist = true, SwfContributionRatePercent = -10f, SwfEquitiesWeight = 40f, SwfBondsWeight = 30f, SwfInfrastructureWeight = 15f, SwfRealEstateWeight = 15f };
            BillConcern createConcern = ParliamentSystem.GetBudgetBillConcern(de, create);
            Check(!createConcern.IsEmpty && Math.Abs(createConcern.Direction - 10f) < 1e-5f && Math.Abs(SpendVsTax(createConcern) + 10f) < 1e-5f,
                F("(a) a fund created in Germany at -10 % of GDP a year is asked - direction {0:0.#####}, spendvtax {1:+0.#####;-0.#####} (until AF-9 nothing was asked and it auto-passed)", createConcern.Direction, SpendVsTax(createConcern)));

            var dissolve = new BudgetBill();   // a whole bill naming no fund: the dissolution
            (float contribution, float dissolved) = ParliamentSystem.FundTermsOf(se, dissolve);
            float expectedDissolved = fund.TotalAssets / se.State.NominalGdp * 100f;
            BillConcern dissolveConcern = ParliamentSystem.GetBudgetBillConcern(se, dissolve);
            Check(Math.Abs(dissolved - expectedDissolved) < 1e-4f && Math.Abs(contribution + fund.ContributionRatePercent) < 1e-6f && Math.Abs(dissolveConcern.Direction - (dissolved + fund.ContributionRatePercent)) < 1e-4f,
                F("(a) dissolving Sweden's fund weighs the {0:0.###} it holds - {1:0.###} % of nominal GDP taken out, the {2:0.##} % contribution ended; direction {3:0.###}", fund.TotalAssets, dissolved, fund.ContributionRatePercent, dissolveConcern.Direction));

            BudgetBill taxAct = dissolve.TaxActPart();
            var resolution = new BudgetBill { ContinuingResolution = true };
            Check(ParliamentSystem.FundTermsOf(se, taxAct) == (0f, 0f) && !taxAct.ChangesFund(se) && ParliamentSystem.FundTermsOf(se, resolution) == (0f, 0f)
                  && ParliamentSystem.FundTermsOf(se, dissolve.PartOf(BudgetBill.StatutePart.Benefits)) == (0f, 0f),
                "(a) a part built new for a vote (the tax act, the benefits act) and a continuing resolution name no fund - no fund term weighed, no fund change read");
        }

        // (b)
        private static void DissolutionCredited(Action<bool, string> Check, List<GameObject> hosts)
        {
            (SimulationManager sim, World world) = Open(hosts, "FundTermsDiagnostic.dissolve", CountryId.Sweden, null);
            Country se = world.GetCountry(CountryId.Sweden);
            SovereignWealthFund fund = se.SovereignWealthFund;
            if (fund == null) { Check(false, "(b) Sweden holds no fund at the seed - VERIFIED NOTHING"); return; }
            var bill = new BudgetBill();   // every line, rate and programme as it stands; the fund dissolved
            BillConcern concern = ParliamentSystem.GetBudgetBillConcern(se, bill);
            // PLANTED: the whole Riksdag to a party the dissolution's own concern puts FOR it, so the vote is not the variable under test
            PartyStance forParty = StanceModel.Stances(se, concern).FirstOrDefault(s => s.Side > 0 && s.Measured);
            if (forParty.Party.Abbrev == null) { Check(false, "(b) no Swedish party stands for the dissolution on any seating - VERIFIED NOTHING"); return; }
            var seats = new Dictionary<string, int>();
            foreach (PoliticalParty p in PartySystems.For(se.Id)) { seats[p.Abbrev] = 0; }
            seats[forParty.Party.Abbrev] = PartySystems.ChamberSeats(se.Id);
            se.ParliamentSeats = seats;
            Check(ParliamentSystem.WouldBillPass(se, concern), F("(b) the premise: the house PLANTED to {0}, which the dissolution's concern puts for it - the bill passes", forParty.Party.Abbrev));

            sim.AdvanceDay();   // the period opened and a day earned on the fund, so the dissolution meets a period carrying the fund's return figures (N3)
            float assets = fund.TotalAssets, debtBefore = se.State.GovernmentDebt, budgetBefore = se.State.Budget;
            bool introduced = BudgetBillHarness.Introduce(sim, CountryId.Sweden, bill);
            for (int day = 0; day <= ParliamentSystem.BillDurationDays && sim.GetPendingBudgetBill(CountryId.Sweden) != null; day++) { sim.AdvanceBudgetBillDay(CountryId.Sweden); }
            bool resolved = sim.GetPendingBudgetBill(CountryId.Sweden) == null;
            float tolerance = 1e-6f * Math.Max(1f, Math.Abs(debtBefore) + assets);
            bool gone = se.SovereignWealthFund == null;
            bool debtOk = Math.Abs(se.State.GovernmentDebt - (debtBefore - assets)) <= tolerance;
            bool budgetOk = Math.Abs(se.State.Budget - (budgetBefore + assets)) <= 1e-6f * Math.Max(1f, Math.Abs(budgetBefore) + assets);
            DebtEventRecord recorded = se.FiscalLedgerAccruing?.Events.LastOrDefault(e => e.Label == "Sovereign fund dissolved");
            bool ledgerOk = recorded != null && Math.Abs(recorded.AppliedDelta + assets) <= tolerance;
            Check(introduced && resolved && gone && debtOk && budgetOk && ledgerOk,
                F("(b) Sweden's fund dissolved by a budget through the game's own path (introduced {0}, resolved {1}): the fund {2}; the debt {3:0.###} -> {4:0.###} ({5}); the Budget accumulator {6:0.###} -> {7:0.###} ({8}); the debt ledger's event {9}",
                    introduced, resolved, gone ? "gone" : "STILL STANDING", debtBefore, se.State.GovernmentDebt, debtOk ? "lower by its " + assets.ToString("0.###", CultureInfo.InvariantCulture) : "WRONG - the assets did not reach the debt",
                    budgetBefore, se.State.Budget, budgetOk ? "credited" : "WRONG", recorded != null ? recorded.AppliedDelta.ToString("0.###", CultureInfo.InvariantCulture) : "MISSING"));

            // the second review's N3: the dissolution zeroes the period's return figures, and a fund created again in the same period inherits none of them
            FieldInfo periodsField = typeof(SimulationManager).GetField("_fiscalPeriods", BindingFlags.Instance | BindingFlags.NonPublic);
            SimulationManager.FiscalPeriod period = (periodsField?.GetValue(sim) as Dictionary<CountryId, SimulationManager.FiscalPeriod>) is Dictionary<CountryId, SimulationManager.FiscalPeriod> periods && periods.TryGetValue(CountryId.Sweden, out SimulationManager.FiscalPeriod seP) ? seP : null;
            Check(period != null && period.PlannedSwfReturn == 0f && period.SwfAssetsAtPeriodOpen == 0f && period.AccruedSwfReturns == 0f,
                F("(b) N3: the dissolution zeroed the period's return figures - planned {0}, open assets {1}, accrued {2}", period?.PlannedSwfReturn ?? float.NaN, period?.SwfAssetsAtPeriodOpen ?? float.NaN, period?.AccruedSwfReturns ?? float.NaN));
            var recreate = new BudgetBill { SwfShouldExist = true, SwfContributionRatePercent = 0f, SwfEquitiesWeight = 55f, SwfBondsWeight = 35f, SwfInfrastructureWeight = 5f, SwfRealEstateWeight = 5f };
            bool recreated = BudgetBillHarness.Introduce(sim, CountryId.Sweden, recreate);
            for (int day = 0; day <= ParliamentSystem.BillDurationDays && sim.GetPendingBudgetBill(CountryId.Sweden) != null; day++) { sim.AdvanceBudgetBillDay(CountryId.Sweden); }
            float plantedAgain = 0f;
            if (se.SovereignWealthFund != null) { plantedAgain = 0.01f * se.State.NominalGdp; se.SovereignWealthFund.TotalAssets = plantedAgain; }   // PLANTED: assets for the new fund to earn on, were it to inherit a return
            float accruedBefore = period?.AccruedSwfReturns ?? float.NaN;
            sim.AdvanceDay();
            Check(recreated && se.SovereignWealthFund != null && period != null && period.AccruedSwfReturns == accruedBefore && period.PlannedSwfReturn == 0f,
                F("(b) N3: a fund created again in the same period (holding {0:0.###}, PLANTED) earns nothing until the next boundary - the period accrued {1} on the day after", plantedAgain, period != null ? (period.AccruedSwfReturns - accruedBefore).ToString("R", CultureInfo.InvariantCulture) : "NO PERIOD"));

            // a period to the boundary: the debt ledger closes and audits the stock it observed against what it recorded - an unrecorded writer is a red line, folded
            var decisions = new Dictionary<CountryId, PolicyDecision>();
            foreach (Country c in world.Countries) { decisions[c.Id] = PolicyDecision.None(); }
            bool boundary = false;
            for (int day = 0; day < SimulationManager.DaysPerTurn + 1 && !boundary; day++) { boundary = sim.AdvanceDay(); }
            if (boundary) { sim.AdvanceTurn(decisions); }
            Check(boundary, "(b) the boundary after the dissolution closed - had the debt ledger's audit failed, its red line would fold this check's exit");
        }

        // (c)
        private static void GatesInTheModel(Action<bool, string> Check, List<GameObject> hosts)
        {
            (SimulationManager sim, World world) = Open(hosts, "FundTermsDiagnostic.gates", CountryId.Sweden, null);
            Country se = world.GetCountry(CountryId.Sweden);
            Check(!sim.GetPendingBudgetProcess(CountryId.Sweden) && !sim.IntroduceBudgetBill(CountryId.Sweden, BudgetBillHarness.RestatingFund(se, new BudgetBill())) && sim.GetPendingBudgetBill(CountryId.Sweden) == null,
                "(c) before the window opens the manager refuses a budget (until AF-9 it accepted one on any day)");
            for (int day = 0; day < 5 && !sim.GetPendingBudgetProcess(CountryId.Sweden); day++) { sim.AdvanceDay(); sim.AdvanceCountryDayTick(CountryId.Sweden); }
            bool open = sim.GetPendingBudgetProcess(CountryId.Sweden);
            Check(open, F("(c) the premise: the player's window opens on the calendar ({0:yyyy-MM-dd})", sim.CurrentDate));

            SpendingLine line = se.SpendingLines.FirstOrDefault(l => l.Amount > 0f);
            if (line == null) { Check(false, "(c) Sweden has no spending line - VERIFIED NOTHING"); return; }
            (float min, float max) = SimulationManager.YearlyRangeOf(se, line);
            BudgetBill outside = BudgetBillHarness.RestatingFund(se, new BudgetBill());
            outside.SpendingNominalTargets[line.Category] = max * 1.01f;
            Check(SimulationManager.BudgetBillOutsideYearlyRange(se, outside) != null && !sim.IntroduceBudgetBill(CountryId.Sweden, outside) && sim.GetPendingBudgetProcess(CountryId.Sweden) && sim.GetPendingBudgetBill(CountryId.Sweden) == null,
                F("(c) a figure past the yearly range ({0} at {1:0.###}, the range {2:0.###} - {3:0.###}) is refused by the manager and the window stays open (until AF-9 only the dial's track held it)", line.Category, outside.SpendingNominalTargets[line.Category], min, max));
            BudgetBill onTheEnd = BudgetBillHarness.RestatingFund(se, new BudgetBill());
            onTheEnd.SpendingNominalTargets[line.Category] = max;
            Check(SimulationManager.BudgetBillOutsideYearlyRange(se, onTheEnd) == null && sim.IntroduceBudgetBill(CountryId.Sweden, onTheEnd) && !sim.GetPendingBudgetProcess(CountryId.Sweden),
                F("(c) a figure on the range's end ({0:0.###}) is introduced, and the window closes", max));

            // the alternative: S in opposition at Sweden's start, the AI government's budget before the chamber (GovernmentBudgetBillDiagnostic's set-up)
            (SimulationManager simS, World worldS) = Open(hosts, "FundTermsDiagnostic.alternative", CountryId.Sweden, "S");
            Country seS = worldS.GetCountry(CountryId.Sweden);
            BudgetBill government = null;
            for (int day = 0; day < 5 && government == null; day++) { simS.AdvanceDay(); simS.AdvanceCountryDayTick(CountryId.Sweden); government = simS.GetPendingBudgetBill(CountryId.Sweden); }
            SpendingLine lineS = seS.SpendingLines.FirstOrDefault(l => l.Amount > 0f);
            if (government == null || !government.GovernmentBill || lineS == null) { Check(false, "(c) the premise: no government budget tabled for S to oppose - VERIFIED NOTHING"); return; }
            (float _, float maxS) = SimulationManager.YearlyRangeOf(seS, lineS);
            BudgetBill alternative = BudgetBillHarness.RestatingFund(seS, new BudgetBill());
            alternative.SpendingNominalTargets[lineS.Category] = maxS * 1.01f;
            bool tabled = simS.TableShadowBudget(CountryId.Sweden, alternative, out string refused);
            Check(!tabled && refused != null && refused.Contains("THIS YEAR'S ALLOWED RANGE") && simS.GetPendingBudgetAlternative(CountryId.Sweden) == null,
                F("(c) an alternative budget carrying a figure past the range is refused: {0}", refused ?? "TABLED"));
        }

        // (d)
        private static void EmptiedFundEarnsNothing(Action<bool, string> Check, List<GameObject> hosts)
        {
            MethodInfo rule = typeof(SimulationManager).GetMethod("SwfReturnNotEarned", BindingFlags.Static | BindingFlags.NonPublic);
            float NotEarned(float slice, float held, float principal, float open) => (float)rule.Invoke(null, new object[] { slice, held, principal, open });
            Check(rule != null && NotEarned(0.5f, 120f, 120f, 100f) == 0f && NotEarned(0.5f, 100f, 100f, 100f) == 0f && NotEarned(0.5f, 0f, 0f, 100f) == 0.5f && NotEarned(-0.5f, 0f, 0f, 100f) == -0.5f
                  && Math.Abs(NotEarned(0.5f, 25f, 25f, 100f) - 0.375f) < 1e-7f && Math.Abs(NotEarned(0.5f, 110f, 90f, 100f) - 0.05f) < 1e-7f && NotEarned(-0.5f, 90f, 100f, 100f) == 0f
                  && NotEarned(0.5f, 3f, 3f, 0f) == 0f && NotEarned(0.5f, 0f, 0f, 0f) == 0.5f,
                "(d) the rule read off its own method: the whole slice earned while the principal is whole, none when empty (either sign), the principal's share between (a quarter kept earns a quarter); the period's own returns are not principal (a balance above its opening on gains with its principal at 90 % earns 90 %) and a loss does not soften itself (a balance below its opening on losses with its principal whole takes the whole loss); an older save's zero read as whole");

            // the review's M2: a LOSS YEAR on a fund that keeps its principal - PLANTED: Sweden's fund at a tenth of GDP paying in a point of GDP a year (more
            // than the structural draw takes), and the opening period's return PLANTED at a fifth of the fund lost. It must realise exactly the drawn loss.
            {
                SimulationRandom.Seed(Seed);
                World lossWorld = WorldFactory.CreateDefault();
                Country lossSe = lossWorld.GetCountry(CountryId.Sweden);
                if (lossSe.SovereignWealthFund == null) { lossSe.SovereignWealthFund = new SovereignWealthFund { EquitiesWeight = 100f }; }
                lossSe.SovereignWealthFund.TotalAssets = 0.1f * lossSe.State.NominalGdp;
                lossSe.SovereignWealthFund.ContributionRatePercent = 1f;
                var lossGo = new GameObject("FundTermsDiagnostic.loss");
                hosts.Add(lossGo);
                SimulationManager lossSim = lossGo.AddComponent<SimulationManager>();
                lossSim.SetWorld(lossWorld);
                MethodInfo seed = typeof(SimulationManager).GetMethod("GetOrSeedFiscalPeriod", BindingFlags.Instance | BindingFlags.NonPublic);
                var lossPeriod = seed?.Invoke(lossSim, new object[] { lossSe }) as SimulationManager.FiscalPeriod;
                if (lossPeriod == null) { Check(false, "(d) the loss year: the opening period could not be seeded - VERIFIED NOTHING"); }
                else
                {
                    lossPeriod.PlannedSwfReturn = -0.2f * lossPeriod.SwfAssetsAtPeriodOpen;
                    int days = 0;
                    bool boundary = false;
                    while (!boundary && days < SimulationManager.DaysPerTurn + 1) { boundary = lossSim.AdvanceDay(); days++; }
                    double drawn = (double)lossPeriod.PlannedSwfReturn * days / SimulationManager.DaysPerTurn;
                    double realised = lossPeriod.AccruedSwfReturns;
                    Check(boundary && Math.Abs(realised - drawn) <= 1e-4 * Math.Abs(drawn),
                        F("(d) a loss year on a fund keeping its principal realises exactly the drawn loss - drawn {0:0.#####} over {1} day(s), realised {2:0.#####} (until the review's M2 the balance's own losses shrank the share, and the loss softened itself)", drawn, days, realised));
                }
            }

            // the second review's N2: a PLANTED deep loss that the zero floor cuts short is accrued as what LANDED, so the principal is not overstated and the
            // next day's contribution is held. PLANTED: Sweden's fund at a hundredth of GDP paying in a point of GDP a year; the period opened on it, then nine
            // tenths of it taken out (standing for a drawdown), and a first day's loss far past what it holds - the floor binds. The second day's return is a
            // whole fund lost over the year (a -100 % draw, a day's slice about one day's contribution): on the principal the floor left, a tenth of it is
            // earned and the contribution held; on an accrual of the loss intended, the principal read whole and the slice swallowed the contribution.
            {
                SimulationRandom.Seed(Seed);
                World floorWorld = WorldFactory.CreateDefault();
                Country floorSe = floorWorld.GetCountry(CountryId.Sweden);
                if (floorSe.SovereignWealthFund == null) { floorSe.SovereignWealthFund = new SovereignWealthFund { EquitiesWeight = 100f }; }
                float opening = 0.01f * floorSe.State.NominalGdp;
                floorSe.SovereignWealthFund.TotalAssets = opening;
                floorSe.SovereignWealthFund.ContributionRatePercent = 1f;
                var floorGo = new GameObject("FundTermsDiagnostic.floor");
                hosts.Add(floorGo);
                SimulationManager floorSim = floorGo.AddComponent<SimulationManager>();
                floorSim.SetWorld(floorWorld);
                MethodInfo seedFloor = typeof(SimulationManager).GetMethod("GetOrSeedFiscalPeriod", BindingFlags.Instance | BindingFlags.NonPublic);
                var floorPeriod = seedFloor?.Invoke(floorSim, new object[] { floorSe }) as SimulationManager.FiscalPeriod;
                if (floorPeriod == null) { Check(false, "(d) N2: the opening period could not be seeded - VERIFIED NOTHING"); }
                else
                {
                    floorSe.SovereignWealthFund.TotalAssets = 0.1f * opening;
                    floorPeriod.PlannedSwfReturn = -20f * opening * SimulationManager.DaysPerTurn;
                    floorSim.AdvanceDay();
                    bool floored = floorSe.SovereignWealthFund.TotalAssets <= 1e-6f * opening;
                    floorPeriod.PlannedSwfReturn = -floorPeriod.SwfAssetsAtPeriodOpen;
                    float contribution = floorSe.State.NominalGdp * 0.01f / SimulationManager.DaysPerTurn;
                    floorSim.AdvanceDay();
                    float heldAfter = floorSe.SovereignWealthFund.TotalAssets;
                    Check(floored && heldAfter > 0.5f * contribution,
                        F("(d) N2: the floor bound on the planted deep loss ({0}); the next day's contribution of {1:0.######} is held - the fund holds {2:0.######} after the day (an accrual of the loss intended swallowed it)",
                            floored ? "the fund at nothing" : "NOT FLOORED", contribution, heldAfter));
                }
            }

            // the third review's P3-1: a drawdown after a loss has accrued takes the period's returns out with the holdings it takes, so the principal keeps its
            // ratio to the holdings. PLANTED: Sweden's fund at a hundredth of GDP paying in a point of GDP a year, its period's return a loss of one and a half
            // times the fund over the year, run for three hundred days (losses taken at the full slice, the principal whole); then an emergency drawdown of 95 %
            // of the holdings (the manager's own apply, by reflection as LeverProbes drives it); then a day whose loss is about one day's contribution.
            {
                SimulationRandom.Seed(Seed);
                World drawWorld = WorldFactory.CreateDefault();
                Country drawSe = drawWorld.GetCountry(CountryId.Sweden);
                if (drawSe.SovereignWealthFund == null) { drawSe.SovereignWealthFund = new SovereignWealthFund { EquitiesWeight = 100f }; }
                float opening = 0.01f * drawSe.State.NominalGdp;
                drawSe.SovereignWealthFund.TotalAssets = opening;
                drawSe.SovereignWealthFund.ContributionRatePercent = 1f;
                var drawGo = new GameObject("FundTermsDiagnostic.drawdown");
                hosts.Add(drawGo);
                SimulationManager drawSim = drawGo.AddComponent<SimulationManager>();
                drawSim.SetWorld(drawWorld);
                MethodInfo seedDraw = typeof(SimulationManager).GetMethod("GetOrSeedFiscalPeriod", BindingFlags.Instance | BindingFlags.NonPublic);
                MethodInfo drawdown = typeof(SimulationManager).GetMethod("ApplySwfDrawdownBillEffects", BindingFlags.Instance | BindingFlags.NonPublic);
                var drawPeriod = seedDraw?.Invoke(drawSim, new object[] { drawSe }) as SimulationManager.FiscalPeriod;
                if (drawPeriod == null || drawdown == null) { Check(false, "(d) P3-1: the opening period or the drawdown's apply could not be reached - VERIFIED NOTHING"); }
                else
                {
                    drawPeriod.PlannedSwfReturn = -1.5f * drawPeriod.SwfAssetsAtPeriodOpen;
                    bool crossed = false;
                    for (int day = 0; day < 300 && !crossed; day++) { crossed = drawSim.AdvanceDay(); }
                    SovereignWealthFund f = drawSe.SovereignWealthFund;
                    double heldBefore = f.TotalAssets, accruedBefore = drawPeriod.AccruedSwfReturns, principalBefore = heldBefore - accruedBefore;
                    drawdown.Invoke(drawSim, new object[] { drawSe, new SwfDrawdownBill { WithdrawalPercentOfGdp = 0.95f * f.TotalAssets / drawSe.State.NominalGdp * 100f } });
                    double heldAfter = f.TotalAssets, principalAfter = heldAfter - drawPeriod.AccruedSwfReturns;
                    double expected = heldBefore > 0.0 ? heldAfter * (principalBefore / heldBefore) : double.NaN;
                    bool ratioKept = !crossed && accruedBefore < 0.0 && heldAfter < heldBefore && Math.Abs(principalAfter - expected) <= 1e-4 * Math.Max(1e-6, Math.Abs(expected));
                    drawPeriod.PlannedSwfReturn = -drawPeriod.SwfAssetsAtPeriodOpen;
                    float contribution = drawSe.State.NominalGdp * 0.01f / SimulationManager.DaysPerTurn;
                    drawSim.AdvanceDay();
                    double gained = f.TotalAssets - heldAfter;
                    Check(ratioKept && gained > 0.5 * contribution,
                        F("(d) P3-1: after a loss of {0:0.######} accrued, a drawdown took the holdings {1:0.######} -> {2:0.######}; the principal {3:0.######} -> {4:0.######} (expected {5:0.######}, its ratio to the holdings kept); the next day's contribution of {6:0.######} held - the fund gained {7:0.######}",
                            accruedBefore, heldBefore, heldAfter, principalBefore, principalAfter, expected, contribution, gained));
                }
            }

            // PLANTED before the world is set: a small Swedish fund at a standing -10 % of GDP a year, so it runs dry inside the first period
            SimulationRandom.Seed(Seed);
            World world = WorldFactory.CreateDefault();
            Country se = world.GetCountry(CountryId.Sweden);
            if (se.SovereignWealthFund == null) { se.SovereignWealthFund = new SovereignWealthFund { EquitiesWeight = 100f }; }
            se.SovereignWealthFund.TotalAssets = 0.005f * se.State.NominalGdp;
            se.SovereignWealthFund.ContributionRatePercent = -10f;
            var go = new GameObject("FundTermsDiagnostic.earns");
            hosts.Add(go);
            SimulationManager sim = go.AddComponent<SimulationManager>();
            sim.SetWorld(world);
            FieldInfo periodsField = typeof(SimulationManager).GetField("_fiscalPeriods", BindingFlags.Instance | BindingFlags.NonPublic);
            SimulationManager.FiscalPeriod Period() => (periodsField?.GetValue(sim) as Dictionary<CountryId, SimulationManager.FiscalPeriod>) is Dictionary<CountryId, SimulationManager.FiscalPeriod> d && d.TryGetValue(CountryId.Sweden, out SimulationManager.FiscalPeriod p) ? p : null;

            var decisions = new Dictionary<CountryId, PolicyDecision>();
            foreach (Country c in world.Countries) { decisions[c.Id] = PolicyDecision.None(); }
            float planted = se.SovereignWealthFund.TotalAssets;
            // "empty" and "nothing" at the cap's float residue (§814: the capped draw lands within a float's last digits of zero), far below the slice the
            // flat return paid an emptied fund each day (the measure of the old behaviour this fails on)
            float emptyBelow = 1e-6f * planted;
            int startTurn = sim.CurrentTurn, emptyDay = -1, emptyTurn = -1, daysChecked = 0, daysAfterBoundary = 0, earningDays = 0, holdingDays = 0;
            float earnedWhileEmpty = 0f, heldWhileEmpty = 0f, plannedAtEmpty = 0f, earnedBelow = 0f;
            double previousEarned = 0.0; int previousTurn = startTurn;
            for (int day = 1; day <= SimulationManager.DaysPerTurn + 30; day++)
            {
                if (sim.AdvanceDay()) { sim.AdvanceTurn(decisions); }
                SimulationManager.FiscalPeriod period = Period();
                if (period == null || se.SovereignWealthFund == null) { break; }
                double earnedSoFar = period.AccruedSwfReturns;
                bool samePeriod = sim.CurrentTurn == previousTurn;
                if (emptyDay >= 0 && day > emptyDay)
                {
                    daysChecked++;
                    if (sim.CurrentTurn != emptyTurn) { daysAfterBoundary++; }
                    if (se.SovereignWealthFund.TotalAssets > emptyBelow) { holdingDays++; heldWhileEmpty = Math.Max(heldWhileEmpty, se.SovereignWealthFund.TotalAssets); }
                    if (samePeriod && Math.Abs(earnedSoFar - previousEarned) > earnedBelow) { earningDays++; earnedWhileEmpty += (float)(earnedSoFar - previousEarned); }
                }
                if (emptyDay < 0 && se.SovereignWealthFund.TotalAssets <= emptyBelow)
                {
                    emptyDay = day; emptyTurn = sim.CurrentTurn; plannedAtEmpty = period.PlannedSwfReturn;
                    earnedBelow = 1e-3f * Math.Abs(plannedAtEmpty) / SimulationManager.DaysPerTurn;   // a thousandth of the day's slice
                }
                previousEarned = earnedSoFar; previousTurn = sim.CurrentTurn;
            }
            SimulationManager.FiscalPeriod last = Period();
            Check(emptyDay > 0 && emptyTurn == startTurn && plannedAtEmpty != 0f,
                F("(d) the premise: the PLANTED fund ({0:0.###}, -10 % of GDP a year) is empty on day {1}, inside its first period, with a planned return of {2:0.#####} still to accrue", planted, emptyDay, plannedAtEmpty));
            Check(daysChecked > 0 && daysAfterBoundary > 0 && holdingDays == 0 && earningDays == 0 && last != null && last.SwfAssetsAtPeriodOpen <= emptyBelow && Math.Abs(last.PlannedSwfReturn) <= emptyBelow,
                F("(d) from the day it is empty the fund holds nothing and earns nothing - {0} day(s) after, {1} of them past the boundary: {2} day(s) holding (at most {3:0.########}), {4} day(s) earning ({5:0.########} in all); the next period's return {6:0.########} drawn on {7:0.########}",
                    daysChecked, daysAfterBoundary, holdingDays, heldWhileEmpty, earningDays, earnedWhileEmpty, last?.PlannedSwfReturn ?? -1f, last?.SwfAssetsAtPeriodOpen ?? -1f));
        }

        private static string F(string format, params object[] args) => string.Format(CultureInfo.InvariantCulture, format, args);
    }
}
