using System.Collections.Generic;
using PoliSim.Data;

namespace PoliSim.Elections
{
    /// <summary>Why a country's chamber did or did not change at an election turn - carried on the record so a screen never has to guess.</summary>
    public enum ElectionMethod
    {
        /// <summary>No live path for this country's electoral system yet. The chamber is left exactly as it was.</summary>
        NotImplemented,
        /// <summary>Sweden: modified Sainte-Lague 1.2 at a 4 % national threshold - the totalfordelning that fixes each party's national entitlement; since GATE-18 q3 the live count
        /// is allocated by the full two-tier procedure on its counted valkretsar (the 12 % route, the fixed and adjustment seats per valkrets, `SeatConversion.Sweden`).</summary>
        SwedenTwoTier,
        /// <summary>Germany: 630 seats allocated purely on nationwide Zweitstimmen by Sainte-Lague/Schepers, 5 % threshold.</summary>
        GermanyNationalProportional,
        /// <summary>§762 (PS-5): Poland's 41 districts, d'Hondt in each, the 5 % and 8 % thresholds, the minority exempt - `PolishSejmAllocation`.</summary>
        PolandDistricts,
        /// <summary>PS-6 K74 (Elias's ruling of 2026-10-23, `docs/RULINGS_2026-10-23.md`: "a US election method is added to the chamber record"): the USA's
        /// House elected on the day loop by R-US18 (b)'s state delegations (`UsCongressElection`), with the Senate seats up - its seats the House the count
        /// elected, by party; seated on 3 January by the day loop, never by the chamber path. No parliamentary formation, round or night reads it
        /// (`NationalElection.CountsParliament`). Appended last, so every stored value keeps its number.</summary>
        UsCongress,
        /// <summary>PS-7 (K150-K158): Italy's Camera by the Rosatellum - the 147 uninominal colleges on the swung 2018 returns, the national and circoscrizione stages (art. 83
        /// lett. b-i), the Estero - with the Senato counted beside it on its regional basis (`ItalianCount`). Appended last, so every stored value keeps its number.</summary>
        ItalyRosatellum,
    }

    /// <summary>
    /// W-G1: THE RECORD OF ONE ELECTION, persisted. Until W-G1 an election left only a transient
    /// `_pendingElectionResult` cleared the moment the player dismissed the reveal, which is why the
    /// Docket's calendar could never mark a past election - there was nothing to mark.
    /// </summary>
    public class ElectionRecord
    {
        public int Turn;
        /// <summary>PS-2 (§619): the polling day the election was held on - the game's own calendar date, which the verdict and the night are keyed to
        /// (the turn is the year it fell in). MinValue on the Editor's turn-labelled runs (a v25 save is refused by the loader, so no undated record is ever loaded).</summary>
        public System.DateTime Date;
        public string CountryId;
        public ElectionMethod Method;
        /// <summary>Seats won, keyed by the abbreviation `PartySystems` uses. Empty when `Method` is `NotImplemented`.</summary>
        public Dictionary<string, int> Seats = new Dictionary<string, int>();
        /// <summary>Vote share per party, same keys. Empty when `Method` is `NotImplemented`.</summary>
        public Dictionary<string, double> Shares = new Dictionary<string, double>();
        /// <summary>Plain English for a screen when `Method` is `NotImplemented` - never an empty result passed off as a real one.</summary>
        public string NotHeldReason;
        /// <summary>PL-C (K147): a Polish Sejm count's campaign movement per okreg by key (`ParliamentaryCount.Result.OkregShift`), which the chamber was
        /// divided on and the night declares (`PolishSejmNight.At`); null where no campaign moved an okreg. Saved with the record (format 68), so a night built after
        /// a load divides the votes the chamber was divided on.</summary>
        public Dictionary<string, double[]> OkregShift;
    }
    /// <summary>
    /// W-G1: an election on the live path, for the countries whose electoral system this model
    /// actually implements.
    ///
    /// TWO OF SIX, AND THE OTHER FOUR SAY SO RATHER THAN PRETENDING. Sweden and Germany are here
    /// because their real methods are built and, for Sweden, proven seat-for-seat against 2022
    /// (W-D2 / W-F1). The other four are NOT, and running them through a national PR allocator
    /// anyway would produce a chamber that is not the one their law produces:
    /// <list type="bullet">
    /// <item><description>POLAND allocates d'Hondt SEPARATELY IN 41 DISTRICTS with no national
    /// compensatory tier (Kodeks wyborczy art. 232 par. 1). A national d'Hondt is a different
    /// electoral system that happens to share a divisor, and it would systematically under-reward
    /// the large parties that district magnitude favours.</description></item>
    /// <item><description>FRANCE elects 577 single-member seats in TWO ROUNDS. There is no second
    /// round in this model and no candidate-level contest to hold one in.</description></item>
    /// <item><description>ITALY is MIXED - 147 uninominal colleges, 245 proportional seats, 8
    /// overseas - and the project's own returns file flags every per-party seat total as
    /// unconfirmed. PS-7 counts it by its own statutes (`ItalianCount`), the Senato beside it.</description></item>
    /// <item><description>USA is 435 SINGLE-MEMBER DISTRICTS, first past the post. A proportional
    /// allocation of the national House vote is famously not what that produces.</description></item>
    /// </list>
    /// For those four the chamber is left untouched and the record carries the reason. That is the
    /// section 36 rule applied to a SYSTEM rather than a figure: what is not modelled is reported as
    /// not modelled, never simulated approximately and drawn as fact.
    /// </summary>
    public static class NationalElection
    {
        /// <summary>The per-valkrets shares behind the most recent prediction, or null when the player's
        /// country has no regions wired. ⚠ **Derived from the national shares, never computed beside**
        /// them - see the swing note in <c>TryPredictShares</c>.</summary>
        public static double[][] LastRegionalShares { get; private set; }

        /// <summary>The region names, in the same order as <see cref="LastRegionalShares"/>.</summary>
        public static string[] LastRegionalNames { get; private set; }

        /// <summary>Each region's valid-vote weight - a per-constituency COUNT is share x weight.</summary>
        public static double[] LastRegionalWeights { get; private set; }

        /// <summary>⚠ **How far the regional breakdown's vote-weighted total sits from the national shares
        /// it was derived from.** Zero unless the zero-floor bit on a party that would have swung negative
        /// somewhere. **Reported rather than absorbed**: a caller drawing both numbers is entitled to know
        /// whether they agree, and a silent reconciliation is how a screen starts lying quietly.</summary>
        public static double LastRegionalWorstAbsError { get; private set; }

        /// <summary>PS-4 (§688): the party keys the regional shares' columns are in - the prediction's own order.</summary>
        public static IReadOnlyList<string> LastRegionalKeys { get; private set; }

