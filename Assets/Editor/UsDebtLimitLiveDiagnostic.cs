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
    /// the limit reached that day; PS-6 K23: the player-president's government tables nothing by itself - the President proposes the raise a bill's course
    /// before the measures run out, voted at the deadline (K25) and law on the day they run out, both chambers by reconciliation.
    /// K215 (MF q29's sim929): from 3 January 2025 the (d)-(n) world carries the RECORD's 119th Congress - the House and the Senate of record, REP in
    /// both - planted after each day's seating (StepOne, K78's StepPlanted pattern), so these sections test the debt limit's mechanics and not the
    /// game's own 2024 count: that count reads the economy (the economic vote), and once MF q29's gap form moved the 2024 path it seated a DEM House
    /// whose hostile majority (K78) withheld the raise. (n) plants its own House over it, as before.</item>
    /// <item>(e) THE CUT, planted on the US world: the limit binding with no raise law - the day tick arms the next day's flows (and tables nothing for a
    /// player-president), whose non-interest outlays are cut to revenue less interest (the debt held), the slip's lines and the national note saying so;
    /// (e3) PS-6 K26: the budget preview's year cut while it binds, and not with nothing armed.</item>
    /// <item>(f) THE SAVE: the current format carries the limit's fields; a save of the format before is refused (ruling A). (e2) PS-6 K43: a raise proposed past
    /// the deadline is voted the day it is tabled.</item>
    /// <item>(h) PS-6 K22 and K43: the president's own party may not withhold the raise; a planted withholder holding a chamber stops one, the slip naming the
    /// cause; its withhold lapses with the presidency; proposed again, the raise is law that day. (j) K23/K25: a suspension proposed while the measures run is
    /// saved with its fields and voted at the deadline; (i) K22: law, no limit applies to its day, and the day after the limit returns at that day's debt.
    /// (m) PS-6 K75: the player-president's own party may withhold the continuing resolution, and its withhold stands.</item>
    /// <item>(g) THE TEETH, every run: a planted limit moves this walk's reach (so the comparison in (c) can fail); the AI's raise is not taken a day early, nor
    /// where Congress votes; the cut is 0 off its armed day, never more than the outlays, and nothing where revenue covers them; the reinstatement on its day only;
    /// PS-6 K77: the AI's raise the excess and its size; PS-6 K22: who may withhold (a party holding a chamber against the presidency).</item>
    /// </list>
    /// </summary>
    public static class UsDebtLimitLiveDiagnostic
    {
        private const string DocRelative = "docs/generated/US_DEBT_LIMIT.md";
        private const int Seed = 777;

        /// <summary>The raise's division title, exactly (the second review's L4) - its figure the UI's own rendering of the raise, which (g) holds equal.</summary>
        private const string RaiseTitle = "Reconciliation (the debt limit): the limit raised by the excess and US$5.00T";   // PS-6 K77: the size added to the excess

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

                // ================================================================ (a2) PS-6 K78: the hostile-majority episodes held to their files
                {
                    string root = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
                    string dir = Path.Combine(root, "ElectionsData/usa/raw/debtlimit_episodes");
                    string Text(string file) => File.ReadAllText(Path.Combine(dir, file), Encoding.UTF8);
                    int unheld = 0;
                    foreach (string line in File.ReadAllLines(Path.Combine(dir, "SHA256SUMS.txt")))
                    {
                        Match m = Regex.Match(line, @"^([0-9a-f]{64}) \*(.+)$");
                        if (!m.Success) { continue; }
                        using (var sha = System.Security.Cryptography.SHA256.Create())
                        {
                            string got = BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(Path.Combine(dir, m.Groups[2].Value)))).Replace("-", string.Empty).ToLowerInvariant();
                            if (got != m.Groups[1].Value) { unheld++; }
                        }
                    }
                    string crs = Regex.Replace(Regex.Replace(Text("secondary_everycrsreport_R43389.html"), "<[^>]+>", " ").Replace("&nbsp;", " "), @"\s+", " ");
                    string reading = File.ReadAllText(Path.Combine(root, "ElectionsData/usa/debt_limit.md"), Encoding.UTF8);
                    bool Roll(string file, DateTime day, string result) => Text(file).Contains("<action-date>" + day.ToString("d-MMM-yyyy", CultureInfo.InvariantCulture) + "</action-date>") && Text(file).Contains("<vote-result>" + result + "</vote-result>");
                    bool Approved(string file, DateTime day) => Text(file).Contains("Approved " + day.ToString("MMMM d, yyyy", CultureInfo.InvariantCulture) + ".");
                    UsDebtLimit.HostileEpisode e11 = UsDebtLimit.HostileEpisodes[0], e13 = UsDebtLimit.HostileEpisodes[1], e23 = UsDebtLimit.HostileEpisodes[2];
                    bool held11 = crs.Contains("On May 16, 2011, U.S. Treasury Secretary Timothy Geithner announced that the federal debt had reached its statutory limit") && e11.Reached == new DateTime(2011, 5, 16)
                                  && crs.Contains("its borrowing authority would be exhausted on August 2") && e11.XDate == new DateTime(2011, 8, 2)
                                  && Roll("clerk_roll_2011_379.xml", e11.FirstRefused, "Failed") && Approved("govinfo_PLAW-112publ25.htm", e11.Law);
                    bool held13 = crs.Contains("May 18, 2013 16,699 May 20, 2013") && e13.Reached == new DateTime(2013, 5, 20) && Text("govinfo_PLAW-113publ3.htm").Contains("ending on May 18, 2013")
                                  && crs.Contains("exhaust its borrowing capacity no later than October 17") && e13.XDate == new DateTime(2013, 10, 17)
                                  && e13.FirstRefused == DateTime.MinValue && Approved("govinfo_PLAW-113publ46.htm", e13.Law);
                    bool held23 = reading.Contains("beginning on January 19, the outstanding debt of the United States was projected to reach the statutory limit") && e23.Reached == new DateTime(2023, 1, 19)
                                  && reading.Contains("if Congress has not raised or suspended the debt limit by June 5") && e23.XDate == new DateTime(2023, 6, 5)
                                  && Roll("clerk_roll_2023_199.xml", e23.FirstRefused, "Passed") && Approved("govinfo_PLAW-118publ5.htm", e23.Law);
                    bool no2021 = crs.Contains("On September 27, 2021, the Senate declined to close further debate on the bill") && Approved("govinfo_PLAW-117publ50.htm", new DateTime(2021, 10, 14))
                                  && UsDebtLimit.HostileEpisodes.All(x => x.Name != "2021");
                    Check(unheld == 0 && held11 && held13 && held23 && no2021 && UsDebtLimit.HostileEpisodes.Length == 3,
                        F("(a2) PS-6 K78: the hostile-majority episodes held to their files (every file of raw/debtlimit_episodes/ to its digest - {0} not): 2011 {1}, 2013 {2}, 2023 {3}; 2021 left out (a Senate minority's filibuster, no hostile majority) {4}",
                            unheld, held11 ? "held" : "NOT HELD", held13 ? "held" : "NOT HELD", held23 ? "held" : "NOT HELD", no2021 ? "as read" : "NOT AS READ"));
                    Check(UsDebtLimit.HostileEpisodes.Select(x => x.LeadDays).SequenceEqual(new[] { 0, 0, 2 }) && UsDebtLimit.AiHoldDaysPastDeadline == 2,
                        F("(a2) K78's rule as K89 amends it, [AUTHORED-DRAFT] on n = {0}: the leads (X-date less law) {1} days, their largest {2} - the AI's hostile majority holds that many days past the day the measures run out, so the cut binds them, and releases before that day's vote",
                            UsDebtLimit.HostileEpisodes.Length, string.Join(", ", UsDebtLimit.HostileEpisodes.Select(x => x.Name + " " + x.LeadDays)), UsDebtLimit.AiHoldDaysPastDeadline));
                }

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
                        double dslAtRaise = double.NaN;
                        bool armed = false, ministryWrote = false;
                        int boundaries = 0;
                        DateTime stopBy = opens.AddYears(3);
                        while (ss.CurrentDate < stopBy)
                        {
                            bool boundary = ss.AdvanceDay();
                            series.Add((ss.CurrentDate, (double)au.State.GovernmentDebt * 1e9 * docRatio));   // this diagnostic's own bridge: the document's vintage over the document's printed seed
                            if (liveReach == DateTime.MinValue && au.DebtLimitReachedOn != DateTime.MinValue) { liveReach = au.DebtLimitReachedOn; }
                            if (liveRaise == DateTime.MinValue && au.DebtLimitRaises > 0) { liveRaise = au.DebtLimitSince; dslAtRaise = UsDebtLimit.SubjectToLimitUsd(au); }   // the day's close the raise was made on
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
                        Check(liveRaise == walkRaise && au.DebtLimitRaises == 1 && Math.Abs(au.DebtLimitUsd - (dslAtRaise + UsDebtLimit.RaiseUsd)) < 1d && dslAtRaise > UsDebtLimit.Pl119LimitUsd && !armed,
                            F("(c) the measures run {0} days to {1}, and that day the AI-governed USA raises the limit by act (R-US21 (vi)) by {2} to {3} - live: raised on {4}, {5} raise(s), the limit {6}; no cut was ever armed",
                                spanFrom, Day(walkRaise), "the excess and " + Usd(UsDebtLimit.RaiseUsd) + " (K77)", Usd(dslAtRaise + UsDebtLimit.RaiseUsd), Day(liveRaise), au.DebtLimitRaises, Usd(au.DebtLimitUsd)));
                        Check(!ministryWrote && boundaries > 0, F("(c) R-US21 (v): at each of the {0} boundaries crossed the AI ministry writes nothing for the USA (no line, no rate) - the dollar limit is its rule", boundaries));
                        int seedDays = walkReach == DateTime.MinValue ? -1 : (walkReach - opens).Days;
                        DateTime vintageDay = DateTime.ParseExact(data["vintage.date"], "yyyy-MM-dd", CultureInfo.InvariantCulture);
                        sb.Append(F("    read      THE CRS WINDOW (IF10292 v22, \"early to mid-2027\", DECLARED as {0} to {1}): the game reaches the limit on {2} - {3}; the measures run out, and the AI raises it, on {4} - {5}. The game's calendar opens on Sweden's start ({6}) on a seed of {7}'s state: carried from the seed's own month-end the same {8} days land on {9} - {10}.\n",
                            Iso(CrsOpens), Iso(CrsCloses), Day(walkReach), Against(walkReach), Day(walkRaise), Against(walkRaise), Iso(opens), Iso(vintageDay), seedDays, Day(vintageDay.AddDays(Math.Max(0, seedDays))), Against(vintageDay.AddDays(Math.Max(0, seedDays)))));

                        // ============================================================ (d) a US game: the reinstatement on 2 January 2025
                        DateTime usStart = WorldClock.StartDate(CountryId.USA);
                        (SimulationManager s, World w, Country u) = UsCongressDiagnostic.OpenUsa(hosts, usStart, "REP");
                        _record119In = w;   // K215: the record's 119th Congress planted from 3 January 2025 (StepOne)
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
                        DateTime bindsOn = UsDebtLimit.BindsOn(u), tabled = DateTime.MinValue, proposedOn = DateTime.MinValue;
                        double onTheFifth = double.NaN;
                        bool armedBefore = false, tabledByItself = false, proposed = false;
                        string proposeWhy = null;
                        while (s.CurrentDate < bindsOn && u.DebtLimitRaises == 0)
                        {
                            StepOne(s, w);
                            // PS-6 K23: the player-president's government tables nothing by itself - he proposes, here a bill's course before the deadline (the day the
                            // government's own tabling used to come), at the standard size; K25: it is voted on the day the measures run out
                            if (proposedOn == DateTime.MinValue) { tabledByItself |= s.GetPendingDebtLimitBill(CountryId.USA) != null; }
                            if (proposedOn == DateTime.MinValue && s.CurrentDate == bindsOn.AddDays(-ParliamentSystem.BillDurationDays))
                            {
                                proposed = s.ProposeDebtLimitRaise(CountryId.USA, UsDebtLimit.RaiseUsd, DateTime.MinValue, out proposeWhy);
                                proposedOn = s.CurrentDate;
                            }
                            if (tabled == DateTime.MinValue && s.GetPendingDebtLimitBill(CountryId.USA) != null) { tabled = s.CurrentDate; }
                            if (s.CurrentDate == UsDebtLimit.Pl119From.AddDays(1)) { onTheFifth = u.DebtLimitUsd; }
                            armedBefore |= u.DebtLimitCutFlowDay != DateTime.MinValue;
                        }
                        Dictionary<string, int> house119 = u.ParliamentSeats, senate119 = u.SecondChamberSeats;
                        Check(Seats(house119, "REP") > Seats(house119, "DEM") && Seats(senate119, "REP") > Seats(senate119, "DEM"),
                            F("(d) K215: the record's 119th Congress planted from {0}: the House REP {1}, DEM {2}; the Senate REP {3}, DEM {4} - the player's party holds both, so no hostile majority (K78) reads the count",
                                Iso(Record119From), Seats(house119, "REP"), Seats(house119, "DEM"), Seats(senate119, "REP"), Seats(senate119, "DEM")));
                        Check(onTheFifth == reinstated, F("(d) R-US21 (ii)(a): on {0} the limit is still the game's {1} - P.L. 119-21's increase of 4 July 2025 is the record's, and later raises are the game's Congress's acts", Iso(UsDebtLimit.Pl119From.AddDays(1)), Usd(onTheFifth)));
                        DivisionRecord senate = u.Divisions.Entries.LastOrDefault(d => d.Title != null && d.Title.StartsWith("Reconciliation (the debt limit)", StringComparison.Ordinal) && d.Chamber == Congress.Senate);
                        DivisionRecord house = u.Divisions.Entries.LastOrDefault(d => d.Title != null && d.Title.StartsWith("Reconciliation (the debt limit)", StringComparison.Ordinal) && d.Chamber == Congress.House);
                        Check(proposed && !tabledByItself && proposedOn == tabled,
                            F("(d) PS-6 K23: the player-president's government tabled nothing by itself through {0} (AI governments keep the automatic tabling); the President proposed the raise on {1} ({2})",
                                Day(proposedOn), Day(proposedOn), proposed ? "tabled" : "REFUSED: " + proposeWhy));
                        Check(tabled == bindsOn.AddDays(-ParliamentSystem.BillDurationDays) && u.DebtLimitRaises == 1 && u.DebtLimitSince == bindsOn && Math.Abs(u.DebtLimitUsd - UsDebtLimit.SubjectToLimitUsd(u) - UsDebtLimit.RaiseUsd) < 1d && u.DebtLimitUsd > reinstated + UsDebtLimit.RaiseUsd && !armedBefore
                              && senate != null && house != null && senate.Passed && house.Passed && senate.Reconciliation && !senate.Cloture && s.GetPendingDebtLimitBill(CountryId.USA) == null && senate.Title == RaiseTitle && house.Title == RaiseTitle,
                            F("(d) the raise tabled {0} by the President, a bill's course ({1} days) before the measures run out; voted at the deadline (K25) on {2} - the House {3}, the Senate {4} by reconciliation (no cloture), titled \"{6}\" in both (no world's own figure - FORK-2's identity, the review's M2) - and law that day: the limit {5}; no cut armed",
                                Day(tabled), ParliamentSystem.BillDurationDays, Day(u.DebtLimitSince), house?.Passed == true ? "passed" : "FAILED", senate?.Passed == true ? "passed" : "FAILED", Usd(u.DebtLimitUsd), senate?.Title));

                        // ============================================================ (e) the cut, planted
                        float cutBefore = u.DebtLimitCutBillions;
                        float debtA = u.State.GovernmentDebt;
                        StepOne(s, w);
                        float unarmedDay = u.State.GovernmentDebt - debtA;
                        u.DebtLimitUsd = UsDebtLimit.SubjectToLimitUsd(u) - 1e9;
                        u.DebtLimitReachedOn = s.CurrentDate.AddDays(-UsDebtLimit.MeasuresSpanDays);
                        StepOne(s, w);   // the bookkeeping sees the limit binding; the day tick arms the next day's flows - and tables nothing for a player-president (K23)
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
                        Check(armedFor == s.CurrentDate && pendingRaise == null && unarmedDay > 1f && Mathf.Abs(armedDay) < 0.05f * unarmedDay && cut > 0.5f * unarmedDay && cut < 2f * unarmedDay,
                            F("(e) THE CUT (planted: the limit {0} under the debt, the measures run out): the day tick arms {1} and tables nothing - the President has proposed none (K23); that day the debt moves {2:0.000} bn where the day before, unarmed, it moved {3:0.000} bn - the day's non-interest outlays cut by {4:0.000} bn to what revenue leaves after interest (R-US21 (iv))",
                                Usd(1e9), Day(armedFor), armedDay, unarmedDay, cut));
                        List<string> lines = FiscalRules.DebtLimitLines(u, s.CurrentDate, s.GetPendingDebtLimitBill(CountryId.USA));
                        string note = FiscalRules.NationalNote(u, s.CurrentDate);
                        // PS-6 US-35 (board 30c): the slip in sentence case - binding, never "held at the limit by the measures" nor "borrowed through them"; the tile's chip its state
                        string chip = FiscalRules.DebtLimitChip(u, s.CurrentDate, s.GetPendingDebtLimitBill(CountryId.USA), out FiscalRules.DebtLimitInk chipInk, out string chipSub);
                        Check(!lines.Any(l => l.Contains("borrowed through them") || l.Contains("Extraordinary measures hold the debt at the limit")) && lines.Any(l => l.StartsWith("Debt subject to it ", StringComparison.Ordinal) && l.EndsWith(" above.", StringComparison.Ordinal))
                              && lines.Any(l => l.StartsWith("The extraordinary measures ran out on ", StringComparison.Ordinal)) && lines.Any(l => l.Contains("cut so far")) && lines.Any(l => l.StartsWith("No raise is before Congress. Propose one", StringComparison.Ordinal))
                              && lines.Any(l => l.StartsWith("Limit ", StringComparison.Ordinal)) && note.Contains("cut to revenue")
                              && chip == "LIMIT BINDS · CUT IN FORCE" && chipInk == FiscalRules.DebtLimitInk.Cut && chipSub == null
                              && FiscalRules.NationalNote(ws.GetCountry(CountryId.Germany), s.CurrentDate) == FiscalRules.NationalNote(CountryId.Germany),
                            F("(e) the slip says so - \"{0}\"; the tile's chip \"{1}\" ({2}); the national note: \"{3}\"; every other country's note unchanged", string.Join(" | ", lines), chip, chipInk, note));

                        // ============================================================ (e3) PS-6 K26: the budget preview shows the cut while the limit binds
                        PolicyPreview cutPreview = s.PreviewTurn(CountryId.USA, PolicyDecision.None());
                        DateTime armedNow = u.DebtLimitCutFlowDay;
                        u.DebtLimitCutFlowDay = DateTime.MinValue;   // the planted tooth: the same day with nothing armed
                        PolicyPreview plainPreview = s.PreviewTurn(CountryId.USA, PolicyDecision.None());
                        u.DebtLimitCutFlowDay = armedNow;
                        Check(armedNow != DateTime.MinValue && cutPreview.DebtLimitCut > 0f && plainPreview.DebtLimitCut == 0f && cutPreview.SpendingEstimate < plainPreview.SpendingEstimate
                              && Mathf.Abs((plainPreview.SpendingEstimate - cutPreview.SpendingEstimate) - cutPreview.DebtLimitCut) < 1e-3f * Mathf.Max(1f, cutPreview.DebtLimitCut),
                            F("(e3) PS-6 K26: while the limit binds the preview's year is cut - {0:0.000} bn of spending other than interest, its spending {1:0.000} bn against {2:0.000} bn with nothing armed (the planted tooth), the difference the cut",
                                cutPreview.DebtLimitCut, cutPreview.SpendingEstimate, plainPreview.SpendingEstimate));

                        // ============================================================ (f) the save
                        string json = SaveGameService.Serialize(SaveGameService.CreateSaveGame(s, w, CountryId.USA, null));
                        SaveGame saved = SaveGameService.Deserialize(json);
                        Country su = saved.World.GetCountry(CountryId.USA);
                        Check(saved.SaveVersion == SaveGameService.CurrentSaveVersion && saved.SaveVersion >= 53 && su.DebtLimitUsd == u.DebtLimitUsd && su.DebtLimitSince == u.DebtLimitSince && su.DebtLimitReachedOn == u.DebtLimitReachedOn
                              && su.DebtLimitCutFlowDay == u.DebtLimitCutFlowDay && su.DebtLimitCutBillions == u.DebtLimitCutBillions && su.DebtLimitRaises == u.DebtLimitRaises
                              && su.DebtLimitWithheldBy == u.DebtLimitWithheldBy && su.ContinuingResolutionWithheldOn == u.ContinuingResolutionWithheldOn
                              && saved.Sim?.PendingDebtLimitBills != null && !saved.Sim.PendingDebtLimitBills.ContainsKey(CountryId.USA),
                            F("(f) a save cut on {0} carries format {1} and the limit's fields, nothing before Congress (the President has proposed none); a pending proposal's own fields in (j)", Day(s.CurrentDate), saved.SaveVersion));
                        string older = json.Replace("\"SaveVersion\": " + SaveGameService.CurrentSaveVersion.ToString(CultureInfo.InvariantCulture), "\"SaveVersion\": " + (SaveGameService.CurrentSaveVersion - 1).ToString(CultureInfo.InvariantCulture));
                        bool refused = false; string why = null;
                        try { SaveGameService.Deserialize(older); } catch (SaveLoadException e) { refused = true; why = e.Message; }
                        Check(older != json && refused, F("(f) a save of format {0} is refused with the plain message (ruling A): \"{1}\"", SaveGameService.CurrentSaveVersion - 1, why));

                        // ============================================================ (e2) the back pay carried through the raise, on the day path (the second review's L4)
                        string firstTitle = senate?.Title;
                        double limitBefore = u.DebtLimitUsd;
                        int raisesBefore = u.DebtLimitRaises;
                        DateTime proposedLate = s.CurrentDate;
                        bool waitsBefore = s.DebtLimitAwaitsPresident(CountryId.USA);   // PS-6 K85: the binding limit, nothing proposed - the played clock holds on it
                        bool proposedPast = s.ProposeDebtLimitRaise(CountryId.USA, UsDebtLimit.RaiseUsd, DateTime.MinValue, out string latePropose);
                        Check(waitsBefore && !s.DebtLimitAwaitsPresident(CountryId.USA),
                            F("(e2) PS-6 K85: the binding limit with nothing proposed waits on the President ({0}) - the clock holds on it - and waits no more once the raise is proposed and law", waitsBefore ? "it waits" : "NO WAIT"));   // K43: past the deadline it is voted the day it is tabled
                        Check(proposedPast && u.DebtLimitRaises == raisesBefore + 1 && u.DebtLimitSince == proposedLate && s.GetPendingDebtLimitBill(CountryId.USA) == null,
                            F("(e2) PS-6 K43: proposed on {0}, past the deadline, the raise is voted that day in both chambers and signed - law on {1} ({2})", Day(proposedLate), Day(u.DebtLimitSince), proposedPast ? "tabled" : "REFUSED: " + latePropose));
                        bool owedOnRaiseDay = u.LapseDeferredOutlays == BackPay && u.DebtLimitCutFlowDay == DateTime.MinValue;
                        float debtC = u.State.GovernmentDebt;
                        StepOne(s, w);   // the first day the cut is not armed: the back pay is paid in full
                        float paidDay = u.State.GovernmentDebt - debtC;
                        DivisionRecord secondSenate = u.Divisions.Entries.LastOrDefault(d => d.Title != null && d.Title.StartsWith("Reconciliation (the debt limit)", StringComparison.Ordinal) && d.Chamber == Congress.Senate);
                        Check(u.DebtLimitRaises == raisesBefore + 1 && owedOnRaiseDay && u.LapseDeferredOutlays == 0f && paidDay - unarmedDay > 0.5f * BackPay && paidDay - unarmedDay < 1.5f * BackPay,
                            F("(e2) the planted binding's raise becomes law on {0} by Congress's act, the day the President proposed it; the back pay ({1} bn) is still owed that day, and the first unarmed day pays it in full - the debt moves {2:0.000} bn against an unarmed day's {3:0.000} bn", Day(u.DebtLimitSince), BackPay, paidDay, unarmedDay));
                        Check(secondSenate != null && !ReferenceEquals(secondSenate, senate) && secondSenate.Title == firstTitle && firstTitle == RaiseTitle && u.DebtLimitUsd != limitBefore && limitBefore != reinstated,
                            F("(e2) the two raises' divisions, voted on limits {0} and {1}, carry one title, the exact \"{2}\" - no world's figure (FORK-2's identity)", Usd(reinstated), Usd(limitBefore), RaiseTitle));

                        // ============================================================ (h) PS-6 K22 and K43: a withheld raise falls, the cause named; proposed again, it is law
                        StepOne(s, w);
                        u.DebtLimitUsd = UsDebtLimit.SubjectToLimitUsd(u) - 1e9;
                        u.DebtLimitReachedOn = s.CurrentDate.AddDays(-UsDebtLimit.MeasuresSpanDays);
                        StepOne(s, w);   // binding again: the flows armed, nothing tabled
                        bool refusedOwn = !UsDebtLimit.MayWithhold(u, true, s.CurrentDate, out string ownWhy) && ownWhy != null && ownWhy.Contains("YOUR OWN GOVERNMENT");
                        bool verbRefused = !s.WithholdDebtLimitRaise(CountryId.USA, true, out string verbWhy);
                        u.DebtLimitWithheldBy = "REP";   // planted: the party holding the Senate withholds (a state the verb refuses the president's party - the tooth that a withhold reaches the vote)
                        int raisesH = u.DebtLimitRaises;
                        bool tabledH = s.ProposeDebtLimitRaise(CountryId.USA, UsDebtLimit.RaiseUsd, DateTime.MinValue, out string whyH);
                        DivisionRecord fell = u.Divisions.Entries.LastOrDefault(d => d.Title == RaiseTitle && d.Date == s.CurrentDate && !d.Passed);   // the chamber the withholder holds
                        List<string> withheldLines = FiscalRules.DebtLimitLines(u, s.CurrentDate, s.GetPendingDebtLimitBill(CountryId.USA));
                        string withheldChip = FiscalRules.DebtLimitChip(u, s.CurrentDate, s.GetPendingDebtLimitBill(CountryId.USA), out FiscalRules.DebtLimitInk withheldInk, out string _);
                        Check(refusedOwn && verbRefused && tabledH && u.DebtLimitRaises == raisesH && s.GetPendingDebtLimitBill(CountryId.USA) == null && UsDebtLimit.Binding(u, s.CurrentDate)
                              && fell != null && fell.Date == s.CurrentDate && !fell.Passed && withheldLines.Any(l => l.StartsWith("The raise is withheld by REP: ", StringComparison.Ordinal))
                              && withheldLines.Any(l => l.StartsWith("No raise is before Congress. Propose one", StringComparison.Ordinal)) && withheldChip == "LIMIT BINDS · CUT IN FORCE" && withheldInk == FiscalRules.DebtLimitInk.Cut,
                            F("(h) PS-6 K22: the president's own party may not withhold (\"{0}\"; the verb: \"{1}\"); planted, a withholder holding a chamber stops the raise the President proposed past the deadline - voted that day, it {2}; the limit binds, the chip \"{3}\", the slip naming the cause: \"{4}\" ({5})",
                                ownWhy, verbWhy, fell == null ? "PASSED OR NOT VOTED" : "fails in the " + (fell.Chamber == Congress.Senate ? "Senate" : "House"), withheldChip, string.Join(" | ", withheldLines.Where(l => l.Contains("withheld") || l.StartsWith("No raise", StringComparison.Ordinal))), tabledH ? "tabled" : "REFUSED: " + whyH));
                        StepOne(s, w);   // the next tick: the withholding party holds the presidency - its withhold lapses (the raise is its own government's)
                        bool withholdLapsed = u.DebtLimitWithheldBy == null;
                        bool again = s.ProposeDebtLimitRaise(CountryId.USA, UsDebtLimit.RaiseUsd, DateTime.MinValue, out string againWhy);
                        Check(withholdLapsed && again && u.DebtLimitRaises == raisesH + 1 && u.DebtLimitSince == s.CurrentDate && u.DebtLimitWithheldBy == null && u.DebtLimitCutFlowDay == DateTime.MinValue,
                            F("(h) PS-6 K43: after the fallen raise the withhold lapsed with the presidency on the next tick; proposed again on {0}, the raise is voted that day and law ({1})", Day(s.CurrentDate), again ? "tabled" : "REFUSED: " + againWhy));

                        // ============================================================ (j) PS-6 K23/K25: a suspension proposed while the measures run - saved, voted at the deadline; (i) K22: law, then the limit returns
                        StepOne(s, w);
                        u.DebtLimitUsd = UsDebtLimit.SubjectToLimitUsd(u) - 1e9;
                        u.DebtLimitReachedOn = s.CurrentDate.AddDays(-10);   // planted: the measures running, ten days in
                        StepOne(s, w);
                        DateTime deadline = UsDebtLimit.BindsOn(u), through = deadline.AddDays(30);
                        bool earlyRefused = !s.ProposeDebtLimitRaise(CountryId.USA, 0d, deadline, out string earlyWhy) && s.GetPendingDebtLimitBill(CountryId.USA) == null
                                            && UsDebtLimit.VoteDayOf(u, s.CurrentDate) == deadline;   // the review's M2: a last day on the vote's own day is refused
                        Check(earlyRefused, F("(j) the review's M2: a suspension through {0}, the day it would be voted, is refused (\"{1}\") - its last day must come after its vote", Day(deadline), earlyWhy));
                        bool tabledJ = s.ProposeDebtLimitRaise(CountryId.USA, 0d, through, out string whyJ);
                        BudgetBill heldJ = s.GetPendingDebtLimitBill(CountryId.USA);
                        string jsonJ = SaveGameService.Serialize(SaveGameService.CreateSaveGame(s, w, CountryId.USA, null));
                        SaveGame savedJ = SaveGameService.Deserialize(jsonJ);
                        bool carriedJ = heldJ != null && savedJ.Sim?.PendingDebtLimitBills != null && savedJ.Sim.PendingDebtLimitBills.TryGetValue(CountryId.USA, out BudgetBill savedHeld)
                                        && savedHeld.DebtLimitSuspendThrough == through && savedHeld.DebtLimitRaiseUsd == 0d && savedHeld.DaysRemaining == heldJ.DaysRemaining;
                        string chipJ = FiscalRules.DebtLimitChip(u, s.CurrentDate, heldJ, out FiscalRules.DebtLimitInk inkJ, out string _);
                        List<string> linesJ = FiscalRules.DebtLimitLines(u, s.CurrentDate, heldJ);
                        Check(tabledJ && heldJ != null && heldJ.DaysRemaining == (deadline - s.CurrentDate).Days && carriedJ && chipJ == "RAISE BEFORE CONGRESS" && inkJ == FiscalRules.DebtLimitInk.Clock
                              && linesJ.Any(l => l.StartsWith("A suspension through ", StringComparison.Ordinal) && l.Contains(" is before Congress, voted on " + deadline.ToString("d MMM yyyy", CultureInfo.InvariantCulture))),
                            F("(j) PS-6 K23/K25: the President proposes a suspension through {0} on {1}, the measures running - voted on {2}, the day they run out ({3} day(s)); a save cut that day carries it (format {4}: its last day, its course); the chip \"{5}\" ({6})",
                                Day(through), Day(s.CurrentDate), Day(deadline), heldJ?.DaysRemaining ?? -1, savedJ.SaveVersion, chipJ, tabledJ ? "tabled" : "REFUSED: " + whyJ));
                        int guardJ = 0;
                        while (s.CurrentDate < deadline && guardJ++ < 400) { StepOne(s, w); }
                        DivisionRecord suspended = u.Divisions.Entries.LastOrDefault(d => d.Title != null && d.Title.StartsWith("Reconciliation (the debt limit): the limit suspended through ", StringComparison.Ordinal) && d.Chamber == Congress.Senate);
                        string chipS = FiscalRules.DebtLimitChip(u, s.CurrentDate, s.GetPendingDebtLimitBill(CountryId.USA), out FiscalRules.DebtLimitInk inkS, out string _);
                        Check(UsDebtLimit.Suspended(u, s.CurrentDate) && u.DebtLimitSuspendedThrough == through && u.DebtLimitUsd == 0d && u.DebtLimitReachedOn == DateTime.MinValue && u.DebtLimitCutFlowDay == DateTime.MinValue
                              && suspended != null && suspended.Passed && suspended.Date == deadline && chipS == "SUSPENDED TO " + through.ToString("d MMM yyyy", CultureInfo.InvariantCulture).ToUpperInvariant() && inkS == FiscalRules.DebtLimitInk.SetAside,
                            F("(i) PS-6 K22: the suspension voted on {0} by reconciliation and law that day - \"{1}\"; no limit applies through {2}, the chip \"{3}\" ({4})", Day(deadline), suspended?.Title ?? "NOT VOTED", Day(through), chipS, inkS));
                        double dslThrough = double.NaN;
                        int guardI = 0;
                        while (s.CurrentDate < through.AddDays(1) && guardI++ < 400)
                        {
                            bool boundaryI = s.AdvanceDay();
                            if (s.CurrentDate == through) { dslThrough = UsDebtLimit.SubjectToLimitUsd(u); }   // the day's close, as the bookkeeping read it
                            s.AdvanceCountryDayTick(CountryId.USA);
                            if (boundaryI) { Turn(s, w); }
                        }
                        Check(!UsDebtLimit.Suspended(u, s.CurrentDate) && u.DebtLimitSince == through.AddDays(1) && Math.Abs(u.DebtLimitUsd - dslThrough) < 1d,
                            F("(i) the day after the suspension, {0}, the limit returns at the debt subject to it at the close of {1}, {2} (P.L. 118-5 s401(b)'s form) - the limit {3}", Day(through.AddDays(1)), Day(through), Usd(dslThrough), Usd(u.DebtLimitUsd)));

                        // ============================================================ (m) PS-6 K75: the player-president's own party may withhold the continuing resolution
                        DateTime horizonWas = u.AppropriatedThrough;
                        string crWithheldWas = u.ContinuingResolutionWithheldBy;
                        DateTime crOnWas = u.ContinuingResolutionWithheldOn;
                        bool crInOfficeWas = u.ContinuingResolutionWithheldInOffice;
                        try
                        {
                            u.AppropriatedThrough = UsBudgetProcedure.FiscalYearEnd(s.CurrentDate);   // planted: no appropriation for the coming year law, so the next resolution is due
                            bool mayOwn = UsBudgetProcedure.MayWithhold(u, null, s.CurrentDate, out string mayWhy);
                            u.ContinuingResolutionWithheldBy = "REP";
                            u.ContinuingResolutionWithheldOn = s.CurrentDate;
                            u.ContinuingResolutionWithheldInOffice = true;   // given in office, as the verb records it
                            bool stands = !UsBudgetProcedure.WithholdLapsesWithPresidency(u);
                            BillConcern cr = ParliamentSystem.GetBudgetBillConcern(u, new BudgetBill { ContinuingResolution = true });
                            Congress.Verdict crVerdict = Congress.Project(u, cr, s.CurrentDate);
                            UsBudgetProcedure.NoteWithholderInOffice(u);
                            bool reElected = !UsBudgetProcedure.WithholdLapsesWithPresidency(u) && u.ContinuingResolutionWithheldInOffice;   // the review's L1: the party never left office - a second oath keeps it
                            u.ContinuingResolutionWithheldInOffice = false;   // planted: withheld out of office (or left it since)
                            bool lapsesWithChange = UsBudgetProcedure.WithholdLapsesWithPresidency(u) && reElected;
                            Check(mayOwn && stands && cr.WithheldBy == "REP" && crVerdict != null && !crVerdict.Passed && lapsesWithChange,
                                F("(m) PS-6 K75: the President's own party (REP) may withhold its government's continuing resolution ({0}); withheld while it holds the presidency the withhold stands, and the resolution is stopped ({1}); one withheld before the party took the presidency lapses with it as before",
                                    mayOwn ? "offered" : "REFUSED: " + mayWhy, crVerdict == null ? "no verdict" : crVerdict.Passed ? "PASSES" : "falls"));
                        }
                        finally { u.AppropriatedThrough = horizonWas; u.ContinuingResolutionWithheldBy = crWithheldWas; u.ContinuingResolutionWithheldOn = crOnWas; u.ContinuingResolutionWithheldInOffice = crInOfficeWas; }

                        // ============================================================ (n) PS-6 K78: the AI's hostile majority withholds from the reach and releases at the deadline
                        Dictionary<string, int> houseWas = new Dictionary<string, int>(u.ParliamentSeats);
                        string limitWithheldWas = u.DebtLimitWithheldBy;
                        try
                        {
                            u.DebtLimitWithheldBy = null;
                            u.ParliamentSeats = new Dictionary<string, int> { { "DEM", 220 }, { "REP", 215 } };   // planted: a DEM House against the REP President (the player's party)
                            StepPlanted(s, w, u);
                            u.DebtLimitUsd = UsDebtLimit.SubjectToLimitUsd(u) - 1e9;
                            u.DebtLimitReachedOn = s.CurrentDate.AddDays(-10);   // planted: the measures running, ten days in
                            StepPlanted(s, w, u);   // the tick: the AI's hostile majority withholds
                            bool aiWithheld = u.DebtLimitWithheldBy == "DEM" && UsDebtLimit.AiWithholder(u, s.CurrentDate) == "DEM";
                            bool playerCannotRelease = !s.WithholdDebtLimitRaise(CountryId.USA, false, out string releaseWhy) && u.DebtLimitWithheldBy == "DEM";
                            List<string> aiLines = FiscalRules.DebtLimitLines(u, s.CurrentDate, s.GetPendingDebtLimitBill(CountryId.USA));
                            bool slipSays = aiLines.Any(l => l.StartsWith("The raise is withheld by DEM: ", StringComparison.Ordinal)) && aiLines.Any(l => l.StartsWith("It is released on ", StringComparison.Ordinal));
                            DateTime withheldOn = s.CurrentDate;
                            StepPlanted(s, w, u);
                            bool stillWithheld = u.DebtLimitWithheldBy == "DEM";
                            // K89: the withhold holds through the deadline - planted: tomorrow the measures run out
                            u.DebtLimitReachedOn = s.CurrentDate.AddDays(1 - UsDebtLimit.MeasuresSpanDays);
                            StepPlanted(s, w, u);   // the deadline's tick: still withheld, the cut armed
                            DateTime deadlineN = s.CurrentDate;
                            int raisesN = u.DebtLimitRaises;
                            bool proposedN = s.ProposeDebtLimitRaise(CountryId.USA, UsDebtLimit.RaiseUsd, DateTime.MinValue, out string proposeNWhy);   // voted at once, withheld: it falls
                            bool fellN = proposedN && u.DebtLimitRaises == raisesN && UsDebtLimit.Binding(u, s.CurrentDate) && u.DebtLimitWithheldBy == "DEM";
                            bool holdsAgain = s.DebtLimitAwaitsPresident(CountryId.USA);   // K85: the fallen raise - the hold returns
                            // K90: the President lets it bind - the hold released, and it stays released the next day
                            bool letBind = s.LetDebtLimitBind(CountryId.USA, out string letWhy) && !s.DebtLimitAwaitsPresident(CountryId.USA);
                            float cutBeforeN = u.DebtLimitCutBillions;
                            StepPlanted(s, w, u);   // the day after the deadline: still withheld (K89), the cut bites, no hold (K90)
                            bool bitesN = u.DebtLimitWithheldBy == "DEM" && u.DebtLimitCutBillions > cutBeforeN && !s.DebtLimitAwaitsPresident(CountryId.USA) && u.DebtLimitLetBind;
                            StepPlanted(s, w, u);   // the release day: AiReleaseDay
                            DateTime releasedOn = s.CurrentDate;
                            bool released = u.DebtLimitWithheldBy == null && releasedOn == UsDebtLimit.AiReleaseDay(u) && releasedOn == deadlineN.AddDays(UsDebtLimit.AiHoldDaysPastDeadline)
                                            && !u.DebtLimitLetBind && s.DebtLimitAwaitsPresident(CountryId.USA);   // K90: the release is a change - the President is asked again
                            u.DebtLimitWithheldBy = "DEM";   // planted again, a withholder now holding neither chamber
                            u.ParliamentSeats = new Dictionary<string, int> { { "DEM", 215 }, { "REP", 220 } };   // planted: DEM loses the House (the Senate REP's)
                            bool outlived = UsDebtLimit.WithholdOutlived(u, s.CurrentDate) && UsDebtLimit.AiWithholder(u, s.CurrentDate) == null;
                            u.ParliamentSeats = new Dictionary<string, int> { { "DEM", 220 }, { "REP", 215 } };
                            outlived &= !UsDebtLimit.WithholdOutlived(u, s.CurrentDate);   // the tooth: holding the House, it bites
                            u.DebtLimitWithheldBy = null;
                            Check(outlived, "(n) the review's L2: a withhold whose party holds neither chamber is read as outlived (the tick then lapses it)");
                            int raisesR = u.DebtLimitRaises;
                            bool passes = s.ProposeDebtLimitRaise(CountryId.USA, UsDebtLimit.RaiseUsd, DateTime.MinValue, out string passWhy) && u.DebtLimitRaises == raisesR + 1;
                            Check(aiWithheld && playerCannotRelease && slipSays && stillWithheld,
                                F("(n) PS-6 K78: a DEM House against the REP President (planted) withholds the raise by the AI's rule on {0}, the measures running ({1}; the player may not release it: \"{2}\"; the slip: \"{3}\"); it stands the next day ({4})",
                                    Day(withheldOn), aiWithheld ? "withheld" : "NEVER WITHHELD", releaseWhy, string.Join(" | ", aiLines.Where(l => l.Contains("withheld") || l.StartsWith("It is released", StringComparison.Ordinal))),
                                    stillWithheld ? "withheld" : "RELEASED EARLY"));
                            Check(fellN && holdsAgain && bitesN && released && passes,
                                F("(n) PS-6 K89: withheld through the deadline {0} - the President's raise voted that day falls ({1}), the hold returns ({2}); the day after, still withheld, the cut bites ({3}); released on {4}, {5} day(s) past the deadline ({6}); the raise proposed that day is law ({7})",
                                    Day(deadlineN), fellN ? "fell" : "DID NOT FALL - " + proposeNWhy, holdsAgain ? "it holds" : "NO HOLD", bitesN ? "it bites" : "NO BITE", Day(releasedOn), UsDebtLimit.AiHoldDaysPastDeadline,
                                    released ? "released" : "NEVER RELEASED OR EARLY", passes ? "passed" : "FAILED: " + passWhy));
                            Check(letBind && bitesN && released,
                                F("(n) PS-6 K90: the President answers the hold with LET IT BIND ({0}) - no hold the next day while the cut bites; the AI's release is a change, and the hold returns that day", letBind ? "the hold released" : "REFUSED: " + letWhy));
                        }
                        finally { u.ParliamentSeats = houseWas; u.DebtLimitWithheldBy = limitWithheldWas; }
                    }
                    finally { EnergyMarket.ResetTurnState(); _record119In = null; }
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
                    Check(earlyHeld && congressHeld && t.DebtLimitRaises == 1 && Math.Abs(t.DebtLimitUsd - (UsDebtLimit.SubjectToLimitUsd(t) + UsDebtLimit.RaiseUsd)) < 1d && t.DebtLimitReachedOn == DateTime.MinValue,
                        "(g) the AI's raise is not taken the day before the measures run out, nor where a Congress of the game votes; it is taken on the day, and closes the episode");
                    // PS-6 K77: the excess plus the size - an AI raise by act where the debt stands $6.0 tn over the limit lands it $5.0 tn above the day's debt, where
                    // K21's floor landed it on the debt and the old fixed raise left it $1.0 tn over (the planted tooth's arithmetic); RaiseOf adds every size to the excess
                    var floor = new Country(CountryId.USA, "floor", new EconomyState(gdp: 30000f, inflation: 2f, unemployment: 4f, approvalRating: 50f, budget: 0f, governmentDebt: 40000f), null, 0f);
                    floor.DebtLimitSince = new DateTime(2029, 1, 1);
                    double dslFloor = UsDebtLimit.SubjectToLimitUsd(floor);
                    floor.DebtLimitUsd = dslFloor - 6e12;
                    floor.DebtLimitReachedOn = d0.AddDays(-UsDebtLimit.MeasuresSpanDays);
                    double excessFloor = UsDebtLimit.ExcessUsd(floor);
                    UsDebtLimit.Track(floor, d0, d0, false);
                    Check(Math.Abs(excessFloor - 6e12) < 1d && floor.DebtLimitRaises == 1 && Math.Abs(floor.DebtLimitUsd - (dslFloor + UsDebtLimit.RaiseUsd)) < 1d && dslFloor - 6e12 + UsDebtLimit.RaiseUsd < dslFloor
                          && UsDebtLimit.RaiseOf(floor, 0d) == UsDebtLimit.ExcessUsd(floor) + UsDebtLimit.RaiseUsd && UsDebtLimit.RaiseOf(t, 0d) == UsDebtLimit.ExcessUsd(t) + UsDebtLimit.RaiseUsd
                          && UsDebtLimit.RaiseOf(t, 2.5e12) == UsDebtLimit.ExcessUsd(t) + 2.5e12,
                        F("(g) PS-6 K77: the debt {0} over the limit - the AI's raise by act is the excess and {1}, landing the limit {1} above the day's debt {2} (K21's floor landed it on the debt; the old fixed raise left {3} over); RaiseOf adds every size to the excess",
                            Usd(excessFloor), Usd(UsDebtLimit.RaiseUsd), Usd(dslFloor), Usd(6e12 - UsDebtLimit.RaiseUsd)));
                    // PS-6 K22: who may withhold - a party holding a chamber against the presidency; not one holding neither
                    var hostile = new Country(CountryId.USA, "hostile", new EconomyState(gdp: 30000f, inflation: 2f, unemployment: 4f, approvalRating: 50f, budget: 0f, governmentDebt: 40000f), null, 0f);
                    hostile.DebtLimitSince = new DateTime(2029, 1, 1); hostile.DebtLimitUsd = 1e12; hostile.DebtLimitReachedOn = d0.AddDays(-5);
                    hostile.PlayerPartyAbbrev = "DEM";
                    hostile.ParliamentSeats = new Dictionary<string, int> { { "DEM", 220 }, { "REP", 215 } };
                    bool holdsHouse = UsDebtLimit.MayWithhold(hostile, true, d0, out string _);
                    hostile.ParliamentSeats = new Dictionary<string, int> { { "DEM", 215 }, { "REP", 220 } };
                    bool holdsNeither = !UsDebtLimit.MayWithhold(hostile, true, d0, out string neitherWhy);
                    hostile.DebtLimitReachedOn = DateTime.MinValue;
                    hostile.ParliamentSeats = new Dictionary<string, int> { { "DEM", 220 }, { "REP", 215 } };
                    bool belowRefused = !UsDebtLimit.MayWithhold(hostile, true, d0, out string belowWhy);
                    Check(holdsHouse && holdsNeither && belowRefused && !UsDebtLimit.MayWithhold(hostile, false, d0, out string _),
                        F("(g) PS-6 K22: a party holding the House against the presidency may withhold the raise; holding neither chamber it may not (\"{0}\"); below the limit nothing is due (\"{1}\"); nor where no Congress of the game votes", neitherWhy, belowWhy));
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
            PlantRecord119(s, w);   // K215: after the day's seating, before the player's day tick reads the chambers
            s.AdvanceCountryDayTick(CountryId.USA);
            if (boundary) { Turn(s, w); }
        }

        /// <summary>K215: the world whose Congress is the record's 119th from <see cref="Record119From"/> - the (d)-(n) world; null outside it.</summary>
        private static World _record119In;

        /// <summary>K215: the day the record's 119th Congress convened (3 January 2025, the Twentieth Amendment's noon).</summary>
        private static readonly DateTime Record119From = new DateTime(2025, 1, 3);

        /// <summary>K215: the record's House (the chamber of record the day's date seats, `PartySystems.InitialSeats` on `WorldClock.SeatedVintage`) and the
        /// record's Senate (`WorldClock.SecondChamberOfRecord`) laid over whatever the day loop seated - K78's StepPlanted pattern, every day.</summary>
        private static void PlantRecord119(SimulationManager s, World w)
        {
            if (_record119In == null || w != _record119In || s.CurrentDate < Record119From) { return; }
            Country u = w.GetCountry(CountryId.USA);
            u.ParliamentSeats = PartySystems.InitialSeats(CountryId.USA, WorldClock.SeatedVintage(CountryId.USA, s.CurrentDate));
            Dictionary<string, int> senate = WorldClock.SecondChamberOfRecord(CountryId.USA, s.CurrentDate);
            if (senate.Count > 0) { u.SecondChamberSeats = senate; }
        }

        private static int Seats(Dictionary<string, int> chamber, string party) => chamber != null && chamber.TryGetValue(party, out int n) ? n : 0;

        /// <summary>K78's (n) (the Unity run n907): one day in the controller's order with the planted House kept - the day loop re-seats the House in office every
        /// day (`SimulationManager.SeatTheRecordOnItsDate`), so the plant is laid again after the day and before the player's day tick reads it.</summary>
        private static void StepPlanted(SimulationManager s, World w, Country u)
        {
            var planted = new Dictionary<string, int>(u.ParliamentSeats);
            bool boundary = s.AdvanceDay();
            u.ParliamentSeats = new Dictionary<string, int>(planted);
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
