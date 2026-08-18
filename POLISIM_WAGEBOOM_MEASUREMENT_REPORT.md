# Wage Boom Management — measured, and DROPPED (2026-08-18)

**Scenario content pass, behind Step 3's shipped format.** Per the pass's own instruction ("author
it against measurement... if the measurement says the boom self-corrects too fast to be a
challenge, that's the finding and the scenario gets re-premised or dropped — say so"): **it was
re-premised once, the second premise also failed on the same measured constant, and the scenario
is dropped.** Nothing was added to `ScenarioLibrary` — every file this pass touched is under
`Assets/Editor/`, excluded from player builds, so **zero production code changed** (confirmed via
`git status`, not assumed).

The Sustained objective form (Step 3's shipped, previously-unexercised `ObjectiveKind`) was
exercised anyway, against a synthetic diagnostic rather than a shipped scenario — see §3.

## 1. The first premise — "manage an inherited boom" — falsified by measurement

**Setup**: Sweden and Poland seeded with unemployment 1.5–4.5 points below NAIRU (Sweden's own
seed: NAIRU 6.5), USA excluded (see §4). No player action, real day loop, real production
functions, seed 777.

**Finding, from the raw trajectory**: Okun's `UnemploymentReversionSpeed` (0.7/turn) closes ANY
tested impulse to within ±0.5 pp of NAIRU by **turn 2–3**, with a brief overshoot to the SLACK
side by turn 3, settling into ±0.05 pp noise from turn 6 onward — **regardless of impulse size**
(1.5, 3.0, and 4.5 pp impulses all converge to the same ~turn-6 state). Inflation peaks once, at
turn 1 (2.55–2.71% against a 2% target, scaling with impulse size), and is back to within 0.2 pp
of target by turn 4–5.

**On the ~12–30 turn horizon a playable scenario actually needs, this is a one-turn blip, not a
challenge.** There is nothing left for a player to "manage" by the time a scenario's entry screen
would even render its second turn.

## 2. The long-horizon "drift" is real but is NOT the boom, and NOT primarily Q5

The 150–200 turn no-policy runs (both Sweden and Poland) do show inflation climbing from ~2% to
3%+ over the full horizon, unemployment sitting near NAIRU almost the entire time. Three checks,
in order, before crediting this to the scenario mechanism:

- **§4 (this pass): the SAME drift appears starting from Sweden's own unmodified seed (U=8.0,
  NAIRU 6.5 — no deliberate impulse at all).** The drift is not caused by a deliberate tight
  start; it is a property of the model's baseline dynamics at long horizons.
- **The jumps are discrete, not a smooth secular climb**, and they land at the **same turn number
  (t80, then t110) across four independently-configured Sweden runs sharing seed 777** (impulse
  vs. no impulse, hiked vs. not) — the one thing all four share is the RNG stream position, and
  `EventSystem.EconomicEvent` carries an `InflationShockPoints` field rolled per turn from that
  same stream. **These are random economic events, not the wage-boom loop.**
- **The one component genuinely attributable to the Q5 loop is small and separable**: comparing a
  3 pp-impulse run against the 0-impulse baseline at turn 150 (both fully reverted on
  unemployment since turn ~10) shows a **+0.29 pp persistent inflation premium** (3.135% vs.
  2.841%) — real, measured, and consistent with Q5's own ~0.03 loop gain compounding a sticky
  (0.5/turn-adapting) expectations residual. **This is the only effect actually traceable to the
  scenario's premise, and it is an order of magnitude too small and too slow (100+ turns) to
  anchor a session-length scenario.**

**A single, realistic rate hike (applied ONCE, not re-applied every turn — see §2a) DOES reduce
this small residual, monotonically and safely**: +1.5 pp → −0.087 pp terminal inflation; +3 pp →
−0.17 pp; no recession, no overshoot, because Sweden's rate has no auto-reversion and simply holds
where the player leaves it. The lever is real. It is just managing an effect too small to be the
spine of a scenario.

### 2a. A measurement-methodology finding, corrected in round 2

Round 1's rate-hike test **re-applied the same `InterestRateChange` delta every turn for 10
turns** — not how a player would use the lever (there is no "hold rate at X" input, only
"change by this much this turn"). That test hit `CurrencySystem.MaxInterestRate` (15%) within
4–7 turns, swinging Sweden into a real recession (U up to 8.4%, GDP contracting turn-over-turn,
approval into the low 40s) — an artifact of the test, not a finding about the lever. Round 2's
single-hike-then-leave-alone test (§2 above) is the honest measurement.

## 3. The reframed premise — "sustain a boom against the model's own reversion" — measured impossible with every available lever

If a one-shot impulse self-corrects too fast, the alternative is asking the player to actively
HOLD tightness open. Measured directly, Sweden, impulse 2 pp, 25 turns, tracking the longest run
of consecutive turns with the gap ≥ 1 pp:

| lever | magnitude | max consecutive turns with gap ≥ 1 pp |
|---|---|---|
| none | — | **1** |
| interest-rate cut | −1 pp (once, held) | **1** |
| interest-rate cut | **−1.75 pp — to the absolute 0% floor** | **1** |
| infrastructure spending | +15% every turn, sustained | **1** |

**Every lever tested — including the interest rate cut all the way to `CurrencySystem`'s
absolute floor — produces the identical result: one turn.** The spending lever shows a literally
identical unemployment trajectory to doing nothing at every checkpoint, which is structurally
correct rather than a bug: infrastructure spending feeds `PotentialGrowthRate` (the ceiling
Okun's growth-gap term is measured against), not a growth-above-potential gap, so it cannot
open the gap Okun would then have to close. **A fourth lever was checked and found immaterial
without being run to trajectory**: `GetWelfareAdjustedReversionSpeed`'s own doc comment states
the mechanism directly — full UBI generosity buys only `UbiUnemploymentReversionPenalty = 0.05`
off the 0.7 base (reversion floor `MinUnemploymentReversionSpeed = 0.3` exists in code but no
currently-implemented program reaches within 0.35 of it). A 7% reduction against a lever that
dominates every other tested effect by roughly an order of magnitude changes nothing material.

**This is not "too hard" — it is unwinnable by construction**, which is worse than "too easy" for
a scenario: a player who cannot move the needle with the strongest available combination of
levers will correctly read the scenario as broken rather than as a fair, hard challenge.

## 4. A separate, unrelated finding: USA is disqualified for this scenario family independent of Q5

USA's `FederalReserveSystem` drives its rate automatically toward `TaylorRule`'s suggestion. The
Q5 derivation already measured USA's output gap as a persistent **−14.5%** structural level (a
seed-time `GDP`-vs-`PotentialGDP` mismatch, not a dynamic effect). `TaylorRule`'s output-gap term
(`0.5 × outputGapPercent`) is large enough to swamp the inflation term, driving the suggested —
and therefore the actual, Fed-Chair-damped — rate to **the 0% floor within ~30 turns regardless
of realized inflation running at 3–3.5%** (measured directly, §1's 60-turn USA run: rate 3.75% →
0.00% by turn ~55 while inflation sits at 3.1–3.5%). USA's own central-bank mechanism is not
usable for ANY inflation-management scenario until that structural output-gap distortion is
addressed — a finding for the roadmap, not something this pass fixes.

## 5. The Sustained objective form — exercised, on a synthetic diagnostic, all three questions answered

No real scenario ships this pass to carry the test, so `SustainedObjectiveDiagnostic` builds a
throwaway, never-shipped `ScenarioDefinition` (inflation ≤3% held for 5 consecutive turns,
Sweden, EndTurn 20) and drives it through the real `ScenarioEvaluator`/`ScenarioProgress`/save
code. This tests the FORM's mechanics, not a claim about content.

- **Does it evaluate as designed? Yes, exactly.** `ConsecutiveTurns` incremented every held turn
  (1→20 across the run, since inflation never breached 3% on this seed), `Met` flipped to `true`
  at precisely turn 5 (when the streak first reached `RequiredTurns`) and stayed `true`.
  **Confirmed explicitly: satisfying the streak early does NOT end the scenario early** — the
  verdict still waits for `EndTurn`, correct given a real scenario may carry other objectives
  still tracking.
- **Does the margin reporting read sensibly? No — a confirmed, specific gap.** The shipped
  verdict screen's figure line (`GameController.cs:4141`) is generic across every
  `ObjectiveKind`: `LastValue vs. Target (margin)`. For this run that renders as `"+1.14"` against
  the FINAL turn's inflation reading — **which says nothing about the 20-turn streak that is
  what actually decided `Met`.** `ObjectiveProgress.ConsecutiveTurns` already carries the right
  number; the verdict screen simply never reads it. **Not fixed this pass** — there is no shipped
  Sustained scenario to justify the UI change plus its own capture pass, and the fix belongs to
  whichever scenario exercises this form for real. Recorded here as the finding, with the exact
  line number, so it isn't rediscovered from scratch.
- **Does it survive a save crossing mid-sustain? Yes, exactly.** Captured at `ConsecutiveTurns=1`
  (not yet `Met`), the streak crossed a real `SaveGameService.Serialize`/`Deserialize` round-trip
  intact, and — the claim that actually matters — **continued counting correctly on the restored
  world** (1 → 2 after one more evaluated turn, matching whether the condition held that turn).
  `ScenarioProgress`/`ObjectiveProgress` needed no new persistence work: the shape Step 3 already
  shipped carries a Sustained streak correctly with zero changes.

## 6. RULINGS NEEDED / findings for the record, not for this pass to act on

- **Model-balance finding, outside this pass's scope**: `UnemploymentReversionSpeed = 0.7/turn`
  dominates every measured player lever by roughly an order of magnitude, up to and including the
  interest rate's absolute floor. This forecloses not just this scenario but the entire class of
  "sustain a deliberate labour-market condition" scenarios as currently tuned. Not a ruling this
  pass makes — a finding to hand to whoever next scopes labour-market content or considers a
  macro-rebalancing pass.
- **USA's Fed Chair mechanism vs. its structural output-gap distortion** (§4) — the same finding,
  independently reachable, worth the same disposition: recorded, not fixed here.
- **The Sustained-form margin display gap** (§5) — recorded with its exact site, deferred to
  whichever scenario ships the form for real.

## 7. The format verdict

**Moot, not "subset."** The pass never reached the point of testing whether "Wage Boom
Management" fits `ScenarioDefinition`'s existing grammar, because the underlying MECHANISM does
not support any legible, playable objective on this theme at all, under any objective-kind
shape — the problem is upstream of the format. Nothing here suggests the four `ObjectiveKind`s
are insufficient; `Sustained` itself worked exactly as designed the moment it had a condition
worth measuring.

## 8. What's next

Per the standing measurement: **The Disinflation** is the recommended next content pass. It is
the deepest coupled chain the model owns (adaptive expectations, Phillips, misery, and Q5's own
wage-surprise channel all in one scenario), the legibility panel (Step 2) now exists to let a
player trace exactly what this report had to reconstruct from raw dumps, and — unlike Wage
Boom — its premise (disinflating FROM an elevated starting rate) does not fight a dominant
reversion constant the way tightness-sustaining does; it works WITH Okun's reversion rather
than against it. **The Unequal Recovery** reads Q1's Gini-gap output and remains a credible
second choice. Content work continues behind Step 3's format until 13 Sept opens Step 4.
