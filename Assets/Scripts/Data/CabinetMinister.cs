using System;

namespace PoliSim.Data
{
    /// <summary>
    /// One cabinet minister: an ORIGINAL FICTIONAL character - never a real person (Master Roadmap
    /// working-discipline rule 9, the same rule FedChair already established). Name, Philosophy, and
    /// Description are flavor/UI text plus the interactive-decision-pool selector (see
    /// CabinetMinisterPhilosophy); CompetenceBias is the only field with a passive, always-on
    /// mechanical effect - see CabinetSystem.GetCompetenceBias and each portfolio's own point-of-use
    /// comment (SimulationManager.ApplyRevenueAndSpending for FinanceTreasury, MacroSystem.
    /// ApplyCrimeIndex for InteriorJustice, MacroSystem.ApplyPovertyRate for HealthSocialAffairs,
    /// MacroSystem.ApplyYouthUnemployment for Education). ⚠ For Defense and ForeignAffairs the
    /// field is authored but INERT this pass - R4-4 ruling R3, see CabinetPortfolio's doc comment.
    /// </summary>
    [Serializable]
    public class CabinetMinister
    {
        public string Name;
        public CabinetPortfolio Portfolio;
        public CabinetMinisterPhilosophy Philosophy;
        public string Description;

        /// <summary>
        /// This minister's skill at the job - always a small, bounded, BENEFICIAL magnitude (never
        /// negative; a weaker candidate has a smaller bias, not a harmful one, since "hire someone
        /// actively bad at governing" isn't a real candidate archetype the way "Dovish vs. Hawkish" is
        /// a real, symmetric monetary-policy axis for FedChair.RateBias). Deliberately a SEPARATE axis
        /// from Philosophy - see CabinetMinisterPhilosophy's own doc comment.
        /// </summary>
        public float CompetenceBias;
        /// <summary>
        /// P2-5.2 (2026-09-02, the P2-5.1 page): four attributes, 0–100, `[AUTHORED-DRAFT]` per fictional candidate
        /// (CabinetSystem.Attributes - the Fed-chair pool's precedent; never a figure for a real person). Each owns
        /// one term: LOYALTY the resignation and leak events under pressure (CabinetSystem.TryRollCabinetEvents);
        /// KNOWLEDGE the Docket's disclosure of an option's estimate (GameController.DrawCabinetOptionEstimate);
        /// EFFICIENCY a multiplier on the portfolio's spending sensitivity (MacroSystem.ApplyCategorySpendingEffects,
        /// three portfolios own one); POPULARITY a multiplier on an option's approval figure and on the dismissal
        /// cost (CabinetSystem.ApplyDecisionOption, the Reshuffle button). 100 is "as written" - a minister with no authored
        /// figure (an older save) moves nothing, rolls nothing and hides nothing; absence is not a penalty.
        /// </summary>
        public float Loyalty = 100f;
        public float Knowledge = 100f;
        public float Efficiency = 100f;
        public float Popularity = 100f;

        public CabinetMinister() { }

        public CabinetMinister(string name, CabinetPortfolio portfolio, CabinetMinisterPhilosophy philosophy, string description, float competenceBias)
        {
            Name = name;
            Portfolio = portfolio;
            Philosophy = philosophy;
            Description = description;
            CompetenceBias = competenceBias;
        }
    }

    /// <summary>P2-5.2 (2026-09-02): what LOYALTY's term produces - a resignation or a leak, under pressure.</summary>
    public enum CabinetEventKind
    {
        Resignation,
        Leak,
    }

    /// <summary>P2-5.2: one cabinet event as it happened, kept on the country for the Docket's minister alerts.</summary>
    [Serializable]
    public class CabinetEventRecord
    {
        public DateTime Date;
        public CabinetPortfolio Portfolio;
        public string MinisterName;
        public CabinetEventKind Kind;
        public float ApprovalDelta;
        public string Text;
    }
}
