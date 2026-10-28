using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using PoliSim.Data;
using PoliSim.Elections;
using PoliSim.Simulation;
using PoliSim.UI;
using UnityEngine;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// PS-6 US-35 (D-US part two's boards 30a-30c, built from Code's PROPOSAL on Elias's go-ahead of 2026-10-23): <b>THE CONGRESS VIEW'S FIGURES ARE THE
    /// MODEL'S</b> - every number the Congress tab, the signing plate's path and the debt tile's chip draw is read from the chambers, the records and the
    /// dated arithmetic, never typed:
    /// <list type="bullet">
    /// <item>(a) THE TEN DAYS (Art. I §7 cl. 2): for every presentment day of a span of two years, `UsBillPath.TenDays` numbers exactly ten days 1 to 10, none
    /// a Sunday, every Sunday between them apart and unnumbered, its last day `PresidentialDocket.LastDay` and the law's day the day after
    /// (`PresidentialDocket.LawDay`); board 30b's case (presented 30 Apr 2027, law 13 May 2027, two Sundays, nine days left on 1 May) and the law of record
    /// that became law unsigned (P.L. 119-101: presented 29 Jun 2026, law 11 Jul 2026, `UsDocketDiagnostic` (f) holds it to its page) reproduced.</item>
    /// <item>(b) THE LINES AND THE CLASSES READ FROM THE CHAMBERS, on a US world at the start (the 118th of record) and on 3 Feb 2025 (the 119th): the House's
    /// majority and override lines are the chamber seated's (`Congress.Sworn`, the override `Congress.OverrideVote.Required`'s count), cloture
    /// `Congress.ClotureLine` of the senators sworn, the Senate's override the same two thirds; each line's holder the chamber's majority
    /// (`Congress.HouseMajority`, `Congress.SenateMajority`) and its seats; the three classes' seats, side by side, equal to the world's seated Senate and,
    /// class by class, to the record's seating of the day (`UsSenate.On`), the independents counted with their caucus and drawn apart; each class dated on
    /// the polling day of its cycle whose Congress has convened; divided government as the presidency's party against the two majorities.</item>
    /// <item>(c) A BILL'S PATH on planted records: a bill on the desk (two chambers passed, the President NOW, the override and the law conditional, the law's
    /// day given); a veto the origin sustained (the President and the override failed, the law dead); a veto overridden in both chambers (the law over the
    /// veto); a bill stopped in the second chamber (nothing after it reached); a Senate-first bill's stations in the Senate's order.</item>
    /// <item>(d) THE DEBT TILE'S CHIP on planted limits: below (no chip, the headroom line), at the limit with the measures' days, a raise before Congress, a raise
    /// on the desk, binding with the cut in force, binding with the raise on the desk, suspended.</item>
    /// <item>(e) FUNDING RESTORED, A SAVED DAY (K42, replacing the review's M4 memo): `UsBudgetProcedure.RestoredWithin` reads `Country.LapseRestoredOn` and its window,
    /// never for a country that never lapsed or has lapsed again; the Desk shows it on the first day drawn in the window. The review's M1-M3, L2 and L6 add cases to (a) and (c): the chamber that sustained a split
    /// override with its own votes, the yeas an uncontested passage holds, the Docket's countdown on the last day, an equal Senate held, a deadline's own desk line.</item>
    /// </list>
    /// The random streams are restored after, the epoch by its scope, and every host destroyed.
    /// </summary>
    public static class UsCongressViewDiagnostic
    {
        public static void Run()
        {
            CheckExit.ArmLogFold();
            var sb = new StringBuilder("=== UsCongressViewDiagnostic (PS-6 US-35): the Congress view, the bill's path and the debt chip read from the model ===\n");
            int failures = 0;
            void Check(bool ok, string what) { if (!ok) { failures++; } sb.Append(ok ? "    ok        " : "    FAIL      ").Append(what).Append('\n'); }
            var hosts = new List<GameObject>();
            int seedWas = SimulationRandom.MasterSeed;
            Dictionary<SimulationRandom.Stream, int> drawsWas = SimulationRandom.CaptureDrawCounts();
            try
            {
                // ============================================================ (a) the ten days
                int spanDays = 0, spanBad = 0;
                string firstBad = null;
                for (DateTime p = new DateTime(2026, 1, 1); p < new DateTime(2028, 1, 1); p = p.AddDays(1))
                {
                    spanDays++;
                    List<UsBillPath.Day> days = UsBillPath.TenDays(CountryId.USA, p);
                    List<UsBillPath.Day> numbered = days.Where(d => d.Number > 0).ToList();
                    bool ok = days.Count > 0 && days[0].Date == p && days[0].Number == 0
                              && numbered.Count == 10 && numbered.Select(d => d.Number).SequenceEqual(Enumerable.Range(1, 10)) && numbered.All(d => d.Date.DayOfWeek != DayOfWeek.Sunday)
                              && days.Where(d => d.Sunday).All(d => d.Date.DayOfWeek == DayOfWeek.Sunday && d.Number < 0)
                              && days.Skip(1).Select((d, k) => d.Date == p.AddDays(k + 1)).All(x => x)
                              && numbered.Last().Date == PresidentialDocket.LastDay(CountryId.USA, p) && days.Last().Date == PresidentialDocket.LastDay(CountryId.USA, p)
                              && PresidentialDocket.LawDay(CountryId.USA, p) == numbered.Last().Date.AddDays(1);
                    if (!ok) { spanBad++; if (firstBad == null) { firstBad = Day(p); } }
                }
                Check(spanBad == 0, F("(a) the ten days for every presentment day of 2026 and 2027 ({0} days): ten counted days numbered 1-10, no Sunday counted, each Sunday apart, the last the docket's last day and the law the day after - {1} wrong{2}",
                    spanDays, spanBad, firstBad != null ? " (first " + firstBad + ")" : string.Empty));
                var boardDay = new DateTime(2027, 4, 30);
                List<UsBillPath.Day> board = UsBillPath.TenDays(CountryId.USA, boardDay);
                Check(PresidentialDocket.LawDay(CountryId.USA, boardDay) == new DateTime(2027, 5, 13) && board.Count(d => d.Sunday) == 2 && UsBillPath.CountedDaysLeft(CountryId.USA, boardDay, boardDay.AddDays(1)) == 9,
                    F("(a) board 30b's case: presented {0}, the cells {1}, law {2}; on {3} {4} counted days are left (the Docket's LAW UNSIGNED IN 9 DAYS)", Day(boardDay),
                        string.Join(" ", board.Select(d => d.Sunday ? "Su" : d.Number.ToString(CultureInfo.InvariantCulture))), Day(PresidentialDocket.LawDay(CountryId.USA, boardDay)), Day(boardDay.AddDays(1)),
                        UsBillPath.CountedDaysLeft(CountryId.USA, boardDay, boardDay.AddDays(1))));
                // the review's M3: the Docket's countdown at its boundary - TOMORROW on every bill's last day, never "IN 0 DAYS"
                DateTime boardLast = PresidentialDocket.LastDay(CountryId.USA, boardDay);
                int lastDayWrong = 0;
                for (DateTime p = new DateTime(2026, 1, 1); p < new DateTime(2028, 1, 1); p = p.AddDays(1))
                {
                    DateTime last = PresidentialDocket.LastDay(CountryId.USA, p);
                    if (UsBillPath.UnsignedWords(CountryId.USA, p, last) != "LAW UNSIGNED TOMORROW" || UsBillPath.UnsignedWords(CountryId.USA, p, p).Contains(" 0 ")) { lastDayWrong++; }
                }
                Check(lastDayWrong == 0 && UsBillPath.UnsignedWords(CountryId.USA, boardDay, boardDay.AddDays(1)) == "LAW UNSIGNED IN 9 DAYS" && UsBillPath.UnsignedWords(CountryId.USA, boardDay, boardLast.AddDays(-1)) == "LAW UNSIGNED IN 1 DAY"
                      && UsBillPath.UnsignedWords(CountryId.USA, boardDay, boardLast) == "LAW UNSIGNED TOMORROW",
                    F("(a) the Docket's countdown: on {0} \"{1}\", on {2} \"{3}\", on the last day {4} \"{5}\" - TOMORROW on the last day of every presentment of 2026-2027 ({6} wrong)",
                        Day(boardDay.AddDays(1)), UsBillPath.UnsignedWords(CountryId.USA, boardDay, boardDay.AddDays(1)), Day(boardLast.AddDays(-1)), UsBillPath.UnsignedWords(CountryId.USA, boardDay, boardLast.AddDays(-1)),
                        Day(boardLast), UsBillPath.UnsignedWords(CountryId.USA, boardDay, boardLast), lastDayWrong));
                var recordDay = new DateTime(2026, 6, 29);
                Check(PresidentialDocket.LawDay(CountryId.USA, recordDay) == new DateTime(2026, 7, 11) && UsBillPath.TenDays(CountryId.USA, recordDay).Last().Date == new DateTime(2026, 7, 10),
                    F("(a) the law of record that became law unsigned (P.L. 119-101, held to its page by UsDocketDiagnostic (f)): presented {0}, the tenth counted day {1}, law {2}", Day(recordDay),
                        Day(UsBillPath.TenDays(CountryId.USA, recordDay).Last().Date), Day(PresidentialDocket.LawDay(CountryId.USA, recordDay))));

                // ============================================================ (b) the lines and the classes, read from the chambers
                foreach ((string label, DateTime epoch, string party) in new[] { ("the 118th of record, the US start", WorldClock.StartDate(CountryId.USA), "DEM"), ("the 119th of record", UsCongressDiagnostic.Day119, "REP") })
                {
                    using (SimulationManager.EpochScope())
                    {
                        try
                        {
                            (SimulationManager sim, World world, Country usa) = UsCongressDiagnostic.OpenUsa(hosts, epoch, party);
                            DateTime day = sim.CurrentDate;
                            UsCongressView.View v = UsCongressView.Read(usa, day);
                            Check(v != null, F("(b) {0} ({1}): the view is read", label, Day(day)));
                            if (v == null) { continue; }
                            int houseSeated = Congress.Sworn(usa.ParliamentSeats), senateSworn = Congress.Sworn(usa.SecondChamberSeats);
                            string hm = Congress.HouseMajority(usa), smj = Congress.SenateMajority(usa, day);
                            var houseOverride = new Congress.OverrideVote { Members = houseSeated };
                            var senateOverride = new Congress.OverrideVote { Members = senateSworn };
                            Check(v.HouseSeated == houseSeated && v.HouseMajorityLine.Figure == houseSeated / 2 + 1 && v.HouseOverride.Figure == houseOverride.Required
                                  && v.HouseMajority == hm && v.HouseMajorityLine.Held == usa.ParliamentSeats[hm] && v.HouseOverride.Held == usa.ParliamentSeats[hm],
                                F("(b) {0}: the House {1} seated - majority {2} ({3} {4}, {5}), override {6} (two thirds of those voting: Congress.OverrideVote.Required of {1}), {3} {7}",
                                    label, houseSeated, v.HouseMajorityLine.Figure, hm, v.HouseMajorityLine.Held, v.HouseMajorityLine.Holds ? "holds it" : v.HouseMajorityLine.Short + " short", v.HouseOverride.Figure,
                                    v.HouseOverride.Holds ? "holds it" : v.HouseOverride.Short + " short"));
                            Check(v.SenateSworn == senateSworn && v.SenateMajorityLine.Figure == senateSworn / 2 + 1 && v.Cloture.Figure == Congress.ClotureLine(senateSworn) && v.SenateOverride.Figure == senateOverride.Required
                                  && v.SenateMajority == smj && v.SenateMajoritySeats == usa.SecondChamberSeats[smj],
                                F("(b) {0}: the Senate {1} sworn - majority {2}, cloture {3} (Congress.ClotureLine), override {4}; {5} {6}{7} ({8} short of cloture, {9} of the override)",
                                    label, senateSworn, v.SenateMajorityLine.Figure, v.Cloture.Figure, v.SenateOverride.Figure, smj, v.SenateMajoritySeats,
                                    v.IndependentsIn(smj) > 0 ? " - a caucus with " + v.IndependentsIn(smj) + " independent(s)" : string.Empty, v.Cloture.Short, v.SenateOverride.Short));

                            // the classes against the world's seated Senate and the record's seating of the day
                            UsSenate.Seating record = UsSenate.On(day);
                            int seatedInClasses = v.Classes.Sum(c => c.Seats.Count(s => s.Side != null));
                            bool bySide = usa.SecondChamberSeats.All(kv => v.Classes.Sum(c => c.Seats.Count(s => s.Side == kv.Key)) == kv.Value);
                            bool byClass = v.Classes.All(c => c.Seats.Count(s => s.Side != null) == record.Seats.Count(s => s.Class == c.Class)
                                                              && c.Seats.Count(s => s.Independent) == record.Seats.Count(s => s.Class == c.Class && s.Party == "I")
                                                              && c.Seats.Where(s => s.Side != null).All(s => record.Seats.Any(r => r.State == s.State && r.Class == s.Class && r.Side == s.Side)));
                            // each class dated on its cycle's polling day whose Congress has convened, and not on the next of its cycle (six years on), which has not
                            bool dated = v.Classes.All(c => c.ElectedOn != DateTime.MinValue && c.ElectedOn.Year % 2 == 0 && UsCongressElection.ClassUp(c.ElectedOn.Year) == c.Class
                                                            && UsCongressElection.ConvenesAfter(c.ElectedOn) <= day
                                                            && UsCongressElection.ConvenesAfter(UsPresidentialElection.PresidentialDay(c.ElectedOn.Year + 6)) > day
                                                            && c.UpOn == UsPresidentialElection.PresidentialDay(c.ElectedOn.Year + 6) && !c.Game);
                            int independents = v.SenateIndependents.Values.Sum();
                            Check(seatedInClasses == senateSworn && bySide && byClass && dated && independents == record.Independents,
                                F("(b) {0}: the three classes hold the {1} seated senators, side by side as the world seats them and class by class as the record seats them, {2} independent(s) apart: {3}",
                                    label, seatedInClasses, independents,
                                    string.Join("; ", v.Classes.Select(c => F("class {0} the record's, elected {1}, up {2}: DEM {3}, IND {4}, REP {5}{6}", c.Class, Day(c.ElectedOn), Day(c.UpOn),
                                        c.Of("DEM", false), c.Of("DEM", true) + c.Of("REP", true), c.Of("REP", false), c.Vacant > 0 ? ", " + c.Vacant + " vacant" : string.Empty)))));
                            Congress.PresidentOn(usa, day, out string president, out string pp, out _);
                            Check(v.PresidentsParty == pp && v.HouseDivided == (hm != null && hm != pp) && v.SenateDivided == (smj != null && smj != pp) && v.PresidentUntil == UsCongressView.TermEndAfter(day),
                                F("(b) {0}: the President {1} ({2}) to {3}; the House {4}, the Senate {5} - {6}; next vote {7} ({8}, class {9}, {10} seats)", label, president, pp, Day(v.PresidentUntil), hm, smj,
                                    v.Divided ? "DIVIDED" : "unified", Day(v.NextVote), v.NextMidterm ? "a midterm" : "with the President", v.NextClass, v.NextSenateSeats));
                        }
                        finally { EnergyMarket.ResetTurnState(); }
                    }
                }

                // ============================================================ (c) a bill's path on planted records
                using (SimulationManager.EpochScope())
                {
                    try
                    {
                        (SimulationManager sim, World world, Country usa) = UsCongressDiagnostic.OpenUsa(hosts, UsCongressDiagnostic.Day119, "REP");
                        DateTime day = sim.CurrentDate;
                        DivisionRecord Rec(int chamber, int origin, int yea, int nay, bool passed, int required = 0, bool cloture = false, DivisionRecord other = null)
                        {
                            var r = new DivisionRecord { Number = 1, Title = "planted", Date = day, Chamber = chamber, Origin = origin, Passed = passed, Required = required, Cloture = cloture, OtherChamber = other, Sworn = chamber == Congress.Senate ? 100 : 0 };
                            if (yea > 0) { r.Sides.Add(new DivisionSide { Abbrev = "DEM", Seats = yea, Side = 1 }); }
                            if (nay > 0) { r.Sides.Add(new DivisionSide { Abbrev = "REP", Seats = nay, Side = -1 }); }
                            return r;
                        }
                        string Marks(List<UsBillPath.Station> p) => p == null ? "null" : string.Join(" ", p.Select(s => s.Mark.ToString()));
                        string Words(List<UsBillPath.Station> p) => p == null ? "null" : string.Join(" -> ", p.Select(s => s.Label + " " + s.Status));
                        DateTime law = PresidentialDocket.LawDay(CountryId.USA, day);
                        DivisionRecord house = Rec(Congress.House, Congress.House, 222, 213, true);
                        DivisionRecord passage = Rec(Congress.Senate, Congress.House, 57, 43, true, other: house);
                        List<UsBillPath.Station> onDesk = UsBillPath.Of(usa, passage, true, law);
                        Check(Marks(onDesk) == "Done Done Now Conditional Conditional" && onDesk[0].Status == "PASSED 222–213" && onDesk[4].Status == law.ToString("d MMM yyyy", CultureInfo.InvariantCulture).ToUpperInvariant(),
                            "(c) on the desk: " + Words(onDesk) + " [" + Marks(onDesk) + "]");
                        List<UsBillPath.Station> signed = UsBillPath.Of(usa, passage, false, DateTime.MinValue);
                        Check(Marks(signed) == "Done Done Done Off Passed", "(c) answered by signature: " + Words(signed) + " [" + Marks(signed) + "]");
                        DivisionRecord sustained = Rec(Congress.House, Congress.House, 222, 213, false, required: UsCongressView.OverrideLine(435));
                        List<UsBillPath.Station> stands = UsBillPath.Of(usa, sustained, false, DateTime.MinValue);
                        Check(Marks(stands) == "Done Done Failed Failed Off" && stands[3].Status == "SUSTAINED", "(c) a veto the House sustains: " + Words(stands) + " [" + Marks(stands) + "]");
                        DivisionRecord firstOverride = Rec(Congress.House, Congress.House, 290, 145, true, required: UsCongressView.OverrideLine(435));
                        DivisionRecord secondOverride = Rec(Congress.Senate, Congress.House, 71, 29, true, required: UsCongressView.OverrideLine(100), other: firstOverride);
                        List<UsBillPath.Station> over = UsBillPath.Of(usa, secondOverride, false, DateTime.MinValue);
                        Check(Marks(over) == "Done Done Failed Passed Passed" && over[3].Status == "OVERRIDDEN", "(c) a veto overridden in both chambers: " + Words(over) + " [" + Marks(over) + "]");
                        // the review's M1: the origin overrides, the other chamber sustains - the station names the chamber that sustained it, with its own votes
                        DivisionRecord houseOverrides = Rec(Congress.House, Congress.House, 300, 135, true, required: UsCongressView.OverrideLine(435));
                        DivisionRecord senateSustains = Rec(Congress.Senate, Congress.House, 60, 40, false, required: UsCongressView.OverrideLine(100), other: houseOverrides);
                        List<UsBillPath.Station> split = UsBillPath.Of(usa, senateSustains, false, DateTime.MinValue);
                        string senateName = Congress.ChamberName(CountryId.USA, Congress.Senate);
                        Check(Marks(split) == "Done Done Failed Failed Off" && split[3].Status == "SUSTAINED" && split[3].Sub.StartsWith("The " + senateName + " 60 for · " + UsCongressView.OverrideLine(100) + " needed", StringComparison.Ordinal),
                            "(c) the House overrides, the Senate sustains: " + Words(split) + " - \"" + split[3].Sub + "\"");
                        // the review's M2: an uncontested passage records no sides - its yeas are the base the override counts (the House seated), never a zero not counted
                        DivisionRecord uncontested = Rec(Congress.House, Congress.House, 0, 0, true);
                        DivisionRecord nothingHeld = Rec(Congress.Senate, Congress.House, 0, 0, true, other: uncontested);
                        nothingHeld.Sworn = 0;
                        Check(UsBillPath.Yeas(usa, uncontested) == Congress.Sworn(usa.ParliamentSeats) && UsBillPath.PassedWith(usa, uncontested) == Congress.Sworn(usa.ParliamentSeats).ToString(CultureInfo.InvariantCulture)
                              && UsBillPath.Yeas(usa, nothingHeld) == -1 && UsBillPath.PassedWith(usa, nothingHeld) == "an unrecorded count" && UsBillPath.Yeas(usa, firstOverride) == 290,
                            F("(c) the desk's \"passed with\": an uncontested House passage {0} (the House seated, {1}); a record holding no count \"{2}\"; a recorded vote its FOR side, {3}",
                                UsBillPath.PassedWith(usa, uncontested), Congress.Sworn(usa.ParliamentSeats), UsBillPath.PassedWith(usa, nothingHeld), UsBillPath.Yeas(usa, firstOverride)));
                        // the review's L6: a bill with a deadline of its own does not claim the ten days
                        List<UsBillPath.Station> deadline = UsBillPath.Of(usa, passage, true, law, countsTenDays: false);
                        Check(!deadline[2].Sub.Contains("ten days") && onDesk[2].Sub.Contains("ten days"), F("(c) the desk's sub: \"{0}\" for a bill with its own deadline, \"{1}\" for the ten days", deadline[2].Sub, onDesk[2].Sub));
                        // the review's L2: an equal division the Vice President's party holds - the line held, none short
                        var equal = new UsCongressView.Line { Figure = 51, Held = 50, Holder = "DEM", ByVicePresident = true };
                        var shortOne = new UsCongressView.Line { Figure = 51, Held = 50, Holder = "DEM" };
                        Check(equal.Holds && equal.Short == 0 && !shortOne.Holds && shortOne.Short == 1, "(c) a 50-50 Senate held with the Vice President's vote reads held, 0 short; 50 without it, 1 short");
                        DivisionRecord stopped = Rec(Congress.Senate, Congress.House, 51, 49, false, required: Congress.ClotureLine(100), cloture: true, other: house);
                        List<UsBillPath.Station> dies = UsBillPath.Of(usa, stopped, false, DateTime.MinValue);
                        Check(Marks(dies) == "Done Failed Off Off Off", "(c) stopped at cloture in the Senate: " + Words(dies) + " [" + Marks(dies) + "]");
                        DivisionRecord senateFirst = Rec(Congress.Senate, Congress.Senate, 57, 43, true, required: Congress.ClotureLine(100), cloture: true);
                        DivisionRecord thenHouse = Rec(Congress.House, Congress.Senate, 222, 213, true, other: senateFirst);
                        List<UsBillPath.Station> senateOrder = UsBillPath.Of(usa, thenHouse, true, law);
                        Check(senateOrder[0].Label.Contains(Congress.ChamberName(CountryId.USA, Congress.Senate)) && senateOrder[1].Label.Contains(Congress.ChamberName(CountryId.USA, Congress.House)),
                            "(c) a Senate-first kind: " + Words(senateOrder));
                    }
                    finally { EnergyMarket.ResetTurnState(); }
                }

                // ============================================================ (d) the debt tile's chip on planted limits
                using (SimulationManager.EpochScope())
                {
                    try
                    {
                        (SimulationManager sim, World world, Country u) = UsCongressDiagnostic.OpenUsa(hosts, UsCongressDiagnostic.Day119, "REP");
                        DateTime day = sim.CurrentDate;
                        double subject = UsDebtLimit.SubjectToLimitUsd(u);
                        u.DebtLimitSince = day.AddDays(-30);
                        u.DebtLimitSuspendedThrough = DateTime.MinValue;
                        string Chip(BudgetBill pending, out FiscalRules.DebtLimitInk ink, out string sub) => FiscalRules.DebtLimitChip(u, day, pending, out ink, out sub);
                        u.DebtLimitUsd = subject + 1e12;
                        u.DebtLimitReachedOn = DateTime.MinValue;
                        string below = Chip(null, out _, out string headroom);
                        Check(below == null && headroom != null && headroom.StartsWith("limit ", StringComparison.Ordinal) && headroom.EndsWith(" below", StringComparison.Ordinal), F("(d) below the limit: no chip, the line \"{0}\"", headroom));
                        u.DebtLimitUsd = subject;
                        u.DebtLimitReachedOn = day.AddDays(-10);
                        string at = Chip(null, out FiscalRules.DebtLimitInk atInk, out _);
                        string before = Chip(new BudgetBill { DaysRemaining = 3 }, out FiscalRules.DebtLimitInk beforeInk, out _);
                        string deskEarly = Chip(new BudgetBill { Docket = new DocketEntry() }, out _, out _);
                        Check(at == "AT THE LIMIT · " + (UsDebtLimit.MeasuresSpanDays - 10) + " DAYS" && atInk == FiscalRules.DebtLimitInk.Clock && before == "RAISE BEFORE CONGRESS" && beforeInk == FiscalRules.DebtLimitInk.Clock && deskEarly == "RAISE ON YOUR DESK",
                            F("(d) reached ten days ago (the measures' {0} days): \"{1}\"; a raise tabled: \"{2}\"; on the desk: \"{3}\" - each a clock", UsDebtLimit.MeasuresSpanDays, at, before, deskEarly));
                        u.DebtLimitReachedOn = day.AddDays(-UsDebtLimit.MeasuresSpanDays - 1);
                        string binds = Chip(null, out FiscalRules.DebtLimitInk bindsInk, out _);
                        string bindsDesk = Chip(new BudgetBill { Docket = new DocketEntry() }, out FiscalRules.DebtLimitInk bindsDeskInk, out _);
                        Check(UsDebtLimit.Binding(u, day) && binds == "LIMIT BINDS · CUT IN FORCE" && bindsInk == FiscalRules.DebtLimitInk.Cut && bindsDesk == "BINDS · RAISE ON YOUR DESK" && bindsDeskInk == FiscalRules.DebtLimitInk.Cut,
                            F("(d) the measures run out: \"{0}\"; the raise on the desk: \"{1}\" - the cut in force", binds, bindsDesk));
                        u.DebtLimitSuspendedThrough = day.AddDays(60);
                        string suspended = Chip(null, out FiscalRules.DebtLimitInk suspendedInk, out _);
                        Check(suspended == "SUSPENDED TO " + day.AddDays(60).ToString("d MMM yyyy", CultureInfo.InvariantCulture).ToUpperInvariant() && suspendedInk == FiscalRules.DebtLimitInk.SetAside,
                            F("(d) suspended: \"{0}\" - set aside", suspended));
                        List<string> lines = FiscalRules.DebtLimitLines(u, day, null);
                        Check(lines.Count > 0 && lines.All(l => l.Length > 1 && !(char.IsUpper(l[1]) && char.IsUpper(l[0]))), F("(d) the slip in sentence case: \"{0}\"", string.Join(" | ", lines)));
                    }
                    finally { EnergyMarket.ResetTurnState(); }
                }

                // ============================================================ (e) K42: funding restored, a saved day (it replaced the controller's memo)
                {
                    var d1 = new DateTime(2025, 11, 12);
                    var restored = new Country { Id = CountryId.USA, LapseRestoredOn = d1 };
                    var neverLapsed = new Country { Id = CountryId.USA };
                    var lapsedAgain = new Country { Id = CountryId.USA, LapseRestoredOn = d1, LapseSince = d1.AddDays(1) };
                    int w = UsBudgetProcedure.RestoredWindowDays;
                    bool onDay = UsBudgetProcedure.RestoredWithin(restored, d1), dayBefore = UsBudgetProcedure.RestoredWithin(restored, d1.AddDays(-1)),
                         lastDay = UsBudgetProcedure.RestoredWithin(restored, d1.AddDays(w)), pastWindow = UsBudgetProcedure.RestoredWithin(restored, d1.AddDays(w + 1));
                    bool never = UsBudgetProcedure.RestoredWithin(neverLapsed, d1), again = UsBudgetProcedure.RestoredWithin(lapsedAgain, d1.AddDays(1));
                    Check(onDay && !dayBefore && lastDay && !pastWindow && !never && !again,
                        F("(e) K42: the restoration's window reads the saved day {0} to {1} days after it ({2}; the day before {3}, the last day {4}, past it {5}); a country that never lapsed {6}; lapsed again since {7} (UsBudgetProcedureDiagnostic (c) holds the model writing it)",
                            Day(d1), w, onDay, dayBefore, lastDay, pastWindow, never, again));
                    // the review's L2: the Desk shows it once, on the first day drawn in the window - read through the controller's own RestoredChipToday
                    var chipHost = new GameObject("UsCongressViewDiagnostic.Restored"); hosts.Add(chipHost);
                    var controller = chipHost.AddComponent<GameController>();
                    MethodInfo shows = typeof(GameController).GetMethod("RestoredChipToday", BindingFlags.Instance | BindingFlags.NonPublic);
                    bool Shows(Country c, DateTime on) => shows != null && (bool)shows.Invoke(controller, new object[] { c, on });
                    bool skipped = Shows(restored, d1.AddDays(2)), skippedAgain = Shows(restored, d1.AddDays(2)), nextDay = Shows(restored, d1.AddDays(3));
                    var loadedCopy = new Country { Id = CountryId.USA, LapseRestoredOn = d1 };   // a load inside the window: a new country object
                    bool onLoad = Shows(loadedCopy, d1.AddDays(3)), pastOnLoad = Shows(new Country { Id = CountryId.USA, LapseRestoredOn = d1 }, d1.AddDays(w + 1));
                    Check(shows != null && skipped && skippedAgain && !nextDay && onLoad && !pastOnLoad,
                        F("(e) K42 (L2): a frame that steps past the day shows the chip on the first day drawn, {0} ({1}, again that day {2}), not the day after ({3}); a game loaded on {4} shows it that day ({5}), none past the window ({6})",
                            Day(d1.AddDays(2)), skipped, skippedAgain, nextDay, Day(d1.AddDays(3)), onLoad, pastOnLoad));
                }

                // ============================================================ (f) K40 by behaviour; K39 and the review's H1 pinned on the drawing code's shape (no IMGUI pass runs in batch)
                {
                    // K40: the masthead's predicate, read through the controller's own MastheadStripApplies on planted countries
                    var mastHost = new GameObject("UsCongressViewDiagnostic.Masthead"); hosts.Add(mastHost);
                    var mast = mastHost.AddComponent<GameController>();
                    const BindingFlags OwnFlags = BindingFlags.Instance | BindingFlags.NonPublic;
                    MethodInfo applies = typeof(GameController).GetMethod("MastheadStripApplies", OwnFlags);
                    FieldInfo playerField = typeof(GameController).GetField("_playerCountry", OwnFlags), overField = typeof(GameController).GetField("_isGameOver", OwnFlags);
                    bool Mast(Country c, bool over)
                    {
                        if (applies == null || playerField == null || overField == null) { return false; }
                        playerField.SetValue(mast, c); overField.SetValue(mast, over);
                        return (bool)applies.Invoke(mast, null);
                    }
                    var usaSeated = new Country { Id = CountryId.USA, SecondChamberSeats = new Dictionary<string, int> { { "REP", 53 }, { "DEM", 47 } } };
                    var usaNoSenate = new Country { Id = CountryId.USA };
                    var sweden = new Country { Id = CountryId.Sweden, SecondChamberSeats = new Dictionary<string, int> { { "S", 10 } } };
                    bool inCongress = Mast(usaSeated, false), atGameOver = Mast(usaSeated, true), noSenate = Mast(usaNoSenate, false), swedish = Mast(sweden, false);
                    // K178 (Elias's ruling: the top strip in every country) widened K40's predicate - a US world with no Senate seated and Sweden carry it too
                    Check(applies != null && inCongress && !atGameOver && noSenate && swedish,
                        F("(f) K40, K178: the top strip carries the role and lapse chips in a Congress ({0}) - not at game over ({1}); in a US world with no Senate seated ({2}) and in Sweden ({3}) too, every country's since K178",
                            inCongress, atGameOver, noSenate, swedish));
                    string rows = SourceText.ReadWithoutComments("Assets/Scripts/UI/GameController.ParliamentRows.cs");
                    string view = SourceText.ReadWithoutComments("Assets/Scripts/UI/GameController.CongressV35.cs");
                    string desk = SourceText.ReadWithoutComments("Assets/Scripts/UI/GameController.Desk.cs");
                    string frame = SourceText.ReadWithoutComments("Assets/Scripts/UI/GameController.cs");
                    string Flat(string s) => System.Text.RegularExpressions.Regex.Replace(s ?? string.Empty, "\\s+", " ");
                    string flatRows = Flat(rows);
                    bool gated = flatRows.Contains("bool congressView = PoliSim.Simulation.UsCongressView.Applies(_playerCountry);")
                                 && flatRows.Contains("if (!congressView) { DrawRecordRow(book); }")
                                 && flatRows.Contains("if (!congressView) { DrawUsPresidentRow(book); DrawUsCongressRow(book); DrawUsNomineeRow(book); }");
                    int calls = System.Text.RegularExpressions.Regex.Matches(flatRows, "Draw(Record|UsPresident|UsCongress|UsNominee)Row\\(book\\);").Count;
                    bool strip = Flat(view).Contains("if (nominee) { DrawWhoGovernsNominee(") && Flat(view).Contains("StepUsNominee(nominee, choices, day, party)");
                    Check(gated && calls == 4 && strip,
                        F("(f) K39, A PIN ON THE CODE'S SHAPE (the film shows the surface): the dated rows (the record's, THE PRESIDENT, CONGRESS, YOUR NOMINEE) are drawn only where no Congress view stands ({0}, {1} call sites), and the nominee's line with its NEXT CHOICE is the who-governs strip's ({2})",
                            gated, calls, strip));
                    // the review's H1: the blocks under the view put their slips on the page's book and leave its anchors alone - one DrawSlips pass judges the page
                    System.Text.RegularExpressions.Match blocks = System.Text.RegularExpressions.Regex.Match(flatRows, "private void DrawParliamentPoliticalBlocks\\(\\) \\{(.*?) private ");
                    bool onePass = blocks.Success && !blocks.Groups[1].Value.Contains("BeginSlipAnchors") && !blocks.Groups[1].Value.Contains("DrawSlips(")
                                   && blocks.Groups[1].Value.Contains("PeopleSlips.Book book = _politicsSlipBook;");
                    Check(onePass, F("(f) H1, A PIN ON THE CODE'S SHAPE: the Parliament blocks clear no anchor and run no slip pass of their own - their slips are the Politics page's, judged once ({0})", onePass));
                    string flatDesk = Flat(desk), flatFrame = Flat(frame);
                    bool deskYields = flatDesk.Contains("(string Words, bool InOffice)? role = strip ? null : RoleChip();") && flatDesk.Contains("lapse = strip ? null : LapseChip(_simulationManager.CurrentDate);");
                    bool frameCarries = flatFrame.Contains("_bannerIsMasthead = MastheadStripApplies();") && flatFrame.Contains("DrawHoldChips(holds, columnWidth, _bannerIsMasthead)");
                    Check(deskYields && frameCarries,
                        F("(f) K40, A PIN ON THE CODE'S SHAPE: the predicate above decides both the Desk's masthead ({0}) and the frame's top masthead ({1})", deskYields, frameCarries));
                }
            }
            catch (Exception e) { failures++; sb.Append("    FAIL      threw - ").Append(e.GetType().Name).Append(": ").Append(e.Message).Append('\n').Append(e.StackTrace).Append('\n'); }
            finally
            {
                foreach (GameObject go in hosts) { if (go != null) { UnityEngine.Object.DestroyImmediate(go); } }
                SimulationRandom.RestoreState(seedWas, drawsWas);
            }

            sb.Append(failures == 0 ? "=== UsCongressViewDiagnostic: the Congress view, the bill's path and the debt chip read the model's own figures ===\n"
                                    : F("=== UsCongressViewDiagnostic: {0} FAILURE(S) ===\n", failures));
            if (failures == 0) { Debug.Log(sb.ToString()); CheckExit.Finish(0); }
            else { Debug.LogError(sb.ToString()); CheckExit.Finish(1); }
        }

        private static string Day(DateTime d) => d == DateTime.MinValue ? "none" : d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        private static string F(string format, params object[] args) => string.Format(CultureInfo.InvariantCulture, format, args);
    }
}
