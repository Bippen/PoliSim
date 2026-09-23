using System.Collections.Generic;
using PoliSim.Data;

namespace PoliSim.Elections
{
    /// <summary>
    /// D-5 (a) — **the declared refusals, per country, as political FACTS with citations.**
    ///
    /// <para><b>Why they are separated from the derived ones.</b> A derived red line is the model's own
    /// inference from two parties' positions: it says *"these two are far enough apart that we infer they
    /// would not sit together"*. A declared line is something a party actually said. Mixing them would
    /// let an inference wear a citation's authority — and, worse, would hide the fact that **only one of
    /// the six countries has its declarations on disk.**</para>
    ///
    /// <para>⚠ <b>SOURCED FOR SWEDEN ONLY, in two vintages.</b> K-1 (2026-09-23): the live game reads the
    /// declarations as of the 2026 election, `ElectionsData/sweden/2026/coalition_declarations_2026.md`; the
    /// backtests that assert 2022's government pin <see cref="ElectionVintage.Sweden2022"/>,
    /// `ElectionsData/sweden/coalition_declarations_2022.md`. For every other country `For` returns the DERIVED
    /// lines alone and `IsSourced` returns false, so a caller can say plainly that the government it formed was
    /// formed without that country's real declarations. **Inventing Germany's would be inventing the central
    /// political fact of its party system**, and a formation run without them can produce a cabinet that
    /// country would never form — which is a limitation to state, not to paper over.</para>
    ///
    /// <para>⚠ This is the ONE definition of Sweden's declared lines. `CoalitionFilm` reads it rather than
    /// keeping a second copy: two surfaces disagreeing about which coalitions are possible would be worse
    /// than either being wrong alone, which is `CoalitionFilm`'s own stated argument for existing.</para>
    /// </summary>
    public static class DeclaredRedLines
    {
        public const string SwedenSource = "See ElectionsData/sweden/2026/coalition_declarations_2026.md";
        public const string SwedenSource2022 = "See ElectionsData/sweden/coalition_declarations_2022.md";

        /// <summary>Whether this country's DECLARED lines are sourced. False means `For` returns derived
        /// lines alone.</summary>
        public static bool IsSourced(CountryId country) => country == CountryId.Sweden;

        /// <summary>The derived lines plus any declared ones this country has on disk, in the party order
        /// of <paramref name="parties"/>, as of <paramref name="vintage"/> - the seated election's unless a
        /// backtest pins 2022's.</summary>
        public static List<RedLine> For(CountryId country, IReadOnlyList<PoliticalParty> parties, ElectionVintage vintage = ElectionVintage.Seated)
        {
            var lrGen = new double[parties.Count];
            var galtan = new double[parties.Count];
            for (int p = 0; p < parties.Count; p++)
            {
                lrGen[p] = parties[p].LrGen;
                galtan[p] = parties[p].Galtan;
            }

            List<RedLine> lines = DerivedRedLines.From(lrGen, galtan);
            if (country != CountryId.Sweden) { return lines; }

            int s = IndexOf(parties, "SD"), c = IndexOf(parties, "C"), m = IndexOf(parties, "M");
            int kd = IndexOf(parties, "KD"), l = IndexOf(parties, "L"), v = IndexOf(parties, "V");

            if (vintage == ElectionVintage.Sweden2022)
            {
                if (s < 0) { return lines; }
                if (c >= 0)
                {
                    lines.Add(new RedLine(c, s, RedLineKind.Declared, blocksSupport: true,
                        basis: "DECLARED: Centerpartiet will not sit in or support a government dependent on SD - "
                               + "Loof, SVT Agenda 2017-05-14, verbatim; conduct 2022 (backed Andersson over Kristersson). " + SwedenSource2022));
                }

                const string NoSdMinisters = "DECLARED: promised in the 2022 campaign not to let SD sit in government, while "
                    + "accepting its support - Tidoavtalet 2022-10-14 (cabinet M+KD+L, SD outside with no ministerial post). " + SwedenSource2022;
                if (m >= 0) { lines.Add(new RedLine(m, s, RedLineKind.Declared, blocksSupport: false, basis: NoSdMinisters)); }
                if (kd >= 0) { lines.Add(new RedLine(kd, s, RedLineKind.Declared, blocksSupport: false, basis: NoSdMinisters)); }
                if (l >= 0) { lines.Add(new RedLine(l, s, RedLineKind.Declared, blocksSupport: false, basis: NoSdMinisters)); }
                return lines;
            }

            // THE 2026 ELECTION'S DECLARATIONS (K-1 part 2). Two lines, both Centerpartiet's; the 2022 no-SD-ministers
            // line is LIFTED for all three Tidö parties - M by the M-SD agreement of 2026-04-01 ([MSD-P1], whose own page does not
            // say SD will sit in government; that term is carried by the independent reports [MSD-I1]-[MSD-I4]), L by its
            // agreement of 2026-03-13 [L-P1] and its board's decision reported the same day [L-I1], KD by its own words as reported
            // 2026-09-08 [KD-I2] (secondary; KD's primary is a GAP) - so no line is written for them: an absent refusal is the fact.
            if (c >= 0 && s >= 0)
            {
                lines.Add(new RedLine(c, s, RedLineKind.Declared, blocksSupport: true,
                    basis: "DECLARED: Centerpartiet will not sit in or support a government that depends on SD or gives it influence - "
                           + "Thand Ringqvist's installation speech 2025-11-13 [C-P5], restated 2026-01-28 [C-P1], 2026-01-30 [C-I10], "
                           + "2026-08-11 [C-P2], 2026-09-08 [C-I1] and after the election 2026-09-14 [C-I6]. " + SwedenSource));
            }

            // THE ONE-WAY SHAPE (RedLine.OneWay, K-1). C refuses any cabinet that CONTAINS V - it will not sit in it, support it
            // or let it through ([C-I1] 2026-09-08, [C-I3] 2026-04-21, [C-P1] 2026-01-28) - and no fetched source that names a
            // mechanism has C refuse V as a mere supporter: TV4 2026-01-30 [C-I10] has that door "inte formellt stängd", and C
            // floated a pure S minority V would have to let through ([C-I8], secondary). Neither of the model's two symmetric
            // strengths says that - cabinet-blocking would let C prop up a cabinet with V in it, support-blocking would stop V
            // tolerating a cabinet C sits in - so the line runs from C to V only. V's own in-or-against demand ([V-P1]: it will
            // not support or let through a government it is not in) is a different shape again and is NOT held here: the
            // pairwise model cannot say it, and the declarations file lists it among what the model cannot hold.
            if (c >= 0 && v >= 0)
            {
                lines.Add(new RedLine(c, v, RedLineKind.Declared, blocksSupport: true, oneWay: true,
                    basis: "DECLARED: Centerpartiet will not sit in, support or let through a cabinet that contains V - "
                           + "first found 2026-01-28 [C-P1], restated 2026-01-30 [C-I10], 2026-04-21 [C-I3] and 2026-09-08 [C-I1] (\"hellre till extraval\"), "
                           + "held after the election 2026-09-14 [C-I6] and 2026-09-18 [C-I8]; one way - no fetched source that names a mechanism has C refuse V's support. "
                           + SwedenSource));
            }

            return lines;
        }

        private static int IndexOf(IReadOnlyList<PoliticalParty> parties, string abbrev)
        {
            for (int p = 0; p < parties.Count; p++)
            {
                if (parties[p].Abbrev == abbrev) { return p; }
            }

            return -1;
        }
    }
}
