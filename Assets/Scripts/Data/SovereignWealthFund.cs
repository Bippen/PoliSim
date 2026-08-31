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

        /// <summary>
        /// Percent of GDP contributed to the fund each turn - a new budget expense when positive.
        /// Can also go negative (Round 3 item 1, the SWF drawdown mechanic) - a real, player-chosen
        /// withdrawal during a recession/emergency, not an automatic response, which shrinks
        /// TotalAssets by that same amount instead of growing it (see SimulationManager.
        /// GetSwfContribution). Player-adjustable via PolicyDecision.SwfContributionRateOverride.
        /// </summary>
        public float ContributionRatePercent = 1f;

        /// <summary>
        /// Percent of the fund invested domestically (the rest, 100 minus this, is international) —
        /// tracked and displayed, but this pass does NOT model different domestic-vs-international returns
        /// (see CLAUDE.md); both draw from the same asset-class return model.
        ///
        /// ⚠ **C-N6, DECIDED AND LOGGED 2026-08-31 (Elias's decide-and-log ruling): THE FIELD STAYS AND
        /// ITS CONSUMER IS BILLED.** `LeverLivenessCheck` found on its first run that
        /// `PolicyDecision.SwfDomesticAllocationOverride` moves nothing at all — the value is clamped,
        /// written here, cloned, seeded per country and carried on a `BudgetBill`, and **nothing reads
        /// it.** The ruling's fork: if a consumer was ever intended, bill it and leave a recorded gap
        /// note; if not, delete the field and its whole plumbing.
        ///
        /// **A consumer WAS intended.** CLAUDE.md's own Round-3 record calls this *"a deliberate scope
        /// simplification, honestly disclosed, not a gap"*, and names the intended consumer: **differing
        /// domestic-vs-international returns.** Deferred on purpose, not forgotten — so the field stays
        /// and the consumer is billed.
        ///
        /// ⚠ **What the bill needs, and why it cannot be guessed:** a sourced spread between a fund's
        /// domestic and international returns, per country. Norway's GPFG — this model's own anchor —
        /// invests almost entirely ABROAD by mandate and so cannot supply a domestic leg; a Swedish
        /// AP-fund basis would be a different institution on a different mandate. **Until that spread is
        /// sourced this dial is honest scenery, and no player-facing surface may imply it does
        /// anything.** Register row **C-N6**.
        /// </summary>
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
