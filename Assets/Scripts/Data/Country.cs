using System;
using System.Collections.Generic;

namespace PoliSim.Data
{
    /// <summary>
    /// A playable country: identity, current economic/political state, the currency zone it
    /// belongs to, and its bilateral trade relationships.
    /// </summary>
    [Serializable]
    public class Country
    {
        public CountryId Id;
        public string Name;
        public EconomyState State;
        public CurrencyZone CurrencyZone;
        public List<TradePartner> TradePartners = new List<TradePartner>();

        /// <summary>
        /// This country's own tariff policy toward imports, used only when it is not a member of
        /// any trade bloc (bloc members instead apply their bloc's common external/internal rates).
        /// </summary>
        public float BaseTariffRate;

        /// <summary>
        /// NAIRU - the non-accelerating-inflation rate of unemployment, a structural per-country
        /// constant. The Phillips Curve compares actual unemployment against this. See
        /// MacroSystem.ApplyPhillipsCurveInflation.
        /// </summary>
        public float NaturalUnemploymentRate;

        /// <summary>
        /// Trend/potential GDP growth rate, in percent per turn. A structural per-country constant
        /// used by Okun's Law (actual vs. potential growth) and to grow PotentialGDP each turn.
        /// </summary>
        public float PotentialGrowthRate;

        /// <summary>
        /// Baseline government consumption expenditure, as a percentage of GDP - the structural
        /// share of the G term in GDP = C + I + G + NX. PolicyDecision.GovernmentSpending is added
        /// on top of this as a discretionary delta, not used in place of it.
        /// </summary>
        public float GovernmentSpendingRate;

        /// <summary>
        /// Automatic stabilizer: unemployment benefit cost as a percentage of GDP per percentage
        /// point of unemployment (e.g. 0.20 means 1 point of unemployment costs 0.20% of GDP in
        /// benefits) - a structural per-country constant reflecting how generous the welfare state
        /// is. See SimulationManager.GetUnemploymentBenefitCost.
        /// </summary>
        public float BenefitRatePerUnemployed;

        public Country() { }

        public Country(
            CountryId id, string name, EconomyState state, CurrencyZone currencyZone, float baseTariffRate = 0f,
            float naturalUnemploymentRate = 4f, float potentialGrowthRate = 2f, float governmentSpendingRate = 20f,
            float benefitRatePerUnemployed = 0.15f)
        {
            Id = id;
            Name = name;
            State = state;
            CurrencyZone = currencyZone;
            BaseTariffRate = baseTariffRate;
            NaturalUnemploymentRate = naturalUnemploymentRate;
            PotentialGrowthRate = potentialGrowthRate;
            GovernmentSpendingRate = governmentSpendingRate;
            BenefitRatePerUnemployed = benefitRatePerUnemployed;
        }
    }
}
