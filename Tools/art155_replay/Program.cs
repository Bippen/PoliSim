using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using PoliSim.Data;
using PoliSim.Elections;

// K302 (Tools/art155_replay): Poland's Art. 155 and its declared support lines measured OUT OF UNITY on the runtime's own functions - the formation
// (`CoalitionFormation.Form` on the election's reading, what `GovernmentFormation.ViewOf` and `SimulationManager.ProjectSejm` read), the Sejm's vote
// (`SejmFormation.Carries`), the President's cabinet and its simple-majority vote (`SejmFormation.PresidentAppoints`), the shortened term's day
// (`SejmFormation.ShortenedTermElectionDay`). An in-run Sejm no record dates walks Art. 154 ust. 1-3 on the formation's projection (it carries wherever the
// formation forms one) and, where none, Art. 155. Run: bash Tools/art155_replay/run.sh <worktree> <mode> ...
//   chamber <yyyy-MM-dd> "<KEY n, ...>" [--pis-konf]   one chamber through the steps, every admissible cabinet's Art. 155 vote listed (konf's line beside)
//   dump <dump log>                              every Polish in-run election a TrajectoryBaselineDump log holds: the log's install against the steps here
//   konf <dump log>                              every "Konf led by Konf with PiS" install in the log, formed again under the reading the declarations do
//                                                NOT hold - PiS -> Konf read in F1's shape (one way, support-blocking) - through Art. 154 and Art. 155
//   dates                                        Art. 98 ust. 5's day against the 2007 shortened term of record
//   strip <yyyy-MM-dd> "<KEY n, ...>"            the formation as read and with the declared lines' support halves cut - what those halves keep out
//   calendar                                     the clauses that need no SimulationManager, on plain Country objects: WorldCalendarDiagnostic's Polish one and
//                                                its M5 second day; SejmFormationDiagnostic (f)'s 2007 day, (g)'s calendar tail (the shortened term's Sejm, its
//                                                re-anchored term, the next ordinary election's own day, the 40th-to-45th-day window) and (j); the
//                                                re-anchored calendar's identity to the chain over 25 ordinary terms, asked weekly and on every count's own
//                                                day. Not (k): the declared lines are the diagnostic's alone. Exits 1 on a miss
// A prediction where it reads a dump: a run that reaches Art. 155 parts from the log after that election, so only the next dump decides the rest.
static class Program
{
    static string F(string f, params object[] a) => string.Format(CultureInfo.InvariantCulture, f, a);

    static int Main(string[] args)
    {
        if (args.Length == 0) { Console.WriteLine("modes: chamber <day> \"<seats>\" [--pis-konf] | dump <log> | konf <log> | dates | strip <day> \"<seats>\" | calendar"); return 2; }
        switch (args[0])
        {
            case "chamber": return Chamber(DateTime.ParseExact(args[1], "yyyy-MM-dd", CultureInfo.InvariantCulture), ParseSeats(args[2], ","), true,
                args.Length > 3 && args[3] == "--pis-konf" ? Counterfactual(PartySystems.For(CountryId.Poland)) : null);
            case "dump": return Dump(args[1], false);
            case "konf": return Dump(args[1], true);
            case "dates": return Dates();
            case "strip": return Strip(DateTime.ParseExact(args[1], "yyyy-MM-dd", CultureInfo.InvariantCulture), ParseSeats(args[2], ","));
            case "calendar": return Calendar();
            default: Console.WriteLine("unknown mode " + args[0]); return 2;
        }
    }

