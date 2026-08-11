using System;
using System.Collections.Generic;

namespace PoliSim.Data
{
    /// <summary>
    /// The USA's real institutions and its real 2024/119th-Congress starting position, seeded at
    /// <c>EpochDate</c> 2026-01-01.
    ///
    /// <para><b>Every figure carries a tag, and the tags are not decoration</b> - Master Roadmap rule 5,
    /// and rule 12's "a status describing the outside world is a cached value and needs an expiry":</para>
    ///
    /// <list type="bullet">
    /// <item><description><b>[VERIFIED]</b> - sourced, with the source named and a retrieval date of
    /// 2026-08-11.</description></item>
    /// <item><description><b>[APPROX]</b> - a real figure recalled but not re-sourced this pass.
    /// Usable, and flagged so it is re-checked before anything depends on its precision.</description></item>
    /// <item><description><b>[GAP]</b> - not found. Left as a gap with its consequence stated, because a
    /// wrong number is worse than a missing one and a placeholder that looks like data is exactly what
    /// rule 5 exists to prevent.</description></item>
    /// </list>
    ///
    /// <para><b>Named office-holders are absent by design.</b> The rule 9 reversal of 2026-08-11 covers
    /// parties only; no real politician is named anywhere in this file, and the player is the President.</para>
    /// </summary>
    public static class UnitedStatesSeed
    {
        // ─── Party ids ──────────────────────────────────────────────────────────────────────────────
        public const string Republican = "us-gop";
        public const string Democratic = "us-dem";
        public const string Libertarian = "us-lib";

        /// <summary>
        /// Everything else on the 2024 House ballot - other minor parties, independents, write-ins.
        ///
        /// <para><b>This bucket is not tidiness, it is a correctness requirement, and it was found by the
        /// check rather than by reading the code.</b> The three named parties sum to 97.41%, so
        /// normalising projected shares to 1 silently inflated all of them: a neutral election returned
        /// the Republicans 51.07% against a seeded 49.75%, a 1.3-point error present at every single
        /// election before any swing was applied. With the residual 2.59% carried explicitly the seed
        /// sums to exactly 1 and normalisation becomes the no-op it was always assumed to be.</para>
        /// </summary>
        public const string Other = "us-oth";

        /// <summary>The date the whole of this file describes. Anything re-seeded later must move this with it, or the two disagree silently.</summary>
        public const string RetrievedOn = "2026-08-11";

        /// <summary>
        /// [VERIFIED] The 2026 midterm elections fall on 2026-11-03 - the Tuesday after the first Monday
        /// in November.
        ///
        /// <para>This lands about ten months after <c>EpochDate</c>, which is a genuine piece of luck for
        /// the design: <b>a new player meets the entire American election system inside their first
        /// term</b>, rather than reading about it in a tooltip and encountering it three hours later.</para>
        /// </summary>
        public static readonly DateTime MidtermElectionDate = new DateTime(2026, 11, 3);

        /// <summary>[VERIFIED] Next presidential election, 2028-11-07.</summary>
        public static readonly DateTime PresidentialElectionDate = new DateTime(2028, 11, 7);

        // ─── The Electoral College ──────────────────────────────────────────────────────────────────

        /// <summary>[VERIFIED] 538 electors; 270 to win.</summary>
        public const int ElectoralVotesTotal = 538;
        public const int ElectoralVotesToWin = 270;

        /// <summary>[VERIFIED] 2024 result: 312 to the Republican ticket, 226 to the Democratic. Source: 2024 United States presidential election, retrieved 2026-08-11.</summary>
        public const int ElectoralVotes2024Republican = 312;
        public const int ElectoralVotes2024Democratic = 226;

        /// <summary>[VERIFIED] Republican national popular vote share, 2024 presidential - 49.8%, a plurality rather than a majority.</summary>
        public const double PresidentialPopularVote2024Republican = 0.498;

        /// <summary>[APPROX] Democratic share, ~48.3%. Recalled, not re-sourced this pass - the source consulted gave the Republican figure and not this one. Re-check before any margin calculation depends on it.</summary>
        public const double PresidentialPopularVote2024Democratic = 0.483;

        /// <summary>
        /// [GAP] Per-state 2024 margins and elector counts.
        ///
        /// <para><b>Consequence, stated rather than hidden:</b> without the state table the Electoral
        /// College cannot be simulated state by state, so <c>UnitedStatesElections</c> resolves it with a
        /// calibrated reduced-form curve instead - one that returns exactly 312-226 at the real 2024
        /// national shares by construction. That reproduces the seed and behaves sensibly under swing,
        /// and it genuinely cannot represent a popular-vote/Electoral-College split arising from a
        /// changed MAP rather than a changed national margin. Filling this table is the single highest-value
        /// upgrade to the American slice.</para>
        /// </summary>
        public const bool HasStateLevelElectoralData = false;

