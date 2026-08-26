using System.Collections.Generic;

namespace PoliSim.Data
{
    /// <summary>
    /// Builds the default six-country scenario: USA, Sweden, Germany, France, Italy, Poland,
    /// with Germany/France/Italy sharing one Eurozone CurrencyZone and Germany/France/Italy/
    /// Sweden/Poland forming the EU trade bloc (Sweden and Poland are EU members but keep their
    /// own currency, same as in reality).
    ///
    /// Policy rates, inflation, and the figures the user specified (USA/Poland unemployment,
    /// USA/Eurozone/Sweden/Poland growth) are seeded to real mid-2026 data. NAIRU, unspecified
    /// unemployment rates, government-spending shares, and starting GDP levels are stylized,
    /// directionally-realistic estimates for flavor, not researched figures - see inline comments.
    /// Starting debt-to-GDP ratios are seeded to real approximate figures (USA ~124%, Italy ~138%,
    /// France ~116%, Germany ~63%, Poland ~59%, Sweden ~35%); BenefitRatePerUnemployed is a stylized
    /// estimate of welfare-state generosity, not a researched figure. Each country's starting
    /// TaxLine portfolio (see SeedTaxLines) uses real approximate 2026 headline rates for its
    /// active taxes; "modest/inactive" lines share one uniform illustrative placeholder rate across
    /// all six countries for simplicity - not researched figures either.
    ///
    /// Each country's CollectionEfficiency is solved for so that its DEFAULT tax portfolio's actual
    /// revenue-to-GDP (theoretical revenue-to-GDP * CollectionEfficiency) lands on that country's
    /// real-world tax-to-GDP target. RECALIBRATED (build-order item 1, terminal rulings 2026-08-26):
    /// the five EU targets re-anchored to ONE basis and vintage - Eurostat `gov_10a_taxag`, total
    /// receipts from taxes and net social contributions, % of GDP, 2024 (API vintage 2026-07-21;
    /// Germany carries Eurostat flag `p`, provisional):
    /// <code>
    /// Country   Implied (Rate*BaseShareOfGdp summed)   Target       CollectionEfficiency = Target/Implied
    /// USA       29.37%                                 18.0%        18.0 / 29.37 = 0.6129  (UNCHANGED)
    /// Germany   48.73%                                 40.9% [p]    40.9 / 48.73 = 0.8393
    /// France    60.45%                                 45.3%        45.3 / 60.45 = 0.7494
    /// Italy     45.10%                                 42.5%        42.5 / 45.10 = 0.9424
    /// Poland    42.10%                                 37.6%        37.6 / 42.10 = 0.8931
    /// Sweden    53.45%                                 42.2%        42.2 / 53.45 = 0.7895
    /// </code>
    /// (The pre-recalibration targets 38/45/43/37/41 were mixed-basis, mixed-vintage figures; the
    /// old table is in git history. USA is deliberately untouched - see the perimeter rule below.)
    ///
    /// THE PERIMETER RULE (stated 2026-08-26; the recalibration's organizing principle): a country's
    /// revenue target and its spending seed must sit on the SAME fiscal perimeter - taxing one
    /// perimeter and spending another is a seed bug, not a calibration choice. USA's target is
    /// FEDERAL-only revenue-to-GDP (~18%, real FY2025 federal revenue $5.235T against this game's
    /// ~$29,000B starting GDP; CBO FY2025: outlays 23.3% GDP, deficit 5.9%, net interest 3.2%,
    /// primary deficit ~2.7%) because the US state/local layer is not modeled - and its SPENDING
    /// seed (SeedUsaSpendingLines, real federal $B) is federal too, so the USA pair was already
    /// perimeter-consistent: measured year-1 primary balance -2.38% vs the real federal -2.7%
    /// (FiscalRecalDiagnostic, seed 777). The five EU countries sit on the GENERAL-GOVERNMENT
    /// perimeter on BOTH sides since this pass: general-government tax targets above, and the
    /// mandatory transfer block (SeedMandatoryTransferLines / Sweden's flipped UO lines) carrying
    /// the ~20%-of-GDP cash-transfer layer their spending seeds previously omitted entirely - the
    /// omission that produced measured year-1 primary surpluses of +14 to +22% of GDP (the item-4
    /// finding, re-measured at the harness before this pass changed anything) while the fiscal
    /// reaction multiplier was crushed to 0.58-0.76 papering over it. Year-1 primary balances now
    /// land on the real 2025 structural positions (Eurostat April-2026 EDP notification deficits;
    /// ECB GFS D.41 interest 2025-Q4: DE 1.10 / FR 2.20 / PL 2.51 / SE 0.61; IT ~3.9 derived from
    /// Eurostat 2025 expenditure shares): DE -1.6, FR -2.9, IT +0.8, PL -4.8, SE -0.7.
    /// The model books TAX revenue only (no non-tax revenue exists), so both sides sit ~5-7pp
    /// below the real general-government totals; THE PRIMARY BALANCE is the anchored quantity.
    ///
    /// "Implied" is the same figure flagged in this class's earlier revision as running higher than
    /// real-world tax burdens for some countries (Sweden, France) once BaseShareOfGdp weights were
    /// combined with real headline rates; CollectionEfficiency (modeling enforcement quality/the
    /// informal economy/evasion, not a researched figure itself) corrects the DEFAULT portfolio back
    /// down to each country's real tax-to-GDP target without changing any seeded rate.
    ///
    /// LaborForceParticipationRate/BaselineLaborForceParticipationRate use real World Bank/OECD
    /// "total population ages 15+" figures per country; MinimumWagePercentOfMedian uses each
    /// country's real approximate Kaitz index (minimum wage as a percent of median wage) for the
    /// four countries that have a statutory minimum wage (USA/Germany/France/Poland) - Sweden and
    /// Italy have none in reality (sector-level collective bargaining instead) - see "Labor Market
    /// Basics" in CLAUDE.md.
    /// </summary>
    public static class WorldFactory
    {
        public static World CreateDefault()
        {
            var eurozone = new CurrencyZone("Eurozone", 2.25f);
            var usDollarZone = new CurrencyZone("US Dollar Zone", 3.75f);
            var swedishKronaZone = new CurrencyZone("Swedish Krona Zone", 1.75f);
            var polishZlotyZone = new CurrencyZone("Polish Zloty Zone", 3.75f);

            // GDP levels are illustrative relative scale (roughly proportional to real nominal
            // GDP), not precise figures - the sim treats them as abstract currency units.
            //
            // USA's PotentialGDP is seeded to 33260 (not left to default to GDP, unlike every other
            // country) - see "Turn-1 GDP Consistency" in CLAUDE.md. Once G was rebased to USA's real,
            // federal-only Discretionary total (~$1,751B - see "Detailed Spending Portfolio"), it no
            // longer summed with C+I to reproduce a 29000 GDP at a 0% output gap: the national
            // accounts identity's own C+I+G+NX evaluated against the 29000/0%-gap seed came out to
            // ~24600, and ApplyNationalAccounts' reversion-to-PotentialGDP then produced a real,
            // one-time ~9% GDP contraction (29000 -> ~26800) on literally the first turn of any new
            // game, before the "Discretionary Spending Growth" fix's -13%to-15% equilibrium gap had
            // even had a chance to develop gradually. 33260 is the empirically-solved PotentialGDP
            // seed (found via the standalone harness's --usapotgdp= sweep, not a closed-form solve,
            // since the turn-1 interest rate/output-gap/reversion chain has no simple closed form) at
            // which GDP=29000 is ALREADY at its own turn-1-consistent fixed point given this country's
            // C/I/G/NX figures - so turn 1 lands within +0.07% of 29000 (29000 -> ~28999-29019
            // depending on random events) instead of dropping ~9%, and the output gap is already at
            // its long-run ~-14.2%to-14.5% equilibrium from turn 1 onward rather than widening into it
            // over the first ~25 turns. This does NOT change USA's headline GDP figure (still 29000,
            // still real-approximate-nominal-GDP-scaled) or any other calibration built against it
            // (debt-to-GDP, CollectionEfficiency's tax-to-GDP targets, etc.) - only PotentialGDP, the
            // "trend output" reference value nothing else in WorldFactory reads, moves.
            var usa = new Country(
                CountryId.USA, "United States",
                new EconomyState(gdp: 29000f, inflation: 2.7f, unemployment: 4.5f, approvalRating: 50f, budget: 0f,
                    potentialGdp: 33260f, governmentDebt: 29000f * 1.24f, povertyRate: 18f, laborForceParticipationRate: 62.5f, crimeIndex: 45f, prisonPopulationRate: 531f, organizedCrimeIndex: 35f, corruptionIndex: 31f,
                    population: 341.8f, birthRate: 10.6f, deathRate: 9.1f, netMigrationRate: 3.7f, dependencyRatio: 28f),
                usDollarZone, baseTariffRate: 3f,
                naturalUnemploymentRate: 4.0f, potentialGrowthRate: 2.0f, governmentSpendingRate: 17f, benefitRatePerUnemployed: 0.10f);

            // Sweden's PotentialGDP is seeded to 614.25 (not left to default to GDP) - the
            // RECALIBRATION's ruled follow-up (terminal ruling 2026-08-26, "re-solve potential
            // now"): the ruled UO10/11/12 mandatory flip removed 5.6pp of GDP from the
            // national-accounts G term, and with potential left at 620 the identity opened a
            // persistent ~2-3% output gap at seed (measured: t1 growth +0.5% against 1.5%
            // potential growth). 614.25 is the empirically-solved seed (SwedenPotentialSolveDiagnostic,
            // two-stage sweep, seed 777 - the USA's own --usapotgdp idiom) at which GDP=620 is
            // ALREADY at its turn-1-consistent fixed point: t1 lands 619.99 (+0.005 error) and
            // t2/t3 grow ~1%/turn. Like the USA's 33260, this does NOT change Sweden's headline
            // GDP or any calibration built against it - only the trend-output reference moves.
            var sweden = new Country(
                CountryId.Sweden, "Sweden",
                new EconomyState(gdp: 620f, inflation: 2.0f, unemployment: 8.0f, approvalRating: 50f, budget: 0f,
                    potentialGdp: 614.25f, governmentDebt: 620f * 0.35f, povertyRate: 9f, laborForceParticipationRate: 72.6f, crimeIndex: 30f, prisonPopulationRate: 60f, organizedCrimeIndex: 32f, corruptionIndex: 18f,
                    population: 10.6f, birthRate: 10.8f, deathRate: 9.5f, netMigrationRate: 1.1f, dependencyRatio: 33f),
                swedishKronaZone, baseTariffRate: 1f,
                naturalUnemploymentRate: 6.5f, potentialGrowthRate: 1.5f, governmentSpendingRate: 26f, benefitRatePerUnemployed: 0.25f);

            var germany = new Country(
                CountryId.Germany, "Germany",
                new EconomyState(gdp: 4700f, inflation: 3.0f, unemployment: 3.5f, approvalRating: 50f, budget: 0f,
                    governmentDebt: 4700f * 0.63f, povertyRate: 11f, laborForceParticipationRate: 61.7f, crimeIndex: 25f, prisonPopulationRate: 72f, organizedCrimeIndex: 20f, corruptionIndex: 22f,
                    population: 83.6f, birthRate: 8.2f, deathRate: 12.2f, netMigrationRate: 1.8f, dependencyRatio: 35f),
                eurozone, baseTariffRate: 1f,
                naturalUnemploymentRate: 3.3f, potentialGrowthRate: 0.8f, governmentSpendingRate: 21f, benefitRatePerUnemployed: 0.20f);

            var france = new Country(
                CountryId.France, "France",
                new EconomyState(gdp: 3200f, inflation: 3.0f, unemployment: 7.3f, approvalRating: 50f, budget: 0f,
                    governmentDebt: 3200f * 1.16f, povertyRate: 8f, laborForceParticipationRate: 56.0f, crimeIndex: 30f, prisonPopulationRate: 111f, organizedCrimeIndex: 28f, corruptionIndex: 30f,
                    population: 69.1f, birthRate: 9.7f, deathRate: 9.5f, netMigrationRate: 1.1f, dependencyRatio: 33f),
                eurozone, baseTariffRate: 1f,
                naturalUnemploymentRate: 7.5f, potentialGrowthRate: 0.8f, governmentSpendingRate: 24f, benefitRatePerUnemployed: 0.22f);

            var italy = new Country(
                CountryId.Italy, "Italy",
                new EconomyState(gdp: 2300f, inflation: 3.0f, unemployment: 7.8f, approvalRating: 50f, budget: 0f,
                    governmentDebt: 2300f * 1.38f, povertyRate: 14f, laborForceParticipationRate: 49.8f, crimeIndex: 18f, prisonPopulationRate: 92f, organizedCrimeIndex: 55f, corruptionIndex: 44f,
                    population: 58.9f, birthRate: 6.3f, deathRate: 10.4f, netMigrationRate: 1.3f, dependencyRatio: 40f),
                eurozone, baseTariffRate: 1f,
                naturalUnemploymentRate: 8.0f, potentialGrowthRate: 0.8f, governmentSpendingRate: 19f, benefitRatePerUnemployed: 0.18f);

            var poland = new Country(
                CountryId.Poland, "Poland",
                new EconomyState(gdp: 840f, inflation: 2.2f, unemployment: 5.4f, approvalRating: 50f, budget: 0f,
                    governmentDebt: 840f * 0.59f, povertyRate: 10f, laborForceParticipationRate: 58.5f, crimeIndex: 20f, prisonPopulationRate: 185f, organizedCrimeIndex: 22f, corruptionIndex: 40f,
                    population: 37.5f, birthRate: 6.7f, deathRate: 10.9f, netMigrationRate: 0.2f, dependencyRatio: 28f),
                polishZlotyZone, baseTariffRate: 1f,
                naturalUnemploymentRate: 5.0f, potentialGrowthRate: 3.5f, governmentSpendingRate: 18f, benefitRatePerUnemployed: 0.12f);

            // BasePotentialGrowthRate (see "Infrastructure Feedback" in CLAUDE.md) - the immutable
            // structural anchor PotentialGrowthRate is now recomputed from each turn, seeded equal to
            // each country's own real potentialGrowthRate constructor argument above (not a
            // constructor parameter itself, since ClonePreviewCountry passes the CURRENT, possibly-
            // already-adjusted PotentialGrowthRate into that same constructor parameter for a preview
            // clone - deriving BasePotentialGrowthRate from it there would wrongly re-baseline the
            // preview to whatever the real country's adjusted rate happens to be that turn).
            usa.BasePotentialGrowthRate = usa.PotentialGrowthRate;
            sweden.BasePotentialGrowthRate = sweden.PotentialGrowthRate;
            germany.BasePotentialGrowthRate = germany.PotentialGrowthRate;
            france.BasePotentialGrowthRate = france.PotentialGrowthRate;
            italy.BasePotentialGrowthRate = italy.PotentialGrowthRate;
            poland.BasePotentialGrowthRate = poland.PotentialGrowthRate;

            SeedTaxLines(usa, incomeTax: 37f, corporateTax: 21f, vat: 0f, vatImplemented: false,
                payrollTax: 15.3f, capitalGainsTax: 20f, salesTax: 7f, salesTaxImplemented: true,
                estateTax: 40f, estateTaxImplemented: true, carbonTax: 5f, carbonTaxImplemented: false);
            SeedTaxLines(germany, incomeTax: 45f, corporateTax: 30f, vat: 19f, vatImplemented: true,
                payrollTax: 38.7f, capitalGainsTax: 25f, salesTax: 0f, salesTaxImplemented: false,
                estateTax: 20f, estateTaxImplemented: false, carbonTax: 5f, carbonTaxImplemented: false);
            SeedTaxLines(france, incomeTax: 45f, corporateTax: 25f, vat: 20f, vatImplemented: true,
                payrollTax: 68f, capitalGainsTax: 30f, salesTax: 0f, salesTaxImplemented: false,
                estateTax: 20f, estateTaxImplemented: false, carbonTax: 5f, carbonTaxImplemented: false);
            SeedTaxLines(italy, incomeTax: 43f, corporateTax: 24f, vat: 22f, vatImplemented: true,
                payrollTax: 30f, capitalGainsTax: 26f, salesTax: 0f, salesTaxImplemented: false,
                estateTax: 20f, estateTaxImplemented: false, carbonTax: 5f, carbonTaxImplemented: false);
            SeedTaxLines(poland, incomeTax: 32f, corporateTax: 19f, vat: 23f, vatImplemented: true,
                payrollTax: 35f, capitalGainsTax: 19f, salesTax: 0f, salesTaxImplemented: false,
                estateTax: 20f, estateTaxImplemented: false, carbonTax: 5f, carbonTaxImplemented: false);
            SeedTaxLines(sweden, incomeTax: 52f, corporateTax: 20.6f, vat: 25f, vatImplemented: true,
                payrollTax: 31.4f, capitalGainsTax: 30f, salesTax: 0f, salesTaxImplemented: false,
                estateTax: 20f, estateTaxImplemented: false, carbonTax: 30f, carbonTaxImplemented: true);

            // CollectionEfficiency = target real-world tax-to-GDP / implied revenue-to-GDP from the
            // default portfolio above (sum of Rate * BaseShareOfGdp over implemented lines) - see
            // this class's doc comment for the full per-country derivation table. RECALIBRATED
            // 2026-08-26 (build-order item 1): the five EU targets re-anchored to Eurostat
            // gov_10a_taxag 2024 (one basis, one vintage - API 2026-07-21); USA unchanged
            // (federal perimeter, already consistent - the doc comment's perimeter rule).
            usa.CollectionEfficiency = 0.6129f;    // 18.0 / 29.37 (federal-only target, not general-government)
            germany.CollectionEfficiency = 0.8393f; // 40.9 [Eurostat flag p] / 48.73
            france.CollectionEfficiency = 0.7494f;  // 45.3 / 60.45
            italy.CollectionEfficiency = 0.9424f;   // 42.5 / 45.10
            poland.CollectionEfficiency = 0.8931f;  // 37.6 / 42.10
            sweden.CollectionEfficiency = 0.7895f;  // 42.2 / 53.45

            // Fiscal reaction function's per-country comfort anchor (see "Fiscal Reaction Function" in
            // CLAUDE.md) - reuses each country's own seeded starting debt-to-GDP ratio from the
            // EconomyState constructions above (governmentDebt: gdp * X), not a separately-researched
            // figure. A country's own fiscal culture/history is a reasonable proxy for what debt level
            // it's institutionally comfortable running.
            usa.ComfortableDebtToGdpPercent = 124f;
            germany.ComfortableDebtToGdpPercent = 63f;
            france.ComfortableDebtToGdpPercent = 116f;
            italy.ComfortableDebtToGdpPercent = 138f;
            poland.ComfortableDebtToGdpPercent = 59f;
            sweden.ComfortableDebtToGdpPercent = 35f;

            // MacroSystem.ApplyPovertyRate's per-country structural anchor - the SAME real OECD
            // relative-poverty-rate figures EconomyState.PovertyRate was just seeded to above, so a
            // new game opens with PovertyRate already at (or very near) its own baseline rather than
            // an artificial turn-1 jump (see Country.BaselinePovertyRate's doc comment).
            usa.BaselinePovertyRate = 18f;
            germany.BaselinePovertyRate = 11f;
            france.BaselinePovertyRate = 8f;
            italy.BaselinePovertyRate = 14f;
            poland.BaselinePovertyRate = 10f;
            sweden.BaselinePovertyRate = 9f;

            // MacroSystem.ApplyLaborForceParticipationRate's per-country structural anchor - the SAME
            // real World Bank/OECD "total population ages 15+" figures EconomyState.
            // LaborForceParticipationRate was just seeded to above, so a new game opens already at (or
            // very near) its own baseline (see "Labor Market Basics" in CLAUDE.md).
            usa.BaselineLaborForceParticipationRate = 62.5f;
            germany.BaselineLaborForceParticipationRate = 61.7f;
            france.BaselineLaborForceParticipationRate = 56.0f;
            italy.BaselineLaborForceParticipationRate = 49.8f;
            poland.BaselineLaborForceParticipationRate = 58.5f;
            sweden.BaselineLaborForceParticipationRate = 72.6f;

            // ROUND 4 BATCH 1 (C3): youth unemployment, RULED SEED = the Feb 2026 cross-section, one
            // period, all six (Elias, 2026-08-02; seed doc §3). ALL RATES (% of the youth labour
            // force), the rate/ratio trap closed at source by construction (`unit=PC_ACT`).
            // Per-country basis: the EU five are Eurostat API `une_rt_m`, age Y_LT25 (15-24),
            // seasonally adjusted, retrieved 2026-08-02; the USA is BLS CPS `LNS14024887` via FRED,
            // **16-24 bracket** (the OECD-harmonised US equivalent - there IS no 15-24 US series;
            // never "correct" this), rate, SA, same retrieval date. France ABOVE Italy is real on
            // this vintage (the Feb 2026 reversal, recorded in the seed doc); Sweden-highest and
            // Germany-lowest hold. States seeded equal to baselines in the loop below - zero gap.
            usa.BaselineYouthUnemploymentRate = 9.5f;      // [VERIFIED] BLS CPS, 16-24
            germany.BaselineYouthUnemploymentRate = 7.3f;  // [VERIFIED] Eurostat SA rate
            france.BaselineYouthUnemploymentRate = 21.1f;  // [VERIFIED] Eurostat SA rate
            italy.BaselineYouthUnemploymentRate = 17.7f;   // [VERIFIED] Eurostat SA rate
            poland.BaselineYouthUnemploymentRate = 11.9f;  // [VERIFIED] Eurostat SA rate
            sweden.BaselineYouthUnemploymentRate = 22.5f;  // [VERIFIED] Eurostat SA rate - genuinely Europe's worst alongside a strong overall market

            // ROUND 4 BATCH 1 (C3): life expectancy at birth, 2024 figures (seed doc §4 as corrected
            // 2026-08-02 from the PRIMARY sources after the 84.1 verified-but-wrong incident).
            // ⚠ NOT 6/6 VERIFIED, and the record must not overstate it: France carries Eurostat
            // status flag `p` (provisional) and Poland `ep` (estimated+provisional) - per the seed
            // doc's own words, good enough to seed a game and never to be quoted as settled. The USA
            // figure is CDC/NCHS FINAL mortality data - the strongest figure in the row.
            usa.BaselineLifeExpectancy = 79.0f;     // [VERIFIED] CDC/NCHS final, 2024
            germany.BaselineLifeExpectancy = 81.2f; // [VERIFIED] Eurostat API, no flag
            france.BaselineLifeExpectancy = 83.0f;  // ⚠ [PROVISIONAL] Eurostat flag `p`
            italy.BaselineLifeExpectancy = 83.7f;   // [VERIFIED] Eurostat API (replaces the wrong 84.1)
            poland.BaselineLifeExpectancy = 78.5f;  // ⚠ [PROVISIONAL] Eurostat flag `ep`
            sweden.BaselineLifeExpectancy = 83.8f;  // [VERIFIED] Eurostat API (replaces the wrong 84.1)

            // ROUND 4 BATCH 2 (C2): Gini coefficient, equivalised disposable income, 0-100 scale
            // (seed doc §2). The EU five are Eurostat `ilc_di12` `GINI_HND` 2024, confirmed at the
            // API 2026-08-02 with no status flags - the 0-100 scale is the SOURCE's own label, so
            // the scale trap is closed by construction. The USA is [ESTIMATED], not [VERIFIED], for
            // two unfixable reasons the seed doc documents: OECD IDD reference year 2019 carried
            // forward (band 38.5-41.0), and the square-root vs modified-OECD equivalence scales,
            // which produce different Ginis FROM IDENTICAL DATA - comparable in spirit, never in
            // construction. Already 0-100 at source (OECD publishes 0.395): NO conversion step, so
            // there is no factor-of-100 opportunity. The US-outlier claim survives the caveat; the
            // exact number does not.
            usa.BaselineGini = 39.5f;     // ⚠ [ESTIMATED] OECD IDD, 2019 carried forward, sqrt scale
            germany.BaselineGini = 29.5f; // [VERIFIED] Eurostat ilc_di12, 2024
            france.BaselineGini = 30.0f;  // [VERIFIED] Eurostat ilc_di12, 2024
            italy.BaselineGini = 32.2f;   // [VERIFIED] Eurostat ilc_di12, 2024 - highest of the five
            poland.BaselineGini = 26.0f;  // [VERIFIED] Eurostat API - the corrected figure (Statista's ~29 was 3 points high; Poland is the MOST equal of the five, not middling)
            sweden.BaselineGini = 27.6f;  // [VERIFIED] Eurostat ilc_di12, 2024

            // ROUND 4 BATCH 3 (C1): housing cost overburden, % of population, Eurostat
            // `ilc_lvho07a` 2024 - THE WHOLE-POPULATION VARIANT (`unit=PC, rskpovth=TOTAL,
            // age=TOTAL, sex=T`), recorded per the doc's own rule for this unusually variant-prone
            // indicator (Sweden alone reads 5.1/10.6/10.8/17.9 across published cuts - a bare
            // number carries no meaning). All five [VERIFIED], no status flags; France/Poland/Italy
            // are the 2026-08-02 API closures that landed inside the 4.0-9.0 bound.
            // ⚠ THE USA IS DELIBERATELY ABSENT - not a gap, the RULING: US sources measure >30%/>50%
            // of gross income where Eurostat measures >40% of disposable, no comparable figure
            // exists, and the seed doc's option 3 gives the USA homeownership as its primary
            // housing metric. TracksHousingOverburden=false carries that fact into the model
            // (early-out), the UI (no row) and the checks (the USA-unmoved assert).
            usa.TracksHousingOverburden = false;           // ⚠ RULED: homeownership-primary instead
            germany.BaselineHousingOverburden = 12.0f;     // [VERIFIED] whole-population, highest of the five
            sweden.BaselineHousingOverburden = 10.6f;      // [VERIFIED] whole-population (NOT the 5.1 tenure-cut variant)
            france.BaselineHousingOverburden = 7.0f;       // [VERIFIED] Eurostat API 2026-08-02
            poland.BaselineHousingOverburden = 5.2f;       // [VERIFIED] Eurostat API 2026-08-02
            italy.BaselineHousingOverburden = 5.1f;        // [VERIFIED] Eurostat API 2026-08-02

            // ROUND 4 BATCH 3 (C1): homeownership, % of HOUSEHOLDS (OECD Affordable Housing
            // Database basis - the doc's "use this basis only"; population-basis figures are a
            // different, larger number). The USA's PRIMARY housing metric per the ruling. Three are
            // [VERIFIED] OECD; three are the doc's own four-point fitted regression from the
            // Eurostat population basis - honest estimates with STATED 95% bands, seeded at the
            // point estimate and never to be quoted as settled.
            usa.BaselineHomeownership = 65.3f;     // [VERIFIED] OECD AHD - the ruled primary metric
            france.BaselineHomeownership = 58.5f;  // [VERIFIED] OECD AHD
            germany.BaselineHomeownership = 41.0f; // [VERIFIED] OECD AHD - the genuine structural outlier
            poland.BaselineHomeownership = 86.8f;  // ⚠ [ESTIMATED] fitted bridge, 95% band 78.4-95.2
            italy.BaselineHomeownership = 74.4f;   // ⚠ [ESTIMATED] fitted bridge, 95% band 66.8-82.1
            sweden.BaselineHomeownership = 62.1f;  // ⚠ [ESTIMATED] fitted bridge, 95% band 54.9-69.4

            // ROUND 4 BATCH R4-5 (C5): labour productivity, GDP per hour worked. ALL SIX on ONE
            // IDENTICAL basis, stated per line as the build directive requires: OECD `DSD_PDB`,
            // MEASURE=GDPHRS, ACTIVITY=_T, UNIT_MEASURE=USD_PPP_H, PRICE_BASE=V (current prices),
            // reference year 2022 (the newest complete same-basis cross-section - mixing France/
            // USA's 2024 values in would fabricate a cross-section that never existed), LIVE
            // VINTAGE RETRIEVED 2026-08-02. ⚠ The retrieval date is part of the basis: this series
            // RESTATES WHOLESALE (the doc's verification-integrity instance - the 2026-04-07
            // archive differs 1-2.3% at the same key), so a bare number is indistinguishable from
            // an error. The old Statista Sweden/Poland placeholders are SUPERSEDED history, not a
            // live basis split. Level is OWN-PAST-ONLY per the OECD methodology caution; the
            // ordering Germany > USA > Sweden > France > Italy > Poland matches the doc's own
            // recorded qualitative claims (Italian stagnation, Polish catch-up).
            germany.State.Productivity = 94.54f; // [VERIFIED] OECD DSD_PDB GDPHRS USD_PPP_H V 2022, retrieved 2026-08-02
            usa.State.Productivity = 90.83f;     // [VERIFIED] same basis, same vintage (revised DOWN vs the 2026-04 archive - the one negative restatement)
            sweden.State.Productivity = 89.95f;  // [VERIFIED] same basis, same vintage (supersedes the Statista ~70 placeholder)
            france.State.Productivity = 86.32f;  // [VERIFIED] same basis, same vintage
            italy.State.Productivity = 78.20f;   // [VERIFIED] same basis, same vintage - narrowly above the OECD 72.59 average
            poland.State.Productivity = 54.09f;  // [VERIFIED] same basis, same vintage (supersedes the Statista ~24.5 placeholder)

            // THE MATURITY RATE-LAG (ruling R4, 2026-08-17): average debt maturity in years, from
            // real debt-office data, source and date per line (searched 2026-08-17).
            // EffectiveDebtInterestRate is deliberately NOT seeded - the -1 sentinel initializes
            // to the current issuance rate on first advance (the zero-gap idiom through one path,
            // shared with pre-mechanism saves), so behavior diverges from the old code only once
            // rates actually move.
            usa.AverageDebtMaturityYears = 5.9f;     // [VERIFIED] US Treasury WAM of marketable debt ~70-72 months across 2025
            france.AverageDebtMaturityYears = 8.5f;  // [VERIFIED] AFT negotiable-debt average maturity 8y180d at 2026-05-31
            italy.AverageDebtMaturityYears = 7.0f;   // ⚠ [ESTIMATED] band 6.9-7.7 - MEF vita media residua, mixed 2023-2025 vintages
            germany.AverageDebtMaturityYears = 7.0f; // ⚠ [GAP-REPORTED to Elias, seeded ESTIMATED] band 6-8: no dated aggregate found; bounded by Finanzagentur portfolio structure (>65% in the 7-30y segment, 10y-dominant issuance). Never to be quoted as settled. BAND-PROVEN INSENSITIVE at the maturity bar (the whole mechanism moves Germany's ratio <= 0.1 points at every horizon, so any M in [6,8] is inside noise) - the estimate STANDS per the ruled rider; no desk graduation needed
            sweden.AverageDebtMaturityYears = 4.75f; // ⚠ [ESTIMATED] midpoint of Riksgalden's 3.5-6y steering range, 2025 guidelines - a TIME-TO-REFIXING basis, which for a REPRICING lag is the mechanism-relevant metric (stated, not glossed)
            poland.AverageDebtMaturityYears = 5.65f; // [VERIFIED-secondary] general-government average maturity, 2024 (official data via aggregator)

            // States open AT their baselines - the standing zero-gap idiom, from one authority
            // rather than twelve constructor arguments that could drift from the block above.
            // RealWageIndex opens at 100 for ALL SIX by ruling (2026-08-16): the seed doc's §5
            // growth row mixes three bases (net vs gross vs economy-wide) and must not seed a
            // level; base-100-at-epoch is the HPI convention applied for the HPI reason. The level
            // is display furniture, cross-country level comparison is NOT claimed, and the
            // simulation consumes only growth - which needs no seed at all (trend growth comes from
            // PotentialGrowthRate, already differentiated per country).
            foreach (Country seeded in new[] { usa, germany, france, italy, poland, sweden })
            {
                seeded.State.YouthUnemployment = seeded.BaselineYouthUnemploymentRate;
                seeded.State.LifeExpectancy = seeded.BaselineLifeExpectancy;
                seeded.State.Gini = seeded.BaselineGini;
                seeded.State.RealWageIndex = 100f;
                // C1: overburden parks at 0 (the field's own default) where untracked - the USA's
                // BaselineHousingOverburden is deliberately never set, so this assignment is a
                // 0=0 no-op there and the zero-gap idiom holds for the five that track it.
                seeded.State.HousingOverburden = seeded.BaselineHousingOverburden;
                seeded.State.Homeownership = seeded.BaselineHomeownership;
                seeded.State.HousePriceIndex = 100f; // the R4-2 index convention, third member: HPI
            }

            // Minimum wage as a percent of median wage (the "Kaitz index" economists use for
            // cross-country comparison) - real, illustrative-precision figures for the four countries
            // with a statutory minimum wage; Sweden and Italy have none (they rely on sector-level
            // collective bargaining instead), matching real-world fact - see
            // Country.MinimumWageImplemented's doc comment and "Labor Market Basics" in CLAUDE.md.
            // BaselineMinimumWagePercentOfMedian is seeded equal to the starting level for each, so a
            // fresh game opens at zero gap (no employment/poverty effect) rather than a turn-1 shock.
            usa.MinimumWageImplemented = true;
            usa.MinimumWagePercentOfMedian = 29f;
            usa.BaselineMinimumWagePercentOfMedian = 29f;
            germany.MinimumWageImplemented = true;
            germany.MinimumWagePercentOfMedian = 55f;
            germany.BaselineMinimumWagePercentOfMedian = 55f;
            france.MinimumWageImplemented = true;
            france.MinimumWagePercentOfMedian = 66f;
            france.BaselineMinimumWagePercentOfMedian = 66f;
            poland.MinimumWageImplemented = true;
            poland.MinimumWagePercentOfMedian = 52f;
            poland.BaselineMinimumWagePercentOfMedian = 52f;
            sweden.MinimumWageImplemented = false;
            italy.MinimumWageImplemented = false;

            // Deeper Labor Market Policies (Round 2 item 3 - see "Deeper Labor Market Policies" in
            // CLAUDE.md) - PaidFamilyLeaveWeeks is real, sourced via web search: USA 0 weeks
            // (confirmed - the USA is the only OECD country with no national statutory paid parental
            // leave), Sweden 69 weeks (confirmed - 480 days, ~390 at ~80% pay), Germany 58 weeks
            // (confirmed - 14 weeks maternity + 44 weeks parental), Poland 20 weeks (confirmed, the
            // full-pay portion specifically - Poland's total leave system extends further at partial
            // pay, not counted here). France (16 weeks) and Italy (22 weeks) are directionally-
            // informed estimates from general knowledge of each country's real statutory maternity-
            // leave system, not individually confirmed to the same precision as the other four.
            // BaselinePaidFamilyLeaveWeeks is seeded equal to the starting value for every country
            // (the same "avoid a turn-1 shock" anchor idiom used throughout this session).
            // OvertimeRegulationLevel/RetrainingProgramLevel are left at Country's own default (50,
            // neutral) for every country - uniform placeholders, since there's no real-world figure
            // to seed them differently by country (matching PoliceFundingLevel/SentencingSeverity's
            // own precedent).
            usa.PaidFamilyLeaveWeeks = 0f; usa.BaselinePaidFamilyLeaveWeeks = 0f;
            sweden.PaidFamilyLeaveWeeks = 69f; sweden.BaselinePaidFamilyLeaveWeeks = 69f;
            germany.PaidFamilyLeaveWeeks = 58f; germany.BaselinePaidFamilyLeaveWeeks = 58f;
            france.PaidFamilyLeaveWeeks = 16f; france.BaselinePaidFamilyLeaveWeeks = 16f;
            italy.PaidFamilyLeaveWeeks = 22f; italy.BaselinePaidFamilyLeaveWeeks = 22f;
            poland.PaidFamilyLeaveWeeks = 20f; poland.BaselinePaidFamilyLeaveWeeks = 20f;

            // THE STATUTORY BASE SYNC (pass 3, coexistence ruling 2026-08-26): every labor dial's
            // bill-owned base opens EQUAL to the dial itself - zero law offset at seed, so a fresh
            // world composes effective = clamp(base + 0) = exactly the values seeded above, and
            // the no-law trajectory is byte-identical by construction. One loop rather than
            // six-by-six assignments so a future seed change up there cannot silently diverge
            // from its base.
            foreach (Country laborBaseCountry in new[] { usa, sweden, germany, france, italy, poland })
            {
                laborBaseCountry.MinimumWagePercentOfMedianBase = laborBaseCountry.MinimumWagePercentOfMedian;
                laborBaseCountry.PaidFamilyLeaveWeeksBase = laborBaseCountry.PaidFamilyLeaveWeeks;
                laborBaseCountry.OvertimeRegulationBase = laborBaseCountry.OvertimeRegulationLevel;
                laborBaseCountry.RetrainingProgramBase = laborBaseCountry.RetrainingProgramLevel;
                laborBaseCountry.FamilyPolicyBase = laborBaseCountry.FamilyPolicyLevel;
                laborBaseCountry.ImmigrationPolicyBase = laborBaseCountry.ImmigrationPolicyLevel;
            }

            // Deeper Crime & Justice (Round 2 item 4) - BaselinePrisonPopulationRate is real,
            // per-100,000 incarceration-rate data from the World Prison Brief (confirmed via web
            // search): USA 531 (highest among developed nations), Germany 72, France 111. Sweden (60)
            // is estimated from its close real-world proximity to Nordic peers (Finland 51, Norway
            // 57), not individually confirmed to the same precision. Italy (92) and Poland (185) are
            // general-knowledge estimates - Poland's notably higher rate among the six (closer to
            // Western Europe's higher end) and Italy's moderate rate are directionally consistent
            // with well-known regional patterns, but not individually confirmed via this search.
            // BailReformLevel/DrugPolicyLevel are left at Country's own default (50, neutral) for
            // every country - uniform placeholders, matching PoliceFundingLevel/SentencingSeverity's
            // own precedent.
            usa.BaselinePrisonPopulationRate = 531f;
            sweden.BaselinePrisonPopulationRate = 60f;
            germany.BaselinePrisonPopulationRate = 72f;
            france.BaselinePrisonPopulationRate = 111f;
            italy.BaselinePrisonPopulationRate = 92f;
            poland.BaselinePrisonPopulationRate = 185f;

            // MacroSystem.ApplyCrimeIndex's per-country structural anchor - the SAME stylized 0-100
            // figures EconomyState.CrimeIndex was just seeded to above, so a new game opens already at
            // (or very near) its own baseline. NOT a literal transformation of any single real
            // indicator - informed by real relative homicide-rate rankings (USA highest of the six;
            // Sweden elevated due to well-documented recent gang violence, comparable to France;
            // Germany and Poland lower; Italy lowest, per UNODC/Eurostat/national reporting) but
            // "crime" as a broad concept has no single clean cross-country comparable metric the way
            // poverty/labor-participation rates do - see "Crime & Justice Basics" in CLAUDE.md.
            // PoliceFundingLevel/SentencingSeverity are left at Country's own default (50, neutral)
            // for every country - a uniform placeholder, since there's no real-world figure to seed
            // them differently by country.
            usa.BaselineCrimeIndex = 45f;
            sweden.BaselineCrimeIndex = 30f;
            germany.BaselineCrimeIndex = 25f;
            france.BaselineCrimeIndex = 30f;
            italy.BaselineCrimeIndex = 18f;
            poland.BaselineCrimeIndex = 20f;

            // Round 3 item 3: MacroSystem.ApplyOrganizedCrimeIndex's per-country structural anchor -
            // the SAME stylized 0-100 figures EconomyState.OrganizedCrimeIndex was just seeded to
            // above. Informed by the real Global Organized Crime Index (GI-TOC): Italy's historic,
            // extremely well-documented organized-crime organizations (Cosa Nostra, Camorra,
            // 'Ndrangheta) give it high confidence as the clear highest of the six; Sweden's real,
            // well-documented recent gang-violence surge (the same fact already informing its
            // elevated BaselineCrimeIndex above) justifies its own elevated figure. USA/France/Poland/
            // Germany's relative ordering beyond those two is a directional, stylized estimate, not
            // independently confirmed against a specific index-year.
            usa.BaselineOrganizedCrimeIndex = 35f;
            sweden.BaselineOrganizedCrimeIndex = 32f;
            germany.BaselineOrganizedCrimeIndex = 20f;
            france.BaselineOrganizedCrimeIndex = 28f;
            italy.BaselineOrganizedCrimeIndex = 55f;
            poland.BaselineOrganizedCrimeIndex = 22f;

            // Round 3 item 3: MacroSystem.ApplyCorruptionIndex's per-country structural anchor - the
            // SAME stylized 0-100 figures EconomyState.CorruptionIndex was just seeded to above.
            // Higher = MORE corrupt (this project's own "higher = worse" convention), informed by
            // roughly 100 minus the real Transparency International Corruption Perceptions Index
            // (itself 0-100, higher = cleaner) - not a literal year-specific score. Nordic/German
            // clean-government reputation and Italy's comparatively lower CPI standing among Western
            // European/G7 peers are both real and well-documented, high confidence; the exact relative
            // ordering of Italy versus Poland specifically is a directional estimate, not confirmed
            // against one index-year. JudicialFundingLevel/BorderEnforcementLevel are left at
            // Country's own default (50, neutral) for every country - the same uniform-placeholder
            // reasoning PoliceFundingLevel/SentencingSeverity already established.
            usa.BaselineCorruptionIndex = 31f;
            sweden.BaselineCorruptionIndex = 18f;
            germany.BaselineCorruptionIndex = 22f;
            france.BaselineCorruptionIndex = 30f;
            italy.BaselineCorruptionIndex = 44f;
            poland.BaselineCorruptionIndex = 40f;

            // Round 3 item 5, Part A: Population/BirthRate/DeathRate/NetMigrationRate seeded from real
            // 2024/2025 data above (USA 341.8M, Germany 83.6M, France 69.1M, Italy 58.9M, Poland
            // 37.5M, Sweden 10.6M population; birth/death/net-migration per-1000 figures per country -
            // see EconomyState's own field-level doc comments for the full per-country figures). The
            // real, standard old-age dependency ratio (65+ population as % of working-age 15-64) is
            // the anchor MacroSystem.ApplyDemographicRates' drift and every gap-based effect (pension
            // pressure, labor force participation) measure against - real/well-documented for Italy
            // (highest of the six, among the highest in the world - its aging population is one of the
            // most well-known real demographic facts globally) and USA/Poland (lowest, both real and
            // consistent with USA's comparatively younger population among developed nations and
            // Poland's historically younger post-WWII demographic structure, though rapidly aging).
            // Germany's figure is informed by an ESTIMATED 65+ population share (~22-23%, full
            // age-cohort breakdown unavailable), honestly not a directly-sourced dependency ratio the
            // way Italy/USA/Poland's are. Sweden/France are directional estimates, informed by their
            // real, well-documented status as moderately-aged (neither the youngest nor oldest)
            // developed European nations. BaselineNetMigrationRate is set equal to each country's own
            // seeded starting NetMigrationRate (the same "avoid a turn-1 shock" anchor idiom every
            // other Baseline field uses) - no policy lever touches NetMigrationRate in Part A, so this
            // anchor only matters once ambient aging-driven drift (see MacroSystem) or, later, Part
            // B's Immigration Policy lever moves the actual rate away from it.
            usa.BaselineDependencyRatio = 28f;
            usa.BaselineNetMigrationRate = 3.7f;
            sweden.BaselineDependencyRatio = 33f;
            sweden.BaselineNetMigrationRate = 1.1f;
            germany.BaselineDependencyRatio = 35f;
            germany.BaselineNetMigrationRate = 1.8f;
            france.BaselineDependencyRatio = 33f;
            france.BaselineNetMigrationRate = 1.1f;
            italy.BaselineDependencyRatio = 40f;
            italy.BaselineNetMigrationRate = 1.3f;
            poland.BaselineDependencyRatio = 28f;
            poland.BaselineNetMigrationRate = 0.2f;

            // Round 3 item 5, Part A (corrected): SteadyStateGrowthRate, per-1000 population per
            // turn/year - the fixed long-run target Country.PopulationGrowthRate mean-reverts toward
            // (see MacroSystem.ApplyPopulationGrowth), added because letting the raw birth/death/
            // migration gap drive Population directly and indefinitely produced implausible aggregate
            // outcomes (near-extinction / near-quadrupling) over this project's 500-turn validation
            // horizon, despite every individual rate staying within its own realistic bound.
            //
            // Directionally real for all six: Poland/Italy are the two most severe, well-documented
            // sub-replacement-fertility decliners in Europe; Germany is a real but more moderate
            // decline; France is the most fertility-resilient large EU economy, real and
            // well-documented, hence near-stable; Sweden/USA are real, modest, immigration-driven
            // growers among developed nations.
            //
            // Magnitudes are HONESTLY DAMPED below a literal extrapolation of current trends. This
            // project's "1 turn ~= 1 year" convention makes the 500-turn validation horizon a ~500-year
            // span - roughly 6.7x Eurostat/UN's actual 2025-2100 (75-year) projection window. Poland's
            // figure is explicitly anchored to Eurostat's own 2025-2100 population projection (-31.6%
            // cumulative decline) via the implied constant annual rate solving (1+r)^75 = 0.684, i.e.
            // r = 0.684^(1/75) - 1 ~= -5.05 per 1000/year. Applying that literal real rate for a full
            // 500 years would ITSELF compound to roughly a 92% decline ((1 - 0.00505)^500 ~= e^-2.53 ~=
            // 0.0795 of the starting population) - a mechanical consequence of the horizon length, not
            // evidence the rate is unrealistic. -3.5 is deliberately damped further below that -5.05
            // figure precisely so the 500-turn outcome reads as a severe-but-plausible trajectory
            // (comparable in spirit, not in literal cumulative percentage, to Eurostat's 75-year
            // figure) rather than a literal 500-year compound of a real 75-year rate. The same
            // "generous, honestly-labeled bound rather than literal reality" idiom as
            // MaxDebtToGdpPercent's 300% ceiling - see CLAUDE.md for the full derivation and the
            // resulting 500-turn validation numbers.
            usa.SteadyStateGrowthRate = 1.8f;
            sweden.SteadyStateGrowthRate = 1.5f;
            germany.SteadyStateGrowthRate = -1.5f;
            france.SteadyStateGrowthRate = -0.3f;
            italy.SteadyStateGrowthRate = -3.0f;
            poland.SteadyStateGrowthRate = -3.5f;

            SeedWelfarePrograms(usa);
            SeedWelfarePrograms(sweden);
            SeedWelfarePrograms(germany);
            SeedWelfarePrograms(france);
            SeedWelfarePrograms(italy);
            SeedWelfarePrograms(poland);

            // Economic Sectors (see "Economic Sectors" in CLAUDE.md) - Output % of GDP is real World
            // Bank data for Manufacturing/Agriculture (Manufacturing value added: USA 10%, Sweden
            // 12.6%, Germany 19.9%, France 10.7%, Italy 16.6%, Poland 18.1%; Agriculture value added:
            // all low single digits, USA ~1.0%, Sweden ~1.4%, Germany ~0.8%, France ~1.6%, Italy
            // ~2.1%, Poland ~2.4% - Poland/Italy notably higher among the six). Finance (financial and
            // insurance services value added) is partially grounded (USA ~8%, confirmed via search;
            // the other five are directional estimates, not individually confirmed). Technology has NO
            // clean standard national-accounts category comparable across countries and is entirely
            // stylized, informed by general knowledge of relative tech-sector size/innovation ranking
            // (USA and Sweden - the latter well known for an outsized startup/tech scene relative to
            // its population - highest; Germany/France/Poland mid; Italy lowest). Employment % and
            // every sector's one-off SectorMetric (Manufacturing: Capacity Utilization %; Technology:
            // a stylized Innovation Index 0-100; Agriculture: Export Share % of sector output;
            // Finance: a stylized annual Credit Growth Rate %) are illustrative estimates throughout,
            // directionally reasonable but not individually sourced - except Poland's Agriculture
            // Employment share (8%), which is real and well-documented: Poland has one of the EU's
            // highest shares of agricultural employment relative to its output share, reflecting its
            // more fragmented, smallholder farm structure.
            //
            // Round 3 item 4 added four more sectors, same real-vs-stylized honesty standard. Energy
            // and Construction are real, standard value-added categories with genuine country
            // differentiation: Poland's coal-heavy energy sector and EU-funded construction boom are
            // both real and well-documented, giving it the clear highest Output in both among the six;
            // the other five countries' figures are directional estimates, not individually confirmed.
            // Retail and Telecommunications Output are directional estimates throughout (no single
            // country stands out clearly enough to flag as confirmed). SectorMetric: Energy ->
            // Renewable Share % (Germany's real, well-documented Energiewende renewable push and
            // Poland's real, well-documented status as the EU's most coal-dependent economy are both
            // confirmed; the rest are directional); Construction -> a stylized 0-100 Building Activity
            // Index (entirely stylized, mirrors Technology's Innovation Index); Retail -> E-Commerce
            // Share % (directional estimate); Telecommunications -> Broadband Penetration % (real
            // OECD-documented pattern - all six are high-broadband developed nations with Nordic
            // countries typically at the very top, though exact figures are directional). Employment %
            // for all four follows the same real-world labor-intensity pattern already established for
            // the original four (Construction/Retail more labor-intensive than their Output share,
            // matching real-world convention; Energy/Telecommunications less so, mirroring Finance's
            // own low employment-to-output ratio) - illustrative, not individually sourced, except
            // Poland's elevated Energy employment share (coal-sector jobs are a real, well-documented
            // political and economic concentration in Poland specifically).
            SeedSectors(usa,
                (SectorType.Manufacturing, 10.0f, 8.0f, 77f),
                (SectorType.Technology, 10.0f, 4.0f, 78f),
                (SectorType.Agriculture, 1.0f, 1.5f, 20f),
                (SectorType.Finance, 8.0f, 4.0f, 4f),
                (SectorType.Energy, 3.5f, 1.0f, 22f),
                (SectorType.Construction, 4.5f, 5.0f, 52f),
                (SectorType.Retail, 5.8f, 10.0f, 16f),
                (SectorType.Telecommunications, 2.0f, 0.8f, 90f));
            SeedSectors(sweden,
                (SectorType.Manufacturing, 12.6f, 11.0f, 78f),
                (SectorType.Technology, 8.0f, 5.0f, 80f),
                (SectorType.Agriculture, 1.4f, 1.8f, 25f),
                (SectorType.Finance, 6.0f, 2.5f, 3.5f),
                (SectorType.Energy, 2.5f, 0.8f, 65f),
                (SectorType.Construction, 6.0f, 7.0f, 55f),
                (SectorType.Retail, 4.5f, 8.0f, 15f),
                (SectorType.Telecommunications, 2.3f, 1.0f, 96f));
            SeedSectors(germany,
                (SectorType.Manufacturing, 19.9f, 18.0f, 79f),
                (SectorType.Technology, 5.5f, 3.0f, 70f),
                (SectorType.Agriculture, 0.8f, 1.2f, 30f),
                (SectorType.Finance, 4.0f, 2.5f, 3f),
                (SectorType.Energy, 3.0f, 1.0f, 48f),
                (SectorType.Construction, 5.5f, 6.0f, 50f),
                (SectorType.Retail, 4.8f, 9.0f, 14f),
                (SectorType.Telecommunications, 1.8f, 0.8f, 93f));
            SeedSectors(france,
                (SectorType.Manufacturing, 10.7f, 10.0f, 76f),
                (SectorType.Technology, 5.0f, 2.8f, 68f),
                (SectorType.Agriculture, 1.6f, 2.5f, 35f),
                (SectorType.Finance, 4.0f, 2.8f, 3.5f),
                (SectorType.Energy, 2.5f, 0.9f, 25f),
                (SectorType.Construction, 5.5f, 6.5f, 50f),
                (SectorType.Retail, 5.0f, 9.5f, 12f),
                (SectorType.Telecommunications, 1.9f, 0.8f, 90f));
            SeedSectors(italy,
                (SectorType.Manufacturing, 16.6f, 15.0f, 75f),
                (SectorType.Technology, 3.5f, 2.0f, 55f),
                (SectorType.Agriculture, 2.1f, 3.8f, 25f),
                (SectorType.Finance, 5.5f, 2.5f, 2.5f),
                (SectorType.Energy, 2.5f, 0.8f, 35f),
                (SectorType.Construction, 4.5f, 5.5f, 45f),
                (SectorType.Retail, 5.2f, 9.0f, 10f),
                (SectorType.Telecommunications, 1.7f, 0.7f, 85f));
            SeedSectors(poland,
                (SectorType.Manufacturing, 18.1f, 17.0f, 78f),
                (SectorType.Technology, 5.0f, 3.0f, 58f),
                (SectorType.Agriculture, 2.4f, 8.0f, 40f),
                (SectorType.Finance, 4.0f, 2.0f, 5f),
                (SectorType.Energy, 4.5f, 1.5f, 18f),
                (SectorType.Construction, 7.0f, 8.0f, 62f),
                (SectorType.Retail, 5.5f, 10.0f, 11f),
                (SectorType.Telecommunications, 1.6f, 0.9f, 87f));

            // Infrastructure System (Round 2 item 5, see "Infrastructure System" in CLAUDE.md) -
            // ConditionIndex (0-100, higher = better) is seeded from the IMD World Competitiveness
            // Ranking's Infrastructure factor (0-100 scale, 2026 edition), used as each country's
            // overall anchor: USA 73.7, Sweden 81.8, Germany 67.7, Italy 58.1, Poland 57.3 - all
            // confirmed via web search. France's overall infrastructure score was not found in the
            // same source and is a directional estimate (66) positioned between Germany and Italy,
            // honestly disclosed as such. Per-type values are illustrative estimates ANCHORED to that
            // real country-level score, except for a handful of well-documented, real divergences from
            // a country's own overall anchor, called out per country below - the full 6x4 matrix isn't
            // independently sourced cell-by-cell, the same "confirmed anchor, illustrative breakdown"
            // honesty standard Economic Sectors already established for Finance/Technology.
            //  - USA: Roads 55 (well below its own anchor - ASCE's 2025 Infrastructure Report Card
            //    gives US roads a D+, a well-documented weak point) and Rail 80 (above anchor - ASCE
            //    gives rail a B-, relatively strong, particularly freight); PowerGrid 62 (below anchor -
            //    ASCE's Energy category also graded D+, citing capacity constraints from electrification
            //    and data-center demand); Broadband 74 (roughly matches the anchor - solid but uneven
            //    urban/rural coverage, not confirmed to the same ASCE-grade precision).
            //  - Sweden: Broadband 90 (above anchor - Sweden is real and well-documented as an OECD/ITU
            //    leader in fiber/broadband penetration and digital infrastructure). Roads/Rail/PowerGrid
            //    all illustrative, anchored near its high 81.8 overall score.
            //  - Germany: Rail 62 (below anchor - Deutsche Bahn's real, widely-reported punctuality and
            //    reliability decline in recent years) and Broadband 68 (below anchor - Germany is real
            //    and widely-reported as lagging in fiber/broadband rollout relative to its overall
            //    economic strength, a recurring OECD talking point). Roads/PowerGrid illustrative.
            //  - France: Rail 78 (above its estimated anchor - France's TGV high-speed rail network is
            //    real and internationally well-regarded). Roads/PowerGrid/Broadband illustrative.
            //  - Italy and Poland: no single well-documented divergence found for any one type: all
            //    four illustrative, anchored near each country's own real overall score (Poland's Roads
            //    at 60 reflects its real, well-documented major highway investment since EU accession,
            //    improving substantially from a low base - a directional, not precisely sourced, figure).
            SeedInfrastructure(usa, roads: 55f, rail: 80f, powerGrid: 62f, broadband: 74f);
            SeedInfrastructure(sweden, roads: 80f, rail: 85f, powerGrid: 78f, broadband: 90f);
            SeedInfrastructure(germany, roads: 70f, rail: 62f, powerGrid: 65f, broadband: 68f);
            SeedInfrastructure(france, roads: 68f, rail: 78f, powerGrid: 70f, broadband: 66f);
            SeedInfrastructure(italy, roads: 55f, rail: 58f, powerGrid: 60f, broadband: 60f);
            SeedInfrastructure(poland, roads: 60f, rail: 55f, powerGrid: 58f, broadband: 62f);

            // Sovereign Wealth Fund (see "Sovereign Wealth Fund" in CLAUDE.md) - none of the six
            // countries has a "classic" Norway/Gulf-state-style oil-revenue sovereign wealth fund;
            // Country.SovereignWealthFund honestly stays null (no fund) for USA, Germany, Italy, and
            // Poland, matching real-world fact (the USA mechanic is still player-creatable via
            // GameController's tab - this seeding doesn't change that). Sweden and France DO have a
            // real, if more modest, partial analog worth seeding directly:
            //  - Sweden's AP pension buffer funds (AP1-AP4, AP6 combined) held ~$195B (~SEK 2.1
            //    trillion) at end of 2024 - against Sweden's real GDP (~$620B, matching this game's
            //    Sweden GDP scale of 620), that's ~31% of GDP, seeded directly (TotalAssets: 195).
            //    Their real mandate is public equities + fixed income + a smaller unlisted-assets
            //    share - EquitiesWeight/BondsWeight/InfrastructureWeight/RealEstateWeight below split
            //    that illustratively (not individually sourced per sub-asset-class). A modest
            //    ContributionRatePercent (0.3%) reflects the funds' real role as a mature, largely
            //    stable pension buffer, not a fast-growing new fund - the exact net contribution rate
            //    isn't individually sourced, an illustrative small figure honestly labeled as such.
            //  - France's FRR (Fonds de reserve pour les retraites) held ~EUR21-24B (~$24-27B) as of
            //    recent reporting - against France's real GDP (~$3T, matching this game's France GDP
            //    scale of 3200), that's under 1% of GDP, seeded as TotalAssets: 27. Real allocation:
            //    ~46% unhedged equities, ~15% unlisted, ~18%+ investment-grade fixed income - mapped
            //    illustratively onto the four asset classes below. The FRR's real recent history is
            //    a NET DRAWDOWN phase (it stopped receiving material new contributions around 2011 and
            //    now pays OUT to pension funds annually) - this model's ContributionRatePercent can
            //    only be non-negative, so that real drawdown dynamic isn't representable in this pass;
            //    a near-zero rate (0.1%) is the closest honest approximation, not a claim that FRR is
            //    still growing via contributions the way it once did.
            //  - Market-return assumptions (SovereignWealthFundSystem's average return per asset
            //    class) are DELIBERATELY NOT forked per country - a given asset class's real long-run
            //    return doesn't meaningfully depend on which country's fund holds it (both Sweden's
            //    and France's funds invest substantially in global, not purely domestic, markets), so
            //    country differentiation belongs in ALLOCATION and CONTRIBUTION RATE, not in the
            //    return-rate model itself.
            sweden.SovereignWealthFund = new SovereignWealthFund
            {
                TotalAssets = 195f,
                ContributionRatePercent = 0.3f,
                DomesticAllocationPercent = 35f,
                EquitiesWeight = 55f,
                BondsWeight = 35f,
                InfrastructureWeight = 5f,
                RealEstateWeight = 5f
            };
            france.SovereignWealthFund = new SovereignWealthFund
            {
                TotalAssets = 27f,
                ContributionRatePercent = 0.1f,
                DomesticAllocationPercent = 50f,
                EquitiesWeight = 50f,
                BondsWeight = 35f,
                InfrastructureWeight = 8f,
                RealEstateWeight = 7f
            };

            SeedUsaSpendingLines(usa);

            // Country-selection task, Part 2: generic spending decomposition for the other five
            // countries - a PURE decomposition of each country's existing GovernmentSpendingRate-
            // derived total into 5 broad categories, not a recalibration (see SeedGenericSpendingLines
            // for how the exact-sum guarantee works, and its own doc comment for why these percentage
            // splits are honestly illustrative, not individually researched). Directionally-informed
            // splits, not researched figures: Social Programs is the largest bucket everywhere
            // (Nordic/Western European welfare states skew high); Defense is notably higher for Poland
            // (real, well-documented - Poland has run one of NATO's highest defense-spending-to-GDP
            // ratios in recent years given its frontline position) and somewhat higher for France (a
            // larger standing military/nuclear deterrent than Germany/Italy/Sweden); the remainder
            // (Infrastructure & Development / Public Services / Administration) fills out each
            // country's own total. None of these five categories maps to this game's existing
            // WelfarePrograms portfolio, which already separately covers each country's actual
            // transfer/entitlement spending - Social Programs here represents broader discretionary
            // social-sector spending (health/education/social-services infrastructure), not the same
            // dollars WelfarePrograms tracks.
            // Playtest-2 item 4 (ruled 2026-08-25): Sweden graduates from the generic 5-line
            // decomposition to its real utgiftsomrade structure - see SeedSwedenSpendingLines.
            // The other four stay on the generic seed until their own ruled passes.
            SeedSwedenSpendingLines(sweden);
            SeedGenericSpendingLines(germany, socialPercent: 40f, defensePercent: 5f, infrastructurePercent: 13f, publicServicesPercent: 24f);
            SeedGenericSpendingLines(france, socialPercent: 38f, defensePercent: 7f, infrastructurePercent: 12f, publicServicesPercent: 25f);
            SeedGenericSpendingLines(italy, socialPercent: 40f, defensePercent: 4f, infrastructurePercent: 11f, publicServicesPercent: 26f);
            SeedGenericSpendingLines(poland, socialPercent: 34f, defensePercent: 10f, infrastructurePercent: 16f, publicServicesPercent: 22f);

            // RECALIBRATION (build-order item 1, terminal rulings 2026-08-26): the mandatory
            // transfer block - the general-government cash-transfer layer (~20% of GDP in every
            // real European state) these four seeds previously omitted entirely, which produced
            // measured year-1 primary surpluses of +14..+22% of GDP (the item-4 finding,
            // re-measured by FiscalRecalDiagnostic before anything here changed) while the fiscal
            // reaction multiplier was crushed to 0.58-0.76 papering over it. G is deliberately
            // untouched: at 18-24% it already matches these countries' real government-consumption
            // shares, and raising it would break every C+I+G+NX seed identity (the USA PotentialGDP
            // lesson). SocialSecurity = real old-age cash benefits; IncomeSecurity = the residual
            // that lands each year-1 primary balance on its real 2025 structural position
            // (DE -1.6, FR -2.9, IT +0.8, PL -4.8) - sources and the solve in the class doc
            // comment and the method's own. Sweden's block is inside SeedSwedenSpendingLines
            // (the flipped UO lines + the out-of-budget pension system + its own residual).
            SeedMandatoryTransferLines(germany, socialSecurityPercent: 9.0f, incomeSecurityPercent: 11.80f);
            SeedMandatoryTransferLines(france, socialSecurityPercent: 12.4f, incomeSecurityPercent: 10.12f);
            SeedMandatoryTransferLines(italy, socialSecurityPercent: 13.6f, incomeSecurityPercent: 7.70f);
            SeedMandatoryTransferLines(poland, socialSecurityPercent: 10.4f, incomeSecurityPercent: 13.35f);

            // Reserve-currency treatment (see "Reserve-Currency Debt Interest Treatment" in
            // CLAUDE.md): the USA doesn't face the same market risk premium as other sovereigns at an
            // equivalent debt-to-GDP ratio, and its effective interest rate on EXISTING debt reflects
            // the real blended average rate on federal debt (~3.3%) rather than today's policy rate
            // applied to the whole stock. Every other country keeps the default (unset override, full
            // risk-premium sensitivity) - Italy/Poland/etc.'s existing curve is untouched.
            usa.BaseDebtInterestRateOverride = 3.3f;
            usa.RiskPremiumSensitivity = 0.05f;

            // Independent Federal Reserve (see CLAUDE.md's "Federal Reserve" section): a non-null
            // CurrentFedChair is what switches CurrencySystem.ApplyInterestRateChanges over to the
            // TaylorRule-plus-bias mechanic for USA specifically. This turn-0 default is a Moderate
            // placeholder, distinct from FederalReserveSystem.CandidatePool's election candidates,
            // until the first election cycle offers the player a real choice.
            usa.CurrentFedChair = new FedChair(
                "Harriet Ellsworth", FedChairPhilosophy.Moderate,
                "The sitting chair at the start of the game - tracks the Taylor Rule's suggested rate closely, with no strong lean in either direction.",
                0f);

            var euMembers = new List<CountryId> { germany.Id, france.Id, italy.Id, sweden.Id, poland.Id };
            var europeanUnion = new TradeBloc("European Union", euMembers, externalTariffRate: 3f, internalTariffRate: 0.1f);

            AddBilateralTrade(usa, germany, aExportVolume: 120f, aImportVolume: 150f);
            AddBilateralTrade(usa, france, aExportVolume: 80f, aImportVolume: 90f);
            AddBilateralTrade(usa, sweden, aExportVolume: 30f, aImportVolume: 25f);
            AddBilateralTrade(usa, poland, aExportVolume: 20f, aImportVolume: 18f);
            AddBilateralTrade(germany, france, aExportVolume: 200f, aImportVolume: 180f);
            AddBilateralTrade(germany, italy, aExportVolume: 150f, aImportVolume: 140f);
            AddBilateralTrade(germany, poland, aExportVolume: 100f, aImportVolume: 90f);
            AddBilateralTrade(germany, sweden, aExportVolume: 70f, aImportVolume: 65f);
            AddBilateralTrade(france, italy, aExportVolume: 90f, aImportVolume: 85f);
            AddBilateralTrade(poland, sweden, aExportVolume: 40f, aImportVolume: 35f);

            var world = new World();
            world.Countries.AddRange(new[] { usa, sweden, germany, france, italy, poland });
            world.TradeBlocs.Add(europeanUnion);

            // Political Systems Overhaul Part B (Parliament), Master Sequence step 4: every country
            // starts at ApprovalRating 50, so PartyArchetypeData.GetInitialSeats() (which assumes
            // exactly that) is a correct seed for all six, not just a convenient shortcut for one.
            foreach (Country country in world.Countries)
            {
                country.ParliamentSeats = PartyArchetypeData.GetInitialSeats();
            }

            return world;
        }

