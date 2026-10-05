using System;
using System.Collections.Generic;
using System.Globalization;
using PoliSim.Data;

namespace PoliSim.Elections
{
    /// <summary>
    /// PS-5 item C4 (§770; Elias, item C of the list resent 2026-10-02: <i>"Do the live wiring, the PiS start and the game's own presidential
    /// election"</i>): THE GAME ELECTS POLAND'S PRESIDENT. The rule is §720's (<see cref="TwoRoundElection"/>, Konstytucja Art. 127-128), the gate
    /// §765's (B5), the vote §727's measured with §764's fit (B4); this runs them on the game's own days and holds the winner as the president the
    /// veto reads (<see cref="PresidentAt"/>, <see cref="PresidentialVeto.Decide"/>).
    /// <list type="bullet">
    /// <item><description><b>The days.</b> A first vote falls in the window the Marshal orders it for - 100 to 75 days before the term ends (Art. 128
    /// ust. 2). The record's day where the record holds one in the window; otherwise the window's last Sunday - RULED (Elias's ruling E2, §773: "one
    /// principle for both votes: follow the most recent practice" - 2025's last Sunday; 2015 and 2020 were ordered for the window's second). The
    /// run-off is the 14th day after (Art. 127 ust. 4). The president-elect takes office on the day the predecessor's term ends - RULED (E2): the
    /// oath on that day, as in 2015, 2020 and 2025 (records_by_date.md §2d).</description></item>
    /// <item><description><b>Who stands</b> (B5). Where the election is of record, <b>the field of record</b> - every candidate the PKW's first round
    /// returned (§772, Elias's ruling E1), each of whom cleared the gate in fact. Otherwise a candidate for each roster party: the incumbent where his
    /// party fields him and the term limit allows (Art. 127 ust. 2), the party's leader where the roster holds one - a created party's, the player's
    /// own included (RULED, E2) - else the party's candidate unnamed (the game names no one a party has not put forward, so a later field holds no
    /// independents). The gate is the signatures - RULED (E2): a party's candidate clears it where the party's predicted voters, at the valid votes
    /// of the latest presidential first round of record on or before the day, number at least the signatures that nominate.</description></item>
    /// <item><description><b>The first round</b> (§772, E1): a candidate a roster party backs carries <b>the party's support in the game's poll × the
    /// candidate's factor</b> - the poll the live prediction, the Sejm's shares on the day with the government's record shifting them, as the game's
    /// Sejm vote reads them; the factors of record [FITTED] once to the PKW's first round of 18 May 2025 against the game's poll that day on the
    /// reference world - the record's state on the eve, as a game started that day holds it - which therefore reproduces that first round
    /// (<see cref="PresidencyOfRecord.CandidateOfRecord.Factor"/>); a later field's candidate at <see cref="FutureFactor"/>. A candidate of record
    /// whose party has no roster row carries its own share of record as its base. The field is normalised; counted by the rule: more than half
    /// elects; otherwise the two with most meet in the run-off. ⚠ PREMISE, DECLARED (§772's review): a game PLAYED from Poland's 2023 start reads
    /// the history of the chamber seated at its own epoch and its own formation's government, so its 2025 first round is not the PKW's.</description></item>
    /// <item><description><b>The run-off</b> (B4): each eliminated electorate splits between the finalists as exp(−d²/τ) in the sovereignty space
    /// (galtan, nationalism and the EU position, equally weighted), τ <see cref="TransferTau"/>; an unplaced one splits as the finalists' first votes
    /// did; the [AUTHORED-DRAFT] abstention <see cref="AbstentionDraftMax"/> beside it. <b>Each candidate stands at their own party's position</b>
    /// (E1), as B4 was fitted: a candidate of record at its CHES unit (<see cref="PresidencyOfRecord.CandidateOfRecord.PositionUnit"/> - Hołownia at
    /// Polska 2050's own row, Zandberg at Razem's, Braun at Konfederacja's, the list his party ran on), any other at the backing party's position.
    /// The positions are taken at the first vote and kept with the contest.</description></item>
    /// </list>
    /// The model predicts shares, not turnout: the rounds are counted in parts per million of the valid vote and printed as shares, never as votes.
    /// </summary>
    public static class PresidentialElection
    {
        /// <summary>[FITTED] B4 (§764): the run-off transfer's τ in the sovereignty space - least squares on the Ipsos exit poll's pairs of
        /// 1 June 2025. `PresidentialVoteBacktest` refits it on every bar and fails where this figure is no longer its fit.</summary>
        public const double TransferTau = 18.301;

