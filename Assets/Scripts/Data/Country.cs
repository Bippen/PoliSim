using System;
using System.Collections.Generic;

namespace PoliSim.Data
{
    /// <summary>
    /// A playable country: identity, current economic/political state, the currency zone it
    /// belongs to, and its bilateral trade relationships.
    /// </summary>
    [Serializable]
    public class Country
    {
        public CountryId Id;
        public string Name;
        public EconomyState State;
        public CurrencyZone CurrencyZone;
        public List<TradePartner> TradePartners = new List<TradePartner>();

        /// <summary>This country's fiscal portfolio - which taxes are implemented and at what rate. See TaxLine and SimulationManager.GetTotalTaxRevenue.</summary>
        public List<TaxLine> TaxLines = new List<TaxLine>();

        /// <summary>
        /// This country's welfare portfolio - which anti-poverty programs are implemented and at what
        /// GenerosityLevel, mirroring TaxLines' implement/adjust/remove pattern exactly. None
        /// implemented by default for any country (see WorldFactory) - see WelfareProgram and
        /// SimulationManager.GetTotalWelfareCost/ApplyWelfareGenerosityChanges.
        /// </summary>
        public List<WelfareProgram> WelfarePrograms = new List<WelfareProgram>();

        /// <summary>
        /// This country's economic sector breakdown (Manufacturing/Technology/Agriculture/Finance) -
        /// a small proof-of-pattern slice, present for all six countries (unlike TaxLines/
        /// WelfarePrograms, there's no implement/remove - every country has all four sectors always).
        /// See Sector.cs and MacroSystem.ApplySectorEffects.
        /// </summary>
        public List<Sector> Sectors = new List<Sector>();

        /// <summary>
        /// This country's four tracked infrastructure types (Roads/Rail/PowerGrid/Broadband) -
        /// present for all six countries always (the same "no implement/remove" idiom Sectors
        /// already established). See InfrastructureAsset.cs and MacroSystem.ApplyInfrastructureCondition.
        /// </summary>
        public List<InfrastructureAsset> InfrastructureAssets = new List<InfrastructureAsset>();

        /// <summary>
        /// This country's detailed spending portfolio (Phase 1: USA only - see CLAUDE.md's "Detailed
        /// Spending Portfolio"). Empty for a country means it still uses the legacy
        /// GovernmentSpendingRate + PolicyDecision's four category-delta fields mechanism unchanged -
        /// see SimulationManager.ApplyDomesticPolicy's hasDetailedSpending branch.
        /// </summary>
        public List<SpendingLine> SpendingLines = new List<SpendingLine>();

        /// <summary>
        /// This country's own tariff policy toward imports, used only when it is not a member of
        /// any trade bloc (bloc members instead apply their bloc's common external/internal rates).
        /// </summary>
        public float BaseTariffRate;

        /// <summary>
        /// NAIRU - the non-accelerating-inflation rate of unemployment, a structural per-country
        /// constant. The Phillips Curve compares actual unemployment against this. See
        /// MacroSystem.ApplyPhillipsCurveInflation.
        /// </summary>
        public float NaturalUnemploymentRate;

        /// <summary>
        /// Trend/potential GDP growth rate, in percent per turn, used by Okun's Law (actual vs.
        /// potential growth) and to grow PotentialGDP each turn. Recomputed every turn as
        /// BasePotentialGrowthRate plus the combined, separately-ceilinged Infrastructure/Sector
        /// growth adjustments (see MacroSystem.ApplyInfrastructureGrowthEffect /
        /// ApplySectorGrowthEffect) - not itself a structural constant any more, though it behaves
        /// like one absent any active adjustment.
        /// </summary>
        public float PotentialGrowthRate;

        /// <summary>
        /// The country's ORIGINAL, un-adjusted trend GDP growth rate, exactly as seeded in
        /// WorldFactory - never mutated by any policy/condition/performance effect. PotentialGrowthRate
        /// itself is recomputed each turn as this base plus the combined infrastructure/sector
        /// adjustments - keeping the true structural anchor separate from those adjustments is what
        /// lets each adjustment's own combined ceiling be enforced against a fixed reference point
        /// rather than a value the adjustments themselves could otherwise drift.
        /// </summary>
        public float BasePotentialGrowthRate;

