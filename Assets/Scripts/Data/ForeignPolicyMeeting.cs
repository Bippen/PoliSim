using System.Collections.Generic;

namespace PoliSim.Data
{
    /// <summary>
    /// One response a player can pick for a fired ForeignPolicyMeeting - a one-time, bounded shock
    /// applied immediately when chosen (see ForeignPolicySystem.ApplyMeetingOption), the SAME shape
    /// CabinetDecisionOption already established (most options only ever set 1-2 fields, the rest
    /// stay 0). TradeBalanceShock originated here as the one field CabinetDecisionOption didn't
    /// have - a meeting with a foreign counterpart being the natural place for a trade-relations
    /// nudge, landing on a stat that already mean-reverts every turn (MacroSystem's own trade
    /// balance computation), so a one-time nudge fades naturally rather than needing its own
    /// ceiling, matching CrimeIndexShock/PovertyRateShock's own reasoning. R4-4's Foreign Affairs
    /// portfolio adopted the same field onto CabinetDecisionOption (ruling R2), so the two
    /// interrupt channels now share it - deliberately, as they share the apply-on-pick idiom.
    /// </summary>
    public struct ForeignPolicyMeetingOption
    {
        public string Label;
        public float ApprovalEffect;
        public float BudgetImpact;
        public float TradeBalanceShock;

        public ForeignPolicyMeetingOption(string label, float approvalEffect = 0f, float budgetImpact = 0f, float tradeBalanceShock = 0f)
        {
            Label = label;
            ApprovalEffect = approvalEffect;
            BudgetImpact = budgetImpact;
            TradeBalanceShock = tradeBalanceShock;
        }
    }

    /// <summary>
    /// One interactive foreign policy meeting: short flavor text plus 2-3 ForeignPolicyMeetingOption
    /// responses. Continuous Time Migration Phase 0 (Master Sequence step 3) short-term gameplay
    /// scaffolding - a single small proof-of-pattern interrupt slice reusing Cabinet's decision-modal
    /// shape (see CabinetDecision), deliberately NOT a full mirrored per-portfolio/per-philosophy pool
    /// like Cabinet's own DecisionPool. Rolled at a small daily chance (see
    /// ForeignPolicySystem.MeetingChancePerDay) rather than once per turn, since these are explicitly
    /// meant to land BETWEEN turn boundaries now that the calendar advances continuously - see
    /// SimulationManager.TryRollForeignPolicyMeeting.
    /// </summary>
    public class ForeignPolicyMeeting
    {
        public string Name;
        public string Description;
        public List<ForeignPolicyMeetingOption> Options = new List<ForeignPolicyMeetingOption>();

        public ForeignPolicyMeeting() { }

        public ForeignPolicyMeeting(string name, string description, params ForeignPolicyMeetingOption[] options)
        {
            Name = name;
            Description = description;
            Options = new List<ForeignPolicyMeetingOption>(options);
        }
    }
}
