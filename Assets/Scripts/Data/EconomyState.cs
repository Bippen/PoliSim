using System;

namespace PoliSim.Data
{
    /// <summary>
    /// Snapshot of a country's economic and political indicators for a single turn.
    /// Plain data holder - no Unity dependencies, no simulation logic.
    /// </summary>
    [Serializable]
    public class EconomyState
    {
        /// <summary>Gross domestic product, in abstract currency units.</summary>
        public float GDP;

        /// <summary>Annualized inflation rate, as a percentage (e.g. 2.5 = 2.5%).</summary>
        public float Inflation;

        /// <summary>P5-B6 (2026-09-05): THE PRICE LEVEL - 1 at the seed, compounding every day at this state's Inflation (MacroSystem.ApplyPriceLevelDaily). The macro block (C+I+G+NX, potential, Okun, the Phillips curve) is real; the BOOK (spending lines, tax bases, revenue, debt) is nominal and carries this. Nominal GDP is GDP x PriceLevel; the constant-price view is GDP itself, the derived readout.</summary>
        public float PriceLevel = 1f;

        /// <summary>P5-B6: GDP in current prices - the figure the book is measured against and the screens print as GDP.</summary>
        public float NominalGdp => GDP * Math.Max(0.0001f, PriceLevel);

        /// <summary>Unemployment rate, as a percentage (e.g. 5.0 = 5.0%).</summary>
        public float Unemployment;

        /// <summary>Public approval of the government, 0-100.</summary>
        public float ApprovalRating;

        /// <summary>The budget ACCUMULATOR, in the same currency units as GDP: every day's balance and every one-time
        /// settlement is added and nothing resets it. P2-0.4 (2026-09-02): NOT a display figure - the player sees the
        /// closed year's balance (FiscalTurnReport.BudgetBalance, StatHistory.BudgetBalanceAnnual); this field serves the
        /// model's own delta reading (PreviewTurn's net budget impact) and the debt stock's twin bookkeeping.</summary>
        public float Budget;

        /// <summary>Net exports (exports minus imports, after tariff effects) for the most recent turn.</summary>
        public float TradeBalance;

        /// <summary>
        /// Relative currency strength index, 100 = neutral. Only meaningful for countries with an
        /// independent currency (not sharing a CurrencyZone with other countries) - see
        /// CurrencySystem.ApplyCurrencyStrength. Shared-currency countries (e.g. Eurozone members)
        /// leave this at its default and it is not used.
        /// </summary>
        public float CurrencyStrength;

        /// <summary>Household consumption (the C in GDP = C + I + G + NX) for the most recent turn. See MacroSystem.ApplyNationalAccounts.</summary>
        public float Consumption;

        /// <summary>Business investment (the I in GDP = C + I + G + NX) for the most recent turn. See MacroSystem.ApplyNationalAccounts.</summary>
        public float Investment;

        /// <summary>
        /// Trend/potential output level - grows independently of actual GDP at the country's
        /// PotentialGrowthRate. Used for Okun's Law's growth gap and the identity's reversion;
        /// TaylorRule.GetOutputGapPercent reads it as a reference LEVEL gap only - the rule itself
        /// reads the unemployment gap since pass 4 (2026-08-26).
        /// </summary>
        public float PotentialGDP;

        /// <summary>Adaptively-formed expectation of inflation, used by the Phillips Curve. See MacroSystem.ApplyInflationExpectations.</summary>
        public float InflationExpectations;

        /// <summary>
        /// ⚠ THE POLICY-DRIFT BASE, not the confidence anything reads (Q2's single-book rider,
        /// R-Q2a). This field is only the accumulator of permanent policy shifts (healthcare
        /// spending, UBI - its two writers), seeded 1.0 = neutral. Everything economic or visible
        /// - the national-accounts identity in both forms, and every display surface - reads
        /// MacroSystem.EffectiveConsumerConfidence (base × the wage-sentiment factor) instead.
        /// No surface may show THIS value as "confidence"; a surface showing it must label it as
        /// the base. Field name kept for the save shape (Newtonsoft serializes field names).
        /// </summary>
        public float ConsumerConfidence;

