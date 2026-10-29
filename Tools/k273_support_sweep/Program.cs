using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using PoliSim.Data;
using PoliSim.Elections;
using PoliSim.Elections.Generated;

// K273 (Tools/k273_support_sweep): the prereg's sweep - main's formation (Variants.cs, proved by `validate`) with K273's support fraction f
// (removed from the runtime by K300), over the four elections of record (the record's seats, and the game's count where a check holds one).
// Run: bash Tools/k273_support_sweep/run.sh <worktree> [<step> | movers <f> | decompose | holdout-payoff | holdout-partnered | holdout-positive | holdout-both]   (step default 0.05; never Unity)
// "shipped Form" is the runtime's; "reproduction" is this sweep at the runtime's own rule (Variants.BuiltFraction, BuiltBonus) and must read IDENTICAL. "movers <f>": every chamber of record
// and seed chamber, both investiture rules, the government at f = 0 against f. "decompose": the Polish record's score terms.
// "holdout-payoff": option (c), MEASURED ONLY - a party holds out on its own Gamson payoff, not the cabinet score (FormHoldoutPayoff), at f = 0 and 0.30. "holdout-partnered" / "holdout-positive" / "holdout-both":
// its narrowed variants (FormHoldoutPayoff's variant argument). Variants.cs adds validate | support-capped | majority-bonus | positive-investiture |
// capped-positive (K289, K300); DumpCheck.cs adds dump-check <branch log> <main log> | replay <main log> <w> (K300's prereg (iii)).
static class Program
{
    static string F(string f, params object[] a) => string.Format(CultureInfo.InvariantCulture, f, a);

    sealed class Case
    {
        public string Name; public CountryId Country; public Dictionary<string, int> Seats; public DeclarationReading Reading; public string[] Expected;
    }

    /// <summary>The formation at a support fraction f (K273, removed by K300) and a majority bonus w - Variants.cs's generic formation, main's arithmetic
    /// re-stated (proved by `validate`), so these modes measure what they measured whatever the runtime now builds.</summary>
    static (GovernmentOption gov, List<GovernmentOption> viable, Variants.Result result) FormWith(int[] seats, double[,] compat, List<RedLine> lines, bool negative, List<InOrAgainst> rules, int[] joint, double f, double w = 0.0)
    {
        Variants.Result r = Variants.FormRaw(seats, compat, lines, negative, rules, joint, f, w);
        return (r.Gov, r.Viable, r);
    }

    static bool WouldHold(GovernmentOption g, List<GovernmentOption> others, int[] seats, double[,] compat, int n)
    {
        int involved = g.Cabinet | g.Support;
        for (int p = 0; p < n; p++)
        {
            if ((involved & (1 << p)) == 0) { continue; }
            double here = CoalitionFormation.Payoff(p, g, seats, compat, n);
            foreach (var alt in others)
            {
                if (alt.Cabinet == g.Cabinet && alt.Support == g.Support) { continue; }
                if (CoalitionFormation.Payoff(p, alt, seats, compat, n) > here + CoalitionFormation.DefectionMargin) { return false; }
            }
        }
        return true;
    }

    sealed class HoldoutInfo
    {
        public int Rounds; public string Note = ""; public string HoldoutLine = "";
        public Dictionary<int, string> Fate = new Dictionary<int, string>();
    }

