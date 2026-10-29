using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using PoliSim.Data;
using PoliSim.Elections;
using PoliSim.Elections.Generated;

// K289 (K273 HELD): the minority-cabinet variants measured for Elias, out of Unity, never shipped. One generic formation - CoalitionFormation.Form's
// procedure (Evaluate's investiture and hold-out, the defection rounds, the ranking) re-stated with hooks - over the chambers of record, every row of
// FormationSweepDiagnostic (Sweden's seed, 2022 and year-32 chambers under every vintage and every dated edge, with and without rules; every other
// country's seated chamber) and Poland's two in-run chambers of seed 777's dump (K289's review, H1), under both investiture rules.
// It reads only API that main and wave/k273 share, so `validate` runs against either runtime: on main (`run.sh --main`) it proves the generic
// formation with no hook equals main's own CoalitionFormation.Form on every row, viable list and score bits included - "today" below IS main.
// "Today" is main's formation computed by this file alone: the base score is main's expression re-stated (MainBase), never read off the runtime, so
// the measurement does not move when the runtime does (K300 adds its bonus to the runtime's base score). `validate` proves the re-statement exact.
// Modes: validate | support-capped | majority-bonus | bonus-at <w> | positive-investiture | capped-positive | dump-check <branch log> <main log> |
// replay <log> <w> | sejm-trace <day> "<seats>" [w].
static class Variants
{
    static string F(string f, params object[] a) => string.Format(CultureInfo.InvariantCulture, f, a);

    sealed class Row
    {
        public string Name, Group; public CountryId Country; public IReadOnlyList<PoliticalParty> Parties; public int[] Seats;
        public List<RedLine> Lines; public List<InOrAgainst> Rules; public int Expected; public int ExpectedSupportMember = -1;
        /// <summary>K300's review L4: the other chambers the cabinet answers to (Italy's count: the Senato beside the Camera, or the Camera beside the Senato), by party order.</summary>
        public int[][] Also;
        public string Names(int mask) => mask == 0 ? "-" : string.Join("+", Enumerable.Range(0, Parties.Count).Where(p => (mask & (1 << p)) != 0).Select(p => Parties[p].Abbrev));
        public int Mask(params string[] keys) { int m = 0; foreach (string k in keys) { int i = Parties.ToList().FindIndex(p => p.Abbrev == k); if (i >= 0) { m |= 1 << i; } } return m; }
    }

    /// <summary>A variant: f the support fraction (K273), Cap (A) supporters counted only up to the seats the cabinet needs for its investiture line,
    /// W (B) a bonus of W of the seat-strength term's full scale for a cabinet whose own seats reach the majority, PosInv (C) under positive investiture a
    /// supporter's seats count only if it would vote FOR - 1 the score reading (it does not hold out on today's score test), 2 the payoff reading (it
    /// has no partnered passable cabinet it prefers by its own payoff).</summary>
    public sealed class Spec
    {
        public string Label; public double Fr; public bool Cap; public double W; public int PosInv;
        public static Spec Today => new Spec { Label = "today (main)" };
    }

    public sealed class Result
    {
        public int TotalSeats, Majority;
        /// <summary>Each admissible cabinet's opposition at its investiture, as this formation weighed it.</summary>
        public Dictionary<int, int> Oppose = new Dictionary<int, int>();
        public List<GovernmentOption> Viable; public GovernmentOption Gov; public bool None => Viable.Count == 0;
        public bool Abandoned; public bool Cycle; public int Rounds;
        public Dictionary<int, string> Fate = new Dictionary<int, string>();
        public Dictionary<int, double> ScoreOfCabinet = new Dictionary<int, double>();
    }

