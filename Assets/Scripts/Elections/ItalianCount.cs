using System;
using System.Collections.Generic;
using PoliSim.Elections.Generated;

namespace PoliSim.Elections
{
    /// <summary>
    /// PS-7 (Elias's rulings K150-K158, `docs/RULINGS_2026-10-23.md`): <b>ITALY'S CAMERA AND SENATO BY THE STATUTES IN FORCE ON 25 SEP 2022</b>, counted on the
    /// generated catalog (<see cref="ItalianCount2022"/>, `Tools/it_count_prep.pl`; the readings it rests on are declared in the generator's header). Pure.
    /// <para><b>The game's vote onto the colleges</b> (K150): a uniform swing from 2018's comune returns re-aggregated onto the 2020 colleges - each key's share
    /// in each college moves by its national change since 2018 (2018's national shares read on the lineage-mapped lists' own sum, the same base the colleges'
    /// shares are read on), floored at zero, times the college's 2018 size - the reading Poland's okregi took (`PolishSejmAllocation`). A key with no 2018
    /// lineage (AzIV, IC, ScN) takes its national share in every college; MAIE stands only abroad and UV only in Valle d'Aosta, so neither is spread over the
    /// colleges (DECLARED). A college with no 2018 list votes (Valle d'Aosta, both chambers - its candidates only, both years) is not counted from the shares:
    /// its seat stays the record's 2022 winner's (DECLARED, the generator's reading (4)).</para>
    /// <para><b>The uninominal colleges</b> (DPR 361/1957 art. 77 c.1 lett. a/b; D.Lgs. 533/1993 art. 16): the candidate with the most votes; a candidate's votes
    /// are the votes of the lists linked to it - the college's 2022 links, fixed (K152, `ItalianCount2022.CameraLinks`). A tie goes to the earlier group in the
    /// record's order (the statute: the younger candidate - stated, not modelled). <b>Which list a college seat counts for</b> (DECLARED, asked): no official
    /// source assigns a winner to a list (SEAT-1's J2); the 2022 winner's own party (`ItalianCount2022.CameraWinnerParty2022`, from the candidate's list
    /// candidacies and parliamentary group of record) is carried where it stands in the group that wins the college now - the coalition's candidacy agreement by
    /// college, DECLARED; any other college of a group is shared among its lists on their votes in the colleges it won (`AssignCollegeSeats`).</para>
    /// <para><b>The Camera's proportional seats</b>: the national stage (art. 83 lett. b-g, <see cref="Rosatellum.AllocateNational"/>), then the circoscrizione
    /// stage (K151: lett. h for the coalitions and single lists, lett. i for each coalition's lists, each with its compensation for seats in excess); art.
    /// 83-bis (the collegi plurinominali) and the art. 84 cascade are NOT built (K151) - they change which deputies sit, never a list's seats. <b>The Senato's</b>
    /// (D.Lgs. 533/1993 art. 16-bis, 17 c.1 lett. a/b): the national thresholds with the 20 % regional route, then each region's seats by itself; art. 17 lett. c
    /// (the region's collegi plurinominali) is not built, for the same reason. <b>The Estero</b> (K158; L. 459/2001 art. 15): each ripartizione's 2022 list
    /// votes swung by the lists' national change since 2022, the floored quotient and the largest remainders among all its lists; a joint list's seats go to
    /// its members by d'Hondt on their national shares (DECLARED).</para>
    /// </summary>
    public static class ItalianCount
    {
        public enum Chamber { Camera, Senato }

        /// <summary>SOURCED - DPR 361/1957 art. 83 c.1 lett. e) n.2 and D.Lgs. 533/1993 art. 16-bis lett. e): the regional line a list can clear instead of the national 3 % (the Senato), and the share of its region a recognised-minority list clears.</summary>
        public const double RegionalLine = 0.20;

        /// <summary>The recognised-minority list (lett. e n.2): presented only in Trentino-Alto Adige, whose statute protects its minority - the SVP-PATT list (DECLARED: the 2022 record's one such list).</summary>
        public const string MinorityKey = "SVP";

        /// <summary>The keys that stand in no Italian college: MAIE abroad only, UV in Valle d'Aosta only (DECLARED).</summary>
        private static bool StandsInColleges(string key) => key != "MAIE" && key != "UV";

        /// <summary>One chamber's count: the seats per roster key and how they were won.</summary>
        public sealed class Result
        {
            public Chamber Of;
            public Dictionary<string, int> Seats = new Dictionary<string, int>();
            public Dictionary<string, int> CollegeSeats = new Dictionary<string, int>();
            public Dictionary<string, int> ProportionalSeats = new Dictionary<string, int>();
            public Dictionary<string, int> EsteroSeats = new Dictionary<string, int>();
            /// <summary>Each college's winning group (the keys of its linked lists, '+' between them) and the key its seat is counted for.</summary>
            public string[] CollegeWinner;
            public string[] CollegeSeatKey;
            /// <summary>The proportional seats per circoscrizione (Camera) or region (Senato) and key - rows the catalog's order, columns <see cref="ItalianCount2022.Keys"/>.</summary>
            public int[][] ByCircoscrizione;
            /// <summary>Estero seats won by a list the roster does not carry - held by no key, and missing from <see cref="Seats"/>' sum.</summary>
            public int EsteroUnkeyed;
            public string Trace;
        }

        private static int K(string[] keys, string key) => Array.IndexOf(keys, key);

        /// <summary>The columns a count on <paramref name="shares"/> carries: the catalog's keys (<see cref="ItalianCount2022.Keys"/>), then every other key the
        /// shares hold a vote for, in ordinal order - a roster unit the 2022 lists did not carry, a party the player created. Such a key has no 2018 lineage and
        /// no 2022 link: it takes its national share in every college, stands alone in each, joins no coalition and has no list abroad (DECLARED).</summary>
        public static string[] KeysFor(IReadOnlyDictionary<string, double> shares)
        {
            var extra = new List<string>();
            if (shares != null) { foreach (KeyValuePair<string, double> kv in shares) { if (kv.Value > 0.0 && Array.IndexOf(ItalianCount2022.Keys, kv.Key) < 0) { extra.Add(kv.Key); } } }
            extra.Sort(StringComparer.Ordinal);
            var keys = new string[ItalianCount2022.Keys.Length + extra.Count];
            ItalianCount2022.Keys.CopyTo(keys, 0);
            extra.CopyTo(keys, ItalianCount2022.Keys.Length);
            return keys;
        }

