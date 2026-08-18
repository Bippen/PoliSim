# Step 3 — Challenge Mode: the scoping package (2026-08-18)

**Design work only; nothing is built.** Options and costs per question, ruling items at the end.
The standing ruling frames it: an authored scenario starts with **win/lose conditions, NOT an
election clock**.

## 0. What a "scenario" IS today — verified at HEAD, and it is the MIRROR IMAGE of what we need

`SimulationTestRunner.MatrixScenarios` holds 15 names. A scenario is a `switch` in
`BuildUsaDecision(scenario, usa, turn, world)` returning **one `PolicyDecision` per turn**, plus
two in-loop hooks in `RunOne` (cabinetstress auto-resolves pending decisions by worst-case
option; parliamentstress day-drives a maximal omnibus bill). **Every scenario starts from an
unmodified `WorldFactory.CreateDefault()`** — no seed-delta mechanism exists anywhere.

⚠ **THE INVERSION, and it is the whole derivation of this pass.** A validation scenario
**supplies the decisions and holds the world constant**. A playable scenario must do the exact
opposite: **supply the world and let the player supply the decisions.** The existing machinery
is therefore *not a foundation to extend* — it is the same idea pointed the other way, and
treating it as reusable would be the category error this pass exists to avoid.

**What DOES transfer, stated precisely:** (1) proof that mid-run state mutation is safe and
deterministic — `swfstress` creates two Sovereign Wealth Funds at turn 1 and `cabinetstress`
appoints six ministers, both *through the decision builder as a side effect*, which is the
right capability smuggled through the wrong seam; (2) the seeded-run discipline; (3) the
anomaly instrumentation, which a scenario author will want pointed at their start.
**What must be built is new:** the data format, the application seam, the evaluator, the entry
UI, the verdict. `grep -r "Objective|Scenario|Challenge" Assets/Scripts` returns **one file** —
the test runner. Production code has never heard of a scenario.

**The four gaps between a validation scenario and a playable one, each with its seam located:**

| gap | what exists at HEAD | the seam |
|---|---|---|
| **Entry** | world built in `Awake` via `CreateDefault()`; `SelectPlayerCountry` is *"the one place `_selectedPlayerCountryId` is ever set"* | apply deltas between construction and selection; the Canvas selector is the surface |
| **Evaluation** | nothing — only `ElectionSystem.RunElection` (approval ≥ 35 at `turn % 4`) | the post-turn hook `CheckElection()` already occupies (GameController:3663) |
| **Ending** | `_isGameOver` + `_gameOverReason` (a free-text string), drivable, and already **persisted** (`SaveGame.IsGameOver/GameOverReason`) | extend, do not invent |
| **Persistence** | no scenario id anywhere in `SaveGame` | one string + progress counters |

## 1. FORMAT (R-S3a) — the objective grammar the model can evaluate HONESTLY

Step 2's honesty classes transfer directly and decide the grammar. Class A/C quantities
(boundary formulas, period stances) are exact at a boundary; Class B (events) are dated; Class D
(compounding feedback) may be **thresholded** but never **attributed** — a scenario may say
"debt ended above 130", never "your tax policy caused 12 of those points".

**The four objective forms, and nothing else in v1:**
1. **Threshold at a date** — `DebtToGdp ≤ 120 by turn 12`. Exact at a boundary.
2. **Sustained condition** — `Inflation within [1,3] for 8 consecutive turns`. Needs one counter
   per objective in scenario state (persisted; the only new state this feature requires).
3. **Never-breach** (the fail-state form) — `approval never below 30`. Same evaluator inverted,
   checked every boundary.
4. **Terminal** — evaluated once, at the scenario's end date.

**Composite scores are excluded here and argued in §4.**

### The published-vs-live sub-question — and the answer is forced by the data, not by taste

R-S2c's live ruling does *not* automatically transfer, correctly flagged. But the option is
**structurally narrower than it looks**: `PublishedStat` holds only the **12 stats with a REAL
sourced release rule**, and its own doc comment forbids adding more as fabrication.
**`DebtToGdpRatio` is deliberately NEVER published** (`ClosingStat` exists precisely so debt can
be *recorded* without being *published*; the comment calls adding it "a permanent null trap") —
and neither is ConsumerConfidence, HousePriceIndex, or Productivity.

⚠ **So a published-judged objective is structurally unavailable on the debt axis — which is
exactly what the slate's two strongest scenarios are about.** A published posture cannot be
global; at most it is a per-scenario opt-in over those 12 stats.

Second cost, measured: **GDP publishes in three revision stages** (Advance → Second → Final,
mapped to Preliminary/Revised/Final, with a random revision draw). An objective judged on a
preliminary figure **can flip on revision**. That is either excellent drama or a bug report, and
it is only the former if the scenario declares the rule up front.

**Recommendation: v1 objectives evaluate the LIVE model.** The reason is the brief's own: the
legibility panel changes what is *fair* to set, and it explains live values. An objective judged
on a number the player cannot trace in the panel is unfair in exactly the way Step 2 exists to
prevent. Published-judged objectives stay a **named deferral** for a scenario that is *about*
information asymmetry, shipping with its revision rule stated (judge the final figure, or judge
the figure as it stood on the date — pick one, in writing).

