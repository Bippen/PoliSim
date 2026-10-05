using System.Collections.Generic;
using PoliSim.Data;

namespace PoliSim.Elections
{
    /// <summary>
    /// D-5 (a) — **the declared refusals, per country, as political FACTS with citations.**
    ///
    /// <para><b>Why they are separated from the derived ones.</b> A derived red line is the model's own
    /// inference from two parties' positions: it says *"these two are far enough apart that we infer they
    /// would not sit together"*. A declared line is something a party actually said. Mixing them would
    /// let an inference wear a citation's authority — and, worse, would hide which countries have their
    /// declarations on disk (`DeclaredRedLines.IsSourced`).</para>
    ///
    /// <para>⚠ <b>SOURCED where `DeclaredRedLines.IsSourced` says so, and dated where `DeclaredRedLines.HasTimeline` does</b> - Germany's and Poland's
    /// timeline summaries name their record files (`GermanySource`, `PolandSource`; since §705 and §776; the lines a record does not carry stay
    /// derived), and Sweden's two are named below. K-1 (2026-09-23): the
    /// live game reads Sweden's declarations as of the 2026 election, `ElectionsData/sweden/2026/coalition_declarations_2026.md`; the
    /// backtests that assert 2022's government pin <see cref="ElectionVintage.Sweden2022"/>,
    /// `ElectionsData/sweden/coalition_declarations_2022.md`. Where `IsSourced` is false, `For` returns the DERIVED
    /// lines alone, so a caller can say plainly that the government it formed was
    /// formed without that country's real declarations. A country's declared refusals can be the central
    /// political fact of its party system (Germany's were, before §705), and a formation run without them can produce a cabinet that
    /// country would never form — which is a limitation to state, not to paper over.</para>
    ///
    /// <para>⚠ This is the ONE definition of Sweden's declared lines. `CoalitionFilm` reads it rather than
    /// keeping a second copy: two surfaces disagreeing about which coalitions are possible would be worse
    /// than either being wrong alone, which is `CoalitionFilm`'s own stated argument for existing.</para>
    /// </summary>
    public static class DeclaredRedLines
    {
        public const string SwedenSource = "See ElectionsData/sweden/2026/coalition_declarations_2026.md";
        public const string SwedenSource2022 = "See ElectionsData/sweden/coalition_declarations_2022.md";

        /// <summary>Whether this country's DECLARED lines are sourced. False means `For` returns derived
        /// lines alone.</summary>
        public static bool IsSourced(CountryId country) => country == CountryId.Sweden || country == CountryId.Germany || country == CountryId.Poland;   // §705: Germany's declarations, dated; §776: Poland's (ruling E2)

        /// <summary>
        /// §705: whether an own-leader candidacy REFUSES another candidate's cabinet - Sweden's ruled pairing rule (K-1f). Not Germany's: a
        /// Kanzlerkandidatur names who leads a cabinet the party forms and draws no line - the SPD's candidate stood against Merz and the SPD entered
        /// his cabinet in 2025 (`ElectionsData/germany/coalition_declarations_2025.md` §3).
        /// </summary>
        public static bool CandidacyRefuses(CountryId country) => country == CountryId.Sweden;

        public const string GermanySource = "See ElectionsData/germany/coalition_declarations_2025.md";

        /// <summary>The derived lines plus any declared ones this country has on disk, in the party order
        /// of <paramref name="parties"/>, as of <paramref name="vintage"/> - the seated election's unless a
        /// backtest pins 2022's.</summary>
        private static List<RedLine> ForSourced(CountryId country, IReadOnlyList<PoliticalParty> parties, ElectionVintage vintage)
        {
            vintage = WorldClock.Resolve(country, vintage);   // PS-1 (§618): the seated chamber's election - every election reads its own date's declarations
            if (country == CountryId.Germany || country == CountryId.Poland) { return ForDateSourced(country, parties, WorldClock.ElectionDayOf(country, vintage)); }   // §705: a German vintage reads its timeline on its polling day; §776: a Polish one too
            var lrGen = new double[parties.Count];
            var galtan = new double[parties.Count];
            for (int p = 0; p < parties.Count; p++)
            {
                lrGen[p] = parties[p].LrGen;
                galtan[p] = parties[p].Galtan;
            }

            List<RedLine> lines = DerivedRedLines.From(lrGen, galtan);
            if (country != CountryId.Sweden) { return lines; }

            int s = IndexOf(parties, "SD"), c = IndexOf(parties, "C"), m = IndexOf(parties, "M");
            int kd = IndexOf(parties, "KD"), l = IndexOf(parties, "L"), v = IndexOf(parties, "V");

            if (vintage == ElectionVintage.Sweden2022)
            {
                AddCandidacyLines(lines, parties, vintage);
                if (s < 0) { return lines; }
                if (c >= 0)
                {
                    lines.Add(new RedLine(c, s, RedLineKind.Declared, blocksSupport: true,
                        basis: "DECLARED: Centerpartiet will not sit in or support a government dependent on SD - "
                               + "Loof, SVT Agenda 2017-05-14, verbatim; conduct 2022 (backed Andersson over Kristersson). " + SwedenSource2022));
                }

                const string NoSdMinisters = "DECLARED: promised in the 2022 campaign not to let SD sit in government, while "
                    + "accepting its support - Tidoavtalet 2022-10-14 (cabinet M+KD+L, SD outside with no ministerial post). " + SwedenSource2022;
                if (m >= 0) { lines.Add(new RedLine(m, s, RedLineKind.Declared, blocksSupport: false, basis: NoSdMinisters)); }
                if (kd >= 0) { lines.Add(new RedLine(kd, s, RedLineKind.Declared, blocksSupport: false, basis: NoSdMinisters)); }
                if (l >= 0) { lines.Add(new RedLine(l, s, RedLineKind.Declared, blocksSupport: false, basis: NoSdMinisters)); }
                return lines;
            }

            // THE 2026 ELECTION'S DECLARATIONS (K-1 part 2). Two lines, both Centerpartiet's; the 2022 no-SD-ministers
            // line is LIFTED for all three Tidö parties - M by the M-SD agreement of 2026-04-01 ([MSD-P1], whose own page does not
            // say SD will sit in government; that term is carried by the independent reports [MSD-I1]-[MSD-I4]), L by its
            // agreement of 2026-03-13 [L-P1] and its board's decision reported the same day [L-I1], KD by its own words as reported
            // 2026-09-08 [KD-I2] (secondary; KD's primary is a GAP) - so no line is written for them: an absent refusal is the fact.
            if (c >= 0 && s >= 0)
            {
                lines.Add(new RedLine(c, s, RedLineKind.Declared, blocksSupport: true,
                    basis: "DECLARED: Centerpartiet will not sit in or support a government that depends on SD or gives it influence - "
                           + "Thand Ringqvist's installation speech 2025-11-13 [C-P5], restated 2026-01-28 [C-P1], 2026-01-30 [C-I10], "
                           + "2026-08-11 [C-P2], 2026-09-08 [C-I1] and after the election 2026-09-14 [C-I6]. " + SwedenSource));
            }

            // THE ONE-WAY SHAPE (RedLine.OneWay, K-1). C refuses any cabinet that CONTAINS V - it will not sit in it, support it
            // or let it through ([C-I1] 2026-09-08, [C-I3] 2026-04-21, [C-P1] 2026-01-28) - and no fetched source that names a
            // mechanism has C refuse V as a mere supporter: TV4 2026-01-30 [C-I10] has that door "inte formellt stängd", and C
            // floated a pure S minority V would have to let through ([C-I8], secondary). Neither of the model's two symmetric
            // strengths says that - cabinet-blocking would let C prop up a cabinet with V in it, support-blocking would stop V
            // tolerating a cabinet C sits in - so the line runs from C to V only. V's own in-or-against demand ([V-P1]: it will
            // not support or let through a government it is not in) is a party's rule, not a pair's: since K-1f it is held by its
            // own shape (InOrAgainstFor below), which the formation reads beside these lines.
            if (c >= 0 && v >= 0)
            {
                lines.Add(new RedLine(c, v, RedLineKind.Declared, blocksSupport: true, oneWay: true,
                    basis: "DECLARED: Centerpartiet will not sit in, support or let through a cabinet that contains V - "
                           + "first found 2026-01-28 [C-P1], restated 2026-01-30 [C-I10], 2026-04-21 [C-I3] and 2026-09-08 [C-I1] (\"hellre till extraval\"), "
                           + "held after the election 2026-09-14 [C-I6] and 2026-09-18 [C-I8]; one way - no fetched source that names a mechanism has C refuse V's support. "
                           + SwedenSource));
            }

            AddCandidacyLines(lines, parties, vintage);

            // K-1g (ruled 2026-09-25): KD'S REFUSAL OF ANDERSSON - it will vote no to her as prime minister all the way to an extra election. On
            // K-1f's premise (a cabinet holding S is its candidate's) that is a one-way line from KD to S at the strength K-1h (ii) ruled for a
            // declared rival: KD votes against the cabinet, and never sits in it. Being support-blocking, the line also counts KD for a motion of
            // no confidence in a cabinet holding S (GovernmentFormation.RedLinedFrom), as M's candidacy line does - KD's words are about the
            // investiture; the motion's reading is the line's strength, stated (§639).
            int sIndex = IndexOf(parties, "S");
            if (kd >= 0 && sIndex >= 0) { lines.Add(new RedLine(kd, sIndex, RedLineKind.Declared, blocksSupport: true, oneWay: true, basis: KdRefusesAndersson)); }
            return lines;
        }