        /// <summary>[AUTHORED-DRAFT] B4 (§764): the share of the eliminated electorate farthest from both finalists that stays home in the run-off;
        /// the others in proportion to their distance to the nearer finalist. No source measures it (the exit poll interviews run-off voters only);
        /// on the calibration list.</summary>
        public const double AbstentionDraftMax = 0.25;

        /// <summary>RULED (Elias's ruling E1, §772): "Future fields start at factor 1.0" - a candidate outside the field of record carries the party's
        /// poll unchanged until play calibrates otherwise.</summary>
        public const double FutureFactor = 1.0;

        /// <summary>How <see cref="PresidentAt"/>'s basis begins for a president the game elected - the one test the pages read ("ELECTED IN THE GAME").</summary>
        public const string GameBasis = "the game's own election";

        /// <summary>The rounds' unit: parts per million of the valid vote.</summary>
        private const double PartsPerMillion = 1e6;

        /// <summary>One candidate of a first round: the name, the party whose standing the candidate inherits, the share of the valid vote (%), and
        /// where the candidate stands in the sovereignty space (<see cref="Placed"/> false: no position - the electorate splits as the first votes did).</summary>
        public sealed class Candidate
        {
            public string Name;
            public string Party;
            public double Share;
            public bool Placed;
            public double Galtan, Nationalism, Eu;
            /// <summary>The CHES unit the position is read from - the backing party's key, or the unit a candidate of record was fitted at.</summary>
            public string PositionUnit;
        }

        /// <summary>One presidential election the game held - kept with the country (<see cref="Country.PresidentialElections"/>), so a save carries it.</summary>
        public sealed class Contest
        {
            public DateTime FirstVote;
            public string DayBasis;
            /// <summary>The day the president it elects takes office - the predecessor's term's end.</summary>
            public DateTime TakesOffice;
            public List<Candidate> Field = new List<Candidate>();
            /// <summary>Who did not stand, with why - the reasons <see cref="PresidentialElection.FirstVote"/> writes: a roster party short of the gate, a candidate of record whose backing party has no share in the poll.</summary>
            public List<string> NotStanding = new List<string>();
            public string RunOffA, RunOffB;
            public DateTime RunOffOn;
            public bool RunOffHeld;
            /// <summary>The run-off: <see cref="RunOffA"/>'s share of the two (%), and the first round's voters who voted in it (%).</summary>
            public double RunOffShareA, RunOffVoting;
            public string Elected;
            public string ElectedParty;
            /// <summary>The count's verdict in a line - the first vote's, then the run-off's.</summary>
            public string Line;

            public bool Decided() => Elected != null;
            /// <summary>A run-off the first vote called and the day has not yet held.</summary>
            public bool RunOffPending() => !Decided() && RunOffA != null && !RunOffHeld;
        }

        /// <summary>A candidate's position in the sovereignty space: CHES galtan and nationalism (0-10) and the EU position (1-7).</summary>
        public readonly struct Point
        {
            public readonly double Galtan, Nationalism, Eu;
            public Point(double galtan, double nationalism, double eu) { Galtan = galtan; Nationalism = nationalism; Eu = eu; }
        }

        /// <summary>The sovereignty space's squared distance (§727): the three axes on 0-10, equally weighted.</summary>
        public static double Distance2(Point p, Point q) => (Sq(p.Galtan - q.Galtan) + Sq(p.Nationalism - q.Nationalism) + Sq(EuTen(p.Eu) - EuTen(q.Eu))) / 3.0;