        /// <summary>
        /// Accumulated PotentialGrowthRate boost earned from sustained Infrastructure-category
        /// spending increases (see MacroSystem.ApplyInfrastructureGrowthEffect) - a lasting,
        /// ratcheting investment effect, only ever non-negative, clamped to its own
        /// [0, MaxInfrastructureSpendingBoost] range. Combined with the live, non-accumulating
        /// Infrastructure-condition drag under one shared ceiling before being added to
        /// BasePotentialGrowthRate - see "Infrastructure Feedback" in CLAUDE.md.
        /// </summary>
        public float InfrastructureSpendingGrowthAdjustment;

        /// <summary>
        /// Baseline government consumption expenditure, as a percentage of GDP - the structural
        /// share of the G term in GDP = C + I + G + NX. PolicyDecision.TotalDiscretionarySpending
        /// (the sum of the four spending categories) is added on top of this as a discretionary
        /// delta, not used in place of it.
        /// </summary>
        public float GovernmentSpendingRate;

        /// <summary>
        /// Automatic stabilizer: unemployment benefit cost as a percentage of GDP per percentage
        /// point of unemployment (e.g. 0.20 means 1 point of unemployment costs 0.20% of GDP in
        /// benefits) - a structural per-country constant reflecting how generous the welfare state
        /// is. See SimulationManager.GetUnemploymentBenefitCost.
        /// </summary>
        public float BenefitRatePerUnemployed;

        /// <summary>
        /// How much of the theoretical tax base is actually collected (0.0-1.0), reflecting
        /// enforcement quality, the size of the informal economy, and evasion - a structural
        /// per-country constant. Applied as a multiplier in SimulationManager.ApplyRevenueAndSpending
        /// (ActualRevenue = GetTotalTaxRevenue() * CollectionEfficiency), not inside
        /// GetTotalTaxRevenue itself, so that method still returns the theoretical figure. Calibrated
        /// per country in WorldFactory so the default tax portfolio's actual revenue-to-GDP lands
        /// close to that country's real-world tax-to-GDP ratio - see WorldFactory's doc comment for
        /// the derivation.
        /// </summary>
        public float CollectionEfficiency = 1f;

        /// <summary>
        /// Overrides SimulationManager.GetInterestOnDebt's base rate (in place of CurrencyZone.
        /// InterestRate) with this country's real blended average interest rate on existing debt,
        /// for a country where today's policy rate is a poor proxy for the average rate across its
        /// entire (long-duration, accumulated-over-many-years) debt stock - see
        /// "Reserve-Currency Debt Interest Treatment" in CLAUDE.md. -1 (the default) means unset -
        /// use CurrencyZone.InterestRate, unchanged behavior.
        /// </summary>
        public float BaseDebtInterestRateOverride = -1f;

        /// <summary>
        /// Scales GetDebtRiskPremium's output before it's added to the effective debt interest rate
        /// (1 = full market exposure, the default - unchanged behavior for every country except a
        /// reserve-currency issuer). A reserve-currency issuer (the USA) doesn't face the same market
        /// risk premium as other sovereigns at an equivalent debt-to-GDP ratio - who holds the debt
        /// and in what currency matters as much as the ratio itself - so its sensitivity is set near
        /// (not exactly) zero. See "Reserve-Currency Debt Interest Treatment" in CLAUDE.md.
        /// </summary>
        public float RiskPremiumSensitivity = 1f;

        /// <summary>
        /// This country's own fiscal-comfort anchor for SimulationManager.GetFiscalReactionMultiplier
        /// - the debt-to-GDP level at which the automatic fiscal reaction (see that method) is
        /// neutral. Above it, the reaction modestly tightens (raises effective revenue); below it, it
        /// loosens. A structural per-country constant, seeded in WorldFactory to match each country's
        /// own real-approximate starting debt-to-GDP ratio - reusing that already-researched figure,
        /// not a separately-tuned new one. See "Fiscal Reaction Function" in CLAUDE.md.
        /// </summary>
        public float ComfortableDebtToGdpPercent = 60f;

        /// <summary>
        /// This country's structural "steady-state" poverty rate - the PovertyRate MacroSystem.
        /// ApplyPovertyRate's baseline computes toward when Unemployment sits at NaturalUnemploymentRate
        /// and Inflation sits at TaylorRule.InflationTarget (i.e. the gaps that otherwise move the
        /// baseline are both zero). Seeded per country from real OECD relative-poverty-rate data (see
        /// WorldFactory) - the same figure EconomyState.PovertyRate is seeded to, so a new game opens
        /// with PovertyRate already at (or very near) its own baseline rather than an artificial
        /// turn-1 jump, the same "avoid a one-time shock" lesson "Turn-1 GDP Consistency" established.
        /// </summary>
        public float BaselinePovertyRate = 10f;

