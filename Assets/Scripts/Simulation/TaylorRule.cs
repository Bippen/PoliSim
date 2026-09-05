using PoliSim.Data;
using UnityEngine;

namespace PoliSim.Simulation
{
    /// <summary>
    /// Taylor rule: the "suggested" policy rate from a country's inflation gap and its CYCLICAL
    /// gap - read as the unemployment gap against the country's own NAIRU, NOT the raw level gap
    /// between GDP and PotentialGDP (pass 4 of the ruled build order, 2026-08-26; the record is
    /// CLAUDE.md "Pass 4 ships - the Taylor path reads the unemployment gap").
    ///
    /// Why the unemployment gap. The level gap (GDP - PotentialGDP)/PotentialGDP is a persistent
    /// per-country LEVEL in this model, not a cycle: it is the fixed point of the identity's own
    /// dynamics (G is discretionary-only, C+I ~0.79 of GDP, the attractor anchors period-open
    /// potential), so no seed value can close it - CLAUDE.md "Discretionary Spending Growth"
    /// proved that in 2026-07 and the pass 4 derivation measured it again at HEAD: USA -14.5% for a
    /// thousand turns (sd 0.6), Poland -7, Italy -4.5, Germany -2.7. Weighted at 0.5, that term
    /// pinned the USA's suggestion at the 0-floor for 95-98 of the 101 turns of the ruled window
    /// while inflation ran 3.5%, collapsed five of eight Fed chairs onto one identical trajectory,
    /// dragged the Eurozone blend by ~1 pp, and (through the housing rate gap) compounded the USA
    /// house-price index at +3.9% a year. The unemployment gap is centred (-0.03 +/- 0.19 pp, every
    /// country, both seeds), it is the variable the Phillips curve drives inflation with, and NAIRU
    /// is the seeded structural baseline ten other consumers already read - the codebase's own
    /// "gaps, not absolute levels" principle, applied to this path.
    ///
    /// Live consumers - this is NOT reference-only, whatever older notes say:
    /// FederalReserveSystem.ApplyFedChairInterestRate (the USA's damped chair path),
    /// EurozoneRateSystem.GetBlendedSuggestedRate (the ECB blend), SimulationManager.PreviewTurn
    /// (the preview), and the Federal Reserve tab's reading lines. Sweden and Poland set their own
    /// rate; for them the reading is advisory until Riksbank-B (the roadmap's Step 4 block).
    /// </summary>
    public static class TaylorRule
    {
        /// <summary>The central bank's announced inflation target for this country, in percent.</summary>
        /// <remarks>SOURCED as real policy targets rather than a modelling choice. 2% is the announced
        /// target of the ECB (Germany, France, Italy), the Federal Reserve (USA) and the Riksbank (Sweden,
        /// on CPIF). ⚠ **Poland is 2.5%**: the NBP targets 2.5% with a symmetric +-1 percentage point band
        /// (verified 2026-09-01 at the NBP's own monetary-policy guidelines). Until 2026-09-02 this was one
        /// constant of 2 for all six, which sat INSIDE Poland's tolerance band and was not its target - S-24.
        /// A per-country figure is the fix; a per-country table with one authored entry would not be, so
        /// every value here is the institution's own published number.</remarks>
        public const float DefaultInflationTarget = 2f;
        /// <remarks>SOURCED - the NBP's own target (see InflationTarget's remarks).</remarks>
        public const float DefaultInflationTargetPoland = 2.5f;
        public static float DefaultInflationTargetFor(CountryId country) =>
            country == CountryId.Poland ? DefaultInflationTargetPoland : DefaultInflationTarget;
        /// <summary>P4-C3 third category (2026-09-05): the target is the ZONE'S state now (CurrencyZone.InflationTarget), seeded from the defaults above and reached by the monetary-regime laws where the parliament owns its bank.</summary>
        public static float InflationTarget(Country country) => Zone(country).InflationTarget;

        /// <summary>Assumed neutral real interest rate, in percent.</summary>
        /// <remarks>[AUTHORED-DRAFT], and this one deserves the mark most: the neutral real rate (r*) is UNOBSERVABLE and actively contested - published estimates for these economies have ranged from below zero to above two in the last decade. 2% is the classic Taylor (1993) assumption, kept for that reason and not because anything measures it now.</remarks>
        public const float DefaultNeutralRealRate = 2f;
        /// <summary>P4-C3: the zone's (CurrencyZone.NeutralRealRate), seeded at the default.</summary>
        public static float NeutralRealRate(Country country) => Zone(country).NeutralRealRate;

