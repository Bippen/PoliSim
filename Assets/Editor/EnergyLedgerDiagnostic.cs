using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using PoliSim.Data;
using PoliSim.Data.Generated;
using PoliSim.Simulation;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// EN-4 (2026-09-11), the fiscal layer's proof - it builds and advances worlds, so it belongs to the simulation group. (1) THE SEED: for six,
    /// the stack reproduces Eurostat's components per class to the unit, the state's presented prices are the stored ones, the incidence book
    /// closes. (2) THE MECHANISM: Poland, its fleet's ETS price stepped twenty euro per tonne (a probe on that fleet alone - since EN-4d, §467, the
    /// national carbon tax does not reach ETS-covered plant), ten years, against untouched - the household price HIGHER, the industrial bill HIGHER,
    /// the confidence channel's cumulative contribution LOWER, the fleet's ETS payment HIGHER, the book closing every year in both runs.
    /// (3) THE SUPPORT LINE: France's energy line raised a billion takes the levy down a billion (taxpayers up, the bills down, the scheme's cost
    /// unchanged); cut a billion, up; raised past the whole levy, the levy floors at zero and the rest is taxpayer support with no retail effect.
    /// (4) CONGESTION: Sweden's links scaled to a third of their NTCs - a snitt binds, the rent is positive, and the next year's network component
    /// is lower by the rent over the consumption, both classes alike, the book closing. (5) THE SINGLE BOOK: after the ten years, recomputing the
    /// stack at the standing rate returns the state's stored figures. (6) B6: at a doubled price level the stack's nominal components double.
    /// </summary>
    public static class EnergyLedgerDiagnostic
    {
        private const int Years = 10;

        public static void Run()
        {
            CheckExit.ArmLogFold();
            bool ok = true;
            var sb = new StringBuilder();
            sb.Append("=== ENERGY LEDGER (EN-4): the retail stack, the two ledgers, the support line, the congestion credit, the single book ===\n");

            SimulationRandom.Seed(777);
            EnergyMarket.ResetCalibration();
            EnergyMarket.ProbeLinkCapacityScale = 1.0;
            World world = WorldFactory.CreateDefault();
            EnergyMarket.BeginTurn(world);

            // (1) the seed
            sb.Append("\n    1. THE SEED: Eurostat's components reproduced, the presented figure the stored one, the book closed\n");
            foreach (Country c in world.Countries)
            {
                if (!EnergyLayer.Has(c.Id)) { continue; }
                int ci = EnergyLayer.Index(c.Id); double usd = EnergyLayerData.UsdPerMarketCurrency[ci];   // the book's dollars per unit of the sources' currency
                EnergyMarket.Result r = EnergyMarket.ClearAtSeed(c.Id);
                EnergyLedger.Book b = EnergyLedger.Compute(c, r, 1.0, 0.0);
                for (int k = 0; k < EnergyLedger.ClassCount; k++)
                {
                    EnergyLedger.ClassStack st = b.Classes[k];
                    // the margin and the VAT rate are floats on the seeds - float precision on the sums; the total is the components' sum (Eurostat's stated total differs by the source's rounding, printed by the dump)
                    if (Math.Abs(st.Wholesale + st.Margin - EnergyLayerData.RetailEnergySupply[ci][k] * usd) > 1e-6 || Math.Abs(st.Network - EnergyLayerData.RetailNetwork[ci][k] * usd) > 1e-9 || Math.Abs(st.Policy - EnergyLayerData.RetailPolicy[ci][k] * usd) > 1e-9
                        || Math.Abs(st.TaxEnv - EnergyLayerData.RetailTaxEnv[ci][k] * usd) > 1e-9 || Math.Abs(st.Vat - EnergyLayerData.RetailVat[ci][k] * usd) > 1e-6 || Math.Abs(st.Total - EnergyLedger.SeedTotalPerKwh(ci, k) * usd) > 1e-6)
                    { ok = false; Debug.LogError($"ENERGY LEDGER: {c.Id} {st.Class}: a seed component is not Eurostat's (wholesale+margin {st.Wholesale + st.Margin:F5}, network {st.Network:F5}, policy {st.Policy:F5}, tax {st.TaxEnv:F5}, VAT {st.Vat:F5}, total {st.Total:F5})."); }
                }
                if (Math.Abs(c.State.EnergyHouseholdPrice - b.Classes[0].Total) > 1e-5 || Math.Abs(c.State.EnergyIndustryPrice - b.Classes[1].PreVat) > 1e-5 || Math.Abs(c.State.EnergyIndustryBill - b.Classes[1].Bill) > 1e-4 * Math.Max(1.0, b.Classes[1].Bill))
                { ok = false; Debug.LogError($"ENERGY LEDGER: {c.Id}'s state at the seed ({c.State.EnergyHouseholdPrice}, {c.State.EnergyIndustryPrice}, {c.State.EnergyIndustryBill}) is not the seed book's ({b.Classes[0].Total:F5}, {b.Classes[1].PreVat:F5}, {b.Classes[1].Bill:F4})."); }
                if (Math.Abs(b.Gap) > 1e-9 * Math.Max(1.0, b.PaidTotal)) { ok = false; Debug.LogError($"ENERGY LEDGER: {c.Id}'s book does not close at the seed (gap {b.Gap:E2} bn)."); }
                if (c.Id == CountryId.Germany && b.BudgetSupport != 0.0) { ok = false; Debug.LogError("ENERGY LEDGER: Germany's budget support is not 0 - its book carries no energy line."); }
                sb.Append(F("    {0,-8} {1}/kWh households {2:F4} (VAT {3:F1} %), non-households {4:F4} pre-VAT · wholesale {5:F4} · margins {6:+0.0000;-0.0000} / {7:+0.0000;-0.0000} · bills {8:F1} + {9:F1} bn, industry {10:F2} % of GDP · levy {11:F2} + budget {12:F2} bn · state's taxes {13:F2} · fossil variable cost {14:F1} of wholesale outlay {15:F1} · gap {16:E1}\n",
                    c.Id, EnergyLedger.BookCurrency, b.Classes[0].Total, 100 * c.Environment.RetailVatRate[0], b.Classes[1].PreVat, b.WholesalePerKwh, c.Environment.RetailMargin[0], c.Environment.RetailMargin[1], b.PaidHouseholds, b.PaidNonHouseholds, c.State.EnergyIndustryBillGdpShare, b.LevyRevenue, b.BudgetSupport, b.ToStateTaxes, b.FossilVariableCost, b.WholesaleOutlay, b.Gap));
            }

            // (2) the mechanism, Poland
            sb.Append(F("\n    2. THE MECHANISM: Poland, its fleet's ETS price stepped {0:F0} EUR/t (a probe on that fleet alone; EN-4d: the national carbon tax does not reach ETS-covered plant), ten years, against untouched\n", EnergyMarket.ResponseStepEtsPerT));
            Outcome untouched = RunCountry(CountryId.Poland, Years, 0f, ref ok);
            Outcome raised = RunCountry(CountryId.Poland, Years, EnergyMarket.ResponseStepEtsPerT, ref ok);
            if (!(raised.HouseholdPrice > untouched.HouseholdPrice)) { ok = false; Debug.LogError($"ENERGY LEDGER: Poland's household price did not rise under the raise ({raised.HouseholdPrice:F5} against {untouched.HouseholdPrice:F5})."); }
            if (!(raised.IndustryBill > untouched.IndustryBill)) { ok = false; Debug.LogError($"ENERGY LEDGER: Poland's industrial bill did not rise under the raise ({raised.IndustryBill:F4} against {untouched.IndustryBill:F4})."); }
            if (!(raised.ChannelSum < untouched.ChannelSum)) { ok = false; Debug.LogError($"ENERGY LEDGER: the confidence channel's cumulative contribution did not fall under the raise ({raised.ChannelSum:E3} against {untouched.ChannelSum:E3})."); }
            if (!(raised.EtsCost > untouched.EtsCost)) { ok = false; Debug.LogError("ENERGY LEDGER: the fleet's ETS payment did not rise under the step."); }
            sb.Append(F("    untouched: households {0:F4}, industry {1:F4} USD/kWh, industrial bill {2:F2} bn ({3:F3} % of GDP), channel Σ {4:E2}, the fleet's ETS cost {5:F3} bn, wholesale {6:F4}; stepped: households {7:F4}, industry {8:F4}, bill {9:F2} ({10:F3} %), channel Σ {11:E2}, ETS cost {12:F3}, wholesale {13:F4} - the book closed in every year of both runs (largest gap {14:E1})\n",
                untouched.HouseholdPrice, untouched.IndustryPrice, untouched.IndustryBill, untouched.BillShare, untouched.ChannelSum, untouched.EtsCost, untouched.Wholesale, raised.HouseholdPrice, raised.IndustryPrice, raised.IndustryBill, raised.BillShare, raised.ChannelSum, raised.EtsCost, raised.Wholesale, Math.Max(untouched.MaxGap, raised.MaxGap)));

            // (3) the support line, France
            sb.Append("\n    3. THE SUPPORT LINE: France's energy line moved off its indexed path, the levy the other way\n");
            {
                SimulationRandom.Seed(777); EnergyMarket.ResetCalibration();
                World w = WorldFactory.CreateDefault(); EnergyMarket.BeginTurn(w);
                Country fr = w.GetCountry(CountryId.France);
                SpendingLine line = null; foreach (SpendingLine l in fr.SpendingLines) { if (l.Category == SpendingCategory.Energy) { line = l; } }
                if (line == null) { ok = false; Debug.LogError("ENERGY LEDGER: France carries no energy line."); }
                else
                {
                    double priceIndex = Math.Max(0.0001f, fr.State.PriceLevel);
                    EnergyMarket.Result r = EnergyMarket.Clear(fr);
                    EnergyLedger.Book path = EnergyLedger.Compute(fr, r, priceIndex, 0.0);
                    float seedAmount = line.Amount;
                    line.Amount = seedAmount + 1f; EnergyLedger.Book up = EnergyLedger.Compute(fr, r, priceIndex, 0.0);
                    line.Amount = seedAmount - 1f; EnergyLedger.Book down = EnergyLedger.Compute(fr, r, priceIndex, 0.0);
                    line.Amount = seedAmount + 50f; EnergyLedger.Book past = EnergyLedger.Compute(fr, r, priceIndex, 0.0);
                    line.Amount = seedAmount;
                    // one for one PRE-VAT: the levy is inside households' VAT base, so the bills move by the levy's change plus the VAT on households' share of it - and the state's VAT receipts move with them (the first run of this probe asserted the bills alone and read −1.11 for −1.00: the VAT on the levy)
                    double tol = 1e-6;
                    double vatOnLevy(EnergyLedger.Book a, EnergyLedger.Book b) => (a.Classes[0].Vat - b.Classes[0].Vat) * a.Classes[0].ConsumptionGwh / 1000.0;
                    double billsUp = (path.PaidHouseholds + path.PaidNonHouseholds) - (up.PaidHouseholds + up.PaidNonHouseholds), billsDown = (down.PaidHouseholds + down.PaidNonHouseholds) - (path.PaidHouseholds + path.PaidNonHouseholds), billsPast = (path.PaidHouseholds + path.PaidNonHouseholds) - (past.PaidHouseholds + past.PaidNonHouseholds);
                    if (Math.Abs((up.PaidTaxpayers - path.PaidTaxpayers) - 1.0) > tol || Math.Abs((path.LevyRevenue - up.LevyRevenue) - 1.0) > 1e-6 || Math.Abs(billsUp - (1.0 + vatOnLevy(path, up))) > 1e-6 || Math.Abs(up.SupportCost - path.SupportCost) > 1e-6)
                    { ok = false; Debug.LogError($"ENERGY LEDGER: a billion on France's energy line did not take a billion off the levy - taxpayers +{up.PaidTaxpayers - path.PaidTaxpayers:F4}, levy {path.LevyRevenue:F4} → {up.LevyRevenue:F4}, bills −{billsUp:F4} (the levy and its VAT {vatOnLevy(path, up):F4}), the scheme's cost {path.SupportCost:F4} → {up.SupportCost:F4}."); }
                    if (!(down.LevyScale > 1.0) || Math.Abs((down.LevyRevenue - path.LevyRevenue) - 1.0) > 1e-6 || Math.Abs(billsDown - (1.0 + vatOnLevy(down, path))) > 1e-6) { ok = false; Debug.LogError($"ENERGY LEDGER: a billion cut from France's energy line did not put a billion on the levy (levy {path.LevyRevenue:F4} → {down.LevyRevenue:F4}, bills +{billsDown:F4})."); }
                    if (Math.Abs(past.LevyScale) > 1e-12 || past.LevyRevenue != 0.0 || Math.Abs(billsPast - (path.LevyRevenue + vatOnLevy(path, past))) > 1e-6 || !(past.SupportCost > path.SupportCost))
                    { ok = false; Debug.LogError($"ENERGY LEDGER: past the whole levy the floor did not hold - scale {past.LevyScale}, levy {past.LevyRevenue:F4}, bills moved {billsPast:F4} against the levy {path.LevyRevenue:F4} and its VAT {vatOnLevy(path, past):F4}."); }
                    if (!(up.ToStateTaxes < path.ToStateTaxes)) { ok = false; Debug.LogError("ENERGY LEDGER: the state's VAT receipts did not fall with the levy."); }
                    foreach (EnergyLedger.Book bk in new[] { up, down, past }) { if (Math.Abs(bk.Gap) > 1e-9 * Math.Max(1.0, bk.PaidTotal)) { ok = false; Debug.LogError($"ENERGY LEDGER: the book does not close with the line moved (gap {bk.Gap:E2})."); } }
                    sb.Append(F("    on its path: levy {0:F3} bn (scale 1), budget {1:F2}, households' levy component {2:F5}/kWh; +1 bn: scale {3:F3}, levy {4:F3}, component {5:F5}, bills −{6:F3} (the billion and {7:F3} of VAT on it), taxpayers +{8:F3}, the state's electricity taxes {9:F3} → {10:F3}; −1 bn: scale {11:F3}, levy {12:F3}, bills +{13:F3}; +50 bn: scale {14:F1}, levy {15:F3}, bills −{16:F3}, the scheme's cost {17:F2} → {18:F2} (support above the whole levy stays with the taxpayers)\n",
                        path.LevyRevenue, path.BudgetSupport, path.Classes[0].Policy, up.LevyScale, up.LevyRevenue, up.Classes[0].Policy, billsUp, vatOnLevy(path, up), up.PaidTaxpayers - path.PaidTaxpayers, path.ToStateTaxes, up.ToStateTaxes, down.LevyScale, down.LevyRevenue, billsDown, past.LevyScale, past.LevyRevenue, billsPast, path.SupportCost, past.SupportCost));
                }
            }

            // (4) congestion: a probe on Sweden's links
            sb.Append("\n    4. CONGESTION RENT AND ITS CREDIT: Sweden's links at a third of their NTCs (a probe), the rent read, the next year's network component lower by it\n");
            {
                Country se = world.GetCountry(CountryId.Sweden);
                double priceIndex = Math.Max(0.0001f, se.State.PriceLevel);
                EnergyMarket.Result open = EnergyMarket.Clear(se);
                EnergyLedger.Book before = EnergyLedger.Compute(se, open, priceIndex, 0.0);
                EnergyMarket.ProbeLinkCapacityScale = 1.0 / 3.0;
                EnergyMarket.Result bound = EnergyMarket.Clear(se);
                EnergyMarket.ProbeLinkCapacityScale = 1.0;
                bool anyBinds = false; foreach (EnergyMarket.LinkResult l in bound.Links) { for (int b = 0; b < 3; b++) { anyBinds |= l.Binding[b]; } }
                EnergyLedger.Book year = EnergyLedger.Compute(se, bound, priceIndex, 0.0);
                if (!anyBinds || !(year.CongestionRent > 0)) { ok = false; Debug.LogError($"ENERGY LEDGER: at a third of the NTCs no snitt binds or the rent is not positive ({year.CongestionRent:F4} bn)."); }
                // EN-3b (§466): the zones clear at the exchange's own 2023 prices, whose differences are 2023's congestion on the hour - so the carried flows earn a rent at the dated NTCs without a block binding; the probe's binding adds to it
                if (!(before.CongestionRent > 0.0)) { ok = false; Debug.LogError($"ENERGY LEDGER: Sweden shows no congestion rent at the dated NTCs ({before.CongestionRent:F4} bn) - the zones' sourced price differences should earn one on the carried flows (EN-3b)."); }
                if (!(year.CongestionRent > before.CongestionRent)) { ok = false; Debug.LogError($"ENERGY LEDGER: a binding snitt did not raise the rent above the dated NTCs' ({year.CongestionRent:F4} against {before.CongestionRent:F4} bn)."); }
                EnergyLedger.Book next = EnergyLedger.Compute(se, open, priceIndex, year.CongestionRent * EnergyLedger.CongestionRentCreditShare);
                double totalCons = 0; int sci = EnergyLayer.Index(CountryId.Sweden); for (int k = 0; k < EnergyLedger.ClassCount; k++) { totalCons += EnergyLayerData.RetailConsumptionGwh[sci][k]; }
                double expectedCredit = year.CongestionRent * 1000.0 / totalCons;
                for (int k = 0; k < EnergyLedger.ClassCount; k++)
                {
                    double drop = before.Classes[k].Network - next.Classes[k].Network;
                    if (Math.Abs(drop - Math.Min(expectedCredit, before.Classes[k].Network)) > 1e-9) { ok = false; Debug.LogError($"ENERGY LEDGER: the credit did not reach {next.Classes[k].Class}' network component ({drop:F6} against {expectedCredit:F6} per kWh)."); }
                }
                if (Math.Abs(next.NetworkCreditPerKwh - expectedCredit) > 1e-12 || Math.Abs(next.Gap) > 1e-9 * Math.Max(1.0, next.PaidTotal)) { ok = false; Debug.LogError("ENERGY LEDGER: the credited year's book does not close or the credit is not the rent over the consumption."); }
                if (!(next.ToNetworks < before.ToNetworks)) { ok = false; Debug.LogError("ENERGY LEDGER: the networks' receipts did not fall under the credit."); }
                sb.Append(F("    at the NTCs: rent {0:F4} bn; at a third: {1} · rent {2:F3} bn ({3} USD per kWh over {4:N0} GWh) · next year's network component households {5:F4} → {6:F4}, non-households {7:F4} → {8:F4} USD/kWh · networks' receipts {9:F2} → {10:F2} bn · gap {11:E1}\n",
                    before.CongestionRent, BindingText(bound), year.CongestionRent, expectedCredit.ToString("F5", CultureInfo.InvariantCulture), totalCons, before.Classes[0].Network, next.Classes[0].Network, before.Classes[1].Network, next.Classes[1].Network, before.ToNetworks, next.ToNetworks, next.Gap));
            }

            // (5) the single book, after the ten years (the raised run's world is gone; re-run the untouched world and compare its state against a recomputation)
            sb.Append("\n    5. THE SINGLE BOOK: after ten years the stored figures are the stack recomputed at the standing rate\n");
            {
                Outcome again = RunCountry(CountryId.Poland, Years, 0f, ref ok);
                if (again.RecomputeGapMax > 1e-5) { ok = false; Debug.LogError($"ENERGY LEDGER: after ten years a stored figure diverges from its recomputation by {again.RecomputeGapMax:E2} (relative)."); }
                sb.Append(F("    six countries, largest relative divergence between the state's prices and bill and the stack recomputed: {0:E2}\n", again.RecomputeGapMax));
            }

            // (6) B6
            sb.Append("\n    6. B6: Poland's stack at the seed dispatch, price index 1 against 2\n");
            {
                Country pl = world.GetCountry(CountryId.Poland);
                EnergyMarket.Result r1 = EnergyMarket.ClearAt(pl.Id, 1.0, 0.0), r2 = EnergyMarket.ClearAt(pl.Id, 2.0, 0.0);
                EnergyLedger.Book b1 = EnergyLedger.Compute(pl, r1, 1.0, 0.0), b2 = EnergyLedger.Compute(pl, r2, 2.0, 0.0);
                for (int k = 0; k < EnergyLedger.ClassCount; k++)
                {
                    if (Math.Abs(b2.Classes[k].Network - 2 * b1.Classes[k].Network) > 1e-9 || Math.Abs(b2.Classes[k].Policy - 2 * b1.Classes[k].Policy) > 1e-9 || Math.Abs(b2.Classes[k].TaxEnv - 2 * b1.Classes[k].TaxEnv) > 1e-9 || Math.Abs(b2.Classes[k].Margin - 2 * b1.Classes[k].Margin) > 1e-9)
                    { ok = false; Debug.LogError($"ENERGY LEDGER: at a doubled price level {b2.Classes[k].Class}' nominal components do not double."); }
                }
                sb.Append(F("    households: network {0:F4} → {1:F4}, levies {2:F4} → {3:F4}, tax {4:F4} → {5:F4}, margin {6:F4} → {7:F4}, wholesale {8:F4} → {9:F4} (every cost doubles - the fitted adders with them since EN-5, the ETS with the level; the ceiling does not - the market's own B6, §460, §465; the national carbon tax is not in the stack since EN-4d)\n",
                    b1.Classes[0].Network, b2.Classes[0].Network, b1.Classes[0].Policy, b2.Classes[0].Policy, b1.Classes[0].TaxEnv, b2.Classes[0].TaxEnv, b1.Classes[0].Margin, b2.Classes[0].Margin, b1.WholesalePerKwh, b2.WholesalePerKwh));
            }

            sb.Append(ok ? "\n=== EnergyLedgerDiagnostic: ALL ASSERTIONS PASS ===\n" : "\n=== EnergyLedgerDiagnostic: FAILED (see above) ===\n");
            if (ok) { Debug.Log(sb.ToString()); } else { Debug.LogError(sb.ToString()); }
            CheckExit.Finish(ok ? 0 : 1);
        }

        private sealed class Outcome
        {
            public double HouseholdPrice, IndustryPrice, IndustryBill, BillShare, ChannelSum, EtsCost, Wholesale, MaxGap, RecomputeGapMax;
        }

        private static Outcome RunCountry(CountryId player, int years, float etsStepPerT, ref bool ok)
        {
            SimulationRandom.Seed(777);
            EnergyMarket.ResetCalibration();
            World world = WorldFactory.CreateDefault();   // resets the probes (ResetTurnState) - the step is set after it
            EnergyMarket.ProbeEtsRisePerT = etsStepPerT; EnergyMarket.ProbeEtsRiseOnly = player;   // EN-4d: the probe on the player's fleet alone, from the top of year 1's turn (the ETS is set at the turn's top, as BeginTurn reads it)
            var go = new GameObject("ENERGYLEDGER");
            try
            {
                SimulationManager sim = go.AddComponent<SimulationManager>();
                sim.SetWorld(world);
                sim.PlayerCountryId = player;
                Country c = world.GetCountry(player);
                var decisions = new Dictionary<CountryId, PolicyDecision>();
                foreach (Country k in world.Countries) { decisions[k.Id] = PolicyDecision.None(); }
                var outcome = new Outcome();
                var creditBefore = new Dictionary<CountryId, double>();
                for (int year = 1; year <= years; year++)
                {
                    for (int day = 0; day < SimulationManager.DaysPerTurn; day++) { sim.AdvanceDay(); }
                    // the credit the coming boundary will carry - last year's rent above the seed's - read BEFORE the turn writes this year's rent (EN-3b: Sweden's zones earn a standing rent)
                    foreach (Country k in world.Countries) { if (EnergyLayer.Has(k.Id)) { creditBefore[k.Id] = EnergyLedger.CreditFor(k); } }
                    sim.AdvanceTurn(decisions);
                    outcome.ChannelSum += -MacroSystem.EnergyConfidenceSensitivity * c.State.EnergyIndustryBillShareChange;
                    // the book closes every year, for every covered country
                    foreach (Country k in world.Countries)
                    {
                        if (!EnergyLayer.Has(k.Id)) { continue; }
                        EnergyLedger.Book bk = EnergyLedger.Compute(k, EnergyMarket.Clear(k), Math.Max(0.0001f, k.State.PriceLevel), creditBefore[k.Id]);
                        outcome.MaxGap = Math.Max(outcome.MaxGap, Math.Abs(bk.Gap));
                        if (Math.Abs(bk.Gap) > 1e-9 * Math.Max(1.0, bk.PaidTotal)) { ok = false; Debug.LogError($"ENERGY LEDGER: {k.Id}'s book does not close in year {year} (gap {bk.Gap:E2})."); }
                        if (year == years)
                        {
                            // the single book: the stored figures against the stack recomputed with the credit the boundary carried
                            double g = Math.Max(Math.Abs(k.State.EnergyHouseholdPrice - bk.Classes[0].Total) / Math.Max(1e-6, bk.Classes[0].Total), Math.Abs(k.State.EnergyIndustryPrice - bk.Classes[1].PreVat) / Math.Max(1e-6, bk.Classes[1].PreVat));
                            g = Math.Max(g, Math.Abs(k.State.EnergyIndustryBill - bk.Classes[1].Bill) / Math.Max(1e-6, bk.Classes[1].Bill));
                            outcome.RecomputeGapMax = Math.Max(outcome.RecomputeGapMax, g);
                        }
                    }
                }
                EnergyLedger.Book last = EnergyLedger.Compute(c, EnergyMarket.Clear(c), Math.Max(0.0001f, c.State.PriceLevel), 0.0);
                outcome.HouseholdPrice = c.State.EnergyHouseholdPrice; outcome.IndustryPrice = c.State.EnergyIndustryPrice; outcome.IndustryBill = c.State.EnergyIndustryBill; outcome.BillShare = c.State.EnergyIndustryBillGdpShare;
                outcome.EtsCost = last.EtsCost; outcome.Wholesale = last.WholesalePerKwh;
                return outcome;
            }
            finally { UnityEngine.Object.DestroyImmediate(go); EnergyMarket.ProbeEtsRisePerT = 0.0; EnergyMarket.ProbeEtsRiseOnly = null; }
        }

        private static string BindingText(EnergyMarket.Result r)
        {
            var parts = new List<string>();
            foreach (EnergyMarket.LinkResult l in r.Links) { for (int b = 0; b < 3; b++) { if (l.Binding[b]) { parts.Add(l.From + "→" + l.To + " " + EnergyLayerData.DispatchBlocks[b]); } } }
            return parts.Count == 0 ? "no snitt binds" : "BINDS: " + string.Join(", ", parts);
        }

        private static string F(string format, params object[] args) => string.Format(CultureInfo.InvariantCulture, format, args);
    }
}
