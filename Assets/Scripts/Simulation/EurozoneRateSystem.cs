using System.Collections.Generic;
using PoliSim.Data;
using UnityEngine;

namespace PoliSim.Simulation
{
    /// <summary>
    /// The Eurozone's shared-rate mechanic: each member sharing a CurrencyZone (Germany/France/Italy,
    /// currently the only zone shared by more than one country - see CurrencySystem.
    /// SharesCurrencyZoneWithOthers) gets a "voice" on the shared rate proportional to its HICP COUNTRY WEIGHT - its share of the members'
    /// household consumption in current prices (FT-16, ruled 2026-09-23, §585; SOURCED: Eurostat, HICP metadata `prc_hicp_esms`, updated
    /// 2026-02-04: "The country weights are derived from National Accounts data for the HFMCE expressed in euros" - household final monetary
    /// consumption expenditure; the ECB's target is the euro area HICP those weights aggregate). Until §585 the weight was REAL GDP, which let a
    /// member's weight fall as its prices rose. Applied to each member's own TaylorRule.GetSuggestedInterestRate reading - a
    /// member with severe inflation or a tight labour market (its unemployment against its NAIRU;
    /// the rule reads that gap since pass 4, 2026-08-26) pulls the shared rate more than a smaller,
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
        /// <summary>[AUTHORED-DRAFT] - see the paragraph above for the reasoning it rests on. A member governor's sway over a currency-union rate is real and bounded; how many percentage points it is worth is not a published quantity, so 0.75 is a game figure carrying the right shape.</summary>
        public const float MemberRatePushRange = 0.75f;

        /// <summary>CONVENTION - a reversion speed, and DERIVED rather than separately chosen. Fraction of the gap between the zone's current rate and this turn's blended target that closes each turn - matches FederalReserveSystem.RateAdjustmentSpeed's value and role (a real central bank moves gradually, not straight to its own textbook target every meeting).</summary>
        private const float RateAdjustmentSpeed = 0.15f;

        /// <summary>
        /// This turn's HICP-country-weighted blend (<see cref="HicpCountryWeight"/>) of every member's own TaylorRule.GetSuggestedInterestRate
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
            // One basis per blend (the s585 review's F1): before the first day's national accounts write consumption, every member reads 0 and the
            // blend would fall to one member's own reading - the zone blends on nominal GDP until consumption exists for every member.
            bool consumptionWritten = zoneMember.State.Consumption > 0f;
            foreach (Country member in world.Countries)
            {
                if (member.Id != zoneMember.Id && member.CurrencyZone == zone && member.State.Consumption <= 0f) { consumptionWritten = false; }
            }
            float totalWeight = consumptionWritten ? HicpCountryWeight(zoneMember) : Mathf.Max(0f, zoneMember.State.NominalGdp);
            float weightedSum = totalWeight * TaylorRule.GetSuggestedInterestRate(zoneMember);

            foreach (Country member in world.Countries)
            {
                if (member.Id == zoneMember.Id || member.CurrencyZone != zone)
                {
                    continue;
                }

                float weight = consumptionWritten ? HicpCountryWeight(member) : Mathf.Max(0f, member.State.NominalGdp);
                totalWeight += weight;
                weightedSum += weight * TaylorRule.GetSuggestedInterestRate(member);
            }

            return totalWeight > 0f ? weightedSum / totalWeight : TaylorRule.GetSuggestedInterestRate(zoneMember);
        }

        /// <summary>FT-16 (§585): a member's HICP country weight before normalising - its household consumption at current prices (the identity's real
        /// Consumption × its price level), Eurostat's HFMCE in the model's terms. ⚠ The model's consumption is one flat share of output for every country
        /// (`MacroSystem.BaseConsumptionRate`), so today these weights read as NOMINAL GDP shares to within a thousandth - the ruling's "nominal shares"
        /// either way - and they do not reproduce Eurostat's published country weights (prc_hicp_cow: Italy's share of the three runs some four points above
        /// the model's); sourced consumption shares would be their own ruling. Zero before the first day's national accounts have written consumption - the
        /// blend then uses nominal GDP (<see cref="GetBlendedSuggestedRate"/>).</summary>
        public static float HicpCountryWeight(Country member)
            => Mathf.Max(0f, member.State.Consumption) * Mathf.Max(0.0001f, member.State.PriceLevel);

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
