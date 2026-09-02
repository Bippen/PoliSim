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
    }
}