        /// <summary>
        /// This country's structural "steady-state" labor force participation rate - the target
        /// MacroSystem.ApplyLaborForceParticipationRate's mean-reversion moves EconomyState.
        /// LaborForceParticipationRate toward when Unemployment sits exactly at NaturalUnemploymentRate
        /// (i.e. the discouraged/encouraged-worker gap is zero). Seeded per country from real World
        /// Bank/OECD data (see WorldFactory) - the same figure EconomyState.LaborForceParticipationRate
        /// is seeded to, so a new game opens already at (or very near) its own baseline rather than an
        /// artificial turn-1 jump, the same "avoid a one-time shock" lesson "Turn-1 GDP Consistency"
        /// established for PovertyRate/BaselinePovertyRate.
        /// </summary>
        public float BaselineLaborForceParticipationRate = 62f;

        /// <summary>
        /// Whether this country has a statutory minimum wage at all - false for Sweden and Italy,
        /// matching real-world fact (both rely on sector-level collective bargaining instead of a
        /// legal minimum), true for the other four. A structural fact, not a player-togglable
        /// implement/remove switch like TaxLine/WelfareProgram - the player can only adjust the LEVEL
        /// where a statutory minimum already exists. See MinimumWagePercentOfMedian.
        /// </summary>
        public bool MinimumWageImplemented;

        /// <summary>
        /// This country's minimum wage expressed as a percentage of its median wage (the "Kaitz
        /// index" economists commonly use for cross-country comparison, e.g. France ~66%) rather
        /// than an absolute currency amount - keeps it comparable across countries with very
        /// different wage levels. Only meaningful when MinimumWageImplemented is true. Persistent,
        /// player-adjustable via PolicyDecision.MinimumWageOverride (an absolute target, the same
        /// "SET, not delta" idiom as TaxLine.Rate) - see SimulationManager.ApplyMinimumWageChange.
        /// </summary>
        public float MinimumWagePercentOfMedian;

        /// <summary>
        /// This country's minimum wage level (percent of median wage) at the start of the game -
        /// the anchor MacroSystem's minimum-wage employment/poverty effects measure the CURRENT
        /// MinimumWagePercentOfMedian against, not a universal constant (the same "gap versus a
        /// country-specific anchor" idiom ComfortableDebtToGdpPercent/BaselinePovertyRate already
        /// use). Seeded equal to the country's own starting MinimumWagePercentOfMedian, so a fresh
        /// game opens at zero gap (no effect) rather than an artificial turn-1 shock, and so the
        /// effect doesn't double-count against NaturalUnemploymentRate, which already reflects each
        /// country's real structural conditions including its actual minimum wage. 0 for a country
        /// with no statutory minimum wage (MinimumWageImplemented false).
        /// </summary>
        public float BaselineMinimumWagePercentOfMedian;

        /// <summary>
        /// This country's real statutory paid family/parental leave, in weeks (see WorldFactory for
        /// per-country sourcing - USA 0/Sweden 69/Germany 58 are directly confirmed via web search;
        /// France/Italy/Poland are directionally-informed estimates from general knowledge of each
        /// country's real statutory system, not individually confirmed to the same precision).
        /// Persistent, player-adjustable via PolicyDecision.PaidFamilyLeaveWeeksOverride (an absolute
        /// target, the same "SET, not delta" idiom as TaxLine.Rate) - see
        /// SimulationManager.ApplyLaborPolicyChanges.
        /// </summary>
        public float PaidFamilyLeaveWeeks;

        /// <summary>
        /// This country's paid-family-leave level at the start of the game - the anchor
        /// MacroSystem's paid-leave LaborForceParticipationRate/ApprovalRating effects measure the
        /// CURRENT PaidFamilyLeaveWeeks against, not a universal constant (the same "gap versus a
        /// country-specific anchor" idiom MinimumWage's own BaselineMinimumWagePercentOfMedian
        /// already uses). Seeded equal to the country's own starting PaidFamilyLeaveWeeks, so a fresh
        /// game opens at zero gap (no effect) rather than an artificial turn-1 shock.
        /// </summary>
        public float BaselinePaidFamilyLeaveWeeks;

