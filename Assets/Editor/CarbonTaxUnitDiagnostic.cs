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
    /// follows the BEHG's schedule by the CALENDAR year each boundary falls in (GA-8-1, 2026-10-14) - on the default epoch, whose first boundary
    /// falls after the schedule, the law's last 55 carried by the level from that boundary; on Germany's own start (2024-11-06) 55, 55 and then the level -
    /// and a figure a bill passed stands at its year's boundary, Germany's and Sweden's alike, the statute resuming at the next; France's 44.6 stands nominal, its real rate falls and the coupling's
    /// tax factor rises above one; every ceiling carries the level; Poland's unimplemented line stays 0. The raise in (5) is applied once, in
    /// year 1, and indexes thereafter as the law indexes it - the factor is read in REAL dollars. (8) AF-36 (q36, 2026-10-18): the date the slip and
    /// the Desk show for a passed figure is the boundary the statute moves it at, its stamp cleared there; a bill carries a passed figure into the year
    /// after its own passage - the figure it passes, or one standing - and a carry of the statute's own figure carries nothing.
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
                // Germany on the default epoch (2026-10-01) - GA-8-1: the boundaries fall in 2027 onward, past the schedule, the first among them: the
                // law's last figure (2026's 55) carried by the level from that boundary, never the seed's 2023 figure carried
                for (int y = 1; y <= Years; y++)
                {
                    double carried = CarbonRateStatute.LastScheduledFigure * run.PriceLevelAtBoundary[CountryId.Germany][y] / run.PriceLevelAtBoundary[CountryId.Germany][0];
                    if (Math.Abs(run.Rate[CountryId.Germany][y] - carried) > 1e-3 * carried) { ok = false; Debug.LogError($"CARBON TAX UNIT: Germany's rate after boundary {y} on the default epoch is {run.Rate[CountryId.Germany][y]:F3} against the law's last 55 carried by the level ({carried:F3}) - a game whose first boundary falls after the schedule."); }
                }
                // France: nominal, the real rate below the seed, the coupling's tax factor above one (the erosion the book shows)
                if (Math.Abs(frLine.Rate - 44.6f) > 1e-6) { ok = false; Debug.LogError($"CARBON TAX UNIT: France's rate moved to {frLine.Rate} - the composante carbone is a nominal amount, fixed since 2018."); }
                double frFactor = EnvironmentFamily.TransportTargetFor(fr, frLine.Rate, fr.Environment.InfrastructurePerHeadSeed) / EnvironmentFamily.TransportTargetFor(fr, 44.6f * fr.State.PriceLevel, fr.Environment.InfrastructurePerHeadSeed);
                if (!(fr.State.PriceLevel > 1.0f) || !(frFactor > 1.0)) { ok = false; Debug.LogError($"CARBON TAX UNIT: France's frozen rate should read as a real erosion - the coupling's tax factor is {frFactor:F5} at a price level of {fr.State.PriceLevel:F4}."); }
                // the zero-rate lines: untouched, their ceilings carried
                if (plLine.IsImplemented || plLine.Rate != 0f) { ok = false; Debug.LogError($"CARBON TAX UNIT: Poland's unimplemented line moved ({plLine.Rate}, {(plLine.IsImplemented ? "implemented" : "unimplemented")}) - a zero-rate country is unaffected."); }
                double plCeilingExpected = 1260.0 * pl.State.PriceLevel;
                if (Math.Abs(plLine.RateCeiling - plCeilingExpected) > 1e-3 * plCeilingExpected) { ok = false; Debug.LogError($"CARBON TAX UNIT: Poland's ceiling reads {plLine.RateCeiling:F1} against 1260 carried by the level ({plCeilingExpected:F1}) - the dial's reach indexes for all six."); }
                sb.Append(F("    Sweden   {0:F0} → {1:F1} SEK/t over {2} boundaries (the rounded ratios' product {3:F5}; the level {4:F5}) - real {5:F1} in the seed's prices; the ceiling 3180 → {6:F0}\n", 1330.0, seLine.Rate, Years, run.RoundedRatioProduct[CountryId.Sweden], se.State.PriceLevel, seReal, seLine.RateCeiling));
                sb.Append(F("    Germany  30 → {0:F2} at the first boundary (2027, past the schedule: the law's last 55 carried by the level) → {1:F2} after {2} years - real {3:F1}\n", run.Rate[CountryId.Germany][1], deLine.Rate, Years, CarbonRateStatute.RealRate(deLine.Rate, de.State.PriceLevel)));
                sb.Append(F("    France   44.6 held nominal; real {0:F2} EUR/t at a price level of {1:F4}; the transport coupling's tax factor {2:F5} (above one: the erosion, shown)\n", CarbonRateStatute.RealRate(frLine.Rate, fr.State.PriceLevel), fr.State.PriceLevel, frFactor));
                sb.Append(F("    Poland   0, unimplemented, untouched; its ceiling 1260 → {0:F0} PLN/t (the dial's reach in the year's prices, all six)\n", plLine.RateCeiling));

                // GA-8-1 (2026-10-14): Germany's own start, 2024-11-06 - the boundaries fall in 2025, 2026 and 2027 and read their CALENDAR years: 2025's 55,
                // 2026's 55 (the corridor's floor), then 55 carried by the level - where the turns' count set 2024's 45 at the first, a year behind the law
                DateTime germanStart = PoliSim.Elections.WorldClock.StartDate(CountryId.Germany);
                StatuteRun atStart = RunStatutes(3, germanStart);
                double[] onStart = { 55.0, 55.0, 55.0 * atStart.PriceLevelAtBoundary[CountryId.Germany][3] / atStart.PriceLevelAtBoundary[CountryId.Germany][2] };
                for (int y = 1; y <= 3; y++) { if (Math.Abs(atStart.Rate[CountryId.Germany][y] - onStart[y - 1]) > 1e-3 * onStart[y - 1]) { ok = false; Debug.LogError($"CARBON TAX UNIT: on Germany's start ({germanStart:yyyy-MM-dd}) its rate after boundary {y} ({germanStart.Year + y}) is {atStart.Rate[CountryId.Germany][y]:F3} against {onStart[y - 1]:F3} - the BEHG's figure for the boundary's calendar year."); } }
                sb.Append(F("    Germany on its own start ({0:yyyy-MM-dd}): 30 → {1:F0} ({2}) → {3:F0} ({4}, the corridor's floor) → {5:F2} ({6}, carried by the level)\n", germanStart, atStart.Rate[CountryId.Germany][1], germanStart.Year + 1, atStart.Rate[CountryId.Germany][2], germanStart.Year + 2, atStart.Rate[CountryId.Germany][3], germanStart.Year + 3));

                // GA-8-1: a figure a budget bill passed stands for the year it was passed for, whatever the rule - Germany's against its schedule, Sweden's
                // against its indexation - and the statute moves it again from the first boundary of a later year. Passed on day 100 of the first year, in 2025
                const int PassedOnDay = 100;
                StatuteRun passed = RunStatutes(2, germanStart, (CountryId.Germany, 1, PassedOnDay, 80f, false), (CountryId.Sweden, 1, PassedOnDay, 1500f, false));
                int passedYear = germanStart.AddDays(PassedOnDay + 1).Year;
                double seIndexed = 1500.0 * Math.Round(passed.PriceLevelAtBoundary[CountryId.Sweden][2] / passed.PriceLevelAtBoundary[CountryId.Sweden][1], CarbonRateStatute.JamforelsetalDecimals);
                if (Math.Abs(passed.Rate[CountryId.Germany][1] - 80.0) > 1e-4 || Math.Abs(passed.Rate[CountryId.Germany][2] - 55.0) > 1e-4) { ok = false; Debug.LogError($"CARBON TAX UNIT: Germany's 80 passed in {passedYear} reads {passed.Rate[CountryId.Germany][1]:F3} at that year's boundary (it stands: 80) and {passed.Rate[CountryId.Germany][2]:F3} at the next (the statute's 55)."); }
                if (Math.Abs(passed.Rate[CountryId.Sweden][1] - 1500.0) > 1e-3 || Math.Abs(passed.Rate[CountryId.Sweden][2] - seIndexed) > 1e-3 * seIndexed) { ok = false; Debug.LogError($"CARBON TAX UNIT: Sweden's 1500 passed in {passedYear} reads {passed.Rate[CountryId.Sweden][1]:F3} at that year's boundary (it stands: 1500, unindexed) and {passed.Rate[CountryId.Sweden][2]:F3} at the next (indexed: {seIndexed:F3})."); }
                sb.Append(F("    A figure passed in {0} (day {1}): Germany 80 → {2:F0} at {0}'s boundary (it stands) → {3:F0} at the next (the statute's); Sweden 1500 → {4:F0} (it stands, unindexed) → {5:F1} (indexed again)\n", passedYear, PassedOnDay, passed.Rate[CountryId.Germany][1], passed.Rate[CountryId.Germany][2], passed.Rate[CountryId.Sweden][1], passed.Rate[CountryId.Sweden][2]));

                // (8) AF-36 (Elias's ruling q36, 2026-10-18): "the slip and the Desk show the date the rate reverts, and a bill may carry a passed figure into
                // the next year". THE DATE: what the two surfaces read (`SimulationManager.TryPassedCarbonFigure`, read just after the bill passed) is the
                // boundary at which the statute moved the passed figure, and the stamp is cleared there. THE CARRY: a bill carrying the figure it passes, or a
                // later bill carrying one standing, keeps it through the year after the bill's own (DECLARED), and the statute moves it from the boundary after; a carry where
                // the figure is the statute's carries nothing. On Germany's own start, the boundaries in 2025 to 2028.
                sb.Append("\n    8. AF-36 - THE DATE A PASSED RATE REVERTS, AND THE CARRY\n");
                bool Near(double a, double b) => Math.Abs(a - b) <= 1e-4 * Math.Max(1.0, Math.Abs(b));
                string Day(DateTime? d) => d.HasValue ? d.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) : "none";
                for (int k = 0; k < 2; k++)
                {
                    CountryId who = k == 0 ? CountryId.Germany : CountryId.Sweden;
                    if (passed.ShownYield[k] != passed.Boundary[2]) { ok = false; Debug.LogError($"CARBON TAX UNIT (AF-36): {who}'s figure passed in {passedYear} shows the day it reverts as {Day(passed.ShownYield[k])}; the statute moved it at {Day(passed.Boundary[2])}."); }
                    if (passed.Stamp[who][1] != passedYear || passed.Stamp[who][2] != 0) { ok = false; Debug.LogError($"CARBON TAX UNIT (AF-36): {who}'s stamp reads {passed.Stamp[who][1]} after {passedYear}'s boundary ({passedYear}) and {passed.Stamp[who][2]} after the next (cleared: 0)."); }
                }
                sb.Append(F("    The date shown when the figure passed: Germany {0}, Sweden {1} - the boundary that moved it ({2}); the stamp {3} then 0\n", Day(passed.ShownYield[0]), Day(passed.ShownYield[1]), Day(passed.Boundary[2]), passed.Stamp[CountryId.Germany][1]));

                StatuteRun carriedRun = RunStatutes(3, germanStart, (CountryId.Germany, 1, PassedOnDay, 80f, true), (CountryId.Sweden, 1, PassedOnDay, 1500f, true));
                double deCarried3 = 80.0 * carriedRun.PriceLevelAtBoundary[CountryId.Germany][3] / carriedRun.PriceLevelAtBoundary[CountryId.Germany][2];
                double seCarried3 = 1500.0 * Math.Round(carriedRun.PriceLevelAtBoundary[CountryId.Sweden][3] / carriedRun.PriceLevelAtBoundary[CountryId.Sweden][2], CarbonRateStatute.JamforelsetalDecimals);
                if (!Near(carriedRun.Rate[CountryId.Germany][1], 80.0) || !Near(carriedRun.Rate[CountryId.Germany][2], 80.0) || !Near(carriedRun.Rate[CountryId.Germany][3], deCarried3)) { ok = false; Debug.LogError($"CARBON TAX UNIT (AF-36): Germany's 80 passed and carried in {passedYear} reads {carriedRun.Rate[CountryId.Germany][1]:F3}, {carriedRun.Rate[CountryId.Germany][2]:F3}, {carriedRun.Rate[CountryId.Germany][3]:F3} at the three boundaries - 80, 80 (carried into {passedYear + 1}), then the statute's {deCarried3:F3} (past the schedule, carried by the level)."); }
                if (!Near(carriedRun.Rate[CountryId.Sweden][1], 1500.0) || !Near(carriedRun.Rate[CountryId.Sweden][2], 1500.0) || !Near(carriedRun.Rate[CountryId.Sweden][3], seCarried3)) { ok = false; Debug.LogError($"CARBON TAX UNIT (AF-36): Sweden's 1500 passed and carried in {passedYear} reads {carriedRun.Rate[CountryId.Sweden][1]:F3}, {carriedRun.Rate[CountryId.Sweden][2]:F3}, {carriedRun.Rate[CountryId.Sweden][3]:F3} - 1500, 1500 (carried, unindexed), then indexed ({seCarried3:F3})."); }
                for (int k = 0; k < 2; k++)
                {
                    CountryId who = k == 0 ? CountryId.Germany : CountryId.Sweden;
                    if (carriedRun.ShownYield[k] != carriedRun.Boundary[3]) { ok = false; Debug.LogError($"CARBON TAX UNIT (AF-36): {who}'s carried figure shows the day it reverts as {Day(carriedRun.ShownYield[k])}; the statute moved it at {Day(carriedRun.Boundary[3])}."); }
                }
                sb.Append(F("    Passed and carried in {0}: Germany 80 → {1:F0} → {2:F0} (carried into {3}) → {4:F2} (the statute's, from {5}); Sweden 1500 → {6:F0} → {7:F0} → {8:F1}; the date shown {9}\n", passedYear, carriedRun.Rate[CountryId.Germany][1], carriedRun.Rate[CountryId.Germany][2], passedYear + 1, carriedRun.Rate[CountryId.Germany][3], Day(carriedRun.Boundary[3]), carriedRun.Rate[CountryId.Sweden][1], carriedRun.Rate[CountryId.Sweden][2], carriedRun.Rate[CountryId.Sweden][3], Day(carriedRun.ShownYield[0])));

                // a later bill carrying the standing figure (a carry alone, no figure named) - passed in the next year, it carries the figure into the year
                // after that - and a carry where the figure is the statute's (Sweden's, never passed)
                StatuteRun later = RunStatutes(4, germanStart, (CountryId.Germany, 1, PassedOnDay, 80f, false), (CountryId.Germany, 2, PassedOnDay, -1f, true), (CountryId.Sweden, 1, PassedOnDay, -1f, true));
                double deLater4 = 80.0 * later.PriceLevelAtBoundary[CountryId.Germany][4] / later.PriceLevelAtBoundary[CountryId.Germany][3];
                if (!Near(later.Rate[CountryId.Germany][2], 80.0) || !Near(later.Rate[CountryId.Germany][3], 80.0) || !Near(later.Rate[CountryId.Germany][4], deLater4) || later.ShownYield[0] != later.Boundary[2] || later.ShownYield[1] != later.Boundary[4]) { ok = false; Debug.LogError($"CARBON TAX UNIT (AF-36): Germany's 80 passed in {passedYear} and carried by a bill of {passedYear + 1} reads {later.Rate[CountryId.Germany][2]:F3} and {later.Rate[CountryId.Germany][3]:F3} at {passedYear + 1}'s and {passedYear + 2}'s boundaries (80, 80) and {later.Rate[CountryId.Germany][4]:F3} at the next ({deLater4:F3}); the date shown {Day(later.ShownYield[0])} before the carry ({Day(later.Boundary[2])}) and {Day(later.ShownYield[1])} after it ({Day(later.Boundary[4])})."); }
                if (later.ShownYield[2].HasValue || later.Stamp[CountryId.Sweden][1] != 0 || Math.Abs(later.Rate[CountryId.Sweden][1] - atStart.Rate[CountryId.Sweden][1]) > 1e-3 * atStart.Rate[CountryId.Sweden][1]) { ok = false; Debug.LogError($"CARBON TAX UNIT (AF-36): a carry of Sweden's statutory figure carried something - the date {Day(later.ShownYield[2])}, the stamp {later.Stamp[CountryId.Sweden][1]}, the rate {later.Rate[CountryId.Sweden][1]:F3} at the first boundary against the statute's {atStart.Rate[CountryId.Sweden][1]:F3}."); }
                sb.Append(F("    Carried by a bill of {0}: Germany 80 → {1:F0} → {2:F0} → {3:F0} → {4:F2}, the date {5} before the carry and {6} after; a carry of Sweden's statutory figure carries nothing ({7:F1}, as the statute indexes it)\n", passedYear + 1, later.Rate[CountryId.Germany][1], later.Rate[CountryId.Germany][2], later.Rate[CountryId.Germany][3], later.Rate[CountryId.Germany][4], Day(later.ShownYield[0]), Day(later.ShownYield[1]), later.Rate[CountryId.Sweden][1]));

                // AF-36 (the review's M1): the player's carry draft is SPENT once the bill carrying it passes - so it never rides a second bill - and stays
                // drafted where nothing moved the line (a failed bill; a carry of the statute's figure, which carried nothing)
                bool spentAfterCarry = carriedRun.CarrySpent[0] == true && carriedRun.CarrySpent[1] == true && later.CarrySpent[1] == true;
                var unmovedLine = new TaxLine(TaxType.CarbonTax, 80f, true) { PassedForYear = passedYear };
                TaxCarryDraft heldDraft = TaxCarryDraft.Of(unmovedLine);
                if (!spentAfterCarry || later.CarrySpent[2] != false || heldDraft.SpentOn(unmovedLine)) { ok = false; Debug.LogError($"CARBON TAX UNIT (AF-36): the carry draft is not spent by its own passage, or is spent with nothing moved - spent after the carried 80 {carriedRun.CarrySpent[0]}, the carried 1500 {carriedRun.CarrySpent[1]}, the later carry {later.CarrySpent[1]} (all True); after the carry of Sweden's statutory figure {later.CarrySpent[2]} and on an unmoved line {heldDraft.SpentOn(unmovedLine)} (both False)."); }
                sb.Append(F("    The carry draft spent by its own passage: Germany {0}, Sweden {1}, the later carry {2}; kept where nothing moved: {3}, {4}\n", carriedRun.CarrySpent[0], carriedRun.CarrySpent[1], later.CarrySpent[1], !(later.CarrySpent[2] ?? true), !heldDraft.SpentOn(unmovedLine)));

                // AF-36 (s873's M1): a carry drafted BEFORE a boundary that moves the line - Germany's schedule (30 → 55), Sweden's index - survives it (a rate
                // move with no stamp is the statute's, never a bill's) and is carried by the bill after it: drafted on day 300 of the first year, the bills on
                // day 100 of the second (80 and 1500, carrying), the figures standing at the second and third boundaries
                const int DraftedOnDay = 300;
                StatuteRun across = RunStatutes(3, germanStart, new[] { (CountryId.Germany, 1, DraftedOnDay), (CountryId.Sweden, 1, DraftedOnDay) },
                    (CountryId.Germany, 2, PassedOnDay, 80f, true), (CountryId.Sweden, 2, PassedOnDay, 1500f, true));
                for (int k = 0; k < 2; k++)
                {
                    CountryId who = k == 0 ? CountryId.Germany : CountryId.Sweden;
                    double figure = k == 0 ? 80.0 : 1500.0;
                    bool movedByBoundary = !Near(across.Rate[who][1], across.Rate[who][0]);   // the premise: the boundary between the draft and the bill moved the line
                    if (!movedByBoundary || across.SpentBeforeBill[k] != false || across.CarrySpent[k] != true || !Near(across.Rate[who][2], figure) || !Near(across.Rate[who][3], figure) || across.Stamp[who][2] != passedYear + 2)
                    {
                        ok = false;
                        Debug.LogError($"CARBON TAX UNIT (AF-36): {who}'s carry drafted before a boundary - the boundary moved the line {across.Rate[who][0]:F3} → {across.Rate[who][1]:F3} (it must), the draft spent when the bill came {across.SpentBeforeBill[k]} (False) and after it passed {across.CarrySpent[k]} (True), the figure {across.Rate[who][2]:F3} and {across.Rate[who][3]:F3} at the next two boundaries ({figure}), the stamp {across.Stamp[who][2]} ({passedYear + 2}).");
                    }
                }
                sb.Append(F("    A carry drafted before a boundary: Germany {0:F0} → {1:F0} at the boundary, the draft kept ({2}), carried by the bill of {3}: 80 → {4:F0} → {5:F0}; Sweden {6:F0} → {7:F1}, kept ({8}): 1500 → {9:F0} → {10:F0}\n",
                    across.Rate[CountryId.Germany][0], across.Rate[CountryId.Germany][1], across.SpentBeforeBill[0] == false, passedYear + 1, across.Rate[CountryId.Germany][2], across.Rate[CountryId.Germany][3],
                    across.Rate[CountryId.Sweden][0], across.Rate[CountryId.Sweden][1], across.SpentBeforeBill[1] == false, across.Rate[CountryId.Sweden][2], across.Rate[CountryId.Sweden][3]));

                // AF-36 (the review s873b's M1): only a bill that CARRIED the line spends the draft. (a) Germany: the carry ticked on day 50, while a bill
                // introduced without it was before the chamber - that bill passes 80 on day 100 and stamps the line, the draft kept; a carry alone in the
                // next year carries it and spends the draft. (b) Sweden: 1500 passed on day 100, the carry ticked on day 200 on that standing figure; the
                // next year's boundary takes the figure back (indexed, the stamp cleared) and the draft is kept; a bill in the third year passes 1600 with
                // the carry and spends it, the figure standing at the third boundary
                StatuteRun onlyCarried = RunStatutes(3, germanStart, new[] { (CountryId.Germany, 1, 50), (CountryId.Sweden, 1, 200) },
                    (CountryId.Germany, 1, PassedOnDay, 80f, false), (CountryId.Germany, 2, PassedOnDay, -1f, true),
                    (CountryId.Sweden, 1, PassedOnDay, 1500f, false), (CountryId.Sweden, 3, PassedOnDay, 1600f, true));
                if (onlyCarried.CarrySpent[0] != false || onlyCarried.CarrySpent[1] != true || !Near(onlyCarried.Rate[CountryId.Germany][2], 80.0) || !Near(onlyCarried.Rate[CountryId.Germany][3], 80.0))
                {
                    ok = false;
                    Debug.LogError($"CARBON TAX UNIT (AF-36): (a) Germany's carry ticked while a bill without it was pending - spent by that bill {onlyCarried.CarrySpent[0]} (False), by the later carrying bill {onlyCarried.CarrySpent[1]} (True); the figure {onlyCarried.Rate[CountryId.Germany][2]:F3} and {onlyCarried.Rate[CountryId.Germany][3]:F3} at the second and third boundaries (80, carried).");
                }
                if (onlyCarried.Stamp[CountryId.Sweden][2] != 0 || onlyCarried.SpentBeforeBill[3] != false || onlyCarried.CarrySpent[3] != true || !Near(onlyCarried.Rate[CountryId.Sweden][3], 1600.0) || onlyCarried.Stamp[CountryId.Sweden][3] != passedYear + 3)
                {
                    ok = false;
                    Debug.LogError($"CARBON TAX UNIT (AF-36): (b) Sweden's carry ticked on a standing figure - the stamp after the second boundary {onlyCarried.Stamp[CountryId.Sweden][2]} (cleared: 0), the draft spent when the third year's bill came {onlyCarried.SpentBeforeBill[3]} (False) and after it {onlyCarried.CarrySpent[3]} (True), the figure {onlyCarried.Rate[CountryId.Sweden][3]:F3} at the third boundary (1600), the stamp {onlyCarried.Stamp[CountryId.Sweden][3]} ({passedYear + 3}).");
                }
                sb.Append(F("    Only a carrying bill spends the draft: (a) Germany - kept past the bill without it ({0}), spent by the carry of {1} ({2}), 80 → {3:F0} → {4:F0}; (b) Sweden - kept past the boundary that took 1500 back ({5:F1}, the stamp {6}) ({7}), spent by the bill of {8} carrying 1600 ({9}) → {10:F0}\n",
                    onlyCarried.CarrySpent[0] == false, passedYear + 1, onlyCarried.CarrySpent[1], onlyCarried.Rate[CountryId.Germany][2], onlyCarried.Rate[CountryId.Germany][3],
                    onlyCarried.Rate[CountryId.Sweden][2], onlyCarried.Stamp[CountryId.Sweden][2], onlyCarried.SpentBeforeBill[3] == false, passedYear + 2, onlyCarried.CarrySpent[3], onlyCarried.Rate[CountryId.Sweden][3]));

                // the carry travels with the rates: Poland's tax act takes it, and the budget act the rates are withheld from loses it
                var parts = new BudgetBill();
                parts.CarriedTaxLines.Add(TaxType.CarbonTax);
                if (!parts.TaxActPart().CarriedTaxLines.Contains(TaxType.CarbonTax) || parts.WithoutRateChanges().CarriedTaxLines.Count != 0) { ok = false; Debug.LogError("CARBON TAX UNIT (AF-36): the carry does not travel with the rates - the tax act must carry it and the act without the rates must not."); }
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
            /// <summary>AF-36: each pass's date as the slip and the Desk read it just after it passed (null where none stands), each country's carbon stamp after each boundary, and each boundary's date.</summary>
            public DateTime?[] ShownYield;
            public readonly Dictionary<CountryId, int[]> Stamp = new Dictionary<CountryId, int[]>();
            public DateTime[] Boundary;
            /// <summary>AF-36 (the review's M1): for a pass that carries or meets an earlier draft, whether the player's carry draft (an earlier one, or taken just before the bill) is spent once it passed (`TaxCarryDraft.SpentOn`); null for the rest.</summary>
            public bool?[] CarrySpent;
            /// <summary>AF-36 (s873's M1): for a pass that carries, whether its draft was already spent when the bill came - a draft taken earlier (the drafts overload of RunStatutes) must survive the boundaries between.</summary>
            public bool?[] SpentBeforeBill;
        }

        /// <summary>One world at no policy: every country's carbon rate and price level read after each boundary, and the product of the boundaries' price ratios rounded as Sweden's statute rounds them.
        /// GA-8-1: opened on <paramref name="epoch"/> where one is given (a country's start - the boundaries then fall in its calendar years), the
        /// default epoch otherwise; each of <paramref name="passes"/> passes a budget bill naming that country's carbon figure on that day of its year (AF-36:
        /// a negative figure names none, and the bill CARRIES the line where the pass says so),
        /// through the bill's own applying step (`ParliamentSystem.ApplyBillResult`) with the year of the day it passes.</summary>
        private static StatuteRun RunStatutes(int years, DateTime? epoch = null, params (CountryId Id, int Year, int Day, float Figure, bool Carry)[] passes) =>
            RunStatutes(years, epoch, new (CountryId, int, int)[0], passes);

        /// <summary>AF-36 (s873's M1): as above, with the player's carry DRAFTED for a country on a day of its own (<paramref name="drafts"/>, its target the draft's
        /// passage year plus one, as the Budget page takes it) - a carrying pass for that country then uses that draft, not one taken on its own day.</summary>
        private static StatuteRun RunStatutes(int years, DateTime? epoch, (CountryId Id, int Year, int Day)[] drafts, params (CountryId Id, int Year, int Day, float Figure, bool Carry)[] passes)
        {
            using IDisposable epochScope = SimulationManager.EpochScope();
            SimulationManager.SetEpoch(epoch ?? SimulationManager.DefaultEpoch);   // the default epoch named, never the process's: a start a tool left would move every reading
            SimulationRandom.Seed(777);
            EnergyMarket.ResetCalibration();
            World world = WorldFactory.CreateDefault();
            var go = new GameObject("CARBONSTATUTE");
            try
            {
                SimulationManager sim = go.AddComponent<SimulationManager>();
                sim.SetWorld(world);
                sim.PlayerCountryId = CountryId.Sweden;
                var run = new StatuteRun { World = world, ShownYield = new DateTime?[passes.Length], Boundary = new DateTime[years + 1], CarrySpent = new bool?[passes.Length], SpentBeforeBill = new bool?[passes.Length] };
                var drafted = new Dictionary<CountryId, TaxCarryDraft>();
                foreach (Country c in world.Countries) { run.Stamp[c.Id] = new int[years + 1]; }
                foreach (Country c in world.Countries) { run.Rate[c.Id] = new double[years + 1]; run.PriceLevelAtBoundary[c.Id] = new double[years + 1]; run.RoundedRatioProduct[c.Id] = 1.0; TaxLine l = Find(c); run.Rate[c.Id][0] = l != null ? l.Rate : 0; run.PriceLevelAtBoundary[c.Id][0] = c.State.PriceLevel; }
                var decisions = new Dictionary<CountryId, PolicyDecision>();
                foreach (Country k in world.Countries) { decisions[k.Id] = PolicyDecision.None(); }
                var before = new Dictionary<CountryId, float>();
                for (int year = 1; year <= years; year++)
                {
                    for (int day = 0; day < SimulationManager.DaysPerTurn; day++)
                    {
                        sim.AdvanceDay();
                        foreach ((CountryId draftFor, int draftYear, int draftDay) in drafts) { if (draftYear == year && draftDay == day) { drafted[draftFor] = TaxCarryDraft.Of(Find(world.GetCountry(draftFor))); } }   // AF-36: an earlier draft
                        // AF-36: a pass in any year
                        for (int p = 0; p < passes.Length; p++)
                        {
                            (CountryId id, int onYear, int onDay, float figure, bool carry) = passes[p];
                            if (onYear != year || onDay != day) { continue; }
                            var bill = new BudgetBill();
                            if (figure >= 0f) { bill.TaxLines[TaxType.CarbonTax] = figure; }   // AF-36: a negative figure is a carry alone
                            TaxCarryDraft draft = drafted.TryGetValue(id, out TaxCarryDraft early) ? early : carry ? TaxCarryDraft.Of(Find(world.GetCountry(id))) : null;   // AF-36 (M1): the UI's draft - an earlier one where drafted, read at every bill of its country whether the bill carries or not
                            if (draft != null) { run.SpentBeforeBill[p] = draft.SpentOn(Find(world.GetCountry(id))); }
                            if (carry) { bill.CarriedTaxLines.Add(TaxType.CarbonTax); }
                            float approvalBeforeBill = world.GetCountry(id).State.ApprovalRating;   // the bill's approval cost recorded as the game's callers record it (SimulationManager's budget resolution) - unrecorded, a hike in a later year failed the approval ledger's audit
                            ParliamentSystem.ApplyBillResult(world.GetCountry(id), bill, true, (c, b) => { }, sim.CurrentDate.Year);
                            ApprovalLedgerRecorder.RecordEvent(world.GetCountry(id), sim.CurrentDate, "Budget bill passed (tax changes)", world.GetCountry(id).State.ApprovalRating - approvalBeforeBill);
                            if (draft != null) { run.CarrySpent[p] = draft.SpentOn(Find(world.GetCountry(id))); }
                            run.ShownYield[p] = sim.TryPassedCarbonFigure(id, out TaxLine _, out DateTime shown) ? shown : (DateTime?)null;   // AF-36: the date the slip and the Desk show, read as they read it
                        }
                    }
                    foreach (Country k in world.Countries) { before[k.Id] = k.PriceLevelAtLastIndex; }   // the reference the statute reads at this boundary
                    sim.AdvanceTurn(decisions);
                    foreach (Country k in world.Countries)
                    {
                        TaxLine l = Find(k); run.Rate[k.Id][year] = l != null ? l.Rate : 0; run.Stamp[k.Id][year] = l != null ? l.PassedForYear : 0; run.Boundary[year] = SimulationManager.TurnBoundary(year); run.PriceLevelAtBoundary[k.Id][year] = k.PriceLevelAtLastIndex;   // the level the boundary indexed at
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
