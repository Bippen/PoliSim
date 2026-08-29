using System;
using System.Collections.Generic;

namespace PoliSim.Elections
{
    /// <summary>
    /// W-B7 / SPEC §15 (debates) with §16's attributes — a debate as a sequence of EXCHANGES,
    /// each a pair of moves resolved from the candidates' attributes, their preparation, their
    /// ownership of the exchange's topic, the opponent's move, and one seeded draw. PURE
    /// FUNCTIONS, WIRED TO NOTHING (R-N2).
    ///
    /// **What a debate produces, and what it cannot.** A <see cref="DebateResult"/> carries each
    /// candidate's performance index, the winner and the margin, and two SHOCKS: a coverage shock
    /// (raw newsworthiness for `MediaCoverage.AddShock`, §13) and a momentum shock in percentage
    /// points (for `MomentumTracker.AddShock`, §22). It has no share, no preference, no party
    /// member — the same architectural bar W-B3 set for actions, asserted by reflection — so a
    /// debate can move where the race APPEARS to be and how much the press talks about it, and
    /// nothing else until those two mechanisms carry it into the model.
    ///
    /// **Performance, per exchange** (§15's list, each a term):
    /// <code>
    /// skill      = the move's attribute blend (§16: debate skill, charisma, policy knowledge, communication, popularity)
    /// prepared   = 0.7 + 0.3 × §35(preparation hours / PreparationScale)     - preparation is a multiplier, never a substitute
    /// ownership  = 0.8 + 0.4 × topic ownership (0-1)                        - a candidate on their own issue is a fifth stronger
    /// clash      = the move-pair table (attack into ignore is wasted; attack into a weak defence lands; counterattack beats an attack from a weaker debater)
    /// event      = a seeded draw on the exchange, ± EventSigma of a point       - §15's "random event", small
    /// points     = skill × prepared × ownership × clash + event
    /// </code>
    /// The performance index is the candidate's mean exchange points (0–100). **The margin is the
    /// difference**, and both shocks scale with it: a close debate makes little news and moves
    /// nothing; a rout does both — bounded by the index's own range.
    ///
    /// **Determinism.** The `System.Random` is the caller's — the harness passes `SimulationRandom`'s
    /// appended `Debate` stream — and the same seed with the same moves reproduces every exchange.
    ///
    /// **[AUTHORED-DRAFT] throughout** (R-N4): the attribute blends per move, the clash table, the
    /// preparation scale and floor, the ownership span, `EventSigma`, the two shock rates. Candidate
    /// attributes are W-F6's (game fiction, labelled); the harness stages two.
    /// </summary>
    public enum DebateMove
    {
        AttackOpponent = 0,
        DefendPolicy = 1,
        ChangeSubject = 2,
        AppealEmotionally = 3,
        PresentStatistics = 4,
        IgnoreAttack = 5,
        Counterattack = 6,
    }

    /// <summary>§15's pre-debate choices, as one candidate makes them.</summary>
    public readonly struct DebatePreparation
    {
        public readonly double Hours;
        /// <summary>The topics emphasised — the candidate's ownership of each is what they bring to an exchange on it.</summary>
        public readonly IssueId[] Topics;
        public readonly DebateMove[] Plan;

        public DebatePreparation(double hours, IssueId[] topics, DebateMove[] plan)
        {
            Hours = hours; Topics = topics ?? new IssueId[0]; Plan = plan ?? new DebateMove[0];
        }
    }

    /// <summary>One exchange as resolved: the topic, both moves, both points, the event draw.</summary>
    public readonly struct DebateExchange
    {
        public readonly IssueId Topic;
        public readonly DebateMove MoveA;
        public readonly DebateMove MoveB;
        public readonly double PointsA;
        public readonly double PointsB;
        public readonly double EventA;
        public readonly double EventB;

        public DebateExchange(IssueId topic, DebateMove moveA, DebateMove moveB, double pointsA, double pointsB, double eventA, double eventB)
        {
            Topic = topic; MoveA = moveA; MoveB = moveB; PointsA = pointsA; PointsB = pointsB; EventA = eventA; EventB = eventB;
        }
    }