        /// <summary>FORK-2 (the review's L1): the regional readout as it stands now, and the act that puts it back - a counterfactual world's count derives its
        /// own regions into these process-wide fields, and the readout is the played world's (election night reads it), so the count is held inside a keep.</summary>
        public static System.Action KeepRegionalReadout()
        {
            double[][] shares = LastRegionalShares;
            string[] names = LastRegionalNames;
            double[] weights = LastRegionalWeights;
            double worst = LastRegionalWorstAbsError;
            IReadOnlyList<string> keys = LastRegionalKeys;
            return () => { LastRegionalShares = shares; LastRegionalNames = names; LastRegionalWeights = weights; LastRegionalWorstAbsError = worst; LastRegionalKeys = keys; };
        }

        /// <summary>SOURCED: par. 4(2) BWahlG. (The Grundmandatsklausel that survived the BVerfG's 2024 judgment turns on constituency wins, which this national path has no notion of - stated here, not silently ignored.)</summary>
        private const double GermanThreshold = 0.05;

        /// <summary>
        /// Round 4 follow-up 1 (§704): **THE NATIONAL-MINORITY EXEMPTION.** § 4 Abs. 2 Satz 3 BWahlG (the text as served 2026-09-30,
        /// `ElectionsData/germany/raw/records/bwahlg_4_2026-09-30.html`): *"Satz 2 Nummer 2 findet keine Anwendung auf Listen, die von Parteien
        /// nationaler Minderheiten eingereicht wurden."* The Bundeswahlausschuss recognised the SSW as such a party for the 2025 election
        /// (press release 06/25 of 14 January 2025, `bwl_2025-01-14_ergebnisse-1bwa.html`: *"Auch wird die 5-Prozent-Klausel auf den SSW nicht
        /// angewendet."*), and it won one seat on 76,138 Zweitstimmen, 0.2 % (the final result, `bwl_2025_bund-99_2026-09-30.html`). The game
        /// counted every party against the 5 % line, so the SSW could never be seated. ⚠ Premise, stated: the recognition is made for each
        /// election; an election the game holds after 2025 carries it forward.
        /// </summary>
        /// <remarks>§762 (PS-5): and Poland's German minority committee - Kodeks wyborczy art. 197 § 1, the exemption on the committee's declaration,
        /// declared at the record's elections (MN; `ElectionsData/poland/returns_2023.md`, Rules); carried forward the same way.</remarks>
        public static bool ExemptFromThreshold(CountryId country, string abbrev) => (country == CountryId.Germany && abbrev == "SSW") || (country == CountryId.Poland && abbrev == "MN");

        /// <summary>GATE-18 q22: the national line a party's list must clear in <paramref name="country"/>'s chamber count, as `Run` allocates it - Sweden's,
        /// Germany's and Poland's party threshold, and since PS-7 Italy's for a list standing alone; NaN for a country whose chamber the game does not count by a national line.</summary>
        public static double ThresholdOf(CountryId country) =>
            country == CountryId.Sweden ? SeatConversion.NationalThreshold
            : country == CountryId.Germany ? GermanThreshold
            : country == CountryId.Poland ? PolishSejmAllocation.PartyThreshold
            : country == CountryId.Italy ? Rosatellum.ListThreshold   // PS-7: a list standing alone, 3 % - DPR 361/1957 art. 83 c.1 lett. e) (a coalition's lists are counted on its own 10 %)
            : double.NaN;

        /// <summary>GATE-18 q22: the line a list must clear inside one region to share in that region's seats, where the country has such a route
        /// (Sweden's, `SeatConversion.RegionalThreshold`); NaN for a country with none.</summary>
        public static double RegionalThresholdOf(CountryId country) => country == CountryId.Sweden ? SeatConversion.RegionalThreshold : double.NaN;

        /// <summary>
        /// W-G1: the vote shares an election returns for a country, through the vote model''s
        /// GOOD layer rather than its bare one.
        ///
        /// ⚠ **The bare §8 national model is not fit to seat a parliament.** `CompositionHarness`
        /// measures it at **MAD 3.25 pp** on Sweden 2022 against **1.47 pp** once the loyalty layer
        /// runs over a prior — and the difference is not academic: seating a chamber from the bare
        /// layer gave **BSW 97 Bundestag seats when it really won none**, and Sweden''s M 33 of its
        /// real 68. So the live path runs `PreferenceModel.Preference` over the last election as the
        /// prior, with per-party loyalty derived from the two elections before this one, which is
        /// exactly the arrangement the backtest reports that 1.47 pp for.
        ///
        /// Returns false when the country has no fitted electorate or no two-election history —
        /// a country without a live path — and the caller holds no election. PS-6 US-6: the USA
        /// predicts (its poll is R-US15's, `PartySystems.TryElectorate`) and still has no seat path:
        /// <see cref="Run"/> records why. PS-6 US-8: its presidential count reads this prediction on the
        /// polling day (<see cref="UsPresidentialElection.Count"/>, from the day loop) where no campaign ran - since PS-6 US-30 a campaign staged on this
        /// prediction's inputs is counted by its final shares.
        /// </summary>
        /// <summary>
        /// C-R4b step 5 (2026-09-02, D-21): the three inputs the live prediction is made of - each
        /// positioned party's COMPATIBILITY on the vote model's GOOD layer (the §8 national shares on
        /// the fitted electorate, on the compatibility scale), its PRIOR (the last election's share)
        /// and its LOYALTY (derived from the two elections before this one) - factored out so the
        /// campaign can be staged on the SAME compatibility election night predicts from. An idle
        /// campaign then reproduces this prediction exactly (`PreferenceModel.Preference` over the same
        /// three arrays), and a campaign moves it: that is what makes the campaign's result election
        /// night's result rather than a second opinion beside it. False for the countries with no
        /// fitted electorate or no two-election history.
        /// PS-6 US-26 (R-US11 (a)): handed the played country, the prior and the loyalty are its history
        /// rolled forward - its own counts before <paramref name="before"/> after the record's pair
        /// (<see cref="PartySystems.OwnCounts"/>); the compatibility reads no history.
        /// </summary>
        public static bool TryCompatibility(CountryId country, out string[] keys, out double[] compatibility, out double[] prior, out double[] loyalty,
            Country played = null, System.DateTime? before = null)
        {
            keys = null; compatibility = null; prior = null; loyalty = null;
            if (!PartySystems.TryElectorate(country, out VoteModel.Electorate electorate, out double economicWeight)) { return false; }
            // PS-6 US-26 (R-US11 (a)): the history rolled forward - the played country's own counts before the day follow the record's pair; no country
            // handed, or none held, the record's pair as before
            if (!PartySystems.TryHistory(country, played, before, out double[] latest, out double[] previous, out int _)) { return false; }

            IReadOnlyList<PoliticalParty> parties = PartySystems.For(country);
            var points = new List<VoteModel.PartyPoint>();
            var keyList = new List<string>();
            var priorList = new List<double>();
            var latestOfMeasured = new List<double>();
            var previousOfMeasured = new List<double>();
            var placed = new List<bool>();

            for (int i = 0; i < parties.Count; i++)
            {
                PoliticalParty party = parties[i];
                // Round 4 follow-up 1 (§704): A PARTY THE SURVEY DOES NOT PLACE KEEPS ITS PRIOR. It cannot be put on the model's two axes, so the spatial
                // layer can neither draw voters to it nor away from it - but it stands, and its last result is a measured share, not a made-up one. It
                // was dropped here ("it takes no share"), under a note that neither Sweden nor Germany had such a party: Germany's SSW (not in CHES 2024)
                // is one, and a German game counted it at zero - its 2021 prior discarded, its seat never won. It is carried now at full loyalty, so its
                // preference is exactly its prior, and its regions derive as every one-Land party's do (§689).
                // PS-6 US-33 (SP-6a; R-US15 (iii), the card's moved point): where the player's own nominee heads the party's ticket, the party's point in the vote
                // model is the nominee's (`OwnNominees.TryPointOf`) - the electorate stays where its history held it (`PartySystems.TryElectorate` reads the
                // roster's points), so the split moves by the spread's response. None registered, the party stands where the roster puts it, as before.
                if (party.HasPosition)
                {
                    if (OwnNominees.TryPointOf(country, party.Abbrev, out double nomineeEcon, out double nomineeSoc)) { points.Add(new VoteModel.PartyPoint(party.Abbrev, nomineeEcon, nomineeSoc)); }
                    else { points.Add(new VoteModel.PartyPoint(party.Abbrev, party.LrEcon, party.Galtan)); }
                }
                placed.Add(party.HasPosition);
                keyList.Add(party.Abbrev);
                priorList.Add(latest[i]);
                latestOfMeasured.Add(latest[i]);
                previousOfMeasured.Add(previous[i]);
            }

            double[] placedCompatibility = ToCompatScale(VoteModel.PredictShares(points.ToArray(), electorate, economicWeight));
            keys = keyList.ToArray();
            prior = priorList.ToArray();
            loyalty = LoyaltyModel.PartyLoyalties(latestOfMeasured.ToArray(), previousOfMeasured.ToArray());
            compatibility = new double[keys.Length];
            for (int i = 0, j = 0; i < keys.Length; i++)
            {
                if (placed[i]) { compatibility[i] = placedCompatibility[j++]; }
                else { compatibility[i] = 0.0; loyalty[i] = UnplacedLoyalty; }
            }
            return true;
        }

