using System;
using System.Collections.Generic;
using PoliSim.Elections.Generated;

namespace PoliSim.Elections
{
    /// <summary>
    /// D-IT (Design's boards 34a-34d, `PoliSim-captures/design/RETURN_2026-10-29_DIT/`): <b>ITALY'S ELECTION NIGHT AS A MODEL</b> - the Camera's and the
    /// Senato's colleges declaring one by one, the leading coalition's certain seats against each chamber's line, and at the close the list seats and the
    /// abroad seats placed. Pure: no Unity, no world.
    ///
    /// <para><b>The count is the game's</b> (`ItalianCount`): the colleges' votes are the swing the chamber was counted on (`ItalianCount.SwungVotes`), each
    /// college's winner and the list its seat counts for are the full count's (`Result.CollegeWinner`, `Result.CollegeSeatKey`), the list seats per
    /// circoscrizione (Camera) and region (Senato) are lett. h/i's and art. 17's (`Result.ByCircoscrizione`), the abroad seats L. 459/2001 art. 15's per
    /// ripartizione (`Result.EsteroByRipartizione`), the senators for life the record's (`ItalianCount.SenatorsForLifeSeatedOn`). DECLARED (as Poland's
    /// night): the night reveals a count the model has already made - turnout is not modelled, so every vote the night will count is known before the first
    /// college declares, and a declared college's seat is the full count's.</para>
    ///
    /// <para><b>The order of declaration</b> is the board's seeded order [AUTHORED] (K282, `ItalianNightCartogram.Chamber.DeclareRank`, Design's 34a), a
    /// presentation choice and never a forecast: the night's index is the Camera's colleges declared, 0 to 147; the Senato's declared at index i are
    /// round(i × 74 / 147), the board's own pairing [AUTHORED].</para>
    ///
    /// <para><b>The call</b> (34a, "Called"): a chamber is called when the leading coalition's CERTAIN seats reach its line - the colleges it has won among
    /// those declared, plus - where its admission is already certain on the counted votes alone - floor(pool × its counted votes ÷ every valid vote, counted
    /// and not) list seats (K281, `Certain`, whose comment carries the proof): a floor on what it ends with in Italy, however the votes still out are cast. The
    /// Camera reads the national pool, the Senato each region's. The board drew one allocation (all to the largest rival), and an allocation is never a bound:
    /// it can push a third list under its line and hand its seats to the leader (`ItalianNightDiagnostic` (e) plants the cases). The floor calls later in the
    /// night than the board did. The abroad seats are never certain before the close. The line is half the chamber plus one - art. 64's "more for than against" with every member voting, as `ConfidenceProcedure.Article94`
    /// models it - read off the chamber's size (the Camera's seats in Italy and abroad; the Senato's with the senators for life, who count against). A call is
    /// stamped where it lands and stands.</para>
    /// </summary>
    public static class ItalianNight
    {
        public enum Stage { Before, Counting, Called, Final }

        /// <summary>[AUTHORED] the night's fixed order (34a: FdI Lega FI NM · PD AVS +E IC · M5S Az-IV SVP ScN MAIE UV) - the count's rows and each block's list
        /// seats in it, never re-sorted; a key the catalog does not carry (a created party) follows, in the count's own order.</summary>
        public static readonly string[] Order = { "FdI", "Lega", "FI", "NM", "PD", "AVS", "PlusE", "IC", "M5S", "AzIV", "SVP", "ScN", "MAIE", "UV" };

        /// <summary>[AUTHORED] the night's clock (34a): the polls close at 23:00, and the board spreads the Camera's colleges over 510 minutes.</summary>
        public const int PollsCloseHour = 23, NightMinutes = 510;

        /// <summary>The night's last index: every Camera college declared.</summary>
        public static int Total => ItalianCount2022.CameraColleges.Length;

