using System;
using System.Collections.Generic;

namespace PoliSim.Elections
{
    /// <summary>§29's five outcomes, in the spec's own order.</summary>
    public enum CoalitionOutcomeKind
    {
        MajorityCoalition,
        MinorityGovernment,
        ConfidenceAndSupply,
        NewElection,
        Collapse,
    }

    /// <summary>
    /// Where a red line comes from, which is not a detail: a DERIVED line is the model's own
    /// reading of how far apart two parties stand (it generalises to any country whose positions
    /// are sourced); a DECLARED line is a party's own public commitment, which is a dated FACT and
    /// can be withdrawn — the Liberals refused a government dependent on the Sweden Democrats
    /// before 2018 and signed the Tidö agreement with them in 2022. A model with only derived
    /// lines cannot express that; a model with only declared ones cannot leave Sweden.
    /// </summary>
    public enum RedLineKind { Derived, Declared }

    /// <summary>
    /// A refusal between two parties. §29 needs TWO strengths, because Sweden 2022 turns on the
    /// difference: <see cref="BlocksCabinet"/> is "I will not sit in a cabinet with you",
    /// <see cref="BlocksSupport"/> is "I will not be in, or support, a government that depends on
    /// you" — the Centre Party's position, which is why a bloc with the arithmetic to govern did
    /// not. A line that blocks support always blocks the cabinet too.
    ///
    /// <para>K-1 (2026-09-23): a THIRD shape, <see cref="OneWay"/>, because Sweden 2026 turns on it: the Centre Party
    /// will not sit in, support or let through any cabinet that CONTAINS the Left Party, and no fetched source that names a
    /// mechanism has it refuse the Left Party as a mere supporter of a cabinet it sits in (`coalition_declarations_2026.md`). Neither symmetric
    /// strength says that: cabinet-blocking would let C prop up a cabinet with V in it, support-blocking would stop V
    /// tolerating a cabinet C is in. A one-way line refuses from <see cref="A"/> to <see cref="B"/> only.</para>
    /// </summary>
    public readonly struct RedLine
    {
        public readonly int A;
        public readonly int B;
        public readonly RedLineKind Kind;
        public readonly bool BlocksSupport;
        /// <summary>K-1: the refusal runs from <see cref="A"/> to <see cref="B"/> only - A will not sit in, or support, a
        /// cabinet that contains B; B may support a cabinet A sits in, and the two may both support one. Implies
        /// <see cref="BlocksSupport"/> in A's direction. False for every derived line and every line before K-1.</summary>
        public readonly bool OneWay;
        /// <summary>The derivation, or the citation and its vintage. Never empty — a red line without a basis is an authored coalition score, which §29 must not have.</summary>
        public readonly string Basis;

        public RedLine(int a, int b, RedLineKind kind, bool blocksSupport, string basis, bool oneWay = false)
        {
            if (a == b) { throw new ArgumentException("a party cannot red-line itself"); }
            if (string.IsNullOrEmpty(basis)) { throw new ArgumentException("a red line needs its basis"); }
            if (oneWay && !blocksSupport) { throw new ArgumentException("a one-way line refuses support in its direction, so it blocks support"); }
            A = a; B = b; Kind = kind; BlocksSupport = blocksSupport; Basis = basis; OneWay = oneWay;
        }

        public bool Covers(int x, int y) => (A == x && B == y) || (A == y && B == x);

        /// <summary>Whether this line stops party <paramref name="p"/> supporting a cabinet that contains party <paramref name="q"/>:
        /// a symmetric support-blocking line in either direction, a one-way line only from its A to its B.</summary>
        public bool RefusesSupport(int p, int q) => BlocksSupport && (OneWay ? A == p && B == q : Covers(p, q));
    }

    /// <summary>
    /// K-1f (ruled 2026-09-24): a party's declared rule that it supports NO cabinet it does not sit in - "in or against" - the mirror of
    /// <see cref="RedLine.OneWay"/>: where a one-way line refuses cabinets that CONTAIN a named party, this refuses every cabinet that does
    /// not contain the declaring one. The party is never a supporter of a cabinet outside it and votes against every such cabinet at its
    /// investiture (it will not "let it through"). It is a party's rule, not a pair's, so it is not a <see cref="RedLine"/>; it never refuses
    /// a cabinet the party sits in. Sourced and dated like a declared line - the basis carries the citation.
    /// <para>K-1g (ruled 2026-09-25): the rule's two halves are separable, because the declarations are. V's and MP's words refuse to support
    /// AND to let through a cabinet they are not in - they vote against it (<see cref="VotesAgainst"/> true). SD's words refuse the support
    /// role - "either a government party or an opposition party", no middle position - and do not say it votes every other cabinet down, so
    /// on the builder's reading, open for Elias (K-1i), SD's rule is never a supporter, its vote left to the lines and the hold-out
    /// (<see cref="VotesAgainst"/> false). The other reading is a row of `Formation2026Diagnostic`.</para>
    /// </summary>
    public readonly struct InOrAgainst
    {
        public readonly int Party;
        /// <summary>The citation and its date. Never empty.</summary>
        public readonly string Basis;
        /// <summary>True: the party votes against every cabinet it is not in (it will not let one through). False: it only refuses to
        /// support one from outside (K-1g, SD's declared form).</summary>
        public readonly bool VotesAgainst;

        public InOrAgainst(int party, string basis, bool votesAgainst = true)
        {
            if (string.IsNullOrEmpty(basis)) { throw new ArgumentException("an in-or-against rule needs its basis"); }
            Party = party; Basis = basis; VotesAgainst = votesAgainst;
        }

        /// <summary>The parties in <paramref name="rules"/> within a chamber of <paramref name="n"/>, as a mask - every rule, or only those that
        /// vote against (a party index past the chamber is ignored, as a line's is).</summary>
        public static int Mask(IReadOnlyList<InOrAgainst> rules, int n, bool votingAgainstOnly)
        {
            int mask = 0;
            if (rules == null) { return 0; }
            foreach (InOrAgainst rule in rules)
            {
                if (rule.Party < 0 || rule.Party >= n || (votingAgainstOnly && !rule.VotesAgainst)) { continue; }
                mask |= 1 << rule.Party;
            }
            return mask;
        }
    }

