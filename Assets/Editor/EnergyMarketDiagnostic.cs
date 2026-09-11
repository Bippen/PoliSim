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
    /// EN-3 (2026-09-11), the market layer's proof - it builds and advances worlds, so it belongs to the simulation group. (1) THE IDENTITY AT THE
    /// SEED: for six, the dispatch's power figure at the seed rate equals the family's seed to 1e-4 - the writer changed hands without the figure
    /// moving. (2) THE CALIBRATION: the five with a fossil fleet reproduce 2023's coal / gas / oil shares of the fossil total within the tolerance;
    /// the adders printed. (3) THE MECHANISM: Poland with its carbon tax raised twenty points through the decision, ten years, against untouched -
    /// power CO₂ per head LOWER, coal's share of the fossil total LOWER, gas's HIGHER, the peak price HIGHER, the carbon revenue rising less than
    /// the rate (the base erodes through dispatch now). (4) SWEDEN'S ZONES: the flows the coincident-hour balances require on the three snitt
    /// against the dated NTCs, and the first thing the dispatch had to answer - whether §457's snitt-4 flow was congestion or export. (5) NO
    /// CLAMP: a block whose lowest offer is negative prices negative; the scarcity term rises past the onset and reaches the ceiling when the
    /// residual exceeds the dependable capacity. (6) B6: at a doubled price level the fuel half of the marginal cost doubles and the tax's points
    /// do not.
    /// </summary>
    public static class EnergyMarketDiagnostic
    {
        private const int Years = 10;

        public static void Run()
        {
            CheckExit.ArmLogFold();
            bool ok = true;
            var sb = new StringBuilder();
            sb.Append("=== ENERGY MARKET (EN-3): the clearing, the calibration, the writer, Sweden's zones ===\n");

            SimulationRandom.Seed(777);
            EnergyMarket.ResetCalibration();
            World world = WorldFactory.CreateDefault();
            EnergyMarket.BeginTurn(world);

            // (1) + (2) per country
            sb.Append("\n    1-2. THE SEED: the dispatch's figure against the family's, the calibration's shares against 2023's\n");
            foreach (Country c in world.Countries)
            {
                if (!EnergyLayer.Has(c.Id)) { continue; }
                float rate = EnvironmentFamily.CarbonTaxRate(c);
                float written = EnergyMarket.PowerCo2PerHead(c, rate);
                if (Mathf.Abs(written - c.Environment.PowerCo2PerCapita) > 1e-4f) { ok = false; Debug.LogError($"ENERGY MARKET: {c.Id}'s dispatch writes {written:F5} t/head at the seed against the family's seed {c.Environment.PowerCo2PerCapita:F5}."); }
                if (Mathf.Abs(c.State.PowerCo2PerCapita - c.Environment.PowerCo2PerCapita) > 1e-4f) { ok = false; Debug.LogError($"ENERGY MARKET: {c.Id}'s state carries {c.State.PowerCo2PerCapita} at the seed, not the seed {c.Environment.PowerCo2PerCapita}."); }
                if (!c.Environment.PowerFromDispatch) { ok = false; Debug.LogError($"ENERGY MARKET: {c.Id}'s power figure is not written by the dispatch."); }
                EnergyMarket.Result r = EnergyMarket.ClearAt(c.Id, 1.0, 0.0);
                double[] target = EnergyMarket.SeedTargets(c.Id);
                bool hasFossil = c.Id != CountryId.Sweden;
                if (hasFossil && (double.IsNaN(r.MaxCalibrationGap) || r.MaxCalibrationGap > EnergyMarket.CalibrationTolerance)) { ok = false; Debug.LogError($"ENERGY MARKET: {c.Id}'s seed dispatch misses 2023's fossil shares by {r.MaxCalibrationGap:F4} (tolerance {EnergyMarket.CalibrationTolerance})."); }
                double fossil = r.AnnualGwh[0] + r.AnnualGwh[1] + r.AnnualGwh[2];
                sb.Append(F("    {0,-8} written {1:F4} = seed {2:F4} t/head · residual {3:F1} Mt · fossil shares coal/gas/oil {4:F3}/{5:F3}/{6:F3} (2023: {7:F3}/{8:F3}/{9:F3}, gap {10:F4}) · adders {11:F1}/{12:F1}/{13:F1} · prices base/mid/peak {14:F0}/{15:F0}/{16:F0}\n",
                    c.Id, written, c.Environment.PowerCo2PerCapita, c.Environment.PowerResidualMt, fossil > 0 ? r.AnnualGwh[0] / fossil : 0, fossil > 0 ? r.AnnualGwh[1] / fossil : 0, fossil > 0 ? r.AnnualGwh[2] / fossil : 0,
                    target[0], target[1], target[2], r.MaxCalibrationGap, r.Adders[0], r.Adders[1], r.Adders[2], r.Zones[0][0].Price, r.Zones[0][1].Price, r.Zones[0][2].Price));
                if (c.Id == CountryId.Germany && r.AnnualGwh[EnergyMarket.Nuclear] > 0) { ok = false; Debug.LogError("ENERGY MARKET: Germany's nuclear dispatches above zero - §457 closed it by law."); }
            }

            // (3) the mechanism, Poland
            sb.Append("\n    3. THE MECHANISM: Poland, its carbon tax line implemented at 5 (the immediate action a player takes; the seed leaves it unimplemented), then up twenty points through the decision, ten years, against untouched\n");
            double[] untouched = RunCountry(CountryId.Poland, Years, 0f);
            double[] raised = RunCountry(CountryId.Poland, Years, 20f);
            // [0] power t/head, [1] coal share, [2] gas share, [3] peak price, [4] carbon revenue, [5] carbon rate, [6] transport
            if (!(raised[0] < untouched[0])) { ok = false; Debug.LogError($"ENERGY MARKET: Poland's power CO₂ did not fall under the raise ({raised[0]:F4} against {untouched[0]:F4})."); }
            if (!(raised[1] < untouched[1]) || !(raised[2] > untouched[2])) { ok = false; Debug.LogError($"ENERGY MARKET: the raise did not move Poland from coal to gas (coal {untouched[1]:F3} → {raised[1]:F3}, gas {untouched[2]:F3} → {raised[2]:F3})."); }
            if (!(raised[3] > untouched[3])) { ok = false; Debug.LogError($"ENERGY MARKET: Poland's peak price did not rise under the raise ({raised[3]:F1} against {untouched[3]:F1})."); }
            double rateRatio = raised[5] / Math.Max(0.0001, untouched[5]), revenueRatio = raised[4] / Math.Max(0.0001, untouched[4]);
            if (!(revenueRatio > 1.0) || !(revenueRatio < rateRatio)) { ok = false; Debug.LogError($"ENERGY MARKET: Poland's carbon revenue rose x{revenueRatio:F4} against a rate x{rateRatio:F4} - the base should erode through the dispatch."); }
            sb.Append(F("    untouched: power {0:F4} t/head, coal {1:F3} / gas {2:F3} of fossil, peak price {3:F1}, revenue {4:F3} at {5:F0}; raised: power {6:F4}, coal {7:F3} / gas {8:F3}, peak price {9:F1}, revenue {10:F3} at {11:F0} - the rate x{12:F3}, the revenue x{13:F3}; transport {14:F4} → {15:F4} (the readout coupling, unchanged in form)\n",
                untouched[0], untouched[1], untouched[2], untouched[3], untouched[4], untouched[5], raised[0], raised[1], raised[2], raised[3], raised[4], raised[5], rateRatio, revenueRatio, untouched[6], raised[6]));

            // (4) Sweden's zones
            sb.Append("\n    4. SWEDEN'S ZONES on coincident hours against the dated NTCs (MW; flows positive southward)\n");
            Country se = world.GetCountry(CountryId.Sweden);
            EnergyMarket.Result rse = EnergyMarket.Clear(se, EnvironmentFamily.CarbonTaxRate(se));
            if (!EnergyMarket.HasWaterValue) { ok = false; Debug.LogError("ENERGY MARKET: no water value was set for the turn - BeginTurn did not run."); }
            for (int b = 0; b < 3; b++)
            {
                sb.Append(F("    {0,-4} water value {1:F1};", EnergyLayerData.DispatchBlocks[b], rse.WaterValue[b]));
                foreach (EnergyMarket.LinkResult l in rse.Links) { sb.Append(F(" {0}→{1} {2,6:F0} / {3:F0} ({4:F0}%){5} p {6:F0}|{7:F0};", l.From, l.To, l.FlowMw[b], l.CapacityMw[b], 100 * Math.Abs(l.FlowMw[b]) / l.CapacityMw[b], l.Binding[b] ? " BINDS" : "", l.PriceNorth[b], l.PriceSouth[b])); }
                sb.Append('\n');
            }
            EnergyMarket.LinkResult snitt4 = rse.Links[2];
            int se4 = EnergyLayer.ZoneIndex("SE4");
            for (int b = 0; b < 3; b++)
            {
                double own = EnergyLayerData.ZoneConsumptionBlockMw[se4][b] - rse.Zones[3][b].MustRunMw, transit = EnergyLayerData.ZoneExternalExportMw[se4][b], unbalance = rse.ChainUnbalanceMw[b];
                if (Math.Abs(snitt4.FlowMw[b] - (own + transit + unbalance)) > 1.0) { ok = false; Debug.LogError($"ENERGY MARKET: snitt 4's flow in the {EnergyLayerData.DispatchBlocks[b]} block ({snitt4.FlowMw[b]:F0}) is not SE4's own deficit ({own:F0}) plus its transit export ({transit:F0}) plus the chain's unbalance ({unbalance:F0})."); }
                if (Math.Abs(unbalance) > 0.02 * Math.Abs(snitt4.FlowMw[b])) { ok = false; Debug.LogError($"ENERGY MARKET: the chain's national unbalance in the {EnergyLayerData.DispatchBlocks[b]} block is {unbalance:F0} MW - more than two per cent of snitt 4's flow; the exchange and the balances no longer agree."); }
                sb.Append(F("    snitt 4, {0}: {1:F0} of {2:F0} MW = SE4's own deficit {3:F0} + transit to Denmark, Germany, Poland and Lithuania {4:F0} (the chain's unbalance {5:F0}, losses and rounding) - {6}\n", EnergyLayerData.DispatchBlocks[b], snitt4.FlowMw[b], snitt4.CapacityMw[b], own, transit, unbalance, snitt4.Binding[b] ? "CONGESTION on the block average" : "EXPORT, not congestion, on the block average"));
            }
            double rent = 0; foreach (EnergyMarket.LinkResult l in rse.Links) { rent += l.RentPerYear; }
            sb.Append(F("    congestion rent over the year on the three snitt: {0:N0} (the currency's units × MWh) - zero where no block binds.\n", rent));

            // (5) no clamp; the scarcity term
            sb.Append("\n    5. NO CLAMP, AND THE SCARCITY TERM (a probe with offers of its own)\n");
            double[] offers = { 30.0, 50.0, double.PositiveInfinity }; double[] caps = { 100.0, 100.0, 0.0 };
            EnergyMarket.BlockResult negative = EnergyMarket.ClearBlock(100.0, 150.0, offers, caps, -5.0);
            if (Math.Abs(negative.Price + 5.0) > 1e-9 || Math.Abs(negative.CurtailedMw - 50.0) > 1e-9) { ok = false; Debug.LogError($"ENERGY MARKET: a surplus block with a lowest offer of −5 priced {negative.Price} with {negative.CurtailedMw} MW curtailed - the price is clamped or the surplus lost."); }
            EnergyMarket.BlockResult normal = EnergyMarket.ClearBlock(150.0, 0.0, offers, caps, 0.0);
            if (Math.Abs(normal.Price - 50.0) > 1e-9 || Math.Abs(normal.FossilMw[0] - 100.0) > 1e-9 || Math.Abs(normal.FossilMw[1] - 50.0) > 1e-9) { ok = false; Debug.LogError($"ENERGY MARKET: the merit order priced {normal.Price} with {normal.FossilMw[0]}/{normal.FossilMw[1]} MW - expected 50 with 100/50."); }
            EnergyMarket.BlockResult tight = EnergyMarket.ClearBlock(195.0, 0.0, offers, caps, 0.0);
            if (!(tight.Price > 50.0) || !(tight.Price < EnergyMarket.MaxClearingPrice)) { ok = false; Debug.LogError($"ENERGY MARKET: at 97.5 % of the dependable capacity the price is {tight.Price} - the scarcity term should lift it above the marginal cost and below the ceiling."); }
            EnergyMarket.BlockResult short_ = EnergyMarket.ClearBlock(250.0, 0.0, offers, caps, 0.0);
            if (Math.Abs(short_.Price - EnergyMarket.MaxClearingPrice) > 1e-9 || Math.Abs(short_.UnservedMw - 50.0) > 1e-9) { ok = false; Debug.LogError($"ENERGY MARKET: a residual above the dependable capacity priced {short_.Price} with {short_.UnservedMw} MW unserved - expected the ceiling and 50."); }
            sb.Append(F("    surplus at −5: price {0:F1}, curtailed {1:F0} · merit order: price {2:F1}, dispatch {3:F0}/{4:F0} · 97.5 % of capacity: price {5:F1} · short by 50: price {6:F0}, unserved {7:F0}\n", negative.Price, negative.CurtailedMw, normal.Price, normal.FossilMw[0], normal.FossilMw[1], tight.Price, short_.Price, short_.UnservedMw));

            // (6) B6
            double mc1 = EnergyMarket.MarginalCost(CountryId.Poland, EnergyMarket.Coal, 1.0, 0.0, 0.0), mc2 = EnergyMarket.MarginalCost(CountryId.Poland, EnergyMarket.Coal, 2.0, 0.0, 0.0), mcTax = EnergyMarket.MarginalCost(CountryId.Poland, EnergyMarket.Coal, 1.0, 10.0, 0.0);
            if (Math.Abs(mc2 - 2 * mc1) > 1e-6) { ok = false; Debug.LogError($"ENERGY MARKET: at a doubled price level Poland's coal costs {mc2} against {mc1} - the nominal half should double exactly."); }
            int pl = EnergyLayer.Index(CountryId.Poland);
            if (Math.Abs((mcTax - mc1) - 10.0 * EnergyMarket.CarbonTaxPointPerTonne * EnergyLayerData.EmissionFactorTPerMwh[pl][0]) > 1e-6) { ok = false; Debug.LogError("ENERGY MARKET: ten points of carbon tax do not add ten units per tonne on the emission factor."); }
            sb.Append(F("\n    6. B6: Poland's coal at the seed {0:F2}, at a doubled price level {1:F2} (x2), with ten points of tax {2:F2} (+{3:F2} = 10 × {4:F4} t/MWh)\n", mc1, mc2, mcTax, mcTax - mc1, EnergyLayerData.EmissionFactorTPerMwh[pl][0]));

            sb.Append(ok ? "\n=== EnergyMarketDiagnostic: ALL ASSERTIONS PASS ===\n" : "\n=== EnergyMarketDiagnostic: FAILED (see above) ===\n");
            if (ok) { Debug.Log(sb.ToString()); } else { Debug.LogError(sb.ToString()); }
            CheckExit.Finish(ok ? 0 : 1);
        }

        private static double[] RunCountry(CountryId player, int years, float extraPoints)
        {
            SimulationRandom.Seed(777);
            EnergyMarket.ResetCalibration();
            World world = WorldFactory.CreateDefault();
            var go = new GameObject("ENERGYMARKET");
            try
            {
                SimulationManager sim = go.AddComponent<SimulationManager>();
                sim.SetWorld(world);
                sim.PlayerCountryId = player;
                Country c = world.GetCountry(player);
                // the seed leaves Poland's carbon tax line unimplemented (5, not levied) and a rate override is a no-op on an unimplemented line; the diagnostic takes the
                // immediate action a player would - implements the line at 5 - in BOTH runs, so the comparison is the twenty points alone
                foreach (TaxLine line in c.TaxLines) { if (line.Type == TaxType.CarbonTax && !line.IsImplemented) { line.IsImplemented = true; line.Rate = 5f; } }
                float seedRate = EnvironmentFamily.CarbonTaxRate(c);
                var decisions = new Dictionary<CountryId, PolicyDecision>();
                foreach (Country k in world.Countries) { decisions[k.Id] = PolicyDecision.None(); }
                for (int year = 1; year <= years; year++)
                {
                    for (int day = 0; day < SimulationManager.DaysPerTurn; day++) { sim.AdvanceDay(); }
                    PolicyDecision d = PolicyDecision.None();
                    if (extraPoints != 0f) { d.TaxRateOverrides[TaxType.CarbonTax] = seedRate + extraPoints; }
                    decisions[player] = d;
                    sim.AdvanceTurn(decisions);
                }
                TaxLine carbon = null;
                foreach (TaxLine line in c.TaxLines) { if (line.Type == TaxType.CarbonTax) { carbon = line; } }
                float rate = carbon != null ? carbon.Rate : 0f;
                EnergyMarket.Result r = EnergyMarket.Clear(c, rate);
                double fossil = r.AnnualGwh[0] + r.AnnualGwh[1] + r.AnnualGwh[2];
                return new[] { c.State.PowerCo2PerCapita, fossil > 0 ? r.AnnualGwh[0] / fossil : 0, fossil > 0 ? r.AnnualGwh[1] / fossil : 0, r.Zones[0][2].Price, carbon != null ? TaxBases.Revenue(c, carbon) : 0f, rate, c.State.TransportCo2PerCapita };
            }
            finally { UnityEngine.Object.DestroyImmediate(go); }
        }

        private static string F(string format, params object[] args) => string.Format(CultureInfo.InvariantCulture, format, args);
    }
}
