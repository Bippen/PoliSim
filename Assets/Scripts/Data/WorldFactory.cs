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
                    potentialGdp: 33260f, governmentDebt: 29000f * 1.24f, povertyRate: 18f, laborForceParticipationRate: 62.5f, crimeIndex: 45f),
                usDollarZone, baseTariffRate: 3f,
                naturalUnemploymentRate: 4.0f, potentialGrowthRate: 2.0f, governmentSpendingRate: 17f, benefitRatePerUnemployed: 0.10f);

            var sweden = new Country(
                CountryId.Sweden, "Sweden",
                new EconomyState(gdp: 620f, inflation: 2.0f, unemployment: 8.0f, approvalRating: 50f, budget: 0f,
                    governmentDebt: 620f * 0.35f, povertyRate: 9f, laborForceParticipationRate: 72.6f, crimeIndex: 30f),
                swedishKronaZone, baseTariffRate: 1f,
                naturalUnemploymentRate: 6.5f, potentialGrowthRate: 1.5f, governmentSpendingRate: 26f, benefitRatePerUnemployed: 0.25f);

            var germany = new Country(
                CountryId.Germany, "Germany",
                new EconomyState(gdp: 4700f, inflation: 3.0f, unemployment: 3.5f, approvalRating: 50f, budget: 0f,
                    governmentDebt: 4700f * 0.63f, povertyRate: 11f, laborForceParticipationRate: 61.7f, crimeIndex: 25f),
                eurozone, baseTariffRate: 1f,
                naturalUnemploymentRate: 3.3f, potentialGrowthRate: 0.8f, governmentSpendingRate: 21f, benefitRatePerUnemployed: 0.20f);

            var france = new Country(
                CountryId.France, "France",
                new EconomyState(gdp: 3200f, inflation: 3.0f, unemployment: 7.3f, approvalRating: 50f, budget: 0f,
                    governmentDebt: 3200f * 1.16f, povertyRate: 8f, laborForceParticipationRate: 56.0f, crimeIndex: 30f),
                eurozone, baseTariffRate: 1f,
                naturalUnemploymentRate: 7.5f, potentialGrowthRate: 0.8f, governmentSpendingRate: 24f, benefitRatePerUnemployed: 0.22f);

            var italy = new Country(
                CountryId.Italy, "Italy",
                new EconomyState(gdp: 2300f, inflation: 3.0f, unemployment: 7.8f, approvalRating: 50f, budget: 0f,
                    governmentDebt: 2300f * 1.38f, povertyRate: 14f, laborForceParticipationRate: 49.8f, crimeIndex: 18f),
                eurozone, baseTariffRate: 1f,
                naturalUnemploymentRate: 8.0f, potentialGrowthRate: 0.8f, governmentSpendingRate: 19f, benefitRatePerUnemployed: 0.18f);

            var poland = new Country(
                CountryId.Poland, "Poland",
                new EconomyState(gdp: 840f, inflation: 2.2f, unemployment: 5.4f, approvalRating: 50f, budget: 0f,
                    governmentDebt: 840f * 0.59f, povertyRate: 10f, laborForceParticipationRate: 58.5f, crimeIndex: 20f),
                polishZlotyZone, baseTariffRate: 1f,
                naturalUnemploymentRate: 5.0f, potentialGrowthRate: 3.5f, governmentSpendingRate: 18f, benefitRatePerUnemployed: 0.12f);

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
            SeedSectors(usa,
                manufacturingOutput: 10.0f, manufacturingEmployment: 8.0f, manufacturingMetric: 77f,
                technologyOutput: 10.0f, technologyEmployment: 4.0f, technologyMetric: 78f,
                agricultureOutput: 1.0f, agricultureEmployment: 1.5f, agricultureMetric: 20f,
                financeOutput: 8.0f, financeEmployment: 4.0f, financeMetric: 4f);
            SeedSectors(sweden,
                manufacturingOutput: 12.6f, manufacturingEmployment: 11.0f, manufacturingMetric: 78f,
                technologyOutput: 8.0f, technologyEmployment: 5.0f, technologyMetric: 80f,
                agricultureOutput: 1.4f, agricultureEmployment: 1.8f, agricultureMetric: 25f,
                financeOutput: 6.0f, financeEmployment: 2.5f, financeMetric: 3.5f);
            SeedSectors(germany,
                manufacturingOutput: 19.9f, manufacturingEmployment: 18.0f, manufacturingMetric: 79f,
                technologyOutput: 5.5f, technologyEmployment: 3.0f, technologyMetric: 70f,
                agricultureOutput: 0.8f, agricultureEmployment: 1.2f, agricultureMetric: 30f,
                financeOutput: 4.0f, financeEmployment: 2.5f, financeMetric: 3f);
            SeedSectors(france,
                manufacturingOutput: 10.7f, manufacturingEmployment: 10.0f, manufacturingMetric: 76f,
                technologyOutput: 5.0f, technologyEmployment: 2.8f, technologyMetric: 68f,
                agricultureOutput: 1.6f, agricultureEmployment: 2.5f, agricultureMetric: 35f,
                financeOutput: 4.0f, financeEmployment: 2.8f, financeMetric: 3.5f);
            SeedSectors(italy,
                manufacturingOutput: 16.6f, manufacturingEmployment: 15.0f, manufacturingMetric: 75f,
                technologyOutput: 3.5f, technologyEmployment: 2.0f, technologyMetric: 55f,
                agricultureOutput: 2.1f, agricultureEmployment: 3.8f, agricultureMetric: 25f,
                financeOutput: 5.5f, financeEmployment: 2.5f, financeMetric: 2.5f);
            SeedSectors(poland,
                manufacturingOutput: 18.1f, manufacturingEmployment: 17.0f, manufacturingMetric: 78f,
                technologyOutput: 5.0f, technologyEmployment: 3.0f, technologyMetric: 58f,
                agricultureOutput: 2.4f, agricultureEmployment: 8.0f, agricultureMetric: 40f,
                financeOutput: 4.0f, financeEmployment: 2.0f, financeMetric: 5f);

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

        /// <summary>Seeds all four Sectors for a country - see this class's call sites for the real-data-vs-stylized breakdown of each argument.</summary>
        private static void SeedSectors(Country country,
            float manufacturingOutput, float manufacturingEmployment, float manufacturingMetric,
            float technologyOutput, float technologyEmployment, float technologyMetric,
            float agricultureOutput, float agricultureEmployment, float agricultureMetric,
            float financeOutput, float financeEmployment, float financeMetric)
        {
            country.Sectors.AddRange(new[]
            {
                new Sector(SectorType.Manufacturing, manufacturingOutput, manufacturingEmployment, manufacturingMetric),
                new Sector(SectorType.Technology, technologyOutput, technologyEmployment, technologyMetric),
                new Sector(SectorType.Agriculture, agricultureOutput, agricultureEmployment, agricultureMetric),
                new Sector(SectorType.Finance, financeOutput, financeEmployment, financeMetric),
            });
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
    }
}