    /// <summary>
    /// §29's party-to-party compatibility, DERIVED from sourced positions and from nothing else.
    /// The spec names seven inputs; four of them are computable from data already on disk and
    /// three are not, and the split is recorded rather than papered over:
    ///
    /// - **Ideological compatibility** — CHES `lrgen` distance. DERIVED.
    /// - **Policy compatibility** — CHES `lrecon`, `galtan` and `eu_position` distance. DERIVED.
    /// - **Seat strength** and **negotiating power** — <see cref="CoalitionMath"/>, from the seat
    ///   distribution alone (a Banzhaf pivotality share, not an opinion). DERIVED.
    /// - **Coalition red lines** — <see cref="RedLine"/>, derived OR declared-and-cited.
    /// - **Leader compatibility** and **personal relationships** — **DEFERRED, and this is the
    ///   reason:** there is no source for them. Candidate attributes in this prototype are already
    ///   [AUTHORED-DRAFT] game fiction (W-B7), and inventing a leaders'-relations matrix on top of
    ///   them would be exactly the "authored coalition score" §29 must not have. They wait for a
    ///   later item; the absence is asserted by the harness so it cannot be filled in by accident.
    ///
    /// Distances are on CHES's own 0–10 scales (`eu_position` is 1–7 and is rescaled to 0–10
    /// before it is compared, since a raw 1–7 gap would be silently smaller than a 0–10 one). An
    /// axis that is NaN for either party is SKIPPED, never centred — the rule `Compatibility`
    /// already follows for §7, for the same reason: reading a missing position as "moderate"
    /// invents it.
    /// </summary>
    public static class CoalitionCompatibility
    {
        /// <summary>[AUTHORED-DRAFT] the two halves of §29's compatibility. Ideology is the larger because it is what survives a change of policy of the week - the same argument `Compatibility` makes for §7's terms, at half the confidence.</summary>
        public const double WeightIdeological = 0.55;
        public const double WeightPolicy = 0.45;

        /// <summary>CHES `eu_position` runs 1-7; every other axis 0-10. Rescaled so a gap means the same thing on every axis.</summary>
        public static double RescaleEu(double euPosition) => double.IsNaN(euPosition) ? double.NaN : (euPosition - 1.0) * (10.0 / 6.0);

        /// <summary>100 at no distance, 0 at the width of the scale. The linear form is deliberate: nothing in §29 justifies a curve, and a curve would hide which pairs the thresholds actually separate.</summary>
        public static double FromDistance(double distance) => Math.Max(0.0, 100.0 * (1.0 - Math.Abs(distance) / 10.0));

        /// <summary>Mean compatibility over the axes both parties define; NaN when they share none.</summary>
        public static double OverAxes(double[] a, double[] b)
        {
            if (a == null || b == null || a.Length != b.Length) { throw new ArgumentException("one value per axis, both parties"); }
            double sum = 0.0; int n = 0;
            for (int i = 0; i < a.Length; i++)
            {
                if (double.IsNaN(a[i]) || double.IsNaN(b[i])) { continue; }
                sum += FromDistance(a[i] - b[i]); n++;
            }

            return n == 0 ? double.NaN : sum / n;
        }
    }

    /// <summary>
    /// §29's "Seat Strength" and "Negotiating Power" from the seat distribution and nothing else.
    ///
    /// Seat strength is a party's share of the chamber. Negotiating power is its **Banzhaf
    /// pivotality**: over every subset of the OTHER parties, the share in which this party's seats
    /// turn a losing bloc into a winning one. It is a standard measure, it is computed rather than
    /// assigned, and it says the thing §29 wants said — a middling party that every majority must
    /// pass through negotiates from strength, and a large party nobody can use does not.
    /// </summary>
    public static class CoalitionMath
    {
        /// <summary>The seats a bloc holds, given a membership mask.</summary>
        public static int Seats(int[] seats, int mask)
        {
            int total = 0;
            for (int p = 0; p < seats.Length; p++) { if ((mask & (1 << p)) != 0) { total += seats[p]; } }
            return total;
        }

        /// <summary>An absolute majority of the whole chamber: more than half the seats.</summary>
        public static int Majority(int[] seats)
        {
            int total = 0;
            foreach (int s in seats) { total += s; }
            return total / 2 + 1;
        }

        /// <summary>
        /// The Banzhaf pivotality share per party, summing to 1 across parties (0 for every party
        /// when no party is ever pivotal, which cannot happen for a chamber with a majority rule).
        /// </summary>
        public static double[] NegotiatingPower(int[] seats)
        {
            int n = seats.Length;
            if (n > 20) { throw new ArgumentException("the pivotality sweep is 2^n - not for a chamber of parties this size"); }
            int majority = Majority(seats);
            var swings = new double[n];
            double total = 0.0;
            for (int p = 0; p < n; p++)
            {
                int othersMask = ((1 << n) - 1) & ~(1 << p);
                for (int sub = othersMask; ; sub = (sub - 1) & othersMask)
                {
                    int without = Seats(seats, sub);
                    if (without < majority && without + seats[p] >= majority) { swings[p] += 1.0; total += 1.0; }
                    if (sub == 0) { break; }
                }
            }

            if (total <= 0.0) { return swings; }
            for (int p = 0; p < n; p++) { swings[p] /= total; }
            return swings;
        }
    }