        /// <summary>Weight on the inflation gap (actual inflation minus target).</summary>
        /// <remarks>SOURCED as the canonical specification: Taylor (1993) sets the coefficient on the inflation gap at 0.5 in the original rule. The weight is the rule's own, not a fit to this model.</remarks>
        public const float DefaultInflationGapWeight = 0.5f;
        /// <summary>P4-C3: the zone's (CurrencyZone.InflationGapWeight), seeded at the default.</summary>
        public static float InflationGapWeight(Country country) => Zone(country).InflationGapWeight;

        /// <summary>
        /// Percentage points of suggested rate per percentage point the unemployment rate sits BELOW
        /// its NAIRU (a tight labour market raises the reading; slack lowers it).
        ///
        /// A TEXTBOOK CONVENTION, stated as such: Taylor's 0.5 on the output gap times Okun's ~2 pp
        /// of output per pp of unemployment - the substitution the Fed's own published rule variants
        /// make (2 x (u* - u) for the output gap). It is deliberately NOT derived from
        /// MacroSystem.OkunCoefficient: that codes the DIFFERENCE form (unemployment moves with the
        /// growth gap) under a 0.7/turn reversion and implies no level relation at all - measured, the
        /// model's own cyclical output gap leads the unemployment gap by one turn with the OPPOSITE
        /// sign, because GDP blips revert within a turn and the difference form reads the reversion
        /// as a downturn. The stakes of this constant are small and were measured before it was
        /// chosen: at no-policy the term is +0.05 pp on average with sd 0.2, and 1.0 against the
        /// model-native 0.5/0.7 = 0.71 moves the reading by ~0.06 pp.
        /// </summary>
        public const float DefaultUnemploymentGapWeight = 1.0f;
        /// <summary>P4-C3: the zone's (CurrencyZone.UnemploymentGapWeight), seeded at the default.</summary>
        public static float UnemploymentGapWeight(Country country) => Zone(country).UnemploymentGapWeight;

        /// <summary>The zone the rule reads for this country. A zone from a save older than the four fields is seeded once, here, from the defaults
        /// (the country's own default target - a euro member's zone gets 2 %, Poland's 2.5 %); a country with no zone (a bare test fixture) reads a private default zone.</summary>
        private static readonly CurrencyZone FallbackZone = new CurrencyZone("(no zone)", 0f);
        public static CurrencyZone Zone(Country country)
        {
            CurrencyZone zone = country.CurrencyZone ?? FallbackZone;
            if (!zone.MonetaryParametersSeeded)
            {
                zone.SeedMonetaryParameters(DefaultInflationTargetFor(country.Id), DefaultNeutralRealRate, DefaultInflationGapWeight, DefaultUnemploymentGapWeight);
            }
            return zone;
        }

        /// <summary>
        /// Output gap as a percentage of potential GDP - the LEVEL gap, positive above trend. Kept
        /// available as a reference reading (nothing consumes it at HEAD - the trajectory dump records
        /// the rule's own term instead); the rule no longer reads it (see the class doc for why a term
        /// on it was a per-country constant, not a cycle).
        /// </summary>
        public static float GetOutputGapPercent(Country country)
        {
            EconomyState state = country.State;
            if (state.PotentialGDP <= 0f)
            {
                return 0f;
            }

            return (state.GDP - state.PotentialGDP) / state.PotentialGDP * 100f;
        }

        /// <summary>The gap the rule reads, in percentage points, signed so that POSITIVE means a tight labour market: NAIRU minus the unemployment rate.</summary>
        public static float GetUnemploymentGapPercent(Country country)
        {
            return country.NaturalUnemploymentRate - country.State.Unemployment;
        }

        /// <summary>The rule's cyclical term in percentage points of rate: UnemploymentGapWeight times the gap. What TrajectoryBaselineDump records as Taylor.GapTermPp.</summary>
        public static float GetGapTermPercentagePoints(Country country)
        {
            return UnemploymentGapWeight(country) * GetUnemploymentGapPercent(country);
        }

        /// <summary>
        /// Suggested interest rate = neutral real rate + inflation + weighted inflation gap + the
        /// cyclical term, floored at 0 BEFORE any Fed chair's RateBias is added (the chair path
        /// clamps the biased target separately - see FederalReserveSystem.ApplyFedChairInterestRate).
        /// </summary>
        public static float GetSuggestedInterestRate(Country country)
        {
            EconomyState state = country.State;
            float inflationGap = state.Inflation - InflationTarget(country);

            float suggested = NeutralRealRate(country) + state.Inflation + InflationGapWeight(country) * inflationGap + GetGapTermPercentagePoints(country);
            return Mathf.Max(0f, suggested);
        }
    }
}
