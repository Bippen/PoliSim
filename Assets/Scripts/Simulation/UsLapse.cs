using System;
using PoliSim.Data;
using UnityEngine;

namespace PoliSim.Simulation
{
    /// <summary>
    /// PS-6 US-22b (Elias's ruling I13 (1), 2026-10-18, `docs/RULINGS_2026-10-18.md`): "A lapse defers spending; it does not cancel it. The lapsed acts'
    /// non-excepted outlays stop, and are paid in full when funding returns (back pay, 31 U.S.C. 1341(c)). Mandatory spending runs on. Take the size and
    /// the output effect from CBO's 29 Oct 2025 estimate (publication 61823 ...) and the delayed outlays you found." Option (B), made up afterwards, with
    /// 61823's output effect added. THE LAPSE IN THE BOOK AND IN OUTPUT - its own BASELINE family wherever a lapse is reached (the LAPSE family), and nothing
    /// anywhere else: every field it reads is zero, or its day unset, in every country and every world that never lapses, and nothing is written there.
    ///
    /// <para><b>The source</b> (`ElectionsData/usa/raw/lapse/`, its fetch log): CBO's letter of 29 October 2025 to the House Budget Committee, "A Quantitative
    /// Analysis of the Effects of the Government Shutdown on the Economy Under Three Scenarios", read in its six-week scenario, which ends on 12 November 2025 -
    /// the record's last day (P.L. 119-37). Its figures are in 2025 dollars and per quarter "at a (nonannualized) quarterly rate".</para>
    ///
    /// <para><b>THE BOOK, DECLARED (nominal with nominal).</b> The delayed outlays become a share of a day's NOMINAL output: CBO's output per day is its own
    /// quarterly level - the four-week scenario's Q4 loss in dollars over the same loss "or 0.24 percent" - over the quarter's days (Q4: October to December),
    /// and the six-week figure is spread over the scenario's days. On each lapsed day that share of `EconomyState.NominalGdp` is stopped from the day's
    /// discretionary outlay (`FiscalPeriod.PlannedGovernmentSpending`, never more than it), held, and paid in full in the first funded day's flows. CBO's output
    /// in 2025 dollars, the letter's own year's prices, is the denominator for its outlays in that year's dollars. The stop per day is constant, so a lapse
    /// longer than CBO's longest scenario (eight weeks) EXTRAPOLATES it past the estimate (DECLARED, the review's M1).</para>
    ///
    /// <para><b>THE OUTPUT, DECLARED (real with real; the review's H1).</b> The game reads quarterly output by POINT SAMPLES - each period's closing day
    /// (`PublicationSystem.PublishDueFigures`), the history's 91-day samples, the growth `DerivedStats` takes from them - so the effect is shaped to the
    /// quarter, as CBO's Table 2 states it: a LEVEL held over each whole calendar quarter, the six-week column's Q4 2025 to Q3 2026 effects
    /// (<see cref="QuarterEffectsBillions"/>) over CBO's quarterly output, each scaled by the lapse's days over the scenario's. During the lapse the level is
    /// the first quarter's, at no less than the scenario's size (its length is not known until it ends); the quarter in which it ends holds the first quarter's
    /// level at its own length, and the three quarters after hold the rest. So any point sample reads CBO's level for its quarter, the annualized growth CBO
    /// prints follows from the four-times rule (the letter's own note), and the part never recovered falls out of the table's sum. It is an OVERLAY on the
    /// level the macro block's identity produces - removed before the next day's identity reads it, so the identity's dynamics never carry it and CBO's effect
    /// is not counted twice through the planned G the identity reads; the turn boundary's year-long anchors read the identity's level too
    /// (<see cref="LiftOverlay"/>, the review's M3). A new lapse inside an old one's quarters drops the old one's remaining quarters. A lapse that runs past a
    /// quarter's end holds the first quarter's level throughout, and its rebound counts from the quarter it ends in (for the record, inside one quarter, the
    /// two readings are the same).</para>
    ///
    /// <para><b>The mapping's residues, DECLARED (the review's L2).</b> (i) CBO's quarterly output comes from a rounded pair ($18 bn, 0.24 %), good to about
    /// 3 %; (ii) CBO's Q4 figure is net of the rebound inside the quarter, and it is held here as the quarter's level whole; (iii) the table's four quarters
    /// sum to the $10 bn the overlay leaves unrecovered, where the letter's text says $11 bn by the end of 2026 (its fifth quarter is "between -$500 million
    /// and zero", and its figures are rounded); (iv) the scaling by the lapse's days is linear, while CBO's per-day effects grow with the length (its
    /// eight-week Q4 loss is more than four thirds of its six-week loss); (v) in 2025 dollars real and nominal output are taken as one, within the year.
    /// The hold rule's own (the second review): (vi) a short lapse still open on a quarter's closing day reads the six-week floor at that close; (vii) within
    /// the quarter a lapse ends in, the quarter's integral is overstated - the level is held over the whole quarter from the lapse's first day, about +23 %
    /// for a 21-day lapse mid-quarter; (viii) a lapse running past a quarter's close counts its loss in both quarters.</para>
    ///
    /// <para><b>The timing.</b> The economy's day runs before the funding tick (`SimulationManager.AdvanceCountryDayTick`), so the tick ARMS the next day's
    /// flows (`Country.LapseFlowDay`): the lapse's flows run from the day after it opens through the day funding returns, one day fewer than its counted
    /// days - for the record's lapse (1 Oct to 12 Nov), CBO's six weeks. THE FORKS (the review's M4, D-45's class): since FORK-1 (Elias's ruling J8) the impact
    /// ledger's forks run the player's day tick, the funding tick among it, as the played world does (`ShadowBaseline`'s <c>dayTick</c>), so a lapse in the
    /// played world is in every except-world too and lands on no family. The no-policy world from the game's start runs it too (AF-12 with the s871 review's H1); only the seeded world, with no player, runs none.</para>
    /// </summary>
    public static class UsLapse
    {
        /// <summary>[SOURCED] CBO 61823, Table 1, the six-week scenario: "Total delayed spending", billions of dollars - delayed compensation for furloughed and
        /// excepted employees, goods and services, and SNAP.</summary>
        public const float DelayedOutlaysBillions = 54f;

