using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using PoliSim.Data;
using PoliSim.Elections;

// K300 prereg (Reviews/2026-10-29_k300_prereg.md) acceptance (iii), read off two TrajectoryBaselineDump logs out of Unity:
// `run.sh <wt> dump-check <branch log> <main log>`. Per seed (and horizon), per country, per election: the government installed (cabinet, head,
// support). (b) is K302's, chamber-matched: an election left without a government fails only where main's formation forms one on that same
// chamber (the first wording, by polling day, is reported beside it). (a) - as the prereg's AMENDMENT 1 words it - no installed government is a lone-party minority (one party, or one parliamentary group,
// under the chamber's majority) carried by a supporter with which any red line stands (declared or derived, cabinet or support-blocking, either
// direction) in the reading of the election that seated the chamber, of a kind (cabinet with that supporter) main's log never installs for that seed
// and country, nor more of them than main's log installs; main's own are reported as pre-existing. A mid-term install is read on that election's
// lines too, and is marked mid-term; (b) no country's elections newly end with no
// government installed against main's log (matched on polling day; where the days diverge, counted). Exit 0 on PASS, 1 on FAIL.
// `replay <main log> <w>` predicts, out of Unity, which of main's in-run chambers form differently at w (see Replay).
static class DumpCheck
{
    static string F(string f, params object[] a) => string.Format(CultureInfo.InvariantCulture, f, a);

    sealed class Install { public DateTime Date; public List<string> Cabinet, Support; public string Head; public bool MidTerm; }
    sealed class Election { public string Segment; public CountryId Country; public DateTime Date; public Dictionary<string, int> Seats; public List<Install> Installs = new List<Install>(); public bool Start; }

    static readonly Regex Traj = new Regex(@"TRAJ: wrote .*\((\d+) turns, seed (\d+)\)");
    static readonly Regex Held = new Regex(@"^CALENDAR: (\w+) held its election on (\d{4}-\d{2}-\d{2}) as an AI country by \S+ - (.+?); the new chamber");
    static readonly Regex Installed = new Regex(@"^CALENDAR: (\w+) - installed (\S+) led by (\S+?)(?: with (\S+))? on (\d{4}-\d{2}-\d{2}) by ");

    static List<Election> Parse(string path) => Parse(path, out List<string> _);

    /// <summary>The log's elections, and its segments - one per `TRAJ: wrote` line (a seed and horizon run to its end). The review's M4: a log with none
    /// is empty or truncated, and anything after its last such line belongs to a run that never finished.</summary>
    static List<Election> Parse(string path, out List<string> segments)
    {
        string[] lines = File.ReadAllLines(path);
        var segmentOf = new string[lines.Length];
        segments = new List<string>();
        string next = null;
        for (int i = lines.Length - 1; i >= 0; i--)
        {
            Match t = Traj.Match(lines[i]);
            if (t.Success) { next = "seed " + t.Groups[2].Value + " (" + t.Groups[1].Value + " turns)"; segments.Insert(0, next); }
            segmentOf[i] = next;
        }
        var elections = new List<Election>();
        var latest = new Dictionary<(string, CountryId), Election>();
        for (int i = 0; i < lines.Length; i++)
        {
            if (segmentOf[i] == null) { continue; }
            Match h = Held.Match(lines[i]);
            if (h.Success && Enum.TryParse(h.Groups[1].Value, out CountryId hc))
            {
                var seats = new Dictionary<string, int>();
                foreach (string part in h.Groups[3].Value.Split(new[] { ", " }, StringSplitOptions.RemoveEmptyEntries))
                {
                    int sp = part.LastIndexOf(' ');
                    if (sp > 0 && int.TryParse(part.Substring(sp + 1), NumberStyles.Integer, CultureInfo.InvariantCulture, out int n)) { seats[part.Substring(0, sp)] = n; }
                }
                var e = new Election { Segment = segmentOf[i], Country = hc, Date = DateTime.ParseExact(h.Groups[2].Value, "yyyy-MM-dd", CultureInfo.InvariantCulture), Seats = seats };
                elections.Add(e);
                latest[(segmentOf[i], hc)] = e;
                continue;
            }
            Match m = Installed.Match(lines[i]);
            if (m.Success && Enum.TryParse(m.Groups[1].Value, out CountryId ic))
            {
                if (!latest.TryGetValue((segmentOf[i], ic), out Election e))
                {
                    e = new Election { Segment = segmentOf[i], Country = ic, Date = DateTime.MinValue, Seats = PartySystems.InitialSeats(ic), Start = true };
                    elections.Add(e);
                    latest[(segmentOf[i], ic)] = e;
                }
                e.Installs.Add(new Install
                {
                    Date = DateTime.ParseExact(m.Groups[5].Value, "yyyy-MM-dd", CultureInfo.InvariantCulture),
                    Cabinet = m.Groups[2].Value.Split('+').ToList(), Head = m.Groups[3].Value,
                    Support = m.Groups[4].Success ? m.Groups[4].Value.Split('+').ToList() : new List<string>(),
                    MidTerm = e.Installs.Count > 0 || e.Start,
                });
            }
        }
        return elections;
    }

