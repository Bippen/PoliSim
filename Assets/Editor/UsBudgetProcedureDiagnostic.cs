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

namespace PoliSim.EditorTools
{
    /// <summary>
    /// PS-6 US-22 (`docs/specs/USA_STAGE_PLAN.md` US-22; R-US9 (a) as ruled, F8): THE US BUDGET DIAGNOSTIC the done-when names, under the 119th of record, on
    /// worlds opened as a new game opens them (the epoch, the default world, the player's country the USA, the record seated through the day loop's own
    /// `SimulationManager.SeatTheRecordOnItsDate`) and stepped in the controller's order where the dates matter:
    /// <list type="bullet">
    /// <item>(a) the words held to their saved pages by SHA-256 - 31 U.S.C. 1102, 1105(a), 1341(a)(1) and 1342; 2 U.S.C. 631's timetable; 641(e)(2) and (g);
    /// 644(b)(1)(F); P.L. 119-4; H.R. 5371's dated actions; P.L. 119-37; CRS R48832 and CBO's letter by digest only - and the DERIVED request days and the
    /// record's 43 days;</item>
    /// <item>(b) W1, the REP player's own budget decided 2025-09-19 (the record's House-passage day): the rates pass by reconciliation, the appropriation
    /// dies at cloture and a continuing resolution carries the standing figures, the pension-age act is its own act at cloture;</item>
    /// <item>(c) W2, the DEM player withholding the continuing resolution against the AI government's budget (US-18's owed line: the AI president's budget
    /// meets Congress): the shutdown opens on 1 October 2025 and ends the day a resolution passes, 12 November - 43 days, by the procedure's own arithmetic;</item>
    /// <item>(d) the vetoes: a player-president's veto of a bipartisan appropriation overridden; an uncontested resolution vetoed and overridden on its whole
    /// seated base (US-21's rule, its consequence stated); a contested resolution's veto sustained, funding unadvanced; rule (a) never fires on the
    /// government's own acts;</item>
    /// <item>(e) N1: a budget pending across a change of presidency is charged as its lost day is marked (`Congress.GovernmentBears`);</item>
    /// <item>(f) R-US17's budget-vote re-run: every act and resolution under `UsVeto.PlantIndex` at n - 1 writes the unplanted records, nothing drawn;</item>
    /// <item>(g) the funding arithmetic and the request days, DERIVED;</item>
    /// <item>(h) scope: an AI USA votes nothing and is never seeded; the procedures enumerated;</item>
    /// <item>(i) the save: a mid-lapse save round-trips into the game at format 41; a format-40 save is refused.</item>
    /// </list>
    /// THE STOP OF NON-EXCEPTED SPENDING IS NOT MODELLED (DECLARED; asked of Elias): no case asserts a figure of the book in a lapse. The random streams are
    /// restored after, the epoch by its scope, the draw counter and every host.
    /// </summary>
    public static class UsBudgetProcedureDiagnostic
    {
        private const string Statutes = "ElectionsData/usa/raw/budget_statutes/";
        private const string Records = "ElectionsData/usa/raw/records/";
        private const string Lapse = "ElectionsData/usa/raw/lapse/";
        private static readonly (string Page, string[] Words)[] Pages =
        {
            (Statutes + "govinfo_USCODE-2024-title31-sec1102.htm", new[] { "begins on October 1 of each year and ends on September 30 of the following year" }),
            (Statutes + "govinfo_USCODE-2024-title31-sec1105.htm", new[] { "not later than the first Monday in February of each year, the President shall submit a budget" }),
            (Statutes + "govinfo_USCODE-2024-title2-sec631.htm", new[] { "First Monday in February", "President submits his budget", "Fiscal year begins" }),
            (Statutes + "govinfo_USCODE-2024-title31-sec1341.htm", new[] { "make or authorize an expenditure or obligation exceeding an amount available in an" }),
            (Statutes + "govinfo_USCODE-2024-title31-sec1342.htm", new[] { "emergencies involving the safety of human life or the protection of property" }),
            (Records + "govinfo_uscode_2_641_reconciliation.html", new[] { "shall be limited to not more than 20 hours", "old-age, survivors, and disability insurance program" }),
            (Records + "govinfo_uscode_2_644_byrd_rule.html", new[] { "shall be considered extraneous if it violates section 641(g) of this title" }),
            (Lapse + "govinfo_PLAW-119publ4.htm", new[] { "fiscal year ending September 30, 2025" }),
            (Lapse + "govinfo_PLAW-119publ37.htm", new[] { "January 30, 2026" }),
        };
        /// <summary>H.R. 5371's dated actions (BILLSTATUS), each held as one action item carrying both its date and its words.</summary>
        private static readonly (string Date, string Words)[] Actions =
        {
            ("2025-09-19", "On passage Passed by the Yeas and Nays: 217 - 212"),
            ("2025-09-19", "not having achieved 60 votes in the affirmative, failed of passage in Senate by Yea-Nay Vote. 44 - 48"),
            ("2025-10-01", "Cloture on the motion to proceed to the measure not invoked in Senate by Yea-Nay Vote. 55 - 45"),
            ("2025-11-09", "the fifth cloture motion on the motion to proceed to the measure invoked in Senate by Yea-Nay Vote. 60 - 40"),
            ("2025-11-10", "Passed Senate with an amendment by Yea-Nay Vote. 60 - 40"),
            ("2025-11-12", "On motion that the House agree to the Senate amendment Agreed to by the Yeas and Nays: 222 - 209"),
            ("2025-11-12", "Became Public Law No: 119-37."),
        };
        private static readonly string[] DigestOnly = { Lapse + "crs_R48832.2.pdf", Lapse + "house_arrington_61823_quantitative_effects_of_a_shutdown.pdf" };

        private static readonly DateTime LapseOpens = new DateTime(2025, 10, 1), LapseEnds = new DateTime(2025, 11, 12);
        private const int Seed = 777;

