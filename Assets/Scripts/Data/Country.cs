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

        /// <summary>This country's fiscal portfolio - which taxes are implemented and at what rate. See TaxLine and SimulationManager.GetTotalTaxRevenue.</summary>
        public List<TaxLine> TaxLines = new List<TaxLine>();

        /// <summary>
        /// This country's welfare portfolio - which anti-poverty programs are implemented and at what
        /// GenerosityLevel, mirroring TaxLines' implement/adjust/remove pattern exactly. None
        /// implemented by default for any country (see WorldFactory) - see WelfareProgram and
        /// SimulationManager.GetTotalWelfareCost/ApplyWelfareGenerosityChanges.
        /// </summary>
        public List<WelfareProgram> WelfarePrograms = new List<WelfareProgram>();

        /// <summary>
        /// This country's detailed spending portfolio (Phase 1: USA only - see CLAUDE.md's "Detailed
        /// Spending Portfolio"). Empty for a country means it still uses the legacy
        /// GovernmentSpendingRate + PolicyDecision's four category-delta fields mechanism unchanged -
        /// see SimulationManager.ApplyDomesticPolicy's hasDetailedSpending branch.
        /// </summary>
        public List<SpendingLine> SpendingLines = new List<SpendingLine>();

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
        /// share of the G term in GDP = C + I + G + NX. PolicyDecision.TotalDiscretionarySpending
        /// (the sum of the four spending categories) is added on top of this as a discretionary
        /// delta, not used in place of it.
        /// </summary>
        public float GovernmentSpendingRate;

        /// <summary>
        /// Automatic stabilizer: unemployment benefit cost as a percentage of GDP per percentage
        /// point of unemployment (e.g. 0.20 means 1 point of unemployment costs 0.20% of GDP in
        /// benefits) - a structural per-country constant reflecting how generous the welfare state
        /// is. See SimulationManager.GetUnemploymentBenefitCost.
        /// </summary>
        public float BenefitRatePerUnemployed;

        /// <summary>
        /// How much of the theoretical tax base is actually collected (0.0-1.0), reflecting
        /// enforcement quality, the size of the informal economy, and evasion - a structural
        /// per-country constant. Applied as a multiplier in SimulationManager.ApplyRevenueAndSpending
        /// (ActualRevenue = GetTotalTaxRevenue() * CollectionEfficiency), not inside
        /// GetTotalTaxRevenue itself, so that method still returns the theoretical figure. Calibrated
        /// per country in WorldFactory so the default tax portfolio's actual revenue-to-GDP lands
        /// close to that country's real-world tax-to-GDP ratio - see WorldFactory's doc comment for
        /// the derivation.
        /// </summary>
        public float CollectionEfficiency = 1f;

        /// <summary>
        /// Overrides SimulationManager.GetInterestOnDebt's base rate (in place of CurrencyZone.
        /// InterestRate) with this country's real blended average interest rate on existing debt,
        /// for a country where today's policy rate is a poor proxy for the average rate across its
        /// entire (long-duration, accumulated-over-many-years) debt stock - see
        /// "Reserve-Currency Debt Interest Treatment" in CLAUDE.md. -1 (the default) means unset -
        /// use CurrencyZone.InterestRate, unchanged behavior.
        /// </summary>
        public float BaseDebtInterestRateOverride = -1f;

        /// <summary>
        /// Scales GetDebtRiskPremium's output before it's added to the effective debt interest rate
        /// (1 = full market exposure, the default - unchanged behavior for every country except a
        /// reserve-currency issuer). A reserve-currency issuer (the USA) doesn't face the same market
        /// risk premium as other sovereigns at an equivalent debt-to-GDP ratio - who holds the debt
        /// and in what currency matters as much as the ratio itself - so its sensitivity is set near
        /// (not exactly) zero. See "Reserve-Currency Debt Interest Treatment" in CLAUDE.md.
        /// </summary>
        public float RiskPremiumSensitivity = 1f;

        /// <summary>
        /// This country's own fiscal-comfort anchor for SimulationManager.GetFiscalReactionMultiplier
        /// - the debt-to-GDP level at which the automatic fiscal reaction (see that method) is
        /// neutral. Above it, the reaction modestly tightens (raises effective revenue); below it, it
        /// loosens. A structural per-country constant, seeded in WorldFactory to match each country's
        /// own real-approximate starting debt-to-GDP ratio - reusing that already-researched figure,
        /// not a separately-tuned new one. See "Fiscal Reaction Function" in CLAUDE.md.
        /// </summary>
        public float ComfortableDebtToGdpPercent = 60f;

        /// <summary>
        /// This country's structural "steady-state" poverty rate - the PovertyRate MacroSystem.
        /// ApplyPovertyRate's baseline computes toward when Unemployment sits at NaturalUnemploymentRate
        /// and Inflation sits at TaylorRule.InflationTarget (i.e. the gaps that otherwise move the
        /// baseline are both zero). Seeded per country from real OECD relative-poverty-rate data (see
        /// WorldFactory) - the same figure EconomyState.PovertyRate is seeded to, so a new game opens
        /// with PovertyRate already at (or very near) its own baseline rather than an artificial
        /// turn-1 jump, the same "avoid a one-time shock" lesson "Turn-1 GDP Consistency" established.
        /// </summary>
        public float BaselinePovertyRate = 10f;

        /// <summary>
        /// This country's independent central bank chair, or null for a country that instead uses
        /// PolicyDecision.InterestRateChange (the player-controlled slider - Sweden, Poland, and the
        /// Eurozone trio; see CurrencySystem.ApplyInterestRateChanges). Non-null (USA only, for now)
        /// means CurrencySystem bypasses PolicyDecision.InterestRateChange entirely and instead sets
        /// InterestRate to TaylorRule.GetSuggestedInterestRate plus this chair's RateBias each turn -
        /// see FederalReserveSystem and CLAUDE.md's "Federal Reserve" section.
        /// </summary>
        public FedChair CurrentFedChair;

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