        /// <summary>§704: an unplaced party's loyalty - the scale's top, so <c>PreferenceModel</c> gives it exactly its prior share (λ = 1).</summary>
        public const double UnplacedLoyalty = ElectionScales.Max;

        /// <summary>§704: a staging's loyalties with every unplaced party set to <see cref="UnplacedLoyalty"/> - the campaign's staging reads the same rule
        /// the idle prediction does.</summary>
        public static double[] UnplacedKeepTheirPrior(IReadOnlyList<PoliticalParty> parties, double[] loyalty)
        {
            if (loyalty == null || parties == null) { return loyalty; }
            for (int i = 0; i < loyalty.Length && i < parties.Count; i++) { if (!parties[i].HasPosition) { loyalty[i] = UnplacedLoyalty; } }
            return loyalty;
        }

        /// <summary>§681: every key's awareness before a campaign - 1 for every real party (established parties seed at full awareness), a created party's
        /// from its Recognition through the pip table (`PartyOrigins.Awareness`). Null where no created party is in the keys (the layer reads null as all 1).</summary>
        public static double[] EntrantAwareness(CountryId country, IReadOnlyList<string> keys)
        {
            if (!CreatedParties.Any(country)) { return null; }
            var a = new double[keys.Count];
            for (int i = 0; i < a.Length; i++)
            {
                a[i] = 1.0;
                foreach (CreatedParty c in CreatedParties.Of(country)) { if (c.Key == keys[i]) { a[i] = PartyOrigins.Pip(PartyOrigins.Awareness, c.Recognition); } }
            }
            return a;
        }

        public static bool TryPredictShares(CountryId country, out Dictionary<string, double> shares, IReadOnlyDictionary<string, double> recordShift = null, System.DateTime? on = null,
            Country played = null)
        {
            shares = null;
            // PS-6 US-26: the played country's own counts before the day (every one, where no day is given) are the history the prediction reads
            if (!TryCompatibility(country, out string[] keys, out double[] compatibility, out double[] prior, out double[] loyalty, played, on)) { return false; }
            double[] preference = PreferenceModel.Preference(compatibility, prior, loyalty);
            preference = EntrantLayer.ApplyFor(preference, prior, country, keys, EntrantAwareness(country, keys));   // §681: an entrant's reach and where it draws from; none, the very array
            preference = EntrantLayer.RegionalAffinity(country, keys, preference, PartyFamilies.For(country, keys));   // K129: a Regional party read through its valkrets's electorate; none, the very array
            preference = EconomicVote.ApplyRecordShift(keys, preference, recordShift);   // PS-3k (§638): the government's record, before the regions are derived; none given, none applied
            preference = EntrantLayer.HoldToReach(country, keys, preference, null);   // GATE-18 q22: a created party held within its origin's reach (no campaign: the opening awareness); none registered, the very array
            if (on.HasValue) { preference = BallotAccess.ApplyToCount(country, keys, preference, on.Value); }   // SP-5 (S5): a created party's refused Land lists cost it their votes; none refused, the very array

            shares = new Dictionary<string, double>();
            for (int i = 0; i < keys.Length; i++) { shares[keys[i]] = preference[i]; }
            DeriveRegional(country, keys, preference, on);   // PS-4 (§688): a date lets a country whose regions depend on the seated chamber (Germany) derive them
            return true;
        }

        /// <summary>
        /// C-R4b step 5: election night on shares the CAMPAIGN produced rather than the prediction -
        /// the same keys, the same regional derivation, so the constituencies still add up to the
        /// headline (F1). The shares are the run's `FinalShares` in the run's party order, which is the
        /// prediction's own order for a staged country (`LiveCampaignSetup` builds on `TryCompatibility`).
        /// </summary>
        public static Dictionary<string, double> SharesFromCampaign(CountryId country, string[] keys, double[] finalShares) =>
            SharesFromCampaign(country, keys, finalShares, null, out _);

        /// <summary>
        /// [AUTHORED-DRAFT] the share of voters who reason strategically about the threshold at all
        /// (§23), 0–1 — an unsourced figure of the tactical layer, the harness's staging value since
        /// W-A4; W-F4's groups would carry it per group.
        /// </summary>
        public const double TacticalAwareness = 0.5;

        /// <summary>
        /// GATE-18 q25 [AUTHORED-DRAFT]: the window of the polls' mean the tactical layer believes, in days up to the polling day - §22's "Polling
        /// should have a moving average", read at the month its own example gives a shock to be gone in ("After one month: +0.0%",
        /// `docs/specs/ELECTIONS_CAMPAIGN_SPEC.md` §22). No source for an aggregator's window is on disk; asked.
        /// </summary>
        public const int TacticalBeliefWindowDays = 28;