        /// <summary>The Senato's colleges declared at night index <paramref name="index"/> - the board's pairing, round(index × 74 / 147) [AUTHORED].</summary>
        public static int SenatoDeclaredAt(int index) => (int)Math.Floor(Math.Max(0, Math.Min(index, Total)) * (double)ItalianCount2022.SenatoColleges.Length / Total + 0.5);

        /// <summary>The minutes after the polls closed at night index <paramref name="index"/> (the board's clock).</summary>
        public static int MinuteAt(int index) => (int)Math.Floor(Math.Max(0, Math.Min(index, Total)) * (double)NightMinutes / Total + 0.5);

        /// <summary>One chamber's night at an instant.</summary>
        public sealed class ChamberNight
        {
            public ItalianCount.Chamber Of;
            /// <summary>The count's keys (`ItalianCount.KeysFor`).</summary>
            public string[] Keys;
            /// <summary>The full count - what the night reveals.</summary>
            public ItalianCount.Result Result;
            public ItalianNightCartogram.Chamber Geometry;
            /// <summary>Each college's votes per key, and the votes of the lists no key carries there (the roster's share of the valid vote on an election of record, else none).</summary>
            public double[][] Votes;
            public double[] Other;
            /// <summary>The roster's share of the valid vote the count was run on (one on every day but an election of record's) - the other lists' votes derive from it.</summary>
            public double Roster;
            public int Colleges, Declared;
            public bool[] CollegeDeclared;
            /// <summary>Votes per key over the declared colleges; the other lists' votes there; every vote the night will count.</summary>
            public double[] Counted;
            public double CountedOther, AllValid;
            /// <summary>Each key's votes over every college - its share at the close is this over <see cref="AllValid"/>.</summary>
            public double[] National;
            public double NationalOther;
            /// <summary>Colleges won among those declared, per key (the list the seat counts for).</summary>
            public int[] CollegesWon;
            /// <summary>The chamber's seats: in Italy, abroad, for life; the line (half plus one).</summary>
            public int ItalySeats, AbroadSeats, LifeSeats, Seats, Line;
            public List<(string Name, string Group, string Key)> Life = new List<(string Name, string Group, string Key)>();
            /// <summary>The coalitions (the record's, `ItalianCount.Coalitions`) and the lists alone - the night's entities - and the leader
            /// by counted votes (null before a vote is counted).</summary>
            public List<string[]> Entities = new List<string[]>();
            public string[] Leader;
            /// <summary>The leader's certain seats now (colleges won + the bound's list seats), the two parts, and where the chamber's call landed (0 while open)
            /// for whom, and that coalition's certain seats at the stamp - what the Called lane draws.</summary>
            public int LeaderCertain, LeaderColleges, LeaderListBound;
            public int CalledAt;
            public string[] CalledFor;
            public int CalledCertain;
            public bool Complete => Declared == Colleges;
            public bool Called => CalledAt > 0;

            /// <summary>The key's index in <see cref="Keys"/>, or -1.</summary>
            public int K(string key) => Array.IndexOf(Keys, key);

            /// <summary>The key's final seats (colleges, list seats and abroad).</summary>
            public int SeatsOf(string key) => Result.Seats.TryGetValue(key, out int s) ? s : 0;

            /// <summary>The colleges a block has declared.</summary>
            public int DeclaredIn(int block)
            {
                int n = 0;
                foreach (int c in Geometry.BlockColleges[block]) { if (CollegeDeclared[c]) { n++; } }
                return n;
            }

            /// <summary>The block's magnitude (its cells): its colleges and its list seats.</summary>
            public int Magnitude(int block) => (Of == ItalianCount.Chamber.Camera ? ItalianCount2022.CameraCircoscrizioneSeats : ItalianCount2022.SenatoCircoscrizioneSeats)[block];