    /// <summary>One government the formation considered, viable or not, with the reason.</summary>
    public readonly struct GovernmentOption
    {
        /// <summary>The parties IN CABINET, as a bit mask.</summary>
        public readonly int Cabinet;
        /// <summary>The parties supporting it from outside the cabinet, as a bit mask (0 = none).</summary>
        public readonly int Support;
        public readonly CoalitionOutcomeKind Kind;
        public readonly int CabinetSeats;
        public readonly int SupportedSeats;
        /// <summary>The seats that would vote AGAINST the cabinet's investiture - what negative parliamentarism actually tests.</summary>
        public readonly int OpposedSeats;
        /// <summary>Mean pairwise compatibility inside the cabinet (100 for a single-party cabinet).</summary>
        public readonly double Cohesion;
        public readonly double Score;

        public GovernmentOption(int cabinet, int support, CoalitionOutcomeKind kind, int cabinetSeats,
            int supportedSeats, int opposedSeats, double cohesion, double score)
        {
            Cabinet = cabinet; Support = support; Kind = kind; CabinetSeats = cabinetSeats;
            SupportedSeats = supportedSeats; OpposedSeats = opposedSeats; Cohesion = cohesion; Score = score;
        }
    }

    /// <summary>The formation's result: what government emerged, and everything it weighed to get there.</summary>
    public sealed class CoalitionResult
    {
        public CoalitionOutcomeKind Outcome;
        public GovernmentOption Government;
        /// <summary>Every viable option, best first - §31's "why" reads this.</summary>
        public List<GovernmentOption> Viable = new List<GovernmentOption>();
        /// <summary>Cabinets an absolute majority of seats would have formed, that a RED LINE refused. The done-when's second clause reads this list.</summary>
        public List<(int Cabinet, RedLine Line)> BlockedByRedLine = new List<(int, RedLine)>();
        public double[] NegotiatingPower;
        public int Majority;
    }

    /// <summary>
    /// §29's negotiation. **The chamber's own investiture rule is the mechanism, not a coalition
    /// score.** Sweden runs NEGATIVE PARLIAMENTARISM: a prime-ministerial candidate is elected
    /// unless an absolute majority of the whole Riksdag votes against — so a minority cabinet
    /// governs on the votes it does NOT provoke, and that is why Swedish minority governments are
    /// the norm rather than a special case the model has to arrange. the `negativeRule` parameter
    /// carries it; a chamber that requires positive investiture sets it false and every cabinet
    /// then needs a majority FOR it.
    ///
    /// The procedure, in full:
    /// 1. Every subset of parties is a candidate cabinet (the chamber is a handful of parties).
    /// 2. A cabinet is refused outright if any red line falls between two of its members.
    /// 3. Its supporters are the parties outside it that no `BlocksSupport` line separates from
    ///    any cabinet member — for a symmetric line support is refused in BOTH directions, because "I will not prop up
    ///    a government containing you" and "I will not be propped up by you" are both real; a one-way line (K-1) refuses
    ///    only from its A to its B, and never parts two supporters. A party with an in-or-against rule (K-1f) supports no cabinet
    ///    it is not in, and votes against every such cabinet at its investiture - SD's declared form (K-1g) only the first half.
    /// 4. The cabinet is viable if it wins its investiture: a majority for it, or under the
    ///    negative rule, fewer than an absolute majority against it. Parties that support it do
    ///    not vote against it; a party red-lined from it does, and so does one holding out for a cabinet of its own that could
    ///    pass (K-1h (i)); every other party abstains.
    /// 5. Viable cabinets are ranked, and the outcome is named by what it actually is — a majority
    ///    in cabinet, a minority with declared support, or a bare minority.
    /// 6. **If nothing is viable, the outcome is a NEW ELECTION.** That is a consequence of the
    ///    red lines and the arithmetic, never a designed-in branch: remove the red lines and the
    ///    same chamber forms a government.
    /// </summary>
    public static class CoalitionFormation
    {
        /// <summary>[AUTHORED-DRAFT] what a formation prefers, once the rules have decided what is POSSIBLE: a cohesive cabinet, a cabinet that does not need to be carried, and a cabinet built around the parties the arithmetic makes pivotal. Every weight strikeable; none of them can make an inadmissible government admissible.</summary>
        public const double WeightCohesion = 0.5;
        public const double WeightSeatStrength = 0.3;
        public const double WeightPower = 0.2;

        /// <summary>
        /// K-1h (i), ruled 2026-09-25: A PARTY HOLDS OUT ONLY FOR A CABINET THAT COULD PASS ITS OWN INVESTITURE - one that wins on the lines
        /// and the in-or-against rules alone, before anyone holds out. Until K-1h a party held out for any admissible cabinet of its own that
        /// scored higher, even one that could not pass (§29's pass 1, as built), and on the seated 2026 chamber every party but S held out
        /// that way, so the chamber formed nothing (§607).
        /// </summary>
        public static CoalitionResult Form(int[] seats, double[,] compatibility, IReadOnlyList<RedLine> redLines, bool negativeRule = true,
            IReadOnlyList<InOrAgainst> inOrAgainst = null)
        {
            // §646 (the formateur's evaluator): the chamber is PREPARED once and every admissible cabinet EVALUATED by the one routine a proposal
            // is judged by - so the formation and the formateur's investiture can never disagree. The formation reproduced exactly is pinned by
            // `FormationSweepDiagnostic`.
            Chamber chamber = Prepare(seats, compatibility, redLines, negativeRule, inOrAgainst);
            var result = new CoalitionResult
            {
                Majority = chamber.Majority,
                NegotiatingPower = chamber.Power,
            };
            result.BlockedByRedLine.AddRange(chamber.Blocked);

            // PASS 2 - support, opposition, and the investiture, cabinet by cabinet (Evaluate).
            foreach (int cabinet in chamber.Admissible)
            {
                CabinetEvaluation evaluation = Evaluate(chamber, cabinet);
                if (!evaluation.Wins) { continue; }
                result.Viable.Add(evaluation.AsOption());
            }

            // DEFECTION. Passing the investiture is not enough: a government also has to be one
            // nobody in or behind it would walk out of. Without this the formation returns the
            // BIGGEST admissible cabinet, because seats dominate the ranking - Sweden 2022 came
            // back as a five-party S+M+C+KD+L bloc of 234, which is arithmetic, not politics.
            //
            // A party's payoff from a government is DERIVED, not authored: its share of the
            // cabinet's seats (portfolios follow seat share - Gamson's law, an empirical
            // regularity, not a preference of ours) scaled by how well the cabinet agrees with
            // itself. A party outside the cabinet holds no portfolios and scores 0, so being IN a
            // government beats propping one up - which is why the small right parties sit in
            // cabinet rather than support from outside, and why the Sweden Democrats, who have no
            // admissible cabinet of their own to hold out for, support without office.
            //
            // Iterated to a fixed point: killing one government can remove the alternative another
            // party was holding out for. If a round would empty the set, the round is abandoned and
            // the survivors stand - a chamber where everyone can always do better elsewhere is a
            // statement about the payoff rule, not a reason to report a new election.
            int n = chamber.N;
            for (int round = 0; round < 32; round++)
            {
                var kept = new List<GovernmentOption>();
                foreach (GovernmentOption g in result.Viable)
                {
                    if (!WouldHold(g, result.Viable, seats, compatibility, n)) { continue; }
                    kept.Add(g);
                }

                if (kept.Count == 0 || kept.Count == result.Viable.Count) { break; }
                result.Viable = kept;
            }

            result.Viable.Sort((a, b) => b.Score.CompareTo(a.Score));
            if (result.Viable.Count == 0)
            {
                result.Outcome = CoalitionOutcomeKind.NewElection;
                return result;
            }

            result.Government = result.Viable[0];
            result.Outcome = result.Government.Kind;
            return result;
        }