        /// <summary>
        /// GATE-18 q25 (Elias, 2026-10-18: "the tactical layer reads the polls' mean, not one tracker"; GA-5-2): what the tactical layer believes about
        /// who clears the threshold - the mean of every tracker the campaign <paramref name="campaign"/> published in the `TacticalBeliefWindowDays` days
        /// up to <paramref name="pollingDay"/> (`PollingSystem.Mean`: the published series only, never a party's commissioned poll - the electorate
        /// sees what is published), in place of the last tracker alone. Null where no tracker falls in the window (the layer is then skipped, as it was
        /// with no tracker); <paramref name="included"/> counts the trackers the mean read.
        /// </summary>
        public static Poll? TacticalBelief(CampaignRun.State campaign, System.DateTime pollingDay, out int included)
        {
            included = 0;
            return campaign?.PublishedPolls == null ? null : PollingSystem.Mean(campaign.PublishedPolls, pollingDay, TacticalBeliefWindowDays, out included);
        }

        /// <summary>GATE-18 q25: the player's polling-day count of a campaign - the tactical layer over <paramref name="finalShares"/> believing
        /// `TacticalBelief` of <paramref name="campaign"/> on <paramref name="pollingDay"/>, the regional readout derived on that day. The one path the
        /// player's chamber election takes (`TacticalBeliefDiagnostic` holds it to the mean and reads the call site).</summary>
        public static Dictionary<string, double> SharesOnPublishedPolls(CountryId country, string[] keys, double[] finalShares, CampaignRun.State campaign, System.DateTime pollingDay,
            out TacticalResult tactical, out Poll? belief, out int beliefPolls)
        {
            belief = TacticalBelief(campaign, pollingDay, out beliefPolls);
            return SharesFromCampaign(country, keys, finalShares, belief, out tactical, on: pollingDay);
        }

        /// <summary>
        /// D-10 (a) (2026-09-02): the blocs the tactical layer lends within, for Sweden's 2022 parties —
        /// SOURCED as the two sides of the Riksdag that formed after the 2022 election (M, KD, L and SD
        /// at 176 seats against S, V, C and MP at 173; Valmyndigheten's returns, the Tidö agreement's
        /// four). Mapped by party key; a key outside the eight is in no bloc (−1). The harness has
        /// staged exactly this table since W-A4 and reads it from here now.
        /// </summary>
        /// <summary>
        /// P2-3.1 (2026-09-02): a party's bloc for the hemicycle's ordering and legend, the inks' bloc fence and the stance model's bloc pull
        /// (0 the left bloc, 1 the right bloc); -1 where no bloc is sourced, which is every other chamber and any party outside the table.
        /// K79/K80 (Elias, 2026-10-23: the dated sides EVERYWHERE): the sides the election's tactical layer reads (`SwedenBlocsOn`), on the day of the
        /// world that asks. Given a country, its own world's day (`Country.CalendarDay`, committed by that world's clock); given only an id, the played
        /// world's day (`SimulationManager.PlayedDay`), else - before any clock has run in the epoch - the world's opening date.
        /// </summary>
        public static int BlocOf(CountryId country, string abbrev) =>
            BlocOf(country, abbrev, Simulation.SimulationManager.PlayedDay != System.DateTime.MinValue ? Simulation.SimulationManager.PlayedDay : Simulation.SimulationManager.EpochDate);

        /// <summary>K80: the same for a country of a world - on that world's own day (a counterfactual's day, not the played world's).</summary>
        public static int BlocOf(Country country, string abbrev) =>
            country == null ? -1 : country.CalendarDay != System.DateTime.MinValue ? BlocOf(country.Id, abbrev, country.CalendarDay) : BlocOf(country.Id, abbrev);

        /// <summary>K79: the same, on a given date - the sides standing that day.</summary>
        public static int BlocOf(CountryId country, string abbrev, System.DateTime on)
        {
            if (country != CountryId.Sweden) { return -1; }
            return SwedenBlocsOn(new[] { abbrev }, on)[0];
        }

        /// <summary>The bloc's name, as the election night film names it; "unaffiliated" for the rest.</summary>
        public static string BlocName(int bloc) => bloc == 0 ? "the left bloc" : bloc == 1 ? "the right bloc" : "unaffiliated";

        public static int[] SwedenBlocs2022(IReadOnlyList<string> keys)
        {
            var bloc = new int[keys.Count];
            for (int i = 0; i < keys.Count; i++)
            {
                switch (keys[i])
                {
                    case "M": case "KD": case "L": case "SD": bloc[i] = 1; break;
                    case "S": case "V": case "C": case "MP": bloc[i] = 0; break;
                    default: bloc[i] = -1; break;
                }
            }
            return bloc;
        }

        // -----------------------------------------------------------------------------------------------------------------------------
        // K53 (Elias, 2026-10-23, on GA-0-45 premise 6): "Sweden's blocs for 2026 and later are sourced from 2026's dated declarations (MP's
        // paraphrase declared)". A party's BLOC is the side it declared - the government it goes to the election to form or back - dated by
        // §621's rules (the party's own record; a leader's words quoted verbatim on a broadcaster's own page, F2) and standing until a later
        // dated one replaces it. 2022's sides (the table above, D-10 (a)) stand from the start of the record until each party's 2026 date.
        // The sources: `ElectionsData/sweden/2026/coalition_declarations_2026.md` (the register; pages under `2026/raw/declarations/`) and
        // the pages K53's pass fetched (`ElectionsData/sweden/raw/declarations2026/`, its fetch_log.txt). The election's tactical layer reads
        // the sides standing on its polling day (`SharesFromCampaign`); since K79 every other bloc reader too (`BlocOf`, on the world's date).
        // -----------------------------------------------------------------------------------------------------------------------------

        /// <summary>K53: one party's declared side - 0 the side of S's candidacy, 1 the Tidö side, -1 neither - from <c>From</c> until <c>Until</c> (exclusive).</summary>
        public readonly struct DatedSide
        {
            public readonly string Party;
            public readonly int Bloc;
            public readonly System.DateTime From;
            public readonly System.DateTime Until;
            public readonly string Basis;

            public DatedSide(string party, int bloc, System.DateTime from, System.DateTime until, string basis)
            {
                Party = party; Bloc = bloc; From = from; Until = until; Basis = basis;
            }

            public bool StandsOn(System.DateTime date) => date.Date >= From && date.Date < Until;
        }

        private const string SidesOf2022 = "D-10 (a): the two sides of the Riksdag that formed after the 2022 election (M, KD, L and SD at 176 seats against S, V, C and MP at 173; Valmyndigheten's returns, the Tido agreement's four)";