            /// <summary>The block's list seats at the close, one key per list cell in the night's fixed order (<see cref="OrderedKeys"/>); a cell the count left
            /// unfilled null. <paramref name="overflow"/>: the seats the count gave the block beyond its list cells (lett. h's last resort can move a seat
            /// across circoscrizioni - zero on every count the night has been held to).</summary>
            public string[] BlockListKeys(int block, out int overflow)
            {
                int cells = Magnitude(block) - Geometry.BlockColleges[block].Length;
                var list = new string[Math.Max(0, cells)];
                int at = 0;
                overflow = 0;
                int[] row = Result.ByCircoscrizione[block];
                foreach (string key in OrderedKeys())
                {
                    int k = K(key);
                    if (k < 0) { continue; }
                    for (int s = 0; s < row[k]; s++) { if (at < list.Length) { list[at++] = key; } else { overflow++; } }
                }
                return list;
            }

            /// <summary>The ripartizione's abroad seats at the close, in the night's order - a seat won by a list no key carries null.</summary>
            public string[] AbroadKeys(int ripartizione)
            {
                int seats = (Of == ItalianCount.Chamber.Camera ? ItalianCount2022.CameraEsteroSeats : ItalianCount2022.SenatoEsteroSeats)[ripartizione];
                var list = new string[seats];
                int at = 0;
                Dictionary<string, int> won = Result.EsteroByRipartizione != null ? Result.EsteroByRipartizione[ripartizione] : null;
                if (won != null)
                {
                    foreach (string key in OrderedKeys()) { if (won.TryGetValue(key, out int n)) { for (int s = 0; s < n && at < seats; s++) { list[at++] = key; } } }
                }
                return list;
            }

            /// <summary>The night's keys in its fixed order: <see cref="Order"/>, then the count's other keys in its own order.</summary>
            public List<string> OrderedKeys()
            {
                var keys = new List<string>();
                foreach (string key in Order) { if (K(key) >= 0) { keys.Add(key); } }
                foreach (string key in Keys) { if (!keys.Contains(key)) { keys.Add(key); } }
                return keys;
            }

            /// <summary>The coalition holding <paramref name="key"/> (its members), or null for a list alone.</summary>
            public string[] CoalitionOf(string key)
            {
                foreach (string[] e in Entities) { if (e.Length > 1 && Array.IndexOf(e, key) >= 0) { return e; } }
                return null;
            }

            /// <summary>The entity's counted votes so far.</summary>
            public double CountedOf(string[] entity)
            {
                double v = 0.0;
                foreach (string key in entity) { int k = K(key); if (k >= 0) { v += Counted[k]; } }
                return v;
            }

            /// <summary>The entity's seats at the close.</summary>
            public int SeatsOf(string[] entity)
            {
                int n = 0;
                foreach (string key in entity) { n += SeatsOf(key); }
                return n;
            }

            /// <summary>The minority list's route (lett. e n. 2 / art. 16-bis) as far as the count has gone: met for certain once its counted votes in its
            /// circoscrizione reach a fifth of every vote that circoscrizione will count, or its group has won a quarter (rounded up) of the circoscrizione's
            /// colleges among those declared - read, as the count reads it, only in a circoscrizione where the list has list votes. False where it has none.</summary>
            public bool MinorityRouteMet(out int circoscrizione)
            {
                circoscrizione = -1;
                int m = K(ItalianCount.MinorityKey);
                if (m < 0) { return false; }
                int[] collegeCirc = Of == ItalianCount.Chamber.Camera ? ItalianCount2022.CameraCollegeCircoscrizione : ItalianCount2022.SenatoCollegeCircoscrizione;
                int blocks = Result.ByCircoscrizione.Length;
                var all = new double[blocks];
                var mine = new double[blocks];
                var wins = new int[blocks];
                var colleges = new int[blocks];
                var listVotes = new double[blocks];
                for (int c = 0; c < Colleges; c++)
                {
                    int i = collegeCirc[c];
                    colleges[i]++;
                    foreach (double v in Votes[c]) { all[i] += v; }
                    listVotes[i] += Votes[c][m];
                    all[i] += Other[c];
                    if (CollegeDeclared[c]) { mine[i] += Votes[c][m]; if (Array.IndexOf(Result.CollegeWinner[c].Split('+'), ItalianCount.MinorityKey) >= 0) { wins[i]++; } }
                    if (Votes[c][m] > 0.0 || Array.IndexOf(Result.CollegeWinner[c].Split('+'), ItalianCount.MinorityKey) >= 0) { circoscrizione = circoscrizione < 0 ? i : circoscrizione; }
                }
                for (int i = 0; i < blocks; i++)
                {
                    if (!(listVotes[i] > 0.0)) { continue; }   // the count's own precondition (`ItalianCount.MinorityQualifies`): the circoscrizione where the list has list votes
                    if (all[i] > 0.0 && mine[i] >= ItalianCount.RegionalLine * all[i]) { circoscrizione = i; return true; }
                    if (colleges[i] > 0 && wins[i] >= (colleges[i] + 3) / 4) { circoscrizione = i; return true; }
                }
                return false;
            }
        }

