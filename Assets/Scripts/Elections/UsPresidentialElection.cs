using System;
using System.Collections.Generic;
using System.Globalization;
using PoliSim.Data;
using PoliSim.Simulation;

namespace PoliSim.Elections
{
    /// <summary>
    /// PS-6 US-7 (`docs/specs/USA_STAGE_PLAN.md`, "The oath: a presidency, not a formation") and US-8 ("The 2024 presidential election held"): THE US
    /// PRESIDENTIAL CONTEST - THE COUNT, AND THE OATH THAT ENDS IT.
    /// <list type="bullet">
    /// <item><description><b>The record of a contest.</b> A presidential election the game holds is kept with the country (<see cref="Country.UsPresidentialElections"/>),
    /// so a save carries it - the polling day and its statute, the electors by candidate, the electors' meeting, the count before Congress, the House's
    /// contingent choice (R-US3), the winner and the oath's day. US-7 built it so that the pair's intent - one save step for US-7 and US-8 (format 39) - would
    /// hold for the 2024 count, and it does: the contest stores no per-state result and needs none, because the states are a pure function of the national
    /// shares it stores and of the catalog (<see cref="StatesOf"/> re-derives them exactly as the count made them), and before US-26 a later count swings from
    /// the record's states, not the game's (R-US11 (a), RULED, is US-26's to build - the standing W-G1 gap, stated).</description></item>
    /// <item><description><b>The count</b> (US-8; <see cref="Count"/>, a pure function of its inputs, callable for any world - R-US5 (a): PS-8 then needs only its
    /// gate). On the polling day of 3 U.S.C. §21(1) (<see cref="PresidentialDay"/>) the game's poll × E1's factors (US-6, <see cref="PresidencyOfRecord.CandidatesOf"/>)
    /// gives the national vote; the states follow from the previous presidential election's by §689's normalised uniform swing (R-US14 (a), RULED - G2) and
    /// the electors by the statutes in force (<see cref="ElectoralCollege.FromNationalShares(int, int, double[], int, IReadOnlyList{ElectoralCollege.Standing}, out ElectoralCollege.StateCall[], out double)"/>); with no one at a majority of the electors appointed the House
    /// chooses by states on the count's day under the Twelfth Amendment (R-US3 (a), RULED - F8; <see cref="HoldHouseChoice"/>). The verdict lands at once: no
    /// night until D-US is built.</description></item>
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
    /// the term, as the record's last president does past the term of record. Congress stays the record's, by date (R-US1 (a); US-16 elects the House).</description></item>
    /// <item><description><b>Player-agnostic</b> (the plan's §8, R-US5): every answer here is a pure function of the country's state, whether or not the USA is
    /// the player's; the day loop asks it for the player's country only, as it seats the record (PS-8 owns the rest).</description></item>
    /// </list>
    /// </summary>
    public static class UsPresidentialElection
    {
        /// <summary>How a government the game's oath installs is labelled - the one test that tells it from a government of record ("of record") or a formation's.</summary>
        public const string ElectedOutcome = "elected";

        /// <summary>The polling day's statute in the count's words - 3 U.S.C. §21(1) as the Electoral Count Reform Act of 2022 wrote it (GPO's 2023 edition,
        /// `ElectionsData/usa/raw/apportionment/govinfo_uscode_3_21.html`, the register `ElectionsData/usa/president_returns.md`).</summary>
        public const string DayBasis = "3 U.S.C. §21(1): \"election day\" means \"the Tuesday next after the first Monday in November, in every fourth year succeeding every election of a President and Vice President\"";

        /// <summary>The name the count gives every candidate but the two nominees, together - one tally, as the states pool them (US-4's DECLARED form).</summary>
        public const string OtherCandidates = "every other candidate";

        /// <summary>PS-6 US-16 - R-US22 RULED (I6): how a government an undecided contest leaves at the oath is labelled - a president ACTING, never elected
        /// (<see cref="ElectedOutcome"/>) nor of record.</summary>
        public const string ActingOutcome = "acting";

        /// <summary>R-US22's two capacities: the Vice President the Senate chose, acting as President until the House chooses (the Twentieth Amendment §3: "the
        /// Vice President elect shall act as President until a President shall have qualified"); the Speaker, acting where the Senate could not choose either
        /// (3 U.S.C. §19(a)(1)).</summary>
        public const string ActsAsVicePresident = "the Vice President chosen by the Senate (the Twelfth Amendment), acting as President (the Twentieth Amendment §3) to the next oath - the House votes once, on the count's day";
        public const string ActsAsSpeaker = "the Speaker of the House, acting as President where neither the House nor the Senate could choose (3 U.S.C. §19(a)(1))";

        /// <summary>One line of a count: a candidate, the candidate's party by key, the votes counted (electors in the electors' count, states in the House's
        /// contingent election) and the candidate's national share of the popular vote (%, NaN where the count carries none).</summary>
        public sealed class Tally
        {
            public string Candidate;
            public string Party;
            public int Votes;
            public double Share = double.NaN;
            /// <summary>PS-6 US-33: the player's own nominee stood on this line in place of the nominee of record (`OwnNominees`) - the person is the line's own name
            /// (`UsTermLimit.PersonOf`). Additive under ruling A: false in a save from before, which held no own nominee.</summary>
            public bool OwnNominee;
        }

        /// <summary>One presidential election the game held - kept with the country (<see cref="Country.UsPresidentialElections"/>), so a save carries it (format 39).
        /// US-7 reads the winner and the oath's day; the rest is US-8's count, recorded here so it rides the same format.</summary>
        public sealed class Contest
        {
            /// <summary>The polling day - 3 U.S.C. §21(1) (<see cref="PresidentialDay"/>).</summary>
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
            /// <summary>The electors each candidate won, largest first. US-8's count lists the two nominees and <see cref="OtherCandidates"/> together (no
            /// electors - the states pool them), each with the national share the count read, so the states can be re-derived from the contest (<see cref="StatesOf"/>).</summary>
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
            /// <summary>PS-6 US-8c (Elias's ruling J4): the seed an exact state or district tie is drawn on (<see cref="ElectoralCollege.TieBreak"/>) - the
            /// world's master seed with the year folded in (<see cref="TieSeedOf"/>), kept so that <see cref="StatesOf"/> re-derives the count's own calls. 0 in a
            /// planted contest. Saved (format 49).</summary>
            public int TieSeed;
            /// <summary>PS-6 US-16 - R-US22 RULED (Elias's ruling I6, `COMPLETED.md` §856), the Twentieth Amendment's order: where the House could not choose, the
            /// Senate's choice of the Vice President (the Twelfth Amendment - "a majority of the whole number shall be necessary to a choice"), one tally per
            /// running mate with the Senate's seats on the ticket's side; empty where no Senate choice was held.</summary>
            public List<Tally> SenateChoice = new List<Tally>();
            /// <summary>R-US22: who acts as President from the oath while the contest is undecided - the person, the party key, and in what capacity
            /// (<see cref="ActsAsVicePresident"/> or <see cref="ActsAsSpeaker"/>); null while no one acts (the House chose, or the oath has not come).</summary>
            public string Acting;
            public string ActingParty;
            public string ActingAs;

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

