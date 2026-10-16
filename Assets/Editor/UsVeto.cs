using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using PoliSim.Data;

namespace PoliSim.Elections
{
    /// <summary>
    /// PS-6, R-US17's rule (c) (Elias's rulings H1-H3; the block in `docs/specs/USA_STAGE_PLAN.md`, preregistered §832, amended §833) - THE DRAW OF
    /// DEFECTORS ON THE USA'S VETO PATH, the arithmetic alone. Parties vote as blocs; only on a bill on the veto path does a party draw crossers, once
    /// per party per chamber per bill: the president's party standing against draws from its cell (HP, SP), the other party standing for draws from
    /// its own (HO, SO). The drawn observation (d, m) - a recorded roll call's crossers and voting members - is carried onto the bloc's seats, rounded
    /// half up and capped so the crossers stay a strict minority (<see cref="Crossers"/>). Rule (a) then decides the veto (<see cref="NamedByA"/>);
    /// the override is two thirds of those voting on the passage's sides (<see cref="OverrideCarries"/>).
    ///
    /// <para>PURE and UNWIRED until US-21: it lives in Assets/Editor beside its instrument (`UsVetoDrawCheck`, the backtest that runs this arithmetic on
    /// the record) and is moved byte for byte into Assets/Scripts/Elections when US-21's gate first calls it, so no runtime file goes unreached before.
    /// No SimulationRandom stream is created, drawn or moved: the draw is keyed (<see cref="Unit"/>, <see cref="Key"/>), as Poland's
    /// <see cref="PresidentialVeto.Draw"/> - whose arithmetic this repeats without editing it - so a projection, a reload, the shadow and a fork that
    /// reach the vote on the same day draw alike. A projection reads exact probabilities (<see cref="PassProbability"/>) and never draws; only the
    /// gate draws (<see cref="Draw"/>), and its counter (<see cref="DrawCalls"/>) proves the budget votes and the five other countries draw nothing.</para>
    /// </summary>
    public static class UsVeto
    {
        /// <summary>The minimum observations a cell is fitted on (the block's N_MIN, preregistered with no data read).</summary>
        public const int MinimumObservations = 12;

        /// <summary>H1: the draw is the USA's alone - every other country's chambers vote their blocs, untouched.</summary>
        public static bool Applies(CountryId country) => country == CountryId.USA;

        /// <summary>The chamber a draw is keyed to.</summary>
        public enum Chamber { House, Senate }

        /// <summary>How a chamber decides. House: a majority of those voting, a tie failing (suspension's two thirds not modelled). SenateMajority: a
        /// statute's expedited procedure or reconciliation - a majority, the Vice President breaking a tie. SenateCloture: Rule XXII - three fifths of
        /// the senators duly chosen and sworn, floor((3W + 4) / 5), never a fixed 60; passage then follows on the same draw.</summary>
        public enum Procedure { House, SenateMajority, SenateCloture }

        // ------------------------------------------------------------------ the key and the unit
        /// <summary>
        /// UNIT(key): FNV-1a 64 over the key's UTF-8 bytes (offset 14695981039346656037, prime 1099511628211), finished with MurmurHash3's fmix64;
        /// the top 53 bits are the fraction, (h &gt;&gt; 11) / 2^53 - <see cref="PresidentialVeto.Draw"/>'s arithmetic, repeated here (that file is not
        /// edited, and this one never reads its plant).
        /// </summary>
        public static double Unit(string key)
        {
            ulong h = 14695981039346656037UL;
            foreach (byte b in Encoding.UTF8.GetBytes(key ?? string.Empty)) { h ^= b; h *= 1099511628211UL; }
            h ^= h >> 33; h *= 0xff51afd7ed558ccdUL; h ^= h >> 33; h *= 0xc4ceb9fe1a85ec53UL; h ^= h >> 33;
            return (h >> 11) * (1.0 / (1UL << 53));
        }

        /// <summary>
        /// THE PLAY KEY: masterSeed (invariant) | USA | the date of THAT CHAMBER'S FIRST DIVISION on the bill (yyyy-MM-dd) | the title | House or
        /// Senate | REP or DEM | defectors. The date is carried on the bill for every later vote of that chamber on it, so one draw per party per
        /// chamber per bill serves its cloture, passage and concurrence. PREMISE, stated (Poland's): two divisions with one title in one chamber on one
        /// day are one act; a bill retitled, or first divided on another day, draws apart.
        /// </summary>
        public static string Key(int masterSeed, DateTime firstDivision, string title, Chamber chamber, string party)
        {
            if (party != "REP" && party != "DEM") { throw new ArgumentException("a draw's party is REP or DEM - a third or created party votes its bloc", nameof(party)); }
            return masterSeed.ToString(CultureInfo.InvariantCulture) + "|USA|" + firstDivision.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) + "|" + (title ?? string.Empty)
                + "|" + (chamber == Chamber.House ? "House" : "Senate") + "|" + party + "|defectors";
        }

