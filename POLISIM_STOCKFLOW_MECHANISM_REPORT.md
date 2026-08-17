# The Stock-versus-Flow Mechanism Report (2026-08-17)

**Report only — derive, evaluate, recommend, stop.** Nothing is built in this pass; the ruling is
Elias's, made on this document. Entry gate: Round 4 closed at `a9fb8b7`; the namespace was claimed
by the FRF sweep's close ("the bond/debt-instrument design space is unclaimed... and the report
should CLAIM that namespace"). Everything below is derived from the code and trajectories at HEAD,
with the record's claims re-verified rather than inherited.

---

## 0. The anchors, re-derived at HEAD

**The signature, recomputed from the `post_r4_5` trajectory CSVs** (the ratio is a derived
property, so it was computed from the dumped `GovernmentDebt`/`GDP` pairs — not quoted from any
earlier summary):

| | t100 | t200 | t1000 (s777) | t1000 (s424242) |
|---|---|---|---|---|
| USA | 140.1 | 139.8 | **155.9** | 155.1 |
| Germany | 38.0 | 38.9 | **80.1** | 80.1 |
| Italy | 114.6 | 116.0 | **165.9** | 167.7 |
| Poland | 28.0 | 28.3 | **46.0** | 46.0 |
| Sweden | 4.5 | 6.2 | 10.6 | 9.3 |
| France | 94.1 | 88.9 | 108.9 | 111.0 |

Matches the sweep record's four-climbers-two-settled signature exactly (USA 155.9, Germany 80.1,
Italy 165.9, Poland 46.0 at s777 — the recorded numbers to the decimal, as the R4 byte-identical
matrix implies they must). Both seeds agree on the shape that matters: **the climb happens after
the ruled 100–200 window** — USA is flat at ~140 through t200 and gains its 16 points later;
Germany doubles after t200; Italy adds 50 of its 51 points after t200. The sweep verdicts are
taken from the record and not re-run (report-only pass): no (S, bounds) pair converges within the
revenue-capacity wall (S≥6 limit-cycles — France 32 points in 50 turns at S=10; the loosened
floor [0.8, 1.5] spirals five countries to −300…−1010% on compounding surpluses — the floor is
load-bearing). The instrumented driver stands: the FRF moves freely and leans correctly (Germany
→ ~1.27, Italy ~1.19–1.45 against the 1.5 wall) while interest reaches 25.7% (Germany) and 45.7%
(Italy) of ALL spending. **The mechanism limit is real: the flow multiplier is working and being
outrun by the stock's compounding.**

---

## 1. The current mechanics, derived precisely

### 1a. Interest — the whole stock reprices DAILY at today's rate

`GetInterestOnDebt` (SimulationManager:2773): `stock × (baseRate + premium × sensitivity)/100`,
where `baseRate` is `CurrencyZone.InterestRate` live (or the override, below) and the premium is
`0.02 × max(0, ratio − 60)` capped at 5 points. Since Phase 3 this is accrued **daily against the
live stock at the live rate** (`AccrueDailyFiscalFlows`:2882) — the implicit claim is not merely
that all debt reprices each period, but that the ENTIRE stock reprices EVERY DAY at the current
spot rate plus the current premium. No sovereign's debt works this way; it is the strongest form
of the assumption maturity structure attacks.

**The USA carve-out is already a frozen maturity model, accepted once.** The Reserve-Currency
Debt Interest Treatment (CLAUDE.md) found the spot-rate-on-whole-stock formula overstated USA
interest by 65–80% and fixed it with `BaseDebtInterestRateOverride = 3.3` — explicitly reasoned
as *"most federal debt is longer-duration bonds issued across many prior years at a blended rate
that doesn't track today's policy rate 1:1."* That is the maturity-structure argument, verbatim,
applied to one country as a hard-coded constant (an infinite-lag blended rate), validated against
the real ~$1.0–1.1T net-interest figure. The other five still reprice daily at spot. The record
also warns (2026-08-11): *"a debt-responsive rate is NOT an available fix — it is already there
and is half the problem"* — the premium→interest→debt→premium loop is the positive feedback, and
its speed is set by this instant repricing.

### 1b. Nominal versus real — the model is REAL, and it charges NOMINAL rates on a real stock

Derived, not assumed: GDP is the identity `C + I + G + NX` reverting toward `PotentialGDP`, which
grows at `PotentialGrowthRate` (MacroSystem:62–72). **No price level scales any dollar quantity
anywhere** — Inflation exists as a rate (driving confidence, approval, wages, the Taylor rule)
but never multiplies GDP, revenue, spending, or the debt stock. The model's dollars are
constant-price units; all growth is volume growth. Meanwhile the policy rate lives in a NOMINAL
convention, confirmed at source: `TaylorRule.GetSuggestedInterestRate = NeutralRealRate +
Inflation + gap terms` (TaylorRule:48) — the textbook Fisher form. The zone rates the NPC
central banks set chase inflation into that nominal space (measured at HEAD: at t150 the
Eurozone rate is 5.51% against 1.9–3.9% inflation; by t1000 it is pinned at
`CurrencySystem.MaxInterestRate = 15` against 7.7–9.6% inflation).