        private static long[][] Base2018(Chamber c) => c == Chamber.Camera ? ItalianCount2022.Camera2018 : ItalianCount2022.Senato2018;
        private static string[] Links(Chamber c) => c == Chamber.Camera ? ItalianCount2022.CameraLinks : ItalianCount2022.SenatoLinks;
        private static string[] Winner2022(Chamber c) => c == Chamber.Camera ? ItalianCount2022.CameraWinner2022 : ItalianCount2022.SenatoWinner2022;
        private static int[] CollegeCirc(Chamber c) => c == Chamber.Camera ? ItalianCount2022.CameraCollegeCircoscrizione : ItalianCount2022.SenatoCollegeCircoscrizione;
        private static int[] CircSeats(Chamber c) => c == Chamber.Camera ? ItalianCount2022.CameraCircoscrizioneSeats : ItalianCount2022.SenatoCircoscrizioneSeats;
        public static string[] Circoscrizioni(Chamber c) => c == Chamber.Camera ? ItalianCount2022.CameraCircoscrizioni : ItalianCount2022.SenatoCircoscrizioni;

        /// <summary>
        /// K150: the votes each college counts per key (rows the catalog's colleges, columns <see cref="ItalianCount2022.Keys"/>) for the national
        /// <paramref name="shares"/> (fractions of the valid vote, keyed by the roster's abbreviations) - the swing this class's head declares. A college with
        /// no 2018 list votes has none (its seat is the record's).
        /// </summary>
        public static double[][] SwungVotes(Chamber chamber, IReadOnlyDictionary<string, double> shares, out string[] keys)
        {
            keys = KeysFor(shares);
            long[][] v18 = Base2018(chamber);
            int colleges = v18.Length, nk = keys.Length, nc = ItalianCount2022.Keys.Length;
            var size = new long[colleges];
            var national2018 = new double[nk];
            long sum = 0;
            for (int c = 0; c < colleges; c++) { for (int k = 0; k < nc; k++) { size[c] += v18[c][k]; national2018[k] += v18[c][k]; } sum += size[c]; }
            for (int k = 0; k < nc; k++) { national2018[k] = sum > 0 ? national2018[k] / sum : 0.0; }   // a key past the catalog's has no lineage: zero
            var result = new double[colleges][];
            for (int c = 0; c < colleges; c++)
            {
                result[c] = new double[nk];
                if (size[c] <= 0) { continue; }
                for (int k = 0; k < nk; k++)
                {
                    if (!StandsInColleges(keys[k]) || shares == null || !shares.TryGetValue(keys[k], out double national)) { continue; }
                    double local = national2018[k] > 0.0 ? Math.Max(0.0, (double)v18[c][k] / size[c] + (national - national2018[k])) : Math.Max(0.0, national);
                    if (local > 0.0) { result[c][k] = local * size[c]; }
                }
            }
            // M3 / K205: the floor at zero adds votes where a falling party's 2018 base was uneven - each key's colleges are scaled back to its national share of
            // the colleges' whole size, so the geography is the swing's and the national total the share handed (exact where no floor bit: the 2018 identity)
            for (int k = 0; k < nk; k++)
            {
                if (!StandsInColleges(keys[k]) || shares == null || !shares.TryGetValue(keys[k], out double national)) { continue; }
                double counted = 0.0;
                for (int c = 0; c < colleges; c++) { counted += result[c][k]; }
                if (!(counted > 0.0)) { continue; }
                double factor = national * sum / counted;
                if (Math.Abs(factor - 1.0) < 1e-12) { continue; }
                for (int c = 0; c < colleges; c++) { result[c][k] *= factor; }
            }
            return result;
        }

        /// <summary>Lett. a/b: each college's winning group and the key its seat counts for, on <paramref name="votes"/> (per college, per key). The seat's key: `AssignCollegeSeats`. A college
        /// whose votes are all zero keeps the record's 2022 winner (Valle d'Aosta), its seat counted for the group's key with the largest
        /// <paramref name="nationalShares"/> (DECLARED; the first key where none is handed).</summary>
        public static void Colleges(Chamber chamber, string[] keys, double[][] votes, IReadOnlyDictionary<string, double> nationalShares, out string[] winner, out string[] seatKey)
        {
            string[] links = Links(chamber);
            winner = new string[links.Length];
            seatKey = new string[links.Length];
            for (int c = 0; c < links.Length; c++)
            {
                double all = 0.0;
                foreach (double v in votes[c]) { all += v; }
                if (!(all > 0.0))
                {
                    winner[c] = Winner2022(chamber)[c];
                    string best = null; double bestShare = double.NegativeInfinity;
                    foreach (string key in winner[c].Split('+'))
                    {
                        double s = nationalShares != null && nationalShares.TryGetValue(key, out double x) ? x : 0.0;
                        if (best == null || s > bestShare) { best = key; bestShare = s; }
                    }
                    seatKey[c] = best;
                    continue;
                }
                string top = null; double topVotes = -1.0;
                var groups = new List<string>(links[c].Split('|'));
                for (int k = ItalianCount2022.Keys.Length; k < keys.Length; k++) { if (votes[c][k] > 0.0) { groups.Add(keys[k]); } }   // KeysFor: a key past the catalog's stands alone
                foreach (string group in groups)
                {
                    double gv = 0.0;
                    foreach (string key in group.Split('+')) { int k = K(keys, key); if (k >= 0) { gv += votes[c][k]; } }
                    if (gv > topVotes) { top = group; topVotes = gv; }
                }
                winner[c] = top;
            }
            string[] party = chamber == Chamber.Camera ? ItalianCount2022.CameraWinnerParty2022 : ItalianCount2022.SenatoWinnerParty2022;
            for (int c = 0; c < links.Length; c++)
            {
                // the 2022 candidacy agreement, carried (DECLARED): a college the record's winner's party stands in, inside the group that wins it now, is that
                // party's - the coalition's candidate there was its own (the generator's reading (6): list candidacies and parliamentary groups of record)
                if (seatKey[c] != null || string.IsNullOrEmpty(party[c])) { continue; }
                if (Array.IndexOf(winner[c].Split('+'), party[c]) >= 0) { seatKey[c] = party[c]; }
            }
            AssignCollegeSeats(keys, links.Length, votes, winner, seatKey);
        }

