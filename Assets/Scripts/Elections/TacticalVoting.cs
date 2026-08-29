using System;
using System.Collections.Generic;

namespace PoliSim.Elections
{
    /// <summary>
    /// What the tactical layer needs to know about the electorate and the system — §23's five
    /// factors in their threshold form: the ELECTORAL SYSTEM (a national threshold, or none),
    /// POLLING (handed in per call — the last PUBLISHED poll, never the truth), LOCAL COMPETITION
    /// (the bloc a party's second choice lives in), VOTER IDEOLOGY (a position per party; the
    /// closer the partner, the more willing the lender) and STRATEGIC AWARENESS (the share of the
    /// electorate that votes on the race rather than on preference alone).
    /// </summary>
    public sealed class TacticalSpec
    {
        /// <summary>The national threshold as a share (Sweden 0.04); 0 or less = no threshold, and the layer is the identity.</summary>
        public double Threshold;

        /// <summary>The share of voters who reason strategically at all, 0–1. [AUTHORED-DRAFT] staging value 0.5; W-F4's groups would carry it per group.</summary>
        public double Awareness;

        /// <summary>The bloc each party belongs to (any integer; −1 = none). A voter lends to, and abandons toward, the bloc only — a party outside any bloc neither lends nor receives.</summary>
        public int[] Bloc;

        /// <summary>A position per party on a 0–10 axis (CHES `lrgen` in the harness), or null for equal affinity within a bloc.</summary>
        public double[] Position;

        public TacticalSpec(double threshold, double awareness, int[] bloc, double[] position = null)
        {
            Threshold = threshold;
            Awareness = awareness;
            Bloc = bloc ?? throw new ArgumentNullException(nameof(bloc));
            Position = position;
        }

        /// <summary>1 for a bloc partner at the same position, falling linearly to 0 at ten points apart; 0 outside the bloc or for the party itself.</summary>
        public double Affinity(int from, int to)
        {
            if (from == to || Bloc[from] < 0 || Bloc[from] != Bloc[to]) { return 0.0; }
            if (Position == null) { return 1.0; }
            double gap = Math.Abs(Position[from] - Position[to]);
            return Math.Max(0.0, 1.0 - gap / 10.0);
        }
    }

    /// <summary>One tactical movement of preference mass, in share units, from one party to another.</summary>
    public readonly struct TacticalFlow
    {
        public readonly int From;
        public readonly int To;
        public readonly double Share;
        /// <summary>True for a rescue (a lender supporting a threatened partner); false for an abandonment (a hopeless party's own voters leaving).</summary>
        public readonly bool Rescue;

        public TacticalFlow(int from, int to, double share, bool rescue)
        {
            From = from;
            To = to;
            Share = share;
            Rescue = rescue;
        }
    }

    /// <summary>The layer's output: the shifted preference, the flows that produced it and each party's believed chance of clearing.</summary>
    public sealed class TacticalResult
    {
        public double[] Preference;
        public TacticalFlow[] Flows;
        /// <summary>Φ((polled − threshold) / σ) per party — what an aware voter believes about the party clearing; 1 everywhere when there is no threshold.</summary>
        public double[] ClearProbability;

        public double Inflow(int party)
        {
            double s = 0.0;
            foreach (TacticalFlow f in Flows) { if (f.To == party) { s += f.Share; } }
            return s;
        }

        public double Outflow(int party)
        {
            double s = 0.0;
            foreach (TacticalFlow f in Flows) { if (f.From == party) { s += f.Share; } }
            return s;
        }
    }