        /// <summary>
        /// Wires a trade link both ways: country A's export volume is country B's import volume,
        /// and vice versa.
        /// </summary>
        private static void AddBilateralTrade(Country a, Country b, float aExportVolume, float aImportVolume)
        {
            a.TradePartners.Add(new TradePartner(b.Id, aExportVolume, aImportVolume));
            b.TradePartners.Add(new TradePartner(a.Id, aImportVolume, aExportVolume));
        }

        // Uniform illustrative placeholder rates for the "modest/inactive" tax types every country
        // starts with - not researched figures, just plausible starting points if the player later
        // implements them (ExciseTax/PropertyTax/WealthTax/StampDuty never vary by country here;
        // VAT/SalesTax/EstateTax/CarbonTax do, since whether/how each is used varies by country - see
        // the per-country SeedTaxLines calls above).
        private const float ModestExciseTaxRate = 8f;
        private const float ModestPropertyTaxRate = 1f;
        private const float ModestWealthTaxRate = 1.5f;
        private const float ModestStampDutyRate = 1f;

        /// <summary>
        /// Builds one country's starting TaxLine portfolio. IncomeTax/CorporateTax/PayrollTax/
        /// CapitalGainsTax are implemented for every country (only their rates vary); VAT/SalesTax/
        /// EstateTax/CarbonTax vary in both rate and whether they're implemented per country;
        /// ExciseTax/PropertyTax/WealthTax/StampDuty are always seeded inactive with a uniform
        /// placeholder rate (see the Modest* constants) - present so the player can implement them,
        /// per the brief no country (including this one) starts with an active general WealthTax.
        /// </summary>
        private static void SeedTaxLines(
            Country country,
            float incomeTax, float corporateTax,
            float vat, bool vatImplemented,
            float payrollTax, float capitalGainsTax,
            float salesTax, bool salesTaxImplemented,
            float estateTax, bool estateTaxImplemented,
            float carbonTax, bool carbonTaxImplemented)
        {
            // ROUND 4 BATCH 2 (C2): the Gini model's redistribution anchor, captured HERE - the one
            // place the seeded income-tax rate exists - so the anchor and the TaxLine cannot drift.
            country.BaselineIncomeTaxRate = incomeTax;

            country.TaxLines.AddRange(new[]
            {
                new TaxLine(TaxType.IncomeTax, incomeTax, isImplemented: true),
                new TaxLine(TaxType.CorporateTax, corporateTax, isImplemented: true),
                new TaxLine(TaxType.VAT, vat, vatImplemented),
                new TaxLine(TaxType.PayrollTax, payrollTax, isImplemented: true),
                new TaxLine(TaxType.CapitalGainsTax, capitalGainsTax, isImplemented: true),
                new TaxLine(TaxType.SalesTax, salesTax, salesTaxImplemented),
                new TaxLine(TaxType.ExciseTax, ModestExciseTaxRate, isImplemented: false),
                new TaxLine(TaxType.PropertyTax, ModestPropertyTaxRate, isImplemented: false),
                new TaxLine(TaxType.EstateTax, estateTax, estateTaxImplemented),
                new TaxLine(TaxType.WealthTax, ModestWealthTaxRate, isImplemented: false),
                new TaxLine(TaxType.CarbonTax, carbonTax, carbonTaxImplemented),
                new TaxLine(TaxType.StampDuty, ModestStampDutyRate, isImplemented: false),
            });
        }

