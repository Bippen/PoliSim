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
    /// <para><b>A committee that stood in ONE okreg in 2023 keeps its votes there</b> (Elias, 2026-10-23, on the D-PL ask: a committee that stood in one
    /// okreg keeps its rise there - MN, the German minority's, okreg 21; `SoleOkreg` reads which committee from the record itself). DECLARED, the reading:
    /// (i) its votes are its national share times the base, all in its okreg - in share terms its 2023 share there plus its national change times
    /// base over the okreg's size - and none anywhere else, so its national total is its national share exactly; (ii) every other list swings as though
    /// that change were not there - its own national change plus its part of the sole committee's change, the parts in proportion to the lists'
    /// national shares (the sole committees' left out) - in every okreg; (iii) in the sole committee's okreg the other lists also give up (or take back)
    /// that change in votes, each its same part, base over size times the change. Before the floor at zero every okreg's votes sum to its 2023 size and
    /// every list's national total is its national share times the base, both exactly (where the shares sum to one and the roster carries the 2023
    /// committees). The floor binds first in the sole committee's okreg: as the committee's national votes near the okreg's 2023 size, the other list
    /// there with the least to give runs out and is held at zero, and from then the okreg's votes exceed its size by what the floor lifted. Past the
    /// share at which the committee's national votes equal that size (the okreg's size over the base), the committee alone has more votes there than
    /// the okreg had voters in 2023. DECLARED: nothing caps it - a cap would be a ruling, not a reading; the committee then takes every seat of the
    /// okreg, the other lists none there, the chamber still its full count, and the night still the chamber (`PolishNightDiagnostic` holds both on a
    /// share past it); each other list's national total there exceeds its share times the base by its lift, which its line never reads (the night's
    /// residue). With every share at 2023's the change is zero
    /// and the count is the record's, expression for expression. A sole committee the shares do not carry has no change and no votes. The night
    /// (`PolishSejmNight`) reads the same votes through `SwungVotes`, so the night and the chamber are one count.</para>
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

        /// <summary>D-PL: a committee's line - `CoalitionThreshold` for a coalition, `PartyThreshold` for a party, 0 for the minority (exempt) - the
        /// same kinds `Admitted` reads, for the election night that calls each list against its own line (`PolishSejmNight`).</summary>
        public static double LineOf(string committee) => KindOf(committee) == Kind.Minority ? 0.0 : KindOf(committee) == Kind.Coalition ? CoalitionThreshold : PartyThreshold;

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
            var keys = new List<string>();
            foreach (KeyValuePair<string, double> kv in shares) { keys.Add(kv.Key); }
            keys.Sort(StringComparer.Ordinal);   // a fixed order, whatever the caller's dictionary
            double[][] spread = SwungVotes(keys, shares);
            var seats = new Dictionary<string, int>();
            foreach (string k in keys) { seats[k] = 0; }
            perDistrict = new List<Dictionary<string, int>>();

            for (int d = 0; d < spread.Length; d++)
            {
                var lists = new List<(string Key, double Votes)>();
                for (int i = 0; i < keys.Count; i++)
                {
                    if (!Admitted(keys[i], shares[keys[i]])) { continue; }
                    if (spread[d][i] > 0.0) { lists.Add((keys[i], spread[d][i])); }
                }
                Dictionary<string, int> won = DHondt(lists, PolishSejmDistricts2023.Magnitudes[d]);
                foreach (KeyValuePair<string, int> kv in won) { seats[kv.Key] += kv.Value; }
                perDistrict.Add(won);
            }
            return seats;
        }

        /// <summary>D-PL2: the okreg (an index into the record's rows) a 2023 committee stood in where it stood in one alone - its 2023 votes are
        /// there and nowhere else - or -1 for a committee that stood in several, or a key with no 2023 committee.</summary>
        public static int SoleOkreg(string committee)
        {
            int c = Array.IndexOf(PolishSejmDistricts2023.Committees, committee);
            if (c < 0) { return -1; }
            int found = -1;
            long[][] votes = PolishSejmDistricts2023.Votes;
            for (int d = 0; d < votes.Length; d++)
            {
                if (votes[d][c] <= 0) { continue; }
                if (found >= 0) { return -1; }
                found = d;
            }
            return found;
        }

        /// <summary>
        /// The votes each okreg counts per key (rows the record's okregi in order, columns <paramref name="keys"/>' order) for the national
        /// <paramref name="shares"/> - the spread the head of this class declares: the uniform swing from 2023, a sole committee's votes in its own okreg
        /// alone and the other lists' swing carrying its change, floored at zero, times the okreg's 2023 size. A key the shares do not carry has none.
        /// The chamber divides these (`Allocate`) and the night declares them (`PolishSejmNight.DistrictVotes`): one count.
        /// </summary>
        internal static double[][] SwungVotes(IReadOnlyList<string> keys, IReadOnlyDictionary<string, double> shares)
        {
            string[] committees = PolishSejmDistricts2023.Committees;
            long[][] votes = PolishSejmDistricts2023.Votes;
            int districts = votes.Length;
            // 2023's national shares on the committees' own sum, and each okreg's
            var national2023 = new double[committees.Length];
            long sum2023 = 0;
            for (int d = 0; d < districts; d++) { for (int c = 0; c < committees.Length; c++) { national2023[c] += votes[d][c]; sum2023 += votes[d][c]; } }
            for (int c = 0; c < committees.Length; c++) { national2023[c] /= sum2023; }
            var size = new long[districts];
            for (int d = 0; d < districts; d++) { for (int c = 0; c < committees.Length; c++) { size[d] += votes[d][c]; } }

            // the sole committees' changes, and what each okreg's other lists carry of them per unit of national share: the change everywhere (ii), less
            // base over size times the change in the committee's own okreg (iii) - zero in every okreg while no sole committee's share has moved
            var sole = new int[committees.Length];
            var carry = new double[districts];
            for (int c = 0; c < committees.Length; c++)
            {
                sole[c] = SoleOkreg(committees[c]);
                if (sole[c] < 0 || shares == null || !shares.TryGetValue(committees[c], out double moved)) { continue; }
                double change = moved - national2023[c];
                for (int d = 0; d < districts; d++) { carry[d] += d == sole[c] ? change * (1.0 - (double)sum2023 / size[d]) : change; }
            }
            // the other lists' parts: their national shares over the sum of every share but the sole committees', read in a fixed order
            var sorted = new List<string>();
            if (shares != null) { foreach (KeyValuePair<string, double> kv in shares) { sorted.Add(kv.Key); } }
            sorted.Sort(StringComparer.Ordinal);
            double others = 0.0;
            foreach (string k in sorted) { int c = Array.IndexOf(committees, k); if (c < 0 || sole[c] < 0) { others += shares[k]; } }

            var result = new double[districts][];
            for (int d = 0; d < districts; d++)
            {
                result[d] = new double[keys.Count];
                for (int k = 0; k < keys.Count; k++)
                {
                    if (shares == null || !shares.TryGetValue(keys[k], out double national)) { continue; }
                    int c = Array.IndexOf(committees, keys[k]);
                    double local;
                    if (c >= 0 && sole[c] >= 0)
                    {
                        // (i) a sole committee: its national votes, all in its okreg
                        if (d != sole[c]) { continue; }
                        local = Math.Max(0.0, (double)votes[d][c] / size[d] + (national - national2023[c]) * sum2023 / size[d]);
                    }
                    else
                    {
                        double part = others > 0.0 ? national / others * carry[d] : 0.0;
                        local = c >= 0 ? Math.Max(0.0, (double)votes[d][c] / size[d] + (national - national2023[c]) + part) : Math.Max(0.0, national + part);
                    }
                    if (local > 0.0) { result[d][k] = local * size[d]; }
                }
            }
            return result;
        }

        /// <summary>d'Hondt in one okreg: each seat to the highest quotient votes / (won + 1); a tie to the list with more votes, then the first in order.</summary>
        internal static Dictionary<string, int> DHondt(List<(string Key, double Votes)> lists, int magnitude)
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
