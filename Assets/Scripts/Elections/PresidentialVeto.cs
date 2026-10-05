using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
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
    /// <item><description><b>Which statutes are at risk</b> (Elias's ruling F3, widening B1): a statute the President's backing party did not vote for - a
    /// majority of its VOTING members voted no or abstained, the absent outside the count (<see cref="AtRisk"/>). In the game a party votes as one, so its
    /// side IS that majority: against or undecided puts the statute at risk - and, READING, DECLARED, so does unmeasured (a party with no position on the
    /// bill's axes is recorded abstaining, so it does not vote for it); a backing party with no seat puts nothing at risk (<see cref="AtRisk"/>'s
    /// reading).</description></item>
    /// <item><description><b>Whether the President vetoes</b> (F3): a seeded draw (<see cref="Draw"/>) at the president's own rate refitted on the widened
    /// base - the record's vetoes among its statutes at risk, generated from the Sejm's own record (`Generated.PolishVetoRates`, `Tools/veto_b1_backtest.pl`,
    /// `docs/generated/VETO_B1_BACKTEST.md`) - and the pooled rate for a president with no record (<see cref="RateOf"/>). The draw is the gate's alone
    /// (`SimulationManager.PresidentialVetoGate`); a projection reads the risk, never a decision.</description></item>
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

        /// <summary>F3 (widening B1): an ordinary statute is AT RISK of the veto when the President's backing party did not vote for it - a majority of its
        /// voting members voted no (<paramref name="clubNo"/>) or abstained (<paramref name="clubAbstain"/>) against <paramref name="clubYes"/>; the
        /// absent are outside the count. READING, stated (the ruling's colon read as its test): a club with no member voting - a backing party that holds
        /// no seat - puts nothing at risk, for no majority of its voting members can be reached.</summary>
        public static bool AtRisk(Act act, int clubYes, int clubNo, int clubAbstain) => MayVeto(act) && clubNo + clubAbstain > clubYes;

        /// <summary>
        /// F3: the rate a statute at risk is vetoed at - the president's own, refitted on the widened base (`Generated.PolishVetoRates`: the statutes at risk
        /// the record shows decided in that president's term, and those vetoed), matched by the name as the record writes it, so a game that re-elects a
        /// president of record keeps the record's rate; the pooled rate of every president on the record where the president has none of their own.
        /// READING, stated (as the tool states it): a referral to the Tribunal is a decided act at risk and not a veto, so it counts in the base.
        /// <paramref name="basis"/> names which, with the record's two counts.
        /// </summary>
        public static double RateOf(string president, out string basis)
        {
            int atRisk = 0, vetoed = 0;
            foreach ((string name, int risked, int vetoedOf) in Generated.PolishVetoRates.OfRecord)
            {
                if (name == president)
                {
                    basis = "the president's own rate on the record, " + Count(vetoedOf) + " of " + Count(risked) + " at risk vetoed";
                    return risked > 0 ? (double)vetoedOf / risked : 0.0;
                }
                atRisk += risked;
                vetoed += vetoedOf;
            }
            basis = "the pooled rate - no record of the president's own - " + Count(vetoed) + " of " + Count(atRisk) + " at risk vetoed";
            return atRisk > 0 ? (double)vetoed / atRisk : 0.0;
        }

        private static string Count(int n) => n.ToString(CultureInfo.InvariantCulture);

        /// <summary>
        /// F3: THE SEEDED DRAW - a number in [0, 1) from the game's master seed and the act itself: the country, the day and the division's title. The same
        /// act on the same day draws the same in every world that resolves it that day - the played one, a reload, the shadow and a fork where they reach it
        /// on the played day - and no random stream moves (a stream draw would shift every later draw on it, and a fork that skips the day ticks would draw
        /// apart). PREMISE, stated: a world that resolves the act on another day draws it apart (the impact ledger's forks resolve the boundary later than
        /// the played world - a timing of their own, outside the veto). Two divisions with one title on one day are one act to the draw: the President
        /// answers the same text the same way that day. FNV-1a 64 over the key's UTF-8, finished with MurmurHash3's 64-bit mix; the top 53 bits are the
        /// fraction.
        /// </summary>
        public static double Draw(int masterSeed, CountryId country, DateTime date, string title)
        {
            if (_planted.HasValue) { return _planted.Value; }
            string key = masterSeed.ToString(CultureInfo.InvariantCulture) + "|" + country + "|" + date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) + "|" + (title ?? string.Empty);
            ulong h = 14695981039346656037UL;
            foreach (byte b in Encoding.UTF8.GetBytes(key)) { h ^= b; h *= 1099511628211UL; }
            h ^= h >> 33; h *= 0xff51afd7ed558ccdUL; h ^= h >> 33; h *= 0xc4ceb9fe1a85ec53UL; h ^= h >> 33;
            return (h >> 11) * (1.0 / (1UL << 53));
        }

        [ThreadStatic] private static double? _planted;

        /// <summary>For the Editor's checks and the film's staged frames: every <see cref="Draw"/> inside the returned scope is <paramref name="u"/> - 0
        /// vetoes every statute at risk, 1 signs every one - so the gate's two branches can be walked by name. Disposing the scope restores the draw.</summary>
        public static IDisposable PlantDraw(double u)
        {
            var restore = new PlantedScope(_planted);
            _planted = u;
            return restore;
        }

        private sealed class PlantedScope : IDisposable
        {
            private readonly double? _before;
            public PlantedScope(double? before) { _before = before; }
            public void Dispose() { _planted = _before; }
        }

        /// <summary>B2: the votes an override needs - 3/5 of those voting, abstentions in the base, rounded up: ceil(0.6 × (yes + no + abstain)).</summary>
        public static int OverrideRequired(int yes, int no, int abstain)
        {
            int voting = yes + no + abstain;
            return (OverrideNumerator * voting + OverrideDenominator - 1) / OverrideDenominator;
        }

        /// <summary>B2: whether the Sejm's re-pass overrides - the quorum present and YES at least the 3/5 of those voting.</summary>
        public static bool Overrides(int yes, int no, int abstain, int present) => present >= Quorum && yes >= OverrideRequired(yes, no, abstain);

        /// <summary>One statute's passage through the veto: whether it is at risk and at what rate, the override's count, and - once the gate has drawn - the
        /// President's answer.</summary>
        public sealed class Outcome
        {
            public string President;
            public string BackingParty;
            /// <summary>F3: the backing party did not vote for the statute.</summary>
            public bool AtRisk;
            /// <summary>F3: the chance the President vetoes it - the president's rate where at risk, 0 where not.</summary>
            public double Risk;
            /// <summary>F3: whose rate the risk is, in words (<see cref="RateOf"/>).</summary>
            public string RiskBasis;
            /// <summary>The backing party's members voting on the passage (yes, no, abstaining) - none where it holds no seat.</summary>
            public int ClubVoting;
            public int Yes, No, Abstain, Required;
            /// <summary>B2: whether a veto would be overridden - the quorum present and 3/5 of those voting for the re-pass, on the passage's sides.</summary>
            public bool OverrideCarries;
            /// <summary>Whether the President vetoed it - decided by the gate's draw (<see cref="DrawOn"/>); false on a projection, which reads the risk.</summary>
            public bool Vetoed { get; private set; }
            /// <summary>Whether the veto was overridden.</summary>
            public bool Overridden => Vetoed && OverrideCarries;
            /// <summary>Whether the statute takes effect - passed and not vetoed, or vetoed and overridden.</summary>
            public bool Stands => !Vetoed || Overridden;
            /// <summary>F3: the gate's decision - vetoed where the statute is at risk and the draw <paramref name="u"/> falls below the risk.</summary>
            public void DrawOn(double u) { Vetoed = AtRisk && u < Risk; }
        }

        /// <summary>
        /// The veto on a statute the Sejm has just passed, read off the passage's division (<paramref name="sides"/>): the President on the day and the
        /// backing party - §770: the game's president once its own election has seated one, else the record's (<see cref="PresidentialElection.PresidentAt"/>
        /// on <paramref name="held"/>, the country's own elections; null reads the record alone); at risk where that party did not vote for it (F3), at the
        /// president's rate; the override counted on the same sides. Nothing is decided here - the gate draws (<see cref="Outcome.DrawOn"/>). Null where the
        /// country's president holds no veto the game runs, or none is in office on the day. READING, stated, Elias's to rule: a president no party backs
        /// (a game-elected candidate who stood for none) has no club, so - as a backing party with no seat (<see cref="AtRisk"/>) - no statute is at risk;
        /// the other reading, every ordinary statute at risk at the pooled rate, waits for his word.
        /// </summary>
        public static Outcome Decide(CountryId country, IReadOnlyList<PresidentialElection.Contest> held, DateTime date, Act act, IReadOnlyList<DivisionSide> sides)
        {
            if (!Applies(country) || sides == null || !PresidentialElection.PresidentAt(country, held, date, out PresidencyOfRecord.President president)) { return null; }
            var outcome = new Outcome { President = president.Name, BackingParty = president.BackingParty };
            int clubYes = 0, clubNo = 0, clubAbstain = 0;
            foreach (DivisionSide side in sides)
            {
                if (side.Side > 0) { outcome.Yes += side.Seats; } else if (side.Side < 0) { outcome.No += side.Seats; } else { outcome.Abstain += side.Seats; }
                if (side.Abbrev == null || side.Abbrev != president.BackingParty) { continue; }
                if (side.Side > 0) { clubYes += side.Seats; } else if (side.Side < 0) { clubNo += side.Seats; } else { clubAbstain += side.Seats; }
            }
            outcome.ClubVoting = clubYes + clubNo + clubAbstain;
            outcome.AtRisk = AtRisk(act, clubYes, clubNo, clubAbstain);
            double rate = RateOf(president.Name, out string basis);
            outcome.Risk = outcome.AtRisk ? rate : 0.0;
            outcome.RiskBasis = basis;
            outcome.Required = OverrideRequired(outcome.Yes, outcome.No, outcome.Abstain);
            outcome.OverrideCarries = Overrides(outcome.Yes, outcome.No, outcome.Abstain, outcome.Yes + outcome.No + outcome.Abstain);
            return outcome;
        }
    }
}
