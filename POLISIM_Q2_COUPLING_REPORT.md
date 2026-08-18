# Q2 — real wages → ConsumerConfidence: the derivation and the form ruling (2026-08-18)

**The queue's fourth graduation attempt; the second of the force kind.** Derive before wiring,
as ruled — and the flag the roadmap transcribed with the item ("own norm needs deriving;
growth-versus-trend is the likelier gap, a DIFFERENT shape than Q1's level-gap") is confirmed
by measurement below, not assumed. **Nothing is built in this pass**; the ruling package is at
the end. Baselines used: the standing `post_q1` dumps (2026-08-17 16:43–16:46, at-HEAD — the
only commit since, `c90e016`, touched Editor tooling and docs and cannot reach simulation math).

## 1. What real wages actually do at no-policy baselines — the measurement that decides the form

From `post_q1`, both seeds, t1000: **RealWageIndex is an unbounded compounding index, exactly
as the roadmap's caution said** — Poland reaches ~3.5×10¹⁵ by t1000 (3%/turn compounded), USA
~2.3×10⁹, France ~1.8×10⁴. Consequences, mirroring Q1's dichotomy but landing differently:

- **A raw level term is not merely a recalibration here — it is structurally impossible.** The
  index has no per-country baseline LEVEL to gap against (seeded 100 for all six by the
  2026-08-16 ruling, then compounds forever). A level gap needs a compounding reference index —
  a new state field growing at trend — before it can even be written down.
- **The growth gap needs NO new state at all, by an identity the code already contains.**
  `ApplyRealWageIndex` (MacroSystem 837–849) sets per-turn growth =
  `clamp(1.0·ProductivityTrendGrowth + 0.3·(NAIRU − U) − 0.3·(π − πₑ), ±10)`. So **realized
  growth minus trend IS the two cyclical terms** — labour-market tightness plus the inflation
  surprise — computable from current state at any application site. (At HEAD, trend ==
  `PotentialGrowthRate` via `ProductivityTrendGrowthRate`'s −1 fallback — nothing seeds it; if
  Q5 splits realized from trend productivity, a shared helper — R-Q2c — makes the factor follow
  the wage equation's own trend term automatically.)

**The measured gap at baseline** (per-turn realized RWI growth minus that turn's
PotentialGrowthRate; both seeds agree): mean **+0.011 to +0.035 pp/turn** (persistently
positive — seeded U sits slightly tight of NAIRU on average), sd **0.035–0.040**, steady-state
extremes ≈ ±0.1–0.26. The ±10 clamp never binds at baseline. **One deterministic transient,
named so it is never misread as a defect later**: the largest gap anywhere is Poland's turn 2 at
**−0.4841 pp — bit-identical across both seeds** — the seed-convergence transient (countries
seeded off cyclical equilibrium close their U/πₑ gaps over the first turns; the series runs
−0.48, −0.31, then noise by t5). A real "cost-of-living squeeze at game open," decaying in ~4
turns.

**The persistent positive mean rules out the accumulator form by measurement.** The one
in-code precedent that WRITES a confidence field on a gap — `ApplyCrimeEffects` →
BusinessConfidence — integrates its gap safely only because CrimeIndex mean-reverts to its
baseline, making the integral bounded. The wage-growth gap does NOT mean to zero: at any
legible sensitivity an integrator ratchets monotonically into the `MaxConfidence` 1.3 clamp
(e.g. s = 0.05/turn/pp on a +0.02 mean gap exhausts the entire 0.3 headroom in ~300 turns; a
larger s proportionally sooner). A coupling whose baseline behaviour is a slow one-way crawl
into a clamp is the compounding-growth failure pattern with extra steps.

## 2. The audit — ConsumerConfidence's writers, readers, posture

**Writers at HEAD, enumerated** (and the enumeration MEASURED: ConsumerConfidence is
**1.0000 for all six countries at every turn, t1–t1000, both seeds** — flat at seed):
(1) the `WorldFactory` seed, 1.0 for all six; (2) `ApplyCategorySpendingEffects` (MacroSystem
2039) — **change-driven**: reads the turn's `PolicyDecision` healthcare delta, so it fires only
when the player moves spending, never at baseline; (3) `ApplyWelfareProgramEffects` (2088) —
**gated on an implemented UBI program**, none by default. Nothing else writes it: no interrupt,
event, or system touches the field (project-wide search). Both live writers clamp into
`[MinConfidence, MaxConfidence]` = [0.7, 1.3] — the field's existing combined ceiling (rule 11:
the coupling folds into THIS, not a new one). **What ConsumerConfidence currently IS, stated
plainly: a policy accumulator around 1.0 — permanent player-earned shifts, no macro dynamics,
no reversion.** The coupling would be its FIRST macro writer.

**Readers — and the containment INVERSION against Q1, which sets this pass's bar.** Approval
wrote nothing economic, so Q1's matrix bar was "one moved field, decomposed." ConsumerConfidence
is the opposite: it multiplies Consumption in BOTH computation paths — the turn form
(`ApplyNationalAccounts`, line 61) and the daily form (`ApplyNationalAccountsDaily`, **twice**:
the Consumption level at 334 AND the contraction share at 337) — and Consumption is 60% of the
GDP identity. **A force here moves the whole trajectory at baseline, because the gap input is
baseline-active (sd 0.037, not zero).** The honest bar is therefore the erosion-term posture —
a RECALIBRATION BY CONSTRUCTION, expected small, with the first application decomposing exactly
and an off-switch negative control — not Q1's byte-identical containment claim. Pre-stated in
§4. One piece of luck recorded as such: `ApplyNationalAccountsDaily`'s own doc comment (line
305) already names "confidences drift within a period" as the EXPECTED equivalence-residual
class — the daily machinery was documented for this coupling before it existed.

