using PoliSim.Data;

namespace PoliSim.Simulation
{
    /// <summary>Outcome of one election check for a country - not stored on EconomyState, since only the player's country runs elections for now (see ElectionSystem's class comment).</summary>
    public class ElectionResult
    {
        public bool Won;
        public float ApprovalAtElection;
        public float Margin;
    }

    /// <summary>
    /// Elections: a game-rule heuristic, not economic theory, so it's kept separate from
    /// MacroSystem. Deliberately country-agnostic - it operates on whichever EconomyState/turn
    /// number is passed in rather than hardcoding "the player" here. Which country is the player is
    /// a UI-layer decision (see GameController.PlayerCountryId, already hardcoded there), so that's
    /// also where the resulting IsGameOver/game-over-reason state belongs, not here.
    /// </summary>
    public static class ElectionSystem
    {
        /// <summary>Turns between elections.</summary>
        public const int ElectionCycle = 12;

        /// <summary>Approval rating below this at an election turn loses the election.</summary>
        public const float LosingThreshold = 35f;

        /// <summary>True on turn numbers that are a multiple of ElectionCycle (and not turn 0, before any turn has run).</summary>
        public static bool IsElectionTurn(int turnNumber)
        {
            return turnNumber > 0 && turnNumber % ElectionCycle == 0;
        }

        /// <summary>Checks a country's ApprovalRating against LosingThreshold at an election turn.</summary>
        public static ElectionResult RunElection(EconomyState state)
        {
            float margin = state.ApprovalRating - LosingThreshold;
            return new ElectionResult
            {
                Won = margin >= 0f,
                ApprovalAtElection = state.ApprovalRating,
                Margin = margin
            };
        }
    }
}
