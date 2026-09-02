using System;
using System.Collections.Generic;

namespace PoliSim.Data
{
    /// <summary>
    /// Continuous Time Migration Phase 0 (Master Sequence step 3): one stat's rolling history at
    /// four resolutions simultaneously (Daily/Weekly/Monthly/Quarterly), each independently capped
    /// at StatHistory.MaxEntries - "daily data alone over 'last 50 entries' would show nothing
    /// meaningful for GDP-scale trends" once Phases 1-5 give stats genuine day-to-day variation,
    /// per the Master Roadmap's own Phase 0 spec. Each resolution only accepts a new point once at
    /// least that resolution's own period has elapsed since its last one - Append can safely be
    /// called far more often than any single resolution actually needs a new entry (in Phase 0
    /// specifically, once per 121-day turn, since the underlying simulation doesn't vary daily yet -
    /// every resolution ends up holding the SAME one-point-per-turn data, since 121 days already
    /// exceeds a day/week/month/quarter's own period; this is an honest reflection of Phase 0's own
    /// stated scope, not a bug - Phases 1-5 are what will make Daily/Weekly/Monthly genuinely diverge
    /// from Quarterly by having the underlying value actually change between turn boundaries).
    /// </summary>
    [Serializable]
    public class MultiResolutionSeries
    {
        public readonly List<float> Daily = new List<float>();
        public readonly List<float> Weekly = new List<float>();
        public readonly List<float> Monthly = new List<float>();

        /// <summary>91 days, not exactly 121 (a turn) or 90 - the real calendar length of a quarter, so this resolution reads as "genuinely quarterly" once Phases 1-5 add sub-turn variation, not just "another name for a turn."</summary>
        public readonly List<float> Quarterly = new List<float>();

        // ⚠ SAVE/LOAD (item 8, hazard 4): these four are the append CADENCE, and they must survive a
        // round trip - omitted, every resolution accepts a duplicate point on the first post-load
        // day and the buckets silently drift one entry apart. [JsonProperty] because Json.NET skips
        // private fields by default; the readonly public lists above need no attribute (Json.NET
        // populates them in place, into the empty defaults the initializers guarantee).
        [Newtonsoft.Json.JsonProperty] private DateTime? _lastDailyDate;
        [Newtonsoft.Json.JsonProperty] private DateTime? _lastWeeklyDate;
        [Newtonsoft.Json.JsonProperty] private DateTime? _lastMonthlyDate;
        [Newtonsoft.Json.JsonProperty] private DateTime? _lastQuarterlyDate;

        /// <summary>
        /// C-C4 (P-G4): the date the LAST quarterly point was appended on — the anchor a caller needs to
        /// place a dated event onto this series, the cadence being fixed at 91 days by `Append` below.
        ///
        /// ⚠ **Exposed rather than left to be re-derived.** A caller guessing the anchor from
        /// `currentDate` would be right only on the exact day a point was appended and progressively
        /// wrong for the other 90, so an enactment marker would drift along the axis as the quarter
        /// advanced. <see cref="QuarterlyPeriodDays"/> goes with it so no caller hard-codes a cadence
        /// this class could later change.
        /// </summary>
        public DateTime? LastQuarterlyDate => _lastQuarterlyDate;

        /// <summary>C-C4: the quarterly cadence, so no caller hard-codes it. See <see cref="LastQuarterlyDate"/>.</summary>
        public const int QuarterlyPeriodDays = 91;

        /// <summary>Offers this turn's value to all four resolutions - each one only actually accepts it (and evicts its own oldest entry past StatHistory.MaxEntries) if at least that resolution's own period has elapsed since its last accepted point.</summary>
        public void Append(DateTime date, float value)
        {
            AppendIfDue(Daily, ref _lastDailyDate, date, value, periodDays: 1);
            AppendIfDue(Weekly, ref _lastWeeklyDate, date, value, periodDays: 7);
            AppendIfDue(Monthly, ref _lastMonthlyDate, date, value, periodDays: 30);
            AppendIfDue(Quarterly, ref _lastQuarterlyDate, date, value, periodDays: 91);
        }