        /// <summary>
        /// Which list each counted college's seat is counted for (DECLARED, asked - no official source assigns a winner to a list, SEAT-1's J2): a group's college
        /// seats are shared among its lists by the floored quotient and the largest remainders on their votes in the colleges the group won (lett. g's operation,
        /// borrowed), and each college is then given to the list with the largest share of the group's vote there whose quota is not yet filled, the largest
        /// shares first. A group of one list takes its colleges whole.
        /// </summary>
        private static void AssignCollegeSeats(string[] keys, int colleges, double[][] votes, string[] winner, string[] seatKey)
        {
            var byGroup = new Dictionary<string, List<int>>();
            for (int c = 0; c < colleges; c++)
            {
                if (seatKey[c] != null) { continue; }   // held by record
                if (!byGroup.TryGetValue(winner[c], out List<int> list)) { byGroup[winner[c]] = list = new List<int>(); }
                list.Add(c);
            }
            var groups = new List<string>(byGroup.Keys);
            groups.Sort(StringComparer.Ordinal);
            foreach (string group in groups)
            {
                List<int> won = byGroup[group];
                string[] members = group.Split('+');
                var memberVotes = new long[members.Length];
                for (int m = 0; m < members.Length; m++) { int k = K(keys, members[m]); if (k < 0) { continue; } foreach (int c in won) { memberVotes[m] += (long)Math.Round(votes[c][k]); } }
                int[] quota = members.Length == 1 ? new[] { won.Count } : Rosatellum.FlooredHare(memberVotes, won.Count, out long _);
                var pairs = new List<(double Share, int College, int Member)>();
                foreach (int c in won)
                {
                    double gv = 0.0;
                    foreach (string key in members) { int k = K(keys, key); if (k >= 0) { gv += votes[c][k]; } }
                    for (int m = 0; m < members.Length; m++) { int k = K(keys, members[m]); pairs.Add((gv > 0.0 && k >= 0 ? votes[c][k] / gv : 0.0, c, m)); }
                }
                pairs.Sort((a, b) => b.Share != a.Share ? b.Share.CompareTo(a.Share) : a.College != b.College ? a.College.CompareTo(b.College) : a.Member.CompareTo(b.Member));
                foreach ((double _, int c, int m) in pairs)
                {
                    if (seatKey[c] != null || quota[m] <= 0) { continue; }
                    seatKey[c] = members[m];
                    quota[m]--;
                }
                foreach (int c in won) { if (seatKey[c] == null) { seatKey[c] = members[0]; } }   // a quota the floored quotient left short (none where the remainders fill it)
            }
        }

        /// <summary>The record's 2022 national coalitions (K152): every published uninominal entry with more than one list (Eligendo's own grouping).</summary>
        public static List<string[]> Coalitions(Chamber chamber)
        {
            var list = new List<string[]>();
            foreach ((int _, string lists) in chamber == Chamber.Camera ? ItalianCount2022.CameraPublished2022Uninominal : ItalianCount2022.SenatoPublished2022Uninominal)
            {
                string[] members = lists.Split('+');
                if (members.Length > 1) { list.Add(members); }
            }
            return list;
        }

        private static int CoalitionOf(List<string[]> coalitions, string key)
        {
            for (int i = 0; i < coalitions.Count; i++) { if (Array.IndexOf(coalitions[i], key) >= 0) { return i; } }
            return -1;
        }

        /// <summary>
        /// The Camera on the national <paramref name="shares"/>: the colleges on the swung votes, the proportional seats by art. 83, the Estero by
        /// L. 459/2001 art. 15 - every roster key of <paramref name="shares"/> in the result.
        /// </summary>
        public static Result Camera(IReadOnlyDictionary<string, double> shares, double rosterShareOfVote = 1.0)
        {
            double[][] votes = SwungVotes(Chamber.Camera, shares, out string[] keys);
            return Count(Chamber.Camera, keys, votes, null, shares, EsteroVotes(Chamber.Camera, shares), rosterShareOfVote);
        }

        /// <summary>The Senato on the national <paramref name="shares"/>, as <see cref="Camera"/>: the elected senators only (the senators for life are seated by record, K154).</summary>
        public static Result Senato(IReadOnlyDictionary<string, double> shares, double rosterShareOfVote = 1.0)
        {
            double[][] votes = SwungVotes(Chamber.Senato, shares, out string[] keys);
            return Count(Chamber.Senato, keys, votes, null, shares, EsteroVotes(Chamber.Senato, shares), rosterShareOfVote);
        }

        /// <summary>L3' (the delta review): the Senato's own roster share of its valid vote at the 2022 election of record (Eligendo's Senato page, the catalog) - the
        /// base the Senato's lines read on that polling day, as `RecordFactors.RosterShareOfVote` is the Camera's (GATE-18's factors are fitted on the Camera).</summary>
        public static double SenatoRosterShare2022
        {
            get { long roster = 0; foreach (long v in ItalianCount2022.SenatoPublished2022Votes) { roster += v; } return ItalianCount2022.SenatoPublished2022Valid > 0 ? (double)roster / ItalianCount2022.SenatoPublished2022Valid : 1.0; }
        }

        /// <summary>K154: the key a senator for life's group is seated as - "Per le Autonomie (SVP-Patt, ...)" as SVP, the list whose name it bears (as SEAT-1
        /// seated the Camera's Minoranze linguistiche, DECLARED, asked); the Misto, and any other group, as the mixed group's key.</summary>
        public static string SenatorForLifeKey(string group) => group == "Aut" ? MinorityKey : "Misto";