        /// <summary>
        /// This country's overtime/working-hour regulation strictness, 0-100 (0 = unregulated/long
        /// hours allowed, 100 = strict caps; 50 = neutral - a uniform placeholder for every country,
        /// the same reasoning Country.PoliceFundingLevel already uses, since there's no single clean
        /// cross-country comparable "regulation strictness" index). Persistent, player-adjustable via
        /// PolicyDecision.OvertimeRegulationOverride. Its Unemployment effect (see
        /// MacroSystem.GetOvertimeUnemploymentAdjustment) represents ONE side of a genuinely contested
        /// real economic debate (the "work-sharing" argument behind France's 35-hour week) - honestly
        /// simplified, not a settled empirical fact.
        /// </summary>
        public float OvertimeRegulationLevel = 50f;

        /// <summary>
        /// This country's workforce retraining program level, 0-100 (50 = neutral placeholder for
        /// every country - no real cross-country comparable figure exists for this). Persistent,
        /// player-adjustable via PolicyDecision.RetrainingProgramOverride. Reduces Unemployment - the
        /// well-established real economic rationale that retraining eases job transitions.
        /// </summary>
        public float RetrainingProgramLevel = 50f;

        /// <summary>
        /// This country's structural "steady-state" CrimeIndex - the target MacroSystem.
        /// ApplyCrimeIndex's mean-reversion moves EconomyState.CrimeIndex toward absent any policy
        /// input (the same "avoid a turn-1 shock" anchor idiom BaselinePovertyRate/
        /// BaselineLaborForceParticipationRate already use). Seeded per country from a STYLIZED 0-100
        /// scale informed by real relative homicide-rate rankings (see WorldFactory) - not a literal
        /// transformation of any single real indicator, since "crime" as a broad concept has no single
        /// clean cross-country comparable metric the way poverty/labor-participation rates do.
        /// </summary>
        public float BaselineCrimeIndex = 25f;

        /// <summary>
        /// This country's relative police funding effort, 0-100 (50 = neutral/baseline for every
        /// country - a uniform placeholder, since there's no clean real-world cross-country "relative
        /// policing effort" figure to seed differently per country, unlike CrimeIndex itself).
        /// Persistent, player-adjustable via PolicyDecision.PoliceFundingOverride (an absolute target,
        /// the same "SET, not delta" idiom as TaxLine.Rate) - see
        /// SimulationManager.ApplyCrimePolicyChanges. Higher funding reduces CrimeIndex - see
        /// MacroSystem.ApplyCrimeIndex.
        /// </summary>
        public float PoliceFundingLevel = 50f;

        /// <summary>
        /// This country's sentencing policy, 0-100 (0 = lenient/rehabilitation-focused, 100 = harsh/
        /// punitive; 50 = neutral, the same uniform-placeholder reasoning as PoliceFundingLevel).
        /// Persistent, player-adjustable via PolicyDecision.SentencingSeverityOverride. Its effect on
        /// CrimeIndex is deliberately smaller than PoliceFundingLevel's - criminology research
        /// consistently finds the CERTAINTY of enforcement matters more for deterrence than the
        /// SEVERITY of punishment (see MacroSystem.ApplyCrimeIndex's doc comment).
        /// </summary>
        public float SentencingSeverity = 50f;

        /// <summary>
        /// This country's structural "steady-state" incarceration rate (per 100,000 population) -
        /// the target MacroSystem.ApplyPrisonPopulationRate's mean-reversion moves EconomyState.
        /// PrisonPopulationRate toward absent any policy input. Seeded per country from real World
        /// Prison Brief data (see WorldFactory) - the same figure EconomyState.PrisonPopulationRate
        /// is seeded to, so a new game opens already at (or very near) its own baseline.
        /// </summary>
        public float BaselinePrisonPopulationRate = 100f;

        /// <summary>
        /// This country's bail policy, 0-100 (0 = traditional cash bail, 100 = full bail reform/
        /// no-cash-bail; 50 = neutral placeholder for every country - bail-bond systems as such are
        /// most directly analogous to the US context, but this dial is kept universal, the same
        /// uniform-dial idiom PoliceFundingLevel/SentencingSeverity already use). Persistent, player-
        /// adjustable via PolicyDecision.BailReformOverride. Reduces PrisonPopulationRate (bail
        /// reform's primary real-world goal - reducing pretrial detention) and modestly increases
        /// CrimeIndex - a small, HONESTLY CONTESTED effect (see MacroSystem.ApplyCrimeIndex), the
        /// same "flag the real debate, don't pretend it's settled" treatment
        /// OvertimeRegulationLevel's own Unemployment effect already got in "Deeper Labor Market
        /// Policies".
        /// </summary>
        public float BailReformLevel = 50f;

