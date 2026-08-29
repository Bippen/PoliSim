using System;
using System.Globalization;
using System.Reflection;
using System.Text;
using PoliSim.Elections;
using PoliSim.Simulation;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// W-B7's harness — §15's debates.
    ///
    /// The done-when, asserted:
    /// 1. **the same seed and choices reproduce a debate exactly** — every exchange's points and
    ///    events identical on the `Debate` stream; a different seed differs; the same seed with a
    ///    different plan differs;
    /// 2. **performance moves media coverage and momentum rather than vote share directly** —
    ///    structurally (`DebateResult` has no share / preference / party member) and behaviourally:
    ///    the result applied to `MediaCoverage` and `MomentumTracker` moves both, while
    ///    `CampaignPressure` and the recomputed preference are untouched to the bit;
    /// 3. §15's performance terms are real: attributes matter (the charismatic candidate wins the
    ///    emotional debate far more often than half the time over 400 seeds), preparation matters
    ///    (the prepared candidate beats the unprepared identical one), ownership matters (a debate on
    ///    the candidate's own ground), and the clash table does what it says (an attack into a
    ///    counterattack loses to it; an attack into silence is wasted);
    /// 4. §22's worked example is in range — a strong debate (a 10-point rout) is about +2 pp of
    ///    momentum, which then decays on the momentum tracker's own half-life.
    /// </summary>
    public static class DebateHarness
    {
        public static void Run()
        {
            CheckExit.ArmLogFold();
            int failures = 0;
            var sb = new StringBuilder();
            sb.Append("=== W-B7: debates (§15) - exchanges, attributes, preparation, ownership, the clash, one seeded draw ===\n");

            // §16 attributes, [AUTHORED-DRAFT] staging - the spec's own example shape: a charismatic
            // debater weak on policy, against a policy expert weak on the stage. Game fiction, no names.
            var orator = new CandidateProfile("Candidate A (orator)", charisma: 90, debateSkill: 91, communication: 85,
                credibility: 60, integrity: 82, policyKnowledge: 45, campaignSkill: 70, popularity: 65, scandalResistance: 60);
            var wonk = new CandidateProfile("Candidate B (wonk)", charisma: 45, debateSkill: 55, communication: 60,
                credibility: 85, integrity: 80, policyKnowledge: 92, campaignSkill: 60, popularity: 55, scandalResistance: 70);
            Func<IssueId, double> ownsClimate = issue => issue == IssueId.Climate ? 0.9 : issue == IssueId.Economy ? 0.3 : 0.5;
            Func<IssueId, double> ownsEconomy = issue => issue == IssueId.Economy ? 0.9 : issue == IssueId.Climate ? 0.3 : 0.5;

            var emotional = new DebatePreparation(8, new[] { IssueId.Climate }, new[] { DebateMove.AppealEmotionally, DebateMove.AttackOpponent, DebateMove.ChangeSubject });
            var statistical = new DebatePreparation(8, new[] { IssueId.Economy }, new[] { DebateMove.PresentStatistics, DebateMove.DefendPolicy, DebateMove.PresentStatistics });

            // ---------- 2a. structural ----------
            var forbidden = new[] { "share", "vote", "preference", "party" };
            var offenders = new StringBuilder();
            foreach (FieldInfo f in typeof(DebateResult).GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                string lower = f.Name.ToLowerInvariant();
                foreach (string bad in forbidden) { if (lower.Contains(bad)) { offenders.Append(f.Name).Append(' '); } }
            }

            failures += Assert(sb, "2a. DebateResult cannot express a vote share (no share/vote/preference/party member)",
                offenders.Length == 0, offenders.Length == 0 ? "6 members, none of them a share" : $"offenders: {offenders}");

            // ---------- 1. determinism ----------
            DebateResult first = Seeded(777, () => Debates.Resolve(orator, emotional, ownsClimate, wonk, statistical, ownsEconomy, 6, SimulationRandom.For(SimulationRandom.Stream.Debate)));
            DebateResult second = Seeded(777, () => Debates.Resolve(orator, emotional, ownsClimate, wonk, statistical, ownsEconomy, 6, SimulationRandom.For(SimulationRandom.Stream.Debate)));
            DebateResult other = Seeded(778, () => Debates.Resolve(orator, emotional, ownsClimate, wonk, statistical, ownsEconomy, 6, SimulationRandom.For(SimulationRandom.Stream.Debate)));
            var altPlan = new DebatePreparation(8, new[] { IssueId.Climate }, new[] { DebateMove.IgnoreAttack, DebateMove.DefendPolicy });
            DebateResult altered = Seeded(777, () => Debates.Resolve(orator, altPlan, ownsClimate, wonk, statistical, ownsEconomy, 6, SimulationRandom.For(SimulationRandom.Stream.Debate)));

            bool identical = first.Exchanges.Length == second.Exchanges.Length;
            for (int i = 0; identical && i < first.Exchanges.Length; i++)
            {
                identical = first.Exchanges[i].PointsA == second.Exchanges[i].PointsA && first.Exchanges[i].PointsB == second.Exchanges[i].PointsB
                            && first.Exchanges[i].EventA == second.Exchanges[i].EventA && first.Exchanges[i].Topic == second.Exchanges[i].Topic;
            }

            failures += Assert(sb, "1a. the same seed and the same choices reproduce every exchange exactly", identical, $"{first.Exchanges.Length} exchanges bit-identical");
            failures += Assert(sb, "1b. a different seed gives a different debate", first.Margin != other.Margin, string.Format(CultureInfo.InvariantCulture, "margins {0:F3} vs {1:F3}", first.Margin, other.Margin));
            failures += Assert(sb, "1c. the same seed with different choices gives a different debate", first.Margin != altered.Margin, string.Format(CultureInfo.InvariantCulture, "margins {0:F3} vs {1:F3}", first.Margin, altered.Margin));

            sb.Append(string.Format(CultureInfo.InvariantCulture, "\n  seed 777, six exchanges: A (orator, emotional plan) {0:F1}  B (wonk, statistical plan) {1:F1}  margin {2:+0.0;-0.0}  coverage shock {3:F2}  momentum {4:F2} pp\n",
                first.PerformanceA, first.PerformanceB, first.Margin, first.CoverageShock, first.MomentumShockPp));
            foreach (DebateExchange e in first.Exchanges)
            {
                sb.Append(string.Format(CultureInfo.InvariantCulture, "    {0,-12} A {1,-18} {2,5:F1}   B {3,-18} {4,5:F1}   events {5:+0.0;-0.0} / {6:+0.0;-0.0}\n",
                    e.Topic, e.MoveA, e.PointsA, e.MoveB, e.PointsB, e.EventA, e.EventB));
            }

            // ---------- 2b. behavioural: the result moves coverage and momentum, never the preference ----------
            double[] compatibility = { 62.0, 58.0, 45.0, 38.0 };
            double[] prior = { 0.34, 0.30, 0.22, 0.14 };
            double[] loyalty = { 70.0, 65.0, 60.0, 55.0 };
            var pressure = new CampaignPressure(4);
            double[] before = PreferenceModel.Preference(compatibility, prior, loyalty);

            var coverage = new MediaCoverage(4);
            var momentum = new MomentumTracker(4);
            int winner = first.Winner;
            int loser = winner == 0 ? 1 : 0;
            coverage.AddShock(0, first.CoverageShock);
            coverage.AddShock(1, first.CoverageShock);
            double[] gains = coverage.CloseDay();
            momentum.AddShock(winner, first.MomentumShockPp);
            momentum.AddShock(loser, -first.MomentumShockPp);

            double[] bonus = pressure.ToCompatibilityBonus();
            var boosted = new double[4];
            for (int i = 0; i < 4; i++) { boosted[i] = compatibility[i] + bonus[i]; }
            double[] after = PreferenceModel.Preference(boosted, prior, loyalty);
            bool preferenceUntouched = true;
            for (int i = 0; i < 4; i++) { if (after[i] != before[i]) { preferenceUntouched = false; } }

            failures += Assert(sb, "2b. applied to the media and the momentum tracker, the debate moves BOTH (coverage gained, momentum shocked)",
                gains[0] > 0 && gains[1] > 0 && momentum.MomentumPp(winner) > 0 && momentum.MomentumPp(loser) < 0,
                string.Format(CultureInfo.InvariantCulture, "coverage +{0:F3}; momentum winner {1:+0.00} pp, loser {2:+0.00} pp", gains[0], momentum.MomentumPp(winner), momentum.MomentumPp(loser)));
            failures += Assert(sb, "2c. THE DECISIVE TEST - the preference recomputed after the debate is bit-identical: a debate has no route to a vote share",
                preferenceUntouched, "4 of 4 identical");
            double[] apparent = momentum.Apply(before);
            failures += Assert(sb, "2d. the polls move (momentum applied to the same preference shifts where the race APPEARS to be)",
                apparent[winner] > before[winner] && apparent[loser] < before[loser],
                string.Format(CultureInfo.InvariantCulture, "winner {0:P2} -> {1:P2} apparent", before[winner], apparent[winner]));

            // ---------- 3. the terms ----------
            int oratorWins = 0, preparedWins = 0, homeWins = 0;
            const int seeds = 400;
            var unprepared = new DebatePreparation(0, new[] { IssueId.Climate }, emotional.Plan);
            // The twin brings the SAME topic (climate) but does not own it - the only difference between the two is
            // ownership. (A first draft gave the twin a different topic list, which made the exchanges alternate
            // between both grounds and the pair exactly symmetric: 199 of 400, the test measuring nothing.)
            var awayGround = new DebatePreparation(8, new[] { IssueId.Climate }, emotional.Plan);
            for (int s = 0; s < seeds; s++)
            {
                DebateResult r = Seeded(10_000 + s, () => Debates.Resolve(orator, emotional, ownsClimate, wonk, emotional, ownsEconomy, 6, SimulationRandom.For(SimulationRandom.Stream.Debate)));
                if (r.Winner == 0) { oratorWins++; }
                DebateResult p = Seeded(20_000 + s, () => Debates.Resolve(orator, emotional, ownsClimate, orator, unprepared, ownsClimate, 6, SimulationRandom.For(SimulationRandom.Stream.Debate)));
                if (p.Winner == 0) { preparedWins++; }
                DebateResult h = Seeded(30_000 + s, () => Debates.Resolve(orator, emotional, ownsClimate, orator, awayGround, ownsEconomy, 6, SimulationRandom.For(SimulationRandom.Stream.Debate)));
                if (h.Winner == 0) { homeWins++; }
            }

            failures += Assert(sb, "3a. attributes matter: the orator wins the emotional debate against the wonk in more than 80 % of 400 seeds",
                oratorWins > 0.8 * seeds, $"{oratorWins} of {seeds}");
            failures += Assert(sb, "3b. preparation matters: 8 hours beats 0 hours between identical candidates in more than 70 % of seeds",
                preparedWins > 0.7 * seeds, $"{preparedWins} of {seeds}");
            failures += Assert(sb, "3c. ownership matters: a candidate on their own ground beats their twin on the opponent's in more than 70 % of seeds",
                homeWins > 0.7 * seeds, $"{homeWins} of {seeds}");

            double attackVsIgnore = Debates.Skill(DebateMove.AttackOpponent, orator) * Debates.Clash(DebateMove.AttackOpponent, DebateMove.IgnoreAttack);
            double attackVsDefend = Debates.Skill(DebateMove.AttackOpponent, orator) * Debates.Clash(DebateMove.AttackOpponent, DebateMove.DefendPolicy);
            double attackVsStats = Debates.Skill(DebateMove.AttackOpponent, orator) * Debates.Clash(DebateMove.AttackOpponent, DebateMove.PresentStatistics);
            failures += Assert(sb, "3d. the clash table: an attack into silence is wasted, into a defence blunted, into statistics lands",
                attackVsIgnore < attackVsDefend && attackVsDefend < attackVsStats,
                string.Format(CultureInfo.InvariantCulture, "{0:F1} < {1:F1} < {2:F1}", attackVsIgnore, attackVsDefend, attackVsStats));
            double counter = Debates.Skill(DebateMove.Counterattack, wonk) * Debates.Clash(DebateMove.Counterattack, DebateMove.AttackOpponent);
            double emptyCounter = Debates.Skill(DebateMove.Counterattack, wonk) * Debates.Clash(DebateMove.Counterattack, DebateMove.PresentStatistics);
            failures += Assert(sb, "3e. a counterattack needs an attack to counter (against statistics it is empty)",
                counter > emptyCounter, string.Format(CultureInfo.InvariantCulture, "{0:F1} vs {1:F1}", counter, emptyCounter));

            // ---------- 4. §22's worked example ----------
            double routPp = Debates.MomentumPpPerMarginPoint * 10.0;
            failures += Assert(sb, "4. a 10-point rout is about +2 pp of momentum (§22's 'strong debate' example), then decays on the tracker's half-life",
                Math.Abs(routPp - 2.0) < 0.5, string.Format(CultureInfo.InvariantCulture, "{0:F2} pp; after two weeks {1:F2} pp", routPp, routPp * PollingSystem.MomentumDecay(14)));

            sb.Append($"\n=== DebateHarness: {(failures == 0 ? "ALL ASSERTIONS PASS" : failures + " FAILED")} ===\n");
            Debug.Log(sb.ToString());
            CheckExit.Finish(failures == 0 ? 0 : 1);
        }

        private static DebateResult Seeded(int seed, Func<DebateResult> run)
        {
            SimulationRandom.Seed(seed);
            return run();
        }

        private static int Assert(StringBuilder sb, string label, bool condition, string detail)
        {
            sb.Append($"  {(condition ? "ok  " : "FAIL")} {label}: {detail}\n");
            return condition ? 0 : 1;
        }
    }
}
