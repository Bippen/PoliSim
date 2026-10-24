using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using PoliSim.Data;
using PoliSim.Elections;
using PoliSim.Elections.Generated;
using PoliSim.Persistence;
using PoliSim.Simulation;
using UnityEngine;
using Object = UnityEngine.Object;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// A-LOOP q13 (2026-10-19, `docs/specs/A_LOOP_PLAN.md` §4 (4)): <b>the fiscal term's pass criteria that an Editor harness can prove</b>, on Sweden (seed
    /// 777, Sweden the player's country, every boundary resolved with no policy), and since Elias's rulings K61-K64 (2026-10-23) the change reading, the
    /// sourced pre-run years, the tenure gate, the robust spread and the cap. No approval is written here (the book is the vote's alone and no ledger
    /// audits it), so nothing the ATTRIB audit reads is touched.
    /// <list type="number">
    /// <item><b>The book on the play path</b>, across two boundaries: at the world's entry the book opens on the epoch with the sourced years K62 seeds
    /// and no day accrued; each day the open year's flow is the fiscal period's accrual to date and its days are the debt ledger's days; at each boundary
    /// the newest closed year is the year the player reads (the fiscal report's `FiscalTurnReport.BudgetBalance`) with nominal GDP at the close (the debt
    /// ledger's `DebtAttribution.GdpAtClose`) and the day it opened, and the open year empties and opens on the boundary; the variables on the day after
    /// the second boundary are formed from the years the book holds.</item>
    /// <item><b>The variables' definitions on a planted balance path</b> (a bare country, no tenure read): four closed years give BALCH_term = ½(BAL-1 +
    /// BAL-2) − ½(BAL-3 + BAL-4), [BD06] Appendix I's; two or three its fallback BAL-1 − BAL-2; one none; BAL0 is the open year's flow annualised plus
    /// its one-offs to date, over nominal GDP; BALCH_ey = BAL0 − BAL-1; delta-z and sigma_v × delta-z. K61's change reading: a surplus held then relaxed
    /// LOSES, a deficit narrowing GAINS - the sign follows the balance's change across the window, never its level.</item>
    /// <item><b>The term's size and its split</b> (Sweden after one day, a path planted under the government's tenure): shift_gov split by the
    /// responsibility vector, summing to it; the record is the economic vote plus the approval term plus the fiscal term, per party.</item>
    /// <item><b>The bound and the cap</b> (K64): a variable past ±`FiscalVote.SupportBound` is held at it; shift_gov is held at ±`FiscalVote.ShareCap`
    /// where sigma_v × delta-z passes it, and the cap binds at the support bound; the shifted shares sum to one, non-negative and finite.</item>
    /// <item><b>Zero at no change</b>: a flat balance path gives delta-z exactly 0 and the shifted shares the economy's and approval's alone to the bit.</item>
    /// <item><b>The save and the clone</b>: a save cut after the boundary carries the book by content, the opening days and the source years included
    /// (format 61); the preview's clone carries its own copy.</item>
    /// <item><b>Tenure</b> (K63, K99, K101): a year that opened before the governing party's tenure began is not read, nor any older one, and the open
    /// year likewise; the record's reading is gated on `FiscalVote.TenureFrom`; a government of another party formed in play answers for none of its
    /// predecessor's years, and the same party re-formed in play (a re-election) answers for its own previous four in the four-year form.</item>
    /// <item><b>The sourced pre-run years at every start</b> (K62, K100): each country's book at its own start opens on the start; its closed years
    /// are the four newest source years that had closed by the start (the USA's federal year from 1 October), at the record's ratio, none still open;
    /// the governing party's tenure is the governments of record walked back over the party's own rows (K101); K98 with K100: before the first game
    /// year closes BAL0 is the source's figure for the election's calendar year (none where the source does not yet publish it), never the game's
    /// own nor a later year; and where a country holds a count, its first count carries a fiscal term. The game's own BAL0 is printed beside.</item>
    /// </list>
    /// Prints `FISCAL VOTE: PASS` and exits 0 when every check holds; each failure is a `FISCAL VOTE:` error naming its check.
    /// Run: `Unity.exe -batchmode -nographics -projectPath &lt;path&gt; -executeMethod PoliSim.EditorTools.FiscalVoteDiagnostic.Run -logFile &lt;path&gt;`.
    /// </summary>
    public static class FiscalVoteDiagnostic
    {
        private const double Exact = 1e-12;
        private const int Boundaries = 2;

        private static int _failures;
        private static StringBuilder _sb;

        public static void Run()
        {
            CheckExit.ArmLogFold();   // the world crosses boundaries: an ATTRIB during the run fails it
            _failures = 0;
            _sb = new StringBuilder("FISCAL VOTE: A-LOOP q13 - deficits reach the vote through Brender & Drazen's two balance variables (Sweden, seed 777)\n");
            _sb.Append(F("    beta_term {0} and beta_ey {1} (the expanded sample's {2} and {3} beside them); sigma_v {4:R} pp, the MAD-based scale (K64; IQR-based {5:F3}, sd {6:F3} with 95 % {7:F3} - {8:F3} under normality, kurtosis {9:F2}; n {10}); the bound +/- {11} of GDP; the cap +/- {12:0.00} pp of share\n",
                FiscalVote.BetaTerm, FiscalVote.BetaElectionYear, FiscalVote.ExpandedBetaTerm, FiscalVote.ExpandedBetaElectionYear, 100.0 * FiscalVote.SwingSpread,
                IncumbentVoteSwing.IqrScalePoints, IncumbentVoteSwing.SdPoints, IncumbentVoteSwing.SdLowerPoints, IncumbentVoteSwing.SdUpperPoints, IncumbentVoteSwing.Kurtosis, IncumbentVoteSwing.N,
                FiscalVote.SupportBound, 100.0 * FiscalVote.ShareCap));
            try
            {
                PlayPath();
                Definitions();
                SizeAndSplit();
                Bound();
                ExactlyZero();
                Tenure();
                SeedAtEveryStart();
                EveWorlds();
            }
            catch (Exception e) { Check(false, "the run threw " + e.GetType().Name + ": " + e.Message + "\n" + e.StackTrace); }
            Debug.Log(_sb.ToString());
            Debug.Log(_failures == 0
                ? "FISCAL VOTE: PASS - the book on the play path over two boundaries, the variables' definitions and the change reading, the term's size and split, the bound and the cap, exactly zero at no change, the save and the clone, the tenure gate, the sourced pre-run years at every start."
                : F("FISCAL VOTE: FAIL - {0} check(s) failed.", _failures));
            CheckExit.Finish(_failures == 0 ? 0 : 1);
        }

        // ============================================================ (1) the book on the play path; (6) the save and the clone
        private static void PlayPath()
        {
            _sb.Append(F("    1. the book on the play path, across {0} boundaries\n", Boundaries));
            (SimulationManager sim, World world, GameObject go) = Open();
            try
            {
                Country se = world.GetCountry(CountryId.Sweden);
                FiscalVoteBook entry = se.FiscalVoteRecord;
                EconomicVote.RecordParts before = EconomicVote.RecordPartsOverTerm(se, sim.CurrentDate);
                Check(entry != null && entry.OpenedOn == SimulationManager.EpochDate && entry.OpenDays == 0 && entry.Closed.All(y => y.SourceYear != 0 && y.ClosedOn <= SimulationManager.EpochDate)
                      && !before.FiscalReading.HasElectionYear,
                    F("(1a) at the world's entry the book opens on the epoch {0:yyyy-MM-dd} with {1} sourced year(s) before it (K62) and no day accrued: no election-year variable - {2}", SimulationManager.EpochDate, entry?.Closed.Count, FiscalVote.Describe(before.FiscalReading)));
                int seeded = entry?.Closed.Count ?? 0;
                MethodInfo periodOf = typeof(SimulationManager).GetMethod("GetOrSeedFiscalPeriod", BindingFlags.NonPublic | BindingFlags.Instance);
                if (periodOf == null) { Check(false, "(1) SimulationManager.GetOrSeedFiscalPeriod not found - the open year verified NOTHING"); return; }
                var decisions = new Dictionary<CountryId, PolicyDecision>();
                foreach (Country k in world.Countries) { decisions[k.Id] = PolicyDecision.None(); }
                int crossed = 0, mirrored = 0, days = 0;
                for (int d = 0; d < 400 * Boundaries && crossed < Boundaries; d++)
                {
                    bool boundary = sim.AdvanceDay();
                    days++;
                    foreach (Country k in world.Countries)
                    {
                        var period = (SimulationManager.FiscalPeriod)periodOf.Invoke(sim, new object[] { k });
                        FiscalVoteBook b = k.FiscalVoteRecord;
                        bool same = b != null && b.OpenFlow == period.AccruedBudgetBalance && k.FiscalLedgerAccruing != null && b.OpenDays == k.FiscalLedgerAccruing.DaysRecorded && b.OpenDays > 0;
                        if (same) { mirrored++; }
                        else if (mirrored >= 0) { Check(false, F("(1b) {0} on {1:yyyy-MM-dd}: the open year's flow {2:R} over {3} day(s) is not the period's accrual {4:R} over the debt ledger's {5} day(s)", k.Id, sim.CurrentDate, b?.OpenFlow, b?.OpenDays, period.AccruedBudgetBalance, k.FiscalLedgerAccruing?.DaysRecorded)); mirrored = -1; }
                    }
                    if (!boundary) { continue; }
                    var countBefore = world.Countries.ToDictionary(k => k.Id, k => k.FiscalVoteRecord.Closed.Count);
                    var openedBefore = world.Countries.ToDictionary(k => k.Id, k => k.FiscalVoteRecord.OpenedOn);
                    sim.AdvanceTurn(decisions);
                    crossed++;
                    foreach (Country k in world.Countries)
                    {
                        FiscalVoteBook b = k.FiscalVoteRecord;
                        FiscalTurnReport report = sim.GetLastFiscalReport(k.Id);
                        FiscalVoteYear newest = b.Closed.Count > 0 ? b.Closed[b.Closed.Count - 1] : null;
                        Check(newest != null && report != null && b.Closed.Count == Math.Min(FiscalVoteBook.Kept, countBefore[k.Id] + 1) && newest.ClosedOn == sim.CurrentDate
                              && newest.OpenedOn == openedBefore[k.Id] && newest.SourceYear == 0 && b.OpenedOn == sim.CurrentDate
                              && newest.Balance == report.BudgetBalance && newest.NominalGdp == k.FiscalLedgerLastPeriod.GdpAtClose && b.OpenFlow == 0f && b.OpenDays == 0,
                            F("(1c) boundary {0} ({1:yyyy-MM-dd}) {2}: the closed year {3:R} bn, opened {4:yyyy-MM-dd}, is the year the player reads ({5:R} = the flow {6:R} + one-offs {7:R}) over nominal GDP {8:R} at the close; {9} closed year(s); the open year emptied, opening on the boundary",
                                crossed, sim.CurrentDate, k.Id, newest?.Balance, newest?.OpenedOn, report?.BudgetBalance, report?.FlowBalance, report?.OneOffs, newest?.NominalGdp, b.Closed.Count));
                    }
                }
                Check(crossed == Boundaries && mirrored > 0, F("(1d) {0} of {1} boundaries crossed in {2} days; the open year mirrored on every country every day ({3} readings)", crossed, Boundaries, days, mirrored));
                Days(sim, 1);
                EconomicVote.RecordParts parts = EconomicVote.RecordPartsOverTerm(se, sim.CurrentDate);
                FiscalVoteBook book = se.FiscalVoteRecord;
                FiscalVote.Reading r = FiscalVote.Judge(se, null);   // the definitions on the years the book holds; the gate is (7)'s
                int n = book.Closed.Count;
                double Bal(int i) => (double)book.Closed[n - i].Balance / book.Closed[n - i].NominalGdp;
                double expectedTerm = n >= 4 ? 0.5 * (Bal(1) + Bal(2)) - 0.5 * (Bal(3) + Bal(4)) : Bal(1) - Bal(2);
                Check(n == Math.Min(FiscalVoteBook.Kept, seeded + Boundaries) && r.ClosedYears == n && r.HasTerm && r.TermFallback == (n < 4) && r.BalchTerm == expectedTerm && r.HasElectionYear
                      && parts.FiscalReading.TenureFrom == FiscalVote.TenureFrom(se),
                    F("(1e) the day after the second boundary the book holds {0} closed year(s) ({1} sourced, {2} played) and the variables are formed on them ({3}); the record reads them under the government's tenure (from {4:yyyy-MM-dd}) - {5}",
                        n, seeded, Boundaries, n >= 4 ? "BD06's four-year form" : "BD06's fallback", parts.FiscalReading.TenureFrom, FiscalVote.Describe(parts.FiscalReading)));
                _sb.Append(F("        (printed) the no-policy balance path: {0}\n", string.Join("; ", world.Countries.Select(k =>
                {
                    FiscalVote.Reading kr = FiscalVote.Judge(k, null);
                    return F("{0} BAL-2 {1:+0.000;-0.000} BAL-1 {2:+0.000;-0.000} pp of GDP, term {3:+0.000;-0.000}, index {4:+0.0000;-0.0000} -> {5:+0.00;-0.00} pp", k.Id, 100.0 * kr.Bal2, 100.0 * kr.Bal1, 100.0 * kr.BalchTerm, kr.Index, 100.0 * kr.GovernmentShift);
                }))));

                _sb.Append("    6. the save and the clone\n");
                string json = SaveGameService.Serialize(SaveGameService.CreateSaveGame(sim, world, CountryId.Sweden, null));
                SaveGame cut = SaveGameService.Deserialize(json);
                Country saved = cut.World.GetCountry(CountryId.Sweden);
                Check(cut.SaveVersion == SaveGameService.CurrentSaveVersion && cut.SaveVersion >= 61 && SameBook(saved.FiscalVoteRecord, book),
                    F("(6a) a save cut on {0:yyyy-MM-dd} carries format {1} and the book by content ({2} closed year(s) with their opening days and source years, the open year {3:R} over {4} day(s) from {5:yyyy-MM-dd})", sim.CurrentDate, cut.SaveVersion, saved.FiscalVoteRecord?.Closed.Count, saved.FiscalVoteRecord?.OpenFlow, saved.FiscalVoteRecord?.OpenDays, saved.FiscalVoteRecord?.OpenedOn));
                MethodInfo clone = typeof(SimulationManager).GetMethod("ClonePreviewCountry", BindingFlags.NonPublic | BindingFlags.Static);
                var copy = clone?.Invoke(null, new object[] { se }) as Country;
                Check(copy != null && SameBook(copy.FiscalVoteRecord, book) && !ReferenceEquals(copy.FiscalVoteRecord, book) && !ReferenceEquals(copy.FiscalVoteRecord.Closed, book.Closed),
                    "(6b) the preview's clone (ClonePreviewCountry) carries the book as its own copy");
            }
            finally { Object.DestroyImmediate(go); }
        }

        // ============================================================ (2) the variables' definitions, on a planted balance path; K61's change reading
        private static void Definitions()
        {
            _sb.Append("    2. the variables' definitions on a planted balance path (a bare country, no tenure read)\n");
            SimulationRandom.Seed(777);
            Country k = WorldFactory.CreateDefault().GetCountry(CountryId.Sweden);   // no simulation runs on it
            float gdp = k.State.NominalGdp;
            k.FiscalLedgerAccruing = null;
            DateTime from = new DateTime(2020, 1, 1);
            // oldest first: BAL-4 -1 %, BAL-3 -2 %, BAL-2 -3 %, BAL-1 -4 % of GDP; the open year -6 % annualised over 100 days
            k.FiscalVoteRecord = Book(gdp, new[] { -0.01, -0.02, -0.03, -0.04 }, -0.06, 100, from);
            FiscalVote.Reading four = FiscalVote.Judge(k, null);
            double b1 = Ratio(k, 3), b2 = Ratio(k, 2), b3 = Ratio(k, 1), b4 = Ratio(k, 0);
            double term = 0.5 * (b1 + b2) - 0.5 * (b3 + b4);
            double bal0 = (double)k.FiscalVoteRecord.OpenFlow * SimulationManager.DaysPerTurn / 100 / gdp;
            Check(four.ClosedYears == 4 && four.HasTerm && !four.TermFallback && four.BalchTerm == term && Math.Abs(term + 0.02) <= 1e-6,
                F("(2a) four closed years: BALCH_term = 1/2 (BAL-1 + BAL-2) - 1/2 (BAL-3 + BAL-4) = {0:+0.000000;-0.000000} (the path's -0.02)", four.BalchTerm));
            Check(four.HasElectionYear && four.Bal0 == bal0 && four.BalchEy == bal0 - b1 && Math.Abs(four.BalchEy + 0.02) <= 1e-6,
                F("(2b) BAL0 = the open year's flow x {0} / 100 days over nominal GDP = {1:+0.000000;-0.000000}; BALCH_ey = BAL0 - BAL-1 = {2:+0.000000;-0.000000}", SimulationManager.DaysPerTurn, four.Bal0, four.BalchEy));
            double index = FiscalVote.BetaTerm * four.BalchTerm + FiscalVote.BetaElectionYear * four.BalchEy;
            Check(four.Index == index && four.Uncapped == FiscalVote.SwingSpread * index && four.GovernmentShift == FiscalVote.Capped(four.Uncapped) && four.HeldTerm == four.BalchTerm && four.HeldEy == four.BalchEy,
                F("(2c) delta-z = {0} x BALCH_term + {1} x BALCH_ey = {2:+0.000000;-0.000000}; shift_gov = sigma_v x delta-z = {3:+0.0000;-0.0000} pp (within the cap); inside the bound nothing is held", FiscalVote.BetaTerm, FiscalVote.BetaElectionYear, four.Index, 100.0 * four.GovernmentShift));

            k.FiscalVoteRecord = Book(gdp, new[] { -0.03, -0.04 }, -0.06, 100, from);
            FiscalVote.Reading two = FiscalVote.Judge(k, null);
            k.FiscalVoteRecord = Book(gdp, new[] { -0.02, -0.03, -0.04 }, -0.06, 100, from);
            FiscalVote.Reading three = FiscalVote.Judge(k, null);
            k.FiscalVoteRecord = Book(gdp, new[] { -0.04 }, -0.06, 100, from);
            FiscalVote.Reading one = FiscalVote.Judge(k, null);
            k.FiscalVoteRecord = Book(gdp, new double[0], -0.06, 100, new DateTime(1990, 1, 1));   // none: an empty book is a first count (K98), read off the record by its election year - planted in 1990, before any year the record holds, so the case reads no year whatever the record's rows
            FiscalVote.Reading none = FiscalVote.Judge(k, null);
            Check(two.HasTerm && two.TermFallback && two.BalchTerm == two.Bal1 - two.Bal2 && three.TermFallback && three.BalchTerm == three.Bal1 - three.Bal2
                  && !one.HasTerm && one.HasElectionYear && !none.HasTerm && !none.HasElectionYear && none.Index == 0.0,
                F("(2d) two or three closed years: [BD06]'s fallback BAL-1 - BAL-2 ({0:+0.000000;-0.000000}); one: no term variable, BALCH_ey {1:+0.000000;-0.000000}; none: no term - {2}", two.BalchTerm, one.BalchEy, FiscalVote.Describe(none)));

            // the open year's one-offs to date count unscaled (declared reading 2): a cost of 1 % of GDP landed on the debt
            k.FiscalVoteRecord = Book(gdp, new[] { -0.04 }, -0.06, 100, from);
            k.FiscalLedgerAccruing = new DebtAttribution();
            k.FiscalLedgerAccruing.Events.Add(new DebtEventRecord { Date = new DateTime(2026, 1, 1), Label = "planted (FiscalVoteDiagnostic): a one-off", AppliedDelta = 0.01f * gdp });
            FiscalVote.Reading withOneOff = FiscalVote.Judge(k, null);
            double expected0 = ((double)k.FiscalVoteRecord.OpenFlow * SimulationManager.DaysPerTurn / 100 - k.FiscalLedgerAccruing.Events[0].AppliedDelta) / gdp;   // the stored field, never a float intermediate
            Check(withOneOff.Bal0 == expected0 && Math.Abs(withOneOff.Bal0 - (bal0 - 0.01)) <= 1e-6,
                F("(2e) a one-off cost of 1 % of GDP to date lowers BAL0 by 1 point, unannualised: {0:+0.000000;-0.000000} against {1:+0.000000;-0.000000}", withOneOff.Bal0, bal0));
            k.FiscalVoteRecord.OpenDays = 0;
            Check(!FiscalVote.Judge(k, null).HasElectionYear, "(2f) with no day of the open year accrued there is no BAL0 and no BALCH_ey");
            k.FiscalLedgerAccruing = null;

            // K98 (2026-10-24): before the first game year closes BAL0 is the source's; from the first played year that closes, the game's own
            k.FiscalVoteRecord = Book(gdp, new[] { -0.01, -0.02 }, -0.06, 100, new DateTime(2021, 1, 1));
            k.FiscalVoteRecord.Closed[0].SourceYear = 2021; k.FiscalVoteRecord.Closed[1].SourceYear = 2022;
            FiscalVote.Reading sourcedOpen = FiscalVote.Judge(k, null);
            int sourceAt = Array.FindIndex(FiscalBalanceRecord.For(CountryId.Sweden), row => row.Year == 2023);
            double source2023 = sourceAt >= 0 ? FiscalBalanceRecord.For(CountryId.Sweden)[sourceAt].Percent / 100.0 : double.NaN;
            k.FiscalVoteRecord.Closed[1].SourceYear = 0;   // the newest closed year now one the game played
            FiscalVote.Reading gameOpen = FiscalVote.Judge(k, null);
            double gameBal0 = (double)k.FiscalVoteRecord.OpenFlow * SimulationManager.DaysPerTurn / 100 / gdp;
            Check(k.FiscalVoteRecord.OpenedOn == new DateTime(2023, 1, 1) && sourcedOpen.Source0 == 2023 && sourcedOpen.Bal0 == source2023 && sourcedOpen.BalchEy == source2023 - sourcedOpen.Bal1
                  && gameOpen.Source0 == 0 && gameOpen.Bal0 == gameBal0 && Math.Abs(gameOpen.Bal0 + 0.06) <= 1e-6,
                F("(2h) K98: with only sourced years closed, BAL0 is the source's 2023 ({0:+0.000;-0.000} %), not the open year's own accrual; once a played year has closed, the game's own ({1:+0.000;-0.000} %) - {2}",
                    100.0 * sourcedOpen.Bal0, 100.0 * gameOpen.Bal0, FiscalVote.Describe(sourcedOpen)));
            k.FiscalVoteRecord = null;

            // K61 (2026-10-23): "a pole whose balance improves across the window gains" - the variables are changes, so the sign follows the change, not the level
            k.FiscalVoteRecord = Book(gdp, new[] { 0.03, 0.03, 0.01, 0.01 }, 0.005, 365, from);   // a surplus held, then relaxed: every year in surplus
            FiscalVote.Reading relaxed = FiscalVote.Judge(k, null);
            k.FiscalVoteRecord = Book(gdp, new[] { -0.06, -0.06, -0.03, -0.03 }, -0.02, 365, from);   // a deficit narrowing: every year in deficit
            FiscalVote.Reading narrowing = FiscalVote.Judge(k, null);
            Check(relaxed.BalchTerm < 0.0 && relaxed.BalchEy < 0.0 && relaxed.GovernmentShift < 0.0 && relaxed.Bal1 > 0.0 && relaxed.Bal0 > 0.0
                  && narrowing.BalchTerm > 0.0 && narrowing.BalchEy > 0.0 && narrowing.GovernmentShift > 0.0 && narrowing.Bal1 < 0.0 && narrowing.Bal0 < 0.0,
                F("(2g) K61, the change reading: a surplus held then relaxed (every year in surplus, BAL-1 {0:+0.0;-0.0} %, BAL0 {1:+0.0;-0.0} %) loses {2:+0.00;-0.00} pp; a deficit narrowing (every year in deficit, BAL-1 {3:+0.0;-0.0} %, BAL0 {4:+0.0;-0.0} %) gains {5:+0.00;-0.00} pp - the sign is the balance's change across the window, never its level",
                    100.0 * relaxed.Bal1, 100.0 * relaxed.Bal0, 100.0 * relaxed.GovernmentShift, 100.0 * narrowing.Bal1, 100.0 * narrowing.Bal0, 100.0 * narrowing.GovernmentShift));
        }

        // ============================================================ (3) the term's size and its split
        private static void SizeAndSplit()
        {
            _sb.Append("    3. the term's size and its split\n");
            WithSwedenAfterOneDay((sim, se, took) =>
            {
                float gdp = se.State.NominalGdp;
                se.FiscalVoteRecord = Tenured(se, Book(gdp, new[] { -0.01, -0.01, -0.02, -0.02 }, -0.03, 200, took));   // a deficit a point worse over the term and again in the election year, under the tenure
                EconomicVote.RecordParts parts = EconomicVote.RecordPartsOverTerm(se, sim.CurrentDate);
                FiscalVote.Reading r = parts.FiscalReading;
                Dictionary<string, double> responsibility = EconomicVote.Responsibility(se, sim.CurrentDate);
                bool split = responsibility.Count > 0 && parts.Fiscal.Count == responsibility.Count
                             && responsibility.All(kv => parts.Fiscal.TryGetValue(kv.Key, out double f) && f == r.GovernmentShift * kv.Value);
                Check(r.Carried && r.ClosedYears == 4 && split && Math.Abs(parts.Fiscal.Values.Sum() - r.GovernmentShift) <= Exact && r.GovernmentShift < 0.0 && !r.CapBinds,
                    F("(3a) shift_gov {0:+0.0000;-0.0000} pp split by responsibility, summing to it: {1}", 100.0 * r.GovernmentShift,
                        string.Join(", ", parts.Fiscal.Select(kv => F("{0} {1:+0.0000;-0.0000} pp", kv.Key, 100.0 * kv.Value)))));
                bool sums = parts.Total.Count == parts.Economy.Count && parts.Economy.All(kv => parts.Total.TryGetValue(kv.Key, out double t)
                    && t == (kv.Value + (parts.Approval.TryGetValue(kv.Key, out double a) ? a : 0.0)) + (parts.Fiscal.TryGetValue(kv.Key, out double f) ? f : 0.0));
                Dictionary<string, double> live = EconomicVote.RecordOverTerm(se, sim.CurrentDate, out _);
                Check(sums && SameBits(live, parts.Total),
                    "(3b) the record is the economic vote plus the approval term (as q15 summed them) plus the fiscal term, per party, and every live count's record (EconomicVote.RecordOverTerm) is that total");
                double s = FiscalVote.SwingSpread;
                _sb.Append(F("        (printed) the plan's scale at sigma_v {0:F3} pp: one point of GDP better over the term is worth {1:+0.000;-0.000} pp of the government's share, one in the election year {2:+0.000;-0.000} pp (the expanded sample's: {3:+0.000;-0.000} and {4:+0.000;-0.000}; at the standard deviation {5:F3}: {6:+0.000;-0.000} and {7:+0.000;-0.000})\n",
                    100.0 * s, 100.0 * s * FiscalVote.BetaTerm * 0.01, 100.0 * s * FiscalVote.BetaElectionYear * 0.01, 100.0 * s * FiscalVote.ExpandedBetaTerm * 0.01, 100.0 * s * FiscalVote.ExpandedBetaElectionYear * 0.01,
                    IncumbentVoteSwing.SdPoints, IncumbentVoteSwing.SdPoints * FiscalVote.BetaTerm * 0.01, IncumbentVoteSwing.SdPoints * FiscalVote.BetaElectionYear * 0.01));
            });
        }

        // ============================================================ (4) the bound and the cap
        private static void Bound()
        {
            _sb.Append("    4. the bound and the cap (K64)\n");
            WithSwedenAfterOneDay((sim, se, took) =>
            {
                float gdp = se.State.NominalGdp;
                List<string> keys = se.ParliamentSeats.Keys.ToList();
                double atBound = FiscalVote.SwingSpread * (FiscalVote.BetaTerm + FiscalVote.BetaElectionYear) * FiscalVote.SupportBound;
                Check(FiscalVote.ShareCap == 0.10 && atBound > FiscalVote.ShareCap,
                    F("(4a) the cap is +/- {0:0.00} pp of share (K64, interim), inside what the support bound alone allows ({1:0.00} pp at sigma_v x (beta_term + beta_ey) x the bound)", 100.0 * FiscalVote.ShareCap, 100.0 * atBound));
                foreach ((double[] years, double open, bool binds) in new[] { (new[] { 0.0, 0.0, -0.30, -0.30 }, -0.60, true), (new[] { -0.30, -0.30, 0.10, 0.10 }, 0.40, true), (new[] { 0.0, 0.0, -0.05, -0.05 }, -0.08, false) })
                {
                    se.FiscalVoteRecord = Tenured(se, Book(gdp, years, open, 365, took));
                    EconomicVote.RecordParts parts = EconomicVote.RecordPartsOverTerm(se, sim.CurrentDate);
                    FiscalVote.Reading r = parts.FiscalReading;
                    double[] after = EconomicVote.ApplyRecordShift(keys, Shares(se, keys), parts.Total);
                    double sum = after.Sum();
                    bool held = r.HeldTerm == FiscalVote.Held(r.BalchTerm) && r.HeldEy == FiscalVote.Held(r.BalchEy) && Math.Abs(r.HeldTerm) <= FiscalVote.SupportBound && Math.Abs(r.HeldEy) <= FiscalVote.SupportBound;
                    bool capped = r.Uncapped == FiscalVote.SwingSpread * r.Index && Math.Abs(r.GovernmentShift) <= FiscalVote.ShareCap && r.CapBinds == binds
                                  && (binds ? Math.Abs(r.GovernmentShift) == FiscalVote.ShareCap && Math.Abs(r.Uncapped) > FiscalVote.ShareCap : r.GovernmentShift == r.Uncapped);
                    bool sane = after.All(x => x >= 0.0 && !double.IsNaN(x) && !double.IsInfinity(x)) && Math.Abs(sum - 1.0) <= 1e-9;
                    Check(held && capped && sane,
                        F("(4b) BALCH_term {0:+0.000;-0.000} held at {1:+0.000;-0.000}, BALCH_ey {2:+0.000;-0.000} held at {3:+0.000;-0.000}: sigma_v x delta-z {4:+0.00;-0.00} pp, shift_gov {5:+0.00;-0.00} pp ({6}), shifted shares sum {7:R}, none negative or NaN",
                            r.BalchTerm, r.HeldTerm, r.BalchEy, r.HeldEy, 100.0 * r.Uncapped, 100.0 * r.GovernmentShift, binds ? "the cap binds" : "inside the cap", sum));
                }
            });
        }

        // ============================================================ (5) zero at no change
        private static void ExactlyZero()
        {
            _sb.Append("    5. zero at no change\n");
            WithSwedenAfterOneDay((sim, se, took) =>
            {
                se.FiscalLedgerAccruing = null;   // no one-off to date, so BAL0 is the flow's alone
                float gdp = se.State.NominalGdp;
                se.FiscalVoteRecord = Tenured(se, Book(gdp, new[] { -0.02, -0.02, -0.02, -0.02 }, -0.02, 365, took));
                EconomicVote.RecordParts parts = EconomicVote.RecordPartsOverTerm(se, sim.CurrentDate);
                FiscalVote.Reading r = parts.FiscalReading;
                List<string> keys = se.ParliamentSeats.Keys.ToList();
                double[] shares = Shares(se, keys);
                Dictionary<string, double> withoutFiscal = new Dictionary<string, double>(parts.Economy);
                foreach (KeyValuePair<string, double> kv in parts.Approval) { withoutFiscal[kv.Key] = withoutFiscal.TryGetValue(kv.Key, out double e) ? e + kv.Value : kv.Value; }
                double[] withRecord = EconomicVote.ApplyRecordShift(keys, shares, parts.Total), without = EconomicVote.ApplyRecordShift(keys, shares, withoutFiscal);
                bool bits = withRecord.Length == without.Length && withRecord.Zip(without, (x, y) => BitConverter.DoubleToInt64Bits(x) == BitConverter.DoubleToInt64Bits(y)).All(same => same);
                Check(r.HasTerm && r.HasElectionYear && r.BalchTerm == 0.0 && r.BalchEy == 0.0 && r.Index == 0.0 && parts.Fiscal.Count > 0 && parts.Fiscal.Values.All(v => v == 0.0) && bits,
                    F("(5) a flat balance path at {0:+0.00;-0.00} % of GDP: both variables 0, delta-z 0, every fiscal part 0, the shifted shares the economy's and approval's alone to the bit", 100.0 * r.Bal1));
            });
        }

        // ============================================================ (7) the tenure gate (K63)
        private static void Tenure()
        {
            _sb.Append("    7. the tenure gate (K63)\n");
            WithSwedenAfterOneDay((sim, se, took) =>
            {
                float gdp = se.State.NominalGdp;
                // four closed years, the two oldest opened before the government took office; the open year under it
                se.FiscalVoteRecord = Tenured(se, Book(gdp, new[] { -0.08, -0.08, -0.02, -0.01 }, -0.01, 200, took.AddDays(-2 * SimulationManager.DaysPerTurn)));
                EconomicVote.RecordParts parts = EconomicVote.RecordPartsOverTerm(se, sim.CurrentDate);
                FiscalVote.Reading gated = parts.FiscalReading, all = FiscalVote.Judge(se, null);
                Check(gated.TenureFrom == took && gated.ClosedYears == 2 && gated.OutsideTenure == 2 && gated.TermFallback && gated.BalchTerm == gated.Bal1 - gated.Bal2 && gated.HasElectionYear && !gated.OpenOutsideTenure
                      && all.ClosedYears == 4 && !all.TermFallback && gated.Index != all.Index,
                    F("(7a) the record reads the tenure from the day the government took office ({0:yyyy-MM-dd}): of four closed years the two that opened before it are not counted, so BALCH_term is [BD06]'s two-year fallback {1:+0.000000;-0.000000} (ungated, the four-year form {2:+0.000000;-0.000000}) - {3}",
                        took, gated.BalchTerm, all.BalchTerm, FiscalVote.Describe(gated)));
                se.FiscalVoteRecord.OpenedOn = took.AddDays(-1);
                FiscalVote.Reading openBefore = FiscalVote.Judge(se);
                Check(openBefore.OpenOutsideTenure && !openBefore.HasElectionYear && double.IsNaN(openBefore.Bal0),
                    F("(7b) an open year that opened the day before the government took office is not read: no BAL0, no BALCH_ey - {0}", FiscalVote.Describe(openBefore)));

                // K101: a government of ANOTHER party formed in play today answers for none of its predecessor's years
                GovernmentRecord held = se.Government;
                string other = se.ParliamentSeats.Keys.First(p => p != held.PmParty);
                se.FiscalVoteRecord = Tenured(se, Book(gdp, new[] { -0.08, -0.06, -0.04, -0.02 }, -0.01, 200, sim.CurrentDate.AddDays(-4 * SimulationManager.DaysPerTurn - 1)));
                se.Government = GovernmentRecord.OfNamedCabinet(other, new[] { other }, new string[0], sim.CurrentDate, "planted (FiscalVoteDiagnostic): another party's government formed in play");
                EconomicVote.RecordParts formed = EconomicVote.RecordPartsOverTerm(se, sim.CurrentDate);
                FiscalVote.Reading f = formed.FiscalReading;
                FiscalVote.NoteDay(se, se.FiscalVoteRecord.OpenFlow, sim.CurrentDate);
                Check(EconomicVote.TookOffice(se) == sim.CurrentDate && f.TenureFrom == sim.CurrentDate && f.ClosedYears == 0 && f.OutsideTenure == 4 && f.OpenOutsideTenure && formed.Fiscal.Count == 0
                      && se.FiscalVoteRecord.TenureParty == other && se.FiscalVoteRecord.TenureFrom == sim.CurrentDate,
                    F("(7c) K101, a change of party: {0}'s government formed in play on {1:yyyy-MM-dd} answers for none of {2}'s four closed years nor the open year, and the day's step notes {0}'s tenure from that day - {3}", other, sim.CurrentDate, held.PmParty, FiscalVote.Describe(f)));

                // K101: the SAME party re-formed (a re-election's oath, a re-formation) answers for its own previous years
                se.Government = held;
                se.FiscalVoteRecord = Tenured(se, Book(gdp, new[] { -0.01, -0.01, -0.02, -0.02 }, -0.03, 200, took));
                se.FiscalVoteRecord.TenureParty = held.PmParty; se.FiscalVoteRecord.TenureFrom = took;
                se.Government = GovernmentRecord.OfNamedCabinet(held.PmParty, held.Cabinet, held.Support, sim.CurrentDate, "planted (FiscalVoteDiagnostic): the same party re-formed in play");
                EconomicVote.RecordParts again = EconomicVote.RecordPartsOverTerm(se, sim.CurrentDate);
                FiscalVote.Reading g = again.FiscalReading;
                FiscalVote.NoteDay(se, se.FiscalVoteRecord.OpenFlow, sim.CurrentDate);
                Check(EconomicVote.TookOffice(se) == sim.CurrentDate && took != sim.CurrentDate && g.TenureFrom == took && g.ClosedYears == 4 && !g.TermFallback && g.OutsideTenure == 0 && again.Fiscal.Count > 0
                      && se.FiscalVoteRecord.TenureFrom == took,
                    F("(7d) K101, a re-election: {0} re-formed in play on {1:yyyy-MM-dd} keeps its tenure from {2:yyyy-MM-dd}, so its own previous four years are read in [BD06]'s four-year form - {3}", held.PmParty, sim.CurrentDate, took, FiscalVote.Describe(g)));
                se.Government = held;
            });
        }

        // ============================================================ (8) the sourced pre-run years at every start (K62, K100, K98, K101)
        private static void SeedAtEveryStart()
        {
            _sb.Append("    8. the sourced pre-run years at every start (K62, K100's no-future rule, K98, K101)\n");
            foreach (CountryId id in (CountryId[])Enum.GetValues(typeof(CountryId)))
            {
                using (SimulationManager.EpochScope())
                {
                    WorldClock.ApplyStart(id);   // the start rule, as a played start opens its world
                    DateTime start = SimulationManager.EpochDate;
                    (SimulationManager sim, World world, GameObject go) = Open();
                    try
                    {
                        Country k = world.GetCountry(id);
                        FiscalVoteBook book = k.FiscalVoteRecord;
                        (int Year, int OpensYear, int OpensMonth, int OpensDay, double Percent)[] record = FiscalBalanceRecord.For(id);
                        // the diagnostic's own reading of K100: the newest four source years whose own year had CLOSED by the start, consecutive
                        var expected = new List<(DateTime Opens, DateTime Closes, int Year, double Percent)>();
                        foreach ((int Year, int OpensYear, int OpensMonth, int OpensDay, double Percent) row in record.OrderByDescending(row => row.Year))
                        {
                            DateTime opens = id == CountryId.USA ? new DateTime(row.Year - 1, 10, 1) : new DateTime(row.Year, 1, 1);
                            DateTime closes = id == CountryId.USA ? new DateTime(row.Year, 10, 1) : new DateTime(row.Year + 1, 1, 1);
                            if (closes > start) { continue; }
                            if (expected.Count == FiscalVoteBook.Kept || (expected.Count > 0 && expected[expected.Count - 1].Year != row.Year + 1)) { break; }
                            expected.Add((opens, closes, row.Year, row.Percent));
                        }
                        expected.Reverse();   // oldest first, as the book holds them
                        bool same = book != null && book.OpenedOn == start && book.OpenDays == 0 && book.Closed.Count == expected.Count;
                        for (int i = 0; same && i < expected.Count; i++)
                        {
                            FiscalVoteYear y = book.Closed[i];
                            same = y.OpenedOn == expected[i].Opens && y.ClosedOn == expected[i].Closes && y.ClosedOn <= start && y.SourceYear == expected[i].Year
                                   && y.NominalGdp == k.State.NominalGdp && Math.Abs((double)y.Balance / y.NominalGdp - expected[i].Percent / 100.0) <= 1e-6;
                        }
                        Check(same, F("(8a) K100, {0}, the start {1:yyyy-MM-dd}: the book opens on the start with the {2} newest source year(s) that had closed by it, none still open - {3}", id, start, book?.Closed.Count,
                            string.Join(", ", expected.Select(e => F("{0} [{1:yyyy-MM-dd}, {2:yyyy-MM-dd}) at {3:+0.0;-0.0} %", e.Year, e.Opens, e.Closes, e.Percent)))));

                        // K101: the governing party's tenure, as the governments of record show it, walked here on the record's own rows
                        DateTime? tenure = null;
                        string party = k.Government?.PmParty;
                        IReadOnlyList<WorldClock.GovernmentOfRecord> rows = WorldClock.Governments(id);
                        int at = rows.Select((row, i) => (row, i)).Where(x => x.row.From <= start && start < x.row.Until).Select(x => x.i).DefaultIfEmpty(-1).Last();
                        string PartyOf(string head) { int o = head.IndexOf(" (", StringComparison.Ordinal), c = o < 0 ? -1 : head.IndexOf(')', o); return o < 0 || c < 0 ? null : head.Substring(o + 2, c - o - 2).Trim(); }
                        if (at >= 0 && party != null && PartyOf(rows[at].Head) == party)
                        {
                            while (at > 0 && rows[at - 1].Until == rows[at].From && PartyOf(rows[at - 1].Head) == party) { at--; }
                            tenure = rows[at].From;
                        }
                        Check(book != null && book.TenureParty == party && (!tenure.HasValue || book.TenureFrom == tenure.Value) && FiscalVote.TenureFrom(k) == book.TenureFrom,
                            F("(8b) K101, {0}: the book carries {1}'s tenure from {2:yyyy-MM-dd} (the governments of record walked back over the same party's rows: {3})", id, party, book?.TenureFrom, tenure.HasValue ? F("{0:yyyy-MM-dd}", tenure.Value) : "no row of the party in force - the day TookOffice reads"));

                        // K98, K100, K105: the first count reads the record by the election's calendar year
                        DateTime pollingDay = DateTime.MinValue;
                        bool election = !WorldClock.NoElectionYet(id) && !WorldClock.GoverningModeOnly(id) && WorldClock.TryNextPollingDay(id, start, out pollingDay);
                        Days(sim, 1);
                        FirstCount(id, k, election ? pollingDay : start, election, F("{0}'s start ({1:yyyy-MM-dd})", id, start));
                    }
                    finally { Object.DestroyImmediate(go); }
                }
            }
        }

        /// <summary>K98, K100, K105 (the s-fisc review 2's H1): the first count's years, held against the diagnostic's own reading of the record - BAL0
        /// the election's calendar year E where the record holds it and the tenure covers it; BAL-1..BAL-4 the years E-1..E-4 in order, each closed by
        /// the election's day and opened within the tenure, a missing year ending the run; every year read older than BAL0's (none read twice).</summary>
        private static void FirstCount(CountryId id, Country k, DateTime electionDay, bool election, string where)
        {
            FiscalVote.ShiftOverTerm(k, electionDay, out FiscalVote.Reading r);
            (int Year, int OpensYear, int OpensMonth, int OpensDay, double Percent)[] record = FiscalBalanceRecord.For(id);
            DateTime? tenure = FiscalVote.TenureFrom(k);
            int e = electionDay.Year;
            var expect = new List<(int Year, double Ratio)>();
            for (int y = e - 1; y >= e - FiscalVoteBook.Kept; y--)
            {
                int at = Array.FindIndex(record, row => row.Year == y);
                if (at < 0) { break; }
                DateTime opens = id == CountryId.USA ? new DateTime(y - 1, 10, 1) : new DateTime(y, 1, 1);
                if (opens.AddYears(1) > electionDay || (tenure.HasValue && opens < tenure.Value)) { break; }
                expect.Add((y, record[at].Percent / 100.0));
            }
            int e0 = Array.FindIndex(record, row => row.Year == e);
            DateTime eOpens = id == CountryId.USA ? new DateTime(e - 1, 10, 1) : new DateTime(e, 1, 1);
            bool bal0Read = e0 >= 0 && (!tenure.HasValue || eOpens >= tenure.Value);
            int[] got = { r.Source1, r.Source2, r.Source3, r.Source4 };
            double[] bal = { r.Bal1, r.Bal2, r.Bal3, r.Bal4 };
            bool years = r.ClosedYears == expect.Count && r.SourcedYears == expect.Count;
            for (int i = 0; years && i < expect.Count; i++) { years = got[i] == expect[i].Year && bal[i] == expect[i].Ratio && got[i] < e; }
            bool zero = bal0Read ? r.Source0 == e && r.Bal0 == record[e0].Percent / 100.0 : r.Source0 == 0 && double.IsNaN(r.Bal0);
            bool form = r.HasTerm == (expect.Count >= 2) && r.TermFallback == (expect.Count is 2 or 3) && r.HasElectionYear == (bal0Read && expect.Count >= 1);
            Check(years && zero && form,
                F("(8c) K98 + K100 + K105, {0}, the count of {1:yyyy-MM-dd}: BAL0 {2}, BAL-1..BAL-{3} {4} - each older than BAL0's year, each closed by the count's day, within the tenure from {5:yyyy-MM-dd} ({6}) - {7}",
                    where, electionDay, bal0Read ? F("{0} at {1:+0.0;-0.0} %", e, record[e0].Percent) : F("none ({0} {1})", e, e0 < 0 ? "not published" : "opened before the tenure"),
                    expect.Count, expect.Count > 0 ? string.Join(", ", expect.Select(x => F("{0} at {1:+0.0;-0.0} %", x.Year, 100.0 * x.Ratio))) : "none",
                    tenure, expect.Count >= 4 ? "the four-year form" : expect.Count >= 2 ? "the two-year fallback" : "no term variable", FiscalVote.Describe(r)));
            if (election) { Check(r.HasTerm || r.HasElectionYear, F("(8d) {0}: the first count, on {1:yyyy-MM-dd}, carries a fiscal term read from the source alone - {2}", where, electionDay, FiscalVote.Describe(r))); }
            double? gameBal0 = FiscalVote.OpenRatio(k);
            _sb.Append(F("        (printed) {0}: the governing party {1}, tenure from {2:yyyy-MM-dd}; BAL0 the source's {3} against the game's own after one day {4} (the gap K98 keeps out of the vote)\n",
                where, k.Government?.PmParty, tenure, double.IsNaN(r.Bal0) ? "n/a" : F("{0:+0.000;-0.000} %", 100.0 * r.Bal0), gameBal0.HasValue ? F("{0:+0.000;-0.000} %", 100.0 * gameBal0.Value) : "n/a"));
        }

        /// <summary>The s-fisc review 2's H1: E1's reference worlds - the epoch on the eve of the vote of record, as `PresidentialReferenceWorld` opens
        /// them (the USA's 5 Nov 2024, Poland's first round of 18 May 2025) - judged for the vote, the same rule as every start.</summary>
        private static void EveWorlds()
        {
            _sb.Append("    8e. E1's reference worlds, on the eve of the vote of record\n");
            foreach (CountryId id in new[] { CountryId.USA, CountryId.Poland })
            {
                DateTime vote = PresidentialReferenceWorld.VoteOfRecord(id);
                using (SimulationManager.EpochScope())
                {
                    SimulationManager.SetEpoch(vote.AddDays(-1));
                    (SimulationManager sim, World world, GameObject go) = Open();
                    try { FirstCount(id, world.GetCountry(id), vote, true, F("{0}'s reference world (the eve of {1:yyyy-MM-dd})", id, vote)); }
                    finally { Object.DestroyImmediate(go); }
                }
            }
        }

        /// <summary>K101: a planted book takes the tenure the world's own book carries (the governing party and the day its tenure began).</summary>
        private static FiscalVoteBook Tenured(Country c, FiscalVoteBook planted)
        {
            planted.TenureParty = c.FiscalVoteRecord?.TenureParty;
            planted.TenureFrom = c.FiscalVoteRecord?.TenureFrom ?? default(DateTime);
            return planted;
        }

        // ============================================================ the harness's hands
        private static (SimulationManager, World, GameObject) Open()
        {
            SimulationRandom.Seed(777);
            EnergyMarket.ResetCalibration();
            World world = WorldFactory.CreateDefault();
            var go = new GameObject("FISCALVOTE");
            SimulationManager sim = go.AddComponent<SimulationManager>();
            sim.SetWorld(world);
            sim.PlayerCountryId = CountryId.Sweden;
            return (sim, world, go);
        }

        /// <summary>A fresh world stepped one day, so the government of record is stored and its responsibility vector reads; the body gets the day it took office.</summary>
        private static void WithSwedenAfterOneDay(Action<SimulationManager, Country, DateTime> body)
        {
            (SimulationManager sim, World world, GameObject go) = Open();
            try
            {
                Days(sim, 1);
                Country se = world.GetCountry(CountryId.Sweden);
                DateTime? took = FiscalVote.TenureFrom(se);   // K101: the governing party's tenure
                if (se.Government == null || !took.HasValue || EconomicVote.Responsibility(se, sim.CurrentDate).Count == 0) { Check(false, "Sweden carries no government or no responsibility vector after its first day"); return; }
                body(sim, se, took.Value);
            }
            finally { Object.DestroyImmediate(go); }
        }

        /// <summary><paramref name="n"/> days of the game, each boundary resolved with no policy.</summary>
        private static void Days(SimulationManager sim, int n)
        {
            var decisions = new Dictionary<CountryId, PolicyDecision>();
            foreach (Country k in sim.World.Countries) { decisions[k.Id] = PolicyDecision.None(); }
            for (int d = 0; d < n; d++) { if (sim.AdvanceDay()) { sim.AdvanceTurn(decisions); } }
        }

        /// <summary>A planted book: closed years at the given balance ratios (oldest first) at nominal GDP <paramref name="gdp"/>, the oldest opening on
        /// <paramref name="firstOpens"/> and each a game year long, and an open year opening after them whose flow annualises to <paramref name="open"/> of
        /// GDP over <paramref name="days"/> days.</summary>
        private static FiscalVoteBook Book(float gdp, double[] closed, double open, int days, DateTime firstOpens)
        {
            var book = new FiscalVoteBook { OpenFlow = (float)(open * gdp * days / SimulationManager.DaysPerTurn), OpenDays = days, OpenedOn = firstOpens.AddDays(closed.Length * SimulationManager.DaysPerTurn) };
            for (int i = 0; i < closed.Length; i++)
            {
                book.Closed.Add(new FiscalVoteYear { OpenedOn = firstOpens.AddDays(i * SimulationManager.DaysPerTurn), ClosedOn = firstOpens.AddDays((i + 1) * SimulationManager.DaysPerTurn), Balance = (float)(closed[i] * gdp), NominalGdp = gdp });
            }
            return book;
        }

        private static double Ratio(Country k, int i) => (double)k.FiscalVoteRecord.Closed[i].Balance / k.FiscalVoteRecord.Closed[i].NominalGdp;

        private static double[] Shares(Country c, List<string> keys)
        {
            double seats = keys.Sum(k => (double)c.ParliamentSeats[k]);
            return keys.Select(k => c.ParliamentSeats[k] / seats).ToArray();
        }

        private static bool SameBits(Dictionary<string, double> a, Dictionary<string, double> b)
            => a != null && b != null && a.Count == b.Count && a.All(kv => b.TryGetValue(kv.Key, out double v) && BitConverter.DoubleToInt64Bits(v) == BitConverter.DoubleToInt64Bits(kv.Value));

        private static bool SameBook(FiscalVoteBook a, FiscalVoteBook b)
            => a != null && b != null && a.OpenFlow == b.OpenFlow && a.OpenDays == b.OpenDays && a.OpenedOn == b.OpenedOn && a.TenureParty == b.TenureParty && a.TenureFrom == b.TenureFrom && a.FormationSeen == b.FormationSeen && a.Closed.Count == b.Closed.Count
               && a.Closed.Zip(b.Closed, (x, y) => x.ClosedOn == y.ClosedOn && x.OpenedOn == y.OpenedOn && x.Balance == y.Balance && x.NominalGdp == y.NominalGdp && x.SourceYear == y.SourceYear).All(same => same);

        private static void Check(bool ok, string what)
        {
            _sb.Append(ok ? "        ok   " : "        FAIL ").Append(what).Append('\n');
            if (!ok) { _failures++; Debug.LogError("FISCAL VOTE: FAIL - " + what); }
        }

        private static string F(string format, params object[] args) => string.Format(CultureInfo.InvariantCulture, format, args);
    }
}
