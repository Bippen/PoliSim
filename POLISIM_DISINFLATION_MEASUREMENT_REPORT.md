# The Disinflation — measured, and DROPPED (2026-08-18)

**Scenario content pass, behind Step 3's shipped format, second of two.** Per the standing
practice ("author against measurement... if the measurement says [it] self-corrects too fast to
be a challenge, that's the finding and the scenario gets re-premised or dropped — say so"): **it
did not self-correct too fast. It did not correct at all — with any tested lever, at any tested
magnitude, in any of five independent country/lever configurations.** Dropped. Nothing added to
`ScenarioLibrary`; every file this pass touched is under `Assets/Editor/` (confirmed via
`git status`), so **zero production code changed** and the default-path bar is trivially
satisfied without a redundant trajectory dump.

## 1. The half-life, measured first, as required

**Undefined — effectively infinite.** Five countries/configurations (Poland at four rate levels,
Sweden at three, Germany with and without its capped player push, Italy uncontested), each
seeded with inflation and expectations both elevated to 10% and unemployment left at its own
country seed, run 30 turns real day-loop, no other intervention:

| config | π at t1 | π at t30 | moved? |
|---|---|---|---|
| Poland, no policy | 9.20% | 9.34% | no — within noise |
| Poland, +1.5pp hike (once) | 9.20% | 9.24% | no |
| Poland, +3pp hike (once) | 9.20% | 9.15% | no |
| Poland, +5pp hike (once) | 9.20% | 9.02% | no |
| Sweden, no policy | 10.08% | 9.87% | no |
| Sweden, +1.5pp hike (once) | 10.08% | 9.75% | no |
| Sweden, +3pp hike (once) | 10.08% | 9.63% | no |
| Germany (Eurozone), auto-follow only | 9.92% | 10.12% | no — **rate climbed to 8.6% on its own and still didn't work** |
| Germany (Eurozone), auto-follow + max ±0.75 player push | 9.92% | 10.12% | no — **identical to three decimals** |
| Italy (Eurozone), auto-follow only | 9.82% | 9.79% | no |

**Every single configuration ends the 30-turn window within a point of where it started**,
regardless of country, regardless of whether the rate moved 0 points or 6+ points. This is
Wage Boom's "self-corrects too fast" disqualifier's mirror image: **nothing corrects it,
period — the same disqualifying shape (every lever gives the identical result) that ended
Wage Boom, now measured on the opposite mechanism.**

### 1a. Why: the model has exactly one disinflationary channel, and it never opens

`ApplyPhillipsCurveInflation`: `Inflation = InflationExpectations − PhillipsCurveSlope ×
(Unemployment − NAIRU)`. Inflation falls below expectations ONLY when unemployment sits in
genuine SLACK (above NAIRU) — and expectations only adapt toward REALIZED inflation each turn,
so if inflation itself never moves, expectations never move either. Starting with
`Inflation = Expectations` elevated and `Unemployment ≈ NAIRU` is therefore a **fixed point by
construction**, exactly as the derivation predicted before running anything: nothing pulls it
down without deliberately engineered, SUSTAINED slack.

**And sustained slack is exactly what Wage Boom Management already measured as unreachable.**
`UnemploymentReversionSpeed` (0.7/turn) pulls unemployment back to NAIRU from EITHER side —
Wage Boom found it prevents sustaining tightness BELOW NAIRU; this pass finds the identical
constant prevents sustaining slack ABOVE NAIRU. A rate hike reduces the consumption/investment
factors and should open a negative growth gap, pushing U into slack via Okun — but Okun's own
reversion term closes that gap back toward NAIRU in the same 1–2 turns it always does,
so the Phillips-curve channel this scenario needs never stays open long enough to pull
inflation down. **Same root cause, opposite direction, both foreclosed.**

## 2. The levers, tested explicitly and quantified — they do not bite

**Poland, terminal inflation vs. one-time hike size — a real but negligible slope:**
9.343% (0pp) → 9.244% (1.5pp) → 9.146% (3pp) → 9.017% (5pp). **≈ −0.065 percentage points of
30-turn terminal inflation per percentage point of hike, remarkably consistent across the whole
tested range** (a 5-point hike — more than doubling Poland's policy rate — buys about a third
of one point of inflation relief). Sweden: **≈ −0.08 pp/pp**, same order of magnitude. **Both
slopes are the quantified version of Wage Boom's "every lever including 0% gives the identical
result"** — here the levers are not literally identical, but they are close enough to be
useless: reaching the 2% target from 10% at this measured rate would require roughly a 120-point
hike, an order of magnitude past `CurrencySystem.MaxInterestRate` (15%).

**The Eurozone's automatic mechanism makes this WORSE, not better.** Germany's shared rate
auto-climbed from 2.25% to 8.6% over 30 turns via `EurozoneRateSystem`'s own GDP-weighted
Taylor-rule blend — a bigger hike than anything I applied to Poland or Sweden by hand — and
still produced **zero measurable disinflation** (10.12% at both t1 and t30). The player's own
capped push (`MemberRatePushRange` = ±0.75, `EurozoneRateSystem.cs:32`) is **literally
invisible against that already-large automatic move**: with and without the maximum player push,
Germany's t30 inflation is identical to three decimal places.

## 3. Attribution discipline — trivially satisfied, and worth stating why

The method that separated Wage Boom's real loop effect from `EventSystem`'s random shocks (same
seed, cross-configuration comparison, watch for jumps landing at the identical turn) applies
here too, but there is nothing to misattribute: **every one of the ten runs above is
statistically indistinguishable from every other at t30**, so there is no apparent recovery a
shock could be mistaken for. The absence of any signal is itself the finding, not something
needing separation from noise.

## 4. Country choice — the Eurozone ambiguity is ruled on, by measurement

**Ruled: Eurozone membership is a genuine, interesting design constraint for a scenario where
the player's own action is decisive — but it is DISQUALIFYING for one where the player needs to
move a large lever, because the automatic mechanism already moves it further than the player
ever could and the result is still nothing.** This scenario needed the second kind. Sweden and
Poland (independent, uncapped, player-set rates) were the correct candidates on paper; the
measurement shows the country choice was never the deciding factor — Poland, Sweden, Germany,
and Italy all failed identically, because the disqualifying constant (Okun's reversion) is
global, not per-currency-regime. USA remains disqualified independently (Q5/Wage Boom finding:
Fed-Chair rate dominated by the structural output-gap distortion).

## 5. TWO DROPS IS A PATTERN, NAMED

**`UnemploymentReversionSpeed = 0.7/turn` has now foreclosed two scenarios testing OPPOSITE
directions of labour-market intervention** — Wage Boom Management needed to SUSTAIN tightness
below NAIRU against the reversion; The Disinflation needed to CREATE slack above NAIRU against
the same reversion. Both failed for the identical reason, at the identical order of magnitude
(every tested lever, up to and including absolute extremes — the rate's 0% floor for Wage Boom,
the rate's near-ceiling auto-climb for this pass — produced no material difference from doing
nothing). **This is not "this scenario's premise was wrong" twice. It is one finding about the
model's own tuning, encountered from two directions**: as currently tuned, this constant
forecloses the entire class of scenarios whose challenge is "move the unemployment gap away
from NAIRU and keep it there," regardless of which direction, regardless of country, regardless
of which lever the player is handed. **A model-balance finding for whoever next scopes
labour-market content or a macro-rebalancing pass — not something this pass fixes**, per the
same standing discipline as Wage Boom's report.

**A second, related structural note, new to this pass**: elevated inflation alone (with no
confounding unemployment problem — cleanest in the Sweden run, whose own unemployment transient
is small) crashes `ApprovalRating` from ~47 to below the 35-point `ElectionSystem.LosingThreshold`
within **3–7 turns in every one of the five configurations**, via the standing misery-index
mechanism, well before any player action could plausibly show results even if a lever DID bite.
A scenario built on this premise would risk an automatic election-loss game-over before its own
management phase could begin — a second, independent reason (beyond the lever failing) that this
premise does not survive as a playable challenge on a realistic turn horizon.

## 6. What's next

Two of the four remaining named scenarios are now known-bad for the SAME reason (any premise
resting on deliberately moving the unemployment gap away from NAIRU and holding it there).
**Italy debt start** and **Poland convergence** were never premised on labour-market-gap
management — their difficulty sources (the erosion/maturity fiscal identity; growth vs. inflation
overheating from productivity convergence, not from an engineered gap) are structurally
different and untested by either drop. **The Unequal Recovery** (Gini-gap management via
Parliament) is also untested and does not obviously depend on the same constant. **Recommend
Italy debt start next** — its mechanism (the debt identity, post-erosion/maturity) is the
best-validated, most-measured part of this model by a wide margin (the erosion term alone has
three dedicated measurement passes behind it), which is the opposite risk profile from the two
drops above. Content work continues behind Step 3's format until 13 Sept opens Step 4.
