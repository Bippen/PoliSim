using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using PoliSim.Data;
using PoliSim.Elections;
using PoliSim.Persistence;
using PoliSim.Simulation;
using UnityEngine;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// PS-6 US-23 (`docs/specs/USA_STAGE_PLAN.md` US-23, "the president's pen"; R-US7 (a) and R-US9 as ruled, F8) with q16 (Elias's ruling of 2026-10-18: "a bloc
    /// votes for a budget when it moves policy toward the party's position against the standing law (spatial)"): THE DESK-BILL DIAGNOSTIC, on worlds opened as a
    /// new game opens them (`UsCongressDiagnostic.OpenUsa`: the 119th of record seated on 2025-02-03 - REP holding the House and the Senate) and stepped in the
    /// controller's order (the day, the player's day tick, the boundary's turn):
    /// <list type="bullet">
    /// <item>(a) the window's arithmetic, DERIVED by hand: 1105(a)'s first Monday in February 2025 opens it, a bill's course before 1 October closes it;</item>
    /// <item>(b) q16's spatial vote on K72's sourced scale (Elias's ruling of 2026-10-23): each party's position stands for its own sourced rate, a rate's place on
    /// the scale and back; a cut halfway toward REP's rate reads +1 for REP and -1 for DEM; a rise to the dial's ceiling, far past DEM's own rate, reads AGAINST
    /// from the standing rate below it (it ends farther - on the dial's map it read FOR) and from a planted rate above it, where the axis midpoint read FOR for
    /// both; GA-2-6's shape (every rate at its ceiling) reads against for REP; and a country without a Congress carries no spatial set. The desk's worlds plant
    /// the standing income tax between the two parties' sourced rates (DECLARED: REP's own is the law in force at the seed, which would leave its majority no book);</item>
    /// <item>(c) a DEM player-president (a planted game-elected Harris sworn 2025-01-20) facing the REP Congress of record: the REP majority tables its
    /// reconciliation (its book: the household rates toward REP's position, never past it) on the first day of the window and Congress sends it - REP for, DEM
    /// against, by a majority in the Senate - to the President's DESK, where it is held, nothing applied; SIGNED, it is law at once and the rates move to the
    /// book's; and none other is tabled before 1 October;</item>
    /// <item>(d) the same world VETOED: the House sustains the veto (two thirds of the passage's sides not reached) and the standing rates hold; and (the
    /// review's M1) the book is built again on the vote day - a rate law planted after the tabling is its base - as (c) applies the rates alone, from the law of
    /// the day it becomes law: a rate law and a fund planted while it waits on the desk are kept;</item>
    /// <item>(e) a president whose party holds Congress (a planted game-elected Trump, the player REP) receives none - nothing tabled, nothing on his desk;</item>
    /// <item>(f) the save cut while the bill is on the desk carries it and the year's mark (additive, no format step) and, loaded into the game, it is answered there.</item>
    /// </list>
    /// The random streams are restored after, the epoch by its scope, and every host destroyed.
    /// </summary>
    public static class UsMajorityReconciliationDiagnostic
    {
        private static readonly DateTime Oath = new DateTime(2025, 1, 20);

        public static void Run()
        {
            CheckExit.ArmLogFold();
            var sb = new StringBuilder("=== UsMajorityReconciliationDiagnostic (PS-6 US-23; q16): the out-party majority's reconciliation on the President's desk, and the spatial budget vote ===\n");
            int failures = 0;
            void Check(bool ok, string what) { if (!ok) { failures++; } sb.Append(ok ? "    ok        " : "    FAIL      ").Append(what).Append('\n'); }
            var hosts = new List<GameObject>();
            int seedWas = SimulationRandom.MasterSeed;
            Dictionary<SimulationRandom.Stream, int> drawsWas = SimulationRandom.CaptureDrawCounts();
            try
            {
                // ================================================================ (a) the window, DERIVED by hand
                var day = new DateTime(2025, 5, 1);
                Check(UsMajorityReconciliation.Deadline(day) == new DateTime(2025, 10, 1) && UsMajorityReconciliation.YearOf(day) == 2025
                      && UsMajorityReconciliation.WindowOpens(day) == new DateTime(2025, 2, 3) && UsMajorityReconciliation.WindowCloses(day) == new DateTime(2025, 10, 1).AddDays(-ParliamentSystem.BillDurationDays - 1)
                      && !UsMajorityReconciliation.InWindow(new DateTime(2025, 2, 2)) && UsMajorityReconciliation.InWindow(new DateTime(2025, 2, 3))
                      && UsMajorityReconciliation.InWindow(UsMajorityReconciliation.WindowCloses(day)) && !UsMajorityReconciliation.InWindow(UsMajorityReconciliation.WindowCloses(day).AddDays(1))
                      && !UsMajorityReconciliation.InWindow(new DateTime(2024, 12, 1)) && UsMajorityReconciliation.YearOf(new DateTime(2024, 12, 1)) == 2025 && UsMajorityReconciliation.YearOf(new DateTime(2025, 10, 1)) == 2026,
                    F("(a) the window for the 1 October 2025 deadline: opens {0} (the first Monday in February, 1105(a)'s request day), closes {1} ({2} days' course and a day before 1 October, so the vote and the presentment fall on or before 30 September); 2 Feb and {3} outside it; 1 Dec 2024 is in the same fiscal year, outside the window; 1 Oct 2025 marks the next year",
                        Day(UsMajorityReconciliation.WindowOpens(day)), Day(UsMajorityReconciliation.WindowCloses(day)), ParliamentSystem.BillDurationDays, Day(UsMajorityReconciliation.WindowCloses(day).AddDays(1))));

                using (SimulationManager.EpochScope())
                {
                    try
                    {
                        // ============================================================ (b) q16, the spatial vote
                        (SimulationManager sq, World wq, Country uq) = DemPresidentWorld(hosts);
                        TaxLine income = uq.TaxLines.First(t => t.Type == TaxType.IncomeTax && t.IsImplemented);
                        float repAt = Position(uq, "REP"), demAt = Position(uq, "DEM");
                        float roundTrip = SpatialBudgetVote.RateAt(uq, income, SpatialBudgetVote.ScaleAt(uq, income, income.Rate));
                        bool repRate = UsMajorityReconciliation.TryPartyRate(uq, "REP", income, out float repRateOf);
                        bool scaled = SpatialBudgetVote.TryUsScale(uq, income, out _, out float demSourced, out _, out float repSourced);
                        Check(scaled && Mathf.Abs(roundTrip - income.Rate) < 1e-3f && repRate && Mathf.Abs(repRateOf - repSourced) < 1e-4f && repRateOf < income.Rate && income.Rate < demSourced
                              && Mathf.Abs(SpatialBudgetVote.RateAt(uq, income, demAt) - demSourced) < 1e-3f,
                            F("(b) q16's scale, K72's sourced one: on `redistribution` REP at {0:0.00} stands for its own {1} % and DEM at {2:0.00} for its own {3} % (the dial's map had put them at {4} % and {5} %); the standing {6} % (planted between them, K72) at {7:0.00} and back to {8} %",
                                repAt, Num(repRateOf), demAt, Num(demSourced), Num(SpatialBudgetVote.RateAt(income, repAt)), Num(SpatialBudgetVote.RateAt(income, demAt)), Num(income.Rate), SpatialBudgetVote.ScaleAt(uq, income, income.Rate), Num(roundTrip)));
                        var cut = new BudgetBill(); cut.TaxLines[TaxType.IncomeTax] = income.Rate - (income.Rate - repRateOf) / 2f;   // halfway to REP's own rate, so it reaches toward REP without passing it
                        var ceiling = new BudgetBill(); ceiling.TaxLines[TaxType.IncomeTax] = income.MaxRate;
                        List<SpatialRate> cutRates = SpatialBudgetVote.RatesOf(uq, cut), ceilingRates = SpatialBudgetVote.RatesOf(uq, ceiling);
                        float repCut = SpatialBudgetVote.Alignment(repAt, cutRates), demCut = SpatialBudgetVote.Alignment(demAt, cutRates);
                        Check(Mathf.Approximately(repCut, 1f) && Mathf.Approximately(demCut, -1f),
                            F("(b) a cut halfway toward REP's own rate reads {0:+0.00;-0.00} for REP and {1:+0.00;-0.00} for DEM", repCut, demCut));
                        // a rise to the dial's ceiling past DEM's own rate, from two standing laws: from the world's own (below DEM's rate) it ends nearer DEM's position
                        // than it began, so it reads FOR, the overshoot shrinking it below +1; from a planted law above DEM's rate it moves straight away, so it reads
                        // AGAINST - where the axis midpoint read the same FOR for both. The expected sign is the distances' own.
                        float demRate = SpatialBudgetVote.RateAt(uq, income, demAt), midpointDem = (5f - demAt) / 5f, standingIncome = income.Rate;
                        float dFrom = Mathf.Abs(SpatialBudgetVote.ScaleAt(uq, income, standingIncome) - demAt), dTo = Mathf.Abs(SpatialBudgetVote.ScaleAt(uq, income, income.MaxRate) - demAt);
                        float demRiseNearer = SpatialBudgetVote.Alignment(demAt, ceilingRates);
                        float plantedAbove = (demRate + income.MaxRate) / 2f;
                        income.Rate = plantedAbove;
                        float demRiseAway;
                        try { demRiseAway = SpatialBudgetVote.Alignment(demAt, SpatialBudgetVote.RatesOf(uq, ceiling)); }
                        finally { income.Rate = standingIncome; }
                        Check(standingIncome < demRate && demRate < income.MaxRate && Mathf.Sign(demRiseNearer) == Mathf.Sign(dFrom - dTo) && dTo > dFrom && demRiseNearer < 0f
                              && Mathf.Approximately(demRiseAway, -1f) && midpointDem > 0f,
                            F("(b) K72: a rise to the dial's ceiling ({0} %), far past DEM's own {1} %: from the standing {2} % ({3:0.000} points from DEM's position) to the ceiling ({4:0.000} points from it) it ends FARTHER - {5:+0.00;-0.00}, AGAINST (on the dial's map it ended nearer and read FOR); from a planted {6} % (restored after) it moves straight away - {7:+0.00;-0.00}, AGAINST; the axis midpoint read {8:+0.00;-0.00} for both",
                                Num(income.MaxRate), Num(demRate), Num(standingIncome), dFrom, dTo, demRiseNearer, Num(plantedAbove), demRiseAway, midpointDem));
                        var ceilings = new BudgetBill();
                        foreach (TaxLine t in uq.TaxLines.Where(t => t.IsImplemented)) { ceilings.TaxLines[t.Type] = t.MaxRate; }
                        BillConcern ceilingsConcern = UsBudgetProcedure.ConcernOf(uq, ceilings, UsBudgetProcedure.Act.Reconciliation);
                        float repCeilings = SpatialBudgetVote.Alignment(repAt, ceilingsConcern.Spatial), demCeilings = SpatialBudgetVote.Alignment(demAt, ceilingsConcern.Spatial);
                        Country se = WorldFactory.CreateDefault().GetCountry(CountryId.Sweden);
                        BillConcern swedish = ParliamentSystem.GetBudgetBillConcern(se, cut);
                        Check(ceilingsConcern.Spatial != null && ceilingsConcern.Spatial.Count == ceilings.TaxLines.Count && repCeilings < 0f && swedish.Spatial == null,
                            F("(b) GA-2-6's shape - a reconciliation with every levied rate at its ceiling ({0} rates): REP {1:+0.00;-0.00}, against; DEM {2:+0.00;-0.00} (reported); a country without a Congress (Sweden) carries no spatial set - its budget votes are unchanged",
                                ceilings.TaxLines.Count, repCeilings, demCeilings));

                        // the review's M-2: K72's consequences on the UNPLANTED world - the seeded law on both anchored lines (the desk worlds plant around them)
                        float plantedIncome = income.Rate;
                        TaxLine corporate = uq.TaxLines.First(t => t.Type == TaxType.CorporateTax && t.IsImplemented);
                        float plantedCorporate = corporate.Rate;
                        try
                        {
                            income.Rate = uq.BaselineTaxRates[TaxType.IncomeTax];
                            corporate.Rate = uq.BaselineTaxRates[TaxType.CorporateTax];
                            SpatialBudgetVote.TryUsScale(uq, corporate, out _, out float demCorporate, out _, out float repCorporate);
                            var incomeCut = new BudgetBill(); incomeCut.TaxLines[TaxType.IncomeTax] = income.Rate - 1f;
                            var corporateCut = new BudgetBill(); corporateCut.TaxLines[TaxType.CorporateTax] = corporate.Rate - 1f;
                            float repIncomeCut = SpatialBudgetVote.Alignment(repAt, SpatialBudgetVote.RatesOf(uq, incomeCut)), repCorporateCut = SpatialBudgetVote.Alignment(repAt, SpatialBudgetVote.RatesOf(uq, corporateCut));
                            BudgetBill repBook = UsMajorityReconciliation.Book(uq, "REP", out string repBookWhy);
                            float gap = demSourced - income.Rate, threshold = 2f * gap;
                            var riseUnder = new BudgetBill(); riseUnder.TaxLines[TaxType.IncomeTax] = income.Rate + threshold - 0.25f;
                            var riseOver = new BudgetBill(); riseOver.TaxLines[TaxType.IncomeTax] = income.Rate + threshold + 0.25f;
                            float demUnder = SpatialBudgetVote.Alignment(demAt, SpatialBudgetVote.RatesOf(uq, riseUnder)), demOver = SpatialBudgetVote.Alignment(demAt, SpatialBudgetVote.RatesOf(uq, riseOver));
                            Check(Mathf.Abs(repRateOf - income.Rate) < 1e-3f && Mathf.Abs(repCorporate - corporate.Rate) < 1e-3f && Mathf.Approximately(repIncomeCut, -1f) && Mathf.Approximately(repCorporateCut, -1f)
                                  && repBook == null && demUnder > 0f && demOver < 0f,
                                F("(b) THE REVIEW'S M-2, K72 UNPLANTED (the seeded law, {0} % income and {1} % corporate): REP's own rate IS the law on both lines ({2} %, {3} %), so a 1-point income cut reads {4:+0.00;-0.00} and a 1-point corporate cut {5:+0.00;-0.00} for REP, and REP's majority has no book (\"{6}\"); DEM's own income rate {7} % lies {8} points above the law, so DEM reads AGAINST any income rise past twice that, {9} points ({10:+0.00;-0.00} at {11} points, {12:+0.00;-0.00} at {13}); DEM's corporate rate {14} % (K111: as sourced)",
                                    Num(income.Rate), Num(corporate.Rate), Num(repRateOf), Num(repCorporate), repIncomeCut, repCorporateCut, repBookWhy ?? "a book was built", Num(demSourced), Num(gap), Num(threshold),
                                    demUnder, Num(threshold - 0.25f), demOver, Num(threshold + 0.25f), Num(demCorporate)));
                        }
                        finally { income.Rate = plantedIncome; corporate.Rate = plantedCorporate; }

                        // ============================================================ (c) the DEM player-president facing the REP Congress of record: SIGNED
                        (SimulationManager s, World w, Country u) = DemPresidentWorld(hosts);
                        bool president = Congress.PresidentOn(u, s.CurrentDate, out string shownAs, out string pp, out bool isPlayer) && isPlayer && pp == "DEM";
                        bool outParty = UsMajorityReconciliation.TryOutPartyMajority(u, s.CurrentDate, out string majority, out _) && majority == "REP";
                        Check(president && outParty && Congress.HouseMajority(u) == "REP" && Congress.SenateMajority(u, s.CurrentDate) == "REP" && u.MajorityReconciliationYear == 0,
                            F("(c) on {0}: the President of the day is {1} ({2}), the player's - the player-president; REP holds the House and the Senate of record - the party out of the White House holds both chambers", Day(s.CurrentDate), shownAs, pp));
                        TaxLine rate = u.TaxLines.First(t => t.Type == TaxType.IncomeTax);
                        float standing = rate.Rate;
                        (DateTime tabled, DateTime presented, BudgetBill book) = StepToDesk(s, w, null);
                        float bookRate = book != null && book.TaxLines.TryGetValue(TaxType.IncomeTax, out float br) ? br : float.NaN;
                        float move = book?.RateMoves != null && book.RateMoves.TryGetValue(TaxType.IncomeTax, out float mv) ? mv : float.NaN;
                        bool towardRep = UsMajorityReconciliation.TryPartyRate(u, "REP", rate, out float repRateNow) && bookRate < standing && bookRate >= repRateNow - 1e-4f;
                        Check(tabled == UsCongressDiagnostic.Day119.AddDays(1) && UsMajorityReconciliation.InWindow(tabled) && book != null && book.TabledBy == "REP" && !book.GovernmentBill && book.SpendingPercentChanges.Count == 0,
                            F("(c) TABLED on {0}, the first day the tick runs in the window (the world opens on {1}, 1105(a)'s request day): REP's book - the income tax {2} % to {3} % (toward REP's {4} %, never past it; the Finance partner's yearly step of {5} points of GDP over the household base), tabled by REP, not the government's",
                                Day(tabled), Day(UsCongressDiagnostic.Day119), Num(standing), Num(bookRate), Num(repRateNow), Num(FinancePartner.StepPointsPerYear)));
                        SimulationManager.DocketItem onDesk = s.GetDocket(CountryId.USA).FirstOrDefault(i => i.Title != null && i.Title.StartsWith("Reconciliation (the REP majority)", StringComparison.Ordinal));
                        DivisionRecord senate = onDesk.Entry?.Passage, house = senate?.OtherChamber;
                        int repHouse = house?.Sides.Where(x => x.Abbrev == "REP" && x.Side > 0).Sum(x => x.Seats) ?? 0, demHouseAgainst = house?.Sides.Where(x => x.Abbrev == "DEM" && x.Side < 0).Sum(x => x.Seats) ?? 0;
                        Check(onDesk.Entry != null && towardRep && presented == tabled.AddDays(ParliamentSystem.BillDurationDays) && presented < UsMajorityReconciliation.Deadline(presented)
                              && house != null && house.Chamber == Congress.House && house.Passed && senate.Passed && senate.Reconciliation && !senate.Cloture && repHouse > 0 && demHouseAgainst > 0
                              && rate.Rate == standing && s.GetPendingMajorityReconciliation(CountryId.USA)?.Docket != null && u.MajorityReconciliationYear == 2025
                              && Mathf.Abs(move - (bookRate - standing)) < 1e-4f && move < 0f,
                            F("(c) ON THE DESK on {0}, before 1 October: \"{1}\" - the House {2} REP for, {3} DEM against; the Senate by a majority (reconciliation, no cloture); HELD for the player-president, nothing applied - the rate still {4} %; the year marked {5}; its move {6} points from the law of the vote day",
                                Day(presented), onDesk.Title, repHouse, demHouseAgainst, Num(rate.Rate), u.MajorityReconciliationYear, Num(move)));
                        string spatialReason = senate?.Sides.FirstOrDefault(x => x.Abbrev == "DEM")?.Reason ?? string.Empty;
                        Check(spatialReason.Contains("spatial, against the standing law"),
                            F("(c) q16 on the vote: DEM's reason reads the rates against the standing law - \"{0}\"", spatialReason));

                        // the save (f) - cut here, while the bill is on the desk
                        string json = SaveGameService.Serialize(SaveGameService.CreateSaveGame(s, w, CountryId.USA, null));

                        // the review's M1: a rate law and a fund made while the bill waits on the desk - planted, as another law would leave them
                        float plantedLaw = standing + 3f;
                        rate.Rate = plantedLaw;
                        SovereignWealthFund fundWas = u.SovereignWealthFund;
                        SovereignWealthFund plantedFund = fundWas ?? new SovereignWealthFund();
                        u.SovereignWealthFund = plantedFund;
                        bool signs = s.SignDocketBill(CountryId.USA, senate, out string signWhy);
                        Check(signs && Mathf.Abs(rate.Rate - (plantedLaw + move)) < 1e-3f && ReferenceEquals(u.SovereignWealthFund, plantedFund) && s.GetPendingMajorityReconciliation(CountryId.USA) == null && !s.GetDocket(CountryId.USA).Any(),
                            F("(c) SIGNED on {0} ({1}), law at once - RATES ALONE, FROM THE LAW OF THE DAY (the review's M1): a rate law planted while it waited ({2} %) is kept and the move ({3}) applied from it - the income tax {4} % (the vote day's figure was {5} %); a fund made in between survives; the slot and the desk empty",
                                Day(s.CurrentDate), signWhy ?? "accepted", Num(plantedLaw), Num(move), Num(rate.Rate), Num(bookRate)));
                        u.SovereignWealthFund = fundWas;
                        DateTime after = s.CurrentDate.AddDays(40);
                        Step(s, w, after);
                        bool noneDueAgain = true;
                        for (DateTime d = s.CurrentDate; d <= UsMajorityReconciliation.WindowCloses(d) && d.Year == 2025; d = d.AddDays(1)) { if (UsMajorityReconciliation.Due(u, d, out _)) { noneDueAgain = false; break; } }
                        Check(s.GetPendingMajorityReconciliation(CountryId.USA) == null && noneDueAgain && u.MajorityReconciliationYear == 2025 && UsMajorityReconciliation.TryOutPartyMajority(u, s.CurrentDate, out _, out _),
                            F("(c) ONCE A FISCAL YEAR: stepped to {0} with REP still holding both chambers, nothing more is tabled, and none is due on any day left in the window (to {1})", Day(after), Day(UsMajorityReconciliation.WindowCloses(after))));

                        // ============================================================ (d) the same world, VETOED
                        (SimulationManager sv, World wv, Country uv) = DemPresidentWorld(hosts);
                        TaxLine rateV = uv.TaxLines.First(t => t.Type == TaxType.IncomeTax);
                        float standingV = rateV.Rate;
                        // the rate law planted between the desk's law and DEM's own rate (K72's scale): above it DEM would want REP's cut too and override its own president
                        float plantedV = SpatialBudgetVote.TryUsScale(uv, rateV, out _, out float demV, out _, out float _) && demV > standingV ? (standingV + demV) / 2f : standingV + 3f;
                        (DateTime tabledV, DateTime _, BudgetBill heldV) = StepToDesk(sv, wv, () => rateV.Rate = plantedV);   // the review's M1: a rate law the day after the tabling
                        float voteDayV = heldV != null && heldV.TaxLines.TryGetValue(TaxType.IncomeTax, out float vd) ? vd : float.NaN;
                        Check(heldV?.RateMoves != null && voteDayV < plantedV && Mathf.Abs(heldV.RateMoves[TaxType.IncomeTax] - (voteDayV - plantedV)) < 1e-4f,   // the move measured from the planted law: the book was built on the vote day
                            F("(d) BUILT AGAIN ON THE VOTE DAY (the review's M1): tabled {0} on the law of {1} %, a rate law planted the next day ({2} %) - Congress votes the book from it: the income tax {2} % to {3} %",
                                Day(tabledV), Num(standingV), Num(plantedV), Num(voteDayV)));
                        standingV = plantedV;
                        DivisionRecord passageV = sv.GetDocket(CountryId.USA).FirstOrDefault(i => i.Title != null && i.Title.StartsWith("Reconciliation (the REP majority)", StringComparison.Ordinal)).Entry?.Passage;
                        int seen = uv.Divisions.Entries.Count > 0 ? uv.Divisions.Entries.Last().Number : 0;
                        bool vetoes = passageV != null && sv.VetoDocketBill(CountryId.USA, passageV, out _);
                        DivisionRecord sustained = uv.Divisions.Entries.Where(r => r.Number > seen).FirstOrDefault(r => r.Title.StartsWith("Vetoed by the President (you, ", StringComparison.Ordinal));
                        int forOverride = sustained?.Sides.Where(x => x.Side > 0).Sum(x => x.Seats) ?? 0;
                        Check(vetoes && sustained != null && !sustained.Passed && sustained.Chamber == Congress.House && sustained.Required > forOverride && rateV.Rate == standingV
                              && sv.GetPendingMajorityReconciliation(CountryId.USA) == null && !sv.GetDocket(CountryId.USA).Any(),
                            F("(d) VETOED on {0}: the override fails - \"{1}\", {2} for of {3} needed; the bill falls and the standing rates hold (the income tax {4} %)",
                                Day(sv.CurrentDate), sustained?.Title, forOverride, sustained?.Required ?? 0, Num(rateV.Rate)));

                        // ============================================================ (e) a president whose party holds Congress receives none
                        (SimulationManager sr, World wr, Country ur) = UsCongressDiagnostic.OpenUsa(hosts, UsCongressDiagnostic.Day119, "REP");
                        var trump = new UsPresidentialElection.Contest { PollingDay = new DateTime(2024, 11, 5), TakesOffice = Oath, Winner = "Donald J. Trump", WinnerParty = "REP", ElectorsAppointed = 538, Line = "planted (UsMajorityReconciliationDiagnostic)" };
                        trump.Electors.Add(new UsPresidentialElection.Tally { Candidate = "Donald J. Trump", Party = "REP", Votes = 312 });
                        trump.Electors.Add(new UsPresidentialElection.Tally { Candidate = "Kamala D. Harris", Party = "DEM", Votes = 226 });
                        ur.UsPresidentialElections.Add(trump);
                        ur.Government = UsPresidentialElection.GovernmentOf(trump, trump.TakesOffice);
                        bool repPresident = Congress.PresidentOn(ur, sr.CurrentDate, out _, out string rpp, out bool rPlayer) && rPlayer && rpp == "REP";
                        bool everHeld = false;
                        DateTime until = UsCongressDiagnostic.Day119.AddDays(ParliamentSystem.BillDurationDays + 10);
                        while (sr.CurrentDate < until) { StepOne(sr, wr); everHeld |= sr.GetPendingMajorityReconciliation(CountryId.USA) != null; }
                        bool dueOnAnyDay = false;
                        for (DateTime d = UsMajorityReconciliation.WindowOpens(until); d <= UsMajorityReconciliation.WindowCloses(until); d = d.AddDays(1)) { if (UsMajorityReconciliation.Due(ur, d, out _)) { dueOnAnyDay = true; break; } }
                        Check(repPresident && !everHeld && !dueOnAnyDay && ur.MajorityReconciliationYear == 0 && !UsMajorityReconciliation.TryOutPartyMajority(ur, sr.CurrentDate, out _, out _)
                              && !sr.GetDocket(CountryId.USA).Any(i => i.Title != null && i.Title.StartsWith("Reconciliation (the", StringComparison.Ordinal)),
                            F("(e) A PRESIDENT WHOSE PARTY HOLDS CONGRESS RECEIVES NONE: under a planted game-elected Trump, the player REP, REP holding both chambers - stepped {0} to {1}, nothing tabled and nothing on his desk; none due on any day of the window", Day(UsCongressDiagnostic.Day119), Day(until)));

                        // ============================================================ (f) the save, cut while the bill is on the desk
                        SaveGame cutSave = SaveGameService.Deserialize(json);
                        BudgetBill savedBill = cutSave.Sim.PendingMajorityReconciliations != null && cutSave.Sim.PendingMajorityReconciliations.TryGetValue(CountryId.USA, out BudgetBill sbb) ? sbb : null;
                        Check(cutSave.SaveVersion == SaveGameService.CurrentSaveVersion && savedBill?.Docket?.Passage != null && savedBill.TabledBy == "REP" && cutSave.World.GetCountry(CountryId.USA).MajorityReconciliationYear == 2025,
                            F("(f) a save cut on {0} with the bill on the desk, at format {1} (unchanged - additive): the bill, its passage and the year's mark ride it", Day(presented), cutSave.SaveVersion));
                        (SimulationManager adopted, PoliSim.UI.GameController _) = PresidencyDiagnostic.Adopt(cutSave, hosts, out string error);
                        if (adopted == null) { Check(false, "(f) the save loaded into the game: " + error); }
                        else
                        {
                            Country la = adopted.World.GetCountry(CountryId.USA);
                            SimulationManager.DocketItem loaded = adopted.GetDocket(CountryId.USA).FirstOrDefault(i => i.Title != null && i.Title.StartsWith("Reconciliation (the REP majority)", StringComparison.Ordinal));
                            TaxLine loadedRate = la.TaxLines.First(t => t.Type == TaxType.IncomeTax);
                            bool answered = loaded.Entry != null && adopted.PlayerAnswersDocket(CountryId.USA) && adopted.SignDocketBill(CountryId.USA, loaded.Entry.Passage, out _);
                            Check(answered && Mathf.Approximately(loadedRate.Rate, bookRate) && adopted.GetPendingMajorityReconciliation(CountryId.USA) == null,
                                F("(f) loaded INTO THE GAME: the bill is on the player-president's desk and he answers it there - signed, the income tax {0} %, as in the world it was cut from", Num(loadedRate.Rate)));
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
                UsVeto.ResetDrawCalls();
            }

            sb.Append(failures == 0 ? "=== UsMajorityReconciliationDiagnostic: the REP majority's reconciliation reaches the DEM player-president once before 1 October - signed it takes effect, vetoed the override fails and the standing rates hold; none for a president whose party holds Congress; the spatial vote reads the rates against the standing law ===\n"
                                    : F("=== UsMajorityReconciliationDiagnostic: {0} FAILURE(S) ===\n", failures));
            if (failures == 0) { Debug.Log(sb.ToString()); CheckExit.Finish(0); }
            else { Debug.LogError(sb.ToString()); CheckExit.Finish(1); }
        }

        /// <summary>The 119th of record on 2025-02-03, the player DEM, with a planted game-elected Harris sworn 2025-01-20 (her presidency installed as the oath
        /// installs it) - so the president of the day is the player's, facing the REP Congress of record.</summary>
        private static (SimulationManager, World, Country) DemPresidentWorld(List<GameObject> hosts)
        {
            (SimulationManager s, World w, Country u) = UsCongressDiagnostic.OpenUsa(hosts, UsCongressDiagnostic.Day119, "DEM");
            var harris = new UsPresidentialElection.Contest { PollingDay = new DateTime(2024, 11, 5), TakesOffice = Oath, Winner = "Kamala D. Harris", WinnerParty = "DEM", ElectorsAppointed = 538, Line = "planted (UsMajorityReconciliationDiagnostic)" };
            harris.Electors.Add(new UsPresidentialElection.Tally { Candidate = "Kamala D. Harris", Party = "DEM", Votes = 270 });
            harris.Electors.Add(new UsPresidentialElection.Tally { Candidate = "Donald J. Trump", Party = "REP", Votes = 268 });
            u.UsPresidentialElections.Add(harris);
            u.Government = UsPresidentialElection.GovernmentOf(harris, harris.TakesOffice);
            // K72: under the sourced scale REP's own income rate is the TCJA's, the law in force at the seed, so the world's standing law gives a REP majority no
            // book to send; the desk's cases plant the standing income tax between the two parties' own rates (DECLARED for the diagnostic) - REP's book cuts toward its rate
            TaxLine income = u.TaxLines.First(t => t.Type == TaxType.IncomeTax);
            if (SpatialBudgetVote.TryUsScale(u, income, out _, out float demRate, out _, out float repRate)) { income.Rate = (demRate + repRate) / 2f; }
            return (s, w, u);
        }


        /// <summary>Steps the world day by day until the majority's reconciliation is on the desk (at most a bill's course and ten days): the day it was tabled, the
        /// day it was presented, and the bill as tabled.</summary>
        private static (DateTime Tabled, DateTime Presented, BudgetBill Held) StepToDesk(SimulationManager s, World w, Action afterTabled)
        {
            DateTime tabled = DateTime.MinValue, presented = DateTime.MinValue;
            BudgetBill held = null;
            DateTime limit = s.CurrentDate.AddDays(ParliamentSystem.BillDurationDays + 10);
            while (s.CurrentDate < limit)
            {
                StepOne(s, w);
                BudgetBill pending = s.GetPendingMajorityReconciliation(CountryId.USA);
                if (pending != null && tabled == DateTime.MinValue) { tabled = s.CurrentDate; afterTabled?.Invoke(); }
                if (pending?.Docket != null) { presented = s.CurrentDate; held = pending; break; }   // the bill the vote day built (the review's M1), as it waits on the desk
            }
            return (tabled, presented, held);
        }

        /// <summary>One day in the controller's order (the day, the player's day tick, the boundary's turn).</summary>
        private static void StepOne(SimulationManager s, World w)
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

        private static void Step(SimulationManager s, World w, DateTime until) { while (s.CurrentDate < until) { StepOne(s, w); } }

        private static float Position(Country u, string party)
        {
            foreach (PoliticalParty p in PartySystems.For(u.Id)) { if (p.Abbrev == party) { return StanceModel.Position(p, StanceAxis.Redistribution, false); } }
            return float.NaN;
        }

        private static string Num(float v) => v.ToString("0.###", CultureInfo.InvariantCulture);
        private static string Day(DateTime d) => d == DateTime.MinValue ? "none" : d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        private static string F(string format, params object[] args) => string.Format(CultureInfo.InvariantCulture, format, args);
    }
}