    /// <summary>`calendar`: the clauses the Unity diagnostics hold that need no SimulationManager, run here on plain Country objects.</summary>
    static int Calendar()
    {
        int misses = 0;
        void Say(bool ok, string what) { if (!ok) { misses++; } Console.WriteLine((ok ? "   ok    " : "   MISS  ") + what); }
        ElectionRecord Count(DateTime d) => new ElectionRecord { Date = d, CountryId = "Poland", Method = ElectionMethod.PolandDistricts };
        // WorldCalendarDiagnostic's Polish clause
        var poland = new Country { Id = CountryId.Poland };
        poland.Government = GovernmentRecord.OfNamedCabinet("KO", new[] { "KO", "TD", "NL" }, new string[0], new DateTime(2023, 12, 13), "planted");
        var plPoll = new DateTime(2027, 10, 17);
        poland.ElectionHistory.Add(Count(plPoll));
        DateTime plSitting = WorldCalendar.FirstSittingAfter(poland, plPoll);
        poland.Government.Discharge(plSitting);
        bool extraPl = WorldCalendar.TryExtraElection(poland, out DateTime plDay, out DateTime plOrdered, out string plWhy);
        bool plPolls = extraPl && WorldCalendar.IsChamberPollingDay(poland, plDay, out bool plExtra) && plExtra;
        poland.ElectionHistory.Add(Count(plDay));
        DateTime plSitting2 = WorldCalendar.FirstSittingAfter(poland, plDay);
        bool plDue = !WorldCalendar.FormationDue(poland, plSitting2.AddDays(-1), out _, out _) && WorldCalendar.FormationDue(poland, plSitting2, out _, out _);
        bool plOwedNothing = !WorldCalendar.TryExtraElection(poland, out _, out _, out _);
        bool plNext = WorldCalendar.TryNextOrdinaryDay(poland, plDay.AddDays(1), out DateTime plOrdinary);
        Say(plSitting == new DateTime(2027, 11, 16) && extraPl && plOrdered == plSitting && plDay == new DateTime(2027, 12, 26) && plPolls && plSitting2 == new DateTime(2028, 1, 10) && plDue && plOwedNothing
            && plNext && plOrdinary == new DateTime(2031, 12, 14) && plWhy != null && plWhy.Contains("Art. 155 ust. 2"),
            F("WorldCalendarDiagnostic, Poland: sitting {0:yyyy-MM-dd}, extra {1:yyyy-MM-dd} ordered {2:yyyy-MM-dd}, polls {3}, next sitting {4:yyyy-MM-dd}, due {5}, owed nothing {6}, next ordinary {7:yyyy-MM-dd}",
                plSitting, plDay, plOrdered, plPolls, plSitting2, plDue, plOwedNothing, plOrdinary));
        // the review's L6: an AI Poland's caretaker is the President's refused cabinet, formed and discharged on the sitting - the calendar owes its election and says so
        var refusedPl = new Country { Id = CountryId.Poland };
        refusedPl.ElectionHistory.Add(Count(plPoll));
        refusedPl.Government = GovernmentRecord.OfNamedCabinet("PiS", new[] { "PiS" }, new string[0], plSitting, "planted: the President's cabinet, refused");
        refusedPl.Government.Discharge(plSitting);
        // R2-L1: without FormAsAi's failure line the same caretaker (a player's Art. 154 ust. 2 designee, refused while ust. 3 runs) owes nothing
        bool owedWithoutLine = WorldCalendar.TryExtraElection(refusedPl, out DateTime _, out DateTime _, out string _);
        refusedPl.Government.Breaks.Add(F("{0:yyyy-MM-dd}: {1}{2:yyyy-MM-dd} - planted; the President's cabinet governs on as caretaker", plSitting, WorldCalendar.FailureMarker, plPoll));
        bool owedRefused = WorldCalendar.TryExtraElection(refusedPl, out DateTime refusedDay, out DateTime refusedOrder, out string _);
        string refusedLine = WorldCalendar.GovernsLine(refusedPl, plSitting);
        bool notDue = !WorldCalendar.FormationDue(refusedPl, plSitting.AddDays(1), out _, out _);
        Say(!owedWithoutLine && owedRefused && refusedDay == new DateTime(2027, 12, 26) && refusedOrder == plSitting && notDue && refusedLine.Contains("governs as caretaker; a new election on 26 Dec 2027"),
            F("L6 / R2-L1: the President's refused cabinet, formed and discharged on {0:yyyy-MM-dd}: without FormAsAi's failure line nothing owed ({1}); with it elections owed {2:yyyy-MM-dd}, no formation due again - \"{3}\"",
                plSitting, owedWithoutLine ? "OWED - WRONG" : "so", refusedDay, refusedLine));
        // M5's second ordinary day, unchanged
        var fresh = new Country { Id = CountryId.Poland };
        fresh.ElectionHistory.Add(Count(plPoll));
        Say(WorldCalendar.TryNextOrdinaryDay(fresh, plPoll.AddDays(1), out DateTime second) && second == new DateTime(2031, 10, 19), F("WorldCalendarDiagnostic M5: Poland after 2027-10-17: {0:yyyy-MM-dd}", second));
        // SejmFormationDiagnostic (g)'s calendar tail: the player's steps' days, and the shortened term's Sejm
        DateTime sitting27 = plPoll.AddDays(30), confidence27 = sitting27.AddDays(SejmFormation.StepWindowDays + SejmFormation.PresidentAppointsAfterSejmVoteDays + SejmFormation.StepWindowDays);
        DateTime polling4 = SejmFormation.ShortenedTermElectionDay(confidence27);
        var pl4 = new Country { Id = CountryId.Poland };
        pl4.ElectionHistory.Add(Count(plPoll));
        pl4.ElectionHistory.Add(Count(polling4));
        DateTime sitting4 = SejmFormation.FirstSittingAfter(pl4, polling4);
        WorldCalendar.TryNextOrdinaryDay(pl4, polling4.AddDays(1), out DateTime reanchored4);
        WorldClock.TryNextPollingDay(CountryId.Poland, polling4.AddDays(1), out DateTime chain4);
        Say(confidence27 == new DateTime(2028, 1, 11) && polling4 == new DateTime(2028, 2, 20) && SejmFormation.ElectedAfterShortenedTerm(pl4, polling4) && sitting4 == new DateTime(2028, 3, 6)
            && SejmFormation.OpenAfter(pl4, polling4).FirstSitting == sitting4 && reanchored4 == new DateTime(2032, 2, 8) && chain4 == new DateTime(2031, 10, 19),
            F("(g): confidence {0:yyyy-MM-dd}, elections {1:yyyy-MM-dd}, first sitting {2:yyyy-MM-dd}, next ordinary {3:yyyy-MM-dd} (the old chain's {4:yyyy-MM-dd})", confidence27, polling4, sitting4, reanchored4, chain4));
        // the review's L1: on that ordinary election's own day, its count in, the calendar answers the day itself
        pl4.ElectionHistory.Add(Count(reanchored4));
        WorldCalendar.TryNextOrdinaryDay(pl4, reanchored4, out DateTime sameDay4);
        WorldCalendar.TryNextOrdinaryDay(pl4, reanchored4.AddDays(1), out DateTime following4);
        WorldClock.TryNextPollingDay(CountryId.Poland, reanchored4, out DateTime chainOnDay4);
        Say(sameDay4 == reanchored4 && following4 == new DateTime(2036, 2, 10) && chainOnDay4 == new DateTime(2035, 10, 21),
            F("(g) L1: on {0:yyyy-MM-dd} with its count in, the calendar answers {1:yyyy-MM-dd} (the old chain {2:yyyy-MM-dd}); the day after, {3:yyyy-MM-dd}", reanchored4, sameDay4, chainOnDay4, following4));
        // the review's L3: the window the steps' orders reach - a Tuesday (after an ordinary count) and a Monday (after a shortened term's, or 2023's record days) -
        // gives the 40th or 41st day; a Wednesday order would give the 39th, which (g) fails
        int[] reached = new int[7];
        for (DateTime o = new DateTime(2027, 1, 4); o < new DateTime(2027, 1, 11); o = o.AddDays(1)) { reached[(int)o.DayOfWeek] = (SejmFormation.ShortenedTermElectionDay(o) - o).Days; }
        Say(reached[(int)DayOfWeek.Tuesday] == 40 && reached[(int)DayOfWeek.Monday] == 41 && reached[(int)DayOfWeek.Wednesday] == 39
            && (polling4 - confidence27).Days >= 40 && (new DateTime(2027, 12, 26) - plSitting).Days >= 40,
            F("(g)/L3: an order on a Tuesday gives day {0}, a Monday day {1}, a Wednesday day {2}; (g)'s player day {3}, the AI's day {4}", reached[(int)DayOfWeek.Tuesday], reached[(int)DayOfWeek.Monday],
                reached[(int)DayOfWeek.Wednesday], (polling4 - confidence27).Days, (new DateTime(2027, 12, 26) - plSitting).Days));
        // (j): the record's 2023 count and an ordinary count unchanged
        var pl1 = new Country { Id = CountryId.Poland };
        DateTime poll23 = WorldClock.LatestElectionDay(CountryId.Poland);
        pl1.ElectionHistory.Add(Count(poll23));
        Say(!SejmFormation.ElectedAfterShortenedTerm(pl1, poll23) && SejmFormation.FirstSittingAfter(pl1, poll23) == new DateTime(2023, 11, 13) && SejmFormation.OpenAfter(pl1, poll23).FirstSitting == new DateTime(2023, 11, 13)
            && !SejmFormation.ElectedAfterShortenedTerm(fresh, plPoll) && SejmFormation.FirstSittingAfter(fresh, plPoll) == plPoll.AddDays(30), "(j): 2023's first sitting 2023-11-13; an ordinary count's the thirtieth day");
        // the identity: with no term shortened, the calendar is the chain's on every day of a century of ordinary counts
        var ordinary = new Country { Id = CountryId.Poland };
        DateTime d = poll23; int days = 0, differ = 0;
        for (int k = 0; k < 25; k++)
        {
            WorldClock.TryNextPollingDay(CountryId.Poland, d.AddDays(1), out DateTime nextChain);
            for (DateTime x = d.AddDays(1); x <= nextChain; x = x.AddDays(7)) { days++; if (!WorldCalendar.TryNextOrdinaryDay(ordinary, x, out DateTime y) || y != nextChain) { differ++; } }
            ordinary.ElectionHistory.Add(Count(nextChain));
            if (SejmFormation.FirstSittingAfter(ordinary, nextChain) != nextChain.AddDays(30)) { differ++; }
            days++;   // the count's own day, its count in (the review's L1): the chain answers the day, and so must the calendar
            if (!WorldCalendar.TryNextOrdinaryDay(ordinary, nextChain, out DateTime onDay) || !WorldClock.TryNextPollingDay(CountryId.Poland, nextChain, out DateTime chainOnDay) || onDay != chainOnDay) { differ++; }
            d = nextChain;
        }
        Say(differ == 0, F("identity: 25 ordinary terms from 2023, {0} days asked (weekly, and every count's own day with its count in) - the re-anchored calendar and the chain differ on {1}", days, differ));
        Say(SejmFormation.ShortenedTermElectionDay(new DateTime(2007, 9, 7)) == new DateTime(2007, 10, 21), "(f): 2007-09-07 -> 2007-10-21");
        Console.WriteLine(F("CALENDAR: {0}", misses == 0 ? "every clause holds" : misses + " MISS(ES)"));
        return misses == 0 ? 0 : 1;
    }

