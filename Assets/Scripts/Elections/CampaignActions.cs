using System;

namespace PoliSim.Elections
{
    /// <summary>
    /// W-B3 / SPEC §12 — the eight campaign actions, and §42's causal chain as the ONLY path an
    /// action's effect can take. PURE FUNCTIONS, WIRED TO NOTHING (R-N2).
    ///
    /// **The item's whole point, stated as an architectural constraint.** Spec §42 forbids
    /// `Campaign Action → +2 % Votes`. So no type in this file can express a vote share:
    /// <see cref="ChainTrace"/> has no share field, no preference field and no party index, and the
    /// only things an action produces are **persuasion** and **enthusiasm** pressures that a later
    /// layer must combine with compatibility and loyalty to get a preference. An action literally
    /// cannot write a vote share, because there is nowhere in its return type to put one.
    ///
    /// **The chain, in the spec's own order, as a product of necessary factors:**
    /// <code>
    /// reach      = audience x channelReach x §35Effectiveness(spend)
    /// salience   = how much this group cares about the message's issue   (0-1)
    /// exposure   = reach x attention                                     (people who noticed)
    /// relevance  = salience x issueMatch                                 (0-1)
    /// credibility= candidate credibility                                 (0-1)
    /// persuasion = exposure x relevance x credibility x persuasionWeight
    /// enthusiasm = exposure x credibility x enthusiasmWeight
    /// </code>
    /// Every stage MULTIPLIES. That is what makes the chain testable rather than decorative: set
    /// the audience to zero, or aim a message at an issue a group does not care about, or send a
    /// candidate nobody believes — and the effect is exactly zero, not merely smaller. A direct
    /// `+2 %` would survive all three; this cannot.
    ///
    /// **[AUTHORED-DRAFT] action costs and weights** (R-N4, one line each in the prototype log,
    /// all strikeable, all to be calibrated by play). The hour costs follow §9's own examples where
    /// it gives them (rally 4, interview 2, policy announcement 3); the money costs and the
    /// reach/persuasion/enthusiasm weights are game fiction shaped to §12's prose — a town hall
    /// persuades more per head and reaches fewer; door-knocking is the enthusiasm/turnout verb;
    /// television reaches most and persuades least per contact.
    /// </summary>
    public static class CampaignActions
    {
        /// <summary>What an action costs and how it behaves in the chain. All [AUTHORED-DRAFT].</summary>
        public readonly struct ActionSpec
        {
            public readonly CampaignActionKind Kind;
            public readonly double MoneyCost;
            public readonly double Hours;
            /// <summary>Fraction of the targeted audience the channel can touch at full spend.</summary>
            public readonly double ChannelReach;
            /// <summary>Share of those reached who actually attend to the message.</summary>
            public readonly double Attention;
            public readonly double PersuasionWeight;
            public readonly double EnthusiasmWeight;
            /// <summary>How much this action shifts the salience of the issue it is about (§18's channel, used by policy announcements most).</summary>
            public readonly double SalienceShift;
            /// <summary>True where the action is inherently local (its audience is one region).</summary>
            public readonly bool IsLocal;

            public ActionSpec(CampaignActionKind kind, double moneyCost, double hours, double channelReach,
                double attention, double persuasionWeight, double enthusiasmWeight, double salienceShift, bool isLocal)
            {
                Kind = kind; MoneyCost = moneyCost; Hours = hours; ChannelReach = channelReach;
                Attention = attention; PersuasionWeight = persuasionWeight;
                EnthusiasmWeight = enthusiasmWeight; SalienceShift = salienceShift; IsLocal = isLocal;
            }
        }

        /// <summary>
        /// Every stage of §42's chain, kept so a harness — and later §31's attribution — can show
        /// WHERE an effect came from. **Deliberately contains no vote share, no preference and no
        /// party**: this is the ceiling of what an action may produce.
        /// </summary>
        public readonly struct ChainTrace
        {
            public readonly CampaignActionKind Kind;
            public readonly double Reach;
            public readonly double Salience;
            public readonly double Exposure;
            public readonly double Relevance;
            public readonly double Credibility;
            public readonly double Persuasion;
            public readonly double Enthusiasm;
            public readonly double SalienceShift;

            public ChainTrace(CampaignActionKind kind, double reach, double salience, double exposure,
                double relevance, double credibility, double persuasion, double enthusiasm, double salienceShift)
            {
                Kind = kind; Reach = reach; Salience = salience; Exposure = exposure;
                Relevance = relevance; Credibility = credibility; Persuasion = persuasion;
                Enthusiasm = enthusiasm; SalienceShift = salienceShift;
            }

            /// <summary>True when every multiplicative stage was non-zero — i.e. the effect actually travelled the chain.</summary>
            public bool TravelledWholeChain =>
                Reach > 0 && Exposure > 0 && Relevance > 0 && Credibility > 0 && Persuasion > 0;
        }

