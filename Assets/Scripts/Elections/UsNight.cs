using System;
using System.Collections.Generic;
using PoliSim.Data;

namespace PoliSim.Elections
{
    /// <summary>
    /// PS-6 US-34 (`docs/specs/USA_STAGE_PLAN.md`, "D-US's night built"; boards 29a-29b, drawn by Code through the Design channel as a PROPOSAL and built
    /// on Elias's go-ahead of 2026-10-23, DECLARED): <b>THE USA'S ELECTION NIGHT, STATE BY STATE</b> - the count the game already holds, declared one
    /// jurisdiction at a time, with three calls.
    /// <list type="bullet">
    /// <item><description><b>Nothing here counts.</b> Every figure is read from the contests the day loop held on the polling day: the presidential count's
    /// states as `UsPresidentialElection.StatesOf` re-derives them (the count's own calls - its electors, Maine's and Nebraska's districts), the Congress
    /// count's House by state (R-US18 (b)) and its Senate seats (R-US19 (c) over (a)). At every jurisdiction declared the night is a sum over what has
    /// declared, so at the last one it IS the count: the College the contest's electors, the House its seats, the Senate the one that sits on 3 January
    /// (`UsNightDiagnostic` fails while any of the three differs).</description></item>
    /// <item><description><b>One declaration lands three verdicts</b> (29b's answer 5): a state's electors, its House delegation and its Senate seat or
    /// seats, because the game counts all three from one night's share.</description></item>
    /// <item><description><b>The order is a presentation choice, not a forecast</b> (29b's For Code 2, the board's own order kept): the safest first and the
    /// closest last, by the distance of the state's REP share of the two-party presidential vote from a half (on a midterm night, its House share); a tie
    /// by postal code. The game holds no poll-closing hours.</description></item>
    /// <item><description><b>The calls</b> (For Code 3): the College for a side once its electors reach a majority of the electors (270 of 538), NO MAJORITY
    /// once neither side can reach it with every elector still to come; the House for a side at a majority of the House (218 of 435); the Senate for a
    /// side at a majority of the seats that sit (51 of 100), the independents counted with the side they caucus with (R-US10 (a): the game seats them by
    /// side), and TIED once neither can pass half - an equal Senate, the Vice President's vote deciding (Art. I §3 cl. 4). A call lands on the first
    /// declaration at which its line is crossed, and never before - it is a fact about the declared count, never a projection.</description></item>
    /// <item><description><b>The Senate not up</b> (For Code 4): the seats the game leaves standing in the Senate that sits from 3 January - the record's by
    /// date with every seat the game elected and seated over them (`UsCongressElection.SeatSides` on that day), less the seats on the ballot tonight - so
    /// the night's full Senate is the one the count seats. DECLARED: read on 3 January, not on the polling day, so a change of record between the two days
    /// to a seat not up is the Senate that sits.</description></item>
    /// <item><description><b>A midterm night</b> (2 U.S.C. §7, US-27) has no presidential count: the House and the Senate class only, the 50 states, no
    /// College. DECLARED - the boards drew the presidential night; a midterm is the same board less its College.</description></item>
    /// </list>
    /// Pure: nothing is written, drawn or read from a clock.
    /// </summary>
    public static class UsNight
    {
        /// <summary>A count's call: still open, a side's, no majority (the College - the House chooses), or tied (the Senate - the Vice President decides).</summary>
        public enum Call { Open, Republican, Democrat, NoMajority, Tied }

        /// <summary>One Senate seat on tonight's ballot, as the count elected it.</summary>
        public sealed class SenateSeat
        {
            public string State;
            public int Class;
            public string Term;
            public string Side;
            public double Share;
        }