    static Dictionary<string, int> ParseSeats(string s, string sep)
    {
        var held = new Dictionary<string, int>();
        foreach (string part in s.Split(new[] { sep }, StringSplitOptions.RemoveEmptyEntries))
        {
            string p = part.Trim(); int sp = p.LastIndexOf(' ');
            held[p.Substring(0, sp)] = int.Parse(p.Substring(sp + 1), CultureInfo.InvariantCulture);
        }
        return held;
    }

    /// <summary>The counterfactual line `konf` reads: PiS neither joins nor supports a cabinet that includes Konfederacja (F1's shape, one way) - NOT what the
    /// declarations say (coalition_declarations_2023.md §1: "A shared government is refused; support is not addressed").</summary>
    static List<RedLine> Counterfactual(IReadOnlyList<PoliticalParty> parties)
    {
        int pis = Index(parties, "PiS"), konf = Index(parties, "Konf");
        return new List<RedLine> { new RedLine(pis, konf, RedLineKind.Declared, true, "COUNTERFACTUAL (measured, not held): PiS -> Konf read as F1's support-blocking shape", oneWay: true) };
    }

    static int Index(IReadOnlyList<PoliticalParty> parties, string key) { for (int p = 0; p < parties.Count; p++) { if (parties[p].Abbrev == key) { return p; } } return -1; }

