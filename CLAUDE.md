# PoliSim

> ## ⚠ READ FIRST — what "N anomalies detected" actually means
>
> This file quotes anomaly counts in roughly a hundred places as evidence that a change was safe. **That
> number is a 5-field check, not a whole-simulation health measure**, and every one of those quotes
> should be read with this in mind.
>
> | Check | Coverage |
> |---|---|
> | `CheckFinite` (NaN / Infinity) | **29 of 29** `EconomyState` floats, plus Country-level fields — complete. No NaN can escape |
> | `CheckSwing` (>20% turn-over-turn) | **5 of 29** — GDP, Unemployment, Inflation, InterestRate, DebtToGdpRatio, and *only* these |
> | Range checks | **4 of 29** — GDP, Unemployment, Inflation, GovernmentDebt |
>
> Anomaly counts are overwhelmingly swing anomalies, and `CheckSwing` is structurally incapable of seeing
> the other 24 tracked values because `Snapshot` stores only those five. **A runaway in PovertyRate,
> Population, Consumption, Investment, CrimeIndex, LaborForceParticipationRate or 19 others produces zero
> swing anomalies and a clean-looking run.**
>
> So "0 anomalies" proves those five fields stayed within 20% turn-over-turn and nothing went non-finite
> anywhere. It does not prove the simulation is healthy, and it has historically been read as though it
> did.
>
> **Elias's decision (2026-08-01): leave coverage at five and state this plainly instead of extending it.**
> Extending would mean ~24 threshold choices plus a third baseline discontinuity in one day, and several
> fields (NetMigrationRate, PopulationGrowthRate) legitimately exceed 20%, so blanket coverage would bury
> real signal in noise. **Revisit if something ever slips through unnoticed.** Full analysis in the
> harness audit section below.
>
> Two further reading notes for anomaly counts anywhere in this file: they are **not comparable across
> the two baseline discontinuities of 2026-08-01** (the near-zero swing floor and the pre-epoch calendar
> fix), and before `f178263` a `-runmatrix` run silently ignored `-seed`, so any matrix count predating
> that commit came from an unseeded run.

> ## ⚠ THREE BASELINE DISCONTINUITIES — READ BEFORE COMPARING ANY TWO NUMBERS IN THIS FILE
>
> **Three, all within about a fortnight.** A figure recorded on one side of any of these cannot be
> compared with a figure recorded on the other. When quoting a number from this file, check which era it
> came from first.
>
> | # | date | what changed | what it invalidates |
> |---|---|---|---|
> | 1 | 2026-08-01 | **near-zero swing floor** in the anomaly detector | every anomaly count recorded before it — the floor lowers counts against all historical figures |
> | 2 | 2026-08-01 | **pre-epoch calendar fix** (the frozen-calendar harness bug) | every run before it, which never advanced the calendar and so never exercised any date-driven system |
> | 3 | **2026-08-10** | **`DaysPerTurn` 121 → 365**, the 3.017x fiscal defect | **every trajectory, debt path, deficit, population figure and anomaly count ever recorded before it** |
>
> ⚠ **Discontinuity 3 is the widest of the three.** The first two changed what was *measured*; this one
> changed what the simulation *was*. Every baseline captured before 2026-08-10 measured a fiscal engine
> charging a full year of spending, revenue and interest every 121 days, on a calendar where 100 turns
> was 33 years. Per-turn fiscal figures happen to be unchanged by the fix — the flows were always a
> year's worth and a turn is now a year — but **population, calendar span, election frequency and the SWF
> draw all moved**, so a post-fix run compared against a pre-fix number will agree on debt and disagree
> on everything demographic, which is the most misleading possible shape for a false match. See "A turn
> is now a year" below.
>
> There is also a fourth, narrower caveat that is not a discontinuity but bites the same way: before
> `f178263` a `-runmatrix` run silently ignored `-seed`, so any matrix count predating it came from an
> unseeded run and is not reproducible at all.

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
                     ElectionSystem, EventSystem, FederalReserveSystem)
    Data/         -- core state/data classes (EconomyState, Country, CurrencyZone, TradeBloc, TradePartner,
                     TaxType, TaxLine, SpendingCategory, SpendingLine, WelfareProgramType, WelfareProgram,
                     FedChairPhilosophy, FedChair, World, WorldFactory)
    Testing/      -- debug tools, not production code (SimulationTestRunner)
    UI/           -- player-facing MonoBehaviours (GameController)
    Editor/       -- Editor-only tooling, excluded from player builds automatically (BatchSimulationRunner)
```
As the project grows, expect additional folders such as `Scripts/Policies`, `Scripts/Events` — keep
simulation logic (state + rules) decoupled from Unity `MonoBehaviour`/UI concerns where practical, so
the simulation can be tested independently of the engine.

## Core Concepts
- **EconomyState**: plain C# data class holding one country's economic/political indicators for a turn — GDP, inflation, unemployment, approval rating, budget, trade balance, currency strength, `GovernmentDebt`, `PovertyRate` (seeded from real OECD data, mean-reverts toward a baseline — see "Welfare Policy" below), plus the macro-theory fields: `Consumption`, `Investment`, `PotentialGDP`, `InflationExpectations`, `ConsumerConfidence`, `BusinessConfidence`. No single tax-rate field — see `TaxType`/`TaxLine` below. `DebtToGdpRatio` is a derived read-only property (`GovernmentDebt / GDP * 100`, expressed as a percentage like `Unemployment`/`Inflation`), not a stored field, so it's always consistent with the current GDP and debt.
- **Country**: identity (`CountryId` enum) + `EconomyState` + the `CurrencyZone` it belongs to + its list of `TradePartner` links + its `TaxLines` portfolio (see below) + its `SpendingLines` portfolio (empty for five of the six countries — see "Detailed Spending Portfolio" below) + its `WelfarePrograms` portfolio (see "Welfare Policy" below - unlike `SpendingLines`, present and adjustable for all six countries) + structural (non-turn-mutated) constants: `BaseTariffRate` (used only when it isn't in a trade bloc), `NaturalUnemploymentRate` (NAIRU), `PotentialGrowthRate` (trend GDP growth, %/turn), `GovernmentSpendingRate` (baseline government consumption as % of GDP — ignored for a country with a non-empty `SpendingLines`), `BenefitRatePerUnemployed` (automatic-stabilizer generosity — % of GDP spent on unemployment benefits per point of unemployment), `CollectionEfficiency` (0.0-1.0, how much of the theoretical tax base is actually collected — enforcement quality/informal economy/evasion — see "Tax Collection Efficiency" below), `BaseDebtInterestRateOverride` (-1 = unset/use `CurrencyZone.InterestRate`, otherwise a country-specific real blended average rate on existing debt), `RiskPremiumSensitivity` (1.0 = full market exposure, the default — see "Reserve-Currency Debt Interest Treatment" below), `ComfortableDebtToGdpPercent` (see "Fiscal Reaction Function" below), and `BaselinePovertyRate` (see "Welfare Policy" below).
- **TaxType** / **TaxLine**: `TaxType` is the enum of individual tax instruments a country's fiscal portfolio can hold — `IncomeTax`, `CorporateTax`, `VAT`, `PayrollTax`, `CapitalGainsTax`, `SalesTax`, `ExciseTax`, `PropertyTax`, `EstateTax`, `WealthTax`, `CarbonTax`, `Tariffs`, `StampDuty`. `Tariffs` is listed for completeness but deliberately never gets a `TaxLine` — tariff revenue is already handled by `BaseTariffRate`/`TradeSystem`, not duplicated here (`TaxTypeBaseShares.GetBaseShareOfGdp` returns 0 for it as a defensive fallback, and `SimulationManager.GetTotalTaxRevenue` skips it explicitly too). A `TaxLine` is one instrument in a country's portfolio: `Type`, `Rate` (%, persistent — *set* turn to turn by `PolicyDecision.TaxRateOverrides`, not reset), `IsImplemented` (toggled *immediately* by the player, not deferred to Advance Turn — see `GameController`'s Tax Policy tab), a derived `BaseShareOfGdp` (looked up from `TaxTypeBaseShares` by `Type`, never stored per-instance, so every `TaxLine` of the same `Type` always agrees), and derived `MinRate`/`MaxRate` (looked up from `TaxTypeRateRanges` by `Type` — see "Tax Rate Ranges" below). `TaxLine.Clone()` exists because `PreviewTurn`'s throwaway country clone needs its own copies — `ApplyTaxRateChanges` mutates `Rate`, so these can't be shared references the way the `CurrencyZone` reference is. `WelfareProgramType`/`WelfareProgram` (see "Welfare Policy" below) mirror this exact pattern for the country's welfare portfolio.
- **CurrencyZone**: a shared, settable interest rate. Countries that use the same currency (e.g. Germany/France/Italy) reference the *same* `CurrencyZone` instance, so a rate change affects all of them at once; independent-currency countries (USA, Sweden, Poland) each get their own instance and set their rate independently.
- **TradeBloc**: a group of member countries (identified by `CountryId`) with a shared internal tariff rate (near zero) between members and one common external tariff rate applied by every member to non-member imports. The EU bloc is built from Germany, France, Italy, Sweden, and Poland.
- **TradePartner**: one bilateral trade relationship from a country's point of view — static export/import volumes (not a full market simulation) that tariffs and currency strength act on each turn, plus `PlayerTariffOverride` (-1 = unset/no override, the default) — a player-set tariff rate specifically for imports from this one partner, which beats the usual trade-bloc/`BaseTariffRate` resolution for that relationship only; see "Per-Partner Tariff Overrides" below. `TradePartner.Clone()` exists for the same reason `TaxLine.Clone()`/`SpendingLine.Clone()` do — `PreviewTurn`'s throwaway country clone needs its own copies, since `SimulationManager.ApplyPartnerTariffOverrides` mutates `PlayerTariffOverride`.
- **World**: the top-level container — all `Country` instances plus all `TradeBloc` instances. `WorldFactory.CreateDefault()` builds the standard six-country scenario with a small hand-authored trade network.
- **PolicyDecision**: per-country turn inputs — `TaxRateOverrides` (a `Dictionary<TaxType, float>` of this turn's requested **absolute** rate per `TaxType` — e.g. `45f` means "set this tax's rate to 45%", not "raise it by 45 points" — only meaningful for `TaxType`s the country currently has implemented; implementing/removing a tax is a separate, immediate action on `TaxLine.IsImplemented`, not part of this dictionary), `InterestRateChange` (summed across countries sharing a `CurrencyZone` into one shared-zone change), `TariffRateChange` (a direct delta to the country's own `BaseTariffRate`, separate from trade-bloc tariffs), `PartnerTariffOverrides` (a `Dictionary<CountryId, float>` of this turn's requested **absolute** tariff-override rate per trade partner — only meaningful for a partner with an *active* override, the same "only currently-implemented/active gets an entry" pattern `TaxRateOverrides` uses; see "Per-Partner Tariff Overrides" below), `WelfareGenerosityOverrides` (a `Dictionary<WelfareProgramType, float>` of this turn's requested **absolute** `GenerosityLevel` per welfare program, the exact same "SET, not delta" semantics as `TaxRateOverrides` — only meaningful for a program the country currently has implemented; see "Welfare Policy" below), `SpendingLineChanges` (a `Dictionary<SpendingCategory, float>` of this turn's requested dollar **change** per `SpendingCategory` — a delta, unlike `TaxRateOverrides` — only meaningful for Discretionary categories on a country with a `SpendingLines` portfolio; see "Detailed Spending Portfolio" below), and four legacy category-specific discretionary spending deltas — `HealthcareSpendingChange`, `DefenseSpendingChange`, `InfrastructureSpendingChange`, `EducationSpendingChange` — each layered on top of the country's baseline `GovernmentSpendingRate`, not the total spending figure, for a country **without** a `SpendingLines` portfolio (five of the six). `TotalDiscretionarySpending` (their sum) is what such a country's G term uses; see "Political Layer" below for why each category is tracked separately rather than combined into one generic spending number.
- **SimulationManager**: orchestrates turn order only — the macro theory and approval formula live in `MacroSystem`, elections in `ElectionSystem`, random events in `EventSystem`. Per turn: `CurrencySystem` applies interest rate changes and drifts currency strength, each country's own `TariffRateChange` is applied, `TradeSystem` resolves trade/tariffs (setting `TradeBalance`), then each country's domestic policy runs — `ApplyTaxRateChanges` (portfolio rate adjustments), `ApplyWelfareGenerosityChanges` (welfare portfolio rate adjustments, mirroring `ApplyTaxRateChanges`), `ResolveSpendingForTurn` (spending resolution — detailed `SpendingLines` or the legacy baseline+category-delta mechanic, see "Detailed Spending Portfolio" below), category spending effects, `MacroSystem.ApplyWelfareProgramEffects` (welfare confidence effects), fiscal spending/budget/debt (see "Fiscal Accounting" below, including `GetTotalTaxRevenue` and `GetTotalWelfareCost` — see "Welfare Policy" below), `MacroSystem`'s national accounts identity (GDP), Okun's Law (unemployment, itself welfare-adjusted — see "Welfare Policy"), the Phillips Curve (inflation), `MacroSystem.ApplyPovertyRate`, `MacroSystem.ApplyApprovalRating`, and a random event roll (`EventSystem`). `PreviewTurn` reruns that same per-country pipeline against a throwaway `Country`/`EconomyState` clone (`ClonePreviewCountry`, including deep-cloned `TaxLines`, `SpendingLines`, `WelfarePrograms`, and `TradePartners`) to produce a `PolicyPreview` — an estimate for a not-yet-committed `PolicyDecision` (see "Live Policy Preview" below) — without mutating the real `World`, recording a `FiscalTurnReport`, or rolling an event.
- **MacroSystem**: the macroeconomic theory and the approval-rating formula — see "Economic Theory" and "Political Layer" below. Also owns `PovertyRate`'s own mean-reversion and every `WelfareProgram`'s small per-type effect — see "Welfare Policy" below.
- **TaylorRule**: reference-only suggested interest rate (see "Economic Theory" below) — never applied automatically; intended for a future UI hint or an AI-controlled country's decision logic.
- **CurrencySystem**: applies summed interest rate changes per `CurrencyZone`; for countries that don't share their `CurrencyZone` with anyone else, drifts `EconomyState.CurrencyStrength` (index, 100 = neutral) toward a target based on how their interest rate compares to the average rate among their trade partners — relatively higher rate pulls strength up, relatively lower pulls it down. Shared-currency countries (Eurozone) skip this, since there's no single national currency to strengthen or weaken. This heuristic (and the export-competitiveness effect it feeds into `TradeSystem`) is still a simplified placeholder, not modeled on a specific theory.
- **TradeSystem**: looks up the applicable tariff rate for an importer/exporter pair, most-specific-wins — the importer's own `TradePartner.PlayerTariffOverride` for that exporter, if set, beats even trade-bloc membership; otherwise shared-bloc internal rate → importer's bloc external rate → importer's own `BaseTariffRate`, in that precedence order (see "Per-Partner Tariff Overrides" below); for non-shared-currency exporters, also scales effective exports by a currency-strength factor (stronger than neutral dampens exports, weaker boosts them; shared-currency exporters always get a neutral factor). Sets `TradeBalance` (the NX term `MacroSystem` reads for GDP) and tariff revenue (added to the budget, and returned so `SimulationManager` can record it on `FiscalTurnReport`) — it does **not** touch GDP directly anymore.
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
checks). **Phase 2** (originally a deliberately separate future task, not started at the time this
section was written): give the categories that get no economic effect in Phase 1 their own effect,
the same way Infrastructure/Healthcare/Education/Defense already have one. **Now partially done** -
see "Detailed Spending Portfolio Phase 2" below, which gave Justice/HomelandSecurity/Energy/Housing
their own effects (Round 2 item 2); the remaining categories still have none, by the same
not-an-exhaustive-list scoping this section originally called for.

- **`SpendingCategory`** (`SpendingCategory.cs`): the enum of individual line items — 6 Mandatory
  (`SocialSecurity`, `Medicare`, `Medicaid`, `IncomeSecurity`, `VeteransBenefitsMandatory`,
  `FederalRetirement`) and 19 Discretionary (`Defense`, `VeteransAffairsDiscretionary`,
  `Transportation`, `HHSDiscretionary`, `HomelandSecurity`, `Education`, `Energy`, `Housing`,
  `Justice`, `StateForeignAffairs`, `Agriculture`, `Interior`, `NASA`, `Commerce`, `Labor`,
  `TreasuryOps`, `NSF`, `EPA`, `SBA`). `InterestOnDebt` is deliberately **not** a category — it stays
  `SimulationManager`'s existing automatic, non-editable `GetInterestOnDebt` calculation, not a
  seeded line, exactly as the task required.
- **`SpendingLine`** (`SpendingLine.cs`, mirrors `TaxLine`'s pattern): `Category`, `Amount` (in the
  same $B-scale units as GDP, persistent — adjusted turn to turn by
  `PolicyDecision.SpendingLineChanges`, for **both** Mandatory and Discretionary lines — see
  "Percentage-Based Spending Sliders" below for how that changed from the original flat-dollar,
  Discretionary-only design), `IsMandatory`.
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
  for USA, this turn's per-category actual dollar changes (see "Percentage-Based Spending Sliders"
  below for how those are derived from the player's requested percentages) are mapped onto the four
  legacy `PolicyDecision` category-spending fields so `MacroSystem.ApplyCategorySpendingEffects`/
  `ApplyApprovalRating` keep working completely unmodified — `Infrastructure` ← `Transportation`,
  `Healthcare` ← `HHSDiscretionary` **only** (Medicaid was originally also folded into this bucket,
  but was removed once Mandatory categories became separately adjustable — see "Percentage-Based
  Spending Sliders" below for why), `Education` ← `Education`, `Defense` ← `Defense`. Every other
  Discretionary category (15 of the 19) gets zero economic effect in this pass — an accurate,
  adjustable dollar amount only, per the task's explicit Phase 1 scope. `MacroSystem` itself needed
  zero changes for Phase 1 — it has no idea `SpendingCategory` exists (it later gained a Mandatory-
  specific approval term — see "Percentage-Based Spending Sliders" below — but still has no idea
  `SpendingCategory` exists; that term is fed a plain dollar total).
- **UI** (`GameController`'s new "Spending Policy" tab, a fourth right-column tab alongside Recent
  Turns/Trade & Spending/Tax Policy — mirroring how Tax Policy already got its own tab rather than
  being crammed into the left-column policy panel): Interest on Debt as a read-only line marked
  "(automatic, last turn)"; a "Mandatory" group and a "Discretionary" group, each line now sliderable
  (originally Mandatory was read-only with no slider and Discretionary used a flat ±$100 dollar-delta
  slider — see "Percentage-Based Spending Sliders" below for the redesign). The left column's old four
  category sliders are gone entirely (dead code once USA always has a `SpendingLines` portfolio, since
  `PlayerCountryId` is hardcoded to USA) — replaced by a short note pointing at the new tab, matching
  how the Tax Policy tab is already referenced there.
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
  subsequent turns. **This turn-1 shock was subsequently addressed** by "Turn-1 GDP Consistency"
  below (USA's seeded `PotentialGDP` was recalibrated so the identity is already self-consistent at
  turn 1) — GDP now moves by well under 0.1% on turn 1 instead of dropping ~9%. **Also**: USA's
  Mandatory total ($4,010B) is brand-new spending with no prior
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
  the GDP-level and debt-trajectory consequences above. **The persistent negative output gap this
  GDP-level drop caused was subsequently investigated and addressed** - see "Discretionary Spending
  Growth" below.

## Percentage-Based Spending Sliders
A follow-up task replaced every Discretionary spending slider's flat-dollar delta with a
**percentage-of-its-own-current-Amount** delta, and made Mandatory categories adjustable the same
way (at a narrower range, with a distinctly higher approval cost) - closing the gap the original
Phase 1 scope had deliberately left open ("Mandatory lines aren't player-adjustable in Phase 1").

- **Why percentage, not flat dollar**: a flat ±$100 delta (the original placeholder) meant the same
  slider swing was trivial for Defense ($850B) and enormous for SBA ($1B). `PolicyDecision.
  SpendingLineChanges[category]` now stores a **percentage** (e.g. `15f` = "+15% of that line's own
  current `Amount`", not an absolute target and not a flat delta) - so +15% on Defense is +$127.5B
  while +15% on SBA is +$150M, proportional to each line's real size.
- **Ranges** (`SimulationManager.DiscretionaryPercentChangeRange` / `MandatoryPercentChangeRange`,
  mirrored in `GameController` for the sliders' bounds and must be kept in sync): Discretionary is
  ±30%; Mandatory is a narrower ±15%, reflecting the real political/legal difficulty of touching
  entitlement programs (Social Security, Medicare, Medicaid, etc.) versus a discretionary line item.
  `SimulationManager.ApplySpendingLineChanges` clamps the requested percentage to the category-
  appropriate range, converts it to a dollar change (`Amount * percent / 100`), and floors the
  resulting `Amount` at 0 (same floor as before, now expressed as a percentage-derived delta rather
  than a flat one).
- **Mandatory's higher approval cost** (`MacroSystem.MandatorySpendingApprovalMultiplier = 3.0f`):
  distinctly higher than any Discretionary category's approval multiplier (Healthcare/Education 1.5,
  Infrastructure 1.0, Defense 0.5) - cutting Social Security by some percentage hurts approval
  noticeably more than an equivalent-percentage cut to NASA, and (symmetrically) an equivalent-size
  *increase* helps more too. Implemented as a new 5th parameter, `totalMandatorySpendingChange`, on
  `MacroSystem.ApplyApprovalRating` - the aggregate actual dollar change summed across all 6 Mandatory
  `SpendingLine`s this turn, run through the same `PercentOfGdp` normalization the other spending
  terms use, then weighted by the multiplier. This is a separate, uniform term alongside the existing
  four-category-bucket system, not folded into it.
- **Medicaid double-counting fix**: Medicaid (Mandatory) was originally mapped into the legacy
  "Healthcare" bucket (`HealthcareSpendingChange = HHSDiscretionary + Medicaid`) in
  `BuildEffectiveDecisionForDetailedSpending`. That was harmless while Medicaid was never actually
  adjustable (Phase 1), but now that Mandatory categories feed their own elevated-multiplier approval
  term, leaving Medicaid in the Healthcare bucket too would double-count it (once at Healthcare's 1.5x,
  once at the Mandatory aggregate's 3.0x). Fixed by removing Medicaid from the Healthcare bucket -
  `HealthcareSpendingChange` is now driven by `HHSDiscretionary` alone. No other Mandatory category was
  ever mapped into a legacy bucket, so this was the only fix needed.
- **UI** (`GameController`'s Spending Policy tab): both the Mandatory and Discretionary groups now show
  a single unified per-line slider (`DrawSpendingLineRow`) bounded by the category-appropriate range,
  displaying both the requested percentage and the dollar amount it implies at the line's current
  size. The Mandatory group's old "(automatic)" label and lack of a slider are gone; its section header
  now reads "Mandatory (narrower range, higher approval cost)" to signal the asymmetry to the player.
- **A real, inherent risk surfaced during validation - sustained compounding**: because each turn's
  change is a percentage *of the current Amount*, holding a large percentage push in the same
  direction on the same category for many turns in a row compounds **geometrically**, not linearly
  (unlike the old flat-dollar sliders, which could only ever grow a line arithmetically). A stress
  test that applied the same +30% push to several Discretionary lines *every single turn* for 100
  turns produced runaway divergence - GDP reaching roughly 6.4 quadrillion by turn 100, inflation
  pinned at its 30% ceiling, and a budget deficit around -27 trillion. This was not patched away at
  the time - it was a direct, expected consequence of the percentage-of-current-value design the task
  asked for, exactly analogous to how a fixed percentage interest rate compounds if never reset,
  flagged honestly rather than hidden behind a stress scenario that avoided exercising it. The
  harness's stress scenario was separately redesigned to apply large pushes only *periodically* (every
  5th turn, alternating sign, mimicking a player making occasional deliberate policy changes rather
  than pinning a slider) so that realistic-use validation wouldn't be contaminated by - or silently
  hide - this finding; the periodic version stayed fully bounded even before the fix below existed.
  **This was subsequently addressed** - see "SpendingLine Amount Ceiling" below, which closes off the
  sustained-every-turn case too, not just the periodic one.
- **Validation - explicit reliance disclosure**: this session had already found the standalone harness
  to be an imperfect stand-in once (a stale swing-detection threshold - see "Federal Reserve Rate
  Damping" below), so validation for this change went further than just running the harness. Before
  trusting the harness's numbers, the two safety-critical formulas (`ApplySpendingLineChanges` and the
  `MandatorySpendingApprovalMultiplier` wiring into `ApplyApprovalRating`) were diffed side-by-side
  against the real files to confirm the harness's ported logic is identical, not just similarly-shaped.
  With that check done, the harness reported: 100-turn baseline (events on/off) and the redesigned
  periodic-stress scenario (events on/off) all stayed numerically bounded - no NaN, no negative GDP, no
  negative/non-finite `SpendingLine.Amount` - with Mandatory categories pushed to their ±15% range
  limits during the periodic pushes. **This environment has no Unity Editor install reachable for
  headless/batch execution** (confirmed by filesystem search during an earlier task this session), so,
  as with the Federal Reserve rate-damping fix, there is no independent real-Unity confirmation for
  this change beyond the harness plus the side-by-side code-fidelity check above - running
  `SimulationTestRunner` in the actual Unity Editor remains a follow-up step for whoever next opens the
  project.

## Discretionary Spending Growth
A follow-up investigation into USA's persistently negative, slowly-widening output gap (flagged in
the "Federal Reserve" section below, where it was masking Moderate/Dovish Fed chair differentiation)
traced the root cause to this section's restructuring: unlike the legacy `GovernmentSpendingRate`
mechanic (a *percentage of GDP*, so G automatically grows in step with the economy), USA's
Discretionary `SpendingLine`s are *fixed dollar amounts* that never grow on their own. Every other
country's G still scales with GDP; USA's doesn't, and that break is what the gap was really about -
not a stale `PotentialGDP` value.

**Two hypotheses were tested and ruled out before finding the real fix:**
- **Recalibrating `PotentialGDP`'s seed** (scaling it down to match the smaller post-restructure G)
  has **zero effect on the long-run gap**. The asymptotic gap is a fixed point of the recursive
  identity dynamics (`GDP_t = 0.5·a·GDP_{t-1} + 0.5·G + 0.5·PotentialGDP_t`, where `a` is the
  GDP-proportional consumption+investment share) - fixed points don't depend on initial conditions,
  only on the structural coefficients. Confirmed empirically: rescaling the seed only moved the
  transient path, converging to the identical ~-18% asymptote either way.
- **Recalibrating `PotentialGrowthRate` downward** does shrink the gap, but nowhere near enough (even
  freezing it at 0% only closed the gap to about -15%), and it directly reintroduces the
  debt-to-GDP-near-ceiling problem from before (~286-299%) - slower `PotentialGDP` growth means
  slower actual GDP growth, which means slower tax revenue growth against largely-fixed spending,
  exactly the mechanism a prior task's debt fix depends on staying healthy. This lever moves the
  output-gap goal and the debt-safety goal in opposite directions with no sweet spot; ruled out.

**The actual fix** (`SimulationManager.ApplyDiscretionarySpendingGrowth`): every turn, before this
turn's `PolicyDecision.SpendingLineChanges` are applied, each Discretionary line grows by the
country's own `PotentialGrowthRate` - the same rate `PotentialGDP` itself compounds at. This restores
the "G grows in step with trend GDP" property the legacy mechanic had (a no-op for the other five
countries, whose `SpendingLines` list is empty). **This specific rate is the only stable choice**,
confirmed by sweeping it in the harness: growing Discretionary spending *faster* than
`PotentialGrowthRate` causes runaway divergence (the growing G term eventually dominates and GDP
explodes exponentially faster than `PotentialGDP` - at +4%/turn the gap crosses zero only briefly
around turn 75 before blowing through to +10% by turn 100 and beyond, with inflation spiraling past
14% at +6%/turn); growing it *slower* reintroduces the widening-gap/debt-ceiling problem described
above. Matching rates is the unique fixed point that keeps the ratio between actual and potential GDP
constant rather than drifting to ±∞.

**Validated in the standalone harness**: USA's output gap now **stabilizes** around -13% to -15%
(confirmed flat from turn ~25 through turn 100, in both baseline and stress modes, with and without
`EventSystem` noise) instead of **diverging** toward -18% and still widening at turn 100. Debt-to-GDP
settles around 150% - up from the 0% the "Reserve-Currency Debt Interest Treatment" fix produced
(G growing again means more total spending), but nowhere near the 300% ceiling, satisfying the
explicit "don't reintroduce the near-ceiling issue" check. No NaN, negative GDP, or divergence in
100-turn baseline or stress runs.

**Honestly, this is a substantial improvement, not a complete fix.** A mathematically exact zero
asymptotic gap turns out to be unreachable via `PotentialGDP`/`PotentialGrowthRate`/spending-growth
calibration alone, given the National Accounts Identity's fixed `BaseConsumptionRate`/
`BaseInvestmentRate` coefficients (out of this investigation's scope to touch - they're core,
theory-anchored constants, not calibration placeholders): closing the remaining gap would require
either inflating Discretionary spending back toward the old ~$4,930B general-government-scale figure
(undoing the accurate FY2025 federal-only sourcing from "Detailed Spending Portfolio" above) or
increasing those core identity coefficients. Concretely, this means **the Fed chair differentiation
gap flagged in "Federal Reserve" below is improved but not resolved** - Hawkish still clears
`TaylorRule`'s output-gap-driven floor and visibly differentiates, but a stable -13% to -15% gap is
still deep enough that Moderate and Dovish both land on `CurrencySystem.MinInterestRate` (0%)
identically, same as before this fix. Fully resolving that would mean revisiting `TaylorRule.
OutputGapWeight` itself (a shared, cross-country constant, not a USA-specific calibration) - a
separate decision, not made here.

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
  how `PlayerCountryId` is already hardcoded there and not in the simulation layer. The same
  `ElectionCycle` cadence also gates USA's Fed chair selection — see "Federal Reserve" below.
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

## Federal Reserve
USA-only mechanic: an independent central bank replaces the player's direct Interest Rate Change
slider (still unchanged for Sweden, Poland, and the Eurozone trio - see `CurrencySystem.
ApplyInterestRateChanges` below). A `FedChair` (`FedChair.cs`) picked by the player every
`ElectionSystem.ElectionCycle` turns drives USA's `CurrencyZone.InterestRate` every turn instead.

- **`FedChairPhilosophy`** (`FedChairPhilosophy.cs`): `Hawkish` / `Moderate` / `Dovish` - a label
  plus, on each `FedChair`, a numeric `RateBias` added on top of `TaylorRule.
  GetSuggestedInterestRate` before clamping. Hawkish candidates carry a positive bias (effectively
  overweighting the inflation gap - tighter policy); Dovish candidates carry a negative bias
  (effectively overweighting the output/employment gap - looser policy); Moderate candidates sit
  near 0 (tracks `TaylorRule` closely).
- **`FedChair`** (`FedChair.cs`): `Name`, `Philosophy`, `Description` (flavor/UI text), `RateBias`
  (the only field with mechanical effect). **Every `FedChair` - `FederalReserveSystem.
  CandidatePool` and the turn-0 default seeded in `WorldFactory` - is an ORIGINAL FICTIONAL
  character, never a real past or present Federal Reserve chair or any real person.** Keep this
  constraint in mind for any future addition to the candidate pool.
- **`FederalReserveSystem`** (`FederalReserveSystem.cs`): `CandidatePool` is a hardcoded list of 7
  fictional candidates (2 Hawkish, 3 Moderate, 2 Dovish - e.g. Marcus Thackeray/Ines Kowalski
  Hawkish, Theodore Voss/Priya Anand/Roland Kade Moderate, Simone Delacroix/Nathaniel Osei Dovish),
  mirroring `EventSystem.EventPool`'s "plain hardcoded data, swappable later" pattern.
  `GenerateCandidates()` draws 2-3 candidates with **distinct** philosophies (shuffles the three
  `FedChairPhilosophy` values, then picks one random candidate per chosen philosophy) using its own
  isolated `System.Random` - separate from `EventSystem`'s, `UnityEngine.Random`, and
  `GameController`'s `_previewRandom`, so drawing candidates at an election boundary never perturbs
  any other RNG consumer's sequence. `ApplyFedChairInterestRate(country)` moves `CurrencyZone.
  InterestRate` partway (`RateAdjustmentSpeed`, 0.15) toward `Clamp(TaylorRule.
  GetSuggestedInterestRate(country) + CurrentFedChair.RateBias, CurrencySystem.MinInterestRate,
  CurrencySystem.MaxInterestRate)` each turn, rather than jumping straight there - see "Federal
  Reserve Rate Damping" below for why the undamped version was a real bug, not just cosmetic.
- **The switch** (`Country.CurrentFedChair`): non-null means this country's `CurrencyZone.
  InterestRate` is Fed-driven (USA only, for now); null (every other country) means it still uses
  `PolicyDecision.InterestRateChange` exactly as before. `WorldFactory` seeds USA's turn-0 default
  as a distinct Moderate placeholder chair ("Harriet Ellsworth", `RateBias` 0), separate from
  `FederalReserveSystem.CandidatePool` so the pool stays purely "election candidates."
- **`CurrencySystem.ApplyInterestRateChanges`**: a Fed-chair country is handled in its own branch,
  calling `FederalReserveSystem.ApplyFedChairInterestRate` and skipping the delta-sum-from-
  `PolicyDecision.InterestRateChange` logic entirely - `PolicyDecision.InterestRateChange` is simply
  never read for that country. Every other country's branch is byte-for-byte unchanged.
- **UI** (`GameController`): a new "Federal Reserve" panel (left column, always visible like the
  dashboard) shows the current chair's name, philosophy, and description. `UpdateFedChairSelectionState`
  checks each frame whether `CurrentTurn + 1` is an election turn; the first time it detects this for
  a given upcoming turn, it draws 2-3 candidates via `FederalReserveSystem.GenerateCandidates` and
  remembers which turn they're for, so picking one doesn't immediately regenerate a fresh set before
  the turn actually advances. While candidates are pending, they render as buttons ("Appoint
  {name}") in the same panel and **Advance Turn is disabled** until one is chosen - picking sets
  `CurrentFedChair` immediately (an immediate action, like Tax Policy's Implement/Remove, not a
  this-turn draft) and forces a preview recompute. The Interest Rate Change slider in `DrawPolicyControls`
  is hidden whenever `CurrentFedChair != null` (in addition to the existing shared-currency check),
  so USA never shows a slider that CurrencySystem would ignore anyway.
- **Live preview**: `SimulationManager.PreviewTurn` branches the same way `CurrencySystem` does -
  for a Fed-chair country it computes the previewed interest rate from `TaylorRule.
  GetSuggestedInterestRate` plus the current chair's `RateBias` instead of reading `decision.
  InterestRateChange` (which is always 0 for USA, since the slider is hidden). `ClonePreviewCountry`
  copies `CurrentFedChair` by reference (not deep-cloned like `TaxLines`/`SpendingLines` - nothing
  ever mutates a `FedChair`'s own fields, only reads `RateBias`, so sharing the reference is safe).
- **Validation** (standalone harness, `--fedchair=Hawkish|Moderate|Dovish` fixing USA's chair for
  the full 100-turn run, `--noevents` to strip `EventSystem`'s per-run random shocks for a clean
  comparison): all three philosophies stayed numerically bounded over 100 turns (no NaN, no negative
  GDP, no divergence) in both baseline and stress modes. Averaged over turns 5-34: Hawkish
  Inflation 0.86% vs. Moderate/Dovish ~0.93% - Hawkish does run measurably tighter/lower inflation,
  the expected direction. **However**, Moderate and Dovish land on the *identical* `InterestRate`
  (0.00%, the `CurrencySystem.MinInterestRate` floor) in this comparison, both early and late in the
  run - not a bug in this mechanic, but `TaylorRule.GetSuggestedInterestRate`'s own internal
  `Math.Max(0f, ...)` floor being hit by both, because USA's output gap was persistently and deeply
  negative (originally diverging toward -18% to -20%, a consequence of the Detailed Spending
  Portfolio task's G-term GDP-level drop - see that section above). A Hawkish chair's positive bias
  is enough to clear the floor and show through; a 0 or negative bias from Moderate/Dovish isn't. **A
  follow-up investigation ("Discretionary Spending Growth" above) found and fixed the root cause** -
  USA's Discretionary spending had stopped scaling with GDP entirely, unlike every other country's G -
  which turned the *diverging* gap into a *stable* one around -13% to -15%, a large improvement. This
  remains **deep enough that Moderate and Dovish still both land on the 0% floor identically** -
  closing the gap the rest of the way would mean either inflating Discretionary spending back toward
  the old general-government-scale figure (undoing accurate FY2025 federal-only sourcing) or revising
  `TaylorRule.OutputGapWeight` itself (a shared, cross-country constant). Neither was done here; the
  mechanism is correct and Hawkish differentiation is real, but full three-way differentiation remains
  a known, not-yet-resolved limitation.

## Federal Reserve Rate Damping
A real bug, distinct from the output-gap/Fed-chair-floor limitation above: `SimulationTestRunner` (run
in the actual Unity Editor) reported 60 anomalies for the shipped Discretionary-Spending-Growth +
Fed-Chair build, including USA's interest rate crashing from 5.05% to 0.00% in a single turn (Turn 2)
and inflation swings of 20-75%+ recurring for every country as late as turns 92-99 - well past where
the standalone harness's own validation had claimed the system "stabilizes" (around turn 25).

**Investigating the harness/Unity discrepancy first**: `FederalReserveSystem.ApplyFedChairInterestRate`
was confirmed byte-for-byte identical between the harness and the real files - the harness was not
stale for this mechanic. It *was* stale in one real way: its swing-anomaly threshold was `25f`,
looser than `SimulationTestRunner.MaxSingleTurnChangePercent`'s actual `20f`, silently under-counting
anomalies for every run. Fixed to match exactly. But this threshold gap doesn't explain the
qualitative issue - the deeper problem was that the harness's own default validation runs (using
`EventSystem`'s fresh-seeded `System.Random` each process launch) simply hadn't happened to roll a
random event late in *that* run's specific 100 turns; a different seed (like the one Unity's
`SimulationTestRunner` happened to draw) rolls one later and exposes the same underlying flaw more
visibly. The 51-91-anomaly range the harness had already been producing across earlier runs was this
exact bug showing through the whole time - previously mischaracterized as "cosmetic near-zero-crossing
swing-check false positives" rather than investigated as a real oscillation.

**Root cause confirmed**: `ApplyFedChairInterestRate` set `CurrencyZone.InterestRate` to
`TaylorRule.GetSuggestedInterestRate(country) + CurrentFedChair.RateBias` *directly* every turn - no
smoothing at all, unlike every other feedback mechanic in this codebase (`CurrencyStrengthDamping`,
`ApprovalReversionSpeed`, `OutputGapReversionSpeed`, `UnemploymentReversionSpeed`,
`ExpectationsAdaptationSpeed` all move only *partway* toward a target each turn). A rate that jumps
the full distance between two very different values creates a real discrete-control oscillation: a
low rate boosts Consumption/Investment this turn, narrowing (or flipping) the output gap, which
pushes next turn's suggested rate up; applying that new rate at full strength immediately cools
Consumption/Investment back down, re-widening the gap, crashing the suggested rate back toward the
floor - a self-sustaining overshoot-correction cycle with no damping to arrest it, especially once
USA is already sitting at the `MinInterestRate` floor with zero cushion (see "Federal Reserve"
above), where *any* perturbation (a random `EventSystem` shock, in particular) can kick off a fresh
cycle.

**This also explains the correlated inflation swings in Sweden, Germany, France, Italy, and Poland**
despite none of them being touched by the Fed chair or Discretionary Spending Growth changes: USA's
interest rate feeds into `CurrencySystem.ApplyCurrencyStrength` for every one of its trade partners
(`averagePartnerRate` includes USA's rate), so a violently oscillating USA rate drives their
`CurrencyStrength` targets to oscillate too - propagating through `TradeSystem`'s currency-driven
export factor into their own `TradeBalance` (NX), then into their GDP, Okun's Law, and Phillips Curve.
Confirmed empirically: with the undamped mechanic, the other five countries logged 62 swing anomalies
over 100 turns; damping the source (below) cut that to 44 without touching anything in
`TradeSystem`/`CurrencySystem` for those five countries directly - confirming the spillover channel
and that fixing it at USA's rate was sufficient.

**Fix**: `FederalReserveSystem.RateAdjustmentSpeed` (0.15 - matching `CurrencySystem.
CurrencyStrengthDamping`'s value and role in the same file) - `ApplyFedChairInterestRate` now moves
USA's rate only 15% of the way toward this turn's target each turn, the same "move gradually toward
a moving target" pattern every other feedback mechanic in the codebase already uses. Swept in the
harness (`--ratespeed=`) from 1.0 (undamped) down to 0.05: non-interest-rate anomalies (GDP/
Unemployment/Inflation/DebtToGdpRatio swings, i.e. excluding the interest rate's own swing count,
which is inherently noisy on percent-swing checks once a decaying rate gets very close to 0) dropped
from 79 (undamped) to 59 at 0.15 - the best of the values tested, and shared with 0.05. 0.15 was kept
for consistency with the existing `CurrencyStrengthDamping` precedent. This is a smoothing fix, not a
fix for the output-gap-driven floor itself - Moderate/Dovish still settle at the same 0% floor in the
long run (confirmed unchanged: 0.00% by turns 71-100 under all three philosophies), just reached
gradually instead of in one jump, and with materially less turn-to-turn noise along the way.

**Re-validated after the fix**: baseline (100 turns, events enabled) and stress runs both stayed
numerically bounded (no NaN, no negative GDP) in the harness, with its swing threshold now matching
`SimulationTestRunner.MaxSingleTurnChangePercent` exactly (see above) so its anomaly counts are a fair
comparison against Unity's. **Actually re-running `SimulationTestRunner` inside the Unity Editor
itself was not done as part of this fix** - the environment this work was done in has no Unity
Editor install reachable for headless/batch execution, so "re-validate in the real Unity
`SimulationTestRunner`" could not be carried out directly; the harness (now confirmed non-stale for
the Fed-chair mechanic, and threshold-aligned) is the closest available substitute, not a substitute
for actually pressing Play. Running `SimulationTestRunner` in the Editor to confirm the sharp
single-turn rate crashes are gone and the anomaly count has dropped meaningfully from 60 is a
follow-up step for whoever next opens the project in Unity.

## Turn-1 GDP Consistency
A follow-up investigation checked whether USA's seeded starting values (GDP, the implicit
Consumption/Investment the national accounts identity would derive from them, the post-restructure
G, TradeBalance/NX) were internally consistent with `MacroSystem.ApplyNationalAccounts`'s own
`GDP = Consumption + Investment + Government + NetExports` identity - i.e. whether evaluating the
identity with USA's real seed values reproduced the seeded 29000 GDP figure at turn 1, or produced
something meaningfully different. They did not: evaluating C+I+G+NX with USA's actual turn-1
inputs (Consumption/Investment derived from the seeded 29000 GDP, G = the accurate federal-only
Discretionary total from "Detailed Spending Portfolio" above, NX from `TradeSystem`) came out to
roughly **24,600** - about 15% below the seeded 29000 - and `ApplyNationalAccounts`'s 50%
reversion-toward-`PotentialGDP` (which defaulted to 29000, same as GDP, since no country's
`PotentialGDP` is seeded separately except USA now) only closed half that gap, producing a real,
one-time **~9% GDP contraction (29000 -> ~26,800) on literally the first turn of any new game** -
before the "Discretionary Spending Growth" fix's -13%-to-15% equilibrium gap even had a chance to
develop gradually over the first ~25 turns the way it was designed to.

**Root cause, confirmed**: this was introduced by "Detailed Spending Portfolio" rebasing G from the
old `GovernmentSpendingRate` (17% of GDP, ≈$4,930B - close enough to `BaseConsumptionRate` +
`BaseInvestmentRate`'s implied ~20% "everything else" share that the identity was roughly
self-consistent before) down to the real federal-only Discretionary total (~$1,751B, ≈6% of GDP) -
without re-deriving USA's seeded GDP or `PotentialGDP` to match the now much-smaller G. That task
explicitly flagged the resulting GDP-level drop as a known, accepted consequence at the time (see
"GDP-level consequence" in "Detailed Spending Portfolio" above) rather than a bug - this investigation
revisits it because a sharp one-time turn-1 shock is a real, avoidable rough edge for a new game to
open with, distinct from the already-accepted gradual equilibration.

**Fix**: USA's seeded `PotentialGDP` is now **33260** (`WorldFactory.cs`), not left to default to GDP
(29000) the way every other country's is. This value was found empirically via the standalone
harness's `--usapotgdp=` sweep (added for this investigation), not a closed-form solve - the turn-1
chain (TaylorRule's suggested rate depends on the output gap, which depends on `PotentialGDP`; the
Fed-chair-driven interest rate then partially damps toward that suggested rate; the interest rate
feeds back into Consumption/Investment's rate sensitivity) has no simple closed form, so the value
was located by testing candidates until turn-1 GDP landed within a fraction of a percent of the
29000 seed. At 33260, GDP moves from 29000 to **~28999-29019 (well under +/-0.1%)** on turn 1
instead of dropping ~9%, and the output gap is already sitting at its long-run **~-14.2% to -14.5%**
equilibrium from turn 1 onward, rather than opening at ~0% and sliding into that equilibrium over the
first ~25 turns. This changes **only** `PotentialGDP` (the internal "trend output" reference value) -
USA's headline GDP figure is still 29000, still on the same real-approximate-nominal-GDP scale as
every other seeded figure (debt-to-GDP, `CollectionEfficiency`'s tax-to-GDP targets, trade volumes,
etc.), none of which reference `PotentialGDP` directly and so are completely unaffected.

**Fed-chair differentiation checked, not regressed**: because opening already at a deeply negative
output gap could plausibly have collapsed Hawkish/Moderate/Dovish differentiation immediately (rather
than just in the late game, as already documented in "Federal Reserve" above), this was checked
directly. It is unaffected: `TaylorRule.GetSuggestedInterestRate` floors its raw (pre-bias) suggested
rate at 0 *before* `FederalReserveSystem.ApplyFedChairInterestRate` adds the chair's `RateBias`, so
Hawkish's target rate is simply `0 + 1.5 = 1.5%` regardless of how negative the underlying gap is -
identical behavior whether the gap opens at -9% (old seed) or -14.5% (new seed) from turn 1. Verified
in the harness across all three philosophies at the new seed: Hawkish settles near 1.5%,
Moderate/Dovish both floor at 0% - the same pattern already documented, not a new regression.

**Validation**: 100-turn baseline (events on/off) and stress runs at the new seed all stayed
numerically bounded (no NaN, no negative GDP) in the harness - see "SpendingLine Amount Ceiling"
below for the combined validation matrix run together with that fix. As with every other fix this
session, there is no independent real-Unity confirmation beyond the harness - this environment has no
reachable Unity Editor install for headless/batch execution.

## SpendingLine Amount Ceiling
A companion fix, requested alongside the turn-1 investigation above: "Percentage-Based Spending
Sliders" (above) had already found and honestly flagged, but not fixed, a sustained-compounding
exploit - holding the same large percentage push on a Discretionary line *every single turn* (rather
than periodically) compounds geometrically and diverges (GDP reaching ~6.4 quadrillion by turn 100 in
that finding's stress test). This closes that off.

- **The mechanism**: `SpendingLine` gained a new field, `SeedAmount` - set once, at construction, to
  that line's starting `Amount`, and never mutated afterward (its `Clone()` copies `SeedAmount`
  explicitly from the source line rather than re-deriving it from the current, possibly-mutated
  `Amount` - the same reasoning `TaxLine`/`SpendingLine.Amount` sharing already required elsewhere in
  `SimulationManager.ClonePreviewCountry`). `SimulationManager.ApplySpendingLineChanges` and
  `ApplyDiscretionarySpendingGrowth` both now clamp the resulting `Amount` to
  `[MinSpendingLineAmountRatio, MaxSpendingLineAmountRatio]` x `SeedAmount` - **0.2x to 3.0x** of
  where that line started, a fixed anchor rather than a moving one. Anchoring to the ORIGINAL seed
  (not the line's current value) is what actually stops the compounding - a clamp expressed relative
  to the current `Amount` would just get carried along by the same exponential growth it's meant to
  bound, since "3x of whatever it is now" grows right along with it. This replaces the old
  floor-at-0 in `ApplySpendingLineChanges` (0.2x `SeedAmount` is always a stricter, higher floor than
  0 for every seeded line) and applies to **both** Mandatory and Discretionary categories, per the
  task's explicit scope - not just Discretionary, even though the original runaway-divergence finding
  happened to be a Discretionary-only stress scenario.
- **Confirmed fixed**: a new harness mode (`--sustainedexploit`) re-runs the exact scenario that
  previously produced the ~6.4-quadrillion-GDP runaway (+30%/turn held on Defense/Transportation/
  Education every turn, -30%/turn on HHSDiscretionary, +15%/turn on SocialSecurity/Medicare, for the
  full 100 turns, no periodic reset). With the clamp in place, GDP instead lands around **~200,000
  -201,000 by turn 100** - comparable in scale to the periodic-stress scenario's own trajectory, not a
  divergent one - with no NaN, no negative GDP, and every pushed line landing exactly on its 3.0x
  ceiling (or 0.2x floor for HHSDiscretionary) rather than growing further, confirmed by direct
  inspection of each `SpendingLine.Amount`/`SeedAmount` ratio at turn 100.
- **A real, honestly-disclosed interaction with "Discretionary Spending Growth" (above), later found
  to have a much bigger consequence than first described here**: that earlier fix deliberately grows
  every Discretionary line at `PotentialGrowthRate` (2%/turn for USA) every turn, forever, with no
  player input at all, specifically so G would keep pace with trend GDP. That passive growth is *also*
  a repeated percentage-of-current-value change stacked over many turns - so it was *also* subject to
  this same clamp, and 1.02^56 ≈ 3.0, meaning every Discretionary line hit the 3.0x ceiling by roughly
  turn 56 purely from passive growth, even under a pure no-policy baseline with zero player input. At
  the time this section was written, this was flagged only as "the output gap widens again a bit after
  turn 56" - a follow-up investigation (see "SpendingLine Amount Ceiling - Debt-to-Zero Fix" below)
  found the consequence was far more severe than that: freezing G in absolute-dollar terms produced an
  ever-widening primary surplus (tax revenue keeps scaling with GDP; G stopped scaling entirely) that
  paid USA's `GovernmentDebt` down to exactly 0 and flatlined it there for the rest of a 100-turn
  run - not a cosmetic gap-widening, a real fiscal-outcome bug. **This has been fixed** - see that
  section for the mechanism (SeedAmount itself now grows for a Discretionary line) and for the
  additional, deeper structural driver that fix alone did not fully resolve.
- **Validation matrix** (standalone harness, combined with "Turn-1 GDP Consistency" above since both
  shipped together): 100-turn baseline (events on/off), the existing periodic-stress scenario (events
  on/off, pushes categories to their +-15%/+-30% limits every 5th turn), and the new
  `--sustainedexploit` scenario (events on/off, pushes held every turn with no reset) - six runs total,
  all numerically bounded, no NaN, no negative GDP, no negative/non-finite `SpendingLine.Amount`
  anywhere. **Explicit reliance disclosure**: before trusting these results, `SpendingLine`'s new
  `SeedAmount` field/clamp logic and the `WorldFactory` seed change were diffed side-by-side against
  the harness's ported versions to confirm identical logic, not just similarly-shaped code - the same
  standard "Percentage-Based Spending Sliders" applied after the harness's swing-threshold staleness
  was found earlier this session. **Correction to every earlier "no Unity Editor reachable" claim in
  this document** (Federal Reserve Rate Damping, Percentage-Based Spending Sliders, Turn-1 GDP
  Consistency, Per-Partner Tariff Overrides all said this): that claim was wrong - it reflected an
  incomplete filesystem search that only checked the conventional `C:\Program Files\Unity` install
  location. A later investigation (see "SpendingLine Amount Ceiling - Debt-to-Zero Fix" below) found
  Unity Editor 6000.5.4f1 actually installed at `G:\UNITY\Unity Hub\6000.5.4f1\Editor\Unity.exe`, with
  this exact project already registered in Unity Hub. Every prior "no reachable Unity Editor" statement
  in this file up to that point should be read as "wasn't found by an incomplete search," not "does not
  exist."

## SpendingLine Amount Ceiling - Debt-to-Zero Fix
A follow-up investigation (re-opening a thread the "SpendingLine Amount Ceiling" section above had
flagged but not resolved) into why USA's `DebtToGdpRatio` reaches exactly 0.00% and stays there for
extended stretches of a 100-turn no-policy baseline - the hypothesis being that Discretionary
`SpendingLine`s flatlining at the 3.0x `SeedAmount` ceiling around turn 56 breaks the "G tracks GDP"
property "Discretionary Spending Growth" relies on, letting tax revenue (which keeps scaling with GDP)
pull ever further ahead of total spending (which stops scaling once the ceiling engages) - an
ever-widening primary surplus that pays the debt down to nothing.

- **Root cause confirmed, via a per-turn fiscal trace**: a new harness diagnostic (`--debtlog`, logging
  GDP/Debt/Revenue/Discretionary-total/each line's ratio-to-seed every turn) showed exactly this
  mechanism - `SpendingLine.Amount` for every Discretionary line hit its `3.00x` `SeedAmount` ceiling
  at turn 56 and stayed there, freezing the Discretionary total at a fixed $5,253B, while `Revenue`
  kept climbing with GDP (from $5,220B at turn 1 to $35,421B at turn 100) - `GovernmentDebt` fell from
  its turn-55 value of $86,079B to exactly $0 by turn 70 and stayed flat there for the remaining 30
  turns.
- **The fix**: `SpendingLine.SeedAmount` is no longer fixed at construction for a Discretionary line -
  `SimulationManager.ApplyDiscretionarySpendingGrowth` now grows `SeedAmount` by the SAME factor as
  `Amount`'s own automatic GDP-tracking growth, every turn (a Mandatory line's `SeedAmount` is
  untouched, since Mandatory has no automatic growth mechanism to track in the first place - this fix
  is Discretionary-only). Since both figures grow by an identical factor, their ratio is unchanged
  whenever it started in `[0.2x, 3.0x]` range - a normal, un-pushed line's ratio stays exactly `1.00x`
  forever (the ceiling never engages for it, exactly reproducing pre-"SpendingLine Amount Ceiling"
  behavior), while a line a player has pushed to the ceiling stays pegged at exactly
  `MaxSpendingLineAmountRatio` times a `SeedAmount` that itself keeps compounding - so even a
  maxed-out/exploited line keeps tracking GDP instead of freezing in absolute dollar terms. Confirmed
  via `--debtlog`: under the fixed code, `DiscMaxRatio` (the highest ratio across all Discretionary
  lines) stays at `1.00x` for the entire 100-turn no-policy baseline (the ceiling never engages absent
  player action), and the Discretionary total grows continuously (`$1,786B` at turn 1 to `$12,685B` by
  turn 100) instead of freezing at `$5,253B`.
- **Result: a real, measured improvement, but the explicit "settles well above 0" bar is NOT met** -
  reported honestly, not glossed over. With the fix, USA's `GovernmentDebt` still reaches exactly $0 by
  turn 100, but the decline is far more gradual (crossing zero around turn 80, not turn 70) and the
  final approach is no longer a near-instant 14-turn wipeout (turn 56→70 previously) but a steadier
  ~25-turn decline (turn ~55→80). This confirms the named hypothesis was a REAL, CONFIRMED contributing
  cause, worth fixing on its own merits (a maxed-out spending line should track GDP, not freeze) - but
  it is not the ONLY driver of the debt-to-zero outcome.
- **A second, previously-unidentified structural driver, found while chasing the remaining gap**:
  Mandatory `SpendingLine`s (`SocialSecurity`, `Medicare`, `Medicaid`, `IncomeSecurity`,
  `VeteransBenefitsMandatory`, `FederalRetirement`, summing to a fixed $4,010B) have **no automatic
  growth mechanism at all** - `ApplyDiscretionarySpendingGrowth` only ever touched Discretionary lines,
  by design (Mandatory categories were only ever meant to change via the player's own
  `PolicyDecision.SpendingLineChanges`, at their narrower +-15% range - see "Percentage-Based Spending
  Sliders"). Left untouched over a 100-turn run, $4,010B of spending against an ever-growing GDP
  shrinks from 13.8% of GDP at turn 1 to under 2% of GDP by turn 100 - a purely structural, guaranteed
  improvement in the primary balance over a long enough game, independent of the Discretionary-ceiling
  bug above and unrelated to any specific ceiling/clamp mechanism.
- **A full calibration sweep was run (not shipped - the result is a negative finding, not a rate to
  ship)**: a harness-only knob (`MandatoryGrowthRatePercent`, never ported to the real files) grows
  Mandatory `SpendingLine`s at a configurable constant rate, mirroring `ApplyDiscretionarySpendingGrowth`
  exactly (SeedAmount grows in lockstep, same reasoning as the Discretionary fix above). Sweeping this
  rate from 0% to 5%/turn, at BOTH a 100-turn and a much longer 500-turn horizon (a new `--turns=`
  harness flag), found that **every rate tested converges to one of exactly two attractors - 0% or
  ~294.1% `DebtToGdpRatio` - never anything stable in between**, and which attractor a given rate
  reaches depends on the time horizon, not just the rate itself:
  - At the 100-turn horizon, rates up to ~0.5%/turn still reach 0% within the window; 0.6-0.9%/turn
    land at plausible-looking intermediate values (e.g. 91.3% at 0.75%) that LOOK stable at turn 100
    but are not - a 300-turn re-run at those same rates shows them still declining toward 0%, just more
    slowly; 1.25%+/turn reaches ~294.1% by turn ~30-100.
  - At a 500-turn horizon, the picture changes again: rates up to 1.3%/turn now ALSO settle at exactly
    0% (including several that looked "high and stable" at 100 turns), while 2%/turn+ genuinely holds at
    294.1% indefinitely (confirmed stable, not still declining).
  - **Conclusion: there is no constant Mandatory growth rate that produces a genuine, lasting
    equilibrium comfortably between 0% and the 300% ceiling.** The debt-to-GDP dynamics under this
    model appear to be genuinely BISTABLE, not continuously tunable - unlike Discretionary spending
    (where `PotentialGrowthRate` is "the unique fixed point" for the OUTPUT-GAP/GDP-identity balance,
    a different, apparently well-behaved balance), the fiscal/debt balance here has no interior stable
    fixed point at all for this lever. The likely mechanism (not confirmed by further investigation,
    which was out of this task's scope): `SimulationManager.GetInterestOnDebt`'s risk premium scales
    with `DebtToGdpRatio` itself (`DebtRiskPremiumRate` per point above `RiskFreeDebtToGdpPercent`),
    creating a self-reinforcing loop - higher debt raises the premium, which raises interest cost,
    which raises the deficit, which raises debt further - a classic unstable-interior/two-stable-
    extremes dynamical signature, which no amount of retuning a SEPARATE lever (Mandatory's growth
    rate) can fix, since it doesn't touch the loop itself.
  - **Nothing from this sweep was shipped to the real files** - `SimulationManager.cs`'s Mandatory
    lines are unchanged from before this investigation (still no automatic growth), since every tested
    rate either still eventually reaches 0% (not meeting the task's bar) or reaches a near-ceiling 294%
    (arguably a worse outcome, not a fix). Genuinely resolving this would mean addressing the
    interest/risk-premium feedback loop directly (e.g. damping how strongly the premium responds to
    `DebtToGdpRatio`, or capping how far debt can compound the premium against itself) - a materially
    different, bigger investigation than "find the right Mandatory growth rate," and one this task did
    not undertake.
- **A genuinely new capability found mid-investigation: real Unity Editor access**. Every earlier
  section this session that discussed validation limits stated flatly that no Unity Editor was
  reachable in this environment. That was based on an incomplete search (checked only
  `C:\Program Files\Unity`-style locations). This investigation found Unity Hub's own records
  (`%APPDATA%\UnityHub\projects-v1.json`) showing this exact project registered against Unity
  `6000.5.4f1`, installed at `G:\UNITY\Unity Hub\6000.5.4f1\Editor\Unity.exe` - a real, working
  install, just on a drive/path this session's earlier searches never checked. A new Editor-only
  script, `Assets/Editor/BatchSimulationRunner.cs`, was added so `SimulationTestRunner` can be run
  headlessly from the command line going forward
  (`Unity.exe -batchmode -nographics -projectPath <path> -executeMethod
  PoliSim.EditorTools.BatchSimulationRunner.Run -logFile <path>`) instead of requiring someone to
  manually press Play in the Editor - it opens `SampleScene`, enters Play mode, waits ~15 frames (more
  than enough for `SimulationTestRunner.Start()`'s single-frame 100-turn loop to finish and log its
  summary), then exits.
- **Clean, deliberately-triggered confirmation obtained**: this project was initially found already
  open in three separate, pre-existing Unity Editor processes (not started by this session), which
  blocked this session's own first `-executeMethod` attempt outright (Unity refuses a second instance
  against an already-open project - confirmed: that attempt's dedicated log shows "Exiting without the
  bug reporter... return code 1" within the same second it started, before any script compilation).
  Before that was resolved, the project's own `Logs/Editor.log` already showed extensive genuine
  `SimulationTestRunner` activity from those pre-existing windows (almost certainly manual Play-mode
  testing during this session), including a turn-100 USA result closely matching the harness - useful
  corroboration, but not a run this session could point to with full certainty about its exact code
  version. The three pre-existing processes were subsequently closed (with explicit sign-off), and
  `BatchSimulationRunner.Run` was re-invoked cleanly against the current code with no other Unity
  instance competing for the project - **this run completed correctly and is the authoritative
  confirmation**: `Turn 100 | United States: GDP=206734.6 (+2.00%), Unemployment=4.00%,
  Inflation=2.45%, InterestRate=0.00%, GovernmentDebt=0.0, DebtToGdpRatio=0.0%`, with `65` anomalies
  detected over the 100-turn run. This GDP figure matches the harness's own current-code run
  (`206734.0`) to five significant figures, and the anomaly count sits squarely inside the harness's
  own events-enabled range (53-71 across several runs) - real Unity and the harness agree, and **real
  Unity directly confirms the debt-to-zero outcome is not resolved**: `DebtToGdpRatio` is exactly
  `0.0%`, not "well above 0," at turn 100 of a no-policy baseline, exactly matching what the harness
  investigation above found and matching the explicit validation bar this task set that was NOT met.
  Also observed, but out of scope for this task and not investigated further: in the same log (and reproduced in the
  harness's own events-enabled run), Sweden/Germany/France/Italy/Poland - none of which have a
  `SpendingLines` portfolio, and none of which are touched by anything in this fix - ALSO show
  `DebtToGdpRatio` at either exactly 0.0% or pinned near the 300% ceiling by turn 100 under
  events-enabled runs. This suggests debt-to-GDP bimodality (settling at one hard clamp or the other
  over a long enough run) may be a broader, pre-existing characteristic of the debt-clamping/reversion
  system generally, not specific to USA's `SpendingLines`/`SeedAmount` mechanism - a real, separate
  lead for a future investigation, not chased down here. **This lead was the correct one - see "Fiscal
  Reaction Function" below**, which found and fixed the missing piece: the debt-to-GDP system had no
  negative feedback at all, for any of the six countries, independent of `SpendingLines`/`SeedAmount`.

## Fiscal Reaction Function
Closes the bimodal debt-to-GDP finding from "SpendingLine Amount Ceiling - Debt-to-Zero Fix" above by
adding the missing piece that investigation's own "separate lead" flagged but didn't chase down: every
country's `DebtToGdpRatio` - not just USA's - was found to settle at either exactly 0% or pinned near
the 300% ceiling given a long enough no-policy run, with nothing stable in between, however
Discretionary/Mandatory spending growth was tuned. The missing ingredient was a NEGATIVE FEEDBACK on
the government's own fiscal behavior - real governments tighten (raise revenue/cut spending) as debt
becomes uncomfortable and loosen as it becomes ample; this game had no such mechanism at all, only
`GetDebtRiskPremium` (the market's side - lenders charging more to an already-indebted borrower, which
is itself a POSITIVE feedback once in motion: more debt -> more premium -> more interest cost -> more
debt).

- **`Country.ComfortableDebtToGdpPercent`**: a new per-country structural constant - the debt-to-GDP
  level at which the reaction is neutral. Seeded in `WorldFactory` to each country's own already-seeded
  starting debt-to-GDP ratio (USA 124, Germany 63, France 116, Italy 138, Poland 59, Sweden 35) -
  reusing an already-researched figure rather than inventing a new one, on the reasoning that a
  country's own fiscal history is a reasonable proxy for what level it's institutionally built to run.
- **`SimulationManager.GetFiscalReactionMultiplier`**: `1 + FiscalReactionSensitivity * (DebtToGdpRatio
  - ComfortableDebtToGdpPercent) / 100`, clamped to `[MinFiscalReactionMultiplier,
  MaxFiscalReactionMultiplier]`. Applied as an extra multiplier on top of `CollectionEfficiency` in
  `ApplyRevenueAndSpending` (`actualRevenue = theoreticalRevenue * CollectionEfficiency *
  GetFiscalReactionMultiplier(country)`) - above the comfort level, effective revenue rises (tightening);
  below it, effective revenue falls (loosening). This is explicitly NOT a substitute for
  `GetDebtRiskPremium`, which stays completely unchanged (it represents the market's own, separate cost
  of lending more to an indebted borrower) - this is the government's own countercyclical response,
  layered on top.
- **Calibration - "modest" wasn't sufficient, and the reason why is itself informative**: swept
  `FiscalReactionSensitivity` from 0.05 to 5.0 in the standalone harness (both 100- and 500-turn no-policy
  baselines, `--noevents`, all six countries). Every value from 0.05 to 0.3 (a single-digit-percent
  revenue swing at realistic debt-gap magnitudes) failed to escape the pre-existing 0%/294% bimodality -
  the underlying risk-premium-driven positive feedback loop is apparently strong enough that a truly
  "modest" (small-magnitude) countervailing force can't out-compete it. **1.0-2.0, with multiplier bounds
  widened to `[0.5, 1.5]` (a 2x range), is what actually stabilizes the system**: every one of the six
  countries settles at a distinct, moderate, country-appropriate `DebtToGdpRatio` - e.g. at 1.5 (the
  shipped value): USA ~142%, Sweden ~13%, Germany ~35%, France ~90%, Italy ~107%, Poland ~26% - none of
  them at either extreme. Confirmed as a GENUINE equilibrium, not a slower transient toward one of the
  old attractors: identical to four significant figures at turn 500, turn 1000, and turn 2000 in the
  harness, and robust across multiple different random-event seeds (events enabled, three separate runs
  landed within 0.2 percentage points of each other for every country). `FiscalReactionSensitivity = 1.5`
  and bounds `[0.5, 1.5]` were shipped to the real files as the calibrated values - a "modest" magnitude
  in the sense of being a smooth, gradual, always-on background force (never a player-visible lever,
  never a single-turn shock), but not modest in absolute size, since the empirical sweep showed anything
  smaller simply doesn't work.
- **Mandatory spending also grows automatically now** (`SimulationManager.ApplyMandatorySpendingGrowth`,
  mirroring `ApplyDiscretionarySpendingGrowth` exactly - same `PotentialGrowthRate`, same lockstep
  `SeedAmount` growth so `MaxSpendingLineAmountRatio`'s ceiling tracks GDP for Mandatory lines too): real
  entitlement spending (Social Security, Medicare, Medicaid, etc.) grows with the economy in reality too
  - demographics and healthcare-cost growth don't pause just because a program is Mandatory rather than
  Discretionary - and the previous complete freeze was a second, independent contributor to the
  bimodality. **This alone was tried in isolation during the debt-to-zero investigation and found to
  overshoot badly** (pegged near 294% within ~30 turns) - it only became viable paired with the fiscal
  reaction function's negative feedback above, which is why both ship together, not as two independently
  toggleable fixes.
- **A real, honestly-flagged trade-off - policy-driven extremes still reach 0%/300%, by design**: the
  reaction function stabilizes the NO-POLICY baseline; it does not (and is not intended to) make 0%/300%
  unreachable under deliberate player policy. Confirmed in the full validation matrix below - the
  `stress` scenario's aggressive repeated tax hikes still drive USA's debt to exactly 0% by late-game,
  and the `sustainedexploit` scenario's sustained max-spending pushes still peg it at 294% - both
  adversarial, deliberately extreme policy sequences, not baselines. This is the correct, expected
  behavior: a player choosing sustained austerity or sustained deficit spending should still be able to
  reach the historical extremes through their own choices; the fix targets the baseline's structural
  bimodality, not the existence of the clamps themselves.
- **Validation - the full matrix, confirmed directly in real Unity** (see "Real-Unity Validation is the
  Standard Path" below for why this is now the primary check, not the harness): `Assets/Scripts/Testing/
  SimulationTestRunner.cs` was extended with a `-runmatrix` command-line flag that runs baseline, stress,
  sustainedexploit, and tariffoverride at both 100 and 500 turns (8 combinations, ported byte-for-byte
  from the standalone harness's own same-named scenarios) in a single Play session, and
  `BatchSimulationRunner.Run` was invoked with it directly against the real Unity Editor
  (`6000.5.4f1`). Results, read straight from that run's own log (not the harness): at both 100 and 500
  turns, baseline and tariffoverride (which barely perturbs USA's own fiscal balance) land USA at ~142%,
  essentially flat between the two horizons (142.4% -> 143.6%/142.6%) confirming genuine stability, not a
  slow drift; Sweden/Germany/France/Italy/Poland land at ~13%/~35%/~90%/~107%/~26% respectively at BOTH
  horizons, matching the harness closely. The stress and sustainedexploit scenarios show USA at the two
  historical extremes (0% and 294% respectively) exactly as expected for deliberately adversarial policy
  (see the trade-off bullet above) - not a bug. Zero NaN/Infinity/negative-value anomalies anywhere
  across all 8 combinations; anomaly counts (34-157, higher for the 500-turn runs simply because there
  are more turns to accumulate them over) are all attributable to ordinary swing/misery-index noise, not
  runaway divergence.

## Continuous Time Phase 3 — the money resolution goes daily, and the one constant that is a decision rather than a flow (2026-08-03)

The fiscal engine's conversion to daily granularity. Structure first, then the finding, which is the part
worth carrying into Phases 4 and 5.

**The split.** `SimulationManager.AccrueDailyFiscalFlows` runs once per country per day inside
`AdvanceDay` and charges `1/DaysPerTurn` of every flow: tax revenue, unemployment benefits, welfare cost,
interest on debt, the SWF's contribution/return/draw, and the debt stock update that follows from them.
The BUDGET RESOLUTION stays on the turn boundary — `ResolveSpendingForTurn` and every policy application
around it are unchanged — because a budget passing is an event on a date, not a flow. The consequence is
that a plan is resolved at one boundary and executed across the following 121 days, which is both what a
budget *is* and the only causally possible ordering: the boundary that resolves a budget is 121 days of
play before the next one. **A policy change's cash effect therefore lands one period after the boundary
that made it**; its effect on the GDP identity's G term does not move, since `ApplyNationalAccounts` is
Phase 5 and still reads the plan at the boundary that resolved it.

The state this required is `SimulationManager.FiscalPeriod`, one per country, holding the plan the current
days are executing plus a running sum of what they have accrued (closed out into `FiscalTurnReport` at the
next boundary, so the report is now a genuine sum of 121 days rather than one step's figure). It exists
because most of the daily step CAN be re-derived from persistent country state but government/mandatory
spending cannot: for the five countries without a detailed `SpendingLines` portfolio the discretionary
figure comes from `PolicyDecision.TotalDiscretionarySpending`, which exists only at a boundary. The period
is seeded on first use — day 1 arrives 121 days before the first `AdvanceTurn`, and without a seed every
country would spend nothing for its opening third of a year — derived directly from the seeded portfolio
and deliberately NOT by calling `ResolveSpendingForTurn`, which is not idempotent.

`SovereignWealthFundSystem.ApplyReturns` became `DrawPeriodReturn` and no longer mutates the fund: the
return is drawn ONCE per period at the boundary and accrued daily. Drawing daily would consume 121x the
RNG and invalidate every recorded baseline for no modelling gain.

### The finding: a constant can be a DECISION rather than a FLOW, and decisions do not divide by 121

The migration methodology asks which mathematical shape a constant is — linear, multiplicative,
probability, or a clamp that does not shrink at all. Phase 3 found a fifth answer, and found it the
expensive way.

Every fiscal flow took the linear shape correctly and is exact by construction. But `ApplyRevenueAndSpending`
also applies `GetFiscalReactionMultiplier`, and the first implementation recomputed it every day — the
obvious "more continuous" choice, since it reads `DebtToGdpRatio` and the debt ratio now moves daily.
**It failed the aggregation bar outright: Sweden 24.8% drift on budget balance, Germany 22.7%, against a
3% bar** (`GovernmentDebt` 13.6% and 7.0%). The cause is not a coding error. `FiscalReactionSensitivity`
is 1.5 and a single period moves a country's debt ratio by ten points or more, so a multiplier that
re-reads that ratio every morning walks a long way down its own surplus over the period it is supposed to
be governing. The turn form structurally could not do this — its debt stock moved exactly once — so daily
recomputation was not a finer version of the validated model. It was a different model.

Freezing it for the period passes at 0.45%/1.35%, and the argument for freezing is not that it passes.
`GetFiscalReactionMultiplier`'s own doc comment describes "a country's own government modestly tightens…
as debt rises above its `ComfortableDebtToGdpPercent` anchor" — that is a fiscal STANCE, and a stance is
adopted when the budget is set, not re-derived daily. It still responds fully to the debt the country
actually accumulated; it does so at the boundary, where every other budget decision in this game is made.
`FiscalPeriod.PlannedFiscalReactionMultiplier` holds it, and `ApplyRevenueAndSpending` gained a `-1`
sentinel override (this file's existing idiom) so the turn form's remaining caller, `PreviewTurn`, reads
exactly as it did.

**The generalisable question, for Phases 4 and 5:** is this constant a quantity that *flows*, or a
decision that was *taken*? Only the first kind divides by 121. Phase 5's macro engine is full of the
second kind.

### Interest is the one deliberate difference from the turn form

Interest stayed daily, and is why the residual is 1.35% rather than zero: it is charged on the debt the
country holds today rather than the debt it held four months ago, so a country running down its debt pays
slightly less over a period than the turn form charged it. That is a real modelling gain, not an error,
and it is the whole of the remaining drift — every country's 120-turn debt lands 1–5% lower, all in the
same direction.

### Validation

⚠ **CORRECTED 2026-08-10 — this section overstated what it had established.** As originally written it
presented "39/39 within 3%" as validating the fiscal engine. **It validated the MIGRATION, not the
engine.** The daily form was verified to reproduce the turn form, and it does, exactly — while the turn
form was charging a full year of spending, revenue and interest every 121 days. Every number below is
still true and still correctly measured; what was wrong was the conclusion drawn from them. Read this
section as "the conversion is faithful", never as "the fiscal engine is right". See "A turn is now a
year" below, and the verification-integrity note in the working discipline.

Aggregation-equivalence 39/39 within 3%. Full matrix (15 scenarios x 100/500 turns) run before and after
under the same seed against real Unity `6000.5.6f1`: **25 of 30 combinations byte-identical**, 1629 → 1637
anomalies total, and the only categories that moved are the two directly downstream of the debt path
(DebtToGdp swings 139 → 145, credit-rating notch moves 18 → 20). Inflation (1257), Unemployment (123) and
InterestRate (92) are unchanged to the anomaly — read that with the file's own opening caveat about what
an anomaly count does and does not cover. `DebtClampDiagnostic`: zero ceiling hits, zero negative-debt
turns and zero runaway-guard hits, before and after. `CreditRatingAnchorCheck`: unchanged at 5/6, Poland's
known expected failure.

One tool is now slightly approximate and it is worth knowing: `DebtClampDiagnostic` reconstructs an
"unclamped" debt as `previousDebt - budgetBalance` to detect clamp hits. With interest compounding within
a period that reconstruction is no longer exact. It is a diagnostic rather than a gate, and its clamp-hit
counts were zero in both runs, so nothing rests on it today.

## Verification integrity: an equivalence check is not a correctness check

**A new variant, and the most dangerous one this project has produced so far** — because unlike every
previous verification failure here, **nothing was broken.**

The catalogue up to now had two shapes. A **broken check**: one that cannot fail, or measures the
harness instead of the thing (`WaitForEndOfFrame` under `-batchmode`; the render-order spike where
neither canvas drew; the guard that caught `UnityException` while Unity threw `ArgumentException`). And
an **overestimated check**: one that runs correctly but is read as covering more than it does
(`DeliveredAssetCheck` answering "did the file land under `Assets/`?" and being read as "can the game
load it?").

This is neither. **Phase 3's aggregation bar covered spending, passed, and was RIGHT to pass.**

> *"Its flows are exact by construction (121 × flow/121)"* — the check's own doc comment.

That identity holds **whether or not the annual figure was the right figure.** The check asks "does the
daily form reproduce the turn form?" It got the right answer. A faithful migration reproduces an error
perfectly, and faithfulness was precisely what it was built to establish.

⚠ **The gap was the assertion NEXT TO it that nobody wrote.** Nothing in this project has ever asserted
that a turn's fiscal flows equal a turn's worth of money. That assertion did not exist, so it could not
fail, so nothing drew attention to its absence — and the check sitting beside it looked like it covered
the ground, because it covered *adjacent* ground convincingly.

**The generalisation, and it is the useful part:** a correct, passing check creates a *shadow* — the
region a reader assumes is covered because something nearby is. A broken check gets found when it lets a
bug through. An overestimated check gets found when someone reads its scope. **A correct check's shadow
is found only by asking, explicitly, what it does NOT assert** — and that question gets asked least
often precisely when the check is green and well-written.

Practically: **when a check passes, write down what it did not test**, in the same breath and the same
place. Phase 3's write-up in this file said "39/39 within 3%" and drew the conclusion "the fiscal engine
is validated". Had it said "the conversion is faithful; nothing here checks whether the turn form was
right", the 3.017x error would have been visible in the sentence that shipped it.

## The overflow assert, and what it found in its first run (2026-08-10)

`UiOverflowGuard` hooks `PoliSimWidgets.MeasuredLabel` — the choke point for every shrink-to-fit label
in the UI. It records anything still too wide after shrinking has hit the 8px floor, and the capture
driver now **exits 1** on any distinct violation. Editor-only; compiled out of player builds.

**Why it was worth building.** Clipping has recurred eleven times and every instance was found by a
person, never by a check. Fifty-five screens have been captured and approved by eye with overflows in
them — eye-approval is precisely what has been missing them.

⚠ **The guard's own first version crashed the Editor.** It appended a violation per label *per frame*,
so a few hundred settle frames became an unbounded list. Dedupe now happens at RECORD time with a
200-entry hard cap. A guard that brings down the run it is guarding reads as flakiness, which is worse
than no guard.

**First run: 55 captured, 0 failed, 200 overflows (cap reached).** Almost all money figures in
`LedgerRow`'s figure column, e.g. `"$1.62T" needs 22.9 in 11.3 at 8px`. The suspected cause was the
trailing-column change of the same day raising `fixedTotal` into `Columns()`'s 0.35-floor squeeze.

**RESOLVED 2026-08-11, and the suspect was innocent — see the next section. Every one of those 200
was a false positive, including both figures quoted above.**

## 188 commits existed on exactly one disk, and every check built that day pointed at sprites (2026-08-11)

**The largest risk carried all day was not in the code.** `git status` showed `## main...origin/main
[gone]`, which was read as *the remote is gone* and recorded as *"concurrent writers with no remote means
a lost update has no recovery path at all."* Both halves of that were wrong in opposite directions, and
the truth was worse than the diagnosis:

- `origin` was **reachable the whole time** and had `main` at `1ee5fce`. What was missing was the LOCAL
  TRACKING REF, not the remote.
- Local was **188 ahead, 0 behind** — every commit since `1ee5fce` existed on one disk, in one
  working tree, that a second agent was concurrently writing to. `git push` was a clean fast-forward and
  had been available at any point.

⚠ **A missing local tracking ref and a genuinely missing remote are indistinguishable from inside the
repository**, and `[gone]` reads as the second. One `git ls-remote` separates them; nobody ran it for 188
commits.

**This is rule 14 pointed at the repo instead of at a sprite.** That day built four checks — coverage,
containment, importer settings, party marks — every one of which asks *does this asset exist and is it
correct*, and not one of which asks *does this work exist anywhere but here*. The enumeration gap was not
inside any check; it was the population none of them was over. A backup is a check whose scope is the
work itself, and it was the only one missing.

**Standing consequence:** `git status -sb` is not a backup check. Confirm the remote is reachable and the
branch is pushed, by fetching, not by reading a tracking line that goes stale silently.

## A next-steps marker is a claim like any other, and goes stale the same way (2026-08-11)

Item 9 §A's **"⚠ NEXT SESSION STARTS HERE"** block said Budget was the only converted screen and offered
*"Statistics, Politics/Parliament and Policy/Laws"* as the candidates for next. **Policy/Laws had been
converted for a day** — `ba2c3c8` Labor Market, `1589008` Crime & Justice and Sectors, `665e0a8` Trade.
The marker was offering a finished screen as work.

⚠ **This is worse than an ordinary stale status, and the difference is worth naming.** A stale claim in
the body of a document misleads whoever reads that paragraph. **A stale next-steps marker is the FIRST
thing read each session, so it misdirects the pass before anything else runs** — a pointer that outlived
its target, rather than a claim that outlived its evidence.

**The fix is not "remember to update it".** The marker now carries a line saying it is **derived, not
narrated**, and states its derivation: `grep LedgerRow.Draw` over `GameController`, plus the commit
history. Re-derive it at the start of a pass; do not edit it forward. The derived table found the true
state in one command — the same move as re-deriving asset status from the filesystem under rule 12,
applied to a plan instead of to a delivery.

## A helper is not evidence its arithmetic is complete (2026-08-11)

`PoliSimWidgets.InnerWidth` was built to end a subtraction that had been forgotten at four sites and
hand-rolled at two. It shipped with **three of its four terms**, and the missing one —
`container.margin.horizontal` — was wrong from the moment it was written, at every call site, for a full
day, while four sites were presumed correct *because they now went through the helper.*

⚠ **What made it invisible is that it had a name.** A raw subtraction invites checking; a call to
`InnerWidth(availableWidth, _boxStyle)` reads as a solved case, and nobody re-derives a solved case.
**Exactly the reference-class `.meta` trap one layer up**: "copy the reference meta" and "call the helper"
both name a thing that looks decided.

**What caught it: reviewing a SECOND implementation of the same idea.** `InnerHeight` arrived from
another session with a fourth term already in it, and the only reason to look was that adopting foreign
code demands a review that one's own does not. **Extracting a twin is the cheapest audit a helper ever
gets** — and the twin here found a defect in the original.

## The accessor pattern closed the label-clipping class where seven site-specific fixes did not (2026-08-11)

The class's signature is **a constant standing in for measured content**. Seven site-specific fixes did
not end it. Three accessors did, each replacing one constant, each read by BOTH the reserve and the
drawing so the two cannot disagree:

| Constant | Replaced by | Measures |
|---|---|---|
| `_labelStyle.fontSize + 8f + _buttonStyle.fixedHeight` | `CalendarAndSpeedControlsHeight` | the real status string, at the width it wraps into, in the style it renders in |
| `_tabButtonStyle.fixedHeight` | `ConsolidatedTabRowHeight` | the larger of the base height and the stacked icon+label, plus row margin |
| `_labelStyle.fontSize * 7f + _headerStyle.fontSize + 16f` | `BudgetProcessHeaderHeight` | all five pieces drawn above the columns row |

**Instance #12, measured against a fresh capture at every step:**

| | Commit | L | T | R | B | clipped |
|---|---|---|---|---|---|---|
| before | — | 0 | 0 | **841** | **663** | 54 |
| `InnerWidth` 4th term + tab margins | `b42ff20` | 0 | 0 | 0 | **663** | 54 |
| two accessors | `8a476bf` | 0 | 0 | 0 | 0 / **1508** | 16 |
| `BudgetProcessHeaderHeight` | `ea612d9` | 0 | 0 | 0 | 0 | **0** |

Confirmed at **1600×929 and 2560×1419**. This is the same discipline `UiContainmentGuard`'s doc names for
`StatTile`: one measurement, two readers.

## You cannot measure what is not a value (2026-08-11)

`headerAllowance` was a constant **because it had to be**: the three strings above the Budget columns row
existed only as arguments to `GUILayout.Label`, and a string that exists only at its draw site cannot be
measured by anything. The accessor was impossible until `BudgetProcessDescription` became a const and
`BuildFullScreenInterruptText` / `BuildBudgetBillStatusText` became methods.

⚠ **So "build it so it can be measured" is step ONE of the accessor pattern, not an incidental refactor
that happened alongside it** — and it will recur at every remaining site, because every remaining
constant-standing-in-for-content is standing in for something that is not yet a value. `BuildTimeStatusText`
was the same move one screen earlier. Expect the split before expecting the accessor.

## A measurement is only comparable to another taken at the same horizon (2026-08-11)

**Debt-to-GDP, seed 777, real Unity, `-runmatrix`, measured at three horizons:**

| | baseline (2026-07-22, 100/500) | 100 | 500 | **1000** |
|---|---|---|---|---|
| USA | ~142–143% | 138.2 | 143.0 | **154.7** |
| Sweden | ~13% | 4.6 | 5.7 | **11.2** |
| Germany | ~35% | 37.8 | 45.4 | **80.4** |
| France | ~90% | 93.8 | 92.4 | **108.8** |
| Italy | ~107% | 113.9 | 125.8 | **165.6** |
| Poland | ~26% | 27.7 | 30.1 | **45.6** |

⚠ **NOT ONE OF THESE IS AN EQUILIBRIUM. All six are still climbing at turn 1000**, several steeply —
Germany nearly doubles between 500 and 1000, Italy adds 40 points. **The figures recorded on 2026-07-22
as "Fiscal Reaction Function equilibria" are waypoints on a rising path**, and they have been quoted as
equilibria in this file and the roadmap ever since.

**How the error compounded, three times in one day, each time by comparing across horizons:**
1. A **120-turn** `DebtClampDiagnostic` snapshot was compared against **100/500-turn** baselines, and
   produced four wrong conclusions — USA "−4.3", France "+2.9", Germany "+2.9", Poland "+1.7", all
   artifacts of the horizon mismatch.
2. Corrected to a like-for-like 100/500 matrix, which said **"USA and France did not move, three others
   are climbing"** — also wrong. They had not settled either; they merely passed near their baselines at
   that horizon.
3. At 1000 turns, **every** country is climbing. The distinction between "moved" and "did not move"
   never existed.

**The rule: a number is comparable only to one taken at the same horizon, and a number at the last turn
measured is a WAYPOINT until a longer run says otherwise.** Record horizon and seed beside every figure.
⚠ **And never write "equilibrium" for a value that has not been shown to stop changing** — the word
asserts a property that only a longer run can establish, and once written it is quoted as though it had
been.

### ✅ ATTRIBUTED 2026-08-11 — pre-fix matrix run at `4de6a1e`, and the answer is PER COUNTRY

Same seed, same 15 scenarios, real Unity, all three horizons, on the commit **before** `0386e83`
(verified by the old form on the checked-out tree: `theoreticalRevenue * efficiency * multiplier +
swfReturns` — returns added AFTER the multiplier).

| | pre 100 | pre 500 | **pre 1000** | post 1000 |
|---|---|---|---|---|
| USA | 142.4 | 145.8 | **157.3** | 154.7 |
| Germany | 38.5 | 45.7 | **81.3** | 80.4 |
| Italy | 115.2 | 126.3 | **166.3** | 165.6 |
| Poland | 29.9 | 32.3 | **48.9** | 45.6 |
| **Sweden** | **−296.1** | **−287.9** | **−297.0** | +11.2 |
| **France** | −69.1 | **−296.7** | **−299.0** | +108.8 |

**Four countries diverge IDENTICALLY before and after.** USA, Germany, Italy and Poland climb on the same
trajectory to within ~3 points at turn 1000. **`0386e83` is innocent for all four.** The Fiscal Reaction
Function has never equilibrated for them — the 2026-07-22 figures were waypoints on the day they were
recorded, and every "equilibrium" quoted since has been a waypoint quoted as a resting place.

⚠ **THE DEFECT IS OLDER AND LARGER THAN THIS SESSION'S WORK, and Italy needs no separate explanation** —
its "+7.0" was never attributable to the SWF change, and neither was anything else.

**Two countries were PINNED AT THE −300% BOUND pre-fix.** Sweden sits at −296/−288/−297 and France
reaches −299 — exactly the pinning Elias's 2026-08-02 ruling named as its first reason (*"France at −298%
against a −300% bound is not a risk, it is already pinning"*). Post-fix they are at +11.2 and +108.8:
real, unpinned values. **`0386e83` plus the bound removal did precisely what it was ruled to do**, and
Sweden's ~13% baseline does not reproduce pre-fix either, so it was never the number to defend.

### ✅ THE DIAGNOSIS, read from the code 2026-08-11 — SATURATION, not a missing mechanism

⚠ **THE HYPOTHESIS THIS DISPROVES, recorded because Elias raised it and it was reasonable:** *"the
missing piece is a debt-level term in the primary balance response — real sovereigns run primary
surpluses when debt is high, and if that isn't in there, that single absence explains everything."*
**It is in there.** `GetFiscalReactionMultiplier`:

```csharp
float debtGap = country.State.DebtToGdpRatio - country.ComfortableDebtToGdpPercent;
float multiplier = 1f + FiscalReactionSensitivity * debtGap / 100f;   // sensitivity 1.5
return Mathf.Clamp(multiplier, 0.5f, 1.5f);
```

`DebtToGdpRatio` is the **STOCK**, measured against each country's own comfort anchor, and the sign is
correct. **The restoring force exists and responds to exactly the quantity it should.**

**THE DEFECT IS SATURATION.** The multiplier hard-clamps at **1.5**, reached once debt is 33.3 points
above comfortable — and **1.5× effective revenue cannot cover interest running at 45.7% of all
spending**. The stabiliser does not fail to respond; it responds, maxes out, and the gap keeps widening.

⚠ **CORRECTION to this file's own earlier line, "the multiplier moves freely, not pinned."** True for
Germany at ~1.27. **Italy at 1.446 is effectively saturated already** — it is the saturated case, not a
country on the way to becoming one.

**TWO FEEDBACKS, ASYMMETRICALLY BOUNDED — and that asymmetry is the mechanism.** `GetDebtRiskPremium`
already responds to the debt stock (`excessDebtToGdp` above `RiskFreeDebtToGdpPercent`) and already
reaches the live interest path via `effectiveRate = baseRate + premium * RiskPremiumSensitivity`. So the
model contains a **positive** feedback (more debt → higher r → more interest → more debt) bounded only by
`MaxDebtRiskPremium`, running against a **negative** feedback capped at 1.5. Both are present, both are
correctly signed, and only one of them is tightly bounded.

⚠ **A debt-responsive rate is therefore NOT an available fix — it is already there and is half the
problem.** Adding one would sharpen the trap.

**NO PRIMARY BALANCE EXISTS IN THE MODEL.** `budgetBalance = actualRevenue - totalSpending`, and
`totalSpending` includes `interestOnDebt` as one of six terms. **Candidate examined and rejected on
evidence:** Italy's headline −3.9 with 45.7% of spending on interest implies a primary surplus of roughly
**+75** in the same units. Italy is already doing everything a primary-balance response would ask of it,
so adding that term would change nothing for the country that most needs it.

### ✅ THE DRIVER, INSTRUMENTED 2026-08-11 — INTEREST COMPOUNDING, not a pinned stabiliser

One 1000-turn baseline, seed 777, real Unity, logging `fiscalReactionMultiplier`, `interestOnDebt`,
`budgetBalance`, debt and GDP per accrual for **Germany and Italy** — one climbing from a low base, one
from an already-high one.

| | multiplier (bounds 0.5–1.5) | interest as % of ALL spending |
|---|---|---|
| **Germany** early → late | 1.000 → 0.624 → **~1.27** | 6.3% → **25.7%** |
| **Italy** early → late | 1.000 → 0.624 → **~1.19–1.45** | 20.5% → **45.7%** |

**The stabiliser is NOT pinned.** It moves freely across the run — down to 0.62 while debt falls early,
then back up above 1.0 as debt rises, sitting at ~1.27 (Germany) and ~1.19–1.45 (Italy) against a 1.5
cap. **It is leaning hard in the correct direction and is simply being outrun.**

⚠ **Interest goes from 6% to 26% of all spending in Germany, and from 20% to 46% in Italy.** By turn 1000
Italy spends nearly half its budget servicing debt. The daily balance is only slightly negative
(−0.9 / −3.9), so this is not profligacy — it is compounding on a stock the stabiliser can slow but not
reverse.

**Same mechanism in both, which is the stronger result**: Italy is simply further along, and its
multiplier at 1.446 is approaching the 1.5 cap. **Italy is on a path to become the pinned case, without
being it yet at turn 1000** — so the two hypotheses are not alternatives but stages, and a fix aimed only
at the cap would arrive after the damage.

## An absence claim greped from ONE FILE is rule 14 inverted (2026-08-11)

⚠ **THIS ENTRY ORIGINALLY READ "A ruling with an unbuilt half is worse than an open question", and its
worked example was false.** Corrected the same day, in place, with the original claim quoted rather than
deleted — the discipline rule 10 requires for a reversal, applied to a lesson.

**The false claim:** that the 2026-08-02 net-creditor ruling *"shipped half"* — the 1000% guard built,
the cause-fix never written.

**What is actually true: `0386e83` shipped BOTH candidate fixes**, and a future reader who finds one
should not assume the other was declined:
1. **Returns run INSIDE the fiscal reaction multiplier.** `SimulationManager.cs:2682` —
   `(theoreticalRevenue * effectiveCollectionEfficiency + swfReturns) * fiscalReactionMultiplier`. The
   comment names the old `... * multiplier + swfReturns` form as what it replaced.
2. **`SwfStructuralDrawPercentPerYear = 3f`** — Norway's *handlingsregel*. The budget receives a smooth
   draw proportional to fund SIZE; **the realised market return no longer reaches the budget at all**,
   which also closed a double-count where the fund kept the return and the government spent it.

**How the error was made: `grep` over `MacroSystem.cs` returned nothing, and that was reported as
absence from the codebase.** The fix lives in `SimulationManager.cs`.

⚠ **This is rule 14 inverted, and it happened IN THE ACT OF WRITING THE RULE 14 ENTRY.** The rule says a
check is evidence only for claims its enumeration contains. A one-file grep enumerates one file; the
claim was about the whole tree. **A passing check that cannot fail is one failure mode; an absence claim
whose search cannot find is the mirror of it, and it is easier to make, because a grep returning nothing
feels like a result.**

**What it cost — three artifacts downstream of one grep:**
- an **escalation** to Elias, in a `RULINGS NEEDED` block;
- a **ruling made on a false premise** ("SWF cause-fix: BUILD"), for work already built;
- a **worked example in the permanent record** — this entry — teaching the wrong lesson from it.

**How it surfaced, which is the part worth keeping:** *no check found it.* It surfaced because the next
pass opened `SimulationManager.cs` to build the thing, and the thing was there. **A grep-shaped error in
a file nobody reopens would still be standing** — and would have been "confirmed" by every later reading
of the document it had been written into.

✅ **THE CHEAP GUARD, and it costs one command: before escalating an absence, search the whole tree, not
the file you expect.** `grep -rn <symbol> Assets/Scripts/` would have returned line 2682 immediately.
State the search that was run when reporting an absence, so its scope is visible to the reader the way a
check's enumeration now is. **"I did not find it in X" and "it does not exist" are different claims**, and
only the first is ever what a grep supports.

**The original lesson still holds** where it applies — a ruling reads as closed, so nothing watches it,
and when recording one it is worth recording which half is built. It just was not what happened here.

## Rule 14, extended: a check is evidence only if it RUNS in the environment that cites it (2026-08-11)

**Python is not installed on this machine.** The `py` launcher registers 3.11 at
`C:\Users\elias\AppData\Local\Programs\Python\Python311\python.exe`, and that directory does not exist.

**Four Python scripts are cited as evidence across `CLAUDE.md`, `COMPLETED.md`,
`POLISIM_MASTER_ROADMAP.md` and `POLISIM_POLITICS_ELECTIONS_ROADMAP.md`. Not one of them could ever have
run here.** This is not an incident hit while porting one of them; it is a property of the whole citation
set, and it extends rule 14:

> **A check is evidence only for claims its enumeration contains — and only if it runs in the environment
> that cites it.**

**Verified state of every cited check, 2026-08-11:**

| Check | Runs here? | Evidence status |
|---|---|---|
| `DeliveredAssetCheck` | ✅ run, exit 0 | confirmed |
| `ImporterSettingsCheck` | ✅ run, 149 sprites, 0/0 | confirmed |
| `StatIconCoverageCheck` | ✅ run, 19 of 19 | confirmed |
| `PartyMarkCoverageCheck` | ✅ run | confirmed (reports NOT PRESENT on `main`) |
| `ScreenEdgeCheck` | ✅ run, verified both ways | confirmed — **C# port of the script below** |
| `screenshot_edge_check.py` | ❌ never | superseded by the port |
| `seat_allocation_check.py` | ❌ never | **read only** — Part D re-scoped accordingly |
| `ledger_geometry_check.py` | ❌ never | **unread, unverified** — sole evidence about 1440p |
| `usa_election_check.py` | ❌ never | scoped to item 10, out of scope until it is scheduled |

⚠ **The dangerous property is not that a script cannot run — it is that its LAST REPORTED OUTPUT stays
quotable in a document forever.** `screenshot_edge_check.py`'s "54/54 then 0/54" was cited in three
documents as though it described the current build. It described a working tree that no longer exists,
and `main` was clipping on 54 of 55 screens the whole time.

## Nine checks existed and not one of them ever ran by itself (2026-08-11)

**Audited after `ImporterSettingsCheck` reached "0 errors, 0 warnings", on the grounds that a clean
result is only worth something if the check runs.** Every `*Check.cs` in `Assets/Editor/` was examined
for `[InitializeOnLoadMethod]` and `[MenuItem]`:

```
AggregationEquivalenceCheck  MenuItem=0  InitializeOnLoad=0
ChromeV2CoverageCheck        MenuItem=0  InitializeOnLoad=0
CreditRatingAnchorCheck      MenuItem=0  InitializeOnLoad=0
DeliveredAssetCheck          MenuItem=0  InitializeOnLoad=0
ImporterSettingsCheck        MenuItem=0  InitializeOnLoad=0
PartyMarkCoverageCheck       MenuItem=0  InitializeOnLoad=0
PublicationCadenceCheck      MenuItem=0  InitializeOnLoad=0
ScreenEdgeCheck              MenuItem=0  InitializeOnLoad=0
StatIconCoverageCheck        MenuItem=0  InitializeOnLoad=0
```

**Nine of nine invokable only from a command line someone has to remember to type.** The premise that
`DeliveredAssetCheck` fires on Editor open is false — §1F's *"run `DeliveredAssetCheck` and
`StatIconCoverageCheck` on the next Editor open"* was a request for a human to remember, phrased as if it
described automation. This is *"a delivery is not self-announcing"* pointed at the checks instead of the
assets, and it is worse there: **a check nobody invokes goes silent without announcing it**, and its last
known result stays quotable in a document indefinitely.

⚠ **THE MECHANICAL CAUSE, WHICH IS WHY NOBODY JUST ADDED A MENU ITEM: every check ended in
`EditorApplication.Exit`.** A menu item calling one would have closed the Editor on whoever clicked it,
so batch-only was not a choice anyone made — it was the only shape available. `CheckExit.Finish`
replaces those eight call sites and behaves identically under `-executeMethod` (verified: 0 on clean,
1 on a known-clipped set) while merely recording the code when a suite is collecting.

`CheckSuite` now runs the four project-scanning checks from a menu item and **once per Editor session**
— not per domain reload, since a script edit reloads many times an hour and re-scanning 149 textures
each time is how a check becomes the thing someone disables.

⚠ `ScreenEdgeCheck` is **deliberately excluded from the automatic run**: it reads whatever PNGs are on
disk, so on Editor open it would report on a capture set of unknown age, and a green result from stale
captures is worse than none — it answers a question about a build nobody is looking at. It has its own
menu item, to be run after a capture pass.

## Re-verified on clean `main` — because the first runs were not (2026-08-11)

⚠ **`ImporterSettingsCheck`'s 149/14/70 and `PartyMarkCoverageCheck`'s results were both produced in a
working tree carrying a closed session's uncommitted code.** That tree no longer exists, and between
`001b3a0` and `2a63cce` the Editor assembly did not compile on `main` at all — so those numbers described
an environment that never shipped. A green run in the wrong context is the defect this whole day is
about; it applies to a check's own verification as much as to anything the check reports.

Re-run on clean `main`, 0 compile errors: **149 sprites, 122 white-on-alpha / 26 full-colour / 1 tiling,
0 errors, 70 warnings (26 compression + 44 mipmaps)** — reproduced exactly. `PartyMarkCoverageCheck`
correctly reports `PARTY SYSTEM NOT PRESENT`, exit 0.

The 14 asset fixes were never in doubt: each was verified per-file by before/after count at the point of
edit, which is independent of the assembly state. It was the RUNS that needed redoing, not the fixes.

## The third compression instance, and what finally caught it (2026-08-11)

`icon_area_*` and `icon_nav_*` — 14 sprites, every navigation and area icon in the game — had imported as
**DXT5** for their whole life. Block compression on white-on-alpha at icon size, which is the one damage
vector §3's import spec exists to prevent.

**The first two instances were fixed in place; the third was found by a check built wide enough to
contain it.** §3's Chrome correction fixed Chrome. `mark_party_*` fixed four marks. Both were correct and
neither could see the next one — `Stats/`, 43 files of the same rendering class one folder over, was
already right, and nothing compared the two. `ImporterSettingsCheck` enumerates all 149 sprites under
`Art/UI/`, classifies each by **treatment rather than folder**, and found the 14 on its first run.

That is this project's standing answer to a defect fixed twice in place, applied for the fourth time
(`MeasuredLabel` for clipping, `InnerWidth` for the box model, `UiOverflowGuard`/`UiContainmentGuard` for
the two containment questions, now this). **The rule it confirms: after the second site-specific fix, the
next thing to build is the check, not the third fix.**

## Instance #4 of the wrong-plausible-mechanism pattern — and the phase split that caught it (2026-08-11)

**The guard was measuring during the IMGUI Layout pass.** Raising its cap gave the true count: **608
overflows, not 200** — 200 was the ceiling, not the number. Splitting by `Event.current.type` gave the
real answer:

```
608  [Layout]        row.width = 1.0, -128.0, -157.8
  0  [repaint]       row.width = 472.9 … 790.0,  squeeze = 1.000 everywhere
```

During Layout, `GUILayoutUtility` returns a dummy rect — measured here at width 1.0, and negative once
a caller subtracts padding from it. Every width derived from it is meaningless, so every comparison
against it "fails". **Nothing is drawn during Layout, so nothing can clip during Layout.**

Both named suspects were cleared by measurement, not by argument:

- **The trailing-column change.** At every real row width `fixedTotal` (406–505) sits well under
  `available` (593–630); it never reaches the squeeze. Running the same build with the change disabled
  produced **22 Repaint overflows** — the old caption bug at exactly 83.8px. The change *removes*
  overflows. It was the fix, never the cause.
- **The 0.35 squeeze floor.** `squeeze = 1.000` in all 106 Repaint geometries. It never engages at a
  real row width. It is not too low; it is untested by any real layout, and appeared in the numbers
  only because a bogus width forced it — pinning every column to floor × 0.35, which is precisely the
  30.2 / 22.6 / 11.3 that got written up as evidence.

⚠ **WHAT DISTINGUISHED THIS ONE, and the reason it is worth a section.** Neither reading the code nor
measuring at a single site would have found it. Both suspects survive any amount of code reading, and
a measurement at one site returns the same bogus width the guard already reported. What broke it was
measuring **the same quantity across both event phases** and noticing that one of them was
structurally meaningless. The generalisation: *when an instrument reports something implausible, check
whether the instrument is sampling a state in which the quantity has no meaning* — before believing
either the instrument or the theory that explains it.

⚠ **AND THE MECHANISM THAT WAS WRONG WAS MINE**, proposed and written into this file the day before as
"plausible, not measured". Recording the uncertainty honestly is what made it cheap to correct; it did
not stop it from being wrong. Three counts to keep straight: **11 instances of the clipping class, 4
of the wrong-plausible-mechanism pattern within it.**

**Second lesson, unrelated to the class: the cap silently understated by two thirds.** A capped list
bounds memory correctly and reports totals incorrectly. `TotalViolations` is now counted before the
cap and the driver fails on that, printing how many went unprinted.

## The widened guard: 144 at Repaint, and the class is NOT closed (2026-08-11)

Coverage was widened past the single choke point, because hooking only `MeasuredLabel` instruments
the labels that were *hardest* to fit and skips every label that fit on the first try.
`LedgerRow.DrawNameCell`'s two fast paths draw through raw `GUI.Label`; each tests one axis and leaves
the other unexamined. A vertical check was added at the same time — the row-pitch class had no
coverage at all.

**Result: 55 captured, 0 failed, 144 Repaint violations — 33 wide, 111 tall.** Two distinct defects,
both invisible to the previous guard, both confirmed against the captures rather than argued.

### The `wide` class — raw enum names, broken mid-word (33, 8 distinct)

`MeansTestedWelfare` needs 183.1 in 123.0; `VeteransAffairsDiscretionary` needs 257.0 in 188.8. These
are **unformatted enum identifiers reaching the UI** — no display-name formatter, so no space for the
wrap to break on.

⚠ **The failure is not what the check's name suggests, and the correction matters.** IMGUI does not
overflow an unbreakable token — it breaks mid-word. `run_05c_budget_welfare_deep.png` renders
`NegativeIncomeTax` as **"NegativeInco / meTax"**: measured as fitting, and unreadable. So the
predicate (widest unbreakable run > column) selects the right rows, but the consequence is a mid-word
break rather than a spill. *The check was right and the reasoning behind it was wrong* — which is the
same shape as the section above, caught faster because the capture was looked at.

The real fix is a CamelCase display-name formatter, which removes the cause and simultaneously gives
the wrap something to break on. **Not yet done.**

### The `tall` class — real geometry, currently inert ink (111, 14 distinct)

Uniformly `needs 26.1 tall in 24.0 at 20px`, at `PolicyScreenStatsRenderer`'s stat chips.
`LineHeightFor` returns `max(style.lineHeight, fontSize + 4)` = 24.0, but what renders is
`CalcSize().y` = 26.1 — **a height derived from a font metric that is not the metric governing
rendering.** Textbook row-pitch class.

⚠ **Verified against pixels before being called a defect: it costs no ink today.** Three magnified
crops (`tiles_3x`, `chips_5x`, `welfare_rows_3x`) show digits, capitals and the descender of "Poverty"
all complete. The 2.1px shortfall lands in internal leading above the cap height. It is latent, not
harmless — any glyph reaching higher in the line box (**Å / Ä / Ö, on a sv-SE machine**) or any font
change makes it visible. Fixing it moves row pitch on every policy screen, so it is Elias's call, not
a quiet edit.

### A third defect, found by eye in the same captures and invisible to any text-fits-its-rect check

`PoliSimWidgets.StatTile`'s delta label escapes the tile. Its cumulative `y` — padding, label, `20`,
`valueHeight`, `9`, an 18px pill — exceeds `tileHeight` (92 × scale), so at 929px the green delta is
drawn *below the tile's bottom edge*, colliding with the next tile's keyline
(`run_02_statistics.png`, top-left). **This is text overlapping beyond its box, and it is what Elias
reported.** The three clipping-#11 commits did not fix it and could not have.

⚠ **No guard of this design can see it.** The label fits its own rect perfectly; the rect is in the
wrong place. This validates text against the rect it was handed and never validates the rect against
its parent. **Child-rect containment is a third axis of this class with zero coverage.**

### All three fixed 2026-08-11 — 144 → 0, and what each one actually was

**The tall class was padding, not leading.** Measured on TeX Gyre Pagella at 20px: `lineHeight`
20.00, `CalcSize().y` 26.13 — the style's vertical padding, which `lineHeight` excludes and
`GUI.Label` obeys. `LineHeightFor` now returns `CalcSize().y`. Third version of that function and the
third distinct reason the previous metric was wrong: `fontSize + 4` was wrong per SIZE, `lineHeight`
was right about the FONT but not about the STYLE. The lesson holds unchanged — *derive the budget from
the quantity that governs rendering*.

⚠ **A claim in the previous section was overstated and is corrected here.** It said ring-accented
glyphs (Å/Ä/Ö) would lose ink. Measured: `CalcSize().y` is *identical* for ASCII, ring-accented
capitals and digits at every size from 16 to 22 — Unity reports the line box, which does not vary with
content. The under-provision was real; the specific harm predicted for it was not measured, and
should not have been asserted.

**StatTile's delta was two statements of one geometry.** The caller sized tiles at a flat `92 * scale`
while the widget's cumulative `y` needs ~107 with a delta present. Now `PoliSimWidgets.StatTileHeight`
walks the same named constants the drawing uses, and the caller asks. A magic number in the caller and
a cumulative `y` in the widget is one more statement of the same fact than can be kept true.

**The wide class needed no formatter — the names already existed.** `PolicyWebRenderer`'s node
metadata already held "Means-Tested Welfare", "Capital Gains Tax", "Veterans Benefits (Mand.)":
hand-written, correctly hyphenated, abbreviated to fit. No formatter produces those. The ledger simply
was not asking. `DisplayName.Of` bridges the model enums to that table by name identity — a parse, not
forty hand-maintained pairs — and falls back to a CamelCase splitter for the members with no node
(`VeteransAffairsDiscretionary`). **Restating the strings was rejected outright**: that is the "two
tables that agree until one is edited" failure already written up twice in this file.

**Verification: 55 captured, 0 failed, 0 overflows, exit 0** — and confirmed against pixels, not just
the counter. `NegativeInco / meTax` now sets as `Negative / Income Tax`; the delta sits inside its
tile.

## Child-rect containment: the second guard, BUILT 2026-08-11

`UiContainmentGuard`, scoped to the three composite widgets that lay out a stack inside a fixed rect —
`StatTile` (content stack vs tile), `LedgerRow` (four column rects vs row), `PolicyScreenStatsRenderer`
(two line rects vs chip). Not a general container stack: that needs infrastructure this codebase does
not have, and building infrastructure to satisfy a check is the wrong trade.

**What it actually guards is a SEPARATION, not a rectangle.** Each of the three has a height accessor
its caller uses to reserve space, and a drawing body that walks the same geometry independently.
`StatTileHeight` walks the same named constants `StatTile` walks — correct today, and precisely the
shape that drifts in silence: add an element to the stack, forget the accessor, and the tile overruns
by exactly that element's height with nothing failing. Same reasoning as one accessor for arc and
swatch, and `DisplayName.Of` over a copied table: make the agreement enforced rather than remembered.

**Built with the overflow guard's lessons applied from the start rather than rediscovered:**
Repaint-gated (Layout returns a dummy rect, which cost 608 false positives last time), deduped at
record time with a hard cap (the first overflow guard appended per frame and took the Editor down),
and `TotalViolations` counted before the cap (200 was read as a count when it was a ceiling).

⚠ **PROVEN TO FIRE, not merely observed to pass.** A clean result from a new check is
indistinguishable from a check that never runs — this project has been bitten by exactly that. So the
tile height was temporarily reverted to the old `92 * scale` and the pass re-run: **40 escapes,
"bottom by 13.0", content 92px in a 79px container.** It catches the defect it was built for. The
other two sites are known live from earlier instrumentation: `LedgerRow.Columns` logged 106 Repaint
geometries, and the stat chip's two rects produced 111 Repaint overflow violations before the
`LineHeightFor` fix.

**Both guards live and clean: 55 captured, 0 failed, 0 overflows, 0 escapes, exit 0.**

The pair now covers both questions the class has ever failed on — *does the text fit its rect* and
*does the rect fit its container* — which between them account for all eleven instances.

## Superseded: the OPEN item this replaced

The delta-escaping-its-tile defect was found **by eye**, in captures the overflow guard had just
passed. That is not a coverage gap the guard can grow into:

- `UiOverflowGuard` asks *"does this text fit the rect it was handed?"* The delta's answer was **yes**.
- The unasked question is *"does that rect fit the container it was drawn into?"* — a different
  relation, between two rects, with no text in it.

Widening the guard cannot reach this. It hooks label drawing; a containment check has to hook
*rect construction*, and the container is usually not in scope at the point a child rect is built.

**Worth building? Probably yes, but narrowly.** A general "every rect inside its parent" check needs a
container stack the codebase does not have, and building one is a large change to satisfy a check. The
cheap 80% is a **debug-only assert on the composite widgets that own a fixed rect and lay out a
stack inside it** — `StatTile`, `LedgerRow`, `PolicyScreenStatsRenderer` — each asserting its final
cumulative `y` is within the rect it was given. That is ~3 call sites, catches exactly the defect that
occurred, and needs no new infrastructure.

✅ **Built as scoped, same day.** Kept here because the reasoning for making it a SECOND check rather
than a wider first one is the durable part, and it is the question that will be asked again the next
time a guard misses something.

### Scope, stated so a clean run is not over-read

~178 raw label calls exist across the UI layer; this covers fixed-rect drawing, which is where all
eleven instances have landed. A clean run is evidence about that population — not about every label
on screen, and (per the delta above) not about whether rects sit inside their containers.

## Caption column: fixed truncation, introduced unevenness (2026-08-10)

`LedgerRow.Columns` now sizes the trailing column from its content, because that column holds two
kinds of thing — figures (~57px) where the board specified it, and dial scale legends (160–243px)
where it was reused. The 11%-of-row proportion served the first and truncated the second.

⚠ **Known consequence, logged rather than left in a commit message: track lengths now vary between
rows in the same group,** because each row's caption differs in width. Strictly better than
truncation, but visibly uneven — the same dial range renders at different physical lengths, which
weakens row-to-row comparison.

**The proper fix is a per-screen shared caption width**, using the same group-maximum pattern as the
spending bars: take the widest caption on the screen, give every row that width, and all tracks match
again. It needs plumbing through the call sites rather than a change inside `LedgerRow`, which is why
it was not done in the same pass.

## Publication cadence, measured — five of six stats never revise (2026-08-10)

`PublicationCadenceCheck` runs headless (`PublicationSystem.PublishDueFigures` is static, so no play
mode) and measures twelve simulated years for the USA:

| stat | 1st release | 1st PRELIMINARY | days PRELIMINARY | releases |
|---|---|---|---|---|
| Unemployment | day 36 | **never** | 0 | 143 |
| Inflation | day 42 | **never** | 0 | 143 |
| **Gdp** | day 1 | **day 119** | **1410** | 142 |
| PovertyRate | day 638 | **never** | 0 | 11 |
| Population | day 638 | **never** | 0 | 11 |
| CrimeIndex | day 638 | **never** | 0 | 11 |

⚠ **GDP is the only series with a revision stage.** The other five are single-estimate — published once
and final immediately. The annual three are not "hard to catch preliminary"; they have no preliminary
state to catch.

**This reframes behaviour 6 rather than scheduling it.** Channel 1 (badge + reference period +
publication date, carrying PUBLISHED-NESS) applies to all six and is where their information lives.
Channel 2 (frame style, carrying REVISION STATUS) is meaningful for GDP alone; the other five always
draw solid, which is correct and carries nothing. The channels are still right to be independent — they
are simply not equally loaded, and only one series can ever exercise the second.

**It also settled a rendering decision.** Eleven annual releases beside a daily live series reads as a
broken graph rather than as a comparison, so the annual three render as BADGED FIGURES — value,
reference period, publication date, no trend line — while the monthly and quarterly ones keep the
comparison graph and its "compare against the live figures above" framing. A published annual figure is
a bulletin, which is a stat block rather than a chart.

⚠ **And it corrected an assumption in the capture driver.** GDP first reaches preliminary at **day 119**
and holds it roughly a third of all days. The driver found one at day 1125 only because its own
1095-day minimum was binding — not because the state was scarce. The wait was never the constraint.

## Behaviour 5's hazard is BACKGROUND-STATE-DRIVEN control counts, not branching (2026-08-10)

Behaviour 5 is written as *"every control renders every frame in the same order; 'not applicable' is a
disabled state, never an omitted element."* Read literally that forbids any branch that changes how many
controls a screen emits. **Read that way it is not checkable** — it flags correct code and it gave no way
to rank the three sites a sweep turned up.

The sweep came after two instances were found by hand during the v2.0 conversion (`DrawMinimumWageControl`
returning early for a country with no statutory minimum wage; the partner tariff override emitting
button+slider when set and button alone when not). Two in one tab is a pattern, so the rest of the
codebase was swept rather than waiting to trip over a third: every control emission in `GameController`
(24 methods), `GraphRenderer` and `LedgerRow`, checked for early returns and for conditionals guarding a
control.

**Result: zero remaining early returns, three conditional sites, ONE of them a real defect.** What
separates them is not whether they branch — all three do — but **what drives the branch**:

| site | branches on | verdict |
|---|---|---|
| interest rate (`GameController`) | `hasIndependentCurrency` | ✅ **fine, and correct by construction** — both branches emit exactly one slider; only the range and label differ |
| cabinet reshuffle (`GameController`) | whether a portfolio is filled | ⚠ **breaks the wording, not the hazard** — `CabinetMinisters` is written only by `GameController` (verified: no simulation system touches it) and the screen has no sliders, so nothing can be mid-drag |
| **`GraphRenderer.DrawPageRow`** | **`totalPages`, derived from `history.Count`** | ⛔ **the real one** — history grows every turn, so the count went 0→2 spontaneously, on screens that DO carry sliders below the graph |

**So the checkable form of the rule is:** a screen's control count may not change as a result of state
the player did not just change. Branching on something immutable for the screen's lifetime is fine.
Branching on something the player toggles with the very control in question is fine, if nothing draggable
is on screen. **Branching on something that moves in the background is the defect** — that is the only
shape where a hot control's ID can shift underneath a drag.

Two things worth keeping from how this one was found. `DrawPageRow` **already had the discipline one
level too shallow** — its buttons were correctly `GUI.enabled`-disabled at the ends rather than omitted,
inside a block that was itself omitted. And it was the only site of the three that could genuinely fire,
while being the one nobody would have found by reading the screens that break — it lives in a shared
renderer, so it was one bug in every place a graph sits above a slider.

## A turn is now a year — the 3.017x fiscal defect (2026-08-10)

Elias reported from a live Editor session: **spending was running a full year's deficit every turn.**
He was exactly right about the symptom, and the mechanism was not where any of us expected.

**Every fiscal quantity in this game is an annual-rate figure, and none of them was ever divided by a
period.** The spending seeds are FY2025 federal outlays (`SocialSecurity 1530`, `Defense 850`, 25 lines
totalling **$5,761B**); GDP is annual (`29000` = $29T); revenue is `GDP x rate x BaseShareOfGdp`;
interest is an annual rate on the debt stock. A 121-day turn charged all of it every 121 days:
**365/121 = 3.017x** too fast.

**Three things this was NOT, each worth recording because each was a live hypothesis:**

- **Not a `/91.25` quarter-versus-year divisor.** There is no `91.25` anywhere in the simulation, and the
  only `365` in `SimulationManager.cs` is the SWF draw. The estimated ~4x was an eyeball reading of a
  3.02x error.
- **Not introduced by Continuous Time Phase 3.** Pre-Phase-3, `ApplyRevenueAndSpending` took the same raw
  annual sums once per turn. Phase 3 preserved the magnitude exactly — `121 x (annual/121) = annual`.
  The error predates the migration by the whole project.
- **Not a verification-integrity failure.** See below.

### The aggregation bar covered spending, passed, and was RIGHT to pass

`AggregationEquivalenceCheck` compares `GovernmentDebt` and `BudgetBalance` — both spending-driven —
turn-form against 121 daily steps. Its own doc comment says why it passed: *"Its flows are exact by
construction (121 × flow/121)."* **That identity holds whether or not the annual figure was the right
figure.**

It is an **equivalence** check, not a **correctness** check. It answers "does the daily form reproduce
the turn form?" and cannot answer "was the turn form right?" A faithful migration reproduces an error
perfectly, and a faithful migration is exactly what it was built to verify.

⚠ **The failure was in how the bar was read, including in this file.** Phase 3's validation notes were
written as though 39/39 within 3% validated the fiscal engine. It validated the migration. **No check in
this project has ever asserted that a turn's fiscal flows correspond to a turn's worth of real money** —
that assertion did not exist to fail.

### The same defect was found and fixed once before, in demographics

`MacroSystem.YearsPerTurn`'s doc comment describes *"3x over-compounding via too many applications of an
annual-scale rate"* — diagnosed and fixed there by scaling each turn's population growth to the turn's
real-time slice. **The fiscal path had the identical defect and was never brought along.**
`SwfStructuralDrawPerTurnFraction` was the single flow that did get the conversion, which is why it read
as deliberate rather than as an oversight everywhere else.

### The fix, and why this direction

**Elias's ruling: move the turn to the year, not the flows to the turn.** `DaysPerTurn` 121 → 365 and
`ElectionSystem.ElectionCycle` 12 → 4. The economy was already annual-per-turn in every respect, so
making the calendar agree with the model is two constants; making the model agree with the calendar is a
divisor on every flow plus a recalibration of every debt-anchored constant.

**It cost one line each because every per-day constant was derived rather than typed** — `PerDayReversion`,
`CrimeEffectsDailyScale`, `InfrastructureDecayRatePerDay`, `FiscalFlowPerDayFraction` all retune
themselves. That discipline, chosen during Phase 0 for a different reason, is the whole reason this was
cheap. `MacroSystem.YearsPerTurn` derives from `ElectionCycle` (`4f / ElectionCycle`) and lands at 1.0.

⚠ **The two constants are a PAIR and must always move together.** They are the project's only two
statements of turn length, and before this change they already disagreed by 0.5%: `121/365 = 0.3315`
years per turn against `YearsPerTurn`'s `4/12 = 0.3333`. They now agree exactly at 1.0.

### What the new baseline shows (seed 987654, 15 scenarios x 100/500)

⚠ **Per-turn fiscal figures are UNCHANGED, and that is the point rather than a null result.** The flows
were always one year's worth per turn; a turn is now a year, so they are correct without moving. Debt,
GDP, unemployment, inflation and debt-to-GDP all land where they did (USA turn 100: debt 286074.4 →
286074.6, DebtToGdp 139.2% both). What changed is what a turn *means*.

| moved | before | after |
|---|---|---|
| USA population, turn 100 | 374.8M | **450.6M** — `YearsPerTurn` 0.333 → 1.0 |
| USA population, turn 500 | 483.7M | 967.9M |
| Italy population, turn 500 | 33.9M | **11.2M** |
| calendar span, 100 turns | 33 years | 100 years |
| elections in a 100-turn run | 8 | 25 |
| SWF structural draw | 0.994%/turn | 3%/turn (= 3%/yr, correct) |

Gates after the change: **aggregation-equivalence 39/39 within 3%** (max drift 1.36%, unmoved),
**`CreditRatingAnchorCheck` 5/6** with Poland's known expected failure, anomalies **30 → 30**, no
population clamp hits.

⚠ **A 500-turn run is now 500 years, and the demographic tail is the honest consequence** — Italy falls
to 11.2M under five centuries of sustained negative growth. That is arithmetic, not a bug, but the
500-turn scenarios are now well past the horizon anything in this model is calibrated for, and results
there should be read as a stress test rather than as a projection.

**Every baseline captured before 2026-08-10 measured a 3.017x fiscal engine on a 121-day turn.** Anything
validated against one needs re-checking against the new capture.

## v2.0 preparation — three measured findings and one live defect (2026-08-03)

Four pieces of groundwork Elias ordered before the v2.0 design brief goes out. Each produced something
that changes what the brief can say, which is why they ran first.

### 1. The render-order spike — the survey's assumption was HALF WRONG

`RenderOrderSpike` layers three full-bleed colours (ScreenSpaceCamera Canvas red / IMGUI green /
ScreenSpaceOverlay Canvas blue) and reads three known pixels out of the captured frame. Result:

| Layer | Assumed | Measured |
|---|---|---|
| ScreenSpaceCamera Canvas | below IMGUI | **below IMGUI** ✅ |
| ScreenSpaceOverlay Canvas | ABOVE IMGUI | **below IMGUI** ❌ |

**IMGUI is always topmost; no Canvas render mode draws above OnGUI.** The hybrid survives — a Canvas
screen simply requires `GameController.OnGUI` to early-return while it is up, which is the
screen-granularity rule enforced by the renderer rather than by discipline. What does NOT survive is the
survey's proposed transition: a Canvas overlay cannot fade in over an IMGUI screen. Transitions have to
be driven from the IMGUI side (a full-screen IMGUI scrim CAN fade over everything) and handed off.

⚠ **The first spike run reported "FAIL — assumption does not hold", and that verdict was worthless.**
The screenshot showed IMGUI's band exactly where it belonged and *neither canvas drawn at all*, over a
background colour no layer in the test used. Two harness bugs: `Camera.main` was reused untouched so its
clear flags were whatever the default scene carried, and `Image` did not produce geometry. `RawImage` plus
an explicitly-configured camera fixed both. **The lesson is not "check your spike" — it is that a spike
which cannot draw its own control layers will still happily print a verdict about their order.** The
rewritten version logs each canvas's mode, material, colour and world corners, and reports INCONCLUSIVE
when no canvas drew, so the same false verdict cannot be produced silently again.

✅ **CONFIRMED IN A BUILT PLAYER, 2026-08-03 — the residual risk is closed.** `PlayerSpikeBuilder` builds
a standalone Windows player containing only the spike; it ran at 1600×900 and produced
`outer=RED, band=GREEN, centre=GREEN` — identical to the Editor. **The whole hybrid rested on an
Editor-only measurement of a compositing order, which is exactly the kind of result that can differ in a
build.** It does not. The player writes its verdict to a text file rather than only to the log, because a
player log lands in a per-user AppData path the caller then has to go and find.

### 2. The font test — IMGUI carries more than expected, and the body face is where it breaks

Two TTFs assigned to the existing `GUIStyle`s, nothing else changed, three variants captured across all
seven screens (`none` = today's default, `period` = Palatino Linotype + Courier New, `legible` = Georgia +
Consolas). Windows system fonts, so the test ran in a scratch copy — **they cannot ship in a build**, and
production needs open-licensed equivalents.

- **A display serif transforms the headers, and it costs one line per style.** This is the single
  highest-return, lowest-risk change available to the aesthetic, and it is available in IMGUI today.
- **A monospace body face is the expensive half.** Courier New pushed the Budget screen's explanatory
  paragraph from 5 lines to 6 and the right column's estimate block off the bottom of the fold — roughly
  35–40% more vertical space for the same words. Consolas cost far less (narrower, taller x-height) but
  reads as a code editor rather than a document. **The typewriter face is an aesthetic win and a density
  loss, and the Budget screen is where that trade gets decided.**
- ⚠ **`PoliSimWidgets` builds its OWN `GUIStyle`s, so a font assigned to `GameController`'s 15 styles
  never reaches it.** Every headline stat value, delta pill and badge stayed in the default font across
  all three variants — visible in the captures as identical numbers under different headers. A real font
  pass has to cover the widget library too, and that is not obvious from the call sites.

### 3. Flags and emblems were delivered, imported — and unreachable for weeks

All 6 `flag_country_*` and 4 `emblem_party_*` sprites sat under `Assets/Art/UI/`, which is **not** under a
`Resources/` folder, and were referenced by zero lines of code. `Resources.Load` could never have found
them; nothing referenced them, so nothing ever failed.

⚠ **`DeliveredAssetCheck` passed on these, and was right to.** It asks *"did every delivered file land
under `Assets/`"* — 191 of 191 entries across 7 zips, 0 missing. **A file existing under `Assets/` does not
mean the game can load it.** `StatIconCoverageCheck` asks the runtime question but only covers the 19
names the UI hard-codes. So: **an asset's status has TWO parts, DELIVERED and REACHABLE, and the project
only had a check for the first.** This is working-discipline rule 12 one layer in — the cached-status
lesson applied to the loader rather than to the inbox.

Both categories are now under `Resources/`, have derived-name accessors (`IconLibrary.GetFlag`,
`GetPartyEmblem`), and are drawn: flags in the country selector's button gutter, emblems in the hemicycle
legend. **They are also the two categories that are NOT white-on-alpha** — a flag and a party emblem are
authored in their own colours (the emblem SVGs carry `#E0B23C` and `#FFFFFF`), so the design brief must
not blanket-require the tintable convention that governs every other category.

**One thing the emblems broke, worth keeping:** drawn *instead of* the legend's colour swatch they looked
better and destroyed the legend's correspondence with the chart — the swatch colour is what keys each row
to its own arc of seats, and the emblem palette has no relationship to `GetCategoricalColor`'s
golden-angle hues. Swatch retained, emblem placed beside it. **This is a small live answer to v2.0's
eleven-hue question: a mark and a colour can carry identity together, and dropping the colour is not free
wherever that colour is also keying a chart.**

### 4. A LIVE DEFECT found by the font test, not caused by it

**`PolicyScreenStatsRenderer.DrawChip` clips both its stat names and its values, in production, today.**
Visible in the baseline capture as `Debt-to` / `Approval` / `Busines` / `Poverty` above vertically-cropped
figures. Cause: `GUI.Label` into hardcoded rects (`18f` tall for the name, `20f` for the value, inside a
`RowHeight = 44f`) while every style's `fontSize` scales with `Screen.height` — at 900px height the label
font is 20px in an 18px box.

**This is instance #8 of the exact class `PoliSimWidgets.MeasuredLabel` was built to end**, in a renderer
that never adopted it. Not fixed here — it was found during a survey and fixing production UI mid-survey
is the wrong moment — but it should be the first thing v2.0 touches on that screen, and it is a reminder
that the helper only helps where someone remembered to call it.

⚠ **Instance #9, found 2026-08-10 in a design specification rather than in code.** Pass 3's redrawn
Budget board fixes the ledger row at `36px` and permits generated names to wrap to two lines at `13px`
with `line-height 1.1`. That is `28.6px` inside `36px` — fine at 1080p, about 7px spare. But §3.2 of the
asset request states the governing constraint in its own words: *"every style in this UI rescales with
`Screen.height`, so there is no single fixed render size."* At 1440p the same name sets at ~`17.3px` and
two lines become `38.1px` — **taller than the row meant to contain it.**

Same defect, one layer earlier: a height fixed in absolute pixels while the type inside it scales.
**`36px` is the value at 1080p, not the row height.** The fix is the one that worked at instances #1–#8
— derive it from the font metric:

```
RowHeight = max(2 x LineHeightFor(nameStyle) + pad, SliderTrackHeight + pad)
```

The general lesson, now that it has appeared nine times: **a number that came out of a mockup is a
measurement at one resolution, never a constant.** Catching this one in the spec rather than in a
capture is the first time the class has been found before it shipped.

### ⚠ THE MOCKUP-NUMBER RULE — and it is not only about resolution

**Instance #10 arrived the same day, in the same file, from the same spec, and I had already written
the warning one method above it.** `LedgerRow` derived its row height from the font metric — correctly,
having just recorded why — and then took the spec's `250 / 150 / 88` column widths and scaled them by
font size alone. The first live capture killed it: those measures are quoted against board 1b's ~1100px
ledger panel, the actual centre column on this screen is ~745px, the three fixed columns summed to more
than the row was wide, the track collapsed to its floor, and both figure columns rendered past the panel
edge where they simply did not appear.

**So the rule is wider than "derive from `Screen.height`".** A number on a design board is a measurement
taken against *that board's* conditions — its resolution, its font size, **and its container width** —
and every one of those varies here. Deriving against one axis while copying another still ships the bug.

**Every remaining number the spec supplies is suspect until derived or explicitly confirmed as fixed.**
Paddings, insets, tick sizes, the 14px scrollbar track, the 6px inset, the 40px minimum thumb, the 11px
shrink floor, the 44/26/22px panel paddings. Each is a value at 1920×1080 in a container of a particular
width. **Confirming one as genuinely fixed is a real answer** — some things should not scale, a hairline
rule being the obvious case — but it has to be a decision that was taken, not a number that was copied.

The tell, in both instances: the number arrived with no unit of comparison attached. `36px` and `250px`
each looked like a constant because the board had nothing else in view to measure them against.

### The screenshot harness, and the batchmode trap that cost the first attempt

The project had **no screenshot tooling at all** (zero `ScreenCapture` references), so every visual
confirmation had been Elias looking at his own Editor — fine in conversation, useless for an async report.
`UiScreenshotDriver` + `UiScreenshotCapture` drive `GameController` to each screen by reflection and
capture at end-of-frame. They live in the scratch copy, not the repo.

⚠ **`WaitForEndOfFrame` NEVER RESUMES under `-batchmode`.** The first run attached, logged
`driver attached, label=none, 640x480`, and hung with no error and no stack trace — a silent hang, the
same failure shape `BatchSimulationRunner`'s own comment warns about. Running with a real Editor window
fixes it and fixes the resolution at the same time, which matters more here than it looks: **every style
in this UI derives its font size from `Screen.height`, so a 640x480 capture would have been a screenshot
of font sizes no player ever sees.** Any future visual tooling in this project must not use `-batchmode`.

## v2.0 wiring — the theme inversion, and two failures worth more than the feature (2026-08-03)

The chrome pack went live at four shared chokepoints — `PoliSimTheme`, `UiPalette`, `PoliSimWidgets`,
`GameController`'s style init — rather than across 80 draw methods. Because every screen already reads
through those, the change propagates. Details are in the commit; three things belong here.

### 1. A theme inversion is not a palette swap

Going from light-on-dark to ink-on-paper invalidates every assumption about the GROUND, and those
assumptions are scattered far from the palette:

- **`DrawColoredLabel` MULTIPLIED `GUI.color`.** Correct while the ramp was near-white (white × hue =
  hue) and useless at `#2B2620`, where near-black × hue is near-black. Every coloured header in the game
  would have rendered as an identical dark smudge. It now sets the style's `textColor` for the call.
  **The multiply was documented as deliberate**, which is exactly why it was easy to miss: the comment
  explained *why* it was multiplicative without stating what it depended on.
- **`MutedIconTint` was white @60%** — invisible on paper.
- **Text needs a surface.** `_boxStyle` inherited `GUI.skin.box`; once the ink ramp landed, every label
  inside a tab container was dark-on-dark and had effectively vanished. **Ink and paper are one change
  and cannot ship apart.**

The generalisable form: *when a theme's ground flips, grep for what assumed the old one — not for the
colours, but for the OPERATIONS (multiply, lighten, alpha-over) that only make sense against it.*

### 2. ⚠ A guard written for exactly one failure, which did not catch it

`UiPalette.GetTintedChrome` calls `Texture2D.GetPixels`, which throws unless the texture is imported
readable. The author knew, and wrapped it in a `try`/`catch` with a comment saying — accurately — that an
escape here "would throw INSIDE OnGUI and take the entire UI down rather than degrading one button".

**It caught `UnityException`. Unity throws `ArgumentException` for a non-readable texture, and
`ArgumentException` does not derive from `UnityException`.** So the defence written for this precise
failure let it through, and the first capture of the wired UI was an empty desk — no error visible in
the game, just nothing drawn.

It survived unnoticed only because every chrome sprite happened to be imported readable. The v2.0 metas
were generated from `icon_stat_gdp.png.meta` per the asset request's §3, which carries `isReadable: 0` —
**correct for icons, which are drawn with a `GUI.color` tint, and wrong for chrome, which is pixel-tinted.
§3 does not draw that distinction and should.**

Two lessons, and the second is the transferable one:

1. Catch broadly at an `OnGUI` boundary. Any escape costs the whole frame, and the fallback here was a
   complete working path — there is no failure mode where re-throwing served the player.
2. **A guard is a hypothesis about how something fails, and hypotheses need testing.** This one had a
   correct diagnosis, a correct remedy, and the wrong exception type, and nothing in between would ever
   have revealed it. If a `catch` exists to prevent a specific disaster, provoke that disaster once.

### 3. The delta pill was retired on someone else's judgment

Asked whether a chip sprite was wanted for the signed delta on a stat tile, Design answered that it was
not: on paper a delta is inked text, not a lozenge. That is now how it renders. **Worth recording because
the question was posed as optional and the useful answer was "don't"** — the brief asked for judgment
instead of a sprite, and got it. B2 is untouched: the ink is still chosen by whether the change is GOOD,
never by whether the number rose.

## Per-Partner Tariff Overrides
A player-settable tariff override per trade partner, extending `TradeSystem`/`TradePartner` rather
than building a new mechanic alongside the existing `BaseTariffRate`/`TariffRateChange` lever:

- **`TradePartner.PlayerTariffOverride`** (`float`, default `-1f` = unset/no override - the same
  sentinel-value idiom `Country.BaseDebtInterestRateOverride` already uses, not a nullable `float?` or
  a separate boolean flag): a `HasPlayerTariffOverride => PlayerTariffOverride >= 0f` derived property
  reads it. When set, it is this country's own tariff rate specifically for imports from that one
  partner, persisting turn to turn exactly like `TaxLine.Rate` does (once set, it stays until the
  player changes or resets it - not a one-turn delta). `TradePartner.Clone()` was added (mirroring
  `TaxLine.Clone()`/`SpendingLine.Clone()`) since `PreviewTurn`'s throwaway country clone needs its
  own copy - `SimulationManager.ApplyPartnerTariffOverrides` mutates `PlayerTariffOverride`, so a
  shared reference would leak a draft preview override into the real `World` (see "a real correctness
  fix, found not asked-for" below).
- **`TradeSystem.GetTariffRate` precedence** (most-specific-first): the importer's own
  `TradePartner.PlayerTariffOverride` for this specific exporter, if set, is checked and returned
  BEFORE the existing shared-bloc/external-bloc/`BaseTariffRate` resolution - an unset override (the
  default, `-1f`) falls through to that existing logic completely unchanged. The lookup is keyed off
  the *importer's own* `TradePartner` link to the exporter (`importer.TradePartners.Find(p =>
  p.PartnerId == exporter.Id)`) - each side of a bilateral relationship has its own separate
  `TradePartner` instance (per `WorldFactory.AddBilateralTrade`), so this can only ever reflect a rate
  the importer itself set on its own imports.
- **One-directional by construction, not by a special case**: the task required this to only affect
  the player's own tariff on its own imports, never the reverse (a partner's tariff on the player's
  exports staying that partner's own policy). This falls out of the existing model's shape with zero
  extra logic - `TradeSystem.ApplyTradeEffects` already calls `GetTariffRate` twice per link, once
  with the country as importer (`tariffOnOurImports`) and once with the partner as importer
  (`tariffOnOurExports`, i.e. what THEY charge on what we sell them) - the override lookup for
  `tariffOnOurExports` reads the PARTNER's own `TradePartner` list, not the player's, and since nothing
  except the player's own `PolicyDecision.PartnerTariffOverrides` ever sets an override (an AI-
  controlled country never receives one, since `PolicyDecision.None()` carries an empty dictionary),
  every non-player country's links stay at the default forever and resolve exactly as before.
- **`PolicyDecision.PartnerTariffOverrides`** (`Dictionary<CountryId, float>`, absolute target per
  partner - same "SET, not delta" semantics as `TaxRateOverrides`): `SimulationManager.
  ApplyPartnerTariffOverrides` clamps the requested rate to the same `[MinBaseTariffRate,
  MaxBaseTariffRate]` (0-50%) range `BaseTariffRate` itself uses (reused directly, not duplicated) and
  sets it straight onto that partner's `PlayerTariffOverride` - a no-op for any partner with no entry.
  Runs in `AdvanceTurn` right alongside `ApplyTariffRateChange`, before `TradeSystem.ApplyTradeEffects`
  resolves trade, so this turn's override is what this turn's trade actually sees. Only a partner with
  an *already-active* override gets an entry from `GameController.BuildPlayerDecision` - deliberately
  NOT every partner unconditionally (unlike `TaxRateOverrides`, where every implemented line's own
  current `Rate` is always a safe, idempotent fallback): if every partner were unconditionally included
  using today's *effective* (possibly bloc-resolved) rate as a fallback, an untouched partner would get
  silently pinned to whatever its dynamic rate happened to be that turn, converting "no override,
  tracks the bloc/base rate" into "a static override" without the player ever touching anything - a
  real idempotency pitfall specific to this field (an override activates a fundamentally different
  resolution path, unlike a tax rate reasserting its own current value).
- **UI** (`GameController`'s Trade tab, `DrawTradePartnerRow`): each partner shows its current
  effective tariff (both directions, as before) plus, mirroring `TaxLine`'s Implement/Remove +
  Rate-slider pattern exactly: a "Set Override" button (shown while no override is active) that
  IMMEDIATELY activates one at today's effective rate (so turning it on is itself a no-op on the
  actual tariff - the player then moves it from there) and forces a preview recompute; while active, a
  slider (bounded `[0,50]`, matching `MinBaseTariffRate`/`MaxBaseTariffRate`) shows and drafts the
  requested override rate (sent via `PartnerTariffOverrides` on Advance Turn, same deferred-commit
  timing as the tax-rate sliders), plus a "Reset to Default" button that IMMEDIATELY clears the
  override back to `-1f` and forces a preview recompute - the same immediate-action pattern
  Implement/Remove already established, not a new one invented for this feature.
- **A real correctness fix, found not asked-for**: `SimulationManager.ClonePreviewCountry` previously
  shared the real `Country.TradePartners` list reference with the preview clone (documented at the
  time as safe, since nothing mutated it - "TradeSystem only ever reads it, never mutates it"). That
  became false the moment `ApplyPartnerTariffOverrides` needed to run inside `PreviewTurn` too (to
  preview a draft override's effect, per the task's point 5) - without deep-cloning, merely *previewing*
  a partner tariff change (no Advance Turn needed) would have written the draft value directly onto the
  real `TradePartner.PlayerTariffOverride`, leaking into actual game state. Fixed by adding
  `TradePartner.Clone()` and a `ClonePreviewTradePartners` helper (mirroring `ClonePreviewTaxLines`/
  `ClonePreviewSpendingLines` exactly) so `TradePartners` is now deep-cloned like every other
  turn-mutated list on the preview country. This doesn't affect the REAL partner countries read via
  `world.GetCountry(...)` inside `TradeSystem.ApplyTradeEffects` - those are only ever read (for the
  `tariffOnOurExports` lookup against the partner's OWN unmodified list), never written, regardless of
  whether the calling country is the real one or a preview clone.
- **Validation - explicit fidelity re-check disclosure**: given the harness's fidelity was found
  lacking twice already this session (the stale swing-threshold, and needing an explicit side-by-side
  diff for the spending-sliders work), its `TradeSystem.GetTariffRate`/`TradePartner`/
  `PolicyDecision.PartnerTariffOverrides`/`ApplyPartnerTariffOverrides` were freshly diffed line-by-line
  against the real files for this task rather than assumed still-accurate - confirmed identical logic
  (same override-first precedence, same `[MinBaseTariffRate, MaxBaseTariffRate]` clamp, same
  find-by-`CountryId` lookup). A new `--tariffoverride` harness scenario sets a high (40%) override on
  Germany (USA's largest direct partner) and a low (0%) override on France at turn 1 only, leaving
  Sweden/Poland (USA's other two direct partners) untouched as a control group, then sends NO further
  `PartnerTariffOverrides` entries for the remaining 99 turns - confirming persistence (both overrides
  still read back correctly at turn 100 without being resent) as well as precedence (Germany resolves
  to 40%, not its otherwise-applicable ~3% `BaseTariffRate`; France to 0%, not 3%; Sweden/Poland
  unaffected at 3%). 100-turn runs (events on/off) stayed numerically bounded - no NaN, no negative
  GDP - with GDP/output-gap trajectories identical to an otherwise-equivalent baseline run, and total
  `Budget` measurably higher (~$5,280B more over 100 turns) than the same baseline without the
  override, confirming the fiscal channel (tariff revenue) responds in the expected direction. **One
  honestly-flagged, pre-existing model characteristic surfaced by this validation, not introduced by
  it**: this game's `TradeSystem` only ever lets a country's OWN import tariff affect the REVENUE it
  collects (`tariffRevenue += link.ImportVolume * (tariffOnOurImports / 100f)`) - `effectiveImports`
  itself is never reduced by that tariff (trade volumes are static inputs, not demand-responsive - see
  `TradeSystem`'s own doc comment), so a partner override moves the budget/debt path but does **not**
  move `TradeBalance`/NX/GDP directly, the same way `BaseTariffRate`/`TariffRateChange` never did
  either. This is not a gap introduced by adding per-partner overrides - it is how tariffs already
  worked in this model before this task, now just reachable at finer (per-partner) granularity. At the
  time this section was originally written, this said "this environment has no reachable Unity Editor
  install" - that was based on an incomplete search; see "SpendingLine Amount Ceiling - Debt-to-Zero
  Fix" below for the correction (a real, working Unity 6000.5.4f1 install exists at
  `G:\UNITY\Unity Hub\6000.5.4f1\Editor\Unity.exe`) and for what real-Unity confirmation was actually
  obtained.
- **A gap from this task's own requirements, found and fixed in a follow-up pass**: this section's
  original UI bullet only covered the NEW per-partner override controls - the general Tariff Rate
  Change slider (`PolicyDecision.TariffRateChange`, moving `Country.BaseTariffRate`) was supposed to
  move out of the left-column policy panel into this same Trade tab (the task's own requirement #4),
  but the slider was left in `DrawPolicyControls` by mistake. Fixed: `DrawPolicyControls` now only
  shows a note pointing at the Trade tab (matching how Tax/Spending policy already point there);
  `DrawTradeSection` draws the Tariff Rate Change slider itself (alongside a read-only display of the
  current `BaseTariffRate`) right above the per-partner override rows, so every tariff-related control
  - general and per-partner - now lives in one place. `_tariffRateChangeInput` and its
  build/cache/reset wiring were untouched - only which method calls `GUILayout.HorizontalSlider` for it
  moved.

## Welfare Policy
A player-settable welfare/anti-poverty portfolio, extending the model with a genuinely new
`PovertyRate` metric plus six `WelfareProgramType`s a country can implement/adjust/remove - mirroring
`TaxLine`'s implement/adjust/remove pattern exactly (same "an absolute target via `PolicyDecision`,
implement/remove is a separate immediate action" idiom), all six for every country, per the task's
explicit requirement:

- **`EconomyState.PovertyRate`**: seeded per country from real OECD relative-poverty-rate data (USA
  18%, Italy 14%, Poland 10%, Germany 11%, Sweden 9%, France 8%) - directionally realistic figures,
  not precisely researched down to the decimal, matching every other real-data figure in this
  codebase's own stated calibration philosophy. `Country.BaselinePovertyRate` is a separate structural
  anchor seeded to the SAME figures (see `MacroSystem.ApplyPovertyRate` below) - the same "avoid a
  turn-1 shock" reasoning `Country.ComfortableDebtToGdpPercent`/"Turn-1 GDP Consistency" already
  established: a new game opens with `PovertyRate` already at (or very near) its own baseline, not an
  artificial jump.
- **`MacroSystem.ApplyPovertyRate`**: `PovertyRate` mean-reverts (at `PovertyReversionSpeed = 0.15`,
  a moderate-slow speed - real poverty rates don't swing wildly turn to turn the way
  unemployment/inflation can) toward a baseline of `Country.BaselinePovertyRate` adjusted by the SAME
  unemployment/inflation GAPS (versus NAIRU/target, not absolute levels) that already drive
  `ApplyApprovalRating`'s own misery index - reusing "already-proven drivers elsewhere in the model"
  per the task's explicit instruction, rather than inventing new sensitivities from scratch. Any
  implemented `WelfareProgram` further reduces the baseline (see `GetPovertyReductionSensitivity`)
  before the reversion is applied. Hard-clamped to `[0, 100]`.
- **`WelfareProgramType`** (`UBI`, `NegativeIncomeTax`, `MeansTestedWelfare`, `UniversalHealthcare`,
  `HousingAssistance`, `ChildcareSubsidies`) / **`WelfareProgram`** (`WelfareProgram.cs`, mirrors
  `TaxLine.cs`'s pattern precisely): `Type`, `GenerosityLevel` (0-100%, persistent - *set* turn to turn
  by `PolicyDecision.WelfareGenerosityOverrides`, an absolute target exactly like `TaxLine.Rate`, not a
  delta), `IsImplemented` (toggled *immediately* by the player, not deferred to Advance Turn - see
  `GameController`'s Welfare Policy tab), and a derived `CostShareOfGdp` (looked up from
  `WelfareProgramCostShares` by `Type`, the same `BaseShareOfGdp` idiom `TaxLine` already uses).
  `WelfareProgram.Clone()` exists for the same reason `TaxLine.Clone()` does - `PreviewTurn`'s
  throwaway country clone needs its own copies, since `ApplyWelfareGenerosityChanges` mutates
  `GenerosityLevel`. None implemented by default for any country (`WorldFactory.SeedWelfarePrograms`) -
  per the task's explicit requirement - each starting at a placeholder 50% `GenerosityLevel` for
  whenever a player later implements it, matching the "modest/inactive" tax lines' own placeholder-rate
  idiom.
- **`WelfareProgramCostShares`** (illustrative real-world-scale, at FULL 100% `GenerosityLevel` -
  gameplay-tuning constants, not precise budget figures, the same "rough illustrative weight" idiom as
  `TaxTypeBaseShares`): `UBI` 18% (meaningfully expensive - paying every resident a flat amount,
  universally, is the most expensive way to move the needle on poverty, per the task's own framing);
  `NegativeIncomeTax` 8%, `MeansTestedWelfare` 6% (targeted/income-tested, so cost far less per point
  of `GenerosityLevel`); `UniversalHealthcare` 10% (a large program in its own right - real
  single-payer systems typically run 8-11% of GDP); `HousingAssistance` 1.5%, `ChildcareSubsidies` 1%
  (narrow, much smaller programs, per the task's own "~1-2%" framing).
- **Cost** (`SimulationManager.GetTotalWelfareCost`): the sum, over every implemented `WelfareProgram`,
  of `GDP * (CostShareOfGdp / 100) * (GenerosityLevel / 100)` - a NEW spending category, added directly
  into `ApplyRevenueAndSpending`'s total budget outflow alongside Mandatory/Discretionary/
  `UnemploymentBenefitCost`/`InterestOnDebt`, deliberately NOT touching `IncomeSecurity` or any other
  existing `SpendingLine` (per the task's explicit requirement). Treated as a transfer (excluded from
  `MacroSystem`'s national accounts G term) - the same reasoning already applied to Mandatory
  `SpendingLine`s/`UnemploymentBenefitCost`/`InterestOnDebt`: welfare programs (UBI, means-tested
  transfers, healthcare/housing/childcare subsidies) are payments to individuals, not government
  purchases of goods and services.
- **Per-program effects** (kept small and separately named, per the task's explicit instruction,
  mirroring `ApplyCategorySpendingEffects`'s own "small, separable per-category profile" style):
  - **PovertyRate reduction** (`MacroSystem.GetPovertyReductionSensitivity`, points-per-100%-
    `GenerosityLevel`): `UBI` 8 and `MeansTestedWelfare` 7.5 are the strongest (direct income
    transfers); `NegativeIncomeTax` 7 is nearly as strong as `UBI` but at less than half `UBI`'s
    `CostShareOfGdp` - deliberately more cost-efficient per point of poverty reduction (efficiency =
    sensitivity/cost: `NegativeIncomeTax` 7/8=0.875 vs `UBI` 8/18=0.444 vs `MeansTestedWelfare`
    7.5/6=1.25, the most cost-efficient of the three - consistent with real economic arguments that
    targeting is the most efficient, if also the most politically contentious, lever); `Universal
    Healthcare`/`HousingAssistance`/`ChildcareSubsidies` are modest (4/3/3), matching the task's own
    framing.
  - **ApprovalRating** (`MacroSystem.GetWelfareApprovalEffect`, threaded into `ApplyApprovalRating` as
    a new additive term - `country` was already a parameter, so no signature change was needed beyond
    that): an ONGOING STOCK effect of each program's CURRENT `GenerosityLevel` every turn (the same
    idiom `TaxLine.Rate` affecting revenue every turn already uses), NOT a one-time "this-turn change"
    shock like the tax-hike/spending-change terms. `UBI`/`UniversalHealthcare` are strongest (3.0 -
    universal, highly visible programs); `NegativeIncomeTax` 2.0; `MeansTestedWelfare`/
    `HousingAssistance`/`ChildcareSubsidies` 1.5 (more modest - targeted spending is politically less
    visible than a universal program of similar poverty-reduction power).
  - **Consumption/Confidence** (`MacroSystem.ApplyWelfareProgramEffects`, a new method mirroring
    `ApplyCategorySpendingEffects`'s pattern, called right alongside it): `UBI` nudges
    `ConsumerConfidence` up (the task's "modest Consumption/GDP boost", modeled the same way Healthcare
    spending already nudges it); `UniversalHealthcare` nudges `BusinessConfidence` up (reduced employer
    healthcare-cost burden, modeled the same way Education spending already nudges it). Both small and
    clamped to `[MinConfidence, MaxConfidence]` alongside `ApplyCategorySpendingEffects`'s own nudges.
    Every other `WelfareProgramType` deliberately has no confidence effect (only poverty-
    reduction/approval) - narrow/targeted programs are modeled as NOT moving broad consumer/business
    sentiment, per the task's "minimal broad GDP effect" framing for `MeansTestedWelfare`.
  - **Unemployment** (`MacroSystem.GetWelfareAdjustedReversionSpeed`, adjusts Okun's Law's own
    `UnemploymentReversionSpeed` rather than shocking `Unemployment` directly - `country` was already a
    parameter to `ApplyOkunsLaw`, so again no signature change beyond that): `UBI` SLOWS reversion
    toward NAIRU slightly at full generosity (the real, debated labor-supply effect - kept subtle
    deliberately, since the real-world effect is itself unsettled, not a confident modeling choice);
    `ChildcareSubsidies` SPEEDS it slightly (the labor-force-participation effect documented
    specifically for parents). Both are small and the combined result is floored at
    `MinUnemploymentReversionSpeed` (0.3) so neither can stall or reverse Okun's Law's own
    mean-reversion, only tilt it.
- **UI** (`GameController`'s new "Welfare Policy" tab, a fifth right-column tab alongside Recent
  Turns/Trade & Spending/Tax Policy/Spending Policy): `DrawWelfarePolicy`/`DrawWelfareProgramRow`
  mirror `DrawTaxPolicy`/`DrawTaxLineRow` line-for-line - an Implement/Remove toggle (immediate,
  forces a preview recompute) plus, only while implemented, a slider that directly sets this turn's
  target `GenerosityLevel` (0-100%, not a small per-turn delta). `PovertyRate` is shown on the
  dashboard alongside the other headline stats, and the live preview gained a matching
  `PovertyRateChange` line (`PolicyPreview.PovertyRateChange`) for consistency with every other
  tracked dashboard stat already getting one.
- **Validation - the full matrix, confirmed directly in real Unity** (per "Real-Unity Validation is
  the Standard Path" below): a new `--welfarestress`/`welfarestress` scenario (added to both the
  standalone harness and `SimulationTestRunner`/`BatchSimulationRunner`'s `-runmatrix`) implements ALL
  SIX `WelfareProgramType`s simultaneously at 90% `GenerosityLevel` for USA at turn 1, then holds
  (persistent, like a tax rate - no need to resend). This is a genuinely unrealistic combination in
  practice (`UBI`/`NegativeIncomeTax`/`MeansTestedWelfare` are typically substitute approaches to the
  same problem, not complements the model treats as mutually exclusive) - deliberately so, as a stress
  test of the system's numeric bounds rather than a realistic policy scenario. Confirmed in the
  standalone harness (100 and 500 turns, events on/off): `PovertyRate` correctly floors at exactly 0%
  (the combined reduction sensitivity, ~29 points at 90% generosity, exceeds USA's ~18% baseline),
  `ApprovalRating` correctly ceilings at 100%, `DebtToGdpRatio` settles at the same ~294% attractor
  "Fiscal Reaction Function" already found for extreme sustained spending - all hard-bounded, none
  diverging, confirmed flat at 500 turns (not just transiently bounded at 100). **Then confirmed
  directly in real Unity**: the full 10-combination matrix (baseline/stress/sustainedexploit/
  tariffoverride/welfarestress x 100/500 turns) ran with zero NaN/negative-value/out-of-range
  anomalies across all ten - `welfarestress`'s anomaly count (45 at 100 turns, 136 at 500) was in the
  same range as the other four scenarios, not dramatically higher, meaning the extreme multi-program
  stress doesn't introduce a qualitatively different instability. `SimulationTestRunner.CheckAnomalies`
  was extended with `PovertyRate`'s own finite/range check (alongside the existing GDP/Unemployment/
  Inflation/etc. checks) and a finite check per `WelfareProgram.GenerosityLevel`, so this is a real,
  checked-every-turn-for-every-country confirmation, not an incidental one.

## Live Policy Preview
`GameController` shows an estimate of this turn's effect under the sliders' *current* (not yet
committed) values, recomputed every `OnGUI` call:

- **`SimulationManager.PreviewTurn(countryId, decision)`** reruns the exact same per-country
  pipeline `ApplyDomesticPolicy` would (`ApplyTariffRateChange`, `TradeSystem.ApplyTradeEffects`,
  `ApplyTaxRateChanges`, `GetBaselineGovernmentSpending`, `MacroSystem.ApplyCategorySpendingEffects`,
  the fiscal helpers including `GetTotalTaxRevenue`, `MacroSystem.ApplyNationalAccounts`/
  `ApplyOkunsLaw`/`ApplyPhillipsCurveInflation`/`ApplyInflationExpectations`/`ApplyApprovalRating`)
  against a throwaway clone (`ClonePreviewCountry`: its own `EconomyState.Clone()`, its own copies of
  the structural fields those formulas mutate, and its own deep-cloned `TaxLines`, `SpendingLines`,
  and `TradePartners` — `Rate`/`Amount`/`PlayerTariffOverride` are each mutated by
  `ApplyTaxRateChanges`/`ApplySpendingLineChanges`/`ApplyPartnerTariffOverrides` respectively, so none
  of these three can be shared references — but the *same* `CurrencyZone` reference, since it's
  read-only from `PreviewTurn`'s perspective (see "Per-Partner Tariff Overrides" above for why
  `TradePartners` moved from shared to deep-cloned)) — so the result stays grounded in the real model
  instead of a separately hand-rolled estimate, and nothing it computes is ever written back to the
  real `World`. Deliberately never rolls an `EventSystem` event or
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

## Real-Unity Validation is the Standard Path
For most of this session, every validation writeup in this file said some version of "this
environment has no reachable Unity Editor install" - that was wrong, based on an incomplete search
(see "SpendingLine Amount Ceiling - Debt-to-Zero Fix" above for how and when that was found and
corrected). Now that a working install is confirmed reachable
(`G:\UNITY\Unity Hub\6000.5.4f1\Editor\Unity.exe`, with this project already registered against it),
**the standard, primary way to validate any change to simulation behavior is a real headless Unity
run via `Assets/Editor/BatchSimulationRunner.cs`, not the standalone C# harness alone**, and not by
asking a person to open the Editor and press Play manually:

**Editor version update (2026-08-01)**: the `6000.5.4f1` install referenced throughout this section
and elsewhere in this file became corrupted (a partial Unity Hub update left `Data\BCLExtensions`,
`Data\DotNetSdk`, `D3D12`, and `BugReporter` incomplete - `Unity.dll failed to load` on launch,
`ERROR_FILE_NOT_FOUND` on every repair retry). Elias reinstalled clean via Unity Hub's own Uninstall
+ reinstall flow, which came back as **`6000.5.6f1`** instead of the same patch version - the current
install is `G:\UNITY\Unity Hub\6000.5.6f1\Editor\Unity.exe`, confirmed healthy (analyzer DLL present,
D3D12/BugReporter no longer empty). Every `6000.5.4f1` path elsewhere in this file is left as-is since
it accurately describes what was true at the time it was written - read version numbers in historical
narrative entries as of that entry's own date, not as current state.

```
Unity.exe -batchmode -nographics -projectPath <path> -executeMethod
PoliSim.EditorTools.BatchSimulationRunner.Run -logFile <path> [-turns=N] [-scenario=X] [-runmatrix]
```

- **`-runmatrix`** runs the full baseline/stress/sustainedexploit/tariffoverride x 100/500-turn matrix
  (8 combinations) in a single Play session - this is what "Fiscal Reaction Function" above was
  validated against, and should be the default choice whenever a change could plausibly affect
  long-run stability (spending/revenue/debt mechanics, growth rates, anything with a feedback loop).
- **`-turns=N -scenario=baseline|stress|sustainedexploit|tariffoverride`** runs one specific
  combination - useful for a faster spot-check once a change is already believed correct.
- Omitting both defaults to a single 100-turn baseline run, matching the tool's original behavior.
- `SimulationTestRunner.cs` reads these from `Environment.GetCommandLineArgs()` directly, so they
  survive Unity's domain reload on entering Play mode - no separate build step or scene change needed.
- **The standalone C# harness (`SimHarness/Program.cs`, outside this repository) is now a fast-iteration
  tool, not the source of truth**: use it for rapid sweeps (many candidate values, many turn counts,
  in seconds rather than minutes) while narrowing in on a fix, exactly as "Fiscal Reaction Function"
  did to find `FiscalReactionSensitivity = 1.5` before it was ever ported - but treat a harness-only
  result as provisional until confirmed with an actual `BatchSimulationRunner` run, the same way this
  session already learned (twice, from the swing-threshold and spending-slider incidents) that the
  harness's own fidelity can't be assumed without checking.
- **Practical notes from getting this working**: a batch-mode Unity process can't open a project
  that's already open in another Editor window - if `-executeMethod` exits immediately with "return
  code 1" before any script compilation appears in the log, that's almost certainly why; check for
  (and close, with permission) other Unity processes for this project first. A `-runmatrix` run's own
  per-turn Debug.Log calls were found to be the dominant cost at scale (not the simulation math
  itself) - `SimulationTestRunner` logs every turn for a single run but only every 25th turn (plus the
  first and last) in matrix mode for exactly this reason. PowerShell's `Start-Process`/`Wait-Process`
  against the PID Unity was launched with can return before the real work is done - Unity sometimes
  hands off to a second process; check `Get-Process -Name Unity` and its CPU-time growth if a launch
  seems to finish suspiciously fast with no matching log activity.
- **Do not pass `-quit` alongside `-executeMethod ...BatchSimulationRunner.Run`**: `Run()` only
  triggers Play mode asynchronously (`EditorApplication.isPlaying = true`) and returns immediately -
  `-quit` was found (during the Expanded Event Pool validation below) to exit Unity right then, before
  Play mode - and therefore `SimulationTestRunner.Start()` - ever actually runs, leaving the log with
  no simulation output at all. `BatchSimulationRunner` calls `EditorApplication.Exit(0)` itself once
  its post-Play wait completes - that's the only quit signal it needs.
- **A real, reproducible post-simulation hang, found and worked around (not fixed) during the same
  session**: after `SimulationTestRunner` finishes and logs its "Sanity check complete" summary,
  returning to Edit mode triggers Unity's own asset/Search re-indexing ("Start Indexing on Editor
  startup") - which was observed to hang indefinitely (CPU time climbing into the tens of minutes,
  sometimes over an hour, with zero further indexing progress in the log) on this environment, twice
  in a row, independent of which scenario/turn-count was run. Since the simulation results are fully
  written to the log file well before this point, the practical workaround is to watch the log for
  the "Sanity check complete" (or matrix-mode equivalent) line rather than waiting for the Unity
  process to exit on its own, then force-close it once that line appears. This is a real, unresolved
  limitation of running `BatchSimulationRunner` in this environment, not a simulation-code bug - it
  has no bearing on the correctness of any validation result obtained this way.
- **Standing note: this hang has now recurred a 3rd time (during "Demographics, Part A"'s two
  same-day corrections), with a DIFFERENT apparent trigger each time** - once with no preceding file
  change at all, ruling out "deleted asset reconciliation" (a theory floated after the 2nd occurrence)
  as any kind of reliable cause. Across all three occurrences the trigger has varied while the symptom
  hasn't (post-Play "Start Indexing on Editor startup", CPU climbing with zero further log progress) -
  this now looks like a genuinely intermittent Unity Editor behavior in this environment, not tied to
  any single specific cause found so far. **Standard response going forward, to avoid re-diagnosing
  this from scratch every time it recurs**: after any batch run, verify via `Get-Process -Name Unity`
  (and `Unity.PackageManager`/`UnityPackageManager`) whether the process(es) actually exited before
  assuming a hang needs investigating - don't trust a bash-side `pgrep`/process-name check alone, since
  `pgrep -f` was found NOT to reliably match a Windows-launched process's command line (a false
  "already exited" reading, corrected by checking `Get-Process` directly). Then check the log for
  whether all scenarios already finished (`grep -c "Sanity check complete"` should equal the expected
  count, e.g. 24 for a full `-runmatrix`) - the simulation data is written well before this hang point,
  so it's very likely already fully present. If so, just kill the process(es), confirm via `Get-Process`
  that nothing Unity-related is left running, and move on with the data already captured - do not spend
  further time root-causing the hang itself each occurrence; that investigation has been repeated three
  times now without converging on a single fixable cause.
- **A plausible contributing factor, noted but not yet investigated**: force-killing a hung Unity
  process (the workaround above) leaves a `Temp/__Backupscenes/0.backup` behind, which the next
  launch reloads on startup - observed during the UI revamp's Phase 2 screenshot attempts (automated
  windowed, non-batch Play-mode runs, driven via a temporary Editor script), where the same "Start
  Indexing on Editor startup" hang recurred across five consecutive attempts, at inconsistent points
  in the lifecycle (sometimes before Play mode even started), independent of scenario workload -
  including a run seeded with only 1 turn instead of 100, which hung at the identical point, ruling
  out log/data volume as the cause. Whether the repeated force-kill -> backup-scene-reload cycle is
  actually contributing (versus coincidence) is unconfirmed - worth investigating if automated
  (non-batch, windowed) screenshot capture is needed again later. Not blocking: manual screenshots
  taken by opening the Editor normally work fine, and this has no bearing on `BatchSimulationRunner`'s
  own batch-mode validation runs, which don't exhibit this issue.
- **Standing note: a recurring stale/duplicate scheduled-wakeup prompt pattern, now seen twice, both
  caught correctly** - not a Unity issue, a session-tooling one. A `ScheduleWakeup` prompt scheduled to
  check on some in-flight background work can arrive again, verbatim, AFTER that work has already been
  completed and reported (once during the circular Policy Web/central-bank-identity round, once during
  Cabinet's own `cabinetstress` diagnostic comparison) - indistinguishable at a glance from a genuine
  new request, since the text is identical to a real instruction. **Standard response**: before acting
  on an incoming prompt that reads like "check task X, then do Y," check whether task X's result has
  already been reported in this same conversation - if so, it's the stale duplicate, not new work.
  Recap what was already found/done in a sentence or two rather than silently re-running the checks or
  (worse) fabricating a second, possibly-different-sounding result. Do not treat the duplicate as
  authorization to redo already-validated work, and do not skip acknowledging it either - a brief,
  accurate recap is the correct response both times this has occurred so far.

## Expanded Event Pool
Queue item 1 of `ROADMAP_BRIEF.md` (the standing autonomous-work brief added this session): grows
`EventSystem.EventPool` from 8 to 24 entries with real, varied economic/political events, keeping
every new entry's `GdpShockPercent`/`InflationShockPoints`/`ApprovalEffect` within the existing 8's
own envelope (`GdpShockPercent` in `[-2.5, +1.5]`, `InflationShockPoints` in `[-0.4, +1.5]`,
`ApprovalEffect` in `[-5, +3]`) - deliberately not map/geographic/severity-tagged yet, per the brief's
explicit scope for this item (that's a separate, later task once this larger pool is validated).

- **The 16 new events**, grouped by real-world category (each a plain `EconomicEvent`, same shape as
  the original 8 - no new fields, no tagging): financial/banking (Banking Sector Stress, Sovereign
  Credit Rating Downgrade, Stock Market Rally), security (Major Cyberattack on Financial
  Infrastructure, Regional Conflict Disrupts Trade Routes), climate/agriculture (Severe Drought Hits
  Agricultural Output, Bumper Harvest), public health (Public Health Emergency, Medical Breakthrough -
  distinct from the existing "Technology Breakthrough"), labor (Major Labor Strikes), housing (Housing
  Market Correction), investment/trade (Major Foreign Investment Announcement, Tourism Boom, Natural
  Resource Discovery, Successful Multilateral Trade Summit), and political (Corruption Scandal Rocks
  Government - the largest approval-only hit, at the existing -5 floor, with a deliberately small GDP
  effect since a scandal's damage is overwhelmingly reputational, not immediately economic).
- **Real-data grounding for magnitude, not just event realism**: web research confirmed the 1973 oil
  shock added roughly 9 percentage points to US inflation (1972's 3.4% to 1974's 12.3%) and the 2008
  crisis contracted OECD GDP at a ~7-8% annualized rate in a single quarter - both far larger than
  this game's existing event envelope. This confirms the original 8 events' "small and bounded" scale
  was already a deliberate compression of real-world shock magnitudes for gameplay pacing, not an
  attempt at 1:1 realism - so every new event was calibrated to match that same existing envelope
  rather than the larger real-world magnitudes that inspired its *type*, consistent with how every
  other illustrative/stylized figure in this codebase is honestly labeled as compressed-for-gameplay
  rather than presented as researched-to-scale.
- **Ported to the standalone harness first** (`SimHarness/Program.cs`, outside this repository) for
  fast-iteration checking before real-Unity validation, per the brief's rule 1.
- **Validated in real Unity** (`BatchSimulationRunner`, both 100 and 500 turns, no-policy baseline):
  100 turns landed at 25-34 anomalies across two runs (re-run once after an unrelated Unity hang
  required killing and relaunching the process - see the new practical-notes bullets in "Real-Unity
  Validation is the Standard Path" above), 500 turns landed at 104 anomalies - every single one a
  known inflation/unemployment/debt-ratio/interest-rate percent-swing false positive on a
  small-magnitude value (the same false-positive pattern already documented in "Federal Reserve Rate
  Damping" above), not a genuine divergence. Zero NaN, zero negative GDP, zero negative
  GovernmentDebt, zero out-of-range Unemployment/Inflation/PovertyRate anywhere in either run. USA's
  500-turn `DebtToGdpRatio` (143.4%) and every other country's (Sweden 13.3%, Germany 35.4%, France
  91.8%, Italy 107.0%, Poland 25.9%) landed within a few tenths of a percentage point of the
  standalone harness's own 500-turn figures, confirming the ported pool didn't disturb the
  "Fiscal Reaction Function" equilibria. None of `ROADMAP_BRIEF.md`'s four named failure patterns
  appeared: no turn-1 discontinuity beyond the pre-existing, already-documented one; no oscillation;
  no unbounded/compounding growth (GDP growth rates at turn 500 ranged from -1.70% to +4.44%, all
  ordinary); no bimodal attractors (six distinct, moderate debt-to-GDP levels, not two extremes).
- **Validated: 2026-07-22, 100/500 turns, real Unity, 25-34/104 anomalies (all known swing
  false-positives, zero NaN/negative/out-of-range/divergence).**

## Labor Market Basics
Queue item 2 of `ROADMAP_BRIEF.md`: adds `LaborForceParticipationRate` as a tracked stat and a
minimum-wage policy lever with small, real-world-grounded effects on `Unemployment` and
`PovertyRate` (both already existed - reused, not duplicated). Deliberately does NOT build the full
labor market system (union membership, gig economy, remote work) - out of scope for this pass.

- **`EconomyState.LaborForceParticipationRate`**: seeded per country from real World Bank/OECD
  "total population ages 15+" figures (USA 62.5%, Sweden 72.6%, Germany 61.7%, France 56.0%, Italy
  49.8%, Poland 58.5% - Sweden highest and Italy lowest among the six, matching well-documented
  OECD rankings). `Country.BaselineLaborForceParticipationRate` is a separate structural anchor
  seeded to the same figures (the same "avoid a turn-1 shock" idiom `BaselinePovertyRate`/
  `ComfortableDebtToGdpPercent` already established) - a new game opens with the stat already at (or
  very near) its own baseline.
- **`MacroSystem.ApplyLaborForceParticipationRate`**: mean-reverts toward
  `Country.BaselineLaborForceParticipationRate`, adjusted by the SAME `Unemployment`-versus-NAIRU gap
  that already drives `ApplyApprovalRating`'s misery index and `ApplyPovertyRate`'s baseline (a
  discouraged/encouraged-worker effect) - reusing an already-proven driver rather than inventing a
  new one, per the task's own instruction. A tracked stat only - nothing currently targets it
  directly with a policy lever. Hard-clamped to `[0, 100]`.
- **Minimum wage lever** (`Country.MinimumWageImplemented` / `MinimumWagePercentOfMedian`): expressed
  as a percent of median wage (the "Kaitz index" economists use for cross-country comparison, e.g.
  France's real minimum wage is ~66% of its median), not an absolute currency amount, so it's
  comparable across countries with very different wage levels. Seeded real-approximate for the four
  countries with a statutory minimum wage - USA 29%, Germany 55%, France 66%, Poland 52% - and
  **not implemented at all for Sweden or Italy**, matching real-world fact (both rely on
  sector-level collective bargaining instead of a legal minimum, per OECD/Eurostat sourcing). This
  asymmetry mirrors existing precedent in this codebase (`CarbonTax` implemented only for Sweden,
  `VAT` not implemented for the USA) rather than inventing a new pattern - not escalated to Open
  Questions since it directly follows an established idiom, not a genuinely novel design choice.
  `PolicyDecision.MinimumWageOverride` (an absolute target, the same "SET, not delta" semantics as
  `TaxRateOverrides`) lets the player adjust the level turn to turn via
  `SimulationManager.ApplyMinimumWageChange`, clamped to `[0, 100]` - there is no implement/remove
  action (unlike `TaxLine`/`WelfareProgram`): whether a country has a statutory minimum wage at all
  is a structural fact, not a player choice.
- **Effects measured against each country's OWN seeded baseline, not a universal reference point**:
  `Country.BaselineMinimumWagePercentOfMedian` is seeded equal to each country's own starting
  `MinimumWagePercentOfMedian` (again mirroring `ComfortableDebtToGdpPercent`/`BaselinePovertyRate`).
  Both new effects are driven by the GAP between the current level and this country-specific
  baseline, not a single universal "neutral" percentage - deliberately, for two reasons: it avoids a
  turn-1 discontinuity (a fresh game opens at zero gap), and it avoids double-counting against
  `NaturalUnemploymentRate`, which already reflects each country's real structural conditions
  including its actual minimum wage.
  - **Unemployment** (`MacroSystem.GetMinimumWageUnemploymentAdjustment`, folded into
    `ApplyOkunsLaw` as an ongoing stock effect of the current level, not a one-time shock): a
    minimum wage above baseline nudges `Unemployment` up a little; below baseline, down a little.
    Directionally grounded (not precisely fitted) by the CBO's 2019 estimate that a federal $15/hr
    minimum wage - raising the effective Kaitz index roughly 20-30 points - would cost a
    median-estimate ~1.3 million jobs against a ~160 million labor force (~0.8%), a modest, debated,
    real-world-scale effect, not a dominant driver of `Unemployment` the way the growth gap is.
  - **PovertyRate** (`MinimumWagePovertyReductionSensitivity`, folded into `ApplyPovertyRate`'s
    baseline the same way a `WelfareProgram`'s reduction is): a minimum wage above baseline reduces
    the poverty baseline; below it, increases it. Smaller than the welfare programs' own
    sensitivities (5 vs. 7-8 for UBI/MeansTestedWelfare) - directionally grounded by the same CBO
    citation, which found the $15/hr minimum wage would lift roughly as many people out of poverty
    as it cost in jobs (~1.3 million each) - a modest effect since a minimum wage only reaches
    low-wage workers, not the whole poor population the way a direct transfer does.
- **UI** (`GameController`): `LaborForceParticipationRate` shown on the dashboard (and in the live
  preview, via a new `PolicyPreview.LaborForceParticipationRateChange` field, for consistency with
  every other tracked stat already getting one). The minimum wage slider lives directly in
  `DrawPolicyControls` (left column, always visible) rather than its own tab - unlike Tax/Spending/
  Welfare Policy's multi-item portfolios, this is a single lever, so a dedicated tab would be
  disproportionate scope for one slider (per the brief's "scope small" rule). Shows a read-only note
  instead of a slider for a country with no statutory minimum wage.
- **Validated in the standalone harness first** (100/500-turn baseline, plus a new
  `--minwagestress` scenario pushing USA's level from its 29% seed to 90% at turn 1 and holding):
  the baseline stayed numerically identical in character to pre-existing runs (no new anomaly
  pattern), and the stress scenario produced a real, bounded, one-time `Unemployment` jump (+1.57
  points) and gradual `PovertyRate` decline (~1.7 points by turn 100) that settled rather than
  diverging - the turn-1 jump is a deliberate, adversarial single-turn policy swing (29% to 90% Kaitz
  index in one turn is not something a real government would do gradually), the same kind of
  expected extreme-policy behavior already documented for tax hikes and welfare stress, not a bug -
  the actual no-policy baseline (the real turn-1-discontinuity concern) opens at zero gap by
  construction and shows no such jump.
- **Validated: 2026-07-22, 100/500 turns, real Unity, 23/106 anomalies (all known swing
  false-positives, zero NaN/negative/out-of-range/divergence)** - `DebtToGdpRatio` equilibria for
  all six countries matched the pre-existing "Fiscal Reaction Function" baseline almost exactly
  (USA ~142-143%, Sweden ~13%, Germany ~35%, France ~90%, Italy ~107%, Poland ~26%), confirming the
  new mechanic introduced no interaction with the debt/fiscal-reaction system.

## Crime & Justice Basics
Queue item 3 of `ROADMAP_BRIEF.md`: adds a stylized `CrimeIndex` tracked stat plus two policies -
police funding and sentencing policy, per the brief's own suggestion - with effects kept to
`ApprovalRating` and `BusinessConfidence` (both already proven elsewhere in this model).

- **`EconomyState.CrimeIndex`** (0-100, higher = more crime): a **stylized index, NOT a literal
  transformation of any single real indicator** - "crime" as a broad concept has no single clean
  cross-country comparable metric the way poverty/labor-participation rates do (homicide rate,
  victimization surveys, and recorded-offense rates all measure different things and aren't
  mutually consistent across these six countries). Seeded **informed by** real relative
  intentional-homicide-rate rankings (UNODC/Eurostat/national sourcing): USA 5.76/100k (clearly
  highest of the six) -> `CrimeIndex` 45; Sweden and France both notably elevated and roughly
  comparable (Sweden's recent, well-documented gang-violence-driven rise; France ~1.34/100k) ->
  both 30; Germany ~0.91/100k -> 25; Poland ~0.68/100k -> 20; Italy ~0.57/100k, the lowest of the
  six -> 18. `Country.BaselineCrimeIndex` is a separate structural anchor seeded to the same
  figures (the same "avoid a turn-1 shock" idiom `BaselinePovertyRate`/
  `BaselineLaborForceParticipationRate` already use).
- **`MacroSystem.ApplyCrimeIndex`**: mean-reverts toward a target of `Country.BaselineCrimeIndex`,
  adjusted by (a) the same Unemployment-versus-NAIRU gap already reused elsewhere (property crime's
  real, well-documented link to joblessness - a modest sensitivity, smaller than the policy levers
  below), and (b) how far `PoliceFundingLevel`/`SentencingSeverity` sit from their shared neutral 50.
  **Police funding's effect is deliberately double sentencing severity's** (0.16 vs. 0.08 points per
  point of gap) - the well-established criminology finding (Nagin and others) that the CERTAINTY of
  enforcement deters crime more reliably than the SEVERITY of punishment, which has a smaller, more
  debated effect. Hard-clamped to `[0, 100]`.
- **Two policy dials, uniform across all six countries** (unlike the minimum wage's country-specific
  asymmetry - there's no real-world "relative policing effort" figure to seed differently per
  country, so both start at a neutral 50 for every country): `Country.PoliceFundingLevel` (0-100)
  and `Country.SentencingSeverity` (0 = lenient/rehabilitation-focused, 100 = harsh/punitive).
  `PolicyDecision.PoliceFundingOverride`/`SentencingSeverityOverride` (absolute targets, the same
  "SET, not delta" semantics as `TaxRateOverrides`) let the player adjust both turn to turn via
  `SimulationManager.ApplyCrimePolicyChanges`, each clamped to `[0, 100]`. Deliberately NOT wired
  into the budget/fiscal system at all - the brief's explicit scope for this item is `ApprovalRating`/
  `BusinessConfidence` only, keeping this self-contained ahead of the riskier, fiscal-touching
  Sovereign Wealth Fund item still to come.
- **Effects, both GAPS versus `Country.BaselineCrimeIndex` (not absolute levels)** - the same "gaps,
  not levels" idiom `ApplyApprovalRating`/`ApplyPovertyRate` already use, so a country with a
  structurally higher real-world baseline (the USA) isn't penalized just for sitting at its own
  normal equilibrium:
  - **`ApplyApprovalRating`** gained a new `CrimeApprovalSensitivity` (0.2) term in the existing
    misery-penalty calculation - smaller than `UnemploymentApprovalSensitivity`/
    `InflationApprovalSensitivity` (both 0.4) since `CrimeIndex` gaps tend to run larger in absolute
    point terms on its 0-100 scale. `country` was already a parameter, so no signature change was
    needed.
  - **`MacroSystem.ApplyCrimeEffects`** (a new method, called alongside
    `ApplyCategorySpendingEffects`/`ApplyWelfareProgramEffects`): nudges `BusinessConfidence` down as
    `CrimeIndex` rises above baseline (and up as it falls below) - higher-than-baseline crime
    deterring investment is a real, well-documented effect. Small (`CrimeBusinessConfidenceSensitivity`
    = 0.0015 per point of gap) and clamped to `[MinConfidence, MaxConfidence]` alongside every other
    confidence nudge in this model.
- **UI** (`GameController`): `CrimeIndex` shown on the dashboard and in the live preview (a new
  `PolicyPreview.CrimeIndexChange` field). Both sliders live directly in `DrawPolicyControls`
  (`DrawCrimeJusticeControls`), the same "single/small lever(s), not a whole tab" reasoning the
  Minimum Wage slider already established - two sliders is still disproportionately small scope for
  a dedicated tab.
- **Validated in the standalone harness first** (100/500-turn baseline, plus a new `--crimestress`
  scenario pushing USA's Police Funding AND Sentencing Severity to their maximum (100) simultaneously
  at turn 1 and holding): the baseline stayed consistent with pre-existing runs (no new anomaly
  pattern), and the stress scenario produced a real, bounded, sensible outcome - Approval rose
  substantially (to 84.1 by turn 100, vs. 26.7 in the equivalent-seed baseline run) and GDP was
  measurably higher too (via the `BusinessConfidence` channel feeding Investment) - both moved in the
  economically sensible direction and settled rather than diverging.
- **Validated: 2026-07-22, 100/500 turns, real Unity, 32/86 anomalies (all known swing
  false-positives, zero NaN/negative/out-of-range/divergence)** - `DebtToGdpRatio` equilibria for all
  six countries matched the pre-existing "Fiscal Reaction Function" baseline almost exactly (this
  mechanic never touches the fiscal system, so this is the expected, confirmed result, not a
  coincidence).

## Economic Sectors
Queue item 4 of `ROADMAP_BRIEF.md` - explicitly framed there as a proof-of-pattern pass, not the
full theoretical sector system: four sectors (`SectorType`: Manufacturing, Technology, Agriculture,
Finance - the brief's own suggested list, chosen for clear, distinct real-world profiles), each
tracking Output (% of GDP), Employment (% of workforce), and one sector-specific metric, plus two
sector policies (subsidy, regulation - a deliberate reduction from the brief's three suggested
categories; see below for why tariffs was dropped).

- **`Sector`/`SectorType`** (mirrors `TaxLine`/`WelfareProgram`'s pattern): every country has all
  four `Sector`s always (`Country.Sectors`, no implement/remove - unlike `TaxLines`/
  `WelfarePrograms`, sectors aren't optional). Each has `OutputShareOfGdp`, `EmploymentShare`,
  `SectorMetric` (meaning varies by `Type`: Manufacturing -> Capacity Utilization %, Technology -> a
  stylized Innovation Index 0-100, Agriculture -> Export Share % of sector output, Finance -> a
  stylized annual Credit Growth Rate %), plus `SubsidyLevel`/`RegulationLevel` (0-100, both start
  neutral at 50 for every sector/country - the same uniform-placeholder reasoning
  `PoliceFundingLevel`/`SentencingSeverity` already established) and `BaselineX` anchors for all
  three tracked stats (seeded equal to the starting value, the same "avoid a turn-1 shock" idiom
  used throughout this session's work).
- **Real-data grounding, varying by sector** (all disclosed honestly in `WorldFactory`'s seeding
  comment, not glossed over): Manufacturing/Agriculture Output is real World Bank value-added data
  (Manufacturing: USA 10%, Sweden 12.6%, Germany 19.9%, France 10.7%, Italy 16.6%, Poland 18.1%;
  Agriculture: all low single digits, Poland/Italy notably higher among the six). Finance Output is
  partially grounded (USA ~8%, confirmed; the other five are directional estimates). Technology has
  **no clean standard national-accounts category comparable across countries** and is entirely
  stylized, informed by general knowledge of relative tech-sector size (USA/Sweden highest - the
  latter well known for an outsized startup/tech scene relative to its population). Every
  Employment % and sector-specific metric is likewise illustrative throughout, EXCEPT Poland's
  Agriculture Employment (8%), which is real and well-documented - Poland has one of the EU's
  highest shares of agricultural employment relative to its output share, reflecting its more
  fragmented, smallholder farm structure.
- **`MacroSystem.ApplySectorEffects`**: each sector's three tracked stats mean-revert toward their
  own baseline, adjusted by that sector's own Subsidy/Regulation gap versus their shared neutral 50
  (subsidy nudges up, regulation nudges down - applied uniformly to all three stats in this first
  pass rather than a bespoke formula per sector type, keeping the mechanic simple and consistent).
  **Deliberately isolated from GDP/Unemployment/Inflation/ApprovalRating/Confidence entirely** - a
  real, escalated design decision, not an oversight; see `ROADMAP_BRIEF.md`'s Open Questions #1 for
  the full reasoning (in short: avoids double-counting risk against the existing C+I+G+NX identity,
  matches the brief's own "proof-of-pattern" framing, and makes the four named failure patterns
  essentially unreachable by construction - confirmed by validation below).
- **Only 2 of the brief's 3 suggested policy categories implemented** (subsidy, regulation - tariffs
  dropped): country-level tariffs already exist in this model (`Country.BaseTariffRate`,
  per-partner overrides); extending to PER-SECTOR tariffs would require deeper `TradeSystem`
  changes (tariffs currently resolve per country-PAIR, not per-sector) than this proof-of-pattern
  pass's scope - a candidate for a later pass if this validates cleanly, not attempted here.
- **UI** (`GameController`'s new "Economic Sectors" tab): each sector shows its current Output/
  Employment/SectorMetric (read-only, descriptive) plus two always-adjustable sliders (Subsidy/
  Regulation, absolute targets like `TaxLine.Rate` - no implement/remove needed, matching the
  minimum-wage/crime-policy precedent of "every country always has this").
- **Validated in the standalone harness first** (100/500-turn baseline, plus a new `--sectorstress`
  scenario pushing Manufacturing/Technology to max Subsidy + min Regulation and Agriculture/Finance
  to min Subsidy + max Regulation, all simultaneously at turn 1, held): as expected given the
  deliberate isolation, GDP/Unemployment/Approval/etc. were statistically indistinguishable from an
  equivalent-seed baseline run - the stress only moved the sectors' own tracked stats, which stayed
  bounded by construction (linear reversion toward a policy-bounded target - the math cannot diverge
  regardless of dial settings, since the target itself is bounded to baseline ± a fixed maximum).
- **Validated: 2026-07-22, 100/500 turns, real Unity, 23/80 anomalies (all known swing
  false-positives, zero NaN/negative/out-of-range/divergence)** - `DebtToGdpRatio` equilibria
  unchanged from the pre-existing "Fiscal Reaction Function" baseline, exactly as expected given
  this mechanic never touches the fiscal or core simulation loop at all.

## Sovereign Wealth Fund
Queue item 5 of `ROADMAP_BRIEF.md` - the deliberately-last, highest-risk item, since it touches
`GovernmentDebt`/the budget directly (the ordering note's own explicit reasoning: fiscal-touching
mechanics have historically needed more debugging rounds in this project than self-contained ones -
confirmed again by this item's own validation, see below). USA-first only, per the task's explicit
permission - the mechanic itself (`SovereignWealthFund`/`SovereignWealthFundSystem`) is
country-agnostic, so a later pass could enable it elsewhere with no code changes, only seeding.

- **`Country.SovereignWealthFund`** (nullable, every country defaults to null/doesn't exist - the
  same idiom `CurrentFedChair` already uses): the player creates/dissolves it via an immediate
  action (`GameController`'s new "Sovereign Wealth Fund" tab, mirroring `TaxLine.IsImplemented`'s
  toggle pattern), not a `PolicyDecision` field. Holds `TotalAssets` (the fund's size, same $B scale
  as GDP), `ContributionRatePercent` (0-10%, of GDP per turn - a gameplay ceiling, not researched),
  `DomesticAllocationPercent` (0-100%, tracked/displayed but this pass does NOT model differing
  domestic-vs-international returns - a deliberate scope simplification, honestly disclosed, not a
  gap), and four independently-adjustable asset-class weights (Equities/Bonds/Infrastructure/
  RealEstate) that don't need to sum to 100 - `GetNormalizedWeight` divides each by their live sum.
- **`SovereignWealthFundSystem`**: a simple market-return model - each asset class earns a small
  random return per turn, centered on a real long-run average NOMINAL return sourced via web search
  (equities ~9%, informed by developed-market real returns of 6-7% plus ~2-3% average inflation;
  bonds ~4.5%; infrastructure ~7%; real estate ~6%, informed by Norway's GPFG real allocation
  benchmarks and UBS's Global Investment Returns Yearbook), varying within a realistic range
  (equities the most volatile of the four, matching real-world relative volatility ordering; bonds
  the least). Its own isolated `System.Random` - separate from `EventSystem`'s, `UnityEngine.Random`,
  and `GameController`'s `_previewRandom` - mirrors `FederalReserveSystem.GenerateCandidates`'s own
  isolation precedent. `SimulationManager.PreviewTurn` uses the deterministic AVERAGE return instead
  of an actual random draw (via `GetAverageReturnEstimate`), matching `PreviewTurn`'s own documented
  "side-effect-free, deterministic" principle - it never rolls an `EventSystem` event either, for the
  same reason. **This return model was later rebalanced against real Norway GPFG data and given
  genuine downside volatility - see "Sovereign Wealth Fund Return-Model Rebalance" below.**
- **Real fiscal integration** (unlike "Economic Sectors"' deliberate isolation - this item's task
  explicitly requires it): the contribution is a new budget EXPENSE, added into
  `ApplyRevenueAndSpending`'s existing total outflow (alongside `UnemploymentBenefitCost`/
  `InterestOnDebt`/`WelfareCost`) - the same, already-validated pathway, not a new one. Market
  returns are INCOME, added into the same method's revenue figure. Both flow into `GovernmentDebt`
  through the existing, unmodified deficit-accumulation mechanism - this item does NOT touch
  `GetDebtRiskPremium` or `GetFiscalReactionMultiplier` at all, minimizing the risk of interacting
  with those already-hard-won equilibria.
- **Only 2 of the brief's suggested "Create/dissolve, contribution rate, allocation, asset mix,
  market-return model, budget/debt interaction" list needed any real design choice**: the
  domestic/international split (tracked but not differently modeled, see above) and the debt/asset
  interaction (see next bullet).
- **"Display both figures separately," not "net out the debt"** (the task's own explicit
  requirement, to prevent the fund from being used to hide a real fiscal problem): `GovernmentDebt`/
  `DebtToGdpRatio` are completely unchanged - every existing formula that reads them (risk premium,
  fiscal reaction multiplier, the dashboard's own existing display) keeps reading the real, gross
  figure exactly as before. A separate "Net Government Position" (`GovernmentDebt - fund
  TotalAssets`) is computed ONLY in `GameController`, for display ALONGSIDE the gross figure (shown
  on the dashboard and in the SWF tab) - it is never written back into `EconomyState`/`Country` and
  never read by any simulation formula.
- **A real, found-and-fixed unbounded-growth risk - the exact failure pattern the brief warns
  about, caught during this item's own validation, not shipped silently**: a sustained-maximum stress
  test (10% of GDP contributed every turn, 100% Equities allocation, held for 500 turns with no
  rebalancing - `--swfstress` in both the harness and `SimulationTestRunner`) drove USA's cumulative
  `Budget` to an ASTRONOMICALLY large figure (~10^23 - still finite, not NaN/Infinity, but wildly
  unrealistic) within a few hundred turns. Root cause: the fund's average return (9% blended down
  toward equities-heavy) structurally and permanently exceeds trend GDP growth (~2%/turn for USA), so
  a fund compounding via both fresh contributions (scaled to a growing GDP) AND its own returns grows
  UNBOUNDEDLY relative to GDP the longer the game runs - there is no natural equilibrium the way
  `CrimeIndex`/`Sector` stats have (those mean-revert toward a fixed target; this compounds without
  a ceiling). **Fix**: `TotalAssets` is now hard-clamped to `[0, MaxSwfToGdpPercent(300%) / 100 *
  GDP]` every turn, immediately after contributions/returns are applied - mirrors
  `GovernmentDebt`'s own clamp exactly (matching its 300% ceiling number for consistency, a gameplay
  safety bound, not a realistic target) and the same principle behind "SpendingLine Amount Ceiling"'s
  fix (the flow - this turn's contribution/return - is still computed and reported accurately even
  in a turn that hits the ceiling; only the STOCK stops compounding further). Confirmed fixed: the
  same 500-turn `--swfstress` scenario now settles `Budget` at a sane ~1.6-1.9 billion (a completely
  reasonable scale relative to a ~$565B GDP) instead of ~10^23.
- **A realistic (non-adversarial) use case was also checked, not just the adversarial stress test**
  (`--swfmoderate` in the standalone harness: default diversified 40/30/15/15 weights, a modest 1.5%
  contribution rate): stayed fully bounded over 500 turns with no elevated anomaly signature beyond
  ordinary swing noise - confirming the fix doesn't just suppress the extreme case, ordinary play
  was never at risk of the extreme figure either.
- **Full existing regression matrix re-run, per this item's own extra-caution requirement**: the
  brief specifically asked that this item re-run the FULL existing matrix (baseline/stress/
  sustainedexploit/tariffoverride/welfarestress), not just a fund-specific scenario, to catch any
  interaction with the fiscal reaction function or debt attractor. `SimulationTestRunner`'s
  `-runmatrix` was extended with the new `swfstress` scenario (now 6 scenarios x 100/500 turns = 12
  combinations) and re-run in full against real Unity: the five pre-existing scenarios' anomaly
  counts (28/41/31/25/36 at 100 turns; 97/82/101/70/92 at 500) landed squarely within the same range
  already documented for each in this file, confirming zero regression - completely expected, since
  every SWF code path is gated behind `Country.SovereignWealthFund != null`, a no-op for any country/
  scenario that never creates one. `swfstress` itself showed a much higher anomaly count (87 at 100
  turns, 338 at 500) - entirely `DebtToGdpRatio`/`Inflation` percent-swing noise (the same
  known-oversensitive-on-small-numbers pattern already documented in "Federal Reserve Rate Damping"),
  not a single NaN/negative/out-of-range value anywhere across all 12 combinations - a real,
  expected consequence of injecting a volatile, undiversified, maximum-leverage income source
  directly into the fiscal system via deliberately adversarial policy, not a bug (matching the
  Fiscal Reaction Function's own established precedent: "policy-driven extremes still reach 0%/300%,
  by design").
- **A genuinely useful validation shortcut found this session**: `dotnet build PoliSim.slnx` (from
  the project root) compiles the real Unity C# files in seconds using Unity's own auto-generated
  `.csproj`/`.slnx` (gitignored, regenerated by Unity on next open) - much faster than a full
  `BatchSimulationRunner` launch for catching a plain compile error before investing in a real
  simulation run. Newly-created files aren't in the `.csproj` until Unity itself re-syncs it, so a
  brand-new file's own `<Compile Include=...>` entry may need adding by hand for this shortcut to
  see it; Unity overwrites this file anyway on its next batch/Editor launch, so a manual addition is
  harmless and temporary. This is a compile-check shortcut only - it does NOT replace
  `BatchSimulationRunner`/`SimulationTestRunner` for validating actual simulation behavior.
- **UI** (`GameController`'s new "Sovereign Wealth Fund" tab): Create/Dissolve button, then (while it
  exists) `TotalAssets`, this-turn estimated contribution/returns, Net Government Position, and
  sliders for every adjustable setting. Dashboard shows fund assets and Net Government Position too,
  whenever a fund exists, so the "display both figures separately" requirement is visible without
  needing to open the tab.
- **Validated: 2026-07-22, 100/500 turns, real Unity, full 12-combination matrix (6 scenarios x 2
  turn counts), zero NaN/negative/out-of-range/divergence anywhere** - the five pre-existing
  scenarios confirmed regression-free; `swfstress` confirmed the unbounded-growth fix holds under
  the worst-case sustained adversarial policy; a separate `swfmoderate` check confirmed ordinary,
  realistic use was never at risk either.

## Sovereign Wealth Fund Expansion to All Six Countries
Round 2 item 1 of `ROADMAP_BRIEF.md` - "primarily a seeding/calibration task, not new mechanism
design" per the brief's own framing, since `SovereignWealthFund`/`SovereignWealthFundSystem` were
already country-agnostic and the USA-first mechanic's 300%-of-GDP growth ceiling was already
validated in Round 1.

- **A real-world fact drove the seeding split, not a uniform rollout**: none of the six countries
  has a "classic" Norway/Gulf-state-style oil-revenue sovereign wealth fund. Research found exactly
  two real, if more modest, partial analogs worth seeding directly - Sweden's AP pension buffer funds
  (AP1-AP4, AP6 combined) held ~$195B at end of 2024 (~31% of Sweden's real GDP, matching this game's
  Sweden GDP scale) and France's FRR (Fonds de reserve pour les retraites) held ~EUR21-24B (under 1%
  of France's real GDP). `Country.SovereignWealthFund` is now seeded non-null for Sweden and France
  from turn 1; USA, Germany, Italy, and Poland honestly stay null (no real major fund) - USA's is
  still player-creatable via the existing `GameController` tab, unchanged from Round 1. This mirrors
  the exact precedent Round 1's own minimum-wage asymmetry established (Sweden/Italy have no
  statutory minimum wage, matching reality) rather than inventing a new pattern, so it wasn't
  escalated to Open Questions - a research question with a clear answer, not a structurally
  ambiguous one.
- **Allocation mapped from each fund's real, publicly-reported mandate** (illustrative sub-splits
  where the source didn't break out all four of this game's asset classes individually): Sweden -
  real mandate is "equities, fixed-income securities, and a small share of unlisted assets" ->
  Equities 55/Bonds 35/Infrastructure 5/RealEstate 5. France - real allocation is ~46% unhedged
  equities, ~15% unlisted, ~18%+ investment-grade fixed income -> Equities 50/Bonds 35/
  Infrastructure 8/RealEstate 7.
- **Contribution rates are honestly labeled as illustrative, not individually sourced** - Sweden's
  AP funds are a mature, largely stable pension buffer (0.3%/turn, a small illustrative figure, not
  a fast-growing new fund); France's FRR is actually in a real NET DRAWDOWN phase (it stopped
  receiving material new contributions around 2011 and now pays OUT to pension funds annually) -
  since `ContributionRatePercent` can only be non-negative in this model, that real drawdown dynamic
  isn't representable in this pass; a near-zero rate (0.1%) is the closest honest approximation, not
  a claim that FRR is still growing via contributions the way it once did.
- **Market-return assumptions were deliberately NOT forked per country** - a given asset class's
  real long-run return doesn't meaningfully depend on which country's fund holds it (both Sweden's
  and France's real funds invest substantially in global, not purely domestic, markets), so country
  differentiation belongs in ALLOCATION and CONTRIBUTION RATE, which now vary by country, not in
  `SovereignWealthFundSystem`'s return-rate model itself, which stays a single global reference per
  asset class.
- **Validation generalized beyond USA, per this item's own explicit requirement**: the existing
  `swfstress` scenario (both the standalone harness and `SimulationTestRunner`) now ALSO creates an
  equally-maxed fund (10% contribution, 100% Equities) for Germany simultaneously with USA's -
  a Eurozone shared-currency country with a different GDP scale and `ComfortableDebtToGdpPercent`
  anchor than USA's independent-currency setup. Confirmed in real Unity: Germany's `DebtToGdpRatio`
  settles cleanly at exactly 0.0% under the 500-turn max-stress scenario (not negative, not
  divergent), the same correctly-bounded behavior the 300%-of-GDP ceiling fix already produced for
  USA in Round 1 - confirming the ceiling generalizes, not a USA-specific coincidence.
- **A new, now-permanent characteristic of the baseline (not a bug)**: since Sweden and France now
  carry an active, real-data-seeded fund from turn 1 onward, EVERY validation run (not just
  SWF-specific stress scenarios) now exercises live fund contribution/return dynamics for two
  countries. This raised baseline anomaly counts across the board (e.g. 100-turn baseline: 28 anomalies
  before this item -> 59 after; 500-turn baseline: 97 -> 191) - confirmed to be entirely
  `DebtToGdpRatio`/`Inflation` percent-swing noise (the same known-oversensitive-on-small-numbers
  pattern documented since "Federal Reserve Rate Damping"), not a single NaN/negative/out-of-range
  value anywhere. Both Sweden and France settle at `DebtToGdpRatio` = 0.0% by turn 500 even under a
  no-policy baseline (their modest-but-real fund returns still compound enough over 500 turns to pay
  down debt entirely) - the same "fund returns exceed trend GDP growth over a long horizon" dynamic
  Round 1 already established and accepted for USA, now visibly reproducing for two more countries
  under ordinary (non-adversarial) settings.
- **Validated: 2026-07-23, 100/500 turns, real Unity, full 12-combination matrix (6 scenarios x 2
  turn counts), zero NaN/negative/out-of-range/divergence anywhere** - the five pre-existing
  scenarios' elevated-but-explained anomaly counts confirmed no regression; the extended `swfstress`
  scenario (now stressing USA AND Germany simultaneously) confirmed the growth-ceiling fix
  generalizes across countries, not just USA.

## Detailed Spending Portfolio Phase 2
Round 2 item 2 of `ROADMAP_BRIEF.md` - wires real economic effects into 4 more of USA's still
effect-less Discretionary spending categories, following "Detailed Spending Portfolio"'s own
Phase 1 precedent exactly (a one-turn spending change permanently nudges a structural value, the
same "lasting trend" idiom `PotentialGrowthRate`'s own Infrastructure nudge established).

- **Justice -> `Country.BaselineCrimeIndex`** (down): court/prosecution funding genuinely affects
  case backlogs and enforcement capacity - `MacroSystem.JusticeCrimeIndexSensitivity` (0.02 per
  percentage-point-of-GDP) permanently lowers the structural crime baseline, distinct from and
  complementary to `PoliceFundingLevel`'s own larger, dial-based effect (Crime & Justice Basics) -
  federal DOJ/courts funding and state/local police funding are different real things, so this
  doesn't duplicate that mechanic.
- **HomelandSecurity -> `ApprovalRating` only** (`HomelandSecurityApprovalMultiplier` = 0.7,
  between Defense's 0.5 and the 1.0 baseline): mirrors Defense's own "approval only, no growth/
  confidence side-effect" pattern exactly - border security/disaster response/TSA spending is pure
  consumption in the G identity with no additional structural effect in this pass.
- **Energy -> `BusinessConfidence`** (`EnergyConfidenceSensitivity` = 0.0015): lower/stabler energy
  costs for businesses - a distinct nudge from Education's own `BusinessConfidence` effect (a
  different real mechanism, workforce skill vs. input-cost stability), smaller in magnitude than
  Education's 0.002 since the connection is a bit more indirect.
- **Housing -> `Country.BaselinePovertyRate`** (down, `HousingPovertyReductionSensitivity` = 0.015):
  represents HUD's baseline federal housing-support spending, deliberately distinct from and much
  smaller than the player-adjustable `WelfareProgramType.HousingAssistance`'s own dedicated
  poverty-reduction sensitivity (3 points per 100% generosity) - this is a much narrower, less-
  targeted budget line, not a substitute for the dedicated welfare program.
- **Approval multipliers for all 4** (`JusticeApprovalMultiplier`/`EnergyApprovalMultiplier` = 1.0,
  baseline like Infrastructure; `HousingApprovalMultiplier` = 1.3, fairly popular like Healthcare/
  Education though slightly less so) added to the existing weighted-spending approval term
  alongside the original four - illustrative, gameplay-tuning judgment calls, the same as the
  original four categories' own multipliers (never claimed as precisely researched either).
- **Wiring**: `PolicyDecision` gained 4 new fields (`JusticeSpendingChange`/
  `HomelandSecuritySpendingChange`/`EnergySpendingChange`/`HousingSpendingChange`), mapped from the
  corresponding `SpendingCategory` in `BuildEffectiveDecisionForDetailedSpending` the same way the
  original four are. No changes to `ApplyCrimeIndex`/`ApplyPovertyRate`'s signatures were needed -
  the new effects live entirely in `ApplyCategorySpendingEffects` (which already took `PolicyDecision`),
  mutating `Country.BaselineCrimeIndex`/`BaselinePovertyRate` directly, the same place Infrastructure's
  own `PotentialGrowthRate` nudge already lives.
- **Validated in the standalone harness first** (100/500-turn baseline, plus a new `--phase2stress`
  scenario pushing all 4 new categories to their max +30%/turn every turn, sustained for the whole
  run - the same "no reset" stress pattern that originally found the `SpendingLine` compounding bug):
  stayed fully bounded, with anomaly counts in the SAME range as the other scenarios (unlike
  `swfstress`'s elevated count) - confirming these four new effects don't introduce their own
  volatility source the way the SWF's market returns do.
- **Validated: 2026-07-23, 100/500 turns, real Unity, full 14-combination matrix (7 scenarios x 2
  turn counts), zero NaN/negative/out-of-range/divergence anywhere** - the six pre-existing
  scenarios showed no regression; `phase2stress` landed in the same anomaly-count range as
  `stress`/`sustainedexploit`/`tariffoverride`/`welfarestress` (60-211 across both turn counts),
  confirming the 4 new effects are appropriately small and don't destabilize anything.

## Deeper Labor Market Policies
Round 2 item 3 of `ROADMAP_BRIEF.md` - three more labor policies building on
`LaborForceParticipationRate`/the minimum-wage lever from "Labor Market Basics," with small effects
routed through the three already-proven channels the brief named (`LaborForceParticipationRate`/
`Unemployment`/`ApprovalRating`).

- **`Country.PaidFamilyLeaveWeeks`** (real per-country data, weeks): USA 0 (confirmed - the USA is
  the only OECD country with no national statutory paid parental leave), Sweden 69 (confirmed - 480
  days, ~390 at ~80% pay), Germany 58 (confirmed - 14 weeks maternity + 44 weeks parental), Poland 20
  (confirmed, the full-pay portion specifically). France (16) and Italy (22) are directionally-
  informed estimates from general knowledge of each country's real statutory maternity-leave system,
  not individually confirmed to the same search-verified precision as the other four.
  `Country.BaselinePaidFamilyLeaveWeeks` is a structural anchor seeded to the same figures (the same
  "avoid a turn-1 shock" idiom used throughout this session). Unlike MinimumWage's country asymmetry,
  no boolean "implemented" flag was needed - 0 weeks already honestly represents the USA's real
  situation without a separate existence switch.
- **Effects, both gaps versus the country's own seeded baseline** (the same idiom MinimumWage's
  employment effect already established): `PaidFamilyLeaveParticipationSensitivity` (0.02 per week of
  gap) added to `ApplyLaborForceParticipationRate`'s target, and `PaidFamilyLeaveApprovalSensitivity`
  (0.05 per week of gap) added to `ApplyApprovalRating`'s delta as an ongoing stock effect (like
  `GetWelfareApprovalEffect`) - paid leave tends to be popular policy.
- **`Country.OvertimeRegulationLevel`** (0-100, neutral 50 for every country - a uniform placeholder,
  matching `PoliceFundingLevel`'s own precedent, since no clean cross-country "regulation strictness"
  index exists): its `Unemployment` effect (`GetOvertimeUnemploymentAdjustment`, gap versus the
  shared neutral 50) represents the "work-sharing" argument behind France's real 35-hour week -
  **honestly flagged as one side of a genuinely contested real economic debate**, not a settled fact
  (some empirical studies find the 35-hour week didn't meaningfully reduce French unemployment as
  intended), so the sensitivity (0.008) is deliberately small.
- **`Country.RetrainingProgramLevel`** (0-100, neutral 50, same uniform-placeholder reasoning):
  reduces `Unemployment` (`GetRetrainingUnemploymentAdjustment`, sensitivity 0.006, smaller than the
  overtime effect since it's a more indirect mechanism) and modestly increases
  `LaborForceParticipationRate` (`RetrainingParticipationSensitivity` = 0.01) - the well-established
  real economic rationale that retraining eases job transitions and re-engages discouraged workers.
- **A real, honestly-disclosed cascade found during validation, not a bug**: a `--laborstress`
  scenario (Paid Family Leave to its 104-week ceiling, Overtime Regulation and Retraining both to
  100, simultaneously, held for the whole run) pushed Unemployment down enough (to ~2.9%, well below
  NAIRU) that the ALREADY-EXISTING Phillips Curve correctly drove Inflation to its 30% ceiling, which
  in turn drove USA's Fed-chair-driven interest rate (via the Taylor Rule) to its own 15% ceiling -
  a three-ceiling cascade, confirmed in both the harness and real Unity, that stayed fully bounded at
  every stage (no NaN, no divergence past the existing clamps). This is the correct, expected
  consequence of deliberately stacking three separate unemployment-reducing policies at maximum
  strength simultaneously - matching the Fiscal Reaction Function's own established precedent that
  adversarial policy combinations reaching existing extremes is by design, not an instability bug.
- **Validated: 2026-07-23, 100/500 turns, real Unity, full 16-combination matrix (8 scenarios x 2
  turn counts), zero NaN/negative/out-of-range/divergence anywhere** - the seven pre-existing
  scenarios showed no regression; `laborstress` landed in the same anomaly-count range as the other
  non-SWF scenarios (69/220), confirming the three-ceiling cascade above is a correctly-bounded
  extreme, not runaway instability.

## Deeper Crime & Justice
Round 2 item 4 of `ROADMAP_BRIEF.md` - adds `EconomyState.PrisonPopulationRate` (a real,
per-100,000 tracked stat, distinct from `CrimeIndex`'s stylized 0-100 scale) plus two more policy
dials (`BailReformLevel`, `DrugPolicyLevel`), building on "Crime & Justice Basics"' existing
`PoliceFundingLevel`/`SentencingSeverity` precedent.

- **`EconomyState.PrisonPopulationRate`** / **`Country.BaselinePrisonPopulationRate`**: seeded from
  real World Prison Brief / national-statistics incarceration-rate data, per-100,000 population -
  USA 531 (confirmed - by a wide margin the highest of the six, matching the US's well-documented
  status as a global outlier), Germany 72 (confirmed), France 111 (confirmed). Sweden 60 is an
  estimate informed by proximity to its confirmed Nordic peers (Finland ~51, Norway ~57) rather than
  independently confirmed; Italy 92 and Poland 185 are general-knowledge directional estimates, not
  individually confirmed - disclosed honestly, the same "confirmed vs. estimated, stated plainly"
  idiom `PaidFamilyLeaveWeeks` already established for France/Italy. `MacroSystem.
  ApplyPrisonPopulationRate` mean-reverts toward `Country.BaselinePrisonPopulationRate`
  (`PrisonPopulationReversionSpeed` = 0.15, matching `PovertyReversionSpeed`'s "moderate-slow, real
  stats don't swing wildly turn to turn" reasoning), adjusted by the gap between
  `BailReformLevel`/`DrugPolicyLevel` and their shared neutral 50, hard-clamped to `[0,
  MaxPrisonPopulationRate]` (1000 - a generous gameplay ceiling, comfortably above any seeded value
  or plausible drift).
- **`Country.BailReformLevel`** (0 = traditional cash bail, 100 = full reform) and **`Country.
  DrugPolicyLevel`** (0 = decriminalized, 100 = strict criminalization): both neutral-50 uniform
  dials for every country (the same `PoliceFundingLevel`/`SentencingSeverity`/
  `OvertimeRegulationLevel` precedent - no clean cross-country "how reformed is this country's bail
  system" index exists to seed differently per country, even though cash bail is most directly a US
  policy concept). `PolicyDecision.BailReformOverride`/`DrugPolicyOverride` (absolute targets, the
  same "SET, not delta" semantics as every other policy dial) let the player adjust both via
  `SimulationManager.ApplyCrimeJusticeDeeperChanges`, reusing the existing `MinPolicyDialLevel`/
  `MaxPolicyDialLevel` bounds rather than adding new ones.
- **Effects, both gaps versus the neutral 50** (the same idiom `PoliceFundingLevel`/
  `SentencingSeverity` already use in `ApplyCrimeIndex`):
  - `BailReformLevel` feeds BOTH `ApplyCrimeIndex` (a new `BailReformCrimeIndexSensitivity` = 0.02
    term - more reform nudges `CrimeIndex` up slightly) AND `ApplyPrisonPopulationRate`
    (`BailReformPrisonPopulationSensitivity` = 2.0 - more reform reduces incarceration, the direct,
    well-documented mechanical effect of reducing pretrial detention). **The `CrimeIndex` effect is
    HONESTLY DISCLOSED as one side of a genuinely contested real criminological debate, not a
    settled fact** - mirroring the "Deeper Labor Market Policies" precedent for
    `OvertimeRegulationLevel`'s unemployment effect: some real research finds bail reform has no
    measurable effect on crime rates, while other studies (and the political arguments against
    reform) claim an increase from releasing more defendants pretrial. The sensitivity is
    deliberately small and clearly commented as contested, not presented as a confident modeling
    choice.
  - `DrugPolicyLevel` feeds `ApplyPrisonPopulationRate` (`DrugPolicyPrisonPopulationSensitivity` =
    1.6 - stricter criminalization raises incarceration, the direct, well-documented effect of
    drug-offense sentencing on prison populations) and `ApplyApprovalRating`
    (`DrugPolicyApprovalSensitivity` = 0.02, an ongoing stock effect like every other policy dial's
    approval term - deliberately small and directionless in framing, since real-world public opinion
    on drug policy is itself split and country-dependent, not a one-sided popularity lever).
- **UI** (`GameController`'s existing `DrawCrimeJusticeControls`, extended with 2 more sliders
  alongside Police Funding/Sentencing Severity - the same "small dials belong in the left-column
  policy panel, not a dedicated tab" reasoning already established for Minimum Wage/Crime & Justice
  Basics): `PrisonPopulationRate` shown on the dashboard as "Incarceration Rate: X per 100,000".
- **Validated in the standalone harness first** (100/500-turn baseline, plus a new
  `--crimejusticestress` scenario pushing USA's `BailReformLevel` AND `DrugPolicyLevel` both to 100
  simultaneously at turn 1 and holding - deliberately pulling `PrisonPopulationRate` in opposite
  directions at once, since bail reform pushes it down while strict drug enforcement pushes it up,
  stress-testing both effects and `CrimeIndex`/`ApprovalRating` interactions together): stayed fully
  bounded, no NaN/negative/out-of-range values, no regression in the existing `sustainedexploit`
  scenario.
- **Validated: 2026-07-23, 100/500 turns, real Unity, full 18-combination matrix (9 scenarios x 2
  turn counts), zero NaN/negative/out-of-range/divergence anywhere** - the eight pre-existing
  scenarios showed no regression (anomaly counts consistent with their previously-documented ranges);
  `crimejusticestress` landed at 65/178 anomalies, squarely within the same range as the other
  non-SWF scenarios (51-72 at 100 turns, 178-215 at 500 turns) - confirming no new instability.
  USA's `DebtToGdpRatio` settled at ~144-147% under `crimejusticestress`, matching the pre-existing
  Fiscal Reaction Function equilibrium (~142%) almost exactly - expected, since neither
  `BailReformLevel` nor `DrugPolicyLevel` touches the fiscal system at all.

## Infrastructure System
Round 2 item 5 of `ROADMAP_BRIEF.md` - the deliberately-last, most novel item this round (a
decay/maintenance mechanic has no direct Round 1 precedent to mirror). Adds `InfrastructureAsset`
(Roads/Rail/PowerGrid/Broadband - the brief's own suggested 4-type list), a 0-100 `ConditionIndex`
per type per country, always present for all six countries (the same "no implement/remove" idiom
`Sector` already established).

- **A stock model, not a gap-to-baseline model** (`InfrastructureAsset.cs`): unlike `CrimeIndex`/
  `PovertyRate`/`Sector` (which mean-revert toward a seeded baseline anchor), `ConditionIndex` moves
  via two flows every turn - a small constant decay (`InfrastructureDecayRatePerTurn` = 0.08,
  representing deferred maintenance: infrastructure needs growing real investment merely to hold
  steady, so a flat spending level still implies gradual real degradation) minus an investment term
  (`InfrastructureInvestmentSensitivity` = 6, points gained per percentage-point-of-GDP this turn's
  Infrastructure spending change represents) - then hard-clamped to `[0, 100]` every turn. This
  mirrors "SpendingLine Amount Ceiling"'s own precedent (a stock that moves via flows, hard-clamped to
  a fixed range) more closely than the mean-reversion idiom used elsewhere this session, since a
  decay/investment mechanic is fundamentally a stock, not an equilibrium-seeking value - the hard
  clamp is what directly satisfies this round's own ordering note, which asked for real attention to
  "unbounded growth/decay with no floor or ceiling" specifically for this item.
- **No new player-facing lever** (`MacroSystem.ApplyInfrastructureCondition`): the investment signal
  reuses `decision.InfrastructureSpendingChange` - the EXACT same `PercentOfGdp` figure
  `ApplyCategorySpendingEffects` already computes for its `PotentialGrowthRate` nudge - per the task's
  explicit "connect to the existing category... rather than inventing a parallel system" instruction.
  For USA this is the Transportation `SpendingLine`'s this-turn change; for the other five countries
  it's the legacy Infrastructure category-delta slider, exactly as `PotentialGrowthRate`'s own nudge
  already reads it. No new `PolicyDecision` field was needed for this item.
- **Real-data grounding** (`WorldFactory.SeedInfrastructure`): `ConditionIndex` is seeded from the IMD
  World Competitiveness Ranking's Infrastructure factor (0-100 scale, 2026 edition, confirmed via web
  search) as each country's overall anchor - USA 73.7, Sweden 81.8, Germany 67.7, Italy 58.1, Poland
  57.3; France's overall score wasn't found in the same source and is a directional estimate (66)
  positioned between Germany and Italy, honestly disclosed as such. Per-type values are illustrative
  estimates anchored to that real country-level score, except a handful of well-documented real
  divergences called out explicitly: USA Roads 55 (ASCE's 2025 Infrastructure Report Card grades US
  roads D+) and Rail 80 (ASCE grades rail B-, particularly strong freight) both diverge from USA's own
  anchor; USA PowerGrid 62 reflects ASCE's Energy grade (D+, citing capacity constraints from
  electrification and data-center demand); Sweden Broadband 90 reflects its real, well-documented
  OECD/ITU leadership in fiber/broadband penetration; Germany Rail 62 reflects Deutsche Bahn's real,
  widely-reported reliability decline, and Germany Broadband 68 reflects Germany's real, widely-
  reported lag in fiber rollout relative to its economic strength; France Rail 78 reflects the TGV
  network's real international reputation. Italy and Poland have no single well-documented divergence
  found for any one type - all four illustrative, anchored near each country's own real overall score
  (Poland Roads 60 reflects its real, well-documented major highway investment since EU accession).
  The full 6x4 matrix isn't independently sourced cell-by-cell - the same "confirmed anchor,
  illustrative breakdown" honesty standard Economic Sectors already established for Finance/
  Technology.
- **UI** (`GameController.GetInfrastructureSummaryLine`): a single dashboard line ("Infrastructure
  Condition: Roads 55 | Rail 80 | PowerGrid 62 | Broadband 74") - no dedicated tab or new sliders,
  since this stat has no new player-facing dial of its own (it's driven entirely by the existing
  Infrastructure spending category), the same "no dedicated tab needed for a lever with no new
  control" reasoning already applied to Minimum Wage/Crime & Justice's smaller additions.
- **Deliberately isolated from GDP/Unemployment/ApprovalRating/Confidence, escalated to Open
  Questions rather than decided silently** (`ROADMAP_BRIEF.md`'s Open Questions #2): `ConditionIndex`
  has zero feedback into the core simulation loop in this pass, mirroring Economic Sectors' own
  Round 1 isolation decision (Open Questions #1) for the same reason - a `ConditionIndex ->
  PotentialGrowthRate` (or `-> BusinessConfidence`) effect would double-count the exact same
  Infrastructure-spending signal that already nudges `PotentialGrowthRate` directly. Not resolved
  here; flagged for Elias alongside Open Questions #1, since both mechanics now face the identical
  "stay isolated to avoid double-counting" design fork.
- **Validated in the standalone harness first** (100/500-turn baseline, plus a new
  `--infrastructurestress` scenario pushing USA's Transportation spending DOWN 30%/turn EVERY turn,
  sustained for the whole run with no reset - the worst-case "zero investment, pure decay" path,
  deliberately covering the case the existing `sustainedexploit` scenario's sustained +30%
  Transportation push doesn't, since that already exercises the opposite, ceiling-bound case): stayed
  fully bounded, zero `InfrastructureAsset` anomalies (finite/range) across the full 500-turn run, no
  regression in the existing `sustainedexploit` scenario.
- **Validated: 2026-07-23, 100/500 turns, real Unity, full 20-combination matrix (10 scenarios x 2
  turn counts), zero NaN/negative/out-of-range/divergence anywhere** - the nine pre-existing scenarios
  showed no regression (anomaly counts consistent with their previously-documented ranges);
  `infrastructurestress` landed at 65/215 anomalies, squarely within the same range as the other
  non-SWF scenarios (58-72 at 100 turns, 150-232 at 500 turns) - confirming no new instability, and
  zero `InfrastructureAsset.ConditionIndex` anomalies specifically anywhere in the matrix. USA's
  `DebtToGdpRatio` settled at ~140-142% under `infrastructurestress`, matching the pre-existing Fiscal
  Reaction Function equilibrium almost exactly - confirming the sustained Transportation spending cut
  doesn't meaningfully disturb the fiscal system beyond what the reaction function already absorbs.

**`ROADMAP_BRIEF.md`'s Round 2 queue is now complete** (2026-07-23): Sovereign Wealth Fund expansion
to all six countries, detailed spending Phase 2, deeper labor market policies, deeper crime &
justice, and the Infrastructure system - each implemented, grounded in real data where available
(honestly labeled where not), validated via the standalone harness first and then
`BatchSimulationRunner` against real Unity at 100 and 500 turns, and committed as its own commit with
validation results in CLAUDE.md. A second design decision was escalated rather than resolved
silently (`ROADMAP_BRIEF.md`'s Open Questions #2 - whether Infrastructure Condition should feed back
into the core simulation loop, the same class of question Open Questions #1 already raised for
Economic Sectors). See `ROADMAP_BRIEF.md` for the full queue history and both escalated questions.

## Infrastructure Feedback
A follow-up task, once Elias resolved `ROADMAP_BRIEF.md`'s Open Questions #2 ("Resolved by Elias:
FEED BACK — ConditionIndex should nudge PotentialGrowthRate"). Explicit design constraint from the
task: do NOT decompose the GDP identity or labor/unemployment accounting to literally incorporate
Infrastructure/Sector values (the approach that caused turn-1 mismatches, the PotentialGDP
recalibration, and the bimodal debt problems earlier this project) — instead, work as a small,
bounded nudge onto an EXISTING proven variable (`PotentialGrowthRate`), the same pattern every
mechanic since has used.

- **A refactor from direct mutation to a recomputed-every-turn value**: `PotentialGrowthRate` was
  previously mutated directly and permanently by the Infrastructure-spending nudge in
  `ApplyCategorySpendingEffects` (`country.PotentialGrowthRate = Clamp(country.PotentialGrowthRate +
  sensitivity * spendingPercent, 0, 8)`) - the only ceiling was the shared, generous global
  `MaxPotentialGrowthRate` (8). This task required a SECOND source (ConditionIndex) to also affect
  the same variable, with an explicit combined ceiling on the TOTAL infrastructure-related effect -
  not reachable by two independent, separately-clamped mutations of the same field. Fixed by
  splitting the concept into three pieces:
  - **`Country.BasePotentialGrowthRate`**: the country's ORIGINAL, immutable, seeded trend growth
    rate (seeded in `WorldFactory` equal to each country's real `potentialGrowthRate` constructor
    argument - USA 2.0%, Sweden 1.5%, Germany/France/Italy 0.8%, Poland 3.5%) - never mutated by any
    policy/condition effect, the fixed reference point every adjustment is measured against.
  - **`Country.InfrastructureSpendingGrowthAdjustment`**: the accumulator the OLD mechanic used to
    mutate `PotentialGrowthRate` directly now targets instead - a lasting, ratcheting investment
    effect (only ever non-negative, since spending can only ever help, never hurt, this specific
    channel), clamped to its own `[0, MaxInfrastructureSpendingBoost]` (0 to 1 point) range.
  - **A live, non-accumulating condition-drag**, computed fresh every turn from the CURRENT average
    `ConditionIndex` across all four `InfrastructureAsset`s versus `InfrastructureConditionGrowthThreshold`
    (50 - the natural midpoint of the 0-100 `ConditionIndex` scale, and the same "50 = neutral"
    convention already used throughout this codebase's policy dials (`PoliceFundingLevel`,
    `SentencingSeverity`, `OvertimeRegulationLevel`, etc.) - chosen so no country's seeded
    `ConditionIndex` (all >= 55) starts below it, avoiding a turn-1 shock, the same idiom "Turn-1 GDP
    Consistency" established): `drag = Clamp(-InfrastructureConditionDragSensitivity *
    Max(0, threshold - averageCondition), -MaxInfrastructureConditionDrag, 0)` (drag capped at -0.5).
    Unlike the spending boost, this EASES AUTOMATICALLY (and can disappear entirely) if
    `ConditionIndex` later recovers back above the threshold - a genuinely different kind of effect
    from the accumulator, which is why the two are computed separately rather than merged into one
    variable.
  - **`MacroSystem.ApplyInfrastructureGrowthEffect`** (a new method, run after both
    `ApplyInfrastructureCondition` and `ApplyCategorySpendingEffects` so it sees this turn's freshly-
    updated `ConditionIndex` and accumulator) combines the two under ONE shared ceiling -
    `MaxCombinedInfrastructureGrowthAdjustment` (0.75 - deliberately tighter than the sum of the two
    individual caps, 0.5 + 1.0 = 1.5, so this ceiling is a genuinely active constraint, not a dead
    safety net that never binds) - then recomputes `PotentialGrowthRate = Clamp(BasePotentialGrowthRate
    + combinedAdjustment, 0, MaxPotentialGrowthRate)`. This is the piece that actually satisfies the
    task's explicit "reconcile the two sources, don't just cap each individually" requirement.
- **`ClonePreviewCountry` updated**: `BasePotentialGrowthRate`/`InfrastructureSpendingGrowthAdjustment`
  are both copied explicitly (mutated during a turn, the same reasoning every other turn-mutated field
  in that method already follows).
- **A real, checked interaction with "Discretionary Spending Growth"**: `ApplyDiscretionarySpendingGrowth`
  reads `country.PotentialGrowthRate` to grow every Discretionary `SpendingLine` (and its `SeedAmount`)
  each turn, and runs BEFORE this turn's `ApplyInfrastructureGrowthEffect` recomputes it (same relative
  ordering as the OLD mechanic, which also mutated `PotentialGrowthRate` after `ResolveSpendingForTurn`
  ran) - so no NEW timing bug was introduced; the growth-rate value that mechanic reads is still "as of
  the end of last turn," exactly as before this refactor.
- **Validated in the standalone harness first** (100/500-turn baseline, the full pre-existing 9-scenario
  regression matrix, plus a new `--deferredmaintenance` scenario - directly forces every USA
  `InfrastructureAsset.ConditionIndex` to 0 at turn 1 (a 50-point gap below threshold, an order of
  magnitude past where the drag's own individual cap already binds) and sustains a -30%/turn
  Transportation cut for the whole run so nothing can recover it, isolating and maximally stressing the
  NEW growth-rate feedback specifically - distinct from the existing `infrastructurestress` scenario,
  which stresses the `ConditionIndex` STOCK bound via a slower ~9-turn decay-to-floor path, not this
  new growth-rate channel): zero real anomalies (finite/range) for `PotentialGrowthRate`/
  `BasePotentialGrowthRate`/`InfrastructureSpendingGrowthAdjustment` across every run. USA's 500-turn
  `deferredmaintenance` GDP (49,052,176) came in substantially lower than the equivalent baseline
  (311,333,824) - a real, bounded, and expected consequence of holding the worst-case condition/
  spending-cut combination for the ENTIRE run (a small persistent per-turn drag compounds
  significantly over 500 turns, the same way a small persistent boost would) - not a divergence: no
  NaN, no negative GDP, output gap settled at -13.8% (matching the pre-existing -13% to -15%
  equilibrium "Discretionary Spending Growth" already established) and `DebtToGdpRatio` at 143.5%
  (matching the Fiscal Reaction Function's ~142% equilibrium) - confirming the mechanism produces a
  real, direction-correct, but fully bounded outcome even under sustained worst-case stress. The full
  pre-existing 9-scenario matrix (`stress`/`sustainedexploit`/`tariffoverride`/`welfarestress`/
  `swfstress`/`phase2stress`/`laborstress`/`crimejusticestress`/`infrastructurestress`) showed zero
  regression - anomaly counts stayed within each scenario's own previously-documented range, and a
  dedicated re-check confirmed zero finite/negative/out-of-range anomalies for any of the three new
  fields across all of them at 500 turns.
- **Real-Unity confirmation OBTAINED (2026-07-29)** - closing the gap left open above. The
  `UnityPackageManager.exe` IPC-server failure was resolved by opening Unity Hub/the Editor normally
  once (full GUI load, then closed) to let the Package Manager server state repair itself, exactly the
  workaround anticipated when this gap was first recorded. `BatchSimulationRunner -runmatrix` (all 12
  scenarios x 100/500 turns, 24 combinations total, including `deferredmaintenance`) then ran clean on
  the first attempt: zero finite/negative/out-of-range anomalies anywhere - every flagged anomaly across
  all 24 combinations was the same pre-existing small-magnitude "swung X% in one turn" false positive
  documented elsewhere in this file (e.g. Sweden's `DebtToGdpRatio` settling turn-1-through-7 toward
  equilibrium), unrelated to this change. `deferredmaintenance` at 500 turns landed at GDP 48,639,590
  and `DebtToGdpRatio` 144.1% - within a fraction of a percent of the harness's own 49,052,176/143.5%
  figures above, confirming the ported logic's fidelity. **This item now carries the same real-Unity
  confirmation as every other change in this file.**

## Sector Integration
A follow-up task, once Elias resolved Round 1's Open Questions #1 ("Resolved by Elias: INTEGRATE —
Sector Output/Employment should feed back into the core economy"). Same explicit design constraint
as "Infrastructure Feedback" above: no GDP-identity or labor-accounting decomposition - a small,
bounded nudge onto `PotentialGrowthRate`/`Unemployment`, the same existing-variable pattern every
mechanic since has used. Explicitly flagged by the task as needing the most care: `PotentialGrowthRate`
now has THREE simultaneous sources (Infrastructure spending, Infrastructure condition, Sector
performance), not two.

- **`MacroSystem.ApplyInfrastructureGrowthEffect` refactored from a `void` mutator into a pure
  `float` getter**: it still computes Infrastructure's own combined (spending + condition) adjustment
  exactly as "Infrastructure Feedback" shipped it, but no longer writes `PotentialGrowthRate` itself -
  that responsibility moved to a new single method, `ApplySectorGrowthEffect`, so there is exactly ONE
  place in the codebase that ever assigns `PotentialGrowthRate`, no matter how many sources feed into
  it.
- **`GetSectorGrowthAdjustment`** (new): sums, across all four `Sector`s, the gap between current
  `OutputShareOfGdp` and each sector's own `BaselineOutputShareOfGdp` ("relative to its own trend",
  per the task's wording) - strong aggregate performance nudges `PotentialGrowthRate` up, weak
  performance drags it down. `SectorGrowthSensitivity` (0.05 points of `PotentialGrowthRate` per
  aggregate percentage-point-of-GDP gap) and its own cap, `MaxSectorGrowthAdjustment` (0.5), are
  calibrated so that even the largest realistic aggregate gap (~16 points of GDP-share, if all four
  sectors were pushed to max Subsidy/min Regulation simultaneously - `SectorSubsidySensitivity`/
  `SectorRegulationSensitivity`'s own existing 0.04-per-point formula, reversion-adjusted) meaningfully
  exceeds the cap (raw 0.8 vs. cap 0.5), confirming the cap is a genuinely active constraint, not a
  dead one - the same "actively binds, not just theoretically present" standard "Infrastructure
  Feedback" already established.
- **`GetSectorUnemploymentAdjustment`** (new): the same aggregate-gap idea applied to `EmploymentShare`
  vs. `BaselineEmploymentShare` - sector employment GROWTH (above baseline) nudges Unemployment DOWN;
  CONTRACTION (below baseline) nudges it UP. Wired into `ApplyOkunsLaw` as a new additive term,
  mirroring `GetMinimumWageUnemploymentAdjustment`/`GetOvertimeUnemploymentAdjustment`/
  `GetRetrainingUnemploymentAdjustment`'s own exact pattern (a small term added directly to
  `unemploymentChange`, not a reversion-speed nudge like the welfare-program idiom). `SectorUnemploymentSensitivity`
  (0.03) and `MaxSectorUnemploymentAdjustment` (0.3) are calibrated the same way, and to the same
  rough order of magnitude as the existing labor-policy adjustments.
- **`MacroSystem.ApplySectorGrowthEffect`** (new, the single method that now writes `PotentialGrowthRate`):
  calls `ApplyInfrastructureGrowthEffect` (Infrastructure's own already-ceilinged contribution) and
  `GetSectorGrowthAdjustment` (Sector's own already-ceilinged contribution), sums them, clamps the SUM
  to a new **`MaxTotalPotentialGrowthAdjustment` (1.0) - the all-sources combined ceiling the task
  explicitly called "the single most important thing to get right here"** - then sets
  `PotentialGrowthRate = Clamp(BasePotentialGrowthRate + total, 0, MaxPotentialGrowthRate)`. Without
  this outer ceiling, Infrastructure's own sub-total (practical range roughly `[-0.5, +0.75]` - see
  below for why the negative side never actually reaches its nominal `-0.75`) and Sector's own
  sub-total (`[-0.5, +0.5]`) could combine toward a theoretical `-1.0` to `+1.25` - clamping the SUM to
  `±1.0` is what actually prevents three simultaneous nudges on one variable from stacking past a
  single sane bound, not each source's own cap checked in isolation.
  - **A subtlety found while calibrating, worth recording**: Infrastructure's own combined adjustment
    can never actually reach its nominal `-0.75` floor on the negative side, because the spending-boost
    accumulator is always `>= 0` and the condition-drag alone floors at `-0.5` - so Infrastructure's
    real practical range is `[-0.5, +0.75]`, asymmetric around zero. This doesn't weaken the new
    all-sources ceiling's protection (confirmed by the stress scenario below, which drives the
    negative-side combination to exactly its analytical worst case, `-0.5 + -0.5 = -1.0`, right at the
    new ceiling's boundary) - it's simply an honestly-disclosed calibration detail, not a bug.
- **`SimulationManager` reordering**: `ApplySectorEffects` moved EARLIER in `ApplyDomesticPolicy`/
  `PreviewTurn` (immediately after `ApplyInfrastructureCondition`, before `ApplySectorGrowthEffect`),
  from its old position much later in the pipeline (right before `ApplyApprovalRating`) - necessary so
  `GetSectorGrowthAdjustment`/`GetSectorUnemploymentAdjustment` (called from `ApplySectorGrowthEffect`/
  `ApplyOkunsLaw`, both of which need to run BEFORE `ApplyPotentialGdpGrowth`/`ApplyOkunsLaw` consume
  `PotentialGrowthRate`) see THIS turn's freshly-updated Sector Output/Employment, the same "must see
  this turn's just-updated value" timing requirement `ApplyInfrastructureGrowthEffect`'s own condition-
  drag already established. Confirmed safe: nothing between the sector dials' own policy-change step
  (`ApplySectorPolicyChanges`, already early) and the new earlier position depends on Sector state
  (Sector was originally isolated specifically so nothing else read it), so moving it earlier changes
  nothing else's behavior.
- **Validated in the standalone harness first** (100/500-turn baseline, the full pre-existing
  11-scenario regression matrix including the ORIGINAL Round 1 `sectorstress` scenario, plus a new
  `--growthstackstress` scenario - forces every USA `ConditionIndex` to 0 AND pushes all four Sectors
  to min Subsidy/max Regulation simultaneously, the worst-case SAME-DIRECTION stacking test:
  Infrastructure's condition-drag and Sector's performance-drag both pushing `PotentialGrowthRate` down
  at once, the genuinely dangerous case for an additive combined ceiling, distinct from
  `deferredmaintenance`'s Infrastructure-only isolation test): zero real anomalies (finite/negative/
  out-of-range) anywhere, including the pre-existing `sectorstress` scenario (confirming the
  `ApplySectorEffects` reordering introduced no regression). USA's 500-turn `growthstackstress` GDP
  (4,178,690) landed well below baseline, output gap settled at -14.3% (matching the pre-existing
  -13%-to-15% equilibrium) and `DebtToGdpRatio` at 147.0% (matching the Fiscal Reaction Function's
  ~142% equilibrium) - a real, bounded, and expected consequence of compounding two simultaneous
  negative growth-rate sources over many turns, not a divergence.
- **Real-Unity confirmation OBTAINED (2026-07-29), together with "Infrastructure Feedback" above** -
  same `BatchSimulationRunner -runmatrix` run (all 12 scenarios x 100/500 turns, including
  `growthstackstress`), same clean result: zero finite/negative/out-of-range anomalies anywhere, every
  flagged anomaly the same pre-existing small-magnitude swing false positive, unrelated to this change.
  `growthstackstress` at 500 turns landed at GDP 4,180,200 and `DebtToGdpRatio` 147.1% - within a
  fraction of a percent of the harness's own 4,178,690/147.0% figures above - and its growth rate was
  observed pinned at essentially exactly `+1.00%`/turn from roughly turn 50 through turn 500, direct
  real-Unity evidence that `MaxTotalPotentialGrowthAdjustment` (1.0) binds correctly under the
  worst-case same-direction stack rather than merely appearing bounded in aggregate GDP. **Both this
  item and "Infrastructure Feedback" now carry the same real-Unity confirmation as every other change
  in this file.**

## UI Revamp (Phases 1-5)
A UI-only revamp of `GameController` - explicitly no simulation/economic-logic changes in any of the
five phases below (Part 2 of the later "Country Selection" work is the one exception that touches
`SimulationManager`/`WorldFactory`, and even that is a pure data decomposition - see that section).
Given as four phases plus an unplanned fifth (the World Map), one commit per phase, with an explicit
pause after Phase 2 for a manual screenshot check before continuing.

### Phase 1 - Rolling Stat History
`Assets/Scripts/Data/StatHistory.cs`: a rolling `MaxEntries = 50`-turn buffer per country, one
`List<float>` per tracked stat (GDP, Unemployment, Inflation, ApprovalRating, DebtToGdpRatio,
PovertyRate, InterestRate - plus four more added in Phase 4, see below), appended once per turn by
`SimulationManager.AdvanceTurn` right after `ApplyDomesticPolicy`, deliberately kept separate from
the existing Recent Turns text log (a formatted string per turn, not raw numbers a graph can use) and
never touched by `PreviewTurn`'s throwaway clone - a live slider drag can never leak a phantom data
point into a country's real history.

### Phase 2 - Reusable Graph Component
`Assets/Scripts/UI/GraphRenderer.cs`: a `Texture2D`-based line-graph component, redrawn only when its
underlying data changes (not every OnGUI frame), auto-scaling its Y-axis per stat, extending one turn
forward via `PreviewTurn()`'s estimate rendered lighter/dashed so a policy change's projected effect is
visible before the player commits. Applied first to GDP/Unemployment/ApprovalRating on the dashboard.
Manually confirmed in the Editor - automated windowed screenshot capture was attempted repeatedly and
consistently hit the Editor indexing hang documented in "Real-Unity Validation is the Standard Path"
above, independent of workload (a 1-turn seed hung at the identical point as a 100-turn one), so manual
confirmation became the reliable path for this entire UI revamp.

Two follow-up additions, same UI-only constraint: **axis labels** (Y-axis min/max plus a midpoint
gridline+label, drawn as `GUI.Label` overlays from the graph's own last-computed min/max, no separate
source of truth) and a **colored change summary** next to each graph's title (first-to-last percentage
change, reusing GameController's existing signed-number formatting rather than inventing a new one,
green/red via the same convention every other stat delta already uses - see "Conventions" below -
correctly direction-aware: Unemployment's green is a DECREASE, GDP/Approval's green is an INCREASE,
never a naive "positive number = green").

### Phase 3 - Color Palette and Action-Coded Buttons
`Assets/Scripts/UI/UiPalette.cs`: one limited palette, a distinct hue per system area, applied to every
right-column tab plus the Federal Reserve/Crime/Labor headers, and a single green-good/red-bad delta
convention (direction-aware per stat, same rule Phase 2's change-summary reuses) applied to the policy
preview, dashboard GDP growth, trade balance, spending net, and SWF returns - `GraphRenderer`'s own
change-summary coloring was refactored to read from this shared palette instead of keeping its own
separate copy, so the two can never drift apart. Every Implement/Remove-style button is now
action-color-coded with real hover/pressed states (solid-color textures per `GUIStyle` state, confirmed
via a temporary Editor-only check that all 6 button kinds produce genuinely distinct normal/hover/active
textures, not just definitions that look different on paper). Also fixed the Economic Sectors tab: the
sector-name column width was sized off `_labelStyle`'s font metrics while actually rendering in the
larger, bolder `_headerStyle`, so "Manufacturing" wrapped and collided with the adjacent stats text -
now measured against the style it's actually drawn in.

Validated: 100-turn baseline in real Unity (`BatchSimulationRunner`), 53 anomalies, all the known
small-magnitude swing false positive - zero finite/negative/out-of-range anomalies. UI-only change, so
a single scenario was sufficient (not the full stress matrix).

### Phase 4 - Per-System Tab Reorganization
Federal Reserve, Labor Market, Crime & Justice, and Infrastructure - each previously either a
bolted-on dashboard panel or plain always-visible sliders with no home of their own - move into their
own tabs, each tinted with its own `UiPalette` hue. The old combined "Trade & Spending" tab splits into
a standalone Trade tab; its spending report moves into Spending Policy, next to the sliders it actually
reports on. Minimum Wage joins Labor Market (a labor-market lever, not a standalone dashboard line).
The tab bar becomes two rows (11 tabs) so nothing gets squeezed illegibly.

`StatHistory` gains four more buffers (`TradeBalance`, `LaborForceParticipationRate`, `CrimeIndex`,
`PrisonPopulationRate` - all already-computed `EconomyState` fields, no new economic logic), and
`GraphRenderer` gains a `DrawNeutral` overload for stats with no clear "good direction" (Interest Rate,
Incarceration Rate - kept honestly neutral rather than inventing a judgment call). Per-tab rollout:
Federal Reserve gets a neutral Interest Rate graph; Labor Market a colored Labor Force Participation
graph; Crime & Justice a colored Crime Index graph plus a neutral Incarceration Rate graph;
Infrastructure (new) proportional bars per asset's Condition Index (a "compare 4 things right now"
snapshot, not a trend, per the task's own bar-vs-graph guidance); Trade a Trade Balance graph plus
proportional export/import bars per partner; Spending Policy a Debt-to-GDP graph plus proportional bars
per spending line (scaled within Mandatory/Discretionary separately, since they differ by orders of
magnitude); Welfare Policy a Poverty Rate graph; Sovereign Wealth Fund proportional bars for the
asset-class mix. The dashboard itself is trimmed to true headline indicators only (GDP/Unemployment/
Inflation/Approval/Poverty/Currency/Debt/Budget plus the three Phase 2 graphs) - everything that moved
to a dedicated tab was removed from here, the "compact home view" the original task asked for.

**Advance Turn stays pinned in the left column, structurally independent of the right column's tab
content** - the earlier scroll/pinned-button layout bug this task explicitly warned against
reintroducing was not reintroduced; every new/restructured tab follows the same
scrollview-height-constrained pattern the existing tabs already used.

Validated: 100-turn baseline in real Unity, 56 anomalies, all the known small-magnitude swing false
positive - zero finite/negative/out-of-range anomalies. Manually confirmed in the Editor.

### Camera Skybox Fix
An unplanned small fix noticed during the tab work: this is an IMGUI-only game, nothing is ever meant
to render behind the UI, so Unity's default Skybox clear was just visual noise in any gap the UI
doesn't cover. Replaced with a solid dark color matching `GraphRenderer`'s own background tone, set
once on `Camera.main` in `GameController.Start()` - no new assets. Validated: 100-turn baseline, 76
anomalies, all the known small-magnitude swing false positive - zero finite/negative/out-of-range
anomalies.

### Phase 5 - World Map Tab (unplanned)
Not part of the original four-phase plan - added after Phase 4 as a natural extension once every
system had its own tab. `Assets/Scripts/UI/MapRenderer.cs`: a scoped-down interactive map using only
existing simulation data, no new geographic data of any kind. Went through several design iterations
(a two-blob landmass backdrop, then six hand-plotted country-outline polygons, then a full
six-continent silhouette - none committed) before landing on an intentionally abstract "network
diagram" layout, since hand-plotting a recognizable coastline from guessed vertices doesn't scale and
kept producing jagged/crowded results.

- A flat dark panel (matching `GraphRenderer`'s tone) with a subtle grid - no ocean/landmass geography.
- Six circular nodes, one per country, GDP-sized (clamped so the smallest is never below 60% of the
  largest - legibility over strict proportionality) and colored by each country's own `UiPalette` hue
  (USA reuses the already-established Political/gold; the other five get the remaining hues, an
  arbitrary but now-consistent pairing). Positioned in two loose clusters (USA west, the five European
  countries east in roughly their real relative order), no attempt at geographic accuracy.
- Trade lines connecting every real bilateral `TradePartner` pair from `WorldFactory`'s network (10
  pairs), thickness and opacity both scaled by that pair's actual trade volume - real data, not
  decoration.
- Event markers: a colored, severity-sized dot per fired event (severity derived from the existing
  `GdpShockPercent`/`InflationShockPoints`/`ApprovalEffect` envelope - see "Expanded Event Pool" - no
  new field invented), fading out over `EventMarkerFadeTurns` turns via a small rolling history
  `GameController` now tracks (`SimulationManager.GetLastEvent` only ever exposes the current turn's
  event).
- Hover shows a compact GDP/Unemployment/Approval tooltip; click pins a detail panel below the map -
  full dashboard-level detail for USA, a labeled read-only summary for the other five.

Also fixed a layout overflow found after the map landed: the 11-tab (now 12) bar let each button
auto-size to its own label with no explicit width, so at smaller window sizes 6 buttons per row could
sum wider than the available column and overflow past the screen edge instead of wrapping - fixed by
explicitly dividing the same screen-relative `rightColumnWidth` across 6 buttons per row, plus
word-wrap so long labels degrade to two lines instead of clipping. The map's own node/label positions
were already percentage-based but had no reserved margin for the label's fixed pixel width - fixed by
insetting the usable drawing area by a margin sized to the label.

Validated: single-scenario 100-turn baseline after every design iteration, zero finite/negative/
out-of-range anomalies each time. Visual results manually confirmed in the Editor - this environment's
automated windowed screenshot capture hit the same unresolved indexing hang on every attempt, so manual
confirmation was the reliable path for this entire map subsystem.

## Country Selection
Two-part task making the player's country runtime-selectable, rather than `PlayerCountryId` staying a
hardcoded USA constant forever.

### Part 1 - Runtime-Selectable PlayerCountryId
`PlayerCountryId` converted from a compile-time constant to a property backed by a nullable
`_selectedPlayerCountryId` field, set once via a new pre-dashboard selector screen
(`DrawCountrySelector`, gated at the top of `OnGUI` before any dashboard code runs). Every existing call
site kept working unchanged - confirmed by grep before touching anything that only the old `const`
declaration itself was hardcoded, every other reference already used the `PlayerCountryId` symbol.
Six selector buttons, one per country, colored with that country's own `UiPalette` identity (the
country-color mapping moved from `MapRenderer`-private into `UiPalette.GetCountryArea`/
`GetCountryColor` so the selector and the World Map tab can never drift apart on which color means
which country). Also improved the Eurozone shared-interest-rate message on the Federal Reserve tab to
name the other two Eurozone members dynamically (excluding whichever is being played) now that a
player can actually end up playing Germany/France/Italy rather than only ever observing USA.

A scoped assumption going in turned out not to hold: `GameController` had no USA-specific conditionals
left to generalize - every branch that mattered (SpendingLine existence, FedChair existence,
shared-currency detection) was already written generically throughout the prior UI-revamp phases,
confirmed directly rather than assumed.

Validated: single-scenario 100-turn baseline, zero finite/negative/out-of-range anomalies - UI-only
change. Selector screen manually confirmed in the Editor.

### Part 2 - Generic Spending Decomposition for the Other Five Countries
Part 1's own commit message noted this directly: Part 2 is what actually exercises the new generality
for the first time, since the other five countries had no detailed `SpendingLines` portfolio at all
before this. Adds a small, generic 5-category decomposition (`SocialPrograms`, `Defense` (reused, not
new), `InfrastructureAndDevelopment`, `PublicServices`, `Administration`) for Sweden/Germany/France/
Italy/Poland, mirroring USA's own original Phase 1 broad-categories stage, not its later detailed work.
`WorldFactory.SeedGenericSpendingLines` computes the total directly from each country's OWN CURRENT
`GDP * GovernmentSpendingRate/100` (read live at seed time, never a separately-hardcoded duplicate that
could drift) and makes `Administration` the exact remainder of the other four, guaranteeing the five
lines sum to EXACTLY that total regardless of floating-point rounding - a pure decomposition, not a
recalibration, and confirmed unable to change any country's fiscal trajectory: `ResolveSpendingForTurn`
branches purely on `SpendingLines.Count > 0`, and before this change these five countries had an empty
list and fell through to the legacy `GetBaselineGovernmentSpending`, which computes the exact same
`GDP * GovernmentSpendingRate/100` total - so the number feeding `ApplyRevenueAndSpending` is bit-for-bit
unchanged, only its breakdown into visible categories is new. The percentage splits themselves are
honestly illustrative, not individually researched, except Poland's higher Defense share (real and
well-documented - Poland has run one of NATO's highest defense-spending-to-GDP ratios given its
frontline position). Only `Defense` and `InfrastructureAndDevelopment` feed an existing economic effect
(`SimulationManager.BuildEffectiveDecisionForDetailedSpending` now sums `GetActualDollarChange` for both
`Transportation` and `InfrastructureAndDevelopment` into `InfrastructureSpendingChange`, safe as a plain
addition since a given country's portfolio only ever contains one of the two) - the other three get zero
effect for now, deliberately mirroring how 15 of USA's own 19 Discretionary categories still have none
either.

**A "Sweden vs. Germany debt-floor divergence" was flagged mid-task and investigated via temporary
`[DIAG]` logging in `ApplyRevenueAndSpending` (removed before this commit) before Part 2 was committed.**
Both countries showed large `DebtToGdpRatio` swings in their first several turns after getting real
`SpendingLines` for the first time - Sweden's collapsing from 35% to a flat 0% by turn ~50 and staying
there for long stretches (oscillating near zero for the rest of a 200-turn diagnostic run), Germany's
settling into a genuinely flat, stable ~35.3-35.7% equilibrium from turn ~10 through turn 200. **Root
cause: NOT a Part 2 bug** - confirmed above that Part 2 cannot change either country's G total. The
actual driver is Sweden having an active Sovereign Wealth Fund (see "Sovereign Wealth Fund Expansion to
All Six Countries") whose `SwfReturns` compound into revenue every turn (growing from ~$8B to over
$3,400B across the 200-turn diagnostic run) on top of a structural Revenue-far-exceeds-spending
imbalance both countries already share (Germany's own `TheoreticalRevenue` also runs roughly 2x its `G`
from turn 1) - Germany has `SwfReturns = 0` throughout (no fund), so its primary surplus is smaller and
it settles at a genuine non-zero equilibrium once `GetFiscalReactionMultiplier` stabilizes around 0.585-
0.59, while Sweden's extra SWF-driven surplus is large enough to run its debt into the pre-existing,
intentional `Mathf.Clamp(state.GovernmentDebt - budgetBalance, 0f, maxDebt)` floor and keep it pinned
near zero. Both the debt floor and the `[0.5, 1.5]` `fiscalReactionMultiplier` clamp
(`MinFiscalReactionMultiplier`/`MaxFiscalReactionMultiplier`) are pre-existing, intentional design
constraints from "Fiscal Reaction Function," not anything introduced by this task - and this same
bimodal debt-to-zero-or-ceiling tendency under a no-policy baseline is already independently documented
for USA in "SpendingLine Amount Ceiling - Debt-to-Zero Fix," so Sweden landing on the zero end of that
same known spectrum is consistent with prior findings, not a new phenomenon.

**France (also SWF-active from turn 1 - see "Sovereign Wealth Fund Expansion to All Six Countries")
was checked with the same rigor and confirmed to hit the identical floor via the identical mechanism,
not a distinct one.** Starting at 116% `DebtToGdpRatio` - over 3x further from the 0% floor than
Sweden's 35% starting point - France's debt still declines to exactly 0% by turn ~80 of the same
200-turn diagnostic run and stays pinned there through turn 200, just later than Sweden's turn ~50,
because it had further to fall. The same compounding signature is directly visible in `SwfReturns`:
$3.8B at turn 1, $74.9B at turn 50, then a 23.5x jump to $1,763.7B by turn 80 - the exact window
`DebtToGdpRatio` collapses from 84.9% to 0% - continuing to $3,585B by turn 200, with
`fiscalReactionMultiplier` floored at exactly 0.5000 throughout, same as Sweden's end state. Two
alternative country-specific explanations were explicitly ruled out rather than assumed away: (1)
France's `CollectionEfficiency` (0.7444) is not exceptional - it's actually the LOWEST of the four
countries carrying `[DIAG]` logging (Germany 0.7799, Sweden 0.7671, Italy 0.9534), so it cannot be
inflating France's revenue relative to the others; (2) France's own turn-1 structural primary surplus
(before any SWF returns matter - 19.2% of GDP) is unremarkable next to Italy's 22.4%, the largest of
the four - yet Italy is the clean control case that DISPROVES a plain revenue-to-spending-ratio
explanation: Italy has `SwfReturns = 0` throughout (no fund, matching Germany), starts at 138%
`DebtToGdpRatio`, and settles into its own genuine flat equilibrium (~108.0-108.7%, stable from turn 10
through turn 200) despite having the largest structural surplus ratio of the four - confirming that a
large primary surplus ALONE, without a compounding SWF-returns term on top, produces a stable non-zero
equilibrium (as it does for both Italy and Germany), and it is specifically the SWF-returns compounding
- present only in Sweden and France - that pushes a country's debt into the floor rather than to a
stable resting point.

Validated: full real-Unity matrix (`BatchSimulationRunner -runmatrix`, all 12 scenarios x 100/500
turns, 24 combinations) after removing the diagnostic logging - 3,445 total flagged anomalies across
every combination, but **every single one** is the already-established small-magnitude swing false
positive (1,798 `DebtToGdpRatio`, 971 `Inflation`, 70 `Unemployment` - all on the five newly-decomposed
countries, concentrated on Sweden at 1,553 given the SWF-driven dynamic above) - zero finite/negative/
out-of-range anomalies anywhere in the full matrix. The elevated anomaly count relative to earlier
UI-only phases (53-76) is expected and specific to this change actually giving these five countries real
spending data to swing on for the first time, not a regression - the full stress matrix (not just a
single baseline scenario) was run here specifically because, unlike the UI-only phases above, Part 2
does touch `SimulationManager`.

## Sovereign Wealth Fund Return-Model Rebalance
A follow-up to the Sweden/France debt-floor investigation above (see "Country Selection") and the
known-limitation note it produced (see "Status" below) - rebalances `SovereignWealthFundSystem`'s
return model against real-world data and gives it genuine downside volatility, without touching the
debt floor clamp, the Fiscal Reaction Function, or any of the six countries' own calibration
(`PotentialGDP`, `CollectionEfficiency`, debt seeds) - the brief for this item was explicit that the
fix should come from making SWF returns realistic, not from adjusting anything else to compensate.

- **Real per-asset-class means, re-anchored to Norway's GPFG**: Equities 8% (global long-run average,
  down from 9%), Bonds 4% (down from 4.5%), Infrastructure unchanged at 7%, Real Estate 7% (up from
  6%). At this game's default 40/30/15/15 weighting these blend to ~6.5%/turn - close to Norway's
  Government Pension Fund Global's own real long-run nominal average (~6-6.6% since 1998, source:
  NBIM's published annual returns), despite this game's default mix being materially more conservative
  than GPFG's own (~70% equities) - a similar-or-lower blended figure for a more conservative mix is
  the expected, correct direction, not a precise target.
- **The player's own Asset Class Mix weights were already driving the weighted-average return before
  this task** - `SovereignWealthFundSystem.ApplyReturns`/`GetAverageReturnEstimate` both already read
  `SovereignWealthFund.GetNormalizedWeight` per asset class, so a more equity-heavy player mix already
  earned (and swung) more than a bond-heavy one. Verified directly before assuming otherwise. The
  fund's own UI text disclaiming "this pass doesn't model differing returns by allocation" refers
  specifically to the separate Domestic Allocation slider (domestic vs. international), which
  genuinely remains unmodeled (both draw from the same asset-class return model) - left in place,
  not removed, since removing it would now overstate what this pass does.
- **Genuine downside volatility, replacing a uniform ± band with a normal distribution**: each asset
  class's return is now a Gaussian draw (Box-Muller transform off the same shared, isolated
  `System.Random`) with its own real-world-grounded standard deviation (Equities 16%, Bonds 6%,
  Infrastructure 10%, Real Estate 10% - real estate's damped somewhat below listed-REIT volatility
  since a sovereign fund's real estate book is typically unlisted/appraisal-based, matching GPFG's
  own), rather than a fixed [average-variance, average+variance] band that made a genuinely negative
  BLENDED turn possible only in an astronomically narrow tail (confirmed by direct calculation before
  changing anything: under the old model, only equities could go negative at all - down to -3% - and
  every other class's band was entirely positive, so a negative blended turn required equities
  simultaneously near its own worst case AND the other three simultaneously near theirs, in a
  continuous-uniform, effectively never-observed tail).
- **Validated exactly as specified - the same per-turn `[SWFDIAG]` diagnostic approach used for the
  original Sweden/France investigation, re-run at 500 turns, before AND after this change, on the same
  code otherwise unmodified**:
  - **Before** (old model): both countries' `DebtToGdpRatio` reaches and then stays pinned at almost
    exactly 0% for the great majority of the 500-turn run - Sweden from turn ~30 onward, France from
    turn ~80 onward - matching the original investigation's finding, just confirmed at the longer
    horizon.
  - **After** (new model): genuine negative `SwfReturns` turns are now directly observed and frequent
    (e.g. Sweden: -34.1 at turn 30, -725.7 at turn 80, -3,694.9 at turn 250; France: -173.7 at turn 50,
    -3,642.7 at turn 300) and produce real, sustained excursions away from the floor that did not exist
    under the old model - France's `DebtToGdpRatio` oscillates between roughly 7% and 90% from turn 80
    through turn 200 (versus permanently pinned at 0% over the same span before), and Sweden bounces up
    to 24.6% at turn 200 (versus never exceeding ~3% before at that point in the run).
  - **Honest disclosure: this does NOT fully resolve the debt-floor limitation at a 500-turn horizon**
    - by turn 500, both countries still land back at exactly 0% `DebtToGdpRatio` in this run, same
    nominal end-state as before. Root cause, confirmed by cross-referencing `SwfAssets` between the
    before/after runs: both converge to almost the exact same figure by turn 500 (Sweden: ~3.06M before
    vs. ~3.06M after; France: ~328.5K in both) - the fund's `TotalAssets` reaches its own pre-existing,
    untouched 300%-of-GDP ceiling (see "Sovereign Wealth Fund"'s `MaxSwfToGdpPercent`) well before turn
    500 in both models, and once there, even a realistic ~6.5% mean return generates an absolute-dollar
    income stream (~6.5% of 3x GDP, i.e. ~19.5% of GDP per turn on average) large enough to still pay
    debt down to zero given enough remaining turns - genuine down years slow this and produce real,
    multi-decade-scale excursions away from the floor (a real, meaningful improvement to the PATH), but
    don't structurally prevent the long-run END STATE, because the dominant driver is fund SIZE against
    its own cap, not return-model smoothness. This is exactly the situation the "known limitation" note
    below anticipated, and this validation confirms - rather than contradicts - that a returns-only fix
    (as this task was explicitly scoped) cannot close the gap alone; doing so would need one of the two
    directions already flagged there (an SWF drawdown lever, or debt-floor slack), neither pursued here
    per this task's own explicit constraint not to adjust anything else to compensate.
- **Full real-Unity matrix re-validation** (`BatchSimulationRunner -runmatrix`, all 12 scenarios x
  100/500 turns, 24 combinations): 6,445 total flagged anomalies (up from Part 2's 3,445, expected
  given genuinely more turn-to-turn volatility is the explicit goal here) - every single one is the
  already-established small-magnitude swing false positive (4,860 `DebtToGdpRatio`, 924 `Inflation`,
  71 `Unemployment`), concentrated on Sweden (2,942) and France (1,917) as expected - zero finite/
  negative/out-of-range anomalies anywhere across the full matrix.

## Eurozone Rate Voice
Replaces the Eurozone's previous fully-shared, non-interactive rate (Germany/France/Italy's own
`InterestRateChange` decisions were summed the same way Sweden/Poland's were, but every AI-controlled
member always gets `PolicyDecision.None()`, so in practice only whichever member the player happened
to be controlling could ever move the rate at all, and only via a raw manual delta, never in response
to any member's actual economic conditions) with a lightweight EU-level "voice" mechanic, mirroring
the real ECB Governing Council's structure at a stylized level. No formal `ROADMAP_BRIEF.md` Open
Question existed for this - it was a known design gap (the country-selection Part 1 UI text explained
this shared rate as read-only "by design," which was accurate under the old mechanic) rather than a
logged open item, so there was nothing to formally resolve there, just implement.

- **`EurozoneRateSystem`** (new file, `EurozoneRateSystem.cs`): `GetBlendedSuggestedRate(world,
  zoneMember)` computes a GDP-weighted average of every member's own `TaylorRule.
  GetSuggestedInterestRate` reading, sharing `zoneMember`'s `CurrencyZone` - a simplified version of
  the real ECB's "capital key" concept (not a precise replica), recomputed fresh every turn since GDP
  changes. A member with severe inflation or a large output gap pulls the shared rate more than a
  smaller, calmer one, the same directional logic as the real ECB. `zoneMember` itself is used
  directly for its own contribution (not re-read from `world`), so this works correctly for
  `SimulationManager.PreviewTurn`'s throwaway clone too - the clone shares the same `CurrencyZone`
  reference as the real country but isn't itself present in `world.Countries`, so the other members
  are found there by excluding this one by `Id` rather than by reference.
- **The player's push**: whichever Eurozone member the player is currently controlling gets a modest,
  bounded push on top of the blend, via the SAME `PolicyDecision.InterestRateChange` field/slider
  Sweden/Poland already use - reused rather than adding a new field, matching this task's own
  "lightweight" framing. `MemberRatePushRange` (0.75) deliberately mirrors a Fed Chair's `RateBias` in
  ORDER OF MAGNITUDE, not its exact range (`FederalReserveSystem.CandidatePool`'s own +-1.5) - half
  that range, reflecting a national governor's real but limited sway over a currency-union-wide rate,
  unlike USA's own unilateral Fed. Every other member's own decision is always `PolicyDecision.None()`
  (`InterestRateChange` defaults to 0) whenever it isn't the player's country, the same convention
  every other decision field already follows - so the other two members' contribution is always just
  their own current-turn Taylor Rule reading, never influenced by any player input, exactly as asked.
- **`ApplyEurozoneRate`**: sums every member's push (each clamped individually to
  `[-MemberRatePushRange, +MemberRatePushRange]` before summing, defense-in-depth against the UI ever
  sending an out-of-range value), adds it to the blended rate, clamps to `CurrencySystem`'s sane
  bounds, then moves the zone's rate partway (`RateAdjustmentSpeed`, 0.15 - matching
  `FederalReserveSystem`'s own damping value and role) toward that target rather than jumping straight
  there, the same gradual-adjustment idiom "Federal Reserve Rate Damping" established.
- **`CurrencySystem.ApplyInterestRateChanges`**: branches on `SharesCurrencyZoneWithOthers` (already
  existed, unchanged) to route a multi-country zone through `EurozoneRateSystem.ApplyEurozoneRate`
  instead of the old raw-additive-sum path - generic on "any zone shared by more than one country"
  rather than hardcoding Germany/France/Italy by `CountryId`, since that's the only such zone in this
  data model today. **Sweden/Poland's own independent-currency branch is byte-for-byte unchanged** -
  a single-country zone never satisfies `SharesCurrencyZoneWithOthers`, so it always falls through to
  the original code, exactly as this task required.
- **`SimulationManager.PreviewTurn`**: gained a third branch (FedChair / shared-Eurozone-zone /
  independent-currency) for `previewedInterestRate`, mirroring `CurrencySystem`'s own three-way split -
  the Eurozone branch calls `GetBlendedSuggestedRate` plus the previewed decision's own clamped push,
  without the real turn's damping term, the same "skip the damping, show the fuller target" cosmetic
  approximation the pre-existing FedChair preview branch already used.
- **UI** (`GameController.DrawFederalReserveTab`): a Eurozone-member player now sees a real "National
  Rate Push" slider (reusing `_interestRateChangeInput`, just bounded to `EurozoneRateSystem.
  MemberRatePushRange` instead of the generic `InterestRateChangeRange`), replacing the old read-only
  framing - the explanatory text was rewritten to describe the GDP-weighted Taylor-Rule blend and the
  player's own bounded push accurately, rather than the superseded "no single member state can set it
  unilaterally... read-only by design" framing that predated this mechanic.
- **Validated**: full real-Unity matrix (`BatchSimulationRunner -runmatrix`, all 12 scenarios x
  100/500 turns, 24 combinations) - zero finite/negative/out-of-range anomalies anywhere; every
  flagged anomaly (6,941 total) was the already-established small-magnitude swing false positive,
  including a newly-active `InterestRate` swing category (72 instances, e.g. USA's own FedChair-driven
  rate moving `0.03 -> 0.08`, a tiny absolute move reading as a huge percentage off a near-zero base) -
  confirmed this specific check (`CheckSwing(..., "InterestRate", ...)`) already existed in
  `SimulationTestRunner` before this change, so its increased activity reflects genuinely more dynamic
  rates now, not a new code path. Germany/France/Italy's baseline 500-turn trajectories stayed bounded
  and consistent with previously-established patterns: the shared rate itself now correctly RISES in
  response to rising inflation (2.74% at turn 1 to 9.86% by turn 500, versus staying frozen at a flat
  2.25% for the entire 500 turns under the old mechanic in an equivalent prior run, direct evidence the
  Taylor-Rule feedback is genuinely live) while GDP kept growing for all three (no collapse), Germany's
  `DebtToGdpRatio` landed at 43.1% and Italy's at 126.1% (both the same order of magnitude as their
  already-validated ~35%/~108% equilibria, not a runaway divergence), and France's landed at exactly
  0% - within the ALREADY-DOCUMENTED bimodal range that country's SWF-return dynamic produces (see
  "Sovereign Wealth Fund Return-Model Rebalance" above), not a new or worsened outcome caused by this
  change. All three members always showed the identical `InterestRate` at any given turn across every
  scenario, confirming the shared-zone mechanic stayed intact throughout.

## Sovereign Wealth Fund Drawdown Mechanic
`ROADMAP_BRIEF.md` Round 3 item 1 - directly closes the gap the SWF-returns rebalance's own
known-limitation note flagged (see "Sovereign Wealth Fund Return-Model Rebalance" above and the
note in "Status"): a real, player-chosen policy lever to withdraw fund assets during a recession or
emergency, rather than only ever being able to contribute to the fund.

- **The minimal implementation**: `SovereignWealthFund.ContributionRatePercent`'s valid range simply
  extends below zero (`SimulationManager.MinSwfContributionRate`: 0 -> -10, symmetric with the
  existing +10 ceiling - a policy LEVER the player chooses to pull, not an automatic
  recession-triggered response, so no separate, narrower cap was invented for this pass). No new
  field, no new code path: `GetSwfContribution` (`GDP * ContributionRatePercent/100`) already goes
  negative automatically, which `ApplyRevenueAndSpending`'s plain sum already treats as a revenue
  offset rather than an expense, and `TotalAssets += swfContribution` (already existed, in both
  `AdvanceTurn` and `PreviewTurn`) already shrinks the fund by exactly the withdrawn amount, clamped
  at 0 by the pre-existing `Mathf.Clamp(..., 0f, maxSwfAssets)` - the fund can't be drawn down past
  empty. Reusing 100% of the existing plumbing this way is exactly what "lower risk than starting
  something new" meant for this item.
- **The one real code change needed**: `PolicyDecision.SwfContributionRateOverride`'s "-1 = no change
  requested" sentinel (shared with every other SWF override field) no longer works once negative
  values are legitimate - a real -1% withdrawal request would be indistinguishable from "untouched."
  Switched this ONE field's sentinel to `float.MinValue` (a value no real percentage will ever
  produce), with a `<remarks>` doc comment explaining the deviation from its siblings' shared idiom -
  every other SWF override field keeps the original -1 sentinel unchanged, since none of them have a
  legitimate negative value.
- **UI** (`GameController`'s Sovereign Wealth Fund tab): the existing Contribution Rate slider's range
  widens the same way (`MinSwfContributionRate` kept in sync between both files, per the existing
  "must match" comment convention), relabeled "Contribution/Withdrawal Rate" with an explicit
  "negative draws the fund down - use during a recession or emergency instead of borrowing" note, and
  the "Estimated this turn" line relabeled "Contribution/Withdrawal" - both already handle a negative
  value's sign correctly via the pre-existing `{value:+0.00;-0.00;0}` format specifier used elsewhere,
  no new formatting logic needed.
- **Validated - the direct test this item asked for**: a throwaway Editor diagnostic (not committed -
  mirrors the temporary `[DIAG]`/`[SWFDIAG]` precedent from the original debt-floor investigation and
  its returns-rebalance follow-up, same reasoning: a real-turn Sweden/France withdrawal decision isn't
  something any existing `SimulationTestRunner` scenario scripts, since every scenario only ever
  builds a decision for USA) ran 500 turns with Sweden AND France both drawing down at a sustained
  -3%/turn, logging `SwfAssets`/`GovernmentDebt`/`DebtToGdpRatio` every 10 turns. **Result: the
  debt-floor pinning is genuinely RESOLVED, not just slowed** - both funds hit exactly 0 `SwfAssets`
  within the first ~20 turns (Sweden) or immediately (France, whose smaller starting fund the first
  withdrawal alone exhausts) and stay there for the rest of the run, and with `SwfReturns` reduced to
  a permanent 0 alongside, both countries' `DebtToGdpRatio` settles into a genuine, STABLE, non-zero
  equilibrium instead of the floor: Sweden at a steady ~8.2-8.5% from turn 40 onward, France at a
  steady ~89.6-96% for the entire run - the same kind of stable equilibrium Germany (~35%) and Italy
  (~108%) already had without any SWF-return complication, confirming the root cause really was fund
  SIZE against the 300%-of-GDP ceiling (as the returns-rebalance investigation concluded), and
  removing that size via a real withdrawal genuinely fixes it, not just masks it.
- **A useful methodology finding, worth recording**: this diagnostic ran ENTIRELY in Edit mode - it
  never set `EditorApplication.isPlaying = true` at all, calling `SimulationManager`/`WorldFactory`
  directly via a plain static method and calling `EditorApplication.Exit(0)` itself when done, the
  same "genuinely decoupled from SimulationTestRunner's pipeline" principle used to work around the
  Editor indexing hang once before. Since the hang is specifically tied to the post-Play-mode return
  to Edit mode (see "Real-Unity Validation is the Standard Path"), a diagnostic that never enters Play
  mode at all can't trigger it - confirmed here: the process exited cleanly on its own with no stray
  `Unity.exe` left running, no force-kill needed. Worth reaching for this pattern first for any future
  one-off diagnostic that doesn't need `SimulationTestRunner`'s own scenario/logging machinery.
- **Full real-Unity matrix re-validation** (`BatchSimulationRunner -runmatrix`, all 12 scenarios x
  100/500 turns, 24 combinations - required since this is fiscal-touching, the same extra caution
  every SWF-adjacent item has needed): zero finite/negative/out-of-range anomalies anywhere; anomaly
  counts (76-193 at 100 turns, 400-897 at 500) landed within the same range already established for
  the equivalent matrix run just before this change - expected, since no existing scenario actually
  exercises a negative contribution rate, so this run confirms the widened range and the sentinel
  change introduced no regression under any of the already-validated conditions, not that the new
  lever itself is exercised by the standard matrix (that's what the dedicated diagnostic above was for).

## Expanded Sector-Specific Policies
`ROADMAP_BRIEF.md` Round 3 item 2 - adds the three suggested policy dials (Tax Credits, Deregulation/
Nationalization as a single axis, Research Grants) to all four existing Sectors, on top of the
already-proven Subsidy/Regulation pair (see "Economic Sectors"). Flagged low risk by the roadmap
itself - same integration pattern already proven, no new tracked stats - confirmed true: no new
`Sector` fields beyond the three dials themselves, `OutputShareOfGdp`/`EmploymentShare`/`SectorMetric`
unchanged.

- **`Sector.TaxCreditLevel`/`ResearchGrantsLevel`/`DeregulationNationalizationLevel`** (0-100, 50 =
  neutral, the same uniform-dial idiom every policy level in this project uses): Tax Credits and
  Research Grants mostly mirror Subsidy's existing shape (a broad, uniform positive nudge to
  Output/Employment/SectorMetric) - Tax Credits at the same sensitivity as Subsidy (a tax credit and a
  direct subsidy have a similar practical effect in this stylized model, just a different fiscal
  mechanism this pass doesn't distinguish); Research Grants at the same sensitivity for Output/
  SectorMetric but HALF for Employment specifically, since grants fund research and output, not broad
  hiring.
- **Deregulation/Nationalization is the one deliberate divergence from the "uniform across all three
  stats" shape every other sector dial (old and new) uses** - and deliberately so, to avoid it being a
  redundant duplicate of the existing RegulationLevel (a genuinely different real-world question:
  ownership structure, not regulatory stringency - a state-owned firm and a private one can each be
  lightly or heavily regulated in principle). Higher (more deregulated/private) nudges Output/
  SectorMetric UP but Employment DOWN; lower (more nationalized) does the reverse - the real,
  well-documented state-owned-enterprise tradeoff (privatization/deregulation typically gains
  efficiency by shedding excess labor; nationalization typically preserves jobs at an efficiency
  cost). `MacroSystem.ApplySectorEffects` computes one `outputAndMetricAdjustment` (all five dials,
  Deregulation's own gap added directly) and one `employmentAdjustment` (same four, Deregulation's gap
  SUBTRACTED instead, Research Grants at its own smaller weight) rather than a single shared
  `policyAdjustment` term - a small, explicit divergence, not a full bespoke-per-sector-type formula
  (the same formula shape still applies identically to Manufacturing/Technology/Agriculture/Finance).
- **`PolicyDecision.SectorTaxCreditOverrides`/`SectorResearchGrantsOverrides`/
  `SectorDeregulationNationalizationOverrides`** (three new `Dictionary<SectorType, float>` fields,
  same "only requested entries matter" pattern as the existing two) and `SimulationManager.
  ApplySectorPolicyChanges`'s three new clamped `TryGetValue` blocks - byte-for-byte the same
  integration pattern as Subsidy/Regulation, confirming the roadmap's own "low risk" framing.
- **UI** (`GameController`'s Economic Sectors tab): three more sliders per sector (12 more controls
  total across the four sectors), same draft/cache/dirty-check/decision-building wiring already
  established for Subsidy/Regulation, duplicated exactly - no new UI pattern invented.
- **`growthstackstress` extended, not left behind**: this scenario's whole purpose is stressing
  `MacroSystem.MaxTotalPotentialGrowthAdjustment` (the all-sources combined ceiling - see "Sector
  Integration") with every available downward-pushing sector lever at once, so leaving the three new
  dials untested here would have been a real gap - extended to also push min Tax Credits/Research
  Grants and full Nationalization (Output-worst-case for all three) alongside the original min
  Subsidy/max Regulation. Honestly disclosed in the scenario's own doc comment: full Nationalization is
  worst-case for OUTPUT but pushes Employment the OPPOSITE direction (nationalization preserves jobs),
  so this remains a worst-case test for `MaxTotalPotentialGrowthAdjustment` specifically, not
  simultaneously for `MaxSectorUnemploymentAdjustment`/Okun's Law - exactly as it was before this
  extension, not a new gap introduced by it.
- **Validated**: full real-Unity matrix (`BatchSimulationRunner -runmatrix`, all 12 scenarios x
  100/500 turns, 24 combinations - chosen over the roadmap's own "single-scenario smoke check is
  acceptable" allowance, since the full matrix was no more effort with the tooling already in place) -
  zero finite/negative/out-of-range anomalies anywhere; anomaly counts (69-177 at 100 turns, 384-987
  at 500) landed within the same range already established for the equivalent matrix run just before
  this change. **Direct confirmation the combined ceiling absorbs the extra stress correctly**: the
  extended `growthstackstress` (now 5 simultaneous downward-pushing sources instead of 2) landed at
  turn-500 GDP 4,141,425 and `DebtToGdpRatio` 147.3% - essentially the SAME equilibrium as the
  original 2-lever version's already-documented 4,178,690/147.0% (harness) and 4,180,200/147.1%
  (real-Unity) figures in "Sector Integration" - direct evidence `MaxTotalPotentialGrowthAdjustment`
  was already fully saturated under the old 2-lever stack, so three more downward-pushing sources
  correctly produce no further movement, not a coincidental match.

## Deeper Crime & Justice II
`ROADMAP_BRIEF.md` Round 3 item 3 - adds two more tracked stats (`OrganizedCrimeIndex`,
`CorruptionIndex`) and two more policy dials (`JudicialFundingLevel`, `BorderEnforcementLevel`),
building on "Crime & Justice Basics"/"Deeper Crime & Justice"'s existing precedent. Effects kept
routed through the four channels the roadmap named explicitly - `ApprovalRating`, `BusinessConfidence`,
`CrimeIndex`, `PrisonPopulationRate` (Incarceration Rate) - no new outcome channel invented.

- **`EconomyState.OrganizedCrimeIndex`** (0-100, higher = more organized crime, the same stylized
  scale `CrimeIndex` uses): informed by the real Global Organized Crime Index (GI-TOC) - Italy's
  historic, extremely well-documented organized-crime organizations (Cosa Nostra, Camorra,
  'Ndrangheta) give it high confidence as the clear highest of the six (seeded 55); Sweden's real,
  well-documented recent gang-violence surge (the SAME fact already informing its elevated
  `BaselineCrimeIndex`) justifies its own elevated figure (32). USA (35)/France (28)/Poland (22)/
  Germany (20)'s relative ordering beyond those two is a directional, stylized estimate, honestly
  NOT independently confirmed against a specific index-year.
- **`EconomyState.CorruptionIndex`** (0-100, higher = MORE corrupt - inverted from the real
  Transparency International Corruption Perceptions Index, which runs the opposite direction, higher
  = cleaner, so this project's own "higher = worse" convention could stay consistent with
  `CrimeIndex`/`PrisonPopulationRate`): Nordic/German clean-government reputation (Sweden 18, Germany
  22) and Italy's comparatively lower CPI standing among Western European/G7 peers (44, the highest
  of the six) are both real and well-documented, high confidence; France (30)/USA (31)/Poland (40)'s
  exact relative ordering, and Italy-versus-Poland specifically, is a directional estimate, not
  confirmed against one index-year.
- **`Country.JudicialFundingLevel`** (0-100, 50 = neutral, the same uniform-dial idiom
  `PoliceFundingLevel` established) reduces BOTH new stats (better prosecution capacity disrupts
  organized crime; an independent, well-funded judiciary is a canonical real-world anti-corruption
  mechanism) AND `PrisonPopulationRate` (well-funded courts process cases faster, reducing the
  pretrial-detention backlog that swells incarceration in underfunded systems - a real, well-
  documented indirect channel, deliberately smaller than `BailReformLevel`'s own direct mechanical
  effect). **`Country.BorderEnforcementLevel`** (0-100, 50 = neutral, 0 = open/lenient, 100 = strict)
  reduces ONLY `OrganizedCrimeIndex` - stricter enforcement disrupts cross-border smuggling/
  trafficking, organized crime's real, well-documented core activity - deliberately scoped to this
  one channel, not a new labor-supply/immigration effect, per the item's own "don't invent a new
  outcome channel" instruction. `PoliceFundingLevel` (existing dial) also contributes a smaller,
  secondary reduction to `OrganizedCrimeIndex` - policing already fights organized crime in reality,
  reusing an existing lever rather than requiring a brand-new one for this one link.
- **`MacroSystem.ApplyOrganizedCrimeIndex`/`ApplyCorruptionIndex`** (new methods, mean-reverting
  toward `Country.BaselineOrganizedCrimeIndex`/`BaselineCorruptionIndex` the same way `ApplyCrimeIndex`
  already does) must run BEFORE `ApplyCrimeIndex`, which now reads `OrganizedCrimeIndex` fresh (a new
  `OrganizedCrimeIndexSensitivity` term - organized crime is a real, direct contributor to overall
  crime levels in most criminological frameworks) - the same "must see this turn's just-updated
  value" timing requirement Infrastructure Feedback's condition-drag already established.
- **Output channels, all GAPS versus each stat's own baseline** (the same idiom every prior crime
  effect uses): `ApplyCrimeEffects` (BusinessConfidence) gained `OrganizedCrimeBusinessConfidenceSensitivity`
  and `CorruptionBusinessConfidenceSensitivity` terms (both the same magnitude as the existing
  `CrimeBusinessConfidenceSensitivity` - organized crime and corruption both deter legitimate
  investment, real and well-documented, per World Bank/IMF governance literature for corruption
  specifically); `ApplyApprovalRating`'s misery penalty gained a `CorruptionApprovalSensitivity` term
  (0.15, slightly smaller than `CrimeApprovalSensitivity`'s 0.2 - corruption's political salience
  varies more by country/culture than crime's does, an honestly-disclosed stylized judgment call, not
  a precisely-fitted figure).
- **UI** (`GameController`'s existing Crime & Justice tab): two more sliders (Judicial Funding,
  Border Enforcement) alongside the existing four, plus two more `GraphRenderer` history graphs
  (Organized Crime Index, Corruption Index - both `higherIsBetter: false`, the same clear-direction
  convention `CrimeIndex`'s own graph uses). `StatHistory` gained matching buffers, purely additive
  bookkeeping like its four existing crime-related buffers.
- **`crimejusticestress` extended, not left behind**: pushed `JudicialFundingOverride`/
  `BorderEnforcementOverride` to 0 (their own worst case - both REDUCE the two new stats when higher,
  so 0 maximizes both) alongside the original `BailReformOverride`/`DrugPolicyOverride` = 100, now
  stress-testing all six Crime & Justice dials and both new tracked stats at once, confirming
  `BusinessConfidence`/`ApprovalRating` (their new output channels) stay bounded too.
  `PoliceFundingLevel` deliberately left at its neutral default, isolating the two genuinely NEW
  levers rather than re-testing the already-proven one.
- **Validated**: full real-Unity matrix (`BatchSimulationRunner -runmatrix`, all 12 scenarios x
  100/500 turns, 24 combinations) - zero finite/negative/out-of-range anomalies anywhere for
  `OrganizedCrimeIndex`/`CorruptionIndex` or any pre-existing field; every flagged anomaly (6,787
  total) was the already-established small-magnitude swing false positive. `crimejusticestress`'s own
  anomaly count ran higher than Round 2's documented range (424 vs. 178-215 at 500 turns), traced to
  the ambient Sweden/France SWF-driven `DebtToGdpRatio` swing noise (already established to vary
  significantly run-to-run given its own unseeded randomness - see "Sovereign Wealth Fund Return-Model
  Rebalance") dominating the count for this specific run's random draw, NOT a new instability from
  this change - confirmed by isolating the scenario's own flagged fields (`DebtToGdpRatio`/`Inflation`/
  `Unemployment`/`InterestRate` only, none of them new to this item) and by USA's own turn-500 figures
  under the extended stress (GDP still growing at +1.55%/turn, `DebtToGdpRatio` 148.5% - within the
  already-established ~142-150% equilibrium range, not a divergence).

## Expanded Economic Sectors II
`ROADMAP_BRIEF.md` Round 3 item 4 - doubles the tracked sector count from four to eight
(Manufacturing/Technology/Agriculture/Finance plus Energy/Construction/Retail/Telecommunications),
using the exact same integrated pattern "Economic Sectors"/"Sector Integration" already proved out -
no new mechanism, just more instances of the existing one. Flagged **moderate risk** by the roadmap
itself (not low, unlike "Expanded Sector-Specific Policies") specifically because `PotentialGrowthRate`
already has three stacked nudge sources feeding through `MacroSystem.GetSectorGrowthAdjustment`'s
aggregate-gap sum, and doubling the sector count doubles how many terms feed that same sum - required
a dedicated stress scenario, not just the standard matrix, to re-confirm the combined ceiling still
actively binds rather than just theoretically existing.

- **`SectorType`** gains Energy/Construction/Retail/Telecommunications - chosen for the same "clear,
  distinct real-world profile" reasoning the original four used: Energy and Construction are real,
  standard value-added categories with genuine country differentiation (Poland's real, well-documented
  coal-heavy energy sector and EU-funded construction boom give it the clear highest Output in both
  among the six); Retail and Telecommunications round out the mix as a labor-intensive consumer-facing
  sector and a capital-intensive, low-employment one, mirroring the original four's own Manufacturing/
  Agriculture vs. Technology/Finance contrast. SectorMetric per new type: Energy -> Renewable Share %
  (Germany's real Energiewende push and Poland's real status as the EU's most coal-dependent economy
  are both confirmed, high confidence; the rest directional); Construction -> a stylized 0-100
  Building Activity Index (entirely stylized, mirrors Technology's own Innovation Index); Retail ->
  E-Commerce Share % (directional estimate); Telecommunications -> Broadband Penetration % (a real,
  OECD-documented pattern - all six are high-broadband developed nations, Nordic countries typically
  highest, though exact figures are directional).
- **`WorldFactory.SeedSectors` refactored from 12 flat positional floats to a
  `params (SectorType, float Output, float Employment, float Metric)[]`** - a genuine maintainability
  necessity, not a stylistic choice: doubling the sector count would have pushed the old signature to
  24 same-typed positional floats, where a single misplaced argument silently seeds the wrong sector
  with no compiler error. Every call site (one per country) now passes a readable tuple list instead.
- **Zero simulation-logic changes needed for the new sectors to fully participate** in `Sector
  Integration`'s existing feedback: `MacroSystem.GetSectorGrowthAdjustment`/
  `GetSectorUnemploymentAdjustment` (which feed `PotentialGrowthRate`/`Unemployment`) and every UI
  drawing method already iterate `country.Sectors` generically (`foreach`), never hardcoded to four -
  confirmed directly before assuming so, the same "verify, don't assume" discipline "Country Selection"
  Part 1 already established when checking for USA-specific conditionals.
- **`growthstackstress` refactored from five hand-listed 4-entry dictionaries to a loop over EVERY
  `SectorType`** (`System.Enum.GetValues`) - both so the stress scenario genuinely covers "all new and
  existing sectors" (the task's own explicit wording) without relying on remembering to hand-add each
  new sector, and so it automatically keeps covering any sector this project adds in the future. Same
  worst-case settings as before (min Subsidy/Tax Credits/Research Grants, max Regulation, fully
  Nationalized), now applied to all eight sectors simultaneously instead of four.
- **Validated - the dedicated stress scenario this item explicitly required, not just the standard
  matrix**: full real-Unity matrix (`BatchSimulationRunner -runmatrix`, all 12 scenarios x 100/500
  turns, 24 combinations) - zero finite/negative/out-of-range anomalies anywhere; anomaly counts
  (69-176 at 100 turns, 400-916 at 500) landed within the same range already established. **Direct
  confirmation the combined ceiling still actively binds with double the sector count**: the
  8-sector `growthstackstress` landed at turn-500 GDP 4,121,293 and `DebtToGdpRatio` 147.3% -
  essentially the SAME equilibrium as both the original 4-sector 2-lever version (4,178,690/147.0%
  harness, 4,180,200/147.1% real-Unity) and "Expanded Sector-Specific Policies"' 4-sector 5-lever
  version (4,141,425/147.3%) - direct evidence `MaxTotalPotentialGrowthAdjustment` was already fully
  saturated before this change and correctly absorbs twice as many downward-pushing sectors without
  producing any further divergence, confirming the ceiling "actively binds, not just theoretically
  present" at the larger sector count exactly as this item required.

## Demographics, Part A (Population, Drift, Reconciled Effects)
`ROADMAP_BRIEF.md` Round 3 item 5, Part A - the largest, most novel item this round, deliberately
split into two sequential parts and validated separately. Part A adds plumbing (`Population`,
`BirthRate`, `DeathRate`, `NetMigrationRate`, a single `DependencyRatio` aging proxy) plus three
bounded effects into existing systems - explicitly NO policy levers yet (Part B). Not the full
age-cohort/population-pyramid model - one scalar per demographic concept per country, the same
"not the full theoretical richness" discipline every first-pass system in this project has followed.

- **`EconomyState.Population`** (millions, matching GDP's own human-readable scale): seeded from real
  2024/2025 data (USA 341.8, Germany 83.6, France 69.1, Italy 58.9, Poland 37.5, Sweden 10.6).
  **`BirthRate`/`DeathRate`/`NetMigrationRate`** (per-1,000 population per turn): seeded from real data
  (USA 10.6/9.1/+3.7, Sweden 10.8/9.5/+1.1, France 9.7/9.5/+1.1, Germany 8.2/12.2/+1.8, Italy
  6.3/10.4/+1.3, Poland 6.7/10.9/+0.2). **`DependencyRatio`** (old-age dependency ratio, 65+ as % of
  working-age 15-64 - a real, standard World Bank/OECD statistic): real/well-documented for Italy
  (40, highest of the six, among the highest in the world) and USA/Poland (28, lowest, both real);
  Germany's figure (35) is informed by an ESTIMATED 65+ population share (~22-23%, full age-cohort
  breakdown unavailable), honestly not directly sourced the way Italy/USA/Poland's are; Sweden/France
  (33 each) are directional estimates.
- **Drift, not a static constant repeated 500 turns**: `MacroSystem.ApplyDemographicRates` (must run
  before `ApplyPopulationGrowth`, which reads its fresh output) evolves all four rate-like quantities
  every turn. `BirthRate` declines on its own independent secular trend (a real, well-documented,
  near-universal fertility decline across developed nations), floored at a realistic low-fertility
  bound. `DependencyRatio` rises when `DeathRate` exceeds `BirthRate` (natural decrease) - the single
  derived aging proxy's own drift mechanism. `DeathRate` and `NetMigrationRate` THEN both drift further
  based on how far `DependencyRatio` has risen above its own baseline - a real mechanical effect
  (aging structurally raises crude death rate) and a real, discussed phenomenon (aging economies lean
  more on immigration over time), deliberately kept as a SEPARATE driver from `BirthRate`'s own
  independent decline (fertility decline isn't itself "caused" by a country's current dependency
  ratio the way the migration-reliance trend plausibly is). `Population` then evolves by the standard
  demographic identity: `(BirthRate - DeathRate + NetMigrationRate)/1000 x Population`.
- **A real tuning problem found and fixed before validation, not glossed over**: the first constant
  pass (`DependencyRatioDriftSensitivity`=0.01, `DeathRateAgingDriftSensitivity`=0.02,
  `MigrationAgingDriftSensitivity`=0.015) created an accelerating positive feedback loop (aging raises
  death rate, which widens the birth-death gap, which accelerates aging further) that slammed
  `DeathRate`/`DependencyRatio`/`NetMigrationRate` into their own safety ceilings within 200-300 turns
  for Germany (the largest starting birth-death gap) and stayed pinned there for the remaining
  turns - exactly the "bimodal attractor" failure pattern this project's own discipline explicitly
  watches for, caught via a throwaway Edit-mode diagnostic (never committed, same pattern as the SWF
  drawdown mechanic's own validation) BEFORE spending a full matrix run on unvalidated constants.
  Reduced all three by roughly 7x (0.0015/0.003/0.002) and re-tested: every country now drifts
  gently and stays comfortably inside its ceilings through turn 500, with genuinely differentiated,
  plausible trajectories - Germany/Italy/Poland (all real negative-natural-growth countries) decline
  gradually, USA grows (immigration plus an initially-favorable birth/death balance), Sweden shows a
  plausible rise-then-decline hump as its own birth rate's secular decline eventually crosses below
  its death rate. Population declines are large by turn 500 for the negative-natural-growth countries
  (Poland reaches roughly 2.1M from a 37.5M start) - a real, expected consequence of compounding even
  a modest sustained negative growth rate over a 500-TURN (effectively 500-year) horizon, the same
  "large but internally consistent, non-alarming at this extreme horizon" standard this project
  already accepts for GDP/debt figures, not evidence of a bug given the smooth, gradual, non-ceiling-
  hitting path that produces it.
- **Pension pressure, reconciled against the existing automatic Mandatory-spending growth
  mechanism**: `SimulationManager.ApplyDemographicPensionPressure` nudges the pension-equivalent
  `SpendingLine`'s `Amount` up as `DependencyRatio` rises above its own baseline - USA's Mandatory
  `SocialSecurity` line, or (the other five countries have no Mandatory portfolio at all) their
  Discretionary `SocialPrograms` line from "Country Selection" Part 2, the closest analog they have
  (honestly an approximation - `SocialPrograms` is broader than pensions specifically). Reconciled by
  nudging `Amount` ONLY, NEVER `SeedAmount` - the automatic growth mechanism (`ApplyMandatorySpendingGrowth`/
  `ApplyDiscretionarySpendingGrowth`, both already run earlier in the same `ResolveSpendingForTurn`
  call) is the sole thing that ever moves `SeedAmount` and therefore the `[0.2x, 3.0x]` ceiling's own
  moving reference point - this can never itself become a SECOND source of ceiling drift the way
  "SpendingLine Amount Ceiling - Debt-to-Zero Fix" once found and fixed for Discretionary spending.
  Small and bounded (a fractional nudge capped at 0.5% of the line's own current Amount per turn,
  reached only once `DependencyRatio` has drifted ~25 points above baseline) and re-clamped through
  the existing `ClampToSeedRange`.
- **Healthcare cost pressure, USA-only, honestly disclosed**: `ApplyDemographicHealthcarePressure`
  nudges USA's Mandatory `Medicare` line the same reconciled way (Amount only, capped at 0.4%/turn) -
  Medicare specifically serves the elderly population, the one existing line with a genuinely direct
  real-world link to aging (Medicaid/`HHSDiscretionary` serve broader populations and were
  deliberately left untouched). The other five countries have no Medicare-equivalent line at all
  (Country Selection Part 2's generic decomposition has no healthcare-specific category) - left
  without this effect entirely rather than forced onto an unrelated line, the same "USA-first, no
  clean analog exists yet" precedent "Detailed Spending Portfolio" and the original Sovereign Wealth
  Fund both already established (not escalated as a design fork - it directly matches this
  already-repeated pattern, not a novel judgment call).
- **Labor force participation, given the same seriousness `PotentialGrowthRate`'s own combined
  ceiling got in "Sector Integration" - audited by direct code search, not assumed**:
  `MacroSystem.ApplyLaborForceParticipationRate` already had two direct policy terms (paid family
  leave, workforce retraining) stacked with NO combined bound before this task - demographics adds
  two more (`DependencyRatio` gap, negative - aging shrinks the working-age share; `NetMigrationRate`
  gap versus its own baseline, positive - immigrants skew working-age). All four are now summed and
  clamped as ONE `MaxLaborForceParticipationAdjustment` (1.0) before being added to the target - the
  already-established Unemployment-gap term (the discouraged/encouraged-worker effect) stays
  deliberately OUTSIDE this ceiling, mirroring how `PotentialGrowthRate`'s own ceiling leaves
  `BasePotentialGrowthRate` itself outside it. Confirmed genuinely binding, not just theoretical: the
  pre-existing `laborstress` scenario (paid leave + retraining both maxed) already exceeds this
  ceiling's raw sum before clamping.
  - **Verified this is the COMPLETE set of direct writers, not a subset**: a full grep audit of every
    reference to `LaborForceParticipationRate` confirms `ApplyLaborForceParticipationRate` is the
    SOLE method that ever writes it, and it has exactly these four direct terms plus the one
    Unemployment-gap pass-through - no other direct writer exists anywhere in the codebase. Minimum
    wage, overtime regulation, and childcare subsidies (three of the five sources this item's own
    brief named) do NOT write to `LaborForceParticipationRate` directly at all, and never did before
    this task - all three only affect `Unemployment` (`GetMinimumWageUnemploymentAdjustment`/
    `GetOvertimeUnemploymentAdjustment` add directly to it in `ApplyOkunsLaw`; UBI/childcare instead
    nudge its reversion SPEED via `GetWelfareAdjustedReversionSpeed`), a genuinely separate tracked
    variable with its own independent hard clamp (`[0, MaxUnemploymentPercent]`). Their influence on
    `LaborForceParticipationRate` is therefore always INDIRECT, mediated entirely through
    `Unemployment`'s own already-bounded value and the single discouraged-worker pass-through term -
    already covered by Unemployment's own clamp, the same "each variable owns its own hard bound"
    pattern this project uses throughout, not a gap in this ceiling's coverage. This ceiling correctly
    bounds every term that is actually direct; it does not (and structurally could not, without
    duplicating Unemployment's own clamp) bound contributions transmitted through a different variable
    first - exactly how `PotentialGrowthRate`'s own ceiling also only governs its direct writers, not
    every upstream influence reaching it through GDP or Unemployment.
  - **One real, pre-existing (not newly introduced) double-channel effect surfaced by this audit**:
    `RetrainingProgramLevel` feeds `LaborForceParticipationRate` through BOTH this direct term AND,
    independently, its own separate `Unemployment` term (`GetRetrainingUnemploymentAdjustment`, from
    "Deeper Labor Market Policies," predating this task) - a real second-order reinforcement of the
    same lever through two channels. Not introduced by this task and not fixed here (out of this
    item's scope), but worth recording honestly now that it's been found.
- **Anomaly-checking apparatus extended** for all five new fields (`Population` can't go
  non-positive; `BirthRate`/`DeathRate` can't go negative; `DependencyRatio` range-checked to
  `[0, 100]`; all five checked for finiteness) - the same discipline every other tracked stat in this
  project already gets.
- **Validated - full real-Unity matrix (`BatchSimulationRunner -runmatrix`, all 12 scenarios x
  100/500 turns, 24 combinations), required since this is fiscal-touching (pension/healthcare
  pressure touch `SpendingLine.Amount`), RE-RUN after the LaborForceParticipationRate audit above
  with `Population`/`DependencyRatio` added to `SimulationTestRunner`'s own per-turn log line (a
  permanent addition, not a throwaway diagnostic this time) so the real per-country trajectory is
  directly readable from the same authoritative run, not inferred**: zero finite/negative/
  out-of-range anomalies anywhere across all 6,964 flagged lines, for `Population`/`BirthRate`/
  `DeathRate`/`NetMigrationRate`/`DependencyRatio` or any pre-existing field - every single one the
  already-established small-magnitude swing false positive; anomaly counts (60-183 at 100 turns,
  406-960 at 500) landed within the same range already established. **All six countries' `Population`
  stayed positive and growth-bounded at turn 500 in the baseline scenario** (USA 1,299.4M, up from
  341.8 on sustained immigration; Sweden 9.9M, down slightly from its own turn-~200 peak of ~14M -
  the plausible rise-then-decline hump already found in the pre-tuning diagnostic; Germany 7.8M, Italy
  7.0M, France 36.7M, Poland 2.1M, all declining smoothly from their much larger starting points) -
  every figure comfortably inside `[MinPopulation, MaxPopulation]` with no discontinuity. **`DependencyRatio`
  stayed sane for all six** (USA 28.96 - barely moved from its 28.0 baseline, since USA's birth rate
  hadn't yet crossed below its death rate by turn 500; Sweden 34.08; France 34.88; Germany 40.41;
  Poland 32.98; Italy 44.66, the highest, consistent with its own highest starting baseline of 40) -
  every figure well inside `[MinDependencyRatio, MaxDependencyRatio]` (`[15, 70]`), nowhere near
  either bound. **Germany and Poland specifically show a smooth, gradual, immigration-tempered
  decline, not a divergence or crash**: both `Population` series decline monotonically with no
  discontinuity turn-to-turn (Germany 83.4 -> 78.9 -> 73.9 -> 63.7 -> ... -> 7.8M at turns
  1/25/50/100/.../500; Poland 37.4 -> 33.8 -> 30.3 -> 23.8 -> ... -> 2.1M on the same schedule), while
  `NetMigrationRate` rises gently alongside `DependencyRatio` for both (the "aging economies lean more
  on immigration" drift term doing exactly what it's designed to - partially, not fully, offsetting
  the underlying natural-decrease pressure) - the large cumulative decline by turn 500 is the expected,
  bounded arithmetic consequence of a genuinely realistic sustained negative natural-growth rate
  compounding over a 500-TURN (effectively 500-year) horizon, the same "large but internally
  consistent at this extreme horizon" standard already accepted for this project's GDP/debt figures,
  not evidence of instability. **No double-counted drift from either reconciliation point**: the
  baseline scenario's turn-500 `DebtToGdpRatio` for all six countries landed consistent with their own
  already-documented equilibria (USA ~145%, Germany ~43%, Italy ~120%, Poland ~32% - a new but
  unremarkable, explicably higher figure given Poland's own already-higher seeded
  `PotentialGrowthRate` compounding further over 500 turns, unrelated to demographics; Sweden/France
  showing their own already-documented SWF-driven low/bimodal pattern) - no sign of the pension/
  healthcare pressure stacking unexpectedly with the automatic Mandatory-spending growth mechanism.
### Correction: Population growth-rate mean-reversion (same day, before Part B started)
The validation above was accepted too quickly. The original design applied
`(BirthRate - DeathRate + NetMigrationRate)/1000 x Population` directly, every turn, with no pull
back toward any long-run figure - realistic at the level of each individual rate (each is
independently bounded) but, unlike EVERY OTHER successfully-stabilized quantity in this model
(`Unemployment` reverts toward `NaturalUnemploymentRate`, `Inflation` toward its target,
`DebtToGdpRatio` toward each country's `ComfortableDebtToGdpPercent`), Population's own growth RATE
had no reversion mechanism at all. A persistent birth/death/migration gap therefore compounded
without limit, and "large decline over a 500-turn horizon" was the wrong frame for judging it - the
actual defect was structural (no bounded long-run anchor), not just a large number at an extreme
horizon.

- **Fix - the growth RATE mean-reverts, not just Population itself**: added
  `EconomyState.PopulationGrowthRate` (per-1,000/year, the quantity `Population` now actually evolves
  by) and `Country.SteadyStateGrowthRate` (a fixed per-country long-run anchor). Each turn,
  `MacroSystem.ApplyPopulationGrowth` computes the raw `impliedRate` (`BirthRate - DeathRate +
  NetMigrationRate`, same as before), takes its gap versus `SteadyStateGrowthRate`, hard-clamps that
  gap to +/-`MaxPopulationGrowthRateDeviation` (2, per-1,000/year) BEFORE weighting it by
  `PopulationGrowthRateSensitivity` (0.4) to form this turn's reversion target, then reverts
  `PopulationGrowthRate` toward that target at `PopulationGrowthReversionSpeed` (0.05 - deliberately
  slower than this project's usual ~0.15 speeds, since real demographic momentum changes over
  generations, not years). The gap is clamped BEFORE weighting - the same "bound the aggregate once"
  idiom `PotentialGrowthRate`'s `MaxTotalPotentialGrowthAdjustment` already uses - because
  `DependencyRatio`'s drift is explicitly one-directional and never reverts (see above), so
  `DeathRate`/`NetMigrationRate` keep drifting for a transient lasting far longer than 500 turns even
  though each is individually clamped; without capping the gap itself, the reversion target would keep
  sliding for the entire validation horizon and never plateau, defeating the point of adding reversion.
  This still allows genuine sustained long-run growth (USA) or decline (Germany/Poland/Italy) -
  `SteadyStateGrowthRate` is non-zero for all six - it just guarantees `PopulationGrowthRate`
  permanently sits within a bounded band around that anchor rather than drifting arbitrarily far from
  it. `PopulationGrowthRate` is seeded at world-creation time equal to each country's own turn-1
  implied rate, avoiding a turn-1 discontinuity (the same idiom every Baseline-anchored field uses).
- **`SteadyStateGrowthRate` calibration, real-data-grounded, magnitude honestly damped**: USA +1.8,
  Sweden +1.5, Germany -1.5, France -0.3, Italy -3.0, Poland -3.5 (all per-1,000/year). Directionally
  real for all six (Poland/Italy: severe, well-documented sub-replacement decline; Germany: moderate
  decline; France: the most fertility-resilient large EU economy, near-stable; Sweden/USA: modest
  immigration-driven growth). Magnitudes are damped below a literal extrapolation of current trends,
  disclosed honestly rather than presented as precise: this project's "1 turn ~= 1 year" convention
  makes the 500-turn validation horizon a ~500-year span, ~6.7x Eurostat/UN's actual 2025-2100
  (75-year) projection window. Poland's figure is anchored to Eurostat's own 2025-2100 projection
  (-31.6% cumulative) via the implied constant annual rate solving `(1+r)^75 = 0.684`, i.e.
  `r = 0.684^(1/75) - 1 ~= -5.05` per 1,000/year - and applying that literal real rate for a full 500
  years would ITSELF compound to roughly a 92% decline (`(1 - 0.00505)^500 ~= e^-2.53 ~= 0.0795` of
  the starting population), a mechanical consequence of the horizon length, not evidence the rate is
  unrealistic. -3.5 is damped further below -5.05 specifically so the 500-turn outcome reads as
  severe-but-plausible rather than a literal 500-year compound of a real 75-year rate.
- **Re-validated - full real-Unity matrix (`BatchSimulationRunner -runmatrix`, all 12 scenarios x
  100/500 turns, 24 combinations)**: zero `Population`/`PopulationGrowthRate`/`DependencyRatio`
  anomalies (non-positive, out-of-range, non-finite) anywhere across all 24 combinations; per-scenario
  total anomaly counts (66-178 at 100 turns, 388-951 at 500) landed within the same range as the prior,
  already-accepted validation run (60-183 / 406-960) - the already-established small-magnitude
  swing false positives (`DebtToGdpRatio`/`Inflation`/`InterestRate`/`Unemployment`), unrelated to
  demographics, not a new category this fix introduced. **`PopulationGrowthRate` now genuinely
  plateaus within the validation horizon for most countries, unlike the pre-fix design, which was
  still trending in one direction at turn 500**: Germany flattens at -2.30 by ~turn 250 and holds
  there through turn 500; Poland flattens at -4.30 by ~turn 250; France flattens near -1.10 by ~turn
  400; Sweden flattens at +0.70 by ~turn 350; Italy sits just past its own cap, near -3.79, essentially
  flat from ~turn 200; USA is still gently decelerating toward its own +1.8 anchor at turn 500 (down
  from an initial +5.2, itself declining because `BirthRate`'s secular decline applies to every
  country, USA included) - genuinely converging, not diverging.
- **Turn-500 baseline Population, all six countries (superseded by the second correction below,
  kept here only as a record of what the FIRST correction pass produced)**: USA 341.8M -> 1,038.1M
  (+203.6%); Sweden 10.6M -> 18.4M (+73.2%); Germany 83.6M -> 27.4M (-67.3%); France 69.1M -> 53.9M
  (-22.1%); Italy 58.9M -> 10.4M (-82.4%); Poland 37.5M -> 4.6M (-87.8%). These were sanity-checked at
  the time against Eurostat/UN projections using this project's "1 turn ~= 1 year" approximation - the
  SECOND correction below found that approximation itself was wrong.
- **Matrix run's own operational note, unrelated to the simulation logic (recurred on every full
  matrix run made this same day, including after the second correction below - see there for the full
  finding)**: this validation run's Unity batch process hung for 25+ minutes AFTER all 24 scenarios had
  already completed and logged (confirmed via the log's own content and process CPU sampling: still
  consuming ~4 CPU-cores across 112 threads with zero new log output, i.e. genuinely stuck, not
  deadlocked) in Unity's own post-Play "Start Indexing on Editor startup" step - a known Unity batchmode
  behavior, not a symptom of this fix's C# code (`ApplyPopulationGrowth`/`ApplyDemographicRates` are
  O(1) per country per turn, no loops, no allocations). The process was killed once the log confirmed
  all 24 scenarios had long since completed with clean data; no orphaned processes remained after the
  kill.
### Second correction: the plausibility check itself used the wrong turn-to-year conversion
The first correction's own sanity check was flawed. It compared 500 turns against real-world data
using a "1 turn ~= 1 year" approximation - but this project's own ESTABLISHED convention
(`ElectionSystem.ElectionCycle` = 12 turns per presidential term = 4 real years, confirmed elsewhere
in this document) means 1 turn = 1/3 year, so 500 turns is ~167 real years, not 500. Redone with the
correct conversion, every country's first-correction population change ran roughly 2-3x more extreme
than a faithful extrapolation of its own real-world trend over the true ~167-year horizon supports
(Poland -88% modeled vs. ~-43% implied; Germany -67% vs. ~-21%; France -22% vs. ~-11%; Sweden +73% vs.
~+37% - implied rates derived by solving `(1+r)^166.7 = 1+target%` for each country's real,
Eurostat/UN-grounded target).

- **Tightening `MaxPopulationGrowthRateDeviation` alone (tried first, empirically insufficient)**:
  reducing the cap from 2 to 1, and separately speeding up `PopulationGrowthReversionSpeed` from 0.05
  to 0.15, barely moved the modeled outcome (USA +204% -> +183% -> +169%; confirmed via a throwaway
  diagnostic before spending a full matrix run on it). This proved the cap/speed constants were not
  the actual defect.
- **Root cause found: `ApplyPopulationGrowth` was applying a full annual-scale rate every TURN, not
  every YEAR**: `BirthRate`/`DeathRate`/`NetMigrationRate`/`SteadyStateGrowthRate` are all real,
  per-1,000-population-PER-YEAR figures, but `Population *= 1 + PopulationGrowthRate/1000` ran once
  per turn, unscaled. With 500 turns representing only ~167 real years, this applies 500 "doses" of an
  annual rate where only ~167 should occur - a structural ~3x over-compounding matching the observed
  2-3x excess almost exactly. No amount of retuning the rate's own reversion (cap or speed) could fix
  an error in how many times the rate gets applied.
- **Actual fix**: added `MacroSystem.YearsPerTurn = 4f / ElectionSystem.ElectionCycle` (~0.333) and
  scaled Population's per-turn update by it: `Population *= 1 + PopulationGrowthRate/1000 x
  YearsPerTurn`. Re-tested via the same throwaway diagnostic: this alone brought every country back
  into the same order of magnitude as its real-world-grounded 167-year target. `MaxPopulationGrowthRateDeviation`
  was then re-tuned from 1 back toward its post-fix optimum (1, as it turns out - 1.5 was also tried
  and made Sweden/Germany's fit worse without meaningfully helping France, so 1 was kept);
  `PopulationGrowthReversionSpeed` was reverted to 0.05 (with `YearsPerTurn` now correctly accounted
  for, 0.05/turn corresponds to a genuinely generational, not merely multi-year, real-time half-life -
  the ORIGINAL intent behind picking a "deliberately slow" value, which the flawed 1-turn~=1-year
  assumption had accidentally undermined).
- **Re-validated - full real-Unity matrix (`BatchSimulationRunner -runmatrix`, all 12 scenarios x
  100/500 turns, 24 combinations)**: zero `Population`/`PopulationGrowthRate`/`DependencyRatio`
  anomalies anywhere; per-scenario anomaly totals (80-173 at 100 turns, 417-940 at 500) landed within
  the same established range as every prior validation run - the same pre-existing swing-detector
  noise, unrelated to demographics.
- **Turn-500/167-year baseline Population, all six countries, checked explicitly against the
  CORRECTLY-converted real-world extrapolation this time**: USA 341.8M -> 483.7M (+41.5% modeled vs.
  ~+35% implied, ratio 1.19x); Sweden 10.6M -> 13.1M (+24.0% vs. ~+37%, ratio 0.65x); Germany 83.6M ->
  60.8M (-27.2% vs. ~-21%, ratio 1.30x); France 69.1M -> 65.0M (-6.0% vs. ~-11%, ratio 0.55x); Italy
  58.9M -> 33.9M (-42.5% vs. an anchor-implied ~-39%, ratio 1.09x); Poland 37.5M -> 19.6M (-47.8% vs.
  ~-43%, ratio 1.11x). Every country now lands between 0.55x and 1.30x of its own real-world-grounded
  167-year target - comfortably within "same order of magnitude, not double," the explicit bar this
  correction was held to. Sweden and France sit on the conservative (undershooting) side rather than
  the overshooting side the first correction had - a real but modest remaining gap attributable to
  those two countries' own `SteadyStateGrowthRate` anchors being mildly under-tuned relative to their
  implied real rates (France's anchor of -0.3 implies a real rate of only -0.7 over 167 years, versus
  a target-implied rate of -0.70 almost exactly at the cap's edge; Sweden's anchor of +1.5 similarly
  undershoots a target-implied +1.89) - not re-tuned further in this pass since the "not double" bar is
  already comfortably met and Part B's Family/Immigration Policy levers will touch these same
  countries' rates directly regardless.
- **Operational note, recurred a third time**: the post-Play "Start Indexing on Editor startup" hang
  recurred on this matrix run too, this time WITHOUT any script deletion immediately preceding launch
  (the throwaway diagnostic was left in place through the run specifically to test that theory) -
  ruling out "deleted asset reconciliation" as the trigger identified in the first correction's writeup
  above. A naive watcher script also produced a false "process exited" signal here (bash `pgrep -f`
  failed to match the Windows process's command line, and a looser mtime-based watcher was fooled by
  Unity's own periodic "Scanning for USB devices" log noise, which continues even while genuinely
  stuck) - corrected by checking actual `Get-Process` CPU accumulation directly via PowerShell instead
  of trusting either method. The underlying hang itself remains unexplained (not this fix's C# code -
  same reasoning as the first occurrence) and unresolved as a general Unity batchmode issue; the
  practical workaround (confirm all scenarios logged complete, then kill and verify no orphaned
  processes) is now exercised three times in this project's history.
- **Part A stops here, deliberately** - no Family Policy/Immigration Policy levers yet (Part B),
  per this item's own explicit two-part sequencing and "validate Part A fully before starting Part B"
  instruction. UI display for the five new fields is also deferred to Part B, where it will sit
  naturally alongside the two new policy sliders rather than being added twice.

## Demographics, Part B (Family Policy and Immigration Policy levers)
Adds the two policy levers Part A deliberately deferred - `Country.FamilyPolicyLevel` (0-100, 50 =
neutral, nudges `EconomyState.BirthRate`) and `Country.ImmigrationPolicyLevel` (0-100, 50 = neutral,
nudges `EconomyState.NetMigrationRate`) - following the exact `PolicyDecision.XOverride` (-1 sentinel)
/ `Country.XLevel` (persistent) / `SimulationManager.ApplyDemographicPolicyChanges` (clamp-and-set,
called before `ApplyDemographicRates` the same "avoid a one-turn lag" way `ApplyCrimeJusticeDeeperChanges`
already does) / GameController slider pattern every other policy dial in this project already uses.
Explicitly required to flow through the ALREADY-CORRECTED `YearsPerTurn`-scaled `ApplyPopulationGrowth`
pipeline from the two Part A corrections above, not bypass it.

- **A real bug found and fixed before validation, not glossed over**: the first version applied each
  lever's effect as a CONSTANT ADDITIVE TERM directly onto `BirthRate`/`NetMigrationRate` every turn
  (`BirthRate = Clamp(BirthRate - decline + policyEffect, Min, Max)`). A throwaway diagnostic (neutral
  vs. maxed vs. minned levers, 500 turns, USA) caught this immediately: holding either slider at 100
  ratchets its target rate to its hard ceiling (`MaxBirthRate`/`MaxNetMigrationRate`) within
  single-digit turns and parks it there for the rest of the run - reintroducing, one layer upstream,
  the EXACT "no reversion, runs to an extreme and stays" failure pattern the Population growth-rate
  corrections above were written to fix, and nowhere close to the "small, bounded... this lever moves
  the needle, it does not reverse the trajectory" discipline this item was scoped under.
- **Fix - policy effect as a fresh, non-compounding offset from a policy-independent trajectory**:
  added `EconomyState.NaturalBirthRate`/`NaturalNetMigrationRate`, which evolve via ONLY the pre-existing
  secular-decline/aging-drift terms (never touched by either lever). `BirthRate`/`NetMigrationRate`
  (the fields everything else already reads) are recomputed FRESH each turn as `Clamp(Natural +
  thisTurnsPolicyOffset, Min, Max)`, not accumulated onto themselves - so holding a slider at any fixed
  value produces a constant, bounded shift from the underlying secular trend (which itself keeps
  moving), not an ever-growing one, while staying fully responsive if the slider changes mid-run.
  Re-tested via the same diagnostic: MAXED (both levers at 100) now shows `BirthRate`/`NetMigrationRate`
  tracking "natural trajectory + constant offset" (e.g. USA `BirthRate` 12.09 -> 11.10 -> ... -> 7.10 at
  turns 1/100/.../500, exactly the neutral run's own declining shape shifted up by +1.5 the whole time)
  instead of pinning at the ceiling.
- **Sensitivities, small and bounded per this item's own discipline**: `FamilyPolicyBirthRateSensitivity`
  = 0.03 (+/-1.5 points on `BirthRate` at the slider's full extremes) - deliberately small, since
  real-world evidence on pro-natalist policy's effect on fertility is itself small and contested (already
  flagged honestly in `EconomyState.BirthRate`'s own doc comment when Part A was written).
  `ImmigrationPolicyNetMigrationSensitivity` = 0.1 (+/-5 points on `NetMigrationRate`) - deliberately
  WIDER, since immigration policy is a genuinely more responsive real-world lever than fertility (visa/
  asylum/quota changes can move actual migration within a single term, unlike birth rates) - exactly
  what `EconomyState.NetMigrationRate`'s own Part A doc comment anticipated. A new `MaxBirthRate` (20)
  safety ceiling was added - unreachable in Part A (`BirthRate` only ever declined) but now needed
  since `FamilyPolicyLevel` can push it up.
- **No double-counting with `LaborForceParticipationRate`, verified structurally, not by convention**:
  `ImmigrationPolicyLevel`'s effect lands on the SAME `NetMigrationRate` that Part A's
  `ApplyLaborForceParticipationRate` combined ceiling already reads (the "NetMigrationRate gap vs.
  Country.BaselineNetMigrationRate" term) - no second, parallel immigration-to-labor-force channel was
  added. This is exactly the double-counting risk this item's own roadmap brief flagged as a "genuine
  design decision to expect"; it's avoided because there is structurally only one variable and one
  downstream channel, not because of a convention that could be violated later.
- **New stress scenario**: `demographicpolicystress` pushes USA's `FamilyPolicyLevel` and
  `ImmigrationPolicyLevel` both to 100 at turn 1 and holds - the worst-case simultaneous push toward
  MORE population growth, exercising `MaxPopulationGrowthRateDeviation`'s cap and the LFPR ceiling's
  `NetMigrationRate`-gap term at once, under the corrected `YearsPerTurn`-scaled pipeline. Added
  directly to `SimulationTestRunner` (no standalone-harness equivalent, since the harness was already
  superseded as the source of truth before this item started).
- **UI**: two new sliders (Family Policy, Immigration Policy) added to the Labor Market tab -
  immigration's connection to labor supply already lives there via the existing LFPR graph, and Family
  Policy fits naturally alongside it rather than opening a new tab for two dials. A plain-text
  demographic summary line (Population, growth rate, Birth/Death/Net Migration, Dependency Ratio) was
  added below the existing LFPR graph too, so the levers are visibly connected to something - full
  `StatHistory` graph tracking for the five demographic fields remains deferred (Part A didn't add it
  either; out of scope for what this pass asked for).
- **Validated - full real-Unity matrix (`BatchSimulationRunner -runmatrix`, all 13 scenarios x
  100/500 turns, 26 combinations)**: zero anomalies for any demographic field (`Population`/
  `PopulationGrowthRate`/`DependencyRatio`/`BirthRate`/`DeathRate`/`NetMigrationRate`/`NaturalBirthRate`/
  `NaturalNetMigrationRate`/`FamilyPolicyLevel`/`ImmigrationPolicyLevel`) anywhere; per-scenario totals
  (76-191 at 100 turns, 399-964 at 500) landed within the same established range, including the new
  `demographicpolicystress` scenario itself (99/399) - not an outlier. `demographicpolicystress`'s
  real-Unity USA results (Population 502.573M at turn 500, `PopulationGrowthRate` 2.200) matched the
  throwaway diagnostic's MAXED run EXACTLY, confirming the diagnostic's fidelity before spending the
  full matrix run on it. Compared to the baseline scenario (neutral 50/50 levers, Population 483.686M),
  maxing both levers produces a real, felt, but bounded +3.9% higher Population at turn 500 - "moves
  the needle," as intended, not a runaway divergence; GDP/Unemployment/Inflation/`DebtToGdpRatio` all
  stayed within their own normal ranges too, confirming no unexpected interaction with the rest of the
  fiscal/labor system.
- **This concludes Round 3 item 5 (Demographics)** - both Part A (plumbing, drift, three reconciled
  effects, corrected twice for the growth-rate reversion and turn-year-conversion bugs) and Part B (the
  two policy levers, corrected once for the ratchet-to-ceiling bug) are done and validated.

## Cabinet (Political Systems Overhaul Part A)
`POLISIM_MASTER_ROADMAP.md` Master Sequence step 1 - the first item under the new master roadmap
that consolidated `ROADMAP_BRIEF.md`, `CONTINUOUS_TIME_MIGRATION.md`, and
`POLITICAL_SYSTEMS_OVERHAUL.md`. Two independent mechanics per appointed cabinet minister: a passive,
always-on competence effect landing on an existing audited channel, and an interactive decision layer
(short scenario + 2-3 response options) reusing `EventSystem`'s overall shape but requiring a
player-picked response rather than auto-applying.

- **Only 3 of the confirmed 6 portfolios implemented this pass** (`CabinetPortfolio`: FinanceTreasury,
  InteriorJustice, HealthSocialAffairs) - the Master Roadmap's own content-authoring warning ("6-8
  roles x 2-3 candidates x multiple decisions each is a real content burden - build 2-3 portfolios
  with real, fully-realized content first... then expand") called for this explicitly, mirroring
  `SectorType`'s own history (4 sectors at launch, 4 more only added when Round 3 item 4 actually built
  them - never defined upfront as unused placeholders). ForeignAffairs/Defense/EconomyTradeIndustry are
  deliberately not yet defined in the enum itself, not just hidden in the UI.
- **Data model**: `CabinetMinister` (name/portfolio/philosophy/description/`CompetenceBias`, an
  ORIGINAL FICTIONAL character - never a real person, the same rule `FedChair` already established) and
  `CabinetDecision`/`CabinetDecisionOption` (scenario text plus a small flat set of possible one-time
  shock fields - `CrimeIndexShock`/`PovertyRateShock`/`BudgetImpact`/`ApprovalEffect` - mirroring
  `EconomicEvent`'s own shape, most options only setting 1-2 of them). `Country.CabinetMinisters`
  (`Dictionary<CabinetPortfolio, CabinetMinister>`) is empty by default for every country, the same
  "doesn't exist until the player acts" idiom `SovereignWealthFund`/`CurrentFedChair` already use - the
  Cabinet UI only ever lets the player appoint into their OWN country, so no NPC/player branching is
  needed anywhere `CabinetSystem` or its effect-landing call sites read this dictionary; every other
  country's dictionary just stays empty forever and an empty-dictionary lookup naturally contributes
  zero effect.
- **`CabinetMinisterPhilosophy` (Reformist/Pragmatic/Traditionalist) is a SEPARATE axis from
  `CompetenceBias`** - unlike `FedChairPhilosophy` (where Hawkish/Dovish directly signs `RateBias`),
  philosophy here only selects which `DecisionPool` a minister draws scenarios from ("a Reformist vs.
  Hardline Interior Minister should generate genuinely different scenarios, not just a different
  number" - the Master Roadmap's own explicit instruction), while `CompetenceBias` is always a small,
  bounded, BENEFICIAL magnitude regardless of philosophy (a weaker candidate has a smaller bias, not a
  harmful one - "hire someone actively bad at governing" isn't a real candidate archetype the way
  Hawkish vs. Dovish is a real, symmetric monetary-policy axis).
- **Passive competence effect, one per portfolio, each folded into an already-audited channel** (see
  `CabinetSystem.GetCompetenceBias`, applied at point-of-use every turn, never mutating a stored
  structural field - the same idiom `FederalReserveSystem.ApplyFedChairInterestRate` already
  established for `RateBias`):
  - **FinanceTreasury -> effective `CollectionEfficiency`** (`SimulationManager.
    ApplyRevenueAndSpending`): `Mathf.Clamp01(country.CollectionEfficiency + bias)`, deliberately NOT
    mutating the stored `CollectionEfficiency` field - that field has no reversion mechanism of its own
    to correct a permanent drift, the specific risk the Master Roadmap's own "may be safer to land
    somewhere more contained" guidance for this portfolio was flagging.
  - **InteriorJustice -> `CrimeIndex` target** (`MacroSystem.ApplyCrimeIndex`): one more gap-based
    subtraction term alongside PoliceFunding/Sentencing/BailReform/OrganizedCrimeIndex, landing inside
    the SAME final `Clamp(0, 100)` that already serves as this stat's combined ceiling - no separate
    ceiling needed since it's structurally the same pattern as every existing sensitivity term there.
  - **HealthSocialAffairs -> `PovertyRate` target** (`MacroSystem.ApplyPovertyRate`): one more
    reduction term alongside `welfareReduction`/`minimumWageReduction`, same reasoning.
- **Interactive decisions**: `CabinetSystem.TryRollDecisions` rolls independently per appointed
  minister each turn (~12%/minister, matching `EventSystem.EventChancePerTurn`'s own baseline), called
  from `SimulationManager.ApplyDomesticPolicy` right alongside `EventSystem.TryRollEvent`. Unlike
  `EconomicEvent`, nothing auto-applies - fired decisions accumulate in a new
  `_pendingCabinetDecisionsByCountry` dictionary until `ResolveCabinetDecision` (called from
  `GameController` once the player picks a response) applies the chosen option's one-time shock via
  `CabinetSystem.ApplyDecisionOption` and clears it. `GameController` blocks Advance Turn while any
  decision is pending (`hasPendingCabinetDecisions`, OR'd into the existing gate alongside
  `hasPendingFedChairSelection`) - the same "must resolve before advancing" idiom Fed Chair candidate
  selection already established, not something silently skippable by racing ahead.
- **Content this pass**: 3 candidates per portfolio (one per philosophy, `CabinetSystem.
  GenerateCandidates` returns all 3 shuffled - simpler than `FederalReserveSystem`'s own sampling,
  since the pool is already exactly "2-3 candidates" without needing to draw a subset), 2 decisions per
  (portfolio, philosophy) = 18 decisions total, each with 2 response options - 9 x (portfolio,
  philosophy) combinations, genuinely different scenario text per philosophy, not the same text with a
  different number.
- **Reshuffle**: player can replace a minister anytime, costs a flat `CabinetSystem.
  ReshuffleApprovalCost` (2 points) - the same small magnitude class as `EventSystem`'s own
  `ApprovalEffect` range (-2 to -5), not a separately invented scale.
- **UI**: new Cabinet tab (14th tab, sharing row 3 with Policy Web at half-width each, `UiPalette.
  SystemArea.Political` - the same area Federal Reserve uses, matching the existing "shared area across
  related tabs" precedent Tax Policy/Spending Policy already set for Fiscal). One panel per portfolio
  (minister name/philosophy/description + Reshuffle, or a candidate picker if vacant, mirroring
  `DrawFedChairCandidateButton`'s own pattern); any pending decision renders above the portfolio panels
  with the same visual weight as the dashboard's own "BREAKING" event banner (`_eventBannerStyle`),
  scenario text plus response-option buttons.
- **A syntax discovery worth recording**: `Dictionary<TKey,TValue>`'s `{ key, value }`
  collection-initializer shorthand doesn't parse reliably when `TKey` is itself a value tuple (here,
  `(CabinetPortfolio, CabinetMinisterPhilosophy)`) - hit `CS1525: Invalid expression term '}'` at every
  entry's closing brace. Fixed by switching to index-initializer syntax (`[key] = value`), which
  sidesteps the ambiguity entirely and is equally valid for a static readonly dictionary.
- **UI smoke test** (screenshot-driven, matching this project's established `UiScreenshotDriverN`/
  `UiScreenshotRunnerN` throwaway pattern): confirmed the vacant-portfolio candidate-search flow, a
  fully-appointed Cabinet (three named ministers with philosophy/description/Reshuffle), and - after
  advancing turns until one fired naturally - a decision modal rendering correctly with the
  BREAKING-banner-style header, scenario text, and both response buttons, AND the Advance Turn button
  visibly disabled while it was pending, confirming the blocking gate works end-to-end.
- **Validated - full real-Unity matrix (`BatchSimulationRunner -runmatrix`, all 14 scenarios x
  100/500 turns, 28 combinations, including a new `cabinetstress` scenario)**: zero new anomaly TYPES
  anywhere - `cabinetstress`'s own anomalies (99 at 100 turns, 444 at 500) are exclusively the
  pre-existing "swung X% in one turn" pattern already documented as ambient Sweden/France SWF-driven
  noise (and near-zero-base percentage swings, e.g. USA's `InterestRate` 0.02 -> 0.03), landing in the
  SAME range as `baseline`'s own 86/433 and every other scenario (410-506 at 500 turns) - not an
  outlier, and no `CrimeIndex`/`PovertyRate`/`Budget`-related anomaly among them anywhere. The other 13
  pre-existing scenarios showed no regression (`GetCompetenceBias` returns 0 for every country in every
  scenario except `cabinetstress`, since none of them ever appoint a minister). `cabinetstress` appoints
  the HIGHEST-`CompetenceBias` candidate in each of the 3 portfolios at turn 1 and holds them for the
  whole run (worst-case SUSTAINED stress on the passive effect, which only ever pushes its target in one
  beneficial direction, unlike a symmetric slider extreme) and auto-resolves every fired decision by
  always picking whichever response option has the largest absolute combined effect (worst-case stress
  on the interactive channel too, since no player is present to click a response).
- **Directional confirmation via a targeted diagnostic** (temporary `CrimeIndex=`/`PovertyRate=`
  logging added to `SimulationTestRunner`'s per-turn line, two quick single-scenario 500-turn runs, then
  reverted): USA's turn-500 figures under `cabinetstress` vs. `baseline` - `CrimeIndex` 41.42 vs. 45.01
  (-3.59), `PovertyRate` 17.44 vs. 19.28 (-1.84), `DebtToGdpRatio` 138.2% vs. 146.1% (-7.9pp) - all three
  channels show a real, felt, correctly-directioned, clearly bounded improvement, not just "technically
  in range."

## UI/Graph Restyling and Political Visualization (Political Systems Overhaul Part C)
`POLISIM_MASTER_ROADMAP.md` Master Sequence step 2. Pure UI/visualization - no new simulation state
or formula beyond `StatHistory.MaxEntries`' own bounded retention increase (still a fixed-size rolling
window, just a bigger one). Three pieces: graph restyling (threshold lines + pagination), a political
compass, and demographic pie charts - all reading data this game already tracks, per this item's own
"grounded in real, already-tracked data" instruction.

- **Threshold/target reference lines** (`GraphRenderer.Draw`'s new optional `thresholdValue`/
  `thresholdLabel` parameters): a second, dashed, distinctly-colored (warm amber, vs. the plain gray
  midline gridline) horizontal line, folded into the same auto-scale range calculation the data/
  projected-value already use - a reference line stays visible even on a page where the data sits far
  from it, the whole point of "how far from target are we." Wired into the two graphs with an obvious
  single reference point: Unemployment -> `Country.NaturalUnemploymentRate` ("NAIRU"), Debt-to-GDP ->
  `Country.ComfortableDebtToGdpPercent` ("Comfortable"). Every other graph left without one - GDP/
  Approval/Trade Balance/etc. have no single natural target value the way NAIRU or a comfortable debt
  level do.
- **"Last N changes" pagination**: `StatHistory.MaxEntries` raised 50 -> 250 (5 pages of
  `GraphRenderer`'s own 50-turn display window) - pagination needs real older data to page back into,
  not just blank pages past the first. `GraphRenderer` now slices its own 50-turn window internally
  from whatever history it's given (up to 250 entries) and exposes "< Older"/"Newer >" buttons plus a
  "how far back" label; the title row's own percent-change figure and the next-turn projection both
  correctly scope to the CURRENT PAGE (the projection only ever shows on the most recent page - it
  makes no sense appended to an older window). Every one of the 12 existing graph call sites in
  `GameController` needed their title text's hardcoded `"(last 50 turns)"` removed (now dynamic/
  paginated, not a fixed window) - a small mechanical edit repeated at each site, not a design change.
- **Political compass** (`PoliticalCompassRenderer`, new): a 2D scatter plot, one dot per country -
  X axis blends each country's average implemented tax rate with its total government spending (%
  of GDP, whichever mechanism it actually uses - detailed `SpendingLines` if present, else the legacy
  `GovernmentSpendingRate` baseline); Y axis blends average `Sector.RegulationLevel` with average
  implemented `WelfareProgram.GenerosityLevel`. Both axes are grounded entirely in this game's own
  already-tracked policy dials - no invented ideology labels, matching this item's own explicit
  instruction. Player's own country ringed in white.
  - **Bug found and fixed before shipping**: a first pass used a FIXED 0-100 axis range (matching
    every policy dial's own scale) - screenshotted with all six countries at turn 65, real-world
    policy variance turned out modest enough that all six dots clustered into a tight, overlapping,
    illegible clump with garbled overlapping name labels. Fixed by auto-scaling both axes to the
    OBSERVED min/max across the given countries (padded 15%) - the exact same "zoom into whatever real
    variance exists" philosophy `GraphRenderer`'s own Y-axis auto-scaling already uses - plus a light
    vertical label-decluttering pass (push a label down just enough to clear the previous one, sorted
    top to bottom) so labels can never overlap even if two dots land close together. Re-screenshotted:
    all six countries clearly separated and legible. The verbose absolute-positioned axis corner labels
    from the same first pass (e.g. "More regulated / generous welfare state") also collided with each
    other and clipped at the panel edge - replaced with two short `GUILayout.Label` rows below the plot
    (plain layout flow, not manually-positioned overlay text prone to the same class of collision)
    showing the actual observed range per axis.
- **Demographic pie charts** (`PieChartRenderer`, new, generic - reused five times): per-pixel angle
  test against the circle's center (every pixel within radius colored by whichever slice's angular
  range its angle falls into) - no polygon-fill logic needed, same "test every pixel" spirit
  `PolicyWebRenderer.BuildCircleTexture` already uses for a plain filled disc. `UiPalette.
  GetCategoricalColor` (new) provides one distinct color per slice for N-way breakdowns with no
  existing per-category color (golden-angle hue spacing, so adjacent indices never look similar
  regardless of N). Five charts, all on the player's own country except Population (inherently
  comparative): Working-Age vs. Dependent Population (`100 - DependencyRatio` vs. `DependencyRatio`),
  Employment Share by Sector, Spending Allocation (detailed `SpendingLines` where present - USA only
  in this pass - else an honest "not tracked for this country yet" fallback message rather than a
  fabricated breakdown), Theoretical Tax Revenue by Source (`GDP * Rate/100 * BaseShareOfGdp` per
  implemented `TaxLine`, the same formula `SimulationManager.GetTotalTaxRevenue` already uses,
  re-derived from PUBLIC fields rather than exposing that private method), and Population Share by
  Country. Ethnicity/religion breakdowns explicitly OUT OF SCOPE per this item's own spec - not
  tracked anywhere in this game's data model.
  - **A screenshot ambiguity worth recording, resolved before it became a wasted fix**: an early
    screenshot showed what looked like a half-filled circle (a plain dome shape) for the first pie
    chart, right at the captured viewport's bottom edge - genuinely unclear whether this was a real
    rendering bug (e.g. an angle-winding/coordinate-flip error between texture-array space and
    screen space) or just the tab's own scroll position cropping the image before the circle's lower
    half. Rather than guessing, a second screenshot with the tab's scroll position forced further
    down (via reflection, the same throwaway-driver technique used throughout this project's UI
    smoke tests) confirmed every pie renders as a genuine full circle with correctly-proportioned
    wedges - the "half circle" was purely a cropped screenshot, not a defect. Worth the extra
    verification step rather than either shipping unverified or spending time "fixing" nonexistent
    coordinate-space code.
- **UI**: new "Compass & Demographics" tab (15th tab, 4th tab row, full-width - the same "one new tab
  alone in its own row" precedent Policy Web's original third row established), `UiPalette.SystemArea.
  Global` (the same cross-cutting-overview area World Map uses, since this tab isn't owned by any one
  policy area either).
- **UI smoke test** (screenshot-driven): confirmed the NAIRU/Comfortable threshold lines render
  correctly on their respective graphs, the pagination row appears once history exceeds 50 turns and
  correctly shows a different, real older data window with an accurate "51-100 turns ago" label when
  paged back, the political compass (after its fix) shows all six countries clearly separated and
  legible, and all five pie charts render as genuine full circles with correct wedge proportions and
  legends. One driver bug found and fixed along the way: advancing 65 turns crosses at least one
  re-election boundary (`ElectionSystem.ElectionCycle` = 12 turns), which sets `_pendingElectionResult`
  and makes `OnGUI` show the election reveal screen instead of any tab content until dismissed - the
  driver now clears that field after each simulated turn (mirroring what clicking "Continue" does),
  the same category of fix `UiScreenshotDriver3`'s duplicate-`GameController`-instance bug and
  `UiScreenshotRunner`'s stale-backup-scene bug were earlier in this project's own UI-testing history.
- **Validated**: single-scenario smoke check (100-turn baseline via `BatchSimulationRunner`, per this
  item's own "pure UI/visual" validation bar, lighter than Cabinet's full-matrix requirement since
  nothing here touches a simulation formula) - 74 anomalies, all the same pre-existing "swung X% in one
  turn" ambient-noise pattern already documented, zero new anomaly types.

## Continuous Time Migration Phase 0 (Master Sequence step 3)
`POLISIM_MASTER_ROADMAP.md` Master Sequence step 3 - the calendar/UI/scaffolding layer that Phases
1-5 (the actual daily-granularity math conversion, step 7, deliberately much later) will build on.
Confirmed with Elias before starting: this phase keeps ALL existing economic math at its current
121-day turn cadence, unchanged internally - no constant translation happens yet. Scoping decisions
made and stated up front for item 5's intentionally vague "short-term gameplay scaffolding" bullet:
skip the law-passing mechanic entirely (the spec's own text says Political Systems Overhaul Part B
supersedes it - building a competing version here would be wasted work), build ONE small
proof-of-pattern interrupt slice (Foreign Policy Meetings) rather than three separate systems, and
explicitly defer "ongoing-process budgets" as a named open item rather than invent scope for it.

- **Calendar core** (`SimulationManager`): `CurrentDate` (a `System.DateTime`, starting at an
  honestly-arbitrary epoch of 2026-01-01) plus `AdvanceDay()`, which advances one day and returns
  `true` exactly when a `DaysPerTurn` (121) boundary is crossed since epoch. `AdvanceDay()` itself
  never calls `AdvanceTurn()` - only the UI layer has visibility into the current draft policy
  sliders needed to build a `PolicyDecision`, so the boundary signal is just that, a signal.
- **Speed control and the day-processing loop** (`GameController`): a new `GameSpeed` enum
  (Paused/Normal/Fast/VeryFast, ~2 minutes/turn at 1x down to ~30s/turn at 3x - an untested first-pass
  pacing placeholder, not tuned against playtesting) drives a new `Update()` method (runs every engine
  frame, unlike `OnGUI` which only fires on repaint) that accumulates `Time.deltaTime` against a
  per-speed "seconds per in-game day" rate and calls `AdvanceDay()` in a loop. On a `true` return it
  calls the EXISTING, byte-for-byte UNCHANGED private `AdvanceTurn()` method - the same one the old
  "Advance Turn" button used to call, just with its call site moved. Time itself now pauses on every
  gate that used to just disable that button (no country selected, game over, a pending election
  reveal/Fed Chair selection/Cabinet decision) PLUS a new one this phase adds (a pending Foreign Policy
  Meeting) - re-checked immediately after each `AdvanceTurn()` call too, so the loop can't drain
  further simulated days against a state that just became blocked mid-frame.
- **UI**: `DrawAdvanceTurnButton` removed entirely, replaced by `DrawCalendarAndSpeedControls` (date
  label, a conditional "why time is paused" reason line, Pause/1x/2x/3x button row) in the same layout
  slot.
- **Live Policy Preview redesign**: a new `PreviewHorizon` enum (1 Day/1 Week/1 Month/Full Turn, 1 Day
  default) with a selector row. Every displayed figure is a DISPLAY-ONLY re-scaling of the SAME
  full-turn `PreviewTurn` output (no new simulation run) - additive/"points changed" values use
  `ScaleLinearForDisplay` (`value * horizonDays / 121`), GDP growth % uses
  `ScaleCompoundingForDisplay` (geometric: full-turn multiplier -> implied daily multiplier via
  `Mathf.Pow(x, 1/121)` -> raised to `horizonDays`) - the same "identify which mathematical shape a
  constant is" methodology the Master Roadmap's own translation section describes, applied here only
  to a display transform, never a real simulated constant. A parallel "Raw" (always full-turn) set of
  cached fields is kept deliberately separate from the new "Scaled" (horizon-dependent) set, so the
  dashboard's own next-turn dashed graph projection - which always means "next turn," regardless of
  which horizon the preview panel has selected - can never accidentally read a horizon-scaled value.
- **Multi-resolution `StatHistory`** (`MultiResolutionSeries`, new): each of the 13 tracked stats is
  now four independently-gated `List<float>` buffers (Daily/Weekly/Monthly/Quarterly, each period
  1/7/30/91 days, each capped at `StatHistory.MaxEntries`) instead of one flat list. Honestly disclosed
  in the class's own doc comment: Phase 0 itself can't yet make the four resolutions diverge - 121
  days already exceeds even the 91-day quarterly period, so every resolution ends up holding IDENTICAL
  one-point-per-turn data until Phases 1-5 give stats genuine day-to-day variation. Every existing
  graph call site (`GameController`, `PolicyWebRenderer.GetHistory`) now reads `.Quarterly`
  specifically, chosen because a 91-day period still only ever accepts one point per 121-day turn -
  zero visual change to any existing graph from this migration.
- **Foreign Policy Meetings** (the one short-term-scaffolding proof-of-pattern slice built this pass):
  `ForeignPolicyMeeting`/`ForeignPolicyMeetingOption` (new, `Assets/Scripts/Data/`) mirror
  `CabinetDecision`/`CabinetDecisionOption`'s shape - short scenario text plus 2-3 response options
  with a small flat set of one-time shock fields (`ApprovalEffect`/`BudgetImpact`/`TradeBalanceShock`,
  the last one new since a meeting with a foreign counterpart is the natural place for a
  trade-relations nudge, landing on a stat that already mean-reverts every turn). `ForeignPolicySystem`
  (new, `Assets/Scripts/Simulation/`) is deliberately a FLAT undifferentiated pool (no per-portfolio/
  per-philosophy branching like Cabinet's own `DecisionPool` - there's only one "foreign ministry"
  here), rolled at 1%/day (`MeetingChancePerDay`, targeting roughly one meeting per 121-day turn on
  average) rather than Cabinet's once-per-turn cadence, since meetings are meant to land BETWEEN turn
  boundaries now that the calendar advances continuously. `SimulationManager` tracks at most ONE
  pending meeting per country (`_pendingForeignPolicyMeetingByCountry`, unlike Cabinet's per-minister
  list) via `TryRollForeignPolicyMeeting`/`GetPendingForeignPolicyMeeting`/
  `ResolveForeignPolicyMeeting`; `GameController.Update()` rolls it once per simulated day for the
  player's country only (NPC countries have no UI to ever resolve one through). New "Foreign Policy"
  tab (16th tab, shares row 4 with Compass & Demographics at half-width each, `UiPalette.SystemArea.
  Trade`) shows the pending meeting (if any) with the same `_eventBannerStyle` visual weight Cabinet's
  own decision modal uses, or "No meeting currently pending."
- **Deferred, not silently dropped**: "ongoing-process budgets" (item 5's third named piece) is out of
  scope for this pass - no design work was done on it here, and it should be scoped fresh (not
  retrofitted onto Foreign Policy Meetings' shape) whenever it's actually picked up.
- **Tick-equivalence validation** (explicit requirement: prove the automatic 121-day tick produces
  results indistinguishable from the old manual "Advance Turn" button): a true byte-identical
  "run twice and diff the output" comparison was deliberately NOT attempted - `EventSystem`/
  `CabinetSystem`/`ForeignPolicySystem` each already kept their own unseeded, process-lifetime
  `static readonly System.Random` before this phase, so two separate trigger paths draw from different
  points in those streams and would legitimately diverge in which random event/decision/meeting fires,
  for reasons that have nothing to do with the day/turn translation itself. Instead, two things were
  proven directly: (1) `git diff` confirms `AdvanceTurn()`'s method body is byte-for-byte unchanged -
  only its call site moved from the old button's `OnClick` handler to `Update()`'s day-boundary
  branch, so identical inputs unconditionally produce identical output regardless of caller; (2) a
  throwaway `TickEquivalenceDiagnostic1` (Edit-mode only, no Play-mode round-trip needed since
  `SimulationManager.Awake()`/`SetWorld` both fire synchronously on `AddComponent`) confirmed the ONLY
  new code in the path - `AdvanceDay()`'s boundary arithmetic - fires at EXACTLY day 121/242/363/
  484/605 over 5 simulated turns, never off-by-one or double-fired, and that non-boundary `AdvanceDay()`
  calls leave every tracked `EconomyState` field (GDP/Unemployment/Inflation/ApprovalRating/Budget)
  completely untouched across 120 consecutive days - a pure calendar advance with zero economic side
  effects of its own. PASS on both.
- **Validated**: single-scenario smoke check (100-turn baseline, `BatchSimulationRunner`, matching this
  phase's own "no economic math touched" validation bar) - completed cleanly with only the same
  pre-existing "swung X% in one turn" ambient-noise warnings already documented elsewhere in this file,
  zero new anomaly types, zero exceptions.
- **UI smoke test** (screenshot-driven): confirmed the calendar date label, the conditional pause-
  reason line, and the Pause/1x/2x/3x speed row all render correctly with 1x active by default; the
  redesigned live preview's horizon button row (1 Day/1 Week/1 Month/Full Turn) renders with 1 Day
  selected; and the new Foreign Policy tab renders correctly (teal `Trade`-area tint, description text,
  "No meeting currently pending" in the no-meeting state).
- **A tooling quirk worth recording, not a code regression**: `BatchSimulationRunner`'s own
  post-Play-mode exit sequence (`EditorApplication.isPlaying = false;` immediately followed by
  `EditorApplication.Exit(0);`) hung for 20+ minutes mid-domain-reload/asset-reindex on one run this
  session, well past every prior run's normal exit time - the 100-turn scenario itself had already
  completed cleanly (visible in the log before the hang) and the hang was killed and worked around by
  building `TickEquivalenceDiagnostic1` as an Edit-mode-only script instead of a Play-mode one. Not
  investigated further since it didn't block validation; worth a look if `BatchSimulationRunner` hangs
  again in a future session.

## Parliament PILOT (Political Systems Overhaul Part B), Master Sequence step 4
`POLISIM_MASTER_ROADMAP.md` Master Sequence step 4 - the largest single architectural change in the
project's history by the roadmap's own framing (Parliament gates all eight policy tabs once fully
rolled out), deliberately proven on ONE tab first. Piloted on Tax Policy specifically (well-understood,
already-isolated implement/adjust/remove semantics - one list of `TaxLine`s, one apply function) rather
than all eight tabs at once, so any problem with the gating MODEL itself (bill timing, seat inertia,
the pass/fail formula) surfaces against a single well-understood lever shape, not entangled with eight
different lever shapes simultaneously. Confirmed with Elias before starting. Full rollout to the other
seven tabs is step 5, not attempted here.

- **Parties** (`PartyArchetype`, `PartyArchetypeData` - `Assets/Scripts/Data/PartyArchetype.cs`): four
  original, generic, clearly-fictional archetypes (Progressive Alliance, Conservative Union, Centrist
  Coalition, Nationalist Front), per the roadmap's own "never a real party name" instruction. A SHARED
  taxonomy applied identically across all six countries (mirroring `CabinetMinisterPhilosophy`'s own
  single shared taxonomy) - "per country" means each country tracks its own seat DISTRIBUTION across
  this same archetype set, not that each country has differently-named parties.
- **Seats** (`ParliamentSystem.UpdateSeats`, `Country.ParliamentSeats`) - the roadmap's own Open
  Question, resolved here as a STATED proposal, not a silent guess:
  - `ParliamentConstants.TotalSeats` = 200 per country (an arbitrary round number for a clean
    visualization, not modeled on any one real chamber's size).
  - Each archetype has three fixed constants: `BaseSupportShare` (floor/starting weight, sums to 1.0),
    `ApprovalSensitivity` (how the archetype's target share moves with the sitting government's
    `ApprovalRating`), and `FiscalStance` (-1 to +1, used for bill alignment - see below).
    Progressive/Conservative/Centrist are ESTABLISHMENT-coded and gain modestly as approval rises
    (political stability favors mainstream parties broadly - this game never assigns the player's own
    government a party identity, so approval reads as benefiting establishment stability generally,
    not one specific party); NationalistFront is the PROTEST archetype, strongly INVERSE (a classic
    anti-incumbent backlash pattern, surging as approval falls).
  - Recomputed once per turn, for EVERY country (not player-gated like `CabinetMinisters` - seat
    composition derives purely from each country's own `ApprovalRating`, no player action needed):
    target share = `BaseSupportShare + ApprovalSensitivity * (ApprovalRating - 50) / 100`, floored at
    2% and renormalized, converted to a target seat count, then actual seats move toward that target by
    a BOUNDED step (at most 6 seats/turn) plus small (±1) random jitter - the same "gap-based target +
    bounded reversion rate" idiom this codebase already uses for `ApprovalRating`/`CrimeIndex`/
    `PovertyRate`, never snapping instantly. Seeded at world creation from `BaseSupportShare` alone
    (every country starts at `ApprovalRating` 50, where the sensitivity term is exactly 0).
- **The gated-legislation flow, Tax Policy only** (`TaxBill`, `ParliamentSystem`,
  `SimulationManager.IntroduceTaxBill`/`GetPendingTaxBill`/`AdvanceLegislativeDay`): sliders/toggles on
  the Tax Policy tab are now DRAFT-only (`_taxImplementDrafts` joins the existing `_taxRateInputs`) -
  adjusting them costs nothing and no longer reaches `TaxLine.Rate`/`IsImplemented` at all.
  `BuildPlayerDecision` (the ONE method both `AdvanceTurn` and the live preview share) no longer
  populates `PolicyDecision.TaxRateOverrides`, so the preview is honest too - it no longer shows a
  tax-driven effect that won't actually happen until a bill passes. Pressing "Introduce Bill" bundles
  EVERY `TaxType`'s current draft state into ONE omnibus `TaxBill` (a real tax bill touches many lines
  at once - simpler than one bill per line, and matches the roadmap's own "introduce the current draft"
  framing) and submits it via `SimulationManager.IntroduceTaxBill` - a no-op if one is already pending
  (only one bill per country at a time, mirroring Cabinet/Foreign-Policy-Meeting's own single-slot
  pattern). The bill counts down `ParliamentSystem.BillDurationDays` (21, a placeholder like Phase 0's
  own `GameSpeed` pacing) real in-game DAYS - not turns - via `SimulationManager.AdvanceLegislativeDay`,
  called once per simulated day from `GameController.Update`'s existing day-processing loop (stands in
  for the roadmap's introduction/committee/debate stages without modeling them separately, a
  deliberately simple first pass). Unlike Cabinet decisions/Foreign Policy meetings, a pending bill
  never pauses time - it's a deterministic countdown, not something needing a player response.
- **Pass/fail formula** (`ParliamentSystem.WouldBillPass`/`GetBillDirection`) - the roadmap's second
  Open Question, also resolved here as a stated proposal: the bill's net fiscal direction (sum of
  requested-effective-rate minus standing-effective-rate across every line, positive = net tax
  increase) is compared against each archetype's `FiscalStance`, weighted by that archetype's CURRENT
  seat share (read at resolution time, not introduction time - seats can shift mid-flight if a bill's
  21 days happen to cross a turn boundary, which is the more realistic and simpler design). A
  genuinely neutral bill (direction exactly 0) auto-passes - nothing to contest; otherwise the bill
  passes if the seat-weighted alignment sums positive, fails (including an exact tie) otherwise.
- **Applying the result** (`ParliamentSystem.ApplyBillResult`): PASS writes every bill line's
  Rate/IsImplemented directly onto the real `TaxLine`s (clamped to each `TaxType`'s own range, the same
  clamp `ApplyTaxRateChanges` already applies) and charges a one-time `ApprovalRating` penalty for any
  net rate increase, reusing `MacroSystem.TaxHikeApprovalSensitivity` - the SAME coefficient a direct
  tax hike already cost before this pilot, applied as a single immediate shock (Cabinet/Foreign-Policy's
  own idiom) rather than threaded through the turn-scoped `ApplyApprovalRating` formula, since a bill
  can resolve on any day, not just a turn boundary. FAIL leaves standing values untouched and charges a
  flat, smaller `ParliamentSystem.BillFailedApprovalCost` (1.5, vs Cabinet Reshuffle's 2 - failure here
  is Parliament's decision, not a player misstep); the draft is never lost either way (nothing clears
  `_taxRateInputs`/`_taxImplementDrafts`), matching the roadmap's own "can revise and reintroduce."
- **Federal Reserve/Eurozone exemption**: implemented as the default with ZERO new code - the
  interest-rate lever was never one of the eight tabs Parliament gates (Tax, Spending, Welfare, Labor,
  Crime & Justice, Sectors, Infrastructure, SWF), so leaving it untouched by this pilot already IS the
  exemption, not something requiring a special-case carve-out.
- **UI**: new Parliament tab (17th tab, 5th tab row, full-width - the same "one new tab alone in its
  own row" precedent Policy Web's own original third row established), `UiPalette.SystemArea.Political`
  (the same area Cabinet/Federal Reserve use). Shows `HemicycleRenderer` (new,
  `Assets/Scripts/UI/HemicycleRenderer.cs`) - a generic half-circle seat visualization reusing
  `PolicyWebRenderer`'s own "point on a circle via cos/sin" node-placement math per the roadmap's
  explicit instruction, just swept across 180 degrees instead of 360, across 5 concentric rows with
  seats-per-row roughly proportional to radius (a simple approximation, not a rigorous real-world
  packing algorithm) - plus a legend (seat count/share per archetype) and the pending bill's status
  (days remaining, and a live PASS/FAIL lean computed against the CURRENT seat composition). The Tax
  Policy tab itself now shows both "Standing" (legislated) and "Draft" (proposed) values per line, plus
  the Introduce Bill action/pending-bill status - the roadmap's own "every gated tab needs a visible
  Standing/Draft value" instruction, applied to the one tab actually gated this pass.
- **Validated - full real-Unity matrix** (`BatchSimulationRunner -runmatrix`, all 15 scenarios (14
  pre-existing plus a new `parliamentstress`) x 100/500 turns, 30 combinations): `parliamentstress`
  (`SimulationTestRunner`) keeps a maximal every-tax-at-its-own-ceiling `TaxBill` permanently in flight
  for USA all game (introduces a fresh one the instant the previous resolves, pass or fail - the same
  "always the most extreme option" stress philosophy `cabinetstress` already established for Cabinet's
  own interactive channel) - zero hard anomalies (no negative/NaN/out-of-range values) and, notably,
  ZERO USA-specific anomalies of any kind at either turn count; its total anomaly count (86 at 100
  turns, 434 at 500) lands squarely in the same range as every other scenario (74-99 / 398-471,
  excluding `swfstress`'s own already-documented outlier), confirming every anomaly present is the same
  pre-existing ambient "swung X% in one turn" noise from unrelated countries, not something new. Since
  `TaxBill` resolution is day-driven (`AdvanceLegislativeDay`) rather than turn-driven,
  `SimulationTestRunner.RunOne` gained a `parliamentstress`-only inner loop stepping 121 legislative
  days per turn (`CurrentDate` itself deliberately NOT advanced, since `AdvanceLegislativeDay` doesn't
  read it - every other scenario's own execution path is completely unchanged).
- **UI smoke test** (screenshot-driven): confirmed the Tax Policy tab's Standing/Draft split renders
  correctly (a drafted `IncomeTax` 37% -> 55% change and a drafted `VAT` implement both showed
  correctly against their unchanged Standing values), the "Introduce Bill" flow correctly shows "A tax
  bill is before Parliament - resolves in 21 day(s)" once submitted, and the Parliament tab renders a
  legible hemicycle (four correctly-colored, correctly-proportioned seat blocks: 64/48/24/64 at the
  turn-0 seed) with an accurate legend and a live PASS/FAIL lean that - independently confirmed by
  hand against the seat/stance numbers - correctly read FAIL for a net-tax-increase bill against that
  specific seat split, real evidence the pass/fail math is doing genuine work, not just displaying a
  static label.

## Master Sequence step 5a/5b/5c/5d (Political Systems Overhaul Part B, full rollout)
Step 5's original plan - "roll the Tax Policy pilot's uniform draft/introduce/vote pattern out to the
remaining seven tabs unchanged" - is SUPERSEDED (2026-07-31). Elias confirmed a more realistic,
better-specified design after a real freeze report (see `POLISIM_MASTER_ROADMAP.md`'s working
discipline patterns 5 and 6 for the investigation - an initial IMGUI stable-control-layout hypothesis
that turned out NOT to be the real cause, followed by the actual root cause: a legitimately
time-blocking decision with no globally-visible indicator) forced a closer look at what step 5 would
actually need: three bill tiers instead of
one uniform pattern (an Annual Budget omnibus bill per country on its real fiscal-year date; standalone
bills for introducing/removing a program entirely; standalone bills for non-budget policy, reusing the
same standalone mechanism as the second tier), built in six sub-phases 5a-5f with aesthetic restyling
deliberately last. Full design and reasoning: `POLISIM_MASTER_ROADMAP.md`'s "Step 5, full rollout -
REVISED DESIGN" under Part B - this file only documents what's actually been BUILT and validated, not
the design itself, to avoid two documents drifting out of sync.

**5a - real per-country fiscal-year dates + the mandatory pause hook. DONE (2026-07-31).**
- **`FiscalYearData`** (new, `Assets/Scripts/Data/FiscalYearData.cs`): `GetFiscalYearStart(CountryId)`
  returns (month, day) per real government fiscal-year conventions - USA October 1; Germany, France,
  Italy, Poland, Sweden all January 1 (calendar-year budgeting).
- **`SimulationManager.IsFiscalYearStart`/`TryOpenBudgetProcess`/`GetPendingBudgetProcess`/
  `AcknowledgeBudgetProcess`**: mirrors the existing `_pendingForeignPolicyMeetingByCountry` single-slot
  pattern (`_pendingBudgetProcessByCountry`, a `HashSet<CountryId>` - no bill payload yet, since 5a is
  pure plumbing and 5c/5d own the actual bill). `TryOpenBudgetProcess` is called once per simulated day
  from `GameController.Update`'s existing day-loop, alongside `TryRollForeignPolicyMeeting`/
  `AdvanceLegislativeDay`. Scoped to whichever country the player is actually controlling
  (`PlayerCountryId`), not hardcoded to USA - consistent with how every other Parliament/Cabinet/
  Foreign-Policy mechanic in this codebase already works; Fed Chair is this codebase's one deliberate
  USA-only exception, for a separately-stated real-world reason, not a precedent to extend here without
  cause. `AcknowledgeBudgetProcess` is an explicitly TEMPORARY 5a-only placeholder (clears the pending
  flag with no bill effect) - without it, the mandatory pause would leave the game genuinely,
  permanently stuck the first time a player's country's fiscal-year date arrives, since 5b (the Budget
  Process screen) and 5c (the real bill) don't exist yet to resolve it for real. Step 5c must replace
  its call site with the real Budget Process flow.
- **`GameController`**: `GetPendingBudgetProcess` added as a fourth condition on both of `Update`'s
  day-loop pause gates (the initial early-return and the mid-loop re-check), and as a fourth case on
  `DrawCalendarAndSpeedControls`' existing global pending-decision banner - originally built to fix a
  Foreign Policy Meeting visibility gap (see `POLISIM_MASTER_ROADMAP.md`'s working discipline pattern 6,
  "a legitimately time-blocking decision with no globally-visible indicator") - extending the existing
  pattern rather than building a fourth separate ad-hoc pause system, per the design's own explicit
  instruction. One ALWAYS-PRESENT "Acknowledge" button was added beneath the status line,
  `GUI.enabled`-gated rather than conditionally omitted - the stable-control-layout discipline (see
  `GameController.DrawTaxPolicy`'s own doc comment, and `POLISIM_MASTER_ROADMAP.md`'s working discipline
  pattern 5, "background/timed state mutation vs. active UI interaction") applies most acutely to
  exactly this screen shape (many sliders, a live-recomputing estimate, real-time day advancement), so
  this was built stable from its first draft rather than retrofitted after a freeze a second time.
- **Validated - a throwaway Edit-mode diagnostic** (same technique as Phase 0's
  `TickEquivalenceDiagnostic1` - no Play-mode round-trip; `GameController.Start()`/`Update()` invoked
  directly via reflection to exercise the REAL production gate logic, not a hand-copied reimplementation
  of it, with `_daySpeedTimer` force-set well above threshold since `Time.deltaTime` is meaningless
  outside Play mode) confirmed, for both USA (October 1) and Germany (January 1): the pending state
  opens correctly on the right date; `Update()`'s actual pause gate declines to advance `CurrentDate`
  while pending; `AcknowledgeBudgetProcess` both clears the flag and lets the day-loop genuinely resume
  on the next `Update()` call. ALL PASS, both countries. Diagnostic not committed (throwaway, per this
  project's own established convention). A separate infrastructure fix was found and committed on its
  own while building this diagnostic: `SimulationTestRunner` ran an unconditional 100-turn baseline pass
  on every Play-mode scene entry with no opt-out, so any unrelated Play-mode diagnostic paid for it
  first - fixed via an additive `-skipsimulationtestrunner` command-line flag, no effect on
  `BatchSimulationRunner` or any existing invocation.
- **Confirmed via live-Editor screenshots** (2026-07-31, by Elias directly - two automated headless
  screenshot-capture attempts both stalled during Unity's own cold-start/asset-reimport, unrelated to
  this code, so manual capture was used instead): the budget-process banner fires correctly on the real
  fiscal date with honest placeholder wording ("Budget Process screen not built yet - acknowledge below
  to continue for now"), and Acknowledge correctly resumes time - re-confirmed a full week later
  in-game (October 8), still ticking normally at 3x speed, i.e. the day-loop genuinely resumed rather
  than a one-frame fluke.

**5b - the Budget Process full-screen UI shell. DONE (2026-07-31).** No new bill logic - that's 5c/5d's
job; this phase only consolidates existing content onto one screen shell, per the design's own explicit
scope.
- **New "Budget Process" tab** (`GameController.DrawBudgetProcessTab`, 18th tab, shares row 5 with
  Parliament): three columns - left a category selector (`BudgetProcessCategory`: Tax/Spending/Welfare/
  Infrastructure/Swf), center the selected category's line-items, right the existing live Policy
  Preview panel (`DrawPolicyPreview`) reused as-is, not a new estimate.
- **Refactored** `DrawTaxPolicy`/`DrawSpendingPolicy`/`DrawWelfarePolicy`/`DrawInfrastructureTab`/
  `DrawSwfPolicy`, each splitting out a `*Content` method (everything but the outer box/scrollview) that
  both the original standalone tab and the new center column call - single source of truth, no
  duplicated slider logic. The five standalone tabs are unchanged otherwise and stay as independent
  entry points; only step 5e's tab consolidation removes them.
- **Stable-control-layout**: the center column's content switches based on `_budgetProcessCategory`,
  which only the player's own left-column button click can change - unlike a bill resolving in the
  background, a click can never race an active drag on a DIFFERENT control (one mouse, one control at a
  time), so this particular conditional swap isn't the hazard class `DrawTaxPolicy`'s own doc comment
  warns about. Each reused `*Content` method already carries its own stable-control-layout guarantee
  independently (including `DrawTaxPolicyContent`'s slider-always-present fix), so that safety property
  carries over automatically by reuse.
- **Two real layout bugs found via Elias's own live-Editor screenshots, fixed before this was
  confirmed:**
  1. The header description label was clipping mid-word instead of word-wrapping - the 3-column row
     below it could push the outer container's computed "natural" width past the real screen edge (a
     boxed column's `GUILayout.Width` request plus its `GUIStyle`'s own padding can add pixels beyond
     what was requested), so the label's wrap boundary was inferred against an inflated width. Fixed
     with an explicit `GUILayout.Width(availableWidth)` on the label, independent of whatever the row
     below computes.
  2. The reused Live Policy Preview panel was rendering catastrophically narrow - "Estimated Effects"
     wrapping to a single character per line. First attempt gave it `Screen.width * LeftColumnWidthFraction`
     (matching its native dashboard-column width) but capped it at half the row's own budget "so
     category/center never collapse to nothing on a narrow window" - that cap turned out to be the
     ACTUAL binding constraint at ordinary window sizes too, not just narrow ones. Root-caused (not
     guessed) via a temporary debug label reporting the real runtime pixel width, confirming ~641px
     observed vs. ~864px actually needed, cap binding rather than the `Screen.width` term. Corrected:
     the panel keeps its natural width unconditionally; category and center columns get sane MINIMUM
     widths instead of a share of leftover space; a genuine narrow-window overflow (all three don't fit
     at their natural/minimum sizes) is now handled explicitly via a horizontal scrollview around the
     whole row (new `_budgetProcessRowScrollPosition` field), rather than silently starving any one
     column.
- **Validated**: `dotnet build` clean (0 errors) after every iteration - a pure UI/layout change with no
  economic math touched, so `BatchSimulationRunner`'s matrix doesn't apply (same reasoning as 5a and the
  original Phase 0/UI Revamp items). Final layout confirmed via two rounds of Elias's own live-Editor
  screenshots (the second specifically requested after the first fix attempt didn't survive contact
  with reality) - code review and a compile check alone cannot verify IMGUI layout at runtime, the same
  lesson this project's IMGUI stable-control-layout investigation already established.

**5c - the omnibus annual budget bill + live-updating vote estimate. DONE (2026-07-31).** Replaces the
Tax Policy pilot's single-tax `TaxBill` with a genuine three-tier-design Tier 1 bill covering Tax +
Spending + Welfare + SWF together, resolved once a year on each country's real fiscal-year date.
- **`BudgetBill`** (new, `Assets/Scripts/Data/BudgetBill.cs`, replaces the deleted `TaxBill.cs`): per-
  `TaxType` implement/rate lines (unchanged shape from `TaxBill`), a `Dictionary<SpendingCategory, float>`
  of percent changes, per-`WelfareProgramType` implement/generosity lines, and six SWF fields (exists
  flag plus contribution rate/domestic allocation/four asset-mix weights) plus `DaysRemaining`.
- **`ParliamentSystem`**: `GetBillDirection` extended to sum Tax (unchanged) + Spending (percent changes
  summed directly) + Welfare (same "effective value, 0 if not implemented" trick as Tax) - SWF is
  deliberately EXCLUDED from the vote direction as a stated simplification (asset-mix/contribution-rate
  changes don't map cleanly onto a single fiscal-stance axis the way a tax rate or spending level does).
  `WouldBillPass`'s seat-weighted `FiscalStance` alignment formula itself is unchanged. `ApplyBillResult`
  now takes a delegate for the Spending/SWF portion (`SimulationManager.ApplyBudgetBillSpendingAndSwf`,
  which reuses the existing private `ApplySpendingLineChanges`/`ApplySwfPolicyChanges` via a throwaway
  `PolicyDecision`) since those two categories apply directly to `Country`/`SovereignWealthFund` rather
  than through the Tax/Welfare "line" structs. Spending/Welfare/SWF changes intentionally carry NO new
  approval-cost penalty on top of the existing `TaxHikeApprovalSensitivity`-based one, matching their
  previous zero-cost immediate-apply behavior before this phase gated them behind a vote.
- **`SimulationManager`**: `_pendingBudgetBillByCountry` (Phase 2, 21-day countdown, never pauses -
  mirrors the retired `TaxBill`'s own behavior) replaces `_pendingTaxBillByCountry`.
  `IntroduceBudgetBill` closes Phase 1 (removes the country from `_pendingBudgetProcessByCountry`, set up
  by 5a) and opens Phase 2 in the same call, so there's no gap where neither is tracking the country.
  `AdvanceBudgetBillDay` (was `AdvanceLegislativeDay`) decrements `DaysRemaining` and resolves at 0 via
  `WouldBillPass`/`ApplyBillResult`. 5a's temporary `AcknowledgeBudgetProcess` placeholder is removed -
  the real flow (Introduce) now closes Phase 1 for real, as 5a's own writeup flagged it would need to.
- **`GameController`**: `DrawBudgetBillStatusAndIntroduce` (new) adds a status label + "Introduce Budget
  Bill" button to the Budget Process tab's header - both ALWAYS rendered (stable-control-layout), the
  button `GUI.enabled`-gated on "no bill already pending AND the budget process is actually open," never
  omitted. `DrawLegislativeSupportEstimate` (new) recomputes `GetBillDirection`/`WouldBillPass` every
  `OnGUI` call from the CURRENT draft state (cheap enough not to need `RecomputePolicyPreview`'s caching)
  so the estimate updates live as any of the four categories' sliders move. `DrawWelfareProgramRow` and
  `DrawSwfPolicyContent` were rewritten to the same stable-control-layout template `DrawTaxLineRow`
  already used: Implement/Remove (or Create/Dissolve for SWF) now edit a draft dictionary/nullable-bool
  only, never apply immediately, and every slider renders unconditionally with `GUI.enabled` composed
  against the draft state - `DrawSwfPolicyContent`'s old `if (fund == null) return;` early-out (which
  omitted all six SWF sliders whenever no fund existed yet) was removed for exactly this reason, since it
  was the same hazard class as the original Tax Policy freeze investigation, just on a different tab.
  `BuildPlayerDecision`'s per-turn automatic Spending/Welfare/SWF application, and
  `PolicyInputsChangedSinceLastPreview`'s change-detection for the same three categories, were both
  removed - those categories now only ever apply through a resolved `BudgetBill`, not every turn.
- **Bug found and fixed during this phase's own live-play validation (not initially reported by
  Elias)**: `DrawCalendarAndSpeedControls`'s global pending-decision banner used an `if`/`else if` chain
  that only ever displayed ONE active pause reason - if a Foreign Policy meeting happened to be pending
  at the same time as a Budget Process pause, only the Foreign Policy message showed, even though
  `Update()`'s actual gate correctly blocked on both underneath. Fixed by collecting every currently-true
  reason into a list and joining them into one combined label, ordered Fed Chair -> Cabinet -> Budget
  Process -> Foreign Policy; still exactly one `Label` control either way, so this doesn't touch the
  stable-control-layout guarantee.
- **Validated - a throwaway Edit-mode diagnostic** (`BudgetBillDiagnostic`, same reflection-based
  technique as 5a's diagnostic): a maxed-out PASS scenario, a scenario engineered to FAIL, and the
  Phase 1 -> Phase 2 hand-off (mandatory pause blocks before Introduce, resumes immediately after,
  and never re-pauses at any point during the 21-day countdown) - ALL PASS. Caught one real bug during
  its own construction: an invalid `WelfareProgramType.UnemploymentBenefits` reference (the real enum
  values are `UBI`/`NegativeIncomeTax`/`MeansTestedWelfare`/`UniversalHealthcare`/`HousingAssistance`/
  `ChildcareSubsidies`) that an initial "compiles clean" claim had missed - caught for real only once
  Unity's own batch-mode compiler was actually run and its full output read, not just grepped for
  "error". A second throwaway diagnostic, `FiscalYearRecurrenceDiagnostic`, was written specifically to
  audit a live-play anomaly (see below) by advancing the calendar through two full fiscal years for USA
  via the same direct `AdvanceDay`/`TryOpenBudgetProcess` calls `Update()`'s own loop makes - confirmed
  the mandatory pause correctly reopens on BOTH October 1, 2026 and October 1, 2027, disproving the
  initial "has this ever fired a second time" hypothesis. Both diagnostics deleted after use, per this
  project's own established convention (never committed).
- **Confirmed via live-Editor play** (2026-07-31, by Elias directly, a genuine end-to-end sequence rather
  than "does the screen render"): the Legislative Support estimate updates live while dragging sliders in
  any of the four categories; SWF and Welfare sliders specifically dragged during ACTIVE 3x-speed time
  advancement across nearly a full in-game year, with zero freeze; a full introduce -> wait -> resolve
  cycle carried out live start to finish. This same live play is also what surfaced the anomaly that led
  to `FiscalYearRecurrenceDiagnostic` above: an SWF draft set up after the October 2026 budget cycle never
  became standing, still showing "no fund exists" by October 2027 a full fiscal year later. The diagnostic
  proved the underlying mechanism was never buggy; Elias separately confirmed the actual cause was Unity
  being closed and reopened multiple times between setting the draft and October 2027, losing the draft to
  the restart - corroborated by a direct code check confirming this project has genuinely zero save/load
  persistence anywhere (no `PlayerPrefs`/`JsonUtility`/`BinaryFormatter`/any save mechanism at all), now
  tracked as its own confirmed gap (`POLISIM_MASTER_ROADMAP.md`'s Master Sequence item 8). Worth noting
  explicitly: every one of this phase's real bugs - the banner masking issue, the lost-SWF-draft anomaly,
  and by extension the save/load gap it exposed - was found through Elias's own live testing, not through
  the diagnostic or the compile check, which is exactly the category of bug automated validation alone
  cannot catch on a screen this interactive.

**5d - standalone tier-2 (program add/remove) and tier-3 (non-budget policy) bills. DONE (2026-07-31).**
Completes the three-tier design's remaining two tiers, reusing 5c's bill-resolution mechanism rather
than inventing a new one, per the roadmap's own explicit instruction.
- **Design fork confirmed via `AskUserQuestion` before implementation** (three questions, all answered
  before any code was written): (1) tier 2 (program add/remove) rebuilt as a genuine ANYTIME standalone
  bill, REPLACING the annual-bill path 5c had briefly folded implement/remove into - `BudgetBill.TaxLines`/
  `WelfarePrograms` narrowed back from `TaxBillLine`/`WelfareBillLine` structs to plain
  `Dictionary<TaxType, float>`/`Dictionary<WelfareProgramType, float>` (rate/generosity only, meaningful
  solely for an ALREADY-implemented program - an entry for a program the country doesn't currently have
  is simply skipped, not an error), matching this section's own original tier 1/tier 2 split description
  that 5c had briefly deviated from. (2) Tier 3's non-fiscal dials score via a STATED sign-convention
  mapping onto the existing single `FiscalStance` axis, rather than either inventing a second scoring
  axis or treating every tier-3 bill as an unconditional neutral auto-pass. (3) One standalone bill per
  TAB, not per dial - four new bill types, each bundling that tab's dials together, mirroring how
  `BudgetBill` already bundled Tax+Spending+Welfare+SWF in 5c.
- **`ProgramBill.cs`** (new): `TaxProgramBill`/`WelfareProgramBill`, each just `{ Type, IsAdd,
  DaysRemaining }`. **`StandalonePolicyBills.cs`** (new): `LaborPolicyBill` (MinimumWage/
  PaidFamilyLeaveWeeks/OvertimeRegulation/RetrainingProgram/FamilyPolicy/ImmigrationPolicy),
  `CrimeJusticePolicyBill` (PoliceFunding/SentencingSeverity/BailReform/DrugPolicy/JudicialFunding/
  BorderEnforcement), `SectorPolicyBill` (five `Dictionary<SectorType, float>`s, one per dial, covering
  every sector at once), `TradePolicyBill` (`NewBaseTariffRate` - an ABSOLUTE target like `TaxLine.Rate`,
  not the small per-turn delta `PolicyDecision.TariffRateChange` itself still uses - plus
  `PartnerTariffOverrides`, deliberately EXCLUDED from the vote direction like SWF's own asset-mix terms,
  though it still applies in full on PASS).
- **`ParliamentSystem`**: `WouldBillPass` refactored - the seat-weighted-alignment core now takes a plain
  `float direction` (`WouldBillPass(Country, float)`), with `WouldBillPass(Country, BudgetBill)` kept as a
  thin convenience overload computing `GetBillDirection` first. Six new `GetXBillDirection`/
  `ApplyXBillResult` pairs share this one core: `GetTaxProgramBillDirection`/`GetWelfareProgramBillDirection`
  reuse the exact "effective value, 0 if not implemented" trick BudgetBill's own tax term used before 5d -
  implementing turns the program's already-persistent Rate/GenerosityLevel "on" (same sign as a rate hike),
  removing turns it "off" - so no new sign convention was needed for tier 2 at all.
  `GetLaborBillDirection`/`GetCrimeJusticeBillDirection`/`GetSectorBillDirection`/`GetTradeBillDirection`
  each document their own stated sign convention directly against real UI label text already in the
  codebase: Sentencing Severity ("0 = lenient, 100 = harsh"), Drug Policy ("0 = decriminalized, 100 =
  strict criminalization"), Border Enforcement ("0 = open/lenient, 100 = strict"), and
  Deregulation/Nationalization ("0 = fully nationalized, 100 = fully deregulated/private") all read
  NEGATIVE (conservative-coded) as the dial rises; Police/Judicial Funding, Bail Reform, Family
  Policy/Immigration Policy, and every Sector dial except Deregulation all read POSITIVE
  (spending-like/progressive-coded). None of the six new `ApplyXBillResult` functions charge a new
  approval-cost penalty on PASS (matching BudgetBill's own Spending/Welfare/SWF precedent - these
  categories were free before 5d gated them, so gating them doesn't newly invent a cost); FAIL still
  charges the shared `BillFailedApprovalCost` uniformly across all seven bill types now in play.
- **`SimulationManager`**: six new pending-bill collections. Tier 2 uses
  `Dictionary<CountryId, Dictionary<TaxType, TaxProgramBill>>` (and the WelfareProgramType equivalent) -
  MULTIPLE different programs can have a bill pending for the same country at once (e.g. "add UBI" and
  "remove CarbonTax" simultaneously), just never two for the SAME program concurrently. Tier 3 uses one
  `Dictionary<CountryId, XBill>` per bill type, single-slot per country per tab, mirroring
  `_pendingBudgetBillByCountry`'s own pattern. All six `AdvanceXDay`/`AdvanceXBillsDay` methods are
  non-blocking (introducible anytime, no mandatory-pause phase, matching the pre-5c `TaxBill`'s own
  countdown-only behavior) and wired into `GameController.Update`'s existing day-loop alongside
  `AdvanceBudgetBillDay`, none needing the pause-gate re-check the annual bill's own `TryOpenBudgetProcess`
  call does. Four private `ApplyXBillEffects` delegate methods (mirroring `ApplyBudgetBillSpendingAndSwf`'s
  own reuse pattern) reuse the EXISTING private `Apply*Changes` methods via a throwaway `PolicyDecision`,
  so `ParliamentSystem` never needs direct access to their clamp-bound constants; `ApplyTradeBillEffects`
  is the one exception needing a small conversion - `ApplyTariffRateChange` only understands a DELTA, so
  the absolute `NewBaseTariffRate` target is converted to `target - country.BaseTariffRate` immediately
  before calling it, landing the existing clamp exactly on the bill's requested target without duplicating
  its bounds.
- **`GameController`**: Tax/Welfare Policy tabs' Implement/Remove buttons now submit a standalone bill
  IMMEDIATELY on click (not drafted first, unlike a rate/generosity slider) - a binary toggle has no
  separate "adjust before submitting" step the way a rate does. Labor Market/Crime & Justice/Economic
  Sectors/Trade tabs each gained a `DrawXBillStatusAndIntroduce`/`DrawXLiveEstimate`/`BuildXBillFromDrafts`
  trio, mirroring `DrawBudgetBillStatusAndIntroduce`/`DrawLegislativeSupportEstimate`/
  `BuildBudgetBillFromDrafts` exactly, with every dial gaining a "Standing: X, Draft: Y" label so it's
  always clear whether an unresolved change is pending. Stable-control-layout discipline applied
  throughout, same as every gated surface since the original Tax Policy freeze investigation.
  `BuildPlayerDecision`/`PolicyInputsChangedSinceLastPreview` pruned of every dial that's now bill-gated -
  only `InterestRateChange` (the Federal Reserve/Eurozone exemption) remains, since it was never gated in
  the first place. `DrawParliamentTab`'s "Pending Legislation" section extended from showing only the
  annual `BudgetBill` to listing every bill across all three tiers at once.
- **Live-play bug found and fixed during this phase's own validation** (a pure UI gap, not caught by the
  structural diagnostic since it involves no simulation logic): the Tax/Welfare Policy tabs' Implement/
  Remove rows had no live pass/fail estimate of their own, unlike every other bill tier - a player could
  easily be reading a DIFFERENT bill's estimate (e.g. the Budget Process tab's) and be confused when the
  actual relevant bill resolved differently. This is exactly what happened live: Elias introduced a
  welfare program bill that failed, having checked what turned out to be an unrelated estimate first.
  Fixed with `DrawTaxProgramBillEstimate`/`DrawWelfareProgramBillEstimate` - a per-row live estimate
  scoring the EXACT hypothetical the Implement/Remove button above it would submit (or the actual pending
  bill's own estimate, once one exists), using the same `GetXBillDirection`/`WouldBillPass` pair every
  other tier's estimate already calls.
- **Validated - a throwaway Edit-mode diagnostic** (`StandaloneBillsDiagnostic`, calling `SimulationManager`/
  `ParliamentSystem`'s public API directly - no reflection needed, unlike 5a/5c's diagnostics, since every
  method involved here is already public): PASS and FAIL scenarios for `TaxProgramBill`/`WelfareProgramBill`
  (add and remove), concurrent bills for different `TaxType`s coexisting correctly, `LaborPolicyBill`'s
  all-expansionary sign convention, `CrimeJusticePolicyBill`'s MIXED sign convention (Sentencing Severity
  negative, Police Funding positive, verified independently), `SectorPolicyBill`'s Deregulation sign flip,
  `TradePolicyBill`'s partner-override vote exclusion (still applies on PASS despite not swaying the vote),
  a `BudgetBill` regression check (an already-implemented TaxType's rate entry still applies; a
  not-implemented one is skipped, not an error), and all six new `AdvanceXDay` methods confirmed as safe
  no-ops with nothing pending - 21/21 PASS. The diagnostic's own FIRST run caught a real bug in the
  DIAGNOSTIC ITSELF, not the shipped code: a FAIL scenario was silently hitting the neutral-auto-pass
  branch instead of genuinely exercising seat-weighted scoring, because the target `TaxLine.Rate` was
  `0f` (a not-yet-implemented tax can legitimately have never had a rate assigned) - direction ends up
  exactly `0` regardless of party alignment when that happens, an easy trap for any future test using this
  exact "toggle a not-yet-implemented program" pattern. Fixed by forcing a genuinely non-zero `Rate`
  before scoring; all 21 assertions passed cleanly afterward. Deleted after use, per this project's own
  established convention (never committed).
- **Confirmed via live-Editor play** (2026-07-31, by Elias directly): freeze-free dragging at active
  3x speed across multiple new tabs; live estimates confirmed updating correctly while dragging; a full
  welfare bill introduce-and-resolve cycle carried out live (this is what surfaced the missing-estimate
  bug above). A follow-up question from that same session - "welfare bills keep failing and tax bills
  keep passing, is that a bug?" - led to a live investigation that resolved with NO code change needed:
  Elias's Parliament tab showed Progressive Alliance and Conservative Union tied at EXACTLY 32% each
  (their identical `PartyArchetypeData.ProgressiveBaseSupportShare`/`ConservativeBaseSupportShare`,
  meaning `ApprovalRating` was sitting right around 50, so neither party had picked up any
  approval-driven bonus yet). At an exact tie, their opposite-sign `FiscalStance` (+0.7 vs -0.7) cancels
  perfectly; `CentristCoalition`'s neutral stance (0.0) contributes nothing either way; that leaves
  `NationalistFront`'s smaller (12%) but purely negative-leaning (-0.3) seat share as the ONLY thing
  actually deciding the net seat-weighted alignment, tipping it slightly negative. A negative net
  alignment makes every contractionary bill (removing an already-implemented tax) score positive and
  PASS, and every expansionary bill (implementing a new welfare program) score negative and FAIL,
  regardless of which specific tax or program it is - the outcome's sign is fixed by current Parliament
  composition, not by the bill's own content. Confirmed by hand-computing the exact weighted sum from
  Elias's own reported percentages (0.32*0.7 + 0.24*0.0 + 0.12*(-0.3) + 0.32*(-0.7) = -0.036) before
  reporting back that this is real, working-as-designed behavior, not a scoring bug. Worth keeping on
  record: it's a genuine, non-obvious emergent property of the archetype design (see
  `PartyArchetypeData`'s own doc comment on why Progressive/Conservative are built as mirror-image
  establishment parties) that will keep surfacing as "why did my bill fail" questions in ordinary play,
  not just this once.

## Baseline validation after the 5e UI work (2026-08-01)

**Run**: 100-turn `baseline` via `BatchSimulationRunner` against real Unity `6000.5.6f1`, executed by
Elias after Master Sequence step 5e's UI batches, the Tax/Spending tab merge, the chrome wiring and the
Budget Process layout fix. **Result: 96 anomalies. Verdict: consistent with documented baseline
behaviour, no regression.** Recorded because it is the first full baseline since that body of work, and
because a naive reading of the log looks alarming.

**Why 96 is unremarkable**: it sits inside this project's own documented range for a 100-turn run - 28,
34, 53, 56, 60, 74, 80, 86, 104, 106, 178 and 215 all appear in earlier validations above. Every entry
is the known "swung X% in one turn" family across exactly four stats (`DebtToGdpRatio`, `Inflation`,
`Unemployment`, `InterestRate`); **no new anomaly type**, no `exceeded`/`invalid`/NaN, no unbounded
growth.

**Sweden and France repeatedly reaching `DebtToGdpRatio` 0.00 is documented and intentional**, not new.
It is the `Mathf.Clamp(state.GovernmentDebt - budgetBalance, 0f, maxDebt)` floor, already investigated
twice - see "SpendingLine Amount Ceiling - Debt-to-Zero Fix" and "Fiscal Reaction Function" above, the
latter of which explicitly records Sweden landing on the zero end of this spectrum as *"consistent with
prior findings, not a new phenomenon"* and confirms France *"hits the identical floor via the identical
mechanism"*. France only develops it from ~turn 75 here, which matches its far higher starting ratio.

**The extreme percentages are a metric artifact, not instability** - worth writing down, because they
are the most alarming-looking thing in the log and will recur in every future baseline. Every outsized
figure has a tiny PRIOR value: 565.8% is `0.87 -> 5.80`, 1616.0% is `1.48 -> 25.47`, 1329.3% is
`1.22 -> 17.51`, 419.4% is `4.24 -> 22.04`. Once debt sits near the floor, relative change explodes
arithmetically - a 24-point move reported as 1616% purely because the denominator is near zero.
`SimulationTestRunner` measures relative change with no absolute-magnitude floor.

**What this run actually established**: every change since the last validated simulation state is
UI-only (`GameController`, `UiPalette`, `IconLibrary`), and the single simulation-layer change in that
window - extracting `ParliamentSystem.GetSeatWeightedAlignment` so `WouldBillPass` calls it, validated
separately at 100-turn `parliamentstress` with zero new anomaly types - is a pure no-op extraction. So
this is positive evidence that the 5e UI work did not disturb the simulation.

**Two follow-ups identified, neither a defect, neither actioned:**
1. **The anomaly detector is noisy by construction near zero.** An absolute-magnitude floor (ignore
   swings where both values are below ~2.0) would collapse most of these 96 into silence and make a
   genuine regression visible instead of buried in clamp noise. Small change to `SimulationTestRunner`;
   deliberately NOT made here, since changing the detector in the same breath as reading its output
   would destroy the comparability with every anomaly count recorded above.
2. **Debt oscillating 0 -> 25% -> 0 is arguably poor simulation**, however intentional the clamp. That
   is a design question for a future item, not a bug - flagged so it isn't rediscovered as one.

## Conventions
- Keep simulation state and logic free of Unity-specific dependencies (`MonoBehaviour`, `GameObject`, etc.) so it can be reasoned about and tested as plain C#.
- Favor small, explicit, named methods for each macro/feedback/trade/currency rule over one large monolithic update function, so individual rules — and individual pieces of economic theory — can be tuned or replaced independently.
- Cross-references between countries (trade partners, bloc membership) go through the `CountryId` enum, never direct object references — avoids reference cycles and keeps the data model Unity-Inspector-serializable. Shared-currency membership is instead detected by *reference equality* on `CurrencyZone` (see `CurrencySystem.SharesCurrencyZoneWithOthers`), since that shared reference is exactly what "using the same currency" means in this model.
- No comments explaining *what* code does; only note *why* when a rule's tuning or a non-obvious interaction needs explanation.
- Validate simulation-affecting changes against real Unity via `BatchSimulationRunner`, not the standalone harness alone - see "Real-Unity Validation is the Standard Path" above.

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
tariff-policy lever (`PolicyDecision`), and a small hardcoded random-event pool (`EventSystem`, grown
from 8 to 24 real-world-grounded events - see "Expanded Event Pool" above) that fires occasionally
with a one-time GDP/inflation/approval shock. All of it was validated
in the same standalone harness used for the fiscal layer before being ported - baseline (no policy)
and stress runs (sustained category spending + tariff changes; separately, implementing/removing
taxes and adjusting several rates at once) all stayed bounded over 100 turns with no
NaN/negative/out-of-range values; the player's country won every simulated election in the fiscal-
layer stress run but lost two toward the end of the tax-portfolio one (turns 84/96, after ~80 turns of
sustained heavy spending plus repeated tax hikes) - a sensible emergent outcome of that policy
combination, not an instability bug. USA also now has an independent Federal Reserve (see "Federal
Reserve" above): a player-picked fictional `FedChair` (Hawkish/Moderate/Dovish, chosen every
election cycle) drives USA's interest rate via `TaylorRule.GetSuggestedInterestRate` plus that
chair's `RateBias`, bypassing the direct interest-rate slider entirely - Sweden, Poland, and the
Eurozone trio are unaffected. Validated in the same harness across all three philosophies for a full
100-turn run each: numerically bounded throughout, and Hawkish measurably runs tighter/lower
inflation than Moderate/Dovish, though Moderate and Dovish currently land on the same
`MinInterestRate` floor given USA's deeply-negative output gap. A follow-up investigation ("Discretionary
Spending Growth" above) found the true root cause - USA's Discretionary `SpendingLine`s had stopped
scaling with GDP entirely when they replaced the old GDP-proportional `GovernmentSpendingRate`
mechanic - and fixed it by growing Discretionary spending at `PotentialGrowthRate` each turn, turning
the previously-*diverging* output gap (toward -18% to -20%) into a *stable* one around -13% to -15%.
Debt-to-GDP settles around 150% under this fix (up from 0%, but safely clear of the 300% ceiling).
The gap is still deep enough that Moderate and Dovish continue to land on the same floor - full
three-way Fed chair differentiation remains a known, unresolved limitation (see "Federal Reserve"
above for the full explanation and why closing it further wasn't pursued here). **A second, similarly
unresolved limitation, alongside this one**: Sweden's and France's no-policy-baseline debt trajectory
(see "Country Selection" above) settles permanently at the 0% debt floor rather than a moderate,
country-appropriate equilibrium the way Germany's (~35%) and Italy's (~108%) do, driven by each
country's Sovereign Wealth Fund returns compounding against the fiscal reaction function's saturation
zone (`fiscalReactionMultiplier` floored at 0.5) faster than that negative feedback can offset it -
confirmed not a crash or genuine anomaly (real-Unity matrix validation: zero finite/negative/
out-of-range anomalies for either country), but a real gap against the "six distinct, realistic fiscal
personalities" goal "Fiscal Reaction Function" otherwise achieved. **Update**: the return model itself
was subsequently rebalanced against real Norway GPFG data and given genuine downside volatility (see
"Sovereign Wealth Fund Return-Model Rebalance" above) - real down years now produce genuine, sustained
multi-decade excursions away from the floor (a real improvement to the PATH), but a 500-turn no-policy
baseline still lands both countries back at exactly 0% by the end, confirming the root cause is fund
SIZE against its own pre-existing 300%-of-GDP cap, not return-model smoothness - a returns-only fix
cannot close this gap alone. Still worth a future look, not urgent - the same two directions, neither
pursued yet, now confirmed necessary rather than just plausible: giving the Sovereign Wealth Fund a
countercyclical drawdown lever (there is currently no mechanic of any kind for a fund to shrink -
`ContributionRatePercent` only ever adds to it, so a fund's returns compound unchecked regardless of
the domestic business cycle), or giving the debt floor itself a small amount of slack instead of a
hard clamp at exactly 0%. **Resolved (Round 3 item 1)**: the first of those two directions shipped -
see "Sovereign Wealth Fund Drawdown Mechanic" above - and a dedicated diagnostic confirmed it
genuinely closes the gap, not just slows it: a sustained -3%/turn withdrawal drives both funds to
exactly 0 `SwfAssets` within the first ~20 turns and keeps them there, after which Sweden and France
each settle into their own genuine, stable, non-zero `DebtToGdpRatio` equilibrium (~8.2-8.5% and
~89.6-96% respectively) instead of the floor - this limitation is no longer open, though it required
the player (or a scripted policy) to actually use the new lever; the *passive*, no-withdrawal baseline
still exhibits the original pinning exactly as before, which is expected and by design (a player-
chosen lever that nobody pulls has no effect, the same as `ContributionRatePercent` itself defaulting
to a small positive value rather than acting on its own). USA's seeded
`PotentialGDP` was subsequently recalibrated (see "Turn-1 GDP Consistency") so this -13% to -15%
equilibrium gap is already in effect from turn 1, instead of opening near 0% and sliding into it over
the first ~25 turns - closing off a real, one-time ~9% GDP contraction the very first turn of any new
game previously produced. Every `SpendingLine`'s `Amount` (Mandatory and Discretionary) is now also
hard-clamped to [0.2x, 3.0x] of its own fixed starting value (see "SpendingLine Amount Ceiling"),
closing off the sustained-percentage-compounding runaway divergence "Percentage-Based Spending
Sliders" had found and flagged but not yet fixed - with the honestly-disclosed side effect that the
same clamp also caps the passive GDP-proportional Discretionary growth above once a line reaches 3x
its start (around turn 56 at USA's growth rate). **That widening was subsequently found to be far more
consequential than first described** - it produced an ever-widening primary surplus that paid USA's
debt to exactly 0 and flatlined it there; see "SpendingLine Amount Ceiling - Debt-to-Zero Fix" for the
fix (a Discretionary line's `SeedAmount` now grows alongside its `Amount`) and for the second driver
found alongside it (Mandatory spending had no automatic growth mechanism at all, and naively giving it
one overshot to the opposite extreme, ~294% debt-to-GDP). **Both are now resolved** by "Fiscal Reaction
Function": every country's government now automatically tightens (raises effective revenue) as its own
`DebtToGdpRatio` rises above a country-specific comfort anchor and loosens as it falls below - the
missing negative feedback the debt-clamping system lacked entirely - shipped alongside giving Mandatory
spending the same automatic growth Discretionary already had (safe now that the reaction function
counterbalances it). All six countries settle at distinct, moderate, country-appropriate debt-to-GDP
levels under a no-policy baseline (confirmed flat from turn 100 through turn 500 in a direct real-Unity
run, not just the harness), rather than the previous two-extremes-only outcome - a deliberately extreme
player policy (heavy sustained tax hikes, or holding spending sliders at max every turn) can still drive
debt to 0% or the 300% ceiling, which is intended; only the passive/no-policy baseline was bimodal.
Every country can also now set a player-specific tariff override on any
one trade partner (see "Per-Partner Tariff Overrides"), extending `TradeSystem.GetTariffRate`'s
existing precedence chain rather than adding a parallel mechanic - a set override beats even trade-
bloc membership for that one relationship, persists turn to turn like a `TaxLine.Rate` does, and only
ever affects the owning country's own tariff on its own imports from that partner, never the reverse -
its own general Tariff Rate Change slider was also relocated from the left-column policy panel into
this same Trade tab, alongside the per-partner controls. This session also found (later than it should
have) that a working Unity Editor install (`6000.5.4f1`) IS reachable in this environment at
`G:\UNITY\Unity Hub\6000.5.4f1\Editor\Unity.exe` - every earlier "no Unity Editor reachable" statement
in this file reflected an incomplete search, not a genuine absence; see "SpendingLine Amount Ceiling -
Debt-to-Zero Fix" for the correction and for `Assets/Editor/BatchSimulationRunner.cs`, a new
Editor-only script that lets `SimulationTestRunner` run headlessly from the command line.

A first playable loop exists: `GameController` (`Assets/Scripts/UI/`), an unstyled immediate-mode
(`OnGUI`) dashboard/policy panel for the player's country (USA, hardcoded) — shows its
`EconomyState` including `ApprovalRating`, takes this turn's `PolicyDecision` via a tariff slider
(and, for a country without an independent Fed chair, an interest rate slider) on the left, a
Federal Reserve panel showing USA's current chair and (on an election-cycle turn) candidate buttons
that must be resolved before Advance Turn is enabled, a dedicated Tax Policy tab for
implementing/removing/adjusting individual taxes, a dedicated Spending Policy tab (see "Detailed
Spending Portfolio" and "Percentage-Based Spending Sliders" above) for USA's detailed Mandatory and
Discretionary spending lines - each a percentage-of-current-amount slider, Mandatory at a narrower
range and higher approval cost - plus a read-only Interest on Debt line, a Trade tab (see
"Per-Partner Tariff Overrides" above) where each trade partner can have its own tariff override
enabled/adjusted/reset independently of the country's general `BaseTariffRate`, and a Welfare Policy
tab (see "Welfare Policy" above) implementing/removing/adjusting each of the six welfare programs the
same way tax lines work, displays the current turn's event (if any) as
a "BREAKING: ..." banner above the dashboard and a game-over banner if the player lost re-election,
and advances the turn on a button press, with every other country getting `PolicyDecision.None()`. A
live preview (see "Live Policy Preview" above) shows the sliders' (including tax-line, spending-line,
per-partner tariff-override, and welfare-generosity sliders, and USA's Fed-chair-driven rate) estimated single-turn effect, with a
cosmetic margin of error, before the player commits by pressing Advance Turn. Every country now also
tracks `PovertyRate` (seeded from real OECD data, shown on the dashboard - see "Welfare Policy" above),
mean-reverting toward a baseline driven by the same unemployment/inflation gaps that already drive
approval, adjustable via any of the six implementable welfare programs. Every country also now tracks
`LaborForceParticipationRate` (real World Bank/OECD data, mean-reverting toward its own baseline via
the same unemployment gap - see "Labor Market Basics" above), and four of the six (USA, Germany,
France, Poland) have a player-adjustable minimum-wage lever (percent of median wage) with small
effects on `Unemployment`/`PovertyRate` - Sweden and Italy have none, matching real-world fact. Every
country also tracks a stylized `CrimeIndex` (informed by real relative homicide-rate rankings, not a
literal transformation of any single indicator - see "Crime & Justice Basics" above) and has two
policy dials - Police Funding and Sentencing Severity - with small effects on `ApprovalRating` and
`BusinessConfidence`. Every country also tracks a real, per-100,000 `PrisonPopulationRate` (see
"Deeper Crime & Justice" above) and has two more dials - Bail Reform and Drug Policy - affecting it
directly, plus a small, honestly-disclosed-as-contested `CrimeIndex` effect from Bail Reform. Every
country also tracks a `ConditionIndex` for four infrastructure types - Roads, Rail, PowerGrid,
Broadband (see "Infrastructure System" above) - a decay/investment stock hard-clamped to [0, 100],
driven entirely by the existing Infrastructure spending category's already-established
`PotentialGrowthRate` signal rather than a new player-facing lever, and likewise deliberately isolated
from the core GDP/Unemployment/Approval loop (see `ROADMAP_BRIEF.md`'s Open Questions #2). Every
country also has four economic sectors (Manufacturing, Technology,
Agriculture, Finance - see "Economic Sectors" above) tracking Output/Employment/one sector-specific
metric each, adjustable via Subsidy/Regulation dials - deliberately isolated from the core GDP/
Unemployment/Approval loop in this proof-of-pattern pass (see `ROADMAP_BRIEF.md`'s Open Questions).
USA can also create a Sovereign Wealth Fund (see "Sovereign Wealth Fund" above) - a real, budget-
integrated mechanic (contributions are an expense, market returns are income) with its own simple
per-asset-class return model, hard-capped at 300% of GDP after a validation pass found and fixed a
genuine unbounded-compounding-growth risk under sustained maximum-aggression play. Sweden and France
now start the game WITH an active fund (see "Sovereign Wealth Fund Expansion to All Six Countries"
above), seeded from their own real partial analogs (Sweden's AP pension buffer funds, France's FRR) -
USA/Germany/Italy/Poland honestly stay without one, matching real-world fact. No save/load, no
full market simulation (trade volumes are static inputs, not supply/demand-driven), and every
constant is a starting-point placeholder meant to be tuned by playtesting.

**`ROADMAP_BRIEF.md`'s full 5-item queue is now complete** (2026-07-22): expanded event system,
labor market basics, crime & justice basics, a small slice of economic sectors, and the Sovereign
Wealth Fund - each implemented, grounded in real data where available (honestly labeled where not),
validated via the standalone harness first and then `BatchSimulationRunner` against real Unity at
100 and 500 turns, and committed as its own commit with validation results in CLAUDE.md. One design
decision was escalated rather than resolved silently (`ROADMAP_BRIEF.md`'s Open Questions #1 - whether
Economic Sectors should feed back into aggregate GDP/Unemployment). See `ROADMAP_BRIEF.md` for the
full queue history and that escalated question.

**`ROADMAP_BRIEF.md`, `CONTINUOUS_TIME_MIGRATION.md`, and `POLITICAL_SYSTEMS_OVERHAUL.md` are now
superseded by `POLISIM_MASTER_ROADMAP.md`** (2026-07-30), which coordinates all three into one
authoritative Master Sequence (see that file's own "why the sequence is different from each source
document's own plan" reasoning - Parliament depends on Continuous Time's Phase 0 calendar mechanic,
and the daily-granularity conversion is deliberately kept out of the same window as the Parliament
rollout). Master Sequence step 1 (Cabinet, Political Systems Overhaul Part A) is done - see "Cabinet
(Political Systems Overhaul Part A)" above - the player's country can now appoint ministers to 3 of
the 6 confirmed portfolios (Finance/Treasury, Interior/Justice, Health & Social Affairs; the other
three deliberately deferred per the Master Roadmap's own content-authoring warning), each with a
passive competence effect on an existing channel and an interactive decision layer. Master Sequence
step 2 (UI/graph restyling and political visualization, Political Systems Overhaul Part C) is also
done - see "UI/Graph Restyling and Political Visualization" above - graphs can now show a threshold/
target reference line and page back through up to 250 turns of retained history (`StatHistory.
MaxEntries` raised from 50), and a new "Compass & Demographics" tab plots all six countries on a
political compass (auto-scaled axes, grounded entirely in real tracked policy data) plus five
demographic pie charts. Master Sequence step 3 (Continuous Time Migration Phase 0) is also done - see
"Continuous Time Migration Phase 0 (Master Sequence step 3)" above - a real in-game calendar
(`SimulationManager.CurrentDate`, advancing daily) with Pause/1x/2x/3x speed controls now drives the
EXISTING, unchanged 121-day turn cadence automatically instead of a manual "Advance Turn" button; the
live Policy Preview shows a selectable-horizon (1 Day/1 Week/1 Month/Full Turn) display-only rescaling
of the same full-turn estimate; `StatHistory` gained multi-resolution (Daily/Weekly/Monthly/Quarterly)
storage, though Phase 0 itself can't yet make the four resolutions diverge; and a small Foreign Policy
Meetings interrupt slice proves the "decisions that land between turn boundaries" pattern Political
Systems Overhaul Part B will eventually need for law-passing specifically (deliberately not built
here, since Part B's design is authoritative for that). Master Sequence step 4 (Political Systems
Overhaul Part B, Parliament PILOT on the Tax Policy tab) is also done - see "Parliament PILOT
(Political Systems Overhaul Part B), Master Sequence step 4" above - four generic fictional party
archetypes with seats derived from ApprovalRating (bounded inertia plus jitter, a stated proposal for
the roadmap's own Open Question) now gate Tax Policy specifically: sliders are draft-only, an
"Introduce Bill" action submits an omnibus TaxBill that takes 21 real in-game days to resolve
pass/fail against seat-weighted party alignment (also a stated proposal), a passed bill becomes the
new standing rates, a failed one leaves them untouched and costs a modest approval hit. The Federal
Reserve/Eurozone exemption needed no new code, since the interest-rate lever was never one of the
eight tabs Parliament gates. Validated via the full 30-combination real-Unity matrix (zero hard
anomalies, zero USA-specific anomalies from the new worst-case parliamentstress scenario) plus a
screenshot smoke test confirming the pass/fail math visibly working correctly against a real seat
split. Master Sequence step 5's original plan (a uniform per-tab repeat of the pilot across the
remaining seven tabs) is SUPERSEDED (2026-07-31) by a revised three-tier bill design (an Annual Budget
omnibus bill per country on its real fiscal-year date, plus a standalone-bill mechanism reused for both
new/removed programs and non-budget policy) built in six sub-phases - see "Master Sequence step
5a/5b/5c/5d (Political Systems Overhaul Part B, full rollout)" above for the design pointer and what's
actually been built. 5a (real per-country fiscal-year dates + the mandatory pause hook), 5b (the Budget
Process full-screen UI shell, consolidating Tax/Spending/Welfare/Infrastructure/SWF onto one
three-column screen), 5c (the omnibus BudgetBill, replacing TaxBill, plus the live vote estimate), and
5d (standalone tier-2 program add/remove bills plus four standalone tier-3 non-budget policy bills, one
per tab) are all DONE (2026-07-31). Each was confirmed via both a structural diagnostic and Elias's own
live-Editor testing, not either alone - 5b needed two real layout bugs fixed in place (a header label
clipping instead of wrapping, and the reused Live Policy Preview panel rendering catastrophically
narrow) before it passed; 5c's own live play caught two further real bugs a diagnostic alone never
would have: a global pause banner that silently masked a Budget Process pause behind a simultaneous
Foreign Policy pause (fixed), and a lost SWF draft that turned out to be a genuine, previously-unknown
project gap - this codebase has zero save/load persistence anywhere, so any Unity restart silently
discards all game state, now tracked as its own item (Master Sequence item 8) in the Roadmap; 5d's own
live play found a missing live pass/fail estimate on the Tax/Welfare Implement/Remove rows (fixed) and,
via a follow-up question from Elias, confirmed a real (not buggy) emergent property of the seat-weighted
vote math - Progressive Alliance and Conservative Union sitting exactly tied cancels their fiscal pull,
leaving Nationalist Front's smaller but purely negative lean to decide close votes. 5e (tab/IA
consolidation into 7 tabs) is next. Round 4 of the original Roadmap stays unscoped until all of step 5
is done, so new features get designed against the
gated-legislation model from day one rather than retrofitted onto it later.

## Simulation determinism, and two batch-run gotchas (2026-08-01)

**The simulation is now deterministic under a seed, and that is a prerequisite for Master Sequence item
9's Step A, not a convenience.** Step A's bar is "changes ZERO simulation numbers, proven by identical
trajectories before and after". That was unfalsifiable before this work: six systems each held their own
`new System.Random()`, clock-seeded, so two runs of IDENTICAL code differed (96 vs 97 anomalies on
consecutive 100-turn baselines). Any before/after comparison would have been noise, and a published-value
leak is exactly the small, slow divergence noise hides.

All six now draw from `SimulationRandom` (commit `75fa05a`), seedable in one call, with
`SimulationTestRunner` accepting `-seed=N` alongside `-turns` and `-scenario`. Unseeded remains the
default, so real play still varies between playthroughs.

**Proven, not assumed**: two independent 100-turn baseline runs at `-seed=12345` produced **all 85
per-turn lines identical**, compared as VALUES via `diff`, not as anomaly counts. The count alone is
insufficient - two runs can share a count while differing in values, which is the exact false pass this
check exists to rule out.

**The sixth source was nearly missed.** `SovereignWealthFundSystem` did not match the first audit's search
pattern and was only caught by re-running the search after rewiring the other five. Left unseeded, one
system would have kept re-randomising every run while everything appeared to work - defeating the proof
silently. Re-check after a mechanical change; the first pass missed it.

### Two operational facts about batch runs, both of which cost real time today

1. **Seeded runs cannot be chained back-to-back.** After `SimulationTestRunner` finishes, the Unity
   process lingers in the documented post-simulation exit hang and KEEPS THE PROJECT LOCK. The next run
   is then refused and dies with `Exiting without the bug reporter. Application will terminate with
   return code 1` after a ~24-line log with no script compilation in it. Any multi-run validation must
   clear the previous process between runs, not loop.
2. **Exit codes from these runs are meaningless - read the log.** A total failure to launch
   (`Unity.dll failed to load`) reported **exit code 0** twice, because the shell pipeline succeeded even
   though Unity produced nothing at all. Only `Sanity check complete` in the log proves a run happened.
   Do not pipe Unity's own output to /dev/null: doing so hid that load failure through two wasted
   attempts.

**Editor install fragility.** Two Editor installs (6000.5.4f1, then 6000.5.6f1) broke with
`Unity.dll failed to load`, each shortly after a Unity Hub self-update. The Hub application and the Editor
share one directory tree (`G:\UNITY\Unity Hub\`, with `6000.5.6f1/` inside it alongside the Hub's own
`ffmpeg.dll`/`dxcompiler.dll`), which is the likely mechanism. Reinstalling fixes it. Note that
`Unity.dll`'s timestamp does NOT indicate whether a reinstall happened - installers preserve build-time
timestamps, so an identical date is what a clean reinstall of the same version looks like. Test by
launching, not by inspecting file metadata.

## Shared RNG structure can invalidate a validation method (2026-08-01)

**The finding, which is about validation methodology rather than about randomness.** Step A0 made the
simulation reproducible by moving all six random consumers - Cabinet, Event, FederalReserve,
ForeignPolicy, Parliament, SovereignWealthFund - from their own private `System.Random` instances onto a
single shared stream. Reproducibility was achieved. Isolation was silently destroyed.

With one shared stream, every consumer draws from the same sequence in call order. Adding a single new
draw ANYWHERE shifts every subsequent draw for every other consumer. Step A was about to add exactly
that: noise on preliminary published figures. Events would have fired on different days, SWF returns
would have differed, Fed chair candidate sets would have changed, parliament seat jitter would have
moved - with no bug anywhere in the new code.

**Why that mattered far more than "the numbers would change".** Step A's entire acceptance bar is an
identical-trajectory proof: run the seeded scenario before and after, and any difference means published
values leaked into the simulation. RNG-sequence contamination would have produced a diff that is
**indistinguishable from that leak** - same symptom, completely different cause. The proof would have
fired correctly and pointed at the wrong thing, and the obvious response (hunt for the leak) would have
found nothing, because there was none. A validation method had been quietly robbed of its meaning by a
change made two commits earlier for an unrelated reason.

**Resolved by per-stream seeding** (commit `121656f`): each consumer gets its own `System.Random` seeded
from `masterSeed + streamOffset`. Same master seed reproduces every stream exactly, and adding, removing
or reordering draws in one stream cannot perturb another. This is the isolation the ORIGINAL per-system
instances had, now made reproducible instead of clock-dependent - strictly better than either the
original design or A0's first attempt. `SimulationRandom.Stream` is append-only: its integer values are
baked into each stream's seed, so renumbering silently changes every seeded run and invalidates any
baseline captured beforehand.

**Credit where due**: Elias caught this before it did damage, from the direction of "the revision noise
draws from the shared generator". The premise cited - that SovereignWealthFundSystem and
FederalReserveSystem still had isolated instances to follow as precedent - was no longer true, since A0
had already replaced them. The underlying concern was correct and the situation was worse than described:
the coupling already applied to all six systems, not merely to a hypothetical new consumer.

### Standing note, alongside the six failure patterns

**Any change to shared RNG structure can invalidate a validation method's ability to mean what it
claims.** More generally: a proof is only as trustworthy as the assumptions underneath it, and those
assumptions are usually implicit and rarely re-checked when unrelated code changes. Before relying on a
validation result, ask what the method assumes and whether anything has changed underneath it. Two
instances of this already exist in this project's history - the clock-seeded RNG that made
identical-trajectory comparison unfalsifiable in the first place, and this shared-stream coupling that
would have made it fire for the wrong reason.

## Verification-integrity failures (2026-08-01)

**A recurring class worth naming, because three instances occurred in rapid succession during Step A and
none of them announced itself.** In each case the thing being CHECKED was fine; the CHECKING MECHANISM
was compromised. That is far more dangerous than an ordinary bug, because the output still looks like a
result - just a wrong one - and the natural response is to act on it.

1. **Shared RNG coupling silently disarmed a proof.** Step A0 moved all six random consumers onto one
   stream to make runs reproducible. That coupled them: any new draw added anywhere would shift every
   other consumer's sequence. Step A was about to add exactly that (noise on preliminary published
   figures). The resulting trajectory diff would have been **indistinguishable from the published-values
   leak the identical-trajectory proof exists to detect** - the alarm would have fired correctly and
   pointed at the wrong cause, and hunting for the leak would have found nothing, because there was none.
   Fixed by per-stream seeding; see "Shared RNG structure can invalidate a validation method" above.

2. **A `cleanup && capture` chain reported success while skipping the capture entirely.** The `rm` step
   hit a file lock held by a lingering post-simulation Unity process, so `&&` short-circuited and the
   Unity run never happened - while the task still reported exit code 0. Nothing in the output said
   "no capture occurred"; it had to be inferred from an unexpected timestamp on a file that should have
   been deleted. **Use `;` rather than `&&` between a cleanup step and the work it precedes**, and verify
   the artifact exists afterwards rather than trusting an exit code.

3. **An audit script fabricated a finding.** A one-liner checking csproj coverage used bad path escaping
   and reported all 62 scripts as missing from a csproj that was demonstrably compiling them. It was
   caught only because the claim contradicted something already known to be true - the build had just
   succeeded. A less obviously absurd false positive would have been acted on.

### Standing tooling gap: the local build does not see unregistered files

`dotnet build Assembly-CSharp.csproj` compiles ONLY the files listed in that csproj. A newly created
script that has not been added to it will be skipped entirely, so the build can pass while that file
references members that do not exist - which is exactly what happened when `PublicationSystem.cs`
referenced `Country.Published` before that field existed. **Unity compiles everything under `Assets/`**
and refuses to run with `Scripts have compiler errors.`, so Unity catches it; the local build
structurally cannot. Register a new file in the csproj BEFORE treating any local compile-check as
evidence about it. Note also that Unity regenerates this csproj on import, which can drop manual entries.

### The general rule

**When a diagnostic produces a surprising result, check it against something already known to be true
before acting on it.** All three failures above were caught that way - by noticing the result
contradicted an established fact - and not by any tool failing loudly. A surprising diagnostic is
evidence about the diagnostic at least as much as about the system under test.

## Unity batch-run process policy (2026-08-01)

**STANDING AUTHORIZATION.** Elias grants blanket, ongoing permission to force-kill `Unity.exe` and
`UnityPackageManager.exe` processes for this project at any time, without asking first. This replaces the
previous ask-every-time rule, which cost significant time across 5+ occurrences of the post-run hang.
**The verification requirement stays**: always confirm with `Get-Process` afterwards that nothing remains.
Only the permission request is removed, not the check.

**CLEAR BEFORE, NOT AFTER.** A process-clear is now the mandatory FIRST step of every Unity invocation,
not a reaction to a lock failure. The hang holds both the project lock (next run dies with
`return code 1` after a ~24-line log) and file locks (`rm` fails with `Device or resource busy`), so
clearing beforehand removes an entire class of wasted runs.

**Never chain the clear to the run with `&&`** - see the verification-integrity note above, where exactly
that pattern reported success while silently skipping the capture. Use `;`, and verify the artifact
exists afterwards rather than trusting an exit code.

Standard invocation:
```
1. Get-Process Unity,UnityPackageManager | Stop-Process -Force      (no permission needed)
2. Get-Process ...                                                  (verify clear)
3. Unity.exe -batchmode -nographics ... -logFile <path>              (separate command, not &&)
4. Confirm "Sanity check complete" IN THE LOG - exit codes have been wrong 3+ times
```

### Root-cause investigation of the hang - findings, not attribution

Every prior occurrence was attributed to Unity's startup Search/asset indexing. **The log evidence says
otherwise**, and the real cause looks self-reinforcing:

- `BatchSimulationRunner: exiting after wait.` - the message logged immediately before
  `EditorApplication.Exit(0)` - **never appears in a hung run's log**.
- Yet `Shut down.` IS the final line. So Unity reached shutdown; the PROCESS then failed to terminate.
- The log also shows `Unexpected transport error from import worker 0 (possible crash). code=10054`,
  with the worker left `WorkerState(Connected)`.
- `[Indexing] Starting Initial Indexing for Assets` does appear, but indexing completes. The hang is
  after `Shut down.`, not during indexing.

**Hypothesis: a crashed/orphaned asset import worker prevents process termination.** This fits the
otherwise-puzzling observation that the hang is MORE common after a force-kill: killing leaves the asset
database and import-worker state inconsistent, the next run crashes a worker, the process won't exit, and
it gets force-killed again. The workaround feeds the problem.

**Untested fix to try first**: delete `Library/` once and let Unity rebuild it cleanly, with no
force-kill during the rebuild - the only step that breaks the cycle rather than continuing it.

**Needs external lookup (no web search available in-session):** the Unity 6000.5 batch-mode flag
controlling asset import workers (something like `-importWorkerCount`, exact name and whether 0 is legal
unconfirmed); whether `EditorApplication.Exit()` is documented to block while import workers are active,
and the recommended batch-mode shutdown sequence; and whether Search indexing can be disabled per-project
via a settings asset rather than a CLI flag.

## The batch-run hang: actual root cause found and FIXED (2026-08-01)

**Supersedes every earlier explanation in this file.** The recurring "Unity batch run never exits" problem
was never Search indexing, and never the crashed asset import worker. **It was a bug in
`BatchSimulationRunner` itself**, present since the tool was written.

**The bug.** `Run()` subscribed `WaitThenExit` to `EditorApplication.update` and then set
`EditorApplication.isPlaying = true`. Entering Play mode triggers a DOMAIN RELOAD, and a domain reload
wipes all static state and unsubscribes every delegate. So the callback was destroyed by the very line
that followed it, `_framesWaited` reset to 0, and nothing ever called `EditorApplication.Exit(0)`. Unity
ran the simulation, logged its summary, and then sat in Play mode forever.

**The diagnostic that identifies it**: `"BatchSimulationRunner: exiting after wait."` - logged
immediately before the Exit call - **never appeared in ANY run's log**. That negative observation held
across every occurrence, while indexing messages and worker crashes came and went. A symptom that is
*always* present discriminates better than one that is merely often present.

**The fix** (commit below): state moved from static fields into `SessionState`, which survives domain
reloads, plus an `[InitializeOnLoadMethod]` that re-subscribes `WaitThenExit` after each reload. The exit
path now survives the event that used to destroy it. `Exit(0)` is also called directly rather than
setting `isPlaying = false` in the same frame, since leaving Play mode is itself asynchronous.

**Verified**: first clean self-termination of the session - `EXIT LINE PRESENT: 1`, `Unity EXITED
CLEANLY`, and 75 anomalies matching the baseline exactly, so shutdown behaviour changed while simulation
behaviour did not.

### Two wrong diagnoses, and why each survived so long

1. **Search/asset indexing** (5 occurrences). Plausible because `[Indexing] Starting Initial Indexing`
   appears in every log and the process spins at high CPU. But indexing COMPLETES well before the hang -
   the hang is after `Shut down.`. Never tested, only assumed.
2. **Crashed asset import worker.** Better evidenced - a real `code=10054` crash line, a worker left
   `Connected`, and it neatly explained why the hang worsened after force-kills. **Disproved by testing
   it**: disabling parallel import (`m_DesiredImportWorkerCountPctOfLogicalCPUs: 0`) removed the worker
   crash entirely and the hang persisted completely unchanged.

The second is the more instructive failure: a plausible mechanism, real supporting evidence in the log,
and an elegant explanation for a puzzling correlation - and still a bystander. **A correlated symptom is
not a cause, and the only thing that separated them was running the experiment.**

### Note on the two changes made while chasing the wrong cause

Both were kept, on their own merits rather than as fixes for this: parallel import disabled (removes a
real crash, and the project is small enough not to need workers), and `Library/` deleted and rebuilt
(cleared genuinely inconsistent state from prior force-kills). Neither fixed the hang.

**`m_ParallelImport` does not exist in Unity 6000.5's EditorSettings.** Verified by searching
`UnityEditor.CoreModule.dll` for it and every plausible variant; the only import-worker field is
`m_DesiredImportWorkerCountPctOfLogicalCPUs`, which matches the "25% of logical cores" default. Setting
it to 0 is how you get no workers in this version. Checking the binary mattered: a misspelled YAML key
would have done nothing while looking correct.

## Verification-integrity, instance 4 — the largest one (2026-08-01)

Belongs with the three verification-integrity failures above, and dwarfs them. In this case the
compromised checking mechanism was **the validation harness itself**, and it went undetected across five
occurrences while the blame sat on Unity.

**The bug.** `BatchSimulationRunner.Run()` subscribed `WaitThenExit` to `EditorApplication.update`, then
set `EditorApplication.isPlaying = true` on the very next line. Entering Play mode triggers a domain
reload, and a domain reload unsubscribes every delegate and wipes every static field - so the callback
was destroyed immediately after being registered, and `EditorApplication.Exit(0)` was never reached.
Fixed by holding state in `SessionState` (survives domain reloads) and re-subscribing via
`[InitializeOnLoadMethod]`.

**Five occurrences were blamed on Unity.** First on Search/asset indexing, later on crashed asset import
workers. Both theories were coherent, both fit real log evidence, and the second even explained a
genuinely puzzling correlation (the hang worsening after force-kills). Both were bystanders. The defect
was in our own tooling the entire time, and every "workaround" was treating a symptom of our own bug.

### The diagnostic lesson, stated generally

**A symptom that is ALWAYS present discriminates better than one that is merely often present.**

`"BatchSimulationRunner: exiting after wait."` was absent from every hung run - and, decisively, had
never appeared in ANY run at all, including ones that looked fine. That constant negative was far more
informative than the noisy positives that dominated attention: indexing messages and worker crashes both
came and went across runs, which is exactly what a bystander looks like. **Read logs for what is
reliably absent, not only for what is conspicuously present.** An expected line that never appears is a
stronger signal than an alarming line that sometimes does.

### Correlation versus cause: only the experiment settled it

The import-worker theory was not defeated by better reasoning - the reasoning was fine. It was defeated
by an experiment: disabling parallel import removed the worker crash **entirely**, and the hang persisted
completely unchanged. **When two phenomena reliably travel together, separating them experimentally is
often the only way to tell which is the cause.** Analysis can generate the hypothesis; only the
intervention distinguishes it from its companions.

### Open question raised by this, not yet investigated

If the validation harness carried a bug this fundamental - never exiting, on every single run - for five
occurrences without being suspected, **what else in the validation tooling is being trusted the same
way?** `SimulationTestRunner` and the various diagnostic patterns have never themselves been validated;
they are the instruments every "PASS" in this file was measured with. Worth an explicit review, on the
principle that an unexamined instrument is an assumption. Note the related precedent already recorded
above: the anomaly detector's percentage metric is mathematically meaningless near zero, which was found
by inspection rather than by the detector ever complaining.

## Narrow harness audit before Step A (2026-08-01)

Scoped deliberately to what Step A's acceptance depends on, not an open-ended review. Three items.

### 1. The identical-trajectory diff — VALIDATED, after fixing a defect in it

**A defect was found in the comparison method itself, not the harness.** Every "identical trajectory"
claim made earlier today used `grep '^Turn '`, which matches only the **anomaly lines**. The log also
contains **600 full-state lines** per 100-turn run (`[baseline/100] Turn N | Country: GDP=…,
Unemployment=…, …`, i.e. 100 turns x 6 countries) beginning with `[scenario/turns]`, and those were never
being compared. A change that shifted simulation values without crossing an anomaly threshold would have
passed silently - precisely the small, slow drift a published-value leak causes.

**Validated by negative control**, because a comparison never shown to fail on a real difference has not
been validated: `OkunCoefficient` was changed 0.5 -> 0.5001 (0.02%), the seeded run repeated, and the
full-state diff caught it at **Turn 4** as `GovernmentDebt=42358,1 -> 42358,2`. Change reverted and
verified clean against HEAD afterwards. Sensitivity to one digit in the fourth significant place is
ample for leak detection.

**Use the full-state lines for any before/after proof:**
```
grep "^\[baseline/100\] Turn" <log> > state.txt      # 600 lines, the real trajectory
grep "^Turn " <log>                                   # anomalies only - NOT sufficient
```

### 2. Anomaly detector near-zero defect — FIXED, and it changes historical counts

`CheckSwing` computes a relative percentage, which is meaningless when both values are near zero:
`DebtToGdpRatio` 1.48 -> 25.47 reported as "swung 1616.0%". The pre-existing `< 0.01f` guard was far too
low to suppress it. Added `MinMagnitudeForSwingCheck = 2f`, requiring **both** values to be under the
floor before suppressing - so a genuine move out of the near-zero range (0.87 -> 5.80) is still reported,
and only tiny-to-tiny noise is filtered.

**DISCONTINUITY WARNING.** This lowers anomaly counts versus every historical figure recorded in this
file (28, 34, 53, 56, 60, 74, 80, 85, 86, 96, 97, 104, 106, 178, 215 all predate the fix). **Do not
compare a post-fix count against a pre-fix one and read the drop as an improvement in the simulation** -
nothing about the simulation changed, only what gets counted. Post-fix counts are comparable only with
each other.

### 3. Seeded reproducibility — CONFIRMED on values

Two seeded runs at `-seed=12345` on different HEADs (before and after the batch-exit fix) produced
**600/600 identical full-state lines**. That confirms both that seeding works and that the harness exit
fix did not perturb the simulation.

### Out of scope, logged not chased

A general review of the diagnostic tooling was explicitly excluded. Open question still standing: the
harness carried a fatal bug for five occurrences without being suspected, and `SimulationTestRunner`'s
other checks (finite-value checks, threshold constants like `MaxUnemploymentPercent`) have never been
validated against known-bad input the way the diff now has been.

## Verification-integrity instance 6 — the worst of the set (2026-08-01)

**Why this one is worse than instances 1-5: a clean diff was exactly what success would have looked
like.** The other five produced output that was wrong in some noticeable way once examined. This one
would have produced the precise result that means "everything is correct", and there would have been no
reason to look further.

**The setup.** Step A's acceptance is an identical-trajectory proof: run the seeded scenario before and
after, and any difference means published values leaked into the simulation. `PublicationSystem` is
DATE-DRIVEN - releases fire on "first Friday", "the 12th", "t+30 after quarter end".

**The trap.** `SimulationTestRunner` called `AdvanceTurn` directly and never called `AdvanceDay`, and
`AdvanceTurn` only READS `CurrentDate` without advancing it. So the calendar was frozen at EpochDate for
every batch run ever performed. Wiring `PublicationSystem` into the daily loop would have meant it
published **nothing** during validation. The diff would have come back byte-identical, Step A would have
been marked validated with "zero simulation change", and the feature would never have executed once.

**Caught before running it**, by asking where the harness advances time rather than assuming it did -
prompted by the standing question of what else the validation tooling was being trusted to do.

### The wider hole this exposed

This was never a Step A problem. `AdvanceDay` is the entry point for everything the continuous-time
migration introduced, so a frozen calendar means **none of it has ever been exercised in validation**:
fiscal-year budget pauses, Fed meeting cadence, `StatHistory`'s multi-resolution Daily/Weekly/Monthly/
Quarterly bucketing, and every day-driven mechanic since. `StatHistory.Append(CurrentDate, ...)` has been
recording the identical date for every turn of every run in this project's history.

Every "validated against real Unity" claim in this file, prior to commit `e15cb49`, describes a
frozen-calendar configuration the game itself can never be in.

### TWO BASELINE DISCONTINUITIES ON THE SAME DAY - read historical numbers with care

Both landed 2026-08-01, and both change anomaly counts for reasons unrelated to simulation quality:

1. **Anomaly-detector floor** (`MinMagnitudeForSwingCheck = 2f`) - suppresses near-zero percentage
   artifacts that were silently padding counts. LOWERS counts.
2. **Calendar now advances** (`e15cb49`) - date-driven events fire in batch for the first time. Direction
   unknown until measured, but trajectories legitimately change.

**Every anomaly count and trajectory baseline recorded in this file before 2026-08-01 measured a
frozen-calendar world with an inflated counter.** Do not compare a post-fix count against a pre-fix one
and read the difference as the simulation improving or regressing. Only post-fix numbers are comparable
with each other.

### Correction: the calendar fix changed NO trajectories (measured 2026-08-01)

Commit `e15cb49`'s message predicted trajectories would change once the calendar advanced. **They did
not.** Measured: the 600 full-state lines from the calendar-driven baseline
(`stepA_baseline_calendar_e15cb49.log`) are byte-identical to the frozen-calendar baseline. The
75 -> 69 anomaly change came entirely from the detector floor, not the calendar.

**Why**, established by tracing the callers rather than assuming: every date-driven GATE lives in the UI
layer. `UpdateFedChairSelectionState` exists only in `GameController`; the budget-process pause is
consumed there too. `SimulationManager.AdvanceTurn` reads `CurrentDate` solely to stamp
`History.Append`. Batch mode drives the simulation directly, so those gates are structurally unreachable
from it - the calendar advancing cannot change what they do, because they are never called.

**The fix is still correct and was still necessary**, for a different reason than stated:
`AdvanceDay()` is now actually invoked in batch runs, which is what allows `PublicationSystem` to execute
during validation at all. Without it, Step A's proof would have been vacuous. `StatHistory` also now
receives advancing dates rather than the same date every turn.

**Verification that the calendar genuinely advances** (since trajectories can no longer serve as
evidence): the harness raises an anomaly whenever `DaysPerTurn` calls to `AdvanceDay` fail to cross a
turn boundary. Zero such anomalies across 100 turns, which requires the date to have advanced 12,100
days - roughly 33 simulated years.

**Revised discontinuity guidance**: only ONE of today's two changes actually moves the numbers. The
anomaly-detector floor does; the calendar fix does not. Pre- and post-`e15cb49` trajectories are directly
comparable.

**Standing caveat this exposes**: batch validation exercises `SimulationManager` only. Every
player-facing gate and interrupt - budget-process pauses, Fed chair selection, cabinet decisions,
foreign-policy meetings - lives in `GameController` and is invisible to it. That is a real limit on what
"validated against real Unity" means in this file, and it is why live-Editor confirmation has repeatedly
caught things batch runs could not.

## Master Sequence step 9, Step A — DONE (2026-08-01)

Release calendar, published series and the revision mechanic. Commit `e3a0feb`, with the data model in
`8e63a6f` and the seed infrastructure in `75fa05a`/`121656f`.

**Built.** `PublishedData`/`PublishedSeries`/`PublishedEntry` on `Country` (never on `EconomyState`);
`ReleaseCalendar` implementing the seed file's [VERIFIED] rules as date arithmetic; `PublicationSystem`
called once per simulated day from `AdvanceDay`. Revisions APPEND rather than overwrite, so a player who
acted on a preliminary figure can still see the number they acted on. Preliminaries are a noisy estimate
OF the true value, so a revision always converges toward reality rather than being an arbitrary jump.

**Six stats publish, not 29** - only those with a real release rule in the seed data. Inventing a cadence
for something like `ConsumerConfidence` would be the fabrication the `[GAP]` discipline forbids; the rest
keep reading live until a schedule is sourced.

### Validation - five checks, and why the last one is the one that counts

1. `EconomyState.cs` unchanged, so the 55 simulation call sites reading `country.State.X` cannot reach a
   published value.
2. Only `ReleaseCalendar`, `PublicationSystem` and `SimulationManager` reference `Published` anywhere
   under `Assets/Scripts/Simulation`.
3. **600/600 full-state lines byte-identical** to the baseline at `-seed=12345`. Zero simulation change.
4. The diff can actually detect a difference - negative control, `OkunCoefficient` 0.5 -> 0.5001 (0.02%),
   caught at Turn 4.
5. **`PublicationSystem` actually executed: 7087 published entries.**

**Check 5 is the one a clean diff would have hidden**, and it is the difference between this being a
proof and a formality. A trajectory diff returns identical both when the feature is correctly inert AND
when it never ran at all. Checks 1-4 were all satisfiable by an implementation that did nothing.

The count corroborates the release RULES too, not just execution: 12,100 simulated days is ~33.1 years,
predicting ~397 monthly unemployment and ~397 monthly inflation entries per country, USA GDP at three
estimates per quarter (~397) against the EU five at two (~265), plus ~99 annual - **~7080 predicted
against 7087 observed**.

### What this validation does NOT cover

It proves publishing does not DISTURB the simulation. It does not prove the published figures are
CORRECT - that reference periods carry the right lag, that revisions land on the right dates, or that the
preliminary/revised distinction behaves as intended. `ReleaseCalendar` is pure date arithmetic and
directly testable; that check is still outstanding and belongs before Step B builds release-point graphs
on top of it.

## Verification-integrity instance 5 — a correct measurement, over-read (2026-08-01)

**Distinct from instances 1-4 and 6, and worth separating: this check was not broken.** It ran correctly,
measured exactly what it measured, and reported a true number. The failure was in what that number was
taken to mean.

**What happened.** Step A's validation counted **7087 published entries** and treated that as evidence the
publication system worked. The count was accurate. Publication genuinely ran 7087 times, on genuinely
correct dates - the schedule was right, and the count corroborated it against a predicted ~7080.

**What it did not show, at all: whether a single published VALUE was correct.** Nearly every one was the
same stale figure. A count proves cadence. It is silent on content, and no amount of agreement between
predicted and observed *counts* can say otherwise.

**How the real defect surfaced**: only by dumping actual entries and reading them. Q1 2026 Revised, Q1
2026 Final and Q2 2026 Revised all read `28999,3` - and that collision was the decisive clue precisely
because it is STRUCTURAL rather than magnitude-based. A wrong-looking number invites argument about
tolerance; **two different reference periods resolving to an identical figure cannot be explained by
noise at all.** Look for impossible relationships between values, not just implausible values.

**A wrong inference worth recording**, because it was reasonable and still wrong: the preliminary figures
varied (29363,6, 29062,6) while the revised ones were identical, which suggested "the preliminary path
works, the revision path is broken - compare them." Both called the same broken `ReadLiveValue`. The
preliminary's variation was ±1.5% publication noise around the *same stale value*, and both observed
figures sit inside that band. **Apparent variation is not evidence of a live read.** There was no working
path to compare against, so that comparison would have found nothing.

### The general rule

**Ask of every check: what does this prove, and what does it merely appear to prove?** A count proves
frequency. A checksum proves equality. An identical-trajectory diff proves nothing changed - not that
anything ran. Each answers a narrower question than it feels like it answers, and the gap between the two
is where a validated-looking feature hides being broken.

## Harness audit — general tooling review (2026-08-01)

Prompted by six verification-integrity instances in a single step. The question asked of every check:
**what does this prove, and what does it merely appear to prove?**

### Coverage, measured rather than assumed

| Check | Fields covered | Assessment |
|---|---|---|
| `CheckFinite` (NaN/Infinity) | **29 of 29** `EconomyState` floats, plus Country-level fields | Complete. Proves what it appears to. |
| `CheckSwing` (>20% turn-over-turn) | **5 of 29** — GDP, Unemployment, Inflation, InterestRate, DebtToGdpRatio | **Major gap.** See below. |
| Range checks (negative / absurd) | **4 of 29** — GDP, Unemployment, Inflation, GovernmentDebt | Narrow, but the four most load-bearing. |

### The swing check covers 5 of 29 tracked values

**This is the instance-5 pattern in the harness's own headline metric.** "N anomalies detected" is the
number every validation writeup in this file quotes as a health signal. It is overwhelmingly composed of
swing anomalies - and the swing check is *structurally incapable* of seeing 24 of the 29 tracked values,
because `Snapshot` stores only five fields to compare against.

A runaway in `PovertyRate`, `CrimeIndex`, `Population`, `LaborForceParticipationRate`, `Consumption`,
`Investment`, `ConsumerConfidence`, `CorruptionIndex` or 16 others would produce **zero** swing anomalies
and a clean-looking run. Only a NaN or a negative GDP/debt would catch it, and only for the four
range-checked fields.

So: the anomaly count proves *those five fields* stayed within 20% turn-over-turn. It does not prove the
simulation is healthy, and it has been read as though it did.

### NOT fixed unilaterally — logged as an Open Question

Extending `Snapshot` to all 29 fields is the obvious fix, and it is deliberately NOT done here, because
it is a judgment call with real consequences rather than a clear defect:

- It would be the **third baseline discontinuity in one day** (after the near-zero floor and the calendar
  fix), further breaking comparability with every historical count.
- Several of the 24 uncovered fields legitimately move sharply - `NetMigrationRate` and
  `PopulationGrowthRate` are small numbers where ordinary variation crosses 20% easily. Coverage could
  bury real signal in noise, which is how the near-zero defect caused harm in the first place.
- The right threshold is likely per-field rather than one global 20%, and choosing 24 thresholds is a
  design task, not a mechanical one.

**Open Question for Elias**: extend swing coverage to all 29 fields (and with what thresholds), extend to
a chosen subset, or leave at five and simply stop describing the anomaly count as a whole-simulation
health measure? The last option costs nothing and removes the misreading, which may be the honest
minimum.

### What was verified as sound

`CheckFinite`'s 29/29 coverage is genuinely complete - every `EconomyState` float plus several
Country-level fields. Whatever else the harness misses, it cannot miss a NaN.

## Verification-integrity instance 7 — a trusted source that was simply wrong (2026-08-01)

**A new variant of the class, and the first that no amount of checking my own work would have caught.**

The six previous instances all shared a shape: a checking mechanism that was broken, absent, or narrower
than it appeared. Instance 7 is different. Nothing in the process malfunctioned. The figures were
sourced deliberately by someone with web access, recorded in a file built specifically to be the
project's ground truth, and marked `[VERIFIED]`. The check *was* "this was sourced carefully by a person
who could actually look it up" — and that turned out not to be sufficient.

**What happened.** `POLISIM_SEED_DATA_MACRO_OVERHAUL.md` recorded housing cost overburden rates as
Germany 9.7, Poland 6.1, Sweden 5.1, France 3.9. Those are real Eurostat figures. They are also the
**"Two adults" household-type subset**, not the headline whole-population indicator (`ilc_lvho07a`).
The correct whole-population 2024 figures are Germany 12.0, Sweden 10.6, EU average 8.2. Sweden differs
by more than 2x between variants — 5.1 versus 10.6 versus 10.8 for the "18–64" cut.

**Why it is worth a numbered entry rather than a footnote.** The seed file *already contained an
explicit warning about exactly this trap*, two sections earlier:

> **CRITICAL METHODOLOGY WARNING:** youth unemployment *rate* and *ratio* are different measures and are
> frequently confused in published tables.

The same mistake was then made again in the very next data section. Knowing a trap exists, and having
written the warning yourself, does not prevent walking into it — because the failure is not one of
attention but of *not recording which variant a number belongs to at the moment you write it down*.

**The practical lesson, which is narrower and more useful than "double-check sources":** when a
statistical indicator has multiple published variants, **record WHICH variant explicitly alongside the
value** — the indicator code, the population base, the threshold — not just the number and the year.
`Germany 9.7` is unfalsifiable after the fact. `Germany 9.7 (ilc_lvho07a, two-adults households, >40%
of disposable income, 2024)` announces its own scope and would have been caught on sight.

This applies directly to every remaining `[VERIFIED]` figure in the seed file. Gini already carries a
methodology warning; productivity already carries a source-conflict warning. Youth unemployment,
life expectancy and real wage growth do not, and have not been re-checked against this failure mode.

**What it does NOT mean.** It is not an argument for distrusting the seed file or for filling gaps by
inference — the opposite. The file remains the only real-data ground truth available in this project,
and the correction arrived through exactly the right channel: Elias re-checked and supplied a fix. The
lesson is about metadata discipline when recording figures, not about the sourcing itself.

### Consequence for Step C1 — the coverage question resolved in the unexpected direction

Three claims existed about housing cost overburden coverage across the six playable countries. The
directive claimed complete coverage ("all EU five"). The seed file's original figures implied 4 of 6 —
which my first C1 gap report reported, correctly catching the directive's overstatement but trusting the
underlying numbers. The corrected figures give **2 of 6**: Germany and Sweden only, with Italy, France
and Poland known solely to be below 9.0, and the USA not obtainable at any comparable threshold.

So the C1 gap report was right that the directive overstated coverage, and wrong about the actual
number, in the same direction it was investigating. Coverage was worse than the pessimistic reading.

This inverts the metric decision. Housing cost overburden was recommended as C1's primary stat partly
because it was believed better covered; it is now covered **half as well** as homeownership rate
(2 of 6 versus 4 of 6), and its remaining gaps are harder — three Eurostat lookups plus one country
where no comparable figure exists at any threshold. The directive's *other* reasons for preferring it
(it measures affordability stress rather than tenure, and responds to interest rates and housing
assistance, both live levers here) are untouched and still good.

**Escalated to Open Questions, not decided.** Choosing between coverage and interest-rate
responsiveness is a judgment about how C1 should play, not a data question. See
`MISSING_PREREQUISITES.md` B1 and B6 for the three options and the per-option gap lists (was `STEP_C1_HOUSING_GAP_REPORT.md`, consolidated 2026-08-02).

### Instance 7, follow-up — why this indicator defeated ordinary care, and the rule that follows

A gap-closing attempt on the three missing overburden figures failed, and the reason upgrades instance 7
from "a mistake was made" to "a bare figure for this indicator is meaningless".

**Eurostat publishes at least eight variants under the one name** "housing cost overburden rate": whole
population, two adults, 18–64, 65+, cities, rural areas, tenant at market price, tenant at reduced price,
owner with mortgage, owner without mortgage, and by income quintile. **Sweden alone reads 5.1 / 10.6 /
10.8 / 17.9** depending which is pulled. The original error was not carelessness about an obscure
distinction — it was picking one of eight equally-real published numbers, all correctly labelled at
source, and recording only the digits.

**Secondary sources make it worse.** Visual Capitalist, explicitly citing Eurostat 2024, publishes
Denmark at 22.7% and Norway at 21.0% against Eurostat's own 14.6% for Denmark. Reputable outlet, correct
attribution, different variant, no label. A reader checking "does this match Eurostat?" would find the
citation correct and the number wrong.

**The rule, and it generalizes past this indicator:** when a statistical indicator has multiple published
variants, **record WHICH variant alongside every value — never just the number.** The indicator code,
the population base, the threshold, the year. `Germany 9.7` cannot be checked after the fact.
`Germany 9.7 (ilc_lvho07a, two-adults households, >40% of disposable income, 2024)` announces its own
scope and is falsifiable on sight.

Corollary for sourcing sessions: **a summary article is not a database.** Eurostat's own 2024 article
names only the extremes — five countries above 9.0%, three below 4.0% — which is why Italy, France and
Poland could be *bounded* to 4.0–9.0 but not valued. That bound is a real, honestly derived constraint
and is recorded as `[BOUNDED]`, deliberately distinct from `[VERIFIED]` and `[GAP]`. **A bound is not a
value and must never be seeded as one.**

### C1 metric decision — homeownership over overburden (Elias, 2026-08-01)

**The directive's recommendation is reversed.** Overburden remains the better *concept* — affordability
stress rather than tenure, responsive to interest rates and housing assistance, both live levers — and
that reasoning was never wrong. It lost on data honesty: 2 of 6 verified means seeding four countries
from a range, which is inventing precision.

Homeownership carries 4 of 6 verified and preserves the sharpest real contrast in the seed data,
**Germany ~47% versus Poland ~87%** — genuine, culturally rooted, and enough on its own to make housing
policy play differently between countries without a single invented figure.

Overburden is **deferred, not dropped**; it returns as a secondary metric if exact `ilc_lvho07a`
whole-population figures are obtained, which requires direct database access rather than search.

Worth recording as a decision pattern: *the better mechanic lost to the better-sourced one.* When those
conflict on this project, coverage wins, because an invented seed is undetectable later while a missing
secondary metric is merely absent.

### Instance 7, refinement — the variant space is consistently larger than the warning written for it

Youth unemployment was re-checked against instance 7's failure mode. **The existing seeds survive**:
Italy 20.1 and France 18.7 are genuine 15–24 *rates*, not ratios, independently confirmed for June 2025
against an EU average of 14.8%, with Eurostat's definition on record. The rate-vs-ratio warning already
in the seed file did its job.

But the re-check found a **second variant axis the warning did not name: age bracket.** Eurostat
publishes both 15–24 and 15–29 series, crossed with rate-vs-ratio, giving four variants rather than two.
EU 2025 reads 14.8% (15–24 rate), 11.7% (15–29 rate), 6.3% (15–29 ratio). Sweden sits on the fault line:
**22.2% (15–24 rate) versus 12.2% (15–29 ratio)** — both real, both correctly attributed, differing by
nearly 2x.

**Recorded as a refinement of instance 7 rather than a new instance**, because the mechanism is
identical. What is new is the pattern across two indicators:

| Indicator | Variants the warning implied | Variants that actually exist |
|---|---|---|
| Housing cost overburden | 3 | **8+** |
| Youth unemployment | 2 (rate/ratio) | **4** (rate/ratio × 15–24/15–29) |

**The generalizable form: variant space is consistently larger than the warning written for it.** In both
cases the warning was correct, useful, and one dimension short. So a warning that enumerates variants
must be read as **"at least these"**, never as "these" — an enumeration is a floor, not a boundary. The
practical consequence is that "I checked it against the documented warning" is not sufficient
verification; the question is whether any *undocumented* axis exists, which is a different and harder
question.

This is itself a verification-integrity shape: the warning is the checking mechanism, and a check that is
sound but incomplete reads exactly like a check that is sound.

### Homeownership carries the same risk, unresolved, and C1 now depends on it

Life expectancy, real wages and homeownership have not been re-checked. Life expectancy is likely
low-risk — period versus cohort is the main axis, and 84.1 for Italy/Sweden is plausible for period.
**Homeownership is not low-risk and is now urgent**, because the C1 decision made it the primary housing
metric. Its axes: Eurostat measures share of *population* in owner-occupied dwellings (EU 68.4%), while
US Census and most OECD reporting measure share of *households* owning; Eurostat additionally splits
nationals-only from all residents.

**Inspection of the existing table suggests the mixing may already be present**, without needing new
sourcing: Germany carries two figures from two bases (46.7 in 2022 versus "Eurostat nationals-only 52.3
in 2024", a 5.6-point spread) and the row settles on ~47 without saying which; Poland's ~87 sits against
a Eurostat line elsewhere in the same file reading "Poland nationals 87.9%"; and the USA's 65.3–65.9
matches the US Census household-based rate, not Eurostat's population base. So the four `[VERIFIED]`
rows may span two or three denominators — each correct for its own source, and not comparable as a set.

**This blocks C1 ahead of the two remaining lookups.** Sourcing Italy and Sweden before fixing the basis
would add a fourth and fifth variant rather than completing a set. Recommended basis: OECD
household-based, since USA and France are already there and the USA has no Eurostat figure at all.

### Instance 7, third confirmation — promoting the pattern to a STANDING RULE

Homeownership was re-checked on a single basis. **The suspicion was correct and understated.** Germany's
spread is three-way, not two:

| Basis | Germany |
|---|---|
| OECD, share of households owning | **41.0** |
| Dwelling-based (2022) | ~46.7 |
| Eurostat, nationals only | 52.3 |

**11.3 points across three definitions**, every one correct for its own source, plus Eurostat's
population-based measure (68.4% EU) as a fourth axis. The figure this project had been carrying — ~47 —
was the middle one, and no row recorded which.

That makes three indicators, each checked against its own documented warning, each hiding a further axis:

| Indicator | Warning implied | Actually exists |
|---|---|---|
| Housing cost overburden | 3 | **8+** |
| Youth unemployment | 2 | **4** |
| Homeownership | (none written) | **4+** |

**STANDING RULE — for any cross-country statistic, assume an undocumented variant axis exists until
proven otherwise, and record the basis alongside every value as a matter of course.** Not "when the
indicator looks tricky" — always. Three for three is no longer an observation about particular
indicators; it is the base rate.

The corollary that keeps biting: **checking a figure against the documented warning is not verification.**
It confirms the axes someone already knew about, which is exactly the subset guaranteed not to contain
the error. This is the verification-integrity class in its purest form — the check is sound, incomplete,
and indistinguishable from complete at the moment you run it.

### Consequence — the C1 margin narrowed, and a claim of mine needed correcting

Single-basis coverage is **3 of 6 for homeownership versus 2 of 6 for overburden**, not the 4-of-6 that
justified the decision. **The decision holds** — three same-basis figures still beat two, and
overburden's missing three are unobtainable by search while homeownership's are ordinary lookups — but
by one country rather than two, and Poland leaves the verified set (its ~87.9 is a Eurostat nationals
line).

I had also argued the decision preserved "the sharpest real contrast, Germany ~47% against Poland ~87%."
**That pair is not usable as stated** — two bases, which is the error being corrected. What survives:
Germany **41.0 against an OECD average of 70.1** is a real same-basis contrast and *more* extreme than
~47 implied, so the German outlier is intact and better evidenced; Poland is directional only until
sourced on the OECD basis.

Recorded because the earlier, stronger claim is in this file's history and should not be left standing.

**Real wage growth is now the last unverified indicator before C3.**

## Verification-integrity instance 8 — `-runmatrix` silently discarded `-seed`

Found while trying to run a before/after trajectory diff for Step A4.

`SimulationTestRunner.Start()` checked `-runmatrix` **first**, and that branch **returns**. The `-seed=N`
handling sat *after* it. So the two flags were mutually exclusive, silently:

```
if (args.Contains("-runmatrix")) { ...run 8 combinations...; return; }   // seed never read
int seed = GetIntArg(args, "-seed=", 0);                                 // unreachable in matrix mode
```

**Why this is the same class again.** `-seed` exists for exactly one purpose — Step A0 added it so
before/after trajectories could be compared strictly. And `-runmatrix` is the run CLAUDE.md recommends
"whenever a change could plausibly affect long-run stability". The single most important validation this
project performs is a seeded matrix diff, and it was the one combination that could not work. A matrix
before/after diff would have compared two **differently seeded** runs and attributed pure RNG drift to
the change under test — producing either a false alarm or, worse, a real divergence dismissed as noise.

Both flags are documented together in this file's own batch-run section, which is what made the
combination look supported. Nothing warned; the seed line simply never printed.

**Fixed** by moving the seed block above the `-runmatrix` branch. Verified working: a
`-seed=777 -turns=20` run now prints "SimulationRandom seeded with 777" and produces exactly 120 state
lines (20 turns × 6 countries).

### Environment notes learned the hard way in the same session

- **Unity 6000.5 ignores `-logFile <path>`** and redirects to `<project>/Logs/Editor.log`, rotating the
  previous run to `Editor-prev.log`. An earlier read of that file mixed a stale run with a current one
  and showed 1200 state lines (two 600-line runs) with two different anomaly counts — briefly
  misdiagnosed as the harness running twice. **Clear or timestamp the log before a run, never trust it
  after.**
- **A killed Unity leaves `Temp/UnityLockfile` behind.** A stale lockfile makes the next `-executeMethod`
  exit immediately, writing a 0-byte log and no error. Remove it when no Unity process is running.
- **`&`-invoking Unity returns before the work finishes.** Follow with `Wait-Process` on the PID.
- **`*>` redirection to a file captured 0 bytes** where direct invocation surfaced the output normally.
  Capture the command's output rather than redirecting it to a file.

---

## Step B2 wired in — sub-screen granularity, and what "deliberately unwired" cost (2026-08-02)

`4869476`. Yesterday B2's rendering was committed **built but unwired**, on the correct reasoning that
`GetConsolidatedTabArea` answers `PolicyLaws` with `Sectors` and driving a stat row off it would print
sector stats on the Labor and Crime & Justice screens. That call was right *at tab granularity*. The
mistake was stopping there: **one level down, the mapping is exact**, and the evidence was already in the
file. Every policy sub-screen declares its own area for its own bill card — `DrawLaborMarketTab` draws
"LABOR MARKET BILL" as `SystemArea.Labor`, and so on. The two new `GetPolicyScreenArea` overloads read
that same declaration, so the stat row cannot disagree with the card directly beneath it.

`GetConsolidatedTabArea`'s own doc comment says it picks hues for **visual distinctness**, not
correctness — `PolicyLaws` got `Sectors` because that colour was unclaimed. It should never be read as a
semantic mapping by anything, and now carries a cross-reference saying so.

**The dead-code finding that prompted this.** All four files added 2026-08-01 had **zero callers**:
`CreditRatingSystem` (none), `DerivedStats` (only `CreditRatingSystem`), `PolicyScreenStats` (none),
`PolicyScreenStatsRenderer` (none) — plus `GraphRenderer.DrawSparkline` and `IconLibrary`'s Stats path,
both reachable only from the dead renderer. 641 lines with no entry point. Only one piece of that was
flagged at the time. **A session that ends with "built but not wired" should state the full reachability
picture, not just the one piece it consciously chose not to connect.**

**`CreditRatingSystem` is still uncalled**, and that is now recorded in the roadmap as an open placement
question rather than quietly fixed. C4 computes correctly and anchors to 5 of 5 verifiable real ratings,
but where a sovereign rating belongs in the UI is a design decision, not a wiring detail.

### Two real findings from the edge list, neither invented to fill a gap

- **No `Infrastructure` policy node has a single Policy Web edge.** The Infrastructure screen therefore
  draws no stat row at all. That is the honest output of a real gap — the row appears by itself the day
  an edge is added, with no change at the call site.
- **`Fiscal` derives 7 stats**, because all 13 `TaxType`s and all 14 spending lines run through the same
  two channels (approval on a hike; revenue/outlay feeding `DebtToGdp`). Hence the 4-stat cap with the
  remainder *stated* ("+N more affected - see Policy Web") rather than silently trimmed. Tax and Spending
  showing the same four stats is correct, not a bug — they genuinely move the same things.

`MeasureHeight` and `Draw` both route through one private `ComputeLayout`, so reserved and drawn height
are the same calculation rather than two that agree until one is edited — the StatTile duplicate-
formatter lesson applied to layout. No stable-control-layout exposure: the row emits only
`GUILayoutUtility.GetRect` plus `GUI.Label`/`DrawTexture`, and no interactive control allocates an ID, so
a chip count changing between frames cannot desync a drag in progress.

**Validation.** Unity 6000.5.6f1 batch compile: **0 errors** — the real compiler, which is what caught
the invented `MutedTextColor` yesterday when `dotnet build` did not. The csproj blind spot does not apply
here (existing files were edited, and their `<Compile Include>` entries already existed), but
registration was confirmed before trusting the build regardless. `dotnet build --no-incremental`: **12
distinct warnings, 1 UAC1001 + 11 UAC1009 — identical to yesterday**, none in the touched files. The
10-turn batch run's 17 anomalies are the documented pre-existing Sweden/France debt-to-zero baseline (see
"SpendingLine Amount Ceiling - Debt-to-Zero Fix"); an OnGUI-only change cannot reach simulation math,
which `BatchSimulationRunner` exercises without ever calling `OnGUI`. **Not visually confirmed** — added
as review item 10, and it is the only item there where rejection would mean a Policy Web *edge* is wrong
rather than something merely looking bad. *(Confirmed 2026-08-02 — see `COMPLETED.md` section 16.)*

### Also fixed: four `.meta` files were never committed

`e185a72`. The four `.cs` files added 2026-08-01 were committed without their Unity `.meta` files, while
all 62 other script metas in the repo are tracked. The GUID lives in the meta, not the source, so a fresh
clone would have had Unity mint new ones and any future serialized reference to these types would have
resolved differently between machines. **Staging a new `.cs` in a Unity project means staging its `.meta`
in the same commit.**

### Corrections to this file's own environment notes

- **`-logFile <path>` IS honoured on 6000.5.6f1.** The note above ("Unity 6000.5 ignores `-logFile`") is
  wrong as stated. Direct evidence from this session: the run wrote 135,445 bytes to the custom path at
  10:20:03, while `Logs/Editor.log` (786,438 bytes) and `Logs/Editor-prev.log` sat untouched from
  2026-08-01. The original observation was real but the cause was misattributed — most likely the stale-
  log confusion described alongside it, not the flag being ignored. **Timestamping the log per run is
  still the right practice**, which is what makes this verifiable at all.
- **"`&`-invoking Unity returns before the work finishes" — confirmed again, the hard way.** `Unity.exe`
  is a GUI-subsystem binary, so PowerShell's `&` does not wait: `$LASTEXITCODE` came back empty and the
  log did not exist yet, which looked like a failed launch when Unity was in fact mid-compile. Poll the
  log for a terminal marker (`exiting after wait` / `Sanity check complete` / `error CS`), or
  `Wait-Process` on the PID.
- **`[regex]::Escape` combined with `-SimpleMatch` silently finds nothing.** A csproj registration check
  reported `GameController.cs = 0` — the escape produced `GameController\.cs` and `-SimpleMatch` then
  searched for that literal. It reported "not registered" for a file that has been registered for
  months. Use one or the other, never both, and treat a *universal* negative from a verification check as
  evidence the check is broken rather than the code.

---

## Step C4 placed, and its first trajectory validation fails (2026-08-02)

`3d77b11`. Elias overruled the escalation from earlier the same session, correctly: placement is not the
kind of genuine design fork working-discipline item 4 covers — it is the same reasoning applied to every
other stat — and leaving C4 uncalled was *blocking* validation rather than protecting it.

**Placement (PROVISIONAL).** Credit Rating joins the dashboard tile grid directly after Debt-to-GDP. A
sovereign rating is a judgment *about* a fiscal position, so it sits beside the number it is mostly a
judgment about. Computed every frame rather than cached — caching is exactly how a rating could come to
disagree with the debt figure rendered next to it. Only a Positive or Negative outlook draws a pill:
`StatTile`'s pill is binary (good/bad) and "Stable" is genuinely neither, so colouring it either way
would assert something the model does not claim.

### The tile was not what made validation possible — the harness change was

**A dashboard tile is `OnGUI` code, and `BatchSimulationRunner` never calls `OnGUI`.** Placing C4 on the
dashboard would have left it exactly as unreachable from a batch run as it was before. `SimulationTestRunner`
now evaluates both A4 and C4 per turn, per country, inside `CheckAnomalies`, which is what actually put
them under the matrix. **Worth remembering the next time "wire it in" is offered as a route to
validation: wiring into the UI and wiring into the harness are different things, and only one of them is
checkable headlessly.**

**Why pure display arithmetic needs coverage at all.** Neither can change a simulation number. But
`CreditRatingSystem.Evaluate` ends in `Mathf.RoundToInt(notches)` followed by a clamp, and
`RoundToInt(NaN)` is **0**, which clamps to **AAA**. A non-finite deficit or growth term would not crash
and would not look wrong — it would render the best possible rating on a broken country. That is the
StatTile failure shape again (a plausible-looking wrong number), and only an input finiteness check
surfaces it.

### Results — full matrix, 15 scenarios × 100 and 500 turns, `-seed=777`, real Unity 6000.5.6f1

**A4 — PASSES.** Zero finiteness failures across all 30 runs, all six countries, every turn:
`GdpPerCapita`, `TaxBurdenPercentOfGdp`, `SpendingPercentOfGdp`, `DeficitPercentOfGdp`,
`RealGdpGrowthPercent` and every sector share. The "NOT trajectory-validated" caveat `70798e9` shipped
with is discharged.

**C4 — FAILS. 3,421 anomalies**, every one a rating moving more than four notches in a single turn.

> ⏩ **This records the state on 2026-08-02 BEFORE the A1 cadence fix, and is not current status.** Elias
> ruled the fix should be review cadence rather than damping. C4's implementation is now **complete**, the
> anchors still hold 5 of 5, and the residual anomalies were attributed to the debt-to-zero bimodality
> rather than to the rating. See "A1 implemented" further down this file.

| Country | Anomalies |
|---|---|
| Sweden | 1,761 |
| France | 1,117 |
| Germany | 240 |
| USA / Italy / Poland | 0 |

**282 are full-ladder 16-notch moves — AAA to CCC and back the following turn.** Present in plain
`baseline` at both horizons, so this is not a stress-scenario artifact.

**The cause is pinned by deduction from the logged data, not guessed.** The anomaly message includes the
effective debt burden precisely so this is answerable without another run. At each of those moments the
burden is 0–45%, which `BurdenCurve` maps to 0–1 notches ((0,0) and (70,0) are its first two points), and
the growth term contributes at most ±0.5. **Only the deficit term can supply the remaining 5–16 notches**,
and it is `notches += (deficitPercent - 3) / 3` — no cap, no smoothing. Worked back: Sweden turn 78
(burden 44.5%, AAA→B) implies a ~45%-of-GDP single-turn deficit; France turn 73 (burden 77.3%, AAA→A)
implies ~17%.

**Escalated rather than fixed, and this is not the earlier over-escalation repeating.** C4's curve was
calibrated against 5 of 5 verifiable real-world ratings, and the USA's AA+ *depends specifically* on its
deficit exceeding 3% — the exact term every plausible fix modifies. Capping it, averaging it over turns,
or rating off a smoothed fiscal position all change what that calibration runs through, so this is a
re-calibration against the real anchors rather than a tweak. Guessing a smoothing window would quietly
invalidate the one thing that made C4 credible. Recommendation and the interim options are in the
roadmap's Open Questions.

**The tile is live and honest about which countries it is wrong for**: correct for USA, Italy and Poland;
visibly thrashing for Sweden, France and Germany.

### Anomaly-count bookkeeping

New anomalies carry a `[DERIVED]` prefix so they stay separable from the counts quoted throughout this
file, which all predate this coverage — **this is a third baseline discontinuity for anomaly counts, and
the READ FIRST note at the top of this file governs it like the other two.** The checks are additive and
pure (no `SimulationRandom` draws, no mutation), so they cannot alter a trajectory; determinism under
`-seed` is preserved, and the run confirmed "SimulationRandom seeded with 777 - this run is reproducible."

Per-scenario split (total / derived / pre-existing) is in `Logs/a4c4_matrix_20260802.log`. Note the
pre-existing counts are **not** comparable to older recorded figures: those were taken at different seeds
and across two prior discontinuities.

### One more environment note

Reading a Unity log while Unity still holds it throws `IOException` (file in use) from
`File.ReadAllText`. `Select-String` opens shared and works fine. Poll with `Select-String`, or wait for
process exit before reading the whole file.

---

## A1 implemented: rating by scheduled review — anchors hold, thrash does NOT clear (2026-08-02)

`a4155ca`. Elias's ruling: fix the thrash by review **cadence**, not by damping. Implemented as specified.
**The acceptance bar was "the 3,421 anomalies must be GONE, not merely reduced". They are not gone.**

### What was built

**Changes WHEN the rating is computed, not HOW.** The formula body moved verbatim out of `Evaluate` into
`EvaluateFrom(debtToGdp, riskPremiumSensitivity, deficit, growth)`; `BurdenCurve`, the reserve-currency
discount, the deficit divisor and the growth thresholds are untouched, and both the live path and the
scheduled review call the same method so they cannot drift into two formulas.

**Cadence: annual, on each country's own fiscal-year start** (USA 1 Oct, EU five 1 Jan). Justified rather
than defaulted — agencies review once or twice a year; the date already exists as
`FiscalYearData.GetFiscalYearStart` and is already what `ReleaseCalendar` treats as the boundary annual
figures settle on, so it adds no date rule and no parallel timer; and it is the same boundary the budget
process turns on, so a review lands when the year it judges has closed.

**The settled deficit is derived from the debt stock, and that is the substantive fix.** The thrash came
from `FiscalTurnReport.BudgetBalance` — one 121-day turn's balance. A year's deficit is by definition the
year's increase in indebtedness, so with both stocks recorded it is exact rather than smoothed:

    Debt = (d/100) * Y   =>   deficit%(of current GDP) = d_now - d_prev * (Y_prev / Y_now)

Both readings come from `PeriodClosingValues` on the **same** quarterly boundaries, which is why
`ClosingStat.DebtToGdpRatio` deliberately shares GDP's period rule.

**On reusing `PeriodClosingValues` rather than a parallel store, as instructed:** it is keyed by
`PublishedStat`, which has no debt member. Rather than add one — that enum makes a deliberate claim that
only stats with a REAL release rule appear in it, and a never-published member would contradict it and
make `Latest()` a permanent null trap — the key was widened to a new `ClosingStat` superset. One store,
one recording pass, an explicit exhaustive map that throws on drift.

### Validation 1 — the 5-anchor calibration check: 5 of 5 PASS, unchanged

Run **before** the matrix, as instructed. New `CreditRatingAnchorCheck` runs headlessly with **no Play
mode** (the formula is pure arithmetic), so it takes seconds.

| Anchor | Debt | Effective burden | Result | Expected |
|---|---|---|---|---|
| Sweden | 35.0% | 35.0% | AAA | AAA ✓ |
| Germany | 63.0% | 63.0% | AAA | AAA ✓ |
| France | 116.0% | 116.0% | AA− | AA− ✓ |
| Italy | 138.0% | 138.0% | BBB+ | BBB+ ✓ |
| USA | 124.0% (deficit 6.4%) | 63.2% | AA+ | AA+ ✓ |

**This is the FIRST EXECUTABLE version of this check.** The original calibration (`76a8f35`) was done by
hand and recorded only in a commit message, so "passes unchanged" means "reproduces the five results
recorded there", not "matches a previous script". Codifying it was overdue: an anchor check that exists
only in prose cannot be re-run after a change, which is exactly what this work needed it for.

The USA anchor is the one with a tunable input, so the check reports the band rather than only that one
value passes: **the USA holds AA+ for deficits in [4.6%, 7.5%] of GDP**, and the anchor uses 6.4% — near
the middle, not balanced on an edge.

### Validation 2 — full matrix: 3,421 → 1,416. Reduced 59%, NOT eliminated

15 scenarios × 100 and 500 turns, `-seed=777`, real Unity 6000.5.6f1, 0 compile errors.

| | Before | After |
|---|---|---|
| Sweden | 1,761 | 616 |
| France | 1,117 | 567 |
| Germany | 240 | 103 |
| USA / Italy / Poland | 0 | **0** ✓ |
| Finiteness failures | 0 | **0** ✓ |

The "countries currently at zero must stay at zero" requirement is met.

**A defect in the check itself was found and fixed mid-validation.** An intermediate run showed 1,446 with
30 new Italy anomalies, all on turn 1. `CreditRating.AAA` is enum value 0, so the pre-run snapshot
recorded an unrated country as AAA and every country's **first** review registered as a spurious
multi-notch downgrade. The check now compares two *reviewed* ratings only. Same
confident-wrong-default failure the tile guards against with its em dash — an unrated sovereign is not a
top-rated one.

### Why the residual is NOT a rating defect — the discrepancy, reported rather than tuned away

Elias's instruction was to report a discrepancy rather than adjust until it passes, because *"a
calibration that needs tuning to survive a change in read-timing is telling you something about the
fiscal position being read."* That is exactly what happened.

**The settled ANNUAL deficit ranges from −135.5% to +170.8% of GDP**, and 773 of 1,416 flagged readings
exceed ±20%. The review is reading correctly; what it reads is not credible. Sweden's `DebtToGdpRatio` in
plain `baseline` runs 21.8% → 0.90% → **0.00%** by turn 50 and stays pinned there, while stress scenarios
show the same stock spiking back to ~44% and collapsing again within a year.

**That is the already-documented debt-to-zero bimodality** (CLAUDE.md, "SpendingLine Amount Ceiling —
Debt-to-Zero Fix"; "Both Sweden and France settle at `DebtToGdpRatio` = 0.0% by turn 500"), and it is
roadmap failure pattern 4, bimodal attractors. The affected set — **Sweden, France, Germany** — is the
documented set. USA, Italy and Poland, whose debt trajectories are well-behaved, produce zero anomalies
both before and after.

**No review cadence can stabilise a rating over a debt stock that oscillates between 0% and 45% of GDP
inside a year, and none should.** A sovereign whose debt genuinely moved like that *would* be downgraded
repeatedly. Reporting the downgrade is correct behaviour; the input is what is wrong. Damping it — the
option Elias rejected — would have hidden this rather than surfaced it.

**Conclusion: C4's implementation is COMPLETE and correct, and the cadence fix worked as intended. The
blocker moves upstream** to the debt-to-zero defect — a pre-existing simulation-model problem C4 is merely
the first stat to read year-over-year. Tracked as `MISSING_PREREQUISITES.md` section F1; recorded as an
upstream dependency rather than left looking like unfinished rating work.

### C4 is the first instrument that makes the bimodality player-facing

**Worth recording separately, because it changes the DEFECT's priority rather than the rating's status.**
Until now the debt-to-zero bimodality was a **log-only** finding: it lived in anomaly counts, batch-run
summaries and this file's own prose. Nothing on screen reported it, and a player could run 100 turns as
Sweden without ever being told their national debt had reached exactly zero and stayed there.

The credit rating tile changes that. It sits in the dashboard grid, visible on every tab, and reports its
input faithfully — so a debt stock swinging 0% to 45% and back inside a year now surfaces as a rating
visibly collapsing and recovering. **The defect did not get worse; it got a display.**

Consequences, both now recorded in the roadmap: its **priority rises** (it blocks a step AND is
player-visible, neither of which was true before), and **it must not be fixed by damping the rating** —
that option was raised and explicitly rejected in A1, and doing it now would return the defect to log-only
while making C4 dishonest. A derived stat that stayed calm while its inputs did this would be the broken
one.

### A further verification-integrity variant, per Elias's note

C4 was reported as "calibrated against 5 of 5 verifiable anchors" while **nothing called it**. Those
anchors were hand-fed static debt/deficit/growth values, so the calibration proved the formula computes
correctly at five points and said nothing about behaviour across a trajectory. The thrash appeared the
moment it became reachable and was fed real simulated state.

**The class: a correct measurement taken under conditions the system will never actually run in.** The
check was sound; its conditions were not representative. This is why the anchor check is now executable
and the harness evaluates the rating per turn — a static check and a trajectory check answer different
questions, and only the second one notices this.

---

## Verification-integrity instance 9 — an enum whose zero value is a meaningful state (2026-08-02)

**The defect was in the check, not the system under test.** Same class as instances 4–8: the checking
mechanism was compromised while the thing being checked was fine.

**What happened.** `SimulationTestRunner`'s rating-thrash check snapshots each country's rating before the
run and flags any single-turn move beyond four notches. The snapshot is taken before day one, when no
review has run yet — so it recorded `country.Rating.Rating`, which was the **default** value of the
`CreditRating` enum.

```csharp
public enum CreditRating
{
    AAA = 0, AAplus = 1, AA = 2, ...
}
```

**`AAA = 0`, and `default(CreditRating)` is therefore AAA.** An unrated country snapshotted as
*top-rated*. Then each country's first scheduled review set its real rating, and the check read that as a
downgrade from AAA:

```
[DERIVED] Turn 1 Italy: CreditRating moved 7 notches in one turn
          (AAA -> BBB+, reviewed burden 138,0%, settled deficit n/a%)
```

Italy at 138% debt is *correctly* BBB+ — it is one of the five calibration anchors, and it passes. Nothing
moved. **30 anomalies were fabricated for Italy alone**, and Italy had been at zero before the change, so
this read as a regression caused by the cadence work. It was not.

**Why it is worth a numbered instance rather than a footnote.** The zero value of an enum is not neutral
when zero is itself a meaningful state. `CreditRating` is ordered best-to-worst by design — the ordering
*is* the semantics, and `InterpolateCurve`/`Mathf.Clamp` depend on it — so AAA has to be 0. The bug is not
the enum; it is that **"no value yet" and "the best possible value" became indistinguishable**, and a
zero-initialised field silently chose the flattering one.

**This is the same shape as two other findings on the same feature, which is what makes it a pattern:**

| Where | The confident wrong default |
|---|---|
| `Evaluate` | `Mathf.RoundToInt(NaN)` is 0 → clamps to **AAA**. A broken country renders as top-rated |
| Dashboard tile | An unreviewed rating would render as **AAA** rather than "not yet rated" |
| Harness snapshot | An unreviewed rating snapshots as **AAA**, so a first rating reads as a downgrade |

All three resolve the same way and all three are now fixed: carry an explicit
`HasBeenReviewed`/`float?`-style "no value yet" flag rather than letting a zero-initialised field stand in
for one. The tile renders an em dash; the harness compares only two *reviewed* ratings; `EvaluateFrom`
takes `float?` for the terms that may not be computable.

**STANDING RULE:** when an enum's zero value is a real, meaningful state — especially a *good* one — a
zero-initialised field cannot distinguish "unset" from that state. Either carry an explicit "has a value"
flag, or reserve slot 0 for a `None`/`Unrated` member. Do not rely on the default being obviously wrong
at a glance: here it was obviously wrong only for Italy, whose real rating happens to be far from AAA. The
same bug on Sweden or Germany — both genuinely AAA — would have produced **no anomaly at all** and stayed
invisible.

**How it was caught:** the number moved in the wrong direction. The cadence change should only ever have
*reduced* anomalies, and Italy went from 0 to 30. A change that improves one metric while regressing
another in a way the change cannot explain is worth attributing before reporting either number.

---

## Verification-integrity instance 10 — three broken verification scripts in one day (2026-08-02)

**This instance is not really about PowerShell.** It is about the fact that on a single day, three
separate verification scripts returned **clean, confidently-formatted, universally-negative results**, and
all three were wrong. Each was caught only because the answer contradicted something already known — not
by anything in the output itself.

| # | The script | What it reported | Why it was wrong |
|---|---|---|---|
| a | csproj registration check | `GameController.cs = 0` — i.e. **nothing** registered | `[regex]::Escape` combined with `-SimpleMatch` searched for the literal `GameController\.cs` |
| b | delivered-sprite check | **no** macro sprites delivered | Pattern `stat_*.png` does not match `icon_stat_gdp.png` — the `icon_` prefix |
| c | zip-import verification | **all 84** `Macro_Data_UI` files zero-length, i.e. every asset pack NOT imported | `.Length` on a `PSCustomObject` collides with the intrinsic member and returned nothing, so the `> 0` filter dropped everything |

**How each was actually caught** — and note that in every case the catch was external to the check:

- (a) `GameController.cs` has been registered for months, so "0 registered" was impossible.
- (b) The record said 42 sprites were delivered; the "bad pattern" reading was only preferred over the
  "not delivered" reading after re-checking.
- (c) `icon_stat_gdp.png` had been read as a 256×256 image earlier in the same session, so "zero-length"
  was known to be false.

**Each of these came within one step of a false report.** (b) *was* briefly reported as "no sprites
delivered" and had to be corrected in the visual review record (now `COMPLETED.md` section 16).
(c) would have declared four
fully-imported asset packs broken and blocked a cleanup on fictional missing files.

### Why a universal negative is the dangerous shape specifically

A partial negative invites scrutiny — *"why did these three fail and not the others?"* A **universal**
negative reads as a clean, decisive finding: *nothing is registered, nothing was delivered, nothing was
imported.* It looks like signal precisely when it is most likely to be the check itself failing, because
the commonest script defects — a bad pattern, a wrong operator, a property collision — break **every**
comparison identically rather than a few of them.

The three failures above share no technology and no author error in common. What they share is shape.

### STANDING RULE — self-test before interpreting

**Any verification script capable of returning a universal negative must first run itself against a
known-good case and print the result.** The self-test output has to appear *before* the findings, so that
"the script is broken" and "the finding is real" are distinguishable **at read time** — not afterwards, by
someone happening to notice a contradiction with something they already knew.

The rewritten zip check does exactly this and is the reference implementation:

```
Indexed 544 distinct basenames under Assets/
SELF-TEST icon_stat_gdp.png -> 1 hit(s), first size 3441 bytes (must be > 0)
```

If that line reads `0 hit(s)` or `0 bytes`, every result below it is void, and that is visible immediately
rather than three paragraphs of confident output later.

**Corollary:** a check whose known-good case cannot be named is not yet a check. If there is no example
that *must* pass, there is no way to tell a working script from a broken one, and the output cannot be
trusted in either direction.

This joins instances 4–9 in the same class — **the checking mechanism was compromised, not the thing
checked** — and is the first to yield a rule about how checks must be *written* rather than about what to
be suspicious of after the fact.

---

## Debt-to-zero: mechanism CONFIRMED, and it is the floor clamp — but not the whole story (2026-08-02)

Investigation only. **Nothing was implemented**, per Elias's explicit gate: *"three wrong theories preceded
the right one on the Unity hang; confirm before building."*

**Method.** `Assets/Editor/DebtClampDiagnostic.cs` — a temporary tool reading **only public API**, with no
production code instrumented. The pre-clamp value is *reconstructed* rather than observed:
`ApplyRevenueAndSpending` computes `Clamp(GovernmentDebt - budgetBalance, 0, maxDebt)`, so last turn's
stored debt minus this turn's `FiscalTurnReport.BudgetBalance` is exactly the value that went into the
clamp. Comparing it against what was stored says, per turn, whether a bound bound.

### Confirmed

**1. The floor is the mechanism. `SimulationManager.cs:2145`:**

```csharp
state.GovernmentDebt = Mathf.Clamp(state.GovernmentDebt - budgetBalance, 0f, maxDebt);
```

Baseline, seed 777, 120 turns:

| Country | Floor hits | Ceiling hits | Net-creditor turns | Final debt/GDP |
|---|---|---|---|---|
| Sweden | **67 / 120** | 0 | **120 / 120** | 0.00% |
| France | **14 / 120** | 0 | 56 | 0.00% |
| Germany | 0 | 0 | 0 | 38.57% |
| USA / Italy / Poland | 0 | 0 | 0 | healthy |

**2. The ceiling is never involved.** Zero hits for all six countries. `MaxDebtToGdpPercent = 300f` plays
no part in this defect — worth stating because "a clamp" could reasonably have meant either bound.

**3. Elias's premise is correct, and stronger than stated.** Sweden is a net creditor from **turn 1** — SWF
203.4 against debt 137.9 — and by turn 16 its net position is **−599**. The single-turn excursion below
zero reaches **−64.3% of GDP**. The simulation is not approximating a country near zero debt; it is
suppressing a large, persistent, genuinely negative net position. That is Norway, which is the country
this project already used to calibrate SWF returns.

**4. The affected set is exactly "countries whose SWF drives net position negative", and this explains
Germany.** Germany showed 0 floor hits in baseline yet contributed 103 matrix anomalies, which looked like
a contradiction. It is not: **Germany's anomalies occur ONLY in `swfstress`** (14 at 100 turns, 89 at 500),
and its `swfstress` debt trajectory is 57.2% → **0.0%** → 10.4% → **0.0%** → 24.8% → **0.0%** → 80.1% →
**0.0%**. Same mechanism; Germany just needs the SWF push to reach the floor. Baseline anomalies are
Sweden and France only — precisely the two countries that hit the floor in baseline.

### NOT confirmed — what removing the clamp would and would not fix

**The clamp creates the BOUNCE, but it does not create the underlying volatility.** Sweden's per-turn
budget balance in baseline runs +79.1, +16.0, +48.0, +0.8, +30.2, **−39.8**, +10.5, +14.9 … That
oscillation is upstream of the clamp and would survive its removal.

So the honest split:

- **Removing the floor should eliminate the 0.00% pinning and the bounce off zero** — the specific artifact
  where a stock is held at a bound and then released. That much follows directly from the evidence.
- **It is NOT established that the rating thrash disappears entirely.** If net debt trends smoothly to
  −64% of GDP the effective burden sits far below the curve's first breakpoint and the rating is a stable
  AAA — but a net position *oscillating* around a negative value would still produce year-over-year
  changes, and whether those alone clear the 4-notch threshold is not answered by this data.

**Recommended check to run WITH the implementation, not after it:** re-run this same diagnostic with the
floor removed and compare the per-turn `budgetBalance` series — unchanged, as it must be if the change is
purely to the stock — alongside the resulting year-over-year debt deltas. If the deltas fall below the
notch threshold, the fix is complete; if they do not, the residual is budget-balance volatility and is a
separate defect that was hidden behind this one.

**Second-order consequence worth designing for deliberately:** with debt clamped at zero, interest on debt
is zero, so a net creditor currently earns **nothing** on its net assets. Removing the floor without
deciding how negative debt interacts with `GetInterestOnDebt` would silently create either free money or a
new asymmetry. This is a design decision, not an implementation detail.

### Method note — the diagnostic's first output was unusable, and the self-test rule caught it

The first version wrote `{x:F2}` into a comma-separated file under a Swedish locale, so decimal commas
split every row into the wrong fields and the parse reported Germany at 4644% debt. **The integer summary
counts were unaffected**, which is exactly how a half-broken output misleads — part of it looked right.

Fixed with `InvariantCulture` and a semicolon separator, and the tool now prints a **self-test first**, per
the standing rule from verification-integrity instance 10:

```
SELFTEST seed Sweden   debtToGdp=35.00% debt=217.00 gdp=620.00
SELFTEST seed Germany  debtToGdp=63.00% debt=2961.00 gdp=4700.00
```

Those are the seeded values and the C4 calibration anchors. If they do not read that way, everything below
is void — visible before any finding is interpreted rather than after.

---

## The sparkline crash — two transferable lessons (2026-08-02)

Fixed in `e9e3f6a`; the defect itself is written up in `COMPLETED.md` section 16, with the stack trace.
Two points generalise well beyond it.

### 1. Sharing an algorithm was right. Sharing its constants was not.

`5701a04` deliberately reused `GraphRenderer.DrawLine` for the sparkline *"so a sparkline can't disagree
with its full-size counterpart"*. That reasoning was sound and is still sound — one Bresenham
implementation, one behaviour. What went wrong is that the shared helper also carried the **full-size
graph's dimensions**: `SetPixelSafe` bounds-checked against `TextureWidth`/`TextureHeight` (300×90) and
strided by 300, while the sparkline handed it a 72×20 buffer.

**A helper shared across callers must take everything that varies between them as a parameter.** A
constant baked into a shared helper is an assumption about *one* caller, silently imposed on all of them.
The giveaway here was in plain sight: a method named `SetPixelSafe` that is only safe for one buffer size.

### 2. The defect survived because its only entry point required `OnGUI`

`DrawSparkline` calls `GUI.DrawTexture`, which throws outside `OnGUI`. So **no headless test could reach
the arithmetic at all** — not because anyone decided against testing it, but because there was no callable
surface that didn't drag the GUI in. It shipped, passed a build, passed a matrix run, and was only caught
by a human opening the screen.

The fix was to split the pure part out: `BuildSparklinePixels(width, height, history, color, maxPoints)`
returns a `Color[]` and touches no GUI. `GraphRendererDiagnostic` now covers **336 width × height ×
series-shape combinations**, which would have caught this before it ever rendered.

**STANDING RULE: where pixel or layout maths sits behind a GUI-only entry point, extract the maths.** The
test for whether this applies: *can I call the calculation from a batch-mode Editor method?* If not, the
calculation cannot be regression-tested, and every future change to it is unverifiable by anything except
a human looking at a screen. Other places this likely applies, unaudited: `MapRenderer`'s projection maths,
`PolicyWebRenderer`'s node layout, `HemicycleRenderer`'s seat arc, `PoliticalCompassRenderer`'s scaling.

---

## P2 — the unit bug: investigation, and one recommended approach (2026-08-02)

**Not patched.** The spec asked for investigation and a single proposal, on the explicit grounds that *"a
unit-aware formatter applied inconsistently is exactly how this bug survived two previous fixes."*

### Finding 1 — the base unit is BILLIONS, and it is consistent

Checked against seeds rather than assumed. USA GDP `29000` = $29,000B = **$29T**; `GovernmentDebt` 35960 =
124% of GDP; `SocialSecurity` 1530 = $1.53T against a real ~$1.5T; `Defense` 850 = $850B against a real
~$850B; Sweden's SWF 195 ≈ its real AP-fund total. Every stored currency value is on the same scale.

**So there is no second, worse problem.** The spec flagged inconsistent units as the outcome that would
matter more; it is not the case. Seven currency fields on `EconomyState` (`GDP`, `Budget`, `TradeBalance`,
`Consumption`, `Investment`, `PotentialGDP`, `GovernmentDebt`), plus `SpendingLine.Amount`,
`SovereignWealthFund.TotalAssets` and all twelve `FiscalTurnReport` money fields — all billions.

⚠ **One genuine exception, and it is a derived value, not a stored one.** `DerivedStats.GdpPerCapita`
returns **thousands per person** (billions ÷ millions), which its own doc comment states. Any formatter
that assumes "currency ⇒ billions" would render it a million times too large. It is not currently
displayed anywhere — but A4's remaining work is precisely to surface it, so the formatter must handle a
per-stat unit rather than one global one.

### Finding 2 — the formatter is magnitude-aware and unit-blind

```csharp
if (magnitude >= 1_000f) return (value / 1_000f).ToString("0.#") + "k";
```

`FormatAxisValue(29000)` → `"29k"`. The arithmetic is right for a base unit of 1. The suffix ladder was
added after the `StatTile` "9,3" bug specifically so magnitude could not be lost — and it works, but it is
scaling from the wrong starting point.

### Finding 3 — 21 display sites, and the game states its units NOWHERE

| Path | Sites | Symptom |
|---|---|---|
| Via `FormatAxisValue` | 4 — graph axis labels ×3, published-graph empty state, "latest:" overlay, B2 chips (`Gdp`, `TradeBalance`) | `"29k"` for $29T |
| Raw `F1`, no suffix, no unit | 17 — GDP/Government Debt/Budget Balance tiles, Statistics GDP line, cumulative budget, trade balance, Revenue/Interest/Welfare/Tariff report rows, SWF panel (assets, gross debt, net position), interest-on-debt row, spending line amounts + implied change, world-map tooltip, Policy Web spending popup, turn log, `[DEBUG]` dump | `28999,3` bare |
| Preview panel | 1 — "Net Budget Impact" | bare |
| **Sparklines** | 0 | **unaffected** — they draw shape only, no text |

Not one site prints a currency symbol or a unit. `GameController.cs:4652` even computes a local named
`impliedDollarChange` and renders it as a bare number.

### Recommendation — ONE approach: a per-stat unit on a single money formatter

**Render `$29.0T`, not `29000` and not `29k`.** Concretely:

1. **One entry point**, `UiFormat.Money(float value, MoneyUnit unit)`, where `MoneyUnit` is `Billions`
   (everything stored) or `Thousands` (`GdpPerCapita`). It normalises to a base amount and then picks
   `$X.XT` / `$XXXB` / `$XXM` — so the suffix describes the real magnitude rather than the storage scale.
2. **The unit travels with the stat, not with the call site.** Extend the existing per-stat metadata
   (`PolicyWebRenderer.StatInfo` already carries `HigherIsBetter` per `StatNodeId`; `PolicyScreenStats`
   already switches per stat) with a unit. Graph axes then ask the series what it is instead of assuming.
3. **Retire `FormatAxisValue` for currency.** It stays for non-currency axes (rates, indices) where a bare
   `F1` is right. A currency axis that calls it becomes a compile-time impossibility if the axis takes a
   `MoneyUnit`-bearing descriptor rather than a raw float.

**Why this over the alternatives.** *Raw plus a unit label* ("29000 ($B)") keeps the reading burden on the
player on every screen and still shows a five-digit number where "$29T" would do. *Differing by context* —
tiles one way, axes another — is precisely the inconsistency that let this survive two fixes; the same
number would read differently on two screens a click apart.

**The property that matters most is that inconsistency becomes hard rather than merely discouraged.**
Two prior fixes were correct in isolation and failed because nothing prevented the next site from doing
something else. Carrying the unit on the stat, and making the currency path require it, is what stops a
22nd site from being added wrong.

**Scope: ~21 call sites, one new formatter, one metadata addition.** No simulation change whatsoever —
every stored value keeps its current number and unit.

### IMPLEMENTED 2026-08-02 — built exactly as recommended, plus four findings

`UiFormat.Money(value, MoneyUnit)` in `Assets/Scripts/UI/UiFormat.cs`, verified by
`MoneyFormatDiagnostic` (`Assets/Editor/`), **6 of 6 checks PASS** in real Unity 6000.5.6f1. USA GDP
`29000` now renders **`$29.0T`**; the seed table (`35960` → `$36.0T`, `1530` → `$1.53T`, `850` → `$850B`,
Sweden's SWF `195` → `$195B`) all render at true magnitude. No simulation file was touched.

**The unit is a required parameter, not a defaulted one**, on `GraphRenderer.Draw`/`DrawPublished`/
`DrawNeutral` and on `PieChartRenderer.Draw`. Every one of the ~19 call sites now states either a unit or
an explicit `moneyUnit: null`. That is the recommendation's "inconsistency becomes hard" property made
real: a new currency graph cannot be added silently, because it will not compile without answering the
question. `FormatAxisValue` survives for rates and indices and now carries a NON-CURRENCY ONLY warning.

**Finding 1 — the 21-site enumeration was low.** The real count is closer to 30. The fiscal report has
**eight** money rows plus a net line, not the four the investigation listed, and **two pie-chart legends**
(Spending Allocation, Theoretical Tax Revenue) print billions through `PieChartRenderer`'s
`valueFormat` and were missed entirely — that widget was never inspected because the investigation
searched display sites in `GameController`, and the chart's formatting lives one file away. *Lesson: an
enumeration built by grepping the file where the symptom was seen will miss the widgets it delegates to.*

**Finding 2 — the worst-stated unit in the game was `" units"`.** Three preview-panel figures (SWF
contribution, SWF returns, Net Budget Impact) called `FormatEstimate(value, " units")`, rendering
`+120.00 units` for $120 billion. Now `FormatMoneyEstimate`, rendering `+$120B (±$9.60B)`.

**Finding 3 — the formatter must pin its culture, and this machine proves why.** The first version used
the ambient locale and produced **`$29,0T`** on this sv-SE machine: a Swedish decimal comma against a US
dollar sign. Money now formats in `InvariantCulture`. Two reasons, and the second is the stronger: the
amounts are US dollars in every country's UI, so the separator should match the symbol; and a
locale-dependent formatter **cannot have a fixed-string regression test**, which is intolerable for the
one function that has now been wrong three times. Non-money numbers keep their existing locale
formatting — nothing else changed. Note the project's own history: the "9,3" incident was a
comma-decimal figure clipped in a narrow rect.

**Finding 4 — round-then-tier is a real bug, and only the diagnostic caught it.** The first version
rounded the dollar amount to three significant digits and then chose a tier, which requires dividing by
a negative power of ten. `1e-9` is not exactly representable, so `$999.97B` came back a hair below
`1e12`, missed the trillions tier and printed **`$1000B`**. Fixed by rounding in the already-scaled
domain and promoting a tier on carry. **This is the case for the diagnostic existing at all**: it is
invisible at every seed value, appears only within 0.003% of a tier boundary, and no visual review would
ever have found it.

**Two OnGUI-safety points, both from the sparkline lesson.**
- `CHECK 6` proves `Money` cannot throw on NaN/±Infinity/`float.MaxValue`. It renders nonsense
  (`$NaN`, `$InfinityT`) and **survives**, which is the correct bar for a function every draw call
  reaches — an exception inside `OnGUI` aborts the frame and blanks the screen.
- The tile call sites state `MoneyUnit.Billions` directly rather than
  `GetStatUnit(StatNodeId.Gdp).Value`. That `.Value` would throw inside `OnGUI` if the metadata entry
  were ever cleared, and billions is a fact about `EconomyState.GDP` (a field) rather than about the
  stat table. **Generic code that formats an arbitrary stat still asks the metadata** — the per-stat
  graph, the B2 chips, and both GDP graphs do — which is exactly the split the metadata exists for.

⚠ **NOT VISUALLY CONFIRMED.** Review item 3 stays open, and items 7/8 are now unblocked rather than
passed. Arithmetic being right is not the same as a graph reading well, which is the whole reason those
items were sequenced behind this one.

---

## P4 — the clipping class: audit, and why a shared helper is warranted (2026-08-02)

**Not patched.** Sixth and seventh recurrences of one class; the spec asked whether a shared helper ends
it. **It would, and the evidence is stronger than expected — the fix already exists and was applied to one
field of one widget.**

### Item 6 is literally the "9,3" bug again, in the same file, one field away

`PoliSimWidgets.StatTile` builds its label style as `new GUIStyle(GUI.skin.label)`, which **inherits
`wordWrap = true`**, and draws it into a fixed `12f * scale`-tall rect with a `MiddleLeft` anchor. A label
too wide for the tile wraps to two lines, and two lines vertically centred in a one-line box lose their
tops and bottoms — exactly "the text above is cut off" on `DEBT-TO-GDP` and similar.

The `StatTile` **value** field has carried the fix for this since the "9,3" incident, and its own comment
names the cause:

> *"this style inherits wordWrap from GUI.skin.label, and the anchor is a BOTTOM one - so a value too wide
> for the tile wrapped, and the bottom anchor rendered only the LAST wrapped line."*

The value got `wordWrap = false` plus shrink-to-fit. **The label, ten lines above it in the same method,
did not.** That is the whole argument for a helper: the correct fix was written, proven, commented — and
then not reused even within its own widget.

### Item 5 is the width variant

`DrawSubCategoryButton` uses `GUILayout.Button(label, style, ExpandWidth(true), MinHeight(...))`. Five
buttons share the Policy/Laws row (Labor Market, Crime & Justice, Economic Sectors, Policy Web, Trade); the
row has no width budget, so when natural widths exceed the container the last one — Trade — clips.

### The class, across seven known instances

Manufacturing sector label · World Map country names · TaxLine/WelfareProgram rows · Policy Web category
headers · Policy/Laws "Trade" · Budget tile labels · StatTile values (fixed).

**Every one is the same root shape: a text rect sized by a hardcoded constant instead of by measuring the
string in the style it will actually be drawn in.**

### Recommendation — `PoliSimWidgets.MeasuredLabel`

One helper that takes the rect, the text and **the style it will actually render in**, then:
- measures via `style.CalcSize`/`CalcHeight` against that style, not a smaller stand-in;
- sets `wordWrap = false` and **shrinks the font until it fits**, never truncating — the value field's
  existing approach, because shrinking makes a figure smaller but never *different*, which is the property
  that mattered for "9,3" and matters identically for a clipped label;
- recomputes per frame, since every style in this project rescales with the window;
- leaves an explicit margin so nothing sits flush against a boundary.

For the width variant, the same helper answers "how wide does this row actually need to be", which is what
`DrawSubCategoryButton`'s container needs in order to stop over-committing.

**Do not fix items 5 and 6 in place.** Six prior site-specific fixes have not ended this; a seventh will
not either. The helper plus a sweep of the seven known sites is the same amount of work with a different
outcome.

---

## `icon_stat_interestrate` — delivered weeks before anyone noticed (2026-08-02)

**Elias pointed out that the icon already existed**, in `Policy rate icon design.zip` at the project root.
`MISSING_PREREQUISITES.md` section E had it as *"REQUEST SENT, awaiting delivery"*. Both statements were
written from the same accurate information; nothing reconciled them.

**The import, done the established way.** `icon_stat_interestrate.png` (256×256 RGBA, verified by PNG
signature and header, white-on-alpha with coverage in line with an existing stat icon) to
`Assets/Resources/Art/UI/Stats/`, its 24×24 `currentColor` SVG source to `Stats/Source/`, both with
hand-written `.meta` files. The PNG's is **byte-identical to `icon_stat_gdp.png.meta` apart from its
guid**, which is what §3 of the request document prescribes, and the two new guids were checked against
all 296 `.meta` files in the project for collisions. Zip archived to `/AssetPackArchive/`.

**The brief was met**: a `%` — a slash with two dots — over a rising stepped line. Distinct from
`icon_stat_inflation`'s price tag, which mattered because the two sit adjacent on the Fiscal stat row and
one is a lever the player pulls while the other is an outcome they observe.

### The verification point: file-on-disk is not the property that matters

The import was confirmed by **loading through `Resources.Load`** — `IconLibrary.GetStat`, the exact path
the game uses — not by finding the file. A hand-written `.meta` is precisely the case where those two
answers can differ: the file can be present and correct while a malformed importer block leaves it
unloadable, and `IconLibrary`'s null-on-missing contract would then swallow the failure silently.

### `StatIconCoverageCheck`, and why a null-on-missing contract needs one

`Assets/Editor/StatIconCoverageCheck.cs` enumerates every `StatNodeId`, resolves its icon name through
`PolicyScreenStatsRenderer.GetIconName` and reports any that does not load. **18 of 18 present.**

The contract that made this icon's absence invisible is a *good* contract — a missing sprite draws
nothing rather than a stand-in that would imply the wrong stat, and it is what lets a new stat land ahead
of its art. But a failure mode designed to be silent in play needs somewhere it is loud, and there was
nowhere: the gap was eventually found by cross-referencing requested names against the disk **by hand**.
This is that cross-reference, made runnable. `GetIconName` went from private to public for it — the
standing rule from the sparkline crash applied to a lookup instead of to maths: where the only entry
point is a draw call, extract the part a batch-mode method can reach.

### Two lessons, both about how the gap survived rather than about the icon

- **A delivery is not self-announcing.** This is the *second* asset delivered and left unimported while a
  document reported it outstanding; `menu_pattern_tile.png` is the first and is still open. The register
  cannot detect a zip appearing at the project root. That is exactly why the unarchived zip is kept as a
  visible reminder — and why "awaiting delivery" is a status worth re-checking against the filesystem
  rather than trusting.
- **A correct derivation against the wrong list still misses.** The macro icon pack derived its stats
  from the 29 fields on `EconomyState` — code-grounded, and the right instinct. `InterestRate` is not one:
  it lives on `CurrencyZone`, because a rate belongs to a currency zone rather than to one country's
  economy (the Eurozone five share one). It was structurally invisible to that derivation while being a
  `StatNodeId`, a `PolicyNodeId` target, a Taylor Rule input, and the headline figure on two screens.
  **Enumerate the display enum, not the storage struct** — anything the UI can show needs an icon
  regardless of which type owns the field.

⚠ **This puts review item 10's pass in further doubt.** It was reviewed with the Interest Rate chip's
label sitting flush left where the missing icon would have been, so the row's spacing is not what was
confirmed. That is now the *second* caveat on item 10, alongside its sparklines never having rendered at
turn 0.

---

## `menu_pattern_tile.png` imported, and "awaiting delivery" made checkable (2026-08-02)

The second of the two delivered-but-unimported assets, and the one that had sat longest. Its zip was kept
unarchived at the project root **on purpose**, as a visible reminder — a convention that worked in the
sense that the reminder survived, and failed in the sense that nothing acted on it for weeks.

### The import, and the one deliberate departure from the icon convention

`Assets/Resources/Art/UI/Textures/menu_pattern_tile.png` — a new folder, with a hand-written folder
`.meta` alongside the texture's own. A seamless 256px dot lattice plus 135° hatch, white on transparent at
very low alpha (sampled 0, 6 and 21 of 255), so it reads as texture on a wash rather than as a pattern.

**Wrap Mode `Repeat`, where every icon in this project is `Clamp`.** Verified as the *only* difference
from `icon_stat_gdp.png.meta` besides the guid, by diffing the two files. The delivery's own README
specifies it, and it is load-bearing rather than cosmetic: the tile is drawn with
`GUI.DrawTextureWithTexCoords` at one repeat per 256px of screen, so `Clamp` would not throw or warn — it
would stretch the edge pixel across the whole display, which looks like a deliberate gradient rather than
a broken import. That is precisely the class of failure this project keeps finding, so
`StatIconCoverageCheck` now asserts `wrapMode == Repeat` rather than merely that the texture loads.

`IconLibrary.GetTexture` is a separate accessor from `Get`/`GetStat` for the same reason: these two
categories are *used* differently, not merely stored apart, and one accessor would invite an icon drawn
tiled or a tile imported clamped.

**Wired into `DrawCountrySelector`**, which drew no background at all before. The flat wash is drawn
whether or not the texture resolves, so a failed import degrades to a plain dark panel — a fine screen —
instead of taking the wash down with it.

### Rule 12, and why the check is on the zip rather than on the register

Two assets, two documents, same shape: `icon_stat_interestrate` recorded as *"REQUEST SENT, awaiting
delivery"* on the day it arrived, and `menu_pattern_tile.png` named as a gap in three places while sitting
in a zip twenty feet away. **Neither register was wrong when written.** A register is only as current as
its last edit; nothing watches the project root; a delivery does not announce itself. Both were closed
only because Elias said the file already existed — which is not a mechanism.

`DeliveredAssetCheck` compares **what was delivered against what exists**, which is the one comparison
that cannot go stale.

**Proven against the real defect before being trusted**, per the standing self-test rule: with the tile
temporarily withdrawn it reported `MISSING PoliSim GUI redesign.zip: menu_pattern_tile.png`, `18 of 19
asset entries present`, and exited 1. Restored: 0 gaps, 7 packs, 191 asset entries, exit 0. A check that
has never been observed failing is not evidence of anything.

**It also retired a hand-verified claim.** The archive README's table (84/84, 42/42, 24/24, 20/20, 2/2)
was produced by a manual reconciliation; the check reproduced every figure independently, and can now
re-derive them on demand. The reverse direction is covered too — an asset deleted *after* its pack was
archived reports as a REGRESSION rather than going unnoticed.

**The alias map is the part that decides whether this check survives.** The GUI redesign pack shipped
`icon_<area>` and the project imports area icons as `icon_area_<SystemArea>`, so 16 entries would report
as permanent false misses. They are mapped, and the mapping was verified against the files on disk rather
than taken from the archive README's "and so on". A check that cries wolf gets ignored, and an ignored
check is worse than none because it looks like coverage.

### The general form of the lesson

**A status describing the outside world is a cached value, and needs an expiry.** "Awaiting delivery",
"blocked on X", "not yet sourced" all describe something outside the repository, and every one of them
goes stale silently. Where the underlying fact is mechanically checkable — a file's existence, an asset
resolving, a package version — the check belongs in `Assets/Editor/` next to the others, and the document
should point at it rather than restate its answer.

---

## The debt floor removed — and the finding that it was never the rating thrash's cause (2026-08-02)

Elias's ruling implemented: the zero floor on `GovernmentDebt` is gone, debt may go negative, and a net
creditor earns nothing on its position. Validated against the full matrix at 100 and 500 turns, seed 777,
real Unity 6000.5.6f1, **with a like-for-like before/after run** — the change was stashed, the pre-change
matrix captured, then restored. Comparing two differently-seeded runs would have attributed RNG drift to
the change, which is verification-integrity instance 8's exact lesson.

### The numbers, by anomaly class

| Anomaly | BEFORE | AFTER | |
|---|---|---|---|
| `DebtToGdpRatio swung` | 6,225 | **2,507** | −60% |
| `CreditRating moved` | 1,416 | **1,394** | −1.6% |
| `Inflation swung` | 1,305 | 1,305 | unchanged |
| `Unemployment swung` | 116 | 116 | unchanged |
| `InterestRate swung` | 93 | 93 | unchanged |

**The three unchanged rows are as informative as the two that moved.** Byte-identical counts across
inflation, unemployment and interest rates are direct evidence the change touched the debt stock and
nothing else — a fiscal change leaking into the macro engine would have shown here first.

### THE FINDING: the floor caused the debt bimodality, and did NOT cause the rating thrash

This is the question the roadmap explicitly gated success on, and the answer is **no**.

- **Debt swings fell 60%**, and the 0.00% pinning is gone entirely. Sweden no longer bounces 0%↔44%; it
  crosses into net-creditor territory smoothly and stays there. The mechanism confirmation was right.
- **Rating moves fell 1.6%** — from 1,416 to 1,394. That is noise. **The rating thrash was never the
  floor's doing.**

Two independent measurements agree, which is why this is stated as a finding rather than a suspicion.
`DebtClampDiagnostic`'s year-over-year test — which isolates the DEBT term by evaluating the rating curve
with the deficit and growth terms omitted — reports **0 notch moves in 117 years for the USA, Sweden,
Germany and Poland**, 1 for Italy and 9 for France. The debt stock's own contribution to the rating is now
almost perfectly stable. Yet the matrix still logs 1,394 multi-notch moves.

**The residual is the deficit term**, exactly as the A1 write-up suspected when it recorded a settled
annual deficit ranging −135.5% to +170.8% of GDP. That is a separate defect, and the floor was hiding it
by making the debt stock's own noise so large that nobody could see past it. **Step C4's closure now waits
on THAT**, not on this.

### A second bound was needed, and it is a decision Elias has not made

Removing the floor outright produced an unbounded NEGATIVE runaway: **Sweden reached −615% of GDP** over
120 turns and France −359%. That is roadmap failure pattern 3 with the sign flipped, so the change could
not pass its own validation bar without a bound.

**The cause is structural.** `ApplyRevenueAndSpending` computes
`actualRevenue = theoretical * efficiency * fiscalReactionMultiplier + swfReturns` — SWF returns are added
*after* the multiplier, so the one stabiliser that would push back against a growing surplus cannot reach
them, while the fund compounds at a rate that structurally exceeds trend GDP growth. That is the same
asymmetry `MaxSwfToGdpPercent` already exists to bound, now visible from the other side.

**Bounded symmetrically at −300% of GDP**, reusing the existing constant. The number is not an arbitrary
mirror: Norway — the country this project used to calibrate SWF returns, and the real world's most extreme
net creditor — holds a fund worth roughly 2.5× GDP, so a net position near −250% is the empirical outer
edge and the bound sits just beyond it. With it, Sweden settles at −220% and France at −298%.

⚠ **France reaches the bound and sits near it, which is the shape the floor's own defect had.** France is
also the only country still showing meaningful year-over-year rating movement (9 of 117 years, up to 2
notches). It is the remaining problem case and should be treated as one.

⚠ **The bound is MY call, not Elias's.** His ruling covered removing the floor and the interest treatment;
it did not cover what happens at the negative extreme, because the runaway was not known until the floor
came off. Logged in Open Questions with the alternative — fixing the SWF-returns asymmetry directly rather
than bounding its symptom — which is the more principled fix and a much larger change.

### Why a net creditor earns nothing, and why that is not the whole answer

Without the guard, negative debt times a positive rate is negative interest, which flows through as a
REDUCTION in total spending — free money that grows with the surplus and compounds. That would have been a
worse defect than the one being fixed, and it would have looked like a well-run economy rather than a bug.

Zero is the conservative half of Elias's ruling. Interest earned on net assets is real, and modelling it is
deliberately deferred: the SWF already models the return on government assets, so paying a second return
on the same position here would double-count it.

---

## P4 — the label-clipping class, fixed with one helper (2026-08-02)

`PoliSimWidgets.MeasuredLabel` and `MeasuredWidth`. The audit's recommendation, built as specified:
measure in the style the text actually renders in, force `wordWrap` off, shrink rather than truncate,
recompute per frame, and reserve space for whatever shares the rect.

### The two reported sites

- **Item 6 — `StatTile`'s label.** The "9,3" bug one field away from its own fix, in the same method:
  `new GUIStyle(GUI.skin.label)` inherits `wordWrap = true`, drawn into a fixed `12f * scale` rect with a
  middle anchor, so `DEBT-TO-GDP` wrapped to two lines and both lost their tops and bottoms.
- **Item 5 — `DrawSubCategoryButton`'s width.** `ExpandWidth(true)` with no width budget: GUILayout
  divides the row evenly, so when natural widths exceed the container the longest labels lose their
  tails, and Trade — last in the row — is where it shows. **`MinWidth` is what `ExpandWidth` was
  missing**: "take a share" never said "and this much is the minimum I need".

### Two more found in the same widget, neither reported

Both are the same class in quieter form, and both were found by reading `StatTile` rather than by looking
for them:

- **The suffix ran off the tile.** The value was fitted to the *full* inner width, then the suffix drawn
  at `x + valueSize.x` — so a value wide enough to fill the tile pushed its own unit past the edge. The
  value now reserves the measured suffix width before shrinking.
- **`subLabel` was given `innerWidth` while starting at the delta pill's right edge**, so its rect
  overran the tile by exactly the pill's width.

### What was deliberately NOT touched, and why

Three of the seven audited sites — the sector name column, World Map country names, and Policy Web
category headers — **already measure correctly against the right style**, each from its own earlier fix.
They were left alone. Refactoring working, visually-confirmed code onto a new helper is churn with real
regression risk, and the helper's value is in being the answer for the *next* site rather than in
rewriting the sites that already got it right.

**That is the honest scope: two reported sites fixed, two latent ones found and fixed, five left
standing.** The class is not "closed" by fiat — it is closed when the next label added goes through the
helper instead of re-deriving the fix, and this is the first time there is something to go through.

---

## Verification-integrity instance — "appears nowhere" is FALSE for a restated level series (2026-08-02)

**The most important lesson from the API work, because it is the one where the rule was wrong rather than
the practitioner.**

Rule 5f-ter in the seed file distinguishes a REVISION from an ERROR by asking whether the disputed value
survives anywhere in the source: *"a revision leaves the old value in the historical record or adjacent
periods; an error leaves no trace because there was never anything to leave."*

**That reasoning holds only where the source preserves vintages.** OECD's GDP-per-hour series does not —
a revision **restates every year at once**, so the pre-revision figure vanishes from the live API
completely, in every year and every cross-section.

### What actually happened

France's productivity seed of **90.86** was searched across its full 2010–2024 series and a 41-country
2024 cross-section. It appeared nowhere. Every observation in that search was correct. **The conclusion
drawn from them was wrong** — 90.86 was the right figure on its own vintage, confirmed later against a
DBnomics snapshot (2026-04-07) reading **90.8595608458969**, alongside the USA's "~97" reading
**97.0466946503153**. Both original seeds were right; the live series had simply been restated, five of
six countries upward by 1.0–2.3%.

**A correct-but-superseded value produces exactly the fingerprint the rule assigns to an error — and the
more exhaustively you search the live source, the more confident you become of the wrong verdict.**

### What saved the figure was the GATE, not the TEST

The test pointed at ERROR. What prevented a correct `[VERIFIED]` figure being overwritten was rule
5f-bis's second condition — *the method must have reproduced an anchor in the same session* — which could
not be met on first contact with a new API. **The default held and the file won.**

That condition had been recorded a few hours earlier as a conservative inconvenience, with a note that it
"can never declare an error on first contact... conservative by design". **It is the only reason the data
survived.** A defensive rule justifies itself the first time it fires against a conclusion that looked
airtight, and this is that occasion.

### The standing form

> **"Appears nowhere in the source" distinguishes error from revision ONLY where the source preserves
> vintages. For a restated level series it distinguishes nothing. Check a third-party archive with
> per-snapshot retrieval dates — DBnomics mirrors OECD SDMX and is reachable where `sdmx.oecd.org` is
> not — BEFORE concluding a value never existed.**
>
> **And record the RETRIEVAL DATE beside every value from such a series, not just the reference year.**
> Without it, a superseded figure and a wrong figure are indistinguishable by any available test.

### Two hypotheses built on the bad verdict, both dead

Both were mine, and both were confident:

- *"90.86 is a pre-revision France 2023, and the direction is wrong for it"* — the vintage used to reason
  about direction was a **secondary aggregator's**, not OECD's. Against the real archived OECD vintage the
  direction reverses. **A secondary source's vintage cannot calibrate a primary source's revision.**
- *"90.86 may be Germany 2022 pre-revision"* — archived Germany 2022 is 92.4008. The 90.9 that made the
  story attractive was a one-decimal coincidence in an aggregator. Declining to log that as a
  verification-integrity instance on one decimal of secondary evidence was the correct call.

**The generalisable point beyond APIs:** an exhaustive search that returns nothing is evidence about the
*source as it exists now*, never about whether a value was ever true. Absence is only evidence of error
when the source is capable of remembering.