        /// <summary>Business confidence index, 1.0 = neutral. Scales Investment; nothing currently feeds this back.</summary>
        public float BusinessConfidence;

        /// <summary>Outstanding government debt, in the same currency units as GDP. Grows by this turn's deficit, shrinks by any surplus - see SimulationManager.ApplyRevenueAndSpending.</summary>
        public float GovernmentDebt;

        /// <summary>
        /// Share of the population below the poverty line, as a percentage (e.g. 18 means 18%,
        /// matching how Unemployment/Inflation are stored, not a raw 0-1 fraction). Seeded per country
        /// from real OECD relative-poverty-rate data (see WorldFactory); mean-reverts each turn toward
        /// a baseline driven by Unemployment/Inflation gaps (both already-proven drivers elsewhere in
        /// this model - see MacroSystem.ApplyPovertyRate), then adjusted by any implemented
        /// Country.WelfarePrograms. Hard-clamped to [0, 100].
        /// </summary>
        public float PovertyRate;

        /// <summary>
        /// Share of the working-age population that is either employed or actively looking for work,
        /// as a percentage (e.g. 62 means 62%, matching how Unemployment/PovertyRate are stored).
        /// Since F2 step 5 (2026-09-02) it opens at, and mean-reverts toward, the participation the
        /// country's own pyramid implies at sourced rates by age (ParticipationRateTable.StructuralRate -
        /// the typed World Bank/OECD "ages 15+" baseline is retired), adjusted by the unemployment gap (a
        /// discouraged/encouraged-worker effect) and the paid-leave and retraining levers - see
        /// MacroSystem.ApplyLaborForceParticipationRate. Aging and immigration reach it through the
        /// pyramid, not through a coupling.
        /// </summary>
        public float LaborForceParticipationRate;

        /// <summary>ROUND 4 BATCH 1 (C3): under-25 unemployment, % of the YOUTH labour force - a RATE,
        /// never a ratio (the seed doc's closed-by-construction trap: `unit=PC_ACT`). More cyclical
        /// than headline unemployment, which is how every existing lever reaches it - inputs-only,
        /// writes nothing back (the Round 4 standing rule). The US seed is the 16-24 bracket by design
        /// (the OECD-harmonised US equivalent); that difference must never be "corrected".</summary>
        public float YouthUnemployment;

        /// <summary>ROUND 4 BATCH 1 (C3): life expectancy at birth, years. Inputs-only: reverts
        /// generationally toward the country's baseline, dragged by poverty above its own baseline and
        /// lifted by an implemented UniversalHealthcare program - both real, documented directions at
        /// modest scales (see MacroSystem.ApplyLifeExpectancy). No denominator, so the UI draws it on
        /// the negative-fill convention (§A.9b), never a gauge.</summary>
        public float LifeExpectancy;

        /// <summary>ROUND 4 BATCH 2 (C2): Gini coefficient of equivalised disposable income on the
        /// 0-100 scale (Eurostat's own label for `GINI_HND`). Inputs-only: reverts toward the
        /// country's structural baseline, pushed up by labour-market slack and pulled down by
        /// implemented welfare programs, income tax above its seeded rate, and a minimum wage above
        /// its own anchor - the PovertyRate idiom exactly (see MacroSystem.ApplyGini). The USA seed
        /// is [ESTIMATED] on a different equivalence scale, documented-irreconcilable at source -
        /// comparable in spirit, never in construction.</summary>
        public float Gini;

