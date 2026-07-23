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

## When Elias returns

- Read this file's Open Questions section first.
- Review the commit log — each item above should be its own commit(s) with validation results in the message or CLAUDE.md.
- Do not assume everything queued got finished — pick up wherever the queue actually stopped.

---

## Open Questions
*(Claude Code: add entries here as they come up. Do not resolve these yourself — flag and move on.)*

### 1. Should Economic Sectors feed back into aggregate GDP/Unemployment, or stay isolated?

**Resolved by Elias: INTEGRATE — Sector Output/Employment should feed back into the core economy,
not stay isolated.** Implemented as small, bounded nudges onto existing proven variables
(PotentialGrowthRate, Unemployment) rather than decomposing the GDP/labor identities themselves -
see "Sector Integration" in CLAUDE.md for the mechanism and validation.

Queue item 4 ("Small slice of economic sectors") required a real design decision: the four new
`Sector`s (Manufacturing/Technology/Agriculture/Finance) each track Output (% of GDP), Employment
(% of workforce), and one sector-specific metric, adjustable via Subsidy/Regulation policy dials -
but I deliberately made all three of those **descriptive only**, mean-reverting toward their own
seeded baseline and responding to their own sector's policy dials, with **zero feedback into the
core national accounts identity, Okun's Law, the Phillips Curve, ApprovalRating, or
Consumer/BusinessConfidence**. A more theoretically complete version would have sector Output sum to
(or at least meaningfully influence) aggregate GDP, and sector Employment feed into aggregate
Unemployment.

**Why I chose isolation for this pass**: (1) `ROADMAP_BRIEF.md`'s own ordering note flags that
mechanics touching the fiscal/core-simulation system directly have historically needed far more
debugging rounds than self-contained ones this project - wiring four new sector Output figures into
the GDP identity risks double-counting against the existing C+I+G+NX terms (which already sum to
GDP without any sector breakdown), a real risk of regressing already-validated, hard-won stability
(Turn-1 GDP Consistency, the Fiscal Reaction Function's debt equilibria, etc.). (2) The brief calls
this item "explicitly a proof-of-pattern pass" and explicitly invites a note here on "whether
expanding further seems safe" - which reads as permission to keep this pass minimal. (3) Isolation
makes the four failure patterns (turn-1 discontinuity, oscillation, unbounded growth, bimodal
attractors) essentially unreachable by construction (linear mean-reversion toward a policy-bounded
target), which the validation results confirm - zero anomalies attributable to sectors in either the
100/500-turn baseline or a dedicated `--sectorstress` scenario maxing every sector's dials
simultaneously.

**Recommendation**: keep sectors isolated through the current queue (items 5, the Sovereign Wealth
Fund, is the last, highest-risk item and doesn't depend on sectors). If a future task wants sectors
to meaningfully affect the core simulation, I'd recommend a SEPARATE, carefully-scoped follow-up
(not a retrofit) that redesigns the GDP identity's G/C/I terms around an explicit sector
decomposition rather than layering a second, competing GDP-driver on top of the existing one -
that's a bigger investigation than this pass's scope, consistent with how "Discretionary Spending
Growth" and "Fiscal Reaction Function" each took a dedicated investigation to get right.

### 2. Should InfrastructureAsset.ConditionIndex feed back into the economy, or stay isolated?

**Resolved by Elias: FEED BACK — ConditionIndex should nudge PotentialGrowthRate (not stay purely
observational).** Implemented as a small, bounded, threshold-based drag when condition sits below a
real-world-grounded healthy level, explicitly reconciled with the existing Infrastructure-spending
growth nudge under one combined ceiling so the two sources can't stack unboundedly - see
"Infrastructure Feedback" in CLAUDE.md for the mechanism and validation.

Round 2 item 5 ("Infrastructure system") raised the same class of question Open Question #1 already
raised for Economic Sectors, and I resolved it the same way for the same reasons: `ConditionIndex`
(Roads/Rail/PowerGrid/Broadband, 0-100 per country) is **descriptive only** - driven by a decay/
investment stock model (see CLAUDE.md's "Infrastructure System"), but with **zero feedback into
PotentialGrowthRate, GDP, Unemployment, ApprovalRating, or BusinessConfidence**. A more complete
version might have crumbling infrastructure drag on PotentialGrowthRate or BusinessConfidence, or
well-maintained infrastructure boost them further.

**Why I chose isolation for this pass**: (1) the task's own wording - "connect to the EXISTING
Infrastructure spending category and its existing PotentialGrowthRate effect rather than inventing a
parallel system" - reads most naturally as reusing the INPUT signal (`decision.
InfrastructureSpendingChange`, the same `PercentOfGdp` figure that already drives
`PotentialGrowthRate`), not as a mandate to add a second, new OUTPUT effect. (2) A ConditionIndex ->
PotentialGrowthRate feedback would double-count the exact same underlying spending signal that
already nudges `PotentialGrowthRate` directly in `ApplyCategorySpendingEffects` - the same
double-counting risk Open Question #1 flagged for Sector Output vs. the C+I+G+NX identity. (3) This
round's own ordering note asked for "real attention to the failure patterns... especially unbounded
growth/decay with no floor or ceiling" for this specific item - keeping ConditionIndex isolated (a
plain, hard-clamped stock with no downstream consumers) makes it trivially easy to reason about in
isolation, which the validation results confirm (a dedicated `--infrastructurestress` scenario -
sustained maximum spending cuts, the worst-case "zero investment, pure decay" path - produced zero
ConditionIndex anomalies across 500 turns, real Unity-confirmed).

**Recommendation**: same as Open Question #1 - if a future task wants infrastructure condition to
meaningfully affect the economy (e.g. crumbling roads/grid dragging on Business/ConsumerConfidence
or PotentialGrowthRate), scope it as a dedicated follow-up that explicitly reasons about
double-counting against the existing Infrastructure-spending-to-PotentialGrowthRate channel, rather
than bolting a feedback term on incidentally. Given Sectors (Open Question #1) and Infrastructure
(this question) are now two separate mechanics facing the identical "stay isolated to avoid
double-counting" design fork, Elias may want to decide both at once rather than one at a time.