        /// <summary>K-1g: KD's refusal of Andersson, its basis - Busch's own words as SVT quotes them, the first report of the line beside them.</summary>
        private const string KdRefusesAndersson = "DECLARED: Kristdemokraterna will vote no to Magdalena Andersson as prime minister, all the way to an extra election - "
            + "Busch's own words 2026-09-02 [KD-I1] (\"Vi kommer att vara beredda att rösta nej till Magdalena Andersson ända fram till ett nyval\"), "
            + "first reported 2026-06-05 [KD-I3] (the newsroom's words, citing DI); the KD primary is a GAP. On K-1f's premise a cabinet holding S is "
            + "Andersson's, so the line runs from KD to S. " + SwedenSource;

        /// <summary>The basis prefix every candidacy line carries (K-1f). The parties said the candidacies; the refusal between two of them is the
        /// ruling's rule, so a surface that quotes what a party said reads this to tell the two apart (<see cref="IsCandidacy"/>).</summary>
        public const string CandidacyPrefix = "DECLARED CANDIDACY: ";

        /// <summary>Whether <paramref name="line"/> is one of the candidacy pair's lines: declared candidacies, a ruled refusal.</summary>
        public static bool IsCandidacy(RedLine line) =>
            line.Kind == RedLineKind.Declared && line.Basis != null && line.Basis.StartsWith(CandidacyPrefix, System.StringComparison.Ordinal);

        /// <summary>
        /// K-1f (ruled 2026-09-24): A DECLARED PRIME-MINISTERIAL CANDIDACY IS A CONSTRAINT - a party that declared ITS OWN LEADER as its candidate
        /// refuses any cabinet led by another party's candidate. The model carries no prime minister, so it is encoded under ONE premise, the
        /// builder's and not the ruling's words: a declared party sits only in a cabinet its own candidate leads, so a cabinet holding a rival
        /// declared party is that rival's. On that premise the rule is a pair of ONE-WAY lines between every two declared parties - neither sits
        /// in, supports or lets through a cabinet containing the other, and both may tolerate a third party's. The case the premise cannot
        /// represent is a cabinet holding S (or M) under a prime minister who is no party's declared candidate; the model has no such cabinet.
        /// <para><b>The strength is ruled</b> (K-1h (ii), 2026-09-25, §639): a declared rival candidacy means voting AGAINST the rival's cabinet -
        /// the one-way line's support-blocking strength. No fetched S or M page says whether its refusal covers sitting in, supporting or letting
        /// through (`coalition_declarations_2026.md`, K-1f); the cabinet-blocking reading's result is a row of `Formation2026Diagnostic`.</para>
        /// <para>Sourced and dated per vintage - declarations are dated, and every election reads its own date's (standing, K-1f). A party that
        /// named another party's leader, or named none, carries no candidacy: the ruling's case is a party's own leader. 2026: SD, KD, L and C
        /// named Kristersson or Andersson, V and MP named none. 2022: C, KD, L and MP named Kristersson or Andersson, SD and V are gaps (none
        /// found). Only S and M declared their own in either vintage. 2026: `coalition_declarations_2026.md`'s K-1f section; 2022:
        /// `coalition_declarations_2022.md`'s.</para>
        /// </summary>
        private static IReadOnlyList<(string Abbrev, string Candidate, string Basis)> CandidaciesSourced(CountryId country, ElectionVintage vintage)
        {
            if (country == CountryId.Germany) { return CandidaciesAtSourced(country, WorldClock.ElectionDayOf(country, WorldClock.Resolve(country, vintage))); }   // §705
            if (country != CountryId.Sweden) { return System.Array.Empty<(string, string, string)>(); }
            vintage = WorldClock.Resolve(country, vintage);
            if (vintage == ElectionVintage.Sweden2018) { return System.Array.Empty<(string, string, string)>(); }   // no 2018 declarations are sourced; no start seats that chamber
            if (vintage == ElectionVintage.Sweden2022)
            {
                return new[]
                {
                    ("S", "Magdalena Andersson", "S's own page of 2022-08-04 [S-P1] (capture 2022-08-13 [S-P1a]) - its leader and sitting prime minister, framed as leadership for Sweden; the candidacy wording is the press's. " + SwedenSource2022),
                    ("M", "Ulf Kristersson", "M's own page [M-P1] of 2022-03-26 (capture 2022-09-10 [M-P1a]): \"Som statsminister kommer Ulf Kristersson ...\". " + SwedenSource2022),
                };
            }
            return new[]
            {
                ("S", "Magdalena Andersson", "S's own pages: 2026-05-01 [S-P2], 2026-08-03 [S-P1] (\"I valet i september kommer jag att söka svenska folkets mandat för att bli Sveriges statsminister\"), 2026-08-09 [S-P3]. " + SwedenSource),
                ("M", "Ulf Kristersson", "M's own page of 2026-04-01 [MSD-P1] (\"Den enda som kan leda den är Ulf Kristersson.\"). " + SwedenSource),
            };
        }

        /// <summary>The parties with a declared own-leader candidacy, as indices into <paramref name="parties"/> - what a guard reads.</summary>
        public static List<int> CandidacyParties(CountryId country, IReadOnlyList<PoliticalParty> parties, ElectionVintage vintage = ElectionVintage.Seated)
        {
            var found = new List<int>();
            foreach ((string abbrev, string _, string _) in Candidacies(country, vintage))
            {
                int p = IndexOf(parties, abbrev);
                if (p >= 0) { found.Add(p); }
            }
            return found;
        }

        private static void AddCandidacyLines(List<RedLine> lines, IReadOnlyList<PoliticalParty> parties, ElectionVintage vintage)
        {
            IReadOnlyList<(string Abbrev, string Candidate, string Basis)> declared = Candidacies(CountryId.Sweden, vintage);
            for (int i = 0; i < declared.Count; i++)
            {
                int a = IndexOf(parties, declared[i].Abbrev);
                if (a < 0) { continue; }
                for (int j = 0; j < declared.Count; j++)
                {
                    int b = IndexOf(parties, declared[j].Abbrev);
                    if (j == i || b < 0) { continue; }
                    lines.Add(new RedLine(a, b, RedLineKind.Declared, blocksSupport: true, oneWay: true,
                        basis: CandidacyPrefix + declared[i].Abbrev + "'s own leader " + declared[i].Candidate + " is its prime-ministerial candidate ("
                               + declared[i].Basis + "); it refuses any cabinet led by another party's candidate - " + declared[j].Abbrev + "'s is "
                               + declared[j].Candidate + " (" + declared[j].Basis + ")."));
                }
            }
        }

