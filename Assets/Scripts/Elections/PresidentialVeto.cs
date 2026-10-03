using System;
using System.Collections.Generic;
using PoliSim.Data;

namespace PoliSim.Elections
{
    /// <summary>
    /// PS-5 (§761) - POLAND'S PRESIDENTIAL VETO AND ITS OVERRIDE, LIVE. §736 proved the constants against the held texts and §757 backtested the rule in an
    /// Editor instrument (`PresidentialVetoDiagnostic`); this is the runtime class the Sejm's statutes now pass through. Konstytucja Art. 122 ust. 2 and 5,
    /// Art. 224, Art. 235 ust. 7, Art. 96 ust. 1; Regulamin Sejmu Art. 64 (`ElectionsData/poland/veto.md`).
    /// <list type="bullet">
    /// <item><description><b>What may be vetoed</b> (Elias's ruling B1, 2026-10-02): an ordinary statute. Two kinds of act cannot be: the budget act
    /// (Art. 224 - the President signs it within 7 days and may only refer it to the Tribunal) and a constitutional amendment (Art. 235 ust. 7). An act
    /// RELATED to the budget (the "okołobudżetowa") is an ordinary statute and can be vetoed.</description></item>
    /// <item><description><b>Who vetoes what</b> (B1): the President vetoes an ordinary statute when a majority of his backing party's deputies voted
    /// against it - more than half of the club's members at the Sejm's vote on the whole bill. The rule is the game's, DECLARED; on the 10th Sejm's record
    /// it catches 45 of 53 vetoes (§757). In the game a party votes as one, so its side IS its members' majority.</description></item>
    /// <item><description><b>The override</b> (Elias's ruling B2): 3/5 of the deputies voting, abstentions in the base - required = ceil(0.6 × (yes + no +
    /// abstain)) - with 230 present, the quorum. PREMISE, stated: the re-pass is voted by the same sides as the passage (the same bill, the same parties);
    /// the veto and the vote on it are taken on the passage's day (Art. 122's 21 days and the Sejm's scheduling are not waited).</description></item>
    /// </list>
    /// </summary>
    public static class PresidentialVeto
    {
        /// <summary>SOURCED - Art. 96 ust. 1: the Sejm's 460 deputies.</summary>
        public const int StatutoryDeputies = 460;
        /// <summary>SOURCED - Art. 122 ust. 5; Regulamin Art. 64 ust. 5: a 3/5 majority.</summary>
        public const int OverrideNumerator = 3, OverrideDenominator = 5;

        /// <summary>SOURCED - Art. 122 ust. 5: "w obecności co najmniej połowy ustawowej liczby posłów" - 230 present.</summary>
        public static int Quorum => (StatutoryDeputies + 1) / 2;

        /// <summary>The kinds of act the veto rule tells apart.</summary>
        public enum Act { OrdinaryStatute, BudgetAct, ConstitutionalAmendment }

        /// <summary>The countries whose president holds a veto the game runs - Poland's (sourced, ruled); no other country's is modelled.</summary>
        public static bool Applies(CountryId country) => country == CountryId.Poland;

        /// <summary>B1: only an ordinary statute can be vetoed - not the budget act (Art. 224), not a constitutional amendment (Art. 235 ust. 7).</summary>
        public static bool MayVeto(Act act) => act == Act.OrdinaryStatute;

        /// <summary>B1 (the game's rule, DECLARED): the President vetoes an ordinary statute a majority of his backing party's deputies voted against -
        /// more than half of the club's <paramref name="clubMembers"/> voting NO.</summary>
        public static bool Vetoes(Act act, int clubMembers, int clubNo) => MayVeto(act) && clubMembers > 0 && clubNo * 2 > clubMembers;

        /// <summary>B2: the votes an override needs - 3/5 of those voting, abstentions in the base, rounded up: ceil(0.6 × (yes + no + abstain)).</summary>
        public static int OverrideRequired(int yes, int no, int abstain)
        {
            int voting = yes + no + abstain;
            return (OverrideNumerator * voting + OverrideDenominator - 1) / OverrideDenominator;
        }

        /// <summary>B2: whether the Sejm's re-pass overrides - the quorum present and YES at least the 3/5 of those voting.</summary>
        public static bool Overrides(int yes, int no, int abstain, int present) => present >= Quorum && yes >= OverrideRequired(yes, no, abstain);

        /// <summary>One statute's passage through the veto: whether the President vetoed it, and - vetoed - the override's count.</summary>
        public sealed class Outcome
        {
            public string President;
            public string BackingParty;
            public bool Vetoed;
            public int Yes, No, Abstain, Required;
            public bool Overridden;
            /// <summary>Whether the statute takes effect - passed and not vetoed, or vetoed and overridden.</summary>
            public bool Stands => !Vetoed || Overridden;
        }

        /// <summary>
        /// The veto on a statute the Sejm has just passed, read off the passage's division (<paramref name="sides"/>): the President on the day and his backing
        /// party - §770: the game's president once its own election has seated one, else the record's (<see cref="PresidentialElection.PresidentAt"/> on
        /// <paramref name="held"/>, the country's own elections; null reads the record alone); vetoed where that party voted against; the override counted
        /// on the same sides. Null where the country's president holds no veto the game runs, or none is in office on the day.
        /// </summary>
        public static Outcome Decide(CountryId country, IReadOnlyList<PresidentialElection.Contest> held, DateTime date, Act act, IReadOnlyList<DivisionSide> sides)
        {
            if (!Applies(country) || sides == null || !PresidentialElection.PresidentAt(country, held, date, out PresidencyOfRecord.President president)) { return null; }
            var outcome = new Outcome { President = president.Name, BackingParty = president.BackingParty };
            int clubMembers = 0, clubNo = 0;
            foreach (DivisionSide side in sides)
            {
                if (side.Side > 0) { outcome.Yes += side.Seats; } else if (side.Side < 0) { outcome.No += side.Seats; } else { outcome.Abstain += side.Seats; }
                if (side.Abbrev == president.BackingParty) { clubMembers += side.Seats; if (side.Side < 0) { clubNo += side.Seats; } }
            }
            outcome.Vetoed = Vetoes(act, clubMembers, clubNo);
            outcome.Required = OverrideRequired(outcome.Yes, outcome.No, outcome.Abstain);
            outcome.Overridden = outcome.Vetoed && Overrides(outcome.Yes, outcome.No, outcome.Abstain, outcome.Yes + outcome.No + outcome.Abstain);
            return outcome;
        }
    }
}
