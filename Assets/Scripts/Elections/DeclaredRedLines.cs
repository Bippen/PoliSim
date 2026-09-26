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
    /// let an inference wear a citation's authority — and, worse, would hide the fact that **only one of
    /// the six countries has its declarations on disk.**</para>
    ///
    /// <para>⚠ <b>SOURCED FOR SWEDEN ONLY, in two vintages.</b> K-1 (2026-09-23): the live game reads the
    /// declarations as of the 2026 election, `ElectionsData/sweden/2026/coalition_declarations_2026.md`; the
    /// backtests that assert 2022's government pin <see cref="ElectionVintage.Sweden2022"/>,
    /// `ElectionsData/sweden/coalition_declarations_2022.md`. For every other country `For` returns the DERIVED
    /// lines alone and `IsSourced` returns false, so a caller can say plainly that the government it formed was
    /// formed without that country's real declarations. **Inventing Germany's would be inventing the central
    /// political fact of its party system**, and a formation run without them can produce a cabinet that
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
        public static bool IsSourced(CountryId country) => country == CountryId.Sweden;

        /// <summary>The derived lines plus any declared ones this country has on disk, in the party order
        /// of <paramref name="parties"/>, as of <paramref name="vintage"/> - the seated election's unless a
        /// backtest pins 2022's.</summary>
        public static List<RedLine> For(CountryId country, IReadOnlyList<PoliticalParty> parties, ElectionVintage vintage = ElectionVintage.Seated)
        {
            vintage = WorldClock.Resolve(country, vintage);   // PS-1 (§618): the seated chamber's election - every election reads its own date's declarations
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
        public static IReadOnlyList<(string Abbrev, string Candidate, string Basis)> Candidacies(CountryId country, ElectionVintage vintage = ElectionVintage.Seated)
        {
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
        public static List<InOrAgainst> InOrAgainstFor(CountryId country, IReadOnlyList<PoliticalParty> parties, ElectionVintage vintage = ElectionVintage.Seated)
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
        private const string MpInOrAgainst = "DECLARED: Miljöpartiet will vote no to a prime minister unless it sits in the government - TT's report of Helldén's condition "
            + "2026-04-04 ([MP-I4], the newsroom's paraphrase: \"röstar nej utan regeringsplats\"), restated 2026-08-10 to Sveriges Radio ([MP-I1]: \"Vi ska sitta i nästa regering\"; "
            + "[MP-I2]: \"Partiet ställer samtidigt ett villkor om att sitta i regering\"); secondary only - MP's own wording is a GAP, and the date is a press report's (§639). " + SwedenSource;

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
        // adds three: SD's refusal of the support role 2025-10-10, MP's in-or-against rule 2026-04-04 (a press report's date, stated), KD → S 2026-09-02.
        // No runtime surface reads the timeline yet - the run-up's declarations are D-PS's (§620/§621); the election reads its own day's. §644 measures a
        // mid-term round on it (`GovernmentFormation.ViewOfSitting` with a date, read by `AiMotionReachDiagnostic`) for Elias's ruling PS-3i-2c.
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

        /// <summary>Sweden's declarations as dated facts - every date the party's own record's (the rules of §621), each fact standing until the dated one that replaced it.</summary>
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
            // MP's in-or-against: its own record is a GAP, so the date is a press report's - the first that carries the vote against (TT, 2026-04-04), a
            // stated deviation from §621's first rule (§639).
            new DatedFact("MP", null, FactKind.InOrAgainst, false, false, null, D(2026, 4, 4), Open, MpInOrAgainst),
            // KD → S, one way: Busch's own words of 2 September as SVT's live report quotes them - spoken words, not a party publication, so an
            // EXTENSION of §621's precedent (a post on X), stated and put to Elias (§639); the June report is the newsroom's.
            new DatedFact("KD", "S", FactKind.PairLine, true, true, null, D(2026, 9, 2), Open, KdRefusesAndersson),
        };

        /// <summary>The derived lines plus the declared ones standing on <paramref name="asOf"/> - the timeline's reading (§621). Sweden only; the other countries return derived lines alone.</summary>
        public static List<RedLine> ForDate(CountryId country, IReadOnlyList<PoliticalParty> parties, System.DateTime asOf)
        {
            var lrGen = new double[parties.Count];
            var galtan = new double[parties.Count];
            for (int p = 0; p < parties.Count; p++) { lrGen[p] = parties[p].LrGen; galtan[p] = parties[p].Galtan; }
            List<RedLine> lines = DerivedRedLines.From(lrGen, galtan);
            if (country != CountryId.Sweden) { return lines; }
            AddCandidacyLines(lines, parties, CandidaciesAt(country, asOf));
            foreach (DatedFact f in SwedenTimeline)
            {
                if (f.Kind != FactKind.PairLine || !f.StandsOn(asOf)) { continue; }
                int a = IndexOf(parties, f.Party), b = IndexOf(parties, f.Other);
                if (a < 0 || b < 0) { continue; }
                lines.Add(new RedLine(a, b, RedLineKind.Declared, blocksSupport: f.BlocksSupport, oneWay: f.OneWay, basis: f.Basis));
            }
            return lines;
        }

        /// <summary>The own-leader candidacies standing on <paramref name="asOf"/> (§621: 2022's carry until 2026's replace them).</summary>
        public static IReadOnlyList<(string Abbrev, string Candidate, string Basis)> CandidaciesAt(CountryId country, System.DateTime asOf)
        {
            var found = new List<(string, string, string)>();
            if (country != CountryId.Sweden) { return found; }
            foreach (DatedFact f in SwedenTimeline) { if (f.Kind == FactKind.Candidacy && f.StandsOn(asOf)) { found.Add((f.Party, f.Candidate, f.Basis)); } }
            return found;
        }

        /// <summary>The in-or-against rules standing on <paramref name="asOf"/>.</summary>
        public static List<InOrAgainst> InOrAgainstAt(CountryId country, IReadOnlyList<PoliticalParty> parties, System.DateTime asOf)
        {
            var rules = new List<InOrAgainst>();
            if (country != CountryId.Sweden) { return rules; }
            foreach (DatedFact f in SwedenTimeline)
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

        private static int IndexOf(IReadOnlyList<PoliticalParty> parties, string abbrev)
        {
            for (int p = 0; p < parties.Count; p++)
            {
                if (parties[p].Abbrev == abbrev) { return p; }
            }

            return -1;
        }
    }
}