        /// <summary>Every WelfareProgram's default starting GenerosityLevel if/when a player later implements it - a plausible mid-range starting point, not a researched figure, mirroring the "modest/inactive" tax lines' own placeholder-rate idiom.</summary>
        private const float DefaultWelfareGenerosity = 50f;

        /// <summary>
        /// Builds one country's starting welfare portfolio: all six WelfareProgramTypes present (so
        /// the player can implement any of them), but NONE implemented by default for any country -
        /// per the task's explicit requirement, matching how a country's tax portfolio always
        /// includes every TaxType but not every one starts implemented.
        /// </summary>
        private static void SeedWelfarePrograms(Country country)
        {
            country.WelfarePrograms.AddRange(new[]
            {
                new WelfareProgram(WelfareProgramType.UBI, DefaultWelfareGenerosity, isImplemented: false),
                new WelfareProgram(WelfareProgramType.NegativeIncomeTax, DefaultWelfareGenerosity, isImplemented: false),
                new WelfareProgram(WelfareProgramType.MeansTestedWelfare, DefaultWelfareGenerosity, isImplemented: false),
                new WelfareProgram(WelfareProgramType.UniversalHealthcare, DefaultWelfareGenerosity, isImplemented: false),
                new WelfareProgram(WelfareProgramType.HousingAssistance, DefaultWelfareGenerosity, isImplemented: false),
                new WelfareProgram(WelfareProgramType.ChildcareSubsidies, DefaultWelfareGenerosity, isImplemented: false),
            });
        }

