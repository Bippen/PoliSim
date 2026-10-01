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

        // §697: each Land's REGISTERED electorate (Wahlberechtigte), in the catalogs' order - ElectionsData/germany/land_eligible.csv, cut from the same
        // two kerg2.csv files the catalogs stand on; GermanRegionsDiagnostic re-reads the file and holds every figure, and the sixteen sum to the Bund's.
        private static readonly long[] Eligible2021 = { 2272717, 1314435, 1298792, 6105381, 459749, 2048844, 1789775, 2460618, 13040267, 3253667, 4383047, 1707726, 3053335, 9517664, 7711531, 755223 };
        private static readonly long[] Eligible2025 = { 2262811, 1294729, 1299289, 6043412, 450564, 2033539, 1734719, 2442042, 12884209, 3186780, 4341919, 1652462, 3014482, 9481659, 7653811, 734204 };

        /// <summary>§697: the Land's registered electorate on the chamber seated on <paramref name="date"/> - who a ground game can mobilise, as distinct from
        /// the valid votes a campaign's audience is (<see cref="RegionAudience.Eligible"/>, F3's distinction, as Sweden's valkretsar draw it).</summary>
        public static long EligibleAt(int region, DateTime date) => (WorldClock.SeatedVintage(CountryId.Germany, date) == ElectionVintage.Germany2021 ? Eligible2021 : Eligible2025)[region];

        /// <summary>§697: the Land's valid Zweitstimmen on the chamber seated on <paramref name="date"/> - a campaign's audience there.</summary>
        /// <summary>§707 (D-DE, 24a ①): the register an ELECTION is held on - 2025's for the 2025 election and after (the latest the record holds),
        /// 2021's before it. Not <see cref="EligibleAt"/>: on the 2025 polling day the 2021 chamber still sits, but the night counts 2025's register.</summary>
        public static long EligibleForElection(int region, DateTime pollingDay) => (pollingDay >= new DateTime(2025, 2, 23) ? Eligible2025 : Eligible2021)[region];

        public static long ValidAt(int region, DateTime date) => Catalog(WorldClock.SeatedVintage(CountryId.Germany, date)).Valid[region];

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