## 3. The form, and the magnitude in the honest unit

**Proposed form (A): a stateless effective-confidence factor at the read sites.** In both
national-accounts methods, one local:

```
effectiveConsumerConfidence = Mathf.Clamp(
    state.ConsumerConfidence * (1f + WageSentimentSensitivity / 100f * wageGrowthGapPp),
    MinConfidence, MaxConfidence);
```

used in every line the raw field currently occupies (61; 334 and 337 together), where
`wageGrowthGapPp` comes from the shared helper (R-Q2c) that also feeds `ApplyRealWageIndex` —
the two can then never disagree, including under the ±10 clamp. Properties, each earned above:
zero new state (save/load untouched); the stored field stays the policy accumulator its two
writers assume; the ceiling is the EXISTING [0.7, 1.3] composite (rule 11 satisfied by
folding); memoryless, so no drift, no ratchet — persistence comes from the driver itself
(tightness episodes last years); daily-cadence-correct as a LEVEL factor on an annualized flow
(no slicing — same semantics as the confidence it multiplies). ⚠ **Named honestly for the
ruling: the enumeration said "real wages → ConsumerConfidence," and form A couples the wage gap
into consumption through an EFFECTIVE confidence at the read site — the stored field itself
does not move.** If the stored field moving is the intent, that is form B: a delta-model with
reversion toward a new base field — real sentiment persistence is the gain, and the costs are a
new `EconomyState` field (save/load shape change), rewriting both policy writers' permanence
semantics to write the base instead, and CC's stored value becoming dynamic under every reader.
Not recommended this pass; recorded as the named alternative.

**Magnitude — proposed: 0.5% consumption per percentage-point of wage-growth gap** (band for
the ruling: **0.25–0.75**). In consequences, using C = 60% of prior GDP and the identity's
output-gap reversion halving first-round deviations (first-round GDP ≈ 0.3 × ΔC%):

- Baseline noise (gap sd 0.037 pp): ±0.019% consumption, ±0.006% GDP — invisible, as it should
  be; orders of magnitude under the swing floor.
- The Poland turn-2 transient (−0.48 pp): −0.24% consumption ≈ −0.07% GDP, gone by t5 — a
  legible opening-squeeze flavour note, not a trajectory event.
- A sustained 1 pp boom (U a full 3.3 pp below NAIRU, or a persistent disinflation surprise):
  +0.5% consumption ≈ +0.15% GDP while it lasts — the size of a modest authored event, sustained
  only as long as the driver is.
- Theoretical clamp edge (±10 pp): ±5% consumption before the composite [0.7, 1.3] clamp —
  contained by the existing ceiling, same as every standing confidence source.

## 4. RULINGS NEEDED (with the derivation's numbers, per the directive)

- **R-Q2a — the form: the stateless effective-confidence factor** (form A above) on the
  growth-versus-trend gap (recommended — level-gap structurally impossible by measurement,
  accumulator ruled out by the measured persistent mean, form B named with its three stated
  costs). **Includes the honest flag that the stored ConsumerConfidence field does not move; A/B
  is the real fork.**
- **R-Q2b — the magnitude: 0.5% consumption per pp of wage-growth gap** (band 0.25–0.75),
  ruled in consumption percent and converted to GDP through the stated 0.3 first-round factor.
- **R-Q2c — the shared-helper posture: extract the wage-growth computation into one helper**
  read by both `ApplyRealWageIndex` and the factor (recommended — one source of truth for
  realized growth, clamp included; also the seam Q5's trend-vs-realized split will need), versus
  recomputing the two-term expression at the factor site.

**The bar, pre-stated for the build pass** (force-kind template, baseline-active variant —
the erosion posture, not Q1's): baseline `pre_q2_<hash>` dumped at the pre-commit hash; matrix
both seeds, all horizons, with movement EXPECTED everywhere downstream of Consumption —
smallness is the claim (baseline order ~2×10⁻⁴ relative on C, less after reversion), zero new
anomalies is the gate, and the first affected computation (day 1) must decompose to the factor
exactly; **the negative control is the off-switch: sensitivity 0 must be byte-identical to
`post_q1`** — that run, not an eyeball, is what proves the factor is the only new force;
equivalence 117/117 within existing bars (within-period confidence drift is the daily form's
own pre-named residual class); save/load untouched-confirmed (no new field); cadence
untouched-confirmed; captures — Economy shows Consumption/GDP, movement at baseline is below
visual resolution, say so if anything visibly moves.

**Stop at the report.** Q5 (labour hoarding / investment deepening) inherits the shared helper
if R-Q2c rules for it, and its trend-vs-realized wage question (the absorbed Q4 residual) lands
in the same helper's seam.
