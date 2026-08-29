using System;

namespace PoliSim.Elections
{
    /// <summary>
    /// SPEC §24/§27's structural half — the national vote as a WEIGHTED SUM OF REGIONS, each with
    /// its own electorate size and its own set of parties actually standing. PURE FUNCTIONS,
    /// WIRED TO NOTHING (R-N2).
    ///
    /// **Why this exists, precisely.** Day-1's national spatial model over-predicted the CSU by
    /// +7.4 pp, and the report named regional structure as the cause. The mechanism is not
    /// demographic subtlety — it is candidacy: **the CSU contests exactly one Land**, and the CDU
    /// contests the other fifteen. A national model lets both compete for all 49.6 million valid
    /// votes, which is simply not what happened. The sourced per-Land file
    /// (`ElectionsData/germany/land_votes_2025.csv`, absolute Zweitstimmen from the official
    /// `kerg2.csv`) states the fact in its own zeros: CDU = 0 in Bayern, CSU = 0 in all fifteen
    /// others.
    ///
    /// **What this layer adds, and what it deliberately does NOT.** It adds two things, both
    /// sourced and neither fitted: per-region **electorate weights** (each region's real valid-vote
    /// count) and per-region **party availability**. It does NOT vary the electorate's ideological
    /// position by region — that would be a per-region parameter set, and fitting one against
    /// regional results is circular in a backtest. The struct carries an optional override so a
    /// later pass CAN vary it when a non-circular source exists (regional demographics from the
    /// model's own seeds), but the Day-2 measurement runs with the national electorate everywhere,
    /// so any improvement it shows is structure and not tuning.
    ///
    /// Aggregation is vote-weighted, never a mean of regional shares — the trap that lets a small
    /// region outvote a large one (asserted in the chain harness).
    /// </summary>
    public static class RegionalVoteModel
    {
        /// <summary>One region as this layer sees it: how many votes it casts, and which parties are on its ballot.</summary>
        public readonly struct RegionInput
        {
            public readonly string Name;
            public readonly double ElectorateWeight;
            public readonly bool[] PartyAvailable;
            public readonly VoteModel.Electorate? ElectorateOverride;

            public RegionInput(string name, double electorateWeight, bool[] partyAvailable,
                VoteModel.Electorate? electorateOverride = null)
            {
                Name = name;
                ElectorateWeight = electorateWeight;
                PartyAvailable = partyAvailable;
                ElectorateOverride = electorateOverride;
            }
        }

        /// <summary>
        /// National vote shares as the weighted sum of regional votes. Within a region only the
        /// available parties compete, so a party standing in one region of ten cannot draw on the
        /// other nine's electorate — the correction the CSU deviation asked for.
        /// </summary>
        public static double[] NationalShares(VoteModel.PartyPoint[] parties, RegionInput[] regions,
            VoteModel.Electorate electorate, double wEcon)
        {
            if (parties == null || parties.Length == 0) { throw new ArgumentException("no parties"); }
            if (regions == null || regions.Length == 0) { throw new ArgumentException("no regions"); }

            var votes = new double[parties.Length];
            double totalVotes = 0.0;

            foreach (RegionInput region in regions)
            {
                if (region.PartyAvailable != null && region.PartyAvailable.Length != parties.Length)
                {
                    throw new ArgumentException($"{region.Name}: availability must be one flag per party");
                }

                // The sub-field actually on this region's ballot.
                int availableCount = 0;
                for (int p = 0; p < parties.Length; p++)
                {
                    if (region.PartyAvailable == null || region.PartyAvailable[p]) { availableCount++; }
                }

                if (availableCount == 0) { continue; }

                var subset = new VoteModel.PartyPoint[availableCount];
                var indexMap = new int[availableCount];
                int cursor = 0;
                for (int p = 0; p < parties.Length; p++)
                {
                    if (region.PartyAvailable != null && !region.PartyAvailable[p]) { continue; }

                    subset[cursor] = parties[p];
                    indexMap[cursor] = p;
                    cursor++;
                }

                VoteModel.Electorate regionElectorate = region.ElectorateOverride ?? electorate;
                double[] shares = VoteModel.PredictShares(subset, regionElectorate, wEcon);

                for (int i = 0; i < shares.Length; i++)
                {
                    double regionVotes = shares[i] * region.ElectorateWeight;
                    votes[indexMap[i]] += regionVotes;
                    totalVotes += regionVotes;
                }
            }

            if (totalVotes <= 0.0) { return votes; }

            for (int p = 0; p < votes.Length; p++) { votes[p] /= totalVotes; }
            return votes;
        }

        /// <summary>
        /// The same, with §8 loyalty applied WITHIN each region before aggregation: a region's
        /// voters are damped toward the prior vote of that region, not of the nation. Prior shares
        /// are given per region over the full party list (zeros for parties not standing there).
        /// </summary>
        public static double[] NationalSharesWithLoyalty(VoteModel.PartyPoint[] parties, RegionInput[] regions,
            VoteModel.Electorate electorate, double wEcon, double[][] regionPriorShares, double loyalty)
        {
            if (regionPriorShares == null || regionPriorShares.Length != regions.Length)
            {
                throw new ArgumentException("one prior-share vector per region");
            }

            var votes = new double[parties.Length];
            double totalVotes = 0.0;

            for (int r = 0; r < regions.Length; r++)
            {
                RegionInput region = regions[r];
                int availableCount = 0;
                for (int p = 0; p < parties.Length; p++)
                {
                    if (region.PartyAvailable == null || region.PartyAvailable[p]) { availableCount++; }
                }

                if (availableCount == 0) { continue; }

                var subset = new VoteModel.PartyPoint[availableCount];
                var indexMap = new int[availableCount];
                int cursor = 0;
                for (int p = 0; p < parties.Length; p++)
                {
                    if (region.PartyAvailable != null && !region.PartyAvailable[p]) { continue; }

                    subset[cursor] = parties[p];
                    indexMap[cursor] = p;
                    cursor++;
                }

                VoteModel.Electorate regionElectorate = region.ElectorateOverride ?? electorate;
                double[] spatial = VoteModel.PredictShares(subset, regionElectorate, wEcon);

                // §8 needs compatibility-like scores; the spatial shares ARE the persuaded
                // distribution, so damping is applied directly between prior and persuaded rather
                // than re-deriving through PreferenceModel's exponentiation (which would apply the
                // sharpness twice).
                var priorSubset = new double[availableCount];
                double priorSum = 0.0;
                for (int i = 0; i < availableCount; i++)
                {
                    priorSubset[i] = Math.Max(0.0, regionPriorShares[r][indexMap[i]]);
                    priorSum += priorSubset[i];
                }

                double lambda = ElectionScales.Clamp(loyalty) / ElectionScales.Max;
                if (priorSum <= 0.0) { lambda = 0.0; priorSum = 1.0; }

                var damped = new double[availableCount];
                double dampedSum = 0.0;
                for (int i = 0; i < availableCount; i++)
                {
                    damped[i] = lambda * (priorSubset[i] / priorSum) + (1.0 - lambda) * spatial[i];
                    dampedSum += damped[i];
                }

                for (int i = 0; i < availableCount; i++)
                {
                    double regionVotes = (damped[i] / dampedSum) * region.ElectorateWeight;
                    votes[indexMap[i]] += regionVotes;
                    totalVotes += regionVotes;
                }
            }

            if (totalVotes <= 0.0) { return votes; }

            for (int p = 0; p < votes.Length; p++) { votes[p] /= totalVotes; }
            return votes;
        }
    }
}