    static List<Row> Rows()
    {
        var rows = new List<Row>();
        Row Make(string name, string group, CountryId id, Dictionary<string, int> held, List<RedLine> lines, List<InOrAgainst> rules)
        {
            var parties = PartySystems.For(id);
            return new Row { Name = name, Group = group, Country = id, Parties = parties, Seats = parties.Select(p => held.TryGetValue(p.Abbrev, out int s) ? s : 0).ToArray(), Lines = lines, Rules = rules };
        }
        Row FromReading(string name, string group, CountryId id, Dictionary<string, int> held, DeclarationReading reading)
        {
            var parties = PartySystems.For(id);
            return Make(name, group, id, held, reading.Lines(id, parties), reading.Platforms(id, parties));
        }
        var sv22 = new Dictionary<string, int> { { "S", 107 }, { "SD", 73 }, { "M", 68 }, { "V", 24 }, { "C", 24 }, { "KD", 19 }, { "MP", 18 }, { "L", 16 } };
        // the four elections of record (K273's prereg): the record's seats, and the game's count where a check holds one
        Row r = FromReading("RECORD Sweden 2022 (record seats, 2022 declarations)", "record", CountryId.Sweden, sv22, DeclarationReading.OfVintage(ElectionVintage.Sweden2022));
        r.Expected = r.Mask("M", "KD", "L"); r.ExpectedSupportMember = r.Parties.ToList().FindIndex(p => p.Abbrev == "SD"); rows.Add(r);
        r = FromReading("RECORD Germany 2025 (record seats, polling day)", "record", CountryId.Germany, PartySystems.InitialSeats(CountryId.Germany, ElectionVintage.Germany2025), DeclarationReading.OfElection(CountryId.Germany, new DateTime(2025, 2, 23)));
        r.Expected = r.Mask("CDU", "CSU", "SPD"); rows.Add(r);
        r = FromReading("RECORD Poland 2023 (record seats, polling day)", "record", CountryId.Poland, new Dictionary<string, int> { { "PiS", 194 }, { "KO", 157 }, { "TD", 65 }, { "NL", 26 }, { "Konf", 18 } }, DeclarationReading.OfElection(CountryId.Poland, new DateTime(2023, 10, 15)));
        r.Expected = r.Mask("KO", "TD", "NL"); rows.Add(r);
        r = FromReading("RECORD Poland 2023 (the game's count, seed 777)", "record", CountryId.Poland, new Dictionary<string, int> { { "PiS", 193 }, { "KO", 158 }, { "TD", 67 }, { "NL", 26 }, { "Konf", 16 } }, DeclarationReading.OfElection(CountryId.Poland, new DateTime(2023, 10, 15)));
        r.Expected = r.Mask("KO", "TD", "NL"); rows.Add(r);
        r = FromReading("RECORD Italy 2022 Camera (record seats, polling day)", "record", CountryId.Italy, PartySystems.InitialSeats(CountryId.Italy, ElectionVintage.Italy2022), DeclarationReading.OfElection(CountryId.Italy, new DateTime(2022, 9, 25)));
        r.Expected = r.Mask("FdI", "Lega", "FI", "NM"); rows.Add(r);
        var even = new Dictionary<string, double>();
        foreach (string key in ItalianCount2022.Keys) { even[key] = (double)ItalianCount2022.CameraPublished2022Votes[Array.IndexOf(ItalianCount2022.Keys, key)] / ItalianCount2022.CameraPublished2022Valid; }
        double total = even.Values.Sum();
        var game = even.ToDictionary(kv => kv.Key, kv => kv.Value / total);
        Dictionary<string, int> countCamera = new Dictionary<string, int>(ItalianCount.Camera(game).Seats), countSenato = ItalianCount.SenatoSeated(game, WorldClock.ItalyFirstSitting2022);
        r = FromReading("RECORD Italy 2022 Camera (the game's count)", "record", CountryId.Italy, new Dictionary<string, int>(ItalianCount.Camera(game).Seats), DeclarationReading.OfElection(CountryId.Italy, new DateTime(2022, 9, 25)));
        r.Expected = r.Mask("FdI", "Lega", "FI", "NM"); r.Also = new[] { Aligned(r.Parties, countSenato) }; rows.Add(r);
        r = FromReading("RECORD Italy 2022 Senato (the game's count; mandate 2)", "record", CountryId.Italy, ItalianCount.SenatoSeated(game, WorldClock.ItalyFirstSitting2022), DeclarationReading.OfVintage(ElectionVintage.Italy2022));
        r.Expected = r.Mask("FdI", "Lega", "FI", "NM"); r.Also = new[] { Aligned(r.Parties, countCamera) }; rows.Add(r);

        // FormationSweepDiagnostic's rows, read as it reads them
        IReadOnlyList<PoliticalParty> sweden = PartySystems.For(CountryId.Sweden);
        var seed = new Dictionary<string, int>(); foreach (var p in sweden) { seed[p.Abbrev] = p.SeedSeats; }
        var chambers = new (string Name, Dictionary<string, int> Seats)[]
        {
            ("seed", seed), ("2022", sv22),
            ("year-32", new Dictionary<string, int> { { "S", 94 }, { "SD", 63 }, { "M", 70 }, { "V", 27 }, { "C", 24 }, { "KD", 27 }, { "MP", 24 }, { "L", 20 } }),
        };
        var edges = new SortedSet<DateTime> { new DateTime(2022, 9, 11), new DateTime(2026, 1, 18), new DateTime(2026, 9, 13) };
        foreach (DeclaredRedLines.DatedFact fact in DeclaredRedLines.SwedenTimeline)
        {
            if (fact.From.Year >= 2022 && fact.From.Year <= 2027) { edges.Add(fact.From.Date); }
            if (fact.Until != DateTime.MaxValue && fact.Until.Year >= 2022 && fact.Until.Year <= 2027) { edges.Add(fact.Until.Date); }
        }
        foreach (var (chamberName, held) in chambers)
        {
            foreach (ElectionVintage vintage in new[] { ElectionVintage.Seated, ElectionVintage.Sweden2018, ElectionVintage.Sweden2022, ElectionVintage.Sweden2026 })
            {
                List<RedLine> lines = DeclaredRedLines.For(CountryId.Sweden, sweden, vintage);
                rows.Add(Make("Sweden " + chamberName + " " + vintage + " with rules", "sweep", CountryId.Sweden, held, lines, DeclaredRedLines.InOrAgainstFor(CountryId.Sweden, sweden, vintage)));
                rows.Add(Make("Sweden " + chamberName + " " + vintage + " without rules", "sweep", CountryId.Sweden, held, lines, new List<InOrAgainst>()));
            }
            foreach (DateTime d in edges)
            {
                List<RedLine> lines = DeclaredRedLines.ForDate(CountryId.Sweden, sweden, d);
                string day = d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                rows.Add(Make("Sweden " + chamberName + " dated " + day + " with rules", "sweep", CountryId.Sweden, held, lines, DeclaredRedLines.InOrAgainstAt(CountryId.Sweden, sweden, d)));
                rows.Add(Make("Sweden " + chamberName + " dated " + day + " without rules", "sweep", CountryId.Sweden, held, lines, new List<InOrAgainst>()));
            }
        }
        foreach (CountryId id in (CountryId[])Enum.GetValues(typeof(CountryId)))
        {
            if (id == CountryId.Sweden) { continue; }
            IReadOnlyList<PoliticalParty> parties = PartySystems.For(id);
            if (parties == null || parties.Count == 0) { continue; }
            var held = new Dictionary<string, int>(); foreach (var p in parties) { held[p.Abbrev] = p.SeedSeats; }
            if (held.Values.Count(s => s != 0) > 16) { continue; }
            rows.Add(Make(id + " seated", "sweep", id, held, DeclaredRedLines.For(id, parties), DeclaredRedLines.InOrAgainstFor(id, parties)));
        }
        // every chamber of record by its own vintage's declarations (K300: the chambers the seated start reads, beyond the sweep's)
        foreach (ElectionVintage v in Enum.GetValues(typeof(ElectionVintage)))
        {
            if (v == ElectionVintage.Seated) { continue; }
            foreach (CountryId id in Enum.GetValues(typeof(CountryId)))
            {
                if (!v.ToString().StartsWith(id == CountryId.USA ? "Usa" : id.ToString())) { continue; }
                Dictionary<string, int> table;
                try { table = PartySystems.InitialSeats(id, v); } catch (Exception) { continue; }
                if (table == null || table.Values.Sum() == 0 || table.Values.Count(s => s != 0) > 16) { continue; }
                rows.Add(FromReading(id + " " + v + " (chamber of record, its vintage's declarations)", "vintage", id, table, DeclarationReading.OfVintage(v)));
            }
        }
        // seed 777's in-run Polish chambers (dumpk273.log; K289's review H1)
        rows.Add(FromReading("DUMP Poland 2055-10-31 (seed 777)", "dump", CountryId.Poland, new Dictionary<string, int> { { "KO", 91 }, { "NL", 34 }, { "PiS", 218 }, { "TD", 117 } }, DeclarationReading.OfElection(CountryId.Poland, new DateTime(2055, 10, 31))));
        rows.Add(FromReading("DUMP Poland 2059-11-02 (seed 777)", "dump", CountryId.Poland, new Dictionary<string, int> { { "KO", 87 }, { "Konf", 4 }, { "NL", 39 }, { "PiS", 216 }, { "TD", 114 } }, DeclarationReading.OfElection(CountryId.Poland, new DateTime(2059, 11, 2))));
        return rows;
    }

