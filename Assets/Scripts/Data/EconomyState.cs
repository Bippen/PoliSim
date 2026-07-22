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
        /// Share of the population below the poverty line, as a percentage (e.g. 18 means 18%,
        /// matching how Unemployment/Inflation are stored, not a raw 0-1 fraction). Seeded per country
        /// from real OECD relative-poverty-rate data (see WorldFactory); mean-reverts each turn toward
        /// a baseline driven by Unemployment/Inflation gaps (both already-proven drivers elsewhere in
        /// this model - see MacroSystem.ApplyPovertyRate), then adjusted by any implemented
        /// Country.WelfarePrograms. Hard-clamped to [0, 100].
        /// </summary>
        public float PovertyRate;

        /// <summary>
        /// Share of the working-age population that is either employed or actively looking for work,
        /// as a percentage (e.g. 62 means 62%, matching how Unemployment/PovertyRate are stored).
        /// Seeded per country from real World Bank/OECD data (see WorldFactory); mean-reverts each
        /// turn toward Country.BaselineLaborForceParticipationRate, adjusted by the same
        /// unemployment gap already used elsewhere (a discouraged/encouraged-worker effect - see
        /// MacroSystem.ApplyLaborForceParticipationRate). A tracked stat only - nothing currently
        /// targets it directly with a policy lever.
        /// </summary>
        public float LaborForceParticipationRate;

        /// <summary>
        /// Government debt as a percentage of GDP (e.g. 124 means 124% of GDP) - matches how
        /// Unemployment/Inflation/TaxRate are stored in this codebase, not a raw 0-1 fraction.
        /// Derived, not stored, so it's always consistent with the current GDP and GovernmentDebt.
        /// </summary>
        public float DebtToGdpRatio => GDP > 0f ? GovernmentDebt / GDP * 100f : 0f;

        public EconomyState() { }

        public EconomyState(
            float gdp, float inflation, float unemployment, float approvalRating, float budget,
            float tradeBalance = 0f, float currencyStrength = 100f, float consumption = 0f, float investment = 0f,
            float potentialGdp = 0f, float inflationExpectations = 0f, float consumerConfidence = 1f, float businessConfidence = 1f,
            float governmentDebt = 0f, float povertyRate = 10f, float laborForceParticipationRate = 62f)
        {
            GDP = gdp;
            Inflation = inflation;
            Unemployment = unemployment;
            ApprovalRating = approvalRating;
            Budget = budget;
            TradeBalance = tradeBalance;
            CurrencyStrength = currencyStrength;
            Consumption = consumption;
            Investment = investment;
            PotentialGDP = potentialGdp > 0f ? potentialGdp : gdp;
            InflationExpectations = inflationExpectations > 0f ? inflationExpectations : inflation;
            ConsumerConfidence = consumerConfidence;
            BusinessConfidence = businessConfidence;
            GovernmentDebt = governmentDebt;
            PovertyRate = povertyRate;
            LaborForceParticipationRate = laborForceParticipationRate;
        }

        /// <summary>Returns a shallow copy so the simulation can compute a next state without mutating the current one.</summary>
        public EconomyState Clone()
        {
            return new EconomyState(
                GDP, Inflation, Unemployment, ApprovalRating, Budget,
                TradeBalance, CurrencyStrength, Consumption, Investment,
                PotentialGDP, InflationExpectations, ConsumerConfidence, BusinessConfidence,
                GovernmentDebt, PovertyRate, LaborForceParticipationRate);
        }

        /// <summary>A generic, fictional developed mixed economy - starting point for the player's country.</summary>
        public static EconomyState CreateDefault()
        {
            return new EconomyState(
                gdp: 20000f,
                inflation: 2.0f,
                unemployment: 5.0f,
                approvalRating: 50f,
                budget: 0f
            );
        }
    }
}
