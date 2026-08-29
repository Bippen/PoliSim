using System;
using System.Collections.Generic;

namespace PoliSim.Elections
{
    /// <summary>
    /// W-D2 / SPEC §28 — vote-to-seat conversion on the LIVE path: an `ElectionDay.Result` in,
    /// seats per party and per valkrets out, by Sweden's own rules (vallagen 14 kap.). PURE
    /// FUNCTIONS, WIRED TO NOTHING (R-N2). `SeatAllocation` (the divisor arithmetic, exact for
    /// five chambers since the overnight) is what this calls; this file is the Swedish
    /// PROCEDURE around it, which the backtest never needed because a national totalfördelning
    /// gives the same party totals as the full procedure whenever no seat is returned.
    ///
    /// **The procedure, in the statute's order:**
    /// 1. **Who may share in seats** — 4 % of the national vote, OR 12 % in a valkrets for THAT
    ///    valkrets's fixed seats only (`Eligibility`).
    /// 2. **310 fixed seats per valkrets** — distributed among the valkretsar in proportion to
    ///    eligible voters by the statute's "one seat per 310th part, remainder by largest
    ///    surplus" rule (`FixedSeatsPerRegion`), then allocated within each valkrets by the
    ///    modified odd-number method (1.2, 3, 5, …) among the parties eligible there.
    /// 3. **The totalfördelning** — 349 seats over the nationally-eligible parties as if the
    ///    country were one valkrets, the same divisors (seats taken by 12 %-only parties deducted
    ///    first).
    /// 4. **Återföring** — a party that won more fixed seats than its total entitlement gives the
    ///    excess back, its lowest-comparison-number seats first, and each returned seat is
    ///    re-allocated within its valkrets to the party with the next highest comparison number
    ///    that still has room under its own total (the 2018 reform's rule; before it, overhangs
    ///    were left standing and the Riksdag was not exactly proportional).
    /// 5. **39 adjustment seats** — each party's total minus its fixed seats, placed valkrets by
    ///    valkrets where the party's next comparison number is highest.
    ///
    /// **What is DERIVED and what is billed.** The fixed seats per valkrets are derived from
    /// eligible voters by the statute's rule, and eligible voters per valkrets are themselves
    /// derived (2018 valid votes ÷ the national turnout) until val.se's per-valkrets
    /// "Röstberättigade" counts are on disk — so the per-valkrets seat table is `[DERIVED]
    /// [PROVISIONAL]` and the real 2022 per-valkrets seat table is billed for its verification.
    /// The party TOTALS do not depend on any of that unless a seat is returned, which is why the
    /// live path reproduces 2022 seat-for-seat from a derived regionalisation (asserted).
    /// </summary>
    public static class SeatConversion
    {
        public const int RiksdagSeats = 349;
        public const int FixedSeats = 310;
        public const double NationalThreshold = 0.04;
        public const double RegionalThreshold = 0.12;

        public sealed class Result
        {
            /// <summary>Seats per party, the totals.</summary>
            public int[] Seats;
            /// <summary>Seats per region per party — fixed and adjustment together.</summary>
            public int[][] RegionSeats;
            public int[] FixedSeatsPerRegion;
            public int[] FixedSeatsWon;
            public int[] AdjustmentSeats;
            public bool[] NationallyEligible;
            public int SeatsReturned;
        }

        /// <summary>The statute's distribution of the fixed seats among the valkretsar: one per 310th part of the national eligible electorate, the remainder by largest surplus.</summary>
        public static int[] FixedSeatsPerRegion(double[] eligiblePerRegion, int fixedSeats = FixedSeats)
        {
            double total = 0.0;
            foreach (double e in eligiblePerRegion) { total += e; }
            double part = total / fixedSeats;
            var seats = new int[eligiblePerRegion.Length];
            var surplus = new double[eligiblePerRegion.Length];
            int given = 0;
            for (int r = 0; r < seats.Length; r++)
            {
                seats[r] = (int)Math.Floor(eligiblePerRegion[r] / part);
                surplus[r] = eligiblePerRegion[r] - seats[r] * part;
                given += seats[r];
            }

            while (given < fixedSeats)
            {
                int best = 0;
                for (int r = 1; r < seats.Length; r++) { if (surplus[r] > surplus[best]) { best = r; } }
                seats[best]++;
                surplus[best] = -1.0;
                given++;
            }

            return seats;
        }

        /// <summary>Step 1: nationally eligible (4 %), and per region whether a party may take that region's fixed seats (4 % nationally or 12 % there).</summary>
        public static void Eligibility(long[][] regionVotes, out bool[] national, out bool[][] regional)
        {
            int regions = regionVotes.Length;
            int parties = regionVotes[0].Length;
            var nationalVotes = new long[parties];
            long valid = 0;
            var regionValid = new long[regions];
            for (int r = 0; r < regions; r++)
            {
                for (int p = 0; p < parties; p++) { nationalVotes[p] += regionVotes[r][p]; regionValid[r] += regionVotes[r][p]; }
                valid += regionValid[r];
            }

            national = new bool[parties];
            for (int p = 0; p < parties; p++) { national[p] = valid > 0 && (double)nationalVotes[p] / valid >= NationalThreshold; }

            regional = new bool[regions][];
            for (int r = 0; r < regions; r++)
            {
                regional[r] = new bool[parties];
                for (int p = 0; p < parties; p++)
                {
                    regional[r][p] = national[p] || (regionValid[r] > 0 && (double)regionVotes[r][p] / regionValid[r] >= RegionalThreshold);
                }
            }
        }