        /// <summary>The night at one index.</summary>
        public sealed class State
        {
            public int Index;
            public Stage Stage;
            public ChamberNight Camera, Senato;
            /// <summary>Both chambers called: the later of the two calls (0 while either is open).</summary>
            public int CalledAt;
            public bool Final => Index >= Total;
            public ChamberNight Of(ItalianCount.Chamber c) => c == ItalianCount.Chamber.Camera ? Camera : Senato;
        }

        /// <summary>The night at <paramref name="index"/> (0 to <see cref="Total"/>) on the election's national <paramref name="shares"/> (fractions of the
        /// valid vote, keyed by the roster's abbreviations - the election record's), each chamber's roster share of the valid vote (the count's
        /// `rosterShareOfVote`: one on every day but an election of record's), the Senato's senators for life as of <paramref name="firstSitting"/>. Null where
        /// no shares are given.</summary>
        public static State At(int index, IReadOnlyDictionary<string, double> shares, double cameraRoster, double senatoRoster, DateTime firstSitting)
        {
            if (shares == null || shares.Count == 0) { return null; }
            index = Math.Max(0, Math.Min(index, Total));
            var state = new State { Index = index };
            state.Camera = Build(ItalianCount.Chamber.Camera, shares, cameraRoster, firstSitting, index);
            state.Senato = Build(ItalianCount.Chamber.Senato, shares, senatoRoster, firstSitting, index);
            bool both = state.Camera.Called && state.Senato.Called;
            state.CalledAt = both ? Math.Max(state.Camera.CalledAt, state.Senato.CalledAt) : 0;
            state.Stage = index == 0 ? Stage.Before : state.Final ? Stage.Final : both ? Stage.Called : Stage.Counting;
            return state;
        }

        private static int DeclaredAt(ItalianCount.Chamber c, int index) => c == ItalianCount.Chamber.Camera ? index : SenatoDeclaredAt(index);