        /// <summary>One jurisdiction as the night declares it: its electors and how they went, its House delegation and its Senate seats tonight.</summary>
        public sealed class Jurisdiction
        {
            public string Code;
            /// <summary>The electors it appoints; 0 on a midterm night (no College is counted).</summary>
            public int Electors;
            /// <summary>The statewide winner as a candidate index (`ElectoralCollege.Republican`, `ElectoralCollege.Democrat`); -1 where no presidential count.</summary>
            public int Winner = -1;
            /// <summary>The count's state shares in `ElectoralCollege.ShareDemocrat`'s order (fractions), null where no presidential count.</summary>
            public double[] Shares;
            /// <summary>The statewide REP share of the two nominees' vote; NaN where no presidential count.</summary>
            public double RepTwoParty = double.NaN;
            /// <summary>Maine's and Nebraska's districts, their winners and their DEM shares of the two; empty elsewhere.</summary>
            public int[] Districts = new int[0];
            public int[] DistrictWinners = new int[0];
            public double[] DistrictDemTwoParty = new double[0];
            /// <summary>Every other candidate together led the state (`ElectoralCollege.StateCall`'s flag): its electors still go to the larger nominee.</summary>
            public bool OthersLead;
            public int ElectorsRep, ElectorsDem;
            /// <summary>The House: the seats in force, each side's, the REP share of REP+DEM the count stood on (NaN where it elects none - DC).</summary>
            public int HouseSeats, HouseRep, HouseDem;
            public double HouseShare = double.NaN;
            /// <summary>The Senate seats on tonight's ballot (none, one, or two - Nebraska's class I and class II special in 2024).</summary>
            public List<SenateSeat> SenateUp = new List<SenateSeat>();
            /// <summary>Its place in the night's order, from 1.</summary>
            public int Rank;
            public bool Declared;
            /// <summary>Who won it, by party key: the electors' statewide winner, else (a midterm) its delegation's majority; null on an equal delegation.</summary>
            public string Leader => Winner == ElectoralCollege.Republican ? "REP" : Winner == ElectoralCollege.Democrat ? "DEM" : HouseRep > HouseDem ? "REP" : HouseDem > HouseRep ? "DEM" : null;
        }

        /// <summary>The night at one instant.</summary>
        public sealed class State
        {
            public DateTime PollingDay;
            /// <summary>Whether a presidential count was held tonight (false on a midterm).</summary>
            public bool Presidential;
            /// <summary>The jurisdictions in the night's order.</summary>
            public Jurisdiction[] InOrder;
            public int Declared, Total;
            public bool Complete => Declared >= Total;

            public int Electors, ElectorsMajority, ElectorsRep, ElectorsDem, ElectorsToCome;
            public int HouseSeats, HouseMajority, HouseRep, HouseDem, HouseToCome;
            /// <summary>The Senate that sits: its seats and the majority of them; the seats not up by side (the independents among DEM's counted apart too);
            /// tonight's seats by side and still to come.</summary>
            public int SenateSeats, SenateMajority, NotUpRep, NotUpDem, NotUpIndependents, UpRep, UpDem, UpToCome;
            public int SenateRep => NotUpRep + UpRep;
            public int SenateDem => NotUpDem + UpDem;

            public Call College, House, Senate;
            /// <summary>The jurisdictions declared when each call landed (0 while open).</summary>
            public int CollegeAt, HouseAt, SenateAt;

            /// <summary>Tonight's House by state delegation, over the states declared (the District elects none): REP majorities, DEM majorities, equal - and
            /// the states still to come - the votes the Twelfth Amendment's House would cast (a tied delegation casts none).</summary>
            public int DelegationsRep, DelegationsDem, DelegationsTied, DelegationsToCome;

            public Jurisdiction Find(string code)
            {
                foreach (Jurisdiction j in InOrder) { if (j.Code == code) { return j; } }
                return null;
            }
        }

        /// <summary>The night of <paramref name="congress"/>'s polling day with <paramref name="declared"/> jurisdictions in: the presidential count of the same day
        /// (<paramref name="president"/>, null on a midterm) and the Congress count, in <paramref name="country"/>'s list. Null where there is no Congress count, or
        /// the presidential contest's states cannot be re-derived.</summary>
        public static State At(int declared, Country country, UsPresidentialElection.Contest president, UsCongressElection.Contest congress)
        {
            if (congress == null) { return null; }
            ElectoralCollege.StateCall[] states = null;
            if (president != null)
            {
                states = UsPresidentialElection.StatesOf(country, president);
                if (states == null) { return null; }
            }
            return Of(declared, congress.PollingDay, states, congress, NotUp(country, congress, out int independents), independents);
        }

