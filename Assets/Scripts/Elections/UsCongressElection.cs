using System;
using System.Collections.Generic;
using System.Globalization;
using PoliSim.Data;

namespace PoliSim.Elections
{
    /// <summary>
    /// PS-6 US-16 (`docs/specs/USA_STAGE_PLAN.md`, "The House and a Senate class elected on the same day" - checkpoint 2): THE GAME ELECTS CONGRESS. On the
    /// presidential polling day (3 U.S.C. §21(1); the House's day by 2 U.S.C. §7, the Senate's by 2 U.S.C. §1 - the same Tuesday) the day loop's count
    /// (`SimulationManager.HoldUsPresidentialCount`) also elects the House and the Senate class whose terms expire; both are seated at noon on 3 January
    /// (the Twentieth Amendment §2, records_by_date.md - "noon" is a date, US-2's premise). Every answer here is a pure function of the country's state, the
    /// day and the poll, whoever the player is (R-US5); the day loop holds it for the player's country only, as it holds the presidential count.
    /// <list type="bullet">
    /// <item><description><b>The House - R-US18 RULED (b)</b> (Elias's ruling I1, `COMPLETED.md` §856): state delegations by a state swing, the block's
    /// arithmetic as `UsHouseCountCheck` measured it (its <c>CountBState</c>, kind "b"): per state, the base's REP share u of REP+DEM over its districts with
    /// votes (the deciding count, `house_districts.csv`'s final_r / final_d; an unopposed district printed without votes is a U seat, held by its party),
    /// moved by the national swing Delta = V(T) - V(B), unclamped; one seat by the sign of uhat - 0.5 (the base winner's party at exactly a half); two or
    /// more anchored on the base's own seats: REP = min(n_T - U_D, max(U_R, R_B + P(c_T, uhat) - P(c_B, u))), P the nearest integer to c times the share
    /// clamped to [0, 1], a half by the half rule (the party that held more of the state's base seats, then the national base majority). V(T) is the
    /// game's poll on the day, REP / (REP + DEM), before E1's factors (R-US15 (a)); V(B) US-5's own-line House vote of the base year (G1: the swing's basis).
    /// B is the record's latest House election before the day (2022 for 5 Nov 2024), or the game's own latest House count where it holds one - its stored
    /// shares, delegations and U seats, so that every share is its base plus the summed swings and rolling forward is exact (US-11's *In play*; the
    /// midterms between are US-27's, the roll-forward's general form US-26's). n_T is the apportionment in force (2 U.S.C. 2a: the catalog's `house_maps.csv`
    /// seats for the year, the latest read carried forward - R-US13 (a)); 435 asserted on every count. (b) reads no district lines, so I1's DECLARED reading
    /// ("a map redrawn since the base enters once an election has been held on it") has nothing to act on in its arithmetic: a redraw moves no state's
    /// seats, and the base is the state's whole vote whatever its lines.</description></item>
    /// <item><description><b>The Senate - R-US19 RULED (c) over (a)</b> (Elias's ruling I2, §856): each seat up that day takes its state's share from the
    /// presidential count that night (R-US14 (a)'s derivation, the night's map - <see cref="UsPresidentialElection.StatesOf"/>), REP / (REP + DEM); on the
    /// field of record (5 Nov 2024) each race carries its factor c_j = logit s - logit p (E1's form), fitted on 2024's class only - the 33 regular Class I
    /// races and Nebraska's Class II special, s the race's base count (final_r / (final_r + final_dc), final_none_top standing for a side with no candidate,
    /// DECLARED by the block) and p the state's share derived from 2020 at 2024's true national shares, both clamped to [1e-9, 1 - 1e-9] - so a world whose
    /// vote follows history reproduces 2024's class; every later field at 1.0 (c_j = 0). A call is REP above a half and DEM below; at exactly a half the
    /// block's cascade - the holder's side at the end of the polling day, else the side holding the Senate majority that day with the Vice President's vote,
    /// else (where the block's instrument fails) a seeded draw (US-8c's, never in one party's favour - DECLARED). The class up is the one the catalog's races
    /// of record elect in the same year of a six-year cycle (Art. I §3 cl. 2); the specials are the catalog's races of the day to another class (Nebraska's
    /// Class II in 2024) - sourced, else none (G6: BILLED where a special of record is not read; no later special is on record). California's same-seat,
    /// same-day special is the full-term seat's (the block: never counted apart).</description></item>
    /// <item><description><b>Seated at noon on 3 January.</b> From <see cref="Contest.Convenes"/> the game's House holds every seat (REP the count, DEM the
    /// rest) and the game's Senate seats replace the record's for the seats it elected; the seats it did not elect stay the record's, by date (R-US1 (a):
    /// "whatever the game does not elect is seated on its record's dates") - so US-2's Congress of record retires chamber by chamber, seat by seat in the
    /// Senate, as the game elects them (`WorldClock.RecordSeatsChamber(Country, DateTime)`).</description></item>
    /// <item><description><b>PS-6 US-27 - the midterms</b> (`docs/specs/USA_STAGE_PLAN.md` US-27; 2 U.S.C. §7, [USC-7]: "The Tuesday next after the 1st Monday
    /// in November, in every even numbered year"): on a polling day between two presidential years (<see cref="IsMidterm"/>) the day loop counts the House and
    /// the class whose terms expire, and no president (<see cref="CountMidterm"/>). The midterm is R-US20 RULED (b) (Elias's ruling I4, `COMPLETED.md` §856):
    /// a FITTED term on the president's party (<see cref="MidtermTermPoints"/>, `Generated.UsMidtermTerm` - the mean midterm swing of the record since 1946
    /// with n and its interval), added to the poll's two-party V before R-US18 (b)'s count, so its seat change is the count's own; the base the House swings
    /// from as on every count (the game's own latest House count, else the record's latest). DECLARED (put to Elias): the term moves the day's one national
    /// vote, so the Senate class reads it too - each state's share derived from the presidential derivation at that vote (R-US19 (a)'s "presidential-derived
    /// share", with no presidential count that night: the game's own previous presidential states where it has counted one, R-US24 (b)'s form, else the
    /// record's latest); the president's party is the party of the president governing on the polling day. The midterm's own result is BILLED until the
    /// Clerk's statistics of its year are saved - never estimated.</description></item>
    /// </list>
    /// Not modelled, stated: third-party winners (two sides, R-US10 (a)); runoffs, ranked-choice transfers and Georgia's majority rule; specials the record
    /// does not hold (2026's Ohio and Florida specials for the seats vacated in January 2025 are not in the catalog - their appointees of record stand, G6);
    /// party switches; a delayed oath (every winner sits from 3 January, R-US18's NY-22 convention); the Senate's own vacancies after the record's reach.
    /// </summary>
    public static class UsCongressElection
    {
        /// <summary>2 U.S.C. 2a's House: the apportionment's total, asserted against the catalog's `house_maps.csv` seats on every count (R-US13 (a)) - SOURCED
        /// ([HH-DIV], every row of the record 2016-2024 sums to it).</summary>
        public const int HouseMembers = 435;

