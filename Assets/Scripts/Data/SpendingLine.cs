using System;

namespace PoliSim.Data
{
    /// <summary>
    /// One government spending line in a country's budget: which SpendingCategory, its current
    /// Amount (in the same $B-scale units as GDP, persistent - adjusted turn to turn for
    /// Discretionary lines by PolicyDecision.SpendingLineChanges, not reset), and whether it's a
    /// Mandatory (automatic entitlement/transfer, not player-adjustable in Phase 1) or Discretionary
    /// (player-adjustable) line. Mirrors TaxLine's pattern. Mandatory lines are excluded from the
    /// national accounts identity's G term (transfers, not purchases - same reasoning as
    /// UnemploymentBenefitCost/InterestOnDebt); Discretionary lines' sum IS G for a country with a
    /// detailed SpendingLines portfolio - see SimulationManager.ApplyDomesticPolicy.
    /// </summary>
    [Serializable]
    public class SpendingLine
    {
        public SpendingCategory Category;
        public float Amount;
        public bool IsMandatory;

        public SpendingLine() { }

        public SpendingLine(SpendingCategory category, float amount, bool isMandatory)
        {
            Category = category;
            Amount = amount;
            IsMandatory = isMandatory;
        }

        /// <summary>Used by SimulationManager.PreviewTurn's throwaway country clone - SpendingLine.Amount is mutated by ApplySpendingLineChanges, so the preview needs its own copies, not shared references.</summary>
        public SpendingLine Clone()
        {
            return new SpendingLine(Category, Amount, IsMandatory);
        }
    }
}
