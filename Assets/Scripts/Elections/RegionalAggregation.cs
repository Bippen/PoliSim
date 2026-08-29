using System;

namespace PoliSim.Elections
{
    /// <summary>
    /// SPEC §27 — election-day simulation: each region calculated independently, each voter group
    /// within it, then aggregated, with controlled uncertainty added at the end. PURE FUNCTIONS,
    /// WIRED TO NOTHING (R-N2).
    ///
    /// The spec's per-group arithmetic, verbatim:
    /// <code>
    /// Population x Eligible Voters x Turnout x Party Preference
    /// </code>
    /// then "aggregate all groups", then `Final Vote = Expected Vote + Election Noise`.
    ///
    /// **The noise is drawn from its own named seeded stream** — `SimulationRandom.Stream.
    /// ElectionNoise`, appended for this purpose — and is passed in as a `System.Random` so these
    /// functions stay pure and a harness can replay any election exactly. The spec's requirement
    /// ("small enough that good strategy matters but large enough that elections cannot be
    /// perfectly predicted") is a magnitude question, and the magnitude is
    /// **[AUTHORED-DRAFT]**, logged and strikeable:
    /// - `RegionalNoiseSigmaPp = 1.2` — one standard deviation, in percentage points, applied per
    ///   party per REGION. Chosen against the day's own measurement: the placeholder vote model
    ///   sat at 3–7 pp of systematic error, so noise at ~1 pp is well inside the signal a real
    ///   campaign moves, while still being enough to flip a genuinely marginal region (§25's swing
    ///   index exists precisely because those regions are decided inside this band).
    /// - Noise is applied REGIONALLY, not nationally, and therefore partially cancels in the
    ///   national total — which is the correct shape: national polls are more accurate than any
    ///   single constituency forecast, and a model whose national number was as noisy as its
    ///   regional ones would be lying about both.
    ///
    /// Noise is additive in share space and the result is re-normalised and floored at zero, so a
    /// party can be pushed to nothing but never below it, and shares always sum to 1.
    /// </summary>
    public static class RegionalAggregation
    {
        public const double RegionalNoiseSigmaPp = 1.2;

        /// <summary>One region's outcome: votes per party, the turnout actually achieved, and the eligible electorate it was drawn from.</summary>
        public readonly struct RegionResult
        {
            public readonly string Region;
            public readonly double[] Votes;
            public readonly double EligibleVoters;
            public readonly double VotesCast;

            public RegionResult(string region, double[] votes, double eligibleVoters, double votesCast)
            {
                Region = region;
                Votes = votes;
                EligibleVoters = eligibleVoters;
                VotesCast = votesCast;
            }

            public double Turnout => EligibleVoters > 0 ? VotesCast / EligibleVoters : 0.0;
        }

        /// <summary>
        /// §27 for ONE region: for each voter group, population × eligible × group share × turnout
        /// × preference, summed into per-party votes. <paramref name="groupPreferences"/> is
        /// [group][party] (from <see cref="PreferenceModel"/>); <paramref name="groupTurnout"/> is
        /// one rate per group (from <see cref="TurnoutModel"/>).
        /// </summary>
        public static RegionResult Region(RegionProfile region, VoterGroupProfile[] groups,
            double[][] groupPreferences, double[] groupTurnout, int partyCount)
        {
            if (groups.Length != groupPreferences.Length || groups.Length != groupTurnout.Length)
            {
                throw new ArgumentException("groups, preferences and turnout must line up");
            }

            if (region.GroupShares != null && region.GroupShares.Length != groups.Length)
            {
                throw new ArgumentException($"{region.Name}: group shares must be one per group");
            }

            var votes = new double[partyCount];
            double eligible = region.Population * region.EligibleShare;
            double cast = 0.0;

            for (int g = 0; g < groups.Length; g++)
            {
                double share = region.GroupShares != null ? region.GroupShares[g] : groups[g].PopulationShare;
                double groupEligible = eligible * share;
                double groupVotes = groupEligible * groupTurnout[g];
                cast += groupVotes;

                for (int p = 0; p < partyCount; p++)
                {
                    votes[p] += groupVotes * groupPreferences[g][p];
                }
            }

            return new RegionResult(region.Name, votes, eligible, cast);
        }

        /// <summary>
        /// Adds §27's election noise to a region's party SHARES and re-normalises. Deterministic
        /// given <paramref name="random"/> — pass `SimulationRandom.For(Stream.ElectionNoise)` in
        /// a wired context, or a seeded `System.Random` in a harness.
        /// </summary>
        public static double[] ApplyNoise(double[] shares, System.Random random, double sigmaPp = RegionalNoiseSigmaPp)
        {
            if (random == null) { throw new ArgumentNullException(nameof(random)); }

            var noisy = new double[shares.Length];
            double total = 0.0;
            for (int i = 0; i < shares.Length; i++)
            {
                double perturbed = shares[i] + Gaussian(random) * (sigmaPp / 100.0);
                noisy[i] = perturbed < 0.0 ? 0.0 : perturbed;
                total += noisy[i];
            }

            if (total <= 0.0) { return shares; }

            for (int i = 0; i < noisy.Length; i++) { noisy[i] /= total; }
            return noisy;
        }

        /// <summary>Sums regional vote counts into a national total, and returns national shares.</summary>
        public static double[] NationalShares(RegionResult[] regions, int partyCount)
        {
            var totals = new double[partyCount];
            double sum = 0.0;
            foreach (RegionResult region in regions)
            {
                for (int p = 0; p < partyCount; p++)
                {
                    totals[p] += region.Votes[p];
                    sum += region.Votes[p];
                }
            }

            if (sum <= 0.0) { return totals; }

            for (int p = 0; p < partyCount; p++) { totals[p] /= sum; }
            return totals;
        }

        /// <summary>Box–Muller standard normal from a `System.Random` — no `UnityEngine.Random` anywhere near the simulation (the A0 rule).</summary>
        private static double Gaussian(System.Random random)
        {
            double u1 = 1.0 - random.NextDouble();
            double u2 = 1.0 - random.NextDouble();
            return Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2.0 * Math.PI * u2);
        }
    }
}
