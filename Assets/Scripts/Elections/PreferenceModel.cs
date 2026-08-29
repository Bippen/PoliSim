using System;

namespace PoliSim.Elections
{
    /// <summary>
    /// SPEC §8 — voter loyalty, as the damping between what a group ALREADY believes and what
    /// this election's compatibility would imply. PURE FUNCTIONS, WIRED TO NOTHING (R-N2).
    ///
    /// **Why this is the first chain layer built.** Day-1's Phase-4 measurement found two
    /// systematic errors in a loyalty-free spatial model — parties alone in an empty ideological
    /// quadrant were over-predicted (BSW +10.2 pp, TD +15.9, KD +8.2) and the largest parties were
    /// under-predicted (KO −16.7, AfD −11.7, CDU −9.3, M −10.0) — and both are the same absence:
    /// nothing held voters where they already were. §8 is that missing force, and the report named
    /// it the highest-value single unit in the plan before this file existed.
    ///
    /// The model:
    /// <code>
    /// persuaded_p  = compatibility_p^Sharpness / SUM(compatibility^Sharpness)
    /// preference_p = lambda * prior_p + (1 - lambda) * persuaded_p,   lambda = loyalty / 100
    /// </code>
    /// At loyalty 90 (the spec's "Strong Party Loyalist") nine tenths of the group's vote is where
    /// it was; at loyalty 20 ("Swing Voter") four fifths is up for grabs. That is exactly the
    /// spec's requirement that loyal voters "require significantly more effort to persuade" while
    /// swing voters are "much more responsive" — expressed as one number, not a special case.
    ///
    /// **[AUTHORED-DRAFT] constants** (R-N4; game fiction, logged one line each, strikeable):
    /// - `Sharpness = 3.0` — how decisively compatibility converts into vote share. 1.0 makes
    ///   preference proportional to compatibility (far too flat: a party a group merely tolerates
    ///   would take a third of it); above ~5 it collapses to winner-take-all within the group.
    ///   3.0 keeps a clear favourite while leaving real support for second and third.
    /// - `MinimumCompatibility = 1.0` — a floor before exponentiation, so a party at compatibility
    ///   0 receives an infinitesimal rather than a hard zero share. Without it a group could
    ///   mathematically never be reached by a party it currently rejects, which would make
    ///   §12's whole campaign layer pointless for the hardest targets.
    ///
    /// Prior attachment is an INPUT, not an invention: it is where the group's vote actually sat
    /// (last election, or the model's previous period). The caller supplies it; this file has no
    /// opinion about any real party's support.
    /// </summary>
    public static class PreferenceModel
    {
        public const double Sharpness = 3.0;
        public const double MinimumCompatibility = 1.0;

        /// <summary>Compatibility scores (0–100, one per party) converted to the shares a group with NO loyalty would give — the fully-persuadable limit.</summary>
        public static double[] PersuadedShares(double[] compatibility)
        {
            if (compatibility == null || compatibility.Length == 0) { throw new ArgumentException("no parties"); }

            var weights = new double[compatibility.Length];
            double sum = 0.0;
            for (int i = 0; i < compatibility.Length; i++)
            {
                double c = Math.Max(MinimumCompatibility, ElectionScales.Clamp(compatibility[i]));
                weights[i] = Math.Pow(c, Sharpness);
                sum += weights[i];
            }

            for (int i = 0; i < weights.Length; i++) { weights[i] /= sum; }
            return weights;
        }

        /// <summary>
        /// §8's damped preference: a group's vote shares given this election's compatibility and
        /// where the group already stood. <paramref name="priorShares"/> must sum to 1 (it is
        /// normalised defensively); <paramref name="loyalty"/> is the group's 0–100 value.
        /// </summary>
        public static double[] Preference(double[] compatibility, double[] priorShares, double loyalty)
        {
            if (priorShares == null || priorShares.Length != compatibility.Length)
            {
                throw new ArgumentException("prior shares must be one per party");
            }

            double[] persuaded = PersuadedShares(compatibility);
            double lambda = ElectionScales.Clamp(loyalty) / ElectionScales.Max;

            double priorSum = 0.0;
            foreach (double p in priorShares) { priorSum += p; }
            if (priorSum <= 0.0) { throw new ArgumentException("prior shares sum to zero"); }

            var result = new double[compatibility.Length];
            double total = 0.0;
            for (int i = 0; i < result.Length; i++)
            {
                result[i] = lambda * (priorShares[i] / priorSum) + (1.0 - lambda) * persuaded[i];
                total += result[i];
            }

            // Normalise against accumulated floating error; the shares are a distribution.
            for (int i = 0; i < result.Length; i++) { result[i] /= total; }
            return result;
        }
    }
}