        /// <summary>PS-6 US-8 - 3 U.S.C. §21(1) (<see cref="DayBasis"/>): the presidential polling day of <paramref name="year"/>, the Tuesday next after the first
        /// Monday in November. Which years hold one - every fourth from the election of record - is `WorldClock.TryNextPollingDay`'s.</summary>
        public static DateTime PresidentialDay(int year)
        {
            var first = new DateTime(year, 11, 1);
            int toMonday = ((int)DayOfWeek.Monday - (int)first.DayOfWeek + 7) % 7;
            return first.AddDays(toMonday + 1);
        }

        /// <summary>PS-6 US-8 - 3 U.S.C. §7 (`govinfo_uscode_3_7.html`): the electors "shall meet and give their votes on the first Tuesday after the second Wednesday
        /// in December next following their appointment" - their appointment the polling day (3 U.S.C. §1).</summary>
        public static DateTime ElectorsMeetAfter(DateTime pollingDay)
        {
            var first = new DateTime(pollingDay.Year, 12, 1);
            int toWednesday = ((int)DayOfWeek.Wednesday - (int)first.DayOfWeek + 7) % 7;
            return first.AddDays(toWednesday + 7 + 6);   // the second Wednesday, then the Tuesday after it
        }

        /// <summary>PS-6 US-8 - 3 U.S.C. §15(a) (`govinfo_uscode_3_15.html`): "Congress shall be in session on the sixth day of January succeeding every meeting of
        /// the electors" - the count before Congress, and the day the House chooses where no one has a majority (the Twelfth Amendment's "immediately").</summary>
        public static DateTime CountedOnAfter(DateTime pollingDay) => new DateTime(pollingDay.Year + 1, 1, 6);

        /// <summary>PS-6 US-8: the countries whose president is chosen by electors - the USA alone.</summary>
        public static bool ElectsIn(CountryId id) => id == CountryId.USA;

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

        /// <summary>The contest whose winner holds the office on <paramref name="date"/>: the latest contest whose oath's day is on or before it, where it is
        /// decided; false where no president the game elected has taken office - and, since PS-6 US-16 (R-US22 RULED, I6: "never the sitting president past noon
        /// on 20 January", Am. XX §1), false where that latest contest is undecided: an earlier winner's term has ended, and someone acts
        /// (<see cref="TryActingOn"/>).</summary>
        public static bool TryInOffice(Country country, DateTime date, out Contest contest)
        {
            contest = null;
            if (country?.UsPresidentialElections == null) { return false; }
            foreach (Contest c in country.UsPresidentialElections)
            {
                if (c.TakesOffice.Date <= date.Date && (contest == null || c.TakesOffice.Date > contest.TakesOffice.Date)) { contest = c; }
            }
            if (contest != null && !contest.Decided()) { contest = null; }
            return contest != null;
        }

