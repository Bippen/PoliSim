using System;

namespace PoliSim.Elections
{
    /// <summary>
    /// PHASE 4's MEASURING INSTRUMENT (2026-08-29) — votes-from-model, the rung above
    /// votes-to-seats. PURE FUNCTIONS, WIRED TO NOTHING (R-N2); the only caller is the editor
    /// backtest harness.
    ///
    /// ⚠ **THIS IS NOT THE SPEC'S CHAIN.** `ELECTIONS_CAMPAIGN_SPEC.md` has still not arrived
    /// (Phase 0's STOP, 2026-08-29), so §7's compatibility core, §8's loyalty damping and
    /// §26/§27's turnout and aggregation are NOT implemented here and are not guessed at. What
    /// this file holds is the SMALLEST defensible spatial model that can be measured against
    /// real returns — a placeholder instrument whose whole purpose is to produce an honest
    /// deviation table for the E-phase plan to be sized against. Every functional-form choice
    /// is an [AUTHORED-DRAFT] call logged in the day report, and every one is strikeable: when
    /// the spec lands, its chain REPLACES this file wholesale rather than absorbing it.
    ///
    /// The model, stated in full (no hidden knobs — the whole point):
    /// - The electorate is a 2-D Gaussian cloud over the CHES axes actually sourced,
    ///   lrecon (economic left-right, 0-10) and galtan (libertarian-authoritarian, 0-10),
    ///   with mean (muEcon, muSoc) and a shared standard deviation sigma.
    /// - Issue salience weights the two axes: wEcon from the sourced Eurobarometer/Gallup
    ///   shares, wSoc = 1 - wEcon (the issue-to-axis mapping is an authored call, logged).
    /// - A voter at (x, y) chooses party p with probability proportional to
    ///   exp(-d2(p) / tau), where d2 = wEcon*(x - p.Econ)^2 + wSoc*(y - p.Soc)^2 — the
    ///   standard proximity/logit form; tau is the choice temperature (tau -> 0 collapses to
    ///   nearest-party, large tau to uniform).
    /// - The party's predicted vote share is the electorate-weighted integral of that
    ///   probability, evaluated by deterministic grid quadrature (no RNG anywhere: the model
    ///   is exactly reproducible, which is the property the night's seeded-stream work exists
    ///   to protect).
    ///
    /// What the model deliberately does NOT contain, so no reader mistakes its silence for a
    /// claim: incumbency, loyalty/partisan attachment, turnout differences between groups,
    /// tactical voting, coalition signalling, regional structure, campaign effects, or any
    /// party-specific constant of any kind. A party is nothing but a point in the plane.
    /// </summary>
    public static class VoteModel
    {
        /// <summary>A party as this model sees it: a name, its two sourced coordinates, nothing else.</summary>
        public readonly struct PartyPoint
        {
            public readonly string Name;
            public readonly double Econ;
            public readonly double Soc;

            public PartyPoint(string name, double econ, double soc)
            {
                Name = name;
                Econ = econ;
                Soc = soc;
            }
        }

        /// <summary>The electorate and choice parameters — the model's ENTIRE free surface, four numbers.</summary>
        public readonly struct Electorate
        {
            public readonly double MuEcon;
            public readonly double MuSoc;
            public readonly double Sigma;
            public readonly double Tau;

            public Electorate(double muEcon, double muSoc, double sigma, double tau)
            {
                MuEcon = muEcon;
                MuSoc = muSoc;
                Sigma = sigma;
                Tau = tau;
            }

            public override string ToString() =>
                string.Format(System.Globalization.CultureInfo.InvariantCulture,
                    "mu=({0:F2},{1:F2}) sigma={2:F2} tau={3:F2}", MuEcon, MuSoc, Sigma, Tau);
        }

        /// <summary>Grid resolution per axis over [0, 10]. 81 points = a 0.125 step; the quadrature error is far below the deviations being measured (checked by doubling it).</summary>
        public const int GridSteps = 81;

