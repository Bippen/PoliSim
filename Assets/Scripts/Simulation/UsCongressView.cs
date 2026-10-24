using System;
using System.Collections.Generic;
using PoliSim.Data;
using PoliSim.Elections;

namespace PoliSim.Simulation
{
    /// <summary>
    /// PS-6 US-35 (`docs/specs/USA_STAGE_PLAN.md` US-35; D-US part two's board 30a, `Congress US v3.5.dc.html`, a PROPOSAL Elias read as the go-ahead on
    /// 2026-10-23): <b>THE CONGRESS VIEW, READ FROM THE MODEL.</b> What the Politics page's Congress tab draws for the USA, every figure read from the chambers
    /// the world seats on the day and never typed: who governs (the president of the day and the two chambers' majorities, DIVIDED where the presidency's party
    /// holds fewer than both), the House's seats and whose they are (the record's or the game's, dated), the Senate's seats by side over its three classes -
    /// each class a row of its seats, each seat its side, an independent apart beside the caucus he sits with (R-US10 (a)), each row the record's or the game's
    /// and dated - the lines a vote is measured against as arithmetic on the day's seats (a majority of each chamber, cloture at three fifths of the senators
    /// sworn, an override at two thirds of each chamber voting, the Vice President's vote on an equal division), and the next polling day with what it elects.
    /// Pure: it reads the country and the date and writes nothing; `UsCongressViewDiagnostic` holds every figure to the reads it is made of.
    /// <para><b>What the model does not hold, and so this does not say:</b> a seat's class in the game's world is the catalog's (US-12's roster) and the game's
    /// contests' - a special the game holds is one the catalog schedules (K14, `senate_specials.csv`), drawn edged in brass once seated; a seat the game elected
    /// carries no independent (the count elects a side); and no absence is modelled, so an override's base is the chamber seated (the plate's "of the N voting").</para>
    /// </summary>
    public static class UsCongressView
    {
        /// <summary>One Senate seat on the day: its state and class, the side it is counted for (null where vacant), whether its holder is an independent of record
        /// (counted with his caucus, drawn apart), whether the game elected it, and whether the game elected it to an unexpired term (a special).</summary>
        public sealed class Seat
        {
            public string State;
            public int Class;
            public string Side;
            public bool Independent;
            public bool Game;
            public bool Special;
        }

        /// <summary>One class of the Senate: its seats in the caucus order (`Read`), whether the class's last regular election was the game's, the
        /// polling day it was elected on, and the polling day it is next up.</summary>
        public sealed class ClassRow
        {
            public int Class;
            public readonly List<Seat> Seats = new List<Seat>();
            public bool Game;
            public DateTime ElectedOn;
            public DateTime UpOn;

            public int Of(string side, bool independent)
            {
                int n = 0;
                foreach (Seat s in Seats) { if (s.Side == side && s.Independent == independent) { n++; } }
                return n;
            }

            public int Vacant
            {
                get { int n = 0; foreach (Seat s in Seats) { if (s.Side == null) { n++; } } return n; }
            }
        }

        /// <summary>A line a vote is measured against: its figure, the party measured against it and the seats it holds.</summary>
        public sealed class Line
        {
            public string Name;
            public int Figure;
            public string Holder;
            public int Held;
            /// <summary>The review's L2: on an equal division the Vice President's party holds the Senate with half - the line is held, not one short.</summary>
            public bool ByVicePresident;
            public bool Holds => Held >= Figure || ByVicePresident;
            public int Short => Holds ? 0 : Math.Max(0, Figure - Held);
        }

        /// <summary>The view on one day.</summary>
        public sealed class View
        {
            public DateTime On;

            // who governs
            public string President, PresidentsParty;
            public bool PresidentOfGame;
            public DateTime PresidentUntil;
            public string VicePresident, VicePresidentParty;

            // the House
            public IReadOnlyDictionary<string, int> House;
            public int HouseSeated;
            public string HouseMajority;
            public int HouseMajoritySeats;
            public bool HouseOfGame;
            public DateTime HouseElectedOn, HouseSeatedOn;
            /// <summary>The day a House the game has already elected is seated, where it is still ahead (the old House sits until then); MinValue where none is.</summary>
            public DateTime HouseSitsUntil;

            // the Senate
            public IReadOnlyDictionary<string, int> Senate;
            public int SenateSworn;
            public string SenateMajority;
            public int SenateMajoritySeats;
            /// <summary>The independents in each caucus on the day (the record's - a seat the game elected carries none).</summary>
            public readonly Dictionary<string, int> SenateIndependents = new Dictionary<string, int>();
            public readonly List<ClassRow> Classes = new List<ClassRow>();

            // the lines
            public Line HouseMajorityLine, SenateMajorityLine, Cloture, HouseOverride, SenateOverride;

            // divided government
            public bool HouseDivided, SenateDivided;
            public bool Divided => HouseDivided || SenateDivided;

            // the next vote
            public DateTime NextVote;
            public bool NextMidterm;
            public int NextClass;
            public int NextSenateSeats;

            public int IndependentsIn(string side) => side != null && SenateIndependents.TryGetValue(side, out int n) ? n : 0;
        }

