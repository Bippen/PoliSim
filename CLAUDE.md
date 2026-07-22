# PoliSim

## Overview
PoliSim is a turn-based political/economic simulation game built in Unity (C#). The player governs a country — starting with six real-world-seeded countries (USA, Sweden, Germany, France, Italy, Poland) — and makes policy decisions (a portfolio of individual taxes, category-specific spending, tariffs, interest rates) each turn. The core of the economy (GDP, unemployment, inflation) is driven by named macroeconomic theory rather than tuned-by-feel curves; a handful of surrounding mechanics (approval rating, currency strength, trade/tariff dampening) are still intentionally simple heuristics, though approval is now itself a Phillips-curve-adjacent formula rather than an ad hoc one (see "Political Layer" below). The player must balance economic performance against public approval to stay in power — literally: they face re-election every `ElectionCycle` turns and lose (game over) if approval has fallen below `ElectionSystem.LosingThreshold`.

This is an early scaffold: core data model and a minimal simulation loop, plus a first functional (unstyled) play loop - not final game content or polished UI.

## Genre & Scope
- Turn-based (not real-time). One "turn" = one simulated period (e.g. a quarter or year — exact cadence still TBD).
- Multiple playable/simulated countries: USA, Sweden, Germany, France, Italy, Poland. `WorldFactory.CreateDefault()` seeds the figures the user specified — policy rates, inflation, and USA/Poland unemployment and USA/Eurozone/Sweden/Poland potential growth — to real mid-2026 data; NAIRU, unspecified unemployment rates, government-spending shares, and starting GDP levels are stylized, directionally-realistic estimates, not researched figures (see comments in `WorldFactory.cs`).
- Germany, France, and Italy form a shared Eurozone (one `CurrencyZone` instance, one interest rate for all three). Sweden and Poland are EU members but keep independent currencies/interest rates, matching how the real EU/Eurozone relationship works. The USA is fully independent (own currency zone, not an EU member).
- The EU is modeled as a `TradeBloc`: near-zero internal tariffs between its five members (Germany, France, Italy, Sweden, Poland), one common external tariff rate applied by all of them to non-members (i.e. the USA).
- Focus is on systemic feedback between policy levers and simulation state — domestic (tax/spending/approval), fiscal (debt/deficit/automatic stabilizers), political (elections, random events), and international (tariffs, bloc membership, bilateral trade) — not on map/geography, diplomacy, or military systems (those may come later).

## Tech Stack
- Engine: Unity
- Language: C#
- No external simulation/economics libraries — game logic is hand-rolled in plain C# classes so it stays easy to reason about and unit test outside of Unity where possible.

## Project Structure
```
Assets/
  Scripts/
    Simulation/   -- simulation loop, turn advancement, macro theory, feedback rules, political layer
                     (SimulationManager, MacroSystem, TaylorRule, TradeSystem, CurrencySystem,
                     ElectionSystem, EventSystem)
    Data/         -- core state/data classes (EconomyState, Country, CurrencyZone, TradeBloc, TradePartner,
                     TaxType, TaxLine, SpendingCategory, SpendingLine, World, WorldFactory)
    Testing/      -- debug tools, not production code (SimulationTestRunner)
    UI/           -- player-facing MonoBehaviours (GameController)
```
As the project grows, expect additional folders such as `Scripts/Policies`, `Scripts/Events` — keep
simulation logic (state + rules) decoupled from Unity `MonoBehaviour`/UI concerns where practical, so
the simulation can be tested independently of the engine.

## Core Concepts
- **EconomyState**: plain C# data class holding one country's economic/political indicators for a turn — GDP, inflation, unemployment, approval rating, budget, trade balance, currency strength, `GovernmentDebt`, plus the macro-theory fields: `Consumption`, `Investment`, `PotentialGDP`, `InflationExpectations`, `ConsumerConfidence`, `BusinessConfidence`. No single tax-rate field — see `TaxType`/`TaxLine` below. `DebtToGdpRatio` is a derived read-only property (`GovernmentDebt / GDP * 100`, expressed as a percentage like `Unemployment`/`Inflation`), not a stored field, so it's always consistent with the current GDP and debt.
- **Country**: identity (`CountryId` enum) + `EconomyState` + the `CurrencyZone` it belongs to + its list of `TradePartner` links + its `TaxLines` portfolio (see below) + its `SpendingLines` portfolio (empty for five of the six countries — see "Detailed Spending Portfolio" below) + structural (non-turn-mutated) constants: `BaseTariffRate` (used only when it isn't in a trade bloc), `NaturalUnemploymentRate` (NAIRU), `PotentialGrowthRate` (trend GDP growth, %/turn), `GovernmentSpendingRate` (baseline government consumption as % of GDP — ignored for a country with a non-empty `SpendingLines`), `BenefitRatePerUnemployed` (automatic-stabilizer generosity — % of GDP spent on unemployment benefits per point of unemployment), `CollectionEfficiency` (0.0-1.0, how much of the theoretical tax base is actually collected — enforcement quality/informal economy/evasion — see "Tax Collection Efficiency" below), `BaseDebtInterestRateOverride` (-1 = unset/use `CurrencyZone.InterestRate`, otherwise a country-specific real blended average rate on existing debt), and `RiskPremiumSensitivity` (1.0 = full market exposure, the default — see "Reserve-Currency Debt Interest Treatment" below).
- **TaxType** / **TaxLine**: `TaxType` is the enum of individual tax instruments a country's fiscal portfolio can hold — `IncomeTax`, `CorporateTax`, `VAT`, `PayrollTax`, `CapitalGainsTax`, `SalesTax`, `ExciseTax`, `PropertyTax`, `EstateTax`, `WealthTax`, `CarbonTax`, `Tariffs`, `StampDuty`. `Tariffs` is listed for completeness but deliberately never gets a `TaxLine` — tariff revenue is already handled by `BaseTariffRate`/`TradeSystem`, not duplicated here (`TaxTypeBaseShares.GetBaseShareOfGdp` returns 0 for it as a defensive fallback, and `SimulationManager.GetTotalTaxRevenue` skips it explicitly too). A `TaxLine` is one instrument in a country's portfolio: `Type`, `Rate` (%, persistent — *set* turn to turn by `PolicyDecision.TaxRateOverrides`, not reset), `IsImplemented` (toggled *immediately* by the player, not deferred to Advance Turn — see `GameController`'s Tax Policy tab), a derived `BaseShareOfGdp` (looked up from `TaxTypeBaseShares` by `Type`, never stored per-instance, so every `TaxLine` of the same `Type` always agrees), and derived `MinRate`/`MaxRate` (looked up from `TaxTypeRateRanges` by `Type` — see "Tax Rate Ranges" below). `TaxLine.Clone()` exists because `PreviewTurn`'s throwaway country clone needs its own copies — `ApplyTaxRateChanges` mutates `Rate`, so these can't be shared references the way `TradePartners` is.
- **CurrencyZone**: a shared, settable interest rate. Countries that use the same currency (e.g. Germany/France/Italy) reference the *same* `CurrencyZone` instance, so a rate change affects all of them at once; independent-currency countries (USA, Sweden, Poland) each get their own instance and set their rate independently.
- **TradeBloc**: a group of member countries (identified by `CountryId`) with a shared internal tariff rate (near zero) between members and one common external tariff rate applied by every member to non-member imports. The EU bloc is built from Germany, France, Italy, Sweden, and Poland.
- **TradePartner**: one bilateral trade relationship from a country's point of view — static export/import volumes (not a full market simulation) that tariffs and currency strength act on each turn.
- **World**: the top-level container — all `Country` instances plus all `TradeBloc` instances. `WorldFactory.CreateDefault()` builds the standard six-country scenario with a small hand-authored trade network.
- **PolicyDecision**: per-country turn inputs — `TaxRateOverrides` (a `Dictionary<TaxType, float>` of this turn's requested **absolute** rate per `TaxType` — e.g. `45f` means "set this tax's rate to 45%", not "raise it by 45 points" — only meaningful for `TaxType`s the country currently has implemented; implementing/removing a tax is a separate, immediate action on `TaxLine.IsImplemented`, not part of this dictionary), `InterestRateChange` (summed across countries sharing a `CurrencyZone` into one shared-zone change), `TariffRateChange` (a direct delta to the country's own `BaseTariffRate`, separate from trade-bloc tariffs), `SpendingLineChanges` (a `Dictionary<SpendingCategory, float>` of this turn's requested dollar **change** per `SpendingCategory` — a delta, unlike `TaxRateOverrides` — only meaningful for Discretionary categories on a country with a `SpendingLines` portfolio; see "Detailed Spending Portfolio" below), and four legacy category-specific discretionary spending deltas — `HealthcareSpendingChange`, `DefenseSpendingChange`, `InfrastructureSpendingChange`, `EducationSpendingChange` — each layered on top of the country's baseline `GovernmentSpendingRate`, not the total spending figure, for a country **without** a `SpendingLines` portfolio (five of the six). `TotalDiscretionarySpending` (their sum) is what such a country's G term uses; see "Political Layer" below for why each category is tracked separately rather than combined into one generic spending number.
- **SimulationManager**: orchestrates turn order only — the macro theory and approval formula live in `MacroSystem`, elections in `ElectionSystem`, random events in `EventSystem`. Per turn: `CurrencySystem` applies interest rate changes and drifts currency strength, each country's own `TariffRateChange` is applied, `TradeSystem` resolves trade/tariffs (setting `TradeBalance`), then each country's domestic policy runs — `ApplyTaxRateChanges` (portfolio rate adjustments), `ResolveSpendingForTurn` (spending resolution — detailed `SpendingLines` or the legacy baseline+category-delta mechanic, see "Detailed Spending Portfolio" below), category spending effects, fiscal spending/budget/debt (see "Fiscal Accounting" below, including `GetTotalTaxRevenue`), `MacroSystem`'s national accounts identity (GDP), Okun's Law (unemployment), the Phillips Curve (inflation), `MacroSystem.ApplyApprovalRating`, and a random event roll (`EventSystem`). `PreviewTurn` reruns that same per-country pipeline against a throwaway `Country`/`EconomyState` clone (`ClonePreviewCountry`, including deep-cloned `TaxLines` and `SpendingLines`) to produce a `PolicyPreview` — an estimate for a not-yet-committed `PolicyDecision` (see "Live Policy Preview" below) — without mutating the real `World`, recording a `FiscalTurnReport`, or rolling an event.
- **MacroSystem**: the macroeconomic theory and the approval-rating formula — see "Economic Theory" and "Political Layer" below.
- **TaylorRule**: reference-only suggested interest rate (see "Economic Theory" below) — never applied automatically; intended for a future UI hint or an AI-controlled country's decision logic.
- **CurrencySystem**: applies summed interest rate changes per `CurrencyZone`; for countries that don't share their `CurrencyZone` with anyone else, drifts `EconomyState.CurrencyStrength` (index, 100 = neutral) toward a target based on how their interest rate compares to the average rate among their trade partners — relatively higher rate pulls strength up, relatively lower pulls it down. Shared-currency countries (Eurozone) skip this, since there's no single national currency to strengthen or weaken. This heuristic (and the export-competitiveness effect it feeds into `TradeSystem`) is still a simplified placeholder, not modeled on a specific theory.
- **TradeSystem**: looks up the applicable tariff rate for an importer/exporter pair (shared-bloc internal rate → importer's bloc external rate → importer's own `BaseTariffRate`, in that precedence order); for non-shared-currency exporters, also scales effective exports by a currency-strength factor (stronger than neutral dampens exports, weaker boosts them; shared-currency exporters always get a neutral factor). Sets `TradeBalance` (the NX term `MacroSystem` reads for GDP) and tariff revenue (added to the budget, and returned so `SimulationManager` can record it on `FiscalTurnReport`) — it does **not** touch GDP directly anymore.
- **ElectionSystem** and **EventSystem**: the rest of the political layer — see "Political Layer" below.

## Economic Theory
The core of the simulation (GDP, unemployment, inflation) is grounded in named macroeconomic
theory, implemented in `MacroSystem` and `TaylorRule`, each concept as its own small method with
named constants:

- **National Accounts Identity** (`MacroSystem.ApplyNationalAccounts`): `GDP = Consumption +
  Investment + Government + NetExports`, reverted partway toward `PotentialGDP`
  (`OutputGapReversionSpeed`) so a one-turn imbalance in the C+I+G shares (they don't sum to exactly
  100% of GDP for every country) can't compound into runaway growth or shrinkage — the identity
  result is a shock to trend output, not a full replacement of it, each turn. Consumption and
  Investment are each a share of the prior turn's GDP (`BaseConsumptionRate`, `BaseInvestmentRate`),
  scaled down by the interest rate *above* `TaylorRule.NeutralRealRate` — not above zero, since every
  seeded country sits at a positive policy rate and shouldn't be permanently penalized just for being
  at a normal rate — (Investment is more rate-sensitive: `InvestmentInterestSensitivity` >
  `ConsumptionInterestSensitivity`) and by `ConsumerConfidence`/`BusinessConfidence`. Government is
  the country's baseline `GovernmentSpendingRate` share of GDP plus the turn's
  `PolicyDecision.TotalDiscretionarySpending` (the sum of the four spending categories - see
  "Political Layer"). NetExports is `TradeBalance`, already computed by
  `TradeSystem` before this runs. GDP is floored at `MinGdp` (not exactly 0) so a country that
  shrinks all the way down can still recover — at literal 0, every term of the identity that scales
  with prior GDP would also be 0 forever.
- **Okun's Law** (`MacroSystem.ApplyOkunsLaw`): unemployment moves opposite to the gap between
  actual GDP growth this turn and the country's structural `PotentialGrowthRate`, scaled by
  `OkunCoefficient`, plus mean-reversion pulling it back toward the country's NAIRU
  (`UnemploymentReversionSpeed`) each turn absent a growth shock — unemployment drifts home to its
  structural rate rather than accumulating a growth-gap delta indefinitely. Hard-clamped to
  `[0, MaxUnemploymentPercent]` as a gameplay safety net.
- **Phillips Curve, expectations-augmented** (`MacroSystem.ApplyPhillipsCurveInflation` +
  `ApplyInflationExpectations`): inflation = `InflationExpectations` minus the unemployment gap
  versus the country's structural NAIRU (`NaturalUnemploymentRate`), scaled by `PhillipsCurveSlope`,
  hard-clamped to `[0, MaxInflationPercent]` as a gameplay safety net. `InflationExpectations` itself
  adapts each turn partway toward realized inflation (`ExpectationsAdaptationSpeed`) — standard
  adaptive-expectations formulation. Monetary policy no longer touches inflation directly; the
  interest rate's effect flows through the real chain: rate → Consumption/Investment → GDP growth →
  Okun's Law → unemployment → Phillips Curve → inflation. That indirection is intentional, not a gap.
- **Taylor Rule** (`TaylorRule.GetSuggestedInterestRate`): `suggested rate = NeutralRealRate +
  Inflation + InflationGapWeight*(Inflation - InflationTarget) + OutputGapWeight*outputGap`, where
  `outputGap = (GDP - PotentialGDP) / PotentialGDP * 100` (`TaylorRule.GetOutputGapPercent`).
  `PotentialGDP` (on `EconomyState`) grows independently each turn at `PotentialGrowthRate`
  (`MacroSystem.ApplyPotentialGdpGrowth`), decoupled from actual GDP shocks, so this is a real
  output gap rather than a growth-rate proxy. Pure reference data — nothing in `SimulationManager`
  calls it; it exists for a future UI hint or an AI-controlled country's policy logic.

When extending the economic core, keep new relationships anchored to a named theory the same way —
add a comment naming the concept, and keep every coefficient a named constant next to the method
that uses it, not an inline magic number.

## Fiscal Accounting
Government revenue, debt/deficit tracking, and automatic stabilizers live in `SimulationManager`
(not `MacroSystem`), since — matching real national-accounts theory — taxes/transfers/debt-service
aren't government *purchases*, and are deliberately excluded from the GDP identity's G term; they
only affect the budget and debt stock, with no feedback into GDP/unemployment/inflation (yet):

- **Tax revenue** (`SimulationManager.GetTotalTaxRevenue`): the *theoretical* revenue — the sum,
  over every `TaxLine` in a country's portfolio where `IsImplemented`, of `GDP * (Rate / 100) *
  BaseShareOfGdp` — replacing the original single flat `TaxRate * GDP` formula entirely (see "Tax
  Portfolio" below for the portfolio itself). `TaxType.Tariffs` is explicitly skipped even though
  it's never constructed as a `TaxLine` — defensive, so a future `TaxLine` accidentally created for
  it could never double-count revenue `TradeSystem` already collects. `SimulationManager.
  ApplyTaxRateChanges` *sets* (not adds to) every currently-implemented line's `Rate` directly to
  this turn's requested absolute value (from `PolicyDecision.TaxRateOverrides`), clamped to that
  `TaxLine`'s own `[MinRate, MaxRate]` (see "Tax Rate Ranges" below) — a request for a
  not-implemented type, or with no entry, is a no-op. It also returns the sum of every positive rate
  increase actually applied (computed before `Rate` is overwritten, since the decision only carries
  an absolute target, not a delta), threaded into `MacroSystem.ApplyApprovalRating`'s tax-hike
  penalty. `SimulationManager.ApplyRevenueAndSpending` then scales that theoretical figure down by
  the country's `CollectionEfficiency` to get the *actual* collected revenue that hits the budget —
  see "Tax Collection Efficiency" below.
- **Automatic stabilizer** (`SimulationManager.GetUnemploymentBenefitCost`): unemployment benefit
  spending that scales with the unemployment rate with no player input — `BenefitRatePerUnemployed *
  Unemployment/100 * GDP` — using this turn's starting (prior-turn) `Unemployment`, the same timing
  convention `GetBaselineGovernmentSpending` uses for prior GDP.
- **Interest on debt** (`SimulationManager.GetInterestOnDebt`): `GovernmentDebt * (baseRate +
  riskPremium * RiskPremiumSensitivity) / 100`, a new spending line. `baseRate` is
  `Country.BaseDebtInterestRateOverride` if set (≥ 0), otherwise `CurrencyZone.InterestRate` — see
  "Reserve-Currency Debt Interest Treatment" below for why the USA overrides this. The risk premium
  (`SimulationManager.GetDebtRiskPremium`) is `DebtRiskPremiumRate` per point of `DebtToGdpRatio`
  above `RiskFreeDebtToGdpPercent` (60%, the conventional EU Stability & Growth Pact benchmark),
  capped at `MaxDebtRiskPremium` — uncapped, the premium (which itself scales with Debt/GDP)
  multiplying Debt again makes `InterestOnDebt` quadratic in Debt, which can diverge to float
  infinity within a couple dozen turns rather than just running up a large-but-finite number. Scaled
  by `Country.RiskPremiumSensitivity` (1.0 for every country except the USA) before being added to
  `baseRate` — see "Reserve-Currency Debt Interest Treatment" below.
- **Budget balance and debt** (`SimulationManager.ApplyRevenueAndSpending`): `BudgetBalance =
  GetTotalTaxRevenue − (GovernmentSpending + UnemploymentBenefitCost + InterestOnDebt)`.
  `GovernmentDebt` grows by the deficit and shrinks by any surplus each turn, hard-clamped to
  `[0, MaxDebtToGdpPercent]` (300%) — a structural primary deficit (a country's spending exceeding its
  tax-portfolio revenue) with no policy response for many turns is a real, not-a-bug scenario this
  model can produce, and the ceiling keeps it a bounded fiscal-stress signal instead of an unbounded
  one. Replacing the flat `TaxRate` with the portfolio changed every country's revenue-to-GDP ratio
  simultaneously (from a uniform 25% to each country's own sum of `Rate * BaseShareOfGdp` across its
  active lines — see "Tax Portfolio" below for the resulting figures) - re-validated in the standalone
  harness the same way the original debt/fiscal work was, before porting.

## Reserve-Currency Debt Interest Treatment
`GetInterestOnDebt`'s original formula (`GovernmentDebt * (CurrencyZone.InterestRate +
riskPremium) / 100`) implicitly treats every country's sovereign risk the same way: today's policy
rate applied to the *entire* debt stock, plus a risk premium that scales purely with `DebtToGdpRatio`.
That's a reasonable simplification for most of the six countries, but it broke down specifically for
the USA once its `CollectionEfficiency` was recalibrated to a lower, federal-only figure (a prior
task) and its detailed `SpendingLines` portfolio added ~$4,010B of previously-unmodeled mandatory
spending (a later task): the resulting structural deficit pushed `DebtToGdpRatio` to ~293-297% by
turn 100 under a no-policy-change baseline — right at the edge of the 300% `MaxDebtToGdpPercent`
ceiling — which prompted this investigation.

Two compounding problems, confirmed by checking the seed-state math (`GovernmentDebt` ≈ $35,960B,
`DebtToGdpRatio` = 124%): the risk premium *was* applying meaningfully (excess 64 points above the
60% risk-free threshold × `DebtRiskPremiumRate` 0.02 = 1.28 points, pushing the effective rate from
3.75% to 5.03%, a ~34% relative increase) — but even without it, using the raw 3.75% *policy* rate
against the whole debt stock already overstated real interest, since most federal debt is
longer-duration bonds issued across many prior years at a blended rate that doesn't track today's
policy rate 1:1. Combined, the seed-state `InterestOnDebt` came out to ~$1,809B against a real net
interest figure in the ~$1.0-1.1T range — roughly 65-80% too high.

Both issues are specific to how a **reserve-currency issuer's** sovereign risk actually works, not a
bug in the risk-premium curve itself: Italy's high-debt danger at 130%+ debt-to-GDP is real (it
borrows in a currency it doesn't control and competes with other Eurozone sovereigns for the same
investor base), but the USA's isn't equivalent even at a similar ratio, because who holds the debt
and in what currency matters as much as the ratio itself — Treasury securities are the global reserve
asset, so US Treasuries don't face the same default/liquidity premium other sovereigns do at an
equivalent debt-to-GDP level. `Country` gained two fields to model this without touching the shared
risk-premium curve itself:

- **`BaseDebtInterestRateOverride`** (default `-1f` = unset, use `CurrencyZone.InterestRate`
  unchanged): lets a country's `GetInterestOnDebt` base rate be a real blended average rate on
  *existing* debt instead of today's policy rate. The USA is set to `3.3f` (~3.3%, the real
  approximate blended average interest rate on federal debt), not a researched-to-the-decimal figure.
- **`RiskPremiumSensitivity`** (default `1f` = full market exposure, unchanged for every country
  except the USA): scales `GetDebtRiskPremium`'s output before it's added to the base rate. The USA
  is set to `0.05f` — near-zero but deliberately not exactly zero, so the mechanism stays a
  responsive (if negligible) curve rather than a hard-coded immunity switch, in case a future turn
  ever pushes USA's debt-to-GDP to a genuinely extreme level.

Sweden, Germany, France, Italy, and Poland are completely untouched (both fields keep their defaults)
- their existing risk-premium curve behaves exactly as before. `SimulationManager.ClonePreviewCountry`
copies both fields explicitly (like `CollectionEfficiency`), since neither is a constructor parameter
and the live preview would otherwise silently revert to full market exposure for the USA.

**Validated in the standalone harness before porting**: at USA's seed state, `InterestOnDebt` now
computes to ~$1,210B (baseRate 3.3% + rawPremium 1.28 × sensitivity 0.05 ≈ 3.364% effective rate) -
down from ~$1,809B, and within ~10% of the real ~$1.0-1.1T citation (the remaining gap is attributable
to `GovernmentDebt` itself being seeded to match *gross* federal debt-to-GDP rather than the somewhat
smaller "debt held by the public" figure real net-interest is computed against - a pre-existing,
out-of-scope seeding choice, not something this fix touches). Re-running the 100-turn no-policy-change
baseline: USA's `DebtToGdpRatio` no longer creeps toward the ceiling - it now settles at 0% by turn
100 (running an overall budget surplus once interest expense is realistic), the same pattern Italy's
debt-to-GDP already followed after the original debt/fiscal work. The 100-turn stress run (heavy tax
hikes/spending) stays numerically bounded too (no NaN/negative/infinite values). The other five
countries' figures and trajectories are completely unaffected.

## Tax Portfolio
Replaces the original single flat `EconomyState.TaxRate` with a per-country portfolio of individual
`TaxLine`s (see `TaxType`/`TaxLine` in "Core Concepts") the player can implement, remove, and adjust
independently, instead of one generic "tax rate":

- **`TaxTypeBaseShares`**: the "rough illustrative weight" constants behind each `TaxType`'s
  `BaseShareOfGdp` — gameplay-tuning constants, not precise economic figures (real tax-base sizes
  vary hugely by country and by how exemptions/thresholds are structured; these are flat, uniform
  stand-ins). `VAT`/`SalesTax` 0.5, `IncomeTax`/`PayrollTax` 0.4, `CorporateTax` 0.15,
  `PropertyTax`/`ExciseTax`/`CarbonTax` 0.1, `CapitalGainsTax`/`WealthTax` 0.05,
  `EstateTax`/`StampDuty` 0.02. `Tariffs` isn't in the switch at all (falls through to the 0 default)
  since it never gets a `TaxLine`.
- **Starting portfolios** (`WorldFactory.SeedTaxLines`): each country's `IncomeTax`, `CorporateTax`,
  `PayrollTax`, and `CapitalGainsTax` are always implemented, at real approximate 2026 headline rates
  (e.g. USA IncomeTax 37%/CorporateTax 21%/PayrollTax 15.3%/CapitalGainsTax 20%; Sweden IncomeTax
  52%/CorporateTax 20.6%/PayrollTax 31.4%/CapitalGainsTax 30%; similarly for Germany, France, Italy,
  Poland — see `WorldFactory` for the full table). `VAT` is implemented at its real rate for every
  country except the USA (which has none); `SalesTax` is the reverse (only the USA, at 7%); `EstateTax`
  is only implemented for the USA (40%, the real top federal marginal rate — the `EstateTax`
  `BaseShareOfGdp` of 0.02 is what keeps its overall revenue contribution small, per its narrow real
  tax base, not the rate itself); `CarbonTax` is only implemented for Sweden (30%, a deliberately
  "notably high" abstraction of Sweden's real famously-high carbon price, not a literal researched
  rate). `ExciseTax`, `PropertyTax`, `WealthTax`, and `StampDuty` start inactive for every country with
  one uniform illustrative placeholder rate each (8%, 1%, 1.5%, 1% respectively) — present so the
  player can implement them, matching that no country (including the USA) starts with an active
  general `WealthTax`, the same as the real world in 2026.
- **UI** (`GameController`'s Tax Policy tab, a third right-column tab alongside Recent Turns and Trade
  & Spending): lists every `TaxType` for the player's country with an Implement/Remove button and,
  only while implemented, a slider that directly sets this turn's target rate (bounded by that
  `TaxType`'s own `[MinRate, MaxRate]` — see "Tax Rate Ranges" below), not a small per-turn delta —
  a meaningful policy shift (e.g. `IncomeTax` 37% → 55%) is reachable in a single turn rather than
  dozens of small nudges. The slider defaults to the `TaxLine`'s currently-persisted `Rate` until
  dragged (`GameController.GetTaxRateInput`), and — unlike the other policy sliders — is deliberately
  *not* reset to a neutral value after Advance Turn (`ResetPolicyInputs` skips it): once committed,
  `TaxLine.Rate` already equals whatever the slider held, so leaving the draft in place keeps it
  showing that same (now-persisted) value instead of snapping back. Implement/Remove is applied
  *immediately* (a structural on/off, separate from the rate) - it also forces an immediate
  live-preview recompute (bypassing the usual change-detection cache) rather than waiting for the
  normal slider-changed check to catch up.
- **Validation**: replacing the flat rate changes every country's revenue-to-GDP ratio at once, so it
  got the same standalone-harness treatment as the original debt/fiscal work (baseline no-policy run
  plus a stress run implementing/removing taxes and adjusting several rates simultaneously) before
  reaching the real files. Both stayed bounded over 100 turns with no NaN/negative/out-of-range
  values. Combining the task-specified `BaseShareOfGdp` weights with each country's real headline
  rates produced noticeably higher *theoretical* revenue-to-GDP ratios than the old uniform 25% for
  some countries — e.g. Sweden ~53% and France ~60%, well above their real-world total tax burdens
  (~42-44% and ~45-46%) — since summing several base-broad taxes (`IncomeTax`+`PayrollTax`+`VAT`, all
  with a 0.4-0.5 weight) understates how much their real bases overlap/exempt. This is no longer a
  live discrepancy: `CollectionEfficiency` (see "Tax Collection Efficiency" below) was added
  specifically to bring each country's *actual* (post-efficiency) revenue-to-GDP back down to its
  real-world figure without touching any seeded rate or `BaseShareOfGdp` weight.

## Tax Rate Ranges
`TaxTypeRateRanges` (in `TaxLine.cs`) gives each `TaxType` its own `[0, Max]` bound (all mins are 0)
that `SimulationManager.ApplyTaxRateChanges` clamps `PolicyDecision.TaxRateOverrides` requests to,
and that `GameController`'s Tax Policy sliders are bounded by — wide enough that a meaningful policy
shift is reachable in one turn, not gradually over dozens:

| TaxType | Max rate |
| --- | --- |
| `IncomeTax` | 70% |
| `CorporateTax` | 50% |
| `VAT` | 30% |
| `PayrollTax` | 75% |
| `CapitalGainsTax` | 50% |
| `SalesTax` | 30% |
| `PropertyTax` | 10% |
| `EstateTax` | 60% |
| `WealthTax` | 5% |
| `ExciseTax` | 30% |
| `StampDuty` | 30% |
| `CarbonTax` | 100% (unchanged — kept at the original generic bound, not given a narrower one) |

These are gameplay-tuning bounds, not precise legal maxima. Re-validated in the standalone harness
with a stress scenario ramping `IncomeTax`/`CorporateTax`/`PayrollTax` up from their seed values and
`WealthTax`/`CarbonTax` up from the turn they're implemented, landing near each type's new maximum
by turn 100 (e.g. USA `IncomeTax` 69%, `PayrollTax` 74%, `WealthTax` at its 5% cap, `CarbonTax` 95%) —
stayed numerically bounded over 100 turns (no NaN/negative GDP/infinite values), alongside the
existing implement/remove/adjust stress pattern and heavy discretionary spending.

## Tax Collection Efficiency
`Country.CollectionEfficiency` (0.0-1.0, a structural per-country constant set in `WorldFactory`)
models how much of the *theoretical* tax base is actually collected — enforcement quality, the size
of the informal economy, evasion — none of it a researched figure itself, just the dial used to
correct the Tax Portfolio's revenue-to-GDP overshoot (see "Tax Portfolio" → Validation above) back
down to each country's real-world total tax-to-GDP ratio, without changing any seeded rate.
`SimulationManager.ApplyRevenueAndSpending` applies it as `ActualRevenue = GetTotalTaxRevenue() *
CollectionEfficiency` — `GetTotalTaxRevenue` itself keeps returning the theoretical (pre-efficiency)
figure. `SimulationManager.PreviewTurn`'s throwaway country clone (`ClonePreviewCountry`) copies
`CollectionEfficiency` explicitly, since it isn't a `Country` constructor parameter (it defaults to
`1f`, same as a real `Country`) and the preview would otherwise overstate revenue for every country
whose real value is below 1.

Each country's value is solved as `CollectionEfficiency = target real-world tax-to-GDP / implied
revenue-to-GDP from the default portfolio` (implied = the harness-confirmed sum of `Rate *
BaseShareOfGdp` over each country's seeded implemented `TaxLine`s):

| Country | Implied (default portfolio) | Target (real tax-to-GDP) | CollectionEfficiency |
| --- | --- | --- | --- |
| USA | 29.37% | 18.0% (federal-only) | 18.0 / 29.37 = 0.6129 |
| Germany | 48.73% | 38% (general-government) | 38 / 48.73 = 0.7799 |
| France | 60.45% | 45% (general-government) | 45 / 60.45 = 0.7444 |
| Italy | 45.10% | 43% (general-government) | 43 / 45.10 = 0.9534 |
| Poland | 42.10% | 37% (general-government) | 37 / 42.10 = 0.8789 |
| Sweden | 53.45% | 41% (general-government) | 41 / 53.45 = 0.7671 |

**USA's target is federal-government-only** (~18.0%, real FY2025 federal revenue $5.235T against this
game's ~$29,000B starting GDP — 0.180027 * 29000 ≈ $5,221B, landing in the intended $5,200-5,300B
range), not the general-government (federal+state+local) figure used for the other five. The US has
a genuinely decentralized state/local fiscal layer (state income/sales taxes, local property taxes,
etc.) that this game doesn't model at all - using a general-government target for the USA would
implicitly credit the single national `CollectionEfficiency` dial with revenue this sim has no
separate layer to collect. Germany/France/Italy/Poland/Sweden keep their general-government targets:
their fiscal policy is comparatively centralized at the national level, and (per the same reasoning)
there's no separate state/local layer for their revenue to be misattributed to either way.

Re-validated in the standalone harness before porting: with these values applied, each country's
baseline (no-policy) actual revenue-to-GDP lands exactly on its target (18.0/38.0/45.0/43.0/37.0/
41.0%), and the 100-turn stability/stress checks (including the wider tax rate ranges above) still
hold with no NaN/negative/out-of-range values. USA's lower CollectionEfficiency (0.6129 vs the prior
0.9193) substantially cuts its actual revenue with no offsetting change to spending, so its baseline
(no-policy) fiscal trajectory genuinely changed: at the time this recalibration shipped, USA's
`DebtToGdpRatio` climbed from its seeded ~124% to ~294% over 100 turns, closely approaching (but not
breaching) the `MaxDebtToGdpPercent` (300%) ceiling - GDP/debt/budget all stayed finite and correctly
clamped, so this wasn't an instability bug, but it was flagged as a real, worth-knowing emergent
consequence of the recalibration. **This was subsequently addressed** (not reversed) by the
"Reserve-Currency Debt Interest Treatment" above, which corrected an unrelated pre-existing overshoot
in USA's `InterestOnDebt` - with that fix, USA's baseline `DebtToGdpRatio` now settles at 0% instead
of approaching the ceiling; the `CollectionEfficiency` values and reasoning here are unchanged and
still current. The other five countries' figures are unaffected (their `CollectionEfficiency` values
are unchanged) and their debt paths are unchanged from before.

## Detailed Spending Portfolio
**Phase 1** (this section): USA's government spending is broken out into detailed, individually-
tracked line items using real approximate FY2025 federal budget data, replacing the old single
`GovernmentSpendingRate` baseline + four generic category-delta sliders *for USA only* — Sweden,
Germany, France, Italy, and Poland are untouched and keep the legacy mechanic exactly as before
(an empty `Country.SpendingLines` list is the switch `SimulationManager.ResolveSpendingForTurn`
checks). **Phase 2** (a deliberately separate future task, not started here): give the 21 categories
that get no economic effect in Phase 1 their own effect, the same way Infrastructure/Healthcare/
Education/Defense already have one.

- **`SpendingCategory`** (`SpendingCategory.cs`): the enum of individual line items — 6 Mandatory
  (`SocialSecurity`, `Medicare`, `Medicaid`, `IncomeSecurity`, `VeteransBenefitsMandatory`,
  `FederalRetirement`) and 19 Discretionary (`Defense`, `VeteransAffairsDiscretionary`,
  `Transportation`, `HHSDiscretionary`, `HomelandSecurity`, `Education`, `Energy`, `Housing`,
  `Justice`, `StateForeignAffairs`, `Agriculture`, `Interior`, `NASA`, `Commerce`, `Labor`,
  `TreasuryOps`, `NSF`, `EPA`, `SBA`). `InterestOnDebt` is deliberately **not** a category — it stays
  `SimulationManager`'s existing automatic, non-editable `GetInterestOnDebt` calculation, not a
  seeded line, exactly as the task required.
- **`SpendingLine`** (`SpendingLine.cs`, mirrors `TaxLine`'s pattern): `Category`, `Amount` (in the
  same $B-scale units as GDP, persistent — *added to* turn to turn by
  `PolicyDecision.SpendingLineChanges` for Discretionary lines only, not reset), `IsMandatory`.
  Mandatory lines aren't player-adjustable in Phase 1 (`SimulationManager.ApplySpendingLineChanges`
  ignores any entry for one) — reforming an entitlement program is a future mechanic, not a slider.
- **Seeding** (`WorldFactory.SeedUsaSpendingLines`, USA only): real approximate FY2025 dollar figures
  — Mandatory: SocialSecurity $1,530B, Medicare $875B, Medicaid $620B, IncomeSecurity $700B,
  VeteransBenefitsMandatory $130B, FederalRetirement $155B (sum $4,010B). Discretionary: Defense
  $850B, VeteransAffairsDiscretionary $135B, Transportation $105B, HHSDiscretionary $130B,
  HomelandSecurity $100B, Education $80B, Energy $50B, Housing $70B, Justice $40B,
  StateForeignAffairs $60B, Agriculture $25B, Interior $17B, NASA $25B, Commerce $16B, Labor $13B,
  TreasuryOps $15B, NSF $9B, EPA $10B, SBA $1B (sum $1,751B). Directionally-realistic approximations
  sourced the same way every other real-data figure in `WorldFactory` is, not exact appropriations.
- **G-term / total-spending resolution** (`SimulationManager.ResolveSpendingForTurn`): for a country
  with a non-empty `SpendingLines` (USA), the national accounts identity's G term is the sum of
  Discretionary line `Amount`s *after* this turn's `SpendingLineChanges` are applied (Mandatory lines
  are transfers, excluded from G — same reasoning as `UnemploymentBenefitCost`/`InterestOnDebt`) —
  `ApplySpendingLineChanges` floors each line at 0 (a category can be cut to zero but not negative).
  The Mandatory total is reported separately and added into `ApplyRevenueAndSpending`'s total budget
  outflow alongside `UnemploymentBenefitCost`/`InterestOnDebt`. For a country without one (the other
  five), this is byte-for-byte the old `GetBaselineGovernmentSpending(country) +
  decision.TotalDiscretionarySpending` mechanic.
- **Mapping the four existing effects** (`SimulationManager.BuildEffectiveDecisionForDetailedSpending`):
  for USA, this turn's per-category `SpendingLineChanges` deltas are mapped onto the four legacy
  `PolicyDecision` category-spending fields so `MacroSystem.ApplyCategorySpendingEffects`/
  `ApplyApprovalRating` keep working completely unmodified — `Infrastructure` ← `Transportation`,
  `Healthcare` ← `HHSDiscretionary` + `Medicaid`, `Education` ← `Education`, `Defense` ← `Defense`.
  Every other Discretionary category (15 of the 19) gets zero economic effect in this pass — an
  accurate, adjustable dollar amount only, per the task's explicit Phase 1 scope. `MacroSystem` itself
  needed zero changes — it has no idea `SpendingCategory` exists.
- **UI** (`GameController`'s new "Spending Policy" tab, a fourth right-column tab alongside Recent
  Turns/Trade & Spending/Tax Policy — mirroring how Tax Policy already got its own tab rather than
  being crammed into the left-column policy panel): Interest on Debt as a read-only line marked
  "(automatic, last turn)"; a "Mandatory (automatic)" group listing each line's category and current
  `Amount` with no slider; a "Discretionary" group where each line gets a this-turn dollar-change
  slider (`DiscretionaryLineChangeRange`, ±100 — a flat starting-point placeholder, not scaled per
  category). The left column's old four category sliders are gone entirely (dead code once USA always
  has a `SpendingLines` portfolio, since `PlayerCountryId` is hardcoded to USA) — replaced by a short
  note pointing at the new tab, matching how the Tax Policy tab is already referenced there.
- **Reconciliation**: the task's ~$7,010B target is real total FY2025 federal outlays (mandatory +
  discretionary + net interest). This restructuring's new lines alone (mandatory $4,010B +
  discretionary $1,751B = $5,761B) plus this game's own `GetInterestOnDebt` this turn (~$1,809B, at
  USA's seed state) reconcile to **~$7,570B — about 8% (~$560B) over the ~$7,010B target**. The gap is
  entirely attributable to `GetInterestOnDebt` itself, not the new spending lines: this game's
  interest calculation (`GovernmentDebt × (InterestRate + risk premium)`, using USA's stylized
  Debt/GDP and policy-rate figures) already ran nearly double real net interest (~$1,809B vs. the real
  ~$950B baked into the ~$7,010B citation) *before* this task touched anything, and that gap is
  out of this task's scope to correct. The new mandatory+discretionary total ($5,761B) reconciles
  almost exactly against a *real* net-interest figure (5,761 + 950 ≈ $6,711B, close to the real
  ~$6,850-7,010B range), confirming the category-level data itself is sound.
- **GDP-level consequence** (flagged honestly, not silently absorbed): USA's G term previously came
  from `GovernmentSpendingRate` (17% of GDP ≈ $4,930B, which the CollectionEfficiency task's own
  federal/general-government distinction retroactively suggests was really a general-government-scale
  figure) and now comes from the Discretionary total alone (~$1,751B, a properly federal-only,
  theory-correct G — Mandatory transfers are deliberately excluded from G, matching the
  `UnemploymentBenefitCost`/`InterestOnDebt` precedent). This is a real, one-time ~$3,180B drop in G
  at the first turn this ships, which the existing `OutputGapReversionSpeed` (50%) reversion halves
  rather than passing through in full — still enough to meaningfully lower USA's GDP level and spike
  Unemployment at turn 1 (Okun's Law reacting to the growth-gap shock) before both mean-revert over
  subsequent turns. **Also**: USA's Mandatory total ($4,010B) is brand-new spending with no prior
  analogue (previously the model had no transfer categories besides `UnemploymentBenefitCost`), added
  directly to total budget outflow — combined with the prior task's lower `CollectionEfficiency`, this
  made USA's structural deficit substantially worse at the time this shipped: over 100 turns of a
  no-policy-change baseline, `DebtToGdpRatio` reached ~293-297%, right at the edge of the 300%
  `MaxDebtToGdpPercent` ceiling (clamped correctly, never breaching it — not a stability bug, but a
  real, worth-knowing outcome flagged at the time). **This was subsequently addressed** by the
  "Reserve-Currency Debt Interest Treatment" section above, which found and corrected an unrelated
  pre-existing overshoot in USA's `InterestOnDebt` itself (not in the Mandatory-spending total above,
  which is unchanged and still current) - with that fix, USA's baseline `DebtToGdpRatio` now settles
  at 0% instead of approaching the ceiling.
- **Validation**: re-validated in the standalone harness before porting — 100-turn baseline (no
  policy) and stress (heavy tax hikes/spending combined with the new mandatory drain) runs both stayed
  numerically bounded (no NaN, no negative GDP, no negative/non-finite `SpendingLine.Amount`) despite
  the GDP-level and debt-trajectory consequences above.

## Political Layer
Elections, richer policy levers, and random events - all game-rule heuristics layered on top of the
economic core, not named theory, but built with the same discipline (small named methods, tunable
constants, no magic numbers):

- **Approval Rating** (`MacroSystem.ApplyApprovalRating`): mean-reverts toward a neutral
  `NeutralApprovalRating` (50) at `ApprovalReversionSpeed`, adjusted by: the growth gap (strong
  growth helps, weak growth hurts, `GrowthApprovalSensitivity`); a Phillips-curve-adjacent "misery
  index" of how far `Unemployment`/`Inflation` sit from `NaturalUnemploymentRate`/
  `TaylorRule.InflationTarget` — **gaps, not absolute levels**, since a healthy economy sitting at
  its own structural equilibrium shouldn't be punished just for having nonzero unemployment/
  inflation (this was a real bug caught during harness validation: an earlier absolute-level version
  crashed every country's approval to 0 within ~20 turns even with no policy input at all); a
  tax-hike penalty proportional to the sum of every positive per-`TaxType` rate increase actually
  applied this turn (`TaxHikeApprovalSensitivity`) — since `PolicyDecision.TaxRateOverrides` carries
  an absolute target rather than a delta, this sum (clamped target minus prior rate, where positive)
  is computed by `SimulationManager.ApplyTaxRateChanges` before it overwrites `TaxLine.Rate`, then
  passed into `ApplyApprovalRating` as a parameter — raising several taxes at once still compounds
  the penalty, same spirit as the original delta-based hike penalty; and a category-weighted
  spending effect (see below) whose *benefit* (not its cut-side penalty) is discounted the more
  `DebtToGdpRatio` already sits above `DeficitAwarenessDebtToGdpThreshold` (60%, the same benchmark
  `SimulationManager`'s risk premium uses) — fiscal-strain awareness. Hard-clamped to `[0, 100]`.
- **Category spending effects** (`MacroSystem.ApplyCategorySpendingEffects`): each of the four
  spending categories has its own small, separable profile instead of one generic "spending" number
  — Healthcare and Education get a larger approval multiplier than Defense (Infrastructure is the
  baseline, no bonus or penalty) in `ApplyApprovalRating`, and additionally: Infrastructure spending
  (for USA, driven by the `Transportation`/`HHSDiscretionary`+`Medicaid`/`Education`/`Defense` detailed
  lines via `SimulationManager.BuildEffectiveDecisionForDetailedSpending` — see "Detailed Spending
  Portfolio" above; unchanged for the other five countries)
  nudges `PotentialGrowthRate` up a little (a lasting trend-growth boost, clamped to
  `MaxPotentialGrowthRate`); Healthcare nudges `ConsumerConfidence` up (modeling long-run
  wellbeing/productivity); Education nudges `BusinessConfidence` up (a better-skilled workforce).
  Defense has no growth/confidence side-effect at all - just its (smaller) approval effect and its
  ordinary contribution to the G term. Confidence is clamped to `[MinConfidence, MaxConfidence]`
  (0.7-1.3) since it multiplies Consumption/Investment directly - unclamped, repeated
  healthcare/education spending over many turns could eventually destabilize GDP.
- **Tariff policy** (`SimulationManager.ApplyTariffRateChange`): `PolicyDecision.TariffRateChange`
  moves a country's own `BaseTariffRate` directly, clamped to `[MinBaseTariffRate,
  MaxBaseTariffRate]` (0-50%). Applied before `TradeSystem` resolves trade each turn, so it's this
  turn's rate that affects this turn's trade — separate from (and lower-precedence than, per
  `TradeSystem.GetTariffRate`) any trade-bloc tariff that applies instead.
- **Elections** (`ElectionSystem`): every `ElectionCycle` turns (12) is an election turn
  (`IsElectionTurn`). `RunElection` checks a country's `ApprovalRating` against `LosingThreshold`
  (35) and returns the margin either way. Deliberately country-agnostic — it doesn't know or care
  which country is "the player"; that association (and the resulting simple `IsGameOver`/reason
  state, since there's no full game-over UI yet) is a `GameController` (UI-layer) concern, matching
  how `PlayerCountryId` is already hardcoded there and not in the simulation layer.
- **Events** (`EventSystem`): each turn, a small chance (`EventChancePerTurn`, 12%) per country that
  one of a hardcoded pool of `EconomicEvent`s (`EventPool`, 8 to start — e.g. "Recession in a Trading
  Partner", "Natural Disaster", "Technology Breakthrough") fires, applying a one-time
  `GdpShockPercent`/`InflationShockPoints`/`ApprovalEffect`. Deliberately plain data with no logic
  tying it to how it's consumed, so the hardcoded pool can later be swapped for AI-generated events
  without changing `EventSystem.ApplyEvent` or anything downstream. GDP shocks fade on their own via
  `MacroSystem`'s existing output-gap reversion (events never touch `PotentialGDP`); inflation shocks
  are genuinely one-turn, since the Phillips Curve fully recomputes `Inflation` from scratch next
  turn rather than carrying a delta forward. `SimulationManager.GetLastEvent(countryId)` exposes the
  turn's fired event (or null) for the UI to show as "BREAKING: ...".

## Live Policy Preview
`GameController` shows an estimate of this turn's effect under the sliders' *current* (not yet
committed) values, recomputed every `OnGUI` call:

- **`SimulationManager.PreviewTurn(countryId, decision)`** reruns the exact same per-country
  pipeline `ApplyDomesticPolicy` would (`ApplyTariffRateChange`, `TradeSystem.ApplyTradeEffects`,
  `ApplyTaxRateChanges`, `GetBaselineGovernmentSpending`, `MacroSystem.ApplyCategorySpendingEffects`,
  the fiscal helpers including `GetTotalTaxRevenue`, `MacroSystem.ApplyNationalAccounts`/
  `ApplyOkunsLaw`/`ApplyPhillipsCurveInflation`/`ApplyInflationExpectations`/`ApplyApprovalRating`)
  against a throwaway clone (`ClonePreviewCountry`: its own `EconomyState.Clone()`, its own copies of
  the structural fields those formulas mutate, and its own deep-cloned `TaxLines` — `Rate` is mutated
  by `ApplyTaxRateChanges`, so these can't be shared references the way `TradePartners` is — but the
  *same* `CurrencyZone` and `TradePartners` references, since those are read-only from `PreviewTurn`'s
  perspective) — so the result stays grounded in the real model instead of a separately hand-rolled
  estimate, and nothing it computes is ever written back to the real `World`. Deliberately never rolls an `EventSystem` event or
  advances `CurrentTurn` — a preview should be side-effect-free and deterministic, not spend part of
  the event-roll randomness budget on a turn the player might not commit to. Two small
  simplifications, both to avoid mutating a `CurrencyZone` that can be shared across countries just
  to compute a display estimate: the previewed interest rate is passed into `ApplyNationalAccounts`
  as a local value rather than actually changing the zone (so `GetInterestOnDebt`'s rate in the
  preview still reflects the current, not previewed, rate), and this turn's `CurrencyStrength` is
  used as-is rather than re-deriving its slow, heavily-damped drift.
- **`GameController.DrawPolicyPreview`** calls `PreviewTurn` with the same `PolicyDecision` the
  sliders would build if committed, then layers a cosmetic ±5-10% margin of error onto each figure
  for display only, rolled from `_previewRandom` — a `System.Random` instance that exists *only* for
  this jitter, is never read anywhere else, and is completely isolated from `EventSystem`'s own
  `System.Random` and from `UnityEngine.Random` — so viewing the preview or dragging a slider can
  never perturb the event roll or any other RNG consumer's sequence.

## Conventions
- Keep simulation state and logic free of Unity-specific dependencies (`MonoBehaviour`, `GameObject`, etc.) so it can be reasoned about and tested as plain C#.
- Favor small, explicit, named methods for each macro/feedback/trade/currency rule over one large monolithic update function, so individual rules — and individual pieces of economic theory — can be tuned or replaced independently.
- Cross-references between countries (trade partners, bloc membership) go through the `CountryId` enum, never direct object references — avoids reference cycles and keeps the data model Unity-Inspector-serializable. Shared-currency membership is instead detected by *reference equality* on `CurrencyZone` (see `CurrencySystem.SharesCurrencyZoneWithOthers`), since that shared reference is exactly what "using the same currency" means in this model.
- No comments explaining *what* code does; only note *why* when a rule's tuning or a non-obvious interaction needs explanation.

## Status
Multi-country scaffold: six countries (USA, Sweden, Germany, France, Italy, Poland) seeded with
real mid-2026 policy rates/inflation/growth, shared/independent `CurrencyZone`s with settable
interest rates, an EU `TradeBloc` with internal/external tariffs, a lightweight bilateral trade
model with currency-strength-driven export competitiveness, and a theory-grounded core (national
accounts identity, Okun's Law, Phillips Curve, plus a reference-only Taylor Rule) for GDP,
unemployment, and inflation. GDP reverts partway toward `PotentialGDP` each turn and is floored above
0, and Okun's Law/the Phillips Curve mean-revert toward NAIRU and hard-clamp their outputs, so the
no-policy-change baseline stays near equilibrium instead of drifting to an extreme over many turns
(verified with `SimulationTestRunner`, a debug `MonoBehaviour` under `Assets/Scripts/Testing/` that
runs 100 turns with no policy input and logs a per-turn/per-country sanity-check summary).
`ConsumerConfidence`/`BusinessConfidence` exist and are read by the national accounts identity but
nothing feeds them back yet (a natural next step). Government debt/deficit tracking and an
unemployment-benefit automatic stabilizer are in place (see "Fiscal Accounting" above), seeded
with real approximate starting debt-to-GDP ratios (USA ~124%, Italy ~138%, France ~116%, Germany
~63%, Poland ~59%, Sweden ~35%); debt/interest don't feed back into GDP/unemployment/inflation yet.
Tax revenue now comes from a per-country portfolio of individual `TaxLine`s instead of one flat
`TaxRate` (see "Tax Portfolio" above) — every country's revenue-to-GDP ratio changed simultaneously
as a result, re-validated in the same standalone harness before porting. Each `TaxType` now has its
own wider rate range (see "Tax Rate Ranges" above), directly settable in one turn via
`PolicyDecision.TaxRateOverrides` rather than a small per-turn delta, and each country has a
calibrated `CollectionEfficiency` (see "Tax Collection Efficiency" above) that brings its default
portfolio's actual revenue-to-GDP down to its real-world tax-to-GDP figure — USA's target is
specifically federal-government-only (~18%, not the general-government ~27% figure the other five
use), since the US has a decentralized state/local fiscal layer this game doesn't model — both
re-validated together in the same harness (baseline calibration landing on target, plus a stress run
pushing several tax types near their new maximums) before porting. USA's government spending is
further broken out into a detailed, real-FY2025-data `SpendingLines` portfolio (Phase 1 — see
"Detailed Spending Portfolio" above; the other five keep the legacy baseline+category-delta
mechanic). Combined with the lower federal-only `CollectionEfficiency` and USA's brand-new Mandatory
spending total, this initially pushed USA's no-policy-change `DebtToGdpRatio` to ~293-297% by turn
100 (right at the edge of the 300% ceiling, still numerically bounded but flagged as worth-knowing);
a follow-up investigation (see "Reserve-Currency Debt Interest Treatment" above) found and fixed an
unrelated, pre-existing ~2x overshoot in USA's `InterestOnDebt` itself (today's policy rate applied
to the whole debt stock, plus the full risk-premium curve, neither of which suits a reserve-currency
issuer) — with that fix, USA's baseline `DebtToGdpRatio` now settles at 0% instead of approaching the
ceiling, `InterestOnDebt` lands close to the real ~$1.0-1.1T figure, and the other five countries'
curves are completely unaffected. Currency strength and
trade/tariff dampening remain simple, un-theorized heuristics; approval rating is now a
Phillips-curve-adjacent formula with real mean-reversion (see "Political Layer"). A political layer
exists: `ApprovalRating`-driven elections every 12 turns (`ElectionSystem`) with a simple game-over
state on a loss, four separately-profiled discretionary spending categories plus a direct
tariff-policy lever (`PolicyDecision`), and a small hardcoded random-event pool (`EventSystem`, 8
events) that fires occasionally with a one-time GDP/inflation/approval shock. All of it was validated
in the same standalone harness used for the fiscal layer before being ported - baseline (no policy)
and stress runs (sustained category spending + tariff changes; separately, implementing/removing
taxes and adjusting several rates at once) all stayed bounded over 100 turns with no
NaN/negative/out-of-range values; the player's country won every simulated election in the fiscal-
layer stress run but lost two toward the end of the tax-portfolio one (turns 84/96, after ~80 turns of
sustained heavy spending plus repeated tax hikes) - a sensible emergent outcome of that policy
combination, not an instability bug.

A first playable loop exists: `GameController` (`Assets/Scripts/UI/`), an unstyled immediate-mode
(`OnGUI`) dashboard/policy panel for the player's country (USA, hardcoded) — shows its
`EconomyState` including `ApprovalRating`, takes this turn's `PolicyDecision` via tariff and (when
applicable) interest rate sliders on the left, a dedicated Tax Policy tab for implementing/removing/
adjusting individual taxes, and a dedicated Spending Policy tab (see "Detailed Spending Portfolio"
above) for USA's detailed Mandatory (read-only)/Discretionary (sliderable) spending lines plus a
read-only Interest on Debt line, displays the current turn's event (if any) as a "BREAKING: ..."
banner above the dashboard and a game-over banner if the player lost re-election, and advances the
turn on a button press, with every other country getting `PolicyDecision.None()`. A live preview
(see "Live Policy Preview" above) shows the sliders' (including tax-line and spending-line sliders')
estimated single-turn effect, with a cosmetic margin of error, before the player commits by pressing
Advance Turn. No save/load, no full market simulation (trade volumes are static inputs, not
supply/demand-driven), and every constant is a starting-point placeholder meant to be tuned by
playtesting.