        /// <summary>K53: Sweden's sides by date - 2022's until each party's 2026 declaration, then the 2026 side with its citation.</summary>
        public static IReadOnlyList<DatedSide> SwedenSides { get; } = new[]
        {
            new DatedSide("M", 1, System.DateTime.MinValue, new System.DateTime(2026, 4, 1), SidesOf2022),
            new DatedSide("M", 1, new System.DateTime(2026, 4, 1), System.DateTime.MaxValue,
                "DECLARED 2026-04-01: Kristersson leads a four-party majority government with SD - Moderaterna's own page [MSD-P1]"),
            new DatedSide("SD", 1, System.DateTime.MinValue, new System.DateTime(2026, 4, 1), SidesOf2022),
            new DatedSide("SD", 1, new System.DateTime(2026, 4, 1), System.DateTime.MaxValue,
                "DECLARED 2026-04-01: the agreement with M stands, Kristersson the PM - Akesson's own words on TV4's page [MSD-I4] (F2; SD's own record silent on the side, a GAP)"),
            new DatedSide("KD", 1, System.DateTime.MinValue, new System.DateTime(2026, 4, 17), SidesOf2022),
            new DatedSide("KD", 1, new System.DateTime(2026, 4, 17), System.DateTime.MaxValue,
                "DECLARED 2026-04-17: four more years of the cooperation under Kristersson - Busch's speech on Kristdemokraterna's own page [KD-P1]; restated 2026-08-30 on SVT's page [KD-I4]"),
            new DatedSide("L", 1, System.DateTime.MinValue, new System.DateTime(2026, 3, 13), SidesOf2022),
            new DatedSide("L", 1, new System.DateTime(2026, 3, 13), System.DateTime.MaxValue,
                "DECLARED 2026-03-13: all four Tido parties in the government if they win - Liberalerna's own page [L-P1]; restated 2026-06-02 with Kristersson as PM [L-P3]"),
            new DatedSide("S", 0, System.DateTime.MinValue, new System.DateTime(2026, 5, 1), SidesOf2022),
            new DatedSide("S", 0, new System.DateTime(2026, 5, 1), System.DateTime.MaxValue,
                "DECLARED 2026-05-01: Andersson seeks the mandate to be prime minister - the party's own pages [S-P2], [S-P1]; the side of the other candidacy (K-1f's pair), S naming no partner (DECLARED reading)"),
            new DatedSide("V", 0, System.DateTime.MinValue, new System.DateTime(2026, 4, 18), SidesOf2022),
            new DatedSide("V", 0, new System.DateTime(2026, 4, 18), System.DateTime.MaxValue,
                "DECLARED 2026-04-18 (the congress decision): a red-green government V sits in, with S and MP first - Vansterpartiet's platform [V-P1]"),
            new DatedSide("MP", 0, System.DateTime.MinValue, new System.DateTime(2026, 1, 22), SidesOf2022),
            new DatedSide("MP", 0, new System.DateTime(2026, 1, 22), System.DateTime.MaxValue,
                "DECLARED 2026-01-22: S the government partner - Hellden's own words on SVT's page [MP-I3] (F2): asked whether MP's nuclear line in an S-led government means a crisis before it takes office, he answers he thinks not and hopes S changes its analysis (the premise of governing with S accepted - a DECLARED reading); TT's paraphrase on Nyheter24 [MP-I4], 2026-04-04, names S the intended partner; MP's own pages name no partner (a GAP: raw/declarations2026/), and its page of 2026-08-24 refuses a right-leaning government"),
            new DatedSide("C", 0, System.DateTime.MinValue, new System.DateTime(2025, 11, 13), SidesOf2022),
            new DatedSide("C", -1, new System.DateTime(2025, 11, 13), new System.DateTime(2026, 6, 9),
                "DECLARED 2025-11-13: ready to negotiate with Andersson or Kristersson - Thand Ringqvist's installation speech on Centerpartiet's own page [C-P6] - neither side until a later declaration replaces it"),
            new DatedSide("C", 0, new System.DateTime(2026, 6, 9), System.DateTime.MaxValue,
                "DECLARED 2026-06-09 (K80, Elias, 2026-10-23): Andersson and S the most likely partner - Thand Ringqvist's own words on SVT's page [C-I11] (F2), at the party's press conference; C's own pages of 2026-09-07, 09-09 and 09-12 (raw/declarations2026/) declare a government in the middle without V or SD and name no prime minister, so they replace nothing"),
        };

        /// <summary>K53: the sides standing on <paramref name="on"/>, mapped by key as <c>SwedenBlocs2022</c> is; a key with no dated side is in no bloc (-1).</summary>
        public static int[] SwedenBlocsOn(IReadOnlyList<string> keys, System.DateTime on)
        {
            var bloc = new int[keys.Count];
            for (int i = 0; i < keys.Count; i++)
            {
                bloc[i] = -1;
                foreach (DatedSide side in SwedenSides) { if (side.Party == keys[i] && side.StandsOn(on)) { bloc[i] = side.Bloc; break; } }
            }
            return bloc;
        }

        /// <summary>
        /// D-10 (a): election night with the TACTICAL layer (§23) over the campaign's shares. The belief
        /// each aware voter holds about a party clearing the threshold comes from the last PUBLISHED
        /// tracker of the campaign — the poll the electorate saw, never the truth — and the layer lends
        /// within the sourced blocs along CHES `lrgen` affinity. Without a tracker (a campaign that never
        /// published one) the layer is skipped and the shares stand; `tactical` is null then. The
        /// regional breakdown is derived from the shifted shares, so the constituencies add up to the
        /// headline the layer produced.
        /// </summary>
        public static Dictionary<string, double> SharesFromCampaign(CountryId country, string[] keys, double[] finalShares, Poll? tracker, out TacticalResult tactical, System.DateTime? on = null)
        {
            tactical = null;
            double[] counted = finalShares;
            if (tracker.HasValue && tracker.Value.PartyCount == keys.Length && country == CountryId.Sweden)
            {
                var polled = new double[keys.Length];
                var moe = new double[keys.Length];
                for (int i = 0; i < keys.Length; i++) { polled[i] = tracker.Value.Share(i); moe[i] = tracker.Value.MarginOfErrorPp(i); }
                IReadOnlyList<PoliticalParty> parties = PartySystems.For(country);
                var position = new double[keys.Length];
                for (int i = 0; i < keys.Length; i++)
                {
                    position[i] = 5.0;   // no published lrgen: the bloc's centre, so affinity neither favours nor shuns the party
                    foreach (PoliticalParty party in parties) { if (party.Abbrev == keys[i] && !float.IsNaN(party.LrGen)) { position[i] = party.LrGen; break; } }
                }
                // K53: the sides standing on the polling day (an undated call reads 2022's, as before)
                var spec = new TacticalSpec(SeatConversion.NationalThreshold, TacticalAwareness, on.HasValue ? SwedenBlocsOn(keys, on.Value) : SwedenBlocs2022(keys), position);
                tactical = TacticalVoting.Apply(finalShares, polled, moe, spec);
                counted = tactical.Preference;
            }
            if (on.HasValue) { counted = BallotAccess.ApplyToCount(country, keys, counted, on.Value); }   // SP-5 (S5): a created party's refused Land lists cost it their votes; none refused, the very array
            var shares = new Dictionary<string, double>();
            for (int i = 0; i < keys.Length && i < counted.Length; i++) { shares[keys[i]] = counted[i]; }
            DeriveRegional(country, keys, counted, on);   // §707: the polling day - Germany derives its Länder on it (a campaign's count dropped it, and no German night was ever shown)
            return shares;
        }