        /// <summary>
        /// K-1f (ruled 2026-09-24): the parties with a declared IN-OR-AGAINST rule - they support no cabinet they are not in, and vote against
        /// every such cabinet (<see cref="InOrAgainst"/>). Sweden 2026: V, by its election platform as its congress decided it on 2026-04-18
        /// ([V-P1], the PDF of 2026-04-19: "Om våra röster behövs för att bilda regering så ska vi också ingå i den. Det betyder att vi inte
        /// kommer att stödja eller släppa fram en regering som vi inte ingår i."), restated 2026-08-25 as the party's unchanged line when SVT asked
        /// after Dadgostar had told TV4 that V would not topple Andersson ([C-I9], secondary), and held after the election on 2026-09-14 ([V-I4]).
        /// <para>⚠ The source's condition - "if our votes are needed to form a government" - is not carried. On ONE investiture vote the two forms
        /// agree: under the negative rule a party voting against a cabinet it is not in changes that vote only where its votes are needed. In the
        /// formation as a whole they can differ only in who is listed as carrying the cabinet: on the K-1f reviews' replica, over 20,000 perturbed
        /// 2026 chambers, the two chose the same cabinet and outcome kind every time, and in 111 the conditional form listed V as a supporter
        /// where the unconditional one did not (none on the seated or year-32 chambers). The ruling's words are unconditional ("V refuses to
        /// support a cabinet it is not in"), and that is what is wired.</para>
        /// <para>K-1g (ruled 2026-09-25): MP's and SD's 2026 rules are wired beside V's. MP's votes against, as V's does - its spokesperson's
        /// condition that MP votes no to a prime minister unless it sits in the government (secondary only: [MP-I4], [MP-I1], [MP-I2]). SD's is
        /// wired on the BUILDER'S READING, open for Elias (K-1i): its words refuse the support role - "either a government party or an opposition
        /// party" ([SD-P2], primary), "no middle position like the one we have today" ([L-C2]) - and do not say, as V's and MP's do, that SD votes
        /// every other cabinet down, so <see cref="InOrAgainst.VotesAgainst"/> is false; the same [L-C2] quotation says "full opposition", which
        /// reads the other way. The alternative is a row of `Formation2026Diagnostic`.</para>
        /// <para>MP's 2022 rule is NOT wired, measured first (K-1g): Stenevi's words are read right ([MP-I1] 2022: "Vi röstar nej till de
        /// regeringar som vi inte ingår i"), and the model cannot hold them - wired, the 2022 chamber forms S alone ahead of M+KD+L carried by SD
        /// (`Formation2026Diagnostic`'s last row), because a formation choosing between two cabinets that both pass ranks them by its one shared
        /// score, on which a single party's cohesion is whole, and KD and L hold out for their own cabinet only where it outscores S alone. In
        /// the Riksdag the Speaker put Kristersson first, carried by a majority for him. 2022 carries no V rule (none was found).</para>
        /// </summary>
        private static List<InOrAgainst> InOrAgainstForSourced(CountryId country, IReadOnlyList<PoliticalParty> parties, ElectionVintage vintage)
        {
            var rules = new List<InOrAgainst>();
            vintage = WorldClock.Resolve(country, vintage);
            if (country != CountryId.Sweden || vintage != ElectionVintage.Sweden2026) { return rules; }   // 2026's declarations (K-1f, K-1g); 2022's set carries none
            int v = IndexOf(parties, "V");
            if (v >= 0) { rules.Add(new InOrAgainst(v, VInOrAgainst)); }
            int mp = IndexOf(parties, "MP");
            if (mp >= 0) { rules.Add(new InOrAgainst(mp, MpInOrAgainst)); }
            int sd = IndexOf(parties, "SD");
            if (sd >= 0) { rules.Add(new InOrAgainst(sd, SdNoSupportRole, votesAgainst: false)); }
            return rules;
        }

        private const string VInOrAgainst = "DECLARED: Vänsterpartiet will not support or let through a government it is not in, where its votes are needed - "
            + "its election platform as decided by the congress 2026-04-18 ([V-P1], dated 2026-04-19; [V-I1]), restated as unchanged 2026-08-25 ([C-I9]), "
            + "held after the election 2026-09-14 ([V-I4]). " + SwedenSource;

        /// <summary>K-1g: MP's rule. MP's own record is a GAP, so its date is a PRESS REPORT's - a stated deviation from §621's first rule, put to Elias
        /// (§639): 2026-04-04, TT's report, the first and only source that carries the half that is wired (the vote against); Sveriges Radio's of
        /// 2026-08-10 carries the demand to sit in government and not the vote.</summary>
        private const string MpInOrAgainst = "DECLARED: Miljöpartiet will vote no to a prime minister unless it sits in the government - Helldén's own words to Sveriges Radio "
            + "2026-08-10 ([MP-I1]: \"Vi ska sitta i nästa regering\"; the condition as [MP-I2] reports it: \"Partiet ställer samtidigt ett villkor om att sitta i regering\"), "
            + "first reported 2026-04-04 ([MP-I4], TT's paraphrase: \"röstar nej utan regeringsplats\"); dated by the leader's own words, ruled 2026-09-29 (§652) - MP's one "
            + "saved publication ([MP-P1], 2026-08-12) is later and does not state the condition. " + SwedenSource;

        /// <summary>K-1g: SD's rule - the support role refused, the vote against not declared.</summary>
        private const string SdNoSupportRole = "DECLARED: Sverigedemokraterna will be either a government party or an opposition party, never a support party again - "
            + "Åkesson's post 2025-10-10 as SvD quotes it ([L-C2]: \"antingen att sitta i regering eller i full opposition. Något mellanläge, motsvarande det vi har idag, "
            + "kommer inte att vara aktuellt för oss\"), restated 2026-04-01 ([MSD-I4]) and in the 2026 platform ([SD-P2], primary: \"Efter nästa val är Sverigedemokraterna "
            + "antingen ett regeringsparti eller ett oppositionsparti.\"). Scoped by its own words to the formation after the 2026 election (\"Efter nästa val\"). "
            + "Read as refusing the support role without voting every other cabinet down - the builder's reading, open for Elias (K-1i); \"full opposition\" in the same quotation reads the other way. " + SwedenSource;

        // -----------------------------------------------------------------------------------------------------------------------------
        // §621 (ruled 2026-09-25): DECLARATIONS BY DATE. Three rules - a declaration is dated by the party's own record, never by press
        // reporting; a document is dated by the decision it records, not its file date; a declaration stands until a later dated one
        // replaces it. Applied to Sweden: the 2022 S/M candidacy pair carries into 2026 until the dated 2026 declarations replace it
        // (M 2026-04-01 [MSD-P1], S 2026-05-01 [S-P2]); KD's no-SD-ministers line lifts on 2026-09-08 (KD's own words [KD-I2]); V's
        // in-or-against rule takes effect 2026-04-18 (the congress decision [V-P1] records); C's one-way line to V starts 2026-01-30
        // (C's own publication [C-P1]). The vintage API above stays for the backtests, pinned by name; `ForDate` is the timeline, and
        // `DeclarationDatesDiagnostic` proves the timeline's 2022-09-11 equals `For(Sweden2022)` and its 2026-09-13 `For(Sweden2026)`. K-1g (§639)
        // adds three: SD's refusal of the support role 2025-10-10, MP's in-or-against rule 2026-08-10 (Helldén's own words, ruled §652), KD → S 2026-09-02.
        // Its readers: an election's formation reads its own polling day's facts (`DeclarationReading.OfElection`, through `ForSourced` for a vintage), a
        // mid-term round the lines standing that day (`DeclarationReading.MidTerm`), and the run-up's declarations page what stands today
        // (`DeclaredRedLines.StandingOn`, D-PS's, §657). §644 measures a mid-term round on it (`GovernmentFormation.ViewOfSitting` with a date, read by
        // `AiMotionReachDiagnostic`) for Elias's ruling PS-3i-2c.
        // -----------------------------------------------------------------------------------------------------------------------------

        /// <summary>One dated declaration: a pair line, a candidacy, an in-or-against rule or SD's refusal of the support role, standing from <see cref="From"/> until <see cref="Until"/> (exclusive; MaxValue while it stands).</summary>
        public readonly struct DatedFact
        {
            public readonly string Party;
            /// <summary>The other party of a pair line; null for a candidacy, an in-or-against rule or a refusal of the support role.</summary>
            public readonly string Other;
            public readonly FactKind Kind;
            public readonly bool BlocksSupport;
            public readonly bool OneWay;
            public readonly string Candidate;
            public readonly System.DateTime From;
            public readonly System.DateTime Until;
            public readonly string Basis;

            public DatedFact(string party, string other, FactKind kind, bool blocksSupport, bool oneWay, string candidate, System.DateTime from, System.DateTime until, string basis)
            {
                Party = party; Other = other; Kind = kind; BlocksSupport = blocksSupport; OneWay = oneWay; Candidate = candidate; From = from; Until = until; Basis = basis;
            }

            public bool StandsOn(System.DateTime date) => date.Date >= From && date.Date < Until;
        }

        /// <summary>K-1g: <see cref="NoSupportRole"/> is SD's form of the in-or-against rule - the support role refused, the vote against not declared.</summary>
        public enum FactKind { PairLine, Candidacy, InOrAgainst, NoSupportRole }

        private static readonly System.DateTime Open = System.DateTime.MaxValue;
        private static System.DateTime D(int y, int m, int d) => new System.DateTime(y, m, d);

