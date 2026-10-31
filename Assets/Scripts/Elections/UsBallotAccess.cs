using System;
using System.Collections.Generic;
using System.Globalization;
using PoliSim.Elections.Generated;

namespace PoliSim.Elections
{
    /// <summary>
    /// SP-6b (`START_POINTS_AND_PARTY_CREATION_SPEC.md` §2.6's second form, decision 10; R-US4 (a), RULED - F8) with Design's section 7 (the return of
    /// 2026-10-30): <b>AN INDEPENDENT PRESIDENTIAL CANDIDATE'S BALLOT ACCESS IN THE 51 JURISDICTIONS</b> - the rules, the drive and what the night reads of it.
    /// Pure: nothing here is drawn, saved or read from a clock.
    /// <list type="bullet">
    /// <item><description><b>The rules are sourced per jurisdiction</b> (<see cref="Rules"/>): NASS's "Summary: State Laws Regarding Presidential Ballot Access for
    /// the General Election" (updated July 2024), each row READ with the quote that carries it and held to the summary's text by `Tools/us_ballot_prep.pl`
    /// (`UsBallotAccess2024`). Where a count's base or a deadline's anchor is not on disk the rule is BILLED, never estimated: a BILLED jurisdiction cannot
    /// be filed in, so the candidate is not on its ballot (DECLARED: the game rules no one onto a ballot whose rule it does not hold). A year other than 2024
    /// holds no rules (BILLED: the summary is 2024's).</description></item>
    /// <item><description><b>The drive</b> (<see cref="Drive"/>, one <see cref="Petition"/> a jurisdiction): signatures gathered and whether the petition is
    /// filed. A jurisdiction is ON THE BALLOT once filed; LOST once its deadline passes unfiled ("a deadline passed short loses the state"); COLLECTING while
    /// signatures are gathered before the deadline; OFF before any; BILLED where its rule is (<see cref="StandingOf"/>).</description></item>
    /// <item><description><b>How signatures accrue</b> is the plan_prereg's decision rule (`docs/specs/USA_STAGE_PLAN.md`, SP-6b): only by GATHER, at a price per
    /// signature - and that price is BILLED (<see cref="PricePerSignatureUsd"/>: no primary source for it is on disk), so GATHER is drawn ABSENT until it is
    /// sourced; a fee that may be paid INSTEAD of the petition (Colorado's and Louisiana's $500) files without signatures.</description></item>
    /// <item><description><b>Not on the ballot gives no votes</b> (<see cref="Off"/>): where the candidate is not on a jurisdiction's ballot the candidate's
    /// share there is zero and its voters split as the rest of that state's vote does (the others renormalised) - the plan_prereg's rule (b).</description></item>
    /// <item><description><b>The night's reading</b> (<see cref="Night"/>): the independent's share by jurisdiction where on the ballot, drawn from every other
    /// column in proportion (DECLARED until a vote-share home on the two-party roster is sourced - R-US4 (a)'s third condition, BILLED), the electors it wins,
    /// and the nearest elector: the state, or Maine's or Nebraska's district, where its gap to the leader is smallest.</description></item>
    /// </list>
    /// </summary>
    public static class UsBallotAccess
    {
        /// <summary>Where a jurisdiction stands for the candidate on a day.</summary>
        public enum Standing { OnBallot, Collecting, Off, Lost, Billed }

        /// <summary>The independent's key on the night and in the slips - the board's own word.</summary>
        public const string Key = "IND";

        /// <summary>BILLED: no primary source for what a petition signature costs is on disk (the plan_prereg's rule prices GATHER by it). NaN until sourced;
        /// the HQ draws GATHER ABSENT while it is.</summary>
        public const double PricePerSignatureUsd = double.NaN;

        /// <summary>The words the slips carry for the price while it is BILLED.</summary>
        public const string PriceBilled = "no sourced price per signature";