    sealed class Outcome { public string Step; public string Government; public bool Formed; public string Detail; public SejmFormation.PresidentsCabinet Named; }

    /// <summary>An in-run Sejm's steps as `ProjectSejm` takes them where no record dates them (the AI's path, run whole on the first sitting).</summary>
    static Outcome Steps(DateTime day, Dictionary<string, int> held, List<RedLine> extra, bool verbose)
    {
        IReadOnlyList<PoliticalParty> parties = PartySystems.For(CountryId.Poland);
        int[] seats = parties.Select(p => held.TryGetValue(p.Abbrev, out int s) ? s : 0).ToArray();
        DeclarationReading reading = DeclarationReading.OfElection(CountryId.Poland, day);
        List<RedLine> lines = reading.Lines(CountryId.Poland, parties);
        if (extra != null) { lines.AddRange(extra); }
        List<InOrAgainst> rules = reading.Platforms(CountryId.Poland, parties);
        int[] joint = ChamberRules.JointMasks(CountryId.Poland, parties, seats);
        double[,] compat = GovernmentFormation.Compatibility(parties);
        CoalitionResult r = CoalitionFormation.Form(seats, compat, lines, ChamberRules.UsesNegativeParliamentarism(CountryId.Poland), rules, joint);
        string Names(int mask) => string.Join("+", Enumerable.Range(0, parties.Count).Where(p => (mask & (1 << p)) != 0).Select(p => parties[p].Abbrev));
        var country = new Country { Id = CountryId.Poland, ParliamentSeats = new Dictionary<string, int>(held) };
        if (extra == null)
        {
            // self-check: the formation here is the game's projection (`GovernmentFormation.ViewOf` on the election's reading)
            GovernmentFormation.View view = GovernmentFormation.ViewOf(country, reading);
            string mine = r.Outcome == CoalitionOutcomeKind.NewElection ? "none" : Names(r.Government.Cabinet);
            string theirs = view.HasGovernment ? string.Join("+", parties.Where(p => view.Cabinet.Any(c => c.Abbrev == p.Abbrev)).Select(p => p.Abbrev)) : "none";
            if (mine != theirs) { throw new InvalidOperationException("the replay's formation " + mine + " is not ViewOf's " + theirs); }
        }
        if (r.Outcome != CoalitionOutcomeKind.NewElection)
        {
            var cab = new List<string>(); var sup = new List<string>();
            for (int p = 0; p < parties.Count; p++) { if ((r.Government.Cabinet & (1 << p)) != 0) { cab.Add(parties[p].Abbrev); } else if ((r.Government.Support & (1 << p)) != 0) { sup.Add(parties[p].Abbrev); } }
            bool carried = SejmFormation.Carries(country, cab, sup, out int yes, out int members);
            string gov = string.Join("+", cab) + (sup.Count > 0 ? " with " + string.Join("+", sup) : string.Empty);
            if (carried) { return new Outcome { Step = "Art. 154 ust. 2", Government = gov, Formed = true, Detail = F("{0} of {1}", yes, members) }; }
            // never reached for an in-run chamber: a formation that forms carries its own investiture's majority
            return new Outcome { Step = "Art. 154 ust. 2 REFUSED", Government = gov, Formed = false, Detail = F("{0} of {1} - UNEXPECTED", yes, members) };
        }
        SejmFormation.PresidentsCabinet named = SejmFormation.PresidentAppoints(CountryId.Poland, parties, seats, reading, extra);
        if (verbose)
        {
            CoalitionFormation.Chamber ch = CoalitionFormation.Prepare(seats, compat, lines, false, rules, joint);
            Console.WriteLine(F("   Art. 154: the formation forms no government; ust. 1 designates none, ust. 3 elects none. Art. 155 ust. 1 - every admissible cabinet's simple-majority vote:"));
            foreach (int c in ch.Admissible.OrderByDescending(c => ch.BaseScore[c]))
            {
                CoalitionFormation.CabinetEvaluation e = CoalitionFormation.Evaluate(ch, c);
                int against = CoalitionFormation.RedLinedMask(c, ch.N, ch.Lines, rules) & ~e.SupportMask;
                int a = CoalitionMath.Seats(seats, against);
                Console.WriteLine(F("      {0,-14} score {1,7:0.000}  for {2,3} ({3}{4})  against {5,3} ({6})  abstain {7,3}  {8}", Names(c), ch.BaseScore[c], e.SupportedSeats, Names(c),
                    e.SupportMask != 0 ? " + " + Names(e.SupportMask) : "", a, against == 0 ? "-" : Names(against), seats.Sum() - e.SupportedSeats - a, e.SupportedSeats > a ? "CARRIES" : "refused"));
            }
        }
        return new Outcome
        {
            Step = named.Carried ? "Art. 155 ust. 1" : "Art. 155 ust. 2", Formed = named.Carried, Named = named,
            Government = string.Join("+", named.Cabinet) + (named.Support.Count > 0 ? " with " + string.Join("+", named.Support) : string.Empty),
            Detail = named.Line() + F("; {0} cabinet(s) carry", named.Carrying),
        };
    }

