namespace PoliSim.Simulation
{
    /// <summary>
    /// THE THIRD DECLARED TABLE (pass 6, tariff costs, 2026-08-27): the three forces that make a tariff
    /// cost something, one named constant each beside its provenance - the LaborCouplings /
    /// CrimeJusticeCouplings shape. Pass 5 routed tariff revenue to the books and measured the lever it
    /// left: a 50% override on every partner was a costless 5-11%-of-GDP revenue button for the EU
    /// five (imports static, nothing retaliated, an overrides-only bill had direction 0). These three
    /// answer it: the change in the tariff wedge passes through to prices for a year (TradeCosts ->
    /// SimulationManager's boundary re-plan -> MacroSystem.ApplyPhillipsCurveInflation), partners
    /// mirror an override's excess over the standing rate back onto our exports
    /// (TradeSystem.GetRetaliatoryTariffRate), and overrides enter the vote as the change in the
    /// average tariff on imports (ParliamentSystem.GetTradeBillDirection).
    ///
    /// EVERY CONSUMER BRANCHES ON ITS CONSTANT (`> 0f`), never multiplies by it: the wired-inert
    /// control (all three at 0f with the whole plumbing live - the couplings precedent) must be the
    /// same code with the forces skipped, and a `x 0f` can still flip a -0f and move float codegen
    /// (the pass-4 one-ulp lesson, MacroSystem.RecordApprovalAttribution's own doc). The constants
    /// stay after the build so each channel is revertible by one literal as well as by commit.
    /// </summary>
    public static class TradeCosts
    {
        /// <summary>
        /// Fraction of a change in the tariff take (as a share of GDP) that prints as inflation for the
        /// year it lands. 1.0 is DERIVED FROM THE STATIC-VOLUME PLACEHOLDER, not chosen: imports are
        /// static (TradeSystem.ApplyTradeEffects, `effectiveImports = ImportVolume`), so the same
        /// quantity sells at the same pre-tariff price and the whole take is paid by domestic buyers -
        /// border incidence is 100% by construction. The border-price literature's near-complete
        /// pass-through corroborates it but is NOT the source and is recorded UNVERIFIED-EXTERNAL
        /// (pass 5's form; the repo holds no citation); a retail figure below 1 would be an invented
        /// number. Its stakes are measured, not asserted: TariffCostsDiagnostic runs the lever at 1.0
        /// and at 0.5 through PassThroughMeasurementScale. The wedge is a price-LEVEL term, so
        /// expectations look through the part of it that actually printed (MacroSystem.
        /// ApplyInflationExpectations' lookThroughPp) - see the pass-6 record for the ruling.
        /// </summary>
        public const float ImportPricePassThrough = 1f;

        /// <summary>
        /// Fraction of the EXCESS of a country's override over the rate it would otherwise charge a
        /// partner that the partner mirrors back onto that country's exports. 1.0 is a CONVENTION -
        /// "their tariff on our exports mirrors ours", the roadmap's own wording for the queued item -
        /// the one value with no free parameter, recorded INVENTED-as-convention in pass 5's
        /// UNVERIFIED-EXTERNAL form (no partner behaviour exists in the model to derive from; the
        /// response is instant and memoryless, a first cut). Cuts are unanswered (the excess floors
        /// at 0) and a base-rate hike is unanswered (a country's base rate IS its standing rate) -
        /// both stated residuals in the record. See TradeSystem.GetRetaliatoryTariffRate.
        /// </summary>
        public const float RetaliationMirrorFraction = 1f;

        /// <summary>
        /// Whether per-partner overrides enter the Trade bill's vote. 1.0 is a unit identity: with
        /// the override term on, ParliamentSystem.GetTradeBillDirection reads the change in the
        /// import-weighted AVERAGE tariff on the country's imports - the same tau-bar the
        /// pass-through formula uses - in the tariff-rate points the base term already scored, so no
        /// conversion factor is invented (the SWF-analogy exclusion rested on one being missing). The
        /// vote reads the SIGN of the direction only, so this converts "free" into "contested on the
        /// fiscal axis" - the model's only axis until item 10; recorded as the pass's taste-adjacent
        /// call, one literal to revert.
        /// </summary>
        /// <remarks>[AUTHORED-DRAFT], and its own summary says so more bluntly than any mark could: the conversion factor is INVENTED, the vote reads only the SIGN, and it is recorded as one literal to revert.</remarks>
        public const float OverrideDirectionWeight = 1f;

        /// <summary>
        /// HARNESS-ONLY (the ForeignPolicyCadenceMultiplier shape): a multiplier TariffCostsDiagnostic
        /// sets to measure the pass-through constant's stakes at a second value (0.5) without a
        /// second build. **1 leaves the constant exactly as it is** (a `x 1f` is bit-exact), it is
        /// only ever read inside the branch ImportPricePassThrough already gates, and nothing in play
        /// sets it - the game never reads a value other than 1.
        /// </summary>
        public static float PassThroughMeasurementScale = 1f;
    }
}