        /// <summary>
        /// K154: the senators for life a Senato whose term first sits on <paramref name="firstSitting"/> seats, by record - each whose mandate the record has open
        /// that day (a senator for life's mandate runs past the XIX legislature unless the record ends it - DECLARED), in the group the record has them in that
        /// day, else the first group the record gives them (a group joined after the first sitting - Rubbia's of 9 Nov 2022 - DECLARED: seated in it from the
        /// first sitting; a later change of group is not followed). Keyed by <see cref="SenatorForLifeKey"/>.
        /// </summary>
        public static Dictionary<string, int> SenatorsForLifeOn(DateTime firstSitting)
        {
            int day = firstSitting.Year * 10000 + firstSitting.Month * 100 + firstSitting.Day;
            var seats = new Dictionary<string, int>();
            foreach ((string name, int _, int until) in ItalianCount2022.SenatorsForLife)
            {
                if (until != 0 && until <= day) { continue; }
                string group = null; int groupFrom = int.MaxValue; string first = null; int firstFrom = int.MaxValue;
                foreach ((string who, string g, int gFrom, int gUntil) in ItalianCount2022.SenatorsForLifeGroups)
                {
                    if (who != name) { continue; }
                    if (gFrom < firstFrom) { first = g; firstFrom = gFrom; }
                    if (gFrom <= day && (gUntil == 0 || gUntil > day) && (group == null || gFrom > groupFrom)) { group = g; groupFrom = gFrom; }
                }
                string key = SenatorForLifeKey(group ?? first);
                seats.TryGetValue(key, out int n);
                seats[key] = n + 1;
            }
            return seats;
        }

        /// <summary>K153, K154: the Senato the count seats - the elected senators (<see cref="Senato"/>) and the senators for life beside them
        /// (<see cref="SenatorsForLifeOn"/>), every key of <paramref name="shares"/> held (zero where it won nothing).</summary>
        public static Dictionary<string, int> SenatoSeated(IReadOnlyDictionary<string, double> shares, DateTime firstSitting, double rosterShareOfVote = 1.0)
        {
            var seats = new Dictionary<string, int>(Senato(shares, rosterShareOfVote).Seats);
            foreach (KeyValuePair<string, int> kv in SenatorsForLifeOn(firstSitting)) { seats.TryGetValue(kv.Key, out int n); seats[kv.Key] = n + kv.Value; }
            return seats;
        }

        /// <summary>
        /// The count on per-college votes (<paramref name="votes"/>, per key) and per-college votes of the lists no key carries (<paramref name="otherVotes"/>,
        /// null for none - the game's lists are the whole vote), the Estero on <paramref name="estero"/> (per ripartizione, per Estero list; null for none). The
        /// backtest feeds the record's 2022 cifre here. M3 / K205: <paramref name="rosterShareOfVote"/> is the roster's share of the valid vote on the polling day of
        /// an election of record (GATE-18's `RecordFactors.RosterShareOfVote`), one on every other day - where no other votes are handed, the lists the roster does
        /// not carry are the rest of every base the statutes read (the 3 % and 10 % lines, the 20 % regional line, the quotients' totals), spread over each
        /// circoscrizione in proportion to its counted votes (DECLARED).
        /// </summary>
        public static Result Count(Chamber chamber, string[] keys, double[][] votes, long[] otherVotes, IReadOnlyDictionary<string, double> nationalShares, double[][] estero, double rosterShareOfVote = 1.0)
        {
            var r = new Result { Of = chamber };
            var log = new System.Text.StringBuilder();
            foreach (string key in keys) { r.Seats[key] = 0; r.CollegeSeats[key] = 0; r.ProportionalSeats[key] = 0; r.EsteroSeats[key] = 0; }
            if (nationalShares != null) { foreach (KeyValuePair<string, double> kv in nationalShares) { if (!r.Seats.ContainsKey(kv.Key)) { r.Seats[kv.Key] = 0; } } }

            Colleges(chamber, keys, votes, nationalShares, out r.CollegeWinner, out r.CollegeSeatKey);
            foreach (string seat in r.CollegeSeatKey) { r.CollegeSeats[seat]++; }

            // the circoscrizione (region) cifre: the sum of their colleges' votes; a circoscrizione with no list votes takes no proportional seat (Valle d'Aosta)
            string[] circs = Circoscrizioni(chamber);
            int[] collegeCirc = CollegeCirc(chamber);
            int nk = keys.Length;
            var cifra = new long[circs.Length][];
            var other = new long[circs.Length];
            var collegesIn = new int[circs.Length];
            for (int i = 0; i < circs.Length; i++) { cifra[i] = new long[nk]; }
            for (int c = 0; c < votes.Length; c++)
            {
                int i = collegeCirc[c];
                collegesIn[i]++;
                for (int k = 0; k < nk; k++) { cifra[i][k] += (long)Math.Round(votes[c][k]); }
                if (otherVotes != null) { other[i] += otherVotes[c]; }
            }
            if (otherVotes == null && rosterShareOfVote > 0.0 && rosterShareOfVote < 1.0)
            {
                for (int i = 0; i < circs.Length; i++) { long counted = 0; foreach (long v in cifra[i]) { counted += v; } other[i] = (long)Math.Round(counted * (1.0 / rosterShareOfVote - 1.0)); }
            }
            var propSeats = new int[circs.Length];
            for (int i = 0; i < circs.Length; i++) { propSeats[i] = CircSeats(chamber)[i] - collegesIn[i]; }

            int[] minorityWins = MinorityWinsByCirc(chamber, r.CollegeWinner);   // lett. e n.2 and art. 16-bis: the minority list's college wins, its second route
            r.ByCircoscrizione = chamber == Chamber.Camera ? CameraProportional(keys, cifra, other, propSeats, minorityWins, log) : SenatoProportional(keys, cifra, other, propSeats, minorityWins, log);
            foreach (int[] row in r.ByCircoscrizione) { for (int k = 0; k < nk; k++) { r.ProportionalSeats[keys[k]] += row[k]; } }

            if (estero != null) { r.EsteroUnkeyed = Estero(chamber, estero, nationalShares, r.EsteroSeats, log); }
            foreach (string key in keys) { r.Seats[key] = r.CollegeSeats[key] + r.ProportionalSeats[key] + r.EsteroSeats[key]; }
            r.Trace = log.ToString();
            return r;
        }