        /// <summary>ROUND 4 BATCH 2 (C2): real wage INDEX, base 100 at epoch per country - the HPI
        /// convention, applied by ruling for the HPI reason: the seed doc's growth figures mix three
        /// bases (net vs gross vs economy-wide), so the LEVEL series is display furniture and
        /// cross-country level comparison is explicitly not claimed. The simulation consumes GROWTH
        /// (nominal minus inflation), which the bases agree on directionally. Unbounded by
        /// construction, so the UI draws it on the negative-fill convention (§A.9b), never a
        /// gauge.</summary>
        public float RealWageIndex;

        /// <summary>ROUND 4 BATCH 3 (C1): housing cost overburden - % of the population in
        /// households spending &gt;40% of disposable income on housing, the Eurostat `ilc_lvho07a`
        /// WHOLE-POPULATION variant (`rskpovth=TOTAL`; the doc's own rule: this indicator is
        /// unusually variant-prone, record which cut or the number means nothing).
        /// ⚠ DELIBERATELY ASYMMETRIC: tracked for the EU five only. The USA has NO comparable
        /// figure at source (>40% disposable vs the US convention's >30%/>50% gross cuts), and the
        /// recorded ruling takes the seed doc's option 3 - homeownership is the USA's primary
        /// housing metric instead. Where Country.TracksHousingOverburden is false this field parks
        /// at 0 and the model never runs; that absence is the ruling made visible, not a gap.</summary>
        public float HousingOverburden;

        /// <summary>ROUND 4 BATCH 3 (C1): homeownership rate, % of HOUSEHOLDS owning (OECD
        /// Affordable Housing Database basis - "use this basis only" per the seed doc; the
        /// population-basis Eurostat figures are a different, larger number). All six tracked;
        /// the USA's PRIMARY housing metric per the asymmetry ruling. Generationally slow.</summary>
        public float Homeownership;

        /// <summary>ROUND 4 BATCH 3 (C1): house price index, base 100 at epoch per country - the
        /// R4-2 compounding-index class VERBATIM (level = display furniture, no cross-country level
        /// claim, sim consumes growth, §A.9b negative-fill display). The arc's first monetary
        /// coupling lives in its growth term: low policy rates inflate house prices.</summary>
        public float HousePriceIndex;

        /// <summary>ROUND 4 BATCH R4-5 (C5): labour productivity - GDP per hour worked, USD PPP
        /// (OECD DSD_PDB `GDPHRS`/`USD_PPP_H`, current prices, ref year 2022, live vintage
        /// retrieved 2026-08-02 - the series restates wholesale, so the retrieval date is part of
        /// the basis). Seeded as a real LEVEL, all six countries on the one identical basis - but
        /// **OWN-PAST-ONLY by the OECD's own methodology caution**: cross-country level comparison
        /// is not claimed anywhere (the Society ledger shows only the player's own country, which
        /// satisfies the caution structurally). Compounding class (the RealWageIndex kit minus its
        /// cyclical terms - pure 1:1 trend pass-through), level unbounded, §A.9b negative-fill
        /// display. Inputs-only: the PotentialGrowthRate COUPLING is ruled OUT of Round 4
        /// (ruling #4) - productivity reads and displays, nothing consumes it.</summary>
        public float Productivity;

        /// <summary>
        /// A stylized 0-100 crime index (higher = more crime), NOT a literal transformation of any
        /// single real indicator - "crime" as a broad concept has no single clean cross-country
        /// comparable metric the way poverty/labor-participation rates do. Seeded per country
        /// informed by real relative homicide-rate rankings (see WorldFactory), honestly labeled as
        /// illustrative. Mean-reverts each turn toward Country.BaselineCrimeIndex, adjusted by
        /// PoliceFundingLevel/SentencingSeverity and the same unemployment gap already used
        /// elsewhere - see MacroSystem.ApplyCrimeIndex. Hard-clamped to [0, 100].
        /// </summary>
        public float CrimeIndex;

