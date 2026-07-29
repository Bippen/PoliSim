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
    /// </summary>
    [Serializable]
    public class StatHistory
    {
        public const int MaxEntries = 50;

        public readonly List<float> Gdp = new List<float>();
        public readonly List<float> Unemployment = new List<float>();
        public readonly List<float> Inflation = new List<float>();
        public readonly List<float> ApprovalRating = new List<float>();
        public readonly List<float> DebtToGdpRatio = new List<float>();
        public readonly List<float> PovertyRate = new List<float>();
        public readonly List<float> InterestRate = new List<float>();

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
