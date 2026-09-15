using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
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
    /// (7) EN-7a: the Energy sector's subsidy at 80 through the boundary's own pressures - France's and the USA's cost on the energy line and out of
    /// the other sectors' support target (the USA's Commerce unmoved; its price unmoved, no levy), the levy down one for one; Germany (no energy line)
    /// its cost left in that target; Poland's regulation twenty points either side of its anchor moving the pre-tax ratio by exactly (1 − k × gap)
    /// with suppliers' receipts unchanged; every class's seeded components positive at Regulation 0 and 100 in all six; the size, all six.
    /// (8) EN-7b: the electricity tax - households' statute +10 and firms' −10 EUR/MWh on every covered country: the component moves by the change
    /// within its coverage (Poland's capped at 1, not 41), firms never below the EU minimum (France's already there moves nothing), households' VAT on
    /// the move, the revenue change the classes' shifts on their consumption, the book closed, the USA untouched to the bit; a statute of zero leaves
    /// the component at or above zero; and through a real boundary - Germany's household relief enacted, the next boundary plans the flow the
    /// ledger computed, the year after books it as the report's "of which", and a repeal returns the statute to its base bit for bit.
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

            // (7) EN-7a: the Energy sector's dials onto the instruments
            sb.Append("\n    7. EN-7a: THE SUBSIDY ON THE ENERGY LINE (retail intervention), THE REGULATION GAP MOVING THE PRE-TAX RATIO (market liberalisation)\n");
            {
                const BindingFlags instance = BindingFlags.Instance | BindingFlags.NonPublic;
                MethodInfo energyPressure = typeof(SimulationManager).GetMethod("ApplyEnergySupportCostPressure", instance);
                MethodInfo sectorPressure = typeof(SimulationManager).GetMethod("ApplySectorSupportCostPressure", instance);
                if (energyPressure == null || sectorPressure == null) { ok = false; Debug.LogError("ENERGY LEDGER: the support pressures were not found by reflection - EN-7a is UNVERIFIED."); }
                else
                {
                    // France: an energy line and a levy, the support cost on Business and industry (SC-1) · the USA: an energy line, no levy, Commerce (the double-booking guard) ·
                    // Germany: no energy line - the subsidy's cost lands with the other sectors' support on Business and industry
                    foreach (CountryId id in new[] { CountryId.France, CountryId.USA, CountryId.Germany })
                    {
                        SimulationRandom.Seed(777); EnergyMarket.ResetCalibration();
                        World w = WorldFactory.CreateDefault(); EnergyMarket.BeginTurn(w);
                        var go = new GameObject("EN7A");
                        try
                        {
                            SimulationManager sim = go.AddComponent<SimulationManager>();
                            sim.SetWorld(w);
                            Country k = w.GetCountry(id);
                            SpendingLine energyLine = SectorCouplings.EnergyLine(k), supportLine = SectorCouplings.SupportLine(k);
                            Sector energy = null; foreach (Sector s in k.Sectors) { if (s.Type == SectorType.Energy) { energy = s; } }
                            double priceIndex = Math.Max(0.0001f, k.State.PriceLevel);
                            EnergyMarket.Result r = EnergyMarket.Clear(k);
                            EnergyLedger.Book before = EnergyLedger.Compute(k, r, priceIndex, 0.0);
                            float energy0 = energyLine != null ? energyLine.Amount : 0f, support0 = supportLine != null ? supportLine.Amount : 0f;
                            double sectorTarget0 = SectorCouplings.SupportCostTarget(k);
                            energy.SubsidyLevel = 80f;
                            energyPressure.Invoke(sim, new object[] { k });
                            sectorPressure.Invoke(sim, new object[] { k });
                            EnergyLedger.Book after = EnergyLedger.Compute(k, r, priceIndex, 0.0);
                            double target = SectorCouplings.EnergySupportCostTarget(k);
                            double subsidyCost = SectorCouplings.SupportCost(k.State.NominalGdp, 80f, SectorCouplings.NeutralDialLevel, SectorCouplings.NeutralDialLevel);
                            double lineMove = energyLine != null ? energyLine.Amount - energy0 : 0.0, supportMove = supportLine != null ? supportLine.Amount - support0 : 0.0;
                            double sectorMove = SectorCouplings.SupportCostTarget(k) - sectorTarget0;
                            string landing = supportLine != null ? F("{0} {1:+0.000;-0.000}", supportLine.Category, supportMove) : "no sector-support line in this book";
                            if (energyLine != null)
                            {
                                bool levyOneForOne = Math.Abs((before.LevyRevenue - after.LevyRevenue) - Math.Min(lineMove, before.LevyRevenue)) <= 1e-6 * Math.Max(1.0, before.LevyRevenue);
                                bool noLevyNoPrice = before.LevyRevenue > 0.0 || Math.Abs(after.Classes[0].Total - before.Classes[0].Total) <= 1e-12;
                                // SC-1 (ruled 2026-09-15): the cost sits outside the line's seed band - the whole target lands, whatever the band would have held
                                if (Math.Abs(target - subsidyCost) > 1e-4 || Math.Abs(lineMove - target) > 1e-3 * Math.Max(1.0, target)
                                    || Math.Abs(sectorMove) > 1e-4 || Math.Abs(supportMove) > 1e-4 || !levyOneForOne || !noLevyNoPrice)
                                { ok = false; Debug.LogError($"ENERGY LEDGER: {id}'s subsidy at 80 did not land on its energy line alone, one for one - target {target:F4} against the cost {subsidyCost:F4}, line +{lineMove:F4}, the other sectors' target +{sectorMove:F4}, {landing}, levy {before.LevyRevenue:F4} → {after.LevyRevenue:F4}, households' price {before.Classes[0].Total:F6} → {after.Classes[0].Total:F6}."); }
                                sb.Append(F("    {0,-8} subsidy 80: the cost {1:F3} bn on the energy line (moved {2:+0.000;-0.000}); the other sectors' support target {3:+0.000;-0.000}, {4}; levy {5:F3} → {6:F3} bn{7}, households' price {8:F5} → {9:F5}/kWh; the book closes ({10:E1})\n",
                                    id, target, lineMove, sectorMove, landing, before.LevyRevenue, after.LevyRevenue, before.LevyRevenue > 0.0 ? F(" (scale {0:F3})", after.LevyScale) : " - no levy in the stack to displace, no retail effect", before.Classes[0].Total, after.Classes[0].Total, after.Gap));
                            }
                            else
                            {
                                // SC-1: the other sectors' support now lands on a line - the subsidy's cost must land there with it, whole
                                bool landsWithSupport = supportLine != null && Math.Abs(supportMove - subsidyCost) <= 1e-3 * Math.Max(1.0, subsidyCost);
                                if (Math.Abs(target) > 1e-9 || Math.Abs(sectorMove - subsidyCost) > 1e-3 * Math.Max(1.0, subsidyCost) || !landsWithSupport || Math.Abs(after.LevyScale - before.LevyScale) > 1e-12)
                                { ok = false; Debug.LogError($"ENERGY LEDGER: {id} has no energy line but its subsidy did not land with the other sectors' support - energy target {target:F4}, that target +{sectorMove:F4} against the cost {subsidyCost:F4}, {landing}, levy scale {before.LevyScale} → {after.LevyScale}."); }
                                sb.Append(F("    {0,-8} subsidy 80: no energy line - the cost {1:F3} bn lands with the other sectors' support (their target moved {2:+0.000;-0.000}; {3}); the levy scale unchanged at {4:F3}\n",
                                    id, subsidyCost, sectorMove, landing, after.LevyScale));
                            }
                            if (Math.Abs(after.Gap) > 1e-9 * Math.Max(1.0, after.PaidTotal)) { ok = false; Debug.LogError($"ENERGY LEDGER: {id}'s book does not close with the subsidy at 80 (gap {after.Gap:E2})."); }
                        }
                        finally { UnityEngine.Object.DestroyImmediate(go); }
                    }
                }

                // the regulation gap: Poland 20 points below and above its anchor - the pre-tax ratio (non-households ÷ households) by exactly (1 ∓ k × 0.2);
                // on a world at its seed, where each class's stack is the catalog's (the rule is sized on the seeded components)
                SimulationRandom.Seed(777); EnergyMarket.ResetCalibration();
                World seedWorld = WorldFactory.CreateDefault(); EnergyMarket.BeginTurn(seedWorld);
                Country pl = seedWorld.GetCountry(CountryId.Poland);
                Sector plEnergy = null; foreach (Sector s in pl.Sectors) { if (s.Type == SectorType.Energy) { plEnergy = s; } }
                double plIndex = Math.Max(0.0001f, pl.State.PriceLevel);
                EnergyMarket.Result plR = EnergyMarket.Clear(pl);
                float anchor = plEnergy.BaselineRegulationLevel, level0 = plEnergy.RegulationLevel;
                EnergyLedger.Book atAnchor = EnergyLedger.Compute(pl, plR, plIndex, 0.0);
                plEnergy.RegulationLevel = anchor - 20f; EnergyLedger.Book freer = EnergyLedger.Compute(pl, plR, plIndex, 0.0);
                plEnergy.RegulationLevel = anchor + 20f; EnergyLedger.Book tighter = EnergyLedger.Compute(pl, plR, plIndex, 0.0);
                plEnergy.RegulationLevel = level0;
                double PreTax(EnergyLedger.ClassStack st) => st.Wholesale + st.Margin + st.Network;
                double ratio0 = PreTax(atAnchor.Classes[1]) / PreTax(atAnchor.Classes[0]), ratioFree = PreTax(freer.Classes[1]) / PreTax(freer.Classes[0]), ratioTight = PreTax(tighter.Classes[1]) / PreTax(tighter.Classes[0]);
                double k7 = EnergyLedger.LiberalisationSplitPerGap;
                bool receipts = Math.Abs(freer.ToSuppliers - atAnchor.ToSuppliers) <= 1e-9 * Math.Max(1.0, atAnchor.ToSuppliers) && Math.Abs(tighter.ToSuppliers - atAnchor.ToSuppliers) <= 1e-9 * Math.Max(1.0, atAnchor.ToSuppliers);
                bool directions = freer.Classes[1].Margin < atAnchor.Classes[1].Margin && freer.Classes[0].Margin > atAnchor.Classes[0].Margin && tighter.Classes[1].Margin > atAnchor.Classes[1].Margin && tighter.Classes[0].Margin < atAnchor.Classes[0].Margin;
                bool proportion = Math.Abs(ratioFree / ratio0 - (1.0 - k7 * 0.2)) <= 1e-6 && Math.Abs(ratioTight / ratio0 - (1.0 + k7 * 0.2)) <= 1e-6;
                if (!receipts || !directions || !proportion || EnergyLedger.LiberalisationGap(pl) != 0.0)
                { ok = false; Debug.LogError($"ENERGY LEDGER: Poland's regulation gap did not move the ratio as the rule states - suppliers {atAnchor.ToSuppliers:F6} / {freer.ToSuppliers:F6} / {tighter.ToSuppliers:F6}, ratio {ratio0:F6} / {ratioFree:F6} / {ratioTight:F6} against × {1.0 - k7 * 0.2:F3} / × {1.0 + k7 * 0.2:F3}, the seed's gap {EnergyLedger.LiberalisationGap(pl)}."); }
                foreach (EnergyLedger.Book bk in new[] { freer, tighter }) { if (Math.Abs(bk.Gap) > 1e-9 * Math.Max(1.0, bk.PaidTotal)) { ok = false; Debug.LogError($"ENERGY LEDGER: Poland's book does not close under a regulation gap (gap {bk.Gap:E2})."); } }
                sb.Append(F("    Poland   regulation 20 below its anchor {0:F1}: non-households' margin {1:F5} → {2:F5}, households' {3:F5} → {4:F5}/kWh, suppliers' receipts {5:F6} → {6:F6} bn; the pre-tax ratio (non-households ÷ households) {7:F4} → {8:F4} (× {9:F4}), 20 above {10:F4} (× {11:F4})\n",
                    anchor, atAnchor.Classes[1].Margin, freer.Classes[1].Margin, atAnchor.Classes[0].Margin, freer.Classes[0].Margin, atAnchor.ToSuppliers, freer.ToSuppliers, ratio0, ratioFree, ratioFree / ratio0, ratioTight, ratioTight / ratio0));

                // every covered country at both ends of the dial: each class's energy-and-supply and pre-tax price above zero; the size, liberalised to 0 from each anchor
                var sizes = new List<string>();
                double lowestEnergy = double.MaxValue; string lowestAt = "";
                foreach (Country k in seedWorld.Countries)
                {
                    if (!EnergyLayer.Has(k.Id)) { continue; }
                    Sector e = null; foreach (Sector s in k.Sectors) { if (s.Type == SectorType.Energy) { e = s; } }
                    double idx = Math.Max(0.0001f, k.State.PriceLevel);
                    EnergyMarket.Result kr = EnergyMarket.Clear(k);
                    EnergyLedger.Book a = EnergyLedger.Compute(k, kr, idx, 0.0);
                    float keep = e.RegulationLevel;
                    foreach (float end in new[] { 0f, 100f })
                    {
                        e.RegulationLevel = end;
                        EnergyLedger.Book z = EnergyLedger.Compute(k, kr, idx, 0.0);
                        for (int c = 0; c < EnergyLedger.ClassCount; c++)
                        {
                            double energyAndSupply = z.Classes[c].Wholesale + z.Classes[c].Margin;
                            if (energyAndSupply < lowestEnergy) { lowestEnergy = energyAndSupply; lowestAt = F("{0} {1} at Regulation {2:F0}", k.Id, z.Classes[c].Class, end); }
                            if (!(energyAndSupply > 0.0) || !(PreTax(z.Classes[c]) > 0.0)) { ok = false; Debug.LogError($"ENERGY LEDGER: {k.Id}'s {z.Classes[c].Class} price is not positive at Regulation {end} - energy and supply {energyAndSupply:F5}, pre-tax {PreTax(z.Classes[c]):F5}."); }
                        }
                        if (end == 0f)
                        {
                            double ra = PreTax(a.Classes[1]) / PreTax(a.Classes[0]), rz = PreTax(z.Classes[1]) / PreTax(z.Classes[0]);
                            sizes.Add(F("{0} {1:F1} → 0: ratio {2:F3} → {3:F3} ({4:+0.0;-0.0} %)", k.Id, e.BaselineRegulationLevel, ra, rz, 100.0 * (rz / ra - 1.0)));
                        }
                    }
                    e.RegulationLevel = keep;
                }
                sb.Append(F("    at Regulation 0 and 100 in all six, the lowest energy-and-supply component {0:F5}/kWh ({1}) - every class's price positive\n", lowestEnergy, lowestAt));
                sb.Append("    full liberalisation from each anchor (the size is the model's; Steiner's table 9, the industrial-to-residential ratio: unbundling −0.051, third-party access −0.035, a wholesale pool −0.114, against a constant of 0.528): " + string.Join(" · ", sizes) + "\n");
            }

            // (8) EN-7b: the electricity tax
            sb.Append("\n    8. EN-7b: THE ELECTRICITY TAX - the statute's change within its coverage at a price index of 1.6 (nominal with nominal), the business floor, households' VAT, the revenue change, the boundary's flow, the flow's reach into the budget\n");
            {
                SimulationRandom.Seed(777); EnergyMarket.ResetCalibration();
                World tw = WorldFactory.CreateDefault(); EnergyMarket.BeginTurn(tw);
                foreach (Country k in tw.Countries)
                {
                    if (!EnergyLayer.Has(k.Id)) { continue; }
                    int ci = EnergyLayer.Index(k.Id);
                    double usd = EnergyLayerData.UsdPerMarketCurrency[ci], idx = 1.6;   // not the seed's 1: a shift that forgot the price index would pass at 1
                    EnergyMarket.Result kr = EnergyMarket.Clear(k);
                    EnergyLedger.Book t0 = EnergyLedger.Compute(k, kr, idx, 0.0);
                    float hh = k.ElectricityTaxHouseholds, nh = k.ElectricityTaxNonHouseholds;
                    k.ElectricityTaxHouseholds = hh + 10f; k.ElectricityTaxNonHouseholds = nh - 10f;
                    EnergyLedger.Book t1 = EnergyLedger.Compute(k, kr, idx, 0.0);
                    float[] effective = { EnergyLedger.EffectiveElectricityTaxEurPerMwh(k, 0), EnergyLedger.EffectiveElectricityTaxEurPerMwh(k, 1) };   // the figure the energy page prints
                    k.ElectricityTaxHouseholds = 0f;
                    EnergyLedger.Book tz = EnergyLedger.Compute(k, kr, idx, 0.0);
                    k.ElectricityTaxHouseholds = hh; k.ElectricityTaxNonHouseholds = nh;
                    if (!EnergyLayer.HasElectricityTax(k.Id))
                    {
                        bool unmoved = t1.Classes[0].Total == t0.Classes[0].Total && t1.Classes[1].Total == t0.Classes[1].Total && t1.ToStateTaxes == t0.ToStateTaxes && t1.ElectricityTaxRevenueChange == 0.0;
                        if (!unmoved) { ok = false; Debug.LogError($"ENERGY LEDGER: {k.Id} levies no electricity tax but its stack moved under a changed statute."); }
                        sb.Append(F("    {0,-8} no statute - the stack unchanged to the bit whatever the field holds\n", k.Id));
                        continue;
                    }
                    double revenue = 0.0; var parts = new List<string>();
                    for (int c = 0; c < EnergyLedger.ClassCount; c++)
                    {
                        float baseValue = c == 0 ? k.ElectricityTaxHouseholdsBase : k.ElectricityTaxNonHouseholdsBase;
                        float composed = c == 0 ? baseValue + 10f : baseValue - 10f;
                        float floor = (float)EnergyLayer.ElectricityTaxFloorEurPerMwh(k.Id, c);
                        if (c == 1 && baseValue >= floor && composed < floor) { composed = floor; }
                        if (effective[c] != composed) { ok = false; Debug.LogError($"ENERGY LEDGER: {k.Id} {t0.Classes[c].Class}: the effective statute the energy page prints ({effective[c]:R}) is not the composed rate held at the floor ({composed:R})."); }
                        double cov = Math.Min(1.0, EnergyLayerData.RetailTaxEnv[ci][c] / (EnergyLayerData.ElectricityTaxEurPerMwh[ci][c] / 1000.0));   // computed here, not read from the accessor it checks
                        if (Math.Abs(cov - EnergyLayer.ElectricityTaxCoverage(k.Id, c)) > 1e-12) { ok = false; Debug.LogError($"ENERGY LEDGER: {k.Id} {t0.Classes[c].Class}: the layer's coverage {EnergyLayer.ElectricityTaxCoverage(k.Id, c)} is not the component over the statute, capped at 1 ({cov})."); }
                        double expected = Math.Max(0.0, t0.Classes[c].TaxEnv + ((double)composed - baseValue) / 1000.0 * cov * usd * idx) - t0.Classes[c].TaxEnv;
                        double moved = t1.Classes[c].TaxEnv - t0.Classes[c].TaxEnv;
                        // the ruling on Poland's failed premise: the law moves the statute's change and never scales the component - whatever else the band's figure
                        // carries (Poland's is 41.5 and 48.9 times its excise, the rest named by no document) stays where the seed put it
                        double statuteMove = Math.Abs(((double)composed - baseValue) / 1000.0 * usd * idx);
                        double componentOverStatute = EnergyLayerData.RetailTaxEnv[ci][c] / (EnergyLayerData.ElectricityTaxEurPerMwh[ci][c] / 1000.0);
                        if (Math.Abs(moved) > statuteMove * (1.0 + 1e-9) + 1e-15) { ok = false; Debug.LogError($"ENERGY LEDGER: {k.Id} {t0.Classes[c].Class}: the component moved {moved:E4} $/kWh, more than the statute's own change ({statuteMove:E4}) - the law scaled the component ({componentOverStatute:F3} times the statute) instead of moving the statute."); }
                        if (componentOverStatute > 1.0) { parts.Add(F("{0} the component {1:F3} times the statute - moved by the statute's change alone", t0.Classes[c].Class, componentOverStatute)); }
                        revenue += expected * t0.Classes[c].ConsumptionGwh / 1000.0;
                        if (Math.Abs(moved - expected) > 1e-12 || Math.Abs(t1.Classes[c].ElectricityTaxShift - moved) > 1e-15) { ok = false; Debug.LogError($"ENERGY LEDGER: {k.Id} {t0.Classes[c].Class}: the component moved {moved:E4} against the statute's change within its coverage {expected:E4}."); }
                        if (c == 0 && Math.Abs((t1.Classes[0].Vat - t0.Classes[0].Vat) - moved * k.Environment.RetailVatRate[0]) > 1e-12) { ok = false; Debug.LogError($"ENERGY LEDGER: {k.Id}'s households' VAT did not move by the rate on the tax's move."); }
                        if (c == 1 && Math.Abs((t1.Classes[1].Bill - t0.Classes[1].Bill) - moved * t0.Classes[1].ConsumptionGwh / 1000.0) > 1e-9) { ok = false; Debug.LogError($"ENERGY LEDGER: {k.Id}'s industrial bill did not move by the tax's move on its consumption (pre-VAT)."); }
                        if (c == 0)
                        {
                            double atZero = Math.Max(0.0, t0.Classes[0].TaxEnv + (0.0 - baseValue) / 1000.0 * cov * usd * idx);   // the base statute out within its coverage - never the rest of the band
                            if (Math.Abs(tz.Classes[0].TaxEnv - atZero) > 1e-12) { ok = false; Debug.LogError($"ENERGY LEDGER: {k.Id}'s households' component at a statute of zero is {tz.Classes[0].TaxEnv:E6} $/kWh, not the seed's less the base statute within its coverage ({atZero:E6}) - at zero the law took more (or less) than the statute."); }
                        }
                        parts.Add(F("{0} {1:0.###} → {2:0.###} EUR/MWh, coverage {3:F3}, component {4:+0.000000;-0.000000} $/kWh", t0.Classes[c].Class, baseValue, composed, cov, moved));
                    }
                    if (Math.Abs(t1.ElectricityTaxRevenueChange - revenue) > 1e-9 * Math.Max(1.0, Math.Abs(revenue))) { ok = false; Debug.LogError($"ENERGY LEDGER: {k.Id}'s revenue change {t1.ElectricityTaxRevenueChange:F6} bn is not the classes' shifts on their consumption ({revenue:F6})."); }
                    if (Math.Abs(t1.Gap) > 1e-9 * Math.Max(1.0, t1.PaidTotal) || Math.Abs(tz.Gap) > 1e-9 * Math.Max(1.0, tz.PaidTotal)) { ok = false; Debug.LogError($"ENERGY LEDGER: {k.Id}'s book does not close with the statute moved (gap {t1.Gap:E2} / {tz.Gap:E2})."); }
                    sb.Append(F("    {0,-8} {1} · revenue change {2:+0.000;-0.000} bn · at a household statute of zero the component {3:F5} $/kWh\n", k.Id, string.Join(" · ", parts), t1.ElectricityTaxRevenueChange, tz.Classes[0].TaxEnv));
                }

                // through a real boundary: Germany's household relief enacted in year 1, planned at its close, booked over year 2, repealed
                SimulationRandom.Seed(777); EnergyMarket.ResetCalibration();
                World bw = WorldFactory.CreateDefault();
                var bgo = new GameObject("EN7B");
                try
                {
                    SimulationManager bsim = bgo.AddComponent<SimulationManager>();
                    bsim.SetWorld(bw);
                    bsim.PlayerCountryId = CountryId.Germany;
                    Country de = bw.GetCountry(CountryId.Germany);
                    const BindingFlags instance = BindingFlags.Instance | BindingFlags.NonPublic;
                    MethodInfo applyLaw = typeof(SimulationManager).GetMethod("ApplyLawBillEffects", instance);
                    FieldInfo periodsField = typeof(SimulationManager).GetField("_fiscalPeriods", instance);
                    if (applyLaw == null || periodsField == null) { ok = false; Debug.LogError("ENERGY LEDGER: ApplyLawBillEffects or _fiscalPeriods was not found by reflection - the boundary's flow is UNVERIFIED."); }
                    else
                    {
                        var decisions = new Dictionary<CountryId, PolicyDecision>();
                        foreach (Country k in bw.Countries) { decisions[k.Id] = PolicyDecision.None(); }
                        int hhBaseBits = BitConverter.SingleToInt32Bits(de.ElectricityTaxHouseholdsBase);
                        for (int day = 0; day < SimulationManager.DaysPerTurn; day++) { bsim.AdvanceDay(); }
                        applyLaw.Invoke(bsim, new object[] { de, new LawBill { LawId = "household_electricity_tax_relief_act", IsRepeal = false } });
                        de.EnactedLaws.Add(new EnactedLaw { LawId = "household_electricity_tax_relief_act", EnactedOn = bsim.CurrentDate });
                        float statuteAfterLaw = de.ElectricityTaxHouseholds;
                        bsim.AdvanceTurn(decisions);
                        var periods = (System.Collections.IDictionary)periodsField.GetValue(bsim);
                        float planned = ((SimulationManager.FiscalPeriod)periods[CountryId.Germany]).PlannedElectricityTaxRevenue;
                        for (int day = 0; day < SimulationManager.DaysPerTurn; day++) { bsim.AdvanceDay(); }
                        bsim.AdvanceTurn(decisions);
                        float booked = bsim.GetLastFiscalReport(CountryId.Germany).ElectricityTaxRevenue;
                        applyLaw.Invoke(bsim, new object[] { de, new LawBill { LawId = "household_electricity_tax_relief_act", IsRepeal = true } });
                        bool planOk = planned < 0f && Math.Abs(statuteAfterLaw - (de.ElectricityTaxHouseholdsBase - 10f)) < 1e-4f;
                        bool bookOk = Math.Abs(booked - planned) <= 1e-4f * Math.Abs(planned) + 1e-6f;
                        bool repealOk = BitConverter.SingleToInt32Bits(de.ElectricityTaxHouseholds) == hhBaseBits;
                        if (!planOk) { ok = false; Debug.LogError($"ENERGY LEDGER: Germany's household relief did not plan a negative flow at the boundary (statute {statuteAfterLaw:F2}, planned {planned:F4} bn)."); }
                        if (!bookOk) { ok = false; Debug.LogError($"ENERGY LEDGER: the year's electricity-tax flow booked {booked:F6} bn against the {planned:F6} planned - the daily slices do not sum to the plan."); }
                        if (!repealOk) { ok = false; Debug.LogError($"ENERGY LEDGER: the relief's repeal left Germany's household statute at {de.ElectricityTaxHouseholds:R}, not its base."); }
                        sb.Append(F("    Germany  the household relief enacted in year 1: the statute {0:0.##} → {1:0.##} EUR/MWh; the boundary planned {2:+0.0000;-0.0000} bn for year 2 and the year booked {3:+0.0000;-0.0000} bn as the report's \"of which\"; the repeal returned the statute to its base bit for bit\n",
                            de.ElectricityTaxHouseholdsBase, statuteAfterLaw, planned, booked));
                    }
                }
                finally { UnityEngine.Object.DestroyImmediate(bgo); }

                // the flow reaches the budget: (a) ApplyRevenueAndSpending books its electricity-tax argument inside the fiscal-reaction multiplier - one state,
                // the argument 0 against +4 bn at a fixed multiplier; (b) the daily path hands it the period's plan, sliced - two worlds identical but for the plan
                SimulationRandom.Seed(777); EnergyMarket.ResetCalibration();
                World fw = WorldFactory.CreateDefault();
                var fgo = new GameObject("EN7B_REACH");
                try
                {
                    SimulationManager fsim = fgo.AddComponent<SimulationManager>();
                    fsim.SetWorld(fw);
                    Country fde = fw.GetCountry(CountryId.Germany);
                    MethodInfo books = typeof(SimulationManager).GetMethod("ApplyRevenueAndSpending", BindingFlags.Instance | BindingFlags.NonPublic);
                    if (books == null || books.GetParameters().Length != 14) { ok = false; Debug.LogError("ENERGY LEDGER: ApplyRevenueAndSpending (14 parameters) was not found by reflection - the flow's reach is UNVERIFIED."); }
                    else
                    {
                        const float flow = 4f, multiplier = 0.75f;
                        float budget0 = fde.State.Budget, debt0 = fde.State.GovernmentDebt;
                        var at0 = new object[] { fde, 0.2f, 0.1f, 0.02f, 0.03f, 0.04f, 0f, 0f, 0f, 0f, 0f, 0f, 0.01f, multiplier };
                        float revenue0 = (float)books.Invoke(fsim, at0);
                        float budgetMove0 = fde.State.Budget - budget0, debtMove0 = fde.State.GovernmentDebt - debt0;
                        fde.State.Budget = budget0; fde.State.GovernmentDebt = debt0;
                        var atFlow = new object[] { fde, 0.2f, 0.1f, 0.02f, 0.03f, 0.04f, 0f, 0f, 0f, flow, 0f, 0f, 0.01f, multiplier };
                        float revenue1 = (float)books.Invoke(fsim, atFlow);
                        float budgetMove1 = fde.State.Budget - budget0, debtMove1 = fde.State.GovernmentDebt - debt0;
                        fde.State.Budget = budget0; fde.State.GovernmentDebt = debt0;
                        float want = flow * multiplier;
                        float dRevenue = revenue1 - revenue0, dBalance = (float)atFlow[11] - (float)at0[11], dBudget = budgetMove1 - budgetMove0, dDebt = debtMove1 - debtMove0;
                        bool reaches = Math.Abs(dRevenue - want) <= 2e-3f && Math.Abs(dBalance - want) <= 2e-3f && Math.Abs(dBudget - want) <= 2e-3f && Math.Abs(dDebt + want) <= 2e-3f;
                        if (!reaches) { ok = false; Debug.LogError($"ENERGY LEDGER: a {flow} bn electricity-tax argument at the multiplier {multiplier} moved revenue {dRevenue:F5}, the balance {dBalance:F5}, the budget {dBudget:F5} and the debt {dDebt:F5} - not {want} into revenue, balance and budget and out of the debt."); }
                        sb.Append(F("    reach    ApplyRevenueAndSpending: a {0} bn flow at the multiplier {1} moved revenue {2:+0.0000;-0.0000}, the balance {3:+0.0000;-0.0000}, the budget {4:+0.0000;-0.0000}, the debt {5:+0.0000;-0.0000} (the flow at the multiplier: {6:0.0000})\n",
                            flow, multiplier, dRevenue, dBalance, dBudget, dDebt, want));
                    }
                }
                finally { UnityEngine.Object.DestroyImmediate(fgo); }

                const float plannedYear = 250f;
                (float Budget, float Debt, float AccruedRevenue, float AccruedTax, float Multiplier)? dayOff = DayOfElectricityTaxFlow(0f), dayOn = DayOfElectricityTaxFlow(plannedYear);
                if (dayOff == null || dayOn == null) { ok = false; Debug.LogError("ENERGY LEDGER: GetOrSeedFiscalPeriod or AccrueDailyFiscalFlows was not found by reflection - the daily path's reach is UNVERIFIED."); }
                else
                {
                    float slice = plannedYear / SimulationManager.DaysPerTurn, m = dayOn.Value.Multiplier;
                    float dBudget = dayOn.Value.Budget - dayOff.Value.Budget, dDebt = dayOn.Value.Debt - dayOff.Value.Debt;
                    float dRevenue = dayOn.Value.AccruedRevenue - dayOff.Value.AccruedRevenue, dTax = dayOn.Value.AccruedTax - dayOff.Value.AccruedTax;
                    bool daily = m == dayOff.Value.Multiplier && Math.Abs(m - 1f) * slice > 10f * 2e-3f && Math.Abs(dTax - slice) <= 1e-4f && Math.Abs(dRevenue - slice * m) <= 2e-3f && Math.Abs(dBudget - slice * m) <= 2e-3f && Math.Abs(dDebt + slice * m) <= 2e-3f;
                    if (!daily) { ok = false; Debug.LogError($"ENERGY LEDGER: a {plannedYear} bn plan moved one day's accrued tax {dTax:F5} (slice {slice:F5}), revenue {dRevenue:F5}, the budget {dBudget:F5} and the debt {dDebt:F5} - not the slice at the period's multiplier {m:F4} ({slice * m:F5})."); }
                    sb.Append(F("    reach    the daily path: a {0} bn plan accrued {1:0.0000} bn of tax in one day (the slice {2:0.0000}), revenue {3:+0.0000;-0.0000}, the budget {4:+0.0000;-0.0000}, the debt {5:+0.0000;-0.0000} (the slice at the period's multiplier {6:F4}: {7:0.0000})\n",
                        plannedYear, dTax, slice, dRevenue, dBudget, dDebt, m, slice * m));
                }
            }

            sb.Append(ok ? "\n=== EnergyLedgerDiagnostic: ALL ASSERTIONS PASS ===\n" : "\n=== EnergyLedgerDiagnostic: FAILED (see above) ===\n");
            if (ok) { Debug.Log(sb.ToString()); } else { Debug.LogError(sb.ToString()); }
            CheckExit.Finish(ok ? 0 : 1);
        }

        private sealed class Outcome
        {
            public double HouseholdPrice, IndustryPrice, IndustryBill, BillShare, ChannelSum, EtsCost, Wholesale, MaxGap, RecomputeGapMax;
        }

        /// <summary>EN-7b: one day of Germany's REAL production accrual (AccrueDailyFiscalFlows, by reflection) on a fresh world at seed 777, its period's electricity-tax plan set to
        /// <paramref name="planned"/> - the day's move of the budget and the debt, the period's accrued revenue and accrued electricity tax, the period's multiplier; null if a method is missing.</summary>
        private static (float Budget, float Debt, float AccruedRevenue, float AccruedTax, float Multiplier)? DayOfElectricityTaxFlow(float planned)
        {
            SimulationRandom.Seed(777); EnergyMarket.ResetCalibration();
            World w = WorldFactory.CreateDefault();
            var go = new GameObject("EN7B_DAY");
            try
            {
                SimulationManager s = go.AddComponent<SimulationManager>();
                s.SetWorld(w);
                Country d = w.GetCountry(CountryId.Germany);
                MethodInfo seedPeriod = typeof(SimulationManager).GetMethod("GetOrSeedFiscalPeriod", BindingFlags.Instance | BindingFlags.NonPublic);
                MethodInfo accrue = typeof(SimulationManager).GetMethod("AccrueDailyFiscalFlows", BindingFlags.Instance | BindingFlags.NonPublic);
                if (seedPeriod == null || accrue == null) { return null; }
                var period = (SimulationManager.FiscalPeriod)seedPeriod.Invoke(s, new object[] { d });
                period.PlannedFiscalReactionMultiplier = 0.75f;   // away from 1 on both runs: Germany's seed multiplier is exactly 1, which could not tell a flow inside the period's multiplier from one outside it
                period.PlannedElectricityTaxRevenue = planned;
                float budget0 = d.State.Budget, debt0 = d.State.GovernmentDebt;
                accrue.Invoke(s, new object[] { d });
                return (d.State.Budget - budget0, d.State.GovernmentDebt - debt0, period.AccruedRevenue, period.AccruedElectricityTaxRevenue, period.PlannedFiscalReactionMultiplier);
            }
            finally { UnityEngine.Object.DestroyImmediate(go); }
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
