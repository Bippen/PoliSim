using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using PoliSim.Data;
using PoliSim.Elections;
using PoliSim.Simulation;
using UnityEngine;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// §716 (Elias's ruling of 2026-10-01, item 7): A PARTNER HOLDING FINANCE HOLDS ITS LEVERS IN FACT, run from its own positions, under a player
    /// chancellor too. Asserted at the start of 6 November 2024 (Scholz's SPD+Grüne+FDP, Finance with the FDP - the record's Lindner): who holds Finance per
    /// country; the FDP's target on the household rates (the seeded rate less a quarter point per CHES point it stands right of the SPD on
    /// `redistribution`); its yearly step written and withdrawn, a rate the ministry wrote left alone; the gate under an SPD player (Finance's levers
    /// refused, the tax programme refused, every other lever the head's); the player's budget stripped of the two rates and nothing else; one turn under
    /// the SPD player and one with no player moving the two rates a step; the FDP player (holding Finance itself) and the instrument's hand untouched;
    /// the cabinet decision resolved on `spendvtax`.
    /// </summary>
    public static class FinancePartnerDiagnostic
    {
        public static void Run()
        {
            CheckExit.ArmLogFold();
            var sb = new StringBuilder();
            int failures = 0;
            sb.Append("=== FinancePartnerDiagnostic (§716): a partner holding Finance holds its levers in fact ===\n");
            void Check(bool ok, string what) { if (!ok) { failures++; } sb.Append(ok ? "    ok        " : "    FAIL      ").Append(what).Append('\n'); }
            using IDisposable epoch = SimulationManager.EpochScope();

            var go = new GameObject("FinancePartnerDiagnostic");
            try
            {
                (SimulationManager sim, World world) = Open(go);
                Country germany = world.GetCountry(CountryId.Germany);

                // (a) who holds Finance at Germany's start, every country
                foreach (Country c in world.Countries)
                {
                    sb.Append("    holder    ").Append(F("{0}: head {1}, Finance {2}{3} -> the partner {4}", c.Id, c.Government?.PmParty ?? "-", FinanceHolder(c) ?? "-", c.Government != null && c.Government.Provisional ? " (a PROVISIONAL stand-in)" : string.Empty, FinancePartner.Holder(c) ?? "none")).Append('\n');
                }
                Check(germany.Government.PmParty == "SPD" && FinancePartner.Holder(germany) == "FDP", F("Germany on 6 November 2024: the FDP holds Finance under the SPD's chancellor (the record's Lindner) - the partner ({0})", FinancePartner.Holder(germany) ?? "none"));
                Country france = world.GetCountry(CountryId.France);
                Check(france.Government.Provisional && FinanceHolder(france) != null && FinanceHolder(france) != france.Government.PmParty && FinancePartner.Holder(france) == null,
                    F("France's PROVISIONAL stand-in seats {0} at Finance under {1} - no partner runs it (the review's decision A: a stand-in is the model's guess at a government)", FinanceHolder(france) ?? "-", france.Government.PmParty));

                // (b) the FDP's target on the household rates
                Check(FinancePartner.TryPosition(CountryId.Germany, "SPD", out float spd) & FinancePartner.TryPosition(CountryId.Germany, "FDP", out float fdp), F("the SPD's and the FDP's redistribution positions read: {0:0.00} and {1:0.00}", spd, fdp));
                TaxLine income = Line(germany, TaxType.IncomeTax), vat = Line(germany, TaxType.VAT);
                foreach (TaxLine line in new[] { income, vat })
                {
                    FinancePartner.Target(germany, line, "FDP", out float target);
                    float seed = TaxSchedule.RateSeedOf(line);
                    float expected = seed - FinancePartner.PointsPerChesPoint * (fdp - spd);
                    Check(Mathf.Abs(target - expected) < 1e-4f && Mathf.Abs(seed - line.Rate) < 1e-4f, F("{0}: seeded {1:0.00}, the FDP ({2:0.00}) against the SPD ({3:0.00}) asks {4:0.000} - the seed less {5:0.000}", line.Type, seed, fdp, spd, target, seed - target));
                }

                // (c) the yearly step written and withdrawn; a rate the ministry wrote is left alone
                var decision = new PolicyDecision();
                FinancePartner.Written wrote = FinancePartner.Apply(germany, decision, sim.CurrentDate);
                Check(wrote.Taxes.Count == 2 && Mathf.Abs(decision.TaxRateOverrides[TaxType.IncomeTax] - (income.Rate - FinancePartner.StepPointsPerYear)) < 1e-4f && Mathf.Abs(decision.TaxRateOverrides[TaxType.VAT] - (vat.Rate - FinancePartner.StepPointsPerYear)) < 1e-4f,
                    F("one year's step: {0}", string.Join("; ", wrote.Moves)));
                FinancePartner.Withdraw(decision, wrote);
                Check(decision.TaxRateOverrides.Count == 0, "withdrawn after the turn, the decision carries nothing of the partner's");
                decision.TaxRateOverrides[TaxType.VAT] = vat.Rate + 1f;
                wrote = FinancePartner.Apply(germany, decision, sim.CurrentDate);
                Check(wrote.Taxes.Count == 1 && wrote.Taxes[0] == TaxType.IncomeTax && Mathf.Abs(decision.TaxRateOverrides[TaxType.VAT] - (vat.Rate + 1f)) < 1e-4f, "a VAT the ministry wrote first is left as written; the income tax steps");

                // (d) the gate under an SPD player (the chancellor's party)
                sim.PlayerCountryId = CountryId.Germany;
                germany.PlayerPartyAbbrev = "SPD";
                Check(sim.FinancePartnerOfPlayer(CountryId.Germany) == "FDP", "the SPD player leads; the FDP's positions run Finance");
                Check(!sim.PlayerMayIntroduce(CountryId.Germany, CabinetPortfolio.FinanceTreasury, out string locked) && locked != null && locked.Contains("HELD BY FDP"), F("Finance's levers refused to the chancellor's party: {0}", locked));
                Check(sim.PlayerMayIntroduce(CountryId.Germany, out _) && sim.PlayerMayIntroduce(CountryId.Germany, CabinetPortfolio.ForeignAffairs, out _) && sim.PlayerMayIntroduce(CountryId.Germany, CabinetPortfolio.InteriorJustice, out _),
                    "the head's own levers and every other portfolio's stay the player's");
                Check(!sim.IntroduceTaxProgramBill(CountryId.Germany, TaxType.WealthTax, true), "the tax programme (a wealth tax implemented) refused - Finance's lever");

                // (e) the player's budget carries everything but the two rates
                TaxLine corporate = Line(germany, TaxType.CorporateTax);
                var bill = new BudgetBill();
                bill.TaxLines[TaxType.IncomeTax] = income.Rate + 2f; bill.TaxLines[TaxType.VAT] = vat.Rate + 1f; bill.TaxLines[TaxType.CorporateTax] = corporate.Rate + 1f;
                bill.BracketRates[TaxType.IncomeTax] = new[] { -1f };
                Check(sim.IntroduceBudgetBill(CountryId.Germany, bill), "the SPD player's budget is introduced");
                Check(!bill.TaxLines.ContainsKey(TaxType.IncomeTax) && !bill.TaxLines.ContainsKey(TaxType.VAT) && !bill.BracketRates.ContainsKey(TaxType.IncomeTax) && bill.TaxLines.ContainsKey(TaxType.CorporateTax),
                    F("its income tax and VAT left out, its corporate tax kept ({0} tax line(s) left)", bill.TaxLines.Count));

                // (g) the FDP player holds Finance itself; the instrument's hand
                germany.PlayerPartyAbbrev = "FDP";
                Check(sim.FinancePartnerOfPlayer(CountryId.Germany) == null && FinancePartner.AiHolder(germany) == null && sim.PlayerMayIntroduce(CountryId.Germany, CabinetPortfolio.FinanceTreasury, out _),
                    "the FDP player, a partner holding Finance, introduces Finance's levers itself (§634) - nothing runs it from positions");
                germany.PlayerPartyAbbrev = string.Empty;
                Check(sim.FinancePartnerOfPlayer(CountryId.Germany) == null && sim.PlayerMayIntroduce(CountryId.Germany, CabinetPortfolio.FinanceTreasury, out _), "the instrument's hand (no party seated): nothing refused, nothing run");

                // (i) the cabinet decision on spendvtax
                // every Finance decision of the pool, read off CabinetSystem's own table: the SPD (spendvtax 2.33) takes the more-state side, the FDP (8.83) the less
                var pool = (System.Collections.IDictionary)typeof(CabinetSystem).GetField("DecisionPool", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static).GetValue(null);
                int financeDecisions = 0, untagged = 0;
                foreach (System.Collections.DictionaryEntry entry in pool)
                {
                    var key = ((CabinetPortfolio, CabinetMinisterPhilosophy))entry.Key;
                    if (key.Item1 != CabinetPortfolio.FinanceTreasury) { continue; }
                    foreach (CabinetDecision real in (List<CabinetDecision>)entry.Value)
                    {
                        financeDecisions++;
                        CabinetDecisionOption bySpd = FinancePartner.Choose(germany, "SPD", real), byFdp = FinancePartner.Choose(germany, "FDP", real);
                        bool tagged = real.Options.Exists(o => o.StateLean == 1) && real.Options.Exists(o => o.StateLean == -1);
                        if (!tagged) { untagged++; }
                        Check(tagged ? bySpd.StateLean == 1 && byFdp.StateLean == -1 : bySpd.Label == real.Options[0].Label && byFdp.Label == real.Options[0].Label,
                            F("'{0}': the SPD {1} ({2:+0;-0;0}), the FDP {3} ({4:+0;-0;0}){5}", real.Name, bySpd.Label, bySpd.BudgetImpact, byFdp.Label, byFdp.BudgetImpact, tagged ? string.Empty : " - no side is more state: the first"));
                    }
                }
                Check(financeDecisions == 6 && untagged == 1, F("{0} Finance decisions, {1} untagged (the windfall's bank-or-announce)", financeDecisions, untagged));
                var choice = new CabinetDecision("Loophole Closure Package", "test", new CabinetDecisionOption("Push it through", budgetImpact: 220f, approvalEffect: -1.5f, stateLean: 1), new CabinetDecisionOption("Water it down first", budgetImpact: 90f, stateLean: -1));
                Check(FinancePartner.Choose(germany, "SPD", choice).Label == "Push it through", "the SPD (spendvtax 2.33, for services) takes the option that raises the most");
                Check(FinancePartner.Choose(germany, "FDP", choice).Label == "Water it down first", "the FDP (spendvtax 8.83, for lower taxes) takes the one that raises the least");
            }
            finally { UnityEngine.Object.DestroyImmediate(go); }

            // (f) one turn with the SPD player, and one with no player: the two rates step by the government standing at the boundary (the player's
            // election may have seated another by then - the boundary follows the year's days, so the government after the turn is the boundary's)
            foreach (string player in new[] { "SPD", null })
            {
                var turnGo = new GameObject("FinancePartnerDiagnostic-turn");
                try
                {
                    (SimulationManager sim, World world) = Open(turnGo);
                    Country germany = world.GetCountry(CountryId.Germany);
                    if (player != null) { sim.PlayerCountryId = CountryId.Germany; germany.PlayerPartyAbbrev = player; }
                    TaxLine income = Line(germany, TaxType.IncomeTax), vat = Line(germany, TaxType.VAT);
                    float incomeBefore = income.Rate, vatBefore = vat.Rate;
                    var decisions = new Dictionary<CountryId, PolicyDecision>();
                    foreach (Country k in world.Countries) { decisions[k.Id] = PolicyDecision.None(); }
                    for (int day = 0; day < SimulationManager.DaysPerTurn; day++) { sim.AdvanceDay(); }
                    sim.AdvanceTurn(decisions);
                    string holder = FinancePartner.AiHolder(germany);
                    float expectedIncome = 0f, expectedVat = 0f;
                    if (holder != null)
                    {
                        FinancePartner.Target(germany, income, holder, out float incomeTarget); FinancePartner.Target(germany, vat, holder, out float vatTarget);
                        expectedIncome = Mathf.Clamp(incomeTarget - incomeBefore, -FinancePartner.StepPointsPerYear, FinancePartner.StepPointsPerYear);
                        expectedVat = Mathf.Clamp(vatTarget - vatBefore, -FinancePartner.StepPointsPerYear, FinancePartner.StepPointsPerYear);
                    }
                    float incomeMove = income.Rate - incomeBefore, vatMove = vat.Rate - vatBefore;
                    string who = player != null ? "the SPD player" : "no player (the AI governs, the ministry first)";
                    Check(holder != null && Mathf.Abs(incomeMove - expectedIncome) < 1e-4f && Mathf.Abs(vatMove - expectedVat) < 1e-4f,
                        F("one turn with {0}, the government at the boundary {1} with Finance's partner {2}: the income tax {3:0.00} -> {4:0.00}, VAT {5:0.00} -> {6:0.00} (the step asked {7:+0.00;-0.00} and {8:+0.00;-0.00})",
                          who, germany.Government.PmParty, holder ?? "none", incomeBefore, income.Rate, vatBefore, vat.Rate, expectedIncome, expectedVat));
                    Check(decisions[CountryId.Germany].TaxRateOverrides.Count == 0, "the decision handed in carries nothing after the turn");
                }
                finally { UnityEngine.Object.DestroyImmediate(turnGo); }
            }

            // (j) the review's defect 1: the player's OWN country under an AI head (the CDU player, in opposition to SPD+Grüne+FDP) - the government's
            // own budget bill carries the partner's step after the ministry's rule, and the boundary takes no step of its own (so never two in a year)
            var billGo = new GameObject("FinancePartnerDiagnostic-bill");
            try
            {
                (SimulationManager sim, World world) = Open(billGo);
                Country germany = world.GetCountry(CountryId.Germany);
                sim.PlayerCountryId = CountryId.Germany;
                germany.PlayerPartyAbbrev = "CDU";
                TaxLine income = Line(germany, TaxType.IncomeTax), vat = Line(germany, TaxType.VAT);
                float incomeBefore = income.Rate, vatBefore = vat.Rate, step = FinancePartner.StepPointsPerYear;
                sim.TableGovernmentBudget(germany);
                BudgetBill bill = sim.GetPendingBudgetBill(CountryId.Germany);
                bool carries = bill != null && bill.TaxLines.TryGetValue(TaxType.IncomeTax, out float billIncome) && Mathf.Abs(billIncome - (incomeBefore - step)) < 1e-4f
                               && bill.TaxLines.TryGetValue(TaxType.VAT, out float billVat) && Mathf.Abs(billVat - (vatBefore - step)) < 1e-4f;
                Check(carries, F("the CDU player in opposition: the government's own budget bill carries the FDP's step - income tax {0:0.00} -> {1}, VAT {2:0.00} -> {3}",
                    incomeBefore, bill != null && bill.TaxLines.TryGetValue(TaxType.IncomeTax, out float shownIncome) ? shownIncome.ToString("0.00", CultureInfo.InvariantCulture) : "none",
                    vatBefore, bill != null && bill.TaxLines.TryGetValue(TaxType.VAT, out float shownVat) ? shownVat.ToString("0.00", CultureInfo.InvariantCulture) : "none"));
                var decisions = new Dictionary<CountryId, PolicyDecision>();
                foreach (Country k in world.Countries) { decisions[k.Id] = PolicyDecision.None(); }
                // the player's day tick runs (the review's second-pass note): it is what counts a pending bill down to its vote, so the bill's step can land
                int billDivisionsBefore = germany.Divisions.Entries.Count;
                for (int day = 0; day < SimulationManager.DaysPerTurn; day++) { if (sim.AdvanceDay()) { sim.AdvanceTurn(decisions); } sim.AdvanceCountryDayTick(CountryId.Germany); }
                if (sim.CurrentTurn == 0) { sim.AdvanceTurn(decisions); }
                float incomeMove = income.Rate - incomeBefore, vatMove = vat.Rate - vatBefore;
                bool voted = sim.GetPendingBudgetBill(CountryId.Germany) == null;
                bool oneStepAtMost = (Mathf.Abs(incomeMove) < 1e-4f || Mathf.Abs(incomeMove + step) < 1e-4f) && (Mathf.Abs(vatMove) < 1e-4f || Mathf.Abs(vatMove + step) < 1e-4f);
                Check(oneStepAtMost && voted, F("a year later the government's bill is {0} and the rates moved {1:+0.00;-0.00;0.00} and {2:+0.00;-0.00;0.00} - the bill's step where the chamber adopted it, never a second, whether a later bill in the year or the boundary ({3} division(s) recorded in the year)",
                    voted ? "voted" : "STILL PENDING", incomeMove, vatMove, germany.Divisions.Entries.Count - billDivisionsBefore));
                Check(germany.FinancePartnerSteppedOn != System.DateTime.MinValue && germany.FinancePartnerSteppedOn.Year == 2024,
                    F("the step's memory: written on {0:yyyy-MM-dd} (the first bill's day) - the bills tabled later in the year carried none (StepDue, once in any {1} days)", germany.FinancePartnerSteppedOn, FinancePartner.StepIntervalDays));
            }
            finally { UnityEngine.Object.DestroyImmediate(billGo); }

            if (failures > 0) { Debug.LogError($"FINANCE PARTNER: {failures} failure(s).\n{sb}"); CheckExit.Finish(1); return; }
            Debug.Log(sb.ToString());
            CheckExit.Finish(0);
        }

        private static (SimulationManager, World) Open(GameObject go)
        {
            WorldClock.ApplyStart(CountryId.Germany);
            SimulationRandom.Seed(777);
            EnergyMarket.ResetCalibration();
            World world = WorldFactory.CreateDefault();
            SimulationManager sim = go.AddComponent<SimulationManager>();
            sim.SetWorld(world);
            return (sim, world);
        }

        private static string FinanceHolder(Country c)
        {
            if (c.Government == null) { return null; }
            foreach (KeyValuePair<string, List<CabinetPortfolio>> held in c.Government.Portfolios) { if (held.Value != null && held.Value.Contains(CabinetPortfolio.FinanceTreasury)) { return held.Key; } }
            return null;
        }

        private static TaxLine Line(Country c, TaxType type) { foreach (TaxLine l in c.TaxLines) { if (l.Type == type) { return l; } } return null; }

        private static string F(string format, params object[] args) => string.Format(CultureInfo.InvariantCulture, format, args);
    }
}
