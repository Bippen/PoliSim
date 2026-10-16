using System;
using System.Collections.Generic;
using PoliSim.Data;

namespace PoliSim.Elections
{
    /// <summary>
    /// PS-6 US-7 (`docs/specs/USA_STAGE_PLAN.md`, "The oath: a presidency, not a formation"): THE US PRESIDENTIAL CONTEST, AND THE OATH THAT ENDS IT.
    /// <list type="bullet">
    /// <item><description><b>The record of a contest.</b> A presidential election the game holds is kept with the country (<see cref="Country.UsPresidentialElections"/>),
    /// so a save carries it - the record US-8's count is meant to fill: the polling day and its statute, the electors by candidate, the electors' meeting, the
    /// count before Congress, the House's contingent choice (R-US3), the winner and the oath's day. It is built here so that the pair's intent - one save step for
    /// US-7 and US-8 (format 39) - can hold for the 2024 count; it carries NO PER-STATE RESULT, which a second in-game count will need for its swing (R-US14 (a)
    /// from the previous election's states, R-US11 (a)), so that count may owe a step of its own. Until US-8's count nothing in play holds a contest; a
    /// diagnostic plants them (the plan: "Tested on planted outcomes; US-8 supplies the real one").</description></item>
    /// <item><description><b>The oath.</b> The winner takes office at noon on the 20th of January after the polling day - the Twentieth Amendment, Section 1
    /// (`ElectionsData/usa/records_by_date.md` §5, [EX-AM]): "The terms of the President and Vice President shall end at noon on the 20th day of January
    /// ... and the terms of their successors shall then begin" - through US-2's dated transition (`SimulationManager.SeatTheRecordOnItsDate`), as a
    /// government of the presidency kind whose cabinet is the president's party (<see cref="GovernmentRecord.OfPresidency"/>). The outgoing president governs
    /// until then. The oath's day is the day the world arrives on it ("noon" is a date - US-2's premise, kept).</description></item>
    /// <item><description><b>The term.</b> Art. II §1 ([CONST]): "He shall hold his Office during the Term of four Years" - no investiture and no confidence vote
    /// (<see cref="ConfidenceProcedure.Rules.Presidential"/>); removal on impeachment (Art. II §4) is not modelled, stated.</description></item>
    /// <item><description><b>The record yields.</b> Once the game holds a presidential election of its own the record seats no president: R-US1 (a) seats the
    /// president of record "until the game holds the presidential election" (<see cref="WorldClock.RecordSeatsExecutive(Country, DateTime)"/>). Between the
    /// polling day and the oath the sitting president stays; from the oath the game's winner holds the office until a later contest's winner takes it - past
    /// the term, as the record's last president does past the term of record.</description></item>
    /// <item><description><b>Player-agnostic</b> (the plan's §8, R-US5): every answer here is a pure function of the country's state, whether or not the USA is
    /// the player's; the day loop asks it for the player's country only, as it seats the record (PS-8 owns the rest).</description></item>
    /// </list>
    /// </summary>
    public static class UsPresidentialElection
    {
        /// <summary>How a government the game's oath installs is labelled - the one test that tells it from a government of record ("of record") or a formation's.</summary>
        public const string ElectedOutcome = "elected";

        /// <summary>One line of a count: a candidate, the candidate's party by key, the votes counted (electors in the electors' count, states in the House's
        /// contingent election) and the candidate's national share of the popular vote (%, NaN where the count carries none).</summary>
        public sealed class Tally
        {
            public string Candidate;
            public string Party;
            public int Votes;
            public double Share = double.NaN;
        }

        /// <summary>One presidential election the game held - kept with the country (<see cref="Country.UsPresidentialElections"/>), so a save carries it (format 39).
        /// US-7 reads the winner and the oath's day; the rest is US-8's count, recorded here so it rides the same format.</summary>
        public sealed class Contest
        {
            /// <summary>The polling day - 3 U.S.C. §21(1), as US-8 reads it.</summary>
            public DateTime PollingDay;
            /// <summary>The polling day's statute, in the count's words.</summary>
            public string DayBasis;
            /// <summary>The day the electors meet and vote in their states (3 U.S.C. §7).</summary>
            public DateTime ElectorsMeet;
            /// <summary>The day Congress counts the electoral votes in joint session (3 U.S.C. §15).</summary>
            public DateTime CountedOn;
            /// <summary>The oath's day - the 20th of January after the polling day (<see cref="OathDayAfter"/>, the Twentieth Amendment §1).</summary>
            public DateTime TakesOffice;
            /// <summary>The electors appointed, all of them - the majority is reckoned on it (<see cref="MajorityOfElectors"/>).</summary>
            public int ElectorsAppointed;
            /// <summary>The electors each candidate won, largest first.</summary>
            public List<Tally> Electors = new List<Tally>();
            /// <summary>R-US3 (a), RULED (F8): no candidate won a majority of the electors appointed, and the House chose by states under the Twelfth Amendment.</summary>
            public bool Contingent;
            /// <summary>R-US3 (a): the states' votes in the House's choice, one per state (a tied delegation casts none - DECLARED by the ruling); empty where no
            /// contingent election was held.</summary>
            public List<Tally> Delegations = new List<Tally>();
            /// <summary>The president elected, by name, and the party by key; null while undecided (a contingent election not yet held).</summary>
            public string Winner;
            public string WinnerParty;
            /// <summary>The count's verdict in a line.</summary>
            public string Line;

