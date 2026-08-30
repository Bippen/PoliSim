using System.Collections.Generic;
using PoliSim.Elections;

namespace PoliSim.Testing
{
    /// <summary>
    /// W-E8's staging, in ONE place so the coalition HARNESS and the coalition SCREEN read the same
    /// compatibility matrix and the same red lines. Two surfaces disagreeing about which coalitions
    /// are possible would be worse than either being wrong alone — the same argument that put
    /// `ElectionNightFilm` between W-E6's harness and its film.
    ///
    /// Positions are SOURCED: CHES 2024, `ElectionsData/positions/party_positions.md`, in the
    /// driver's party order (S, SD, M, V, C, KD, MP, L). The declared lines are SOURCED with their
    /// citations in `ElectionsData/sweden/coalition_declarations_2022.md`.
    /// </summary>
    public static class CoalitionFilm
    {
        private const int S = 0, SD = 1, M = 2, V = 3, C = 4, KD = 5, MP = 6, L = 7;

        public static readonly double[] Lrgen = { 3.74, 8.53, 7.58, 1.58, 5.95, 8.00, 2.74, 6.74 };
        public static readonly double[] Lrecon = { 3.68, 6.32, 7.89, 1.89, 7.84, 7.26, 3.16, 7.32 };
        public static readonly double[] Galtan = { 4.74, 9.00, 6.47, 2.42, 2.95, 7.79, 1.95, 4.47 };
        public static readonly double[] EuPosition = { 5.74, 2.68, 5.74, 3.32, 6.11, 5.35, 5.32, 6.84 };

        /// <summary>§29's party-to-party compatibility, DERIVED from the sourced positions.</summary>
        public static double[,] Compatibility()
        {
            int n = Lrgen.Length;
            var m = new double[n, n];
            for (int a = 0; a < n; a++)
            {
                for (int b = 0; b < n; b++)
                {
                    if (a == b) { m[a, b] = 100.0; continue; }
                    double ideological = CoalitionCompatibility.FromDistance(Lrgen[a] - Lrgen[b]);
                    double policy = CoalitionCompatibility.OverAxes(
                        new[] { Lrecon[a], Galtan[a], CoalitionCompatibility.RescaleEu(EuPosition[a]) },
                        new[] { Lrecon[b], Galtan[b], CoalitionCompatibility.RescaleEu(EuPosition[b]) });
                    m[a, b] = CoalitionCompatibility.WeightIdeological * ideological
                        + CoalitionCompatibility.WeightPolicy * policy;
                }
            }

            return m;
        }

        /// <summary>The DERIVED lines alone - the counterfactual that shows what the declarations are doing.</summary>
        public static List<RedLine> DerivedOnly() => DerivedRedLines.From(Lrgen, Galtan);

        /// <summary>Derived plus the DECLARED lines, each carrying its citation.</summary>
        public static List<RedLine> AllLines()
        {
            List<RedLine> lines = DerivedOnly();
            lines.Add(new RedLine(C, SD, RedLineKind.Declared, blocksSupport: true,
                basis: "DECLARED: Centerpartiet will not sit in or support a government dependent on SD - Loof, SVT Agenda 2017-05-14, verbatim; conduct 2022 (backed Andersson over Kristersson). See ElectionsData/sweden/coalition_declarations_2022.md"));

            const string noSdMinisters = "DECLARED: promised in the 2022 campaign not to let SD sit in government, while accepting its support - Tidoavtalet 2022-10-14 (cabinet M+KD+L, SD outside with no ministerial post). See ElectionsData/sweden/coalition_declarations_2022.md";
            lines.Add(new RedLine(M, SD, RedLineKind.Declared, blocksSupport: false, basis: noSdMinisters));
            lines.Add(new RedLine(KD, SD, RedLineKind.Declared, blocksSupport: false, basis: noSdMinisters));
            lines.Add(new RedLine(L, SD, RedLineKind.Declared, blocksSupport: false, basis: noSdMinisters));
            return lines;
        }
    }
}