        /// <summary>Whether the minority list qualifies by lett. e n.2 / art. 16-bis: at least a fifth of its circoscrizione's valid votes, or its candidates elected
        /// in at least a quarter (rounded up) of that circoscrizione's colleges - read on the circoscrizione where it stands (where its votes are).</summary>
        private static bool MinorityQualifies(string[] keys, long[][] cifra, long[] other, int[] collegesIn, int[] minorityWins)
        {
            int m = K(keys, MinorityKey);
            for (int i = 0; i < cifra.Length; i++)
            {
                if (cifra[i][m] <= 0) { continue; }
                long all = other[i];
                foreach (long v in cifra[i]) { all += v; }
                if (all > 0 && cifra[i][m] >= RegionalLine * all) { return true; }
                if (minorityWins != null && collegesIn[i] > 0 && minorityWins[i] >= (collegesIn[i] + 3) / 4) { return true; }
            }
            return false;
        }

        // ------------------------------------------------------------------------------------------------------------------------------- the Camera

        private static int[][] CameraProportional(string[] keys, long[][] cifra, long[] other, int[] propSeats, int[] minorityWins, System.Text.StringBuilder log)
        {
            int nk = keys.Length, circs = cifra.Length;
            List<string[]> coalitions = Coalitions(Chamber.Camera);
            var national = new long[nk];
            long valid = 0;
            for (int i = 0; i < circs; i++) { if (propSeats[i] <= 0) { continue; } for (int k = 0; k < nk; k++) { national[k] += cifra[i][k]; valid += cifra[i][k]; } valid += other[i]; }
            int pool = 0;
            for (int i = 0; i < circs; i++) { pool += Math.Max(0, propSeats[i]); }

            // lett. e n.2: the minority route - a fifth of its circoscrizione, or its college wins
            bool minority = MinorityQualifies(keys, cifra, other, CollegesPerCirc(Chamber.Camera), minorityWins);
            var entries = new Rosatellum.ListEntry[nk];
            for (int k = 0; k < nk; k++) { entries[k] = new Rosatellum.ListEntry(keys[k], national[k], CoalitionOf(coalitions, keys[k]), keys[k] == MinorityKey && minority); }
            int[] listSeats = Rosatellum.AllocateNational(entries, valid, pool, out string trace);
            log.Append("CAMERA national stage (art. 83 lett. b-g), pool ").Append(pool).Append(":\n").Append(trace);

            // the entities lett. f) allocated: each admitted coalition (its lists' seats summed) and each list seated alone
            var entityKeys = new List<int[]>();
            var entitySeats = new List<int>();
            var entityNational = new List<long>();
            var done = new bool[nk];
            for (int k = 0; k < nk; k++)
            {
                if (done[k]) { continue; }
                int co = CoalitionOf(coalitions, keys[k]);
                var members = new List<int>();
                if (co >= 0) { foreach (string m in coalitions[co]) { int mk = K(keys, m); if (mk >= 0) { members.Add(mk); done[mk] = true; } } }
                else { members.Add(k); done[k] = true; }
                int seats = 0; long fig = 0;
                foreach (int mk in members) { seats += listSeats[mk]; fig += national[mk]; }
                if (seats <= 0) { continue; }
                entityKeys.Add(members.ToArray()); entitySeats.Add(seats); entityNational.Add(fig);
            }

            // lett. h: the entities into the circoscrizioni - each circoscrizione's figure per entity is its admitted lists' cifre (lett. h reads the
            // coalition's cifra circoscrizionale; DECLARED: the coalition's cifra as lett. c) forms it - the lists below 1 % struck)
            double onePercent = Rosatellum.MinorityCoalitionFloor * valid;
            int entities = entitySeats.Count;
            var fig2 = new long[entities][];
            for (int e = 0; e < entities; e++)
            {
                fig2[e] = new long[circs];
                for (int i = 0; i < circs; i++)
                {
                    foreach (int mk in entityKeys[e])
                    {
                        bool strike = entityKeys[e].Length > 1 && national[mk] < onePercent && !(keys[mk] == MinorityKey && minority);
                        if (!strike) { fig2[e][i] += cifra[i][mk]; }
                    }
                }
            }
            int[][] entityByCirc = Distribute(fig2, propSeats, entitySeats.ToArray(), entityNational.ToArray(), log, "lett. h");

            // lett. i: each coalition's seats in each circoscrizione among its lists seated at lett. g
            var result = new int[circs][];
            for (int i = 0; i < circs; i++) { result[i] = new int[nk]; }
            for (int e = 0; e < entities; e++)
            {
                int[] members = entityKeys[e];
                if (members.Length == 1) { for (int i = 0; i < circs; i++) { result[i][members[0]] = entityByCirc[e][i]; } continue; }
                var seated = new List<int>();
                foreach (int mk in members) { if (listSeats[mk] > 0 || national[mk] >= Rosatellum.ListThreshold * valid || (keys[mk] == MinorityKey && minority)) { seated.Add(mk); } }   // L2: lett. i's quotient over the lists admitted by lett. g
                var lf = new long[seated.Count][];
                var targets = new int[seated.Count];
                var nat = new long[seated.Count];
                for (int j = 0; j < seated.Count; j++)
                {
                    lf[j] = new long[circs];
                    for (int i = 0; i < circs; i++) { lf[j][i] = cifra[i][seated[j]]; }
                    targets[j] = listSeats[seated[j]];
                    nat[j] = national[seated[j]];
                }
                int[][] listByCirc = Distribute(lf, entityByCirc[e], targets, nat, log, "lett. i");
                for (int j = 0; j < seated.Count; j++) { for (int i = 0; i < circs; i++) { result[i][seated[j]] = listByCirc[j][i]; } }
            }
            return result;
        }

