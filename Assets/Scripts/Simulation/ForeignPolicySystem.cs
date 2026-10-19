using System.Collections.Generic;
using PoliSim.Data;
using UnityEngine;

namespace PoliSim.Simulation
{
    /// <summary>
    /// Continuous Time Migration Phase 0 (Master Sequence step 3) short-term gameplay scaffolding:
    /// one small proof-of-pattern interrupt slice, deliberately NOT the full "ongoing-process
    /// budgets"/"decisions and interrupts system" scope the Master Roadmap's Phase 0 spec gestures
    /// at, and NOT a law-passing mechanic (that's explicitly Political Systems Overhaul Part B's job
    /// - the roadmap itself says not to build a competing version here). Reuses CabinetSystem's own
    /// shape (a flat pool, a per-tick roll chance, apply-on-pick) but flattened to a single
    /// undifferentiated pool (no per-portfolio/per-philosophy branching - there's only one "portfolio"
    /// here) and rolled per DAY instead of per TURN, since these are meant to land between turn
    /// boundaries now that the calendar advances continuously - see
    /// SimulationManager.TryRollForeignPolicyMeeting, called once per simulated day from
    /// GameController.Update's day-processing loop.
    /// </summary>
    public static class ForeignPolicySystem
    {
        /// <summary>
        /// Chance PER DAY that a meeting fires, only rolled while no meeting is already pending (see
        /// SimulationManager.TryRollForeignPolicyMeeting). 0.01 was calibrated as "roughly one
        /// meeting per turn" when a turn was 121 days; at DaysPerTurn = 365 the same rate is ~3.65
        /// expected meetings per turn (97% chance of at least one), while the DAY-denominated
        /// experience - about one meeting per 100 days of play - is exactly what it always was.
        /// ⚠ R4-4 corrected this comment only (the 121-sweep precedent, zero behavior); whether
        /// ~3.65/turn is the INTENDED pacing or an inherited one is flagged for playtesting in the
        /// R4-4 record, deliberately not tuned here.
        /// </summary>
        /// <summary>[AUTHORED-DRAFT] pacing figure, and ⚠ **flagged as unresolved by its own record**: the
        /// R4-4 note above says whether ~3.65 meetings a turn is the INTENDED pacing or an inherited one
        /// is a question for playtesting, and it was deliberately not tuned. The mark records the class;
        /// the open question stays open.</summary>
        private const float MeetingChancePerDay = 0.01f;

        private static System.Random RandomSource => SimulationRandom.For(SimulationRandom.Stream.ForeignPolicy);

        /// <summary>The meeting cards. Every figure is [AUTHORED-DRAFT]. AF-31 (q31, ruled 2026-10-18): every card's options trade off - no option is at least as good as a
        /// rival on every LASTING count (approval, budget) and better on one (`CardTradeOffCheck`). A trade-balance nudge is not a cost: the trade system
        /// recomputes the balance at the next boundary. The entries AF-31 re-authored say so.</summary>
        private static readonly List<ForeignPolicyMeeting> MeetingPool = new List<ForeignPolicyMeeting>
        {
            new ForeignPolicyMeeting(
                "Trade Delegation Visit",
                "A neighboring country's trade delegation has requested a meeting to discuss lowering mutual barriers. Your Foreign Ministry sees an opening, but domestic producers are wary.",
                new ForeignPolicyMeetingOption("Pursue closer trade ties", tradeBalanceShock: -0.4f, approvalEffect: 0.5f, budgetImpact: -15f),   // AF-31 [AUTHORED-DRAFT]: the barriers lowered are tariff revenue given up - before it, this option won every lasting count
                new ForeignPolicyMeetingOption("Keep the relationship arm's length", tradeBalanceShock: 0f)),
            new ForeignPolicyMeeting(
                "Disaster Relief Request",
                "A friendly country has suffered a natural disaster and formally requested aid. Your ambassador there is pushing hard for a generous response.",
                new ForeignPolicyMeetingOption("Send substantial aid", budgetImpact: -60f, approvalEffect: 1.5f),
                new ForeignPolicyMeetingOption("Send a modest goodwill package", budgetImpact: -20f, approvalEffect: 0.5f),
                new ForeignPolicyMeetingOption("Decline for now", approvalEffect: -1f)),
            new ForeignPolicyMeeting(
                "Joint Security Exercise Proposal",
                "An allied country has proposed a joint military exercise, framed as routine cooperation but likely to draw attention from rivals in the region.",
                new ForeignPolicyMeetingOption("Agree to the exercise", budgetImpact: -30f, approvalEffect: -0.5f),
                new ForeignPolicyMeetingOption("Decline politely", approvalEffect: -1f)),   // AF-31 [AUTHORED-DRAFT]: the ally's disappointment is public (was 0) - before it, declining won on every count
            new ForeignPolicyMeeting(
                "Diplomatic Recognition Dispute",
                "A minor diplomatic incident involving one of your embassies abroad has reached the press. The other government wants a formal statement.",
                new ForeignPolicyMeetingOption("Issue a conciliatory statement", approvalEffect: -0.5f),
                new ForeignPolicyMeetingOption("Stand firm on the original position", approvalEffect: 0.5f, budgetImpact: -20f, tradeBalanceShock: -0.2f)),   // AF-31 [AUTHORED-DRAFT] (GA-9-1): the other government answers - contracts lost, and on trade (was +0.2 trade, nothing else); before it, this option won on every count
        };

        /// <summary>
        /// Rolls whether a meeting fires today - see MeetingChancePerDay. Caller (SimulationManager)
        /// is responsible for only calling this when no meeting is already pending.
        ///
        /// <paramref name="cadenceMultiplier"/> was R-S3e's per-scenario pacing lever - an authored
        /// scenario would scale this roll rather than anyone retuning the global constant; the challenge
        /// scenarios set it to 1 and were retired (§825), so it is 1 everywhere now. **1 by default, and the
        /// draw is taken either way** - the RandomSource call happens before the comparison, so a
        /// multiplier can never shift the ForeignPolicy stream's position and desync a seeded run
        /// against its baseline.
        /// </summary>
        public static ForeignPolicyMeeting TryRollMeeting(float cadenceMultiplier = 1f)
        {
            double roll = RandomSource.NextDouble();
            if (roll > MeetingChancePerDay * Mathf.Max(0f, cadenceMultiplier))
            {
                return null;
            }

            return MeetingPool[RandomSource.Next(MeetingPool.Count)];
        }

        /// <summary>Applies a chosen ForeignPolicyMeetingOption's one-time, bounded effect immediately - see ForeignPolicyMeetingOption's own doc comment for why each field lands where it does.</summary>
        public static void ApplyMeetingOption(Country country, ForeignPolicyMeetingOption option)
        {
            EconomyState state = country.State;
            // F1: routed through the one real path (stock + accumulator together) - see
            // SimulationManager.ApplyOneTimeBudgetImpact for the routing claim and the boundary.
            // P2-0.1 (2026-09-02): the authored figure is on the USA seed's scale and lands as the same
            // share of THIS country's GDP - see AuthoredImpactScale for the seam and its measurement.
            SimulationManager.ApplyOneTimeBudgetImpact(country, AuthoredImpactScale.ToCountryBillions(option.BudgetImpact, country));
            state.TradeBalance += option.TradeBalanceShock;
            TaxTerm.Shock(country, option.ApprovalEffect);   // A-LOOP q37 (review 879b M-1): the shock through TaxTerm.Shock - the same write, and the run without the tax term takes it through its own clamp
        }
    }
}