        /// <summary>GATE-18 q2: the regional breakdown `DeriveRegional` writes into the readout, returned instead - each region's shares in
        /// <paramref name="keys"/>' order, its name and its valid-vote weight - for the count that starts from it (`ParliamentaryCount`); null for a
        /// country with no regions wired (Germany without a date).</summary>
        internal static double[][] RegionalOf(CountryId country, IReadOnlyList<string> keys, double[] preference, System.DateTime on, out string[] names, out double[] weights) =>
            RegionalOf(country, keys, preference, on, out names, out weights, null);

        /// <summary>Regional 5a (K147): as the five-argument form, each region's prior moved first by <paramref name="priorShift"/> (the region's name and a key's index
        /// give the campaign's movement there, a fraction; each moved share held at zero and the row rescaled to its sum before the movement) - so the uniform swing, re-solved to the national shares, carries the
        /// campaign's regional pattern: the region a contact landed in keeps it, and the regions still sum to the national shares. Null shift: the form above.</summary>
        internal static double[][] RegionalOf(CountryId country, IReadOnlyList<string> keys, double[] preference, System.DateTime on, out string[] names, out double[] weights, System.Func<string, int, double> priorShift)
        {
            names = null;
            weights = null;
            RegionalVoteModel.RegionInput[] regions;
            double[][] prior;
            if (country == CountryId.Sweden) { regions = SwedishRegions.Regions(keys); prior = SwedishRegions.PriorShares(keys); }
            else if (country == CountryId.Germany) { regions = GermanRegions.Regions(keys, on); prior = GermanRegions.PriorShares(keys, on); }
            else { return null; }
            if (priorShift != null)
            {
                // DECLARED (reviews L3 and L-r1): a share the movement would push below zero is held at zero, and where any was the region's row is rescaled to the
                // sum it had before the movement - the row's sum is kept, so the movement adds no vote to the region. The clamped party's unrealised movement is NOT
                // kept there: the national shares handed still carry the campaign's whole movement, so the swing's re-solve spreads the part the clamp withheld over
                // every region where that party stands (and the rescale takes the region's excess from all its parties in proportion, the gainer's included). With
                // no clamp the arithmetic is the plain sum, bit for bit.
                for (int r = 0; r < regions.Length; r++)
                {
                    double before = 0.0, after = 0.0;
                    bool clamped = false;
                    for (int p = 0; p < keys.Count; p++)
                    {
                        before += prior[r][p];
                        double v = prior[r][p] + priorShift(regions[r].Name, p);
                        if (v < 0.0) { v = 0.0; clamped = true; }
                        prior[r][p] = v;
                        after += v;
                    }
                    if (clamped && after > 0.0) { for (int p = 0; p < keys.Count; p++) { prior[r][p] *= before / after; } }
                }
            }
            double[][] shares = RegionalVoteModel.RegionalSharesByUniformSwing(preference, regions, prior, out double _);
            names = new string[regions.Length];
            weights = new double[regions.Length];
            for (int r = 0; r < regions.Length; r++) { names[r] = regions[r].Name; weights[r] = regions[r].ElectorateWeight; }
            return shares;
        }

        /// <summary>GATE-18 q2: the readout set to the regions as COUNTED (`ParliamentaryCount`) - each region's counted shares and its votes cast as
        /// its weight, so the night's constituencies sum to the headline the count produced (F1), exactly: the national shares ARE their vote-weighted sum.</summary>
        internal static void SetCountedReadout(IReadOnlyList<string> keys, string[] names, double[][] shares, double[] weights)
        {
            LastRegionalKeys = keys;
            LastRegionalNames = names;
            LastRegionalShares = shares;
            LastRegionalWeights = weights;
            LastRegionalWorstAbsError = 0.0;
        }

        private static void DeriveRegional(CountryId country, IReadOnlyList<string> keys, double[] preference, System.DateTime? on = null)
        {
            LastRegionalKeys = keys;

            // ⚠ F1 (2026-09-01): THE REGIONAL BREAKDOWN, DERIVED FROM the national result rather than
            // computed beside it. RegionalSharesByUniformSwing applies one swing to every valkrets' own
            // 2022 prior, so the vote-weighted regional total REPRODUCES the shares above. A screen whose
            // constituencies did not add up to the headline is the exact failure F1 forbids, and making
            // the two independent computations is how that failure happens.
            //
            // ⚠ The 2022 file is the PRIOR, never the result: it says where each valkrets started and how
            // many votes it casts, and the model supplies the movement. Without it every region would
            // return the national percentages unchanged - all eight parties stand everywhere - and
            // election night would declare Stockholm and Skane identical, which is false and would look
            // exactly like a working screen.
            if (country == CountryId.Sweden)
            {
                RegionalVoteModel.RegionInput[] regions = SwedishRegions.Regions(keys);
                double[][] regionPrior = SwedishRegions.PriorShares(keys);
                LastRegionalShares = RegionalVoteModel.RegionalSharesByUniformSwing(
                    preference, regions, regionPrior, out double worstAbsError);
                LastRegionalNames = new string[regions.Length];
                LastRegionalWeights = new double[regions.Length];
                for (int r = 0; r < regions.Length; r++)
                {
                    LastRegionalNames[r] = regions[r].Name;
                    LastRegionalWeights[r] = regions[r].ElectorateWeight;
                }

                LastRegionalWorstAbsError = worstAbsError;
            }
            else if (country == CountryId.Germany && on.HasValue)
            {
                // PS-4 (§688): the sixteen Länder on the chamber seated that day (GermanRegions: 2021's count before the 2025 chamber sat, 2025's
                // after), by the same uniform swing - a readout of the national result, never an input to it. A party standing in a subset of the
                // Länder (the CSU, the SSW) has its swing spread over its own Länder and scaled, so its Länder sum to its national share (§689).
                RegionalVoteModel.RegionInput[] regions = GermanRegions.Regions(keys, on.Value);
                LastRegionalShares = RegionalVoteModel.RegionalSharesByUniformSwing(preference, regions, GermanRegions.PriorShares(keys, on.Value), out double worstAbsError);
                LastRegionalNames = new string[regions.Length];
                LastRegionalWeights = new double[regions.Length];
                for (int r = 0; r < regions.Length; r++) { LastRegionalNames[r] = regions[r].Name; LastRegionalWeights[r] = regions[r].ElectorateWeight; }
                LastRegionalWorstAbsError = worstAbsError;
            }
            else
            {
                // ⚠ Not "no regions" but "this country has none wired" - a caller deciding whether to
                // draw an election night at all needs the difference.
                LastRegionalShares = null;
                LastRegionalNames = null;
                LastRegionalWeights = null;
                LastRegionalWorstAbsError = 0.0;
            }
        }

