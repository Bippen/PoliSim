using System;
using System.Collections.Generic;
using System.Globalization;
using PoliSim.Data;

namespace PoliSim.Elections
{
    /// <summary>
    /// PS-6 US-28 (`docs/specs/USA_STAGE_PLAN.md`, "The two-term limit and the next nominee"; R-US6 (b) as Elias ruled it, F8 §781: "the term limit doesn't end
    /// the run. The player picks the party's next nominee (the Vice President by default) and campaigns on the outgoing record"): THE TWENTY-SECOND AMENDMENT AS
    /// DATA, AND THE NOMINEE OF A TERM-LIMITED PARTY.
    /// <list type="bullet">
    /// <item><description><b>Each person's elections</b> (<see cref="ElectionsBefore"/>) are read, never typed: the record's - the winner of each presidential
    /// election the generated catalog holds (<see cref="Generated.UsPresidentialReturns.Years"/>, NARA's names and electoral votes; the winner the nominee with a
    /// majority of the electors) - before the game's first contest, then the game's own decided contests (<see cref="Country.UsPresidentialElections"/>), which
    /// the save already carries. From the game's first polling day the record yields, as it does for the office (R-US1 (a)): a record's election counts only
    /// where the game did not hold that year's. Poland's pattern (<c>PresidentialElection.TermsServed</c>) counts terms; the Twenty-second Amendment counts
    /// ELECTIONS, so the USA has its own counter.</description></item>
    /// <item><description><b>The person, not the label</b> (<see cref="PersonOf"/>): a nominee of an election whose field is the record's (2024's, R-US2 (a)) is
    /// NARA's nominee for the ticket whatever words the count prints for him - so the count's naming, asked of Elias with US-6's asymmetry, can change without
    /// changing who is barred. Any other nominee is the name the game gave it; an unnamed one carries its year, so two of a party's are never one person (the
    /// rule Poland's unnamed candidates follow).</description></item>
    /// <item><description><b>A person elected twice cannot stand</b> (<see cref="FirstClause"/>, held to NARA's saved amendments page by `TermLimitDiagnostic`).
    /// The amendment's second clause (more than two years of a term to which another was elected) arises only through succession, which the game does not model
    /// (the plan's §8: no Twenty-fifth Amendment) - DECLARED not reachable, never counted.</description></item>
    /// <item><description><b>The next field</b> (<see cref="NomineesFor"/>), for a polling day whose field is not the record's (2028 on; 2024's is R-US2 (a)'s and
    /// is left exactly as US-6 and US-8 built it): the president's party fields the president where the amendment allows (DECLARED, Poland's built rule - the
    /// incumbent within the limit stands); a TERM-LIMITED president's party fields the Vice President by default - the record's by date while the record's
    /// president holds the office (`ElectionsData/usa/records_by_date.md` §5), the game ticket's running mate once the game's president does (below) - and the
    /// player's party fields the player's pick where one stands (<see cref="Pick"/>); an AI party always takes the default. A party out of the White House
    /// fields its candidate unnamed, as US-8 built it - ASKED of Elias (the US-28 review's M1): POLITICAL_SYSTEM_SPEC.md §6's "A defeated president may run
    /// again four years later" is not built by it (a Harris elected in 2024 and beaten in 2028 is eligible in 2032 and cannot be fielded); the options put are
    /// the out-party's pick from its people, its last eligible nominee, or §6 amended. Every nominee stands at E1's later factor
    /// (<see cref="PresidentialElection.FutureFactor"/>): the pick changes who stands, never the shares (US-9: no US campaign moves the national vote either).</description></item>
    /// <item><description><b>The running mate.</b> The record's tickets are NARA's (<see cref="RunningMatesOfRecord"/>). DECLARED, not a consequence of R-US2 (a)
    /// (which names the presidential nominee only), and ASKED with the rest of this reading: a 2024 ticket the game elects carries the record's running mate
    /// (Vance, Walz) - the alternative, the plan's "the party's own" read literally, would make REP's 2028 default "Republican Party's candidate for Vice President
    /// (2024)". A ticket the game makes (2028 on) carries the party's own running mate, unnamed and dated (<see cref="UnnamedRunningMate"/>) - the plan's premise,
    /// DECLARED until a ticket is modelled - a re-elected president's ticket too, so the sitting Vice President drops to an alternative (the other way, keeping
    /// the sitting Vice President on a re-elected ticket, is asked beside it).</description></item>
    /// <item><description><b>The party's people</b> (<see cref="PartyPeople"/>, DECLARED - the plan leaves the pool open): the Vice President in office and the
    /// president in office where of the party, every person on the party's tickets of the game's contests (2024's the record's tickets) and the party's own
    /// candidate, unnamed, for the year (R-US6 (b)'s "an unnamed nominee at factor 1.0", which US-33's own nominee will stand beside). The record's earlier
    /// tickets are not in it: the catalog does not hold every earlier election of theirs, so an eligibility read from it could be false for them.</description></item>
    /// <item><description><b>Nothing ends the run here</b>: the limit binds the person, never the run (POLITICAL_SYSTEM_SPEC.md §6 and §7 as amended at §781).</description></item>
    /// </list>
    /// Pure functions of the country's state (R-US5: player-agnostic); the pick is the one thing written, by <see cref="Pick"/>, and only for the player's party.
    /// </summary>
    public static class UsTermLimit
    {
        /// <summary>The Twenty-second Amendment, Section 1, its first clause in its own words ([CONST-AM], `ElectionsData/usa/records_by_date.md` §4).</summary>
        public const string FirstClause = "No person shall be elected to the office of the President more than twice";

