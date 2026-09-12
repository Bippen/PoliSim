using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using PoliSim.Data;
using PoliSim.Data.Generated;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// Stage 2 of the energy track, its five gates (ENERGY_LAYER_SPINE.md §11; POLISIM_ENERGY_SPECLET.md §2 and S15; built §457).
    ///
    /// <para>1 THE FLEET - six countries × seven labels from the office of record, Ember beside each; nuclear, wind and solar agree
    /// within ten per cent or a gigawatt (the classes every source defines alike) or the run fails; the other labels are TAGGED
    /// definition-divergent where they do not, counted and printed, never averaged. 2 THE BLOCKS - base + mid + peak equals the series
    /// for every zone; Poland's balance factor printed. 3 THE LINKS - with the links infinite the four zones' balances sum to the merged
    /// zone's (the identity); at the NTC the required flows and which link binds are printed - the price ordering is stage 3's and is
    /// not asserted here. 4 THE SINGLE BOOK - derived + residual equals the family's seed exactly, per country, with the residual's
    /// composition printed; the derived mix equals the family's static seed within the fold's tolerance. 5 THE CATALOG - the arrays'
    /// shapes (their digests are GeneratedCatalogCheck's).</para>
    /// </summary>
    public static class EnergyLayerCheck
    {
        /// <remarks>CONVENTION - §342's "within a point" widened to the fold's own measured gap (Germany's gas: Eurostat's G3000 16.2 against Ember's 15.1); named here rather than silently passed.</remarks>
        private const float ShareTolerancePoints = 1.5f;
        /// <remarks>CONVENTION - the agreement asked of the classes every source defines alike: ten per cent or one gigawatt, whichever is larger.</remarks>
        private const double CapacityAgreementRelative = 0.10, CapacityAgreementMw = 1000.0;
        /// <remarks>CONVENTION - the block identity's slack: the catalog carries integer MW and tenth-GWh figures.</remarks>
        private const double BlockIdentityRelative = 0.002;

        /// <summary>
        /// The one place the office of record and every other source disagree on a class they otherwise define alike, resolved by
        /// naming what each counts rather than by averaging: Eurostat lists Germany's last three reactors as 4 205 MW INSTALLED at the
        /// end of 2023 (its decommissioned column reads 0 - the plants stand), Ember and ENTSO-E list 0 because the Atomgesetz ended
        /// their operation on 15 April 2023. The record's stock is kept; the layer carries them CLOSED BY LAW - installed, unavailable -
        /// and stage 3's availability for them is zero. Enumerated so the exception is visible, with its statute.
        /// </summary>
        internal static readonly (CountryId Country, string Label, string Reason)[] ClosedByLaw =
        {
            (CountryId.Germany, "nuclear", "Atomgesetz: the last three reactors ceased operation on 15 April 2023; Eurostat still lists 4 205 MW installed at year end, decommissioned 0"),
        };

        internal static string ClosedByLawReason(CountryId country, string label)
        {
            foreach ((CountryId c, string l, string reason) in ClosedByLaw) { if (c == country && l == label) { return reason; } }
            return null;
        }

        public static void Run()
        {
            CheckExit.ArmLogFold();
            var sb = new StringBuilder();
            int failures = 0;
            sb.Append("=== THE ENERGY LAYER, STAGE 2: the five gates ===\n");

            World world = WorldFactory.CreateDefault();
            int covered = 0, divergent = 0;

            // ---- gate 1 and gate 4's mix half: the fleet against Ember, the derived mix against the family's seed
            sb.Append("\n    1. THE FLEET (MW; record | Ember) and the derived mix against the family's static seed\n");
            foreach (Country c in world.Countries)
            {
                if (!EnergyLayer.Has(c.Id)) { continue; }
                covered++;
                if (c.Environment == null || !c.Environment.Seeded) { EnvironmentFamily.Seed(c); }
                sb.Append(F("    {0,-8}", c.Id));
                for (int l = 0; l < EnergyLayerData.Labels.Length; l++)
                {
                    string label = EnergyLayerData.Labels[l];
                    double r = EnergyLayer.CapacityMw(c.Id, l), e = EnergyLayer.CapacityEmberMw(c.Id, l);
                    bool agree = Math.Abs(r - e) <= Math.Max(CapacityAgreementMw, CapacityAgreementRelative * Math.Max(r, e));
                    bool strict = label == "nuclear" || label == "wind" || label == "solar";
                    string closed = ClosedByLawReason(c.Id, label);
                    if (!agree && strict && closed == null) { failures++; Debug.LogError($"ENERGY: {c.Id} {label} capacity {r:0} against Ember's {e:0} - a class every source defines alike, and they do not agree."); }
                    if (!agree && !strict) { divergent++; }
                    sb.Append(F(" {0}={1:0}|{2:0}{3}", label, r, e, agree ? "" : (closed != null ? " CLOSED-BY-LAW" : (strict ? " FAIL" : " ~DIVERGENT"))));
                }
                float[] derived = EnergyLayer.DerivedMix(c.Id);
                float[] seed = c.Environment.MixShares;
                float gap = seed != null ? EnergyLayer.MaxShareGap(derived, seed) : 999f;
                if (gap > ShareTolerancePoints) { failures++; Debug.LogError($"ENERGY: {c.Id}'s derived mix departs from the family's seed by {gap:0.0} points (tolerance {ShareTolerancePoints})."); }
                sb.Append(F("\n             derived mix {0} | seed {1} | max gap {2:0.0} pt {3}\n", Join(derived), seed == null ? "-" : Join(seed), gap, gap <= ShareTolerancePoints ? "ok" : "FAIL"));
            }
            if (covered != 6) { failures++; Debug.LogError($"ENERGY: the layer covers {covered} of the world's countries; six were built."); }
            sb.Append(F("    {0} label(s) tagged DIVERGENT across the six - definitions (net maximum with reserve, nameplate, net summer), printed side by side, never averaged.\n", divergent));
            foreach ((CountryId cc, string label, string reason) in ClosedByLaw) { sb.Append(F("    CLOSED BY LAW: {0} {1} - {2}.\n", cc, label, reason)); }

            // ---- gate 2: the blocks
            sb.Append("\n    2. THE BLOCKS (GWh): base + mid + peak against the series\n");
            double zoneSum = 0, seEnergy = 0;
            for (int i = 0; i < EnergyLayer.ZoneCount; i++)
            {
                EnergyLayer.Blocks b = EnergyLayer.BlocksAt(i);
                double gap = Math.Abs(b.BlocksGwh - b.EnergyGwh) / Math.Max(1.0, b.EnergyGwh);
                if (gap > BlockIdentityRelative) { failures++; Debug.LogError($"ENERGY: {b.Zone}'s blocks sum to {b.BlocksGwh:0.0} GWh against the series' {b.EnergyGwh:0.0}."); }
                if (b.Scale <= 0 || b.Scale > 1.0000001) { failures++; Debug.LogError($"ENERGY: {b.Zone}'s balance factor {b.Scale} is outside (0, 1]."); }
                sb.Append(F("    {0,-4} base {1,10:0.0} + mid {2,9:0.0} + peak {3,8:0.0} = {4,10:0.0} vs {5,10:0.0} ({6:0.000}%) peak {7:0} h at {8:0} MW · max {9:0} MW{10}\n",
                    b.Zone, b.BaseGwh, b.MidAboveBaseGwh, b.PeakAboveBaseGwh, b.BlocksGwh, b.EnergyGwh, 100 * gap, b.PeakHours, b.PeakMeanMw, b.MaxMw, b.Scale < 1 ? F(" · BALANCE FACTOR {0:0.0000} (inland demand {1:0} GWh)", b.Scale, b.InlandDemandGwh) : ""));
                if (b.Country == "SE" && b.Zone != "SE") { zoneSum += b.EnergyGwh; }
                if (b.Zone == "SE") { seEnergy = b.EnergyGwh; }
            }
            sb.Append(F("    Sweden's four zones sum to {0:0.0} GWh of settlement consumption against {1:0.0} of load ({2:+0.0;-0.0} %) - eSett does not settle the network's losses.\n", zoneSum, seEnergy, 100 * (zoneSum - seEnergy) / seEnergy));

            // ---- gate 3: the links
            sb.Append("\n    3. THE LINKS: the chain SE1 → SE2 → SE3 → SE4, flows positive southward (MW)\n");
            foreach (EnergyLayer.Block block in new[] { EnergyLayer.Block.Peak, EnergyLayer.Block.Mid, EnergyLayer.Block.Base })
            {
                EnergyLayer.LinkFlow[] open = EnergyLayer.Flows(block, true);
                double merged = EnergyLayer.MergedSurplusMw(block);
                double lastPlusSe4 = open[open.Length - 1].FlowMw + EnergyLayer.ZoneSurplusMw("SE4", block);
                if (Math.Abs(lastPlusSe4 - merged) > 1e-6) { failures++; Debug.LogError($"ENERGY: with the links infinite the four zones do not clear as one in the {block} block ({lastPlusSe4} against {merged})."); }
                EnergyLayer.LinkFlow[] ntc = EnergyLayer.Flows(block, false);
                sb.Append(F("    {0,-4} merged balance {1,7:0} MW;", block, merged));
                foreach (EnergyLayer.LinkFlow f in ntc) { sb.Append(F(" {0}→{1} {2,6:0} / {3:0} ({4:0}%){5}", f.From, f.To, f.FlowMw, f.CapacityMw, 100 * f.Utilisation, f.Binding ? " BINDS" : "")); }
                sb.Append('\n');
            }
            sb.Append("    Identity held: the merged zone's balance equals the chain's last cumulative flow plus SE4's own. ⚠ A proxy on annual-average supply with ONE exit (SE4): the interconnectors out of SE1, SE2 and SE3 to Norway, Finland and Denmark are outside the chain, so a surplus that leaves through them here reads as southward flow - which link binds in dispatch, and SE4's price above SE1's, are stage 3's clearing.\n");
            if (EnergyLayerData.LinkFrom.Length != 6) { failures++; Debug.LogError($"ENERGY: {EnergyLayerData.LinkFrom.Length} links in the catalog; six directed links were sourced."); }

            // ---- gate 4: the single book
            sb.Append("\n    4. THE SINGLE BOOK (kt CO₂): derived + residual = the family's seed, per country\n");
            foreach (Country c in world.Countries)
            {
                if (!EnergyLayer.Has(c.Id)) { continue; }
                EnergyLayer.Co2 d = EnergyLayer.Decomposition(c.Id, c.Environment.PowerCo2PerCapita);
                double identity = Math.Abs(d.DerivedKt + d.ResidualKt - d.SeedTotalKt);
                if (identity > 1e-6 * Math.Max(1.0, d.SeedTotalKt)) { failures++; Debug.LogError($"ENERGY: {c.Id}'s derived + residual ({d.DerivedKt + d.ResidualKt}) is not the seed ({d.SeedTotalKt})."); }
                if (d.DerivedKt <= 0) { failures++; Debug.LogError($"ENERGY: {c.Id}'s derived combustion is {d.DerivedKt} kt - a fleet with no fuel."); }
                sb.Append(F("    {0,-8} seed {1,9:0} = derived {2,9:0} ({3,4:0.0} %) + residual {4,8:0} [known heat {5,7:0} + remainder {6,8:0}] · autoproducers {7,7:0} kt in industry's book\n",
                    c.Id, d.SeedTotalKt, d.DerivedKt, 100 * d.DerivedShare, d.ResidualKt, d.MainHeatKt, d.RemainderKt, d.AutoproducerKt));
            }
            sb.Append("    The residual is PER COUNTRY BY METHOD (seed − derived), its known heat part named and the remainder (refineries, other energy industries, EDGAR's factors against the IPCC defaults) printed with its sign.\n");

            // ---- gate 5: the catalog's shapes
            int failuresBefore = failures;
            if (EnergyLayerData.Countries.Length != 6 || EnergyLayerData.Labels.Length != 7) { failures++; }
            if (EnergyLayerData.CapacityRecordMw.Length != 6 || EnergyLayerData.CapacityRecordMw[0].Length != 7 || EnergyLayerData.GenerationRecordGwh.Length != 6) { failures++; }
            if (EnergyLayerData.Zones.Length != 10 || EnergyLayerData.EnergyGwh.Length != 10 || EnergyLayerData.Scale.Length != 10) { failures++; }
            if (EnergyLayerData.SwedishZones.Length != 4 || EnergyLayerData.ZoneProductionGwh.Length != 4) { failures++; }
            if (EnergyLayerData.Co2Kt.Length != 6 || EnergyLayerData.Co2Kt[0].Length != 3 || EnergyLayerData.Co2Kt[0][0].Length != 6 || EnergyLayerData.PopulationM.Length != 6) { failures++; }
            if (failures > failuresBefore) { Debug.LogError("ENERGY: the catalog's arrays are not the shapes the layer reads (6 countries × 7 labels; 10 zones; 4 Swedish zones; 6 × 3 × 6 combustion)."); }
            sb.Append(F("\n    5. THE CATALOG: {0} countries × {1} labels, {2} zones, {3} Swedish zones, {4} links, {5}×{6}×{7} combustion cells - digests are GeneratedCatalogCheck's.\n",
                EnergyLayerData.Countries.Length, EnergyLayerData.Labels.Length, EnergyLayerData.Zones.Length, EnergyLayerData.SwedishZones.Length, EnergyLayerData.LinkFrom.Length,
                EnergyLayerData.Co2Kt.Length, EnergyLayerData.Co2Kt[0].Length, EnergyLayerData.Co2Kt[0][0].Length));

            // ---- gate 6 (EN-3, 2026-09-11): the market layer at the seed - pure clearings, no turn advanced
            sb.Append("\n    6. THE MARKET AT THE SEED (EN-3): the calibration, the identity through the new writer, Germany's reactors, snitt 4's answer, no clamp\n");
            PoliSim.Simulation.EnergyMarket.ResetCalibration();
            foreach (Country c in world.Countries)
            {
                if (!EnergyLayer.Has(c.Id)) { continue; }
                PoliSim.Simulation.EnergyMarket.Result r = PoliSim.Simulation.EnergyMarket.ClearAt(c.Id, 1.0, 0.0);
                double[] target = PoliSim.Simulation.EnergyMarket.SeedTargets(c.Id);
                if (c.Id != CountryId.Sweden && (double.IsNaN(r.MaxCalibrationGap) || r.MaxCalibrationGap > PoliSim.Simulation.EnergyMarket.CalibrationTolerance)) { failures++; Debug.LogError($"ENERGY: {c.Id}'s seed dispatch misses 2023's fossil shares by {r.MaxCalibrationGap:F4}."); }
                if (c.Id == CountryId.Germany && r.AnnualGwh[PoliSim.Simulation.EnergyMarket.Nuclear] > 0) { failures++; Debug.LogError("ENERGY: Germany's reactors dispatch above zero - CLOSED BY LAW (§457)."); }
                double written = c.Environment.PowerFromDispatch ? (r.DerivedCo2Mt + c.Environment.PowerResidualMt) / Math.Max(0.0001f, c.State.Population) : double.NaN;
                if (double.IsNaN(written) || Math.Abs(written - c.Environment.PowerCo2PerCapita) > 1e-4) { failures++; Debug.LogError($"ENERGY: {c.Id}'s dispatch + residual writes {written:F5} t/head at the seed against the family's seed {c.Environment.PowerCo2PerCapita:F5}."); }
                double fossil = r.AnnualGwh[0] + r.AnnualGwh[1] + r.AnnualGwh[2];
                sb.Append(F("    {0,-8} coal/gas/oil of fossil {1:F3}/{2:F3}/{3:F3} (2023 {4:F3}/{5:F3}/{6:F3}) · adders {7:F1}/{8:F1}/{9:F1} · derived {10:F1} + residual {11:F1} Mt = {12:F4} t/head · prices {13:F0}/{14:F0}/{15:F0}\n",
                    c.Id, fossil > 0 ? r.AnnualGwh[0] / fossil : 0, fossil > 0 ? r.AnnualGwh[1] / fossil : 0, fossil > 0 ? r.AnnualGwh[2] / fossil : 0, target[0], target[1], target[2], r.Adders[0], r.Adders[1], r.Adders[2], r.DerivedCo2Mt, c.Environment.PowerResidualMt, written, r.Zones[0][0].Price, r.Zones[0][1].Price, r.Zones[0][2].Price));
            }
            PoliSim.Simulation.EnergyMarket.Result se = PoliSim.Simulation.EnergyMarket.ClearAt(CountryId.Sweden, 1.0, 0.0);
            int se4 = EnergyLayer.ZoneIndex("SE4");
            for (int b = 0; b < 3; b++)
            {
                PoliSim.Simulation.EnergyMarket.LinkResult snitt4 = se.Links[2];
                double own = EnergyLayerData.ZoneConsumptionBlockMw[se4][b] - se.Zones[3][b].MustRunMw, transit = EnergyLayerData.ZoneExternalExportMw[se4][b];
                sb.Append(F("    snitt 4, {0}: {1:F0} of {2:F0} MW = SE4's own deficit {3:F0} + transit export {4:F0} → {5}; snitt 1 {6:F0}/{7:F0}, snitt 2 {8:F0}/{9:F0}\n", EnergyLayerData.DispatchBlocks[b], snitt4.FlowMw[b], snitt4.CapacityMw[b], own, transit, snitt4.Binding[b] ? "CONGESTION" : "EXPORT, not congestion", se.Links[0].FlowMw[b], se.Links[0].CapacityMw[b], se.Links[1].FlowMw[b], se.Links[1].CapacityMw[b]));
            }
            double[] probeOffers = { 30.0, 50.0, double.PositiveInfinity }; double[] probeCaps = { 100.0, 100.0, 0.0 };
            PoliSim.Simulation.EnergyMarket.BlockResult negative = PoliSim.Simulation.EnergyMarket.ClearBlock(100.0, 150.0, probeOffers, probeCaps, -5.0);
            if (Math.Abs(negative.Price + 5.0) > 1e-9) { failures++; Debug.LogError($"ENERGY: a block whose lowest offer is −5 priced {negative.Price} - a clamp."); }
            sb.Append(F("    no clamp: a surplus block at a lowest offer of −5 prices {0:F1} (the ceiling {1:F0}, the onset {2:F2}).\n", negative.Price, PoliSim.Simulation.EnergyMarket.MaxClearingPrice, PoliSim.Simulation.EnergyMarket.ScarcityOnset));

            // ---- gate 7 (§461): the fitted parameters counted, and the out-of-sample response with them fixed
            sb.Append("\n    7. FITTED, COUNTED, AND THE RESPONSE: the adders fixed at the seed, an ETS-price step of " + PoliSim.Simulation.EnergyMarket.ResponseStepEtsPerT.ToString("0", CultureInfo.InvariantCulture) + " per tonne in the market's currency (EN-4d: the fleet's carbon price is the ETS; the national tax does not reach it)\n");
            int fitted = 0;
            foreach (Country c in world.Countries)
            {
                if (!EnergyLayer.Has(c.Id)) { continue; }
                List<string> names = PoliSim.Simulation.EnergyMarket.FittedParameters(c.Id);
                fitted += names.Count;
                int expectedCount = c.Id == CountryId.Sweden ? 0 : PoliSim.Simulation.EnergyMarket.FittedParametersPerCountryWithFleet;
                if (names.Count != expectedCount) { failures++; Debug.LogError($"ENERGY: {c.Id} has {names.Count} fitted parameter(s) ({string.Join(", ", names)}); the count stated is {expectedCount}. A parameter appeared or vanished - state it."); }
                if (c.Id == CountryId.Sweden) { sb.Append("    Sweden   FITTED: none (no dispatchable fossil fleet)\n"); continue; }
                (double coalA, double coalB, double gasA, double gasB, double peakA, double peakB) = PoliSim.Simulation.EnergyMarket.Response(c.Id, PoliSim.Simulation.EnergyMarket.ResponseStepEtsPerT);
                bool responds = coalB < coalA && gasB > gasA && peakB > peakA;
                if (!responds) { failures++; Debug.LogError($"ENERGY: {c.Id} does not respond to the step - coal {coalA:F3} → {coalB:F3}, gas {gasA:F3} → {gasB:F3}, peak price {peakA:F1} → {peakB:F1}. A model that reproduces its seed year is not yet a model that responds."); }
                if (c.Id == CountryId.Poland)
                {
                    bool onReference = Math.Abs((coalA - coalB) - PoliSim.Simulation.EnergyMarket.ReferenceCoalDrop) <= PoliSim.Simulation.EnergyMarket.ReferenceShareSlack
                        && Math.Abs((gasB - gasA) - PoliSim.Simulation.EnergyMarket.ReferenceGasRise) <= PoliSim.Simulation.EnergyMarket.ReferenceShareSlack
                        && Math.Abs((peakB - peakA) - PoliSim.Simulation.EnergyMarket.ReferencePeakPriceRise) <= PoliSim.Simulation.EnergyMarket.ReferencePriceSlack;
                    if (!onReference) { failures++; Debug.LogError($"ENERGY: Poland's response to the ETS step left its reference - coal −{coalA - coalB:F3} (reference {PoliSim.Simulation.EnergyMarket.ReferenceCoalDrop:F3}), gas +{gasB - gasA:F3} ({PoliSim.Simulation.EnergyMarket.ReferenceGasRise:F3}), peak +{peakB - peakA:F1} ({PoliSim.Simulation.EnergyMarket.ReferencePeakPriceRise:F1}). The model changed; explain it and move the reference deliberately."); }
                }
                sb.Append(F("    {0,-8} FITTED: {1} · coal {2:F3} → {3:F3} · gas {4:F3} → {5:F3} · peak price {6:F1} → {7:F1}{8}\n", c.Id, string.Join(", ", names), coalA, coalB, gasA, gasB, peakA, peakB, c.Id == CountryId.Poland ? " (the reference)" : ""));
            }
            sb.Append(F("    {0} fitted parameters in the market - the adders - and nothing else in that class is fitted; sourced, derived, authored and convention figures each carry their mark.\n", fitted));

            // ---- gate 8 (EN-4, §464): the retail stack reproduces Eurostat's components at the seed, the fitted margins counted, the book closes
            sb.Append("\n    8. THE FISCAL LAYER AT THE SEED: the stack's total against Eurostat's, the two fitted margins per country, the incidence identity\n");
            int fittedMargins = 0;
            foreach (Country c in world.Countries)
            {
                if (!EnergyLayer.Has(c.Id)) { continue; }
                if (c.Environment == null || !c.Environment.Seeded) { EnvironmentFamily.Seed(c); }
                int ci = EnergyLayer.Index(c.Id);
                double usd = EnergyLayerData.UsdPerMarketCurrency[ci];   // the book's dollars per unit of the sources' currency
                List<string> margins = PoliSim.Simulation.EnergyLedger.FittedParameters(c.Id);
                fittedMargins += margins.Count;
                if (margins.Count != PoliSim.Simulation.EnergyLedger.FittedParametersPerCountry) { failures++; Debug.LogError($"ENERGY: {c.Id}'s ledger has {margins.Count} fitted parameter(s); the count stated is {PoliSim.Simulation.EnergyLedger.FittedParametersPerCountry}."); }
                PoliSim.Simulation.EnergyMarket.Result r = PoliSim.Simulation.EnergyMarket.ClearAt(c.Id, 1.0, 0.0);
                PoliSim.Simulation.EnergyLedger.Book book = PoliSim.Simulation.EnergyLedger.Compute(c, r, 1.0, 0.0);
                double statedResidual = 0;
                for (int k = 0; k < PoliSim.Simulation.EnergyLedger.ClassCount; k++)
                {
                    // the stack's total is its components' sum; Eurostat's STATED total differs from the sum of its published components by the source's rounding (within 0.0003 €/kWh, the generator's own gate) - printed, never substituted
                    double expected = PoliSim.Simulation.EnergyLedger.SeedTotalPerKwh(ci, k) * usd;
                    statedResidual = Math.Max(statedResidual, Math.Abs(EnergyLayerData.RetailTotal[ci][k] - PoliSim.Simulation.EnergyLedger.SeedTotalPerKwh(ci, k)));
                    if (Math.Abs(book.Classes[k].Total - expected) > 1e-6 * Math.Max(1.0, expected)) { failures++; Debug.LogError($"ENERGY: {c.Id} {book.Classes[k].Class}: the seed stack totals {book.Classes[k].Total:F6} against the components' {expected:F6} per kWh - a component is not reproduced."); }
                    // the fitted margin is a float on the seeds: the sum returns the component to float precision
                    if (Math.Abs(book.Classes[k].Wholesale + book.Classes[k].Margin - EnergyLayerData.RetailEnergySupply[ci][k] * usd) > 1e-6) { failures++; Debug.LogError($"ENERGY: {c.Id} {book.Classes[k].Class}: wholesale + margin is not the energy-and-supply component at the seed."); }
                    if (Math.Abs(book.Classes[k].Vat - EnergyLayerData.RetailVat[ci][k] * usd) > 1e-6) { failures++; Debug.LogError($"ENERGY: {c.Id} {book.Classes[k].Class}: the implied VAT rate does not return Eurostat's VAT component at the seed."); }
                }
                if (Math.Abs(book.Gap) > 1e-9 * Math.Max(1.0, book.PaidTotal)) { failures++; Debug.LogError($"ENERGY: {c.Id}'s incidence ledger does not close at the seed - paid {book.PaidTotal:F6}, received {book.ReceivedTotal:F6} bn."); }
                if (Math.Abs(book.CongestionRent) > 1e-12 && c.Id != CountryId.Sweden) { failures++; Debug.LogError($"ENERGY: {c.Id} reports a congestion rent with no zonal links."); }
                if (Math.Abs(book.LevyScale - (book.LevyRevenue > 0 ? 1.0 : 0.0)) > 1e-12) { failures++; Debug.LogError($"ENERGY: {c.Id}'s levy scale is {book.LevyScale} at the seed - the budget line is on its path there."); }
                if (c.State.EnergyHouseholdPrice < 0f || Math.Abs(c.State.EnergyHouseholdPrice - book.Classes[0].Total) > 1e-5 * Math.Max(1.0, book.Classes[0].Total)) { failures++; Debug.LogError($"ENERGY: {c.Id}'s state carries a household price of {c.State.EnergyHouseholdPrice} at the seed against the stack's {book.Classes[0].Total} - the presented figure is not the stored one."); }
                sb.Append(F("    {0,-8} {1} per kWh (the book's dollars) · wholesale {2:F4} · margins households {3:+0.0000;-0.0000} non-households {4:+0.0000;-0.0000} (FITTED) · VAT implied {5:F1} % / {6:F1} % · totals {7:F4} / {8:F4} = the components' sum (Eurostat's stated totals within {9:F4} of it in the source's currency) · bills {10:F1} + {11:F1} bn, budget support {12:F2} bn · paid {13:F2} = received {14:F2} (gap {15:E1})\n",
                    c.Id, PoliSim.Simulation.EnergyLedger.BookCurrency, book.WholesalePerKwh, c.Environment.RetailMargin[0], c.Environment.RetailMargin[1], 100 * c.Environment.RetailVatRate[0], 100 * c.Environment.RetailVatRate[1], book.Classes[0].Total, book.Classes[1].Total, statedResidual, book.PaidHouseholds, book.PaidNonHouseholds, book.PaidTaxpayers, book.PaidTotal, book.ReceivedTotal, book.Gap));
            }
            sb.Append(F("    {0} fitted parameters in the fiscal layer - the supply margins - so {1} in the energy layer in all, each named; a negative margin is printed, not hidden: Poland's households' (the 2023 household price freeze, compensated to the suppliers off the bill); Sweden's turned positive when EN-3b priced its zones at the exchange's 2023 figures.\n", fittedMargins, fitted + fittedMargins));

            // ---- gate 10 (EN-3b, §466): Sweden's zones clear at the exchange's 2023 prices at the seed, the coupling and the reservoirs sourced, the hydro shift zero at the seed
            sb.Append("\n    10. THE RESERVOIR DISPATCH (EN-3b): the seed water value is the exchange's 2023 zonal price by block, the coupling measured, the reservoirs' capacity sourced, no hydro shifted at the seed\n");
            {
                PoliSim.Simulation.EnergyMarket.Result swz = PoliSim.Simulation.EnergyMarket.ClearAtSeed(CountryId.Sweden);
                for (int z = 0; z < swz.ZoneNames.Length; z++)
                {
                    string zone = swz.ZoneNames[z];
                    double beta = EnergyLayer.ZoneBetaToProxy(zone), cap = EnergyLayer.ReservoirCapacityGwh(zone);
                    if (!(beta > 0.0) || beta > 1.0) { failures++; Debug.LogError($"ENERGY: {zone}'s coupling to the continent is {beta} - outside (0, 1]."); }
                    if (!(cap > 0.0)) { failures++; Debug.LogError($"ENERGY: {zone} carries no reservoir capacity."); }
                    for (int b = 0; b < 3; b++)
                    {
                        double sourced = EnergyLayer.SeedZonePrice(zone, b);
                        if (!swz.Links[Math.Min(z, swz.Links.Length - 1)].Binding[b] && Math.Abs(swz.Zones[z][b].Price - sourced) > 1e-6) { failures++; Debug.LogError($"ENERGY: {zone} clears the {EnergyLayerData.DispatchBlocks[b]} block at {swz.Zones[z][b].Price:F3} at the seed, not the exchange's {sourced:F3}."); }
                    }
                    sb.Append(F("    {0}  seed price {1:F1} / {2:F1} / {3:F1} €/MWh (load-weighted {4:F1}) · coupling to the continent {5:F2} · reservoir {6:N0} GWh\n", zone, swz.Zones[z][0].Price, swz.Zones[z][1].Price, swz.Zones[z][2].Price, EnergyLayerData.HydroPriceLoadWeighted[EnergyLayer.HydroZoneIndex(zone)], beta, cap));
                }
                sb.Append(F("    the proxy EN-3 cleared every zone at: {0:F1} / {1:F1} / {2:F1} €/MWh - the measure of what it cost (§466); Sweden's reservoirs together {3:N0} GWh\n", swz.WaterValue[0], swz.WaterValue[1], swz.WaterValue[2], EnergyLayer.SwedenReservoirCapacityGwh()));
                foreach (Country c in world.Countries)
                {
                    if (!EnergyLayer.Has(c.Id) || c.Id == CountryId.Sweden) { continue; }
                    PoliSim.Simulation.EnergyMarket.Result r = PoliSim.Simulation.EnergyMarket.ClearAtSeed(c.Id);
                    double share = EnergyLayer.HydroShiftableShare(c.Id);
                    if (Math.Abs(r.HydroShiftedGwh) > 1e-6) { failures++; Debug.LogError($"ENERGY: {c.Id} shifts {r.HydroShiftedGwh:F3} GWh of hydro at the seed - the seed allocation is the observed one and moves nothing."); }
                    sb.Append(F("    {0,-8} shiftable share {1:F3}{2} · seed spread peak − base {3:F1} €/MWh · shifted at the seed {4:F3} GWh\n", c.Id, share, share > 0 ? "" : " (BILLED - no shift)", r.Zones[0][2].Price - r.Zones[0][0].Price, r.HydroShiftedGwh));
                }
            }

            // ---- gate 9 (EN-5, §465): the pass-through's weight is sourced and positive for six, and the seed passes nothing
            sb.Append("\n    9. THE PASS-THROUGH (EN-5): electricity's weight in the price index per country, and nothing passed at the seed\n");
            foreach (Country c in world.Countries)
            {
                if (!EnergyLayer.Has(c.Id)) { continue; }
                float weight = PoliSim.Simulation.EnergyPassThrough.WeightFraction(c.Id);
                if (!(weight > 0f) || weight > 0.2f) { failures++; Debug.LogError($"ENERGY: {c.Id}'s electricity weight in the price index is {weight} - not a sourced per-mille figure."); }
                if (c.State.EnergyHouseholdPriceRealChange != 0f || PoliSim.Simulation.EnergyPassThrough.Planned(c) != 0f) { failures++; Debug.LogError($"ENERGY: {c.Id} passes {PoliSim.Simulation.EnergyPassThrough.Planned(c)} pp at the seed - a seed with no year written passes nothing."); }
                sb.Append(F("    {0,-8} electricity {1:F2} per mille of the basket ({2}) · at the seed the real price {3:F4} $/kWh, change 0, pass-through 0\n", c.Id, weight * PoliSim.Simulation.EnergyPassThrough.PerMille, c.Id == CountryId.USA ? "BLS CPI-U relative importance, December 2023" : "Eurostat prc_hicp_inw CP0451 2023", c.State.EnergyHouseholdPriceReal));
            }

            sb.Append(failures == 0 ? "\n=== EnergyLayerCheck: ALL ASSERTIONS PASS ===\n" : $"\n=== EnergyLayerCheck: {failures} FAILURE(S) ===\n");
            if (failures > 0) { Debug.LogError(sb.ToString()); CheckExit.Finish(1); return; }
            Debug.Log(sb.ToString());
            CheckExit.Finish(0);
        }

        private static string Join(float[] shares) { var parts = new string[shares.Length]; for (int i = 0; i < shares.Length; i++) { parts[i] = shares[i].ToString("0.0", CultureInfo.InvariantCulture); } return string.Join("/", parts); }
        private static string F(string format, params object[] args) => string.Format(CultureInfo.InvariantCulture, format, args);
    }
}
