using System;
using System.Collections.Generic;
using PoliSim.Elections.Generated;

namespace PoliSim.Elections
{
    /// <summary>
    /// D-PL (Elias, 2026-10-23: "Build the Polish election night"; Design's boards 28a-28b): <b>POLAND'S SEJM NIGHT AS A MODEL</b> - the 41 okregi
    /// declaring one by one, each list called against its own line, and the seats each declared okreg returns that no open call can change.
    ///
    /// <para><b>The votes are the chamber's own.</b> An okreg's count is the spread `PolishSejmAllocation.Allocate` seats the Sejm from - the game's
    /// national share moved onto 2023's district result by a uniform swing, floored at zero, a list with no 2023 committee at its national share in
    /// every okreg - computed here in the same expressions and the same order, so a declared okreg's seats are the d'Hondt of the votes the chamber
    /// was counted on. `PolishNightDiagnostic` holds the night's count at 41 of 41 to the chamber's, okreg by okreg.</para>
    ///
    /// <para><b>The call</b> (28b's answer 2, "IN once counted votes ≥ line × all votes cast; turnout is not modelled, so the total is known"): a list
    /// is IN once its counted votes reach its line (`PolishSejmAllocation.LineOf`: 5 % a party, 8 % a coalition) of the national vote, OUT once
    /// its counted votes and every vote still out together cannot; the minority's list is exempt (art. 197 § 1) and called against nothing. Both
    /// bounds only tighten as okregi declare, so a call never reverses. The line reads the national vote, as art. 196 does: where the zero floor
    /// lifted a list in an okreg, the lift counts in that okreg's seats and never toward its line (`PolishSejmNight.Residue`), so at 41 of 41 the night calls
    /// exactly the lists the chamber admitted. DECLARED (D-PL): the total, and each list's residue, are known before the first okreg declares - the
    /// game models no turnout, and the night reveals a count the model has already made.</para>
    ///
    /// <para><b>Certain seats</b> (28b's answer 2, Code's item 1): a list's seat in a declared okreg is certain where it holds it however the open calls
    /// land (the minimum over the outcomes, which d'Hondt makes a closed form - see `At`); the rest of the okreg's magnitude is IN PLAY until the calls
    /// land. An okreg not yet declared contributes nothing and is NOT YET, never zero.</para>
    ///
    /// <para><b>The order of declaration is a presentation choice, not a forecast</b> - the build's rule (`ElectionNightFromModel`'s): the okreg with
    /// fewer votes counted in 2023 declares sooner, ties by number, spread evenly across the night so the last lands on the final minute.</para>
    /// </summary>
    public static class PolishSejmNight
    {
        /// <summary>Where a list stands against its line at an instant of the night.</summary>
        public enum Call { Open, In, Out, Exempt }

        /// <summary>One okreg at an instant: declared or not, and - only when declared - its votes and the seats certain under the open calls.</summary>
        public sealed class Okreg
        {
            /// <summary>The PKW's number, 1 to 41.</summary>
            public int Number;
            public int Magnitude;
            public bool Declared;
            /// <summary>The okreg's place in the declaration order, 1 to 41.</summary>
            public int Rank;
            /// <summary>Votes per key (the night's key order) - the count the okreg will return; a screen draws them only once it is `Declared` (where a list stands is read from them before).</summary>
            public double[] Votes;
            /// <summary>Seats per key certain under every outcome of the open calls. Only meaningful when `Declared`.</summary>
            public int[] Certain;
            /// <summary>The magnitude less the certain seats: the seats waiting on an open call.</summary>
            public int InPlay;
        }

