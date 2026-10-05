using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using PoliSim.Data;
using PoliSim.Elections;
using PoliSim.Simulation;
using UnityEngine;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// §776 (Elias's ruling E2: "source the parties' real 2023 declarations by read, dated, before PS-6"): POLAND'S 2023 DECLARATIONS HELD TO
    /// THEIR RECORD, AND WHAT THEY DO TO THE FORMATION. `DeclaredRedLines.PolandTimeline` against `ElectionsData/poland/coalition_declarations_2023.md`:
    /// <list type="bullet">
    /// <item>the record's timeline table (its §8) is the array, row for row - each declarer, other, shape, from and until, the until after the
    /// from, the row's shape words and its F2 mark - and every fact's two keys are Polish roster keys;</item>
    /// <item>every source tag a fact cites is a row of the record's register, and every file the register names is held under
    /// `raw/declarations_2023/` at its digest;</item>
    /// <item>on polling day, 15 October 2023, exactly the facts still open stand.</item>
    /// </list>
    /// Read again under Elias's rulings F1 ("keep X from power" is a red line - the declarer neither joins nor supports a cabinet that includes X), F2
    /// (a leader's words count quoted verbatim on the broadcaster's or news agency's own page, dated by that page; a date from the party's own record
    /// always wins) and F7 (the smaller doubts as built, with one acceptance test: "in a world that follows history, a Polish game's own 2023 election
    /// forms KO+TD+NL"): a fact is one-way and support-blocking exactly when its basis carries F1's mark; a fact marked
    /// `DeclaredRedLines.PolandSpokenWords` is first-tagged by a page the register marks (F2) - the pages the record dates an F2 fact by (each the
    /// broadcaster's own page, never a relay, a newspaper or a portal) - and every other fact by its declarer's own page; each fact's from is the first
    /// date its first tag's register cell gives.
    /// Then THE MEASUREMENT, on two chambers - the chamber of record (the 2023 count's seed seats, which no game forms: a Polish start seats the
    /// government of record) and the played count (a Polish game's own 2023 count, by `PollingDayDiagnostic`'s path) - each formed by the
    /// chamber's own investiture rule (`ChamberRules.UsesNegativeParliamentarism`) and the game's compatibility and groups, under each reading: the
    /// derived lines alone; the declarations of polling day as recorded (what the game reads); those without the PiS-KO pair (doubt 1 read as aims,
    /// as before F1); and every support half read cabinet-only. Then THE WIRING: the 2023 election's lines, by its vintage and by its own dated
    /// reading, are the recorded reading line for line; on the chamber of record that reading seats the record's majority with PiS outside (a
    /// backtest); on the played count the game's own entry - the government the night stores (`GovernmentFormation.ViewOf`) - forms R1's cabinet and
    /// support, and F7's acceptance holds: KO+TD+NL.
    /// </summary>
    public static class PolishDeclarationsDiagnostic
    {
        private static readonly DateTime PollingDay = new DateTime(2023, 10, 15);

        public static void Run()
        {
            CheckExit.ArmLogFold();
            var sb = new StringBuilder("=== PolishDeclarationsDiagnostic: Poland's 2023 declarations held to their record, and measured ===\n");
            int failures = 0;
            void Check(bool ok, string what) { if (!ok) { failures++; } sb.Append(ok ? "    ok        " : "    FAIL      ").Append(what).Append('\n'); }
            try
            {
                string poland = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "ElectionsData", "poland"));
                string record = File.ReadAllText(Path.Combine(poland, "coalition_declarations_2023.md"), Encoding.UTF8);
                IReadOnlyList<DeclaredRedLines.DatedFact> facts = DeclaredRedLines.PolandTimeline;

                // (a) the record's §8 is the array, row for row - a row "A → B, C" is one fact per other party, in that order
                var rows = new List<(string Party, string Other, string Shape, bool Blocks, bool OneWay, DateTime From, DateTime Until, string Basis)>();
                foreach (Match m in Regex.Matches(record, @"^\| [0-9–-]+ \| (\S+) → ([^|]+?) \| ([^|]+?) \| (true|false) \| (true|false) \| (\d{4}-\d{2}-\d{2}) \| (Open|\d{4}-\d{2}-\d{2}) \| ([^|\n]+?) \|", RegexOptions.Multiline))
                {
                    foreach (string other in m.Groups[2].Value.Split(new[] { "," }, StringSplitOptions.RemoveEmptyEntries))
                    {
                        rows.Add((m.Groups[1].Value, other.Trim(), m.Groups[3].Value, m.Groups[4].Value == "true", m.Groups[5].Value == "true", Day(m.Groups[6].Value),
                            m.Groups[7].Value == "Open" ? DateTime.MaxValue : Day(m.Groups[7].Value), m.Groups[8].Value));
                    }
                }
                bool sameTable = rows.Count == facts.Count;
                for (int i = 0; sameTable && i < rows.Count; i++)
                {
                    DeclaredRedLines.DatedFact f = facts[i];
                    sameTable = f.Kind == DeclaredRedLines.FactKind.PairLine && f.Party == rows[i].Party && f.Other == rows[i].Other && f.BlocksSupport == rows[i].Blocks
                        && f.OneWay == rows[i].OneWay && f.From == rows[i].From && f.Until == rows[i].Until && f.Candidate == null && rows[i].From < rows[i].Until
                        // the review's finding: the row's words held too - "one-way, support-blocking (F1 ..." exactly for the one-way support-blocking facts,
                        // "cabinet" exactly for the cabinet ones, and "- F2" in the basis cell exactly where the fact carries F2's mark
                        && rows[i].Shape.StartsWith("one-way, support-blocking (F1", StringComparison.Ordinal) == (f.BlocksSupport && f.OneWay)
                        && (rows[i].Shape == "cabinet") == (!f.BlocksSupport && !f.OneWay)
                        && rows[i].Basis.Contains("- F2") == f.Basis.Contains(DeclaredRedLines.PolandSpokenWords)
                        && Regex.Match(rows[i].Basis, @"\[[A-Z]+-[A-Z]\d+\]").Value == Regex.Match(f.Basis, @"\[[A-Z]+-[A-Z]\d+\]").Value;   // the row names the page that dates the fact
                }
                Check(sameTable, F("the record's timeline table is DeclaredRedLines.PolandTimeline, row for row, its shape words, F2 marks and first tags included ({0} facts in the array, {1} read from the table)", facts.Count, rows.Count));

                // (b) every tag a fact cites is a register row; every fact declared, sourced to the record
                var register = new HashSet<string>(Regex.Matches(record, @"^\| \[([A-Z]+-[A-Z]+\d+)\] \|", RegexOptions.Multiline).Cast<Match>().Select(m => m.Groups[1].Value));
                var unregistered = new List<string>();
                foreach (DeclaredRedLines.DatedFact f in facts)
                {
                    foreach (Match t in Regex.Matches(f.Basis, @"\[([A-Z]+-[A-Z]+\d+)\]")) { if (!register.Contains(t.Groups[1].Value)) { unregistered.Add(t.Groups[1].Value); } }
                }
                bool cited = facts.All(f => f.Basis.StartsWith("DECLARED", StringComparison.Ordinal) && f.Basis.EndsWith(DeclaredRedLines.PolandSource, StringComparison.Ordinal) && Regex.IsMatch(f.Basis, @"\[[A-Z]+-[A-Z]+\d+\]"));
                Check(cited && unregistered.Count == 0, F("every fact is DECLARED, cites a source tag and the record, and every tag is a register row ({0} rows){1}",
                    register.Count, unregistered.Count > 0 ? "; NOT in the register: " + string.Join(", ", unregistered.Distinct()) : string.Empty));
                // F2 (the review's finding: the check must see F2's distinction, not a tag's letter): the first tag a fact cites dates it. A fact carrying F2's
                // mark is first-tagged by a page the register marks "(F2)" - the pages the record dates an F2 fact by (each the broadcaster's own page,
                // never a relay, a newspaper or a portal); every other fact by its declarer's own page ([X-Pn], X the declarer's key); and the fact's from is
                // the FIRST date its first tag's register cell gives (yyyy-MM-dd, dd.MM.yyyy, or the Polish day, month and year) - a dateModified or a second
                // stamp after it dates nothing (the second review's finding: a substring match let "13 lipca" pass for the 3rd)
                var pageDate = new Dictionary<string, string>();
                var f2Pages = new HashSet<string>();
                foreach (Match m in Regex.Matches(record, @"^\| \[([A-Z]+-[A-Z]+\d+)\] \|([^|\n]*)\|([^|\n]*)\|([^|\n]*)\|", RegexOptions.Multiline))
                {
                    pageDate[m.Groups[1].Value] = m.Groups[4].Value;
                    if (m.Groups[3].Value.Contains("(F2)")) { f2Pages.Add(m.Groups[1].Value); }
                }
                string[] months = { "sty", "lut", "mar", "kwi", "maj", "cze", "lip", "sie", "wrz", "paź", "lis", "gru" };
                bool DatedBy(DateTime from, string cell)
                {
                    if (cell == null) { return false; }
                    Match d = Regex.Match(cell, @"(?<!\d)(?:(\d{4})-(\d{2})-(\d{2})|(\d{2})\.(\d{2})\.(\d{4})|(\d{1,2}) (sty|lut|mar|kwi|maj|cze|lip|sie|wrz|paź|lis|gru)\p{L}*,? (\d{4}))");
                    if (!d.Success) { return false; }
                    int G(int g) => int.Parse(d.Groups[g].Value, CultureInfo.InvariantCulture);
                    DateTime day = d.Groups[1].Success ? new DateTime(G(1), G(2), G(3)) : d.Groups[4].Success ? new DateTime(G(6), G(5), G(4))
                        : new DateTime(G(9), Array.IndexOf(months, d.Groups[8].Value) + 1, G(7));
                    return day == from;
                }
                var misdated = facts.Where(f =>
                {
                    Match first = Regex.Match(f.Basis, @"\[(([A-Z]+)-([A-Z])\d+)\]");
                    if (!first.Success) { return true; }
                    string tag = first.Groups[1].Value;
                    bool page = f.Basis.Contains(DeclaredRedLines.PolandSpokenWords) ? f2Pages.Contains(tag) : first.Groups[3].Value == "P" && first.Groups[2].Value == f.Party.ToUpperInvariant();
                    return !page || !DatedBy(f.From, pageDate.TryGetValue(tag, out string cell) ? cell : null);
                }).Select(Line).ToList();
                Check(f2Pages.Count > 0 && f2Pages.All(t => Regex.IsMatch(t, @"^[A-Z]+-I\d+$")) && misdated.Count == 0,
                    F("F2: a fact carrying F2's mark is first-tagged by a page the register marks (F2) ({0} such pages, {1} facts); every other fact by its declarer's own page; each fact's from is the first date of its first tag's register cell{2}",
                        f2Pages.Count, facts.Count(f => f.Basis.Contains(DeclaredRedLines.PolandSpokenWords)), misdated.Count > 0 ? "; NOT: " + string.Join("; ", misdated) : string.Empty));
                // F1: a fact is one-way and support-blocking exactly when its basis carries F1's mark (the review's finding: the formation checks cannot tell
                // every support half apart - the derived PiS-NL line masks NL's on every Polish chamber)
                var misshaped = facts.Where(f => f.Basis.StartsWith("DECLARED (F1", StringComparison.Ordinal) != (f.BlocksSupport && f.OneWay)).Select(Line).ToList();
                Check(misshaped.Count == 0, F("F1: a fact is one-way and support-blocking exactly when its basis carries F1's mark{0}", misshaped.Count > 0 ? "; NOT: " + string.Join("; ", misshaped) : string.Empty));

                // (c) every register file held in tree at its digest
                string raw = Path.Combine(poland, "raw", "declarations_2023");
                int held = 0;
                var bad = new List<string>();
                foreach (Match m in Regex.Matches(record, @"^\| \[[A-Z]+-[A-Z]+\d+\] \|[^\n]*", RegexOptions.Multiline))
                {
                    string[] cells = m.Value.Split('|');
                    List<string> files = cells.Length > 6 ? Regex.Matches(cells[5], "`([^`]+)`").Cast<Match>().Select(x => x.Groups[1].Value).ToList() : new List<string>();
                    List<string> digests = cells.Length > 6 ? Regex.Matches(cells[6], "`([0-9a-f]{64})`").Cast<Match>().Select(x => x.Groups[1].Value).ToList() : new List<string>();
                    if (files.Count == 0 || files.Count != digests.Count) { bad.Add(cells.Length > 1 ? cells[1].Trim() : m.Value); continue; }
                    for (int i = 0; i < files.Count; i++)
                    {
                        string path = Path.Combine(raw, files[i].Replace('/', Path.DirectorySeparatorChar));
                        if (File.Exists(path) && Sha256(path) == digests[i]) { held++; } else { bad.Add(files[i]); }
                    }
                }
                Check(held > 0 && bad.Count == 0, F("every file the register names is held under raw/declarations_2023 at its digest ({0} held){1}", held, bad.Count > 0 ? "; NOT: " + string.Join(", ", bad) : string.Empty));

                // (d) polling day: exactly the open facts stand
                List<DeclaredRedLines.DatedFact> standing = facts.Where(f => f.StandsOn(PollingDay)).ToList();
                bool openStand = standing.Count > 0 && standing.All(f => f.Until == DateTime.MaxValue) && facts.Where(f => f.Until == DateTime.MaxValue).All(f => f.StandsOn(PollingDay));
                Check(openStand, F("on polling day, {0:yyyy-MM-dd}, exactly the facts still open stand: {1}", PollingDay, string.Join("; ", standing.Select(Line))));

                IReadOnlyList<PoliticalParty> parties;
                using (SimulationManager.EpochScope()) { parties = PartySystems.For(CountryId.Poland); }
                int Index(string abbrev) { for (int p = 0; p < parties.Count; p++) { if (parties[p].Abbrev == abbrev) { return p; } } return -1; }
                string Names(int mask) => mask == 0 ? "none" : string.Join("+", Enumerable.Range(0, parties.Count).Where(p => (mask & (1 << p)) != 0).Select(p => parties[p].Abbrev));
                int Mask(params string[] abbrevs) => abbrevs.Aggregate(0, (acc, a) => Index(a) >= 0 ? acc | (1 << Index(a)) : acc);

                // (a2) the review's finding 15: every fact's two keys are Polish roster keys - a fact keyed outside the roster would vanish from both sides
                // of (f)'s comparison and pass it vacuously
                List<string> unkeyed = facts.SelectMany(f => new[] { f.Party, f.Other }).Where(k => Index(k) < 0).Distinct().ToList();
                Check(unkeyed.Count == 0, F("every fact's declarer and other is a key of the Polish roster ({0}){1}", string.Join(", ", parties.Select(p => p.Abbrev)),
                    unkeyed.Count > 0 ? "; NOT: " + string.Join(", ", unkeyed) : string.Empty));

                // the readings - each the derived lines plus the declarations standing on polling day, read one way or another
                var lrGen = parties.Select(p => (double)p.LrGen).ToArray();
                var galtan = parties.Select(p => (double)p.Galtan).ToArray();
                List<RedLine> Derived() => DerivedRedLines.From(lrGen, galtan);
                List<RedLine> WithDeclarations(bool withoutPisKo = false, bool cabinetOnly = false)
                {
                    List<RedLine> lines = Derived();
                    foreach (DeclaredRedLines.DatedFact f in standing)
                    {
                        int a = Index(f.Party), b = Index(f.Other);
                        if (a < 0 || b < 0) { continue; }
                        if (withoutPisKo && ((f.Party == "PiS" && f.Other == "KO") || (f.Party == "KO" && f.Other == "PiS"))) { continue; }
                        bool blocks = !cabinetOnly && f.BlocksSupport;
                        lines.Add(new RedLine(a, b, RedLineKind.Declared, blocks, f.Basis, blocks && f.OneWay));
                    }
                    return lines;
                }
                // F1 ruled doubt 1: a pledge to keep a party from power is a line. R2 reads the PiS-KO pair as the record read it before F1 (aims, no line),
                // so the measurement shows what that pair changes, F1's other lines standing; R3 reads every support half cabinet-only, so it shows what the
                // shape adds
                var readings = new List<(string Name, List<RedLine> Lines)>
                {
                    ("R0 the derived lines alone", Derived()),
                    ("R1 the declarations of polling day, as recorded (F1, F2) - the game's", WithDeclarations()),
                    ("R2 R1 without the PiS-KO pair (doubt 1 read as aims, as before F1)", WithDeclarations(withoutPisKo: true)),
                    ("R3 R1 with every support half read cabinet-only", WithDeclarations(cabinetOnly: true)),
                };
                foreach (RedLine l in Derived()) { sb.Append(F("    MEASURED  the derived line {0}-{1}: {2}\n", parties[l.A].Abbrev, parties[l.B].Abbrev, l.Basis)); }
                int ofRecord = Mask("KO", "TD", "NL");
                bool negative = ChamberRules.UsesNegativeParliamentarism(CountryId.Poland);
                string rule = negative ? "negative parliamentarism" : "positive investiture";

                // forms a chamber under every reading and prints each government, the record's cabinet's standing and the best options
                bool Measure(string chamber, int[] seats)
                {
                    double[,] compatibility = GovernmentFormation.Compatibility(parties);
                    int[] joint = ChamberRules.JointMasks(CountryId.Poland, parties, seats);
                    sb.Append(F("    MEASURED  {0} (seats {1}; majority {2} of {3}; {4}) - the record's cabinet {5} ({6} seats):\n", chamber,
                        string.Join(", ", Enumerable.Range(0, parties.Count).Where(p => seats[p] > 0).Select(p => parties[p].Abbrev + " " + seats[p])), seats.Sum() / 2 + 1, seats.Sum(), rule,
                        Names(ofRecord), Enumerable.Range(0, parties.Count).Where(p => (ofRecord & (1 << p)) != 0).Sum(p => seats[p])));
                    bool formable = true;
                    // the second review's finding 14: a refusal named by its cause - a line inside, a seatless member, a split group
                    string Refusal(CoalitionFormation.CabinetEvaluation e)
                    {
                        var why = new List<string>();
                        if (e.InternalLine.Basis != null) { why.Add(Names((1 << e.InternalLine.A) | (1 << e.InternalLine.B))); }
                        if (e.SeatlessMember) { why.Add(Names(Enumerable.Range(0, parties.Count).Where(p => (e.Cabinet & (1 << p)) != 0 && seats[p] <= 0).Aggregate(0, (m, p) => m | (1 << p))) + " holds no seat"); }
                        if (e.SplitsJointGroup) { why.Add("splits a parliamentary group"); }
                        return "refused (" + string.Join("; ", why) + ")";
                    }
                    foreach ((string name, List<RedLine> lines) in readings)
                    {
                        CoalitionResult r = CoalitionFormation.Form(seats, compatibility, lines, negativeRule: negative, inOrAgainst: new List<InOrAgainst>(), joint: joint);
                        CoalitionFormation.Chamber prepared = CoalitionFormation.Prepare(seats, compatibility, lines, negative, new List<InOrAgainst>(), joint);
                        CoalitionFormation.CabinetEvaluation recordEval = CoalitionFormation.Evaluate(prepared, ofRecord);
                        int rank = r.Viable.FindIndex(g => g.Cabinet == ofRecord);
                        formable &= recordEval.Admissible && recordEval.Wins;
                        sb.Append(F("              {0}: {1} - {2}{3} ({4} in cabinet, {5} supported, score {6:0.000}); the record's cabinet {7}; best: {8}\n",
                            name, r.Outcome, Names(r.Government.Cabinet), r.Government.Support != 0 ? " on " + Names(r.Government.Support) + "'s support" : string.Empty,
                            r.Government.CabinetSeats, r.Government.SupportedSeats, r.Government.Score,
                            !recordEval.Admissible ? Refusal(recordEval) : recordEval.Wins ? (rank >= 0 ? "viable, ranked " + (rank + 1) + " of " + r.Viable.Count : "wins its vote but is not among the viable") : "loses its vote",
                            string.Join(" | ", r.Viable.Take(3).Select(g => Names(g.Cabinet) + (g.Support != 0 ? " on " + Names(g.Support) : string.Empty) + " " + g.SupportedSeats.ToString(CultureInfo.InvariantCulture)))));
                    }
                    return formable;
                }

                // (e) THE CHAMBER OF RECORD - the 2023 count's seed seats, which no game forms (the government of record is seated at a Polish start;
                // a Polish game forms its own count, (h)) - under every reading
                int[] recordSeats = parties.Select(p => p.SeedSeats).ToArray();
                Check(Measure("the chamber of record (the 2023 count)", recordSeats), "on the chamber of record, the record's cabinet (KO+TD+NL) is formable under every reading - no sourced line falls among its members");

                // (f) THE WIRING (§776; the review's finding 14 - the 2023 election pinned by name, never the ambient epoch): the vintage reader and the
                // election's own dated reader both give R1, line for line, the declared lines exactly the polling day's standing set, and no platform
                string Key(RedLine l) => parties[l.A].Abbrev + ">" + parties[l.B].Abbrev + "|" + l.Kind + "|" + l.BlocksSupport + "|" + l.OneWay + "|" + l.Basis;
                List<string> r1 = WithDeclarations().Select(Key).OrderBy(k => k, StringComparer.Ordinal).ToList();
                List<RedLine> byVintage = DeclaredRedLines.For(CountryId.Poland, parties, ElectionVintage.Poland2023);
                DeclarationReading night = DeclarationReading.OfElection(CountryId.Poland, PollingDay);
                List<RedLine> byNight = night.Lines(CountryId.Poland, parties);
                bool sameLines = byVintage.Select(Key).OrderBy(k => k, StringComparer.Ordinal).SequenceEqual(r1) && byNight.Select(Key).OrderBy(k => k, StringComparer.Ordinal).SequenceEqual(r1);
                bool declaredCount = byVintage.Count(l => l.Kind == RedLineKind.Declared) == standing.Count;
                Check(DeclaredRedLines.IsSourced(CountryId.Poland) && DeclaredRedLines.HasTimeline(CountryId.Poland) && !DeclaredRedLines.CandidacyRefuses(CountryId.Poland)
                      && sameLines && declaredCount && night.Dated && night.Platforms(CountryId.Poland, parties).Count == 0
                      && DeclaredRedLines.InOrAgainstFor(CountryId.Poland, parties, ElectionVintage.Poland2023).Count == 0,
                    F("the game reads R1 - the 2023 election's {0} lines, by its vintage and by the election's own dated reading, are the derived ones and the declarations of its polling day, line for line, its declared lines the {1} standing; no platform; Poland sourced and dated, no candidacy rule",
                        byVintage.Count, standing.Count));

                // (g) A BACKTEST on the chamber of record, formed with the game's reader: the record's majority seated, PiS outside it
                using (SimulationManager.EpochScope())
                {
                    double[,] compatibility = GovernmentFormation.Compatibility(parties);
                    CoalitionResult formed = CoalitionFormation.Form(recordSeats, compatibility, byVintage, negativeRule: negative, inOrAgainst: new List<InOrAgainst>(),
                        joint: ChamberRules.JointMasks(CountryId.Poland, parties, recordSeats));
                    int governs = formed.Government.Cabinet | formed.Government.Support;
                    Check(formed.Outcome != CoalitionOutcomeKind.NewElection && governs == ofRecord && (formed.Government.Cabinet & Mask("PiS")) == 0,
                        F("the chamber of record, formed with the game's reader, seats the record's majority: {0}{1}, {2} supported - KO, TD and NL in it or carrying it, PiS outside (R0 above is what the derived lines alone seated)",
                            Names(formed.Government.Cabinet), formed.Government.Support != 0 ? " on " + Names(formed.Government.Support) + "'s support" : string.Empty, formed.Government.SupportedSeats));
                }

                // (h) THE PLAYED COUNT (the review's finding 9): the chamber a Polish game forms - its own 2023 count, by a copy of the path
                // `GameController.RunNationalElection` takes with no campaign staged (Poland's start, no policy, the live prediction counted through the 41
                // districts on polling day; `PollingDayDiagnostic` (5) runs the same path) - formed by the game's own entry and under every reading. The
                // entry is the government the night STORES, `GovernmentFormation.ViewOf` on the election's dated reading (the second review's finding 11):
                // its cabinet and its support are held to R1's, and to F7's acceptance test - "in a world that follows history, a Polish game's own 2023
                // election forms KO+TD+NL" (READ AS: the world stepped from the start with no policy - the reading the record's §12 states and puts to Elias)
                using (SimulationManager.EpochScope())
                {
                    WorldClock.ApplyStart(CountryId.Poland);
                    var go = new GameObject("PolishDeclarationsDiagnostic played count");
                    try
                    {
                        SimulationRandom.Seed(PlayedSeed);
                        EnergyMarket.ResetCalibration();
                        World world = WorldFactory.CreateDefault();
                        SimulationManager sim = go.AddComponent<SimulationManager>();
                        sim.SetWorld(world);
                        sim.PlayerCountryId = CountryId.Poland;
                        Country player = world.GetCountry(CountryId.Poland);
                        player.PlayerPartyAbbrev = "PiS";
                        var none = new Dictionary<CountryId, PolicyDecision>();
                        foreach (Country c in world.Countries) { none[c.Id] = PolicyDecision.None(); }
                        ElectionRecord counted = null;
                        for (int day = 1; day <= 400 && counted == null && sim.CurrentDate <= PollingDay; day++)
                        {
                            if (sim.AdvanceDay()) { sim.AdvanceTurn(none); }
                            if (sim.PollingDayToday && NationalElection.TryPredictShares(CountryId.Poland, out Dictionary<string, double> predicted, EconomicVote.RecordOverTerm(player, sim.CurrentDate, out _), on: sim.CurrentDate))
                            {
                                counted = NationalElection.Run(CountryId.Poland, sim.CurrentTurn, predicted, sim.CurrentDate);
                            }
                        }
                        Check(counted != null && counted.Date == PollingDay && counted.Seats.Values.Sum() == recordSeats.Sum(),
                            F("a Polish game's own count on {0:yyyy-MM-dd} (seed {1}, no policy): {2}", PollingDay, PlayedSeed,
                                counted == null ? "NOT COUNTED" : string.Join(", ", counted.Seats.Where(kv => kv.Value > 0).Select(kv => kv.Key + " " + kv.Value))));
                        if (counted != null)
                        {
                            int[] played = parties.Select(p => counted.Seats.TryGetValue(p.Abbrev, out int s) ? s : 0).ToArray();
                            Measure("the played count (a Polish game's own 2023 count)", played);
                            ParliamentSystem.SetSeatsFromElection(player, counted.Seats);
                            GovernmentFormation.View view = GovernmentFormation.ViewOf(player, DeclarationReading.OfElection(CountryId.Poland, PollingDay));
                            CoalitionResult r1Played = CoalitionFormation.Form(played, GovernmentFormation.Compatibility(parties), WithDeclarations(), negativeRule: negative,
                                inOrAgainst: new List<InOrAgainst>(), joint: ChamberRules.JointMasks(CountryId.Poland, parties, played));
                            HashSet<string> Keys(int mask) => new HashSet<string>(Enumerable.Range(0, parties.Count).Where(p => (mask & (1 << p)) != 0).Select(p => parties[p].Abbrev));
                            var gameCabinet = new HashSet<string>(view.Cabinet.Select(c => c.Abbrev));
                            var gameSupport = new HashSet<string>(view.Support.Select(c => c.Abbrev));
                            string Say(IEnumerable<string> cabinet, IEnumerable<string> support) => string.Join("+", cabinet) + (support.Any() ? " on " + string.Join("+", support) + "'s support" : string.Empty);
                            Check(view.HasGovernment && gameCabinet.SetEquals(Keys(r1Played.Government.Cabinet)) && gameSupport.SetEquals(Keys(r1Played.Government.Support)),
                                F("the game's own entry - the government the night stores - forms the played count as R1 does: {0} (R1: {1})",
                                    view.HasGovernment ? Say(view.Cabinet.Select(c => c.Abbrev), view.Support.Select(c => c.Abbrev)) : "no government - " + view.Reason,
                                    Say(Keys(r1Played.Government.Cabinet), Keys(r1Played.Government.Support))));
                            Check(view.HasGovernment && gameCabinet.SetEquals(Keys(ofRecord)),
                                F("F7's ACCEPTANCE: in a world that follows history, a Polish game's own 2023 election forms KO+TD+NL - the game's entry seats {0}",
                                    view.HasGovernment ? Say(view.Cabinet.Select(c => c.Abbrev), view.Support.Select(c => c.Abbrev)) : "no government - " + view.Reason));
                        }
                    }
                    finally
                    {
                        UnityEngine.Object.DestroyImmediate(go);
                        EnergyMarket.ResetTurnState();
                    }
                }
            }
            catch (Exception ex) { failures++; sb.Append("    FAIL      threw: ").Append(ex).Append('\n'); }

            sb.Append(failures == 0 ? "=== PolishDeclarationsDiagnostic: held, and measured ===" : F("=== PolishDeclarationsDiagnostic: {0} FAILED ===", failures));
            if (failures == 0) { Debug.Log(sb.ToString()); } else { Debug.LogError(sb.ToString()); }
            CheckExit.Finish(failures == 0 ? 0 : 1);
        }

        /// <summary>The played count's seed - the one `PollingDayDiagnostic` (5) steps its Polish game on, copied (a copy, stated: the two paths are
        /// both copies of `GameController.RunNationalElection`'s no-campaign path, and nothing ties their seeds but this note).</summary>
        private const int PlayedSeed = 777;

        private static string Line(DeclaredRedLines.DatedFact f) => f.Party + " -> " + f.Other + (f.OneWay ? " one way" : string.Empty) + (f.BlocksSupport ? " support-blocking" : " cabinet");

        private static DateTime Day(string iso) => DateTime.ParseExact(iso, "yyyy-MM-dd", CultureInfo.InvariantCulture);

        private static string Sha256(string path)
        {
            using (SHA256 sha = SHA256.Create()) { return string.Concat(sha.ComputeHash(File.ReadAllBytes(path)).Select(b => b.ToString("x2", CultureInfo.InvariantCulture))); }
        }

        private static string F(string format, params object[] args) => string.Format(CultureInfo.InvariantCulture, format, args);
    }
}