        /// <summary>Its second clause in its own words - not reachable while no succession is modelled (see the class).</summary>
        public const string SecondClause = "no person who has held the office of President, or acted as President, for more than two years of a term to which some other person was elected President shall be elected to the office of the President more than once";

        /// <summary>The first clause's "more than twice": a person with this many elections cannot be elected again.</summary>
        public const int MostElections = 2;

        /// <summary>One presidential election a person won: the polling day, the person, the party by key, and whether the game held it (else the record's).</summary>
        public readonly struct Election
        {
            public readonly DateTime PollingDay;
            public readonly string Person;
            public readonly string Party;
            public readonly bool InGame;
            public Election(DateTime pollingDay, string person, string party, bool inGame) { PollingDay = pollingDay; Person = person; Party = party; InGame = inGame; }
        }

        /// <summary>The player's pick of the party's nominee for a polling day - kept with the country (<see cref="Country.UsNomineePicks"/>), so a save carries it.
        /// Additive under ruling A: a save from before carries none, and none is the default - the state every earlier build was in.</summary>
        public sealed class NomineePick
        {
            public DateTime PollingDay;
            public string Party;
            public string Person;
        }

        /// <summary>A party's nominee on a polling day whose field is the game's, with the reading that put it there.</summary>
        public sealed class Nominee
        {
            /// <summary>The party, by key.</summary>
            public string Party;
            /// <summary>The person who stands.</summary>
            public string Person;
            /// <summary>The person who stands by default - the Vice President, the president, or the party's candidate unnamed; the player's pick replaces it.</summary>
            public string Default;
            /// <summary>Why this person stands, in a line.</summary>
            public string Basis;
            /// <summary>The president is of this party and cannot be elected again.</summary>
            public bool TermLimited;
            /// <summary>The person is the player's pick.</summary>
            public bool Picked;
            /// <summary>The default is the Vice President (else the party's candidate unnamed, or the president).</summary>
            public bool ViceDefault;
            /// <summary>The barred president as the game shows him, and the elections that bar him ("elected 2016 and 2024"); null unless term-limited.</summary>
            public string Barred;
            public string BarredBecause;
        }