        /// <summary>The night at one instant.</summary>
        public sealed class State
        {
            public int Minute;
            public string[] Keys;
            public Okreg[] Okregi;
            public int Declared;
            public int Total;
            /// <summary>Votes per key over the declared okregi only.</summary>
            public double[] Counted;
            public double CountedValid;
            /// <summary>Every vote the night will count - known before the first okreg declares (turnout is not modelled).</summary>
            public double AllValid;
            /// <summary>The base the national shares are read on - 2023's committees' own sum, the okregi's sizes summed.</summary>
            public double Base;
            /// <summary>Each key's national share (the chamber's, as given).</summary>
            public double[] National;
            /// <summary>Each key's ZERO-FLOOR RESIDUE: the votes the okregi carry for it beyond its national share times the base, where the floor at zero
            /// lifted an okreg the swing took below nothing. Zero unless the floor bit; never negative.</summary>
            public double[] Residue;
            /// <summary>Each key's line (a fraction of all votes); 0 for an exempt list.</summary>
            public double[] Line;
            public Call[] Calls;
            /// <summary>The declared count at which each key's call landed; 0 while open (and for an exempt list).</summary>
            public int[] CalledAt;
            /// <summary>Seats per key certain over the declared okregi.</summary>
            public int[] Seats;
            /// <summary>Seats waiting on an open call, over the declared okregi.</summary>
            public int InPlay;
            /// <summary>The magnitudes of the okregi not yet declared.</summary>
            public int NotYet;

            public bool Complete => Declared == Total;

            /// <summary>What the line reads for a key so far: its counted votes less its zero-floor residue, over the base - a floor that only grows,
            /// reaching the key's national share at 41 of 41 (the share the law's line reads; see `At`). Never below zero.</summary>
            public double LineShare(int key) => Base > 0.0 ? Math.Max(0.0, (Counted[key] - Residue[key]) / Base) : 0.0;

            /// <summary>The seats a key's row may print: a list whose call has landed, or an exempt list once every okreg it stands in has declared.</summary>
            public bool SeatsKnown(int key) => Calls[key] == Call.In || Calls[key] == Call.Out || (Calls[key] == Call.Exempt && StandsDeclared(key));

            /// <summary>Whether every okreg where the key has a vote has declared.</summary>
            public bool StandsDeclared(int key)
            {
                foreach (Okreg o in Okregi) { if (!o.Declared && o.Votes != null && o.Votes[key] > 0.0) { return false; } }
                return true;
            }
        }

        /// <summary>The last minute of the night - every okreg has declared by here (`ElectionNightFromModel.FinalMinute`'s, so the masthead's clock is one rule).</summary>
        public const int FinalMinute = ElectionNightFromModel.FinalMinute;

        /// <summary>[AUTHORED] How far past a line a call made before the last okreg must stand, as a share - a rounding's width, so a list sitting on its
        /// line is called by the chamber's own test at 41 of 41 and never by a sum that met it from one side.</summary>
        private const double LineMargin = 1e-9;

        /// <summary>2023's national shares on the six committees' own sum - the base the districts' shares are read on (`PolishSejmAllocation`): the
        /// record's own vote, which the night divides into the PKW's chamber.</summary>
        public static Dictionary<string, double> Record2023Shares()
        {
            string[] committees = PolishSejmDistricts2023.Committees;
            var sums = new double[committees.Length];
            double total = 0.0;
            foreach (long[] row in PolishSejmDistricts2023.Votes) { for (int c = 0; c < committees.Length; c++) { sums[c] += row[c]; total += row[c]; } }
            var shares = new Dictionary<string, double>();
            for (int c = 0; c < committees.Length; c++) { shares[committees[c]] = sums[c] / total; }
            return shares;
        }

        /// <summary>The okregi's numbers in the order they declare: fewer 2023 votes first, ties by number (see the class).</summary>
        public static int[] DeclarationOrder()
        {
            long[][] votes = PolishSejmDistricts2023.Votes;
            int n = votes.Length;
            var size = new long[n];
            var order = new int[n];
            for (int d = 0; d < n; d++) { foreach (long v in votes[d]) { size[d] += v; } order[d] = d; }
            Array.Sort(order, (a, b) => size[a] != size[b] ? size[a].CompareTo(size[b]) : a.CompareTo(b));
            var numbers = new int[n];
            for (int i = 0; i < n; i++) { numbers[i] = order[i] + 1; }
            return numbers;
        }

        /// <summary>The minute the okreg in place <paramref name="rank"/> (1-based) declares - spread evenly, the last on `FinalMinute`.</summary>
        public static int ArrivalMinute(int rank, int total) => total <= 1 ? FinalMinute : (int)Math.Round(FinalMinute * (double)rank / total);