        /// <summary>The seats not on tonight's ballot in the Senate that sits from <paramref name="congress"/>'s 3 January, by (state, class) and side (see the
        /// class head); <paramref name="independents"/> the DEM-side seats among them an independent holds on the record (`UsSenate.Seat.Party` the letter I; the game seats sides only).</summary>
        public static Dictionary<(string State, int Class), string> NotUp(Country country, UsCongressElection.Contest congress, out int independents)
        {
            independents = 0;
            var up = new HashSet<(string, int)>();
            foreach (UsCongressElection.SenateSeat s in congress.Senate) { up.Add((s.State, s.Class)); }
            var notUp = new Dictionary<(string, int), string>();
            foreach (KeyValuePair<(string State, int Class), string> kv in UsCongressElection.SeatSides(country, congress.Convenes))
            {
                if (!up.Contains(kv.Key)) { notUp[kv.Key] = kv.Value; }
            }
            // the seats the game has elected over the record's: their holder is a side, never a person
            var gameSeats = new HashSet<(string, int)>();
            if (country?.UsCongressElections != null)
            {
                foreach (UsCongressElection.Contest c in country.UsCongressElections)
                {
                    if (c.Convenes.Date <= congress.Convenes.Date) { foreach (UsCongressElection.SenateSeat s in c.Senate) { gameSeats.Add((s.State, s.Class)); } }
                }
            }
            UsSenate.Seating record = UsSenate.On(congress.Convenes);
            if (record.Covered)
            {
                foreach (UsSenate.Seat s in record.Seats)
                {
                    if (s.Party == "I" && s.Side == "DEM" && notUp.TryGetValue((s.State, s.Class), out string side) && side == "DEM" && !gameSeats.Contains((s.State, s.Class))) { independents++; }
                }
            }
            return notUp;
        }