        /// <summary>
        /// The running mates of the record's tickets, by NARA's Electoral College results pages: "Vice President Kamala D. Harris [D]" (2020, [EX-EC20],
        /// `raw/executive/archives_electoral_college_2020.html`); "Vice President JD Vance [R] V.P. Opponent Tim Walz [D]" (2024, [EX-EC24],
        /// `raw/executive/archives_electoral_college_2024.html`) - NARA's names, NARA's tags read as the roster's keys. Names, not figures (the record's
        /// presidents are typed the same way, `WorldClock.Governments`); `TermLimitDiagnostic` holds each to its page's words. Two readers: the record's Vice
        /// President by date (records §5: Harris from 20 Jan 2021, Vance from 20 Jan 2025 - the running mate of the ticket that elected the president of record
        /// then; read only through <see cref="TryVicePresidentOfRecordOn"/>), and the running mate on a 2024 ticket the game elects (Vance, Walz - DECLARED, asked).
        /// </summary>
        private static readonly (int Year, string Party, string Person)[] MatesOfRecord =
        {
            (2020, "DEM", "Kamala D. Harris"),
            (2024, "REP", "JD Vance"),
            (2024, "DEM", "Tim Walz"),
        };

        /// <summary>The record's running mates (<see cref="MatesOfRecord"/>) - read by the diagnostic, which holds each to NARA's page.</summary>
        public static IReadOnlyList<(int Year, string Party, string Person)> RunningMatesOfRecord => MatesOfRecord;

        /// <summary>The running mate of the record's ticket of a party in a year; null where the record above holds none.</summary>
        public static string RunningMateOfRecord(int year, string party)
        {
            foreach ((int y, string p, string person) in MatesOfRecord) { if (y == year && p == party) { return person; } }
            return null;
        }

        /// <summary>Whether a polling day's field is the record's (2024's, R-US2 (a) - <see cref="PresidencyOfRecord.CandidatesOf"/>): then no nominee is the
        /// game's to name, and the field stands as US-6 built it.</summary>
        public static bool FieldOfRecord(DateTime pollingDay) => PresidencyOfRecord.CandidatesOf(CountryId.USA, pollingDay.Year + 1).Count > 0;

        /// <summary>A party's candidate unnamed - the label US-8's later field gives, its year in it (two of a party's are never one person).</summary>
        public static string UnnamedNominee(PoliticalParty party, int year) => party.Name + "'s candidate (" + year.ToString(CultureInfo.InvariantCulture) + ")";

        /// <summary>The running mate of a ticket the game makes - the party's own, unnamed and dated, DECLARED until a ticket is modelled. Worded as a candidate
        /// so every reader that keeps an unnamed candidate's label whole (`WorldClock.SurnameOf`) keeps this one whole.</summary>
        public static string UnnamedRunningMate(PoliticalParty party, int year) => party.Name + "'s candidate for Vice President (" + year.ToString(CultureInfo.InvariantCulture) + ")";

        /// <summary>The person a contest's tally names: for an election whose field is the record's, the nominee NARA names for the ticket (the catalog's year row),
        /// whatever label the count gave; otherwise the tally's own name.</summary>
        public static string PersonOf(UsPresidentialElection.Contest contest, UsPresidentialElection.Tally tally)
        {
            if (contest == null || tally == null) { return null; }
            if ((tally.Party == "REP" || tally.Party == "DEM") && FieldOfRecord(contest.PollingDay))
            {
                foreach (var y in Generated.UsPresidentialReturns.Years)
                {
                    if (y.Year == contest.PollingDay.Year) { return tally.Party == "REP" ? y.NomineeR : y.NomineeD; }
                }
            }
            return tally.Candidate;
        }

        /// <summary>The person a decided contest elected (<see cref="PersonOf"/> on its winner's tally; its winner's name where it carries no such tally); null while undecided.</summary>
        public static string WinnerOf(UsPresidentialElection.Contest contest)
        {
            if (contest == null || !contest.Decided()) { return null; }
            foreach (UsPresidentialElection.Tally t in contest.Electors) { if (t.Party == contest.WinnerParty && t.Candidate == contest.Winner) { return PersonOf(contest, t); } }
            return PersonOf(contest, new UsPresidentialElection.Tally { Candidate = contest.Winner, Party = contest.WinnerParty });
        }

