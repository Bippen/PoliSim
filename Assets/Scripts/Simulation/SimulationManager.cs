using System.Collections.Generic;
using PoliSim.Data;
using UnityEngine;

namespace PoliSim.Simulation
{
    /// <summary>
    /// Snapshot of one country's fiscal computation for a single turn - the revenue/spending
    /// breakdown behind that turn's change to Budget/GovernmentDebt. Exposed for tools/UI (e.g.
    /// GameController's Trade &amp; Spending panel) that want to show where the money went without
    /// duplicating SimulationManager's formulas.
    /// </summary>
    public class FiscalTurnReport
    {
        public float Revenue;
        public float BaselineGovernmentSpending;
        public float DiscretionarySpending;
        public float UnemploymentBenefitCost;
        public float InterestOnDebt;
        public float TariffRevenue;
    }

    /// <summary>
    /// Drives the turn-based simulation loop for every country in the world: currency/trade
    /// effects resolve first, then each country's domestic policy - fiscal (tax/spending),
    /// the national accounts identity (GDP), Okun's Law (unemployment), and the Phillips Curve
    /// (inflation) - produces next turn's state. See MacroSystem for the macroeconomic theory
    /// itself; this class only orchestrates turn order and the fiscal/approval rules.
    ///
    /// Each rule is a small, separately named method with named constants rather than one large
    /// update function, so individual pieces of theory can be tuned or replaced independently.
    /// </summary>
    public class SimulationManager : MonoBehaviour
    {
        public int CurrentTurn { get; private set; }

        [SerializeField]
        private World _world;

        public World World => _world;

        // --- Fiscal accounting: automatic stabilizers + sovereign risk premium on debt ---

        /// <summary>Conventional "safe" debt-to-GDP benchmark (the EU Stability & Growth Pact reference value) above which lenders start charging extra.</summary>
        private const float RiskFreeDebtToGdpPercent = 60f;

        /// <summary>Extra interest-rate points charged per point of debt-to-GDP above the risk-free threshold.</summary>
        private const float DebtRiskPremiumRate = 0.02f;

        /// <summary>Caps the risk premium - otherwise it scales with Debt/GDP while also multiplying Debt, making InterestOnDebt quadratic in Debt and able to diverge to infinity within a handful of turns.</summary>
        private const float MaxDebtRiskPremium = 5f;

        /// <summary>Hard ceiling on debt-to-GDP - a sustained structural deficit with no policy response (e.g. this turn's GovernmentSpendingRate exceeding TaxRate) shouldn't be able to grow without bound.</summary>
        private const float MaxDebtToGdpPercent = 300f;

        private readonly Dictionary<CountryId, FiscalTurnReport> _lastFiscalReports = new Dictionary<CountryId, FiscalTurnReport>();

        /// <summary>The most recent turn's fiscal breakdown for a country, or null if no turn has been advanced yet.</summary>
        public FiscalTurnReport GetLastFiscalReport(CountryId countryId)
        {
            return _lastFiscalReports.TryGetValue(countryId, out FiscalTurnReport report) ? report : null;
        }

        /// <summary>Lets tools/tests (e.g. SimulationTestRunner) inject a specific World instead of the Awake-created default.</summary>
        public void SetWorld(World world)
        {
            _world = world;
        }

        private void Awake()
        {
            if (_world == null)
            {
                _world = WorldFactory.CreateDefault();
            }
        }

        /// <summary>
        /// Advances the simulation by one turn for every country. Countries with no entry in
        /// <paramref name="decisions"/> get a no-op policy decision for the turn.
        ///
        /// Order matters: interest rates and currency strength must resolve before trade (export
        /// competitiveness depends on this turn's currency strength), and trade must resolve before
        /// domestic policy (the national accounts identity needs this turn's TradeBalance as NX).
        /// </summary>
        public void AdvanceTurn(Dictionary<CountryId, PolicyDecision> decisions)
        {
            CurrencySystem.ApplyInterestRateChanges(_world, decisions);

            foreach (Country country in _world.Countries)
            {
                CurrencySystem.ApplyCurrencyStrength(country, _world);
            }

            var tariffRevenueByCountry = new Dictionary<CountryId, float>();
            foreach (Country country in _world.Countries)
            {
                tariffRevenueByCountry[country.Id] = TradeSystem.ApplyTradeEffects(country, _world);
            }

            foreach (Country country in _world.Countries)
            {
                PolicyDecision decision = decisions != null && decisions.TryGetValue(country.Id, out var d)
                    ? d
                    : PolicyDecision.None();

                ApplyDomesticPolicy(country, decision, tariffRevenueByCountry[country.Id]);
            }

            CurrentTurn++;
        }