        /// <summary>The night from its parts: the presidential count's states (null on a midterm), the Congress count, the seats not up by side and how many of
        /// DEM's an independent holds. Public so a staged frame (the film's) and the diagnostic can hand it states of their own; the game reads `At`.</summary>
        public static State Of(int declared, DateTime pollingDay, ElectoralCollege.StateCall[] states, UsCongressElection.Contest congress,
            Dictionary<(string State, int Class), string> notUp, int independents)
        {
            var byCode = new Dictionary<string, Jurisdiction>(StringComparer.Ordinal);
            Jurisdiction Get(string code)
            {
                if (!byCode.TryGetValue(code, out Jurisdiction j)) { j = new Jurisdiction { Code = code }; byCode[code] = j; }
                return j;
            }
            if (states != null)
            {
                foreach (ElectoralCollege.StateCall c in states)
                {
                    Jurisdiction j = Get(c.State);
                    j.Electors = c.Electors;
                    j.Winner = c.Winner;
                    j.OthersLead = c.OthersLead;
                    j.Shares = c.Shares;
                    double two = c.Shares[ElectoralCollege.ShareRepublican] + c.Shares[ElectoralCollege.ShareDemocrat];
                    j.RepTwoParty = two > 0.0 ? c.Shares[ElectoralCollege.ShareRepublican] / two : double.NaN;
                    j.Districts = c.Districts ?? new int[0];
                    j.DistrictWinners = c.DistrictWinners ?? new int[0];
                    j.DistrictDemTwoParty = c.DistrictDemocraticTwoParty ?? new double[0];
                    int atLarge = c.Electors - j.Districts.Length;
                    if (c.Winner == ElectoralCollege.Republican) { j.ElectorsRep += atLarge; } else { j.ElectorsDem += atLarge; }
                    foreach (int w in j.DistrictWinners) { if (w == ElectoralCollege.Republican) { j.ElectorsRep++; } else { j.ElectorsDem++; } }
                }
            }
            foreach (UsCongressElection.StateHouse h in congress.House)
            {
                Jurisdiction j = Get(h.State);
                j.HouseSeats = h.Seats;
                j.HouseRep = h.Rep;
                j.HouseDem = h.Dem;
                j.HouseShare = h.Share;
            }
            foreach (UsCongressElection.SenateSeat s in congress.Senate)
            {
                Get(s.State).SenateUp.Add(new SenateSeat { State = s.State, Class = s.Class, Term = s.Term, Side = s.Side, Share = s.Share });
            }

            // the order (For Code 2): the safest first, the closest last; on a midterm night the states the House count holds
            var list = new List<Jurisdiction>();
            foreach (Jurisdiction j in byCode.Values) { if (states != null || j.HouseSeats > 0) { list.Add(j); } }
            double MarginOf(Jurisdiction j)
            {
                double share = states != null ? j.RepTwoParty : j.HouseShare;
                return double.IsNaN(share) ? 0.0 : Math.Abs(share - 0.5);
            }
            list.Sort((a, b) =>
            {
                int byMargin = MarginOf(b).CompareTo(MarginOf(a));
                return byMargin != 0 ? byMargin : string.CompareOrdinal(a.Code, b.Code);
            });

            var night = new State
            {
                PollingDay = pollingDay.Date,
                Presidential = states != null,
                InOrder = list.ToArray(),
                Total = list.Count,
                Declared = Math.Max(0, Math.Min(declared, list.Count)),
            };
            for (int i = 0; i < list.Count; i++) { list[i].Rank = i + 1; list[i].Declared = i < night.Declared; }

            foreach (Jurisdiction j in list) { night.Electors += j.Electors; night.HouseSeats += j.HouseSeats; night.UpToCome += j.SenateUp.Count; }
            night.ElectorsMajority = night.Electors / 2 + 1;
            night.HouseMajority = night.HouseSeats / 2 + 1;
            foreach (string side in notUp.Values) { if (side == "REP") { night.NotUpRep++; } else if (side == "DEM") { night.NotUpDem++; } }
            night.NotUpIndependents = independents;
            night.SenateSeats = notUp.Count + night.UpToCome;
            night.SenateMajority = night.SenateSeats / 2 + 1;
            night.ElectorsToCome = night.Electors;
            night.HouseToCome = night.HouseSeats;
            night.DelegationsToCome = 0;
            foreach (Jurisdiction j in list) { if (j.HouseSeats > 0) { night.DelegationsToCome++; } }

            // the calls, judged after each declaration in turn - each lands on the first at which its line is crossed
            for (int k = 0; k <= night.Declared; k++)
            {
                if (k > 0)
                {
                    Jurisdiction j = list[k - 1];
                    night.ElectorsRep += j.ElectorsRep; night.ElectorsDem += j.ElectorsDem; night.ElectorsToCome -= j.Electors;
                    night.HouseRep += j.HouseRep; night.HouseDem += j.HouseDem; night.HouseToCome -= j.HouseSeats;
                    foreach (SenateSeat s in j.SenateUp) { if (s.Side == "REP") { night.UpRep++; } else { night.UpDem++; } night.UpToCome--; }
                    if (j.HouseSeats > 0)
                    {
                        night.DelegationsToCome--;
                        if (j.HouseRep > j.HouseDem) { night.DelegationsRep++; } else if (j.HouseDem > j.HouseRep) { night.DelegationsDem++; } else { night.DelegationsTied++; }
                    }
                }
                if (night.College == Call.Open && night.Presidential)
                {
                    night.College = CollegeCallOf(night.ElectorsRep, night.ElectorsDem, night.ElectorsToCome, night.ElectorsMajority);
                    if (night.College != Call.Open) { night.CollegeAt = k; }
                }
                if (night.House == Call.Open)
                {
                    night.House = MajorityCallOf(night.HouseRep, night.HouseDem, night.HouseToCome, night.HouseSeats);
                    if (night.House != Call.Open) { night.HouseAt = k; }
                }
                if (night.Senate == Call.Open)
                {
                    night.Senate = MajorityCallOf(night.SenateRep, night.SenateDem, night.UpToCome, night.SenateSeats);
                    if (night.Senate != Call.Open) { night.SenateAt = k; }
                }
            }
            return night;
        }

        /// <summary>One of the dates between a night and the oath, as the Docket lists it: the day, its kind and the words the item and its slip say.</summary>
        public readonly struct OathDate
        {
            public readonly DateTime Day;
            public readonly string Key;
            public readonly string Name;
            public readonly string Line;

            public OathDate(DateTime day, string key, string name, string line) { Day = day; Key = key; Name = name; Line = line; }
        }