        /// <summary>[SOURCED] CBO 61823, Table 2, the six-week scenario: "Effect on real GDP (billions of 2025 dollars)", 2025 Q4, 2026 Q1, Q2 and Q3 - a level
        /// for each quarter at a nonannualized quarterly rate (its 2026 Q4, between -$500 million and zero, is not read).</summary>
        public static readonly float[] QuarterEffectsBillions = { -28f, 13f, 4f, 1f };

        /// <summary>[SOURCED] CBO 61823: the six-week scenario's length, "one that ends after six weeks (on November 12)", in days.</summary>
        public const int ScenarioDays = 6 * 7;

        /// <summary>[SOURCED] CBO 61823, the four-week scenario's worked example: "real GDP in the fourth quarter of 2025 would be $18 billion (in 2025 dollars),
        /// or 0.24 percent, lower" - the pair that gives CBO's quarterly output.</summary>
        public const float LevelPairBillions = 18f;
        /// <summary>[SOURCED] the same sentence's percent.</summary>
        public const float LevelPairPercent = 0.24f;

        /// <summary>DERIVED: CBO's quarterly real output, billions of 2025 dollars at a nonannualized quarterly rate - the pair.</summary>
        public static readonly double QuarterOutputBillions = LevelPairBillions / (LevelPairPercent / 100.0);

        /// <summary>DERIVED: the days of 2025 Q4, the quarter CBO's Q4 level is measured over.</summary>
        public static readonly int QuarterDays = (new DateTime(2026, 1, 1) - new DateTime(2025, 10, 1)).Days;

        /// <summary>DERIVED: CBO's output per day, billions of 2025 dollars - its quarterly level over the quarter's days.</summary>
        public static readonly double DayOutputBillions = QuarterOutputBillions / QuarterDays;

        /// <summary>DERIVED: the outlays stopped on a lapsed day, as a share of the day's nominal output.</summary>
        public static readonly float DeferredShare = (float)(DelayedOutlaysBillions / ScenarioDays / DayOutputBillions);

        /// <summary>DERIVED: the level of real output in the k-th quarter of the profile (0 the quarter the lapse ends in), as a share LOST (positive lower,
        /// negative higher), for a lapse of the scenario's length - CBO's Table 2 over its quarterly output.</summary>
        public static float QuarterLevelLost(int k) => (float)(-QuarterEffectsBillions[k] / QuarterOutputBillions);

        /// <summary>Whether <paramref name="country"/>'s flows on <paramref name="today"/> run lapsed - the day the funding tick armed.</summary>
        public static bool Lapsed(Country country, DateTime today) => country.LapseFlowDay == today;

