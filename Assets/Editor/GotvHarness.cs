using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using PoliSim.Elections;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// W-B11's harness — §26's Get-Out-The-Vote on Sweden's 29 valkretsar.
    ///
    /// The done-when, asserted:
    /// 1. **mobilisation spending moves turnout in targeted regions ONLY** — one party works
    ///    three valkretsar; their turnout (and that party's votes there) rise, the other 26 stay at
    ///    exactly base turnout to the last bit, and the other parties' turnout in the worked
    ///    regions is unchanged;
    /// 2. **the national turnout stays inside historically plausible bounds** — with every
    ///    party's whole war chest (8 × 2.4 m kr) and a large volunteer force put into door-knocking
    ///    everywhere, the national figure stays within Sweden's 2002–2022 range widened by two
    ///    points (80.11 % … 87.18 %, Valmyndigheten; `ElectionsData/sweden/turnout_history.md`),
    ///    and it can never exceed 100 % whatever is spent;
    /// 3. volunteers BIND — an operation with money but no hours makes no contacts; §35's shape —
    ///    the first 10 000 doors are worth more than the next 10 000.
    ///
    /// Staging: base turnout SOURCED (2022: 84.21 %), applied uniformly per valkrets because per-
    /// valkrets eligible counts ARE on disk as of W-F1 (SOURCED antalRostberattigade per valkrets,
    /// `valkrets_votes_2022.csv` column 11 - the DERIVED "2018 valid ÷ 87.18 %" is retired);
    /// preference the 2022 result; engagement,
    /// enthusiasm and salience at `TurnoutModel`'s neutral 50 so the only thing moving is GOTV.
    /// </summary>
    public static class GotvHarness
    {
        private static readonly double[] Shares2022 = { 0.3033, 0.2054, 0.1910, 0.0675, 0.0671, 0.0534, 0.0508, 0.0461 };
        private const double BaseTurnout2022 = 0.8421;   // SOURCED: Valmyndigheten, 6,547,801 of 7,775,390
        // W-F1: Turnout2018 retired as an eligible-derivation - eligible is now SOURCED per valkrets.
        private const double HistoricLow = 0.8011;       // 2002, the lowest of 2002-2022 (turnout_history.md)
        private const double HistoricHigh = 0.8718;      // 2018, the highest
        private const double Widening = 0.02;

        public static void Run()
        {
            CheckExit.ArmLogFold();
            int failures = 0;
            var sb = new StringBuilder();
            sb.Append("=== W-B11: Get-Out-The-Vote (§26) on the 29 valkretsar ===\n");

            double[] prior = Normalised(Shares2022);
            string[] names;
            double[] eligible = ReadEligible(out names);
            double nationalEligible = 0.0;
            foreach (double e in eligible) { nationalEligible += e; }
            sb.Append(string.Format(CultureInfo.InvariantCulture,
                "  staging: {0} valkretsar, eligible SOURCED per valkrets (W-F1) = {1:N0} nationally (published 7,775,390); base turnout 84.21 % (2022)\n",
                eligible.Length, nationalEligible));

            const double neutral = 50.0;

            // ---------- 1. targeted regions only ----------
            var state = new RegionalMobilization(eligible, prior.Length);
            double before = state.NationalTurnout(prior, BaseTurnout2022, neutral, neutral, neutral);
            failures += Assert(sb, "0. with no operation anywhere the national turnout IS the base (84.21 %)",
                Math.Abs(before - BaseTurnout2022) < 1e-12, string.Format(CultureInfo.InvariantCulture, "{0:P4}", before));

            int[] targets = { IndexOf(names, "Stockholms län"), IndexOf(names, "Skåne läns södra"), IndexOf(names, "Gotlands län") };
            const int party = 0;   // S works three valkretsar
            double[] regionBefore = new double[eligible.Length];
            for (int r = 0; r < eligible.Length; r++) { regionBefore[r] = state.RegionTurnout(r, prior, BaseTurnout2022, neutral, neutral, neutral); }

            sb.Append("\n  S runs door-knocking in three valkretsar with 400 000 kr and 20 000 volunteer-hours each:\n");
            foreach (int r in targets)
            {
                double contacts = state.Operate(r, party, GotvOperation.DoorKnocking, 400_000, 20_000, out double money, out double hours);
                sb.Append(string.Format(CultureInfo.InvariantCulture,
                    "    {0,-28} {1,8:N0} doors, {2,9:N0} kr, {3,8:N0} h -> mobilization {4:F1}, turnout {5:P2} -> {6:P2}\n",
                    names[r], contacts, money, hours, state.Mobilization(r, party), regionBefore[r],
                    state.RegionTurnout(r, prior, BaseTurnout2022, neutral, neutral, neutral)));
            }

            bool targetsRose = true, othersUnchanged = true, otherPartiesUnchanged = true;
            for (int r = 0; r < eligible.Length; r++)
            {
                double after = state.RegionTurnout(r, prior, BaseTurnout2022, neutral, neutral, neutral);
                bool isTarget = Array.IndexOf(targets, r) >= 0;
                if (isTarget && !(after > regionBefore[r])) { targetsRose = false; }
                if (!isTarget && after != regionBefore[r]) { othersUnchanged = false; }
                for (int p = 1; p < prior.Length; p++)
                {
                    if (state.PartyTurnout(r, p, BaseTurnout2022, neutral, neutral, neutral) != BaseTurnout2022) { otherPartiesUnchanged = false; }
                }
            }

            failures += Assert(sb, "1a. turnout rose in the three worked valkretsar", targetsRose, "3 of 3");
            failures += Assert(sb, "1b. the other 26 valkretsar are at EXACTLY base turnout (bit for bit) - targeted regions only", othersUnchanged, "26 of 26");
            failures += Assert(sb, "1c. the other parties' supporters' turnout is unchanged everywhere (GOTV is the working party's)", otherPartiesUnchanged, "7 parties x 29 regions");

            double[] votesBefore = new RegionalMobilization(eligible, prior.Length).RegionVotes(targets[0], prior, BaseTurnout2022, neutral, neutral, neutral);
            double[] votesAfter = state.RegionVotes(targets[0], prior, BaseTurnout2022, neutral, neutral, neutral);
            double shareBefore = votesBefore[party] / Sum(votesBefore);
            double shareAfter = votesAfter[party] / Sum(votesAfter);
            failures += Assert(sb, "1d. in a worked valkrets the working party's VOTE SHARE rises with no change to preference (the turnout advantage)",
                shareAfter > shareBefore && votesAfter[1] == votesBefore[1],
                string.Format(CultureInfo.InvariantCulture, "{0}: S {1:P2} -> {2:P2}; SD's votes unchanged at {3:N0}", names[targets[0]], shareBefore, shareAfter, votesAfter[1]));

            // ---------- 3. volunteers bind; §35's shape ----------
            var bound = new RegionalMobilization(eligible, prior.Length);
            double none = bound.Operate(0, 0, GotvOperation.DoorKnocking, 1_000_000, 0.0, out _, out _);
            failures += Assert(sb, "3a. money without volunteer-hours makes NO contacts (volunteers bind)", none == 0.0, $"{none} contacts from 1 m kr and 0 hours");

            double first = GotvModel.Mobilization(10_000, eligible[0]) - 50.0;
            double second = GotvModel.Mobilization(20_000, eligible[0]) - GotvModel.Mobilization(10_000, eligible[0]);
            failures += Assert(sb, "3b. the first 10 000 doors move mobilization more than the next 10 000 (§35's shape)",
                first > second, string.Format(CultureInfo.InvariantCulture, "+{0:F3} then +{1:F3}", first, second));
            failures += Assert(sb, "3c. mobilization never exceeds 100 whatever is spent",
                GotvModel.Mobilization(1e12, eligible[0]) <= 100.0, string.Format(CultureInfo.InvariantCulture, "{0:F6} at 10^12 contacts", GotvModel.Mobilization(1e12, eligible[0])));

            // ---------- 2. national turnout within historical bounds ----------
            var everyone = new RegionalMobilization(eligible, prior.Length);
            double totalMoney = 0.0, totalHours = 0.0, totalContacts = 0.0;
            for (int p = 0; p < prior.Length; p++)
            {
                // Every party's whole 2.4 m kr chest and 60 000 volunteer-hours (say 2 000 volunteers x 30 h) spread by eligible share.
                for (int r = 0; r < eligible.Length; r++)
                {
                    double share = eligible[r] / nationalEligible;
                    totalContacts += everyone.Operate(r, p, GotvOperation.DoorKnocking, 2_400_000 * share, 60_000 * share, out double m, out double h);
                    totalMoney += m; totalHours += h;
                }
            }

            double national = everyone.NationalTurnout(prior, BaseTurnout2022, neutral, neutral, neutral);
            sb.Append(string.Format(CultureInfo.InvariantCulture,
                "\n  every party door-knocks everywhere with its whole chest: {0:N0} doors, {1:N0} kr, {2:N0} h -> national turnout {3:P2} (base {4:P2})\n",
                totalContacts, totalMoney, totalHours, national, BaseTurnout2022));
            failures += Assert(sb, "2a. with every party's whole chest on the doors the national turnout stays within 2002-2022's range widened by two points",
                national >= HistoricLow - Widening && national <= HistoricHigh + Widening,
                string.Format(CultureInfo.InvariantCulture, "{0:P2} within [{1:P2}, {2:P2}]", national, HistoricLow - Widening, HistoricHigh + Widening));

            // The absurd case: unlimited money and volunteers everywhere - still never past 100 %.
            var absurd = new RegionalMobilization(eligible, prior.Length);
            for (int p = 0; p < prior.Length; p++) { for (int r = 0; r < eligible.Length; r++) { absurd.Operate(r, p, GotvOperation.Transport, 1e12, 1e12, out _, out _); } }
            double absurdNational = absurd.NationalTurnout(prior, BaseTurnout2022, neutral, neutral, neutral);
            failures += Assert(sb, "2b. with unlimited lifts for everyone everywhere the national turnout is still at most 100 % (and above the plausible range - stated, not hidden)",
                absurdNational <= 1.0, string.Format(CultureInfo.InvariantCulture, "{0:P2}", absurdNational));

            // A realistic single party's ground game moves the national figure by a fraction of a point.
            var one = new RegionalMobilization(eligible, prior.Length);
            for (int r = 0; r < eligible.Length; r++) { one.Operate(r, 0, GotvOperation.DoorKnocking, 2_400_000 * eligible[r] / nationalEligible, 60_000 * eligible[r] / nationalEligible, out _, out _); }
            double oneNational = one.NationalTurnout(prior, BaseTurnout2022, neutral, neutral, neutral);
            sb.Append(string.Format(CultureInfo.InvariantCulture, "  one party's whole chest on the doors nationwide: {0:P2} -> {1:P2} (+{2:F2} pp), its own supporters {3:P2}\n",
                BaseTurnout2022, oneNational, 100 * (oneNational - BaseTurnout2022), one.PartyTurnout(0, 0, BaseTurnout2022, neutral, neutral, neutral)));

            sb.Append($"\n=== GotvHarness: {(failures == 0 ? "ALL ASSERTIONS PASS" : failures + " FAILED")} ===\n");
            Debug.Log(sb.ToString());
            CheckExit.Finish(failures == 0 ? 0 : 1);
        }

        private static double[] ReadEligible(out string[] names)
        {
            string path = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "ElectionsData", "sweden", "valkrets_votes_2022.csv"));
            var eligible = new List<double>();
            var nameList = new List<string>();
            foreach (string raw in File.ReadAllLines(path))
            {
                string line = raw.Trim();
                if (line.Length == 0 || line.StartsWith("#") || line.StartsWith("valkrets;")) { continue; }
                string[] cells = line.Split(';');
                nameList.Add(cells[0]);
                eligible.Add(double.Parse(cells[10], CultureInfo.InvariantCulture));   // W-F1: SOURCED antalRostberattigade, no longer valid / a national turnout
            }

            names = nameList.ToArray();
            return eligible.ToArray();
        }

        private static int IndexOf(string[] names, string name)
        {
            int i = Array.IndexOf(names, name);
            if (i < 0) { throw new InvalidDataException($"valkrets '{name}' not in the 2022 file"); }
            return i;
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
