using System;

namespace PoliSim.Data
{
    /// <summary>
    /// One of a country's four tracked infrastructure types (Round 2's "Infrastructure system") -
    /// a 0-100 ConditionIndex (higher = better condition), always present for every country (no
    /// implement/remove, the same "every country always has this" idiom Sector already established).
    ///
    /// Unlike CrimeIndex/PovertyRate/Sector (which mean-revert toward a seeded baseline anchor),
    /// ConditionIndex is a plain STOCK that moves via two flows each turn - a small constant decay
    /// (deferred maintenance: infrastructure needs growing real investment merely to hold steady, so
    /// a flat spending level still implies gradual real degradation) and an investment term driven by
    /// the country's EXISTING Infrastructure spending category, reusing the same signal that already
    /// feeds PotentialGrowthRate rather than inventing a parallel one - see
    /// MacroSystem.ApplyInfrastructureCondition. This mirrors the "SpendingLine Amount Ceiling"
    /// precedent (a stock that moves via flows, hard-clamped to a fixed range) more closely than the
    /// gap-to-baseline idiom used elsewhere, since a decay/investment mechanic is fundamentally a
    /// stock, not an equilibrium-seeking value - hard-clamped to [0, 100] every turn so it can never
    /// diverge or decay unboundedly, directly addressing the failure pattern
    /// ROADMAP_BRIEF.md's Round 2 ordering note flagged for this specific item.
    /// </summary>
    [Serializable]
    public class InfrastructureAsset
    {
        public InfrastructureType Type;

        /// <summary>0-100, higher = better condition. See WorldFactory for per-country/per-type seeding and its real-data sourcing.</summary>
        public float ConditionIndex;

        public InfrastructureAsset() { }

        public InfrastructureAsset(InfrastructureType type, float conditionIndex)
        {
            Type = type;
            ConditionIndex = conditionIndex;
        }

        /// <summary>Used by SimulationManager.PreviewTurn's throwaway country clone - ConditionIndex is mutated every turn, so the preview needs its own copy, not a shared reference.</summary>
        public InfrastructureAsset Clone()
        {
            return new InfrastructureAsset(Type, ConditionIndex);
        }
    }
}
