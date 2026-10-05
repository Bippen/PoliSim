using System;
using System.Collections.Generic;
using PoliSim.Data;
using PoliSim.Elections;
using UnityEngine;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// §772 (Elias's ruling E1): THE FRESH WORLD THE 2025 FIT AND ITS ACCEPTANCE READ - one definition, so the factors are fitted on the world they are
    /// tested on. <b>Built as a new game builds a world, on the eve of the first vote of record</b>: the epoch set first and the world created on it -
    /// the order a new game takes (`GameController`), so it seats what the record holds that day (the government of record, the chamber the last
    /// election seated, the published economy since that election) and reads that chamber's history. Then one day stepped by the game's own loop
    /// (`SimulationManager.AdvanceDay` - the hook holds the first round) and the days to the run-off by the same loop on the same world (the hook holds
    /// the run-off). "A fresh world taken straight to 1 June 2025" (the ruling's acceptance test). The game's poll on the first vote's day is read again
    /// after the step, by the same call the hook makes, on the state the hook read (nothing the hook writes is read by the poll); the term reading it
    /// returns is kept, so a check can hold that the poll carries the government's record.
    /// <para>⚠ PREMISE, DECLARED (§772's review): a game PLAYED from Poland's 2023 start reaches 18 May 2025 on another basis - its prediction reads the
    /// history of the chamber seated at its own epoch (the standing history gap, §767), and its government is the one its own formation seated - so the
    /// factors fitted on this world do not reproduce its first round. A fit on a world stepped from the start would reproduce that world's, but a played
    /// game's poll still moves with play (its formation, its economy), so no factors fitted once reproduce every played game's. This world is the
    /// record's state on the eve, as a game started that day would hold it; which poll E1 means is Elias's to rule.</para>
    /// </summary>
    public static class PresidentialReferenceWorld
    {
        public sealed class Result
        {
            public Country Poland;
            /// <summary>The game's poll on the first vote's day - the live prediction, the government of record's record shifting it (the hook's own call).</summary>
            public Dictionary<string, double> PollOnFirstVote;
            /// <summary>The contest the game held - null where the hook held none.</summary>
            public PresidentialElection.Contest Contest;
            public DateTime FirstVoteDay, RunOffDay;
            public bool RunOffStepped;
            /// <summary>The government the world holds on the first vote's day - its prime minister's party and its cabinet, as the record gives them.</summary>
            public string GovernmentPmParty; public List<string> GovernmentCabinet;
            /// <summary>§772's second review: the government's record over its term as the hook's poll read it - so a check can hold that the poll carries one.</summary>
            public PerceivedPerformance.TermReading Term;
        }

        /// <summary>Builds the world on <paramref name="host"/> (the caller destroys it) and steps it through both rounds of 2025; restores the epoch.</summary>
        public static Result Hold(GameObject host)
        {
            var result = new Result();
            using (Simulation.SimulationManager.EpochScope())
            {
                TwoRoundElection.RoundOfRecord(CountryId.Poland, 2025, 1, out DateTime firstVote);
                result.FirstVoteDay = firstVote;
                Simulation.SimulationManager.SetEpoch(firstVote.AddDays(-1));   // before the world exists - SetEpoch's own contract, and a new game's order
                World world = WorldFactory.CreateDefault();
                result.Poland = world.GetCountry(CountryId.Poland);
                var sim = host.AddComponent<Simulation.SimulationManager>();
                sim.SetWorld(world);
                sim.PlayerCountryId = CountryId.Poland;
                sim.AdvanceDay();
                NationalElection.TryPredictShares(CountryId.Poland, out result.PollOnFirstVote, EconomicVote.RecordOverTerm(result.Poland, sim.CurrentDate, out result.Term), on: sim.CurrentDate);
                result.GovernmentPmParty = result.Poland.Government?.PmParty;
                result.GovernmentCabinet = result.Poland.Government?.Cabinet;
                result.Contest = result.Poland.PresidentialElections.Count == 1 ? result.Poland.PresidentialElections[0] : null;
                if (result.Contest != null && result.Contest.RunOffPending())
                {
                    result.RunOffDay = result.Contest.RunOffOn;
                    for (int day = 0; day < 31 && sim.CurrentDate < result.RunOffDay; day++) { sim.AdvanceDay(); }
                    result.RunOffStepped = sim.CurrentDate == result.RunOffDay;
                }
            }
            return result;
        }

        /// <summary>E1's fit: each candidate of record a roster party backs, the factor that makes the party's poll on the first vote's day carry the
        /// candidate's share of record - share of record ÷ the party's poll. NaN where the party has no share in the poll.</summary>
        public static double FittedFactor(Result world, PresidencyOfRecord.CandidateOfRecord candidate)
        {
            if (candidate.BackingParty == null || world.PollOnFirstVote == null || !world.PollOnFirstVote.TryGetValue(candidate.BackingParty, out double poll) || poll <= 0.0) { return double.NaN; }
            return PresidencyOfRecord.ShareOfRecord(CountryId.Poland, world.FirstVoteDay.Year, candidate.Surname) / poll;
        }
    }
}
