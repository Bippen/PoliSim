# Step A — live-value leak audit

**Purpose:** the checklist the identical-trajectory proof is actually verifying. Written BEFORE the
published-series data model exists, deliberately: knowing every consumer of live state up front is what
prevents the leak from being introduced, rather than detecting it afterwards.

**The risk, restated precisely.** The player-facing UI will read a PUBLISHED series — lagged, and
sometimes a preliminary figure later revised. Every internal system must keep reading LIVE values. If a
published value reaches a simulation input, the model begins consuming its own stale output. That is a
slow feedback corruption, not a crash: per the directive it "may not surface for hundreds of turns."

---

## 1. The architectural decision that makes this safe

**Do not add published values to `EconomyState`.** Put the published series in a separate structure
owned by `Country` (e.g. `Country.Published`), leaving `EconomyState` exactly as it is — 29 fields, all
live.

This matters more than any review discipline. Every simulation call site below reads `country.State.X`.
If published values never appear on `EconomyState`, those call sites are **structurally incapable** of
reading a published value: a leak becomes a compile-time impossibility rather than something reviewers
must keep noticing. The alternative — a flag or parallel field on `EconomyState` — would leave every one
of the 55 reads below one typo away from silently consuming stale data.

**Consequence for Step A's diff:** if `EconomyState.cs` shows any change beyond comments, that alone is
evidence the design drifted, independent of what the trajectory comparison says.

---

## 2. Simulation-layer consumers of live state — ALL must keep reading `country.State.*`

Counted by references to `.State.` / `EconomyState` per file:

| File | Refs | Notes |
|---|---|---|
| `MacroSystem.cs` | 18 | Contains two of the three systems the directive names by name |
| `SimulationManager.cs` | 15 | Contains the third (fiscal reaction function) |
| `ParliamentSystem.cs` | 9 | Bill scoring reads live fiscal state |
| `TradeSystem.cs` | 3 | |
| `TaylorRule.cs` | 2 | Policy rate from live inflation/output gap |
| `EurozoneRateSystem.cs` | 2 | |
| `CurrencySystem.cs` | 2 | |
| `ForeignPolicySystem.cs` | 1 | |
| `EventSystem.cs` | 1 | |
| `ElectionSystem.cs` | 1 | |
| `CabinetSystem.cs` | 1 | |

**55 live reads across 11 files.** Every one stays live. None may be redirected to a published value.

## 3. The three systems the directive names — exact reads, verified in code

**Okun's Law** — `MacroSystem.ApplyOkunsLaw` (line 163):
```
state.Unemployment, country.PotentialGrowthRate, country.NaturalUnemploymentRate
```

**Phillips Curve** — `MacroSystem.ApplyPhillipsCurveInflation` (line 223):
```
state.Unemployment, state.InflationExpectations, country.NaturalUnemploymentRate
```

**Fiscal Reaction Function** — `SimulationManager.GetFiscalReactionMultiplier` (line 142):
```
country.State.DebtToGdpRatio, country.ComfortableDebtToGdpPercent
```

All three read through `country.State` / `country.*`. Under the section-1 design none of them can reach
a published value without an explicit new reference that would be obvious in review.

Note that `Unemployment` and `DebtToGdpRatio` are among the stats that WILL gain published series and
are also among the most-published in the real release calendar — so these three are simultaneously the
highest-risk and the easiest to get wrong.

---

## 4. The proof

**Baseline captured at commit `6a53878`** (pre-change HEAD), 100-turn `baseline` scenario, real Unity
6000.5.6f1 — stored at `baselines/stepA_baseline_6a53878.log`. Captured BEFORE any Step A code, because
once the code changes the untainted reference cannot be reconstructed.

**Acceptance for Step A:** the same scenario after Step A must produce an identical trajectory. Not
"similar", not "within noise" — Step A adds no simulation inputs, so any difference at all is a leak or
an accident, and both need explaining before the step can be called done.

Practical comparison: diff the per-turn logged lines. The anomaly COUNT alone is insufficient — two runs
can share a count while differing in values, which is exactly the kind of near-miss this project has been
caught by before.
