using System.Collections.Generic;

namespace PoliSim.Data
{
    /// <summary>
    /// One response a player can pick for a fired CabinetDecision - a one-time, bounded shock applied
    /// immediately when chosen (see CabinetSystem.ApplyDecisionOption), the SAME "one-time shock that
    /// fades via the target stat's own existing mean-reversion" idiom EventSystem.EconomicEvent
    /// already established, not a persistent structural change. Deliberately a small flat set of
    /// fields mirroring EconomicEvent's own shape (GdpShockPercent/InflationShockPoints/ApprovalEffect)
    /// - most options only ever set 1-2 of these, the rest stay 0, rather than one field per portfolio
    /// needing its own option type. CrimeIndexShock/PovertyRateShock land on stats that already
    /// mean-revert every turn (MacroSystem.ApplyCrimeIndex/ApplyPovertyRate), so a one-time nudge
    /// naturally fades rather than needing its own separate ceiling. BudgetImpact is Finance/
    /// Treasury's channel instead of a CollectionEfficiency shock specifically BECAUSE
    /// CollectionEfficiency has no reversion mechanism of its own (a structural per-country constant,
    /// only ever nudged by CabinetMinister.CompetenceBias at point-of-use, never mutated in place) -
    /// a one-time addition to the already-cumulative, already-turn-adjusted EconomyState.Budget is the
    /// contained, bounded landing spot the Master Roadmap's own Finance/Treasury guidance asked for.
    /// </summary>
    public struct CabinetDecisionOption
    {
        public string Label;
        public float CrimeIndexShock;
        public float PovertyRateShock;
        public float BudgetImpact;
        public float ApprovalEffect;

        /// <summary>ROUND 4 BATCH R4-4 (ruling R2): the Foreign Affairs channel -
        /// ForeignPolicyMeetingOption established this exact one-time shock (TradeBalance is
        /// recomputed by the trade system every turn, so a nudge fades naturally, the same
        /// qualification CrimeIndexShock/PovertyRateShock passed). That struct's doc comment used
        /// to call this "the one field CabinetDecisionOption doesn't have" - true until a cabinet
        /// portfolio existed whose decisions are foreign-relations-shaped.</summary>
        public float TradeBalanceShock;

        /// <summary>ROUND 4 BATCH R4-4 (ruling R2): the Education channel - lands on
        /// EconomyState.YouthUnemployment, which mean-reverts by construction (R4-1's model), so
        /// the same fade rule applies. Youth-U has no downstream readers (a Round 4 inputs-only
        /// stat), so this write's rule-11 ceiling audit is empty - and the direction is compatible
        /// with the standing posture, which bars new STATS from writing existing variables, not
        /// decisions from nudging new stats.</summary>
        public float YouthUnemploymentShock;

        public CabinetDecisionOption(string label, float crimeIndexShock = 0f, float povertyRateShock = 0f, float budgetImpact = 0f, float approvalEffect = 0f, float tradeBalanceShock = 0f, float youthUnemploymentShock = 0f)
        {
            Label = label;
            CrimeIndexShock = crimeIndexShock;
            PovertyRateShock = povertyRateShock;
            BudgetImpact = budgetImpact;
            ApprovalEffect = approvalEffect;
            TradeBalanceShock = tradeBalanceShock;
            YouthUnemploymentShock = youthUnemploymentShock;
        }
    }

    /// <summary>
    /// One interactive cabinet scenario: short flavor text plus 2-3 CabinetDecisionOption responses,
    /// belonging to a specific Portfolio AND CabinetMinisterPhilosophy (see CabinetSystem.DecisionPool
    /// - a Reformist minister's pool reads as genuinely different scenarios from a Traditionalist
    /// minister's, not the same text with a different number, per the Master Roadmap's own explicit
    /// instruction). Rolled probabilistically per appointed minister each turn - see
    /// CabinetSystem.TryRollDecisions.
    /// </summary>
    public class CabinetDecision
    {
        public string Name;
        public string Description;
        public List<CabinetDecisionOption> Options = new List<CabinetDecisionOption>();

        public CabinetDecision() { }

        public CabinetDecision(string name, string description, params CabinetDecisionOption[] options)
        {
            Name = name;
            Description = description;
            Options = new List<CabinetDecisionOption>(options);
        }
    }
}
