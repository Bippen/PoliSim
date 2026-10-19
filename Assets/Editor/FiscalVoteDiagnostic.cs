using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using PoliSim.Data;
using PoliSim.Elections;
using PoliSim.Persistence;
using PoliSim.Simulation;
using UnityEngine;
using Object = UnityEngine.Object;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// A-LOOP q13 (2026-10-19, `docs/specs/A_LOOP_PLAN.md` §4 (4)): <b>the fiscal term's pass criteria that an Editor harness can prove</b>, on Sweden (seed
    /// 777, Sweden the player's country, every boundary resolved with no policy). No approval is written here (the book is the vote's alone and no
    /// ledger audits it), so nothing the ATTRIB audit reads is touched.
    /// <list type="number">
    /// <item><b>The book on the play path</b>, across two boundaries: before the first day no book; each day the open year's flow is the fiscal period's
    /// accrual to date and its days are the debt ledger's days; at each boundary the newest closed year is the year the player reads (the fiscal report's
    /// `FiscalTurnReport.BudgetBalance`, the flow it stored plus its one-offs) with nominal GDP at the close (the debt ledger's `DebtAttribution.GdpAtClose`),
    /// and the open year empties; the record's fiscal term on the first boundary's next day is formed from one closed year (BALCH_ey only).</item>
    /// <item><b>The variables' definitions on a planted balance path</b> (a bare country): four closed years give BALCH_term = ½(BAL-1 + BAL-2) − ½(BAL-3 +
    /// BAL-4), [BD06] Appendix I's; two or three give its fallback BAL-1 − BAL-2; one gives none; BAL0 is the open year's flow annualised plus its
    /// one-offs to date, over nominal GDP; BALCH_ey = BAL0 − BAL-1; delta-z = beta_term × BALCH_term + beta_ey × BALCH_ey; shift_gov = sigma_v × delta-z.</item>
    /// <item><b>The term's size and its split</b> (Sweden after one day): shift_gov split by the responsibility vector, summing to it; the record is the
    /// economic vote plus the approval term (as q15 summed them) plus the fiscal term, per party, and every live count's record is that total. The plan's
    /// scale printed: one point of GDP over the term and one in the election year, at the fitted sigma_v.</item>
    /// <item><b>The bound</b>: a variable past ±`FiscalVote.SupportBound` is held at it, |shift_gov| ≤ sigma_v × (beta_term + beta_ey) × the bound, and the
    /// shifted shares sum to one, non-negative and finite.</item>
    /// <item><b>Zero at no change</b>: a flat balance path gives delta-z exactly 0, every fiscal part 0, and the shifted shares the economy's and approval's
    /// alone to the bit.</item>
    /// <item><b>The save and the clone</b>: a save cut after the boundary carries the book by content (format 46); the preview's clone carries its own copy.</item>
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
            _sb.Append(F("    beta_term {0} and beta_ey {1} (the expanded sample's {2} and {3} beside them); sigma_v {4:R} pp (n {5}, 95 % {6:F3} - {7:F3}); the bound +/- {8} of GDP\n",
                FiscalVote.BetaTerm, FiscalVote.BetaElectionYear, FiscalVote.ExpandedBetaTerm, FiscalVote.ExpandedBetaElectionYear, 100.0 * FiscalVote.SwingSpread,
                PoliSim.Elections.Generated.IncumbentVoteSwing.N, PoliSim.Elections.Generated.IncumbentVoteSwing.SigmaLowerPoints, PoliSim.Elections.Generated.IncumbentVoteSwing.SigmaUpperPoints, FiscalVote.SupportBound));
            try
            {
                PlayPath();
                Definitions();
                SizeAndSplit();
                Bound();
                ExactlyZero();
            }
            catch (Exception e) { Check(false, "the run threw " + e.GetType().Name + ": " + e.Message + "\n" + e.StackTrace); }
            Debug.Log(_sb.ToString());
            Debug.Log(_failures == 0
                ? "FISCAL VOTE: PASS - the book on the play path over two boundaries, the variables' definitions, the term's size and split, the bound, exactly zero at no change, the save and the clone."
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
                EconomicVote.RecordParts before = EconomicVote.RecordPartsOverTerm(se, sim.CurrentDate);
                Check(se.FiscalVoteRecord == null && before.Fiscal.Count == 0 && !before.FiscalReading.HasTerm && !before.FiscalReading.HasElectionYear,
                    "(1a) before the first day no book and no fiscal term - " + FiscalVote.Describe(before.FiscalReading));
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
                    var openBefore = world.Countries.ToDictionary(k => k.Id, k => k.FiscalVoteRecord.Closed.Count);
                    sim.AdvanceTurn(decisions);
                    crossed++;
                    foreach (Country k in world.Countries)
                    {
                        FiscalVoteBook b = k.FiscalVoteRecord;
                        FiscalTurnReport report = sim.GetLastFiscalReport(k.Id);
                        FiscalVoteYear newest = b.Closed.Count > 0 ? b.Closed[b.Closed.Count - 1] : null;
                        Check(newest != null && report != null && b.Closed.Count == Math.Min(FiscalVoteBook.Kept, openBefore[k.Id] + 1) && newest.ClosedOn == sim.CurrentDate
                              && newest.Balance == report.BudgetBalance && newest.NominalGdp == k.FiscalLedgerLastPeriod.GdpAtClose && b.OpenFlow == 0f && b.OpenDays == 0,
                            F("(1c) boundary {0} ({1:yyyy-MM-dd}) {2}: the closed year {3:R} bn is the year the player reads ({4:R} = the flow {5:R} + one-offs {6:R}) over nominal GDP {7:R} at the close; {8} closed year(s); the open year emptied",
                                crossed, sim.CurrentDate, k.Id, newest?.Balance, report?.BudgetBalance, report?.FlowBalance, report?.OneOffs, newest?.NominalGdp, b.Closed.Count));
                    }
                }
                Check(crossed == Boundaries && mirrored > 0, F("(1d) {0} of {1} boundaries crossed in {2} days; the open year mirrored on every country every day ({3} readings)", crossed, Boundaries, days, mirrored));
                Days(sim, 1);
                EconomicVote.RecordParts parts = EconomicVote.RecordPartsOverTerm(se, sim.CurrentDate);
                FiscalVote.Reading r = parts.FiscalReading;
                FiscalVoteBook book = se.FiscalVoteRecord;
                double bal1 = (double)book.Closed[book.Closed.Count - 1].Balance / book.Closed[book.Closed.Count - 1].NominalGdp;
                double bal2 = (double)book.Closed[book.Closed.Count - 2].Balance / book.Closed[book.Closed.Count - 2].NominalGdp;
                Check(r.ClosedYears == 2 && r.HasTerm && r.TermFallback && r.BalchTerm == bal1 - bal2 && r.HasElectionYear,
                    F("(1e) the day after the second boundary Sweden's record carries the fiscal term on two closed years ([BD06]'s fallback, BAL-1 {0:+0.0000;-0.0000} less BAL-2 {1:+0.0000;-0.0000}) and one open day (whether a governing party carries it is printed, not required: no count is held here) - {2}", bal1, bal2, FiscalVote.Describe(r)));
                _sb.Append(F("        (printed) the no-policy balance path: {0}\n", string.Join("; ", world.Countries.Select(k =>
                {
                    FiscalVote.Reading kr = FiscalVote.Judge(k);
                    return F("{0} BAL-2 {1:+0.000;-0.000} BAL-1 {2:+0.000;-0.000} pp of GDP, term {3:+0.000;-0.000}, index {4:+0.0000;-0.0000} -> {5:+0.00;-0.00} pp", k.Id, 100.0 * kr.Bal2, 100.0 * kr.Bal1, 100.0 * kr.BalchTerm, kr.Index, 100.0 * kr.GovernmentShift);
                }))));

                _sb.Append("    6. the save and the clone\n");
                string json = SaveGameService.Serialize(SaveGameService.CreateSaveGame(sim, world, CountryId.Sweden, null));
                SaveGame cut = SaveGameService.Deserialize(json);
                Country saved = cut.World.GetCountry(CountryId.Sweden);
                Check(cut.SaveVersion == SaveGameService.CurrentSaveVersion && cut.SaveVersion >= 46 && SameBook(saved.FiscalVoteRecord, book),
                    F("(6a) a save cut on {0:yyyy-MM-dd} carries format {1} and the book by content ({2} closed year(s), the open year {3:R} over {4} day(s))", sim.CurrentDate, cut.SaveVersion, saved.FiscalVoteRecord?.Closed.Count, saved.FiscalVoteRecord?.OpenFlow, saved.FiscalVoteRecord?.OpenDays));
                MethodInfo clone = typeof(SimulationManager).GetMethod("ClonePreviewCountry", BindingFlags.NonPublic | BindingFlags.Static);
                var copy = clone?.Invoke(null, new object[] { se }) as Country;
                Check(copy != null && SameBook(copy.FiscalVoteRecord, book) && !ReferenceEquals(copy.FiscalVoteRecord, book) && !ReferenceEquals(copy.FiscalVoteRecord.Closed, book.Closed),
                    "(6b) the preview's clone (ClonePreviewCountry) carries the book as its own copy");
            }
            finally { Object.DestroyImmediate(go); }
        }

        // ============================================================ (2) the variables' definitions, on a planted balance path
        private static void Definitions()
        {
            _sb.Append("    2. the variables' definitions on a planted balance path (a bare country)\n");
            SimulationRandom.Seed(777);
            Country k = WorldFactory.CreateDefault().GetCountry(CountryId.Sweden);   // no simulation runs on it
            float gdp = k.State.NominalGdp;
            k.FiscalLedgerAccruing = null;
            // oldest first: BAL-4 -1 %, BAL-3 -2 %, BAL-2 -3 %, BAL-1 -4 % of GDP; the open year -6 % annualised over 100 days
            k.FiscalVoteRecord = Book(gdp, new[] { -0.01, -0.02, -0.03, -0.04 }, -0.06, 100);
            FiscalVote.Reading four = FiscalVote.Judge(k);
            double b1 = Ratio(k, 3), b2 = Ratio(k, 2), b3 = Ratio(k, 1), b4 = Ratio(k, 0);
            double term = 0.5 * (b1 + b2) - 0.5 * (b3 + b4);
            double bal0 = (double)k.FiscalVoteRecord.OpenFlow * SimulationManager.DaysPerTurn / 100 / gdp;
            Check(four.ClosedYears == 4 && four.HasTerm && !four.TermFallback && four.BalchTerm == term && Math.Abs(term + 0.02) <= 1e-6,
                F("(2a) four closed years: BALCH_term = 1/2 (BAL-1 + BAL-2) - 1/2 (BAL-3 + BAL-4) = {0:+0.000000;-0.000000} (the path's -0.02)", four.BalchTerm));
            Check(four.HasElectionYear && four.Bal0 == bal0 && four.BalchEy == bal0 - b1 && Math.Abs(four.BalchEy + 0.02) <= 1e-6,
                F("(2b) BAL0 = the open year's flow x {0} / 100 days over nominal GDP = {1:+0.000000;-0.000000}; BALCH_ey = BAL0 - BAL-1 = {2:+0.000000;-0.000000}", SimulationManager.DaysPerTurn, four.Bal0, four.BalchEy));
            double index = FiscalVote.BetaTerm * four.BalchTerm + FiscalVote.BetaElectionYear * four.BalchEy;
            Check(four.Index == index && four.GovernmentShift == FiscalVote.SwingSpread * index && four.HeldTerm == four.BalchTerm && four.HeldEy == four.BalchEy,
                F("(2c) delta-z = {0} x BALCH_term + {1} x BALCH_ey = {2:+0.000000;-0.000000}; shift_gov = sigma_v x delta-z = {3:+0.0000;-0.0000} pp; inside the bound nothing is held", FiscalVote.BetaTerm, FiscalVote.BetaElectionYear, four.Index, 100.0 * four.GovernmentShift));

            k.FiscalVoteRecord = Book(gdp, new[] { -0.03, -0.04 }, -0.06, 100);
            FiscalVote.Reading two = FiscalVote.Judge(k);
            k.FiscalVoteRecord = Book(gdp, new[] { -0.02, -0.03, -0.04 }, -0.06, 100);
            FiscalVote.Reading three = FiscalVote.Judge(k);
            k.FiscalVoteRecord = Book(gdp, new[] { -0.04 }, -0.06, 100);
            FiscalVote.Reading one = FiscalVote.Judge(k);
            k.FiscalVoteRecord = Book(gdp, new double[0], -0.06, 100);
            FiscalVote.Reading none = FiscalVote.Judge(k);
            Check(two.HasTerm && two.TermFallback && two.BalchTerm == two.Bal1 - two.Bal2 && three.TermFallback && three.BalchTerm == three.Bal1 - three.Bal2
                  && !one.HasTerm && one.HasElectionYear && !none.HasTerm && !none.HasElectionYear && none.Index == 0.0,
                F("(2d) two or three closed years: [BD06]'s fallback BAL-1 - BAL-2 ({0:+0.000000;-0.000000}); one: no term variable, BALCH_ey {1:+0.000000;-0.000000}; none: no term - {2}", two.BalchTerm, one.BalchEy, FiscalVote.Describe(none)));

            // the open year's one-offs to date count unscaled (declared reading 2): a cost of 1 % of GDP landed on the debt
            k.FiscalVoteRecord = Book(gdp, new[] { -0.04 }, -0.06, 100);
            k.FiscalLedgerAccruing = new DebtAttribution();
            k.FiscalLedgerAccruing.Events.Add(new DebtEventRecord { Date = new DateTime(2026, 1, 1), Label = "planted (FiscalVoteDiagnostic): a one-off", AppliedDelta = 0.01f * gdp });
            FiscalVote.Reading withOneOff = FiscalVote.Judge(k);
            double expected0 = ((double)k.FiscalVoteRecord.OpenFlow * SimulationManager.DaysPerTurn / 100 - k.FiscalLedgerAccruing.Events[0].AppliedDelta) / gdp;   // the stored field, never a float intermediate
            Check(withOneOff.Bal0 == expected0 && Math.Abs(withOneOff.Bal0 - (bal0 - 0.01)) <= 1e-6,
                F("(2e) a one-off cost of 1 % of GDP to date lowers BAL0 by 1 point, unannualised: {0:+0.000000;-0.000000} against {1:+0.000000;-0.000000}", withOneOff.Bal0, bal0));
            k.FiscalVoteRecord.OpenDays = 0;
            Check(!FiscalVote.Judge(k).HasElectionYear, "(2f) with no day of the open year accrued there is no BAL0 and no BALCH_ey");
            k.FiscalLedgerAccruing = null;
        }

        // ============================================================ (3) the term's size and its split
        private static void SizeAndSplit()
        {
            _sb.Append("    3. the term's size and its split\n");
            WithSwedenAfterOneDay((sim, se) =>
            {
                float gdp = se.State.NominalGdp;
                se.FiscalVoteRecord = Book(gdp, new[] { -0.01, -0.01, -0.02, -0.02 }, -0.03, 200);   // a deficit a point worse over the term and again in the election year
                EconomicVote.RecordParts parts = EconomicVote.RecordPartsOverTerm(se, sim.CurrentDate);
                FiscalVote.Reading r = parts.FiscalReading;
                Dictionary<string, double> responsibility = EconomicVote.Responsibility(se, sim.CurrentDate);
                bool split = responsibility.Count > 0 && parts.Fiscal.Count == responsibility.Count
                             && responsibility.All(kv => parts.Fiscal.TryGetValue(kv.Key, out double f) && f == r.GovernmentShift * kv.Value);
                Check(r.Carried && split && Math.Abs(parts.Fiscal.Values.Sum() - r.GovernmentShift) <= Exact && r.GovernmentShift < 0.0,
                    F("(3a) shift_gov {0:+0.0000;-0.0000} pp split by responsibility, summing to it: {1}", 100.0 * r.GovernmentShift,
                        string.Join(", ", parts.Fiscal.Select(kv => F("{0} {1:+0.0000;-0.0000} pp", kv.Key, 100.0 * kv.Value)))));
                bool sums = parts.Total.Count == parts.Economy.Count && parts.Economy.All(kv => parts.Total.TryGetValue(kv.Key, out double t)
                    && t == (kv.Value + (parts.Approval.TryGetValue(kv.Key, out double a) ? a : 0.0)) + (parts.Fiscal.TryGetValue(kv.Key, out double f) ? f : 0.0));
                Dictionary<string, double> live = EconomicVote.RecordOverTerm(se, sim.CurrentDate, out _);
                Check(sums && SameBits(live, parts.Total),
                    "(3b) the record is the economic vote plus the approval term (as q15 summed them) plus the fiscal term, per party, and every live count's record (EconomicVote.RecordOverTerm) is that total");
                double s = FiscalVote.SwingSpread;
                _sb.Append(F("        (printed) the plan's scale at sigma_v {0:F3} pp: one point of GDP better over the term is worth {1:+0.000;-0.000} pp of the government's share, one in the election year {2:+0.000;-0.000} pp (the expanded sample's: {3:+0.000;-0.000} and {4:+0.000;-0.000})\n",
                    100.0 * s, 100.0 * s * FiscalVote.BetaTerm * 0.01, 100.0 * s * FiscalVote.BetaElectionYear * 0.01, 100.0 * s * FiscalVote.ExpandedBetaTerm * 0.01, 100.0 * s * FiscalVote.ExpandedBetaElectionYear * 0.01));
            });
        }

        // ============================================================ (4) the bound
        private static void Bound()
        {
            _sb.Append("    4. the bound\n");
            WithSwedenAfterOneDay((sim, se) =>
            {
                float gdp = se.State.NominalGdp;
                List<string> keys = se.ParliamentSeats.Keys.ToList();
                double cap = FiscalVote.SwingSpread * (FiscalVote.BetaTerm + FiscalVote.BetaElectionYear) * FiscalVote.SupportBound;
                foreach ((double[] years, double open) in new[] { (new[] { 0.0, 0.0, -0.30, -0.30 }, -0.60), (new[] { -0.30, -0.30, 0.10, 0.10 }, 0.40), (new[] { 0.0, 0.0, -0.05, -0.05 }, -0.08) })
                {
                    se.FiscalVoteRecord = Book(gdp, years, open, 365);
                    EconomicVote.RecordParts parts = EconomicVote.RecordPartsOverTerm(se, sim.CurrentDate);
                    FiscalVote.Reading r = parts.FiscalReading;
                    double[] after = EconomicVote.ApplyRecordShift(keys, Shares(se, keys), parts.Total);
                    double sum = after.Sum();
                    bool held = r.HeldTerm == FiscalVote.Held(r.BalchTerm) && r.HeldEy == FiscalVote.Held(r.BalchEy) && Math.Abs(r.HeldTerm) <= FiscalVote.SupportBound && Math.Abs(r.HeldEy) <= FiscalVote.SupportBound;
                    bool sane = after.All(x => x >= 0.0 && !double.IsNaN(x) && !double.IsInfinity(x)) && Math.Abs(sum - 1.0) <= 1e-9;
                    Check(held && Math.Abs(r.GovernmentShift) <= cap + Exact && sane,
                        F("(4) BALCH_term {0:+0.000;-0.000} held at {1:+0.000;-0.000}, BALCH_ey {2:+0.000;-0.000} held at {3:+0.000;-0.000}: shift_gov {4:+0.00;-0.00} pp (|.| <= {5:0.00}), shifted shares sum {6:R}, none negative or NaN",
                            r.BalchTerm, r.HeldTerm, r.BalchEy, r.HeldEy, 100.0 * r.GovernmentShift, 100.0 * cap, sum));
                }
            });
        }

        // ============================================================ (5) zero at no change
        private static void ExactlyZero()
        {
            _sb.Append("    5. zero at no change\n");
            WithSwedenAfterOneDay((sim, se) =>
            {
                se.FiscalLedgerAccruing = null;   // no one-off to date, so BAL0 is the flow's alone
                float gdp = se.State.NominalGdp;
                se.FiscalVoteRecord = Book(gdp, new[] { -0.02, -0.02, -0.02, -0.02 }, -0.02, 365);
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

        /// <summary>A fresh world stepped one day, so the government of record is stored and its responsibility vector reads.</summary>
        private static void WithSwedenAfterOneDay(Action<SimulationManager, Country> body)
        {
            (SimulationManager sim, World world, GameObject go) = Open();
            try
            {
                Days(sim, 1);
                Country se = world.GetCountry(CountryId.Sweden);
                if (se.Government == null || EconomicVote.Responsibility(se, sim.CurrentDate).Count == 0) { Check(false, "Sweden carries no government or no responsibility vector after its first day"); return; }
                body(sim, se);
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

        /// <summary>A planted book: closed years at the given balance ratios (oldest first) at nominal GDP <paramref name="gdp"/>, and an open year
        /// whose flow annualises to <paramref name="open"/> of GDP over <paramref name="days"/> days.</summary>
        private static FiscalVoteBook Book(float gdp, double[] closed, double open, int days)
        {
            var book = new FiscalVoteBook { OpenFlow = (float)(open * gdp * days / SimulationManager.DaysPerTurn), OpenDays = days };
            for (int i = 0; i < closed.Length; i++) { book.Closed.Add(new FiscalVoteYear { ClosedOn = new DateTime(2020 + i, 1, 1), Balance = (float)(closed[i] * gdp), NominalGdp = gdp }); }
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
            => a != null && b != null && a.OpenFlow == b.OpenFlow && a.OpenDays == b.OpenDays && a.Closed.Count == b.Closed.Count
               && a.Closed.Zip(b.Closed, (x, y) => x.ClosedOn == y.ClosedOn && x.Balance == y.Balance && x.NominalGdp == y.NominalGdp).All(same => same);

        private static void Check(bool ok, string what)
        {
            _sb.Append(ok ? "        ok   " : "        FAIL ").Append(what).Append('\n');
            if (!ok) { _failures++; Debug.LogError("FISCAL VOTE: FAIL - " + what); }
        }

        private static string F(string format, params object[] args) => string.Format(CultureInfo.InvariantCulture, format, args);
    }
}
