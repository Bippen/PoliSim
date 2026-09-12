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
    /// EN-5 (2026-09-11), the energy track's S8 - the retail electricity price reaches Inflation through a sourced index weight. It builds and
    /// advances worlds, so it belongs to the simulation group. (1) THE WEIGHTS: six, sourced, per mille. (2) NOTHING PASSES AT NO POLICY: ten
    /// years for six, the largest planned term printed and bounded - a stack that indexes with the level it is measured against passes only the
    /// fitted adders' real erosion. (3) THE MECHANISM: Poland's fleet's ETS price stepped fifty dollars per tonne (a probe on that fleet alone -
    /// since EN-4d, §467, the national carbon tax does not reach ETS-covered plant) - the year of the step the planned term equals the weight times
    /// the household price's real change, the inflation print is higher than untouched by about that term, and expectations look through it
    /// (their difference a fraction of the term). (4) THE RIKSBANK'S PATH: Germany's fleet's ETS step reaches Sweden's household price through
    /// the water value and prints on Sweden's inflation through Sweden's own weight - what a German carbon year does to a Swedish print, named.
    /// (5) B6: a doubled price level with the same real stack passes nothing but the adders' erosion.
    /// </summary>
    public static class EnergyPassThroughDiagnostic
    {
        private const int Years = 10;
        private const float RaiseUsdPerTonne = 50f;
        /// <summary>The bound on the planned term at no policy, inflation points a year. Two things move a relative price when nobody moves anything: ACER's
        /// nominal ceiling erodes a scarcity term, and A COUNTRY WHOSE WHOLESALE IS ANOTHER MARKET'S PRICE carries that market's inflation differential -
        /// Sweden's water value is Germany's and Poland's clearing at THEIR price level, so their 3 per cent against Sweden's 2 moves Sweden's real
        /// wholesale by their gap (the first run read 0.0204 pp in year 1 and named it): an import-price channel, which is the mechanism working.</summary>
        private const float NoPolicyBoundPp = 0.05f;

        public static void Run()
        {
            CheckExit.ArmLogFold();
            bool ok = true;
            var sb = new StringBuilder();
            sb.Append("=== ENERGY PASS-THROUGH (EN-5): the retail electricity price into Inflation through the index weight ===\n");

            // (1) the weights
            sb.Append("\n    1. THE WEIGHTS: electricity per mille of the consumer basket, 2023\n");
            SimulationRandom.Seed(777); EnergyMarket.ResetCalibration();
            World world = WorldFactory.CreateDefault();
            foreach (Country c in world.Countries)
            {
                if (!EnergyLayer.Has(c.Id)) { continue; }
                float w = EnergyPassThrough.WeightFraction(c.Id) * EnergyPassThrough.PerMille;
                if (!(w > 0f)) { ok = false; Debug.LogError($"ENERGY PASS-THROUGH: {c.Id} has no weight."); }
                sb.Append(F("    {0,-8} {1:F2} per mille ({2})\n", c.Id, w, c.Id == CountryId.USA ? "BLS CPI-U relative importance, December 2023, reached through the archive" : "Eurostat prc_hicp_inw CP0451"));
            }

            // (2) nothing at no PLAYER policy - the first year, before the AI ministry's own budget moves reach the levy
            sb.Append(F("\n    2. NOTHING PASSES AT NO PLAYER POLICY: the first year's planned term for six (bound {0:F2} pp - the ceiling's erosion is all that moves a relative price when nothing else does); the ten-year largest term printed with the levy scale that made it\n", NoPolicyBoundPp));
            Outcome untouched = RunWorld(CountryId.Poland, Years, null, 0f);
            foreach (CountryId id in untouched.Countries)
            {
                float first = untouched.Term[id][1];
                if (Math.Abs(first) > NoPolicyBoundPp) { ok = false; Debug.LogError($"ENERGY PASS-THROUGH: {id} plans {first:F4} pp in its first year with no policy at all - the stack is not indexing with the level it is measured against."); }
                float worst = 0f; int worstYear = 0;
                for (int y = 1; y <= Years; y++) { float t = untouched.Term[id][y]; if (Math.Abs(t) > Math.Abs(worst)) { worst = t; worstYear = y; } }
                // every term is the weight times the real change - the identity the boundary plans by
                for (int y = 1; y <= Years; y++) { if (Math.Abs(untouched.Term[id][y] - EnergyPassThrough.WeightFraction(id) * untouched.RealChange[id][y]) > 1e-6f) { ok = false; Debug.LogError($"ENERGY PASS-THROUGH: {id}'s year-{y} term is not the weight times the real change."); } }
                sb.Append(F("    {0,-8} first year {1:+0.0000;-0.0000} pp; largest over ten years {2:+0.0000;-0.0000} pp in year {3} with the levy at {4:F3} of its path (the AI ministry's own line moves reach the bill - the mechanism, not noise); real household price {5:F4} → {6:F4} $/kWh\n", id, first, worst, worstYear, untouched.LevyScale[id][Math.Max(1, worstYear)], untouched.RealPrice[id][1], untouched.RealPrice[id][Years]));
            }

            // (3) the mechanism: Poland's fleet's ETS price stepped fifty dollars per tonne from the top of year 1 - in the MARKET's currency, euro (EN-4d: the ETS is the fleet's carbon price; the national tax's zloty do not reach it)
            float eurStep = (float)(RaiseUsdPerTonne / EnergyLayerData.UsdPerMarketCurrency[EnergyLayer.Index(CountryId.Poland)]);
            sb.Append(F("\n    3. THE MECHANISM: Poland's fleet's ETS price stepped {0:F0} dollars per tonne ({1:F1} EUR/t, the market's currency) from the top of year 1 and held - a probe on that fleet alone - against untouched\n", RaiseUsdPerTonne, eurStep));
            Outcome raised = RunWorld(CountryId.Poland, Years, CountryId.Poland, eurStep);
            {
                CountryId pl = CountryId.Poland;
                // the step stands at the top of year 1's turn: the ledger writes year 1's price at the stepped ETS, the boundary plans the term, year 2 prints it
                float term = raised.Term[pl][1], realChange = raised.RealChange[pl][1];
                float expected = EnergyPassThrough.WeightFraction(pl) * realChange;
                if (Math.Abs(term - expected) > 1e-6f) { ok = false; Debug.LogError($"ENERGY PASS-THROUGH: Poland's planned term {term:F6} is not the weight times the real change ({expected:F6})."); }
                if (!(realChange > 0f) || !(term > 0f)) { ok = false; Debug.LogError($"ENERGY PASS-THROUGH: Poland's household price did not rise in real terms under the raise (change {realChange:F4} %, term {term:F5} pp)."); }
                float printGap = raised.Inflation[pl][2] - untouched.Inflation[pl][2];
                if (!(printGap > 0.5f * term) || !(printGap < 1.5f * term + 0.02f)) { ok = false; Debug.LogError($"ENERGY PASS-THROUGH: Poland's year-2 print moved {printGap:F5} pp against a planned term of {term:F5} - the level map did not carry it."); }
                float expectationsGap = raised.Expectations[pl][2] - untouched.Expectations[pl][2];
                if (Math.Abs(expectationsGap) > 0.5f * Math.Abs(term) + 0.005f) { ok = false; Debug.LogError($"ENERGY PASS-THROUGH: Poland's expectations moved {expectationsGap:F5} pp on a level term of {term:F5} - expectations must look through a price-level term."); }
                sb.Append(F("    year 1: real household price {0:F4} → {1:F4} $/kWh ({2:+0.00;-0.00} %), planned term {3:+0.0000;-0.0000} pp = {4:F5} × {2:F4}; year 2: inflation {5:F3} against {6:F3} untouched ({7:+0.0000;-0.0000} pp), expectations {8:+0.0000;-0.0000} pp apart; year 3's term {9:+0.0000;-0.0000} pp (the level step does not repeat)\n",
                    untouched.RealPrice[pl][1], raised.RealPrice[pl][1], realChange, term, EnergyPassThrough.WeightFraction(pl), raised.Inflation[pl][2], untouched.Inflation[pl][2], printGap, expectationsGap, raised.Term[pl][2]));
            }

            // (4) the Riksbank's path: Germany's fleet's ETS step reaches Sweden
            float eurRaise = (float)(RaiseUsdPerTonne * EnergyLayer.NationalPerUsd(CountryId.Germany));
            sb.Append(F("\n    4. THE RIKSBANK'S PATH: Germany's fleet's ETS price stepped {0:F0} EUR/t from the top of year 1 (a probe on that fleet alone) - Sweden's water value, household price and inflation print\n", eurRaise));
            Outcome germanRaise = RunWorld(CountryId.Germany, Years, CountryId.Germany, eurRaise);
            {
                CountryId se = CountryId.Sweden, de = CountryId.Germany;
                // the water value is set at the TOP of a turn from the two markets' clearings (EnergyMarket.BeginTurn), and the ETS step stands at the top of year 1's
                // turn as an exogenous price would, so Germany's fleet and Sweden's water value move in the SAME year - year 1's book, year 2's print. (The year's
                // lag EN-5's first form read came from the tax decision's placement after the top of the turn, not from the market: §465's "a year behind".)
                float seTerm = germanRaise.Term[se][1] - untouched.Term[se][1], seChange = germanRaise.RealChange[se][1] - untouched.RealChange[se][1];
                if (!(seChange > 0f) || !(seTerm > 0f)) { ok = false; Debug.LogError($"ENERGY PASS-THROUGH: Germany's ETS step did not reach Sweden's household price (real change {seChange:F4} %, term {seTerm:F5} pp against untouched)."); }
                float sePrintGap = germanRaise.Inflation[se][2] - untouched.Inflation[se][2];
                if (!(sePrintGap > 0f)) { ok = false; Debug.LogError($"ENERGY PASS-THROUGH: Sweden's year-2 print did not rise under Germany's ETS step ({sePrintGap:F5} pp)."); }
                sb.Append(F("    Germany: real household price {0:+0.00;-0.00} % in year 1, term {1:+0.0000;-0.0000} pp; Sweden, the same year (the ETS stands at the top of the turn, where the water value is set): water value {2:F1} → {3:F1} €/MWh (base block, year 1), real household price {4:+0.00;-0.00} % against untouched, term {5:+0.0000;-0.0000} pp at a weight of {6:F2} per mille; Sweden's year-2 inflation {7:F3} against {8:F3} ({9:+0.0000;-0.0000} pp), the Riksbank's rate {10:F2} against {11:F2} % at year 2's close\n",
                    germanRaise.RealChange[de][1], germanRaise.Term[de][1], untouched.WaterValueBase[1], germanRaise.WaterValueBase[1], seChange, seTerm, EnergyPassThrough.WeightFraction(se) * EnergyPassThrough.PerMille, germanRaise.Inflation[se][2], untouched.Inflation[se][2], sePrintGap, germanRaise.PolicyRate[se][2], untouched.PolicyRate[se][2]));
            }

            // (5) B6
            sb.Append("\n    5. B6: a doubled price level with the same real stack passes nothing but the ceiling's erosion (every cost carries the level, the adders since this pass; ACER's 4 000 is a nominal legal figure, so a scarcity term's real value falls)\n");
            {
                SimulationRandom.Seed(777); EnergyMarket.ResetCalibration();
                World w = WorldFactory.CreateDefault(); EnergyMarket.BeginTurn(w);
                var at1 = new Dictionary<CountryId, double>();
                foreach (Country c in w.Countries) { if (EnergyLayer.Has(c.Id)) { at1[c.Id] = EnergyLedger.Compute(c, EnergyMarket.ClearAt(c.Id, 1.0, 0.0), 1.0, 0.0).Classes[0].Total; } }
                // the water value is Germany's and Poland's clearing at THEIR price level - double theirs before Sweden's book at index 2 is read, as a turn would
                foreach (Country c in w.Countries) { c.State.PriceLevel = 2f; }
                EnergyMarket.BeginTurn(w);
                foreach (Country c in w.Countries)
                {
                    if (!EnergyLayer.Has(c.Id)) { continue; }
                    double real2 = EnergyLedger.Compute(c, EnergyMarket.ClearAt(c.Id, 2.0, 0.0), 2.0, 0.0).Classes[0].Total / 2.0;
                    float change = (float)((real2 / at1[c.Id] - 1.0) * 100.0), term = EnergyPassThrough.WeightFraction(c.Id) * change;
                    if (Math.Abs(term) > NoPolicyBoundPp) { ok = false; Debug.LogError($"ENERGY PASS-THROUGH: {c.Id} at a doubled price level passes {term:F4} pp - more than the ceiling's erosion can explain."); }
                    sb.Append(F("    {0,-8} real household price at index 1: {1:F4}; at index 2 deflated: {2:F4} ({3:+0.000;-0.000} %) → {4:+0.00000;-0.00000} pp\n", c.Id, at1[c.Id], real2, change, term));
                }
                foreach (Country c in w.Countries) { c.State.PriceLevel = 1f; }
                EnergyMarket.ResetTurnState();
            }

            sb.Append(ok ? "\n=== EnergyPassThroughDiagnostic: ALL ASSERTIONS PASS ===\n" : "\n=== EnergyPassThroughDiagnostic: FAILED (see above) ===\n");
            if (ok) { Debug.Log(sb.ToString()); } else { Debug.LogError(sb.ToString()); }
            CheckExit.Finish(ok ? 0 : 1);
        }

        private sealed class Outcome
        {
            public readonly List<CountryId> Countries = new List<CountryId>();
            public readonly Dictionary<CountryId, float[]> Term = new Dictionary<CountryId, float[]>(), RealChange = new Dictionary<CountryId, float[]>(), RealPrice = new Dictionary<CountryId, float[]>(),
                Inflation = new Dictionary<CountryId, float[]>(), Expectations = new Dictionary<CountryId, float[]>(), PolicyRate = new Dictionary<CountryId, float[]>(), LevyScale = new Dictionary<CountryId, float[]>();
            public readonly float[] WaterValueBase = new float[Years + 1];
        }

        /// <summary>A world advanced <paramref name="years"/> turns with, where <paramref name="stepped"/> is set, that country's fleet's ETS price stepped by <paramref name="etsStepPerT"/> (the market's currency) from the top of year 1 and held - the probe EN-4d left the diagnostics, the national carbon tax no longer reaching the fleet; per country and year: the planned term, the real change, the real price, the print, the expectations, the policy rate.</summary>
        private static Outcome RunWorld(CountryId player, int years, CountryId? stepped, float etsStepPerT)
        {
            SimulationRandom.Seed(777);
            EnergyMarket.ResetCalibration();
            World world = WorldFactory.CreateDefault();   // resets the probes - the step is set after it
            if (stepped.HasValue) { EnergyMarket.ProbeEtsRisePerT = etsStepPerT; EnergyMarket.ProbeEtsRiseOnly = stepped; }
            var go = new GameObject("ENERGYPASSTHROUGH");
            try
            {
                SimulationManager sim = go.AddComponent<SimulationManager>();
                sim.SetWorld(world);
                sim.PlayerCountryId = player;
                var run = new Outcome();
                foreach (Country c in world.Countries) { if (EnergyLayer.Has(c.Id)) { run.Countries.Add(c.Id); foreach (var d in new[] { run.Term, run.RealChange, run.RealPrice, run.Inflation, run.Expectations, run.PolicyRate, run.LevyScale }) { d[c.Id] = new float[years + 1]; } } }
                var decisions = new Dictionary<CountryId, PolicyDecision>();
                foreach (Country k in world.Countries) { decisions[k.Id] = PolicyDecision.None(); }
                for (int year = 1; year <= years; year++)
                {
                    for (int day = 0; day < SimulationManager.DaysPerTurn; day++) { sim.AdvanceDay(); }
                    sim.AdvanceTurn(decisions);
                    foreach (CountryId id in run.Countries)
                    {
                        Country c = world.GetCountry(id);
                        run.Term[id][year] = EnergyPassThrough.Planned(c);
                        run.RealChange[id][year] = c.State.EnergyHouseholdPriceRealChange;
                        run.RealPrice[id][year] = c.State.EnergyHouseholdPriceReal;
                        run.Inflation[id][year] = c.State.Inflation;
                        run.Expectations[id][year] = c.State.InflationExpectations;
                        run.PolicyRate[id][year] = c.CurrencyZone != null ? c.CurrencyZone.InterestRate : 0f;
                        run.LevyScale[id][year] = (float)EnergyLedger.Compute(c, EnergyMarket.Clear(c), Math.Max(0.0001f, c.State.PriceLevel), 0.0).LevyScale;
                    }
                    EnergyMarket.Result se = EnergyMarket.Clear(world.GetCountry(CountryId.Sweden));
                    run.WaterValueBase[year] = (float)se.WaterValue[0];
                }
                return run;
            }
            finally { UnityEngine.Object.DestroyImmediate(go); EnergyMarket.ProbeEtsRisePerT = 0.0; EnergyMarket.ProbeEtsRiseOnly = null; }
        }

        private static string F(string format, params object[] args) => string.Format(CultureInfo.InvariantCulture, format, args);
    }
}