        /// <summary>
        /// Applies one country's domestic feedback rules for the turn, in place: fiscal policy,
        /// the national accounts identity (GDP), Okun's Law (unemployment), the Phillips Curve
        /// (inflation), and approval. <paramref name="tariffRevenue"/> was already collected (and
        /// already added to Budget) by TradeSystem earlier this same turn - it's threaded through
        /// only to record it on this turn's FiscalTurnReport, not applied again here.
        /// </summary>
        private void ApplyDomesticPolicy(Country country, PolicyDecision decision, float tariffRevenue)
        {
            EconomyState state = country.State;
            float interestRate = country.CurrencyZone.InterestRate;
            float gdpBeforeThisTurn = state.GDP;

            ApplyTaxRateChange(state, decision);
            float baselineGovernmentSpending = GetBaselineGovernmentSpending(country);
            float governmentSpending = baselineGovernmentSpending + decision.GovernmentSpending;
            float unemploymentBenefitCost = GetUnemploymentBenefitCost(country);
            float interestOnDebt = GetInterestOnDebt(country);
            float revenue = ApplyRevenueAndSpending(state, governmentSpending, unemploymentBenefitCost, interestOnDebt);

            _lastFiscalReports[country.Id] = new FiscalTurnReport
            {
                Revenue = revenue,
                BaselineGovernmentSpending = baselineGovernmentSpending,
                DiscretionarySpending = decision.GovernmentSpending,
                UnemploymentBenefitCost = unemploymentBenefitCost,
                InterestOnDebt = interestOnDebt,
                TariffRevenue = tariffRevenue
            };

            MacroSystem.ApplyNationalAccounts(country, governmentSpending, interestRate);
            MacroSystem.ApplyPotentialGdpGrowth(country);

            float actualGrowthRate = (state.GDP - gdpBeforeThisTurn) / Mathf.Max(gdpBeforeThisTurn, 1f) * 100f;
            MacroSystem.ApplyOkunsLaw(country, actualGrowthRate);
            MacroSystem.ApplyPhillipsCurveInflation(country);
            MacroSystem.ApplyInflationExpectations(state);

            ApplyApprovalRating(state, decision);
        }

        /// <summary>Tax rate moves directly by the requested change, clamped to a sane range.</summary>
        private void ApplyTaxRateChange(EconomyState state, PolicyDecision decision)
        {
            state.TaxRate = Mathf.Clamp(state.TaxRate + decision.TaxRateChange, 0f, 100f);
        }

        /// <summary>
        /// This turn's baseline government consumption expenditure - the country's structural share
        /// of GDP, before the player's discretionary PolicyDecision.GovernmentSpending is added on
        /// top by the caller. Split out (rather than returning the combined total) so callers can
        /// report baseline and discretionary spending as separate line items.
        /// </summary>
        private float GetBaselineGovernmentSpending(Country country)
        {
            return country.State.GDP * (country.GovernmentSpendingRate / 100f);
        }

        /// <summary>
        /// Automatic stabilizer: unemployment benefit spending that scales with the unemployment
        /// rate with no player input, via the country's own BenefitRatePerUnemployed. Uses this
        /// turn's starting (prior-turn) Unemployment, matching how GetBaselineGovernmentSpending
        /// uses prior GDP - the value known at the start of the turn, before this turn's updates run.
        /// </summary>
        private float GetUnemploymentBenefitCost(Country country)
        {
            EconomyState state = country.State;
            return country.BenefitRatePerUnemployed * state.Unemployment / 100f * state.GDP;
        }

        /// <summary>Sovereign risk premium: lenders charge more above a conventional "safe" debt-to-GDP benchmark, capped so it can't make InterestOnDebt quadratic in Debt.</summary>
        private float GetDebtRiskPremium(EconomyState state)
        {
            float excessDebtToGdp = Mathf.Max(0f, state.DebtToGdpRatio - RiskFreeDebtToGdpPercent);
            return Mathf.Min(MaxDebtRiskPremium, DebtRiskPremiumRate * excessDebtToGdp);
        }

        /// <summary>New spending line: interest on the country's existing debt stock, at its policy rate plus the risk premium.</summary>
        private float GetInterestOnDebt(Country country)
        {
            EconomyState state = country.State;
            float effectiveRate = country.CurrencyZone.InterestRate + GetDebtRiskPremium(state);
            return state.GovernmentDebt * (effectiveRate / 100f);
        }

        /// <summary>
        /// Government revenue comes from taxing GDP; this turn's budget balance is revenue minus
        /// total spending (baseline+discretionary government spending, unemployment benefits, and
        /// interest on debt - benefits and interest are transfers, not purchases, so they're
        /// deliberately excluded from MacroSystem's national accounts G term). A deficit adds to
        /// GovernmentDebt, a surplus reduces it, hard-clamped to a sane debt-to-GDP range. Returns
        /// the revenue computed so the caller can record it on this turn's FiscalTurnReport.
        /// </summary>
        private float ApplyRevenueAndSpending(EconomyState state, float governmentSpending, float unemploymentBenefitCost, float interestOnDebt)
        {
            float revenue = state.GDP * (state.TaxRate / 100f);
            float totalSpending = governmentSpending + unemploymentBenefitCost + interestOnDebt;
            float budgetBalance = revenue - totalSpending;

            state.Budget += budgetBalance;
            float maxDebt = MaxDebtToGdpPercent / 100f * state.GDP;
            state.GovernmentDebt = Mathf.Clamp(state.GovernmentDebt - budgetBalance, 0f, maxDebt);

            return revenue;
        }

        /// <summary>
        /// Approval falls when taxes are raised or unemployment/inflation are high, and recovers
        /// slowly otherwise. This is the core "raising taxes costs you approval" feedback rule.
        /// </summary>
        private void ApplyApprovalRating(EconomyState state, PolicyDecision decision)
        {
            float taxHikePenalty = Mathf.Max(0f, decision.TaxRateChange) * 1.5f;
            float unemploymentPenalty = state.Unemployment * 0.5f;
            float inflationPenalty = state.Inflation * 0.5f;
            float passiveRecovery = 0.5f;

            float delta = passiveRecovery - taxHikePenalty - unemploymentPenalty - inflationPenalty;
            state.ApprovalRating = Mathf.Clamp(state.ApprovalRating + delta, 0f, 100f);
        }
    }
}
