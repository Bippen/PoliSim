# PoliSim — Master Roadmap

This replaces three previously-separate standing documents (`ROADMAP_BRIEF.md`, `CONTINUOUS_TIME_MIGRATION.md`, `POLITICAL_SYSTEMS_OVERHAUL.md`), which had grown real dependencies on each other without being coordinated in one place. Read this in full before starting anything.

---

## Non-negotiable working discipline (applies to everything below, no exceptions)

1. **Real Unity is the standard of truth, not the standalone harness.** It has been wrong about project state multiple times this project (a stale swing threshold, an interest-rate crash mischaracterized as noise, a debt trajectory that flatly contradicted real Unity). Use it for fast iteration only. Before considering *anything* done, validate via `BatchSimulationRunner` against real Unity (`G:\UNITY\Unity Hub\6000.5.6f1\` - migrated from `6000.5.4f1` on 2026-08-01 after the older install became corrupted; see CLAUDE.md's "Real-Unity Validation is the Standard Path" for the full story) at both 100 and 500 turn horizons (or their day-equivalent, once the continuous-time migration changes the unit).
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
10. **REVERSED (2026-07-31), was a hard rule through Master Sequence step 5d**: visuals are now a MIXED procedural/sprite model, not "all procedural." Elias has explicitly approved imported sprite art for **icons, portraits, and background/menu textures specifically** — see `CLAUDE_DESIGN_ASSET_REQUEST_5E.md` for the concrete Master Sequence step 5e asset request this decision unblocks. **Stays procedural, unchanged, no exception**: all UI chrome/layout (`PoliSimTheme.cs`'s `RoundedBox`/`RoundedCard`/`Pill`/`Rule`/`TopAccent`/`LeftSpine` — pure `GUI.DrawTexture` rounded-rect/line geometry, no art asset, no reason to change) and every existing DATA visualization (`GraphRenderer`, `MapRenderer`, `PolicyWebRenderer`, `PoliticalCompassRenderer`, `HemicycleRenderer`) — none of these draw a "picture," they render real tracked simulation data, which is exactly what rule 5 ("ground new mechanics in real data") already protects; nothing about the icon/portrait decision touches that. **Becomes sprite-based**: one icon per `UiPalette.SystemArea` (policy area), one portrait per Cabinet minister candidate, one emblem per `PartyArchetype`, and background/menu textures — all sourced from Claude Design with the same origin-verification and security-review discipline already established for the first pack (Zone.Identifier mark-of-the-web check, full code/asset read-through before treating anything as trusted). This is a real, deliberate policy reversal, documented as such per this same working-discipline section's own precedent for recording a caveat/correction honestly rather than letting it look like silent drift - any FUTURE reversal of a standing rule must be recorded the same explicit way.
11. **Any new mechanic that nudges an existing tracked variable must fold into that variable's existing combined ceiling**, not add an uncounted new source — audit the actual ceiling code before adding a contributor, don't assume there's room.

---

## Where things stand right now

- **Roadmap Rounds 1-3: fully complete.** 15 items, all implemented, validated, and committed. Full detail lives in `CLAUDE.md`'s per-item sections (Expanded Event Pool, Labor Market Basics, Crime & Justice Basics, Economic Sectors, Sovereign Wealth Fund, SWF Drawdown, Expanded Sector Policies, Deeper Crime & Justice II, Expanded Economic Sectors II, Demographics Parts A & B). Both prior Open Questions (Sector Integration, Infrastructure Feedback) are resolved — see the Resolved Open Questions section near the bottom of this document for the short version.
- **Master Sequence step 1 (Political Systems Overhaul Part A — Cabinet): DONE (2026-07-30).** Only 3 of the 6 confirmed portfolios implemented this pass (Finance/Treasury, Interior/Justice, Health & Social Affairs), per Part A's own content-authoring warning — see "Cabinet (Political Systems Overhaul Part A)" in `CLAUDE.md` for the full writeup and real-Unity validation (28-combination matrix, zero new anomaly types, directional confirmation via a targeted diagnostic).
- **Master Sequence step 2 (Political Systems Overhaul Part C — UI/graph restyling and political visualization): DONE (2026-07-30).** Graph threshold/target lines (NAIRU, comfortable debt level) and "last N changes" pagination (`StatHistory.MaxEntries` raised 50 → 250), a political compass (auto-scaled to observed variance after a first-pass clustering bug), and five demographic pie charts — see "UI/Graph Restyling and Political Visualization" in `CLAUDE.md` for the full writeup, the two bugs found and fixed during the UI smoke test, and validation (single-scenario smoke check, zero new anomaly types).
- **Master Sequence step 3 (Continuous Time Migration Phase 0 — calendar, speed control, short-term gameplay scaffolding): DONE (2026-07-30).** Real in-game calendar with Pause/1x/2x/3x speed controls automatically firing the existing, unchanged 121-day turn cadence; a selectable-horizon live Policy Preview; multi-resolution `StatHistory`; and one small Foreign Policy Meetings interrupt slice (law-passing and "ongoing-process budgets" both explicitly deferred/superseded) — see "Continuous Time Migration Phase 0 (Master Sequence step 3)" in `CLAUDE.md` for the full writeup, the tick-equivalence proof, and validation (100-turn smoke check, UI screenshot smoke test).
- **Continuous Time Migration Phases 1-5: not started.** Full plan below.
- **Master Sequence step 4 (Political Systems Overhaul Part B, PILOT — Tax Policy tab only): DONE (2026-07-30).** Four generic fictional party archetypes with seats derived from ApprovalRating (bounded inertia plus jitter — a stated proposal resolving this item's own Open Question); the full draft → introduce → 21-day wait → pass/fail flow gates Tax Policy specifically (a passed TaxBill becomes the new standing rates, a failed one costs a modest approval hit and isn't lost); pass/fail scored via seat-weighted FiscalStance alignment against the bill's net direction (a second stated proposal). Federal Reserve/Eurozone exemption needed zero new code. Validated via the full 30-combination real-Unity matrix (15 scenarios × 100/500 turns, including a new worst-case `parliamentstress` scenario — zero hard anomalies, zero USA-specific anomalies) plus a screenshot smoke test — see "Parliament PILOT (Political Systems Overhaul Part B), Master Sequence step 4" in `CLAUDE.md` for the full writeup.
- **Political Systems Overhaul Part B, full rollout (Master Sequence step 5): plan REVISED (2026-07-31), 5a DONE (2026-07-31), 5b DONE (2026-07-31), 5c DONE (2026-07-31), 5d DONE (2026-07-31), 5e IN PROGRESS (Phase A DONE and confirmed via live-Editor click-through, Phase B starting, 2026-07-31) — 5e's scope was also revised to absorb 5f (combined tab/IA reorg + full sprite-based visual overhaul, see working discipline item 10's reversal) — see Part B's "5e implementation plan" for the full Phase A/B/C plan.** The original "same uniform per-tab bill pattern rolled out to all seven remaining tabs" plan is superseded — Elias confirmed a more realistic, better-specified three-tier design (annual omnibus budget bill on each country's real fiscal-year date, plus a standalone-bill mechanism reused for both new/removed programs and non-budget policy changes) with an explicit six-phase build order, 5a through 5f, aesthetic restyling deliberately last. **5a (real per-country fiscal-year dates + the mandatory pause hook) is DONE and confirmed via live-Editor screenshots**: the budget-process banner fires correctly on the real fiscal date with honest placeholder wording, and Acknowledge correctly resumes time (re-confirmed a week later at October 8, still ticking normally at 3x speed). **5b (the Budget Process full-screen UI shell) is also DONE and confirmed via live-Editor screenshots**, after two real layout bugs found in Elias's own screenshots and fixed in place - a header label clipping mid-word instead of wrapping, and the reused Live Policy Preview panel rendering catastrophically narrow (a first-attempt width cap turned out to be the actual binding constraint, not the Screen.width-based calculation it was meant to backstop). **5c (the omnibus BudgetBill retiring TaxBill, plus the live vote estimate) is also DONE**, confirmed via a structural diagnostic (`BudgetBillDiagnostic`: PASS/FAIL scoring, mandatory-pause hand-off, 21-day countdown, no re-pausing mid-countdown — all PASS) AND real live-Editor play: SWF and Welfare sliders dragged under active 3x-speed time advancement for nearly a year with zero freeze, a full introduce → wait → resolve cycle confirmed end-to-end, and two full fiscal-year cycles proven to both correctly reopen the mandatory pause (`FiscalYearRecurrenceDiagnostic`). Live play also caught two real bugs no automated matrix would have found: a global pending-decision banner that silently masked a Budget Process pause behind a simultaneous Foreign Policy pause (fixed — all active pause reasons now list together), and the total absence of any save/load system in the project (a lost SWF draft was first suspected as a bill-mechanism bug, diagnostics proved the mechanism correct across two years, and Elias confirmed the real cause was Unity being closed/reopened between setting the draft and the next fiscal date — now tracked as Master Sequence item 8). **5d (standalone tier-2 program add/remove bills plus the four standalone tier-3 non-budget policy bills) is also DONE**, confirmed via a structural diagnostic (`StandaloneBillsDiagnostic`: 21/21 PASS across all seven bill types, including the diagnostic's own first run catching a test-design bug, not a shipped one) AND live-Editor play: freeze-free dragging across multiple new tabs at 3x speed, live estimates updating correctly, a full welfare bill resolve cycle, and one real UI gap found and fixed live (the Tax/Welfare Implement/Remove rows had no live pass/fail estimate of their own, unlike every other bill tier, so a player could easily read a DIFFERENT bill's estimate and be surprised by the outcome). A follow-up question from that same live testing — "welfare bills always fail, tax bills always pass, is that a bug?" — turned out to be the vote math working correctly: Progressive Alliance and Conservative Union sit exactly tied at 32% each (identical `BaseSupportShare`, `ApprovalRating` near 50), canceling each other's `FiscalStance` pull, leaving Nationalist Front's smaller but purely negative lean as the actual swing vote — a real, now-documented emergent property of the archetype design, not a defect. See "Master Sequence step 5a/5b/5c/5d (Political Systems Overhaul Part B, full rollout)" in `CLAUDE.md` for the full writeup of all four. Full spec for 5e-5f in Part B below.
- **No Round 4 has been scoped.** Per the sequencing below, don't scope one yet — new features should be built against the post-Parliament interaction model, not the current one, to avoid retrofitting.
- **A save/load system is now a confirmed, real gap — see item 8 in the Master Sequence below.** Found while investigating a live-play anomaly (an SWF draft that never became standing across two fiscal years): this game has ZERO persistence anywhere (confirmed - no `PlayerPrefs`/`JsonUtility`/`BinaryFormatter`/any save mechanism exists in the codebase), so every Unity Editor/Play-mode restart silently discards ALL game state. Not yet scoped or started.

---

## THE MASTER SEQUENCE — work this list top to bottom, do not skip ahead

This is the one authoritative order, replacing whatever each original document separately suggested. It exists because Political Systems Overhaul Part B depends on Continuous Time Phase 0, and because building new Roadmap features or converting existing systems to daily granularity while Parliament's gating is mid-rollout would mean touching the same code for two different reasons at once — exactly the kind of overlap this project's discipline exists to avoid.

1. **Political Systems Overhaul — Part A (Cabinet). DONE (2026-07-30) — see "Cabinet (Political Systems Overhaul Part A)" in `CLAUDE.md`.** No dependencies. Full spec in Part A below.
2. **Political Systems Overhaul — Part C (UI/graph restyling). DONE (2026-07-30) — see "UI/Graph Restyling and Political Visualization" in `CLAUDE.md`.** No dependencies. Full spec in Part C below.
3. **Continuous Time Migration — Phase 0 (calendar, speed control, short-term gameplay scaffolding). DONE (2026-07-30) — see "Continuous Time Migration Phase 0 (Master Sequence step 3)" in `CLAUDE.md`.** No dependencies beyond what already exists. Full spec below. Keeps all existing economic math at its current cadence — this phase is purely the calendar/UI layer.
4. **Political Systems Overhaul — Part B, PILOT ONLY (Tax Policy tab). DONE (2026-07-30) — see "Parliament PILOT (Political Systems Overhaul Part B), Master Sequence step 4" in `CLAUDE.md`.** Depends on step 3. Prove the full draft → introduce → vote → pass/fail flow end-to-end on one tab before touching any other. Full spec below.
5. **Political Systems Overhaul — Part B, full rollout** — REVISED (2026-07-31): not a uniform per-tab repeat of the pilot, but a three-tier bill design (annual omnibus budget, standalone program add/remove, standalone non-budget policy) built in six sub-phases, 5a through 5f. Only once step 4 is validated (it is). **5a, 5b, 5c, and 5d DONE (2026-07-31); 5e IN PROGRESS (Phase A DONE, Phase B starting; scope now combined with 5f).** Full spec in Part B below.
6. **Resume Roadmap work (a new Round 4)** — only scope this once step 5 is done, so anything new is built directly against the gated-legislation model from day one.
7. **Continuous Time Migration — Phases 1 through 5** (the actual daily-granularity conversion of each system's math, safest-first, core macro engine last). This is deliberately positioned after the political-systems work — it's a separate concern (simulation granularity, not who can change policy) and touching the same files for two unrelated reasons in the same window is worth avoiding.
8. **NEW (2026-07-31) — Build a save/load system.** Not yet scoped, not yet sequenced into the numbered order above (appended here rather than renumbering 1-7, which are referenced extensively throughout this document and `CLAUDE.md`). **Recommendation, pending Elias's confirmation**: scope and build this before or alongside Round 4 (item 6) — Round 4 is already unscoped and is the natural next planning point, and building more features on top of an unpersisted game only compounds the amount of state a save system will eventually need to cover. Reasoning this is a real severity issue, not a nice-to-have: confirmed via direct investigation (zero `PlayerPrefs`/`JsonUtility`/`BinaryFormatter`/any persistence mechanism anywhere in the codebase) that every Unity Editor/Play-mode restart discards ALL game state silently, with no error or warning - and the amount of state that now matters has grown substantially since this was last a non-issue: Cabinet ministers and their competence/philosophy, Parliament seat composition, any pending TaxBill/BudgetBill and its DaysRemaining countdown, every draft dictionary across every gated tab, the calendar date itself, Fed Chair terms, SWF holdings - losing any of this on an ordinary restart is a real loss of play, not a cosmetic gap. This was the leading suspect for a live-play anomaly where an SWF draft never became standing across two observed fiscal-year cycles - **now confirmed as the actual cause**: Elias confirmed Unity was closed/reopened multiple times between setting the draft and the next fiscal date, and the underlying bill mechanism itself was independently proven correct across two full fiscal years via a targeted diagnostic (see CLAUDE.md's "Master Sequence step 5a/5b/5c" writeup). Needs its own design pass before implementation starts, not a guess: what serializes cleanly under Unity's own `JsonUtility` (which - like Unity's Inspector serialization generally - doesn't support `Dictionary<>` natively either, the same limitation already visible as `UAC1009` warnings on several existing fields, e.g. `PolicyDecision.TaxRateOverrides`/`SpendingLineChanges`/every Sector-override dictionary; `BudgetBill`'s own dictionaries would hit the same wall), whether a mid-cycle pending bill's DaysRemaining and a real save timestamp interact cleanly, and how much of `World`/`Country`'s current in-memory object graph can serialize as-is versus needs a dedicated save-data shape. Escalate format/scope decisions rather than guessing, per this document's own working discipline item 4.

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

## Part B — Parliament (MASTER SEQUENCE STEPS 4 and 5; step 4 DONE, step 5 plan REVISED 2026-07-31, 5a/5b/5c/5d DONE, 5e IN PROGRESS - Phase A DONE, Phase B starting, scope now combined with 5f)

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
- **5a. DONE (2026-07-31).** Real per-country fiscal-year dates, plus the mandatory pause hook (extends the existing pause-gate pattern — see Mandatory budget pause above). Confirmed via live-Editor screenshots: the budget-process banner fires correctly on the real fiscal date with honest placeholder wording ("Budget Process screen not built yet - acknowledge below to continue for now"), and Acknowledge correctly resumes time - re-confirmed a week later (October 8) still ticking normally at 3x speed, i.e. the day-loop genuinely resumed, not just a one-frame fluke. Full writeup: "Master Sequence step 5a/5b (Political Systems Overhaul Part B, full rollout)" in `CLAUDE.md`.
- **5b. DONE (2026-07-31).** The Budget Process full-screen UI shell (left categories / center line-items / right live summary, per Elias's reference screenshots' LAYOUT only, not their dated visual style) — consolidates the existing Tax/Spending/Welfare/Infrastructure/SWF rate sliders onto one screen. No new bill logic yet. Two real layout bugs found via Elias's own live-Editor screenshots and fixed in place before this was confirmed: a header description label clipping mid-word instead of word-wrapping (the 3-column row below it could push the outer container's computed width past the real screen edge, so the label's wrap boundary was inferred against an inflated width - fixed with an explicit width tied directly to the actual available width), and the reused Live Policy Preview panel rendering catastrophically narrow ("Estimated Effects" wrapping to a single character per line) - a first attempt gave it its native `Screen.width * LeftColumnWidthFraction` size but capped it at half the row's own budget "so the other two columns never collapse to nothing," and that cap turned out to be the actual binding constraint at ordinary window sizes too, confirmed via a temporary debug label reporting the real runtime pixel width rather than reasoning about the code again blind. Corrected: the preview panel keeps its natural width unconditionally, the other two columns get sane minimum widths instead of a share of leftover space, and a genuine narrow-window overflow is now handled explicitly via a horizontal scrollview around the row rather than silently starved. Full writeup: "Master Sequence step 5a/5b (Political Systems Overhaul Part B, full rollout)" in `CLAUDE.md`.
- **5c. DONE (2026-07-31).** Wired the omnibus annual budget bill (`BudgetBill.cs`, replacing the retired `TaxBill.cs`; covers Tax + Spending + Welfare + SWF in one bill) plus the live-updating vote estimate on the Budget Process screen. Structural correctness confirmed via a targeted diagnostic (`BudgetBillDiagnostic`): PASS scoring, FAIL scoring, the Phase 1 → Phase 2 mandatory-pause hand-off (blocks before Introduce, resumes after, never re-pauses during the 21-day countdown) — all PASS. Live-Editor validation went beyond "does it render": the Legislative Support estimate confirmed to update live while dragging any of the four categories' sliders; SWF and Welfare sliders specifically dragged during ACTIVE 3x-speed time advancement across nearly a full in-game year with zero freeze (the exact interaction shape working discipline pattern 5/6 exists to guard against); a full introduce → wait → resolve cycle confirmed end-to-end live. Separately, a live-play anomaly (an SWF draft that never became standing) prompted a second diagnostic (`FiscalYearRecurrenceDiagnostic`) proving the fiscal-year recurrence mechanism itself fires correctly across two full fiscal years — the mechanism was never buggy; Elias confirmed the real cause was Unity being closed/reopened between setting the draft and the next fiscal date (now tracked as save/load gap, Master Sequence item 8). The same investigation also surfaced and fixed a real bug: the global pending-decision banner (`GameController.DrawCalendarAndSpeedControls`) only ever showed ONE active pause reason, silently masking a Budget Process pause whenever a Foreign Policy meeting was ALSO pending — fixed to list every active reason together. Full writeup: "Master Sequence step 5a/5b/5c (Political Systems Overhaul Part B, full rollout)" in `CLAUDE.md`.
- **5d. DONE (2026-07-31).** Standalone bill mechanism for new/removed programs (tier 2: `TaxProgramBill`/`WelfareProgramBill`, one per program, introducible anytime, multiple different programs able to have bills pending concurrently) and non-budget policy (tier 3: `LaborPolicyBill`/`CrimeJusticePolicyBill`/`SectorPolicyBill`/`TradePolicyBill`, one bill per tab bundling that tab's dials together), reusing 5c's `WouldBillPass` core (refactored to take a plain direction float, shared by all seven bill types now in play) rather than inventing new scoring logic. A design fork confirmed via `AskUserQuestion` before implementation: tier 2 was rebuilt as a genuine anytime standalone bill, REPLACING the annual-bill path 5c had briefly folded implement/remove into (`BudgetBill` narrowed back to rate/generosity-only for already-implemented programs, matching the tier 1/tier 2 split this section's own spec originally described). Tier 3's non-fiscal dials score against the existing single FiscalStance axis via a stated sign-convention mapping, documented per bill type directly in `ParliamentSystem` (e.g. Sentencing Severity/Drug Policy/Border Enforcement/Deregulation read negative - conservative-coded; Police/Judicial Funding/Subsidy/Regulation/Tax Credits/Research Grants read positive - spending-like). Validated via a structural diagnostic (`StandaloneBillsDiagnostic`: 21/21 PASS across all seven bill types) AND live-Editor play, which found and fixed one real UI gap a headless diagnostic couldn't have caught: the Tax/Welfare Policy tabs' Implement/Remove rows had no live pass/fail estimate of their own, unlike every other bill tier, so a player could easily be looking at a DIFFERENT bill's estimate and be confused when the actual relevant bill resolved differently — fixed by adding a per-row live estimate matching the pattern every other tier already used. That same live testing also surfaced (and resolved, with no code change needed) a real question about the seat-weighted vote math itself — see this section's own closing note below. Full writeup: "Master Sequence step 5a/5b/5c/5d (Political Systems Overhaul Part B, full rollout)" in `CLAUDE.md`.

**Confirmed real behavior, not a bug (found via Elias's live 5d testing, 2026-07-31)**: with Progressive Alliance and Conservative Union tied at exactly 32% of seats each (their identical `BaseSupportShare` — meaning `ApprovalRating` is sitting right around 50, so neither has picked up any approval-driven bonus), their opposite-sign `FiscalStance` (+0.7 vs -0.7) cancels out exactly, leaving `CentristCoalition` (neutral, contributes nothing) and `NationalistFront` (smaller seat share but a purely negative lean) to actually decide the outcome. A net-negative seat-weighted alignment like this makes every contractionary bill (e.g. removing an implemented tax) pass and every expansionary bill (e.g. implementing a new welfare program) fail, regardless of which specific tax/program it is — the sign is fixed by current Parliament composition, not by the bill's own content. Worth knowing for anyone reading a "why did this bill fail" report in the future: check the Parliament tab's exact seat percentages and run the same weighted-sum arithmetic before assuming a scoring bug.
- **5e. SCOPE REVISED (2026-07-31) — now a combined tab/IA reorganization + full sprite-based visual overhaul, per Elias's explicit decision.** Originally scoped as tab/IA consolidation only (see "Original 5e scope" below, kept for the record), with the aesthetic pass held as a separate, later 5f. Elias has since decided 5e absorbs both: the tab/IA consolidation into 7 tabs (Statistics, Decisions, Demographics, Tax, Spending, Policy/Laws, Politics - unchanged from the original scope below) AND the visual overhaul (working discipline item 10's rule reversal above), sourced together via Claude Design rather than as two separate passes. **Current status: asset request answered and all 58 files (42 new + 8 renamed) imported into `Assets/Art/UI/` (2026-07-31) - security-reviewed, verified via a clean Unity batch-mode import (every file got a `.meta`, zero PNG import errors). `GameController.cs` still untouched - no rendering or tab-restructuring code written yet.** See `CLAUDE_DESIGN_ASSET_REQUEST_5E.md` for the full request/answers, and the three-phase implementation plan immediately below for how the actual code work will proceed. Original 5e scope, unchanged by this revision: consolidate into 7 tabs - Statistics (Recent Turns, World Map, graphs), Decisions (pending Foreign Policy/Cabinet/bill-vote interrupts), Demographics (population/pie charts), Tax, Spending (both now largely folded into the Budget Process screen from 5b — these tabs may become entry points into it rather than separate content), Policy/Laws (standalone bills from 5d), Politics (Parliament/Compass/Cabinet). Only reorganize navigation once 5a-5d are stable (they are) — don't reorganize navigation around a mechanic that's still changing.

### 5e implementation plan — three phases (confirmed by Elias, 2026-07-31)

Structure settles first using the EXISTING procedural rendering (no visual style changes) - THEN the
sprite reskin gets piloted on Statistics/Dashboard specifically - THEN rolled out to the rest once
proven. Written here before any code changes so it survives a session restart. Standing rules apply
throughout: one commit per phase (or per batch within Phase C), escalate genuine design forks rather
than guessing, never mark a phase done without a live screenshot.

#### Phase A — Tab/IA restructuring (no visual style changes yet). DONE (2026-07-31).

Reuses the existing tab-bar UI mechanics (color-coded per area, same interaction pattern, same
`DrawRightColumnTabButton` mechanics, now `DrawConsolidatedTabButton`) - only the grouping/navigation
changed in this phase, not the visual style (icons/sprites are Phase B/C's job, no `icon_*` texture is
drawn anywhere yet).

**Real tab audit (confirmed against `GameController.cs`'s actual `RightPanelTab` enum, not assumed -
18 tabs, matching Elias's own list exactly, none missing).**

**Old → new mapping - CONFIRMED (directly stated in this document's own original 5e scope text, or
require no judgment call):**

| Old tab (`RightPanelTab`) | New tab | Basis |
|---|---|---|
| `RecentTurns` | Statistics | Original 5e scope text names it explicitly. |
| `WorldMap` | Statistics | Original 5e scope text names it explicitly. |
| `TaxPolicy` | Tax | Retired as standalone - becomes an ENTRY POINT into the existing Budget Process screen (`DrawBudgetProcessTab`), opened at its Tax category. Confirmed via `DrawRightColumnTabs`'s own code comment: *"Budget Process consolidates Tax/Spending/Welfare/Infrastructure/SWF's existing content... those five tabs stay as independent entry points for now per the Master Sequence step 5 design, not removed until step 5e's own tab consolidation."* This IS that consolidation. |
| `SpendingPolicy` | Spending | Same as Tax above, opened at its Spending category. |
| `WelfarePolicy` | Tax or Spending (same screen) | Same mechanism - Welfare is one of `BudgetProcessCategory`'s 5 existing values, reachable from EITHER new entry point once inside the consolidated screen, not a separate top-level tab. |
| `SwfPolicy` | Tax or Spending (same screen) | Same mechanism - `BudgetProcessCategory.Swf`. |
| `LaborMarket` | Policy/Laws | Original 5e scope text: "Policy/Laws (standalone bills from 5d)" - `LaborPolicyBill` is exactly this. |
| `CrimeJustice` | Policy/Laws | Same - `CrimeJusticePolicyBill`. |
| `SectorPolicy` | Policy/Laws | Same - `SectorPolicyBill`. |
| `Cabinet` | SPLIT: Decisions + Politics | Original 5e scope text names Cabinet under BOTH "Decisions (pending... Cabinet... interrupts)" AND "Politics (Parliament/Compass/Cabinet)" - confirmed via code that `DrawCabinetTab` genuinely has two distinct pieces: the pending-decision modal loop (`GetPendingCabinetDecisions`/`DrawCabinetDecisionModal`) moves to Decisions, the portfolio/candidate-picker UI stays under Politics. |
| `CompassAndDemographics` | SPLIT: Politics + Demographics | Original 5e scope text separates "Compass" (under Politics) from "Demographics (population/pie charts)" as its own tab - this single existing tab's two halves (the Political Compass chart vs. the demographic pie charts) split accordingly. |
| `ForeignPolicy` | Decisions | Original 5e scope text names it explicitly, AND confirmed via code that the ENTIRE standalone tab's content is the pending-meeting interrupt (explanatory text + either the modal or "No meeting currently pending") - nothing left behind to place anywhere else. |
| `Parliament` | Politics | Original 5e scope text names it explicitly. Deliberately NOT also split into Decisions - unlike Foreign Policy/Cabinet, a pending bill never pauses time (only the ANNUAL budget process's Phase 1 does, and that's covered by the global banner already, not a tab), so there's no true "interrupt" here needing Decisions' attention pattern - Parliament's own "Pending Legislation" list (all 7 bill types) is informational, not blocking. |
| `BudgetProcess` | Tax or Spending (same screen) | This tab doesn't move anywhere new - it effectively becomes what "Tax" and "Spending" both open into (see `TaxPolicy` row above). Not retired so much as promoted to the thing the other five folded into. |
| `Trade` | SPLIT: Statistics + Policy/Laws | **RESOLVED by Elias (2026-07-31), confirmed as originally proposed.** The Trade Balance graph and per-partner import/export volume bars are informational (Statistics-flavored); the tariff bill status/live-estimate/Introduce action (`TradePolicyBill`, a genuine standalone bill from 5d) is exactly what "Policy/Laws (standalone bills from 5d)" describes. Implementation refinement (not a design fork): the per-partner override CONTROLS stay bundled with their own bars in one row rather than splitting a single row's rendering across two different tabs (a real UX regression - a player adjusting an override wants the volume bars right next to it) - so Statistics gets only the aggregate Trade Balance graph, and Policy/Laws gets the full per-partner section (bars AND override controls together). |
| `FederalReserve` | Politics | **RESOLVED by Elias (2026-07-31), confirmed as originally proposed.** A real political institution with its own active lever (interest rate, Fed Chair selection) that groups naturally with Parliament/Cabinet, even though the Fed/Eurozone exemption means it's never Parliament-gated. |
| `PolicyWeb` | Policy/Laws | **RESOLVED by Elias (2026-07-31), OVERRIDING the original recommendation (Statistics).** Elias's own reasoning: "it's a relationship/reference tool consulted while deciding what to change, closer to where bills get drafted than to a pure stats readout." |
| `Infrastructure` | Tax or Spending (same screen) | **RESOLVED by Elias (2026-07-31), confirmed as originally proposed.** Consistent with Welfare/SWF above - `DrawInfrastructureContent` is ALREADY reused verbatim inside Budget Process's own Infrastructure category, and the standalone tab has no independent lever of its own. |
| Does "Decisions" need the Budget Process mandatory-pause interrupt too? | Yes | **RESOLVED by Elias (2026-07-31), confirmed as originally proposed.** Elias's own reasoning: "any 'time is blocked until you respond' state belongs in the same place, not treated as an exception." Reuses the same `DrawBudgetBillStatusAndIntroduce` status+Introduce UI already built for Tax/Spending, shown in Decisions ONLY while `GetPendingBudgetProcess` is true (mirroring Foreign Policy/Cabinet's own "only appears while actually pending" pattern) - the ongoing per-bill countdown status stays under Tax/Spending, this is specifically the blocking Phase-1 moment. |

All five previously-escalated placement questions are now resolved - see Open Questions below, marked
closed. Phase A implementation proceeded using the mapping above in full, exactly as confirmed - the
`ConsolidatedTab`/`StatisticsCategory`/`PolicyLawsCategory`/`PoliticsCategory` enums and their dispatch
in `GameController.cs` match this table row for row. One extension beyond the five resolved items,
applying Elias's own stated general principle (see the Budget Process row above - "any 'time is blocked
until you respond' state belongs in the same place, not treated as an exception") rather than guessing
at something new: Fed Chair selection is ALSO a blocking interrupt (see `UpdateFedChairSelectionState`)
that was never one of the five items Elias was asked about - added to Decisions too
(`DrawFedChairSelectionModal`, extracted from Federal Reserve's own tab so both places render the exact
same UI), flagged clearly rather than silently added.

**Validation for Phase A: DONE.** `dotnet build` clean (0 errors, full output read - not just grepped
for "error"). The single-scenario automated smoke check (100-turn baseline via
`BatchSimulationRunner`) was attempted twice and abandoned - both attempts stalled inside Unity's own
cold-start asset-reimport/indexing phase (confirmed via process CPU/responsiveness checks: the process
was genuinely busy, not deadlocked, but never got past Editor startup to actually run the scenario),
the same category of infrastructure issue already documented earlier in this project's history as
unrelated to the code under test. Not retried a third time - Phase A's code changes are 100% UI-layer
(`GameController.cs` only; `ParliamentSystem.cs`/`SimulationManager.cs`/`MacroSystem.cs`/every
simulation file untouched), so the real risk here was always navigation/UI breakage, which only a live
click-through can verify anyway (a headless batch run never drives real `OnGUI`). **Confirmed via
Elias's own live-Editor click-through (2026-07-31)**: all 7 new tabs render correctly, every
sub-category selector (Statistics' 3, Policy/Laws' 5, Politics' 4) switches correctly, and the mapping
matches this section's own table exactly.

#### Phase B — Sprite reskin pilot: Statistics/Dashboard only

**Prerequisite check: DONE (2026-07-31), confirmed BEFORE any real Statistics/Dashboard rendering work
started, per Elias's own explicit instruction not to build on an unverified assumption.** The
icon-tinting helper (`UiPalette.DrawTintedIcon(Rect, Texture2D, Color)` - `GUI.color`-multiply tinting,
the exact mechanism the Claude Design asset pack's own README specifies, mirroring `HemicycleRenderer`'s
own existing per-seat dot-tinting idiom) is now real, permanent code in `UiPalette.cs`, not just planned.
Verified via a throwaway, fully isolated Editor-only test window (`Assets/Editor/IconTintingTest.cs`,
zero production-code changes, deleted after use per this project's own established convention) showing
3 real imported icons (`icon_area_fiscal`, `icon_area_infrastructure`, `icon_nav_statistics`) each tinted
3 ways (white/its own area color/dimmed grey). **Confirmed by Elias directly**: icon shapes clearly
visible in every cell, background stays genuinely transparent, and the three tint colors are visibly
different from each other - the core visual claim actually holds, not just assumed from the asset
pack's own README text. Real production usage (which texture reference mechanism Statistics' actual
tab-bar button and any other real call site uses - serialized Inspector fields vs. `Resources.Load` vs.
something else - `AssetDatabase.LoadAssetAtPath` only works in-Editor, so the throwaway test's own
loading method is NOT what production code will use) is still Phase B's own work below, not resolved by
this prerequisite check alone.

Apply the now-confirmed icon-tinting helper and `PoliSimTheme`/`PoliSimWidgets`' card/stat-tile/
threshold-bar primitives to the Statistics tab specifically - its own nav icon (`icon_nav_statistics.png`,
already imported) tinted appropriately in the tab bar, and its headline stats/graphs restyled using the
new widget patterns instead of raw `OnGUI.Label` layout. Highest-visibility screen, validates both
pieces of infrastructure (icon tinting + card widgets) together, in production code, before trusting
them anywhere else. Do NOT touch any other tab's rendering in this phase.

**Validation**: live-Editor screenshot confirming the new look renders correctly AND that all the same
data is still accurate - a visual change must never be able to silently change a number. Hold here for
Elias's confirmation before Phase C.

#### Phase C — Rollout to remaining 6 tabs

Only after Phase B is confirmed. Apply the same now-proven pattern to Decisions, Demographics,
Tax/Spending (Budget Process), Policy/Laws, and Politics - NOT all 6 simultaneously. Split into 2-3 at a
time, the same discipline the original Parliament gating rollout used (step 5's own revised design
explicitly avoided touching all seven remaining tabs in one pass, for exactly this reason). Screenshot
and confirm after each batch before continuing to the next.

- **5f. FOLDED INTO 5e (2026-07-31)** — the aesthetic restyling pass originally scoped as its own later phase is now part of 5e's combined scope (see above), not a separate step. Kept here, not deleted, per this document's own practice of marking supersession explicitly rather than silently rewriting history. Original 5f scope: aesthetic restyling pass (reference image 1: rounded cards, dark theme, big-number/small-label hierarchy, progress-bar visualizations, generous spacing) — LAST, applied to the final consolidated 7-tab structure, not to tabs about to be merged/removed, precisely because restyling a screen that's still being consolidated/rewired means restyling it twice. **Prep material referenced below is now folded into 5e's own broader asset request, not held separately.**

**5f prep, superseded by 5e's own consolidated asset request — "PoliSim GUI redesign.zip" asset pack** (`G:\UNITY\Projects\PoliSim\PoliSim GUI redesign.zip`, still not yet imported) — **origin confirmed**: Windows' Zone.Identifier mark-of-the-web on the file shows `ZoneId=3` (Internet zone), `HostUrl=https://claude.ai/`, i.e. a browser download from claude.ai (a Claude Design handoff), not an unknown/untrusted source. **Full security review completed** before this was treated as trusted prep work: both C# files (`PoliSimTheme.cs`, `PoliSimWidgets.cs`) read line-by-line and grepped clean for `System.Net`/`System.IO`/`System.Diagnostics`/`System.Reflection`/`Process.Start`/`UnityEditor`/`WebRequest`/`HttpClient`/`File.`/`Application.OpenURL`/`PlayerPrefs`/`Socket` — zero matches; all 8 SVG icon sources read in full and confirmed pure static geometry (`rect`/`circle`/`ellipse`/`path` only, no `<script>`, no event handlers, no external references); all 9 PNGs verified as genuine PNG image data via magic-byte detection, scanned for embedded scripts/URLs/executable signatures with none found. **Two distinct pieces, two different statuses**: the C# theming/widget code (`PoliSimTheme.cs` design tokens + rounded-rect primitives, `PoliSimWidgets.cs`'s six widgets) is PURE PROCEDURAL DRAWING LOGIC — unaffected by the rule 10 reversal, since it was already compliant either way. The actual icon/texture image files (8 SVGs + 9 PNGs) were the genuinely different case rule 10's reversal above now explicitly clears for import - the 8 existing `SystemArea` icons cover 8 of the 11 areas (all but Infrastructure, Global, and Neutral) and are folded into `CLAUDE_DESIGN_ASSET_REQUEST_5E.md`'s asset manifest as reusable, avoiding a duplicate request; the `menu_pattern_tile.png` background texture is likewise reusable as-is. **Still not yet imported into the project** - importing is a GameController.cs rendering change, explicitly deferred until Elias has reviewed the full 5e asset request and the remaining (new) assets are back.

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
- **SWF emergency drawdown fast-track** — LOAD-BEARING, not hypothetical: SWF rate/allocation changes have been part of the annual omnibus budget bill since 5c (DONE), so a genuine emergency drawdown can currently get stuck behind that country's next fiscal-year vote (up to a year away). **Recommendation (2026-07-31), pending Elias's confirmation**: emergency SWF drawdown becomes a standalone bill — the SAME tier 2/3 mechanism 5d already built (now real, not hypothetical - see 5d above) for new/removed programs and non-budget policy — not bundled into the annual budget, and NOT fully exempt like the Fed/Eurozone carve-out. Reasoning: real governments handle fiscal emergencies via expedited votes, not zero-oversight unilateral action; Norway's own GPFG withdrawal is itself an ordinary budget-process matter, not a central-bank-style independent decision, so a full exemption would overstate SWF's real-world independence. This needs zero new mechanism — it's exactly 5d's standalone-bill pattern, reused (most naturally as a fifth tier-3 bill type alongside Labor/CrimeJustice/Sector/Trade). Not yet confirmed — do not build against this until Elias signs off.
- **RESOLVED (2026-07-31) — "PoliSim GUI redesign.zip" icon/texture assets vs. working discipline item 10.** Elias explicitly reversed item 10: icons, portraits, and background/menu textures are now approved as imported sprite art (see item 10's own updated text above). `CLAUDE_DESIGN_ASSET_REQUEST_5E.md`'s full 58-file request (all six original sub-questions plus a later tab-navigation-icon addition) has been answered by Elias, delivered by Claude Design, security-reviewed, and imported into `Assets/Art/UI/` (2026-07-31). Fully closed - no image assets remain outside the project. `GameController.cs` rendering work is still deliberately not started - see the new "5e implementation plan" open items directly below for what's actually gating that now.
- **RESOLVED (2026-07-31) — Phase A tab-placement calls.** All five confirmed by Elias; see Part B's "5e implementation plan" mapping table above for the final placements and reasoning. Kept here for the record:
  1. `Trade` tab - confirmed split: informational content (Trade Balance graph) to Statistics, policy content (the `TradePolicyBill` and per-partner override rows) to Policy/Laws.
  2. `FederalReserve` tab - confirmed Politics.
  3. `PolicyWeb` tab - **Policy/Laws, NOT Statistics** (overrides the original recommendation) - Elias's own reasoning: "it's a relationship/reference tool consulted while deciding what to change, closer to where bills get drafted than to a pure stats readout."
  4. `Infrastructure` tab - confirmed folding into the Tax/Spending (Budget Process) destination alongside Welfare/SWF.
  5. Budget Process mandatory-pause interrupt surfaces under Decisions too - confirmed. Elias's own reasoning: "any 'time is blocked until you respond' state belongs in the same place, not treated as an exception."
- **Real reporting lag for data releases** (Continuous Time Migration) — optional realism refinement, not required for a first pass.
- **"Ongoing-process budgets"** (Continuous Time Migration Phase 0, item 5) — RESOLVED IN DESIGN (2026-07-31): this is now Master Sequence step 5's Annual Budget bill tier — see Part B above for the full design (real per-country fiscal-year dates, USA-only mandatory pause, the rest AI-resolved). Implementation is that plan itself (phases 5a-5c), not yet built.

---

## When Elias returns to this document

- Check the Master Sequence section — confirm which step is actually in progress or next, don't assume.
- Check Open Questions first.
- Review the commit log — each step should be its own commit(s), validation results in the message or CLAUDE.md.