        /// <summary>Who and what an action is aimed at. A null issue means a general message, which scores the group's AVERAGE salience rather than any issue's.</summary>
        public readonly struct ActionTarget
        {
            public readonly int RegionIndex;   // -1 = national
            public readonly int GroupIndex;    // -1 = all groups
            public readonly IssueId? Issue;

            public ActionTarget(int regionIndex, int groupIndex, IssueId? issue)
            {
                RegionIndex = regionIndex; GroupIndex = groupIndex; Issue = issue;
            }

            public static ActionTarget National(IssueId? issue = null) => new ActionTarget(-1, -1, issue);
        }

        /// <summary>The [AUTHORED-DRAFT] table. Hours follow §9's own figures where it gives them.</summary>
        public static ActionSpec Spec(CampaignActionKind kind)
        {
            switch (kind)
            {
                //                                          money    hrs  reach  atten  persu  enthu  salience local
                case CampaignActionKind.Rally:
                    return new ActionSpec(kind,             300_000,  4.0,  0.06,  0.80,  0.30,  1.00,  0.02,  true);
                case CampaignActionKind.TownHall:
                    return new ActionSpec(kind,              25_000,  3.0,  0.01,  0.95,  1.00,  0.55,  0.01,  true);
                case CampaignActionKind.DoorToDoor:
                    return new ActionSpec(kind,              15_000,  5.0,  0.02,  0.90,  0.55,  0.90,  0.00,  true);
                case CampaignActionKind.TelevisionAd:
                    return new ActionSpec(kind,             500_000,  1.0,  0.55,  0.35,  0.22,  0.15,  0.03,  false);
                case CampaignActionKind.DigitalAd:
                    return new ActionSpec(kind,             150_000,  1.0,  0.30,  0.45,  0.30,  0.20,  0.02,  false);
                case CampaignActionKind.SocialPost:
                    return new ActionSpec(kind,               5_000,  1.0,  0.12,  0.40,  0.20,  0.45,  0.02,  false);
                case CampaignActionKind.Interview:
                    return new ActionSpec(kind,                   0,  2.0,  0.20,  0.60,  0.45,  0.25,  0.02,  false);
                case CampaignActionKind.PolicyAnnouncement:
                    return new ActionSpec(kind,              50_000,  3.0,  0.18,  0.55,  0.50,  0.30,  0.10,  false);
                default:
                    throw new ArgumentException($"{kind} is not one of §12's eight campaign actions");
            }
        }

        /// <summary>The eight, in the spec's order — what a UI offers and what a harness sweeps.</summary>
        public static readonly CampaignActionKind[] TheEight =
        {
            CampaignActionKind.Rally,
            CampaignActionKind.TownHall,
            CampaignActionKind.DoorToDoor,
            CampaignActionKind.TelevisionAd,
            CampaignActionKind.DigitalAd,
            CampaignActionKind.SocialPost,
            CampaignActionKind.Interview,
            CampaignActionKind.PolicyAnnouncement,
        };