        /// <summary>
        /// §646 (the formateur's evaluator): THE CHAMBER, PREPARED - everything <see cref="Form"/> reads of the whole chamber before it weighs one
        /// cabinet: which cabinets are admissible (no red line inside) and each one's own score, which of them pass their investiture on the lines
        /// and the party rules alone, and the best passable cabinet each party could sit in - what it holds out for (K-1h (i)).
        /// </summary>
        public sealed class Chamber
        {
            public int N;
            public int[] Seats;
            public double[,] Compatibility;
            public IReadOnlyList<RedLine> Lines;
            public bool NegativeRule;
            /// <summary>K-1f: the parties that support no cabinet they are not in; K-1g: of those, the ones that also vote against every such cabinet.</summary>
            public int NoSupportMask;
            public int InOrAgainstMask;
            public IReadOnlyList<InOrAgainst> Rules;
            public int Majority;
            public int TotalSeats;
            public double[] Power;
            public readonly List<int> Admissible = new List<int>();
            public double[] BaseScore;
            public bool[] PassesOnLines;
            public double[] BestOwn;
            public readonly List<(int Cabinet, RedLine Line)> Blocked = new List<(int, RedLine)>();
        }

        /// <summary>§646: prepare a chamber for evaluation - the formation's pass 1, its passable set and each party's hold-out, as <see cref="Form"/> computes them.</summary>
        public static Chamber Prepare(int[] seats, double[,] compatibility, IReadOnlyList<RedLine> redLines, bool negativeRule = true,
            IReadOnlyList<InOrAgainst> inOrAgainst = null)
        {
            if (seats == null) { throw new ArgumentNullException(nameof(seats)); }
            int n = seats.Length;
            // K-1f: the parties that support no cabinet they are not in; K-1g: of those, the ones that also vote against every such cabinet.
            int noSupportMask = InOrAgainst.Mask(inOrAgainst, n, votingAgainstOnly: false);
            int inOrAgainstMask = InOrAgainst.Mask(inOrAgainst, n, votingAgainstOnly: true);
            if (compatibility.GetLength(0) != n || compatibility.GetLength(1) != n) { throw new ArgumentException("compatibility must be party by party"); }
            var lines = redLines ?? new List<RedLine>();
            var chamber = new Chamber
            {
                N = n, Seats = seats, Compatibility = compatibility, Lines = lines, NegativeRule = negativeRule,
                NoSupportMask = noSupportMask, InOrAgainstMask = inOrAgainstMask, Rules = inOrAgainst ?? new List<InOrAgainst>(),
                Majority = CoalitionMath.Majority(seats),
                Power = CoalitionMath.NegotiatingPower(seats),
            };

            int all = (1 << n) - 1;
            int totalSeats = CoalitionMath.Seats(seats, all);
            chamber.TotalSeats = totalSeats;

            // PASS 1 - which cabinets are admissible at all, and what each is worth on its own
            // terms. The score uses only cohesion, seats and pivotality, so it does NOT depend on
            // who supports whom; that is what lets pass 2 ask "would this party rather have a
            // different government?" without the question chasing its own tail.
            var baseScore = new double[all + 1];
            for (int cabinet = 1; cabinet <= all; cabinet++)
            {
                int cabinetSeats = CoalitionMath.Seats(seats, cabinet);
                if (TryFindInternalRedLine(cabinet, n, lines, out RedLine broken))
                {
                    // A cabinet an absolute majority of seats would have carried, refused by a red
                    // line: the done-when's second clause reads exactly this list.
                    if (cabinetSeats >= chamber.Majority) { chamber.Blocked.Add((cabinet, broken)); }
                    continue;
                }

                chamber.Admissible.Add(cabinet);
                baseScore[cabinet] = WeightCohesion * Cohesion(cabinet, n, compatibility)
                    + WeightSeatStrength * 100.0 * cabinetSeats / Math.Max(1, totalSeats)
                    + WeightPower * 100.0 * PowerOf(cabinet, n, chamber.Power);
            }
            chamber.BaseScore = baseScore;

            // K-1h (i): which admissible cabinets pass their investiture on the lines and rules alone, before any party holds out - the only
            // cabinets a party holds out for.
            var passesOnLines = new bool[all + 1];
            foreach (int cabinet in chamber.Admissible)
            {
                int support = SupportersOf(cabinet, n, lines, compatibility, chamber.Power, noSupportMask);
                int opposeMask = 0;
                for (int p = 0; p < n; p++)
                {
                    if ((cabinet & (1 << p)) != 0 || (support & (1 << p)) != 0) { continue; }
                    if (SupportBlocked(p, cabinet, n, lines) || (inOrAgainstMask & (1 << p)) != 0) { opposeMask |= 1 << p; }
                }
                int supported = CoalitionMath.Seats(seats, cabinet) + CoalitionMath.Seats(seats, support);
                passesOnLines[cabinet] = supported >= chamber.Majority || (negativeRule && CoalitionMath.Seats(seats, opposeMask) < chamber.Majority);
            }
            chamber.PassesOnLines = passesOnLines;

            // The best government each party could hope to sit in - what it is holding out for: a cabinet of its own that could pass (K-1h (i)).
            var bestOwn = new double[n];
            for (int p = 0; p < n; p++) { bestOwn[p] = double.NegativeInfinity; }
            foreach (int cabinet in chamber.Admissible)
            {
                if (!passesOnLines[cabinet]) { continue; }
                for (int p = 0; p < n; p++)
                {
                    if ((cabinet & (1 << p)) != 0 && baseScore[cabinet] > bestOwn[p]) { bestOwn[p] = baseScore[cabinet]; }
                }
            }
            chamber.BestOwn = bestOwn;
            return chamber;
        }

