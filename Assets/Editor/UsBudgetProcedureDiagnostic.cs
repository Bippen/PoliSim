using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
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
    /// dies at cloture, the pension-age act is its own act at cloture; US-22b (I13 (3)): nothing is tabled that day, and the continuing resolution decided on
    /// the day before the horizon carries the standing figures - nothing withheld, no lapse;</item>
    /// <item>(c) W2, the DEM player withholding the continuing resolution against the AI government's budget (US-18's owed line: the AI president's budget
    /// meets Congress): the shutdown opens on 1 October 2025 and ends the day a resolution passes. US-22b: with the withholder's senators drawn at each
    /// lapse resolution's cloture (I13 (2)), each on the record's spacing (M1), it is run five ways - the drawn index planted where the draw does not carry cloture (12 November, 43 days,
    /// by the procedure's arithmetic, and the lapse's book and output measured against CBO's figures, I13 (1)), planted where it does (the lapse ends
    /// through its defectors at its first vote), released mid-lapse and after the eve (the review's M2: the lapse ends on release), and keyed on the seed as play draws it (printed); the output read as the game publishes it, each quarter's closing value against CBO's level (the review's H1);</item>
    /// <item>(d) the vetoes: a player-president's veto of a bipartisan appropriation overridden; an uncontested resolution vetoed and overridden on its whole
    /// seated base (US-21's rule, its consequence stated); a contested resolution's veto sustained, funding unadvanced; rule (a) never fires on the
    /// government's own acts;</item>
    /// <item>(e) N1: a budget pending across a change of presidency is charged as its lost day is marked (`Congress.GovernmentBears`);</item>
    /// <item>(f) R-US17's budget-vote re-run: every act and resolution under `UsVeto.PlantIndex` at n - 1 writes the unplanted records, nothing drawn;</item>
    /// <item>(g) the funding arithmetic and the request days, DERIVED;</item>
    /// <item>(h) scope: an AI USA votes nothing and is never seeded; the procedures enumerated;</item>
    /// <item>(i) the save: a mid-lapse save round-trips into the game at format 42, the lapse's economics with it (US-22b); a format-41 save is refused;</item>
    /// <item>(j) US-22b (I13 (2)): the withholder's list recomputed from the Senate's saved roll calls, its label, its pooled share and interval, and the chance
    /// it carries cloture in the 119th;</item>
    /// <item>(k) US-22b (I13 (3)): a governing player who tables no budget does not lapse; the review's L2 - a resolution in the slot on the request day is
    /// withdrawn and the request is tabled.</item>
    /// </list>
    /// The random streams are restored after, the epoch by its scope, the draw counters and every host.
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
            // PS-6 US-22b (I13 (1)): CBO publication 61823's own page, its summary in its words (the letter's tables are the PDF's, held by digest below)
            (Lapse + "wayback_20251103181036_cbo_publication_61823.html", new[] { "reduce annualized real GDP growth in that quarter by 1.0 to 2.0 percentage points",
                "between $7 billion and $14 billion (in 2025 dollars) will not be", "one that ends after six weeks (on November 12)" }),
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
        private static readonly string[] DigestOnly = { Lapse + "crs_R48832.2.pdf", Lapse + "house_arrington_61823_quantitative_effects_of_a_shutdown.pdf", Lapse + "wayback_20251029174958_cbo_61823-Shutdown.pdf" };
        private const string Rolls = Lapse + "senate/";

        private static readonly DateTime LapseOpens = new DateTime(2025, 10, 1), LapseEnds = new DateTime(2025, 11, 12);
        private const int Seed = 777;

        private static readonly FieldInfo SlotField = typeof(SimulationManager).GetField("_pendingBudgetBillByCountry", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo UsedField = typeof(SimulationManager).GetField("_incomingBudgetWindowUsed", BindingFlags.Instance | BindingFlags.NonPublic);
        /// <summary>US-22b: a fallen appropriation tables no resolution before the horizon's eve (I13 (3)), so the cases that vote one off that day plant it through
        /// the manager's own tabling (the government of the day's, a bill's course).</summary>
        private static readonly MethodInfo TableResolution = typeof(SimulationManager).GetMethod("TableContinuingResolution", BindingFlags.Instance | BindingFlags.NonPublic);

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
                Check(SlotField != null && UsedField != null && TableResolution != null, "the manager's pending budget slot, the arrival window's flag and its resolution tabling resolved (the planted bills' staging)");

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

                // ================================================================ (a) US-22b: CBO 61823 into the model's shares and quarterly levels (I13 (1), `UsLapse`; the review's H1)
                double[] levels = Enumerable.Range(0, UsLapse.QuarterEffectsBillions.Length).Select(k => -100.0 * UsLapse.QuarterLevelLost(k)).ToArray();
                double[] cboPoints = { -1.5, 2.2, -0.5, -0.1 };   // CBO 61823, Table 2, six weeks: the effect on the annualized quarterly growth rate, Q4 2025 to Q3 2026
                double[] points = levels.Select((l, k) => 4.0 * (l - (k == 0 ? 0.0 : levels[k - 1]))).ToArray();
                Check(UsLapse.QuarterDays == 92 && UsLapse.ScenarioDays == (LapseEnds - LapseOpens).Days && Math.Abs(UsLapse.QuarterOutputBillions - 7500.0) < 1e-3   // 0.24f is stored as 0.2399999946, so the quotient is 7500.00017
                      && points.Take(2).Select((p, k) => Math.Abs(p - cboPoints[k]) < 0.06).All(x => x) && Math.Abs(UsLapse.QuarterEffectsBillions.Sum() + 10f) < 1e-6,
                    F("(a) CBO 61823's six-week scenario (it ends on 12 November, the record's last day; {0} days) in the model's terms - CBO's quarter ${1:0} bn of 2025 dollars ($18 bn \"or 0.24 percent\"), ${2:0.00} bn a day over Q4's {3} days: the stopped outlays ($54 bn) {4:0.0000} of a day's NOMINAL output; Table 2's quarterly levels {5} % of real output, held over each whole quarter, so the four-times rule gives annualized growth {6} points beside CBO's {7} (Q4 and Q1 held within 0.06; the later quarters, from CBO's rounded levels, printed); the table's quarters sum to ${8:0} bn never recovered (its text: $11 bn by the end of 2026, DECLARED residue)",
                        UsLapse.ScenarioDays, UsLapse.QuarterOutputBillions, UsLapse.DayOutputBillions, UsLapse.QuarterDays, UsLapse.DeferredShare,
                        string.Join(" / ", levels.Select(l => l.ToString("+0.000;-0.000", CultureInfo.InvariantCulture))), string.Join(" / ", points.Select(p => p.ToString("+0.00;-0.00", CultureInfo.InvariantCulture))),
                        string.Join(" / ", cboPoints.Select(p => p.ToString("+0.0;-0.0", CultureInfo.InvariantCulture))), -UsLapse.QuarterEffectsBillions.Sum()));

                // ================================================================ (j) US-22b: the withholder's list, recomputed from the Senate's saved roll calls (I13 (2))
                List<(string Identity, int Defectors, int Voting, string Question)> rolls = SenateRolls(out List<string> unheld);
                List<(string Identity, int Defectors, int Voting, string Question)> counted = rolls
                    .Where(r => r.Question == "On Cloture on the Motion to Proceed H.R. 5371" && string.CompareOrdinal(r.Identity.Substring(6, 10), "2025-10-01") >= 0 && string.CompareOrdinal(r.Identity.Substring(6, 10), "2025-11-12") <= 0)
                    .OrderBy(r => r.Identity.Substring(6, 10), StringComparer.Ordinal).ToList();
                bool listHeld = unheld.Count == 0 && counted.Count == UsLapseDraw.Record.Length
                                && counted.Select((r, i) => r.Identity == UsLapseDraw.Record[i].Identity && r.Defectors == UsLapseDraw.Record[i].Defectors && r.Voting == UsLapseDraw.Record[i].Voting).All(x => x);
                Check(listHeld, F("(j) the withholder's list (`UsLapseDraw.Record`) equals the Senate's own roll calls, each held to its SHA-256: of {0} saved votes on H.R. 5371, the cloture motions to proceed from 2025-10-01 to 2025-11-12, the Democratic caucus's yeas of its yeas and nays - {1}{2}",
                    rolls.Count, string.Join(", ", counted.Select(r => r.Identity.Substring(6) + " " + r.Defectors + "/" + r.Voting)), unheld.Count == 0 ? string.Empty : " - NOT HELD: " + string.Join(", ", unheld)));
                Check(rolls.Any(r => r.Question.StartsWith("On the Cloture Motion", StringComparison.Ordinal)) && !counted.Any(r => r.Question.StartsWith("On the Cloture Motion", StringComparison.Ordinal)),
                    "(j) the cloture votes on the substitute and on the bill that followed the successful attempt are saved and not counted (one observation per attempt, DECLARED)");
                (int pooledD, int pooledM) = UsLapseDraw.Pooled();
                (double low, double high) = UsLapseDraw.Wilson(pooledD, pooledM);
                int demSeats = 47, repSeats = 53, line = Congress.ClotureLine(demSeats + repSeats);
                int carries = UsLapseDraw.List.Count(o => repSeats + UsVeto.Crossers(demSeats, o.Defectors, o.Voting) >= line);
                Check(UsLapseDraw.FittedLabel == "FITTED n=" + UsLapseDraw.Record.Length && carries >= 1 && carries < UsLapseDraw.Record.Length,
                    F("(j) {0}: the pooled crossing share {1}/{2} = {3:0.0} %, Wilson 95 % interval [{4:0.0} %, {5:0.0} %] (senator-votes as trials; the per-vote shares run {6:0.0} % to {7:0.0} %); drawn as R-US17 draws, a 119th Senate of REP {8} and DEM {9} withholding carries cloture ({10} of 100) on {11} of the {12} indices - K by index {13}",
                        UsLapseDraw.FittedLabel, pooledD, pooledM, 100.0 * pooledD / pooledM, 100.0 * low, 100.0 * high,
                        100.0 * UsLapseDraw.Record.Min(v => (double)v.Defectors / v.Voting), 100.0 * UsLapseDraw.Record.Max(v => (double)v.Defectors / v.Voting),
                        repSeats, demSeats, line, carries, UsLapseDraw.Record.Length, string.Join(",", UsLapseDraw.List.Select(o => UsVeto.Crossers(demSeats, o.Defectors, o.Voting)))));

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
                        Check(resolution1 == null,
                            "(b) W1: the appropriation fell on 2025-09-19 and NOTHING is tabled that day (US-22b, I13 (3): the resolution comes on the day before the horizon - the plan's 9-September case gone)");
                        int seenCr = Last(u1);
                        bool w1Lapsed = false;
                        Step(s1, w1, new DateTime(2025, 10, 10), d => { if (u1.LapseSince != DateTime.MinValue || u1.LapseFlowDay != DateTime.MinValue) { w1Lapsed = true; } });
                        List<DivisionRecord> cr1 = u1.Divisions.Entries.Where(r => r.Number > seenCr && r.Title.StartsWith(UsBudgetProcedure.ContinuingResolutionWords, StringComparison.Ordinal)).ToList();
                        Check(!w1Lapsed && cr1.Count == 2 && cr1.All(r => r.Passed && r.Sides.Count == 0 && r.Date == new DateTime(2025, 9, 29)) && cr1[0].Chamber == Congress.House && cr1[1].Chamber == Congress.Senate && cr1[1].Cloture
                              && u1.LapseSince == DateTime.MinValue && u1.AppropriatedThrough == new DateTime(2026, 9, 30) && s1.GetPendingBudgetBill(CountryId.USA) == null
                              && Mathf.Approximately(Line(u1, SpendingCategory.Transportation), transportWas) && u1.LapseDeferredOutlays == 0f && u1.LapseOutputWedge == 0f && u1.LapseOutputDays == 0,
                            F("(b) W1: the continuing resolution, tabled on 2025-09-29 - the day before the horizon, no appropriation for the coming year law - and asked nothing (no party withholds it), passes both chambers uncontested THAT DAY (\"{0}\") and carries the standing figures - Transportation unchanged; funded through {1}; funding never lapsed, and the lapse's book and output never moved: nothing withheld means no lapse (I13 (3))",
                                cr1.LastOrDefault()?.Title, Day(u1.AppropriatedThrough)));

                        // ============================================================ (c) W2: the DEM player withholds against the AI government's budget
                        // US-22b: run several ways - the withholder's draw planted where it does not carry cloture (index 0), planted where it does (the last index),
                        // keyed as play draws it, and released mid-lapse and after the eve's fall (the review's M2); a lapse's resolutions on the record's spacing (M1)
                        int spacing = UsLapseDraw.RecordSpacingDays;
                        DateTime released = new DateTime(2025, 11, 11);
                        W2Run held = RunW2(hosts, 0, released, new DateTime(2026, 10, 2));
                        Check(held.GovernmentBothChambers,
                            "(c) W2 opened 2025-07-29: the DEM player does not govern; the AI government's budget (Transportation -10 %, planted with its day on 2025-08-19) goes through both chambers - US-18's owed line closed");
                        DivisionRecord appS2 = Find(held.All, "Appropriation: ", Congress.Senate);
                        Check(appS2 != null && !appS2.Passed && appS2.Cloture && Count(appS2).For == 53 && Find(held.All, "Appropriation: ", Congress.House)?.Passed == true && appS2.Date == new DateTime(2025, 8, 19),
                            F("(c) W2's appropriation, the government's (\"{0}\"), on 2025-08-19: the House {1}, the Senate at cloture {2} - it dies", appS2?.Title, Describe(Find(held.All, "Appropriation: ", Congress.House)), Describe(appS2)));
                        List<DivisionRecord> crs = held.All.Where(r => r.Title.StartsWith(UsBudgetProcedure.ContinuingResolutionWords, StringComparison.Ordinal)).ToList();
                        List<DivisionRecord> crSenates = crs.Where(r => r.Chamber == Congress.Senate).ToList();
                        DivisionRecord sep29 = crSenates.FirstOrDefault(r => r.Date == new DateTime(2025, 9, 29));
                        DivisionRecord sep29House = crs.FirstOrDefault(r => r.Date == new DateTime(2025, 9, 29) && r.Chamber == Congress.House);
                        Check(held.Withheld && held.NothingAfterFall && sep29House != null && sep29House.Passed && Count(sep29House) == (220, 215) && sep29 != null && !sep29.Passed && sep29.Cloture && Count(sep29) == (53, 47) && !sep29.Sides.Any(x => x.Defectors),
                            F("(c) W2: the DEM player withholds in advance on 2025-08-19 (nothing is before Congress: the resolution is DUE, I13 (3)); nothing is tabled when the appropriation falls; on 2025-09-29, the day before the horizon, the resolution passes the House {0} and fails the Senate at cloture {1}, nothing drawn before a lapse - the record's shape (H.R. 5371: the House 217-212, then the Senate 44-48 and 55-45, not 60)",
                                Describe(sep29House), Describe(sep29)));
                        List<DateTime> lapseVotes = crSenates.Where(r => r.Date > LapseOpens && r.Date < LapseEnds).Select(r => r.Date).ToList();
                        bool onSpacing = lapseVotes.Count > 0 && lapseVotes[0] == LapseOpens.AddDays(spacing) && lapseVotes.Zip(lapseVotes.Skip(1), (a, b) => (b - a).Days == spacing).All(x => x);
                        Check(held.LapseOpened == LapseOpens && held.Retabled1001 && onSpacing && crSenates.Where(r => r.Date > LapseOpens && r.Date < LapseEnds).All(r => !r.Passed && r.Cloture && !r.Sides.Any(x => x.Defectors))
                              && held.LapseDraws == lapseVotes.Count && held.LapseIndices.All(j => j == 0),
                            F("(c) W2 (the draw planted at index 0, {0}): funding lapses on {1}, the day after P.L. 119-4's horizon, and the government tables a resolution that day on the record's spacing, {2} days (DECLARED from the dated votes, the review's M1); still withheld, the resolutions fall at cloture on {3} - {4} votes, DEM's own senators drawn at each, {5} crosser(s), short of the line, each re-tabled the same day",
                                UsLapseDraw.Record[0].Identity, Day(held.LapseOpened), spacing, string.Join(", ", lapseVotes.Select(Day)), lapseVotes.Count,
                                UsVeto.Crossers(47, UsLapseDraw.Record[0].Defectors, UsLapseDraw.Record[0].Voting)));
                        DivisionRecord nov12 = crSenates.FirstOrDefault(r => r.Date == LapseEnds);
                        Check(held.Released && nov12 != null && nov12.Passed && nov12.Sides.Count == 0 && held.LapseEnded == LapseEnds && held.EndedDays == 43 && held.FundedAfter == new DateTime(2026, 9, 30) && held.WithheldAfter == null,
                            F("(c) W2: released on 2025-11-11, the resolution before Congress passes both chambers uncontested on 2025-11-12 (\"{0}\") - THE SHUTDOWN ENDS AFTER {1} DAYS, beside the record's 43 (CRS R48832's gap 1 Oct to 12 Nov; P.L. 119-37 of 12 Nov) - by the procedure's arithmetic, not fitted; funded through {2}",
                                nov12?.Title, held.EndedDays, Day(held.FundedAfter)));
                        Check(held.Charges.Count == 1 + lapseVotes.Count && held.Charges.All(c => c.Contains("-1.50") && c.EndsWith(" marked", StringComparison.Ordinal)),
                            F("(c) W2: every failed resolution is the government's (its own bill, `Congress.GovernmentBears`) - each of the {0} charged the failed vote's cost and its day marked lost while the player withholds (a consequence, stated)", held.Charges.Count));
                        sb.Append("    printed   W2's resolutions (index 0 planted): ").Append(string.Join(" | ", held.CrSides)).Append('\n');
                        bool aSigns = held.All.Any(r => r.Passed && r.Chamber == Congress.Senate) && !held.All.Any(v => v.Title.StartsWith("Vetoed", StringComparison.Ordinal));
                        Check(aSigns, "(d) rule (a) never fires on the government's own acts: under Trump of record (an AI president) every act and resolution both chambers sent was signed - the president's party votes for its own government's bills");

                        // (c) US-22b, I13 (1): the lapse's book in W2's held run
                        int lapseDays = UsBudgetProcedure.LapseDays(LapseOpens, LapseEnds);
                        Check(held.ArmedDays == lapseDays - 1 && held.ArmedDays == UsLapse.ScenarioDays && held.FirstArmed == LapseOpens.AddDays(1) && held.LastArmed == LapseEnds && held.PaidOn == LapseEnds.AddDays(1) && held.StockAtPeak > 0f && held.StockAfterPay == 0f && held.StopsNearShare,
                            F("(c) W2's book (I13 (1)): the non-excepted outlays stop on each of the lapse's {0} armed days ({1} to {2} - the economy's day runs before the funding tick, one fewer than its {3} counted days; CBO's six weeks are {4}); the deferred stock reaches {5} (nominal, the game's units; {6:0.000} % of the year's nominal output, beside CBO's $54 bn of a ${7:0} bn year, {8:0.000} %) and is PAID IN FULL on {9} (1341(c)), the stock 0 after; mandatory spending untouched",
                                held.ArmedDays, Day(held.FirstArmed), Day(held.LastArmed), lapseDays, UsLapse.ScenarioDays, Num(held.StockAtPeak), 100.0 * held.StockAtPeak / held.NominalGdpAtPeak,
                                UsLapse.DayOutputBillions * 365.0, 100.0 * UsLapse.DelayedOutlaysBillions / (UsLapse.DayOutputBillions * 365.0), Day(held.PaidOn)));
                        // (c) US-22b, I13 (1) and the review's H1: the output as the game PUBLISHES it - each quarter's closing value (`PublicationSystem`: the period's last
                        // recorded day, the state the day before its last), read off the run and held against CBO's quarterly level for that quarter
                        DateTime[] quarters = { new DateTime(2025, 10, 1), new DateTime(2026, 1, 1), new DateTime(2026, 4, 1), new DateTime(2026, 7, 1) };
                        var closings = new List<string>();
                        bool closingsHold = true;
                        double previous = 0.0;
                        for (int k = 0; k < quarters.Length; k++)
                        {
                            DateTime sampled = quarters[k].AddMonths(3).AddDays(-2);   // the closing value is recorded at the start of the quarter's last day, from the day before
                            bool has = held.U.Published.PeriodClosingValues.TryGetValue((ClosingStat.RealGdp, quarters[k]), out float closing);
                            bool day = held.Days.TryGetValue(sampled, out (float Gdp, float Wedge) state);
                            double level = day ? -100.0 * state.Wedge / ((double)state.Gdp + state.Wedge) : double.NaN;
                            bool ok = has && day && closing == state.Gdp && Math.Abs(level + 100.0 * UsLapse.QuarterLevelLost(k)) < 1e-4;
                            if (k < 2) { closingsHold &= ok; }   // the review's H1 asks Q4 and Q1; the later two printed beside
                            closings.Add(F("{0} closing {1} ({2}: {3:+0.000;-0.000} % against the identity, CBO {4:+0.000;-0.000} %; annualized growth {5:+0.00;-0.00} points){6}",
                                Day(quarters[k]).Substring(0, 7), has ? Num(closing) : "MISSING", Day(sampled), level, -100.0 * UsLapse.QuarterLevelLost(k), 4.0 * (level - previous), ok ? string.Empty : " NOT HELD"));
                            previous = level;
                        }
                        Check(closingsHold && held.WedgeAtEnd == 0f && held.OutputDaysAtEnd == 0 && held.ProfileAtEnd == DateTime.MinValue && Math.Abs(held.TotalShare * UsLapse.DayOutputBillions - 10.0) < 1.0,
                            F("(c) W2's output (I13 (1), real with real; the review's H1 - shaped to the quarter): the PUBLISHED real GDP of each quarter is CBO's level for it - {0}; the overlay's net over the run {1:0.0} bn of CBO's 2025 dollars never recovered (the table's quarters sum to $10 bn; its text says $11 bn); nothing applied or held by {2}",
                                string.Join("; ", closings), held.TotalShare * UsLapse.DayOutputBillions, Day(new DateTime(2026, 10, 2))));

                        // (c) US-22b, I13 (2)/(3): the lapse ends through the withholder's defectors - the last index planted, the record's 9 November vote
                        W2Run crossed = RunW2(hosts, UsLapseDraw.Record.Length - 1, released, LapseOpens.AddDays(spacing + 2));
                        DateTime firstVote = LapseOpens.AddDays(spacing);
                        DivisionRecord crossedVote = crossed.All.LastOrDefault(r => r.Date == firstVote && r.Chamber == Congress.Senate && r.Title.StartsWith(UsBudgetProcedure.ContinuingResolutionWords, StringComparison.Ordinal));
                        int kLast = UsVeto.Crossers(47, UsLapseDraw.Record[UsLapseDraw.Record.Length - 1].Defectors, UsLapseDraw.Record[UsLapseDraw.Record.Length - 1].Voting);
                        Check(crossedVote != null && crossedVote.Passed && crossedVote.Cloture && crossedVote.Sides.Any(x => x.Abbrev == "DEM" && x.Defectors && x.Seats == kLast) && crossed.LapseEnded == firstVote
                              && crossed.EndedDays == UsBudgetProcedure.LapseDays(LapseOpens, firstVote) && crossed.FundedAfter == new DateTime(2026, 9, 30) && crossed.PaidOn == firstVote.AddDays(1) && crossed.StockAfterPay == 0f,
                            F("(c) W2 with the draw planted at the last index ({0}): on {1}, the lapse's first vote, DEM draws {2} crosser(s) at cloture and the resolution carries ({3}) - the lapse ends THROUGH ITS DEFECTORS on day {4} (I13 (3)); the stopped outlays paid on {5}",
                                UsLapseDraw.Record[UsLapseDraw.Record.Length - 1].Identity, Day(firstVote), kLast, Describe(crossedVote), crossed.EndedDays, Day(crossed.PaidOn)));

                        // (c) the review's M2: "on release" ends the lapse - released mid-lapse, an uncontested resolution is decided on the next funding tick
                        DateTime midRelease = new DateTime(2025, 10, 20);
                        W2Run releasedRun = RunW2(hosts, 0, midRelease, midRelease.AddDays(3));
                        Check(releasedRun.Released && releasedRun.LapseEnded == midRelease.AddDays(1) && releasedRun.EndedDays == UsBudgetProcedure.LapseDays(LapseOpens, midRelease.AddDays(1)) && releasedRun.FundedAfter == new DateTime(2026, 9, 30)
                              && releasedRun.PendingAtEnd == null && releasedRun.HorizonAtEnd == new DateTime(2026, 9, 30),
                            F("(c) the review's M2: released on {0} with a resolution mid-course before Congress, the resolution is withdrawn and an uncontested one is decided on the next tick - the lapse ENDS ON RELEASE, on {1}, day {2}; nothing left in the slot, funded through {3} once",
                                Day(midRelease), Day(releasedRun.LapseEnded), releasedRun.EndedDays, Day(releasedRun.HorizonAtEnd)));
                        W2Run eveReleased = RunW2(hosts, 0, new DateTime(2025, 9, 29), LapseOpens.AddDays(2));
                        Check(eveReleased.Released && eveReleased.LapseOpened == DateTime.MinValue && eveReleased.EverLapsed == false && eveReleased.HorizonAtEnd == new DateTime(2026, 9, 30) && eveReleased.Charges.Count == 1,
                            F("(c) the review's M2, before the horizon: the eve's resolution falls on 2025-09-29 (withheld); released that day, an uncontested one is decided on the horizon itself, 2025-09-30, and is law - funding never lapses (funded through {0})",
                                Day(eveReleased.HorizonAtEnd)));

                        // (c) the keyed draw, as play draws it (seed 777): reported, and held to the arithmetic of the indices it drew
                        W2Run keyed = RunW2(hosts, null, released, LapseEnds.AddDays(1));
                        int carryAt = keyed.LapseIndices.FindIndex(j => 53 + UsVeto.Crossers(47, UsLapseDraw.List[j].Defectors, UsLapseDraw.List[j].Voting) >= Congress.ClotureLine(100));
                        DateTime keyedEnd = carryAt >= 0 ? keyed.LapseVoteDays[carryAt] : LapseEnds;
                        Check(keyed.LapseDraws >= 1 && keyed.LapseDraws == keyed.LapseIndices.Count && keyed.LapseEnded == keyedEnd && keyed.EndedDays == UsBudgetProcedure.LapseDays(LapseOpens, keyedEnd),
                            F("(c) W2 keyed (seed {0}, the play key `UsLapseDraw.Key`): the draws take indices {1} on {2} - {3}; the shutdown ends after {4} days",
                                Seed, string.Join(",", keyed.LapseIndices), string.Join(", ", keyed.LapseVoteDays.Select(Day)),
                                carryAt >= 0 ? "cloture carries on " + Day(keyedEnd) + " and the lapse ends through its defectors" : "none carries, so the lapse runs to the release", keyed.EndedDays));

                        // ============================================================ (i) the save, cut mid-lapse in a W2 world
                        (SimulationManager s3, World w3, Country u3) = UsCongressDiagnostic.OpenUsa(hosts, new DateTime(2025, 9, 20), "DEM");
                        ((HashSet<CountryId>)UsedField.GetValue(s3)).Add(CountryId.USA);
                        var w3Bill = new BudgetBill { GovernmentBill = true, DaysRemaining = 3 };
                        w3Bill.SpendingPercentChanges[SpendingCategory.Transportation] = -10f;
                        ((Dictionary<CountryId, BudgetBill>)SlotField.GetValue(s3))[CountryId.USA] = w3Bill;
                        bool held3;
                        string why3;
                        // the draw planted at index 0, as W2's held run: by the play key (seed 777) cloture carries on 2025-10-10, before the cut
                        using (UsLapseDraw.PlantIndex(0))
                        {
                            Step(s3, w3, new DateTime(2025, 9, 23));
                            held3 = s3.WithholdContinuingResolution(CountryId.USA, true, out why3);
                            Step(s3, w3, new DateTime(2025, 10, 15));
                        }
                        BudgetBill pending3 = s3.GetPendingBudgetBill(CountryId.USA);
                        string json = SaveGameService.Serialize(SaveGameService.CreateSaveGame(s3, w3, CountryId.USA, null));
                        SaveGame cut = SaveGameService.Deserialize(json);
                        Country saved = cut.World.GetCountry(CountryId.USA);
                        BudgetBill savedBill = cut.Sim.PendingBudgetBills.TryGetValue(CountryId.USA, out BudgetBill sbill) ? sbill : null;
                        Check(held3 && u3.LapseSince == LapseOpens && pending3 != null && pending3.ContinuingResolution && cut.SaveVersion == 42 && cut.SaveVersion == SaveGameService.CurrentSaveVersion
                              && saved.LapseSince == u3.LapseSince && saved.AppropriatedThrough == u3.AppropriatedThrough && saved.ContinuingResolutionWithheldBy == "DEM"
                              && u3.LapseFlowDay == new DateTime(2025, 10, 16) && u3.LapseDeferredOutlays > 0f && u3.LapseOutputWedge > 0f && u3.LapseOutputDays > 0
                              && saved.LapseFlowDay == u3.LapseFlowDay && saved.LapseDeferredOutlays == u3.LapseDeferredOutlays && saved.LapseOutputWedge == u3.LapseOutputWedge
                              && saved.LapseOutputDays == u3.LapseOutputDays && saved.LapseProfileQuarter == u3.LapseProfileQuarter && saved.State.GDP == u3.State.GDP
                              && savedBill != null && savedBill.ContinuingResolution && savedBill.GovernmentBill && savedBill.DaysRemaining == pending3.DaysRemaining,
                            F("(i) a save cut mid-lapse on 2025-10-15 carries format {0}, the lapse ({1}), the horizon ({2}), the withheld resolution ({3}), the pending resolution (its flag, the government's, {4} days left) and US-22b's economics bit for bit - the armed day {6}, the stopped outlays {7}, the overlay on output {8} and the lapse's days counted for its quarterly profile {9}{5}",
                                cut.SaveVersion, Day(saved.LapseSince), Day(saved.AppropriatedThrough), saved.ContinuingResolutionWithheldBy ?? "none", savedBill?.DaysRemaining, held3 ? string.Empty : " - WITHHOLD REFUSED: " + why3,
                                Day(saved.LapseFlowDay), Num(saved.LapseDeferredOutlays), Num(saved.LapseOutputWedge), saved.LapseOutputDays));
                        (SimulationManager adopted, PoliSim.UI.GameController _) = PresidencyDiagnostic.Adopt(cut, hosts, out string error);
                        if (adopted == null) { Check(false, "(i) the save loaded into the game: " + error); }
                        else
                        {
                            Country u = adopted.World.GetCountry(CountryId.USA);
                            BudgetBill loaded = adopted.GetPendingBudgetBill(CountryId.USA);
                            Check(u.LapseSince == LapseOpens && u.AppropriatedThrough == new DateTime(2025, 9, 30) && u.ContinuingResolutionWithheldBy == "DEM" && loaded != null
                                  && u.LapseFlowDay == u3.LapseFlowDay && u.LapseDeferredOutlays == u3.LapseDeferredOutlays && u.LapseOutputWedge == u3.LapseOutputWedge && u.LapseOutputDays == u3.LapseOutputDays && loaded.ContinuingResolution && loaded.DaysRemaining == pending3.DaysRemaining,
                                F("(i) loaded INTO THE GAME (the controller's RestoreFromSave): the lapse, the horizon, the withheld resolution, the pending resolution and the lapse's economics kept as saved - not re-seeded ({0}, day {1} of the shutdown)",
                                    UsBudgetProcedure.FundedWords(u), UsBudgetProcedure.LapseDays(u.LapseSince, adopted.CurrentDate)));
                        }
                        string older = json.Replace("\"SaveVersion\": 42", "\"SaveVersion\": 41");
                        bool refused = false; string refusedWhy = null;
                        try { SaveGameService.Deserialize(older); } catch (SaveLoadException e) { refused = true; refusedWhy = e.Message; }
                        Check(older != json && refused && refusedWhy.Contains("format version 41"), F("(i) a format-41 save is refused with the plain message (ruling A): \"{0}\"", refusedWhy));

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
                        // the uncontested resolution vetoed by the player-president - overridden on the whole seated base, so law. US-22b: a fallen appropriation
                        // tables none before the horizon's eve (I13 (3)), so the resolution is planted through the manager's own tabling (a bill's course)
                        TableResolution.Invoke(s4, new object[] { u4 });
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
                        TableResolution.Invoke(s4, new object[] { u4 });
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

                        // ============================================================ (k) US-22b, I13 (3): the cadence - the 21-day case and L2
                        (SimulationManager s6, World w6, Country u6) = UsCongressDiagnostic.OpenUsa(hosts, new DateTime(2025, 8, 29), "REP");
                        int seen6 = Last(u6);
                        bool lapsed6 = false;
                        Step(s6, w6, LapseOpens.AddDays(1), d => { if (u6.LapseSince != DateTime.MinValue) { lapsed6 = true; } });
                        List<DivisionRecord> cr6 = u6.Divisions.Entries.Where(r => r.Number > seen6 && r.Title.StartsWith(UsBudgetProcedure.ContinuingResolutionWords, StringComparison.Ordinal)).ToList();
                        Check(s6.PlayerGoverns(u6) && !lapsed6 && cr6.Count == 2 && cr6.All(r => r.Passed && r.Date == new DateTime(2025, 9, 29)) && u6.AppropriatedThrough == new DateTime(2026, 9, 30) && u6.LapseDeferredOutlays == 0f,
                            F("(k) the 21-day case gone: REP governing, no budget tabled, opened 2025-08-29 and stepped to {0} - the resolution decided on 2025-09-29 passes uncontested that day (\"{1}\"), funding through {2}, and nothing lapses (before US-22b a governing player who tabled no budget lapsed 21 days each October)",
                                Day(LapseOpens.AddDays(1)), cr6.LastOrDefault()?.Title, Day(u6.AppropriatedThrough)));
                        (SimulationManager s7, World w7, Country u7) = UsCongressDiagnostic.OpenUsa(hosts, new DateTime(2026, 2, 1), "DEM");
                        ((HashSet<CountryId>)UsedField.GetValue(s7)).Add(CountryId.USA);   // the arrival window spent (staging: mid-term)
                        u7.AppropriatedThrough = new DateTime(2025, 9, 30);   // planted: a lapse in progress since 1 October 2025, a resolution before Congress on its course
                        u7.LapseSince = LapseOpens;
                        u7.ContinuingResolutionWithheldBy = "DEM";   // planted: withheld, so the lapse runs on (released, M2 would end it on the next tick)
                        TableResolution.Invoke(s7, new object[] { u7 });
                        BudgetBill crBefore = s7.GetPendingBudgetBill(CountryId.USA);
                        DateTime requestDay = FiscalYearData.BudgetRequestDay(CountryId.USA, 2026);
                        Step(s7, w7, requestDay);
                        BudgetBill request = s7.GetPendingBudgetBill(CountryId.USA);
                        Check(crBefore != null && crBefore.ContinuingResolution && crBefore.DaysRemaining > (requestDay - new DateTime(2026, 2, 1)).Days && request != null && !request.ContinuingResolution && request.GovernmentBill
                              && u7.LapseSince == LapseOpens && u7.LapseFlowDay == requestDay.AddDays(1),
                            F("(k) L2 gone: a withheld resolution planted before Congress during a lapse on 2026-02-01, on its course (the record's spacing) past the request day; on {0} (1105(a)'s first Monday) it is withdrawn and the president's request is tabled in its place (\"the request is tabled on its day whatever is before Congress\") - the government's bill ({1}); the lapse runs on, its flows armed",
                                Day(requestDay), SimulationManager.DescribeBudgetBill(request)));

                        // (k) US-21b with US-22b: the eve's resolution, the government's own, signed on the day by a player-president (the second review's N1)
                        (SimulationManager s8, World w8, Country u8) = UsCongressDiagnostic.OpenUsa(hosts, new DateTime(2025, 9, 27), "REP");
                        var trump8 = new UsPresidentialElection.Contest { PollingDay = new DateTime(2024, 11, 5), TakesOffice = new DateTime(2025, 1, 20), Winner = "Donald J. Trump", WinnerParty = "REP", ElectorsAppointed = 538, Line = "planted (UsBudgetProcedureDiagnostic, k)" };
                        trump8.Electors.Add(new UsPresidentialElection.Tally { Candidate = "Donald J. Trump", Party = "REP", Votes = 312 });
                        trump8.Electors.Add(new UsPresidentialElection.Tally { Candidate = "Kamala D. Harris", Party = "DEM", Votes = 226 });
                        u8.UsPresidentialElections.Add(trump8);
                        u8.Government = UsPresidentialElection.GovernmentOf(trump8, trump8.TakesOffice);
                        DateTime eve8 = new DateTime(2025, 9, 29);
                        bool docketed8 = true, lapsed8 = false;
                        int seen8 = Last(u8);
                        Step(s8, w8, LapseOpens.AddDays(2), d =>
                        {
                            BudgetBill p8 = s8.GetPendingBudgetBill(CountryId.USA);
                            if (d == eve8) { docketed8 = p8 != null && p8.Docket != null; }
                            if (u8.LapseSince != DateTime.MinValue) { lapsed8 = true; }
                        });
                        List<DivisionRecord> cr8 = u8.Divisions.Entries.Where(r => r.Number > seen8 && r.Title.StartsWith(UsBudgetProcedure.ContinuingResolutionWords, StringComparison.Ordinal)).ToList();
                        Check(Congress.PresidentOn(u8, eve8, out _, out _, out bool player8) && player8 && !docketed8 && !lapsed8 && cr8.Count == 2 && cr8.All(r => r.Passed && r.Date == eve8)
                              && s8.GetPendingBudgetBill(CountryId.USA) == null && s8.GetDocket(CountryId.USA).Count == 0 && u8.AppropriatedThrough == new DateTime(2026, 9, 30),
                            F("(k) the second review's N1 (DECLARED, put to Elias): a planted game-elected Trump, the player REP - the eve's resolution passes both chambers uncontested on {0} and, the government's own resolution decided before any lapse, is TAKEN AS SIGNED ON THE DAY by the player-president, never docketed; funded through {1} and NO LAPSE OPENS (nothing withheld means no lapse); resolutions voted while lapsed still meet his docket",
                                Day(eve8), Day(u8.AppropriatedThrough)));

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
                        Check(WorldClock.CongressActs(CountryId.USA) && !UsBudgetProcedure.Applies(aiUsa) && aiUsa.AppropriatedThrough == DateTime.MinValue && aiUsa.LapseSince == DateTime.MinValue && ss.GetPendingBudgetBill(CountryId.USA) == null
                              && ws.Countries.All(c => c.LapseFlowDay == DateTime.MinValue && c.LapseDeferredOutlays == 0f && c.LapseOutputWedge == 0f && c.LapseOutputDays == 0),
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
                UsLapseDraw.ResetDrawCalls();
            }

            sb.Append(failures == 0 ? "=== UsBudgetProcedureDiagnostic: the US budget procedure holds - the acts, the continuing resolution, the lapse and its 43 days, its book and output by CBO 61823, the withholder's draw, the cadence, the vetoes, N1 ===\n"
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
        /// call run to its day, the player-president answering by <paramref name="vetoes"/> - on the day, so no act is held on his docket while the date stands
        /// still (PS-6 US-21b: null signs on the day, as a player-president's answer did before the docket; an AI president reads no answer).</summary>
        private static void Resolve(SimulationManager s, CountryId id, BudgetBill bill, Func<DivisionRecord, bool> vetoes)
        {
            Func<DivisionRecord, bool> was = s.PlayerPresidentVetoes;
            s.PlayerPresidentVetoes = vetoes ?? (_ => false);
            try
            {
                if (bill != null) { BudgetBillHarness.Introduce(s, id, bill); }
                BudgetBill pending = s.GetPendingBudgetBill(id);
                for (int i = 0; i <= ParliamentSystem.BillDurationDays && pending != null && ReferenceEquals(s.GetPendingBudgetBill(id), pending); i++) { s.AdvanceBudgetBillDay(id); }
            }
            finally { s.PlayerPresidentVetoes = was; }
        }

        /// <summary>(f)'s run: W1's budget and a withheld resolution (planted, US-22b) decided on 2025-02-03 (the 119th, Trump of record) - planted at n - 1 or not; the records written.</summary>
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
                TableResolution.Invoke(s, new object[] { u });   // US-22b: planted - a fallen appropriation tables none before the horizon's eve (I13 (3))
                Resolve(s, CountryId.USA, null, null);
            }
            finally { plant?.Dispose(); u.ContinuingResolutionWithheldBy = null; }
            return u.Divisions.Entries.Where(r => r.Number > seen).ToList();
        }

        // ------------------------------------------------------------------ (c)'s run: W2, the DEM player withholding against the AI government's budget
        private sealed class W2Run
        {
            public Country U;
            public List<DivisionRecord> All;
            public List<string> CrSides = new List<string>(), Charges = new List<string>();
            public bool GovernmentBothChambers, Withheld, NothingAfterFall, Released, Retabled1001, StopsNearShare = true, EverLapsed;
            public DateTime LapseOpened = DateTime.MinValue, LapseEnded = DateTime.MinValue, FundedAfter = DateTime.MinValue, FirstArmed = DateTime.MinValue, LastArmed = DateTime.MinValue, PaidOn = DateTime.MinValue;
            public DateTime HorizonAtEnd, ProfileAtEnd;
            public string WithheldAfter = "unset";
            public int EndedDays, ArmedDays, OutputDaysAtEnd;
            public long LapseDraws;
            public List<int> LapseIndices = new List<int>();
            public List<DateTime> LapseVoteDays = new List<DateTime>();
            public float StockAtPeak, StockAfterPay = -1f, NominalGdpAtPeak, WedgeAtEnd;
            public double TotalShare;
            public BudgetBill PendingAtEnd;
            /// <summary>Each day's real GDP as the day left it and the overlay on it - the state the next day's closing value records.</summary>
            public Dictionary<DateTime, (float Gdp, float Wedge)> Days = new Dictionary<DateTime, (float Gdp, float Wedge)>();
        }

        /// <summary>W2 staged as the US-22 record staged it - opened 2025-07-29 with the DEM player in opposition, the arrival window spent, the government's
        /// budget (a Transportation cut) planted so its day is 2025-08-19 - and stepped in the controller's order to <paramref name="until"/>: the DEM player
        /// withholds on 2025-08-19 and releases on <paramref name="releaseOn"/>, after its tick. <paramref name="plant"/>: the withholder's draw at that index
        /// (`UsLapseDraw.PlantIndex`), or keyed where null. Read each day: the resolutions and each lapse vote's drawn index, the lapse's opening and end, the
        /// deferred outlays, and the overlay on output.</summary>
        private static W2Run RunW2(List<GameObject> hosts, int? plant, DateTime releaseOn, DateTime until)
        {
            var run = new W2Run();
            (SimulationManager s2, World w2, Country u2) = UsCongressDiagnostic.OpenUsa(hosts, new DateTime(2025, 7, 29), "DEM");
            run.U = u2;
            ((HashSet<CountryId>)UsedField.GetValue(s2)).Add(CountryId.USA);   // the arrival window spent (DECLARED staging: the run starts mid-term)
            var w2Bill = new BudgetBill { GovernmentBill = true, DaysRemaining = ParliamentSystem.BillDurationDays };
            w2Bill.SpendingPercentChanges[SpendingCategory.Transportation] = -10f;
            ((Dictionary<CountryId, BudgetBill>)SlotField.GetValue(s2))[CountryId.USA] = w2Bill;   // the government's bill, planted so its day is 2025-08-19
            run.GovernmentBothChambers = !s2.PlayerGoverns(u2) && Congress.VotesBothChambers(u2, ParliamentSystem.GetBudgetBillConcern(u2, w2Bill));
            int seen2 = Last(u2);
            run.All = new List<DivisionRecord>();   // collected day by day: W2 writes more divisions than `DivisionLog.MaxEntries` keeps, so a read at the end loses the first
            long drawsBefore = UsLapseDraw.DrawCalls;
            float stockWas = 0f;
            DateTime lapseWas = DateTime.MinValue;
            IDisposable planted = plant.HasValue ? UsLapseDraw.PlantIndex(plant.Value) : null;
            try
            {
                Step(s2, w2, until, d =>
                {
                    run.All.AddRange(u2.Divisions.Entries.Where(r => r.Number > seen2));
                    seen2 = Last(u2);
                    if (d == new DateTime(2025, 8, 19)) { run.Withheld = s2.WithholdContinuingResolution(CountryId.USA, true, out _); run.NothingAfterFall = s2.GetPendingBudgetBill(CountryId.USA) == null; }
                    if (d == releaseOn) { run.Released = s2.WithholdContinuingResolution(CountryId.USA, false, out _); }
                    if (d == LapseOpens) { run.LapseOpened = u2.LapseSince; run.Retabled1001 = s2.GetPendingBudgetBill(CountryId.USA)?.ContinuingResolution == true && s2.GetPendingBudgetBill(CountryId.USA).DaysRemaining == UsLapseDraw.RecordSpacingDays; }
                    if (u2.LapseSince != DateTime.MinValue) { run.EverLapsed = true; }
                    if (lapseWas != DateTime.MinValue && u2.LapseSince == DateTime.MinValue && run.LapseEnded == DateTime.MinValue)
                    {
                        run.LapseEnded = d; run.EndedDays = UsBudgetProcedure.LapseDays(lapseWas, d); run.FundedAfter = u2.AppropriatedThrough; run.WithheldAfter = u2.ContinuingResolutionWithheldBy;
                    }
                    // the book: the stopped outlays - each armed day's stop near the share of the day's nominal output (the accrual read the level before the
                    // day's overlay) - and the payment
                    float stock = u2.LapseDeferredOutlays;
                    if (stock > stockWas)
                    {
                        run.ArmedDays++;
                        if (run.FirstArmed == DateTime.MinValue) { run.FirstArmed = d; }
                        run.LastArmed = d;
                        double ratio = (stock - stockWas) / (UsLapse.DeferredShare * (double)u2.State.NominalGdp / SimulationManager.DaysPerTurn);
                        if (ratio < 0.97 || ratio > 1.03) { run.StopsNearShare = false; }
                        if (stock > run.StockAtPeak) { run.StockAtPeak = stock; run.NominalGdpAtPeak = u2.State.NominalGdp; }
                    }
                    if (stockWas > 0f && stock == 0f && run.PaidOn == DateTime.MinValue) { run.PaidOn = d; run.StockAfterPay = stock; }
                    stockWas = stock;
                    // the output: the overlay as a share of the level the identity produced, and the day's state for the closing values
                    double wedge = u2.LapseOutputWedge;
                    run.TotalShare += wedge / ((double)u2.State.GDP + wedge);
                    run.Days[d] = (u2.State.GDP, u2.LapseOutputWedge);
                    lapseWas = u2.LapseSince;   // read by the next day's divisions and its end-of-lapse test
                }, (d, approvalBefore, approvalAfter, written) =>
                {
                    DivisionRecord crSenate = written.LastOrDefault(x => x.Title.StartsWith(UsBudgetProcedure.ContinuingResolutionWords, StringComparison.Ordinal));
                    if (crSenate != null) { run.CrSides.Add(Day(d) + " " + string.Join(" then ", written.Where(x => x.Title.StartsWith(UsBudgetProcedure.ContinuingResolutionWords, StringComparison.Ordinal)).Select(Describe))); }
                    if (crSenate != null && !crSenate.Passed) { run.Charges.Add(F("{0} {1:+0.00;-0.00} lost {2}", Day(d), approvalAfter - approvalBefore, u2.BillLostOn == d ? "marked" : "NOT MARKED")); }
                    if (lapseWas != DateTime.MinValue && crSenate != null && crSenate.Chamber == Congress.Senate && crSenate.Sides.Count > 0)
                    {
                        // a resolution voted during the lapse while withheld: the withholder's draw - the index the play key (or the plant) gives
                        int index = plant ?? UsVeto.Index(UsVeto.Unit(UsLapseDraw.Key(SimulationRandom.MasterSeed, d, crSenate.Title, "DEM")), UsLapseDraw.List.Count);
                        run.LapseIndices.Add(Math.Min(UsLapseDraw.List.Count - 1, Math.Max(0, index)));
                        run.LapseVoteDays.Add(d);
                    }
                });
            }
            finally { planted?.Dispose(); }
            run.LapseDraws = UsLapseDraw.DrawCalls - drawsBefore;
            run.WedgeAtEnd = u2.LapseOutputWedge;
            run.OutputDaysAtEnd = u2.LapseOutputDays;
            run.ProfileAtEnd = u2.LapseProfileQuarter;
            run.PendingAtEnd = s2.GetPendingBudgetBill(CountryId.USA);
            run.HorizonAtEnd = u2.AppropriatedThrough;
            return run;
        }

        // ------------------------------------------------------------------ (j)'s reader: the Senate's saved roll calls
        /// <summary>Every saved Senate roll call under `raw/lapse/senate/`, each held to its SHA-256 (a file not held is named in <paramref name="unheld"/> and
        /// not read) and read as the Senate's LIS XML - decompressed where the archive served it gzip-encoded: its identity (S | 119 | its date | its number),
        /// the Democratic caucus's yeas (D and I, R-US10 (a)'s caucus) and its yeas and nays, and its question.</summary>
        private static List<(string Identity, int Defectors, int Voting, string Question)> SenateRolls(out List<string> unheld)
        {
            unheld = new List<string>();
            var rolls = new List<(string Identity, int Defectors, int Voting, string Question)>();
            string dir = Path.Combine(Directory.GetCurrentDirectory(), Rolls);
            if (!Directory.Exists(dir)) { unheld.Add(Rolls); return rolls; }
            foreach (string path in Directory.GetFiles(dir, "vote_119_1_*.xml").OrderBy(p => p, StringComparer.Ordinal))
            {
                string name = Path.GetFileName(path);
                if (!PresidencyDiagnostic.Page(Rolls + name, out _)) { unheld.Add(name); continue; }
                byte[] bytes = File.ReadAllBytes(path);
                string xml;
                if (bytes.Length > 2 && bytes[0] == 0x1f && bytes[1] == 0x8b)
                {
                    using (var gz = new GZipStream(new MemoryStream(bytes), CompressionMode.Decompress))
                    using (var reader = new StreamReader(gz, Encoding.UTF8)) { xml = reader.ReadToEnd(); }
                }
                else { xml = Encoding.UTF8.GetString(bytes); }
                string number = Regex.Match(xml, "<vote_number>0*(\\d+)<").Groups[1].Value;
                string date = Regex.Match(xml, "<vote_date>([A-Za-z]+ \\d+, \\d{4})").Groups[1].Value;
                string question = Regex.Match(xml, "<vote_question_text>([^<]*)<").Groups[1].Value.Trim();
                if (number.Length == 0 || !DateTime.TryParseExact(date, "MMMM d, yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime day)) { unheld.Add(name + " (unread)"); continue; }
                int yeas = 0, nays = 0;
                foreach (Match member in Regex.Matches(xml, "<member>(.*?)</member>", RegexOptions.Singleline))
                {
                    string party = Regex.Match(member.Groups[1].Value, "<party>([A-Z])<").Groups[1].Value;
                    string cast = Regex.Match(member.Groups[1].Value, "<vote_cast>([^<]*)<").Groups[1].Value;
                    if (party != "D" && party != "I") { continue; }
                    if (cast == "Yea") { yeas++; } else if (cast == "Nay") { nays++; }
                }
                rolls.Add(("S|119|" + Day(day) + "|" + number, yeas, yeas + nays, question));
            }
            return rolls;
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