        /// <summary>One jurisdiction's rule, as `UsBallotAccess2024` carries it.</summary>
        public sealed class Rule
        {
            public string State;
            public int Electors;
            /// <summary>The signatures the petition needs; -1 BILLED (<see cref="SignaturesBasis"/> says why).</summary>
            public int Signatures;
            public string SignaturesBasis;
            /// <summary>The last day to file; null BILLED.</summary>
            public DateTime? Deadline;
            public string DeadlineBasis;
            /// <summary>The fee in dollars; -1 where none is named or it is BILLED (<see cref="FeeRoute"/>).</summary>
            public int FeeUsd;
            /// <summary>add (a fee beside the petition), alt (a fee instead of it), none (the summary names none), billed.</summary>
            public string FeeRoute;
            public string FeeBasis;
            /// <summary>The summary's own words for the rule ("-" where the summary has none).</summary>
            public string Quote;

            public bool FeeInstead => FeeRoute == "alt" && FeeUsd > 0;
            /// <summary>Whether the rule cannot be applied: its deadline BILLED, or its count BILLED with no fee to pay instead.</summary>
            public bool Billed => !Deadline.HasValue || (Signatures < 0 && !FeeInstead);
        }

        /// <summary>The rules for the election on <paramref name="pollingDay"/>, in the catalog's order (the postal codes'); empty for a year not on disk.</summary>
        public static IReadOnlyList<Rule> Rules(DateTime pollingDay)
        {
            var rules = new List<Rule>();
            if (pollingDay.Date != new DateTime(2024, 11, 5)) { return rules; }
            foreach (var r in UsBallotAccess2024.Rows)
            {
                rules.Add(new Rule
                {
                    State = r.State, Electors = r.Electors, Signatures = r.Signatures, SignaturesBasis = r.SignaturesBasis,
                    Deadline = string.IsNullOrEmpty(r.Deadline) ? (DateTime?)null : DateTime.ParseExact(r.Deadline, "yyyy-MM-dd", CultureInfo.InvariantCulture),
                    DeadlineBasis = r.DeadlineBasis, FeeUsd = r.FeeUsd, FeeRoute = r.FeeRoute, FeeBasis = r.FeeBasis, Quote = r.Quote,
                });
            }
            return rules;
        }

        /// <summary>One jurisdiction's rule for <paramref name="pollingDay"/>, or null.</summary>
        public static Rule RuleOf(DateTime pollingDay, string state)
        {
            foreach (Rule r in Rules(pollingDay)) { if (r.State == state) { return r; } }
            return null;
        }

        /// <summary>One jurisdiction's petition: the signatures gathered, and whether (and on what day) it was filed.</summary>
        public sealed class Petition
        {
            public string State;
            public int Gathered;
            public bool Filed;
            public DateTime FiledOn;
        }

        /// <summary>The candidate's ballot drive for one election: a petition per jurisdiction the rules hold.</summary>
        public sealed class Drive
        {
            public DateTime PollingDay;
            public List<Petition> Petitions = new List<Petition>();

            public Petition Of(string state)
            {
                foreach (Petition p in Petitions) { if (p.State == state) { return p; } }
                return null;
            }
        }

        /// <summary>A new drive for <paramref name="pollingDay"/>: nothing gathered, nothing filed, in every jurisdiction the rules hold.</summary>
        public static Drive NewDrive(DateTime pollingDay)
        {
            var d = new Drive { PollingDay = pollingDay.Date };
            foreach (Rule r in Rules(pollingDay)) { d.Petitions.Add(new Petition { State = r.State }); }
            return d;
        }

        /// <summary>Where <paramref name="r"/>'s jurisdiction stands on <paramref name="today"/> for <paramref name="p"/>: filed is on the ballot whatever the day;
        /// a BILLED rule cannot be applied; past the deadline unfiled is lost; signatures gathered before it is collecting; else off.</summary>
        public static Standing StandingOf(Rule r, Petition p, DateTime today)
        {
            if (p != null && p.Filed) { return Standing.OnBallot; }
            if (r == null || r.Billed) { return Standing.Billed; }
            if (today.Date > r.Deadline.Value.Date) { return Standing.Lost; }
            return p != null && p.Gathered > 0 ? Standing.Collecting : Standing.Off;
        }