        /// <summary>§646: one party's side at a cabinet's investiture, and why - the formation's own reasons, never an authored willingness.</summary>
        public enum InvestitureSide { InCabinet, Supports, Against, Abstains }

        /// <summary>§646: ONE CABINET'S INVESTITURE, as the formation weighs it: who supports it, who votes against and why, whether it wins, and what kind of government it is.</summary>
        public sealed class CabinetEvaluation
        {
            public int Cabinet;
            public int SupportMask;
            public int OpposeMask;
            public int CabinetSeats;
            public int SupportedSeats;
            public int OpposedSeats;
            /// <summary>False when a red line falls inside the cabinet - <see cref="InternalLine"/> names it.</summary>
            public bool Admissible;
            public RedLine InternalLine;
            public bool Wins;
            public CoalitionOutcomeKind Kind;
            public double Cohesion;
            public double Score;
            public InvestitureSide[] Sides;
            /// <summary>Each party's reason, in the formation's own terms; the red line's basis where one decides it.</summary>
            public string[] Reasons;

            public GovernmentOption AsOption() => new GovernmentOption(Cabinet, SupportMask, Kind, CabinetSeats, SupportedSeats, OpposedSeats, Cohesion, Score);
        }

        /// <summary>
        /// §646: EVALUATE ONE CABINET in a prepared chamber - pass 2 of the formation, for one cabinet. <paramref name="support"/> null derives the
        /// supporters as the formation does; a mask (the formateur's proposal) takes those parties as supporting it.
        /// </summary>
        public static CabinetEvaluation Evaluate(Chamber chamber, int cabinet, int? support = null)
        {
            int n = chamber.N;
            int[] seats = chamber.Seats;
            var e = new CabinetEvaluation { Cabinet = cabinet, Sides = new InvestitureSide[n], Reasons = new string[n] };
            e.Admissible = !TryFindInternalRedLine(cabinet, n, chamber.Lines, out RedLine inside);
            if (!e.Admissible) { e.InternalLine = inside; }
            int cabinetSeats = CoalitionMath.Seats(seats, cabinet);
            int supportMask = support.HasValue ? support.Value & ~cabinet : SupportersOf(cabinet, n, chamber.Lines, chamber.Compatibility, chamber.Power, chamber.NoSupportMask);
            int supported = cabinetSeats + CoalitionMath.Seats(seats, supportMask);
            double score = e.Admissible ? chamber.BaseScore[cabinet] : double.NaN;

            // Who actually votes AGAINST. A party red-lined from the cabinet does; so does one
            // holding out for a government it prefers and could be part of - one that could pass its own
            // investiture on the lines and rules alone (K-1h (i)). Everyone else
            // ABSTAINS - which is the whole point of negative parliamentarism, and without it
            // the rule would be arithmetic in disguise (opposed < majority would just be
            // supported >= majority restated).
            int opposeMask = 0;
            for (int p = 0; p < n; p++)
            {
                if ((cabinet & (1 << p)) != 0) { e.Sides[p] = InvestitureSide.InCabinet; e.Reasons[p] = "in the cabinet"; continue; }
                if ((supportMask & (1 << p)) != 0) { e.Sides[p] = InvestitureSide.Supports; e.Reasons[p] = "supports it from outside"; continue; }
                // A party that will not SUPPORT you votes against you. A party that merely
                // will not SIT with you can still tolerate you from outside - which is the
                // whole Tido arrangement, so conflating the two would erase it.
                // K-1f: an in-or-against party is never outside a cabinet it tolerates - outside one, it votes against (K-1g: V's and MP's
                // form; SD's refuses support only, and its vote is the lines' and the hold-out's).
                bool blocked = SupportBlocked(p, cabinet, n, chamber.Lines);
                bool inOrAgainst = (chamber.InOrAgainstMask & (1 << p)) != 0;
                bool holdsOut = e.Admissible && chamber.BestOwn[p] > score;
                if (blocked || inOrAgainst || holdsOut)
                {
                    opposeMask |= 1 << p;
                    e.Sides[p] = InvestitureSide.Against;
                    e.Reasons[p] = blocked ? "a red line against a cabinet party - " + SupportBlockBasis(p, cabinet, n, chamber.Lines)
                        : inOrAgainst ? "votes against every cabinet it is not in - " + RuleBasis(chamber.Rules, p)
                        : "holds out for a cabinet of its own that could pass";
                    continue;
                }
                e.Sides[p] = InvestitureSide.Abstains;
                e.Reasons[p] = "abstains - no line bars its support, and it holds out for nothing it could pass";
            }

            int opposed = CoalitionMath.Seats(seats, opposeMask);
            e.SupportMask = supportMask;
            e.OpposeMask = opposeMask;
            e.CabinetSeats = cabinetSeats;
            e.SupportedSeats = supported;
            e.OpposedSeats = opposed;
            e.Score = score;
            e.Cohesion = Cohesion(cabinet, n, chamber.Compatibility);
            e.Wins = e.Admissible && (supported >= chamber.Majority || (chamber.NegativeRule && opposed < chamber.Majority));
            e.Kind = cabinetSeats >= chamber.Majority ? CoalitionOutcomeKind.MajorityCoalition
                : supportMask != 0 && supported >= chamber.Majority ? CoalitionOutcomeKind.ConfidenceAndSupply
                : CoalitionOutcomeKind.MinorityGovernment;
            return e;
        }