**Consequence, stated as the standard debt-dynamics identity.** Reality:
`Δb = (r_nominal − g_real − π)·b − primary_balance/GDP`. This model:
`Δb = (r_nominal − g_real)·b − pb`. **The −π·b term — inflation eroding the ratio because debt
is nominal while nominal GDP grows with prices — does not exist here**, because the stock update
(`GovernmentDebt −= budgetBalance`, SimulationManager:3059, verified as the ONLY writer) never
sees inflation. The missing term is a restoring force proportional to the ratio itself — largest
exactly where the divergence lives. Sized at HEAD: in the ruled window (π ≈ 1.9–3.9%), Italy at
b ≈ 115 forgoes **−2.2 to −4.4 ratio-points per year** of erosion; Germany at 38 forgoes ~1.5.
The measured terminal divergence slopes the sweep judged were **+0.014…+0.037 points per turn**
— the missing term is one to two orders of magnitude larger than the divergence it would oppose.
So inflation erosion is not merely "a candidate": item 1's derivation says the model omits a
textbook term whose size dwarfs the defect. (It also says the erosion candidate does NOT require
building nominal accounting — see §2b: the term can be added at the stock update, leaving every
validated flow untouched.)

### 1c. The primary balance — no field exists, and the rejection stands

`budgetBalance = actualRevenue − totalSpending`, with `interestOnDebt` one of six spending terms
(SimulationManager:3027–3030). No primary-balance quantity exists anywhere. The record's
rejection is re-verified and re-affirmed: Italy's headline −3.9 with 45.7% of spending on
interest implies a primary surplus of roughly **+75** in the same units — Italy already does
everything a primary-surplus rule would mandate, and diverges anyway. **The confrontation the
directive required: what would a rule do that Italy's surplus doesn't? Nothing — a rule that
mandates what is already happening changes nothing, and a rule that mandates MORE is the FRF
with a higher cap, which the sweep proved cannot converge within the revenue-capacity wall and
which the wall (enforced by a throw in `SetFiscalReactionPairForSweep`) exists to forbid. The
candidate dies here.**

### 1d. The Round 4 footprint — inputs-only verified per candidate, with the searches

- **Productivity**: consumers at HEAD are its seed lines, its own model, StatHistory, display,
  and the checks (search: `grep -rn "\.Productivity"` over Assets/) — it reads
  `PotentialGrowthRate` and nothing reads it back. **Zero contact with any candidate.**
- **C1 housing**: reads `CurrencyZone.InterestRate` against the epoch anchor, writes only its own
  three stats; no fiscal quantity consumes them (search: housing identifiers in Simulation/ hit
  only their own model, publication wiring, and the daily call site). A maturity mechanism that
  adds an *effective* rate would NOT touch C1 — housing deliberately reads the live policy rate
  (its design feature), and nothing here changes that rate. **Zero contact.**
