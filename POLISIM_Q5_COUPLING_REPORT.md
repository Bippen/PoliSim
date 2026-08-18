# Q5 — the cyclical pair: derivation, and the fork (2026-08-18)

**Nothing is built.** The derivation forked, as the directive anticipated, so this stops at the
report. Four for four: Q3's premise was wrong, Q1's form was decided by measuring a flat Gini,
Q2's shape was forced by confidence having no reversion — and Q5's premise is wrong in a
specific, structural way stated in §2.

---

## 1. The audit — productivity's writers at HEAD, and what the pipe actually is

| what | where | reads / writes |
|---|---|---|
| `ApplySectorGrowthEffect` (the finalizer) | MacroSystem:1752 | writes **both** `ProductivityTrendGrowthRate` **and** `PotentialGrowthRate`, to the *same* clamped value |
| the ledger | infra spending + infra condition + sector output gap | summed, clamped ±1.0 (`MaxTotalPotentialGrowthAdjustment`), then `Clamp(Base + total, 0, 8)` |
| `ApplyProductivity` | MacroSystem:1046 | compounds `state.Productivity` by `1.0 × ProductivityTrendGrowth`, clamp ±10 pp/turn |
| `ApplyRealWageIndex` | MacroSystem:869 | reads `ProductivityTrendGrowth` at 1:1, plus its own tightness and inflation-surprise terms |
| Q2's factor | MacroSystem:2164 | reads `RealWageGrowthPerTurnPercent − 1.0 × ProductivityTrendGrowth` — i.e. **exactly the wage equation's cyclical terms** |

**`state.Productivity` is consumed by NOTHING economic** — the UI's Society line and
`StatHistory`, and that is the complete reader set (verified project-wide). This matters more
than it looks: it means a cyclical term landing *only* on the productivity stat is, by
construction, a display feature.