        /// <summary>Sweden's declarations as dated facts (records: `SwedenSource2022`, `SwedenSource`) - dated by §621's rules, or, where a fact's comment
        /// says so, by §652's ruling or by Elias's ruling F2 (a leader's words quoted verbatim on the broadcaster's own page, dated by that page); each
        /// fact standing until the dated one that replaced it.</summary>
        public static IReadOnlyList<DatedFact> SwedenTimeline { get; } = new[]
        {
            // C ↔ SD, support-blocking: Lööf's statement, then Thand Ringqvist's installation speech restates it - the same shape, a new basis.
            new DatedFact("C", "SD", FactKind.PairLine, true, false, null, D(2017, 5, 14), D(2025, 11, 13),
                "DECLARED: Centerpartiet will not sit in or support a government dependent on SD - Loof, SVT Agenda 2017-05-14, verbatim; conduct 2022 (backed Andersson over Kristersson). " + SwedenSource2022),
            new DatedFact("C", "SD", FactKind.PairLine, true, false, null, D(2025, 11, 13), Open,
                "DECLARED: Centerpartiet will not sit in or support a government that depends on SD or gives it influence - Thand Ringqvist's installation speech 2025-11-13 [C-P5], restated 2026-01-30 [C-P1] (C's own publication; reported 2026-01-28), 2026-01-30 [C-I10], 2026-08-11 [C-P2], 2026-09-08 [C-I1] and after the election 2026-09-14 [C-I6]. " + SwedenSource),
            // M / KD / L ↔ SD, cabinet-blocking: promised in the 2022 campaign (no own-record date on disk - held from the campaign's first day, the model's window), executed by Tidö 2022-10-14; lifted by each party's own dated record.
            new DatedFact("M", "SD", FactKind.PairLine, false, false, null, new CampaignCalendar(D(2022, 9, 11)).CampaignStart, D(2026, 4, 1),
                "DECLARED: promised in the 2022 campaign not to let SD sit in government, while accepting its support - Tidoavtalet 2022-10-14; lifted by the M-SD agreement of 2026-04-01 [MSD-P1]. " + SwedenSource2022),
            new DatedFact("KD", "SD", FactKind.PairLine, false, false, null, new CampaignCalendar(D(2022, 9, 11)).CampaignStart, D(2026, 9, 8),
                "DECLARED: promised in the 2022 campaign not to let SD sit in government, while accepting its support - Tidoavtalet 2022-10-14; lifted by KD's own words 2026-09-08 [KD-I2] (ruled §621: the party's own record, 8 September). " + SwedenSource2022),
            new DatedFact("L", "SD", FactKind.PairLine, false, false, null, new CampaignCalendar(D(2022, 9, 11)).CampaignStart, D(2026, 3, 13),
                "DECLARED: promised in the 2022 campaign not to let SD sit in government, while accepting its support - Tidoavtalet 2022-10-14; lifted by L's agreement of 2026-03-13 [L-P1]. " + SwedenSource2022),
            // C → V, one way, support-blocking: from C's own publication (ruled §621: 30 January, not the article of the 28th).
            new DatedFact("C", "V", FactKind.PairLine, true, true, null, D(2026, 1, 30), Open,
                "DECLARED: Centerpartiet will not sit in, support or let through a cabinet that contains V - C's own publication 2026-01-30 [C-P1] (first reported 2026-01-28), restated 2026-04-21 [C-I3] and 2026-09-08 [C-I1], held after the election 2026-09-14 [C-I6] and 2026-09-18 [C-I8]; one way. " + SwedenSource),
            // The candidacies: 2022's carry until 2026's replace them (ruled §621).
            new DatedFact("S", null, FactKind.Candidacy, false, false, "Magdalena Andersson", D(2022, 8, 4), D(2026, 5, 1),
                "S's own page of 2022-08-04 [S-P1] (capture 2022-08-13 [S-P1a]) - its leader and sitting prime minister; carried until the 2026 declaration. " + SwedenSource2022),
            new DatedFact("S", null, FactKind.Candidacy, false, false, "Magdalena Andersson", D(2026, 5, 1), Open,
                "S's own pages: 2026-05-01 [S-P2], 2026-08-03 [S-P1], 2026-08-09 [S-P3]. " + SwedenSource),
            new DatedFact("M", null, FactKind.Candidacy, false, false, "Ulf Kristersson", D(2022, 3, 26), D(2026, 4, 1),
                "M's own page [M-P1] of 2022-03-26 (capture 2022-09-10 [M-P1a]); carried until the 2026 declaration. " + SwedenSource2022),
            new DatedFact("M", null, FactKind.Candidacy, false, false, "Ulf Kristersson", D(2026, 4, 1), Open,
                "M's own page of 2026-04-01 [MSD-P1] (\"Den enda som kan leda den är Ulf Kristersson.\"). " + SwedenSource),
            // V's in-or-against rule: the congress decision of 2026-04-18 (ruled §621: the decision the document records, not the PDF's date).
            new DatedFact("V", null, FactKind.InOrAgainst, false, false, null, D(2026, 4, 18), Open,
                "DECLARED: Vänsterpartiet will not support or let through a government it is not in, where its votes are needed - its election platform as decided by the congress 2026-04-18 ([V-P1], the PDF dated 2026-04-19; [V-I1]), restated as unchanged 2026-08-25 ([C-I9]), held after the election 2026-09-14 ([V-I4]). " + SwedenSource),
            // K-1g (ruled 2026-09-25). SD's refusal of the support role, from Åkesson's own post as SvD quotes it - §621's precedent as ruled: a party
            // leader's own post, quoted verbatim, dates the party's record (KD's line lifted on Busch's post on X as Bulletin quotes it). The rule is scoped
            // to the formation after the 2026 election; no runtime path reads the timeline before it (the vintage carries it).
            new DatedFact("SD", null, FactKind.NoSupportRole, false, false, null, D(2025, 10, 10), Open, SdNoSupportRole),
            // MP's in-or-against: dated by Helldén's own words to Sveriges Radio, 2026-08-10 (ruled 2026-09-29, §652); TT's paraphrase of 4 April is
            // a newsroom's, and MP's one saved publication (12 August) is later and does not state the condition.
            new DatedFact("MP", null, FactKind.InOrAgainst, false, false, null, D(2026, 8, 10), Open, MpInOrAgainst),
            // KD → S, one way: Busch's own words of 2 September as SVT's live report quotes them - spoken words, quoted verbatim on the broadcaster's
            // own page and dated by it: RULED by Elias's ruling F2 (K-1i (3), first put with §639); the June report is the newsroom's and never counts.
            new DatedFact("KD", "S", FactKind.PairLine, true, true, null, D(2026, 9, 2), Open, KdRefusesAndersson),
        };