        /// <summary>
        /// The run-off from a first round (B4): the finalists keep their own votes; each eliminated electorate splits by exp(−d²/τ) toward each
        /// finalist, one with no position as the finalists' first votes did; <paramref name="stayMax"/> &gt; 0 keeps home a share in proportion to
        /// the electorate's distance to the nearer finalist, the farthest at <paramref name="stayMax"/>. Returns <paramref name="a"/>'s share of
        /// the two (%); <paramref name="voting"/> is the first round's share voting in the run-off (%). The Editor backtest proves this arithmetic
        /// equal to its own on each run-off of record it measures.
        /// </summary>
        public static double RunOffShare(IReadOnlyList<(string Name, double Share, Point? At)> firstRound, string a, string b, Point atA, Point atB, double tau, double stayMax, out double voting)
        {
            double va = 0.0, vb = 0.0, total = 0.0;
            foreach ((string name, double share, Point? _) in firstRound)
            {
                total += share;
                if (name == a) { va += share; } else if (name == b) { vb += share; }
            }
            double a0 = va, b0 = vb, far = 0.0;
            foreach ((string name, double _, Point? at) in firstRound)
            {
                if (name != a && name != b && at.HasValue) { far = Math.Max(far, Math.Min(Distance2(at.Value, atA), Distance2(at.Value, atB))); }
            }
            foreach ((string name, double share, Point? at) in firstRound)
            {
                if (name == a || name == b) { continue; }
                if (!at.HasValue) { va += share * a0 / (a0 + b0); vb += share * b0 / (a0 + b0); continue; }
                double dA = Distance2(at.Value, atA), dB = Distance2(at.Value, atB);
                double votes = stayMax > 0.0 && far > 0.0 ? share * (1.0 - stayMax * Math.Min(dA, dB) / far) : share;
                double ea = Math.Exp(-dA / tau), eb = Math.Exp(-dB / tau);
                va += votes * ea / (ea + eb);
                vb += votes * eb / (ea + eb);
            }
            voting = total > 0.0 ? 100.0 * (va + vb) / total : 0.0;
            return 100.0 * va / (va + vb);
        }

        /// <summary>
        /// The president in office on <paramref name="date"/>: the latest of <paramref name="held"/> decided whose winner has taken office by the day;
        /// where none has, the president of record on the day. An undecided contest seats no one - the reading before it stands, which before the
        /// game's first decided contest is the record's (whose next president the record may name).
        /// </summary>
        public static bool PresidentAt(CountryId id, IReadOnlyList<Contest> held, DateTime date, out PresidencyOfRecord.President president)
        {
            Contest sitting = null;
            if (held != null)
            {
                foreach (Contest c in held) { if (c.Decided() && c.TakesOffice <= date && (sitting == null || c.TakesOffice > sitting.TakesOffice)) { sitting = c; } }
            }
            if (sitting == null) { return PresidencyOfRecord.TryAt(id, date, out president); }
            DateTime until = DateTime.MaxValue;
            foreach (Contest c in held) { if (c.Decided() && c.TakesOffice > sitting.TakesOffice && c.TakesOffice < until) { until = c.TakesOffice; } }
            president = new PresidencyOfRecord.President(sitting.Elected, sitting.TakesOffice, until, sitting.FirstVote, sitting.RunOffHeld ? sitting.RunOffOn : DateTime.MinValue,
                GameBasis + " (" + sitting.Line + ")", sitting.ElectedParty, "the party whose standing the candidate inherited (§770)");
            return true;
        }

        /// <summary>Whether <paramref name="president"/> is one the game elected (<see cref="GameBasis"/>), not the record's.</summary>
        public static bool ElectedInGame(PresidencyOfRecord.President president) => president.Basis != null && president.Basis.StartsWith(GameBasis, StringComparison.Ordinal);

        /// <summary>
        /// The first vote's day for the term ending <paramref name="termEnds"/>: the record's where it holds one in the window, else the window's last
        /// Sunday (RULED, E2 - see the class).
        /// </summary>
        public static DateTime FirstVoteFor(TwoRoundElection.Rule rule, CountryId id, DateTime termEnds, out string basis)
        {
            (DateTime _, DateTime opens, DateTime closes) = TwoRoundElection.TermOf(rule, termEnds.AddYears(-rule.TermYears));
            TwoRoundElection.RoundOfRecord(id, termEnds.Year, 1, out DateTime ofRecord);
            if (ofRecord >= opens && ofRecord <= closes)
            {
                basis = "the record's day, ordered by the Marshal of the Sejm (" + rule.Basis + ")";
                return ofRecord;
            }
            DateTime day = closes;
            while (day.DayOfWeek != DayOfWeek.Sunday) { day = day.AddDays(-1); }
            basis = "RULED (E2) - the most recent practice: the last Sunday of the window Art. 128 ust. 2 sets, 100 to 75 days before the term ends, as in 2025 (2015 and 2020 were ordered for its second)";
            return day;
        }

