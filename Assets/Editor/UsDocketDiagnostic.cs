using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
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
    /// PS-6 US-21b (Elias's ruling I12 (i), 2026-10-18: "A bill unanswered after the Constitution's ten days (Sundays excepted) becomes law (Art. I §7).
    /// Poland's player-president is the same at Art. 122's 21 days, DECLARED. The pocket veto stays out of scope."): THE DOCKET DIAGNOSTIC, on worlds opened
    /// as a new game opens them (`UsCongressDiagnostic.OpenUsa`: the 119th of record seated on 2025-02-03, the player REP) and stepped in the controller's
    /// order (the day, the player's day tick):
    /// <list type="bullet">
    /// <item>(a) the words held to their saved pages by SHA-256 - Art. I §7 cl. 2's ten days and its adjournment clause (the pocket veto, not modelled), and
    /// Konstytucja Art. 122 ust. 2's 21 days;</item>
    /// <item>(b) the period's arithmetic (`PresidentialDocket`), DERIVED by hand: the USA's count from the day after presentment with no Sunday counted, a span
    /// with one Sunday and one with two; Poland's 21 calendar days; no period elsewhere; and the docket's one holder (`SimulationManager.UsVetoGate`) acting
    /// in the USA alone (`UsVeto.Applies`) - so NO POLISH PATH REACHES THE DOCKET YET (Poland has no player-president until SP-7b);</item>
    /// <item>(c) a planted game-elected Trump, the player REP - the player-president: three REP tax-programme bills and a REP rates budget (its appropriation
    /// and its reconciliation) pass both chambers on one day and are HELD on his docket, nothing applied; unanswered, a bill is still held on its last day
    /// and is LAW THE DAY AFTER (a Sunday inside the span); US-21c (Elias's ruling J6 (i)): signed, it is law AT ONCE, on the answer's day, no money moved
    /// by the answer itself; vetoed, the veto goes to the override at once - the House sustains it at two thirds of the passage's sides, the bill falls and
    /// his own party's day is lost that day; the budget's acts, unanswered, apply once at the period's end and fund the government; the verbs refuse a second
    /// answer and a bill not on the docket; and an answer given on the eve of a change of presidency is law that day, before the successor whose rule (a)
    /// would veto it takes office;</item>
    /// <item>(f) US-21c (Elias's ruling J6 (ii)): the law's date against a law of record that became law unsigned - P.L. 119-101's slip law (the Office of the
    /// Federal Register's note) and its BILLSTATUS, held to their SHA-256: presented 29 June 2026, law without signature 11 July 2026, the day the rule gives;</item>
    /// <item>(d) the AI president unchanged: under Trump of record the same bill is signed by rule (a) on the day it passes, nothing held, and the verbs
    /// refuse a player who does not hold the presidency;</item>
    /// <item>(e) the save, cut mid-docket: the held bills, their passages and their presentment day ride it (the format unchanged - additive), and loaded INTO
    /// THE GAME the docket is live - stepped on, every held bill is law on the same day as in the world it was cut from.</item>
    /// </list>
    /// The random streams are restored after, the epoch by its scope, and every host destroyed.
    /// </summary>
    public static class UsDocketDiagnostic
    {
        private const string ConstitutionPage = "ElectionsData/usa/raw/records/archives_constitution_transcript.html";
        private const string KonstytucjaPage = "ElectionsData/poland/raw/records/trybunal_konstytucja.html";
        private static readonly string[] ConstitutionWords =
        {
            "If any Bill shall not be returned by the President within ten Days (Sundays excepted) after it shall have been presented to him, the Same shall be a Law, in like Manner as if he had signed it,",
            "unless the Congress by their Adjournment prevent its Return, in which Case it shall not be a Law.",
        };
        private const string KonstytucjaWords = "Prezydent Rzeczypospolitej podpisuje ustawę w ciągu 21 dni od dnia przedstawienia";
        private const int Seed = 777;
        /// <summary>US-21c (J6 (ii)): the law of record that became law unsigned - its slip law and its BILLSTATUS (`ElectionsData/usa/raw/unsigned/`).</summary>
        private const string UnsignedLaw = "ElectionsData/usa/raw/unsigned/govinfo_PLAW-119publ101.htm";
        private const string UnsignedStatus = "ElectionsData/usa/raw/unsigned/govinfo_BILLSTATUS-119hr6644.xml";
        /// <summary>[SOURCED] the slip law's note by the Office of the Federal Register, its whitespace closed up.</summary>
        private const string UnsignedNote = "having been presented to the President of the United States on Monday, June 29, 2026, and not having been returned by him to the House of Congress in which it originated within the time prescribed by the Constitution of the United States, has become law without his signature on July 11, 2026.";

        public static void Run()
        {
            CheckExit.ArmLogFold();
            var sb = new StringBuilder("=== UsDocketDiagnostic (PS-6 US-21b, US-21c): the player-president's docket - the ten days, Sundays excepted; an answer acts at once ===\n");
            int failures = 0;
            void Check(bool ok, string what) { if (!ok) { failures++; } sb.Append(ok ? "    ok        " : "    FAIL      ").Append(what).Append('\n'); }
            var hosts = new List<GameObject>();
            int seedWas = SimulationRandom.MasterSeed;
            Dictionary<SimulationRandom.Stream, int> drawsWas = SimulationRandom.CaptureDrawCounts();
            try
            {
                // ================================================================ (a) the words, held to their pages
                Check(PresidencyDiagnostic.Page(ConstitutionPage, out string constitution) && ConstitutionWords.All(constitution.Contains),
                    F("(a) Art. I §7 cl. 2 (archives_constitution_transcript.html) held to its SHA-256, holding \"{0}\" - the adjournment clause is the pocket veto, NOT MODELLED (out of scope by the ruling)", string.Join("\" ... \"", ConstitutionWords)));
                Check(PresidencyDiagnostic.Page(KonstytucjaPage, out string konstytucja) && konstytucja.Contains(KonstytucjaWords),
                    F("(a) Konstytucja Art. 122 ust. 2 (trybunal_konstytucja.html) held to its SHA-256, holding \"{0}\"", KonstytucjaWords));

                // ================================================================ (f) US-21c (J6 (ii)): the law's date against a law of record that became law unsigned
                bool slipHeld = PresidencyDiagnostic.Page(UnsignedLaw, out string slip);
                bool statusHeld = PresidencyDiagnostic.Page(UnsignedStatus, out string status);
                List<string> actionItems = statusHeld ? status.Split(new[] { "<item>" }, StringSplitOptions.None).ToList() : new List<string>();
                bool presentedOfRecord = actionItems.Any(i => i.Contains("<actionDate>2026-06-29</actionDate>") && i.Contains("Presented to President."));
                bool lawOfRecord = actionItems.Any(i => i.Contains("<actionDate>2026-07-11</actionDate>") && i.Contains("Became Public Law No: 119-101."));
                DateTime unsignedPresented = new DateTime(2026, 6, 29), unsignedLaw = new DateTime(2026, 7, 11);
                Check(slipHeld && Regex.Replace(slip, "\\s+", " ").Contains(UnsignedNote) && statusHeld && presentedOfRecord && lawOfRecord
                      && PresidentialDocket.LastDay(CountryId.USA, unsignedPresented) == new DateTime(2026, 7, 10) && PresidentialDocket.LawDay(CountryId.USA, unsignedPresented) == unsignedLaw
                      && SimulationManager.DocketLawDay(CountryId.USA, new DocketEntry { PresentedOn = unsignedPresented }) == unsignedLaw,
                    F("(f) J6 (ii), the law's date SOURCED: P.L. 119-101 (H.R. 6644), its slip law held to its SHA-256 - \"{0}\" - and its BILLSTATUS, \"Presented to President.\" on 2026-06-29 and \"Became Public Law No: 119-101.\" on 2026-07-11, each an action item of its own; the rule gives the last day {1} (Tue 30 Jun day 1, Sunday 5 Jul not counted, Fri 10 Jul day 10) and the law day {2} - THE RECORD'S DATE",
                        UnsignedNote, Day(PresidentialDocket.LastDay(CountryId.USA, unsignedPresented)), Day(PresidentialDocket.LawDay(CountryId.USA, unsignedPresented))));

                // ================================================================ (b) the period's arithmetic, DERIVED by hand
                var cases = new (CountryId Country, DateTime Presented, DateTime Last, string Why)[]
                {
                    (CountryId.USA, new DateTime(2025, 2, 3), new DateTime(2025, 2, 14), "presented Monday 3 Feb 2025: Tue 4 is day 1, Sat 8 day 5, Sunday 9 not counted, Fri 14 day 10"),
                    (CountryId.USA, new DateTime(2025, 2, 8), new DateTime(2025, 2, 20), "presented Saturday 8 Feb: Sundays 9 and 16 not counted, Thu 20 day 10"),
                    (CountryId.USA, new DateTime(2025, 2, 24), new DateTime(2025, 3, 7), "presented Monday 24 Feb: Sunday 2 Mar not counted, Fri 7 Mar day 10"),
                    (CountryId.Poland, new DateTime(2025, 2, 3), new DateTime(2025, 2, 24), "presented 3 Feb 2025: 21 calendar days from the day after, Mon 24 Feb (Sundays counted)"),
                };
                foreach ((CountryId country, DateTime presented, DateTime last, string why) in cases)
                {
                    Check(PresidentialDocket.LastDay(country, presented) == last && !PresidentialDocket.Expired(country, presented, last) && PresidentialDocket.Expired(country, presented, last.AddDays(1))
                          && !PresidentialDocket.Expired(country, presented, presented),
                        F("(b) {0}: the last day to answer {1} ({2}); still held on it, law without signature from {3}", country, Day(PresidentialDocket.LastDay(country, presented)), why, Day(last.AddDays(1))));
                }
                bool noneElse = Enum.GetValues(typeof(CountryId)).Cast<CountryId>().All(c => PresidentialDocket.TryPeriodOf(c, out _, out _) == (c == CountryId.USA || c == CountryId.Poland)
                                                                                            && (c == CountryId.USA || c == CountryId.Poland || PresidentialDocket.LastDay(c, new DateTime(2025, 2, 3)) == DateTime.MinValue));
                bool usaAlone = Enum.GetValues(typeof(CountryId)).Cast<CountryId>().All(c => UsVeto.Applies(c) == (c == CountryId.USA));
                Check(noneElse && usaAlone && PresidentialDocket.TryPeriodOf(CountryId.Poland, out int plDays, out bool plSundays) && plDays == 21 && !plSundays,
                    "(b) a period in the USA and Poland alone (ten days with Sundays excepted; 21 days, DECLARED for Poland's player-president), none elsewhere; the docket's one holder, the US veto gate, acts in the USA alone - NO POLISH PATH REACHES THE DOCKET YET (Poland has no player-president until SP-7b; its president answers by its own gate on the day)");

                using (SimulationManager.EpochScope())
                {
                    try
                    {
                        // ============================================================ (c) the player-president's docket
                        (SimulationManager s, World w, Country u) = PlayerPresidentWorld(hosts);
                        bool playerPresident = Congress.PresidentOn(u, s.CurrentDate, out string shownAs, out string pp, out bool isPlayer) && isPlayer && pp == "REP";
                        Check(playerPresident && s.PlayerPresidentVetoes == null, F("(c) a planted game-elected Trump sworn 2025-01-20, the player REP: the president of the day ({0}, {1}) is the player's - the player-president; no answer on the day is set (play's default)", shownAs, pp));
                        List<TaxType> taxes = Candidates(u, 4);
                        Check(taxes.Count == 4, F("(c) four levied taxes whose REP removal both chambers pass on the blocs (reconciliation; nothing drawn on a budget vote): {0}", string.Join(", ", taxes)));
                        if (taxes.Count == 4)
                        {
                            TaxType unanswered = taxes[0], signed = taxes[1], vetoed = taxes[2];
                            TaxLine rateLine = u.TaxLines.First(t => t.Type == taxes[3]);
                            float rateWas = rateLine.Rate, rateTo = Mathf.Clamp(rateLine.Rate - 2f, rateLine.MinRate, rateLine.MaxRate);
                            var rates = new BudgetBill();
                            rates.TaxLines[rateLine.Type] = rateTo;
                            bool introduced = s.IntroduceTaxProgramBill(CountryId.USA, unanswered, false) && s.IntroduceTaxProgramBill(CountryId.USA, signed, false) && s.IntroduceTaxProgramBill(CountryId.USA, vetoed, false)
                                              && BudgetBillHarness.Introduce(s, CountryId.USA, rates);   // AF-9: off the budget calendar, so the window is planted
                            DateTime fundedWas = u.AppropriatedThrough;
                            Step(s, w, PresentmentDay);
                            List<SimulationManager.DocketItem> docket = s.GetDocket(CountryId.USA);
                            List<SimulationManager.DocketItem> acts = docket.Where(i => i.Entry.Act >= 0).ToList(), billsHeld = docket.Where(i => i.Entry.Act < 0).ToList();
                            bool heldShape = introduced && docket.Count == 5 && acts.Count == 2 && billsHeld.Count == 3 && docket.All(i => i.PresentedOn == PresentmentDay && i.Answer == 0 && i.Entry.Passage?.OtherChamber != null && i.Entry.Passage.Passed && i.Entry.Passage.OtherChamber.Passed)
                                             && Implemented(u, unanswered) && Implemented(u, signed) && Implemented(u, vetoed) && rateLine.Rate == rateWas && u.AppropriatedThrough == fundedWas
                                             && s.GetPendingTaxProgramBills(CountryId.USA).Count() == 3 && s.GetPendingBudgetBill(CountryId.USA)?.ActsHeld?.Count == 2;
                            Check(heldShape, F("(c) on {0} both chambers pass the three tax bills and the budget's appropriation and reconciliation; all five are PRESENTED AND HELD on the docket ({1}) - nothing applied: the taxes levied, the rate {2}, the horizon {3}, the bills in their slots",
                                Day(PresentmentDay), string.Join("; ", docket.Select(i => i.Title)), Num(rateLine.Rate), Day(u.AppropriatedThrough)));
                            SimulationManager.DocketItem first = docket.FirstOrDefault(i => i.Entry.Act < 0);
                            int sundays = 0;
                            for (DateTime d = PresentmentDay.AddDays(1); d <= first.LastDay; d = d.AddDays(1)) { if (d.DayOfWeek == DayOfWeek.Sunday) { sundays++; } }
                            Check(first.Entry != null && first.LastDay == new DateTime(2025, 3, 7) && sundays == 1 && (first.LastDay - PresentmentDay).Days == 10 + sundays && first.LastDay.DayOfWeek != DayOfWeek.Sunday,
                                F("(c) the last day to answer, as the docket reads it: {0} - ten days counted from {1}, {2} Sunday inside the span not counted ({3} calendar days)", Day(first.LastDay), Day(PresentmentDay.AddDays(1)), sundays, first.Entry == null ? -1 : (first.LastDay - PresentmentDay).Days));

                            // the save, cut mid-docket (e) - taken here, before any answer
                            Step(s, w, new DateTime(2025, 2, 25));
                            string json = SaveGameService.Serialize(SaveGameService.CreateSaveGame(s, w, CountryId.USA, null));

                            // the verbs: sign one, veto one, on 2025-02-26, between that day's step and the next - US-21c (J6 (i)): each acts at once
                            Step(s, w, AnswerDay);
                            DivisionRecord signedPassage = PassageOf(s, signed), vetoedPassage = PassageOf(s, vetoed);
                            int seen = Last(u);
                            u.BillLostOn = DateTime.MinValue;
                            float debtBefore = u.State.GovernmentDebt, outputBefore = u.State.NominalGdp;
                            bool signs = s.SignDocketBill(CountryId.USA, signedPassage, out string signWhy);
                            bool signedAtOnce = !Implemented(u, signed) && PassageOf(s, signed) == null;
                            bool vetoes = s.VetoDocketBill(CountryId.USA, vetoedPassage, out string vetoWhy);
                            bool noMoneyMoved = u.State.GovernmentDebt == debtBefore && u.State.NominalGdp == outputBefore;
                            List<DivisionRecord> onAnswer = u.Divisions.Entries.Where(r => r.Number > seen).ToList();
                            DivisionRecord sustained = onAnswer.FirstOrDefault(r => r.Title.StartsWith("Vetoed by the President (you, ", StringComparison.Ordinal));
                            bool again = s.SignDocketBill(CountryId.USA, signedPassage, out string againWhy);
                            bool stranger = s.SignDocketBill(CountryId.USA, new DivisionRecord { Title = "not on the docket" }, out string strangerWhy);
                            Check(signs && vetoes && !again && againWhy == "that bill is not on your docket" && !stranger && strangerWhy == "that bill is not on your docket" && s.GetDocket(CountryId.USA).Count == 3,
                                F("(c) on {0} the player-president signs \"{1}\" and vetoes \"{2}\" through the manager's verbs ({3}; {4}); a second answer is refused - the bill is no longer on the docket (\"{5}\") - and so is a bill never on it (\"{6}\"); the unanswered bill and the budget's two acts still held",
                                    Day(AnswerDay), signedPassage?.Title, vetoedPassage?.Title, signWhy ?? "accepted", vetoWhy ?? "accepted", againWhy, strangerWhy));
                            Check(signedAtOnce && noMoneyMoved && !onAnswer.Any(r => r.Title.StartsWith("Vetoed", StringComparison.Ordinal) && r.Title.Contains(": Remove " + signed + " - ")),
                                F("(c) SIGNED, AT ONCE (J6 (i)): on {0}, the answer's day, \"Remove {1}\" is law the moment it is signed - the tax no longer levied, the bill out of its slot, no vote on a veto written; the answer moves no money itself (the debt {2} and nominal output {3} bit for bit as before it), so the law's money flows from the next day step, as every law's does",
                                    Day(AnswerDay), signed, Num(u.State.GovernmentDebt), Num(u.State.NominalGdp)));
                            Check(sustained != null && !sustained.Passed && sustained.Required > 0 && !sustained.Cloture && sustained.Chamber == Congress.House && sustained.Title.Contains(": Remove " + vetoed + " - ") && sustained.Title.Contains("the veto stands")
                                  && sustained.Date == AnswerDay && Implemented(u, vetoed) && PassageOf(s, vetoed) == null && u.BillLostOn == AnswerDay,
                                F("(c) VETOED, AT ONCE (J6 (i)): on {0}, the answer's day, the veto goes to the override, the chamber the bill began in first - \"{1}\"; the bill falls (the tax still levied), and as his own party's bill the day is the government's loss (Congress.RecordLost)",
                                    Day(AnswerDay), sustained?.Title));
                            int afterAnswers = Last(u);
                            Step(s, w, AnswerDay.AddDays(1));
                            Check(!u.Divisions.Entries.Where(r => r.Number > afterAnswers).Any(r => r.Title.Contains(": Remove " + signed + " - ") || r.Title.Contains(": Remove " + vetoed + " - ")) && !Implemented(u, signed) && Implemented(u, vetoed),
                                F("(c) the next day step, {0}, acts on neither again - nothing written for either bill", Day(AnswerDay.AddDays(1))));

                            // unanswered: held on its last day, law the day after
                            Step(s, w, new DateTime(2025, 3, 7));
                            bool heldOnLast = Implemented(u, unanswered) && PassageOf(s, unanswered) != null && rateLine.Rate == rateWas && s.GetPendingBudgetBill(CountryId.USA)?.ActsHeld?.Count == 2 && u.AppropriatedThrough == fundedWas;
                            Step(s, w, new DateTime(2025, 3, 8));
                            Check(heldOnLast && !Implemented(u, unanswered) && PassageOf(s, unanswered) == null && s.GetDocket(CountryId.USA).Count == 0,
                                F("(c) UNANSWERED: \"Remove {0}\" is still held on 2025-03-07, the tenth day counted, and is LAW WITHOUT SIGNATURE on 2025-03-08 - the docket empty", unanswered));
                            Check(heldOnLast && Mathf.Approximately(rateLine.Rate, rateTo) && s.GetPendingBudgetBill(CountryId.USA) == null && u.AppropriatedThrough == UsBudgetProcedure.FundedThrough(fundedWas, new DateTime(2025, 3, 8)),
                                F("(c) the budget's acts, unanswered: held through 2025-03-07, both law on 2025-03-08 and applied once - {0} from {1} to {2}; the appropriation funds the government through {3}; the slot empty",
                                    rateLine.Type, Num(rateWas), Num(rateLine.Rate), Day(u.AppropriatedThrough)));

                            // the review of 0c669fe6, M1: an answer given on the eve of a change of presidency stands - US-21c (J6 (i)): law that very day, before the oath
                            bool reIntroduced = s.IntroduceTaxProgramBill(CountryId.USA, vetoed, false);
                            DateTime evePresented = new DateTime(2025, 3, 8).AddDays(ParliamentSystem.BillDurationDays), eve = evePresented.AddDays(1), oath = eve.AddDays(1);
                            Step(s, w, eve);
                            DivisionRecord evePassage = PassageOf(s, vetoed);
                            bool signedOnEve = s.SignDocketBill(CountryId.USA, evePassage, out string eveWhy);
                            bool lawOnEve = !Implemented(u, vetoed) && PassageOf(s, vetoed) == null;
                            var harris = new UsPresidentialElection.Contest { PollingDay = new DateTime(2024, 11, 5), TakesOffice = oath, Winner = "Kamala D. Harris", WinnerParty = "DEM", ElectorsAppointed = 538, Line = "planted (UsDocketDiagnostic, the eve of a change of presidency)" };
                            harris.Electors.Add(new UsPresidentialElection.Tally { Candidate = "Kamala D. Harris", Party = "DEM", Votes = 270 });
                            harris.Electors.Add(new UsPresidentialElection.Tally { Candidate = "Donald J. Trump", Party = "REP", Votes = 268 });
                            u.UsPresidentialElections.Add(harris);
                            int eveSeen = Last(u);
                            Step(s, w, oath);
                            bool successor = Congress.PresidentOn(u, oath, out string successorName, out string successorParty, out bool successorPlayer) && !successorPlayer && successorParty == "DEM";
                            DivisionRecord eveHouse = evePassage?.Chamber == Congress.House ? evePassage : evePassage?.OtherChamber;
                            int demSeats = eveHouse?.Sides.Where(x => x.Abbrev == "DEM").Sum(x => x.Seats) ?? 0, demNays = eveHouse?.Sides.Where(x => x.Abbrev == "DEM" && x.Side < 0).Sum(x => x.Seats) ?? 0;
                            Check(reIntroduced && evePassage != null && signedOnEve && lawOnEve && successor && UsVeto.NamedByA(demSeats, demNays) && !Implemented(u, vetoed) && PassageOf(s, vetoed) == null
                                  && !u.Divisions.Entries.Where(r => r.Number > eveSeen).Any(r => r.Title.StartsWith("Vetoed", StringComparison.Ordinal)),
                                F("(c) AN ANSWER STANDS ACROSS A CHANGE OF PRESIDENCY: \"Remove {0}\" presented {1}, signed {2} by the player-president ({3}) - law THAT DAY (J6 (i)), so on {4}, the day {5} ({6}) takes office - whose rule (a) would veto it ({7} of DEM's {8} House members against) - nothing is left to decide and no veto is written",
                                    vetoed, Day(evePresented), Day(eve), eveWhy ?? "accepted", Day(oath), successorName, successorParty, demNays, demSeats));

                            // ======================================================== (e) the save, loaded into the game
                            SaveGame cut = SaveGameService.Deserialize(json);
                            Country savedUsa = cut.World.GetCountry(CountryId.USA);
                            List<TaxProgramBill> savedBills = cut.Sim.PendingTaxProgramBills.TryGetValue(CountryId.USA, out var savedTaxes) ? savedTaxes.Values.ToList() : new List<TaxProgramBill>();
                            BudgetBill savedBudget = cut.Sim.PendingBudgetBills.TryGetValue(CountryId.USA, out BudgetBill sbb) ? sbb : null;
                            bool byReference = savedBills.All(b => b.Docket?.Passage != null && savedUsa.Divisions.Entries.Any(e => ReferenceEquals(e, b.Docket.Passage)));
                            Check(cut.SaveVersion == SaveGameService.CurrentSaveVersion && savedBills.Count == 3 && savedBills.All(b => b.Docket != null && b.Docket.PresentedOn == PresentmentDay && b.Docket.Answer == 0 && b.Docket.Passage?.OtherChamber != null)
                                  && savedBudget?.ActsHeld?.Count == 2 && savedBudget.ActsHeld.All(e => e.Act >= 0 && e.Passage != null) && savedBudget.ActsLaw != null && savedBudget.ActsFallen != null,
                                F("(e) a save cut on 2025-02-25, mid-docket, at format {0} (unchanged - the fields are additive, null on every bill a save from before carries): the three bills and the budget's two acts held, each with its passage and its presentment day ({1}); the passages {2}",
                                    cut.SaveVersion, Day(PresentmentDay), byReference ? "by reference to the division log's entries" : "carried whole"));
                            (SimulationManager adopted, PoliSim.UI.GameController _) = PresidencyDiagnostic.Adopt(cut, hosts, out string error);
                            if (adopted == null) { Check(false, "(e) the save loaded into the game: " + error); }
                            else
                            {
                                Country la = adopted.World.GetCountry(CountryId.USA);
                                TaxLine loadedRate = la.TaxLines.First(t => t.Type == rateLine.Type);
                                int loadedHeld = adopted.GetDocket(CountryId.USA).Count;
                                Step(adopted, adopted.World, new DateTime(2025, 3, 7));
                                bool stillHeld = adopted.GetDocket(CountryId.USA).Count == 5 && Implemented(la, unanswered) && Implemented(la, signed) && Implemented(la, vetoed) && loadedRate.Rate == rateWas;
                                Step(adopted, adopted.World, new DateTime(2025, 3, 8));
                                Check(loadedHeld == 5 && stillHeld && adopted.GetDocket(CountryId.USA).Count == 0 && !Implemented(la, unanswered) && !Implemented(la, signed) && !Implemented(la, vetoed)
                                      && Mathf.Approximately(loadedRate.Rate, rateTo) && la.AppropriatedThrough == u.AppropriatedThrough,
                                    F("(e) loaded INTO THE GAME (the controller's RestoreFromSave): the docket holds the five as saved; stepped with no answer, all five are still held on 2025-03-07 and law on 2025-03-08 - the day the world it was cut from made its unanswered bill law; funded through {0}", Day(la.AppropriatedThrough)));
                            }
                        }

                        // ============================================================ (d) the AI president unchanged
                        (SimulationManager sa, World wa, Country ua) = UsCongressDiagnostic.OpenUsa(hosts, UsCongressDiagnostic.Day119, "REP");
                        bool ai = Congress.PresidentOn(ua, sa.CurrentDate, out string ofRecord, out string aiParty, out bool aiPlayer) && !aiPlayer && aiParty == "REP";
                        List<TaxType> aiTaxes = Candidates(ua, 1);
                        if (aiTaxes.Count == 1)
                        {
                            bool aiIntroduced = sa.IntroduceTaxProgramBill(CountryId.USA, aiTaxes[0], false);
                            int aiSeen = Last(ua);
                            Step(sa, wa, PresentmentDay);
                            List<DivisionRecord> aiWritten = ua.Divisions.Entries.Where(r => r.Number > aiSeen).ToList();
                            bool refused = !sa.SignDocketBill(CountryId.USA, aiWritten.LastOrDefault(), out string aiWhy);
                            Check(ai && aiIntroduced && !Implemented(ua, aiTaxes[0]) && sa.GetDocket(CountryId.USA).Count == 0 && !sa.GetPendingTaxProgramBills(CountryId.USA).Any() && !aiWritten.Any(r => r.Title.StartsWith("Vetoed", StringComparison.Ordinal))
                                  && refused && aiWhy == "you do not hold the presidency today",
                                F("(d) the AI president unchanged: under {0} of record ({1}, not the player's), the REP player's \"Remove {2}\" passes both chambers on {3} and is signed by rule (a) THAT DAY - law, nothing held; the verbs refuse a player who does not hold the presidency (\"{4}\")",
                                    ofRecord, aiParty, aiTaxes[0], Day(PresentmentDay), aiWhy));
                        }
                        else { Check(false, "(d) a levied tax whose REP removal both chambers pass under Trump of record"); }
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

            sb.Append(failures == 0 ? "=== UsDocketDiagnostic: the docket holds - unanswered law after the ten days (Sundays excepted) on the date P.L. 119-101 bears, signed and vetoed to the override at once, the AI president unchanged, the save mid-docket ===\n"
                                    : F("=== UsDocketDiagnostic: {0} FAILURE(S) ===\n", failures));
            if (failures == 0) { Debug.Log(sb.ToString()); CheckExit.Finish(0); }
            else { Debug.LogError(sb.ToString()); CheckExit.Finish(1); }
        }

        /// <summary>The day the bills introduced on 2025-02-03 are voted and presented: `ParliamentSystem.BillDurationDays` later.</summary>
        private static DateTime PresentmentDay => UsCongressDiagnostic.Day119.AddDays(ParliamentSystem.BillDurationDays);
        private static DateTime AnswerDay => PresentmentDay.AddDays(2);

        /// <summary>The 119th of record on 2025-02-03, the player REP, with a planted game-elected Trump sworn 2025-01-20 (his presidency installed as the oath
        /// installs it) - so the president of the day is the player's: the player-president.</summary>
        private static (SimulationManager, World, Country) PlayerPresidentWorld(List<GameObject> hosts)
        {
            (SimulationManager s, World w, Country u) = UsCongressDiagnostic.OpenUsa(hosts, UsCongressDiagnostic.Day119, "REP");
            var trump = new UsPresidentialElection.Contest { PollingDay = new DateTime(2024, 11, 5), TakesOffice = new DateTime(2025, 1, 20), Winner = "Donald J. Trump", WinnerParty = "REP", ElectorsAppointed = 538, Line = "planted (UsDocketDiagnostic)" };
            trump.Electors.Add(new UsPresidentialElection.Tally { Candidate = "Donald J. Trump", Party = "REP", Votes = 312 });
            trump.Electors.Add(new UsPresidentialElection.Tally { Candidate = "Kamala D. Harris", Party = "DEM", Votes = 226 });
            u.UsPresidentialElections.Add(trump);
            u.Government = UsPresidentialElection.GovernmentOf(trump, trump.TakesOffice);
            return (s, w, u);
        }

        /// <summary>Up to <paramref name="n"/> levied taxes whose removal by REP both chambers pass on the blocs' projection (`Congress.Project` - a budget vote
        /// draws nothing, so the projection is the vote).</summary>
        private static List<TaxType> Candidates(Country u, int n)
        {
            var found = new List<TaxType>();
            foreach (TaxLine line in u.TaxLines.Where(t => t.IsImplemented))
            {
                BillConcern concern = ParliamentSystem.GetTaxProgramBillConcern(u, new TaxProgramBill { Type = line.Type, IsAdd = false });
                if (Congress.Project(u, concern, PresentmentDay, Congress.OriginOf(u, concern, PresentmentDay)).Passed) { found.Add(line.Type); }
                if (found.Count == n) { break; }
            }
            return found;
        }

        private static bool Implemented(Country u, TaxType type) => u.TaxLines.First(t => t.Type == type).IsImplemented;

        private static DivisionRecord PassageOf(SimulationManager s, TaxType type) =>
            s.GetPendingTaxProgramBills(CountryId.USA).FirstOrDefault(b => b.Type == type)?.Docket?.Passage;

        /// <summary>Steps <paramref name="s"/> to <paramref name="until"/> in the controller's order (the day, the player's day tick, the boundary's turn).</summary>
        private static void Step(SimulationManager s, World w, DateTime until)
        {
            while (s.CurrentDate < until)
            {
                bool boundary = s.AdvanceDay();
                s.AdvanceCountryDayTick(CountryId.USA);
                if (boundary)
                {
                    var none = new Dictionary<CountryId, PolicyDecision>();
                    foreach (Country c in w.Countries) { none[c.Id] = PolicyDecision.None(); }
                    s.AdvanceTurn(none);
                }
            }
        }

        private static int Last(Country u) => u.Divisions.Entries.Count > 0 ? u.Divisions.Entries.Last().Number : 0;
        private static string Num(float v) => v.ToString("0.###", CultureInfo.InvariantCulture);
        private static string Day(DateTime d) => d == DateTime.MinValue ? "none" : d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        private static string F(string format, params object[] args) => string.Format(CultureInfo.InvariantCulture, format, args);
    }
}