        /// <summary>The running mate on a contest's ticket for a party: the record's where the field is the record's, else the party's own, unnamed.</summary>
        public static string RunningMateOf(UsPresidentialElection.Contest contest, string party)
        {
            if (contest == null || party == null) { return null; }
            if (FieldOfRecord(contest.PollingDay)) { return RunningMateOfRecord(contest.PollingDay.Year, party); }
            foreach (PoliticalParty p in PartySystems.For(CountryId.USA)) { if (p.Abbrev == party) { return UnnamedRunningMate(p, contest.PollingDay.Year); } }
            return null;
        }

        /// <summary>
        /// Every presidential election won before <paramref name="before"/>, oldest first: the record's - each catalog year's nominee with a majority of the
        /// electors (the Twelfth Amendment's test on the electoral votes cast for him) - whose polling day falls before the game's first contest, then each
        /// contest the game decided (chosen by the House included). A record's year the game held is the game's, never counted twice.
        /// </summary>
        public static List<Election> ElectionsBefore(Country country, DateTime before)
        {
            var won = new List<Election>();
            DateTime gameFirst = DateTime.MaxValue;
            List<UsPresidentialElection.Contest> contests = country?.UsPresidentialElections;
            if (contests != null) { foreach (UsPresidentialElection.Contest c in contests) { if (c.PollingDay.Date < gameFirst) { gameFirst = c.PollingDay.Date; } } }
            foreach (var y in Generated.UsPresidentialReturns.Years)
            {
                DateTime day = UsPresidentialElection.PresidentialDay(y.Year);
                if (day >= before.Date || day >= gameFirst) { continue; }
                int majority = y.Electors / 2 + 1;
                if (y.CastR >= majority) { won.Add(new Election(day, y.NomineeR, "REP", false)); }
                else if (y.CastD >= majority) { won.Add(new Election(day, y.NomineeD, "DEM", false)); }
            }
            if (contests != null)
            {
                foreach (UsPresidentialElection.Contest c in contests)
                {
                    if (c.Decided() && c.PollingDay.Date < before.Date) { won.Add(new Election(c.PollingDay.Date, WinnerOf(c), c.WinnerParty, true)); }
                }
            }
            won.Sort((a, b) => a.PollingDay.CompareTo(b.PollingDay));
            return won;
        }

        /// <summary>The elections <paramref name="person"/> won before <paramref name="before"/>, oldest first. ⚠ True only for a person whose every presidential
        /// election falls in the catalog's window or in the game's contests (the review's L7): an earlier winner's elections before the catalog's first year are not
        /// held (Barack Obama's 2008 among them), so his count reads short. No one the pick can offer is such a person - `TermLimitDiagnostic` (i) holds the pool's
        /// people of record against NARA's earlier pages.</summary>
        public static List<Election> ElectionsOf(Country country, string person, DateTime before)
        {
            var of = new List<Election>();
            if (string.IsNullOrEmpty(person)) { return of; }
            foreach (Election e in ElectionsBefore(country, before)) { if (e.Person == person) { of.Add(e); } }
            return of;
        }

        /// <summary>Whether <paramref name="person"/> may be elected on <paramref name="pollingDay"/>: fewer than <see cref="MostElections"/> elections before it -
        /// with <see cref="ElectionsOf"/>'s limit: the answer holds only for a person whose elections all fall in the catalog's window (from its first year) or the
        /// game's own contests; check (i) of `TermLimitDiagnostic` guards the pick's pool against that.</summary>
        public static bool Eligible(Country country, string person, DateTime pollingDay) => ElectionsOf(country, person, pollingDay).Count < MostElections;