        /// <summary>Seeds all four InfrastructureAssets for a country - see this class's call site for the real-data-vs-stylized breakdown of each argument.</summary>
        private static void SeedInfrastructure(Country country, float roads, float rail, float powerGrid, float broadband)
        {
            country.InfrastructureAssets.AddRange(new[]
            {
                new InfrastructureAsset(InfrastructureType.Roads, roads),
                new InfrastructureAsset(InfrastructureType.Rail, rail),
                new InfrastructureAsset(InfrastructureType.PowerGrid, powerGrid),
                new InfrastructureAsset(InfrastructureType.Broadband, broadband),
            });
        }

        /// <summary>
        /// Seeds every Sector for a country from a (Type, Output, Employment, Metric) tuple per
        /// sector - see this class's call sites for the real-data-vs-stylized breakdown of each one.
        /// Round 3 item 4 refactored this from 12 flat positional float parameters (one triplet per
        /// sector) to a tuple array specifically because doubling the sector count to eight would have
        /// pushed that flat-parameter signature to 24 same-typed floats - a genuine maintainability
        /// problem, not a stylistic preference, since a single misplaced argument in a 24-float call
        /// would silently seed the wrong sector with no compiler error.
        /// </summary>
        private static void SeedSectors(Country country, params (SectorType Type, float Output, float Employment, float Metric)[] sectors)
        {
            foreach (var sector in sectors)
            {
                country.Sectors.Add(new Sector(sector.Type, sector.Output, sector.Employment, sector.Metric));
            }
        }