        /// <summary>The block's clamp on a logit's argument (R-US19 STEP 3: "both clamped to [1e-9, 1 - 1e-9]") - a bound, CONVENTION.</summary>
        private const double LogitClamp = 1e-9;

        /// <summary>One state's House count: its seats in force, the seats each party won, the REP share of REP+DEM the count stood on (uhat, unclamped - the
        /// next count's base share) and the base's unopposed seats by party, carried forward (U_R, U_D).</summary>
        public sealed class StateHouse
        {
            public string State;
            public int Seats;
            public int Rep;
            public int Dem;
            public double Share;
            public int UnopposedRep;
            public int UnopposedDem;
        }

        /// <summary>One Senate seat the game elected: the state, the class, the term ("full", or "unexpired" for a special), the night's state share (the
        /// parent, (a)), the race's factor c_j (0 on a later field), the share the call reads, the side elected and, where the share was exactly a half, the
        /// cascade's words.</summary>
        public sealed class SenateSeat
        {
            public string State;
            public int Class;
            public string Term;
            public double Parent;
            public double Factor;
            public double Share;
            public string Side;
            public string Cascade;
        }

        /// <summary>One Congress election the game held - kept with the country (<see cref="Country.UsCongressElections"/>), so a save carries it (format 49;
        /// PS-6 US-27's fields, format 52).</summary>
        public sealed class Contest
        {
            /// <summary>The polling day - every even year's (2 U.S.C. §§1 and 7), a presidential year's the presidential day (3 U.S.C. §21(1)) too.</summary>
            public DateTime PollingDay;
            /// <summary>The day the new Congress is seated: noon on the 3rd of January after the polling day (the Twentieth Amendment §2).</summary>
            public DateTime Convenes;
            /// <summary>V(T): the REP share of REP+DEM the House count stood on, before E1's factors (R-US15 (a)) - the poll's in a presidential year; PS-6 US-27: in
            /// a midterm the poll's moved by the midterm term (<see cref="PollV"/>, <see cref="TermPoints"/>). The next count's base and the history rolled
            /// forward read it (US-26).</summary>
            public double V;
            /// <summary>PS-6 US-27: the poll's own REP share of REP+DEM on the day, before the midterm term (equal to <see cref="V"/> in a presidential year).</summary>
            public double PollV;
            /// <summary>PS-6 US-27: the midterm term applied, in points of the two-party vote on the president's party (<see cref="MidtermTermPoints"/> on the day;
            /// 0 in a presidential year), and that party (null in a presidential year, or where no president governed).</summary>
            public double TermPoints;
            public string PresidentsParty;
            /// <summary>V(B) and B: the base House election swung from - the record's (US-5's own-line vote of its year) or the game's own count.</summary>
            public double VBase;
            public int BaseYear;
            public bool BaseOfRecord;
            /// <summary>The House by state, in the catalog's state order, and its totals.</summary>
            public List<StateHouse> House = new List<StateHouse>();
            public int HouseRep;
            public int HouseDem;
            /// <summary>The Senate class up that day (1, 2 or 3) and every seat the game elected, the specials included.</summary>
            public int SenateClass;
            public List<SenateSeat> Senate = new List<SenateSeat>();
            /// <summary>The count's verdict in a line.</summary>
            public string Line;

            /// <summary>The House's seats by party key for a country's roster: REP the count, DEM the rest, every other roster party 0.</summary>
            public Dictionary<string, int> HouseSeats(CountryId id)
            {
                var seats = new Dictionary<string, int>();
                foreach (PoliticalParty p in PartySystems.For(id)) { seats[p.Abbrev] = p.Abbrev == "REP" ? HouseRep : p.Abbrev == "DEM" ? HouseDem : 0; }
                return seats;
            }

            /// <summary>The House's delegations by state - the Twelfth Amendment's votes read them (R-US3 (a)).</summary>
            public List<(string State, int Republicans, int Democrats)> Delegations()
            {
                var d = new List<(string, int, int)>();
                foreach (StateHouse s in House) { d.Add((s.State, s.Rep, s.Dem)); }
                d.Sort((a, b) => string.CompareOrdinal(a.Item1, b.Item1));
                return d;
            }

            /// <summary>The Senate seats it elected, by side.</summary>
            public int SenateOf(string side)
            {
                int n = 0;
                foreach (SenateSeat s in Senate) { if (s.Side == side) { n++; } }
                return n;
            }
        }

        /// <summary>The countries whose Congress the game elects - the USA alone (PS-8 owns an AI USA's row).</summary>
        public static bool ElectsIn(CountryId id) => id == CountryId.USA;

        /// <summary>The Twentieth Amendment §2 (`archives_amendments_11-27.html`, [EX-AM]): "such meeting shall begin at noon on the 3d day of January" - the
        /// Congress elected on <paramref name="pollingDay"/> is seated on the 3rd of January after it (US-2's House of record convenes on the same day).</summary>
        public static DateTime ConvenesAfter(DateTime pollingDay) => new DateTime(pollingDay.Year + 1, 1, 3);

        /// <summary>PS-6 US-27: whether <paramref name="day"/> is a midterm polling day - 2 U.S.C. §7's Tuesday of an even year that elects no president
        /// (<see cref="UsPresidentialElection.IsPresidentialYear"/>).</summary>
        public static bool IsMidterm(DateTime day) =>
            day.Year % 2 == 0 && !UsPresidentialElection.IsPresidentialYear(day.Year) && UsPresidentialElection.PresidentialDay(day.Year) == day.Date;

