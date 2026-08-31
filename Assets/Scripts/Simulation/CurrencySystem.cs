using System.Collections.Generic;
using PoliSim.Data;
using UnityEngine;

namespace PoliSim.Simulation
{
    /// <summary>
    /// Interest rate policy and the resulting currency strength drift for countries with an
    /// independent currency. Countries that share a CurrencyZone (e.g. the Eurozone) share one
    /// interest rate and have no individual currency strength to model.
    /// </summary>
    public static class CurrencySystem
    {
        /// <summary>CONVENTION - bounds, not a forecast. Sane clamps for any CurrencyZone's interest rate; public so GameController's policy preview can clamp its own estimated rate the same way. 15% is above any policy rate the six have set in the modern era and is a guard against a runaway, not a claim that a rate could reach it.</summary>
        public const float MinInterestRate = 0f;
        public const float MaxInterestRate = 15f;

        /// <summary>CONVENTION - an index origin. 100 represents a country neither strong nor weak relative to its peers; the index has no unit and the number is a scale choice, not a measurement.</summary>
        /// <remarks>CONVENTION.</remarks>
        public const float NeutralCurrencyStrength = 100f;

        /// <summary>CONVENTION - a floor on the strength index (half the neutral 100), a state-space clamp rather than an economic claim.</summary>
        private const float MinCurrencyStrength = 50f;

        /// <summary>CONVENTION - a ceiling on the strength index (twice the neutral 100), the symmetric partner of the floor above.</summary>
        private const float MaxCurrencyStrength = 200f;

        /// <summary>[AUTHORED-DRAFT] scale factor: how much a 1 percentage point interest rate gap versus trade partners shifts the currency strength target. The DIRECTION is uncontroversial (higher relative rates attract capital and strengthen a currency); the 5 index points per point is a game figure on an index that has no unit, so no real estimate could be transplanted onto it.</summary>
        private const float InterestRateDifferentialScale = 5f;

        /// <summary>CONVENTION - a reversion speed on a unitless index, 0-1. How quickly currency strength moves toward its target each turn.</summary>
        private const float CurrencyStrengthDamping = 0.15f;

        /// <summary>
        /// Applies each turn's interest rate policy decisions. A country with a non-null
        /// Country.CurrentFedChair (USA, for now) bypasses PolicyDecision.InterestRateChange
        /// entirely - see FederalReserveSystem.ApplyFedChairInterestRate. A CurrencyZone shared by
        /// more than one country (Germany/France/Italy, the Eurozone) uses EurozoneRateSystem's own
        /// GDP-weighted, Taylor-Rule-blended mechanic instead - see that class. Every other country
        /// (an independent, single-country zone - Sweden, Poland) has its own decision's
        /// InterestRateChange applied directly as a raw rate delta, unchanged from before.
        /// </summary>
        public static void ApplyInterestRateChanges(World world, Dictionary<CountryId, PolicyDecision> decisions)
        {
            var processedZones = new HashSet<CurrencyZone>();

            foreach (Country country in world.Countries)
            {
                if (country.CurrentFedChair != null)
                {
                    FederalReserveSystem.ApplyFedChairInterestRate(country);
                    processedZones.Add(country.CurrencyZone);
                    continue;
                }

                CurrencyZone zone = country.CurrencyZone;
                if (!processedZones.Add(zone))
                {
                    continue;
                }

                if (SharesCurrencyZoneWithOthers(country, world))
                {
                    EurozoneRateSystem.ApplyEurozoneRate(world, country, decisions);
                    continue;
                }

                float totalChange = 0f;
                foreach (Country member in world.Countries)
                {
                    if (member.CurrencyZone != zone)
                    {
                        continue;
                    }

                    if (decisions != null && decisions.TryGetValue(member.Id, out PolicyDecision decision))
                    {
                        totalChange += decision.InterestRateChange;
                    }
                }

                zone.InterestRate = Mathf.Clamp(zone.InterestRate + totalChange, MinInterestRate, MaxInterestRate);
            }
        }

        /// <summary>True if this country's CurrencyZone instance is also used by at least one other country.</summary>
        public static bool SharesCurrencyZoneWithOthers(Country country, World world)
        {
            int sharingCount = 0;
            foreach (Country other in world.Countries)
            {
                if (other.CurrencyZone == country.CurrencyZone)
                {
                    sharingCount++;
                }
            }

            return sharingCount > 1;
        }

        /// <summary>
        /// Drifts a non-shared-currency country's CurrencyStrength toward a target set by how its
        /// interest rate compares to the average rate among its trade partners: relatively higher
        /// pulls strength up, relatively lower pulls it down. Shared-currency countries are skipped.
        /// </summary>
        public static void ApplyCurrencyStrength(Country country, World world)
        {
            if (SharesCurrencyZoneWithOthers(country, world))
            {
                return;
            }

            float partnerRateSum = 0f;
            int partnerCount = 0;
            foreach (TradePartner link in country.TradePartners)
            {
                Country partner = world.GetCountry(link.PartnerId);
                if (partner == null)
                {
                    continue;
                }

                partnerRateSum += partner.CurrencyZone.InterestRate;
                partnerCount++;
            }

            if (partnerCount == 0)
            {
                return;
            }

            float averagePartnerRate = partnerRateSum / partnerCount;
            float rateDifferential = country.CurrencyZone.InterestRate - averagePartnerRate;
            float target = NeutralCurrencyStrength + rateDifferential * InterestRateDifferentialScale;

            float strength = country.State.CurrencyStrength + (target - country.State.CurrencyStrength) * CurrencyStrengthDamping;
            country.State.CurrencyStrength = Mathf.Clamp(strength, MinCurrencyStrength, MaxCurrencyStrength);
        }
    }
}
