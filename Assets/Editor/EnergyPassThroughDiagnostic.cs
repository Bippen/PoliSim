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
    /// nominal ceiling's erosion of a scarcity term (none on the seed fleets; the adders carry the level since EN-5). (3) THE MECHANISM: Poland's fleet's ETS price stepped fifty dollars per tonne (a probe on that fleet alone -
    /// since EN-4d, §467, the national carbon tax does not reach ETS-covered plant) - the year of the step the planned term equals the weight times
    /// the household price's real change, the inflation print is higher than untouched by about that term, and expectations look through it
    /// (their difference a fraction of the term). (4) THE RIKSBANK'S PATH: Germany's fleet's ETS step reaches Sweden's household price through
    /// the water value and prints on Sweden's inflation through Sweden's own weight - what a German carbon year does to a Swedish print, named.
    /// (5) B6: a doubled price level with the same real stack passes nothing but the ceiling's erosion - and with Poland's level doubled twice (P-B, §552) Sweden's still nothing. (6) EN-7a, THE PREVIEW READS THE TURN: France
    /// (the player, so no AI ministry moves its lines) with its Energy subsidy at 80 standing into a boundary, against untouched - the preview's planned
    /// term moves by what the boundary's plan moves (the levy the subsidy displaces), within a tenth; read before the clone's spending resolved, it moved by nothing.
    /// (7) EN-7b, THE PREVIEW READS THE LAW: Germany (the player) with the household electricity relief in force - the preview's planned budget flow
    /// equals the boundary's within a hundredth, the clone carrying the statute and its base (a clone missing either previews the whole statute or a repeal).
    /// </summary>
    public static class EnergyPassThroughDiagnostic
    {
        private const int Years = 10;
        private const float RaiseUsdPerTonne = 50f;
        /// <summary>CONVENTION - the bound on the planned term at no policy, inflation points a year. ONE thing moves a relative price when nobody moves anything: ACER's nominal
        /// ceiling erodes a scarcity term (none stands on the seed fleets). Until FT-10 · P-B (§552) there was a second - Sweden's water value read Germany's and Poland's
        /// clearing at THEIR price levels, so their inflation against Sweden's moved Sweden's real wholesale (0.0204 pp in year 1), which this comment called *the mechanism
        /// working* and §541 found to be the B6 class across books; the bound stood at 0.05 to admit it. With the water value in the seed's prices all six read 0.0000 in
        /// year 1 and at a doubled price level - exact zeros, the float's noise standing near a millionth of a point - and the bound is a fiftieth of what it was: the tree just before
        /// P-B read 0.0080 pp on Sweden's year 1 (`traj_p6pa`: a real change of 0.113 % on a weight of 70.5 per mille; the 0.0204 above is §465's world), so 0.001 stands eight
        /// times under the defect it excludes and a thousand times over the noise.</summary>
        private const float NoPolicyBoundPp = 0.001f;

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
            sb.Append(F("\n    2. NOTHING PASSES AT NO PLAYER POLICY: the first year's planned term for six (bound {0:F3} pp - the ceiling's erosion is all that moves a relative price when nothing else does); the ten-year largest term printed with the levy scale that made it\n", NoPolicyBoundPp));
            Outcome untouched = RunWorld(CountryId.Poland, Years, null, 0f);
            foreach (CountryId id in untouched.Countries)
            {
                float first = untouched.Term[id][1];
                if (Math.Abs(first) > NoPolicyBoundPp) { ok = false; Debug.LogError($"ENERGY PASS-THROUGH: {id} plans {first:F4} pp in its first year with no policy at all - the stack is not indexing with the level it is measured against."); }
                float worst = 0f; int worstYear = 0;
                for (int y = 1; y <= Years; y++) { float t = untouched.Term[id][y]; if (Math.Abs(t) > Math.Abs(worst)) { worst = t; worstYear = y; } }
                // every term is the weight times the real change - the identity the boundary plans by
                for (int y = 1; y <= Years; y++) { if (Math.Abs(untouched.Term[id][y] - EnergyPassThrough.WeightFraction(id) * untouched.RealChange[id][y]) > 1e-6f) { ok = false; Debug.LogError($"ENERGY PASS-THROUGH: {id}'s year-{y} term is not the weight times the real change."); } }
                sb.Append(F("    {0,-8} first year {1:+0.0000;-0.0000} pp; largest over ten years {2:+0.0000;-0.0000} pp in year {3} with the levy at {4:F3} of its path (the AI ministry's own line moves reach the bill at the line's support share - France's alone since P-A′; the mechanism, not noise); real household price {5:F4} → {6:F4} $/kWh\n", id, first, worst, worstYear, untouched.LevyScale[id][Math.Max(1, worstYear)], untouched.RealPrice[id][1], untouched.RealPrice[id][Years]));
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
            sb.Append("\n    5. B6: a doubled price level with the same real stack passes nothing but the ceiling's erosion (every cost carries the level; ACER's 4 000 is a nominal legal figure, so a scarcity term's real value falls) - and Sweden's water value, with Poland's level doubled twice, reads each neighbour's price over its own level (P-B)\n");
            {
                SimulationRandom.Seed(777); EnergyMarket.ResetCalibration();
                World w = WorldFactory.CreateDefault(); EnergyMarket.BeginTurn(w);
                var at1 = new Dictionary<CountryId, double>();
                foreach (Country c in w.Countries) { if (EnergyLayer.Has(c.Id)) { at1[c.Id] = EnergyLedger.Compute(c, EnergyMarket.ClearAt(c.Id, 1.0, 0.0), 1.0, 0.0).Classes[0].Total; } }
                // every country's level is doubled and POLAND'S DOUBLED TWICE, then the turn begun as a boundary would (FT-10 · P-B, §552). A level doubled everywhere cancels in the
                // water value's old form too, so it told nothing apart; with Poland's at 4 the old form read a continental price near three times the seed's, and the new one - each
                // neighbour's clearing over ITS OWN level - reads the seed's. Each country's own book below is cleared at the explicit 2, Poland's too; powers of two keep it exact.
                foreach (Country c in w.Countries) { c.State.PriceLevel = c.Id == CountryId.Poland ? 4f : 2f; }
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

            // (6) EN-7a: the preview's term against the boundary's, for a standing Energy subsidy
            sb.Append("\n    6. THE PREVIEW READS THE TURN: France's Energy subsidy at 80 standing into year 2's boundary, against untouched - the preview's planned term and the boundary's\n");
            {
                (float Preview, float Turn, float Levy) at50 = PreviewAgainstTurn(50f), at80 = PreviewAgainstTurn(80f);
                float dPreview = at80.Preview - at50.Preview, dTurn = at80.Turn - at50.Turn;
                bool moved = dTurn < 0f;
                bool agrees = Math.Abs(dPreview - dTurn) <= 0.1f * Math.Abs(dTurn) + 1e-5f;
                if (!moved) { ok = false; Debug.LogError($"ENERGY PASS-THROUGH: France's Energy subsidy at 80 did not lower the boundary's planned term ({at50.Turn:F5} → {at80.Turn:F5} pp) - the parity case tests nothing."); }
                else if (!agrees) { ok = false; Debug.LogError($"ENERGY PASS-THROUGH: the preview's term moved {dPreview:F5} pp under France's Energy subsidy at 80, the boundary's {dTurn:F5} - the preview does not read the subsidy's route."); }
                sb.Append(F("    France   untouched: preview {0:+0.00000;-0.00000} pp, boundary {1:+0.00000;-0.00000}; subsidy 80: preview {2:+0.00000;-0.00000}, boundary {3:+0.00000;-0.00000} (the levy scale {4:F3} → {5:F3}) - the preview moved {6:+0.00000;-0.00000}, the boundary {7:+0.00000;-0.00000}\n",
                    at50.Preview, at50.Turn, at80.Preview, at80.Turn, at50.Levy, at80.Levy, dPreview, dTurn));
            }

            // (7) EN-7b: the preview's electricity-tax flow against the boundary's, a law in force
            sb.Append("\n    7. THE PREVIEW READS THE LAW: Germany's household electricity relief in force into year 2's boundary - the preview's planned flow and the boundary's\n");
            {
                (float Preview, float Turn, float RevenueMove, float Multiplier) law = PreviewTaxFlowAgainstTurn();
                bool agrees = law.Turn < 0f && Math.Abs(law.Preview - law.Turn) <= 0.01f * Math.Abs(law.Turn) + 1e-6f;
                if (!agrees) { ok = false; Debug.LogError($"ENERGY PASS-THROUGH: the preview planned {law.Preview:F5} bn of electricity-tax flow, the boundary {law.Turn:F5} - the preview does not read the law (the clone's statute or its base)."); }
                // the preview's revenue estimate carries the flow: the same preview with the statute at its base, the difference against the flow in the fiscal-reaction multiplier
                float wantMove = law.Preview * law.Multiplier;
                float tolerance = 0.002f * Math.Abs(wantMove) + 1e-4f;
                bool separable = Math.Abs(law.Multiplier - 1f) * Math.Abs(law.Preview) > 4f * tolerance;   // a flow booked outside the multiplier must miss by more than the tolerance
                if (!separable) { ok = false; Debug.LogError($"ENERGY PASS-THROUGH: Germany's multiplier {law.Multiplier:F4} is too near 1 for the preview's revenue move to tell a flow inside it from one outside - the check would be vacuous; move the fixture."); }
                bool reaches = Math.Abs(law.RevenueMove - wantMove) <= tolerance && Math.Abs(wantMove) > 0.1f;
                if (!reaches) { ok = false; Debug.LogError($"ENERGY PASS-THROUGH: the preview's revenue estimate moved {law.RevenueMove:F5} bn with the law in force against its flow {law.Preview:F5} bn at the multiplier {law.Multiplier:F4} ({wantMove:F5}) - the flow does not reach the preview's budget."); }
                sb.Append(F("    Germany  the household relief in force: the preview planned {0:+0.00000;-0.00000} bn, the boundary {1:+0.00000;-0.00000} bn; the preview's revenue estimate moved {2:+0.00000;-0.00000} bn against the statute at its base (the flow at the multiplier {3:F4}: {4:+0.00000;-0.00000})\n", law.Preview, law.Turn, law.RevenueMove, law.Multiplier, wantMove));
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

        /// <summary>EN-7a: France as the player, one year advanced, its Energy subsidy set to <paramref name="subsidy"/> as a passed bill leaves it, a second year of
        /// days - then the preview's planned term, the boundary, and the boundary's own plan (the term it wrote) with the levy scale it wrote it at.</summary>
        private static (float Preview, float Turn, float Levy) PreviewAgainstTurn(float subsidy)
        {
            SimulationRandom.Seed(777);
            EnergyMarket.ResetCalibration();
            World world = WorldFactory.CreateDefault();
            var go = new GameObject("ENERGYPREVIEWPARITY");
            try
            {
                SimulationManager sim = go.AddComponent<SimulationManager>();
                sim.SetWorld(world);
                sim.PlayerCountryId = CountryId.France;
                var decisions = new Dictionary<CountryId, PolicyDecision>();
                foreach (Country k in world.Countries) { decisions[k.Id] = PolicyDecision.None(); }
                for (int day = 0; day < SimulationManager.DaysPerTurn; day++) { sim.AdvanceDay(); }
                sim.AdvanceTurn(decisions);
                Country fr = world.GetCountry(CountryId.France);
                foreach (Sector s in fr.Sectors) { if (s.Type == SectorType.Energy) { s.SubsidyLevel = subsidy; } }
                for (int day = 0; day < SimulationManager.DaysPerTurn; day++) { sim.AdvanceDay(); }
                float preview = sim.PreviewTurn(CountryId.France, PolicyDecision.None()).PreviewEnergyPassThroughPp;
                sim.AdvanceTurn(decisions);
                float levy = (float)EnergyLedger.Compute(fr, EnergyMarket.Clear(fr), Math.Max(0.0001f, fr.State.PriceLevel), 0.0).LevyScale;
                return (preview, EnergyPassThrough.Planned(fr), levy);
            }
            finally { UnityEngine.Object.DestroyImmediate(go); }
        }

        /// <summary>EN-7b: Germany as the player, the household electricity relief enacted in year 1, a second year of days - then the preview's planned electricity-tax flow and,
        /// after the boundary, the flow the boundary planned (read from the period by reflection); and the preview's revenue estimate with the law against the same preview with the
        /// household statute put back at its base (restored bit for bit before the boundary), with the real country's fiscal-reaction multiplier the move is read against.</summary>
        private static (float Preview, float Turn, float RevenueMove, float Multiplier) PreviewTaxFlowAgainstTurn()
        {
            SimulationRandom.Seed(777);
            EnergyMarket.ResetCalibration();
            World world = WorldFactory.CreateDefault();
            var go = new GameObject("ENERGYTAXPREVIEW");
            try
            {
                SimulationManager sim = go.AddComponent<SimulationManager>();
                sim.SetWorld(world);
                sim.PlayerCountryId = CountryId.Germany;
                const System.Reflection.BindingFlags instance = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
                var decisions = new Dictionary<CountryId, PolicyDecision>();
                foreach (Country k in world.Countries) { decisions[k.Id] = PolicyDecision.None(); }
                Country de = world.GetCountry(CountryId.Germany);
                for (int day = 0; day < SimulationManager.DaysPerTurn; day++) { sim.AdvanceDay(); }
                typeof(SimulationManager).GetMethod("ApplyLawBillEffects", instance).Invoke(sim, new object[] { de, new LawBill { LawId = "household_electricity_tax_relief_act", IsRepeal = false } });
                de.EnactedLaws.Add(new EnactedLaw { LawId = "household_electricity_tax_relief_act", EnactedOn = sim.CurrentDate });
                sim.AdvanceTurn(decisions);
                for (int day = 0; day < SimulationManager.DaysPerTurn; day++) { sim.AdvanceDay(); }
                PolicyPreview withLaw = sim.PreviewTurn(CountryId.Germany, PolicyDecision.None());
                float statuteWithLaw = de.ElectricityTaxHouseholds;
                de.ElectricityTaxHouseholds = de.ElectricityTaxHouseholdsBase;
                PolicyPreview atBase = sim.PreviewTurn(CountryId.Germany, PolicyDecision.None());
                de.ElectricityTaxHouseholds = statuteWithLaw;
                float multiplier = (float)typeof(SimulationManager).GetMethod("GetFiscalReactionMultiplier", instance).Invoke(sim, new object[] { de });
                sim.AdvanceTurn(decisions);
                var periods = (System.Collections.IDictionary)typeof(SimulationManager).GetField("_fiscalPeriods", instance).GetValue(sim);
                return (withLaw.PreviewElectricityTaxRevenue, ((SimulationManager.FiscalPeriod)periods[CountryId.Germany]).PlannedElectricityTaxRevenue, withLaw.RevenueEstimate - atBase.RevenueEstimate, multiplier);
            }
            finally { UnityEngine.Object.DestroyImmediate(go); }
        }

        private static string F(string format, params object[] args) => string.Format(CultureInfo.InvariantCulture, format, args);
    }
}
