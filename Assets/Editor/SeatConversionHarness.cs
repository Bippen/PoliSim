using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using PoliSim.Elections;
using PoliSim.Simulation;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// W-D2's harness — §28's vote-to-seat conversion on the LIVE path.
    ///
    /// The done-when, asserted: **Sweden's 2022 returns reproduce seat-for-seat through the live
    /// path** — the REAL 2022 per-valkrets counts (SOURCED, W-F1; they sum to the published
    /// national counts party by party), fed as an
    /// `ElectionDay.Result` (σ = 0) into `SeatConversion.Sweden`: 107 / 73 / 68 / 24 / 24 / 19 / 18 /
    /// 16, 310 fixed + 39 adjustment = 349 — not the backtest's national shortcut, the full
    /// procedure. Plus: the 12 % rule on a synthetic (a party at 3 % nationally and 15 % in one
    /// valkrets takes fixed seats there and nothing else), återföring on a synthetic (a party
    /// concentrated in one valkrets wins more fixed seats than its total and gives them back;
    /// every party ends exactly at its total), determinism, and the live election of W-D1's
    /// staging converted.
    /// </summary>
    public static class SeatConversionHarness
    {
        private static readonly string[] Parties = { "S", "SD", "M", "V", "C", "KD", "MP", "L" };
        // 2022 exact national counts (returns_2022.md) and the real seats.
        private static readonly long[] Votes2022 = { 1964474, 1330325, 1237428, 437050, 434945, 345712, 329242, 298542 };
        private static readonly int[] Seats2022 = { 107, 73, 68, 24, 24, 19, 18, 16 };
        // W-F1: Turnout2018 retired - eligible is now SOURCED per valkrets (valkrets_votes_2022.csv column 11).

        public static void Run()
        {
            CheckExit.ArmLogFold();
            int failures = 0;
            var sb = new StringBuilder();
            sb.Append("=== W-D2: vote-to-seat on the live path (§28), Sweden's own procedure ===\n");

            // W-F1: the REAL 2022 per-valkrets counts, from Valmyndigheten's own per-constituency
            // backend. Until W-F1 this harness regionalised the 2022 NATIONAL counts by 2018's
            // per-valkrets distribution, so "reproduces seat-for-seat" was a claim about a
            // synthetic chamber that happened to sum right. It is now a claim about the election
            // that actually happened, and the 12 % rule and the adjustment placement are being
            // asked a real question rather than a smoothed one.
            long[][] region2022 = ReadValkrets(out string[] names, out double[] eligible);
            int regions = names.Length;
            int parties = Parties.Length;

            bool sumsExact = true;
            for (int p = 0; p < parties; p++)
            {
                long s = 0;
                for (int r = 0; r < regions; r++) { s += region2022[r][p]; }
                if (s != Votes2022[p]) { sumsExact = false; }
            }

            failures += Assert(sb, "0. the per-valkrets 2022 file sums to the published national counts, party by party", sumsExact, "8 of 8 to the vote");

            // ---------- the live path: an ElectionDay.Result (sigma 0) in, seats out ----------
            var regionResults = new RegionalAggregation.RegionResult[regions];
            for (int r = 0; r < regions; r++)
            {
                var v = new double[parties];
                double cast = 0;
                for (int p = 0; p < parties; p++) { v[p] = region2022[r][p]; cast += v[p]; }
                regionResults[r] = new RegionalAggregation.RegionResult(names[r], v, eligible[r], cast);
            }

            var count = new ElectionDay.Result { Regions = regionResults };
            SeatConversion.Result seats = SeatConversion.Sweden(count);

            sb.Append("\n  Sweden 2022 through the live path (fixed per valkrets + totalfördelning + återföring + adjustment):\n  party   real  live  fixed  adj\n");
            bool exact = true;
            int fixedSum = 0, adjSum = 0;
            for (int p = 0; p < parties; p++)
            {
                sb.Append(string.Format(CultureInfo.InvariantCulture, "  {0,-6} {1,5} {2,5} {3,6} {4,4}\n", Parties[p], Seats2022[p], seats.Seats[p], seats.FixedSeatsWon[p], seats.AdjustmentSeats[p]));
                if (seats.Seats[p] != Seats2022[p]) { exact = false; }
                fixedSum += seats.FixedSeatsWon[p]; adjSum += seats.AdjustmentSeats[p];
            }

            failures += Assert(sb, "1a. Sweden 2022 reproduces SEAT-FOR-SEAT through the live path (107/73/68/24/24/19/18/16)", exact, exact ? "8 of 8 exact" : "deviation - see the table");
            failures += Assert(sb, "1b. 310 fixed seats + 39 adjustment seats = 349", fixedSum == 310 && adjSum == 39 && Sum(seats.Seats) == 349, $"{fixedSum} + {adjSum} = {Sum(seats.Seats)}");
            failures += Assert(sb, "1c. no seat was returned in 2022 (no party's fixed seats exceeded its total)", seats.SeatsReturned == 0, $"{seats.SeatsReturned} returned");

            int fixedPerRegionSum = 0;
            bool regionSeatsConsistent = true;
            for (int r = 0; r < regions; r++)
            {
                fixedPerRegionSum += seats.FixedSeatsPerRegion[r];
                int inRegion = 0;
                for (int p = 0; p < parties; p++) { inRegion += seats.RegionSeats[r][p]; }
                if (inRegion < seats.FixedSeatsPerRegion[r]) { regionSeatsConsistent = false; }
            }

            failures += Assert(sb, "1d. the fixed seats per valkrets (DERIVED from eligible by the 310th-part rule) sum to 310 and every valkrets holds at least its fixed seats", fixedPerRegionSum == 310 && regionSeatsConsistent, $"{fixedPerRegionSum}; Stockholms län {seats.FixedSeatsPerRegion[Array.IndexOf(names, "Stockholms län")]} fixed, Gotlands län {seats.FixedSeatsPerRegion[Array.IndexOf(names, "Gotlands län")]}");

            // ---------- determinism ----------
            SeatConversion.Result again = SeatConversion.Sweden(count);
            bool same = true;
            for (int r = 0; r < regions; r++) { for (int p = 0; p < parties; p++) { if (again.RegionSeats[r][p] != seats.RegionSeats[r][p]) { same = false; } } }
            failures += Assert(sb, "2. the conversion is deterministic (no draw anywhere; the same count gives the same seat table)", same, "29 x 8 identical");

            // ---------- the 12 % rule ----------
            var synthetic = Clone(region2022);
            int gotland = Array.IndexOf(names, "Gotlands län");
            // Give L (4.61 %) nothing anywhere but Gotland, where it takes 35 % (enough for a fixed seat among Gotland's two): nationally under 4 %, regionally over 12 %.
            for (int r = 0; r < regions; r++) { synthetic[r][7] = 0; }
            long gotlandValid = 0;
            for (int p = 0; p < parties; p++) { gotlandValid += synthetic[gotland][p]; }
            synthetic[gotland][7] = (long)(0.35 / 0.65 * gotlandValid) + 1;
            SeatConversion.Result twelve = SeatConversion.Sweden(synthetic, eligible);
            int lSeatsElsewhere = 0;
            for (int r = 0; r < regions; r++) { if (r != gotland) { lSeatsElsewhere += twelve.RegionSeats[r][7]; } }
            failures += Assert(sb, "3. the 12 % rule: a party under 4 % nationally but over 12 % in Gotland (35 %) takes fixed seats there and nothing anywhere else",
                !twelve.NationallyEligible[7] && twelve.RegionSeats[gotland][7] >= 1 && lSeatsElsewhere == 0 && twelve.AdjustmentSeats[7] == 0 && Sum(twelve.Seats) == 349,
                $"L: Gotland {twelve.RegionSeats[gotland][7]}, elsewhere {lSeatsElsewhere}, adjustment {twelve.AdjustmentSeats[7]}, total {Sum(twelve.Seats)}");

            // ---------- återföring ----------
            // Fixed seats follow ELIGIBLE voters, so a party concentrated in one valkrets normally
            // stays under its total (KD all in Stockholm: 12 fixed of 19 - a first draft of this test
            // exercised nothing). What makes fixed seats exceed a total is a valkrets where few OTHER
            // votes are cast relative to its eligible voters: cut every other party's Stockholm vote
            // by 70 % and put all of KD there - KD takes ~58 % of 39 fixed seats against a national
            // entitlement of ~19, and the excess must come back.
            var concentrated = Clone(region2022);
            int stockholm = Array.IndexOf(names, "Stockholms län");
            long kdTotal = 0;
            for (int r = 0; r < regions; r++) { kdTotal += concentrated[r][5]; concentrated[r][5] = 0; }
            for (int p = 0; p < parties; p++) { if (p != 5) { concentrated[stockholm][p] = concentrated[stockholm][p] * 3 / 10; } }
            concentrated[stockholm][5] = kdTotal;
            SeatConversion.Result back = SeatConversion.Sweden(concentrated, eligible);
            bool allAtTotal = true;
            int[] totalsCheck = SeatAllocation.AllocateWithThreshold(NationalVotes(concentrated), SumAll(concentrated), 0.04, 349, SeatAllocation.ModifiedSainteLagueDivisor);
            for (int p = 0; p < parties; p++) { if (back.Seats[p] != totalsCheck[p]) { allAtTotal = false; } }
            failures += Assert(sb, "4a. återföring FIRES: a party whose fixed seats exceed its national entitlement gives the excess back (seats returned > 0)",
                back.SeatsReturned > 0, $"KD in a low-turnout Stockholm: fixed won after return {back.FixedSeatsWon[5]}, total {back.Seats[5]}, seats returned {back.SeatsReturned}");
            failures += Assert(sb, "4b. after the return every party ends at exactly its national entitlement, 349 in all",
                allAtTotal && Sum(back.Seats) == 349, $"8 of 8 at their totals, {Sum(back.Seats)} seats");
            int stockholmSeats = 0;
            for (int p = 0; p < parties; p++) { stockholmSeats += back.RegionSeats[stockholm][p]; }
            failures += Assert(sb, "4c. the returned seats were re-allocated within the valkrets (Stockholm still holds at least its fixed seats)",
                stockholmSeats >= back.FixedSeatsPerRegion[stockholm], $"Stockholm {stockholmSeats} seats against {back.FixedSeatsPerRegion[stockholm]} fixed");

            // ---------- W-D1's staged election, converted ----------
            SimulationRandom.Seed(777);
            double[] prior = Normalised(new double[] { 0.3033, 0.2054, 0.1910, 0.0675, 0.0671, 0.0534, 0.0508, 0.0461 });
            var preference = new double[regions][];
            for (int r = 0; r < regions; r++) { preference[r] = prior; }
            var gotv = new RegionalMobilization(eligible, parties);
            ElectionDay.Result live = ElectionDay.Count(names, preference, gotv, 0.8421, 50, 50, 50, SimulationRandom.For(SimulationRandom.Stream.ElectionNoise));
            SeatConversion.Result liveSeats = SeatConversion.Sweden(live);
            sb.Append("\n  W-D1's staged election (seed 777, the 2022 vector everywhere, noise on): seats ");
            for (int p = 0; p < parties; p++) { sb.Append($"{Parties[p]} {liveSeats.Seats[p]}  "); }
            sb.Append($"= {Sum(liveSeats.Seats)}\n");
            failures += Assert(sb, "5. a counted election converts to 349 seats through the same path", Sum(liveSeats.Seats) == 349, $"{Sum(liveSeats.Seats)}");

            sb.Append($"\n=== SeatConversionHarness: {(failures == 0 ? "ALL ASSERTIONS PASS" : failures + " FAILED")} ===\n");
            Debug.Log(sb.ToString());
            CheckExit.Finish(failures == 0 ? 0 : 1);
        }

        private static long[][] ReadValkrets(out string[] names, out double[] eligible)
        {
            string path = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "ElectionsData", "sweden", "valkrets_votes_2022.csv"));
            var rows = new List<long[]>();
            var nameList = new List<string>();
            var eligibleList = new List<double>();
            // csv order: valkrets;valid;S;M;SD;C;V;KD;L;MP -> our order S, SD, M, V, C, KD, MP, L
            int[] map = { 2, 4, 3, 6, 5, 7, 9, 8 };
            foreach (string raw in File.ReadAllLines(path))
            {
                string line = raw.Trim();
                if (line.Length == 0 || line.StartsWith("#") || line.StartsWith("valkrets;")) { continue; }
                string[] cells = line.Split(';');
                nameList.Add(cells[0]);
                eligibleList.Add(double.Parse(cells[10], CultureInfo.InvariantCulture));   // W-F1: SOURCED antalRostberattigade
                var row = new long[8];
                for (int p = 0; p < 8; p++) { row[p] = long.Parse(cells[map[p]], CultureInfo.InvariantCulture); }
                rows.Add(row);
            }

            names = nameList.ToArray();
            eligible = eligibleList.ToArray();
            return rows.ToArray();
        }

        private static long[][] Clone(long[][] v) { var c = new long[v.Length][]; for (int i = 0; i < v.Length; i++) { c[i] = (long[])v[i].Clone(); } return c; }
        private static long[] NationalVotes(long[][] v) { var n = new long[v[0].Length]; foreach (long[] row in v) { for (int p = 0; p < n.Length; p++) { n[p] += row[p]; } } return n; }
        private static long SumAll(long[][] v) { long s = 0; foreach (long[] row in v) { foreach (long x in row) { s += x; } } return s; }
        private static int Sum(int[] v) { int s = 0; foreach (int x in v) { s += x; } return s; }
        private static double[] Normalised(double[] v) { double s = 0; foreach (double x in v) { s += x; } var r = new double[v.Length]; for (int i = 0; i < r.Length; i++) { r[i] = v[i] / s; } return r; }

        private static int Assert(StringBuilder sb, string label, bool condition, string detail)
        {
            sb.Append($"  {(condition ? "ok  " : "FAIL")} {label}: {detail}\n");
            return condition ? 0 : 1;
        }
    }
}
