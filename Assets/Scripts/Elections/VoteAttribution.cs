using System;
using System.Collections.Generic;

namespace PoliSim.Elections
{
    /// <summary>
    /// §31's "why you won / why you lost", as a LEDGER rather than a story. The sources a party's
    /// vote share can be moved by, each one a MECHANISM the model actually applies — never a
    /// phrase. A line's label is this enum's name; there is no prose anywhere in the instrument.
    /// </summary>
    public enum VoteAttributionSource
    {
        Rally = 0,
        TownHall = 1,
        DoorToDoor = 2,
        TelevisionAd = 3,
        DigitalAd = 4,
        SocialPost = 5,
        Interview = 6,
        PolicyAnnouncement = 7,
        /// <summary>Persuasion pressure aimed AGAINST this party by others' negative campaigning (§11).</summary>
        AttacksReceived = 8,
        /// <summary>Everything every OTHER party did, as one source - a party's own share moves when its rivals campaign, and that is not its own doing.</summary>
        OpponentCampaigns = 9,
        /// <summary>C-N1: §39's Media Effects layer — the persuasion the party's own earned coverage carried, the press's account rather than any action's.</summary>
        EarnedCoverage = 10,
    }

    /// <summary>
    /// W-D4 — post-election attribution (§31), built as the approval attribution ledger pointed at
    /// vote share: the nine-term instrument's idiom, every line DERIVED, and the lines summing to
    /// the movement they explain. PURE, WIRED TO NOTHING (R-N2).
    ///
    /// **The identity is exact, not approximate, and that is the whole point.** An explanation that
    /// does not add up to the thing it explains is a story about the model rather than a reading of
    /// it — the failure `ApprovalAttribution` carries `ClampLoss` to avoid. Here exactness comes
    /// from using SHAPLEY VALUES over the sources: the share a party ends with is a non-linear
    /// function of the pressures (compatibility is linear in pressure, but `PreferenceModel`
    /// normalises across parties), so leaving one source out at a time would NOT sum to the total
    /// and would need a residual line to hide the gap. Shapley's efficiency axiom gives
    /// Σ contributions = v(all) − v(none) identically, and its symmetry axiom means the order the
    /// sources are considered in cannot change the answer — which a sequential decomposition, the
    /// obvious alternative, cannot say.
    ///
    /// The cost is 2^n evaluations for n sources, which is why the sources are the eleven above and
    /// not seventy-two: a party's own eight actions, the attacks aimed at it, the persuasion its
    /// own earned coverage carried (C-N1, §39's Media Effects layer), and **everything every
    /// other party did as ONE source**. That aggregation is honest — it says "your rivals'
    /// campaigning moved your share by this much" without pretending to know which of their rallies
    /// did it — and it keeps the sweep at 1 024 evaluations.
    /// </summary>
    public static class VoteAttribution
    {
        public static readonly VoteAttributionSource[] Sources =
        {
            VoteAttributionSource.Rally, VoteAttributionSource.TownHall, VoteAttributionSource.DoorToDoor,
            VoteAttributionSource.TelevisionAd, VoteAttributionSource.DigitalAd, VoteAttributionSource.SocialPost,
            VoteAttributionSource.Interview, VoteAttributionSource.PolicyAnnouncement,
            VoteAttributionSource.AttacksReceived, VoteAttributionSource.OpponentCampaigns,
            VoteAttributionSource.EarnedCoverage,
        };

        /// <summary>What the campaign actually delivered, as recorded at the write sites - never recomputed.</summary>
        public sealed class Inputs
        {
            /// <summary>Persuasion pressure this party's own actions delivered, per §12 action kind, in `CampaignActions.TheEight`'s order.</summary>
            public double[] OwnPersuasionByAction;
            /// <summary>Persuasion pressure aimed against this party by others (a positive magnitude; it lowers the party).</summary>
            public double AttacksReceived;
            /// <summary>C-N1: the party's own persuasion carried by its earned coverage (`PartyLedger.PersuasionFromCoverage`).</summary>
            public double OwnPersuasionFromCoverage;
            /// <summary>Every party's total persuasion, so the opponents' bloc can be applied or withheld.</summary>
            public double[] TotalPersuasionPerParty;
            /// <summary>The compatibility the electorate would have felt with no campaign at all.</summary>
            public double[] BaseCompatibility;
            public double[] PriorShares;
            public double[] LoyaltyPerParty;
        }

