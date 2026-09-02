using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using PoliSim.Data;
using PoliSim.Elections.Generated;

namespace PoliSim.Elections
{
    /// <summary>
    /// C-R4b step 2 (2026-09-02) — **a campaign <see cref="CampaignRun.Setup"/> from what the game
    /// already carries, not from a harness's private tables.** Its first game-path caller is
    /// `SimulationManager.AdvanceCampaign` (C-R4b step 3), which is why it lives in the runtime assembly - it
    /// sat in the Editor assembly for one commit, until that caller existed (`UnwiredSubsystemCheck`). Until this the only builder of a Setup
    /// was `CampaignAiHarness.BuildSetup`, in the Editor assembly, reading its own copies of Sweden's
    /// 2022 and 2018 shares and its own copy of the valkrets file; the game path had nothing to hand
    /// `CampaignRun`. This is the same staging, one type, from the runtime tables, ready to move with its first game-path consumer; every input
    /// taken from the runtime table that already holds it:
    ///
    /// - the parties and their order: `PartySystems.TryHistory(Sweden)`'s order (S, SD, M, V, C, KD, MP, L);
    /// - the prior: the 2022 national shares, and loyalty from 2022 against 2018 (`LoyaltyModel`, W-A1);
    /// - the regions: the 29 valkretsar and their VALID votes, `SwedishValkretsReturns2022` (W-F1);
    /// - compatibility: DERIVED at the fixed point where an idle campaign reproduces 2022 exactly;
    /// - salience: SOURCED, Eurobarometer 105 (Spring 2026), Sweden, the four top-five issues §6 has a slot for.
    ///
    /// ⚠ **What is still [AUTHORED-DRAFT] is labelled field by field and is the same draft the harness
    /// has run since W-C1**: which personality each party plays (§32's descriptions cast onto the
    /// eight), the candidates' attributes, the offices each personality opens, the staff it hires, the
    /// war chest (D-1 (c): equal, 2 400 000 kr), the volunteers, the flat issue-match and credibility.
    /// Moving them here changes nothing about their provenance; it changes who can call them.
    /// Sweden is the only country with a staged campaign - the spec's own first case (§3's calendar is
    /// `CampaignCalendar.Sweden2026`) - and <see cref="TryFor"/> says so for the other five rather than
    /// inventing one.
    ///
    /// **Proven by the harness's own digest:** `CampaignAiHarness.BuildSetup` now delegates here, and
    /// its decision digest under seed 777 is the one it had before the move.
    /// </summary>
    public static class LiveCampaignSetup
    {
        /// <summary>DERIVED scaling anchor: the largest party's compatibility at the fixed point; the rest follow `PreferenceModel.Sharpness`.</summary>
        public const double CompatibilityCeiling = 70.0;
        /// <summary>[AUTHORED-DRAFT] W-F2 sources per-issue positions; until then every party half-matches every issue (§36's "no information", not a measured middle).</summary>
        public const double FlatIssueMatch = 0.5;
        /// <summary>[AUTHORED-DRAFT] W-F6 sources candidate credibility.</summary>
        public const double FlatCredibility = 0.6;
        /// <summary>[AUTHORED-DRAFT] D-1 (c): the equal war chest, a playability scale - `DATA_BILL.md` carries the bill for a sourced one.</summary>
        public const double WarChest = 2_400_000.0;
        /// <summary>[AUTHORED-DRAFT] W-B11: 800 volunteers × 3 h a day, equal for all by design (W-B4's offices grow them).</summary>
        public const int Volunteers = 800;
        /// <summary>[AUTHORED-DRAFT] W-B4: what each staged office puts into its own daily ground operation (400 doors a day at 5 kr).</summary>
        public const double OfficeOperationsPerDay = 2_000.0;
        /// <summary>CONVENTION - how often the published tracker fields, in days; W-E4's ladder.</summary>
        public const int PublicPollEveryDays = 7;

