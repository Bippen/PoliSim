using System;
using System.Collections.Generic;

namespace PoliSim.Data
{
    /// <summary>A-LOOP q13 (2026-10-19, `docs/specs/A_LOOP_PLAN.md` §4): one closed fiscal year as the vote reads it - the year the player reads
    /// (`FiscalTurnReport.BudgetBalance`, AF-17: the accrued flow and the year's one-offs) and nominal GDP at its close, the denominator the
    /// finance ministry divides the same balance by.</summary>
    public class FiscalVoteYear
    {
        /// <summary>The boundary the year closed on.</summary>
        public DateTime ClosedOn;
        /// <summary>The closed year's balance, nominal billions, positive a surplus.</summary>
        public float Balance;
        /// <summary>Nominal GDP at the close, billions.</summary>
        public float NominalGdp;
    }

    /// <summary>A-LOOP q13: <b>the fiscal record the vote reads</b> (`Country.FiscalVoteRecord`) - the latest closed years, oldest first, and the open year's
    /// flow to date with the days it covers, mirrored from the fiscal period the daily step accrues (`FiscalVote.NoteDay`) and emptied as a year
    /// closes (`FiscalVote.CloseYear`). Read by the vote alone, never by the simulation.</summary>
    public class FiscalVoteBook
    {
        /// <summary>The closed years, oldest first - the newest <see cref="Kept"/> of them.</summary>
        public List<FiscalVoteYear> Closed = new List<FiscalVoteYear>();
        /// <summary>The open year's accrued flow balance to date, nominal billions (the period's daily balances summed; the one-offs are not in it).</summary>
        public float OpenFlow;
        /// <summary>The days the open year's flow covers.</summary>
        public int OpenDays;

        /// <summary>CONVENTION: the closed years kept - the four Brender &amp; Drazen's term variable reads (E-4 to E-1).</summary>
        public const int Kept = 4;

        /// <summary>The book's own copy, for the preview's clone.</summary>
        public FiscalVoteBook Copy()
        {
            var copy = new FiscalVoteBook { OpenFlow = OpenFlow, OpenDays = OpenDays };
            foreach (FiscalVoteYear y in Closed) { copy.Closed.Add(new FiscalVoteYear { ClosedOn = y.ClosedOn, Balance = y.Balance, NominalGdp = y.NominalGdp }); }
            return copy;
        }
    }
}
