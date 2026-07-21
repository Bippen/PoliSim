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
        private const float MinInterestRate = 0f;
        private const float MaxInterestRate = 15f;

        /// <summary>Currency strength index value representing a country neither strong nor weak relative to its peers.</summary>
        public const float NeutralCurrencyStrength = 100f;

        private const float MinCurrencyStrength = 50f;
        private const float MaxCurrencyStrength = 200f;

        /// <summary>How much a 1 percentage point interest rate gap versus trade partners shifts the currency strength target.</summary>
        private const float InterestRateDifferentialScale = 5f;

        /// <summary>How quickly currency strength moves toward its target each turn (0-1).</summary>
        private const float CurrencyStrengthDamping = 0.15f;

        /// <summary>
        /// Applies each turn's interest rate policy decisions. Countries sharing a CurrencyZone
        /// (by reference) have their decisions summed into a single rate change for that zone, so
        /// e.g. Germany/France/Italy move together as one Eurozone rate.
        /// </summary>
        public static void ApplyInterestRateChanges(World world, Dictionary<CountryId, PolicyDecision> decisions)
        {
            var processedZones = new HashSet<CurrencyZone>();

            foreach (Country country in world.Countries)
            {
                CurrencyZone zone = country.CurrencyZone;
                if (!processedZones.Add(zone))
                {
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
