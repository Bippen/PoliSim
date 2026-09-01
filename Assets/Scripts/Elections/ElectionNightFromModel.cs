using System;
using System.Collections.Generic;
using PoliSim.Data;

namespace PoliSim.Elections
{
    /// <summary>
    /// **F1 step 4's model half: the live election's own regional result, as an election night.**
    ///
    /// <para>`ElectionNight.At` has always been able to draw a night — it was built, filmed at four widths
    /// and delivered — but **nothing in the game could give it a result**, because the per-constituency
    /// numbers did not exist at runtime. They do now, and this is the one place they become the counts a
    /// screen reads.</para>
    ///
    /// <para>⚠ <b>The votes are the MODEL'S, converted once.</b> A region's count is its predicted share
    /// times its own valid-vote weight, rounded — so the night's arithmetic and the headline come from a
    /// single computation. **Two independent paths to the same number is how a screen and a result start
    /// disagreeing**, and it is the specific failure F1 names.</para>
    ///
    /// <para>⚠ <b>What ROUNDING costs, stated.</b> Shares are real numbers and votes are whole, so the
    /// per-region counts sum to within a handful of votes of the weight they came from. That is a rounding
    /// residue of at most one vote per party per region, and it is left visible rather than smoothed away
    /// into a party's total, because a smoothed total is a number nobody can re-derive.</para>
    ///
    /// <para>⚠ <b>The declaration ORDER is a presentation choice and is not a prediction.</b> Real
    /// constituencies declare when their count finishes, which depends on staffing, geography and postal
    /// volume — none of which this model has. **Smaller electorates finish sooner** is the one honest
    /// regularity available, so arrival is ordered by electorate size and spread across the night. It says
    /// nothing about who wins and it is not offered as a forecast of timing.</para>
    /// </summary>
    public static class ElectionNightFromModel
    {
        /// <summary>The last minute of the night — every constituency has declared by here.</summary>
        public const int FinalMinute = 300;

        /// <summary>Whether the player's country can produce a night at all. ⚠ It is not "is there a
        /// screen" but "is there a per-constituency RESULT", which is the thing that was missing.</summary>
        public static bool Available(CountryId country) =>
            country == CountryId.Sweden && NationalElection.LastRegionalShares != null;

        /// <summary>
        /// Build the night from the prediction `NationalElection.TryPredictShares` most recently made.
        /// Returns null when the country has no regional result, which a caller must treat as
        /// "no night to show" rather than as an empty one.
        /// </summary>
        public static NightState At(int minute, CountryId country, IReadOnlyList<string> partyKeys,
            int totalSeats, double threshold)
        {
            if (!Available(country)) { return null; }

            double[][] shares = NationalElection.LastRegionalShares;
            string[] names = NationalElection.LastRegionalNames;
            double[] weights = NationalElection.LastRegionalWeights;
            if (shares == null || names == null || weights == null) { return null; }
            if (partyKeys == null || partyKeys.Count == 0) { return null; }

            int regions = names.Length;
            var votes = new long[regions][];
            var valid = new long[regions];
            var eligible = new long[regions];

            for (int r = 0; r < regions; r++)
            {
                votes[r] = new long[partyKeys.Count];
                long cast = 0L;
                for (int p = 0; p < partyKeys.Count && p < shares[r].Length; p++)
                {
                    long v = (long)Math.Round(shares[r][p] * weights[r], MidpointRounding.AwayFromZero);
                    votes[r][p] = v;
                    cast += v;
                }

                // ⚠ VALID is what was actually cast for the parties counted, not the weight it came from.
                // Using the weight would make the shares reconcile by construction and hide the rounding
                // residue - a number that agrees because it was told to is not evidence.
                valid[r] = cast;
                eligible[r] = SwedishRegions.EligibleAt(r);
            }

            int[] arrivals = ArrivalsBySize(eligible);

            var partyNames = new string[partyKeys.Count];
            for (int p = 0; p < partyKeys.Count; p++) { partyNames[p] = partyKeys[p]; }

            return ElectionNight.At(minute, names, votes, valid, eligible, arrivals, totalSeats, threshold, partyNames);
        }

        /// <summary>
        /// Declaration minutes, smallest electorate first, spread evenly across the night.
        /// ⚠ **A presentation choice, not a forecast.** See the note on the class.
        /// </summary>
        private static int[] ArrivalsBySize(long[] eligible)
        {
            int n = eligible.Length;
            var order = new int[n];
            for (int i = 0; i < n; i++) { order[i] = i; }

            Array.Sort(order, (a, b) => eligible[a].CompareTo(eligible[b]));

            var arrivals = new int[n];
            for (int rank = 0; rank < n; rank++)
            {
                // First declaration lands early in the night, the last exactly on FinalMinute, so a screen
                // asking for FinalMinute always has a complete count rather than one short.
                arrivals[order[rank]] = n == 1 ? FinalMinute : (int)Math.Round(FinalMinute * (rank + 1.0) / n);
            }

            return arrivals;
        }
    }
}