        /// <summary>[AUTHORED-DRAFT] §32's five personalities cast onto Sweden's eight, in `TryHistory`'s order: S professional, SD populist, M establishment, V grassroots, C chaotic, KD establishment, MP grassroots, L professional.</summary>
        public static readonly AiPersonality[] SwedenPersonalities =
        {
            AiPersonality.Professional, AiPersonality.Populist, AiPersonality.Establishment, AiPersonality.Grassroots,
            AiPersonality.Chaotic, AiPersonality.Establishment, AiPersonality.Grassroots, AiPersonality.Professional,
        };

        /// <summary>The party keys in the order the staging uses - `PartySystems.TryHistory(Sweden)`'s, which is `SwedishValkretsReturns2022.Parties`' order too.</summary>
        public static readonly string[] SwedenParties = { "S", "SD", "M", "V", "C", "KD", "MP", "L" };

        /// <summary>
        /// The country's staged campaign, or false with the reason when none is staged. Only Sweden today.
        /// <paramref name="scandals"/> is the caller's staging (the harness stages one; a game passes none
        /// until §17's dynamic generation exists).
        /// </summary>
        public static bool TryFor(CountryId country, (int Day, int Party, Scandal Scandal)[] scandals, CampaignCalendar? calendar, out CampaignRun.Setup setup, out string note,
            bool onVoteModelCompatibility = false)
        {
            if (country == CountryId.Sweden)
            {
                double[] compatibilityOverride = null;
                if (onVoteModelCompatibility)
                {
                    // C-R4b step 5 (D-21): the GAME stages the campaign on the compatibility election night
                    // predicts from - the vote model's good layer - so an idle campaign reproduces election
                    // night's own prediction and a campaign moves it. Mapped by party key, never by position.
                    if (!NationalElection.TryCompatibility(country, out string[] keys, out double[] compatibility, out _, out _))
                    {
                        setup = default;
                        note = $"no campaign is staged for {country} on the vote model: it has no fitted electorate or no two-election history";
                        return false;
                    }
                    compatibilityOverride = new double[SwedenParties.Length];
                    for (int p = 0; p < SwedenParties.Length; p++)
                    {
                        int k = System.Array.IndexOf(keys, SwedenParties[p]);
                        compatibilityOverride[p] = k >= 0 ? compatibility[k] : 0.0;
                    }
                }
                setup = Sweden(scandals, out note, calendar, compatibilityOverride);
                return true;
            }
            setup = default;
            note = $"no campaign is staged for {country}: the calendar, the regions and the personality cast exist for Sweden only (§3's first case); staging another country is its own item, not a copy of Sweden's";
            return false;
        }

