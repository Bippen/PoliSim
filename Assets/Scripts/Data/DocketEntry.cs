using System;

namespace PoliSim.Data
{
    /// <summary>
    /// PS-6 US-21b (Elias's ruling I12 (i), 2026-10-18): ONE BILL ON A PLAYER-PRESIDENT'S DOCKET - a bill both chambers passed and presented to him, held
    /// in its own pending slot until he answers or the period runs out (`PresidentialDocket`, `SimulationManager.UsVetoGate`). It rides the save inside its
    /// bill, the passage by reference to the division log's entry (the save's PreserveReferencesHandling) or whole where the log has evicted it.
    /// ADDITIVE: null on a bill is "not on the docket", which is every bill a save of an earlier format carries - before the docket a player-president
    /// answered on the day, so no saved bill was ever held.
    /// </summary>
    [Serializable]
    public class DocketEntry
    {
        /// <summary>The division the bill was presented on: the second chamber's passage, carrying the first's (`DivisionRecord.OtherChamber`).</summary>
        public DivisionRecord Passage;

        /// <summary>The day it was presented - the day both chambers had passed it. The period counts from the day after.</summary>
        public DateTime PresentedOn;

        /// <summary>The player-president's answer: 0 none yet, +1 signed, -1 vetoed - acted on at the bill's next day step, and honoured there whoever holds the
        /// presidency by then (the answer was given in office).</summary>
        public int Answer;

        /// <summary>The day the answer was given - the president it is presented to at the step is the president of THAT day. MinValue while unanswered.</summary>
        public DateTime AnsweredOn;

        /// <summary>For a US budget's act, which act it is (`UsBudgetProcedure.Act` as its number); -1 on every other bill.</summary>
        public int Act = -1;
    }
}
