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
        public float MandatorySpending;
        public float UnemploymentBenefitCost;
        public float InterestOnDebt;
        public float TariffRevenue;
    }

    /// <summary>
    /// Estimated single-turn effect of a not-yet-committed PolicyDecision, computed by
    /// SimulationManager.PreviewTurn against a throwaway clone - see that method for what it does
    /// and doesn't reproduce faithfully. Purely a display-layer estimate; nothing here is ever
    /// written back into real EconomyState/Country/World.
    /// </summary>
    public class PolicyPreview
    {
        public float GdpGrowthPercent;
        public float UnemploymentChange;
        public float InflationChange;
        public float ApprovalChange;
        public float NetBudgetImpact;
    }

    /// <summary>
    /// Drives the turn-based simulation loop for every country in the world: currency/trade
    /// effects resolve first (including each country's own tariff-policy change), then each
    /// country's domestic policy - fiscal (tax/spending/debt), the national accounts identity
    /// (GDP), Okun's Law (unemployment), the Phillips Curve (inflation), approval, and a random
    /// event roll - produces next turn's state. See MacroSystem for the macroeconomic theory and
    /// approval formula themselves, ElectionSystem/EventSystem for the rest of the political layer;
    /// this class only orchestrates turn order and the fiscal-accounting rules.
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

        /// <summary>Sane bounds for a country's own tariff policy (see PolicyDecision.TariffRateChange).</summary>
        private const float MinBaseTariffRate = 0f;
        private const float MaxBaseTariffRate = 50f;

        private readonly Dictionary<CountryId, FiscalTurnReport> _lastFiscalReports = new Dictionary<CountryId, FiscalTurnReport>();
        private readonly Dictionary<CountryId, EconomicEvent> _lastEventsByCountry = new Dictionary<CountryId, EconomicEvent>();

        /// <summary>The most recent turn's fiscal breakdown for a country, or null if no turn has been advanced yet.</summary>
        public FiscalTurnReport GetLastFiscalReport(CountryId countryId)
        {
            return _lastFiscalReports.TryGetValue(countryId, out FiscalTurnReport report) ? report : null;
        }

        /// <summary>The event that fired for a country this turn, or null if none did (most turns).</summary>
        public EconomicEvent GetLastEvent(CountryId countryId)
        {
            return _lastEventsByCountry.TryGetValue(countryId, out EconomicEvent economicEvent) ? economicEvent : null;
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

            foreach (Country country in _world.Countries)
            {
                PolicyDecision tariffDecision = decisions != null && decisions.TryGetValue(country.Id, out var td)
                    ? td
                    : PolicyDecision.None();

                ApplyTariffRateChange(country, tariffDecision);
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

            float totalTaxHike = ApplyTaxRateChanges(country, decision);
            DetailedSpendingResult spendingResult = ResolveSpendingForTurn(country, decision);
            MacroSystem.ApplyCategorySpendingEffects(country, spendingResult.EffectiveDecision);

            float unemploymentBenefitCost = GetUnemploymentBenefitCost(country);
            float interestOnDebt = GetInterestOnDebt(country);
            float revenue = ApplyRevenueAndSpending(country, spendingResult.GovernmentSpending, spendingResult.MandatorySpending, unemploymentBenefitCost, interestOnDebt);

            _lastFiscalReports[country.Id] = new FiscalTurnReport
            {
                Revenue = revenue,
                BaselineGovernmentSpending = spendingResult.BaselineGovernmentSpending,
                DiscretionarySpending = spendingResult.DiscretionarySpendingChangeThisTurn,
                MandatorySpending = spendingResult.MandatorySpending,
                UnemploymentBenefitCost = unemploymentBenefitCost,
                InterestOnDebt = interestOnDebt,
                TariffRevenue = tariffRevenue
            };

            MacroSystem.ApplyNationalAccounts(country, spendingResult.GovernmentSpending, interestRate);
            MacroSystem.ApplyPotentialGdpGrowth(country);

            float actualGrowthRate = (state.GDP - gdpBeforeThisTurn) / Mathf.Max(gdpBeforeThisTurn, 1f) * 100f;
            MacroSystem.ApplyOkunsLaw(country, actualGrowthRate);
            MacroSystem.ApplyPhillipsCurveInflation(country);
            MacroSystem.ApplyInflationExpectations(state);

            MacroSystem.ApplyApprovalRating(country, spendingResult.EffectiveDecision, actualGrowthRate, totalTaxHike);

            EconomicEvent economicEvent = EventSystem.TryRollEvent();
            _lastEventsByCountry[country.Id] = economicEvent;
            EventSystem.ApplyEvent(country, economicEvent);
        }

        /// <summary>
        /// Estimates what ApplyDomesticPolicy would do to <paramref name="countryId"/> this turn
        /// under <paramref name="decision"/>, WITHOUT mutating the real World/Country/EconomyState
        /// or recording a FiscalTurnReport - runs the same formulas (this class's own private fiscal
        /// helpers plus MacroSystem's national accounts identity, Okun's Law, Phillips Curve, and
        /// ApplyApprovalRating) against a throwaway clone of the country's EconomyState, so the
        /// result stays grounded in the actual model rather than a separate hand-rolled estimate.
        ///
        /// Two deliberate simplifications, both because they'd otherwise require mutating shared
        /// state (the country's CurrencyZone can be shared with other countries, e.g. the Eurozone)
        /// just to compute a display-only estimate: the previewed interest rate is threaded through
        /// as a local value into ApplyNationalAccounts rather than actually changing the
        /// CurrencyZone, so GetInterestOnDebt's rate still reflects the current (not previewed) rate;
        /// and this turn's CurrencyStrength is used as-is rather than re-deriving its (heavily
        /// damped, slow-moving) drift for the preview's trade-balance estimate.
        ///
        /// Never rolls an EventSystem event and never advances CurrentTurn - a preview should be
        /// deterministic and side-effect-free, not spend part of the "will an event fire" randomness
        /// budget on a turn the player might not even commit to.
        /// </summary>
        public PolicyPreview PreviewTurn(CountryId countryId, PolicyDecision decision)
        {
            Country previewCountry = ClonePreviewCountry(_world.GetCountry(countryId));
            EconomyState state = previewCountry.State;

            float gdpBeforeThisTurn = state.GDP;
            float unemploymentBefore = state.Unemployment;
            float inflationBefore = state.Inflation;
            float approvalBefore = state.ApprovalRating;
            float budgetBefore = state.Budget;

            ApplyTariffRateChange(previewCountry, decision);
            TradeSystem.ApplyTradeEffects(previewCountry, _world);

            float totalTaxHike = ApplyTaxRateChanges(previewCountry, decision);
            DetailedSpendingResult spendingResult = ResolveSpendingForTurn(previewCountry, decision);
            MacroSystem.ApplyCategorySpendingEffects(previewCountry, spendingResult.EffectiveDecision);

            float unemploymentBenefitCost = GetUnemploymentBenefitCost(previewCountry);
            float interestOnDebt = GetInterestOnDebt(previewCountry);
            ApplyRevenueAndSpending(previewCountry, spendingResult.GovernmentSpending, spendingResult.MandatorySpending, unemploymentBenefitCost, interestOnDebt);

            float previewedInterestRate = Mathf.Clamp(
                previewCountry.CurrencyZone.InterestRate + decision.InterestRateChange,
                CurrencySystem.MinInterestRate, CurrencySystem.MaxInterestRate);
            MacroSystem.ApplyNationalAccounts(previewCountry, spendingResult.GovernmentSpending, previewedInterestRate);
            MacroSystem.ApplyPotentialGdpGrowth(previewCountry);

            float actualGrowthRate = (state.GDP - gdpBeforeThisTurn) / Mathf.Max(gdpBeforeThisTurn, 1f) * 100f;
            MacroSystem.ApplyOkunsLaw(previewCountry, actualGrowthRate);
            MacroSystem.ApplyPhillipsCurveInflation(previewCountry);
            MacroSystem.ApplyInflationExpectations(state);

            MacroSystem.ApplyApprovalRating(previewCountry, spendingResult.EffectiveDecision, actualGrowthRate, totalTaxHike);

            return new PolicyPreview
            {
                GdpGrowthPercent = actualGrowthRate,
                UnemploymentChange = state.Unemployment - unemploymentBefore,
                InflationChange = state.Inflation - inflationBefore,
                ApprovalChange = state.ApprovalRating - approvalBefore,
                NetBudgetImpact = state.Budget - budgetBefore
            };
        }

        /// <summary>
        /// A throwaway Country for PreviewTurn: its own EconomyState clone (so GDP/Inflation/etc.
        /// mutations never touch the real one), its own copy of the structural fields that
        /// MacroSystem.ApplyCategorySpendingEffects/ApplyTariffRateChange mutate (PotentialGrowthRate,
        /// BaseTariffRate), and its own deep-cloned TaxLines (ApplyTaxRateChanges mutates TaxLine.Rate,
        /// so these can't be shared references the way TradePartners is) - but the SAME CurrencyZone
        /// reference (read-only here - see PreviewTurn's remarks on why its InterestRate is never
        /// written) and the SAME TradePartners list (TradeSystem only ever reads it, never mutates it).
        /// CollectionEfficiency, BaseDebtInterestRateOverride, and RiskPremiumSensitivity are all
        /// copied explicitly since none is a constructor parameter (each defaults the same as on a
        /// real Country) - without this the preview would overstate revenue for every country whose
        /// real CollectionEfficiency is below 1, and overstate InterestOnDebt for a reserve-currency
        /// issuer (the USA) whose real risk-premium sensitivity is near zero. SpendingLines is
        /// deep-cloned for the same reason TaxLines is - ApplySpendingLineChanges mutates
        /// SpendingLine.Amount, so these can't be shared references either.
        /// </summary>
        private static Country ClonePreviewCountry(Country country)
        {
            return new Country(
                country.Id, country.Name, country.State.Clone(), country.CurrencyZone, country.BaseTariffRate,
                country.NaturalUnemploymentRate, country.PotentialGrowthRate, country.GovernmentSpendingRate,
                country.BenefitRatePerUnemployed)
            {
                TradePartners = country.TradePartners,
                TaxLines = ClonePreviewTaxLines(country.TaxLines),
                SpendingLines = ClonePreviewSpendingLines(country.SpendingLines),
                CollectionEfficiency = country.CollectionEfficiency,
                BaseDebtInterestRateOverride = country.BaseDebtInterestRateOverride,
                RiskPremiumSensitivity = country.RiskPremiumSensitivity
            };
        }

        private static List<TaxLine> ClonePreviewTaxLines(List<TaxLine> taxLines)
        {
            var clones = new List<TaxLine>(taxLines.Count);
            foreach (TaxLine taxLine in taxLines)
            {
                clones.Add(taxLine.Clone());
            }
            return clones;
        }

        private static List<SpendingLine> ClonePreviewSpendingLines(List<SpendingLine> spendingLines)
        {
            var clones = new List<SpendingLine>(spendingLines.Count);
            foreach (SpendingLine spendingLine in spendingLines)
            {
                clones.Add(spendingLine.Clone());
            }
            return clones;
        }

        /// <summary>Direct tariff-policy control: the country's own BaseTariffRate moves by the requested change, clamped to a sane range.</summary>
        private void ApplyTariffRateChange(Country country, PolicyDecision decision)
        {
            country.BaseTariffRate = Mathf.Clamp(country.BaseTariffRate + decision.TariffRateChange, MinBaseTariffRate, MaxBaseTariffRate);
        }

        /// <summary>
        /// Sets every currently-implemented TaxLine's Rate directly to this turn's requested
        /// PolicyDecision.TaxRateOverrides value (clamped to that TaxLine's own TaxTypeRateRanges), a
        /// no-op for any TaxType with no entry or that isn't implemented - implementing/removing a
        /// tax is a separate, immediate action on TaxLine.IsImplemented, not something this method
        /// does. Returns the sum of every positive rate increase actually applied (clamped target
        /// minus the prior rate, where positive), computed here - before TaxLine.Rate is overwritten -
        /// since PolicyDecision only carries the absolute target, not a delta; the caller threads this
        /// into MacroSystem.ApplyApprovalRating's tax-hike penalty.
        /// </summary>
        private float ApplyTaxRateChanges(Country country, PolicyDecision decision)
        {
            float totalTaxHike = 0f;
            foreach (TaxLine taxLine in country.TaxLines)
            {
                if (!taxLine.IsImplemented)
                {
                    continue;
                }

                if (!decision.TaxRateOverrides.TryGetValue(taxLine.Type, out float requestedRate))
                {
                    continue;
                }

                float clampedRate = Mathf.Clamp(requestedRate, taxLine.MinRate, taxLine.MaxRate);
                float hike = clampedRate - taxLine.Rate;
                if (hike > 0f)
                {
                    totalTaxHike += hike;
                }

                taxLine.Rate = clampedRate;
            }

            return totalTaxHike;
        }

        /// <summary>
        /// This turn's total THEORETICAL tax revenue (before CollectionEfficiency): the sum, over
        /// every implemented TaxLine, of GDP * (Rate / 100) * BaseShareOfGdp. Tariffs is explicitly
        /// skipped even though it's never constructed as a TaxLine (see TaxType's doc comment) -
        /// defensive, so a future TaxLine accidentally created for it could never double-count revenue
        /// TradeSystem already collects. See ApplyRevenueAndSpending for where CollectionEfficiency is
        /// applied to get the actual collected revenue.
        /// </summary>
        private float GetTotalTaxRevenue(Country country)
        {
            float revenue = 0f;
            float gdp = country.State.GDP;
            foreach (TaxLine taxLine in country.TaxLines)
            {
                if (!taxLine.IsImplemented || taxLine.Type == TaxType.Tariffs)
                {
                    continue;
                }

                revenue += gdp * (taxLine.Rate / 100f) * taxLine.BaseShareOfGdp;
            }

            return revenue;
        }

        /// <summary>
        /// This turn's baseline government consumption expenditure - the country's structural share
        /// of GDP, before the player's discretionary category spending is added on top by the
        /// caller. Split out (rather than returning the combined total) so callers can report
        /// baseline and discretionary spending as separate line items. Only used for a country
        /// WITHOUT a detailed SpendingLines portfolio - see ResolveSpendingForTurn.
        /// </summary>
        private float GetBaselineGovernmentSpending(Country country)
        {
            return country.State.GDP * (country.GovernmentSpendingRate / 100f);
        }

        /// <summary>Bundles what ResolveSpendingForTurn resolves this turn's spending down to, for either mechanism (detailed SpendingLines or the legacy baseline+category-delta one).</summary>
        private class DetailedSpendingResult
        {
            public float BaselineGovernmentSpending;
            public float GovernmentSpending;
            public float DiscretionarySpendingChangeThisTurn;
            public float MandatorySpending;
            public PolicyDecision EffectiveDecision;
        }

        /// <summary>
        /// For a country with a detailed SpendingLines portfolio (Phase 1: USA only): applies this
        /// turn's SpendingLineChanges to the Discretionary lines (ApplySpendingLineChanges), then G
        /// is the sum of Discretionary line Amounts AFTER that change (Mandatory lines are transfers,
        /// excluded from G - same reasoning as UnemploymentBenefitCost/InterestOnDebt) and
        /// MandatorySpending is reported separately for ApplyRevenueAndSpending to add to total
        /// budget outflow. BaselineGovernmentSpending/DiscretionarySpendingChangeThisTurn are split
        /// as before-this-turn's-total / actual-net-change-observed (not the raw requested delta sum,
        /// since ApplySpendingLineChanges' floor-at-0 can clip an individual line's requested cut) so
        /// their SUM always equals GovernmentSpending, matching the legacy mechanic's semantics below
        /// exactly - callers (e.g. GameController's "net" display) can keep subtracting both without
        /// double-counting this turn's change. EffectiveDecision maps this turn's per-category deltas
        /// onto the four legacy category-spending fields (see BuildEffectiveDecisionForDetailedSpending)
        /// so MacroSystem's existing category-effect/approval formulas keep working unmodified.
        ///
        /// For a country without one, this is exactly the old baselineGovernmentSpending +
        /// decision.TotalDiscretionarySpending mechanic, byte-for-byte unchanged.
        /// </summary>
        private DetailedSpendingResult ResolveSpendingForTurn(Country country, PolicyDecision decision)
        {
            if (country.SpendingLines.Count > 0)
            {
                float discretionaryTotalBefore = GetSpendingLineTotal(country, mandatory: false);
                ApplySpendingLineChanges(country, decision);
                float discretionaryTotalAfter = GetSpendingLineTotal(country, mandatory: false);
                float mandatoryTotal = GetSpendingLineTotal(country, mandatory: true);

                return new DetailedSpendingResult
                {
                    BaselineGovernmentSpending = discretionaryTotalBefore,
                    GovernmentSpending = discretionaryTotalAfter,
                    DiscretionarySpendingChangeThisTurn = discretionaryTotalAfter - discretionaryTotalBefore,
                    MandatorySpending = mandatoryTotal,
                    EffectiveDecision = BuildEffectiveDecisionForDetailedSpending(decision)
                };
            }

            float baselineGovernmentSpending = GetBaselineGovernmentSpending(country);
            return new DetailedSpendingResult
            {
                BaselineGovernmentSpending = baselineGovernmentSpending,
                GovernmentSpending = baselineGovernmentSpending + decision.TotalDiscretionarySpending,
                DiscretionarySpendingChangeThisTurn = decision.TotalDiscretionarySpending,
                MandatorySpending = 0f,
                EffectiveDecision = decision
            };
        }

        /// <summary>Applies this turn's requested dollar CHANGE to every Discretionary SpendingLine (Mandatory lines are ignored - not player-adjustable in Phase 1), floored at 0 so a line can be cut to zero but not negative.</summary>
        private void ApplySpendingLineChanges(Country country, PolicyDecision decision)
        {
            foreach (SpendingLine line in country.SpendingLines)
            {
                if (line.IsMandatory)
                {
                    continue;
                }

                if (!decision.SpendingLineChanges.TryGetValue(line.Category, out float delta) || delta == 0f)
                {
                    continue;
                }

                line.Amount = Mathf.Max(0f, line.Amount + delta);
            }
        }

        private static float GetSpendingLineTotal(Country country, bool mandatory)
        {
            float total = 0f;
            foreach (SpendingLine line in country.SpendingLines)
            {
                if (line.IsMandatory == mandatory)
                {
                    total += line.Amount;
                }
            }
            return total;
        }

        private static float GetSpendingLineChangeDelta(PolicyDecision decision, SpendingCategory category)
        {
            return decision.SpendingLineChanges.TryGetValue(category, out float value) ? value : 0f;
        }

        /// <summary>
        /// Maps this turn's per-category deltas onto the four legacy category-spending-effect fields
        /// (Infrastructure -&gt; Transportation, Healthcare -&gt; HHSDiscretionary + Medicaid,
        /// Education -&gt; Education, Defense -&gt; Defense) so MacroSystem.ApplyCategorySpendingEffects/
        /// ApplyApprovalRating (unmodified) still read a meaningful this-turn delta for each of their
        /// four existing effects, without MacroSystem needing to know about SpendingCategory at all.
        /// Every other new category deliberately gets zero effect in this pass (Phase 1) - see
        /// CLAUDE.md's "Detailed Spending Portfolio" for the planned Phase 2.
        /// </summary>
        private static PolicyDecision BuildEffectiveDecisionForDetailedSpending(PolicyDecision decision)
        {
            return new PolicyDecision
            {
                TaxRateOverrides = decision.TaxRateOverrides,
                InterestRateChange = decision.InterestRateChange,
                TariffRateChange = decision.TariffRateChange,
                InfrastructureSpendingChange = GetSpendingLineChangeDelta(decision, SpendingCategory.Transportation),
                HealthcareSpendingChange = GetSpendingLineChangeDelta(decision, SpendingCategory.HHSDiscretionary)
                    + GetSpendingLineChangeDelta(decision, SpendingCategory.Medicaid),
                EducationSpendingChange = GetSpendingLineChangeDelta(decision, SpendingCategory.Education),
                DefenseSpendingChange = GetSpendingLineChangeDelta(decision, SpendingCategory.Defense)
            };
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

        /// <summary>
        /// Interest on the country's existing debt stock, at its base rate (CurrencyZone.InterestRate,
        /// unless overridden - see Country.BaseDebtInterestRateOverride) plus the risk premium, scaled
        /// by the country's RiskPremiumSensitivity. For most countries this is unchanged: today's
        /// policy rate plus the full risk premium. A reserve-currency issuer (the USA) uses a real
        /// blended average rate on existing debt instead of today's policy rate, and a near-zero
        /// sensitivity to the risk-premium curve - see "Reserve-Currency Debt Interest Treatment" in
        /// CLAUDE.md for why market risk premium at a given debt-to-GDP ratio isn't equivalent across
        /// countries.
        /// </summary>
        private float GetInterestOnDebt(Country country)
        {
            EconomyState state = country.State;
            float baseRate = country.BaseDebtInterestRateOverride >= 0f
                ? country.BaseDebtInterestRateOverride
                : country.CurrencyZone.InterestRate;
            float effectiveRate = baseRate + GetDebtRiskPremium(state) * country.RiskPremiumSensitivity;
            return state.GovernmentDebt * (effectiveRate / 100f);
        }

        /// <summary>
        /// Government revenue is GetTotalTaxRevenue's theoretical figure scaled down by the country's
        /// CollectionEfficiency (enforcement quality/informal economy/evasion - see Country's doc
        /// comment); this turn's budget balance is that actual revenue minus total spending
        /// (government spending, Mandatory SpendingLine total (0 for a country without a detailed
        /// portfolio), unemployment benefits, and interest on debt - benefits, mandatory transfers,
        /// and interest are transfers, not purchases, so they're deliberately excluded from
        /// MacroSystem's national accounts G term). A deficit adds to GovernmentDebt, a surplus
        /// reduces it, hard-clamped to a sane debt-to-GDP range. Returns the actual (post-efficiency)
        /// revenue so the caller can record it on this turn's FiscalTurnReport.
        /// </summary>
        private float ApplyRevenueAndSpending(Country country, float governmentSpending, float mandatorySpending, float unemploymentBenefitCost, float interestOnDebt)
        {
            EconomyState state = country.State;
            float theoreticalRevenue = GetTotalTaxRevenue(country);
            float actualRevenue = theoreticalRevenue * country.CollectionEfficiency;
            float totalSpending = governmentSpending + mandatorySpending + unemploymentBenefitCost + interestOnDebt;
            float budgetBalance = actualRevenue - totalSpending;

            state.Budget += budgetBalance;
            float maxDebt = MaxDebtToGdpPercent / 100f * state.GDP;
            state.GovernmentDebt = Mathf.Clamp(state.GovernmentDebt - budgetBalance, 0f, maxDebt);

            return actualRevenue;
        }
    }
}