        /// <summary>
        /// Predicted vote shares (summing to 1) for <paramref name="parties"/> under
        /// <paramref name="electorate"/>, with axis weights <paramref name="wEcon"/> /
        /// (1 - wEcon). Deterministic quadrature; no allocation beyond the result array.
        /// </summary>
        public static double[] PredictShares(PartyPoint[] parties, Electorate electorate, double wEcon)
        {
            if (parties == null || parties.Length == 0) { throw new ArgumentException("no parties"); }

            double wSoc = 1.0 - wEcon;
            var shares = new double[parties.Length];
            var utility = new double[parties.Length];
            double weightSum = 0.0;
            double twoSigmaSq = 2.0 * electorate.Sigma * electorate.Sigma;
            double step = 10.0 / (GridSteps - 1);

            for (int ix = 0; ix < GridSteps; ix++)
            {
                double x = ix * step;
                double dx = x - electorate.MuEcon;
                for (int iy = 0; iy < GridSteps; iy++)
                {
                    double y = iy * step;
                    double dy = y - electorate.MuSoc;
                    double weight = Math.Exp(-(dx * dx + dy * dy) / twoSigmaSq);
                    if (weight < 1e-12) { continue; }

                    weightSum += weight;

                    // Softmax over -d2/tau, shifted by the max for numerical safety.
                    double best = double.NegativeInfinity;
                    for (int p = 0; p < parties.Length; p++)
                    {
                        double ex = x - parties[p].Econ;
                        double ey = y - parties[p].Soc;
                        double d2 = wEcon * ex * ex + wSoc * ey * ey;
                        utility[p] = -d2 / electorate.Tau;
                        if (utility[p] > best) { best = utility[p]; }
                    }

                    double norm = 0.0;
                    for (int p = 0; p < parties.Length; p++)
                    {
                        utility[p] = Math.Exp(utility[p] - best);
                        norm += utility[p];
                    }

                    for (int p = 0; p < parties.Length; p++)
                    {
                        shares[p] += weight * (utility[p] / norm);
                    }
                }
            }

            for (int p = 0; p < shares.Length; p++)
            {
                shares[p] /= weightSum;
            }

            return shares;
        }

        /// <summary>Mean absolute deviation in PERCENTAGE POINTS between predicted shares (0-1) and actual shares (0-1).</summary>
        public static double MeanAbsoluteDeviationPp(double[] predicted, double[] actual)
        {
            double sum = 0.0;
            for (int i = 0; i < predicted.Length; i++)
            {
                sum += Math.Abs(predicted[i] - actual[i]);
            }

            return 100.0 * sum / predicted.Length;
        }

        /// <summary>
        /// THE ONE CALIBRATION PASS the kickoff allows: a deterministic grid search over the
        /// four declared parameters, minimising mean absolute deviation against the real
        /// result. Returns the best electorate found and its MAD. Nothing party-specific is
        /// ever fitted — four numbers per country, all four printed in the report, all four
        /// strikeable. (A search, not an optimiser: same inputs, same answer, every run.)
        /// </summary>
        public static Electorate Calibrate(PartyPoint[] parties, double[] actual, double wEcon, out double bestMad)
        {
            bestMad = double.PositiveInfinity;
            var best = new Electorate(5.0, 5.0, 2.0, 2.0);

            for (double mu = 3.0; mu <= 7.0001; mu += 0.25)
            {
                for (double ms = 3.0; ms <= 7.0001; ms += 0.25)
                {
                    foreach (double sigma in new[] { 1.0, 1.5, 2.0, 2.5, 3.0, 3.5 })
                    {
                        foreach (double tau in new[] { 0.5, 1.0, 2.0, 4.0, 8.0, 16.0 })
                        {
                            var candidate = new Electorate(mu, ms, sigma, tau);
                            double[] predicted = PredictShares(parties, candidate, wEcon);
                            double mad = MeanAbsoluteDeviationPp(predicted, actual);
                            if (mad < bestMad)
                            {
                                bestMad = mad;
                                best = candidate;
                            }
                        }
                    }
                }
            }

            return best;
        }
    }
}