        private static int[] CollegesPerCirc(Chamber chamber)
        {
            var n = new int[Circoscrizioni(chamber).Length];
            foreach (int i in CollegeCirc(chamber)) { n[i]++; }
            return n;
        }

        /// <summary>
        /// Lett. h and lett. i's operation (the same words in both): units (<paramref name="figure"/>, per unit per circoscrizione) share each circoscrizione's
        /// <paramref name="seatsIn"/> by the floored quotient over the units' figures there, integer parts, then the largest decimals - a unit whose integer
        /// parts across every circoscrizione already reach its national seats (<paramref name="target"/>) excluded from the decimals; ties to the larger national
        /// figure. Then the compensation: from the unit with the most seats in excess (ties to the larger national figure), a seat it won on a decimal goes, in
        /// the ascending order of its decimals, to a unit short of its seats with an unused decimal in the same circoscrizione (the highest such decimal; ties to
        /// the larger national figure); where no circoscrizione allows it, the excess unit gives up the seat of its smallest decimal and the short unit with the
        /// largest unused decimal anywhere takes one there (the statute's last resort, read as one seat at a time - DECLARED). Returns per unit per circoscrizione.
        /// </summary>
        public static int[][] Distribute(long[][] figure, int[] seatsIn, int[] target, long[] nationalFigure, System.Text.StringBuilder log, string stage)
        {
            int units = figure.Length, circs = seatsIn.Length;
            var seats = new int[units][];
            var dec = new double[units][];
            var byDecimal = new bool[units][];
            for (int u = 0; u < units; u++) { seats[u] = new int[circs]; dec[u] = new double[circs]; byDecimal[u] = new bool[circs]; }
            var integers = new int[units];
            for (int i = 0; i < circs; i++)
            {
                if (seatsIn[i] <= 0) { continue; }
                long sum = 0;
                for (int u = 0; u < units; u++) { sum += figure[u][i]; }
                long q = sum / seatsIn[i];
                if (q <= 0) { continue; }
                for (int u = 0; u < units; u++)
                {
                    double a = (double)figure[u][i] / q;
                    seats[u][i] = (int)Math.Floor(a);
                    dec[u][i] = a - seats[u][i];
                    integers[u] += seats[u][i];
                }
            }
            for (int i = 0; i < circs; i++)
            {
                if (seatsIn[i] <= 0) { continue; }
                int given = 0;
                for (int u = 0; u < units; u++) { given += seats[u][i]; }
                var taken = new bool[units];
                while (given < seatsIn[i])
                {
                    int best = -1;
                    for (int u = 0; u < units; u++)
                    {
                        if (taken[u] || integers[u] >= target[u] || figure[u][i] <= 0) { continue; }
                        if (best < 0 || dec[u][i] > dec[best][i] || (dec[u][i] == dec[best][i] && nationalFigure[u] > nationalFigure[best])) { best = u; }
                    }
                    if (best < 0)
                    {
                        // every unit excluded or taken: the remaining seats by decimals among all - the compensation below corrects any excess
                        for (int u = 0; u < units; u++)
                        {
                            if (taken[u] || figure[u][i] <= 0) { continue; }
                            if (best < 0 || dec[u][i] > dec[best][i] || (dec[u][i] == dec[best][i] && nationalFigure[u] > nationalFigure[best])) { best = u; }
                        }
                        if (best < 0) { break; }
                    }
                    seats[best][i]++; byDecimal[best][i] = true; taken[best] = true; given++;
                }
            }

            int Total(int u) { int t = 0; for (int i = 0; i < circs; i++) { t += seats[u][i]; } return t; }
            int moves = 0;
            // L2: the statute fixes the order once - "iniziando dalla coalizione ... che abbia il maggior numero di seggi eccedenti ... proseguendo poi con le altre ...
            // in ordine decrescente di seggi eccedenti" (ties to the larger national figure) - and each unit gives up all its excess before the next
            var excessOrder = new List<int>();
            for (int u = 0; u < units; u++) { if (Total(u) > target[u]) { excessOrder.Add(u); } }
            excessOrder.Sort((a, b) => { int ea = Total(a) - target[a], eb = Total(b) - target[b]; return ea != eb ? eb.CompareTo(ea) : nationalFigure[a] != nationalFigure[b] ? nationalFigure[b].CompareTo(nationalFigure[a]) : a.CompareTo(b); });
            foreach (int excess in excessOrder)
            {
                for (int guard = 0; guard < 10000 && Total(excess) > target[excess]; guard++)
                {
                    var order = new List<int>();
                    for (int i = 0; i < circs; i++) { if (byDecimal[excess][i] && seats[excess][i] > 0) { order.Add(i); } }
                    order.Sort((a, b) => dec[excess][a] != dec[excess][b] ? dec[excess][a].CompareTo(dec[excess][b]) : a.CompareTo(b));   // a tie to the earlier circoscrizione (the statute draws lots - stated)
                    bool moved = false;
                    foreach (int i in order)
                    {
                        int to = -1;
                        for (int u = 0; u < units; u++)
                        {
                            if (u == excess || Total(u) >= target[u] || byDecimal[u][i] || !(dec[u][i] > 0.0)) { continue; }
                            if (to < 0 || dec[u][i] > dec[to][i] || (dec[u][i] == dec[to][i] && nationalFigure[u] > nationalFigure[to])) { to = u; }
                        }
                        if (to < 0) { continue; }
                        seats[excess][i]--; byDecimal[excess][i] = false;
                        seats[to][i]++; byDecimal[to][i] = true;
                        moved = true; moves++;
                        break;
                    }
                    if (moved) { continue; }
                    // the last resort: the excess unit's seats "ottenuti con le minori parti decimali" - a seat it won on a decimal, the smallest first (L2: never an
                    // integer-won seat while a decimal-won one remains); the short unit's largest unused decimal in another circoscrizione
                    int from = -1;
                    for (int i = 0; i < circs; i++) { if (byDecimal[excess][i] && seats[excess][i] > 0 && (from < 0 || dec[excess][i] < dec[excess][from])) { from = i; } }
                    if (from < 0) { for (int i = 0; i < circs; i++) { if (seats[excess][i] > 0 && (from < 0 || dec[excess][i] < dec[excess][from])) { from = i; } } }   // no decimal seat left (stated; not reached on the record)
                    int shortUnit = -1, at = -1;
                    for (int u = 0; u < units; u++)
                    {
                        if (u == excess || Total(u) >= target[u]) { continue; }
                        for (int i = 0; i < circs; i++)
                        {
                            if (byDecimal[u][i] || seatsIn[i] <= 0) { continue; }
                            if (shortUnit < 0 || dec[u][i] > dec[shortUnit][at]) { shortUnit = u; at = i; }
                        }
                    }
                    if (from < 0 || shortUnit < 0) { break; }
                    seats[excess][from]--; byDecimal[excess][from] = false;
                    // the seat leaves its circoscrizione for the short unit's (the statute's last resort crosses circoscrizioni) - DECLARED: the circoscrizioni's own
                    // totals move by one each way, as the statute's words have it
                    seats[shortUnit][at]++; byDecimal[shortUnit][at] = true;
                    moves++;
                }
            }
            if (moves > 0) { log.Append("  ").Append(stage).Append(": ").Append(moves).Append(" seat(s) moved by the compensation for seats in excess\n"); }
            return seats;
        }