### Format as data, and its cost

A `ScenarioDefinition` class + a static authored list, mirroring **`EventSystem.EventPool` and
`ForeignPolicySystem.MeetingPool`** — both are hardcoded in-code pools, both carry a comment
saying nothing downstream cares where the content comes from. Cost: near zero, compile-time
checked, no asset pipeline, no serializer risk. **JSON/ScriptableObject is NOT recommended for
v1**: Newtonsoft is present for saves, but authored content as code costs nothing to add and the
pool precedent exists twice already. The save needs only the scenario's **id** plus objective
progress — never the definition itself (the same reason `FiscalTurnReport` records values, not
formulas).

## 2. THE SLATE (R-S3b) — six, each with why the MODEL makes it hard

*A scenario the simulation cannot make hard is a menu item, not a challenge.* Each entry names
the mechanism that supplies the difficulty.

1. **"Inherit the Fund" — the creditor start.** *Deltas:* a large SWF, `GovernmentDebt` negative
   (net creditor), modest growth. *Objectives:* end turn 12 still a net creditor **and**
   PovertyRate ≤ seed. *Fail:* position crosses back to net debtor. **Why hard, from the
   record:** the erosion term is **symmetric by ruling R3** — a creditor's real claim erodes at
   π too, "no free money in either direction" — and the measured finding that **a net creditor
   earns nothing on its position** means hoarding decays while spending is irreversible. **This
   is the recorded coverage gap turned into content**: R3's creditor branch is code-verified but
   *live-unexercised* — "no scenario at HEAD creates a net creditor".
2. **Italy debt start.** *Deltas:* seeded ~138% debt, Italy's own maturity years and risk
   premium. *Objectives:* debt/GDP ≤ 130 by turn 10 with approval never below 30. *Fail:* rating
   downgrade past a named notch. **Why hard:** post-erosion the identity is real — (r − g − π)·b
   — and Italy's potential growth is **0.3%/turn**, so `g` cannot rescue the ratio; the FRF
   tightens as debt rises, and tightening costs approval through the spending term. The maturity
   rate-lag means today's rate relief arrives over years, not turns.
3. **Poland convergence.** *Deltas:* the seeded 3.0%/turn potential, 59% debt. *Objectives:*
   sustained real-wage growth with inflation in band N consecutive turns. *Fail:* inflation > 6%
   three turns running. **Why hard:** growth is the easy half — the tightness → wage →
   **(Q2) sentiment → consumption** loop plus the Phillips curve means a convergence boom
   overheats itself, and the Taylor rule answers with rates.
4. **NEW — "The Disinflation."** *Deltas:* inflation ~8%, expectations ~6%. *Objectives:*
   inflation ≤ 3% by turn 8 and still in office. *Fail:* approval < 30, or inflation > 5% at
   turn 12. **Why hard — the deepest coupled chain the model owns:** adaptive expectations make
   disinflation slow, Phillips makes it cost unemployment, unemployment costs approval through
   misery, and the wage index's **inflation-surprise term** erodes real wages, which since Q2
   feeds sentiment and consumption. Every link is visible in the trace panel — the scenario
   Step 2 makes fair.
5. **NEW — "Wage Boom Management."** *Deltas:* U well below NAIRU at start. ⚠ **Sequencing
   flag, stated rather than buried: its depth is partly Q5's** (labour hoarding, investment
   deepening) — at HEAD the tightness channel is real but thin, so this is the one slate entry
   that is **better authored after Step 5**, not now.
6. **NEW — "The Unequal Recovery."** *Deltas:* elevated Gini, a hostile seat composition.
   *Objectives:* Gini back to baseline without losing a confidence vote. *Fail:* approval < 30.
   **Why hard:** every lever that closes the Gini gap (welfare, minimum wage, tax) runs through
   Parliament, and each failed bill charges approval — while the Q1 Gini term is itself pushing
   approval down until it closes. **This is the scenario that proves Step 2's output is
   load-bearing**, per the spine's claim that scenario authoring reads this feature's output.

## 3. EVALUATION + ENDING (R-S3c)

**Where:** a `ScenarioEvaluator` reading the same live state the checks read — static, no
recomputation of model quantities, the `CreditRatingAnchorCheck`/`FrfSweepDiagnostic` posture.
**When:** **boundary-resident**, from the post-turn hook `CheckElection()` already occupies —
the seam is proven, ordered after `AdvanceTurn`, and already the place a run-ending verdict is
raised. Sustained conditions count in turns there; never-breach conditions are checked at the
same instant. (Day-resident evaluation is rejected: a threshold that can trip mid-period would
claim a precision the Class-D quantities do not have.)

