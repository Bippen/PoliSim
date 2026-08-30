using System;

namespace PoliSim.Elections
{
    /// <summary>Which part of a debate the screen is showing. The three states W-E5 is filmed in.</summary>
    public enum DebateStage
    {
        /// <summary>Before the first exchange: the preparation the player bought, and nothing about how it will go.</summary>
        Preparation = 0,
        /// <summary>Exchanges resolving one at a time; the rest have not happened and are drawn as absent, never as zero.</summary>
        InProgress = 1,
        /// <summary>Every exchange resolved: the margin, and the two shocks it produced.</summary>
        Verdict = 2,
    }

    /// <summary>
    /// W-E5 — everything the debate screen draws. PURE DATA (R-N2); carries a
    /// <see cref="CampaignSnapshot"/> so the money, the days left and the last poll on the strip are
    /// the same values every other campaign screen shows.
    ///
    /// **§36's gate, drawn as absence.** A debate in progress has resolved some exchanges and not
    /// others, and the ones that have not happened are shown as an em dash rather than a zero — the
    /// Desk's Year-0 convention. The screen never renders a points figure for an exchange that has
    /// not run, because a zero and an unknown are different claims and the player can tell them
    /// apart only if the screen does.
    ///
    /// **What the screen must NOT show, and the model makes that easy:** a debate produces no vote
    /// share, no preference and no party standing (`DebateResult` has no such member, asserted by
    /// reflection since W-B7). So the verdict panel can only report the performance indices, the
    /// margin, and the two SHOCKS — coverage and momentum — which is exactly the ceiling §15 sets.
    /// A screen that showed "+1.2 % in the polls" after a debate would be inventing the one number
    /// the model deliberately refuses to produce.
    /// </summary>
    public readonly struct DebateScreenSnapshot
    {
        public readonly CampaignSnapshot Campaign;
        public readonly DebateStage Stage;
        public readonly string NameA;
        public readonly string NameB;
        /// <summary>The two candidates' §16 attributes, as the screen shows them - the blend a move draws on is the model's, not a display average.</summary>
        public readonly CandidateProfile CandidateA;
        public readonly CandidateProfile CandidateB;
        public readonly DebatePreparation PreparationA;
        public readonly DebatePreparation PreparationB;
        /// <summary>The debate as far as it has run. In <see cref="DebateStage.Preparation"/> this is empty.</summary>
        public readonly DebateExchange[] Resolved;
        /// <summary>How many exchanges the debate will have in total - so the unresolved ones can be drawn as absent rather than omitted.</summary>
        public readonly int TotalExchanges;
        /// <summary>The finished result; only meaningful at <see cref="DebateStage.Verdict"/>.</summary>
        public readonly DebateResult Result;

        public DebateScreenSnapshot(CampaignSnapshot campaign, DebateStage stage, string nameA, string nameB,
            CandidateProfile candidateA, CandidateProfile candidateB,
            DebatePreparation preparationA, DebatePreparation preparationB,
            DebateExchange[] resolved, int totalExchanges, DebateResult result)
        {
            Campaign = campaign; Stage = stage; NameA = nameA; NameB = nameB;
            CandidateA = candidateA; CandidateB = candidateB;
            PreparationA = preparationA; PreparationB = preparationB;
            Resolved = resolved ?? new DebateExchange[0];
            TotalExchanges = Math.Max(totalExchanges, Resolved == null ? 0 : resolved.Length);
            Result = result;
        }

        public bool HasVerdict => Stage == DebateStage.Verdict;

        /// <summary>The running points so far - what a mid-debate screen can honestly total, which is not the performance index (that is a mean over ALL exchanges).</summary>
        public void RunningPoints(out double a, out double b)
        {
            a = 0.0; b = 0.0;
            foreach (DebateExchange e in Resolved) { a += e.PointsA; b += e.PointsB; }
            if (Resolved.Length > 0) { a /= Resolved.Length; b /= Resolved.Length; }
        }
    }
}