        // ─── Turnout ────────────────────────────────────────────────────────────────────────────────

        /// <summary>[VERIFIED] 2024 presidential turnout, 64.1% of the voting-eligible population. The Census CPS supplement reports 65.3% on its own (voting-age-citizen) basis; the two are different denominators, not a disagreement.</summary>
        public const double PresidentialTurnout2024 = 0.641;

        /// <summary>[APPROX] Midterm turnout, ~46%. The 2018 and 2022 midterms both landed near this, well above the ~36-41% typical before 2018. Not re-sourced this pass.</summary>
        public const double MidtermTurnoutBaseline = 0.46;

        /// <summary>
        /// [GAP] Turnout by age band from the Census CPS Voting and Registration Supplement.
        ///
        /// <para>The 2024 release was located but the age table was not retrieved - the press release
        /// gives overall (65.3%), sex and education breakdowns and points at a separate table package for
        /// age. The cohorts below therefore use a MODELLED curve, labelled as such, matching the two
        /// things that were verified: the national totals, and the well-established shape in which
        /// turnout rises monotonically with age and the youngest cohort falls furthest at a midterm.</para>
        ///
        /// <para>Source to fetch when this is closed:
        /// census.gov/data/datasets/2024/demo/cps/cps-voting.html</para>
        /// </summary>
        public const bool HasVerifiedCohortTurnout = false;

        /// <summary>
        /// MODELLED cohort turnout - not data. Shares and turnout rates are chosen so the weighted
        /// aggregate reproduces the two verified national figures (64.1% presidential, ~46% midterm);
        /// the distribution ACROSS bands is the modelled part.
        /// </summary>
        public static List<ElectorateCohort> BuildCohorts()
        {
            return new List<ElectorateCohort>
            {
                // Tuned 2026-08-11 so the weighted aggregate lands on the two VERIFIED nationals:
                // 64.07% against 64.1% presidential, 45.78% against ~46% midterm. The first draft of
                // these bands aggregated to 64.5%/47.0% and was corrected by the check, not by eye.
                new ElectorateCohort { Label = "18-24", ShareOfElectorate = 0.12, HighSalienceTurnout = 0.435, LowSalienceTurnout = 0.220 },
                new ElectorateCohort { Label = "25-44", ShareOfElectorate = 0.34, HighSalienceTurnout = 0.585, LowSalienceTurnout = 0.375 },
                new ElectorateCohort { Label = "45-64", ShareOfElectorate = 0.32, HighSalienceTurnout = 0.697, LowSalienceTurnout = 0.520 },
                new ElectorateCohort { Label = "65+",   ShareOfElectorate = 0.22, HighSalienceTurnout = 0.757, LowSalienceTurnout = 0.625 }
            };
        }

        // ─── Parties ────────────────────────────────────────────────────────────────────────────────

