using System;
using System.Collections.Generic;

namespace PoliSim.Elections
{
    /// <summary>
    /// W-D2 / SPEC §28 — vote-to-seat conversion on the LIVE path: an `ElectionDay.Result` in,
    /// seats per party and per valkrets out, by Sweden's own rules (vallagen 14 kap.). PURE
    /// FUNCTIONS. `SeatAllocation` (the divisor arithmetic) is what this calls; this file is the Swedish
    /// PROCEDURE around it. **LIVE since GATE-18 piece three (q3, Elias's ruling of 2026-10-18:
    /// "wire Sweden's 12 % route and per-valkrets seats into the live count")**: the player's chamber
    /// count (`ParliamentaryCount`) hands its counted valkretsar to `NationalElection.Run`, which
    /// allocates them here on the fixed seats Valmyndigheten decided (`DecidedFixedSeats`); a call
    /// with no count (the backtests) still allocates the national totalfördelning, which gives the
    /// same party totals as this procedure whenever no list takes seats by the 12 % route alone.
    ///
    /// **The statute's bases** (regeringsformen 3 kap. 7 §, `ElectionsData/sweden/2026/raw/government/riksdagen_sfs-1974-152_regeringsformen.html`:
    /// 4 % "av rösterna i hela riket", "minst tolv procent av rösterna" in the valkrets; the procedure
    /// itself vallagen 14 kap., `ElectionsData/sweden/riksdagen_vallag_2005_837.html`): every share is read over ALL valid votes, so the overload that
    /// takes a national and a per-valkrets base reads the lists the vote table does not carry as
    /// the rest of each base (`Sweden(long[][], int[], long, long[])`).
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
    ///    excess back, its lowest-comparison-number seats first - never in a valkrets with fewer
    ///    than three fixed seats (14 kap. 4 a §) - and each returned seat is
    ///    re-allocated within its valkrets to the party with the next highest comparison number
    ///    that still has room under its own total (the 2018 reform's rule; before it, overhangs
    ///    were left standing and the Riksdag was not exactly proportional) - every returned seat
    ///    together, the largest comparison number first (14 kap. 4 b §). An excess that cannot be
    ///    returned stands, its party set aside with its seats (DECLARED, the statute silent).
    /// 5. **39 adjustment seats** — each party's total minus its fixed seats, placed valkrets by
    ///    valkrets where the party's next comparison number is highest, by the odd numbers 1, 3, 5, …
    ///    (14 kap. 5 §: in a valkrets where the party holds no seat the first comparison number is
    ///    the party's vote, not the vote over 1.2).
    ///
    /// **What is SOURCED and what is derived.** The fixed seats per valkrets of an election are
    /// Valmyndigheten's decision for it (`DecidedFixedSeats`, generated from its workbook); the
    /// statute's own rule over an electorate (`FixedSeatsPerRegion`) stays for a call that hands
    /// eligible voters instead, and for a valkrets the decision does not name.
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
            /// <summary>GATE-18 q3: the fixed seats per region per party, after the return (step 4) and before the adjustment seats.</summary>
            public int[][] RegionFixedSeats;
            public int[] FixedSeatsPerRegion;
            public int[] FixedSeatsWon;
            public int[] AdjustmentSeats;
            public bool[] NationallyEligible;
            public int SeatsReturned;
            /// <summary>The review's L-2: the parties set aside with seats they could not return (DECLARED reading, `Sweden(long[][], int[], long, long[])`).</summary>
            public bool[] SetAside;
            /// <summary>The review's L-1: the returned seats in the order 14 kap. 4 b § distributed them - the valkrets, the party and its comparison number.</summary>
            public List<(int Region, int Party, double Number)> Reallocated;
            /// <summary>Returned seats no party in their valkrets could take, left to the adjustment seats.</summary>
            public int SeatsUnfilledByReturn;
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

        /// <summary>Step 1: nationally eligible (4 %), and per region whether a party may take that region's fixed seats (4 % nationally or 12 % there) -
        /// each share over the votes the table carries.</summary>
        public static void Eligibility(long[][] regionVotes, out bool[] national, out bool[][] regional) => Eligibility(regionVotes, 0L, null, out national, out regional);

