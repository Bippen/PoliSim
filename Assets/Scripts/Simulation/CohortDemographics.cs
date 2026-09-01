using PoliSim.Data;
using PoliSim.Data.Generated;
using UnityEngine;

namespace PoliSim.Simulation
{
    /// <summary>
    /// F2 step 4 (P-I2 stage 4, 2026-09-02) — THE COHORT SUBSTRATE IS THE DEMOGRAPHY. The eight
    /// demographic scalars on <see cref="EconomyState"/> (Population, BirthRate, DeathRate,
    /// NetMigrationRate, NaturalBirthRate, NaturalNetMigrationRate, DependencyRatio,
    /// PopulationGrowthRate) are no longer stepped by their own rules; they are READINGS of the
    /// 21-band pyramid on <see cref="Country.Cohorts"/>, which steps once a year on
    /// <see cref="PopulationCohorts.StepOneYearAnchored"/>'s arithmetic. The old rules
    /// (MacroSystem's ApplyDemographicRates and ApplyPopulationGrowth, twelve constants, a secular
    /// decline, three aging drifts, a growth-rate reversion and a population clamp) are DELETED, not
    /// reconciled — the cohort spec-let's collision map §4.1 said double-stepping was the first way
    /// this build goes wrong, and running both "to compare" would have been exactly that.
    ///
    /// <para><b>One year, read daily.</b> The pyramid holds the stock at the START of the current
    /// turn (a turn is a year, `DaysPerTurn` = 365). Each day the year's step is computed from that
    /// stock and the levers as they stand, and the scalars are read at the day's fraction of the
    /// year — Population and DependencyRatio interpolated linearly between the year's two ends, the
    /// rates as the year's own flows. The step is COMMITTED to the pyramid once, at the turn
    /// boundary (<see cref="CommitYear"/>). The daily form at day 365 and the turn form are the same
    /// computation at fraction 1, so the aggregation-equivalence check holds by construction.</para>
    ///
    /// <para><b>Anchor semantics survive, which §4.2 called the single most likely silent
    /// breakage.</b> The step is anchored on the publisher's projection rebased onto the seeded
    /// pyramid (D-15 (c), D-19 (b)); the NEUTRAL step — no lever — follows the publisher exactly, and
    /// a held lever displaces the pyramid by a constant amount rather than a compounding one.
    /// `NaturalBirthRate` is therefore the neutral step's own crude birth rate this year, and
    /// `BirthRate` the levered step's: the two levers are re-pointed here in the same pass, at the
    /// SAME magnitudes they had (§4.4 — a lever left pointing at a deleted rule is a dead lever):
    /// - Family policy: the old rule added `FamilyPolicyBirthRateSensitivity × (level − 50)` points
    ///   per thousand to the natural crude birth rate; here the same points per thousand become the
    ///   fertility multiplier `(natural + effect) / natural` on the step's births.
    /// - Immigration policy: the old rule added `ImmigrationPolicyNetMigrationSensitivity × (level −
    ///   50)` per thousand to the natural net migration rate; here the same per-thousand figure is
    ///   the additive migration the step distributes over the sourced immigration age profile.</para>
    ///
    /// <para>⚠ <b>What is derived and what is held, stated.</b> The publisher's projection folds
    /// deaths and its own migration assumption into one survival ratio (D-6), so the substrate
    /// cannot tell a death from an emigrant, and one of the two must be held to read the other.
    /// `DeathRate` is HELD at its seeded, sourced crude figure — a slow-moving observation; the
    /// aging that would raise it is inside the survival ratios the publisher folded with migration
    /// and cannot be read out. The migration readings then close the identity
    /// `Δpopulation = births − deaths + net migration`: `NaturalNetMigrationRate` on the neutral
    /// step — which is exactly the net migration the publisher's projection ASSUMES, the
    /// policy-independent trajectory that field has always meant — and `NetMigrationRate` on the
    /// levered step, so the lever's people are in it by construction. ⚠ The first build held the
    /// migration anchor and derived deaths instead, and read Sweden's crude death rate as 6.0 against
    /// a sourced 9.5 — the projection's assumed immigration booked as people not dying. A death rate
    /// is the better-observed of the two, so it is the one held. `BirthRate` is the inflow to the
    /// 0–4 band net of the lever's migrants into it. These are readings, not observations, and
    /// their per-country distance from the retired seeds is in the record (`COMPLETED.md` §195).</para>
    ///
    /// <para>⚠ <b>The substrate's calendar.</b> The seeded pyramid is the 2024 stock (Eurostat
    /// 1 January; the US Census 1 July). The game opens on 2026-01-01, so `WorldFactory` walks the
    /// seed two neutral years along the publisher's trajectory (<see cref="WalkToEpoch"/>) — no figure
    /// typed, and the substrate year is the calendar year from then on. Beyond the projection's last
    /// year (2100) the last published pyramid is the anchor for every later year: the population
    /// holds its 2100 shape, displaced only by the levers. Stated rather than extrapolated.</para>
    /// </summary>
    public static class CohortDemographics
    {
        /// <summary>The year the substrate is stepping during the given turn: the epoch's year plus the turn count (turn 0 = 2026).</summary>
        public static int SubstrateYear(int turn) => SimulationManager.EpochDate.Year + turn;