    static int Seats(int[] seats, int mask) => CoalitionMath.Seats(seats, mask);

    /// <summary>A chamber by key, in the roster's order.</summary>
    static int[] Aligned(IReadOnlyList<PoliticalParty> parties, IReadOnlyDictionary<string, int> chamber) => parties.Select(p => chamber.TryGetValue(p.Abbrev, out int s) ? s : 0).ToArray();

    /// <summary>K300's review L4, re-stated (main's runtime has no such routine): a majority of its own in every other chamber given; true where none is.</summary>
    static bool OwnMajorityIn(int[][] chambers, int cabinet)
    {
        if (chambers == null) { return true; }
        foreach (int[] other in chambers) { if (other == null || CoalitionMath.Seats(other, cabinet) < CoalitionMath.Majority(other)) { return false; } }
        return true;
    }

    /// <summary>The runtime's own Form, with the other chambers where its Form takes them (the branch's) - main's has no such parameter, and needs none at w = 0.</summary>
    static CoalitionResult RuntimeForm(Row row, bool negative)
    {
        var args = new List<object> { row.Seats, GovernmentFormation.Compatibility(row.Parties), row.Lines, negative, row.Rules, ChamberRules.JointMasks(row.Country, row.Parties, row.Seats) };
        System.Reflection.MethodInfo form = typeof(CoalitionFormation).GetMethods().Where(m => m.Name == "Form").OrderByDescending(m => m.GetParameters().Length).First();
        if (form.GetParameters().Length == 7) { args.Add(row.Also); }
        return (CoalitionResult)form.Invoke(null, args.ToArray());
    }