        /// <summary>PS-6 US-16 - R-US22 RULED (I6): the contest an acting president serves for on <paramref name="date"/> - the latest contest whose oath's day has
        /// come, still undecided; false where none.</summary>
        public static bool TryActingOn(Country country, DateTime date, out Contest contest)
        {
            contest = null;
            if (country?.UsPresidentialElections == null) { return false; }
            foreach (Contest c in country.UsPresidentialElections)
            {
                if (c.TakesOffice.Date <= date.Date && (contest == null || c.TakesOffice.Date > contest.TakesOffice.Date)) { contest = c; }
            }
            if (contest != null && contest.Decided()) { contest = null; }
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

        /// <summary>PS-6 US-8: the latest contest held on or before <paramref name="date"/>, decided or not; false where the game has held none.</summary>
        public static bool TryLatest(Country country, DateTime date, out Contest contest)
        {
            contest = null;
            if (country?.UsPresidentialElections == null) { return false; }
            foreach (Contest c in country.UsPresidentialElections) { if (c.PollingDay.Date <= date.Date && (contest == null || c.PollingDay > contest.PollingDay)) { contest = c; } }
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
            string basis = string.Format(CultureInfo.InvariantCulture,
                "the game's own election of {0:yyyy-MM-dd}: {1}, {2} of {3} electors{4}; took office on {5:yyyy-MM-dd} - the oath is due at noon on the 20th of January, the Twentieth Amendment §1",
                contest.PollingDay, ExecutiveOf(contest), electors, contest.ElectorsAppointed,
                contest.Contingent ? ", chosen by the House under the Twelfth Amendment (R-US3 (a))" : string.Empty, on);
            return GovernmentRecord.OfPresidency(ExecutiveOf(contest), contest.WinnerParty, on, ElectedOutcome, basis);
        }

        /// <summary>PS-6 US-16 - R-US22: whether <paramref name="government"/> is the acting presidency an undecided <paramref name="contest"/> left at the oath.</summary>
        public static bool ActingGoverns(GovernmentRecord government, Contest contest) =>
            government != null && contest?.Acting != null && government.Kind == WorldClock.ExecutiveKind.Presidency && government.Outcome == ActingOutcome
            && government.Executive == contest.Acting + " (" + contest.ActingParty + ")" && government.FormedOn.Date >= contest.TakesOffice.Date;

        /// <summary>
        /// PS-6 US-16 - R-US22 RULED (Elias's ruling I6, `COMPLETED.md` §856): THE TWENTIETH AMENDMENT'S ORDER, at the oath of a contest still undecided on
        /// <paramref name="on"/>: "never the sitting president past noon on 20 January" (Am. XX §1). The Vice President the Senate chose acts as President
        /// (Am. XX §3); where the Senate did not choose either, the Speaker acts (3 U.S.C. §19(a)(1)). The Senate's choice is the count day's
        /// (<see cref="HoldHouseChoice"/>); a contest whose count day passed without one (a world opened after it) holds it here, on the Senate seated today -
        /// DECLARED. The Speaker is of the House's majority party - the House elects its Speaker (Art. I §2 cl. 5), and the model reads the majority's choice
        /// (DECLARED; the person is not named: "the Speaker of the House"); a House with no majority party takes its larger side, and an equal one a seeded
        /// draw (US-8c's, never in one party's favour). DECLARED: the House votes once, on the count's day, with the delegations it then holds, so the acting
        /// president serves until a later contest's winner takes office. Writes the contest's <c>Acting</c> fields; returns the government.
        /// </summary>
        public static GovernmentRecord ActingGovernmentOf(Country country, Contest contest, DateTime on)
        {
            if (contest.Acting == null)
            {
                if (contest.SenateChoice.Count == 0) { ChooseVicePresident(country, contest, on); }
                // the chosen Vice President acts; one with no name (a party off the roster) cannot be seated as a person, so the Speaker acts - and either way
                // Acting is set here once, so the oath's government is seated once (ActingGoverns), never again each day
                Tally vice = contest.SenateChoice.Find(t => t.Votes >= UsSenate.Members / 2 + 1);
                if (vice != null && !string.IsNullOrEmpty(vice.Candidate) && !string.IsNullOrEmpty(vice.Party))
                {
                    contest.Acting = vice.Candidate; contest.ActingParty = vice.Party; contest.ActingAs = ActsAsVicePresident;
                }
                else
                {
                    string speaker = SpeakerParty(country, contest);
                    contest.Acting = "the Speaker of the House"; contest.ActingParty = speaker; contest.ActingAs = ActsAsSpeaker;
                }
                contest.Line += "; " + string.Format(CultureInfo.InvariantCulture, "no one elected by {0:yyyy-MM-dd}: {1} ({2}) acts - {3}", contest.TakesOffice, contest.Acting, contest.ActingParty, contest.ActingAs);
            }
            string basis = string.Format(CultureInfo.InvariantCulture, "the game's own election of {0:yyyy-MM-dd} left no president elected at the oath: {1} ({2}), {3}; took office on {4:yyyy-MM-dd} - never the sitting president past noon on the 20th of January (the Twentieth Amendment §1)",
                contest.PollingDay, contest.Acting, contest.ActingParty, contest.ActingAs, on);
            return GovernmentRecord.OfPresidency(contest.Acting + " (" + contest.ActingParty + ")", contest.ActingParty, on, ActingOutcome, basis);
        }

        /// <summary>R-US22: the Speaker's party - the party holding a majority of the House seated in <paramref name="country"/>; else the larger side, an equal
        /// one by a seeded draw (DECLARED).</summary>
        private static string SpeakerParty(Country country, Contest contest)
        {
            int rep = 0, dem = 0, total = 0;
            if (country?.ParliamentSeats != null)
            {
                foreach (KeyValuePair<string, int> kv in country.ParliamentSeats) { total += kv.Value; if (kv.Key == "REP") { rep = kv.Value; } else if (kv.Key == "DEM") { dem = kv.Value; } }
            }
            if (rep != dem) { return rep > dem ? "REP" : "DEM"; }
            return ElectoralCollege.TieBreak(contest.TieSeed, contest.PollingDay.Year, "SPEAKER") == ElectoralCollege.Republican ? "REP" : "DEM";
        }

        /// <summary>
        /// R-US22 (I6), the Twelfth Amendment's second choice: "if no person have a majority, then from the two highest numbers on the list, the Senate shall
        /// choose the Vice President; a quorum for the purpose shall consist of two-thirds of the whole number of Senators, and a majority of the whole number
        /// shall be necessary to a choice." Each running mate of a ticket that won electors (<see cref="UsTermLimit.RunningMateOf"/>) takes the seats of its
        /// side in the Senate seated on <paramref name="on"/> (senators vote their party's ticket - the House's reading, DECLARED); a majority of the whole
        /// number - the Senate's members, not those present (US-15's <see cref="UsSenate.Members"/>) - elects. The Vice President of the old Senate does not
        /// vote: no equal division decides a choice that needs a majority of the whole number (DECLARED). The quorum is met by every Senate the model seats.
        /// Appends its line to the contest's.
        /// </summary>
        public static string ChooseVicePresident(Country country, Contest contest, DateTime on)
        {
            contest.SenateChoice.Clear();
            IReadOnlyDictionary<string, int> senate = country?.SecondChamberSeats;
            int members = UsSenate.Members, majority = members / 2 + 1;
            foreach (Tally t in contest.Electors)
            {
                if (t.Party == null || t.Votes <= 0) { continue; }
                int seats = senate != null && senate.TryGetValue(t.Party, out int n) ? n : 0;
                contest.SenateChoice.Add(new Tally { Candidate = UsTermLimit.RunningMateOf(country, contest, t.Party), Party = t.Party, Votes = seats });
            }
            contest.SenateChoice.Sort((a, b) => b.Votes != a.Votes ? b.Votes.CompareTo(a.Votes) : string.CompareOrdinal(a.Party, b.Party));   // stable: an equal division ordered by party key
            Tally chosen = contest.SenateChoice.Find(t => t.Votes >= majority);
            string line = string.Format(CultureInfo.InvariantCulture, "on {0:yyyy-MM-dd} the Senate chose the Vice President (R-US22) - {1}; a majority of the whole number, {2} senators, is {3}",
                on, string.Join(", ", contest.SenateChoice.ConvertAll(t => t.Candidate + " (" + t.Party + ") " + t.Votes.ToString(CultureInfo.InvariantCulture))), members, majority);
            line += chosen != null ? " - " + chosen.Candidate + " chosen" : " - no one has it: the Senate cannot choose either";
            contest.Line += "; " + line;
            return line;
        }

        // ---------------------------------------------------------------------------------------------------------------------------------------------
        // PS-6 US-8: THE COUNT. A pure function of the country, the day and the poll: nothing here writes a world, draws a random number or reads a clock -
        // the day loop (`SimulationManager`) hands it the player's country and the poll it reads that day, and adds what it returns.
        // ---------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// The field's national vote on <paramref name="pollingDay"/>, from the party <paramref name="poll"/> (the game's prediction that day, by party key) -
        /// E1's rule as US-6 built it: where the election is of record (2024, the term ending 20 January 2025), the field of record - each nominee NARA names at
        /// the party's poll × the nominee's [FITTED] factor, every other column at its national share of record as its base (in the states, standing only
        /// where on the ballot of record - US-6c, <see cref="Count"/>); otherwise each roster party's candidate at the party's poll × E1's future factor (<see cref="PresidentialElection.FutureFactor"/>),
        /// unnamed ("Republican Party's candidate (2028)") unless a nominee is given (US-28, below) and with no minor candidate (no field of record holds one). Normalised; each
        /// share in %. The nominees are named by NARA's name for the year's ticket (<see cref="PresidencyOfRecord.NameOfRecord"/>) - DECLARED: the count names
        /// the two as its certificates do, after the count, while US-6's naming by date (<see cref="PresidencyOfRecord.NomineeName"/> - R-US23 as ruled: even-handed, neither named until both conventions
        /// are dated) governs the screens before it - the count's naming unchanged by that ruling. False, with the reason, where a nominee's factor is not fitted (NaN until
        /// `Tools/us_factor_patch.pl` patches US-6's FIT lines in) or where no candidate has support.
        /// <para>PS-6 US-28: a later field names its nominees from <paramref name="nominees"/> (<see cref="UsTermLimit.NomineesFor"/> - the president within the
        /// Twenty-second Amendment's limit, a term-limited party's Vice President or the player's pick), each still at the future factor, so the shares are the same
        /// whoever stands; a party with no nominee given keeps the unnamed label. The field of record ignores them (R-US2 (a)) - but one:</para>
        /// <para>PS-6 US-33 (SP-6a; the own nominee in place of R-US2 (a)'s nominee of record for the player's party - the plan's "a leader at the head of the DEM or REP ticket"): the player's own nominee
        /// (<see cref="UsTermLimit.Nominee.Own"/>) replaces the nominee of record of its party - its own name, at the party's poll × E1's future factor in place of
        /// the nominee of record's fitted one (E1: "Future fields start at factor 1.0"), its tally marked (<see cref="Tally.OwnNominee"/>). The poll it reads is
        /// already the moved point's: the party's point is the nominee's in the vote model (`OwnNominees.TryPointOf`, read by `NationalElection.TryCompatibility`).
        /// Every other column of record stands as before; with no own nominee given every expression here is the one US-6 and US-8 built.</para>
        /// </summary>
        public static bool TryField(CountryId id, DateTime pollingDay, IReadOnlyDictionary<string, double> poll, out List<Tally> field, out string why) =>
            TryField(id, pollingDay, poll, null, out field, out why);

        /// <summary>The field, its later nominees named from <paramref name="nominees"/> (see the overload above); null names none.</summary>
        public static bool TryField(CountryId id, DateTime pollingDay, IReadOnlyDictionary<string, double> poll, IReadOnlyList<UsTermLimit.Nominee> nominees, out List<Tally> field, out string why)
        {
            field = new List<Tally>();
            why = null;
            if (poll == null) { why = "no poll"; return false; }
            var support = new List<(string Name, string Party, double Support, bool Own)>();
            IReadOnlyList<PresidencyOfRecord.CandidateOfRecord> ofRecord = PresidencyOfRecord.CandidatesOf(id, pollingDay.Year + 1);
            if (ofRecord.Count > 0)
            {
                foreach (PresidencyOfRecord.CandidateOfRecord c in ofRecord)
                {
                    string name = PresidencyOfRecord.NameOfRecord(id, pollingDay.Year, c.Surname);
                    double s;
                    UsTermLimit.Nominee own = c.BackingParty == null ? null : OwnNomineeOf(nominees, c.BackingParty);
                    if (own != null)
                    {
                        // PS-6 US-33: the player's own nominee in place of the nominee of record - its name, the party's poll at E1's future factor
                        name = own.Person;
                        s = poll.TryGetValue(c.BackingParty, out double q) && !double.IsNaN(q) && q > 0.0 ? q * PresidentialElection.FutureFactor : 0.0;
                    }
                    else if (c.BackingParty != null)
                    {
                        if (double.IsNaN(c.Factor)) { why = name + "'s factor (" + c.BackingParty + ") is not fitted - the factors are NaN until the fit is patched in (Tools/us_factor_patch.pl)"; field.Clear(); return false; }
                        s = poll.TryGetValue(c.BackingParty, out double p) && !double.IsNaN(p) && p > 0.0 ? p * c.Factor : 0.0;
                    }
                    else { s = PresidencyOfRecord.ShareOfRecord(id, pollingDay.Year, c.Surname); }
                    if (double.IsNaN(s) || s <= 0.0) { continue; }
                    support.Add((name, c.BackingParty, s, own != null));
                }
            }
            else
            {
                foreach (PoliticalParty party in PartySystems.For(id))
                {
                    if (!poll.TryGetValue(party.Abbrev, out double p) || double.IsNaN(p) || p <= 0.0) { continue; }
                    string name = UsTermLimit.UnnamedNominee(party, pollingDay.Year);
                    if (nominees != null) { foreach (UsTermLimit.Nominee n in nominees) { if (n.Party == party.Abbrev && !string.IsNullOrEmpty(n.Person)) { name = n.Person; } } }
                    support.Add((name, party.Abbrev, p * PresidentialElection.FutureFactor, false));
                }
            }
            double sum = 0.0;
            foreach ((string _, string _, double s, bool _) in support) { sum += s; }
            if (sum <= 0.0) { why = "no candidate has support in the poll"; return false; }
            foreach ((string name, string party, double s, bool own) in support) { field.Add(new Tally { Candidate = name, Party = party, Share = 100.0 * s / sum, OwnNominee = own }); }
            return true;
        }

        /// <summary>PS-6 US-33: the player's own nominee among <paramref name="nominees"/> for <paramref name="party"/>, or null - only a nominee marked
        /// <see cref="UsTermLimit.Nominee.Own"/> replaces a nominee of record.</summary>
        private static UsTermLimit.Nominee OwnNomineeOf(IReadOnlyList<UsTermLimit.Nominee> nominees, string party)
        {
            if (nominees == null) { return null; }
            foreach (UsTermLimit.Nominee n in nominees) { if (n != null && n.Own && n.Party == party && !string.IsNullOrEmpty(n.Person)) { return n; } }
            return null;
        }

        /// <summary>
        /// THE COUNT of <paramref name="pollingDay"/> on <paramref name="poll"/> - pure: nothing is written, nothing drawn. The field's national vote
        /// (<see cref="TryField"/>) pooled as the states pool it - the Republican nominee, the Democratic nominee, <see cref="OtherCandidates"/> - the states
        /// derived from the catalog's latest presidential election before the day (R-US14 (a); the record's, never the game's - a 2028 count swings from the record's 2024 states even after the game's own 2024 count, which <see cref="StatesOf"/> could give: ASKED of Elias, with whether US-26 owns it) and the electors
        /// those in force that year or carried forward (R-US13 (a)), by <see cref="ElectoralCollege.FromNationalShares(int, int, double[], int, IReadOnlyList{ElectoralCollege.Standing}, out ElectoralCollege.StateCall[], out double)"/>; then <see cref="FromCollege"/>.
        /// <paramref name="states"/> is every jurisdiction's call. Null, with the reason, where no count can be made.
        /// </summary>
        public static Contest Count(CountryId id, DateTime pollingDay, IReadOnlyDictionary<string, double> poll, out ElectoralCollege.StateCall[] states, out string why) =>
            Count(id, pollingDay, poll, null, out states, out why);

        /// <summary>PS-6 US-28: the count with a later field's nominees named (<see cref="UsTermLimit.NomineesFor"/>; null names none) - the same count, the same
        /// shares; where a party is term-limited its line says who cannot be elected again and who stands (<see cref="UsTermLimit.ClauseOf"/>). Pure, as the other.</summary>
        public static Contest Count(CountryId id, DateTime pollingDay, IReadOnlyDictionary<string, double> poll, IReadOnlyList<UsTermLimit.Nominee> nominees, out ElectoralCollege.StateCall[] states, out string why) =>
            Count(id, pollingDay, poll, nominees, 0, out states, out why);

        /// <summary>PS-6 US-8c (Elias's ruling J4): the count with its exact state and district ties drawn on <paramref name="tieSeed"/> (<see cref="TieSeedOf"/>;
        /// the overloads without one draw on seed 0), the seed kept on the contest so that <see cref="StatesOf"/> re-derives the same calls. Pure, as the others.</summary>
        public static Contest Count(CountryId id, DateTime pollingDay, IReadOnlyDictionary<string, double> poll, IReadOnlyList<UsTermLimit.Nominee> nominees, int tieSeed, out ElectoralCollege.StateCall[] states, out string why)
        {
            states = null;
            if (!ElectsIn(id)) { why = id + " chooses no president by electors"; return null; }
            if (!TryField(id, pollingDay, poll, nominees, out List<Tally> field, out why)) { return null; }
            Tally rep = null, dem = null;
            double others = 0.0;
            foreach (Tally t in field)
            {
                if (t.Party == "REP" && rep == null) { rep = t; }
                else if (t.Party == "DEM" && dem == null) { dem = t; }
                else { others += t.Share; }
            }
            if (rep == null || dem == null) { why = "the count needs both nominees, and " + (rep == null ? "the Republican" : "the Democratic") + " has no support in the poll"; return null; }
            var pooled = new Tally { Candidate = OtherCandidates, Party = null, Share = others };
            if (!ElectoralCollege.TryPriorYear(pollingDay.Year, out int prior) || !ElectoralCollege.TryElectorsYear(pollingDay.Year, out int inForce))
            {
                why = "the catalog holds no presidential election before " + pollingDay.Year.ToString(CultureInfo.InvariantCulture) + " to swing from";
                return null;
            }
            IReadOnlyList<ElectoralCollege.Standing> onBallots = OthersOnTheBallots(id, pollingDay.Year, pooled.Share);
            ElectoralCollege.Jurisdiction[] college = ElectoralCollege.FromNationalShares(prior, inForce, National(rep, dem, pooled), tieSeed, onBallots, out states, out double residual);
            string basis = string.Format(CultureInfo.InvariantCulture, "{0}; the states from {1}'s by the normalised uniform swing{5} (worst residual {2:E1}); the electors in force in {3}{4}",
                DayBasis, prior, residual, inForce, ElectorsCarriedForward(pollingDay, out int _) ? " - carried forward, no later apportionment being on record" : string.Empty,
                onBallots != null ? ", every other candidate standing only where on the ballot of record" : string.Empty);
            Contest contest = FromCollege(pollingDay, basis, college, rep, dem, pooled);
            contest.TieSeed = tieSeed;
            var othersLead = new List<string>();
            foreach (ElectoralCollege.StateCall call in states) { if (call.OthersLead) { othersLead.Add(call.State); } }
            if (othersLead.Count > 0)
            {
                contest.Line += "; every other candidate together leads in " + string.Join(", ", othersLead) + " - its electors go to the larger nominee (a single minor candidate's win is not modelled: the states pool the others)";
            }
            string limited = UsTermLimit.FieldOfRecord(pollingDay) ? null : UsTermLimit.ClauseOf(nominees);
            if (limited != null) { contest.Line += "; " + limited; }
            // PS-6 US-33: who stood in place of the nominee of record - said only where the player's own nominee did, so every other count's line is as before
            foreach (Tally own in new[] { rep, dem })
            {
                if (!own.OwnNominee) { continue; }
                UsTermLimit.Nominee n = OwnNomineeOf(nominees, own.Party);
                contest.Line += string.Format(CultureInfo.InvariantCulture, "; {0} stands for {1} in place of the nominee of record{2} - the player's own nominee, at the future factor, {3:0.0}",
                    own.Candidate, own.Party, n != null && !string.IsNullOrEmpty(n.Default) ? ", " + n.Default : string.Empty, PresidentialElection.FutureFactor);
            }
            return contest;
        }

        /// <summary>The census whose apportionment a presidential election of <paramref name="year"/> is held on: the Representatives a census apportions are
        /// those of the Congress elected two years after it (2 U.S.C. 2a(b)), and the electors those to which the states are entitled when the president comes
        /// into office (3 U.S.C. 3) - so 2012-2020 the 2010 census's and 2024 the 2020 census's (`ElectionsData/usa/president_returns.md`, reading 4), and 2028
        /// the 2020 census's by the same rule (DERIVED). An
        /// election on a later census than the catalog's electors were cut on carries them forward (R-US13 (a), RULED - F8: from 2032).</summary>
        private static int CensusOf(int year) => (year - 2) / 10 * 10;

        /// <summary>Whether an election on <paramref name="pollingDay"/> allocates on electors carried forward - the catalog's latest electors cut on an
        /// earlier census than the one the election stands on (<see cref="CensusOf"/>) - and the year whose electors it carries. Derived from the year alone,
        /// never saved: R-US13 (a), RULED (F8), carries them forward "labelled on screen", and the Parliament page's president row labels it from this.</summary>
        public static bool ElectorsCarriedForward(DateTime pollingDay, out int electorsYear) =>
            ElectoralCollege.TryElectorsYear(pollingDay.Year, out electorsYear) && CensusOf(pollingDay.Year) > CensusOf(electorsYear);

        /// <summary>The national shares a contest's three tallies carry, in <see cref="ElectoralCollege.ShareDemocrat"/>'s order, as fractions - the one
        /// conversion <see cref="Count"/> and <see cref="StatesOf"/> both make, so the states re-derived from a stored contest are the count's own.</summary>
        private static double[] National(Tally rep, Tally dem, Tally others) => new[] { dem.Share / 100.0, rep.Share / 100.0, others.Share / 100.0 };

        /// <summary>
        /// The count from its college on: the electors each nominee won (<see cref="ElectoralCollege.Allocate"/>, the statutes' rule in every jurisdiction), the
        /// dates (3 U.S.C. §§7 and 15, the Twentieth Amendment), and the Twelfth Amendment's test - "if such number be a majority of the whole number of Electors
        /// appointed" - the winner; with no one at a majority the contest is CONTINGENT and undecided until the House chooses by states on the count's day
        /// (<see cref="HoldHouseChoice"/>). <paramref name="rep"/> and <paramref name="dem"/> are the nominees' tallies (their shares the national vote, %);
        /// <paramref name="others"/> every other candidate's, who wins no elector. Pure; public so that a diagnostic can plant a college (R-US3's tie).
        /// </summary>
        public static Contest FromCollege(DateTime pollingDay, string dayBasis, ElectoralCollege.Jurisdiction[] college, Tally rep, Tally dem, Tally others)
        {
            int[] electors = ElectoralCollege.Allocate(college, 2);
            int appointed = 0;
            foreach (ElectoralCollege.Jurisdiction j in college) { appointed += j.TotalEv; }
            var contest = new Contest
            {
                PollingDay = pollingDay.Date, DayBasis = dayBasis, ElectorsMeet = ElectorsMeetAfter(pollingDay), CountedOn = CountedOnAfter(pollingDay),
                TakesOffice = OathDayAfter(pollingDay), ElectorsAppointed = appointed,
            };
            var r = new Tally { Candidate = rep.Candidate, Party = rep.Party, Votes = electors[ElectoralCollege.Republican], Share = rep.Share, OwnNominee = rep.OwnNominee };
            var d = new Tally { Candidate = dem.Candidate, Party = dem.Party, Votes = electors[ElectoralCollege.Democrat], Share = dem.Share, OwnNominee = dem.OwnNominee };
            bool repFirst = r.Votes > d.Votes || (r.Votes == d.Votes && r.Share >= d.Share);
            contest.Electors.Add(repFirst ? r : d);
            contest.Electors.Add(repFirst ? d : r);
            contest.Electors.Add(new Tally { Candidate = others.Candidate, Party = others.Party, Votes = 0, Share = others.Share });
            int majority = contest.MajorityOfElectors();
            Tally winner = r.Votes >= majority ? r : d.Votes >= majority ? d : null;
            string counted = string.Format(CultureInfo.InvariantCulture, "the count of {0:yyyy-MM-dd}: {1} ({2}) {3}, {4} ({5}) {6} of {7} electors ({8:0.00} % and {9:0.00} % of the vote, {10} {11:0.00} %)",
                contest.PollingDay, r.Candidate, r.Party, r.Votes, d.Candidate, d.Party, d.Votes, appointed, r.Share, d.Share, others.Candidate, others.Share);
            if (winner != null)
            {
                contest.Winner = winner.Candidate;
                contest.WinnerParty = winner.Party;
                contest.Line = string.Format(CultureInfo.InvariantCulture, "{0} - {1} elected; the electors meet {2:yyyy-MM-dd}, Congress counts {3:yyyy-MM-dd}, the oath {4:yyyy-MM-dd}",
                    counted, winner.Candidate, contest.ElectorsMeet, contest.CountedOn, contest.TakesOffice);
            }
            else
            {
                contest.Contingent = true;
                contest.Line = string.Format(CultureInfo.InvariantCulture, "{0} - no candidate has a majority of the {1} electors appointed ({2}): the House chooses by states on {3:yyyy-MM-dd} (the Twelfth Amendment)",
                    counted, appointed, majority, contest.CountedOn);
            }
            return contest;
        }

        /// <summary>
        /// The states a stored contest's count called, re-derived from the national shares its tallies carry (the two nominees and <see cref="OtherCandidates"/>)
        /// on the catalog's prior and electors for its year - the same function on the same doubles as <see cref="Count"/>, so the calls are the count's own and
        /// the save needs no per-state field. Null where the contest carries no such tallies (a planted one).
        /// </summary>
        public static ElectoralCollege.StateCall[] StatesOf(Contest contest)
        {
            Tally rep = null, dem = null, others = null;
            foreach (Tally t in contest.Electors)
            {
                if (t.Party == "REP") { rep = t; } else if (t.Party == "DEM") { dem = t; } else if (t.Candidate == OtherCandidates) { others = t; }
            }
            if (rep == null || dem == null || others == null || double.IsNaN(rep.Share) || double.IsNaN(dem.Share) || double.IsNaN(others.Share)) { return null; }
            if (!ElectoralCollege.TryPriorYear(contest.PollingDay.Year, out int prior) || !ElectoralCollege.TryElectorsYear(contest.PollingDay.Year, out int inForce)) { return null; }
            ElectoralCollege.FromNationalShares(prior, inForce, National(rep, dem, others), contest.TieSeed, OthersOnTheBallots(CountryId.USA, contest.PollingDay.Year, others.Share), out ElectoralCollege.StateCall[] states, out double _);
            return states;
        }

        /// <summary>
        /// PS-6 US-6c (Elias's ruling J3, 2026-10-19, `COMPLETED.md` §870: "a minor candidate stands only where on the ballot of record (the FEC's states)"):
        /// every other candidate of a field of record as the states hold it - each column of the field that no roster party backs, at its part of
        /// <paramref name="othersPercent"/> (the count's <see cref="OtherCandidates"/> tally, %) in proportion to its national share of record (the base
        /// <see cref="TryField"/> gives it, so each is its own share of the field), standing where `PresidencyOfRecord.StandingOfRecord` puts it, in the record's own pattern there. A pure function of
        /// the year and the pooled share, so <see cref="StatesOf"/> re-derives the count's states from a stored contest. Null where the election holds no field
        /// of record - a later field has no minor candidate, and the states take US-8's form, every other candidate swung from the prior election's.
        /// DECLARED (no format step): a 2024 contest saved by a build before US-6c re-derives its states by this standing, not by the pooled form its electors
        /// were counted on; until US-26 reads the game's own states, only the diagnostics read them.
        /// </summary>
        private static IReadOnlyList<ElectoralCollege.Standing> OthersOnTheBallots(CountryId id, int year, double othersPercent)
        {
            IReadOnlyList<PresidencyOfRecord.CandidateOfRecord> ofRecord = PresidencyOfRecord.CandidatesOf(id, year + 1);
            var columns = new List<(string Surname, double Base)>();
            double bases = 0.0;
            foreach (PresidencyOfRecord.CandidateOfRecord c in ofRecord)
            {
                if (c.BackingParty != null) { continue; }
                double b = PresidencyOfRecord.ShareOfRecord(id, year, c.Surname);
                if (double.IsNaN(b) || b <= 0.0) { continue; }   // TryField's own filter: a column with no base is not in the field
                columns.Add((c.Surname, b));
                bases += b;
            }
            if (columns.Count == 0 || bases <= 0.0) { return null; }
            var standing = new List<ElectoralCollege.Standing>();
            foreach ((string surname, double b) in columns)
            {
                standing.Add(new ElectoralCollege.Standing(surname, othersPercent / 100.0 * b / bases, PresidencyOfRecord.StandingOfRecord(id, year, surname)));
            }
            return standing;
        }

        /// <summary>Whether <paramref name="date"/> is the country's presidential polling day (`WorldClock.TryNextPollingDay`) and the game has not counted it yet.</summary>
        public static bool IsPollingDay(Country country, DateTime date)
        {
            if (country?.UsPresidentialElections == null || !ElectsIn(country.Id) || !WorldClock.TryNextPollingDay(country.Id, date, out DateTime day) || day != date.Date) { return false; }
            foreach (Contest c in country.UsPresidentialElections) { if (c.PollingDay.Date == date.Date) { return false; } }
            return true;
        }

        /// <summary>The day loop's count: on the country's polling day, not yet counted, the count on <paramref name="poll"/> added to the country and returned;
        /// null otherwise, with the reason where a polling day could not be counted. PS-6 US-28: a later field's nominees are the country's own
        /// (<see cref="UsTermLimit.NomineesFor"/> - read from its contests, the record and the player's pick; none for the field of record). US-8b (R-US25 as
        /// ruled): the poll carries the election's noise where a σ is set (<see cref="NoiseSigmaPp"/>), drawn from the day's common source
        /// (<see cref="SimulationRandom.ElectionNoiseOn"/>), so every fork of the game counts its own election on the live game's noise.</summary>
        public static Contest Hold(Country country, DateTime date, IReadOnlyDictionary<string, double> poll, out string why)
        {
            if (country?.UsPresidentialElections == null) { why = "the country carries no list of contests"; return null; }
            if (!IsPollingDay(country, date)) { why = "not a polling day the game has still to count"; return null; }
            IReadOnlyDictionary<string, double> counted = CountedPoll(country, date, poll);
            Contest contest = Count(country.Id, date.Date, counted, UsTermLimit.NomineesFor(country, date.Date), TieSeedOf(SimulationRandom.MasterSeed, date.Year), out ElectoralCollege.StateCall[] _, out why);
            if (contest != null) { country.UsPresidentialElections.Add(contest); }
            return contest;
        }

        /// <summary>The poll a polling day counts: <paramref name="poll"/> with the election's noise where a σ is set (<see cref="NoiseSigmaPp"/>, the day's
        /// common source) - the presidential count's and, since PS-6 US-16, the House's V(T) on the same day (one poll, R-US15 (a)).</summary>
        public static IReadOnlyDictionary<string, double> CountedPoll(Country country, DateTime date, IReadOnlyDictionary<string, double> poll) =>
            NoiseSigmaPp > 0.0 && poll != null ? WithNoise(poll, SimulationRandom.ElectionNoiseOn(country.Id, date.Date), NoiseSigmaPp) : poll;

        /// <summary>PS-6 US-8c (J4): the tie seed of the game's count in <paramref name="year"/> - the world's master seed and the year folded together, so two
        /// elections of one game draw their ties apart (<see cref="ElectoralCollege.TieBreak"/>; CONVENTION).</summary>
        public static int TieSeedOf(int masterSeed, int year) => unchecked(masterSeed * 31 + year);

        /// <summary>
        /// US-8b (R-US25 as ruled): the election-day noise on the national vote, σ in percentage points of a share - ZERO in play, DECLARED: no election noise
        /// is on in play yet (the audit's q2 turns turnout and the declared noise on in play, with its own figure), so the count draws nothing and every count is
        /// the one US-8 built. Where a σ is set, <see cref="Hold"/> draws it from <see cref="SimulationRandom.ElectionNoiseOn"/> - the live game's and every
        /// fork's the same numbers for the same polling day (common random numbers) - so a fork elects a different president only because of policy.
        /// </summary>
        public static readonly double NoiseSigmaPp = 0.0;

        /// <summary>US-8b: <paramref name="poll"/> with election-day noise of <paramref name="sigmaPp"/> on each party's share (§27's form,
        /// <see cref="RegionalAggregation.ApplyNoise"/>: a normal draw per share, a negative share floored at zero, re-normalised) - the parties taken in ordinal
        /// order of their keys, so the draws meet the same parties in every world; a share that is not a positive number is left as it is and draws nothing. The
        /// noisy shares keep the poll's own total. Pure, given <paramref name="noise"/>.</summary>
        public static Dictionary<string, double> WithNoise(IReadOnlyDictionary<string, double> poll, System.Random noise, double sigmaPp)
        {
            var noisy = new Dictionary<string, double>();
            var keys = new List<string>();
            double total = 0.0;
            foreach (KeyValuePair<string, double> kv in poll)
            {
                noisy[kv.Key] = kv.Value;
                if (!double.IsNaN(kv.Value) && !double.IsInfinity(kv.Value) && kv.Value > 0.0) { keys.Add(kv.Key); total += kv.Value; }
            }
            if (keys.Count == 0 || total <= 0.0) { return noisy; }
            keys.Sort(StringComparer.Ordinal);
            var shares = new double[keys.Count];
            for (int i = 0; i < keys.Count; i++) { shares[i] = poll[keys[i]] / total; }
            double[] drawn = RegionalAggregation.ApplyNoise(shares, noise, sigmaPp);
            for (int i = 0; i < keys.Count; i++) { noisy[keys[i]] = drawn[i] * total; }
            return noisy;
        }

        /// <summary>
        /// R-US3 (a), RULED (F8) - THE HOUSE'S CHOICE, on the count's day: a contingent contest still undecided whose day <paramref name="date"/> is chooses by
        /// states (<see cref="ChooseByStates"/>) among the delegations of the House that sits that day (<see cref="DelegationsOn"/>), a majority of all the
        /// states (<see cref="AllStates"/>) to elect. Returns the contest it decided or left undecided, or null where none was due; <paramref name="choice"/> is
        /// the House's own line (the count's line is not repeated in it).
        /// </summary>
        public static Contest HoldHouseChoice(Country country, DateTime date, out string choice)
        {
            choice = null;
            if (country?.UsPresidentialElections == null || !ElectsIn(country.Id)) { return null; }
            foreach (Contest c in country.UsPresidentialElections)
            {
                if (!c.Contingent || c.Decided() || c.Delegations.Count > 0 || c.CountedOn.Date != date.Date) { continue; }
                choice = ChooseByStates(c, DelegationsOn(country, date, out string house), AllStates(c.PollingDay), house, date);
                // PS-6 US-16 - R-US22 RULED (I6): a House that cannot choose hands the Vice President to the Senate, the same day
                if (!c.Decided()) { choice += "; " + ChooseVicePresident(country, c, date); }
                return c;
            }
            return null;
        }

        /// <summary>The Twelfth Amendment's "a majority of all the states shall be necessary to a choice" (the US-8 review's L1): how many states there are for an
        /// election on <paramref name="pollingDay"/>, read from the apportionment the catalog holds for its electors - every jurisdiction with electors but the
        /// District of Columbia, whose electors the Twenty-third Amendment gives and which is no state - never from the delegations a House lists, so a state
        /// with no member of either party still counts towards the majority it cannot vote in.</summary>
        public static int AllStates(DateTime pollingDay)
        {
            if (!ElectoralCollege.TryElectorsYear(pollingDay.Year, out int year)) { return 0; }
            var states = new HashSet<string>(StringComparer.Ordinal);
            foreach (var s in Generated.UsPresidentialReturns.States) { if (s.Year == year && s.State != "DC") { states.Add(s.State); } }
            return states.Count;
        }

        /// <summary>
        /// The House that sits on <paramref name="date"/>, by state: each state's members by party - the winners by district of the election whose House the
        /// record seats that day (`WorldClock.SeatedVintage`; past the record the last of record stands, as the game seats it, US-2), read from the catalog
        /// (<see cref="Generated.UsPresidentialReturns.HouseDistricts"/>, the Clerk's statistics). DECLARED: until the game elects the House (US-16) the
        /// House of record chooses, seated as elected (a member's later vacancy or change is a deviation the record's rule never seats, §618).
        /// <paramref name="house"/> names it in the line's words.
        /// </summary>
        public static List<(string State, int Republicans, int Democrats)> DelegationsOn(Country country, DateTime date, out string house)
        {
            // PS-6 US-16: the House the game elected, from the day it is seated (3 January) - its delegations as its count gave them
            if (UsCongressElection.TryInOffice(country, date, out UsCongressElection.Contest elected))
            {
                house = "the House the game elected on " + elected.PollingDay.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                return elected.Delegations();
            }
            return DelegationsOn(date, out house);
        }

        /// <summary>The House of record on <paramref name="date"/>, by state (see the overload above) - the House the record seats that day.</summary>
        public static List<(string State, int Republicans, int Democrats)> DelegationsOn(DateTime date, out string house)
        {
            ElectionVintage seated = WorldClock.SeatedVintage(CountryId.USA, date);
            int year = WorldClock.ElectionDayOf(CountryId.USA, seated).Year;
            house = "the House elected in " + year.ToString(CultureInfo.InvariantCulture);
            var byState = new SortedDictionary<string, (int R, int D)>(StringComparer.Ordinal);
            foreach (var district in Generated.UsPresidentialReturns.HouseDistricts)
            {
                if (district.Year != year) { continue; }
                byState.TryGetValue(district.State, out (int R, int D) n);   // every state of the year, whatever its winners - one with none of either party lists 0-0
                byState[district.State] = district.Winner == "R" ? (n.R + 1, n.D) : district.Winner == "D" ? (n.R, n.D + 1) : n;
            }
            var delegations = new List<(string State, int Republicans, int Democrats)>();
            foreach (KeyValuePair<string, (int R, int D)> kv in byState) { delegations.Add((kv.Key, kv.Value.R, kv.Value.D)); }
            return delegations;
        }

        /// <summary>
        /// The Twelfth Amendment's choice (`archives_amendments_11-27.html`, [CONST-AM]): "from the persons having the highest numbers not exceeding three on the
        /// list of those voted for as President, the House of Representatives shall choose immediately, by ballot ... the votes shall be taken by states, the
        /// representation from each state having one vote ... and a majority of all the states shall be necessary to a choice." DECLARED readings: a delegation
        /// votes for the nominee of the party holding more of its seats than the other's - members vote their party's nominee, as the stance model reads every
        /// vote; a tied delegation casts no vote (the ruling's own words), and a state listed with no member of either party, or not listed, none; only a
        /// nominee who won electors is eligible. A candidate with a majority of <paramref name="allStates"/> - ALL the states (<see cref="AllStates"/>), not those
        /// listed or voting - is elected; else nothing is decided, and since PS-6 US-16 who governs then is R-US22's order as ruled (I6): the Senate chooses the
        /// Vice President the same day (<see cref="ChooseVicePresident"/>), who acts from the oath, else the Speaker (<see cref="ActingGovernmentOf"/>). The quorum (a member from two thirds of the states) is met by every House of record, stated. Returns the House's own line, which is
        /// also appended to the contest's.
        /// </summary>
        public static string ChooseByStates(Contest contest, IReadOnlyList<(string State, int Republicans, int Democrats)> delegations, int allStates, string house, DateTime on)
        {
            Tally rep = null, dem = null;
            foreach (Tally t in contest.Electors) { if (t.Party == "REP" && t.Votes > 0) { rep = t; } else if (t.Party == "DEM" && t.Votes > 0) { dem = t; } }
            int forRep = 0, forDem = 0, tied = 0, none = 0, empty = 0;
            foreach ((string _, int r, int d) in delegations)
            {
                if (r > d) { if (rep != null) { forRep++; } else { none++; } }
                else if (d > r) { if (dem != null) { forDem++; } else { none++; } }
                else if (r == 0) { empty++; }   // no member of either party - not a tie; casts no vote
                else { tied++; }
            }
            int states = Math.Max(allStates, delegations.Count), majority = states / 2 + 1, absent = states - delegations.Count;
            contest.Delegations.Clear();
            if (rep != null) { contest.Delegations.Add(new Tally { Candidate = rep.Candidate, Party = rep.Party, Votes = forRep }); }
            if (dem != null) { contest.Delegations.Add(new Tally { Candidate = dem.Candidate, Party = dem.Party, Votes = forDem }); }
            contest.Delegations.Sort((a, b) => b.Votes.CompareTo(a.Votes));
            Tally chosen = rep != null && forRep >= majority ? rep : dem != null && forDem >= majority ? dem : null;
            string tally = string.Format(CultureInfo.InvariantCulture, "on {0:yyyy-MM-dd} {1} chose by states - {2} {3}, {4} {5}, {6} tied (no vote){7}{8} of {9}; a majority of all the states is {10}",
                on, house, rep?.Candidate ?? "the Republican nominee", forRep, dem?.Candidate ?? "the Democratic nominee", forDem, tied,
                (none > 0 ? ", " + none.ToString(CultureInfo.InvariantCulture) + " for no eligible candidate" : string.Empty)
                + (empty > 0 ? ", " + empty.ToString(CultureInfo.InvariantCulture) + " with no member of either party (no vote)" : string.Empty),
                absent > 0 ? ", " + absent.ToString(CultureInfo.InvariantCulture) + " with no delegation listed (no vote)" : string.Empty, states, majority);
            string choice = chosen != null
                ? string.Format(CultureInfo.InvariantCulture, "{0} - {1} elected, the oath {2:yyyy-MM-dd}", tally, chosen.Candidate, contest.TakesOffice)
                : tally + " - no candidate has it: the House cannot choose; nothing is decided";
            if (chosen != null)
            {
                contest.Winner = chosen.Candidate;
                contest.WinnerParty = chosen.Party;
            }
            contest.Line += "; " + choice;
            return choice;
        }
    }
}
