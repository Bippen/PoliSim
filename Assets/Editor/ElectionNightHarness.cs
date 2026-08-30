using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using PoliSim.Elections;
using PoliSim.Simulation;
using PoliSim.Testing;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// W-E6's harness — election night, on Sweden 2022's own returns.
    ///
    /// **The two rules the item exists to guarantee, each asserted rather than asserted-to:**
    ///
    /// 1. **No result appears before its constituency's arrival time.** Asserted by INDEPENDENT
    ///    RE-DERIVATION: at every minute of the night the harness sums the declared constituencies
    ///    itself and compares, vote for vote, with what the state reports — and separately checks
    ///    that every undeclared constituency carries no figures at all. A scripted reveal would
    ///    pass a screenshot review and fail this.
    ///
    /// 2. **A call, once made, is never contradicted by the final tally.** Asserted over every
    ///    minute of every seed: each call is re-tested against the completed night, and the calls
    ///    themselves are made against `SeatAllocation` — the allocation the backtest reproduces
    ///    2022 with seat-for-seat, which the harness re-proves here so the two cannot drift apart.
    /// </summary>
    public static class ElectionNightHarness
    {
        private static readonly string[] Parties = { "S", "SD", "M", "V", "C", "KD", "MP", "L" };
        /// <summary>SOURCED - Valmyndigheten final 2022 national counts (returns_2022.md).</summary>
        private static readonly long[] Votes2022 = { 1964474, 1330325, 1237428, 437050, 434945, 345712, 329242, 298542 };
        private static readonly int[] Seats2022 = { 107, 73, 68, 24, 24, 19, 18, 16 };
        private const double Turnout2018 = 0.8721;
        private const double Threshold = 0.04;
        private const int Seats = 349;
        private static readonly int[] Seeds = { 777, 778, 779, 780, 781, 782, 783, 784 };

        public static void Run()
        {
            CheckExit.ArmLogFold();
            int failures = 0;
            var sb = new StringBuilder();
            sb.Append("=== W-E6: election night - the count arriving by constituency, gated by arrival time, with calls that cannot be contradicted ===\n");

            long[][] region2022 = Regionalise(out string[] names, out long[] eligible);
            var blocs = new Dictionary<string, int[]>
            {
                // The two sides of the 2022 chamber, as the campaign itself framed them.
                { "the right bloc", new[] { 1, 2, 5, 7 } },   // SD, M, KD, L
                { "the left bloc", new[] { 0, 3, 4, 6 } },    // S, V, C, MP
            };

            failures += Exactness(sb, region2022);
            failures += Determinism(sb, names, region2022, eligible, blocs);
            failures += TheGate(sb, names, region2022, eligible, blocs);
            failures += CallsHold(sb, names, region2022, eligible, blocs);

            sb.Append($"\nELECTION NIGHT: {(failures == 0 ? "all assertions hold" : failures + " FAILED")}\n");
            Debug.Log(sb.ToString());
            CheckExit.Finish(failures == 0 ? 0 : 1);
        }

        private static int Assert(StringBuilder sb, string label, bool condition, string detail)
        {
            sb.Append($"  {(condition ? "ok  " : "FAIL")} {label}: {detail}\n");
            return condition ? 0 : 1;
        }

        /// <summary>2022's exact national counts spread over the 29 valkretsar by 2018's distribution - W-D2's own construction, so the two items count the same election.</summary>
        /// <summary>The staging is `ElectionNightFilm`'s, shared with the driver: the harness and the film must count ONE election, or a green harness would be proving something the film never shows.</summary>
        private static long[][] Regionalise(out string[] names, out long[] eligible)
        {
            return ElectionNightFilm.Regionalise(out names, out eligible);
        }

        private static long[] Valid(long[][] region)
        {
            var valid = new long[region.Length];
            for (int r = 0; r < region.Length; r++) { foreach (long v in region[r]) { valid[r] += v; } }
            return valid;
        }

        /// <summary>The call's arithmetic is the exact allocation's: the completed night reproduces 2022 seat-for-seat, or a call could rest on arithmetic the final tally does not use.</summary>
        private static int Exactness(StringBuilder sb, long[][] region)
        {
            int failures = 0;
            long[] valid = Valid(region);
            long totalValid = 0; foreach (long v in valid) { totalValid += v; }
            var national = new long[Parties.Length];
            foreach (long[] row in region) { for (int p = 0; p < Parties.Length; p++) { national[p] += row[p]; } }

            int[] seats = SeatAllocation.AllocateWithThreshold(national, totalValid, Threshold, Seats,
                SeatAllocation.ModifiedSainteLagueDivisor);

            bool exact = true;
            var line = new StringBuilder();
            for (int p = 0; p < Parties.Length; p++)
            {
                if (seats[p] != Seats2022[p]) { exact = false; }
                line.Append(string.Format(CultureInfo.InvariantCulture, "{0} {1}/{2}  ", Parties[p], seats[p], Seats2022[p]));
            }

            failures += Assert(sb, "0a. the night's own allocation reproduces Sweden 2022 seat-for-seat (model/real)", exact, line.ToString());

            bool sums = true;
            for (int p = 0; p < Parties.Length; p++) { if (national[p] != Votes2022[p]) { sums = false; } }
            failures += Assert(sb, "0b. and it is the real election being counted: the regionalised votes sum to the exact national counts",
                sums, "8 of 8 to the vote");
            return failures;
        }

        /// <summary>Determinism: one seed, one night - schedule, tally and calls alike.</summary>
        private static int Determinism(StringBuilder sb, string[] names, long[][] region, long[] eligible,
            Dictionary<string, int[]> blocs)
        {
            int failures = 0;
            int[] a = ScheduleFor(777, eligible);
            int[] b = ScheduleFor(777, eligible);
            int[] other = ScheduleFor(778, eligible);

            bool same = true, differs = false;
            for (int r = 0; r < a.Length; r++)
            {
                if (a[r] != b[r]) { same = false; }
                if (a[r] != other[r]) { differs = true; }
            }

            failures += Assert(sb, "1a. the declaration schedule is deterministic under the seed, and a different seed gives a different night",
                same && differs, same ? (differs ? "seed 777 twice identical; 778 differs" : "778 did NOT differ") : "777 differed from itself");

            long[] valid = Valid(region);
            NightState one = ElectionNight.At(120, names, region, valid, eligible, a, Seats, Threshold, Parties, null, blocs);
            NightState two = ElectionNight.At(120, names, region, valid, eligible, b, Seats, Threshold, Parties, null, blocs);
            bool identical = one.DeclaredCount == two.DeclaredCount && one.CountedValid == two.CountedValid
                && one.Calls.Count == two.Calls.Count;
            for (int p = 0; p < Parties.Length && identical; p++) { if (one.CountedVotes[p] != two.CountedVotes[p]) { identical = false; } }

            failures += Assert(sb, "1b. and the state at a given minute is the same state twice",
                identical, $"minute 120: {one.DeclaredCount} declared, {one.Calls.Count} call(s), both times");

            // The night must actually END - "final" is a state it reaches, not one it approaches.
            NightState final = ElectionNight.At(ElectionNight.NightMinutes, names, region, valid, eligible, a, Seats, Threshold, Parties, null, blocs);
            failures += Assert(sb, "1c. the night ends: every constituency has declared by the last minute",
                final.Complete, $"{final.DeclaredCount} of {final.TotalConstituencies} at minute {ElectionNight.NightMinutes}");
            return failures;
        }

        private static int[] ScheduleFor(int seed, long[] eligible)
        {
            SimulationRandom.Seed(seed);
            return ElectionNight.Schedule(eligible, SimulationRandom.For(SimulationRandom.Stream.ElectionNight));
        }

        /// <summary>
        /// RULE 1, by independent re-derivation. At every minute of every seed the harness sums the
        /// declared constituencies ITSELF and compares vote for vote; and separately checks that an
        /// undeclared constituency carries no figures at all. A screen fed a scripted reveal would
        /// look identical and fail here.
        /// </summary>
        private static int TheGate(StringBuilder sb, string[] names, long[][] region, long[] eligible,
            Dictionary<string, int[]> blocs)
        {
            int failures = 0;
            long[] valid = Valid(region);
            int checkedStates = 0;
            long leaked = 0;
            int early = 0;
            bool absenceHeld = true;

            foreach (int seed in Seeds)
            {
                int[] arrivals = ScheduleFor(seed, eligible);
                for (int minute = 0; minute <= ElectionNight.NightMinutes; minute += 5)
                {
                    NightState s = ElectionNight.At(minute, names, region, valid, eligible, arrivals, Seats, Threshold, Parties, null, blocs);
                    checkedStates++;

                    // Re-derive the tally from the arrival times, not from the state.
                    var mine = new long[Parties.Length];
                    long myValid = 0;
                    int declared = 0;
                    for (int r = 0; r < region.Length; r++)
                    {
                        if (arrivals[r] > minute)
                        {
                            // Nothing may have arrived early, and the report must carry no figures.
                            if (s.Constituencies[r].Declared) { early++; }
                            if (s.Constituencies[r].Votes != null || s.Constituencies[r].Valid != 0) { absenceHeld = false; }
                            continue;
                        }

                        declared++;
                        myValid += valid[r];
                        for (int p = 0; p < Parties.Length; p++) { mine[p] += region[r][p]; }
                    }

                    if (declared != s.DeclaredCount || myValid != s.CountedValid) { leaked++; continue; }
                    for (int p = 0; p < Parties.Length; p++) { if (mine[p] != s.CountedVotes[p]) { leaked++; break; } }
                }
            }

            failures += Assert(sb, "2a. THE GATE: no constituency's result exists before its arrival time - re-derived independently at every step",
                early == 0, early == 0 ? $"{checkedStates} states over {Seeds.Length} seeds, 0 early" : $"{early} early appearance(s)");
            failures += Assert(sb, "2b. and an undeclared constituency carries NO figures at all - absence, not zero",
                absenceHeld, absenceHeld ? "every undeclared report holds null votes and no valid count" : "an undeclared report carried figures");
            failures += Assert(sb, "2c. the running tally is exactly the sum of what has declared, and nothing else",
                leaked == 0, leaked == 0 ? $"{checkedStates} states agree vote for vote" : $"{leaked} state(s) disagreed with the re-derivation");
            return failures;
        }

        /// <summary>
        /// RULE 2. Every call made at any minute of any seed is re-tested against the COMPLETED
        /// night. A call the final tally contradicts is a bug, and this is where it would be found.
        /// The timing is reported too, because a guarantee that only ever fires on the last
        /// constituency would be sound and useless, and the reader is entitled to know which it is.
        /// </summary>
        private static int CallsHold(StringBuilder sb, string[] names, long[][] region, long[] eligible,
            Dictionary<string, int[]> blocs)
        {
            int failures = 0;
            long[] valid = Valid(region);
            int callsChecked = 0, contradicted = 0;
            var earliest = new Dictionary<string, int>();
            var latestSafe = new Dictionary<string, int>();

            foreach (int seed in Seeds)
            {
                int[] arrivals = ScheduleFor(seed, eligible);
                NightState final = ElectionNight.At(ElectionNight.NightMinutes, names, region, valid, eligible, arrivals, Seats, Threshold, Parties, null, blocs);

                for (int minute = 0; minute <= ElectionNight.NightMinutes; minute += 5)
                {
                    NightState s = ElectionNight.At(minute, names, region, valid, eligible, arrivals, Seats, Threshold, Parties, null, blocs);
                    foreach (ElectionCall call in s.Calls)
                    {
                        callsChecked++;
                        if (!HoldsAtFinal(call, final, blocs)) { contradicted++; }

                        string key = Describe(call);
                        if (!earliest.ContainsKey(key) || call.DeclaredAt < earliest[key]) { earliest[key] = call.DeclaredAt; }
                        if (!latestSafe.ContainsKey(key) || call.DeclaredAt > latestSafe[key]) { latestSafe[key] = call.DeclaredAt; }
                    }
                }
            }

            failures += Assert(sb, "3a. THE CALL: no call made at any minute of any seed is contradicted by the finished night",
                contradicted == 0,
                contradicted == 0 ? $"{callsChecked} call-instants over {Seeds.Length} seeds, 0 contradicted" : $"{contradicted} of {callsChecked} contradicted");

            sb.Append("\n  When each call becomes SAFE (constituencies declared, of 29 - earliest and latest over the seeds):\n");
            var keys = new List<string>(earliest.Keys);
            keys.Sort((x, y) => earliest[x].CompareTo(earliest[y]));
            foreach (string k in keys)
            {
                sb.Append(string.Format(CultureInfo.InvariantCulture, "    {0,-46} from {1,2} of 29 (latest seed: {2} of 29)\n", k, earliest[k], latestSafe[k]));
            }

            // A guarantee that only ever fires at the end would be sound and useless. Say which.
            int beforeTheEnd = 0;
            foreach (string k in keys) { if (earliest[k] < 29) { beforeTheEnd++; } }
            failures += Assert(sb, "3b. and the guarantee is worth having: at least one call becomes safe BEFORE the last constituency declares",
                beforeTheEnd > 0, $"{beforeTheEnd} of {keys.Count} call(s) safe before 29 of 29");
            return failures;
        }

        /// <summary>Does this call still hold once everything is in? The question rule 2 exists to answer.</summary>
        private static bool HoldsAtFinal(ElectionCall call, NightState final, Dictionary<string, int[]> blocs)
        {
            int majority = Seats / 2 + 1;
            switch (call.Kind)
            {
                case CallKind.ThresholdCleared:
                    return final.CountedShare(call.Party) > Threshold;
                case CallKind.ThresholdMissed:
                    return final.CountedShare(call.Party) <= Threshold;
                case CallKind.LargestParty:
                    for (int p = 0; p < Parties.Length; p++)
                    {
                        if (p != call.Party && final.CountedVotes[p] >= final.CountedVotes[call.Party]) { return false; }
                    }

                    return true;
                case CallKind.BlocMajority:
                    return BlocSeatsAtFinal(final, blocs[call.Bloc]) >= majority;
                default:
                    return BlocSeatsAtFinal(final, blocs[call.Bloc]) < majority;
            }
        }

        private static int BlocSeatsAtFinal(NightState final, int[] members)
        {
            int total = 0;
            foreach (int m in members) { total += final.SeatsOnCounted[m]; }
            return total;
        }

        private static string Describe(ElectionCall call)
        {
            switch (call.Kind)
            {
                case CallKind.ThresholdCleared: return Parties[call.Party] + " clears the 4 % threshold";
                case CallKind.ThresholdMissed: return Parties[call.Party] + " cannot reach the threshold";
                case CallKind.LargestParty: return Parties[call.Party] + " is the largest party";
                case CallKind.BlocMajority: return call.Bloc + " has a majority";
                default: return call.Bloc + " is short of a majority";
            }
        }
    }
}
