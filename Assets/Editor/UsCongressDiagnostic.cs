using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using PoliSim.Data;
using PoliSim.Elections;
using PoliSim.Simulation;
using UnityEngine;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// PS-6 US-18 (`docs/specs/USA_STAGE_PLAN.md`, "Bills through both chambers"; R-US9 for what rides reconciliation; R-US10 (a) for the caucus; R-US17's
    /// terms for the divisions' sides): THE CONGRESS DIAGNOSTIC the done-when names, on worlds opened as a new game opens them (the epoch, the default world,
    /// the player's country the USA, the record seated on its date through the day loop's own `SimulationManager.SeatTheRecordOnItsDate`):
    /// <list type="bullet">
    /// <item>(a) the rules' words held to their saved pages by SHA-256: Rule XXII ¶2 ([SEN-RULES]), 2 U.S.C. 641(a), (e)(2) and (g) ([USC-641]), Art. I §7
    /// cl. 1 and 2 ([CONST]);</item>
    /// <item>(b) the 119th of record: a party-line REP ordinary bill passes the House and fails cloture; a party-line REP reconciliation passes both chambers
    /// without cloture; an uncontested bill (nothing asked) and a bill both blocs stand for pass both - each through the projection AND the gate's own
    /// records (`Congress.Divide`): the chambers' divisions, the order, the second carrying the first, the day's loss;</item>
    /// <item>(c) the 118th of record: a party-line DEM reconciliation passes the Senate with its caucus - the independents counted with DEM (R-US10 (a)), the
    /// letter D alone short - and dies in the REP House (the House's origin: it never reaches the Senate; a benefits-only reconciliation begun in the
    /// Senate: passed there, killed in the House); a party-line REP ordinary bill passes the House and dies at cloture;</item>
    /// <item>(d) cloture is three fifths of the senators sworn ON THE DAY, never a fixed sixty - the 119th's opening days with their vacancies; the Vice
    /// President decides an equal division on the majority path by his party's stance, the record's vice president of the day, and has no cloture vote; the House's tie fails (a planted 217-217 House the single-chamber count would pass on its lean); an undecided bloc is no yea at cloture; the government's own budget goes through both chambers since US-22 (the review's M1, closed)</item>
    /// <item>(e) one stance per party, the same in both chambers (R-US17 premise (B)); the Senate's sides sum to the senators sworn;</item>
    /// <item>(f) scope: Sweden, Germany and Poland vote through their one chamber exactly as before (the dated verdict the count's, one record per bill, no
    /// chamber field moved); an AI USA (a Sweden player's world) seats no Senate and is not voted through two chambers;</item>
    /// <item>(g) the DECLARED reading of what rides reconciliation, printed; and the PASS-RATE SWEEP over the game's bill kinds in the 118th and 119th of
    /// record - REPORTED, NOT TUNED: no figure of it is asserted.</item>
    /// </list>
    /// The random streams are restored after, the epoch by its scope, and every host destroyed, so no later check starts elsewhere.
    /// </summary>
    public static class UsCongressDiagnostic
    {
        private const string RulesPage = "ElectionsData/usa/raw/records/wayback_senate_rules_of_the_senate_20260831.html";
        private const string ReconciliationPage = "ElectionsData/usa/raw/records/govinfo_uscode_2_641_reconciliation.html";
        private const string ConstitutionPage = "ElectionsData/usa/raw/records/archives_constitution_transcript.html";
        private const string RulesWords = "three-fifths of the Senators duly chosen and sworn";
        private static readonly string[] ReconciliationWords =
        {
            "A concurrent resolution on the budget for any fiscal year",
            "new entitlement authority which is to become effective",
            "the total amount by which revenues are to be changed",
            "Debate in the Senate on any reconciliation bill reported under subsection (b)",
            "shall be limited to not more than 20 hours",
            "old-age, survivors, and disability insurance program",
        };
        private static readonly string[] ConstitutionWords =
        {
            "All Bills for raising Revenue shall originate in the House of Representatives",
            "Every Bill which shall have passed the House of Representatives and the Senate",
        };

        /// <summary>A day of the 119th of record with every seat held (after the oaths of 21 January 2025).</summary>
        internal static readonly DateTime Day119 = new DateTime(2025, 2, 3);
        /// <summary>A day of the 119th of record with seats vacant (senate_record.md readings 1-3; `SenateChamberDiagnostic` (d) holds the vacancies).</summary>
        private static readonly DateTime Vacancies119 = new DateTime(2025, 1, 10);
        /// <summary>The last day of the 118th's vice president of record - read for the tie on a 119th world (the record's vice president is a function of the date).</summary>
        internal static readonly DateTime HarrisLastDay = new DateTime(2025, 1, 19);
        private const int Seed = 777;

        public static void Run()
        {
            CheckExit.ArmLogFold();
            var sb = new StringBuilder("=== UsCongressDiagnostic (PS-6 US-18, R-US9, R-US10 (a), R-US17's terms): bills through both chambers ===\n");
            int failures = 0;
            void Check(bool ok, string what) { if (!ok) { failures++; } sb.Append(ok ? "    ok        " : "    FAIL      ").Append(what).Append('\n'); }
            var hosts = new List<GameObject>();
            int seedWas = SimulationRandom.MasterSeed;
            Dictionary<SimulationRandom.Stream, int> drawsWas = SimulationRandom.CaptureDrawCounts();
            try
            {
                // ---- (a) the rules' words, held to their saved pages
                Check(PresidencyDiagnostic.Page(RulesPage, out string rules) && rules.Contains(RulesWords),
                    F("(a) Rule XXII ¶2 ([SEN-RULES], {0}) held to its SHA-256, holding \"{1}\"", System.IO.Path.GetFileName(RulesPage), RulesWords));
                Check(PresidencyDiagnostic.Page(ReconciliationPage, out string usc) && ReconciliationWords.All(usc.Contains),
                    F("(a) 2 U.S.C. 641 ([USC-641], {0}) held to its SHA-256, holding \"{1}\"", System.IO.Path.GetFileName(ReconciliationPage), string.Join("\" ... \"", ReconciliationWords)));
                Check(PresidencyDiagnostic.Page(ConstitutionPage, out string constitution) && ConstitutionWords.All(constitution.Contains),
                    F("(a) Art. I §7 cl. 1 and 2 ([CONST], {0}) held to its SHA-256, holding \"{1}\"", System.IO.Path.GetFileName(ConstitutionPage), string.Join("\" ... \"", ConstitutionWords)));
                Check(Congress.ClotureLine(100) == 60 && Congress.ClotureLine(99) == 60 && Congress.ClotureLine(98) == 59 && Congress.ClotureLine(97) == 59 && Congress.ClotureLine(96) == 58 && Congress.ClotureLine(0) == 0,
                    F("(a) the cloture line, three fifths of the sworn rounded up (floor((3W + 4) / 5)): 100 -> {0}, 99 -> {1}, 98 -> {2}, 97 -> {3}, 96 -> {4}",
                        Congress.ClotureLine(100), Congress.ClotureLine(99), Congress.ClotureLine(98), Congress.ClotureLine(97), Congress.ClotureLine(96)));

                // ---- (g) the reading, printed
                sb.Append("    reading   WHAT RIDES RECONCILIATION (DECLARED; R-US9 (a) applied to the game's bill kinds): ")
                  .Append(string.Join("; ", ((Congress.Kind[])Enum.GetValues(typeof(Congress.Kind))).Select(k => k + (k == Congress.Kind.Budget ? " by its content (no spending line, not the pension age)" : Congress.IsReconciliationKind(k) ? " reconciliation" : " cloture"))))
                  .Append("; the once-a-year limit NOT MODELLED (no concurrent resolution on the budget is modelled)\n");

                using (SimulationManager.EpochScope())
                {
                    try
                    {
                        // =========================== the 119th of record
                        (SimulationManager sim, World world, Country usa) = OpenUsa(hosts, Day119, "REP");
                        Check(UsSenate.CongressOn(Day119) == 119 && Same(usa.ParliamentSeats, PartySystems.InitialSeats(CountryId.USA, ElectionVintage.Usa2024))
                              && Same(usa.SecondChamberSeats, WorldClock.SecondChamberOfRecord(CountryId.USA, Day119)) && Congress.Applies(usa) && usa.Government?.PmParty == "REP",
                            F("(b) the 119th of record on {0}: the House {1}, the Senate {2} ({3} sworn), the president {4} - a bill goes through both chambers here",
                                Day(Day119), Seats(usa.ParliamentSeats), Seats(usa.SecondChamberSeats), Congress.Sworn(usa.SecondChamberSeats), usa.Government?.Executive));

                        // a party-line REP ordinary bill: the House passes it, cloture fails
                        BillConcern ordinary = ParliamentSystem.GetCrimeJusticeBillConcern(usa, CrimeBill(usa, "REP"));
                        Congress.Verdict v = Congress.Project(usa, ordinary, sim.CurrentDate);
                        Check(PartyLine(v, "REP", "DEM") && !ordinary.Reconciliation && v.House.Passed && v.Senate.Cloture && !v.Senate.ClotureInvoked && !v.Senate.Passed
                              && v.Senate.Required == Congress.ClotureLine(v.Senate.Sworn) && v.StoppedIn == Congress.Senate && !v.Passed && !ParliamentSystem.WouldBillPass(usa, ordinary, sim.CurrentDate),
                            F("(b) a party-line REP ordinary bill (a crime-and-justice bill, REP's own): {0} - {1}; {2}", Congress.StopWords(CountryId.USA, v), string.Join("; ", Congress.OrderWords(CountryId.USA, v)), Sides(v)));
                        Check(Gate(usa, "Crime & Justice bill (US-18 diagnostic)", ordinary, sim.CurrentDate, Congress.House, wantPassed: false, wantChambers: 2, out string ordinaryRecords), "(b) through the gate: " + ordinaryRecords);

                        // a party-line REP reconciliation: both chambers pass it, no cloture
                        BudgetBill cut = RatesBill(usa, "REP");
                        BillConcern reconciliation = ParliamentSystem.GetBudgetBillConcern(usa, cut);
                        v = Congress.Project(usa, reconciliation, sim.CurrentDate);
                        Check(PartyLine(v, "REP", "DEM") && reconciliation.Reconciliation && Congress.RidesReconciliation(usa, cut) && v.House.Passed && !v.Senate.Cloture && v.Senate.Required == 0
                              && v.Senate.For > v.Senate.Against && v.Senate.Passed && v.Passed && ParliamentSystem.WouldBillPass(usa, reconciliation, sim.CurrentDate),
                            F("(b) a party-line REP reconciliation (a budget moving the {0} rate alone): {1} - {2}; {3}", Describe(cut), Congress.StopWords(CountryId.USA, v), string.Join("; ", Congress.OrderWords(CountryId.USA, v)), Sides(v)));
                        Check(Gate(usa, "Annual budget bill (US-18 diagnostic, reconciliation)", reconciliation, sim.CurrentDate, Congress.House, wantPassed: true, wantChambers: 2, out string reconciliationRecords), "(b) through the gate: " + reconciliationRecords);

                        // an uncontested bill - nothing asked - and a bill both blocs stand for
                        BillConcern nothing = ParliamentSystem.GetBudgetBillConcern(usa, new BudgetBill());
                        v = Congress.Project(usa, nothing, sim.CurrentDate);
                        Check(nothing.IsEmpty && v.House.Passed && v.Senate.Passed && v.Passed && !v.House.Contested && ParliamentSystem.WouldBillPass(usa, nothing, sim.CurrentDate),
                            F("(b) an uncontested bill (a budget that moves nothing - the count's own uncontested): {0}", Congress.StopWords(CountryId.USA, v)));
                        BudgetBill both = SpendingBill(usa, SpendingCategory.Transportation, 10f);
                        BillConcern bipartisan = ParliamentSystem.GetBudgetBillConcern(usa, both);
                        v = Congress.Project(usa, bipartisan, sim.CurrentDate);
                        bool bothFor = v.House.Stances.All(s => s.Side > 0) && v.Senate.Stances.All(s => s.Side > 0) && v.House.Stances.Count >= 2;
                        Check(bothFor && !bipartisan.Reconciliation && v.House.Passed && v.Senate.Cloture && v.Senate.ClotureInvoked && v.Senate.Passed && v.Passed,
                            F("(b) a bill both blocs stand for (REP's own, Transportation +10 %: spending, so cloture): {0} - {1}; {2}", Congress.StopWords(CountryId.USA, v), string.Join("; ", Congress.OrderWords(CountryId.USA, v)), Sides(v)));
                        Check(Gate(usa, "Annual budget bill (US-18 diagnostic, both blocs)", bipartisan, sim.CurrentDate, Congress.House, wantPassed: true, wantChambers: 2, out string bothRecords), "(b) through the gate: " + bothRecords);

                        // (e) one stance per party in both chambers, over every bill asked on this world
                        foreach (BillConcern c in new[] { ordinary, reconciliation, bipartisan })
                        {
                            Congress.Verdict w = Congress.Project(usa, c, sim.CurrentDate);
                            bool one = w.Senate.Stances.All(s => w.House.Stances.Any(h => h.Party.Abbrev == s.Party.Abbrev && h.Side == s.Side && h.Alignment == s.Alignment))
                                       && w.Senate.Stances.Sum(s => s.Seats) == w.Senate.Sworn && w.House.Stances.Sum(s => s.Seats) == usa.ParliamentSeats.Values.Sum();
                            Check(one, F("(e) one stance a party, the same in both chambers (R-US17 premise (B)), and every seat in its chamber's sides: {0}", Sides(w)));
                        }

                        // (d) the Vice President on an equal division (planted 50-50): his party's stance decides the majority path; none at cloture
                        Dictionary<string, int> seated = usa.SecondChamberSeats;
                        try
                        {
                            usa.SecondChamberSeats = new Dictionary<string, int> { { "REP", 50 }, { "DEM", 50 } };
                            Congress.Verdict withVance = Congress.Project(usa, reconciliation, sim.CurrentDate), withHarris = Congress.Project(usa, reconciliation, HarrisLastDay);
                            Congress.Verdict clotureTie = Congress.Project(usa, ordinary, sim.CurrentDate);
                            Check(withVance.Senate.For == withVance.Senate.Against && withVance.Senate.VicePresidentParty == "REP" && withVance.Senate.VicePresidentVote > 0 && withVance.Senate.Passed
                                  && withHarris.Senate.VicePresidentParty == "DEM" && withHarris.Senate.VicePresidentVote < 0 && !withHarris.Senate.Passed
                                  && clotureTie.Senate.VicePresidentParty == null && clotureTie.Senate.VicePresidentVote == 0 && !clotureTie.Senate.Passed && clotureTie.Senate.Required == 60,
                                F("(d) a planted 50-50 Senate: the REP reconciliation {0} with the vice president of {1} ({2}) and {3} with the vice president of {4} ({5}); at cloture the tie is {6} - the vice president has no cloture vote",
                                    withVance.Senate.Passed ? "carries" : "fails", Day(sim.CurrentDate), withVance.Senate.VicePresidentParty, withHarris.Senate.Passed ? "carries" : "fails", Day(HarrisLastDay),
                                    withHarris.Senate.VicePresidentParty, clotureTie.Senate.Passed ? "carried" : "lost"));
                            Check(Gate(usa, "Annual budget bill (US-18 diagnostic, the tie)", reconciliation, sim.CurrentDate, Congress.House, wantPassed: true, wantChambers: 2, out string tieRecords)
                                  && usa.Divisions.Entries.Last().VicePresidentParty == "REP" && usa.Divisions.Entries.Last().VicePresidentVote == 1,
                                "(d) the tie through the gate, the vice president's vote on the Senate's record: " + tieRecords);
                        }
                        finally { usa.SecondChamberSeats = seated; }

                        // (d) THE HOUSE'S TIE FAILS - a majority of those voting: a planted 217-217 House on REP's crime bill, which the single-chamber count (the
                        // chamber's count before US-18, the lean breaking a tie) passes
                        Dictionary<string, int> houseSeated = usa.ParliamentSeats;
                        try
                        {
                            usa.ParliamentSeats = new Dictionary<string, int> { { "REP", 217 }, { "DEM", 217 } };
                            Congress.Verdict houseTie = Congress.Project(usa, ordinary, sim.CurrentDate);
                            float lean = ParliamentSystem.GetSeatWeightedAlignment(usa, ordinary);
                            bool oldCount = ParliamentSystem.WouldBillPass(usa, ordinary);
                            Check(houseTie.House.For == 217 && houseTie.House.Against == 217 && !houseTie.House.Passed && houseTie.StoppedIn == Congress.House && !houseTie.Passed && lean > 0f && oldCount,
                                F("(d) a planted 217-217 House on REP's crime bill: {0} for, {1} against - rejected, a tie failing a majority of those voting; the single-chamber count would pass it on the lean {2:+0.000;-0.000}",
                                    houseTie.House.For, houseTie.House.Against, lean));
                        }
                        finally { usa.ParliamentSeats = houseSeated; }

                        // (d) AN UNDECIDED BLOC IS NO YEA AT CLOTURE: a planted bill DEM stands undecided on (galtan toward 10 against lrecon toward 0, weighted so its
                        // two positions cancel) - at cloture REP's yeas alone fall short of three fifths of the sworn though no senator stands against; by
                        // reconciliation (a majority of those voting) the same sides carry it
                        BillConcern undecidedBloc = new BillConcern { Direction = 1f, Author = "REP" }.Add(StanceAxis.Galtan, 1f).Add(StanceAxis.LrEcon, -2f);
                        BillConcern undecidedBlocR = new BillConcern { Direction = 1f, Author = "REP", Reconciliation = true }.Add(StanceAxis.Galtan, 1f).Add(StanceAxis.LrEcon, -2f);
                        Congress.Verdict abstaining = Congress.Project(usa, undecidedBloc, sim.CurrentDate), abstainingR = Congress.Project(usa, undecidedBlocR, sim.CurrentDate);
                        int demSeated = usa.SecondChamberSeats.TryGetValue("DEM", out int demHeld) ? demHeld : 0;
                        bool demUndecided = abstaining.Senate.Stances.Any(s => s.Party.Abbrev == "DEM" && s.Measured && s.Side == 0) && abstaining.House.Stances.Any(s => s.Party.Abbrev == "DEM" && s.Measured && s.Side == 0);
                        Check(demUndecided && abstaining.Senate.Against == 0 && abstaining.Senate.Abstaining == demSeated && abstaining.Senate.For > 0 && abstaining.Senate.For < abstaining.Senate.Required
                              && !abstaining.Senate.ClotureInvoked && !abstaining.Senate.Passed && abstaining.House.Passed && abstainingR.Senate.Passed && !abstainingR.Senate.Cloture,
                            F("(d) an undecided bloc at cloture: DEM undecided, REP for - the Senate {0} for, {1} against, {2} undecided, {3} needed of {4} sworn: cloture not invoked, the undecided counting as no yea; by reconciliation the same sides carry it {0}-{1}",
                                abstaining.Senate.For, abstaining.Senate.Against, abstaining.Senate.Abstaining, abstaining.Senate.Required, abstaining.Senate.Sworn));

                        // PS-6 US-22 (closing the review's M1, US-18's DECLARED asymmetry): THE GOVERNMENT'S OWN BUDGET GOES THROUGH BOTH CHAMBERS - a government-authored
                        // concern is voted by the House and then the Senate as every bill of a Congress is (the budget itself as its acts, UsBudgetProcedureDiagnostic);
                        // the gate writes both chambers' records and the dated verdict is the projection's
                        BudgetBill governmentBudget = SpendingBill(usa, SpendingCategory.Transportation, -2f);
                        governmentBudget.GovernmentBill = true;
                        BillConcern government = ParliamentSystem.GetBudgetBillConcern(usa, governmentBudget);
                        int beforeGov = usa.Divisions.Entries.Count > 0 ? usa.Divisions.Entries.Last().Number : 0;
                        DivisionRecord govRecord = ParliamentSystem.Divide(usa, "Annual budget: the government's bill (US-18 diagnostic)", government, sim.CurrentDate, out bool govPassed);
                        List<DivisionRecord> govWritten = usa.Divisions.Entries.Where(r => r.Number > beforeGov).ToList();
                        Congress.Verdict govVerdict = Congress.Project(usa, government, sim.CurrentDate, Congress.OriginOf(usa, government, sim.CurrentDate));
                        Check(government.GovernmentAuthored && Congress.VotesBothChambers(usa, government) && Congress.VotesBothChambers(usa, ordinary) && govWritten.Count == (govVerdict.House.Passed ? 2 : 1) && govWritten[0].Chamber == Congress.House
                              && govPassed == govVerdict.Passed && govPassed == ParliamentSystem.WouldBillPass(usa, government, sim.CurrentDate) && (govWritten.Count == 1 || (govWritten[1].Chamber == Congress.Senate && ReferenceEquals(govRecord.OtherChamber, govWritten[0]))),
                            F("(d) the government's own budget (a Transportation cut, the AI's bill): through both chambers since US-22 - the House {0}{1}; the dated verdict the projection's", govVerdict.House.Passed ? "passes it" : "rejects it", govVerdict.House.Passed ? ", then the Senate " + (govVerdict.Senate.Passed ? "passes it" : "rejects it - " + Congress.StopWords(CountryId.USA, govVerdict)) : string.Empty));

                        // =========================== the 119th's opening days: cloture is three fifths of those sworn on the day
                        (SimulationManager simV, World worldV, Country usaV) = OpenUsa(hosts, Vacancies119, "REP");
                        UsSenate.Seating seatingV = UsSenate.On(Vacancies119);
                        Congress.Verdict vacancies = Congress.Project(usaV, ParliamentSystem.GetCrimeJusticeBillConcern(usaV, CrimeBill(usaV, "REP")), simV.CurrentDate);
                        Check(seatingV.Vacant > 0 && vacancies.Senate.Sworn == seatingV.Members - seatingV.Vacant && vacancies.Senate.Required == Congress.ClotureLine(vacancies.Senate.Sworn)
                              && vacancies.Senate.Required != Congress.ClotureLine(seatingV.Members) && vacancies.Senate.Sworn == Congress.Sworn(usaV.SecondChamberSeats),
                            F("(d) on {0}, {1} seat(s) vacant: {2} senators sworn, so cloture needs {3} - not the {4} of a full chamber: \"{5}\"",
                                Day(Vacancies119), seatingV.Vacant, vacancies.Senate.Sworn, vacancies.Senate.Required, Congress.ClotureLine(seatingV.Members), Congress.ChamberLine(CountryId.USA, vacancies.Senate)));

                        // =========================== the 118th of record
                        DateTime start = WorldClock.StartDate(CountryId.USA);
                        (SimulationManager sim118, World world118, Country usa118) = OpenUsa(hosts, start, "DEM");
                        UsSenate.Seating seating118 = UsSenate.On(start);
                        int letterD = seating118.Seats.Count(s => s.Party == "D"), independents = seating118.IndependentsBySide.TryGetValue("DEM", out int ind) ? ind : 0;
                        int repSenate = usa118.SecondChamberSeats.TryGetValue("REP", out int rs) ? rs : 0;
                        Check(UsSenate.CongressOn(start) == 118 && Same(usa118.ParliamentSeats, PartySystems.InitialSeats(CountryId.USA, ElectionVintage.Usa2022)) && Congress.Applies(usa118) && usa118.Government?.PmParty == "DEM",
                            F("(c) the 118th of record on {0} (the US start): the House {1}, the Senate {2} - DEM's {3} the letter D's {4} and {5} independent(s) counted with it (R-US10 (a)) - the president {6}",
                                Day(start), Seats(usa118.ParliamentSeats), Seats(usa118.SecondChamberSeats), usa118.SecondChamberSeats.TryGetValue("DEM", out int dem118) ? dem118 : 0, letterD, independents, usa118.Government?.Executive));

                        // a party-line DEM reconciliation: the Senate passes it with its caucus, the REP House kills it
                        BudgetBill raise = RatesBill(usa118, "DEM");
                        BillConcern demReconciliation = ParliamentSystem.GetBudgetBillConcern(usa118, raise);
                        v = Congress.Project(usa118, demReconciliation, sim118.CurrentDate);
                        Check(PartyLine(v, "DEM", "REP") && demReconciliation.Reconciliation && !v.Senate.Cloture && v.Senate.Passed && v.Senate.For == letterD + independents && letterD <= repSenate && independents > 0
                              && !v.House.Passed && v.StoppedIn == Congress.House && !v.Passed,
                            F("(c) a party-line DEM reconciliation (a budget moving the {0} rate alone): the Senate {1} it {2}-{3} with its caucus - the letter D's {4} alone would not beat REP's {5} - and {6}; {7}",
                                Describe(raise), v.Senate.Passed ? "passes" : "rejects", v.Senate.For, v.Senate.Against, letterD, repSenate, Congress.StopWords(CountryId.USA, v), Sides(v)));
                        Check(Gate(usa118, "Annual budget bill (US-18 diagnostic, DEM reconciliation)", demReconciliation, sim118.CurrentDate, Congress.House, wantPassed: false, wantChambers: 1, out string houseFirst),
                            "(c) begun in the House (Art. I §7 cl. 1: a revenue bill), it dies there and never reaches the Senate: " + houseFirst);
                        BudgetBill benefits = BenefitsBill(usa118, 10f);
                        BillConcern demBenefits = ParliamentSystem.GetBudgetBillConcern(usa118, benefits);
                        Congress.Verdict senateFirst = Congress.Project(usa118, demBenefits, sim118.CurrentDate, Congress.Senate);
                        bool senateGate = Gate(usa118, "Benefits bill (US-18 diagnostic, begun in the Senate)", demBenefits, sim118.CurrentDate, Congress.Senate, wantPassed: false, wantChambers: 2, out string senateRecords);
                        Check(PartyLine(senateFirst, "DEM", "REP") && demBenefits.Reconciliation && senateFirst.Origin == Congress.Senate && senateFirst.Senate.Passed && !senateFirst.House.Passed && senateFirst.StoppedIn == Congress.House && senateGate,
                            F("(c) a benefits-only DEM reconciliation ({0}) begun in the Senate (no revenue bill, so Art. I §7 cl. 1 does not keep it in the House): passed there, killed in the House - {1}",
                                Describe(benefits), senateRecords));

                        // a party-line REP ordinary bill in the 118th: the House passes it, cloture kills it
                        usa118.PlayerPartyAbbrev = "REP";
                        BillConcern repOrdinary = ParliamentSystem.GetCrimeJusticeBillConcern(usa118, CrimeBill(usa118, "REP"));
                        v = Congress.Project(usa118, repOrdinary, sim118.CurrentDate);
                        Check(PartyLine(v, "REP", "DEM") && v.House.Passed && v.Senate.Cloture && !v.Senate.ClotureInvoked && !v.Passed && v.StoppedIn == Congress.Senate,
                            F("(c) a party-line REP ordinary bill: {0} - {1}; {2}", Congress.StopWords(CountryId.USA, v), string.Join("; ", Congress.OrderWords(CountryId.USA, v)), Sides(v)));
                        // PS-6 US-21: on the veto path the gate draws defectors (R-US17 rule (c)) - the least recorded crossing planted, so this row holds the blocs' arithmetic it was written for, not the hash of its title (the keyed draw stops it in the Senate too)
                        bool repGate; string repRecords;
                        using (UsVeto.PlantIndex(0)) { repGate = Gate(usa118, "Crime & Justice bill (US-18 diagnostic, 118th)", repOrdinary, sim118.CurrentDate, Congress.House, wantPassed: false, wantChambers: 2, out repRecords); }
                        Check(repGate, "(c) through the gate: " + repRecords);

                        // =========================== (f) scope
                        foreach (CountryId id in new[] { CountryId.Sweden, CountryId.Germany, CountryId.Poland })
                        {
                            Country c = world.GetCountry(id);
                            if (string.IsNullOrEmpty(c.PlayerPartyAbbrev)) { c.PlayerPartyAbbrev = c.ParliamentSeats.OrderByDescending(kv => kv.Value).ThenBy(kv => kv.Key, StringComparer.Ordinal).First().Key; }
                            var asked = new List<BillConcern> { ParliamentSystem.GetCrimeJusticeBillConcern(c, CrimeBill(c, "x")), ParliamentSystem.GetBudgetBillConcern(c, RatesBill(c, "x")), ParliamentSystem.GetBudgetBillConcern(c, SpendingBill(c, c.SpendingLines[0].Category, 10f)) };
                            bool same = asked.All(a => ParliamentSystem.WouldBillPass(c, a, sim.CurrentDate) == ParliamentSystem.WouldBillPass(c, a) && !a.Reconciliation);
                            int before = c.Divisions.Entries.Count;
                            DivisionRecord r = ParliamentSystem.Divide(c, "Crime & Justice bill (US-18 diagnostic, one chamber)", asked[0], sim.CurrentDate, out bool passedOne);
                            bool oneRecord = c.Divisions.Entries.Count == Math.Min(DivisionLog.MaxEntries, before + 1) && ReferenceEquals(r, c.Divisions.Entries.Last()) && r.Chamber == 0 && r.Origin == 0 && !r.Cloture
                                             && !r.Reconciliation && r.Sworn == 0 && r.OtherChamber == null && r.Required == 0 && !r.Motion && r.Passed == passedOne && passedOne == ParliamentSystem.WouldBillPass(c, asked[0]);
                            bool noKind = ((Congress.Kind[])Enum.GetValues(typeof(Congress.Kind))).All(k => !Congress.RidesReconciliation(c, k));
                            Check(!Congress.Applies(c) && same && oneRecord && noKind,
                                F("(f) {0}: one chamber - the dated verdict the count's on {1} bills, none riding reconciliation, and the gate writes one record with no chamber field moved ({2})", id, asked.Count, r.Passed ? "carried" : "lost"));
                        }
                        Country aiUsa = OpenOther(hosts, Day119, CountryId.Sweden).GetCountry(CountryId.USA);
                        Check(!Congress.Applies(aiUsa) && Congress.Sworn(aiUsa.SecondChamberSeats) == 0,
                            "(f) an AI USA (a Sweden player's world on " + Day(Day119) + "): no Senate seated, so no bill of its goes through two chambers (and AI countries vote none - plan risk 19)");

                        // =========================== (g) the sweep: reported, not tuned
                        sb.Append("    sweep     THE PASS-RATE SWEEP over the game's bill kinds (REPORTED, NOT TUNED - no figure here is asserted). Per Congress and author: bills, House passes, Senate passes (its own division), both, stopped in the House, stopped in the Senate\n");
                        foreach ((string congress, SimulationManager host, World atWorld, Country u) in new[] { ("119th", sim, world, usa), ("118th", sim118, world118, usa118) })
                        {
                            foreach (string author in new[] { "REP", "DEM" })
                            {
                                u.PlayerPartyAbbrev = author;
                                var rows = new Dictionary<string, int[]>();
                                foreach ((string kind, BillConcern concern) in Sweep(atWorld, u))
                                {
                                    if (concern == null || concern.IsEmpty) { continue; }
                                    Congress.Verdict x = Congress.Project(u, concern, host.CurrentDate);
                                    if (!rows.TryGetValue(kind, out int[] row)) { row = new int[6]; rows[kind] = row; }
                                    row[0]++; if (x.House.Passed) { row[1]++; } if (x.Senate.Passed) { row[2]++; } if (x.Passed) { row[3]++; }
                                    if (x.StoppedIn == Congress.House) { row[4]++; } else if (x.StoppedIn == Congress.Senate) { row[5]++; }
                                }
                                int[] all = new int[6];
                                foreach (int[] r in rows.Values) { for (int i = 0; i < 6; i++) { all[i] += r[i]; } }
                                sb.Append(F("    sweep     {0}, {1} the author: {2}\n", congress, author,
                                    string.Join("; ", rows.Select(kv => F("{0} {1} - {2}/{3}/{4} - {5} {6}", kv.Key, kv.Value[0], kv.Value[1], kv.Value[2], kv.Value[3], kv.Value[4], kv.Value[5])))));
                                sb.Append(F("    sweep     {0}, {1} the author, every kind: {2} bills, the House passes {3}, the Senate {4}, both {5} ({6:0.0} %); stopped in the House {7}, in the Senate {8}\n",
                                    congress, author, all[0], all[1], all[2], all[3], all[0] > 0 ? 100.0 * all[3] / all[0] : 0.0, all[4], all[5]));
                            }
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

            sb.Append(failures == 0 ? "=== UsCongressDiagnostic: bills go through both chambers as ruled - the House by a majority of those voting, the Senate at cloture of the sworn or by reconciliation ===\n"
                                    : F("=== UsCongressDiagnostic: {0} FAILURE(S) ===\n", failures));
            if (failures == 0) { Debug.Log(sb.ToString()); CheckExit.Finish(0); }
            else { Debug.LogError(sb.ToString()); CheckExit.Finish(1); }
        }

        // ------------------------------------------------------------------ the worlds

        /// <summary>A US world on <paramref name="epoch"/> as a new game opens it - the default world, the player's country the USA with <paramref name="party"/>, the
        /// record seated on its date by the day loop's own method (the House, the Senate, the president). Call inside `SimulationManager.EpochScope`.</summary>
        internal static (SimulationManager Sim, World World, Country Usa) OpenUsa(List<GameObject> hosts, DateTime epoch, string party)
        {
            SimulationRandom.Seed(Seed);
            EnergyMarket.ResetCalibration();
            SimulationManager.SetEpoch(epoch);
            World w = WorldFactory.CreateDefault();
            var go = new GameObject("UsCongressDiagnostic"); hosts.Add(go);
            var s = go.AddComponent<SimulationManager>();
            s.SetWorld(w);
            s.PlayerCountryId = CountryId.USA;
            Country u = w.GetCountry(CountryId.USA);
            u.PlayerPartyAbbrev = party;
            MethodInfo seat = typeof(SimulationManager).GetMethod("SeatTheRecordOnItsDate", BindingFlags.Instance | BindingFlags.NonPublic);
            if (seat == null) { throw new MissingMethodException("SimulationManager.SeatTheRecordOnItsDate"); }
            seat.Invoke(s, null);
            return (s, w, u);
        }

        private static World OpenOther(List<GameObject> hosts, DateTime epoch, CountryId player)
        {
            SimulationRandom.Seed(Seed);
            EnergyMarket.ResetCalibration();
            SimulationManager.SetEpoch(epoch);
            World w = WorldFactory.CreateDefault();
            var go = new GameObject("UsCongressDiagnostic.other"); hosts.Add(go);
            var s = go.AddComponent<SimulationManager>();
            s.SetWorld(w);
            s.PlayerCountryId = player;
            MethodInfo seat = typeof(SimulationManager).GetMethod("SeatTheRecordOnItsDate", BindingFlags.Instance | BindingFlags.NonPublic);
            seat?.Invoke(s, null);
            return w;
        }

        // ------------------------------------------------------------------ the gate

        /// <summary>The bill through the gate (`ParliamentSystem.Divide` - `Congress.Divide` here): the records it wrote, in order - the first chamber's (a motion
        /// where the second followed), the second's carrying the first - their chambers, origin, procedure, line, verdicts, and the day's loss.</summary>
        private static bool Gate(Country usa, string title, BillConcern concern, DateTime date, int origin, bool wantPassed, int wantChambers, out string line)
        {
            int lastBefore = usa.Divisions.Entries.Count > 0 ? usa.Divisions.Entries.Last().Number : 0;   // the records written are those numbered after it (the log evicts, so a count would not do)
            usa.BillLostOn = DateTime.MinValue;
            DivisionRecord last = origin == Congress.House ? ParliamentSystem.Divide(usa, title, concern, date, out bool passed) : Congress.Divide(usa, title, concern, date, out passed, BillAxis.Fiscal, origin);
            List<DivisionRecord> records = usa.Divisions.Entries.Where(r => r.Number > lastBefore).ToList();
            int written = records.Count;
            bool shape = ReferenceEquals(last, usa.Divisions.Entries.Last()) && records.All(r => r.Title == title && r.Origin == origin && r.Date == date && r.Reconciliation == concern.Reconciliation);
            bool chambers = written == wantChambers && (wantChambers == 1
                ? last.Chamber == origin && last.OtherChamber == null && !last.Motion && !last.Passed
                : records[0].Chamber == origin && records[1].Chamber != origin && records[0].Motion && records[0].Passed && !records[1].Motion && ReferenceEquals(records[1].OtherChamber, records[0])
                  && records[1].Passed == passed);
            DivisionRecord senate = records.FirstOrDefault(r => r.Chamber == Congress.Senate);
            bool senateFields = senate == null || (senate.Sworn == Congress.Sworn(usa.SecondChamberSeats) && senate.Cloture == !concern.Reconciliation
                                                  && senate.Required == (senate.Cloture ? Congress.ClotureLine(senate.Sworn) : 0));
            // PS-6 US-19 (the review's M2): the day is the government's loss only where the government bears the bill - a congressional majority's from out of it
            // (Congress.GovernmentBears) loses nothing of the government's
            bool borne = Congress.GovernmentBears(usa, concern);
            bool lost = passed || !borne ? usa.BillLostOn == DateTime.MinValue : usa.BillLostOn == date.Date;
            line = string.Join(" then ", records.Select(Records)) + F(" - {0}{1}", passed ? "both passed it" : borne ? "lost, the day marked lost" : "lost, the government's day not marked - the bill a congressional majority's from out of it", lost ? string.Empty : " (THE DAY'S LOSS NOT AS EXPECTED)");
            return written == wantChambers && passed == wantPassed && shape && chambers && senateFields && lost;
        }

        private static string Records(DivisionRecord r) =>
            F("No. {0} the {1}{2} {3}{4}{5}", r.Number, Congress.ChamberName(CountryId.USA, r.Chamber).ToLowerInvariant(), r.Cloture ? F(" at cloture ({0} of {1} sworn)", r.Required, r.Sworn) : r.Chamber == Congress.Senate ? " by reconciliation" : string.Empty,
                r.Passed ? "passed" : "failed", r.Motion ? " (its ceremony the next record's)" : string.Empty, r.OtherChamber != null ? F(" carrying No. {0}", r.OtherChamber.Number) : string.Empty);

        // ------------------------------------------------------------------ the bills

        /// <summary>A crime-and-justice bill moving sentencing severity toward the author's end - REP's up (galtan's 10), anyone else's down - within the dial.</summary>
        internal static CrimeJusticePolicyBill CrimeBill(Country c, string author)
        {
            float step = author == "REP" ? 15f : -15f;
            float to = Mathf.Clamp(c.SentencingSeverity + step, 0f, 100f);
            if (Mathf.Approximately(to, c.SentencingSeverity)) { to = Mathf.Clamp(c.SentencingSeverity - step, 0f, 100f); }
            return new CrimeJusticePolicyBill { PoliceFunding = c.PoliceFundingLevel, SentencingSeverity = to, BailReform = c.BailReformLevel, DrugPolicy = c.DrugPolicyLevel, JudicialFunding = c.JudicialFundingLevel, BorderEnforcement = c.BorderEnforcementLevel };
        }

        /// <summary>A budget moving one levied rate alone - the first levied line's, two points down for REP, up for anyone else, within its range.</summary>
        internal static BudgetBill RatesBill(Country c, string author)
        {
            TaxLine line = c.TaxLines.First(t => t.IsImplemented);
            float step = author == "REP" ? -2f : 2f;
            float to = Mathf.Clamp(line.Rate + step, line.MinRate, line.MaxRate);
            if (Mathf.Approximately(to, line.Rate)) { to = Mathf.Clamp(line.Rate - step, line.MinRate, line.MaxRate); }
            var bill = new BudgetBill();
            bill.TaxLines[line.Type] = to;
            return bill;
        }

        internal static BudgetBill SpendingBill(Country c, SpendingCategory category, float percent)
        {
            var bill = new BudgetBill();
            SpendingCategory line = c.SpendingLines.Any(l => l.Category == category) ? category : c.SpendingLines[0].Category;
            bill.SpendingPercentChanges[line] = percent;
            return bill;
        }

        /// <summary>A budget raising one implemented programme's benefit level alone (the first with room) - the benefit levels' part only.</summary>
        private static BudgetBill BenefitsBill(Country c, float points)
        {
            WelfareProgram program = c.WelfarePrograms.First(p => p.IsImplemented && p.GenerosityLevel + points <= 100f);
            var bill = new BudgetBill();
            bill.WelfarePrograms[program.Type] = program.GenerosityLevel + points;
            return bill;
        }

        private static string Describe(BudgetBill bill) =>
            bill.TaxLines.Count > 0 ? string.Join(", ", bill.TaxLines.Select(kv => kv.Key + " to " + kv.Value.ToString("0.##", CultureInfo.InvariantCulture)))
            : bill.WelfarePrograms.Count > 0 ? string.Join(", ", bill.WelfarePrograms.Select(kv => kv.Key + " to " + kv.Value.ToString("0.#", CultureInfo.InvariantCulture)))
            : string.Join(", ", bill.SpendingPercentChanges.Select(kv => kv.Key + " " + kv.Value.ToString("+0.#;-0.#", CultureInfo.InvariantCulture) + " %"));

        /// <summary>THE SWEEP'S BILLS - every kind the game votes, each lever moved both ways where it has two (a step a player's dial takes), and each programme
        /// toggled; the budget split by what it moves (its rates, a spending line, a benefit level, the pension age), since that decides its procedure.</summary>
        private static IEnumerable<(string Kind, BillConcern Concern)> Sweep(World world, Country u)
        {
            foreach (TaxLine t in u.TaxLines.Where(t => t.IsImplemented))
            {
                foreach (float step in new[] { -2f, 2f })
                {
                    var b = new BudgetBill(); b.TaxLines[t.Type] = Mathf.Clamp(t.Rate + step, t.MinRate, t.MaxRate);
                    yield return ("budget: a rate (reconciliation)", ParliamentSystem.GetBudgetBillConcern(u, b));
                }
            }
            foreach (SpendingLine l in u.SpendingLines)
            {
                foreach (float step in new[] { -10f, 10f }) { yield return ("budget: a spending line (cloture)", ParliamentSystem.GetBudgetBillConcern(u, SpendingBill(u, l.Category, step))); }
            }
            foreach (WelfareProgram p in u.WelfarePrograms.Where(p => p.IsImplemented))
            {
                foreach (float step in new[] { -10f, 10f })
                {
                    var b = new BudgetBill(); b.WelfarePrograms[p.Type] = Mathf.Clamp(p.GenerosityLevel + step, 0f, 100f);
                    yield return ("budget: a benefit level (reconciliation)", ParliamentSystem.GetBudgetBillConcern(u, b));
                }
            }
            if (PensionAgeStatute.Has(u.Id))
            {
                float age = PensionAgeStatute.AgeInForce(u, u.CalendarYear);
                foreach (float step in new[] { -1f, 1f })
                {
                    var b = new BudgetBill { PensionAgeSet = true, PensionAge = Mathf.Clamp(age + step, BudgetBill.PensionAgeMin, BudgetBill.PensionAgeMax) };
                    yield return ("budget: the pension age (cloture)", ParliamentSystem.GetBudgetBillConcern(u, b));
                }
            }
            foreach (TaxLine t in u.TaxLines) { yield return ("tax programme (reconciliation)", ParliamentSystem.GetTaxProgramBillConcern(u, new TaxProgramBill { Type = t.Type, IsAdd = !t.IsImplemented })); }
            foreach (WelfareProgram p in u.WelfarePrograms) { yield return ("welfare programme (reconciliation)", ParliamentSystem.GetWelfareProgramBillConcern(u, new WelfareProgramBill { Type = p.Type, IsAdd = !p.IsImplemented })); }
            for (int dial = 0; dial < 6; dial++)
            {
                foreach (float step in new[] { -10f, 10f })
                {
                    var b = new LaborPolicyBill { MinimumWage = u.MinimumWagePercentOfMedianBase, PaidFamilyLeaveWeeks = u.PaidFamilyLeaveWeeksBase, OvertimeRegulation = u.OvertimeRegulationBase,
                        RetrainingProgram = u.RetrainingProgramBase, FamilyPolicy = u.FamilyPolicyBase, ImmigrationPolicy = u.ImmigrationPolicyBase };
                    if (dial == 0) { b.MinimumWage += step; } else if (dial == 1) { b.PaidFamilyLeaveWeeks += step; } else if (dial == 2) { b.OvertimeRegulation += step; }
                    else if (dial == 3) { b.RetrainingProgram += step; } else if (dial == 4) { b.FamilyPolicy += step; } else { b.ImmigrationPolicy += step; }
                    yield return ("labour (cloture)", ParliamentSystem.GetLaborBillConcern(u, b));
                }
            }
            for (int dial = 0; dial < 6; dial++)
            {
                foreach (float step in new[] { -15f, 15f })
                {
                    var b = new CrimeJusticePolicyBill { PoliceFunding = u.PoliceFundingLevel, SentencingSeverity = u.SentencingSeverity, BailReform = u.BailReformLevel, DrugPolicy = u.DrugPolicyLevel,
                        JudicialFunding = u.JudicialFundingLevel, BorderEnforcement = u.BorderEnforcementLevel };
                    if (dial == 0) { b.PoliceFunding += step; } else if (dial == 1) { b.SentencingSeverity += step; } else if (dial == 2) { b.BailReform += step; }
                    else if (dial == 3) { b.DrugPolicy += step; } else if (dial == 4) { b.JudicialFunding += step; } else { b.BorderEnforcement += step; }
                    yield return ("crime and justice (cloture)", ParliamentSystem.GetCrimeJusticeBillConcern(u, b));
                }
            }
            foreach (LawDefinition law in LawCatalog.All)
            {
                if (!LawCatalog.IsWithinCompetence(world, u, law)) { continue; }
                bool enacted = u.EnactedLaws.Exists(e => e.LawId == law.Id);
                yield return ("law (cloture)", ParliamentSystem.GetLawBillConcern(u, new LawBill { LawId = law.Id, IsRepeal = enacted }));
            }
            foreach (Sector sector in u.Sectors)
            {
                foreach (float step in new[] { -10f, 10f })
                {
                    var b = new SectorPolicyBill(); b.SubsidyLevels[sector.Type] = Mathf.Clamp(sector.SubsidyLevel + step, 0f, 100f);
                    yield return ("sector (cloture)", ParliamentSystem.GetSectorBillConcern(u, b));
                }
            }
            foreach (float step in new[] { -2f, 2f }) { yield return ("trade (cloture)", ParliamentSystem.GetTradeBillConcern(u, new TradePolicyBill { NewBaseTariffRate = Mathf.Max(0f, u.BaseTariffRate + step) }, world)); }
            yield return ("sovereign fund drawdown (cloture)", ParliamentSystem.GetSwfDrawdownBillConcern(u, new SwfDrawdownBill { WithdrawalPercentOfGdp = 1f }));
        }

        // ------------------------------------------------------------------ the words

        /// <summary>A party-line bill: <paramref name="author"/> FOR and <paramref name="other"/> AGAINST in both chambers.</summary>
        private static bool PartyLine(Congress.Verdict v, string author, string other)
        {
            bool In(Congress.Division d) => d.Stances.Any(s => s.Party.Abbrev == author && s.Side > 0) && d.Stances.Any(s => s.Party.Abbrev == other && s.Side < 0);
            return In(v.House) && In(v.Senate);
        }

        private static string Sides(Congress.Verdict v) =>
            "the House " + string.Join(", ", v.House.Stances.Select(s => s.Party.Abbrev + " " + s.Seats + " " + Side(s))) + "; the Senate " + string.Join(", ", v.Senate.Stances.Select(s => s.Party.Abbrev + " " + s.Seats + " " + Side(s)));

        private static string Side(PartyStance s) => !s.Measured ? "unmeasured" : s.Side > 0 ? "for" : s.Side < 0 ? "against" : "undecided";

        private static bool Same(IReadOnlyDictionary<string, int> a, IReadOnlyDictionary<string, int> b) => a != null && b != null && a.Count == b.Count && a.All(kv => b.TryGetValue(kv.Key, out int n) && n == kv.Value);

        private static string Seats(IReadOnlyDictionary<string, int> s) => s == null ? "none" : s.Count == 0 ? "empty" : string.Join(", ", s.Where(kv => kv.Value > 0).OrderBy(kv => kv.Key, StringComparer.Ordinal).Select(kv => kv.Key + " " + kv.Value.ToString(CultureInfo.InvariantCulture)));

        private static string Day(DateTime d) => d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

        private static string F(string format, params object[] args) => string.Format(CultureInfo.InvariantCulture, format, args);
    }
}