        /// <summary>The night's keys in its fixed order (28b's answer 5, never re-sorted mid-count): 2023's committees in their columns' order, then any
        /// other key the shares carry above zero (a created party), in the shares' own order.</summary>
        public static string[] KeysFor(IReadOnlyDictionary<string, double> shares)
        {
            var keys = new List<string>(PolishSejmDistricts2023.Committees);
            if (shares != null)
            {
                foreach (KeyValuePair<string, double> kv in shares) { if (!keys.Contains(kv.Key) && kv.Value > 0.0) { keys.Add(kv.Key); } }
            }
            return keys.ToArray();
        }

        /// <summary>
        /// Each okreg's votes per key (rows okreg 1 to 41, columns <paramref name="keys"/>): the uniform swing `PolishSejmAllocation.Allocate` divides,
        /// in its expressions and its order - 2023's national shares on the committees' own sum, the okreg's 2023 share moved by the national change and
        /// floored at zero, times the okreg's 2023 size; a key with no 2023 committee at its national share. A key the shares do not carry has none.
        /// </summary>
        public static double[][] DistrictVotes(IReadOnlyList<string> keys, IReadOnlyDictionary<string, double> shares)
        {
            string[] committees = PolishSejmDistricts2023.Committees;
            long[][] votes = PolishSejmDistricts2023.Votes;
            int districts = votes.Length;
            var national2023 = new double[committees.Length];
            long sum2023 = 0;
            for (int d = 0; d < districts; d++) { for (int c = 0; c < committees.Length; c++) { national2023[c] += votes[d][c]; sum2023 += votes[d][c]; } }
            for (int c = 0; c < committees.Length; c++) { national2023[c] /= sum2023; }

            var result = new double[districts][];
            for (int d = 0; d < districts; d++)
            {
                long size = 0;
                for (int c = 0; c < committees.Length; c++) { size += votes[d][c]; }
                result[d] = new double[keys.Count];
                for (int k = 0; k < keys.Count; k++)
                {
                    if (shares == null || !shares.TryGetValue(keys[k], out double national)) { continue; }
                    int c = Array.IndexOf(committees, keys[k]);
                    double local = c >= 0 ? Math.Max(0.0, (double)votes[d][c] / size + (national - national2023[c])) : national;
                    if (local > 0.0) { result[d][k] = local * size; }
                }
            }
            return result;
        }

        /// <summary>
        /// The okreg's seats with <paramref name="admitted"/> lists in the division: d'Hondt (`PolishSejmAllocation.DHondt`) over the admitted keys with a
        /// vote there, in the ordinal order of their keys - the order `Allocate` divides in, so a tie breaks the same way.
        /// </summary>
        public static int[] DistrictSeats(int district, IReadOnlyList<string> keys, double[] votes, bool[] admitted)
        {
            var order = new List<int>();
            for (int k = 0; k < keys.Count; k++) { order.Add(k); }
            order.Sort((a, b) => string.CompareOrdinal(keys[a], keys[b]));
            var lists = new List<(string Key, double Votes)>();
            foreach (int k in order) { if (admitted[k] && votes[k] > 0.0) { lists.Add((keys[k], votes[k])); } }
            Dictionary<string, int> won = PolishSejmAllocation.DHondt(lists, PolishSejmDistricts2023.Magnitudes[district]);
            var seats = new int[keys.Count];
            for (int k = 0; k < keys.Count; k++) { if (won.TryGetValue(keys[k], out int s)) { seats[k] = s; } }
            return seats;
        }

