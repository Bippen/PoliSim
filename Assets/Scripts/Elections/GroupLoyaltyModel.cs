using System;
using System.Collections.Generic;

namespace PoliSim.Elections
{
    /// <summary>
    /// **Per-GROUP loyalty, derived** (E-1 landed, 2026-09-10) - `LoyaltyModel`'s formula applied inside each voter
    /// group, so a party whose 2018 voters were not its 2013 voters in one age band and were in another is not
    /// represented by one lambda. PURE FUNCTIONS.
    ///
    /// <para><b>The invariant is `LoyaltyModel`'s and is inherited whole:</b> a group's loyalty to a party is computed
    /// from the two elections BEFORE the one being modelled - for the 2022 backtest, 2013 and 2018 - and never from the
    /// target. This type takes two historical group-by-party matrices and is never given the target.</para>
    ///
    /// <para><b>Where the group shares come from, and the one operation done to them.</b> The surveys (ITANES 2013 and
    /// 2018, `Generated.ItanesVoteByAge`) give each age band's reported vote. Their national marginals sit off the
    /// official returns in different directions by wave (2013 CAPI under-reports M5S and PdL; the 2018 web panel
    /// over-reports M5S), so a loyalty read off the surveys' own levels would score the change of survey MODE as voter
    /// movement. <see cref="AnchoredGroupShares"/> therefore takes from a survey only the distribution ACROSS bands -
    /// each band's propensity for a party relative to the whole sample - and anchors it to the official national share
    /// of that same wave: <c>share_g,i = official_i × survey_g,i ⁄ survey_all,i</c>. Averaged over the bands with the
    /// SURVEY's own band weights this reproduces the official national share exactly, so the per-group derivation
    /// cannot drift from the per-party one on the national level; it can only differ inside the groups, which is the
    /// point. ⚠ The anchor is the wave's OWN official result - T−2's for T−2, T−1's for T−1 - never the target's.</para>
    ///
    /// <para><b>Zero authored constants.</b> A party the survey never sees in a band scores 0 there at that wave, which
    /// makes its loyalty in that band 0 - `LoyaltyModel`'s own statement that nobody there had a habit of voting for it.
    /// ⚠ With a thin cell that is a statement about the sample as much as about the electorate, and the harness prints
    /// the cell's n beside every such figure.</para>
    /// </summary>
    public static class GroupLoyaltyModel
    {
        /// <summary>
        /// Each group's shares of the modelled parties, anchored to the official national result of the same wave.
        /// <paramref name="surveyCounts"/> is [group][party] as the survey counted it (weighted or not);
        /// <paramref name="surveyWeight"/> is each group's weight sum in that survey; <paramref name="officialPct"/> the
        /// official national shares of the same election, party order matching. A party with no survey count anywhere
        /// takes the official share in every group (the survey has no information about its distribution); a party with
        /// no official share is 0 everywhere.
        /// </summary>
        public static double[][] AnchoredGroupShares(double[] officialPct, double[][] surveyCounts, double[] surveyWeight)
        {
            int groups = surveyCounts.Length;
            int parties = officialPct.Length;
            double totalWeight = 0.0;
            foreach (double w in surveyWeight) { totalWeight += w; }
            if (groups == 0 || totalWeight <= 0.0) { throw new ArgumentException("no survey groups"); }

            var surveyAll = new double[parties];
            for (int g = 0; g < groups; g++)
            {
                if (surveyCounts[g].Length != parties) { throw new ArgumentException("survey counts must be one per party per group"); }
                for (int p = 0; p < parties; p++) { surveyAll[p] += surveyCounts[g][p]; }
            }

            var result = new double[groups][];
            for (int g = 0; g < groups; g++)
            {
                result[g] = new double[parties];
                for (int p = 0; p < parties; p++)
                {
                    if (officialPct[p] <= 0.0) { continue; }
                    if (surveyAll[p] <= 0.0) { result[g][p] = officialPct[p]; continue; }
                    // the band's propensity relative to the sample: (count_g / weight_g) / (count_all / weight_all)
                    double propensity = (surveyCounts[g][p] / surveyWeight[g]) / (surveyAll[p] / totalWeight);
                    result[g][p] = officialPct[p] * propensity;
                }
            }

            return result;
        }

        /// <summary>Loyalty [group][party] from the two preceding elections' group shares - <see cref="LoyaltyModel.PartyLoyalty"/> per cell.</summary>
        public static double[][] GroupLoyalties(double[][] previousByGroup, double[][] previousPreviousByGroup)
        {
            if (previousByGroup == null || previousPreviousByGroup == null || previousByGroup.Length != previousPreviousByGroup.Length)
            {
                throw new ArgumentException("the two group matrices must line up");
            }

            var loyalty = new double[previousByGroup.Length][];
            for (int g = 0; g < loyalty.Length; g++)
            {
                loyalty[g] = LoyaltyModel.PartyLoyalties(previousByGroup[g], previousPreviousByGroup[g]);
            }

            return loyalty;
        }

        /// <summary>The group weights normalised to sum to one - the groups' shares of the electorate the blend aggregates over.</summary>
        public static double[] NormalisedWeights(IReadOnlyList<double> weights)
        {
            double sum = 0.0;
            foreach (double w in weights) { sum += w; }
            if (sum <= 0.0) { throw new ArgumentException("group weights must sum to a positive number"); }
            var result = new double[weights.Count];
            for (int g = 0; g < result.Length; g++) { result[g] = weights[g] / sum; }
            return result;
        }

        /// <summary>The per-party loyalty the group matrix implies nationally: each party's loyalty averaged over the groups,
        /// weighted by the group's share of that party's T−1 vote - the figure to set beside the uniform per-party one.</summary>
        public static double[] ImpliedPartyLoyalty(double[][] loyaltyByGroup, double[][] previousByGroup, double[] groupWeights)
        {
            int parties = loyaltyByGroup[0].Length;
            var result = new double[parties];
            for (int p = 0; p < parties; p++)
            {
                double num = 0.0, den = 0.0;
                for (int g = 0; g < loyaltyByGroup.Length; g++)
                {
                    double w = groupWeights[g] * previousByGroup[g][p];
                    num += w * loyaltyByGroup[g][p];
                    den += w;
                }
                result[p] = den > 0.0 ? num / den : 0.0;
            }
            return result;
        }
    }
}
