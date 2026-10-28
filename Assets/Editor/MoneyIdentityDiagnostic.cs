using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text;
using PoliSim.Data;
using PoliSim.Simulation;
using UnityEngine;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// MF q32/q33 (ruled 2026-10-18 q32, q33; K141-K144): **NO MONEY VANISHES**, held on stress runs that drive every place the old clamps used to bind -
    /// sources equal uses, day by day:
    /// <list type="bullet">
    /// <item><b>(a) the convex spread</b> (Pamies, Carnot and Patarau 2021, Table 1 regression (2)): zero at and below the quadratic's minimum, convex above
    /// it, and uncapped - past the retired cap of 5 points at a debt ratio the old premium capped.</item>
    /// <item><b>(b) the fund's ceiling refuses at booking</b>: Sweden's fund opened above its ceiling with the contribution at the dial's top - what stands above
    /// it is paid out (the overflow) and every later contribution that would pass it is refused; each day the fund's assets move by exactly what the period
    /// booked (contributions, returns landed, draws and the overflow), and the refused and the overflowed sums are both non-zero (exercised, never vacuous).</item>
    /// <item><b>(c) the programme</b> (K142, K144): Italy opened at 290 % of GDP - CCC at the first review, the programme the same day, its loans at the base
    /// rate without the premium, its yearly reviews, its end after three years.</item>
    /// <item><b>(d) the default</b> (K142, K143, K212): first, one day with the USA opened at 290 % of GDP, its sensitivity as seeded - the trigger reads the rating without the
    /// reserve discount, so it fires where the undiscounted rating reaches CCC while the standing (discounted) rating does not. Then six years end to end (N2): the debt raised
    /// to 290 % on the first 1 July a year after the start, the default at the 1 October review with a year of closings behind it; the 37 % haircut booked as a
    /// dated ledger event, the exclusion's length, the cut while excluded, and the output cost: 2.6 % of the day's GDP landed over the year.</item>
    /// <item><b>(e) the identity</b> on every day of (b)-(d): the debt plus the running budget plus the haircut written off stands still (the deficit is the
    /// debt's whole change), no runaway guard absorbs anything, and every closed debt ledger reports no clamp day and no clamp loss.</item>
    /// <item><b>(d) also, the q32 review's H1 and M1</b>: on the default's day the haircut sits on the debt ledger as a stock write-off that the year's one-offs and the
    /// fiscal vote's open ratio leave out; the year closing after it books no haircut line and its balance is the flow plus the other one-offs; the rating's
    /// settled deficit adds a haircut inside its year back (in the run where a review reaches it, and as arithmetic at the run's last review); and every
    /// crisis the run opens opens on a review day - an exclusion ending between reviews leaves the market open.</item>
    /// <item><b>(f) the draw booked whole</b> (K213): two Swedish worlds a day apart only in the fund (one holding its assets, one emptied), the period's
    /// fiscal-reaction multiplier planted at 0.5 in both - the budgets differ by exactly the draw that left the fund, not by half of it.</item>
    /// </list>
    /// Off-path fund writes (a dissolution, an emergency drawdown bill) are not run here - the stress worlds pass no bill.
    /// Run: <c>-executeMethod PoliSim.EditorTools.MoneyIdentityDiagnostic.Run</c>; a passing run prints <c>MONEY IDENTITY: PASS</c>.
    /// </summary>
    public static class MoneyIdentityDiagnostic
    {
        private static int _failures;
        private static StringBuilder _sb;
        private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;
        private static string F(string format, params object[] args) => string.Format(Inv, format, args);

        private static readonly FieldInfo FiscalPeriods = typeof(SimulationManager).GetField("_fiscalPeriods", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly MethodInfo IssuanceRate = typeof(SimulationManager).GetMethod("GetDebtIssuanceRate", BindingFlags.Instance | BindingFlags.NonPublic);

        public static void Run()
        {
            CheckExit.ArmLogFold();
            _failures = 0;
            _sb = new StringBuilder("MONEY IDENTITY: MF q32/q33 - the convex spread, the fund's ceiling at booking, the programme, the default, sources = uses (seed 777)\n");
            try
            {
                ConvexSpread();
                FundCeiling();
                Programme();
                DefaultTriggerDayOne();
                Default();
                DrawBookedWhole();
            }
            catch (Exception e) { Check(false, "the run threw " + e.GetType().Name + ": " + e.Message + "\n" + e.StackTrace); }
            Debug.Log(_sb.ToString());
            Debug.Log(_failures == 0
                ? "MONEY IDENTITY: PASS - the spread is convex and uncapped, the fund's ceiling refuses at booking and pays out its overflow, the programme and the default fire at CCC with their sourced terms, and on every stressed day the debt's change is the deficit (plus the booked haircut), no guard absorbs anything and no ledger reports a clamp."
                : F("MONEY IDENTITY: FAIL - {0} check(s) failed.", _failures));
            CheckExit.Finish(_failures == 0 ? 0 : 1);
        }

        private static void Check(bool ok, string what)
        {
            _sb.Append(ok ? "      ok   " : "      FAIL ").Append(what).Append('\n');
            if (!ok) { _failures++; Debug.LogError("MONEY IDENTITY: FAIL - " + what); }
        }

        // ============================================================ (a) the convex spread
        private static void ConvexSpread()
        {
            _sb.Append("    a. the convex spread (Pamies, Carnot and Patarau 2021, Table 1 regression (2))\n");
            float floor = 0.0281f / (2f * 0.000300f);
            float at40 = SimulationManager.ConvexSpreadAt(40f), atFloor = SimulationManager.ConvexSpreadAt(floor);
            float at100 = SimulationManager.ConvexSpreadAt(100f), at150 = SimulationManager.ConvexSpreadAt(150f), at200 = SimulationManager.ConvexSpreadAt(200f), at300 = SimulationManager.ConvexSpreadAt(300f);
            Check(at40 == 0f && atFloor < 1e-6f, F("zero at and below the quadratic's minimum ({0:0.0} % of GDP): {1} at 40 %, {2:0.000000} at the minimum", floor, at40, atFloor));
            Check(at150 - at100 < at200 - at150 && at100 > 0f, F("convex: 100 % {0:0.000} pts, 150 % {1:0.000}, 200 % {2:0.000} (the rise from 150 to 200 exceeds the rise from 100 to 150)", at100, at150, at200));
            // the paper's own marginal reading: the slope crosses the benchmark's 0.0291 at about 95.3 % (its "higher than ... beyond 95% of GDP"); asserted here is the derivative alone
            float slope95 = (SimulationManager.ConvexSpreadAt(95.5f) - SimulationManager.ConvexSpreadAt(94.5f)) / 1f;
            Check(Mathf.Abs(slope95 - (-0.0281f + 2f * 0.000300f * 95f)) < 1e-4f, F("the slope at 95 % is the quadratic's derivative -0.0281 + 0.0006 x 95 = {0:0.0000} pts per point (read {1:0.0000})", -0.0281f + 2f * 0.000300f * 95f, slope95));
            Check(at300 > 5f, F("uncapped: {0:0.00} pts at 300 % of GDP, past the retired cap of 5", at300));
        }

        // ============================================================ the world and the day loop the stress runs share
        private sealed class StressRun
        {
            public World World;
            public SimulationManager Sim;
            public GameObject Go;
            public IDisposable Epoch;
            public Dictionary<CountryId, PolicyDecision> Decisions;
        }

        private static StressRun Open(CountryId player, string name)
        {
            SimulationRandom.Seed(777);
            EnergyMarket.ResetCalibration();
            var run = new StressRun { Epoch = SimulationManager.EpochScope() };
            PoliSim.Elections.WorldClock.ApplyStart(player);
            run.World = WorldFactory.CreateDefault();
            run.Go = new GameObject(name);
            run.Sim = run.Go.AddComponent<SimulationManager>();
            run.Sim.SetWorld(run.World);
            run.Sim.PlayerCountryId = player;
            run.Decisions = new Dictionary<CountryId, PolicyDecision>();
            foreach (Country k in run.World.Countries) { run.Decisions[k.Id] = PolicyDecision.None(); }
            return run;
        }

        private static void Close(StressRun run)
        {
            if (run.Go != null) { UnityEngine.Object.DestroyImmediate(run.Go); }
            run.Epoch?.Dispose();
        }

        private struct Evening
        {
            public int Turn;
            public double Assets, Contribution, Returns, Draw;
        }

        /// <summary>Advances <paramref name="days"/> days (and each boundary's turn), asserting the identity (e) on <paramref name="c"/> every day; calls
        /// <paramref name="eachDay"/> after the day. Returns the count of identity breaches (each also failed once, by its first instance).</summary>
        private static int Advance(StressRun run, Country c, int days, Action<int> eachDay)
        {
            int breaches = 0, fundBreaches = 0, ledgerBreaches = 0, ledgersRead = 0;
            DebtAttribution lastLedger = c.FiscalLedgerLastPeriod;
            var periods = FiscalPeriods?.GetValue(run.Sim) as Dictionary<CountryId, SimulationManager.FiscalPeriod>;
            Evening? evening = null;
            string firstBreach = null, firstFundBreach = null, firstLedgerBreach = null;
            for (int day = 1; day <= days; day++)
            {
                double before = (double)c.State.GovernmentDebt + c.State.Budget + c.Crisis.HaircutBillions;
                bool boundary = run.Sim.AdvanceDay();

                // the fund's identity, within a period (the boundary resets the accruals, so a day is compared with the evening before in the same turn)
                if (c.SovereignWealthFund != null && periods != null && periods.TryGetValue(c.Id, out SimulationManager.FiscalPeriod period))
                {
                    var now = new Evening { Turn = run.Sim.CurrentTurn, Assets = c.SovereignWealthFund.TotalAssets, Contribution = period.AccruedSwfContribution, Returns = period.AccruedSwfReturns, Draw = period.AccruedSwfDraw };
                    if (evening.HasValue && evening.Value.Turn == now.Turn)
                    {
                        double expected = evening.Value.Assets + (now.Contribution - evening.Value.Contribution) + (now.Returns - evening.Value.Returns) - (now.Draw - evening.Value.Draw);
                        double tolerance = 1e-3 + 1e-5 * Math.Abs(now.Assets);
                        if (Math.Abs(now.Assets - expected) > tolerance)
                        {
                            fundBreaches++;
                            if (firstFundBreach == null) { firstFundBreach = F("day {0} ({1:yyyy-MM-dd}): the fund holds {2:R}, its booked flows say {3:R}", day, run.Sim.CurrentDate, now.Assets, expected); }
                        }
                    }
                    evening = now;
                }

                if (boundary) { run.Sim.AdvanceTurn(run.Decisions); }
                double after = (double)c.State.GovernmentDebt + c.State.Budget + c.Crisis.HaircutBillions;
                double tol = 1e-3 + 1e-6 * (Math.Abs(c.State.GovernmentDebt) + Math.Abs(c.State.Budget) + c.Crisis.HaircutBillions);
                if (Math.Abs(after - before) > tol)
                {
                    breaches++;
                    if (firstBreach == null) { firstBreach = F("day {0} ({1:yyyy-MM-dd}): debt + budget + haircut moved {2:R}", day, run.Sim.CurrentDate, after - before); }
                }

                // every closed debt ledger: no clamp day, no clamp loss (the ledger's own tolerance)
                if (!ReferenceEquals(c.FiscalLedgerLastPeriod, lastLedger) && c.FiscalLedgerLastPeriod != null)
                {
                    lastLedger = c.FiscalLedgerLastPeriod;
                    ledgersRead++;
                    float ledgerTolerance = Mathf.Max(1e-3f, 1e-6f * Mathf.Max(Mathf.Abs(lastLedger.DebtAtPeriodOpen), Mathf.Abs(lastLedger.DebtAtClose)));
                    if (lastLedger.ClampBoundDays != 0 || Mathf.Abs(lastLedger.ClampLoss) > ledgerTolerance * 10f)
                    {
                        ledgerBreaches++;
                        if (firstLedgerBreach == null) { firstLedgerBreach = F("the period closed {0:yyyy-MM-dd}: {1} clamp day(s), clamp loss {2:R}", lastLedger.PeriodCloseDate, lastLedger.ClampBoundDays, lastLedger.ClampLoss); }
                    }
                }

                eachDay?.Invoke(day);
            }

            Check(breaches == 0, F("{0}: the debt's change is the deficit (plus the booked haircut) on every one of {1} days{2}", c.Id, days, firstBreach == null ? "" : " - " + breaches + " breach(es), first " + firstBreach));
            if (c.SovereignWealthFund != null) { Check(fundBreaches == 0, F("{0}: the fund moves by exactly its booked flows on every day of a period{1}", c.Id, firstFundBreach == null ? "" : " - " + fundBreaches + " breach(es), first " + firstFundBreach)); }
            Check(ledgersRead > 0 && ledgerBreaches == 0, F("{0}: {1} closed debt ledger(s), none with a clamp day or a clamp loss{2}", c.Id, ledgersRead, firstLedgerBreach == null ? "" : " - " + firstLedgerBreach));
            Check(c.Crisis.GuardAbsorbedBillions == 0f, F("{0}: no runaway guard absorbed anything (booked {1:R})", c.Id, c.Crisis.GuardAbsorbedBillions));
            return breaches + fundBreaches + ledgerBreaches;
        }

        // ============================================================ (b) the fund's ceiling refuses at booking
        private static void FundCeiling()
        {
            _sb.Append("    b. Sweden's fund opened above its 300 % ceiling, the contribution at the dial's top (10 % of GDP), three years\n");
            StressRun run = Open(CountryId.Sweden, "MONEYIDENTITY_FUND");
            try
            {
                Country c = run.World.GetCountry(CountryId.Sweden);
                if (c.SovereignWealthFund == null) { Check(false, "Sweden holds a fund - the fund's ceiling is UNVERIFIED, not clean"); return; }
                c.SovereignWealthFund.ContributionRatePercent = 10f;
                c.SovereignWealthFund.TotalAssets = 3.05f * c.State.NominalGdp;
                Advance(run, c, 3 * SimulationManager.DaysPerTurn, null);
                float ratio = c.SovereignWealthFund.TotalAssets / Mathf.Max(1e-6f, c.State.NominalGdp) * 100f;
                Check(c.Crisis.SwfOverflowBillions > 0f, F("the overflow above the ceiling was paid out into the budget: {0:0.###} bn over the run", c.Crisis.SwfOverflowBillions));
                Check(c.Crisis.SwfRefusedBillions > 0f, F("contributions that would pass the ceiling were refused at booking: {0:0.###} bn over the run (the money stayed in the budget)", c.Crisis.SwfRefusedBillions));
                Check(ratio <= 300.01f, F("the fund ends at {0:0.00} % of GDP, at or under its ceiling", ratio));
            }
            finally { Close(run); }
        }

        // ============================================================ (c) the programme (Italy)
        private static void Programme()
        {
            _sb.Append("    c. Italy opened at 290 % of GDP: CCC at the first review, the programme (K142), four years\n");
            StressRun run = Open(CountryId.Italy, "MONEYIDENTITY_PROGRAMME");
            try
            {
                Country c = run.World.GetCountry(CountryId.Italy);
                c.State.GovernmentDebt = 2.9f * c.State.NominalGdp;
                DateTime? entered = null, ended = null;
                bool premiumFree = true, rateRead = false;
                Advance(run, c, 4 * SimulationManager.DaysPerTurn, day =>
                {
                    if (!entered.HasValue && c.Crisis.Phase == CrisisPhase.Programme) { entered = run.Sim.CurrentDate; }
                    if (entered.HasValue && !ended.HasValue && (c.Crisis.Phase == CrisisPhase.None || c.Crisis.StartedOn.Date != entered.Value.Date)) { ended = run.Sim.CurrentDate; }   // a second programme may open the day the first ends
                    if (c.Crisis.Phase == CrisisPhase.Programme && IssuanceRate != null)
                    {
                        float baseRate = c.BaseDebtInterestRateOverride >= 0f ? c.BaseDebtInterestRateOverride : c.CurrencyZone.InterestRate;
                        float issuance = (float)IssuanceRate.Invoke(run.Sim, new object[] { c });
                        rateRead = true;
                        if (Mathf.Abs(issuance - baseRate) > 1e-5f) { premiumFree = false; }
                    }
                });
                Check(entered.HasValue && c.Crisis.Crises >= 1, F("the programme opened on {0} (the first review rated CCC); crises entered {1}", entered.HasValue ? entered.Value.ToString("yyyy-MM-dd", Inv) : "never", c.Crisis.Crises));
                Check(rateRead && premiumFree, "the programme's issuance priced at the base rate, without the market's premium, on every programme day" + (rateRead ? "" : " - NOT READ (GetDebtIssuanceRate not found)"));
                int span = entered.HasValue && ended.HasValue ? (ended.Value - entered.Value).Days : -1;
                Check(span >= 365 * SovereignCrisis.ProgrammeYears - 3 && span <= 366 * SovereignCrisis.ProgrammeYears + 1, F("the programme ran {0} days ({1} years, the Portugal MoU's); it ended on {2}", span, SovereignCrisis.ProgrammeYears, ended.HasValue ? ended.Value.ToString("yyyy-MM-dd", Inv) : "never"));
                _sb.Append(F("      read: entry deficit {0:0.0} % of GDP, {1} target(s) missed, {2:0.###} bn cut in withheld years; the debt at the end {3:0.0} % of GDP\n",
                    c.Crisis.EntryDeficitPercent, c.Crisis.TargetsMissed, c.Crisis.CutBillions, c.State.DebtToGdpRatio));
            }
            finally { Close(run); }
        }

        // ============================================================ (f) the draw booked whole (K213)
        private static void DrawBookedWhole()
        {
            _sb.Append("    f. the fund's draw booked whole (K213): Sweden, one day, the multiplier planted at 0.5, the fund held against the fund emptied\n");
            double DayOf(bool emptied, out double drawn)
            {
                StressRun run = Open(CountryId.Sweden, emptied ? "MONEYIDENTITY_DRAW_EMPTY" : "MONEYIDENTITY_DRAW_HELD");
                try
                {
                    Country c = run.World.GetCountry(CountryId.Sweden);
                    drawn = 0.0;
                    if (c.SovereignWealthFund == null) { return double.NaN; }
                    c.SovereignWealthFund.ContributionRatePercent = 0f;
                    var periods = FiscalPeriods?.GetValue(run.Sim) as Dictionary<CountryId, SimulationManager.FiscalPeriod>;
                    // the first day seeds the period; the planted day is the second
                    if (run.Sim.AdvanceDay()) { run.Sim.AdvanceTurn(run.Decisions); }
                    if (periods == null || !periods.TryGetValue(c.Id, out SimulationManager.FiscalPeriod period)) { return double.NaN; }
                    period.PlannedFiscalReactionMultiplier = 0.5f;
                    period.PlannedSwfReturn = 0f;   // no return accrues, so the fund's fall is the draw alone
                    if (emptied) { c.SovereignWealthFund.TotalAssets = 0f; }   // the worlds were identical through day 1; only the fund differs on the planted day
                    double budget = c.State.Budget, draw = period.AccruedSwfDraw;
                    if (run.Sim.AdvanceDay()) { return double.NaN; }   // a boundary would re-plan the period - never on day 2
                    drawn = period.AccruedSwfDraw - draw;
                    return c.State.Budget - budget;
                }
                finally { Close(run); }
            }
            double held = DayOf(false, out double drawnHeld), empty = DayOf(true, out double drawnEmpty);
            if (double.IsNaN(held) || double.IsNaN(empty)) { Check(false, "the two worlds' planted day ran - the draw's booking is UNVERIFIED, not clean"); return; }
            double difference = held - empty;
            Check(drawnHeld > 0.0 && drawnEmpty == 0.0 && Math.Abs(difference - drawnHeld) <= 1e-4 + 1e-6 * Math.Abs(held),
                F("the budgets differ by {0:R} where the fund paid out {1:R} (the emptied fund {2:R}) - the draw reached the budget whole at a multiplier of 0.5", difference, drawnHeld, drawnEmpty));
        }

        // ============================================================ (d) the default (the USA)
        /// <summary>(d), K212 on day one: the USA opened at 290 % of GDP, its sensitivity as seeded - the first review's trigger rates CCC without the reserve
        /// discount while the standing rating does not, and the default opens that day.</summary>
        private static void DefaultTriggerDayOne()
        {
            _sb.Append("    d0. K212: the USA opened at 290 % of GDP, its sensitivity as seeded, one day\n");
            StressRun run = Open(CountryId.USA, "MONEYIDENTITY_K212");
            try
            {
                Country c = run.World.GetCountry(CountryId.USA);
                c.State.GovernmentDebt = 2.9f * c.State.NominalGdp;
                // K212: the debt ratio from which the undiscounted rating (sensitivity 1) reads CCC on debt alone, and what the discounted one reads there
                float threshold = float.NaN;
                for (float d = 60f; d <= 400f; d += 0.5f) { if (CreditRatingSystem.EvaluateFrom(d, 1f, null, null).Rating == CreditRating.CCC) { threshold = d; break; } }
                CreditRating discountedThere = CreditRatingSystem.EvaluateFrom(290f, c.RiskPremiumSensitivity, null, null).Rating;
                Check(!float.IsNaN(threshold) && threshold <= 290f && discountedThere != CreditRating.CCC && SovereignCrisis.TriggerSensitivity(c) == 1f,
                    F("K212: the undiscounted rating reads CCC on debt alone from {0:0.0} % of GDP; at 290 % the standing rating (sensitivity {1:0.00}) reads {2}, the trigger's sensitivity is {3:0.00}",
                        threshold, c.RiskPremiumSensitivity, CreditRatingSystem.Format(discountedThere), SovereignCrisis.TriggerSensitivity(c)));
                if (run.Sim.AdvanceDay()) { run.Sim.AdvanceTurn(run.Decisions); }
                Check(c.Crisis.Phase == CrisisPhase.Default && c.Crisis.TriggerRating == CreditRating.CCC && c.Rating.Rating != CreditRating.CCC,
                    F("K212: on day one the trigger's rating read {0}, the standing rating {1}, the phase {2} - the market closed on the undiscounted reading",
                        CreditRatingSystem.Format(c.Crisis.TriggerRating), CreditRatingSystem.Format(c.Rating.Rating), c.Crisis.Phase));
            }
            finally { Close(run); }
        }

        /// <summary>(d), the default end to end (the q32 review 2's N2): the USA runs as seeded until the first 1 July a year or more after its start, its debt
        /// is then raised to 290 % of GDP (booked as a probe one-off on the ledger, the budget paying it, so the identity holds), the 1 October review reads it
        /// with a year of closings behind it and the default opens; the review a year later reads the haircut inside its year.</summary>
        private static void Default()
        {
            _sb.Append("    d. the USA run as seeded, its debt raised to 290 % of GDP on the first 1 July a year after its start: the default (K142) at the 1 October review, six years\n");
            StressRun run = Open(CountryId.USA, "MONEYIDENTITY_DEFAULT");
            try
            {
                Country c = run.World.GetCountry(CountryId.USA);
                DateTime startDate = run.Sim.CurrentDate.Date;
                bool raised = false;
                DateTime? raisedOn = null;
                float debtAtEntry = float.NaN, gdpAtEntry = float.NaN, haircut = float.NaN, perDay = float.NaN;
                DateTime? entered = null, ended = null;
                float cutWhileExcluded = 0f;
                bool endedOpen = false, endedOnReview = false;
                // the q32 review's H1: the haircut out of the year's balance, the fiscal vote's open ratio and the rating's settled deficit
                bool oneOffClean = false, openRatioClean = false, reportRead = false, reportClean = false, ratingRead = false, ratingClean = false;
                string reportLine = "no year closed after the default", ratingLine = "no review after the default";
                int yearsClosed = c.History.BudgetBalanceAnnual.Count;
                // the q32 review's M1: a crisis opens only on a review day
                DateTime lastStart = DateTime.MinValue;
                int offReviewEntries = 0;
                string offReviewLine = null;
                Advance(run, c, 6 * SimulationManager.DaysPerTurn, day =>
                {
                    if (!raised && run.Sim.CurrentDate.Month == 7 && run.Sim.CurrentDate.Day == 1 && (run.Sim.CurrentDate.Date - startDate).Days >= 365)
                    {
                        raised = true;
                        raisedOn = run.Sim.CurrentDate.Date;
                        float before = c.State.GovernmentDebt;
                        SimulationManager.ApplyOneTimeBudgetImpact(c, -(2.9f * c.State.NominalGdp - before));
                        DebtLedgerRecorder.RecordEvent(c, run.Sim.CurrentDate, "MoneyIdentity probe: the debt raised to 290 % of GDP", before, c.State.GovernmentDebt);
                    }
                    if (c.Crisis.Phase != CrisisPhase.None && c.Crisis.StartedOn != lastStart)
                    {
                        lastStart = c.Crisis.StartedOn;
                        if (c.Crisis.TriggerReviewedOn != run.Sim.CurrentDate.Date) { offReviewEntries++; offReviewLine = offReviewLine ?? F("a crisis opened on {0:yyyy-MM-dd}, its trigger last reviewed {1:yyyy-MM-dd}", run.Sim.CurrentDate, c.Crisis.TriggerReviewedOn); }
                    }
                    if (!entered.HasValue && c.Crisis.Phase == CrisisPhase.Default)
                    {
                        // H1, on the day itself: the ledger holds the haircut as a stock write-off; the one-offs and the open ratio leave it out
                        DebtAttribution ledger = c.FiscalLedgerAccruing;
                        DebtEventRecord cut = ledger?.Events?.Find(e => e.StockWriteOff);
                        if (cut != null)
                        {
                            List<FiscalOneOff> lines = FiscalOneOff.FromLedger(ledger, out float oneOffSum);
                            double others = 0.0;
                            foreach (DebtEventRecord e in ledger.Events) { if (!e.StockWriteOff) { others -= e.AppliedDelta; } }
                            oneOffClean = lines.TrueForAll(l => l.Label != cut.Label) && Math.Abs(oneOffSum - others) < 1e-3;
                            double? withEvent = PoliSim.Elections.FiscalVote.OpenRatio(c);
                            ledger.Events.Remove(cut);
                            double? withoutEvent = PoliSim.Elections.FiscalVote.OpenRatio(c);
                            ledger.Events.Add(cut);
                            openRatioClean = withEvent.HasValue == withoutEvent.HasValue && (!withEvent.HasValue || Math.Abs(withEvent.Value - withoutEvent.Value) < 1e-9);
                        }
                    }
                    if (entered.HasValue && !reportRead && c.History.BudgetBalanceAnnual.Count > yearsClosed)
                    {
                        reportRead = true;
                        FiscalTurnReport r = run.Sim.GetLastFiscalReport(c.Id);
                        bool noHaircutLine = r != null && (r.OneOffLines == null || r.OneOffLines.TrueForAll(l => !l.Label.StartsWith("Sovereign default")));
                        reportClean = r != null && noHaircutLine && Math.Abs(r.BudgetBalance - (r.FlowBalance + r.OneOffs)) <= 1e-3f + 1e-6f * Math.Abs(r.BudgetBalance);
                        reportLine = r == null ? "no report" : F("the year closed {0:yyyy-MM-dd}: balance {1:0.#} = flow {2:0.#} + one-offs {3:0.#}, no haircut line {4}", run.Sim.CurrentDate, r.BudgetBalance, r.FlowBalance, r.OneOffs, noHaircutLine);
                    }
                    // the first scheduled review after the default whose year holds both closings and the haircut: its deficit is no haircut-sized surplus
                    // (the run may reach none - a start with no closing a year back - and then the line says so; the arithmetic is held below either way)
                    if (entered.HasValue && !ratingRead && c.Crisis.TriggerReviewedOn > entered.Value.Date && c.Crisis.TriggerReviewedOn == run.Sim.CurrentDate.Date
                        && entered.Value.Date >= run.Sim.CurrentDate.Date.AddYears(-1))
                    {
                        CreditRatingSystem.GetSettledPosition(c, run.Sim.CurrentDate, out _, out float? deficit, out _);
                        if (deficit.HasValue)
                        {
                            ratingRead = true;
                            float haircutPercent = c.Crisis.HaircutBillions / Mathf.Max(1f, c.State.NominalGdp) * 100f;
                            ratingClean = deficit.Value > -0.5f * haircutPercent;
                            ratingLine = F("the review of {0:yyyy-MM-dd} read a deficit of {1:0.0} % of GDP (the haircut was {2:0.0} %)", run.Sim.CurrentDate, deficit.Value, haircutPercent);
                        }
                    }
                    if (!entered.HasValue && c.Crisis.Phase == CrisisPhase.Default)
                    {
                        entered = run.Sim.CurrentDate;
                        haircut = c.Crisis.HaircutBillions;
                        debtAtEntry = c.State.GovernmentDebt + haircut;   // the stock the haircut was taken on, give or take the day's own balance (checked loosely below)
                        perDay = c.Crisis.OutputCostPerDay;
                        gdpAtEntry = c.State.GDP;
                    }
                    if (entered.HasValue && !ended.HasValue && (c.Crisis.Phase == CrisisPhase.None || c.Crisis.StartedOn.Date != entered.Value.Date)) { ended = run.Sim.CurrentDate; cutWhileExcluded = c.Crisis.CutBillions; endedOpen = c.Crisis.Phase == CrisisPhase.None; endedOnReview = c.Crisis.TriggerReviewedOn == run.Sim.CurrentDate.Date; }   // a second default opens only on a review day (M1)
                });
                Check(oneOffClean, "H1: on the default's day the debt ledger holds the haircut as a stock write-off, and the year's one-offs leave it out");
                Check(openRatioClean, "H1: the fiscal vote's open ratio is the same with the haircut's ledger event and without it");
                Check(reportRead && reportClean, "H1: " + reportLine);
                Check(ratingRead && ratingClean, "H1 (N2: reached, or it fails): the rating's settled deficit adds the haircut back - " + ratingLine);
                // H1, the arithmetic: at the run's last fiscal-year review, a haircut dated inside the year moves the settled deficit by exactly its share of the
                // closing GDP the reading divides by
                DateTime review = new DateTime(run.Sim.CurrentDate.Year, 10, 1);
                if (review > run.Sim.CurrentDate) { review = review.AddYears(-1); }
                CreditRatingSystem.GetSettledPosition(c, review, out _, out float? before, out _);
                DateTime settled = ReleaseCalendar.GetCurrentPeriodStart(ClosingStat.Gdp, review.AddDays(-1));
                float? gdpNow = c.Published.ClosingValue(ClosingStat.Gdp, settled);
                var planted = new DatedAmount { Date = review.AddDays(-30), Billions = 1000f };
                c.Crisis.Haircuts.Add(planted);
                CreditRatingSystem.GetSettledPosition(c, review, out _, out float? after, out _);
                c.Crisis.Haircuts.Remove(planted);
                bool arithmetic = before.HasValue && after.HasValue && gdpNow.HasValue && gdpNow.Value > 0f && Mathf.Abs((after.Value - before.Value) - 1000f / gdpNow.Value * 100f) < 1e-3f;
                Check(arithmetic, F("H1: a 1000 bn haircut dated inside the year to {0:yyyy-MM-dd} adds {1} points to the settled deficit (its share of the closing GDP {2})",
                    review, before.HasValue && after.HasValue ? (after.Value - before.Value).ToString("0.000", Inv) : "unread", gdpNow.HasValue ? gdpNow.Value.ToString("0.#", Inv) : "unread"));
                Check(offReviewEntries == 0, offReviewEntries == 0 ? "M1: every crisis the run opened, opened on a review day" : "M1: " + offReviewEntries + " crisis/crises opened off a review day - first " + offReviewLine);
                Check(ended.HasValue && (endedOpen || endedOnReview), F("M1: the exclusion ended on {0} ({1}); the market stood open that evening: {2}; crises entered over the run {3}",
                    ended.HasValue ? ended.Value.ToString("yyyy-MM-dd", Inv) : "never", endedOnReview ? "a review day" : "off a review day", endedOpen, c.Crisis.Crises));
                Check(entered.HasValue, F("the default fired on {0} (the first review rated CCC)", entered.HasValue ? entered.Value.ToString("yyyy-MM-dd", Inv) : "never"));
                if (!entered.HasValue) { return; }
                Check(raisedOn.HasValue && entered.Value.Month == 10 && entered.Value.Day == 1 && entered.Value > raisedOn.Value,
                    F("N2: the debt raised on {0}, the default opened on {1} - a 1 October review with a year of closings behind it", raisedOn.HasValue ? raisedOn.Value.ToString("yyyy-MM-dd", Inv) : "never", entered.Value.ToString("yyyy-MM-dd", Inv)));
                Check(Mathf.Abs(haircut / Mathf.Max(1f, debtAtEntry) - SovereignCrisis.HaircutFraction) < 0.01f, F("the haircut wrote {0:0.#} bn off, {1:0.000} of the stock (Cruces and Trebesch's {2:0.00})", haircut, haircut / Mathf.Max(1f, debtAtEntry), SovereignCrisis.HaircutFraction));
                float cost = perDay * SovereignCrisis.OutputCostDays / Mathf.Max(1f, gdpAtEntry) * 100f;
                Check(Mathf.Abs(cost - SovereignCrisis.OutputCostPercent) < 0.1f && c.Crisis.OutputCostDaysLeft == 0, F("the output cost: {0:0.00} % of GDP over {1} days (Borensztein and Panizza's {2:0.0}), all landed ({3} days left)", cost, SovereignCrisis.OutputCostDays, SovereignCrisis.OutputCostPercent, c.Crisis.OutputCostDaysLeft));
                int span = ended.HasValue ? (ended.Value - entered.Value).Days : -1;
                int expected = (int)Math.Round(SovereignCrisis.ExclusionYears * 365.25);
                Check(Math.Abs(span - expected) <= 1, F("the exclusion ran {0} days ({1:0.0} years x 365.25 = {2})", span, SovereignCrisis.ExclusionYears, expected));
                Check(cutWhileExcluded > 0f, F("excluded, the deficit went unfinanced: {0:0.#} bn of non-interest outlays cut", cutWhileExcluded));
            }
            finally { Close(run); }
        }
    }
}