        /// <summary>Sweden 2026 on the 2022 returns - the staging `CampaignAiHarness` has run since W-C1, from the runtime tables.</summary>
        public static CampaignRun.Setup Sweden((int Day, int Party, Scandal Scandal)[] scandals, out string note, CampaignCalendar? calendar = null,
            double[] compatibilityOverride = null)
        {
            if (!PartySystems.TryHistory(CountryId.Sweden, out double[] shares2022, out double[] shares2018))
            {
                throw new InvalidOperationException("PartySystems carries no 2022/2018 history for Sweden");
            }
            var sb = new StringBuilder();
            double[] prior = Normalised(shares2022);
            double[] loyalty = LoyaltyModel.PartyLoyalties(shares2022, shares2018);
            // DERIVED: compatibility at the fixed point where PersuadedShares == prior, so an idle
            // campaign reproduces the 2022 result exactly. c_i = ceiling * (prior_i / max prior)^(1/Sharpness).
            double maxPrior = 0.0;
            foreach (double p in prior) { if (p > maxPrior) { maxPrior = p; } }
            var compatibility = new double[prior.Length];
            for (int i = 0; i < prior.Length; i++)
            {
                compatibility[i] = CompatibilityCeiling * Math.Pow(prior[i] / maxPrior, 1.0 / PreferenceModel.Sharpness);
            }
            // D-21: the game hands in the vote model's compatibility instead (see TryFor); the harness
            // keeps the fixed point above, so its staging and digest are unchanged.
            if (compatibilityOverride != null && compatibilityOverride.Length == compatibility.Length) { compatibility = compatibilityOverride; }
            // SOURCED salience: EB105 Spring 2026, Sweden - the four top-five issues §6 has a slot for.
            var salience = new double[IssueVector.IssueCount];
            for (int i = 0; i < salience.Length; i++) { salience[i] = double.NaN; }
            salience[(int)IssueId.Climate] = 0.26;
            salience[(int)IssueId.Crime] = 0.18;
            salience[(int)IssueId.Defense] = 0.17;
            salience[(int)IssueId.Education] = 0.16;
            // SOURCED regions: the 29 valkretsar's valid votes, 2022 (W-F1) - the runtime catalog.
            RegionAudience[] regions = SwedenRegions(out double national);
            var parties = new CampaignRun.PartySetup[SwedenParties.Length];
            for (int p = 0; p < parties.Length; p++)
            {
                var match = new double[IssueVector.IssueCount];
                for (int i = 0; i < match.Length; i++) { match[i] = double.IsNaN(salience[i]) ? double.NaN : FlatIssueMatch; }
                AiPersonality personality = SwedenPersonalities[p];
                parties[p] = new CampaignRun.PartySetup(SwedenParties[p], personality, FlatCredibility, WarChest, match, Volunteers,
                    CandidateFor(personality, SwedenParties[p]), OfficesFor(personality, regions), OfficeOperationsPerDay,
                    StaffFor(personality), TelevisionBuysFor(personality));
            }
            var publicHouse = new PollingHouse("Public tracker", 600, 40_000, new double[SwedenParties.Length]);
            var internalHouse = new PollingHouse("Standard commission", 1_200, 120_000, new double[SwedenParties.Length], isInternal: true);
            sb.Append("\n  staging: 8 parties on Sweden 2022 (SOURCED prior), loyalty derived from 2018->2022 (W-A1):\n    ");
            for (int p = 0; p < parties.Length; p++)
            {
                sb.Append(string.Format(CultureInfo.InvariantCulture, "{0} L{1:F0}/C{2:F1}  ", SwedenParties[p], loyalty[p], compatibility[p]));
            }
            sb.Append(string.Format(CultureInfo.InvariantCulture,
                "\n    {0} valkretsar (SOURCED 2022 valid votes, W-F1), national audience {1:N0}; salience EB105 SE: climate .26 crime .18 defence .17 education .16\n" +
                "    [AUTHORED-DRAFT] issue-match {2:F2} flat, credibility {3:F2} flat, war chest {4:N0} kr each - EQUAL, and W-F5 measured why " +
                "(a seat-proportional split starves the small parties before it separates the personalities; see WarChestFor); houses from W-E4's ladder\n",
                regions.Length, national, FlatIssueMatch, FlatCredibility, WarChest));
            // W-B6: the electorate as one group at W-A1's size-weighted mean loyalty (a public
            // derivation from past returns), until W-F4's voter groups give the strategies their
            // per-group targets.
            double electorateLoyalty = LoyaltyModel.WeightedMeanLoyalty(loyalty, prior);
            sb.Append(string.Format(CultureInfo.InvariantCulture,
                "    strategies (W-B6): prof SwingVoter, pop Populist, est BroadAppeal, grass BaseMobilization, chaos NegativeCampaign; electorate loyalty {0:F1} (one group, W-A1 weighted mean)\n",
                electorateLoyalty));
            note = sb.ToString();
            return new CampaignRun.Setup(calendar ?? CampaignCalendar.Sweden2026, parties, prior, loyalty, compatibility, salience,
                national, regions, publicHouse, PublicPollEveryDays, internalHouse, electorateLoyalty, null, null, scandals);
        }

