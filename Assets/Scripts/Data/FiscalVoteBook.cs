using System;
using System.Collections.Generic;

namespace PoliSim.Data
{
    /// <summary>A-LOOP q13 (2026-10-19, `docs/specs/A_LOOP_PLAN.md` §4): one closed fiscal year as the vote reads it - the year the player reads
    /// (`FiscalTurnReport.BudgetBalance`, AF-17: the accrued flow and the year's one-offs) and nominal GDP at its close, the denominator the
    /// finance ministry divides the same balance by. Since K62 (2026-10-23) a year before the start is SOURCED (`FiscalVote.SeedPreRun`).</summary>
    public class FiscalVoteYear
    {
        /// <summary>The boundary the year closed on.</summary>
        public DateTime ClosedOn;
        /// <summary>K63 (2026-10-23): the day the year opened - a year counts against a government only when it opened under that government's
        /// tenure (`FiscalVote.Judge`).</summary>
        public DateTime OpenedOn;
        /// <summary>The closed year's balance, nominal billions, positive a surplus.</summary>
        public float Balance;
        /// <summary>Nominal GDP at the close, billions.</summary>
        public float NominalGdp;
        /// <summary>K62: for a year before the start, seeded from the sourced annual balance series (`Generated.FiscalBalanceRecord`) rather than played, the
        /// source year it reads (its number, as the record numbers it); 0 for a year the game played.</summary>
        public int SourceYear;
    }

    /// <summary>A-LOOP q13: <b>the fiscal record the vote reads</b> (`Country.FiscalVoteRecord`) - the latest closed years, oldest first, and the open year's
    /// flow to date with the days it covers and the day it opened, mirrored from the fiscal period the daily step accrues (`FiscalVote.NoteDay`) and
    /// emptied as a year closes (`FiscalVote.CloseYear`); seeded at a world's entry with the sourced years before the start (K62, `FiscalVote.SeedPreRun`).
    /// Read by the vote alone, never by the simulation.</summary>
    public class FiscalVoteBook
    {
        /// <summary>The closed years, oldest first - the newest <see cref="Kept"/> of them.</summary>
        public List<FiscalVoteYear> Closed = new List<FiscalVoteYear>();
        /// <summary>The open year's accrued flow balance to date, nominal billions (the period's daily balances summed; the one-offs are not in it).</summary>
        public float OpenFlow;
        /// <summary>The days the open year's flow covers.</summary>
        public int OpenDays;
        /// <summary>K63: the day the open year opened - the start, then each boundary.</summary>
        public DateTime OpenedOn;
        /// <summary>K101 (2026-10-24): the governing party whose tenure the book counts (the prime minister's party, the president's party) and the day that
        /// tenure began - it runs on through re-elections and re-formations under the same party, and restarts where another party leads.</summary>
        public string TenureParty;
        public DateTime TenureFrom;
        /// <summary>The s-fisc review 2's L6: the formation day of the government the book last saw, so each formation is printed once.</summary>
        public DateTime FormationSeen;

        /// <summary>CONVENTION: the closed years kept - the four Brender &amp; Drazen's term variable reads (E-4 to E-1).</summary>
        public const int Kept = 4;

        /// <summary>The book's own copy, for the preview's clone.</summary>
        public FiscalVoteBook Copy()
        {
            var copy = new FiscalVoteBook { OpenFlow = OpenFlow, OpenDays = OpenDays, OpenedOn = OpenedOn, TenureParty = TenureParty, TenureFrom = TenureFrom, FormationSeen = FormationSeen };
            foreach (FiscalVoteYear y in Closed) { copy.Closed.Add(new FiscalVoteYear { ClosedOn = y.ClosedOn, OpenedOn = y.OpenedOn, Balance = y.Balance, NominalGdp = y.NominalGdp, SourceYear = y.SourceYear }); }
            return copy;
        }
    }
}