        /// <summary>Whether <paramref name="p"/> may be filed on <paramref name="today"/>, and why not: not twice, not where the rule is BILLED, not past the
        /// deadline; with the signatures the rule asks for, or by the fee that may be paid instead of them.</summary>
        public static bool CanFile(Rule r, Petition p, DateTime today, out string why)
        {
            why = null;
            if (r == null || p == null) { why = "no rule for this jurisdiction"; return false; }
            if (p.Filed) { why = "filed " + p.FiledOn.ToString("d MMM", CultureInfo.InvariantCulture); return false; }
            if (r.Billed) { why = "the rule is BILLED"; return false; }
            if (today.Date > r.Deadline.Value.Date) { why = "the deadline passed " + r.Deadline.Value.ToString("d MMM", CultureInfo.InvariantCulture); return false; }
            if (r.FeeInstead) { return true; }
            if (p.Gathered < r.Signatures) { why = (r.Signatures - p.Gathered).ToString("N0", CultureInfo.InvariantCulture) + " signatures short"; return false; }
            return true;
        }

        /// <summary>Files <paramref name="p"/> on <paramref name="today"/> where <see cref="CanFile"/> allows it.</summary>
        public static bool File(Rule r, Petition p, DateTime today, out string why)
        {
            if (!CanFile(r, p, today, out why)) { return false; }
            p.Filed = true;
            p.FiledOn = today.Date;
            return true;
        }

        /// <summary>What filing costs in dollars: the fee where one is named and sourced (beside the petition, or instead of it where the signatures fall short),
        /// 0 where the summary names none, NaN where it is BILLED - DECLARED: a BILLED fee is not charged, and its slip says so.</summary>
        public static double FilingFeeUsd(Rule r) => r == null ? double.NaN : r.FeeRoute == "billed" ? double.NaN : r.FeeUsd > 0 ? r.FeeUsd : 0.0;

        /// <summary>The share of the requirement gathered, 0-1; NaN where the count is BILLED.</summary>
        public static double Progress(Rule r, Petition p) => r == null || r.Signatures <= 0 ? double.NaN : Math.Min(1.0, (p?.Gathered ?? 0) / (double)r.Signatures);

        /// <summary>The electors by standing on <paramref name="today"/>: on the ballot, collecting, lost (off and BILLED are neither), and the count on the ballot.</summary>
        public static void Tally(Drive d, DateTime today, out int onElectors, out int collectingElectors, out int lostElectors, out int onCount)
        {
            onElectors = collectingElectors = lostElectors = onCount = 0;
            if (d == null) { return; }
            foreach (Rule r in Rules(d.PollingDay))
            {
                switch (StandingOf(r, d.Of(r.State), today))
                {
                    case Standing.OnBallot: onElectors += r.Electors; onCount++; break;
                    case Standing.Collecting: collectingElectors += r.Electors; break;
                    case Standing.Lost: lostElectors += r.Electors; break;
                }
            }
        }

        /// <summary>The next <paramref name="n"/> deadlines from <paramref name="today"/> among the jurisdictions still open (not filed, not lost, not BILLED),
        /// soonest first, the larger electors first on one day.</summary>
        public static List<Rule> NextDeadlines(Drive d, DateTime today, int n)
        {
            var open = new List<Rule>();
            if (d == null) { return open; }
            foreach (Rule r in Rules(d.PollingDay))
            {
                Standing st = StandingOf(r, d.Of(r.State), today);
                if (st == Standing.Collecting || st == Standing.Off) { open.Add(r); }
            }
            open.Sort((a, b) => a.Deadline.Value != b.Deadline.Value ? a.Deadline.Value.CompareTo(b.Deadline.Value) : b.Electors != a.Electors ? b.Electors.CompareTo(a.Electors) : string.CompareOrdinal(a.State, b.State));
            if (open.Count > n) { open.RemoveRange(n, open.Count - n); }
            return open;
        }

        // ------------------------------------------------------------------------------------------
        // The night's reading.
        // ------------------------------------------------------------------------------------------