            public bool Decided() => Winner != null;

            /// <summary>A majority of the whole number of electors appointed - the Twelfth Amendment's test.</summary>
            public int MajorityOfElectors() => ElectorsAppointed / 2 + 1;

            /// <summary>The electors a candidate's party won, 0 where none.</summary>
            public int ElectorsOf(string party)
            {
                foreach (Tally t in Electors) { if (t.Party == party) { return t.Votes; } }
                return 0;
            }
        }

        /// <summary>The oath's day for a contest held on <paramref name="pollingDay"/>: the 20th of January of the following year - the Twentieth Amendment §1 applied to
        /// a term that ends four years after its oath (Art. II §1), as the record's own rows read it (usa records §5: Biden's term "ends at noon 20 January by the 20th
        /// Amendment §1", DERIVED; Trump's from "January 20, 2025 at Noon - Inauguration Day" [EX-DATES]). A November polling day is the statute's (US-8).</summary>
        public static DateTime OathDayAfter(DateTime pollingDay) => new DateTime(pollingDay.Year + 1, 1, 20);

        /// <summary>The government label a president's oath gives - "Kamala D. Harris (DEM)", the record's own form for a president (`WorldClock.Governments`).</summary>
        public static string ExecutiveOf(Contest contest) => contest.Winner + " (" + contest.WinnerParty + ")";

        /// <summary>Whether the game has held a presidential election in this country on or before <paramref name="date"/> - decided or not. From then the record
        /// seats no president (R-US1 (a); <see cref="WorldClock.RecordSeatsExecutive(Country, DateTime)"/>).</summary>
        public static bool Held(Country country, DateTime date)
        {
            if (country?.UsPresidentialElections == null) { return false; }
            foreach (Contest c in country.UsPresidentialElections) { if (c.PollingDay.Date <= date.Date) { return true; } }
            return false;
        }

        /// <summary>The contest whose winner holds the office on <paramref name="date"/>: the latest decided contest whose oath's day is on or before it; false where
        /// no president the game elected has taken office.</summary>
        public static bool TryInOffice(Country country, DateTime date, out Contest contest)
        {
            contest = null;
            if (country?.UsPresidentialElections == null) { return false; }
            foreach (Contest c in country.UsPresidentialElections)
            {
                if (c.Decided() && c.TakesOffice.Date <= date.Date && (contest == null || c.TakesOffice.Date > contest.TakesOffice.Date)) { contest = c; }
            }
            return contest != null;
        }

        /// <summary>The president-elect on <paramref name="date"/>: a decided contest whose oath is still ahead (the earliest, were there two); false where none waits.</summary>
        public static bool TryPresidentElect(Country country, DateTime date, out Contest contest)
        {
            contest = null;
            if (country?.UsPresidentialElections == null) { return false; }
            foreach (Contest c in country.UsPresidentialElections)
            {
                if (c.Decided() && c.TakesOffice.Date > date.Date && (contest == null || c.TakesOffice.Date < contest.TakesOffice.Date)) { contest = c; }
            }
            return contest != null;
        }

        /// <summary>Whether <paramref name="government"/> is the one <paramref name="contest"/>'s oath installed: a presidency the game elected, its president the
        /// winner, formed on or after the oath's day - so a second term's oath forms the term's own government (the arrival budget window reset, as every oath).</summary>
        public static bool Governs(GovernmentRecord government, Contest contest) =>
            government != null && contest != null && government.Kind == WorldClock.ExecutiveKind.Presidency && government.Outcome == ElectedOutcome
            && government.Executive == ExecutiveOf(contest) && government.FormedOn.Date >= contest.TakesOffice.Date;

        /// <summary>The government <paramref name="contest"/>'s winner forms at the oath, on <paramref name="on"/> - a presidency, its cabinet the president's party
        /// (<see cref="GovernmentRecord.OfPresidency"/>), its basis the game's own count. The basis names the day the government took office, <paramref name="on"/>
        /// (its <c>FormedOn</c>) - the oath's day where the world arrives on it, a later day where the oath is applied after it (a world opened past the oath).</summary>
        public static GovernmentRecord GovernmentOf(Contest contest, DateTime on)
        {
            int electors = contest.ElectorsOf(contest.WinnerParty);
            string basis = string.Format(System.Globalization.CultureInfo.InvariantCulture,
                "the game's own election of {0:yyyy-MM-dd}: {1}, {2} of {3} electors{4}; took office on {5:yyyy-MM-dd} - the oath is due at noon on the 20th of January, the Twentieth Amendment §1",
                contest.PollingDay, ExecutiveOf(contest), electors, contest.ElectorsAppointed,
                contest.Contingent ? ", chosen by the House under the Twelfth Amendment (R-US3 (a))" : string.Empty, on);
            return GovernmentRecord.OfPresidency(ExecutiveOf(contest), contest.WinnerParty, on, ElectedOutcome, basis);
        }
    }
}
