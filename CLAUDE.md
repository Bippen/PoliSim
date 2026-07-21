# PoliSim

## Overview
PoliSim is a turn-based political/economic simulation game built in Unity (C#). The player governs a country — starting with six real-world-seeded countries (USA, Sweden, Germany, France, Italy, Poland) — and makes policy decisions (taxation, spending, interest rates, regulation, etc.) each turn. The core of the economy (GDP, unemployment, inflation) is driven by named macroeconomic theory rather than tuned-by-feel curves; a handful of surrounding mechanics (approval rating, currency strength, trade/tariff dampening) are still intentionally simple heuristics. The player must balance economic performance against public approval to stay in power.

This is an early scaffold: core data model and a minimal simulation loop, not final game content or UI.

## Genre & Scope
- Turn-based (not real-time). One "turn" = one simulated period (e.g. a quarter or year — exact cadence still TBD).
- Multiple playable/simulated countries: USA, Sweden, Germany, France, Italy, Poland. `WorldFactory.CreateDefault()` seeds the figures the user specified — policy rates, inflation, and USA/Poland unemployment and USA/Eurozone/Sweden/Poland potential growth — to real mid-2026 data; NAIRU, unspecified unemployment rates, government-spending shares, and starting GDP levels are stylized, directionally-realistic estimates, not researched figures (see comments in `WorldFactory.cs`).
- Germany, France, and Italy form a shared Eurozone (one `CurrencyZone` instance, one interest rate for all three). Sweden and Poland are EU members but keep independent currencies/interest rates, matching how the real EU/Eurozone relationship works. The USA is fully independent (own currency zone, not an EU member).
- The EU is modeled as a `TradeBloc`: near-zero internal tariffs between its five members (Germany, France, Italy, Sweden, Poland), one common external tariff rate applied by all of them to non-members (i.e. the USA).
- Focus is on systemic feedback between policy levers and simulation state — domestic (tax/spending/approval) and now international (tariffs, bloc membership, bilateral trade) — not on map/geography, diplomacy, or military systems (those may come later).

## Tech Stack
- Engine: Unity
- Language: C#
- No external simulation/economics libraries — game logic is hand-rolled in plain C# classes so it stays easy to reason about and unit test outside of Unity where possible.

## Project Structure
```
Assets/
  Scripts/
    Simulation/   -- simulation loop, turn advancement, macro theory, feedback rules
                     (SimulationManager, MacroSystem, TaylorRule, TradeSystem, CurrencySystem)
    Data/         -- core state/data classes (EconomyState, Country, CurrencyZone, TradeBloc, TradePartner, World, WorldFactory)
```
As the project grows, expect additional folders such as `Scripts/UI`, `Scripts/Policies`, `Scripts/Events` — keep simulation logic (state + rules) decoupled from Unity `MonoBehaviour`/UI concerns where practical, so the simulation can be tested independently of the engine.

## Core Concepts
- **EconomyState**: plain C# data class holding one country's economic/political indicators for a turn — GDP, inflation, unemployment, approval rating, budget, tax rate, trade balance, currency strength, plus the macro-theory fields: `Consumption`, `Investment`, `PotentialGDP`, `InflationExpectations`, `ConsumerConfidence`, `BusinessConfidence`.
- **Country**: identity (`CountryId` enum) + `EconomyState` + the `CurrencyZone` it belongs to + its list of `TradePartner` links + structural (non-turn-mutated) constants: `BaseTariffRate` (used only when it isn't in a trade bloc), `NaturalUnemploymentRate` (NAIRU), `PotentialGrowthRate` (trend GDP growth, %/turn), and `GovernmentSpendingRate` (baseline government consumption as % of GDP).
- **CurrencyZone**: a shared, settable interest rate. Countries that use the same currency (e.g. Germany/France/Italy) reference the *same* `CurrencyZone` instance, so a rate change affects all of them at once; independent-currency countries (USA, Sweden, Poland) each get their own instance and set their rate independently.
- **TradeBloc**: a group of member countries (identified by `CountryId`) with a shared internal tariff rate (near zero) between members and one common external tariff rate applied by every member to non-member imports. The EU bloc is built from Germany, France, Italy, Sweden, and Poland.
- **TradePartner**: one bilateral trade relationship from a country's point of view — static export/import volumes (not a full market simulation) that tariffs and currency strength act on each turn.
- **World**: the top-level container — all `Country` instances plus all `TradeBloc` instances. `WorldFactory.CreateDefault()` builds the standard six-country scenario with a small hand-authored trade network.
- **PolicyDecision**: per-country turn inputs — tax rate change, `InterestRateChange` (summed across countries sharing a `CurrencyZone` into one shared-zone change), and `GovernmentSpending` — a *discretionary delta* layered on top of the country's baseline `GovernmentSpendingRate`, not the total spending figure.
- **SimulationManager**: orchestrates turn order only — the macro theory itself lives in `MacroSystem`/`TaylorRule`. Per turn: `CurrencySystem` applies interest rate changes and drifts currency strength, `TradeSystem` resolves trade/tariffs (setting `TradeBalance`), then each country's domestic policy runs — tax, fiscal spending/budget, `MacroSystem`'s national accounts identity (GDP), Okun's Law (unemployment), the Phillips Curve (inflation), and approval.
- **MacroSystem**: the macroeconomic theory — see "Economic Theory" below.
- **TaylorRule**: reference-only suggested interest rate (see "Economic Theory" below) — never applied automatically; intended for a future UI hint or an AI-controlled country's decision logic.
- **CurrencySystem**: applies summed interest rate changes per `CurrencyZone`; for countries that don't share their `CurrencyZone` with anyone else, drifts `EconomyState.CurrencyStrength` (index, 100 = neutral) toward a target based on how their interest rate compares to the average rate among their trade partners — relatively higher rate pulls strength up, relatively lower pulls it down. Shared-currency countries (Eurozone) skip this, since there's no single national currency to strengthen or weaken. This heuristic (and the export-competitiveness effect it feeds into `TradeSystem`) is still a simplified placeholder, not modeled on a specific theory.
- **TradeSystem**: looks up the applicable tariff rate for an importer/exporter pair (shared-bloc internal rate → importer's bloc external rate → importer's own `BaseTariffRate`, in that precedence order); for non-shared-currency exporters, also scales effective exports by a currency-strength factor (stronger than neutral dampens exports, weaker boosts them; shared-currency exporters always get a neutral factor). Sets `TradeBalance` (the NX term `MacroSystem` reads for GDP) and tariff revenue (added to the budget) — it does **not** touch GDP directly anymore.
- **Approval rating**: still a simple, tunable heuristic (not modeled on a named theory) — raising taxes, high unemployment, and high inflation all reduce approval; it recovers slowly otherwise.

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
  the country's baseline `GovernmentSpendingRate` share of GDP plus the turn's discretionary
  `PolicyDecision.GovernmentSpending`. NetExports is `TradeBalance`, already computed by
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
nothing feeds them back yet (a natural next step). Approval rating, currency strength, and
trade/tariff dampening remain simple, un-theorized heuristics. Still no UI, no save/load, no full
market simulation (trade volumes are static inputs, not supply/demand-driven), and every constant is
a starting-point placeholder meant to be tuned by playtesting.
