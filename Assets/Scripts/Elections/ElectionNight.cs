using System;
using System.Collections.Generic;

namespace PoliSim.Elections
{
    /// <summary>
    /// W-E6 — election night as a MODEL, before it is a screen. PURE, WIRED TO NOTHING (R-N2).
    ///
    /// **Two rules govern this file and both are structural, not conventions to be remembered.**
    ///
    /// 1. **No result exists before its constituency has declared.** The running tally is not a
    ///    number that is revealed gradually — it is COMPUTED, at every instant, from the declared
    ///    constituencies and from nothing else (<see cref="NightState.At"/>). A constituency that
    ///    has not declared contributes no votes, and its own figures are reported as ABSENT rather
    ///    than as zero (<see cref="ConstituencyReport.Declared"/>). The drama is the data arriving;
    ///    a scripted reveal would be a different thing wearing its clothes, and the harness asserts
    ///    the difference by re-deriving the tally from the declared set at every step.
    ///
    /// 2. **A call, once made, cannot be contradicted by the final tally.** Not "is unlikely to
    ///    be" — CANNOT. Every call is a claim that holds across the whole feasible range of what is
    ///    still outstanding: each undeclared constituency is bounded by its own ELIGIBLE electorate
    ///    (a published figure known before a single vote is counted, and a hard cap since turnout
    ///    cannot exceed 100 %), and a call is made only when the claim holds at BOTH extremes —
    ///    every outstanding vote going to the claim's enemy, and none of them going anywhere. The
    ///    seats at those extremes come from <see cref="SeatAllocation"/>, the same allocation the
    ///    backtest reproduces Sweden 2022 with seat-for-seat; nothing here re-implements it.
    ///
    /// The cost of rule 2 is honest and is reported rather than tuned away: a guarantee arrives
    /// later than a projection would. A network calls on a model and is sometimes wrong; this calls
    /// on a bound and is never wrong, and the harness prints how late that makes it.
    /// </summary>
    public enum CallKind
    {
        /// <summary>A party will finish above the national threshold, whatever is still out.</summary>
        ThresholdCleared = 0,
        /// <summary>A party cannot reach the threshold, whatever is still out.</summary>
        ThresholdMissed = 1,
        /// <summary>A party will finish with more votes than every other, whatever is still out.</summary>
        LargestParty = 2,
        /// <summary>A named bloc will hold an absolute majority of seats, whatever is still out.</summary>
        BlocMajority = 3,
        /// <summary>A named bloc CANNOT hold an absolute majority, whatever is still out.</summary>
        BlocShortOfMajority = 4,
    }

    /// <summary>One call, with the moment it became safe and the evidence that made it safe.</summary>
    public readonly struct ElectionCall
    {
        public readonly CallKind Kind;
        /// <summary>The party the call is about (-1 for a bloc call).</summary>
        public readonly int Party;
        /// <summary>The bloc the call is about (null for a party call).</summary>
        public readonly string Bloc;
        /// <summary>How many constituencies had declared when the call became safe.</summary>
        public readonly int DeclaredAt;
        public readonly int OfTotal;
        /// <summary>The bound that made it safe, in the call's own units - a share, a vote gap or a seat count.</summary>
        public readonly double Margin;

        public ElectionCall(CallKind kind, int party, string bloc, int declaredAt, int ofTotal, double margin)
        {
            Kind = kind; Party = party; Bloc = bloc; DeclaredAt = declaredAt; OfTotal = ofTotal; Margin = margin;
        }
    }