        /// <summary>
        /// THE BOOK: the change to the day's discretionary outlay - on a lapsed day minus the stopped outlays (<see cref="DeferredShare"/> of the day's nominal
        /// output, never more than the day's discretionary outlay <paramref name="discretionaryToday"/>), held as owed; on the first day after, plus everything
        /// owed, paid in full; otherwise 0, and nothing written. Mandatory spending, benefits, welfare and interest are not touched.
        /// </summary>
        public static float OutlayToday(Country country, DateTime today, float discretionaryToday)
        {
            if (Lapsed(country, today))
            {
                float stopped = Mathf.Min(DeferredShare * country.State.NominalGdp / SimulationManager.DaysPerTurn, Mathf.Max(0f, discretionaryToday));
                country.LapseDeferredOutlays += stopped;
                return -stopped;
            }
            if (country.LapseDeferredOutlays <= 0f) { return 0f; }
            float owed = country.LapseDeferredOutlays;
            country.LapseDeferredOutlays = 0f;
            return owed;
        }

        /// <summary>THE OUTPUT OVERLAY, removed: the level the identity produced yesterday, before today's identity reads it. Nothing where none is applied.</summary>
        public static void RestoreOutput(Country country)
        {
            if (country.LapseOutputWedge == 0f) { return; }
            country.State.GDP += country.LapseOutputWedge;
            country.LapseOutputWedge = 0f;
        }

        /// <summary>The review's M3: the overlay LIFTED for a reading that must see the identity's level (the turn boundary's re-plan), and
        /// <see cref="LowerOverlay"/> puts it back - the wedge kept, both exact (the overlay is the difference of two floats within a factor of two). Nothing
        /// where none is applied.</summary>
        public static void LiftOverlay(Country country)
        {
            if (country.LapseOutputWedge == 0f) { return; }
            country.State.GDP += country.LapseOutputWedge;
        }

        /// <summary>The overlay put back after <see cref="LiftOverlay"/>.</summary>
        public static void LowerOverlay(Country country)
        {
            if (country.LapseOutputWedge == 0f) { return; }
            country.State.GDP -= country.LapseOutputWedge;
        }

        /// <summary>The first day of the calendar quarter <paramref name="day"/> falls in.</summary>
        public static DateTime QuarterStart(DateTime day) => new DateTime(day.Year, (day.Month - 1) / 3 * 3 + 1, 1);

        /// <summary>Whole calendar quarters from the quarter starting <paramref name="quarterStart"/> to the one <paramref name="day"/> falls in.</summary>
        public static int QuartersFrom(DateTime quarterStart, DateTime day) => (day.Year * 4 + (day.Month - 1) / 3) - (quarterStart.Year * 4 + (quarterStart.Month - 1) / 3);

        /// <summary>
        /// THE OUTPUT OVERLAY, applied after the identity and Okun's read of the day's growth, shaped to the quarter (the review's H1): on a lapsed day, the
        /// lapse's days counted and the level lower by the first quarter's share at the larger of its days so far and the scenario's; after it, the quarter it
        /// ended in held at the first quarter's share and the next three at theirs, each scaled by its days over the scenario's; then nothing, and the profile
        /// cleared. Nothing where nothing is lapsed and no profile runs.
        /// </summary>
        public static void ApplyOutput(Country country, DateTime today)
        {
            EconomyState state = country.State;
            float lost;
            if (Lapsed(country, today))
            {
                if (country.LapseProfileQuarter != DateTime.MinValue) { country.LapseProfileQuarter = DateTime.MinValue; country.LapseOutputDays = 0; }   // a new lapse inside an old one's quarters: the old one's rest dropped (DECLARED)
                country.LapseOutputDays++;
                lost = QuarterLevelLost(0) * Mathf.Max(country.LapseOutputDays, ScenarioDays) / ScenarioDays;
            }
            else if (country.LapseOutputDays > 0)
            {
                if (country.LapseProfileQuarter == DateTime.MinValue) { country.LapseProfileQuarter = QuarterStart(today.AddDays(-1)); }   // the quarter of the lapse's last flow day
                int k = QuartersFrom(country.LapseProfileQuarter, today);
                if (k >= QuarterEffectsBillions.Length) { country.LapseOutputDays = 0; country.LapseProfileQuarter = DateTime.MinValue; return; }
                lost = QuarterLevelLost(k) * country.LapseOutputDays / ScenarioDays;
            }
            else { return; }
            float before = state.GDP;
            state.GDP = Mathf.Max(MacroSystem.MinGdp, before - lost * before);
            country.LapseOutputWedge = before - state.GDP;
        }