    /// <summary>
    /// What a debate produced. **Deliberately contains no share, no preference and no party
    /// member** — the ceiling of what a debate may do is a coverage shock and a momentum shock.
    /// </summary>
    public readonly struct DebateResult
    {
        public readonly DebateExchange[] Exchanges;
        public readonly double PerformanceA;
        public readonly double PerformanceB;
        /// <summary>Positive when A won, negative when B did, in index points.</summary>
        public readonly double Margin;
        /// <summary>Raw newsworthiness for `MediaCoverage.AddShock` — the same for both (a debate is one story).</summary>
        public readonly double CoverageShock;
        /// <summary>Percentage points of §22 momentum for the winner (+) and the loser (−): apply +MomentumShockPp to the winner, −MomentumShockPp to the loser.</summary>
        public readonly double MomentumShockPp;

        public DebateResult(DebateExchange[] exchanges, double performanceA, double performanceB, double margin, double coverageShock, double momentumShockPp)
        {
            Exchanges = exchanges; PerformanceA = performanceA; PerformanceB = performanceB; Margin = margin;
            CoverageShock = coverageShock; MomentumShockPp = momentumShockPp;
        }

        public int Winner => Margin > 0 ? 0 : (Margin < 0 ? 1 : -1);
    }

    public static class Debates
    {
        /// <summary>[AUTHORED-DRAFT] hours of preparation at which ~63 % of the preparation bonus is earned (§35's curve).</summary>
        public const double PreparationScale = 12.0;
        /// <summary>[AUTHORED-DRAFT] an unprepared candidate performs at 70 % of skill; fully prepared at 100 %.</summary>
        public const double PreparationFloor = 0.7;
        /// <summary>[AUTHORED-DRAFT] a candidate on a topic they fully own performs at 120 % of skill; on one they do not at 80 %.</summary>
        public const double OwnershipFloor = 0.8;
        public const double OwnershipSpan = 0.4;
        /// <summary>[AUTHORED-DRAFT] §15's "random event": the standard deviation of the per-exchange draw, in index points.</summary>
        public const double EventSigma = 4.0;
        /// <summary>[AUTHORED-DRAFT] raw newsworthiness per index point of margin (a 10-point rout is a 1.0 news day — the media system's full scale).</summary>
        public const double CoveragePerMarginPoint = 0.10;
        /// <summary>[AUTHORED-DRAFT] percentage points of momentum per index point of margin (§22's worked example: a strong debate ≈ +2.0 pp).</summary>
        public const double MomentumPpPerMarginPoint = 0.20;

        public static readonly DebateMove[] TheSeven =
        {
            DebateMove.AttackOpponent, DebateMove.DefendPolicy, DebateMove.ChangeSubject, DebateMove.AppealEmotionally,
            DebateMove.PresentStatistics, DebateMove.IgnoreAttack, DebateMove.Counterattack,
        };

        /// <summary>
        /// The attribute blend a move draws on (§16), weights summing to 1. [AUTHORED-DRAFT]: an
        /// emotional appeal is charisma, statistics are policy knowledge, an attack is debate skill
        /// with communication, a defence is knowledge with credibility, and so on.
        /// </summary>
        public static double Skill(DebateMove move, CandidateProfile c)
        {
            switch (move)
            {
                case DebateMove.AttackOpponent: return 0.45 * c.DebateSkill + 0.25 * c.Communication + 0.15 * c.Charisma + 0.15 * c.Popularity;
                case DebateMove.DefendPolicy: return 0.35 * c.PolicyKnowledge + 0.25 * c.Credibility + 0.25 * c.DebateSkill + 0.15 * c.Communication;
                case DebateMove.ChangeSubject: return 0.40 * c.DebateSkill + 0.30 * c.Communication + 0.30 * c.Charisma;
                case DebateMove.AppealEmotionally: return 0.50 * c.Charisma + 0.25 * c.Communication + 0.25 * c.Popularity;
                case DebateMove.PresentStatistics: return 0.55 * c.PolicyKnowledge + 0.25 * c.Credibility + 0.20 * c.Communication;
                case DebateMove.IgnoreAttack: return 0.40 * c.Credibility + 0.30 * c.Integrity + 0.30 * c.DebateSkill;
                case DebateMove.Counterattack: return 0.50 * c.DebateSkill + 0.30 * c.Charisma + 0.20 * c.Communication;
                default: throw new ArgumentException($"{move} is not one of §15's seven moves");
            }
        }

        /// <summary>
        /// The clash table: how a move fares against the opponent's, as a multiplier on its
        /// points. [AUTHORED-DRAFT], one row per §15 verb:
        /// an attack into IgnoreAttack is wasted (×0.6) and into a Counterattack is dangerous
        /// (×0.8 for the attacker, ×1.25 for the counter); a Counterattack with nothing to counter
        /// is empty (×0.7); DefendPolicy answers an attack well (×1.15) and is dull otherwise
        /// (×0.9); ChangeSubject against statistics lands (×1.1); everything else 1.0.
        /// </summary>
        public static double Clash(DebateMove mine, DebateMove theirs)
        {
            switch (mine)
            {
                case DebateMove.AttackOpponent:
                    return theirs == DebateMove.IgnoreAttack ? 0.6 : theirs == DebateMove.Counterattack ? 0.8 : theirs == DebateMove.DefendPolicy ? 0.9 : 1.0;
                case DebateMove.Counterattack:
                    return theirs == DebateMove.AttackOpponent ? 1.25 : 0.7;
                case DebateMove.DefendPolicy:
                    return theirs == DebateMove.AttackOpponent ? 1.15 : 0.9;
                case DebateMove.IgnoreAttack:
                    return theirs == DebateMove.AttackOpponent ? 1.05 : 0.85;
                case DebateMove.ChangeSubject:
                    return theirs == DebateMove.PresentStatistics ? 1.1 : 1.0;
                default:
                    return 1.0;
            }
        }