    static int Chamber(DateTime day, Dictionary<string, int> held, bool verbose, List<RedLine> extra)
    {
        Console.WriteLine(F("CHAMBER {0:yyyy-MM-dd}: {1}; {2} members", day, string.Join(", ", held.Select(kv => kv.Key + " " + kv.Value)), held.Values.Sum()));
        Outcome o = Steps(day, held, extra, verbose);
        Console.WriteLine(F("   => {0}: {1} - {2}", o.Step, o.Government, o.Detail));
        if (o.Step == "Art. 155 ust. 2") { Console.WriteLine(F("   => the term shortened on the first sitting {0:yyyy-MM-dd} (the AI's day), elections on {1:yyyy-MM-dd}", day.AddDays(30), SejmFormation.ShortenedTermElectionDay(day.AddDays(30)))); }
        return 0;
    }

    static readonly Regex Traj = new Regex(@"TRAJ: wrote .*\((\d+) turns, seed (\d+)\)");
    static readonly Regex Held = new Regex(@"^CALENDAR: Poland held its (extra )?election on (\d{4}-\d{2}-\d{2}) as an AI country by \S+ - (.+?); the new chamber first sits on (\d{4}-\d{2}-\d{2})");
    static readonly Regex Installed = new Regex(@"^CALENDAR: Poland - installed (\S+) led by (\S+?)(?: with (\S+))? on (\d{4}-\d{2}-\d{2}) by .*?\(([^()]*\([^()]*\)[^()]*|[^()]*)\);");
    static readonly Regex None = new Regex(@"^CALENDAR: Poland - no government formed on (\d{4}-\d{2}-\d{2})");

