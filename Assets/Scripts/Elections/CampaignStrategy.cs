using System;

namespace PoliSim.Elections
{
    /// <summary>
    /// W-B6 / SPEC §11 — the five campaign strategies as MODIFIERS OVER THE WHOLE CHAIN. PURE
    /// FUNCTIONS, WIRED TO NOTHING (R-N2).
    ///
    /// A strategy is not an action and not a vote delta: it is a set of multipliers applied to
    /// §42's stages for a message aimed at a voter group, and every multiplier depends on WHO the
    /// group is (how loyal, whether it prioritises the message's issue). That is what makes each
    /// strategy a trade-off rather than a bonus — the same strategy that lifts one group lowers
    /// another, and which groups an electorate contains decides which strategy wins. The
    /// harness's sweep asserts exactly that: across electorates no strategy dominates.
    ///
    /// **The five, each as the spec's own bullets turned into a shape** (all [AUTHORED-DRAFT],
    /// R-N4; the SHAPES are the spec's claims, the magnitudes are game fiction, one line each in
    /// the prototype log, calibrated by play):
    /// - **Broad Appeal** — "small gains across many groups, lower ideological intensity, reduced
    ///   polarization": reach ×1.15 (the message is made for everyone), persuasion ×0.85 per head
    ///   for every group, polarisation (salience shift) ×0.5.
    /// - **Base Mobilization** — "increased turnout among loyal voters, lower persuasion of swing
    ///   voters, stronger grassroots organization": enthusiasm ×(1 + 0.6 × loyalty), persuasion
    ///   ×(1 − 0.5 × swing) — a loyal group's turnout lifts, a swing group hears nothing new.
    /// - **Swing Voter** — "strong gains among independents, potential loss of ideological
    ///   voters": persuasion ×(0.7 + 0.8 × swing), enthusiasm ×(1 − 0.3 × loyalty).
    /// - **Negative Campaign** — "can reduce opponent popularity, high media attention, risk of
    ///   backlash, increased polarization": 60 % of the message's persuasion lands AGAINST the
    ///   targeted opponent (a negative pressure on their compatibility — the ONLY route by which a
    ///   campaign can lower another party, and still through `CampaignPressure`), own persuasion
    ///   ×0.8, credibility ×0.9 (the backlash, as an expected cost — a seeded backlash EVENT is
    ///   §17/§18's, W-B8), media attention ×1.5 (W-B9's input), polarisation ×1.5.
    /// - **Populist** — "strong gains among voters prioritizing those issues, reduced support
    ///   among other groups": persuasion ×1.5 and enthusiasm ×1.3 for a group that prioritises the
    ///   focus issue, persuasion ×0.6 for a group that does not.
    /// `None` is the identity — what every earlier harness measured.
    ///
    /// Loyalty enters as the group's 0–100 value (§5's `PartyLoyalty`, or W-A1's derived
    /// per-party loyalty where the electorate is one national group); `swing = 1 − loyalty/100`.
    /// </summary>
    public enum CampaignStrategy
    {
        None = 0,
        BroadAppeal = 1,
        BaseMobilization = 2,
        SwingVoter = 3,
        NegativeCampaign = 4,
        Populist = 5,
    }

    /// <summary>The multipliers one strategy applies to one message aimed at one group. 1.0 everywhere is the identity.</summary>
    public readonly struct StrategyModifiers
    {
        public readonly double ReachMultiplier;
        public readonly double PersuasionMultiplier;
        public readonly double EnthusiasmMultiplier;
        public readonly double CredibilityMultiplier;
        public readonly double SalienceShiftMultiplier;
        /// <summary>How much more (or less) newsworthy the message is — §13's input, consumed by W-B9. Carried, not yet read by any chain stage.</summary>
        public readonly double MediaAttentionMultiplier;
        /// <summary>The share of the message's (modified) persuasion that lands as NEGATIVE pressure on the targeted opponent; 0 for every strategy but the negative campaign.</summary>
        public readonly double OpponentShare;

        public StrategyModifiers(double reach, double persuasion, double enthusiasm, double credibility,
            double salienceShift, double mediaAttention, double opponentShare)
        {
            ReachMultiplier = reach; PersuasionMultiplier = persuasion; EnthusiasmMultiplier = enthusiasm;
            CredibilityMultiplier = credibility; SalienceShiftMultiplier = salienceShift;
            MediaAttentionMultiplier = mediaAttention; OpponentShare = opponentShare;
        }

        public static StrategyModifiers Identity => new StrategyModifiers(1, 1, 1, 1, 1, 1, 0);
    }

