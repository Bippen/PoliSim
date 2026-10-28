using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using PoliSim.Data;
using PoliSim.Elections;
using PoliSim.Simulation;
using UnityEngine;
using Object = UnityEngine.Object;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// MF-13 (Elias's rulings K134-K135 and K191, 2026-10-27; J1/Q3; `docs/specs/A_LOOP_PLAN.md` §3): <b>approval's reversion on the daily step at the
    /// sourced persistence, its sustained terms at their old equilibria, no step at the turn boundary.</b> Sections a-d drive approval's daily step
    /// (`MacroSystem.ApplyApprovalDay`) by itself on bare countries of the seeded world (seed 777) - nothing else writes approval and no period closes,
    /// so no ledger is audited; section e plays a world.
    /// <list type="number">
    /// <item><b>(a) The persistence</b> (K134, K191): the tax term's figure is 0.81 a quarter; approval keeps one less its daily speed of a gap a day, which
    /// is `TaxTerm.DailyPersistence` - 0.81 to the power 1 ⁄ `TaxTerm.DaysPerQuarter` - so a quarter's days keep 0.81 and a 365-day turn 0.81⁴; the
    /// economy stock carries the same (`ApprovalVote.EconomyStockPersistence`).</item>
    /// <item><b>(b) The equilibria</b> (K135): every sustained term's authored figure is the one it was before MF-13 (listed below, independently of the
    /// scale the model applies) and its size a day ÷ the daily speed is that figure ÷ 0.05, the once-a-turn speed approval ran at before MF-13; and,
    /// run live on every country until approval settles, a planted gap (Gini, growth, crime, paid leave, drug policy) settles approval where the old
    /// figure's equilibrium puts it - R-Q1's Gini term at its ruled 1.0 point per Gini point.</item>
    /// <item><b>(c) The one-off shock</b>, measured on EVERY day (not only at turn boundaries, the MF-13 review's M-4): a planted shock keeps
    /// `TaxTerm.DailyPersistence` to the power of the days since of itself on each of 400 days, and falls to half on the day the tax term's half-life
    /// (`TaxTerm.HalfLifeDays`, ln 0.5 ⁄ ln 0.81 quarters) names.</item>
    /// <item><b>(d) The one-off terms keep their size</b> (K135's "only one-off shocks fade faster"): the tax term's s and the spending change's sensitivity
    /// are their authored figures, unscaled.</item>
    /// <item><b>(e) No step at the turn boundary</b> (K191): a world played across two boundaries - the boundary records no reversion and no sustained
    /// term (they ran on the days), and what the boundary moved approval by beyond the turn's one-off terms and its events is no larger than the
    /// largest single day's move of the thirty days before it; and (e2, review 2's N-5) the growth input's unit at the real call site: each closed
    /// period's growth term, read back as the mean growth it was paid on, matches the period's realised growth annualised over its days - a call
    /// site passing the day's raw growth would read 365 times too small. (b2) holds the step's own reading of the unit (+1 point of ANNUAL growth
    /// settles +6.000), (e2) that the game hands it annual growth.</item>
    /// <item><b>(f) A mid-period writer lies inside the ledger's window</b> (the integration finding of 2026-10-28): the daily step opens the period's ledger on
    /// its first day, so a law's enactment cost written mid-period opens the audit identity's gap by exactly that cost until it is recorded.</item>
    /// </list>
    /// Prints `APPROVAL REVERSION: PASS` and exits 0 when every check holds; each failure is an `APPROVAL REVERSION:` error naming its check.
    /// Run: `Unity.exe -batchmode -nographics -projectPath &lt;path&gt; -executeMethod PoliSim.EditorTools.ApprovalReversionDiagnostic.Run -logFile &lt;path&gt;`.
    /// </summary>
    public static class ApprovalReversionDiagnostic
    {
        /// <summary>The speed approval reverted at before MF-13, per turn, at the boundary - the unit the sustained terms' authored figures were set in.</summary>
        private const double PreMf13Speed = 0.05;

        /// <summary>The sourced persistence a quarter (K134; [LBS00] Table 2, cited at `TaxTerm.QuarterlyPersistence`).</summary>
        private const double SourcedQuarterly = 0.81;

        /// <summary>Days a planted gap is run for: the daily persistence to that power is under 1e-8, nothing a float can see.</summary>
        private const int SettleDays = 8000;

        /// <summary>The bar on a live equilibrium: approval is a float, whose step near 50 is 3.8e-6, and the daily step stops moving it once its move is
        /// under half that step - within about 3.8e-6 ⁄ 2 ⁄ 0.0023 ≈ 8e-4 of the equilibrium. Two settled runs differ by at most twice that.</summary>
        private const double SettledTolerance = 5e-3;

        /// <summary>The days a one-off shock is followed for, past its half-life.</summary>
        private const int ShockDays = 400;

        /// <summary>The sustained terms and their authored figures as they stood before MF-13 (the tree at c34f1099), by the constant that carries each.
        /// Each one's equilibrium, figure ÷ 0.05, is what K135 keeps.</summary>
        private static readonly (Type Owner, string Name, double PreFigure)[] Sustained =
        {
            (typeof(MacroSystem), "GrowthApprovalSensitivity", 0.3),
            (typeof(MacroSystem), "UnemploymentApprovalSensitivity", 0.4),
            (typeof(MacroSystem), "InflationApprovalSensitivity", 0.4),
            (typeof(MacroSystem), "CrimeApprovalSensitivity", 0.2),
            (typeof(MacroSystem), "CorruptionApprovalSensitivity", 0.15),
            (typeof(MacroSystem), "GiniApprovalSensitivity", 0.05),
            (typeof(MacroSystem), "UbiApprovalSensitivity", 3.0),
            (typeof(MacroSystem), "NegativeIncomeTaxApprovalSensitivity", 2.0),
            (typeof(MacroSystem), "MeansTestedWelfareApprovalSensitivity", 1.5),
            (typeof(MacroSystem), "UniversalHealthcareApprovalSensitivity", 3.0),
            (typeof(MacroSystem), "HousingAssistanceApprovalSensitivity", 1.5),
            (typeof(MacroSystem), "ChildcareSubsidiesApprovalSensitivity", 1.5),
            (typeof(LaborCouplings), "PaidFamilyLeaveApprovalSensitivity", 0.05),
            (typeof(CrimeJusticeCouplings), "DrugPolicyApprovalSensitivity", 0.02),
        };

        /// <summary>The one-off terms, which K135 leaves at their authored size.</summary>
        private static readonly (Type Owner, string Name, double Figure)[] OneOff =
        {
            (typeof(MacroSystem), "TaxHikeApprovalSensitivity", 1.5),
            (typeof(MacroSystem), "SpendingApprovalSensitivity", 0.8),
            (typeof(MacroSystem), "MandatorySpendingApprovalMultiplier", 3.0),
        };

        private static int _failures;
        private static StringBuilder _sb;

        public static void Run()
        {
            CheckExit.ArmLogFold();   // section e plays a world: an ATTRIB during it fails the run
            _failures = 0;
            _sb = new StringBuilder("APPROVAL REVERSION: MF-13 - approval's daily reversion at 0.81 a quarter, the sustained terms at their old equilibria, no step at the boundary (seed 777)\n");
            try
            {
                double speed = Persistence();
                Equilibria(speed);
                OneOffShock();
                OneOffTerms();
                NoBoundaryStep();
                MidPeriodWriter();
            }
            catch (Exception e) { Check(false, "the run threw " + e.GetType().Name + ": " + e.Message + "\n" + e.StackTrace); }
            Debug.Log(_sb.ToString());
            Debug.Log(_failures == 0
                ? "APPROVAL REVERSION: PASS - the persistence 0.81 a quarter on the daily step, every sustained term at its old equilibrium (by figure and live on every country), a one-off shock fading at the daily persistence on every day with the tax term's half-life, the one-off terms unscaled, no step at the turn boundary, a mid-period writer inside the ledger's window."
                : F("APPROVAL REVERSION: FAIL - {0} check(s) failed.", _failures));
            CheckExit.Finish(_failures == 0 ? 0 : 1);
        }

        // ============================================================ (a) the persistence
        private static double Persistence()
        {
            _sb.Append("    a. the persistence (K134, on the daily step K191)\n");
            double speed = Value(typeof(MacroSystem), "ApprovalReversionSpeed");
            double perDay = 1.0 - speed;
            double sourcedDaily = Math.Pow(SourcedQuarterly, 1.0 / TaxTerm.DaysPerQuarter);
            Check(TaxTerm.QuarterlyPersistence == (float)SourcedQuarterly && SimulationManager.DaysPerTurn / (double)TaxTerm.DaysPerQuarter == 4.0,
                F("(a1) the tax term's persistence {0} a quarter (K134's figure); a quarter is {1} days, a turn of {2} days four of them", TaxTerm.QuarterlyPersistence, TaxTerm.DaysPerQuarter, SimulationManager.DaysPerTurn));
            Check((float)perDay == TaxTerm.DailyPersistence && Math.Abs(perDay - sourcedDaily) <= 1e-6,
                F("(a2) approval keeps {0:R} of a gap a day (speed {1:R}): the tax term's daily persistence {2:R}; 0.81 to the power 1/{3} = {4:R}", perDay, speed, TaxTerm.DailyPersistence, TaxTerm.DaysPerQuarter, sourcedDaily));
            double quarter = Math.Pow(perDay, TaxTerm.DaysPerQuarter), turn = Math.Pow(perDay, SimulationManager.DaysPerTurn);
            Check(Math.Abs(quarter - SourcedQuarterly) <= 1e-4 && Math.Abs(turn - Math.Pow(SourcedQuarterly, 4.0)) <= 1e-4,
                F("(a3) over a quarter's days approval keeps {0:F5} of a gap (0.81); over a turn's {1:F5} (0.81 to the fourth {2:F5}; before MF-13 0.95, in one step at the boundary)", quarter, turn, Math.Pow(SourcedQuarterly, 4.0)));
            Check(ApprovalVote.EconomyStockPersistence == (float)perDay,
                F("(a4) the economy stock carries the same persistence a day: {0:R}", ApprovalVote.EconomyStockPersistence));
            return speed;
        }

        // ============================================================ (b) the equilibria
        private static void Equilibria(double speed)
        {
            _sb.Append("    b. every sustained term's equilibrium as before MF-13 (K135)\n");
            double scale = Value(typeof(MacroSystem), "SustainedApprovalTermScale");
            Check(Math.Abs(scale / speed - 1.0 / PreMf13Speed) <= 1e-5 * (1.0 / PreMf13Speed),
                F("(b0) the scale a day {0:R} over the daily speed {1:R} is {2:F5} - one over the pre-MF-13 speed, 20", scale, speed, scale / speed));
            foreach ((Type owner, string name, double pre) in Sustained)
            {
                double figure = Value(owner, name);
                double was = pre / PreMf13Speed, now = figure * scale / speed;
                Check(figure == (float)pre && Math.Abs(now - was) <= 1e-5 * Math.Max(1.0, Math.Abs(was)),
                    F("(b1) {0}.{1}: authored {2:R} (before MF-13 {3}), {4:R} a day, equilibrium {5:F5} points per unit of gap (before MF-13 {6:F5})", owner.Name, name, figure, pre, figure * scale, now, was));
            }

            // Live: each planted gap, run on every country until approval settles, against the same country with no gap. A country whose approval
            // settles at a bound is no measurement and is named; Sweden must be measured.
            World world = SeededWorld();
            var probes = new (string What, double Expected, Action<Country> Plant, Action<Country> Undo, float GrowthGap)[]
            {
                ("Gini +2 points above its baseline (R-Q1: 1.0 a Gini point, ruled band 0.5-1.5)", -0.05 / PreMf13Speed * 2.0, c => c.State.Gini += 2f, c => c.State.Gini -= 2f, 0f),
                ("growth 1 point above potential", 0.3 / PreMf13Speed * 1.0, c => { }, c => { }, 1f),
                ("crime 2 points above its baseline", -0.2 / PreMf13Speed * 2.0, c => c.State.CrimeIndex += 2f, c => c.State.CrimeIndex -= 2f, 0f),
                ("paid leave 2 weeks above its baseline", 0.05 / PreMf13Speed * 2.0, c => c.PaidFamilyLeaveWeeks += 2f, c => c.PaidFamilyLeaveWeeks -= 2f, 0f),
                ("drug policy 5 dial points stricter", 0.02 / PreMf13Speed * 5.0, c => c.DrugPolicyLevel += 5f, c => c.DrugPolicyLevel -= 5f, 0f),
            };
            foreach (var probe in probes)
            {
                var clamped = new List<string>();
                double worst = 0.0;
                bool swedenMeasured = false;
                foreach (Country c in world.Countries)
                {
                    float start = c.State.ApprovalRating, stock = c.TaxTermStock, economyStock = c.ApprovalEconomyStock;
                    float none = Settle(c, start, 0f, SettleDays);
                    probe.Plant(c);
                    float with = Settle(c, start, probe.GrowthGap, SettleDays);
                    probe.Undo(c);
                    c.State.ApprovalRating = start;
                    c.TaxTermStock = stock;
                    c.ApprovalEconomyStock = economyStock;
                    if (none <= 0.5f || none >= 99.5f || with <= 0.5f || with >= 99.5f) { clamped.Add(F("{0} ({1:F2} / {2:F2})", c.Id, none, with)); continue; }
                    worst = Math.Max(worst, Math.Abs((with - none) - probe.Expected));
                    if (c.Id == CountryId.Sweden) { swedenMeasured = true; }
                }
                Check(swedenMeasured && worst <= SettledTolerance,
                    F("(b2) {0}: approval settles {1:+0.000;-0.000} off the run with no gap after {2} days, the old figure's equilibrium, on every measured country (worst miss {3:E2}){4}",
                        probe.What, probe.Expected, SettleDays, worst, clamped.Count > 0 ? "; settled at a bound, not measured: " + string.Join(", ", clamped) : ""));
            }
        }

        // ============================================================ (c) a one-off shock, every day
        private static void OneOffShock()
        {
            _sb.Append("    c. a one-off shock fades at 0.81 a quarter, measured on every day\n");
            Country se = SeededWorld().GetCountry(CountryId.Sweden);
            float settled = Settle(se, se.State.ApprovalRating, 0f, SettleDays);
            float shock = settled < 50f ? 10f : -10f;
            float shocked = settled + shock, quiet = settled;
            double worst = 0.0;
            int halfDay = -1, worstDay = 0;
            for (int d = 1; d <= ShockDays; d++)
            {
                shocked = Settle(se, shocked, 0f, 1);
                quiet = Settle(se, quiet, 0f, 1);
                double kept = (shocked - quiet) / shock, want = Math.Pow(TaxTerm.DailyPersistence, d);
                if (Math.Abs(kept - want) > worst) { worst = Math.Abs(kept - want); worstDay = d; }
                if (halfDay < 0 && kept <= 0.5) { halfDay = d; }
            }
            Check(worst <= 1e-3,
                F("(c1) Sweden settled at {0:F3}, a shock of {1:+0;-0}: on each of {2} days the share standing is the daily persistence to the power of the days since, within {3:E2} (worst on day {4}, {5} a turn boundary)",
                    settled, shock, ShockDays, worst, worstDay, worstDay % SimulationManager.DaysPerTurn == 0 ? "on" : "not"));
            Check(halfDay > 0 && Math.Abs(halfDay - TaxTerm.HalfLifeDays) <= 1.0,
                F("(c2) the shock first stands at half or less on day {0}; the tax term's half-life {1:F1} days, ln 0.5 / ln 0.81 quarters (before MF-13: {2:F0} days, in yearly steps)",
                    halfDay, TaxTerm.HalfLifeDays, Math.Log(0.5) / Math.Log(1.0 - PreMf13Speed) * SimulationManager.DaysPerTurn));
        }

        // ============================================================ (d) the one-off terms
        private static void OneOffTerms()
        {
            _sb.Append("    d. the one-off terms keep their authored size\n");
            foreach ((Type owner, string name, double figure) in OneOff)
            {
                double live = Value(owner, name);
                Check(live == (float)figure, F("(d) {0}.{1} = {2:R}, the authored {3}, unscaled", owner.Name, name, live, figure));
            }
        }

        // ============================================================ (e) no step at the turn boundary
        private static void NoBoundaryStep()
        {
            _sb.Append("    e. no step at the turn boundary (K191), a world played across two boundaries\n");
            SimulationRandom.Seed(777);
            EnergyMarket.ResetCalibration();
            World world = WorldFactory.CreateDefault();
            var go = new GameObject("APPROVALREVERSION");
            try
            {
                SimulationManager sim = go.AddComponent<SimulationManager>();
                sim.SetWorld(world);
                sim.PlayerCountryId = CountryId.Sweden;
                var decisions = new Dictionary<CountryId, PolicyDecision>();
                foreach (Country k in world.Countries) { decisions[k.Id] = PolicyDecision.None(); }
                var recent = new Dictionary<CountryId, Queue<float>>();
                foreach (Country k in world.Countries) { recent[k.Id] = new Queue<float>(); }
                // N-5 (review 2): the growth input's unit - per country, GDP at the period's open, the potential rate summed over the period's days
                var gdpOpen = world.Countries.ToDictionary(k => k.Id, k => k.State.GDP + k.LapseOutputWedge);   // the identity level, the US lapse overlay off, as the game's growth and period-open GDP read it (review 3's R3-2)
                var potentialSum = world.Countries.ToDictionary(k => k.Id, k => 0.0);
                int periodDays = 0, unitMeasured = 0;
                double scale = Value(typeof(MacroSystem), "SustainedApprovalTermScale"), growthFigure = Value(typeof(MacroSystem), "GrowthApprovalSensitivity");
                int crossed = 0;
                for (int d = 0; d < 2 * SimulationManager.DaysPerTurn + 10 && crossed < 2; d++)
                {
                    var dayBefore = world.Countries.ToDictionary(k => k.Id, k => k.State.ApprovalRating);
                    bool boundary = sim.AdvanceDay();
                    periodDays++;
                    foreach (Country k in world.Countries) { potentialSum[k.Id] += k.PotentialGrowthRate; }
                    foreach (Country k in world.Countries)
                    {
                        Queue<float> q = recent[k.Id];
                        q.Enqueue(Math.Abs(k.State.ApprovalRating - dayBefore[k.Id]));
                        while (q.Count > 30) { q.Dequeue(); }
                    }
                    if (!boundary) { continue; }
                    var gdpClose = world.Countries.ToDictionary(k => k.Id, k => k.State.GDP + k.LapseOutputWedge);
                    var open = new Dictionary<CountryId, (float Approval, float Daily, float Events, float Clamp, float Fading)>();
                    foreach (Country k in world.Countries)
                    {
                        ApprovalAttribution l = k.ApprovalLedgerAccruing;
                        open[k.Id] = (k.State.ApprovalRating, l != null ? DailyTerms(l) : 0f, l != null ? l.EventSum : 0f, l != null ? l.ClampLoss : 0f, l != null ? l.TaxTermFading : 0f);
                    }
                    sim.AdvanceTurn(decisions);
                    crossed++;
                    foreach (Country k in world.Countries)
                    {
                        ApprovalAttribution closed = k.ApprovalLedgerLastPeriod;
                        if (closed == null || closed.PeriodCloseDate != sim.CurrentDate) { Check(false, F("(e) {0}: no approval period closed on {1:yyyy-MM-dd}", k.Id, sim.CurrentDate)); continue; }
                        var o = open[k.Id];
                        float dailyAtBoundary = DailyTerms(closed) - o.Daily;
                        float oneOffs = closed.TaxHikePenalty + closed.SpendingEffect;
                        float events = closed.EventSum - o.Events;
                        float beyond = (k.State.ApprovalRating - o.Approval) - oneOffs - events - (closed.ClampLoss - o.Clamp) - (closed.TaxTermFading - o.Fading);
                        float largestDay = recent[k.Id].Count > 0 ? recent[k.Id].Max() : 0f;
                        Check(dailyAtBoundary == 0f && Math.Abs(beyond) <= Math.Max(1e-4f, largestDay),
                            F("(e) boundary {0} ({1:yyyy-MM-dd}) {2}: the reversion and the sustained terms recorded at the boundary {3:+0.000000;-0.000000}; the boundary moved approval {4:+0.0000;-0.0000} beyond its one-off terms ({5:+0.0000;-0.0000}) and events ({6:+0.0000;-0.0000}); the largest day's move of the 30 before {7:F4}",
                                crossed, sim.CurrentDate, k.Id, dailyAtBoundary, beyond, oneOffs, events, largestDay));
                        // (e2) N-5: the daily step reads ANNUAL growth - the period's growth term, read back as the mean growth it was paid on, matches the period's
                        // realised growth annualised over its days; a call site passing the day's raw growth would read 365 times too small
                        double implied = closed.GrowthEffect / (scale * growthFigure * periodDays) + potentialSum[k.Id] / periodDays;
                        double realised = (gdpClose[k.Id] - gdpOpen[k.Id]) / gdpOpen[k.Id] * 100.0 * SimulationManager.DaysPerTurn / periodDays;
                        if (Math.Abs(realised) > 0.5) { unitMeasured++; }
                        Check(Math.Abs(implied - realised) <= 0.1 + 0.05 * Math.Abs(realised),
                            F("(e2) boundary {0} {1}: the period's growth term was paid on a mean growth of {2:F3} % a year; the period's realised growth, annualised over its {3} days, {4:F3} %", crossed, k.Id, implied, periodDays, realised));
                    }
                    periodDays = 0;   // the next period opens after the boundary
                    foreach (Country k in world.Countries) { gdpOpen[k.Id] = k.State.GDP + k.LapseOutputWedge; potentialSum[k.Id] = 0.0; }
                }
                Check(crossed == 2, F("(e) {0} of 2 boundaries crossed", crossed));
                Check(unitMeasured > 0, F("(e2) {0} country-period(s) grew or shrank by more than half a point, so the unit test had teeth", unitMeasured));
            }
            finally { Object.DestroyImmediate(go); }
        }

        // ============================================================ (f) a mid-period writer is inside the ledger's window
        /// <summary>
        /// (f) The integration finding of 2026-10-28: approval's daily step opens the period's ledger on the period's first day, so every approval write
        /// after it - here a law's enactment cost through the real private enact path, as EnergyLedgerDiagnostic and EnergyPassThroughDiagnostic drive
        /// it - lies INSIDE the window the boundary's ATTRIB audit checks. Before MF-13 the ledger first opened at the boundary, AFTER such a write, so an
        /// unrecorded harness write went unseen; afterwards the audit failed by exactly the law's cost (Germany, 1.000). Germany played 100 days, then
        /// the audit's own identity (approval − the period's opening against events + terms + clamp) read on the accruing ledger: (f1) it holds before the
        /// write; (f2) an unrecorded enactment cost opens a gap of exactly that cost - the window covers it, so the audit would catch the writer; (f3)
        /// recorded as the game's law resolution records it, the gap closes; and the boundary that follows closes the period clean (an ATTRIB here
        /// fails the run through ArmLogFold).
        /// </summary>
        private static void MidPeriodWriter()
        {
            _sb.Append("    f. a mid-period approval writer lies inside the period's ledger (the integration finding of 2026-10-28)\n");
            SimulationRandom.Seed(777);
            EnergyMarket.ResetCalibration();
            World world = WorldFactory.CreateDefault();
            var go = new GameObject("APPROVALREVERSION.F");
            try
            {
                SimulationManager sim = go.AddComponent<SimulationManager>();
                sim.SetWorld(world);
                sim.PlayerCountryId = CountryId.Germany;
                var decisions = new Dictionary<CountryId, PolicyDecision>();
                foreach (Country k in world.Countries) { decisions[k.Id] = PolicyDecision.None(); }
                Country de = world.GetCountry(CountryId.Germany);
                for (int d = 0; d < 100; d++) { if (sim.AdvanceDay()) { sim.AdvanceTurn(decisions); } }
                float Gap()
                {
                    ApprovalAttribution l = de.ApprovalLedgerAccruing;
                    return l == null ? float.NaN : (de.State.ApprovalRating - l.ApprovalAtPeriodOpen) - (l.EventSum + l.TermSum + l.ClampLoss);
                }
                float g0 = Gap();
                MethodInfo enact = typeof(SimulationManager).GetMethod("ApplyLawBillEffects", BindingFlags.Instance | BindingFlags.NonPublic);
                if (enact == null) { Check(false, "(f) SimulationManager.ApplyLawBillEffects not found by reflection - this section VERIFIED NOTHING"); return; }
                LawDefinition law = LawCatalog.All.FirstOrDefault(l => l.EnactmentApprovalCost > 0.5f && !de.EnactedLaws.Exists(e => e.LawId == l.Id) && LawCatalog.IsWithinCompetence(world, de, l));
                if (law == null) { Check(false, "(f) no unenacted law with an enactment cost for Germany - this section VERIFIED NOTHING"); return; }
                float before = de.State.ApprovalRating;
                enact.Invoke(sim, new object[] { de, new LawBill { LawId = law.Id, IsRepeal = false } });
                float delta = de.State.ApprovalRating - before;
                float g1 = Gap();
                ApprovalLedgerRecorder.RecordEvent(de, sim.CurrentDate, "Enact: " + law.Id + " (ApprovalReversionDiagnostic)", delta);
                float g2 = Gap();
                Check(Math.Abs(g0) <= 1e-3f && delta <= -0.5f && Math.Abs(g1 - delta) <= 1e-3f && Math.Abs(g2) <= 1e-3f,
                    F("(f) Germany on {0:yyyy-MM-dd}, 100 days in: the audit identity's gap {1:+0.00000;-0.00000} before; the law {2} costs {3:+0.0000;-0.0000} through the real enact path, unrecorded the gap is {4:+0.00000;-0.00000} (the window covers the write, so the audit would catch it); recorded as the game records it {5:+0.00000;-0.00000}",
                        sim.CurrentDate, g0, law.Id, delta, g1, g2));
                int crossed = 0;
                for (int d = 0; d < SimulationManager.DaysPerTurn + 2 && crossed == 0; d++) { if (sim.AdvanceDay()) { sim.AdvanceTurn(decisions); crossed++; } }
                Check(crossed == 1 && de.ApprovalLedgerLastPeriod != null && de.ApprovalLedgerLastPeriod.Closed,
                    F("(f) the boundary that follows ({0:yyyy-MM-dd}) closed the period with the recorded write (an ATTRIB there would fail this run)", sim.CurrentDate));
            }
            finally { Object.DestroyImmediate(go); }
        }

        // ============================================================ the harness's hands
        /// <summary>The seeded world (seed 777), unplayed.</summary>
        private static World SeededWorld()
        {
            SimulationRandom.Seed(777);
            EnergyMarket.ResetCalibration();
            return WorldFactory.CreateDefault();
        }

        /// <summary>Approval set to <paramref name="start"/>, then approval's daily step alone for <paramref name="days"/> days with the growth gap given;
        /// returns approval. Writes approval, the tax term's stock and the economy stock (the callers restore them), and the bare country's own ledger.</summary>
        private static float Settle(Country c, float start, float growthGap, int days)
        {
            c.State.ApprovalRating = start;
            DateTime date = new DateTime(2026, 1, 1);
            for (int t = 0; t < days; t++) { MacroSystem.ApplyApprovalDay(c, c.PotentialGrowthRate + growthGap, date); }
            return c.State.ApprovalRating;
        }

        /// <summary>The ledger's daily-step terms: the reversion and every sustained term.</summary>
        private static float DailyTerms(ApprovalAttribution l)
            => l.Reversion + l.GrowthEffect + l.MiseryUnemployment + l.MiseryInflation + l.MiseryCrime + l.MiseryCorruption + l.WelfareEffect + l.PaidLeaveEffect + l.DrugPolicyEffect + l.GiniEffect;

        /// <summary>A constant or static read-only figure read by name, whatever its access - the check reads the figures the model reads.</summary>
        private static double Value(Type owner, string name)
        {
            FieldInfo field = owner.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            if (field == null || !(field.IsLiteral || field.IsInitOnly)) { Check(false, F("{0}.{1} is not a constant or read-only figure here", owner.Name, name)); return double.NaN; }
            object raw = field.IsLiteral ? field.GetRawConstantValue() : field.GetValue(null);
            return raw is float f ? f : Convert.ToDouble(raw, CultureInfo.InvariantCulture);
        }

        private static void Check(bool ok, string what)
        {
            _sb.Append(ok ? "        ok   " : "        FAIL ").Append(what).Append('\n');
            if (!ok) { _failures++; Debug.LogError("APPROVAL REVERSION: FAIL - " + what); }
        }

        private static string F(string format, params object[] args) => string.Format(CultureInfo.InvariantCulture, format, args);
    }
}