        /// <summary>The years of a list of elections in words - "2016 and 2024".</summary>
        public static string YearsOf(IReadOnlyList<Election> elections)
        {
            var years = new List<string>();
            foreach (Election e in elections) { years.Add(e.PollingDay.Year.ToString(CultureInfo.InvariantCulture)); }
            if (years.Count <= 1) { return string.Join(string.Empty, years); }
            return string.Join(", ", years.GetRange(0, years.Count - 1)) + " and " + years[years.Count - 1];
        }

        /// <summary>
        /// The president in office on <paramref name="date"/>: the winner of the game's latest contest sworn by then (<see cref="UsPresidentialElection.TryInOffice"/>);
        /// else, while the record seats the office, the president of record by date; else the sitting president the country's government names (a contest the House
        /// could not decide leaves him there). <paramref name="person"/> is who he is (<see cref="PersonOf"/>), <paramref name="shownAs"/> the name the game shows.
        /// </summary>
        public static bool TryPresidentOn(Country country, DateTime date, out string person, out string party, out string shownAs)
        {
            person = null; party = null; shownAs = null;
            if (country == null || !UsPresidentialElection.ElectsIn(country.Id)) { return false; }
            if (UsPresidentialElection.TryInOffice(country, date, out UsPresidentialElection.Contest sworn))
            {
                person = WinnerOf(sworn); party = sworn.WinnerParty; shownAs = sworn.Winner;
                return person != null;
            }
            if (WorldClock.RecordSeatsExecutive(country, date) && WorldClock.TryGovernmentAt(country.Id, date, out WorldClock.GovernmentOfRecord g) && g.Kind == WorldClock.ExecutiveKind.Presidency)
            {
                person = NameOf(g.President ?? g.Head); party = g.Cabinet != null && g.Cabinet.Length > 0 ? g.Cabinet[0] : null; shownAs = person;
                return person != null;
            }
            GovernmentRecord sitting = country.Government;
            if (sitting != null && sitting.Kind == WorldClock.ExecutiveKind.Presidency && !string.IsNullOrEmpty(sitting.Executive))
            {
                person = NameOf(sitting.Executive); party = sitting.PmParty; shownAs = person;
                return true;
            }
            return false;
        }

        /// <summary>
        /// The Vice President in office on <paramref name="date"/>: where the game's president holds the office, the running mate on that contest's ticket
        /// (<see cref="RunningMateOf"/>); where the record's does, the Vice President of record by date (<see cref="TryVicePresidentOfRecordOn"/>); where a sitting
        /// president of record stays past an undecided contest, the Vice President of record on the day his row opened. False where none is held.
        /// </summary>
        public static bool TryVicePresidentOn(Country country, DateTime date, out string person, out string party, out string basis)
        {
            person = null; party = null; basis = null;
            if (country == null || !UsPresidentialElection.ElectsIn(country.Id)) { return false; }
            if (UsPresidentialElection.TryInOffice(country, date, out UsPresidentialElection.Contest sworn))
            {
                party = sworn.WinnerParty;
                person = RunningMateOf(sworn, party);
                int year = sworn.PollingDay.Year;
                basis = FieldOfRecord(sworn.PollingDay)
                    ? "the running mate on the " + year.ToString(CultureInfo.InvariantCulture) + " ticket of record, which the game elected (NARA's results page; DECLARED, asked of Elias)"
                    : "the running mate on the game's " + year.ToString(CultureInfo.InvariantCulture) + " ticket - the party's own, unnamed until a ticket is modelled";
                return person != null;
            }
            if (WorldClock.RecordSeatsExecutive(country, date)) { return TryVicePresidentOfRecordOn(country.Id, date, out person, out party, out basis); }
            if (country.Government != null && country.Government.Kind == WorldClock.ExecutiveKind.Presidency)
            {
                // a sitting president of record carried past an undecided contest: the Vice President of record on the day his row opened
                foreach (WorldClock.GovernmentOfRecord g in WorldClock.Governments(country.Id))
                {
                    if (g.Kind == WorldClock.ExecutiveKind.Presidency && g.President == country.Government.Executive) { return TryVicePresidentOfRecordOn(country.Id, g.From, out person, out party, out basis); }
                }
            }
            return false;
        }