        /// <summary>The rebased target pyramid for a year (D-19 (b)), or null when the country has no seeded pyramid or projection.</summary>
        public static float[] Target(CountryId id, int year)
        {
            float[] seeded = PopulationPyramids.Bands.TryGetValue(id, out float[] s) ? s : null;
            float[] projBase = PopulationProjections.For(id, PopulationProjections.FirstYear);
            float[] projYear = PopulationProjections.For(id, year);
            return PopulationCohorts.RebasedTarget(seeded, projBase, projYear);
        }

        /// <summary>One country's year, as the step computes it: the start stock, the levered end stock, and the flows the scalars are read from.</summary>
        public readonly struct YearStep
        {
            public readonly float[] Start;
            public readonly float[] Next;
            public readonly float[] Neutral;
            /// <summary>Inflow to the 0–4 band net of the lever's migrants into it, in millions - the levered step's births.</summary>
            public readonly float Births;
            /// <summary>The same for the neutral step.</summary>
            public readonly float NeutralBirths;
            /// <summary>The immigration lever's additive net migration this year, in millions (signed).</summary>
            public readonly float LeverMigrationMillions;

            public YearStep(float[] start, float[] next, float[] neutral, float births, float neutralBirths, float leverMigrationMillions)
            {
                Start = start; Next = next; Neutral = neutral;
                Births = births; NeutralBirths = neutralBirths; LeverMigrationMillions = leverMigrationMillions;
            }

            public bool IsValid => Start != null && Next != null;
        }

        /// <summary>
        /// Computes the year's step for a country from its current pyramid and levers WITHOUT
        /// committing it. Returns an invalid step when the country carries no pyramid or no rate
        /// table - the caller then leaves the scalars where they are rather than inventing a year.
        /// </summary>
        public static YearStep Step(Country country, int year)
        {
            PopulationCohorts cohorts = country.Cohorts;
            CohortStepRates rates = CohortStepRateTable.For(country.Id);
            if (cohorts == null || rates == null) { return default; }
            float[] tThis = Target(country.Id, year);
            float[] tNext = Target(country.Id, year + 1);

            float[] neutral = cohorts.AnchoredNext(rates, tThis, tNext);
            float neutralBirths = PopulationCohorts.ImpliedBirths(cohorts.Counts, neutral, rates, 0f);
            float startTotal = Sum(cohorts.Counts);
            float neutralMid = 0.5f * (startTotal + Sum(neutral));
            float naturalCrude = neutralMid > 0f ? neutralBirths / neutralMid * 1000f : 0f;

            // The two levers, at the magnitudes the retired rules gave them (see the class doc).
            float familyEffect = LaborCouplings.FamilyPolicyBirthRateSensitivity * (country.FamilyPolicyLevel - 50f);
            float fertilityMultiplier = naturalCrude > 0f ? Mathf.Max(0f, (naturalCrude + familyEffect) / naturalCrude) : 1f;
            float immigrationEffect = LaborCouplings.ImmigrationPolicyNetMigrationSensitivity * (country.ImmigrationPolicyLevel - 50f);
            float leverMigration = immigrationEffect / 1000f * startTotal;
            float[] profile = CohortStepRateTable.ImmigrationProfile(country.Id);
            if (profile == null) { leverMigration = 0f; }   // no sourced age profile: the lever has nowhere to put people, and says so rather than spreading them uniformly (spec-let §2 (4))

            float[] next = fertilityMultiplier == 1f && leverMigration == 0f
                ? neutral
                : cohorts.AnchoredNext(rates, tThis, tNext, fertilityMultiplier, leverMigration, profile);
            float migrantsIntoBand0 = profile != null ? leverMigration * profile[0] : 0f;
            float births = PopulationCohorts.ImpliedBirths(cohorts.Counts, next, rates, migrantsIntoBand0);
            return new YearStep(cohorts.Counts, next, neutral, births, neutralBirths, leverMigration);
        }

