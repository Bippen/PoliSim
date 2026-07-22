using System;

namespace PoliSim.Data
{
    /// <summary>
    /// Illustrative real-world-scale cost of each WelfareProgramType, as a percentage of GDP AT FULL
    /// (100%) GenerosityLevel - gameplay-tuning constants, not precise budget figures, mirroring
    /// TaxTypeBaseShares' "rough illustrative weight" idiom. UBI is meaningfully expensive (paying
    /// every resident a flat amount, universally, is the most expensive way to move the needle on
    /// poverty); NegativeIncomeTax/MeansTestedWelfare are targeted (income-tested) so cost much less
    /// per point of GenerosityLevel; UniversalHealthcare is a large program in its own right (real
    /// single-payer systems typically run 8-11% of GDP); HousingAssistance/ChildcareSubsidies are
    /// narrow, much smaller programs.
    /// </summary>
    public static class WelfareProgramCostShares
    {
        public const float UbiCostShare = 18f;
        public const float NegativeIncomeTaxCostShare = 8f;
        public const float MeansTestedWelfareCostShare = 6f;
        public const float UniversalHealthcareCostShare = 10f;
        public const float HousingAssistanceCostShare = 1.5f;
        public const float ChildcareSubsidiesCostShare = 1f;

        public static float GetCostShareOfGdp(WelfareProgramType type)
        {
            switch (type)
            {
                case WelfareProgramType.UBI: return UbiCostShare;
                case WelfareProgramType.NegativeIncomeTax: return NegativeIncomeTaxCostShare;
                case WelfareProgramType.MeansTestedWelfare: return MeansTestedWelfareCostShare;
                case WelfareProgramType.UniversalHealthcare: return UniversalHealthcareCostShare;
                case WelfareProgramType.HousingAssistance: return HousingAssistanceCostShare;
                case WelfareProgramType.ChildcareSubsidies: return ChildcareSubsidiesCostShare;
                default: return 0f;
            }
        }
    }

    /// <summary>
    /// One welfare instrument in a country's welfare portfolio: which WelfareProgramType, its current
    /// GenerosityLevel (0-100%, persistent - set turn to turn by
    /// PolicyDecision.WelfareGenerosityOverrides, not reset - mirrors TaxLine.Rate exactly), and
    /// whether it's currently implemented (toggled immediately by the player, not deferred to Advance
    /// Turn - see GameController's Welfare Policy tab, mirroring TaxLine.IsImplemented). Cost only
    /// comes from implemented programs; see SimulationManager.GetTotalWelfareCost.
    /// </summary>
    [Serializable]
    public class WelfareProgram
    {
        public WelfareProgramType Type;
        public float GenerosityLevel;
        public bool IsImplemented;

        /// <summary>Derived from Type via WelfareProgramCostShares, not stored, so every WelfareProgram of the same Type always agrees.</summary>
        public float CostShareOfGdp => WelfareProgramCostShares.GetCostShareOfGdp(Type);

        public WelfareProgram() { }

        public WelfareProgram(WelfareProgramType type, float generosityLevel, bool isImplemented)
        {
            Type = type;
            GenerosityLevel = generosityLevel;
            IsImplemented = isImplemented;
        }

        /// <summary>Used by SimulationManager.PreviewTurn's throwaway country clone - WelfareProgram.GenerosityLevel is mutated by ApplyWelfareGenerosityChanges, so the preview needs its own copies, not shared references.</summary>
        public WelfareProgram Clone()
        {
            return new WelfareProgram(Type, GenerosityLevel, IsImplemented);
        }
    }
}