    /// <summary>
    /// Option (c), MEASURED ONLY (never shipped): a party outside a cabinet - a derived supporter included, since support buys no portfolio - holds out
    /// (withdraws its support and votes against) when its OWN Gamson payoff (CoalitionFormation.Payoff) in the best cabinet it could sit in that passes on
    /// the lines and rules alone (K-1h (i)'s passable set) beats its payoff here (0 outside a cabinet) by DefectionMargin - in place of today's comparison of
    /// the cabinet SCORES. Lines, in-or-against rules, joint groups, admissibility, the defection rounds and the ranking are today's; the score counts the
    /// supporters left after the hold-outs at f. No score decides a hold-out, so nothing chases its tail.
    /// <paramref name="variant"/> narrows which alternatives ground a hold-out: "payoff" (c) as above; "partnered" - passable on the lines and rules AND
    /// holding the party and at least one other (a lone-party cabinet never grounds one); "positive" - its members alone hold a majority of the chamber
    /// (more for than against counting only members' seats), solo cabinets allowed, the lines-and-rules test not asked.
    /// "both" - the partnered hold-out ADDED to today's score hold-out (<paramref name="todayResult"/>'s opposition, at f), not in its place.
    /// </summary>
    static (GovernmentOption gov, List<GovernmentOption> viable, HoldoutInfo info) FormHoldoutPayoff(int[] seats, double[,] compat, List<RedLine> lines, bool negative,
        List<InOrAgainst> rules, int[] joint, double f, Func<int, string> names, string variant = "payoff", Variants.Result todayResult = null)
    {
        var info = new HoldoutInfo();
        var chamber = CoalitionFormation.Prepare(seats, compat, lines, negative, rules, joint);   // admissibility, passability, power; the base score is main's (Variants.MainBase)
        int n = chamber.N;
        var bestPay = new double[n]; var bestCab = new int[n];
        for (int p = 0; p < n; p++) { bestPay[p] = double.NegativeInfinity; }
        foreach (int c in chamber.Admissible)
        {
            bool grounds = variant == "positive" ? CoalitionMath.Seats(seats, c) >= chamber.Majority
                : variant == "partnered" || variant == "both" ? chamber.PassesOnLines[c] && (c & (c - 1)) != 0
                : chamber.PassesOnLines[c];
            if (!grounds) { continue; }
            var option = new GovernmentOption(c, 0, CoalitionOutcomeKind.MinorityGovernment, CoalitionMath.Seats(seats, c), CoalitionMath.Seats(seats, c), 0, 0.0, 0.0);
            for (int p = 0; p < n; p++)
            {
                if ((c & (1 << p)) == 0) { continue; }
                double pay = CoalitionFormation.Payoff(p, option, seats, compat, n);
                if (pay > bestPay[p]) { bestPay[p] = pay; bestCab[p] = c; }
            }
        }
        var lineParts = new List<string>();
        for (int p = 0; p < n; p++) { if (seats[p] > 0 && bestPay[p] > CoalitionFormation.DefectionMargin) { lineParts.Add(F("{0} ({1}, {2:0.000})", names(1 << p), names(bestCab[p]), bestPay[p])); } }
        info.HoldoutLine = lineParts.Count == 0 ? "none" : string.Join(", ", lineParts);

        var viable = new List<GovernmentOption>();
        foreach (int c in chamber.Admissible)
        {
            var e0 = CoalitionFormation.Evaluate(chamber, c);   // admissibility and the derived supporters, as today
            int holds = 0;
            for (int p = 0; p < n; p++) { if ((c & (1 << p)) == 0 && seats[p] > 0 && bestPay[p] > 0.0 + CoalitionFormation.DefectionMargin) { holds |= 1 << p; } }
            int support = CoalitionFormation.JointAlike(e0.SupportMask & ~holds, c, joint, seats);
            int oppose = (CoalitionFormation.RedLinedMask(c, n, lines, rules) | holds) & ~c & ~support;
            if (variant == "both") { oppose = (oppose | todayResult.Oppose[c]) & ~c & ~support; }   // today's score hold-outs kept beside the partnered payoff ones
            if (joint != null)
            {
                foreach (int m in joint)
                {
                    if ((c & m) != 0) { continue; }
                    int larger = -1;
                    for (int p = 0; p < n; p++) { if ((m & (1 << p)) != 0 && (larger < 0 || seats[p] > seats[larger])) { larger = p; } }
                    if (larger < 0) { continue; }
                    oppose = (oppose & (1 << larger)) != 0 ? oppose | (m & ~support) : oppose & ~m;
                }
            }
            int cabSeats = CoalitionMath.Seats(seats, c), supported = cabSeats + CoalitionMath.Seats(seats, support), opposed = CoalitionMath.Seats(seats, oppose);
            bool wins = e0.Admissible && (supported >= chamber.Majority || (negative && opposed < chamber.Majority));
            double score = Variants.MainBase(chamber, c, compat) + f * CoalitionFormation.WeightSeatStrength * 100.0 * CoalitionMath.Seats(seats, support) / Math.Max(1, chamber.TotalSeats);
            var kind = cabSeats >= chamber.Majority ? CoalitionOutcomeKind.MajorityCoalition : support != 0 && supported >= chamber.Majority ? CoalitionOutcomeKind.ConfidenceAndSupply : CoalitionOutcomeKind.MinorityGovernment;
            info.Fate[c] = wins ? F("wins its vote ({0} for, {1} against, majority {2}) at score {3:0.000}", supported, opposed, chamber.Majority, score)
                : F("LOSES its vote ({0} for, {1} against, majority {2}; holding out: {3}; derived support was {4})", supported, opposed, chamber.Majority, names(holds & ~c), names(e0.SupportMask));
            if (wins) { viable.Add(new GovernmentOption(c, support, kind, cabSeats, supported, opposed, CoalitionFormation.Cohesion(c, n, compat), score)); }
        }
        int round = 0;
        for (; round < 32; round++)
        {
            var kept = viable.Where(g => WouldHold(g, viable, seats, compat, n)).ToList();
            if (kept.Count == 0 && viable.Count == 0) { info.Note = " (nothing wins its vote: a new election)"; break; }
            if (kept.Count == 0) { info.Note = F(" (the defection rounds were ABANDONED at round {0}: every survivor had a party better off elsewhere; the {1} survivors stand)", round + 1, viable.Count); break; }
            if (kept.Count == viable.Count) { break; }
            foreach (var g in viable) { if (!kept.Contains(g) && info.Fate.ContainsKey(g.Cabinet)) { info.Fate[g.Cabinet] += F(", but a party in or behind it defects in round {0}", round + 1); } }
            viable = kept;
        }
        if (round == 32) { info.Note = " (CYCLE: the defection rounds hit their limit of 32)"; }
        info.Rounds = round + 1;
        viable.Sort((a, b) => b.Score.CompareTo(a.Score));
        foreach (var g in viable) { info.Fate[g.Cabinet] += F(", viable after the rounds, ranked {0} of {1}", viable.IndexOf(g) + 1, viable.Count); }
        return (viable.Count == 0 ? default : viable[0], viable, info);
    }

