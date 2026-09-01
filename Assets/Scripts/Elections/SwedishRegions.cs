using System;
using System.Collections.Generic;
using PoliSim.Elections.Generated;

namespace PoliSim.Elections
{
    /// <summary>
    /// **Sweden's 29 valkrets as the live election's regions — F1's runtime consumer.**
    ///
    /// <para><b>Why this file exists at all.</b> `ElectionsData/` sits outside `Assets/`, so runtime code
    /// could not read it and a built player would not have it. That was the root under S-33: board 1h,
    /// `RegionalVoteModel` and the tactical layer were all unreachable **because their data was**. The
    /// generated catalog moved into the runtime assembly on the day this consumer arrived, which is the
    /// condition the generator wrote for itself.</para>
    ///
    /// <para>⚠ <b>THE 2022 RESULT IS A PRIOR, NOT A STAND-IN, and the distinction is the whole honesty of
    /// this layer.</b> F1's rule is that no 2022 count may stand in for a simulated election. What is taken
    /// from 2022 here is <b>structure and starting position</b>: how many valid votes each valkrets casts
    /// (its weight), which parties stood there, and where each valkrets sat last time. **The model then
    /// moves it.** `RegionalVoteModel.NationalSharesWithLoyalty` applies the simulated national movement to
    /// each region's own prior, with the loyalty term deciding how far a region travels — which is how a
    /// region ends up somewhere the 2022 file never said.</para>
    ///
    /// <para>⚠ <b>Without the prior this layer would be arithmetic wearing a result's clothes.</b> Every
    /// one of Sweden's eight parties stands in every valkrets, so with uniform availability and no
    /// per-region position, `RegionalVoteModel` returns **exactly the national shares** — and a
    /// per-constituency "count" would be the national percentage multiplied by each region's electorate.
    /// **That would declare identical percentages in Stockholm and Skåne**, which anybody who has seen a
    /// Swedish election night would know to be false. The layer's own doc says as much: with no
    /// non-circular source of regional preference variation, the honest regional prediction IS the national
    /// one. **The prior is that source, and it is a measurement rather than a fit.**</para>
    ///
    /// <para><b>What is NOT claimed.</b> The prior is 2022's, so a valkrets that has realigned since is
    /// modelled as it was — this layer has no source for a region moving differently from the nation
    /// beyond its starting point and its loyalty. That is a stated limit, not a defect to be tuned away,
    /// and the honest fix is regional demographics from the model's own cohorts (F2/F3), never a
    /// per-region parameter fitted against regional results, which is circular by construction.</para>
    /// </summary>
    public static class SwedishRegions
    {
        /// <summary>The party order the catalog stores votes in.</summary>
        public static IReadOnlyList<string> Parties => SwedishValkretsReturns2022.Parties;

        /// <summary>How many valkrets the catalog carries. 29 is the Riksdag's real count.</summary>
        public static int Count => SwedishValkretsReturns2022.Names.Length;

        /// <summary>
        /// The regions in the party order <paramref name="partyKeys"/> gives, so a caller that has dropped
        /// a party (one with no published position, say) gets availability vectors that line up with the
        /// points it is actually predicting.
        ///
        /// <para>⚠ <b>The weight is VALID VOTES, not the eligible electorate.</b> `RegionalVoteModel`
        /// aggregates vote-weighted — a region contributes what it actually casts — so weighting by
        /// eligibility would give a low-turnout region a say it did not take. Turnout is a real, unequal
        /// quantity across Swedish valkrets and using eligibility would silently flatten it.</para>
        /// </summary>
        public static RegionalVoteModel.RegionInput[] Regions(IReadOnlyList<string> partyKeys)
        {
            if (partyKeys == null || partyKeys.Count == 0) { throw new ArgumentException("no party keys"); }

            int[] column = MapColumns(partyKeys);
            var regions = new RegionalVoteModel.RegionInput[Count];

            for (int r = 0; r < Count; r++)
            {
                var available = new bool[partyKeys.Count];
                for (int p = 0; p < partyKeys.Count; p++)
                {
                    // A party the catalog does not carry is treated as standing: absence from an
                    // eight-party file is not evidence that a ninth party did not stand, and assuming
                    // otherwise would silently remove it from every region.
                    available[p] = column[p] < 0 || SwedishValkretsReturns2022.Votes[r][column[p]] > 0L;
                }

                regions[r] = new RegionalVoteModel.RegionInput(
                    SwedishValkretsReturns2022.Names[r],
                    SwedishValkretsReturns2022.Valid[r],
                    available);
            }

            return regions;
        }

        /// <summary>
        /// Each valkrets' 2022 shares, in <paramref name="partyKeys"/>' order — the PRIOR the model moves.
        /// ⚠ A party the catalog does not carry gets 0 here, which is correct for a prior: it means *this
        /// region has no record of that party*, and the loyalty term is what decides how much a region's
        /// record binds it.
        /// </summary>
        public static double[][] PriorShares(IReadOnlyList<string> partyKeys)
        {
            if (partyKeys == null || partyKeys.Count == 0) { throw new ArgumentException("no party keys"); }

            int[] column = MapColumns(partyKeys);
            var prior = new double[Count][];

            for (int r = 0; r < Count; r++)
            {
                double valid = SwedishValkretsReturns2022.Valid[r];
                prior[r] = new double[partyKeys.Count];
                if (valid <= 0.0) { continue; }

                for (int p = 0; p < partyKeys.Count; p++)
                {
                    if (column[p] < 0) { continue; }
                    prior[r][p] = SwedishValkretsReturns2022.Votes[r][column[p]] / valid;
                }
            }

            return prior;
        }

        /// <summary>A valkrets' eligible electorate — how many people COULD vote there, which is what an
        /// election night's outstanding-votes figure is measured against. ⚠ Distinct from the weight, which
        /// is what was actually cast: the difference is turnout, and conflating them would make a night
        /// report every constituency as fully counted the moment it declared.</summary>
        public static long EligibleAt(int region) => SwedishValkretsReturns2022.Eligible[region];

        /// <summary>Where each requested party sits in the catalog's column order, or -1.</summary>
        private static int[] MapColumns(IReadOnlyList<string> partyKeys)
        {
            var column = new int[partyKeys.Count];
            for (int p = 0; p < partyKeys.Count; p++)
            {
                column[p] = -1;
                for (int c = 0; c < SwedishValkretsReturns2022.Parties.Length; c++)
                {
                    if (string.Equals(SwedishValkretsReturns2022.Parties[c], partyKeys[p], StringComparison.Ordinal))
                    {
                        column[p] = c;
                        break;
                    }
                }
            }

            return column;
        }
    }
}
