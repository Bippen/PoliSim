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
    /// - the prior: the seated election's national shares, and loyalty from the one before (`LoyaltyModel`, W-A1) - 2026
    ///   against 2022 in the game since K-1 (2026-09-23), 2022 against 2018 where a harness pins <see cref="ElectionVintage.Sweden2022"/>;
    /// - the regions: the 29 valkretsar and their VALID votes, the same vintage's returns catalog (W-F1);
    /// - compatibility: DERIVED at the fixed point where an idle campaign reproduces the prior exactly;
    /// - salience: SOURCED, Eurobarometer 105 (Spring 2026), Sweden, the four top-five issues §6 has a slot for.
    ///
    /// ⚠ **What is still [AUTHORED-DRAFT] is labelled field by field and is the same draft the harness
    /// has run since W-C1**: which personality each party plays (§32's descriptions cast onto the
    /// eight), the candidates' attributes, the offices each personality opens, the staff it hires, the
    /// war chest (D-1 (c): equal, 2 400 000 kr), the volunteers, the flat issue-match and credibility.
    /// Moving them here changes nothing about their provenance; it changes who can call them.
    /// Sweden's was the first staged campaign - the spec's own first case (§3's calendar is
    /// `CampaignCalendar.Sweden2026`); §697 staged Germany's on the Länder (<see cref="Germany"/>), PS-6 US-30 the USA's on its 51
    /// jurisdictions (<see cref="TryUsa"/>), and <see cref="TryFor"/> says so for the other three rather than inventing one.
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

        /// <summary>§695 (decision 5): Sweden's eight cast by the CHES rule (<see cref="CampaignCasts"/>), in `TryHistory`'s order - DERIVED, never cast by hand.
        /// It retires C-R4b's hand list ([AUTHORED-DRAFT]: S professional, SD populist, M establishment, V grassroots, C chaotic, KD establishment,
        /// MP grassroots, L professional); the rule casts S, M, C, KD and L establishment, SD and V populist, MP grassroots (`CampaignCastDiagnostic`).</summary>
        public static AiPersonality[] SwedenPersonalities => CastsOf(CountryId.Sweden, SwedenParties);

        /// <summary>§695: the rule's cast of each key among the country's real parties; a key that is not a real party's reads as <see cref="AiPersonality.Professional"/>, the rule's own last line.</summary>
        public static AiPersonality[] CastsOf(CountryId country, string[] keys)
        {
            PoliticalParty[] roster = PartySystems.RealRoster(country);
            var casts = new AiPersonality[keys.Length];
            for (int i = 0; i < keys.Length; i++)
            {
                int at = Array.FindIndex(roster, p => p.Abbrev == keys[i]);
                casts[i] = at >= 0 ? CampaignCasts.Of(roster[at]) : AiPersonality.Professional;
            }
            return casts;
        }

        /// <summary>The party keys in the order the staging uses - `PartySystems.TryHistory(Sweden)`'s. ⚠ NOT the returns catalogs' column order
        /// (S, M, SD, C, V, KD, L, MP): every read of a catalog maps by key, never by position. (This doc said the two orders were one until K-1.)</summary>
        public static readonly string[] SwedenParties = { "S", "SD", "M", "V", "C", "KD", "MP", "L" };

        /// <summary>§675 (SP-3 part two): the campaign's cast - `PartySystems.For`'s keys, the real parties in their order and the created parties after them.
        /// With none created it is <see cref="SwedenParties"/> exactly, so every staging and digest stands.</summary>
        public static string[] Keys(CountryId country)
        {
            IReadOnlyList<PoliticalParty> roster = PartySystems.For(country);
            var keys = new string[roster.Count];
            for (int i = 0; i < keys.Length; i++) { keys[i] = roster[i].Abbrev; }
            return keys;
        }

        /// <summary>§675: a party's cast personality - a real party's by the CHES rule (§695); a created party's by its origin ([AUTHORED-DRAFT]): Grassroots and Single-issue grassroots,
        /// a Splinter its parent's, Protest populist, Business-backed professional, Regional grassroots (its ground game).</summary>
        public static AiPersonality PersonalityOf(int index, string key)
        {
            if (index < SwedenParties.Length && SwedenParties[index] == key) { return SwedenPersonalities[index]; }
            return PersonalityOf(CountryId.Sweden, key);
        }

        /// <summary>§697: <paramref name="country"/>'s cast of <paramref name="key"/> - a real party's by the CHES rule (§695); a created party's by its origin,
        /// a Splinter its parent's rule cast.</summary>
        public static AiPersonality PersonalityOf(CountryId country, string key)
        {
            PoliticalParty[] real = PartySystems.RealRoster(country);
            int realAt = Array.FindIndex(real, p => p.Abbrev == key);
            if (realAt >= 0) { return CampaignCasts.Of(real[realAt]); }
            CreatedParty c = null;
            foreach (CreatedParty x in CreatedParties.Of(country)) { if (x.Key == key) { c = x; } }
            if (c == null) { return AiPersonality.Professional; }
            switch (c.Origin)
            {
                case PartyOrigin.Splinter: { int parent = Array.FindIndex(real, p => p.Abbrev == c.ParentKey); return parent >= 0 ? CampaignCasts.Of(real[parent]) : AiPersonality.Grassroots; }
                case PartyOrigin.Protest: return AiPersonality.Populist;
                case PartyOrigin.BusinessTechnocrats: return AiPersonality.Professional;
                default: return AiPersonality.Grassroots;
            }
        }

        /// <summary>§675: a created party's day 0 from its five stats through the pip table (`PartyOrigins`, [AUTHORED-DRAFT]) - Funding the war chest as a share of
        /// the real parties' (equal) chest, Organisation the offices (a Regional party's region first), Activists the volunteers as a share of the real parties',
        /// Leader the candidate's attributes; no state support (the party-support act pays on past elections, §625 - a created party has none).
        /// ⚠ Recognition reaches nothing: neither the vote model nor the campaign has an awareness term (§674).</summary>
        public static bool TryCreatedDayZero(string key, RegionAudience[] regions, out double money, out int volunteers, out int[] offices, out CandidateProfile candidate)
        {
            money = 0; volunteers = 0; offices = null; candidate = default;
            CreatedParty c = null;
            foreach (CreatedParty x in CreatedParties.Of(CountryId.Sweden)) { if (x.Key == key) { c = x; } }
            if (c == null) { return false; }
            money = WarChest * PartyOrigins.Pip(PartyOrigins.FundingShare, c.Funding);
            volunteers = (int)Math.Round(Volunteers * PartyOrigins.Pip(PartyOrigins.ActivistShare, c.Activists));
            int count = PartyOrigins.Pip(PartyOrigins.Offices, c.Organisation);
            var order = new List<int>();
            for (int r = 0; r < regions.Length; r++) { order.Add(r); }
            order.Sort((a, b) => (c.Origin == PartyOrigin.Regional && regions[a].Name == c.Region ? 0 : 1).CompareTo(c.Origin == PartyOrigin.Regional && regions[b].Name == c.Region ? 0 : 1) != 0
                ? (c.Origin == PartyOrigin.Regional && regions[a].Name == c.Region ? 0 : 1).CompareTo(c.Origin == PartyOrigin.Regional && regions[b].Name == c.Region ? 0 : 1)
                : regions[b].Audience.CompareTo(regions[a].Audience));
            offices = order.GetRange(0, Math.Min(count, order.Count)).ToArray();
            int a100 = (int)Math.Round(100.0 * PartyOrigins.Pip(PartyOrigins.LeaderAttribute, c.Leader));
            candidate = new CandidateProfile(key, a100, a100, a100, a100, a100, a100, a100, a100, a100);
            return true;
        }

        /// <summary>
        /// The country's staged campaign, or false with the reason when none is staged. Sweden and, since §697, Germany; since PS-6 US-30 the USA
        /// (<see cref="TryUsa"/>, staged on the vote model whatever <paramref name="onVoteModelCompatibility"/> says).
        /// <paramref name="scandals"/> is the caller's staging (the harness stages one; a game passes none
        /// until §17's dynamic generation exists).
        /// PS-6 US-26 (R-US11 (a)): <paramref name="played"/> - the game's country - stages the prior and the loyalty on its history rolled forward to the
        /// calendar's polling day (`PartySystems.OwnCounts`), as the prediction reads them; none handed, the record's pair (every harness). DECLARED (no format step): a save cut
        /// mid-campaign by a build before US-26, the game's first count already held, replays its campaign on the rolled history rather than the record's pair it was
        /// stepped on - its replay can land off the saved stream counts (the replay's own assertion compares draw counts only, so a replay whose counts happen
        /// to match differs with no line in the log).
        /// </summary>
        public static bool TryFor(CountryId country, (int Day, int Party, Scandal Scandal)[] scandals, CampaignCalendar? calendar, out CampaignRun.Setup setup, out string note,
            bool onVoteModelCompatibility = false, int playerParty = -1, Func<int, AiDecision[]> playerScript = null, PreCampaignRun.Outcome? playerOutcome = null,
            Func<int, ScandalResponse?> playerScandalScript = null, double liveScandalRate = 0.0, Country played = null)
        {
            if (country == CountryId.Sweden || country == CountryId.Germany)   // §697: Germany's campaign on the Länder, its own staging
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
                    string[] cast = Keys(country);   // §675: the cast, created parties included
                    compatibilityOverride = new double[cast.Length];
                    for (int p = 0; p < cast.Length; p++)
                    {
                        int k = System.Array.IndexOf(keys, cast[p]);
                        compatibilityOverride[p] = k >= 0 ? compatibility[k] : 0.0;
                    }
                }
                setup = country == CountryId.Sweden
                    ? Sweden(scandals, out note, calendar, compatibilityOverride, playerParty, playerScript, playerOutcome, playerScandalScript, liveScandalRate, played: played)
                    : Germany(scandals, out note, calendar, compatibilityOverride, playerParty, playerScript, playerOutcome, playerScandalScript, liveScandalRate, played);
                return true;
            }
            if (country == CountryId.USA)   // PS-6 US-30: the USA's campaign on the jurisdictions, its own staging - always on the vote model (the prediction's only inputs)
            {
                return TryUsa(scandals, calendar, out setup, out note, playerParty, playerScript, playerOutcome, playerScandalScript, liveScandalRate, played);
            }
            setup = default;
            note = $"no campaign is staged for {country}: the calendar, the regions and the personality cast exist for Sweden, Germany and the USA only; staging another country is its own item, not a copy of theirs";
            return false;
        }

        /// <summary>[SOURCED] PS-6 US-30: Gallup's "Most Important Problem", August 2024 - the newest wave fielded before the 2024 campaign opened: immigration
        /// 19 % (`ElectionsData/usa/raw/salience/gallup_mip_vEKjO_47_dataset.csv`, held by <c>UsCampaignDiagnostic</c>).</summary>
        public const double GallupAugust2024Immigration = 0.19;

        /// <summary>[SOURCED] PS-6 US-30: the same wave's economy - "Economy in general" 18 % against "High cost of living/Inflation" 15 %, the larger standing
        /// where two items map to one slot (`StanceModel`'s convention). "The government/Poor leadership" (16 %) has no slot and is dropped, as in the seed.</summary>
        public const double GallupAugust2024Economy = 0.18;

        /// <summary>[SOURCED] the first day after the month the August 2024 wave was fielded in: a campaign opening on or after it reads that wave.</summary>
        private static readonly DateTime GallupAugust2024Published = new DateTime(2024, 9, 1);

        /// <summary>[SOURCED] the first day after the month of Gallup's July 2026 column - the USA's salience seed (`StanceModel`, `ElectionsData/salience/issue_salience.md`):
        /// a campaign opening on or after it reads the seed, the newest wave on disk.</summary>
        private static readonly DateTime GallupJuly2026Published = new DateTime(2026, 8, 1);

        /// <summary>PS-6 US-30: the salience a US campaign opening on <paramref name="campaignStart"/> reads - the newest Gallup wave on disk fielded before it
        /// (DECLARED: "before the campaign" as before the campaign proper's opening; the run-up stages on the same calendar, so it reads the same wave):
        /// the July 2026 seed from its month's end, August 2024 before that. A campaign opening before September 2024 - none in the game - reads August 2024
        /// all the same, and its note says the wave postdates it.</summary>
        public static double[] UsSalience(DateTime campaignStart, out string label)
        {
            var salience = new double[IssueVector.IssueCount];
            for (int i = 0; i < salience.Length; i++) { salience[i] = double.NaN; }
            if (campaignStart >= GallupJuly2026Published)
            {
                var parts = new List<string>();
                foreach (IssueId issue in Enum.GetValues(typeof(IssueId)))
                {
                    float seed = PoliSim.Simulation.StanceModel.SalienceOf(CountryId.USA, issue, out bool measured);
                    if (!measured) { continue; }
                    salience[(int)issue] = Math.Round(seed, 2);   // DECLARED: Gallup's figures are whole per cents; the seed's float read back to them
                    parts.Add(issue.ToString().ToLowerInvariant() + " " + salience[(int)issue].ToString("0.00", CultureInfo.InvariantCulture));
                }
                label = "Gallup MIP July 2026 (the seed): " + string.Join(", ", parts);
                return salience;
            }
            salience[(int)IssueId.Immigration] = GallupAugust2024Immigration;
            salience[(int)IssueId.Economy] = GallupAugust2024Economy;
            label = string.Format(CultureInfo.InvariantCulture, "Gallup MIP August 2024: immigration {0:0.00}, economy {1:0.00}{2}", GallupAugust2024Immigration, GallupAugust2024Economy,
                campaignStart >= GallupAugust2024Published ? string.Empty : " - fielded after this campaign opened, no earlier wave on disk");
            return salience;
        }

        /// <summary>
        /// PS-6 US-30: <b>THE USA'S CAMPAIGN ON THE JURISDICTIONS</b> - its own staging, not a copy of Germany's. The 51 jurisdictions are its regions
        /// (<see cref="UsRegions"/>: each one's valid presidential votes at the previous election the audience a local act can reach, its citizen voting-age
        /// population the electorate a ground game can mobilise); every party stands where its candidate is on the ballot of record (§699 - the two major
        /// nominees everywhere). Staged on the vote model the prediction is made of (<see cref="NationalElection.TryCompatibility"/>: US-6's compatibility,
        /// its prior and its loyalty, mapped by key), so an idle campaign reproduces the day's prediction and a campaign moves it; the count reads the run's
        /// final shares, then the states (R-US14), then the electors (`SimulationManager`'s US count). The casts are the CHES rule's (§695, GPS 2019 for the
        /// USA); the salience the newest Gallup wave fielded before the campaign (<see cref="UsSalience"/>). ⚠ The price table - the war chest, every action's
        /// cost - is the model's one, as the HQ prints it in the player's currency: a US table in dollars is BILLED, not invented (Germany's is billed too).
        /// PS-6 US-26 (R-US11 (a)): <paramref name="played"/> - the game's country - stages the prior and the loyalty on its history rolled forward to the
        /// calendar's polling day, the very call the count's prediction makes on that day (`NationalElection.TryCompatibility` with the country and the day), so a
        /// campaign of a later cycle stages on the game's own House counts and its idle run still reproduces the day's prediction; none handed, the record's pair.
        /// PS-6 US-31 (R-US16 (b)): the staging declares where every AI weighs its local acts (<see cref="UsGroundGame.TargetingFor"/>, [AUTHORED-DRAFT]) in
        /// place of the largest audiences. PS-6 K27 (Elias's ruling of 2026-10-23: "the AI's offices follow the targeting rule (electors x closeness)"): every AI
        /// party's day-0 offices open where the same rule weighs most (`OfficesByTargeting`), read on the staging's opening shares - the poll an idle campaign
        /// opens on, since no poll is published before day 0 (DECLARED); the player's party's offices are its own (the run-up's, or by audience where it brings
        /// none). False, with the reason, where the vote model or the regions cannot be read.
        /// </summary>
        public static bool TryUsa((int Day, int Party, Scandal Scandal)[] scandals, CampaignCalendar? calendar, out CampaignRun.Setup setup, out string note,
            int playerParty = -1, Func<int, AiDecision[]> playerScript = null, PreCampaignRun.Outcome? playerOutcome = null,
            Func<int, ScandalResponse?> playerScandalScript = null, double liveScandalRate = 0.0, Country played = null)
        {
            setup = default;
            CampaignCalendar cal;
            if (calendar.HasValue) { cal = calendar.Value; }
            else
            {
                DateTime start = WorldClock.StartDate(CountryId.USA);
                if (!WorldClock.TryNextPollingDay(CountryId.USA, start, out DateTime polling)) { note = "no campaign is staged for the USA: no polling day follows its start"; return false; }
                cal = CampaignCalendar.FromWorldStart(polling, start);
            }
            // PS-6 US-27 (DECLARED, the conservative reading, asked): the staged campaign is the presidential one; a midterm stages none
            if (UsCongressElection.IsMidterm(cal.ElectionDate)) { note = "no campaign is staged for a US midterm: the campaign staged is the presidential one"; return false; }
            if (!NationalElection.TryCompatibility(CountryId.USA, out string[] modelKeys, out double[] modelCompatibility, out double[] modelPrior, out double[] modelLoyalty, played, cal.ElectionDate))   // PS-6 US-26: the history rolled forward to the polling day, as the count's prediction reads it
            {
                note = "no campaign is staged for the USA on the vote model: it has no fitted electorate or no two-election history";
                return false;
            }
            RegionAudience[] regions = UsRegions.Regions(cal.ElectionDate, out double national, out int priorYear, out string regionsWhy);
            if (regions.Length == 0) { note = "no campaign is staged for the USA: " + regionsWhy; return false; }
            string[] cast = Keys(CountryId.USA);
            var compatibility = new double[cast.Length];
            var prior = new double[cast.Length];
            var loyalty = new double[cast.Length];
            for (int p = 0; p < cast.Length; p++)
            {
                int k = Array.IndexOf(modelKeys, cast[p]);   // mapped by key, never by position
                compatibility[p] = k >= 0 ? modelCompatibility[k] : 0.0;
                prior[p] = k >= 0 ? modelPrior[k] : 0.0;
                loyalty[p] = k >= 0 ? modelLoyalty[k] : NationalElection.UnplacedLoyalty;
            }
            bool[][] stands = UsRegions.Stands(cast, regions, cal.ElectionDate);
            double[] salience = UsSalience(cal.CampaignStart, out string salienceLabel);
            // PS-6 K27: the targeting rule, read once on the staging's opening shares for the AI's day-0 offices, and handed to the run for its local acts
            RegionTargeting targeting = UsGroundGame.TargetingFor(cal.ElectionDate, cast, regions, played);
            int[] officeOrder = OpeningOrder(targeting, cal.CampaignStart, compatibility, prior, loyalty);
            var parties = new CampaignRun.PartySetup[cast.Length];
            for (int p = 0; p < parties.Length; p++)
            {
                var match = new double[IssueVector.IssueCount];
                for (int i = 0; i < match.Length; i++) { match[i] = double.IsNaN(salience[i]) ? double.NaN : FlatIssueMatch; }
                AiPersonality personality = PersonalityOf(CountryId.USA, cast[p]);
                PreCampaignRun.Outcome? brought = p == playerParty ? playerOutcome : null;
                parties[p] = new CampaignRun.PartySetup(cast[p], personality, FlatCredibility,
                    brought.HasValue ? brought.Value.Money : WarChest, match, brought.HasValue ? brought.Value.Volunteers : Volunteers,
                    CandidateFor(personality, cast[p]), brought.HasValue ? brought.Value.Offices : p == playerParty ? OfficesFor(personality, regions, stands[p]) : OfficesByTargeting(personality, regions, stands[p], officeOrder), OfficeOperationsPerDay,
                    brought.HasValue ? brought.Value.Staff : StaffFor(personality), brought.HasValue ? brought.Value.TelevisionBuys : TelevisionBuysFor(personality),
                    p == playerParty ? playerScript : null, p == playerParty ? playerScandalScript : null);
            }
            var publicHouse = new PollingHouse("Public tracker", 600, 40_000, new double[cast.Length]);
            var internalHouse = new PollingHouse("Standard commission", 1_200, 120_000, new double[cast.Length], isInternal: true);
            double eligible = 0.0;
            foreach (RegionAudience r in regions) { eligible += r.Eligible; }
            List<(DateTime Day, double[] Shares)> own = played?.Id == CountryId.USA ? PartySystems.OwnCounts(played, cal.ElectionDate) : new List<(DateTime Day, double[] Shares)>();   // PS-6 US-26: the game's own House counts the pair rolled forward on, for the note - the USA's only, as TryHistory reads them
            string rolled = own.Count == 0 ? string.Empty
                : own.Count == 1 ? ", on the history rolled forward - the game's House count of " + own[0].Day.Year.ToString(CultureInfo.InvariantCulture) + " against the record's last"
                : ", on the history rolled forward - the game's House counts of " + own[own.Count - 2].Day.Year.ToString(CultureInfo.InvariantCulture) + " and " + own[own.Count - 1].Day.Year.ToString(CultureInfo.InvariantCulture);
            var sb = new StringBuilder();
            sb.Append("\n  staging: " + cast.Length.ToString(CultureInfo.InvariantCulture) + " parties on the USA's vote model (US-6: compatibility, prior and loyalty as the prediction reads them" + rolled + "):\n    ");
            for (int p = 0; p < parties.Length; p++)
            {
                int where = 0;
                foreach (bool s in stands[p]) { if (s) { where++; } }
                sb.Append(string.Format(CultureInfo.InvariantCulture, "{0} L{1:F0}/C{2:F1}/{3}J  ", cast[p], loyalty[p], compatibility[p], where));
            }
            sb.Append(string.Format(CultureInfo.InvariantCulture,
                "\n    {0} jurisdictions (SOURCED {1} valid presidential votes, US-3's catalog; the Census Bureau's CVAP 2020-2024), national audience {2:N0}, electorate {3:N0}; salience {4}\n" +
                "    each party only where it is on the ballot (/nJ above); [AUTHORED-DRAFT] issue-match {5:F2} flat, credibility {6:F2} flat, war chest {7:N0} each - the model's one price table, a US one in dollars BILLED\n",
                regions.Length, priorYear, national, eligible, salienceLabel, FlatIssueMatch, FlatCredibility, WarChest));
            sb.Append("    [AUTHORED-DRAFT] targeting: every AI weighs its local acts where electors x the swing index of the margin derived from the last published poll are largest, and opens its day-0 offices where the same rule weighs most on the staging's opening shares (K27); the count counts each state with the ground game and the declared regional noise (R-US16 (b))\n");
            note = sb.ToString();
            setup = new CampaignRun.Setup(cal, parties, prior, loyalty, compatibility, salience,
                national, regions, publicHouse, PublicPollEveryDays, internalHouse, LoyaltyModel.WeightedMeanLoyalty(loyalty, prior), null, null, scandals, liveScandalRate,
                null, PartyFamilies.For(CountryId.USA, cast), NationalElection.EntrantAwareness(CountryId.USA, cast), EntrantLayer.GroupingOf(CountryId.USA, cast), EntrantSimilarity.For(CountryId.USA, cast),
                stands, targeting);   // PS-6 US-31: the AI weighs its local acts where the declared rule says (R-US16 (b))
            return true;
        }

        /// <summary>
        /// §697 (PS-4): <b>GERMANY'S CAMPAIGN ON THE LÄNDER</b> - its own staging, not a copy of Sweden's. The sixteen Länder are its regions
        /// (<see cref="GermanRegions"/>: each Land's valid Zweitstimmen the audience a local act can reach, its registered electorate the one a
        /// ground game can mobilise), read on the chamber seated at the campaign's opening, so the snap campaign of 2024-25 stands on 2021's Länder.
        /// Each party campaigns only where it stands - the candidacy fact those returns carry (<see cref="CampaignRun.Setup.Stands"/>): the CSU in
        /// Bayern alone, the CDU in the other fifteen, the SSW in Schleswig-Holstein, and on 2021's Länder the Grüne nowhere in Saarland (their 2021
        /// list was rejected; their 2025 list was not - a limit of reading the seated election's candidacies, stated). The prior and the loyalty are
        /// that chamber's pair (2021 against 2017 for the snap campaign, §696); the salience the last Eurobarometer wave fielded before the campaign
        /// (EB102 for the snap - immigration .35, economy .31, housing .15 - and EB105 for an election after it); the casts the CHES rule's (§695).
        /// ⚠ The price table - the war chest, every action's cost - is the model's one, in kronor, as the HQ prints it: a German table is billed,
        /// not invented.
        /// </summary>
        public static CampaignRun.Setup Germany((int Day, int Party, Scandal Scandal)[] scandals, out string note, CampaignCalendar? calendar = null,
            double[] compatibilityOverride = null, int playerParty = -1, Func<int, AiDecision[]> playerScript = null, PreCampaignRun.Outcome? playerOutcome = null,
            Func<int, ScandalResponse?> playerScandalScript = null, double liveScandalRate = 0.0, Country played = null)
        {
            CampaignCalendar cal = calendar ?? CampaignCalendar.FromWorldStart(WorldClock.LatestElectionDay(CountryId.Germany), WorldClock.StartDate(CountryId.Germany));
            DateTime opening = cal.CampaignStart;
            ElectionVintage seated = WorldClock.SeatedVintage(CountryId.Germany, opening);
            // PS-6 US-26 (R-US11 (a)): the played country's own counts before the polling day roll the pair forward; none, the seated chamber's pair as before
            if (!PartySystems.TryHistory(CountryId.Germany, played, cal.ElectionDate, out double[] latestShares, out double[] previousShares, out int ownCounts, seated))
            {
                throw new InvalidOperationException("PartySystems carries no two-election history for Germany's seated chamber");
            }
            bool snap = seated == ElectionVintage.Germany2021;
            string latestYear = snap ? "2021" : "2025", previousYear = snap ? "2017" : "2021";
            bool epochSnap = WorldClock.Resolve(CountryId.Germany, ElectionVintage.Seated) == ElectionVintage.Germany2021;
            string history = HistoryPhrase("Germany", played, cal.ElectionDate, ownCounts, latestYear, previousYear, epochSnap ? "2021" : "2025");
            var sb = new StringBuilder();
            double[] prior = Normalised(latestShares);
            // §704: a party the survey does not place (the SSW) keeps its prior - its 2017 absence gave it no loyalty and its missing position no
            // spatial share, and the campaign had counted it at zero from day 0
            double[] loyalty = NationalElection.UnplacedKeepTheirPrior(PartySystems.For(CountryId.Germany), LoyaltyModel.PartyLoyalties(latestShares, previousShares));
            // DERIVED: the fixed point where an idle campaign reproduces the prior (Sweden's form); the game hands in the vote model's instead (TryFor).
            double maxPrior = 0.0;
            foreach (double p in prior) { if (p > maxPrior) { maxPrior = p; } }
            var compatibility = new double[prior.Length];
            for (int i = 0; i < prior.Length; i++)
            {
                compatibility[i] = maxPrior > 0.0 ? CompatibilityCeiling * Math.Pow(prior[i] / maxPrior, 1.0 / PreferenceModel.Sharpness) : 0.0;
            }
            if (compatibilityOverride != null && compatibilityOverride.Length == compatibility.Length) { compatibility = compatibilityOverride; }
            // SOURCED salience (ElectionsData/salience/issue_salience.md), mapped to the issue slots by StanceModel's convention (rising prices onto Economy)
            var salience = new double[IssueVector.IssueCount];
            for (int i = 0; i < salience.Length; i++) { salience[i] = double.NaN; }
            if (snap)
            {
                salience[(int)IssueId.Immigration] = 0.35;   // EB102, Nationaler Bericht Deutschland, fieldwork 10-31 Oct 2024
                salience[(int)IssueId.Economy] = 0.31;
                salience[(int)IssueId.Housing] = 0.15;
            }
            else
            {
                salience[(int)IssueId.Economy] = 0.36;   // EB105, Spring 2026 - the latest wave on disk for an election after the snap
                salience[(int)IssueId.Immigration] = 0.14;
            }
            // SOURCED regions: the sixteen Länder of the seated election (kerg2.csv), audience the valid Zweitstimmen, eligible the registered electorate
            string[] cast = Keys(CountryId.Germany);
            RegionalVoteModel.RegionInput[] lands = GermanRegions.Regions(cast, opening);
            var regions = new RegionAudience[lands.Length];
            double national = 0.0;
            for (int r = 0; r < regions.Length; r++)
            {
                regions[r] = new RegionAudience(lands[r].Name, GermanRegions.ValidAt(r, opening), eligible: GermanRegions.EligibleAt(r, opening));
                national += regions[r].Audience;
            }
            var stands = new bool[cast.Length][];
            for (int p = 0; p < cast.Length; p++)
            {
                stands[p] = new bool[regions.Length];
                for (int r = 0; r < regions.Length; r++) { stands[p][r] = lands[r].PartyAvailable[p]; }
            }
            var parties = new CampaignRun.PartySetup[cast.Length];
            for (int p = 0; p < parties.Length; p++)
            {
                var match = new double[IssueVector.IssueCount];
                for (int i = 0; i < match.Length; i++) { match[i] = double.IsNaN(salience[i]) ? double.NaN : FlatIssueMatch; }
                AiPersonality personality = PersonalityOf(CountryId.Germany, cast[p]);
                PreCampaignRun.Outcome? brought = p == playerParty ? playerOutcome : null;
                parties[p] = new CampaignRun.PartySetup(cast[p], personality, FlatCredibility,
                    brought.HasValue ? brought.Value.Money : WarChest, match, brought.HasValue ? brought.Value.Volunteers : Volunteers,
                    CandidateFor(personality, cast[p]), brought.HasValue ? brought.Value.Offices : OfficesFor(personality, regions, stands[p]), OfficeOperationsPerDay,
                    brought.HasValue ? brought.Value.Staff : StaffFor(personality), brought.HasValue ? brought.Value.TelevisionBuys : TelevisionBuysFor(personality),
                    p == playerParty ? playerScript : null, p == playerParty ? playerScandalScript : null);
            }
            var publicHouse = new PollingHouse("Public tracker", 600, 40_000, new double[cast.Length]);
            var internalHouse = new PollingHouse("Standard commission", 1_200, 120_000, new double[cast.Length], isInternal: true);
            sb.Append("\n  staging: " + cast.Length.ToString(CultureInfo.InvariantCulture) + " parties on " + history + ":\n    ");
            for (int p = 0; p < parties.Length; p++)
            {
                int where = 0;
                foreach (bool s in stands[p]) { if (s) { where++; } }
                sb.Append(string.Format(CultureInfo.InvariantCulture, "{0} L{1:F0}/C{2:F1}/{3}L  ", cast[p], loyalty[p], compatibility[p], where));
            }
            sb.Append(string.Format(CultureInfo.InvariantCulture,
                "\n    {0} Länder (SOURCED " + latestYear + " valid Zweitstimmen and Wahlberechtigte, kerg2.csv), national audience {1:N0}; salience " + (snap ? "EB102 DE: immigration .35 economy .31 housing .15" : "EB105 DE: economy .36 immigration .14") + "\n" +
                "    each party only where it stands (/nL above); [AUTHORED-DRAFT] issue-match {2:F2} flat, credibility {3:F2} flat, war chest {4:N0} kr each - the model's one price table, a German one billed\n",
                regions.Length, national, FlatIssueMatch, FlatCredibility, WarChest));
            double electorateLoyalty = LoyaltyModel.WeightedMeanLoyalty(loyalty, prior);
            note = sb.ToString();
            return new CampaignRun.Setup(cal, parties, prior, loyalty, compatibility, salience,
                national, regions, publicHouse, PublicPollEveryDays, internalHouse, electorateLoyalty, null, null, scandals, liveScandalRate,
                null, PartyFamilies.For(CountryId.Germany, cast), NationalElection.EntrantAwareness(CountryId.Germany, cast), EntrantLayer.GroupingOf(CountryId.Germany, cast), EntrantSimilarity.For(CountryId.Germany, cast),
                stands, doorCacePp: GotvTurnout.EuropeanDoorCacePp);   // A-LOOP q24 5b: the count reads an opened door at [CP20]'s turnout CACE (K7)
        }

        /// <summary>Sweden's campaign staging from the runtime tables - on the seated election (2026's) in the game; on 2022's where a harness
        /// pins <see cref="ElectionVintage.Sweden2022"/>, the staging `CampaignAiHarness` has run since W-C1, kept byte for byte by K-1.
        /// §695: <paramref name="castOverride"/> - a harness's FIXTURE cast, one personality per seat, where a harness measures the personalities'
        /// mechanics and needs all five on the field; the game never passes one, and every party it stages is cast by the CHES rule.</summary>
        public static CampaignRun.Setup Sweden((int Day, int Party, Scandal Scandal)[] scandals, out string note, CampaignCalendar? calendar = null,
            double[] compatibilityOverride = null, int playerParty = -1, Func<int, AiDecision[]> playerScript = null, PreCampaignRun.Outcome? playerOutcome = null,
            Func<int, ScandalResponse?> playerScandalScript = null, double liveScandalRate = 0.0, ElectionVintage vintage = ElectionVintage.Seated,
            AiPersonality[] castOverride = null, Country played = null)
        {
            vintage = WorldClock.Resolve(CountryId.Sweden, vintage);   // PS-1 (§618): the seated chamber's election at the world's epoch
            // PS-6 US-26 (R-US11 (a)): the played country's own counts before the polling day roll the pair forward; none, the seated chamber's pair as before
            DateTime electionDay = (calendar ?? CampaignCalendar.Sweden2026).ElectionDate;
            if (!PartySystems.TryHistory(CountryId.Sweden, played, electionDay, out double[] latestShares, out double[] previousShares, out int ownCounts, vintage))
            {
                throw new InvalidOperationException("PartySystems carries no two-election history for Sweden");
            }
            bool pinned2022 = vintage == ElectionVintage.Sweden2022;
            string latestYear = pinned2022 ? "2022" : "2026", previousYear = pinned2022 ? "2018" : "2022";
            string history = HistoryPhrase("Sweden", played, electionDay, ownCounts, latestYear, previousYear,
                WorldClock.Resolve(CountryId.Sweden, ElectionVintage.Seated) == ElectionVintage.Sweden2022 ? "2022" : "2026");
            var sb = new StringBuilder();
            double[] prior = Normalised(latestShares);
            double[] loyalty = LoyaltyModel.PartyLoyalties(latestShares, previousShares);
            // DERIVED: compatibility at the fixed point where PersuadedShares == prior, so an idle
            // campaign reproduces the prior's result exactly. c_i = ceiling * (prior_i / max prior)^(1/Sharpness).
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
            // SOURCED regions: the 29 valkretsar's valid votes, the same vintage as the prior (W-F1) - the runtime catalog.
            RegionAudience[] regions = SwedenRegions(out double national, vintage);
            string[] cast = Keys(CountryId.Sweden);   // §675: the real eight, then the created parties
            var parties = new CampaignRun.PartySetup[cast.Length];
            for (int p = 0; p < parties.Length; p++)
            {
                var match = new double[IssueVector.IssueCount];
                for (int i = 0; i < match.Length; i++) { match[i] = double.IsNaN(salience[i]) ? double.NaN : FlatIssueMatch; }
                AiPersonality personality = castOverride != null && p < castOverride.Length ? castOverride[p] : PersonalityOf(p, cast[p]);
                // C-R4b step 4b: the player's party plays the HQ's queue (a scripted party, W-C2's seam);
                // every other party, and the player's until a script is given, is its cast personality.
                // CL-1 (2026-09-12): the player's party brings its PRE-campaign's outcome to day 0 - the chest as the run-up
                // left it, the offices it planned and the staff it hired beside the staging's, the buys it prepared into the
                // plan. Every other party's day 0 IS its staging (its run-up, authored once). An idle run-up reproduces the
                // staging exactly, which CampaignClockHarness 7a asserts on the campaign's own decision digest.
                PreCampaignRun.Outcome? brought = p == playerParty ? playerOutcome : null;
                // §675: a created party's day 0 is its stats' (TryCreatedDayZero); a real party's is its cast staging, unchanged
                bool made = TryCreatedDayZero(cast[p], regions, out double madeMoney, out int madeVolunteers, out int[] madeOffices, out CandidateProfile madeCandidate);
                parties[p] = new CampaignRun.PartySetup(cast[p], personality, FlatCredibility,
                    brought.HasValue ? brought.Value.Money : made ? madeMoney : WarChest, match, brought.HasValue ? brought.Value.Volunteers : made ? madeVolunteers : Volunteers,
                    made ? madeCandidate : CandidateFor(personality, cast[p]), brought.HasValue ? brought.Value.Offices : made ? madeOffices : OfficesFor(personality, regions), OfficeOperationsPerDay,
                    brought.HasValue ? brought.Value.Staff : made ? new StaffRole[0] : StaffFor(personality), brought.HasValue ? brought.Value.TelevisionBuys : made ? 0 : TelevisionBuysFor(personality),
                    p == playerParty ? playerScript : null, p == playerParty ? playerScandalScript : null);
            }
            var publicHouse = new PollingHouse("Public tracker", 600, 40_000, new double[cast.Length]);
            var internalHouse = new PollingHouse("Standard commission", 1_200, 120_000, new double[cast.Length], isInternal: true);
            sb.Append("\n  staging: " + cast.Length.ToString(CultureInfo.InvariantCulture) + " parties on " + history + " (W-A1):\n    ");
            for (int p = 0; p < parties.Length; p++)
            {
                sb.Append(string.Format(CultureInfo.InvariantCulture, "{0} L{1:F0}/C{2:F1}  ", cast[p], loyalty[p], compatibility[p]));
            }
            sb.Append(string.Format(CultureInfo.InvariantCulture,
                "\n    {0} valkretsar (SOURCED " + latestYear + " valid votes, W-F1), national audience {1:N0}; salience EB105 SE: climate .26 crime .18 defence .17 education .16\n" +
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
                national, regions, publicHouse, PublicPollEveryDays, internalHouse, electorateLoyalty, null, null, scandals, liveScandalRate,
                null, PartyFamilies.For(CountryId.Sweden, cast), NationalElection.EntrantAwareness(CountryId.Sweden, cast), EntrantLayer.GroupingOf(CountryId.Sweden, cast), EntrantSimilarity.For(CountryId.Sweden, cast),   // §681, §684: the entrant layer - inert with no entrant
                doorCacePp: GotvTurnout.EuropeanDoorCacePp);   // A-LOOP q24 5b: the count reads an opened door at [CP20]'s turnout CACE (K7)
        }

        /// <summary>The 29 valkretsar as campaign regions: name and VALID votes (the audience a local action can address), from the runtime catalog
        /// of <paramref name="vintage"/> - the seated election's (2026's) unless a harness pins 2022's.</summary>
        public static RegionAudience[] SwedenRegions(out double national, ElectionVintage vintage = ElectionVintage.Seated)
        {
            bool pinned2022 = WorldClock.Resolve(CountryId.Sweden, vintage) == ElectionVintage.Sweden2022;
            string[] names = pinned2022 ? SwedishValkretsReturns2022.Names : SwedishValkretsReturns2026.Names;
            long[] validVotes = pinned2022 ? SwedishValkretsReturns2022.Valid : SwedishValkretsReturns2026.Valid;
            long[] roll = pinned2022 ? SwedishValkretsReturns2022.Eligible : SwedishValkretsReturns2026.Eligible;
            var regions = new RegionAudience[SwedishRegions.Count];
            national = 0.0;
            for (int r = 0; r < regions.Length; r++)
            {
                double valid = validVotes[r];
                // F3 (2026-09-02): the region's ELIGIBLE electorate for the ground game - who can be mobilised -
                // is Valmyndigheten's own roll for the valkrets (the returns catalog's Eligible), not its valid
                // votes: the doors an office can knock are the electorate's, not the turnout's. ⚠ It is NOT the
                // 18+ population of SwedishValkretsPopulation2024, which the per-valkrets voter-group VIEW is
                // built on: that is residents, and 11-15 % of the metropolitan valkretsar's adult residents are
                // not Swedish citizens (VoterGroupViewDiagnostic prints the ratio). A Riksdag electorate is
                // citizens on the roll, and the roll is the sourced figure.
                double eligible = roll[r];
                regions[r] = new RegionAudience(names[r], valid, eligible: eligible);
                national += valid;
            }
            return regions;
        }

        /// <summary>[AUTHORED-DRAFT] W-B4 staging: the offices each personality opens on day 0, the largest regions first - grassroots 6, populist 4, professional 3, establishment 2, chaotic 1.</summary>
        public static int[] OfficesFor(AiPersonality personality, RegionAudience[] regions) => OfficesFor(personality, regions, null);

        /// <summary>§697: the same plan among the regions where the party stands (<paramref name="stands"/>; null everywhere) - the CSU's offices are Bavarian,
        /// and a party standing in fewer regions than its personality's count opens one in each. PS-6 US-31 (DECLARED, asked): the USA's offices too are placed by audience - only the local acts follow the targeting rule.</summary>
        public static int[] OfficesFor(AiPersonality personality, RegionAudience[] regions, bool[] stands)
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
            for (int r = 0; r < regions.Length; r++) { if (stands == null || stands[r]) { order.Add(r); } }
            order.Sort((a, b) => regions[b].Audience.CompareTo(regions[a].Audience));
            return order.GetRange(0, Math.Min(count, order.Count)).ToArray();
        }

        /// <summary>PS-6 K27: the order the targeting rule <paramref name="targeting"/> contests the regions in on the staging's opening shares - the preference
        /// model's own reading of <paramref name="compatibility"/>, <paramref name="prior"/> and <paramref name="loyalty"/>, the shares an idle campaign opens on, as a
        /// poll with no error fielded on <paramref name="opens"/>; null where no rule is declared.</summary>
        public static int[] OpeningOrder(RegionTargeting targeting, DateTime opens, double[] compatibility, double[] prior, double[] loyalty)
        {
            if (targeting == null) { return null; }
            double[] opening = PreferenceModel.Preference(compatibility, prior, loyalty);
            return targeting.OrderOn(new Poll(opens, 0, "the staging's opening shares", opening, new double[opening.Length]));
        }

        /// <summary>PS-6 K27 (Elias's ruling of 2026-10-23: "the AI's offices follow the targeting rule (electors x closeness)"): the personality's office count
        /// (W-B4's, `OfficesFor`) opened in the regions <paramref name="targeted"/> contests first, heaviest
        /// first, among those where the party stands; where the rule contests fewer than the count, the rest by audience as before (the local acts' own
        /// fallback). No rule handed, the audience's plan whole.</summary>
        public static int[] OfficesByTargeting(AiPersonality personality, RegionAudience[] regions, bool[] stands, int[] targeted)
        {
            int[] byAudience = OfficesFor(personality, regions, stands);
            if (targeted == null) { return byAudience; }
            int count = OfficesFor(personality, regions, null).Length;
            var order = new List<int>();
            foreach (int r in targeted) { if (r >= 0 && r < regions.Length && (stands == null || stands[r]) && !order.Contains(r)) { order.Add(r); } }
            var rest = new List<int>();
            for (int r = 0; r < regions.Length; r++) { if ((stands == null || stands[r]) && !order.Contains(r)) { rest.Add(r); } }
            rest.Sort((a, b) => regions[b].Audience.CompareTo(regions[a].Audience));
            order.AddRange(rest);
            return order.GetRange(0, Math.Min(count, order.Count)).ToArray();
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

        /// <summary>PS-6 US-26: the staging note's words for the pair the history read - with no count of the game's own before the polling day, the seated
        /// election's years as they always read ("Sweden 2026 (SOURCED prior), loyalty derived from 2022->2026"); otherwise the game's counts by year, the earlier
        /// of the pair the record's last election at the epoch (<paramref name="epochLatest"/>) until the game has held two ("Sweden, the game count of 2030
        /// (the history rolled forward), loyalty derived from the record of 2026 to 2030").</summary>
        private static string HistoryPhrase(string country, Country played, DateTime before, int ownCounts, string latestYear, string previousYear, string epochLatest)
        {
            if (ownCounts == 0) { return country + " " + latestYear + " (SOURCED prior), loyalty derived from " + previousYear + "->" + latestYear; }
            List<(DateTime Day, double[] Shares)> own = PartySystems.OwnCounts(played, before);
            string latest = own[own.Count - 1].Day.Year.ToString(CultureInfo.InvariantCulture);
            string from = ownCounts > 1 ? "the game count of " + own[own.Count - 2].Day.Year.ToString(CultureInfo.InvariantCulture) : "the record of " + epochLatest;
            return country + ", the game count of " + latest + " (the history rolled forward), loyalty derived from " + from + " to " + latest;
        }
    }
}