        private static void AppendIfDue(List<float> buffer, ref DateTime? lastAppendDate, DateTime date, float value, int periodDays)
        {
            if (lastAppendDate.HasValue && (date - lastAppendDate.Value).Days < periodDays)
            {
                return;
            }

            buffer.Add(value);
            if (buffer.Count > StatHistory.MaxEntries)
            {
                buffer.RemoveAt(0);
            }
            lastAppendDate = date;
        }
    }

    /// <summary>
    /// Rolling numeric history of a country's key tracked stats - a UI-facing convenience so a
    /// graph can show a trend without re-deriving it from the turn-activity text log, which
    /// is a formatted string per turn, not raw numbers. Appended once per turn by
    /// SimulationManager.AdvanceTurn, for every country (not just the player's), after that turn's
    /// state is fully settled - never touched by PreviewTurn's throwaway clone, so a live slider drag
    /// can never leak a phantom data point into the real history.
    ///
    /// Political Systems Overhaul Part C ("last N changes" pagination): MaxEntries raised from 50 to
    /// 250 (5 pages of GraphRenderer's own 50-turn display window) so pagination has real older data
    /// to page back into, not just blank pages past the first - a bounded, still-capped increase
    /// (still a fixed-size rolling window, just a bigger one), not new tracked data or simulation
    /// logic. Continuous Time Migration Phase 0: each stat is now a MultiResolutionSeries (Daily/
    /// Weekly/Monthly/Quarterly) instead of a single flat list - see that class's own doc comment for
    /// why Phase 0 itself can't yet make the four resolutions genuinely diverge. Every existing graph
    /// call site in GameController reads .Quarterly specifically, matching this project's own
    /// established one-point-per-turn graph cadence exactly (a quarter's 91-day period is shorter
    /// than a 121-day turn, so - like every other resolution here in Phase 0 - it still only ever
    /// accepts one point per turn) - no visual change to any existing graph from this migration.
    /// </summary>
    [Serializable]
    public class StatHistory
    {
        public const int MaxEntries = 250;

        public readonly MultiResolutionSeries Gdp = new MultiResolutionSeries();
        public readonly MultiResolutionSeries Unemployment = new MultiResolutionSeries();
        public readonly MultiResolutionSeries Inflation = new MultiResolutionSeries();
        public readonly MultiResolutionSeries ApprovalRating = new MultiResolutionSeries();
        public readonly MultiResolutionSeries DebtToGdpRatio = new MultiResolutionSeries();
        public readonly MultiResolutionSeries PovertyRate = new MultiResolutionSeries();
        public readonly MultiResolutionSeries InterestRate = new MultiResolutionSeries();

        // Added for Phase 4 of the UI revamp's per-tab graph rollout (Trade/Labor Market/Crime &
        // Justice tabs) - all four already exist as EconomyState fields computed every turn by the
        // real simulation, so recording them here is purely additive bookkeeping, not new logic.
        public readonly MultiResolutionSeries TradeBalance = new MultiResolutionSeries();
        public readonly MultiResolutionSeries LaborForceParticipationRate = new MultiResolutionSeries();
        public readonly MultiResolutionSeries CrimeIndex = new MultiResolutionSeries();
        public readonly MultiResolutionSeries PrisonPopulationRate = new MultiResolutionSeries();

        // Round 3 item 3: two more Crime & Justice tab stats, same "already an EconomyState field,
        // purely additive bookkeeping" reasoning as the four above.
        public readonly MultiResolutionSeries OrganizedCrimeIndex = new MultiResolutionSeries();
        public readonly MultiResolutionSeries CorruptionIndex = new MultiResolutionSeries();

        // ROUND 4 BATCH 1 (C3): the two new inputs-only stats. Daily-native from birth - unlike
        // every series above, these never had a turn-cadence era, so their Daily/Weekly/Monthly
        // buckets genuinely diverge from Quarterly on day one (the equivalence tool asserts this
        // rather than assuming it).
        public readonly MultiResolutionSeries YouthUnemployment = new MultiResolutionSeries();
        public readonly MultiResolutionSeries LifeExpectancy = new MultiResolutionSeries();