    static int IndexOf(IReadOnlyList<PoliticalParty> parties, string key)
    {
        for (int p = 0; p < parties.Count; p++) { if (parties[p].Abbrev == key) { return p; } }
        for (int p = 0; p < parties.Count; p++) { if (parties[p].ShortName == key) { return p; } }
        return -1;
    }

    /// <summary>(a): the lines that make this install a lone-party minority carried by a party it is red-lined against; empty where it is not one.</summary>
    static List<string> Violations(Election e, Install g, out bool lone, out bool minority)
    {
        var found = new List<string>();
        IReadOnlyList<PoliticalParty> parties = PartySystems.For(e.Country);
        int total = e.Seats.Values.Sum(), majority = total / 2 + 1;
        int cabSeats = g.Cabinet.Sum(k => e.Seats.TryGetValue(k, out int s) ? s : 0);
        var groups = ChamberRules.JointGroups(e.Country);
        lone = g.Cabinet.Count == 1 || (g.Cabinet.Count == 2 && groups.Any(j => g.Cabinet.Contains(j.A) && g.Cabinet.Contains(j.B)));
        minority = cabSeats < majority;
        if (!lone || !minority || g.Support.Count == 0) { return found; }
        DeclarationReading reading = e.Start ? DeclarationReading.OfVintage(ElectionVintage.Seated) : DeclarationReading.OfElection(e.Country, e.Date);
        List<RedLine> lines = reading.Lines(e.Country, parties);
        foreach (string s in g.Support)
        {
            int si = IndexOf(parties, s);
            foreach (string c in g.Cabinet)
            {
                int ci = IndexOf(parties, c);
                foreach (RedLine l in lines)
                {
                    if ((l.A == ci && l.B == si) || (l.A == si && l.B == ci))
                    {
                        found.Add(F("{0}-{1} ({2}{3}{4}): {5}", parties[l.A].Abbrev, parties[l.B].Abbrev, l.Kind, l.BlocksSupport ? ", support-blocking" : ", cabinet", l.OneWay ? ", one way" : "",
                            (l.Basis ?? string.Empty).Length > 90 ? l.Basis.Substring(0, 90) + "..." : l.Basis));
                    }
                }
            }
        }
        return found;
    }

    /// <summary>Prereg amendment (a): the kinds of flagged install (cabinet with red-lined supporter) and their count, per seed and country.</summary>
    static Dictionary<(string, CountryId), (int Count, HashSet<string> Kinds)> Flags(List<Election> log)
    {
        var flags = new Dictionary<(string, CountryId), (int Count, HashSet<string> Kinds)>();
        foreach (Election e in log)
        {
            foreach (Install g in e.Installs)
            {
                if (Violations(e, g, out bool _, out bool _).Count == 0) { continue; }
                var key = (e.Segment, e.Country);
                if (!flags.TryGetValue(key, out var f)) { f = (0, new HashSet<string>()); }
                foreach (string k in KindsOf(e, g)) { f.Kinds.Add(k); }
                flags[key] = (f.Count + 1, f.Kinds);
            }
        }
        return flags;
    }

    static IEnumerable<string> KindsOf(Election e, Install g)
    {
        IReadOnlyList<PoliticalParty> parties = PartySystems.For(e.Country);
        List<RedLine> lines = (e.Start ? DeclarationReading.OfVintage(ElectionVintage.Seated) : DeclarationReading.OfElection(e.Country, e.Date)).Lines(e.Country, parties);
        foreach (string s in g.Support)
        {
            int si = IndexOf(parties, s);
            if (g.Cabinet.Any(c => { int ci = IndexOf(parties, c); return lines.Any(l => (l.A == ci && l.B == si) || (l.A == si && l.B == ci)); })) { yield return string.Join("+", g.Cabinet) + " with " + s; }
        }
    }

