using System;

namespace PoliSim.Data
{
    /// <summary>
    /// A monetary union with a single shared interest rate. Countries that share a currency
    /// (e.g. the Eurozone) reference the SAME CurrencyZone instance, so a rate change affects
    /// all of them at once; countries with their own currency get their own instance.
    /// </summary>
    [Serializable]
    public class CurrencyZone
    {
        public string Name;
        public float InterestRate;

        /// <summary>ROUND 4 BATCH 3 (C1): the zone's rate AT EPOCH - the fixed anchor the housing
        /// stats measure the live policy rate against (the BaselineIncomeTaxRate logic verbatim:
        /// InterestRate is player-mutable with no stored seed, so the anchor must be its own field).
        /// Captured in the ctor, the one place the seeded rate exists; NEVER mutated afterwards.
        ///
        /// ⚠ -1 is a deliberate SENTINEL, not a default rate: a pre-R4-3 save deserializes with
        /// this initializer (MissingMemberHandling.Ignore keeps it), and every reader must go
        /// through <see cref="HousingRateAnchor"/>, whose fallback makes the rate channel INERT on
        /// such saves (gap reads zero at the loaded rate) - which is exactly the behaviour those
        /// saves had before this field existed. Never read the raw field in a model.</summary>
        public float BaselineInterestRate = -1f;

        /// <summary>The anchor the housing models subtract from the live rate - the epoch rate on
        /// any world created since R4-3, the current rate (gap 0, channel inert) on a pre-R4-3
        /// save. Read-only: the fallback deliberately does NOT write the field, so loading an old
        /// save never fabricates an "epoch" the world was not created with.</summary>
        public float HousingRateAnchor => BaselineInterestRate >= 0f ? BaselineInterestRate : InterestRate;

        public CurrencyZone() { }

        public CurrencyZone(string name, float interestRate)
        {
            Name = name;
            InterestRate = interestRate;
            BaselineInterestRate = interestRate;
        }
    }
}
