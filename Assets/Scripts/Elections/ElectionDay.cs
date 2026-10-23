using System;
using System.Globalization;
using System.Text;

namespace PoliSim.Elections
{
    /// <summary>
    /// W-D1 / SPEC §27 — election day: every region counted independently, then aggregated, with
    /// controlled uncertainty from the generator the caller passes. PURE: nothing here touches a
    /// `World`. PS-6 US-31: the US count reads it through `UsGroundGame.Effects` where a
    /// campaign ran (R-US16 (b), I3). GATE-18 q2 (turnout and the declared noise on in play) with A-LOOP q24 5b: Sweden's,
    /// Germany's and Poland's counts read its second form, each region on its own base turnout and 5b's turnout CACE
    /// (`ParliamentaryCount`).
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
            double noiseSigmaPp = RegionalAggregation.RegionalNoiseSigmaPp, bool scaledNoise = false)
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
                // Elias's ruling of 2026-10-23: the count in play (the US count's, `UsGroundGame.Effects`) draws the scaled form; W-D1's harnesses keep §27's own
                double[] noisy = scaledNoise ? RegionalAggregation.ApplyScaledNoise(shares, random, noiseSigmaPp) : RegionalAggregation.ApplyNoise(shares, random, noiseSigmaPp);

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

        /// <summary>
        /// GATE-18 q2 with A-LOOP q24 5b: <b>the count in play</b> - every region counted on its own base turnout
        /// (<paramref name="baseTurnoutByRegion"/>, a rate: the region's own votes cast over its electorate), each party's supporters at 5b's turnout
        /// (`GotvTurnout.RegionVotes`: the base, plus the turnout CACE <paramref name="doorCacePpByRegion"/> on the share of them its doors reached), then
        /// the same noise on the region's shares as the count above. A party with no share in a region - one that does not stand there (the CSU outside
        /// Bayern) - gets no vote from the noise (DECLARED, as the US count's column with no candidate): its draw is made and set aside, so every other
        /// party's draw is the one it would have been. With no doors and a zero σ, every region's votes are its votes cast times its preference, and the
        /// nation is the vote-weighted sum the derivation started from - the count reproduces the shares it was handed, to the rounding of a vote.
        /// </summary>
        public static Result Count(string[] regionNames, double[][] regionPreference, RegionalMobilization gotv, double[] baseTurnoutByRegion,
            double[] doorCacePpByRegion, System.Random random, double noiseSigmaPp)
        {
            if (random == null) { throw new ArgumentNullException(nameof(random)); }
            if (regionNames.Length != gotv.RegionCount || regionPreference.Length != gotv.RegionCount || baseTurnoutByRegion.Length != gotv.RegionCount || doorCacePpByRegion.Length != gotv.RegionCount)
            {
                throw new ArgumentException("one name, one preference vector, one base turnout and one CACE per region");
            }

            int partyCount = gotv.PartyCount;
            var regions = new RegionalAggregation.RegionResult[gotv.RegionCount];
            var national = new double[partyCount];
            double cast = 0.0, eligible = 0.0;
            var digest = new StringBuilder();
            var doors = new double[partyCount];

            for (int r = 0; r < gotv.RegionCount; r++)
            {
                for (int p = 0; p < partyCount; p++) { doors[p] = gotv.DoorContacts(r, p); }
                double[] expected = GotvTurnout.RegionVotes(gotv.Eligible(r), regionPreference[r], baseTurnoutByRegion[r], doors, doorCacePpByRegion[r]);
                double regionCast = 0.0;
                foreach (double v in expected) { regionCast += v; }

                var shares = new double[partyCount];
                for (int p = 0; p < partyCount; p++) { shares[p] = regionCast > 0 ? expected[p] / regionCast : 0.0; }
                // THE NOISE'S FORM, ONE SEAM - Elias's ruling of 2026-10-23 (the second review's M2): each party's sigma scaled by the square root of
                // s(1-s), W-D1's 1.2 points read as the sigma at s = 0.5 (`RegionalAggregation.ApplyScaledNoise`), then the clamp and renormalisation
                double[] noisy = RegionalAggregation.ApplyScaledNoise(shares, random, noiseSigmaPp);
                double kept = 0.0;
                for (int p = 0; p < partyCount; p++)
                {
                    if (shares[p] <= 0.0) { noisy[p] = 0.0; }
                    kept += noisy[p];
                }

                var votes = new double[partyCount];
                for (int p = 0; p < partyCount; p++)
                {
                    votes[p] = kept > 0.0 ? Math.Round(noisy[p] / kept * regionCast) : 0.0;
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