        /// <summary>
        /// This country's drug policy, 0-100 (0 = decriminalized/harm-reduction, 100 = strict
        /// criminalization; 50 = neutral placeholder for every country - no clean cross-country
        /// comparable index exists for this). Persistent, player-adjustable via
        /// PolicyDecision.DrugPolicyOverride. Increases PrisonPopulationRate when stricter - the
        /// well-documented real link between strict drug enforcement and mass incarceration (the US
        /// "war on drugs" being the clearest real-world example) - and modestly increases
        /// ApprovalRating (a "tough on crime" political dynamic, small like PoliceFundingLevel's own
        /// political framing).
        /// </summary>
        public float DrugPolicyLevel = 50f;

        /// <summary>
        /// Round 3 item 3: this country's structural "steady-state" OrganizedCrimeIndex - the target
        /// MacroSystem.ApplyOrganizedCrimeIndex's mean-reversion moves EconomyState.OrganizedCrimeIndex
        /// toward absent any policy input (the same "avoid a turn-1 shock" anchor idiom
        /// BaselineCrimeIndex already uses). Seeded per country informed by the real Global Organized
        /// Crime Index (GI-TOC) - Italy's historic, extremely well-documented organized-crime
        /// organizations (Cosa Nostra, Camorra, 'Ndrangheta) give it high confidence as the clear
        /// highest of the six; Sweden's real, well-documented recent gang-violence surge (the same
        /// fact already informing its elevated BaselineCrimeIndex) justifies its own elevated figure.
        /// The remaining relative ordering (USA/France/Poland/Germany) is a directional, stylized
        /// estimate, not independently confirmed against a specific index-year - see WorldFactory.
        /// </summary>
        public float BaselineOrganizedCrimeIndex = 25f;

        /// <summary>
        /// Round 3 item 3: this country's structural "steady-state" CorruptionIndex - the target
        /// MacroSystem.ApplyCorruptionIndex's mean-reversion moves EconomyState.CorruptionIndex toward
        /// absent any policy input (the same anchor idiom BaselineCrimeIndex already uses). Higher =
        /// MORE corrupt (matching this project's own "higher = worse" convention for CrimeIndex/
        /// PrisonPopulationRate) - seeded as roughly 100 minus the real Transparency International
        /// Corruption Perceptions Index (CPI, itself 0-100 with higher = cleaner), not a literal
        /// year-specific score. Nordic/German clean-government reputation and Italy's comparatively
        /// lower CPI standing among Western European/G7 peers are both real and well-documented, high
        /// confidence; the exact relative ordering of Italy versus Poland specifically is a directional
        /// estimate, not confirmed against one index-year - see WorldFactory.
        /// </summary>
        public float BaselineCorruptionIndex = 30f;

        /// <summary>
        /// Round 3 item 3: this country's judicial system funding level, 0-100 (50 = neutral placeholder
        /// for every country - no clean real-world cross-country "relative judicial funding" figure
        /// exists to seed differently per country, the same uniform-dial idiom PoliceFundingLevel
        /// already uses). Persistent, player-adjustable via PolicyDecision.JudicialFundingOverride.
        /// Reduces OrganizedCrimeIndex (better prosecution capacity disrupts organized-crime networks)
        /// and CorruptionIndex (an independent, well-funded judiciary is a canonical real-world
        /// anti-corruption mechanism), and modestly reduces PrisonPopulationRate (well-funded courts
        /// process cases faster, reducing pretrial-detention backlog - a real, well-documented driver
        /// of high incarceration in underfunded systems) - see MacroSystem.
        /// </summary>
        public float JudicialFundingLevel = 50f;

        /// <summary>
        /// Round 3 item 3: this country's border enforcement strictness, 0-100 (0 = open/lenient, 100
        /// = strict; 50 = neutral placeholder for every country, the same uniform-dial idiom
        /// PoliceFundingLevel already uses). Persistent, player-adjustable via
        /// PolicyDecision.BorderEnforcementOverride. Reduces OrganizedCrimeIndex - stricter enforcement
        /// disrupts cross-border smuggling/trafficking, organized crime's real, well-documented core
        /// activity - see MacroSystem.ApplyOrganizedCrimeIndex. Deliberately scoped to this ONE channel
        /// for this pass, not a new labor-supply/immigration effect (keeping effects routed through
        /// already-proven channels, per this item's own explicit instruction).
        /// </summary>
        public float BorderEnforcementLevel = 50f;