        /// <summary>
        /// K38 (Elias, 2026-10-23: "the four oath dates become informational Docket items"; board 29b's ask): THE DATES BETWEEN THE NIGHT AND THE OATH still to
        /// come on <paramref name="today"/>, in date order - the electors' meeting, the new Congress seated, the count before Congress (the House's choice, one
        /// vote a state, where the College had no majority) and the oath - each read from the contest the game held, as the night's "Until the oath" reads them
        /// (`UsPresidentialElection.Contest`'s ElectorsMeet, CountedOn and TakesOffice; `UsCongressElection.Contest.Convenes`), never typed. From the polling
        /// day (the night) until each day has passed, the day itself included. A midterm's night lists the new Congress alone (the night's own "Until the new
        /// Congress"). Information only: none of them holds the clock. Empty where the game has held no US count, or every date has passed.
        /// </summary>
        public static List<OathDate> DatesToTheOath(Country country, DateTime today)
        {
            var dates = new List<OathDate>();
            if (country == null) { return dates; }
            DateTime day = today.Date;
            if (UsPresidentialElection.TryLatest(country, day, out UsPresidentialElection.Contest p) && p.TakesOffice != DateTime.MinValue && p.TakesOffice.Date >= day)
            {
                if (p.ElectorsMeet != DateTime.MinValue && p.ElectorsMeet.Date >= day) { dates.Add(new OathDate(p.ElectorsMeet.Date, "electors", "The electors vote", "In their states · the College's votes are cast")); }
                if (p.CountedOn != DateTime.MinValue && p.CountedOn.Date >= day)
                {
                    dates.Add(p.Contingent
                        ? new OathDate(p.CountedOn.Date, "count", "The House chooses the President", "No College majority · one vote a state")
                        : new OathDate(p.CountedOn.Date, "count", "Congress counts the electoral votes", "A joint session · the count declared"));
                }
                if (p.TakesOffice.Date >= day) { dates.Add(new OathDate(p.TakesOffice.Date, "oath", "The oath", "Noon · the President's term begins")); }
            }
            if (UsCongressElection.TryLatest(country, day, out UsCongressElection.Contest c) && c.Convenes != DateTime.MinValue && c.Convenes.Date >= day)
            {
                dates.Add(new OathDate(c.Convenes.Date, "congress", "The new Congress is seated", "The House and the Senate class elected on " + c.PollingDay.ToString("d MMM yyyy", System.Globalization.CultureInfo.InvariantCulture)));
            }
            dates.Sort((a, b) => a.Day.CompareTo(b.Day));
            return dates;
        }

        /// <summary>The College's call on what has declared: a side at a majority of the electors; no majority once neither can reach it with every elector to
        /// come; else open (the Twelfth Amendment's "a majority of the whole number of Electors appointed").</summary>
        public static Call CollegeCallOf(int rep, int dem, int toCome, int majority) =>
            rep >= majority ? Call.Republican : dem >= majority ? Call.Democrat : rep + toCome < majority && dem + toCome < majority ? Call.NoMajority : Call.Open;

        /// <summary>A chamber's call on what has declared, of <paramref name="seats"/> in all: a side holding more than half; tied once neither can pass half
        /// with every seat to come (only where the seats are even); else open.</summary>
        public static Call MajorityCallOf(int rep, int dem, int toCome, int seats)
        {
            int majority = seats / 2 + 1;
            if (rep >= majority) { return Call.Republican; }
            if (dem >= majority) { return Call.Democrat; }
            return rep + toCome < majority && dem + toCome < majority && seats % 2 == 0 ? Call.Tied : Call.Open;
        }

        /// <summary>The party key a call names, or null.</summary>
        public static string PartyOf(Call call) => call == Call.Republican ? "REP" : call == Call.Democrat ? "DEM" : null;

        // ------------------------------------------------------------------ a staged College with no majority (29a's 269-269 pick)