        /// <summary>Whether the view is drawn for <paramref name="country"/>: the USA with its Congress seated (`Congress.Applies`).</summary>
        public static bool Applies(Country country) => Congress.Applies(country);

        /// <summary>The view of <paramref name="country"/>'s Congress at the end of <paramref name="today"/>; null where `Applies` is false.</summary>
        public static View Read(Country country, DateTime today)
        {
            if (!Applies(country)) { return null; }
            DateTime day = today.Date;
            var v = new View { On = day };

            // ---- who governs: the president of the day (the game's sworn winner, else the record's by date) and the term's end (the Twentieth Amendment's noon of 20 January)
            if (Congress.PresidentOn(country, day, out string president, out string party, out _))
            {
                v.President = president;
                v.PresidentsParty = party;
            }
            v.PresidentOfGame = UsPresidentialElection.TryInOffice(country, day, out UsPresidentialElection.Contest sworn);
            v.PresidentUntil = v.PresidentOfGame ? UsPresidentialElection.OathDayAfter(UsPresidentialElection.PresidentialDay(sworn.PollingDay.Year + 4)) : TermEndAfter(day);
            if (UsTermLimit.TryVicePresidentOn(country, day, out string vice, out string viceParty, out _))
            {
                v.VicePresident = vice;
                v.VicePresidentParty = viceParty;
            }

            // ---- the House: the chamber the world seats, its majority, and whose it is
            v.House = country.ParliamentSeats;
            v.HouseSeated = Congress.Sworn(country.ParliamentSeats);
            v.HouseMajority = Congress.HouseMajority(country);
            v.HouseMajoritySeats = SeatsOf(v.House, v.HouseMajority);
            if (UsCongressElection.TryInOffice(country, day, out UsCongressElection.Contest elected))
            {
                v.HouseOfGame = true;
                v.HouseElectedOn = elected.PollingDay.Date;
                v.HouseSeatedOn = elected.Convenes.Date;
            }
            else
            {
                // the record's House by date - none where the record holds no chamber that day (a gap), and the view then says no date
                try
                {
                    WorldClock.ChamberOfRecord record = WorldClock.ChamberAt(CountryId.USA, day);
                    v.HouseElectedOn = record.ElectionDay;
                    v.HouseSeatedOn = record.Convened;
                }
                catch (InvalidOperationException) { v.HouseElectedOn = v.HouseSeatedOn = DateTime.MinValue; }
            }
            v.HouseSitsUntil = UsCongressElection.TryLatest(country, day, out UsCongressElection.Contest latest) && latest.Convenes.Date > day ? latest.Convenes.Date : DateTime.MinValue;

            // ---- the Senate: the chamber the world seats, by side, its majority (the Vice President's on an equal division), and its classes
            v.Senate = country.SecondChamberSeats;
            v.SenateSworn = Congress.Sworn(country.SecondChamberSeats);
            v.SenateMajority = Congress.SenateMajority(country, day);
            v.SenateMajoritySeats = SeatsOf(v.Senate, v.SenateMajority);
            ReadClasses(country, day, v);
            foreach (ClassRow row in v.Classes)
            {
                foreach (Seat s in row.Seats)
                {
                    if (s.Independent && s.Side != null) { v.SenateIndependents[s.Side] = (v.SenateIndependents.TryGetValue(s.Side, out int n) ? n : 0) + 1; }
                }
            }

            // ---- the lines: arithmetic on the day's seats, never a forecast
            v.HouseMajorityLine = new Line { Name = "Majority · House", Figure = v.HouseSeated / 2 + 1, Holder = v.HouseMajority, Held = v.HouseMajoritySeats };
            v.SenateMajorityLine = new Line { Name = "Majority · Senate", Figure = v.SenateSworn / 2 + 1, Holder = v.SenateMajority, Held = v.SenateMajoritySeats, ByVicePresident = v.SenateMajority != null && UsSenate.EquallyDivided(country.SecondChamberSeats) };
            v.Cloture = new Line { Name = "Cloture", Figure = Congress.ClotureLine(v.SenateSworn), Holder = v.SenateMajority, Held = v.SenateMajoritySeats };
            v.HouseOverride = new Line { Name = "Override · House", Figure = OverrideLine(v.HouseSeated), Holder = v.HouseMajority, Held = v.HouseMajoritySeats };
            v.SenateOverride = new Line { Name = "Override · Senate", Figure = OverrideLine(v.SenateSworn), Holder = v.SenateMajority, Held = v.SenateMajoritySeats };

            // ---- divided government: the presidency's party holds fewer than both chambers
            if (!string.IsNullOrEmpty(v.PresidentsParty))
            {
                v.HouseDivided = v.HouseMajority != null && v.HouseMajority != v.PresidentsParty;
                v.SenateDivided = v.SenateMajority != null && v.SenateMajority != v.PresidentsParty;
            }

            // ---- the next vote
            if (WorldClock.TryNextPollingDay(CountryId.USA, day, out DateTime next))
            {
                v.NextVote = next;
                v.NextMidterm = UsCongressElection.IsMidterm(next);
                v.NextClass = UsCongressElection.ClassUp(next.Year);
                v.NextSenateSeats = UsCongressElection.SeatsUp(next).Count;
            }
            return v;
        }