        /// <summary>
        /// §705 (round 4 follow-up 4): GERMANY'S DECLARATIONS AS DATED FACTS - the Union's incompatibility resolutions and the 2025 chancellor
        /// candidacies, each quoted and saved in `ElectionsData/germany/coalition_declarations_2025.md`. The rest of Germany's lines stay DERIVED
        /// (the CSU-Linke pair included: no CSU resolution against Die Linke was found). A German candidacy draws no line (<see cref="CandidacyRefuses"/>).
        /// </summary>
        public static IReadOnlyList<DatedFact> GermanyTimeline { get; } = new[]
        {
            // CDU: the 31st party congress, Hamburg, 7-8 December 2018 - "lehnt Koalitionen und ähnliche Formen der Zusammenarbeit sowohl mit der
            // Linkspartei als auch mit der Alternative für Deutschland ab" [CDU-PT31]. SYMMETRIC and support-blocking - "I will not be in, or support,
            // a government that depends on you": a coalition is refused, and so is every "similar form of cooperation", which takes in a CDU cabinet
            // governing on the AfD's toleration as much as the CDU tolerating theirs. Not K-1's one-way shape: that one lets B support a cabinet A
            // sits in (C's refusal names cabinets that CONTAIN V, and no source has it refuse V's support), and the CDU's words refuse exactly that.
            new DatedFact("CDU", "AfD", FactKind.PairLine, true, false, null, D(2018, 12, 8), Open,
                "DECLARED: the CDU \"lehnt Koalitionen und ähnliche Formen der Zusammenarbeit sowohl mit der Linkspartei als auch mit der Alternative für Deutschland ab\" - its 31st party congress, Hamburg, 8 December 2018 [CDU-PT31]. " + GermanySource),
            new DatedFact("CDU", "Linke", FactKind.PairLine, true, false, null, D(2018, 12, 8), Open,
                "DECLARED: the CDU \"lehnt Koalitionen und ähnliche Formen der Zusammenarbeit sowohl mit der Linkspartei als auch mit der Alternative für Deutschland ab\" - its 31st party congress, Hamburg, 8 December 2018 [CDU-PT31]. " + GermanySource),
            // CSU: its Parteivorstand, 24 July 2023 - "Die Brandmauer gegen die AfD steht ... Zusammenarbeit mit der AfD ab" [CSU-PV23]; the same
            // shape - cooperation refused, in either direction
            new DatedFact("CSU", "AfD", FactKind.PairLine, true, false, null, D(2023, 7, 24), Open,
                "DECLARED: \"Die Brandmauer gegen die AfD steht\" - the CSU rejects cooperation with the AfD; its Parteivorstand, 24 July 2023 [CSU-PV23]. " + GermanySource),
            // The 2025 candidacies (the four the Bundestag's Datenhandbuch 6.5 lists, "2025 vor der Wahl" [BT-DHB65]), each from the earliest day a source
            // saved whole dates it - the review of §705 found three dated at the polling day, so a German run-up read Habeck's alone:
            // the Union's from the CSU's own page of 12 October 2024 (DERIVED: the page is the earliest saved; the CDU and CSU boards' nomination of
            // 23 September is press-reported, and cdu.de did not answer); the SPD's from its own page of 25 November 2024; the AfD's from ZDF's report
            // of its Bundesvorstand's decision of 7 December 2024; the Greens' from their own congress page of 17 November 2024.
            new DatedFact("CDU", null, FactKind.Candidacy, false, false, "Friedrich Merz", D(2024, 10, 12), Open,
                "the Union's chancellor candidate, Friedrich Merz (CDU) - the CSU's \"Rede des gemeinsamen Kanzlerkandidaten\", its party congress, \"Artikel vom 12.10.2024\" [CSU-PT24]; listed by the Bundestag's Datenhandbuch 6.5 [BT-DHB65]. " + GermanySource),
            new DatedFact("SPD", null, FactKind.Candidacy, false, false, "Olaf Scholz", D(2024, 11, 25), Open,
                "the SPD's chancellor candidate, Olaf Scholz - \"Heute haben wir Olaf Scholz zu unserem Kanzlerkandidaten nominiert\", the SPD Schleswig-Holstein, 25 November 2024 [SPD-N24]; confirmed by the party congress of 11 January 2025. " + GermanySource),
            new DatedFact("AfD", null, FactKind.Candidacy, false, false, "Alice Weidel", D(2024, 12, 7), Open,
                "the AfD's chancellor candidate, Alice Weidel - \"Der AfD-Vorstand nominierte die 45-Jährige am Samstag für das Spitzenamt\", ZDF, 7 December 2024 [ZDF-AFD24]. " + GermanySource),
            new DatedFact("Grune", null, FactKind.Candidacy, false, false, "Robert Habeck", D(2024, 11, 17), Open,
                "the Greens' candidate, Robert Habeck - their congress of 17 November 2024, \"Der entsprechende Antrag wurde mit 96,48 Prozent der Stimmen angenommen\" [GR-BDK24]. " + GermanySource),
        };

        public const string PolandSource = "See ElectionsData/poland/coalition_declarations_2023.md";

        /// <summary>Elias's ruling F2 (2026-10-05, §780, accepted with §652's condition; K-1i (3) answered): the mark on a Polish fact dated by a leader's
        /// spoken words as the broadcaster's or the news agency's OWN page quotes them verbatim, dated by that page - a journalist's paraphrase never counts,
        /// and a date from the party's own record always wins (READ, pending Elias, as §652's condition words it: where the party's own record carries the
        /// same declaration earlier, in whatever words). The pages a Polish fact is dated by under F2 are marked (F2) in the record's register. Every other Polish fact is dated by the party's own record
        /// (§621).</summary>
        public const string PolandSpokenWords = "DATED BY RULING F2: a leader's words as the broadcaster's own page quotes them verbatim, dated by that page. ";