        /// <summary>PS-6 US-27 - R-US20 RULED (b) (Elias's ruling I4): the midterm term on the president's party, in points of the two-party House vote - FITTED,
        /// `Generated.UsMidtermTerm`'s mean (written by `Tools/us_midterm_fit.pl` from US-13's record; `UsMidtermFitCheck` refits it), never typed.</summary>
        public static double MidtermTermPoints => Generated.UsMidtermTerm.MeanPoints;

        /// <summary>PS-6 US-27 (DECLARED): the president's party on a polling day - the party of the presidency governing that day (the record's or the game's
        /// president, an acting one included); null where no presidency governs.</summary>
        public static string PresidentsPartyOn(Country country) =>
            country?.Government != null && country.Government.Kind == WorldClock.ExecutiveKind.Presidency ? country.Government.PmParty : null;

        /// <summary>PS-6 US-27: a REP share of REP+DEM <paramref name="v"/> moved by <paramref name="termPoints"/> points on <paramref name="presidentsParty"/>
        /// (its own share up by the term, the other's down by it); unmoved where that party is neither REP nor DEM.</summary>
        public static double WithMidtermTerm(double v, string presidentsParty, double termPoints) =>
            presidentsParty == "REP" ? v + termPoints / 100.0 : presidentsParty == "DEM" ? v - termPoints / 100.0 : v;

        /// <summary>PS-6 US-27: what a US polling day counts, in the view's words - the president, the House and the Senate class in a presidential year; the House
        /// and the class in a midterm, no president.</summary>
        public static string ContestsWords(DateTime pollingDay)
        {
            int cls = ClassUp(pollingDay.Year);
            string senate = cls > 0 ? "THE SENATE'S CLASS " + cls.ToString(CultureInfo.InvariantCulture) : "A SENATE CLASS";
            return IsMidterm(pollingDay) ? "A MIDTERM: THE HOUSE AND " + senate + ", NO PRESIDENT" : "THE PRESIDENT, THE HOUSE AND " + senate;
        }

        /// <summary>
        /// PS-6 US-27 (the second review's M1; MEASUREMENT ONLY - nothing counted reads it): the government's record in the midterm's poll split by component, in
        /// points of V (REP's share of REP+DEM) - each component alone against the poll with no record: the economic vote, q15's approval term and q13's fiscal
        /// term (`EconomicVote.RecordPartsOverTerm`), and all three together (the poll the count reads, before the fitted term). Printed beside the fitted term so
        /// the overlap I4's revisit clause governs ("Revisit against (c) once approval reaches the vote") is measured on every run. False where no prediction can be made.
        /// </summary>
        public static bool TryRecordInV(Country country, DateTime day, out double economy, out double approval, out double fiscal, out double total)
        {
            economy = approval = fiscal = total = double.NaN;
            if (country == null) { return false; }
            EconomicVote.RecordParts parts = EconomicVote.RecordPartsOverTerm(country, day);
            double V(IReadOnlyDictionary<string, double> shift) =>
                NationalElection.TryPredictShares(country.Id, out Dictionary<string, double> poll, shift, on: day, played: country) ? TwoPartyV(poll) : double.NaN;
            double none = V(null);
            if (double.IsNaN(none)) { return false; }
            economy = 100.0 * (V(parts.Economy) - none);
            approval = 100.0 * (V(parts.Approval) - none);
            fiscal = 100.0 * (V(parts.Fiscal) - none);
            total = 100.0 * (V(parts.Total) - none);
            return true;
        }

        /// <summary>The words of <see cref="TryRecordInV"/> beside the fitted term, for the midterm's CONGRESS line and the diagnostic.</summary>
        public static string RecordInVWords(Country country, DateTime day, string presidentsParty, double termPoints)
        {
            if (!TryRecordInV(country, day, out double e, out double a, out double f, out double t)) { return "the record's components in the poll: no prediction"; }
            return string.Format(CultureInfo.InvariantCulture,
                "the record in the poll, in points of REP's two-party V: economy {0:+0.0000;-0.0000}, approval {1:+0.0000;-0.0000}, fiscal {2:+0.0000;-0.0000}, together {3:+0.0000;-0.0000}; beside the fitted term {4:+0.0000;-0.0000} on {5} (I4's revisit clause: the term is the record's unconditional mean swing)",
                e, a, f, t, presidentsParty == "DEM" ? -termPoints : termPoints, presidentsParty ?? "no president's party");
        }

        /// <summary>Whether the game has held a Congress election in this country on or before <paramref name="date"/>.</summary>
        public static bool Held(Country country, DateTime date)
        {
            if (country?.UsCongressElections == null) { return false; }
            foreach (Contest c in country.UsCongressElections) { if (c.PollingDay.Date <= date.Date) { return true; } }
            return false;
        }

        /// <summary>The Congress the game elected that sits on <paramref name="date"/>: the latest contest seated by then; false where none has been seated.</summary>
        public static bool TryInOffice(Country country, DateTime date, out Contest contest)
        {
            contest = null;
            if (country?.UsCongressElections == null) { return false; }
            foreach (Contest c in country.UsCongressElections) { if (c.Convenes.Date <= date.Date && (contest == null || c.Convenes > contest.Convenes)) { contest = c; } }
            return contest != null;
        }

        /// <summary>The latest Congress election the game held on or before <paramref name="date"/>, seated or not.</summary>
        public static bool TryLatest(Country country, DateTime date, out Contest contest)
        {
            contest = null;
            if (country?.UsCongressElections == null) { return false; }
            foreach (Contest c in country.UsCongressElections) { if (c.PollingDay.Date <= date.Date && (contest == null || c.PollingDay > contest.PollingDay)) { contest = c; } }
            return contest != null;
        }

        /// <summary>V: a poll's REP share of REP+DEM (R-US15 (a): the poll's own measure); NaN where either is missing or not positive.</summary>
        public static double TwoPartyV(IReadOnlyDictionary<string, double> poll)
        {
            if (poll == null || !poll.TryGetValue("REP", out double r) || !poll.TryGetValue("DEM", out double d) || double.IsNaN(r) || double.IsNaN(d) || r <= 0.0 || d <= 0.0) { return double.NaN; }
            return r / (r + d);
        }

