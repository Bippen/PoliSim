using System.Collections.Generic;
using PoliSim.Data;
using UnityEngine;

namespace PoliSim.Simulation
{
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

            foreach (Country country in _world.Countries)
            {
                TradeSystem.ApplyTradeEffects(country, _world);
            }

            foreach (Country country in _world.Countries)
            {
                PolicyDecision decision = decisions != null && decisions.TryGetValue(country.Id, out var d)
                    ? d
                    : PolicyDecision.None();

                ApplyDomesticPolicy(country, decision);
            }

            CurrentTurn++;
        }

        /// <summary>
        /// Applies one country's domestic feedback rules for the turn, in place: fiscal policy,
        /// the national accounts identity (GDP), Okun's Law (unemployment), the Phillips Curve
        /// (inflation), and approval.
        /// </summary>
        private void ApplyDomesticPolicy(Country country, PolicyDecision decision)
        {
            EconomyState state = country.State;
            float interestRate = country.CurrencyZone.InterestRate;
            float gdpBeforeThisTurn = state.GDP;

            ApplyTaxRateChange(state, decision);
            float governmentSpending = GetGovernmentSpending(country, decision);
            ApplyRevenueAndSpending(state, governmentSpending);

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
        /// This turn's total government consumption expenditure - the country's structural baseline
        /// share of GDP, plus the player's discretionary PolicyDecision.GovernmentSpending on top.
        /// This is the G used both for the budget and for MacroSystem's national accounts identity.
        /// </summary>
        private float GetGovernmentSpending(Country country, PolicyDecision decision)
        {
            return country.State.GDP * (country.GovernmentSpendingRate / 100f) + decision.GovernmentSpending;
        }

        /// <summary>Government revenue comes from taxing GDP; budget balance is revenue minus total government spending.</summary>
        private void ApplyRevenueAndSpending(EconomyState state, float governmentSpending)
        {
            float revenue = state.GDP * (state.TaxRate / 100f);
            state.Budget += revenue - governmentSpending;
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
