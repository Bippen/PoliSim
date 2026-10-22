using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using PoliSim.Data;
using PoliSim.Elections;
using PoliSim.Persistence;
using PoliSim.Simulation;
using PoliSim.UI;
using UnityEngine;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// PS-6 US-24 (`docs/specs/USA_STAGE_PLAN.md` US-24's done-when: "a debt-limit diagnostic - a Sweden-start world's AI USA meets the limit on its measured
    /// timetable, its miss against the CRS window stated, and a US game reinstates the limit on 2 Jan 2025"): THE FEDERAL DEBT LIMIT LIVE (`UsDebtLimit`,
    /// `SimulationManager.DebtLimit.cs`), on worlds opened as the game opens them, each under `SimulationManager.EpochScope`, the random streams restored after.
    /// <list type="bullet">
    /// <item>(a) THE CONSTANTS HELD TO THE DOCUMENT: every figure and day `UsDebtLimit` carries against `docs/generated/US_DEBT_LIMIT.md`'s Data block (which
    /// `UsDebtLimitCheck` holds to Treasury's files and the acts), the seed against a fresh world, the bridge's ratio against the engine block.</item>
    /// <item>(b) THE LAW AT THE START (R-US21 (ii), J3) on planted days either side of every step, and printed for every playable start.</item>
    /// <item>(c) A SWEDEN-START WORLD'S AI USA: the debt subject to limit kept day by day by this diagnostic's own walk, and the live bookkeeping held to it -
    /// the day the limit is reached, the raise by act on the day the measures run out (R-US21 (vi)), no cut armed, the ministry writing nothing for the USA at
    /// any boundary (R-US21 (v)); and the reach and the binding day beside CRS IF10292's "early to mid-2027", the miss stated.</item>
    /// <item>(d) A US GAME (the USA's own start, stepped in the controller's order): the suspension reinstated on 2 January 2025 at the game's own debt subject to
    /// limit at the end of 1 January (R-US21 (ii)'s words, no headroom - the review's L2), P.L. 119-21's increase NOT applied on 4 July 2025 (later raises are the game's Congress's acts),
    /// the limit reached that day, the raise tabled a bill's course before the measures run out and law on the day they do, both chambers by reconciliation.</item>
    /// <item>(e) THE CUT, planted on the US world: the limit binding with no raise law - the day tick tables the raise and arms the next day's flows, whose
    /// non-interest outlays are cut to revenue less interest (the debt held), the slip's lines and the national note saying so.</item>
    /// <item>(f) THE SAVE: format 53 carries the limit's fields and the pending raise; a save of the format before is refused (ruling A).</item>
    /// <item>(g) THE TEETH, every run: a planted limit moves this walk's reach (so the comparison in (c) can fail); the AI's raise is not taken a day early, nor
    /// where Congress votes; the cut is 0 off its armed day, never more than the outlays, and nothing where revenue covers them; the reinstatement on its day only.</item>
    /// </list>
    /// </summary>
    public static class UsDebtLimitLiveDiagnostic
    {
        private const string DocRelative = "docs/generated/US_DEBT_LIMIT.md";
        private const int Seed = 777;

        /// <summary>The raise's division title, exactly (the second review's L4) - its figure the UI's own rendering of the raise, which (g) holds equal.</summary>
        private const string RaiseTitle = "Reconciliation (the debt limit): the limit raised by US$5.00T";

        /// <summary>DECLARED - CRS IF10292 v22 (21 Sep 2026): outside projections put debt reaching the limit in "early to mid-2027" - read as 1 January to 30 June 2027.</summary>
        private static readonly DateTime CrsOpens = new DateTime(2027, 1, 1);
        private static readonly DateTime CrsCloses = new DateTime(2027, 6, 30);

        public static void Run()
        {
            CheckExit.ArmLogFold();
            var sb = new StringBuilder("=== UsDebtLimitLiveDiagnostic (PS-6 US-24; R-US21 (i)-(vi) as ruled): the federal debt limit live ===\n");
            int failures = 0;
            void Check(bool ok, string what) { if (!ok) { failures++; } sb.Append(ok ? "    ok        " : "    FAIL      ").Append(what).Append('\n'); }
            var hosts = new List<GameObject>();
            int seedWas = SimulationRandom.MasterSeed;
            Dictionary<SimulationRandom.Stream, int> drawsWas = SimulationRandom.CaptureDrawCounts();
            DateTime epochWas = SimulationManager.EpochDate;
            var watch = System.Diagnostics.Stopwatch.StartNew();
            try
            {
                // ================================================================ (a) the constants held to the document
                string doc = File.ReadAllText(Path.Combine(Path.GetFullPath(Path.Combine(Application.dataPath, "..")), DocRelative), Encoding.UTF8).Replace("\r\n", "\n");
                Dictionary<string, string> data = DataBlock(doc);
                double DocUsd(string key, int field) => double.Parse(data[key].Split('|')[field].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture);
                DateTime DocDay(string key, int field) => DateTime.ParseExact(data[key].Split('|')[field].Trim().Substring(0, 10), "yyyy-MM-dd", CultureInfo.InvariantCulture);
                Check(DocUsd("law.1", 1) == UsDebtLimit.Pl117LimitUsd && DocDay("law.1", 0) == UsDebtLimit.Pl117From
                      && DocDay("law.2", 0) == UsDebtLimit.SuspendedFrom && data["law.2"].Contains("through " + Iso(UsDebtLimit.SuspendedThrough))
                      && DocUsd("law.3", 1) == UsDebtLimit.Pl118ReinstatedUsd && DocDay("law.3", 0) == UsDebtLimit.SuspendedThrough.AddDays(1)
                      && DocUsd("law.4", 1) == UsDebtLimit.Pl119LimitUsd && DocDay("law.4", 0) == UsDebtLimit.Pl119From && DocUsd("law.4", 1) - DocUsd("law.3", 1) == UsDebtLimit.Pl119IncreaseUsd,
                    F("(a) the law of record as `UsDebtLimit` carries it is the document's: {0} {1}; suspended {2} through {3}; {4} {5}; {6} {7} (+{8})", Iso(UsDebtLimit.Pl117From), Usd(UsDebtLimit.Pl117LimitUsd), Iso(UsDebtLimit.SuspendedFrom), Iso(UsDebtLimit.SuspendedThrough),
                        Iso(UsDebtLimit.SuspendedThrough.AddDays(1)), Usd(UsDebtLimit.Pl118ReinstatedUsd), Iso(UsDebtLimit.Pl119From), Usd(UsDebtLimit.Pl119LimitUsd), Usd(UsDebtLimit.Pl119IncreaseUsd)));
                double vintage = double.Parse(data["vintage.dsl_usd"], NumberStyles.Float, CultureInfo.InvariantCulture);
                int spanFrom = int.Parse(data["em.days_to_exhaustion"].Split(new[] { ".." }, StringSplitOptions.None)[0], CultureInfo.InvariantCulture);
                Check(vintage == UsDebtLimit.VintageSubjectToLimitUsd && spanFrom == UsDebtLimit.MeasuresSpanDays
                      && Iso(UsDebtLimit.RecordReached) == data["em.reached"] && data["em.exhaustion"].StartsWith(Iso(UsDebtLimit.RecordExhaustionFirstDay), StringComparison.Ordinal),
                    F("(a) the vintage's debt subject to limit {0}, and the measures' span {1} days ({2} to {3}, the first day of the projected exhaustion - R-US21 (iii) as ruled) are the document's",
                        Usd(vintage), UsDebtLimit.MeasuresSpanDays, Iso(UsDebtLimit.RecordReached), Iso(UsDebtLimit.RecordExhaustionFirstDay)));
                double seedUsd;
                SimulationRandom.LiveState rng = SimulationRandom.CaptureLive();
                try { seedUsd = (double)WorldFactory.CreateDefault().GetCountry(CountryId.USA).State.GovernmentDebt * 1e9; }
                finally { SimulationRandom.Reinstate(rng); }
                Match engine = Regex.Match(doc, @"the game's debt x ([0-9.]+) - the seed's own ratio");
                double engineRatio = engine.Success ? double.Parse(engine.Groups[1].Value, CultureInfo.InvariantCulture) : double.NaN;
                // the review's H1 and L8: the seed as the DOCUMENT's engine block printed it (WriteBridge), apart from the code - a re-seed with no WriteBridge, or a
                // bridge read off the wrong seed, fails here; and the ratio this diagnostic's walk uses is the document's own, its vintage over its printed seed
                Match engineSeed = Regex.Match(doc, @"debt seed \([^)]*\)[^*]*\*\*\$([0-9,]+)\*\*");
                double docSeedUsd = engineSeed.Success ? double.Parse(engineSeed.Groups[1].Value.Replace(",", string.Empty), CultureInfo.InvariantCulture) : double.NaN;
                double docRatio = vintage / docSeedUsd;
                Check(seedUsd == UsDebtLimit.SeedDebtUsd && docSeedUsd == seedUsd && Math.Abs(UsDebtLimit.BridgeRatio - engineRatio) < 5e-7 && Math.Abs(docRatio - UsDebtLimit.BridgeRatio) < 1e-12,
                    F("(a) the seed {0} is a fresh world's US debt, read from the seed's own constant, and the engine block's printed seed ({1}); the bridge {2:0.000000000} is the engine block's {3} and the document's vintage over its seed", Usd(seedUsd), engineSeed.Success ? Usd(docSeedUsd) : "NOT FOUND", UsDebtLimit.BridgeRatio, engine.Success ? engine.Groups[1].Value : "NOT FOUND"));

                // ================================================================ (b) the law at the start
                var planted = new (DateTime Day, double Limit, bool Suspended, DateTime Since)[]
                {
                    (new DateTime(2022, 7, 21), UsDebtLimit.Pl117LimitUsd, false, UsDebtLimit.Pl117From),
                    (UsDebtLimit.SuspendedFrom.AddDays(-1), UsDebtLimit.Pl117LimitUsd, false, UsDebtLimit.Pl117From),
                    (UsDebtLimit.SuspendedFrom, 0d, true, UsDebtLimit.SuspendedFrom),
                    (UsDebtLimit.SuspendedThrough, 0d, true, UsDebtLimit.SuspendedFrom),
                    (UsDebtLimit.SuspendedThrough.AddDays(1), UsDebtLimit.Pl118ReinstatedUsd, false, UsDebtLimit.SuspendedThrough.AddDays(1)),
                    (UsDebtLimit.Pl119From.AddDays(-1), UsDebtLimit.Pl118ReinstatedUsd, false, UsDebtLimit.SuspendedThrough.AddDays(1)),
                    (UsDebtLimit.Pl119From, UsDebtLimit.Pl119LimitUsd, false, UsDebtLimit.Pl119From),
                };
                var probe = new Country(CountryId.USA, "probe", new EconomyState(gdp: 1000f, inflation: 2f, unemployment: 4f, approvalRating: 50f, budget: 0f), null, 0f);
                var wrong = new List<string>();
                foreach (var p in planted)
                {
                    UsDebtLimit.ReadLaw(probe, p.Day);
                    if (probe.DebtLimitUsd != p.Limit || (probe.DebtLimitSuspendedThrough != DateTime.MinValue) != p.Suspended || probe.DebtLimitSince != p.Since) { wrong.Add(Iso(p.Day) + ": " + UsDebtLimit.LawWords(probe)); }
                }
                Check(wrong.Count == 0, F("(b) the law in force at the start on {0} planted days either side of every step (J3: the law in force that day){1}", planted.Length, wrong.Count == 0 ? string.Empty : " - WRONG: " + string.Join("; ", wrong)));
                var starts = new List<string>();
                foreach (CountryId id in (CountryId[])Enum.GetValues(typeof(CountryId)))
                {
                    foreach (StartPoints.StartPoint sp in StartPoints.For(id)) { if (!sp.Playable) { continue; } UsDebtLimit.ReadLaw(probe, sp.Opens); starts.Add(F("{0} {1:yyyy-MM-dd}: {2}", id, sp.Opens, UsDebtLimit.LawWords(probe))); }
                }
                sb.Append("    read      THE LAW AT EACH PLAYABLE START (R-US21 (ii), the USA's law whoever plays): ").Append(string.Join("; ", starts)).Append('\n');

                using (SimulationManager.EpochScope())
                {
                    try
                    {
                        // ============================================================ (c) a Sweden-start world's AI USA
                        SimulationRandom.Seed(Seed);
                        EnergyMarket.ResetCalibration();
                        WorldClock.ApplyStart(CountryId.Sweden);
                        DateTime opens = SimulationManager.EpochDate;
                        World ws = WorldFactory.CreateDefault();
                        var gos = new GameObject("UsDebtLimitLive.sweden"); hosts.Add(gos);
                        SimulationManager ss = gos.AddComponent<SimulationManager>();
                        ss.SetWorld(ws);
                        ss.PlayerCountryId = CountryId.Sweden;
                        Country au = ws.GetCountry(CountryId.USA);
                        var none = new Dictionary<CountryId, PolicyDecision>();
                        foreach (Country k in ws.Countries) { none[k.Id] = PolicyDecision.None(); }
                        var series = new List<(DateTime Day, double Dsl)>();
                        DateTime liveReach = DateTime.MinValue, liveRaise = DateTime.MinValue;
                        bool armed = false, ministryWrote = false;
                        int boundaries = 0;
                        DateTime stopBy = opens.AddYears(3);
                        while (ss.CurrentDate < stopBy)
                        {
                            bool boundary = ss.AdvanceDay();
                            series.Add((ss.CurrentDate, (double)au.State.GovernmentDebt * 1e9 * docRatio));   // this diagnostic's own bridge: the document's vintage over the document's printed seed
                            if (liveReach == DateTime.MinValue && au.DebtLimitReachedOn != DateTime.MinValue) { liveReach = au.DebtLimitReachedOn; }
                            if (liveRaise == DateTime.MinValue && au.DebtLimitRaises > 0) { liveRaise = au.DebtLimitSince; }
                            armed |= au.DebtLimitCutFlowDay != DateTime.MinValue || au.DebtLimitCutBillions != 0f;
                            if (boundary)
                            {
                                PolicyDecision eve = AiFinanceMinistry.Decide(au, ss.GetLastFiscalReport(CountryId.USA));
                                ministryWrote |= eve.SpendingLineChanges.Count > 0 || eve.TaxRateOverrides.Count > 0;
                                ss.AdvanceTurn(none);
                                boundaries++;
                            }
                            if (liveRaise != DateTime.MinValue && ss.CurrentDate > liveRaise.AddDays(1)) { break; }
                        }
                        DateTime walkReach = FirstReach(series, UsDebtLimit.Pl119LimitUsd);
                        DateTime walkRaise = walkReach == DateTime.MinValue ? DateTime.MinValue : walkReach.AddDays(spanFrom);
                        // the review's L8: the comparison below can fail - a bridge 1 per cent high, or a limit a day and a half's borrowing lower, moves the walk's reach
                        int reachAt = series.FindIndex(x => x.Day == walkReach);
                        double dayBorrowing = reachAt > 0 ? series[reachAt].Dsl - series[reachAt - 1].Dsl : double.NaN;
                        DateTime wrongRatio = FirstReach(series.Select(x => (x.Day, x.Dsl * 1.01)).ToList(), UsDebtLimit.Pl119LimitUsd);
                        DateTime wrongLimit = FirstReach(series, UsDebtLimit.Pl119LimitUsd - 1.5 * dayBorrowing);
                        Check(dayBorrowing > 0d && wrongRatio != DateTime.MinValue && wrongRatio < walkReach && wrongLimit != DateTime.MinValue && wrongLimit < walkReach,
                            F("(c) teeth: a bridge 1 per cent high reaches on {0}, a limit a day and a half's borrowing lower ({1}) on {2} - each before {3}, so a wrong bridge or a wrong limit fails the comparison", Day(wrongRatio), Usd(1.5 * dayBorrowing), Day(wrongLimit), Day(walkReach)));
                        Check(UsDebtLimit.IsRead(au) && opens > UsDebtLimit.Pl119From && walkReach != DateTime.MinValue && liveReach == walkReach,
                            F("(c) SWEDEN'S START ({0}, the USA AI-governed, no Congress of the game seated - {1}): the law in force at the start is P.L. 119-21's {2}; the debt subject to limit reaches it on {3} by this diagnostic's own walk, and the live bookkeeping reached it on {4}",
                                Iso(opens), UsBudgetProcedure.Applies(au) ? "a Congress votes - WRONG" : "none votes", Usd(UsDebtLimit.Pl119LimitUsd), Day(walkReach), Day(liveReach)));
                        Check(liveRaise == walkRaise && au.DebtLimitRaises == 1 && au.DebtLimitUsd == UsDebtLimit.Pl119LimitUsd + UsDebtLimit.RaiseUsd && !armed,
                            F("(c) the measures run {0} days to {1}, and that day the AI-governed USA raises the limit by act (R-US21 (vi)) by {2} to {3} - live: raised on {4}, {5} raise(s), the limit {6}; no cut was ever armed",
                                spanFrom, Day(walkRaise), Usd(UsDebtLimit.RaiseUsd), Usd(UsDebtLimit.Pl119LimitUsd + UsDebtLimit.RaiseUsd), Day(liveRaise), au.DebtLimitRaises, Usd(au.DebtLimitUsd)));
                        Check(!ministryWrote && boundaries > 0, F("(c) R-US21 (v): at each of the {0} boundaries crossed the AI ministry writes nothing for the USA (no line, no rate) - the dollar limit is its rule", boundaries));
                        int seedDays = walkReach == DateTime.MinValue ? -1 : (walkReach - opens).Days;
                        DateTime vintageDay = DateTime.ParseExact(data["vintage.date"], "yyyy-MM-dd", CultureInfo.InvariantCulture);
                        sb.Append(F("    read      THE CRS WINDOW (IF10292 v22, \"early to mid-2027\", DECLARED as {0} to {1}): the game reaches the limit on {2} - {3}; the measures run out, and the AI raises it, on {4} - {5}. The game's calendar opens on Sweden's start ({6}) on a seed of {7}'s state: carried from the seed's own month-end the same {8} days land on {9} - {10}.\n",
                            Iso(CrsOpens), Iso(CrsCloses), Day(walkReach), Against(walkReach), Day(walkRaise), Against(walkRaise), Iso(opens), Iso(vintageDay), seedDays, Day(vintageDay.AddDays(Math.Max(0, seedDays))), Against(vintageDay.AddDays(Math.Max(0, seedDays)))));

                        // ============================================================ (d) a US game: the reinstatement on 2 January 2025
                        DateTime usStart = WorldClock.StartDate(CountryId.USA);
                        (SimulationManager s, World w, Country u) = UsCongressDiagnostic.OpenUsa(hosts, usStart, "REP");
                        s.PlayerPresidentVetoes = _ => false;   // a player-president signs on the day: the raise is law the day it is voted
                        DateTime jan1 = UsDebtLimit.SuspendedThrough, jan2 = jan1.AddDays(1);
                        double dslJan1 = double.NaN;
                        while (s.CurrentDate < jan2)
                        {
                            bool boundary = s.AdvanceDay();
                            if (s.CurrentDate == jan1) { dslJan1 = UsDebtLimit.SubjectToLimitUsd(u); }   // the day's close, as the bookkeeping read it after the day's flows - before the day tick
                            s.AdvanceCountryDayTick(CountryId.USA);
                            if (boundary) { Turn(s, w); }
                        }
                        Check(UsBudgetProcedure.Applies(u) && dslJan1 > 0d && u.DebtLimitSince == jan2 && u.DebtLimitSuspendedThrough == DateTime.MinValue && Math.Abs(u.DebtLimitUsd - dslJan1) < 1d,
                            F("(d) A US GAME (opened {0}, the record's Congress seated, the player REP): suspended through {1} (P.L. 118-5 s401(a)); REINSTATED from {2} at the game's own debt subject to limit at the end of {1}, {3} - the limit {4} (the record's figure, {5}, is not the game's)",
                                Iso(usStart), Iso(jan1), Day(u.DebtLimitSince), Usd(dslJan1), Usd(u.DebtLimitUsd), Usd(UsDebtLimit.Pl118ReinstatedUsd)));
                        double reinstated = u.DebtLimitUsd;
                        Check(u.DebtLimitReachedOn == jan2, F("(d) the limit is reached on {0} - the day's deficit takes the debt over it - and the measures run {1} days, to {2}", Day(u.DebtLimitReachedOn), UsDebtLimit.MeasuresSpanDays, Day(UsDebtLimit.BindsOn(u))));
                        DateTime bindsOn = UsDebtLimit.BindsOn(u), tabled = DateTime.MinValue;
                        double onTheFifth = double.NaN;
                        bool armedBefore = false;
                        while (s.CurrentDate < bindsOn && u.DebtLimitRaises == 0)
                        {
                            StepOne(s, w);
                            if (tabled == DateTime.MinValue && s.GetPendingDebtLimitBill(CountryId.USA) != null) { tabled = s.CurrentDate; }
                            if (s.CurrentDate == UsDebtLimit.Pl119From.AddDays(1)) { onTheFifth = u.DebtLimitUsd; }
                            armedBefore |= u.DebtLimitCutFlowDay != DateTime.MinValue;
                        }
                        Check(onTheFifth == reinstated, F("(d) R-US21 (ii)(a): on {0} the limit is still the game's {1} - P.L. 119-21's increase of 4 July 2025 is the record's, and later raises are the game's Congress's acts", Iso(UsDebtLimit.Pl119From.AddDays(1)), Usd(onTheFifth)));
                        DivisionRecord senate = u.Divisions.Entries.LastOrDefault(d => d.Title != null && d.Title.StartsWith("Reconciliation (the debt limit)", StringComparison.Ordinal) && d.Chamber == Congress.Senate);
                        DivisionRecord house = u.Divisions.Entries.LastOrDefault(d => d.Title != null && d.Title.StartsWith("Reconciliation (the debt limit)", StringComparison.Ordinal) && d.Chamber == Congress.House);
                        Check(tabled == bindsOn.AddDays(-ParliamentSystem.BillDurationDays) && u.DebtLimitRaises == 1 && u.DebtLimitSince == bindsOn && u.DebtLimitUsd == reinstated + UsDebtLimit.RaiseUsd && !armedBefore
                              && senate != null && house != null && senate.Passed && house.Passed && senate.Reconciliation && !senate.Cloture && s.GetPendingDebtLimitBill(CountryId.USA) == null && senate.Title == RaiseTitle && house.Title == RaiseTitle,
                            F("(d) the raise tabled {0} by the government of the day, a bill's course ({1} days) before the measures run out; voted on {2} - the House {3}, the Senate {4} by reconciliation (no cloture), titled \"{6}\" in both (no world's own figure - FORK-2's identity, the review's M2) - and law that day: the limit {5}; no cut armed",
                                Day(tabled), ParliamentSystem.BillDurationDays, Day(u.DebtLimitSince), house?.Passed == true ? "passed" : "FAILED", senate?.Passed == true ? "passed" : "FAILED", Usd(u.DebtLimitUsd), senate?.Title));

                        // ============================================================ (e) the cut, planted
                        float cutBefore = u.DebtLimitCutBillions;
                        float debtA = u.State.GovernmentDebt;
                        StepOne(s, w);
                        float unarmedDay = u.State.GovernmentDebt - debtA;
                        u.DebtLimitUsd = UsDebtLimit.SubjectToLimitUsd(u) - 1e9;
                        u.DebtLimitReachedOn = s.CurrentDate.AddDays(-UsDebtLimit.MeasuresSpanDays);
                        StepOne(s, w);   // the bookkeeping sees the limit binding; the day tick tables the raise and arms the next day's flows
                        DateTime armedFor = u.DebtLimitCutFlowDay;
                        BudgetBill pendingRaise = s.GetPendingDebtLimitBill(CountryId.USA);
                        float debtB = u.State.GovernmentDebt;
                        const float BackPay = 7f;
                        u.LapseDeferredOutlays = BackPay;   // the review's M3: a lapse's back pay owed on the armed day - it must stay owed, not be cut
                        var periods = typeof(SimulationManager).GetField("_fiscalPeriods", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)?.GetValue(s) as Dictionary<CountryId, SimulationManager.FiscalPeriod>;
                        float lineBefore = periods != null && periods.TryGetValue(CountryId.USA, out SimulationManager.FiscalPeriod p0) ? p0.AccruedDebtLimitCut : float.NaN;
                        StepOne(s, w);   // the armed day: the flows cut to revenue
                        float armedDay = u.State.GovernmentDebt - debtB;
                        float cut = u.DebtLimitCutBillions - cutBefore;
                        float lineAfter = periods != null && periods.TryGetValue(CountryId.USA, out SimulationManager.FiscalPeriod p1) ? p1.AccruedDebtLimitCut : float.NaN;
                        Check(u.LapseDeferredOutlays == BackPay && Mathf.Abs((lineAfter - lineBefore) - cut) < 1e-3f * Mathf.Max(1f, cut),
                            F("(e) the back pay owed ({0} bn) stays owed on the armed day ({1} bn after) - paid in full the first day the cut is not armed (the review's M3); the period books the day's cut as its own line ({2:0.000} bn; the review's M4)", BackPay, u.LapseDeferredOutlays, lineAfter - lineBefore));
                        Check(armedFor == s.CurrentDate && pendingRaise != null && unarmedDay > 1f && Mathf.Abs(armedDay) < 0.05f * unarmedDay && cut > 0.5f * unarmedDay && cut < 2f * unarmedDay,
                            F("(e) THE CUT (planted: the limit {0} under the debt, the measures run out): the day tick tables the raise and arms {1}; that day the debt moves {2:0.000} bn where the day before, unarmed, it moved {3:0.000} bn - the day's non-interest outlays cut by {4:0.000} bn to what revenue leaves after interest (R-US21 (iv))",
                                Usd(1e9), Day(armedFor), armedDay, unarmedDay, cut));
                        List<string> lines = FiscalRules.DebtLimitLines(u, s.CurrentDate, s.GetPendingDebtLimitBill(CountryId.USA));
                        string note = FiscalRules.NationalNote(u, s.CurrentDate);
                        Check(!lines.Any(l => l.Contains("BORROWED THROUGH THEM") || l.Contains("HELD AT IT BY EXTRAORDINARY MEASURES")) && lines.Any(l => l.Contains("ABOVE IT · BORROWED THROUGH THE MEASURES TO")) && lines.Any(l => l.Contains("CUT SO FAR")) && lines.Any(l => l.Contains("IS BEFORE CONGRESS")) && lines.Any(l => l.StartsWith("THE FEDERAL DEBT LIMIT ", StringComparison.Ordinal)) && note.Contains("cut to revenue")
                              && FiscalRules.NationalNote(ws.GetCountry(CountryId.Germany), s.CurrentDate) == FiscalRules.NationalNote(CountryId.Germany),
                            F("(e) the slip says so - \"{0}\"; the national note: \"{1}\"; every other country's note unchanged", string.Join(" | ", lines), note));

                        // ============================================================ (f) the save
                        string json = SaveGameService.Serialize(SaveGameService.CreateSaveGame(s, w, CountryId.USA, null));
                        SaveGame saved = SaveGameService.Deserialize(json);
                        Country su = saved.World.GetCountry(CountryId.USA);
                        Check(saved.SaveVersion == SaveGameService.CurrentSaveVersion && saved.SaveVersion >= 53 && su.DebtLimitUsd == u.DebtLimitUsd && su.DebtLimitSince == u.DebtLimitSince && su.DebtLimitReachedOn == u.DebtLimitReachedOn
                              && su.DebtLimitCutFlowDay == u.DebtLimitCutFlowDay && su.DebtLimitCutBillions == u.DebtLimitCutBillions && su.DebtLimitRaises == u.DebtLimitRaises
                              && saved.Sim?.PendingDebtLimitBills != null && saved.Sim.PendingDebtLimitBills.TryGetValue(CountryId.USA, out BudgetBill savedRaise) && savedRaise.DaysRemaining == pendingRaise.DaysRemaining,
                            F("(f) a save cut on {0} carries format {1}, the limit's fields and the raise before Congress ({2} day(s) to its vote)", Day(s.CurrentDate), saved.SaveVersion, pendingRaise.DaysRemaining));
                        string older = json.Replace("\"SaveVersion\": " + SaveGameService.CurrentSaveVersion.ToString(CultureInfo.InvariantCulture), "\"SaveVersion\": " + (SaveGameService.CurrentSaveVersion - 1).ToString(CultureInfo.InvariantCulture));
                        bool refused = false; string why = null;
                        try { SaveGameService.Deserialize(older); } catch (SaveLoadException e) { refused = true; why = e.Message; }
                        Check(older != json && refused, F("(f) a save of format {0} is refused with the plain message (ruling A): \"{1}\"", SaveGameService.CurrentSaveVersion - 1, why));

                        // ============================================================ (e2) the back pay carried through the raise, on the day path (the second review's L4)
                        string firstTitle = senate?.Title;
                        double limitBefore = u.DebtLimitUsd;
                        int raisesBefore = u.DebtLimitRaises, guard = 0;
                        while (u.DebtLimitRaises == raisesBefore && guard++ < ParliamentSystem.BillDurationDays + 12) { StepOne(s, w); }
                        bool owedOnRaiseDay = u.LapseDeferredOutlays == BackPay && u.DebtLimitCutFlowDay == DateTime.MinValue;
                        float debtC = u.State.GovernmentDebt;
                        StepOne(s, w);   // the first day the cut is not armed: the back pay is paid in full
                        float paidDay = u.State.GovernmentDebt - debtC;
                        DivisionRecord secondSenate = u.Divisions.Entries.LastOrDefault(d => d.Title != null && d.Title.StartsWith("Reconciliation (the debt limit)", StringComparison.Ordinal) && d.Chamber == Congress.Senate);
                        Check(u.DebtLimitRaises == raisesBefore + 1 && owedOnRaiseDay && u.LapseDeferredOutlays == 0f && paidDay - unarmedDay > 0.5f * BackPay && paidDay - unarmedDay < 1.5f * BackPay,
                            F("(e2) the planted binding's raise becomes law on {0} by Congress's act; the back pay ({1} bn) is still owed that day, and the first unarmed day pays it in full - the debt moves {2:0.000} bn against an unarmed day's {3:0.000} bn", Day(u.DebtLimitSince), BackPay, paidDay, unarmedDay));
                        Check(secondSenate != null && !ReferenceEquals(secondSenate, senate) && secondSenate.Title == firstTitle && firstTitle == RaiseTitle && u.DebtLimitUsd != limitBefore && limitBefore != reinstated,
                            F("(e2) the two raises' divisions, voted on limits {0} and {1}, carry one title, the exact \"{2}\" - no world's figure (FORK-2's identity)", Usd(reinstated), Usd(limitBefore), RaiseTitle));
                    }
                    finally { EnergyMarket.ResetTurnState(); }
                }

                // ================================================================ (g) the teeth
                {
                    var tooth = new List<(DateTime, double)> { (new DateTime(2030, 1, 1), 10d), (new DateTime(2030, 1, 2), 20d), (new DateTime(2030, 1, 3), 30d) };
                    Check(FirstReach(tooth, 20d) == new DateTime(2030, 1, 2) && FirstReach(tooth, 15d) == new DateTime(2030, 1, 2) && FirstReach(tooth, 25d) == new DateTime(2030, 1, 3) && FirstReach(tooth, 31d) == DateTime.MinValue,
                        "(g) this walk's reach moves with the limit (a planted series: 20 -> the second day, 25 -> the third, 31 -> never), so (c)'s comparison can fail");
                    var t = new Country(CountryId.USA, "tooth", new EconomyState(gdp: 1000f, inflation: 2f, unemployment: 4f, approvalRating: 50f, budget: 0f, governmentDebt: 1000f), null, 0f);
                    DateTime d0 = new DateTime(2030, 6, 1);
                    t.DebtLimitSince = new DateTime(2029, 1, 1); t.DebtLimitUsd = 1e12;   // the debt subject to limit about 1.09 tn, over it
                    t.DebtLimitReachedOn = d0.AddDays(-UsDebtLimit.MeasuresSpanDays + 1);
                    UsDebtLimit.Track(t, d0, d0, false);
                    bool earlyHeld = t.DebtLimitRaises == 0;
                    UsDebtLimit.Track(t, d0.AddDays(1), d0, true);
                    bool congressHeld = t.DebtLimitRaises == 0;
                    UsDebtLimit.Track(t, d0.AddDays(1), d0, false);
                    Check(earlyHeld && congressHeld && t.DebtLimitRaises == 1 && t.DebtLimitUsd == 1e12 + UsDebtLimit.RaiseUsd && t.DebtLimitReachedOn == DateTime.MinValue,
                        "(g) the AI's raise is not taken the day before the measures run out, nor where a Congress of the game votes; it is taken on the day, and closes the episode");
                    t.DebtLimitCutFlowDay = d0;
                    float off = UsDebtLimit.CutToday(t, d0.AddDays(1), 1f, 0f, 10f), under = UsDebtLimit.CutToday(t, d0, 1f, 3f, 10f), covered = UsDebtLimit.CutToday(t, d0, 20f, 3f, 10f), part = UsDebtLimit.CutToday(t, d0, 8f, 3f, 10f);
                    Check(off == 0f && under == 10f && covered == 0f && part == 5f,
                        F("(g) the cut is 0 off its armed day ({0}), all the non-interest outlays where revenue does not cover interest ({1} of 10), nothing where revenue covers everything ({2}), and what exceeds revenue less interest otherwise ({3}: 10 - (8 - 3))", off, under, covered, part));
                    t.DebtLimitSuspendedThrough = d0; t.DebtLimitUsd = 0d; t.DebtLimitReachedOn = DateTime.MinValue;
                    UsDebtLimit.Track(t, d0.AddDays(-1), d0, true);
                    bool waited = t.DebtLimitUsd == 0d && t.DebtLimitSuspendedThrough == d0;
                    UsDebtLimit.Track(t, d0, d0, true);
                    Check(waited && Math.Abs(t.DebtLimitUsd - UsDebtLimit.SubjectToLimitUsd(t)) < 1d && t.DebtLimitSince == d0.AddDays(1) && t.DebtLimitSuspendedThrough == DateTime.MinValue,
                        "(g) a suspension is reinstated on its last day's close for the next day - and not the day before");
                    t.DebtLimitCutFlowDay = d0; t.LapseDeferredOutlays = 3f; t.LapseFlowDay = DateTime.MinValue;
                    float held = UsLapse.OutlayToday(t, d0, 100f); float stillOwed = t.LapseDeferredOutlays;
                    float paid = UsLapse.OutlayToday(t, d0.AddDays(1), 100f);
                    Check(held == 0f && stillOwed == 3f && paid == 3f && t.LapseDeferredOutlays == 0f && UsDebtLimit.BillTitle() == RaiseTitle && UsDebtLimit.RaiseWords == UiFormat.Money((float)(UsDebtLimit.RaiseUsd / 1e9), MoneyUnit.Billions),
                        F("(g) a lapse's back pay is held on an armed day ({0}, {1} still owed) and paid in full the next ({2}); the raise's title carries no world's figure: \"{3}\"", held, stillOwed, paid, UsDebtLimit.BillTitle()));
                }
            }
            catch (Exception e) { failures++; sb.Append("    FAIL      threw - ").Append(e.GetType().Name).Append(": ").Append(e.Message).Append('\n').Append(e.StackTrace).Append('\n'); }
            finally
            {
                foreach (GameObject go in hosts) { if (go != null) { UnityEngine.Object.DestroyImmediate(go); } }
                SimulationRandom.RestoreState(seedWas, drawsWas);
                UsVeto.ResetDrawCalls();
            }
            Check(SimulationManager.EpochDate == epochWas, F("the epoch is put back ({0})", Iso(SimulationManager.EpochDate)));
            sb.Append(F("    read      the diagnostic ran {0:F1} s\n", watch.Elapsed.TotalSeconds));
            sb.Append(failures == 0 ? "=== UsDebtLimitLiveDiagnostic: the AI USA of a Sweden-start world meets the limit on its walk's day and raises it when the measures run out; a US game reinstates it on 2 Jan 2025 and its Congress raises it; the cut holds spending to revenue ===\n"
                                    : F("=== UsDebtLimitLiveDiagnostic: {0} FAILURE(S) ===\n", failures));
            if (failures == 0) { Debug.Log(sb.ToString()); CheckExit.Finish(0); }
            else { Debug.LogError(sb.ToString()); CheckExit.Finish(1); }
        }

        /// <summary>One day in the controller's order (the day, the player's day tick, the boundary's turn).</summary>
        private static void StepOne(SimulationManager s, World w)
        {
            bool boundary = s.AdvanceDay();
            s.AdvanceCountryDayTick(CountryId.USA);
            if (boundary) { Turn(s, w); }
        }

        private static void Turn(SimulationManager s, World w)
        {
            var none = new Dictionary<CountryId, PolicyDecision>();
            foreach (Country c in w.Countries) { none[c.Id] = PolicyDecision.None(); }
            s.AdvanceTurn(none);
        }

        /// <summary>The first day of <paramref name="series"/> whose debt subject to limit is at or over <paramref name="limitUsd"/>; MinValue where none is.</summary>
        private static DateTime FirstReach(List<(DateTime Day, double Dsl)> series, double limitUsd)
        {
            foreach ((DateTime day, double dsl) in series) { if (dsl >= limitUsd) { return day; } }
            return DateTime.MinValue;
        }

        /// <summary>A day against the CRS window, in days before its first day or after its last.</summary>
        private static string Against(DateTime day)
        {
            if (day == DateTime.MinValue) { return "not reached"; }
            if (day < CrsOpens) { return F("{0} days before the window opens", (CrsOpens - day).Days); }
            if (day > CrsCloses) { return F("{0} days after it closes", (day - CrsCloses).Days); }
            return "inside it";
        }

        private static Dictionary<string, string> DataBlock(string doc)
        {
            int a = doc.IndexOf("<!-- US14-DATA BEGIN -->", StringComparison.Ordinal), b = doc.IndexOf("<!-- US14-DATA END -->", StringComparison.Ordinal);
            if (a < 0 || b < a) { throw new InvalidDataException(DocRelative + " carries no Data block"); }
            var lines = new Dictionary<string, string>();
            foreach (string raw in doc.Substring(a, b - a).Split('\n'))
            {
                int eq = raw.IndexOf(" = ", StringComparison.Ordinal);
                if (eq > 0) { lines[raw.Substring(0, eq).Trim()] = raw.Substring(eq + 3).Trim(); }
            }
            return lines;
        }

        private static string Usd(double usd) => (usd < 0 ? "-$" : "$") + Math.Abs(usd).ToString("#,0.##", CultureInfo.InvariantCulture);
        private static string Iso(DateTime d) => d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        private static string Day(DateTime d) => d == DateTime.MinValue ? "none" : Iso(d);
        private static string F(string format, params object[] args) => string.Format(CultureInfo.InvariantCulture, format, args);
    }
}