        /// <summary>V(E) of the record: US-5's own-line House vote, REP / (REP + DEM) over `house_by_state.csv`'s rows of <paramref name="year"/> (G1) -
        /// the one reader `UsHouseCountCheck` and `UsSenateRaceCheck` share; NaN where the catalog holds none.</summary>
        public static double RecordV(int year)
        {
            long r = 0, d = 0;
            foreach (var h in Generated.UsPresidentialReturns.House) { if (h.Year == year) { r += h.VotesR; d += h.VotesD; } }
            return r + d > 0 ? (double)r / (r + d) : double.NaN;
        }

        /// <summary>The record's latest House election before <paramref name="pollingDay"/> - the catalog's latest year of district returns before it; 0 where none.</summary>
        public static int RecordBaseYear(DateTime pollingDay)
        {
            int year = 0;
            foreach (var h in Generated.UsPresidentialReturns.HouseDistricts) { if (h.Year < pollingDay.Year && h.Year > year) { year = h.Year; } }
            return year;
        }

        /// <summary>Each state's seats in force for an election in <paramref name="year"/>: `house_maps.csv`'s rows of the latest year read at or before it
        /// (R-US13 (a): the 2020 census's apportionment carried forward). Empty where the catalog holds none.</summary>
        public static SortedDictionary<string, int> SeatsInForce(int year)
        {
            int read = 0;
            foreach (var m in Generated.UsPresidentialReturns.HouseMaps) { if (m.Year <= year && m.Year > read) { read = m.Year; } }
            var seats = new SortedDictionary<string, int>(StringComparer.Ordinal);
            foreach (var m in Generated.UsPresidentialReturns.HouseMaps) { if (m.Year == read) { seats[m.State] = m.Seats; } }
            return seats;
        }

        /// <summary>One state's base as R-US18 (b) reads it: u, U_R, U_D, the decided seats m_B, REP's R_B, the base's seats by party for the half rule, and
        /// a one-seat base's winner.</summary>
        private sealed class StateBase
        {
            public double U;
            public int UR, UD, MB, RB, DB;
            public bool OneSeatRep;
        }

        private static Dictionary<string, StateBase> RecordBases(int year)
        {
            var bases = new Dictionary<string, StateBase>(StringComparer.Ordinal);
            var votes = new Dictionary<string, (long R, long D, bool Any, int Count, string First)>(StringComparer.Ordinal);
            foreach (var x in Generated.UsPresidentialReturns.HouseDistricts)
            {
                if (x.Year != year) { continue; }
                if (!bases.TryGetValue(x.State, out StateBase b)) { bases[x.State] = b = new StateBase(); }
                bool unopposed = x.Flags != null && x.Flags.Contains("U");
                if (unopposed) { if (x.Winner == "R") { b.UR++; } else if (x.Winner == "D") { b.UD++; } }
                if (x.Winner == "R") { b.MB++; b.RB++; } else if (x.Winner == "D") { b.MB++; b.DB++; }
                votes.TryGetValue(x.State, out var v);
                if (!unopposed && x.FinalR + x.FinalD > 0) { v.R += x.FinalR; v.D += x.FinalD; v.Any = true; }
                v.Count++;
                if (v.Count == 1) { v.First = x.Winner; }
                votes[x.State] = v;
            }
            foreach (KeyValuePair<string, StateBase> kv in bases)
            {
                var v = votes[kv.Key];
                StateBase b = kv.Value;
                int cB = b.MB - b.UR - b.UD;
                b.U = (cB == 0 || !v.Any) ? (double)b.RB / b.MB : (double)v.R / (v.R + v.D);
                b.OneSeatRep = v.Count == 1 ? v.First == "R" : b.RB * 2 > b.MB;
            }
            return bases;
        }

        private static Dictionary<string, StateBase> GameBases(Contest prior)
        {
            var bases = new Dictionary<string, StateBase>(StringComparer.Ordinal);
            foreach (StateHouse s in prior.House)
            {
                bases[s.State] = new StateBase
                {
                    U = s.Share, UR = s.UnopposedRep, UD = s.UnopposedDem, MB = s.Seats, RB = s.Rep, DB = s.Dem,
                    OneSeatRep = s.Seats == 1 ? s.Rep == 1 : s.Rep * 2 > s.Seats,
                };
            }
            return bases;
        }

        /// <summary>R-US18's P(c, x): the nearest integer to c times x clamped to [0, 1], a half going by the half rule.</summary>
        private static int P(int c, double x, bool halfToRep)
        {
            double v = c * Math.Min(1.0, Math.Max(0.0, x));
            double f = Math.Floor(v);
            double frac = v - f;
            if (frac > 0.5) { return (int)f + 1; }
            if (frac < 0.5) { return (int)f; }
            return halfToRep ? (int)f + 1 : (int)f;
        }

        /// <summary>
        /// THE HOUSE COUNT of <paramref name="pollingDay"/> at <paramref name="vT"/> - R-US18 (b), pure. The base is <paramref name="gameBase"/>'s (the game's own
        /// latest House count) where one is handed, else the record's latest House election before the day. Null, with the reason, where no count can be made;
        /// otherwise each state's count, in the catalog's state order, with the base named.
        /// </summary>
        public static List<StateHouse> CountHouse(DateTime pollingDay, double vT, Contest gameBase, out int baseYear, out double vBase, out bool baseOfRecord, out string why)
        {
            why = null;
            baseOfRecord = gameBase == null;
            baseYear = gameBase != null ? gameBase.PollingDay.Year : RecordBaseYear(pollingDay);
            vBase = gameBase != null ? gameBase.V : RecordV(baseYear);
            if (double.IsNaN(vT)) { why = "the poll gives no REP share of REP+DEM"; return null; }
            if (baseYear == 0 || double.IsNaN(vBase)) { why = "the catalog holds no House election before " + pollingDay.Year.ToString(CultureInfo.InvariantCulture) + " to swing from"; return null; }
            Dictionary<string, StateBase> bases = gameBase != null ? GameBases(gameBase) : RecordBases(baseYear);
            SortedDictionary<string, int> inForce = SeatsInForce(pollingDay.Year);
            int nationalR = 0, nationalD = 0;
            foreach (StateBase b in bases.Values) { nationalR += b.RB; nationalD += b.DB; }
            double delta = vT - vBase;
            var counted = new List<StateHouse>();
            int total = 0;
            foreach (KeyValuePair<string, int> kv in inForce)
            {
                if (!bases.TryGetValue(kv.Key, out StateBase b) || b.MB == 0) { why = kv.Key + " has no decided seat in the base (" + baseYear.ToString(CultureInfo.InvariantCulture) + ")"; return null; }
                int nT = kv.Value;
                double uhat = b.U + delta;
                bool half = b.RB != b.DB ? b.RB > b.DB : nationalR > nationalD;   // the half rule: the state's base seats, then the national base majority
                int rep;
                if (nT == 1) { rep = uhat > 0.5 ? 1 : uhat < 0.5 ? 0 : (b.OneSeatRep ? 1 : 0); }
                else
                {
                    int cB = b.MB - b.UR - b.UD, cT = nT - b.UR - b.UD;
                    rep = Math.Min(nT - b.UD, Math.Max(b.UR, b.RB + P(cT, uhat, half) - P(cB, b.U, half)));
                }
                counted.Add(new StateHouse { State = kv.Key, Seats = nT, Rep = rep, Dem = nT - rep, Share = uhat, UnopposedRep = b.UR, UnopposedDem = b.UD });
                total += nT;
            }
            if (total != HouseMembers) { throw new InvalidOperationException($"the House in force for {pollingDay.Year} seats {total} members across {inForce.Count} states, not {HouseMembers} (2 U.S.C. 2a)"); }
            return counted;
        }