        /// <summary>The votes an override needs in a chamber of <paramref name="voting"/> members voting: two thirds of those voting, a quorum present - the
        /// least YEA with YEA &gt;= 2 (voting - YEA), the count `Congress.OverrideVote.Required` holds.</summary>
        public static int OverrideLine(int voting) => voting <= 0 ? 0 : (2 * voting + 2) / 3;

        /// <summary>The end of a president of record's term after <paramref name="day"/>: noon on the first 20 January after it that follows a presidential year
        /// (the Twentieth Amendment §1).</summary>
        public static DateTime TermEndAfter(DateTime day)
        {
            for (int y = day.Year; y <= day.Year + 4; y++)
            {
                var oath = new DateTime(y, 1, 20);
                if (oath > day && UsPresidentialElection.IsPresidentialYear(y - 1)) { return oath; }
            }
            return DateTime.MinValue;
        }

        private static int SeatsOf(IReadOnlyDictionary<string, int> seats, string party) => seats != null && party != null && seats.TryGetValue(party, out int n) ? n : 0;

        /// <summary>
        /// The three classes on the day: every (state, class) the catalog's roster holds (US-12's `senate_seats.csv` - two senators from each state, in three
        /// classes), each seat's side as the world seats it (`UsCongressElection.SeatSides`: the record's by date, the game's elected seats over it; a vacant seat
        /// none), the record's independents apart (their letter on the day, `UsSenate.On`), and the seats the game elected marked - a special where it filled an
        /// unexpired term. Each class is the game's from the 3 January after the game's first count of it, else the record's, elected on the last polling day of
        /// its year in the cycle whose Congress has convened (`UsCongressElection.ClassUp`); it is next up six years after.
        /// </summary>
        private static void ReadClasses(Country country, DateTime day, View v)
        {
            Dictionary<(string State, int Class), string> sides = UsCongressElection.SeatSides(country, day);
            var independents = new HashSet<(string, int)>();
            UsSenate.Seating record = UsSenate.On(day);
            foreach (UsSenate.Seat s in record.Seats) { if (s.Party == "I") { independents.Add((s.State, s.Class)); } }
            var gameSeats = new Dictionary<(string, int), UsCongressElection.SenateSeat>();
            var gameClassDay = new Dictionary<int, DateTime>();
            if (country.UsCongressElections != null)
            {
                var seated = new List<UsCongressElection.Contest>();
                foreach (UsCongressElection.Contest c in country.UsCongressElections) { if (c.Convenes.Date <= day) { seated.Add(c); } }
                seated.Sort((a, b) => a.Convenes.CompareTo(b.Convenes));
                foreach (UsCongressElection.Contest c in seated)
                {
                    foreach (UsCongressElection.SenateSeat s in c.Senate) { gameSeats[(s.State, s.Class)] = s; }
                    if (c.SenateClass > 0) { gameClassDay[c.SenateClass] = c.PollingDay.Date; }
                }
            }
            var roster = new SortedSet<(string State, int Class)>();
            foreach (var row in Elections.Generated.UsPresidentialReturns.SenateSeats) { roster.Add((row.State, row.Class)); }
            string first = v.SenateMajority ?? "DEM";
            for (int cls = 1; cls <= UsSenate.Classes; cls++)
            {
                var row = new ClassRow { Class = cls };
                foreach ((string state, int c) in roster)
                {
                    if (c != cls) { continue; }
                    sides.TryGetValue((state, c), out string side);
                    bool game = gameSeats.TryGetValue((state, c), out UsCongressElection.SenateSeat elected);
                    row.Seats.Add(new Seat
                    {
                        State = state, Class = c, Side = side, Game = game, Special = game && elected.Term != "full",
                        Independent = !game && side != null && independents.Contains((state, c)),
                    });
                }
                // the caucus order: the majority's party, its independents, the other side's independents, the other side, the vacant
                int Rank(Seat s) => s.Side == null ? 4 : s.Side == first ? (s.Independent ? 1 : 0) : (s.Independent ? 2 : 3);
                row.Seats.Sort((a, b) => Rank(a) != Rank(b) ? Rank(a).CompareTo(Rank(b)) : string.CompareOrdinal(a.State, b.State));
                if (gameClassDay.TryGetValue(cls, out DateTime gameDay))
                {
                    row.Game = true;
                    row.ElectedOn = gameDay;
                }
                else
                {
                    for (int y = day.Year; y >= day.Year - 8; y--)
                    {
                        DateTime polling = UsPresidentialElection.PresidentialDay(y);
                        if (y % 2 == 0 && UsCongressElection.ClassUp(y) == cls && UsCongressElection.ConvenesAfter(polling) <= day) { row.ElectedOn = polling; break; }
                    }
                }
                row.UpOn = row.ElectedOn == DateTime.MinValue ? DateTime.MinValue : UsPresidentialElection.PresidentialDay(row.ElectedOn.Year + 6);
                v.Classes.Add(row);
            }
        }
    }
}
