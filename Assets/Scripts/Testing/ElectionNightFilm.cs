using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using PoliSim.Elections;
using PoliSim.Simulation;
using UnityEngine;

namespace PoliSim.Testing
{
    /// <summary>
    /// W-E6's staging, in ONE place so the harness and the film count the same election. Sweden
    /// 2022's exact national counts (SOURCED, Valmyndigheten) spread over the 29 valkretsar by
    /// 2018's own per-valkrets distribution (SOURCED) — W-D2's construction, reused rather than
    /// re-derived, because two items disagreeing about which election they are counting would be
    /// worse than either being wrong alone.
    /// </summary>
    public static class ElectionNightFilm
    {
        public static readonly string[] Parties = { "S", "SD", "M", "V", "C", "KD", "MP", "L" };
        /// <summary>SOURCED - Valmyndigheten final 2022 national counts (ElectionsData/sweden/returns_2022.md).</summary>
        public static readonly long[] Votes2022 = { 1964474, 1330325, 1237428, 437050, 434945, 345712, 329242, 298542 };
        public static readonly int[] Seats2022 = { 107, 73, 68, 24, 24, 19, 18, 16 };
        /// <summary>
        /// SOURCED - Valmyndigheten, Riksdag 2018-09-09 slutligt valresultat, via
        /// `ElectionsData/priors/previous_elections.md`. In the driver's party order.
        /// Sweden is the one clean party-for-party join between the two elections: all eight
        /// 2022 parties contested 2018 as the same entities - no coalition lists, no splits,
        /// no renames - which is why a swing column against it is honest arithmetic rather
        /// than a mapping with a bias to declare.
        /// </summary>
        public static readonly long[] Votes2018 = { 1830386, 1135627, 1284698, 518454, 557500, 409478, 285899, 355546 };
        private const double Turnout2018 = 0.8721;

        /// <summary>
        /// SOURCED - Valmyndigheten via `ElectionsData/sweden/returns_2022.md:49`: 6 547 801
        /// ballots cast of 7 775 390 eligible = 84.21 %. The turnout basis is val.se's own
        /// (all ballots cast, blank and invalid included, over eligible voters).
        ///
        /// A results screen must quote THIS, not a turnout re-derived from the eight parties'
        /// own votes over a derived electorate: that arithmetic reads 85.88 %, which is not
        /// what Sweden's turnout was, and a screen is where a derived figure would be mistaken
        /// for the published one.
        /// </summary>
        public const long Eligible2022 = 7_775_390;
        public const long BallotsCast2022 = 6_547_801;
        public const double Turnout2022 = 0.8421187;

        /// <summary>The film's seed, so a capture family is reproducible (the FilmSeed discipline).</summary>
        public const int NightSeed = 777;

        public static void Stage(out string[] names, out long[][] votes, out long[] valid, out long[] eligible,
            out int[] arrivals, out string[] parties, out Dictionary<string, int[]> blocs)
        {
            votes = Regionalise(out names, out eligible);
            valid = new long[votes.Length];
            for (int r = 0; r < votes.Length; r++) { foreach (long v in votes[r]) { valid[r] += v; } }

            SimulationRandom.Seed(NightSeed);
            arrivals = ElectionNight.Schedule(eligible, SimulationRandom.For(SimulationRandom.Stream.ElectionNight));

            parties = Parties;
            blocs = new Dictionary<string, int[]>
            {
                { "the right bloc", new[] { 1, 2, 5, 7 } },
                { "the left bloc", new[] { 0, 3, 4, 6 } },
            };
        }

        /// <summary>The first minute at which at least this many constituencies have declared - the film picks its states by the count, not the clock.</summary>
        public static int MinuteFor(int declared, int[] arrivals)
        {
            for (int minute = 0; minute <= ElectionNight.NightMinutes; minute++)
            {
                int n = 0;
                foreach (int a in arrivals) { if (a <= minute) { n++; } }
                if (n >= declared) { return minute; }
            }

            return ElectionNight.NightMinutes;
        }

        public static long[][] Regionalise(out string[] names, out long[] eligible)
        {
            long[][] votes2018 = ReadValkrets(out names, out eligible);
            int regions = votes2018.Length;
            int parties = Parties.Length;
            var region = new long[regions][];
            for (int r = 0; r < regions; r++) { region[r] = new long[parties]; }

            for (int p = 0; p < parties; p++)
            {
                long total2018 = 0;
                for (int r = 0; r < regions; r++) { total2018 += votes2018[r][p]; }

                var fraction = new double[regions];
                long given = 0;
                for (int r = 0; r < regions; r++)
                {
                    double share = (double)Votes2022[p] * votes2018[r][p] / total2018;
                    region[r][p] = (long)Math.Floor(share);
                    fraction[r] = share - region[r][p];
                    given += region[r][p];
                }

                while (given < Votes2022[p])
                {
                    int best = 0;
                    for (int r = 1; r < regions; r++) { if (fraction[r] > fraction[best]) { best = r; } }
                    region[best][p]++;
                    fraction[best] = -1.0;
                    given++;
                }
            }

            return region;
        }

        private static long[][] ReadValkrets(out string[] names, out long[] eligible)
        {
            string path = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "ElectionsData", "sweden", "valkrets_votes_2018.csv"));
            var rows = new List<long[]>();
            var nameList = new List<string>();
            var eligibleList = new List<long>();
            int[] map = { 2, 4, 3, 6, 5, 7, 9, 8 };   // csv S;M;SD;C;V;KD;L;MP -> our S, SD, M, V, C, KD, MP, L
            foreach (string raw in File.ReadAllLines(path))
            {
                string line = raw.Trim();
                if (line.Length == 0 || line.StartsWith("#") || line.StartsWith("valkrets;")) { continue; }
                string[] cells = line.Split(';');
                nameList.Add(cells[0]);
                eligibleList.Add((long)Math.Round(double.Parse(cells[1], CultureInfo.InvariantCulture) / Turnout2018));
                var row = new long[Parties.Length];
                for (int p = 0; p < Parties.Length; p++) { row[p] = long.Parse(cells[map[p]], CultureInfo.InvariantCulture); }
                rows.Add(row);
            }

            names = nameList.ToArray();
            eligible = eligibleList.ToArray();
            return rows.ToArray();
        }
    }
}