        // ------------------------------------------------------------------ the Senate

        /// <summary>The class whose terms expire after the polling day of <paramref name="year"/> (Art. I §3 cl. 2: one class every second year) - the class of
        /// the catalog's regular full-term races of record in a year six years apart from it; 0 where the catalog holds none.</summary>
        public static int ClassUp(int year)
        {
            foreach (var r in Generated.UsPresidentialReturns.SenateRaces)
            {
                if (r.Term == "full" && ((year - r.Year) % 6 + 6) % 6 == 0) { return r.Class; }
            }
            return 0;
        }

        /// <summary>The seats up on <paramref name="pollingDay"/>: every state holding a seat of the class up (US-12's roster, `senate_seats.csv`), each a
        /// full term; and the catalog's races of the day to another class (the specials of record - Nebraska's Class II in 2024).</summary>
        public static List<(string State, int Class, string Term)> SeatsUp(DateTime pollingDay)
        {
            int cls = ClassUp(pollingDay.Year);
            var states = new SortedSet<string>(StringComparer.Ordinal);
            foreach (var s in Generated.UsPresidentialReturns.SenateSeats) { if (s.Class == cls) { states.Add(s.State); } }
            var up = new List<(string, int, string)>();
            foreach (string st in states) { up.Add((st, cls, "full")); }
            foreach (var r in Generated.UsPresidentialReturns.SenateRaces)
            {
                if (r.Year == pollingDay.Year && r.Class != cls) { up.Add((r.State, r.Class, r.Term)); }
            }
            return up;
        }

        private static double Clamp(double v) => Math.Max(LogitClamp, Math.Min(1.0 - LogitClamp, v));
        private static double Logit(double v) => Math.Log(v / (1.0 - v));

        private static Dictionary<string, double> _parentOfRecord;

        /// <summary>p for the factors: each state's REP share of REP+DEM derived from 2020 at 2024's true national shares (R-US14 (a), the derivation US-4's and
        /// R-US19's instruments read - sum votes_d, votes_r, votes_other over sum votes_total of 2024's rows), read once.</summary>
        private static Dictionary<string, double> ParentsOfRecord()
        {
            if (_parentOfRecord != null) { return _parentOfRecord; }
            double d = 0, r = 0, o = 0, t = 0;
            foreach (var s in Generated.UsPresidentialReturns.States) { if (s.Year == 2024) { d += s.VotesD; r += s.VotesR; o += s.VotesOther; t += s.VotesTotal; } }
            ElectoralCollege.FromNationalShares(2020, 2024, new[] { d / t, r / t, o / t }, out ElectoralCollege.StateCall[] calls, out double _);
            var p = new Dictionary<string, double>(StringComparer.Ordinal);
            foreach (ElectoralCollege.StateCall c in calls) { p[c.State] = c.Shares[ElectoralCollege.ShareRepublican] / (c.Shares[ElectoralCollege.ShareRepublican] + c.Shares[ElectoralCollege.ShareDemocrat]); }
            _parentOfRecord = p;
            return p;
        }

        /// <summary>
        /// R-US19 (c) over (a), RULED (I2): a race's factor c_j - FITTED on 2024's class only (E1's form), one per race: logit s - logit p, s the race's base
        /// count (final_r / (final_r + final_dc); where no count has a candidate of one side, final_none_top stands for it - DECLARED by the block) and p the
        /// state's share derived from 2020 at 2024's true national shares, both clamped. 0 on every later field (factor 1.0) and for a race not fitted (no count on
        /// either side), and for a seat the catalog holds no race of that day for. Computed from the generated catalog, never typed.
        /// </summary>
        public static double FactorOf(string state, int cls, string term, DateTime pollingDay)
        {
            if (!UsTermLimit.FieldOfRecord(pollingDay)) { return 0.0; }
            foreach (var r in Generated.UsPresidentialReturns.SenateRaces)
            {
                if (r.Year != pollingDay.Year || r.State != state || r.Class != cls || r.Term != term) { continue; }
                long fr = r.FinalR, fd = r.FinalDc;
                if (fr == 0 && r.FinalNoneTop > 0) { fr = r.FinalNoneTop; } else if (fd == 0 && r.FinalNoneTop > 0) { fd = r.FinalNoneTop; }
                if (fr == 0 || fd == 0 || !ParentsOfRecord().TryGetValue(state, out double p)) { return 0.0; }
                return Logit(Clamp((double)fr / (fr + fd))) - Logit(Clamp(p));
            }
            return 0.0;
        }

