using System;
using PoliSim.Data;

namespace PoliSim.Simulation
{
    /// <summary>
    /// Votes to seats. Pure functions over primitive arrays - no Unity types, no game state, no
    /// randomness - so the whole of this file runs in the standalone C# harness and in a plain unit
    /// test, which is the property that makes the reproduction test in the roadmap's §3.1 cheap enough
    /// to actually run.
    ///
    /// <para><b>The standard this file is held to is REPRODUCTION, not plausibility.</b> Fed Sweden's
    /// real 2022 vote shares it must return 107/73/68/24/24/19/18/16, not "roughly that". Fed Germany's
    /// 2025 second votes it must return 630 seats split 164/152/120/85/64/44/1. A formula that returns
    /// approximately the right answer is not implemented, it is approximated, and the difference is
    /// invisible until a player who knows the real result looks at the opening screen.</para>
    ///
    /// <para>⚠ <b>Ties are broken deterministically here and by LOT in real law.</b> Swedish and Polish
    /// electoral law both resolve an exact quotient tie by drawing lots; this returns the party with
    /// more raw votes, and failing that the earlier index. That is a deliberate departure: a simulation
    /// that produced a different chamber each time it loaded the same seed would break the
    /// reproducibility every other validation in this project depends on. Exact ties are vanishingly
    /// rare at national scale - they need identical vote counts - so this costs realism only in a case
    /// that will not occur.</para>
    /// </summary>
    public static class SeatAllocation
    {
        /// <summary>Sweden's first divisor since the 2018 reform. **Was 1.4**, and using the old value quietly under-rewards small parties by a seat or two - see ElectoralFormula.SainteLagueModified.</summary>
        public const double SwedishFirstDivisor = 1.2;

        /// <summary>
        /// Allocates <paramref name="seats"/> across <paramref name="votes"/> by the given formula.
        ///
        /// <para>Thresholds are NOT applied here - call <see cref="ApplyThreshold"/> first and pass the
        /// filtered vote array. The two are separate because a threshold is a legal test on parties
        /// while this is arithmetic on numbers, and because Germany's basic-mandate clause needs
        /// constituency results the allocator has no business knowing about.</para>
        /// </summary>
        /// <returns>Seats per party, same indexing as <paramref name="votes"/>, summing exactly to <paramref name="seats"/>.</returns>
        public static int[] Allocate(ElectoralFormula formula, long[] votes, int seats)
        {
            if (votes == null)
            {
                throw new ArgumentNullException(nameof(votes));
            }

            var result = new int[votes.Length];
            if (seats <= 0 || votes.Length == 0)
            {
                return result;
            }

            switch (formula)
            {
                case ElectoralFormula.SainteLagueModified:
                    return AllocateHighestAverages(votes, seats, SwedishFirstDivisor, oddDivisors: true);
                case ElectoralFormula.DHondt:
                    return AllocateHighestAverages(votes, seats, firstDivisor: 1.0, oddDivisors: false);
                case ElectoralFormula.LargestRemainder:
                    return AllocateLargestRemainder(votes, seats);
                default:
                    throw new NotSupportedException(
                        $"{formula} is not a list-allocation formula. FPTP, TwoRound, ElectoralCollege " +
                        "and MixedMemberProportional resolve per constituency or per state and have " +
                        "their own entry points; IndirectlyElected has no election at all.");
            }
        }

        /// <summary>
        /// The highest-averages family. Each party's quotient is its votes divided by a divisor that
        /// grows with the seats it already holds; the largest quotient takes the next seat.
        ///
        /// <para><paramref name="oddDivisors"/> selects the family: true gives Sainte-Laguë's 1, 3, 5,
        /// 7... (with the first term overridden by <paramref name="firstDivisor"/> for the modified
        /// variant), false gives D'Hondt's 1, 2, 3, 4... **That single boolean is the whole difference
        /// between the Swedish and Polish chambers**, and it is worth several seats to every party
        /// outside the top two.</para>
        ///
        /// <para>Seat-at-a-time rather than closed-form: it is O(seats x parties) - 630 x 8 at the worst
        /// real size, which is nothing - and it is the formulation the law is written in, so it stays
        /// readable against the statute it implements.</para>
        /// </summary>
        private static int[] AllocateHighestAverages(long[] votes, int seats, double firstDivisor, bool oddDivisors)
        {
            var awarded = new int[votes.Length];

            for (int seat = 0; seat < seats; seat++)
            {
                int best = -1;
                double bestQuotient = double.NegativeInfinity;

                for (int party = 0; party < votes.Length; party++)
                {
                    if (votes[party] <= 0L)
                    {
                        continue;
                    }

                    double divisor = Divisor(awarded[party], firstDivisor, oddDivisors);
                    double quotient = votes[party] / divisor;

                    // Strictly-greater, so the earlier index wins an exact tie once raw votes have also
                    // tied - see this class's doc comment on why lots are not drawn.
                    if (quotient > bestQuotient || (quotient == bestQuotient && best >= 0 && votes[party] > votes[best]))
                    {
                        bestQuotient = quotient;
                        best = party;
                    }
                }

                if (best < 0)
                {
                    // Every party polled zero. Returning early leaves seats unfilled, which is honest -
                    // there is no defensible way to distribute a chamber among parties nobody voted for.
                    break;
                }

                awarded[best]++;
            }

            return awarded;
        }