    /// <summary>K302: main's formation (w = 0) on an election's own chamber - the government it forms, or null where it forms none.</summary>
    static string MainFormsOn(Election e)
    {
        IReadOnlyList<PoliticalParty> parties = PartySystems.For(e.Country);
        int[] seats = parties.Select(p => e.Seats.TryGetValue(p.Abbrev, out int s) ? s : 0).ToArray();
        DeclarationReading reading = e.Start ? DeclarationReading.OfVintage(ElectionVintage.Seated) : DeclarationReading.OfElection(e.Country, e.Date);
        Variants.Result r = Variants.FormRaw(seats, GovernmentFormation.Compatibility(parties), reading.Lines(e.Country, parties), ChamberRules.UsesNegativeParliamentarism(e.Country),
            reading.Platforms(e.Country, parties), ChamberRules.JointMasks(e.Country, parties, seats), 0.0, 0.0);
        if (r.Viable.Count == 0) { return null; }
        string Names(int mask) => string.Join("+", Enumerable.Range(0, parties.Count).Where(p => (mask & (1 << p)) != 0).Select(p => parties[p].Abbrev));
        return Names(r.Gov.Cabinet) + (r.Gov.Support != 0 ? " with " + Names(r.Gov.Support) : "");
    }

    static string Gov(Install g) => string.Join("+", g.Cabinet) + " led by " + g.Head + (g.Support.Count > 0 ? " with " + string.Join("+", g.Support) : string.Empty);

    /// <summary>`replay <main log> <w>`: a PREDICTION, not the acceptance - every in-run chamber main's dump elected, formed out of Unity by main's formation
    /// and at the majority bonus w (Variants.FormRaw, the election's polling-day reading, the country's rule). It names the first elections whose
    /// formation answer changes; the in-engine run diverges after the first change (a new government moves the AI's book and the next vote), and the
    /// install goes through each country's procedure (the Speaker's round, the Bundestag's ballots, the Sejm's steps, Italy's mandates), whose agreement
    /// with the formation's answer is printed beside it.</summary>
    public static int Replay(string[] args)
    {
        List<Election> log = Parse(args[1]);
        double w = double.Parse(args[2], CultureInfo.InvariantCulture);
        int formed = 0, agree = 0, changes = 0;
        foreach (Election e in log.Where(x => !x.Start))
        {
            IReadOnlyList<PoliticalParty> parties = PartySystems.For(e.Country);
            int[] seats = parties.Select(p => e.Seats.TryGetValue(p.Abbrev, out int s) ? s : 0).ToArray();
            DeclarationReading reading = DeclarationReading.OfElection(e.Country, e.Date);
            List<RedLine> lines = reading.Lines(e.Country, parties);
            List<InOrAgainst> rules = reading.Platforms(e.Country, parties);
            bool negative = ChamberRules.UsesNegativeParliamentarism(e.Country);
            int[] joint = ChamberRules.JointMasks(e.Country, parties, seats);
            double[,] compat = GovernmentFormation.Compatibility(parties);
            Variants.Result a = Variants.FormRaw(seats, compat, lines, negative, rules, joint, 0.0, 0.0), b = Variants.FormRaw(seats, compat, lines, negative, rules, joint, 0.0, w);
            string Names(int mask) => mask == 0 ? "-" : string.Join("+", Enumerable.Range(0, parties.Count).Where(p => (mask & (1 << p)) != 0).Select(p => parties[p].Abbrev));
            string Sig(Variants.Result r) => r.Viable.Count == 0 ? "NO GOVERNMENT" : Names(r.Gov.Cabinet) + (r.Gov.Support != 0 ? " with " + Names(r.Gov.Support) : "");
            formed++;
            bool same = e.Installs.Count > 0 && a.Viable.Count > 0 && new HashSet<string>(e.Installs[0].Cabinet).SetEquals(Names(a.Gov.Cabinet).Split('+'));
            if (same) { agree++; }
            if (Sig(a) == Sig(b)) { continue; }
            changes++;
            Console.WriteLine(F("   {0} {1} {2:yyyy-MM-dd}: main's formation {3} -> at w = {4:0.00} {5} (main's log installed {6}){7}", e.Segment, e.Country, e.Date, Sig(a), w, Sig(b),
                e.Installs.Count > 0 ? Gov(e.Installs[0]) : "none", b.Viable.Count > 0 ? F(" scores {0:0.000} -> {1:0.000}", a.Viable.Count > 0 ? a.Gov.Score : double.NaN, b.Gov.Score) : ""));
        }
        Console.WriteLine(F("REPLAY (a prediction): {0} in-run elections; main's formation names main's installed cabinet in {1} of them; at w = {2:0.00} the formation's answer changes in {3}", formed, agree, w, changes));
        return 0;
    }

