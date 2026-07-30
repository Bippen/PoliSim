using System.Collections.Generic;
using PoliSim.Data;
using UnityEngine;

namespace PoliSim.Simulation
{
    /// <summary>
    /// The Eurozone's shared-rate mechanic: each member sharing a CurrencyZone (Germany/France/Italy,
    /// currently the only zone shared by more than one country - see CurrencySystem.
    /// SharesCurrencyZoneWithOthers) gets a "voice" on the shared rate proportional to its own share
    /// of the three countries' combined GDP (a simplified version of the real ECB's "capital key" -
    /// not a precise replica), applied to its own TaylorRule.GetSuggestedInterestRate reading - a
    /// member with severe inflation or a large output gap pulls the shared rate more than a smaller,
    /// calmer one, the same directional logic as the real ECB Governing Council. Whichever member the
    /// player currently controls gets a modest, bounded push on top of that blend (mirroring a Fed
    /// Chair's RateBias in scale, not USA's unilateral authority) via the same PolicyDecision.
    /// InterestRateChange field Sweden/Poland already use - the other members' contribution is always
    /// their own current-turn Taylor Rule reading, unweighted by any player input, since an AI-
    /// controlled country always gets PolicyDecision.None() (InterestRateChange defaults to 0), the
    /// same convention every other decision field already follows. Does not touch Sweden/Poland's own
    /// independent-currency mechanic or USA's Fed Chair mechanic - this only ever runs for a
    /// multi-country shared CurrencyZone.
    /// </summary>
    public static class EurozoneRateSystem
    {
        /// <summary>
        /// A national governor's bounded push range - deliberately smaller than a Fed Chair's own
        /// RateBias range (+-1.5, see FederalReserveSystem.CandidatePool), reflecting a national
        /// governor's real but limited sway over a currency-union-wide rate, unlike USA's own
        /// unilateral Fed.
        /// </summary>
        public const float MemberRatePushRange = 0.75f;

        /// <summary>Fraction of the gap between the zone's current rate and this turn's blended target that closes each turn - matches FederalReserveSystem.RateAdjustmentSpeed's value and role (a real central bank moves gradually, not straight to its own textbook target every meeting).</summary>
        private const float RateAdjustmentSpeed = 0.15f;

        /// <summary>
        /// This turn's GDP-weighted blend of every member's own TaylorRule.GetSuggestedInterestRate
        /// reading, sharing <paramref name="zoneMember"/>'s CurrencyZone. <paramref name="zoneMember"/>
        /// itself is used directly for its own contribution (rather than re-reading it from
        /// <paramref name="world"/>) so this works correctly for SimulationManager.PreviewTurn's
        /// throwaway clone too - the clone shares the same CurrencyZone reference as the real country
        /// but isn't itself present in world.Countries, so the other members are found there by
        /// iterating and excluding this one by Id.
        /// </summary>
        public static float GetBlendedSuggestedRate(World world, Country zoneMember)
        {
            CurrencyZone zone = zoneMember.CurrencyZone;
            float totalGdp = Mathf.Max(0f, zoneMember.State.GDP);
            float weightedSum = totalGdp * TaylorRule.GetSuggestedInterestRate(zoneMember);

            foreach (Country member in world.Countries)
            {
                if (member.Id == zoneMember.Id || member.CurrencyZone != zone)
                {
                    continue;
                }

                float gdp = Mathf.Max(0f, member.State.GDP);
                totalGdp += gdp;
                weightedSum += gdp * TaylorRule.GetSuggestedInterestRate(member);
            }

            return totalGdp > 0f ? weightedSum / totalGdp : TaylorRule.GetSuggestedInterestRate(zoneMember);
        }

        /// <summary>Sums every member's PolicyDecision.InterestRateChange, each clamped individually to [-MemberRatePushRange, +MemberRatePushRange] before summing - in practice only ever nonzero for whichever member the player is currently controlling.</summary>
        private static float GetMemberPush(World world, CurrencyZone zone, Dictionary<CountryId, PolicyDecision> decisions)
        {
            if (decisions == null)
            {
                return 0f;
            }

            float totalPush = 0f;
            foreach (Country member in world.Countries)
            {
                if (member.CurrencyZone != zone)
                {
                    continue;
                }

                if (decisions.TryGetValue(member.Id, out PolicyDecision decision))
                {
                    totalPush += Mathf.Clamp(decision.InterestRateChange, -MemberRatePushRange, MemberRatePushRange);
                }
            }

            return totalPush;
        }

        /// <summary>
        /// Moves the shared zone's rate partway (RateAdjustmentSpeed) toward this turn's target -
        /// GetBlendedSuggestedRate plus GetMemberPush, clamped to CurrencySystem's sane bounds -
        /// rather than jumping straight there. Called from CurrencySystem.ApplyInterestRateChanges for
        /// any CurrencyZone shared by more than one country.
        /// </summary>
        public static void ApplyEurozoneRate(World world, Country zoneMember, Dictionary<CountryId, PolicyDecision> decisions)
        {
            CurrencyZone zone = zoneMember.CurrencyZone;
            float blended = GetBlendedSuggestedRate(world, zoneMember);
            float push = GetMemberPush(world, zone, decisions);
            float target = Mathf.Clamp(blended + push, CurrencySystem.MinInterestRate, CurrencySystem.MaxInterestRate);

            float current = zone.InterestRate;
            zone.InterestRate = current + (target - current) * RateAdjustmentSpeed;
        }
    }
}