        /// <summary>
        /// §646: why party <paramref name="p"/> would refuse to support <paramref name="cabinet"/> from outside, on the formation's first conditions of
        /// support (<see cref="SupportersOf"/>): a support-blocking line to a cabinet member, its own rule refusing the support role (K-1f, K-1g), or a
        /// party left outside suiting it better. Null where it would support. The second condition - supporters tolerating each other - is
        /// <see cref="SharedSupportRefusal"/>, asked among the parties that pass this one, as the formation asks it.
        /// </summary>
        public static string SupportRefusal(Chamber chamber, int p, int cabinet)
        {
            int n = chamber.N;
            if (SupportBlocked(p, cabinet, n, chamber.Lines)) { return "a red line against a cabinet party - " + SupportBlockBasis(p, cabinet, n, chamber.Lines); }
            if ((chamber.NoSupportMask & (1 << p)) != 0) { return "supports no cabinet it is not in - " + RuleBasis(chamber.Rules, p); }
            double toCabinet = MeanCompatibility(p, cabinet, n, chamber.Compatibility);
            double bestOutside = double.NegativeInfinity;
            int bestParty = -1;
            for (int q = 0; q < n; q++)
            {
                if (q == p || (cabinet & (1 << q)) != 0) { continue; }
                if (!double.IsNaN(chamber.Compatibility[p, q]) && chamber.Compatibility[p, q] > bestOutside) { bestOutside = chamber.Compatibility[p, q]; bestParty = q; }
            }
            if (double.IsNaN(toCabinet)) { return "no compatibility with the cabinet is defined"; }
            if (bestOutside > double.NegativeInfinity && toCabinet < bestOutside)
            {
                return string.Format(System.Globalization.CultureInfo.InvariantCulture, "a party left outside the cabinet suits it better ({0:0.0} against {1:0.0} with the cabinet; party index {2})", bestOutside, toCabinet, bestParty);
            }
            return null;
        }

        /// <summary>
        /// §646: the formation's second condition of support, for a proposal - among <paramref name="survivors"/> (the proposed supporters that pass
        /// <see cref="SupportRefusal"/>), supporters that red-line each other cannot both stay; the weaker by negotiating power leaves, the later index on
        /// a tie, repeated to a fixed point as <see cref="SupportersOf"/> does. Returns the mask that stays.
        /// </summary>
        public static int SharedSupport(Chamber chamber, int survivors)
        {
            int n = chamber.N;
            int support = survivors;
            bool changed = true;
            while (changed)
            {
                changed = false;
                for (int i = 0; i < chamber.Lines.Count && !changed; i++)
                {
                    RedLine line = chamber.Lines[i];
                    if (!line.BlocksSupport || line.OneWay || line.A >= n || line.B >= n) { continue; }
                    if ((support & (1 << line.A)) == 0 || (support & (1 << line.B)) == 0) { continue; }
                    int weaker = chamber.Power[line.A] < chamber.Power[line.B] ? line.A
                        : chamber.Power[line.B] < chamber.Power[line.A] ? line.B
                        : Math.Max(line.A, line.B);
                    support &= ~(1 << weaker);
                    changed = true;
                }
            }
            return support;
        }

        /// <summary>The basis of the first support-blocking line that parts a party from a cabinet member.</summary>
        private static string SupportBlockBasis(int p, int cabinet, int n, IReadOnlyList<RedLine> lines)
        {
            for (int i = 0; i < lines.Count; i++)
            {
                RedLine line = lines[i];
                if (!line.BlocksSupport) { continue; }
                for (int q = 0; q < n; q++) { if ((cabinet & (1 << q)) != 0 && line.RefusesSupport(p, q)) { return line.Basis; } }
            }
            return "no line found";
        }

        private static string RuleBasis(IReadOnlyList<InOrAgainst> rules, int p)
        {
            foreach (InOrAgainst rule in rules) { if (rule.Party == p) { return rule.Basis; } }
            return "no rule found";
        }

        /// <summary>[AUTHORED-DRAFT] the margin by which a party must do better elsewhere before it walks - below it, two governments are the same offer and nobody moves.</summary>
        public const double DefectionMargin = 0.01;

        /// <summary>What a party gets out of a government: its share of the cabinet's seats (portfolios follow seat share) scaled by the cabinet's own agreement. Zero for a party outside the cabinet - support buys no portfolios.</summary>
        public static double Payoff(int party, GovernmentOption g, int[] seats, double[,] compatibility, int n)
        {
            if ((g.Cabinet & (1 << party)) == 0) { return 0.0; }
            int cabinetSeats = CoalitionMath.Seats(seats, g.Cabinet);
            if (cabinetSeats <= 0) { return 0.0; }
            return (double)seats[party] / cabinetSeats * (Cohesion(g.Cabinet, n, compatibility) / 100.0);
        }

        /// <summary>
        /// §646: THE GOVERNMENTS THAT WOULD HOLD - of the formation's viable governments, those no party in or behind would walk out of (<see cref="WouldHold"/>);
        /// where the defection rounds were abandoned and none holds, the government the formation stands on alone. What a party answering a proposal
        /// compares it against: never a government the formation itself would not settle on.
        /// </summary>
        public static List<GovernmentOption> Holding(CoalitionResult result, int[] seats, double[,] compatibility)
        {
            var holding = new List<GovernmentOption>();
            int n = seats.Length;
            foreach (GovernmentOption g in result.Viable) { if (WouldHold(g, result.Viable, seats, compatibility, n)) { holding.Add(g); } }
            if (holding.Count == 0 && result.Outcome != CoalitionOutcomeKind.NewElection) { holding.Add(result.Government); }
            return holding;
        }

