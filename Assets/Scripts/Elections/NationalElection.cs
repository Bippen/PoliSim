using System.Collections.Generic;
using PoliSim.Data;

namespace PoliSim.Elections
{
    /// <summary>Why a country's chamber did or did not change at an election turn - carried on the record so a screen never has to guess.</summary>
    public enum ElectionMethod
    {
        /// <summary>No live path for this country's electoral system yet. The chamber is left exactly as it was.</summary>
        NotImplemented,
        /// <summary>Sweden: modified Sainte-Lague 1.2 at a 4 % national threshold - the totalfordelning that fixes each party's national entitlement. W-D2 reproduces the 2022 chamber seat-for-seat through the full two-tier path.</summary>
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
    /// unconfirmed.</description></item>
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
            preference = EconomicVote.ApplyRecordShift(keys, preference, recordShift);   // PS-3k (§638): the government's record, before the regions are derived; none given, none applied

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
        /// (§23), 0–1 — the tactical layer's one unsourced figure, the harness's staging value since
        /// W-A4; W-F4's groups would carry it per group.
        /// </summary>
        public const double TacticalAwareness = 0.5;

        /// <summary>
        /// D-10 (a) (2026-09-02): the blocs the tactical layer lends within, for Sweden's 2022 parties —
        /// SOURCED as the two sides of the Riksdag that formed after the 2022 election (M, KD, L and SD
        /// at 176 seats against S, V, C and MP at 173; Valmyndigheten's returns, the Tidö agreement's
        /// four). Mapped by party key; a key outside the eight is in no bloc (−1). The harness has
        /// staged exactly this table since W-A4 and reads it from here now.
        /// </summary>
        /// <summary>
        /// P2-3.1 (2026-09-02): a party's bloc for the hemicycle's ordering and legend - the same 2022 table
        /// the tactical model reads for Sweden (0 the left bloc, 1 the right bloc); -1 where no bloc is
        /// sourced, which is every other chamber and any party outside the table.
        /// </summary>
        public static int BlocOf(CountryId country, string abbrev)
        {
            if (country != CountryId.Sweden) { return -1; }
            return SwedenBlocs2022(new[] { abbrev })[0];
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
                var spec = new TacticalSpec(SeatConversion.NationalThreshold, TacticalAwareness, SwedenBlocs2022(keys), position);
                tactical = TacticalVoting.Apply(finalShares, polled, moe, spec);
                counted = tactical.Preference;
            }
            var shares = new Dictionary<string, double>();
            for (int i = 0; i < keys.Length && i < counted.Length; i++) { shares[keys[i]] = counted[i]; }
            DeriveRegional(country, keys, counted, on);   // §707: the polling day - Germany derives its Länder on it (a campaign's count dropped it, and no German night was ever shown)
            return shares;
        }

        /// <summary>GATE-18 q2: the regional breakdown `DeriveRegional` writes into the readout, returned instead - each region's shares in
        /// <paramref name="keys"/>' order, its name and its valid-vote weight - for the count that starts from it (`ParliamentaryCount`); null for a
        /// country with no regions wired (Germany without a date).</summary>
        internal static double[][] RegionalOf(CountryId country, IReadOnlyList<string> keys, double[] preference, System.DateTime on, out string[] names, out double[] weights)
        {
            names = null;
            weights = null;
            RegionalVoteModel.RegionInput[] regions;
            double[][] prior;
            if (country == CountryId.Sweden) { regions = SwedishRegions.Regions(keys); prior = SwedishRegions.PriorShares(keys); }
            else if (country == CountryId.Germany) { regions = GermanRegions.Regions(keys, on); prior = GermanRegions.PriorShares(keys, on); }
            else { return null; }
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

        /// <summary>Runs the country's own procedure on a set of national vote shares, or reports that the model does not implement it.</summary>
        public static ElectionRecord Run(CountryId country, int turn, IReadOnlyDictionary<string, double> shares, System.DateTime date = default)
        {
            var record = new ElectionRecord { Turn = turn, Date = date, CountryId = country.ToString() };

            switch (country)
            {
                case CountryId.Germany:
                    record.Method = ElectionMethod.GermanyNationalProportional;
                    Allocate(record, country, shares, PartySystems.ChamberSeats(country), GermanThreshold,
                        SeatAllocation.SainteLagueDivisor);
                    return record;

                case CountryId.Sweden:
                    record.Method = ElectionMethod.SwedenTwoTier;
                    // Sweden's full two-tier procedure needs votes PER CONSTITUENCY, which a national
                    // share vector does not carry. What runs here is the same modified Sainte-Lague
                    // at the same 4 % threshold that the two-tier procedure's own totalfordelning
                    // uses to fix each party's national entitlement - and that entitlement IS the
                    // chamber's composition. Placing those seats across the 29 valkretsar is
                    // SeatConversion.Sweden, and it needs a regional count to do it.
                    Allocate(record, country, shares, PartySystems.ChamberSeats(country),
                        SeatConversion.NationalThreshold, SeatAllocation.ModifiedSainteLagueDivisor);
                    return record;

                case CountryId.Poland:
                    // §762 (PS-5): the Sejm by its own law - the national vote spread over the 41 okregi by a uniform swing from 2023, d'Hondt in each
                    // among the lists that clear 5 % (a coalition 8 %; the minority exempt) - no national tier (PolishSejmAllocation)
                    record.Method = ElectionMethod.PolandDistricts;
                    foreach (PoliticalParty party in PartySystems.For(country)) { record.Shares[party.Abbrev] = shares != null && shares.TryGetValue(party.Abbrev, out double s) ? s : 0.0; }
                    foreach (KeyValuePair<string, int> kv in PolishSejmAllocation.Allocate(record.Shares, out _)) { record.Seats[kv.Key] = kv.Value; }
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
                    // PS-7's "until then" (PS-7-interim): what holds instead, until Italy's mixed-system stages and the Senato are built (PS-7) - never the snap election's day
                    return "No Italian election is held in this game yet: the Camera and the government seated at the start hold. " +
                           "Italy is a mixed system - 147 uninominal colleges, 245 proportional seats, 8 overseas - that the game does not count yet, " +
                           "and the per-party seat totals on disk are unconfirmed; no Senato is modelled.";
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
            System.Func<int, double> divisor)
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
            long thresholdBase = System.Math.Max(total, 100_000_000L);
            int[] seats = SeatAllocation.AllocateWithThreshold(votes.ToArray(), thresholdBase, threshold, chamber, divisor, exempt.ToArray());
            for (int i = 0; i < keys.Count; i++)
            {
                record.Seats[keys[i]] = seats[i];
            }
        }
    }
}
