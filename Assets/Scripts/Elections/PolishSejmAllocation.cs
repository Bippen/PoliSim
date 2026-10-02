using System;
using System.Collections.Generic;
using PoliSim.Data;
using PoliSim.Elections.Generated;

namespace PoliSim.Elections
{
    /// <summary>
    /// §762 (PS-5, the live wiring part two): POLAND'S SEJM BY ITS OWN LAW, LIVE. §730 proved the law on the record in an Editor instrument
    /// (`PolishSejmAllocationDiagnostic`: the 2023 chamber seat for seat); this is the runtime count the game's own Polish elections run. The law
    /// (Kodeks wyborczy; `ElectionsData/poland/returns_2023.md`, Rules): <b>41 multi-member districts</b> (art. 201), <b>d'Hondt in each district</b>
    /// (art. 232), <b>no national compensatory tier</b>; a party's committee must win <b>5 %</b> of the valid votes nationwide, a coalition's
    /// <b>8 %</b> (art. 196 § 1); a national minority's committee is exempt (art. 197 § 1 - `NationalElection.ExemptFromThreshold`, the one rule).
    /// <para><b>The game's vote into the districts</b> (DERIVED, the standard reading §730 named, stated): a uniform swing from 2023's district results -
    /// each committee's share in each okreg moves by its national change since 2023 (the national shares read on the 2023 committees' own sum, the same
    /// base the districts' shares are read on), floored at zero; a list with no 2023 committee (the 2019 lists the roster carries) takes its national
    /// share in every okreg. The thresholds read the game's national shares, a fraction of the valid vote (a game's shares sum to one - the lists the
    /// roster does not carry are not modelled, stated). The committees' kinds are 2023's (KO and TD coalitions).</para>
    /// </summary>
    public static class PolishSejmAllocation
    {
        private enum Kind { Party, Coalition, Minority }

        /// <summary>SOURCED - Kodeks wyborczy art. 196 § 1: a party's committee, 5 % of the valid votes nationwide.</summary>
        public const double PartyThreshold = 0.05;
        /// <summary>SOURCED - Kodeks wyborczy art. 196 § 1: a coalition's committee, 8 %.</summary>
        public const double CoalitionThreshold = 0.08;

        /// <summary>The committees' kinds as the PKW registered them in 2023 (returns_2023.md): KO and TD coalitions; the minority's by the exemption rule.</summary>
        private static Kind KindOf(string committee) =>
            committee == "KO" || committee == "TD" ? Kind.Coalition
            : NationalElection.ExemptFromThreshold(CountryId.Poland, committee) ? Kind.Minority
            : Kind.Party;

        /// <summary>Whether a list clears the national threshold on <paramref name="share"/> (a fraction of the valid vote) - a minority's always does.
        /// Seen from outside as which lists take part in each okreg's division (<see cref="Allocate"/>'s per-district result).</summary>
        private static bool Admitted(string committee, double share) =>
            KindOf(committee) == Kind.Minority || share >= (KindOf(committee) == Kind.Coalition ? CoalitionThreshold : PartyThreshold);

        /// <summary>
        /// The Sejm the game's national vote returns: <paramref name="shares"/> (fractions of the valid vote, keyed by the roster's abbreviations) spread over
        /// the 41 okregi by the uniform swing, d'Hondt in each among the lists that clear the threshold. Every key of <paramref name="shares"/> is in the
        /// result (0 where it wins nothing). <paramref name="perDistrict"/>: each okreg's seats by list, for a caller that shows them.
        /// </summary>
        public static Dictionary<string, int> Allocate(IReadOnlyDictionary<string, double> shares, out List<Dictionary<string, int>> perDistrict)
        {
            string[] committees = PolishSejmDistricts2023.Committees;
            long[][] votes = PolishSejmDistricts2023.Votes;
            int districts = votes.Length;
            // 2023's national shares on the committees' own sum, and each okreg's
            var national2023 = new double[committees.Length];
            long sum2023 = 0;
            for (int d = 0; d < districts; d++) { for (int c = 0; c < committees.Length; c++) { national2023[c] += votes[d][c]; sum2023 += votes[d][c]; } }
            for (int c = 0; c < committees.Length; c++) { national2023[c] /= sum2023; }

            var keys = new List<string>();
            foreach (KeyValuePair<string, double> kv in shares) { keys.Add(kv.Key); }
            keys.Sort(StringComparer.Ordinal);   // a fixed order, whatever the caller's dictionary
            var seats = new Dictionary<string, int>();
            foreach (string k in keys) { seats[k] = 0; }
            perDistrict = new List<Dictionary<string, int>>();

            for (int d = 0; d < districts; d++)
            {
                long size = 0;
                for (int c = 0; c < committees.Length; c++) { size += votes[d][c]; }
                var lists = new List<(string Key, double Votes)>();
                foreach (string k in keys)
                {
                    double national = shares[k];
                    if (!Admitted(k, national)) { continue; }
                    int c = Array.IndexOf(committees, k);
                    double local = c >= 0 ? Math.Max(0.0, (double)votes[d][c] / size + (national - national2023[c])) : national;
                    if (local > 0.0) { lists.Add((k, local * size)); }
                }
                Dictionary<string, int> won = DHondt(lists, PolishSejmDistricts2023.Magnitudes[d]);
                foreach (KeyValuePair<string, int> kv in won) { seats[kv.Key] += kv.Value; }
                perDistrict.Add(won);
            }
            return seats;
        }

        /// <summary>d'Hondt in one okreg: each seat to the highest quotient votes / (won + 1); a tie to the list with more votes, then the first in order.</summary>
        private static Dictionary<string, int> DHondt(List<(string Key, double Votes)> lists, int magnitude)
        {
            var won = new Dictionary<string, int>();
            foreach ((string key, double _) in lists) { won[key] = 0; }
            for (int s = 0; s < magnitude && lists.Count > 0; s++)
            {
                int best = 0; double bestQ = -1.0;
                for (int i = 0; i < lists.Count; i++)
                {
                    double q = lists[i].Votes / (won[lists[i].Key] + 1);
                    if (q > bestQ || (q == bestQ && lists[i].Votes > lists[best].Votes)) { best = i; bestQ = q; }
                }
                won[lists[best].Key]++;
            }
            return won;
        }
    }
}
