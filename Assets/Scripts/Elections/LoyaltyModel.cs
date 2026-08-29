using System;

namespace PoliSim.Elections
{
    /// <summary>
    /// W-A1 — **loyalty DERIVED from measured volatility, not assumed.** PURE FUNCTIONS, WIRED TO
    /// NOTHING (R-N2).
    ///
    /// **Why this replaces a constant.** Day-2's gate failed on Italy: a uniform loyalty of 60
    /// asserts that ~60 % of every electorate votes as it did last time, which is true enough for
    /// Sweden and false for Italy 2022, where FdI grew 6.7× and M5S halved. The layer was right
    /// and the constant was wrong. Spec §5/§8 say as much — loyalty is a per-group (and plainly
    /// per-party, per-country) attribute, not one number for the world.
    ///
    /// **NON-CIRCULARITY IS AN INVARIANT OF THIS TYPE, NOT A CONVENTION** (ruled 2026-08-29, and
    /// written here so no future session can regress it). A party's loyalty is computed from **the
    /// two elections BEFORE the one being modelled** — never from the target election itself:
    /// - modelling the NEXT, unplayed election → the two most recent results;
    /// - backtesting 2022 → the 2013 and 2018 results, NOT 2018 and 2022.
    ///
    /// Deriving loyalty from how much a party changed AT T and then using it to predict T reads the
    /// answer off the answer sheet, and any figure so produced is worthless as validation even when
    /// it looks excellent — especially then. This type therefore takes two historical share vectors
    /// and is never given the target; a caller that wants the circular form has to construct it
    /// deliberately and visibly, which is the point. **W-A3's gate re-run uses the backtest
    /// direction.**
    ///
    /// <code>
    /// relativeChange_i = |v(T-1)_i - v(T-2)_i| / max(v(T-1)_i, v(T-2)_i)
    /// loyalty_i        = 100 * (1 - min(1, relativeChange_i))
    /// </code>
    ///
    /// Relative rather than absolute change, because a 5-point move means something different to a
    /// 40 % party than to a 6 % party; dividing by the LARGER of the two is what keeps a party that
    /// doubled and a party that halved symmetric (both score 50). **Zero authored constants** — the
    /// formula's only inputs are two sourced election results.
    ///
    /// **A party that did not exist at T−2 scores loyalty 0**, and that is the correct statement
    /// rather than a fallback: nobody had a habit of voting for it, so it must win its whole vote
    /// from the persuadable fraction. The same is true of a party that existed then and not now.
    ///
    /// <see cref="PedersenIndex"/> is included because it is the standard published measure of an
    /// election pair's total volatility, and reporting it beside the per-party figures lets a
    /// reader check the derivation against political-science literature rather than trusting it.
    /// </summary>
    public static class LoyaltyModel
    {
        /// <summary>
        /// **A country with fewer than two prior elections on file has NO loyalty value, and the
        /// model refuses to run rather than defaulting** (ruled 2026-08-29). A silent default would
        /// reinstate exactly the global constant this type exists to remove — and it would do so
        /// invisibly, which is worse than the constant was. The USA (one election on disk) and
        /// France (one, and out of scope for seats by R-EL10) are the current cases; the USA's
        /// second election is billed as a data line.
        /// </summary>
        public static bool CanDerive(double[] previous, double[] previousPrevious)
        {
            return previous != null && previousPrevious != null
                   && previous.Length == previousPrevious.Length && previous.Length > 0;
        }

        /// <summary>One party's loyalty (0–100) from its shares at the two preceding elections. Shares may be percentages or fractions as long as both are on the same scale.</summary>
        public static double PartyLoyalty(double sharePrevious, double sharePreviousPrevious)
        {
            double larger = Math.Max(sharePrevious, sharePreviousPrevious);
            if (larger <= 0.0)
            {
                return 0.0;   // present at neither election - no electorate to be loyal to
            }

            double relativeChange = Math.Abs(sharePrevious - sharePreviousPrevious) / larger;
            if (relativeChange > 1.0) { relativeChange = 1.0; }

            return ElectionScales.Max * (1.0 - relativeChange);
        }

        /// <summary>Loyalty per party, index-aligned with the two share vectors.</summary>
        public static double[] PartyLoyalties(double[] previous, double[] previousPrevious)
        {
            if (previous == null || previousPrevious == null || previous.Length != previousPrevious.Length)
            {
                throw new ArgumentException("the two historical share vectors must line up");
            }

            var loyalty = new double[previous.Length];
            for (int i = 0; i < previous.Length; i++)
            {
                loyalty[i] = PartyLoyalty(previous[i], previousPrevious[i]);
            }

            return loyalty;
        }

        /// <summary>
        /// The Pedersen index of electoral volatility for a pair of elections: half the sum of the
        /// absolute changes in every party's share. Reported alongside the per-party figures as the
        /// standard cross-check — a high Pedersen index and uniformly high loyalties would mean the
        /// derivation is wrong.
        /// </summary>
        public static double PedersenIndex(double[] later, double[] earlier)
        {
            if (later == null || earlier == null || later.Length != earlier.Length)
            {
                throw new ArgumentException("the two share vectors must line up");
            }

            double sum = 0.0;
            for (int i = 0; i < later.Length; i++)
            {
                sum += Math.Abs(later[i] - earlier[i]);
            }

            return 0.5 * sum;
        }

        /// <summary>The mean of a loyalty vector, weighted by each party's size at T−1 — the number to compare against the old global constant of 60.</summary>
        public static double WeightedMeanLoyalty(double[] loyalty, double[] weights)
        {
            double weighted = 0.0;
            double total = 0.0;
            for (int i = 0; i < loyalty.Length; i++)
            {
                weighted += loyalty[i] * weights[i];
                total += weights[i];
            }

            return total > 0 ? weighted / total : 0.0;
        }
    }
}
