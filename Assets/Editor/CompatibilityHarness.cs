using System;
using PoliSim.Elections;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// SPEC §7's unit harness — SYNTHETIC VECTORS ONLY (Phase 1's requirement). No sourced data
    /// appears here on purpose: this checks that the compatibility core behaves the way the spec
    /// says it must, independent of whether any real party happens to exercise the behaviour.
    ///
    /// THE ENUMERATION, stated so the coverage claim is checkable (rule 14):
    /// 1. **Identity** — a party at the group's exact position with perfect party scalars scores
    ///    the maximum, 100.
    /// 2. **Opposition** — a party at the opposite pole on every axis and issue, with zero
    ///    scalars, scores the minimum, 0.
    /// 3. **Weighted discrimination (§6's core claim)** — for a group that cares about ONE issue,
    ///    the party matching that issue beats the party that matches every OTHER issue. This is
    ///    "the player should NOT simply maximize overall popularity" as an assertion.
    /// 4. **Undefined is skipped, not centred** — a party defining only the three sourced axes
    ///    scores exactly what those three axes imply; padding the missing five with 50 would give
    ///    a different number, and the test pins the difference.
    /// 5. **Weight redistribution** — a group with no issue positions loses the policy term
    ///    entirely, and the total equals the remaining terms renormalised (NOT dragged toward
    ///    zero by a missing sub-score).
    /// 6. **Monotonicity** — moving a party closer on a weighted issue never lowers
    ///    compatibility; a 21-step sweep asserts it at every step.
    /// 7. **Boundedness** — across a 3-axis × 3-issue sweep of extremes, every result stays inside
    ///    0–100 (the spec's normalisation, asserted rather than assumed).
    ///
    /// Run: `Unity.exe -batchmode -nographics -projectPath &lt;path&gt; -executeMethod
    /// PoliSim.EditorTools.CompatibilityHarness.Run -logFile &lt;path&gt;`. Deterministic; no RNG.
    /// </summary>
    public static class CompatibilityHarness
    {
        public static void Run()
        {
            int failures = 0;

            // --- 1. Identity ---
            var groupCentre = Group("centre", positions: 50, weights: 60, ideology: 50);
            var partyIdentical = Party("identical", positions: 50, ideology: 50, reputation: 100, leader: 100, campaign: 100);
            failures += Near("1. identity (exact match, perfect scalars)", Compatibility.Score(partyIdentical, groupCentre), 100.0);

            // --- 2. Opposition ---
            var groupLow = Group("low", positions: 0, weights: 80, ideology: 0);
            var partyHigh = Party("opposite", positions: 100, ideology: 100, reputation: 0, leader: 0, campaign: 0);
            failures += Near("2. opposition (opposite poles, zero scalars)", Compatibility.Score(partyHigh, groupLow), 0.0);

            // --- 3. Weighted discrimination: the group cares ONLY about Housing ---
            var housingWeights = new double[IssueVector.IssueCount];
            for (int i = 0; i < housingWeights.Length; i++) { housingWeights[i] = 0.0; }
            housingWeights[(int)IssueId.Housing] = 100.0;
            var singleIssueGroup = new VoterGroupProfile("housing-only", 0.1, 60,
                new IssueVector(housingWeights), IssueVector.Uniform(80), IdeologyVector.Uniform(50));

            var housingMatcher = PartyWithOneIssue("matches housing", housingValue: 80, elsewhere: 10);
            var everythingElseMatcher = PartyWithOneIssue("matches everything else", housingValue: 10, elsewhere: 80);
            double matchScore = Compatibility.Score(housingMatcher, singleIssueGroup);
            double elseScore = Compatibility.Score(everythingElseMatcher, singleIssueGroup);
            failures += Assert("3. weighted discrimination (one-issue group prefers the issue-matcher)",
                matchScore > elseScore, $"housing-matcher {matchScore:F2} vs other {elseScore:F2}");

            // --- 4. Undefined is skipped, not centred ---
            var sparseParty = new PartyProfile("sparse (3 sourced axes)",
                IdeologyVector.FromSourcedAxes(20, 20, 20), IssueVector.Uniform(50),
                reputation: 50, leaderAppeal: 50, campaignEffectiveness: 50);
            var paddedParty = new PartyProfile("padded (5 axes forced to 50)",
                new IdeologyVector(new[] { 20.0, 20.0, 20.0, 50.0, 50.0, 50.0, 50.0, 50.0 }), IssueVector.Uniform(50),
                reputation: 50, leaderAppeal: 50, campaignEffectiveness: 50);
            double sparse = Compatibility.Score(sparseParty, groupCentre);
            double padded = Compatibility.Score(paddedParty, groupCentre);
            failures += Assert("4. undefined axes skipped, not centred",
                Math.Abs(sparse - padded) > 1.0, $"sparse {sparse:F2} vs padded {padded:F2} (must differ)");
            double expectedSparseIdeology = 100.0 - 30.0;   // |20-50| on each of three defined axes
            failures += Near("4b. sparse ideological match = the three defined axes only",
                Compatibility.IdeologicalMatch(sparseParty.Ideology, groupCentre.Ideology, out _), expectedSparseIdeology);

            // --- 5. Weight redistribution when the policy term is undefined ---
            var noPositionGroup = new VoterGroupProfile("no stated positions", 0.1, 60,
                IssueVector.Uniform(70), IssueVector.Uniform(double.NaN), IdeologyVector.Uniform(50));
            var plainParty = Party("plain", positions: 50, ideology: 50, reputation: 80, leader: 60, campaign: 40);
            Compatibility.Breakdown b = Compatibility.Explain(plainParty, noPositionGroup);
            double expectedNoPolicy =
                (Compatibility.WeightIdeologicalMatch * 100.0
                 + Compatibility.WeightReputation * 80.0
                 + Compatibility.WeightLeaderAppeal * 60.0
                 + Compatibility.WeightCampaignEffectiveness * 40.0)
                / (Compatibility.WeightIdeologicalMatch + Compatibility.WeightReputation
                   + Compatibility.WeightLeaderAppeal + Compatibility.WeightCampaignEffectiveness);
            failures += Assert("5. policy term reported undefined", !b.PolicyMatchDefined, $"defined={b.PolicyMatchDefined}");
            failures += Near("5b. weight redistributed across the defined terms", b.Total, expectedNoPolicy);

            // --- 6. Monotonicity: 21 steps toward the group's position ---
            bool monotone = true;
            double previous = double.NegativeInfinity;
            for (int step = 0; step <= 20; step++)
            {
                double position = step * 5.0;   // 0 -> 100, group sits at 100
                var group = Group("high", positions: 100, weights: 90, ideology: 50);
                var party = Party($"step{step}", positions: position, ideology: 50);
                double score = Compatibility.Score(party, group);
                if (score < previous - 1e-9) { monotone = false; }
                previous = score;
            }

            failures += Assert("6. monotonicity (closer on a weighted issue never scores lower)", monotone, "21-step sweep");

            // --- 7. Boundedness across extremes ---
            bool bounded = true;
            foreach (double partyPos in new[] { 0.0, 50.0, 100.0 })
            {
                foreach (double groupPos in new[] { 0.0, 50.0, 100.0 })
                {
                    foreach (double scalar in new[] { 0.0, 50.0, 100.0 })
                    {
                        double score = Compatibility.Score(
                            Party("x", positions: partyPos, ideology: partyPos, reputation: scalar, leader: scalar, campaign: scalar),
                            Group("y", positions: groupPos, weights: 75, ideology: groupPos));
                        if (score < -1e-9 || score > 100.0 + 1e-9) { bounded = false; }
                    }
                }
            }

            failures += Assert("7. boundedness (27-case sweep stays within 0-100)", bounded, "3x3x3");

            Debug.Log($"=== CompatibilityHarness (spec §7): {(failures == 0 ? "ALL 9 ASSERTIONS PASS" : failures + " FAILED")} ===");
            CheckExit.Finish(failures == 0 ? 0 : 1);
        }

        private static PartyProfile Party(string name, double positions, double ideology,
            double reputation = 50, double leader = 50, double campaign = 50)
        {
            return new PartyProfile(name, IdeologyVector.Uniform(ideology), IssueVector.Uniform(positions),
                reputation: reputation, leaderAppeal: leader, campaignEffectiveness: campaign);
        }

        private static PartyProfile PartyWithOneIssue(string name, double housingValue, double elsewhere)
        {
            var positions = new double[IssueVector.IssueCount];
            for (int i = 0; i < positions.Length; i++) { positions[i] = elsewhere; }
            positions[(int)IssueId.Housing] = housingValue;
            return new PartyProfile(name, IdeologyVector.Uniform(50), new IssueVector(positions));
        }

        private static VoterGroupProfile Group(string name, double positions, double weights, double ideology)
        {
            return new VoterGroupProfile(name, 0.1, 60, IssueVector.Uniform(weights),
                IssueVector.Uniform(positions), IdeologyVector.Uniform(ideology));
        }

        private static int Near(string label, double actual, double expected)
        {
            bool ok = Math.Abs(actual - expected) < 0.01;
            Debug.Log(string.Format(System.Globalization.CultureInfo.InvariantCulture,
                "  {0} {1}: got {2:F4}, expected {3:F4}", ok ? "ok  " : "FAIL", label, actual, expected));
            return ok ? 0 : 1;
        }

        private static int Assert(string label, bool condition, string detail)
        {
            Debug.Log($"  {(condition ? "ok  " : "FAIL")} {label}: {detail}");
            return condition ? 0 : 1;
        }
    }
}
