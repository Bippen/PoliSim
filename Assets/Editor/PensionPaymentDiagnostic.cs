using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using PoliSim.Data;
using PoliSim.Simulation;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// PN-2 (2026-09-16, the plan's S6): **the pension payment as a readout, MEASURED FIRST.** The model has one pension line
    /// (`SpendingCategory.SocialSecurity`) and a pyramid; the average benefit it implies is that line over the people at or above
    /// the statutory age, and the replacement rate is that benefit over the mean income the schedules already read.
    ///
    /// <para><b>PN-4 (2026-09-16, §519): the seed against its source SAME YEAR, share of GDP against share of GDP.</b> §518's gate held the
    /// model's 2026 line, in euro, against ESSPROS's 2022 euro band, and read Poland ×1.209 above it and Sweden ×0.724 below. Measured
    /// (section 5 below decomposes both figures), Poland's was THE YEAR - the model's GDP in euro over 2022's, times the share's own
    /// 2022→2024 indexation - and in the same year's band Poland is inside; Sweden's was THE PERIMETER - COFOG counts general government
    /// (S13) and ESSPROS every scheme, and Sweden's occupational schemes pay 2.9 % of GDP of old-age pensions outside the state - times
    /// a slip in the seed's own arithmetic (a top-up constant derived for a budget sum of 1,314 bn SEK while the code's areas sum to
    /// 1,504), which this pass lands on the source's 7.0. So the gate now compares like with like: the seed's share against the COFOG
    /// share it was typed from (provenance, the tight guard), and that share against ESSPROS's two definitions FOR THE SAME YEAR, with
    /// the perimeter - what ESSPROS counts that general government does not - printed beside the verdict rather than read as an error.
    /// A euro figure of one year is never again held against a line of another.</para>
    ///
    /// <para><b>The headcount's sub-band assumption, stated.</b> The pyramid is five-year bands, so a statutory age inside a band
    /// takes that band's fraction ((band end − age) ⁄ 5) and every band above it whole - the uniform-within-cohort approximation
    /// `PopulationCohorts` names, applied here and said out loud rather than discovered later (DS-3b's own rule).</para>
    ///
    /// <para><b>What the sources measure, which is not the same thing.</b> ESSPROS is expenditure over beneficiaries, so it counts
    /// everyone drawing a pension including survivors and the disabled and those below the statutory age, from every scheme; this
    /// readout's denominator is the cohorts at or above the age and its line is general government's. `ilc_pnp3` is the median
    /// individual gross pension of 65–74 over the median gross earnings of 50–59; this readout's is a mean over a mean. Each difference
    /// is printed as the reason a gap is not automatically an error.</para>
    /// </summary>
    public static class PensionPaymentDiagnostic
    {
        /// <summary>The table S1's prep script derives from the four Eurostat files on disk (`Tools/pension_prep.pl`, section 5): COFOG and
        /// ESSPROS side by side per country and year, as shares of GDP, with nominal GDP and the beneficiary counts. Read here, never typed.</summary>
        public const string SourceRelative = "ElectionsData/pensions/pension_line_sources_2019_2024.csv";

        /// <summary>The seed's own vintage: every EU pension line is COFOG GF10.02/D62 of this year (WorldFactory's seed comments).</summary>
        private const int SeedSourceYear = 2024;
        /// <summary>The year §518's band was taken in - the euro figures its two ratios were measured against.</summary>
        private const int OldGateYear = 2022;

        /// <summary>Eurostat `ilc_pnp3` 2024, sex T: the aggregate replacement ratio (`ElectionsData/pensions/replacement_ratio_2024.csv`, §475).</summary>
        private static readonly (CountryId Id, double Ratio)[] SourcedReplacementRatio =
        {
            (CountryId.Germany, 0.49), (CountryId.France, 0.61), (CountryId.Italy, 0.79), (CountryId.Poland, 0.60), (CountryId.Sweden, 0.59),
        };

        /// <summary>The verdicts §519 measured, source against source in the latest year both publish - held so a seed or a table that drifts trips the bar by name.</summary>
        private static readonly (CountryId Id, string Verdict)[] RecordedVerdicts =
        {
            (CountryId.Germany, "BELOW"), (CountryId.France, "BELOW"), (CountryId.Italy, "WITHIN"), (CountryId.Poland, "WITHIN"), (CountryId.Sweden, "BELOW"),
        };

        /// <summary>The year the game opens in - the seed world is 2026, the same year the pension row is filmed at.</summary>
        private const int SeedYear = 2026;

        private static readonly (CountryId Id, string Geo)[] Geo = { (CountryId.Germany, "DE"), (CountryId.France, "FR"), (CountryId.Italy, "IT"), (CountryId.Poland, "PL"), (CountryId.Sweden, "SE") };

        private static string F(string f, params object[] a) => string.Format(CultureInfo.InvariantCulture, f, a);

        /// <summary>One row of the sourced table. An ESSPROS field is NaN where that year is not yet published (the flags column names it).</summary>
        private sealed class SourceRow
        {
            public string Geo; public int Year;
            public double CofogPct, CofogMeur, EssprosOldPct, EssprosOldMeur, EssprosAllPct, EssprosAllMeur, GdpMeur, GdpMnac, BenOld, BenAll;
            public string Flags;
        }

        public static void Run()
        {
            CheckExit.ArmLogFold();
            var sb = new StringBuilder();
            sb.Append("=== PENSION PAYMENT (PN-2, PN-4): the line over the people the statute retires; the seed against its source SAME YEAR, share against share ===\n");
            bool ok = true;

            SimulationRandom.Seed(777);
            EnergyMarket.ResetCalibration();
            World world = WorldFactory.CreateDefault();
            EnergyMarket.BeginTurn(world);
            CountryId[] order = { CountryId.Sweden, CountryId.Germany, CountryId.France, CountryId.Italy, CountryId.Poland, CountryId.USA };

            Dictionary<string, List<SourceRow>> table = ReadTable(ref ok);

            sb.Append("\n    1. THE HEADCOUNT the statute's age implies - the bands at or above it, the straddled band by its fraction (uniform within the cohort)\n");
            foreach (CountryId id in order)
            {
                Country c = world.GetCountry(id);
                float age = PensionAgeStatute.AgeInForce(id, SeedYear);
                double head = PensionPayment.PensionersMillions(c, age); double straddleFraction = StraddleFraction(c, age, out int straddleBand);
                sb.Append(F("    {0,-8} age {1:F2} · band {2} takes {3:P1} of itself · pensioners {4:N3} m of {5:N3} m people ({6:P1})\n",
                    id, age, straddleBand, straddleFraction, head, TotalMillions(c), TotalMillions(c) > 0 ? head / TotalMillions(c) : 0));
            }

            sb.Append("\n    2. THE BENEFIT the line implies - the pension line over that headcount, in the book's dollars and in the statute's currency; the line as its share of the seed's nominal GDP\n");
            foreach (CountryId id in order)
            {
                Country c = world.GetCountry(id);
                float age = PensionAgeStatute.AgeInForce(id, SeedYear);
                double head = PensionPayment.PensionersMillions(c, age);
                double line = PensionPayment.LineBillions(c);
                double perYearUsd = head > 0 ? line * 1e9 / (head * 1e6) : 0;   // billions over millions = thousands; × 1000 = the person's own figure
                double nationalPerUsd = EnergyLayer.NationalPerUsd(id);
                double perYearNational = perYearUsd * nationalPerUsd;
                sb.Append(F("    {0,-8} line {1:N1} bn = {2:F3} % of GDP {3:N0} · {4:N3} m pensioners · benefit {5:N0} USD/yr = {6:N0} {7}/yr\n",
                    id, line, SharePct(c), c.State.NominalGdp, head, perYearUsd, perYearNational, EnergyLayer.CurrencyCode(id)));
            }

            // 3. THE GATE, re-cut (PN-4). Two comparisons, both like with like:
            //   (a) PROVENANCE - the seed's share of nominal GDP against the COFOG GF10.02/D62 share of the seed's own year, which is what the line was typed
            //       from. This is the tight guard: a seed that drifts from its source by a hundredth of a point is named.
            //   (b) THE INDEPENDENT SOURCE, SAME YEAR - that COFOG share against ESSPROS's two definitions (old-age pensions .. all pension types) for the
            //       latest year BOTH publish. ESSPROS counts every scheme and COFOG general government alone, so the difference between ESSPROS's old-age
            //       figure and COFOG's is the PERIMETER - old-age pensions paid outside the state - and it is printed with its sign, not read as an error.
            //       What CAN be a verdict regardless of perimeter is the upper edge: general government's old-age cash cannot exceed every scheme's every
            //       pension type, so a seed above ESSPROS's all-types figure is wrong in any perimeter. That bound is asserted; the old-age edge is read.
            sb.Append(F("\n    3. THE SEED AGAINST ITS SOURCE - the share of GDP against COFOG GF10.02/D62 {0} (provenance), and that against ESSPROS's own two definitions for the same year (the perimeter named)\n", SeedSourceYear));
            int within = 0, below = 0, above = 0, unsourced = 0;
            var verdicts = new Dictionary<CountryId, string>();
            foreach (CountryId id in order)
            {
                Country c = world.GetCountry(id);
                string geo = GeoOf(id);
                if (geo == null || table == null || !table.ContainsKey(geo)) { unsourced++; sb.Append(F("    {0,-8} NO SOURCE ROW - the table does not carry this country (ESSPROS and COFOG are EU collections); no verdict is formed\n", id)); continue; }
                SourceRow seedRow = RowOf(table, geo, SeedSourceYear);
                SourceRow same = LatestWithEsspros(table, geo);
                double share = SharePct(c);
                double provenance = share - seedRow.CofogPct;
                string verdict = same.CofogPct < same.EssprosOldPct ? "BELOW" : same.CofogPct > same.EssprosAllPct ? "ABOVE" : "WITHIN";
                if (verdict == "WITHIN") { within++; } else if (verdict == "BELOW") { below++; } else { above++; }
                verdicts[id] = verdict;
                double perimeter = same.EssprosOldPct - same.CofogPct;
                sb.Append(F("    {0,-8} seed {1:F3} % against COFOG {2} {3:F1} % (Δ {4:+0.000;-0.000} pp) · {5}: COFOG {6:F1} % against ESSPROS old-age {7:F2} .. all types {8:F2} % · {9} · perimeter {10:+0.00;-0.00} pp {11}\n",
                    id, share, SeedSourceYear, seedRow.CofogPct, provenance, same.Year, same.CofogPct, same.EssprosOldPct, same.EssprosAllPct, verdict, perimeter,
                    perimeter > 0 ? "(old-age pensions ESSPROS counts that general government does not pay)" : "(old-age cash COFOG counts that ESSPROS files elsewhere)"));
                if (Math.Abs(provenance) > 0.01)
                {
                    ok = false;
                    Debug.LogError(F("PENSION PAYMENT: {0}'s pension line is {1:F3} % of the seed's GDP against the {2:F1} % COFOG GF10.02/D62 {3} it was typed from - the seed does not land on its own source.", id, share, seedRow.CofogPct, SeedSourceYear));
                }
                if (share > same.EssprosAllPct + 0.005)
                {
                    ok = false;
                    Debug.LogError(F("PENSION PAYMENT: {0}'s pension line at {1:F3} % of GDP exceeds ESSPROS's all-pension-types figure {2:F2} % ({3}) - general government's old-age cash cannot exceed every scheme's every pension type; the seed is wrong in any perimeter.", id, share, same.EssprosAllPct, same.Year));
                }
            }
            sb.Append(F("    {0} WITHIN the same year's band, {1} BELOW its old-age edge by the perimeter, {2} ABOVE its all-types edge, {3} unsourced.\n", within, below, above, unsourced));
            foreach ((CountryId id, string recorded) in RecordedVerdicts)
            {
                if (verdicts.TryGetValue(id, out string v) && v != recorded)
                {
                    ok = false;
                    Debug.LogError(F("PENSION PAYMENT: {0} reads {1} against the same year's ESSPROS band where §519 measured it {2} - a seed moved, or the table did, and the record's verdict is stale.", id, v, recorded));
                }
            }

            sb.Append("\n    4. THE DRIVER'S FACTOR - the cohorts at or above the age against ESSPROS's beneficiaries (every scheme, every pension type counted once) - a factor on the BENEFIT, never on the line\n");
            foreach (CountryId id in order)
            {
                string geo = GeoOf(id); if (geo == null || table == null || !table.ContainsKey(geo)) { continue; }
                Country c = world.GetCountry(id);
                SourceRow same = LatestWithEsspros(table, geo);
                double head = PensionPayment.PensionersMillions(c, PensionAgeStatute.AgeInForce(id, SeedYear));
                sb.Append(F("    {0,-8} heads {1:N3} m ({2}) against {3:N3} m old-age beneficiaries (x{4:F3}) and {5:N3} m of all types (x{6:F3}) in {7}\n",
                    id, head, SeedYear, same.BenOld / 1e6, head / (same.BenOld / 1e6), same.BenAll / 1e6, head / (same.BenAll / 1e6), same.Year));
            }

            // 5. §518's TWO FIGURES, DECOMPOSED. Its ratio was (the line in euro at the model's rate) over (the ESSPROS 2022 edge in euro), which factors exactly as
            //    YEAR (the model's GDP in euro over the source year's GDP in euro) × VINTAGE (the seed's COFOG share over COFOG's share of the source year - the line's
            //    own indexation between the two years) × PERIMETER (COFOG's share of the source year over ESSPROS's edge share of the same year) × SEED (the seed's
            //    share over the COFOG share it was typed from - 1 when the seed lands on its source). The product must reproduce the direct ratio to the third place.
            double eurPerUsd = EnergyLayer.NationalPerUsd(CountryId.Germany);   // the book is in dollars, the source in euro; Germany's rate IS the euro's
            sb.Append(F("\n    5. §518'S TWO FIGURES DECOMPOSED - line / ESSPROS {0} edge in euro = year × vintage × perimeter × seed (1 USD = {1:F4} EUR)\n", OldGateYear, eurPerUsd));
            var products = new Dictionary<CountryId, double>();
            foreach ((CountryId id, bool againstAllTypes) in new[] { (CountryId.Poland, true), (CountryId.Sweden, false) })
            {
                string geo = GeoOf(id); if (table == null || !table.ContainsKey(geo)) { continue; }
                Country c = world.GetCountry(id);
                SourceRow old = RowOf(table, geo, OldGateYear), seedRow = RowOf(table, geo, SeedSourceYear);
                double modelGdpEur = c.State.NominalGdp * eurPerUsd;                       // bn
                double year = modelGdpEur / (old.GdpMeur / 1000.0);
                double vintage = seedRow.CofogPct / old.CofogPct;
                double edgePct = againstAllTypes ? old.EssprosAllPct : old.EssprosOldPct;
                double edgeMeur = againstAllTypes ? old.EssprosAllMeur : old.EssprosOldMeur;
                double perimeter = old.CofogPct / edgePct;
                double seed = SharePct(c) / seedRow.CofogPct;
                double product = year * vintage * perimeter * seed;
                double direct = PensionPayment.LineBillions(c) * eurPerUsd / (edgeMeur / 1000.0);
                products[id] = product;
                sb.Append(F("    {0,-8} year {1:F3} ({2:N1} bn EUR model GDP over {3:N1} bn {4}) × vintage {5:F3} (COFOG {6:F1} over {7:F1}) × perimeter {8:F3} (COFOG {9:F1} over ESSPROS {10} {11:F2}) × seed {12:F3} = {13:F3} · direct {14:F3}\n",
                    id, year, modelGdpEur, old.GdpMeur / 1000.0, OldGateYear, vintage, seedRow.CofogPct, old.CofogPct, perimeter, old.CofogPct, againstAllTypes ? "all types" : "old-age", edgePct, seed, product, direct));
                if (Math.Abs(product - direct) > 0.002)
                {
                    ok = false;
                    Debug.LogError(F("PENSION PAYMENT: {0}'s decomposition multiplies to {1:F3} against the direct ratio {2:F3} - the four factors no longer account for the figure.", id, product, direct));
                }
            }
            // the two figures the records carry: Poland's ×1.209 (§518, untouched by this pass - the seed is inside the same year's band and stays) and Sweden's
            // product with its seed landed on the source (§519); §518's 0.724 was this times the slip, and the slip is measured by the probe that turns the landing off
            AssertProduct(products, CountryId.Poland, 1.209, "§518", ref ok);
            AssertProduct(products, CountryId.Sweden, 0.739, "§519", ref ok);

            sb.Append("\n    6. THE REPLACEMENT RATE - the benefit over the mean income the schedules read, against Eurostat's aggregate replacement ratio\n");
            foreach ((CountryId id, double ratio) in SourcedReplacementRatio)
            {
                Country c = world.GetCountry(id);
                double perYearUsd = PensionPayment.AverageBenefitPerYear(c, SeedYear);
                double meanIncomeStatute = TaxSchedule.AverageIncome(c, 1.0);            // the statute's own currency, the schedules' reading
                double meanIncomeUsd = meanIncomeStatute / Math.Max(1e-9, EnergyLayer.NationalPerUsd(id));
                double model = meanIncomeUsd > 0 ? perYearUsd / meanIncomeUsd : 0;
                sb.Append(F("    {0,-8} benefit {1:N0} USD/yr over mean income {2:N0} USD/yr = {3:F3} against ilc_pnp3 {4:F2} · ratio {5:F3}\n",
                    id, perYearUsd, meanIncomeUsd, model, ratio, ratio > 0 ? model / ratio : 0));
            }
            sb.Append("\n    ⚠ THE REPLACEMENT RATE IS A READING, NOT A GATE: ilc_pnp3 is a median individual pension over median earnings of a named age band, and this is a\n");
            sb.Append("    mean over a mean, so the two cannot be equal even where the model is right. It is printed because a reader will ask, and the gap is named with it.\n");

            // 7. PN-1's DRIVER (§520): the pension line's driver IS the payment's headcount - one set of people, read through one accessor - and the statute's own
            //    path moves it. The USA's federal-retirement and veterans' lines are other systems and stay on the 65+ cohort.
            sb.Append("\n    7. THE DRIVER (PN-1, §520) - the pension line's driver against the payment's headcount at the age in force, and what the statute's own path does to the same pyramid\n");
            if (SpendingDrivers.Of(SpendingCategory.SocialSecurity) != SpendingDriver.StatutoryPensionAge)
            {
                ok = false;
                Debug.LogError("PENSION PAYMENT: the pension line's driver is not the statutory-age cohort - the line follows the 65+ cohort where the statute retires people at its own age.");
            }
            if (SpendingDrivers.Of(SpendingCategory.FederalRetirement) != SpendingDriver.Elderly65Plus || SpendingDrivers.Of(SpendingCategory.VeteransBenefitsMandatory) != SpendingDriver.Elderly65Plus)
            {
                ok = false;
                Debug.LogError("PENSION PAYMENT: the USA's federal-retirement or veterans' line follows the statutory pension age - those are other systems and stay on the 65+ cohort.");
            }
            foreach (CountryId id in order)
            {
                Country c = world.GetCountry(id);
                if (c.CalendarYear != SeedYear) { ok = false; Debug.LogError(F("PENSION PAYMENT: {0}'s calendar year reads {1} at the seed, not {2}.", id, c.CalendarYear, SeedYear)); }
                float ageNow = PensionAgeStatute.AgeInForce(id, c.CalendarYear);
                float driverLevel = SpendingDrivers.Level(SpendingDrivers.Of(SpendingCategory.SocialSecurity), c);
                double heads = PensionPayment.PensionersMillions(c, ageNow);
                float sixtyFive = SpendingDrivers.Level(SpendingDriver.Elderly65Plus, c);
                PensionAgeStatute.Rule rule = PensionAgeStatute.Of(id);
                float endAge = rule.Path[rule.Path.Length - 1].Age; int endYear = rule.Path[rule.Path.Length - 1].Year;
                double atEnd = PensionPayment.PensionersMillions(c, endAge);   // the path's last age on the seed's own pyramid - what a rising age does to the line, the pyramid held still
                sb.Append(F("    {0,-8} driver {1:N3} m against the payment's heads {2:N3} m at {3} (age {4}) · the 65+ cohort {5:N3} m (x{6:F3}) · the path's end {7} in {8}: {9:N3} m on the same pyramid (x{10:F3})\n",
                    id, driverLevel, heads, c.CalendarYear, PensionAgeStatute.Format(ageNow), sixtyFive, sixtyFive > 0 ? driverLevel / sixtyFive : 0, PensionAgeStatute.Format(endAge), endYear, atEnd, heads > 0 ? atEnd / heads : 0));
                if (Math.Abs(driverLevel - heads) > 1e-4 * Math.Max(1.0, heads))
                {
                    ok = false;
                    Debug.LogError(F("PENSION PAYMENT: {0}'s pension driver reads {1:N4} m against the payment's {2:N4} m at the age in force - the line and the readout count two sets of people.", id, driverLevel, heads));
                }
            }

            // 8. THE YEAR THE DRIVER READS IS THE CLOCK'S: a manager advanced one turn commits its year to every country, and the index at the boundary takes the new year's headcount
            sb.Append("\n    8. THE CLOCK'S YEAR - a manager advanced one turn: every country's calendar year is the clock's, and the pension line's driver reference is the new year's headcount\n");
            var go = new GameObject("PensionPaymentDiagnostic");
            try
            {
                SimulationRandom.Seed(777);
                EnergyMarket.ResetCalibration();
                World advanced = WorldFactory.CreateDefault();
                SimulationManager sim = go.AddComponent<SimulationManager>();
                sim.SetWorld(advanced);
                var decisions = new Dictionary<CountryId, PolicyDecision>();
                foreach (Country c in advanced.Countries) { decisions[c.Id] = PolicyDecision.None(); }
                for (int d = 0; d < SimulationManager.DaysPerTurn; d++) { sim.AdvanceDay(); }
                sim.AdvanceTurn(decisions);
                int clockYear = sim.CurrentDate.Year;
                foreach (Country c in advanced.Countries)
                {
                    SpendingLine pension = null;
                    foreach (SpendingLine l in c.SpendingLines) { if (l.Category == SpendingCategory.SocialSecurity) { pension = l; } }
                    float expected = SpendingDrivers.Level(SpendingDriver.StatutoryPensionAge, c);
                    sb.Append(F("    {0,-8} clock {1} · country {2} · age in force {3} · driver reference {4:N3} m against the level now {5:N3} m · the year's ratio x{6:F4}\n",
                        c.Id, clockYear, c.CalendarYear, PensionAgeStatute.Format(PensionAgeStatute.AgeInForce(c.Id, c.CalendarYear)), pension != null ? pension.DriverReference : 0f, expected, pension != null ? pension.LastDriverRatio : 0f));
                    if (c.CalendarYear != clockYear)
                    {
                        ok = false;
                        Debug.LogError(F("PENSION PAYMENT: {0}'s calendar year reads {1} after a turn while the clock reads {2} - the driver reads a year the turn is not in.", c.Id, c.CalendarYear, clockYear));
                    }
                    if (pension != null && Math.Abs(pension.DriverReference - expected) > 1e-4f * Math.Max(1f, expected))
                    {
                        ok = false;
                        Debug.LogError(F("PENSION PAYMENT: {0}'s pension line's driver reference {1:N4} m is not the statutory cohort at the clock's year, {2:N4} m.", c.Id, pension.DriverReference, expected));
                    }
                }
            }
            finally { UnityEngine.Object.DestroyImmediate(go); }

            // the identity the row and the gate both stand on: the benefit IS the line over the heads
            foreach (CountryId id in order)
            {
                Country c = world.GetCountry(id);
                if (!PensionAgeStatute.Has(id)) { continue; }
                double heads = PensionPayment.PensionersMillions(c, PensionAgeStatute.AgeInForce(id, SeedYear));
                double benefit = PensionPayment.AverageBenefitPerYear(c, SeedYear);
                double implied = heads > 0 ? benefit * heads * 1e6 / 1e9 : 0;
                if (Math.Abs(implied - PensionPayment.LineBillions(c)) > 0.01)
                {
                    ok = false;
                    Debug.LogError(F("PENSION PAYMENT: {0}'s benefit × heads reads {1:N3} bn against the line's {2:N3} bn - the readout is not the line over the people it names.", id, implied, PensionPayment.LineBillions(c)));
                }
            }

            Debug.Log(sb.ToString());
            Debug.Log(ok ? "=== PensionPaymentDiagnostic: ALL ASSERTIONS PASS ===" : "=== PensionPaymentDiagnostic: FAILED ===");
            CheckExit.Finish(ok ? 0 : 1);
        }

        /// <summary>The pension line as a share of the seed's nominal GDP, in percent - the quantity the seed was typed as.</summary>
        private static double SharePct(Country c) => c.State.NominalGdp > 0 ? PensionPayment.LineBillions(c) / c.State.NominalGdp * 100.0 : 0;

        private static string GeoOf(CountryId id) { foreach ((CountryId i, string g) in Geo) { if (i == id) { return g; } } return null; }

        private static SourceRow RowOf(Dictionary<string, List<SourceRow>> table, string geo, int year)
        {
            foreach (SourceRow r in table[geo]) { if (r.Year == year) { return r; } }
            throw new InvalidOperationException(F("{0} has no {1} row in {2}", geo, year, SourceRelative));
        }

        /// <summary>The latest year the table carries both sources for - ESSPROS lags COFOG by a year for some countries, and the comparison is only ever same-year.</summary>
        private static SourceRow LatestWithEsspros(Dictionary<string, List<SourceRow>> table, string geo)
        {
            SourceRow best = null;
            foreach (SourceRow r in table[geo]) { if (!double.IsNaN(r.EssprosOldPct) && (best == null || r.Year > best.Year)) { best = r; } }
            if (best == null) { throw new InvalidOperationException(F("{0} has no year with ESSPROS published in {1}", geo, SourceRelative)); }
            return best;
        }

        /// <summary>A recorded figure held: the decomposition's product for a country, at the figure the named record measured.</summary>
        private static void AssertProduct(Dictionary<CountryId, double> products, CountryId id, double recorded, string record, ref bool ok)
        {
            if (!products.TryGetValue(id, out double product)) { return; }
            if (Math.Abs(product - recorded) > 0.005)
            {
                ok = false;
                Debug.LogError(F("PENSION PAYMENT: {0}'s line sits x{1:F3} from ESSPROS's {2} edge against the x{3:F3} {4} measured - the gap moved and the record's figure is stale.", id, product, OldGateYear, recorded, record));
            }
        }

        /// <summary>The sourced table, by geo. Absent file: a failure - the gate is what the table is on disk for.</summary>
        private static Dictionary<string, List<SourceRow>> ReadTable(ref bool ok)
        {
            string path = Path.Combine(Directory.GetCurrentDirectory(), SourceRelative.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(path)) { ok = false; Debug.LogError(F("PENSION PAYMENT: {0} is not on disk - the seed cannot be held against its source.", SourceRelative)); return null; }
            var table = new Dictionary<string, List<SourceRow>>();
            string[] lines = File.ReadAllLines(path);
            for (int i = 1; i < lines.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(lines[i])) { continue; }
                List<string> f = SplitCsv(lines[i]);
                if (f.Count < 15) { ok = false; Debug.LogError(F("PENSION PAYMENT: row {0} of {1} has {2} fields, 15 expected.", i + 1, SourceRelative, f.Count)); continue; }
                var r = new SourceRow
                {
                    Geo = f[0], Year = int.Parse(f[2], CultureInfo.InvariantCulture),
                    CofogPct = Num(f[3]), CofogMeur = Num(f[4]), EssprosOldPct = Num(f[5]), EssprosOldMeur = Num(f[6]), EssprosAllPct = Num(f[7]), EssprosAllMeur = Num(f[8]),
                    GdpMeur = Num(f[9]), GdpMnac = Num(f[10]), BenOld = Num(f[11]), BenAll = Num(f[12]), Flags = f[13],
                };
                if (!table.TryGetValue(r.Geo, out List<SourceRow> rows)) { rows = new List<SourceRow>(); table[r.Geo] = rows; }
                rows.Add(r);
            }
            return table;
        }

        private static double Num(string s) => string.IsNullOrEmpty(s) ? double.NaN : double.Parse(s, CultureInfo.InvariantCulture);

        /// <summary>A CSV line into fields - a quoted field may carry commas and doubled quotes (the flags and source columns do).</summary>
        private static List<string> SplitCsv(string line)
        {
            var fields = new List<string>(); var cur = new StringBuilder(); bool quoted = false;
            for (int i = 0; i < line.Length; i++)
            {
                char ch = line[i];
                if (quoted)
                {
                    if (ch == '"') { if (i + 1 < line.Length && line[i + 1] == '"') { cur.Append('"'); i++; } else { quoted = false; } }
                    else { cur.Append(ch); }
                }
                else if (ch == '"') { quoted = true; }
                else if (ch == ',') { fields.Add(cur.ToString()); cur.Clear(); }
                else { cur.Append(ch); }
            }
            fields.Add(cur.ToString());
            return fields;
        }

        private static double TotalMillions(Country c)
        {
            double t = 0;
            if (c.Cohorts?.Counts != null) { foreach (float n in c.Cohorts.Counts) { t += n; } }
            return t;
        }

        /// <summary>Which band the statutory age falls inside and how much of it the headcount takes - the sub-band assumption, printed so it is read
        /// rather than assumed. The headcount itself is <see cref="PensionPayment.PensionersMillions"/>'s, which the row on the screen reads too.</summary>
        private static double StraddleFraction(Country c, float age, out int straddleBand)
        {
            straddleBand = -1;
            if (c?.Cohorts?.Counts == null) { return 0; }
            for (int b = 0; b < PopulationCohorts.OpenBandIndex; b++)
            {
                int start = b * PopulationCohorts.CohortWidth, end = start + PopulationCohorts.CohortWidth;
                if (age > start && age < end) { straddleBand = b; return (end - age) / (double)PopulationCohorts.CohortWidth; }
            }
            return 0;
        }
    }
}