        /// <summary>
        /// THE SENATE COUNT of <paramref name="pollingDay"/> on the night's states <paramref name="night"/> (the presidential count's, <see cref="UsPresidentialElection.StatesOf"/>) -
        /// R-US19 (c) over (a), pure but for the cascade's reads of <paramref name="country"/> at the end of the polling day (the holder's side; the majority with
        /// the Vice President's vote). Null, with the reason, where a state up has no share that night.
        /// </summary>
        public static List<SenateSeat> CountSenate(Country country, DateTime pollingDay, ElectoralCollege.StateCall[] night, int tieSeed, out string why)
        {
            why = null;
            if (night == null) { why = "the night's states are not to hand"; return null; }
            var parent = new Dictionary<string, double>(StringComparer.Ordinal);
            foreach (ElectoralCollege.StateCall c in night) { parent[c.State] = c.Shares[ElectoralCollege.ShareRepublican] / (c.Shares[ElectoralCollege.ShareRepublican] + c.Shares[ElectoralCollege.ShareDemocrat]); }
            Dictionary<(string, int), string> holders = SeatSides(country, pollingDay);
            var seats = new List<SenateSeat>();
            foreach ((string state, int cls, string term) in SeatsUp(pollingDay))
            {
                if (!parent.TryGetValue(state, out double p)) { why = state + " has no share in the night's count"; return null; }
                double c = FactorOf(state, cls, term, pollingDay);
                double share = c == 0.0 ? p : 1.0 / (1.0 + Math.Exp(-(Logit(Clamp(p)) + c)));
                var seat = new SenateSeat { State = state, Class = cls, Term = term, Parent = p, Factor = c, Share = share };
                if (share > 0.5) { seat.Side = "REP"; }
                else if (share < 0.5) { seat.Side = "DEM"; }
                else if (holders.TryGetValue((state, cls), out string held)) { seat.Side = held; seat.Cascade = "exactly a half - the holder's side, " + held; }
                else
                {
                    Dictionary<string, int> sides = SenateOn(country, pollingDay);
                    string majority = UsSenate.MajorityOf(sides, UsSenate.VicePresidentParty(country, pollingDay));
                    if (majority == "REP" || majority == "DEM") { seat.Side = majority; seat.Cascade = "exactly a half, the seat vacant - the Senate's majority, " + majority; }
                    else
                    {
                        seat.Side = ElectoralCollege.TieBreak(tieSeed, pollingDay.Year, state + "-S" + cls.ToString(CultureInfo.InvariantCulture)) == ElectoralCollege.Republican ? "REP" : "DEM";
                        seat.Cascade = "exactly a half, the seat vacant and the Senate's majority undecided - a seeded draw (US-8c), " + seat.Side;
                    }
                }
                seats.Add(seat);
            }
            return seats;
        }

        /// <summary>
        /// Who holds each Senate seat on <paramref name="date"/>, by (state, class), and on which side: the record's seats by date (US-15, `UsSenate.On`;
        /// independents with their conference, R-US10 (a); a vacant seat absent), and over them every seat the game has elected and seated by then - its latest
        /// contest's side for the seat. Empty before the record's first day (nothing is seated there). DECLARED (R-US1 (a), "whatever the game does not elect
        /// is seated on its record's dates"): the seats the game did not elect keep the record's resignations and appointments, those that follow from the
        /// record's own presidential result included - Vance's Ohio seat vacant from 10 Jan 2025 and Rubio's Florida seat on 20 Jan, both filled on 21 Jan,
        /// whoever the game elected - so the Senate seats fewer than 100 on those days; and a seat of record that the game's class will hold (California's,
        /// whose special the record seated on 9 Dec 2024) sits as the record seats it until the game's seats on 3 January.
        /// </summary>
        public static Dictionary<(string State, int Class), string> SeatSides(Country country, DateTime date)
        {
            var sides = new Dictionary<(string, int), string>();
            UsSenate.Seating record = UsSenate.On(date);
            if (!record.Covered) { return sides; }
            foreach (UsSenate.Seat s in record.Seats) { sides[(s.State, s.Class)] = s.Side; }
            if (country?.UsCongressElections == null) { return sides; }
            var seated = new List<Contest>();
            foreach (Contest c in country.UsCongressElections) { if (c.Convenes.Date <= date.Date) { seated.Add(c); } }
            seated.Sort((a, b) => a.Convenes.CompareTo(b.Convenes));
            foreach (Contest c in seated) { foreach (SenateSeat s in c.Senate) { sides[(s.State, s.Class)] = s.Side; } }
            return sides;
        }

        /// <summary>The Senate seated on <paramref name="date"/> by party key - every roster party, zero where it holds no seat (US-15's convention): the record's
        /// by date where the game has elected none of it, else the seats the game elected over the record's (<see cref="SeatSides"/>). Empty before the record's
        /// first day.</summary>
        public static Dictionary<string, int> SenateOn(Country country, DateTime date)
        {
            var seats = new Dictionary<string, int>();
            Dictionary<(string, int), string> sides = SeatSides(country, date);
            if (sides.Count == 0) { return seats; }
            CountryId id = country?.Id ?? CountryId.USA;
            foreach (PoliticalParty party in PartySystems.For(id)) { seats[party.Abbrev] = 0; }
            foreach (string side in sides.Values)
            {
                if (!seats.ContainsKey(side)) { throw new InvalidOperationException($"{id}'s Senate seats '{side}', which its roster does not hold"); }
                seats[side]++;
            }
            return seats;
        }

        /// <summary>The polling days of every Congress the game elected and seated by <paramref name="date"/>, oldest first - whose Senate seats sit over the
        /// record's (<see cref="SeatSides"/>).</summary>
        public static List<DateTime> SeatedPollingDays(Country country, DateTime date)
        {
            var days = new List<DateTime>();
            if (country?.UsCongressElections == null) { return days; }
            foreach (Contest c in country.UsCongressElections) { if (c.Convenes.Date <= date.Date) { days.Add(c.PollingDay.Date); } }
            days.Sort();
            return days;
        }

        /// <summary>Whether the game elected any of the Senate seated on <paramref name="date"/> - from the first contest's 3 January.</summary>
        public static bool GameSeatsSenate(Country country, DateTime date) => TryInOffice(country, date, out _);

        // ------------------------------------------------------------------ the count, and the day loop's hold