        /// <summary>
        /// For a STAGED frame only (the film's and the diagnostic's - the game never calls it): the closest states whose statewide electors, called the
        /// other way, leave the College equal - 29a's 269-269 pick, which no uniform swing reaches, staged as the board stages it. The leading side's
        /// statewide winners are taken closest first, and the first set whose electors (the at-large two in Maine and Nebraska, their districts unmoved)
        /// sum to the leader's surplus over half is returned; null where none does or the College is already equal.
        /// </summary>
        public static string[] FlipsToTie(ElectoralCollege.StateCall[] states)
        {
            if (states == null) { return null; }
            int rep = 0, total = 0;
            foreach (ElectoralCollege.StateCall c in states)
            {
                total += c.Electors;
                int atLarge = c.Electors - (c.Districts?.Length ?? 0);
                if (c.Winner == ElectoralCollege.Republican) { rep += atLarge; }
                if (c.DistrictWinners != null) { foreach (int w in c.DistrictWinners) { if (w == ElectoralCollege.Republican) { rep++; } } }
            }
            if (total % 2 != 0) { return null; }
            int leader = rep * 2 > total ? ElectoralCollege.Republican : ElectoralCollege.Democrat;
            int surplus = Math.Abs(rep - total / 2);
            if (surplus == 0) { return null; }
            var candidates = new List<ElectoralCollege.StateCall>();
            foreach (ElectoralCollege.StateCall c in states) { if (c.Winner == leader) { candidates.Add(c); } }
            double Margin(ElectoralCollege.StateCall c)
            {
                double two = c.Shares[ElectoralCollege.ShareRepublican] + c.Shares[ElectoralCollege.ShareDemocrat];
                return two > 0.0 ? Math.Abs(c.Shares[ElectoralCollege.ShareRepublican] / two - 0.5) : 1.0;
            }
            candidates.Sort((a, b) => Margin(a).CompareTo(Margin(b)));
            // the first subset reached for each sum, the states added closest first
            var reached = new Dictionary<int, List<string>> { { 0, new List<string>() } };
            foreach (ElectoralCollege.StateCall c in candidates)
            {
                int moved = c.Electors - (c.Districts?.Length ?? 0);
                var sums = new List<int>(reached.Keys);
                sums.Sort();
                for (int i = sums.Count - 1; i >= 0; i--)
                {
                    int to = sums[i] + moved;
                    if (to > surplus || reached.ContainsKey(to)) { continue; }
                    reached[to] = new List<string>(reached[sums[i]]) { c.State };
                }
                if (reached.ContainsKey(surplus)) { return reached[surplus].ToArray(); }
            }
            return null;
        }

        /// <summary>For a STAGED frame only: <paramref name="states"/> with the statewide calls of <paramref name="flip"/> turned the other way (their two
        /// nominees' shares exchanged, their districts unmoved) - copies; the count's own states are not touched.</summary>
        public static ElectoralCollege.StateCall[] Flipped(ElectoralCollege.StateCall[] states, IReadOnlyCollection<string> flip)
        {
            var copy = new ElectoralCollege.StateCall[states.Length];
            var set = new HashSet<string>(flip ?? new string[0], StringComparer.Ordinal);
            for (int i = 0; i < states.Length; i++)
            {
                ElectoralCollege.StateCall c = states[i];
                var s = new ElectoralCollege.StateCall
                {
                    State = c.State, Electors = c.Electors, Shares = (double[])c.Shares.Clone(), PriorShares = c.PriorShares, Winner = c.Winner, OthersLead = c.OthersLead,
                    Districts = c.Districts, DistrictDemocraticTwoParty = c.DistrictDemocraticTwoParty, DistrictWinners = c.DistrictWinners,
                };
                if (set.Contains(c.State))
                {
                    s.Winner = c.Winner == ElectoralCollege.Republican ? ElectoralCollege.Democrat : ElectoralCollege.Republican;
                    s.Shares[ElectoralCollege.ShareRepublican] = c.Shares[ElectoralCollege.ShareDemocrat];
                    s.Shares[ElectoralCollege.ShareDemocrat] = c.Shares[ElectoralCollege.ShareRepublican];
                }
                copy[i] = s;
            }
            return copy;
        }
    }
}
