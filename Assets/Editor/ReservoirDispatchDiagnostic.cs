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
    /// EN-3b (2026-09-11), the reservoir dispatch - it builds and advances worlds, so it belongs to the simulation group. (1) THE MEASURE: Sweden's
    /// zones clear at the exchange's 2023 prices at the seed, against the proxy EN-3 cleared them at, and the fiscal layer's Swedish margins with them.
    /// (2) THE COUPLING: Germany's fleet's ETS price stepped fifty dollars per tonne (a probe on that fleet alone - since EN-4d, §467, the national
    /// carbon tax does not reach ETS-covered plant) moves each Swedish zone by its own measured share of the proxy's move, read in the step's own year
    /// (the year after carries Sweden's own price level, moved by the step's pass-through, beside the coupling).
    /// (3) THE RESERVOIR: an inflow shortfall of a fifth (a probe) runs a deficit, the water value rises by the slope times the deficit share, the
    /// balance is bounded by the store; at the 2023 inflow the balance stays zero. (4) THE SHIFT: Italy's fleet's ETS price stepped fifty dollars per tonne
    /// moves the peak–base spread off the seed's, the shiftable hydro moves toward the peak and brings it back within the tolerance; the year's hydro
    /// energy is conserved; at the seed nothing moves. (5) B6: at a doubled price level with the continent doubled first, every Swedish zone's price
    /// doubles.
    /// </summary>
    public static class ReservoirDispatchDiagnostic
    {
        private const int Years = 6;
        private const float RaiseUsdPerTonne = 50f;
        private const double InflowShortfall = 0.2;

        public static void Run()
        {
            CheckExit.ArmLogFold();
            bool ok = true;
            var sb = new StringBuilder();
            sb.Append("=== RESERVOIR DISPATCH (EN-3b): the water value sourced and coupled, the reservoirs carried, the shiftable hydro ===\n");

            SimulationRandom.Seed(777); EnergyMarket.ResetCalibration(); EnergyMarket.ResetTurnState();
            World world = WorldFactory.CreateDefault();
            EnergyMarket.BeginTurn(world);

            // (1) the measure
            sb.Append("\n    1. THE MEASURE: the zones at the seed against the proxy, and the Swedish margins\n");
            {
                EnergyMarket.Result se = EnergyMarket.ClearAtSeed(CountryId.Sweden);
                double lwProxy = EnergyLedger.LoadWeightedPricePerMwh(new EnergyMarket.Result { Country = CountryId.Sweden, ZoneNames = se.ZoneNames, Zones = ProxyZones(se) });
                double lwNow = EnergyLedger.LoadWeightedPricePerMwh(se);
                for (int z = 0; z < se.ZoneNames.Length; z++)
                {
                    for (int b = 0; b < 3; b++) { if (Math.Abs(se.Zones[z][b].Price - EnergyLayer.SeedZonePrice(se.ZoneNames[z], b)) > 1e-6) { ok = false; Debug.LogError($"RESERVOIR: {se.ZoneNames[z]} clears the {EnergyLayerData.DispatchBlocks[b]} block at {se.Zones[z][b].Price:F3} at the seed, not the exchange's {EnergyLayer.SeedZonePrice(se.ZoneNames[z], b):F3}."); } }
                    sb.Append(F("    {0}  {1:F1} / {2:F1} / {3:F1} €/MWh (the proxy: {4:F1} / {5:F1} / {6:F1}) · β {7:F2} · reservoir {8:N0} GWh\n", se.ZoneNames[z], se.Zones[z][0].Price, se.Zones[z][1].Price, se.Zones[z][2].Price, se.WaterValue[0], se.WaterValue[1], se.WaterValue[2], EnergyLayer.ZoneBetaToProxy(se.ZoneNames[z]), EnergyLayer.ReservoirCapacityGwh(se.ZoneNames[z])));
                }
                Country sweden = world.GetCountry(CountryId.Sweden);
                if (!(lwNow < lwProxy)) { ok = false; Debug.LogError($"RESERVOIR: Sweden's load-weighted wholesale {lwNow:F1} is not below the proxy's {lwProxy:F1}."); }
                if (!(sweden.Environment.RetailMargin[0] > 0f) || !(sweden.Environment.RetailMargin[1] > 0f)) { ok = false; Debug.LogError($"RESERVOIR: Sweden's fitted margins are {sweden.Environment.RetailMargin[0]:F4} / {sweden.Environment.RetailMargin[1]:F4} - the exchange's prices should leave a positive supply margin."); }
                sb.Append(F("    Sweden's load-weighted wholesale {0:F1} €/MWh against the proxy's {1:F1} - the measure of what the proxy cost, {2:F1} €/MWh ({3:F0} %); the fiscal layer's Swedish margins now +{4:F4} / +{5:F4} $/kWh (households / non-households), positive where §463 had −0.0068 / −0.0188\n", lwNow, lwProxy, lwProxy - lwNow, 100 * (lwProxy - lwNow) / lwNow, sweden.Environment.RetailMargin[0], sweden.Environment.RetailMargin[1]));
            }

            // (2) the coupling
            float eurRaise = (float)(RaiseUsdPerTonne * EnergyLayer.NationalPerUsd(CountryId.Germany));
            sb.Append(F("\n    2. THE COUPLING: Germany's fleet's ETS price stepped {0:F0} EUR/t from the top of year 1 (a probe on that fleet alone; EN-4d); each Swedish zone moves by its own share of the proxy's move\n", eurRaise));
            {
                Outcome untouched = RunWorld(Years, null, 0f, 1.0);
                Outcome raised = RunWorld(Years, CountryId.Germany, eurRaise, 1.0);
                // EN-4d: the step stands at the top of year 1's turn, where the water value is set, so year 1's clearing carries the whole move while the two runs'
                // Swedish price levels are still one (the pass-through and the bills reach Sweden's own level from year 2) - the share is read exactly in year 1. In year 2
                // the zone's own seed price rides a level the stepped run has moved, so the apparent share carries ΔP × (seed − β × proxy seed) beside β × the proxy's
                // move (the first run of this form read year 2 as the tax-decision form had, and found SE3's base at 0.595 for 0.60 - the level's residual, not the coupling's)
                for (int b = 0; b < 3; b++)
                {
                    double proxyMove = raised.Proxy[1][b] - untouched.Proxy[1][b];
                    for (int z = 0; z < 4; z++)
                    {
                        double move = raised.ZonePrice[1][z][b] - untouched.ZonePrice[1][z][b];
                        double expected = EnergyLayer.ZoneBetaToProxy(EnergyLayer.SwedenZones[z]) * proxyMove;
                        if (Math.Abs(move - expected) > 0.01 + 1e-3 * Math.Abs(expected)) { ok = false; Debug.LogError($"RESERVOIR: {EnergyLayer.SwedenZones[z]}'s {EnergyLayerData.DispatchBlocks[b]} block moved {move:F4} in year 1 on a proxy move of {proxyMove:F4} - not its share {EnergyLayer.ZoneBetaToProxy(EnergyLayer.SwedenZones[z]):F2}."); }
                    }
                    double proxyMove2 = raised.Proxy[2][b] - untouched.Proxy[2][b];
                    sb.Append(F("    {0,-4} year 1: proxy {1:+0.00;-0.00} €/MWh → SE1 {2:+0.00;-0.00} · SE2 {3:+0.00;-0.00} · SE3 {4:+0.00;-0.00} · SE4 {5:+0.00;-0.00}; year 2: proxy {6:+0.00;-0.00} → SE3 {7:+0.00;-0.00} (an apparent share of {8:F3} for β 0.60 - the rest is Sweden's own price level, moved by the step's pass-through)\n",
                        EnergyLayerData.DispatchBlocks[b], proxyMove, raised.ZonePrice[1][0][b] - untouched.ZonePrice[1][0][b], raised.ZonePrice[1][1][b] - untouched.ZonePrice[1][1][b], raised.ZonePrice[1][2][b] - untouched.ZonePrice[1][2][b], raised.ZonePrice[1][3][b] - untouched.ZonePrice[1][3][b],
                        proxyMove2, raised.ZonePrice[2][2][b] - untouched.ZonePrice[2][2][b], (raised.ZonePrice[2][2][b] - untouched.ZonePrice[2][2][b]) / Math.Max(1e-9, proxyMove2)));
                }
                // (3) the reservoir
                sb.Append(F("\n    3. THE RESERVOIR: an inflow of {0:F0} % of the 2023 hydro energy (a probe), against the 2023 inflow\n", 100 * (1 - InflowShortfall)));
                Outcome dry = RunWorld(Years, null, 0f, 1.0 - InflowShortfall);
                double capacity = EnergyLayer.SwedenReservoirCapacityGwh();
                for (int y = 1; y <= Years; y++) { if (Math.Abs(untouched.Balance[y]) > 1e-3) { ok = false; Debug.LogError($"RESERVOIR: the balance is {untouched.Balance[y]:F3} GWh in year {y} at the 2023 inflow - the cycle should close."); } }
                if (!(dry.Balance[1] < 0) || !(dry.Balance[Years] <= dry.Balance[1])) { ok = false; Debug.LogError($"RESERVOIR: a shortfall did not run a deficit ({dry.Balance[1]:F0} GWh in year 1, {dry.Balance[Years]:F0} in year {Years})."); }
                if (dry.Balance[Years] < -capacity - 1e-6) { ok = false; Debug.LogError("RESERVOIR: the deficit ran past the store."); }
                double deficitShareY2 = Math.Max(0, -dry.Balance[1]) / capacity;   // year 2's turn reads year 1's balance
                double expectedFactor = 1.0 + EnergyMarket.ReservoirDeficitSlope * deficitShareY2;
                double observed = dry.ZonePrice[2][2][1] / untouched.ZonePrice[2][2][1];   // SE3, mid block, year 2
                if (Math.Abs(observed - expectedFactor) > 1e-6) { ok = false; Debug.LogError($"RESERVOIR: SE3's mid price factor under the deficit is {observed:F5}, not 1 + slope × deficit share ({expectedFactor:F5})."); }
                sb.Append(F("    balance at the 2023 inflow: 0 in every year; at {0:F0} %: {1:+0;-0} GWh after year 1, {2:+0;-0} after year {3} (the store {4:N0}) - deficit share {5:F3} in year 2, SE3's mid price ×{6:F4} = 1 + {7:F2} × {5:F3}\n", 100 * (1 - InflowShortfall), dry.Balance[1], dry.Balance[Years], Years, capacity, deficitShareY2, observed, EnergyMarket.ReservoirDeficitSlope));
            }

            // (4) the shift: Italy
            float itRaise = (float)(RaiseUsdPerTonne * EnergyLayer.NationalPerUsd(CountryId.Italy));
            sb.Append(F("\n    4. THE SHIFT: Italy's fleet's ETS price stepped {0:F0} EUR/t at the seed dispatch (EN-4d: the fleet's carbon price) - the shiftable hydro restores the seed's peak–base spread\n", itRaise));
            {
                Country it = world.GetCountry(CountryId.Italy);
                EnergyMarket.Result seed = EnergyMarket.ClearAtSeed(CountryId.Italy);
                double seedSpread = EnergyMarket.SeedSpread(CountryId.Italy);
                if (Math.Abs(seed.HydroShiftedGwh) > 1e-9) { ok = false; Debug.LogError($"RESERVOIR: Italy shifts {seed.HydroShiftedGwh:F3} GWh at the seed."); }
                EnergyMarket.Result raisedIt = EnergyMarket.ClearAt(CountryId.Italy, 1.0, itRaise);
                double spreadAfter = raisedIt.Zones[0][2].Price - raisedIt.Zones[0][0].Price;
                // the unshifted spread under the raise, for the print: clear with the share taken away is not possible from here, so read the shift's size and direction instead
                if (!(raisedIt.HydroShiftedGwh > 0)) { ok = false; Debug.LogError("RESERVOIR: Italy's ETS step moved no hydro - the spread should have left the seed's and the operator answered."); }
                if (Math.Abs(spreadAfter - seedSpread) > EnergyMarket.SpreadTolerance + 1e-6 && raisedIt.HydroShiftedGwh < EnergyLayer.HydroShiftableShare(CountryId.Italy) * seed.AnnualGwh[EnergyMarket.Hydro] - 1e-6) { ok = false; Debug.LogError($"RESERVOIR: Italy's spread after the shift is {spreadAfter:F2} against the seed's {seedSpread:F2} with shiftable water left ({raisedIt.HydroShiftedGwh:F1} of {EnergyLayer.HydroShiftableShare(CountryId.Italy) * seed.AnnualGwh[EnergyMarket.Hydro]:F1} GWh)."); }
                if (Math.Abs(raisedIt.AnnualGwh[EnergyMarket.Hydro] - seed.AnnualGwh[EnergyMarket.Hydro]) > 1e-6) { ok = false; Debug.LogError("RESERVOIR: the shift changed the year's hydro energy."); }
                sb.Append(F("    seed spread {0:F2} €/MWh, no shift; under the raise: spread {1:F2}, hydro moved {2:F1} GWh of {3:F1} shiftable ({4:+0;-0} MW on the base block, {5:+0;-0} on the peak), hydro energy {6:N0} = {7:N0} GWh, CO₂ {8:F2} → {9:F2} Mt, peak price {10:F1} → {11:F1}, base {12:F1} → {13:F1}\n",
                    seedSpread, spreadAfter, raisedIt.HydroShiftedGwh, EnergyLayer.HydroShiftableShare(CountryId.Italy) * seed.AnnualGwh[EnergyMarket.Hydro], raisedIt.HydroShiftMw[0], raisedIt.HydroShiftMw[2], seed.AnnualGwh[EnergyMarket.Hydro], raisedIt.AnnualGwh[EnergyMarket.Hydro], seed.DerivedCo2Mt, raisedIt.DerivedCo2Mt, seed.Zones[0][2].Price, raisedIt.Zones[0][2].Price, seed.Zones[0][0].Price, raisedIt.Zones[0][0].Price));
                _ = it;
            }

            // (5) B6
            sb.Append("\n    5. B6: at a doubled price level, the continent doubled first, every Swedish zone's price doubles\n");
            {
                SimulationRandom.Seed(777); EnergyMarket.ResetCalibration(); EnergyMarket.ResetTurnState();
                World w = WorldFactory.CreateDefault(); EnergyMarket.BeginTurn(w);
                EnergyMarket.Result at1 = EnergyMarket.Clear(w.GetCountry(CountryId.Sweden));
                foreach (Country c in w.Countries) { c.State.PriceLevel = 2f; }
                EnergyMarket.BeginTurn(w);
                EnergyMarket.Result at2 = EnergyMarket.Clear(w.GetCountry(CountryId.Sweden));
                for (int z = 0; z < 4; z++) { for (int b = 0; b < 3; b++) { if (Math.Abs(at2.Zones[z][b].Price - 2 * at1.Zones[z][b].Price) > 1e-6) { ok = false; Debug.LogError($"RESERVOIR: {at1.ZoneNames[z]}'s {EnergyLayerData.DispatchBlocks[b]} price at a doubled level is {at2.Zones[z][b].Price:F3} against twice {at1.Zones[z][b].Price:F3}."); } } }
                sb.Append(F("    SE3 {0:F1} / {1:F1} / {2:F1} → {3:F1} / {4:F1} / {5:F1} €/MWh\n", at1.Zones[2][0].Price, at1.Zones[2][1].Price, at1.Zones[2][2].Price, at2.Zones[2][0].Price, at2.Zones[2][1].Price, at2.Zones[2][2].Price));
                foreach (Country c in w.Countries) { c.State.PriceLevel = 1f; }
                EnergyMarket.ResetTurnState();
            }

            sb.Append(ok ? "\n=== ReservoirDispatchDiagnostic: ALL ASSERTIONS PASS ===\n" : "\n=== ReservoirDispatchDiagnostic: FAILED (see above) ===\n");
            if (ok) { Debug.Log(sb.ToString()); } else { Debug.LogError(sb.ToString()); }
            CheckExit.Finish(ok ? 0 : 1);
        }

        /// <summary>The zones' blocks re-priced at the proxy - what EN-3 cleared them at - for the load-weighted comparison.</summary>
        private static EnergyMarket.BlockResult[][] ProxyZones(EnergyMarket.Result se)
        {
            var zones = new EnergyMarket.BlockResult[se.Zones.Length][];
            for (int z = 0; z < se.Zones.Length; z++) { zones[z] = new EnergyMarket.BlockResult[3]; for (int b = 0; b < 3; b++) { zones[z][b] = new EnergyMarket.BlockResult { Zone = se.ZoneNames[z], Block = b, Price = se.WaterValue[b] }; } }
            return zones;
        }

        private sealed class Outcome
        {
            public readonly double[][][] ZonePrice = new double[Years + 1][][];   // [year][zone][block]
            public readonly double[][] Proxy = new double[Years + 1][];
            public readonly double[] Balance = new double[Years + 1];
        }

        private static Outcome RunWorld(int years, CountryId? stepped, float etsStepPerT, double inflowScale)
        {
            SimulationRandom.Seed(777);
            EnergyMarket.ResetCalibration(); EnergyMarket.ResetTurnState();
            World world = WorldFactory.CreateDefault();   // resets the probes - both are set after it
            EnergyMarket.ProbeInflowScale = inflowScale;
            if (stepped.HasValue) { EnergyMarket.ProbeEtsRisePerT = etsStepPerT; EnergyMarket.ProbeEtsRiseOnly = stepped; }   // EN-4d: the fleet's carbon price is the ETS; the probe steps one fleet from the top of year 1
            var go = new GameObject("RESERVOIR");
            try
            {
                SimulationManager sim = go.AddComponent<SimulationManager>();
                sim.SetWorld(world);
                sim.PlayerCountryId = stepped ?? CountryId.Sweden;
                var decisions = new Dictionary<CountryId, PolicyDecision>();
                foreach (Country k in world.Countries) { decisions[k.Id] = PolicyDecision.None(); }
                var outcome = new Outcome();
                Country se = world.GetCountry(CountryId.Sweden);
                for (int year = 1; year <= years; year++)
                {
                    for (int day = 0; day < SimulationManager.DaysPerTurn; day++) { sim.AdvanceDay(); }
                    sim.AdvanceTurn(decisions);
                    EnergyMarket.Result r = EnergyMarket.Clear(se);   // at the turn's water value and deficit share
                    outcome.ZonePrice[year] = new double[4][]; for (int z = 0; z < 4; z++) { outcome.ZonePrice[year][z] = new[] { r.Zones[z][0].Price, r.Zones[z][1].Price, r.Zones[z][2].Price }; }
                    outcome.Proxy[year] = new[] { r.WaterValue[0], r.WaterValue[1], r.WaterValue[2] };
                    outcome.Balance[year] = se.State.HydroReservoirBalanceGwh;
                }
                return outcome;
            }
            finally { UnityEngine.Object.DestroyImmediate(go); EnergyMarket.ProbeInflowScale = 1.0; EnergyMarket.ProbeEtsRisePerT = 0.0; EnergyMarket.ProbeEtsRiseOnly = null; }
        }

        private static string F(string format, params object[] args) => string.Format(CultureInfo.InvariantCulture, format, args);
    }
}
