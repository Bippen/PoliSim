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

        private static readonly List<ForeignPolicyMeeting> MeetingPool = new List<ForeignPolicyMeeting>
        {
            new ForeignPolicyMeeting(
                "Trade Delegation Visit",
                "A neighboring country's trade delegation has requested a meeting to discuss lowering mutual barriers. Your Foreign Ministry sees an opening, but domestic producers are wary.",
                new ForeignPolicyMeetingOption("Pursue closer trade ties", tradeBalanceShock: -0.4f, approvalEffect: 0.5f),
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
                new ForeignPolicyMeetingOption("Decline politely", approvalEffect: 0f)),
            new ForeignPolicyMeeting(
                "Diplomatic Recognition Dispute",
                "A minor diplomatic incident involving one of your embassies abroad has reached the press. The other government wants a formal statement.",
                new ForeignPolicyMeetingOption("Issue a conciliatory statement", approvalEffect: -0.5f),
                new ForeignPolicyMeetingOption("Stand firm on the original position", approvalEffect: 0.5f, tradeBalanceShock: 0.2f)),
        };

        /// <summary>
        /// Rolls whether a meeting fires today - see MeetingChancePerDay. Caller (SimulationManager)
        /// is responsible for only calling this when no meeting is already pending.
        ///
        /// <paramref name="cadenceMultiplier"/> is R-S3e's per-scenario pacing lever: the authored
        /// scenario scales this roll rather than anyone retuning the global constant, so pacing is a
        /// design value per scenario (a diplomatic start legitimately wants more interrupts; a
        /// disinflation run wants the player watching the Phillips curve). **1 by default, and the
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
            SimulationManager.ApplyOneTimeBudgetImpact(country, option.BudgetImpact);
            state.TradeBalance += option.TradeBalanceShock;
            state.ApprovalRating = Mathf.Clamp(state.ApprovalRating + option.ApprovalEffect, 0f, 100f);
        }
    }
}