    public static class CampaignStrategyModel
    {
        // [AUTHORED-DRAFT] magnitudes - the class doc carries the shapes they implement.
        public const double BroadReach = 1.15;
        public const double BroadPersuasion = 0.85;
        public const double BroadPolarisation = 0.5;
        public const double BaseEnthusiasmPerLoyalty = 0.6;
        public const double BasePersuasionCutPerSwing = 0.5;
        public const double SwingPersuasionFloor = 0.7;
        public const double SwingPersuasionPerSwing = 0.8;
        public const double SwingEnthusiasmCutPerLoyalty = 0.3;
        public const double NegativeOpponentShare = 0.6;
        public const double NegativeOwnPersuasion = 0.8;
        public const double NegativeCredibility = 0.9;
        public const double NegativeMediaAttention = 1.5;
        public const double NegativePolarisation = 1.5;
        public const double PopulistFocusPersuasion = 1.5;
        public const double PopulistFocusEnthusiasm = 1.3;
        public const double PopulistOtherPersuasion = 0.6;

        public static readonly CampaignStrategy[] TheFive =
        {
            CampaignStrategy.BroadAppeal, CampaignStrategy.BaseMobilization, CampaignStrategy.SwingVoter,
            CampaignStrategy.NegativeCampaign, CampaignStrategy.Populist,
        };

        /// <summary>
        /// The modifiers a strategy applies to a message aimed at a group of the given loyalty
        /// (0–100) which does or does not prioritise the message's focus issue.
        /// </summary>
        public static StrategyModifiers Modifiers(CampaignStrategy strategy, double groupLoyalty0To100, bool groupPrioritisesFocusIssue)
        {
            double loyalty = ElectionScales.Clamp(groupLoyalty0To100) / ElectionScales.Max;
            double swing = 1.0 - loyalty;

            switch (strategy)
            {
                case CampaignStrategy.None:
                    return StrategyModifiers.Identity;

                case CampaignStrategy.BroadAppeal:
                    return new StrategyModifiers(BroadReach, BroadPersuasion, 1.0, 1.0, BroadPolarisation, 1.0, 0.0);

                case CampaignStrategy.BaseMobilization:
                    return new StrategyModifiers(1.0, 1.0 - BasePersuasionCutPerSwing * swing,
                        1.0 + BaseEnthusiasmPerLoyalty * loyalty, 1.0, 1.0, 1.0, 0.0);

                case CampaignStrategy.SwingVoter:
                    return new StrategyModifiers(1.0, SwingPersuasionFloor + SwingPersuasionPerSwing * swing,
                        1.0 - SwingEnthusiasmCutPerLoyalty * loyalty, 1.0, 1.0, 1.0, 0.0);

                case CampaignStrategy.NegativeCampaign:
                    return new StrategyModifiers(1.0, NegativeOwnPersuasion, 1.0, NegativeCredibility,
                        NegativePolarisation, NegativeMediaAttention, NegativeOpponentShare);

                case CampaignStrategy.Populist:
                    return groupPrioritisesFocusIssue
                        ? new StrategyModifiers(1.0, PopulistFocusPersuasion, PopulistFocusEnthusiasm, 1.0, 1.0, 1.0, 0.0)
                        : new StrategyModifiers(1.0, PopulistOtherPersuasion, 1.0, 1.0, 1.0, 1.0, 0.0);

                default:
                    throw new ArgumentException($"{strategy} is not one of §11's five strategies");
            }
        }

        /// <summary>
        /// §42's chain for one message under a strategy: `CampaignActions.Resolve` with the
        /// reach and credibility multipliers applied to its INPUTS (they are stages of the chain,
        /// so a zero anywhere still annihilates the effect) and the persuasion, enthusiasm and
        /// salience-shift multipliers applied to its outputs. Returns the same `ChainTrace` every
        /// other consumer reads — a strategy cannot write a vote share either.
        /// </summary>
        public static CampaignActions.ChainTrace Resolve(CampaignActions.ActionSpec spec, double audience,
            double issueSalience, double issueMatch, double credibility, double spend, StrategyModifiers m)
        {
            CampaignActions.ChainTrace t = CampaignActions.Resolve(spec, audience * m.ReachMultiplier, issueSalience,
                issueMatch, credibility * m.CredibilityMultiplier, spend);

            return new CampaignActions.ChainTrace(t.Kind, t.Reach, t.Salience, t.Exposure, t.Relevance, t.Credibility,
                t.Persuasion * m.PersuasionMultiplier, t.Enthusiasm * m.EnthusiasmMultiplier,
                t.SalienceShift * m.SalienceShiftMultiplier);
        }

        /// <summary>
        /// Whether a group "prioritises" an issue for the populist's purposes: the issue's weight
        /// is at or above the group's mean weight over the issues it defines. A group with no
        /// defined weights prioritises nothing.
        /// </summary>
        public static bool Prioritises(IssueVector groupWeights, IssueId issue)
        {
            if (!groupWeights.IsDefined(issue)) { return false; }

            double sum = 0.0;
            int n = 0;
            for (int i = 0; i < IssueVector.IssueCount; i++)
            {
                var id = (IssueId)i;
                if (!groupWeights.IsDefined(id)) { continue; }
                sum += groupWeights[id];
                n++;
            }

            return n > 0 && groupWeights[issue] >= sum / n;
        }
    }
}