        /// <summary>
        /// This country's incarceration rate, per 100,000 population - a real, well-documented World
        /// Prison Brief statistic (see WorldFactory for per-country sourcing), unlike CrimeIndex's own
        /// stylized scale. Mean-reverts toward Country.BaselinePrisonPopulationRate, adjusted by
        /// BailReformLevel/DrugPolicyLevel - see MacroSystem.ApplyPrisonPopulationRate. Hard-clamped
        /// to [0, 1000] (a generous gameplay safety bound, comfortably above any real-world value).
        /// </summary>
        public float PrisonPopulationRate;

        /// <summary>
        /// Round 3 item 3: a stylized 0-100 index (higher = more organized crime), NOT a literal
        /// transformation of any single real indicator - informed by the real Global Organized Crime
        /// Index (GI-TOC), with Italy's historic, extremely well-documented organized-crime
        /// organizations (Cosa Nostra, Camorra, 'Ndrangheta) as the clear, high-confidence highest of
        /// the six (see Country.BaselineOrganizedCrimeIndex for full sourcing). Mean-reverts each turn
        /// toward Country.BaselineOrganizedCrimeIndex, adjusted by PoliceFundingLevel/
        /// JudicialFundingLevel/BorderEnforcementLevel - see MacroSystem.ApplyOrganizedCrimeIndex.
        /// Hard-clamped to [0, 100].
        /// </summary>
        public float OrganizedCrimeIndex;

        /// <summary>
        /// Round 3 item 3: a stylized 0-100 index (higher = MORE corrupt, matching this project's own
        /// "higher = worse" convention), informed by (roughly 100 minus) the real Transparency
        /// International Corruption Perceptions Index, not a literal year-specific score (see
        /// Country.BaselineCorruptionIndex for full sourcing). Mean-reverts each turn toward
        /// Country.BaselineCorruptionIndex, adjusted by JudicialFundingLevel - see
        /// MacroSystem.ApplyCorruptionIndex. Hard-clamped to [0, 100].
        /// </summary>
        public float CorruptionIndex;

        // ---- THE EIGHT DEMOGRAPHIC SCALARS: READINGS OF THE COHORT SUBSTRATE SINCE F2 STEP 4 (2026-09-02) ----
        // Every field below is written by CohortDemographics.Apply from the 21-band pyramid on
        // Country.Cohorts and its anchored yearly step; none is stepped by a rule of its own any more.
        // The "Round 3 item 5" rules that used to step them (a secular birth-rate decline, three
        // aging drifts, a growth-rate reversion toward a typed steady-state growth rate (deleted with it), a population
        // clamp) were deleted with their twelve constants, per the cohort spec-let's collision map
        // §4.1. The constructor's arguments for these fields are the retired rules' seeds and are
        // overwritten at world creation (WorldFactory); their per-country distance from the readings
        // is recorded in COMPLETED.md §195.

        /// <summary>
        /// This country's total population, in MILLIONS (matching how GDP is stored at a human-
        /// readable scale). A READING: the pyramid's band total, interpolated day by day between the
        /// start and the end of the year the substrate is stepping (CohortDemographics.Apply). The
        /// pyramid is the 2024 published stock walked to the epoch on the publisher's own projection
        /// and anchored to it thereafter, so the population cannot run away and needs no clamp.
        /// </summary>
        public float Population;

        /// <summary>
        /// Crude birth rate, per 1,000 population per year. A READING: the year's inflow to the 0–4
        /// band, net of the immigration lever's migrants into it, over the mid-year population
        /// (CohortDemographics.Apply). Family policy reaches it as a fertility multiplier on the
        /// step's births, at the magnitude the retired additive rule had - see NaturalBirthRate.
        /// </summary>
        public float BirthRate;