    /// <summary>
    /// W-A4 / SPEC §23 — tactical voting in its THRESHOLD form. PURE FUNCTIONS, WIRED TO NOTHING
    /// (R-N2). Sweden's 4 % threshold produces real support-voting ("stödröstning"): in May of
    /// each election year the party polling below 4 % finished above it in September, every time
    /// with the bloc's largest partner falling (`ElectionsData/sweden/psu_2018_2022.md`).
    ///
    /// **The voter's belief is the poll, widened.** An aware voter reads the last PUBLISHED poll
    /// and believes a party clears with probability Φ((polled − T) / σ), where σ combines the
    /// poll's own sampling error with <see cref="BeliefSigmaPp"/> — §20's rule that polls miss by
    /// more than their margin (late swings, turnout, tactical voting itself). No sample size
    /// removes the doubt.
    ///
    /// **Two behaviours, one variable.** Where the outcome is IN PLAY — the belief near even —
    /// the bloc's other voters LEND: the aware, willing fraction of each partner moves to the
    /// threatened party, up to what the party NEEDS to stand one belief-sigma clear of the
    /// threshold, weighted by `4P(1 − P)` (the pivotality of a vote, 1 at even odds, 0 at
    /// certainty either way). Where the outcome is HOPELESS — the belief well below even — the
    /// party's OWN aware voters ABANDON it for the bloc (§23's own example: Party C has no
    /// realistic chance, the voter switches to Party B), weighted by `((1 − P)(1 − 2P))²` below
    /// even odds and by nothing above it. A party polling comfortably clear needs nothing and
    /// loses nothing. Mass is conserved to the bit: every flow leaves one party and arrives at
    /// another.
    ///
    /// **[AUTHORED-DRAFT]** <see cref="BeliefSigmaPp"/> = 1.0 pp — fixed BEFORE the 2022 test from
    /// the worklist's own window (3.5–4.5 % must show measurable inflow, so the doubt must span at
    /// least ±0.5 pp around the threshold); <see cref="MaxLendFraction"/> = 0.15 — the most of a
    /// partner's aware voters that lend (with awareness 0.5, 7.5 % of the partner; the PSU's May
    /// lending, ≈ 1 % of M's sympathisers, is a lower bound on the final week's). Both strikeable
    /// (calibration entry 13). **No threshold → the identity**, asserted; other forms of §23 (a
    /// district's two-horse race under FPTP, a runoff) are not this item.
    /// </summary>
    public static class TacticalVoting
    {
        public const double BeliefSigmaPp = 1.0;
        public const double MaxLendFraction = 0.15;

