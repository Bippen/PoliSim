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
    /// </summary>
    public readonly struct RedLine
    {
        public readonly int A;
        public readonly int B;
        public readonly RedLineKind Kind;
        public readonly bool BlocksSupport;
        /// <summary>The derivation, or the citation and its vintage. Never empty — a red line without a basis is an authored coalition score, which §29 must not have.</summary>
        public readonly string Basis;

        public RedLine(int a, int b, RedLineKind kind, bool blocksSupport, string basis)
        {
            if (a == b) { throw new ArgumentException("a party cannot red-line itself"); }
            if (string.IsNullOrEmpty(basis)) { throw new ArgumentException("a red line needs its basis"); }
            A = a; B = b; Kind = kind; BlocksSupport = blocksSupport; Basis = basis;
        }

        public bool Covers(int x, int y) => (A == x && B == y) || (A == y && B == x);
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
    ///    any cabinet member — support is refused in BOTH directions, because "I will not prop up
    ///    a government containing you" and "I will not be propped up by you" are both real.
    /// 4. The cabinet is viable if it wins its investiture: a majority for it, or under the
    ///    negative rule, fewer than an absolute majority against it. Parties that support it do
    ///    not vote against it; every other party does.
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

        public static CoalitionResult Form(int[] seats, double[,] compatibility, IReadOnlyList<RedLine> redLines, bool negativeRule = true)
        {
            if (seats == null) { throw new ArgumentNullException(nameof(seats)); }
            int n = seats.Length;
            if (compatibility.GetLength(0) != n || compatibility.GetLength(1) != n) { throw new ArgumentException("compatibility must be party by party"); }
            var lines = redLines ?? new List<RedLine>();
            var result = new CoalitionResult
            {
                Majority = CoalitionMath.Majority(seats),
                NegotiatingPower = CoalitionMath.NegotiatingPower(seats),
            };

            int all = (1 << n) - 1;
            int totalSeats = CoalitionMath.Seats(seats, all);

            // PASS 1 - which cabinets are admissible at all, and what each is worth on its own
            // terms. The score uses only cohesion, seats and pivotality, so it does NOT depend on
            // who supports whom; that is what lets pass 2 ask "would this party rather have a
            // different government?" without the question chasing its own tail.
            var admissible = new List<int>();
            var baseScore = new double[all + 1];
            for (int cabinet = 1; cabinet <= all; cabinet++)
            {
                int cabinetSeats = CoalitionMath.Seats(seats, cabinet);
                if (TryFindInternalRedLine(cabinet, n, lines, out RedLine broken))
                {
                    // A cabinet an absolute majority of seats would have carried, refused by a red
                    // line: the done-when's second clause reads exactly this list.
                    if (cabinetSeats >= result.Majority) { result.BlockedByRedLine.Add((cabinet, broken)); }
                    continue;
                }

                admissible.Add(cabinet);
                baseScore[cabinet] = WeightCohesion * Cohesion(cabinet, n, compatibility)
                    + WeightSeatStrength * 100.0 * cabinetSeats / Math.Max(1, totalSeats)
                    + WeightPower * 100.0 * PowerOf(cabinet, n, result.NegotiatingPower);
            }

            // The best government each party could hope to sit in - what it is holding out for.
            var bestOwn = new double[n];
            for (int p = 0; p < n; p++) { bestOwn[p] = double.NegativeInfinity; }
            foreach (int cabinet in admissible)
            {
                for (int p = 0; p < n; p++)
                {
                    if ((cabinet & (1 << p)) != 0 && baseScore[cabinet] > bestOwn[p]) { bestOwn[p] = baseScore[cabinet]; }
                }
            }

            // PASS 2 - support, opposition, and the investiture.
            foreach (int cabinet in admissible)
            {
                int cabinetSeats = CoalitionMath.Seats(seats, cabinet);
                int support = SupportersOf(cabinet, n, lines, compatibility, result.NegotiatingPower);
                int supported = cabinetSeats + CoalitionMath.Seats(seats, support);

                // Who actually votes AGAINST. A party red-lined from the cabinet does; so does one
                // holding out for a government it prefers and could be part of. Everyone else
                // ABSTAINS - which is the whole point of negative parliamentarism, and without it
                // the rule would be arithmetic in disguise (opposed < majority would just be
                // supported >= majority restated).
                int opposeMask = 0;
                for (int p = 0; p < n; p++)
                {
                    if ((cabinet & (1 << p)) != 0 || (support & (1 << p)) != 0) { continue; }
                    // A party that will not SUPPORT you votes against you. A party that merely
                    // will not SIT with you can still tolerate you from outside - which is the
                    // whole Tido arrangement, so conflating the two would erase it.
                    bool redLined = SupportBlocked(p, cabinet, n, lines);
                    if (redLined || bestOwn[p] > baseScore[cabinet]) { opposeMask |= 1 << p; }
                }

                int opposed = CoalitionMath.Seats(seats, opposeMask);
                bool wins = supported >= result.Majority || (negativeRule && opposed < result.Majority);
                if (!wins) { continue; }

                CoalitionOutcomeKind kind = cabinetSeats >= result.Majority ? CoalitionOutcomeKind.MajorityCoalition
                    : support != 0 && supported >= result.Majority ? CoalitionOutcomeKind.ConfidenceAndSupply
                    : CoalitionOutcomeKind.MinorityGovernment;

                result.Viable.Add(new GovernmentOption(cabinet, support, kind, cabinetSeats, supported, opposed,
                    Cohesion(cabinet, n, compatibility), baseScore[cabinet]));
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
            double[,] compatibility, double[] power)
        {
            int support = 0;
            for (int p = 0; p < n; p++)
            {
                if ((cabinet & (1 << p)) != 0) { continue; }
                if (SupportBlocked(p, cabinet, n, lines)) { continue; }

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
                    if (!line.BlocksSupport || line.A >= n || line.B >= n) { continue; }
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

        /// <summary>Whether a support-blocking red line separates a party from any cabinet member.</summary>
        private static bool SupportBlocked(int p, int cabinet, int n, IReadOnlyList<RedLine> lines)
        {
            for (int i = 0; i < lines.Count; i++)
            {
                RedLine line = lines[i];
                if (!line.BlocksSupport) { continue; }
                for (int q = 0; q < n; q++)
                {
                    if ((cabinet & (1 << q)) != 0 && line.Covers(p, q)) { return true; }
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