    /// <summary>What one constituency contributes to the night - and, before it declares, what it does NOT.</summary>
    public readonly struct ConstituencyReport
    {
        public readonly string Name;
        /// <summary>False until this constituency's arrival time. While false EVERY figure below is meaningless and the screen must draw absence, not zero.</summary>
        public readonly bool Declared;
        /// <summary>Votes per party. Only meaningful when <see cref="Declared"/>.</summary>
        public readonly long[] Votes;
        public readonly long Valid;
        /// <summary>The published electorate - known BEFORE the night, so it is legible whether or not the constituency has declared. This is what bounds the outstanding.</summary>
        public readonly long Eligible;
        /// <summary>The minute of the night this one declares.</summary>
        public readonly int ArrivesAtMinute;

        public ConstituencyReport(string name, bool declared, long[] votes, long valid, long eligible, int arrivesAtMinute)
        {
            Name = name; Declared = declared; Votes = votes; Valid = valid; Eligible = eligible; ArrivesAtMinute = arrivesAtMinute;
        }
    }

    /// <summary>
    /// The night as the screen reads it at one instant: which constituencies have declared, the
    /// tally OF THOSE ONLY, the seats that tally would give if nothing else arrived, and the calls
    /// that are safe.
    /// </summary>
    public sealed class NightState
    {
        public int Minute;
        public int DeclaredCount;
        public int TotalConstituencies;
        public ConstituencyReport[] Constituencies;
        /// <summary>Votes per party over the DECLARED constituencies only.</summary>
        public long[] CountedVotes;
        public long CountedValid;
        /// <summary>The electorate of everything still out - the bound every call is made against.</summary>
        public long OutstandingEligible;
        /// <summary>Seats on the counted votes alone, by the exact allocation. A PROJECTION, and the screen must say so until the last constituency is in.</summary>
        public int[] SeatsOnCounted;
        public List<ElectionCall> Calls = new List<ElectionCall>();

        public bool Complete => DeclaredCount == TotalConstituencies;

        /// <summary>Share of the counted vote, per party - never of the whole electorate, and never a projection dressed as a count.</summary>
        public double CountedShare(int party) => CountedValid <= 0 ? 0.0 : (double)CountedVotes[party] / CountedValid;
    }

    /// <summary>The night itself: the arrival schedule, the gated tally, and the calls.</summary>
    public static class ElectionNight
    {
        /// <summary>[AUTHORED-DRAFT] the night runs from the close of polls to this many minutes after. Sweden's real count runs past midnight; four hours is the dramatic window a screen can hold.</summary>
        public const int NightMinutes = 240;

        /// <summary>[AUTHORED-DRAFT] the earliest any constituency declares - nothing lands in the first quarter hour, because a night that opens with results has no opening.</summary>
        public const int FirstDeclarationMinute = 15;

        /// <summary>
        /// The declaration schedule, DETERMINISTIC under the seed. Small constituencies count
        /// faster than large ones, which is a fact about counting and not a dramatic choice, so the
        /// expected arrival is monotone in the electorate; the seeded draw then moves each one
        /// within a band so two runs of one seed are identical and two seeds differ.
        ///
        /// Returns a minute per constituency. The LAST one is pinned to the end of the night, so
        /// "final" is a state the night actually reaches rather than one it approaches.
        /// </summary>
        public static int[] Schedule(long[] eligible, System.Random random)
        {
            if (eligible == null || eligible.Length == 0) { throw new ArgumentException("no constituencies"); }
            if (random == null) { throw new ArgumentNullException(nameof(random)); }

            long smallest = long.MaxValue, largest = 0;
            foreach (long e in eligible) { if (e < smallest) { smallest = e; } if (e > largest) { largest = e; } }
            double span = Math.Max(1.0, largest - smallest);

            var minutes = new int[eligible.Length];
            for (int i = 0; i < eligible.Length; i++)
            {
                double size = (eligible[i] - smallest) / span;                 // 0 = smallest, 1 = largest
                double expected = FirstDeclarationMinute + size * (NightMinutes - FirstDeclarationMinute - 20);
                double jitter = (random.NextDouble() - 0.5) * 2.0 * ScheduleJitterMinutes;
                minutes[i] = (int)Math.Round(Math.Min(NightMinutes, Math.Max(FirstDeclarationMinute, expected + jitter)));
            }

            // The night must END: whatever the jitter did, the last one lands on the last minute.
            int latest = 0;
            for (int i = 0; i < minutes.Length; i++) { if (minutes[i] > minutes[latest]) { latest = i; } }
            minutes[latest] = NightMinutes;
            return minutes;
        }