        /// <summary>The preparation multiplier: §35's curve on the hours, between the floor and 1.</summary>
        public static double Prepared(double hours) =>
            PreparationFloor + (1.0 - PreparationFloor) * (1.0 - Math.Exp(-Math.Max(0.0, hours) / PreparationScale));

        /// <summary>The ownership multiplier for a topic: 0.8 off the candidate's ground, 1.2 fully on it.</summary>
        public static double Owned(double ownership0To1) => OwnershipFloor + OwnershipSpan * Math.Max(0.0, Math.Min(1.0, ownership0To1));

        /// <summary>
        /// Resolve a debate. The exchanges' topics alternate between the two candidates' emphasised
        /// lists (each brings its own ground; a ChangeSubject move pulls the NEXT topic from the
        /// changer's list). Ownership per topic is supplied per candidate (0–1) — the party's
        /// issue-match with the electorate, W-F2's to source. The moves come from each candidate's
        /// plan (cycled if shorter than the exchange count).
        /// </summary>
        public static DebateResult Resolve(CandidateProfile a, DebatePreparation prepA, Func<IssueId, double> ownershipA,
            CandidateProfile b, DebatePreparation prepB, Func<IssueId, double> ownershipB, int exchanges, System.Random random)
        {
            if (random == null) { throw new ArgumentNullException(nameof(random)); }
            if (exchanges <= 0) { throw new ArgumentException("a debate has at least one exchange"); }
            if (prepA.Plan.Length == 0 || prepB.Plan.Length == 0) { throw new ArgumentException("both candidates need a plan"); }

            double preparedA = Prepared(prepA.Hours);
            double preparedB = Prepared(prepB.Hours);
            var list = new DebateExchange[exchanges];
            double sumA = 0.0, sumB = 0.0;
            bool nextFromA = true;
            int cursorA = 0, cursorB = 0;

            for (int i = 0; i < exchanges; i++)
            {
                IssueId topic = nextFromA ? Pick(prepA.Topics, cursorA++) : Pick(prepB.Topics, cursorB++);
                nextFromA = !nextFromA;

                DebateMove moveA = prepA.Plan[i % prepA.Plan.Length];
                DebateMove moveB = prepB.Plan[i % prepB.Plan.Length];

                double eventA = Gaussian(random) * EventSigma;
                double eventB = Gaussian(random) * EventSigma;

                double pointsA = Skill(moveA, a) * preparedA * Owned(ownershipA(topic)) * Clash(moveA, moveB) + eventA;
                double pointsB = Skill(moveB, b) * preparedB * Owned(ownershipB(topic)) * Clash(moveB, moveA) + eventB;
                pointsA = Clamp(pointsA); pointsB = Clamp(pointsB);

                list[i] = new DebateExchange(topic, moveA, moveB, pointsA, pointsB, eventA, eventB);
                sumA += pointsA; sumB += pointsB;

                // A subject change hands the next topic to the changer.
                if (moveA == DebateMove.ChangeSubject && moveB != DebateMove.ChangeSubject) { nextFromA = true; }
                else if (moveB == DebateMove.ChangeSubject && moveA != DebateMove.ChangeSubject) { nextFromA = false; }
            }

            double performanceA = sumA / exchanges;
            double performanceB = sumB / exchanges;
            double margin = performanceA - performanceB;
            double coverage = CoveragePerMarginPoint * Math.Abs(margin);
            double momentum = MomentumPpPerMarginPoint * Math.Abs(margin);
            return new DebateResult(list, performanceA, performanceB, margin, coverage, momentum);
        }

        private static IssueId Pick(IssueId[] topics, int cursor) => topics.Length == 0 ? IssueId.Economy : topics[cursor % topics.Length];

        private static double Clamp(double v) => v < 0 ? 0 : (v > 100 ? 100 : v);

        private static double Gaussian(System.Random random)
        {
            double u1 = 1.0 - random.NextDouble();
            double u2 = 1.0 - random.NextDouble();
            return Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2.0 * Math.PI * u2);
        }
    }
}
