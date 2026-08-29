using System;

namespace PoliSim.Elections
{
    /// <summary>
    /// SPEC §7 — party-voter compatibility, the foundation polling and election results are built
    /// on. PURE FUNCTIONS, WIRED TO NOTHING (R-N2).
    ///
    /// The spec's formula, verbatim:
    /// <code>
    /// Compatibility = Policy Match + Ideological Match + Party Reputation
    ///               + Leader Appeal + Campaign Effectiveness      (normalised 0-100)
    /// </code>
    ///
    /// The spec gives the TERMS but not their relative weights, so the weights below are
    /// **[AUTHORED-DRAFT]** (R-N4: game fiction, never dressed as researched), one logged line
    /// each in the Day-1 report, and every one strikeable:
    /// - PolicyMatch 0.40 — the spec's §6 insists attractiveness depends on matching what voters
    ///   actually prioritise, so it is the largest single term.
    /// - IdeologicalMatch 0.30 — §4's multi-axis position; second because it is what remains true
    ///   when a party's stated policy of the week changes.
    /// - Reputation 0.12, LeaderAppeal 0.10, CampaignEffectiveness 0.08 — the three party-side
    ///   scalars, deliberately summing to only 0.30 so that **campaigning cannot outrun
    ///   positioning**, which is §44's whole design question ("where can I actually gain votes?"
    ///   rather than "which button gives the most popularity?").
    /// They sum to 1.0, and because every term is already 0–100 the result is 0–100 with no
    /// further rescaling — the spec's "normalize the result to 0–100" satisfied by construction.
    ///
    /// **Undefined is skipped, never centred.** An ideological axis or issue position of NaN is
    /// omitted from its sub-score rather than treated as 50: the sourced CHES data fills three of
    /// eight axes, and a model that quietly read the missing five as "centrist" would be inventing
    /// party positions — the exact failure R-N4 exists to prevent. If NO axis or NO weighted issue
    /// is shared, that sub-score is reported as undefined and its weight is redistributed across
    /// the terms that do exist, so a sparse profile is penalised for nothing.
    /// </summary>
    public static class Compatibility
    {
        // [AUTHORED-DRAFT] term weights - see the class doc; every one strikeable.
        public const double WeightPolicyMatch = 0.40;
        public const double WeightIdeologicalMatch = 0.30;
        public const double WeightReputation = 0.12;
        public const double WeightLeaderAppeal = 0.10;
        public const double WeightCampaignEffectiveness = 0.08;

        /// <summary>The five sub-scores behind a compatibility figure, so §31's "why" can decompose it later — the approval ledger's idiom applied to a vote model.</summary>
        public readonly struct Breakdown
        {
            public readonly double PolicyMatch;
            public readonly double IdeologicalMatch;
            public readonly double Reputation;
            public readonly double LeaderAppeal;
            public readonly double CampaignEffectiveness;
            public readonly double Total;
            public readonly bool PolicyMatchDefined;
            public readonly bool IdeologicalMatchDefined;

            public Breakdown(double policyMatch, bool policyDefined, double ideologicalMatch, bool ideologicalDefined,
                double reputation, double leaderAppeal, double campaignEffectiveness, double total)
            {
                PolicyMatch = policyMatch;
                PolicyMatchDefined = policyDefined;
                IdeologicalMatch = ideologicalMatch;
                IdeologicalMatchDefined = ideologicalDefined;
                Reputation = reputation;
                LeaderAppeal = leaderAppeal;
                CampaignEffectiveness = campaignEffectiveness;
                Total = total;
            }
        }

        /// <summary>Compatibility of one party with one voter group, 0–100 (§7).</summary>
        public static double Score(PartyProfile party, VoterGroupProfile group)
        {
            return Explain(party, group).Total;
        }

        /// <summary>The same figure with its five terms exposed.</summary>
        public static Breakdown Explain(PartyProfile party, VoterGroupProfile group)
        {
            double policy = PolicyMatch(party.PolicyPositions, group.IssuePositions, group.IssueWeights, out bool policyDefined);
            double ideology = IdeologicalMatch(party.Ideology, group.Ideology, out bool ideologyDefined);

            double reputation = ElectionScales.Clamp(party.Reputation);
            double leader = ElectionScales.Clamp(party.LeaderAppeal);
            double campaign = ElectionScales.Clamp(party.CampaignEffectiveness);

            // Redistribute the weight of any undefined sub-score across the defined ones, so a
            // sparse profile is not silently punished (see the class doc).
            double weightSum = WeightReputation + WeightLeaderAppeal + WeightCampaignEffectiveness;
            if (policyDefined) { weightSum += WeightPolicyMatch; }
            if (ideologyDefined) { weightSum += WeightIdeologicalMatch; }

            double total = 0.0;
            if (policyDefined) { total += WeightPolicyMatch * policy; }
            if (ideologyDefined) { total += WeightIdeologicalMatch * ideology; }
            total += WeightReputation * reputation;
            total += WeightLeaderAppeal * leader;
            total += WeightCampaignEffectiveness * campaign;
            total = ElectionScales.Clamp(total / weightSum);

            return new Breakdown(policy, policyDefined, ideology, ideologyDefined, reputation, leader, campaign, total);
        }

        /// <summary>
        /// How well a party's positions match what a group wants, WEIGHTED BY HOW MUCH THE GROUP
        /// CARES (§6): sum over issues of weight × (100 − |partyPosition − groupPosition|), divided
        /// by the weight actually used. An issue either side leaves undefined, or whose weight is
        /// zero, contributes nothing to either numerator or denominator.
        /// </summary>
        public static double PolicyMatch(IssueVector partyPositions, IssueVector groupPositions, IssueVector groupWeights, out bool defined)
        {
            double weighted = 0.0;
            double weightUsed = 0.0;
            for (int i = 0; i < IssueVector.IssueCount; i++)
            {
                var issue = (IssueId)i;
                if (!partyPositions.IsDefined(issue) || !groupPositions.IsDefined(issue) || !groupWeights.IsDefined(issue))
                {
                    continue;
                }

                double weight = ElectionScales.Clamp(groupWeights[issue]);
                if (weight <= 0.0) { continue; }

                double closeness = ElectionScales.Max - Math.Abs(partyPositions[issue] - groupPositions[issue]);
                weighted += weight * ElectionScales.Clamp(closeness);
                weightUsed += weight;
            }

            defined = weightUsed > 0.0;
            return defined ? weighted / weightUsed : 0.0;
        }

        /// <summary>Ideological closeness across §4's axes: 100 − the mean absolute distance over the axes BOTH sides define (NaN axes skipped, never centred).</summary>
        public static double IdeologicalMatch(IdeologyVector partyIdeology, IdeologyVector groupIdeology, out bool defined)
        {
            double distanceSum = 0.0;
            int axesUsed = 0;
            for (int i = 0; i < IdeologyVector.AxisCount; i++)
            {
                var axis = (IdeologyAxis)i;
                if (!partyIdeology.IsDefined(axis) || !groupIdeology.IsDefined(axis)) { continue; }

                distanceSum += Math.Abs(partyIdeology[axis] - groupIdeology[axis]);
                axesUsed++;
            }

            defined = axesUsed > 0;
            return defined ? ElectionScales.Clamp(ElectionScales.Max - distanceSum / axesUsed) : 0.0;
        }
    }
}
