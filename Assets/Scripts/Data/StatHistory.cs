using System;
using System.Collections.Generic;

namespace PoliSim.Data
{
    /// <summary>
    /// Rolling numeric history (last MaxEntries turns) of a country's key tracked stats - a UI-facing
    /// convenience so a dashboard graph can show a trend without re-deriving it from the Recent Turns
    /// text log, which is a formatted string per turn, not raw numbers. Appended once per turn by
    /// SimulationManager.AdvanceTurn, for every country (not just the player's), after that turn's
    /// state is fully settled - never touched by PreviewTurn's throwaway clone, so a live slider drag
    /// can never leak a phantom data point into the real history.
    ///
    /// Political Systems Overhaul Part C ("last N changes" pagination): raised from 50 to 250 (5
    /// pages of GraphRenderer's own 50-turn display window) so pagination has real older data to page
    /// back into, not just blank pages past the first - a bounded, still-capped increase (still a
    /// fixed-size rolling window, just a bigger one), not new tracked data or simulation logic.
    /// </summary>
    [Serializable]
    public class StatHistory
    {
        public const int MaxEntries = 250;

        public readonly List<float> Gdp = new List<float>();
        public readonly List<float> Unemployment = new List<float>();
        public readonly List<float> Inflation = new List<float>();
        public readonly List<float> ApprovalRating = new List<float>();
        public readonly List<float> DebtToGdpRatio = new List<float>();
        public readonly List<float> PovertyRate = new List<float>();
        public readonly List<float> InterestRate = new List<float>();

        // Added for Phase 4 of the UI revamp's per-tab graph rollout (Trade/Labor Market/Crime &
        // Justice tabs) - all four already exist as EconomyState fields computed every turn by the
        // real simulation, so recording them here is purely additive bookkeeping, not new logic.
        public readonly List<float> TradeBalance = new List<float>();
        public readonly List<float> LaborForceParticipationRate = new List<float>();
        public readonly List<float> CrimeIndex = new List<float>();
        public readonly List<float> PrisonPopulationRate = new List<float>();

        // Round 3 item 3: two more Crime & Justice tab stats, same "already an EconomyState field,
        // purely additive bookkeeping" reasoning as the four above.
        public readonly List<float> OrganizedCrimeIndex = new List<float>();
        public readonly List<float> CorruptionIndex = new List<float>();

        /// <summary>
        /// Appends this turn's already-settled values. <paramref name="interestRate"/> is passed
        /// separately (not read from <paramref name="state"/>) since the rate lives on the country's
        /// CurrencyZone, not EconomyState.
        /// </summary>
        public void Append(EconomyState state, float interestRate)
        {
            AppendBounded(Gdp, state.GDP);
            AppendBounded(Unemployment, state.Unemployment);
            AppendBounded(Inflation, state.Inflation);
            AppendBounded(ApprovalRating, state.ApprovalRating);
            AppendBounded(DebtToGdpRatio, state.DebtToGdpRatio);
            AppendBounded(PovertyRate, state.PovertyRate);
            AppendBounded(InterestRate, interestRate);
            AppendBounded(TradeBalance, state.TradeBalance);
            AppendBounded(LaborForceParticipationRate, state.LaborForceParticipationRate);
            AppendBounded(CrimeIndex, state.CrimeIndex);
            AppendBounded(PrisonPopulationRate, state.PrisonPopulationRate);
            AppendBounded(OrganizedCrimeIndex, state.OrganizedCrimeIndex);
            AppendBounded(CorruptionIndex, state.CorruptionIndex);
        }

        private static void AppendBounded(List<float> buffer, float value)
        {
            buffer.Add(value);
            if (buffer.Count > MaxEntries)
            {
                buffer.RemoveAt(0);
            }
        }
    }
}