        // ROUND 4 BATCH 2 (C2): same daily-native-from-birth reasoning as the batch-1 pair above.
        public readonly MultiResolutionSeries Gini = new MultiResolutionSeries();
        public readonly MultiResolutionSeries RealWageIndex = new MultiResolutionSeries();

        // ROUND 4 BATCH 3 (C1): the housing three. The USA's HousingOverburden series records a
        // flat untracked 0 - honest (that IS its state), cheap, and simpler than a per-country
        // series set.
        public readonly MultiResolutionSeries HousingOverburden = new MultiResolutionSeries();
        public readonly MultiResolutionSeries Homeownership = new MultiResolutionSeries();
        public readonly MultiResolutionSeries HousePriceIndex = new MultiResolutionSeries();

        // ROUND 4 BATCH R4-5 (C5): productivity - the arc's last series.
        public readonly MultiResolutionSeries Productivity = new MultiResolutionSeries();

        /// <summary>P2-0.4 (2026-09-02): the budget balance PER CLOSED FISCAL PERIOD - the series the Domestic
        /// sheet draws and the Budget screen reads. One point per close (SimulationManager writes it beside the
        /// fiscal report), with its date in the parallel list. The cumulative accumulator (EconomyState.Budget)
        /// is not a display figure; debt carries the cumulative on its own row.</summary>
        public readonly List<float> BudgetBalanceAnnual = new List<float>();
        public readonly List<DateTime> BudgetBalanceAnnualDates = new List<DateTime>();
        /// <summary>P2-3.3 (2026-09-02): the compass trail - the chamber's seat-weighted CHES mean at every turn
        /// close (CompassPositions.RecordTrailPoint, written beside the fiscal report), three parallel lists so
        /// the pair persists without a Vector2 (whose self-referential properties the serializer trips on). The
        /// compass draws the list as it is stored; a diagnostic holds the drawn trail to it.</summary>
        public readonly List<float> CompassTrailLrEcon = new List<float>();
        public readonly List<float> CompassTrailGaltan = new List<float>();
        public readonly List<DateTime> CompassTrailDates = new List<DateTime>();

        /// <summary>
        /// Appends this turn's already-settled values. <paramref name="date"/> is the in-game
        /// calendar date the turn resolved on (Continuous Time Migration Phase 0 - see
        /// SimulationManager.CurrentDate), needed so each MultiResolutionSeries can decide which of
        /// its four resolutions this point is actually due for. <paramref name="interestRate"/> is
        /// passed separately (not read from <paramref name="state"/>) since the rate lives on the
        /// country's CurrencyZone, not EconomyState.
        /// </summary>
        public void Append(DateTime date, EconomyState state, float interestRate)
        {
            Gdp.Append(date, state.GDP);
            Unemployment.Append(date, state.Unemployment);
            Inflation.Append(date, state.Inflation);
            ApprovalRating.Append(date, state.ApprovalRating);
            DebtToGdpRatio.Append(date, state.DebtToGdpRatio);
            PovertyRate.Append(date, state.PovertyRate);
            InterestRate.Append(date, interestRate);
            TradeBalance.Append(date, state.TradeBalance);
            LaborForceParticipationRate.Append(date, state.LaborForceParticipationRate);
            CrimeIndex.Append(date, state.CrimeIndex);
            PrisonPopulationRate.Append(date, state.PrisonPopulationRate);
            OrganizedCrimeIndex.Append(date, state.OrganizedCrimeIndex);
            CorruptionIndex.Append(date, state.CorruptionIndex);
            YouthUnemployment.Append(date, state.YouthUnemployment);
            LifeExpectancy.Append(date, state.LifeExpectancy);
            Gini.Append(date, state.Gini);
            RealWageIndex.Append(date, state.RealWageIndex);
            HousingOverburden.Append(date, state.HousingOverburden);
            Homeownership.Append(date, state.Homeownership);
            HousePriceIndex.Append(date, state.HousePriceIndex);
            Productivity.Append(date, state.Productivity);
        }
    }
}