        /// <summary>The next first vote not yet held, on or after <paramref name="onOrAfter"/>, and the end of the term it elects a successor for; false
        /// where the country elects no president in two rounds, none is in office to count a term from, or no such day falls in the next four terms.</summary>
        public static bool TryNextFirstVote(Country country, DateTime onOrAfter, out DateTime day, out DateTime termEnds, out string basis)
        {
            day = termEnds = DateTime.MinValue;
            basis = null;
            TwoRoundElection.Rule rule = TwoRoundElection.RuleOf(country.Id);
            if (rule == null || !PresidentAt(country.Id, country.PresidentialElections, onOrAfter, out PresidencyOfRecord.President incumbent)) { return false; }
            termEnds = incumbent.TookOffice.AddYears(rule.TermYears);
            for (int term = 0; term < 4; term++)
            {
                DateTime candidateDay = FirstVoteFor(rule, country.Id, termEnds, out basis);
                if (candidateDay >= onOrAfter && !country.PresidentialElections.Exists(c => c.FirstVote == candidateDay)) { day = candidateDay; return true; }
                termEnds = termEnds.AddYears(rule.TermYears);
            }
            termEnds = DateTime.MinValue;
            basis = null;
            return false;
        }

        /// <summary>
        /// The game's presidential rounds on <paramref name="date"/>: the run-off of a contest whose day it is, or the first vote where today is its day
        /// (the field and the count from <paramref name="predicted"/>, the live prediction - null where none could be made, and nothing is held).
        /// Returns the contest a round was held in today, or null.
        /// </summary>
        public static Contest HoldRounds(Country country, DateTime date, IReadOnlyDictionary<string, double> predicted)
        {
            TwoRoundElection.Rule rule = TwoRoundElection.RuleOf(country.Id);
            if (rule == null) { return null; }
            foreach (Contest c in country.PresidentialElections)
            {
                if (c.RunOffPending() && c.RunOffOn == date) { HoldRunOff(c); return c; }
            }
            if (predicted == null || !TryNextFirstVote(country, date, out DateTime day, out DateTime termEnds, out string basis) || day != date) { return null; }
            Contest contest = FirstVote(country, rule, date, termEnds, basis, predicted);
            country.PresidentialElections.Add(contest);
            return contest;
        }

        /// <summary>Whether a round of the game's presidential election falls on <paramref name="date"/> - a first vote not yet held, or a pending run-off.</summary>
        public static bool IsRoundDay(Country country, DateTime date, out bool runOff)
        {
            runOff = country.PresidentialElections.Exists(c => c.RunOffPending() && c.RunOffOn == date);
            if (runOff) { return true; }
            return TryNextFirstVote(country, date, out DateTime day, out DateTime _, out string _) && day == date;
        }