        /// <summary>Whether every party in or behind a government does at least as well there as in any other government still standing.</summary>
        private static bool WouldHold(GovernmentOption g, List<GovernmentOption> others, int[] seats, double[,] compatibility, int n)
        {
            int involved = g.Cabinet | g.Support;
            for (int p = 0; p < n; p++)
            {
                if ((involved & (1 << p)) == 0) { continue; }
                double here = Payoff(p, g, seats, compatibility, n);
                foreach (GovernmentOption alt in others)
                {
                    if (alt.Cabinet == g.Cabinet && alt.Support == g.Support) { continue; }
                    if (Payoff(p, alt, seats, compatibility, n) > here + DefectionMargin) { return false; }
                }
            }

            return true;
        }

        /// <summary>The first red line falling between two members of the cabinet, if any.</summary>
        private static bool TryFindInternalRedLine(int cabinet, int n, IReadOnlyList<RedLine> lines, out RedLine found)
        {
            for (int i = 0; i < lines.Count; i++)
            {
                RedLine line = lines[i];
                if (line.A < n && line.B < n && (cabinet & (1 << line.A)) != 0 && (cabinet & (1 << line.B)) != 0)
                {
                    found = line; return true;
                }
            }

            found = default; return false;
        }

        /// <summary>
        /// Who would support a cabinet from outside it. Absence of a red line is NOT support — on
        /// that rule every party in a Western chamber supports every government, because a party
        /// system's pairwise distances are mostly small. Two conditions, both from §29's own list:
        ///
        /// 1. **No better partner outside.** A party supports a cabinet only if that cabinet suits
        ///    it at least as well as any party left out of it does. The Social Democrats are not
        ///    red-lined from the 2022 right bloc and are 70.2 compatible with it — but they are
        ///    75.9 compatible with the Left, who is outside it, so they do not prop it up. This is
        ///    a comparison, not a threshold: no constant is chosen, and none can be tuned.
        /// 2. **Supporters must tolerate each other.** A party that will not depend on another will
        ///    not join a support arrangement containing it. Where two candidate supporters red-line
        ///    each other, the one with the greater NEGOTIATING POWER (§29's own term, the Banzhaf
        ///    pivotality of the seat distribution) stays and the other drops — which is what a
        ///    negotiation is. In 2022 that keeps the Sweden Democrats (73 seats, pivotal to every
        ///    right majority) and drops the Centre Party (24, pivotal to none of them), reproducing
        ///    the actual outcome from the arithmetic rather than from a stored answer.
        /// </summary>
        private static int SupportersOf(int cabinet, int n, IReadOnlyList<RedLine> lines,
            double[,] compatibility, double[] power, int noSupportMask = 0)
        {
            int support = 0;
            for (int p = 0; p < n; p++)
            {
                if ((cabinet & (1 << p)) != 0) { continue; }
                if (SupportBlocked(p, cabinet, n, lines)) { continue; }
                if ((noSupportMask & (1 << p)) != 0) { continue; }   // K-1f/K-1g: it supports no cabinet it is not in

                double toCabinet = MeanCompatibility(p, cabinet, n, compatibility);
                double bestOutside = double.NegativeInfinity;
                for (int q = 0; q < n; q++)
                {
                    if (q == p || (cabinet & (1 << q)) != 0) { continue; }
                    if (!double.IsNaN(compatibility[p, q]) && compatibility[p, q] > bestOutside) { bestOutside = compatibility[p, q]; }
                }

                if (double.IsNaN(toCabinet)) { continue; }
                if (bestOutside > double.NegativeInfinity && toCabinet < bestOutside) { continue; }
                support |= 1 << p;
            }

            // 2. supporters that red-line each other cannot both stay; the weaker leaves, and
            // removing one can free nothing, so a single pass to a fixed point is enough.
            bool changed = true;
            while (changed)
            {
                changed = false;
                for (int i = 0; i < lines.Count && !changed; i++)
                {
                    RedLine line = lines[i];
                    // K-1: a one-way line refuses the other party's CABINET, never its company among the supporters.
                    if (!line.BlocksSupport || line.OneWay || line.A >= n || line.B >= n) { continue; }
                    if ((support & (1 << line.A)) == 0 || (support & (1 << line.B)) == 0) { continue; }
                    int weaker = power[line.A] < power[line.B] ? line.A
                        : power[line.B] < power[line.A] ? line.B
                        : Math.Max(line.A, line.B);   // equal power: the later index leaves, so the result is deterministic
                    support &= ~(1 << weaker);
                    changed = true;
                }
            }

            return support;
        }

        /// <summary>
        /// PS-3i (§636): THE PARTIES RED-LINED FROM A SITTING CABINET - the same rule the investiture's opposition reads (a support-blocking line to any
        /// cabinet member, or an in-or-against rule that votes against, outside the cabinet - K-1g), exposed for the confidence motion: a party that would vote against the cabinet
        /// at its investiture votes for no confidence in it. The hold-out term is the investiture's alone (a party holding out for a cabinet it prefers
        /// has nothing to hold out for once one sits) - stated.
        /// </summary>
        public static int RedLinedMask(int cabinet, int n, IReadOnlyList<RedLine> lines, IReadOnlyList<InOrAgainst> inOrAgainst)
        {
            int inOrAgainstMask = InOrAgainst.Mask(inOrAgainst, n, votingAgainstOnly: true);
            int mask = 0;
            for (int p = 0; p < n; p++)
            {
                if ((cabinet & (1 << p)) != 0) { continue; }
                if (SupportBlocked(p, cabinet, n, lines) || (inOrAgainstMask & (1 << p)) != 0) { mask |= 1 << p; }
            }
            return mask;
        }

