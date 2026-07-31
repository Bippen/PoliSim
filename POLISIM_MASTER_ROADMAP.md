# PoliSim — Master Roadmap

This replaces three previously-separate standing documents (`ROADMAP_BRIEF.md`, `CONTINUOUS_TIME_MIGRATION.md`, `POLITICAL_SYSTEMS_OVERHAUL.md`), which had grown real dependencies on each other without being coordinated in one place. Read this in full before starting anything.

---

## Non-negotiable working discipline (applies to everything below, no exceptions)

1. **Real Unity is the standard of truth, not the standalone harness.** It has been wrong about project state multiple times this project (a stale swing threshold, an interest-rate crash mischaracterized as noise, a debt trajectory that flatly contradicted real Unity). Use it for fast iteration only. Before considering *anything* done, validate via `BatchSimulationRunner` against real Unity (`G:\UNITY\Unity Hub\6000.5.4f1\`) at both 100 and 500 turn horizons (or their day-equivalent, once the continuous-time migration changes the unit).
2. **Watch for the six failure patterns already seen repeatedly**: turn-1 discontinuities, oscillation, unbounded/compounding growth, bimodal attractors, and two new ones (both new as of Continuous Time + Parliament + Cabinet/Foreign-Policy coexisting, both surfaced investigating the SAME reported live-play freeze):
   - **Background/timed state mutation vs. active UI interaction** — a background system (a bill resolving, or any future timed/probabilistic mechanic) mutating live state that a GUILayout control is reading, on a day/frame the player has an active multi-frame drag in progress on that exact control. GUILayout allocates control IDs positionally, not by a stable key, so a control disappearing or a preceding control's count changing mid-drag is a documented Unity IMGUI hang/desync trigger, especially inside a ScrollView — and it's invisible to `BatchSimulationRunner`, which applies policy decisions programmatically and never drives real OnGUI/mouse-drag events, so no batch run can ever catch it. First hypothesized in the Tax Policy tab (Master Sequence step 4 pilot) when a pending TaxBill could resolve while the player was mid-drag on a rate slider; hardened there via the stable-control-layout pattern (see `GameController.DrawTaxPolicy`'s doc comment, commit `adb34ae`) regardless — every control a gated tab can ever draw renders every frame, in the same order, with "not currently applicable" expressed via `GUI.enabled = false` (composed with, not clobbering, any ambient enabled state) rather than by omitting or swapping the control. **Caveat, recorded honestly**: this fix did NOT resolve the reported freeze — Elias reproduced it again under the same conditions after commit `adb34ae`. The pattern and fix are still real and worth keeping (every one of the seven remaining tabs gains this exact same theoretical exposure once Master Sequence step 5 wires them into the draft/bill/vote model), but it was not the actual trigger of the original report. See the next pattern for what the investigation found instead.
   - **A legitimately time-blocking decision with no globally-visible indicator** — Fed Chair term appointment, a Cabinet decision, and a Foreign Policy meeting all correctly pause `GameController.Update`'s day-loop (every gate is checked correctly - this is NOT a simulation bug), but each one's actual resolution UI (the Fed Chair candidate picker, `DrawCabinetDecisionModal`, `DrawForeignPolicyMeetingModal`) renders ONLY inside its own specific tab's draw call - never globally. A player on any other tab (e.g. Tax Policy) when one of these fires sees simulated days silently stop advancing with no visible cause - indistinguishable from a hang. Before the fix, `DrawCalendarAndSpeedControls`'s always-visible status line (the one piece of UI pinned outside the scroll view on every tab) named the reason for Fed Chair and Cabinet only, in a modest, easy-to-miss label style, and said NOTHING for a pending Foreign Policy meeting - the one of the three statistically most likely to fire early in a fresh session, since it rolls per DAY (~1% chance) rather than per 121-day TURN like the other two. Fixed by escalating that line to the same bold/orange `_eventBannerStyle` used for the dashboard's own BREAKING banner whenever ANY of the three is pending, always naming which one and which tab resolves it - still exactly one Label control either way, per the stable-control-layout pattern above. This is a genuine UX gap, not a code crash: every future interrupt/decision system (gated legislation on the remaining seven tabs very much included) needs its "something needs your attention" state represented somewhere visible from every tab, not only on the tab where it originated.

   Assume a new mechanic is guilty of all six until the full-horizon batch run (for the first four) and direct live-Editor confirmation (for the last two, which batch runs cannot exercise) prove otherwise.
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
- **Master Sequence step 2 (Political Systems Overhaul Part C — UI/graph restyling and political visualization): DONE (2026-07-30).** Graph threshold/target lines (NAIRU, comfortable debt level) and "last N changes" pagination (`StatHistory.MaxEntries` raised 50 → 250), a political compass (auto-scaled to observed variance after a first-pass clustering bug), and five demographic pie charts — see "UI/Graph Restyling and Political Visualization" in `CLAUDE.md` for the full writeup, the two bugs found and fixed during the UI smoke test, and validation (single-scenario smoke check, zero new anomaly types).
- **Master Sequence step 3 (Continuous Time Migration Phase 0 — calendar, speed control, short-term gameplay scaffolding): DONE (2026-07-30).** Real in-game calendar with Pause/1x/2x/3x speed controls automatically firing the existing, unchanged 121-day turn cadence; a selectable-horizon live Policy Preview; multi-resolution `StatHistory`; and one small Foreign Policy Meetings interrupt slice (law-passing and "ongoing-process budgets" both explicitly deferred/superseded) — see "Continuous Time Migration Phase 0 (Master Sequence step 3)" in `CLAUDE.md` for the full writeup, the tick-equivalence proof, and validation (100-turn smoke check, UI screenshot smoke test).
- **Continuous Time Migration Phases 1-5: not started.** Full plan below.
- **Master Sequence step 4 (Political Systems Overhaul Part B, PILOT — Tax Policy tab only): DONE (2026-07-30).** Four generic fictional party archetypes with seats derived from ApprovalRating (bounded inertia plus jitter — a stated proposal resolving this item's own Open Question); the full draft → introduce → 21-day wait → pass/fail flow gates Tax Policy specifically (a passed TaxBill becomes the new standing rates, a failed one costs a modest approval hit and isn't lost); pass/fail scored via seat-weighted FiscalStance alignment against the bill's net direction (a second stated proposal). Federal Reserve/Eurozone exemption needed zero new code. Validated via the full 30-combination real-Unity matrix (15 scenarios × 100/500 turns, including a new worst-case `parliamentstress` scenario — zero hard anomalies, zero USA-specific anomalies) plus a screenshot smoke test — see "Parliament PILOT (Political Systems Overhaul Part B), Master Sequence step 4" in `CLAUDE.md` for the full writeup.
- **Political Systems Overhaul Part B, full rollout (Master Sequence step 5): plan REVISED (2026-07-31), 5a DONE (2026-07-31), 5b in progress.** The original "same uniform per-tab bill pattern rolled out to all seven remaining tabs" plan is superseded — Elias confirmed a more realistic, better-specified three-tier design (annual omnibus budget bill on each country's real fiscal-year date, plus a standalone-bill mechanism reused for both new/removed programs and non-budget policy changes) with an explicit six-phase build order, 5a through 5f, aesthetic restyling deliberately last. **5a (real per-country fiscal-year dates + the mandatory pause hook) is DONE and confirmed via live-Editor screenshots**: the budget-process banner fires correctly on the real fiscal date with honest placeholder wording, and Acknowledge correctly resumes time (re-confirmed a week later at October 8, still ticking normally at 3x speed) — see "Master Sequence step 5a (Political Systems Overhaul Part B, full rollout)" in `CLAUDE.md` for the full writeup. Full spec for 5b-5f in Part B below.
- **No Round 4 has been scoped.** Per the sequencing below, don't scope one yet — new features should be built against the post-Parliament interaction model, not the current one, to avoid retrofitting.

---

## THE MASTER SEQUENCE — work this list top to bottom, do not skip ahead

This is the one authoritative order, replacing whatever each original document separately suggested. It exists because Political Systems Overhaul Part B depends on Continuous Time Phase 0, and because building new Roadmap features or converting existing systems to daily granularity while Parliament's gating is mid-rollout would mean touching the same code for two different reasons at once — exactly the kind of overlap this project's discipline exists to avoid.

1. **Political Systems Overhaul — Part A (Cabinet). DONE (2026-07-30) — see "Cabinet (Political Systems Overhaul Part A)" in `CLAUDE.md`.** No dependencies. Full spec in Part A below.
2. **Political Systems Overhaul — Part C (UI/graph restyling). DONE (2026-07-30) — see "UI/Graph Restyling and Political Visualization" in `CLAUDE.md`.** No dependencies. Full spec in Part C below.
3. **Continuous Time Migration — Phase 0 (calendar, speed control, short-term gameplay scaffolding). DONE (2026-07-30) — see "Continuous Time Migration Phase 0 (Master Sequence step 3)" in `CLAUDE.md`.** No dependencies beyond what already exists. Full spec below. Keeps all existing economic math at its current cadence — this phase is purely the calendar/UI layer.
4. **Political Systems Overhaul — Part B, PILOT ONLY (Tax Policy tab). DONE (2026-07-30) — see "Parliament PILOT (Political Systems Overhaul Part B), Master Sequence step 4" in `CLAUDE.md`.** Depends on step 3. Prove the full draft → introduce → vote → pass/fail flow end-to-end on one tab before touching any other. Full spec below.
5. **Political Systems Overhaul — Part B, full rollout** — REVISED (2026-07-31): not a uniform per-tab repeat of the pilot, but a three-tier bill design (annual omnibus budget, standalone program add/remove, standalone non-budget policy) built in six sub-phases, 5a through 5f. Only once step 4 is validated (it is). **5a DONE (2026-07-31, confirmed via live-Editor screenshots), 5b in progress.** Full spec in Part B below.
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

## Phase 0 — Calendar, speed control, short-term gameplay scaffolding (MASTER SEQUENCE STEP 3) — DONE (2026-07-30)

1. Real in-game calendar date, advancing daily. Pause/1x/2x/3x speed controls.
2. The EXISTING turn-cadence economic tick fires automatically every 121 in-game days, unchanged internally. Proves the calendar layer works on trusted math before any translation begins.
3. Redesign the UI around this — remove manual "Advance Turn," replace with date + speed controls. Redesign the live Policy Preview (effect-per-day plus a selectable-horizon projection).
4. StatHistory needs multi-resolution storage (raw daily + aggregated weekly/monthly/quarterly buckets) — daily data alone over "last 50 entries" would show nothing meaningful for GDP-scale trends.
5. Build short-term gameplay scaffolding: ongoing-process budgets, a decisions/interrupts system, foreign policy meetings, and — **superseded by Political Systems Overhaul Part B once both are ready** — a law-passing mechanic where legislation takes real in-game days/weeks to move through stages. Do not build a competing version of this once Part B exists; Part B's design is authoritative for law-passing specifically.
6. Validate: single-scenario smoke check (no economic math touched) plus direct confirmation the automatic 121-day tick matches a manually-clicked turn exactly.

**Result**: items 1-4 built as scoped. Item 5 was treated as a menu of candidate systems, not three
mandatory builds: law-passing was skipped entirely (per this item's own "supersedes"/"do not build a
competing version" instruction), "ongoing-process budgets" was explicitly deferred as a named open
item rather than invented on the spot, and exactly one small proof-of-pattern interrupt slice (Foreign
Policy Meetings) was built, reusing Cabinet's decision-modal pattern. Item 6 validated: a 100-turn
`BatchSimulationRunner` smoke check completed cleanly (pre-existing ambient warnings only, zero new
anomaly types), and tick-equivalence was proven via `git diff` (confirming `AdvanceTurn()`'s body is
byte-for-byte unchanged - only its call site moved) plus a throwaway Edit-mode diagnostic confirming
the new `AdvanceDay()` boundary arithmetic fires at exactly every 121st day with zero economic side
effects of its own. Full writeup: "Continuous Time Migration Phase 0 (Master Sequence step 3)" in
`CLAUDE.md`.

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

## Part B — Parliament (MASTER SEQUENCE STEPS 4 and 5; step 4 DONE, step 5 plan REVISED 2026-07-31, 5a DONE, 5b in progress)

**Step 4 (PILOT, Tax Policy only) result: DONE (2026-07-30).** Both Open Questions below resolved as
stated proposals, not silent guesses: seats derive from ApprovalRating (bounded inertia + jitter, see
CLAUDE.md for the exact per-archetype constants) and pass/fail is scored via seat-weighted FiscalStance
alignment against the bill's net direction. The gated-legislation model (draft → introduce → 21-day
wait → pass/fail) is live on Tax Policy only; Fed/Eurozone exemption required no code (the interest-rate
lever was never a gated tab). Validated via the full 30-combination real-Unity matrix plus a screenshot
smoke test — full writeup: "Parliament PILOT (Political Systems Overhaul Part B), Master Sequence step
4" in `CLAUDE.md`. Step 5 (full rollout, revised three-tier design — see "Step 5, full rollout — REVISED
DESIGN" below): 5a DONE (2026-07-31, confirmed via live-Editor screenshots), 5b (Budget Process
full-screen UI shell) in progress.

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

### Step 5, full rollout — REVISED DESIGN (2026-07-31), supersedes the original plan below

The original step 5 plan (immediately below this subsection, kept for historical record) was "roll the pilot's single uniform draft → introduce → vote → pass/fail pattern out to the remaining seven tabs unchanged." Elias has since confirmed a more realistic, better-specified design in detail. **This subsection is authoritative for step 5; the plan below it is superseded and must NOT be built.**

**Three bill tiers, not one uniform pattern:**

1. **Annual Budget** — every EXISTING program's rate/amount change (the Tax Policy pilot's own sliders, plus Spending, Welfare, Infrastructure, and SWF rate/allocation changes) bundles into ONE omnibus bill per country, voted on that country's own real fiscal-year date: USA October 1; Germany, France, Italy, Poland, Sweden January 1 (source: real government fiscal-year conventions). USA is the only player-facing country under this design, so only the USA's own annual budget process triggers a mandatory pause — the other five countries are AI-controlled and resolve their own annual budgets automatically, no player-facing pause needed for them.
2. **Standalone bills — program add/remove**: introducing or removing a program or tax type entirely (new/removed `TaxLine`s, `WelfareProgram`s) is its own individual bill with its own vote, not tied to the annual budget date — can be introduced anytime.
3. **Standalone bills — non-budget policy**: minimum wage, sentencing/bail/drug policy, sector subsidy/regulation/tax-credit/research-grant/deregulation dials, tariffs. Same individual-bill pattern as tier 2, reusing the SAME mechanism — do not build a second standalone-bill system for this tier.

**Live support estimate**: extend the Tax Policy pilot's existing seat-weighted alignment scoring (`ParliamentSystem.GetBillDirection`/`WouldBillPass`) to recompute and display continuously as the player edits ANY draft — budget or standalone — not just after clicking Introduce. This is the same proven formula recomputed more often, not new scoring logic.

**Mandatory budget pause**: extend the EXISTING global pending-decision banner (`GameController.DrawCalendarAndSpeedControls`, built fixing the Foreign Policy Meeting visibility gap — see POLISIM_MASTER_ROADMAP.md's working discipline pattern 6) to also cover "the USA's annual budget process is open and unresolved," alongside its existing Fed Chair/Cabinet/Foreign-Policy-meeting conditions. Do not build a fourth, separate, ad-hoc pause-check system parallel to the other three — same gate, same banner, one more condition.

**CRITICAL — the stable-control-layout lesson applies MOST to this feature specifically.** The Budget Process screen is a big, multi-slider UI with a continuously-recomputing live vote estimate while the player is actively dragging — exactly the interaction shape that caused the real freeze investigated after step 4 (see working discipline pattern 5/6 above). Build it with stable control counts/order from the FIRST draft, per `GameController.DrawTaxPolicy`'s now-documented stable-control-layout template — do not retrofit this after a freeze a second time.

**Sequencing — build in this exact order, one commit per phase, full validation matrix for anything touching bill/vote logic:**
- **5a. DONE (2026-07-31).** Real per-country fiscal-year dates, plus the mandatory pause hook (extends the existing pause-gate pattern — see Mandatory budget pause above). Confirmed via live-Editor screenshots: the budget-process banner fires correctly on the real fiscal date with honest placeholder wording ("Budget Process screen not built yet - acknowledge below to continue for now"), and Acknowledge correctly resumes time - re-confirmed a week later (October 8) still ticking normally at 3x speed, i.e. the day-loop genuinely resumed, not just a one-frame fluke. Full writeup: "Master Sequence step 5a (Political Systems Overhaul Part B, full rollout)" in `CLAUDE.md`.
- **5b. IN PROGRESS.** The Budget Process full-screen UI shell (left categories / center line-items / right live summary, per Elias's reference screenshots' LAYOUT only, not their dated visual style) — consolidates the existing Tax/Spending/Welfare/Infrastructure/SWF rate sliders onto one screen. No new bill logic yet.
- **5c.** Wire the omnibus annual budget bill plus the live vote estimate.
- **5d.** Standalone bill mechanism for new/removed programs AND non-budget policy tabs (tiers 2 and 3), reusing 5c's live-estimate pattern.
- **5e.** Tab/IA consolidation into 7 tabs: Statistics (Recent Turns, World Map, graphs), Decisions (pending Foreign Policy/Cabinet/bill-vote interrupts), Demographics (population/pie charts), Tax, Spending (both now largely folded into the Budget Process screen from 5b — these tabs may become entry points into it rather than separate content), Policy/Laws (standalone bills from 5d), Politics (Parliament/Compass/Cabinet). Only do this once 5a-5d are stable — don't reorganize navigation around a mechanic that's still changing.
- **5f.** Aesthetic restyling pass (reference image 1: rounded cards, dark theme, big-number/small-label hierarchy, progress-bar visualizations, generous spacing) — LAST, applied to the final consolidated 7-tab structure, not to tabs about to be merged/removed. Deliberately last because restyling a screen that's still being consolidated/rewired means restyling it twice; the navigation and bill mechanics need to stop moving first.

**Open tie-in**: the Annual Budget tier explicitly includes SWF rate/allocation changes — this sharpens the existing "SWF emergency drawdown fast-track" Open Question below into something 5c/5d actually needs an answer to, not just a hypothetical. Resolve it before SWF is wired into the omnibus bill, not after.

#### Original step 5 plan (SUPERSEDED 2026-07-31 — historical record only, do not build)

**Rollout discipline**: PILOT on Tax Policy only first (master sequence step 4) — well-understood, clean implement/adjust/remove semantics already in place. Full validation matrix on the pilot before touching any other tab. Only then (step 5) roll out to the remaining seven, using the exact same uniform draft → introduce → vote → pass/fail pattern the pilot used, unchanged per tab.

## Part C — UI/graph restyling and political visualization (MASTER SEQUENCE STEP 2) — DONE (2026-07-30)

**Result: implemented exactly as scoped** — see "UI/Graph Restyling and Political Visualization" in
`CLAUDE.md` for the full writeup. `StatHistory.MaxEntries` raised 50 → 250 (a bounded retention
increase, not new tracked data) so pagination has real older data to page into; `GraphRenderer` gained
an optional dashed threshold-line parameter (wired into Unemployment → NAIRU and Debt-to-GDP →
comfortable level, the two graphs with an obvious single reference value) and internal Prev/Next
pagination slicing its own 50-turn window from the larger retained history. `PoliticalCompassRenderer`
and `PieChartRenderer` (both new, procedurally-drawn) plot all six countries and five demographic
breakdowns respectively, on a new "Compass & Demographics" tab. Two real bugs were found via
screenshot smoke test and fixed before shipping: the compass's first pass used a fixed 0-100 axis
range, which clustered all six countries' modest real policy variance into an illegible overlapping
clump — fixed by auto-scaling both axes to the observed min/max (the same philosophy `GraphRenderer`'s
own Y-axis auto-scaling already uses) plus a label-decluttering pass; and an apparent "half-circle" pie
chart in one screenshot was verified (via a second, scrolled screenshot) to be scroll cropping, not a
real rendering defect, before any unnecessary fix was attempted.

- **Graph restyling**: clearer threshold/target lines where relevant (debt comfortable-level, NAIRU), "last N changes" pagination. Reuses GraphRenderer.
- **Political compass**: grounded in this game's OWN real, already-tracked data, not invented ideology labels — e.g. one axis from average tax rate + spending level, another from average sector Regulation + Welfare generosity.
- **Demographic pie charts**: build from data that already exists (Population, DependencyRatio, sector employment shares, spending/tax breakdowns, election vote share once Parliament exists). Ethnicity/religion breakdowns are explicitly OUT OF SCOPE — not tracked anywhere in this game's data model; would need its own real-data-sourcing decision as a separate future item, not a restyling task.

**Validate**: single-scenario smoke check (pure UI/visual). **Done: 100-turn baseline via `BatchSimulationRunner`, 74 anomalies, all the pre-existing "swung X% in one turn" ambient-noise pattern, zero new anomaly types.**

---

## Resolved Open Questions (from Roadmap Rounds 1-3 — historical, no action needed)

1. **Economic Sectors feedback**: Resolved INTEGRATE. Implemented as bounded nudges onto PotentialGrowthRate/Unemployment under an all-sources ceiling (MaxTotalPotentialGrowthAdjustment = 1.0). Real-Unity confirmed, growth rate observed pinned exactly at the ceiling under worst-case stress — direct evidence it binds correctly. Full detail: CLAUDE.md "Sector Integration."
2. **Infrastructure ConditionIndex feedback**: Resolved FEED BACK. Threshold-based drag on PotentialGrowthRate, reconciled with the pre-existing Infrastructure-spending nudge under one combined ceiling (0.75). Real-Unity confirmed. Full detail: CLAUDE.md "Infrastructure Feedback."

## Open Questions (live — add new entries here as they come up; do not resolve silently)

- **Cabinet appointment confirmation** — should appointing a minister also require a parliamentary vote, or does the player retain unilateral appointment power? Not yet decided.
- **SWF emergency drawdown fast-track** — NOW LOAD-BEARING, not just hypothetical: Master Sequence step 5's revised design (see Part B above) explicitly folds SWF rate/allocation changes into the annual omnibus budget bill, so a genuine emergency drawdown could get stuck behind that country's next fiscal-year vote (up to a year away) unless this is resolved before 5c/5d wire SWF into the bill. **Recommendation (2026-07-31), pending Elias's confirmation**: emergency SWF drawdown becomes a standalone bill — the SAME tier 2/3 mechanism 5d already builds for new/removed programs and non-budget policy — not bundled into the annual budget, and NOT fully exempt like the Fed/Eurozone carve-out. Reasoning: real governments handle fiscal emergencies via expedited votes, not zero-oversight unilateral action; Norway's own GPFG withdrawal is itself an ordinary budget-process matter, not a central-bank-style independent decision, so a full exemption would overstate SWF's real-world independence. This needs zero new mechanism — it's exactly 5d's standalone-bill pattern, reused. Not yet confirmed — do not build against this until Elias signs off; still resolve before implementing 5c/5d.
- **Real reporting lag for data releases** (Continuous Time Migration) — optional realism refinement, not required for a first pass.
- **"Ongoing-process budgets"** (Continuous Time Migration Phase 0, item 5) — RESOLVED IN DESIGN (2026-07-31): this is now Master Sequence step 5's Annual Budget bill tier — see Part B above for the full design (real per-country fiscal-year dates, USA-only mandatory pause, the rest AI-resolved). Implementation is that plan itself (phases 5a-5c), not yet built.

---

## When Elias returns to this document

- Check the Master Sequence section — confirm which step is actually in progress or next, don't assume.
- Check Open Questions first.
- Review the commit log — each step should be its own commit(s), validation results in the message or CLAUDE.md.
