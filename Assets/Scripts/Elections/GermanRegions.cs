using System;
using System.Collections.Generic;
using PoliSim.Data;
using PoliSim.Elections.Generated;

namespace PoliSim.Elections
{
    /// <summary>
    /// PS-4 (POLITICAL_SYSTEM_SPEC.md §9, stage 4), part one: **GERMANY'S SIXTEEN LÄNDER AS THE ELECTION'S REGIONS** - the runtime reader of
    /// the generated Länder catalogs, the way <see cref="SwedishRegions"/> reads the valkretsar. A Land is where the Zweitstimme is counted and
    /// the Landeslisten are drawn, so it is the unit a German campaign's regions and an election night's map are made of.
    ///
    /// <para><b>The SEATED election's result is the prior</b>, as for Sweden: what is taken from it is structure and starting position - each
    /// Land's weight (its valid Zweitstimmen), which parties stood there, and where each Land sat - and the model moves it. The seated election
    /// is the date's (<see cref="WorldClock.SeatedVintage"/>): a game opened on the snap start of 6 November 2024 sits on the 2021 chamber, so
    /// the 2025 campaign's prior is 2021's count per Land; after 23 February 2025 it is 2025's.</para>
    ///
    /// <para><b>Availability is the candidacy fact the returns carry</b>, and in Germany it is real: the CSU stands only in Bayern and the CDU
    /// everywhere else; the SSW only in Schleswig-Holstein; in 2021 the Grüne's Saarland list was rejected. A zero in the returns is a party
    /// that did not stand there. A party the vintage's file does not carry at all (the BSW, founded in January 2024, has no 2021 column) is
    /// treated as standing everywhere with no record of its own - absence from a file is not evidence of absence from a ballot, and its prior
    /// share is 0, which is what the loyalty term reads as "no record here".</para>
    /// </summary>
    public static class GermanRegions
    {
        /// <summary>The catalog for a seated vintage: 2021's before the 2025 chamber sat, 2025's from then on.</summary>
        private static (string[] Parties, string[] Names, long[] Valid, long[][] Votes) Catalog(ElectionVintage vintage) => vintage == ElectionVintage.Germany2021
            ? (GermanLandReturns2021.Parties, GermanLandReturns2021.Names, GermanLandReturns2021.Valid, GermanLandReturns2021.Votes)
            : (GermanLandReturns2025.Parties, GermanLandReturns2025.Names, GermanLandReturns2025.Valid, GermanLandReturns2025.Votes);

        /// <summary>Sixteen: the Länder of the Grundgesetz's preamble.</summary>
        public static int Count => GermanLandReturns2025.Names.Length;

        /// <summary>The Land's name at <paramref name="region"/>, in the catalogs' shared order.</summary>
        public static string NameAt(int region) => GermanLandReturns2025.Names[region];

        /// <summary>The regions for <paramref name="partyKeys"/> on the chamber seated on <paramref name="date"/>: each Land's valid Zweitstimmen as its
        /// weight and each party's availability there.</summary>
        public static RegionalVoteModel.RegionInput[] Regions(IReadOnlyList<string> partyKeys, DateTime date)
        {
            if (partyKeys == null || partyKeys.Count == 0) { throw new ArgumentException("no party keys"); }
            var c = Catalog(WorldClock.SeatedVintage(CountryId.Germany, date));
            int[] column = MapColumns(c.Parties, partyKeys);
            var regions = new RegionalVoteModel.RegionInput[c.Names.Length];
            for (int r = 0; r < c.Names.Length; r++)
            {
                var available = new bool[partyKeys.Count];
                for (int p = 0; p < partyKeys.Count; p++) { available[p] = column[p] < 0 || c.Votes[r][column[p]] > 0L; }
                regions[r] = new RegionalVoteModel.RegionInput(c.Names[r], c.Valid[r], available);
            }
            return regions;
        }

        /// <summary>Each Land's shares at the seated election, in <paramref name="partyKeys"/>' order - the PRIOR the model moves. A party the vintage
        /// does not carry reads 0.</summary>
        public static double[][] PriorShares(IReadOnlyList<string> partyKeys, DateTime date)
        {
            if (partyKeys == null || partyKeys.Count == 0) { throw new ArgumentException("no party keys"); }
            var c = Catalog(WorldClock.SeatedVintage(CountryId.Germany, date));
            int[] column = MapColumns(c.Parties, partyKeys);
            var prior = new double[c.Names.Length][];
            for (int r = 0; r < c.Names.Length; r++)
            {
                prior[r] = new double[partyKeys.Count];
                double valid = c.Valid[r];
                if (valid <= 0.0) { continue; }
                for (int p = 0; p < partyKeys.Count; p++) { if (column[p] >= 0) { prior[r][p] = c.Votes[r][column[p]] / valid; } }
            }
            return prior;
        }

        /// <summary>Each Land's count at the seated election, in <paramref name="partyKeys"/>' order - the previous election a night compares against.</summary>
        public static long[][] PreviousVotes(IReadOnlyList<string> partyKeys, DateTime date)
        {
            if (partyKeys == null || partyKeys.Count == 0) { throw new ArgumentException("no party keys"); }
            var c = Catalog(WorldClock.SeatedVintage(CountryId.Germany, date));
            int[] column = MapColumns(c.Parties, partyKeys);
            var votes = new long[c.Names.Length][];
            for (int r = 0; r < c.Names.Length; r++)
            {
                votes[r] = new long[partyKeys.Count];
                for (int p = 0; p < partyKeys.Count; p++) { if (column[p] >= 0) { votes[r][p] = c.Votes[r][column[p]]; } }
            }
            return votes;
        }

        private static int[] MapColumns(string[] parties, IReadOnlyList<string> partyKeys)
        {
            var column = new int[partyKeys.Count];
            for (int p = 0; p < partyKeys.Count; p++) { column[p] = Array.IndexOf(parties, partyKeys[p]); }
            return column;
        }
    }
}