**Ending:** `_isGameOver` + `_gameOverReason` already exist, are drivable, and already persist —
the verdict extends that path rather than inventing one. **IMGUI in v1, on the bare-desk
grammar** the election reveal uses (with its recorded lesson: `TextOnDesk`, never the paper ink
ramp — a defect found only by that screen's first capture). **Not Canvas, and the reason is
sequencing, not effort:** Canvas 3-of-3 (election night) is gated behind Step 4, and building a
verdict ceremony now would either duplicate that grammar or pre-empt a ruling that belongs to
it. A verdict is also **information-dense** — an objective list with met/missed and margins —
which is the ledger idiom, not the ceremony idiom. Revisit when Canvas 3 lands.

## 4. SCORING (R-S3d) — arguing with the prior, and ending where it started

The prior (pass/fail + stats epilogue over numeric scores) is **right, and for stronger reasons
than taste**:
- A composite score requires **weighting incommensurate objectives** — debt points against
  approval points — which is precisely the invented-number-that-looks-researched this project's
  rule 5 forbids.
- A leaderboard promises **cross-version comparability**, and this codebase has **five baseline
  discontinuities inside one fortnight**, each documented as invalidating prior figures. A score
  comparable across builds is a promise the project cannot keep and should not print.

**Recommended: pass/fail per objective + a legibility-powered epilogue**, plus one concession:
record the **margin** per objective ("debt ended 128.4 against ≤130") — a measured number, not a
fabricated composite.

⚠ **One real cost, found in this pass:** the epilogue wants the run's story, and **the
attribution ledger persists exactly ONE period** (R-S2e). Two honest options: **(a)** the
epilogue reads the final period's ledger plus `StatHistory`'s existing multi-resolution series —
cheap, honest, and already built; **(b)** accumulate per-scenario running term totals — new
persisted state, and a second summation to keep audited. **Recommend (a) for v1**, with (b)
named for the day an epilogue needs to say *why* rather than *what*.

## 5. FA CADENCE (R-S3e) — the playtest question, made concrete

Measured at HEAD: `MeetingChancePerDay = 0.01` → **~3.65 meetings/turn, ~97% chance of at least
one per turn**, each pausing the day loop until resolved — and the constant's own comment records
that whether this is *intended* or *inherited* was flagged for playtesting, deliberately not
tuned.

**The question is not "is 3.65 right" in the abstract — scenario pacing is what makes it
answerable:** *in a scenario with a 10-turn objective clock, at what rate do interrupts stop
reading as events and start reading as a toll booth?* **Proposed playtest:** the same scenario,
same seed, at **0.01 (3.65/turn), 0.005 (1.8/turn), and 0.002 (0.73/turn)**, one question asked
each time — "did the meetings feel like part of the story, or like a speed bump?" **Proposed
shape of the answer:** a per-scenario authorable multiplier rather than a global retune, so
pacing becomes a design lever (a diplomatic scenario legitimately wants more; a disinflation
run wants the player's attention on the Phillips curve).

## 6. THE MINIMUM PLAYABLE SLICE — Step 3's Q1-equivalent

**"Inherit the Fund," end to end.** Four reasons, in order of weight:
1. **It closes a recorded coverage gap as content** — R3's creditor branch, live-unexercised at
   HEAD — so the slice has standing value even if challenge mode never grows past it.
2. **Its objectives need only the simplest grammar** — a terminal threshold and a never-breach —
   so the evaluator ships without the sustained-counter state, and that state's cost is deferred
   until a scenario actually needs it.
3. **It stresses the seed-delta seam hardest in the slate** (a fund plus a sign-flipped debt
   stock is the largest starting-state delta proposed): if the format survives this one, the
   rest are subsets.
4. **It needs no UI beyond entry and verdict**, both of which are in scope for v1 anyway.

*The named alternative:* Italy is the cheaper build (no state sign-flip, deltas are pure
numbers) but closes nothing — it would prove the format without buying coverage.

## 7. RULINGS NEEDED

- **R-S3a — FORMAT**: `ScenarioDefinition` as authored C# data (the EventPool precedent), the
  four objective forms above, **objectives evaluated LIVE**, with published-judged objectives
  deferred as a per-scenario opt-in that must ship with a stated revision rule. (Recommended;
  the debt axis cannot be published-judged at all — structural, not preference.)
- **R-S3b — THE SLATE**: the six above, with **#5 (Wage Boom) sequenced after Step 5** by its
  own flag. Approve, cut, or reorder.
- **R-S3c — EVALUATION + ENDING**: boundary-resident `ScenarioEvaluator` on the `CheckElection`
  hook; verdict extends `_isGameOver`/`_gameOverReason`; **IMGUI on the bare desk in v1**, Canvas
  revisited when election night lands at Step 4. (Recommended.)
- **R-S3d — SCORING**: pass/fail per objective + margins + a legibility-powered epilogue reading
  the final ledger and `StatHistory`; **no composite score, no leaderboard** (the discontinuity
  argument). (Recommended.)
- **R-S3e — FA CADENCE**: run the three-rate playtest above and make the rate a **per-scenario
  authorable multiplier** rather than retuning the global constant. (Recommended; blocks
  nothing — decide when convenient, but the playtest wants a scenario to run inside, so it
  naturally follows the MVS.)
- **R-S3f — THE SLICE**: "Inherit the Fund" end-to-end as the minimum playable slice.
  (Recommended; Italy is the named cheaper alternative that closes no gap.)

**Stop at the package.** Nothing here is built; the build pass starts from the rulings.