    sealed class Election { public string Segment; public DateTime Day; public bool Extra; public Dictionary<string, int> Seats; public string Logged = "nothing logged"; public string LoggedStep = ""; public bool LoggedFormed; }

    static List<Election> Parse(string path)
    {
        string[] lines = File.ReadAllLines(path);
        var segmentOf = new string[lines.Length];
        string next = null;
        for (int i = lines.Length - 1; i >= 0; i--) { Match t = Traj.Match(lines[i]); if (t.Success) { next = "seed " + t.Groups[2].Value; } segmentOf[i] = next; }
        var list = new List<Election>();
        Election last = null;
        for (int i = 0; i < lines.Length; i++)
        {
            Match h = Held.Match(lines[i]);
            if (h.Success)
            {
                last = new Election { Segment = segmentOf[i], Day = DateTime.ParseExact(h.Groups[2].Value, "yyyy-MM-dd", CultureInfo.InvariantCulture), Extra = h.Groups[1].Success, Seats = ParseSeats(h.Groups[3].Value, ", ") };
                list.Add(last);
                continue;
            }
            if (last == null) { continue; }
            Match m = Installed.Match(lines[i]);
            if (m.Success && last.Logged == "nothing logged") { last.Logged = m.Groups[1].Value + (m.Groups[3].Success ? " with " + m.Groups[3].Value : string.Empty); last.LoggedStep = m.Groups[5].Value; last.LoggedFormed = true; continue; }
            if (None.IsMatch(lines[i]) && last.Logged == "nothing logged") { last.Logged = "NO GOVERNMENT"; }
        }
        return list;
    }

