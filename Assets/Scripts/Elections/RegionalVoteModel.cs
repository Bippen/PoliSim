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
    ///
    /// ⚠ **THE SCOPE OF THIS LAYER IS DELIBERATE — DO NOT "IMPROVE" IT INTO GENERALITY** (ruled
    /// 2026-08-29). Measured: §27's value is **concentrated in regionally-confined parties** — the
    /// CSU standing in one Land of sixteen, the SSW in one, the Greens' rejected Saarland list.
    /// With such a party in the field this layer is worth several points of MAD; with the field
    /// restricted to nationally-uniform parties it is worth almost nothing, and Day-2/W-A3 measured
    /// exactly that (Germany's nine-party set improves, the eight-party set barely moves).
    ///
    /// **That is the layer working, not a shortfall.** A regional layer that also moved
    /// nationally-uniform parties would be adding noise dressed as signal: if a party stands
    /// everywhere and the model has no non-circular source of regional preference variation, then
    /// the honest regional prediction IS the national one. A later session that "generalises" this
    /// by inventing per-region electorate shifts would be manufacturing precision it cannot source.
    /// The `ElectorateOverride` hook exists for the day a NON-CIRCULAR source of regional variation
    /// arrives (regional demographics from the model's own seeds) — and for no other day.
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

        /// <summary>
        /// W-A2's form — §27 and §8 composing properly: each region is damped toward **its own**
        /// prior with **per-party** loyalty (from <see cref="LoyaltyModel"/>), rather than toward a
        /// national prior with one constant.
        ///
        /// Day-2 measured why this matters: the both-layers run came out WORSE than §8 alone
        /// (Germany 5.01 vs 4.55) because every region was damped toward the national prior, which
        /// is the wrong prior for any particular region — Bavaria is not Germany-in-miniature. With
        /// each region carrying its own history the two layers stop fighting.
        ///
        /// <paramref name="regionPriorShares"/> is [region][party], each row that region's own
        /// previous-election shares (zeros for parties that did not stand there — a real fact, e.g.
        /// the Greens' rejected Saarland list in 2021, not missing data).
        /// </summary>
        /// <summary>
        /// **F1 step 3: each region''s shares, given the national result the model already predicted.**
        /// A UNIFORM ADDITIVE SWING applied to each region''s own prior.
        ///
        /// <para>⚠ <b>Why additive and not proportional.</b> Additive swing is the one form whose
        /// vote-weighted regional sum **reproduces the national shares exactly** — every region moves by
        /// the same number of points, so the weighted total moves by that number and nothing else. **A
        /// screen showing constituencies that do not add up to the headline is the exact failure F1 forbids**,
        /// and proportional swing does not have that property.</para>
        ///
        /// <para>⚠ <b>§689 (ruled 2026-09-30): regional breakdowns sum to their national figures.</b> A party's swing is spread only over
        /// the regions where it stands, scaled by the total weight over the weight where it stands, so a party standing in a subset (the CSU, the
        /// SSW) reproduces its national share instead of moving by its regions' weight share of the swing. The floor at zero (a party polling
        /// below the swing where it is weak) and each region's renormalisation to one still move a total; each party's one swing figure is
        /// corrected by what its regions still miss until the totals agree - the uniform form re-solved, never a per-region fit. **The caller is
        /// still given the residual** (a rounding residue, or a party with a share and no region to stand in); it is reported, not absorbed, and
        /// `RegionalSumCheck` asserts it for every party.</para>
        ///
        /// <para><b>What this does NOT claim.</b> Uniform swing says every region moves alike. Real regions
        /// do not — a party can surge in cities and fall in the countryside within one election. This layer
        /// has no non-circular source for differential swing, and inventing one by fitting against regional
        /// results is circular by construction. **Uniform is the honest floor, and the prior is what makes
        /// the regions differ at all.**</para>
        /// </summary>
        public static double[][] RegionalSharesByUniformSwing(double[] nationalShares,
            RegionInput[] regions, double[][] regionPriorShares, out double worstAbsError)
        {
            if (nationalShares == null || nationalShares.Length == 0) { throw new ArgumentException("no national shares"); }
            if (regions == null || regions.Length == 0) { throw new ArgumentException("no regions"); }
            if (regionPriorShares == null || regionPriorShares.Length != regions.Length)
            {
                throw new ArgumentException("one prior-share vector per region");
            }

            int n = nationalShares.Length;
            double totalWeight = 0.0;
            foreach (RegionInput r in regions) { totalWeight += r.ElectorateWeight; }
            if (totalWeight <= 0.0) { throw new ArgumentException("regions carry no weight"); }

            // The prior''s OWN national position, vote-weighted the same way the result will be. The swing
            // is measured against this and not against a remembered figure, so the two are commensurable.
            var priorNational = new double[n];
            for (int r = 0; r < regions.Length; r++)
            {
                for (int p = 0; p < n; p++)
                {
                    priorNational[p] += regionPriorShares[r][p] * regions[r].ElectorateWeight;
                }
            }

            for (int p = 0; p < n; p++) { priorNational[p] /= totalWeight; }

            // §689 (ruled 2026-09-30): THE SWING IS SPREAD ONLY OVER THE REGIONS WHERE A PARTY STANDS, scaled so its regions sum to its national
            // share. A party standing in a subset (the CSU in Bayern alone, the SSW in Schleswig-Holstein) used to take the national swing only in
            // its regions, so its national total moved by its regions' weight share of the swing, not the swing (the CSU 6.83 % national, 5.87 %
            // rebuilt, §688). Scaled by the total weight over the weight where it stands, the swing is still ONE figure per party, applied alike in
            // every region it stands in - the uniform additive form kept; only the normalisation is fixed.
            var standingWeight = new double[n];
            for (int r = 0; r < regions.Length; r++)
            {
                for (int p = 0; p < n; p++)
                {
                    if (regions[r].PartyAvailable == null || regions[r].PartyAvailable[p]) { standingWeight[p] += regions[r].ElectorateWeight; }
                }
            }

            var swing = new double[n];
            for (int p = 0; p < n; p++) { swing[p] = standingWeight[p] > 0.0 ? (nationalShares[p] - priorNational[p]) * totalWeight / standingWeight[p] : 0.0; }

            // The two steps that still move a total - a region's shares renormalised to one (the prior's rows leave out the parties the returns do not
            // itemise, and scaled swings need not cancel inside one region) and the floor at zero - are closed by correcting each party's one swing
            // figure by what its regions still miss, scaled the same way, until the totals agree: the same form, re-solved, never a per-region fit.
            // What cannot be closed (a party with a national share and no region to stand in) is REPORTED below, never absorbed.
            double[][] result = null;
            var rebuilt = new double[n];
            const int MaxCorrections = 200;
            const double Closed = 1e-12;
            for (int pass = 0; pass <= MaxCorrections; pass++)
            {
                result = ApplySwing(regions, regionPriorShares, swing, n);
                Array.Clear(rebuilt, 0, n);
                for (int r = 0; r < regions.Length; r++)
                {
                    for (int p = 0; p < n; p++) { rebuilt[p] += result[r][p] * regions[r].ElectorateWeight; }
                }

                double worst = 0.0;
                for (int p = 0; p < n; p++)
                {
                    rebuilt[p] /= totalWeight;
                    if (standingWeight[p] > 0.0) { worst = Math.Max(worst, Math.Abs(rebuilt[p] - nationalShares[p])); }
                }
                if (worst < Closed) { break; }
                for (int p = 0; p < n; p++)
                {
                    if (standingWeight[p] > 0.0) { swing[p] += (nationalShares[p] - rebuilt[p]) * totalWeight / standingWeight[p]; }
                }
            }

            // ⚠ The reproduction error, MEASURED and handed back - a rounding residue unless a party has a share and nowhere to stand.
            worstAbsError = 0.0;
            for (int p = 0; p < n; p++)
            {
                double e = Math.Abs(rebuilt[p] - nationalShares[p]);
                if (e > worstAbsError) { worstAbsError = e; }
            }

            return result;
        }

        /// <summary>One party's swing figure added to its prior in every region it stands in (nowhere else), floored at zero, each region renormalised.</summary>
        private static double[][] ApplySwing(RegionInput[] regions, double[][] regionPriorShares, double[] swing, int n)
        {
            var result = new double[regions.Length][];
            for (int r = 0; r < regions.Length; r++)
            {
                result[r] = new double[n];
                double sum = 0.0;
                for (int p = 0; p < n; p++)
                {
                    bool stands = regions[r].PartyAvailable == null || regions[r].PartyAvailable[p];
                    double v = stands ? regionPriorShares[r][p] + swing[p] : 0.0;
                    if (v < 0.0) { v = 0.0; }
                    result[r][p] = v;
                    sum += v;
                }

                if (sum > 0.0)
                {
                    for (int p = 0; p < n; p++) { result[r][p] /= sum; }
                }
            }
            return result;
        }
        public static double[] NationalSharesWithRegionalLoyalty(VoteModel.PartyPoint[] parties,
            RegionInput[] regions, VoteModel.Electorate electorate, double wEcon,
            double[][] regionPriorShares, double[] loyaltyPerParty)
        {
            if (regionPriorShares == null || regionPriorShares.Length != regions.Length)
            {
                throw new ArgumentException("one prior-share vector per region");
            }

            if (loyaltyPerParty == null || loyaltyPerParty.Length != parties.Length)
            {
                throw new ArgumentException("loyalty must be one per party");
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

                // This region's own prior, restricted to the parties standing here.
                var priorSubset = new double[availableCount];
                double priorSum = 0.0;
                for (int i = 0; i < availableCount; i++)
                {
                    priorSubset[i] = Math.Max(0.0, regionPriorShares[r][indexMap[i]]);
                    priorSum += priorSubset[i];
                }

                var damped = new double[availableCount];
                double dampedSum = 0.0;
                for (int i = 0; i < availableCount; i++)
                {
                    double lambda = priorSum > 0.0
                        ? ElectionScales.Clamp(loyaltyPerParty[indexMap[i]]) / ElectionScales.Max
                        : 0.0;
                    double priorShare = priorSum > 0.0 ? priorSubset[i] / priorSum : 0.0;
                    damped[i] = lambda * priorShare + (1.0 - lambda) * spatial[i];
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