**Rule 11, fired on each candidate write target:**
- **`ProductivityTrendGrowthRate` / `PotentialGrowthRate`** — ceiling is the ±1.0 all-sources
  adjustment clamp plus `[0, 8]`. A cyclical contributor here would fold into a ceiling built
  for *structural* adjustments, and would be clipped asymmetrically at the `0` floor for any
  country whose base trend is low (Eurozone's 0.3 sits 0.3 from the floor).
- **`state.Productivity`'s own growth** — ceiling is `±10 pp/turn` and `MinProductivityLevel`.
  Room is ample against the measured driver (§3).
- **wage growth** — ceiling is `±10 pp/turn`, shared with the existing trend + tightness +
  surprise terms. Combined exposure computed in §4.

---

## 2. THE FORK — and the directive's own hypothesis is structurally disqualified

The directive asks whether the cyclical terms enter **the ledger** or sit **beside it** as a
deviation, and says a ledger contributor "would make a recession permanently lower a country's
potential". **In this codebase that is not a risk — it is an identity.** The finalizer's last
two lines are:

```csharp
country.ProductivityTrendGrowthRate = Mathf.Clamp(country.BasePotentialGrowthRate + totalAdjustment, 0f, MaxPotentialGrowthRate);
country.PotentialGrowthRate = country.ProductivityTrendGrowthRate;
```

Potential *is* trend productivity, assigned. So a cyclical contributor in the ledger is a
cyclical `PotentialGrowthRate` — which then feeds Okun's own growth gap and the identity's
attractor. **Ledger entry is rejected on structure, not on taste.**

That settles half the fork and opens the real one. A deviation needs a home, and the three
candidates make *different claims*, all of them coherent:

| | what it says | trajectory consequence | new state |
|---|---|---|---|
| **A — stat-only** | measured productivity is procyclical; nothing else changes | **byte-identical except `Productivity`** | none |
| **B1 — additive to wages** | productivity's cycle is a *further* channel into pay, on top of bargaining | a real force; **creates a closed loop** (§5) | none |
| **B2 — carve-out** | part of the wage tightness term *already was* productivity's cycle; re-root it | **byte-identical except `Productivity`** — same sum, different causation | none |

**B2 is Q3's move applied again**: wages read `trend + h×gap` while the direct tightness term
becomes `(0.3 − h)×gap`, so the sum is unchanged, Q2's gap is unchanged, GDP is unchanged, and
the only thing that moves is the productivity stat — which becomes genuinely cyclical, and
*correctly attributed*. It is the honest option if the claim is "we mis-attributed a channel".
It delivers **no new dynamics**, which is worth saying plainly because the spine promised Q5
would be where "the deferred trajectory movement arrives".

**A and B2 have the same bar (byte-identical, re-rooting template) and different claims.
B1 is the only one that is a FORCE.** Hence the ruling.

⚠ **R-Q3b's 1:1 pipe needs amending under A or B1, and not under B2.** Under A the pipe stays
literally true but the productivity *stat* stops equalling the trend path. Under B1 wages read
trend + cycle while potential reads trend only — which is a refinement of the pipe, exactly as
the directive anticipated, and it must be written down as an amendment rather than left as
drift.

---

## 3. Measurement before magnitude — and it decides the driver

From `post_s3`, seed 777, no-policy, t1–t1000 (both seeds agree):

| country | output gap % (mean / sd / min / max) | **U gap pp** (mean / sd / min / max) |
|---|---|---|
| USA | **−14.54** / 0.64 / −17.2 / −12.4 | −0.04 / 0.18 / −0.77 / +1.24 |
| Poland | **−4.52** / 3.33 / −10.5 / +5.0 | −0.04 / 0.21 / −0.92 / +2.64 |
| Sweden | **+3.86** / 1.70 / −0.02 / +9.1 | −0.04 / 0.20 / −1.03 / +0.64 |
| Germany | +0.32 / 3.01 / −4.3 / +8.8 | −0.04 / 0.20 / −1.26 / +0.51 |
| Italy | −2.36 / 2.12 / −6.2 / +5.8 | −0.03 / 0.19 / −0.90 / +0.71 |
| France | −0.06 / 0.61 / −2.8 / +1.6 | −0.03 / 0.19 / −1.07 / +0.62 |

**The output gap is disqualified as the driver, by measurement.** It is dominated by a
persistent country-specific LEVEL — the USA sits at −14.5% for the entire run with sd 0.64,
because `PotentialGDP` was seeded 12.8% above `GDP` and both compound at similar rates, so the
gap never closes. A term on it is a **per-country constant**: the USA would carry a permanent
hoarding penalty and Sweden a permanent bonus, forever. That is precisely Q1's disqualified
"raw level term is a recalibration, not a coupling", arriving on a different variable.

**The unemployment gap is the driver.** Mean ≈ **−0.04 pp** (zero to two decimals), sd ≈
**0.19**, with real transients that decay (Poland +2.64 at t1 → ~0 by t5; USA +1.24 → 0 by t5).
Centred on zero, live, self-limiting: a coupling, not a constant. **And it is the same tightness
term the wage index already reads**, which is what makes B1's double-count question real rather
than theoretical.

**Growth gap** (realized − potential): sd ≈ 0.55 pp, also centred — a viable second candidate,
but it is Okun's own input, so a hoarding term on it would feed Okun's driver back into Okun.
The U gap is one step removed and is the quantity the hoarding story is actually about.

### Investment deepening — the model lacks both the mechanism AND the driver

- **No capital stock exists anywhere.** Project-wide, every "Capital" match is
  `CapitalGainsTax`. `Investment` is a pure flow, recomputed each period as
  `priorGdp × BaseInvestmentRate × interestFactor × BusinessConfidence`, with no accumulation
  and no memory.
- **And the flow has no cyclical variation to deepen from**: measured I/GDP is
  **19.5 – 20.9%** across the whole run for both countries sampled — effectively a constant.

So deepening would need a new `EconomyState` field (capital stock), a depreciation constant, a
daily treatment under the Phase-5 taxonomy, a save-shape change, and its own derivation of what
capital-per-worker means when `Population` and `LaborForceParticipationRate` both move. **That
is its own pass, not half of this one.** Recommended: **defer, with the trigger stated** — the
first scenario or coupling that genuinely needs an investment STOCK (the Wage Boom scenario does
not; see §7). One term shipping cleanly beats two terms forced.

---

## 4. Magnitude, in the unit the player experiences

**Labour hoarding, proposed: `h` = 0.4 pp of productivity growth per pp of unemployment gap**
(band **0.2 – 0.6**), signed so that a tight market (U below NAIRU) raises measured
productivity — procyclical, which is the claim.

In consequences, against the measured driver:
- **Typical** (|U gap| ≈ 0.19 sd): ±0.08 pp on productivity growth — visible in the stat's
  year-on-year line, invisible in its level.
- **A real transient** (Poland's +2.64 at t1): +1.05 pp for one turn, decaying with the gap —
  a legible "the recovery flatters productivity" beat.
- **Clamp headroom**: worst case 0.6 × 2.64 = 1.6 pp against the ±10 pp growth clamp. Ample.

**Under B1 only**, the wage-side exposure to tightness becomes `0.3 + h` (bargaining +
productivity-linked), i.e. **0.5–0.9 per pp** against a ±10 pp clamp; at the measured extreme
that is 2.4 pp of wage growth. Contained, but it **doubles the tightness channel into Q2's
sentiment factor**, which is the loop below. **Under B2 the combined stays exactly 0.3 by
construction.**

---

## 5. LOOP OR CHAIN — the headline finding

**Under A and B2: a chain, and a short one** (driver → productivity stat → display). No
feedback, because nothing economic reads `state.Productivity`.

**Under B1: a genuine closed LOOP — the first in this model built entirely from the couplings
this sequence added.** The circuit, with each link's constant read from the code:

```
U gap ──h──▶ productivity cycle ──1:1──▶ wage growth ──▶ Q2 gap ──0.5 %C/pp──▶ Consumption
   ▲                                                                              │
   └────── Okun (0.5) ◀── GDP growth ◀── identity (C = 60% of GDP, ×0.5 reversion) ┘
```

**Loop gain, derived from the constants:** per +1 pp of tightness gap →
`h` pp productivity growth → `h` pp wage growth → Q2 factor moves consumption by
`0.5 × h` % → first-round GDP ≈ `0.3 × (0.5h)` = `0.15h` pp of growth (Q2's own measured
0.3 first-round factor) → Okun returns `0.5 × 0.15h` = **`0.075h` pp of additional tightness**.

**Gain = 0.075 × h ≈ 0.03 at the proposed h = 0.4** (0.015–0.045 across the band). Positive
feedback, amplification `1/(1−0.03)` ≈ **+3%** on a tightness episode — against Okun's own
reversion pulling U back to NAIRU at **0.7 per turn**. **Stable by more than an order of
magnitude, and the margin is structural** (the gain scales linearly in `h`; it would take
`h ≈ 13` to reach unity, which is 20× outside the proposed band and far outside the ±10 clamp).

**This is a derived figure and the build pass must MEASURE it** — the honest test is the s=0
control plus a single-turn impulse decomposition, not this arithmetic.

---

## 6. The bar, pre-stated for whichever design is ruled

**Under A or B2 (re-rooting template):** baseline `pre_q5_<hash>`; matrix **byte-identical on
38 of 39 fields with `Productivity` the ONLY mover** — and under B2 the wage/GDP identity is a
same-sum claim, so any movement there is a failure, not a finding. Equivalence rows:
Productivity's existing R4-5 rows plus the new cyclical term at both regimes.

**Under B1 (force template, erosion posture):** s=0 byte-identity control FIRST, run again after
any fix; then decomposable movement — productivity, wages, Q2's gap, consumption, GDP, Okun —
each step against the stated chain, with the **loop gain measured** from a one-turn tightness
impulse and reported as the headline.

⚠ **A pre-named hazard for the build, from Q2's own scar:** the cyclical term's driver (the U
gap) **moves daily**, and `ApplyProductivityDaily` applies growth as a **power slice**. A live
daily gap inside that slice is exactly the shape that failed Q2's equivalence bar at the
`@8%shock` row (11.78% drift) and needed the **fifth fixed reference**. Expect the same answer —
anchor the gap at period open — and budget for it rather than discovering it.

Also: save/load **untouched under all three** (no new state, verified — this is why the
investment term's deferral matters); captures only if the Society productivity line's rendered
figure changes (F1 formatting: a ±0.08 pp growth change will not move the level's first
decimal for many turns — derive and state); cabinetstress bounded-and-explained; **scenario
slate:** see §7.

---

## 7. Wage Boom Management — does this pass make it authorable?

**Under B1: yes, and it becomes the scenario the loop exists for** — a tight-start scenario
where the player must cool an economy whose own productivity/wage/sentiment circuit is
amplifying the boom by ~3%. Authorable within Step 3's shipped format: a `U`-below-NAIRU seed
delta, a `Sustained` inflation-band objective (which would also be the **first exercise of the
`Sustained` objective form**, still unexercised), and a `NeverBreach` on approval.

**Under A or B2: no — and the scenario should stay deferred rather than be authored thin.**
Neither adds a dynamic the player can manage; the boom would be exactly today's boom with a
livelier productivity readout.

---

## 8. RULINGS NEEDED

- **R-Q5a — THE FORK (the pass's real decision): A (stat-only), B1 (additive force), or B2
  (carve-out re-rooting)?** *Recommendation: **B1**, because it is the only option that
  delivers what the spine deferred to this step (real cyclical dynamics reaching the economy),
  its loop is measurably stable with a 20× margin, and it is what makes Wage Boom authorable.
  **B2 is the honest fallback** if the double-count reading is rejected — it is a pure
  re-rooting, byte-identical, and still makes productivity truthfully cyclical.* Ledger entry
  is **not** offered: it is structurally disqualified (§2).
- **R-Q5b — the double-count, only if B1: are bargaining and productivity-linked pay two
  channels or one?** *Recommendation: **two** — wage bargaining and productivity-linked pay are
  distinct mechanisms that happen to share a driver, so the combined 0.3 + h stands, folded
  into the existing ±10 pp wage clamp per rule 11. If ruled **one**, B1 collapses into B2.*
- **R-Q5c — magnitude: `h` = 0.4 pp productivity growth per pp of unemployment gap** (band
  0.2–0.6), driver = the **unemployment gap**, the output gap having been disqualified by
  measurement.
- **R-Q5d — R-Q3b's amendment (A and B1 only): potential reads the LEDGER/trend; the
  productivity stat (and, under B1, wages) read trend + cycle.** *Recommendation: adopt and
  record as an amendment to R-Q3b, not as a correction of it — the 1:1 pipe was right about
  causation and is now being given the cycle/trend distinction it did not need until now.*
- **R-Q5e — investment deepening: DEFER, with the trigger** (a coupling or scenario that
  genuinely needs a capital STOCK), on the finding that the model has neither the stock nor —
  measured — any cyclical variation in I/GDP to deepen from. *Recommendation: defer; it is a
  pass of its own, not half of this one.*

**Stop at the report.** The build pass starts from R-Q5a.
