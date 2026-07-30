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
    /// revenue-to-GDP (theoretical revenue-to-GDP * CollectionEfficiency) lands close to that
    /// country's real-world tax-to-GDP target:
    /// <code>
    /// Country   Implied (Rate*BaseShareOfGdp summed)   Target   CollectionEfficiency = Target/Implied
    /// USA       29.37%                                 18.0%    18.0 / 29.37 = 0.6129
    /// Germany   48.73%                                 38%      38 / 48.73   = 0.7799
    /// France    60.45%                                 45%      45 / 60.45   = 0.7444
    /// Italy     45.10%                                 43%      43 / 45.10   = 0.9534
    /// Poland    42.10%                                 37%      37 / 42.10   = 0.8789
    /// Sweden    53.45%                                 41%      41 / 53.45   = 0.7671
    /// </code>
    /// USA's target is FEDERAL-government-only revenue-to-GDP (~18%, real FY2025 federal revenue
    /// $5.235T against this game's ~$29,000B starting GDP) rather than the general-government
    /// (federal+state+local) figure used for the other five - the US has a genuinely decentralized
    /// state/local fiscal layer this game doesn't model at all, so a general-government target would
    /// overstate what this sim's single national CollectionEfficiency dial should represent for the
    /// USA specifically. Germany/France/Italy/Poland/Sweden keep their general-government targets
    /// since their fiscal policy is comparatively centralized at the national level and this game has
    /// no separate state/local layer to misattribute revenue to either way.
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
                    potentialGdp: 33260f, governmentDebt: 29000f * 1.24f, povertyRate: 18f, laborForceParticipationRate: 62.5f, crimeIndex: 45f, prisonPopulationRate: 531f, organizedCrimeIndex: 35f, corruptionIndex: 31f),
                usDollarZone, baseTariffRate: 3f,
                naturalUnemploymentRate: 4.0f, potentialGrowthRate: 2.0f, governmentSpendingRate: 17f, benefitRatePerUnemployed: 0.10f);

            var sweden = new Country(
                CountryId.Sweden, "Sweden",
                new EconomyState(gdp: 620f, inflation: 2.0f, unemployment: 8.0f, approvalRating: 50f, budget: 0f,
                    governmentDebt: 620f * 0.35f, povertyRate: 9f, laborForceParticipationRate: 72.6f, crimeIndex: 30f, prisonPopulationRate: 60f, organizedCrimeIndex: 32f, corruptionIndex: 18f),
                swedishKronaZone, baseTariffRate: 1f,
                naturalUnemploymentRate: 6.5f, potentialGrowthRate: 1.5f, governmentSpendingRate: 26f, benefitRatePerUnemployed: 0.25f);

            var germany = new Country(
                CountryId.Germany, "Germany",
                new EconomyState(gdp: 4700f, inflation: 3.0f, unemployment: 3.5f, approvalRating: 50f, budget: 0f,
                    governmentDebt: 4700f * 0.63f, povertyRate: 11f, laborForceParticipationRate: 61.7f, crimeIndex: 25f, prisonPopulationRate: 72f, organizedCrimeIndex: 20f, corruptionIndex: 22f),
                eurozone, baseTariffRate: 1f,
                naturalUnemploymentRate: 3.3f, potentialGrowthRate: 0.8f, governmentSpendingRate: 21f, benefitRatePerUnemployed: 0.20f);

            var france = new Country(
                CountryId.France, "France",
                new EconomyState(gdp: 3200f, inflation: 3.0f, unemployment: 7.3f, approvalRating: 50f, budget: 0f,
                    governmentDebt: 3200f * 1.16f, povertyRate: 8f, laborForceParticipationRate: 56.0f, crimeIndex: 30f, prisonPopulationRate: 111f, organizedCrimeIndex: 28f, corruptionIndex: 30f),
                eurozone, baseTariffRate: 1f,
                naturalUnemploymentRate: 7.5f, potentialGrowthRate: 0.8f, governmentSpendingRate: 24f, benefitRatePerUnemployed: 0.22f);

            var italy = new Country(
                CountryId.Italy, "Italy",
                new EconomyState(gdp: 2300f, inflation: 3.0f, unemployment: 7.8f, approvalRating: 50f, budget: 0f,
                    governmentDebt: 2300f * 1.38f, povertyRate: 14f, laborForceParticipationRate: 49.8f, crimeIndex: 18f, prisonPopulationRate: 92f, organizedCrimeIndex: 55f, corruptionIndex: 44f),
                eurozone, baseTariffRate: 1f,
                naturalUnemploymentRate: 8.0f, potentialGrowthRate: 0.8f, governmentSpendingRate: 19f, benefitRatePerUnemployed: 0.18f);

            var poland = new Country(
                CountryId.Poland, "Poland",
                new EconomyState(gdp: 840f, inflation: 2.2f, unemployment: 5.4f, approvalRating: 50f, budget: 0f,
                    governmentDebt: 840f * 0.59f, povertyRate: 10f, laborForceParticipationRate: 58.5f, crimeIndex: 20f, prisonPopulationRate: 185f, organizedCrimeIndex: 22f, corruptionIndex: 40f),
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
            // this class's doc comment for the full per-country derivation table.
            usa.CollectionEfficiency = 0.6129f; // 18.0 / 29.37 (federal-only target, not general-government)
            germany.CollectionEfficiency = 0.7799f;
            france.CollectionEfficiency = 0.7444f;
            italy.CollectionEfficiency = 0.9534f;
            poland.CollectionEfficiency = 0.8789f;
            sweden.CollectionEfficiency = 0.7671f;

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
            SeedGenericSpendingLines(sweden, socialPercent: 42f, defensePercent: 4f, infrastructurePercent: 14f, publicServicesPercent: 25f);
            SeedGenericSpendingLines(germany, socialPercent: 40f, defensePercent: 5f, infrastructurePercent: 13f, publicServicesPercent: 24f);
            SeedGenericSpendingLines(france, socialPercent: 38f, defensePercent: 7f, infrastructurePercent: 12f, publicServicesPercent: 25f);
            SeedGenericSpendingLines(italy, socialPercent: 40f, defensePercent: 4f, infrastructurePercent: 11f, publicServicesPercent: 26f);
            SeedGenericSpendingLines(poland, socialPercent: 34f, defensePercent: 10f, infrastructurePercent: 16f, publicServicesPercent: 22f);

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
    }
}
