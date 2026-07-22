using System;

namespace PoliSim.Data
{
    /// <summary>
    /// One government spending line in a country's budget: which SpendingCategory, its current
    /// Amount (in the same $B-scale units as GDP, persistent - adjusted turn to turn by a PERCENTAGE
    /// change from PolicyDecision.SpendingLineChanges, not reset), and whether it's a Mandatory
    /// (entitlement/transfer program - adjustable, but within a narrower percentage range than
    /// Discretionary, reflecting the real political difficulty of entitlement reform) or
    /// Discretionary (wider range) line. Mirrors TaxLine's pattern. Mandatory lines are excluded from
    /// the national accounts identity's G term (transfers, not purchases - same reasoning as
    /// UnemploymentBenefitCost/InterestOnDebt); Discretionary lines' sum IS G for a country with a
    /// detailed SpendingLines portfolio - see SimulationManager.ApplyDomesticPolicy.
    /// </summary>
    [Serializable]
    public class SpendingLine
    {
        public SpendingCategory Category;
        public float Amount;
        public bool IsMandatory;

        /// <summary>
        /// This line's starting Amount, initialized at construction - the anchor SimulationManager
        /// clamps Amount against (see MinSpendingLineAmountRatio/MaxSpendingLineAmountRatio) so
        /// repeated PLAYER-driven percentage changes, however many turns they're stacked over, can
        /// never compound this line to an absurd multiple of where it started. For a Mandatory line
        /// this stays genuinely fixed forever (Mandatory has no automatic growth mechanism). For a
        /// Discretionary line it is NOT fixed - SimulationManager.ApplyDiscretionarySpendingGrowth
        /// grows it by the same factor as Amount's own automatic GDP-tracking growth each turn, so the
        /// ceiling this anchors keeps pace with GDP instead of silently freezing G in absolute dollar
        /// terms (see that method's doc comment and CLAUDE.md's "SpendingLine Amount Ceiling -
        /// Debt-to-Zero Fix" for why a genuinely-fixed anchor caused an ever-widening primary surplus
        /// that paid USA's debt to exactly 0 by turn ~70).
        /// </summary>
        public float SeedAmount;

        public SpendingLine() { }

        public SpendingLine(SpendingCategory category, float amount, bool isMandatory)
        {
            Category = category;
            Amount = amount;
            IsMandatory = isMandatory;
            SeedAmount = amount;
        }

        /// <summary>Used by SimulationManager.PreviewTurn's throwaway country clone - SpendingLine.Amount and (for a Discretionary line) SeedAmount are both mutated turn to turn, so the preview needs its own copies, not shared references. SeedAmount is copied explicitly (not re-derived from the current, possibly-mutated Amount) since it must stay independently anchored, not reset to Amount's current value.</summary>
        public SpendingLine Clone()
        {
            return new SpendingLine(Category, Amount, IsMandatory) { SeedAmount = SeedAmount };
        }
    }
}
