using System;
using System.Collections.Generic;
using PoliSim.Simulation;

namespace PoliSim.Elections
{
    /// <summary>
    /// C-R4b step 3 (2026-09-02) — what a save carries of the player's campaign: NOT the run's state
    /// (`CampaignRun.State` is thirty-odd live objects, none of them shaped for a save), but what
    /// REPLAYS it exactly. A campaign is deterministic under its three RNG streams (`CampaignAi`,
    /// `Debate`, `Scandal`): given the streams' draw counts at the campaign's first day and the number
    /// of days stepped, `SimulationManager.RestoreCampaign` rebuilds the same Setup from the runtime
    /// tables, rewinds the three streams to these counts, steps the same days and lands on the same
    /// state - and on the same draw counts the save recorded, which it asserts. The replay is the
    /// harness's own reproducibility proof (`CampaignAiHarness` 1a) used as a save format.
    /// </summary>
    [Serializable]
    public class PlayerCampaignRecord
    {
        /// <summary>The election boundary this campaign runs up to (the first day of the election turn).</summary>
        public DateTime ElectionDate;
        /// <summary>The campaign's first day (`CampaignCalendar.CampaignStart` for that boundary).</summary>
        public DateTime StartDate;
        /// <summary>Days stepped so far - `CampaignRun.State.Day` at the save.</summary>
        public int DaysStepped;
        /// <summary>The three campaign streams' draw counts the moment the campaign began - the replay's rewind point.</summary>
        public Dictionary<SimulationRandom.Stream, int> DrawCountsAtStart = new Dictionary<SimulationRandom.Stream, int>();

        /// <summary>
        /// C-R4b step 4b: the PLAYER's decisions, by campaign day - what the HQ queued. The player's
        /// party plays each day exactly this list (`PartySetup.Script`), so the queue is part of what
        /// replays the campaign: a load re-steps the same days with the same queue and lands on the
        /// same state. Empty for a day means the party does nothing that day - the hours go unspent.
        /// </summary>
        public List<QueuedDecisionRecord> Queue = new List<QueuedDecisionRecord>();

        /// <summary>The decisions queued for one campaign day, in order.</summary>
        public List<QueuedDecisionRecord> QueuedFor(int day)
        {
            var list = new List<QueuedDecisionRecord>();
            foreach (QueuedDecisionRecord q in Queue) { if (q.Day == day) { list.Add(q); } }
            return list;
        }
    }

    /// <summary>One queued decision as the save carries it: the action, its target (a region index or −1 for national; an issue or −1 for the general message), the outlay.</summary>
    [Serializable]
    public class QueuedDecisionRecord
    {
        public int Day;
        public CampaignActionKind Kind;
        public int RegionIndex = -1;
        public int Issue = -1;
        public double Spend;

        /// <summary>The decision as the run resolves it - the spec's hours, the label from the region or "national".</summary>
        public AiDecision ToDecision(CampaignRun.Setup setup)
        {
            CampaignActions.ActionSpec spec = CampaignActions.Spec(Kind);
            IssueId? issue = Issue >= 0 ? (IssueId?)(IssueId)Issue : null;
            string label = RegionIndex >= 0 && RegionIndex < setup.Regions.Length ? setup.Regions[RegionIndex].Name : "national";
            var target = RegionIndex >= 0 ? new CampaignActions.ActionTarget(RegionIndex, -1, issue) : CampaignActions.ActionTarget.National(issue);
            return new AiDecision(Kind, target, label, Spend, spec.Hours, 0.0, false);
        }
    }
}
