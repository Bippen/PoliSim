using System;

namespace PoliSim.Elections
{
    /// <summary>
    /// SPEC §26 — Get-Out-The-Vote: "winning support is not enough", the campaign must actually
    /// get supporters to vote. PURE FUNCTIONS, WIRED TO NOTHING (R-N2).
    ///
    /// The spec's formula, verbatim:
    /// <code>
    /// Base Turnout x Political Engagement x Campaign Mobilization
    ///              x Candidate Enthusiasm x Election Salience
    /// </code>
    /// Base turnout is a RATE (0–1, the group's habitual participation — the sourced national
    /// turnouts are its calibration target). The other four are 0–100 attributes, and a bare
    /// product of five raw factors would collapse to nothing, so each is mapped to a MULTIPLIER
    /// around 1.0: an attribute at 50 leaves turnout unchanged, at 100 it lifts it by the factor's
    /// span, at 0 it cuts it by the same span.
    ///
    /// **[AUTHORED-DRAFT] spans** (R-N4; logged one line each, all strikeable). They are
    /// deliberately unequal, and the ordering is the design claim:
    /// - `EngagementSpan = 0.30` — habitual political engagement is the strongest of the four:
    ///   who votes is mostly settled before any campaign begins.
    /// - `MobilizationSpan = 0.20` — what door-knocking, phone banking and election-day operations
    ///   can actually move. Second-largest because §26's whole point is that grassroots work is
    ///   strategically important, and §10's offices must be worth building.
    /// - `EnthusiasmSpan = 0.15` — candidate enthusiasm; real but smaller than organisation, so a
    ///   charismatic leader cannot substitute for a field operation.
    /// - `SalienceSpan = 0.15` — how much this particular election feels like it matters.
    /// Combined extremes therefore range roughly 0.35× to 1.9× of base, and the result is clamped
    /// to [0, 1] because a turnout rate above 100 % is not a thing.
    ///
    /// Note what is NOT here: no party-specific term. Turnout is a property of a GROUP in a
    /// context, and which party the group then votes for is §8's business. Keeping them separate
    /// is what lets §31 later say "you lost on turnout, not on persuasion" and mean it.
    /// </summary>
    public static class TurnoutModel
    {
        public const double EngagementSpan = 0.30;
        public const double MobilizationSpan = 0.20;
        public const double EnthusiasmSpan = 0.15;
        public const double SalienceSpan = 0.15;

        /// <summary>Maps a 0–100 attribute to a multiplier in [1-span, 1+span]; 50 is neutral.</summary>
        public static double Multiplier(double attribute0To100, double span)
        {
            double normalised = (ElectionScales.Clamp(attribute0To100) - 50.0) / 50.0;   // -1 .. +1
            return 1.0 + span * normalised;
        }

        /// <summary>
        /// §26's turnout rate for one voter group in one context. <paramref name="baseTurnout"/>
        /// is a rate in [0,1]; the rest are 0–100 attributes. Returns a rate in [0,1].
        /// </summary>
        public static double Turnout(double baseTurnout, double politicalEngagement, double campaignMobilization,
            double candidateEnthusiasm, double electionSalience)
        {
            double rate = baseTurnout
                          * Multiplier(politicalEngagement, EngagementSpan)
                          * Multiplier(campaignMobilization, MobilizationSpan)
                          * Multiplier(candidateEnthusiasm, EnthusiasmSpan)
                          * Multiplier(electionSalience, SalienceSpan);

            return rate < 0.0 ? 0.0 : (rate > 1.0 ? 1.0 : rate);
        }

        /// <summary>The same for a whole group table, one context shared across groups (each group brings its own base turnout and engagement).</summary>
        public static double[] Turnout(VoterGroupProfile[] groups, double campaignMobilization,
            double candidateEnthusiasm, double electionSalience)
        {
            var rates = new double[groups.Length];
            for (int i = 0; i < groups.Length; i++)
            {
                rates[i] = Turnout(groups[i].TurnoutBase / ElectionScales.Max, groups[i].PoliticalEngagement,
                    campaignMobilization, candidateEnthusiasm, electionSalience);
            }

            return rates;
        }
    }
}
