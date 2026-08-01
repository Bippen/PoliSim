# PoliSim — Completed Work

**What this is:** the permanent record of finished work, grouped by initiative. It answers "what has this
project actually accomplished."

**What this is not:** the technical record. `CLAUDE.md` remains the detailed authority on every item here
— implementation reasoning, bugs found, validation methodology, findings — and is **never superseded by
this file**. When the two disagree, `CLAUDE.md` is right.

**Standing pattern established 2026-08-01:** finished items move here; the roadmap documents hold only
live work. `POLISIM_MASTER_ROADMAP.md` should shrink over time, not grow.

---

## 1. Foundation — Roadmap Rounds 1–3

**15 items, all implemented, validated and committed.** The base simulation: macro model, fiscal system,
and the first three rounds of feature work.

| Area | Commits |
|---|---|
| Macro stability (GDP floor + reversion, Okun's Law mean-reversion, clamped inflation/unemployment) | `76ed3fc` |
| Government debt, deficit tracking, automatic stabilizers, debt risk premium | `f386f16` |
| Full-screen OnGUI dashboard, policy sliders, turn log | `4a8e2b4`, `9f97553` |
| Political layer, tax portfolio, detailed spending, fiscal calibration | `97a7d0b` |
| Federal Reserve, welfare policy, fiscal reaction function, batch validation tooling | `82c5c0c` |
| Expanded event pool (8 → 24 real-world-grounded events) | `1546d43` |
| Labor Market Basics, Crime & Justice Basics | `e1f5d35`, `e9c32b2` |
| Economic Sectors, Sovereign Wealth Fund + drawdown | `3250ebd`, `33a2686`, `95f79e9` |
| Expanded Sector Policies, Deeper Crime & Justice II, Expanded Sectors II | `b7c6d1f`, `3f0674e`, `7d20bda` |
| Infrastructure + feedback into PotentialGrowthRate | `c796547`, `d01632e` |
| Sector Output/Employment integration under an all-sources ceiling | `8235975` |
| Demographics Parts A & B | *see `CLAUDE.md`* |

**Validated by:** per-item real-Unity scenario matrices. Both Open Questions from this era (Sector
Integration, Infrastructure Feedback) are resolved.

**Lasting decision — the combined ceiling.** Multiple systems feed `PotentialGrowthRate` and
`LaborForceParticipationRate`. Every new contributor must fold into that variable's **existing combined
ceiling**, audited first. Both variables are already heavily stacked; this is standing rule 11 and it
still binds all future work.

**Lasting limitation:** Demographics needed two structural bug fixes and three correction rounds. It is
the precedent for why large changes get split into independently-validated batches.

---

## 2. Political Systems Overhaul — Part A (Cabinet)

*Master Sequence step 1. DONE 2026-07-30.*

Cabinet ministers with competence and philosophy, driving real decision outcomes.

**Validated by:** 28-combination real-Unity matrix, zero new anomaly types, plus a targeted diagnostic
confirming directional correctness.

**Lasting limitation worth remembering:** only **3 of the 6 confirmed portfolios** were implemented
(Finance/Treasury, Interior/Justice, Health & Social Affairs), deliberately, per Part A's own
content-authoring warning. The remaining three are unbuilt — not forgotten, but not done either.

---

## 3. Political Systems Overhaul — Part C (UI / graph restyling)

*Master Sequence step 2. DONE 2026-07-30.*

Graph threshold/target lines (NAIRU, comfortable debt level), "last N changes" pagination with
`StatHistory.MaxEntries` raised 50 → 250, a political compass, and five demographic pie charts.

**Validated by:** single-scenario smoke check, zero new anomaly types, plus a UI smoke test that **found
two real bugs** — including a political-compass clustering defect fixed by auto-scaling to observed
variance.

**Lasting decision:** graphs carry direction-aware green/red and threshold lines. Both conventions were
inherited by the Step B graph overhaul rather than reinvented.

---

## 4. Continuous Time Migration — Phase 0

*Master Sequence step 3. DONE 2026-07-30.*

Real in-game calendar with Pause/1x/2x/3x speed controls firing the existing, unchanged 121-day turn
cadence; a selectable-horizon live Policy Preview; multi-resolution `StatHistory`; and a Foreign Policy
Meetings interrupt slice.

**Validated by:** tick-equivalence proof, 100-turn smoke check, UI screenshot smoke test.

**Lasting decision — the phase changed no economic math.** Phase 0 is purely the calendar/UI layer.
`DaysPerTurn = 121` and `EpochDate = 2026-01-01` date from here and are depended on widely. The actual
daily-granularity conversion (Phases 1–5) remains unstarted.

---

## 5. Political Systems Overhaul — Part B (Parliament)

*Master Sequence steps 4 and 5.*

### Step 4 — pilot (Tax Policy tab only). DONE 2026-07-30.

Four fictional party archetypes with seats derived from `ApprovalRating` (bounded inertia plus jitter);
the full draft → introduce → 21-day wait → pass/fail flow gating one tab.

**Validated by:** the full 30-combination real-Unity matrix (15 scenarios × 100/500 turns, including a
`parliamentstress` worst case) — zero hard anomalies — plus a screenshot smoke test.

**Lasting decision — the vote model.** Pass/fail is `seatShare * fiscalStance * billSign` summed and
tested against **zero**. There is no seats-based majority. A documented consequence: Progressive Alliance
and Conservative Union sit tied at 32%, cancelling each other, which leaves Nationalist Front's smaller
purely-negative lean as the actual swing vote. "Welfare bills always fail, tax bills always pass" is this
working correctly, not a defect.

### Step 5 — full rollout, 5a–5d. DONE 2026-07-31.

A three-tier bill design replacing the original "repeat the pilot on seven tabs" plan:

- **5a** — real per-country fiscal-year dates (USA 1 Oct, EU five 1 Jan) + the mandatory pause hook.
  *Confirmed via live-Editor screenshots.*
- **5b** — the Budget Process full-screen UI shell. *Confirmed via live screenshots, after two real
  layout bugs found in Elias's own screenshots: a header clipping mid-word, and a preview panel
  rendering catastrophically narrow — where a first-attempt width cap turned out to be the binding
  constraint, not the calculation it was meant to backstop.*
- **5c** — the omnibus `BudgetBill` retiring `TaxBill`, plus the live vote estimate. *Confirmed via
  `BudgetBillDiagnostic` (all PASS) and real live play: sliders dragged under active 3x time
  advancement for nearly a year with zero freeze; two full fiscal-year cycles reopening the pause.*
- **5d** — standalone tier-2 program add/remove bills and four tier-3 non-budget policy bills.
  *Confirmed via `StandaloneBillsDiagnostic` (21/21 PASS across all seven bill types) and live play.*

**Two bugs live play caught that no automated matrix would have:** a global pending-decision banner that
silently masked a Budget Process pause behind a simultaneous Foreign Policy pause (fixed — all active
reasons now list together), and **the total absence of any save/load system**, discovered when a lost SWF
draft was misattributed to the bill mechanism. Diagnostics proved the mechanism correct across two years;
the real cause was Unity being closed between sessions.

**Lasting limitation:** `StandingDraftPair` / `DraftTrack` were designed and then **withdrawn** after
checking them against the real model — they did not match how drafts actually work.

### Step 5e — visual rollout. Phases A and B done; Phase C batches 1–3 live-confirmed.

Scope was revised to absorb 5f (tab/IA reorg + full sprite-based overhaul). Batches 1–3 confirmed
2026-08-01. **Batches 4–6 are built but await visual confirmation** — see `VISUAL_REVIEW_BACKLOG.md`;
they are not recorded as complete here.

**Lasting decisions from the visual work:**
- Tax and Spending merged into one **Budget** tab (`ConsolidatedTab` 7 → 6 values).
- IMGUI 9-slicing uses `style.border`, **not** the sprite's `spriteBorder` — IMGUI never reads the latter.
- Icons are stacked above labels, with space reserved via `style.padding.top` *before* the button draws.

---

## 6. Macro Data & Release Calendar Overhaul — Steps A, B1, D

*Master Sequence step 9. Partially complete — see the roadmap for what remains.*

### Step A1–A3 — release calendar, published-series model, revisions. DONE 2026-08-01.

`ReleaseCalendar.cs` (rules, not dates: BEA advance/second/third at t+30/60/90; Eurostat flash/regular),
`PublishedData.cs` (`PublishedStat`, `RevisionStatus`, `PublishedEntry`, `PublishedSeries`),
`PublicationSystem.cs` (daily publishing, preliminary noise at 1.5%, revisions converging on truth).

**Commits:** `737357b` (design), `c268d85` (live-value leak audit), `8e63a6f` (model + calendar),
`e3a0feb` (wiring), `92965ea` and `ea0a6a4` (two real bug fixes), `f66d678` (validation record).

**Validated by five checks, and the fifth is the one that counts:** a trajectory diff returns identical
both when the feature is correctly inert *and when it never ran at all*, so checks 1–4 were all
satisfiable by an implementation that did nothing. The **7087 published entries against ~7080 predicted
from the schedules** is what separates a proof from a formality.

**Lasting architectural decision — the one-directional rule.** `PublicationSystem` **writes** to
`Country.Published` and **reads** `Country.State`, never the reverse. Published data lives on `Country`,
never on `EconomyState` — checked across 55 call sites. A published value reaching a simulation input
would make the model consume its own stale output, and per the directive "may not surface for hundreds of
turns."

**Two real bugs fixed, both worth remembering:**
1. Publication emitted figures for reference periods **predating the game's epoch**, stamping pre-epoch
   quarters with present-day values and inverting the apparent trend. Fixed by suppressing pre-epoch
   releases, with `SeedInheritedHistory` seeding exactly **one** inherited quarter carrying the real
   sourced starting GDP — a second would require inventing a value.
2. Revisions converged on the **publication date's** live value rather than their own reference period's
   closing value, so Q1 and Q2 revisions could report identical figures. Fixed via `PeriodClosingValues`.

**Known limitation:** the validation proves publishing does not disturb the simulation. It does **not**
prove the published figures are correct — reference-period lag, revision dates and the
preliminary/revised distinction remain untested by automation.

### Step B1 — graph overhaul. Built 2026-08-01, awaiting visual confirmation.

All five directive requirements met: real calendar date axis, release-point markers, distinct
preliminary/revised treatment, selectable 1yr/5yr/all ranges, retained threshold lines and direction-aware
colouring. **Commits:** `59a2c1e`, `f1996e1`, `dd7e323`. Recorded here for the magnitude-aware formatter,
which **is** validated:

**`FormatAxisValue` — verified at multiple magnitudes**, directly addressing the StatTile bug that once
displayed GDP as "9,3": 30555,1→"30,6k", 42358,1→"42,4k", 138,6→"138,6", 999,9→"999,9", 1000→"1k",
−42358,1→"−42,4k", plus M and B ranges.

### Step D — sprite asset request. DELIVERED 2026-08-01.

42 assets requested (`6a53878`), delivered, security-reviewed and imported (`be97ebb`), with hand-written
`.meta` files (`65be9ab`) carrying the correct import settings: `nPOTScale`, `alphaIsTransparency`, no
block compression, no mipmaps, Clamp wrapping — Unity's defaults are wrong for UI sprites.

**Current state: imported but entirely unwired.** No code references them; `IconLibrary` has no Stats
path. **Known gap:** `icon_stat_interestrate` was never requested and is still missing.

---

## 7. Engineering, tooling and determinism

Not a roadmap item, but real work that everything above depends on.

### Reproducible validation

- **`SimulationRandom`** (`121656f`) — per-stream seeding from `masterSeed + streamOffset * 7919`,
  preserving reproducibility *and* isolation simultaneously. **The `Stream` enum is append-only** —
  integer values are baked into seeds.
- **`-seed=N` override** (`75fa05a`) for batch runs.
- **Batch-run hang fixed** (`8fbfde6`) — the exit callback was destroyed by a domain reload. Root-caused
  after five misattributions. `SessionState` survives domain reload; delegates do not.
- **Harness driven day by day** (`e15cb49`), as the real game does, with a cadence anomaly guard.

**Lasting decision — RNG isolation is load-bearing, not tidiness.** Publication noise draws from its own
`PublicationRevision` stream specifically so publishing cannot perturb the draw sequence of events, SWF
returns, Fed chair candidates, cabinet decisions or parliament jitter.

### Verification-integrity failures — seven instances, one named class

Documented in full in `CLAUDE.md`. The pattern: **the checking mechanism was compromised, not the thing
checked.** Recorded here because it is the most transferable output of this project so far.

Instances include: a diff comparing only 75 anomaly lines instead of 600 full-state lines; an anomaly
detector with a near-zero defect; an audit script whose bad escaping fabricated a finding contradicted by
a successful build; piping Unity output to `/dev/null`, hiding a fatal load error through two runs
reported as exit 0; and a `cleanup && capture` chain that reported success while silently skipping the
capture.

**Instance 7 is the one that generalizes furthest** — a *trusted source that was simply wrong*. Three
indicators, each checked against its own documented warning, each hiding a further variant axis:

| Indicator | Warning implied | Actually exists |
|---|---|---|
| Housing cost overburden | 3 | **8+** |
| Youth unemployment | 2 | **4** |
| Homeownership | none written | **4+** |

**STANDING RULE:** for any cross-country statistic, assume an undocumented variant axis exists until
proven otherwise, and record the basis alongside every value as a matter of course. **Checking a figure
against the documented warning is not verification** — it confirms exactly the subset of axes guaranteed
not to contain the error.

### Harness coverage, measured rather than assumed

`CheckFinite` covers **29 of 29** `EconomyState` floats — complete. `CheckSwing` covers **5 of 29**, and
range checks **4 of 29**. The headline "N anomalies detected" figure quoted throughout this project's
history is a 5-field measure, not a whole-simulation health signal.

**RESOLVED 2026-08-01 — coverage stays at five, and the count is described accurately instead.**
Extending would mean ~24 threshold choices plus a third baseline discontinuity in one day, and several
fields legitimately exceed 20% turn-over-turn. The fix was documentary: `CLAUDE.md` now opens with a
READ FIRST note stating what the number covers. Revisit if something ever slips through unnoticed.

### Two baseline discontinuities (2026-08-01)

Anomaly counts before and after these are **not comparable**: the near-zero floor fix
(`MinMagnitudeForSwingCheck = 2f`) and the pre-epoch calendar fix.

---

## 8. Asset pipeline

| Request | Status |
|---|---|
| `CLAUDE_DESIGN_ASSET_REQUEST_5E.md` | Delivered — nav/area icons, 14 imported |
| `CLAUDE_DESIGN_ASSET_REQUEST_UI_CHROME.md` + addendum | Delivered, security-reviewed, imported |
| `CLAUDE_DESIGN_ASSET_REQUEST_MACRO.md` | Delivered (42 assets), imported, unwired |

**Lasting decision:** sprites live under `Assets/Resources/` because `IconLibrary` uses `Resources.Load`
rather than `AssetDatabase` (Editor-only, would break in a player build). Two design-pack widgets
(`SupportBar`, `StandingDraftPair`) were **rejected** after checking them against the real model.

---

## Appendix — what completion does *not* mean here

Everything above covers Master Sequence items 1–5 plus part of item 9. **Items 6 (Round 4) and 7
(Continuous Time Phases 1–5) are entirely unstarted and are weeks of work each**, Phase 5 being the core
macro engine and the highest-risk work in the project. Step C of the macro overhaul is four batches, all
blocked on data requiring database access. Save/load is scoped but unbuilt.

This project is meaningfully underway, not nearly finished.
