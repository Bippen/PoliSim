using System;
using System.Globalization;
using System.Text;
using PoliSim.Elections;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// W-B6's harness — §11's five strategies as modifiers over §42's chain.
    ///
    /// The done-when, asserted:
    /// 1. **each strategy shows its STATED trade-off** — the same message (a rally spec at full
    ///    spend, salience 0.6, match 0.6, credibility 0.7) is resolved for a LOYAL group (loyalty
    ///    85) and a SWING group (loyalty 20), on a focus issue and off it, under every strategy
    ///    and under `None`; each of the spec's bullets is then a comparison of two numbers;
    /// 2. **no strategy dominates in a measured sweep** — thirty electorates (loyal share 0.1 to
    ///    0.9, in an issue-concentrated and an issue-diffuse variant, against a strong and a weak
    ///    opponent) are campaigned identically under each strategy, the outcome scored in the
    ///    model's own units (own persuasion + enthusiasm, minus the opponent's, all via
    ///    `CampaignPressure`), and the winner tabled per cell: no strategy wins every cell.
    /// 3. **a strategy cannot write a vote share** — it returns the same `ChainTrace` every other
    ///    consumer reads, and the negative campaign's only route to an opponent is
    ///    `CampaignPressure.AddAgainst`, still a compatibility pressure.
    /// </summary>
    public static class CampaignStrategyHarness
    {
        public static void Run()
        {
            CheckExit.ArmLogFold();
            int failures = 0;
            var sb = new StringBuilder();
            sb.Append("=== W-B6: §11's five strategies as modifiers over the chain ===\n");

            CampaignActions.ActionSpec rally = CampaignActions.Spec(CampaignActionKind.Rally);
            const double audience = 100_000, salience = 0.6, match = 0.6, credibility = 0.7;
            double spend = rally.MoneyCost;
            const double loyalGroup = 85.0, swingGroup = 20.0;

            CampaignActions.ChainTrace Resolve(CampaignStrategy s, double loyalty, bool focus) =>
                CampaignStrategyModel.Resolve(rally, audience, salience, match, credibility, spend,
                    CampaignStrategyModel.Modifiers(s, loyalty, focus));

            CampaignActions.ChainTrace noneLoyal = Resolve(CampaignStrategy.None, loyalGroup, true);
            CampaignActions.ChainTrace noneSwing = Resolve(CampaignStrategy.None, swingGroup, true);

            sb.Append("\n  the same rally under each strategy (persuasion / enthusiasm), loyal group 85 vs swing group 20, on the focus issue:\n");
            sb.Append("  strategy          loyal P    loyal E    swing P    swing E   reach\n");
            foreach (CampaignStrategy s in new[] { CampaignStrategy.None }.Concat(CampaignStrategyModel.TheFive))
            {
                CampaignActions.ChainTrace l = Resolve(s, loyalGroup, true);
                CampaignActions.ChainTrace w = Resolve(s, swingGroup, true);
                sb.Append(string.Format(CultureInfo.InvariantCulture, "  {0,-16} {1,8:N0}   {2,8:N0}   {3,8:N0}   {4,8:N0}   {5,6:N0}\n",
                    s, l.Persuasion, l.Enthusiasm, w.Persuasion, w.Enthusiasm, l.Reach));
            }

            // ---------- 1. each stated trade-off ----------
            CampaignActions.ChainTrace broad = Resolve(CampaignStrategy.BroadAppeal, loyalGroup, true);
            failures += Assert(sb, "1a. Broad Appeal: more reach, less persuasion per head (small gains across many)",
                broad.Reach > noneLoyal.Reach && broad.Persuasion / broad.Exposure < noneLoyal.Persuasion / noneLoyal.Exposure,
                string.Format(CultureInfo.InvariantCulture, "reach {0:N0} -> {1:N0}; per head {2:F4} -> {3:F4}; polarisation x{4}",
                    noneLoyal.Reach, broad.Reach, noneLoyal.Persuasion / noneLoyal.Exposure, broad.Persuasion / broad.Exposure,
                    CampaignStrategyModel.BroadPolarisation));

            CampaignActions.ChainTrace baseLoyal = Resolve(CampaignStrategy.BaseMobilization, loyalGroup, true);
            CampaignActions.ChainTrace baseSwing = Resolve(CampaignStrategy.BaseMobilization, swingGroup, true);
            failures += Assert(sb, "1b. Base Mobilization: lifts LOYAL turnout (enthusiasm) and lowers SWING persuasion",
                baseLoyal.Enthusiasm > noneLoyal.Enthusiasm && baseSwing.Persuasion < noneSwing.Persuasion,
                string.Format(CultureInfo.InvariantCulture, "loyal E {0:N0} -> {1:N0}; swing P {2:N0} -> {3:N0}",
                    noneLoyal.Enthusiasm, baseLoyal.Enthusiasm, noneSwing.Persuasion, baseSwing.Persuasion));

            CampaignActions.ChainTrace swingLoyal = Resolve(CampaignStrategy.SwingVoter, loyalGroup, true);
            CampaignActions.ChainTrace swingSwing = Resolve(CampaignStrategy.SwingVoter, swingGroup, true);
            failures += Assert(sb, "1c. Swing Voter: strong gains among SWING voters, a loss among LOYAL ones",
                swingSwing.Persuasion > noneSwing.Persuasion && swingLoyal.Persuasion < noneLoyal.Persuasion
                && swingLoyal.Enthusiasm < noneLoyal.Enthusiasm,
                string.Format(CultureInfo.InvariantCulture, "swing P {0:N0} -> {1:N0}; loyal P {2:N0} -> {3:N0}, loyal E {4:N0} -> {5:N0}",
                    noneSwing.Persuasion, swingSwing.Persuasion, noneLoyal.Persuasion, swingLoyal.Persuasion,
                    noneLoyal.Enthusiasm, swingLoyal.Enthusiasm));

            StrategyModifiers negative = CampaignStrategyModel.Modifiers(CampaignStrategy.NegativeCampaign, loyalGroup, true);
            CampaignActions.ChainTrace neg = Resolve(CampaignStrategy.NegativeCampaign, loyalGroup, true);
            var pressure = new CampaignPressure(2);
            pressure.Add(0, neg);
            pressure.AddAgainst(1, neg.Persuasion * negative.OpponentShare);
            failures += Assert(sb, "1d. Negative Campaign: reduces the OPPONENT (negative pressure), costs own credibility, raises media attention",
                pressure.Persuasion(1) < 0 && neg.Credibility < noneLoyal.Credibility && negative.MediaAttentionMultiplier > 1.0
                && neg.Persuasion < noneLoyal.Persuasion,
                string.Format(CultureInfo.InvariantCulture, "against opponent {0:N0}; own P {1:N0} -> {2:N0}; credibility {3:F2} -> {4:F2}; media x{5}",
                    pressure.Persuasion(1), noneLoyal.Persuasion, neg.Persuasion, noneLoyal.Credibility, neg.Credibility, negative.MediaAttentionMultiplier));

            CampaignActions.ChainTrace popFocus = Resolve(CampaignStrategy.Populist, loyalGroup, true);
            CampaignActions.ChainTrace popOther = Resolve(CampaignStrategy.Populist, loyalGroup, false);
            failures += Assert(sb, "1e. Populist: strong gains among groups that PRIORITISE the issue, reduced support among the others",
                popFocus.Persuasion > noneLoyal.Persuasion && popOther.Persuasion < noneLoyal.Persuasion,
                string.Format(CultureInfo.InvariantCulture, "focus group P {0:N0} -> {1:N0}; other group P {0:N0} -> {2:N0}",
                    noneLoyal.Persuasion, popFocus.Persuasion, popOther.Persuasion));

            // Prioritises: the populist's own test of a group.
            var weights = new double[IssueVector.IssueCount];
            for (int i = 0; i < weights.Length; i++) { weights[i] = 30.0; }
            weights[(int)IssueId.Immigration] = 90.0;
            var v = new IssueVector(weights);
            failures += Assert(sb, "1f. a group prioritises an issue weighted above its mean, and not one weighted below",
                CampaignStrategyModel.Prioritises(v, IssueId.Immigration) && !CampaignStrategyModel.Prioritises(v, IssueId.Housing),
                "Immigration 90 yes, Housing 30 no (mean 36.7)");

            // ---------- 3. structural ----------
            failures += Assert(sb, "3. None is the identity (every earlier measurement stands)",
                noneLoyal.Persuasion == CampaignActions.Resolve(rally, audience, salience, match, credibility, spend).Persuasion,
                "identical trace");

            // ---------- 2. the sweep: no strategy dominates ----------
            sb.Append("\n  the sweep - outcome per electorate (own persuasion + enthusiasm, minus the opponent's; model units):\n");
            sb.Append("  loyal share  issues    opponent    Broad      Base     Swing   Negative  Populist   winner\n");
            var wins = new int[6];
            int cells = 0;
            double[] loyalShares = { 0.1, 0.3, 0.5, 0.7, 0.9 };
            foreach (double loyalShare in loyalShares)
            {
                foreach (bool concentrated in new[] { true, false })
                {
                    foreach (double opponentStrength in new[] { 0.4, 1.0, 1.6 })
                    {
                        double[] outcome = SweepCell(loyalShare, concentrated, opponentStrength);
                        int best = 1;
                        for (int s = 2; s <= 5; s++) { if (outcome[s] > outcome[best]) { best = s; } }
                        wins[best]++;
                        cells++;
                        sb.Append(string.Format(CultureInfo.InvariantCulture,
                            "  {0,11:F1}  {1,-8}  {2,8:F1}  {3,9:F2} {4,9:F2} {5,9:F2} {6,9:F2} {7,9:F2}   {8}\n",
                            loyalShare, concentrated ? "focused" : "diffuse", opponentStrength,
                            outcome[1], outcome[2], outcome[3], outcome[4], outcome[5], (CampaignStrategy)best));
                    }
                }
            }

            int strategiesThatWin = 0;
            int maxWins = 0;
            for (int s = 1; s <= 5; s++) { if (wins[s] > 0) { strategiesThatWin++; } if (wins[s] > maxWins) { maxWins = wins[s]; } }
            failures += Assert(sb, "2a. no strategy wins every electorate (no dominant strategy in the sweep)",
                maxWins < cells, $"wins of {cells}: Broad {wins[1]}, Base {wins[2]}, Swing {wins[3]}, Negative {wins[4]}, Populist {wins[5]}");
            failures += Assert(sb, "2b. at least three of the five strategies win somewhere (the trade-offs are real, not ornamental)",
                strategiesThatWin >= 3, $"{strategiesThatWin} strategies win at least one electorate");

            sb.Append($"\n=== CampaignStrategyHarness: {(failures == 0 ? "ALL ASSERTIONS PASS" : failures + " FAILED")} ===\n");
            Debug.Log(sb.ToString());
            CheckExit.Finish(failures == 0 ? 0 : 1);
        }

        /// <summary>
        /// One electorate: two groups (loyal 85 / swing 20) in the given shares; issue weights
        /// concentrated on one issue or spread; an opponent of the given strength campaigning
        /// under None. Every strategy runs the same week (the W-B3 burst) and the cell's outcome
        /// per strategy is own persuasion + enthusiasm delivered minus what the opponent delivered,
        /// with the negative campaign's pressure against the opponent counted as the model counts
        /// it: through `CampaignPressure`.
        /// </summary>
        private static double[] SweepCell(double loyalShare, bool concentrated, double opponentStrength)
        {
            const double electorate = 1_000_000;
            const double credibility = 0.7;
            double focusSalience = concentrated ? 0.8 : 0.4;
            double otherSalience = concentrated ? 0.2 : 0.4;
            bool loyalPrioritises = concentrated;      // the focused electorate's loyal base cares about the focus issue
            bool swingPrioritises = !concentrated;     // in the diffuse one it is the swing groups that do

            var outcome = new double[6];
            for (int s = 1; s <= 5; s++)
            {
                var strategy = (CampaignStrategy)s;
                var pressure = new CampaignPressure(2);

                // Own campaign: the W-B3 week, each action split across the two groups by share.
                Burst(pressure, 0, strategy, electorate * loyalShare, 85.0, loyalPrioritises, focusSalience, otherSalience, credibility);
                Burst(pressure, 0, strategy, electorate * (1 - loyalShare), 20.0, swingPrioritises, focusSalience, otherSalience, credibility);

                // Negative: 60 % of the own persuasion lands against the opponent.
                if (strategy == CampaignStrategy.NegativeCampaign)
                {
                    pressure.AddAgainst(1, pressure.Persuasion(0) * CampaignStrategyModel.NegativeOpponentShare);
                }

                // The opponent's identical week under None, scaled by its strength.
                var opponent = new CampaignPressure(2);
                Burst(opponent, 1, CampaignStrategy.None, electorate * loyalShare * opponentStrength, 85.0, loyalPrioritises, focusSalience, otherSalience, credibility);
                Burst(opponent, 1, CampaignStrategy.None, electorate * (1 - loyalShare) * opponentStrength, 20.0, swingPrioritises, focusSalience, otherSalience, credibility);

                double own = pressure.Persuasion(0) / CampaignPressure.PersuasionPerCompatibilityPoint
                             + pressure.Enthusiasm(0) / CampaignPressure.EnthusiasmPerTurnoutPoint;
                double theirs = (opponent.Persuasion(1) + pressure.Persuasion(1)) / CampaignPressure.PersuasionPerCompatibilityPoint
                                + opponent.Enthusiasm(1) / CampaignPressure.EnthusiasmPerTurnoutPoint;
                outcome[s] = own - theirs;
            }

            return outcome;
        }

        private static void Burst(CampaignPressure pressure, int party, CampaignStrategy strategy, double audience,
            double loyalty, bool prioritises, double focusSalience, double otherSalience, double credibility)
        {
            // W-B3's hard week: three rallies, two town halls, a TV buy, daily door-knocking -
            // half the messages on the focus issue, half on the rest.
            (CampaignActionKind kind, int times)[] week =
            {
                (CampaignActionKind.Rally, 3), (CampaignActionKind.TownHall, 2),
                (CampaignActionKind.TelevisionAd, 1), (CampaignActionKind.DoorToDoor, 7),
            };

            foreach ((CampaignActionKind kind, int times) in week)
            {
                CampaignActions.ActionSpec spec = CampaignActions.Spec(kind);
                for (int i = 0; i < times; i++)
                {
                    bool onFocus = i % 2 == 0;
                    StrategyModifiers m = CampaignStrategyModel.Modifiers(strategy, loyalty, onFocus && prioritises);
                    double salience = onFocus ? focusSalience : otherSalience;
                    pressure.Add(party, CampaignStrategyModel.Resolve(spec, audience, salience, 0.6, credibility, spec.MoneyCost, m));
                }
            }
        }

        private static int Assert(StringBuilder sb, string label, bool condition, string detail)
        {
            sb.Append($"  {(condition ? "ok  " : "FAIL")} {label}: {detail}\n");
            return condition ? 0 : 1;
        }
    }

    internal static class StrategyEnumerable
    {
        public static CampaignStrategy[] Concat(this CampaignStrategy[] first, CampaignStrategy[] second)
        {
            var all = new CampaignStrategy[first.Length + second.Length];
            first.CopyTo(all, 0);
            second.CopyTo(all, first.Length);
            return all;
        }
    }
}