        /// <summary>[AUTHORED-DRAFT] how far a constituency's declaration can slip from what its size predicts.</summary>
        public const double ScheduleJitterMinutes = 25.0;

        /// <summary>
        /// The night at one instant. **The gate is here and it is the whole point:** a constituency
        /// contributes to `CountedVotes` if and only if its arrival minute has passed, and its own
        /// report carries `Declared = false` until then so a screen cannot draw a figure that has
        /// not happened. Nothing is revealed; everything is summed.
        /// </summary>
        public static NightState At(int minute, string[] names, long[][] votes, long[] valid, long[] eligible,
            int[] arrivals, int seats, double threshold, string[] partyNames,
            Func<int, double> divisor = null, IDictionary<string, int[]> blocs = null)
        {
            if (names == null || votes == null || arrivals == null) { throw new ArgumentNullException(nameof(names)); }
            int regions = names.Length;
            int parties = partyNames.Length;
            var state = new NightState
            {
                Minute = minute,
                TotalConstituencies = regions,
                Constituencies = new ConstituencyReport[regions],
                CountedVotes = new long[parties],
            };

            for (int r = 0; r < regions; r++)
            {
                bool declared = arrivals[r] <= minute;
                state.Constituencies[r] = new ConstituencyReport(names[r], declared,
                    declared ? votes[r] : null, declared ? valid[r] : 0L, eligible[r], arrivals[r]);

                if (declared)
                {
                    state.DeclaredCount++;
                    state.CountedValid += valid[r];
                    for (int p = 0; p < parties; p++) { state.CountedVotes[p] += votes[r][p]; }
                }
                else
                {
                    state.OutstandingEligible += eligible[r];
                }
            }

            Func<int, double> d = divisor ?? SeatAllocation.ModifiedSainteLagueDivisor;
            state.SeatsOnCounted = state.CountedValid > 0
                ? SeatAllocation.AllocateWithThreshold(state.CountedVotes, state.CountedValid, threshold, seats, d)
                : new int[parties];

            state.Calls = SafeCalls(state, seats, threshold, partyNames, d, blocs);
            return state;
        }

