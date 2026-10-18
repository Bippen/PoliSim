using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text;
using PoliSim.Data;
using PoliSim.Simulation;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// P2-0.1 (Playtest 2, 2026-09-02) — **every money figure on Statistics › Domestic traced to its
    /// basis, and the one seam where a figure authored on one country's scale reached another's.**
    ///
    /// <para><b>The finding as Elias sent it:</b> a deficit figure on the Domestic sheet looked as if a
    /// national-unit (SEK) line had been mixed into a USD total, the likeliest seam being the budget
    /// decomposition's sourced lines against the USD seed. <b>Measure first.</b> This diagnostic is the
    /// measurement; it ran red before the fix and the numbers it printed are the record.</para>
    ///
    /// <para><b>What it enumerates.</b> For each of the six countries: (1) the seed — GDP, the debt stock
    /// and the ratio identity, GDP per capita's identity, and every seeded spending line as a share of
    /// GDP (a SEK figure applied to a $-basis economy would be a multiple of it, not a share of it);
    /// (2) after two closed years — the fiscal report's revenue, spending and balance as shares of GDP,
    /// with the sign convention the sheet prints, and the cumulative accumulator beside the year's
    /// balance (P2-0.4's fact - section 4 below asserts the annual series against the debt identity); (3) <b>the one-time settlements</b> —
    /// every option in the cabinet decision pool and the foreign-policy meeting pool, its authored
    /// figure, and what it lands as on each country's economy. ⚠ <b>This is where the basis broke:</b>
    /// the pools are authored in billions on the USA seed's scale and were applied unscaled, so the same
    /// option was a fraction of a percent of the USA's GDP and a third of Sweden's (the exact shares are
    /// printed by every run, before and after the seam). The fix is at the seam
    /// (<see cref="AuthoredImpactScale"/>), never at the display, and this diagnostic asserts the applied
    /// figure against a ceiling on every country.</para>
    ///
    /// <para><b>Directions (S-37).</b> Every bound below is a ceiling on a share of GDP; growth above it
    /// fails. None is a target.</para>
    /// </summary>
    public static class DomesticMoneyBasisDiagnostic
    {
        /// <summary>No single seeded spending line above this share of GDP. A ceiling, not a target: the
        /// largest real line (Sweden's pension top-up plus UO11, or the USA's Social Security) sits well
        /// under it, and a national-unit figure on a $-basis economy would be several multiples of it.</summary>
        private const float MaxSeededLineShareOfGdp = 30f;
        /// <summary>The seeded portfolio's sum as a share of GDP, a ceiling.</summary>
        private const float MaxSeededPortfolioShareOfGdp = 70f;
        /// <summary>The closed year's revenue and spending as shares of GDP, floors and ceilings. Wide on
        /// purpose - these catch a basis error (a figure off by the SEK/USD ratio), not a calibration.</summary>
        private const float MinFlowShareOfGdp = 10f;
        private const float MaxFlowShareOfGdp = 70f;
        /// <summary>The closed year's deficit or surplus as a share of GDP, a ceiling on the magnitude.</summary>
        private const float MaxBalanceShareOfGdp = 15f;
        /// <summary>An authored one-time settlement, as applied, above this share of the deciding country's
        /// GDP is a basis defect. The ceiling sits above the largest authored option's share of the USA seed's
        /// GDP (printed by the run) and gives nothing beyond that headroom.</summary>
        private const float MaxOneTimeImpactShareOfGdp = 2f;
        private const int TurnsToClose = 2;
        /// <summary>P2-0.4: years closed one at a time for the annual series and the debt identity.</summary>
        private const int AnnualYearsToClose = 4;

        public static void Run()
        {
            CheckExit.ArmLogFold();
            int failures = 0;
            var sb = new StringBuilder();
            sb.Append("=== P2-0.1: every Domestic money figure to its basis ===\n");

            SimulationRandom.Seed(777);
            World world = WorldFactory.CreateDefault();
            var go = new GameObject("DomesticMoneyBasis");
            try
            {
                SimulationManager sim = go.AddComponent<SimulationManager>();
                sim.SetWorld(world);

                sb.Append("\n--- 1. The seed: the headline figures and every spending line as a share of GDP ---\n");
                foreach (Country country in world.Countries)
                {
                    EconomyState s = country.State;
                    float ratio = s.GovernmentDebt / s.NominalGdp * 100f;   // R-T3 (§584): the identity is nominal over nominal - at the seed the price level is 1 and the two read alike, which is how the crossing hid
                    float perCapita = s.Population > 0f ? s.GDP / s.Population : float.NaN;
                    sb.Append(F("  {0,-8} GDP {1,9:0.0}  debt {2,9:0.0}  debt/GDP {3,6:0.0}% (identity {4,6:0.0}%)  per capita {5,6:0.0}k  population {6,6:0.0}M\n",
                        country.Id, s.GDP, s.GovernmentDebt, s.DebtToGdpRatio, ratio, perCapita, s.Population));
                    failures += Assert(sb, $"{country.Id}: Debt-to-GDP is the identity debt/GDP",
                        Mathf.Abs(s.DebtToGdpRatio - ratio) < 1e-3f, F("{0} vs {1}", s.DebtToGdpRatio, ratio));

                    float sum = 0f;
                    float largest = 0f;
                    SpendingCategory largestCategory = default;
                    foreach (SpendingLine line in country.SpendingLines)
                    {
                        sum += line.Amount;
                        if (line.Amount > largest) { largest = line.Amount; largestCategory = line.Category; }
                    }

                    float sumShare = sum / s.NominalGdp * 100f;
                    float largestShare = largest / s.NominalGdp * 100f;
                    sb.Append(F("           {0} spending lines sum {1,8:0.0} = {2,5:0.0}% of GDP; largest {3} {4,7:0.0} = {5,5:0.0}% of GDP\n",
                        country.SpendingLines.Count, sum, sumShare, largestCategory, largest, largestShare));
                    failures += Assert(sb, $"{country.Id}: no seeded line above {MaxSeededLineShareOfGdp}% of GDP",
                        largestShare <= MaxSeededLineShareOfGdp, F("{0} at {1:0.0}%", largestCategory, largestShare));
                    failures += Assert(sb, $"{country.Id}: the seeded portfolio sums to at most {MaxSeededPortfolioShareOfGdp}% of GDP",
                        sumShare <= MaxSeededPortfolioShareOfGdp, F("{0:0.0}%", sumShare));
                }

                sb.Append(F("\n--- 2. After {0} closed years: the fiscal report's shares, as the sheet prints them ---\n", TurnsToClose));
                var noDecisions = new Dictionary<CountryId, PolicyDecision>();
                int closed = 0;
                int guard = 0;
                while (closed < TurnsToClose && guard++ < 3000)
                {
                    if (sim.AdvanceDay())
                    {
                        sim.AdvanceTurn(noDecisions);
                        closed++;
                    }
                }

                foreach (Country country in world.Countries)
                {
                    FiscalTurnReport report = sim.GetLastFiscalReport(country.Id);
                    failures += Assert(sb, $"{country.Id}: a fiscal report exists after {TurnsToClose} closed years", report != null, "null");
                    if (report == null) { continue; }

                    float? tax = DerivedStats.TaxBurdenPercentOfGdp(country, report);
                    float? spending = DerivedStats.SpendingPercentOfGdp(country, report);
                    float? deficit = DerivedStats.DeficitPercentOfGdp(country, report);
                    float? primary = DerivedStats.PrimaryDeficitPercentOfGdp(country, report);
                    sb.Append(F("  {0,-8} revenue {1,7:0.0} ({2,5:0.0}% of GDP)  spending {3,7:0.0} ({4,5:0.0}%)  balance {5,7:+0.0;-0.0} -> the sheet says {6} {7:0.0}%  primary {8:0.0}%  cumulative accumulator {9,8:+0.0;-0.0}\n",
                        country.Id, report.Revenue, tax ?? float.NaN, report.TotalSpending, spending ?? float.NaN, report.BudgetBalance,
                        deficit.HasValue && deficit.Value < 0f ? "SURPLUS" : "DEFICIT", Mathf.Abs(deficit ?? float.NaN), primary ?? float.NaN, country.State.Budget));
                    failures += Assert(sb, $"{country.Id}: revenue share of GDP within [{MinFlowShareOfGdp}, {MaxFlowShareOfGdp}]",
                        tax.HasValue && tax.Value >= MinFlowShareOfGdp && tax.Value <= MaxFlowShareOfGdp, F("{0:0.0}%", tax ?? float.NaN));
                    failures += Assert(sb, $"{country.Id}: spending share of GDP within [{MinFlowShareOfGdp}, {MaxFlowShareOfGdp}]",
                        spending.HasValue && spending.Value >= MinFlowShareOfGdp && spending.Value <= MaxFlowShareOfGdp, F("{0:0.0}%", spending ?? float.NaN));
                    failures += Assert(sb, $"{country.Id}: the year's balance within {MaxBalanceShareOfGdp}% of GDP either way",
                        deficit.HasValue && Mathf.Abs(deficit.Value) <= MaxBalanceShareOfGdp, F("{0:0.0}%", deficit ?? float.NaN));
                    failures += Assert(sb, $"{country.Id}: the sheet's deficit is the report's balance with the sign flipped, over the same GDP",
                        deficit.HasValue && Mathf.Abs(deficit.Value - (-report.BudgetBalance / country.State.NominalGdp * 100f)) < 1e-3f,
                        F("{0} vs {1}", deficit ?? float.NaN, -report.BudgetBalance / country.State.NominalGdp * 100f));
                }

                sb.Append("\n--- 3. The one-time settlements: every pooled option, authored and as applied, per country ---\n");
                sb.Append(F("    (authored on the USA seed's scale, GDP {0:0}; applied as the same share of the deciding country's GDP)\n", AuthoredImpactScale.AuthoredScaleGdp));
                List<(string Source, string Option, float Authored)> options = EnumeratePooledOptions(sb, ref failures);
                foreach (Country country in world.Countries)
                {
                    float worstShare = 0f;
                    string worst = "-";
                    float worstApplied = 0f;
                    float worstUnscaled = 0f;
                    foreach ((string source, string option, float authored) in options)
                    {
                        float applied = AuthoredImpactScale.ToCountryBillions(authored, country);
                        float share = Mathf.Abs(applied) / country.State.NominalGdp * 100f;   // FT-14 (§587): sized on nominal GDP, read against it
                        if (share > worstShare)
                        {
                            worstShare = share;
                            worst = source + " / " + option;
                            worstApplied = applied;
                            worstUnscaled = authored;
                        }
                    }

                    sb.Append(F("  {0,-8} largest as applied {1,7:+0.0;-0.0} = {2,5:0.00}% of GDP (unscaled it was {3,7:+0.0;-0.0} = {4,6:0.00}% of GDP)  <- {5}\n",
                        country.Id, worstApplied, worstShare, worstUnscaled, Mathf.Abs(worstUnscaled) / country.State.NominalGdp * 100f, worst));
                    failures += Assert(sb, $"{country.Id}: no pooled settlement lands above {MaxOneTimeImpactShareOfGdp}% of its GDP",
                        worstShare <= MaxOneTimeImpactShareOfGdp, F("{0:0.00}% ({1})", worstShare, worst));

                    // FT-14 (§587): an authored figure is sized on NOMINAL GDP - the check the share above cannot make, because a share read
                    // against the same GDP the figure was sized on is authored / scale under either rule (the s587 review's F1). At a price level
                    // other than 1 the applied figure must be authored × GDP × the price level ÷ the scale; on real GDP it misses by the level.
                    float heldLevel = country.State.PriceLevel;
                    country.State.PriceLevel = heldLevel * 2.5f;
                    float probeApplied = AuthoredImpactScale.ToCountryBillions(100f, country);
                    float probeExpected = 100f * country.State.GDP * country.State.PriceLevel / AuthoredImpactScale.AuthoredScaleGdp;
                    country.State.PriceLevel = heldLevel;
                    failures += Assert(sb, $"{country.Id}: an authored figure is sized on nominal GDP (the price level raised 2.5-fold, the figure with it)",
                        Mathf.Abs(probeApplied - probeExpected) <= 1e-3f * Mathf.Abs(probeExpected), F("{0:0.000} applied against {1:0.000} expected", probeApplied, probeExpected));
                }

                // The identity claim is about the SEED (the scale is the USA seed's GDP), so it is read off a
                // fresh world rather than the one that has closed two years above - the first run of this
                // check asked it of a USA that had grown for two years and reported the growth as a defect.
                Country usa = WorldFactory.CreateDefault().GetCountry(CountryId.USA);
                bool usaIdentical = true;
                foreach ((string _, string _, float authored) in options)
                {
                    if (AuthoredImpactScale.ToCountryBillions(authored, usa) != authored) { usaIdentical = false; break; }
                }

                failures += Assert(sb, "USA at seed: every option applies exactly as authored (the scale is the USA seed, byte-identical)",
                    usaIdentical, F("USA GDP now {0}", usa.State.GDP));

                // P2-0.4 (2026-09-02): THE ANNUAL SERIES SUMS TO THE DEBT DELTA. A fresh world, closed one year at a
                // time; after every close the year's balance must be the newest point of the annual series the
                // sheet draws. The tolerance is the ledger's idiom: relative to the stock, floored.
                //
                // ⚠ AF-17 (Elias's ruling q17, 2026-10-18; GA-2-9): THE ONE-OFFS COUNT IN THE YEAR THE PLAYER READS, ITEMISED. §220 built
                // the balance as the year's flow and named the one-time settlements beside it here only, so a year with a cabinet decision
                // printed a deficit smaller than the debt's own change. Now, every closed year: the debt's change IS the balance with the
                // sign flipped, beyond the two terms that are not a year's money - the erosion term (retired by P5-B6, read as it stands) and
                // the clamp's truncation; the balance is the ledger's flow terms PLUS its dated events; and the report itemises every event
                // as a named line, the lines summing to its one-offs. The harness PUTS one-offs in (the pool's largest cost and largest
                // windfall in year 1, the cost again in year 2, through the player's own path, ResolveCabinetDecision) - section 4 used to
                // run with no decisions, so its settlements were zero and the omission could not fail it. The review's L6: the windfall
                // again on year 2's BOUNDARY DAY, after AdvanceDay and before AdvanceTurn (it belongs to the closing year), and the cost
                // again directly AFTER year 2's close (it belongs to year 3). Year 4 carries none: its balance must be the flow exactly.
                // On the pre-AF-17 tree the identities of every planted year fail by the one-offs. The review's M2: the report's
                // FlowBalance - the figure the EU ministry reads - stays the ledger's flow terms alone, net of the one-offs, every year.
                sb.Append(F("\n--- 4. The annual budget balance: the series the sheet draws, and the debt identity over {0} closed years, one-offs counted ---\n", AnnualYearsToClose));
                (string Source, string Option, float Authored) cost = default, windfall = default;
                foreach ((string source, string option, float authored) in options)
                {
                    if (authored < cost.Authored) { cost = (source, option, authored); }
                    if (authored > windfall.Authored) { windfall = (source, option, authored); }
                }
                failures += Assert(sb, "AF-17: the pools carry a cost and a windfall to plant (a harness that plants nothing verifies nothing)",
                    cost.Authored < 0f && windfall.Authored > 0f, F("cost {0} ({1}), windfall {2} ({3})", cost.Authored, cost.Option, windfall.Authored, windfall.Option));
                // what each planted day lands: (the closed-year index it falls in, the day of that year - BoundaryEve on the boundary day before
                // AdvanceTurn, AfterClose directly after the previous year's AdvanceTurn - the pooled settlement)
                const int BoundaryEve = -1, AfterClose = -2;
                var planted = new List<(int Year, int Day, (string Source, string Option, float Authored) Option)>
                    { (0, 60, cost), (0, 120, windfall), (1, 90, cost), (1, BoundaryEve, windfall), (2, AfterClose, cost) };
                void Plant(SimulationManager sim, World w, int year, int day)
                {
                    foreach (var plant in planted)
                    {
                        if (plant.Year != year || plant.Day != day) { continue; }
                        var option = new CabinetDecisionOption("AF-17 probe - " + plant.Option.Option, budgetImpact: plant.Option.Authored);
                        var decision = new CabinetDecision("AF-17 probe", plant.Option.Source, option);
                        foreach (Country c in w.Countries) { sim.ResolveCabinetDecision(c.Id, CabinetPortfolio.FinanceTreasury, decision, option); }
                    }
                }
                SimulationRandom.Seed(777);
                World annualWorld = WorldFactory.CreateDefault();
                var annualGo = new GameObject("DomesticMoneyBasisAnnual");
                try
                {
                    SimulationManager annualSim = annualGo.AddComponent<SimulationManager>();
                    annualSim.SetWorld(annualWorld);
                    var debtAtSeed = new Dictionary<CountryId, float>();
                    var sumNegBalance = new Dictionary<CountryId, float>();
                    var sumErosion = new Dictionary<CountryId, float>();
                    var sumOneOffs = new Dictionary<CountryId, float>();
                    var sumClamp = new Dictionary<CountryId, float>();
                    var lineCount = new Dictionary<CountryId, int>();
                    foreach (Country c in annualWorld.Countries)
                    {
                        debtAtSeed[c.Id] = c.State.GovernmentDebt;
                        sumNegBalance[c.Id] = 0f; sumErosion[c.Id] = 0f; sumOneOffs[c.Id] = 0f; sumClamp[c.Id] = 0f; lineCount[c.Id] = 0;
                    }

                    int yearsClosed = 0;
                    int dayOfYear = 0;
                    int dayGuard = 0;
                    while (yearsClosed < AnnualYearsToClose && dayGuard++ < 400 * AnnualYearsToClose)
                    {
                        bool boundary = annualSim.AdvanceDay();
                        dayOfYear++;
                        Plant(annualSim, annualWorld, yearsClosed, dayOfYear);
                        if (!boundary) { continue; }
                        Plant(annualSim, annualWorld, yearsClosed, BoundaryEve);
                        annualSim.AdvanceTurn(noDecisions);
                        yearsClosed++;
                        dayOfYear = 0;
                        Plant(annualSim, annualWorld, yearsClosed, AfterClose);
                        int plantedThisYear = 0;
                        foreach (var plant in planted) { if (plant.Year == yearsClosed - 1) { plantedThisYear++; } }
                        foreach (Country c in annualWorld.Countries)
                        {
                            FiscalTurnReport yearReport = annualSim.GetLastFiscalReport(c.Id);
                            DebtAttribution ledger = c.FiscalLedgerLastPeriod;
                            IReadOnlyList<float> series = c.History.BudgetBalanceAnnual;
                            bool newestIsTheYear = yearReport != null && series.Count == yearsClosed && series[series.Count - 1] == yearReport.BudgetBalance;
                            failures += Assert(sb, $"{c.Id} year {yearsClosed}: the annual series' newest point IS the closed year's balance",
                                newestIsTheYear, F("series has {0} point(s), newest {1}, report {2}", series.Count, series.Count > 0 ? series[series.Count - 1] : float.NaN, yearReport?.BudgetBalance ?? float.NaN));
                            if (yearReport == null || ledger == null) { continue; }

                            float tolerance = Mathf.Max(0.01f, 2e-4f * Mathf.Max(Mathf.Abs(ledger.DebtAtPeriodOpen), Mathf.Abs(ledger.DebtAtClose)));
                            float debtChange = ledger.DebtAtClose - ledger.DebtAtPeriodOpen;
                            float readAsDeficit = -yearReport.BudgetBalance + ledger.Erosion + ledger.ClampLoss;
                            sb.Append(F("  {0,-8} year {1}: debt change {2,9:+0.000;-0.000} | the year's balance {3,9:+0.000;-0.000}, of which one-offs {4,8:+0.000;-0.000} in {5} line(s); erosion {6:+0.000;-0.000}, clamp {7:+0.000;-0.000}\n",
                                c.Id, yearsClosed, debtChange, yearReport.BudgetBalance, yearReport.OneOffs, yearReport.OneOffLines?.Count ?? -1, ledger.Erosion, ledger.ClampLoss));
                            failures += Assert(sb, $"{c.Id} year {yearsClosed}: AF-17 - the debt's change IS the closed year's deficit (beyond erosion and the clamp)",
                                Mathf.Abs(debtChange - readAsDeficit) <= tolerance, F("debt change {0:0.0000} vs -balance+erosion+clamp {1:0.0000} (tol {2:0.0000})", debtChange, readAsDeficit, tolerance));
                            float flowTerms = ledger.TermSum - ledger.Erosion;
                            failures += Assert(sb, $"{c.Id} year {yearsClosed}: the report's FlowBalance (the EU ministry's reading) is the ledger's flow terms alone, sign flipped - net of the one-offs",
                                Mathf.Abs(-yearReport.FlowBalance - flowTerms) <= tolerance && Mathf.Abs(yearReport.BudgetBalance - yearReport.FlowBalance - yearReport.OneOffs) <= tolerance,
                                F("-flow {0:0.0000} vs terms-erosion {1:0.0000}; balance-flow {2:0.0000} vs one-offs {3:0.0000} (tol {4:0.0000})", -yearReport.FlowBalance, flowTerms, yearReport.BudgetBalance - yearReport.FlowBalance, yearReport.OneOffs, tolerance));
                            float flowAndEvents = ledger.TermSum - ledger.Erosion + ledger.EventSum;
                            failures += Assert(sb, $"{c.Id} year {yearsClosed}: the year's balance is the ledger's flow terms plus its dated events, sign flipped",
                                Mathf.Abs(-yearReport.BudgetBalance - flowAndEvents) <= tolerance, F("-balance {0:0.0000} vs terms-erosion+events {1:0.0000} (tol {2:0.0000})", -yearReport.BudgetBalance, flowAndEvents, tolerance));

                            // itemised: one named line per event, as it landed, the lines summing to the one-offs
                            List<FiscalOneOff> lines = yearReport.OneOffLines;
                            bool itemised = lines != null && lines.Count == ledger.Events.Count;
                            float lineSum = 0f;
                            for (int i = 0; itemised && i < lines.Count; i++)
                            {
                                itemised &= lines[i].Label == ledger.Events[i].Label && lines[i].Date == ledger.Events[i].Date && lines[i].Amount == -ledger.Events[i].AppliedDelta && !string.IsNullOrEmpty(lines[i].Label)
                                    && lines[i].Source == ledger.Events[i].Source && (!lines[i].Label.StartsWith("Cabinet: AF-17 probe", StringComparison.Ordinal) || lines[i].Source == "AF-17 probe");   // L2: the decision's name rides the line
                                lineSum += lines[i].Amount;
                            }
                            failures += Assert(sb, $"{c.Id} year {yearsClosed}: every one-off is its own named, dated line, as it landed on the debt, and the lines sum to the year's one-offs",
                                itemised && Mathf.Abs(lineSum - yearReport.OneOffs) <= 1e-5f * Mathf.Max(1f, Mathf.Abs(yearReport.OneOffs)), F("{0} line(s) for {1} event(s); lines sum {2:0.0000}, one-offs {3:0.0000}", lines?.Count ?? -1, ledger.Events.Count, lineSum, yearReport.OneOffs));
                            failures += Assert(sb, $"{c.Id} year {yearsClosed}: the harness's {plantedThisYear} planted one-off(s) reached the year",
                                lines != null && lines.Count >= plantedThisYear && (plantedThisYear == 0 || yearReport.OneOffs != 0f), F("{0} line(s), one-offs {1:0.0000}", lines?.Count ?? -1, yearReport.OneOffs));
                            sumNegBalance[c.Id] += -yearReport.BudgetBalance;
                            sumErosion[c.Id] += ledger.Erosion;
                            sumOneOffs[c.Id] += yearReport.OneOffs;
                            sumClamp[c.Id] += ledger.ClampLoss;
                            lineCount[c.Id] += lines?.Count ?? 0;
                        }
                    }

                    foreach (Country c in annualWorld.Countries)
                    {
                        float observed = c.State.GovernmentDebt - debtAtSeed[c.Id];
                        float explained = sumNegBalance[c.Id] + sumErosion[c.Id] + sumClamp[c.Id];
                        float tolerance = Mathf.Max(0.01f, 2e-4f * Mathf.Max(Mathf.Abs(debtAtSeed[c.Id]), Mathf.Abs(c.State.GovernmentDebt))) * AnnualYearsToClose;
                        sb.Append(F("  {0,-8} debt {1,9:0.0} -> {2,9:0.0}  delta {3,8:+0.0;-0.0} = -sum(annual balance) {4,8:+0.0;-0.0} (of which one-offs {5,6:+0.0;-0.0} in {6} line(s)) + erosion {7,7:+0.0;-0.0} + clamp {8,6:+0.0;-0.0}\n",
                            c.Id, debtAtSeed[c.Id], c.State.GovernmentDebt, observed, sumNegBalance[c.Id], -sumOneOffs[c.Id], lineCount[c.Id], sumErosion[c.Id], sumClamp[c.Id]));
                        failures += Assert(sb, $"{c.Id}: over {AnnualYearsToClose} years the annual series sums to the debt delta, the one-offs inside it, erosion and clamp named",
                            Mathf.Abs(observed - explained) <= tolerance, F("observed {0:0.0000} vs explained {1:0.0000} (tol {2:0.0000})", observed, explained, tolerance));
                    }
                    // P2-3.3 (2026-09-02): THE COMPASS TRAIL IS THE STORED HISTORY, ONE POINT PER CLOSE. After the
                    // closes above: every country whose chamber has a mean carries exactly one trail point per
                    // closed year, dated at the close; the newest equals the chamber mean recomputed now (seats
                    // change only at elections, and the last close saw the current seats); and the list the compass
                    // draws (CompassPositions.Trail) is the stored lists element for element.
                    sb.Append(F("\n--- P2-3.3. The compass trail after {0} closed years ---\n", AnnualYearsToClose));
                    foreach (Country c in annualWorld.Countries)
                    {
                        PoliSim.Elections.CompassPositions.Point? now = PoliSim.Elections.CompassPositions.ChamberMean(c, out int _);
                        List<(DateTime Date, float LrEcon, float Galtan)> trail = PoliSim.Elections.CompassPositions.Trail(c);
                        if (!now.HasValue)
                        {
                            failures += Assert(sb, $"{c.Id}: a chamber with no mean stores no trail", trail.Count == 0, F("{0} point(s)", trail.Count));
                            continue;
                        }
                        failures += Assert(sb, $"{c.Id}: one trail point per closed year", trail.Count == yearsClosed, F("{0} point(s) for {1} close(s)", trail.Count, yearsClosed));
                        if (trail.Count == 0) { continue; }
                        (DateTime Date, float LrEcon, float Galtan) newest = trail[trail.Count - 1];
                        failures += Assert(sb, $"{c.Id}: the newest trail point is the chamber mean now",
                            Mathf.Abs(newest.LrEcon - now.Value.LrEcon) < 1e-5f && Mathf.Abs(newest.Galtan - now.Value.Galtan) < 1e-5f,
                            F("stored ({0:F3}, {1:F3}) vs now ({2:F3}, {3:F3})", newest.LrEcon, newest.Galtan, now.Value.LrEcon, now.Value.Galtan));
                        bool datesOrdered = true, listsAgree = c.History.CompassTrailLrEcon.Count == trail.Count && c.History.CompassTrailGaltan.Count == trail.Count && c.History.CompassTrailDates.Count == trail.Count;
                        for (int i = 0; i < trail.Count && listsAgree; i++)
                        {
                            listsAgree &= trail[i].LrEcon == c.History.CompassTrailLrEcon[i] && trail[i].Galtan == c.History.CompassTrailGaltan[i] && trail[i].Date == c.History.CompassTrailDates[i];
                            if (i > 0 && trail[i].Date <= trail[i - 1].Date) { datesOrdered = false; }
                        }
                        failures += Assert(sb, $"{c.Id}: the drawn trail is the stored lists, element for element, dated in order", listsAgree && datesOrdered, F("{0} point(s), first {1:yyyy-MM-dd}, last {2:yyyy-MM-dd}", trail.Count, trail[0].Date, newest.Date));
                    }
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(annualGo);
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }

            sb.Append(F("\n=== P2-0.1: {0} failure(s) ===\n", failures));
            Debug.Log(sb.ToString());
            CheckExit.Finish(failures == 0 ? 0 : 1);
        }

        /// <summary>Both pools, read by reflection - a harness reaching private state is the project's
        /// idiom, and a public accessor for a diagnostic would be a door nothing in play uses.</summary>
        private static List<(string Source, string Option, float Authored)> EnumeratePooledOptions(StringBuilder sb, ref int failures)
        {
            var options = new List<(string, string, float)>();
            FieldInfo cabinetField = typeof(CabinetSystem).GetField("DecisionPool", BindingFlags.NonPublic | BindingFlags.Static);
            if (cabinetField?.GetValue(null) is IDictionary cabinetPool)
            {
                foreach (DictionaryEntry entry in cabinetPool)
                {
                    if (!(entry.Value is List<CabinetDecision> decisions)) { continue; }
                    foreach (CabinetDecision decision in decisions)
                    {
                        foreach (CabinetDecisionOption option in decision.Options)
                        {
                            options.Add(("cabinet: " + decision.Name, option.Label, option.BudgetImpact));
                        }
                    }
                }
            }

            FieldInfo meetingField = typeof(ForeignPolicySystem).GetField("MeetingPool", BindingFlags.NonPublic | BindingFlags.Static);
            if (meetingField?.GetValue(null) is List<ForeignPolicyMeeting> meetings)
            {
                foreach (ForeignPolicyMeeting meeting in meetings)
                {
                    foreach (ForeignPolicyMeetingOption option in meeting.Options)
                    {
                        options.Add(("foreign policy: " + meeting.Name, option.Label, option.BudgetImpact));
                    }
                }
            }

            int withMoney = 0;
            float largest = 0f;
            foreach ((string _, string _, float authored) in options)
            {
                if (authored != 0f) { withMoney++; }
                largest = Mathf.Max(largest, Mathf.Abs(authored));
            }

            sb.Append(F("    {0} pooled options, {1} with a settlement, largest authored magnitude {2:0}\n", options.Count, withMoney, largest));
            failures += Assert(sb, "both pools were read (a diagnostic that enumerates nothing verified nothing)", withMoney > 0, "no options with a settlement found by reflection");
            return options;
        }

        private static int Assert(StringBuilder sb, string claim, bool holds, string detail)
        {
            sb.Append(holds ? "    ok    " : "    FAIL  ").Append(claim).Append("  (").Append(detail).Append(")\n");
            return holds ? 0 : 1;
        }

        private static string F(string format, params object[] args) => string.Format(CultureInfo.InvariantCulture, format, args);
    }
}