        /// <summary>The drawn index into a cell's list of n observations: floor(u * n) in IEEE-754 double, clamped to n - 1.</summary>
        public static int Index(double u, int n) => Math.Min(n - 1, (int)Math.Floor(u * n));

        // ------------------------------------------------------------------ the crossers
        /// <summary>The cap: crossers stay a strict minority of the bloc - floor((S - 1) / 2), and 0 for a party with no seat.</summary>
        public static int Cap(int seats) => seats <= 0 ? 0 : (seats - 1) / 2;

        /// <summary>
        /// K, the crossers a bloc of <paramref name="seats"/> sends to the other side for the drawn observation (<paramref name="defectors"/> of
        /// <paramref name="voting"/>): the recorded crossing share carried onto the bloc and rounded half up, floor((2dS + m) / (2m)), capped at
        /// floor((S - 1) / 2); 0 where S = 0. Integer arithmetic throughout.
        /// </summary>
        public static int Crossers(int seats, int defectors, int voting)
        {
            if (seats <= 0) { return 0; }
            if (voting <= 0 || defectors < 0 || defectors > voting) { throw new ArgumentException("an observation is d of m with 0 <= d <= m and m >= 1"); }
            long carried = (2L * defectors * seats + voting) / (2L * voting);
            return (int)Math.Min(Cap(seats), carried);
        }

        // ------------------------------------------------------------------ a chamber's vote
        /// <summary>
        /// One chamber's vote on one bill: its procedure, the bloc yeas and nays with every K = 0 (<see cref="Yea0"/>, <see cref="Nay0"/>, the side
        /// each seat counted takes - House independents and vacancies as the caller's premise counts them), the sworn base for cloture, the Vice
        /// President's tie vote, and the two draws. A party that draws has its list (in fit order) and its bloc seats; a party that does not draw has a
        /// null list. The president's party draws only standing AGAINST (its crossers move nay to yea); the other party only standing FOR (its crossers
        /// move yea to nay). So Y = Yea0 + Kp - Ko and N = Nay0 - Kp + Ko.
        /// </summary>
        public sealed class ChamberVote
        {
            public Procedure Procedure;
            public int Yea0, Nay0, Sworn;
            /// <summary>The Vice President's vote on a Senate majority tie: +1 yea, -1 nay, 0 none (a vacant vice presidency, or no Vice President
            /// modelled) - a tie then fails. He has no cloture vote and is never a crosser.</summary>
            public int VicePresident;
            public int PpSeats, OpSeats;
            public IReadOnlyList<(int Defectors, int Voting)> PpList, OpList;
        }

        /// <summary>Whether the chamber passes the bill with <paramref name="kp"/> of the president's party crossing to yea and <paramref name="ko"/>
        /// of the other party crossing to nay.</summary>
        public static bool ChamberOutcome(ChamberVote v, int kp, int ko)
        {
            int y = v.Yea0 + kp - ko, n = v.Nay0 - kp + ko;
            switch (v.Procedure)
            {
                case Procedure.House: return y > n;
                case Procedure.SenateMajority: return y > n || (y == n && v.VicePresident > 0);
                case Procedure.SenateCloture: return y >= (3 * v.Sworn + 4) / 5;
                default: throw new ArgumentOutOfRangeException(nameof(v));
            }
        }

        /// <summary>The crossers each index of a party's list gives: the list's K values in its order, or the single K = 0 where the party does not draw
        /// or its list is empty (n := 1).</summary>
        public static int[] CrossersOf(IReadOnlyList<(int Defectors, int Voting)> list, int seats)
        {
            if (list == null || list.Count == 0) { return new[] { 0 }; }
            var k = new int[list.Count];
            for (int i = 0; i < list.Count; i++) { k[i] = Crossers(seats, list[i].Defectors, list[i].Voting); }
            return k;
        }

