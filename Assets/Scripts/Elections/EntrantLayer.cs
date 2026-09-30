using System;
using System.Collections.Generic;
using PoliSim.Data;

namespace PoliSim.Elections
{
    /// <summary>
    /// §681 (Elias's ruling, 2026-09-30): **THE ENTRANT LAYER - AWARENESS AND FAMILY-GROUPED SUBSTITUTION, ONE FAMILY OF CHANGE.** Applied to a
    /// preference vector wherever the model forms one (the idle prediction, the campaign's daily truth, the backtests' loyalty runs), after
    /// `PreferenceModel` and before anything reads the shares.
    ///
    /// <para><b>What it changes, and only when.</b> An ENTRANT is a party with no share at the previous election (prior 0) - a party the
    /// model had to invent a vote for. With no entrant the input array is returned untouched, so every existing path is byte-identical.
    /// With one: (1) <b>awareness</b> - an entrant reaches only the voters aware of it, so its share is <c>a · p</c>, where <c>p</c> is what the
    /// preference gave it and <c>a</c> its awareness (1 for every real party - established parties seed at full awareness; a created party's
    /// from its Recognition, growing in the campaign); (2) <b>family-grouped substitution</b> - where that share comes from. The established
    /// parties' shares with the entrants removed (<c>s</c>) are the base; a nested logit over the expert survey's party families
    /// (<see cref="PartyFamilies"/>) is calibrated to reproduce <c>s</c> exactly, the entrants join their families' nests, and each entrant's
    /// attraction is solved so its share is <c>a · p</c>. A nest's λ is 1 (the draw proportional to size - the preference model's own property,
    /// confirmed by `EntrantLayerDiagnostic`) unless an entrant in it carries a grouping strength, which draws it first from its own family.</para>
    ///
    /// <para>⚠ <b>Who carries the grouping (measured, §681):</b> a CREATED party - its family is the model's own assignment and no election has
    /// measured its draw. A real entrant keeps the proportional draw: grouped with Die Linke (CHES family 6), BSW worsened every German 2025
    /// backtest (loyalty run 4.55 → 4.85 pp, both layers 5.01 → 5.37, the eight-party set 4.66 → 5.04) - the 2025 Linke rose while BSW took its
    /// share. Grouping real entrants too is the alternative, measured and recorded for Elias.</para>
    /// </summary>
    public static class EntrantLayer
    {
        /// <summary>[AUTHORED-DRAFT] a created party's family grouping strength, 1 - λ of its nest (0 = proportional to size, 1 = only its own
        /// family). On the play-calibration list.</summary>
        public const double GroupingStrength = 0.5;

        /// <summary>[AUTHORED-DRAFT] how fast a created party's awareness grows in a campaign: per unit of its coverage and of its campaigning
        /// pressure, each as a share of the leading party's. On the play-calibration list.</summary>
        public const double AwarenessPerCoverage = 1.0;
        public const double AwarenessPerPressure = 1.0;

        /// <summary>Awareness after a campaign's reach so far: <c>1 - (1 - a0) · exp(-(kc · coverage + kp · pressure))</c>, each input 0-1 against the leader.</summary>
        public static double GrownAwareness(double start, double coverageShare, double pressureShare) =>
            1.0 - (1.0 - Clamp01(start)) * Math.Exp(-(AwarenessPerCoverage * Clamp01(coverageShare) + AwarenessPerPressure * Clamp01(pressureShare)));

        private static double Clamp01(double v) => v < 0.0 ? 0.0 : v > 1.0 ? 1.0 : v;

        /// <summary>Each key's grouping strength: <see cref="GroupingStrength"/> for a created party, 0 for every real one (see the class note).
        /// Null where the country has no created party.</summary>
        public static double[] GroupingOf(CountryId country, IReadOnlyList<string> keys)
        {
            if (!CreatedParties.Any(country)) { return null; }
            var g = new double[keys.Count];
            for (int i = 0; i < g.Length; i++)
            {
                foreach (CreatedParty c in CreatedParties.Of(country)) { if (c.Key == keys[i]) { g[i] = GroupingStrength; } }
            }
            return g;
        }

        public static bool IsEntrant(double prior) => prior <= 0.0;