    /// <summary>CoalitionFormation.Form's procedure, re-stated with the variant's hooks (no hook: main's arithmetic, stored double for stored double).</summary>
    static Result Form(Row row, bool negative, Spec s) =>
        Core(row.Seats, GovernmentFormation.Compatibility(row.Parties), ChamberRules.JointMasks(row.Country, row.Parties, row.Seats), row.Lines, row.Rules, negative, s, row.Names, row.Also);

    /// <summary>The formation on raw inputs, at a support fraction <paramref name="f"/> and a majority bonus <paramref name="w"/> - for Program.cs's modes.</summary>
    public static Result FormRaw(int[] seats, double[,] compat, List<RedLine> lines, bool negative, List<InOrAgainst> rules, int[] joint, double f, double w = 0.0, int[][] also = null) =>
        Core(seats, compat, joint, lines, rules, negative, new Spec { Fr = f, W = w }, m => m.ToString(CultureInfo.InvariantCulture), also);

    /// <summary>MAIN'S BASE SCORE, re-stated operand for operand from CoalitionFormation.Prepare as main has it (cohesion, seat strength, pivotality;
    /// PowerOf's sum in party order) - bit-identical to main's stored double (`validate` on main proves it), and independent of any runtime change.</summary>
    public static double MainBase(CoalitionFormation.Chamber ch, int c, double[,] compat)
    {
        double power = 0.0;
        for (int p = 0; p < ch.N; p++) { if ((c & (1 << p)) != 0) { power += ch.Power[p]; } }
        return CoalitionFormation.WeightCohesion * CoalitionFormation.Cohesion(c, ch.N, compat)
            + CoalitionFormation.WeightSeatStrength * 100.0 * CoalitionMath.Seats(ch.Seats, c) / Math.Max(1, ch.TotalSeats)
            + CoalitionFormation.WeightPower * 100.0 * power;
    }