- **R4-4 decisions — a FINDING the derivation surfaced (F1):** every interrupt-layer
  `BudgetImpact` (`CabinetSystem.ApplyDecisionOption`, `ForeignPolicySystem.ApplyMeetingOption`,
  and EventSystem's equivalent) writes `state.Budget` — which is a cumulative DISPLAY
  accumulator (`state.Budget += budgetBalance`, line 3030). **The debt stock moves only by the
  local `budgetBalance` flow (line 3059), so no event, meeting, or cabinet decision has ever
  touched the debt path.** The entire "budget impact" channel is cosmetic with respect to fiscal
  dynamics. This changes nothing for the candidates (it means the interrupt layer PROVABLY
  cannot perturb them), but it is a real gap between what the game says ("Bank it against the
  debt": +200) and what it does (debt unchanged), recorded here for its own ruling — not fixed
  inside a fiscal derivation.

---

## 2. The candidates, each as a claim about the world

### 2a. Maturity structure — "only new issuance takes today's rate"

**The claim:** sovereign debt is issued in tranches; the stock's cost is a blended coupon that
follows the spot rate only at the pace of rollover. TRUE of the world, and already accepted once
in this codebase (the USA carve-out, §1a).

**Two forms evaluated:**
- **Full tranches** (a list of issuance cohorts per country): the honest microstructure, and
  rejected on cost — per-tranche state multiplies save/load surface (a new collection per
  country riding the round-trip diagnostic), turns `GetInterestOnDebt` into a loop over cohorts,
  and adds calibration per cohort for a benefit the aggregate form captures.
- **Average-maturity effective rate** (recommended form if this candidate is ruled in): ONE new
  `Country` float — `EffectiveDebtInterestRate` — reverting toward the live
  `spot + premium × sensitivity` at speed `1/AverageMaturityYears` per year (the taxonomy's
  PerDayReversion shape, daily-native from day one). `GetInterestOnDebt` charges the effective
  rate instead of the live one. The USA's frozen 3.3 override RETIRES into the initial value of
  its lagged state — the carve-out generalized rather than duplicated (`RiskPremiumSensitivity`
  stays; that models who holds the debt, not how fast it reprices). Pre-mechanism saves take the
  R4-3 sentinel pattern (−1 → initialize from the rate the old code would have charged; never
  fabricate an epoch).

**Effect on the signature (reasoned, not run):** at constant rates the lag converges to spot —
steady-state cost unchanged; what changes is LOOP GAIN: a premium/rate rise reaches the interest
bill at ~1/6th per year instead of instantly, damping the doom loop's speed and the S≥6
limit-cycle mechanism (which is rate-feedback overshoot). In the ruled window, where the
Eurozone rate held ~5.51 flat, it changes little — consistent with the window already passing.
**It is a damper, not a restoring force**: it slows divergence and cannot by itself make the
ratio dynamics restoring. Floor/spiral interaction: none — it never touches the FRF pair, and
the net-creditor guard is upstream of the rate entirely. Blast radius: `GetInterestOnDebt` and
preview (shared path); the rating/FRF/premium all read the RATIO, not the rate composition.
Calibration: one constant (average maturity ≈ 6 years, real-world figures 4–8.5 across the six;
global first, per-country later). Equivalence: a rate-reversion interacting with daily accrual
against a moving stock — the Phase-3 class, needing a small honest drift budget, stated.

### 2b. Inflation erosion — "debt is nominal; inflation shrinks the ratio"

**Contingent on §1b's answer, which came back: the model is real, the term is missing, and it
does NOT require nominal accounting.** Two forms:

- **Full nominal accounting** (price level; nominal GDP, debt, flows; real displays): the
  complete fix and REJECTED on cost — it re-bases every dollar quantity in a model whose every
  validated number, seed, baseline and capture is in today's single-unit convention; the blast
  radius is the entire fiscal record. The directive anticipated exactly this: the accounting
  would have to be built before the mechanism.
- **The stock-side erosion term** (recommended form — surfaced by the derivation itself): add
  the missing identity term at the one line where the stock moves:
  `GovernmentDebt −= (Inflation/100) × GovernmentDebt × dayFraction`, applied when debt > 0,
  alongside the existing `−budgetBalance`. This says, in one line, "the stock is nominal in a
  real-unit ledger": every validated FLOW (interest bill, revenue, budgetBalance, the Budget
  accumulator, the USA's $1.2T-anchored interest) is UNTOUCHED — only the stock's drift gains
  the −π·b term reality has. Deflation (π < 0) grows the real stock through the same line,
  correctly signed, no special case. Net-creditor scope: positive debt only, mirroring the
  2026-08-02 interest guard's conservatism (erosion of a creditor's real position is real but
  deferred, exactly like interest income was — no free money in either direction).

**Effect on the signature (reasoned):** a restoring term proportional to b — in the window,
−2.2…−4.4 pts/year for Italy, ~−1.5 for Germany, ~−3 to −5 for the USA, versus measured
divergence slopes of +0.014…+0.037/turn. **This is the only candidate that changes the
compounding term's sign structure rather than its speed** — precisely the "flow cannot outrun
stock compounding" limit answered at the stock. Floor/spiral: the FRF pair untouched; the term
vanishes at b ≤ 0 so it cannot deepen the surplus spiral; and it SHRINKS as b falls (self-
limiting, unlike the SWF-returns compounding that caused the floor-arm catastrophe). Blast
radius: one line inside `ApplyRevenueAndSpending` (shared by play, preview, and the validation
hook — preview correct automatically); zero new state; zero save/load surface; zero new
constants. **Calibration burden: none — π is state, the coefficient is 1 by identity.**
Consequence to rule on: inflation becomes fiscally rewarding (the real "inflate the debt away"
escape valve), which the model already prices politically — approval's misery index, confidence,
real-wage erosion, and the Fed/Taylor layer all resist it. That is the world's own tradeoff, now
present instead of absent.

### 2c. Primary-surplus rules — dead at §1c

Dies on the record's own evidence, re-verified: Italy's +75 implied primary surplus diverges
anyway; a rule adds nothing below the wall and is forbidden above it. Not carried to §3.

### 2d. Surfaced and named, per the directive — including the rejected

- **The stock-side erosion form itself** (§2b's second form) — the derivation's main yield.
- **A premium/rate lag alone** (lag only `GetDebtRiskPremium`'s output): strictly dominated by
  the effective-rate form of maturity (same state cost, half the realism), rejected.
- **Restructuring/default events** (a debt crisis interrupt): a game-design feature, not a
  restoring force — it changes what happens AFTER divergence, not whether divergence occurs.
  Out of this report's namespace; noted for the events layer someday.
- **F2, named but needing no ruling:** at t1000 the Eurozone rate is pinned at
  `CurrencySystem.MaxInterestRate = 15` — the rate cap is already bounding the doom loop's
  upper end (and is itself a nominal-on-real artifact: 15% nominal at 9% inflation is a ~6%
  real rate the model books at 15).

---

## 3. Recommendation and the ruling package

**Recommendation: the stock-side inflation-erosion term (§2b), alone, as this pass's mechanism —
with the maturity effective-rate lag (§2a) ruled as a SEPARATE later item rather than bundled.**

**Why it beats the others.** It is the only candidate that adds a restoring force rather than a
damper (maturity) or a redundancy (primary rules); it answers the sweep's mechanism limit at the
stock, where the limit lives; its magnitude — derived, not tuned — is 50–100× the divergence
slope in the ruled window; it costs one guarded line, no state, no constants, no save surface;
and it is not a gameplay invention but the textbook identity term the model turns out to omit.
Maturity is real and worth having, but it is a second-order damper here, it carries state and
calibration, and bundling both would blur attribution across one baseline — the record's own
one-change-per-baseline discipline says sequence them (erosion first because it is the restoring
force; maturity later against erosion's fresh baseline, where its damping of the S≥6-class
overshoot can be measured on its own).

**The one-sentence claim to accept or reject:** *Government debt is a nominal quantity in a
constant-price ledger, so inflation erodes the real stock at π per year while deflation grows
it — the standard debt-dynamics term this model currently omits.*

**Validation plan, in this project's terms:** fresh `pre_mechanism_<hash>` baseline (both seeds,
three horizons) before the line is written; equivalence extension with stated enumeration (the
daily slice of −π·b against the turn form under a driven inflation path — the Phase-3 class,
honest drift budget stated, since π moves daily while the turn form samples it); the trajectory
matrix run EXPECTING non-identical (the first intended behavior change since the daily
migration) and judged on both diff columns; **the signature re-measured at the 100–200 window
with 1000 as shape check**; the floor arm's five countries checked for non-reintroduction of
the surplus spiral (the b ≤ 0 guard makes this structural, verified anyway); save/load 12/12
(no new fields); captures unaffected (no UI change; the debt figures move, the screens don't);
anomaly-count comparisons acknowledged non-comparable across this discontinuity (the fourth
baseline break, recorded as the first three were). Real Unity only — the harness is gone and
stays gone.

**What "working" means, concretely (the bar, per the standing scoping):** the restoring term
EXISTS in the dynamics (structural — verifiable by inspection and by the equivalence row) and
BINDS at the ruled window — measured as: each of the four climbers' 100–200 trajectory slope
reduced against the fresh pre-mechanism baseline by an amount consistent with −π·b at the
window's own inflation, with no new anomaly types and no floor-arm regression. **NOT that turn
1000 converges** — t1000 remains a shape check, and any settling observed there is reported as
a waypoint, never as equilibrium.

### RULINGS NEEDED

- **R1 — the mechanism:** the stock-side inflation-erosion term, as specified in §2b.
  Accept/reject the one-sentence world-claim above.
- **R2 — the accounting decision item 1 forces:** the model's dollars are declared
  constant-price (real) units with a nominal debt stock bridged by R1's term — the cheap form —
  versus full nominal accounting (priced in §2b, recommended against). This ruling is what R1's
  line ASSERTS; they are one decision viewed twice, separated so the accounting is chosen
  knowingly rather than smuggled.
- **R3 — creditor-side scope:** erosion applies to positive debt only (recommended, mirroring
  the net-creditor interest ruling), or symmetrically to net-creditor positions (not
  recommended; interacts with the SWF's already-modeled real returns).
- **R4 — maturity structure:** queue the effective-rate lag as its own later item against
  erosion's fresh baseline (recommended), rule it in now alongside (not recommended — blurred
  attribution), or reject it outright.
- **R5 — finding F1:** the interrupt layer's `BudgetImpact` never reaches the debt stock
  (display-accumulator only, all three systems). Rule: intended display-only behavior, or a
  defect for its own small pass (recommended: its own pass — if fixed, option text and fiscal
  reality align at event scale; either way, not inside this mechanism's change).

**Stop.** Nothing builds until R1–R3 land; R4–R5 can land later without blocking R1.