    static int Dump(string path, bool konfOnly)
    {
        List<Election> log = Parse(path);
        Console.WriteLine(F("{0} {1}: {2} Polish in-run elections", konfOnly ? "KONF" : "DUMP", Path.GetFileName(path), log.Count));
        int agree = 0, reach155 = 0, carried155 = 0, shortened = 0, konf = 0, konfMoves = 0;
        var moved = new Dictionary<string, int>();
        foreach (Election e in log)
        {
            Outcome here = Steps(e.Day, e.Seats, null, false);
            bool same = here.Formed == e.LoggedFormed && (!here.Formed || Same(here.Government, e.Logged));
            if (same || (!e.LoggedFormed && here.Step.StartsWith("Art. 155", StringComparison.Ordinal))) { agree++; }
            string seats = string.Join(" ", e.Seats.OrderByDescending(kv => kv.Value).Select(kv => kv.Key + " " + kv.Value));
            if (konfOnly)
            {
                if (e.Logged != "Konf with PiS" && !e.Logged.StartsWith("Konf with PiS+", StringComparison.Ordinal)) { continue; }
                konf++;
                IReadOnlyList<PoliticalParty> parties = PartySystems.For(CountryId.Poland);
                Outcome cf = Steps(e.Day, e.Seats, Counterfactual(parties), false);
                if (!Same(cf.Government, e.Logged) || !cf.Formed) { konfMoves++; }
                string key = cf.Step + ": " + (cf.Formed ? cf.Government : "SHORTENED TERM (" + cf.Government + " refused)");
                moved[key] = moved.TryGetValue(key, out int n) ? n + 1 : 1;
                Console.WriteLine(F("   {0} {1:yyyy-MM-dd} ({2}): logged {3}; as read: {4} {5}; PiS -> Konf support-blocking: {6} {7} - {8}", e.Segment, e.Day, seats, e.Logged, here.Step, here.Government,
                    cf.Step, cf.Formed ? cf.Government : "NO GOVERNMENT, the term shortened (" + cf.Government + " refused)", cf.Detail));
                continue;
            }
            if (here.Step.StartsWith("Art. 155", StringComparison.Ordinal))
            {
                reach155++;
                if (here.Formed) { carried155++; } else { shortened++; }
                DateTime sitting = e.Day.AddDays(e.Extra ? SejmFormation.ShortenedTermFirstSittingAfterDays : 30);
                Console.WriteLine(F("   {0} {1:yyyy-MM-dd}{2} ({3}): logged {4}; HERE {5}: {6} - {7}{8}", e.Segment, e.Day, e.Extra ? " (an extra election)" : "", seats, e.Logged, here.Step, here.Government, here.Detail,
                    here.Formed ? "" : F("; the term shortened on {0:yyyy-MM-dd}, elections on {1:yyyy-MM-dd}", sitting, SejmFormation.ShortenedTermElectionDay(sitting))));
            }
            else if (!same) { Console.WriteLine(F("   {0} {1:yyyy-MM-dd} ({2}): logged {3}; HERE {4}: {5} - DIFFERS", e.Segment, e.Day, seats, e.Logged, here.Step, here.Government)); }
        }
        if (konfOnly)
        {
            Console.WriteLine(F("KONF: {0} 'Konf with PiS' installs; read as the declarations say, every one forms again as logged at Art. 154; under PiS -> Konf support-blocking {1} move:", konf, konfMoves));
            foreach (KeyValuePair<string, int> kv in moved.OrderByDescending(kv => kv.Value)) { Console.WriteLine(F("      {0,3} x {1}", kv.Value, kv.Key)); }
            return 0;
        }
        Console.WriteLine(F("DUMP: {0} of {1} elections agree with the log at Art. 154 (or the log formed none where Art. 155 now acts); {2} reach Art. 155 - {3} carried by a simple majority, {4} end in a shortened term",
            agree, log.Count, reach155, carried155, shortened));
        return 0;
    }