        /// <summary>The whole procedure. <paramref name="regionVotes"/> is [region][party] whole votes.</summary>
        public static Result Sweden(long[][] regionVotes, double[] eligiblePerRegion)
        {
            int regions = regionVotes.Length;
            int parties = regionVotes[0].Length;
            Eligibility(regionVotes, out bool[] national, out bool[][] regional);
            int[] fixedPerRegion = FixedSeatsPerRegion(eligiblePerRegion);

            // Step 2: fixed seats within each valkrets, among the parties eligible there.
            var regionSeats = new int[regions][];
            for (int r = 0; r < regions; r++) { regionSeats[r] = AllocateInRegion(regionVotes[r], regional[r], fixedPerRegion[r], null); }

            // Step 3: the totalfördelning over the nationally-eligible parties, less what 12 %-only parties hold.
            var nationalVotes = new long[parties];
            var fixedWon = new int[parties];
            for (int r = 0; r < regions; r++) { for (int p = 0; p < parties; p++) { nationalVotes[p] += regionVotes[r][p]; fixedWon[p] += regionSeats[r][p]; } }

            int reserved = 0;
            var masked = new long[parties];
            for (int p = 0; p < parties; p++)
            {
                if (national[p]) { masked[p] = nationalVotes[p]; }
                else { reserved += fixedWon[p]; }
            }

            int[] totals = SeatAllocation.HighestAverages(masked, RiksdagSeats - reserved, SeatAllocation.ModifiedSainteLagueDivisor);
            for (int p = 0; p < parties; p++) { if (!national[p]) { totals[p] = fixedWon[p]; } }

            // Step 4: återföring - a party over its total gives back its weakest fixed seats; each
            // returned seat is re-allocated in its valkrets under every party's cap. Iterate until
            // no party exceeds its total.
            int returned = 0;
            for (int guard = 0; guard < 1000; guard++)
            {
                int over = -1;
                for (int p = 0; p < parties; p++) { if (fixedWon[p] > totals[p]) { over = p; break; } }
                if (over < 0) { break; }

                // The over party's seat with the lowest comparison number, across regions.
                int worstRegion = -1;
                double worstNumber = double.MaxValue;
                for (int r = 0; r < regions; r++)
                {
                    if (regionSeats[r][over] <= 0) { continue; }
                    double number = regionVotes[r][over] / SeatAllocation.ModifiedSainteLagueDivisor(regionSeats[r][over] - 1);
                    if (number < worstNumber) { worstNumber = number; worstRegion = r; }
                }

                if (worstRegion < 0) { break; }
                regionSeats[worstRegion][over]--;
                fixedWon[over]--;
                returned++;

                // Re-allocate that one seat in the valkrets to the next comparison number among
                // parties eligible there and still under their total.
                int taker = -1;
                double takerNumber = -1.0;
                for (int p = 0; p < parties; p++)
                {
                    if (p == over || !regional[worstRegion][p] || regionVotes[worstRegion][p] <= 0) { continue; }
                    if (fixedWon[p] >= totals[p]) { continue; }
                    double number = regionVotes[worstRegion][p] / SeatAllocation.ModifiedSainteLagueDivisor(regionSeats[worstRegion][p]);
                    if (number > takerNumber) { takerNumber = number; taker = p; }
                }

                if (taker >= 0) { regionSeats[worstRegion][taker]++; fixedWon[taker]++; }
            }

            // Step 5: adjustment seats - each party's shortfall placed where its next comparison number is highest.
            var adjustment = new int[parties];
            for (int p = 0; p < parties; p++)
            {
                int shortfall = totals[p] - fixedWon[p];
                for (int s = 0; s < shortfall; s++)
                {
                    int bestRegion = -1;
                    double bestNumber = -1.0;
                    for (int r = 0; r < regions; r++)
                    {
                        if (regionVotes[r][p] <= 0) { continue; }
                        double number = regionVotes[r][p] / SeatAllocation.ModifiedSainteLagueDivisor(regionSeats[r][p]);
                        if (number > bestNumber) { bestNumber = number; bestRegion = r; }
                    }

                    if (bestRegion < 0) { break; }
                    regionSeats[bestRegion][p]++;
                    adjustment[p]++;
                }
            }

            var seats = new int[parties];
            for (int r = 0; r < regions; r++) { for (int p = 0; p < parties; p++) { seats[p] += regionSeats[r][p]; } }

            return new Result
            {
                Seats = seats, RegionSeats = regionSeats, FixedSeatsPerRegion = fixedPerRegion, FixedSeatsWon = fixedWon,
                AdjustmentSeats = adjustment, NationallyEligible = national, SeatsReturned = returned,
            };
        }

        /// <summary>The live path: an election-day count in, seats out.</summary>
        public static Result Sweden(ElectionDay.Result count)
        {
            var votes = new long[count.Regions.Length][];
            var eligible = new double[count.Regions.Length];
            for (int r = 0; r < votes.Length; r++)
            {
                votes[r] = new long[count.Regions[r].Votes.Length];
                for (int p = 0; p < votes[r].Length; p++) { votes[r][p] = (long)Math.Round(count.Regions[r].Votes[p]); }
                eligible[r] = count.Regions[r].EligibleVoters;
            }

            return Sweden(votes, eligible);
        }

        private static int[] AllocateInRegion(long[] votes, bool[] eligible, int seats, int[] caps)
        {
            var masked = new long[votes.Length];
            for (int p = 0; p < votes.Length; p++) { masked[p] = eligible[p] ? votes[p] : 0; }
            return SeatAllocation.HighestAverages(masked, seats, SeatAllocation.ModifiedSainteLagueDivisor);
        }
    }
}
