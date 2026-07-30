# PoliSim — Master Roadmap

This replaces three previously-separate standing documents (`ROADMAP_BRIEF.md`, `CONTINUOUS_TIME_MIGRATION.md`, `POLITICAL_SYSTEMS_OVERHAUL.md`), which had grown real dependencies on each other without being coordinated in one place. Read this in full before starting anything.

---

## Non-negotiable working discipline (applies to everything below, no exceptions)

1. **Real Unity is the standard of truth, not the standalone harness.** It has been wrong about project state multiple times this project (a stale swing threshold, an interest-rate crash mischaracterized as noise, a debt trajectory that flatly contradicted real Unity). Use it for fast iteration only. Before considering *anything* done, validate via `BatchSimulationRunner` against real Unity (`G:\UNITY\Unity Hub\6000.5.4f1\`) at both 100 and 500 turn horizons (or their day-equivalent, once the continuous-time migration changes the unit).
2. **Watch for the four failure patterns already seen repeatedly**: turn-1 discontinuities, oscillation, unbounded/compounding growth, bimodal attractors. Assume a new mechanic is guilty until the full-horizon batch run proves otherwise.
3. **Commit per unit of work.** One feature, one commit, descriptive message. Confirm staged contents match the message before committing.
4. **Escalate, don't guess, on genuine design decisions.** Add to Open Questions with a recommendation and reasoning; move to the next item rather than blocking.
5. **Ground new mechanics in real data.** Label anything stylized honestly — never let a placeholder look like real data.
6. **Scope every new system small on the first pass.** Plumbing plus a few clearly-justified effects, not full theoretical richness.
7. **Update CLAUDE.md after every item**, including validation results, so history stays traceable.
8. **Verify Unity processes actually exited** (`Get-Process Unity*,UnityPackageManager`) before trusting that a closed window means it's safe to run a batch validation — confirmed to cause false failures more than once.
9. **All new named entities (cabinet ministers, party names, legislators) are original and fictional** — never real people or real political parties. Same rule the Fed Chair mechanic already established, extended to every new character/entity going forward.
10. **All visuals stay procedurally-drawn** (Texture2D, the GraphRenderer/MapRenderer/Policy Web technique) — no imported sprite art, per Elias's explicit choice.
11. **Any new mechanic that nudges an existing tracked variable must fold into that variable's existing combined ceiling**, not add an uncounted new source — audit the actual ceiling code before adding a contributor, don't assume there's room.

---

## Where things stand right now

- **Roadmap Rounds 1-3: fully complete.** 15 items, all implemented, validated, and committed. Full detail lives in `CLAUDE.md`'s per-item sections (Expanded Event Pool, Labor Market Basics, Crime & Justice Basics, Economic Sectors, Sovereign Wealth Fund, SWF Drawdown, Expanded Sector Policies, Deeper Crime & Justice II, Expanded Economic Sectors II, Demographics Parts A & B). Both prior Open Questions (Sector Integration, Infrastructure Feedback) are resolved — see the Resolved Open Questions section near the bottom of this document for the short version.
- **Master Sequence step 1 (Political Systems Overhaul Part A — Cabinet): DONE (2026-07-30).** Only 3 of the 6 confirmed portfolios implemented this pass (Finance/Treasury, Interior/Justice, Health & Social Affairs), per Part A's own content-authoring warning — see "Cabinet (Political Systems Overhaul Part A)" in `CLAUDE.md` for the full writeup and real-Unity validation (28-combination matrix, zero new anomaly types, directional confirmation via a targeted diagnostic).
- **Continuous Time Migration: not started.** Full plan below.
- **Political Systems Overhaul Parts B and C: not started.** Full plan below.
- **No Round 4 has been scoped.** Per the sequencing below, don't scope one yet — new features should be built against the post-Parliament interaction model, not the current one, to avoid retrofitting.

---

## THE MASTER SEQUENCE — work this list top to bottom, do not skip ahead

This is the one authoritative order, replacing whatever each original document separately suggested. It exists because Political Systems Overhaul Part B depends on Continuous Time Phase 0, and because building new Roadmap features or converting existing systems to daily granularity while Parliament's gating is mid-rollout would mean touching the same code for two different reasons at once — exactly the kind of overlap this project's discipline exists to avoid.

1. **Political Systems Overhaul — Part A (Cabinet). DONE (2026-07-30) — see "Cabinet (Political Systems Overhaul Part A)" in `CLAUDE.md`.** No dependencies. Full spec in Part A below.
2. **Political Systems Overhaul — Part C (UI/graph restyling).** No dependencies. Full spec in Part C below.
3. **Continuous Time Migration — Phase 0 (calendar, speed control, short-term gameplay scaffolding).** No dependencies beyond what already exists. Full spec below. Keeps all existing economic math at its current cadence — this phase is purely the calendar/UI layer.
4. **Political Systems Overhaul — Part B, PILOT ONLY (Tax Policy tab).** Depends on step 3. Prove the full draft → introduce → vote → pass/fail flow end-to-end on one tab before touching any other. Full spec below.
5. **Political Systems Overhaul — Part B, full rollout** to the remaining seven tabs, only once step 4 is validated.
6. **Resume Roadmap work (a new Round 4)** — only scope this once step 5 is done, so anything new is built directly against the gated-legislation model from day one.
7. **Continuous Time Migration — Phases 1 through 5** (the actual daily-granularity conversion of each system's math, safest-first, core macro engine last). This is deliberately positioned after the political-systems work — it's a separate concern (simulation granularity, not who can change policy) and touching the same files for two unrelated reasons in the same window is worth avoiding.

If a step's own validation fails, fix it before moving to the next — never proceed past a failing step to "make progress" on the next one.

---

# PART ONE: Continuous Time Migration

*(Full original plan preserved — becomes step 3 and step 7 of the master sequence above.)*

## Why this exists

Migrating from turn-based (1 turn ≈ 121 days / 4 months) to true daily-granularity continuous time with Pause/1x/2x/3x speed controls. Nearly every tuned constant in the game is implicitly calibrated against a ~121-day step. This is the largest single risk in the project's history — do not attempt it as one pass.

## The translation methodology — do not guess new constants

Identify which mathematical shape a constant is before touching it:

1. **Linear/additive rates**: `rate_per_day ≈ rate_per_turn / 121`
2. **Multiplicative/compounding rates**: `rate_per_day = (1 + rate_per_turn)^(1/121) − 1`
3. **Probabilities**: `p_per_day = 1 − (1 − p_per_turn)^(1/121)`
4. **Hard clamps/ceilings do NOT shrink by 121x** — a ceiling bounds the state itself, not a per-step increment. Only the *speed of approach* changes (via #1 or #2 above). Treating a ceiling as something to also divide by 121 is a likely first-attempt bug.

## The validation bar: aggregation-equivalence

Before any system's daily version is trusted: simulate 121 consecutive days and confirm the result is within ±3-5% of what the existing, already-validated single turn-level step produces for the same inputs. This is the ground truth every phase below must pass before moving to the next.

## Real-world data release cadence (cross-cutting, applies to every phase)

The internal simulation can evolve daily; the player-facing display should only update stats on the schedule real institutions actually publish them, for realism and pacing:
- **Continuous/real-time**: Currency Strength.
- **Monthly**: Unemployment, Inflation, Trade Balance, sector output figures.
- **Quarterly**: GDP and GDP growth %, DebtToGdpRatio.
- **Annual**: Population, demographic rates, PovertyRate, CrimeIndex/PrisonPopulationRate, Infrastructure ConditionIndex, annual budget figures.
- **Central-bank-meeting-based** (not calendar-periodic, ~8/year like the real Fed/ECB): interest rate decisions.
- **Election-cycle-based**: elections, Fed Chair appointment.
Optional refinement (Open Question, not required for a first pass): real reporting lag between a period ending and its data being published.

## Phase 0 — Calendar, speed control, short-term gameplay scaffolding (MASTER SEQUENCE STEP 3)

1. Real in-game calendar date, advancing daily. Pause/1x/2x/3x speed controls.
2. The EXISTING turn-cadence economic tick fires automatically every 121 in-game days, unchanged internally. Proves the calendar layer works on trusted math before any translation begins.
3. Redesign the UI around this — remove manual "Advance Turn," replace with date + speed controls. Redesign the live Policy Preview (effect-per-day plus a selectable-horizon projection).
4. StatHistory needs multi-resolution storage (raw daily + aggregated weekly/monthly/quarterly buckets) — daily data alone over "last 50 entries" would show nothing meaningful for GDP-scale trends.
5. Build short-term gameplay scaffolding: ongoing-process budgets, a decisions/interrupts system, foreign policy meetings, and — **superseded by Political Systems Overhaul Part B once both are ready** — a law-passing mechanic where legislation takes real in-game days/weeks to move through stages. Do not build a competing version of this once Part B exists; Part B's design is authoritative for law-passing specifically.
6. Validate: single-scenario smoke check (no economic math touched) plus direct confirmation the automatic 121-day tick matches a manually-clicked turn exactly.

## Phases 1-5 — daily-granularity conversion (MASTER SEQUENCE STEP 7, safest-first)

- **Phase 1**: Sectors and Infrastructure (smallest blast radius, proves the methodology).
- **Phase 2**: Labor Market and Crime & Justice (moderate risk).
- **Phase 3**: Tax portfolio, Welfare, Spending categories, SWF (revenue/spending-critical, same seriousness as the original debt work).
- **Phase 4**: Demographics (its YearsPerTurn scaling is a direct dependency on turn-length — cannot start until the new day-length constant is threaded through correctly; use the same throwaway-diagnostic-before-full-matrix discipline that caught its two prior structural bugs).
- **Phase 5**: The core macro engine — GDP identity, Okun's Law, Phillips Curve, interest rate transmission, Fiscal Reaction Function, debt dynamics. Highest risk, last on purpose — this system has the worst track record for hidden instability in the project. Do not start until every other phase has proven the methodology reliable.

Each phase: apply the correct transform per constant, aggregation-equivalence check FIRST, full scenario matrix SECOND, one commit per phase, escalate ambiguous constant shapes to Open Questions rather than guessing.

---

# PART TWO: Political Systems Overhaul

*(Full original plan preserved — becomes steps 1, 2, 4, and 5 of the master sequence above.)*

## Confirmed scope (both at maximum ambition — treat accordingly)

- **Cabinet is INTERACTIVE** — ministers periodically bring decisions/events, not a passive stat bonus.
- **Parliament gates ALL existing policy changes across every tab** — Tax, Spending, Welfare, Labor, Crime & Justice, Sectors, Infrastructure, SWF. This is the largest single architectural consequence in the project's history, larger in code surface than the time migration itself.

## Part A — Cabinet (MASTER SEQUENCE STEP 1) — DONE (2026-07-30)

**Result: implemented exactly as scoped, including the explicit "defer 3 of 6 portfolios" instruction**
— see "Cabinet (Political Systems Overhaul Part A)" in `CLAUDE.md` for the full writeup. Finance/
Treasury, Interior/Justice, and Health & Social Affairs are live (chosen for having the
best-understood existing channels to audit and fold into — CollectionEfficiency, Crime & Justice's
gap-based target formulas, PovertyRate's reduction terms); Foreign Affairs, Defense, and Economy/
Trade & Industry are deliberately not yet defined in `CabinetPortfolio` itself, not just hidden in the
UI, mirroring `SectorType`'s own "4 now, 4 later" history. Both mechanics from this section's own spec
are implemented: a passive competence bias per portfolio (landing on CollectionEfficiency at
point-of-use, and on CrimeIndex/PovertyRate's existing gap-based target formulas, inside their
existing combined-ceiling clamps) and an interactive decision layer (18 decisions across 3 portfolios
x 3 philosophies, 2 response options each, genuinely different scenario text per philosophy, not the
same text with a different number). Validated via the full 28-combination real-Unity matrix (zero new
anomaly types) plus a dedicated `cabinetstress` scenario appointing the highest-competence minister in
each portfolio and auto-resolving every fired decision with its most extreme option — see CLAUDE.md
for the specific numbers. Reshuffle costs a flat 2-point ApprovalRating hit. UI, candidate picker, and
decision modal all match this section's own spec, confirmed via screenshot-driven smoke test
including the Advance-Turn-blocked-while-pending gate.

Six generic portfolios (deliberately generic rather than replicating any one real country's exact structure): **Finance/Treasury, Foreign Affairs, Defense, Interior/Justice, Health & Social Affairs, Economy/Trade & Industry.**

Each portfolio's competence effect lands on an existing system, folded into that system's existing combined ceiling where one exists — Finance nudges tax-collection/budget efficiency (audit the Fiscal Reaction Function/CollectionEfficiency code first, may be safer to land somewhere more contained), Foreign Affairs nudges Trade or ApprovalRating's international sensitivity, Defense nudges Defense spending's existing effect or ApprovalRating, Interior/Justice folds into Crime & Justice's existing channels, Health & Social Affairs folds into Welfare/Healthcare's existing channels, Economy/Trade & Industry nudges Sector performance or GDP growth (MUST fold into PotentialGrowthRate's existing ceiling).

**Interactive mechanic**: reuse the existing EventSystem architecture. Each appointed minister has their own small decision pool tied to portfolio AND personality (mirroring FedChairPhilosophy's differentiation — a "Reformist" vs. "Hardline" Interior Minister should generate genuinely different scenarios, not just a different number). Roll probabilistically per minister (~12%/period baseline, tune per-minister). A decision = short scenario + 2-3 response options, each with its own bounded, ceiling-audited effect.

**Content-authoring warning**: 6-8 roles × 2-3 candidates × multiple decisions each is a real content burden. Build 2-3 portfolios with real, fully-realized content first (mirroring the Sectors 4→8 pattern), prove the pattern feels right, then expand.

**Reshuffle**: player can replace a minister anytime; costs a modest ApprovalRating hit (reuses the existing approval-cost pattern).

**UI**: Cabinet tab, six portfolios, candidate picker mirroring Fed Chair's, decision-event modal reusing the BREAKING banner's visual weight.

**Validate**: full scenario matrix plus a dedicated stress scenario cycling every minister decision option, confirming none push a shared ceiling past its bound.

## Part B — Parliament (MASTER SEQUENCE STEPS 4 and 5; BLOCKED until Continuous Time Phase 0)

**Parties**: small number of original, generic, clearly-fictional archetypes per country (e.g. "Progressive Alliance," "Conservative Union") — never real party names.

**Seats**: derive from ApprovalRating plus bounded inertia/randomness — exact formula is an Open Question, worth designing once the pilot's overall flow is proven rather than guessed at now.

**The gated-legislation interaction model** (uniform across every tab once rolled out past the pilot):
1. Sliders/toggles remain DRAFT values — adjusting costs nothing, no vote needed just to experiment.
2. Player explicitly INTRODUCES a draft as a bill via a dedicated action, separate from adjusting sliders.
3. Bill enters the process defined by Continuous Time's day-based mechanic (introduction → committee/debate → vote), taking real in-game days/weeks.
4. Pass/fail depends on projected seats relative to the bill's alignment with current party composition (formula: Open Question).
5. PASS: new values become standing/in-effect, replacing the previous legislated values.
6. FAIL: previous standing values remain; draft isn't lost (can revise and reintroduce); costs a modest ApprovalRating hit.

**Strong recommendation, implement as default**: Federal Reserve and Eurozone rate decisions are EXEMPT from parliamentary gating — mirrors real central bank independence, and this game already deliberately built Fed Chair/Eurozone around bounded influence rather than direct political control for exactly this reason. Reverse only on Elias's explicit override.

**UI**: Parliament tab, hemicycle seat visualization (same node-placement math as the Policy Web's circular layout, arranged as a half-circle). Every gated tab needs a visible "Standing (legislated)" value alongside the "Draft (proposed)" value once rolled out to it.

**Rollout discipline**: PILOT on Tax Policy only first (master sequence step 4) — well-understood, clean implement/adjust/remove semantics already in place. Full validation matrix on the pilot before touching any other tab. Only then (step 5) roll out to the remaining seven.

## Part C — UI/graph restyling and political visualization (MASTER SEQUENCE STEP 2)

- **Graph restyling**: clearer threshold/target lines where relevant (debt comfortable-level, NAIRU), "last N changes" pagination. Reuses GraphRenderer.
- **Political compass**: grounded in this game's OWN real, already-tracked data, not invented ideology labels — e.g. one axis from average tax rate + spending level, another from average sector Regulation + Welfare generosity.
- **Demographic pie charts**: build from data that already exists (Population, DependencyRatio, sector employment shares, spending/tax breakdowns, election vote share once Parliament exists). Ethnicity/religion breakdowns are explicitly OUT OF SCOPE — not tracked anywhere in this game's data model; would need its own real-data-sourcing decision as a separate future item, not a restyling task.

**Validate**: single-scenario smoke check (pure UI/visual).

---

## Resolved Open Questions (from Roadmap Rounds 1-3 — historical, no action needed)

1. **Economic Sectors feedback**: Resolved INTEGRATE. Implemented as bounded nudges onto PotentialGrowthRate/Unemployment under an all-sources ceiling (MaxTotalPotentialGrowthAdjustment = 1.0). Real-Unity confirmed, growth rate observed pinned exactly at the ceiling under worst-case stress — direct evidence it binds correctly. Full detail: CLAUDE.md "Sector Integration."
2. **Infrastructure ConditionIndex feedback**: Resolved FEED BACK. Threshold-based drag on PotentialGrowthRate, reconciled with the pre-existing Infrastructure-spending nudge under one combined ceiling (0.75). Real-Unity confirmed. Full detail: CLAUDE.md "Infrastructure Feedback."

## Open Questions (live — add new entries here as they come up; do not resolve silently)

- **Parliament seat calculation formula** — deferred by design; worth a real pass once the pilot's overall flow is proven.
- **Cabinet appointment confirmation** — should appointing a minister also require a parliamentary vote, or does the player retain unilateral appointment power? Not yet decided.
- **SWF emergency drawdown fast-track** — if SWF drawdown becomes subject to the same gating as everything else, a genuine emergency response could get stuck behind a multi-week process, undermining its purpose. Worth an exemption similar to the Fed/Eurozone carve-out. Not yet decided.
- **Real reporting lag for data releases** (Continuous Time Migration) — optional realism refinement, not required for a first pass.
- **Exact StatHistory bucket scheme** (Continuous Time Migration Phase 0) — multiple reasonable designs exist; decide during implementation.

---

## When Elias returns to this document

- Check the Master Sequence section — confirm which step is actually in progress or next, don't assume.
- Check Open Questions first.
- Review the commit log — each step should be its own commit(s), validation results in the message or CLAUDE.md.