    static int Main(string[] args)
    {
        // K289: the minority-cabinet variants (Variants.cs)
        if (args.Length > 0 && new[] { "validate", "support-capped", "majority-bonus", "positive-investiture", "capped-positive", "dump-check", "replay", "sejm-trace", "bonus-at" }.Contains(args[0])) { return Variants.Run(args); }
        var cases = new List<Case>();
        cases.Add(new Case { Name = "Sweden 2022 (record seats, 2022 declarations)", Country = CountryId.Sweden, Reading = DeclarationReading.OfVintage(ElectionVintage.Sweden2022),
            Seats = new Dictionary<string, int> { { "S", 107 }, { "SD", 73 }, { "M", 68 }, { "V", 24 }, { "C", 24 }, { "KD", 19 }, { "MP", 18 }, { "L", 16 } }, Expected = new[] { "M", "KD", "L" } });
        cases.Add(new Case { Name = "Germany 2025 (record seats, polling-day reading)", Country = CountryId.Germany, Reading = DeclarationReading.OfElection(CountryId.Germany, new DateTime(2025, 2, 23)),
            Seats = PartySystems.InitialSeats(CountryId.Germany, ElectionVintage.Germany2025), Expected = new[] { "CDU", "CSU", "SPD" } });
        cases.Add(new Case { Name = "Poland 2023 (record seats, polling-day reading)", Country = CountryId.Poland, Reading = DeclarationReading.OfElection(CountryId.Poland, new DateTime(2023, 10, 15)),
            Seats = new Dictionary<string, int> { { "PiS", 194 }, { "KO", 157 }, { "TD", 65 }, { "NL", 26 }, { "Konf", 18 } }, Expected = new[] { "KO", "TD", "NL" } });
        cases.Add(new Case { Name = "Poland 2023 (the game's count, nplc2 seed 777)", Country = CountryId.Poland, Reading = DeclarationReading.OfElection(CountryId.Poland, new DateTime(2023, 10, 15)),
            Seats = new Dictionary<string, int> { { "PiS", 193 }, { "KO", 158 }, { "TD", 67 }, { "NL", 26 }, { "Konf", 16 } }, Expected = new[] { "KO", "TD", "NL" } });
        cases.Add(new Case { Name = "Italy 2022 Camera (record seats, polling-day reading)", Country = CountryId.Italy, Reading = DeclarationReading.OfElection(CountryId.Italy, new DateTime(2022, 9, 25)),
            Seats = PartySystems.InitialSeats(CountryId.Italy, ElectionVintage.Italy2022), Expected = new[] { "FdI", "Lega", "FI", "NM" } });
        try
        {
            var even = new Dictionary<string, double>();
            foreach (string key in ItalianCount2022.Keys) { even[key] = (double)ItalianCount2022.CameraPublished2022Votes[Array.IndexOf(ItalianCount2022.Keys, key)] / ItalianCount2022.CameraPublished2022Valid; }
            double total = even.Values.Sum();
            var game = even.ToDictionary(kv => kv.Key, kv => kv.Value / total);
            cases.Add(new Case { Name = "Italy 2022 Camera (the game's count on the 2022 vote)", Country = CountryId.Italy, Reading = DeclarationReading.OfElection(CountryId.Italy, new DateTime(2022, 9, 25)),
                Seats = new Dictionary<string, int>(ItalianCount.Camera(game).Seats), Expected = new[] { "FdI", "Lega", "FI", "NM" } });
            cases.Add(new Case { Name = "Italy 2022 Senato (the game's count on the 2022 vote; mandate 2's arithmetic)", Country = CountryId.Italy, Reading = DeclarationReading.OfVintage(ElectionVintage.Italy2022),
                Seats = ItalianCount.SenatoSeated(game, WorldClock.ItalyFirstSitting2022), Expected = new[] { "FdI", "Lega", "FI", "NM" } });
        }
        catch (Exception ex) { Console.WriteLine("Italian count unavailable: " + ex.GetType().Name + " " + ex.Message); }

        if (args.Length > 0 && args[0] == "movers")
        {
            double fm = double.Parse(args[1], CultureInfo.InvariantCulture);
            var chambers = new List<(string Name, CountryId Country, Dictionary<string, int> Seats, DeclarationReading Reading)>();
            foreach (ElectionVintage v in Enum.GetValues(typeof(ElectionVintage)))
            {
                if (v == ElectionVintage.Seated) { continue; }
                foreach (CountryId id in Enum.GetValues(typeof(CountryId)))
                {
                    if (!v.ToString().StartsWith(id == CountryId.USA ? "Usa" : id.ToString())) { continue; }
                    Dictionary<string, int> t = null;
                    try { t = PartySystems.InitialSeats(id, v); } catch (Exception ex) { Console.WriteLine(F("   {0} {1}: no table ({2})", id, v, ex.GetType().Name)); continue; }
                    if (t == null || t.Values.Sum() == 0) { continue; }
                    chambers.Add((id + " " + v + " (vintage declarations)", id, t, DeclarationReading.OfVintage(v)));
                }
            }
            foreach (CountryId id in Enum.GetValues(typeof(CountryId)))
            {
                var parties = PartySystems.For(id);
                var seed = new Dictionary<string, int>(); foreach (var p in parties) { seed[p.Abbrev] = p.SeedSeats; }
                chambers.Add((id + " seed seats (seated declarations)", id, seed, DeclarationReading.OfVintage(ElectionVintage.Seated)));
            }
            chambers.Add(("Sweden year-32 (seated declarations)", CountryId.Sweden, new Dictionary<string, int> { { "S", 94 }, { "SD", 63 }, { "M", 70 }, { "V", 27 }, { "C", 24 }, { "KD", 27 }, { "MP", 24 }, { "L", 20 } }, DeclarationReading.OfVintage(ElectionVintage.Seated)));
            foreach (var c in chambers)
            {
                var parties = PartySystems.For(c.Country);
                var seats = parties.Select(p => c.Seats.TryGetValue(p.Abbrev, out int s) ? s : 0).ToArray();
                if (seats.Sum() == 0 || parties.Count > 20) { Console.WriteLine("   skip " + c.Name); continue; }
                var compat = GovernmentFormation.Compatibility(parties);
                var lines = c.Reading.Lines(c.Country, parties);
                var rules = c.Reading.Platforms(c.Country, parties);
                int[] joint = ChamberRules.JointMasks(c.Country, parties, seats);
                string Names(int mask) => string.Join("+", Enumerable.Range(0, parties.Count).Where(p => (mask & (1 << p)) != 0).Select(p => parties[p].Abbrev));
                foreach (bool negative in new[] { ChamberRules.UsesNegativeParliamentarism(c.Country), !ChamberRules.UsesNegativeParliamentarism(c.Country) })
                {
                    var (a, va, _) = FormWith(seats, compat, lines, negative, rules, joint, 0.0);
                    var (b, vb, _) = FormWith(seats, compat, lines, negative, rules, joint, fm);
                    string sa = va.Count == 0 ? "NEW ELECTION" : Names(a.Cabinet) + (a.Support != 0 ? " on " + Names(a.Support) : "");
                    string sb2 = vb.Count == 0 ? "NEW ELECTION" : Names(b.Cabinet) + (b.Support != 0 ? " on " + Names(b.Support) : "");
                    // the governments that hold (what a proposal's answers compare against) and their order, beside the government formed
                    string Set(List<GovernmentOption> v) => string.Join(" | ", v.Select(g => Names(g.Cabinet) + (g.Support != 0 ? " on " + Names(g.Support) : "")));
                    string setA = Set(va), setB = Set(vb);
                    Console.WriteLine(F("   {0} {1} | negative {2}{3}: f=0 {4} -> f={5:0.00} {6}{7}", sa == sb2 ? "same " : "MOVES", c.Name, negative, negative == ChamberRules.UsesNegativeParliamentarism(c.Country) ? " (the country's rule)" : " (the other rule)", sa, fm, sb2,
                        setA == setB ? "; holding set and order same" : new HashSet<string>(setA.Split(" | ")).SetEquals(setB.Split(" | ")) ? "; holding set same, ORDER differs: " + setA + " -> " + setB : "; HOLDING SET differs: " + setA + " -> " + setB));
                }
            }
            return 0;
        }
        if (args.Length > 0 && (args[0] == "holdout-payoff" || args[0] == "holdout-partnered" || args[0] == "holdout-positive" || args[0] == "holdout-both"))
        {
            // Option (c), measured for Elias: today's formation (K273's f) against FormHoldoutPayoff, on the 7 chambers of record and the
            // six seed chambers, both investiture rules, at f = 0 and 0.30.
            var chambers = new List<(string Name, CountryId Country, Dictionary<string, int> Seats, DeclarationReading Reading, string[] Expected)>();
            foreach (Case c in cases) { chambers.Add((c.Name, c.Country, c.Seats, c.Reading, c.Expected)); }
            foreach (CountryId id in Enum.GetValues(typeof(CountryId)))
            {
                var seed = new Dictionary<string, int>(); foreach (var p in PartySystems.For(id)) { seed[p.Abbrev] = p.SeedSeats; }
                chambers.Add((id + " seed seats (seated declarations)", id, seed, DeclarationReading.OfVintage(ElectionVintage.Seated), null));
            }
            var recordPass = new List<string>();
            foreach (double f in new[] { 0.0, 0.30 })
            {
                Console.WriteLine(F("##### f = {0:0.00}", f));
                var passes = new List<string>();
                foreach (var c in chambers)
                {
                    var parties = PartySystems.For(c.Country);
                    var seats = parties.Select(p => c.Seats.TryGetValue(p.Abbrev, out int s) ? s : 0).ToArray();
                    var compat = GovernmentFormation.Compatibility(parties);
                    var lines = c.Reading.Lines(c.Country, parties);
                    var rules = c.Reading.Platforms(c.Country, parties);
                    int[] joint = ChamberRules.JointMasks(c.Country, parties, seats);
                    string Names(int mask) => mask == 0 ? "-" : string.Join("+", Enumerable.Range(0, parties.Count).Where(p => (mask & (1 << p)) != 0).Select(p => parties[p].Abbrev));
                    string Sig(GovernmentOption g, int count) => count == 0 ? "NO GOVERNMENT" : Names(g.Cabinet) + (g.Support != 0 ? " on " + Names(g.Support) : "");
                    int expected = 0;
                    if (c.Expected != null) { foreach (string k in c.Expected) { expected |= 1 << parties.ToList().FindIndex(p => p.Abbrev == k); } }
                    bool countryRule = ChamberRules.UsesNegativeParliamentarism(c.Country);
                    foreach (bool negative in new[] { countryRule, !countryRule })
                    {
                        var (today, vt, todayResult) = FormWith(seats, compat, lines, negative, rules, joint, f);
                        var (cg, vc, info) = FormHoldoutPayoff(seats, compat, lines, negative, rules, joint, f, Names, args[0].Substring("holdout-".Length), todayResult);
                        string st = Sig(today, vt.Count), sc = Sig(cg, vc.Count);
                        string verdict = "";
                        if (c.Expected != null && negative == countryRule)
                        {
                            bool ok = vc.Count > 0 && cg.Cabinet == expected;
                            verdict = ok ? " [RECORD PASS]" : " [RECORD MISS]";
                            passes.Add((ok ? "PASS " : "MISS ") + c.Name);
                        }
                        Console.WriteLine(F("   {0} {1} | negative {2}{3}: today {4} | (c) {5}{6}; rounds {7}{8}",
                            st == sc ? "same " : "MOVES", c.Name, negative, negative == countryRule ? " (country's rule)" : " (other rule)", st, sc, verdict, info.Rounds, info.Note));
                        if (st != sc)
                        {
                            Console.WriteLine(F("        today's winner {0} (score {1:0.000}); under (c) it {2}", st, vt.Count > 0 ? today.Score : double.NaN,
                                vt.Count > 0 && info.Fate.TryGetValue(today.Cabinet, out string fate) ? fate : "-"));
                            if (vc.Count > 0)
                            {
                                int at = vt.FindIndex(v => v.Cabinet == cg.Cabinet);
                                Console.WriteLine(F("        (c)'s winner {0} (score {1:0.000}); today it {2}", sc, cg.Score, at >= 0 ? "was viable, ranked " + (at + 1) + " of " + vt.Count : "was not among today's viable"));
                            }
                            Console.WriteLine("        parties with a passable own cabinet worth more than the margin (best cabinet, own payoff): " + info.HoldoutLine);
                        }
                    }
                }
                recordPass.Add(F("RECORDS under " + args[0] + " at f = {0:0.00}: {1} of {2} pass - {3}", f, passes.Count(s => s.StartsWith("PASS")), passes.Count, string.Join("; ", passes)));
            }
            foreach (string line in recordPass) { Console.WriteLine(line); }
            return 0;
        }
        if (args.Length > 0 && args[0] == "decompose")
        {
            var parties = PartySystems.For(CountryId.Poland);
            foreach (var held in new[] { new Dictionary<string, int> { { "PiS", 194 }, { "KO", 157 }, { "TD", 65 }, { "NL", 26 }, { "Konf", 18 } }, new Dictionary<string, int> { { "PiS", 193 }, { "KO", 158 }, { "TD", 67 }, { "NL", 26 }, { "Konf", 16 } } })
            {
                var seats = parties.Select(p => held.TryGetValue(p.Abbrev, out int s) ? s : 0).ToArray();
                var compat = GovernmentFormation.Compatibility(parties);
                var reading = DeclarationReading.OfElection(CountryId.Poland, new DateTime(2023, 10, 15));
                var ch = CoalitionFormation.Prepare(seats, compat, reading.Lines(CountryId.Poland, parties), false, reading.Platforms(CountryId.Poland, parties), ChamberRules.JointMasks(CountryId.Poland, parties, seats));
                int M(params string[] k) { int m = 0; foreach (string s in k) { m |= 1 << parties.ToList().FindIndex(p => p.Abbrev == s); } return m; }
                foreach (int cab in new[] { M("KO", "TD"), M("KO", "TD", "NL"), M("KO") })
                {
                    double coh = CoalitionFormation.Cohesion(cab, ch.N, compat), seat = 100.0 * CoalitionMath.Seats(seats, cab) / ch.TotalSeats, pow = 0; for (int p = 0; p < ch.N; p++) { if ((cab & (1 << p)) != 0) { pow += ch.Power[p]; } }
                    var e = CoalitionFormation.Evaluate(ch, cab);
                    Console.WriteLine(F("   {0}: seats {1}; cohesion {2:0.000} (x0.5 = {3:0.000}), seat share {4:0.000} (x0.3 = {5:0.000}), power {6:0.000} (x0.2x100 = {7:0.000}); base {8:0.000}; derived support {9} ({10} seats, x0.3 share per unit f = {11:0.000})",
                        string.Join("+", Enumerable.Range(0, ch.N).Where(p => (cab & (1 << p)) != 0).Select(p => parties[p].Abbrev)), CoalitionMath.Seats(seats, cab), coh, 0.5 * coh, seat, 0.3 * seat, pow, 20 * pow, Variants.MainBase(ch, cab, compat),
                        string.Join("+", Enumerable.Range(0, ch.N).Where(p => (e.SupportMask & (1 << p)) != 0).Select(p => parties[p].Abbrev)), CoalitionMath.Seats(seats, e.SupportMask), 30.0 * CoalitionMath.Seats(seats, e.SupportMask) / ch.TotalSeats));
                }
            }
            return 0;
        }
        double step = args.Length > 0 ? double.Parse(args[0], CultureInfo.InvariantCulture) : 0.05;
        bool verbose = args.Length > 1;
        var passAll = new List<double>();
        var perCase = new Dictionary<string, List<double>>();
        foreach (Case c in cases)
        {
            var parties = PartySystems.For(c.Country);
            var seats = parties.Select(p => c.Seats.TryGetValue(p.Abbrev, out int s) ? s : 0).ToArray();
            var compat = GovernmentFormation.Compatibility(parties);
            var lines = c.Reading.Lines(c.Country, parties);
            var rules = c.Reading.Platforms(c.Country, parties);
            bool negative = ChamberRules.UsesNegativeParliamentarism(c.Country);
            int[] joint = ChamberRules.JointMasks(c.Country, parties, seats);
            string Names(int mask) => string.Join("+", Enumerable.Range(0, parties.Count).Where(p => (mask & (1 << p)) != 0).Select(p => parties[p].Abbrev));
            int expected = 0; foreach (string k in c.Expected) { int i = parties.ToList().FindIndex(p => p.Abbrev == k); expected |= 1 << i; }
            // baseline: the shipped Form against the reproduction at f = 0
            CoalitionResult shipped = CoalitionFormation.Form(seats, compat, lines, negative, rules, joint);
            var (g0, v0, ch0) = FormWith(seats, compat, lines, negative, rules, joint, Variants.BuiltFraction, Variants.BuiltBonus);
            Console.WriteLine(F("== {0}: seats {1}; total {2}, majority {3}, negative {4}", c.Name, string.Join(" ", parties.Select((p, i) => p.Abbrev + " " + seats[i]).Where((x, i) => seats[i] > 0)), ch0.TotalSeats, ch0.Majority, negative));
            Console.WriteLine(F("   shipped Form: {0} {1} on {2} score {3:R}; reproduction at the built rule: {4} on {5} score {6:R} -> {7}", shipped.Outcome, Names(shipped.Government.Cabinet), Names(shipped.Government.Support), shipped.Government.Score,
                Names(g0.Cabinet), Names(g0.Support), g0.Score, shipped.Government.Cabinet == g0.Cabinet && shipped.Government.Support == g0.Support && shipped.Government.Score.Equals(g0.Score) ? "IDENTICAL" : "DIFFERENT"));
            var passes = new List<double>();
            string last = null;
            for (int k = 0; k * step <= 1.0 + 1e-9; k++)
            {
                double f = Math.Round(k * step, 6);
                var (g, viable, ch) = FormWith(seats, compat, lines, negative, rules, joint, f);
                bool ok = viable.Count > 0 && g.Cabinet == expected;
                if (ok) { passes.Add(f); }
                string sig = viable.Count == 0 ? "NEW ELECTION" : Names(g.Cabinet) + (g.Support != 0 ? " on " + Names(g.Support) : "");
                if (sig != last || verbose)
                {
                    var rec = viable.FirstOrDefault(v => v.Cabinet == expected);
                    string top = string.Join(" | ", viable.Take(4).Select(v => F("{0}{1} {2:0.000}", Names(v.Cabinet), v.Support != 0 ? " on " + Names(v.Support) : "", v.Score)));
                    Console.WriteLine(F("   f={0:0.00}: {1} {2}; record cabinet {3}; top: {4}", f, ok ? "PASS" : "miss", sig,
                        viable.Any(v => v.Cabinet == expected) ? F("viable, score {0:0.000}", rec.Score) : "NOT viable", top));
                    last = sig;
                }
            }
            perCase[c.Name] = passes;
            Console.WriteLine(F("   passing f: {0}", passes.Count == 0 ? "NONE" : Ranges(passes, step)));
        }
        return 0;
    }

    static string Ranges(List<double> xs, double step)
    {
        var parts = new List<string>(); double a = xs[0], b = xs[0];
        for (int i = 1; i < xs.Count; i++) { if (xs[i] - b > step * 1.5) { parts.Add(F("[{0:0.00},{1:0.00}]", a, b)); a = xs[i]; } b = xs[i]; }
        parts.Add(F("[{0:0.00},{1:0.00}]", a, b));
        return string.Join(" ", parts);
    }
}