        private static double Divisor(int seatsAlreadyHeld, double firstDivisor, bool oddDivisors)
        {
            if (seatsAlreadyHeld == 0)
            {
                return firstDivisor;
            }

            return oddDivisors ? 2 * seatsAlreadyHeld + 1 : seatsAlreadyHeld + 1;
        }

        /// <summary>
        /// Hare quota plus largest remainders - Italy's Rosatellum PR tier.
        ///
        /// <para>The quota is total votes / seats, each party takes the whole number of quotas it can
        /// afford, and the seats left over go to the largest fractional remainders. Integer division
        /// deliberately, then remainders compared as exact cross-multiplied integers rather than as
        /// floats: at Italian vote counts a double has enough precision, but a remainder comparison that
        /// is right for the wrong reason is the kind of thing that silently changes a seat when the
        /// numbers move.</para>
        /// </summary>
        private static int[] AllocateLargestRemainder(long[] votes, int seats)
        {
            var awarded = new int[votes.Length];

            long totalVotes = 0L;
            foreach (long v in votes)
            {
                if (v > 0L)
                {
                    totalVotes += v;
                }
            }

            if (totalVotes <= 0L)
            {
                return awarded;
            }

            int allocated = 0;
            for (int party = 0; party < votes.Length; party++)
            {
                if (votes[party] <= 0L)
                {
                    continue;
                }

                // floor(votes * seats / total) - the quota expressed so it never leaves integer maths.
                int full = (int)(votes[party] * seats / totalVotes);
                awarded[party] = full;
                allocated += full;
            }

            // Remainder = votes * seats - full * total, compared directly. Same ordering as the
            // fractional part, without ever forming the fraction.
            while (allocated < seats)
            {
                int best = -1;
                long bestRemainder = -1L;

                for (int party = 0; party < votes.Length; party++)
                {
                    if (votes[party] <= 0L)
                    {
                        continue;
                    }

                    long remainder = votes[party] * seats - (long)awarded[party] * totalVotes;
                    if (remainder > bestRemainder || (remainder == bestRemainder && best >= 0 && votes[party] > votes[best]))
                    {
                        bestRemainder = remainder;
                        best = party;
                    }
                }

                if (best < 0)
                {
                    break;
                }

                awarded[best]++;
                allocated++;
            }

            return awarded;
        }

        /// <summary>
        /// Zeroes the votes of every party that failed the legal threshold, returning a NEW array so the
        /// caller keeps the real vote totals for display - a party that polled 4.9% and won nothing still
        /// polled 4.9%, and a results screen that showed it as zero would be lying.
        /// </summary>
        /// <param name="votes">Raw votes per party.</param>
        /// <param name="rule">The chamber's threshold rule.</param>
        /// <param name="bestConstituencyShare">Each party's best single-constituency vote share, 0-1, for the alternative route (Sweden's 12%, Italy's 20% in one region). Pass null where the country has no such route.</param>
        /// <param name="constituencySeatsWon">Each party's directly-won constituency seats, for Germany's basic-mandate clause. Pass null where it does not apply.</param>
        /// <param name="isRecognisedMinority">Parties exempt from the threshold entirely - Germany's SSW, Italy's SVP. Pass null where none exist.</param>
        public static long[] ApplyThreshold(
            long[] votes,
            ThresholdRule rule,
            double[] bestConstituencyShare = null,
            int[] constituencySeatsWon = null,
            bool[] isRecognisedMinority = null)
        {
            if (votes == null)
            {
                throw new ArgumentNullException(nameof(votes));
            }

            var filtered = new long[votes.Length];

            long totalVotes = 0L;
            foreach (long v in votes)
            {
                if (v > 0L)
                {
                    totalVotes += v;
                }
            }

            if (totalVotes <= 0L)
            {
                return filtered;
            }

            for (int party = 0; party < votes.Length; party++)
            {
                if (votes[party] <= 0L)
                {
                    continue;
                }

                // ⚠ Order matters, and it is the order the statutes are written in: an exemption or an
                // alternative route is checked BEFORE the national bar, never after. Checking the bar
                // first and then "adding back" reads the same and is a different rule - it loses any
                // party the bar would have removed for a reason the exemption was meant to override.
                bool exempt = isRecognisedMinority != null && isRecognisedMinority[party];

                bool clearedAlternative = bestConstituencyShare != null
                                          && rule.AlternativeConstituencyShare > 0.0
                                          && bestConstituencyShare[party] >= rule.AlternativeConstituencyShare;

                bool clearedBasicMandate = constituencySeatsWon != null
                                           && rule.BasicMandateSeats > 0
                                           && constituencySeatsWon[party] >= rule.BasicMandateSeats;

                double share = (double)votes[party] / totalVotes;
                bool clearedNational = share >= rule.NationalShare;

                if (exempt || clearedAlternative || clearedBasicMandate || clearedNational)
                {
                    filtered[party] = votes[party];
                }
            }

            return filtered;
        }
    }
}
