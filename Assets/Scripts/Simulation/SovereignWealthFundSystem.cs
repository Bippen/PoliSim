using PoliSim.Data;
using UnityEngine;

namespace PoliSim.Simulation
{
    /// <summary>
    /// The sovereign wealth fund's simple market-return model: a small random return per turn per
    /// asset class, centered on a real historical long-run AVERAGE nominal return (sourced via web
    /// search, not precisely fitted) and varying within a realistic historical range. Each asset
    /// class's own isolated System.Random - separate from EventSystem's, UnityEngine.Random, and
    /// GameController's _previewRandom - so drawing a return never perturbs any other RNG consumer's
    /// sequence, mirroring FederalReserveSystem.GenerateCandidates' own isolation precedent.
    /// </summary>
    public static class SovereignWealthFundSystem
    {
        private static readonly System.Random RandomSource = new System.Random();

        /// <summary>
        /// Real long-run average nominal annual returns (source: UBS Global Investment Returns
        /// Yearbook and related historical surveys) - equities ~6-7% REAL in developed markets, so
        /// ~9% nominal assuming ~2-3% average inflation; bonds notably lower and less volatile;
        /// real estate/infrastructure fall between the two. Applied per turn at this same magnitude,
        /// the same "treat a turn like roughly a year" convention every other rate in this model
        /// (PotentialGrowthRate, interest rates, etc.) already uses.
        /// </summary>
        private const float EquitiesAverageReturnPercent = 9f;
        private const float BondsAverageReturnPercent = 4.5f;
        private const float InfrastructureAverageReturnPercent = 7f;
        private const float RealEstateAverageReturnPercent = 6f;

        /// <summary>How far a single turn's actual return can vary from its average, in percentage points either direction - equities are the most volatile of the four, matching real-world relative volatility ordering; bonds the least.</summary>
        private const float EquitiesReturnVariance = 12f;
        private const float BondsReturnVariance = 3f;
        private const float InfrastructureReturnVariance = 4f;
        private const float RealEstateReturnVariance = 5f;

        /// <summary>The real long-run average return for an asset class - used by SimulationManager.PreviewTurn (deterministic, no RNG, matching PreviewTurn's own documented "side-effect-free" principle) instead of an actual random draw.</summary>
        public static float GetAverageReturnPercent(SovereignWealthAssetClass assetClass)
        {
            switch (assetClass)
            {
                case SovereignWealthAssetClass.Equities: return EquitiesAverageReturnPercent;
                case SovereignWealthAssetClass.Bonds: return BondsAverageReturnPercent;
                case SovereignWealthAssetClass.Infrastructure: return InfrastructureAverageReturnPercent;
                case SovereignWealthAssetClass.RealEstate: return RealEstateAverageReturnPercent;
                default: return 0f;
            }
        }

        private static float GetReturnVariance(SovereignWealthAssetClass assetClass)
        {
            switch (assetClass)
            {
                case SovereignWealthAssetClass.Equities: return EquitiesReturnVariance;
                case SovereignWealthAssetClass.Bonds: return BondsReturnVariance;
                case SovereignWealthAssetClass.Infrastructure: return InfrastructureReturnVariance;
                case SovereignWealthAssetClass.RealEstate: return RealEstateReturnVariance;
                default: return 0f;
            }
        }

        /// <summary>This turn's actual random return for an asset class - uniformly distributed within [average - variance, average + variance]. Used only for a real, committed turn (ApplyDomesticPolicy), never for PreviewTurn.</summary>
        private static float GetRandomReturnPercent(SovereignWealthAssetClass assetClass)
        {
            float average = GetAverageReturnPercent(assetClass);
            float variance = GetReturnVariance(assetClass);
            float roll = (float)RandomSource.NextDouble() * 2f - 1f;
            return average + roll * variance;
        }

        /// <summary>
        /// Applies this turn's market return to the fund: each asset class's share of TotalAssets
        /// (via SovereignWealthFund.GetNormalizedWeight) earns its own randomly-drawn return, summed
        /// and added directly to TotalAssets. Returns the total dollar return (positive or negative)
        /// so SimulationManager can report it as this turn's fiscal income figure.
        /// </summary>
        public static float ApplyReturns(SovereignWealthFund fund)
        {
            float totalReturn = 0f;
            foreach (SovereignWealthAssetClass assetClass in AssetClasses)
            {
                float share = fund.TotalAssets * fund.GetNormalizedWeight(assetClass);
                totalReturn += share * (GetRandomReturnPercent(assetClass) / 100f);
            }

            fund.TotalAssets = Mathf.Max(0f, fund.TotalAssets + totalReturn);
            return totalReturn;
        }

        /// <summary>The deterministic AVERAGE-return estimate of this turn's total return, for SimulationManager.PreviewTurn - does not mutate the fund or roll any randomness.</summary>
        public static float GetAverageReturnEstimate(SovereignWealthFund fund)
        {
            float totalReturn = 0f;
            foreach (SovereignWealthAssetClass assetClass in AssetClasses)
            {
                float share = fund.TotalAssets * fund.GetNormalizedWeight(assetClass);
                totalReturn += share * (GetAverageReturnPercent(assetClass) / 100f);
            }

            return totalReturn;
        }

        private static readonly SovereignWealthAssetClass[] AssetClasses =
        {
            SovereignWealthAssetClass.Equities,
            SovereignWealthAssetClass.Bonds,
            SovereignWealthAssetClass.Infrastructure,
            SovereignWealthAssetClass.RealEstate
        };
    }
}
