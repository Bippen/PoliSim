using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using PoliSim.Data;
using PoliSim.Elections;
using PoliSim.Elections.Generated;
using PoliSim.Persistence;
using PoliSim.Simulation;
using UnityEngine;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// PS-6 US-15 (`docs/specs/USA_STAGE_PLAN.md`, "The Senate as a chamber"; Elias's ruling F8 - R-US1 (a), R-US10 (a); Sinema's caucus a reading he ruled,
    /// G3): THE SENATE OF RECORD BY DATE, THE GENERIC SECOND CHAMBER. The done-when's Senate diagnostic:
    /// <list type="bullet">
    /// <item>(a) Art. I §3 cl. 1, 2 and 4 held to their saved page (NARA's transcript, its SHA-256 in `SHA256SUMS.txt`) by their words;</item>
    /// <item>(b) the chamber: two senators from each state, every state in two of the three classes, the members twice the states; every named day of US-12's
    /// table read again by the runtime (<see cref="UsSenate.On"/>) - the seats by class and party, the vacancies and the caucus sums the generated rows';
    /// (b') every day of each of [SEN-DIV]'s stretches, the runtime's division the page's;</item>
    /// <item>(c) the 118th at the US start (<see cref="WorldClock.StartDate"/>) with the ruled caucus - King, Sanders and Sinema counted with DEM, Manchin from his
    /// change on 2024-06-05; (d) the 119th after 3 January 2025, the vacancies on their days;</item>
    /// <item>(e) the vice president of record on every day of the record, of the president's party; the vice president's vote deciding an equal division - the
    /// 117th's 50-50 Senate held to the Senate's own pages, the game window's 49-49 of 8 December 2024, and the vote's rule (<see cref="UsSenate.Carries"/>);
    /// (f) the view's words, past the record the last of record standing;</item>
    /// <item>(g) a US world opened as a new game builds one, the Senate seated at selection and stepped in the controller's order across the 119th's opening and
    /// the oath - seated as the record on every day, re-seated exactly on the record's change days; EconomicVote's "divided" read over both chambers, planted
    /// both ways;</item>
    /// <item>(h) scope: the gate the USA's alone; a world with no player country, and one whose player is another country, seat no Senate;</item>
    /// <item>(i) the save: format 40 carries it, a load into the game keeps the save's own Senate where nothing is owed, a Senate off its record is put right as
    /// it loads, a save of the format before is refused; (j) the preview's clone carries it as its own copy.</item>
    /// </list>
    /// </summary>
    public static class SenateChamberDiagnostic
    {
        private const string ConstitutionPage = "ElectionsData/usa/raw/records/archives_constitution_transcript.html";
        private const string DivisionPage = "ElectionsData/usa/raw/records/wayback_senate_party_division_20260919.html";
        private const string LeadersPage = "ElectionsData/usa/raw/records/wayback_senate_majority_minority_leaders_briefing_20260905.html";
        private static readonly string[] ConstitutionWords =
        {
            "The Senate of the United States shall be composed of two Senators from each State,",
            "for six Years; and each Senator shall have one Vote.",
            "they shall be divided as equally as may be into three Classes.",
            "The Vice President of the United States shall be President of the Senate, but shall have no Vote, unless they be equally divided.",
        };
        private const string LeadersWords = "tie-breaking vote established a Democratic majority in the Senate";
        private static readonly DateTime Manchin = new DateTime(2024, 6, 5);
        private static readonly DateTime Opens119 = new DateTime(2025, 1, 3);
        private static readonly DateTime Equal117 = new DateTime(2022, 11, 8);
        private static readonly DateTime EqualInWindow = new DateTime(2024, 12, 8);
        private static readonly DateTime WorldOpens = new DateTime(2024, 12, 6);
        private static readonly DateTime WorldUntil = new DateTime(2025, 1, 22);
        private const int Seed = 777;

        public static void Run()
        {
            CheckExit.ArmLogFold();
            var sb = new StringBuilder("=== SenateChamberDiagnostic (PS-6 US-15, R-US1 (a), R-US10 (a)): the Senate of record by date, the generic second chamber ===\n");
            int failures = 0;
            void Check(bool ok, string what) { if (!ok) { failures++; } sb.Append(ok ? "    ok        " : "    FAIL      ").Append(what).Append('\n'); }
            var hosts = new List<GameObject>();
            int seedWas = SimulationRandom.MasterSeed;
            Dictionary<SimulationRandom.Stream, int> drawsWas = SimulationRandom.CaptureDrawCounts();
            try
            {
                string Day(DateTime d) => d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                DateTime Iso(string s) => DateTime.ParseExact(s, "yyyy-MM-dd", CultureInfo.InvariantCulture);
                string Seats(IReadOnlyDictionary<string, int> s) => s == null ? "none" : s.Count == 0 ? "empty" : string.Join(", ", s.Where(kv => kv.Value > 0).OrderBy(kv => kv.Key, StringComparer.Ordinal).Select(kv => kv.Key + " " + kv.Value.ToString(CultureInfo.InvariantCulture)));
                bool Same(IReadOnlyDictionary<string, int> a, IReadOnlyDictionary<string, int> b) => a != null && b != null && a.Count == b.Count && a.All(kv => b.TryGetValue(kv.Key, out int n) && n == kv.Value);
                int Side(UsSenate.Seating s, string key) => s.BySide.TryGetValue(key, out int n) ? n : 0;
                int Letter(UsSenate.Seating s, string letter) => s.Seats.Count(x => x.Party == letter);

                // ---- (a) the Constitution's words, held to the saved page
                Check(PresidencyDiagnostic.Page(ConstitutionPage, out string constitution) && ConstitutionWords.All(constitution.Contains),
                    F("(a) Art. I §3 cl. 1, 2 and 4 ([CONST], archives_constitution_transcript.html) held to its SHA-256, holding \"{0}\"", string.Join("\" ... \"", ConstitutionWords)));

                // ---- (b) the chamber: two senators from each state, in three classes
                var slots = UsPresidentialReturns.SenateSeats.Select(s => (s.State, s.Class)).Distinct().ToList();
                int states = slots.Select(s => s.State).Distinct().Count();
                var byState = slots.GroupBy(s => s.State).ToDictionary(g => g.Key, g => g.Select(x => x.Class).OrderBy(c => c).ToArray());
                int[] perClass = Enumerable.Range(1, UsSenate.Classes).Select(c => slots.Count(s => s.Class == c)).ToArray();
                bool twoClassesEach = byState.Values.All(c => c.Length == UsSenate.SenatorsPerState && c.Distinct().Count() == UsSenate.SenatorsPerState && c.All(k => k >= 1 && k <= UsSenate.Classes));
                bool classesEven = perClass.Max() - perClass.Min() <= 1;   // cl. 2: "divided as equally as may be"
                Check(states * UsSenate.SenatorsPerState == slots.Count && UsSenate.Members == slots.Count && twoClassesEach && classesEven && slots.All(s => s.Class >= 1 && s.Class <= UsSenate.Classes),
                    F("(b) the chamber: {0} states, {1} seats - two senators from each state (cl. 1), each state's two in two different classes, the three classes {2} seats (cl. 2, as equally as may be); the runtime's members {3}",
                        states, slots.Count, string.Join(", ", perClass), UsSenate.Members));

                // every named day of US-12's table, read again by the runtime
                var namedWrong = new List<string>();
                foreach (var on in UsPresidentialReturns.SenateOn)
                {
                    UsSenate.Seating s = UsSenate.On(Iso(on.Day));
                    int[] Of(int c) => new[] { s.Seats.Count(x => x.Class == c && x.Party == "D"), s.Seats.Count(x => x.Class == c && x.Party == "R"), s.Seats.Count(x => x.Class == c && x.Party == "I"), perClass[c - 1] - s.Seats.Count(x => x.Class == c) };
                    int[] got = Of(1).Concat(Of(2)).Concat(Of(3)).Concat(new[] { Letter(s, "D"), Letter(s, "R"), Letter(s, "I"), s.Vacant, Side(s, "DEM"), Side(s, "REP") }).ToArray();
                    int[] want = { on.C1D, on.C1R, on.C1I, on.C1Vacant, on.C2D, on.C2R, on.C2I, on.C2Vacant, on.C3D, on.C3R, on.C3I, on.C3Vacant, on.D, on.R, on.I, on.Vacant, on.DemCaucus, on.RepCaucus };
                    if (!got.SequenceEqual(want)) { namedWrong.Add(F("{0} ({1}): runtime {2}, the table {3}", on.Day, on.What, string.Join(" ", got), string.Join(" ", want))); }
                }
                Check(namedWrong.Count == 0, F("(b) every named day of US-12's table read again by the runtime (UsSenate.On) - {0} days, {1} to {2}: the seats by class and party (D, R, I as the pages print them), the vacancies, the totals and the caucus sums the generated rows'{3}",
                    UsPresidentialReturns.SenateOn.Length, UsPresidentialReturns.SenateOn.First().Day, UsPresidentialReturns.SenateOn.Last().Day, namedWrong.Count > 0 ? "; NOT: " + string.Join("; ", namedWrong) : string.Empty));

                // (b') every day of [SEN-DIV]'s stretches
                int stretchDays = 0;
                var stretchWrong = new List<string>();
                foreach (var d in UsPresidentialReturns.SenateDivision)
                {
                    for (DateTime day = Iso(d.HoldsFrom); day <= Iso(d.HoldsTo); day = day.AddDays(1))
                    {
                        stretchDays++;
                        UsSenate.Seating s = UsSenate.On(day);
                        bool right = Letter(s, "D") == d.D && Letter(s, "R") == d.R && Letter(s, "I") == d.I && s.Vacant == 0 && UsSenate.CongressOn(day) == d.Congress;
                        if (!right && stretchWrong.Count < 5) { stretchWrong.Add(F("{0}: D {1} R {2} I {3} vacant {4}, the {5}", Day(day), Letter(s, "D"), Letter(s, "R"), Letter(s, "I"), s.Vacant, UsSenate.CongressOn(day))); }
                    }
                }
                Check(stretchWrong.Count == 0, F("(b') [SEN-DIV]'s stretches: every day of each ({0} days, {1} stretches) - the runtime's D / R / I the page's line for its Congress, none vacant, the Congress the runtime names (UsSenate.CongressOn) the line's{2}",
                    stretchDays, UsPresidentialReturns.SenateDivision.Length, stretchWrong.Count > 0 ? "; NOT: " + string.Join("; ", stretchWrong) : string.Empty));

                // ---- (c) the 118th at the US start, with the ruled caucus
                DateTime start = WorldClock.StartDate(CountryId.USA);
                UsSenate.Seating atStart = UsSenate.On(start);
                string Independents(UsSenate.Seating s) => string.Join(", ", s.Seats.Where(x => x.Party == "I").Select(x => x.Senator + " (" + x.State + ", with " + x.Side + ")"));
                var sinema = UsPresidentialReturns.SenateSeats.First(s => s.State == "AZ" && s.Party == "D/I");
                bool startRight = UsSenate.CongressOn(start) == 118 && Side(atStart, "DEM") == Letter(atStart, "D") + atStart.Independents && atStart.IndependentsBySide.Keys.All(k => k == "DEM")
                                  && atStart.Seats.Where(x => x.Party == "I").Select(x => x.State).OrderBy(x => x, StringComparer.Ordinal).SequenceEqual(new[] { "AZ", "ME", "VT" })
                                  && atStart.Seats.Any(x => x.State == "WV" && x.Senator.StartsWith("Joe Manchin", StringComparison.Ordinal) && x.Party == "D")
                                  && sinema.Caucus == "D" && sinema.CaucusBy.Contains("G3") && UsSenate.MajorityOf(atStart.BySide, null) == "DEM" && atStart.Vacant == 0
                                  && UsPresidentialReturns.SenateOn.Any(o => o.Day == Day(start) && o.DemCaucus == Side(atStart, "DEM") && o.RepCaucus == Side(atStart, "REP"));
                Check(startRight, F("(c) the 118th at the US start ({0}, WorldClock.StartDate): the {1} Congress, DEM {2} of {3} ({4} D and {5} independents counted with DEM: {6}; Sinema's caucus by Elias's ruling G3, her row's caucus_by), REP {7}, {8} vacant - the majority DEM, and the catalog's own row for the day",
                    Day(start), UsSenate.Ordinal(UsSenate.CongressOn(start)), Side(atStart, "DEM"), atStart.Members, Letter(atStart, "D"), atStart.Independents, Independents(atStart), Side(atStart, "REP"), atStart.Vacant));
                UsSenate.Seating manchin = UsSenate.On(Manchin), dayBefore = UsSenate.On(Manchin.AddDays(-1));
                Check(manchin.Seats.Any(x => x.State == "WV" && x.Party == "I" && x.Side == "DEM") && dayBefore.Seats.Any(x => x.State == "WV" && x.Party == "D") && Side(manchin, "DEM") == Side(dayBefore, "DEM") && manchin.Independents == dayBefore.Independents + 1,
                    F("(c) Manchin an independent from {0} (the Changed Parties page), counted with DEM - the Democrats' caucus {1} the day before and on the day, the independents {2} then {3}",
                        Day(Manchin), Side(dayBefore, "DEM"), dayBefore.Independents, manchin.Independents));

                // ---- (d) the 119th after 3 January 2025, the vacancies on their days
                UsSenate.Seating open119 = UsSenate.On(Opens119), last118 = UsSenate.On(Opens119.AddDays(-1)), full119 = UsSenate.On(new DateTime(2025, 1, 21));
                string Vacant(UsSenate.Seating s)
                {
                    var held = new HashSet<(string, int)>(s.Seats.Select(x => (x.State, x.Class)));
                    var empty = slots.Where(k => !held.Contains(k)).OrderBy(k => k.State, StringComparer.Ordinal).Select(k => k.State + " class " + k.Class.ToString(CultureInfo.InvariantCulture)).ToList();
                    return empty.Count == 0 ? "none vacant" : string.Join(", ", empty) + " vacant";
                }
                Check(UsSenate.CongressOn(Opens119) == 119 && UsSenate.CongressOn(Opens119.AddDays(-1)) == 118 && UsSenate.MajorityOf(open119.BySide, null) == "REP" && UsSenate.MajorityOf(last118.BySide, null) == "DEM"
                      && UsSenate.MajorityOf(full119.BySide, null) == "REP" && full119.Vacant == 0,
                    F("(d) the 119th from {0}: on its last day the 118th's DEM {1} REP {2} ({3}); on the opening the {4}'s DEM {5} REP {6} ({7}); on 2025-01-21 DEM {8} REP {9} ({10})",
                        Day(Opens119), Side(last118, "DEM"), Side(last118, "REP"), Vacant(last118), UsSenate.Ordinal(UsSenate.CongressOn(Opens119)), Side(open119, "DEM"), Side(open119, "REP"), Vacant(open119),
                        Side(full119, "DEM"), Side(full119, "REP"), Vacant(full119)));
                var changes = new List<string>();
                foreach (var c in UsPresidentialReturns.SenateChanges)
                {
                    if (c.Kind != "resigned" && c.Kind != "died" && c.Kind != "sworn") { continue; }
                    DateTime day = Iso(c.Day);
                    UsSenate.Seating s = UsSenate.On(day);
                    bool holds = s.Seats.Any(x => x.State == c.State && x.Class == c.Class && x.Senator == c.Senator);
                    bool right = c.Kind == "sworn" ? holds : !holds;
                    changes.Add(F("{0} {1} class {2} {3} {4} - {5}", c.Day, c.State, c.Class, c.Senator, c.Kind, right ? (c.Kind == "sworn" ? "holds the seat that day" : "holds none that day") : "WRONG"));
                    if (!right) { failures++; }
                }
                sb.Append("    vacancy   ").Append(string.Join("\n    vacancy   ", changes)).Append('\n');
                Check(UsSenate.On(new DateTime(2024, 8, 20)).Vacant == 1 && UsSenate.On(EqualInWindow).Vacant == 2 && UsSenate.On(new DateTime(2025, 1, 10)).Vacant == 2 && UsSenate.On(new DateTime(2025, 1, 20)).Vacant == 2,
                    F("(d) the vacancies on their days: 2024-08-20 {0}; 2024-12-08 {1}; 2025-01-10 {2}; 2025-01-20 {3} - each seat held from the oath and not on the last day (senate_record.md readings 1-3)",
                        Vacant(UsSenate.On(new DateTime(2024, 8, 20))), Vacant(UsSenate.On(EqualInWindow)), Vacant(UsSenate.On(new DateTime(2025, 1, 10))), Vacant(UsSenate.On(new DateTime(2025, 1, 20)))));

                // ---- (e) the vice president of record - PS-6 US-28's one derivation (UsTermLimit.TryVicePresidentOfRecordOn) over the record's tickets, the table
                // TermLimitDiagnostic holds to NARA's pages (UsTermLimit.RunningMatesOfRecord), each ticket read here held to its page too - and the equal division
                int vpDays = 0;
                var vpWrong = new List<string>();
                var vpNames = new List<string>();
                var vpTickets = new List<(int Year, string Party, string Person)>();
                for (DateTime day = new DateTime(2021, 1, 20); day <= WorldClock.RecordDate; day = day.AddDays(1))
                {
                    vpDays++;
                    bool has = UsTermLimit.TryVicePresidentOfRecordOn(CountryId.USA, day, out string person, out string party, out string _);
                    bool gov = WorldClock.TryGovernmentAt(CountryId.USA, day, out WorldClock.GovernmentOfRecord g) && g.Kind == WorldClock.ExecutiveKind.Presidency && g.Cabinet != null && g.Cabinet.Length == 1;
                    int elected = gov ? g.From.Year - 1 : 0;
                    bool inTable = UsTermLimit.RunningMatesOfRecord.Any(t => t.Year == elected && t.Party == party && t.Person == person);
                    if (!has || !gov || party != g.Cabinet[0] || !inTable) { if (vpWrong.Count < 5) { vpWrong.Add(F("{0}: {1} ({2}) beside the president's {3}, the {4} ticket{5}", Day(day), person, party, gov ? g.Cabinet[0] : "none", elected, inTable ? string.Empty : " - not in the table")); } continue; }
                    string label = person + " (" + party + ") from " + Day(g.From);
                    if (!vpNames.Contains(label)) { vpNames.Add(label); vpTickets.Add((elected, party, person)); }
                }
                var vpPages = new List<string>();
                bool pagesHold = vpTickets.Count > 0;
                foreach ((int year, string party, string person) in vpTickets)
                {
                    string words = "Vice President " + person + " [" + (party == "REP" ? "R" : "D") + "]";
                    string page = F("ElectionsData/usa/raw/executive/archives_electoral_college_{0}.html", year);
                    bool held = PresidencyDiagnostic.Page(page, out string html) && html.Contains(words);
                    pagesHold &= held;
                    vpPages.Add(F("\"{0}\" on {1}{2}", words, System.IO.Path.GetFileName(page), held ? string.Empty : " - NOT HELD"));
                }
                Check(vpWrong.Count == 0 && vpNames.Count == 2 && vpNames[0].StartsWith("Kamala D. Harris (DEM)", StringComparison.Ordinal) && vpNames[1].StartsWith("JD Vance (REP)", StringComparison.Ordinal) && pagesHold,
                    F("(e) the vice president of record on every day from 2021-01-20 to the record's date ({0} days), by US-28's derivation (UsTermLimit.TryVicePresidentOfRecordOn): {1} - each the running mate on the ticket that elected the president of record, of his party, an entry of the table TermLimitDiagnostic holds to NARA's pages, and held here to the page: {2}{3}",
                        vpDays, string.Join("; ", vpNames), string.Join("; ", vpPages), vpWrong.Count > 0 ? "; NOT: " + string.Join("; ", vpWrong) : string.Empty));
                bool divisionHeld = PresidencyDiagnostic.Page(DivisionPage, out string division) && division.IndexOf("117th Congress (2021&ndash;2023)", StringComparison.Ordinal) is int at117 && at117 >= 0
                                    && division.IndexOf("Majority Party: Democrats (48 seats)", at117, StringComparison.Ordinal) is int dem117 && dem117 > at117
                                    && division.IndexOf("Congress (", at117 + 20, StringComparison.Ordinal) is int next && (next < 0 || dem117 < next);
                bool leadersHeld = PresidencyDiagnostic.Page(LeadersPage, out string leaders) && leaders.Contains(LeadersWords);
                UsSenate.Seating equal117 = UsSenate.On(Equal117), equalWindow = UsSenate.On(EqualInWindow);
                UsTermLimit.TryVicePresidentOfRecordOn(CountryId.USA, Equal117, out string vp117, out string vpParty117, out string _);
                UsTermLimit.TryVicePresidentOfRecordOn(CountryId.USA, EqualInWindow, out string vpWindow, out string vpPartyWindow, out string _);
                Check(divisionHeld && leadersHeld && UsSenate.EquallyDivided(equal117.BySide) && UsSenate.MajorityOf(equal117.BySide, vpParty117) == "DEM" && UsSenate.MajorityOf(equal117.BySide, null) == null && UsSenate.CongressOn(Equal117) == 117,
                    F("(e) the vice president's vote decides an equal division (cl. 4): the 117th's Senate on {0}, DEM {1} REP {2}, goes to {3}, the party of the vice president of record ({4}) - the Senate's own pages held to their SHA-256: [SEN-DIV]'s 117th \"Majority Party: Democrats (48 seats)\", [SEN-LEAD-H]'s \"{5}\"; with no vice president's party, no one's",
                        Day(Equal117), Side(equal117, "DEM"), Side(equal117, "REP"), UsSenate.MajorityOf(equal117.BySide, vpParty117), vp117, LeadersWords));
                Check(UsSenate.EquallyDivided(equalWindow.BySide) && UsSenate.MajorityOf(equalWindow.BySide, vpPartyWindow) == "DEM" && equalWindow.Vacant == 2,
                    F("(e) in the game's window: on {0} the Senate of record is DEM {1} REP {2} with {3} - an equal division held by the party of the vice president of record, {4} ({5}) (the reading, DECLARED; the record shows no tie-breaking vote that day)",
                        Day(EqualInWindow), Side(equalWindow, "DEM"), Side(equalWindow, "REP"), Vacant(equalWindow), vpWindow, vpPartyWindow));
                var fifty = new Dictionary<string, int> { { "DEM", 50 }, { "REP", 50 } };
                var withZero = new Dictionary<string, int> { { "REP", 49 }, { "DEM", 49 }, { "IND", 0 } };
                Check(UsSenate.Carries(50, 50, true) && !UsSenate.Carries(50, 50, false) && UsSenate.Carries(51, 49, false) && !UsSenate.Carries(49, 51, true)
                      && UsSenate.MajorityOf(fifty, "REP") == "REP" && UsSenate.MajorityOf(fifty, "DEM") == "DEM" && UsSenate.MajorityOf(fifty, "GRN") == null && UsSenate.MajorityOf(withZero, "REP") == "REP"
                      && UsSenate.MajorityOf(new Dictionary<string, int> { { "DEM", 51 }, { "REP", 49 } }, "REP") == "DEM",
                    "(e) the vote's rule: 50-50 carries with the vice president's yea and fails with his nay; 51-49 carries without him; 49-51 fails whatever he votes; a 50-50 chamber is the vice president's party's either way, no one's where his party is not among the tied, and a seatless party never ties; 51-49 is the larger side's whoever is vice president");

                // ---- (b'') before the record's first day the roster is not whole: nothing is seated there (the review's L1)
                DateTime early = UsSenate.RecordOpens.AddDays(-57);
                UsSenate.Seating preRecord = UsSenate.On(early), first = UsSenate.On(UsSenate.RecordOpens);
                Check(!preRecord.Covered && preRecord.Seats.Count == 0 && preRecord.Vacant == 0 && WorldClock.SecondChamberOfRecord(CountryId.USA, early).Count == 0
                      && !StartBrief.TrySecondChamberMajority(CountryId.USA, early, out _, out _, out _, out _, out _, out _) && first.Covered && first.Vacant == 0 && first.Seats.Count == first.Members
                      && WorldClock.SecondChamberStanding(CountryId.USA, early) == "NOT ON RECORD BEFORE " + UsSenate.RecordOpens.ToString("d MMM yyyy", CultureInfo.InvariantCulture).ToUpperInvariant(),
                    F("(b'') before the record's first day ({0}): on {1} no seat is held and nothing is seated (the second chamber of record empty, no majority, \"{2}\"); from {0} the roster is whole - {3} seats held",
                        Day(UsSenate.RecordOpens), Day(early), WorldClock.SecondChamberStanding(CountryId.USA, early), first.Seats.Count));

                // ---- (f) the view's words
                string wordsStart = WorldClock.SecondChamberStanding(CountryId.USA, start), wordsAfter = WorldClock.SecondChamberStanding(CountryId.USA, new DateTime(2026, 10, 1)),
                       wordsPast = WorldClock.SecondChamberStanding(CountryId.USA, new DateTime(2027, 6, 1));
                Check(wordsStart == "THE 118TH CONGRESS · CONVENED 3 JAN 2023" && wordsAfter == "THE 119TH CONGRESS · CONVENED 3 JAN 2025 · AS THE RECORD LAST READS IT, 6 SEP 2026"
                      && wordsPast == "STANDS AS THE 119TH CONGRESS LEFT IT ON RECORD, 6 SEP 2026 · THAT CONGRESS ENDED 3 JAN 2027 · NONE AFTER IT IS ON RECORD"
                      && Same(UsSenate.On(new DateTime(2027, 6, 1)).BySide, UsSenate.On(UsSenate.RecordReach).BySide) && UsSenate.On(new DateTime(2027, 6, 1)).Read == UsSenate.RecordReach
                      && WorldClock.SecondChamberStanding(CountryId.Sweden, start) == null && WorldClock.SecondChamberOfRecord(CountryId.Sweden, start).Count == 0,
                    F("(f) the words: on the start \"{0}\"; past the record's reach ({1}) \"{2}\"; past the last Congress on record \"{3}\" - the reach's Senate stands ({4}); none for a country whose second chamber the record does not seat",
                        wordsStart, Day(UsSenate.RecordReach), wordsAfter, wordsPast, Seats(UsSenate.On(new DateTime(2027, 6, 1)).BySide)));

                using (SimulationManager.EpochScope())
                {
                    // a world on an epoch, as a new game builds one; the player's country the USA with a party, another country, or none
                    (SimulationManager Sim, World World, Country Usa) Open(CountryId? player, DateTime epoch)
                    {
                        SimulationRandom.Seed(Seed);
                        EnergyMarket.ResetCalibration();
                        SimulationManager.SetEpoch(epoch);
                        World w = WorldFactory.CreateDefault();
                        var go = new GameObject("SenateChamberDiagnostic"); hosts.Add(go);
                        var s = go.AddComponent<SimulationManager>();
                        s.SetWorld(w);
                        if (player.HasValue) { s.PlayerCountryId = player; w.GetCountry(player.Value).PlayerPartyAbbrev = player == CountryId.USA ? "DEM" : "CDU"; }
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
                    MethodInfo seatAtSelection = typeof(SimulationManager).GetMethod("SeatTheSecondChamberOfRecord", BindingFlags.Instance | BindingFlags.NonPublic);
                    Check(seatAtSelection != null, "the selection's seat (SimulationManager.SeatTheSecondChamberOfRecord) reached by name");

                    // ---- (h) scope
                    bool usaOnly = Enum.GetValues(typeof(CountryId)).Cast<CountryId>().All(id => WorldClock.RecordSeatsSecondChamber(id) == (id == CountryId.USA));
                    Check(usaOnly, "(h) the gate: the record seats a second chamber in the USA alone, over every country");
                    try
                    {
                        (SimulationManager bare, World bareWorld, Country bareUsa) = Open(null, new DateTime(2024, 12, 30));
                        seatAtSelection?.Invoke(bare, null);
                        for (int i = 0; i < 6; i++) { Step(bare, bareWorld); }
                        (SimulationManager other, World otherWorld, Country otherUsa) = Open(CountryId.Germany, new DateTime(2024, 12, 30));
                        seatAtSelection?.Invoke(other, null);
                        for (int i = 0; i < 6; i++) { Step(other, otherWorld); }
                        bool noneSeated = bareWorld.Countries.All(c => c.SecondChamberSeats != null && c.SecondChamberSeats.Count == 0) && otherWorld.Countries.All(c => c.SecondChamberSeats != null && c.SecondChamberSeats.Count == 0);
                        Check(noneSeated && bare.CurrentDate > Opens119 && other.CurrentDate > Opens119,
                            F("(h) a US world with no player country, and one whose player is Germany, stepped across the 119th's opening to {0}: no country seats a second chamber - the AI USA's {1}, Germany's {2}",
                                Day(bare.CurrentDate), Seats(otherUsa.SecondChamberSeats), Seats(otherWorld.GetCountry(CountryId.Germany).SecondChamberSeats)));
                    }
                    finally { EnergyMarket.ResetTurnState(); }

                    // ---- (g) a US world, the Senate seated at selection and stepped across the 119th's opening and the oath
                    (SimulationManager sim, World world, Country usa) = Open(CountryId.USA, WorldOpens);
                    try
                    {
                        bool emptyBefore = usa.SecondChamberSeats != null && usa.SecondChamberSeats.Count == 0;
                        seatAtSelection?.Invoke(sim, null);
                        Dictionary<string, int> atOpen = WorldClock.SecondChamberOfRecord(CountryId.USA, WorldOpens);
                        Check(emptyBefore && Same(usa.SecondChamberSeats, atOpen) && !ReferenceEquals(usa.SecondChamberSeats, atOpen),
                            F("(g) a world opened {0} as a new game builds one seats no second chamber until the selection seats the record's - {1} (its own copy)", Day(WorldOpens), Seats(usa.SecondChamberSeats)));
                        // EconomicVote on the opening day: Biden (DEM), the 118th House (REP), the Senate DEM
                        Dictionary<string, double> evOpen = EconomicVote.Magnitudes(usa, sim.CurrentDate);
                        int days = 0, offRecord = 0;
                        var reseated = new List<string>();
                        var recordChanges = new List<string>();
                        // the vice president whose party decides an equal division, read on every day as the tie-break reads it (UsSenate.VicePresidentParty) against
                        // US-28's derivation (UsTermLimit.TryVicePresidentOn) and the presidency in office's own party - the three must agree, so no figure moves
                        var vpDisagree = new List<string>();
                        var vpRuns = new List<string>();
                        while (sim.CurrentDate < WorldUntil && days < 60)
                        {
                            object before = usa.SecondChamberSeats;
                            Dictionary<string, int> recordBefore = WorldClock.SecondChamberOfRecord(CountryId.USA, sim.CurrentDate);
                            Step(sim, world);
                            days++;
                            Dictionary<string, int> record = WorldClock.SecondChamberOfRecord(CountryId.USA, sim.CurrentDate);
                            if (!Same(usa.SecondChamberSeats, record)) { offRecord++; }
                            if (!ReferenceEquals(before, usa.SecondChamberSeats)) { reseated.Add(Day(sim.CurrentDate)); }
                            if (!Same(recordBefore, record)) { recordChanges.Add(Day(sim.CurrentDate) + " (" + Seats(record) + ")"); }
                            string tieBreak = UsSenate.VicePresidentParty(usa, sim.CurrentDate);
                            bool held28 = UsTermLimit.TryVicePresidentOn(usa, sim.CurrentDate, out string vp28, out string vpParty28, out string _);
                            if (!held28 || tieBreak != vpParty28 || tieBreak != usa.Government?.PmParty) { if (vpDisagree.Count < 5) { vpDisagree.Add(F("{0}: tie-break {1}, US-28 {2} ({3}), the presidency's {4}", Day(sim.CurrentDate), tieBreak, vp28, vpParty28, usa.Government?.PmParty)); } }
                            string run = vp28 + " (" + vpParty28 + ")";
                            if (vpRuns.Count == 0 || !vpRuns[vpRuns.Count - 1].StartsWith(run, StringComparison.Ordinal)) { vpRuns.Add(run + " from " + Day(sim.CurrentDate)); }
                        }
                        Check(vpDisagree.Count == 0 && vpRuns.Count == 2,
                            F("(g) on each of the {0} days the tie-break's vice president's party (UsSenate.VicePresidentParty) is US-28's (UsTermLimit.TryVicePresidentOn) and the presidency in office's own: {1}{2}",
                                days, string.Join("; ", vpRuns), vpDisagree.Count > 0 ? "; NOT: " + string.Join("; ", vpDisagree) : string.Empty));
                        Check(sim.CurrentDate == WorldUntil && offRecord == 0 && reseated.Count == recordChanges.Count && reseated.SequenceEqual(recordChanges.Select(r => r.Substring(0, 10)))
                              && recordChanges.Any(r => r.StartsWith(Day(Opens119), StringComparison.Ordinal)),
                            F("(g) stepped in the controller's order to {0} ({1} days): the Senate seated is the record's on every day, and re-seated on exactly the record's {2} change days - {3}",
                                Day(sim.CurrentDate), days, recordChanges.Count, string.Join(", ", recordChanges)));
                        Check(usa.Government?.Executive == "Donald J. Trump (REP)" && Same(usa.SecondChamberSeats, WorldClock.SecondChamberOfRecord(CountryId.USA, WorldUntil)) && UsSenate.VicePresidentParty(usa, sim.CurrentDate) == "REP",
                            F("(g) on {0}: the president {1}, the vice president's party {2} (the president's, one ticket), the Senate {3}", Day(sim.CurrentDate), usa.Government?.Executive, UsSenate.VicePresidentParty(usa, sim.CurrentDate), Seats(usa.SecondChamberSeats)));

                        // EconomicVote's "divided" over both chambers (DECLARED), live and planted
                        Dictionary<string, double> evAfter = EconomicVote.Magnitudes(usa, sim.CurrentDate);
                        Dictionary<string, int> saved = usa.SecondChamberSeats;
                        double Planted(Dictionary<string, int> senate) { usa.SecondChamberSeats = senate; try { return EconomicVote.Magnitudes(usa, sim.CurrentDate).TryGetValue("REP", out double m) ? m : double.NaN; } finally { usa.SecondChamberSeats = saved; } }
                        double toDem = Planted(new Dictionary<string, int> { { "REP", 47 }, { "DEM", 53 } }), even = Planted(new Dictionary<string, int> { { "REP", 50 }, { "DEM", 50 } }), none = Planted(new Dictionary<string, int>());
                        bool evRight = evOpen.Count == 1 && evOpen.TryGetValue("DEM", out double open) && open == EconomicVote.UsPresidentDivided
                                       && evAfter.Count == 1 && evAfter.TryGetValue("REP", out double after) && after == EconomicVote.UsPresidentUnified
                                       && toDem == EconomicVote.UsPresidentDivided && even == EconomicVote.UsPresidentUnified && none == EconomicVote.UsPresidentUnified && ReferenceEquals(usa.SecondChamberSeats, saved);
                        Check(evRight, F("(g) EconomicVote reads \"divided\" over both chambers where a second chamber is seated (DECLARED, [BK-D] p. 245): on {0} Biden (DEM), the House REP, the Senate DEM - {1:0.00}; on {2} Trump (REP), the House and the Senate REP - {3:0.00}; planted under Trump and the REP House: the Senate DEM 53 - {4:0.00} (the House alone would read unified), 50-50 - {5:0.00} (the vice president's party), none seated - {6:0.00} (the House alone, as before)",
                            Day(WorldOpens), evOpen.TryGetValue("DEM", out double o) ? o : double.NaN, Day(WorldUntil), evAfter.TryGetValue("REP", out double a) ? a : double.NaN, toDem, even, none));

                        // ---- (j) the preview's clone
                        MethodInfo clone = typeof(SimulationManager).GetMethod("ClonePreviewCountry", BindingFlags.NonPublic | BindingFlags.Static);
                        var cloned = clone?.Invoke(null, new object[] { usa }) as Country;
                        Check(cloned != null && Same(cloned.SecondChamberSeats, usa.SecondChamberSeats) && !ReferenceEquals(cloned.SecondChamberSeats, usa.SecondChamberSeats),
                            F("(j) the preview's clone (ClonePreviewCountry) carries the Senate as its own copy - {0}", Seats(cloned?.SecondChamberSeats)));

                        // ---- (i) the save
                        string json = SaveGameService.Serialize(SaveGameService.CreateSaveGame(sim, world, CountryId.USA, null));
                        SaveGame cut = SaveGameService.Deserialize(json);
                        Country savedUsa = cut.World.GetCountry(CountryId.USA);
                        Check(cut.SaveVersion == SaveGameService.CurrentSaveVersion && Same(savedUsa.SecondChamberSeats, usa.SecondChamberSeats),
                            F("(i) a save cut on {0} carries format {1} and the Senate by content - {2}", Day(sim.CurrentDate), cut.SaveVersion, Seats(savedUsa.SecondChamberSeats)));
                        object savedSenate = savedUsa.SecondChamberSeats;
                        (SimulationManager adopted, PoliSim.UI.GameController controller) = PresidencyDiagnostic.Adopt(cut, hosts, out string error);
                        if (adopted == null) { Check(false, "(i) the save loaded into the game: " + error); }
                        else
                        {
                            try
                            {
                                Country u = adopted.World.GetCountry(CountryId.USA);
                                bool kept = ReferenceEquals(u.SecondChamberSeats, savedSenate);
                                object loadedSenate = u.SecondChamberSeats;
                                Step(adopted, adopted.World);
                                Check(kept && ReferenceEquals(u.SecondChamberSeats, loadedSenate) && Same(u.SecondChamberSeats, WorldClock.SecondChamberOfRecord(CountryId.USA, adopted.CurrentDate)),
                                    F("(i) loaded INTO THE GAME (the controller's RestoreFromSave): the save's own Senate object kept as it loads, nothing owed, and stepped to {0} unchanged - {1}", Day(adopted.CurrentDate), Seats(u.SecondChamberSeats)));
                            }
                            finally { EnergyMarket.ResetTurnState(); }
                        }
                        SaveGame planted = SaveGameService.Deserialize(json);
                        Dictionary<string, int> the118th = WorldClock.SecondChamberOfRecord(CountryId.USA, start);
                        planted.World.GetCountry(CountryId.USA).SecondChamberSeats = new Dictionary<string, int>(the118th);
                        (SimulationManager fixedUp, PoliSim.UI.GameController _) = PresidencyDiagnostic.Adopt(planted, hosts, out string plantedError);
                        if (fixedUp == null) { Check(false, "(i) the planted save loaded into the game: " + plantedError); }
                        else
                        {
                            try
                            {
                                Country u = fixedUp.World.GetCountry(CountryId.USA);
                                Check(Same(u.SecondChamberSeats, WorldClock.SecondChamberOfRecord(CountryId.USA, fixedUp.CurrentDate)) && !Same(u.SecondChamberSeats, the118th),
                                    F("(i) a save holding the 118th's Senate ({0}) past its day is put right as it loads, before any step - {1} on {2} (held as state)", Seats(the118th), Seats(u.SecondChamberSeats), Day(fixedUp.CurrentDate)));
                            }
                            finally { EnergyMarket.ResetTurnState(); }
                        }
                        string older = json.Replace("\"SaveVersion\": " + SaveGameService.CurrentSaveVersion.ToString(CultureInfo.InvariantCulture), "\"SaveVersion\": " + (SaveGameService.CurrentSaveVersion - 1).ToString(CultureInfo.InvariantCulture));
                        bool refused = false; string why = null;
                        try { SaveGameService.Deserialize(older); } catch (SaveLoadException e) { refused = true; why = e.Message; }
                        Check(older != json && refused && why.Contains("format version " + (SaveGameService.CurrentSaveVersion - 1).ToString(CultureInfo.InvariantCulture)),
                            F("(i) a save of the format before ({0}) is refused with the plain message (ruling A): \"{1}\"", SaveGameService.CurrentSaveVersion - 1, why));
                    }
                    finally { EnergyMarket.ResetTurnState(); }
                }
            }
            catch (Exception ex) { failures++; sb.Append("    FAIL      threw: ").Append(ex).Append('\n'); }
            finally
            {
                foreach (GameObject go in hosts) { if (go != null) { UnityEngine.Object.DestroyImmediate(go); } }
                EnergyMarket.ResetTurnState();
                SimulationRandom.RestoreState(seedWas, drawsWas);
            }

            sb.Append(failures == 0 ? "=== SenateChamberDiagnostic: the Senate of record seated on its dates ===" : F("=== SenateChamberDiagnostic: {0} FAILED ===", failures));
            if (failures == 0) { Debug.Log(sb.ToString()); } else { Debug.LogError(sb.ToString()); }
            CheckExit.Finish(failures == 0 ? 0 : 1);
        }

        private static string F(string format, params object[] args) => string.Format(CultureInfo.InvariantCulture, format, args);
    }
}
