using System;
using System.Globalization;
using System.Text;
using PoliSim.Elections;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// W-A4's harness — §23's tactical voting in its threshold form.
    ///
    /// The done-when, asserted:
    /// 1. **a party polling 3.5–4.5 % shows measurable support inflow** — the 2022 staging with L
    ///    set to each of 3.5 / 3.75 / 4.0 / 4.25 / 4.5 % gains at least 0.1 pp net from its bloc,
    ///    and a party polling clear (6 %) gains nothing; §23's own example — a party with no
    ///    realistic chance (1.5 %) — LOSES its aware voters to the bloc;
    /// 2. **the effect vanishes in the absence of a threshold** — threshold 0 returns the input
    ///    to the bit with no flows; so does awareness 0; a party outside any bloc is untouched;
    ///    mass is conserved; regions apply as the nation does;
    /// 3. **2022's near-threshold behaviour is reproduced no worse than without the layer** —
    ///    SCB's May 2022 PSU as the poll and as the preference, Valmyndigheten's September count
    ///    as the answer: the layer's error is no larger than the poll's own, on the three
    ///    near-threshold parties (KD, MP, L) and on the whole vector, and L and MP move toward
    ///    the count rather than away. 2018 (the KD case, with 2018's blocs) is measured and
    ///    reported, not asserted — the worklist names 2022.
    ///
    /// Staging: `ElectionsData/sweden/psu_2018_2022.md` (SOURCED), `returns_2022.md` and
    /// `priors/previous_elections.md` (SOURCED), CHES 2024 `lrgen` from `positions/party_positions.md`
    /// (SOURCED) as the affinity axis; the 2022 blocs (M/KD/L/SD against S/V/C/MP — the two
    /// 176–173 sides of the Riksdag that formed) and the 2018 blocs (the Alliance M/C/KD/L, the
    /// red-greens S/V/MP, SD outside) as the harness stages them; awareness 0.5 [AUTHORED-DRAFT].
    /// The eight parties are renormalised without "others" (2.0 % / 2.9 % in May; 1.5 % on the day).
    /// </summary>
    public static class TacticalVotingHarness
    {
        private static readonly string[] Parties = { "S", "SD", "M", "V", "C", "KD", "MP", "L" };
        private static readonly double[] Lrgen = { 3.74, 8.53, 7.58, 1.58, 5.95, 8.00, 2.74, 6.74 };
        private static readonly int[] Blocs2022 = { 0, 1, 1, 0, 0, 1, 0, 1 };
        private static readonly int[] Blocs2018 = { 0, -1, 1, 0, 1, 1, 0, 1 };

        // SCB PSU, "val idag" %, and ± (95 %): May 2022 and May 2018.
        private static readonly double[] Psu2022 = { 33.3, 17.0, 21.3, 7.8, 6.7, 5.2, 3.3, 3.4 };
        private static readonly double[] Moe2022 = { 1.3, 1.0, 1.1, 0.7, 0.7, 0.6, 0.5, 0.5 };
        private static readonly double[] Psu2018 = { 27.9, 18.8, 22.2, 7.4, 8.8, 3.0, 4.2, 4.9 };
        private static readonly double[] Moe2018 = { 1.1, 1.0, 1.1, 0.7, 0.7, 0.4, 0.5, 0.6 };
        // Valmyndigheten, final counts, %.
        private static readonly double[] Result2022 = { 30.33, 20.54, 19.10, 6.75, 6.71, 5.34, 5.08, 4.61 };
        private static readonly double[] Result2018 = { 28.26, 17.53, 19.84, 8.00, 8.61, 6.32, 4.41, 5.49 };

        private const double Threshold = 0.04;
        private const double Awareness = 0.5;   // [AUTHORED-DRAFT]
        private const int L = 7, MP = 6, KD = 5, M = 2, S = 0, SD = 1;

        public static void Run()
        {
            CheckExit.ArmLogFold();
            int failures = 0;
            var sb = new StringBuilder();
            sb.Append("=== W-A4: tactical voting, threshold form (§23) - the belief from the published poll, lending in play, abandonment when hopeless ===\n");

            var spec = new TacticalSpec(Threshold, Awareness, Blocs2022, Lrgen);
            double[] poll = Normalised(Psu2022);
            double[] moe = Moe2022;

            // ---------- 1. the window ----------
            sb.Append("\n  L set to each polled share (M absorbing the difference), 2022 staging, awareness 0.5:\n");
            bool windowHolds = true;
            foreach (double pct in new[] { 3.5, 3.75, 4.0, 4.25, 4.5 })
            {
                double[] v = WithParty(poll, L, pct / 100.0, M);
                TacticalResult r = TacticalVoting.Apply(v, v, moe, spec);
                double net = (r.Preference[L] - v[L]) * 100.0;
                sb.Append(string.Format(CultureInfo.InvariantCulture, "    L {0:F2} % -> P(clear) {1:F2}, inflow {2:F2} pp, outflow {3:F2} pp, net {4:+0.00;-0.00} pp -> {5:F2} %\n",
                    pct, r.ClearProbability[L], r.Inflow(L) * 100, r.Outflow(L) * 100, net, r.Preference[L] * 100));
                windowHolds &= net >= 0.1;
            }
            failures += Assert(sb, "1a. a party polling 3.5-4.5 % gains at least 0.1 pp net from its bloc at every point of the window", windowHolds, "see the sweep");

            {
                double[] v = WithParty(poll, L, 0.06, M);
                TacticalResult r = TacticalVoting.Apply(v, v, moe, spec);
                failures += Assert(sb, "1b. a party polling clear (6 %) gains nothing and loses nothing", Math.Abs(r.Preference[L] - v[L]) < 1e-12,
                    string.Format(CultureInfo.InvariantCulture, "P(clear) {0:F3}, net {1:+0.000;-0.000} pp", r.ClearProbability[L], (r.Preference[L] - v[L]) * 100));
            }

            {
                double[] v = WithParty(poll, L, 0.015, M);
                TacticalResult r = TacticalVoting.Apply(v, v, moe, spec);
                double net = (r.Preference[L] - v[L]) * 100.0;
                failures += Assert(sb, "1c. section 23's example: a party with no realistic chance (1.5 %) loses aware voters to its bloc", net < -0.1 && r.Outflow(L) > r.Inflow(L),
                    string.Format(CultureInfo.InvariantCulture, "P(clear) {0:F3}, inflow {1:F2} pp, outflow {2:F2} pp, net {3:+0.00;-0.00} pp; the receivers: {4}",
                        r.ClearProbability[L], r.Inflow(L) * 100, r.Outflow(L) * 100, net, Receivers(r, L)));
            }

            // ---------- 2. vanishing, conservation, blocs, regions ----------
            {
                TacticalResult none = TacticalVoting.Apply(poll, poll, moe, new TacticalSpec(0.0, Awareness, Blocs2022, Lrgen));
                bool identical = true;
                for (int p = 0; p < poll.Length; p++) { identical &= none.Preference[p] == poll[p]; }
                failures += Assert(sb, "2a. no threshold -> the identity to the bit, no flows", identical && none.Flows.Length == 0, $"{none.Flows.Length} flows");

                TacticalResult unaware = TacticalVoting.Apply(poll, poll, moe, new TacticalSpec(Threshold, 0.0, Blocs2022, Lrgen));
                identical = true;
                for (int p = 0; p < poll.Length; p++) { identical &= unaware.Preference[p] == poll[p]; }
                failures += Assert(sb, "2b. awareness 0 -> the identity, no flows", identical && unaware.Flows.Length == 0, $"{unaware.Flows.Length} flows");

                TacticalResult with = TacticalVoting.Apply(poll, poll, moe, spec);
                failures += Assert(sb, "2c. mass is conserved", Math.Abs(Sum(with.Preference) - Sum(poll)) < 1e-12,
                    string.Format(CultureInfo.InvariantCulture, "sum {0:F15} before, {1:F15} after, {2} flows", Sum(poll), Sum(with.Preference), with.Flows.Length));

                double[] poll18 = Normalised(Psu2018);
                TacticalResult alone = TacticalVoting.Apply(poll18, poll18, Moe2018, new TacticalSpec(Threshold, Awareness, Blocs2018, Lrgen));
                failures += Assert(sb, "2d. a party outside any bloc (SD in 2018) neither lends nor receives", alone.Preference[SD] == poll18[SD] && alone.Inflow(SD) == 0 && alone.Outflow(SD) == 0,
                    string.Format(CultureInfo.InvariantCulture, "SD {0:F4} before and after", poll18[SD] * 100));

                var regions = new double[29][];
                for (int r = 0; r < regions.Length; r++) { regions[r] = poll; }
                double[][] shifted = TacticalVoting.ApplyToRegions(regions, poll, moe, spec);
                bool regionsAgree = true;
                for (int r = 0; r < regions.Length; r++)
                {
                    for (int p = 0; p < poll.Length; p++) { regionsAgree &= shifted[r][p] == with.Preference[p]; }
                    regionsAgree &= regions[r] == poll;
                }
                failures += Assert(sb, "2e. regions apply as the nation does, reading the one national poll; inputs untouched", regionsAgree, "29 regions");
            }

            // ---------- 3. 2022 ----------
            {
                double[] answer = Normalised(Result2022);
                TacticalResult with = TacticalVoting.Apply(poll, poll, moe, spec);
                sb.Append("\n  2022 - SCB PSU May as poll and preference, the September count as the answer (eight parties renormalised):\n");
                sb.Append("    party  polled  layer   count  | err.poll err.layer  P(clear)\n");
                double l1Before = 0, l1After = 0, nearBefore = 0, nearAfter = 0;
                for (int p = 0; p < poll.Length; p++)
                {
                    double eb = Math.Abs(poll[p] - answer[p]) * 100, ea = Math.Abs(with.Preference[p] - answer[p]) * 100;
                    l1Before += eb;
                    l1After += ea;
                    if (p == KD || p == MP || p == L) { nearBefore += eb; nearAfter += ea; }
                    sb.Append(string.Format(CultureInfo.InvariantCulture, "    {0,-5} {1,6:F2}  {2,6:F2}  {3,6:F2}  | {4,7:F2} {5,8:F2}  {6,6:F3}\n",
                        Parties[p], poll[p] * 100, with.Preference[p] * 100, answer[p] * 100, eb, ea, with.ClearProbability[p]));
                }

                sb.Append(string.Format(CultureInfo.InvariantCulture, "    L1 error over eight: poll {0:F2} pp, with the layer {1:F2} pp; near-threshold (KD, MP, L): poll {2:F2}, layer {3:F2}\n",
                    l1Before, l1After, nearBefore, nearAfter));
                foreach (TacticalFlow f in with.Flows)
                {
                    sb.Append(string.Format(CultureInfo.InvariantCulture, "    {0} {1} -> {2}: {3:F2} pp\n", f.Rescue ? "lend" : "leave", Parties[f.From], Parties[f.To], f.Share * 100));
                }

                failures += Assert(sb, "3a. near-threshold error (KD, MP, L) no larger with the layer than the poll's own", nearAfter <= nearBefore + 1e-9,
                    string.Format(CultureInfo.InvariantCulture, "{0:F2} -> {1:F2} pp", nearBefore, nearAfter));
                failures += Assert(sb, "3b. whole-vector L1 error no larger with the layer", l1After <= l1Before + 1e-9,
                    string.Format(CultureInfo.InvariantCulture, "{0:F2} -> {1:F2} pp", l1Before, l1After));
                failures += Assert(sb, "3c. L and MP move toward the count, not away", with.Preference[L] > poll[L] && with.Preference[L] <= answer[L] + 0.005
                    && with.Preference[MP] > poll[MP] && with.Preference[MP] <= answer[MP] + 0.005,
                    string.Format(CultureInfo.InvariantCulture, "L {0:F2} -> {1:F2} (count {2:F2}); MP {3:F2} -> {4:F2} (count {5:F2})",
                        poll[L] * 100, with.Preference[L] * 100, answer[L] * 100, poll[MP] * 100, with.Preference[MP] * 100, answer[MP] * 100));
                bool rescuedKd = false;
                foreach (TacticalFlow f in with.Flows) { rescuedKd |= f.Rescue && f.To == KD; }
                failures += Assert(sb, "3d. KD, polling clear in May 2022, needs nothing and is rescued by nobody - it lends, and receives only what L's leavers bring", !rescuedKd && with.Outflow(KD) > 0.0,
                    string.Format(CultureInfo.InvariantCulture, "KD {0:F2} -> {1:F2}, lent {2:F2} pp, received from leavers {3:F2} pp", poll[KD] * 100, with.Preference[KD] * 100, with.Outflow(KD) * 100, with.Inflow(KD) * 100));
            }

            // ---------- 4. 2018, measured not asserted ----------
            {
                double[] poll18 = Normalised(Psu2018);
                double[] answer = Normalised(Result2018);
                TacticalResult with = TacticalVoting.Apply(poll18, poll18, Moe2018, new TacticalSpec(Threshold, Awareness, Blocs2018, Lrgen));
                double l1Before = 0, l1After = 0;
                for (int p = 0; p < poll18.Length; p++)
                {
                    l1Before += Math.Abs(poll18[p] - answer[p]) * 100;
                    l1After += Math.Abs(with.Preference[p] - answer[p]) * 100;
                }

                sb.Append(string.Format(CultureInfo.InvariantCulture, "\n  2018 (measured, not asserted; the Alliance and red-green blocs, SD outside): KD polled {0:F2} -> layer {1:F2}, count {2:F2} (P(clear) {3:F3}); L {4:F2} -> {5:F2}, count {6:F2}; MP {7:F2} -> {8:F2}, count {9:F2}; L1 poll {10:F2} -> layer {11:F2} pp\n",
                    poll18[KD] * 100, with.Preference[KD] * 100, answer[KD] * 100, with.ClearProbability[KD],
                    poll18[L] * 100, with.Preference[L] * 100, answer[L] * 100,
                    poll18[MP] * 100, with.Preference[MP] * 100, answer[MP] * 100, l1Before, l1After));
                foreach (TacticalFlow f in with.Flows)
                {
                    sb.Append(string.Format(CultureInfo.InvariantCulture, "    {0} {1} -> {2}: {3:F2} pp\n", f.Rescue ? "lend" : "leave", Parties[f.From], Parties[f.To], f.Share * 100));
                }
            }

            sb.Append($"\nTACTICAL: {(failures == 0 ? "all assertions hold" : failures + " FAILED")}\n");
            Debug.Log(sb.ToString());
            CheckExit.Finish(failures == 0 ? 0 : 1);
        }

        private static double[] WithParty(double[] v, int party, double share, int absorber)
        {
            var r = (double[])v.Clone();
            r[absorber] += r[party] - share;
            r[party] = share;
            return r;
        }

        private static string Receivers(TacticalResult r, int from)
        {
            var sb = new StringBuilder();
            foreach (TacticalFlow f in r.Flows)
            {
                if (f.From != from) { continue; }
                if (sb.Length > 0) { sb.Append(", "); }
                sb.Append(string.Format(CultureInfo.InvariantCulture, "{0} {1:F2} pp", Parties[f.To], f.Share * 100));
            }

            return sb.ToString();
        }

        private static double Sum(double[] v) { double s = 0; foreach (double x in v) { s += x; } return s; }

        private static double[] Normalised(double[] v)
        {
            double sum = Sum(v);
            var r = new double[v.Length];
            for (int i = 0; i < r.Length; i++) { r[i] = v[i] / sum; }
            return r;
        }

        private static int Assert(StringBuilder sb, string label, bool condition, string detail)
        {
            sb.Append($"  {(condition ? "ok  " : "FAIL")} {label}: {detail}\n");
            return condition ? 0 : 1;
        }
    }
}