        // ------------------------------------------------------------------------------------------------------------------------------- the Senato

        private static int[][] SenatoProportional(string[] keys, long[][] cifra, long[] other, int[] propSeats, int[] minorityWins, System.Text.StringBuilder log)
        {
            int nk = keys.Length, regions = cifra.Length;
            List<string[]> coalitions = Coalitions(Chamber.Senato);
            var national = new long[nk];
            long valid = 0;
            for (int i = 0; i < regions; i++) { for (int k = 0; k < nk; k++) { national[k] += cifra[i][k]; valid += cifra[i][k]; } valid += other[i]; }
            var regionValid = new long[regions];
            for (int i = 0; i < regions; i++) { regionValid[i] = other[i]; foreach (long v in cifra[i]) { regionValid[i] += v; } }
            bool TwentyIn(int k, int i) => regionValid[i] > 0 && cifra[i][k] >= RegionalLine * regionValid[i];
            bool TwentyAnywhere(int k) { for (int i = 0; i < regions; i++) { if (TwentyIn(k, i)) { return true; } } return false; }
            bool minority = MinorityQualifies(keys, cifra, other, CollegesPerCirc(Chamber.Senato), minorityWins);
            bool Minority(int k) => keys[k] == MinorityKey && minority;
            bool ThreeNational(int k) => national[k] >= Rosatellum.ListThreshold * valid;
            // art. 16-bis lett. c: a coalition's figure strikes a list below 1 % nationally unless it has 20 % in a region or is the minority's
            bool Counts(int k) => national[k] >= Rosatellum.MinorityCoalitionFloor * valid || TwentyAnywhere(k) || Minority(k);

            // lett. e n.1: the admitted coalitions
            var admittedCoalition = new bool[coalitions.Count];
            for (int co = 0; co < coalitions.Count; co++)
            {
                long fig = 0; bool member = false;
                foreach (string m in coalitions[co]) { int k = K(keys, m); if (k < 0) { continue; } if (Counts(k)) { fig += national[k]; } member |= ThreeNational(k) || TwentyAnywhere(k) || Minority(k); }
                admittedCoalition[co] = fig >= Rosatellum.CoalitionThreshold * valid && member;
                log.Append("SENATO coalition ").Append(string.Join("+", coalitions[co])).Append(": ").Append(fig).Append(admittedCoalition[co] ? " ADMITTED\n" : " rejected\n");
            }

            var result = new int[regions][];
            for (int i = 0; i < regions; i++)
            {
                result[i] = new int[nk];
                if (propSeats[i] <= 0) { continue; }
                // art. 17 lett. a: the admitted coalitions and the single lists (3 % nationally, 20 % in this region, or the minority's) in this region
                var unitKeys = new List<int[]>();
                var unitFig = new List<long>();
                var inCoalition = new bool[nk];
                for (int co = 0; co < coalitions.Count; co++)
                {
                    if (!admittedCoalition[co]) { continue; }
                    var members = new List<int>(); long fig = 0;
                    foreach (string m in coalitions[co]) { int k = K(keys, m); if (k < 0) { continue; } members.Add(k); inCoalition[k] = true; if (Counts(k)) { fig += cifra[i][k]; } }
                    unitKeys.Add(members.ToArray()); unitFig.Add(fig);
                }
                for (int k = 0; k < nk; k++)
                {
                    if (inCoalition[k]) { continue; }
                    if (!(ThreeNational(k) || TwentyIn(k, i) || Minority(k))) { continue; }
                    unitKeys.Add(new[] { k }); unitFig.Add(cifra[i][k]);
                }
                int[] unitSeats = Rosatellum.FlooredHare(unitFig.ToArray(), propSeats[i], out long _);
                // lett. b: inside each coalition among its lists of 3 % nationally, 20 % in the region or the minority's
                for (int u = 0; u < unitKeys.Count; u++)
                {
                    int[] members = unitKeys[u];
                    if (members.Length == 1 && !IsCoalitionMember(keys, coalitions, members[0])) { result[i][members[0]] += unitSeats[u]; continue; }
                    var admitted = new List<int>(); var mv = new List<long>();
                    foreach (int k in members) { if (ThreeNational(k) || TwentyIn(k, i) || Minority(k)) { admitted.Add(k); mv.Add(cifra[i][k]); } }
                    int[] ms = Rosatellum.FlooredHare(mv.ToArray(), unitSeats[u], out long _);
                    for (int j = 0; j < admitted.Count; j++) { result[i][admitted[j]] += ms[j]; }
                }
            }
            return result;
        }

        private static bool IsCoalitionMember(string[] keys, List<string[]> coalitions, int k) => CoalitionOf(coalitions, keys[k]) >= 0;

        // ------------------------------------------------------------------------------------------------------------------------------- the Estero

