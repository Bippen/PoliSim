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
    /// §755 (Elias's ruling A2: "Finance partner: nothing is frozen. A partner holding Finance acts through the fiscal stance only. The rule is symmetric:
    /// it is the same whether the player or the AI holds Finance."), replacing §716's household-rate step and its refusals. Asserted at the start of
    /// 6 November 2024 (Scholz's SPD+Grüne+FDP, the record's Lindner PLANTED at Finance): who holds Finance per country; the stance the FDP asks (a quarter
    /// point of GDP per CHES `lrecon` point it stands right of the SPD - a tightening); one year's step through the ministry's uniform instrument, written,
    /// counted and withdrawn, a line the ministry wrote left alone; nothing frozen under an SPD player (Finance's levers, the tax programme, the budget's
    /// household rates and the Finance decision all the head's); the FDP player holding Finance as a partner - its dial, the same step and pace, Finance's
    /// other levers refused; one turn under the SPD player and one with no player moving the stance a step; the government's own bill carrying the step
    /// where the AI leads the player's country; a new holder counting from zero; once in any year.
    /// </summary>
    public static class FinancePartnerDiagnostic
    {
        /// <summary>CONVENTION - the check's tolerance on a stance, points of GDP: the lines' clamps and float sums move the last digits.</summary>
        private const float StanceTolerance = 0.005f;

        public static void Run()
        {
            CheckExit.ArmLogFold();
            var sb = new StringBuilder();
            int failures = 0;
            sb.Append("=== FinancePartnerDiagnostic (§755, ruling A2): nothing frozen - a partner holding Finance acts through the fiscal stance only, the same for the player and the AI ===\n");
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
                    bool planted = c.Id == CountryId.Germany || c.Id == CountryId.France;
                    sb.Append("    holder    ").Append(F("{0}: head {1}, Finance {2}{3}{5} -> the partner {4}", c.Id, c.Government?.PmParty ?? "-", FinanceHolder(c) ?? "-", c.Government != null && c.Government.Provisional ? " (a PROVISIONAL stand-in)" : string.Empty, FinancePartner.Holder(c) ?? "none",
                        planted ? " - PLANTED by this check (§753: the allocation seats the head's own party there)" : string.Empty)).Append('\n');
                }
                Check(germany.Government.PmParty == "SPD" && FinancePartner.Holder(germany) == "FDP", F("Germany on 6 November 2024, the record's Lindner PLANTED at Finance: the FDP holds it under the SPD's chancellor - the partner ({0})", FinancePartner.Holder(germany) ?? "none"));
                Country france = world.GetCountry(CountryId.France);
                Check(france.Government.Provisional && FinanceHolder(france) != null && FinanceHolder(france) != france.Government.PmParty && FinancePartner.Holder(france) == null,
                    F("France's PROVISIONAL stand-in, {0} PLANTED at Finance under {1} - no partner runs it (§716's decision A: a stand-in is the model's guess at a government)", FinanceHolder(france) ?? "-", france.Government.PmParty));

                // (b) the stance the FDP asks
                Check(FinancePartner.TryEconomicPosition(CountryId.Germany, "SPD", out float spd) & FinancePartner.TryEconomicPosition(CountryId.Germany, "FDP", out float fdp), F("the SPD's and the FDP's lrecon read: {0:0.00} and {1:0.00}", spd, fdp));
                FinancePartner.Target(germany, "FDP", out float asked);
                Check(Mathf.Abs(asked - FinancePartner.StancePointsPerChesPoint * (spd - fdp)) < 1e-4f && asked < 0f,
                    F("the FDP, {0:0.00} CHES points right of the SPD, asks {1:+0.000;-0.000} pp of GDP - a tightening", fdp - spd, asked));

                // (c) one year's step: written through the uniform instrument, counted, withdrawn; a line the ministry wrote is left alone
                float gdp = germany.State.NominalGdp;
                var decision = new PolicyDecision();
                FinancePartner.Written wrote = FinancePartner.Apply(germany, decision, sim.CurrentDate);
                float cut = 0f; bool uniform = true; float first = float.NaN;
                foreach (SpendingCategory c in wrote.Lines)
                {
                    float pct = decision.SpendingLineChanges[c];
                    if (float.IsNaN(first)) { first = pct; } else if (Mathf.Abs(pct - first) > 1e-4f) { uniform = false; }
                    cut += LineOf(germany, c).Amount * -pct / 100f;
                }
                Check(wrote.Lines.Count > 0 && uniform && first < 0f && Mathf.Abs(wrote.StancePoints + FinancePartner.StepPointsPerYear) < StanceTolerance && Mathf.Abs(cut / gdp * 100f + wrote.StancePoints) < StanceTolerance && wrote.Holder == "FDP",
                    F("one year's step: {0} - {1} line(s) cut {2:0.000} % each, {3:0.000} pp of GDP taken", string.Join("; ", wrote.Moves), wrote.Lines.Count, -first, cut / gdp * 100f));
                FinancePartner.Withdraw(decision, wrote);
                Check(decision.SpendingLineChanges.Count == 0 && decision.TaxRateOverrides.Count == 0, "withdrawn after the turn, the decision carries nothing of the partner's");
                SpendingCategory claimed = germany.SpendingLines[0].Category;
                decision.SpendingLineChanges[claimed] = 1f;
                wrote = FinancePartner.Apply(germany, decision, sim.CurrentDate);
                Check(!wrote.Lines.Contains(claimed) && Mathf.Abs(decision.SpendingLineChanges[claimed] - 1f) < 1e-4f && Mathf.Abs(wrote.StancePoints + FinancePartner.StepPointsPerYear) < StanceTolerance,
                    F("a line the ministry wrote first ({0}) is left as written; the other lines carry the whole step ({1:+0.000;-0.000} pp)", claimed, wrote.StancePoints));
                FinancePartner.Withdraw(decision, wrote);
                decision.SpendingLineChanges.Remove(claimed);

                // (k) the count: a step recorded moves it; a new holder starts from zero; once in any year
                germany.FinanceStanceHolder = "Grune"; germany.FinanceStanceApplied = -1f;
                germany.FinanceStanceGovernment = germany.Government.FormedOn;
                Check(Mathf.Abs(FinancePartner.Applied(germany, "FDP")) < 1e-6f && Mathf.Abs(FinancePartner.Applied(germany, "Grune") + 1f) < 1e-6f, "the count is the holder's own: the FDP starts from zero where another party moved the stance before it");
                germany.FinanceStanceHolder = "FDP"; germany.FinanceStanceGovernment = germany.Government.FormedOn.AddDays(-400);
                Check(Mathf.Abs(FinancePartner.Applied(germany, "FDP")) < 1e-6f, "the review's defect 2: and the government's own - the same FDP's count from a government before this one reads zero (its target is read against the head that sits)");
                germany.FinanceStanceHolder = null; germany.FinanceStanceApplied = 0f;
                wrote = FinancePartner.Apply(germany, decision, sim.CurrentDate);
                FinancePartner.Record(germany, wrote, sim.CurrentDate);
                FinancePartner.Withdraw(decision, wrote);
                FinancePartner.Written again = FinancePartner.Apply(germany, decision, sim.CurrentDate.AddDays(200));
                Check(germany.FinanceStanceHolder == "FDP" && Mathf.Abs(germany.FinanceStanceApplied - wrote.StancePoints) < 1e-6f && !again.Any,
                    F("recorded: the FDP has moved {0:+0.000;-0.000} pp; 200 days later nothing is due (once in any {1} days)", germany.FinanceStanceApplied, FinancePartner.StepIntervalDays));
                germany.FinancePartnerSteppedOn = DateTime.MinValue; germany.FinanceStanceHolder = null; germany.FinanceStanceApplied = 0f;

                // (d) nothing frozen under an SPD player (the chancellor's party)
                sim.PlayerCountryId = CountryId.Germany;
                germany.PlayerPartyAbbrev = "SPD";
                Check(sim.FinancePartnerOfPlayer(CountryId.Germany) == "FDP", "the SPD player leads; the FDP holds Finance");
                Check(sim.PlayerMayIntroduce(CountryId.Germany, CabinetPortfolio.FinanceTreasury, out string locked) && locked == null, "Finance's levers are the chancellor's party's - nothing refused (§716's refusal gone)");
                Check(sim.IntroduceTaxProgramBill(CountryId.Germany, TaxType.WealthTax, true), "the tax programme (a wealth tax implemented) introduced - no longer held");

                // (e) the player's budget carries every rate, the household rates included
                TaxLine income = Line(germany, TaxType.IncomeTax), vat = Line(germany, TaxType.VAT), corporate = Line(germany, TaxType.CorporateTax);
                var bill = new BudgetBill();
                bill.TaxLines[TaxType.IncomeTax] = income.Rate + 2f; bill.TaxLines[TaxType.VAT] = vat.Rate + 1f; bill.TaxLines[TaxType.CorporateTax] = corporate.Rate + 1f;
                bill.BracketRates[TaxType.IncomeTax] = new[] { -1f };
                Check(sim.IntroduceBudgetBill(CountryId.Germany, bill), "the SPD player's budget is introduced");
                Check(bill.TaxLines.ContainsKey(TaxType.IncomeTax) && bill.TaxLines.ContainsKey(TaxType.VAT) && bill.BracketRates.ContainsKey(TaxType.IncomeTax) && bill.TaxLines.ContainsKey(TaxType.CorporateTax),
                    F("its income tax, VAT and corporate tax all carried ({0} tax line(s))", bill.TaxLines.Count));

                // (g) the FDP player holds Finance as a partner under the SPD's chancellor: symmetric - its dial, the same step; Finance's other levers refused
                germany.PlayerPartyAbbrev = "FDP";
                Check(FinancePartner.PlayerHolds(germany) && sim.FinancePartnerOfPlayer(CountryId.Germany) == null, "the FDP player is the partner holding Finance");
                Check(!sim.PlayerMayIntroduce(CountryId.Germany, CabinetPortfolio.FinanceTreasury, out string partnerLock) && partnerLock != null && partnerLock.Contains("FISCAL STANCE"),
                    F("Finance's other levers refused to the player partner, as the AI partner holds none of them: {0}", partnerLock));
                Check(sim.SetFinanceStanceTarget(CountryId.Germany, 9f) && Mathf.Abs(germany.FinanceStancePlayerTarget - FinancePartner.PlayerTargetLimit) < 1e-6f, F("the dial reaches {0:0.0} pp of GDP either way, no further (asked +9.0)", FinancePartner.PlayerTargetLimit));
                Check(sim.SetFinanceStanceTarget(CountryId.Germany, 1f) && FinancePartner.Target(germany, "FDP", out float dial) && Mathf.Abs(dial - 1f) < 1e-6f, "the player's dial is the stance it asks (+1.00)");
                wrote = FinancePartner.Apply(germany, decision, sim.CurrentDate);
                float given = 0f;
                foreach (SpendingCategory c in wrote.Lines) { given += LineOf(germany, c).Amount * decision.SpendingLineChanges[c] / 100f; }
                Check(wrote.Lines.Count > 0 && wrote.Taxes.Count == 0 && Mathf.Abs(wrote.StancePoints - FinancePartner.StepPointsPerYear) < StanceTolerance && Mathf.Abs(given / gdp * 100f - wrote.StancePoints) < StanceTolerance,
                    F("the same step for the player's stance: {0} - the lines restored, {1:+0.000;-0.000} pp of GDP", string.Join("; ", wrote.Moves), given / gdp * 100f));
                FinancePartner.Withdraw(decision, wrote);
                germany.PlayerPartyAbbrev = "SPD";
                Check(!sim.SetFinanceStanceTarget(CountryId.Germany, 1f), "the SPD player (the head) sets no partner's stance - the FDP's is read from its position");
                germany.PlayerPartyAbbrev = string.Empty;
                Check(sim.FinancePartnerOfPlayer(CountryId.Germany) == null && sim.PlayerMayIntroduce(CountryId.Germany, CabinetPortfolio.FinanceTreasury, out _), "the instrument's hand (no party seated): nothing refused");
            }
            finally { UnityEngine.Object.DestroyImmediate(go); }

            // (f) one turn with the SPD player, and one with no player: the stance steps by the government standing at the boundary - the ministry's claim
            // first where the AI governs (its lines taken, the step falls to the household rates)
            foreach (string player in new[] { "SPD", null })
            {
                var turnGo = new GameObject("FinancePartnerDiagnostic-turn");
                try
                {
                    (SimulationManager sim, World world) = Open(turnGo);
                    Country germany = world.GetCountry(CountryId.Germany);
                    if (player != null) { sim.PlayerCountryId = CountryId.Germany; germany.PlayerPartyAbbrev = player; }
                    var decisions = new Dictionary<CountryId, PolicyDecision>();
                    foreach (Country k in world.Countries) { decisions[k.Id] = PolicyDecision.None(); }
                    for (int day = 0; day < SimulationManager.DaysPerTurn; day++) { sim.AdvanceDay(); }
                    sim.AdvanceTurn(decisions);
                    string holder = FinancePartner.Holder(germany);
                    string who = player != null ? "the SPD player" : "no player (the AI governs, the ministry first)";
                    bool stepped = holder != null && germany.FinanceStanceHolder == holder && Mathf.Abs(germany.FinanceStanceApplied + FinancePartner.StepPointsPerYear) < StanceTolerance && germany.FinancePartnerSteppedOn != DateTime.MinValue;
                    // where the AI governs, the treaty's rule writes first; had it written every line and both rates, the partner would have nothing left to
                    // move that year - stated, not excused: the check prints which
                    bool claimedAll = player == null && germany.FinancePartnerSteppedOn == DateTime.MinValue && Mathf.Abs(germany.FinanceStanceApplied) < 1e-6f;
                    Check(holder != null && (stepped || claimedAll),
                        F("one turn with {0}, the government at the boundary {1} with Finance's partner {2}: {3}",
                          who, germany.Government.PmParty, holder ?? "none", stepped ? F("the stance moved {0:+0.000;-0.000} pp of GDP, recorded on {1:yyyy-MM-dd}", germany.FinanceStanceApplied, germany.FinancePartnerSteppedOn)
                            : claimedAll ? "THE MINISTRY'S RULE CLAIMED EVERY LINE AND RATE FIRST - no step this year" : F("MOVED {0:+0.000;-0.000} pp", germany.FinanceStanceApplied)));
                    Check(decisions[CountryId.Germany].SpendingLineChanges.Count == 0 && decisions[CountryId.Germany].TaxRateOverrides.Count == 0, "the decision handed in carries nothing after the turn");
                }
                finally { UnityEngine.Object.DestroyImmediate(turnGo); }
            }

            // (j) the player's OWN country under an AI head (the CDU player, in opposition to SPD+Grüne+FDP): the government's own budget bill carries the
            // partner's step after the ministry's rule, and the boundary takes no step of its own (so never two in a year)
            var billGo = new GameObject("FinancePartnerDiagnostic-bill");
            try
            {
                (SimulationManager sim, World world) = Open(billGo);
                Country germany = world.GetCountry(CountryId.Germany);
                sim.PlayerCountryId = CountryId.Germany;
                germany.PlayerPartyAbbrev = "CDU";
                sim.TableGovernmentBudget(germany);
                BudgetBill bill = sim.GetPendingBudgetBill(CountryId.Germany);
                float tabled = bill != null ? bill.FinanceStancePoints : 0f;
                Check(bill != null && bill.FinanceStanceHolder == "FDP" && Mathf.Abs(tabled + FinancePartner.StepPointsPerYear) < StanceTolerance && (bill.SpendingPercentChanges.Count > 0 || bill.TaxLines.Count > 0)
                      && Mathf.Abs(germany.FinanceStanceApplied) < 1e-6f && germany.FinancePartnerSteppedOn != System.DateTime.MinValue,
                    F("the CDU player in opposition: the government's own budget bill carries the FDP's step ({0:+0.000;-0.000} pp; {1} line(s), {2} rate(s) on the bill) - the year's step spent, nothing counted until the chamber adopts it (the review's defect 1)", tabled, bill?.SpendingPercentChanges.Count ?? 0, bill?.TaxLines.Count ?? 0));
                var decisions = new Dictionary<CountryId, PolicyDecision>();
                foreach (Country k in world.Countries) { decisions[k.Id] = PolicyDecision.None(); }
                for (int day = 0; day < SimulationManager.DaysPerTurn; day++) { if (sim.AdvanceDay()) { sim.AdvanceTurn(decisions); } sim.AdvanceCountryDayTick(CountryId.Germany); }
                if (sim.CurrentTurn == 0) { sim.AdvanceTurn(decisions); }
                bool voted = sim.GetPendingBudgetBill(CountryId.Germany) == null;
                string verdict = null;
                foreach (DivisionRecord d in germany.Divisions.Entries) { if (d.Title != null && d.Title.StartsWith("Annual budget", StringComparison.Ordinal)) { verdict = d.Title; break; } }
                bool adopted = verdict != null && (verdict.Contains("the government's bill adopted") || verdict.Contains("the government's frames adopted"));
                float expected = adopted ? tabled : 0f;
                Check(voted && verdict != null && Mathf.Abs(germany.FinanceStanceApplied - expected) < 1e-6f && germany.FinancePartnerSteppedOn.Year == 2024,
                    F("a year later the government's bill is {0} ('{1}') and the count {2:+0.000;-0.000} pp - {3}; never a second step in the year (spent on {4:yyyy-MM-dd})",
                        voted ? "voted" : "STILL PENDING", verdict ?? "no budget division", germany.FinanceStanceApplied, adopted ? "the step counted on adoption" : "rejected, nothing counted", germany.FinancePartnerSteppedOn));

                // the rework pass's note: the ADOPTION branch, driven directly - the tabled bill's step counted when it is adopted under the government that
                // tabled it, and not where another government sits by the vote's day
                var adoptedBill = new BudgetBill { FinanceStanceHolder = "FDP", FinanceStancePoints = -FinancePartner.StepPointsPerYear, FinanceStanceAppliedBefore = 0f, FinanceStanceGovernment = germany.Government.FormedOn };
                germany.FinanceStanceHolder = null; germany.FinanceStanceApplied = 0f;
                FinancePartner.CreditAdopted(germany, adoptedBill);
                bool counted = germany.FinanceStanceHolder == "FDP" && germany.FinanceStanceGovernment == germany.Government.FormedOn && Mathf.Abs(germany.FinanceStanceApplied + FinancePartner.StepPointsPerYear) < 1e-6f;
                germany.FinanceStanceHolder = null; germany.FinanceStanceApplied = 0f;
                adoptedBill.FinanceStanceGovernment = germany.Government.FormedOn.AddDays(-30);
                FinancePartner.CreditAdopted(germany, adoptedBill);
                bool notOthers = germany.FinanceStanceHolder == null && Mathf.Abs(germany.FinanceStanceApplied) < 1e-6f;
                Check(counted && notOthers, F("adopted, the bill's step is counted ({0}); tabled by a government that no longer sits, it is not ({1})", counted ? "-0.250 pp, the FDP's, this government's" : "NOT COUNTED", notOthers ? "nothing counted" : "COUNTED"));
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
            PlantRecordFinance(world.GetCountry(CountryId.Germany), "FDP");
            PlantRecordFinance(world.GetCountry(CountryId.France), "RN");   // decision A's test needs a non-head party at a stand-in's Finance (§753 seats the head's ENS there)
            SimulationManager sim = go.AddComponent<SimulationManager>();
            sim.SetWorld(world);
            return (sim, world);
        }

        /// <summary>
        /// §753 (Elias's ruling A3): Germany's BMBF summed as Druckman &amp; Warwick sum a merged post moves the 2021 chamber's Finance from the FDP to
        /// the SPD - a miss against the record's Lindner, recorded and not adjusted (`PortfolioSalienceDiagnostic`). This check is the partner's
        /// machinery, not the allocation's, so it plants the record's: Finance with <paramref name="party"/>, the post it held going to the party that held Finance.
        /// </summary>
        private static void PlantRecordFinance(Country country, string party)
        {
            GovernmentRecord g = country?.Government;
            if (g == null || !g.Portfolios.TryGetValue(party, out List<CabinetPortfolio> its) || its.Contains(CabinetPortfolio.FinanceTreasury)) { return; }
            foreach (KeyValuePair<string, List<CabinetPortfolio>> held in g.Portfolios)
            {
                if (held.Value == null || !held.Value.Contains(CabinetPortfolio.FinanceTreasury)) { continue; }
                CabinetPortfolio given = its.Count > 0 ? its[its.Count - 1] : CabinetPortfolio.Education;
                held.Value.Remove(CabinetPortfolio.FinanceTreasury);
                if (its.Count > 0) { its.Remove(given); held.Value.Add(given); }
                its.Add(CabinetPortfolio.FinanceTreasury);
                return;
            }
        }

        private static string FinanceHolder(Country c)
        {
            if (c.Government == null) { return null; }
            foreach (KeyValuePair<string, List<CabinetPortfolio>> held in c.Government.Portfolios) { if (held.Value != null && held.Value.Contains(CabinetPortfolio.FinanceTreasury)) { return held.Key; } }
            return null;
        }

        private static TaxLine Line(Country c, TaxType type) { foreach (TaxLine l in c.TaxLines) { if (l.Type == type) { return l; } } return null; }

        private static SpendingLine LineOf(Country c, SpendingCategory category) { foreach (SpendingLine l in c.SpendingLines) { if (l.Category == category) { return l; } } return null; }

        private static string F(string format, params object[] args) => string.Format(CultureInfo.InvariantCulture, format, args);
    }
}
