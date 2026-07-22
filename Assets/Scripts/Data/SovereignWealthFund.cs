using System;

namespace PoliSim.Data
{
    /// <summary>
    /// A country's sovereign wealth fund - null (the default, every country) means it doesn't exist;
    /// the player creates one via an immediate action (mirrors TaxLine.IsImplemented's on/off pattern,
    /// not a PolicyDecision field), the same way FedChair's null/non-null switches USA's interest-rate
    /// mechanic. USA-first only in this pass (see "Sovereign Wealth Fund" in CLAUDE.md) - the class
    /// itself is country-agnostic so a later pass could enable it elsewhere without changes here.
    /// TotalAssets is the fund's actual size (same $B scale as GDP), grown each turn by this turn's
    /// contribution (an expense - see SimulationManager.GetSwfContribution) plus market returns (income -
    /// see SovereignWealthFundSystem). The four asset-class weights are independently player-adjustable
    /// and NOT required to sum to 100 - GetNormalizedWeight divides each by their live sum so they
    /// always behave as if they do, without forcing the UI to enforce that constraint directly.
    /// </summary>
    [Serializable]
    public class SovereignWealthFund
    {
        public float TotalAssets;

        /// <summary>Percent of GDP contributed to the fund each turn - a new budget expense. Player-adjustable via PolicyDecision.SwfContributionRateOverride.</summary>
        public float ContributionRatePercent = 1f;

        /// <summary>Percent of the fund invested domestically (the rest, 100 minus this, is international) - tracked and displayed, but this pass does NOT model different domestic-vs-international returns (see CLAUDE.md); both draw from the same asset-class return model.</summary>
        public float DomesticAllocationPercent = 50f;

        public float EquitiesWeight = 40f;
        public float BondsWeight = 30f;
        public float InfrastructureWeight = 15f;
        public float RealEstateWeight = 15f;

        /// <summary>Each weight normalized against the live sum of all four, so they always behave as if they summed to 100 regardless of what the player set them to individually. Floors the sum at 1 to avoid a divide-by-zero if the player somehow zeroes all four.</summary>
        public float GetNormalizedWeight(SovereignWealthAssetClass assetClass)
        {
            float sum = Math.Max(1f, EquitiesWeight + BondsWeight + InfrastructureWeight + RealEstateWeight);
            switch (assetClass)
            {
                case SovereignWealthAssetClass.Equities: return EquitiesWeight / sum;
                case SovereignWealthAssetClass.Bonds: return BondsWeight / sum;
                case SovereignWealthAssetClass.Infrastructure: return InfrastructureWeight / sum;
                case SovereignWealthAssetClass.RealEstate: return RealEstateWeight / sum;
                default: return 0f;
            }
        }

        /// <summary>Deep copy for SimulationManager.PreviewTurn's throwaway country clone - every field here is mutated during a turn (TotalAssets by contributions/returns, the rest by ApplySwfPolicyChanges), so this can't be a shared reference the way CurrentFedChair is.</summary>
        public SovereignWealthFund Clone()
        {
            return new SovereignWealthFund
            {
                TotalAssets = TotalAssets,
                ContributionRatePercent = ContributionRatePercent,
                DomesticAllocationPercent = DomesticAllocationPercent,
                EquitiesWeight = EquitiesWeight,
                BondsWeight = BondsWeight,
                InfrastructureWeight = InfrastructureWeight,
                RealEstateWeight = RealEstateWeight
            };
        }
    }

    /// <summary>The four asset classes a SovereignWealthFund can allocate across - kept to four, not the full real-world list, per the brief's explicit scope.</summary>
    public enum SovereignWealthAssetClass
    {
        Equities,
        Bonds,
        Infrastructure,
        RealEstate
    }
}
