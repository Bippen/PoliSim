using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using PoliSim.Data;
using PoliSim.Simulation;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// EN-4c (2026-09-11), the carbon tax's unit - ruled: the rate is the country's currency per tonne of CO₂, the statutory meaning; revenue is
    /// rate × the taxed tonnes; one stored rate, one presented rate, one meaning. It builds and advances worlds, so it belongs to the simulation
    /// group. (1) THE SEEDS: Sweden 1 330 SEK, Germany 30 EUR, France 44.6 EUR per tonne, implemented; Italy, Poland and the USA 0, unimplemented;
    /// each line's dial ceiling is the 300-dollar bound in its own currency. (2) REVENUE = RATE × TAXED TONNES for the three, in the book's dollars
    /// through the ECB rate, and the Budget's estimate is the same accessor - TRANSPORT's tonnes since EN-4d (ruled 2026-09-11, §467): the
    /// ETS-covered power fleet is exempt of the national carbon tax by statute, applied at sector level for want of an installation register. (3) THE ANCHOR HELD: the coverage bridge was re-solved so Implied × CE
    /// - D-16's anchored revenue-to-GDP - stands to the second decimal for the five. (4) ONE MEANING: the seed's references (the C-N4 baseline, the
    /// family's seed rate) are the line's own figure; the dispatch does not read the tax (EN-4d) - at the seed it is the seed dispatch and a raise
    /// leaves it where it stands; a full dial is one hundred political
    /// points. (5) THE MECHANISM: Sweden raised fifty dollars per tonne through the decision, ten years, against untouched - the carbon revenue
    /// higher by the rate's ratio less the base's erosion, the transport intensity lower by the elasticity's chain, the approval term charged the
    /// dial's per cent. (6) B6: a rate per tonne does not read the price level - the revenue in dollars at price index 2 is the revenue at 1.
    /// (7) THE STATUTES (EN-4e, ruled 2026-09-12, §471): at no policy for ten years Sweden's rate is the seed times the product of the years'
    /// price ratios rounded to four decimals (2 kap. 1 b §), its real rate the seed's and the transport coupling's real term nil; Germany's rate
    /// follows the BEHG's schedule (45, 55, 55) and then carries the level; France's 44.6 stands nominal, its real rate falls and the coupling's
    /// tax factor rises above one; every ceiling carries the level; Poland's unimplemented line stays 0. The raise in (5) is applied once, in
    /// year 1, and indexes thereafter as the law indexes it - the factor is read in REAL dollars.
    /// </summary>
    public static class CarbonTaxUnitDiagnostic
    {
        private const int Years = 10;
        private const float RaiseUsdPerTonne = 50f;

        private static readonly (CountryId Id, float Rate, bool Implemented)[] Seeds =
        {
            (CountryId.Sweden, 1330f, true), (CountryId.Germany, 30f, true), (CountryId.France, 44.6f, true),
            (CountryId.Italy, 0f, false), (CountryId.Poland, 0f, false), (CountryId.USA, 0f, false),
        };

        /// <summary>D-16's anchored revenue-to-GDP (Implied × CE, %), WorldFactory's own comment table.</summary>
        private static readonly (CountryId Id, float Anchor)[] Anchors = { (CountryId.Sweden, 42.04f), (CountryId.Germany, 40.81f), (CountryId.France, 45.22f), (CountryId.Italy, 42.49f), (CountryId.Poland, 37.51f) };

        public static void Run()
        {
            CheckExit.ArmLogFold();
            bool ok = true;
            var sb = new StringBuilder();
            sb.Append("=== CARBON TAX UNIT (EN-4c): the country's currency per tonne of CO₂ - the statutory seeds, revenue = rate × taxed tonnes, the anchor held, one meaning ===\n");

            SimulationRandom.Seed(777);
            EnergyMarket.ResetCalibration();
            World world = WorldFactory.CreateDefault();
            EnergyMarket.BeginTurn(world);

            // (1) the seeds
            sb.Append("\n    1. THE SEEDS: the statutory rate per tonne, the line's own currency, the dial's ceiling\n");
            foreach ((CountryId id, float rate, bool implemented) in Seeds)
            {
                Country c = world.GetCountry(id);
                TaxLine line = Find(c);
                if (line == null) { ok = false; Debug.LogError($"CARBON TAX UNIT: {id} has no carbon tax line."); continue; }
                if (Mathf.Abs(line.Rate - rate) > 1e-4f || line.IsImplemented != implemented) { ok = false; Debug.LogError($"CARBON TAX UNIT: {id}'s line reads {line.Rate} ({(line.IsImplemented ? "implemented" : "unimplemented")}); the statutory seed is {rate} ({(implemented ? "implemented" : "unimplemented")})."); }
                double natPerUsd = EnergyLayer.NationalPerUsd(id);
                float ceiling = Mathf.Round((float)(TaxTypeRateRanges.CarbonTaxMax * natPerUsd) / 10f) * 10f;
                if (Mathf.Abs(line.MaxRate - ceiling) > 1e-3f) { ok = false; Debug.LogError($"CARBON TAX UNIT: {id}'s dial ceiling is {line.MaxRate}, not {TaxTypeRateRanges.CarbonTaxMax} dollars per tonne in its currency ({ceiling})."); }
                sb.Append(F("    {0,-8} {1,7:F1} {2}/t CO₂ {3} · ceiling {4:F0} {2}/t ({5:F0} USD/t) · {6:F2} {2} per USD\n", id, line.Rate, EnergyLayer.CurrencyCode(id), line.IsImplemented ? "IMPLEMENTED" : "unimplemented, 0 - no carbon tax distinct from the ETS", line.MaxRate, TaxTypeRateRanges.CarbonTaxMax, natPerUsd));
            }

            // (2) revenue = rate × taxed tonnes
            sb.Append("\n    2. REVENUE = RATE × TAXED TONNES (the book's dollars through the ECB rate; the Budget's estimate is the same accessor) - transport's tonnes since EN-4d: the ETS-covered fleet is exempt of the tax\n");
            foreach ((CountryId id, float rate, bool implemented) in Seeds)
            {
                Country c = world.GetCountry(id); TaxLine line = Find(c);
                float tonnesMt = TaxBases.Level(TaxBaseDriver.Emissions, c);
                double expected = rate * tonnesMt / 1000.0 / EnergyLayer.NationalPerUsd(id);
                float revenue = implemented ? TaxBases.Revenue(c, line) : 0f;
                if (implemented && Math.Abs(revenue - expected) > 1e-6 * Math.Max(1.0, expected)) { ok = false; Debug.LogError($"CARBON TAX UNIT: {id}'s carbon revenue is {revenue:F4} bn against rate × tonnes {expected:F4}."); }
                if (!implemented && TaxBases.Revenue(c, line) != 0f) { ok = false; Debug.LogError($"CARBON TAX UNIT: {id}'s unimplemented line at rate 0 yields revenue."); }
                // EN-4d: the taxed tonnes are transport's alone - the power fleet pays the ETS and is exempt of the tax by statute (sector level)
                if (Math.Abs(tonnesMt - c.State.TransportCo2PerCapita * c.State.Population) > 1e-4 * Math.Max(1.0, tonnesMt)) { ok = false; Debug.LogError($"CARBON TAX UNIT: {id}'s taxed tonnes are {tonnesMt:F3} Mt against transport's {c.State.TransportCo2PerCapita * c.State.Population:F3} - the power fleet's tonnes are in the base (EN-4d exempts them)."); }
                sb.Append(F("    {0,-8} taxed CO₂ {1:F2} Mt (transport {3:F2} t/head × {4:F1} M; power's {2:F2} t/head pays the ETS, exempt of the tax - EN-4d) × {5:F1} {6}/t = {7:F3} bn {6} = {8:F3} bn USD = {9:F3} % of GDP\n",
                    id, tonnesMt, c.State.PowerCo2PerCapita, c.State.TransportCo2PerCapita, c.State.Population, rate, EnergyLayer.CurrencyCode(id), rate * tonnesMt / 1000.0, revenue, 100.0 * revenue / Math.Max(1f, c.State.NominalGdp)));
            }

            // (3) the anchor held
            sb.Append("\n    3. THE ANCHOR HELD: Implied × CE (revenue-to-GDP, %) against D-16's table - the bridge re-solved for the three whose carbon line changed (EN-4c), and again under EN-4d for the power share taken out of the base\n");
            foreach ((CountryId id, float anchor) in Anchors)
            {
                Country c = world.GetCountry(id);
                double implied = 0; foreach (TaxLine l in c.TaxLines) { if (l.IsImplemented && l.Type != TaxType.Tariffs) { implied += TaxBases.Revenue(c, l); } }
                double impliedPct = 100.0 * implied / Math.Max(1f, c.State.NominalGdp);
                double anchored = impliedPct * c.CollectionEfficiency;
                if (Math.Abs(anchored - anchor) > 0.011) { ok = false; Debug.LogError($"CARBON TAX UNIT: {id}'s Implied × CE is {anchored:F3} against the anchored {anchor:F2} - the coverage bridge was not re-solved to hold it."); }
                sb.Append(F("    {0,-8} implied {1:F4} % × CE {2:F4} = {3:F3} (anchor {4:F2})\n", id, impliedPct, c.CollectionEfficiency, anchored, anchor));
            }

            // (4) one stored rate, one presented rate, one meaning
            sb.Append("\n    4. ONE MEANING: the seed's references are the line's own figure; the dispatch does not read the tax (EN-4d) - at the seed it is the seed dispatch, and a raise leaves it; a full dial is one hundred political points\n");
            foreach (Country c in world.Countries)
            {
                TaxLine line = Find(c); if (line == null) { continue; }
                float baseline = c.BaselineTaxRates.TryGetValue(TaxType.CarbonTax, out float b) ? b : float.NaN;
                if (Mathf.Abs(baseline - line.Rate) > 1e-6f) { ok = false; Debug.LogError($"CARBON TAX UNIT: {c.Id}'s C-N4 baseline carries {baseline} against the line's {line.Rate}."); }
                float familySeed = c.Environment != null ? c.Environment.CarbonTaxRateSeed : 0f;
                float expectedSeed = line.IsImplemented ? line.Rate : 0f;
                if (Mathf.Abs(familySeed - expectedSeed) > 1e-6f) { ok = false; Debug.LogError($"CARBON TAX UNIT: {c.Id}'s environment seed carries {familySeed} against the line's {expectedSeed}."); }
                if (Mathf.Abs(line.PointsOf(line.MaxRate) - 100f) > 1e-4f) { ok = false; Debug.LogError($"CARBON TAX UNIT: {c.Id}'s full dial is {line.PointsOf(line.MaxRate)} political points, not 100."); }
                if (EnergyLayer.Has(c.Id))
                {
                    EnergyMarket.Result atRate = EnergyMarket.Clear(c), atSeed = EnergyMarket.ClearAtSeed(c.Id);
                    if (Math.Abs(atRate.DerivedCo2Mt - atSeed.DerivedCo2Mt) > 1e-9) { ok = false; Debug.LogError($"CARBON TAX UNIT: {c.Id}'s dispatch this turn is not the seed dispatch."); }
                    // EN-4d: a raise of the line leaves the dispatch where it stands - the fleet's carbon price is the ETS, and the tax reaches transport
                    float held = line.Rate; line.Rate = held + 100f;
                    EnergyMarket.Result atRaised = EnergyMarket.Clear(c);
                    line.Rate = held;
                    if (Math.Abs(atRaised.DerivedCo2Mt - atSeed.DerivedCo2Mt) > 1e-9) { ok = false; Debug.LogError($"CARBON TAX UNIT: {c.Id}'s dispatch moved under a carbon tax raise ({atSeed.DerivedCo2Mt:F4} → {atRaised.DerivedCo2Mt:F4} Mt) - ETS-covered plant is exempt of the tax (EN-4d)."); }
                }
                sb.Append(F("    {0,-8} line {1:F1} · baseline {2:F1} · family seed {3:F1} · full dial = {4:F0} points · a {5:F0} {6}/t raise = {7:F1} points\n", c.Id, line.Rate, baseline, familySeed, line.PointsOf(line.MaxRate), RaiseUsdPerTonne * EnergyLayer.NationalPerUsd(c.Id), EnergyLayer.CurrencyCode(c.Id), line.PointsOf((float)(RaiseUsdPerTonne * EnergyLayer.NationalPerUsd(c.Id)))));
            }

            // (5) the mechanism: Sweden raised fifty dollars per tonne
            sb.Append(F("\n    5. THE MECHANISM: Sweden's rate raised {0:F0} dollars per tonne ({1:F0} SEK/t) through the decision, ten years, against untouched\n", RaiseUsdPerTonne, RaiseUsdPerTonne * EnergyLayer.NationalPerUsd(CountryId.Sweden)));
            Outcome untouched = RunCountry(CountryId.Sweden, Years, 0f);
            Outcome raised = RunCountry(CountryId.Sweden, Years, (float)(RaiseUsdPerTonne * EnergyLayer.NationalPerUsd(CountryId.Sweden)));
            double rateRatio = raised.Rate / Math.Max(1e-6, untouched.Rate), revenueRatio = raised.Revenue / Math.Max(1e-6, untouched.Revenue);
            if (!(revenueRatio > 1.0) || !(revenueRatio <= rateRatio + 1e-6)) { ok = false; Debug.LogError($"CARBON TAX UNIT: Sweden's carbon revenue rose x{revenueRatio:F4} against a rate x{rateRatio:F4} - it should rise, and by no more than the rate (the base erodes)."); }
            if (!(raised.Transport < untouched.Transport)) { ok = false; Debug.LogError($"CARBON TAX UNIT: Sweden's transport intensity did not fall under the raise ({raised.Transport:F4} against {untouched.Transport:F4})."); }
            if (Math.Abs(raised.Power - untouched.Power) > 1e-5 * Math.Max(1e-6, untouched.Power)) { ok = false; Debug.LogError($"CARBON TAX UNIT: Sweden's power intensity moved under the raise ({untouched.Power:F5} → {raised.Power:F5}) - the fleet is exempt of the tax (EN-4d)."); }
            // EN-4e: the raise was applied once, at year 1's boundary, in that year's prices, and indexed since (Sweden's statute) - so the real raise is the
            // nominal raise over the price level at the raise; the coupling reads real dollars, and the factor is read against the seed at the same infrastructure term
            double expectedRealDollars = RaiseUsdPerTonne / Math.Max(0.0001, raised.PriceLevelAtRaise);
            if (Math.Abs(raised.RealDollarsAboveSeed - expectedRealDollars) > 0.15) { ok = false; Debug.LogError($"CARBON TAX UNIT: Sweden's real raise reads {raised.RealDollarsAboveSeed:F3} dollars per tonne after ten indexed years against {expectedRealDollars:F3} (the nominal {RaiseUsdPerTonne:F0} over the level at the raise, {raised.PriceLevelAtRaise:F4})."); }
            double expectedTransportFactor = 1.0 - EnvironmentFamily.TransportElasticityPerDollarPerTonne * raised.RealDollarsAboveSeed;
            double observedFactor = raised.TaxFactor;
            if (Math.Abs(observedFactor - expectedTransportFactor) > 1e-5) { ok = false; Debug.LogError($"CARBON TAX UNIT: the transport target's tax factor under the raise is {observedFactor:F5}, not 1 − e × real dollars ({expectedTransportFactor:F5})."); }
            if (Math.Abs(untouched.RealDollarsAboveSeed) > 0.05) { ok = false; Debug.LogError($"CARBON TAX UNIT: untouched Sweden's real rate drifted {untouched.RealDollarsAboveSeed:F3} dollars per tonne from its seed - the statute's indexation should hold it (EN-4e)."); }
            float politicalPoints = raised.PointsCharged;
            sb.Append(F("    untouched: rate {0:F0} SEK/t (indexed by statute from 1330; real {18:F1} in the seed's prices), revenue {1:F3} bn USD, transport {2:F4} t/head (target {3:F4}); raised once in year 1 by {12:F0} dollars and indexed since: rate {4:F0} (real {19:F1}), revenue {5:F3} (x{6:F4} against the rate x{7:F4}), transport {8:F4} (target {9:F4}; the tax factor {10:F5} = 1 − {11:F5} × {20:F2} real dollars, the raise over the level at the raise {21:F4}); power {16:F4} → {17:F4} t/head (held: the fleet pays the ETS, not the tax - EN-4d); the hike charged {13:F1} political points ({14:F1} % of the dial) to approval at {15:F2} per point\n",
                untouched.Rate, untouched.Revenue, untouched.Transport, untouched.TransportTarget, raised.Rate, raised.Revenue, revenueRatio, rateRatio, raised.Transport, raised.TransportTarget, observedFactor, EnvironmentFamily.TransportElasticityPerDollarPerTonne, RaiseUsdPerTonne, politicalPoints, politicalPoints, MacroSystem.TaxHikeApprovalSensitivity, untouched.Power, raised.Power, untouched.RealRate, raised.RealRate, raised.RealDollarsAboveSeed, raised.PriceLevelAtRaise));

            // (6) B6
            sb.Append("\n    6. B6: a rate per tonne reads the tonnes, not the price level\n");
            {
                Country se = world.GetCountry(CountryId.Sweden); TaxLine line = Find(se);
                float at1 = TaxBases.Revenue(se, line);
                float level = se.State.PriceLevel; se.State.PriceLevel = 2f;
                float at2 = TaxBases.Revenue(se, line);
                se.State.PriceLevel = level;
                if (Math.Abs(at1 - at2) > 1e-9) { ok = false; Debug.LogError($"CARBON TAX UNIT: Sweden's carbon revenue changed with the price level alone ({at1} → {at2}) - a rate per tonne on the same tonnes is the same money."); }
                sb.Append(F("    Sweden's carbon revenue at price index 1: {0:F3} bn; at 2 with the same tonnes: {1:F3} bn - the same nominal money, half the share of a doubled nominal GDP: an unindexed rate per tonne erodes as excise does - which is why the rate moves by statute between decisions since EN-4e (7 below): Sweden's indexed, Germany's scheduled, France's nominal and shown eroding\n", at1, at2));
            }

            // (7) the statutes (EN-4e): one world at no policy, ten years, every country's carbon line read at each boundary
            sb.Append("\n    7. THE STATUTES (EN-4e): at no policy for ten years - Sweden indexed by the year's price ratio to four decimals, Germany on the BEHG's schedule then carried by the level, France nominal, the ceilings carried, the zero-rate lines untouched\n");
            {
                StatuteRun run = RunStatutes(Years);
                Country se = run.World.GetCountry(CountryId.Sweden), de = run.World.GetCountry(CountryId.Germany), fr = run.World.GetCountry(CountryId.France), pl = run.World.GetCountry(CountryId.Poland);
                TaxLine seLine = Find(se), deLine = Find(de), frLine = Find(fr), plLine = Find(pl);
                // Sweden: the seed times the product of the rounded ratios; the real rate the seed's within the rounding; the ceiling by the unrounded level
                double expectedSe = 1330.0 * run.RoundedRatioProduct[CountryId.Sweden];
                if (Math.Abs(seLine.Rate - expectedSe) > 1e-3 * expectedSe) { ok = false; Debug.LogError($"CARBON TAX UNIT: Sweden's rate after {Years} years is {seLine.Rate:F2} against the seed times the rounded ratios' product {expectedSe:F2} (2 kap. 1 b §)."); }
                double seReal = CarbonRateStatute.RealRate(seLine.Rate, se.State.PriceLevel);
                if (Math.Abs(seReal - 1330.0) > 1330.0 * 5e-4 * Years) { ok = false; Debug.LogError($"CARBON TAX UNIT: Sweden's real rate after {Years} years is {seReal:F2} SEK/t in the seed's prices against 1330 - the indexation should hold it within the four-decimal rounding."); }
                double seCeilingExpected = 3180.0 * se.State.PriceLevel;
                if (Math.Abs(seLine.RateCeiling - seCeilingExpected) > 1e-3 * seCeilingExpected) { ok = false; Debug.LogError($"CARBON TAX UNIT: Sweden's ceiling reads {seLine.RateCeiling:F1} against 3180 carried by the level ({seCeilingExpected:F1})."); }
                // Germany: 45 (2024), 55 (2025), 55 (2026, the corridor's floor), then the last figure carried by the level
                double[] deExpected = { 45.0, 55.0, 55.0 };
                for (int y = 1; y <= 3; y++) { if (Math.Abs(run.Rate[CountryId.Germany][y] - deExpected[y - 1]) > 1e-4) { ok = false; Debug.LogError($"CARBON TAX UNIT: Germany's rate after boundary {y} is {run.Rate[CountryId.Germany][y]:F2} against the BEHG's {deExpected[y - 1]:F0} for {CarbonRateStatute.SeedYear + y}."); } }
                double deAfter = 55.0 * run.PriceLevelAtBoundary[CountryId.Germany][Years] / run.PriceLevelAtBoundary[CountryId.Germany][3];
                if (Math.Abs(deLine.Rate - deAfter) > 1e-3 * deAfter) { ok = false; Debug.LogError($"CARBON TAX UNIT: Germany's rate after {Years} years is {deLine.Rate:F3} against the last legislated 55 carried by the level since 2026 ({deAfter:F3})."); }
                // France: nominal, the real rate below the seed, the coupling's tax factor above one (the erosion the book shows)
                if (Math.Abs(frLine.Rate - 44.6f) > 1e-6) { ok = false; Debug.LogError($"CARBON TAX UNIT: France's rate moved to {frLine.Rate} - the composante carbone is a nominal amount, fixed since 2018."); }
                double frFactor = EnvironmentFamily.TransportTargetFor(fr, frLine.Rate, fr.Environment.InfrastructurePerHeadSeed) / EnvironmentFamily.TransportTargetFor(fr, 44.6f * fr.State.PriceLevel, fr.Environment.InfrastructurePerHeadSeed);
                if (!(fr.State.PriceLevel > 1.0f) || !(frFactor > 1.0)) { ok = false; Debug.LogError($"CARBON TAX UNIT: France's frozen rate should read as a real erosion - the coupling's tax factor is {frFactor:F5} at a price level of {fr.State.PriceLevel:F4}."); }
                // the zero-rate lines: untouched, their ceilings carried
                if (plLine.IsImplemented || plLine.Rate != 0f) { ok = false; Debug.LogError($"CARBON TAX UNIT: Poland's unimplemented line moved ({plLine.Rate}, {(plLine.IsImplemented ? "implemented" : "unimplemented")}) - a zero-rate country is unaffected."); }
                double plCeilingExpected = 1260.0 * pl.State.PriceLevel;
                if (Math.Abs(plLine.RateCeiling - plCeilingExpected) > 1e-3 * plCeilingExpected) { ok = false; Debug.LogError($"CARBON TAX UNIT: Poland's ceiling reads {plLine.RateCeiling:F1} against 1260 carried by the level ({plCeilingExpected:F1}) - the dial's reach indexes for all six."); }
                sb.Append(F("    Sweden   {0:F0} → {1:F1} SEK/t over {2} boundaries (the rounded ratios' product {3:F5}; the level {4:F5}) - real {5:F1} in the seed's prices; the ceiling 3180 → {6:F0}\n", 1330.0, seLine.Rate, Years, run.RoundedRatioProduct[CountryId.Sweden], se.State.PriceLevel, seReal, seLine.RateCeiling));
                sb.Append(F("    Germany  30 → {0:F0} ({1}) → {2:F0} ({3}) → {4:F0} ({5}, the corridor's floor) → {6:F2} after {7} years (55 carried by the level since 2027, the ETS's own convention) - real {8:F1}\n", run.Rate[CountryId.Germany][1], CarbonRateStatute.SeedYear + 1, run.Rate[CountryId.Germany][2], CarbonRateStatute.SeedYear + 2, run.Rate[CountryId.Germany][3], CarbonRateStatute.SeedYear + 3, deLine.Rate, Years, CarbonRateStatute.RealRate(deLine.Rate, de.State.PriceLevel)));
                sb.Append(F("    France   44.6 held nominal; real {0:F2} EUR/t at a price level of {1:F4}; the transport coupling's tax factor {2:F5} (above one: the erosion, shown)\n", CarbonRateStatute.RealRate(frLine.Rate, fr.State.PriceLevel), fr.State.PriceLevel, frFactor));
                sb.Append(F("    Poland   0, unimplemented, untouched; its ceiling 1260 → {0:F0} PLN/t (the dial's reach in the year's prices, all six)\n", plLine.RateCeiling));
            }

            sb.Append(ok ? "\n=== CarbonTaxUnitDiagnostic: ALL ASSERTIONS PASS ===\n" : "\n=== CarbonTaxUnitDiagnostic: FAILED (see above) ===\n");
            if (ok) { Debug.Log(sb.ToString()); } else { Debug.LogError(sb.ToString()); }
            CheckExit.Finish(ok ? 0 : 1);
        }

        private sealed class Outcome { public double Rate, Revenue, Transport, TransportTarget, Power, RealRate, RealDollarsAboveSeed, TaxFactor, PriceLevelAtRaise; public float PointsCharged; }

        private sealed class StatuteRun
        {
            public World World;
            public readonly Dictionary<CountryId, double[]> Rate = new Dictionary<CountryId, double[]>(), PriceLevelAtBoundary = new Dictionary<CountryId, double[]>();
            public readonly Dictionary<CountryId, double> RoundedRatioProduct = new Dictionary<CountryId, double>();
        }

        /// <summary>One world at no policy: every country's carbon rate and price level read after each boundary, and the product of the boundaries' price ratios rounded as Sweden's statute rounds them.</summary>
        private static StatuteRun RunStatutes(int years)
        {
            SimulationRandom.Seed(777);
            EnergyMarket.ResetCalibration();
            World world = WorldFactory.CreateDefault();
            var go = new GameObject("CARBONSTATUTE");
            try
            {
                SimulationManager sim = go.AddComponent<SimulationManager>();
                sim.SetWorld(world);
                sim.PlayerCountryId = CountryId.Sweden;
                var run = new StatuteRun { World = world };
                foreach (Country c in world.Countries) { run.Rate[c.Id] = new double[years + 1]; run.PriceLevelAtBoundary[c.Id] = new double[years + 1]; run.RoundedRatioProduct[c.Id] = 1.0; TaxLine l = Find(c); run.Rate[c.Id][0] = l != null ? l.Rate : 0; run.PriceLevelAtBoundary[c.Id][0] = c.State.PriceLevel; }
                var decisions = new Dictionary<CountryId, PolicyDecision>();
                foreach (Country k in world.Countries) { decisions[k.Id] = PolicyDecision.None(); }
                var before = new Dictionary<CountryId, float>();
                for (int year = 1; year <= years; year++)
                {
                    for (int day = 0; day < SimulationManager.DaysPerTurn; day++) { sim.AdvanceDay(); }
                    foreach (Country k in world.Countries) { before[k.Id] = k.PriceLevelAtLastIndex; }   // the reference the statute reads at this boundary
                    sim.AdvanceTurn(decisions);
                    foreach (Country k in world.Countries)
                    {
                        TaxLine l = Find(k); run.Rate[k.Id][year] = l != null ? l.Rate : 0; run.PriceLevelAtBoundary[k.Id][year] = k.PriceLevelAtLastIndex;   // the level the boundary indexed at
                        double ratio = before[k.Id] > 0f ? k.PriceLevelAtLastIndex / before[k.Id] : 1.0;
                        run.RoundedRatioProduct[k.Id] *= Math.Round(ratio, CarbonRateStatute.JamforelsetalDecimals);
                    }
                }
                return run;
            }
            finally { UnityEngine.Object.DestroyImmediate(go); }
        }

        private static Outcome RunCountry(CountryId player, int years, float raiseNationalPerTonne)
        {
            SimulationRandom.Seed(777);
            EnergyMarket.ResetCalibration();
            World world = WorldFactory.CreateDefault();
            var go = new GameObject("CARBONTAXUNIT");
            try
            {
                SimulationManager sim = go.AddComponent<SimulationManager>();
                sim.SetWorld(world);
                sim.PlayerCountryId = player;
                Country c = world.GetCountry(player);
                TaxLine line = Find(c);
                float seedRate = line.Rate;
                var outcome = new Outcome { PointsCharged = line.PointsOf(raiseNationalPerTonne) };
                var decisions = new Dictionary<CountryId, PolicyDecision>();
                foreach (Country k in world.Countries) { decisions[k.Id] = PolicyDecision.None(); }
                for (int year = 1; year <= years; year++)
                {
                    for (int day = 0; day < SimulationManager.DaysPerTurn; day++) { sim.AdvanceDay(); }
                    PolicyDecision d = PolicyDecision.None();
                    // EN-4e: the raise is applied ONCE, at year 1's boundary, in that boundary's prices; thereafter the statute indexes it as it indexes the seed
                    if (raiseNationalPerTonne != 0f && year == 1) { d.TaxRateOverrides[TaxType.CarbonTax] = seedRate * CarbonRateStatute.YearRatio(c) + raiseNationalPerTonne; }
                    decisions[player] = d;
                    sim.AdvanceTurn(decisions);
                    if (year == 1) { outcome.PriceLevelAtRaise = c.PriceLevelAtLastIndex; }
                }
                outcome.Rate = line.Rate;
                outcome.RealRate = CarbonRateStatute.RealRate(line.Rate, c.State.PriceLevel);
                outcome.RealDollarsAboveSeed = (outcome.RealRate - c.Environment.CarbonTaxRateSeed) / EnergyLayer.NationalPerUsd(c.Id);
                float infra = EnvironmentFamily.PerHead(c, SpendingCategory.InfrastructureAndDevelopment, SpendingCategory.Transportation);
                outcome.TaxFactor = EnvironmentFamily.TransportTargetFor(c, line.Rate, infra) / Math.Max(1e-9, EnvironmentFamily.TransportTargetFor(c, c.Environment.CarbonTaxRateSeed * c.State.PriceLevel, infra));   // against the seed in today's prices at the same infrastructure term
                outcome.Revenue = TaxBases.Revenue(c, line);
                outcome.Transport = c.State.TransportCo2PerCapita;
                outcome.Power = c.State.PowerCo2PerCapita;
                outcome.TransportTarget = EnvironmentFamily.TransportTargetFor(c, EnvironmentFamily.CarbonTaxRate(c), EnvironmentFamily.PerHead(c, SpendingCategory.InfrastructureAndDevelopment, SpendingCategory.Transportation));
                return outcome;
            }
            finally { UnityEngine.Object.DestroyImmediate(go); }
        }

        private static TaxLine Find(Country c) { foreach (TaxLine l in c.TaxLines) { if (l.Type == TaxType.CarbonTax) { return l; } } return null; }
        private static string F(string format, params object[] args) => string.Format(CultureInfo.InvariantCulture, format, args);
    }
}