        private static Contest FirstVote(Country country, TwoRoundElection.Rule rule, DateTime date, DateTime termEnds, string basis, IReadOnlyDictionary<string, double> predicted)
        {
            var contest = new Contest { FirstVote = date, DayBasis = basis, TakesOffice = termEnds };
            long gate = GateTotal(country.Id, date, out int gateYear);
            PresidentAt(country.Id, country.PresidentialElections, date, out PresidencyOfRecord.President incumbent);
            // §772 (E1): each candidate's support before the field is normalised - a candidate a roster party backs carries the party's poll × the
            // candidate's factor (the fitted one of record, FutureFactor otherwise); a candidate of record with no roster party carries its own share
            // of record as its base
            var support = new List<(Candidate Candidate, double Support)>();
            var fielded = new HashSet<string>();
            foreach (PresidencyOfRecord.CandidateOfRecord c in PresidencyOfRecord.CandidatesOf(country.Id, termEnds.Year))
            {
                // the field of record stood - the PKW registered every candidate it returned - so the gate's reading is not put to it
                string name = PresidencyOfRecord.NameOfRecord(country.Id, termEnds.Year, c.Surname);
                double s;
                if (c.BackingParty != null)
                {
                    fielded.Add(c.BackingParty);
                    s = predicted.TryGetValue(c.BackingParty, out double poll) && !double.IsNaN(poll) && poll > 0.0 ? poll * c.Factor : 0.0;
                }
                else
                {
                    s = PresidencyOfRecord.ShareOfRecord(country.Id, termEnds.Year, c.Surname);
                }
                if (double.IsNaN(s) || s <= 0.0) { contest.NotStanding.Add(name + " - no support to count: the backing party has no share in the poll (" + c.Why + ")"); continue; }
                support.Add((new Candidate { Name = name, Party = c.BackingParty, PositionUnit = c.PositionUnit }, s));
            }
            foreach (PoliticalParty party in PartySystems.For(country.Id))
            {
                if (fielded.Contains(party.Abbrev) || !predicted.TryGetValue(party.Abbrev, out double share) || double.IsNaN(share) || share <= 0.0) { continue; }
                if (share * gate < rule.NominationSignatures)
                {
                    contest.NotStanding.Add(string.Format(CultureInfo.InvariantCulture, "{0} - its predicted voters, {1:0.00} % of {2}'s {3:N0} valid votes, fall short of the {4:N0} signatures that nominate",
                        PartySystems.ShortName(country.Id, party.Abbrev), 100.0 * share, gateYear, gate, rule.NominationSignatures));
                    continue;
                }
                support.Add((new Candidate { Name = CandidateFor(country, rule, party, date, incumbent), Party = party.Abbrev, PositionUnit = party.Abbrev }, share * FutureFactor));
            }
            double sum = 0.0;
            foreach ((Candidate _, double s) in support) { sum += s; }
            var votes = new List<(string Candidate, long Votes)>();
            foreach ((Candidate candidate, double s) in support)
            {
                candidate.Share = 100.0 * s / sum;
                if (candidate.PositionUnit != null && TryPosition(country.Id, candidate.PositionUnit, out Point at))
                {
                    candidate.Placed = true;
                    candidate.Galtan = at.Galtan;
                    candidate.Nationalism = at.Nationalism;
                    candidate.Eu = at.Eu;
                }
                contest.Field.Add(candidate);
                votes.Add((candidate.Name, (long)Math.Round(PartsPerMillion * s / sum)));
            }
            contest.Field.Sort((x, y) => y.Share.CompareTo(x.Share));
            TwoRoundElection.FirstVote counted = TwoRoundElection.CountFirstVote(rule, date, votes);
            if (counted.ElectedOutright != null)
            {
                contest.Elected = counted.ElectedOutright;
                contest.ElectedParty = Find(contest, counted.ElectedOutright)?.Party;
                contest.Line = string.Format(CultureInfo.InvariantCulture, "{0} elected in the first vote with {1:0.00} % - more than half", contest.Elected, Find(contest, contest.Elected)?.Share ?? 0.0);
            }
            else if (counted.RunOff != null && counted.RunOff.Length == 2)
            {
                contest.RunOffA = counted.RunOff[0];
                contest.RunOffB = counted.RunOff[1];
                contest.RunOffOn = counted.RunOffOn;
                contest.Line = string.Format(CultureInfo.InvariantCulture, "no candidate has more than half - {0} ({1:0.00} %) and {2} ({3:0.00} %) meet in the run-off on {4:yyyy-MM-dd}",
                    contest.RunOffA, Find(contest, contest.RunOffA)?.Share ?? 0.0, contest.RunOffB, Find(contest, contest.RunOffB)?.Share ?? 0.0, contest.RunOffOn);
            }
            else
            {
                // a tie the statute's lot would break (the count's line says nothing is decided), or fewer than two standing (it does not)
                contest.Line = counted.Line.EndsWith("nothing is decided", StringComparison.Ordinal) ? counted.Line : counted.Line + "; nothing is decided";
            }
            return contest;
        }

        private static void HoldRunOff(Contest contest)
        {
            contest.RunOffHeld = true;
            Candidate a = Find(contest, contest.RunOffA), b = Find(contest, contest.RunOffB);
            if (a == null || b == null || !a.Placed || !b.Placed)
            {
                contest.Line += "; the run-off cannot be counted - a finalist has no position in the sovereignty space; nothing is decided";
                return;
            }
            var firstRound = new List<(string Name, double Share, Point? At)>();
            foreach (Candidate c in contest.Field) { firstRound.Add((c.Name, c.Share, c.Placed ? new Point(c.Galtan, c.Nationalism, c.Eu) : (Point?)null)); }
            contest.RunOffShareA = RunOffShare(firstRound, a.Name, b.Name, new Point(a.Galtan, a.Nationalism, a.Eu), new Point(b.Galtan, b.Nationalism, b.Eu), TransferTau, AbstentionDraftMax, out contest.RunOffVoting);
            long votesA = (long)Math.Round(PartsPerMillion * contest.RunOffShareA / 100.0);
            string elected = TwoRoundElection.CountRunOff(new List<(string Candidate, long Votes)> { (a.Name, votesA), (b.Name, (long)PartsPerMillion - votesA) });
            if (elected == null) { contest.Line += "; the run-off tied - the statute's resolution is not modelled; nothing is decided"; return; }
            contest.Elected = elected;
            contest.ElectedParty = Find(contest, elected).Party;
            double winner = elected == a.Name ? contest.RunOffShareA : 100.0 - contest.RunOffShareA;
            contest.Line += string.Format(CultureInfo.InvariantCulture, "; {0} elected in the run-off of {1:yyyy-MM-dd} with {2:0.00} % of the two, taking office {3:yyyy-MM-dd}",
                elected, contest.RunOffOn, winner, contest.TakesOffice);
        }

