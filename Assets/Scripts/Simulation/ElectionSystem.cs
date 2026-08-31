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
        /// <summary>
        /// Turns between elections. **A presidential term: 4 turns, because a turn is a year.**
        ///
        /// Was 12 when a turn was 121 days (12 x 121 = 1452 days = 3.98 years). It moved with
        /// `SimulationManager.DaysPerTurn` on 2026-08-10 and must always move with it - between them
        /// these are the project's only two statements of how long a turn is, and `MacroSystem`'s
        /// `YearsPerTurn` is derived from THIS one (`4f / ElectionCycle`) while every daily constant is
        /// derived from the other. Change one alone and the two conventions silently disagree: before
        /// this change they already did, by 0.5% (121/365 = 0.3315 years per turn against YearsPerTurn's
        /// 0.3333). They now agree exactly at 1.0 year per turn.
        /// </summary>
        public const int ElectionCycle = 4;

        /// <summary>[AUTHORED-DRAFT]: approval rating below this at an election turn loses the election.
        /// ⚠ **Narrowed at D-5 (a), 2026-09-01**: this rule now decides only the four countries whose vote
        /// model returns `NotImplemented`. Where a chamber can be formed, office is decided by whether the
        /// player's party sits in the cabinet (`GovernmentFormation`), which is a mechanism rather than a
        /// threshold. Nothing measures "the approval at which a real government falls" - it is not a
        /// quantity real politics has - so the figure was authored and stays authored.</summary>
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