        /// <summary>An exact chance: <see cref="Passing"/> of <see cref="Total"/> equally likely index pairs.</summary>
        public readonly struct Odds
        {
            public readonly long Passing, Total;
            public Odds(long passing, long total) { Passing = passing; Total = total; }
            public override string ToString() => Passing.ToString(CultureInfo.InvariantCulture) + "/" + Total.ToString(CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// P(pass), exact: the number of index pairs (j_PP, j_OP) over the two lists whose crossers pass the chamber, over n_PP * n_OP - a party with
        /// no draw, or an empty list, being the single index K = 0. Counted on the histograms of K (the pass depends on Kp - Ko alone); the instrument
        /// asserts it equal to the pair-by-pair enumeration (<see cref="CrossersOf"/>, <see cref="ChamberOutcome"/>).
        /// </summary>
        public static Odds PassProbability(ChamberVote v) => CountPairs(v, (kp, ko) => ChamberOutcome(v, kp, ko));

        /// <summary>The exact count of index pairs meeting <paramref name="test"/> on (Kp, Ko), over the K histograms.</summary>
        public static Odds CountPairs(ChamberVote v, Func<int, int, bool> test)
        {
            int[] kp = CrossersOf(v.PpList, v.PpSeats), ko = CrossersOf(v.OpList, v.OpSeats);
            var hp = Histogram(kp); var ho = Histogram(ko);
            long passing = 0;
            foreach (KeyValuePair<int, long> p in hp) { foreach (KeyValuePair<int, long> o in ho) { if (test(p.Key, o.Key)) { passing += p.Value * o.Value; } } }
            return new Odds(passing, (long)kp.Length * ko.Length);
        }

        private static SortedDictionary<int, long> Histogram(int[] k)
        {
            var h = new SortedDictionary<int, long>();
            foreach (int x in k) { h.TryGetValue(x, out long c); h[x] = c + 1; }
            return h;
        }

        // ------------------------------------------------------------------ the veto and the override
        /// <summary>
        /// Rule (a), DETERMINISTIC (H1: "Rule (a) then decides"): the president vetoes a bill sent to him iff his party holds a House seat and more than
        /// half its House bloc voted nay - 2 * nays &gt; S. The nays are S - Kp where the party stood against, 0 otherwise; under the cap that is always
        /// more than half, so the draw decides whether a bill reaches the president, never whether (a) fires. A president whose party holds no House seat
        /// vetoes nothing. A player-president chooses, and (a) is not read for him.
        /// </summary>
        public static bool NamedByA(int ppHouseSeats, int ppHouseNays) => ppHouseSeats >= 1 && 2 * ppHouseNays > ppHouseSeats;

        /// <summary>The override (CRS RS22654): "two-thirds of the Members voting, a quorum being present" - a chamber overrides iff Y &gt;= 2N, on the
        /// passage's recorded sides (crossers counting only where they entered the passage's division). The origin chamber votes first; the veto falls
        /// iff both override.</summary>
        public static bool OverrideCarries(int yea, int nay) => yea >= 2 * nay;

        // ------------------------------------------------------------------ the gate's draw
        /// <summary>
        /// THE GATE'S DRAW: K for one party in one chamber on one bill - the keyed unit over the play key, the index into the cell's list, the crossers.
        /// Refused (K = 0, nothing counted, nothing hashed) outside the USA, on a budget vote (H1: the appropriation, a continuing resolution, the
        /// pension-age act, reconciliation, the debt-limit act), for a party with no seat, and for any party but REP or DEM. An empty list is the single
        /// index K = 0. Every call not refused is counted in <see cref="DrawCalls"/>; inside <see cref="PlantIndex"/> the index is the planted one.
        /// </summary>
        public static int Draw(CountryId country, bool budgetVote, int masterSeed, DateTime firstDivision, string title, Chamber chamber, string party, int seats,
            IReadOnlyList<(int Defectors, int Voting)> list, out int index)
        {
            index = 0;
            if (!Applies(country) || budgetVote || seats <= 0 || (party != "REP" && party != "DEM")) { return 0; }
            _drawCalls++;
            if (list == null || list.Count == 0) { return 0; }
            index = _planted != null ? Math.Min(list.Count - 1, Math.Max(0, _planted(chamber, party, list.Count))) : Index(Unit(Key(masterSeed, firstDivision, title, chamber, party)), list.Count);
            return Crossers(seats, list[index].Defectors, list[index].Voting);
        }

        /// <summary>The draws made since the counter was last reset - planted or keyed, each counted; 0 on every budget vote and every non-US path.</summary>
        public static long DrawCalls => _drawCalls;
        public static void ResetDrawCalls() { _drawCalls = 0; }
        [ThreadStatic] private static long _drawCalls;

        [ThreadStatic] private static Func<Chamber, string, int, int> _planted;

        /// <summary>For the checks: every <see cref="Draw"/> inside the returned scope takes index <paramref name="index"/> (clamped to the list, so
        /// int.MaxValue plants the largest share, n - 1) instead of the keyed one. UsVeto's own plant: Poland's is never read.</summary>
        public static IDisposable PlantIndex(int index) => PlantIndex((chamber, party, n) => index);

        /// <summary>The plant per chamber and party: <paramref name="pick"/> gives the index from the chamber, the party (REP or DEM) and the list's n -
        /// so a check can enumerate the index pairs (j_PP, j_OP) of a chamber one planted draw at a time. Disposing the scope restores the draw.</summary>
        public static IDisposable PlantIndex(Func<Chamber, string, int, int> pick)
        {
            var restore = new PlantedScope(_planted);
            _planted = pick;
            return restore;
        }

        private sealed class PlantedScope : IDisposable
        {
            private readonly Func<Chamber, string, int, int> _before;
            public PlantedScope(Func<Chamber, string, int, int> before) { _before = before; }
            public void Dispose() { _planted = _before; }
        }
    }
}