        /// <summary>The candidate a party outside the field of record fields (see the class): the incumbent within the term limit, the party's leader
        /// where the roster holds one, else the party's candidate unnamed - with the election's year, so two of a party's are never read as one person
        /// (the term limit counts by name).</summary>
        private static string CandidateFor(Country country, TwoRoundElection.Rule rule, PoliticalParty party, DateTime firstVote, PresidencyOfRecord.President incumbent)
        {
            if (incumbent.Name != null && incumbent.BackingParty == party.Abbrev && TermsServed(country, incumbent.Name) < rule.MaxTerms) { return incumbent.Name; }
            if (party.Leaders != null && party.Leaders.Length > 0) { return party.Leaders[0].Name; }
            return PartySystems.ShortName(country.Id, party.Abbrev) + "'s candidate (" + firstVote.Year.ToString(CultureInfo.InvariantCulture) + ")";
        }

        /// <summary>The terms a person has served: the game's elections won, and the record's presidencies that began before the game's first.</summary>
        private static int TermsServed(Country country, string name)
        {
            int terms = 0;
            DateTime gameFirst = DateTime.MaxValue;
            foreach (Contest c in country.PresidentialElections)
            {
                if (c.FirstVote < gameFirst) { gameFirst = c.FirstVote; }
                if (c.Elected == name) { terms++; }
            }
            foreach (PresidencyOfRecord.President p in PresidencyOfRecord.Of(country.Id)) { if (p.Name == name && p.FirstVote < gameFirst) { terms++; } }
            return terms;
        }

        /// <summary>The gate's base: the valid votes of the latest presidential first round of record on or before <paramref name="date"/> (the
        /// earliest held where none is).</summary>
        private static long GateTotal(CountryId id, DateTime date, out int year)
        {
            long valid = 0;
            year = 0;
            if (id != CountryId.Poland) { return valid; }
            foreach (var r in Generated.PolishPresidentialReturns.Rounds)
            {
                if (r.Round == 1 && new DateTime(r.Year, r.Month, r.Day) <= date && r.Year > year) { year = r.Year; valid = r.Valid; }
            }
            if (year == 0)
            {
                foreach (var r in Generated.PolishPresidentialReturns.Rounds) { if (r.Round == 1 && (year == 0 || r.Year < year)) { year = r.Year; valid = r.Valid; } }
            }
            return valid;
        }

        /// <summary>
        /// The position of a CHES unit in the sovereignty space: a roster party by its key, or - for a candidate of record standing at a unit the roster
        /// does not seat - a joint list's member (<see cref="PartySystems.PolandTdMembers"/>) or an unseated unit (<see cref="PartySystems.PolandUnseatedUnits"/>),
        /// read where the roster holds no positioned party by that key. False where none holds the unit with a position on all three axes.
        /// </summary>
        public static bool TryPosition(CountryId id, string unit, out Point at)
        {
            at = default;
            foreach (PoliticalParty p in PartySystems.For(id)) { if (p.Abbrev == unit && TryPoint(p, out at)) { return true; } }   // a key with no position (PSL's) falls through to the member's row
            if (id == CountryId.Poland)
            {
                foreach ((PoliticalParty member, int _) in PartySystems.PolandTdMembers) { if (member.Abbrev == unit) { return TryPoint(member, out at); } }
                foreach (PoliticalParty unseated in PartySystems.PolandUnseatedUnits) { if (unseated.Abbrev == unit) { return TryPoint(unseated, out at); } }
            }
            return false;
        }

        private static bool TryPoint(PoliticalParty p, out Point at)
        {
            at = default;
            if (float.IsNaN(p.Galtan) || float.IsNaN(p.Nationalism) || !p.HasEuPosition) { return false; }
            at = new Point(p.Galtan, p.Nationalism, p.EuPosition);
            return true;
        }

        private static Candidate Find(Contest contest, string name)
        {
            foreach (Candidate c in contest.Field) { if (c.Name == name) { return c; } }
            return null;
        }

        private static double Sq(double x) => x * x;
        /// <summary>CHES's EU position runs 1 (strongly against) to 7 (strongly for); on the 0-10 scale the other two axes use.</summary>
        private static double EuTen(double eu) => (eu - 1.0) / 6.0 * 10.0;
    }
}