        /// <summary>
        /// Phase 1 of the detailed spending portfolio (see CLAUDE.md's "Detailed Spending Portfolio")
        /// - USA only for now; the other five countries keep the legacy GovernmentSpendingRate +
        /// PolicyDecision category-delta mechanism unchanged (an empty SpendingLines list is the
        /// switch SimulationManager.ApplyDomesticPolicy checks). Amounts are real approximate FY2025
        /// federal dollar figures ($B, same scale as GDP) - directionally realistic, not exact
        /// appropriations. InterestOnDebt deliberately has no line here - it stays
        /// SimulationManager's existing automatic GetInterestOnDebt calculation.
        /// </summary>
        private static void SeedUsaSpendingLines(Country usa)
        {
            usa.SpendingLines.AddRange(new[]
            {
                // Mandatory - entitlement/transfer programs, excluded from the G term (transfers, not
                // purchases - same reasoning as UnemploymentBenefitCost/InterestOnDebt). Adjustable via
                // PolicyDecision.SpendingLineChanges within a narrower percentage range than
                // Discretionary, and weighted by a distinctly higher approval-rating penalty per
                // relative size - see MacroSystem.MandatorySpendingApprovalMultiplier.
                new SpendingLine(SpendingCategory.SocialSecurity, 1530f, isMandatory: true),
                new SpendingLine(SpendingCategory.Medicare, 875f, isMandatory: true),
                new SpendingLine(SpendingCategory.Medicaid, 620f, isMandatory: true),
                new SpendingLine(SpendingCategory.IncomeSecurity, 700f, isMandatory: true),
                new SpendingLine(SpendingCategory.VeteransBenefitsMandatory, 130f, isMandatory: true),
                new SpendingLine(SpendingCategory.FederalRetirement, 155f, isMandatory: true),

                // Discretionary - sum feeds the G term; player-adjustable via PolicyDecision.SpendingLineChanges.
                new SpendingLine(SpendingCategory.Defense, 850f, isMandatory: false),
                new SpendingLine(SpendingCategory.VeteransAffairsDiscretionary, 135f, isMandatory: false),
                new SpendingLine(SpendingCategory.Transportation, 105f, isMandatory: false),
                new SpendingLine(SpendingCategory.HHSDiscretionary, 130f, isMandatory: false),
                new SpendingLine(SpendingCategory.HomelandSecurity, 100f, isMandatory: false),
                new SpendingLine(SpendingCategory.Education, 80f, isMandatory: false),
                new SpendingLine(SpendingCategory.Energy, 50f, isMandatory: false),
                new SpendingLine(SpendingCategory.Housing, 70f, isMandatory: false),
                new SpendingLine(SpendingCategory.Justice, 40f, isMandatory: false),
                new SpendingLine(SpendingCategory.StateForeignAffairs, 60f, isMandatory: false),
                new SpendingLine(SpendingCategory.Agriculture, 25f, isMandatory: false),
                new SpendingLine(SpendingCategory.Interior, 17f, isMandatory: false),
                new SpendingLine(SpendingCategory.NASA, 25f, isMandatory: false),
                new SpendingLine(SpendingCategory.Commerce, 16f, isMandatory: false),
                new SpendingLine(SpendingCategory.Labor, 13f, isMandatory: false),
                new SpendingLine(SpendingCategory.TreasuryOps, 15f, isMandatory: false),
                new SpendingLine(SpendingCategory.NSF, 9f, isMandatory: false),
                new SpendingLine(SpendingCategory.EPA, 10f, isMandatory: false),
                new SpendingLine(SpendingCategory.SBA, 1f, isMandatory: false),
            });
        }

