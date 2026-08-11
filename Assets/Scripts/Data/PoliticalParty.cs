using System.Collections.Generic;

namespace PoliSim.Data
{
    /// <summary>
    /// A real political party, as an INSTANCE rather than an enum member.
    ///
    /// <para><b>This replaces <see cref="PartyArchetype"/>'s four shared fictional archetypes, under a
    /// deliberate reversal of Master Roadmap working-discipline rule 9 taken by Elias on 2026-08-11.</b>
    /// The reversal is a SPLIT, not a blanket one, and the half that did not move matters as much as the
    /// half that did:</para>
    ///
    /// <list type="bullet">
    /// <item><description><b>Parties — reversed.</b> Real names, real vote shares, real seat counts. A
    /// party is an institution.</description></item>
    /// <item><description><b>People — unchanged.</b> Cabinet ministers, party leaders, legislators, Fed
    /// Chairs and heads of state stay original and fictional, exactly as the Fed Chair rule established.
    /// A politician is a person.</description></item>
    /// </list>
    ///
    /// <para><b>Why an enum could not carry this.</b> Six countries hold 40+ parties between them, each
    /// with its own baseline share, ideological placement and coalition compatibility. An enum member
    /// carries a name and nothing else, which is precisely why <see cref="PartyArchetypeData"/> had to
    /// keep its constants in a parallel static switch - a shape that only stayed correct because there
    /// were four of them and they were the same in every country. Neither condition survives real
    /// parties.</para>
    ///
    /// <para>⚠ <b>These are cached values describing the outside world, and they expire</b> - Master
    /// Roadmap rule 12. Sweden votes 2026-09-13. Every seeded party carries the retrieval date of the
    /// election it was seeded from, so a stale set is visible rather than merely wrong.</para>
    /// </summary>
    public class PoliticalParty
    {
        /// <summary>Stable identifier used in save data and lookups - never displayed. Kept separate from the display names so a re-seed after an election cannot silently repoint saved references.</summary>
        public string Id;

        /// <summary>The party's name in its own language ("Sverigedemokraterna"). What a player in that country would actually see on a ballot.</summary>
        public string NativeName;

        /// <summary>English rendering ("Sweden Democrats"), for UI locales that need it. Equal to <see cref="NativeName"/> for anglophone countries rather than null, so no call site has to branch.</summary>
        public string EnglishName;

        /// <summary>Ballot-paper abbreviation ("SD", "CDU", "PiS", "GOP"). The only form that fits a hemicycle legend or a narrow ledger column.</summary>
        public string ShortCode;

        /// <summary>
        /// -1 (favours lower taxes) to +1 (favours higher taxes and more government revenue).
        ///
        /// <para>Deliberately the SAME axis and the same range <see cref="PartyArchetypeData.GetFiscalStance"/>
        /// already used, because <c>ParliamentSystem</c>'s seat-weighted bill scoring reads it unchanged.
        /// Real parties are a data change to that system, not a rewrite of it - the whole point of
        /// keeping the axis identical.</para>
        /// </summary>
        public float FiscalStance;

        /// <summary>-1 (socially liberal) to +1 (socially conservative). Second axis for <c>PoliticalCompassRenderer</c>, which already plots two dimensions and currently has only one real one to plot.</summary>
        public float SocialStance;

        /// <summary>
        /// Vote share at the election this party was seeded from, 0-1. The anchor the swing model in
        /// <see cref="PoliSim.Simulation.NationalVoteModel"/> moves away from.
        ///
        /// <para>Seeded from real vote COUNTS where available rather than published percentages. That is
        /// not fussiness: rounding Germany's 2025 shares to one decimal moves a Bundestag seat, measured
        /// 2026-08-11, and it took a sensitivity sweep to establish that the allocator was right and the
        /// input was not.</para>
        /// </summary>
        public double BaselineVoteShare;

        /// <summary>Relative appeal to each <see cref="ElectorateCohort"/>, indexed to match the country's cohort array. A multiplier around 1.0, not a share - 1.2 means this cohort breaks 20% more strongly for this party than the electorate as a whole.</summary>
        public double[] CohortAppeal;

        /// <summary>Exempt from the electoral threshold entirely - Germany's SSW, Italy's SVP. A property of the party under its country's law, not of the chamber's rule, which is why it lives here and not on <see cref="ThresholdRule"/>.</summary>
        public bool IsRecognisedMinority;

        /// <summary>Party ids this one will sit in government with. Asymmetric on purpose: a party may rule another out without being ruled out in return, which is how real cordons sanitaires behave.</summary>
        public List<string> CoalitionCompatibleWith = new List<string>();

        /// <summary>Packed 0xRRGGBB brand colour, for the hemicycle, the compass and the results screens. The party's real colour - the one thing about a party a player recognises before they read the label.</summary>
        public int DisplayColor;

        /// <summary>
        /// Filename of this party's ballot-stamp mark in the `mark_party_*` family, or null until one is
        /// drawn. Loaded via <c>IconLibrary.GetPartyMark</c> and tinted from <see cref="DisplayColor"/> -
        /// see that method for why marks tint and emblems do not.
        /// </summary>
        public string MarkName;

        /// <summary>The election this party's figures were taken from, ISO date. Rule 12's expiry made visible: without it a superseded figure and a wrong figure are indistinguishable by any available test.</summary>
        public string SeededFrom;

        public override string ToString()
        {
            return $"{ShortCode} ({EnglishName}) {BaselineVoteShare:P2} @ {SeededFrom}";
        }
    }
}
