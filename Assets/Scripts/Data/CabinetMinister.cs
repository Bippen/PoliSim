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
}