        /// <summary>
        /// Runs §42's chain for one action. Every stage multiplies the last, so a zero anywhere is
        /// a zero at the end — the property that makes "it travelled the chain" checkable.
        /// </summary>
        /// <param name="audience">People the action could in principle touch (region × group).</param>
        /// <param name="issueSalience">How much the target group cares about the message's issue, 0–1.</param>
        /// <param name="issueMatch">How well the party's position on that issue suits the group, 0–1.</param>
        /// <param name="credibility">The candidate's credibility with this audience, 0–1.</param>
        /// <param name="spend">Money actually spent, which enters through §35's saturating curve.</param>
        public static ChainTrace Resolve(ActionSpec spec, double audience, double issueSalience,
            double issueMatch, double credibility, double spend)
        {
            if (audience < 0) { throw new ArgumentException("audience cannot be negative"); }

            // 1. REACH - the audience the channel touches, scaled by §35's diminishing curve.
            //    A free action (interview) still reaches, because its cost is time not money.
            double spendFactor = spec.MoneyCost > 0
                ? CampaignEconomy.Effectiveness(spend, Math.Max(1.0, spec.MoneyCost))
                : 1.0;
            double reach = audience * spec.ChannelReach * spendFactor;

            // 2. SALIENCE - how much this audience cares about what the message is about.
            double salience = Clamp01(issueSalience);

            // 3. EXPOSURE - of those reached, who actually attended.
            double exposure = reach * Clamp01(spec.Attention);

            // 4. MESSAGE RELEVANCE - salience x how well the position suits them.
            double relevance = salience * Clamp01(issueMatch);

            // 5. CREDIBILITY - whether they believe the messenger.
            double credible = Clamp01(credibility);

            // 6. PERSUASION and ENTHUSIASM - the ONLY outputs. Not shares; pressures.
            double persuasion = exposure * relevance * credible * spec.PersuasionWeight;
            double enthusiasm = exposure * credible * spec.EnthusiasmWeight;

            // The salience shift an action leaves behind (§18's channel) also requires exposure -
            // an unheard announcement changes nobody's priorities.
            double salienceShift = exposure > 0 ? spec.SalienceShift * Clamp01(spendFactor) : 0.0;

            return new ChainTrace(spec.Kind, reach, salience, exposure, relevance, credible,
                persuasion, enthusiasm, salienceShift);
        }

        private static double Clamp01(double v) => v < 0 ? 0 : (v > 1 ? 1 : v);
    }

    /// <summary>
    /// Where an action's pressures accumulate. Still **no vote shares**: this holds per-party
    /// persuasion and enthusiasm pressure, which <see cref="ToCompatibilityBonus"/> converts into a
    /// COMPATIBILITY adjustment — so the preference is always recomputed by
    /// <see cref="PreferenceModel"/> from compatibility and loyalty, exactly as it is with no
    /// campaign at all. The campaign changes the inputs; it never touches the output.
    /// </summary>
    public sealed class CampaignPressure
    {
        private readonly double[] _persuasion;
        private readonly double[] _enthusiasm;

        public CampaignPressure(int partyCount)
        {
            _persuasion = new double[partyCount];
            _enthusiasm = new double[partyCount];
        }

        public int PartyCount => _persuasion.Length;

        public void Add(int partyIndex, CampaignActions.ChainTrace trace)
        {
            _persuasion[partyIndex] += trace.Persuasion;
            _enthusiasm[partyIndex] += trace.Enthusiasm;
        }

        public double Persuasion(int partyIndex) => _persuasion[partyIndex];

        public double Enthusiasm(int partyIndex) => _enthusiasm[partyIndex];

        /// <summary>
        /// [AUTHORED-DRAFT] `PersuasionPerCompatibilityPoint = 40 000` — persuasion pressure needed
        /// to move a party's compatibility with a group by one point of 100. Deliberately large:
        /// §39 forbids any single variable dominating, and a campaign that could rewrite
        /// compatibility outright would make positioning irrelevant, which is the opposite of §44.
        /// </summary>
        public const double PersuasionPerCompatibilityPoint = 40_000.0;

        /// <summary>Converts accumulated persuasion into a compatibility bonus per party — the ONLY route from campaigning into the vote model.</summary>
        public double[] ToCompatibilityBonus()
        {
            var bonus = new double[_persuasion.Length];
            for (int i = 0; i < bonus.Length; i++)
            {
                bonus[i] = _persuasion[i] / PersuasionPerCompatibilityPoint;
            }

            return bonus;
        }

        /// <summary>[AUTHORED-DRAFT] `EnthusiasmPerTurnoutPoint = 60 000` — enthusiasm pressure per point of turnout attribute (§26 consumes this, not the preference model).</summary>
        public const double EnthusiasmPerTurnoutPoint = 60_000.0;

        public double[] ToTurnoutBonus()
        {
            var bonus = new double[_enthusiasm.Length];
            for (int i = 0; i < bonus.Length; i++)
            {
                bonus[i] = _enthusiasm[i] / EnthusiasmPerTurnoutPoint;
            }

            return bonus;
        }
    }
}