        /// <summary>The 29 valkretsar as campaign regions: name and VALID votes (the audience a local action can address), from the runtime catalog.</summary>
        public static RegionAudience[] SwedenRegions(out double national)
        {
            var regions = new RegionAudience[SwedishRegions.Count];
            national = 0.0;
            for (int r = 0; r < regions.Length; r++)
            {
                double valid = SwedishValkretsReturns2022.Valid[r];
                regions[r] = new RegionAudience(SwedishValkretsReturns2022.Names[r], valid);
                national += valid;
            }
            return regions;
        }

        /// <summary>[AUTHORED-DRAFT] W-B4 staging: the offices each personality opens on day 0, the largest regions first - grassroots 6, populist 4, professional 3, establishment 2, chaotic 1.</summary>
        public static int[] OfficesFor(AiPersonality personality, RegionAudience[] regions)
        {
            int count;
            switch (personality)
            {
                case AiPersonality.Grassroots: count = 6; break;
                case AiPersonality.Populist: count = 4; break;
                case AiPersonality.Professional: count = 3; break;
                case AiPersonality.Establishment: count = 2; break;
                default: count = 1; break;
            }
            var order = new List<int>();
            for (int r = 0; r < regions.Length; r++) { order.Add(r); }
            order.Sort((a, b) => regions[b].Audience.CompareTo(regions[a].Audience));
            return order.GetRange(0, count).ToArray();
        }

        /// <summary>[AUTHORED-DRAFT] §16's candidate attributes per personality - game fiction until W-F6 labels real candidates. Charisma, debate, communication, credibility, integrity, knowledge, campaign, popularity, scandal resistance.</summary>
        public static CandidateProfile CandidateFor(AiPersonality personality, string party)
        {
            switch (personality)
            {
                case AiPersonality.Populist: return new CandidateProfile(party, 85, 80, 75, 50, 55, 45, 65, 70, 55);
                case AiPersonality.Professional: return new CandidateProfile(party, 65, 70, 70, 70, 70, 70, 75, 60, 70);
                case AiPersonality.Establishment: return new CandidateProfile(party, 55, 65, 65, 80, 75, 80, 60, 60, 75);
                case AiPersonality.Grassroots: return new CandidateProfile(party, 70, 60, 65, 80, 85, 60, 60, 55, 70);
                default: return new CandidateProfile(party, 75, 75, 60, 45, 45, 50, 55, 65, 40);
            }
        }

        /// <summary>[AUTHORED-DRAFT] W-B5: each personality's hires as §32 describes it - the professional a manager and a pollster; the populist a manager and a digital strategist; the establishment a manager and a media advisor; the grassroots party a field organizer; the chaotic nobody.</summary>
        public static StaffRole[] StaffFor(AiPersonality personality)
        {
            switch (personality)
            {
                case AiPersonality.Professional: return new[] { StaffRole.CampaignManager, StaffRole.Pollster };
                case AiPersonality.Populist: return new[] { StaffRole.CampaignManager, StaffRole.DigitalStrategist };
                case AiPersonality.Establishment: return new[] { StaffRole.CampaignManager, StaffRole.MediaAdvisor };
                case AiPersonality.Grassroots: return new[] { StaffRole.FieldOrganizer };
                default: return new StaffRole[0];
            }
        }

        /// <summary>[AUTHORED-DRAFT] W-B5: the manager's plan - television buys the establishment 2, the professional and the populist 1, the rest none.</summary>
        public static int TelevisionBuysFor(AiPersonality personality)
        {
            switch (personality)
            {
                case AiPersonality.Establishment: return 2;
                case AiPersonality.Professional: return 1;
                case AiPersonality.Populist: return 1;
                default: return 0;
            }
        }

        private static double[] Normalised(double[] shares)
        {
            double sum = 0.0;
            foreach (double s in shares) { sum += s; }
            var result = new double[shares.Length];
            for (int i = 0; i < shares.Length; i++) { result[i] = sum > 0.0 ? shares[i] / sum : 0.0; }
            return result;
        }
    }
}