        /// <summary>The night at <paramref name="minute"/> on the national <paramref name="shares"/> (fractions of the valid vote, keyed by the roster's
        /// abbreviations - an election record's). Null where no shares are given.</summary>
        public static State At(int minute, IReadOnlyDictionary<string, double> shares)
        {
            if (shares == null || shares.Count == 0) { return null; }
            string[] keys = KeysFor(shares);
            double[][] votes = DistrictVotes(keys, shares);
            int districts = votes.Length;
            int[] order = DeclarationOrder();
            var rank = new int[districts];
            for (int i = 0; i < order.Length; i++) { rank[order[i] - 1] = i + 1; }

            var state = new State
            {
                Minute = minute, Keys = keys, Total = districts, Okregi = new Okreg[districts],
                Counted = new double[keys.Length], Line = new double[keys.Length], Calls = new Call[keys.Length], CalledAt = new int[keys.Length], Seats = new int[keys.Length],
            };
            for (int k = 0; k < keys.Length; k++) { state.Line[k] = PolishSejmAllocation.LineOf(keys[k]); }
            for (int d = 0; d < districts; d++) { foreach (double v in votes[d]) { state.AllValid += v; } }
            foreach (long[] row in PolishSejmDistricts2023.Votes) { foreach (long v in row) { state.Base += v; } }
            state.National = new double[keys.Length];
            state.Residue = new double[keys.Length];
            for (int k = 0; k < keys.Length; k++)
            {
                state.National[k] = shares.TryGetValue(keys[k], out double national) ? national : 0.0;
                double carried = 0.0;
                for (int d = 0; d < districts; d++) { carried += votes[d][k]; }
                state.Residue[k] = Math.Max(0.0, carried - state.National[k] * state.Base);
            }

            // the calls, walked in declaration order to the declared count - each stamped where it landed. ⚠ The line reads the NATIONAL vote (art. 196
            // counts a committee's votes nationwide), and the okregi's floored votes can sum past it: where the swing takes a list below nothing in an
            // okreg, the floor lifts it to zero there, and that lift is the list's residue - counted in the okreg's seats, never toward its line. Without
            // the floor a list's counted votes less its residue ARE its national share times the base at 41 of 41, so the night calls what the chamber
            // seats; at 41 of 41 the call is the chamber's own test on the national share, so a rounding at the line cannot part them.
            for (int k = 0; k < keys.Length; k++) { state.Calls[k] = state.Line[k] <= 0.0 ? Call.Exempt : Call.Open; }
            var running = new double[keys.Length];
            double runningValid = 0.0;
            for (int i = 0; i < order.Length; i++)
            {
                int d = order[i] - 1;
                if (ArrivalMinute(i + 1, districts) > minute) { break; }
                state.Declared++;
                for (int k = 0; k < keys.Length; k++) { running[k] += votes[d][k]; }
                foreach (double v in votes[d]) { runningValid += v; }
                bool last = state.Declared == districts;
                double outstanding = state.AllValid - runningValid;
                for (int k = 0; k < keys.Length; k++)
                {
                    if (state.Calls[k] != Call.Open) { continue; }
                    double toward = (running[k] - state.Residue[k]) / state.Base;
                    bool inNow = last ? state.National[k] >= state.Line[k] : toward >= state.Line[k] + LineMargin;
                    bool outNow = last ? !inNow : toward + outstanding / state.Base < state.Line[k] - LineMargin;
                    if (inNow) { state.Calls[k] = Call.In; state.CalledAt[k] = state.Declared; }
                    else if (outNow) { state.Calls[k] = Call.Out; state.CalledAt[k] = state.Declared; }
                }
            }
            state.Counted = running;
            state.CountedValid = runningValid;

            // the seats certain under every outcome of the calls still open. d'Hondt in one okreg gives its seats to the highest quotients in one strict
            // order (the quotient, then the votes, then the list's order), so taking a list out of the division only takes its quotients out of the pool:
            // no other list's seats can fall. The least a called list holds over every way the open calls can land is therefore what it holds with every
            // open list admitted, and an open list's least is nothing (it may yet be out). `PolishNightDiagnostic` holds this closed form to the minimum
            // over every outcome, counted out in full.
            for (int d = 0; d < districts; d++)
            {
                var okreg = new Okreg { Number = d + 1, Magnitude = PolishSejmDistricts2023.Magnitudes[d], Rank = rank[d], Declared = ArrivalMinute(rank[d], districts) <= minute, Votes = votes[d] };
                state.Okregi[d] = okreg;
                if (!okreg.Declared) { state.NotYet += okreg.Magnitude; continue; }
                var admitted = new bool[keys.Length];
                for (int k = 0; k < keys.Length; k++) { admitted[k] = state.Calls[k] != Call.Out; }
                int[] seats = DistrictSeats(d, keys, votes[d], admitted);
                var certain = new int[keys.Length];
                for (int k = 0; k < keys.Length; k++) { certain[k] = state.Calls[k] == Call.Open ? 0 : seats[k]; }
                int held = 0;
                for (int k = 0; k < keys.Length; k++) { held += certain[k]; state.Seats[k] += certain[k]; }
                okreg.Certain = certain;
                okreg.InPlay = okreg.Magnitude - held;
                state.InPlay += okreg.InPlay;
            }
            return state;
        }
    }
}
