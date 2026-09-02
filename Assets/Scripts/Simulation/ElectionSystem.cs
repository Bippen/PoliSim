namespace PoliSim.Simulation
{
    /// <summary>
    /// The election calendar - which turns are election turns. Country-agnostic: it operates on the
    /// turn number passed in and nothing else.
    ///
    /// ⚠ P2-0.2 (2026-09-02): this class used to carry the game's first election rule as well - an
    /// approval threshold, "re-elected" above it and "election lost" below - and that rule was the
    /// last clause of the pre-item-10 politics (D0's map named it as what item 10 replaces). It is
    /// retired: the only election outcome a player sees is election night's count
    /// (`GameController.ShowElectionNight`) and the office test C-R4 ruled (`GovernmentFormation.Form`
    /// - is the player's party in the cabinet the chamber forms). A country whose vote model returns
    /// NotImplemented holds no election and reaches no verdict; its record says why
    /// (`NationalElection.NotHeldReason`). `OfficeTestDiagnostic` asserts the threshold is gone.
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

        /// <summary>True on turn numbers that are a multiple of ElectionCycle (and not turn 0, before any turn has run).</summary>
        public static bool IsElectionTurn(int turnNumber)
        {
            return turnNumber > 0 && turnNumber % ElectionCycle == 0;
        }
    }
}