        /// <summary>
        /// THE RECORD'S VICE PRESIDENT BY DATE - the one place the record's Vice President is read (the US-28 review's M2: isolated so that it can be repointed
        /// to the record's own Vice Presidents by date where those are kept beside the presidents, `ElectionsData/usa/records_by_date.md` §5, and no second
        /// copy can drift from them). Today: the presidency of record on <paramref name="date"/>, and the running mate on the ticket that elected it
        /// (<see cref="RunningMateOfRecord"/>) - the election being the November before the 20 January its row opens on (the Twentieth Amendment §1, DERIVED).
        /// False where no presidency of record holds that day or the record above names no running mate for it.
        /// </summary>
        public static bool TryVicePresidentOfRecordOn(CountryId id, DateTime date, out string person, out string party, out string basis)
        {
            person = null; party = null; basis = null;
            if (!WorldClock.TryGovernmentAt(id, date, out WorldClock.GovernmentOfRecord row) || row.Kind != WorldClock.ExecutiveKind.Presidency) { return false; }
            int elected = row.From.Year - 1;
            party = row.Cabinet != null && row.Cabinet.Length > 0 ? row.Cabinet[0] : null;
            person = RunningMateOfRecord(elected, party);
            basis = "the Vice President of record by date - the running mate on the " + elected.ToString(CultureInfo.InvariantCulture) + " ticket that elected the president of record (NARA's results page)";
            return person != null;
        }

        /// <summary>The person a tagged name names - "Donald J. Trump (REP)" is Donald J. Trump.</summary>
        private static string NameOf(string tagged)
        {
            if (string.IsNullOrEmpty(tagged)) { return null; }
            int tag = tagged.LastIndexOf(" (", StringComparison.Ordinal);
            return tag > 0 ? tagged.Substring(0, tag) : tagged;
        }

        private static PoliticalParty? PartyOf(CountryId id, string abbrev)
        {
            foreach (PoliticalParty p in PartySystems.For(id)) { if (p.Abbrev == abbrev) { return p; } }
            return null;
        }

        /// <summary>
        /// The field's nominees on <paramref name="pollingDay"/>, a nominee for each roster party, in the roster's order; empty where the day's field is the
        /// record's (<see cref="FieldOfRecord"/>) or the country chooses no president by electors. See the class for the rule.
        /// </summary>
        public static List<Nominee> NomineesFor(Country country, DateTime pollingDay)
        {
            var nominees = new List<Nominee>();
            if (country == null || !UsPresidentialElection.ElectsIn(country.Id) || FieldOfRecord(pollingDay)) { return nominees; }
            foreach (PoliticalParty party in PartySystems.For(country.Id)) { nominees.Add(NomineeOf(country, pollingDay, party)); }
            return nominees;
        }

        /// <summary>The nominee of the party keyed <paramref name="party"/> on <paramref name="pollingDay"/>; null where the day's field is the record's or the party is
        /// not on the roster.</summary>
        public static Nominee NomineeOf(Country country, DateTime pollingDay, string party)
        {
            if (country == null || !UsPresidentialElection.ElectsIn(country.Id) || FieldOfRecord(pollingDay)) { return null; }
            PoliticalParty? p = PartyOf(country.Id, party);
            return p.HasValue ? NomineeOf(country, pollingDay, p.Value) : null;
        }