        /// <summary>
        /// The plan_prereg's rule (b): <paramref name="shares"/> (any columns, fractions summing to the jurisdiction's whole) with column <paramref name="column"/>
        /// off the ballot - its share zero, every other column scaled up in proportion so the whole stands. A copy; the input is not touched.
        /// </summary>
        public static double[] Off(double[] shares, int column)
        {
            var o = (double[])shares.Clone();
            double rest = 0.0;
            for (int i = 0; i < o.Length; i++) { if (i != column) { rest += o[i]; } }
            double whole = rest + o[column];
            o[column] = 0.0;
            if (rest > 0.0) { for (int i = 0; i < o.Length; i++) { if (i != column) { o[i] *= whole / rest; } } }
            return o;
        }

        /// <summary>One place an elector is won at - a state (its winner takes its electors, or Maine's and Nebraska's two at large) or one of their districts
        /// ("ME-2") - with the independent's share and the leader's there.</summary>
        public sealed class Place
        {
            public string Code;
            public string State;
            public int District;
            public int Electors;
            public double Independent;
            public double Leader;
            public string LeaderKey;
            public bool OnBallot;
            /// <summary>The leader's lead over the independent in points (negative where the independent leads).</summary>
            public double GapPp => 100.0 * (Leader - Independent);
            public bool Won => OnBallot && Independent > Leader;
        }

        /// <summary>The night as the independent reads it: every place's shares, the electors won, the nearest elector, and the national share re-summed.</summary>
        public sealed class NightReading
        {
            public double NationalShare;
            /// <summary>The nominees' and every other candidate's national shares with the independent standing, re-summed as its own is.</summary>
            public double NationalRep, NationalDem, NationalOther;
            public int Electors;
            public int ElectorsRep, ElectorsDem;
            public List<Place> Places = new List<Place>();
            public List<string> NotOnBallot = new List<string>();
            /// <summary>The places not won where the candidate is on the ballot, the smallest gap first (the nearest elector first).</summary>
            public List<Place> Nearest = new List<Place>();
            public bool NoMajority => Math.Max(Electors, Math.Max(ElectorsRep, ElectorsDem)) < ElectoralCollege.MajorityToElect;
        }

