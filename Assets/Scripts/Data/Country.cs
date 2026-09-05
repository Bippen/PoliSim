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

        /// <summary>
        /// Master Sequence step 9, Step A: the player-facing PUBLISHED view - lagged, and sometimes a
        /// preliminary figure later revised. Deliberately a sibling of State rather than part of it: every
        /// simulation system reads `country.State.X`, so keeping published values out of EconomyState
        /// makes a leak into Okun's Law, the Phillips Curve or the Fiscal Reaction Function a
        /// compile-time impossibility rather than something reviewers must keep noticing across 55 call
        /// sites. See COMPLETED.md section 12 (was STEP_A_LIVE_VALUE_AUDIT.md, consolidated 2026-08-02).
        /// </summary>
        public PublishedData Published = new PublishedData();

        /// <summary>
        /// The standing sovereign rating, set by scheduled review rather than recomputed every turn - see
        /// `CreditRatingSystem.ReviewIfDue` and Elias's A1 ruling (2026-08-02). A derived OUTPUT, sitting
        /// beside Published for the same reason Published sits beside State: nothing in the simulation
        /// reads it, it folds into no ceiling, and it draws no randomness.
        /// </summary>
        public SovereignRatingState Rating = new SovereignRatingState();

        public CurrencyZone CurrencyZone;
        public List<TradePartner> TradePartners = new List<TradePartner>();

        /// <summary>This country's fiscal portfolio - which taxes are implemented and at what rate. See TaxLine and SimulationManager.GetTotalTaxRevenue.</summary>
        public List<TaxLine> TaxLines = new List<TaxLine>();

        /// <summary>
        /// This country's welfare portfolio - which anti-poverty programs are implemented and at what
        /// GenerosityLevel, mirroring TaxLines' implement/adjust/remove pattern exactly. Seeded per
        /// country by WorldFactory.SeedWelfarePrograms (the seed-spread slots of 2026-08-27; every
        /// slot still empty pending sourced figures) - see WelfareProgram and
        /// SimulationManager.GetTotalWelfareCost/ApplyWelfareGenerosityChanges.
        /// </summary>
        public List<WelfareProgram> WelfarePrograms = new List<WelfareProgram>();

        /// <summary>
        /// Playtest 3's seed-spread ruling (2026-08-27): the welfare portfolio AS SEEDED - a snapshot
        /// WorldFactory.SeedWelfarePrograms takes and nothing mutates - the anchor every welfare effect
        /// measures from (MacroSystem.WelfareEffectDelta, SimulationManager.GetTotalWelfareCost). The
        /// sourced baselines already contain each country's real programs, so a program implemented
        /// at seed contributes nothing on the no-policy path and a player's change is booked from the
        /// country's own real position. Empty (the pre-ruling state: delta == live) for a country
        /// seeded before the ruling and for any save that lacks the field.
        /// </summary>
        public List<WelfareProgram> BaselineWelfarePrograms = new List<WelfareProgram>();

        /// <summary>
        /// This country's economic sector breakdown (Manufacturing/Technology/Agriculture/Finance) -
        /// a small proof-of-pattern slice, present for all six countries (unlike TaxLines/
        /// WelfarePrograms, there's no implement/remove - every country has all four sectors always).
        /// See Sector.cs and MacroSystem.ApplySectorEffects.
        /// </summary>
        public List<Sector> Sectors = new List<Sector>();

        /// <summary>
        /// Law system MVP slice: laws this country currently has in force, each a named preset over
        /// the existing dial space rather than a bespoke effect - see LawDefinition/LawCatalog for
        /// what a law IS and ParliamentSystem.GetLawBillDirection/ApplyLawBillResult for how one
        /// reaches this list. A List&lt;EnactedLaw&gt;, not a Dictionary&lt;LawId,bool&gt; - matching this
        /// codebase's dominant "collection of things a country has" shape (TaxLines/Sectors), not
        /// the Dictionary shape reserved for closed structural enums (CabinetMinisters/
        /// ParliamentSeats). A bare bool would discard the provenance (which law, when) this project
        /// has otherwise consistently kept - see DivisionLog, built for exactly this reason. Empty
        /// for every country by default - no law starts enacted. Deliberately NOT in
        /// SimulationManager.ClonePreviewCountry's hand-list, matching CabinetMinisters/Divisions'
        /// own omission - nothing in the PreviewTurn pipeline ever reads or mutates it, since bill
        /// resolution (the only writer) runs from the day-tick loop, never from a preview.
        /// </summary>
        public List<EnactedLaw> EnactedLaws = new List<EnactedLaw>();

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

        /// <summary>P4-C3 (2026-09-04): the seeded natural rate, the base the enacted LabourInstitutions laws compose on -
        /// NaturalUnemploymentRate is recomputed from this plus the enacted set (SimulationManager.RecomputeStructuralParametersFromEnactedLaws),
        /// never mutated incrementally, the clamp-safe idiom the crime dials taught (COMPOSITION).</summary>

        public float NaturalUnemploymentRateBase;

        /// <summary>P4-C3: the seeded bases of the other structural parameters a law may move (StructuralParameters) - captured once the
        /// seeds are placed (CaptureStructuralBases, at the end of WorldFactory.CreateDefault), composed on by the enacted set.</summary>
        public float ComfortableDebtToGdpPercentBase;
        public float AverageDebtMaturityYearsBase;
        public float RiskPremiumSensitivityBase;

        /// <summary>P5-B3 (2026-09-05): the seed's GDP and the seed level of each tax-base driver (TaxBases.Level, indexed by TaxBaseDriver), captured by CaptureStructuralBases; a base is its sourced share of the seed's GDP times its driver's ratio to these. 0 = not yet referenced (consumption is computed by the first day; TaxBases.Base takes it then).</summary>
        public float RevenueBaseSeedGdp;
        public float[] RevenueBaseSeeds = new float[TaxBases.DriverCount];

        /// <summary>P5-B7 (2026-09-05): potential output's factors - the seed's potential, the seed's labour input (PotentialOutput.LabourInput) and the productivity index that compounds daily at the trend (1 at the seed); the labour input as it stood at the last turn, for the derived growth rate. Captured by CaptureStructuralBases; 0 = a save from before this pass, which keeps the old compounding.</summary>
        public float PotentialGdpSeed;
        public float PotentialLabourSeed;
        public float PotentialProductivityIndex;
        public float PotentialLabourAtLastTurn;

        /// <summary>P5-B6 (2026-09-05): the price level as it stood when the spending lines were last indexed, so the lines carry the year's prices as the ratio now/then (IndexSpendingLines). 1 at the seed; 0 = a save from before this pass (the first index takes the level and applies 1).</summary>
        public float PriceLevelAtLastIndex;
        public float CollectionEfficiencyBase;
        public float GovernmentSpendingRateBase;
        /// <summary>P4-C3 (2026-09-05, the labour institutions' second reach): the seeded benefit rate per point of unemployment; the labour laws that cut benefit levels or duration compose on it.</summary>
        public float BenefitRatePerUnemployedBase;

        /// <summary>P4-C3: record every structural parameter's seeded value as its base. Called once, after seeding; a save carries the bases. P5-B2: also the seed level of every spending line's driver (SpendingLine.DriverReference), so the first year's index counts the seed-to-year-one change.</summary>
        public void CaptureStructuralBases()
        {
            foreach (SpendingLine line in SpendingLines) { line.DriverReference = SpendingDrivers.Level(SpendingDrivers.Of(line.Category), this); }
            RevenueBaseSeedGdp = State.GDP;   // P5-B3
            PotentialGdpSeed = State.PotentialGDP;   // P5-B7: potential is its factors from here on
            PotentialLabourSeed = PotentialOutput.LabourInput(this);
            PotentialProductivityIndex = 1f;
            PotentialLabourAtLastTurn = PotentialLabourSeed;
            PriceLevelAtLastIndex = State.PriceLevel;   // P5-B6
            RevenueBaseSeeds = new float[TaxBases.DriverCount];
            for (int d = 0; d < TaxBases.DriverCount; d++) { RevenueBaseSeeds[d] = TaxBases.Level((TaxBaseDriver)d, this); }
            NaturalUnemploymentRateBase = NaturalUnemploymentRate;
            ComfortableDebtToGdpPercentBase = ComfortableDebtToGdpPercent;
            AverageDebtMaturityYearsBase = AverageDebtMaturityYears;
            RiskPremiumSensitivityBase = RiskPremiumSensitivity;
            CollectionEfficiencyBase = CollectionEfficiency;
            GovernmentSpendingRateBase = GovernmentSpendingRate;
            BenefitRatePerUnemployedBase = BenefitRatePerUnemployed;   // P4-C3, the labour institutions' second reach
        }

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
        /// THE COVERAGE BRIDGE between the four modelled instruments and a whole tax system - re-documented
        /// by D-16 (a), 2026-09-04 (COMPLETED.md §282). Applied as a multiplier in
        /// SimulationManager.ApplyRevenueAndSpending (ActualRevenue = GetTotalTaxRevenue() * CollectionEfficiency),
        /// not inside GetTotalTaxRevenue itself, so that method still returns the instruments' own figure. Solved
        /// per country in WorldFactory as Target / Implied so the default portfolio's actual revenue-to-GDP lands
        /// on that country's calibration target - see WorldFactory's doc comment for the derivation.
        ///
        /// ⚠ WHAT THE NUMBER MEANS DEPENDS ON THE BASE IT IS SOLVED OVER. For the five countries on the sourced
        /// per-country bases (TaxBaseTable: realised revenue over the seeded rate), rate x base already IS
        /// realised revenue - the collection loss is inside the base - so the constant no longer marks anything
        /// down: it EXCEEDS 1, and the excess is the receipts the four instruments do not model (property, excise,
        /// social contributions beyond payroll...). It is COVERAGE, not efficiency, and its old reading
        /// ("how much of the theoretical base is actually collected, 0.0-1.0") is false for the five. For the USA,
        /// on the uniform stand-in bases by F-B's perimeter ruling, the old reading still holds and the value is
        /// below 1. A Finance minister's competence bias still adds to it (CabinetSystem); the floor is 0 and there
        /// is no ceiling - SimulationManager no longer clamps it to 1.
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

        /// <summary>Q3 (Master Sequence II step 1, rulings R-Q3a/b, 2026-08-17): the trend growth
        /// rate of labour PRODUCTIVITY, percent per year - the quantity the potential-growth
        /// ledger's sum now IS, per Design B's causal re-rooting: {infrastructure, sector}
        /// adjustments flow through productivity, wages read productivity's own growth, and
        /// PotentialGrowthRate reads productivity at 1:1 through the pipe
        /// (MacroSystem.ApplySectorGrowthEffect writes both, same sum, same clamps - a pure
        /// re-rooting, byte-identical by construction and by bar).
        /// â  -1 is a SENTINEL (the R4-3 pattern): pre-Q3 saves and any read before the
        /// finalizer's first write fall back to PotentialGrowthRate via
        /// <see cref="ProductivityTrendGrowth"/> - bit-for-bit the value the old readers read,
        /// in every ordering. Readers use the property, never the raw field.</summary>
        public float ProductivityTrendGrowthRate = -1f;

        /// <summary>The read-side of the Q3 sentinel - see the field above.</summary>
        public float ProductivityTrendGrowth => ProductivityTrendGrowthRate >= 0f ? ProductivityTrendGrowthRate : PotentialGrowthRate;

        /// <summary>
        /// Step 2 (R-S2d/R-S2e): last CLOSED period's approval attribution - what the trace panel
        /// shows - and the ACCRUING one collecting this period's events until the boundary
        /// formula closes it. Both persisted: the closed one is R-S2e's "one period in the save
        /// shape"; the accruing one rides along so a mid-period save does not silently drop
        /// recorded events (the exact silent-gap class R-S2e's no-case predicted). Null on old
        /// saves and at seed - every reader guards, and the recorder lazily creates the accruing
        /// ledger at first touch. â  NEVER EconomyState fields: the trajectory dump reflects
        /// EconomyState's public fields, and recording is OBSERVATION - it must not change the
        /// dump.
        /// </summary>
        public ApprovalAttribution ApprovalLedgerLastPeriod;
        public ApprovalAttribution ApprovalLedgerAccruing;

        /// <summary>
        /// Step 2's THIRD section (2026-08-25, on the trigger Italy Debt Crisis fired): the debt
        /// stock's attribution, the same closed/accruing pair as the approval ledger above and
        /// under the same rules - both persisted per R-S2e, null on old saves and at seed, every
        /// reader guards, the recorder opens the accruing one at the PRE-write stock on first
        /// touch. Terms accrue by observation one daily slice at a time (the stock moves daily),
        /// close where the FiscalTurnReport closes. â  NEVER EconomyState fields - same reason.
        /// </summary>
        public DebtAttribution FiscalLedgerLastPeriod;
        public DebtAttribution FiscalLedgerAccruing;

        /// <summary>THE MATURITY RATE-LAG (2026-08-17, mechanism-report ruling R4, ruled IN with
        /// its target quantified by the erosion pass): this country's average debt maturity in
        /// YEARS - the stock rolls over at ~1/M per year, so the effective rate it pays reverts
        /// toward the current issuance rate at that speed (see
        /// SimulationManager.AdvanceEffectiveDebtRate). A structural per-country constant, seeded
        /// in WorldFactory from real debt-office data with source and date per line - Sweden's
        /// figure is a TIME-TO-REFIXING steering range, which for a repricing lag is the
        /// mechanism-relevant basis, stated there; Germany's is the batch's one REPORTED [GAP],
        /// seeded [ESTIMATED] with its bound stated.</summary>
        public float AverageDebtMaturityYears = 6f;

        /// <summary>THE MATURITY RATE-LAG's one piece of state: the blended rate this country's
        /// existing debt stock currently pays, in percent - reverting toward
        /// SimulationManager.GetDebtIssuanceRate at 1/AverageDebtMaturityYears per year.
        /// â  -1 is a SENTINEL (the R4-3 HousingRateAnchor pattern): a pre-mechanism save
        /// deserializes with this initializer and every reader falls back to the CURRENT issuance
        /// rate - exactly what the old code charged - so old saves behave identically until the
        /// lag has something to lag. AdvanceEffectiveDebtRate initializes it on first advance;
        /// GetInterestOnDebt reads through a non-mutating fallback and never writes it (preview
        /// safety). Rides the World save layer as a public field; snapshotted explicitly in the
        /// round-trip diagnostic - confirmed, not assumed.</summary>
        public float EffectiveDebtInterestRate = -1f;

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
        /// and Inflation sits at TaylorRule.InflationTarget(Id) (i.e. the gaps that otherwise move the
        /// baseline are both zero). Seeded per country from real OECD relative-poverty-rate data (see
        /// WorldFactory) - the same figure EconomyState.PovertyRate is seeded to, so a new game opens
        /// with PovertyRate already at (or very near) its own baseline rather than an artificial
        /// turn-1 jump, the same "avoid a one-time shock" lesson "Turn-1 GDP Consistency" established.
        /// </summary>
        public float BaselinePovertyRate = 10f;

        /// <summary>ROUND 4 BATCH 1 (C3): the structural youth-unemployment anchor - the target of
        /// ApplyYouthUnemployment's reversion when headline Unemployment sits exactly at NAIRU. Seeded
        /// to the same real Feb 2026 figure EconomyState.YouthUnemployment opens at (the standing
        /// zero-gap-at-start idiom).</summary>
        public float BaselineYouthUnemploymentRate = 15f;

        /// <summary>ROUND 4 BATCH 1 (C3): the structural life-expectancy anchor, years at birth -
        /// ApplyLifeExpectancy's reversion target before the poverty drag and healthcare lift. Seeded
        /// to the same real 2024 figure the state opens at.</summary>
        public float BaselineLifeExpectancy = 80f;

        /// <summary>ROUND 4 BATCH 2 (C2): the structural inequality anchor - ApplyGini's reversion
        /// target when unemployment sits at NAIRU, no welfare program is implemented, income tax is
        /// at its seeded rate and the minimum wage at its own anchor. Seeded to the same real 2024
        /// Eurostat/OECD figure EconomyState.Gini opens at (zero-gap idiom).</summary>
        public float BaselineGini = 30f;

        /// <summary>ROUND 4 BATCH 2 (C2): the income-tax rate this country SEEDED with - the anchor
        /// ApplyGini measures redistribution against, captured once in WorldFactory from the seeded
        /// TaxLine (one authority; TaxLine.Rate itself is player-mutable with no stored seed, which
        /// is exactly why this anchor must exist as its own field). NOT a lever and never mutated
        /// after world creation - the same role BaselineMinimumWagePercentOfMedian plays for the
        /// minimum-wage gap.</summary>
        public float BaselineIncomeTaxRate = 30f;

        /// <summary>
        /// C-N4 (2026-08-31): **every tax line's rate AS SEEDED**, snapshotted by `SeedTaxLines` and never
        /// mutated â the anchor the disposable-income term measures a player's change from.
        ///
        /// <para>â  **This is what makes C-N4 SAFE rather than BASELINE, and it is not an accident.** It is
        /// `BaselineWelfarePrograms`' own idiom, adopted for the same reason that field records: the
        /// sourced seeds already contain each country's real tax position, so a country sitting at its
        /// seeded rates must contribute **exactly zero** to the new term, and a player's change is booked
        /// from the country's own real position rather than from an arbitrary zero. The consequence is
        /// that the no-policy trajectory cannot move â and the dump is run to prove that rather than the
        /// reasoning being trusted.</para>
        /// </summary>
        public Dictionary<TaxType, float> BaselineTaxRates = new Dictionary<TaxType, float>();

        /// <summary>P-I2 stage 1: the country's FIVE-YEAR AGE PYRAMID, 21 bands in millions of persons,
        /// seeded from `PopulationPyramids` (Eurostat demo_pjan 1 Jan 2024 for the EU five, US Census PEP
        /// vintage 2024 for the USA).
        /// <para>⚠ <b>Read-only in this stage, and that is the whole point of landing it alone.</b> Nothing
        /// ages it and nothing in `EconomyState` derives from it yet, so the no-policy trajectory cannot
        /// move — which the dump is run to PROVE rather than the reasoning being trusted. The spec-let's
        /// five collisions (`COMPLETED.md §201` §4) all live in the stage that retires the eight
        /// demographic scalars, and that stage carries its own explained baseline family.</para>
        /// <para>⚠ Not persisted yet, deliberately: it is re-seeded from the sourced table on load, exactly
        /// as `BaselineTaxRates` is. The save-layer bump belongs to the stage that makes it mutable, where
        /// an absent pyramid would mean a different game state rather than a harmless default.</para>
        /// </summary>
        public PopulationCohorts Cohorts;

        /// <summary>ROUND 4 BATCH 3 (C1): whether housing cost overburden is a tracked stat for
        /// this country - TRUE for the EU five ([VERIFIED] Eurostat whole-population figures),
        /// FALSE for the USA, whose sources measure a different threshold on a different income
        /// basis (no comparable figure exists; the recorded ruling gives the USA homeownership as
        /// its primary housing metric instead). The MinimumWageImplemented idiom: a structural
        /// per-country fact, not a lever. Where false, ApplyHousingOverburden early-outs and the
        /// UI draws no overburden row - the asymmetry is deliberate everywhere it appears.</summary>
        public bool TracksHousingOverburden = true;

        /// <summary>ROUND 4 BATCH 3 (C1): structural overburden anchor, % of population - the
        /// reversion target at the epoch policy rate with no HousingAssistance. Meaningless where
        /// TracksHousingOverburden is false (left at 0 for the USA).</summary>
        public float BaselineHousingOverburden = 0f;

        /// <summary>ROUND 4 BATCH 3 (C1): structural homeownership anchor, % of households (OECD
        /// AHD basis). Seeded for all six; the USA's primary housing metric per the ruling.</summary>
        public float BaselineHomeownership = 65f;

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
        /// THE STATUTORY BASE fields (pass 3, the Labor Market law category, coexistence ruling
        /// 2026-08-26): the BILL-OWNED half of each labor dial. Elias ruled the Labor tab KEEPS its
        /// sliders when labor laws ship (the deliberate opposite of the Crime &amp; Justice tab's
        /// read-only conversion), so the two-books problem is solved by splitting each dial in two
        /// books EXPLICITLY: LaborPolicyBill sets these base fields absolutely (same clamps as
        /// before), enacted laws contribute a pure delta sum on top, and
        /// SimulationManager.RecomputeLaborDialsFromEnactedLaws composes
        /// effective = clamp(base + law deltas) into the effective field above/below it - clamped
        /// ONCE at composition, never persisted into either component, so full repeal returns the
        /// effective dial exactly to base and a passed bill never stomps law effects.
        ///
        /// Seeded equal to each dial's own starting value in WorldFactory (zero law offset at seed).
        /// -1 is the "unset" sentinel for OLD SAVES only (a save written before these fields
        /// existed deserializes them at this default): RestoreSaveState adopts the saved dial value
        /// as the base, which is exactly right because no pre-pass-3 save can hold a labor law.
        /// MinimumWagePercentOfMedianBase stays meaningful only where MinimumWageImplemented (0 for
        /// Sweden/Italy, matching the dial itself).
        /// </summary>
        public float MinimumWagePercentOfMedianBase = -1f;
        public float PaidFamilyLeaveWeeksBase = -1f;
        public float OvertimeRegulationBase = -1f;
        public float RetrainingProgramBase = -1f;
        public float FamilyPolicyBase = -1f;
        public float ImmigrationPolicyBase = -1f;

        /// <summary>
        /// This country's structural "steady-state" CrimeIndex - the target MacroSystem.
        /// ApplyCrimeIndex's mean-reversion moves EconomyState.CrimeIndex toward absent any policy
        /// input (the same "avoid a turn-1 shock" anchor idiom BaselinePovertyRate/
        /// BaselineLaborForceParticipationRate - retired at F2 step 5 - used). Seeded per country from a STYLIZED 0-100
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
        ///
        /// THE COUPLINGS PASS (terminal ruling 2026-08-26) RE-EXAMINED the single-edge status and
        /// DECLINED a second sim edge, reasons recorded: the migration channel belongs to
        /// ImmigrationPolicyLevel (0.1/pt, the anti-double-counting design), and the documented
        /// deterrence elasticity is modest (Angelucci 2012: -0.4..-0.8). The dial DID gain a
        /// budget edge (its enforcement cost lands on a real spending line - see
        /// SimulationManager.ApplyEnforcementCostPressure) and keeps its transitive crime chain.
        /// Single direct edge, ruled honest rather than incomplete.
        /// </summary>
        public float BorderEnforcementLevel = 50f;

        /// <summary>
        /// THE COUPLINGS PASS (2026-08-26): the last dollar amount of dial-driven enforcement cost
        /// applied to this country's justice-routed spending line (Justice, else PublicServices) -
        /// police + judicial dial gaps at their ruled shares plus the incarceration variable cost.
        /// The tracker that lets a STATELESS cost target compose with the STATEFUL line writers:
        /// each boundary applies only the difference from this. Additive save field; old saves
        /// load 0 and self-correct at their first boundary. See
        /// SimulationManager.ApplyEnforcementCostPressure.
        /// </summary>
        public float AppliedJusticeEnforcementCost = 0f;

        /// <summary>The border twin of <see cref="AppliedJusticeEnforcementCost"/> - the last
        /// applied border-enforcement cost on the border-routed line (HomelandSecurity, else
        /// Migration, else PublicServices).</summary>
        public float AppliedBorderEnforcementCost = 0f;

        /// <summary>P4-B3 (2026-09-04): the sector twin of <see cref="AppliedJusticeEnforcementCost"/> - the last applied
        /// sector-support cost (SectorCouplings: subsidies, tax credits and research grants above the neutral dial) on the
        /// Commerce line (else PublicServices). Additive save field; old saves load 0 and self-correct at their first boundary.</summary>
        public float AppliedSectorSupportCost = 0f;

        /// <summary>
        /// This country's old-age dependency ratio at the start of play (65+ as a percentage of
        /// 15–64), the fixed anchor every gap-based effect (pension pressure, labor force
        /// participation) measures against - the "avoid a turn-1 shock" idiom BaselineCrimeIndex and
        /// BaselinePovertyRate use. Since F2 step 4 (2026-09-02) it is RE-SEEDED at world creation
        /// from the cohort pyramid walked to the epoch (WorldFactory, CohortDemographics.WalkToEpoch),
        /// so it is exactly the ratio EconomyState.DependencyRatio reads on day one and the gap opens
        /// at zero. The typed per-country figures in WorldFactory (28–40) were the retired scalar
        /// demography's seeds, kept there as the record of what they were; the distance between them
        /// and the pyramid's own ratio is in COMPLETED.md §195.
        /// </summary>
        public float BaselineDependencyRatio = 30f;

        /// <summary>
        /// This country's sourced starting net migration rate (per 1,000 population per year) - the
        /// fixed anchor the gap-based LaborForceParticipationRate effect measures against, the same
        /// idiom every other Baseline field uses. Since F2 step 4 the live NetMigrationRate is a
        /// reading of the cohort substrate (the net migration the publisher's projection assumes,
        /// plus the lever), so a projection assuming more immigration than 2024 had reads as a
        /// positive gap against this sourced anchor - see CohortDemographics.
        /// </summary>
        public float BaselineNetMigrationRate = 1f;

        /// <summary>
        /// Family/childcare policy support intensity, 0-100 (0 = minimal support, 100 = maximal
        /// pro-natalist support; 50 = neutral, the same uniform-dial idiom BorderEnforcementLevel
        /// uses). Persistent, player-adjustable via PolicyDecision.FamilyPolicyOverride - see
        /// SimulationManager.ApplyDemographicPolicyChanges. Since F2 step 4 it reaches the cohort
        /// substrate as a fertility multiplier on the year's births (CohortDemographics.Step), at the
        /// magnitude the retired additive rule had (LaborCouplings.FamilyPolicyBirthRateSensitivity
        /// points per thousand on the natural crude birth rate). Deliberately SMALL: real-world
        /// evidence on pro-natalist policy's effect on fertility is itself small and contested - this
        /// lever nudges the trajectory, it does not reverse a country's demographic direction alone.
        /// </summary>
        public float FamilyPolicyLevel = 50f;

        /// <summary>
        /// Immigration policy openness, 0-100 (0 = maximally restrictive, 100 = maximally open; 50 =
        /// neutral). Persistent, player-adjustable via PolicyDecision.ImmigrationPolicyOverride - see
        /// SimulationManager.ApplyDemographicPolicyChanges. Since F2 step 4 it reaches the cohort
        /// substrate as additive net migration distributed over the sourced immigration age profile
        /// (CohortDemographics.Step), at the magnitude the retired rule had
        /// (LaborCouplings.ImmigrationPolicyNetMigrationSensitivity per thousand). A wider bound than
        /// FamilyPolicyLevel's - immigration is a genuinely more responsive real-world lever than
        /// fertility (visa/asylum/quota policy can move actual migration within a single term).
        /// Its labor-force effect deliberately reuses the EXISTING NetMigrationRate-gap term in
        /// ApplyLaborForceParticipationRate's combined ceiling rather than a second, parallel
        /// immigration-to-labor-force channel - one variable, one downstream channel, structurally.
        /// </summary>
        public float ImmigrationPolicyLevel = 50f;

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
        /// Political Systems Overhaul Part A (Cabinet): this country's currently-appointed ministers,
        /// keyed by portfolio - empty for every country by default (the same "doesn't exist until the
        /// player acts" idiom SovereignWealthFund/CurrentFedChair already use). No NPC/player
        /// distinction is needed anywhere CabinetSystem or its three effect-landing call sites read
        /// this dictionary: the Cabinet UI only ever lets the player appoint into their OWN country, so
        /// every other country's dictionary simply stays empty forever, and an empty-dictionary lookup
        /// naturally contributes zero effect - the same structural no-op SovereignWealthFund's null
        /// check already gets for free, just via TryGetValue instead of a null check.
        /// </summary>
        public Dictionary<CabinetPortfolio, CabinetMinister> CabinetMinisters = new Dictionary<CabinetPortfolio, CabinetMinister>();
        /// <summary>P2-5.2 (2026-09-02): the cabinet events LOYALTY's term has produced (resignations, leaks), newest last - the Docket's minister alerts read them.</summary>
        public List<CabinetEventRecord> CabinetEvents = new List<CabinetEventRecord>();

        /// <summary>
        /// This country's chamber, seats per REAL party, keyed by the abbreviation that country's own
        /// election authority uses ("S", "SD", "CDU", "PiS", "UG", "REP"...) and summing to
        /// `PartySystems.ChamberSeats(Id)` - 349 Sweden, 630 Germany, 577 France, 400 Italy, 460
        /// Poland, 435 USA.
        ///
        /// W-G1 RE-KEYED THIS FIELD, from a dictionary over four generic fictional archetypes shared
        /// by all six countries, to real parties per country. That is the named save-breaking swap in
        /// `SaveGameService.CurrentSaveVersion`'s own doc comment, and it is why that constant bumped.
        ///
        /// Seeded from `PartySystems.InitialSeats(Id)` - each country's most recent real election -
        /// and CHANGED ONLY BY AN ELECTION, not by a per-turn drift off ApprovalRating. See
        /// `ParliamentSystem.UpdateSeats` for what that replaced and why it could not survive real
        /// parties.
        /// </summary>
        public Dictionary<string, int> ParliamentSeats = new Dictionary<string, int>();

        /// <summary>
        /// W-G1: every election this country has held, oldest first.
        ///
        /// â  **IT LIVES ON `Country`, INSIDE `World`, AND THAT IS A DELIBERATE CHOICE ABOUT WHICH
        /// SAVE LAYER IT LANDS IN.** The obvious home was `UiDraftState`, following the
        /// `FedChairCandidates` precedent â but `SaveLoadRoundTripDiagnostic` records in its own
        /// header that "Layer 3 (UI drafts) is structurally out of reach (no OnGUI in batch)", so
        /// that precedent has itself never been machine-proven. The World graph is a layer the
        /// diagnostic already round-trips field by field across 6 countries and 2 seeds. Putting an
        /// election record where the harness can actually see it was worth more than matching a
        /// precedent the harness cannot.
        ///
        /// Before W-G1 an election left only a transient `_pendingElectionResult`, cleared the moment
        /// the player dismissed the reveal â which is why the Docket's calendar marks no past
        /// election. There was nothing to mark.
        /// </summary>
        public List<Elections.ElectionRecord> ElectionHistory = new List<Elections.ElectionRecord>();

        /// <summary>
        /// C-D4 (Â§38, R-CL3): **each party's long-term political capital â what survives an election.**
        ///
        /// <para>Put HERE, beside <see cref="ElectionHistory"/>, for that field's own recorded reason:
        /// the World graph is the layer `SaveLoadRoundTripDiagnostic` round-trips field by field across
        /// six countries and two seeds, so persistence here can be PROVEN. `UiDraftState` cannot be, and
        /// matching a precedent the harness cannot see was worth less than being checkable.</para>
        ///
        /// <para>â  Donor and grassroots networks are **specified ABSENT** on
        /// <see cref="Elections.PartyCampaignCapital"/> rather than invented â Â§38 names them, nothing on
        /// disk sizes them, and a fabricated donor stock is what Â§0.4 forbids.</para>
        /// </summary>
        public List<Elections.PartyCampaignCapital> PartyCapital = new List<Elections.PartyCampaignCapital>();

        /// <summary>
        /// C-R1/C-R2 (R-CL1, "the player has a party"): **which of this country's real seeded parties the
        /// player leads.** Null where none has been chosen.
        ///
        /// <para>â  **INTERIM RULE, and it is DERIVED rather than invented.** The ruling says the player
        /// picks at country selection, and the picker is a Canvas-screen build that is BILLED, not done
        /// (see `COMPLETED.md` Â§119). Until it exists, selection seats **the largest party in that
        /// country's own seeded chamber** â you are the government, and which party that is comes from the
        /// real returns on disk, not from a choice this code made up. It is an interim DEFAULT, marked as
        /// one, and the first thing the picker replaces.</para>
        ///
        /// <para>World state, so it rides `SaveGame.World` beside `ElectionHistory` and `PartyCapital` â
        /// the layer `SaveLoadRoundTripDiagnostic` can prove, which `UiDraftState` cannot.</para>
        /// </summary>
        public string PlayerPartyAbbrev;

        /// <summary>
        /// C-R3: **the player's PARTY approval, a NEW ADDITIVE STOCK.**
        ///
        /// <para>â  The design constraint that keeps this SAFE rather than BASELINE, and it is the whole
        /// point of the row: **personal approval keeps `EconomyState.ApprovalRating`, its name and every
        /// one of its consumers, untouched.** Party approval is added beside it and nothing reads it into
        /// the simulation, so the no-policy trajectory is predicted byte-identical â and the dump is run
        /// to prove that rather than assert it.</para>
        ///
        /// <para>â  **Nothing moves it yet, for C-D4's reason and stated the same way.** A rule coupling
        /// party approval to events would need a coefficient nothing on disk sources, and inventing one to
        /// make a stock look alive is what the standing rules forbid. It opens at the personal rating and
        /// PERSISTS - which is itself the change, since before this there was no such stock at all.</para>
        /// </summary>
        public float PartyApprovalRating = 50f;

        /// <summary>
        /// Rolling numeric history of this country's key tracked stats, for UI graphs - see
        /// StatHistory.cs. Appended once per turn by SimulationManager.AdvanceTurn, kept entirely
        /// separate from the existing turn-activity text log (shown under Statistics -> International since 2026-08-01).
        /// </summary>
        public StatHistory History = new StatHistory();

        /// <summary>
        /// Bounded history of resolved parliamentary divisions - see DivisionRecord.cs. Written by
        /// ParliamentSystem.RecordDivision at the moment each bill resolves, read by the UI only.
        /// **Nothing in the simulation may read it back** - DivisionLog's own doc comment explains why
        /// that constraint is the whole point rather than a style preference. Distinct from History
        /// above (numeric per-turn series) and from the turn-activity text log (UI-side prose).
        /// </summary>
        public DivisionLog Divisions = new DivisionLog();

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
            NaturalUnemploymentRateBase = naturalUnemploymentRate;
            PotentialGrowthRate = potentialGrowthRate;
            GovernmentSpendingRate = governmentSpendingRate;
            BenefitRatePerUnemployed = benefitRatePerUnemployed;
        }
    }
}
