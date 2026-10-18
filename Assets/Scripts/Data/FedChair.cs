using System;

namespace PoliSim.Data
{
    /// <summary>
    /// One Federal Reserve chair: an ORIGINAL FICTIONAL character - never a real past or present Fed
    /// chair or any real person (see FederalReserveSystem.CandidatePool and CLAUDE.md's "Federal
    /// Reserve" section). Name, Philosophy, and Description are flavor/UI text; RateBias is the only
    /// field with mechanical effect - see FederalReserveSystem.ApplyFedChairInterestRate.
    /// </summary>
    [Serializable]
    public class FedChair
    {
        public string Name;
        public FedChairPhilosophy Philosophy;
        public string Description;

        /// <summary>Percentage points added to TaylorRule.GetSuggestedInterestRate before clamping - positive (Hawkish) or negative (Dovish), near 0 for Moderate.</summary>
        public float RateBias;

        public FedChair() { }

        /// <summary>AF-11 (GA-1-5): a copy for a counterfactual world - no head is shared between two worlds, as <see cref="CabinetMinister.Copy"/>.</summary>
        public FedChair Copy() => (FedChair)MemberwiseClone();

        public FedChair(string name, FedChairPhilosophy philosophy, string description, float rateBias)
        {
            Name = name;
            Philosophy = philosophy;
            Description = description;
            RateBias = rateBias;
        }
    }
}
