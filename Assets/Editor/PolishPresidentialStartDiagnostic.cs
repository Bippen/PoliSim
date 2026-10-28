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
    /// SP-7b (`docs/specs/POLAND_PRESIDENTIAL_START_PLAN.md` §1.3, §5a, §6; Elias's rulings I14, installed §857, and J6 (iii), installed §870): THE PRESIDENTIAL
    /// START DIAGNOSTIC - the done-when's: a fresh presidential-start world taken through 18 May and 1 June 2025 holds both rounds once with the player's
    /// candidate in the field; a save across the rounds and one across the oath load INTO THE GAME (§557); both outcomes forced and stepped past 6 Aug 2025.
    /// Every world is opened as a new game opens it (`SimulationManager.SetEpoch` on the start's day before the world exists, the candidate seated through
    /// `PresidentialStart.Seat` - the controller's own call - and the new game's average cabinet) and stepped in the controller's order (the day, the player's
    /// day tick, the boundary's turn).
    /// <list type="bullet">
    /// <item>(a) the words, held to their page by SHA-256 (`trybunal_konstytucja.html`, [TK-KONST]): Art. 122 ust. 2's and Art. 235 ust. 7's 21 days, Art. 224
    /// ust. 1's and Art. 123 ust. 3's 7, Art. 127 ust. 2's one re-election, Art. 132 (the reading the office's hand-over rests on) and Art. 144 ust. 3's
    /// signing without countersignature;</item>
    /// <item>(b) J6 (iii)'s terms, DERIVED by hand (`PresidentialDocket`): an ordinary statute presented 10 Aug 2025 answered by 31 Aug and signed on 1 Sep; the
    /// budget act presented 19 Aug answered by 26 Aug; an urgent bill 7 days, an amendment 21; the USA's ten days whatever the kind;</item>
    /// <item>(c) THE WIN (world W, the player Trzaskowski of KO, the prime minister's party): the start's day seats the 10th Sejm, Tusk's KO+TD+NL government and
    /// Duda; one contest on 18 May with the player's candidate marked and in the field - identical, share for share, to the same world with no candidate
    /// seated (control C: the mark changes no share, the factor of record stands); the run-off on 1 June; the win forced where the model did not give it
    /// (PLANTED, said so); on the eve of the oath the party governs, from 6 Aug the player is President - the role gate refuses in the office's words, and so
    /// do the party's own verbs (leaving the government, withdrawing support, moving no confidence: the review's M2); the party's chairs and Finance dial are the AI's; four statutes the Sejm passes on 10 Aug are HELD on the docket to 31 Aug, nothing applied; signed, one
    /// is law at once (J6 (i)'s rule, read for Poland); vetoed, the Sejm votes on the veto at 3/5 the same day (the passage marked a motion); unanswered, the third is held on 31 Aug and signed on 1
    /// Sep; the budget adopted 19 Aug goes to the docket - the budget act at 7 days and refusing a veto, the tax act at 21 - and applies once, the day its
    /// last act is decided; the fourth, signed while the budget is held, is law at once and moves neither the budget's held acts nor a bill still counting down;</item>
    /// <item>(d) THE SAVE ACROSS THE ROUNDS (cut 25 May, loaded into the game): the adopted world holds the run-off on 1 June exactly as W did; THE LOSS forced on
    /// it (PLANTED where the model gave the win): past 6 Aug the player holds no office, the party governs on (L1), nothing is held, and no later contest is
    /// the player's;</item>
    /// <item>(d2) THE WINDOW AT THE OATH (the review's M3; the same save loaded again, the win forced): the arrival window open and unused on the eve is closed on
    /// the oath's day, and on the next request day (1 Jan 2026) the AI government tables its budget;</item>
    /// <item>(e) THE SAVE ACROSS THE OATH (cut 12 Aug, mid-docket, this build's format): the candidate, the start's key, the office and the held statutes ride it; loaded
    /// into the game the player is President, the docket as cut; a held statute is STILL ANSWERABLE (the review's M1) - vetoed after the load through the Docket
    /// page's answer (the controller's AnswerDocketItem), the Sejm votes on the veto at once; stepped with no other answer, every other held statute is
    /// signed on 1 Sep - while the budget, voted on 19 Aug in that world too, still waits on its tax act's 21 days.</item>
    /// </list>
    /// The random streams are restored after, the epoch by its scope, and every host destroyed. Not in the bar's tiers by itself: registered by the integrator.
    /// </summary>
    public static class PolishPresidentialStartDiagnostic
    {
        private const string KonstytucjaPage = "ElectionsData/poland/raw/records/trybunal_konstytucja.html";
        private static readonly string[] KonstytucjaWords =
        {
            "Prezydent Rzeczypospolitej podpisuje ustawę w ciągu 21 dni od dnia przedstawienia",   // Art. 122 ust. 2 and Art. 235 ust. 7
            "podpisuje w ciągu 7 dni ustawę budżetową",                                             // Art. 224 ust. 1
            "termin podpisania ustawy przez Prezydenta Rzeczypospolitej wynosi 7 dni",              // Art. 123 ust. 3
            "może być ponownie wybrany tylko raz",                                                  // Art. 127 ust. 2
            "nie może piastować żadnego innego urzędu",                                             // Art. 132
            "podpisywania albo odmowy podpisania ustawy",                                           // Art. 144 ust. 3 pkt 6
        };
        private const int Seed = 777;
        private static readonly DateTime FirstVote = new DateTime(2025, 5, 18), RunOff = new DateTime(2025, 6, 1), Oath = new DateTime(2025, 8, 6);
        private static readonly DateTime CutRounds = new DateTime(2025, 5, 25), BillsIn = new DateTime(2025, 7, 20), BudgetIn = new DateTime(2025, 7, 29);
        private static readonly DateTime Presented = BillsIn.AddDays(ParliamentSystem.BillDurationDays), BudgetPresented = BudgetIn.AddDays(ParliamentSystem.BillDurationDays);
        private static readonly DateTime CutOath = new DateTime(2025, 8, 12);

        public static void Run()
        {
            CheckExit.ArmLogFold();
            var sb = new StringBuilder("=== PolishPresidentialStartDiagnostic (SP-7b): Poland's presidential start - the candidate, the rounds, the office, the docket's terms ===\n");
            int failures = 0;
            void Check(bool ok, string what) { if (!ok) { failures++; } sb.Append(ok ? "    ok        " : "    FAIL      ").Append(what).Append('\n'); }
            var hosts = new List<GameObject>();
            int seedWas = SimulationRandom.MasterSeed;
            Dictionary<SimulationRandom.Stream, int> drawsWas = SimulationRandom.CaptureDrawCounts();
            try
            {
                // ================================================================ (a) the words
                Check(PresidencyDiagnostic.Page(KonstytucjaPage, out string konstytucja) && KonstytucjaWords.All(konstytucja.Contains),
                    F("(a) the Konstytucja (trybunal_konstytucja.html) held to its SHA-256, holding \"{0}\"", string.Join("\" · \"", KonstytucjaWords)));

                // ================================================================ (b) J6 (iii)'s terms, DERIVED by hand
                Check(PresidentialDocket.LastDay(CountryId.Poland, Presented, PresidentialDocket.Bill.Ordinary) == new DateTime(2025, 8, 31)
                      && PresidentialDocket.LawDay(CountryId.Poland, Presented, PresidentialDocket.Bill.Ordinary) == new DateTime(2025, 9, 1)
                      && PresidentialDocket.LastDay(CountryId.Poland, Presented, PresidentialDocket.Bill.ConstitutionalAmendment) == new DateTime(2025, 8, 31)
                      && PresidentialDocket.LastDay(CountryId.Poland, BudgetPresented, PresidentialDocket.Bill.BudgetAct) == new DateTime(2025, 8, 26)
                      && PresidentialDocket.LastDay(CountryId.Poland, BudgetPresented, PresidentialDocket.Bill.Urgent) == new DateTime(2025, 8, 26)
                      && PresidentialDocket.LastDay(CountryId.Poland, Presented) == PresidentialDocket.LastDay(CountryId.Poland, Presented, PresidentialDocket.Bill.Ordinary)
                      && PresidentialDocket.LastDay(CountryId.USA, new DateTime(2025, 2, 3), PresidentialDocket.Bill.BudgetAct) == new DateTime(2025, 2, 14)
                      && !PresidentialDocket.TryPeriodOf(CountryId.Sweden, PresidentialDocket.Bill.BudgetAct, out _, out _),
                    F("(b) J6 (iii): an ordinary statute presented {0} answered by 2025-08-31 (21 days from the day after, Art. 122 ust. 2), signed unanswered on 2025-09-01; an amendment the same (Art. 235 ust. 7); the budget act presented {1} by 2025-08-26 (7, Art. 224 ust. 1) and an urgent bill likewise (Art. 123 ust. 3); the USA's ten days whatever the kind; no term elsewhere",
                        Day(Presented), Day(BudgetPresented)));

                StartPoints.StartPoint start = StartPoints.For(CountryId.Poland)[1];
                string trzaskowski = PresidencyOfRecord.NameOfRecord(CountryId.Poland, 2025, "TRZASKOWSKI");
                using (SimulationManager.EpochScope())
                {
                    try
                    {
                        // ============================================================ (c) the start, the rounds, the control
                        (SimulationManager s, World w, Country pl) = Open(hosts, start, trzaskowski);
                        Check(pl.PlayerCandidate == trzaskowski && pl.PlayerPartyAbbrev == "KO" && pl.PlayerStart == "PRESIDENTIAL ELECTION 2025" && s.CurrentDate == PresidentialStart.PolandOpens
                              && pl.Government?.PmParty == "KO" && string.Join("+", pl.Government.Cabinet) == "KO+TD+NL" && s.PlayerGoverns(pl) && !pl.PlayerHoldsPresidency
                              && PresidencyOfRecord.TryAt(CountryId.Poland, s.CurrentDate, out PresidencyOfRecord.President duda) && duda.Name == "Andrzej Duda"
                              && WorldClock.SeatedVintage(CountryId.Poland, s.CurrentDate) == ElectionVintage.Poland2023,
                            F("(c) the start's day {0}: the player stands as {1}, the party {2} - the prime minister's in Tusk's {3} government; Duda the president of record; the 10th Sejm seated; the key \"{4}\"",
                                Day(s.CurrentDate), pl.PlayerCandidate, pl.PlayerPartyAbbrev, string.Join("+", pl.Government?.Cabinet ?? new List<string>()), pl.PlayerStart));
                        Check(!PresidentialStart.Seat(pl, start, PresidencyOfRecord.NameOfRecord(CountryId.Poland, 2025, "BRAUN"), out string braunWhy) && braunWhy == PresidentialStart.UnbackedReason && pl.PlayerCandidate == trzaskowski,
                            F("(c) a candidate no Sejm party backs is not seated - \"{0}\"", braunWhy));

                        Step(s, w, FirstVote.AddDays(-1));
                        Dictionary<string, double> eveSpent = FirstRoundSpentOnTheEve(s);   // N1: the finished first-round campaign's own figures, by party name
                        Step(s, w, FirstVote);
                        PresidentialElection.Contest held = pl.PresidentialElections.Count == 1 ? pl.PresidentialElections[0] : null;
                        Check(held != null && held.FirstVote == FirstVote && held.PlayerCandidate == trzaskowski && held.PlayerStood() && PresidentialElection.PlayerContestOn(pl, FirstVote) == held,
                            F("(c) one contest on {0}, the player's candidate marked and in the field: {1}", Day(FirstVote), held?.Line));

                        // PL-C: both worlds campaign now (LiveCampaignSetup.TryPoland), and the control's opening reseeds the process's streams - the player's world's
                        // streams are kept across it and put back, so its run-off campaign's draws stay contiguous and the save across the rounds below replays it
                        SimulationRandom.LiveState playerStreams = SimulationRandom.CaptureLive();
                        (SimulationManager sc, World wc, Country plc) = Open(hosts, start, null);
                        Step(sc, wc, FirstVote);
                        PresidentialElection.Contest control = plc.PresidentialElections.Count == 1 ? plc.PresidentialElections[0] : null;
                        SimulationRandom.Reinstate(playerStreams);

                        // PL-C (the review's M3): THE GAME'S OWN PRESIDENTIAL DAY LOOP - the first vote read the player's campaign, the game wrote each committee's
                        // first-round spending on the contest, and the run-off's campaign opened tonight (L7) with the two finalists' chests the one limit less it
                        string[] cast = LiveCampaignSetup.Keys(CountryId.Poland);
                        bool spentWritten = held?.FirstRoundSpent != null && held.FirstRoundSpent.Count == cast.Length && held.FirstRoundSpent.TryGetValue("KO", out double koSpent)
                                            && koSpent > 0.0 && koSpent <= LiveCampaignSetup.WarChest && held.FirstRoundSpent.Values.All(v => v >= 0.0 && v <= LiveCampaignSetup.WarChest);
                        bool pairedByName = eveSpent != null && held?.FirstRoundSpent != null && eveSpent.Count == held.FirstRoundSpent.Count
                                            && eveSpent.All(kv => held.FirstRoundSpent.TryGetValue(kv.Key, out double written) && Math.Abs(written - kv.Value) < 1e-9);
                        Check(pairedByName, F("(c) PL-C (N1): the spending the game wrote is the finished first-round campaign's own, party by name ({0} on the eve: {1})",
                            eveSpent == null ? 0 : eveSpent.Count, eveSpent == null ? "no finished campaign on the eve" : string.Join(", ", eveSpent.Where(kv => kv.Value > 0.0).Select(kv => F("{0} {1:N0}", kv.Key, kv.Value)))));
                        Check(spentWritten, F("(c) PL-C: the first vote read the game's campaign - the game wrote every committee's first-round spending on the contest (KO {0:N0} of its {1:N0} units)",
                            held?.FirstRoundSpent != null && held.FirstRoundSpent.TryGetValue("KO", out double ko0) ? ko0 : double.NaN, LiveCampaignSetup.WarChest));
                        CampaignRun.State runOffState = s.PlayerCampaign;
                        string partyA = held?.Field.Find(c => c.Name == held.RunOffA)?.Party, partyB = held?.Field.Find(c => c.Name == held.RunOffB)?.Party;
                        bool koFinalist = held != null && held.RunOffPending() && (partyA == "KO" || partyB == "KO");
                        if (koFinalist)
                        {
                            bool chests = runOffState != null && Enumerable.Range(0, cast.Length).All(p => cast[p] == partyA || cast[p] == partyB
                                ? Math.Abs(runOffState.Setup.Parties[p].StartingMoney - (LiveCampaignSetup.WarChest - held.FirstRoundSpent[cast[p]])) < 1e-6
                                : runOffState.Setup.Parties[p].StartingMoney == 0.0);
                            Check(runOffState != null && runOffState.Setup.Contest == CampaignContest.PresidentialRunOff && runOffState.TotalDays == 14 && runOffState.Day == 1
                                  && runOffState.Setup.Calendar.ElectionDate == held.RunOffOn && chests,
                                F("(c) PL-C: the run-off's campaign opened on the first vote's own day, its day 0 stepped ({0} of {1} days); {2} and {3} hold the limit less their first round, every other party nothing",
                                    runOffState?.Day ?? -1, runOffState?.TotalDays ?? -1, partyA, partyB));
                        }
                        else
                        {
                            Check(held != null && held.RunOffPending() && runOffState?.Setup.Contest != CampaignContest.PresidentialRunOff,
                                F("(c) PL-C: the player's party backs neither finalist ({0}, {1}) - no run-off campaign is the player's (L6)", partyA, partyB));
                        }
                        bool same = held != null && control != null && control.PlayerCandidate == null && control.Field.Count == held.Field.Count
                                    && control.Field.Zip(held.Field, (a, b) => a.Name == b.Name && a.Share == b.Share).All(x => x) && control.Line == held.Line;
                        Check(same, F("(c) THE FACTOR OF RECORD STANDS: the same world with no candidate seated holds the same first round, share for share ({0} candidates; {1}) - the mark changes no share",
                            held?.Field.Count ?? 0, held == null ? "-" : string.Join(", ", held.Field.Select(c => F("{0} {1:0.00} %", WorldClock.SurnameOf(c.Name), c.Share)))));

                        Step(s, w, CutRounds);
                        int runOffDayAtCut = s.PlayerCampaign != null && s.PlayerCampaign.Setup.Contest == CampaignContest.PresidentialRunOff ? s.PlayerCampaign.Day : -1;
                        string jsonRounds = SaveGameService.Serialize(SaveGameService.CreateSaveGame(s, w, CountryId.Poland, null));
                        Step(s, w, RunOff);
                        bool decided = held != null && held.RunOffHeld && held.Decided();
                        string elected = held?.Elected;
                        double shareA = held?.RunOffShareA ?? double.NaN;
                        double transferA = held?.RunOffTransferShareA ?? double.NaN;
                        if (koFinalist) { Check(!double.IsNaN(transferA) && held.Line.Contains("the finalists' campaign between the rounds"), F("(c) PL-C: the run-off counted the campaign's share of the two in B4's place: {0}", held?.Line)); }
                        Check(decided && pl.PresidentialElections.Count == 1, F("(c) the run-off on {0}, once: {1}", Day(RunOff), held?.Line));
                        bool plantedWin = decided && held.Elected != trzaskowski;
                        if (plantedWin) { held.Elected = trzaskowski; held.ElectedParty = "KO"; held.Line += " - PLANTED by PolishPresidentialStartDiagnostic: the player's win forced"; }
                        Check(held != null && held.PlayerWon(), F("(c) THE WIN, {0}: the player's candidate elected, taking office {1}", plantedWin ? "PLANTED (the model elected " + elected + ")" : "as the model counted it", Day(held?.TakesOffice ?? DateTime.MinValue)));
                        // K44 (Elias, 2026-10-23): the Desk's role chip reads PRESIDENT-ELECT from the deciding vote to the oath's eve, by PresidentialElection.PlayerElectOn
                        bool electSpan = held != null && PresidentialElection.PlayerElectOn(pl, RunOff.AddDays(-1)) == null && PresidentialElection.PlayerElectOn(pl, RunOff) == held
                                         && PresidentialElection.PlayerElectOn(pl, Oath.AddDays(-1)) == held && PresidentialElection.PlayerElectOn(pl, Oath) == null;
                        Check(electSpan, F("(c) K44 PRESIDENT-ELECT: none on {0}, the player's on the run-off {1} and on the oath's eve {2}, none on the oath {3} (the office's PRESIDENT from it)",
                            Day(RunOff.AddDays(-1)), Day(RunOff), Day(Oath.AddDays(-1)), Day(Oath)));
                        // the review of wave/usf, L3: a first vote won outright - PRESIDENT-ELECT from the first vote itself, no run-off held (a planted contest)
                        var outright = new Country { Id = CountryId.Poland };
                        var outrightWin = new PresidentialElection.Contest { FirstVote = FirstVote, PlayerCandidate = trzaskowski, Elected = trzaskowski, ElectedParty = "KO", RunOffHeld = false, TakesOffice = Oath };
                        outright.PresidentialElections.Add(outrightWin);
                        bool outrightSpan = PresidentialElection.PlayerElectOn(outright, FirstVote.AddDays(-1)) == null && PresidentialElection.PlayerElectOn(outright, FirstVote) == outrightWin
                                            && PresidentialElection.PlayerElectOn(outright, Oath.AddDays(-1)) == outrightWin && PresidentialElection.PlayerElectOn(outright, Oath) == null;
                        Check(outrightSpan, F("(c) K44 an outright first-round win (planted): none on {0}, the player's from the first vote {1} to the oath's eve, none on the oath {2}",
                            Day(FirstVote.AddDays(-1)), Day(FirstVote), Day(Oath)));

                        // the statutes and the budget, introduced while the party still governs
                        Step(s, w, BillsIn);
                        List<TaxType> taxes = Removable(pl, 5);
                        bool billsIn = taxes.Count >= 5 && taxes.Take(4).All(t => s.IntroduceTaxProgramBill(CountryId.Poland, t, false));
                        Step(s, w, BudgetIn);
                        (BudgetBill budget, TaxType budgetTax, float budgetRateTo) = RateBudget(pl, taxes.Count >= 5 ? taxes[4] : (TaxType?)null, taxes.Take(4).ToList());
                        float budgetRateWas = budget == null ? float.NaN : pl.TaxLines.First(t => t.Type == budgetTax).Rate;
                        bool budgetIn = budget != null && BudgetBillHarness.Introduce(s, CountryId.Poland, budget);
                        Check(billsIn && budgetIn, F("(c) while the party governs: four removals introduced on {0} ({1}), a budget moving {2} to {3} on {4}", Day(BillsIn), string.Join(", ", taxes.Take(4)), budgetTax, Num(budgetRateTo), Day(BudgetIn)));

                        // K44: the Desk's role chip, read through the controller's own RoleChip (a controller in edit mode, the manager and the player's country handed to it)
                        var chipHost = new GameObject("PolishPresidentialStartDiagnostic.Chip"); hosts.Add(chipHost);
                        chipHost.AddComponent<SimulationManager>();
                        var chipController = chipHost.AddComponent<PoliSim.UI.GameController>();
                        string Chip()
                        {
                            const BindingFlags Own = BindingFlags.Instance | BindingFlags.NonPublic;
                            typeof(PoliSim.UI.GameController).GetField("_simulationManager", Own)?.SetValue(chipController, s);
                            typeof(PoliSim.UI.GameController).GetField("_playerCountry", Own)?.SetValue(chipController, pl);
                            object read = typeof(PoliSim.UI.GameController).GetMethod("RoleChip", Own)?.Invoke(chipController, null);
                            return read is ValueTuple<string, bool> chip ? chip.Item1 + (chip.Item2 ? " (filled)" : " (outlined)") : "NO CHIP";
                        }
                        string chipOnRunOff = Chip();
                        Step(s, w, Oath.AddDays(-1));
                        bool eve = s.PlayerGoverns(pl) && !pl.PlayerHoldsPresidency && s.PlayerMayIntroduce(CountryId.Poland, out _);
                        string chipOnEve = Chip();
                        Step(s, w, Oath);
                        string chipOnOath = Chip();
                        Check(chipOnRunOff == "PRESIDENT-ELECT (outlined)" && chipOnEve == "PRESIDENT-ELECT (outlined)" && chipOnOath == "PRESIDENT (filled)",
                            F("(c) K44 the Desk's role chip: after the run-off \"{0}\", on the oath's eve \"{1}\" (words only - the party still governs), on the oath \"{2}\"", chipOnRunOff, chipOnEve, chipOnOath));
                        bool office = pl.PlayerHoldsPresidency && !s.PlayerGoverns(pl) && !s.PlayerMayIntroduce(CountryId.Poland, out string gate) && gate == SimulationManager.PresidentOfficeWords
                                      && !s.PlayerMayIntroduce(CountryId.Poland, CabinetPortfolio.FinanceTreasury, out _) && !CabinetSystem.ChairIsThePlayers(pl, CabinetPortfolio.FinanceTreasury)
                                      && !FinancePartner.PlayerHolds(pl) && PresidentialElection.PlayerHoldsOffice(pl, Oath, out string sworn) && sworn == trzaskowski;
                        Check(eve && office, F("(c) on the eve ({0}) the party governs; on the oath's day {1} the player is President - the role gate refuses in the office's words (\"{2}\"), no chair or Finance dial is his: the party is the AI's",
                            Day(Oath.AddDays(-1)), Day(Oath), SimulationManager.PresidentOfficeWords));
                        // the review's M2: the party's own verbs refuse in the office's words too (their chips are not drawn while he holds it)
                        bool leaveRefused = !s.LeaveGovernment(CountryId.Poland, out string leaveWhy) && leaveWhy == SimulationManager.PresidentOfficeWords;
                        bool withdrawRefused = !s.WithdrawSupport(CountryId.Poland, out string withdrawWhy) && withdrawWhy == SimulationManager.PresidentOfficeWords;
                        bool motionRefused = !s.MoveNoConfidence(CountryId.Poland, out string motionWhy, out _) && motionWhy == SimulationManager.PresidentOfficeWords;
                        Check(leaveRefused && withdrawRefused && motionRefused && pl.Government.Cabinet.Contains("KO"),
                            F("(c) the party's own verbs refuse while he is President - leaving the government (\"{0}\"), withdrawing support (\"{1}\"), moving no confidence (\"{2}\"); the cabinet unchanged",
                                leaveWhy, withdrawWhy, motionWhy));

                        Step(s, w, Presented);
                        List<SimulationManager.DocketItem> docket = s.GetDocket(CountryId.Poland).Where(i => i.Entry.Act < 0 && i.Entry.Bill == 0).ToList();
                        List<TaxType> heldTaxes = taxes.Take(4).Where(t => PassageOf(s, t) != null).ToList();
                        bool heldShape = docket.Count == heldTaxes.Count && heldTaxes.Count >= 2 && docket.All(i => i.PresentedOn == Presented && i.LastDay == new DateTime(2025, 8, 31) && i.Answer == 0 && i.Entry.Passage.Passed)
                                         && heldTaxes.All(t => Implemented(pl, t));
                        Check(heldShape, F("(c) on {0} the Sejm passes {1} of the four and each is HELD on the President's docket to 2025-08-31 - nothing applied ({2})", Day(Presented), heldTaxes.Count, string.Join("; ", docket.Select(i => i.Title))));

                        Step(s, w, CutOath);
                        string jsonOath = SaveGameService.Serialize(SaveGameService.CreateSaveGame(s, w, CountryId.Poland, null));
                        int heldAtCut = s.GetDocket(CountryId.Poland).Count;
                        if (heldTaxes.Count >= 2)
                        {
                            TaxType signed = heldTaxes[0], vetoed = heldTaxes[1];
                            DivisionRecord signedPassage = PassageOf(s, signed), vetoedPassage = PassageOf(s, vetoed);
                            int seen = Last(pl);
                            bool signs = s.SignDocketBill(CountryId.Poland, signedPassage, out string signWhy);
                            bool vetoes = s.VetoDocketBill(CountryId.Poland, vetoedPassage, out string vetoWhy);
                            bool again = s.SignDocketBill(CountryId.Poland, signedPassage, out string againWhy);
                            bool stranger = s.VetoDocketBill(CountryId.Poland, new DivisionRecord { Title = "not on the docket" }, out string strangerWhy);
                            Check(signs && vetoes && !again && againWhy == "that bill is not on your docket" && !stranger && strangerWhy == "that bill is not on your docket",
                                F("(c) on {0} the President signs \"{1}\" and vetoes \"{2}\" ({3}; {4}) - each acted on at once (J6 (i), read for Poland); answered again, the law is no longer held (\"{5}\"), and a bill never held (\"{6}\")",
                                    Day(CutOath), signedPassage?.Title, vetoedPassage?.Title, signWhy ?? "accepted", vetoWhy ?? "accepted", againWhy, strangerWhy));
                            DivisionRecord vote = pl.Divisions.Entries.FirstOrDefault(r => r.Number > seen && r.Title.StartsWith("Vetoed by the President (" + trzaskowski + "): ", StringComparison.Ordinal));
                            Check(!Implemented(pl, signed) && PassageOf(s, signed) == null, F("(c) SIGNED: on {0}, the answer's day, \"Remove {1}\" is law - the tax no longer levied, the bill out of its slot", Day(CutOath), signed));
                            Check(vote != null && vote.Required > 0 && vote.Date == CutOath && vetoedPassage.Motion && PassageOf(s, vetoed) == null && Implemented(pl, vetoed) != vote.Passed,
                                F("(c) VETOED: on {0}, the answer's day, the Sejm votes on the veto at 3/5 of those voting - \"{1}\"; the passage marked a motion; the tax {2}", Day(CutOath), vote?.Title,
                                    vote == null ? "-" : vote.Passed ? "removed (overridden)" : "still levied (the veto stands)"));
                        }

                        // the budget: the budget act at 7 days, its tax act at 21, applied once
                        Step(s, w, BudgetPresented);
                        BudgetBill pending = s.GetPendingBudgetBill(CountryId.Poland);
                        List<SimulationManager.DocketItem> budgetItems = s.GetDocket(CountryId.Poland).Where(i => pending?.ActsHeld != null && pending.ActsHeld.Contains(i.Entry)).ToList();
                        SimulationManager.DocketItem act224 = budgetItems.FirstOrDefault(i => i.Entry.Act < 0), taxAct = budgetItems.FirstOrDefault(i => i.Entry.Act == (int)BudgetBill.StatutePart.Rates);
                        bool budgetHeld = act224.Entry != null && taxAct.Entry != null && act224.LastDay == new DateTime(2025, 8, 26) && taxAct.LastDay == new DateTime(2025, 9, 9)
                                          && act224.Entry.Bill == (int)PresidentialDocket.Bill.BudgetAct && Mathf.Approximately(pl.TaxLines.First(t => t.Type == budgetTax).Rate, budgetRateWas);
                        bool refusesVeto = act224.Entry != null && !s.VetoDocketBill(CountryId.Poland, act224.Entry.Passage, out string act224Why) && act224Why == "the budget act cannot be vetoed";
                        Check(budgetHeld && refusesVeto, F("(c) the budget adopted on {0} goes to the docket: the budget act by {1} (7 days, Art. 224 ust. 1) - refusing a veto - and its tax act by {2} (21); the rate unchanged",
                            Day(BudgetPresented), Day(act224.LastDay), Day(taxAct.LastDay)));
                        // the rebase review's L5: an answer moves nothing but its own bill - a bill still counting down (planted beside the docket, two days left) keeps
                        // its two days, and answering a statute while the budget is held leaves the budget's held acts as they were
                        TaxType countdownTax = pl.TaxLines.Select(t => t.Type).FirstOrDefault(t => !taxes.Contains(t) && t != budgetTax);
                        var countdown = new TaxProgramBill { Type = countdownTax, IsAdd = !pl.TaxLines.First(t => t.Type == countdownTax).IsImplemented, DaysRemaining = 2 };
                        bool planted = PlantPending(s, countdown);
                        int budgetHeldBefore = s.GetPendingBudgetBill(CountryId.Poland)?.ActsHeld?.Count ?? -1;
                        DivisionRecord fourth = heldTaxes.Count >= 4 ? PassageOf(s, heldTaxes[3]) : null;
                        bool fourthSigned = fourth != null && s.SignDocketBill(CountryId.Poland, fourth, out _);
                        Check(planted && fourthSigned && !Implemented(pl, heldTaxes[3]) && countdown.DaysRemaining == 2 && countdown.Docket == null && PassageOf(s, countdownTax) == null
                              && s.GetPendingTaxProgramBills(CountryId.Poland).Contains(countdown) && budgetHeldBefore == 2 && s.GetPendingBudgetBill(CountryId.Poland)?.ActsHeld?.Count == 2,
                            F("(c) AN ANSWER MOVES ITS OWN BILL ALONE: \"Remove {0}\" signed on {1} is law at once, while a bill still counting down keeps its {2} days and the held budget keeps its {3} held acts",
                                heldTaxes.Count >= 4 ? heldTaxes[3].ToString() : "-", Day(BudgetPresented), countdown.DaysRemaining, s.GetPendingBudgetBill(CountryId.Poland)?.ActsHeld?.Count ?? 0));
                        UnplantPending(s, countdown);
                        if (taxAct.Entry != null) { s.SignDocketBill(CountryId.Poland, taxAct.Entry.Passage, out _); }
                        bool actAtOnce = s.GetPendingBudgetBill(CountryId.Poland)?.ActsHeld?.Count == 1 && s.GetPendingBudgetBill(CountryId.Poland).ActsLaw.Contains((int)BudgetBill.StatutePart.Rates);   // law at once; the budget still waits on its act
                        Step(s, w, new DateTime(2025, 8, 26));
                        bool waits = s.GetPendingBudgetBill(CountryId.Poland)?.ActsHeld?.Count == 1 && Mathf.Approximately(pl.TaxLines.First(t => t.Type == budgetTax).Rate, budgetRateWas);
                        Step(s, w, new DateTime(2025, 8, 27));
                        Check(actAtOnce && waits && s.GetPendingBudgetBill(CountryId.Poland) == null && Mathf.Approximately(pl.TaxLines.First(t => t.Type == budgetTax).Rate, budgetRateTo),
                            F("(c) the tax act signed on {0} is law at once, the budget act still held on 2025-08-26; unanswered it is signed on 2025-08-27 and the budget applies once - {1} from {2} to {3}",
                                Day(BudgetPresented), budgetTax, Num(budgetRateWas), Num(pl.TaxLines.First(t => t.Type == budgetTax).Rate)));

                        if (heldTaxes.Count >= 3)
                        {
                            TaxType unanswered = heldTaxes[2];
                            Step(s, w, new DateTime(2025, 8, 31));
                            bool onLast = Implemented(pl, unanswered) && PassageOf(s, unanswered) != null;
                            Step(s, w, new DateTime(2025, 9, 1));
                            Check(onLast && !Implemented(pl, unanswered) && PassageOf(s, unanswered) == null, F("(c) UNANSWERED: \"Remove {0}\" is held on 2025-08-31, the 21st day, and signed on 2025-09-01", unanswered));
                        }
                        else { Check(false, "(c) a third statute held, to run unanswered (the Sejm passed fewer than three)"); }

                        // ============================================================ (c2) N2: A PLANTED RUN-OFF - the player's party backing one finalist, whatever the
                        // first round produced: the game opens its campaign, its chests are the limit less the first round, a save inside it loads and replays to the
                        // same run-off
                        PlantedRunOff(hosts, start, trzaskowski, Check);

                        // ============================================================ (d) the save across the rounds, and the loss
                        SaveGame rounds = SaveGameService.Deserialize(jsonRounds);
                        (SimulationManager sa, PoliSim.UI.GameController _) = Adopt(rounds, hosts, out string errorA);
                        if (sa == null) { Check(false, "(d) the save across the rounds loaded into the game: " + errorA); }
                        else
                        {
                            Country la = sa.World.GetCountry(CountryId.Poland);
                            PresidentialElection.Contest pending0 = la.PresidentialElections.Count == 1 ? la.PresidentialElections[0] : null;
                            bool between = rounds.SaveVersion == SaveGameService.CurrentSaveVersion && pending0 != null && pending0.RunOffPending() && pending0.PlayerCandidate == trzaskowski && la.PlayerCandidate == trzaskowski
                                           && la.PlayerStart == "PRESIDENTIAL ELECTION 2025";
                            // PL-C (M3): the run-off's campaign replayed by the load to the day it was cut on, on the saved first-round spending
                            bool replayed = !koFinalist || (sa.PlayerCampaign != null && sa.PlayerCampaign.Setup.Contest == CampaignContest.PresidentialRunOff && sa.PlayerCampaign.Day == runOffDayAtCut
                                            && pending0?.FirstRoundSpent != null && pending0.FirstRoundSpent.Count == held.FirstRoundSpent.Count && pending0.FirstRoundSpent.All(kv => held.FirstRoundSpent[kv.Key] == kv.Value));
                            Step(sa, sa.World, RunOff);
                            Check(replayed && (!koFinalist || pending0.RunOffTransferShareA == transferA), F("(d) PL-C: the load replays the run-off's campaign to its day {0} on the saved first-round spending, and counts the same run-off (B4's transfer {1:0.00} % beside it)", runOffDayAtCut, transferA));
                            Check(between && pending0.Decided() && pending0.Elected == elected && pending0.RunOffShareA == shareA && la.PresidentialElections.Count == 1,
                                F("(d) a save cut on {0}, between the rounds, at format {1}: loaded INTO THE GAME (RestoreFromSave) with the contest pending and the player's candidate marked; stepped, the run-off on {2} is the one the world it was cut from held - {3} {4:0.00} % of the two",
                                    Day(CutRounds), rounds.SaveVersion, Day(RunOff), elected, shareA));
                            bool plantedLoss = pending0 != null && pending0.Elected == trzaskowski;
                            if (plantedLoss)
                            {
                                string other = pending0.RunOffA == trzaskowski ? pending0.RunOffB : pending0.RunOffA;
                                pending0.Elected = other;
                                pending0.ElectedParty = pending0.Field.FirstOrDefault(c => c.Name == other)?.Party;
                                pending0.Line += " - PLANTED by PolishPresidentialStartDiagnostic: the player's loss forced";
                            }
                            Step(sa, sa.World, Oath.AddDays(1));
                            bool lost = pending0 != null && !pending0.PlayerWon() && !la.PlayerHoldsPresidency && sa.PlayerGoverns(la) && sa.PlayerMayIntroduce(CountryId.Poland, out _)
                                        && sa.GetDocket(CountryId.Poland).Count == 0 && PresidentialElection.PresidentAt(CountryId.Poland, la.PresidentialElections, Oath, out PresidencyOfRecord.President successor)
                                        && successor.Name == pending0.Elected && PresidentialElection.PlayerCandidateFor(la, new DateTime(2030, 5, 1)) == null;
                            Check(lost, F("(d) THE LOSS, {0}: on {1} {2} takes office; the player holds none and plays on as KO, the prime minister's party (L1) - the levers his, nothing held; no later contest is the player's",
                                plantedLoss ? "PLANTED (the model elected the player's candidate)" : "as the model counted it", Day(Oath), pending0?.Elected));
                        }

                        // ============================================================ (d2) the window open at the oath (the review's M3)
                        SaveGame roundsAgain = SaveGameService.Deserialize(jsonRounds);
                        (SimulationManager sw, PoliSim.UI.GameController _) = Adopt(roundsAgain, hosts, out string errorW);
                        if (sw == null) { Check(false, "(d2) the save across the rounds loaded into the game a second time: " + errorW); }
                        else
                        {
                            Country lw = sw.World.GetCountry(CountryId.Poland);
                            Step(sw, sw.World, RunOff);
                            PresidentialElection.Contest won = lw.PresidentialElections.Count == 1 ? lw.PresidentialElections[0] : null;
                            if (won != null && won.Decided() && !won.PlayerWon()) { won.Elected = trzaskowski; won.ElectedParty = "KO"; won.Line += " - PLANTED by PolishPresidentialStartDiagnostic: the player's win forced"; }
                            Step(sw, sw.World, Oath.AddDays(-1));
                            bool openOnEve = sw.GetPendingBudgetProcess(CountryId.Poland) && sw.GetPendingBudgetBill(CountryId.Poland) == null && sw.PlayerGoverns(lw);
                            Step(sw, sw.World, Oath);
                            bool closedAtOath = lw.PlayerHoldsPresidency && !sw.GetPendingBudgetProcess(CountryId.Poland);
                            DateTime requestDay = FiscalYearData.BudgetRequestDay(CountryId.Poland, 2026);
                            Step(sw, sw.World, requestDay);
                            BudgetBill tabled = sw.GetPendingBudgetBill(CountryId.Poland);
                            Check(openOnEve && closedAtOath && tabled != null && tabled.GovernmentBill,
                                F("(d2) THE WINDOW AT THE OATH: the arrival window, open and unused on the eve ({0}), is closed on the oath's day {1} (the party no longer governs for the player, §698's class); on the request day {2} the AI government tables its budget ({3})",
                                    Day(Oath.AddDays(-1)), Day(Oath), Day(requestDay), tabled == null ? "none tabled" : tabled.GovernmentBill ? "the government's bill" : "a bill not the government's"));
                        }

                        // ============================================================ (e) the save across the oath
                        SaveGame oath = SaveGameService.Deserialize(jsonOath);
                        Country cutPl = oath.World.GetCountry(CountryId.Poland);
                        Check(oath.SaveVersion == SaveGameService.CurrentSaveVersion && cutPl.PlayerHoldsPresidency && cutPl.PlayerCandidate == trzaskowski && cutPl.PlayerStart == "PRESIDENTIAL ELECTION 2025"
                              && cutPl.PresidentialElections.Count == 1 && cutPl.PresidentialElections[0].PlayerCandidate == trzaskowski,
                            F("(e) a save cut on {0}, past the oath, at format {1}: the candidate, the start's key, the office and the contest's mark ride it", Day(CutOath), oath.SaveVersion));
                        (SimulationManager so, PoliSim.UI.GameController co) = Adopt(oath, hosts, out string errorO);
                        if (so == null) { Check(false, "(e) the save across the oath loaded into the game: " + errorO); }
                        else
                        {
                            Country lo = so.World.GetCountry(CountryId.Poland);
                            int loadedHeld = so.GetDocket(CountryId.Poland).Count;
                            bool president = lo.PlayerHoldsPresidency && !so.PlayerGoverns(lo);
                            // the review's M1: a held statute stays answerable after the load - vetoed through the Docket page's own answer (the controller's
                            // AnswerDocketItem, the row's SIGN / VETO), acted on at once (J6 (i)), as before the save
                            TaxType vetoedAfterLoad = heldTaxes.Count > 0 ? heldTaxes[0] : default;
                            DivisionRecord loadedPassage = heldTaxes.Count > 0 ? PassageOf(so, vetoedAfterLoad) : null;
                            bool plateWouldAsk = loadedPassage != null && so.TryUnansweredDocketItem(CountryId.Poland, loadedPassage, out _);
                            MethodInfo answer = typeof(PoliSim.UI.GameController).GetMethod("AnswerDocketItem", BindingFlags.Instance | BindingFlags.NonPublic);
                            int seenLoaded = Last(lo);
                            bool vetoedLoaded = answer != null && loadedPassage != null && answer.Invoke(co, new object[] { loadedPassage, -1 }) is bool done && done;
                            DivisionRecord loadedVote = lo.Divisions.Entries.FirstOrDefault(r => r.Number > seenLoaded && r.Title.StartsWith("Vetoed by the President (" + trzaskowski + "): ", StringComparison.Ordinal));
                            Check(president && loadedHeld == heldAtCut && plateWouldAsk && vetoedLoaded && loadedVote != null && loadedVote.Required > 0 && loadedPassage.Motion && PassageOf(so, vetoedAfterLoad) == null
                                  && Implemented(lo, vetoedAfterLoad) != loadedVote.Passed,
                                F("(e) loaded INTO THE GAME: the player is President, the docket as cut ({0} held); \"Remove {1}\" still answerable - vetoed on the Docket page after the load, the Sejm votes on the veto at once, on {2}: \"{3}\"",
                                    loadedHeld, vetoedAfterLoad, Day(CutOath), loadedVote?.Title));
                            Step(so, so.World, new DateTime(2025, 9, 1));
                            BudgetBill stillBudget = so.GetPendingBudgetBill(CountryId.Poland);
                            Check(so.GetDocket(CountryId.Poland).All(i => i.Entry.Act == (int)BudgetBill.StatutePart.Rates) && heldTaxes.Skip(1).All(t => !Implemented(lo, t)) && stillBudget?.ActsHeld?.Count == 1,
                                F("(e) stepped with no other answer, every other held statute is signed on 2025-09-01, and the budget voted 2025-08-19 in this world waits on its tax act's 21 days (the budget act signed after its 7)"));
                        }
                    }
                    finally { EnergyMarket.ResetTurnState(); }
                }
            }
            catch (Exception e) { failures++; sb.Append("    FAIL      threw - ").Append(e.GetType().Name).Append(": ").Append(e.Message).Append('\n').Append(e.StackTrace).Append('\n'); }
            finally
            {
                foreach (GameObject go in hosts) { if (go != null) { UnityEngine.Object.DestroyImmediate(go); } }
                SimulationRandom.RestoreState(seedWas, drawsWas);
            }

            sb.Append(failures == 0 ? "=== PolishPresidentialStartDiagnostic: the presidential start holds - the candidate on the contest, both rounds once, the office from the oath, the docket's terms, both saves into the game ===\n"
                                    : F("=== PolishPresidentialStartDiagnostic: {0} FAILURE(S) ===\n", failures));
            if (failures == 0) { Debug.Log(sb.ToString()); CheckExit.Finish(0); }
            else { Debug.LogError(sb.ToString()); CheckExit.Finish(1); }
        }

        /// <summary>N1: the finished first-round campaign's spending on the eve of the vote, by its party's name, as the limit reads it - the committee's limit in
        /// the purse's units (the model's chest where it stands) less what its run left; null where no finished presidential campaign is in hand.</summary>
        private static Dictionary<string, double> FirstRoundSpentOnTheEve(SimulationManager s)
        {
            if (s.PlayerCampaign == null || s.PlayerCampaignResult == null || s.PlayerCampaign.Setup.Contest != CampaignContest.PresidentialFirstVote) { return null; }
            var spent = new Dictionary<string, double>(StringComparer.Ordinal);
            CampaignRun.Setup setup = s.PlayerCampaign.Setup;
            for (int p = 0; p < setup.Parties.Length; p++)
            {
                double limit = setup.StandingShare(p) > 0.0 ? LiveCampaignSetup.WarChest : 0.0;
                spent[setup.Parties[p].Name] = Math.Max(0.0, limit - s.PlayerCampaignResult.Parties[p].MoneyLeft);
            }
            return spent;
        }

        /// <summary>N2: a fresh presidential-start world stepped to the first vote; where the first round did not put the player's party (KO) in a run-off, one is
        /// PLANTED on the contest - KO's candidate against the strongest other placed one, on the 14th day - so the game's own loop opens the run-off's
        /// campaign the next day. Held: its chests, the count in B4's place, a save inside it loading INTO THE GAME and replaying to the same run-off.</summary>
        private static void PlantedRunOff(List<GameObject> hosts, StartPoints.StartPoint start, string trzaskowski, Action<bool, string> Check)
        {
            (SimulationManager s, World w, Country pl) = Open(hosts, start, trzaskowski);
            Step(s, w, FirstVote);
            PresidentialElection.Contest c = pl.PresidentialElections.Count == 1 ? pl.PresidentialElections[0] : null;
            PresidentialElection.Candidate ko = c?.Field.Find(x => x.Party == "KO" && x.Placed);
            PresidentialElection.Candidate other = c?.Field.Where(x => x.Party != null && x.Party != "KO" && x.Placed).OrderByDescending(x => x.Share).FirstOrDefault();
            if (c == null || ko == null || other == null || c.FirstRoundSpent == null) { Check(false, "(c2) N2: no first vote to plant a run-off on (" + (c?.Line ?? "no contest") + ")"); return; }
            bool planted = !(c.RunOffPending() && (c.Field.Find(x => x.Name == c.RunOffA)?.Party == "KO" || c.Field.Find(x => x.Name == c.RunOffB)?.Party == "KO"));
            if (planted)
            {
                c.Elected = null; c.ElectedParty = null; c.RunOffHeld = false;
                c.RunOffA = ko.Name; c.RunOffB = other.Name; c.RunOffOn = FirstVote.AddDays(14);
                c.Line += " - PLANTED by PolishPresidentialStartDiagnostic (N2): " + ko.Name + " against " + other.Name + " in the run-off";
            }
            Step(s, w, FirstVote.AddDays(2));
            CampaignRun.State ro = s.PlayerCampaign;
            string[] cast = LiveCampaignSetup.Keys(CountryId.Poland);
            string a = c.Field.Find(x => x.Name == c.RunOffA)?.Party, b = c.Field.Find(x => x.Name == c.RunOffB)?.Party;
            bool chests = ro != null && Enumerable.Range(0, cast.Length).All(p => cast[p] == a || cast[p] == b
                ? Math.Abs(ro.Setup.Parties[p].StartingMoney - (LiveCampaignSetup.WarChest - c.FirstRoundSpent[cast[p]])) < 1e-6
                : ro.Setup.Parties[p].StartingMoney == 0.0 && !Enumerable.Range(0, ro.Setup.Regions.Length).Any(r => ro.Setup.StandsIn(p, r)));
            Check(ro != null && ro.Setup.Contest == CampaignContest.PresidentialRunOff && ro.TotalDays == 14 && ro.Setup.Calendar.ElectionDate == c.RunOffOn && chests,
                F("(c2) N2: {0}the run-off {1} ({2}) against {3} ({4}) is campaigned by the game's own loop - {5} of 14 days stepped two days on; the finalists hold the limit less their first round, every other party stands nowhere and holds nothing",
                    planted ? "PLANTED - " : string.Empty, c.RunOffA, a, c.RunOffB, b, ro?.Day ?? -1));
            DateTime cut = c.RunOffOn.AddDays(-7);
            Step(s, w, cut);
            int dayAtCut = s.PlayerCampaign != null ? s.PlayerCampaign.Day : -1;
            string json = SaveGameService.Serialize(SaveGameService.CreateSaveGame(s, w, CountryId.Poland, null));
            Step(s, w, c.RunOffOn);
            Check(c.Decided() && c.RunOffHeld && !double.IsNaN(c.RunOffTransferShareA) && c.Line.Contains("the finalists' campaign between the rounds"),
                F("(c2) N2: the run-off counted the campaign's share of the two in B4's place - {0}", c.Line));
            (SimulationManager sa, PoliSim.UI.GameController _) = Adopt(SaveGameService.Deserialize(json), hosts, out string error);
            if (sa == null) { Check(false, "(c2) N2: the save inside the run-off loaded into the game: " + error); return; }
            Country la = sa.World.GetCountry(CountryId.Poland);
            PresidentialElection.Contest lc = la.PresidentialElections.Count == 1 ? la.PresidentialElections[0] : null;
            bool replayed = sa.PlayerCampaign != null && sa.PlayerCampaign.Setup.Contest == CampaignContest.PresidentialRunOff && sa.PlayerCampaign.Day == dayAtCut && lc != null && lc.RunOffPending()
                            && lc.RunOffA == c.RunOffA && lc.RunOffB == c.RunOffB && lc.FirstRoundSpent != null && lc.FirstRoundSpent.All(kv => c.FirstRoundSpent[kv.Key] == kv.Value);
            Step(sa, sa.World, c.RunOffOn);
            Check(replayed && lc.Decided() && lc.Elected == c.Elected && lc.RunOffShareA == c.RunOffShareA && lc.RunOffTransferShareA == c.RunOffTransferShareA,
                F("(c2) N2: a save cut on {0}, inside the run-off, loads INTO THE GAME and replays its campaign to day {1}; stepped, the run-off is the one the world it was cut from held - {2} {3:0.00} % of the two",
                    Day(cut), dayAtCut, c.RunOffA, c.RunOffShareA));
        }

        /// <summary>A world opened on <paramref name="start"/> as a new game opens it: the epoch set before the world exists, the player's country Poland, the
        /// candidate seated through the controller's own call (none: KO seated as the party picker would, the control) and the new game's average cabinet.</summary>
        private static (SimulationManager, World, Country) Open(List<GameObject> hosts, StartPoints.StartPoint start, string candidate)
        {
            SimulationRandom.Seed(Seed);
            EnergyMarket.ResetCalibration();
            SimulationManager.SetEpoch(start.Opens);
            World w = WorldFactory.CreateDefault();
            var go = new GameObject("PolishPresidentialStartDiagnostic"); hosts.Add(go);
            var s = go.AddComponent<SimulationManager>();
            s.SetWorld(w);
            s.PlayerCountryId = CountryId.Poland;
            Country pl = w.GetCountry(CountryId.Poland);
            if (candidate == null || !PresidentialStart.Seat(pl, start, candidate, out _)) { pl.PlayerPartyAbbrev = "KO"; pl.PartyApprovalRating = pl.State.ApprovalRating; }
            CabinetSystem.SeatAverageCabinet(pl);
            return (s, w, pl);
        }

        /// <summary>Up to <paramref name="n"/> levied taxes whose removal the Sejm passes on today's projection (`ParliamentSystem.WouldBillPass`).</summary>
        private static List<TaxType> Removable(Country pl, int n)
        {
            var found = new List<TaxType>();
            foreach (TaxLine line in pl.TaxLines.Where(t => t.IsImplemented))
            {
                if (ParliamentSystem.WouldBillPass(pl, ParliamentSystem.GetTaxProgramBillConcern(pl, new TaxProgramBill { Type = line.Type, IsAdd = false }))) { found.Add(line.Type); }
                if (found.Count == n) { break; }
            }
            return found;
        }

        /// <summary>A budget moving one levied rate a point down - the fund restated, so no fund act - whose budget act and tax act the Sejm passes on today's
        /// projection; the fifth removable tax first, then any other levied line not among the four removals.</summary>
        private static (BudgetBill, TaxType, float) RateBudget(Country pl, TaxType? first, List<TaxType> removals)
        {
            IEnumerable<TaxLine> lines = pl.TaxLines.Where(t => t.IsImplemented && t.Rate >= 1f && !removals.Contains(t.Type));
            if (first.HasValue) { lines = lines.OrderBy(t => t.Type == first.Value ? 0 : 1); }
            foreach (TaxLine line in lines)
            {
                BudgetBill bill = BudgetBillHarness.RestatingFund(pl, new BudgetBill());
                float to = line.Rate - 1f;
                bill.TaxLines[line.Type] = to;
                if (!bill.Changes(BudgetBill.StatutePart.Rates, pl)) { continue; }
                bool act = ParliamentSystem.WouldBillPass(pl, ParliamentSystem.GetBudgetBillConcern(pl, bill.PartOf(BudgetBill.StatutePart.Rates)));
                bool whole = ParliamentSystem.WouldBillPass(pl, ParliamentSystem.GetBudgetBillConcern(pl, ParliamentSystem.BudgetActOf(pl, bill)));
                if (act && whole) { return (bill, line.Type, to); }
            }
            return (null, default, float.NaN);
        }

        /// <summary>The save adopted by a controller as the player's Load adopts it (`GameController.RestoreFromSave`) - PresidencyDiagnostic's adoption, for Poland.</summary>
        private static (SimulationManager, PoliSim.UI.GameController) Adopt(SaveGame save, List<GameObject> hosts, out string error)
        {
            error = null;
            var go = new GameObject("PolishPresidentialStartDiagnostic.Controller"); hosts.Add(go);
            SimulationManager sim = go.AddComponent<SimulationManager>();
            var controller = go.AddComponent<PoliSim.UI.GameController>();
            const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
            Type type = typeof(PoliSim.UI.GameController);
            FieldInfo simField = type.GetField("_simulationManager", Private), playerField = type.GetField("_playerCountry", Private);
            MethodInfo restore = type.GetMethod("RestoreFromSave", Private);
            if (simField == null || playerField == null || restore == null) { error = "the controller's load path could not be reached by name"; return (null, null); }
            simField.SetValue(controller, sim);
            try { restore.Invoke(controller, new object[] { save }); }
            catch (TargetInvocationException e) { error = "the controller cannot adopt the save - " + (e.InnerException ?? e).GetType().Name + ": " + (e.InnerException ?? e).Message; return (null, null); }
            finally
            {
                (type.GetField("_impactLedger", Private)?.GetValue(controller) as PolicyImpactLedger)?.Dispose();
                (type.GetField("_shadowBaseline", Private)?.GetValue(controller) as ShadowBaseline)?.Dispose();
            }
            var player = playerField.GetValue(controller) as Country;
            if (player == null || player.Id != CountryId.Poland) { error = "adopted, the controller governs " + (player == null ? "no country" : player.Id.ToString()) + ", not Poland"; return (null, null); }
            if (!ReferenceEquals(sim.World, save.World)) { error = "adopted, the manager and the save hold two worlds"; return (null, null); }
            return (sim, controller);
        }

        /// <summary>Steps <paramref name="s"/> to <paramref name="until"/> in the controller's order (the day, the player's day tick, the boundary's turn).</summary>
        private static void Step(SimulationManager s, World w, DateTime until)
        {
            while (s.CurrentDate < until)
            {
                bool boundary = s.AdvanceDay();
                s.AdvanceCountryDayTick(CountryId.Poland);
                if (boundary)
                {
                    var none = new Dictionary<CountryId, PolicyDecision>();
                    foreach (Country c in w.Countries) { none[c.Id] = PolicyDecision.None(); }
                    s.AdvanceTurn(none);
                }
            }
        }

        /// <summary>A tax-programme bill seated in the manager's pending slot as an introduced one sits there, still counting down - no answer on it, no docket.</summary>
        private static bool PlantPending(SimulationManager s, TaxProgramBill bill)
        {
            var slots = typeof(SimulationManager).GetField("_pendingTaxProgramBillsByCountry", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(s) as Dictionary<CountryId, Dictionary<TaxType, TaxProgramBill>>;
            if (slots == null) { return false; }
            if (!slots.TryGetValue(CountryId.Poland, out Dictionary<TaxType, TaxProgramBill> pending)) { pending = new Dictionary<TaxType, TaxProgramBill>(); slots[CountryId.Poland] = pending; }
            if (pending.ContainsKey(bill.Type)) { return false; }
            pending[bill.Type] = bill;
            return true;
        }

        /// <summary>The planted bill taken back, so the run goes on as it was.</summary>
        private static void UnplantPending(SimulationManager s, TaxProgramBill bill)
        {
            var slots = typeof(SimulationManager).GetField("_pendingTaxProgramBillsByCountry", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(s) as Dictionary<CountryId, Dictionary<TaxType, TaxProgramBill>>;
            if (slots != null && slots.TryGetValue(CountryId.Poland, out Dictionary<TaxType, TaxProgramBill> pending) && pending.TryGetValue(bill.Type, out TaxProgramBill seated) && ReferenceEquals(seated, bill)) { pending.Remove(bill.Type); }
        }

        private static bool Implemented(Country pl, TaxType type) => pl.TaxLines.First(t => t.Type == type).IsImplemented;

        private static DivisionRecord PassageOf(SimulationManager s, TaxType type) =>
            s.GetPendingTaxProgramBills(CountryId.Poland).FirstOrDefault(b => b.Type == type)?.Docket?.Passage;

        private static int Last(Country pl) => pl.Divisions.Entries.Count > 0 ? pl.Divisions.Entries.Last().Number : 0;
        private static string Num(float v) => v.ToString("0.###", CultureInfo.InvariantCulture);
        private static string Day(DateTime d) => d == DateTime.MinValue ? "none" : d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        private static string F(string format, params object[] args) => string.Format(CultureInfo.InvariantCulture, format, args);
    }
}
