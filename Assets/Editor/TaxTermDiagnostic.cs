using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using PoliSim.Data;
using PoliSim.Persistence;
using PoliSim.Simulation;
using UnityEngine;
using Object = UnityEngine.Object;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// A-LOOP q37 (2026-10-19, `docs/specs/A_LOOP_PLAN.md` §2 (4)): <b>the tax term's pass criteria that an Editor harness can prove</b>, on Sweden
    /// (seed 777, the default epoch named). Every bill is applied through the game's own application (`ParliamentSystem.ApplyBillResult`; a levy through
    /// `ParliamentSystem.ApplyTaxProgramBillResult` and `TaxTerm.ChargeLevyMove`, as `SimulationManager.AdvanceTaxProgramBillsDay` does), every planted
    /// approval and every bill's approval move recorded on the approval ledger as the game's callers record it - an unrecorded harness write fails ATTRIB.
    /// <list type="number">
    /// <item><b>The one unit</b> (review 877's M1, M2): an unlevied line stands at 0 whatever its placeholder; its levy moves 0 → its rate; Sweden's income
    /// line with sub-rows of its own reads its EFFECTIVE rate, off its lever by more than a point; a seed-levied per-tonne line reads Rate × 100 ⁄ MaxRate.</item>
    /// <item><b>The form, through the bill</b>: a cut alone +m·s·points; a rise −s·points; a pair on one day nets T to exactly zero; a band move and a
    /// rate move of the same effective points charge the same; the income lever is charged on its effective points; a levy charges s × its rate and its
    /// repeal on the day nets to zero; a seed tax's removal earns the mirror; a move no one booked (a statute's step) shifts the opening, so cutting it
    /// back is a cut at the mirror (review 878's M-3); a bill the government does not bear charges nothing and enters no net (L-1).</item>
    /// <item><b>Against the run with neither</b> (four worlds, each played alone from the seed): a cut alone +m·s·points on the day; the game's day fades
    /// T by exactly `TaxTerm.DailyPersistence` and records it as `TaxTermFading`; a restore thirty days on leaves m·s·x·(1 − p³⁰); a pair on one day
    /// leaves approval within 0.01 of neither sixty days on; at the boundary every country's ledger holds observed = events + terms + clamp with the
    /// fading named; the next fiscal year's first day empties the net, and a rise then is a full one.</item>
    /// <item><b>The player's decision at the boundary</b> (review 878's M-4), through `SimulationManager.ApplyTaxRateChanges` and the formula: a VAT cut
    /// of 2 books the plan's impulse in the period's `TaxHikePenalty` and into T against the run restating VAT (+m·s·2 and −m·s·2 where the year's net
    /// is 0); a rise of 2, −s·2 and +s·2; one reversion in the three runs (the turn's impulse is not in its base) and the formula's approval moved by
    /// exactly −impulse (review 878b's M-6).</item>
    /// <item><b>A mid-year save</b> with T and the net standing: both come back through the JSON load into a manager, and the restore that follows charges
    /// on the loaded game what it charges on the continuous one.</item>
    /// <item><b>The clamp</b> (M-2, M-5): a rise at the floor books only what landed and hands back no more; a cut at the ceiling likewise; a levy at the
    /// floor; a standing T absorbed by a shock to the ceiling is forgiven; the formula's own clamp at a boundary books what landed, approval + T equal
    /// to the restating run's; and through a bound in two runs (a rise then a shock past the ceiling, a cut then a shock past the floor) the move run
    /// IS the no-move run every day for ten years; and (6g, review 879b's M-1) a shock that meets a bound AGAINST a standing T - a rise at 4 then an
    /// event of −2, a cut at 96 then +2, through `TaxTerm.Shock` as every clamped writer goes - leaves the move run the no-move run for ten years.</item>
    /// <item><b>The reversion</b> (M-1): over ten years of the game's daily fading, approval's daily step (MF-13, K191) and the yearly formula, a rise's approval path stays at or below the
    /// no-move path every day - it is exactly the no-move path less T - and converges to it.</item>
    /// <item><b>The half-life</b> on the game's own days: a rise's T is at half its size within ±10 days of the declared half-life
    /// (ln 0.5 ⁄ ln 0.81 quarters of 365 ⁄ 4 days, the plan's "some 300 days").</item>
    /// </list>
    /// Prints `TAX TERM: PASS` and exits 0 when every check holds; each failure is a `TAX TERM:` error naming its check.
    /// Run: `Unity.exe -batchmode -nographics -projectPath &lt;path&gt; -executeMethod PoliSim.EditorTools.TaxTermDiagnostic.Run -logFile &lt;path&gt;`.
    /// </summary>
    public static class TaxTermDiagnostic
    {
        private const float CutPoints = 2f;
        private const float BandRaise = 2f;
        private const float Tolerance = 1e-3f;
        private const int MoveDayIntoYear = 10;
        private const int RestoreAfterDays = 30;
        private const int CompareAfterDays = 60;
        private const int LongRunYears = 10;

        private static int _failures;
        private static StringBuilder _sb;

        public static void Run()
        {
            CheckExit.ArmLogFold();   // the worlds cross a boundary: an ATTRIB during the run fails it
            using IDisposable epoch = SimulationManager.EpochScope();
            SimulationManager.SetEpoch(SimulationManager.DefaultEpoch);   // the default epoch named, never the process's: a start a check left would move the timeline
            _failures = 0;
            _sb = new StringBuilder("TAX TERM: A-LOOP q37 - the tax term against the fiscal year's opening, the mirror, the fading (Sweden, seed 777)\n");
            float s = MacroSystem.TaxHikeApprovalSensitivity, m = TaxTerm.MirrorFraction;
            _sb.Append(F("    s = {0}, m = {1}, persistence {2} a quarter, {3:R} a day, declared half-life {4:F1} days\n", s, m, TaxTerm.QuarterlyPersistence, TaxTerm.DailyPersistence, TaxTerm.HalfLifeDays));
            try
            {
                Units();
                Form(s, m);
                AgainstNeither(s, m);
                Decision(s, m);
                SaveAndLoad(s, m);
                Clamp(s, m);
                AgainstT();
                Reversion(s);
                HalfLife(s);
            }
            catch (Exception e) { Check(false, "the run threw " + e.GetType().Name + ": " + e.Message + "\n" + e.StackTrace); }
            Debug.Log(_sb.ToString());
            Debug.Log(_failures == 0
                ? "TAX TERM: PASS - the one unit, the form through the bill, against neither, the player's decision, a mid-year save, the clamp, the reversion and the half-life."
                : F("TAX TERM: FAIL - {0} check(s) failed.", _failures));
            CheckExit.Finish(_failures == 0 ? 0 : 1);
        }

        // ============================================================ (1) the one unit
        private static void Units()
        {
            _sb.Append("    1. the one unit (review 877's M1, M2)\n");
            WithSweden((sim, se) =>
            {
                TaxLine excise = Line(se, TaxType.ExciseTax);
                Check(excise != null && !excise.IsImplemented && excise.Rate > 0f && ParliamentSystem.TaxLevelPoints(se, excise) == 0f,
                    F("(1a) the unlevied excise line (placeholder rate {0}) stands at 0 through TaxLevelPoints", excise?.Rate));
                TaxLevyMove levy = PassLevy(sim, se, TaxType.ExciseTax, true, out float _);
                Check(levy.Moved && levy.FromPoints == 0f && levy.ToPoints == excise.Rate && TaxTerm.NetPoints(se, TaxType.ExciseTax) == excise.Rate,
                    F("(1b) the excise levy's move reads 0 -> {0} (from {1} to {2}; the year's net {3})", excise.Rate, levy.FromPoints, levy.ToPoints, TaxTerm.NetPoints(se, TaxType.ExciseTax)));

                TaxLine income = Line(se, TaxType.IncomeTax);
                if (income == null || !income.IsImplemented || !TaxSchedule.Responds(CountryId.Sweden)) { Check(false, "(1c) Sweden carries no responding, levied income line - the effective unit is not tested"); }
                else
                {
                    income.BracketRates = SubRowRates(CountryId.Sweden, row => row.Adjustable ? 0f : -1f);   // GA-2-12's case: the flat layer and the state band at 0
                    se.ScheduleMemo = null;
                    float effective = TaxSchedule.EffectiveRate(se, income, income.Rate);
                    float lever = income.Rate;
                    TaxLevyMove removal = PassLevy(sim, se, TaxType.IncomeTax, false, out float _);
                    Check(removal.Moved && Mathf.Abs(removal.FromPoints - effective) <= 1e-4f && Mathf.Abs(removal.FromPoints - lever) > 1f && removal.ToPoints == 0f,
                        F("(1c) the income line with its sub-rows at 0, removed: from {0:F4} (the effective rate {1:F4}; the lever {2:F2}) to {3}", removal.FromPoints, effective, lever, removal.ToPoints));
                }

                int perTonne = 0;
                foreach (Country k in sim.World.Countries)
                {
                    TaxLine carbon = Line(k, TaxType.CarbonTax);
                    if (carbon == null || !carbon.IsImplemented || !carbon.IsPerTonne) { continue; }
                    perTonne++;
                    float rate = carbon.Rate, max = carbon.MaxRate, dial = carbon.Rate * 100f / carbon.MaxRate;
                    TaxLevyMove removal = PassLevy(sim, k, TaxType.CarbonTax, false, out float _);
                    Check(removal.Moved && Mathf.Abs(removal.FromPoints - dial) <= 1e-4f && Mathf.Abs(removal.FromPoints - rate) > 1f && removal.ToPoints == 0f,
                        F("(1d) {0}'s carbon line (per tonne, levied at the seed) removed: from {1:F4} - the dial's per cent, Rate {2} x 100 / MaxRate {3} = {4:F4}, not the rate", k.Id, removal.FromPoints, rate, max, dial));
                }
                if (perTonne == 0) { Check(false, "(1d) no country levies a per-tonne line at the seed - the dial's unit is not tested"); }
            });
        }

        // ============================================================ (2) the form, through the bill
        private static void Form(float s, float m)
        {
            _sb.Append("    2. the form, through the bill (fresh worlds at the seed)\n");
            float vat = SeedRate(TaxType.VAT);

            WithSweden((sim, se) =>
            {
                float cut = PassRate(sim, se, TaxType.VAT, vat - CutPoints);
                Check(Mathf.Abs(cut - m * s * CutPoints) <= Tolerance && Mathf.Abs(se.TaxTermStock + m * s * CutPoints) <= Tolerance,
                    F("(2a) a cut alone of {0} points: approval {1:+0.0000;-0.0000} (m*s*points = {2:F4}), T {3:F4}", CutPoints, cut, m * s * CutPoints, se.TaxTermStock));
                float restore = PassRate(sim, se, TaxType.VAT, vat);
                Check(Mathf.Abs(restore + m * s * CutPoints) <= Tolerance && se.TaxTermStock == 0f && TaxTerm.NetPoints(se, TaxType.VAT) == 0f,
                    F("(2b) its restore on the day: approval {0:+0.0000;-0.0000}, T {1:R}, the net {2:R} - the impulses net to zero", restore, se.TaxTermStock, TaxTerm.NetPoints(se, TaxType.VAT)));
            });
            WithSweden((sim, se) =>
            {
                float rise = PassRate(sim, se, TaxType.VAT, vat + CutPoints);
                float back = PassRate(sim, se, TaxType.VAT, vat);
                Check(Mathf.Abs(rise + s * CutPoints) <= Tolerance && Mathf.Abs(back - s * CutPoints) <= Tolerance && se.TaxTermStock == 0f,
                    F("(2c) a rise of {0} points: approval {1:+0.0000;-0.0000} (-s*points = {2:F4}); its cut back on the day {3:+0.0000;-0.0000}, T {4:R}", CutPoints, rise, -s * CutPoints, back, se.TaxTermStock));
            });

            // (2d) a band move and a rate move of the same effective points charge the same
            float bandPoints = float.NaN, bandCharge = float.NaN;
            WithSweden((sim, se) =>
            {
                TaxLine income = Line(se, TaxType.IncomeTax);
                double top = 0.0;
                foreach (TaxSchedule.SubRow row in TaxSchedule.SubRows(TaxSchedule.Of(CountryId.Sweden))) { if (row.Adjustable) { top = Math.Max(top, row.StatuteRate); } }
                float before = TaxSchedule.EffectiveRate(se, income, income.Rate);
                var bill = new BudgetBill();
                bill.BracketRates[TaxType.IncomeTax] = SubRowRates(CountryId.Sweden, row => row.Adjustable && row.StatuteRate == top ? (float)top + BandRaise : -1f);
                bandCharge = Pass(sim, se, bill);
                bandPoints = TaxSchedule.EffectiveRate(se, income, income.Rate) - before;
            });
            WithSweden((sim, se) =>
            {
                float rateCharge = PassRate(sim, se, TaxType.VAT, vat + bandPoints);
                Check(bandPoints > 0.01f && Mathf.Abs(bandCharge - rateCharge) <= Tolerance && Mathf.Abs(bandCharge + s * bandPoints) <= Tolerance,
                    F("(2d) Sweden's top adjustable sub-row +{4} moves the effective rate {0:F4} points and charges {1:+0.0000;-0.0000}; VAT +{0:F4} points charges {2:+0.0000;-0.0000} (-s*points = {3:F4})", bandPoints, bandCharge, rateCharge, -s * bandPoints, BandRaise));
            });
            WithSweden((sim, se) =>
            {
                TaxLine income = Line(se, TaxType.IncomeTax);
                float before = TaxSchedule.EffectiveRate(se, income, income.Rate);
                float charge = PassRate(sim, se, TaxType.IncomeTax, income.Rate + 1f);
                float effective = TaxSchedule.EffectiveRate(se, income, income.Rate) - before;
                Check(Mathf.Abs(charge + s * effective) <= Tolerance,
                    F("(2e) the income lever +1 moves the effective rate {0:F4} points and charges {1:+0.0000;-0.0000} (-s x the effective points, D-33; before q37 -s x the lever's 1)", effective, charge));
            });
            WithSweden((sim, se) =>
            {
                float rate = Line(se, TaxType.ExciseTax).Rate;
                PassLevy(sim, se, TaxType.ExciseTax, true, out float levied);
                PassLevy(sim, se, TaxType.ExciseTax, false, out float repealed);
                Check(Mathf.Abs(levied + s * rate) <= Tolerance && Mathf.Abs(repealed - s * rate) <= Tolerance && se.TaxTermStock == 0f,
                    F("(2f) a levy of the excise at {0}: approval {1:+0.0000;-0.0000} (-s x its rate, a rise from 0); its repeal on the day {2:+0.0000;-0.0000}, T {3:R}", rate, levied, repealed, se.TaxTermStock));
            });
            WithSweden((sim, se) =>
            {
                PassLevy(sim, se, TaxType.VAT, false, out float removed);
                Check(Mathf.Abs(removed - m * s * vat) <= Tolerance,
                    F("(2g) the seed's VAT ({0}) removed: approval {1:+0.0000;-0.0000} (the mirror, m*s*rate = {2:F4})", vat, removed, m * s * vat));
            });
            WithSweden((sim, se) =>
            {
                // (2h) review 878's M-3: a move no one booked - the line's rate written as a statute's step writes it - shifts the opening, so cutting it back
                // is a CUT of the government's (the mirror), not the removal of a rise it never made (s)
                TaxLine line = Line(se, TaxType.VAT);
                line.Rate = vat + 1f;   // the unbooked step
                float back = PassRate(sim, se, TaxType.VAT, vat);
                Check(Mathf.Abs(back - m * s * 1f) <= Tolerance && Mathf.Abs(TaxTerm.NetPoints(se, TaxType.VAT) + 1f) <= 1e-5f,
                    F("(2h) an unbooked +1 step, then the government's cut of 1: approval {0:+0.0000;-0.0000} (the mirror m*s = {1:F4}; drift priced at the kink would read s = {2:F4}), the net {3:R}", back, m * s, s, TaxTerm.NetPoints(se, TaxType.VAT)));
            });
            WithSweden((sim, se) =>
            {
                // (2i) review 878's L-1: a bill the government does not bear (a congressional majority's) charges it nothing and enters no net
                float before = se.State.ApprovalRating;
                var bill = new BudgetBill();
                bill.TaxLines[TaxType.VAT] = vat + CutPoints;
                ParliamentSystem.ApplyBillResult(se, bill, true, (k, b) => { }, sim.CurrentDate.Year, governmentBears: false);
                ApprovalLedgerRecorder.RecordEvent(se, sim.CurrentDate, "Budget bill passed (tax changes)", se.State.ApprovalRating - before);
                Check(se.State.ApprovalRating == before && se.TaxTermStock == 0f && TaxTerm.NetPoints(se, TaxType.VAT) == 0f && Line(se, TaxType.VAT).Rate == vat + CutPoints,
                    F("(2i) a VAT rise of {0} the government does not bear: the rate moved to {1}, approval {2:+0.0000;-0.0000}, T {3:R}, the net {4:R}", CutPoints, Line(se, TaxType.VAT).Rate, se.State.ApprovalRating - before, se.TaxTermStock, TaxTerm.NetPoints(se, TaxType.VAT)));
                // review 878b's L-8: the levy path, borne by no government (a congressional majority's levy), as SimulationManager.AdvanceTaxProgramBillsDay passes it
                float beforeLevy = se.State.ApprovalRating;
                TaxLevyMove notBorne = ParliamentSystem.ApplyTaxProgramBillResult(se, new TaxProgramBill { Type = TaxType.ExciseTax, IsAdd = true }, true, false);
                TaxTerm.ChargeLevyMove(se, notBorne, false);
                ApprovalLedgerRecorder.RecordEvent(se, sim.CurrentDate, EnumNames.ProgrammeBillWords(TaxType.ExciseTax, true) + " bill passed", se.State.ApprovalRating - beforeLevy);
                Check(notBorne.Moved && Line(se, TaxType.ExciseTax).IsImplemented && se.State.ApprovalRating == beforeLevy && se.TaxTermStock == 0f && TaxTerm.NetPoints(se, TaxType.ExciseTax) == 0f,
                    F("(2i) the excise levied by a bill the government does not bear: levied {0}, approval {1:+0.0000;-0.0000}, T {2:R}, the net {3:R}", Line(se, TaxType.ExciseTax).IsImplemented, se.State.ApprovalRating - beforeLevy, se.TaxTermStock, TaxTerm.NetPoints(se, TaxType.ExciseTax)));
            });
        }

        // ============================================================ (3) against the run with neither
        private enum Variant { Neither, PairOnOneDay, CutAlone, PairApart }

        /// <summary>What one played world read: approval on the move day after its moves, and at the comparison day.</summary>
        private sealed class Reading { public float ApprovalAfterMoves, ApprovalAtCompare; public DateTime MoveDay; }

        /// <summary>The four worlds are played ONE AFTER ANOTHER, each from seed 777: the simulation's random streams are process-wide, so two worlds
        /// advanced in turn would draw each other's numbers and part for that alone.</summary>
        private static void AgainstNeither(float s, float m)
        {
            _sb.Append("    3. the played worlds against the run with neither (each played alone from the seed)\n");
            float vat = SeedRate(TaxType.VAT);
            Reading neither = Play(Variant.Neither, vat, s, m), pair = Play(Variant.PairOnOneDay, vat, s, m), alone = Play(Variant.CutAlone, vat, s, m), apart = Play(Variant.PairApart, vat, s, m);
            if (neither == null || pair == null || alone == null || apart == null) { return; }
            Check(Mathf.Abs((alone.ApprovalAfterMoves - neither.ApprovalAfterMoves) - m * s * CutPoints) <= Tolerance,
                F("(3a) a cut alone on {0:yyyy-MM-dd}: approval {1:F4} against the run with neither {2:F4} (+m*s*points = {3:F4})", alone.MoveDay, alone.ApprovalAfterMoves, neither.ApprovalAfterMoves, m * s * CutPoints));
            float pairDelta = Mathf.Abs(pair.ApprovalAtCompare - neither.ApprovalAtCompare);
            Check(pairDelta <= 0.01f,
                F("(3d) a cut and its restore on one day, {0} days on: approval {1:F4} against the run with neither {2:F4} - |delta| {3:F5} <= 0.01", CompareAfterDays, pair.ApprovalAtCompare, neither.ApprovalAtCompare, pairDelta));
            _sb.Append(F("        (printed) {0} days on: the pair {1} days apart {2:F4}, the cut alone {3:F4}, against neither {4:F4} (the pair apart keeps (3c)'s residue, fading)\n",
                CompareAfterDays, RestoreAfterDays, apart.ApprovalAtCompare, alone.ApprovalAtCompare, neither.ApprovalAtCompare));
        }

        private static Reading Play(Variant variant, float vat, float s, float m)
        {
            var hosts = new List<GameObject>();
            try
            {
                SimulationManager sim = Played(hosts, out World world);
                Country se = world.GetCountry(CountryId.Sweden);
                DateTime boundary = SimulationManager.TurnBoundary(1);
                var reading = new Reading { MoveDay = sim.CurrentDate.AddDays(MoveDayIntoYear) };
                if (!(reading.MoveDay.AddDays(CompareAfterDays) < boundary))
                {
                    Check(false, F("(3) the timeline: the moves on {0:yyyy-MM-dd} and the reading {1} days on do not fit before the boundary {2:yyyy-MM-dd}", reading.MoveDay, CompareAfterDays, boundary));
                    return null;
                }
                Days(sim, MoveDayIntoYear);
                if (variant != Variant.Neither) { PassRate(sim, se, TaxType.VAT, vat - CutPoints); }
                if (variant == Variant.PairOnOneDay) { PassRate(sim, se, TaxType.VAT, vat); }
                reading.ApprovalAfterMoves = se.State.ApprovalRating;

                if (variant == Variant.CutAlone)
                {
                    // (3b) the game's own day fades T by exactly the daily persistence and records what it handed back as the ledger's named term
                    float stockBefore = se.TaxTermStock, fadingBefore = se.ApprovalLedgerAccruing?.TaxTermFading ?? 0f;
                    Days(sim, 1);
                    float expectedStock = stockBefore * TaxTerm.DailyPersistence;
                    float fadingDay = (se.ApprovalLedgerAccruing?.TaxTermFading ?? 0f) - fadingBefore;
                    Check(stockBefore != 0f && se.TaxTermStock == expectedStock && Mathf.Abs(fadingDay - (stockBefore - expectedStock)) <= 1e-6f,
                        F("(3b) a day of the game: T {0:R} -> {1:R} (x {2:R}), the ledger's TaxTermFading took {3:R} (handed back {4:R})", stockBefore, se.TaxTermStock, TaxTerm.DailyPersistence, fadingDay, stockBefore - expectedStock));
                    Days(sim, RestoreAfterDays - 1);
                }
                else { Days(sim, RestoreAfterDays); }

                if (variant == Variant.PairApart)
                {
                    float faded = se.TaxTermStock;
                    PassRate(sim, se, TaxType.VAT, vat);
                    float residue = m * s * CutPoints * (1f - Mathf.Pow(TaxTerm.DailyPersistence, RestoreAfterDays));
                    Check(Mathf.Abs(se.TaxTermStock - residue) <= Tolerance,
                        F("(3c) the restore {0} days after the cut: T {1:F4} before it, {2:F4} after (m*s*x*(1 - p^{0}) = {3:F4}: the cut's faded part, the declared reading)", RestoreAfterDays, faded, se.TaxTermStock, residue));
                }
                Days(sim, CompareAfterDays - RestoreAfterDays);
                reading.ApprovalAtCompare = se.State.ApprovalRating;

                if (variant == Variant.CutAlone) { AcrossTheOpening(sim, world, s); }
                return reading;
            }
            finally { foreach (GameObject go in hosts) { Object.DestroyImmediate(go); } }
        }

        /// <summary>(3e)-(3g): the cut-alone world played across the boundary to the next fiscal year's first day - every country's approval ledger holds
        /// its identity at the boundary with the tax term's fading a named term; the year's net is emptied; a rise then is a full one.</summary>
        private static void AcrossTheOpening(SimulationManager sim, World world, float s)
        {
            Country se = world.GetCountry(CountryId.Sweden);
            DateTime nextOpening = new DateTime(sim.CurrentDate.Year + 1, 1, 1);
            Days(sim, (int)(nextOpening - sim.CurrentDate).TotalDays);
            foreach (Country k in world.Countries)
            {
                ApprovalAttribution closed = k.ApprovalLedgerLastPeriod;
                if (closed == null || !closed.Closed) { Check(false, F("(3e) {0} has no closed approval period after the boundary", k.Id)); continue; }
                float observed = closed.ApprovalAtClose - closed.ApprovalAtPeriodOpen;
                float explained = closed.EventSum + closed.TermSum + closed.ClampLoss;
                bool named = k.Id != CountryId.Sweden || closed.TaxTermFading != 0f;   // Sweden's cut stood and faded through the period
                Check(Mathf.Abs(observed - explained) <= 0.001f && named,
                    F("(3e) {0}'s approval ledger at the boundary: observed {1:F4} = events {2:F4} + terms {3:F4} (the tax term's fading {4:F4}, the turn's impulse {5:F4}) + clamp {6:F4}", k.Id, observed, closed.EventSum, closed.TermSum, closed.TaxTermFading, closed.TaxHikePenalty, closed.ClampLoss));
            }
            Check(sim.CurrentDate == nextOpening && se.TaxTermNet == null && TaxTerm.NetPoints(se, TaxType.VAT) == 0f,
                F("(3f) {0:yyyy-MM-dd}, the fiscal year's first day: the year's net emptied - every line at its opening (VAT stands at {1})", nextOpening, Line(se, TaxType.VAT).Rate));
            TaxLine vatLine = Line(se, TaxType.VAT);
            float rise = Mathf.Min(CutPoints, vatLine.MaxRate - vatLine.Rate);
            float charge = PassRate(sim, se, TaxType.VAT, vatLine.Rate + rise);
            Check(rise > 0.5f && Mathf.Abs(charge + s * rise) <= Tolerance,
                F("(3g) in the new fiscal year VAT +{0:F2} points from where it stands: approval {1:+0.0000;-0.0000} - a full rise from the new opening (-s*points = {2:F4})", rise, charge, -s * rise));
        }

        // ============================================================ (4) the player's decision at the boundary
        private sealed class AtBoundary { public float Penalty, Stock, RateBefore, RateAfter, NetBefore, Reversion, AfterFormula, Approval; }

        private static void Decision(float s, float m)
        {
            _sb.Append("    4. the player's decision at the boundary, through ApplyTaxRateChanges and the formula (against the run restating VAT where it stands)\n");
            AtBoundary none = AtFirstBoundary(0f), cut = AtFirstBoundary(-CutPoints), rise = AtFirstBoundary(CutPoints);
            if (none == null || cut == null || rise == null) { return; }
            // the plan's P on the net the year had booked before the boundary (the same in the three runs, which part only at it)
            float net = none.NetBefore;
            float cutImpulse = Plan(net - CutPoints, s, m) - Plan(net, s, m), riseImpulse = Plan(net + CutPoints, s, m) - Plan(net, s, m);
            float cutPenalty = cut.Penalty - none.Penalty, cutStock = cut.Stock - none.Stock;
            float risePenalty = rise.Penalty - none.Penalty, riseStock = rise.Stock - none.Stock;
            Check(cut.NetBefore == net && cut.RateAfter == cut.RateBefore - CutPoints && none.RateAfter == none.RateBefore && Mathf.Abs(cutPenalty + cutImpulse) <= Tolerance && Mathf.Abs(cutStock - cutImpulse) <= Tolerance && cutImpulse < 0f,
                F("(4a) the decision VAT -{0} (from {1}, the year's net {2}): the period's TaxHikePenalty {3:+0.0000;-0.0000} against the restating run, T {4:+0.0000;-0.0000} (the plan's impulse {5:+0.0000;-0.0000}; +m*s*points where the net is 0)", CutPoints, cut.RateBefore, net, cutPenalty, cutStock, cutImpulse));
            Check(rise.RateAfter == rise.RateBefore + CutPoints && Mathf.Abs(risePenalty + riseImpulse) <= Tolerance && Mathf.Abs(riseStock - riseImpulse) <= Tolerance && riseImpulse > 0f,
                F("(4b) the decision VAT +{0}: TaxHikePenalty {1:+0.0000;-0.0000}, T {2:+0.0000;-0.0000} (the plan's impulse {3:+0.0000;-0.0000}; -s*points where the net is 0)", CutPoints, risePenalty, riseStock, riseImpulse));
            // review 878b's M-6: the turn's own impulse is not in its reversion's base (the three runs are one run until the boundary, so one reversion),
            // and the formula moved approval by exactly −impulse against the restating run
            Check(cut.Reversion == none.Reversion && rise.Reversion == none.Reversion
                  && Mathf.Abs((cut.AfterFormula - none.AfterFormula) + cutImpulse) <= Tolerance && Mathf.Abs((rise.AfterFormula - none.AfterFormula) + riseImpulse) <= Tolerance,
                F("(4c) the reversion {0:R} in all three runs ({1:R}, {2:R}); the formula's approval against the restating run: the cut {3:+0.0000;-0.0000}, the rise {4:+0.0000;-0.0000} (-impulse: {5:+0.0000;-0.0000}, {6:+0.0000;-0.0000})",
                    none.Reversion, cut.Reversion, rise.Reversion, cut.AfterFormula - none.AfterFormula, rise.AfterFormula - none.AfterFormula, -cutImpulse, -riseImpulse));
        }

        /// <summary>The plan's P(x), written from §2 (2)'s words for the check: s·x for a rise, m·s·x for a cut.</summary>
        private static float Plan(float x, float s, float m) => x >= 0f ? s * x : m * s * x;

        /// <summary>Sweden played to its first boundary, the player's decision there setting VAT <paramref name="delta"/> off where it stands (0: restated);
        /// the closed period's TaxHikePenalty, T and VAT read right after it.</summary>
        private static AtBoundary AtFirstBoundary(float delta, float? plantApproval = null)
        {
            (SimulationManager sim, World world, GameObject go) = Open();
            try
            {
                var decisions = new Dictionary<CountryId, PolicyDecision>();
                foreach (Country k in world.Countries) { decisions[k.Id] = PolicyDecision.None(); }
                Country se = world.GetCountry(CountryId.Sweden);
                for (int d = 0; d < 2 * SimulationManager.DaysPerTurn; d++)
                {
                    if (!sim.AdvanceDay()) { continue; }
                    if (plantApproval.HasValue) { Plant(sim, se, plantApproval.Value); }
                    var at = new AtBoundary { RateBefore = Line(se, TaxType.VAT).Rate, NetBefore = TaxTerm.NetPoints(se, TaxType.VAT) };
                    decisions[CountryId.Sweden].TaxRateOverrides[TaxType.VAT] = at.RateBefore + delta;
                    sim.AdvanceTurn(decisions);
                    at.Penalty = se.ApprovalLedgerLastPeriod?.TaxHikePenalty ?? float.NaN;
                    at.Reversion = se.ApprovalLedgerLastPeriod?.Reversion ?? float.NaN;
                    at.AfterFormula = se.ApprovalLedgerLastPeriod?.ApprovalAfterFormula ?? float.NaN;
                    at.Approval = se.State.ApprovalRating;   // after the whole boundary - the year's event and any cabinet event land after the formula, through TaxTerm.Shock
                    at.Stock = se.TaxTermStock;
                    at.RateAfter = Line(se, TaxType.VAT).Rate;
                    return at;
                }
                Check(false, "(4) no boundary within two years");
                return null;
            }
            finally { Object.DestroyImmediate(go); }
        }

        // ============================================================ (5) a mid-year save with T and the net standing
        private static void SaveAndLoad(float s, float m)
        {
            _sb.Append("    5. a mid-year save with T and the net standing\n");
            float vat = SeedRate(TaxType.VAT);
            var hosts = new List<GameObject>();
            try
            {
                SimulationManager sim = Played(hosts, out World world);
                Country se = world.GetCountry(CountryId.Sweden);
                Days(sim, MoveDayIntoYear);
                PassRate(sim, se, TaxType.VAT, vat - CutPoints);
                Days(sim, 5);
                string json = SaveGameService.Serialize(SaveGameService.CreateSaveGame(sim, world, CountryId.Sweden, null));
                SaveGame loaded = SaveGameService.Deserialize(json);
                var goB = new GameObject("TAXTERM.loaded"); hosts.Add(goB);
                SimulationManager simB = goB.AddComponent<SimulationManager>();
                SaveGameService.RestoreInto(simB, loaded);
                Country back = simB.World.GetCountry(CountryId.Sweden);
                Check(back.TaxTermStock == se.TaxTermStock && se.TaxTermStock != 0f && TaxTerm.NetPoints(back, TaxType.VAT) == TaxTerm.NetPoints(se, TaxType.VAT) && TaxTerm.NetPoints(se, TaxType.VAT) == -CutPoints,
                    F("(5a) loaded into a manager: T {0:R} (saved {1:R}), the VAT net {2:R} (saved {3:R})", back.TaxTermStock, se.TaxTermStock, TaxTerm.NetPoints(back, TaxType.VAT), TaxTerm.NetPoints(se, TaxType.VAT)));
                float onSaved = PassRate(sim, se, TaxType.VAT, vat);
                float onLoaded = PassRate(simB, back, TaxType.VAT, vat);
                Check(Mathf.Abs(onSaved - onLoaded) <= 1e-5f && back.TaxTermStock == se.TaxTermStock && Mathf.Abs(onSaved + m * s * CutPoints) <= Tolerance,
                    F("(5b) the restore on the continuous game {0:+0.0000;-0.0000} and on the loaded one {1:+0.0000;-0.0000} (-m*s*points = {2:F4}); T {3:R} and {4:R}", onSaved, onLoaded, -m * s * CutPoints, se.TaxTermStock, back.TaxTermStock));
            }
            finally { foreach (GameObject go in hosts) { Object.DestroyImmediate(go); } }
        }

        // ============================================================ (6) the clamp
        private static void Clamp(float s, float m)
        {
            _sb.Append("    6. the clamp (review 878's M-2)\n");
            float vat = SeedRate(TaxType.VAT);
            WithSweden((sim, se) =>
            {
                Plant(sim, se, 0.5f);
                PassRate(sim, se, TaxType.VAT, vat + 5f);   // an impulse of 7.5 against 0.5 of room
                float bookedAtFloor = se.TaxTermStock;
                float highest = Fade(sim, se, out float lowest);
                Check(Mathf.Abs(bookedAtFloor - 0.5f) <= 1e-5f && highest <= 0.5f + 1e-4f && Mathf.Abs(se.State.ApprovalRating - 0.5f) <= 1e-3f,
                    F("(6a) a rise of 5 at approval 0.5: T booked {0:R} (what landed, not s*5 = {1}); the hand-back peaks at {2:F5}, ends at {3:F5} - no more than the 0.5 that was lost", bookedAtFloor, s * 5f, highest, se.State.ApprovalRating));
            });
            WithSweden((sim, se) =>
            {
                Plant(sim, se, 99.5f);
                PassRate(sim, se, TaxType.VAT, vat - 4f);   // a mirror of 3 against 0.5 of room
                float bookedAtCeiling = se.TaxTermStock;
                Fade(sim, se, out float lowest);
                // the end at 2e-3, not (6a)'s 1e-3: approval is a float, whose step at 100 is 7.6e-6, so the last ~1000 days' hand-backs (each under half that
                // step) round away and leave ~1.2e-3 standing (n879: ends at 99.50116). At the floor (6a) the step is 6e-8 and nothing rounds away. The ledger records
                // the approval that actually moved, so ATTRIB's identity is exact either way.
                Check(Mathf.Abs(bookedAtCeiling + 0.5f) <= 1e-5f && lowest >= 99.5f - 1e-4f && Mathf.Abs(se.State.ApprovalRating - 99.5f) <= 2e-3f,
                    F("(6b) a cut of 4 at approval 99.5: T booked {0:R} (what landed, not -m*s*4 = {1}); the fading takes back at most that: lowest {2:F5}, ends at {3:F5}", bookedAtCeiling, -m * s * 4f, lowest, se.State.ApprovalRating));
            });
            WithSweden((sim, se) =>
            {
                Plant(sim, se, 1f);
                PassLevy(sim, se, TaxType.ExciseTax, true, out float levied);
                Check(se.State.ApprovalRating == 0f && Mathf.Abs(se.TaxTermStock - 1f) <= 1e-5f,
                    F("(6c) the excise levied at approval 1: approval {0:+0.0000;-0.0000}, T booked {1:R} (not s x its rate)", levied, se.TaxTermStock));
            });
            WithSweden((sim, se) =>
            {
                PassRate(sim, se, TaxType.VAT, vat + CutPoints);   // T = s*2
                float standing = se.TaxTermStock;
                Plant(sim, se, 100f);   // a shock that takes approval past the ceiling - the run without the rise stands there too
                float fadingBefore = se.ApprovalLedgerAccruing?.TaxTermFading ?? 0f;
                TaxTerm.AdvanceDay(se, sim.CurrentDate.AddDays(1));
                Check(se.State.ApprovalRating == 100f && se.TaxTermStock == 0f && standing > 0f && (se.ApprovalLedgerAccruing?.TaxTermFading ?? 0f) == fadingBefore,
                    F("(6d) a standing rise's T ({0:R}) under a shock to the ceiling: approval {1}, T {2:R} - forgiven, the run without the rise sits at the same bound (review 878b's M-5); nothing recorded", standing, se.State.ApprovalRating, se.TaxTermStock));
            });

            // (6e) the formula's own clamp: the player's VAT +5 at a boundary with approval planted at 0, against the same boundary restating VAT
            AtBoundary floorNone = AtFirstBoundary(0f, 0f), floorRise = AtFirstBoundary(5f, 0f);
            if (floorNone != null && floorRise != null)
            {
                float impulse = Plan(floorRise.NetBefore + 5f, s, m) - Plan(floorRise.NetBefore, s, m);
                float booked = floorRise.Stock - floorNone.Stock;
                // T and approval are both read after the WHOLE boundary: the year's event and a cabinet event land after the formula, through
                // TaxTerm.Shock (review 879b's M-1), and may take part of the booked term with them where they meet the floor - so approval + T is
                // compared at that one instant, never the formula's approval against the boundary's closing T (n879b's (6e) failure was that mix)
                float baseGap = (floorRise.Approval + floorRise.Stock) - (floorNone.Approval + floorNone.Stock);
                Check(floorRise.AfterFormula == 0f && booked < impulse - 0.01f && booked >= 0f && Mathf.Abs(baseGap) <= Tolerance,
                    F("(6e) VAT +5 at a boundary from approval 0: the formula left {0:F4} (restating: {1:F4}); after the boundary approval {5:F4} (restating: {6:F4}) and T {2:F4} of the impulse {3:F4} - what landed; approval + T against the restating run {4:+0.00000;-0.00000}",
                        floorRise.AfterFormula, floorNone.AfterFormula, booked, impulse, baseGap, floorRise.Approval, floorNone.Approval));
            }

            // (6f) review 878b's M-5, through a bound in two runs each: a move, then a shock past the bound in both; ten years on the move run is the no-move run
            foreach (bool ceiling in new[] { true, false })
            {
                float[] moveRun = ThroughABound(ceiling, true, vat), noMoveRun = ThroughABound(ceiling, false, vat);
                float worst = 0f;
                for (int i = 0; i < moveRun.Length; i++) { worst = Mathf.Max(worst, Mathf.Abs(moveRun[i] - noMoveRun[i])); }
                float last = moveRun[moveRun.Length - 1] - noMoveRun[noMoveRun.Length - 1];
                Check(worst <= 1e-4f && Mathf.Abs(last) <= 0.01f,
                    F("(6f) {0}: the largest daily gap between the move run and the no-move run in the {1} years after the shock {2:F6}; at the end {3:+0.00000;-0.00000}",
                        ceiling ? "a rise of 2 at 50, then a shock past the ceiling in both runs (the gift M-5 found)" : "a cut of 4 at 2, then a shock past the floor in both runs (the debt M-5 found)", LongRunYears, worst, last));
            }
        }

        /// <summary>(6f): Sweden at the seed, the move (or none), a shock of 60 points past the bound recorded as a harness must, then ten years of the tax
        /// term's day and the yearly formula as the boundary runs them (`TaxTerm.Reclamp`, the formula, its twin, `TaxTerm.BookFormula`); approval each day.</summary>
        private static float[] ThroughABound(bool ceiling, bool move, float vat)
        {
            (SimulationManager sim, World world, GameObject go) = Open();
            try
            {
                Country c = world.GetCountry(CountryId.Sweden);
                Plant(sim, c, ceiling ? 50f : 2f);
                if (move) { PassRate(sim, c, TaxType.VAT, ceiling ? vat + CutPoints : vat - 4f); }
                float before = c.State.ApprovalRating;
                c.State.ApprovalRating = Mathf.Clamp(before + (ceiling ? 60f : -60f), 0f, 100f);
                ApprovalLedgerRecorder.RecordEvent(c, sim.CurrentDate, "shock (TaxTermDiagnostic)", c.State.ApprovalRating - before);
                var path = new float[LongRunYears * SimulationManager.DaysPerTurn];
                for (int d = 1; d <= path.Length; d++)
                {
                    DateTime day = sim.CurrentDate.AddDays(d);
                    TaxTerm.AdvanceDay(c, day);
                    MacroSystem.ApplyApprovalDay(c, c.PotentialGrowthRate, day);   // MF-13 (K191): approval's daily step, as the game's day runs it
                    if (d % SimulationManager.DaysPerTurn == 0)
                    {
                        TaxTerm.Reclamp(c);
                        float a0 = c.State.ApprovalRating;
                        MacroSystem.ApplyApprovalRating(c, PolicyDecision.None(), 0f, 0f);
                        MacroSystem.RecordApprovalAttribution(c, PolicyDecision.None(), 0f, 0f, day, a0);
                        TaxTerm.BookFormula(c, 0f, a0, c.ApprovalLedgerAccruing);
                    }
                    path[d - 1] = c.State.ApprovalRating;
                }
                return path;
            }
            finally { Object.DestroyImmediate(go); }
        }

        /// <summary>Approval set to <paramref name="to"/> and the write recorded on the ledger, as a harness must.</summary>
        private static void Plant(SimulationManager sim, Country c, float to)
        {
            float delta = to - c.State.ApprovalRating;
            c.State.ApprovalRating = to;
            ApprovalLedgerRecorder.RecordEvent(c, sim.CurrentDate, "planted (TaxTermDiagnostic)", delta);
        }

        /// <summary>The tax term's daily step alone for ten years, nothing else writing approval; returns the highest approval, and the lowest by out.</summary>
        private static float Fade(SimulationManager sim, Country c, out float lowest)
        {
            float highest = c.State.ApprovalRating;
            lowest = c.State.ApprovalRating;
            for (int d = 1; d <= LongRunYears * SimulationManager.DaysPerTurn; d++)
            {
                TaxTerm.AdvanceDay(c, sim.CurrentDate.AddDays(d));
                highest = Mathf.Max(highest, c.State.ApprovalRating);
                lowest = Mathf.Min(lowest, c.State.ApprovalRating);
            }
            return highest;
        }

        // ============================================================ (6g) a shock against a standing T, at a bound
        private static void AgainstT()
        {
            _sb.Append("    6g. a shock that meets a bound against a standing T (review 879b's M-1), through TaxTerm.Shock\n");
            float vat = SeedRate(TaxType.VAT);
            foreach (bool rise in new[] { true, false })
            {
                float[] moveRun = AcrossAShock(rise, true, vat), noMoveRun = AcrossAShock(rise, false, vat);
                float worst = 0f;
                for (int i = 0; i < moveRun.Length; i++) { worst = Mathf.Max(worst, Mathf.Abs(moveRun[i] - noMoveRun[i])); }
                float last = moveRun[moveRun.Length - 1] - noMoveRun[noMoveRun.Length - 1];
                float firstGap = moveRun[0] - noMoveRun[0];
                // after the shock both runs stand at the bound's side of it; the move run sits below (a rise) or above (a cut) the no-move run by what the
                // standing term still holds, then the term fades - the path never crosses to the wrong side and ends on the no-move run
                bool rightSide = true;
                for (int i = 0; i < moveRun.Length; i++) { if (rise ? moveRun[i] > noMoveRun[i] + 1e-4f : moveRun[i] < noMoveRun[i] - 1e-4f) { rightSide = false; break; } }
                Check(rightSide && Mathf.Abs(last) <= 0.01f,
                    F("(6g) {0}: the day after the shock the move run stands {1:+0.0000;-0.0000} off the no-move run; it never crosses to the {2} side in {3} years (largest gap {4:F4}); at the end {5:+0.00000;-0.00000}",
                        rise ? "a rise of 2 at approval 4, then a shock of -2 past the floor (the gift M-1 found)" : "a cut of 4 at approval 96, then a shock of +2 past the ceiling (the debt M-1 found)",
                        firstGap, rise ? "upper" : "lower", LongRunYears, worst, last));
            }
        }

        /// <summary>(6g): Sweden at the seed planted at 4 (or 96), the move (a VAT rise of 2, or a cut of 4) or none, a shock of −2 (or +2) through
        /// `TaxTerm.Shock` recorded as a harness must, then ten years of the tax term's day and the yearly formula as the boundary runs them; approval each day.</summary>
        private static float[] AcrossAShock(bool rise, bool move, float vat)
        {
            (SimulationManager sim, World world, GameObject go) = Open();
            try
            {
                Country c = world.GetCountry(CountryId.Sweden);
                Plant(sim, c, rise ? 4f : 96f);
                if (move) { PassRate(sim, c, TaxType.VAT, rise ? vat + CutPoints : vat - 4f); }
                float before = c.State.ApprovalRating;
                TaxTerm.Shock(c, rise ? -2f : 2f);
                ApprovalLedgerRecorder.RecordEvent(c, sim.CurrentDate, "shock (TaxTermDiagnostic)", c.State.ApprovalRating - before);
                var path = new float[LongRunYears * SimulationManager.DaysPerTurn];
                for (int d = 1; d <= path.Length; d++)
                {
                    DateTime day = sim.CurrentDate.AddDays(d);
                    TaxTerm.AdvanceDay(c, day);
                    MacroSystem.ApplyApprovalDay(c, c.PotentialGrowthRate, day);   // MF-13 (K191): approval's daily step, as the game's day runs it
                    if (d % SimulationManager.DaysPerTurn == 0)
                    {
                        TaxTerm.Reclamp(c);
                        float a0 = c.State.ApprovalRating;
                        MacroSystem.ApplyApprovalRating(c, PolicyDecision.None(), 0f, 0f);
                        MacroSystem.RecordApprovalAttribution(c, PolicyDecision.None(), 0f, 0f, day, a0);
                        TaxTerm.BookFormula(c, 0f, a0, c.ApprovalLedgerAccruing);
                    }
                    path[d - 1] = c.State.ApprovalRating;
                }
                return path;
            }
            finally { Object.DestroyImmediate(go); }
        }

        // ============================================================ (7) the reversion
        private static void Reversion(float s)
        {
            _sb.Append("    7. the reversion (review 878's M-1): ten years of the daily fading, approval's daily step and the yearly formula, a rise against no move\n");
            float vat = SeedRate(TaxType.VAT);
            (SimulationManager simA, World worldA, GameObject goA) = Open();
            (SimulationManager simB, World worldB, GameObject goB) = Open();
            try
            {
                Country a = worldA.GetCountry(CountryId.Sweden), b = worldB.GetCountry(CountryId.Sweden);
                PassRate(simA, a, TaxType.VAT, vat + CutPoints);
                float worstAbove = float.NegativeInfinity, worstGap = 0f;
                for (int d = 1; d <= LongRunYears * SimulationManager.DaysPerTurn; d++)
                {
                    DateTime day = simA.CurrentDate.AddDays(d);
                    TaxTerm.AdvanceDay(a, day);
                    TaxTerm.AdvanceDay(b, day);
                    MacroSystem.ApplyApprovalDay(a, a.PotentialGrowthRate, day);   // MF-13 (K191): approval's daily step, as the game's day runs it
                    MacroSystem.ApplyApprovalDay(b, b.PotentialGrowthRate, day);
                    if (d % SimulationManager.DaysPerTurn == 0)
                    {
                        MacroSystem.ApplyApprovalRating(a, PolicyDecision.None(), 0f, 0f);
                        MacroSystem.ApplyApprovalRating(b, PolicyDecision.None(), 0f, 0f);
                    }
                    float diff = a.State.ApprovalRating - b.State.ApprovalRating;
                    worstAbove = Mathf.Max(worstAbove, diff);
                    worstGap = Mathf.Max(worstGap, Mathf.Abs(diff + a.TaxTermStock));
                }
                float last = a.State.ApprovalRating - b.State.ApprovalRating;
                Check(worstAbove <= 1e-4f && worstGap <= Tolerance && Mathf.Abs(last) <= 0.01f,
                    F("(7) a rise of {0} (T {1:F4}): the most the rise's path stood above the no-move path in {2} years {3:+0.00000;-0.00000} (never above); the path less the no-move path against -T at most {4:F6} apart; after {2} years {5:+0.00000;-0.00000}",
                        CutPoints, s * CutPoints, LongRunYears, worstAbove, worstGap, last));
            }
            finally { Object.DestroyImmediate(goA); Object.DestroyImmediate(goB); }
        }

        // ============================================================ (8) the half-life, on the game's own days
        private static void HalfLife(float s)
        {
            _sb.Append("    8. the half-life, on the game's own days\n");
            double declared = Math.Log(0.5) / Math.Log(0.81) * (365.0 / 4.0);   // the plan's figures: 0.81 a quarter, a quarter of the game's 365-day year
            WithSweden((sim, se) =>
            {
                Days(sim, 1);
                PassRate(sim, se, TaxType.VAT, SeedRate(TaxType.VAT) + CutPoints);
                float stock = se.TaxTermStock;
                int toBoundary = (int)(SimulationManager.TurnBoundary(1) - sim.CurrentDate).TotalDays;
                int days = 0;
                while (se.TaxTermStock > 0.5f * stock && days < toBoundary - 1) { Days(sim, 1); days++; }
                Check(Mathf.Abs(stock - s * CutPoints) <= Tolerance && se.TaxTermStock <= 0.5f * stock && Math.Abs(days - declared) <= 10.0,
                    F("(8) a rise's T {0:F4} is at half its size after {1} days of the game (no boundary or bill between), the declared half-life {2:F1} days (+/-10)", stock, days, declared));
            });
        }

        // ============================================================ the harness's hands
        private static (SimulationManager, World, GameObject) Open()
        {
            SimulationRandom.Seed(777);
            EnergyMarket.ResetCalibration();
            World world = WorldFactory.CreateDefault();
            var go = new GameObject("TAXTERM");
            SimulationManager sim = go.AddComponent<SimulationManager>();
            sim.SetWorld(world);
            sim.PlayerCountryId = CountryId.Sweden;
            return (sim, world, go);
        }

        private static void WithSweden(Action<SimulationManager, Country> body)
        {
            (SimulationManager sim, World world, GameObject go) = Open();
            try { body(sim, world.GetCountry(CountryId.Sweden)); }
            finally { Object.DestroyImmediate(go); }
        }

        /// <summary>A world played to its first fiscal-year start (Sweden's 1 January), every boundary on the way resolved with no policy.</summary>
        private static SimulationManager Played(List<GameObject> hosts, out World world)
        {
            (SimulationManager sim, World w, GameObject go) = Open();
            hosts.Add(go);
            world = w;
            DateTime opening = new DateTime(sim.CurrentDate.Year + 1, 1, 1);
            Days(sim, (int)(opening - sim.CurrentDate).TotalDays);
            return sim;
        }

        /// <summary><paramref name="n"/> days of the game, each boundary resolved with no policy; every country's approval and T held in bounds every day.</summary>
        private static void Days(SimulationManager sim, int n)
        {
            var decisions = new Dictionary<CountryId, PolicyDecision>();
            foreach (Country k in sim.World.Countries) { decisions[k.Id] = PolicyDecision.None(); }
            for (int d = 0; d < n; d++)
            {
                if (sim.AdvanceDay()) { sim.AdvanceTurn(decisions); }
                foreach (Country k in sim.World.Countries)
                {
                    float a = k.State.ApprovalRating;
                    if (float.IsNaN(a) || a < 0f || a > 100f || float.IsNaN(k.TaxTermStock) || float.IsInfinity(k.TaxTermStock))
                    {
                        Check(false, F("bounds: {0} on {1:yyyy-MM-dd} - approval {2:R}, T {3:R}", k.Id, sim.CurrentDate, a, k.TaxTermStock));
                    }
                }
            }
        }

        /// <summary>A passed budget bill applied as the game applies it, its approval move recorded as the game's callers record it; returns the move.</summary>
        private static float Pass(SimulationManager sim, Country c, BudgetBill bill)
        {
            float before = c.State.ApprovalRating;
            ParliamentSystem.ApplyBillResult(c, bill, true, (k, b) => { }, sim.CurrentDate.Year);
            float delta = c.State.ApprovalRating - before;
            ApprovalLedgerRecorder.RecordEvent(c, sim.CurrentDate, "Budget bill passed (tax changes)", delta);
            return delta;
        }

        private static float PassRate(SimulationManager sim, Country c, TaxType type, float rate)
        {
            var bill = new BudgetBill();
            bill.TaxLines[type] = rate;
            return Pass(sim, c, bill);
        }

        /// <summary>A passed tax programme bill as `SimulationManager.AdvanceTaxProgramBillsDay` applies it - the flip, then the levy's charge where the
        /// government bears it - its approval move recorded as that caller records it.</summary>
        private static TaxLevyMove PassLevy(SimulationManager sim, Country c, TaxType type, bool add, out float delta)
        {
            float before = c.State.ApprovalRating;
            TaxLevyMove move = ParliamentSystem.ApplyTaxProgramBillResult(c, new TaxProgramBill { Type = type, IsAdd = add }, true);
            TaxTerm.ChargeLevyMove(c, move, true);
            delta = c.State.ApprovalRating - before;
            ApprovalLedgerRecorder.RecordEvent(c, sim.CurrentDate, EnumNames.ProgrammeBillWords(type, add) + " bill passed", delta);   // M4: the label as the caller now writes it
            return move;
        }

        private static float SeedRate(TaxType type)
        {
            SimulationRandom.Seed(777);
            TaxLine line = Line(WorldFactory.CreateDefault().GetCountry(CountryId.Sweden), type);
            return line != null ? line.Rate : float.NaN;
        }

        private static TaxLine Line(Country c, TaxType type)
        {
            foreach (TaxLine line in c.TaxLines) { if (line.Type == type) { return line; } }
            return null;
        }

        private static float[] SubRowRates(CountryId id, Func<TaxSchedule.SubRow, float> pick)
        {
            List<TaxSchedule.SubRow> rows = TaxSchedule.SubRows(TaxSchedule.Of(id));
            var rates = new float[rows.Count];
            for (int i = 0; i < rows.Count; i++) { rates[i] = pick(rows[i]); }
            return rates;
        }

        private static void Check(bool ok, string what)
        {
            _sb.Append(ok ? "        ok   " : "        FAIL ").Append(what).Append('\n');
            if (!ok) { _failures++; Debug.LogError("TAX TERM: FAIL - " + what); }
        }

        private static string F(string format, params object[] args) => string.Format(CultureInfo.InvariantCulture, format, args);
    }
}
