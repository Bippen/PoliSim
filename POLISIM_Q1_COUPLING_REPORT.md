# Q1 — Gini → ApprovalRating: the derivation and the form ruling (2026-08-17)

**The queue's third graduation attempt; the first coupling-adds-force pass under the amended
template.** Derive before wiring, as ruled — and the expected escalation (the coupling's FORM)
arrives with its numbers. **Nothing is built in this pass**; the ruling package is at the end.

## 1. What Gini actually does at no-policy baselines — the measurement that decides the form

From the standing `post_q3` dumps, both seeds, all horizons: **Gini is FLAT** — every country
holds within ±0.15 points of its seed from t1 to t1000 (USA 39.4–39.5 · Germany 29.4–29.5 ·
Italy 32.1–32.2 · Poland 25.9–26.1 · Sweden 27.5–27.6 · France 29.9–30.0). The R4-2 design
doing exactly what its bucket-exclusion note said: slow reversion to a baseline the no-policy
target never leaves. Consequences, as the directive's own dichotomy anticipated:

- **The change term is INERT at baseline** (|ΔGini| ≈ 0.003/turn — its approval effect would be
  invisible at any legible sensitivity) and stays small even under policy (Gini moves by slow
  reversion — a change term punishes the RATE of a deliberately slow variable).
- **A raw level term is a RECALIBRATION, not a coupling** — on a flat series it is a per-country
  constant (USA −39.5×s forever, Sweden −27.6×s forever): every baseline approval re-anchored,
  the exact outcome the directive named as disqualifying.

**The derivation's answer is the third form the dichotomy invites: the GAP form** —
`−s × (Gini − BaselineGini)`. Zero at seed for all six by construction (the standing zero-gap
idiom); active exactly when POLICY moves inequality off the country's own norm (income tax vs
its anchor ≈ 1.5 Gini-pts per 10 tax-pts; welfare transfers at half poverty scale ≈ 1–2 pts;
minimum wage ≈ 0.5–1 pt). And it IS the directive's "change-dominant with a mild level anchor"
resolved into one term: voters punish DISPLACEMENT from their society's own inequality norm —
habituation to the level is baked in (Sweden's 27.6 and the USA's 39.5 both sit unpunished),
worsening is what costs, and the "level anchor" is the norm itself. **The formula already
contains this exact idiom twice**: `paidLeaveApprovalEffect = s × (level − Baseline)` and the
welfare effect — gap-versus-own-baseline sustained terms, named in the code as the pattern.

## 2. The audit — ApprovalRating's writers, posture, and the coupling against it

**Writers at HEAD, enumerated**: (1) the standing formula `ApplyApprovalRating` — a DELTA model:
`Δ = reversion×(50 − approval) + growth − misery(unemployment/inflation/crime/corruption gaps)
− taxHike + weightedSpending(×deficit-awareness) + welfare + paidLeave + drugPolicy`, clamped
[0,100], **turn-boundary-resident** (called from AdvanceTurn and preview only — approval is
politics, and politics lives at boundaries per the Phase 5 taxonomy; **the coupling therefore
needs NO daily shape and no equivalence rows — stated by derivation, verified by the unchanged
117/117 at build time**). (2) The interrupt shocks: cabinet options, events, meetings (±2–5
face values). (3) ParliamentSystem: six bill-failed cost sites + the tax-hike penalty. (4) The
reshuffle cost. **Readers (the containment claim's search)**: ElectionSystem (game-over
threshold), ParliamentSystem.UpdateSeats (approval → seats — which affect bill passage, which
is player-gated, and bill-failed costs, which need bills), preview/UI. **Approval writes
nothing economic**: in a no-policy run its value feeds no simulation quantity, so the
force-kind matrix expectation is: **ApprovalRating is the ONLY moved field**, its movement
decomposing to the coupling against the dumped Gini path — everything else byte-identical.
(At baseline the gap is the ±0.15 wiggle → approval moves by ≤ ~0.2 equilibrium points —
small, real, and decomposable without remainder, the erosion standard in miniature.)

**The posture finding**: approval's sustained gap-terms carry **NO combined ceiling** — unlike
PotentialGrowthRate's ±1.0 all-sources clamp, approval's containment is the 0.05/turn reversion
plus the [0,100] range, and the existing stack is already loose (drug policy alone can shift
equilibrium ±20 points at dial extremes). **The audit recommends NO new ceiling this pass**:
approval is the political variable — large sustained shifts are the design, elections are lost
on it, and the realistic Gini contribution (below) is small in the standing company. The
absence-of-ceiling is NAMED here as a standing property and flagged to the LEGIBILITY feature
(MS II step 2), whose job will be explaining exactly this stack to the player.

## 3. The magnitude, in the honest unit

The delta model turns a per-turn term into an EQUILIBRIUM shift of `s / reversion` — with
reversion 0.05, the multiplier is **20×**, so magnitudes must be ruled in equilibrium points
(the precedents in that unit: paid leave = 1.0 eq-pt/week; drug policy = 0.4 eq-pt/dial-pt).

**Proposed: 1.0 equilibrium approval point per Gini point** (per-turn sensitivity 0.05). A
serious redistribution reversal (+3 Gini) costs −3 approval SUSTAINED — the size of one serious
authored shock (±2–5), but permanent until the policy reverts: legible beside the shocks, never
dominant against misery/growth (which swing far more), and zero for every country that leaves
inequality at its norm. Theoretical extreme (clamp-edge gap ~25–39 pts) is contained by the
[0,100] range and reversion, same as every standing term. **Band for the ruling: 0.5–1.5
eq-pts/Gini-pt** (s = 0.025–0.075).

## 4. RULINGS NEEDED (with the derivation's numbers, per the directive)

- **R-Q1a — the form: the GAP term** `−s × (Gini − BaselineGini)` in the standing formula
  (recommended — change-term inert by measurement, raw level a recalibration by measurement,
  gap form zero-at-seed, habituation-true, and twice-precedented in the formula itself).
- **R-Q1b — the magnitude: 1.0 equilibrium point per Gini point** (s = 0.05/turn; band
  0.5–1.5 eq-pts). Ruled in equilibrium units, converted through the 0.05 reversion.
- **R-Q1c — the posture: no new combined ceiling on approval's sustained terms** (recommended),
  with the absence named as a standing property and handed to the legibility feature.

**The bar, pre-stated for the build pass** (force-kind template): baseline `pre_q1_<hash>`;
matrix — ApprovalRating the ONLY moved field, movement decomposing to `s × gap path` without
remainder, all 38 other fields byte-identical (the containment claim, verified not assumed);
equivalence 117/117 unchanged (turn-boundary term, no daily shape — by derivation); save/load
and cadence untouched-confirmed; cabinetstress bounded-and-explained with the distrust rule on
any exact-baseline match; no capture changes expected (Society shows Gini, Politics shows
approval — both already on screen; say so if anything moves).

**Stop at the report.** Q2 (real wages → ConsumerConfidence) inherits the form precedent this
ruling sets — the gap-versus-own-norm shape is its natural candidate too.
