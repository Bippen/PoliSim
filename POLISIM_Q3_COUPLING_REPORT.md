# Q3 — productivity → PotentialGrowthRate: the derivation, the audit, and the fork (2026-08-17)

**The queue's second graduation attempt, on the Education-term template: derive before wiring.**
The derivation completed; the rule-11 stacking audit ran; and the derivation surfaced a
premise-contradiction that is exactly the escalation class the directive named as expected.
**Nothing is built in this pass** — a contradicted premise is a stop, the same way a cycle would
have been. The ruling fork is at the end.

## 1. What exists at HEAD, derived not inherited

**PotentialGrowthRate is already a per-turn DERIVED quantity with a coded ledger** — not a
mutated-in-place constant. `MacroSystem.ApplySectorGrowthEffect` (the single finalizer) computes:

```
PotentialGrowthRate = Clamp( BasePotentialGrowthRate
                             + Clamp(infrastructureAdj + sectorAdj, ±1.0),   // the all-sources ceiling
                             0, 8 )
```

**The writers, enumerated (the audit's first output): exactly two contributors through one
finalizer** — Infrastructure (`ApplyInfrastructureGrowthEffect`, own ceiling) and Sectors
(`GetSectorGrowthAdjustment`, ±cap on the aggregate output gap). The all-sources ceiling
(±1.0) is genuinely active (tighter than the 1.25 worst-case stack, per its own comment); the
hard range [0, 8] backstops. `BasePotentialGrowthRate` is captured from the seeds at world
creation (USA 2.0 · Sweden 1.5 · Germany 0.8 · France 0.8 · Italy 0.8 · Poland 3.5).

**The readers**: PotentialGDP growth (turn + daily forms), Okun's growth gaps, the spending
baselines, TaylorRule's output gap (via PotentialGDP), the preview clone — and three Round 4
consumers: RealWageIndex (the 1:1 pass-through Q3 retires), HousePriceIndex (its own 1:1 trend
read, NOT named by Q3 — untouched), and Productivity itself (the pure 1:1 trend read that Q3
inverts).

**The adjustment path, MEASURED from the standing no-policy baselines** (post_f1, s777;
adjustment = dumped `Country.PotentialGrowthRate` − Base): **ZERO at the ruled window** (every
country sits exactly at Base through t100 — the sector gap and infrastructure term are
quiescent without policy) **and −0.5 by t500–t1000 for all six** (infrastructure-condition
decay saturating its own floor). This number decides everything below.

## 2. The circularity check and the two lawful graphs

Q3's ruled wiring: productivity growth DRIVES potential growth; wages read PRODUCTIVITY'S OWN
growth, never a loop through potential. Both candidate graphs below are acyclic (the only
recurrence is the existing across-time sector/infrastructure feedback, which is temporal, not
a within-step cycle). RealWageIndex's cyclical terms (tightness, inflation surprise) are
untouched in both.

**Design A — adjustments stay potential-side:**
`trend_p → Productivity → (wages, and 1:1 → core of Potential); {infra, sectors} → Potential only`
Productivity gets its own trend (seeded = Base, value-preserving at t0); Potential =
productivity growth + adjustments; wages read productivity growth — and therefore STOP seeing
the adjustments they see today (they currently arrive through the potential pass-through).

**Design B — adjustments flow through productivity:**
`{infra, sectors} → trend_p → Productivity → (wages, and 1:1 → Potential)`
The adjustment ledger relocates inside productivity's growth; Potential = productivity growth,
1:1, nothing else; wages see everything they see today.

## 3. The finding that forces the fork

- **Design B is the economically correct claim** — decayed roads and booming sectors ARE
  labour-productivity channels; in B, infrastructure decay lowers productivity, which lowers
  wages AND potential coherently. **And B is VALUE-IDENTICAL at HEAD**: every quantity equals
  its current value at every step (same sums, same clamps, same 1:1s — a pure causal
  re-rooting). Its matrix bar would be **byte-identical, zero new fields** — the strongest
  validation a re-rooting can have, and the direct contradiction of the pass premise
  "trajectory-moving by construction."
- **Design A moves trajectories** — but only LATE (the window adjustment is measured zero, so
  the ruled 100–200 window shows nothing; by t1000 wages/productivity run ~0.5 pt/yr above
  today's paths as they shed the decay drag) — **and it does so by asserting something false
  about the world**: that infrastructure decay reduces potential output while leaving labour
  productivity and wages untouched.
- **The fiscal signature re-check comes free under either design**: Potential's PATH is
  identical in B and in A (A changes only who reads what; the potential sum is unchanged), so
  g is unchanged, (r − g − π)·b is unchanged, and the erosion-era debt trajectories carry over
  bit-for-bit. Also stated with its search: the Education competence term touches youth-U only
  (no reader chain into any of this — cabinetstress interaction nil).

## 4. The stacking audit's output (the ruled trigger, fired)

The coupling consumes **zero new ceiling headroom in either design**: it replaces the CORE of
the sum (Base → productivity-trend growth, seeded identically) at 1:1, adds no third
contributor, and leaves the ±1.0 all-sources ceiling, both per-contributor caps, and the [0, 8]
range exactly as they stand. The audit's forward-looking half: once Q3 lands, **any future
productivity-moving term (Q5's labour-hoarding/investment-deepening; Education-class policy
effects) enters Potential through this pipe** — in B they would also enter wages coherently —
and each such term folds into the existing ceiling at ITS OWN ruling, which is the pipe doing
rule 11's work for it. **Proposed coupling magnitude: 1:1, no damping constant** — anything
other than 1:1 would assert that potential growth and trend productivity growth diverge
permanently, which is the claim the old pass-through already made and Q3 exists to retire.

## 5. RULINGS NEEDED (rule 4 — the escalations the directive predicted)

- **R-Q3a — the fork: Design B (recommended) or Design A.** B is the true claim with a
  byte-identical bar (the premise corrects: Q3's TRAJECTORY movement arrives with Q5's inputs
  in the now-correct pipe, not with the re-rooting itself). A is trajectory-moving at the cost
  of an economically false split. If B: the bar inverts to byte-identical-expected, R4-4
  style, and "trajectory-moving by construction" is struck from the pass record with this
  report cited.
- **R-Q3b — the magnitude: 1:1 (recommended)** per the audit; any damping constant is a new
  world-claim needing its own justification.
- **R-MS2 — the canonical six-step enumeration** of Master Sequence II (the roadmap block
  records the steps the directive names; the full list is Elias's text).

**Stop at the report.** Q1 queues behind R-Q3a either way — its own baseline, its own pass.
