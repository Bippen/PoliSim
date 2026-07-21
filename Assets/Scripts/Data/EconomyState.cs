using System;

namespace PoliSim.Data
{
    /// <summary>
    /// Snapshot of a country's economic and political indicators for a single turn.
    /// Plain data holder - no Unity dependencies, no simulation logic.
    /// </summary>
    [Serializable]
    public class EconomyState
    {
        /// <summary>Gross domestic product, in abstract currency units.</summary>
        public float GDP;

        /// <summary>Annualized inflation rate, as a percentage (e.g. 2.5 = 2.5%).</summary>
        public float Inflation;

        /// <summary>Unemployment rate, as a percentage (e.g. 5.0 = 5.0%).</summary>
        public float Unemployment;

        /// <summary>Public approval of the government, 0-100.</summary>
        public float ApprovalRating;

        /// <summary>Current government budget balance, in the same currency units as GDP. Negative values indicate deficit.</summary>
        public float Budget;

        /// <summary>Current tax rate, as a percentage of income/output taxed.</summary>
        public float TaxRate;

        /// <summary>Net exports (exports minus imports, after tariff effects) for the most recent turn.</summary>
        public float TradeBalance;

        /// <summary>
        /// Relative currency strength index, 100 = neutral. Only meaningful for countries with an
        /// independent currency (not sharing a CurrencyZone with other countries) - see
        /// CurrencySystem.ApplyCurrencyStrength. Shared-currency countries (e.g. Eurozone members)
        /// leave this at its default and it is not used.
        /// </summary>
        public float CurrencyStrength;

        /// <summary>Household consumption (the C in GDP = C + I + G + NX) for the most recent turn. See MacroSystem.ApplyNationalAccounts.</summary>
        public float Consumption;

        /// <summary>Business investment (the I in GDP = C + I + G + NX) for the most recent turn. See MacroSystem.ApplyNationalAccounts.</summary>
        public float Investment;

        /// <summary>
        /// Trend/potential output level - grows independently of actual GDP at the country's
        /// PotentialGrowthRate. Used for Okun's Law's growth gap and the Taylor Rule's output gap.
        /// </summary>
        public float PotentialGDP;

        /// <summary>Adaptively-formed expectation of inflation, used by the Phillips Curve. See MacroSystem.ApplyInflationExpectations.</summary>
        public float InflationExpectations;

        /// <summary>Consumer confidence index, 1.0 = neutral. Scales Consumption; nothing currently feeds this back.</summary>
        public float ConsumerConfidence;

        /// <summary>Business confidence index, 1.0 = neutral. Scales Investment; nothing currently feeds this back.</summary>
        public float BusinessConfidence;

        /// <summary>Outstanding government debt, in the same currency units as GDP. Grows by this turn's deficit, shrinks by any surplus - see SimulationManager.ApplyRevenueAndSpending.</summary>
        public float GovernmentDebt;

        /// <summary>
        /// Government debt as a percentage of GDP (e.g. 124 means 124% of GDP) - matches how
        /// Unemployment/Inflation/TaxRate are stored in this codebase, not a raw 0-1 fraction.
        /// Derived, not stored, so it's always consistent with the current GDP and GovernmentDebt.
        /// </summary>
        public float DebtToGdpRatio => GDP > 0f ? GovernmentDebt / GDP * 100f : 0f;

        public EconomyState() { }

        public EconomyState(
            float gdp, float inflation, float unemployment, float approvalRating, float budget, float taxRate,
            float tradeBalance = 0f, float currencyStrength = 100f, float consumption = 0f, float investment = 0f,
            float potentialGdp = 0f, float inflationExpectations = 0f, float consumerConfidence = 1f, float businessConfidence = 1f,
            float governmentDebt = 0f)
        {
            GDP = gdp;
            Inflation = inflation;
            Unemployment = unemployment;
            ApprovalRating = approvalRating;
            Budget = budget;
            TaxRate = taxRate;
            TradeBalance = tradeBalance;
            CurrencyStrength = currencyStrength;
            Consumption = consumption;
            Investment = investment;
            PotentialGDP = potentialGdp > 0f ? potentialGdp : gdp;
            InflationExpectations = inflationExpectations > 0f ? inflationExpectations : inflation;
            ConsumerConfidence = consumerConfidence;
            BusinessConfidence = businessConfidence;
            GovernmentDebt = governmentDebt;
        }

        /// <summary>Returns a shallow copy so the simulation can compute a next state without mutating the current one.</summary>
        public EconomyState Clone()
        {
            return new EconomyState(
                GDP, Inflation, Unemployment, ApprovalRating, Budget, TaxRate,
                TradeBalance, CurrencyStrength, Consumption, Investment,
                PotentialGDP, InflationExpectations, ConsumerConfidence, BusinessConfidence,
                GovernmentDebt);
        }

        /// <summary>A generic, fictional developed mixed economy - starting point for the player's country.</summary>
        public static EconomyState CreateDefault()
        {
            return new EconomyState(
                gdp: 20000f,
                inflation: 2.0f,
                unemployment: 5.0f,
                approvalRating: 50f,
                budget: 0f,
                taxRate: 25f
            );
        }
    }
}