        /// <summary>
        /// Every call that is SAFE at this instant — safe meaning it holds at both extremes of what
        /// is still outstanding, so no arrival can contradict it.
        ///
        /// The bound is the outstanding ELIGIBLE electorate: a published figure, known before the
        /// night, and a hard cap because turnout cannot exceed 100 %. `O` below is that number.
        /// - A party's FLOOR of votes is what it has counted; its CEILING is that plus O.
        /// - The valid vote's FLOOR is what has been counted; its CEILING is that plus O.
        /// - So a party's share floor is `counted[p] / (countedValid + O)` (it gains nothing while
        ///   everyone else gains everything) and its share ceiling is
        ///   `(counted[p] + O) / (countedValid + O)`.
        /// Every call below is one of those two bounds crossing a line that the exact allocation,
        /// not an approximation of it, draws.
        /// </summary>
        private static List<ElectionCall> SafeCalls(NightState state, int seats, double threshold,
            string[] partyNames, Func<int, double> divisor, IDictionary<string, int[]> blocs)
        {
            var calls = new List<ElectionCall>();
            int parties = state.CountedVotes.Length;
            long o = state.OutstandingEligible;
            long floorValid = state.CountedValid;
            long ceilingValid = state.CountedValid + o;
            if (floorValid <= 0) { return calls; }

            for (int p = 0; p < parties; p++)
            {
                double shareFloor = (double)state.CountedVotes[p] / ceilingValid;
                double shareCeiling = ceilingValid > 0 ? (double)(state.CountedVotes[p] + o) / ceilingValid : 0.0;

                if (shareFloor > threshold)
                {
                    calls.Add(new ElectionCall(CallKind.ThresholdCleared, p, null, state.DeclaredCount, state.TotalConstituencies, shareFloor - threshold));
                }
                else if (shareCeiling < threshold)
                {
                    calls.Add(new ElectionCall(CallKind.ThresholdMissed, p, null, state.DeclaredCount, state.TotalConstituencies, threshold - shareCeiling));
                }

                // Largest party: p's floor must beat every rival's ceiling. Both are vote counts,
                // so the denominator plays no part and the comparison is exact.
                bool largest = true;
                long worstGap = long.MaxValue;
                for (int q = 0; q < parties && largest; q++)
                {
                    if (q == p) { continue; }
                    long gap = state.CountedVotes[p] - (state.CountedVotes[q] + o);
                    if (gap <= 0) { largest = false; }
                    else if (gap < worstGap) { worstGap = gap; }
                }

                if (largest && parties > 1)
                {
                    calls.Add(new ElectionCall(CallKind.LargestParty, p, null, state.DeclaredCount, state.TotalConstituencies, worstGap));
                }
            }

            if (blocs != null)
            {
                int majority = seats / 2 + 1;
                foreach (KeyValuePair<string, int[]> bloc in blocs)
                {
                    // The bloc's WORST case: every outstanding vote goes to its largest opponent.
                    // The bloc's BEST case: every outstanding vote goes to its own largest member.
                    int worst = BlocSeats(state, bloc.Value, o, seats, threshold, divisor, toBloc: false);
                    int best = BlocSeats(state, bloc.Value, o, seats, threshold, divisor, toBloc: true);
                    if (worst >= majority)
                    {
                        calls.Add(new ElectionCall(CallKind.BlocMajority, -1, bloc.Key, state.DeclaredCount, state.TotalConstituencies, worst - majority));
                    }
                    else if (best < majority)
                    {
                        calls.Add(new ElectionCall(CallKind.BlocShortOfMajority, -1, bloc.Key, state.DeclaredCount, state.TotalConstituencies, majority - best));
                    }
                }
            }

            return calls;
        }

        /// <summary>
        /// The bloc's seats when every outstanding vote goes the worst (or best) way for it. The
        /// allocation is `SeatAllocation`'s — the one the backtest reproduces Sweden 2022 with
        /// seat-for-seat — so a call can never rest on arithmetic the final tally does not use.
        /// </summary>
        private static int BlocSeats(NightState state, int[] members, long outstanding, int seats,
            double threshold, Func<int, double> divisor, bool toBloc)
        {
            int parties = state.CountedVotes.Length;
            var inBloc = new bool[parties];
            foreach (int m in members) { if (m >= 0 && m < parties) { inBloc[m] = true; } }

            // Give the whole outstanding electorate to ONE party: the bloc's strongest member for
            // the best case, its strongest opponent for the worst. Concentrating it is what makes
            // the bound extreme, and an extreme bound is what makes the call safe.
            int target = -1;
            for (int p = 0; p < parties; p++)
            {
                if (inBloc[p] != toBloc) { continue; }
                if (target < 0 || state.CountedVotes[p] > state.CountedVotes[target]) { target = p; }
            }

            var votes = new long[parties];
            Array.Copy(state.CountedVotes, votes, parties);
            if (target >= 0) { votes[target] += outstanding; }

            long valid = state.CountedValid + outstanding;
            int[] allocated = SeatAllocation.AllocateWithThreshold(votes, valid, threshold, seats, divisor);

            int total = 0;
            for (int p = 0; p < parties; p++) { if (inBloc[p]) { total += allocated[p]; } }
            return total;
        }
    }
}