        /// <summary>
        /// THE CONGRESS COUNT of <paramref name="pollingDay"/>: the House by R-US18 (b) at the poll's V and the Senate class (and the day's specials) by R-US19 (c)
        /// over (a) on the presidential count's states (<paramref name="presidential"/>, re-derived exactly by <see cref="UsPresidentialElection.StatesOf(Country, UsPresidentialElection.Contest)"/> - on the game's own previous count's states after its first, PS-6 US-26). Pure
        /// but for the Senate cascade's reads of the country. Null, with the reason, where it cannot be made.
        /// </summary>
        public static Contest Count(Country country, DateTime pollingDay, IReadOnlyDictionary<string, double> poll, UsPresidentialElection.Contest presidential, out string why)
        {
            if (country == null || !ElectsIn(country.Id)) { why = "the game elects no Congress there"; return null; }
            if (presidential == null) { why = "no presidential count that day - the Senate reads its states"; return null; }
            double v = TwoPartyV(poll);
            Contest prior = null;
            if (country.UsCongressElections != null) { foreach (Contest c in country.UsCongressElections) { if (c.PollingDay < pollingDay.Date && (prior == null || c.PollingDay > prior.PollingDay)) { prior = c; } } }
            List<StateHouse> house = CountHouse(pollingDay.Date, v, prior, out int baseYear, out double vBase, out bool baseOfRecord, out why);
            if (house == null) { return null; }
            List<SenateSeat> senate = CountSenate(country, pollingDay.Date, UsPresidentialElection.StatesOf(country, presidential), presidential.TieSeed, out why);   // PS-6 US-26: the night's states as the count called them - a later count's on the game's own previous states
            if (senate == null) { return null; }
            var contest = new Contest
            {
                PollingDay = pollingDay.Date, Convenes = ConvenesAfter(pollingDay), V = v, VBase = vBase, BaseYear = baseYear, BaseOfRecord = baseOfRecord,
                House = house, SenateClass = ClassUp(pollingDay.Year), Senate = senate,
                PollV = v, TermPoints = 0.0,   // PS-6 US-27: a presidential year carries no midterm term
            };
            foreach (StateHouse s in house) { contest.HouseRep += s.Rep; contest.HouseDem += s.Dem; }
            int delegationsR = 0, delegationsD = 0;
            foreach (StateHouse s in house) { if (s.Rep > s.Dem) { delegationsR++; } else if (s.Dem > s.Rep) { delegationsD++; } }
            contest.Line = string.Format(CultureInfo.InvariantCulture,
                "the House of {0:yyyy-MM-dd}: REP {1}, DEM {2} of {3} (R-US18 (b), V {4:0.0000} against {5}'s {6:0.0000} {7}); delegations REP {8}, DEM {9}, tied {10}; the Senate's class {11} and {12} special(s): REP {13}, DEM {14} (R-US19 (c) over (a)); seated {15:yyyy-MM-dd}",
                contest.PollingDay, contest.HouseRep, contest.HouseDem, HouseMembers, v, baseYear, vBase, baseOfRecord ? "of record" : "the game's own", delegationsR, delegationsD, house.Count - delegationsR - delegationsD,
                contest.SenateClass, senate.FindAll(s => s.Class != contest.SenateClass).Count, contest.SenateOf("REP"), contest.SenateOf("DEM"), contest.Convenes);
            return contest;
        }

        /// <summary>
        /// PS-6 US-27: the national vote a midterm counts, in <see cref="ElectoralCollege.ShareDemocrat"/>'s order as fractions of the poll's whole: every other
        /// party's share as the poll gives it, and REP's and DEM's two-party split at <paramref name="v"/> (the poll's V moved by the midterm term). Null where
        /// the poll gives no positive REP and DEM share.
        /// </summary>
        public static double[] MidtermNational(IReadOnlyDictionary<string, double> poll, double v)
        {
            if (poll == null || double.IsNaN(v) || !poll.TryGetValue("REP", out double r) || !poll.TryGetValue("DEM", out double d) || double.IsNaN(r) || double.IsNaN(d) || r <= 0.0 || d <= 0.0) { return null; }
            double total = 0.0;
            foreach (KeyValuePair<string, double> kv in poll) { if (!double.IsNaN(kv.Value) && kv.Value > 0.0) { total += kv.Value; } }
            double two = (r + d) / total;
            return new[] { two * (1.0 - v), two * v, 1.0 - two };
        }

        /// <summary>
        /// PS-6 US-27 (DECLARED): the states a midterm's Senate class reads - R-US19 (a)'s presidential-derived share with no presidential count that night: the
        /// presidential derivation at the midterm's national vote <paramref name="national"/>, on the game's own previous presidential count's states where it has
        /// counted one (<see cref="ElectoralCollege.FromGameStates"/>, R-US24 (b)'s form, the states re-derived from the stored contest), else on the catalog's
        /// latest presidential election before the day (<see cref="ElectoralCollege.FromNationalShares(int, int, double[], int, out ElectoralCollege.StateCall[], out double)"/>,
        /// R-US14 (a)); the electors those in force, as a count of that day would read them. Null, with the reason, where the catalog holds no election to swing
        /// from. <paramref name="basis"/> names the states' source.
        /// </summary>
        public static ElectoralCollege.StateCall[] MidtermStates(Country country, DateTime pollingDay, double[] national, int tieSeed, out string basis, out string why)
        {
            basis = null;
            why = null;
            if (national == null) { why = "the poll gives no national vote to derive the states from"; return null; }
            if (!ElectoralCollege.TryPriorYear(pollingDay.Year, out int prior) || !ElectoralCollege.TryElectorsYear(pollingDay.Year, out int inForce))
            {
                why = "the catalog holds no presidential election before " + pollingDay.Year.ToString(CultureInfo.InvariantCulture) + " to derive the states from";
                return null;
            }
            UsPresidentialElection.Contest previous = UsPresidentialElection.PreviousCount(country, pollingDay.Date);
            ElectoralCollege.StateCall[] previousStates = previous != null ? UsPresidentialElection.StatesOf(country, previous) : null;
            ElectoralCollege.StateCall[] states;
            if (previousStates != null)
            {
                ElectoralCollege.FromGameStates(previousStates, prior, inForce, national, tieSeed, out states, out double _);
                basis = string.Format(CultureInfo.InvariantCulture, "each state's share derived from the game's own presidential count of {0:yyyy-MM-dd}", previous.PollingDay);
            }
            else
            {
                ElectoralCollege.FromNationalShares(prior, inForce, national, tieSeed, out states, out double _);
                basis = string.Format(CultureInfo.InvariantCulture, "each state's share derived from the presidential election of record of {0}", prior);
            }
            return states;
        }

