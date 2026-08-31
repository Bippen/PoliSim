using System;
using PoliSim.Data;
using UnityEngine;

namespace PoliSim.Simulation
{
    /// <summary>
    /// The sovereign wealth fund's market-return model: each asset class earns its own randomly-drawn
    /// return per turn, normally distributed around a real historical long-run AVERAGE nominal return
    /// (not precisely fitted) with a real historical volatility (standard deviation), so a single turn
    /// CAN and DOES produce a genuine negative blended return some of the time - not just a smaller
    /// positive one - the same way a real diversified fund has real down years (Norway's GPFG, this
    /// model's own anchor - see CLAUDE.md's "Sovereign Wealth Fund" - had negative nominal years in
    /// 2018 and 2022). One shared `System.Random`, isolated from `EventSystem`'s, `UnityEngine.Random`,
    /// (mirrors `FederalReserveSystem.GenerateCandidates`'s own isolation precedent; it also named
    /// `GameController`'s `_previewRandom` until C-C14 deleted that field with the rolled display margin
    /// it existed for) - a single stream drawn from sequentially per asset class, not a separate
    /// instance per class (nothing here depends on the four classes' draws being independently seeded).
    /// </summary>
    public static class SovereignWealthFundSystem
    {
        private static System.Random RandomSource => SimulationRandom.For(SimulationRandom.Stream.SovereignWealth);

        /// <summary>
        /// Real long-run average NOMINAL annual returns per asset class - equities ~8% (global
        /// long-run average across developed markets); bonds ~4% (aggregate global bond returns,
        /// notably lower and less volatile); infrastructure ~7% and real estate ~7% (both fall
        /// between the two, real assets with some inflation-linked upside). At this game's default
        /// 40/30/15/15 (Equities/Bonds/Infrastructure/RealEstate) weighting this blends to ~6.5%,
        /// matching Norway's Government Pension Fund Global's own real long-run nominal average
        /// (~6-6.6% since 1998) despite GPFG running a materially more equity-heavy allocation
        /// (~70%) - expected, since this game's default mix is more conservative and a lower blended
        /// figure is the right ballpark here, not a precise target. Applied per turn at this same
        /// magnitude, the same "treat a turn like roughly a year" convention every other rate in this
        /// model (PotentialGrowthRate, interest rates, etc.) already uses.
        /// </summary>
        private const float EquitiesAverageReturnPercent = 8f;
        private const float BondsAverageReturnPercent = 4f;
        private const float InfrastructureAverageReturnPercent = 7f;
        private const float RealEstateAverageReturnPercent = 7f;

        /// <summary>
        /// Annualized standard deviation of return per asset class - real-world historical volatility
        /// ordering (equities most volatile, bonds least, infrastructure/real estate in between,
        /// real estate's reported volatility damped somewhat below listed-REIT levels since a
        /// sovereign fund's real estate book is typically unlisted/appraisal-based, same as GPFG's
        /// own). Standard deviation, not a hard band - the normal distribution these feed has no
        /// fixed min/max, so a severe multi-sigma year (a real crash, not just an unlucky roll near
        /// an artificial ceiling) remains possible, however rare.
        /// </summary>
        private const float EquitiesReturnStdDevPercent = 16f;
        private const float BondsReturnStdDevPercent = 6f;
        private const float InfrastructureReturnStdDevPercent = 10f;
        private const float RealEstateReturnStdDevPercent = 10f;

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

        private static float GetReturnStdDevPercent(SovereignWealthAssetClass assetClass)
        {
            switch (assetClass)
            {
                case SovereignWealthAssetClass.Equities: return EquitiesReturnStdDevPercent;
                case SovereignWealthAssetClass.Bonds: return BondsReturnStdDevPercent;
                case SovereignWealthAssetClass.Infrastructure: return InfrastructureReturnStdDevPercent;
                case SovereignWealthAssetClass.RealEstate: return RealEstateReturnStdDevPercent;
                default: return 0f;
            }
        }

        /// <summary>Box-Muller transform - one standard-normal sample from two uniform draws off the shared RandomSource. `1.0 - NextDouble()` on the first draw avoids ever taking Log(0).</summary>
        private static float NextStandardNormal()
        {
            double u1 = 1.0 - RandomSource.NextDouble();
            double u2 = RandomSource.NextDouble();
            return (float)(Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2.0 * Math.PI * u2));
        }

        /// <summary>This turn's actual random return for an asset class - normally distributed around its average with its own real-world standard deviation, floored at -100% (an asset class can lose its full value in a turn but never go further negative). Used only for a real, committed turn (ApplyDomesticPolicy), never for PreviewTurn.</summary>
        private static float GetRandomReturnPercent(SovereignWealthAssetClass assetClass)
        {
            float average = GetAverageReturnPercent(assetClass);
            float stdDev = GetReturnStdDevPercent(assetClass);
            return Mathf.Max(-100f, average + NextStandardNormal() * stdDev);
        }

        /// <summary>
        /// Draws this period's market return for the fund: each asset class's share of TotalAssets
        /// (via SovereignWealthFund.GetNormalizedWeight - the player's own Asset Class Mix sliders,
        /// so a more equity-heavy mix genuinely earns more on average and swings harder either way)
        /// earns its own randomly-drawn return, summed into one total dollar figure (positive OR
        /// NEGATIVE) so SimulationManager can report it as this period's fiscal income figure - a real
        /// down period is a real, reported revenue shortfall, not silently floored to zero.
        ///
        /// CONTINUOUS TIME PHASE 3: this used to be ApplyReturns, and applied the return to TotalAssets
        /// itself. It no longer does. The draw happens ONCE at a turn boundary and SimulationManager
        /// accrues 1/DaysPerTurn of it per day, so the fund's balance drifts instead of jumping - and the
        /// zero floor moved with it, into the daily step that now owns every write to TotalAssets.
        /// Drawing daily instead was rejected: 121x the RNG, every recorded baseline invalidated, and no
        /// modelling gain, since the draw's granularity is not what the migration is about.
        /// </summary>
        public static float DrawPeriodReturn(SovereignWealthFund fund)
        {
            float totalReturn = 0f;
            foreach (SovereignWealthAssetClass assetClass in AssetClasses)
            {
                float share = fund.TotalAssets * fund.GetNormalizedWeight(assetClass);
                totalReturn += share * (GetRandomReturnPercent(assetClass) / 100f);
            }

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