        /// <summary>
        /// Crude death rate, per 1,000 population per year - HELD at its sourced seed (WorldFactory).
        /// The publisher's projection folds deaths and its own migration assumption into one survival
        /// ratio (D-6), so the substrate cannot read a death rate out; one of the two must be held to
        /// read the other, and a crude death rate is the better-observed and slower-moving of the
        /// two. The aging that would raise it lives inside the survival ratios. Stated in
        /// CohortDemographics' own doc.
        /// </summary>
        public float DeathRate;

        /// <summary>
        /// Net migration rate, per 1,000 population per year (positive = net inflow). A READING
        /// that closes the identity Δpopulation = births − deaths + net migration on the LEVERED
        /// step (CohortDemographics.Apply), so the immigration lever's people are in it by
        /// construction; they enter the pyramid over the sourced immigration age profile
        /// (CohortStepRateTable.ImmigrationProfile), never uniformly.
        /// </summary>
        public float NetMigrationRate;

        /// <summary>
        /// BirthRate's policy-INDEPENDENT trajectory: the crude birth rate of the NEUTRAL anchored
        /// step this year - the pyramid stepped with no lever, which follows the publisher's
        /// projection exactly (CohortDemographics.Step). The anchor semantics the retired rule had
        /// are kept on purpose (spec-let §4.2 named their loss "the single most likely silent
        /// breakage"): holding Country.FamilyPolicyLevel at any fixed value displaces BirthRate from
        /// this trajectory by a constant amount, never a compounding one, because next year's base is
        /// read from the publisher again rather than from this year's levered result.
        /// </summary>
        public float NaturalBirthRate;

        /// <summary>
        /// NetMigrationRate's policy-INDEPENDENT trajectory: the same identity reading taken on the
        /// NEUTRAL step (CohortDemographics.Apply) - which is exactly the net migration the
        /// publisher's projection ASSUMES for the year, given the held DeathRate. The retired
        /// aging-driven drift on this field was an authored rule; this is the publisher's own
        /// assumption instead. Country.BaselineNetMigrationRate (the sourced 2024 rate) stays the
        /// anchor the labor-force gap measures against, so a projection that assumes more
        /// immigration than 2024 had reads as a positive gap - which is what it is.
        /// </summary>
        public float NaturalNetMigrationRate;

        /// <summary>
        /// Old-age dependency ratio (65+ as a percentage of 15–64). A READING of the pyramid
        /// (PopulationCohorts.OldAgeDependencyRatio), interpolated day by day between the year's two
        /// ends - exactly computable now, which is why Country.BaselineDependencyRatio is re-seeded
        /// from the same pyramid at world creation so every gap-based effect opens at zero. It rises
        /// or falls as the publisher's projection and the levers say; nothing forces it one way.
        /// </summary>
        public float DependencyRatio;

        /// <summary>
        /// This country's net population growth rate, per 1,000 population per year: the year's
        /// step's own net result, (end total / start total − 1) × 1000 (CohortDemographics.Apply).
        /// The typed steady-state growth rate the retired reversion pulled toward was deleted with it and no longer
        /// enters the demography.
        /// </summary>
        public float PopulationGrowthRate;

        /// <summary>
        /// Government debt as a percentage of GDP (e.g. 124 means 124% of GDP) - matches how
        /// Unemployment/Inflation/TaxRate are stored in this codebase, not a raw 0-1 fraction.
        /// Derived, not stored, so it's always consistent with the current GDP and GovernmentDebt.
        /// </summary>
        public float DebtToGdpRatio => GDP > 0f ? GovernmentDebt / NominalGdp * 100f : 0f;   // P5-B6: a nominal stock over nominal GDP

        public EconomyState() { }