        /// <summary>
        /// PS-6 US-27 - THE MIDTERM COUNT of <paramref name="pollingDay"/> (2 U.S.C. §7): no presidential count; the House by R-US18 (b) and the class whose terms
        /// expire (and the day's specials of record) by R-US19 (c) over (a), both at the poll's V moved by R-US20 (b)'s fitted term on the president's party
        /// (<see cref="MidtermTermPoints"/>, <see cref="WithMidtermTerm"/>), the Senate on <see cref="MidtermStates"/>. The House's base is every count's - the
        /// game's own latest House count, else the record's latest House election. Pure but for the Senate cascade's reads of the country and the president's
        /// party read from it. Null, with the reason, where it cannot be made.
        /// </summary>
        public static Contest CountMidterm(Country country, DateTime pollingDay, IReadOnlyDictionary<string, double> poll, int tieSeed, out string why)
        {
            if (country == null || !ElectsIn(country.Id)) { why = "the game elects no Congress there"; return null; }
            if (!IsMidterm(pollingDay)) { why = "not a midterm polling day"; return null; }
            double pollV = TwoPartyV(poll);
            string party = PresidentsPartyOn(country);
            double term = party == "REP" || party == "DEM" ? MidtermTermPoints : 0.0;
            double v = WithMidtermTerm(pollV, party, term);
            Contest prior = null;
            if (country.UsCongressElections != null) { foreach (Contest c in country.UsCongressElections) { if (c.PollingDay < pollingDay.Date && (prior == null || c.PollingDay > prior.PollingDay)) { prior = c; } } }
            List<StateHouse> house = CountHouse(pollingDay.Date, v, prior, out int baseYear, out double vBase, out bool baseOfRecord, out why);
            if (house == null) { return null; }
            ElectoralCollege.StateCall[] states = MidtermStates(country, pollingDay.Date, MidtermNational(poll, v), tieSeed, out string statesBasis, out why);
            if (states == null) { return null; }
            List<SenateSeat> senate = CountSenate(country, pollingDay.Date, states, tieSeed, out why);
            if (senate == null) { return null; }
            var contest = new Contest
            {
                PollingDay = pollingDay.Date, Convenes = ConvenesAfter(pollingDay), V = v, VBase = vBase, BaseYear = baseYear, BaseOfRecord = baseOfRecord,
                House = house, SenateClass = ClassUp(pollingDay.Year), Senate = senate, PollV = pollV, TermPoints = term, PresidentsParty = party,
            };
            foreach (StateHouse s in house) { contest.HouseRep += s.Rep; contest.HouseDem += s.Dem; }
            int delegationsR = 0, delegationsD = 0;
            foreach (StateHouse s in house) { if (s.Rep > s.Dem) { delegationsR++; } else if (s.Dem > s.Rep) { delegationsD++; } }
            contest.Line = string.Format(CultureInfo.InvariantCulture,
                "the midterm House of {0:yyyy-MM-dd}: REP {1}, DEM {2} of {3} (R-US18 (b), V {4:0.0000} - the poll's {5:0.0000} with the midterm term {6:+0.0000;-0.0000} pp on {7} (R-US20 (b), FITTED, n {8}) - against the {10:0.0000} of {9}, {11}); delegations REP {12}, DEM {13}, tied {14}; the Senate's class {15} and {16} special(s): REP {17}, DEM {18} (R-US19 (c) over (a), {19}); no presidential count; seated {20:yyyy-MM-dd}",
                contest.PollingDay, contest.HouseRep, contest.HouseDem, HouseMembers, v, pollV, term, party ?? "no president's party", Generated.UsMidtermTerm.N, baseYear, vBase, baseOfRecord ? "of record" : "the game's own",
                delegationsR, delegationsD, house.Count - delegationsR - delegationsD, contest.SenateClass, senate.FindAll(s => s.Class != contest.SenateClass).Count, contest.SenateOf("REP"), contest.SenateOf("DEM"),
                statesBasis, contest.Convenes);
            return contest;
        }

        /// <summary>Whether <paramref name="date"/> is a Congress polling day the game has still to count in this country - every even year's (2 U.S.C. §7,
        /// `WorldClock.TryNextPollingDay`; PS-6 US-27: the midterms with the presidential years).</summary>
        public static bool IsPollingDay(Country country, DateTime date)
        {
            if (country?.UsCongressElections == null || !ElectsIn(country.Id)) { return false; }
            if (!WorldClock.TryNextPollingDay(country.Id, date.Date, out DateTime day) || day != date.Date) { return false; }
            foreach (Contest c in country.UsCongressElections) { if (c.PollingDay.Date == date.Date) { return false; } }
            return true;
        }

        /// <summary>PS-6 US-27: the day loop's midterm count - on a midterm polling day, once, on <paramref name="poll"/> (the day's counted poll) with the day's
        /// tie seed; the contest added to the country and returned, null otherwise with the reason.</summary>
        public static Contest HoldMidterm(Country country, DateTime date, IReadOnlyDictionary<string, double> poll, int tieSeed, out string why)
        {
            if (country?.UsCongressElections == null) { why = "the country carries no list of Congress elections"; return null; }
            if (!IsPollingDay(country, date) || !IsMidterm(date)) { why = "not a midterm polling day the game has still to count"; return null; }
            Contest contest = CountMidterm(country, date.Date, poll, tieSeed, out why);
            if (contest != null) { country.UsCongressElections.Add(contest); }
            return contest;
        }

        /// <summary>The day loop's count: on the presidential polling day, once, after the presidential count <paramref name="presidential"/> of the same day,
        /// on the same counted poll - the contest added to the country and returned; null otherwise, with the reason.</summary>
        public static Contest Hold(Country country, DateTime date, IReadOnlyDictionary<string, double> poll, UsPresidentialElection.Contest presidential, out string why)
        {
            if (country?.UsCongressElections == null) { why = "the country carries no list of Congress elections"; return null; }
            if (!IsPollingDay(country, date) || presidential == null || presidential.PollingDay.Date != date.Date) { why = "not a polling day the game has still to count"; return null; }
            Contest contest = Count(country, date.Date, poll, presidential, out why);
            if (contest != null) { country.UsCongressElections.Add(contest); }
            return contest;
        }
    }
}
