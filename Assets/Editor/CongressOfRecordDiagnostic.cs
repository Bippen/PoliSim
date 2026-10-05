using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
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
    /// PS-6 US-2 (Elias's ruling F8, R-US1 (a): "whatever the game does not elect is seated on its record's dates - the 119th Congress at noon on 3 Jan
    /// 2025, and the president of record at noon on 20 Jan 2025 ... A chamber not yet sourced (the 120th) is not seated: the 119th stands, said on
    /// screen"): THE RECORD BY DATE INSIDE A US GAME. A US world is opened on an eve before the House's day - a world built as a new game builds one
    /// (`PresidentialReferenceWorld`'s order) - and stepped day by day in the controller's order:
    /// <list type="bullet">
    /// <item>(a) the 118th House and Biden before; the 119th House from its day, Biden still; Trump from the oath's day, of record - each change once; each
    /// party's capital carried once, on the House's day, by the 119th's seats over the 118th's;</item>
    /// <item>(b) the player's role follows the oath: a DEM player governs to the day before, a REP player from the day; the oath closes the DEM player's
    /// open arrival window and the new government's budget is tabled that day with its whole term to run - so the hook sits in the day loop, before the
    /// player's tick; and a REP player, whose AI government spent its arrival budget on the first day, gets its own arrival window on the oath's day - the
    /// window reset as the record installs, not only closed;</item>
    /// <item>(c) WorldClock's answer, not a stepped day: the 120th, elected after the record's date, is not on record - the 119th's table is the one seated
    /// on the 120th's day and past the 121st's, and the words say it stands, its term ended, naming no later election; the president's term of record ends
    /// on 20 January 2029 and the last of record stands, said so. (c') A game day: a world opened just before the 120th's day keeps its House and president
    /// across it - compared as the same objects, since seat values cannot tell the 120th from the 119th while its table is unsourced; the slip's coming dates
    /// (`WorldClock.NextOfRecord`) name the House's day and the oath while they are ahead, and nothing after;</item>
    /// <item>(d) a save cut on the eve of each day, loaded INTO THE GAME (the controller's own `RestoreFromSave`, §557), keeps the save's own House and
    /// government objects as it loads and steps across the day as the unbroken world did - the same objects kept where nothing is owed, the capital, the
    /// oath's budget; (e) a save cut after both days moves nothing as it loads and nothing on its next day;</item>
    /// <item>(f) STATE, not transition: a world holding the 118th, Biden and the 118th's capital past their days - a save from a build before US-2 - is
    /// put right on its next day by the day loop alone (`AdvanceDay`, not the player's tick); (h) the same save, loaded into the game, has both rewritten as
    /// it loads and is right before any step;</item>
    /// <item>(g) scope: the gates are the USA's alone over every country; a non-US chamber planted off its record, and every other country's chamber and
    /// government, are untouched; a US world with no player country (the seeded shadow's, the trajectory dump's), and one whose player is another country,
    /// keep the AI USA's eve House and president;</item>
    /// <item>(i) the row's surname: BIDEN to the oath, TRUMP from it - a generational suffix is not the surname.</item>
    /// </list>
    /// </summary>
    public static class CongressOfRecordDiagnostic
    {
        private static readonly DateTime Eve = new DateTime(2024, 12, 28);
        private static readonly DateTime HouseDay = new DateTime(2025, 1, 3);
        private static readonly DateTime OathDay = new DateTime(2025, 1, 20);
        private static readonly DateTime After = new DateTime(2025, 1, 22);
        private static readonly DateTime Day120 = new DateTime(2027, 1, 3);
        private static readonly DateTime TermEnds = new DateTime(2029, 1, 20);
        private static readonly DateTime Late = new DateTime(2029, 6, 1);
        private static readonly string[] Parties = { "DEM", "REP" };
        private const int Seed = 777;

        public static void Run()
        {
            CheckExit.ArmLogFold();
            var sb = new StringBuilder("=== CongressOfRecordDiagnostic (PS-6 US-2, R-US1 (a)): the record by date inside a US game ===\n");
            int failures = 0;
            void Check(bool ok, string what) { if (!ok) { failures++; } sb.Append(ok ? "    ok        " : "    FAIL      ").Append(what).Append('\n'); }
            var hosts = new List<GameObject>();
            string dir = Path.Combine(Application.temporaryCachePath, "congress_of_record_" + Guid.NewGuid().ToString("N"));
            int seedWas = SimulationRandom.MasterSeed;
            Dictionary<SimulationRandom.Stream, int> drawsWas = SimulationRandom.CaptureDrawCounts();
            try
            {
                using (SimulationManager.EpochScope())
                {
                    Directory.CreateDirectory(dir);
                    Dictionary<string, int> house118 = PartySystems.InitialSeats(CountryId.USA, ElectionVintage.Usa2022);
                    Dictionary<string, int> house119 = PartySystems.InitialSeats(CountryId.USA, ElectionVintage.Usa2024);
                    string Seats(IReadOnlyDictionary<string, int> s) => s == null ? "none" : string.Join(", ", s.Where(kv => kv.Value > 0).OrderBy(kv => kv.Key, StringComparer.Ordinal).Select(kv => kv.Key + " " + kv.Value));
                    bool Same(IReadOnlyDictionary<string, int> a, IReadOnlyDictionary<string, int> b) => a != null && b != null && a.Count == b.Count && a.All(kv => b.TryGetValue(kv.Key, out int n) && n == kv.Value);
                    string Day(DateTime d) => d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

                    // a world on an epoch, as a new game builds one; the player's country the USA with a party, or none
                    (SimulationManager Sim, World World, Country Usa) Open(bool player, DateTime epoch)
                    {
                        SimulationRandom.Seed(Seed);
                        EnergyMarket.ResetCalibration();
                        SimulationManager.SetEpoch(epoch);
                        World w = WorldFactory.CreateDefault();
                        var go = new GameObject("CongressOfRecordDiagnostic"); hosts.Add(go);
                        var s = go.AddComponent<SimulationManager>();
                        s.SetWorld(w);
                        Country u = w.GetCountry(CountryId.USA);
                        if (player) { s.PlayerCountryId = CountryId.USA; u.PlayerPartyAbbrev = "DEM"; }
                        return (s, w, u);
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
                    // the oath's budget: no window open, and the new government's bill tabled that day - its whole term to run (the bill's day counts before
                    // the window's in the tick, so a bill tabled after the hook on the oath's day still has every day)
                    bool OathBudget(SimulationManager s) => !s.GetPendingBudgetProcess(CountryId.USA)
                        && s.GetPendingBudgetBill(CountryId.USA) is BudgetBill b && b.GovernmentBill && b.DaysRemaining == ParliamentSystem.BillDurationDays;

                    // (g) the gates: the record seats the chamber and the president in the USA alone
                    bool usaOnly = Enum.GetValues(typeof(CountryId)).Cast<CountryId>()
                        .All(id => WorldClock.RecordSeatsChamber(id) == (id == CountryId.USA) && WorldClock.RecordSeatsExecutive(id) == (id == CountryId.USA));
                    Check(usaOnly, "(g) the gates: the record seats the chamber and the president in the USA alone, over every country");

                    // (a), (b), (f), (g) on the unbroken world - every step of it before any load (the energy market's turn state is the process's)
                    (SimulationManager sim, World world, Country usa) = Open(player: true, Eve);
                    // (g) a non-US chamber planted off its record - Germany's 2025 Bundestag while the 20th sits - so a hook over every country would move it
                    world.GetCountry(CountryId.Germany).ParliamentSeats = PartySystems.InitialSeats(CountryId.Germany, ElectionVintage.Germany2025);
                    var others = world.Countries.Where(c => c.Id != CountryId.USA).ToDictionary(c => c.Id, c => (Seats: c.ParliamentSeats, Gov: c.Government));
                    // each party's capital on the eve, by value: the 118th's mandate, and the strength the House's day carries by the 119th's seats over it
                    Dictionary<string, double> eveStrength = Parties.ToDictionary(k => k, k => PartyCapital.For(usa.PartyCapital, k)?.OrganizationalStrength ?? double.NaN);
                    bool CapitalAt(Country u, Dictionary<string, int> mandate, Func<string, double> strength) =>
                        Parties.All(k => PartyCapital.For(u.PartyCapital, k) is PartyCampaignCapital r && r.SeatsAtLastUpdate == mandate[k] && Math.Abs(r.OrganizationalStrength - strength(k)) < 1e-9);
                    bool Uncarried(Country u) => CapitalAt(u, house118, k => eveStrength[k]);
                    bool Carried(Country u) => CapitalAt(u, house119, k => Math.Min(100.0, Math.Max(0.0, eveStrength[k] * house119[k] / (double)house118[k])));
                    string Capital(Country u) => string.Join(", ", Parties.Select(k => PartyCapital.For(u.PartyCapital, k) is PartyCampaignCapital r
                        ? k + " " + r.OrganizationalStrength.ToString("0.######", CultureInfo.InvariantCulture) + " at " + r.SeatsAtLastUpdate.ToString(CultureInfo.InvariantCulture) : k + " none"));

                    bool eveRight = Same(usa.ParliamentSeats, house118) && usa.Government != null && usa.Government.Executive == "Joseph R. Biden Jr. (DEM)" && Uncarried(usa);
                    Check(eveRight, F("(a) on the eve, {0}: the 118th House ({1}), Biden, each party's capital at the 118th's mandate - {2}; {3}", Day(sim.CurrentDate), Seats(house118), usa.Government?.Executive, Capital(usa)));
                    object seatsRef = usa.ParliamentSeats, govRef = usa.Government;
                    int seatChanges = 0, govChanges = 0;
                    DateTime seatChangedOn = DateTime.MinValue, govChangedOn = DateTime.MinValue;
                    bool dayBeforeHouse = false, houseDay = false, dayBeforeOath = false, oathDay = false, demBefore = false, repBefore = true, demAfter = true, repAfter = false;
                    bool capitalBeforeHouse = false, capitalOnHouse = false, capitalOnOath = false, capitalAfter = false, windowBeforeOath = false, budgetOnOath = false;
                    string saveHouse = Path.Combine(dir, "eve_of_house.json"), saveOath = Path.Combine(dir, "eve_of_oath.json"), saveAfter = Path.Combine(dir, "after_both.json"), saveBefore = Path.Combine(dir, "before_us2.json");
                    bool Governs(string party) { string was = usa.PlayerPartyAbbrev; usa.PlayerPartyAbbrev = party; bool g = sim.PlayerGoverns(usa); usa.PlayerPartyAbbrev = was; return g; }
                    for (int day = 0; day < 60 && sim.CurrentDate < After; day++)
                    {
                        Step(sim, world);
                        if (!ReferenceEquals(seatsRef, usa.ParliamentSeats)) { seatChanges++; seatChangedOn = sim.CurrentDate; seatsRef = usa.ParliamentSeats; }
                        if (!ReferenceEquals(govRef, usa.Government)) { govChanges++; govChangedOn = sim.CurrentDate; govRef = usa.Government; }
                        DateTime today = sim.CurrentDate;
                        if (today == HouseDay.AddDays(-1))
                        {
                            dayBeforeHouse = Same(usa.ParliamentSeats, house118) && usa.Government.Executive.StartsWith("Joseph R. Biden", StringComparison.Ordinal);
                            capitalBeforeHouse = Uncarried(usa);
                            SaveGameService.SaveToFile(saveHouse, SaveGameService.CreateSaveGame(sim, world, CountryId.USA, null));
                        }
                        if (today == HouseDay) { houseDay = Same(usa.ParliamentSeats, house119) && usa.Government.Executive.StartsWith("Joseph R. Biden", StringComparison.Ordinal); capitalOnHouse = Carried(usa); }
                        if (today == OathDay.AddDays(-1))
                        {
                            dayBeforeOath = Same(usa.ParliamentSeats, house119) && usa.Government.Executive.StartsWith("Joseph R. Biden", StringComparison.Ordinal);
                            demBefore = Governs("DEM"); repBefore = Governs("REP");
                            // the DEM player's arrival window, open since its first day - the controller's default hold would not step past it; this steps
                            windowBeforeOath = sim.GetPendingBudgetProcess(CountryId.USA) && sim.IsIncomingGovernmentBudgetWindow(CountryId.USA);
                            SaveGameService.SaveToFile(saveOath, SaveGameService.CreateSaveGame(sim, world, CountryId.USA, null));
                        }
                        if (today == OathDay)
                        {
                            GovernmentRecord g = usa.Government;
                            oathDay = Same(usa.ParliamentSeats, house119) && g.Executive == "Donald J. Trump (REP)" && g.PmParty == "REP" && g.Kind == WorldClock.ExecutiveKind.Presidency
                                      && g.Outcome == "of record" && g.FormedOn == OathDay && g.Cabinet.SequenceEqual(new[] { "REP" });
                            demAfter = Governs("DEM"); repAfter = Governs("REP");
                            capitalOnOath = Carried(usa);
                            budgetOnOath = OathBudget(sim);
                        }
                        if (today == After.AddDays(-1)) { SaveGameService.SaveToFile(saveAfter, SaveGameService.CreateSaveGame(sim, world, CountryId.USA, null)); }
                        if (today == After) { capitalAfter = Carried(usa); }
                    }
                    Check(dayBeforeHouse && houseDay, F("(a) {0}: the 118th House and Biden; {1}: the 119th House ({2}), Biden still", Day(HouseDay.AddDays(-1)), Day(HouseDay), Seats(house119)));
                    Check(dayBeforeOath && oathDay, F("(a) {0}: Biden; {1}: Trump (REP) takes office, of record, a presidency, formed that day - {2}", Day(OathDay.AddDays(-1)), Day(OathDay), usa.Government?.Executive));
                    Check(seatChanges == 1 && seatChangedOn == HouseDay && govChanges == 1 && govChangedOn == OathDay,
                        F("(a) each change once: the House seated {0} time(s), on {1}; the president {2} time(s), on {3}", seatChanges, Day(seatChangedOn), govChanges, Day(govChangedOn)));
                    Check(capitalBeforeHouse && capitalOnHouse && capitalOnOath && capitalAfter,
                        F("(a) each party's capital carried once, on the House's day, by the 119th's seats over the 118th's - not before it, not again at the oath: {0}", Capital(usa)));
                    Check(demBefore && !repBefore && !demAfter && repAfter, "(b) the player's role follows the oath: a DEM player governs to the day before it, a REP player from the day");
                    Check(windowBeforeOath && budgetOnOath,
                        "(b) the oath closes the DEM player's open arrival window and the new government's budget is tabled that day, its whole term to run - the window reset and closed as the record installs, before the day's tick");
                    Check(others.All(kv => ReferenceEquals(world.GetCountry(kv.Key).ParliamentSeats, kv.Value.Seats) && ReferenceEquals(world.GetCountry(kv.Key).Government, kv.Value.Gov)),
                        "(g) a non-US chamber planted off its record (Germany's 2025 Bundestag while the 20th sits), and every other country's chamber and government, untouched across both days");

                    // (c) WorldClock's answer, not a stepped day: the 120th not on record - the 119th's table seated on its day and past the 121st's, said so
                    const string Stands = "ELECTED 5 NOV 2024 · SEATED 3 JAN 2025 · ITS TERM ENDED 3 JAN 2027 · NONE ELECTED AFTER IT IS ON RECORD, SO IT STANDS";
                    string standing119 = WorldClock.RecordStanding(CountryId.USA, HouseDay), standing120 = WorldClock.RecordStanding(CountryId.USA, Day120), standingLate = WorldClock.RecordStanding(CountryId.USA, Late);
                    Check(WorldClock.SeatedVintage(CountryId.USA, Day120) == ElectionVintage.Usa2024 && WorldClock.SeatedVintage(CountryId.USA, Late) == ElectionVintage.Usa2024
                          && WorldClock.ChamberSeatedOn(CountryId.USA, Late).Vintage == ElectionVintage.Usa2024
                          && standing119 == "ELECTED 5 NOV 2024 · SEATED 3 JAN 2025" && standing120 == Stands && standingLate == Stands,
                        F("(c) WorldClock: the 120th is not on record - on {0} and on {1} the 119th's table is seated and the words say it stands: \"{2}\" / \"{3}\"; on its own day: \"{4}\"", Day(Day120), Day(Late), standing120, standingLate, standing119));
                    string inTerm = WorldClock.ExecutiveStanding(CountryId.USA, OathDay), eveOfEnd = WorldClock.ExecutiveStanding(CountryId.USA, TermEnds.AddDays(-1)), ended = WorldClock.ExecutiveStanding(CountryId.USA, TermEnds);
                    Check(inTerm == "IN OFFICE FROM 20 JAN 2025" && eveOfEnd == inTerm
                          && ended == "IN OFFICE FROM 20 JAN 2025 · THE TERM OF RECORD ENDED 20 JAN 2029 · NO SUCCESSOR IS ON RECORD, SO THE LAST OF RECORD STANDS"
                          && WorldClock.TryGovernmentAt(CountryId.USA, Late, out WorldClock.GovernmentOfRecord stillOfRecord) && stillOfRecord.President == "Donald J. Trump (REP)",
                        F("(c) WorldClock: the president's term of record ends {0} and the last of record stands (R-US1 (a): until the game holds the presidential election) - \"{1}\"", Day(TermEnds), ended));
                    // (i) the row's surname: a generational suffix is not the surname
                    bool SurnameIs(DateTime d, string name, string surname) => WorldClock.TryPresidentOfRecord(CountryId.USA, d, out string n, out string s) && n == name && s == surname;
                    Check(SurnameIs(Eve, "Joseph R. Biden Jr.", "Biden") && SurnameIs(OathDay.AddDays(-1), "Joseph R. Biden Jr.", "Biden") && SurnameIs(OathDay, "Donald J. Trump", "Trump")
                          && SurnameIs(Late, "Donald J. Trump", "Trump") && WorldClock.SurnameOf("Karol Nawrocki") == "Nawrocki" && WorldClock.SurnameOf("KO's candidate (2030)") == "KO's candidate (2030)",
                        "(i) the row's surname: BIDEN to the oath (\"Joseph R. Biden Jr.\" - the suffix is not the surname), TRUMP from it; a name without a suffix and an unnamed candidate as before");
                    // (c) the slip's coming dates: what the record seats next, named while it is ahead, nothing once the record holds no later one
                    const string NextHouse = "NEXT OF RECORD: THE HOUSE ELECTED 5 NOV 2024 · SEATED 3 JAN 2025", NextHead = "NEXT OF RECORD: DONALD J. TRUMP · REP · TAKES OFFICE 20 JAN 2025";
                    string[] NextOn(DateTime d) => WorldClock.NextOfRecord(CountryId.USA, d).ToArray();
                    Check(NextOn(Eve).SequenceEqual(new[] { NextHouse, NextHead }) && NextOn(HouseDay.AddDays(-1)).SequenceEqual(new[] { NextHouse, NextHead })
                          && NextOn(HouseDay).SequenceEqual(new[] { NextHead }) && NextOn(OathDay.AddDays(-1)).SequenceEqual(new[] { NextHead })
                          && NextOn(OathDay).Length == 0 && NextOn(Day120).Length == 0 && NextOn(Late).Length == 0,
                        F("(c) the slip's coming dates: \"{0}\" and \"{1}\" to their days, then nothing - none on {2}, {3} or {4}", NextHouse, NextHead, Day(OathDay), Day(Day120), Day(Late)));

                    // (f) STATE, not transition: the 118th, Biden and the 118th's capital put back, as a save from a build before US-2 holds them - put right on
                    // the next day by the day loop alone (the hook's place; the player's tick does not run)
                    usa.ParliamentSeats = new Dictionary<string, int>(house118);
                    usa.Government = GovernmentRecord.AtStart(usa, Eve, world);
                    foreach (string k in Parties) { if (PartyCapital.For(usa.PartyCapital, k) is PartyCampaignCapital r) { r.SeatsAtLastUpdate = house118[k]; r.OrganizationalStrength = eveStrength[k]; } }
                    SaveGameService.SaveToFile(saveBefore, SaveGameService.CreateSaveGame(sim, world, CountryId.USA, null));
                    sim.AdvanceDay();
                    Check(Same(usa.ParliamentSeats, house119) && usa.Government.Executive == "Donald J. Trump (REP)" && usa.Government.FormedOn == sim.CurrentDate && Carried(usa),
                        F("(f) a world holding the 118th, Biden and the 118th's capital past their days (a save before US-2) is put right on its next day, {0}, by the day loop alone: the 119th, Trump, the capital carried - {1}", Day(sim.CurrentDate), Capital(usa)));
                    EnergyMarket.ResetTurnState();

                    // (g) a US world with no player country keeps its eve's House and president - the hook is the player's country's
                    (SimulationManager bare, World bareWorld, Country bareUsa) = Open(player: false, Eve);
                    for (int day = 0; day < 60 && bare.CurrentDate < After; day++) { Step(bare, bareWorld); }
                    Check(Same(bareUsa.ParliamentSeats, house118) && bareUsa.Government.Executive.StartsWith("Joseph R. Biden", StringComparison.Ordinal),
                        F("(g) a US world with no player country keeps its eve's House and president on {0} - {1}", Day(bare.CurrentDate), bareUsa.Government.Executive));
                    EnergyMarket.ResetTurnState();

                    // (g) a world whose player is another country keeps the AI USA's House and president objects across both days - the hook is the player's
                    // country's, never every country's under the same gates
                    (SimulationManager other, World otherWorld, Country otherUsa) = Open(player: false, Eve);
                    other.PlayerCountryId = CountryId.Germany;
                    otherWorld.GetCountry(CountryId.Germany).PlayerPartyAbbrev = "CDU";
                    object otherSeats = otherUsa.ParliamentSeats, otherGov = otherUsa.Government;
                    for (int day = 0; day < 60 && other.CurrentDate < After; day++) { Step(other, otherWorld); }
                    Check(ReferenceEquals(otherUsa.ParliamentSeats, otherSeats) && ReferenceEquals(otherUsa.Government, otherGov) && Same(otherUsa.ParliamentSeats, house118)
                          && otherUsa.Government.Executive.StartsWith("Joseph R. Biden", StringComparison.Ordinal),
                        F("(g) a world whose player is another country (Germany, CDU) keeps the AI USA's eve House and president, the same objects, to {0}", Day(other.CurrentDate)));
                    EnergyMarket.ResetTurnState();

                    // (b) a REP player: the AI government's arrival budget is spent on the first day and resolved by the eve of the oath (a 21-day bill tabled the
                    // 29th - one day of slack, so a longer bill fails here, naming the premise); on the oath's day the REP player's own arrival window opens
                    (SimulationManager rep, World repWorld, Country repUsa) = Open(player: true, Eve);
                    repUsa.PlayerPartyAbbrev = "REP";
                    bool aiBillFirstDay = false, clearOnEve = false, windowOnOath = false;
                    for (int day = 0; day < 60 && rep.CurrentDate < OathDay; day++)
                    {
                        Step(rep, repWorld);
                        DateTime d = rep.CurrentDate;
                        if (d == Eve.AddDays(1))
                        {
                            aiBillFirstDay = !rep.GetPendingBudgetProcess(CountryId.USA) && rep.GetPendingBudgetBill(CountryId.USA) is BudgetBill b && b.GovernmentBill
                                             && b.DaysRemaining == ParliamentSystem.BillDurationDays;
                        }
                        if (d == OathDay.AddDays(-1)) { clearOnEve = !rep.GetPendingBudgetProcess(CountryId.USA) && rep.GetPendingBudgetBill(CountryId.USA) == null; }
                        if (d == OathDay)
                        {
                            windowOnOath = rep.GetPendingBudgetProcess(CountryId.USA) && rep.IsIncomingGovernmentBudgetWindow(CountryId.USA) && rep.GetPendingBudgetBill(CountryId.USA) == null;
                        }
                    }
                    Check(aiBillFirstDay && clearOnEve && windowOnOath,
                        F("(b) a REP player: the AI government's arrival budget tabled on {0} and resolved by {1}; on {2} the REP player's own arrival window opens - the window reset as the record installs",
                            Day(Eve.AddDays(1)), Day(OathDay.AddDays(-1)), Day(OathDay)));
                    EnergyMarket.ResetTurnState();

                    // (c') a game day across the 120th's day: the same House and president objects - nothing seated
                    DateTime lateEve = Day120.AddDays(-4);
                    (SimulationManager late, World lateWorld, Country lateUsa) = Open(player: true, lateEve);
                    object lateSeats = lateUsa.ParliamentSeats, lateGov = lateUsa.Government;
                    bool lateOpened = Same(lateUsa.ParliamentSeats, house119) && lateUsa.Government?.Executive == "Donald J. Trump (REP)";
                    for (int day = 0; day < 10 && late.CurrentDate <= Day120; day++) { Step(late, lateWorld); }
                    Check(lateOpened && late.CurrentDate == Day120.AddDays(1) && ReferenceEquals(lateUsa.ParliamentSeats, lateSeats) && ReferenceEquals(lateUsa.Government, lateGov),
                        F("(c') a game day: a world opened {0} on the 119th and Trump, stepped across the 120th's day to {1}, keeps the same House and president - nothing seated", Day(lateEve), Day(late.CurrentDate)));
                    EnergyMarket.ResetTurnState();

                    // (d), (e): each save loaded INTO THE GAME - the save's own House and government objects kept as it loads, where nothing is owed - and stepped
                    // one day; (h) the save before US-2, both rewritten as it loads, read before any step
                    void FromSave(string path, string label, bool loadMovesNothing, bool step, Func<SimulationManager, Country, object, object, bool> holds, string expect)
                    {
                        SaveGame save = SaveGameService.LoadFromFile(path);
                        Country saved = save.World.GetCountry(CountryId.USA);
                        object seatsSaved = saved.ParliamentSeats, govSaved = saved.Government;
                        SimulationManager adopted = Adopt(save, hosts, out string error);
                        if (adopted == null) { Check(false, F("{0}: {1}", label, error)); return; }
                        try
                        {
                            Country u = adopted.World.GetCountry(CountryId.USA);
                            bool asLoaded = loadMovesNothing
                                ? ReferenceEquals(u.ParliamentSeats, seatsSaved) && ReferenceEquals(u.Government, govSaved)
                                : !ReferenceEquals(u.ParliamentSeats, seatsSaved) && !ReferenceEquals(u.Government, govSaved);
                            object s0 = u.ParliamentSeats, g0 = u.Government;
                            DateTime from = adopted.CurrentDate;
                            if (step) { Step(adopted, adopted.World); }
                            Check(asLoaded && holds(adopted, u, s0, g0), F("{0}: cut on {1}, loaded into the game ({2}){3} - {4} ({5}; {6}; {7})", label, Day(from),
                                loadMovesNothing ? "the save's own House and government kept as it loads" : "both rewritten as it loads",
                                step ? " and stepped to " + Day(adopted.CurrentDate) : ", before any step", expect, Seats(u.ParliamentSeats), u.Government?.Executive, Capital(u)));
                        }
                        finally { EnergyMarket.ResetTurnState(); }
                    }
                    FromSave(saveHouse, "(d) the eve of the House's day", true, true,
                        (s, u, s0, g0) => Same(u.ParliamentSeats, house119) && ReferenceEquals(u.Government, g0) && u.Government.Executive.StartsWith("Joseph R. Biden", StringComparison.Ordinal) && Carried(u),
                        "the 119th House, the same Biden government, the capital carried");
                    FromSave(saveOath, "(d) the eve of the oath", true, true,
                        (s, u, s0, g0) => ReferenceEquals(u.ParliamentSeats, s0) && Same(u.ParliamentSeats, house119) && u.Government.Executive == "Donald J. Trump (REP)" && u.Government.FormedOn == OathDay
                                          && Carried(u) && OathBudget(s),
                        "Trump takes office over the same House; the capital as carried; the window closed and the new government's budget tabled");
                    FromSave(saveAfter, "(e) after both days", true, true,
                        (s, u, s0, g0) => ReferenceEquals(u.ParliamentSeats, s0) && ReferenceEquals(u.Government, g0) && u.Government.Executive == "Donald J. Trump (REP)" && u.Government.FormedOn == OathDay && Carried(u),
                        "nothing changes - the same House and government, Trump formed on the oath's day, the capital as carried");
                    FromSave(saveBefore, "(h) a save before US-2, holding the 118th, Biden and the 118th's capital past their days", false, false,
                        (s, u, s0, g0) => Same(u.ParliamentSeats, house119) && u.Government.Executive == "Donald J. Trump (REP)" && u.Government.FormedOn == s.CurrentDate && Carried(u),
                        "put right as it loads: the 119th, Trump, the capital carried");
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

            sb.Append(failures == 0 ? "=== CongressOfRecordDiagnostic: the record seated on its dates ===" : F("=== CongressOfRecordDiagnostic: {0} FAILED ===", failures));
            if (failures == 0) { Debug.Log(sb.ToString()); } else { Debug.LogError(sb.ToString()); }
            CheckExit.Finish(failures == 0 ? 0 : 1);
        }

        /// <summary>A save loaded INTO THE GAME (§557): a controller in edit mode adopts it through its own `RestoreFromSave` - `PlayProtocolCheck`'s path - its
        /// manager handed to it; the forks the load builds are disposed. Returns the manager the controller now drives, or null with the reason.</summary>
        private static SimulationManager Adopt(SaveGame save, List<GameObject> hosts, out string error)
        {
            error = null;
            var go = new GameObject("CongressOfRecordDiagnostic.Controller"); hosts.Add(go);
            SimulationManager sim = go.AddComponent<SimulationManager>();
            var controller = go.AddComponent<PoliSim.UI.GameController>();
            const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
            Type type = typeof(PoliSim.UI.GameController);
            FieldInfo simField = type.GetField("_simulationManager", Private), playerField = type.GetField("_playerCountry", Private), speedField = type.GetField("_gameSpeed", Private);
            MethodInfo restore = type.GetMethod("RestoreFromSave", Private);
            if (simField == null || playerField == null || speedField == null || restore == null) { error = "the controller's load path could not be reached by name"; return null; }
            simField.SetValue(controller, sim);
            try { restore.Invoke(controller, new object[] { save }); }
            catch (TargetInvocationException e) { error = "the controller cannot adopt the save - " + (e.InnerException ?? e).GetType().Name + ": " + (e.InnerException ?? e).Message; return null; }
            finally
            {
                (type.GetField("_impactLedger", Private)?.GetValue(controller) as PolicyImpactLedger)?.Dispose();
                (type.GetField("_shadowBaseline", Private)?.GetValue(controller) as ShadowBaseline)?.Dispose();
            }
            var player = playerField.GetValue(controller) as Country;
            if (player == null || player.Id != CountryId.USA) { error = "adopted, the controller governs " + (player == null ? "no country" : player.Id.ToString()) + ", not the USA"; return null; }
            if (!ReferenceEquals(sim.World, save.World)) { error = "adopted, the manager and the save hold two worlds"; return null; }
            if (speedField.GetValue(controller).ToString() != "Paused") { error = "adopted, the game is " + speedField.GetValue(controller) + ", not PAUSED"; return null; }
            return sim;
        }

        private static string F(string format, params object[] args) => string.Format(CultureInfo.InvariantCulture, format, args);
    }
}
