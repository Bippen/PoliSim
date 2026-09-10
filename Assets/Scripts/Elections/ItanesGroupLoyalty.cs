using System;
using System.Collections.Generic;
using PoliSim.Data;
using PoliSim.Elections.Generated;

namespace PoliSim.Elections
{
    /// <summary>
    /// **Italy's per-group inputs, built once** (E-1 landed, 2026-09-10): the ITANES cross-tabs (`ItanesVoteByAge`)
    /// anchored to the official 2013 and 2018 returns, the per-group loyalty from them, and the six bands' weights -
    /// one construction read by `LoyaltyHarness`, `GateReRun` and `ItalySurgeCeilingDiagnostic`, so the three cannot
    /// disagree about what the surveys said.
    ///
    /// <para><b>Non-circular by construction:</b> it is handed the T−2 and T−1 official shares and nothing later. The
    /// caller decides which two elections those are; for the 2022 backtest they are 2013 and 2018, and the surveys ARE
    /// the 2013 and 2018 waves, so there is no target-year input to hand in by mistake.</para>
    ///
    /// <para><b>The group weights</b> are the six bands' shares of Italy's eligible population over the seeded pyramid
    /// (`PopulationPyramids`, Eurostat 1 January 2024) - the electorate's age structure, since no turnout by age is
    /// sourced for Italy. ⚠ It is a 2024 stock standing in for 2022's, the nearest sourced pyramid; the model's own
    /// cohort projection begins at 2024 too. The difference over two years is small and it is stated rather than hidden.</para>
    ///
    /// <para><b>The party join</b> is by name: a case party joins the survey column of the same name, or the column
    /// `&lt;name&gt;_lineage` where the survey carries a predecessor (AVS ← SEL / LeU), and a party with no column (AzIV,
    /// founded 2019) has no survey count anywhere, so every band takes its official share - which is 0 for both waves,
    /// and its loyalty is 0, as the per-party model already says.</para>
    /// </summary>
    public static class ItanesGroupLoyalty
    {
        public sealed class Inputs
        {
            public string[] Bands;
            public string[] Parties;
            /// <summary>Anchored group shares at T−2 (2013) and T−1 (2018), [band][party], percent.</summary>
            public double[][] T2ByGroup, T1ByGroup;
            /// <summary>Loyalty [band][party], 2013→2018 inside each band.</summary>
            public double[][] LoyaltyByGroup;
            /// <summary>Each band's share of the eligible electorate, normalised.</summary>
            public double[] GroupWeights;
            /// <summary>Respondents behind each band, per wave, and per party per band at 2013 (the thin cells).</summary>
            public double[] N2013, N2018;
            public double[][] Counts2013;
            /// <summary>Which survey column each party read, or -1.</summary>
            public int[] Column;
        }

        /// <summary>Build for a party order and its official T−2 / T−1 shares (percent).</summary>
        public static Inputs Build(string[] partyNames, double[] officialT2Pct, double[] officialT1Pct)
        {
            if (partyNames == null || officialT2Pct == null || officialT1Pct == null
                || partyNames.Length != officialT2Pct.Length || partyNames.Length != officialT1Pct.Length)
            {
                throw new ArgumentException("party names and the two official share vectors must line up");
            }

            int groups = ItanesVoteByAge.Bands.Length;
            var column = new int[partyNames.Length];
            for (int p = 0; p < partyNames.Length; p++)
            {
                column[p] = Array.IndexOf(ItanesVoteByAge.Parties, partyNames[p]);
                if (column[p] < 0) { column[p] = Array.IndexOf(ItanesVoteByAge.Parties, partyNames[p] + "_lineage"); }
            }

            double[][] survey13 = Project(ItanesVoteByAge.Counts2013, column, groups, partyNames.Length);
            double[][] survey18 = Project(ItanesVoteByAge.Counts2018, column, groups, partyNames.Length);

            PopulationCohorts pyramid = PopulationPyramids.For(CountryId.Italy);
            double[] weights = GroupLoyaltyModel.NormalisedWeights(
                CohortVoterGroups.SixBandShares(pyramid, CohortVoterGroups.VotingAge(CountryId.Italy)));

            var inputs = new Inputs
            {
                Bands = (string[])ItanesVoteByAge.Bands.Clone(),
                Parties = (string[])partyNames.Clone(),
                T2ByGroup = GroupLoyaltyModel.AnchoredGroupShares(officialT2Pct, survey13, ItanesVoteByAge.WeightSum2013),
                T1ByGroup = GroupLoyaltyModel.AnchoredGroupShares(officialT1Pct, survey18, ItanesVoteByAge.WeightSum2018),
                GroupWeights = weights,
                N2013 = (double[])ItanesVoteByAge.N2013.Clone(),
                N2018 = (double[])ItanesVoteByAge.N2018.Clone(),
                Counts2013 = survey13,
                Column = column,
            };
            inputs.LoyaltyByGroup = GroupLoyaltyModel.GroupLoyalties(inputs.T1ByGroup, inputs.T2ByGroup);
            return inputs;
        }

        private static double[][] Project(double[][] counts, int[] column, int groups, int parties)
        {
            var result = new double[groups][];
            for (int g = 0; g < groups; g++)
            {
                result[g] = new double[parties];
                for (int p = 0; p < parties; p++) { result[g][p] = column[p] >= 0 ? counts[g][column[p]] : 0.0; }
            }
            return result;
        }
    }
}