        /// <summary>K158: each ripartizione's 2022 list votes, swung by the list's national change since 2022 (its keys' summed game share less their summed
        /// published 2022 share, Area Italia), floored at zero; a list no roster key carries is not counted - the game's lists are the whole vote abroad as at home (DECLARED; at 2022's shares no such list won a seat, so the record's seats stand). Rows the ripartizioni, columns the Estero lists.</summary>
        public static double[][] EsteroVotes(Chamber chamber, IReadOnlyDictionary<string, double> shares)
        {
            long[][] v22 = chamber == Chamber.Camera ? ItalianCount2022.CameraEstero2022 : ItalianCount2022.SenatoEstero2022;
            long[] pub = chamber == Chamber.Camera ? ItalianCount2022.CameraPublished2022Votes : ItalianCount2022.SenatoPublished2022Votes;
            // M2 (the review): the 2022 share on the roster's own sum - the game's shares sum to one over the roster, so both sides of the change are read on
            // one basis (as the colleges' swing reads 2018 on the mapped lists' own sum); on the record's own vote every change is zero
            long valid = 0;
            foreach (long v in pub) { valid += v; }
            string[] listKeys = ItalianCount2022.EsteroListKeys;
            var change = new double[listKeys.Length];
            for (int l = 0; l < listKeys.Length; l++)
            {
                if (string.IsNullOrEmpty(listKeys[l])) { continue; }
                foreach (string key in listKeys[l].Split('+'))
                {
                    int k = K(ItalianCount2022.Keys, key);
                    double was = k >= 0 && valid > 0 ? (double)pub[k] / valid : 0.0;
                    double now = shares != null && shares.TryGetValue(key, out double s) ? s : was;
                    if (was > 0.0) { change[l] += now - was; }   // a key with no Area Italia vote in 2022 (MAIE) has no national change
                }
            }
            var result = new double[v22.Length][];
            for (int r = 0; r < v22.Length; r++)
            {
                long size = 0;
                foreach (long v in v22[r]) { size += v; }
                result[r] = new double[listKeys.Length];
                if (size <= 0) { continue; }
                for (int l = 0; l < listKeys.Length; l++)
                {
                    if (v22[r][l] <= 0) { continue; }   // a list that did not stand in the ripartizione stands nowhere new
                    if (string.IsNullOrEmpty(listKeys[l])) { continue; }   // not a roster list: its votes are not modelled
                    result[r][l] = Math.Max(0.0, (double)v22[r][l] / size + change[l]) * size;
                }
            }
            return result;
        }

        /// <summary>The 2022 Estero votes as recorded (the backtest's input).</summary>
        public static double[][] EsteroRecord(Chamber chamber)
        {
            long[][] v22 = chamber == Chamber.Camera ? ItalianCount2022.CameraEstero2022 : ItalianCount2022.SenatoEstero2022;
            var result = new double[v22.Length][];
            for (int r = 0; r < v22.Length; r++) { result[r] = new double[v22[r].Length]; for (int l = 0; l < v22[r].Length; l++) { result[r][l] = v22[r][l]; } }
            return result;
        }

        /// <summary>L. 459/2001 art. 15 lett. c in each ripartizione; returns the seats won by lists the roster does not carry.</summary>
        /// <summary>L. 459/2001 art. 15 lett. c: each ripartizione's seats per Estero list (rows the ripartizioni, columns <see cref="ItalianCount2022.EsteroLists"/>).</summary>
        public static int[][] EsteroSeatsByList(Chamber chamber, double[][] votes)
        {
            int[] seats = chamber == Chamber.Camera ? ItalianCount2022.CameraEsteroSeats : ItalianCount2022.SenatoEsteroSeats;
            var result = new int[votes.Length][];
            for (int r = 0; r < votes.Length; r++)
            {
                var v = new long[votes[r].Length];
                for (int l = 0; l < v.Length; l++) { v[l] = (long)Math.Round(votes[r][l]); }
                result[r] = Rosatellum.FlooredHare(v, seats[r], out long _);
            }
            return result;
        }

        private static int Estero(Chamber chamber, double[][] votes, IReadOnlyDictionary<string, double> nationalShares, Dictionary<string, int> seatsOut, System.Text.StringBuilder log)
        {
            string[] listKeys = ItalianCount2022.EsteroListKeys;
            int unkeyed = 0;
            int[][] byList = EsteroSeatsByList(chamber, votes);
            for (int r = 0; r < votes.Length; r++)
            {
                int[] won = byList[r];
                for (int l = 0; l < won.Length; l++)
                {
                    if (won[l] <= 0) { continue; }
                    if (string.IsNullOrEmpty(listKeys[l])) { unkeyed += won[l]; log.Append("  Estero ").Append(ItalianCount2022.EsteroRipartizioni[r]).Append(": ").Append(won[l]).Append(" seat(s) to ").Append(ItalianCount2022.EsteroLists[l]).Append(", which no roster key carries\n"); continue; }
                    string[] members = listKeys[l].Split('+');
                    var given = new int[members.Length];
                    for (int s = 0; s < won[l]; s++)
                    {
                        int best = 0; double bestQ = -1.0;
                        for (int m = 0; m < members.Length; m++)
                        {
                            double share = nationalShares != null && nationalShares.TryGetValue(members[m], out double x) ? x : (members.Length == 1 ? 1.0 : 0.0);
                            double q = share / (given[m] + 1);
                            if (q > bestQ) { best = m; bestQ = q; }
                        }
                        given[best]++;
                    }
                    for (int m = 0; m < members.Length; m++) { seatsOut[members[m]] += given[m]; }
                }
            }
            return unkeyed;
        }

        /// <summary>The colleges per circoscrizione whose winning group holds the minority list - lett. e n.2's second route.</summary>
        internal static int[] MinorityWinsByCirc(Chamber chamber, string[] winner)
        {
            var wins = new int[Circoscrizioni(chamber).Length];
            int[] collegeCirc = CollegeCirc(chamber);
            for (int c = 0; c < winner.Length; c++) { if (Array.IndexOf(winner[c].Split('+'), MinorityKey) >= 0) { wins[collegeCirc[c]]++; } }
            return wins;
        }
    }
}