        /// <summary>Whether a support-blocking red line separates a party from any cabinet member - a one-way line only in its own direction (K-1).</summary>
        private static bool SupportBlocked(int p, int cabinet, int n, IReadOnlyList<RedLine> lines)
        {
            for (int i = 0; i < lines.Count; i++)
            {
                RedLine line = lines[i];
                if (!line.BlocksSupport) { continue; }
                for (int q = 0; q < n; q++)
                {
                    if ((cabinet & (1 << q)) != 0 && line.RefusesSupport(p, q)) { return true; }
                }
            }

            return false;
        }

        /// <summary>A party's mean compatibility with a cabinet's members; NaN when none is defined.</summary>
        public static double MeanCompatibility(int p, int cabinet, int n, double[,] compatibility)
        {
            double sum = 0.0; int k = 0;
            for (int q = 0; q < n; q++)
            {
                if (q == p || (cabinet & (1 << q)) == 0 || double.IsNaN(compatibility[p, q])) { continue; }
                sum += compatibility[p, q]; k++;
            }

            return k == 0 ? double.NaN : sum / k;
        }

        /// <summary>Mean pairwise compatibility inside a cabinet; 100 for one party alone (it agrees with itself), and undefined pairs are skipped rather than centred.</summary>
        public static double Cohesion(int cabinet, int n, double[,] compatibility)
        {
            double sum = 0.0; int pairs = 0;
            for (int a = 0; a < n; a++)
            {
                if ((cabinet & (1 << a)) == 0) { continue; }
                for (int b = a + 1; b < n; b++)
                {
                    if ((cabinet & (1 << b)) == 0) { continue; }
                    if (double.IsNaN(compatibility[a, b])) { continue; }
                    sum += compatibility[a, b]; pairs++;
                }
            }

            return pairs == 0 ? 100.0 : sum / pairs;
        }

        private static double PowerOf(int cabinet, int n, double[] power)
        {
            double s = 0.0;
            for (int p = 0; p < n; p++) { if ((cabinet & (1 << p)) != 0) { s += power[p]; } }
            return s;
        }

        /// <summary>The parties in a mask, as their indices - for a harness's report line.</summary>
        public static List<int> Members(int mask, int n)
        {
            var m = new List<int>();
            for (int p = 0; p < n; p++) { if ((mask & (1 << p)) != 0) { m.Add(p); } }
            return m;
        }
    }

    /// <summary>
    /// Red lines DERIVED from sourced positions: two parties refuse each other when they stand
    /// further apart than a threshold on an axis. Nothing here is per-pair — the rule is the same
    /// for every pair and every country, which is the whole point of deriving it.
    ///
    /// **What the thresholds are, honestly.** They are [AUTHORED-DRAFT], and they were chosen by
    /// looking at Sweden 2022's KNOWN exclusions. So this rule is CALIBRATED on that case and is
    /// not evidence that it predicts it — a two-threshold rule over seven pairs can be fitted to
    /// almost anything, and a harness that reported "the derived lines reproduce Sweden" as a
    /// success would be reporting its own fitting. What can be measured WITHOUT circularity is
    /// the slack, and the harness measures it: over what window of single-axis thresholds do the
    /// sourced positions separate exactly the parties that really refused the Sweden Democrats
    /// from exactly those that really governed with them? A wide window means the data carries
    /// the distinction; no window means only the fitting did. The harness prints the bounds.
    ///
    /// <see cref="RedLineKind.Declared"/> does not exist because the derived rule cannot reach a
    /// particular pair — it may well reach all of them. It exists because a declaration is a
    /// DATED FACT that can be WITHDRAWN while no position moves: the Liberals refused a government
    /// dependent on the Sweden Democrats before 2018 and signed the Tidö agreement with them in
    /// 2022, and no distance on any axis changed to license that. A model with only derived lines
    /// cannot express it.
    /// </summary>
    public static class DerivedRedLines
    {
        /// <summary>[AUTHORED-DRAFT] a gap on CHES `lrgen` past which two parties will not sit together. Calibrated on Sweden 2022 - see the class doc, which says so plainly.</summary>
        public const double IdeologicalGap = 4.5;

        /// <summary>[AUTHORED-DRAFT] a gap on CHES `galtan` past which two parties will not sit together, nor support each other. The social axis is the one Sweden's cordon was argued on, and a party that refuses on it refuses support too.</summary>
        public const double SocialGap = 5.0;

        /// <summary>
        /// Every pair further apart than a threshold, as red lines. A pair past the SOCIAL gap
        /// blocks support as well as the cabinet; a pair past the ideological gap alone blocks
        /// only the cabinet — parties that are far apart left-to-right still trade votes, and
        /// parties that hold each other beyond the pale do not.
        /// </summary>
        public static List<RedLine> From(double[] lrgen, double[] galtan,
            double ideologicalGap = IdeologicalGap, double socialGap = SocialGap)
        {
            if (lrgen == null || galtan == null || lrgen.Length != galtan.Length) { throw new ArgumentException("one lrgen and one galtan per party"); }
            var lines = new List<RedLine>();
            for (int a = 0; a < lrgen.Length; a++)
            {
                for (int b = a + 1; b < lrgen.Length; b++)
                {
                    double social = Math.Abs(galtan[a] - galtan[b]);
                    double ideological = Math.Abs(lrgen[a] - lrgen[b]);
                    bool socialBreak = !double.IsNaN(social) && social > socialGap;
                    bool ideologicalBreak = !double.IsNaN(ideological) && ideological > ideologicalGap;
                    if (!socialBreak && !ideologicalBreak) { continue; }
                    string basis = socialBreak
                        ? string.Format(System.Globalization.CultureInfo.InvariantCulture, "DERIVED: CHES galtan gap {0:F2} > {1:F2}", social, socialGap)
                        : string.Format(System.Globalization.CultureInfo.InvariantCulture, "DERIVED: CHES lrgen gap {0:F2} > {1:F2}", ideological, ideologicalGap);
                    lines.Add(new RedLine(a, b, RedLineKind.Derived, socialBreak, basis));
                }
            }

            return lines;
        }
    }
}