        /// <summary>The backtest''s own share-to-compatibility mapping, so the live path and `CompositionHarness` cannot disagree about what the model is.</summary>
        private static double[] ToCompatScale(double[] shares)
        {
            double max = 0.0;
            foreach (double s in shares) { if (s > max) { max = s; } }
            var scaled = new double[shares.Length];
            for (int i = 0; i < shares.Length; i++)
            {
                scaled[i] = max > 0 ? 100.0 * System.Math.Pow(shares[i] / max, 1.0 / PreferenceModel.Sharpness) : 0.0;
            }

            return scaled;
        }
        /// <summary>PS-6 K74: whether a record of <paramref name="method"/> counted a PARLIAMENTARY chamber - the one every formation, Speaker's round, extra-election
        /// window, election night and reference row reads: every method but `NotImplemented` (nothing counted) and
        /// `UsCongress` (the US Congress, elected and seated on the day loop, where no parliamentary formation follows).</summary>
        public static bool CountsParliament(ElectionMethod method) => method != ElectionMethod.NotImplemented && method != ElectionMethod.UsCongress;

        /// <summary>Runs the country's own procedure on a set of national vote shares, or reports that the model does not implement it. GATE-18 q3: where
        /// <paramref name="counted"/> carries Sweden's counted valkretsar (`ParliamentaryCount.Count`), the Riksdag is allocated on them by its own law and
        /// the allocation is kept on the count (`ParliamentaryCount.Result.Riksdag`); <paramref name="regionRosterShare"/> is each counted valkrets's
        /// roster share of its valid vote on the polling day of an election of record (`RecordFactors.RosterShareByRegion`), null on every other day.</summary>
        public static ElectionRecord Run(CountryId country, int turn, IReadOnlyDictionary<string, double> shares, System.DateTime date = default, double rosterShareOfVote = 1.0,
            ParliamentaryCount.Result counted = null, double[] regionRosterShare = null, bool logFallback = true)
        {
            var record = new ElectionRecord { Turn = turn, Date = date, CountryId = country.ToString() };

            switch (country)
            {
                case CountryId.Germany:
                    record.Method = ElectionMethod.GermanyNationalProportional;
                    Allocate(record, country, shares, PartySystems.ChamberSeats(country), GermanThreshold,
                        SeatAllocation.SainteLagueDivisor, rosterShareOfVote);
                    return record;

                case CountryId.Sweden:
                    record.Method = ElectionMethod.SwedenTwoTier;
                    // GATE-18 q3 (Elias, 2026-10-18: "wire Sweden's 12 % route and per-valkrets seats into the live count"): a count that carries its
                    // valkretsar is allocated by the Riksdag's own law on them
                    if (counted != null && counted.Regional != null && counted.RegionKeys != null && AllocateRiksdag(record, shares, date, rosterShareOfVote, counted, regionRosterShare, logFallback))
                    {
                        return record;
                    }
                    // A call with no count (the backtests, the harnesses) carries no votes PER CONSTITUENCY. What runs then is the same modified
                    // Sainte-Lague at the same 4 % threshold that the two-tier procedure's own totalfordelning uses to fix each party's national
                    // entitlement - the chamber's composition whenever no list takes seats by the 12 % route alone.
                    Allocate(record, country, shares, PartySystems.ChamberSeats(country),
                        SeatConversion.NationalThreshold, SeatAllocation.ModifiedSainteLagueDivisor, rosterShareOfVote);
                    return record;

                case CountryId.Poland:
                    // §762 (PS-5): the Sejm by its own law - the national vote spread over the 41 okregi by a uniform swing from 2023, d'Hondt in each
                    // among the lists that clear 5 % (a coalition 8 %; the minority exempt) - no national tier (PolishSejmAllocation)
                    record.Method = ElectionMethod.PolandDistricts;
                    foreach (PoliticalParty party in PartySystems.For(country)) { record.Shares[party.Abbrev] = shares != null && shares.TryGetValue(party.Abbrev, out double s) ? s : 0.0; }
                    record.OkregShift = counted?.OkregShift;   // PL-C (K147): the okregi the campaign moved, divided as counted
                    foreach (KeyValuePair<string, int> kv in PolishSejmAllocation.Allocate(record.Shares, out _, rosterShareOfVote, record.OkregShift)) { record.Seats[kv.Key] = kv.Value; }
                    return record;

                case CountryId.Italy:
                    // PS-7 (K150-K158): the Camera by the statutes in force on 25 Sep 2022 - the colleges on the swung 2018 returns, art. 83's national and
                    // circoscrizione stages, the Estero (ItalianCount); the Senato is counted beside it by the player's election (SimulationManager)
                    record.Method = ElectionMethod.ItalyRosatellum;
                    foreach (PoliticalParty party in PartySystems.For(country)) { record.Shares[party.Abbrev] = shares != null && shares.TryGetValue(party.Abbrev, out double s) ? s : 0.0; }
                    foreach (KeyValuePair<string, int> kv in ItalianCount.Camera(record.Shares, rosterShareOfVote).Seats) { if (record.Shares.ContainsKey(kv.Key)) { record.Seats[kv.Key] = kv.Value; } }
                    return record;

                default:
                    record.Method = ElectionMethod.NotImplemented;
                    record.NotHeldReason = NotHeldReason(country);
                    return record;
            }
        }
        /// <summary>PS-6 US-27: <see cref="NotHeldReason(CountryId)"/> for a polling day - a US midterm's own words (no president is elected that day); every other
        /// day and country the reason as before.</summary>
        public static string NotHeldReasonOn(CountryId country, System.DateTime pollingDay) =>
            country == CountryId.USA && UsCongressElection.IsMidterm(pollingDay)
                ? "No president is elected at a midterm: the House and the Senate seats up are elected state by state; the new Congress is seated on the third of January, and until then the House and Senate in office sit."
                : NotHeldReason(country);

        /// <summary>Plain English, per country, for a screen. Never a shrug.</summary>
        public static string NotHeldReason(CountryId country)
        {
            switch (country)
            {
                case CountryId.France:
                    return "France elects 577 single-member seats in two rounds. This model holds no second round.";
                case CountryId.Italy:
                    // PS-7: Italy's Camera and Senato are counted (ItalianCount); a day this reason is read is a day the vote model could not predict
                    return "No Italian election could be counted: the vote model holds no prediction for Italy on this day, so the Camera and the Senato in office stay.";
                case CountryId.USA:
                    // PS-6 US-8 (R-US1 (a), RULED - F8: "after that only Congress, until the game elects it"), in place of US-1's line: the polling day elects the
                    // president through the Electoral College (the day loop's count) and no chamber - Congress stays the record's, by date, until the House races
                    // are built (US-16); PS-6 US-15: the Senate is the record's too, senator by senator on its dates, until its races are built (US-16).
                    // PS-6 US-16: the polling day now elects the House and a Senate class too - on the day loop, beside the president (UsCongressElection), seated
                    // on 3 January - so no chamber is elected through this path, and the words say where it is
                    return "The president is elected through the Electoral College, and the House and the Senate seats up are elected on the same day, " +
                           "state by state; the new Congress is seated on the third of January, and until then the House and Senate in office sit.";
                default:
                    return "This model does not implement this country's electoral system.";
            }
        }

