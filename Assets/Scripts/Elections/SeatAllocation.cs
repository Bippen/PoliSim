using System;
using System.Collections.Generic;

namespace PoliSim.Elections
{
    /// <summary>
    /// The elections track's vote-to-seat layer (overnight 2026-08-28→29, E-2's first landed
    /// piece) — PURE FUNCTIONS, WIRED TO NOTHING (R-N2: no gameplay path, no UI, no turn-loop
    /// or save hook reaches this namespace; the only caller is the editor backtest harness).
    ///
    /// This file is the C# port-and-reproduce that `COMPLETED.md §188`
    /// Part 5 demands before any allocator claim is relied on ("port seat_allocation_check.py
    /// to C# and reproduce its numbers; re-derive Germany and Poland from scratch"). The
    /// methods implemented are exactly the SOURCED ones (`ElectionsData/<country>/returns_*.md`
    /// carry the statute cites):
    /// - d'Hondt (divisors 1, 2, 3, …) — Poland's per-district method (Kodeks wyborczy
    ///   art. 232 §1); run NATIONALLY it is a DIFFERENT, more proportional system (the recorded
    ///   70-seat signature) and the harness uses that deliberately, as a signature check.
    /// - Sainte-Laguë/Schepers (1, 3, 5, …) — Germany's national method (BWahlG).
    /// - Modified Sainte-Laguë (1.2, then 3, 5, 7, …) — Sweden's jämkade uddatalsmetoden
    ///   (val.se; the 2022 decision PDF's own first quotients confirm the 1.2).
    ///
    /// Determinism: no randomness anywhere. Tie policy (two equal quotients for the last
    /// seat): the larger raw vote count wins, then the LOWER party index — stated so the
    /// enumeration is complete; the real statutes draw lots (Sweden) or have their own rules,
    /// and no sourced dataset in tonight's harness produces a tie. A future wiring pass
    /// replaces the tie branch with the statute's rule per country and a seeded stream draw
    /// where the statute says lots (`SimulationRandom.Stream.ElectionNoise`, reserved, not
    /// taken — COMPLETED.md §183).
    ///
    /// All arithmetic is double quotients over long votes; no parsing, no formatting — the
    /// B3 decimal-comma class cannot reach a function with no string in its signature.
    /// </summary>
    public static class SeatAllocation
    {
        /// <summary>Divisor sequences as functions of the number of seats a party ALREADY holds (k = 0 for its first seat).</summary>
        public static double DHondtDivisor(int k) => k + 1;

        public static double SainteLagueDivisor(int k) => 2 * k + 1;

        /// <summary>Sweden's jämkade uddatalsmetoden: 1.2 for the first seat, then the odd numbers 3, 5, 7…</summary>
        public static double ModifiedSainteLagueDivisor(int k) => k == 0 ? 1.2 : 2 * k + 1;

        /// <summary>
        /// Highest-averages allocation: <paramref name="seats"/> seats over <paramref name="votes"/>
        /// (only ELIGIBLE parties — apply thresholds first), each next seat to the party with the
        /// highest quotient votes[i] / divisor(heldSeats[i]). Returns seats per party, same order.
        /// </summary>
        public static int[] HighestAverages(long[] votes, int seats, Func<int, double> divisor)
        {
            if (votes == null) { throw new ArgumentNullException(nameof(votes)); }

            var result = new int[votes.Length];
            for (int s = 0; s < seats; s++)
            {
                int best = -1;
                double bestQuotient = double.NegativeInfinity;
                for (int i = 0; i < votes.Length; i++)
                {
                    if (votes[i] <= 0) { continue; }

                    double q = votes[i] / divisor(result[i]);
                    // Tie policy, stated in the class doc: larger raw votes, then lower index.
                    if (q > bestQuotient
                        || (q == bestQuotient && best >= 0 && votes[i] > votes[best]))
                    {
                        best = i;
                        bestQuotient = q;
                    }
                }

                if (best < 0)
                {
                    break; // no party with votes remains - fewer seats assigned than asked, honestly
                }

                result[best]++;
            }

            return result;
        }

        /// <summary>
        /// National threshold as a share of TOTAL VALID VOTES (Sweden 4 %, Germany 5 %, Poland
        /// 5 %/8 %): a party is eligible when its votes/totalValid >= threshold, or when its
        /// <paramref name="exempt"/> flag is set (minority-list regimes: SSW under § 4(2) BWahlG,
        /// MN under Kodeks wyborczy art. 197 §1). totalValid is the DENOMINATOR THE STATUTE
        /// NAMES — pass the source's own valid-votes figure, never a sum of the listed parties
        /// (the excluded small lists are part of the denominator).
        /// </summary>
        public static bool[] ApplyNationalThreshold(long[] votes, long totalValid, double threshold, bool[] exempt = null)
        {
            var eligible = new bool[votes.Length];
            for (int i = 0; i < votes.Length; i++)
            {
                eligible[i] = (exempt != null && exempt[i]) || (double)votes[i] / totalValid >= threshold;
            }

            return eligible;
        }

        /// <summary>Convenience: allocation over the eligible subset, zeros for the ineligible, original order kept.</summary>
        public static int[] AllocateWithThreshold(long[] votes, long totalValid, double threshold, int seats, Func<int, double> divisor, bool[] exempt = null)
        {
            bool[] eligible = ApplyNationalThreshold(votes, totalValid, threshold, exempt);
            var masked = new long[votes.Length];
            for (int i = 0; i < votes.Length; i++)
            {
                masked[i] = eligible[i] ? votes[i] : 0;
            }

            return HighestAverages(masked, seats, divisor);
        }

        /// <summary>
        /// Per-district allocation summed to a national result (Poland's REAL system: d'Hondt in
        /// each okręg over that district's eligible lists — eligibility is decided NATIONALLY
        /// first, art. 196, then the district runs alone). districtVotes[d][p]; magnitudes[d].
        /// The night's data bill notes the per-district ABSOLUTE counts still owed for Poland —
        /// this function is ready for them and is exercised tonight on synthetic vectors only.
        /// </summary>
        public static int[] PerDistrictSum(long[][] districtVotes, int[] magnitudes, bool[] nationallyEligible, Func<int, double> divisor)
        {
            if (districtVotes.Length != magnitudes.Length) { throw new ArgumentException("districts/magnitudes mismatch"); }

            int parties = districtVotes[0].Length;
            var total = new int[parties];
            for (int d = 0; d < districtVotes.Length; d++)
            {
                var masked = new long[parties];
                for (int p = 0; p < parties; p++)
                {
                    masked[p] = nationallyEligible[p] ? districtVotes[d][p] : 0;
                }

                int[] seats = HighestAverages(masked, magnitudes[d], divisor);
                for (int p = 0; p < parties; p++)
                {
                    total[p] += seats[p];
                }
            }

            return total;
        }
    }
}
