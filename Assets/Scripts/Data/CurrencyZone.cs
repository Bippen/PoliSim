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


        // ---- P4-C3, the third category (2026-09-05, ruling (a)): THE MONETARY REGIME IS THE ZONE'S ----
        // The four Taylor-rule constants became per-currency-zone STATE so that monetary-regime laws can reach
        // them. They live on the zone, not the country, because the rule they feed is the zone's (the ECB blend
        // averages its members' suggestions; one target, one mandate). A euro member's parliament does not reach
        // them - treaty competence (TFEU Art. 127 and 130), recorded as the reason they stay unreachable for
        // Germany, France and Italy - while Sweden, Poland and the USA reach their own bank's. The seeded value
        // is the BASE (the StructuralParameters convention); SimulationManager.RecomputeStructuralParametersFromEnactedLaws
        // sets value = base + the enacted set's deltas, clamped by the table's bounds.

        /// <summary>The zone's announced inflation target, percent (TaylorRule.DefaultInflationTargetFor seeds it: 2 % for the ECB, the Fed and the Riksbank; 2.5 % for the NBP).</summary>
        public float InflationTarget;
        /// <summary>The bank's assumed neutral real rate r*, percent (seeded at Taylor 1993's 2 %, [AUTHORED-DRAFT] as the constant it replaced was).</summary>
        public float NeutralRealRate;
        /// <summary>Weight on the inflation gap (Taylor 1993: 0.5).</summary>
        public float InflationGapWeight;
        /// <summary>Points of suggested rate per point of unemployment below the NAIRU (the textbook 1.0 the constant carried).</summary>
        public float UnemploymentGapWeight;
        public float InflationTargetBase;
        public float NeutralRealRateBase;
        public float InflationGapWeightBase;
        public float UnemploymentGapWeightBase;
        /// <summary>False on a zone deserialized from a save written before the four fields existed (MissingMemberHandling.Ignore
        /// keeps the default); TaylorRule.Zone seeds such a zone once from the defaults before reading it.</summary>
        public bool MonetaryParametersSeeded;

        /// <summary>Seeds the four monetary parameters and their bases in one move - the constructor's defaults, or an older save's repair.</summary>
        public void SeedMonetaryParameters(float inflationTarget, float neutralRealRate, float inflationGapWeight, float unemploymentGapWeight)
        {
            InflationTarget = InflationTargetBase = inflationTarget;
            NeutralRealRate = NeutralRealRateBase = neutralRealRate;
            InflationGapWeight = InflationGapWeightBase = inflationGapWeight;
            UnemploymentGapWeight = UnemploymentGapWeightBase = unemploymentGapWeight;
            MonetaryParametersSeeded = true;
        }


        public CurrencyZone() { }

        public CurrencyZone(string name, float interestRate)
        {
            Name = name;
            InterestRate = interestRate;
            BaselineInterestRate = interestRate;
            SeedMonetaryParameters(2f, 2f, 0.5f, 1.0f);   // P4-C3: the Taylor constants as seeded state; WorldFactory re-seeds the NBP's 2.5 % target
        }
    }
}