    static bool Same(string a, string b) => Norm(a) == Norm(b);
    static string Norm(string g)
    {
        string[] halves = g.Split(new[] { " with " }, StringSplitOptions.None);
        string Sort(string s) => string.Join("+", s.Split('+').OrderBy(x => x, StringComparer.Ordinal));
        return Sort(halves[0]) + (halves.Length > 1 ? " with " + Sort(halves[1]) : "");
    }

    /// <summary>`strip <day> "<seats>"`: the formation with every declared line as the game reads it, and with the declared lines' support halves cut (each
    /// support-blocking declared line re-cast as a cabinet line) - which supporters the declared support lines are what keeps out.</summary>
    static int Strip(DateTime day, Dictionary<string, int> held)
    {
        IReadOnlyList<PoliticalParty> parties = PartySystems.For(CountryId.Poland);
        int[] seats = parties.Select(p => held.TryGetValue(p.Abbrev, out int s) ? s : 0).ToArray();
        DeclarationReading reading = DeclarationReading.OfElection(CountryId.Poland, day);
        List<RedLine> lines = reading.Lines(CountryId.Poland, parties);
        List<RedLine> cut = lines.Select(l => l.Kind == RedLineKind.Declared && l.BlocksSupport ? new RedLine(l.A, l.B, l.Kind, false, l.Basis) : l).ToList();
        string Names(int mask) => string.Join("+", Enumerable.Range(0, parties.Count).Where(p => (mask & (1 << p)) != 0).Select(p => parties[p].Abbrev));
        foreach ((string name, List<RedLine> ls) in new[] { ("as read", lines), ("support halves cut", cut) })
        {
            CoalitionResult r = CoalitionFormation.Form(seats, GovernmentFormation.Compatibility(parties), ls, false, reading.Platforms(CountryId.Poland, parties), ChamberRules.JointMasks(CountryId.Poland, parties, seats));
            Console.WriteLine(F("   {0}: {1}", name, r.Outcome == CoalitionOutcomeKind.NewElection ? "none" : Names(r.Government.Cabinet) + (r.Government.Support != 0 ? " with " + Names(r.Government.Support) : "")));
        }
        return 0;
    }

    static int Dates()
    {
        DateTime ordered2007 = new DateTime(2007, 9, 7), held2007 = new DateTime(2007, 10, 21);
        DateTime day = SejmFormation.ShortenedTermElectionDay(ordered2007);
        Console.WriteLine(F("DATES: ordered {0:yyyy-MM-dd} ({1}) -> {2:yyyy-MM-dd} ({3}); the record's {4:yyyy-MM-dd} ({5}): {6}", ordered2007, ordered2007.DayOfWeek, day, day.DayOfWeek, held2007, held2007.DayOfWeek,
            day == held2007 ? "AGREES" : "DIFFERS"));
        return day == held2007 ? 0 : 1;
    }
}