        private static ChamberNight Build(ItalianCount.Chamber chamber, IReadOnlyDictionary<string, double> shares, double roster, DateTime firstSitting, int index)
        {
            bool camera = chamber == ItalianCount.Chamber.Camera;
            var n = new ChamberNight { Of = chamber, Geometry = camera ? ItalianNightCartogram.Camera : ItalianNightCartogram.Senato, Roster = roster };
            n.Votes = ItalianCount.SwungVotes(chamber, shares, out n.Keys);
            n.Result = camera ? ItalianCount.Camera(shares, roster) : ItalianCount.Senato(shares, roster);
            n.Colleges = n.Votes.Length;
            int nk = n.Keys.Length;
            double otherFactor = roster > 0.0 && roster < 1.0 ? 1.0 / roster - 1.0 : 0.0;
            n.Other = new double[n.Colleges];
            n.National = new double[nk];
            for (int c = 0; c < n.Colleges; c++)
            {
                double sum = 0.0;
                for (int k = 0; k < nk; k++) { n.National[k] += n.Votes[c][k]; sum += n.Votes[c][k]; }
                n.Other[c] = sum * otherFactor;
                n.NationalOther += n.Other[c];
                n.AllValid += sum + n.Other[c];
            }
            foreach (int s in camera ? ItalianCount2022.CameraCircoscrizioneSeats : ItalianCount2022.SenatoCircoscrizioneSeats) { n.ItalySeats += s; }
            foreach (int s in camera ? ItalianCount2022.CameraEsteroSeats : ItalianCount2022.SenatoEsteroSeats) { n.AbroadSeats += s; }
            if (!camera) { n.Life = ItalianCount.SenatorsForLifeSeatedOn(firstSitting); }
            n.LifeSeats = n.Life.Count;
            n.Seats = n.ItalySeats + n.AbroadSeats + n.LifeSeats;
            n.Line = n.Seats / 2 + 1;

            foreach (string[] co in ItalianCount.Coalitions(chamber)) { n.Entities.Add(co); }
            foreach (string key in n.OrderedKeys()) { if (n.CoalitionOf(key) == null) { n.Entities.Add(new[] { key }); } }

            // the call, walked to the index - stamped where it lands (the leader at that instant), and standing
            int[] rank = n.Geometry.DeclareRank;
            int lastDeclared = -1;
            for (int i = 1; i <= index; i++)
            {
                int declared = DeclaredAt(chamber, i);
                if (declared == lastDeclared) { continue; }
                lastDeclared = declared;
                if (n.CalledAt > 0) { break; }
                Certain(n, declared, out string[] leader, out int certain, out int _, out int _);
                if (leader != null && certain >= n.Line) { n.CalledAt = i; n.CalledFor = leader; n.CalledCertain = certain; }
            }
            n.Declared = DeclaredAt(chamber, index);
            Certain(n, n.Declared, out n.Leader, out n.LeaderCertain, out n.LeaderColleges, out n.LeaderListBound);
            n.CollegeDeclared = new bool[n.Colleges];
            n.Counted = new double[nk];
            n.CollegesWon = new int[nk];
            for (int c = 0; c < n.Colleges; c++)
            {
                if (rank[c] >= n.Declared) { continue; }
                n.CollegeDeclared[c] = true;
                for (int k = 0; k < nk; k++) { n.Counted[k] += n.Votes[c][k]; }
                n.CountedOther += n.Other[c];
                int won = n.K(n.Result.CollegeSeatKey[c]);
                if (won >= 0) { n.CollegesWon[won]++; }
            }
            return n;
        }

