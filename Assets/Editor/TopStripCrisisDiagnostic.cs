using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using PoliSim.Data;
using PoliSim.Elections;
using PoliSim.UI;
using UnityEngine;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// K178 and MF q33 on screen (D-WORKLIST §1-§2, Design's return of 2026-10-28, boards 32a-32e and 33a): <b>THE TOP STRIP SAYS WHAT THE MODEL HOLDS,
    /// AND THE DEBT CRISIS'S PLATE COMES ONCE A CRISIS</b> - read on planted countries through <see cref="TopStrip"/>'s pure readings and the controller's own
    /// private members (no IMGUI pass runs in batch; the film shows the surface):
    /// <list type="bullet">
    /// <item>(a) THE MOMENT: owed on the day a crisis opens and not the day after, not once acknowledged (Continue's own method), owed again for a second
    /// crisis; never with the market open.</item>
    /// <item>(b) THE CRISIS CHIP: a default binds on the BAD ground with its time left; a programme year is coming on the AMB ground; a withheld year binds on the
    /// BAD ground with its day; the market's reopening is GOOD on its one day and gone the next; none with no crisis; the slip carries the crisis's sentences.</item>
    /// <item>(c) THE WORDS: the time left, the polling day's countdown inside thirty days and its date beyond, the fold of a fourth time chip into "+1".</item>
    /// <item>(d) THE STANDING on planted governments: minority with support, minority, majority, coalition majority, caretaker, France's relative majority,
    /// Italy's majority in both chambers, Poland's cohabitation against the president of record's backing party.</item>
    /// <item>(e) THE DECLARATION: a declared, unanswered no-confidence motion is coming (AMB) where the chamber takes motions, and nothing for a caretaker, for
    /// Poland or for the USA.</item>
    /// <item>(f) K178: the controller's strip predicate holds for all six countries and not at game over; A PIN ON THE CODE'S SHAPE - the plate gates the
    /// clock in both of Update's places and is named among the holds, and the Desk's first card is the crisis's while one stands.</item>
    /// </list>
    /// </summary>
    public static class TopStripCrisisDiagnostic
    {
        public static void Run()
        {
            CheckExit.ArmLogFold();
            var sb = new StringBuilder("=== TopStripCrisisDiagnostic (K178, MF q33 on screen): the top strip in every country and the debt crisis's moment ===\n");
            int failures = 0;
            void Check(bool ok, string what) { if (!ok) { failures++; } sb.Append(ok ? "    ok        " : "    FAIL      ").Append(what).Append('\n'); }
            var hosts = new List<GameObject>();
            try
            {
                var d = new DateTime(2031, 5, 14);

                // ============================================================ (a) the moment
                {
                    var c = new Country { Id = CountryId.USA, Crisis = new SovereignCrisisState { Phase = CrisisPhase.Default, StartedOn = d, EndsOn = d.AddDays(1058), Crises = 1 } };
                    bool onDay = TopStrip.CrisisPlateOwed(c, d, 0), dayAfter = TopStrip.CrisisPlateOwed(c, d.AddDays(1), 0), seen = TopStrip.CrisisPlateOwed(c, d, 1);
                    var open = new Country { Id = CountryId.USA, Crisis = new SovereignCrisisState { Phase = CrisisPhase.None, StartedOn = d, Crises = 1 } };
                    bool none = TopStrip.CrisisPlateOwed(open, d, 0);
                    var second = new Country { Id = CountryId.Sweden, Crisis = new SovereignCrisisState { Phase = CrisisPhase.Programme, StartedOn = d.AddYears(5), Crises = 2 } };
                    bool again = TopStrip.CrisisPlateOwed(second, d.AddYears(5), 1);
                    Check(onDay && !dayAfter && !seen && !none && again,
                        F("(a) the plate is owed on the crisis's first day ({0}), not the day after ({1}), not once acknowledged ({2}), not with the market open ({3}); a second crisis owes it again ({4})", onDay, dayAfter, seen, none, again));

                    var host = new GameObject("TopStripCrisisDiagnostic.Plate"); hosts.Add(host);
                    var controller = host.AddComponent<GameController>();
                    const BindingFlags Own = BindingFlags.Instance | BindingFlags.NonPublic;
                    FieldInfo player = typeof(GameController).GetField("_playerCountry", Own), seenField = typeof(GameController).GetField("_crisisPlateSeenCrises", Own),
                              seenCountry = typeof(GameController).GetField("_crisisPlateSeenCountry", Own);
                    MethodInfo ack = typeof(GameController).GetMethod("AcknowledgeCrisisPlate", Own);
                    bool acknowledged = false;
                    if (player != null && seenField != null && seenCountry != null && ack != null)
                    {
                        player.SetValue(controller, second);
                        ack.Invoke(controller, null);
                        acknowledged = (int)seenField.GetValue(controller) == 2 && ReferenceEquals(seenCountry.GetValue(controller), second);
                    }
                    Check(acknowledged, F("(a) Continue (the controller's AcknowledgeCrisisPlate) records the crises entered on this country object ({0})", acknowledged));
                }

                // ============================================================ (a2) the model's day is the controller's (the review's L7)
                {
                    // the model's own step on a planted review day reading CCC: the crisis opens with StartedOn the day the manager passes, which is its
                    // CurrentDate after the advance - so the plate the controller reads on that date is owed
                    var day = new DateTime(2031, 4, 1);
                    var c = new Country
                    {
                        Id = CountryId.Sweden,
                        State = new EconomyState { GDP = 100f, GovernmentDebt = 1000f },
                        Rating = new SovereignRatingState { HasBeenReviewed = true, LastReviewDate = day, Rating = CreditRating.CCC },
                        Crisis = new SovereignCrisisState(),
                    };
                    PoliSim.Simulation.SovereignCrisis.AdvanceDay(c, day);
                    bool opened = c.Crisis.Phase == CrisisPhase.Programme && c.Crisis.StartedOn == day && c.Crisis.Crises == 1;
                    bool owed = TopStrip.CrisisPlateOwed(c, day, 0);
                    string manager = Regex.Replace(SourceText.ReadWithoutComments("Assets/Scripts/Simulation/SimulationManager.cs") ?? string.Empty, "\\s+", " ");
                    int advance = manager.IndexOf("public bool AdvanceDay() { CurrentDate = CurrentDate.AddDays(1);", StringComparison.Ordinal);
                    int step = manager.IndexOf("SovereignCrisis.AdvanceDay(country, CurrentDate);", StringComparison.Ordinal);
                    bool ordered = advance >= 0 && step > advance;
                    Check(opened && owed && ordered,
                        F("(a2) the model's step on a CCC review day opens the crisis on that day ({0}: {1}, from {2}), the plate is owed on it ({3}); the manager advances CurrentDate before the crisis step reads it ({4})",
                            opened, c.Crisis.Phase, Day(c.Crisis.StartedOn), owed, ordered));
                }

                // ============================================================ (g) the calendar card fits its worst month at 1280 (the review's M2)
                {
                    // August 2026 opens on a Saturday: with Monday first it takes six rows. The 1280 x 699 geometry: the first row's 330 less the strip's
                    // 39 and the banner's 4, the card's 12 px padding twice, its 28 px head and 8 px gap; the day number at 16 px (the label's 17 x 0.95)
                    // in a line of 22 with its dots' row of 10 and a 1.3 px gap (the 1280 film: rows on a 33 px pitch), the weekday header 22 with its margins
                    int rows = CalendarFit.Rows(new DateTime(2026, 8, 1), DayOfWeek.Monday);
                    float body = 330f - 39f - 4f - 12f * 2f - 28f - 8f;
                    CalendarFit.Result withDense = CalendarFit.Fit(body, 64f, 22f, rows, 22f, 10f, 1.3f, 8f, 1f);
                    CalendarFit.Result noDense = CalendarFit.Fit(body, 0f, 22f, rows, 22f, 10f, 1.3f, 8f, 1f);
                    CalendarFit.Result tight = CalendarFit.Fit(210f, 64f, 22f, rows, 22f, 10f, 1.3f, 8f, 1f);
                    CalendarFit.Result none = CalendarFit.Fit(150f, 64f, 22f, rows, 22f, 10f, 1.3f, 8f, 1f);
                    bool sixRows = rows == 6 && CalendarFit.Rows(new DateTime(2027, 2, 1), DayOfWeek.Monday) == 4;
                    bool dropped = withDense.Fits && !withDense.DenseKept && withDense.DotRow == 10f;
                    bool plain = noDense.Fits && noDense.DotRow == 10f && noDense.Need <= body;
                    bool floored = tight.Fits && !tight.DenseKept && tight.DotRow == 8f && tight.Need <= 210f;
                    bool loud = !none.Fits;
                    Check(sixRows && dropped && plain && floored && loud,
                        F("(g) August 2026 takes {0} rows (February 2027, four); on heights DERIVED from the 1280 film's pitch (not measured here - the film's CALENDAR FIT line measures), in {1:0} px the provenance line drops into the slip and the grid keeps its full cell ({2}, needs {3:0}); without it, it fits whole ({4}); in 210 px the cell goes to its floor ({5}, needs {6:0}); in 150 px nothing fits and it is said ({7})",
                            rows, body, dropped, withDense.Need, plain, floored, tight.Need, loud));
                }

                // ============================================================ (b) the crisis chip
                {
                    var today = d.AddDays(220);
                    var def = new Country { Id = CountryId.USA, Crisis = new SovereignCrisisState { Phase = CrisisPhase.Default, StartedOn = d, EndsOn = d.AddDays(1058), Crises = 1, HaircutBillions = 1000f } };
                    TopStrip.Chip a = TopStrip.CrisisChip(def, today, out bool aBinds);
                    var prog = new Country { Id = CountryId.Sweden, Crisis = new SovereignCrisisState { Phase = CrisisPhase.Programme, StartedOn = d, EndsOn = d.AddYears(3).AddDays(-1), Crises = 1, ProgrammeYear = 1, EntryDeficitPercent = 8.4f, NextReviewOn = d.AddYears(1), WithheldThrough = DateTime.MinValue } };
                    TopStrip.Chip b = TopStrip.CrisisChip(prog, today, out bool bBinds);
                    var held = new Country { Id = CountryId.Sweden, Crisis = new SovereignCrisisState { Phase = CrisisPhase.Programme, StartedOn = d, EndsOn = d.AddYears(3).AddDays(-1), Crises = 1, ProgrammeYear = 2, EntryDeficitPercent = 8.4f, NextReviewOn = d.AddYears(2), WithheldThrough = d.AddYears(2).AddDays(-1), TargetsMissed = 1 } };
                    TopStrip.Chip e = TopStrip.CrisisChip(held, d.AddYears(1).AddDays(30), out bool eBinds);
                    var ended = new Country { Id = CountryId.Sweden, Crisis = new SovereignCrisisState { Phase = CrisisPhase.None, StartedOn = d, EndsOn = d.AddYears(3).AddDays(-1), Crises = 1 } };
                    TopStrip.Chip g = TopStrip.CrisisChip(ended, d.AddYears(3), out _);
                    TopStrip.Chip gNext = TopStrip.CrisisChip(ended, d.AddYears(3).AddDays(1), out _);
                    TopStrip.Chip never = TopStrip.CrisisChip(new Country { Id = CountryId.Sweden, Crisis = new SovereignCrisisState() }, d, out _);
                    bool defOk = a != null && aBinds && a.Face == TopStrip.Face.Bad && a.Words.StartsWith("Default · ", StringComparison.Ordinal) && a.Words.EndsWith(" left", StringComparison.Ordinal) && a.Slip.Count >= 3;
                    bool progOk = b != null && !bBinds && b.Face == TopStrip.Face.Amber && b.Words == "Programme · year 1 of 3" && b.Slip.Count >= 3;
                    bool heldOk = e != null && eBinds && e.Face == TopStrip.Face.Bad && e.Words == "Loan withheld · to 13 May 2033";   // the review, L7: written by hand, never by the formatter under test
                    bool restoredOk = g != null && g.Face == TopStrip.Face.Good && g.Words == "Market access restored" && gNext == null && never == null;
                    Check(defOk, F("(b) a default binds, BAD, with its time left and the crisis's sentences on its slip: '{0}' ({1} lines)", a?.Words, a?.Slip.Count));
                    Check(progOk, F("(b) a programme year is coming, AMB: '{0}'", b?.Words));
                    Check(heldOk, F("(b) a withheld year binds, BAD, dated: '{0}'", e?.Words));
                    Check(restoredOk, F("(b) the market's reopening is GOOD on {0} ('{1}'), gone the day after ({2}); none with no crisis ({3})", Day(d.AddYears(3)), g?.Words, gNext == null, never == null));
                }

                // ============================================================ (c) the words and the fold
                {
                    string left = TopStrip.TimeLeft(new DateTime(2031, 12, 20), new DateTime(2034, 4, 6)), shortLeft = TopStrip.TimeLeft(d, d.AddDays(11));
                    bool leftOk = Regex.IsMatch(left, @"^2 y \d{1,2} m left$") && shortLeft == "12 days left";
                    Check(leftOk, F("(c) the time left reads '{0}' over 2.3 years and '{1}' over twelve days", left, shortLeft));
                    string far = TopStrip.PollingWords(d.AddDays(31), d), near = TopStrip.PollingWords(d.AddDays(30), d), one = TopStrip.PollingWords(d.AddDays(1), d),
                           today = TopStrip.PollingWords(d, d), past = TopStrip.PollingWords(d.AddDays(-1), d);
                    Check(far == "14 Jun 2031" && near == "in 30 days" && one == "in 1 day" && today == "Polling day today" && past == null,
                        F("(c) the polling day: 31 days '{0}', 30 '{1}', 1 '{2}', the day '{3}', past {4}", far, near, one, today, past == null ? "none" : past));
                    TopStrip.Chip C(string w) => new TopStrip.Chip { Id = w, Words = w, Face = TopStrip.Face.Amber };
                    List<TopStrip.Chip> shown = TopStrip.FoldTimeChips(new[] { C("lapse"), C("default") }, new[] { C("programme"), C("confidence") }, out TopStrip.Chip more);
                    List<TopStrip.Chip> three = TopStrip.FoldTimeChips(new[] { C("lapse") }, new[] { C("programme"), C("confidence") }, out TopStrip.Chip none);
                    bool foldOk = shown.Count == TopStrip.TimeChipsMax && shown[0].Words == "lapse" && more != null && more.Words == "+1" && more.Slip.Count == 1 && more.Slip[0] == "confidence"
                                  && three.Count == 3 && none == null;
                    Check(foldOk, F("(c) four time chips show {0} and fold the fourth into '{1}' listing '{2}'; three fold nothing ({3})", shown.Count, more?.Words, more != null && more.Slip.Count > 0 ? more.Slip[0] : "-", none == null));
                }

                // ============================================================ (d) the standing
                {
                    var today = new DateTime(2026, 1, 18);
                    var seSeats = new Dictionary<string, int> { { "S", 107 }, { "SD", 73 }, { "M", 68 }, { "V", 24 }, { "C", 24 }, { "KD", 19 }, { "MP", 18 }, { "L", 16 } };
                    string Words(CountryId id, Dictionary<string, int> seats, string[] cabinet, string[] support, bool caretaker = false, Dictionary<string, int> senate = null)
                    {
                        var g = new GovernmentRecord { Kind = WorldClock.ExecutiveKind.Cabinet, Caretaker = caretaker, PmParty = cabinet.Length > 0 ? cabinet[0] : null };
                        g.Cabinet.AddRange(cabinet); g.Support.AddRange(support);
                        var c = new Country { Id = id, ParliamentSeats = seats, Government = g, SecondChamberSeats = senate ?? new Dictionary<string, int>() };
                        return TopStrip.StandingChip(c, today)?.Words;
                    }
                    string withSupport = Words(CountryId.Sweden, seSeats, new[] { "M", "KD", "L" }, new[] { "SD" });
                    string minority = Words(CountryId.Sweden, seSeats, new[] { "S" }, new string[0]);
                    string coalition = Words(CountryId.Sweden, seSeats, new[] { "S", "SD" }, new string[0]);
                    string single = Words(CountryId.Sweden, new Dictionary<string, int> { { "S", 200 }, { "M", 149 } }, new[] { "S" }, new string[0]);
                    string caretaker = Words(CountryId.Sweden, seSeats, new[] { "S" }, new string[0], caretaker: true);
                    string france = Words(CountryId.France, new Dictionary<string, int> { { "RE", 160 }, { "RN", 140 }, { "NFP", 180 }, { "LR", 97 } }, new[] { "RE", "LR" }, new string[0]);
                    var itCamera = new Dictionary<string, int> { { "FdI", 119 }, { "LEGA", 66 }, { "FI", 45 }, { "PD", 69 }, { "M5S", 52 }, { "AVS", 12 }, { "AZ", 37 } };
                    var itSenate = new Dictionary<string, int> { { "FdI", 65 }, { "LEGA", 29 }, { "FI", 18 }, { "PD", 40 }, { "M5S", 28 }, { "AVS", 4 }, { "AZ", 16 } };
                    string italy = Words(CountryId.Italy, itCamera, new[] { "FdI", "LEGA", "FI" }, new string[0], senate: itSenate);
                    bool presidentOfRecord = PresidentialElection.PresidentAt(CountryId.Poland, null, new DateTime(2024, 6, 1), out PresidencyOfRecord.President polish);
                    var pl = new GovernmentRecord { Kind = WorldClock.ExecutiveKind.Cabinet, PmParty = "KO" };
                    pl.Cabinet.AddRange(new[] { "KO", "TD", "LEWICA" });
                    string cohabitation = TopStrip.StandingChip(new Country { Id = CountryId.Poland, ParliamentSeats = new Dictionary<string, int> { { "KO", 157 }, { "TD", 65 }, { "LEWICA", 26 }, { "PiS", 194 } }, Government = pl }, new DateTime(2024, 6, 1))?.Words;
                    Check(withSupport == "Minority, with support" && minority == "Minority government" && coalition == "Coalition majority" && single == "Majority" && caretaker == "Caretaker",
                        F("(d) Sweden: M+KD+L with SD '{0}', S alone '{1}', S+SD '{2}', a single-party majority '{3}', a caretaker '{4}'", withSupport, minority, coalition, single, caretaker));
                    Check(france == "Minority government" && italy == "Majority in both chambers",
                        F("(d) France's minority '{0}' (the review's L8: no relative-majority term); Italy's FdI+LEGA+FI, a majority in the Camera and the Senate, '{1}'", france, italy));
                    Check(presidentOfRecord && cohabitation == "Cohabitation",
                        F("(d) Poland on 1 Jun 2024: the president of record {0} backed by {1}, a KO-led cabinet without it - '{2}'", polish.Name, polish.BackingParty, cohabitation));
                }

                // ============================================================ (e) the declaration
                {
                    var day = new DateTime(2027, 3, 14);
                    Country With(CountryId id, bool caretaker)
                    {
                        var g = new GovernmentRecord { Kind = WorldClock.ExecutiveKind.Cabinet, NoConfidenceOn = day, NoConfidenceMover = "M", Caretaker = caretaker, PmParty = "S" };
                        g.Cabinet.Add("S");
                        return new Country { Id = id, Government = g };
                    }
                    TopStrip.Chip se = TopStrip.ConfidenceChip(With(CountryId.Sweden, false), day);
                    TopStrip.Chip seCaretaker = TopStrip.ConfidenceChip(With(CountryId.Sweden, true), day);
                    TopStrip.Chip pl = TopStrip.ConfidenceChip(With(CountryId.Poland, false), day);
                    TopStrip.Chip us = TopStrip.ConfidenceChip(With(CountryId.USA, false), day);
                    Check(se != null && se.Face == TopStrip.Face.Amber && se.Words == "No confidence declared · 14 Mar" && seCaretaker == null && pl == null && us == null,
                        F("(e) a declared motion in Sweden '{0}'; none for a caretaker ({1}), Poland ({2}) or the USA ({3})", se?.Words, seCaretaker == null, pl == null, us == null));
                }

                // ============================================================ (f) K178 in every country, and the code's shape
                {
                    var host = new GameObject("TopStripCrisisDiagnostic.Strip"); hosts.Add(host);
                    var controller = host.AddComponent<GameController>();
                    const BindingFlags Own = BindingFlags.Instance | BindingFlags.NonPublic;
                    MethodInfo applies = typeof(GameController).GetMethod("MastheadStripApplies", Own);
                    FieldInfo player = typeof(GameController).GetField("_playerCountry", Own), over = typeof(GameController).GetField("_isGameOver", Own);
                    int carried = 0;
                    var six = new[] { CountryId.Sweden, CountryId.Germany, CountryId.France, CountryId.Italy, CountryId.Poland, CountryId.USA };
                    bool atOver = true;
                    if (applies != null && player != null && over != null)
                    {
                        foreach (CountryId id in six)
                        {
                            player.SetValue(controller, new Country { Id = id }); over.SetValue(controller, false);
                            if ((bool)applies.Invoke(controller, null)) { carried++; }
                        }
                        over.SetValue(controller, true);
                        atOver = (bool)applies.Invoke(controller, null);
                    }
                    Check(carried == six.Length && !atOver, F("(f) K178: the strip in {0} of {1} countries, not at game over ({2})", carried, six.Length, !atOver));

                    string Flat(string s) => Regex.Replace(s ?? string.Empty, "\\s+", " ");
                    string frame = Flat(SourceText.ReadWithoutComments("Assets/Scripts/UI/GameController.cs"));
                    string desk = Flat(SourceText.ReadWithoutComments("Assets/Scripts/UI/GameController.Desk.cs"));
                    bool gated = frame.Contains("if (CrisisPlateHolds()) { return; }") && frame.Contains("_signingQueue.Count > 0 || CrisisPlateHolds()");
                    bool named = frame.Contains("if (CrisisPlateHolds()) { holds.Add((");
                    bool drawn = frame.Contains("CrisisPlateInput(crisisPage);") && frame.Contains("DrawCrisisPlate(crisisPage);");
                    bool card = desk.Contains("if (SovereignCrisis.InCrisis(_playerCountry)) { DrawDeskCrisisCard(");
                    bool paused = frame.Contains("bool isTimePaused = hasPendingFedChairSelection || ClockHeld();");
                    Check(gated && named && drawn && card && paused,
                        F("(f) A PIN ON THE CODE'S SHAPE: the plate gates the clock before a day and after each ({0}), is named among the holds ({1}), takes the page's input and draws over it ({2}); the Desk's first card is the crisis's while one stands ({3}); the frame's held reading counts it ({4})",
                            gated, named, drawn, card, paused));

                    // the review's M1 and K-b, M2: the strip's clock reads every hold; the first row gives the strip's height back in every country, the USA's
                    // included; the calendar card fits its grid (CalendarFit) and reports what it could not
                    string strip = Flat(SourceText.ReadWithoutComments("Assets/Scripts/UI/GameController.TopStrip.cs"));
                    // review 2, N1: ONE hold predicate - Update's gate before a day and after each, the frame's held reading and the strip's clock all call
                    // ClockHeld, which is CollectHolds' list; and no hand-kept hold list is left in Update
                    bool clockAll = strip.Contains("bool paused = ClockHeldBy(ClockHeld(), _gameSpeed == GameSpeed.Paused);");
                    bool heldReads = frame.Contains("private bool ClockHeld() => UpdateFedChairSelectionState() | (CollectHolds(includeBudgetProcess: true).Count > 0);")
                                     && frame.Contains("if (ClockHeld()) { return; }") && frame.Contains("CrisisPlateHolds() || ClockHeld())")
                                     && !frame.Contains("|| BudgetWindowHolds() || DeclarationHolds() || FormationHolds() || DebtLimitHolds())");
                    bool everyRow = desk.Contains("rowWorld = Mathf.Max(V35.Px(DeskRowWorld) * 0.5f, rowWorld - _topStripHeight);") && !desk.Contains("if (!Congress.Applies(_playerCountry)) { rowWorld");
                    bool fitted = desk.Contains("CalendarFit.Result fit = DeskCalendarFit(monthStart, under, slip);") && desk.Contains("DrawCalendarMonthGrid(monthStart, today, markers, withTitle: false, fit: fit);")
                                  && desk.Contains("Debug.LogError(\"CALENDAR FIT: \"");
                    Check(clockAll && heldReads && everyRow && fitted,
                        F("(f) M1, N1: the strip's clock reads the one hold predicate ({0}), which Update's two gates and the held reading call, built on CollectHolds with no hand-kept list left ({1}); K-b: the first row gives the strip back in every country, the USA's included ({2}); M2: the calendar card fits its grid and says it when it cannot ({3})",
                            clockAll, heldReads, everyRow, fitted));
                }
            }
            catch (Exception e) { failures++; sb.Append("    FAIL      threw - ").Append(e.GetType().Name).Append(": ").Append(e.Message).Append('\n').Append(e.StackTrace).Append('\n'); }
            finally
            {
                foreach (GameObject go in hosts) { if (go != null) { UnityEngine.Object.DestroyImmediate(go); } }
            }

            sb.Append(failures == 0 ? "=== TopStripCrisisDiagnostic: the top strip reads the model in every country, and the crisis's plate comes once a crisis ===\n"
                                    : F("=== TopStripCrisisDiagnostic: {0} FAILURE(S) ===\n", failures));
            if (failures == 0) { Debug.Log(sb.ToString()); CheckExit.Finish(0); }
            else { Debug.LogError(sb.ToString()); CheckExit.Finish(1); }
        }

        private static string Day(DateTime d) => d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        private static string F(string format, params object[] args) => string.Format(CultureInfo.InvariantCulture, format, args);
    }
}