        /// <summary>
        /// [VERIFIED] Baselines are the 2024 US HOUSE national popular vote, not the presidential vote:
        /// R 49.75% (74,390,864), D 47.19% (70,571,330), Libertarian 0.47% (709,405). Source: 2024 United
        /// States House of Representatives elections, retrieved 2026-08-11.
        ///
        /// <para>The House vote is the right anchor because it is the only one of the two that is
        /// actually a party-vs-party national tally. A presidential share is a vote for a named person,
        /// and this game names no politicians.</para>
        ///
        /// <para><b>Cohort appeal is MODELLED</b>, as multipliers around 1.0 rather than shares. It
        /// encodes only the age gradient that is not seriously disputed, and is deliberately mild - a
        /// strong prior here would let the model manufacture confident results from an assumption.</para>
        /// </summary>
        public static List<PoliticalParty> BuildParties()
        {
            return new List<PoliticalParty>
            {
                new PoliticalParty
                {
                    Id = Republican,
                    NativeName = "Republican Party",
                    EnglishName = "Republican Party",
                    ShortCode = "GOP",
                    FiscalStance = -0.7f,
                    SocialStance = 0.6f,
                    BaselineVoteShare = 0.4975,
                    CohortAppeal = new[] { 0.78, 0.95, 1.08, 1.12 },
                    DisplayColor = 0xD22B2B,
                    MarkName = "mark_party_us_rep",
                    SeededFrom = "2024-11-05 US House national popular vote"
                },
                new PoliticalParty
                {
                    Id = Democratic,
                    NativeName = "Democratic Party",
                    EnglishName = "Democratic Party",
                    ShortCode = "DEM",
                    FiscalStance = 0.7f,
                    SocialStance = -0.6f,
                    BaselineVoteShare = 0.4719,
                    CohortAppeal = new[] { 1.24, 1.06, 0.93, 0.88 },
                    DisplayColor = 0x2B5FD2,
                    MarkName = "mark_party_us_dem",
                    SeededFrom = "2024-11-05 US House national popular vote"
                },
                new PoliticalParty
                {
                    Id = Libertarian,
                    NativeName = "Libertarian Party",
                    EnglishName = "Libertarian Party",
                    ShortCode = "LIB",
                    FiscalStance = -0.9f,
                    SocialStance = -0.2f,
                    BaselineVoteShare = 0.0047,
                    CohortAppeal = new[] { 1.30, 1.10, 0.90, 0.70 },
                    DisplayColor = 0xD8B72E,
                    SeededFrom = "2024-11-05 US House national popular vote"
                },
                new PoliticalParty
                {
                    Id = Other,
                    NativeName = "Other and independent",
                    EnglishName = "Other and independent",
                    ShortCode = "OTH",
                    FiscalStance = 0.0f,
                    SocialStance = 0.0f,
                    // 1 - 0.4975 - 0.4719 - 0.0047. Carried explicitly so the seed sums to exactly 1;
                    // see the Other constant for the 1.3-point error that omitting it caused.
                    BaselineVoteShare = 0.0259,
                    CohortAppeal = new[] { 1.15, 1.05, 0.95, 0.85 },
                    DisplayColor = 0x8A8A8A,
                    SeededFrom = "2024-11-05 US House national popular vote"
                }
            };
        }

        // ─── Chambers ───────────────────────────────────────────────────────────────────────────────

        /// <summary>
        /// [VERIFIED] House: 435 seats, two-year terms, whole chamber every cycle, single-member
        /// plurality, no threshold. Seeded R 220 / D 215 - the 119th Congress as elected, and the
        /// narrowest majority since 1930.
        /// </summary>
        public static Chamber BuildHouse()
        {
            var house = new Chamber
            {
                Name = "House of Representatives",
                TotalSeats = 435,
                TermDays = 730,
                Formula = ElectoralFormula.Fptp,
                Threshold = ThresholdRule.None,
                Renewal = ChamberRenewal.Whole,
                Constituencies = 435,
                LevellingSeats = 0
            };

            house.Seats[Republican] = 220;
            house.Seats[Democratic] = 215;
            return house;
        }

        /// <summary>
        /// [VERIFIED] Senate: 100 seats, six-year terms, staggered thirds {33, 33, 34}, Class 2 up on
        /// 2026-11-03. Seeded R 53 / D 47, the composition at the start of the 119th Congress.
        ///
        /// <para><b>[GAP] - the party split WITHIN Class 2 was not sourced.</b> Which specific seats are
        /// exposed in 2026 is what actually decides whether a midterm is winnable, so
        /// <c>UnitedStatesElections</c> currently exposes the class in proportion to the whole chamber
        /// and says so. That is a real simplification: a party can be at 53 seats and still be defending
        /// a badly-drawn map, and the model cannot currently represent that.</para>
        /// </summary>
        public static Chamber BuildSenate()
        {
            var senate = new Chamber
            {
                Name = "Senate",
                TotalSeats = 100,
                TermDays = 2192,
                Formula = ElectoralFormula.Fptp,
                Threshold = ThresholdRule.None,
                Renewal = ChamberRenewal.StaggeredThirds,
                ClassSeats = new[] { 33, 33, 34 },
                NextClassUp = 1,
                Constituencies = 100,
                LevellingSeats = 0
            };

            senate.Seats[Republican] = 53;
            senate.Seats[Democratic] = 47;
            return senate;
        }

        /// <summary>[VERIFIED] Vetoes are overridden by two-thirds of both chambers.</summary>
        public const double VetoOverrideFraction = 2.0 / 3.0;

        /// <summary>[VERIFIED] Cloture needs 60 of 100. A Senate convention rather than a constitutional rule, and modelled as a soft threshold: below 60 a bill is not blocked outright, it is slowed and diluted, which is what the filibuster actually does to legislation.</summary>
        public const int ClotureVotes = 60;
    }
}