        /// <summary>
        /// K281: the leader by counted votes over the first <paramref name="declared"/> colleges, and its CERTAIN seats there - a provable floor on the seats in
        /// Italy it ends with: the colleges it has won among those declared, plus floor(pool × L ÷ D) list seats where its admission is already certain on the
        /// counted votes alone, and none otherwise. D is every valid vote the count's lines and quotient will read, counted and not (the night knows every
        /// college's total; it is computed as `ItalianCount.Count` computes its base); L is the leader's counted votes that will count for it whatever comes
        /// (a coalition's lists already over 1 % of D - lett. c can strike no list whose counted votes clear it). The Camera reads the national pool (lett. f);
        /// the Senato each region's own pool, summed (art. 17 lett. a). At the close the floor is the count's own list seats.
        /// <para><b>The proof, the Camera</b> (`Rosatellum.AllocateNational`, `Rosatellum.FlooredHare`, `ItalianCount.Distribute`):
        /// (1) admission is certain: the coalition's struck figure on counted votes is ≥ 10 % of D with a list ≥ 3 % of D (a list alone ≥ 3 %), and every
        /// figure only grows as colleges declare while D is fixed, so lett. e admits it at the close; (2) lett. f's quotient is q = floor(T ÷ pool) with T the
        /// admitted entities' figures, T ≤ D, so q ≤ D ÷ pool; its final figure V ≥ L (votes only add; a list over 1 % of D on counted votes is over it at the
        /// close, so lett. c strikes none of L); FlooredHare gives it at least floor(V ÷ q) and never takes a seat back (the remainders only add one), and
        /// floor(V ÷ q) ≥ floor(pool × L ÷ D); (3) lett. g only shares the coalition's seats among its lists - the total is the coalition's; (4) lett. h/i
        /// (`Distribute`) devolve each entity's national seats to the circoscrizioni: the first pass fills every circoscrizione to its seats - where a
        /// quotient is zero (the units' figures there sum to nothing) K283's rule seats them, so every circoscrizione is filled and the pool conserved (the count
        /// records any shortfall, `Result.ListSeatsShort`, and the checks fail on it) - and the compensation only moves a seat from a unit OVER its national
        /// total to one UNDER it, so a unit short of its total only gains. ⚠ The one exit the floor does not cover: the compensation's last resort breaks off
        /// (`Distribute`'s `shortUnit < 0`) where every short unit already holds a decimal seat in every circoscrizione with seats - a short unit can then stay
        /// short. Its reach, measured honestly: review 3's 12,000 wide random votes reached it once, at lett. h with PD at 73 %, and there it did not bite the
        /// leader; `ItalianNightDiagnostic` (e) walks fourteen votes, none of which reaches it - so (e) is no cover for it. Stated, not proved away. Integer
        /// parts never over-fill the pool (T ÷ q &lt; pool + 1 whenever T ≥ pool × (pool + 1), any real electorate), so no step trims a unit back.
        /// <b>The Senato</b> (`ItalianCount.SenatoProportional`): no compensation; in each region the admitted units share its seats by FlooredHare over their
        /// regional figures, whose sum is ≤ the region's valid vote D_i, so a unit holds at least floor(seats_i × L_i ÷ D_i) - with admission certain nationally
        /// (a coalition ≥ 10 % with a list ≥ 3 % of the national D or ≥ 20 % of a region's; a list alone ≥ 3 %) or, for a list alone, ≥ 20 % of that region.
        /// A region whose quotient is zero (the units' or a coalition's admitted lists' figures sum to fewer votes than seats) is filled by K283's rule
        /// (`ItalianCount.Conserve`), which only adds seats.</para>
        /// </summary>
        public static void Certain(ChamberNight n, int declared, out string[] leader, out int certain, out int colleges, out int listBound)
        {
            int[] rank = n.Geometry.DeclareRank;
            leader = null;
            certain = colleges = listBound = 0;
            int nk = n.Keys.Length;
            var counted = new double[nk];
            for (int c = 0; c < n.Colleges; c++) { if (rank[c] < declared) { for (int k = 0; k < nk; k++) { counted[k] += n.Votes[c][k]; } } }
            double best = 0.0;
            foreach (string[] e in n.Entities)
            {
                double v = 0.0;
                foreach (string key in e) { int k = n.K(key); if (k >= 0) { v += counted[k]; } }
                if (v > best) { best = v; leader = e; }
            }
            if (leader == null) { return; }
            for (int c = 0; c < n.Colleges; c++)
            {
                if (rank[c] < declared && Array.IndexOf(leader, n.Result.CollegeSeatKey[c]) >= 0) { colleges++; }
            }
            if (declared >= n.Colleges)
            {
                foreach (string key in leader) { listBound += n.Result.ProportionalSeats.TryGetValue(key, out int s) ? s : 0; }
                certain = colleges + listBound;
                return;
            }

            // the count's own base, per circoscrizione (region): its colleges' votes rounded as the count rounds them, the other lists' as it derives them
            bool camera = n.Of == ItalianCount.Chamber.Camera;
            int[] collegeCirc = camera ? ItalianCount2022.CameraCollegeCircoscrizione : ItalianCount2022.SenatoCollegeCircoscrizione;
            int[] circSeats = camera ? ItalianCount2022.CameraCircoscrizioneSeats : ItalianCount2022.SenatoCircoscrizioneSeats;
            int circs = circSeats.Length;
            var cifraAll = new long[circs];
            var countedCirc = new long[circs][];
            var collegesIn = new int[circs];
            for (int i = 0; i < circs; i++) { countedCirc[i] = new long[nk]; }
            for (int c = 0; c < n.Colleges; c++)
            {
                int i = collegeCirc[c];
                collegesIn[i]++;
                for (int k = 0; k < nk; k++)
                {
                    long v = (long)Math.Round(n.Votes[c][k]);
                    cifraAll[i] += v;
                    if (rank[c] < declared) { countedCirc[i][k] += v; }
                }
            }
            double factor = n.Roster > 0.0 && n.Roster < 1.0 ? 1.0 / n.Roster - 1.0 : 0.0;
            var regionValid = new long[circs];
            var propSeats = new int[circs];
            long nationalValid = 0;
            for (int i = 0; i < circs; i++)
            {
                regionValid[i] = cifraAll[i] + (long)Math.Round(cifraAll[i] * factor);
                propSeats[i] = circSeats[i] - collegesIn[i];
                if (!camera || propSeats[i] > 0) { nationalValid += regionValid[i]; }   // the Camera's base skips a circoscrizione with no list seat, as the count does
            }
            var countedNational = new long[nk];
            for (int i = 0; i < circs; i++) { if (!camera || propSeats[i] > 0) { for (int k = 0; k < nk; k++) { countedNational[k] += countedCirc[i][k]; } } }
            bool OverRegionalLine(int k)
            {
                for (int i = 0; i < circs; i++) { if (regionValid[i] > 0 && countedCirc[i][k] >= ItalianCount.RegionalLine * regionValid[i]) { return true; } }
                return false;
            }
            var members = new List<int>();
            foreach (string key in leader) { int k = n.K(key); if (k >= 0) { members.Add(k); } }
            bool coalition = leader.Length > 1;
            // the lists whose votes count for the leader whatever comes: a coalition's over 1 % of the base (or 20 % of a region, on the Senato); a list alone itself
            var counts = new List<int>();
            bool qualifying = false;
            foreach (int k in members)
            {
                bool countsNow = !coalition || countedNational[k] >= Rosatellum.MinorityCoalitionFloor * nationalValid || (!camera && OverRegionalLine(k));
                if (countsNow) { counts.Add(k); }
                qualifying |= countedNational[k] >= Rosatellum.ListThreshold * nationalValid || (!camera && OverRegionalLine(k));
            }
            long figure = 0;
            foreach (int k in counts) { figure += countedNational[k]; }
            bool admitted = coalition ? figure >= Rosatellum.CoalitionThreshold * nationalValid && qualifying : countedNational[members[0]] >= Rosatellum.ListThreshold * nationalValid;
            if (camera)
            {
                int pool = 0;
                foreach (int s in propSeats) { pool += Math.Max(0, s); }
                if (admitted && nationalValid > 0) { listBound = (int)Math.Floor(pool * (double)figure / nationalValid); }
            }
            else
            {
                for (int i = 0; i < circs; i++)
                {
                    if (propSeats[i] <= 0 || regionValid[i] <= 0) { continue; }
                    bool here = admitted || (!coalition && countedCirc[i][members[0]] >= ItalianCount.RegionalLine * regionValid[i]);
                    if (!here) { continue; }
                    long inRegion = 0;
                    foreach (int k in counts) { inRegion += countedCirc[i][k]; }
                    listBound += (int)Math.Floor(propSeats[i] * (double)inRegion / regionValid[i]);
                }
            }
            certain = colleges + listBound;
        }
    }
}