        private static readonly FieldInfo SlotField = typeof(SimulationManager).GetField("_pendingBudgetBillByCountry", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo UsedField = typeof(SimulationManager).GetField("_incomingBudgetWindowUsed", BindingFlags.Instance | BindingFlags.NonPublic);

        public static void Run()
        {
            CheckExit.ArmLogFold();
            var sb = new StringBuilder("=== UsBudgetProcedureDiagnostic (PS-6 US-22, R-US9 (a) as ruled): the US budget procedure ===\n");
            int failures = 0;
            void Check(bool ok, string what) { if (!ok) { failures++; } sb.Append(ok ? "    ok        " : "    FAIL      ").Append(what).Append('\n'); }
            var hosts = new List<GameObject>();
            int seedWas = SimulationRandom.MasterSeed;
            Dictionary<SimulationRandom.Stream, int> drawsWas = SimulationRandom.CaptureDrawCounts();
            try
            {
                Check(SlotField != null && UsedField != null, "the manager's pending budget slot and the arrival window's flag resolved (the planted government bill's staging)");

                // ================================================================ (a) the words, held to their pages
                foreach ((string page, string[] words) in Pages)
                {
                    bool held = PresidencyDiagnostic.Page(page, out string text);
                    Check(held && words.All(text.Contains), F("(a) {0} held to its SHA-256, holding \"{1}\"", System.IO.Path.GetFileName(page), string.Join("\" ... \"", words)));
                }
                bool billHeld = PresidencyDiagnostic.Page(Lapse + "govinfo_BILLSTATUS-119hr5371.xml", out string bill);
                List<string> items = billHeld ? bill.Split(new[] { "<item>" }, StringSplitOptions.None).ToList() : new List<string>();
                var missing = Actions.Where(a => !items.Any(i => i.Contains("<actionDate>" + a.Date + "</actionDate>") && i.Contains(a.Words))).Select(a => a.Date + " " + a.Words).ToList();
                Check(billHeld && missing.Count == 0, F("(a) H.R. 5371's BILLSTATUS held to its SHA-256, each of its {0} dated actions an action item of its own - the House 217-212 and the Senate 44-48 on 19 Sep, cloture not invoked 55-45 on 1 Oct, the fifth cloture motion invoked 60-40 on 9 Nov, the Senate 60-40 on 10 Nov, the House 222-209 and P.L. 119-37 on 12 Nov{1}",
                    Actions.Length, missing.Count == 0 ? string.Empty : " - MISSING: " + string.Join("; ", missing)));
                foreach (string page in DigestOnly) { Check(PresidencyDiagnostic.Page(page, out _), F("(a) {0} held by its SHA-256 alone (a PDF, read by digest - US-14's precedent)", System.IO.Path.GetFileName(page))); }
                var requestDays = new[] { new DateTime(2024, 2, 5), new DateTime(2025, 2, 3), new DateTime(2026, 2, 2), new DateTime(2027, 2, 1) };
                bool requestRight = requestDays.All(d => FiscalYearData.BudgetRequestDay(CountryId.USA, d.Year) == d && d.DayOfWeek == DayOfWeek.Monday && d.Day <= 7 && d.Month == 2)
                                    && Enumerable.Range(2020, 20).All(y => { DateTime r = FiscalYearData.BudgetRequestDay(CountryId.USA, y); return r.Month == 2 && r.DayOfWeek == DayOfWeek.Monday && r.Day <= 7; });
                Check(requestRight, F("(a) the request days, DERIVED (the first Monday in February, 1105(a)'s last day - DECLARED): {0}; every year 2020-2039 a Monday among February's first seven days", string.Join(", ", requestDays.Select(Day))));
                Check(UsBudgetProcedure.LapseDays(LapseOpens, LapseEnds) == 43 && UsBudgetProcedure.LapseDays(LapseOpens, LapseOpens) == 1,
                    F("(a) the record's lapse, by the count's own arithmetic: (2025-11-12 - 2025-10-01) + 1 = {0} days (F8: \"from 1 Oct to 12 Nov 2025 (43 days)\")", UsBudgetProcedure.LapseDays(LapseOpens, LapseEnds)));

                // ================================================================ (g) the arithmetic, and (h) the procedures enumerated
                Check(UsBudgetProcedure.FiscalYearEnd(new DateTime(2024, 3, 12)) == new DateTime(2024, 9, 30) && UsBudgetProcedure.FiscalYearEnd(new DateTime(2025, 9, 30)) == new DateTime(2025, 9, 30)
                      && UsBudgetProcedure.FiscalYearEnd(LapseOpens) == new DateTime(2026, 9, 30),
                    "(g) the fiscal year's end, DERIVED from 1102's start: 2024-03-12 in FY2024 (through 2024-09-30); 2025-09-30 its own year's last day; 2025-10-01 in FY2026");
                Check(UsBudgetProcedure.FundedThrough(new DateTime(2024, 9, 30), new DateTime(2024, 4, 2)) == new DateTime(2025, 9, 30)
                      && UsBudgetProcedure.FundedThrough(new DateTime(2025, 9, 30), new DateTime(2025, 10, 22)) == new DateTime(2026, 9, 30)
                      && UsBudgetProcedure.FundedThrough(new DateTime(2025, 9, 30), new DateTime(2025, 9, 19)) == new DateTime(2026, 9, 30)
                      && UsBudgetProcedure.FundedThrough(DateTime.MinValue, new DateTime(2025, 3, 1)) == new DateTime(2025, 9, 30),
                    "(g) the horizon after a measure enacted (DECLARED): funded on 2024-04-02, through 2025-09-30 (the next year); enacted in a lapse on 2025-10-22, through 2026-09-30 (the year in progress); enacted 2025-09-19, funded, through 2026-09-30; with no horizon, the year in progress");
                bool bounded = true, monotone = true;
                for (DateTime horizon = new DateTime(2024, 9, 30); horizon <= new DateTime(2027, 9, 30); horizon = horizon.AddYears(1))
                {
                    for (DateTime enacted = new DateTime(2024, 3, 12); enacted <= new DateTime(2027, 12, 31); enacted = enacted.AddDays(5))
                    {
                        DateTime after = UsBudgetProcedure.FundedThrough(horizon, enacted);
                        if (after < horizon) { monotone = false; }
                        if (after > UsBudgetProcedure.FiscalYearEnd(enacted).AddYears(1) && after != horizon) { bounded = false; }
                    }
                }
                Check(bounded && monotone, "(g) never further than the next fiscal year, and never back (every horizon 2024-2027 against every fifth day 2024-03-12 to 2027-12-31)");
                var procedures = ((CountryId[])Enum.GetValues(typeof(CountryId))).ToDictionary(c => c, WorldClock.BudgetProcedureOf);
                Check(procedures[CountryId.Sweden] == WorldClock.BudgetProcedure.RiksdagFrameDecision && procedures[CountryId.Poland] == WorldClock.BudgetProcedure.SejmBudgetActAndStatutes
                      && procedures[CountryId.USA] == WorldClock.BudgetProcedure.UsCongress && procedures[CountryId.Germany] == WorldClock.BudgetProcedure.Unsourced
                      && procedures[CountryId.France] == WorldClock.BudgetProcedure.Unsourced && procedures[CountryId.Italy] == WorldClock.BudgetProcedure.Unsourced
                      && procedures.Keys.All(c => WorldClock.CongressActs(c) == (c == CountryId.USA)) && !WorldClock.StatutePartsAreActs(CountryId.USA) && !WorldClock.WeighsAlternative(CountryId.USA),
                    "(h) the procedures: " + string.Join(", ", procedures.Select(kv => kv.Key + " " + kv.Value)) + " - Congress's acts in the USA alone, no Polish act and no alternative weighed there");
                Check(procedures.Keys.Where(c => c != CountryId.USA).All(c => FiscalYearData.BudgetRequestDay(c, 2025) == new DateTime(2025, FiscalYearData.GetFiscalYearStart(c).Month, FiscalYearData.GetFiscalYearStart(c).Day)),
                    "(g) every other country's request day is its fiscal-year start - the day its window opened before, unchanged");

                using (SimulationManager.EpochScope())
                {
                    try
                    {
                        // ============================================================ (b) W1: the REP player's own budget
                        (SimulationManager s1, World w1, Country u1) = UsCongressDiagnostic.OpenUsa(hosts, new DateTime(2025, 8, 29), "REP");
                        Check(UsBudgetProcedure.Applies(u1) && s1.PlayerGoverns(u1) && u1.AppropriatedThrough == new DateTime(2025, 9, 30) && u1.LapseSince == DateTime.MinValue && Congress.Sworn(u1.SecondChamberSeats) == 100 && Congress.ClotureLine(100) == 60,
                            F("(b) W1 opened 2025-08-29, the 119th seated (the House {0}, the Senate {1}), Trump of record: REP governs, Congress votes its budget as its acts; funded through {2} (seeded: the fiscal year in progress, DECLARED)", Seats(u1.ParliamentSeats), Seats(u1.SecondChamberSeats), Day(u1.AppropriatedThrough)));
                        TaxLine rate = u1.TaxLines.First(t => t.IsImplemented);
                        float rateWas = rate.Rate, transportWas = Line(u1, SpendingCategory.Transportation);
                        var w1Bill = new BudgetBill { PensionAgeSet = true, PensionAge = PensionAgeStatute.AgeInForce(u1, u1.CalendarYear) + 1f };
                        w1Bill.TaxLines[rate.Type] = Mathf.Clamp(rate.Rate - 2f, rate.MinRate, rate.MaxRate);
                        w1Bill.SpendingPercentChanges[SpendingCategory.Transportation] = -10f;
                        bool introduced = BudgetBillHarness.Introduce(s1, CountryId.USA, w1Bill);
                        int seen1 = Last(u1);
                        Step(s1, w1, new DateTime(2025, 9, 19));
                        List<DivisionRecord> acts1 = u1.Divisions.Entries.Where(r => r.Number > seen1).ToList();
                        DivisionRecord recH = Find(acts1, "Reconciliation: ", Congress.House), recS = Find(acts1, "Reconciliation: ", Congress.Senate);
                        DivisionRecord appH = Find(acts1, "Appropriation: ", Congress.House), appS = Find(acts1, "Appropriation: ", Congress.Senate);
                        DivisionRecord penH = Find(acts1, "Pension act: ", Congress.House), penS = Find(acts1, "Pension act: ", Congress.Senate);
                        BudgetBill resolution1 = s1.GetPendingBudgetBill(CountryId.USA);
                        Check(introduced && recH != null && recH.Passed && Count(recH) == (220, 215) && recS != null && recS.Passed && recS.Reconciliation && !recS.Cloture && Count(recS) == (53, 47)
                              && !acts1.Any(r => r.Title.StartsWith("Vetoed", StringComparison.Ordinal)) && Mathf.Approximately(u1.TaxLines.First(t => t.Type == rate.Type).Rate, w1Bill.TaxLines[rate.Type]),
                            F("(b) W1's reconciliation (\"{0}\"): the House {1}, the Senate by reconciliation {2} - no cloture, a majority; signed by rule (a) (the president's party cast no House nay), and the rate is law: {3} {4} to {5}",
                                recS?.Title, Describe(recH), Describe(recS), rate.Type, Num(rateWas), Num(u1.TaxLines.First(t => t.Type == rate.Type).Rate)));
                        Check(appH != null && appH.Passed && Count(appH) == (220, 215) && appS != null && !appS.Passed && appS.Cloture && appS.Required == 60 && Count(appS).For == 53
                              && Mathf.Approximately(Line(u1, SpendingCategory.Transportation), transportWas),
                            F("(b) W1's appropriation (\"{0}\"): the House {1}, then the Senate at cloture {2} - it dies, 53 for and 60 needed of 100; Transportation stands at {3}",
                                appS?.Title, Describe(appH), Describe(appS), Num(Line(u1, SpendingCategory.Transportation))));
                        Check(penH != null && penH.Passed && penS != null && !penS.Passed && penS.Cloture && !penS.Reconciliation && Mathf.Approximately(u1.PensionAgeOverride, -1f),
                            F("(b) W1's pension act (\"{0}\"), its own act at cloture (641(g); 644(b)(1)(F)): the House {1}, the Senate {2} - it dies, the pension age stands (no override)", penS?.Title, Describe(penH), Describe(penS)));
                        Check(resolution1 != null && resolution1.ContinuingResolution && !resolution1.GovernmentBill && resolution1.DaysRemaining == ParliamentSystem.BillDurationDays,
                            F("(b) W1: the appropriation fell, so a continuing resolution is tabled the same day by the government of the day (REP's), decided in {0} days", resolution1?.DaysRemaining));
                        int seenCr = Last(u1);
                        DateTime w1Lapse = DateTime.MinValue;
                        Step(s1, w1, new DateTime(2025, 10, 10), d => { if (d == LapseOpens) { w1Lapse = u1.LapseSince; } });
                        List<DivisionRecord> cr1 = u1.Divisions.Entries.Where(r => r.Number > seenCr && r.Title.StartsWith(UsBudgetProcedure.ContinuingResolutionWords, StringComparison.Ordinal)).ToList();
                        Check(w1Lapse == LapseOpens && cr1.Count == 2 && cr1.All(r => r.Passed && r.Sides.Count == 0) && cr1[0].Chamber == Congress.House && cr1[1].Chamber == Congress.Senate && cr1[1].Cloture
                              && u1.LapseSince == DateTime.MinValue && u1.AppropriatedThrough == new DateTime(2026, 9, 30) && s1.GetPendingBudgetBill(CountryId.USA) == null
                              && Mathf.Approximately(Line(u1, SpendingCategory.Transportation), transportWas),
                            F("(b) W1: the continuing resolution, asked nothing (no party withholds it), passes both chambers uncontested on 2025-10-10 (\"{0}\") and carries the standing figures - Transportation unchanged; funded through {1}. A resolution takes a bill's course (21 days), so one tabled on 19 Sep is law only on 10 Oct: funding lapsed on {2} and the shutdown ran 10 days, ended by it - the plan's \"no lapse on 1 Oct\" holds only for a budget decided by 9 Sep (DECLARED, reported)",
                                cr1.LastOrDefault()?.Title, Day(u1.AppropriatedThrough), Day(w1Lapse)));

                        // ============================================================ (c) W2: the DEM player withholds against the AI government's budget
                        (SimulationManager s2, World w2, Country u2) = UsCongressDiagnostic.OpenUsa(hosts, new DateTime(2025, 7, 29), "DEM");
                        ((HashSet<CountryId>)UsedField.GetValue(s2)).Add(CountryId.USA);   // the arrival window spent (DECLARED staging: the run starts mid-term)
                        var w2Bill = new BudgetBill { GovernmentBill = true, DaysRemaining = ParliamentSystem.BillDurationDays };
                        w2Bill.SpendingPercentChanges[SpendingCategory.Transportation] = -10f;
                        ((Dictionary<CountryId, BudgetBill>)SlotField.GetValue(s2))[CountryId.USA] = w2Bill;   // the government's bill, planted so its day is 2025-08-19
                        Check(!s2.PlayerGoverns(u2) && Congress.VotesBothChambers(u2, ParliamentSystem.GetBudgetBillConcern(u2, w2Bill)),
                            "(c) W2 opened 2025-07-29: the DEM player does not govern; the AI government's budget (Transportation -10 %, planted with its day on 2025-08-19) goes through both chambers - US-18's owed line closed");
                        var crSides = new List<string>();
                        var charges = new List<string>();
                        DateTime lapseOpened = DateTime.MinValue, lapseEnded = DateTime.MinValue;
                        int endedDays = 0;
                        bool withheld = false, released = false, retabled1001 = false, retabled1022 = false;
                        int seen2 = Last(u2);
                        Step(s2, w2, LapseEnds, d =>
                        {
                            if (d == new DateTime(2025, 8, 19)) { withheld = s2.WithholdContinuingResolution(CountryId.USA, true, out _); }
                            if (d == new DateTime(2025, 11, 11)) { released = s2.WithholdContinuingResolution(CountryId.USA, false, out _); }
                            if (d == LapseOpens) { lapseOpened = u2.LapseSince; retabled1001 = s2.GetPendingBudgetBill(CountryId.USA)?.ContinuingResolution == true; }
                            if (d == new DateTime(2025, 10, 22)) { retabled1022 = s2.GetPendingBudgetBill(CountryId.USA)?.ContinuingResolution == true && s2.GetPendingBudgetBill(CountryId.USA).DaysRemaining == ParliamentSystem.BillDurationDays; }
                            if (d == LapseEnds) { endedDays = u2.LapseSince == DateTime.MinValue ? UsBudgetProcedure.LapseDays(LapseOpens, d) : 0; lapseEnded = u2.LapseSince == DateTime.MinValue ? d : DateTime.MinValue; }
                        }, (d, approvalBefore, approvalAfter, written) =>
                        {
                            DivisionRecord crSenate = written.LastOrDefault(x => x.Title.StartsWith(UsBudgetProcedure.ContinuingResolutionWords, StringComparison.Ordinal));
                            if (crSenate != null) { crSides.Add(Day(d) + " " + string.Join(" then ", written.Where(x => x.Title.StartsWith(UsBudgetProcedure.ContinuingResolutionWords, StringComparison.Ordinal)).Select(Describe))); }
                            if (crSenate != null && !crSenate.Passed) { charges.Add(F("{0} {1:+0.00;-0.00} lost {2}", Day(d), approvalAfter - approvalBefore, u2.BillLostOn == d ? "marked" : "NOT MARKED")); }
                        });
                        List<DivisionRecord> all2 = u2.Divisions.Entries.Where(r => r.Number > seen2).ToList();
                        DivisionRecord appS2 = Find(all2, "Appropriation: ", Congress.Senate);
                        Check(appS2 != null && !appS2.Passed && appS2.Cloture && Count(appS2).For == 53 && Find(all2, "Appropriation: ", Congress.House)?.Passed == true && appS2.Date == new DateTime(2025, 8, 19),
                            F("(c) W2's appropriation, the government's (\"{0}\"), on 2025-08-19: the House {1}, the Senate at cloture {2} - it dies", appS2?.Title, Describe(Find(all2, "Appropriation: ", Congress.House)), Describe(appS2)));
                        List<DivisionRecord> crs = all2.Where(r => r.Title.StartsWith(UsBudgetProcedure.ContinuingResolutionWords, StringComparison.Ordinal)).ToList();
                        List<DivisionRecord> crSenates = crs.Where(r => r.Chamber == Congress.Senate).ToList();
                        DivisionRecord sep9 = crSenates.FirstOrDefault(r => r.Date == new DateTime(2025, 9, 9));
                        DivisionRecord sep9House = crs.FirstOrDefault(r => r.Date == new DateTime(2025, 9, 9) && r.Chamber == Congress.House);
                        Check(withheld && sep9House != null && sep9House.Passed && Count(sep9House) == (220, 215) && sep9 != null && !sep9.Passed && sep9.Cloture && Count(sep9) == (53, 47),
                            F("(c) W2: the DEM player withholds the continuing resolution (tabled 2025-08-19); on 2025-09-09 it passes the House {0} and fails the Senate at cloture {1} - the record's shape (H.R. 5371: the House 217-212, then the Senate 44-48, not 60)",
                                Describe(sep9House), Describe(sep9)));
                        bool noneBetween = !crs.Any(r => r.Date > new DateTime(2025, 9, 9) && r.Date < new DateTime(2025, 10, 22));
                        Check(lapseOpened == LapseOpens && retabled1001 && noneBetween,
                            F("(c) W2: nothing re-tabled before the lapse; funding lapses on {0} - the day after P.L. 119-4's horizon, 2025-09-30 - and the government tables a resolution that day (the cadence, DECLARED)", Day(lapseOpened)));
                        DivisionRecord oct22 = crSenates.FirstOrDefault(r => r.Date == new DateTime(2025, 10, 22));
                        DivisionRecord nov12 = crSenates.FirstOrDefault(r => r.Date == LapseEnds);
                        Check(oct22 != null && !oct22.Passed && retabled1022 && oct22.Title.Contains("on day 22 of the shutdown"),
                            F("(c) W2: still withheld, the resolution falls again on 2025-10-22 (\"{0}\") and is re-tabled the same day", oct22?.Title));
                        Check(released && nov12 != null && nov12.Passed && nov12.Sides.Count == 0 && lapseEnded == LapseEnds && endedDays == 43 && u2.AppropriatedThrough == new DateTime(2026, 9, 30) && u2.ContinuingResolutionWithheldBy == null,
                            F("(c) W2: released, the resolution passes both chambers uncontested on 2025-11-12 (\"{0}\") - THE SHUTDOWN ENDS AFTER {1} DAYS, beside the record's 43 (CRS R48832's gap 1 Oct to 12 Nov; P.L. 119-37 of 12 Nov) - by the procedure's arithmetic, not fitted; funded through {2}",
                                nov12?.Title, endedDays, Day(u2.AppropriatedThrough)));
                        Check(charges.Count == 2 && charges.All(c => c.Contains("-1.50") && c.EndsWith(" marked", StringComparison.Ordinal)),
                            "(c) W2: every failed resolution is the government's (its own bill, `Congress.GovernmentBears`) - each charged the failed vote's cost and its day marked lost while the player withholds (a consequence, stated): " + string.Join("; ", charges));
                        sb.Append("    printed   W2's resolutions: ").Append(string.Join(" | ", crSides)).Append('\n');
                        bool aSigns = all2.Any(r => r.Passed && r.Chamber == Congress.Senate) && !all2.Any(v => v.Title.StartsWith("Vetoed", StringComparison.Ordinal));
                        Check(aSigns, "(d) rule (a) never fires on the government's own acts: under Trump of record (an AI president) every act and resolution both chambers sent was signed - the president's party votes for its own government's bills");

                        // ============================================================ (i) the save, cut mid-lapse in a W2 world
                        (SimulationManager s3, World w3, Country u3) = UsCongressDiagnostic.OpenUsa(hosts, new DateTime(2025, 9, 20), "DEM");
                        ((HashSet<CountryId>)UsedField.GetValue(s3)).Add(CountryId.USA);
                        var w3Bill = new BudgetBill { GovernmentBill = true, DaysRemaining = 3 };
                        w3Bill.SpendingPercentChanges[SpendingCategory.Transportation] = -10f;
                        ((Dictionary<CountryId, BudgetBill>)SlotField.GetValue(s3))[CountryId.USA] = w3Bill;
                        Step(s3, w3, new DateTime(2025, 9, 23));
                        bool held3 = s3.WithholdContinuingResolution(CountryId.USA, true, out string why3);
                        Step(s3, w3, new DateTime(2025, 10, 15));
                        BudgetBill pending3 = s3.GetPendingBudgetBill(CountryId.USA);
                        string json = SaveGameService.Serialize(SaveGameService.CreateSaveGame(s3, w3, CountryId.USA, null));
                        SaveGame cut = SaveGameService.Deserialize(json);
                        Country saved = cut.World.GetCountry(CountryId.USA);
                        BudgetBill savedBill = cut.Sim.PendingBudgetBills.TryGetValue(CountryId.USA, out BudgetBill sbill) ? sbill : null;
                        Check(held3 && u3.LapseSince == LapseOpens && pending3 != null && pending3.ContinuingResolution && cut.SaveVersion == 41 && cut.SaveVersion == SaveGameService.CurrentSaveVersion
                              && saved.LapseSince == u3.LapseSince && saved.AppropriatedThrough == u3.AppropriatedThrough && saved.ContinuingResolutionWithheldBy == "DEM"
                              && savedBill != null && savedBill.ContinuingResolution && savedBill.GovernmentBill && savedBill.DaysRemaining == pending3.DaysRemaining,
                            F("(i) a save cut mid-lapse on 2025-10-15 carries format {0}, the lapse ({1}), the horizon ({2}), the withheld resolution ({3}) and the pending resolution (its flag, the government's, {4} days left){5}",
                                cut.SaveVersion, Day(saved.LapseSince), Day(saved.AppropriatedThrough), saved.ContinuingResolutionWithheldBy ?? "none", savedBill?.DaysRemaining, held3 ? string.Empty : " - WITHHOLD REFUSED: " + why3));
                        (SimulationManager adopted, PoliSim.UI.GameController _) = PresidencyDiagnostic.Adopt(cut, hosts, out string error);
                        if (adopted == null) { Check(false, "(i) the save loaded into the game: " + error); }
                        else
                        {
                            Country u = adopted.World.GetCountry(CountryId.USA);
                            BudgetBill loaded = adopted.GetPendingBudgetBill(CountryId.USA);
                            Check(u.LapseSince == LapseOpens && u.AppropriatedThrough == new DateTime(2025, 9, 30) && u.ContinuingResolutionWithheldBy == "DEM" && loaded != null && loaded.ContinuingResolution && loaded.DaysRemaining == pending3.DaysRemaining,
                                F("(i) loaded INTO THE GAME (the controller's RestoreFromSave): the lapse, the horizon, the withheld resolution and the pending resolution kept as saved - not re-seeded ({0}, day {1} of the shutdown)",
                                    UsBudgetProcedure.FundedWords(u), UsBudgetProcedure.LapseDays(u.LapseSince, adopted.CurrentDate)));
                        }
                        string older = json.Replace("\"SaveVersion\": 41", "\"SaveVersion\": 40");
                        bool refused = false; string refusedWhy = null;
                        try { SaveGameService.Deserialize(older); } catch (SaveLoadException e) { refused = true; refusedWhy = e.Message; }
                        Check(older != json && refused && refusedWhy.Contains("format version 40"), F("(i) a format-40 save is refused with the plain message (ruling A): \"{0}\"", refusedWhy));

                        // ============================================================ (d) the vetoes: a planted game-elected REP president, the player REP
                        (SimulationManager s4, World w4, Country u4) = UsCongressDiagnostic.OpenUsa(hosts, UsCongressDiagnostic.Day119, "REP");
                        var trump = new UsPresidentialElection.Contest { PollingDay = new DateTime(2024, 11, 5), TakesOffice = new DateTime(2025, 1, 20), Winner = "Donald J. Trump", WinnerParty = "REP", ElectorsAppointed = 538, Line = "planted (UsBudgetProcedureDiagnostic)" };
                        trump.Electors.Add(new UsPresidentialElection.Tally { Candidate = "Donald J. Trump", Party = "REP", Votes = 312 });
                        trump.Electors.Add(new UsPresidentialElection.Tally { Candidate = "Kamala D. Harris", Party = "DEM", Votes = 226 });
                        u4.UsPresidentialElections.Add(trump);
                        u4.Government = UsPresidentialElection.GovernmentOf(trump, trump.TakesOffice);
                        bool playerPresident = Congress.PresidentOn(u4, s4.CurrentDate, out _, out string pp4, out bool isPlayer) && isPlayer && pp4 == "REP";
                        // a bipartisan appropriation, vetoed by the player-president and overridden on its recorded sides
                        var bipartisan = new BudgetBill();
                        bipartisan.SpendingPercentChanges[SpendingCategory.Transportation] = 10f;
                        int seen4 = Last(u4);
                        DateTime funded4 = u4.AppropriatedThrough;
                        Resolve(s4, CountryId.USA, bipartisan, r => r.Title.StartsWith("Appropriation: ", StringComparison.Ordinal));
                        List<DivisionRecord> veto4 = u4.Divisions.Entries.Where(r => r.Number > seen4).ToList();
                        DivisionRecord overrideLast = veto4.LastOrDefault(r => r.Title.StartsWith("Vetoed", StringComparison.Ordinal));
                        Check(playerPresident && overrideLast != null && overrideLast.Passed && overrideLast.Title.Contains("(you, ") && overrideLast.Title.Contains("two thirds in each chamber, so it becomes law")
                              && s4.GetPendingBudgetBill(CountryId.USA) == null && u4.AppropriatedThrough == UsBudgetProcedure.FundedThrough(funded4, s4.CurrentDate),
                            F("(d) a planted game-elected Trump, the player's party REP - the player-president: REP's bipartisan appropriation (Transportation +10 %, both blocs for, cloture invoked) vetoed through the delegate is overridden on its recorded sides - \"{0}\"; it is law and funds the government (through {1})",
                                overrideLast?.Title, Day(u4.AppropriatedThrough)));
                        // the uncontested resolution: a failed appropriation's resolution vetoed by the player-president - overridden on the whole seated base, so law
                        var cut4 = new BudgetBill();
                        cut4.SpendingPercentChanges[SpendingCategory.Transportation] = -10f;
                        Resolve(s4, CountryId.USA, cut4, null);
                        BudgetBill cr4 = s4.GetPendingBudgetBill(CountryId.USA);
                        int seenCr4 = Last(u4);
                        DateTime fundedCr4 = u4.AppropriatedThrough;
                        Resolve(s4, CountryId.USA, null, r => r.Title.StartsWith(UsBudgetProcedure.ContinuingResolutionWords, StringComparison.Ordinal));
                        List<DivisionRecord> crVeto = u4.Divisions.Entries.Where(r => r.Number > seenCr4).ToList();
                        DivisionRecord crOverride = crVeto.LastOrDefault(r => r.Title.StartsWith("Vetoed", StringComparison.Ordinal));
                        Check(cr4 != null && cr4.ContinuingResolution && crOverride != null && crOverride.Passed && crOverride.Title.Contains("so it becomes law") && crOverride.Sworn == Congress.Sworn(u4.SecondChamberSeats)
                              && u4.AppropriatedThrough == UsBudgetProcedure.FundedThrough(fundedCr4, s4.CurrentDate),
                            F("(d) an uncontested continuing resolution vetoed by the player-president is OVERRIDDEN ON ITS WHOLE SEATED BASE (US-21's rule, `Congress.OverrideCount`: every member of each chamber for it) - \"{0}\" - so a player-president cannot veto an uncontested resolution into a shutdown (the consequence, stated)", crOverride?.Title));
                        // a contested resolution's veto sustained - the Senate planted REP 61 of 100 and DEM's withholding planted for this case alone: funding unadvanced
                        Resolve(s4, CountryId.USA, cut4, null);
                        BudgetBill cr5 = s4.GetPendingBudgetBill(CountryId.USA);
                        Dictionary<string, int> senateWas = u4.SecondChamberSeats;
                        DateTime fundedCr5 = u4.AppropriatedThrough;
                        int seenCr5 = Last(u4);
                        bool sustained = false;
                        try
                        {
                            u4.SecondChamberSeats = new Dictionary<string, int> { { "REP", 61 }, { "DEM", 39 } };
                            u4.ContinuingResolutionWithheldBy = "DEM";
                            u4.BillLostOn = DateTime.MinValue;
                            Resolve(s4, CountryId.USA, null, r => r.Title.StartsWith(UsBudgetProcedure.ContinuingResolutionWords, StringComparison.Ordinal));
                            List<DivisionRecord> crSustained = u4.Divisions.Entries.Where(r => r.Number > seenCr5).ToList();
                            DivisionRecord sustain = crSustained.LastOrDefault(r => r.Title.StartsWith("Vetoed", StringComparison.Ordinal));
                            sustained = cr5 != null && cr5.ContinuingResolution && crSustained.Any(r => r.Chamber == Congress.Senate && r.Cloture && r.Passed) && sustain != null && !sustain.Passed && sustain.Title.Contains("the veto stands")
                                        && u4.AppropriatedThrough == fundedCr5 && u4.BillLostOn == s4.CurrentDate.Date;
                            Check(sustained, F("(d) a contested resolution (DEM's withholding and a REP Senate of 61 planted for this case alone) passes both chambers, cloture invoked; vetoed by the player-president, the veto stands - \"{0}\" - and funding is NOT advanced (through {1}); the government's own resolution, so its day is marked lost",
                                sustain?.Title, Day(u4.AppropriatedThrough)));
                        }
                        finally { u4.SecondChamberSeats = senateWas; u4.ContinuingResolutionWithheldBy = null; }

                        // ============================================================ (e) N1: a DEM budget introduced under Biden, decided under Trump
                        (SimulationManager s5, World w5, Country u5) = UsCongressDiagnostic.OpenUsa(hosts, new DateTime(2025, 1, 10), "DEM");
                        var raise = UsCongressDiagnostic.RatesBill(u5, "DEM");
                        bool n1Introduced = s5.PlayerGoverns(u5) && BudgetBillHarness.Introduce(s5, CountryId.USA, raise);
                        float n1Before = float.NaN, n1After = float.NaN;
                        int seen5 = Last(u5);
                        u5.BillLostOn = DateTime.MinValue;
                        Step(s5, w5, new DateTime(2025, 1, 31), null, (d, before, after, written) => { if (d == new DateTime(2025, 1, 31)) { n1Before = before; n1After = after; } });
                        DivisionRecord n1House = Find(u5.Divisions.Entries.Where(r => r.Number > seen5).ToList(), "Reconciliation: ", Congress.House);
                        Check(n1Introduced && !s5.PlayerGoverns(u5) && n1House != null && !n1House.Passed && n1After == n1Before && u5.BillLostOn == DateTime.MinValue,
                            F("(e) N1: DEM's own budget (a rate raised) introduced 2025-01-10 under Biden of record and decided 2025-01-31 under Trump: its reconciliation dies in the REP House ({0}) and charges the government nothing (approval {1} before the day's acts and after) and marks no day - the bill is no longer the government's (`Congress.GovernmentBears`); before US-22 it was charged with no day marked",
                                Describe(n1House), Num(n1Before)));
                        Country sweden = w5.GetCountry(CountryId.Sweden);
                        float swedenBefore = sweden.State.ApprovalRating;
                        ParliamentSystem.ApplyBillResult(sweden, new BudgetBill(), false, (c, b) => { }, 2026);
                        Check(Mathf.Approximately(swedenBefore - sweden.State.ApprovalRating, Mathf.Min(swedenBefore, ParliamentSystem.BillFailedApprovalCost)),
                            F("(e) N1: a failed budget elsewhere still charges the failed vote's cost, the default (Sweden: {0} to {1})", Num(swedenBefore), Num(sweden.State.ApprovalRating)));

                        // ============================================================ (f) R-US17's budget-vote re-run: planted at n - 1, nothing drawn, the records the unplanted run's
                        UsVeto.ResetDrawCalls();
                        List<DivisionRecord> plain = ReRun(hosts, planted: false), planted = ReRun(hosts, planted: true);
                        Check(plain.Count > 0 && SameRecords(plain, planted) && UsVeto.DrawCalls == 0 && plain.Any(r => r.Title.StartsWith(UsBudgetProcedure.ContinuingResolutionWords, StringComparison.Ordinal) && r.Sides.Count > 0),
                            F("(f) R-US17's budget-vote re-run (installed at US-21, re-run here): every act and the withheld resolution, {0} records, through the gate under UsVeto.PlantIndex at n - 1 identical to the unplanted run's; UsVeto refuses each - the counter {1}",
                                plain.Count, UsVeto.DrawCalls));

                        // ============================================================ (h) scope: an AI USA in a Sweden player's world
                        SimulationRandom.Seed(Seed);
                        EnergyMarket.ResetCalibration();
                        SimulationManager.SetEpoch(WorldClock.StartDate(CountryId.Sweden));
                        World ws = WorldFactory.CreateDefault();
                        var gos = new GameObject("UsBudgetProcedureDiagnostic.sweden"); hosts.Add(gos);
                        SimulationManager ss = gos.AddComponent<SimulationManager>();
                        ss.SetWorld(ws);
                        ss.PlayerCountryId = CountryId.Sweden;
                        ws.GetCountry(CountryId.Sweden).PlayerPartyAbbrev = "S";
                        Country aiUsa = ws.GetCountry(CountryId.USA);
                        for (int i = 0; i < 3; i++) { ss.AdvanceDay(); foreach (Country c in ws.Countries) { ss.AdvanceCountryDayTick(c.Id); } }
                        Check(WorldClock.CongressActs(CountryId.USA) && !UsBudgetProcedure.Applies(aiUsa) && aiUsa.AppropriatedThrough == DateTime.MinValue && aiUsa.LapseSince == DateTime.MinValue && ss.GetPendingBudgetBill(CountryId.USA) == null,
                            "(h) an AI USA (a Sweden player's world, three days stepped): the procedure is Congress's acts, but no Senate is seated, so nothing is voted and no horizon is seeded - the dump and the sentinel cannot move (the plan's risk 19)");
                    }
                    finally { EnergyMarket.ResetTurnState(); }
                }
            }
            catch (Exception e) { failures++; sb.Append("    FAIL      threw - ").Append(e.GetType().Name).Append(": ").Append(e.Message).Append('\n').Append(e.StackTrace).Append('\n'); }
            finally
            {
                foreach (GameObject go in hosts) { if (go != null) { UnityEngine.Object.DestroyImmediate(go); } }
                SimulationRandom.RestoreState(seedWas, drawsWas);
                UsVeto.ResetDrawCalls();
            }

            sb.Append(failures == 0 ? "=== UsBudgetProcedureDiagnostic: the US budget procedure holds - the acts, the continuing resolution, the lapse and its 43 days, the vetoes, N1 ===\n"
                                    : F("=== UsBudgetProcedureDiagnostic: {0} FAILURE(S) ===\n", failures));
            if (failures == 0) { Debug.Log(sb.ToString()); CheckExit.Finish(0); }
            else { Debug.LogError(sb.ToString()); CheckExit.Finish(1); }
        }

        // ------------------------------------------------------------------ the day loop, as the controller runs it
        /// <summary>Steps <paramref name="s"/> to <paramref name="until"/> in the controller's order (the day, the player's day tick, the boundary's turn), calling
        /// <paramref name="each"/> after each day and <paramref name="around"/> with the approval before and after the day tick and the records it wrote.</summary>
        private static void Step(SimulationManager s, World w, DateTime until, Action<DateTime> each = null, Action<DateTime, float, float, List<DivisionRecord>> around = null)
        {
            Country u = w.GetCountry(CountryId.USA);
            while (s.CurrentDate < until)
            {
                bool boundary = s.AdvanceDay();
                int seen = Last(u);
                float before = u.State.ApprovalRating;
                s.AdvanceCountryDayTick(CountryId.USA);
                float after = u.State.ApprovalRating;
                if (boundary)
                {
                    var none = new Dictionary<CountryId, PolicyDecision>();
                    foreach (Country c in w.Countries) { none[c.Id] = PolicyDecision.None(); }
                    s.AdvanceTurn(none);
                }
                around?.Invoke(s.CurrentDate, before, after, u.Divisions.Entries.Where(r => r.Number > seen).ToList());
                each?.Invoke(s.CurrentDate);
            }
        }

        /// <summary>A bill decided on the day, the date held: <paramref name="bill"/> introduced (null where one is already pending), then the manager's own day
        /// call run to its day, the player-president answering by <paramref name="vetoes"/>.</summary>
        private static void Resolve(SimulationManager s, CountryId id, BudgetBill bill, Func<DivisionRecord, bool> vetoes)
        {
            Func<DivisionRecord, bool> was = s.PlayerPresidentVetoes;
            s.PlayerPresidentVetoes = vetoes;
            try
            {
                if (bill != null) { BudgetBillHarness.Introduce(s, id, bill); }
                BudgetBill pending = s.GetPendingBudgetBill(id);
                for (int i = 0; i <= ParliamentSystem.BillDurationDays && pending != null && ReferenceEquals(s.GetPendingBudgetBill(id), pending); i++) { s.AdvanceBudgetBillDay(id); }
            }
            finally { s.PlayerPresidentVetoes = was; }
        }

        /// <summary>(f)'s run: W1's budget and a withheld resolution decided on 2025-02-03 (the 119th, Trump of record) - planted at n - 1 or not; the records written.</summary>
        private static List<DivisionRecord> ReRun(List<GameObject> hosts, bool planted)
        {
            (SimulationManager s, World w, Country u) = UsCongressDiagnostic.OpenUsa(hosts, UsCongressDiagnostic.Day119, "REP");
            int seen = Last(u);
            IDisposable plant = planted ? UsVeto.PlantIndex((chamber, party, n) => n - 1) : null;
            try
            {
                TaxLine rate = u.TaxLines.First(t => t.IsImplemented);
                var bill = new BudgetBill { PensionAgeSet = true, PensionAge = PensionAgeStatute.AgeInForce(u, u.CalendarYear) + 1f };
                bill.TaxLines[rate.Type] = Mathf.Clamp(rate.Rate - 2f, rate.MinRate, rate.MaxRate);
                bill.SpendingPercentChanges[SpendingCategory.Transportation] = -10f;
                Resolve(s, CountryId.USA, bill, null);
                u.ContinuingResolutionWithheldBy = "DEM";   // planted: the resolution contested, so the gate is asked to draw on it (it refuses a budget vote)
                Resolve(s, CountryId.USA, null, null);
            }
            finally { plant?.Dispose(); u.ContinuingResolutionWithheldBy = null; }
            return u.Divisions.Entries.Where(r => r.Number > seen).ToList();
        }

        // ------------------------------------------------------------------ small readers
        private static int Last(Country u) => u.Divisions.Entries.Count > 0 ? u.Divisions.Entries.Last().Number : 0;

        private static DivisionRecord Find(List<DivisionRecord> records, string head, int chamber) => records.FirstOrDefault(r => r.Title.StartsWith(head, StringComparison.Ordinal) && r.Chamber == chamber);

        private static (int For, int Against) Count(DivisionRecord r) => r == null ? (-1, -1) : (r.Sides.Where(x => x.Side > 0).Sum(x => x.Seats), r.Sides.Where(x => x.Side < 0).Sum(x => x.Seats));

        private static float Line(Country c, SpendingCategory category) => c.SpendingLines.First(l => l.Category == category).Amount;

        private static string Describe(DivisionRecord r)
        {
            if (r == null) { return "NONE"; }
            (int f, int a) = Count(r);
            return (r.Passed ? "passes" : "rejects") + (r.Sides.Count == 0 ? " (nothing asked, uncontested)" : F(" {0}-{1}", f, a)) + (r.Cloture ? F(" at cloture ({0} needed of {1})", r.Required, r.Sworn) : r.Chamber == Congress.Senate ? " by reconciliation" : string.Empty);
        }

        private static bool SameRecords(List<DivisionRecord> a, List<DivisionRecord> b)
        {
            if (a.Count != b.Count) { return false; }
            for (int i = 0; i < a.Count; i++)
            {
                DivisionRecord x = a[i], y = b[i];
                if (x.Title != y.Title || x.Passed != y.Passed || x.Chamber != y.Chamber || x.Origin != y.Origin || x.Required != y.Required || x.Cloture != y.Cloture || x.Sworn != y.Sworn || x.Motion != y.Motion
                    || x.Reconciliation != y.Reconciliation || x.VicePresidentVote != y.VicePresidentVote || x.Alignment != y.Alignment || x.Sides.Count != y.Sides.Count) { return false; }
                for (int s = 0; s < x.Sides.Count; s++)
                {
                    if (x.Sides[s].Abbrev != y.Sides[s].Abbrev || x.Sides[s].Seats != y.Sides[s].Seats || x.Sides[s].Side != y.Sides[s].Side || x.Sides[s].Defectors != y.Sides[s].Defectors) { return false; }
                }
            }
            return true;
        }

        private static string Seats(IReadOnlyDictionary<string, int> s) => s == null ? "none" : string.Join(", ", s.Where(kv => kv.Value > 0).OrderBy(kv => kv.Key, StringComparer.Ordinal).Select(kv => kv.Key + " " + kv.Value.ToString(CultureInfo.InvariantCulture)));
        private static string Num(float v) => v.ToString("0.###", CultureInfo.InvariantCulture);
        private static string Day(DateTime d) => d == DateTime.MinValue ? "none" : d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        private static string F(string format, params object[] args) => string.Format(CultureInfo.InvariantCulture, format, args);
    }
}