        /// <summary>
        /// §776 (Elias's ruling E2: "source the parties' real 2023 declarations by read, dated, before PS-6"), read again under rulings F1 and F2:
        /// POLAND'S DECLARATIONS AS DATED FACTS - the lines the record carries for the 2023 lists, declared on or before the Sejm election of 15 October
        /// 2023, each quoted from a saved page (`ElectionsData/poland/coalition_declarations_2023.md`, whose timeline table is this array).
        /// <b>F1:</b> a pledge to keep a party from power - to remove it, end its rule, block its return, not let it govern - is a line: the declarer
        /// neither joins nor supports a cabinet that includes it (the one-way, support-blocking shape). <b>Dated</b> by §621's rules - the party's own
        /// record, never press reporting - and by F2, where a fact carries `PolandSpokenWords`: a leader's words quoted verbatim on the broadcaster's or
        /// the news agency's own page, dated by that page; a paraphrase never counts, and the party's own record always wins (read as §652's condition).
        /// The member parties' earlier refusals of PiS and the 2019 lists' lines are recorded there, not carried (its doubts 3 and 12). What is not
        /// here stays DERIVED. Poland carries no candidacy (none was declared before the vote), no in-or-against rule and no refusal of the support
        /// role. TD is one key for two parties (`ElectionsData/poland/td_list_2023.md`): its line to PiS rests on the list's founding words, its line to
        /// Konfederacja on PSL's half alone. A line's arrow is attribution; a cabinet-only line is symmetric in the model.
        /// </summary>
        public static IReadOnlyList<DatedFact> PolandTimeline { get; } = new[]
        {
            new DatedFact("PiS", "Konf", FactKind.PairLine, false, false, null, D(2023, 7, 23), Open,
                "DECLARED: PiS will not govern together with Konfederacja - Kaczyński at Stawiski, 2023-07-23, on the party's own page [PIS-P1] (\"Nie wierzcie we wspólne rządy PiS i Konfederacji!\") and on TVN24 the same day [PIS-I1] (\"Nie będziemy\"). A shared cabinet only; support is not addressed, and no saved PiS words pledge to keep Konfederacja from power. Before it the door was open (2023-06-28 [PIS-I4]; the club chairman Terlecki, 2023-07-16 [KONF-I15]). " + PolandSource),
            new DatedFact("PiS", "KO", FactKind.PairLine, true, true, null, D(2023, 9, 8), Open,
                "DECLARED (F1): PiS will block the return to power of the Platform and Tusk - Morawiecki, then prime minister, at Tomaszów Lubelski, on the party's own page, 2023-09-08 [PIS-P2] (\"Zablokujemy powrót do władzy Platformy Obywatelskiej i D. Tuska.\"). One way: nothing refuses KO's support for a cabinet PiS sits in. Earlier and aimed at the person, not the party: Kaczyński on TVN24, 2023-07-23 [PIS-I1] (\"Ten człowiek nie może rządzić Polską.\", of Tusk) - not read as the start (the record's §12). " + PolandSource),
            new DatedFact("Konf", "PiS", FactKind.PairLine, false, false, null, D(2023, 6, 20), D(2023, 7, 6),
                "DECLARED: Konfederacja's co-chairman Mentzen will enter no coalition with PiS, nor with anyone in the next term - RMF FM's own record of its debate, 2023-06-20 [KONF-I4] (\"Nie wejdę w koalicję z PiS-em. W przyszłej kadencji nie wejdę w koalicję z nikim.\"). His 26 June opening to PiS or PO (\"…to wszystko jest na stole\") stands only on Super Express's interview and its relays by PAP and RMF24 [KONF-I8], [KONF-I10], [KONF-I11], which F2, as the record reads it (the record's §12, doubt 4), does not count, so it lifts nothing. Replaced on 2023-07-06 by the one-way support-blocking line (F1). A cabinet only. " + PolandSpokenWords + PolandSource),
            new DatedFact("Konf", "KO", FactKind.PairLine, false, false, null, D(2023, 6, 20), D(2023, 7, 13),
                "DECLARED: Konfederacja's co-chairman Mentzen will enter no coalition with anyone in the next term, answering that a finance ministry would need a coalition with PiS or PO - RMF FM's own record of its debate, 2023-06-20 [KONF-I4] (\"W przyszłej kadencji nie wejdę w koalicję z nikim.\"). The 26 June opening stands only on pages F2, as the record reads it, does not count [KONF-I8], [KONF-I11]. Replaced on 2023-07-13 by the one-way support-blocking line (F1). A cabinet only. " + PolandSpokenWords + PolandSource),
            new DatedFact("Konf", "TD", FactKind.PairLine, false, false, null, D(2023, 6, 20), Open,
                "DECLARED: Konfederacja will enter no coalition with anyone - Mentzen, RMF FM's own record of its debate, 2023-06-20 [KONF-I4] (\"W przyszłej kadencji nie wejdę w koalicję z nikim.\"); restated on the party's own page, 2023-08-02 [KONF-P1] (\"Koalicja z PiS czy z Platformą? Z nikim!\"; \"Jasno i klarownie mówimy: nie będzie z nikim koalicji.\" - Przemysław Wipler's words on Onet Rano, published under the party's headline). TD is not named; this is the rule's \"z nikim\". The 26 June opening to anyone who adopts the tax programme stands only on pages F2, as the record reads it, does not count [KONF-I10], [KONF-I11]. A cabinet only. " + PolandSpokenWords + PolandSource),
            new DatedFact("Konf", "NL", FactKind.PairLine, false, false, null, D(2023, 6, 20), Open,
                "DECLARED: Konfederacja will enter no coalition with anyone - Mentzen, RMF FM's own record of its debate, 2023-06-20 [KONF-I4] (\"W przyszłej kadencji nie wejdę w koalicję z nikim.\"); restated on the party's own page, 2023-08-02 [KONF-P1] (\"Koalicja z PiS czy z Platformą? Z nikim!\"). NL is not named; this is the rule's \"z nikim\". Earlier and hedged: \"Może poza sojuszem z Lewicą\" (Mentzen, 2023-03-30 [KONF-I3]). The 26 June opening stands only on pages F2, as the record reads it, does not count [KONF-I10], [KONF-I11]. A cabinet only. " + PolandSpokenWords + PolandSource),
            new DatedFact("Konf", "MN", FactKind.PairLine, false, false, null, D(2023, 6, 20), Open,
                "DECLARED: Konfederacja will enter no coalition with anyone - Mentzen, RMF FM's own record of its debate, 2023-06-20 [KONF-I4] (\"W przyszłej kadencji nie wejdę w koalicję z nikim.\"); restated on the party's own page, 2023-08-02 [KONF-P1] (\"Koalicja z PiS czy z Platformą? Z nikim!\"). MN is never named; this is the rule's \"z nikim\". MN won no seat in 2023. A cabinet only. " + PolandSpokenWords + PolandSource),
            new DatedFact("Konf", "PiS", FactKind.PairLine, true, true, null, D(2023, 7, 6), Open,
                "DECLARED (F1): Konfederacja will end PiS's rule and enter no coalition with it - Mentzen on X, his own post, 2023-07-06 [KONF-P2] (\"Koalicji z PiS nie chcą wyborcy Konfederacji, działacze Konfederacji ani władze Konfederacji. Cały czas mówimy, że chcemy zakończyć rządy PiS\"). Restated by Bosak on TVN24, 2023-07-13 [KONF-I14] (\"Nie zamierzamy przedłużać władzy PiS-u. Nie zamierzamy z PiS-em zawierać koalicji\") and 2023-07-16 [KONF-I15] (\"My chcemy PiS odsunąć od władzy\"); on the party's own page, 2023-08-02 [KONF-P1] (Wipler's words on Onet Rano); by Mentzen on Polsat News, 2023-10-10 [KONF-I23] (\"chcemy zakończyć rządy PiS-u\"), and on TVN24, 2023-10-11 [KONF-I25]. One way: no source has Konfederacja refuse PiS's support for a cabinet it sits in. " + PolandSource),
            new DatedFact("Konf", "KO", FactKind.PairLine, true, true, null, D(2023, 7, 13), Open,
                "DECLARED (F1): Konfederacja will neither sit in a cabinet with KO nor let Tusk back to power - Bosak, TVN24 Fakty po południu, 2023-07-13 [KONF-I14] (\"nie zamierzamy zawierać koalicji z PO. Nie zamierzamy umożliwić powrotu Tuskowi do władzy.\"). Restated 2023-08-02 [KONF-P1], 2023-10-10 [KONF-I23] (\"nie dopuścić do rządów Donalda Tuska\") and 2023-10-11 [KONF-I25]. The premise, stated: a cabinet holding KO is Tusk's. One way. " + PolandSpokenWords + PolandSource),
            new DatedFact("TD", "PiS", FactKind.PairLine, true, true, null, D(2023, 5, 15), Open,
                "DECLARED (F1): Trzecia Droga will remove PiS from power - Hołownia at the press conference that named the list, on Polska 2050's own page, 2023-05-15 [TD-P11] (\"Musimy wygrać te wybory po to, żeby odsunąć PiS od władzy\"). Restated on PSL's own page 2023-08-10 [TD-P6] (\"odsunięcie PiS od władzy\"), in the list's paid material 2023-10-10 [TD-P8] (\"nie z PiS\") and on Polska 2050's own page 2023-10-12 [TD-P9] (\"Koalicja z PiSem? Po moim trupie.\"). One way: nothing refuses PiS's support for a cabinet TD sits in. " + PolandSource),
            new DatedFact("TD", "Konf", FactKind.PairLine, true, true, null, D(2023, 8, 10), Open,
                "DECLARED (F1; PSL's half of TD): the list's committee will not let Konfederacja's populists govern - PSL's campaign chief Jarubas and PSL's own post, on PSL's own page registering the committee, 2023-08-10 [TD-P6] (\"Komitet, który doprowadzi do odsunięcia PIS-u od władzy i nie dopuści do rządów populistów z Konfederacji\"). Restated as a coalition refusal in the list's paid material, 2023-10-10 [TD-P8] (\"nie z PiS ani nie z Konfederacją\"). Against it: Polska 2050's vice-chair Kobosko, 2023-09-01 [TD-I10] - not on a qualifying page. One way. " + PolandSource),
            new DatedFact("NL", "PiS", FactKind.PairLine, false, false, null, D(2021, 5, 6), D(2022, 9, 20),
                "DECLARED: Nowa Lewica will enter no coalition with PiS - Czarzasty, its leader, on the party's own page, 2021-05-06 [NL-P3] (\"Z PiS-em nigdy w życiu nie wejdę w żadną koalicję\"). Restated by the club chair 2021-11-15 [NL-P4], who kept votes on bills open; the predecessor SLD 2019-10-17 [NL-P1]. A cabinet only. Replaced on 2022-09-20 by the one-way support-blocking line (F1). " + PolandSource),
            new DatedFact("NL", "PiS", FactKind.PairLine, true, true, null, D(2022, 9, 20), Open,
                "DECLARED (F1): Nowa Lewica will remove PiS from power - its co-chairman Czarzasty to the opposition's leaders, on the party's own page, 2022-09-20 [NL-P31]: whether the opposition runs one list, two or three, those lists must win, to remove PiS from power (\"Ważne jest to, aby te listy były zwycięskie, aby odsunąć PiS od władzy.\"). Restated by its National Board's resolution of 21 January 2023 [NL-P32] (\"Ciężko pracujemy, by odsunąć PiS od władzy\") and by the club chair Gawkowski on TVN24, 2023-10-12 [KO-I12] (\"Startujemy w wyborach po to, żeby odsunąć PiS od władzy.\"). One way: nothing refuses PiS's support for a cabinet NL sits in. Earlier and less plain: \"PiS trzeba pokonać\" [NL-P3], 2021-05-06 - not read as the start (the record's §12). " + PolandSource),
            new DatedFact("NL", "Konf", FactKind.PairLine, false, false, null, D(2023, 7, 5), Open,
                "DECLARED: Lewica will not enter any government in which Konfederacja sits - Czarzasty to Rzeczpospolita, on the party's own page, 2023-07-05 [NL-P17] (\"na pewno Lewica nie wejdzie do żadnego rządu, w którym będzie Konfederacja\"; to PO and TD: \"nie liczcie na nas\"). Restated 2023-09-11 [NL-P25] (\"Lewica w żadnym rządzie nie będzie stała czy siedziała przy Konfederacji\"). A cabinet only: its support half - no Lewica cabinet on Konfederacja's votes - survives only on PAP copies and a portal (2023-08-27, 2023-08-31), which F2 does not count, and NL's own words toward Konfederacja are not F1's in terms (the record's §4); the derived NL-Konf line blocks support (the record's §10). " + PolandSource),
            new DatedFact("KO", "PiS", FactKind.PairLine, true, true, null, D(2022, 2, 9), Open,
                "DECLARED (F1): KO will remove PiS from power - Tusk, its leader, on X, his own post, 2022-02-09 [KO-P1] (\"Najwyższy czas, aby wszyscy zrozumieli, że odsunięcie PiS od władzy to nasze wspólne być albo nie być.\"). Restated on KO's own account, 2022-02-17 [KO-P2] (\"Chcę odsunąć PiS od władzy poprzez wygraną w wyborach.\", Tusk on TOK FM), 2022-05-18 [KO-P3] (\"Moim zadaniem jest odsunięcie PiS od władzy.\") and 2023-09-20 [KO-P4], and on TVN24 Fakty, 2023-10-12 [KO-I11] (\"…odrodzenie Polski, które będzie możliwe bez PiS-u u władzy\"). Dated by KO's own record - F2: a date from the party's own record always wins (the record's §5). One way: nothing refuses PiS's support for a cabinet KO sits in. " + PolandSource),
        };