        /// <summary>
        /// Playtest-2 item 4 (ruled 2026-08-25): Sweden's REAL budget structure - the 27
        /// utgiftsomraden of the state budget, consolidated to 24 lines at USA's granularity - as a
        /// PURE DECOMPOSITION of the country's existing GDP x GovernmentSpendingRate total
        /// (SeedGenericSpendingLines' exact-sum invariant kept: every line is the game total times
        /// the area's share of the sourced SEK sum, and the largest line, MunicipalGrants, is the
        /// REMAINDER, so the set sums to exactly the old total and the trajectory bar holds
        /// byte-identically).
        ///
        /// SOURCE (rules 5/9/12 - sourced, dated, basis stated): regeringen.se "Statens budget i
        /// siffror", per-utgiftsomrade prognosis from the 2026 ekonomiska varpropositionen, rounded
        /// billions of SEK, retrieved 2026-08-25. The SEK figures are share weights only - the game
        /// total stays the country's own (the ruled decomposition-not-recalibration split; the
        /// level question is the later ruled recalibration pass, which also owns the revenue-side
        /// seed artifact recorded in "Live playtest 2").
        ///
        /// Consolidations and exclusions, stated: UO26 statsskuldsrantor (26 bn) is EXCLUDED -
        /// interest has no line, it stays SimulationManager's automatic GetInterestOnDebt exactly
        /// as USA's own seed rules; UO5 Internationell samverkan (3) folds into UO7 bistand as
        /// InternationalAid (49); UO18 Samhallsplanering (2) + UO19 Regional utveckling (5) fold
        /// into RegionalPlanningAndDevelopment (7). Alderspensionssystemet sits outside the state
        /// budget in reality and outside this seed too.
        ///
        /// ✅ THE FLAGS FLIPPED IN THE RECALIBRATION PASS, as the item-4 ruling recorded they
        /// would ("the flags flip in the recalibration pass under the full sim-math bar" -
        /// terminal ruling confirmed 2026-08-26): UO10 (sickness/disability), UO11 (old-age) and
        /// UO12 (family/children) - the state budget's three cash-transfer systems, 284 of 1,314
        /// bn SEK - are now MANDATORY lines: they leave the national-accounts G term (transfers,
        /// not purchases), grow on the mandatory path, take demographic pressure, and carry the
        /// mandatory approval weighting. G consequently falls 26% -> ~20.38% of GDP, a real
        /// identity change this pass's fresh baselines measure rather than hide.
        ///
        /// Two RECALIBRATION additions beyond the state budget (both mandatory, sources dated):
        /// the SocialSecurity line is TOPPED UP at construction to Sweden's real total old-age
        /// cash benefits - 7.0% of GDP (Eurostat gov_10a_exp GF10.02/D62, 2024, API vintage
        /// 2026-07-21) - because UO11 (garantipension etc., ~1.19% of GDP) is only the state
        /// budget's slice of a pension system that mostly sits OUTSIDE it (the inkomstpension/AP
        /// system this class's own SWF comment describes); and an IncomeSecurity residual line
        /// lands the year-1 primary balance on Sweden's real 2025 structural position (-0.7% of
        /// GDP: Eurostat April-2026 notification deficit -1.3, ECB GFS D.41 interest 0.61).
        /// ApplyDemographicPensionPressure targets SocialSecurity first, so Sweden's aging
        /// channel points at the real, full-sized pension line - still inert while births
        /// (10.8) exceed deaths (9.5), armed for the day the demographics turn.
        /// </summary>
        private static void SeedSwedenSpendingLines(Country sweden)
        {
            float total = sweden.State.GDP * (sweden.GovernmentSpendingRate / 100f);

            // (category, 2026 prognosis in bn SEK) - every utgiftsomrade except the remainder line.
            var areas = new (SpendingCategory Category, float SekBillion)[]
            {
                (SpendingCategory.CentralGovernment, 22f),              // UO1 Rikets styrelse
                (SpendingCategory.FinancialAdministration, 22f),        // UO2 Samhallsekonomi och finansforvaltning
                (SpendingCategory.TaxAdministration, 16f),              // UO3 Skatt, tull och exekution
                (SpendingCategory.Justice, 94f),                        // UO4 Rattsvasendet
                (SpendingCategory.Defense, 221f),                       // UO6 Forsvar och samhallets krisberedskap
                (SpendingCategory.InternationalAid, 49f),               // UO7 Internationellt bistand (46) + UO5 Internationell samverkan (3)
                (SpendingCategory.Migration, 13f),                      // UO8 Migration
                (SpendingCategory.HealthcareAndSocialCare, 127f),       // UO9 Halsovard, sjukvard och social omsorg
                (SpendingCategory.SicknessAndDisability, 122f),         // UO10 Ekonomisk trygghet vid sjukdom och funktionsnedsattning
                (SpendingCategory.SocialSecurity, 60f),                 // UO11 Ekonomisk trygghet vid alderdom (see doc comment)
                (SpendingCategory.FamilyAndChildren, 102f),             // UO12 Ekonomisk trygghet for familjer och barn
                (SpendingCategory.IntegrationAndEquality, 6f),          // UO13 Integration och jamstalldhet
                (SpendingCategory.LaborMarket, 90f),                    // UO14 Arbetsmarknad och arbetsliv
                (SpendingCategory.StudentAid, 31f),                     // UO15 Studiestod
                (SpendingCategory.Education, 105f),                     // UO16 Utbildning och universitetsforskning
                (SpendingCategory.CultureAndMedia, 17f),                // UO17 Kultur, medier, trossamfund och fritid
                (SpendingCategory.RegionalPlanningAndDevelopment, 7f),  // UO18 Samhallsplanering (2) + UO19 Regional utveckling (5)
                (SpendingCategory.ClimateAndEnvironment, 18f),          // UO20 Klimat, miljo och natur
                (SpendingCategory.Energy, 10f),                         // UO21 Energi
                (SpendingCategory.Transportation, 104f),                // UO22 Kommunikationer
                (SpendingCategory.Agriculture, 22f),                    // UO23 Areella naringar, landsbygd och livsmedel
                (SpendingCategory.BusinessAndIndustry, 9f),             // UO24 Naringsliv
                (SpendingCategory.EuMembershipFee, 56f),                // UO27 Avgiften till Europeiska unionen
            };
            const float MunicipalGrantsSekBillion = 181f;               // UO25 Allmanna bidrag till kommuner - the remainder line

            float sekSum = MunicipalGrantsSekBillion;
            foreach ((SpendingCategory _, float sek) in areas)
            {
                sekSum += sek;
            }

            // RECALIBRATION constants (terminal rulings 2026-08-26; derivations in the class doc
            // comment). The pension top-up raises SocialSecurity from UO11's state-budget slice
            // (~1.19% of GDP) to Sweden's REAL total old-age cash benefits, 7.0% of GDP
            // (Eurostat gov_10a_exp GF10.02/D62 2024): top-up = 7.0 - 26*(60/1314) = 5.8128.
            // The residual transfer line solves the year-1 primary balance to Sweden's real 2025
            // structural -0.7% of GDP given the recalibrated revenue target (42.2%), the fund's
            // structural draw (~0.94%), benefits (2.0%) and the contribution (0.3%).
            const float OutOfBudgetPensionPercentOfGdp = 5.8128f;
            const float ResidualTransfersPercentOfGdp = 9.73f;

            float allocated = 0f;
            foreach ((SpendingCategory category, float sek) in areas)
            {
                float amount = total * (sek / sekSum);
                allocated += amount;

                // The flipped transfer systems (the item-4 ruling, executed this pass): UO10
                // sickness/disability, UO11 old-age, UO12 family/children are MANDATORY - they
                // leave G. Every other utgiftsomrade stays discretionary (state consumption).
                bool mandatory = category == SpendingCategory.SicknessAndDisability
                    || category == SpendingCategory.SocialSecurity
                    || category == SpendingCategory.FamilyAndChildren;

                if (category == SpendingCategory.SocialSecurity)
                {
                    // Constructed at the full real size rather than mutated afterwards, so
                    // SeedAmount (the player-change clamp anchor) is the honest figure too.
                    amount += sweden.State.GDP * OutOfBudgetPensionPercentOfGdp / 100f;
                }

                sweden.SpendingLines.Add(new SpendingLine(category, amount, mandatory));
            }

            // The remainder line keeps the STATE-BUDGET exact-sum invariant: MunicipalGrants is
            // total-minus-allocated where 'allocated' counts only the SEK-derived amounts (the
            // pension top-up sits deliberately outside the state-budget decomposition).
            sweden.SpendingLines.Add(new SpendingLine(SpendingCategory.MunicipalGrants, total - allocated, isMandatory: false));

            // The recalibration residual: general-government cash transfers outside the state
            // budget and the pension system (municipal-sector-funded transfers and the rest),
            // sized so the year-1 primary balance lands on the real -0.7% of GDP.
            sweden.SpendingLines.Add(new SpendingLine(SpendingCategory.IncomeSecurity, sweden.State.GDP * ResidualTransfersPercentOfGdp / 100f, isMandatory: true));
        }

