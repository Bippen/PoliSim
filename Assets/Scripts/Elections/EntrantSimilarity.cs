using System;
using System.Collections.Generic;
using PoliSim.Data;

namespace PoliSim.Elections
{
    /// <summary>
    /// §684 (Elias's ruling, 2026-09-30): **THE MEASURED ALTERNATIVE - SUBSTITUTION BY POSITION ACROSS THE SURVEY'S NINE DIMENSIONS, FOR EVERY
    /// ENTRANT.** One rule, no family label: an entrant keeps its share <c>a · p</c> (`EntrantLayer`'s awareness), and each established party loses in
    /// proportion to <c>share × similarity</c>, the similarity <c>exp(-D²/2)</c> over the nine CHES positions (lrgen, lrecon, galtan, EU position,
    /// environment, regions, spend-vs-tax, immigration, deregulation), each dimension scaled by its own spread across the chamber's real parties and
    /// <c>D²</c> the mean over the dimensions both parties carry. Every similarity equal reduces it to the proportional draw exactly; a loss is capped
    /// at the party's share and the remainder drawn again from the rest.
    /// </summary>
    public static class EntrantSimilarity
    {
        /// <summary>The nine positions a party carries (NaN where CHES has none).</summary>
        public static double[] Positions(in PoliticalParty p) => new double[]
        {
            p.LrGen, p.LrEcon, p.Galtan, p.EuPosition, p.Environment, p.Regions, p.SpendVsTax, p.ImmigratePolicy, p.Deregulation,
        };

        /// <summary>Each key's nine positions from the country's roster (created parties included), or null where the key is not in it.</summary>
        public static double[][] For(CountryId country, IReadOnlyList<string> keys)
        {
            IReadOnlyList<PoliticalParty> roster = PartySystems.For(country);
            var pos = new double[keys.Count][];
            for (int i = 0; i < keys.Count; i++)
            {
                foreach (PoliticalParty p in roster) { if (p.Abbrev == keys[i]) { pos[i] = Positions(p); break; } }
            }
            return pos;
        }

        /// <summary>Similarity of two position vectors, each dimension scaled by <paramref name="spread"/>: exp(-D²/2), D² the mean over the shared dimensions; 1 where none is shared.</summary>
        public static double Similarity(double[] a, double[] b, double[] spread)
        {
            if (a == null || b == null) { return 1.0; }
            double sum = 0.0;
            int k = 0;
            for (int d = 0; d < a.Length && d < b.Length; d++)
            {
                if (double.IsNaN(a[d]) || double.IsNaN(b[d]) || spread[d] <= 0.0) { continue; }
                double z = (a[d] - b[d]) / spread[d];
                sum += z * z;
                k++;
            }
            return k == 0 ? 1.0 : Math.Exp(-0.5 * sum / k);
        }

        /// <summary>The spread of each dimension across the established parties (population standard deviation of the ones that carry it).</summary>
        public static double[] Spread(double[][] positions, double[] prior)
        {
            int dims = 9;
            var spread = new double[dims];
            for (int d = 0; d < dims; d++)
            {
                var v = new List<double>();
                for (int i = 0; i < positions.Length; i++)
                {
                    if (EntrantLayer.IsEntrant(prior[i]) || positions[i] == null || double.IsNaN(positions[i][d])) { continue; }
                    v.Add(positions[i][d]);
                }
                if (v.Count < 2) { continue; }
                double mean = 0.0; foreach (double x in v) { mean += x; } mean /= v.Count;
                double ss = 0.0; foreach (double x in v) { ss += (x - mean) * (x - mean); }
                spread[d] = Math.Sqrt(ss / v.Count);
            }
            return spread;
        }

        /// <summary>The rule. Returns the same array when nothing is an entrant.</summary>
        public static double[] Apply(double[] preference, double[] prior, double[][] positions, double[] awareness)
        {
            if (preference == null || prior == null || prior.Length != preference.Length) { return preference; }
            int n = preference.Length;
            bool any = false;
            for (int i = 0; i < n; i++) { if (EntrantLayer.IsEntrant(prior[i])) { any = true; break; } }
            if (!any) { return preference; }

            double established = 0.0;
            for (int i = 0; i < n; i++) { if (!EntrantLayer.IsEntrant(prior[i])) { established += preference[i]; } }
            if (established <= 0.0) { return preference; }
            var result = new double[n];
            for (int i = 0; i < n; i++) { result[i] = EntrantLayer.IsEntrant(prior[i]) ? 0.0 : preference[i] / established; }
            double[] spread = positions != null ? Spread(positions, prior) : new double[9];

            for (int e = 0; e < n; e++)
            {
                if (!EntrantLayer.IsEntrant(prior[e])) { continue; }
                double a = awareness != null && e < awareness.Length ? Math.Max(0.0, Math.Min(1.0, awareness[e])) : 1.0;
                double take = a * preference[e];
                result[e] = take;
                // draw the take from the established parties by share × similarity, capping each at what it holds
                var open = new bool[n];
                for (int i = 0; i < n; i++) { open[i] = !EntrantLayer.IsEntrant(prior[i]) && result[i] > 0.0; }
                for (int round = 0; round < n && take > 1e-15; round++)
                {
                    double weightSum = 0.0;
                    var w = new double[n];
                    for (int i = 0; i < n; i++)
                    {
                        if (!open[i]) { continue; }
                        w[i] = result[i] * Similarity(positions?[e], positions?[i], spread);
                        weightSum += w[i];
                    }
                    if (weightSum <= 0.0) { break; }
                    double remaining = 0.0;
                    for (int i = 0; i < n; i++)
                    {
                        if (!open[i]) { continue; }
                        double loss = take * w[i] / weightSum;
                        if (loss >= result[i]) { remaining += loss - result[i]; result[i] = 0.0; open[i] = false; }
                        else { result[i] -= loss; }
                    }
                    take = remaining;
                }
            }
            return result;
        }
    }
}