        /// <summary>
        /// Apply the layer to one preference vector, reading one published poll.
        /// <paramref name="polledShares"/> and <paramref name="marginOfErrorPp"/> come from a
        /// <see cref="Poll"/> (or a catalog of one); the truth never enters.
        /// </summary>
        public static TacticalResult Apply(double[] preference, double[] polledShares, double[] marginOfErrorPp, TacticalSpec spec)
        {
            if (preference == null || polledShares == null || marginOfErrorPp == null || spec == null) { throw new ArgumentNullException(); }
            int n = preference.Length;
            if (polledShares.Length != n || marginOfErrorPp.Length != n || spec.Bloc.Length != n || (spec.Position != null && spec.Position.Length != n))
            {
                throw new ArgumentException("one entry per party in every vector");
            }

            var result = new TacticalResult
            {
                Preference = (double[])preference.Clone(),
                ClearProbability = new double[n],
                Flows = Array.Empty<TacticalFlow>(),
            };

            if (spec.Threshold <= 0.0 || spec.Awareness <= 0.0)
            {
                for (int p = 0; p < n; p++) { result.ClearProbability[p] = 1.0; }
                return result;
            }

            double thresholdPp = spec.Threshold * 100.0;
            var need = new double[n];
            var inPlay = new double[n];
            for (int p = 0; p < n; p++)
            {
                double sampling = marginOfErrorPp[p] / PollingSystem.ConfidenceZ;
                double sigma = Math.Sqrt(sampling * sampling + BeliefSigmaPp * BeliefSigmaPp);
                double polledPp = polledShares[p] * 100.0;
                double z = (polledPp - thresholdPp) / sigma;
                double clear = NormalCdf(z);
                result.ClearProbability[p] = clear;
                need[p] = Math.Max(0.0, thresholdPp + BeliefSigmaPp - polledPp) / 100.0;
                inPlay[p] = 4.0 * clear * (1.0 - clear);
            }

            // A lender's remaining capacity: the aware, willing fraction of its own voters.
            var capacity = new double[n];
            for (int p = 0; p < n; p++) { capacity[p] = need[p] > 0.0 ? 0.0 : preference[p] * spec.Awareness * MaxLendFraction; }

            // The most threatened party first — lenders' capacity depletes in that order.
            var order = new List<int>();
            for (int p = 0; p < n; p++) { if (need[p] > 0.0) { order.Add(p); } }
            order.Sort((a, b) => polledShares[a].CompareTo(polledShares[b]));

            var flows = new List<TacticalFlow>();
            foreach (int x in order)
            {
                double clear = result.ClearProbability[x];

                // Rescue: what the bloc can lend, weighted by the pivotality of a vote.
                var willing = new double[n];
                double available = 0.0;
                for (int y = 0; y < n; y++)
                {
                    willing[y] = capacity[y] * spec.Affinity(y, x);
                    available += willing[y];
                }

                double rescue = Math.Min(need[x], available) * inPlay[x];
                if (rescue > 0.0 && available > 0.0)
                {
                    for (int y = 0; y < n; y++)
                    {
                        if (willing[y] <= 0.0) { continue; }
                        double lent = rescue * willing[y] / available;
                        capacity[y] -= lent;
                        result.Preference[y] -= lent;
                        result.Preference[x] += lent;
                        flows.Add(new TacticalFlow(y, x, lent, rescue: true));
                    }
                }

                // Abandonment: below even odds the party's own aware voters leave for the bloc.
                if (clear < 0.5)
                {
                    double hopeless = (1.0 - clear) * (1.0 - 2.0 * clear);
                    double leaving = preference[x] * spec.Awareness * hopeless * hopeless;
                    var weight = new double[n];
                    double total = 0.0;
                    for (int y = 0; y < n; y++)
                    {
                        weight[y] = need[y] > 0.0 ? 0.0 : preference[y] * spec.Affinity(x, y);
                        total += weight[y];
                    }

                    if (leaving > 0.0 && total > 0.0)
                    {
                        for (int y = 0; y < n; y++)
                        {
                            if (weight[y] <= 0.0) { continue; }
                            double moved = leaving * weight[y] / total;
                            result.Preference[x] -= moved;
                            result.Preference[y] += moved;
                            flows.Add(new TacticalFlow(x, y, moved, rescue: false));
                        }
                    }
                }
            }

            result.Flows = flows.ToArray();
            return result;
        }

        /// <summary>The layer over every region's vector, reading the one NATIONAL poll (the threshold is national). Returns fresh arrays; the inputs are untouched.</summary>
        public static double[][] ApplyToRegions(double[][] regionPreference, double[] polledShares, double[] marginOfErrorPp, TacticalSpec spec)
        {
            if (regionPreference == null) { throw new ArgumentNullException(nameof(regionPreference)); }
            var shifted = new double[regionPreference.Length][];
            for (int r = 0; r < regionPreference.Length; r++)
            {
                shifted[r] = Apply(regionPreference[r], polledShares, marginOfErrorPp, spec).Preference;
            }

            return shifted;
        }

        /// <summary>The standard normal CDF (Abramowitz &amp; Stegun 7.1.26 on erf; |error| &lt; 1.5e-7).</summary>
        public static double NormalCdf(double z)
        {
            double x = Math.Abs(z) / Math.Sqrt(2.0);
            double t = 1.0 / (1.0 + 0.3275911 * x);
            double poly = t * (0.254829592 + t * (-0.284496736 + t * (1.421413741 + t * (-1.453152027 + t * 1.061405429))));
            double erf = 1.0 - poly * Math.Exp(-x * x);
            double cdf = 0.5 * (1.0 + erf);
            return z >= 0 ? cdf : 1.0 - cdf;
        }
    }
}
