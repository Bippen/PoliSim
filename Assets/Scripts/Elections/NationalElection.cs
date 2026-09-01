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
    }

    /// <summary>
    /// W-G1: THE RECORD OF ONE ELECTION, persisted. Until W-G1 an election left only a transient
    /// `_pendingElectionResult` cleared the moment the player dismissed the reveal, which is why the
    /// Docket's calendar could never mark a past election - there was nothing to mark.
    /// </summary>
    public class ElectionRecord
    {
        public int Turn;
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

        /// <summary>SOURCED: par. 4(2) BWahlG. (The Grundmandatsklausel that survived the BVerfG's 2024 judgment turns on constituency wins, which this national path has no notion of - stated here, not silently ignored.)</summary>
        private const double GermanThreshold = 0.05;

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
        /// the four countries without a live path — and the caller holds no election.
        /// </summary>
        public static bool TryPredictShares(CountryId country, out Dictionary<string, double> shares)
        {
            shares = null;
            if (!PartySystems.TryElectorate(country, out VoteModel.Electorate electorate, out double economicWeight)) { return false; }
            if (!PartySystems.TryHistory(country, out double[] latest, out double[] previous)) { return false; }

            IReadOnlyList<PoliticalParty> parties = PartySystems.For(country);
            var points = new List<VoteModel.PartyPoint>();
            var keys = new List<string>();
            var prior = new List<double>();
            var latestOfMeasured = new List<double>();
            var previousOfMeasured = new List<double>();

            for (int i = 0; i < parties.Count; i++)
            {
                PoliticalParty party = parties[i];
                // A party with no published position cannot be placed on the model''s two axes. It
                // stands, it simply cannot be predicted - so it takes no share rather than a made-up
                // one. Neither Sweden nor Germany has such a party, but the guard is real for the
                // countries a live path may be built for later.
                if (!party.HasPosition) { continue; }
                points.Add(new VoteModel.PartyPoint(party.Abbrev, party.LrEcon, party.Galtan));
                keys.Add(party.Abbrev);
                prior.Add(latest[i]);
                latestOfMeasured.Add(latest[i]);
                previousOfMeasured.Add(previous[i]);
            }

            double[] national = VoteModel.PredictShares(points.ToArray(), electorate, economicWeight);
            double[] loyalty = LoyaltyModel.PartyLoyalties(latestOfMeasured.ToArray(), previousOfMeasured.ToArray());
            double[] preference = PreferenceModel.Preference(ToCompatScale(national), prior.ToArray(), loyalty);

            shares = new Dictionary<string, double>();
            for (int i = 0; i < keys.Count; i++) { shares[keys[i]] = preference[i]; }

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
            else
            {
                // ⚠ Not "no regions" but "this country has none wired" - a caller deciding whether to
                // draw an election night at all needs the difference.
                LastRegionalShares = null;
                LastRegionalNames = null;
                LastRegionalWeights = null;
                LastRegionalWorstAbsError = 0.0;
            }

            return true;
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
        /// <summary>Runs the country's own procedure on a set of national vote shares, or reports that the model does not implement it.</summary>
        public static ElectionRecord Run(CountryId country, int turn, IReadOnlyDictionary<string, double> shares)
        {
            var record = new ElectionRecord { Turn = turn, CountryId = country.ToString() };

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

                default:
                    record.Method = ElectionMethod.NotImplemented;
                    record.NotHeldReason = NotHeldReason(country);
                    return record;
            }
        }
        /// <summary>Plain English, per country, for a screen. Never a shrug.</summary>
        public static string NotHeldReason(CountryId country)
        {
            switch (country)
            {
                case CountryId.Poland:
                    return "Poland allocates d'Hondt separately in 41 districts with no national compensatory tier. " +
                           "This model has no district path for it, and a national allocation would be a different system.";
                case CountryId.France:
                    return "France elects 577 single-member seats in two rounds. This model holds no second round.";
                case CountryId.Italy:
                    return "Italy is a mixed system - 147 uninominal colleges, 245 proportional seats, 8 overseas - " +
                           "and the per-party seat totals on disk are unconfirmed.";
                case CountryId.USA:
                    return "The US House is 435 single-member districts, first past the post. " +
                           "A proportional allocation of the national vote is not what that produces.";
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
            long total = 0;

            foreach (PoliticalParty party in parties)
            {
                double share = shares != null && shares.TryGetValue(party.Abbrev, out double s) ? s : 0.0;
                // Shares to notional votes: only the RATIOS matter to a divisor method, and a large
                // scale keeps a small party's rounding from deciding whether it clears the threshold.
                long v = (long)System.Math.Round(share * 100_000_000.0);
                keys.Add(party.Abbrev);
                votes.Add(v);
                total += v;
                record.Shares[party.Abbrev] = share;
            }

            int[] seats = SeatAllocation.AllocateWithThreshold(votes.ToArray(), total, threshold, chamber, divisor);
            for (int i = 0; i < keys.Count; i++)
            {
                record.Seats[keys[i]] = seats[i];
            }
        }
    }
}
