# PoliSim — Standing Working Brief

This document is a standing instruction set for autonomous work on PoliSim while Elias is away. It exists because every mechanic built this project so far needed real validation and, sometimes, a real human decision — this brief is meant to let work continue safely without either being skipped.

**Read this in full before starting anything. Follow it for every item in the queue below.**

---

## Non-negotiable working discipline

1. **Real Unity is the standard of truth, not the standalone harness.** The harness has been wrong about project state at least three times this session (a stale swing threshold, an interest-rate crash it mischaracterized as "settling noise," and a debt trajectory that flatly contradicted real Unity). Use it for fast iteration only. Before considering *anything* done, validate via `BatchSimulationRunner` against real Unity (`G:\UNITY\Unity Hub\6000.5.4f1\`) at both 100 and 500 turn horizons.

2. **Watch for the specific failure patterns already seen in this project**, in every new mechanic:
   - A turn-1 discontinuity (a value jumping the moment the simulation starts, rather than easing in)
   - Oscillation (a value swinging back and forth turn over turn rather than settling)
   - Unbounded/compounding growth (a value racing to an extreme with no ceiling)
   - Bimodal attractors (a value that only ever settles at two extremes, never anywhere realistic in between)
   
   All four have happened before. Assume a new mechanic is guilty until the 500-turn batch run proves otherwise.

3. **Commit per unit of work.** One feature, one commit, with a descriptive message. Never bundle unrelated changes. Confirm staged contents match the commit message before committing.

4. **Escalate, don't guess, on genuine design decisions.** Examples of what counts: which real-world data source to trust when sources disagree, how to model something structurally ambiguous (like the EU's currency split was), whether a fix might regress previously-validated behavior, any modeling choice that trades accuracy for a hard technical constraint. When this comes up: **do not pick silently.** Add it to the "Open Questions" section at the bottom of this document with your recommendation and reasoning, and move to the next queue item instead of blocking on it.

5. **Ground new mechanics in real data**, sourced via web search, the same way every prior mechanic was (tax rates, debt-to-GDP, spending categories, poverty rates). Label anything illustrative/stylized honestly in code comments and CLAUDE.md — never let a placeholder look like real data.

6. **Scope every new system small on the first pass.** Plumbing plus 2-4 clearly-justified, separately-named effects — not the full theoretical richness the roadmap originally described. Each item below is already scoped this way; don't expand it further without a documented reason in Open Questions.

7. **Update CLAUDE.md after every item**, including a brief "Validated: [date], 100/500 turns, real Unity, N anomalies" line so the history stays traceable.

8. **Closing the Unity Editor window doesn't always mean the process has actually exited (confirmed twice now).** A `BatchSimulationRunner` launch will fail (or silently fight for the project lock) if a stale `Unity.exe`/`UnityPackageManager.exe` is still alive, even after its window is gone. Before assuming Unity is closed and it's safe to start a batch run, verify with `Get-Process Unity*,UnityPackageManager -ErrorAction SilentlyContinue` in PowerShell (or `Get-CimInstance Win32_Process -Filter "Name='Unity.exe'"` for command-line detail) — don't rely on the window having disappeared.

---

## Queue (work top to bottom; do not skip ahead or parallelize)

**Ordering note:** items are sequenced safest/most-proven-pattern first, riskiest last. Mechanics that touched the fiscal system directly (debt, spending, interest) have historically needed far more debugging rounds this project than self-contained ones — so the Sovereign Wealth Fund, which touches GovernmentDebt and the budget directly, is deliberately last. If time runs out before reaching it, that's the correct outcome, not a failure.

### 1. Expand the event system
- Grow the existing 8-event pool meaningfully (real, varied economic/political events — recessions abroad, commodity shocks, diplomatic incidents, scientific breakthroughs). Keep each event's effect small and bounded, same as the existing 8.
- Do not build the map or geographic/severity tagging yet — that's a later, separate task once this larger pool is validated.

### 2. Labor market basics
- Add LaborForceParticipationRate as a tracked stat (real OECD data per country). Add a minimum wage policy lever with a small, real-world-grounded effect on Unemployment and PovertyRate (both already exist — reuse them).
- Do not build the full labor market system (union membership, gig economy, remote work, etc.) yet.

### 3. Crime & justice basics
- Add one or two tracked stats (suggest: a general CrimeIndex, real data if findable, or a stylized 0-100 index if not — label honestly either way) and 2-3 policies (police funding, sentencing policy). Keep effects on ApprovalRating and BusinessConfidence (both proven).

### 4. Small slice of economic sectors
- 4-5 sectors only (suggest: Manufacturing, Technology/Software, Agriculture, Finance — pick ones with clear, distinct real-world profiles). Each tracks Output, Employment, and one sector-specific metric — not the full 10-metric roadmap list yet.
- 2-3 sector policies max (subsidies, tariffs, regulation) — not all 14.
- This is explicitly a proof-of-pattern pass. If it validates cleanly, note in Open Questions whether expanding further seems safe.

### 5. Sovereign Wealth Fund (highest risk — see ordering note above)
- Create/dissolve, annual contribution rate, domestic vs. international allocation, asset class mix (equities/bonds/infrastructure/real estate — keep to 4, not the full original list), a simple market-return model (small random return per asset class within a realistic historical range — source real average return figures), interaction with the budget (contributions are an expense, returns are income) and with GovernmentDebt (fund assets can offset net debt figures, but do not let this be used to hide a real fiscal problem — display both figures separately).
- USA-first is fine for the initial implementation; only expand to all six if it validates cleanly.
- Given this item's history of interactions elsewhere in the project, budget extra validation passes here specifically — re-run the full existing matrix (baseline/stress/exploit/tariff-override/welfarestress), not just a fund-specific scenario, to catch any interaction with the fiscal reaction function or debt attractor.

---

## If the queue finishes early

This is a fine, safe outcome — do not start inventing new scope. Instead: re-run the full validation matrix one more time across everything built this session to confirm nothing has drifted, write a summary of all queue items completed and their validation status to CLAUDE.md, and stop.

**Status: all 5 items completed (2026-07-22), not early — this is the full queue.** All five were
implemented, validated (standalone harness first, then `BatchSimulationRunner` against real Unity at
100/500 turns), and committed as their own commits, with validation results recorded in CLAUDE.md's
own dedicated section for each ("Expanded Event Pool," "Labor Market Basics," "Crime & Justice
Basics," "Economic Sectors," "Sovereign Wealth Fund"). Item 5's own extra-caution requirement (re-run
the full existing matrix) was satisfied via `SimulationTestRunner`'s `-runmatrix`, extended with a
new `swfstress` scenario (now 6 scenarios x 100/500 turns = 12 combinations) — zero regression in the
five pre-existing scenarios, zero NaN/negative/out-of-range/divergence anywhere including the new
scenario. One genuine design decision was escalated rather than resolved silently (see Open Questions
#1 below) — Economic Sectors' deliberate isolation from the core GDP/Unemployment loop. No new scope
was invented beyond the five items as specified.

---

## Round 2 Queue

Same standing brief, same non-negotiable working discipline above (real-Unity validation via
`BatchSimulationRunner` at 100/500 turns, one commit per item, escalate genuine design judgment
calls to Open Questions rather than deciding silently, ground new mechanics in real data, keep scope
small on the first pass) — none of it is superseded or relaxed for this round. Work through in order,
same as Round 1; do not skip ahead or parallelize. If the round finishes early, do not invent new
scope — re-run the full validation matrix across everything built (both rounds) and stop.

**Do not revisit Round 1's Open Questions #1.** Economic Sectors stay isolated from the core
GDP/Unemployment/Approval loop exactly as Round 1 shipped them — that call is still pending Elias's
own decision, not something to resolve or integrate further during this round, even incidentally
while touching a related area (e.g. item 5's Infrastructure system connects to the existing
Infrastructure *spending category*, not to Economic Sectors).

**Ordering note:** items are sequenced by how directly they build on already-validated Round 1
foundations versus how much genuinely new territory they open. Items 1-4 extend mechanisms that
already exist and are already validated (the SWF's growth-ceiling fix, LaborForceParticipationRate/
minimum wage, CrimeIndex); item 5 (Infrastructure) is the most novel — a new decay/maintenance
mechanic with no direct Round 1 precedent — so it's deliberately last. If time runs out before
reaching it, that's the correct outcome, not a failure.

### 1. Expand the Sovereign Wealth Fund to all six countries
- Primarily a seeding/calibration task, not new mechanism design — `SovereignWealthFund`/
  `SovereignWealthFundSystem` are already country-agnostic; the USA-first mechanic and its
  300%-of-GDP growth ceiling are already validated.
- Source real (or honestly-labeled illustrative) starting contribution rates, domestic/international
  allocation splits, and asset-class mixes per country — Norway's real GPFG allocation benchmarks
  (already used to inform the original return-rate calibration) are a reasonable reference point for
  at least one other country, but do not assume every country's real posture matches Norway's.
- Re-run the full validation matrix (not just a fund-specific scenario) at 100/500 turns given this
  item's fiscal-integration history, the same extra caution Round 1's SWF item required.

### 2. Detailed spending "Phase 2"
- Wire real economic effects into 4-5 more of the still-effect-less USA discretionary spending
  categories from "Detailed Spending Portfolio" (suggest: Justice, HomelandSecurity, Energy,
  Housing) — 15 of the 19 Discretionary categories currently have zero economic effect by original
  Phase 1 design.
- Follow the exact same pattern as the four categories that already have one (Transportation ->
  PotentialGrowthRate, Healthcare -> ConsumerConfidence, Education -> BusinessConfidence, Defense ->
  approval only) — one or two small, clearly-justified, separately-named effects per category, not
  an exhaustive list per category.

### 3. Deeper labor market policies
- Building on LaborForceParticipationRate/the minimum-wage lever: paid family leave, overtime/
  working-hour regulation, workforce retraining programs.
- Real data where findable (e.g. real paid-leave policy differences across the six countries - some
  have statutory paid leave, some don't, mirroring how minimum wage's own real-world asymmetry was
  handled in Round 1). Small effects on LaborForceParticipationRate/Unemployment/ApprovalRating (all
  already proven) - do not build the full theoretical labor-policy list.

### 4. Deeper crime & justice
- Building on CrimeIndex: bail reform, drug enforcement vs. decriminalization as a policy axis,
  prison population as a new tracked stat.
- Keep effects routed through ApprovalRating/BusinessConfidence/CrimeIndex (all already proven) -
  do not invent a new outcome channel for this pass.

### 5. Infrastructure system (most novel this round — see ordering note above)
- Road/rail/power-grid/broadband condition tracking with a maintenance/decay mechanic - deferred
  maintenance should degrade the metric over time, investment should improve it.
- Keep to 3-4 infrastructure types, not the full original list.
- Connect to the EXISTING Infrastructure spending category and its existing PotentialGrowthRate
  effect rather than inventing a parallel system - this is new territory (a decay mechanic has no
  direct Round 1 precedent to mirror), so budget real attention to the failure patterns above,
  especially unbounded growth/decay with no floor or ceiling.

---

## Round 3 Queue

Same standing brief, same non-negotiable working discipline above (real-Unity validation via
`BatchSimulationRunner` at 100/500 turns, one commit per item, escalate genuine design judgment
calls to Open Questions rather than deciding silently, ground new mechanics in real data, keep scope
small on the first pass) — none of it is superseded or relaxed for this round. Full validation
matrix required for anything fiscal-touching (items 1, 4, and 5 explicitly) — a single-scenario smoke
check is acceptable only for item 2 (UI/policy-only, no new tracked feedback).

**Not started — queued for future work, no timeline.** Added 2026-07-30, after Round 2 and the
UI revamp / country-selection / SWF-return-model work that followed it had all landed.

**Ordering note:** items are sequenced safest/most-proven-pattern first, riskiest/most novel last —
the same principle Round 2's own ordering note used. Item 1 (SWF drawdown) extends an already-built,
already-validated system rather than inventing one, so it leads. Items 2-4 each extend an existing,
proven integration pattern (sector policies, crime stats/policies, more sectors), with item 4 flagged
moderate-risk given it adds MORE contributors into an already-shared, already-near-its-ceiling
combined growth adjustment. Item 5 (Demographics) is deliberately last: brand-new tracked data
feeding three existing systems simultaneously, the same reasoning that put the Sovereign Wealth Fund
last in Round 2. If time runs out before reaching it, that's the correct outcome, not a failure.

### 1. Sovereign Wealth Fund drawdown mechanic — DONE (2026-07-30)
- Directly closes the gap identified in the just-finished SWF-returns rebalance task (see "Sovereign
  Wealth Fund Return-Model Rebalance" in CLAUDE.md and the known-limitation note in "Status"): allow
  withdrawing fund assets during a recession/emergency — a policy lever, not automatic — reducing
  `SwfAssets` and correspondingly reducing the fund's ability to pay debt to zero purely through
  unconstrained growth against its own 300%-of-GDP ceiling.
- **Lower risk than starting something new** — this extends an already-built, already-validated
  system (`SovereignWealthFund`/`SovereignWealthFundSystem`) rather than inventing one.
- Full validation matrix required (fiscal-touching, same extra caution every SWF-adjacent item has
  needed) — specifically re-test Sweden/France's 500-turn trajectory (the same per-turn diagnostic
  approach used for the original debt-floor investigation and its returns-rebalance follow-up) to
  confirm this, combined with the realistic-returns fix already shipped, actually RESOLVES the
  debt-floor pinning rather than just slowing/delaying it further.
- **Result: implemented and confirmed to genuinely resolve the pinning, not just slow it** - see
  "Sovereign Wealth Fund Drawdown Mechanic" in CLAUDE.md. `ContributionRatePercent`'s range simply
  extends below zero (reusing 100% of the existing contribution/return/clamp plumbing, no new field
  or code path beyond fixing one sentinel-value collision); a dedicated diagnostic confirmed a
  sustained -3%/turn withdrawal drives both funds to 0 `SwfAssets` within ~20 turns and keeps them
  there, after which Sweden and France each settle into a genuine, stable, non-zero `DebtToGdpRatio`
  equilibrium instead of the floor. Full 24-combination matrix re-validated: zero finite/negative/
  out-of-range anomalies, no regression under any existing scenario.

### 2. Expand sector-specific policies
- Add 3-4 more of the original roadmap's sector policy types (suggest: Tax Credits,
  Deregulation/Nationalization as a single axis, Research Grants) to the 4 existing sectors
  (Manufacturing/Technology/Agriculture/Finance).
- **Low risk** — same integration pattern already proven (Subsidy/Regulation dials), no new tracked
  stats. Single-scenario smoke check is acceptable here — UI/policy-only, no new tracked feedback.

### 3. Deeper crime & justice
- Building on the existing CrimeIndex/Incarceration Rate: add Organized Crime and Corruption as new
  tracked stats (real data if findable, honestly labeled stylized if not — the same rule
  "Crime & Justice Basics" already followed), plus Judicial Funding and Border Enforcement as
  policies.
- Same risk profile as Round 2's crime depth work ("Deeper Crime & Justice") — keep effects routed
  through already-proven channels (ApprovalRating/BusinessConfidence/CrimeIndex/Incarceration Rate),
  don't invent a new outcome channel for this pass.

### 4. Expand economic sectors
- Add 3-4 more sectors beyond the initial Manufacturing/Technology/Agriculture/Finance (suggest:
  Energy, Construction, Retail, Telecommunications), using the now-proven integrated pattern (Output/
  Employment/one sector-specific metric, Subsidy/Regulation dials, feeding `PotentialGrowthRate`/
  Unemployment per "Sector Integration").
- **Moderate risk**: `PotentialGrowthRate` already has THREE stacked nudge sources (Infrastructure
  spending, Infrastructure condition, Sector performance — see "Sector Integration"). Adding more
  sectors means MORE contributions into that same combined ceiling, not a fourth independent one —
  re-confirm the ceiling still holds with a larger sector count via a DEDICATED stress scenario
  (all new and existing sectors pushed to their Subsidy/Regulation extremes simultaneously), not just
  the standard matrix, the same "actively binds, not just theoretically present" standard
  "Infrastructure Feedback"/"Sector Integration" already established.

### 5. Demographic system (population aging, birth/death rates, immigration)
- **Large, independent system — do not treat this as a small addition.** Unlike most items above,
  this doesn't extend an existing proven mechanic; it's new plumbing (an age-structure/population
  model) that several OTHER systems then read from. Scope the first pass with the same "small,
  bounded effects" discipline as every item above, but budget it as its own multi-part effort, not a
  single quick pass — expect it to take longer than any single item in Rounds 1-2.
- **Core plumbing (first pass)**: per-country tracked demographic stats, each seeded from real
  per-country data (UN World Population Prospects / OECD are the obvious sources — the same caliber
  of source this project already used for tax rates, debt-to-GDP, poverty rates, etc.; label
  anything stylized honestly, the same rule every prior mechanic followed). Suggest starting with an
  old-age-dependency-ratio-style figure (or MedianAge), BirthRatePer1000, DeathRatePer1000, and
  NetMigrationRate — not a full age-cohort pyramid.
- **Feeds into exactly three things on the first pass, not the full theoretical richness "a
  demographic system" suggests**:
  1. **Pension sustainability** — a small, bounded upward-cost pressure on the existing Social-
     Security-equivalent Mandatory `SpendingLine` (USA has one directly; the other five countries'
     generic `SocialPrograms` line from "Country Selection" Part 2 is the closest analog for them),
     following the same "small, separately-named effect" pattern every spending-category effect in
     this project already uses (see "Detailed Spending Portfolio Phase 2").
  2. **Labor force size** — a small, bounded nudge on the existing `LaborForceParticipationRate`
     (see "Labor Market Basics"), not a new parallel labor-supply model.
  3. **Healthcare cost pressure** — a small, bounded nudge on the existing Healthcare-equivalent
     spending line's cost (USA's `HHSDiscretionary`, which already has a real economic effect — see
     "Detailed Spending Portfolio").
- **This is fiscal-touching, like the Sovereign Wealth Fund was** — pension/healthcare cost pressure
  feeds Mandatory spending, which feeds `GovernmentDebt` and the Fiscal Reaction Function's already
  hard-won equilibria. Budget the same extra validation caution the SWF item required: re-run the
  FULL existing matrix (all 12 scenarios, not just a demographics-specific one) at both 100 and 500
  turns before considering this done — every established failure pattern (turn-1 discontinuity,
  oscillation, unbounded growth, bimodal attractors) applies here, and this is exactly the class of
  fiscal-system-touching change that has historically needed the most debugging rounds in this
  project.
- **Two genuine design decisions to expect — escalate to Open Questions rather than resolving
  silently if they come up**: (1) whether demographic drift should be slow/monotonic (population
  aging gradually over hundreds of turns) or meaningfully shiftable by policy levers (immigration
  policy, family/childcare incentives) on this first pass; (2) how immigration should interact with
  the existing `LaborForceParticipationRate`/`Unemployment` figures without double-counting against
  effects "Labor Market Basics"/"Deeper Labor Market Policies" already established.
- Do not build the full theoretical richness a "demographic system" implies (detailed age-cohort
  pyramids, migration flows between the six modeled countries specifically, generational
  wealth/inequality effects) on this first pass — plumbing plus the three bounded effects above,
  matching every other item's scoping discipline.

---

## When Elias returns

- Read this file's Open Questions section first.
- Review the commit log — each item above should be its own commit(s) with validation results in the message or CLAUDE.md.
- Do not assume everything queued got finished — pick up wherever the queue actually stopped.

---

## Open Questions
*(Claude Code: add entries here as they come up. Do not resolve these yourself — flag and move on.)*

### 1. Should Economic Sectors feed back into aggregate GDP/Unemployment, or stay isolated?

**Resolved by Elias: INTEGRATE — Sector Output/Employment should feed back into the core economy,
not stay isolated.** Implemented and shipped as commit 8235975 ("Integrate Sector Output/Employment
into PotentialGrowthRate/Unemployment under an all-sources ceiling") — see "Sector Integration" in
CLAUDE.md for the full mechanism and validation detail; summarized here.

Built as small, bounded nudges onto existing proven variables (`PotentialGrowthRate`, `Unemployment`)
rather than decomposing the GDP/labor identities themselves. `GetSectorGrowthAdjustment` sums each
`Sector`'s current `OutputShareOfGdp` gap against its own `BaselineOutputShareOfGdp`, clamped to its
own `MaxSectorGrowthAdjustment` (0.5); `GetSectorUnemploymentAdjustment` does the same for
`EmploymentShare` vs. `BaselineEmploymentShare`, feeding `ApplyOkunsLaw` directly. Because
`PotentialGrowthRate` now has three simultaneous sources (Infrastructure spending, Infrastructure
condition, and this new Sector performance term), `MacroSystem.ApplySectorGrowthEffect` sums all
three and clamps the TOTAL to a new all-sources ceiling, `MaxTotalPotentialGrowthAdjustment` (1.0) —
the piece that actually prevents three separately-capped nudges from stacking past one sane bound.

**Validated**: standalone harness first (100/500-turn baseline, the full 11-scenario regression
matrix, plus a new `--growthstackstress` scenario forcing Infrastructure condition to 0 AND all four
Sectors to their worst-case settings simultaneously — the genuinely dangerous same-direction stacking
case for an additive ceiling) — zero real anomalies anywhere; 500-turn `growthstackstress` GDP
4,178,690, `DebtToGdpRatio` 147.0%, both matching established equilibria. **Real-Unity confirmed
(2026-07-29)** via `BatchSimulationRunner -runmatrix` (all 12 scenarios x 100/500 turns): same clean
result, `growthstackstress` at 500 turns landing at GDP 4,180,200 / `DebtToGdpRatio` 147.1% — within a
fraction of a percent of the harness figures — and growth rate observed pinned at essentially exactly
`+1.00%`/turn from roughly turn 50 through turn 500, direct evidence the 1.0 combined ceiling binds
correctly under the worst-case stack.

### 2. Should InfrastructureAsset.ConditionIndex feed back into the economy, or stay isolated?

**Resolved by Elias: FEED BACK — ConditionIndex should nudge PotentialGrowthRate (not stay purely
observational).** Implemented and shipped as commit d01632e ("Feed Infrastructure ConditionIndex back
into PotentialGrowthRate under a combined ceiling") — see "Infrastructure Feedback" in CLAUDE.md for
the full mechanism and validation detail; summarized here.

Built as a small, bounded, threshold-based drag rather than decomposing the GDP identity: split into
`Country.BasePotentialGrowthRate` (the original, immutable, seeded trend rate), the pre-existing
Infrastructure-spending accumulator (`InfrastructureSpendingGrowthAdjustment`, non-negative, clamped
to `[0, MaxInfrastructureSpendingBoost]`), and a new live, non-accumulating condition-drag computed
fresh every turn from the average `ConditionIndex` across all four `InfrastructureAsset`s versus a
50-point threshold (`drag = Clamp(-InfrastructureConditionDragSensitivity * Max(0, threshold -
averageCondition), -MaxInfrastructureConditionDrag, 0)`), which eases automatically if condition later
recovers. `MacroSystem.ApplyInfrastructureGrowthEffect` combines both under ONE shared ceiling,
`MaxCombinedInfrastructureGrowthAdjustment` (0.75 — deliberately tighter than the sum of the two
individual caps, so it's a genuinely active constraint) — the piece that satisfies "reconcile the two
sources, don't just cap each individually."

**Validated**: standalone harness first (100/500-turn baseline, the full 9-scenario regression
matrix, plus a new `--deferredmaintenance` scenario forcing every `ConditionIndex` to 0 at turn 1 and
sustaining a -30%/turn Transportation cut for the whole run, isolating and maximally stressing this
new growth-rate channel specifically) — zero real anomalies anywhere; 500-turn `deferredmaintenance`
GDP 49,052,176, `DebtToGdpRatio` 143.5%, both matching established equilibria. **Real-Unity confirmed
(2026-07-29)** via `BatchSimulationRunner -runmatrix` (all 12 scenarios x 100/500 turns): same clean
result, `deferredmaintenance` at 500 turns landing at GDP 48,639,590 / `DebtToGdpRatio` 144.1% —
within a fraction of a percent of the harness figures, confirming the ported logic's fidelity.