        private static Nominee NomineeOf(Country country, DateTime pollingDay, PoliticalParty party)
        {
            string unnamed = UnnamedNominee(party, pollingDay.Year);
            bool president = TryPresidentOn(country, pollingDay, out string incumbent, out string incumbentParty, out string shownAs);
            if (!president || incumbentParty != party.Abbrev)
            {
                return new Nominee { Party = party.Abbrev, Person = unnamed, Default = unnamed, Basis = "the party's candidate, unnamed - the party does not hold the presidency" };
            }
            List<Election> won = ElectionsOf(country, incumbent, pollingDay);
            if (won.Count < MostElections)
            {
                return new Nominee
                {
                    Party = party.Abbrev, Person = incumbent, Default = incumbent,
                    Basis = "the president, who may be elected again - " + (won.Count == 0 ? "never elected" : "elected " + YearsOf(won)),
                };
            }
            var nominee = new Nominee { Party = party.Abbrev, TermLimited = true, Barred = shownAs, BarredBecause = "elected " + YearsOf(won) };
            if (TryVicePresidentOn(country, pollingDay, out string vice, out string viceParty, out string viceBasis) && viceParty == party.Abbrev && Eligible(country, vice, pollingDay))
            {
                nominee.Default = vice;
                nominee.ViceDefault = true;
                nominee.Basis = "the Vice President, " + viceBasis;
            }
            else
            {
                nominee.Default = unnamed;
                nominee.Basis = "the party's own candidate, unnamed - no Vice President of the party may be elected";
            }
            nominee.Person = nominee.Default;
            if (party.Abbrev == country.PlayerPartyAbbrev && TryPickOf(country, pollingDay, party.Abbrev, out string picked) && picked != nominee.Default)
            {
                if (EligibleFor(country, party.Abbrev, pollingDay).Contains(picked))
                {
                    nominee.Person = picked;
                    nominee.Picked = true;
                    nominee.Basis = "the player's pick, in place of the default (" + nominee.Default + ")";
                }
                else { nominee.Basis += "; the player's pick (" + picked + ") is not among the party's people who may be elected, so the default stands"; }
            }
            return nominee;
        }

        /// <summary>
        /// The party's people on <paramref name="pollingDay"/> (see the class - DECLARED), eligible or not: the Vice President in office and the president in office
        /// where of the party, every person on the party's tickets of the game's contests held before the day, and the party's candidate unnamed for the year; in
        /// that order, each once.
        /// </summary>
        public static List<string> PartyPeople(Country country, string party, DateTime pollingDay)
        {
            var people = new List<string>();
            if (country == null || !UsPresidentialElection.ElectsIn(country.Id)) { return people; }
            PoliticalParty? p = PartyOf(country.Id, party);
            if (!p.HasValue) { return people; }
            void Add(string person) { if (!string.IsNullOrEmpty(person) && !people.Contains(person)) { people.Add(person); } }
            if (TryVicePresidentOn(country, pollingDay, out string vice, out string viceParty, out string _) && viceParty == party) { Add(vice); }
            if (TryPresidentOn(country, pollingDay, out string president, out string presidentParty, out string _) && presidentParty == party) { Add(president); }
            if (country.UsPresidentialElections != null)
            {
                var held = new List<UsPresidentialElection.Contest>(country.UsPresidentialElections);
                held.Sort((a, b) => a.PollingDay.CompareTo(b.PollingDay));
                foreach (UsPresidentialElection.Contest c in held)
                {
                    if (c.PollingDay.Date >= pollingDay.Date) { continue; }
                    foreach (UsPresidentialElection.Tally t in c.Electors) { if (t.Party == party) { Add(PersonOf(c, t)); } }
                    if (c.Electors.Exists(t => t.Party == party)) { Add(RunningMateOf(c, party)); }
                }
            }
            Add(UnnamedNominee(p.Value, pollingDay.Year));
            return people;
        }

