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
        public float WelfareCost;
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
        public float PovertyRateChange;
        public float LaborForceParticipationRateChange;
        public float CrimeIndexChange;
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

        /// <summary>
        /// The fiscal reaction function's slope: how much the effective-revenue multiplier moves per
        /// point of gap between DebtToGdpRatio and the country's own ComfortableDebtToGdpPercent (see
        /// GetFiscalReactionMultiplier). This is the missing NEGATIVE feedback the debt-to-GDP system
        /// previously lacked - GetDebtRiskPremium is a real, separate mechanism (the market's own cost
        /// of lending more to an already-indebted borrower) and stays completely unchanged; it was
        /// never a substitute for a government's own countercyclical fiscal response, which is what
        /// this represents. See "Fiscal Reaction Function" in CLAUDE.md for the empirical case this
        /// value was calibrated against (every one of 0.05-0.3 failed to escape the pre-existing
        /// bimodal 0%/~294% outcome; 1.0+ genuinely stabilizes all six countries at distinct,
        /// country-appropriate levels, confirmed flat from turn 500 through turn 2000).
        /// </summary>
        private const float FiscalReactionSensitivity = 1.5f;

        /// <summary>Bounds on GetFiscalReactionMultiplier's output - a 2x range (0.5x-1.5x effective revenue) is what the calibration above needed to actually overcome the debt-risk-premium's own reinforcing loop at realistic debt-to-GDP extremes, not a "modest" single-digit-percent cap that empirically failed to do so.</summary>
        private const float MinFiscalReactionMultiplier = 0.5f;
        private const float MaxFiscalReactionMultiplier = 1.5f;

        /// <summary>
        /// The missing negative feedback in the debt-to-GDP system: a country's own government
        /// modestly tightens (collects relatively more of its theoretical tax revenue) as debt rises
        /// above its ComfortableDebtToGdpPercent anchor, and loosens (collects relatively less) as debt
        /// falls below it - independent of, and stacked on top of, CollectionEfficiency and
        /// GetDebtRiskPremium (which represents the market's side of the equation, not the
        /// government's). Without this, the system was found to be bimodal - every country's
        /// DebtToGdpRatio eventually settled at either exactly 0% or pinned near the 300% ceiling,
        /// never anything in between, however Discretionary/Mandatory spending growth was tuned (see
        /// "SpendingLine Amount Ceiling - Debt-to-Zero Fix" in CLAUDE.md for that investigation).
        /// </summary>
        private float GetFiscalReactionMultiplier(Country country)
        {
            float debtGap = country.State.DebtToGdpRatio - country.ComfortableDebtToGdpPercent;
            float multiplier = 1f + FiscalReactionSensitivity * debtGap / 100f;
            return Mathf.Clamp(multiplier, MinFiscalReactionMultiplier, MaxFiscalReactionMultiplier);
        }

        /// <summary>Sane bounds for a country's own tariff policy (see PolicyDecision.TariffRateChange).</summary>
        private const float MinBaseTariffRate = 0f;
        private const float MaxBaseTariffRate = 50f;

        /// <summary>Bounds for a WelfareProgram's GenerosityLevel - uniform across every WelfareProgramType (unlike TaxLine's per-type MinRate/MaxRate), since the task specifies a single 0-100% range for all six.</summary>
        private const float MinGenerosityLevel = 0f;
        private const float MaxGenerosityLevel = 100f;

        /// <summary>Bounds for Country.MinimumWagePercentOfMedian (a Kaitz-index-style percent of median wage) - a gameplay ceiling above any real-world minimum wage's Kaitz index, not a researched maximum.</summary>
        private const float MinMinimumWagePercent = 0f;
        private const float MaxMinimumWagePercent = 100f;

        /// <summary>Bounds for Country.PoliceFundingLevel/SentencingSeverity - both share one uniform 0-100 range (unlike TaxLine's per-type ranges), since there's no per-country real-world figure to bound them against.</summary>
        private const float MinPolicyDialLevel = 0f;
        private const float MaxPolicyDialLevel = 100f;

        /// <summary>Bounds for this turn's requested PERCENTAGE change to a Discretionary SpendingLine (see PolicyDecision.SpendingLineChanges).</summary>
        private const float DiscretionaryPercentChangeRange = 30f;

        /// <summary>
        /// Narrower than DiscretionaryPercentChangeRange - reflects the real political difficulty of
        /// entitlement reform (Social Security, Medicare, Medicaid, etc. aren't cut or expanded as
        /// freely, in one turn, as a Discretionary line).
        /// </summary>
        private const float MandatoryPercentChangeRange = 15f;

        /// <summary>
        /// Hard floor/ceiling on every SpendingLine's Amount, expressed as a multiple of that line's
        /// own SpendingLine.SeedAmount - not of its current Amount. Percentage-of-current-value changes
        /// compound geometrically if the same large percentage is held for many turns in a row (see
        /// "Percentage-Based Spending Sliders" in CLAUDE.md for the runaway-divergence finding this
        /// closes off); anchoring the clamp to SeedAmount, rather than letting it ride with the current
        /// Amount, is what actually stops the compounding - a clamp relative to the current value would
        /// just get carried along by the same exponential growth it's supposed to bound. Applies to
        /// both Discretionary (ApplySpendingLineChanges) and Mandatory categories' PLAYER-driven
        /// changes, regardless of how many turns of repeated changes are stacked.
        ///
        /// For a Discretionary line, SeedAmount is NOT frozen at construction - ApplyDiscretionarySpendingGrowth
        /// grows it in lockstep with the automatic GDP-tracking growth applied to Amount (see that
        /// method's doc comment for why this was necessary - an earlier version left SeedAmount fixed
        /// forever, which silently froze this ceiling in absolute dollar terms and broke the "G tracks
        /// GDP" property "Discretionary Spending Growth" depends on; see "SpendingLine Amount Ceiling
        /// - Debt-to-Zero Fix" in CLAUDE.md). A Mandatory line's SeedAmount stays genuinely fixed at
        /// construction, since Mandatory lines have no automatic growth mechanism to track in the
        /// first place.
        /// </summary>
        private const float MinSpendingLineAmountRatio = 0.2f;
        private const float MaxSpendingLineAmountRatio = 3.0f;

        private static float ClampToSeedRange(SpendingLine line, float amount)
        {
            return Mathf.Clamp(amount, line.SeedAmount * MinSpendingLineAmountRatio, line.SeedAmount * MaxSpendingLineAmountRatio);
        }

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
                ApplyPartnerTariffOverrides(country, tariffDecision);
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
            ApplyWelfareGenerosityChanges(country, decision);
            ApplyMinimumWageChange(country, decision);
            ApplyCrimePolicyChanges(country, decision);
            DetailedSpendingResult spendingResult = ResolveSpendingForTurn(country, decision);
            MacroSystem.ApplyCategorySpendingEffects(country, spendingResult.EffectiveDecision);
            MacroSystem.ApplyWelfareProgramEffects(country);

            float unemploymentBenefitCost = GetUnemploymentBenefitCost(country);
            float interestOnDebt = GetInterestOnDebt(country);
            float welfareCost = GetTotalWelfareCost(country);
            float revenue = ApplyRevenueAndSpending(country, spendingResult.GovernmentSpending, spendingResult.MandatorySpending, unemploymentBenefitCost, interestOnDebt, welfareCost);

            _lastFiscalReports[country.Id] = new FiscalTurnReport
            {
                Revenue = revenue,
                BaselineGovernmentSpending = spendingResult.BaselineGovernmentSpending,
                DiscretionarySpending = spendingResult.DiscretionarySpendingChangeThisTurn,
                MandatorySpending = spendingResult.MandatorySpending,
                UnemploymentBenefitCost = unemploymentBenefitCost,
                InterestOnDebt = interestOnDebt,
                TariffRevenue = tariffRevenue,
                WelfareCost = welfareCost
            };

            MacroSystem.ApplyNationalAccounts(country, spendingResult.GovernmentSpending, interestRate);
            MacroSystem.ApplyPotentialGdpGrowth(country);

            float actualGrowthRate = (state.GDP - gdpBeforeThisTurn) / Mathf.Max(gdpBeforeThisTurn, 1f) * 100f;
            MacroSystem.ApplyOkunsLaw(country, actualGrowthRate);
            MacroSystem.ApplyPhillipsCurveInflation(country);
            MacroSystem.ApplyInflationExpectations(state);
            MacroSystem.ApplyPovertyRate(country);
            MacroSystem.ApplyLaborForceParticipationRate(country);
            MacroSystem.ApplyCrimeIndex(country);
            MacroSystem.ApplyCrimeEffects(country);

            MacroSystem.ApplyApprovalRating(country, spendingResult.EffectiveDecision, actualGrowthRate, totalTaxHike, spendingResult.MandatorySpendingChangeThisTurn);

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
            float povertyBefore = state.PovertyRate;
            float laborForceParticipationBefore = state.LaborForceParticipationRate;
            float crimeIndexBefore = state.CrimeIndex;

            ApplyTariffRateChange(previewCountry, decision);
            ApplyPartnerTariffOverrides(previewCountry, decision);
            TradeSystem.ApplyTradeEffects(previewCountry, _world);

            float totalTaxHike = ApplyTaxRateChanges(previewCountry, decision);
            ApplyWelfareGenerosityChanges(previewCountry, decision);
            ApplyMinimumWageChange(previewCountry, decision);
            ApplyCrimePolicyChanges(previewCountry, decision);
            DetailedSpendingResult spendingResult = ResolveSpendingForTurn(previewCountry, decision);
            MacroSystem.ApplyCategorySpendingEffects(previewCountry, spendingResult.EffectiveDecision);
            MacroSystem.ApplyWelfareProgramEffects(previewCountry);

            float unemploymentBenefitCost = GetUnemploymentBenefitCost(previewCountry);
            float interestOnDebt = GetInterestOnDebt(previewCountry);
            float welfareCost = GetTotalWelfareCost(previewCountry);
            ApplyRevenueAndSpending(previewCountry, spendingResult.GovernmentSpending, spendingResult.MandatorySpending, unemploymentBenefitCost, interestOnDebt, welfareCost);

            float previewedInterestRate = previewCountry.CurrentFedChair != null
                ? Mathf.Clamp(
                    TaylorRule.GetSuggestedInterestRate(previewCountry) + previewCountry.CurrentFedChair.RateBias,
                    CurrencySystem.MinInterestRate, CurrencySystem.MaxInterestRate)
                : Mathf.Clamp(
                    previewCountry.CurrencyZone.InterestRate + decision.InterestRateChange,
                    CurrencySystem.MinInterestRate, CurrencySystem.MaxInterestRate);
            MacroSystem.ApplyNationalAccounts(previewCountry, spendingResult.GovernmentSpending, previewedInterestRate);
            MacroSystem.ApplyPotentialGdpGrowth(previewCountry);

            float actualGrowthRate = (state.GDP - gdpBeforeThisTurn) / Mathf.Max(gdpBeforeThisTurn, 1f) * 100f;
            MacroSystem.ApplyOkunsLaw(previewCountry, actualGrowthRate);
            MacroSystem.ApplyPhillipsCurveInflation(previewCountry);
            MacroSystem.ApplyInflationExpectations(state);
            MacroSystem.ApplyPovertyRate(previewCountry);
            MacroSystem.ApplyLaborForceParticipationRate(previewCountry);
            MacroSystem.ApplyCrimeIndex(previewCountry);
            MacroSystem.ApplyCrimeEffects(previewCountry);

            MacroSystem.ApplyApprovalRating(previewCountry, spendingResult.EffectiveDecision, actualGrowthRate, totalTaxHike, spendingResult.MandatorySpendingChangeThisTurn);

            return new PolicyPreview
            {
                GdpGrowthPercent = actualGrowthRate,
                UnemploymentChange = state.Unemployment - unemploymentBefore,
                InflationChange = state.Inflation - inflationBefore,
                ApprovalChange = state.ApprovalRating - approvalBefore,
                NetBudgetImpact = state.Budget - budgetBefore,
                PovertyRateChange = state.PovertyRate - povertyBefore,
                LaborForceParticipationRateChange = state.LaborForceParticipationRate - laborForceParticipationBefore,
                CrimeIndexChange = state.CrimeIndex - crimeIndexBefore
            };
        }

        /// <summary>
        /// A throwaway Country for PreviewTurn: its own EconomyState clone (so GDP/Inflation/etc.
        /// mutations never touch the real one), its own copy of the structural fields that
        /// MacroSystem.ApplyCategorySpendingEffects/ApplyTariffRateChange mutate (PotentialGrowthRate,
        /// BaseTariffRate), and its own deep-cloned TaxLines (ApplyTaxRateChanges mutates TaxLine.Rate,
        /// so these can't be shared references the way the CurrencyZone reference is) - but the SAME
        /// CurrencyZone reference (read-only here - see PreviewTurn's remarks on why its InterestRate
        /// is never written). CollectionEfficiency, BaseDebtInterestRateOverride,
        /// RiskPremiumSensitivity, ComfortableDebtToGdpPercent, and BaselinePovertyRate are all copied
        /// explicitly since none is a constructor parameter (each defaults the same as on a real
        /// Country) - without this the preview would overstate revenue for every country whose real
        /// CollectionEfficiency is below 1, overstate InterestOnDebt for a reserve-currency issuer (the
        /// USA) whose real risk-premium sensitivity is near zero, misjudge GetFiscalReactionMultiplier
        /// for every country whose real comfort anchor isn't the 60f default, and misjudge
        /// ApplyPovertyRate's baseline for every country whose real baseline isn't the 10f default.
        /// BaselineLaborForceParticipationRate, MinimumWageImplemented, MinimumWagePercentOfMedian, and
        /// BaselineMinimumWagePercentOfMedian are copied for the same reason - none is a constructor
        /// parameter, and ApplyMinimumWageChange mutates MinimumWagePercentOfMedian directly.
        /// SpendingLines is deep-cloned for the same reason TaxLines is -
        /// ApplySpendingLineChanges mutates SpendingLine.Amount, so these can't be shared references
        /// either. WelfarePrograms is ALSO deep-cloned (via WelfareProgram.Clone()) for the same reason -
        /// ApplyWelfareGenerosityChanges mutates WelfareProgram.GenerosityLevel. TradePartners is ALSO
        /// deep-cloned (via TradePartner.Clone()) for the same reason -
        /// ApplyPartnerTariffOverrides mutates TradePartner.PlayerTariffOverride, so a shared reference
        /// would leak the preview's draft override into the real World the moment PreviewTurn ran, not
        /// just when the player commits it. CurrentFedChair is a shared (not cloned) reference -
        /// nothing in PreviewTurn or ApplyDomesticPolicy ever mutates a FedChair's own fields, only
        /// reads RateBias, so this is safe unlike TaxLines/SpendingLines/TradePartners/WelfarePrograms.
        /// </summary>
        private static Country ClonePreviewCountry(Country country)
        {
            return new Country(
                country.Id, country.Name, country.State.Clone(), country.CurrencyZone, country.BaseTariffRate,
                country.NaturalUnemploymentRate, country.PotentialGrowthRate, country.GovernmentSpendingRate,
                country.BenefitRatePerUnemployed)
            {
                TradePartners = ClonePreviewTradePartners(country.TradePartners),
                TaxLines = ClonePreviewTaxLines(country.TaxLines),
                SpendingLines = ClonePreviewSpendingLines(country.SpendingLines),
                WelfarePrograms = ClonePreviewWelfarePrograms(country.WelfarePrograms),
                CollectionEfficiency = country.CollectionEfficiency,
                BaseDebtInterestRateOverride = country.BaseDebtInterestRateOverride,
                RiskPremiumSensitivity = country.RiskPremiumSensitivity,
                ComfortableDebtToGdpPercent = country.ComfortableDebtToGdpPercent,
                BaselinePovertyRate = country.BaselinePovertyRate,
                BaselineLaborForceParticipationRate = country.BaselineLaborForceParticipationRate,
                MinimumWageImplemented = country.MinimumWageImplemented,
                MinimumWagePercentOfMedian = country.MinimumWagePercentOfMedian,
                BaselineMinimumWagePercentOfMedian = country.BaselineMinimumWagePercentOfMedian,
                BaselineCrimeIndex = country.BaselineCrimeIndex,
                PoliceFundingLevel = country.PoliceFundingLevel,
                SentencingSeverity = country.SentencingSeverity,
                CurrentFedChair = country.CurrentFedChair
            };
        }

        private static List<TradePartner> ClonePreviewTradePartners(List<TradePartner> tradePartners)
        {
            var clones = new List<TradePartner>(tradePartners.Count);
            foreach (TradePartner tradePartner in tradePartners)
            {
                clones.Add(tradePartner.Clone());
            }
            return clones;
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

        private static List<WelfareProgram> ClonePreviewWelfarePrograms(List<WelfareProgram> welfarePrograms)
        {
            var clones = new List<WelfareProgram>(welfarePrograms.Count);
            foreach (WelfareProgram welfareProgram in welfarePrograms)
            {
                clones.Add(welfareProgram.Clone());
            }
            return clones;
        }

        /// <summary>Direct tariff-policy control: the country's own BaseTariffRate moves by the requested change, clamped to a sane range.</summary>
        private void ApplyTariffRateChange(Country country, PolicyDecision decision)
        {
            country.BaseTariffRate = Mathf.Clamp(country.BaseTariffRate + decision.TariffRateChange, MinBaseTariffRate, MaxBaseTariffRate);
        }

        /// <summary>
        /// Sets a per-partner tariff override directly (an absolute target, not a delta - same
        /// semantics as ApplyTaxRateChanges) for every partner with an entry in
        /// PolicyDecision.PartnerTariffOverrides, clamped to the same [MinBaseTariffRate,
        /// MaxBaseTariffRate] range BaseTariffRate itself uses. A no-op for any partner with no entry
        /// this turn - once set, TradePartner.PlayerTariffOverride persists turn to turn like
        /// TaxLine.Rate does, until the player either changes it again or resets it back to "no
        /// override" (a separate, immediate action - see TradePartner.PlayerTariffOverride's doc
        /// comment). Runs before TradeSystem.ApplyTradeEffects each turn (same ordering as
        /// ApplyTariffRateChange) so this turn's override is what this turn's trade actually resolves
        /// against.
        /// </summary>
        private void ApplyPartnerTariffOverrides(Country country, PolicyDecision decision)
        {
            foreach (TradePartner tradePartner in country.TradePartners)
            {
                if (decision.PartnerTariffOverrides.TryGetValue(tradePartner.PartnerId, out float requestedRate))
                {
                    tradePartner.PlayerTariffOverride = Mathf.Clamp(requestedRate, MinBaseTariffRate, MaxBaseTariffRate);
                }
            }
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
        /// Sets every currently-implemented WelfareProgram's GenerosityLevel directly to this turn's
        /// requested PolicyDecision.WelfareGenerosityOverrides value (clamped to [MinGenerosityLevel,
        /// MaxGenerosityLevel]), a no-op for any WelfareProgramType with no entry or that isn't
        /// implemented - implementing/removing a welfare program is a separate, immediate action on
        /// WelfareProgram.IsImplemented, not something this method does. Mirrors ApplyTaxRateChanges
        /// exactly, minus the hike-tracking return value - welfare's approval effect is an ongoing
        /// STOCK effect of the current GenerosityLevel (see MacroSystem.GetWelfareApprovalEffect), not
        /// a one-time "this-turn change" penalty/bonus like the tax-hike term, so there's nothing
        /// equivalent to thread through here.
        /// </summary>
        private void ApplyWelfareGenerosityChanges(Country country, PolicyDecision decision)
        {
            foreach (WelfareProgram program in country.WelfarePrograms)
            {
                if (!program.IsImplemented)
                {
                    continue;
                }

                if (!decision.WelfareGenerosityOverrides.TryGetValue(program.Type, out float requestedGenerosity))
                {
                    continue;
                }

                program.GenerosityLevel = Mathf.Clamp(requestedGenerosity, MinGenerosityLevel, MaxGenerosityLevel);
            }
        }

        /// <summary>
        /// Sets Country.MinimumWagePercentOfMedian directly to this turn's requested
        /// PolicyDecision.MinimumWageOverride value (clamped to [MinMinimumWagePercent,
        /// MaxMinimumWagePercent]) - a no-op if the country has no statutory minimum wage
        /// (Country.MinimumWageImplemented false, e.g. Sweden/Italy) or no request was made this
        /// turn (the -1 sentinel). Mirrors ApplyTaxRateChanges/ApplyWelfareGenerosityChanges' "SET,
        /// not delta" pattern - there is no implement/remove action, since whether a country has a
        /// statutory minimum wage at all is a structural fact, not a player choice.
        /// </summary>
        private void ApplyMinimumWageChange(Country country, PolicyDecision decision)
        {
            if (!country.MinimumWageImplemented || decision.MinimumWageOverride < 0f)
            {
                return;
            }

            country.MinimumWagePercentOfMedian = Mathf.Clamp(decision.MinimumWageOverride, MinMinimumWagePercent, MaxMinimumWagePercent);
        }

        /// <summary>
        /// Sets Country.PoliceFundingLevel/SentencingSeverity directly to this turn's requested
        /// PolicyDecision overrides (each clamped to [MinPolicyDialLevel, MaxPolicyDialLevel]) - a
        /// no-op for either dial with no request this turn (the -1 sentinel). Every country has both
        /// dials (unlike minimum wage's country-specific asymmetry) - police funding and sentencing
        /// policy are universal government functions.
        /// </summary>
        private void ApplyCrimePolicyChanges(Country country, PolicyDecision decision)
        {
            if (decision.PoliceFundingOverride >= 0f)
            {
                country.PoliceFundingLevel = Mathf.Clamp(decision.PoliceFundingOverride, MinPolicyDialLevel, MaxPolicyDialLevel);
            }

            if (decision.SentencingSeverityOverride >= 0f)
            {
                country.SentencingSeverity = Mathf.Clamp(decision.SentencingSeverityOverride, MinPolicyDialLevel, MaxPolicyDialLevel);
            }
        }

        /// <summary>
        /// This turn's total welfare cost: the sum, over every implemented WelfareProgram, of
        /// GDP * (CostShareOfGdp / 100) * (GenerosityLevel / 100) - a new spending category alongside
        /// Mandatory/Discretionary/UnemploymentBenefitCost/InterestOnDebt (see ApplyRevenueAndSpending),
        /// deliberately NOT touching IncomeSecurity or any other existing SpendingLine. Treated as a
        /// transfer (excluded from MacroSystem's national accounts G term), the same reasoning already
        /// applied to Mandatory SpendingLines/UnemploymentBenefitCost/InterestOnDebt - welfare programs
        /// (UBI, means-tested transfers, healthcare/housing/childcare subsidies) are payments to
        /// individuals, not government purchases of goods and services.
        /// </summary>
        private float GetTotalWelfareCost(Country country)
        {
            float cost = 0f;
            float gdp = country.State.GDP;
            foreach (WelfareProgram program in country.WelfarePrograms)
            {
                if (!program.IsImplemented)
                {
                    continue;
                }

                cost += gdp * (program.CostShareOfGdp / 100f) * (program.GenerosityLevel / 100f);
            }

            return cost;
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
            public float MandatorySpendingChangeThisTurn;
            public PolicyDecision EffectiveDecision;
        }

        /// <summary>Per-category actual dollar change observed this turn (post-clamp against MinSpendingLineAmountRatio/MaxSpendingLineAmountRatio) from ApplySpendingLineChanges, plus the total across Mandatory lines.</summary>
        private class SpendingLineChangeResult
        {
            public readonly Dictionary<SpendingCategory, float> ActualDollarChangeByCategory = new Dictionary<SpendingCategory, float>();
            public float MandatoryDollarChangeTotal;
        }

        /// <summary>
        /// For a country with a detailed SpendingLines portfolio (Phase 1: USA only): applies this
        /// turn's SpendingLineChanges to the Discretionary lines (ApplySpendingLineChanges), then G
        /// is the sum of Discretionary line Amounts AFTER that change (Mandatory lines are transfers,
        /// excluded from G - same reasoning as UnemploymentBenefitCost/InterestOnDebt) and
        /// MandatorySpending is reported separately for ApplyRevenueAndSpending to add to total
        /// budget outflow. BaselineGovernmentSpending/DiscretionarySpendingChangeThisTurn are split
        /// as before-this-turn's-total / actual-net-change-observed (not the raw requested delta sum,
        /// since ApplySpendingLineChanges' seed-ratio clamp can clip an individual line's requested
        /// change) so
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
                ApplyDiscretionarySpendingGrowth(country);
                ApplyMandatorySpendingGrowth(country);
                float discretionaryTotalBefore = GetSpendingLineTotal(country, mandatory: false);
                SpendingLineChangeResult changeResult = ApplySpendingLineChanges(country, decision);
                float discretionaryTotalAfter = GetSpendingLineTotal(country, mandatory: false);
                float mandatoryTotal = GetSpendingLineTotal(country, mandatory: true);

                return new DetailedSpendingResult
                {
                    BaselineGovernmentSpending = discretionaryTotalBefore,
                    GovernmentSpending = discretionaryTotalAfter,
                    DiscretionarySpendingChangeThisTurn = discretionaryTotalAfter - discretionaryTotalBefore,
                    MandatorySpending = mandatoryTotal,
                    MandatorySpendingChangeThisTurn = changeResult.MandatoryDollarChangeTotal,
                    EffectiveDecision = BuildEffectiveDecisionForDetailedSpending(decision, changeResult)
                };
            }

            float baselineGovernmentSpending = GetBaselineGovernmentSpending(country);
            return new DetailedSpendingResult
            {
                BaselineGovernmentSpending = baselineGovernmentSpending,
                GovernmentSpending = baselineGovernmentSpending + decision.TotalDiscretionarySpending,
                DiscretionarySpendingChangeThisTurn = decision.TotalDiscretionarySpending,
                MandatorySpending = 0f,
                MandatorySpendingChangeThisTurn = 0f,
                EffectiveDecision = decision
            };
        }

        /// <summary>
        /// Every Discretionary line drifts up each turn at the country's own PotentialGrowthRate -
        /// the same rate PotentialGDP itself compounds at - restoring the property the old
        /// GDP-proportional GovernmentSpendingRate mechanic had (G growing in step with trend GDP),
        /// which a fixed-dollar SpendingLines portfolio otherwise loses entirely. See "Discretionary
        /// Spending Growth" in CLAUDE.md for why this specific rate is the only stable choice - a
        /// faster rate causes runaway divergence (the growing G term eventually dominates and GDP
        /// explodes), a slower one reintroduces a widening gap. A no-op for a country without a
        /// detailed SpendingLines portfolio (the loop body never runs against an empty list).
        ///
        /// SeedAmount grows by this SAME factor, right alongside Amount - this is what stops the
        /// MaxSpendingLineAmountRatio ceiling (see that constant) from silently freezing G in absolute
        /// dollar terms. An earlier version left SeedAmount fixed at construction forever, so this
        /// passive growth alone reached the 3x ceiling after ~56 turns with zero player input and then
        /// flattened Amount there for the rest of the game - which broke the very "G tracks GDP"
        /// property this method exists to provide: G stopped growing while tax revenue (proportional to
        /// GDP) kept climbing, producing an ever-widening primary surplus that paid USA's debt to
        /// exactly 0 by turn ~70 and flatlined it there - see "SpendingLine Amount Ceiling -
        /// Debt-to-Zero Fix" in CLAUDE.md. Growing both figures by the identical factor leaves their
        /// ratio unchanged when it started in range (so a previously-uncapped line is unaffected), and
        /// still lets ClampToSeedRange correct anything that drifted out - a maxed-out (player-exploited)
        /// line now stays pegged at exactly MaxSpendingLineAmountRatio times a SeedAmount that itself
        /// keeps compounding, so even the exploited case keeps tracking GDP instead of freezing.
        /// </summary>
        private void ApplyDiscretionarySpendingGrowth(Country country)
        {
            foreach (SpendingLine line in country.SpendingLines)
            {
                if (line.IsMandatory)
                {
                    continue;
                }

                float growthFactor = 1f + country.PotentialGrowthRate / 100f;
                line.SeedAmount *= growthFactor;
                line.Amount = ClampToSeedRange(line, line.Amount * growthFactor);
            }
        }

        /// <summary>
        /// Every Mandatory line ALSO drifts up each turn at the country's own PotentialGrowthRate,
        /// mirroring ApplyDiscretionarySpendingGrowth exactly (same growth rate, same lockstep
        /// SeedAmount growth so MaxSpendingLineAmountRatio's ceiling tracks GDP rather than freezing).
        /// Real mandatory/entitlement spending (Social Security, Medicare, Medicaid, etc.) grows with
        /// the economy too - demographics and healthcare-cost growth don't stop just because a program
        /// is Mandatory rather than Discretionary - and its previous fixed-dollar freeze was a second,
        /// separate contributor (alongside the debt-risk-premium feedback loop) to the debt-to-GDP
        /// bimodality "SpendingLine Amount Ceiling - Debt-to-Zero Fix" investigated. Growing Mandatory
        /// at this same rate was tried in isolation during that investigation and found to overshoot
        /// badly (DebtToGdpRatio pegged near 294%) - it only became viable once paired with
        /// GetFiscalReactionMultiplier's negative feedback (see "Fiscal Reaction Function" in
        /// CLAUDE.md), which is the fix that actually closed the gap; this growth is shipped alongside
        /// it, not in isolation. A no-op for a country without a detailed SpendingLines portfolio.
        /// </summary>
        private void ApplyMandatorySpendingGrowth(Country country)
        {
            foreach (SpendingLine line in country.SpendingLines)
            {
                if (!line.IsMandatory)
                {
                    continue;
                }

                float growthFactor = 1f + country.PotentialGrowthRate / 100f;
                line.SeedAmount *= growthFactor;
                line.Amount = ClampToSeedRange(line, line.Amount * growthFactor);
            }
        }

        /// <summary>
        /// Applies this turn's requested PERCENTAGE change (PolicyDecision.SpendingLineChanges) to
        /// every SpendingLine - both Mandatory and Discretionary are adjustable, but the requested
        /// percentage is clamped to a narrower range for Mandatory (MandatoryPercentChangeRange) than
        /// Discretionary (DiscretionaryPercentChangeRange) before being applied to that line's OWN
        /// current Amount (not a flat dollar figure - a +15% slider on an $850B line and a +15%
        /// slider on a $1B line move by proportionally different dollar amounts). The result is then
        /// clamped to [MinSpendingLineAmountRatio, MaxSpendingLineAmountRatio] of the line's own fixed
        /// SeedAmount (replacing the old floor-at-0 - 0.2x SeedAmount is always a stricter, higher
        /// floor than 0) - this is what actually stops the sustained-compounding exploit (see
        /// "Percentage-Based Spending Sliders" in CLAUDE.md): holding a slider at its max every turn
        /// for 100 turns previously produced a ~geometric blowup, and now flattens out once the line
        /// hits 3x its starting size, however many turns that took. Returns the actual dollar change
        /// observed per category (post-clamp) and the total across Mandatory lines, so callers can
        /// feed the real applied effect into MacroSystem's category-effect/approval formulas rather
        /// than the raw requested percentage.
        /// </summary>
        private SpendingLineChangeResult ApplySpendingLineChanges(Country country, PolicyDecision decision)
        {
            var result = new SpendingLineChangeResult();
            foreach (SpendingLine line in country.SpendingLines)
            {
                float amountBefore = line.Amount;

                if (decision.SpendingLineChanges.TryGetValue(line.Category, out float requestedPercent) && requestedPercent != 0f)
                {
                    float maxRange = line.IsMandatory ? MandatoryPercentChangeRange : DiscretionaryPercentChangeRange;
                    float clampedPercent = Mathf.Clamp(requestedPercent, -maxRange, maxRange);
                    line.Amount = ClampToSeedRange(line, line.Amount * (1f + clampedPercent / 100f));
                }

                float actualChange = line.Amount - amountBefore;
                result.ActualDollarChangeByCategory[line.Category] = actualChange;
                if (line.IsMandatory)
                {
                    result.MandatoryDollarChangeTotal += actualChange;
                }
            }

            return result;
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

        private static float GetActualDollarChange(SpendingLineChangeResult changeResult, SpendingCategory category)
        {
            return changeResult.ActualDollarChangeByCategory.TryGetValue(category, out float value) ? value : 0f;
        }

        /// <summary>
        /// Maps this turn's ACTUAL dollar change per Discretionary category (not the raw requested
        /// percentage - PolicyDecision.SpendingLineChanges is now a percentage of that line's own
        /// Amount, so it must be converted through ApplySpendingLineChanges' real, clamped effect
        /// before it means anything in dollar terms) onto the four legacy category-spending-effect
        /// fields (Infrastructure -&gt; Transportation, Healthcare -&gt; HHSDiscretionary,
        /// Education -&gt; Education, Defense -&gt; Defense) so MacroSystem.ApplyCategorySpendingEffects/
        /// ApplyApprovalRating (unmodified) still read a meaningful this-turn dollar delta for each of
        /// their four existing effects, without MacroSystem needing to know about SpendingCategory at
        /// all. Medicaid (Mandatory) is deliberately NOT folded into the Healthcare bucket here anymore -
        /// now that Mandatory categories are player-adjustable, doing so would double-count Medicaid's
        /// effect through both this legacy bucket AND the new, distinctly-weighted Mandatory approval
        /// term (see MacroSystem.MandatorySpendingApprovalMultiplier) that now covers it uniformly with
        /// every other Mandatory category. Every other new Discretionary category deliberately gets
        /// zero effect in this pass (Phase 1) - see CLAUDE.md's "Detailed Spending Portfolio" for the
        /// planned Phase 2.
        /// </summary>
        private static PolicyDecision BuildEffectiveDecisionForDetailedSpending(PolicyDecision decision, SpendingLineChangeResult changeResult)
        {
            return new PolicyDecision
            {
                TaxRateOverrides = decision.TaxRateOverrides,
                InterestRateChange = decision.InterestRateChange,
                TariffRateChange = decision.TariffRateChange,
                InfrastructureSpendingChange = GetActualDollarChange(changeResult, SpendingCategory.Transportation),
                HealthcareSpendingChange = GetActualDollarChange(changeResult, SpendingCategory.HHSDiscretionary),
                EducationSpendingChange = GetActualDollarChange(changeResult, SpendingCategory.Education),
                DefenseSpendingChange = GetActualDollarChange(changeResult, SpendingCategory.Defense)
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
        /// comment) and then by GetFiscalReactionMultiplier (the automatic fiscal-tightening/-loosening
        /// response to this country's own debt-to-GDP gap - see that method and "Fiscal Reaction
        /// Function" in CLAUDE.md); this turn's budget balance is that actual revenue minus total
        /// spending (government spending, Mandatory SpendingLine total (0 for a country without a
        /// detailed portfolio), unemployment benefits, interest on debt, and welfare program cost - see
        /// GetTotalWelfareCost - benefits, mandatory transfers, interest, and welfare are all transfers,
        /// not purchases, so they're deliberately excluded from MacroSystem's national accounts G
        /// term). A deficit adds to GovernmentDebt, a surplus reduces it, hard-clamped to a sane
        /// debt-to-GDP range. Returns the actual (post-efficiency, post-reaction) revenue so the caller
        /// can record it on this turn's FiscalTurnReport.
        /// </summary>
        private float ApplyRevenueAndSpending(Country country, float governmentSpending, float mandatorySpending, float unemploymentBenefitCost, float interestOnDebt, float welfareCost)
        {
            EconomyState state = country.State;
            float theoreticalRevenue = GetTotalTaxRevenue(country);
            float actualRevenue = theoreticalRevenue * country.CollectionEfficiency * GetFiscalReactionMultiplier(country);
            float totalSpending = governmentSpending + mandatorySpending + unemploymentBenefitCost + interestOnDebt + welfareCost;
            float budgetBalance = actualRevenue - totalSpending;

            state.Budget += budgetBalance;
            float maxDebt = MaxDebtToGdpPercent / 100f * state.GDP;
            state.GovernmentDebt = Mathf.Clamp(state.GovernmentDebt - budgetBalance, 0f, maxDebt);

            return actualRevenue;
        }
    }
}
