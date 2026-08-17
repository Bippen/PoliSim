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

        /// <summary>Unemployment rate, as a percentage (e.g. 5.0 = 5.0%).</summary>
        public float Unemployment;

        /// <summary>Public approval of the government, 0-100.</summary>
        public float ApprovalRating;

        /// <summary>Current government budget balance, in the same currency units as GDP. Negative values indicate deficit.</summary>
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
        /// PotentialGrowthRate. Used for Okun's Law's growth gap and the Taylor Rule's output gap.
        /// </summary>
        public float PotentialGDP;

        /// <summary>Adaptively-formed expectation of inflation, used by the Phillips Curve. See MacroSystem.ApplyInflationExpectations.</summary>
        public float InflationExpectations;

        /// <summary>Consumer confidence index, 1.0 = neutral. Scales Consumption; nothing currently feeds this back.</summary>
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
        /// Seeded per country from real World Bank/OECD data (see WorldFactory); mean-reverts each
        /// turn toward Country.BaselineLaborForceParticipationRate, adjusted by the same
        /// unemployment gap already used elsewhere (a discouraged/encouraged-worker effect - see
        /// MacroSystem.ApplyLaborForceParticipationRate). A tracked stat only - nothing currently
        /// targets it directly with a policy lever.
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

        /// <summary>
        /// Round 3 item 5, Part A: this country's total population, in MILLIONS (matching how GDP is
        /// stored at a human-readable scale rather than raw units) - seeded from real 2024/2025 data
        /// (USA 341.8, Germany 83.6, France 69.1, Italy 58.9, Poland 37.5, Sweden 10.6 - see
        /// WorldFactory). Evolves each turn from (BirthRate - DeathRate + NetMigrationRate)/1000 x
        /// Population - see MacroSystem.ApplyPopulationGrowth. Floored well above zero (MinPopulation)
        /// so a shrinking population can still recover instead of locking at exactly 0, and hard-capped
        /// at a generous gameplay safety bound (not a realistic constraint) - see MacroSystem for both.
        /// </summary>
        public float Population;

        /// <summary>
        /// Round 3 item 5, Part A: crude birth rate, per 1,000 population per turn - seeded from real
        /// data (see WorldFactory). Drifts down slowly on its own (a real, well-documented, near-
        /// universal secular fertility decline across developed nations - see
        /// MacroSystem.ApplyDemographicRates), floored well above zero at a realistic low-fertility
        /// bound. No policy lever in Part A - Part B's Family Policy adjusts this, deliberately kept
        /// modest given real-world evidence on pro-natalist policy's effect on fertility is itself
        /// small and contested.
        /// </summary>
        public float BirthRate;

        /// <summary>
        /// Round 3 item 5, Part A: crude death rate, per 1,000 population per turn - seeded from real
        /// data (see WorldFactory). Drifts up slowly as DependencyRatio rises above its own baseline (a
        /// real, well-documented mechanical effect - an aging population structurally raises the crude
        /// death rate even with no change in age-specific mortality) - see
        /// MacroSystem.ApplyDemographicRates. Hard-capped at a generous gameplay safety bound.
        /// </summary>
        public float DeathRate;

        /// <summary>
        /// Round 3 item 5, Part A: net migration rate, per 1,000 population per turn (positive = net
        /// inflow) - seeded from real data (see WorldFactory). Drifts up slowly as DependencyRatio rises
        /// above its own baseline - a real, discussed phenomenon (aging developed economies leaning
        /// more on immigration to offset a shrinking working-age population), distinct from BirthRate's
        /// own independent secular-decline drift. No policy lever in Part A - Part B's Immigration
        /// Policy adjusts this directly, a more responsive real-world lever than BirthRate so it can
        /// have a comparatively larger (but still bounded) effect. See MacroSystem.ApplyDemographicRates.
        /// </summary>
        public float NetMigrationRate;

        /// <summary>
        /// Round 3 item 5, Part B: BirthRate's policy-INDEPENDENT secular trajectory - evolves ONLY
        /// via BirthRateSecularDeclineRate's own drift (see MacroSystem.ApplyDemographicRates), never
        /// touched by Country.FamilyPolicyLevel. BirthRate itself is recomputed FRESH each turn as
        /// Clamp(NaturalBirthRate + this turn's policy offset, ...) rather than accumulating the
        /// policy offset onto itself turn after turn - necessary because a CONSTANT per-turn additive
        /// policy term (the first version of this lever) ratchets BirthRate to its hard ceiling within
        /// single-digit turns and parks it there, reintroducing the exact "no reversion, runs to an
        /// extreme and stays" failure pattern the Population growth-rate corrections above were written
        /// to fix - one layer upstream. Keeping FamilyPolicyLevel's effect as a bounded OFFSET from a
        /// policy-independent trajectory, recomputed fresh rather than compounded, avoids that failure
        /// mode entirely: holding the slider at any fixed value produces a constant (not ever-growing)
        /// shift from the natural trend, so BirthRate keeps following its underlying secular decline
        /// merely offset by the policy, not pinned at MaxBirthRate forever.
        /// </summary>
        public float NaturalBirthRate;

        /// <summary>
        /// Round 3 item 5, Part B: NetMigrationRate's policy-INDEPENDENT trajectory (aging-driven drift
        /// only, never touched by Country.ImmigrationPolicyLevel) - same "fresh offset, not compounded"
        /// reasoning as NaturalBirthRate above, see MacroSystem.ApplyDemographicRates.
        /// </summary>
        public float NaturalNetMigrationRate;

        /// <summary>
        /// Round 3 item 5, Part A: old-age dependency ratio (65+ population as a percentage of
        /// working-age 15-64 population) - the single derived aging/dependency proxy this pass uses,
        /// deliberately NOT a full age-cohort/population-pyramid model (see Country.
        /// BaselineDependencyRatio for full sourcing). Rises as the DeathRate-versus-BirthRate gap
        /// persists (aging accelerates as natural decrease continues) - see
        /// MacroSystem.ApplyDemographicRates. Hard-clamped to [Country.BaselineDependencyRatio's own
        /// realistic floor via MinDependencyRatio, MaxDependencyRatio] - can rise, never assumed to
        /// reverse in this pass (real developed-world aging trends are one-directional over any
        /// timescale this game's turns plausibly represent).
        /// </summary>
        public float DependencyRatio;

        /// <summary>
        /// Round 3 item 5, Part A (corrected): this country's net population growth rate, per-1000
        /// population per turn - the actual quantity Population evolves by (Population *= 1 +
        /// PopulationGrowthRate/1000, see MacroSystem.ApplyPopulationGrowth). Distinct from the raw
        /// (BirthRate - DeathRate + NetMigrationRate) figure: that raw figure is only ever a pull on
        /// this rate, which itself mean-reverts each turn toward Country.SteadyStateGrowthRate - the
        /// same reversion idiom Unemployment/Inflation/DebtToGdpRatio already use, added because the
        /// original design let the raw birth/death/migration gap drive Population directly and
        /// indefinitely, producing implausible aggregate outcomes over a 500-turn horizon despite each
        /// individual rate staying within its own realistic bound. Seeded at world-creation time equal
        /// to each country's own turn-1 raw implied rate (avoiding a turn-1 discontinuity, the same
        /// idiom every other Baseline-anchored variable uses) - see WorldFactory.
        /// </summary>
        public float PopulationGrowthRate;

        /// <summary>
        /// Government debt as a percentage of GDP (e.g. 124 means 124% of GDP) - matches how
        /// Unemployment/Inflation/TaxRate are stored in this codebase, not a raw 0-1 fraction.
        /// Derived, not stored, so it's always consistent with the current GDP and GovernmentDebt.
        /// </summary>
        public float DebtToGdpRatio => GDP > 0f ? GovernmentDebt / GDP * 100f : 0f;

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
            float housingOverburden = 0f, float homeownership = 65f, float housePriceIndex = 100f)
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