        /// <summary>Step 1 on the statute's bases: each national share over <paramref name="nationalBase"/> and each valkrets's over its
        /// <paramref name="regionBase"/> - all valid votes, the lists the table does not carry among them - or over the table's own sum where a base is
        /// smaller than it (zero, or null, reads the sum).</summary>
        public static void Eligibility(long[][] regionVotes, long nationalBase, long[] regionBase, out bool[] national, out bool[][] regional)
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
            valid = Math.Max(valid, nationalBase);
            for (int r = 0; r < regions && regionBase != null; r++) { regionValid[r] = Math.Max(regionValid[r], regionBase[r]); }

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

        /// <summary>The whole procedure, the fixed seats per valkrets derived from <paramref name="eligiblePerRegion"/> by the statute's rule and every
        /// share over the votes the table carries. <paramref name="regionVotes"/> is [region][party] whole votes.</summary>
        public static Result Sweden(long[][] regionVotes, double[] eligiblePerRegion) => Sweden(regionVotes, FixedSeatsPerRegion(eligiblePerRegion), 0L, null);

        /// <summary>GATE-18 q3: the whole procedure on the fixed seats <paramref name="fixedPerRegion"/> (an election's decided table,
        /// `DecidedFixedSeats`) and the statute's bases - <paramref name="nationalBase"/> the national valid vote, <paramref name="regionBase"/> each
        /// valkrets's (zero or null reads the table's own sums). <paramref name="regionVotes"/> is [region][party] whole votes.</summary>
        public static Result Sweden(long[][] regionVotes, int[] fixedPerRegion, long nationalBase, long[] regionBase)
        {
            int regions = regionVotes.Length;
            int parties = regionVotes[0].Length;
            if (fixedPerRegion == null || fixedPerRegion.Length != regions) { throw new ArgumentException("one fixed-seat count per region"); }
            Eligibility(regionVotes, nationalBase, regionBase, out bool[] national, out bool[][] regional);

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

            // Step 4: återföring, in the statute's two steps (the review's L-1, L-2; vallagen 14 kap., `ElectionsData/sweden/riksdagen_vallag_2005_837.html`).
            // 4 a §: "Om ett parti vid fördelningen av de fasta valkretsmandaten har fått fler mandat än vad som motsvarar en proportionell representation
            // i hela landet, ska överskjutande mandat återföras" - from the seats with the lowest comparison number, never in a valkrets with fewer than
            // three fixed seats ("Återföring får dock inte göras i en valkrets som har färre än tre fasta valkretsmandat").
            // DECLARED (L-2, the statute silent): an excess that cannot be returned stands, and the party is set aside with its seats as 5 § sets aside a
            // party its fixed seats represent - the totalfördelning is taken again among the rest on what remains, so the chamber stays at RiksdagSeats.
            int returned = 0;
            var setAside = new bool[parties];
            var pending = new List<int>();   // the valkrets of every returned seat, awaiting 4 b's distribution
            for (int guard = 0; guard < 4 * parties + 4; guard++)
            {
                bool newlySetAside = false;
                for (int p = 0; p < parties; p++)
                {
                    while (fixedWon[p] > totals[p])
                    {
                        int worstRegion = -1;
                        double worstNumber = double.MaxValue;
                        for (int r = 0; r < regions; r++)
                        {
                            if (regionSeats[r][p] <= 0 || fixedPerRegion[r] < 3) { continue; }
                            double number = regionVotes[r][p] / SeatAllocation.ModifiedSainteLagueDivisor(regionSeats[r][p] - 1);
                            if (number < worstNumber) { worstNumber = number; worstRegion = r; }
                        }
                        if (worstRegion < 0) { break; }
                        regionSeats[worstRegion][p]--;
                        fixedWon[p]--;
                        returned++;
                        pending.Add(worstRegion);
                    }
                    if (fixedWon[p] > totals[p] && !setAside[p]) { setAside[p] = true; newlySetAside = true; }
                }
                if (!newlySetAside) { break; }
                int pool = RiksdagSeats - reserved;
                var remaining = new long[parties];
                for (int p = 0; p < parties; p++) { if (setAside[p]) { pool -= fixedWon[p]; } else { remaining[p] = masked[p]; } }
                totals = SeatAllocation.HighestAverages(remaining, Math.Max(0, pool), SeatAllocation.ModifiedSainteLagueDivisor);
                for (int p = 0; p < parties; p++) { if (!national[p] || setAside[p]) { totals[p] = fixedWon[p]; } }
            }

            // 4 b §: "Om mandat har återförts i två eller flera valkretsar ska dessa mandat fördelas i den ordningen att det parti som har störst
            // jämförelsetal tilldelas första mandatet och så vidare, allt efter storleken på jämförelsetalen för de partier som ska tilldelas mandat" -
            // each returned seat by 3 § in its own valkrets, among "de övriga partier i valkretsen som inte har uppnått proportionell representation"
            // (4 a §), the largest comparison number over every returned seat taken first. A returned seat no party there can take is left to the
            // adjustment seats, which keep the chamber at its total.
            var reallocated = new List<(int Region, int Party, double Number)>();
            while (pending.Count > 0)
            {
                int bestSeat = -1, bestParty = -1;
                double bestNumber = -1.0;
                for (int i = 0; i < pending.Count; i++)
                {
                    int r = pending[i];
                    for (int p = 0; p < parties; p++)
                    {
                        if (!regional[r][p] || regionVotes[r][p] <= 0 || fixedWon[p] >= totals[p]) { continue; }
                        double number = regionVotes[r][p] / SeatAllocation.ModifiedSainteLagueDivisor(regionSeats[r][p]);
                        if (number > bestNumber) { bestNumber = number; bestSeat = i; bestParty = p; }
                    }
                }
                if (bestSeat < 0) { break; }
                int region = pending[bestSeat];
                pending.RemoveAt(bestSeat);
                regionSeats[region][bestParty]++;
                fixedWon[bestParty]++;
                reallocated.Add((region, bestParty, bestNumber));
            }

            // Step 5: adjustment seats - each party's shortfall placed where its next comparison number is highest, on the odd numbers 1, 3, 5, ...
            // (14 kap. 5 §: "I en valkrets där partiet inte har fått något fast valkretsmandat ska dock jämförelsetalet vid tilldelningen av det
            // första mandatet vara lika med partiets röstetal" - the first divisor is one, not 1.2)
            var regionFixed = new int[regions][];
            for (int r = 0; r < regions; r++) { regionFixed[r] = (int[])regionSeats[r].Clone(); }
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
                        double number = regionVotes[r][p] / SeatAllocation.SainteLagueDivisor(regionSeats[r][p]);
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
                AdjustmentSeats = adjustment, NationallyEligible = national, SeatsReturned = returned, RegionFixedSeats = regionFixed, SetAside = setAside, Reallocated = reallocated, SeatsUnfilledByReturn = pending.Count,
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

        /// <summary>
        /// GATE-18 q3: the fixed seats of the valkretsar <paramref name="regionNames"/> (matched by name) as Valmyndigheten decided them for the
        /// election of <paramref name="pollingDay"/> (`SwedishFixedSeats`, generated from its workbook): vallagen 4 kap. 3 § has them decided by
        /// 30 April of the year of an ordinary election, so a polling day reads the decision of its own year - 2022's from 2022, 2026's from 2026,
        /// and 2026's carried forward to every later election until a newer decision is sourced (DECLARED: the latest dated decision stands, as a
        /// declaration does). Null where a name is not in the decision, or the day falls before 2022's.
        /// </summary>
        public static int[] DecidedFixedSeats(DateTime pollingDay, IReadOnlyList<string> regionNames)
        {
            if (regionNames == null || pollingDay.Year < 2022) { return null; }
            int[] table = pollingDay.Year >= 2026 ? Generated.SwedishFixedSeats.Fixed2026 : Generated.SwedishFixedSeats.Fixed2022;
            var seats = new int[regionNames.Count];
            for (int r = 0; r < seats.Length; r++)
            {
                int at = Array.IndexOf(Generated.SwedishFixedSeats.Names, regionNames[r]);
                if (at < 0) { return null; }
                seats[r] = table[at];
            }
            return seats;
        }

        private static int[] AllocateInRegion(long[] votes, bool[] eligible, int seats, int[] caps)
        {
            var masked = new long[votes.Length];
            for (int p = 0; p < votes.Length; p++) { masked[p] = eligible[p] ? votes[p] : 0; }
            return SeatAllocation.HighestAverages(masked, seats, SeatAllocation.ModifiedSainteLagueDivisor);
        }
    }
}