        /// <summary>The dated facts of a country with a timeline; none for the rest.</summary>
        public static IReadOnlyList<DatedFact> TimelineOf(CountryId country) =>
            country == CountryId.Sweden ? SwedenTimeline : country == CountryId.Germany ? GermanyTimeline : country == CountryId.Poland ? PolandTimeline : System.Array.Empty<DatedFact>();

        /// <summary>Whether a country's declarations are dated on a timeline (§621) - Sweden's, Germany's since §705 and Poland's since §776; every other country reads its vintage.</summary>
        public static bool HasTimeline(CountryId country) => country == CountryId.Sweden || country == CountryId.Germany || country == CountryId.Poland;

        /// <summary>§657: the dated facts standing on <paramref name="asOf"/>, in the timeline's order - the run-up's declarations page (D-PS, the DECLARED block only). Empty without a timeline.</summary>
        public static List<DatedFact> StandingOn(CountryId country, System.DateTime asOf)
        {
            var standing = new List<DatedFact>();
            if (!HasTimeline(country)) { return standing; }
            foreach (DatedFact f in TimelineOf(country)) { if (f.StandsOn(asOf)) { standing.Add(f); } }
            return standing;
        }

        /// <summary>§657: the facts LIFTED after <paramref name="since"/> and on or before <paramref name="asOf"/> - ended with no dated fact of the same party, other and kind
        /// taking over on that day (a restatement replaces, a lift ends). A lifted line is a declaration too.</summary>
        public static List<DatedFact> LiftedSince(CountryId country, System.DateTime since, System.DateTime asOf)
        {
            var lifted = new List<DatedFact>();
            if (!HasTimeline(country)) { return lifted; }
            foreach (DatedFact f in TimelineOf(country))
            {
                if (f.Until == Open || f.Until <= since.Date || f.Until > asOf.Date) { continue; }
                bool replaced = false;
                foreach (DatedFact g in TimelineOf(country)) { if (g.From == f.Until && g.Party == f.Party && g.Other == f.Other && g.Kind == f.Kind) { replaced = true; break; } }
                if (!replaced) { lifted.Add(f); }
            }
            return lifted;
        }

        /// <summary>The derived lines plus the declared ones standing on <paramref name="asOf"/> - the timeline's reading (§621). Sweden, Germany (§705) and Poland (§776); the other countries return derived lines alone.</summary>
        private static List<RedLine> ForDateSourced(CountryId country, IReadOnlyList<PoliticalParty> parties, System.DateTime asOf)
        {
            var lrGen = new double[parties.Count];
            var galtan = new double[parties.Count];
            for (int p = 0; p < parties.Count; p++) { lrGen[p] = parties[p].LrGen; galtan[p] = parties[p].Galtan; }
            List<RedLine> lines = DerivedRedLines.From(lrGen, galtan);
            if (!HasTimeline(country)) { return lines; }
            if (CandidacyRefuses(country)) { AddCandidacyLines(lines, parties, CandidaciesAt(country, asOf)); }   // §705: Sweden's ruled pairing rule; a German candidacy draws no line
            foreach (DatedFact f in TimelineOf(country))
            {
                if (f.Kind != FactKind.PairLine || !f.StandsOn(asOf)) { continue; }
                int a = IndexOf(parties, f.Party), b = IndexOf(parties, f.Other);
                if (a < 0 || b < 0) { continue; }
                lines.Add(new RedLine(a, b, RedLineKind.Declared, blocksSupport: f.BlocksSupport, oneWay: f.OneWay, basis: f.Basis));
            }
            return lines;
        }

        /// <summary>The own-leader candidacies standing on <paramref name="asOf"/> (§621: 2022's carry until 2026's replace them).</summary>
        private static IReadOnlyList<(string Abbrev, string Candidate, string Basis)> CandidaciesAtSourced(CountryId country, System.DateTime asOf)
        {
            var found = new List<(string, string, string)>();
            if (!HasTimeline(country)) { return found; }
            foreach (DatedFact f in TimelineOf(country)) { if (f.Kind == FactKind.Candidacy && f.StandsOn(asOf)) { found.Add((f.Party, f.Candidate, f.Basis)); } }
            return found;
        }

        /// <summary>The in-or-against rules standing on <paramref name="asOf"/>.</summary>
        private static List<InOrAgainst> InOrAgainstAtSourced(CountryId country, IReadOnlyList<PoliticalParty> parties, System.DateTime asOf)
        {
            var rules = new List<InOrAgainst>();
            if (!HasTimeline(country)) { return rules; }
            foreach (DatedFact f in TimelineOf(country))
            {
                if ((f.Kind != FactKind.InOrAgainst && f.Kind != FactKind.NoSupportRole) || !f.StandsOn(asOf)) { continue; }
                int p = IndexOf(parties, f.Party);
                if (p >= 0) { rules.Add(new InOrAgainst(p, f.Basis, votesAgainst: f.Kind == FactKind.InOrAgainst)); }
            }
            return rules;
        }

        private static void AddCandidacyLines(List<RedLine> lines, IReadOnlyList<PoliticalParty> parties, IReadOnlyList<(string Abbrev, string Candidate, string Basis)> declared)
        {
            for (int i = 0; i < declared.Count; i++)
            {
                int a = IndexOf(parties, declared[i].Abbrev);
                if (a < 0) { continue; }
                for (int j = 0; j < declared.Count; j++)
                {
                    int b = IndexOf(parties, declared[j].Abbrev);
                    if (j == i || b < 0) { continue; }
                    lines.Add(new RedLine(a, b, RedLineKind.Declared, blocksSupport: true, oneWay: true,
                        basis: CandidacyPrefix + declared[i].Abbrev + "'s own leader " + declared[i].Candidate + " is its prime-ministerial candidate ("
                               + declared[i].Basis + "); it refuses any cabinet led by another party's candidate - " + declared[j].Abbrev + "'s is "
                               + declared[j].Candidate + " (" + declared[j].Basis + ")."));
                }
            }
        }

        // ── §679: A CREATED PARTY'S DECLARATIONS FEED THE FORMATION LIKE EVERY PARTY'S ──────────────────────────────────────────────────────
        // The six readers the formation calls (through `DeclarationReading`) are the sourced declarations plus the created parties' (SP-4 stores
        // them on `CreatedParty` by KEY). A red line is symmetric and support-blocking (the flow's "will not sit in or support a cabinet with");
        // a one-way line is K-1's shape; an own-leader candidacy joins the candidacy list, so the ruled pairing rule makes it refuse the declared
        // candidates' cabinets and theirs refuse it; backing a real party's candidate refuses the OTHER candidates' cabinets, one way; in-or-against
        // is K-1f's rule, voting against. With none registered every list is exactly the sourced one.

        /// <summary>The basis every created party's declaration carries - a founder's declaration, not a citation.</summary>
        public const string CreatedPrefix = "DECLARED at the party's founding: ";

        public static List<RedLine> For(CountryId country, IReadOnlyList<PoliticalParty> parties, ElectionVintage vintage = ElectionVintage.Seated)
        {
            List<RedLine> lines = ForSourced(country, parties, vintage);
            AddCreatedLines(country, lines, parties, Candidacies(country, vintage));
            return lines;
        }

        public static List<RedLine> ForDate(CountryId country, IReadOnlyList<PoliticalParty> parties, System.DateTime asOf)
        {
            List<RedLine> lines = ForDateSourced(country, parties, asOf);
            AddCreatedLines(country, lines, parties, CandidaciesAt(country, asOf));
            return lines;
        }