        private static void Allocate(ElectionRecord record, CountryId country,
            IReadOnlyDictionary<string, double> shares, int chamber, double threshold,
            System.Func<int, double> divisor, double rosterShareOfVote = 1.0)
        {
            IReadOnlyList<PoliticalParty> parties = PartySystems.For(country);
            var keys = new List<string>();
            var votes = new List<long>();
            var exempt = new List<bool>();   // §704: a national minority's list is not held to the threshold
            long total = 0;

            foreach (PoliticalParty party in parties)
            {
                exempt.Add(ExemptFromThreshold(country, party.Abbrev));
                double share = shares != null && shares.TryGetValue(party.Abbrev, out double s) ? s : 0.0;
                // Shares to notional votes: only the RATIOS matter to a divisor method, and a large
                // scale keeps a small party's rounding from deciding whether it clears the threshold.
                long v = (long)System.Math.Round(share * 100_000_000.0);
                keys.Add(party.Abbrev);
                votes.Add(v);
                total += v;
                record.Shares[party.Abbrev] = share;
            }

            // §704: the threshold's base is ALL valid votes (§ 4 Abs. 2 BWahlG: "der im Wahlgebiet abgegebenen gültigen Zweitstimmen"). Shares are fractions
            // of the valid vote; where they sum to less than one, the rest went to lists the roster does not carry, and they still count in the base - on
            // the official 2025 counts the BSW's 4.981 % read 5.21 % of the roster's sum and would have cleared the line. A game's shares sum to one.
            // GATE-18 piece two: on the polling day of an election of record the shares are the roster's vote and the lists it does not carry are the rest of the
            // base - the roster's share of the valid vote as that election's returns give it (`RecordFactors.RosterShareOfVote`); one on every other day
            long thresholdBase = System.Math.Max(total, rosterShareOfVote == 1.0 ? 100_000_000L : (long)System.Math.Round(100_000_000.0 / rosterShareOfVote));
            int[] seats = SeatAllocation.AllocateWithThreshold(votes.ToArray(), thresholdBase, threshold, chamber, divisor, exempt.ToArray());
            for (int i = 0; i < keys.Count; i++)
            {
                record.Seats[keys[i]] = seats[i];
            }
        }

        /// <summary>
        /// GATE-18 q3: the Riksdag by its own law on the count's own valkretsar - each counted valkrets's whole votes per list (`ParliamentaryCount.Result.Regional`),
        /// the fixed seats Valmyndigheten decided for the polling day's election (`SeatConversion.DecidedFixedSeats`; the statute's own rule over the count's
        /// electorate where no decision on disk names the valkretsar), then `SeatConversion.Sweden`: the 4 % national line or 12 % in a valkrets for its
        /// fixed seats, the fixed seats per valkrets, the totalfördelning, the return and the adjustment seats.
        /// <para><b>The bases</b> (regeringsformen 3 kap. 7 §: shares of all the votes): the 4 % line reads the count's national vote over the record's base
        /// on the polling day of an election of record, as §913 has it (<paramref name="rosterShareOfVote"/>, `RecordFactors.RosterShareOfVote`: the counted
        /// vote is the roster's, the lists it does not carry the rest); the 12 % line reads each counted valkrets's votes over that valkrets's own record base
        /// that day (<paramref name="regionRosterShare"/>, the roster's share of the valkrets's valid vote by the same election's returns per valkrets -
        /// DECLARED: the per-valkrets form of §913's reading), and over the valkrets's counted votes on every other day, when the game's lists are the whole
        /// vote. False, with nothing written, where the count carries no electorate to fall back on.</para>
        /// </summary>
        private static bool AllocateRiksdag(ElectionRecord record, IReadOnlyDictionary<string, double> shares, System.DateTime date, double rosterShareOfVote,
            ParliamentaryCount.Result counted, double[] regionRosterShare, bool logFallback)
        {
            IReadOnlyList<PoliticalParty> parties = PartySystems.For(CountryId.Sweden);
            ElectionDay.Result regional = counted.Regional;
            int regions = regional.Regions.Length;
            if (regions == 0) { return false; }
            int[] fixedSeats = SeatConversion.DecidedFixedSeats(date, counted.RegionNames);
            if (fixedSeats == null)
            {
                var eligible = new double[regions];
                double electorate = 0.0;
                for (int r = 0; r < regions; r++) { eligible[r] = regional.Regions[r].EligibleVoters; electorate += eligible[r]; }
                if (!(electorate > 0.0)) { return false; }
                fixedSeats = SeatConversion.FixedSeatsPerRegion(eligible);
                // the review's L-5: the fallback says so - a renamed valkrets or a day before any decision on disk would otherwise pass unseen; the played world only (the review2 L-3: the shadow and the forks count the same day)
                if (logFallback) { UnityEngine.Debug.Log($"COUNT: Sweden on {date:yyyy-MM-dd} - no decided fixed-seat table names the counted valkretsar for this polling day; the fixed seats were drawn by the statute's 310th-part rule over the count's electorate"); }
            }

            var column = new int[parties.Count];
            for (int p = 0; p < parties.Count; p++) { column[p] = counted.RegionKeys.IndexOf(parties[p].Abbrev); }
            var votes = new long[regions][];
            var regionBase = new long[regions];
            long total = 0;
            for (int r = 0; r < regions; r++)
            {
                double[] v = regional.Regions[r].Votes;
                double all = 0.0;
                foreach (double x in v) { all += x; }   // every list the count carries is in the valkrets's valid vote, seated or not
                votes[r] = new long[parties.Count];
                for (int p = 0; p < parties.Count; p++) { votes[r][p] = column[p] >= 0 ? (long)System.Math.Round(v[column[p]]) : 0L; }
                long sum = (long)System.Math.Round(all);
                double rho = regionRosterShare != null && r < regionRosterShare.Length && regionRosterShare[r] > 0.0 && regionRosterShare[r] < 1.0 ? regionRosterShare[r] : 1.0;
                regionBase[r] = rho == 1.0 ? sum : (long)System.Math.Round(sum / rho);
                total += sum;
            }
            long nationalBase = rosterShareOfVote == 1.0 ? total : (long)System.Math.Round(total / rosterShareOfVote);

            SeatConversion.Result law = SeatConversion.Sweden(votes, fixedSeats, nationalBase, regionBase);
            for (int p = 0; p < parties.Count; p++)
            {
                string key = parties[p].Abbrev;
                record.Shares[key] = shares != null && shares.TryGetValue(key, out double s) ? s : 0.0;
                record.Seats[key] = law.Seats[p];
            }
            counted.Riksdag = law;
            return true;
        }
    }
}