        public EconomyState(
            float gdp, float inflation, float unemployment, float approvalRating, float budget,
            float tradeBalance = 0f, float currencyStrength = 100f, float consumption = 0f, float investment = 0f,
            float potentialGdp = 0f, float inflationExpectations = 0f, float consumerConfidence = 1f, float businessConfidence = 1f,
            float governmentDebt = 0f, float povertyRate = 10f, float laborForceParticipationRate = 62f, float crimeIndex = 25f,
            float prisonPopulationRate = 100f, float organizedCrimeIndex = 25f, float corruptionIndex = 30f,
            float population = 50f, float birthRate = 10f, float deathRate = 10f, float netMigrationRate = 1f,
            float dependencyRatio = 30f, float populationGrowthRate = float.NaN,
            float naturalBirthRate = float.NaN, float naturalNetMigrationRate = float.NaN,
            float youthUnemployment = 15f, float lifeExpectancy = 80f,
            float gini = 30f, float realWageIndex = 100f,
            float housingOverburden = 0f, float homeownership = 65f, float housePriceIndex = 100f,
            float productivity = 85f)
        {
            GDP = gdp;
            Inflation = inflation;
            Unemployment = unemployment;
            ApprovalRating = approvalRating;
            Budget = budget;
            TradeBalance = tradeBalance;
            CurrencyStrength = currencyStrength;
            Consumption = consumption;
            Investment = investment;
            PotentialGDP = potentialGdp > 0f ? potentialGdp : gdp;
            InflationExpectations = inflationExpectations > 0f ? inflationExpectations : inflation;
            ConsumerConfidence = consumerConfidence;
            BusinessConfidence = businessConfidence;
            GovernmentDebt = governmentDebt;
            PovertyRate = povertyRate;
            LaborForceParticipationRate = laborForceParticipationRate;
            CrimeIndex = crimeIndex;
            YouthUnemployment = youthUnemployment;
            LifeExpectancy = lifeExpectancy;
            Gini = gini;
            RealWageIndex = realWageIndex;
            HousingOverburden = housingOverburden;
            Homeownership = homeownership;
            HousePriceIndex = housePriceIndex;
            Productivity = productivity;
            Population = population;
            BirthRate = birthRate;
            DeathRate = deathRate;
            NetMigrationRate = netMigrationRate;
            DependencyRatio = dependencyRatio;
            PopulationGrowthRate = float.IsNaN(populationGrowthRate)
                ? birthRate - deathRate + netMigrationRate
                : populationGrowthRate;
            NaturalBirthRate = float.IsNaN(naturalBirthRate) ? birthRate : naturalBirthRate;
            NaturalNetMigrationRate = float.IsNaN(naturalNetMigrationRate) ? netMigrationRate : naturalNetMigrationRate;
            PrisonPopulationRate = prisonPopulationRate;
            OrganizedCrimeIndex = organizedCrimeIndex;
            CorruptionIndex = corruptionIndex;
        }

        /// <summary>Returns a copy so the simulation can compute a next state without mutating the
        /// current one.
        ///
        /// ⚠ R4-3 STRUCTURAL FIX, derived not assumed: this was a positional hand-list into the ctor,
        /// and R4-1 proved that shape drifts silently (two fields added to the ctor never reached the
        /// list - clones reset them to defaults; caught and patched in R4-2, class-fixed here). This
        /// type is PURE VALUE STATE - every field is a public float, the one property is derived with
        /// no backing field - so MemberwiseClone IS an exact copy, absorbs every future field with no
        /// hand list to forget, and skips the ctor's seed-time fallback branches (PotentialGDP&lt;=0,
        /// NaN-defaulting) that a copy should never re-run. If a REFERENCE-TYPE field is ever added
        /// here, this becomes a shallow copy of that field and must be revisited - that is the one
        /// residue of the retired checklist entry.</summary>
        public EconomyState Clone()
        {
            return (EconomyState)MemberwiseClone();
        }

        /// <summary>A generic, fictional developed mixed economy - starting point for the player's country.</summary>
        public static EconomyState CreateDefault()
        {
            return new EconomyState(
                gdp: 20000f,
                inflation: 2.0f,
                unemployment: 5.0f,
                approvalRating: 50f,
                budget: 0f
            );
        }
    }
}
