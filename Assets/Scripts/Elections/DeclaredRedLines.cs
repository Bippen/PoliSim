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
    /// <para>⚠ <b>SOURCED FOR SWEDEN 2022 ONLY.</b> `ElectionsData/sweden/coalition_declarations_2022.md`
    /// carries the citations. For every other country `For` returns the DERIVED lines alone and
    /// `IsSourced` returns false, so a caller can say plainly that the government it formed was formed
    /// without that country's real declarations. **Inventing Germany's would be inventing the central
    /// political fact of its party system**, and a formation run without them can produce a cabinet that
    /// country would never form — which is a limitation to state, not to paper over.</para>
    ///
    /// <para>⚠ This is the ONE definition of Sweden's declared lines. `CoalitionFilm` reads it rather than
    /// keeping a second copy: two surfaces disagreeing about which coalitions are possible would be worse
    /// than either being wrong alone, which is `CoalitionFilm`'s own stated argument for existing.</para>
    /// </summary>
    public static class DeclaredRedLines
    {
        public const string SwedenSource = "See ElectionsData/sweden/coalition_declarations_2022.md";

        /// <summary>Whether this country's DECLARED lines are sourced. False means `For` returns derived
        /// lines alone.</summary>
        public static bool IsSourced(CountryId country) => country == CountryId.Sweden;

        /// <summary>The derived lines plus any declared ones this country has on disk, in the party order
        /// of <paramref name="parties"/>.</summary>
        public static List<RedLine> For(CountryId country, IReadOnlyList<PoliticalParty> parties)
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
            int kd = IndexOf(parties, "KD"), l = IndexOf(parties, "L");
            if (s < 0) { return lines; }

            if (c >= 0)
            {
                lines.Add(new RedLine(c, s, RedLineKind.Declared, blocksSupport: true,
                    basis: "DECLARED: Centerpartiet will not sit in or support a government dependent on SD - "
                           + "Loof, SVT Agenda 2017-05-14, verbatim; conduct 2022 (backed Andersson over Kristersson). " + SwedenSource));
            }

            const string NoSdMinisters = "DECLARED: promised in the 2022 campaign not to let SD sit in government, while "
                + "accepting its support - Tidoavtalet 2022-10-14 (cabinet M+KD+L, SD outside with no ministerial post). " + SwedenSource;
            if (m >= 0) { lines.Add(new RedLine(m, s, RedLineKind.Declared, blocksSupport: false, basis: NoSdMinisters)); }
            if (kd >= 0) { lines.Add(new RedLine(kd, s, RedLineKind.Declared, blocksSupport: false, basis: NoSdMinisters)); }
            if (l >= 0) { lines.Add(new RedLine(l, s, RedLineKind.Declared, blocksSupport: false, basis: NoSdMinisters)); }
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