        /// <summary>
        /// Country-selection task, Part 2: a SMALL, generic 5-category spending decomposition for a
        /// country that (unlike USA) keeps the legacy GovernmentSpendingRate mechanic as its source of
        /// truth - mirrors USA's own original Phase 1 broad-categories stage, not the later detailed
        /// work. CRITICAL invariant: this is a PURE decomposition, not a recalibration - the five
        /// lines' Amounts are computed directly from the country's OWN CURRENT GDP *
        /// GovernmentSpendingRate (read live at seed time, never a separately-hardcoded duplicate
        /// figure that could drift out of sync), and Administration is deliberately the REMAINDER
        /// (total minus the other four), not its own independently-rounded percentage - this
        /// guarantees the five lines sum to EXACTLY the country's existing total regardless of
        /// floating-point rounding in the other four percentages, not just approximately. All five
        /// lines are Discretionary (feed G, like USA's own Discretionary lines) - no Mandatory/
        /// Discretionary split was introduced for this small decomposition (see this method's call
        /// site in CreateDefault for why). Only Defense and InfrastructureAndDevelopment feed an
        /// existing economic effect (see SimulationManager.BuildEffectiveDecisionForDetailedSpending) -
        /// SocialPrograms/PublicServices/Administration get zero effect for now, an accurate,
        /// adjustable dollar amount only, deliberately mirroring how 15 of USA's own 19 Discretionary
        /// categories still have no effect either.
        /// </summary>
        private static void SeedGenericSpendingLines(Country country, float socialPercent, float defensePercent, float infrastructurePercent, float publicServicesPercent)
        {
            float total = country.State.GDP * (country.GovernmentSpendingRate / 100f);
            float social = total * socialPercent / 100f;
            float defense = total * defensePercent / 100f;
            float infrastructure = total * infrastructurePercent / 100f;
            float publicServices = total * publicServicesPercent / 100f;
            float administration = total - social - defense - infrastructure - publicServices;

            country.SpendingLines.AddRange(new[]
            {
                new SpendingLine(SpendingCategory.SocialPrograms, social, isMandatory: false),
                new SpendingLine(SpendingCategory.Defense, defense, isMandatory: false),
                new SpendingLine(SpendingCategory.InfrastructureAndDevelopment, infrastructure, isMandatory: false),
                new SpendingLine(SpendingCategory.PublicServices, publicServices, isMandatory: false),
                new SpendingLine(SpendingCategory.Administration, administration, isMandatory: false),
            });
        }