        /// <summary>
        /// Round 3 item 5, Part A: this country's structural "steady-state" old-age dependency ratio
        /// (65+ population as a percentage of working-age 15-64 population, a real, standard World
        /// Bank/OECD demographic statistic - NOT the full age-cohort/population-pyramid breakdown,
        /// deliberately a single scalar per the task's own "not the full theoretical richness"
        /// discipline). Never mutated after seeding - the fixed anchor MacroSystem.ApplyDemographicRates'
        /// drift and every gap-based effect (pension pressure, labor force participation) measure
        /// against, the same "avoid a turn-1 shock" idiom BaselineCrimeIndex/BaselinePovertyRate
        /// already use. Real/well-documented for Italy (highest of the six, among the highest in the
        /// world) and the USA/Poland (lowest, both real); Germany's figure is informed by an ESTIMATED
        /// 65+ population share (~22-23%, full age-cohort breakdown unavailable), honestly not a
        /// directly-sourced dependency ratio - see WorldFactory's seeding comment.
        /// </summary>
        public float BaselineDependencyRatio = 30f;

        /// <summary>
        /// Round 3 item 5, Part A: this country's real, seeded starting NetMigrationRate (per-1000
        /// population per turn) - the fixed anchor EconomyState.NetMigrationRate's own aging-driven
        /// drift (see MacroSystem.ApplyDemographicRates) and its gap-based LaborForceParticipationRate
        /// effect measure against, the same "avoid a turn-1 shock" idiom every other Baseline field
        /// uses. Distinct from BirthRate/DeathRate, which have no baseline anchor of their own in this
        /// pass - BirthRate's decline and DeathRate's rise are absolute drifts, not gap-based effects,
        /// so no anchor is needed for either.
        /// </summary>
        public float BaselineNetMigrationRate = 1f;

        /// <summary>
        /// This country's independent central bank chair, or null for a country that instead uses
        /// PolicyDecision.InterestRateChange (the player-controlled slider - Sweden, Poland, and the
        /// Eurozone trio; see CurrencySystem.ApplyInterestRateChanges). Non-null (USA only, for now)
        /// means CurrencySystem bypasses PolicyDecision.InterestRateChange entirely and instead sets
        /// InterestRate to TaylorRule.GetSuggestedInterestRate plus this chair's RateBias each turn -
        /// see FederalReserveSystem and CLAUDE.md's "Federal Reserve" section.
        /// </summary>
        public FedChair CurrentFedChair;

        /// <summary>
        /// This country's sovereign wealth fund, or null (the default - every country) if it doesn't
        /// exist. The player creates/dissolves it via an immediate action (mirrors TaxLine.
        /// IsImplemented's on/off pattern), the same way a non-null CurrentFedChair switches USA's
        /// interest-rate mechanic. USA-first only in this pass - see "Sovereign Wealth Fund" in
        /// CLAUDE.md.
        /// </summary>
        public SovereignWealthFund SovereignWealthFund;

        /// <summary>
        /// Rolling numeric history of this country's key tracked stats, for UI graphs - see
        /// StatHistory.cs. Appended once per turn by SimulationManager.AdvanceTurn, kept entirely
        /// separate from the existing Recent Turns text log.
        /// </summary>
        public StatHistory History = new StatHistory();

        public Country() { }

        public Country(
            CountryId id, string name, EconomyState state, CurrencyZone currencyZone, float baseTariffRate = 0f,
            float naturalUnemploymentRate = 4f, float potentialGrowthRate = 2f, float governmentSpendingRate = 20f,
            float benefitRatePerUnemployed = 0.15f)
        {
            Id = id;
            Name = name;
            State = state;
            CurrencyZone = currencyZone;
            BaseTariffRate = baseTariffRate;
            NaturalUnemploymentRate = naturalUnemploymentRate;
            PotentialGrowthRate = potentialGrowthRate;
            GovernmentSpendingRate = governmentSpendingRate;
            BenefitRatePerUnemployed = benefitRatePerUnemployed;
        }
    }
}