    /// <summary>
    /// `sejm-trace <polling day> "<seats>" [w]`: one Polish in-run chamber traced through the Sejm's steps as `SimulationManager.ProjectSejm` takes them for a
    /// Sejm no record dates (an in-run election): Art. 154 ust. 1, the President designates the formation's projection (`GovernmentFormation.ViewOf` on the
    /// election's reading - the formation's top government; none where the formation forms none); ust. 2, the Sejm's confidence, the cabinet's and its
    /// supporters' seats for (`SejmFormation.Carries`: more than half of all members); ust. 3, the Sejm elects the same projection on the same rule; else no
    /// government (Art. 155 not modelled). Under main's formation (w = 0) and at w, with every admissible cabinet's own seats, its supporters, its vote and
    /// whether the bonus reaches it.
    /// </summary>
    public static int SejmTrace(string[] args)
    {
        DateTime day = DateTime.ParseExact(args[1], "yyyy-MM-dd", CultureInfo.InvariantCulture);
        var held = new Dictionary<string, int>();
        foreach (string part in args[2].Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
        {
            string p = part.Trim(); int sp = p.LastIndexOf(' ');
            held[p.Substring(0, sp)] = int.Parse(p.Substring(sp + 1), CultureInfo.InvariantCulture);
        }
        double w = args.Length > 3 ? double.Parse(args[3], CultureInfo.InvariantCulture) : 0.53;
        IReadOnlyList<PoliticalParty> parties = PartySystems.For(CountryId.Poland);
        int[] seats = parties.Select(p => held.TryGetValue(p.Abbrev, out int s) ? s : 0).ToArray();
        DeclarationReading reading = DeclarationReading.OfElection(CountryId.Poland, day);
        List<RedLine> lines = reading.Lines(CountryId.Poland, parties);
        List<InOrAgainst> rules = reading.Platforms(CountryId.Poland, parties);
        int[] joint = ChamberRules.JointMasks(CountryId.Poland, parties, seats);
        double[,] compat = GovernmentFormation.Compatibility(parties);
        bool negative = ChamberRules.UsesNegativeParliamentarism(CountryId.Poland);
        string Names(int mask) => mask == 0 ? "-" : string.Join("+", Enumerable.Range(0, parties.Count).Where(p => (mask & (1 << p)) != 0).Select(p => parties[p].Abbrev));
        int total = seats.Sum(), majority = total / 2 + 1;
        Console.WriteLine(F("SEJM-TRACE {0:yyyy-MM-dd}: {1}; {2} members, a majority {3}; {4}", day, string.Join(", ", held.Select(kv => kv.Key + " " + kv.Value)), total, majority, reading));
        CoalitionFormation.Chamber ch = CoalitionFormation.Prepare(seats, compat, lines, negative, rules, joint);
        Console.WriteLine(F("   admissible cabinets: {0}; blocked majorities: {1}", ch.Admissible.Count, string.Join(", ", ch.Blocked.Select(b => Names(b.Cabinet) + " (" + parties[b.Line.A].Abbrev + "-" + parties[b.Line.B].Abbrev + ")"))));
        foreach (int c in ch.Admissible)
        {
            int sup = CoalitionFormation.Evaluate(ch, c).SupportMask, cab = CoalitionMath.Seats(seats, c), carried = cab + CoalitionMath.Seats(seats, sup);
            Console.WriteLine(F("      {0,-16} {1,3} own{2} + support {3,-12} {4,3} = {5,3}: {6}", Names(c), cab, cab >= majority ? " (a majority of its own: the bonus reaches it)" : "", Names(sup), CoalitionMath.Seats(seats, sup), carried,
                carried >= majority ? "wins its investiture" : "short of the majority"));
        }
        foreach (double wv in new[] { 0.0, w })
        {
            Variants.Result r = Variants.FormRaw(seats, compat, lines, negative, rules, joint, 0.0, wv);
            string who = r.Viable.Count == 0 ? "none (the formation forms no government)" : Names(r.Gov.Cabinet) + (r.Gov.Support != 0 ? " with " + Names(r.Gov.Support) : "");
            Console.WriteLine(F("   w = {0:0.00}: viable after the rounds: {1}", wv, r.Viable.Count == 0 ? "NONE" : string.Join(" | ", r.Viable.Select(g => F("{0}{1} {2:0.000}", Names(g.Cabinet), g.Support != 0 ? " with " + Names(g.Support) : "", g.Score)))));
            Console.WriteLine(F("      Art. 154 ust. 1: the President designates the formation's projection - {0}", who));
            if (r.Viable.Count == 0)
            {
                Console.WriteLine("      ust. 1's fourteen days pass without an appointment; ust. 3: the Sejm elects the same projection - none; NO GOVERNMENT (Art. 155 not modelled)");
                continue;
            }
            int yes = CoalitionMath.Seats(seats, r.Gov.Cabinet | r.Gov.Support);
            Console.WriteLine(F("      ust. 2: the Sejm's confidence - {0} of {1} for: {2}", yes, total, yes * 2 > total ? "carried; the designee's cabinet governs" : "refused; ust. 3 puts the same projection, refused again: NO GOVERNMENT"));
        }
        return 0;
    }

    public static int Run(string[] args)
    {
        if (args.Length > 0 && args[0] == "replay") { return Replay(args); }
        if (args.Length > 0 && args[0] == "sejm-trace") { return SejmTrace(args); }
        if (args.Length < 3 || !File.Exists(args[1]) || !File.Exists(args[2])) { Console.WriteLine("dump-check <branch log> <main log> | replay <main log> <w> | sejm-trace <day> \"<seats>\" [w]"); return 2; }
        List<Election> branch = Parse(args[1], out List<string> branchSegments), main = Parse(args[2], out List<string> mainSegments);
        int violations = 0, preExisting = 0, newlyNone = 0, chamberNone = 0, countFails = 0, moved = 0;
        // the review's M4: both logs whole - one segment (seed, horizon) per finished run, the same set in each, and every country main's run elects in elected in the branch's too
        var incomplete = new List<string>();
        if (branchSegments.Count == 0) { incomplete.Add("the branch log holds no finished run (no `TRAJ: wrote` line): empty or truncated"); }
        if (mainSegments.Count == 0) { incomplete.Add("main's log holds no finished run (no `TRAJ: wrote` line): empty or truncated"); }
        foreach (string s in mainSegments.Except(branchSegments)) { incomplete.Add("main's " + s + " is missing from the branch log"); }
        foreach (string s in branchSegments.Except(mainSegments)) { incomplete.Add("the branch's " + s + " is missing from main's log"); }
        foreach (string s in mainSegments.Intersect(branchSegments))
        {
            foreach (CountryId c in main.Where(e => e.Segment == s && !e.Start).Select(e => e.Country).Distinct())
            {
                if (!branch.Any(e => e.Segment == s && e.Country == c && !e.Start)) { incomplete.Add(F("{0}: {1} elects in main's log and never in the branch's", s, c)); }
            }
        }
        foreach (string why in incomplete) { Console.WriteLine("   INCOMPLETE: " + why); }
        var mainFlags = Flags(main);
        var branchFlags = Flags(branch);
        Console.WriteLine(F("DUMP-CHECK (K300 prereg (iii)): {0} against {1}", Path.GetFileName(args[1]), Path.GetFileName(args[2])));
        foreach (string segment in branch.Select(e => e.Segment).Distinct())
        {
            Console.WriteLine("== " + segment);
            foreach (CountryId country in Enum.GetValues(typeof(CountryId)))
            {
                List<Election> mine = branch.Where(e => e.Segment == segment && e.Country == country).ToList();
                List<Election> theirs = main.Where(e => e.Segment == segment && e.Country == country).ToList();
                if (mine.Count == 0 && theirs.Count == 0)
                {
                    Console.WriteLine(F("   {0}: no government formed by this formation in the run (no AI parliamentary election with a formation is logged)", country));
                    continue;
                }
                int noneMine = mine.Count(e => !e.Start && e.Installs.Count == 0), noneTheirs = theirs.Count(e => !e.Start && e.Installs.Count == 0);
                Console.WriteLine(F("   {0}: {1} elections, {2} with no government installed (main: {3} elections, {4} with none)", country, mine.Count(e => !e.Start), noneMine, theirs.Count(e => !e.Start), noneTheirs));
                foreach (Election e in mine)
                {
                    Election twin = theirs.FirstOrDefault(t => t.Date == e.Date);
                    string seats = string.Join(" ", e.Seats.OrderByDescending(kv => kv.Value).Select(kv => kv.Key + " " + kv.Value));
                    string head = e.Start ? "the start's chamber (mid-term installs)" : F("{0:yyyy-MM-dd} ({1})", e.Date, seats);
                    if (e.Installs.Count == 0)
                    {
                        bool wasInstalled = twin != null && twin.Installs.Count > 0;
                        if (wasInstalled) { newlyNone++; }
                        // K302 (Elias): (b) is read CHAMBER-MATCHED - the bonus must never leave without a government a chamber main's formation would form on. Main's
                        // formation on THIS chamber (Variants.FormRaw at w = 0, the election's reading, the country's rule; Italy's Camera alone - the log carries no Senato)
                        string mainOnIt = MainFormsOn(e);
                        if (mainOnIt != null) { chamberNone++; }
                        Console.WriteLine(F("      {0}: NO GOVERNMENT INSTALLED{1}; {2}", head, wasInstalled ? " - newly by polling day (main installed " + Gov(twin.Installs[0]) + " on its own chamber)" : twin != null ? " (none in main either)" : "",
                            mainOnIt != null ? "(b) FAIL (K302, chamber-matched): main's formation forms " + mainOnIt + " on this chamber" : "(b) chamber-matched: main's formation forms none on this chamber either"));
                        continue;
                    }
                    var parts = new List<string>();
                    foreach (Install g in e.Installs)
                    {
                        List<string> v = Violations(e, g, out bool lone, out bool minority);
                        // the amendment (a): a flagged install fails where its kind is one main's log never installs for this seed and country; one main also installs is pre-existing
                        bool known = v.Count > 0 && mainFlags.TryGetValue((segment, country), out var mf) && KindsOf(e, g).All(k => mf.Kinds.Contains(k));
                        if (v.Count > 0) { if (known) { preExisting++; } else { violations++; } }
                        parts.Add(F("{0}{1} {2:yyyy-MM-dd}{3}", g.MidTerm ? "mid-term " : "", Gov(g), g.Date,
                            v.Count > 0 ? (known ? " [(a) PRE-EXISTING - main's log installs this kind too: " : " [(a) FAIL - NEW, main's log never installs this kind: ") + string.Join("; ", v) + "]" : lone && minority ? " [a lone-party minority, no line to its supporters]" : ""));
                    }
                    string vsMain = twin == null ? " (no election on this day in main)" : twin.Installs.Count == 0 ? " (main: no government)" : Gov(twin.Installs[0]) == Gov(e.Installs[0]) ? "" : " (main: " + Gov(twin.Installs[0]) + ")";
                    if (twin != null && twin.Installs.Count > 0 && Gov(twin.Installs[0]) != Gov(e.Installs[0])) { moved++; }
                    Console.WriteLine(F("      {0}: {1}{2}", head, string.Join(" | ", parts), vsMain));
                }
                var mineDays = new HashSet<DateTime>(mine.Where(e => !e.Start).Select(e => e.Date));
                var theirDays = new HashSet<DateTime>(theirs.Where(e => !e.Start).Select(e => e.Date));
                int flaggedMine = branchFlags.TryGetValue((segment, country), out var bf) ? bf.Count : 0, flaggedTheirs = mainFlags.TryGetValue((segment, country), out var tf) ? tf.Count : 0;
                if (flaggedMine > 0 || flaggedTheirs > 0) { Console.WriteLine(F("      (a) lone-party minorities carried across a red line: {0} (main: {1}){2}", flaggedMine, flaggedTheirs, flaggedMine > flaggedTheirs ? " - (a) FAIL by count" : "")); }
                if (flaggedMine > flaggedTheirs) { countFails++; }
                if (!mineDays.SetEquals(theirDays) && noneMine > noneTheirs) { Console.WriteLine(F("      (b) as worded, by count (information - K302 reads (b) chamber-matched): polling days diverge and {0} elections end with no government against main's {1}", noneMine, noneTheirs)); }
            }
        }
        bool pass = violations == 0 && chamberNone == 0 && countFails == 0 && incomplete.Count == 0;
        Console.WriteLine(F("DUMP-CHECK (iii): {0} - (a) {1} NEW install(s) a lone-party minority carried by a party it is red-lined against ({2} of a kind main's log installs too, reported); (b, K302 chamber-matched) {3} chamber(s) left without a government main's formation forms on ({7} newly by polling day, as first worded - information); {4} (a) count failure(s); {5} election(s) whose first government differs from main's; {6} completeness failure(s) (M4)",
            pass ? "PASS" : "FAIL", violations, preExisting, chamberNone, countFails, moved, incomplete.Count, newlyNone));
        return pass ? 0 : 1;
    }
}