        /// <summary>One party's finished ledger: the baseline, the close, and a signed contribution per source that sums to the movement.</summary>
        public sealed class Ledger
        {
            public int Party;
            public double ShareAtBaseline;
            public double ShareAtClose;
            public readonly Dictionary<VoteAttributionSource, double> Lines = new Dictionary<VoteAttributionSource, double>();

            public double Deviation => ShareAtClose - ShareAtBaseline;

            public double LineSum
            {
                get { double s = 0.0; foreach (KeyValuePair<VoteAttributionSource, double> kv in Lines) { s += kv.Value; } return s; }
            }

            /// <summary>How far the lines are from the movement they explain. Zero by construction; kept so the identity can be ASSERTED rather than assumed.</summary>
            public double Residual => Deviation - LineSum;
        }

        /// <summary>
        /// The share this party ends with when only the sources in <paramref name="subset"/> are
        /// applied - the coalition value the Shapley sweep is taken over. Everything outside the
        /// subset is simply not there, which is what "this source's contribution" has to mean.
        /// </summary>
        private static double Value(Inputs input, int party, int subset)
        {
            int n = input.BaseCompatibility.Length;
            var persuasion = new double[n];

            for (int i = 0; i < 8; i++)
            {
                if ((subset & (1 << i)) != 0) { persuasion[party] += input.OwnPersuasionByAction[i]; }
            }

            if ((subset & (1 << (int)VoteAttributionSource.AttacksReceived)) != 0)
            {
                persuasion[party] -= input.AttacksReceived;
            }
            if ((subset & (1 << (int)VoteAttributionSource.EarnedCoverage)) != 0)
            {
                persuasion[party] += input.OwnPersuasionFromCoverage;
            }

            if ((subset & (1 << (int)VoteAttributionSource.OpponentCampaigns)) != 0)
            {
                for (int q = 0; q < n; q++) { if (q != party) { persuasion[q] += input.TotalPersuasionPerParty[q]; } }
            }

            var compatibility = new double[n];
            for (int i = 0; i < n; i++)
            {
                compatibility[i] = input.BaseCompatibility[i] + persuasion[i] / CampaignPressure.PersuasionPerCompatibilityPoint;
            }

            return PreferenceModel.Preference(compatibility, input.PriorShares, input.LoyaltyPerParty)[party];
        }

        /// <summary>
        /// The ledger for one party: a Shapley contribution per source, over every ordering at
        /// once. `Σ lines == ShareAtClose − ShareAtBaseline` identically (efficiency), and no
        /// source is privileged by being considered first (symmetry).
        /// </summary>
        public static Ledger Explain(Inputs input, int party)
        {
            if (input == null) { throw new ArgumentNullException(nameof(input)); }
            if (input.OwnPersuasionByAction == null || input.OwnPersuasionByAction.Length != 8) { throw new ArgumentException("one persuasion figure per §12 action"); }
            int n = Sources.Length;
            int all = (1 << n) - 1;

            // The Shapley weights: |S|! (n-|S|-1)! / n!, precomputed per subset size.
            var weight = new double[n];
            for (int size = 0; size < n; size++) { weight[size] = Factorial(size) * Factorial(n - size - 1) / Factorial(n); }

            // Every coalition's value once - the sweep is 2^n, and each value is one preference
            // computation, so caching them turns n * 2^n evaluations into 2^n.
            var value = new double[all + 1];
            for (int subset = 0; subset <= all; subset++) { value[subset] = Value(input, party, subset); }

            var ledger = new Ledger
            {
                Party = party,
                ShareAtBaseline = value[0],
                ShareAtClose = value[all],
            };

            foreach (VoteAttributionSource source in Sources)
            {
                int bit = 1 << (int)source;
                double contribution = 0.0;
                for (int subset = 0; subset <= all; subset++)
                {
                    if ((subset & bit) != 0) { continue; }
                    contribution += weight[CountBits(subset)] * (value[subset | bit] - value[subset]);
                }

                ledger.Lines[source] = contribution;
            }

            return ledger;
        }

        private static double Factorial(int k)
        {
            double f = 1.0;
            for (int i = 2; i <= k; i++) { f *= i; }
            return f;
        }

        private static int CountBits(int v)
        {
            int c = 0;
            while (v != 0) { c += v & 1; v >>= 1; }
            return c;
        }
    }
}