        /// <summary>
        /// RECALIBRATION (build-order item 1, terminal rulings 2026-08-26): the two-line mandatory
        /// transfer block for a generic-decomposition country - the general-government
        /// cash-transfer layer its seed previously omitted (measured cost: year-1 primary
        /// surpluses of +14..+22% of GDP, the fiscal reaction multiplier crushed to 0.58-0.76
        /// permanently compensating - FiscalRecalDiagnostic, seed 777, pre-recalibration run).
        ///
        /// SocialSecurity carries REAL old-age cash benefits - Eurostat `gov_10a_exp`, COFOG
        /// GF10.02 "Old age", na_item D62 (social benefits in cash - deliberately NOT the
        /// function's total expenditure, which bundles in-kind elderly care that belongs to
        /// government consumption), % of GDP, 2024, API vintage 2026-07-21; Germany and France
        /// carry Eurostat flag `p`. This is also ApplyDemographicPensionPressure's target
        /// category, so each country's aging channel now points at its real pension bill.
        /// IncomeSecurity is the RESIDUAL, solved (class doc comment) so the year-1 primary
        /// balance lands on the country's real 2025 structural position. Mandatory lines are
        /// transfers: excluded from the national-accounts G term, narrower player adjustment
        /// range, higher approval weight per relative change - the USA seed's own shape, now on
        /// all six countries. The later ruled decomposition passes split these blocks into real
        /// per-country structure; until then, two honestly-sourced lines beat twenty invented ones.
        /// </summary>
        private static void SeedMandatoryTransferLines(Country country, float socialSecurityPercent, float incomeSecurityPercent)
        {
            float gdp = country.State.GDP;
            country.SpendingLines.Add(new SpendingLine(SpendingCategory.SocialSecurity, gdp * socialSecurityPercent / 100f, isMandatory: true));
            country.SpendingLines.Add(new SpendingLine(SpendingCategory.IncomeSecurity, gdp * incomeSecurityPercent / 100f, isMandatory: true));
        }
    }
}