        /// <summary>The layer. <paramref name="preference"/> sums to 1; <paramref name="prior"/> marks the entrants (0); <paramref name="family"/> the
        /// party's family (-1 none: its own nest); <paramref name="awareness"/> null means every party at 1; <paramref name="grouping"/> null means
        /// every nest proportional. Returns the same array when nothing is an entrant.</summary>
        public static double[] Apply(double[] preference, double[] prior, int[] family, double[] awareness, double[] grouping)
        {
            if (preference == null || prior == null || prior.Length != preference.Length) { return preference; }
            int n = preference.Length;
            bool any = false;
            for (int i = 0; i < n; i++) { if (IsEntrant(prior[i])) { any = true; break; } }
            if (!any) { return preference; }

            // the base: the established parties with the entrants removed
            double established = 0.0;
            for (int i = 0; i < n; i++) { if (!IsEntrant(prior[i])) { established += preference[i]; } }
            if (established <= 0.0) { return preference; }
            var s = new double[n];
            for (int i = 0; i < n; i++) { s[i] = IsEntrant(prior[i]) ? 0.0 : preference[i] / established; }

            // nests: a family with members, or a party of its own (-1 or a unique id)
            var nestOf = new int[n];
            var nestIds = new Dictionary<int, int>();
            int nests = 0;
            for (int i = 0; i < n; i++)
            {
                int f = family != null && i < family.Length ? family[i] : -1;
                int key = f >= 0 ? f : -1000 - i;
                if (!nestIds.TryGetValue(key, out int k)) { k = nests++; nestIds[key] = k; }
                nestOf[i] = k;
            }
            var nestShare = new double[nests];
            for (int i = 0; i < n; i++) { nestShare[nestOf[i]] += s[i]; }

            // each nest's λ: 1 (proportional) unless an entrant in it carries a grouping strength - the strongest one's
            var lambda = new double[nests];
            for (int k = 0; k < nests; k++) { lambda[k] = 1.0; }
            for (int i = 0; i < n; i++)
            {
                if (!IsEntrant(prior[i]) || grouping == null || i >= grouping.Length) { continue; }
                lambda[nestOf[i]] = Math.Min(lambda[nestOf[i]], Math.Max(1e-3, 1.0 - Clamp01(grouping[i])));
            }

            // calibration: y_i = s_i · S_N^(1/λ_N - 1) reproduces s exactly (Y_N = S_N^(1/λ_N), P(N) = S_N)
            var y = new double[n];
            for (int i = 0; i < n; i++) { if (!IsEntrant(prior[i]) && s[i] > 0.0) { y[i] = s[i] * Math.Pow(nestShare[nestOf[i]], 1.0 / lambda[nestOf[i]] - 1.0); } }

            // the entrants' targets, a · p; their attractions solved so each lands on its target (Gauss-Seidel over the entrants)
            var target = new double[n];
            double targetSum = 0.0;
            for (int i = 0; i < n; i++)
            {
                if (!IsEntrant(prior[i])) { continue; }
                double a = awareness != null && i < awareness.Length ? Clamp01(awareness[i]) : 1.0;
                target[i] = a * preference[i];
                targetSum += target[i];
            }
            if (targetSum >= 0.999) { return preference; }   // nothing left to take from - the base cannot be defined
            for (int sweep = 0; sweep < 30; sweep++)
            {
                for (int e = 0; e < n; e++)
                {
                    if (!IsEntrant(prior[e])) { continue; }
                    if (target[e] <= 0.0) { y[e] = 0.0; continue; }
                    double lo = 0.0, hi = 1.0;
                    while (Shares(y, nestOf, nests, lambda, e, hi)[e] < target[e] && hi < 1e12) { hi *= 2.0; }
                    for (int it = 0; it < 80; it++)
                    {
                        double mid = 0.5 * (lo + hi);
                        if (Shares(y, nestOf, nests, lambda, e, mid)[e] < target[e]) { lo = mid; } else { hi = mid; }
                    }
                    y[e] = 0.5 * (lo + hi);
                }
            }
            return Shares(y, nestOf, nests, lambda, -1, 0.0);
        }

        /// <summary>The nested logit's shares for attractions <paramref name="y"/> (with party <paramref name="e"/>'s replaced by <paramref name="ye"/> when e ≥ 0).</summary>
        private static double[] Shares(double[] y, int[] nestOf, int nests, double[] lambda, int e, double ye)
        {
            int n = y.Length;
            var Y = new double[nests];
            for (int i = 0; i < n; i++) { Y[nestOf[i]] += i == e ? ye : y[i]; }
            double denom = 0.0;
            var W = new double[nests];
            for (int k = 0; k < nests; k++) { W[k] = Y[k] > 0.0 ? Math.Pow(Y[k], lambda[k]) : 0.0; denom += W[k]; }
            var p = new double[n];
            for (int i = 0; i < n; i++)
            {
                double yi = i == e ? ye : y[i];
                int k = nestOf[i];
                p[i] = denom > 0.0 && Y[k] > 0.0 ? W[k] / denom * yi / Y[k] : 0.0;
            }
            return p;
        }
    }
}