        public static IReadOnlyList<(string Abbrev, string Candidate, string Basis)> Candidacies(CountryId country, ElectionVintage vintage = ElectionVintage.Seated) =>
            WithCreatedCandidacies(country, CandidaciesSourced(country, vintage));

        public static IReadOnlyList<(string Abbrev, string Candidate, string Basis)> CandidaciesAt(CountryId country, System.DateTime asOf) =>
            WithCreatedCandidacies(country, CandidaciesAtSourced(country, asOf));

        public static List<InOrAgainst> InOrAgainstFor(CountryId country, IReadOnlyList<PoliticalParty> parties, ElectionVintage vintage = ElectionVintage.Seated)
        {
            List<InOrAgainst> rules = InOrAgainstForSourced(country, parties, vintage);
            AddCreatedRules(country, rules, parties);
            return rules;
        }

        public static List<InOrAgainst> InOrAgainstAt(CountryId country, IReadOnlyList<PoliticalParty> parties, System.DateTime asOf)
        {
            List<InOrAgainst> rules = InOrAgainstAtSourced(country, parties, asOf);
            AddCreatedRules(country, rules, parties);
            return rules;
        }

        private static IReadOnlyList<(string Abbrev, string Candidate, string Basis)> WithCreatedCandidacies(CountryId country, IReadOnlyList<(string Abbrev, string Candidate, string Basis)> sourced)
        {
            if (!CreatedParties.Any(country)) { return sourced; }
            var all = new List<(string Abbrev, string Candidate, string Basis)>(sourced);
            foreach (CreatedParty c in CreatedParties.Of(country))
            {
                if (string.IsNullOrEmpty(c.BacksCandidateOf) || c.BacksCandidateOf != c.Key) { continue; }
                all.Add((c.Key, string.IsNullOrEmpty(c.LeaderName) ? c.Key + "'s leader" : c.LeaderName, CreatedPrefix + c.Name + " named its own leader for prime minister"));
            }
            return all;
        }

        private static void AddCreatedLines(CountryId country, List<RedLine> lines, IReadOnlyList<PoliticalParty> parties, IReadOnlyList<(string Abbrev, string Candidate, string Basis)> candidacies)
        {
            foreach (CreatedParty c in CreatedParties.Of(country))
            {
                int me = IndexOf(parties, c.Key);
                if (me < 0) { continue; }
                foreach (string other in c.RedLinesAgainst ?? new List<string>())
                {
                    int o = IndexOf(parties, other);
                    if (o >= 0 && o != me) { lines.Add(new RedLine(me, o, RedLineKind.Declared, blocksSupport: true, basis: CreatedPrefix + c.Name + " will not sit in or support a cabinet with " + other)); }
                }
                foreach (string other in c.OneWayAgainst ?? new List<string>())
                {
                    int o = IndexOf(parties, other);
                    if (o >= 0 && o != me) { lines.Add(new RedLine(me, o, RedLineKind.Declared, blocksSupport: true, oneWay: true, basis: CreatedPrefix + c.Name + " will not sit in or support any cabinet that contains " + other)); }
                }
                // backing a REAL party's candidate: every other declared candidate's cabinet refused, one way (the candidacy lines' own strength)
                if (CandidacyRefuses(country) && !string.IsNullOrEmpty(c.BacksCandidateOf) && c.BacksCandidateOf != c.Key)   // §705: Sweden's pairing rule, not Germany's
                {
                    foreach ((string abbrev, string candidate, string _) in candidacies)
                    {
                        int o = IndexOf(parties, abbrev);
                        if (o < 0 || o == me || abbrev == c.BacksCandidateOf) { continue; }
                        lines.Add(new RedLine(me, o, RedLineKind.Declared, blocksSupport: true, oneWay: true,
                            basis: CandidacyPrefix + CreatedPrefix + c.Name + " backs " + c.BacksCandidateOf + "'s candidate; it refuses any cabinet led by " + abbrev + "'s, " + candidate));
                    }
                }
            }
        }

        private static void AddCreatedRules(CountryId country, List<InOrAgainst> rules, IReadOnlyList<PoliticalParty> parties)
        {
            foreach (CreatedParty c in CreatedParties.Of(country))
            {
                int me = IndexOf(parties, c.Key);
                if (me >= 0 && c.InOrAgainst) { rules.Add(new InOrAgainst(me, CreatedPrefix + c.Name + " will not support or let through a cabinet it is not in")); }
            }
        }

        private static int IndexOf(IReadOnlyList<PoliticalParty> parties, string abbrev)
        {
            for (int p = 0; p < parties.Count; p++)
            {
                if (parties[p].Abbrev == abbrev) { return p; }
            }

            return -1;
        }
    }

    /// <summary>
    /// PS-3i-2c (ruled 2026-09-29, §653): WHICH DECLARATIONS A FORMATION READS. Where a country's declarations are dated (§621's timeline), the
    /// pair lines and the candidacies are read on one day and the PLATFORMS - the in-or-against rules and the support role refused, each a party's
    /// terms for the formation after an election - on another: **any election reads everything dated at its own polling day**; **a mid-term round
    /// reads the lines and candidacies standing today, with the platforms held to the election that seated the chamber** (they address that
    /// election's formation, not the next one's until it is held). A country without a timeline reads its vintage, as every formation did before.
    /// </summary>
    public readonly struct DeclarationReading
    {
        /// <summary>The vintage read where the reading is not dated - and, where it is, the election's that seated the chamber.</summary>
        public readonly ElectionVintage Vintage;
        /// <summary>The day the pair lines and candidacies are read on; MinValue where the vintage is read.</summary>
        public readonly System.DateTime LinesOn;
        /// <summary>The day the platforms (in-or-against, the support role refused) are read on.</summary>
        public readonly System.DateTime PlatformsOn;

        private DeclarationReading(ElectionVintage vintage, System.DateTime linesOn, System.DateTime platformsOn)
        {
            Vintage = vintage; LinesOn = linesOn; PlatformsOn = platformsOn;
        }

        public bool Dated => LinesOn != System.DateTime.MinValue;

        /// <summary>A vintage's declarations, whole - the backtests, the start's chamber of record, a country without a timeline.</summary>
        public static DeclarationReading OfVintage(ElectionVintage vintage) => new DeclarationReading(vintage, System.DateTime.MinValue, System.DateTime.MinValue);

        /// <summary>An election's: everything dated at its own polling day (ruled) - an extra election before the next ordinary one included.</summary>
        public static DeclarationReading OfElection(CountryId country, System.DateTime pollingDay)
        {
            ElectionVintage vintage = WorldClock.VintageOfElection(country, pollingDay);
            return DeclaredRedLines.HasTimeline(country) ? new DeclarationReading(vintage, pollingDay.Date, pollingDay.Date) : OfVintage(vintage);
        }

        /// <summary>A mid-term reading: the lines and candidacies standing on <paramref name="today"/>, the platforms held to the sitting chamber's polling day.</summary>
        public static DeclarationReading MidTerm(CountryId country, ElectionVintage sitting, System.DateTime today, System.DateTime sittingPollingDay) =>
            DeclaredRedLines.HasTimeline(country) ? new DeclarationReading(sitting, today.Date, sittingPollingDay.Date) : OfVintage(sitting);

        /// <summary>Everything on one day - the measuring instrument §644 built (`AiMotionReachDiagnostic`); no game path reads this way.</summary>
        public static DeclarationReading AllOn(CountryId country, ElectionVintage vintage, System.DateTime day) => new DeclarationReading(vintage, day.Date, day.Date);

        public List<RedLine> Lines(CountryId country, IReadOnlyList<PoliticalParty> parties) =>
            Dated ? DeclaredRedLines.ForDate(country, parties, LinesOn) : DeclaredRedLines.For(country, parties, Vintage);

        public List<InOrAgainst> Platforms(CountryId country, IReadOnlyList<PoliticalParty> parties) =>
            Dated ? DeclaredRedLines.InOrAgainstAt(country, parties, PlatformsOn) : DeclaredRedLines.InOrAgainstFor(country, parties, Vintage);

        public IReadOnlyList<(string Abbrev, string Candidate, string Basis)> Candidacies(CountryId country) =>
            Dated ? DeclaredRedLines.CandidaciesAt(country, LinesOn) : DeclaredRedLines.Candidacies(country, Vintage);

        public override string ToString() => Dated
            ? string.Format(System.Globalization.CultureInfo.InvariantCulture, "lines and candidacies of {0:yyyy-MM-dd}, platforms of {1:yyyy-MM-dd}", LinesOn, PlatformsOn)
            : "the " + Vintage + " declarations";
    }
}
