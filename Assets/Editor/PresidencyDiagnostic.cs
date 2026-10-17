using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using PoliSim.Data;
using PoliSim.Elections;
using PoliSim.Persistence;
using PoliSim.Simulation;
using UnityEngine;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// PS-6 US-7 (`docs/specs/USA_STAGE_PLAN.md`, "The oath: a presidency, not a formation"): THE OATH, ON PLANTED OUTCOMES. The plan's done-when: "a presidency
    /// diagnostic forces each outcome and steps to 21 Jan 2025 - each world seats its winner on the oath's date, and a DEM-player world whose candidate wins reads
    /// PRESIDENT; ... a save straddling the oath loads into the game". Each contest is PLANTED as the count records one (the plan: "Tested on planted outcomes;
    /// US-8 supplies the real one"), its figures read from the catalog (NARA's electors), never typed - except where PS-6 US-8's count now holds the real one: a
    /// world on the eve of 5 November 2024 counts its own polling day (b), and an undecided contingent contest is chosen by the House on the count's day by the
    /// count's own path (e). Every world is built as a new game builds one (the epoch set before the world is created) and stepped in the controller's order
    /// (`CongressOfRecordDiagnostic`'s).
    /// <list type="bullet">
    /// <item>(a) the sources: the Twentieth Amendment §1 and Art. II §1 held to their saved pages (SHA-256 in SHA256SUMS.txt), and NARA's key dates for 2024 - the
    /// electors' meeting, the count of 6 January and the inauguration the planted contests carry; the oath's day the rule gives equals the record's own row;</item>
    /// <item>(b) REP wins, a REP player - the long world, opened on the eve of 5 November 2024, whose own count (US-8 - E1's acceptance world) elects REP's nominee
    /// on the polling day: from the polling day the record seats no president, REP is the
    /// PRESIDENT-ELECT (the chip and the gate's reason) and in opposition toward the sitting government; the 119th House still seated by the record on 3 January;
    /// Biden to the 19th; the game's Trump on the 20th - a presidency the game elected, formed that day, not the record's - and PRESIDENT from it, REP's own
    /// arrival window opening on the oath's day; each change once; every other country untouched;</item>
    /// <item>(c) DEM wins, a DEM player: PRESIDENT before (Biden's party) and after (Harris takes office on the 20th) - the done-when's words; the arrival window
    /// open on the eve stays open and stays the one arrival window (the party's own oath closes nothing and hands no second); (c') the same world cut on the
    /// eve of the oath and loaded into the game: the open window loads as the arrival window, and the oath keeps it so - the load equals the continuous run;</item>
    /// <item>(d) REP wins, a DEM player: PRESIDENT to the 19th, IN OPPOSITION from the 20th, the DEM window closed and the new government's budget tabled that
    /// day - US-2's oath, through the same transition;</item>
    /// <item>(e) a contingent election, half the electors appointed each, undecided until the House chooses on 6 January (R-US3 (a)) - by US-8's count on the day
    /// loop, the delegations of the House then seated - no president-elect before it, REP's after, its winner sworn on the 20th, the government saying the House
    /// chose; the contingent contest - its flag, its delegations, its tallies' NaN shares - crosses a save whole;</item>
    /// <item>(f) ASKED of Elias (US-7's reading 3), the code's answer pinned until he rules: a contest still undecided on the oath's day seats no one - the
    /// sitting president stays;</item>
    /// <item>(g) scope: a world whose player is another country keeps the AI USA's president across the oath, a contest planted or not (PS-8 owns the AI row);</item>
    /// <item>(h) a save cut on the eve of the oath, loaded INTO THE GAME (the controller's own `RestoreFromSave`, §557), keeps its government and its contest as it
    /// loads, reads PRESIDENT-ELECT on the controller's own chip, and swears the game's president on its next day; (i) a save cut after the oath keeps the
    /// game's president as it loads and on its next day - the record does not seat its own over him; the contest rides the save whole; a format-38 save is
    /// refused;</item>
    /// <item>(j) the verdict path: an election of the chamber planted in a presidency installs nothing - the government the same object, no round, the sentence
    /// the president's; a chamber election the player's party leaves still ends the run.</item>
    /// </list>
    /// </summary>
    public static class PresidencyDiagnostic
    {
        private static readonly DateTime Eve = new DateTime(2024, 11, 4);
        private static readonly DateTime Polled = new DateTime(2024, 11, 5);
        private static readonly DateTime ElectorsMeet = new DateTime(2024, 12, 17);
        private static readonly DateTime HouseDay = new DateTime(2025, 1, 3);
        private static readonly DateTime CountedOn = new DateTime(2025, 1, 6);
        private static readonly DateTime OathEve = new DateTime(2025, 1, 17);
        private static readonly DateTime Oath = new DateTime(2025, 1, 20);
        private static readonly DateTime StepTo = new DateTime(2025, 1, 21);
        private const int Seed = 777;

        /// <summary>The saved pages the oath, the term and the planted dates are held to, and the words each must hold.</summary>
        private const string AmendmentsPage = "ElectionsData/usa/raw/executive/archives_amendments_11-27.html";
        private const string ConstitutionPage = "ElectionsData/usa/raw/records/archives_constitution_transcript.html";
        private const string KeyDatesPage = "ElectionsData/usa/raw/executive/archives_electoral_college_key_dates.html";
        private const string OathWords = "The terms of the President and Vice President shall end at noon on the 20th day of January";
        private const string TermWords = "He shall hold his Office during the Term of four Years";
        private const string MeetWords = "December 17, 2024—electors vote in their States";
        private const string CountWords = "January 6, 2025—Congress counts the electoral votes";
        private const string InaugurationWords = "January 20, 2025 at Noon—Inauguration Day";

        public static void Run()
        {
            CheckExit.ArmLogFold();
            var sb = new StringBuilder("=== PresidencyDiagnostic (PS-6 US-7): the oath - a presidency, not a formation ===\n");
            int failures = 0;
            void Check(bool ok, string what) { if (!ok) { failures++; } sb.Append(ok ? "    ok        " : "    FAIL      ").Append(what).Append('\n'); }
            var hosts = new List<GameObject>();
            string dir = Path.Combine(Application.temporaryCachePath, "presidency_" + Guid.NewGuid().ToString("N"));
            int seedWas = SimulationRandom.MasterSeed;
            Dictionary<SimulationRandom.Stream, int> drawsWas = SimulationRandom.CaptureDrawCounts();
            try
            {
                using (SimulationManager.EpochScope())
                {
                    Directory.CreateDirectory(dir);
                    string Day(DateTime d) => d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                    bool Same(IReadOnlyDictionary<string, int> a, IReadOnlyDictionary<string, int> b) => a != null && b != null && a.Count == b.Count && a.All(kv => b.TryGetValue(kv.Key, out int n) && n == kv.Value);
                    Dictionary<string, int> house119 = PartySystems.InitialSeats(CountryId.USA, ElectionVintage.Usa2024);

                    // ---- (a) the sources, and the oath's day against the record's own row
                    Check(Page(AmendmentsPage, out string amendments) && amendments.Contains(OathWords),
                        F("(a) the Twentieth Amendment §1 ([EX-AM]): {0} held to its SHA-256 in SHA256SUMS.txt, holding \"{1}\"", Path.GetFileName(AmendmentsPage), OathWords));
                    Check(Page(ConstitutionPage, out string constitution) && constitution.Contains(TermWords),
                        F("(a) Art. II §1 ([CONST]): {0} held to its SHA-256, holding \"{1}\"", Path.GetFileName(ConstitutionPage), TermWords));
                    Check(Page(KeyDatesPage, out string keyDates) && keyDates.Contains(MeetWords) && keyDates.Contains(CountWords) && keyDates.Contains(InaugurationWords),
                        F("(a) NARA's key dates ([EX-DATES]): {0} held to its SHA-256 - the planted contests' dates, the electors' meeting {1}, the count {2}, the oath {3}", Path.GetFileName(KeyDatesPage), Day(ElectorsMeet), Day(CountedOn), Day(Oath)));
                    bool recordHasNext = WorldClock.TryNextGovernmentOfRecord(CountryId.USA, Polled, out WorldClock.GovernmentOfRecord recordNext);
                    Check(UsPresidentialElection.OathDayAfter(Polled) == Oath && recordHasNext && recordNext.From == Oath,
                        F("(a) the oath's day for a contest held {0}: {1} by the rule (the 20th of January after) - the record's own row for the next president takes office on {2}", Day(Polled), Day(UsPresidentialElection.OathDayAfter(Polled)), Day(recordNext.From)));

                    // ---- the planted contests: the record's 2024 count, read from the catalog
                    var y2024 = Array.Find(PoliSim.Elections.Generated.UsPresidentialReturns.Years, r => r.Year == 2024);
                    int states = PoliSim.Elections.Generated.UsPresidentialReturns.States.Count(r => r.Year == 2024 && r.State != "DC");
                    UsPresidentialElection.Contest Planted(string winnerParty, string label)
                    {
                        bool rep = winnerParty == "REP";
                        int most = Math.Max(y2024.CastR, y2024.CastD), least = Math.Min(y2024.CastR, y2024.CastD);
                        var c = new UsPresidentialElection.Contest
                        {
                            PollingDay = Polled, DayBasis = "planted (PresidencyDiagnostic): the record's polling day [EX-DATES]", ElectorsMeet = ElectorsMeet, CountedOn = CountedOn,
                            TakesOffice = UsPresidentialElection.OathDayAfter(Polled), ElectorsAppointed = y2024.Electors,
                            Winner = rep ? y2024.NomineeR : y2024.NomineeD, WinnerParty = winnerParty, Line = "planted (PresidencyDiagnostic): " + label,
                        };
                        // REP's win is the record's count; DEM's is the record's electors swapped - planted, no record holds it
                        c.Electors.Add(new UsPresidentialElection.Tally { Candidate = c.Winner, Party = winnerParty, Votes = most, Share = 100.0 * PresidencyOfRecord.ShareOfRecord(CountryId.USA, 2024, rep ? "TRUMP" : "HARRIS") });
                        c.Electors.Add(new UsPresidentialElection.Tally { Candidate = rep ? y2024.NomineeD : y2024.NomineeR, Party = rep ? "DEM" : "REP", Votes = least, Share = 100.0 * PresidencyOfRecord.ShareOfRecord(CountryId.USA, 2024, rep ? "HARRIS" : "TRUMP") });
                        return c;
                    }
                    UsPresidentialElection.Contest Tied()
                    {
                        // R-US3 (a): each candidate half the electors appointed - no majority; the House chooses on the count's day
                        var c = new UsPresidentialElection.Contest
                        {
                            PollingDay = Polled, DayBasis = "planted (PresidencyDiagnostic): the record's polling day [EX-DATES]", ElectorsMeet = ElectorsMeet, CountedOn = CountedOn,
                            TakesOffice = UsPresidentialElection.OathDayAfter(Polled), ElectorsAppointed = y2024.Electors, Contingent = true,
                            Line = "planted (PresidencyDiagnostic): no majority of the electors - the House chooses",
                        };
                        c.Electors.Add(new UsPresidentialElection.Tally { Candidate = y2024.NomineeR, Party = "REP", Votes = y2024.Electors / 2 });
                        c.Electors.Add(new UsPresidentialElection.Tally { Candidate = y2024.NomineeD, Party = "DEM", Votes = y2024.Electors - y2024.Electors / 2 });
                        return c;
                    }
                    // PS-6 US-8: the House's choice on the count's day is the count's own now (UsPresidentialElection.HoldHouseChoice, on the day loop) - it was planted here
                    Check(y2024.Electors > 0 && y2024.CastR + y2024.CastD == y2024.Electors && y2024.CastR > y2024.CastD && states > 0,
                        F("the planted contests read the catalog's 2024 row: {0} {1} - {2} {3} of {4} electors appointed (REP's win the record's count; DEM's the same electors swapped; the tie half each); {5} states for the House's choice",
                            y2024.NomineeR, y2024.CastR, y2024.NomineeD, y2024.CastD, y2024.Electors, states));

                    // a world on an epoch, as a new game builds one; the player's country and party, or another country
                    (SimulationManager Sim, World World, Country Usa) Open(DateTime epoch, CountryId player, string party)
                    {
                        SimulationRandom.Seed(Seed);
                        EnergyMarket.ResetCalibration();
                        SimulationManager.SetEpoch(epoch);
                        World w = WorldFactory.CreateDefault();
                        var go = new GameObject("PresidencyDiagnostic"); hosts.Add(go);
                        var s = go.AddComponent<SimulationManager>();
                        s.SetWorld(w);
                        s.PlayerCountryId = player;
                        w.GetCountry(player).PlayerPartyAbbrev = party;
                        return (s, w, w.GetCountry(CountryId.USA));
                    }
                    // one day in the controller's order (GameController: AdvanceDay, the player's day tick, the boundary's turn)
                    void Step(SimulationManager s, World w)
                    {
                        bool boundary = s.AdvanceDay();
                        if (s.PlayerCountryId.HasValue) { s.AdvanceCountryDayTick(s.PlayerCountryId.Value); }
                        if (boundary)
                        {
                            var none = new Dictionary<CountryId, PolicyDecision>();
                            foreach (Country c in w.Countries) { none[c.Id] = PolicyDecision.None(); }
                            s.AdvanceTurn(none);
                        }
                    }
                    // the desk's chip, read through the controller's own RoleChip (a controller in edit mode, its manager and player country handed to it)
                    var chipHost = new GameObject("PresidencyDiagnostic.Chip"); hosts.Add(chipHost);
                    chipHost.AddComponent<SimulationManager>();
                    var chipController = chipHost.AddComponent<PoliSim.UI.GameController>();
                    string Chip(PoliSim.UI.GameController controller, SimulationManager s, Country c)
                    {
                        if (s != null) { ControllerField("_simulationManager").SetValue(controller, s); ControllerField("_playerCountry").SetValue(controller, c); }
                        object read = typeof(PoliSim.UI.GameController).GetMethod("RoleChip", BindingFlags.Instance | BindingFlags.NonPublic)?.Invoke(controller, null);
                        return read is ValueTuple<string, bool> chip ? chip.Item1 + (chip.Item2 ? " (filled)" : " (outlined)") : "NO CHIP";
                    }
                    bool Governs(SimulationManager s, Country c, string party) { string was = c.PlayerPartyAbbrev; c.PlayerPartyAbbrev = party; bool g = s.PlayerGoverns(c); c.PlayerPartyAbbrev = was; return g; }
                    bool IsGameElected(GovernmentRecord g, string executive) => g != null && g.Kind == WorldClock.ExecutiveKind.Presidency && g.Outcome == UsPresidentialElection.ElectedOutcome
                        && g.Executive == executive && g.FormedOn == Oath && g.Cabinet.Count == 1 && g.Support.Count == 0 && g.Basis != null && g.Basis.StartsWith("the game's own election of " + Day(Polled), StringComparison.Ordinal);
                    bool OathBudget(SimulationManager s) => !s.GetPendingBudgetProcess(CountryId.USA)
                        && s.GetPendingBudgetBill(CountryId.USA) is BudgetBill b && b.GovernmentBill && b.DaysRemaining == ParliamentSystem.BillDurationDays;
                    const string Biden = "Joseph R. Biden Jr. (DEM)";
                    string trump = y2024.NomineeR + " (REP)", harris = y2024.NomineeD + " (DEM)";
                    const string NextHouse = "NEXT OF RECORD: THE HOUSE ELECTED 5 NOV 2024 · SEATED 3 JAN 2025", NextHead = "NEXT OF RECORD: DONALD J. TRUMP · REP · TAKES OFFICE 20 JAN 2025";

                    // ---- (b) REP wins, a REP player - the long world, from the eve of the polling day to the day after the oath
                    string saveEve = Path.Combine(dir, "eve_of_oath.json"), saveAfter = Path.Combine(dir, "after_oath.json"), saveDemEve = Path.Combine(dir, "dem_eve_of_oath.json");
                    UsPresidentialElection.Contest contestA;
                    {
                        (SimulationManager sim, World world, Country usa) = Open(Eve, CountryId.USA, "REP");
                        var others = world.Countries.Where(c => c.Id != CountryId.USA).ToDictionary(c => c.Id, c => (Seats: c.ParliamentSeats, Gov: c.Government));
                        GovernmentRecord eveGov = usa.Government;
                        string eveChip = Chip(chipController, sim, usa);
                        Check(eveGov != null && eveGov.Executive == Biden && WorldClock.RecordSeatsExecutive(usa, Eve) && GovernmentRecord.RoleOn(usa, "REP", Eve) == PlayerRole.Opposition && eveChip == "IN OPPOSITION (outlined)",
                            F("(b) on the eve, {0}: {1} governs, the record seats the president, REP in opposition - the chip \"{2}\"", Day(Eve), eveGov?.Executive, eveChip));
                        Step(sim, world);
                        // PS-6 US-8: the count's day - the world's own count holds the contest (the day loop's hook); where it held none, the planted REP win stands in so
                        // the oath's checks still run, and the count's absence fails by name
                        contestA = usa.UsPresidentialElections.Count == 1 ? usa.UsPresidentialElections[0] : null;
                        Check(contestA != null && contestA.PollingDay == Polled && contestA.DayBasis != null && contestA.DayBasis.StartsWith(UsPresidentialElection.DayBasis, StringComparison.Ordinal)
                              && contestA.WinnerParty == "REP" && contestA.Winner == y2024.NomineeR && !contestA.Contingent,
                            F("(b) on the polling day the world's own count (US-8) holds the contest and elects REP's nominee - {0}", contestA?.Line ?? usa.UsPresidentialElections.Count + " contest(s) held"));
                        if (contestA == null) { usa.UsPresidentialElections.Clear(); contestA = Planted("REP", "REP's win, the record's count - planted, the world's count held none"); usa.UsPresidentialElections.Add(contestA); }
                        DateTime today = sim.CurrentDate;
                        string elect = Chip(chipController, sim, usa);
                        bool gate = !sim.PlayerMayIntroduce(CountryId.USA, out string locked);
                        Check(today == Polled && !WorldClock.RecordSeatsExecutive(usa, today) && WorldClock.RecordSeatsExecutive(CountryId.USA) && ReferenceEquals(usa.Government, eveGov),
                            F("(b) on the polling day, {0}, REP's win held: the record seats no president in this world from today (the country's gate unchanged); {1} still governs", Day(today), usa.Government?.Executive));
                        Check(GovernmentRecord.RoleOn(usa, "REP", today) == PlayerRole.PresidentElect && usa.Government.RoleOf("REP") == PlayerRole.Opposition && !sim.PlayerGoverns(usa) && elect == "PRESIDENT-ELECT (outlined)",
                            F("(b) REP is the PRESIDENT-ELECT - toward the sitting government in opposition, it does not govern; the chip \"{0}\"", elect));
                        Check(gate && locked == "PRESIDENT-ELECT · THE OUTGOING ADMINISTRATION GOVERNS UNTIL 20 JAN 2025 · YOUR PARTY VOTES ON ITS BILLS", F("(b) the role gate's reason: \"{0}\"", locked));
                        Check(WorldClock.NextOfRecord(usa, today).SequenceEqual(new[] { NextHouse }) && WorldClock.NextOfRecord(CountryId.USA, today).SequenceEqual(new[] { NextHouse, NextHead }),
                            "(b) the record row's coming dates name the House's day and no president of record in this world; the record's own view still names its next (US-2's words)");
                        object seatsRef = usa.ParliamentSeats, govRef = usa.Government;
                        int seatChanges = 0, govChanges = 0;
                        DateTime seatOn = DateTime.MinValue, govOn = DateTime.MinValue;
                        bool houseDay = false, eveOfOath = false, oathDay = false, after = false, windowOnOath = false;
                        string eveOfOathChip = null, oathChip = null, afterChip = null;
                        GovernmentRecord sworn = null;
                        for (int d = 0; d < 120 && sim.CurrentDate < StepTo; d++)
                        {
                            Step(sim, world);
                            today = sim.CurrentDate;
                            if (!ReferenceEquals(seatsRef, usa.ParliamentSeats)) { seatChanges++; seatOn = today; seatsRef = usa.ParliamentSeats; }
                            if (!ReferenceEquals(govRef, usa.Government)) { govChanges++; govOn = today; govRef = usa.Government; }
                            if (today == HouseDay) { houseDay = Same(usa.ParliamentSeats, house119) && ReferenceEquals(usa.Government, eveGov); }
                            if (today == Oath.AddDays(-1))
                            {
                                eveOfOathChip = Chip(chipController, sim, usa);
                                eveOfOath = ReferenceEquals(usa.Government, eveGov) && eveOfOathChip == "PRESIDENT-ELECT (outlined)";
                                SaveGameService.SaveToFile(saveEve, SaveGameService.CreateSaveGame(sim, world, CountryId.USA, null));
                            }
                            if (today == Oath)
                            {
                                sworn = usa.Government;
                                oathChip = Chip(chipController, sim, usa);
                                oathDay = IsGameElected(sworn, trump) && sworn.PmParty == "REP" && UsPresidentialElection.Governs(sworn, contestA) && sim.PlayerGoverns(usa)
                                          && sim.PlayerMayIntroduce(CountryId.USA, out _) && oathChip == "PRESIDENT (filled)";
                                windowOnOath = sim.GetPendingBudgetProcess(CountryId.USA) && sim.IsIncomingGovernmentBudgetWindow(CountryId.USA);
                            }
                            if (today == StepTo) { afterChip = Chip(chipController, sim, usa); after = ReferenceEquals(usa.Government, sworn) && afterChip == "PRESIDENT (filled)"; }
                        }
                        Check(houseDay, F("(b) {0}: the 119th House seated by the record, as before - the chamber is still the record's; {1} still governs", Day(HouseDay), eveGov.Executive));
                        Check(eveOfOath, F("(b) {0}: {1} governs, the chip \"{2}\"", Day(Oath.AddDays(-1)), eveGov.Executive, eveOfOathChip));
                        Check(oathDay, F("(b) {0}: {1} takes office - a presidency the game elected (\"{2}\"), formed that day, REP's alone - and governs; the chip \"{3}\"", Day(Oath), sworn?.Executive, sworn?.Basis, oathChip));
                        Check(seatChanges == 1 && seatOn == HouseDay && govChanges == 1 && govOn == Oath,
                            F("(b) each change once: the House seated {0} time(s), on {1}; the president {2} time(s), on {3}", seatChanges, Day(seatOn), govChanges, Day(govOn)));
                        Check(windowOnOath, "(b) REP, now governing, gets its own arrival window on the oath's day - the window reset as the president installs (US-2's premise, through the same transition)");
                        Check(after, F("(b) {0}: the same government, the chip \"{1}\" - seated once, then left alone", Day(StepTo), afterChip));
                        Check(others.All(kv => ReferenceEquals(world.GetCountry(kv.Key).ParliamentSeats, kv.Value.Seats) && ReferenceEquals(world.GetCountry(kv.Key).Government, kv.Value.Gov)),
                            "(b) every other country's chamber and government untouched from the eve to the day after the oath");
                        EnergyMarket.ResetTurnState();
                    }

                    // ---- (c) DEM wins, a DEM player: PRESIDENT before (Biden's party) and after (Harris)
                    UsPresidentialElection.Contest contestB = Planted("DEM", "DEM's win, the record's electors swapped");
                    {
                        (SimulationManager sim, World world, Country usa) = Open(OathEve, CountryId.USA, "DEM");
                        usa.UsPresidentialElections.Add(contestB);   // a world cut after the count, as a save carries it
                        GovernmentRecord eveGov = usa.Government;
                        string before = Chip(chipController, sim, usa);
                        Check(eveGov.Executive == Biden && !WorldClock.RecordSeatsExecutive(usa, OathEve) && GovernmentRecord.RoleOn(usa, "DEM", OathEve) == PlayerRole.PrimeMinister && before == "PRESIDENT (filled)",
                            F("(c) {0}, DEM's win held: {1} governs, DEM is the head's party (its own candidate elected - no president-elect role), the chip \"{2}\"", Day(OathEve), eveGov.Executive, before));
                        object govRef = usa.Government;
                        int govChanges = 0;
                        DateTime govOn = DateTime.MinValue;
                        bool windowEve = false, windowOath = false, oathDay = false, after = false;
                        string oathChip = null;
                        GovernmentRecord sworn = null;
                        for (int d = 0; d < 10 && sim.CurrentDate < StepTo; d++)
                        {
                            Step(sim, world);
                            DateTime today = sim.CurrentDate;
                            if (!ReferenceEquals(govRef, usa.Government)) { govChanges++; govOn = today; govRef = usa.Government; }
                            if (today == Oath.AddDays(-1))
                            {
                                windowEve = sim.GetPendingBudgetProcess(CountryId.USA) && sim.IsIncomingGovernmentBudgetWindow(CountryId.USA);
                                SaveGameService.SaveToFile(saveDemEve, SaveGameService.CreateSaveGame(sim, world, CountryId.USA, null));   // (c'): the same eve, through a load
                            }
                            if (today == Oath)
                            {
                                sworn = usa.Government;
                                oathChip = Chip(chipController, sim, usa);
                                oathDay = IsGameElected(sworn, harris) && sworn.PmParty == "DEM" && sim.PlayerGoverns(usa) && !Governs(sim, usa, "REP") && GovernmentRecord.RoleOn(usa, "REP", today) == PlayerRole.Opposition && oathChip == "PRESIDENT (filled)";
                                // the window kept AND still marked used - so no second arrival window opens once it closes (TakeOffice's reset skipped while it stands)
                                windowOath = sim.GetPendingBudgetProcess(CountryId.USA) && sim.IsIncomingGovernmentBudgetWindow(CountryId.USA) && sim.CaptureSaveState().IncomingBudgetWindowUsed.Contains(CountryId.USA);
                            }
                            if (today == StepTo)
                            {
                                after = ReferenceEquals(usa.Government, sworn) && Chip(chipController, sim, usa) == "PRESIDENT (filled)";
                                SaveGameService.SaveToFile(saveAfter, SaveGameService.CreateSaveGame(sim, world, CountryId.USA, null));
                            }
                        }
                        Check(oathDay && govChanges == 1 && govOn == Oath,
                            F("(c) {0}: {1} takes office, a presidency the game elected, DEM's alone; REP in opposition; a DEM-player world whose candidate wins reads \"{2}\"; the one change of government that day", Day(Oath), sworn?.Executive, oathChip));
                        Check(windowEve && windowOath, "(c) the DEM player's arrival window, open on the eve, stays open and stays the one arrival window - the party's own oath closes nothing and hands no second (DECLARED: the window open on the eve is the incoming president's)");
                        Check(after, F("(c) {0}: the same government, the chip PRESIDENT", Day(StepTo)));
                        EnergyMarket.ResetTurnState();
                    }

                    // ---- (d) REP wins, a DEM player: PRESIDENT to the eve, IN OPPOSITION from the oath - US-2's oath, through the same transition
                    {
                        (SimulationManager sim, World world, Country usa) = Open(OathEve, CountryId.USA, "DEM");
                        usa.UsPresidentialElections.Add(Planted("REP", "REP's win, the record's count"));
                        bool windowBefore = false, oathDay = false;
                        string eveChip = null, oathChip = null, locked = null;
                        for (int d = 0; d < 10 && sim.CurrentDate < Oath; d++)
                        {
                            Step(sim, world);
                            DateTime today = sim.CurrentDate;
                            if (today == Oath.AddDays(-1)) { eveChip = Chip(chipController, sim, usa); windowBefore = sim.GetPendingBudgetProcess(CountryId.USA) && sim.IsIncomingGovernmentBudgetWindow(CountryId.USA); }
                            if (today == Oath)
                            {
                                oathChip = Chip(chipController, sim, usa);
                                oathDay = IsGameElected(usa.Government, trump) && !sim.PlayerGoverns(usa) && !sim.PlayerMayIntroduce(CountryId.USA, out locked) && OathBudget(sim);
                            }
                        }
                        Check(eveChip == "PRESIDENT (filled)" && windowBefore, F("(d) {0}: DEM governs under Biden - the chip \"{1}\", its arrival window open", Day(Oath.AddDays(-1)), eveChip));
                        Check(oathDay && oathChip == "IN OPPOSITION (outlined)" && locked == "IN OPPOSITION · THE GOVERNMENT INTRODUCES BILLS · YOUR PARTY VOTES ON THEM",
                            F("(d) {0}: the game's {1} takes office; DEM in opposition (\"{2}\"; the gate: \"{3}\"); the DEM window closed and the new government's budget tabled that day, its whole term to run", Day(Oath), usa.Government?.Executive, oathChip, locked));
                        EnergyMarket.ResetTurnState();
                    }

                    // ---- (e) a contingent election: no majority, the House chooses on 6 January (R-US3 (a))
                    {
                        (SimulationManager sim, World world, Country usa) = Open(HouseDay.AddDays(-1), CountryId.USA, "REP");
                        UsPresidentialElection.Contest tied = Tied();
                        usa.UsPresidentialElections.Add(tied);
                        GovernmentRecord eveGov = usa.Government;
                        bool undecided = !tied.Decided() && tied.ElectorsOf("REP") < tied.MajorityOfElectors() && tied.ElectorsOf("DEM") < tied.MajorityOfElectors()
                                         && !WorldClock.RecordSeatsExecutive(usa, sim.CurrentDate) && GovernmentRecord.RoleOn(usa, "REP", sim.CurrentDate) == PlayerRole.Opposition;
                        bool electAfterCount = false, oathDay = false, crossed = false, houseChose = false;
                        string crossedWhat = "not cut", choseWhat = "not reached";
                        for (int d = 0; d < 30 && sim.CurrentDate < StepTo; d++)
                        {
                            Step(sim, world);
                            DateTime today = sim.CurrentDate;
                            if (today < CountedOn && (GovernmentRecord.RoleOn(usa, "REP", today) != PlayerRole.Opposition || tied.Delegations.Count > 0)) { undecided = false; }
                            if (today == CountedOn)
                            {
                                // PS-6 US-8: the House's choice on the count's day is the count's own (the day loop's hook) - by the delegations of the House then seated
                                List<(string State, int Republicans, int Democrats)> house = UsPresidentialElection.DelegationsOn(today, out string houseName);
                                int forRep = house.Count(h => h.Republicans > h.Democrats), forDem = house.Count(h => h.Democrats > h.Republicans);
                                houseChose = tied.Decided() && tied.Delegations.Count == 2 && tied.Delegations.Sum(t => t.Votes) == forRep + forDem && house.Count == states
                                             && tied.WinnerParty == (forRep > house.Count / 2 ? "REP" : forDem > house.Count / 2 ? "DEM" : null);
                                choseWhat = F("{0}: {1} states REP, {2} DEM, {3} tied of {4} - {5}", houseName, forRep, forDem, house.Count - forRep - forDem, house.Count, tied.Line);
                            }
                            if (today == CountedOn.AddDays(1))
                            {
                                electAfterCount = GovernmentRecord.RoleOn(usa, "REP", today) == PlayerRole.PresidentElect && ReferenceEquals(usa.Government, eveGov);
                                // the contingent shape across a save: the flag, the delegations, and the tallies' NaN shares (Tally's default "where the count carries none")
                                SaveGame cut = SaveGameService.Deserialize(SaveGameService.Serialize(SaveGameService.CreateSaveGame(sim, world, CountryId.USA, null)));
                                List<UsPresidentialElection.Contest> back = cut.World.GetCountry(CountryId.USA).UsPresidentialElections;
                                UsPresidentialElection.Contest b = back.Count == 1 ? back[0] : null;
                                crossed = b != null && SameContest(b, tied) && b.Contingent && b.Delegations.Count > 0 && b.Electors.Count > 0 && b.Electors.All(t => double.IsNaN(t.Share));
                                crossedWhat = b == null ? back.Count + " contest(s) came back" : F("contingent {0}, {1} delegation tallies, {2} elector tallies with NaN shares, the winner {3}", b.Contingent, b.Delegations.Count, b.Electors.Count(t => double.IsNaN(t.Share)), b.Winner);
                            }
                            if (today == Oath) { oathDay = IsGameElected(usa.Government, trump) && usa.Government.Basis.Contains("chosen by the House") && sim.PlayerGoverns(usa); }
                        }
                        Check(undecided, F("(e) a tie, {0} to {1} of {2} electors (a majority is {3}): undecided until the House chooses - no president-elect before {4}, the record seating no president", tied.ElectorsOf("REP"), tied.ElectorsOf("DEM"), tied.ElectorsAppointed, tied.MajorityOfElectors(), Day(CountedOn)));
                        Check(houseChose, F("(e) on {0} the count's own House choice (US-8, the day loop) decides it by the delegations then seated - {1}", Day(CountedOn), choseWhat));
                        Check(electAfterCount, F("(e) the House chooses REP's candidate on {0}: REP is the PRESIDENT-ELECT from then, the sitting government unchanged", Day(CountedOn)));
                        Check(crossed, F("(e) the contingent contest crosses a save whole, field by field, cut {0}: {1}", Day(CountedOn.AddDays(1)), crossedWhat));
                        Check(oathDay, F("(e) {0}: {1} takes office - \"{2}\"", Day(Oath), usa.Government?.Executive, usa.Government?.Basis));
                        EnergyMarket.ResetTurnState();
                    }

                    // ---- (f) ASKED of Elias (US-7's reading 3), the code's answer pinned until he rules: a contest undecided on the oath's day seats no one
                    {
                        (SimulationManager sim, World world, Country usa) = Open(OathEve, CountryId.USA, "REP");
                        usa.UsPresidentialElections.Add(Tied());
                        GovernmentRecord eveGov = usa.Government;
                        for (int d = 0; d < 10 && sim.CurrentDate < StepTo; d++) { Step(sim, world); }
                        Check(ReferenceEquals(usa.Government, eveGov) && eveGov.Executive == Biden && GovernmentRecord.RoleOn(usa, "REP", sim.CurrentDate) == PlayerRole.Opposition && Governs(sim, usa, "DEM"),
                            F("(f) ASKED (reading 3 - the code's answer until Elias rules among: the sitting president stays; the record's president for the date; the Speaker acts under 3 U.S.C. §19): a contest the House never decided, stepped past the oath to {0} - no one is seated and the sitting president stays ({1})", Day(sim.CurrentDate), usa.Government?.Executive));
                        EnergyMarket.ResetTurnState();
                    }

                    // ---- (g) scope: a world whose player is another country keeps the AI USA's president across the oath, a contest planted or not
                    {
                        (SimulationManager sim, World world, Country usa) = Open(OathEve, CountryId.Germany, "CDU");
                        usa.UsPresidentialElections.Add(Planted("DEM", "DEM's win, on an AI USA"));
                        GovernmentRecord eveGov = usa.Government;
                        for (int d = 0; d < 10 && sim.CurrentDate < StepTo; d++) { Step(sim, world); }
                        Check(ReferenceEquals(usa.Government, eveGov) && eveGov.Executive == Biden,
                            F("(g) a world whose player is Germany (CDU), a DEM win planted on the AI USA: stepped to {0}, the AI USA keeps its president, the same object - the oath is the player's country's (PS-8 owns the AI row)", Day(sim.CurrentDate)));
                        EnergyMarket.ResetTurnState();
                    }

                    // ---- (h) the save cut on the eve of the oath, loaded into the game, then stepped
                    {
                        SaveGame save = SaveGameService.LoadFromFile(saveEve);
                        Country saved = save.World.GetCountry(CountryId.USA);
                        object govSaved = saved.Government;
                        bool carried = saved.UsPresidentialElections.Count == 1 && SameContest(saved.UsPresidentialElections[0], contestA);
                        (SimulationManager adopted, PoliSim.UI.GameController controller) = Adopt(save, hosts, out string error);
                        if (adopted == null) { Check(false, "(h) the eve's save: " + error); }
                        else
                        {
                            try
                            {
                                Country u = adopted.World.GetCountry(CountryId.USA);
                                string loadedChip = Chip(controller, null, null);
                                bool asLoaded = ReferenceEquals(u.Government, govSaved) && u.Government.Executive == Biden && loadedChip == "PRESIDENT-ELECT (outlined)";
                                DateTime from = adopted.CurrentDate;
                                Step(adopted, adopted.World);
                                string steppedChip = Chip(controller, null, null);
                                Check(carried && asLoaded && adopted.CurrentDate == Oath && IsGameElected(u.Government, trump) && steppedChip == "PRESIDENT (filled)",
                                    F("(h) the save cut {0}, loaded into the game: its government ({1}) and its contest kept as it loads, the controller's chip \"{2}\"; stepped to {3}: {4} takes office, the chip \"{5}\"",
                                        Day(from), Biden, loadedChip, Day(adopted.CurrentDate), u.Government?.Executive, steppedChip));
                            }
                            finally { EnergyMarket.ResetTurnState(); }
                        }
                    }

                    // ---- (c') (c)'s world cut on the eve of the oath, loaded into the game, then stepped: the load takes the oath as the continuous run did. The open
                    //      arrival window rides the save (format 39) - before it did, RestoreSaveState cleared the "open now" mark and nothing restored it, so the
                    //      loaded window read as the annual one (as-loaded clause false) and TakeOffice's guard was false at the oath, the used flag reset and a
                    //      second arrival window owed after this one closed (oath-day clauses false): every clause below was false on 6686f04c
                    {
                        SaveGame save = SaveGameService.LoadFromFile(saveDemEve);
                        bool savedOpen = save.Sim.IncomingBudgetWindowOpenNow != null && save.Sim.IncomingBudgetWindowOpenNow.Contains(CountryId.USA);
                        (SimulationManager adopted, PoliSim.UI.GameController controller) = Adopt(save, hosts, out string error);
                        if (adopted == null) { Check(false, "(c') the DEM eve's save: " + error); }
                        else
                        {
                            try
                            {
                                Country u = adopted.World.GetCountry(CountryId.USA);
                                DateTime from = adopted.CurrentDate;
                                bool loadedArrival = adopted.GetPendingBudgetProcess(CountryId.USA) && adopted.IsIncomingGovernmentBudgetWindow(CountryId.USA);
                                Step(adopted, adopted.World);
                                bool keptUsed = adopted.CaptureSaveState().IncomingBudgetWindowUsed.Contains(CountryId.USA);
                                bool keptArrival = adopted.GetPendingBudgetProcess(CountryId.USA) && adopted.IsIncomingGovernmentBudgetWindow(CountryId.USA);
                                Check(savedOpen && loadedArrival, F("(c') the save cut {0} carries the open arrival window (format 39), and it loads as the arrival window, not the annual one", Day(from)));
                                Check(adopted.CurrentDate == Oath && IsGameElected(u.Government, harris) && keptUsed && keptArrival,
                                    F("(c') stepped to {0}: {1} takes office and the arrival window stays the one arrival window - used {2}, open as the arrival window {3} - as in the continuous run (c)",
                                        Day(adopted.CurrentDate), u.Government?.Executive, keptUsed, keptArrival));
                            }
                            finally { EnergyMarket.ResetTurnState(); }
                        }
                    }

                    // ---- (i) the save cut after the oath, loaded into the game: the game's president stays; the contest whole; a format-38 save refused
                    {
                        string json = File.ReadAllText(saveAfter);
                        SaveGame save = SaveGameService.Deserialize(json);
                        Country saved = save.World.GetCountry(CountryId.USA);
                        object govSaved = saved.Government;
                        bool carried = saved.UsPresidentialElections.Count == 1 && SameContest(saved.UsPresidentialElections[0], contestB);
                        (SimulationManager adopted, PoliSim.UI.GameController controller) = Adopt(save, hosts, out string error);
                        if (adopted == null) { Check(false, "(i) the save after the oath: " + error); }
                        else
                        {
                            try
                            {
                                Country u = adopted.World.GetCountry(CountryId.USA);
                                bool asLoaded = ReferenceEquals(u.Government, govSaved) && IsGameElected(u.Government, harris);
                                DateTime from = adopted.CurrentDate;
                                Step(adopted, adopted.World);
                                string chip = Chip(controller, null, null);
                                Check(carried && asLoaded && ReferenceEquals(u.Government, govSaved) && chip == "PRESIDENT (filled)",
                                    F("(i) the save cut {0}, loaded into the game: {1} kept as it loads and on {2} - the same object, the record's president not seated over the game's; the contest rides the save whole (format {3}); the chip \"{4}\"",
                                        Day(from), u.Government?.Executive, Day(adopted.CurrentDate), save.SaveVersion, chip));

                                // ---- (j) the verdict path: a chamber election planted in a presidency installs nothing (as US-16's House count will be held)
                                FieldInfo verdictField = ControllerField("_pendingElectionVerdict"), endsField = ControllerField("_pendingElectionVerdictEndsGame"), dateField = ControllerField("_pendingElectionDate");
                                MethodInfo resolve = typeof(PoliSim.UI.GameController).GetMethod("ResolveElectionVerdict", BindingFlags.Instance | BindingFlags.NonPublic);
                                if (verdictField == null || endsField == null || dateField == null || resolve == null) { Check(false, "(j) the controller's verdict path could not be reached by name - this assertion VERIFIED NOTHING"); }
                                else
                                {
                                    GovernmentRecord before = u.Government;
                                    var held = new ElectionRecord { Date = adopted.CurrentDate, CountryId = "USA", Method = ElectionMethod.GermanyNationalProportional, Seats = new Dictionary<string, int>(u.ParliamentSeats) };   // planted: any method but NotImplemented
                                    u.ElectionHistory.Add(held);
                                    dateField.SetValue(controller, adopted.CurrentDate);
                                    resolve.Invoke(controller, null);
                                    string verdict = verdictField.GetValue(controller) as string;
                                    bool ends = (bool)endsField.GetValue(controller);
                                    Check(ReferenceEquals(u.Government, before) && adopted.RoundOf(CountryId.USA) == null && !ends && verdict != null
                                          && verdict.StartsWith("The chamber is elected on", StringComparison.Ordinal) && verdict.Contains(harris + " governs to the end of the term"),
                                        F("(j) a chamber election held in the presidency installs nothing: the same government, no round, the run goes on - \"{0}\"", verdict));
                                    var lost = new Dictionary<string, int>(u.ParliamentSeats) { ["DEM"] = 0 };
                                    u.ElectionHistory[u.ElectionHistory.Count - 1] = new ElectionRecord { Date = adopted.CurrentDate, CountryId = "USA", Method = ElectionMethod.GermanyNationalProportional, Seats = lost };
                                    resolve.Invoke(controller, null);
                                    Check(ReferenceEquals(u.Government, before) && (bool)endsField.GetValue(controller) && (verdictField.GetValue(controller) as string ?? string.Empty).StartsWith("Out of the chamber", StringComparison.Ordinal),
                                        F("(j) one the player's party leaves still ends the run - \"{0}\"", verdictField.GetValue(controller)));
                                    u.ElectionHistory.RemoveAt(u.ElectionHistory.Count - 1);
                                }
                            }
                            finally { EnergyMarket.ResetTurnState(); }
                        }
                        string older = json.Replace("\"SaveVersion\": " + SaveGameService.CurrentSaveVersion.ToString(CultureInfo.InvariantCulture), "\"SaveVersion\": " + (SaveGameService.CurrentSaveVersion - 1).ToString(CultureInfo.InvariantCulture));
                        bool refused = false; string why = null;
                        try { SaveGameService.Deserialize(older); } catch (SaveLoadException e) { refused = true; why = e.Message; }
                        Check(older != json && refused && why.Contains("format version " + (SaveGameService.CurrentSaveVersion - 1).ToString(CultureInfo.InvariantCulture)),
                            F("(i) a save of the format before this build's ({0}) is refused with the plain message: \"{1}\"", SaveGameService.CurrentSaveVersion, why));
                    }
                }
            }
            catch (Exception ex) { failures++; sb.Append("    FAIL      threw: ").Append(ex).Append('\n'); }
            finally
            {
                foreach (GameObject go in hosts) { if (go != null) { UnityEngine.Object.DestroyImmediate(go); } }
                EnergyMarket.ResetTurnState();
                SimulationRandom.RestoreState(seedWas, drawsWas);
                try { if (Directory.Exists(dir)) { Directory.Delete(dir, true); } } catch (Exception e) { sb.Append("    note      the temporary folder stays: ").Append(e.Message).Append('\n'); }
            }

            sb.Append(failures == 0 ? "=== PresidencyDiagnostic: each world seats its winner on the oath's date ===" : F("=== PresidencyDiagnostic: {0} FAILED ===", failures));
            if (failures == 0) { Debug.Log(sb.ToString()); } else { Debug.LogError(sb.ToString()); }
            CheckExit.Finish(failures == 0 ? 0 : 1);
        }

        /// <summary>A contest as the save carries it, field by field - the dates, the electors, the delegations, the contingent flag, the winner, the line.</summary>
        internal static bool SameContest(UsPresidentialElection.Contest a, UsPresidentialElection.Contest b)
        {
            bool Tallies(List<UsPresidentialElection.Tally> x, List<UsPresidentialElection.Tally> y) =>
                x.Count == y.Count && x.Zip(y, (p, q) => p.Candidate == q.Candidate && p.Party == q.Party && p.Votes == q.Votes && p.Share.Equals(q.Share)).All(same => same);
            return a.PollingDay == b.PollingDay && a.DayBasis == b.DayBasis && a.ElectorsMeet == b.ElectorsMeet && a.CountedOn == b.CountedOn && a.TakesOffice == b.TakesOffice
                   && a.ElectorsAppointed == b.ElectorsAppointed && a.Contingent == b.Contingent && a.Winner == b.Winner && a.WinnerParty == b.WinnerParty && a.Line == b.Line
                   && Tallies(a.Electors, b.Electors) && Tallies(a.Delegations, b.Delegations);
        }

        internal static FieldInfo ControllerField(string name) => typeof(PoliSim.UI.GameController).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);

        /// <summary>A saved page's text, true where its SHA-256 is the one its folder's SHA256SUMS.txt lists.</summary>
        internal static bool Page(string relative, out string text)
        {
            text = string.Empty;
            string path = Path.Combine(Directory.GetCurrentDirectory(), relative);
            string sums = Path.Combine(Path.GetDirectoryName(path), "SHA256SUMS.txt");
            if (!File.Exists(path) || !File.Exists(sums)) { return false; }
            byte[] bytes = File.ReadAllBytes(path);
            string digest;
            using (var sha = SHA256.Create()) { digest = BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", string.Empty).ToLowerInvariant(); }
            text = Encoding.UTF8.GetString(bytes);
            string name = Path.GetFileName(path);
            return File.ReadAllLines(sums).Any(line => line.StartsWith(digest, StringComparison.Ordinal) && line.TrimEnd().EndsWith(name, StringComparison.Ordinal));
        }

        /// <summary>A save loaded INTO THE GAME (§557): a controller in edit mode adopts it through its own `RestoreFromSave` - `CongressOfRecordDiagnostic`'s path - its
        /// manager handed to it; the forks the load builds are disposed. Returns the manager the controller now drives and the controller, or nulls with the reason.</summary>
        internal static (SimulationManager, PoliSim.UI.GameController) Adopt(SaveGame save, List<GameObject> hosts, out string error)
        {
            error = null;
            var go = new GameObject("PresidencyDiagnostic.Controller"); hosts.Add(go);
            SimulationManager sim = go.AddComponent<SimulationManager>();
            var controller = go.AddComponent<PoliSim.UI.GameController>();
            const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
            Type type = typeof(PoliSim.UI.GameController);
            FieldInfo simField = type.GetField("_simulationManager", Private), playerField = type.GetField("_playerCountry", Private), speedField = type.GetField("_gameSpeed", Private);
            MethodInfo restore = type.GetMethod("RestoreFromSave", Private);
            if (simField == null || playerField == null || speedField == null || restore == null) { error = "the controller's load path could not be reached by name"; return (null, null); }
            simField.SetValue(controller, sim);
            try { restore.Invoke(controller, new object[] { save }); }
            catch (TargetInvocationException e) { error = "the controller cannot adopt the save - " + (e.InnerException ?? e).GetType().Name + ": " + (e.InnerException ?? e).Message; return (null, null); }
            finally
            {
                (type.GetField("_impactLedger", Private)?.GetValue(controller) as PolicyImpactLedger)?.Dispose();
                (type.GetField("_shadowBaseline", Private)?.GetValue(controller) as ShadowBaseline)?.Dispose();
            }
            var player = playerField.GetValue(controller) as Country;
            if (player == null || player.Id != CountryId.USA) { error = "adopted, the controller governs " + (player == null ? "no country" : player.Id.ToString()) + ", not the USA"; return (null, null); }
            if (!ReferenceEquals(sim.World, save.World)) { error = "adopted, the manager and the save hold two worlds"; return (null, null); }
            if (speedField.GetValue(controller).ToString() != "Paused") { error = "adopted, the game is " + speedField.GetValue(controller) + ", not PAUSED"; return (null, null); }
            return (sim, controller);
        }

        private static string F(string format, params object[] args) => string.Format(CultureInfo.InvariantCulture, format, args);
    }
}
