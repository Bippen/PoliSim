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
    /// <para>RULED (Elias's ruling F4: "the factors stay fitted on the world that follows history, so a played game's first round moves with its play"):
    /// this world - the record's state on the eve, as a game started that day would hold it - is the poll E1 means. A game PLAYED from Poland's 2023
    /// start reaches 18 May 2025 on another basis - its prediction reads the history of the chamber seated at its own epoch (the standing history gap,
    /// §767), and its government is the one its own formation seated - so its first round is its own, not the PKW's, by design.</para>
    /// <para>PS-6 US-6: <b>GENERALISED BY COUNTRY</b> (the s772 review's fix, carried: never a start world with its clock moved). The vote of record is
    /// the country's (<see cref="VoteOfRecord"/>): Poland's first round of 18 May 2025 as before - its path statement for statement what it was, so
    /// `PresidentialElectionLiveDiagnostic` and `PresidentialVoteBacktest` print what they printed - and the USA's general election of 5 Nov 2024, the
    /// epoch set to its eve before the world is created and one day stepped. Its world is the poll on the day of the vote and the record the poll
    /// carries; `UsPresidentialFactorCheck` fits and holds E1's US factors on it. PS-6 US-8: the step now holds the USA's own count on that day, on the
    /// same poll (the day loop's hook makes the same call), kept with the country (`Country.UsPresidentialElections`, not <see cref="Result.Contest"/>) -
    /// E1's acceptance for the USA, `UsPresidentialElectionDiagnostic`'s. The same
    /// world is the one R-US18's (c), were it ruled, names as E1's point ("US-6's reference world on the eve of 5 Nov 2024"). A game PLAYED from the
    /// USA's start (12 Mar 2024) reaches 5 Nov on its own economy and the one seed vintage (`WorldClock.SeedVintageLine`), so its count is its own.</para>
    /// </summary>
    public static class PresidentialReferenceWorld
    {
        public sealed class Result
        {
            /// <summary>PS-6 US-6: the country the world was held for, and its country object in the world.</summary>
            public CountryId Id;
            public Country Country;
            /// <summary>§772: Poland's name for <see cref="Country"/> - null on any other country's world.</summary>
            public Country Poland => Id == CountryId.Poland ? Country : null;
            /// <summary>The game's poll on the first vote's day - the live prediction, the government of record's record shifting it (the hook's own call).</summary>
            public Dictionary<string, double> PollOnFirstVote;
            /// <summary>PS-6 US-6: the record's shift that poll carries (`EconomicVote.RecordOverTerm`), by party key.</summary>
            public Dictionary<string, double> Shift;
            /// <summary>The two-round contest the game held - null where the hook held none (always for the USA, whose count rides its own list, US-8).</summary>
            public PresidentialElection.Contest Contest;
            public DateTime FirstVoteDay, RunOffDay;
            /// <summary>PS-6 US-6: the day the world stood on after its one step - the vote's own day.</summary>
            public DateTime Day;
            public bool RunOffStepped;
            /// <summary>The government the world holds on the first vote's day - its prime minister's party and its cabinet, as the record gives them.</summary>
            public string GovernmentPmParty; public List<string> GovernmentCabinet;
            /// <summary>PS-6 US-6: that government's kind (a presidency for the USA) and the chamber the world seats that day, by party key.</summary>
            public WorldClock.ExecutiveKind GovernmentKind;
            public Dictionary<string, int> Seats;
            /// <summary>§772's second review: the government's record over its term as the hook's poll read it - so a check can hold that the poll carries one.</summary>
            public PerceivedPerformance.TermReading Term;
            /// <summary>PS-6 US-6: where the world was stepped on to a later day (<c>thenTo</c>), that day, the chamber it then seats, and the history and
            /// electorate the poll then reads - so a check can hold that a chamber the record seats later moves the seats and not the history.</summary>
            public DateTime ThenDay;
            public Dictionary<string, int> SeatsThen;
            public double[] HistoryThenLatest, HistoryThenPrevious;
            public VoteModel.Electorate ElectorateThen;
            /// <summary>PS-6 US-6: the live prediction on that later day without the record's shift (`NationalElection.TryPredictShares`, the call the
            /// game makes) - the history it reads shows in its split, which the check holds against both houses' histories.</summary>
            public Dictionary<string, double> PlainPollThen;
            /// <summary>PS-6 US-6: what the later steps threw, if they did - the vote-day reading above stands either way.</summary>
            public string ThenError;
        }

        /// <summary>Builds Poland's world on <paramref name="host"/> (the caller destroys it) and steps it through both rounds of 2025; restores the epoch.</summary>
        public static Result Hold(GameObject host) => Hold(host, CountryId.Poland, null);

        /// <summary>PS-6 US-6: builds <paramref name="id"/>'s world on the eve of its vote of record on <paramref name="host"/> (the caller destroys it),
        /// steps it to the vote's day and - where the hook held a first round with a run-off to come - on to the run-off - and, given <paramref name="thenTo"/>,
        /// on by the same loop to that day, reading what the world then seats and what its poll then reads; restores the epoch. The caller keeps the
        /// random streams and the energy state where its bar needs them kept (the US check puts both back, as US-5's eve worlds do).</summary>
        public static Result Hold(GameObject host, CountryId id, DateTime? thenTo)
        {
            var result = new Result { Id = id };
            // GATE-18 (Elias's ruling of 2026-10-23, the noise always on in play): E1's reference world is a measurement at sigma 0 by design - the
            // count its factors are fitted to reads the poll and nothing drawn - so it is held inside the noise-at-zero scope
            using (ParliamentaryCount.NoiseAtZero())
            using (Simulation.SimulationManager.EpochScope())
            {
                DateTime firstVote = VoteOfRecord(id);
                result.FirstVoteDay = firstVote;
                Simulation.SimulationManager.SetEpoch(firstVote.AddDays(-1));   // before the world exists - SetEpoch's own contract, and a new game's order
                World world = WorldFactory.CreateDefault();
                result.Country = world.GetCountry(id);
                var sim = host.AddComponent<Simulation.SimulationManager>();
                sim.SetWorld(world);
                sim.PlayerCountryId = id;
                sim.AdvanceDay();
                result.Day = sim.CurrentDate;
                result.Shift = EconomicVote.RecordOverTerm(result.Country, sim.CurrentDate, out result.Term);
                NationalElection.TryPredictShares(id, out result.PollOnFirstVote, result.Shift, on: sim.CurrentDate);
                result.GovernmentPmParty = result.Country.Government?.PmParty;
                result.GovernmentCabinet = result.Country.Government?.Cabinet;
                result.GovernmentKind = result.Country.Government?.Kind ?? default;
                result.Seats = result.Country.ParliamentSeats != null ? new Dictionary<string, int>(result.Country.ParliamentSeats) : null;
                result.Contest = result.Country.PresidentialElections.Count == 1 ? result.Country.PresidentialElections[0] : null;
                if (result.Contest != null && result.Contest.RunOffPending())
                {
                    result.RunOffDay = result.Contest.RunOffOn;
                    for (int day = 0; day < 31 && sim.CurrentDate < result.RunOffDay; day++) { sim.AdvanceDay(); }
                    result.RunOffStepped = sim.CurrentDate == result.RunOffDay;
                }
                if (thenTo.HasValue)
                {
                    try
                    {
                        for (int day = 0; day < 400 && sim.CurrentDate < thenTo.Value; day++) { sim.AdvanceDay(); }
                        result.ThenDay = sim.CurrentDate;
                        result.SeatsThen = result.Country.ParliamentSeats != null ? new Dictionary<string, int>(result.Country.ParliamentSeats) : null;
                        PartySystems.TryHistory(id, out result.HistoryThenLatest, out result.HistoryThenPrevious);   // the world still lives: the epoch is its own
                        PartySystems.TryElectorate(id, out result.ElectorateThen, out double _);
                        NationalElection.TryPredictShares(id, out result.PlainPollThen, null, on: sim.CurrentDate);
                    }
                    catch (Exception ex) { result.ThenError = ex.ToString(); }
                }
            }
            return result;
        }

        /// <summary>
        /// PS-6 US-6: the day of the presidential vote of record whose result E1's factors are fitted to - Poland's 2025 first round (the PKW's,
        /// `TwoRoundElection.RoundOfRecord`); the USA's general election of 2024, the day the House election of record that seated the 119th was held
        /// (the chamber's row in `WorldClock.Chambers`; the presidential electors are chosen the same day). Throws for a country E1 has no fit in.
        /// </summary>
        public static DateTime VoteOfRecord(CountryId id)
        {
            switch (id)
            {
                case CountryId.Poland:
                    TwoRoundElection.RoundOfRecord(CountryId.Poland, 2025, 1, out DateTime firstVote);
                    return firstVote;
                case CountryId.USA:
                    DateTime day = WorldClock.ElectionDayOf(CountryId.USA, ElectionVintage.Usa2024);
                    if (day == DateTime.MinValue) { throw new InvalidOperationException("the USA's 2024 election of record has no day in WorldClock.Chambers"); }
                    return day;
                default:
                    throw new ArgumentException($"E1 fits no presidential vote of record for {id}");
            }
        }

        /// <summary>E1's fit: each candidate of record a roster party backs, the factor that makes the party's poll on the first vote's day carry the
        /// candidate's share of record - share of record ÷ the party's poll. NaN where the party has no share in the poll. PS-6 US-6: the share of
        /// record is the world's country's, for the year of its vote (the USA's: the FEC's national total).</summary>
        public static double FittedFactor(Result world, PresidencyOfRecord.CandidateOfRecord candidate)
        {
            if (candidate.BackingParty == null || world.PollOnFirstVote == null || !world.PollOnFirstVote.TryGetValue(candidate.BackingParty, out double poll) || poll <= 0.0) { return double.NaN; }
            return PresidencyOfRecord.ShareOfRecord(world.Id, world.FirstVoteDay.Year, candidate.Surname) / poll;
        }
    }
}