        /// <summary>The pick's choices for a term-limited party on <paramref name="pollingDay"/>: the party's people who may be elected, the default first; empty
        /// where the party is not term-limited that day.</summary>
        public static List<string> EligibleFor(Country country, string party, DateTime pollingDay)
        {
            var choices = new List<string>();
            if (country == null || !UsPresidentialElection.ElectsIn(country.Id) || FieldOfRecord(pollingDay)) { return choices; }
            if (!TryPresidentOn(country, pollingDay, out string president, out string presidentParty, out string _) || presidentParty != party || Eligible(country, president, pollingDay)) { return choices; }
            PoliticalParty? p = PartyOf(country.Id, party);
            if (!p.HasValue) { return choices; }
            string fallback = TryVicePresidentOn(country, pollingDay, out string vice, out string viceParty, out string _) && viceParty == party && Eligible(country, vice, pollingDay)
                ? vice : UnnamedNominee(p.Value, pollingDay.Year);
            choices.Add(fallback);
            foreach (string person in PartyPeople(country, party, pollingDay)) { if (!choices.Contains(person) && Eligible(country, person, pollingDay)) { choices.Add(person); } }
            return choices;
        }

        /// <summary>The player's pick for a party on a polling day, where one is kept.</summary>
        public static bool TryPickOf(Country country, DateTime pollingDay, string party, out string person)
        {
            person = null;
            if (country?.UsNomineePicks == null) { return false; }
            foreach (NomineePick pick in country.UsNomineePicks)
            {
                if (pick.PollingDay.Date == pollingDay.Date && pick.Party == party) { person = pick.Person; return true; }
            }
            return false;
        }

        /// <summary>
        /// The player picks the nominee of the player's party for <paramref name="pollingDay"/> (R-US6 (b)): only the player's party, only while it is term-limited
        /// that day, only one of its people who may be elected (<see cref="EligibleFor"/>), only for a presidential polling day (`WorldClock.TryNextPollingDay`) and
        /// only before it is counted. Picking the default keeps no pick. False,
        /// with the reason, where the pick is refused; nothing is written then.
        /// </summary>
        public static bool Pick(Country country, DateTime pollingDay, string party, string person, out string refused)
        {
            refused = null;
            if (country == null || !UsPresidentialElection.ElectsIn(country.Id)) { refused = "the country chooses no president by electors"; return false; }
            if (string.IsNullOrEmpty(party) || party != country.PlayerPartyAbbrev) { refused = "only the player's party picks its nominee - an AI party fields its default"; return false; }
            if (!WorldClock.TryNextPollingDay(country.Id, pollingDay.Date, out DateTime day) || day != pollingDay.Date) { refused = "that day is not a presidential polling day"; return false; }
            if (country.UsPresidentialElections != null && country.UsPresidentialElections.Exists(c => c.PollingDay.Date == pollingDay.Date)) { refused = "that polling day is counted"; return false; }
            Nominee nominee = NomineeOf(country, pollingDay, party);
            if (nominee == null) { refused = "the field of that day is the record's"; return false; }
            if (!nominee.TermLimited) { refused = "the party is not term-limited that day - " + nominee.Basis; return false; }
            List<string> choices = EligibleFor(country, party, pollingDay);
            if (!choices.Contains(person)) { refused = (person ?? "no one") + " is not among the party's people who may be elected (" + string.Join("; ", choices) + ")"; return false; }
            if (country.UsNomineePicks == null) { country.UsNomineePicks = new List<NomineePick>(); }
            country.UsNomineePicks.RemoveAll(k => k.PollingDay.Date == pollingDay.Date && k.Party == party);
            if (person != nominee.Default) { country.UsNomineePicks.Add(new NomineePick { PollingDay = pollingDay.Date, Party = party, Person = person }); }
            return true;
        }

        /// <summary>The count's clause for a term-limited party - who cannot be elected again and who stands instead; null where no party is term-limited.</summary>
        public static string ClauseOf(IReadOnlyList<Nominee> nominees)
        {
            if (nominees == null) { return null; }
            var parts = new List<string>();
            foreach (Nominee n in nominees)
            {
                if (!n.TermLimited) { continue; }
                parts.Add(string.Format(CultureInfo.InvariantCulture, "{0}, {1}, cannot be elected again (the Twenty-second Amendment): {2} fields {3} - {4}",
                    n.Barred, n.BarredBecause, n.Party, n.Person, n.Basis));
            }
            return parts.Count == 0 ? null : string.Join("; ", parts);
        }
    }
}