        /// <summary>
        /// The independent on the count's states (<paramref name="states"/>, `UsPresidentialElection.StatesOf`): in every jurisdiction where it is on the ballot
        /// (<paramref name="onBallot"/>) it takes <paramref name="share"/> of the vote, drawn from every other column in proportion (DECLARED - the plan_prereg's
        /// rule (a) until a vote-share home is sourced), and nothing where it is not (rule (b)); Maine's and Nebraska's districts take their state's share and split
        /// the rest by their own two-party reading. A place goes to the larger of the independent and the larger nominee; the electors summed by the statutes'
        /// rule; the national share re-summed over the jurisdictions' votes cast in <paramref name="weightsYear"/>. Null where the states are null.
        /// </summary>
        public static NightReading Night(ElectoralCollege.StateCall[] states, double share, ICollection<string> onBallot, int weightsYear)
        {
            if (states == null) { return null; }
            var weights = new Dictionary<string, long>(StringComparer.Ordinal);
            foreach (var s in UsPresidentialReturns.States) { if (s.Year == weightsYear) { weights[s.State] = s.VotesTotal; } }
            var night = new NightReading();
            double sum = 0.0, total = 0.0, sumR = 0.0, sumD = 0.0, sumO = 0.0;
            foreach (ElectoralCollege.StateCall c in states)
            {
                bool on = onBallot != null && onBallot.Contains(c.State);
                double s = on ? share : 0.0;
                if (!on) { night.NotOnBallot.Add(c.State); }
                double d = c.Shares[ElectoralCollege.ShareDemocrat] * (1.0 - s), r = c.Shares[ElectoralCollege.ShareRepublican] * (1.0 - s);
                if (weights.TryGetValue(c.State, out long w)) { sum += w * s; sumR += w * r; sumD += w * d; sumO += w * c.Shares[ElectoralCollege.ShareOthers] * (1.0 - s); total += w; }
                int[] districts = c.Districts ?? new int[0];
                int atLarge = districts.Length > 0 ? c.Electors - districts.Length : c.Electors;
                var state = new Place { Code = c.State, State = c.State, Electors = atLarge, Independent = s, Leader = Math.Max(d, r), LeaderKey = r > d ? "REP" : "DEM", OnBallot = on };
                if (r == d) { state.LeaderKey = c.Winner == ElectoralCollege.Republican ? "REP" : "DEM"; }
                night.Places.Add(state);
                Count(night, state, c.Winner);
                for (int k = 0; k < districts.Length && k < (c.DistrictDemocraticTwoParty?.Length ?? 0) && k < (c.DistrictWinners?.Length ?? 0); k++)
                {
                    double nominees = (c.Shares[ElectoralCollege.ShareDemocrat] + c.Shares[ElectoralCollege.ShareRepublican]) * (1.0 - s);
                    double dd = nominees * c.DistrictDemocraticTwoParty[k], dr = nominees * (1.0 - c.DistrictDemocraticTwoParty[k]);
                    var district = new Place
                    {
                        Code = c.State + "-" + districts[k].ToString(CultureInfo.InvariantCulture), State = c.State, District = districts[k], Electors = 1,
                        Independent = s, Leader = Math.Max(dd, dr), LeaderKey = c.DistrictWinners[k] == ElectoralCollege.Republican ? "REP" : "DEM", OnBallot = on,
                    };
                    night.Places.Add(district);
                    Count(night, district, c.DistrictWinners[k]);
                }
            }
            night.NationalShare = total > 0.0 ? sum / total : double.NaN;
            night.NationalRep = total > 0.0 ? sumR / total : double.NaN;
            night.NationalDem = total > 0.0 ? sumD / total : double.NaN;
            night.NationalOther = total > 0.0 ? sumO / total : double.NaN;
            foreach (Place p in night.Places) { if (p.OnBallot && !p.Won) { night.Nearest.Add(p); } }
            night.Nearest.Sort((a, b) => a.GapPp != b.GapPp ? a.GapPp.CompareTo(b.GapPp) : string.CompareOrdinal(a.Code, b.Code));
            return night;
        }

        private static void Count(NightReading night, Place p, int nomineeWinner)
        {
            if (p.Won) { night.Electors += p.Electors; }
            else if (nomineeWinner == ElectoralCollege.Republican) { night.ElectorsRep += p.Electors; }
            else { night.ElectorsDem += p.Electors; }
        }

        /// <summary>The least share (to 0.01 point) at which the independent wins a place on <paramref name="states"/> with <paramref name="onBallot"/> - the
        /// share that closes the nearest gap; NaN where it is on no ballot.</summary>
        public static double LeastShareForOneElector(ElectoralCollege.StateCall[] states, ICollection<string> onBallot, int weightsYear)
        {
            if (states == null || onBallot == null || onBallot.Count == 0) { return double.NaN; }
            double lo = 0.0, hi = 1.0;
            for (int i = 0; i < 40 && hi - lo > 1e-4; i++)
            {
                double mid = 0.5 * (lo + hi);
                NightReading n = Night(states, mid, onBallot, weightsYear);
                if (n != null && n.Electors > 0) { hi = mid; } else { lo = mid; }
            }
            return Math.Ceiling(hi * 1e4) / 1e4;
        }

        /// <summary>
        /// For Code 4 (Design asked whether the game models it): where no candidate has a majority of the electors appointed, the House chooses by states under
        /// the Twelfth Amendment - "from the persons having the highest numbers not exceeding three" - one vote a state; and the game models it
        /// (`UsPresidentialElection.HoldHouseChoice`, R-US3 (a), RULED - F8; R-US22's Senate choice of the Vice President and the acting President, ruled I6).
        /// DECLARED: each delegation votes its party's nominee, so a candidate with no House members carries no state - an independent among the three is named
        /// and wins none (the statute allows it; the game's delegations do not choose it).
        /// </summary>
        public const string HouseContingency = "No one at 270: the House decides";
        public const string HouseContingencyRule = "12th Amendment · one vote a state";
    }
}