    static Result Core(int[] seats, double[,] compat, int[] joint, List<RedLine> lines, List<InOrAgainst> rules, bool negative, Spec s, Func<int, string> names, int[][] also = null)
    {
        CoalitionFormation.Chamber ch = CoalitionFormation.Prepare(seats, compat, lines, negative, rules, joint);   // admissibility, passability, power - never its base score (MainBase)
        int n = ch.N, total = ch.TotalSeats, majority = ch.Majority;
        var derived = new Dictionary<int, int>();
        foreach (int c in ch.Admissible) { derived[c] = CoalitionFormation.Evaluate(ch, c).SupportMask; }   // the derived supporters: no score decides them
        // (A): the seats the cabinet needs for its investiture rule's line - a majority FOR (positive), or so few left that they cannot make a majority AGAINST (negative)
        int Need(int c) => Math.Max(0, (negative ? total - majority + 1 : majority) - Seats(seats, c));
        double ScoreOf(int c, int support)
        {
            double x = MainBase(ch, c, compat);
            if (s.Fr != 0.0)
            {
                int counted = Seats(seats, support);
                if (s.Cap) { counted = Math.Min(counted, Need(c)); }
                x = x + s.Fr * CoalitionFormation.WeightSeatStrength * 100.0 * counted / Math.Max(1, total);
            }
            if (s.W != 0.0 && Seats(seats, c) >= majority && OwnMajorityIn(also, c)) { x = x + s.W * CoalitionFormation.WeightSeatStrength * 100.0; }   // (B); L4: in every chamber it answers to
            return x;
        }
        var bestOwn = new double[n];
        for (int p = 0; p < n; p++) { bestOwn[p] = double.NegativeInfinity; }
        foreach (int c in ch.Admissible)
        {
            if (!ch.PassesOnLines[c]) { continue; }
            double sc = ScoreOf(c, derived[c]);
            for (int p = 0; p < n; p++) { if ((c & (1 << p)) != 0 && sc > bestOwn[p]) { bestOwn[p] = sc; } }
        }
        var bestPay = new double[n];   // (C) payoff reading: the best partnered passable cabinet a party sits in, by its own payoff
        for (int p = 0; p < n; p++) { bestPay[p] = double.NegativeInfinity; }
        if (s.PosInv == 2)
        {
            foreach (int c in ch.Admissible)
            {
                if (!ch.PassesOnLines[c] || (c & (c - 1)) == 0) { continue; }
                var option = new GovernmentOption(c, 0, CoalitionOutcomeKind.MinorityGovernment, Seats(seats, c), Seats(seats, c), 0, 0.0, 0.0);
                for (int p = 0; p < n; p++) { if ((c & (1 << p)) != 0) { bestPay[p] = Math.Max(bestPay[p], CoalitionFormation.Payoff(p, option, seats, compat, n)); } }
            }
        }

        var result = new Result { TotalSeats = total, Majority = majority };
        var viable = new List<GovernmentOption>();
        foreach (int c in ch.Admissible)
        {
            int support = derived[c];
            double score = ScoreOf(c, support);
            int notFor = 0;
            if (s.PosInv != 0 && !negative)
            {
                for (int p = 0; p < n; p++)
                {
                    if ((support & (1 << p)) == 0) { continue; }
                    if (s.PosInv == 1 ? bestOwn[p] > score : bestPay[p] > CoalitionFormation.DefectionMargin) { notFor |= 1 << p; }
                }
                if (notFor != 0) { support = CoalitionFormation.JointAlike(support & ~notFor, c, joint, seats); score = ScoreOf(c, support); }
            }
            int red = CoalitionFormation.RedLinedMask(c, n, lines, rules);
            int oppose = 0;
            for (int p = 0; p < n; p++)
            {
                if ((c & (1 << p)) != 0 || (support & (1 << p)) != 0) { continue; }
                if ((red & (1 << p)) != 0 || bestOwn[p] > score || (notFor & (1 << p)) != 0) { oppose |= 1 << p; }
            }
            if (joint != null)
            {
                foreach (int m in joint)
                {
                    if ((c & m) != 0) { continue; }
                    int larger = -1;
                    for (int p = 0; p < n; p++) { if ((m & (1 << p)) != 0 && (larger < 0 || seats[p] > seats[larger])) { larger = p; } }
                    if (larger < 0) { continue; }
                    for (int smaller = 0; smaller < n; smaller++)
                    {
                        if (smaller == larger || (m & (1 << smaller)) == 0) { continue; }
                        if ((oppose & (1 << larger)) != 0) { oppose |= 1 << smaller; } else { oppose &= ~(1 << smaller); }
                    }
                }
            }
            int cab = Seats(seats, c), supported = cab + Seats(seats, support), opposed = Seats(seats, oppose);
            bool wins = supported >= majority || (negative && opposed < majority);
            var kind = cab >= majority ? CoalitionOutcomeKind.MajorityCoalition : support != 0 && supported >= majority ? CoalitionOutcomeKind.ConfidenceAndSupply : CoalitionOutcomeKind.MinorityGovernment;
            result.ScoreOfCabinet[c] = score;
            result.Fate[c] = wins ? F("wins its vote ({0} for, {1} against) at {2:0.000}", supported, opposed, score)
                : F("loses its vote ({0} for, {1} against, majority {2}){3}", supported, opposed, majority, notFor != 0 ? " - not voting for: " + names(notFor) : "");
            result.Oppose[c] = oppose;
            if (wins) { viable.Add(new GovernmentOption(c, support, kind, cab, supported, opposed, CoalitionFormation.Cohesion(c, n, compat), score)); }
        }
        int round = 0;
        for (; round < 32; round++)
        {
            var kept = viable.Where(g => WouldHold(g, viable, seats, compat, n)).ToList();
            if (kept.Count == 0) { result.Abandoned = viable.Count > 0; break; }
            if (kept.Count == viable.Count) { break; }
            foreach (var g in viable) { if (!kept.Contains(g)) { result.Fate[g.Cabinet] += F(", killed in defection round {0}", round + 1); } }
            viable = kept;
        }
        result.Cycle = round == 32;
        result.Rounds = round + 1;
        viable.Sort((a, b) => b.Score.CompareTo(a.Score));
        result.Viable = viable;
        result.Gov = viable.Count > 0 ? viable[0] : default;
        return result;
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

    static string Sig(Row row, Result r) => r.None ? "NO GOVERNMENT" : row.Names(r.Gov.Cabinet) + (r.Gov.Support != 0 ? " on " + row.Names(r.Gov.Support) : "");
    static bool CountryRule(Row row) => ChamberRules.UsesNegativeParliamentarism(row.Country);
    static bool Passes(Row row, Result r) => !r.None && r.Gov.Cabinet == row.Expected && (row.ExpectedSupportMember < 0 || (r.Gov.Support & (1 << row.ExpectedSupportMember)) != 0);

    sealed class Tally { public int RecordPass, Moves, MovesOther, NoGov, Cycles, SwedenOtherMoves; public List<string> RecordLine = new List<string>(), SwedenRecordLike = new List<string>(); }

    /// <summary>The variant against today on every row, both rules; prints the rows when <paramref name="print"/>.</summary>
    static Tally Measure(List<Row> rows, Spec v, bool print)
    {
        var t = new Tally();
        var today = Spec.Today;
        if (print) { Console.WriteLine(F("##### {0}", v.Label)); }
        foreach (Row row in rows)
        {
            bool own = CountryRule(row);
            foreach (bool negative in new[] { own, !own })
            {
                Result a = Form(row, negative, today), b = Form(row, negative, v);
                string sa = Sig(row, a), sb = Sig(row, b);
                bool moved = sa != sb, ownRule = negative == own;
                string verdict = row.Expected != 0 && ownRule ? (Passes(row, b) ? " [RECORD PASS]" : " [RECORD MISS]") : "";
                if (row.Expected != 0 && ownRule) { if (Passes(row, b)) { t.RecordPass++; } t.RecordLine.Add((Passes(row, b) ? "PASS " : "MISS ") + row.Name + " -> " + sb); }
                if (ownRule && row.Group == "sweep" && row.Name.StartsWith("Sweden 2022 ") && (row.Name.Contains("dated") || row.Name.Contains("Sweden2022")))
                {
                    t.SwedenRecordLike.Add(F("{0}: today {1} -> {2}{3}", row.Name, sa, sb, moved ? "  MOVES" : ""));
                }
                if (!ownRule && moved && row.Group == "sweep" && row.Name.StartsWith("Sweden 2022 ") && (row.Name.Contains("dated") || row.Name.Contains("Sweden2022"))) { t.SwedenOtherMoves++; }
                if (moved) { if (ownRule) { t.Moves++; } else { t.MovesOther++; } }
                if (b.None && ownRule) { t.NoGov++; }
                if (b.Cycle) { t.Cycles++; }
                if (!print || (!ownRule && !moved)) { continue; }
                Console.WriteLine(F("   {0} {1} | {2}: today {3} | variant {4}{5}{6}{7}{8}", moved ? "MOVES" : "same ", row.Name, ownRule ? "country's rule" : "OTHER rule", sa, sb, verdict,
                    b.None ? (a.None ? " (no government today either)" : " (NO GOVERNMENT)") : "", b.Abandoned ? " (defection rounds abandoned - the score decides)" : "", b.Cycle ? " (CYCLE: 32 rounds)" : ""));
                if (moved)
                {
                    if (!a.None) { Console.WriteLine(F("        today's {0} (today {1:0.000}): under the variant it {2}", sa, a.Gov.Score, b.Fate.TryGetValue(a.Gov.Cabinet, out string fa) ? fa : "-")); }
                    if (!b.None)
                    {
                        int rank = a.Viable.FindIndex(g => g.Cabinet == b.Gov.Cabinet);
                        Console.WriteLine(F("        the variant's {0} ({1:0.000}): today it {2}", sb, b.Gov.Score, rank >= 0 ? F("ranked {0} of {1} at {2:0.000}", rank + 1, a.Viable.Count, a.Viable[rank].Score) : a.Fate.TryGetValue(b.Gov.Cabinet, out string fb) ? fb : "was not admissible"));
                    }
                }
            }
        }
        if (print)
        {
            Console.WriteLine(F("   RECORDS: {0} of 7 pass - {1}", t.RecordPass, string.Join("; ", t.RecordLine)));
            Console.WriteLine("   SWEDEN 2022 RECORD-LIKE ROWS (the sweep's 2022 chamber (S 107, SD 73, M 68) under 2022's declarations and every dated edge, the Riksdag's rule):");
            foreach (string l in t.SwedenRecordLike) { Console.WriteLine("      " + l); }
            Console.WriteLine(F("   MOVES against today: {0} rows under the country's rule, {1} under the other; NO GOVERNMENT under the country's rule: {2}; cycles: {3}", t.Moves, t.MovesOther, t.NoGov, t.Cycles));
        }
        return t;
    }

    /// <summary>The runtime's own rule, by reflection - 0 where the constant is absent (main has neither; K300 removes K273's).</summary>
    public static double BuiltFraction => Constant("SupportWeight");
    public static double BuiltBonus => Constant("MajorityBonusWeight");
    static double Constant(string name) { var f = typeof(CoalitionFormation).GetField(name); return f != null ? Convert.ToDouble(f.GetValue(null), CultureInfo.InvariantCulture) : 0.0; }

    public static int Run(string[] args)
    {
        if (args.Length > 0 && (args[0] == "dump-check" || args[0] == "replay" || args[0] == "sejm-trace")) { return DumpCheck.Run(args); }
        string mode = args.Length > 0 ? args[0] : "validate";
        List<Row> rows = Rows();
        if (mode == "validate")
        {
            // the generic formation at the runtime's own rule (reflection: the support fraction K273 built, the majority bonus K300 builds; main has neither),
            // against the runtime's own Form
            var spec = new Spec { Label = "validate", Fr = BuiltFraction, W = BuiltBonus };
            int same = 0, diff = 0;
            foreach (Row row in rows)
            {
                foreach (bool negative in new[] { true, false })
                {
                    Result g = Form(row, negative, spec);
                    CoalitionResult shipped = RuntimeForm(row, negative);
                    bool ok = shipped.Viable.Count == g.Viable.Count && (g.None ? shipped.Outcome == CoalitionOutcomeKind.NewElection : shipped.Outcome == g.Gov.Kind);
                    for (int i = 0; ok && i < g.Viable.Count; i++)
                    {
                        GovernmentOption x = shipped.Viable[i], y = g.Viable[i];
                        ok = x.Cabinet == y.Cabinet && x.Support == y.Support && x.Kind == y.Kind && x.CabinetSeats == y.CabinetSeats && x.SupportedSeats == y.SupportedSeats
                            && x.OpposedSeats == y.OpposedSeats && x.Score.Equals(y.Score) && x.Cohesion.Equals(y.Cohesion);
                    }
                    if (ok) { same++; } else { diff++; Console.WriteLine(F("   DIFFERENT {0} | negative {1}: shipped {2} viable, generic {3}", row.Name, negative, shipped.Viable.Count, g.Viable.Count)); }
                }
            }
            Console.WriteLine(F("VALIDATE (the runtime's support fraction {0}, majority bonus {1}): {2} of {3} formations IDENTICAL to the runtime's CoalitionFormation.Form - every viable option, its seats and its score bits",
                BuiltFraction.ToString("0.00", CultureInfo.InvariantCulture), BuiltBonus.ToString("0.00", CultureInfo.InvariantCulture), same, same + diff));
            return diff == 0 ? 0 : 1;
        }
        if (mode == "support-capped")
        {
            Measure(rows, new Spec { Label = "(A) support-capped, f = 0.30 (supporters counted up to the seats the cabinet needs for its line)", Fr = 0.30, Cap = true }, true);
            Measure(rows, new Spec { Label = "K273 as built for comparison: uncapped, f = 0.30", Fr = 0.30 }, true);
            Console.WriteLine("(A) over f (country's rule): f, records passing, rows moving against today (country's rule / other), no government");
            for (int k = 1; k <= 19; k++) { double fr = k * 0.05; Tally t = Measure(rows, new Spec { Fr = fr, Cap = true }, false); Console.WriteLine(F("   f={0:0.00}: records {1}/7, moves {2}/{3}, no government {4}{5}", fr, t.RecordPass, t.Moves, t.MovesOther, t.NoGov, t.SwedenRecordLike.Any(l => l.EndsWith("MOVES")) ? ", a Sweden 2022 row MOVES" : "")); }
            return 0;
        }
        if (mode == "bonus-at")
        {
            // K311: the rule at one w (Elias ruled 0.25) against main, every row, both rules - the governments that move, the records, the Sweden 2022 rows
            double w = double.Parse(args[1], CultureInfo.InvariantCulture);
            Tally t = Measure(rows, new Spec { Label = F("(B) majority-bonus at w = {0:0.00}", w), W = w }, true);
            return t.RecordPass == 7 && !t.SwedenRecordLike.Any(l => l.EndsWith("MOVES")) ? 0 : 1;
        }
        if (mode == "majority-bonus")
        {
            // K300's prereg (Reviews/2026-10-29_k300_prereg.md): the grid 0..1.00 by 0.01; a w passes on (i) the 7 records and (ii) every Sweden 2022 sweep row
            // under the Riksdag's rule; the passing interval is the longest contiguous run (a tie: the lower); w is its midpoint rounded down to the grid
            Console.WriteLine("(B) over w (f = 0): w, records passing, Sweden 2022 sweep rows moving (the Riksdag's rule / the other rule), rows moving against main (country's rule / other), no government");
            var pass = new bool[101];
            for (int k = 0; k <= 100; k++)
            {
                double w = k / 100.0;
                Tally t = Measure(rows, new Spec { W = w }, false);
                int swMoves = t.SwedenRecordLike.Count(l => l.EndsWith("MOVES"));
                pass[k] = t.RecordPass == 7 && swMoves == 0;
                Console.WriteLine(F("   w={0:0.00}: records {1}/7, Sweden 2022 rows moving {2}/{3}, moves {4}/{5}, no government {6}{7}", w, t.RecordPass, swMoves, t.SwedenOtherMoves, t.Moves, t.MovesOther, t.NoGov, pass[k] ? "  PASS" : ""));
            }
            int bestLo = -1, bestHi = -1;
            for (int k = 0; k <= 100; k++)
            {
                if (!pass[k] || (k > 0 && pass[k - 1])) { continue; }
                int e = k; while (e < 100 && pass[e + 1]) { e++; }
                if (bestLo < 0 || e - k > bestHi - bestLo) { bestLo = k; bestHi = e; }
            }
            if (bestLo < 0) { Console.WriteLine("   NO w passes (i) and (ii): by the prereg, K300 is not built and returns to Elias"); return 1; }
            int chosen = (bestLo + bestHi) / 2;   // the midpoint, rounded down to the grid
            Console.WriteLine(F("   PASSING INTERVAL [{0:0.00}, {1:0.00}]{2}; the midpoint rounded down to the grid: w = {3:0.00}", bestLo / 100.0, bestHi / 100.0,
                bestHi == 100 ? " - its upper edge is the grid's end (no flip found below 1.00), so the midpoint is taken against the scale's end" : "", chosen / 100.0));
            Measure(rows, new Spec { Label = F("(B) majority-bonus, w = {0:0.00} (the prereg's choice)", chosen / 100.0), W = chosen / 100.0 }, true);
            return 0;
        }
        if (mode == "positive-investiture" || mode == "capped-positive")
        {
            bool capped = mode == "capped-positive";
            foreach (double fr in capped ? new[] { 0.30 } : new[] { 0.0, 0.30 })
            {
                foreach (int reading in new[] { 1, 2 })
                {
                    Measure(rows, new Spec
                    {
                        Label = F("{0}, f = {1:0.00}{2}, {3}", capped ? "(D) support-capped + positive investiture" : "(C) positive investiture", fr, capped ? " capped" : fr > 0 ? " uncapped (K273 as built)" : "",
                            reading == 1 ? "the SCORE reading (a supporter votes for unless it holds out on today's score test)" : "the PAYOFF reading (a supporter votes for unless a partnered passable cabinet pays it more)"),
                        Fr = fr, Cap = capped, PosInv = reading,
                    }, true);
                }
            }
            return 0;
        }
        Console.WriteLine("modes: validate | support-capped | majority-bonus | positive-investiture | capped-positive");
        return 2;
    }
}

#if VARIANTS_ONLY
static class VariantsEntry { static int Main(string[] args) => Variants.Run(args); }
#endif
