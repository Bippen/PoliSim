using System;
using System.Globalization;
using System.Text;

namespace PoliSim.Elections
{
    /// <summary>
    /// W-D1 / SPEC §27 — election day: every region counted independently, then aggregated, with
    /// controlled uncertainty on `SimulationRandom`'s `ElectionNoise` stream. PURE, WIRED TO
    /// NOTHING (R-N2): the harness is the only caller; nothing here touches a `World`.
    ///
    /// §27, per region: population × eligible × turnout × preference, aggregated over groups —
    /// here `RegionalMobilization.RegionVotes` (W-B11: eligible × preference × each party's
    /// supporters' turnout, which is where the ground game lands) — then `Final Vote = Expected
    /// Vote + Election Noise` as `RegionalAggregation.ApplyNoise` on the region's SHARES (σ 1.2 pp
    /// per party per region, declared there on Day-1), re-normalised and turned back into votes
    /// against the region's votes cast. National shares are the vote-weighted sum of regions —
    /// never a mean of regional shares — so regional noise cancels nationally at the 1/√N_eff
    /// rate Day-1 measured (0.95 pp regional → 0.35 pp national on eight equal regions), which
    /// the harness asserts over 400 replays on the real valkretsar.
    ///
    /// **Determinism.** Every draw comes from the `System.Random` the caller passes — the harness
    /// passes `SimulationRandom.For(Stream.ElectionNoise)` after `Seed(n)` — so the same seed
    /// reproduces the count to the vote, and one election can be re-run without re-running the
    /// economy (the stream's own reason for existing).
    ///
    /// **What is NOT here.** Seats (W-D2 — `SeatAllocation` is exact and waits for this result),
    /// voter GROUPS (the electorate is one group per region until W-F4; the region's preference
    /// vector is what the caller hands it), tactical voting (§23, W-A4), events on the day.
    /// </summary>
    public static class ElectionDay
    {
        /// <summary>One region's count and the whole nation's.</summary>
        public sealed class Result
        {
            public RegionalAggregation.RegionResult[] Regions;
            /// <summary>Votes per party, summed over regions (after noise).</summary>
            public double[] NationalVotes;
            public double[] NationalShares;
            public double NationalTurnout;
            public double VotesCast;
            public double Eligible;
            /// <summary>A deterministic digest of every regional vote count — two runs of one seed must print the same one.</summary>
            public string Digest;
        }

        /// <summary>
        /// Count the election. <paramref name="regionPreference"/> is [region][party] (one vector
        /// per region — the same national vector repeated until W-A2's per-region priors and
        /// W-F4's groups make them differ); <paramref name="gotv"/> holds every party's ground
        /// operation per region (W-B11); the four turnout attributes are §26's shared context.
        /// </summary>
        public static Result Count(string[] regionNames, double[][] regionPreference, RegionalMobilization gotv,
            double baseTurnout, double engagement, double enthusiasm, double salience, System.Random random,
            double noiseSigmaPp = RegionalAggregation.RegionalNoiseSigmaPp)
        {
            if (random == null) { throw new ArgumentNullException(nameof(random)); }
            if (regionNames.Length != gotv.RegionCount || regionPreference.Length != gotv.RegionCount)
            {
                throw new ArgumentException("one name and one preference vector per region");
            }

            int partyCount = gotv.PartyCount;
            var regions = new RegionalAggregation.RegionResult[gotv.RegionCount];
            var national = new double[partyCount];
            double cast = 0.0, eligible = 0.0;
            var digest = new StringBuilder();

            for (int r = 0; r < gotv.RegionCount; r++)
            {
                // Expected votes: eligible × preference × each party's supporters' turnout (W-B11).
                double[] expected = gotv.RegionVotes(r, regionPreference[r], baseTurnout, engagement, enthusiasm, salience);
                double regionCast = 0.0;
                foreach (double v in expected) { regionCast += v; }

                // Controlled uncertainty on the SHARES, then back to votes against the votes cast.
                var shares = new double[partyCount];
                for (int p = 0; p < partyCount; p++) { shares[p] = regionCast > 0 ? expected[p] / regionCast : 0.0; }
                double[] noisy = RegionalAggregation.ApplyNoise(shares, random, noiseSigmaPp);

                var votes = new double[partyCount];
                for (int p = 0; p < partyCount; p++)
                {
                    votes[p] = Math.Round(noisy[p] * regionCast);
                    national[p] += votes[p];
                    digest.Append(votes[p].ToString("F0", CultureInfo.InvariantCulture)).Append(',');
                }

                regions[r] = new RegionalAggregation.RegionResult(regionNames[r], votes, gotv.Eligible(r), regionCast);
                cast += regionCast;
                eligible += gotv.Eligible(r);
                digest.Append(';');
            }

            return new Result
            {
                Regions = regions,
                NationalVotes = national,
                NationalShares = RegionalAggregation.NationalShares(regions, partyCount),
                NationalTurnout = eligible > 0 ? cast / eligible : 0.0,
                VotesCast = cast,
                Eligible = eligible,
                Digest = Fnv1a64(digest.ToString()),
            };
        }

        /// <summary>The effective number of equally-weighted regions behind a vote-weighted national figure: 1 / Σ w² — what regional noise divides by (√) on the way to the nation.</summary>
        public static double EffectiveRegions(double[] weights)
        {
            double sum = 0.0, sumSq = 0.0;
            foreach (double w in weights) { sum += w; }
            if (sum <= 0.0) { return 0.0; }
            foreach (double w in weights) { double f = w / sum; sumSq += f * f; }
            return sumSq > 0 ? 1.0 / sumSq : 0.0;
        }

        private static string Fnv1a64(string text)
        {
            const ulong offset = 14695981039346656037UL;
            const ulong prime = 1099511628211UL;
            ulong hash = offset;
            foreach (char c in text) { hash ^= c; hash *= prime; }
            return hash.ToString("x16", CultureInfo.InvariantCulture);
        }
    }
}