        // ---------------------------------------------------------------------------------------------------------------------------------------------
        // PS-6 US-22c (Elias's ruling J7 (iv), 2026-10-19: "Furloughs reach unemployment as BLS counts them (unemployed on temporary layoff, in the household
        // survey), with the headcount from CBO"). BLS's treatment (`ElectionsData/usa/raw/furlough/`, the box notes of the October 2013 and January 2019
        // Employment Situation): "Workers who indicate[d] that they were not working during the entire survey reference week and expected to be recalled to
        // their jobs should be classified ... as unemployed on temporary layoff"; "the reference period is generally the calendar week that includes the 12th
        // of the month". So a month's unemployment counts the furloughed where a lapse covered that month's reference week whole, and only then - the PUBLISHED
        // rate (`PublicationSystem`'s closing value for the month), which is what the household survey is; the macro block's unemployment, which Okun, the
        // Phillips curve and the benefits read, is not touched (the furloughed are paid in full when funding returns, 31 U.S.C. 1341(c)). READINGS, DECLARED:
        // (i) the reference week is always the Sunday-to-Saturday week holding the 12th (BLS's "generally"); (ii) the lapse's last day, the day its law is
        // enacted, is one of its days (its count, `UsBudgetProcedure.LapseDays`); (iii) every furloughed worker is counted as BLS's definition classifies him -
        // both box notes report some recorded instead as employed but absent, a misclassification BLS leaves as recorded, which the game does not reproduce;
        // (iv) the headcount is CBO's average for the 2025 lapse, the only one it estimates, held for every lapse; (v) the labour force is the model's
        // (`PotentialOutput.LabourForce`, ages 15 and over, where the household survey's is 16 and over).
        // ---------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>[SOURCED] CBO 61823 (`ElectionsData/usa/debt_limit.md`, section 10, the letter's own words): "650,000 employees will be furloughed, on average"
        /// - in millions of persons.</summary>
        public const float FurloughedMillions = 0.65f;

        /// <summary>The household survey's reference week for the month <paramref name="day"/> falls in: its Sunday (the calendar week holding the 12th).</summary>
        public static DateTime ReferenceWeekStart(DateTime day)
        {
            var twelfth = new DateTime(day.Year, day.Month, 12);
            return twelfth.AddDays(-(int)twelfth.DayOfWeek);
        }

        /// <summary>The funding tick's note, each lapsed day and the day a lapse ends: where the lapse has covered this month's reference week whole (it began on
        /// or before the week's Sunday and <paramref name="today"/> is the week's Saturday or later), the month is marked (`Country.FurloughCountedMonth`).
        /// Nothing where no lapse runs.</summary>
        public static void NoteReferenceWeek(Country country, DateTime today)
        {
            if (country.LapseSince == DateTime.MinValue) { return; }
            DateTime sunday = ReferenceWeekStart(today);
            if (country.LapseSince.Date > sunday || today.Date < sunday.AddDays(6)) { return; }
            var month = new DateTime(today.Year, today.Month, 1);
            if (country.FurloughCountedMonth != month) { country.FurloughCountedMonth = month; }
        }

        /// <summary>DERIVED: the furloughed as points of the unemployment rate - CBO's headcount over the model's labour force (millions, as `PotentialOutput.LabourForce`).</summary>
        public static float FurloughPoints(Country country) => 100f * FurloughedMillions / Mathf.Max(PotentialOutput.LabourForce(country), 1f);

        /// <summary>The points a month's published unemployment counts for the furloughed: <see cref="FurloughPoints"/> for the month marked, 0 for every other -
        /// so a world that never lapses records exactly what it did.</summary>
        public static float CountedPoints(Country country, DateTime periodStart) => country.FurloughCountedMonth == periodStart ? FurloughPoints(country) : 0f;

        /// <summary>The lapse's economics in a page's words.</summary>
        public const string StopWords = "NON-EXCEPTED OUTLAYS STOP AND ARE PAID IN FULL WHEN FUNDING RETURNS · OUTPUT FALLS AND MOSTLY RECOVERS (CBO, 29 OCT 2025)";
    }
}