        /// <summary>
        /// Writes the eight scalars as readings of the year's step at <paramref name="yearFraction"/>
        /// (0 = the start of the turn, 1 = its end). The pyramid itself is not touched.
        /// </summary>
        public static void Apply(Country country, int year, float yearFraction)
        {
            YearStep step = Step(country, year);
            if (!step.IsValid) { return; }
            float f = Mathf.Clamp01(yearFraction);
            EconomyState state = country.State;

            float startTotal = Sum(step.Start);
            float nextTotal = Sum(step.Next);
            float mid = 0.5f * (startTotal + nextTotal);
            float neutralMid = 0.5f * (startTotal + Sum(step.Neutral));

            state.NaturalBirthRate = neutralMid > 0f ? step.NeutralBirths / neutralMid * 1000f : 0f;
            state.BirthRate = mid > 0f ? step.Births / mid * 1000f : 0f;
            // DeathRate is HELD at its sourced seed (the class doc says why); the two migration readings
            // close the identity Δpopulation = births − deaths + net migration on the neutral and the
            // levered step respectively, so the lever's people are inside NetMigrationRate by construction.
            float neutralDeaths = state.DeathRate / 1000f * neutralMid;
            float leveredDeaths = state.DeathRate / 1000f * mid;
            float neutralMigration = (Sum(step.Neutral) - startTotal) - step.NeutralBirths + neutralDeaths;
            float leveredMigration = (nextTotal - startTotal) - step.Births + leveredDeaths;
            state.NaturalNetMigrationRate = neutralMid > 0f ? neutralMigration / neutralMid * 1000f : 0f;
            state.NetMigrationRate = mid > 0f ? leveredMigration / mid * 1000f : 0f;
            state.PopulationGrowthRate = startTotal > 0f ? (nextTotal / startTotal - 1f) * 1000f : 0f;
            state.Population = Mathf.Lerp(startTotal, nextTotal, f);
            float startRatio = new PopulationCohorts(step.Start).OldAgeDependencyRatio;
            float nextRatio = new PopulationCohorts(step.Next).OldAgeDependencyRatio;
            state.DependencyRatio = Mathf.Lerp(startRatio, nextRatio, f);
        }

        /// <summary>The turn form: the year's readings at its end. The preview uses this on its clone.</summary>
        public static void ApplyTurn(Country country, int year) => Apply(country, year, 1f);

        /// <summary>The daily form: day <paramref name="dayOfTurn"/> (1..DaysPerTurn) of the year. Day DaysPerTurn equals the turn form exactly.</summary>
        public static void ApplyDaily(Country country, int year, int dayOfTurn) =>
            Apply(country, year, dayOfTurn / (float)SimulationManager.DaysPerTurn);

        /// <summary>Commits the year's step to the pyramid at the turn boundary - the one place the substrate itself moves in play.</summary>
        public static void CommitYear(Country country, int year)
        {
            YearStep step = Step(country, year);
            if (!step.IsValid) { return; }
            country.Cohorts.Counts = step.Next;
        }

        /// <summary>
        /// World creation: the seeded pyramid is the 2024 stock; the game opens in 2026. Walks the
        /// seed neutrally along the publisher's own trajectory from its vintage to the epoch's year
        /// and reads the scalars at the start of the epoch year. Levers are at their neutral 50 at
        /// creation, so the walk is the anchored step following the projection exactly.
        /// </summary>
        public static void WalkToEpoch(Country country)
        {
            if (country.Cohorts == null) { return; }
            CohortStepRates rates = CohortStepRateTable.For(country.Id);
            if (rates == null) { return; }
            int epochYear = SimulationManager.EpochDate.Year;
            for (int year = PopulationProjections.FirstYear; year < epochYear; year++)
            {
                country.Cohorts.Counts = country.Cohorts.AnchoredNext(rates, Target(country.Id, year), Target(country.Id, year + 1));
            }
            Apply(country, epochYear, 0f);
        }

        private static float Sum(float[] counts)
        {
            float s = 0f;
            for (int i = 0; i < counts.Length; i++) { s += counts[i]; }
            return s;
        }
    }
}
