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
content-authoring warning. ~~The remaining three are unbuilt — not forgotten, but not done either.~~
✅ **Limitation CLOSED by Round 4 batch R4-4 (2026-08-17)** — Defense, Foreign Affairs and Education
authored on the proven pattern (with the ruled passive-competence asymmetry: Education live on the
youth-U target, Defense/Foreign Affairs decisions-only). All six confirmed portfolios now exist.
See CLAUDE.md "Round 4 batch R4-4".

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
`DaysPerTurn` and `EpochDate = 2026-01-01` date from here and are depended on widely. *(Corrected
2026-08-27: `DaysPerTurn` was 121 here and has been 365 since `d8f55ce`, 2026-08-10; the daily-granularity
conversion — Phases 1–5 — is CLOSED, 2026-08-16, §28.)*

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
2026-08-01. **Batches 4–6 confirmed 2026-08-02** — see section 16, which closed step 5 entirely.

**Lasting decisions from the visual work:**
- Tax and Spending merged into one **Budget** tab (`ConsolidatedTab` 7 → 6 values).
- IMGUI 9-slicing uses `style.border`, **not** the sprite's `spriteBorder` — IMGUI never reads the latter.
- Icons are stacked above labels, with space reserved via `style.padding.top` *before* the button draws.

---

## 6. Macro Data & Release Calendar Overhaul — Steps A1–A3, D

*Master Sequence step 9 (macro). COMPLETE — A/B/D here and in §9; Step C shipped as Round 4's
five batches (§19). The directive itself was consumed and deleted 2026-08-26 (§25).*

⚠ **Scope correction (2026-08-02).** This section previously read "Steps A, B1, D". Both halves of that
were wrong, found by verifying against the commits rather than the summary:

- **"Step A" is A1–A3 only. A4 is NOT done** — see §9 below. `POLISIM_MASTER_ROADMAP.md` had marked all of
  Step A *including Tier 0 derived stats* as "DONE (2026-08-01), commit `e3a0feb`". That commit contains
  exactly two files, `PublicationSystem.cs` and `SimulationManager.cs`; `DerivedStats.cs` was not added
  until `70798e9`, whose own message says "NOT trajectory-validated".
- **B1 was built-not-confirmed when this was written; confirmed 2026-08-02** as review items 3, 7 and 8 —
  see section 16. Its entry below is retained for the part that was validated at the time,
  `FormatAxisValue`, which the P2 fix has since scoped to non-currency axes only.

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

### Step D — sprite asset request. DELIVERED 2026-08-01. Wired 2026-08-02.

42 assets requested (`6a53878`), delivered, security-reviewed and imported (`be97ebb`), with hand-written
`.meta` files (`65be9ab`) carrying the correct import settings: `nPOTScale`, `alphaIsTransparency`, no
block compression, no mipmaps, Clamp wrapping — Unity's defaults are wrong for UI sprites.

**Now wired**: `IconLibrary` gained its Stats path in `5701a04` and B2's stat row (`4869476`) is the first
thing that draws them. 41 of 42 render.

**Known gap, root-caused 2026-08-02:** `icon_stat_interestrate` was never requested because the macro
pack derived its list from the **29 fields on `EconomyState`**, and `InterestRate` is not one — it lives on
`CurrencyZone`, since a rate belongs to a currency zone rather than one country's economy. It was
therefore invisible to a code-grounded derivation while being one of the 18 policy-screen stats, the
target of its own policy node, a Taylor Rule input and the Fed/Eurozone headline figure. **Lesson:
enumerate the display enum (`StatNodeId`), not the storage struct.** Now the sole item in
`CLAUDE_DESIGN_ASSET_REQUEST.md`.

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

### Verification-integrity failures — ten instances, one named class

*Count corrected 2026-08-02: this said "seven", written before instances 8, 9 and 10 existed.*

Documented in full in `CLAUDE.md`. The pattern: **the checking mechanism was compromised, not the thing
checked.** Recorded here because it is the most transferable output of this project so far.

Instances include: a diff comparing only 75 anomaly lines instead of 600 full-state lines; an anomaly
detector with a near-zero defect; an audit script whose bad escaping fabricated a finding contradicted by
a successful build; piping Unity output to `/dev/null`, hiding a fatal load error through two runs
reported as exit 0; and a `cleanup && capture` chain that reported success while silently skipping the
capture.

**Instance 8** — `-runmatrix` silently discarded `-seed`, so the single most important validation this
project performs (a seeded matrix diff) was the one combination that could never work. Both flags were
documented together, which is what made the combination look supported.

**Instance 9 — an enum whose zero value is a meaningful state.** `CreditRating.AAA = 0`, so
`default(CreditRating)` is AAA and an *unrated* country snapshotted as *top-rated*; every country's first
scheduled review then read as a downgrade, fabricating 30 anomalies for Italy alone. The same
confident-wrong-default appeared three times on one feature — `Mathf.RoundToInt(NaN)` is also 0 and also
clamps to AAA, and the dashboard tile would have rendered an unreviewed rating as AAA. **STANDING RULE:
when an enum's zero value is a real and especially a *good* state, a zero-initialised field cannot
distinguish "unset" from it** — carry an explicit has-a-value flag, or reserve slot 0 for `None`. Note it
was only visible because Italy's true rating is far from AAA; the identical bug on Sweden or Germany,
both genuinely AAA, would have produced no anomaly at all.

**Instance 10 — three broken verification scripts in one day**, each returning a clean, confidently
formatted, **universally negative** result: "nothing registered", "no sprites delivered", "no asset pack
imported". `[regex]::Escape` + `-SimpleMatch`; a `stat_*.png` pattern that misses the `icon_` prefix; and
`.Length` on a `PSCustomObject` colliding with the intrinsic member. All three were caught only because
the answer contradicted something already known — one of them was briefly reported before correction.

**A universal negative is the dangerous shape**: a partial one invites scrutiny, while "nothing matched"
reads as a decisive finding — precisely when it is most likely to be the check failing, since the
commonest defects break *every* comparison identically.

**STANDING RULE:** any verification script capable of returning a universal negative must **self-test
against a known-good case first and print the result**, so "the script is broken" and "the finding is
real" are distinguishable *at read time* rather than afterwards by noticing a contradiction. **Corollary:
a check whose known-good case cannot be named is not yet a check.**

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

---

## 9. Master Sequence step 9 continued — B2, C4 placement, A4 validation (2026-08-02)

### B2 — contextual policy-screen stats. BUILT AND WIRED. Awaiting review item 10.

Data layer `3dcf038`, rendering `5701a04`, `UiPalette.MutedIconTint` fix `58c4442`, wiring `4869476`.

**Recorded here for the wiring decision, which is settled**; the visual result is not confirmed, so B2 is
not complete. `GetConsolidatedTabArea` is a **hue picker** — its own doc says it chooses for visual
distinctness across the tab bar, not correctness, and answers `PolicyLaws` with `Sectors` only because
that colour was unclaimed. Driving a stat row from it would have shown sector stats on the Labor and
Crime & Justice screens. **At sub-screen granularity the mapping is exact**, because every policy screen
already declares its own area for its bill card. That is what `GetPolicyScreenArea` reads.

**Lasting decision — derived, never authored.** Which stats appear comes from the Policy Web's edge list,
so adding an edge makes a chip appear with no second list to maintain. A wrong chip therefore means a
wrong *edge*, which is why review item 10 is a correctness check wearing a visual disguise.

**Two real findings from that edge list:**
- **No `Infrastructure` policy node has a single Policy Web edge**, so that screen correctly draws no row.
- **`Fiscal` derives 7 stats** — all 13 `TaxType`s and all 14 spending lines touch `DebtToGdp` and
  `Approval`. Hence a 4-stat cap with the remainder stated ("+N more affected — see Policy Web"), never
  silently trimmed. Tax and Spending showing identical stats is correct, not a bug.

### C4 — credit rating placed on the dashboard. PROVISIONAL. Model defect open.

`76a8f35` built, `3d77b11` placed beside Debt-to-GDP. **Placement is Elias's call and explicitly
revisable after review item 11.** Computed every frame rather than cached — caching is how a rating comes
to disagree with the debt figure next to it. Only a Positive/Negative outlook draws a pill, because
`StatTile`'s pill is binary and "Stable" is neither.

**UPDATED 2026-08-02 — C4's implementation is COMPLETE** (`a4155ca`). Its first trajectory validation
failed with 3,421 rating-thrash anomalies; Elias's A1 ruling fixed it by review **cadence** rather than
damping, and the cadence fix worked as intended. Anchors hold **5 of 5, unchanged**; matrix anomalies fell
to **1,416**, and the residual is **not a C4 defect** — see §13.

**Only C4's CLOSURE remains outstanding**, and it waits on an upstream simulation defect rather than on
any further rating work. `MISSING_PREREQUISITES.md` §F1.

### A4 — Tier 0 derived stats. TRAJECTORY-VALIDATED, but NOT surfaced. Not complete.

`70798e9` built, validated `3d77b11`.

**A4 passes its validation**: zero finiteness failures across the full matrix — 15 scenarios × 100 and
500 turns × 6 countries, `-seed=777`, real Unity 6000.5.6f1. The "NOT trajectory-validated" caveat
`70798e9` shipped with is discharged.

**A4 is still not done, and this is the honest reason.** The directive defines it as *"pure display
arithmetic"* — and it displays nothing. Verified 2026-08-02 by enumerating callers: of its six methods,
four (`GdpPerCapita`, `TaxBurdenPercentOfGdp`, `SpendingPercentOfGdp`, `SectorSharesOfGdp`) have **only a
test-harness caller**, and the other two are consumed internally by `CreditRatingSystem`. **A display-only
feature that displays nothing is not complete**, however well it validates.

**Deviation from `STEP_A_DESIGN.md` worth recording:** that design recommended computing Tier 0 stats
"from published inputs throughout". As built they read **live**. Consistent with Elias's later A3 ruling
(live on policy screens), but it was never explicitly reconciled — worth confirming when A4 is surfaced.

**A4 DONE 2026-08-02 → the look closed 2026-08-26 (migrated from the roadmap 2026-08-27).** The derived
stats went on screen (Statistics → Domestic, under the headline tiles: GDP per capita, tax burden,
government spending, deficit/surplus and sector shares); the Derived block converted to read-only ledger
rows (`397d829`, 2026-08-11 — `DrawDerivedStatRow`, called at HEAD); and the Statistics screen was in
front of Elias in playtest 2 (2026-08-25), on which the "needs a visual look" proviso was struck
(2026-08-26, ruling C3). Tier 0 stats are display-time derivations, never state — the seed doc's Part 3
said so and is retired to this line.

### Harness coverage extended to the derived layer

**The generalizable lesson: wiring into the UI and wiring into the harness are different things.** A
dashboard tile is `OnGUI` code and `BatchSimulationRunner` never calls `OnGUI`, so placing C4 on the
dashboard would have left it exactly as unreachable from a batch run. `SimulationTestRunner` now evaluates
both A4 and C4 per turn, per country, which is what actually put them under the matrix.

**Why pure display arithmetic needs coverage at all:** `CreditRatingSystem.Evaluate` ends in
`Mathf.RoundToInt(notches)` then a clamp, and `RoundToInt(NaN)` is **0**, which clamps to **AAA**. A
non-finite input would not crash and would not look wrong — it would render the best possible rating on a
broken country.

**Third baseline discontinuity for anomaly counts.** `[DERIVED]`-prefixed anomalies did not exist before
this, so counts either side are not comparable. Governed by `CLAUDE.md`'s READ FIRST note like the other
two.

### Unity `.meta` files for the four new scripts

`e185a72`. The four `.cs` files added 2026-08-01 were committed without their `.meta`, while all 62 other
script metas are tracked. **The GUID lives in the meta, not the source**, so a fresh clone would have had
Unity mint new ones and any future serialized reference would resolve differently between machines.
**Staging a new `.cs` in a Unity project means staging its `.meta` in the same commit.**

---

## 10. Master Sequence step 5e — Phases A and B, Phase C batches 1–3

*Consolidated out of the roadmap 2026-08-02; batches 4–6 confirmed the same day — see section 16.*

**Phase A — tab/IA restructuring. DONE 2026-07-31.** 18 tabs to 7 consolidated tabs with sub-categories.
Elias confirmed all five placement calls: Trade split (informational to Statistics, policy to
Policy/Laws); Federal Reserve to Politics; **Policy Web to Policy/Laws, overriding the original
recommendation** on Elias's reasoning that "it's a relationship/reference tool consulted while deciding
what to change"; Infrastructure folded into Budget Process; Budget Process interrupts surface under
Decisions too, because "any 'time is blocked until you respond' state belongs in the same place".

**Phase B — sprite reskin pilot (Statistics/Dashboard). DONE 2026-08-01**, confirmed live across two
rounds.

**Batch 1 — tab bar** (`a8decf9`), **Batch 2 — card chrome + Decisions** (`5df7811`), **Batch 3 —
Politics** (`6922f9f`). All three confirmed by Elias in the live Editor.

**Demographics needs no restyle — a finding, not an omission.** Its content is entirely pie charts, which
working discipline item 10 keeps procedural. Decorative cards would add clutter without meaning.

### Two lasting lessons from batch 3, both worth more than the batch

**`PoliSimWidgets.SupportBar` was the wrong widget, and the reason generalizes.** It renders "N of 200
seats, majority 101". **This simulation has no seats-based majority**: `ParliamentSystem` sums
`seatShare * fiscalStance * billSign` and tests against zero, so a bill can pass with fewer aligned seats
than opposed. Using it would have drawn a rule the model does not implement. **The design pack's widgets
were authored against an assumed generic political sim, not this codebase — check each against the real
model before reaching for it.** `UiPalette.DrawDivergingBar` is the honest substitute, fed by
`ParliamentSystem.GetSeatWeightedAlignment`, which `WouldBillPass` also calls so the two cannot disagree.

**`Mathf.Sign(0f)` returns 1 in Unity, not 0.** A zero-direction bill passes unconditionally via a
short-circuit in `WouldBillPass`, but scoring it anyway yields parliament's raw net stance — negative in
the tied-parties case — which would have painted a red bar beside "leans PASS". **Any derived display must
short-circuit on the same condition its verdict does.** Caught while writing repro steps, not in review.

---

## 11. Resolved design decisions

*Consolidated out of the roadmap's Open Questions 2026-08-02. All resolved; none live.*

| # | Question | Resolution |
|---|---|---|
| A1 | `SimulationRandom` stream position across save/load | **Counting shim** — record draws per stream, fast-forward on load. Reversible beats permanent; xorshift revisitable once real load times are known. Preserves every recorded baseline. Implement with Master Sequence item 8 |
| A2 | Harness swing-check coverage (5 of 29) | **Stays at five**; fix is documentary. Extending meant ~24 threshold choices plus a third discontinuity in one day, and several fields legitimately exceed 20% turn-over-turn. `CLAUDE.md` now opens with a READ FIRST note stating what the number covers |
| A3 | B2 shows LIVE or PUBLISHED values | **LIVE.** A lagged, possibly preliminary figure in a "what am I doing right now" panel misrepresents itself, and the instruction was only satisfiable for 6 of 18 stats. Published view stays on Statistics |
| A4 | `PublishedData.PeriodClosingValues` retention | **Keep everything, no pruning.** Data is small; a revision converging on a missing closing value is a bug already fixed once (`ea0a6a4`). Flatten to `{stat, periodStart, value}` on save, rebuild on load |
| A5 | Build C4 out of order? | **Yes**, with the justification recorded so it is not precedent: skipping is warranted only when a later item is **genuinely independent AND** the earlier blocker is outside the project's control. Neither condition alone suffices |
| C1a | Primary housing metric | **Homeownership**, reversing the directive's overburden recommendation. Elias's call, on data honesty — see below |
| C1b | USA housing figure | Homeownership gives the USA a comparable 65.3, dissolving the overburden methodology mismatch |
| C1c | Homeownership measurement basis | **OECD Affordable Housing Database, share of HOUSEHOLDS owning. This basis only** |
| — | Economic Sectors feedback | **INTEGRATE** — bounded nudges under an all-sources ceiling. Real-Unity confirmed pinned at the ceiling under stress |
| — | Infrastructure `ConditionIndex` feedback | **FEED BACK** — threshold-based drag, reconciled with the pre-existing nudge under one combined ceiling (0.75) |

### The housing metric decision, and the six-stage correction behind it

Overburden remains the **better concept** — it measures affordability *stress* rather than tenure and
responds to interest rates and housing assistance, both live levers. **It lost on data honesty**: 2 of 6
verified means seeding four countries from a range, and a bound is not a value.

**The margin is 3–2, not 4–2.** Poland's ~87.9 turned out to be a Eurostat *nationals* line, not the OECD
household basis, so it left the verified set. The decision holds — 3 same-basis figures still beat 2, and
overburden's missing three are unobtainable by search while homeownership's are ordinary lookups — but by
one country rather than two.

**Germany alone appears three ways**, an 11.3-point spread across three definitions each correct for its
own source: OECD households 41.0, dwelling-based ~46.7, Eurostat nationals 52.3. Eurostat's
population-based measure (68.4% EU) is a fourth. **Germany 41.0 against an OECD average of 70.1** is the
real same-basis contrast, and *more* extreme than the earlier ~47 figure suggested.

**Six successive claims about housing coverage, each correcting the last** — kept because it is the
clearest single illustration of verification-integrity instance 7:

| Stage | Claim | Verdict |
|---|---|---|
| Directive | Overburden 6 of 6 | Overstated |
| Seed file, original | Overburden 4 of 6 | Wrong variant *(the seed doc later retracted the "two adults" attribution from the API's own structure — no household-type dimension exists; the figures were simply another variant. `POLISIM_SEED_DATA_MACRO_OVERHAUL.md` §"CORRECTION TO THE CORRECTION" is the authority; corrected here 2026-08-27)* |
| First gap report | Overburden 4 of 6 | Caught the directive; trusted the numbers |
| Corrected + gap-closing | Overburden **2 of 6** | Correct |
| Metric decision | Homeownership 4 of 6 | Overstated — mixed bases |
| Basis re-check | Homeownership **3 of 6** | Correct |

**Both metrics were overstated at first, and in both cases the error was invisible to a check against the
documented warning.**

---

## 12. Step A design and audit artifacts

*`STEP_A_DESIGN.md` and `STEP_A_LIVE_VALUE_AUDIT.md` were consolidated here and deleted 2026-08-02. Both
were pre-implementation artifacts for work now complete; git history preserves them in full.*

**The audit's central finding, which drove the architecture:** all **55 live reads across 11 simulation
files** go through `country.State.X`. Keeping published values off `EconomyState` entirely makes a leak a
**compile-time impossibility** rather than a review obligation across 55 sites. Per-file counts:
`MacroSystem` 18, `SimulationManager` 15, `ParliamentSystem` 9, `TradeSystem` 3, `TaylorRule` 2,
`EurozoneRateSystem` 2, `CurrencySystem` 2, and one each in `ForeignPolicySystem`, `EventSystem`,
`ElectionSystem`, `CabinetSystem`.

**The free check that came with it:** any diff to `EconomyState.cs` beyond comments is itself evidence the
design drifted — catchable before the expensive trajectory comparison.

**The three systems the directive named, and why they were highest-risk:** Okun's Law and the Phillips
Curve both read `state.Unemployment`; the Fiscal Reaction Function reads `country.State.DebtToGdpRatio`.
**`Unemployment` and `DebtToGdpRatio` are also among the most-published stats in the real release
calendar**, making these three simultaneously the highest-risk and the easiest to get wrong.

**Baseline captured at `6a53878`** (pre-change HEAD), stored at `baselines/stepA_baseline_6a53878.log` —
captured *before* any Step A code, because once the code changes the untainted reference cannot be
reconstructed.

Both of the design doc's open items were resolved by implementation: which stats get published series
(`PublishedStat` has 6 members, only those with real specified release rules), and what the UI shows
before a stat's first publication (`SeedInheritedHistory` seeds exactly one inherited quarter carrying the
real sourced starting GDP — a second would require inventing a value).

---

## 13. Step C4 — scheduled rating review. IMPLEMENTATION COMPLETE (2026-08-02)

`a4155ca`. Elias's A1 ruling: fix the rating thrash by review **cadence**, not by damping. **The cadence
fix worked as intended and Step C4's implementation is finished** — nothing about the rating remains to be
built or tuned. Only its *closure* is outstanding, and that waits on an upstream defect (§F1 of
`MISSING_PREREQUISITES.md`), not on more rating work.

**Changed WHEN the rating is computed, not HOW.** The formula body moved verbatim out of `Evaluate` into
`EvaluateFrom(debtToGdp, riskPremiumSensitivity, deficit, growth)`. `BurdenCurve`, the reserve-currency
discount, the deficit divisor and the growth thresholds are untouched, and the live path and the scheduled
review share the one method so they cannot drift into two formulas. **That is what preserved the
calibration**, and the anchor check proves it did.

**Cadence: annual, on each country's own fiscal-year start** (USA 1 Oct, EU five 1 Jan). Justified rather
than defaulted — agencies review once or twice a year; the date already exists as
`FiscalYearData.GetFiscalYearStart` and is already the boundary `ReleaseCalendar` treats annual figures as
settling on, so no new date rule and no parallel timer; and it is the same boundary the budget process
turns on, so a review lands when the year it judges has closed. Outlook refreshes quarterly while the
rating moves annually — the real division of labour between the two signals, and what makes the outlook
worth having when the rating is deliberately still between reviews.

**The settled deficit is derived from the debt stock**, which is the substantive fix. The thrash came from
`FiscalTurnReport.BudgetBalance` — one 121-day turn's balance. A year's deficit is by definition the
year's increase in indebtedness, so with both stocks recorded it is exact rather than smoothed:
`deficit% = d_now − d_prev × (Y_prev / Y_now)`, both readings from `PeriodClosingValues` on the **same**
quarterly boundaries.

**Lasting decision — `ClosingStat` vs `PublishedStat`.** Reusing `PeriodClosingValues` was instructed, but
it is keyed by `PublishedStat`, which has no debt member. The key was widened to a new `ClosingStat`
superset rather than adding a member to `PublishedStat`, because that enum makes a deliberate claim — only
stats with a real sourced release rule appear in it, since inventing a cadence is fabrication — and a
never-published member would contradict it and make `Latest()` a permanent null trap. One store, one
recording pass, an explicit exhaustive map that throws on drift.

### Validation

**5-anchor calibration: 5 of 5 PASS, unchanged.** Run before the matrix, as instructed.
`CreditRatingAnchorCheck` runs headlessly with **no Play mode** — the formula is pure arithmetic — so it
takes seconds.

**This is the first EXECUTABLE version of that check.** The original calibration (`76a8f35`) was done by
hand and recorded only in a commit message. "Unchanged" therefore means "reproduces those five recorded
results", not "matches a previous script". An anchor check that exists only in prose cannot be re-run
after a change, which is exactly what this work needed it for. The check also reports the band its one
tunable input survives in — **the USA holds AA+ for deficits in [4.6%, 7.5%] of GDP**, and the anchor uses
6.4%, near the middle rather than balanced on an edge.

**Full matrix: 3,421 → 1,416 anomalies**, `-seed=777`, 15 scenarios × 100 and 500 turns, 0 compile errors,
0 finiteness failures. USA, Italy and Poland stayed at **zero**, as required.

### The residual is not a C4 defect — and C4 is what made it visible

The remaining 1,416 (Sweden 616, France 567, Germany 103) trace entirely to the **debt-to-zero
bimodality**. The settled annual deficit the review reads ranges **−135.5% to +170.8% of GDP**, derived
correctly from a debt stock that collapses to exactly 0.00% and spikes back to ~44% inside a year.
Sweden in plain `baseline`: 21.8% (turn 1) → 0.90% (turn 25) → **0.00%** from turn 50 on.

**A sovereign whose debt genuinely moved like that would be downgraded repeatedly.** The rating reports its
input faithfully; the input is what is wrong.

**C4 is the first instrument that makes this pre-existing defect player-facing rather than log-only.**
Until now the bimodality lived in anomaly counts, batch summaries and CLAUDE.md prose — a player could run
100 turns as Sweden and never be told their national debt had gone to zero and stayed there. The rating
tile sits in the dashboard grid on every tab, so the same defect now shows up on screen. **It did not get
worse; it got a display.** That is why its priority was raised in the roadmap: it now blocks a step and is
visible to a player, neither of which was true before.

**Do not fix it by damping the rating.** That option was raised and explicitly rejected in A1; doing it now
would return the defect to log-only while making C4 dishonest. A derived stat that stayed calm while its
inputs did this would be the broken one.

---

## 14. Visual review — the four items Elias closed (2026-08-02)

**Elias reviewed all eleven items live, playing as USA.** Four passed clean and are closed; the rest stay
in `VISUAL_REVIEW_BACKLOG.md`. **Master Sequence step 5 does NOT close** — it needs items 1–9, and 3, 7, 8
and 9 all failed.

| Item | Verdict |
|---|---|
| **1. Statistics nav icon sizing** | ✅ *"it reads like an icon"* — closes the "colored speck" thread from the Phase B pilot |
| **2. Statistics restructure** | ✅ *"natural"* — Domestic/International split confirmed, Trade sitting inside International reads correctly, and the graphs-out-of-the-left-column change holds |
| **4. Amber draft cue** | ✅ *"says it is a draft"* — the 25-call-site `DrawDraftLabel` treatment communicates "changed, not yet law" without reading as a warning |
| **11. Credit Rating tile** | ✅ **placement confirmed** — beside Debt-to-GDP in the dashboard grid. The provisional marker on C4's placement is discharged |

**Item 2's dependency resolved favourably:** it gated item 3, and rejecting item 2 would have left item 3's
redesign without a home. Item 2 passing means item 3's failure is confined to item 3.

**Item 11's confirmation settles C4's placement**, which had been explicitly recorded as provisional and
revisable. It is no longer provisional. *(C4's own closure remains blocked upstream — see
`MISSING_PREREQUISITES.md` §F1.)*

### Not closed, and why the distinction matters

**Item 10 passed but is NOT closed.** It is Tier 0, so it was reviewed at turn 0 — and `DrawSparkline`
returns early below two history points, so **its sparklines never rendered during the review**. The same
component then crashed the Budget tab at day 273. Its chips, icons and layout are confirmed; the part that
failed was never seen. It needs re-review with item 9.

**Items 5 and 6 passed with defects** and stay open: text clipping, which is the label-measurement class
recurring for the sixth and seventh time.

---

## 15. Asset pipeline — both outstanding deliveries imported, and the gap that hid them closed (2026-08-02)

Two assets were sitting in zips at the project root while three documents recorded them as outstanding.
Both are now in production, and the pattern that let them sit is now a check rather than a note.

### `icon_stat_interestrate` — delivered the same day it was recorded as "awaiting delivery" (`6ff2e1f`)

The Interest Rate chip on B2's contextual stat row drew no icon. `MISSING_PREREQUISITES.md` section E
read *"REQUEST SENT, awaiting delivery"*; Elias pointed out it had already arrived in
`Policy rate icon design.zip`.

256×256 RGBA PNG to `Assets/Resources/Art/UI/Stats/`, 24×24 `currentColor` SVG source to `Stats/Source/`,
both with hand-written `.meta` files. The PNG's is **byte-identical to `icon_stat_gdp.png.meta` apart from
its guid**, which is what the request document's own import spec prescribes. Brief met: a `%` — slash plus
two dots — over a rising stepped line, distinct from `icon_stat_inflation`'s price tag, which mattered
because the two sit adjacent on the Fiscal row and one is a lever the player pulls while the other is an
outcome they watch.

**Why it was missed originally, and the lesson that generalises:** the macro icon pack derived its stat
list from the 29 fields on `EconomyState` — code-grounded, and the right instinct. `InterestRate` is not
one of them; it lives on `CurrencyZone`, because a rate belongs to a currency zone rather than to one
country's economy (the Eurozone five share one). It was structurally invisible to that derivation while
being a `StatNodeId`, a `PolicyNodeId` target, a Taylor Rule input and the headline figure on two screens.
**Enumerate the display enum, not the storage struct.**

### `menu_pattern_tile.png` — delivered, then unimported for weeks (`this commit`)

A seamless 256px dot-lattice-and-hatch tile, white on transparent at very low alpha (sampled values 0, 6
and 21 of 255) so it reads as texture rather than as a pattern in its own right. Imported to
`Assets/Resources/Art/UI/Textures/` — a new folder, with a hand-written folder `.meta` — and wired into
`DrawCountrySelector`, which previously drew no background at all.

**Its `.meta` deliberately departs from the icon convention in exactly one respect: Wrap Mode `Repeat`
rather than `Clamp`**, verified as the only difference besides the guid. That is what the delivery's own
README specifies, and it is load-bearing: the tile is drawn with `GUI.DrawTextureWithTexCoords` at one
tile per 256px of screen, so `Clamp` would not error — it would stretch the edge pixel across the display
and read as a design choice rather than as a broken import. `StatIconCoverageCheck` now asserts the wrap
mode for exactly that reason.

The wash beneath it is drawn whether or not the texture loads, so a failed import degrades to a flat dark
panel instead of taking the background down with it.

### The pattern both shared, now a standing rule and two checks

**Working-discipline rule 12: "awaiting delivery" must be re-derived from the filesystem, never trusted
from a document.** Neither register was wrong when written. Nothing watches the project root, a delivery
does not announce itself, and the status outlived the fact — twice.

- **`DeliveredAssetCheck`** (`Assets/Editor/`) compares every zip's contents against what exists under
  `Assets/` and fails on any gap. **Proven against the real defect before being trusted**: with
  `menu_pattern_tile.png` temporarily withdrawn it reported `MISSING ... 18 of 19 asset entries present`
  and exited 1; restored, it reports 0 gaps across all 7 packs and 191 asset entries. It also independently
  reproduces every figure in the archive README (84/84, 42/42, 24/24, 20/20, 2/2), which had until now
  been a hand-verified claim. It carries a documented alias map for the 16 entries imported under
  reconciled names (`icon_crime` → `icon_area_crimejustice`, and so on), verified against the files on
  disk rather than taken from the README's "and so on" — without it the check would report 16 permanent
  false misses and become noise that gets ignored.
- **`StatIconCoverageCheck`** asks the runtime half for **the 19 names it enumerates** — every
  `StatNodeId` icon, plus `menu_pattern_tile` — resolving through `Resources.Load`. **19 of 19.** A file
  existing on disk does not guarantee this when its `.meta` is hand-written: a malformed importer block
  leaves the asset present and unloadable, and the null-on-missing contract would swallow that silently.

  ⚠ **This read "every name the UI hard-codes" until 2026-08-11, and the overclaim was load-bearing.**
  The UI hard-codes far more than 19 — chrome, emblems, portraits, party marks — none of which this
  check touches. §1F of the design request and `PARTY_EMBLEM_QUESTION.md` both cited it as proof that
  four newly imported party marks resolved; it passed 19 of 19, and would have passed with those marks
  absent or corrupt. `CLAUDE.md` had the scope right the whole time ("only covers the 19 names the UI
  hard-codes"); this document's looser phrasing is what licensed the misuse. **A check is evidence only
  for claims its enumeration contains.** `PartyMarkCoverageCheck` answers the emblem question.

**The project root now holds no zips at all**, for the first time. That state is itself the signal: a zip
at the root means something in it is unfinished.

⚠ **One consequence, raised not resolved:** review item 10 was seen with the Interest Rate chip's label
flush left where the missing icon would have been, so that row's spacing is not what Elias approved. That
is item 10's *second* caveat, alongside its sparklines never having rendered at turn 0.

---

## 16. Visual review CLOSED — all eleven items confirmed, and Master Sequence step 5 with them (2026-08-02)

**Elias re-reviewed items 3, 7, 8, 9 and 10 and passed all five.** With 1, 2, 4 and 11 closed earlier the
same day and 5 and 6 having passed with a defect, **every one of the eleven items is confirmed.**
`VISUAL_REVIEW_BACKLOG.md` was deleted per the standing pattern — an emptied document drifts back into
use — and this section is its record.

### Final results — all eleven

| Item | Result |
|---|---|
| 1. Statistics nav icon | ✅ PASS — *"it reads like an icon"* |
| 2. Statistics restructure | ✅ PASS — *"natural"* |
| 3. Published graph, empty state | ✅ PASS on re-review, after the P2 unit fix (`628d78e`) |
| 4. Amber draft cue | ✅ PASS — *"says it is a draft"* |
| 5. Policy/Laws restyle | ✅ PASS with a defect — *"trade is cut off"* |
| 6. Budget full-screen | ✅ PASS with a defect — text above the icons clipped |
| 7. First release + reporting lag | ✅ PASS on re-review, once the axis stopped misreporting magnitude |
| 8. Revision treatment | ✅ PASS on re-review |
| 9. Budget Process restyle | ✅ PASS on re-review, after the sparkline crash fix (`e9e3f6a`) |
| 10. B2 contextual stat row | ✅ PASS on re-review, with both caveats cleared |
| 11. Credit Rating tile | ✅ PASS — placement confirmed |

**The two defects in 5 and 6 remain live work and did NOT block closure**, correctly: the items are
confirmed as designs, and what is left is the label-clipping class (P4), which is a defect with its own
entry in the roadmap rather than an unconfirmed screen. Keeping the review open for it would have
conflated "has not been seen" with "has a known bug".

*(Added 2026-08-27: items 4, 5 and 9 ARE 5e's batches 6, 4 and 5 — the mapping recovered from the
deleted backlog's history — so those batches were live-confirmed here on 2026-08-02 although the
roadmap carried "Not yet live-confirmed" on them until the third consolidation (§29). Item 3's P2 fix
(`628d78e`) was likewise SEEN here; its roadmap entry never closed with it.)*

### What the four failures cost, and what each taught

Three of the four failures were real defects rather than taste, and each produced a permanent check:

- **Item 3 — the currency unit bug.** `FormatAxisValue(29000)` rendered "29k" for $29 trillion; the third
  instance on the same value. Fixed by `UiFormat.Money` with the unit a **required** parameter, covered by
  `MoneyFormatDiagnostic` (6 of 6). Full record in `CLAUDE.md`.
- **Items 7 and 8 — "hard to make out any of the graphs".** Not a marker-design problem at all, which is
  why the sequencing mattered: both were held behind item 3 rather than iterated on, and both passed once
  the axis stopped lying about magnitude. **Two review items were fixed by changing neither of them.**
- **Item 9 — a black screen**, and the most instructive. `SetPixelSafe` bounds-checked against the
  full-size graph's 300×90 while `DrawSparkline` passed a 72×20 buffer; at y≥5 the index ran past the
  1,440-element array and threw inside `OnGUI`, aborting the frame. 2,309 occurrences in Elias's own
  `Editor.log`.

  ```
  IndexOutOfRangeException: Index was outside the bounds of the array.
    at GraphRenderer.SetPixelSafe          (GraphRenderer.cs:815)
    at GraphRenderer.DrawSparkline         (GraphRenderer.cs:769)
    at PolicyScreenStatsRenderer.DrawChip  (PolicyScreenStatsRenderer.cs:143)
    at GameController.OnGUI                (GameController.cs:833)
  ```

  It came from B2's own "reuse, don't duplicate" decision backfiring: `DrawLine` was deliberately reused
  *"so a sparkline can't disagree with its full-size counterpart"* — the right instinct with a shared
  helper that was not dimension-agnostic. **Sharing the algorithm was correct; sharing the constants was
  not.** The maths is now `BuildSparklinePixels`, callable headlessly, and `GraphRendererDiagnostic`
  covers 336 size/shape combinations plus the exact failing case.

### Item 10 carried two caveats into the re-review, and both are now cleared

Worth recording because both were found by reasoning about *what the reviewer could actually have seen*,
not by re-testing:

1. Its sparklines never rendered during the first pass — item 10 is Tier 0, so it was reviewed at turn 0,
   and `DrawSparkline` returns early below two history points. The component that had been confirmed was
   not the component that later crashed.
2. It was reviewed with the Interest Rate chip drawing no icon, because `icon_stat_interestrate` had not
   yet been imported and the null-on-missing contract shifted the label into the icon's space.

**A pass is only valid for what was on screen.** Both caveats invalidated a confirmation that looked
complete, and neither would have surfaced from the review notes alone.

## 17. Screen-edge clipping — the frame itself, fixed and measured (2026-08-11)

Elias, from live play: *"There are borders at the sides which cutoff the game. For example the time
selector at bottom and the politics tab top right."* The desk margin is intentional; `OnGUI`'s
`BeginArea` clipping content laid out past it was not.

**It was already sitting in that morning's capture set.** `screenshots/run_*` had been written at the
resolution the defect is visible at and not looked at closely, which is the eleven-instances pattern
repeating one more time — and this time with two purpose-built guards reporting clean.

### The numbers

Content pixels on the last drawable column/row of the 1536x891 clip rect, at 1600x929:

| | right edge | bottom edge |
|---|---|---|
| before | **54 / 54** gameplay screens | **54 / 54** |
| after | 0 / 54 | 0 / 54 |

Left and top were clean throughout — the asymmetry is what identifies a clip rather than a design
margin. `01_country_selector` reads flush on all four edges in both passes and is not a finding:
`DrawMenuBackground` is full-bleed by design.

### What was wrong

Five budgets, one mistake: each computed from the space a container was *allocated* rather than the
space its children can *use*. `DrawConsolidatedTabs` divided by six without the per-button margins
(~32px, the Politics tab); the left column box was handed its full column width with its own margin
added outside (8px, carrying the right column with it); `InnerWidth` carried three of the four terms
(~9px, "Federal Reserve"); no `InnerHeight` existed, so `_boxStyle`'s vertical chrome was never
subtracted from `areaHeight`; and two reserves were fixed multiples of the font size standing in for
wrapped prose — the calendar strip's one line against a two-line pause banner, and the Budget screen's
186px against ~290px drawn, which put the columns row ~100px below the screen.

Fixed with a fourth term in `InnerWidth`, a new `InnerHeight`, one shared accessor for the tab bar's
height, and two measured header accessors reading the same strings the drawing prints.

### Validation

Two independent Editor capture passes at 1600x929, exit code 0 both times, `55 captured, 0 failed`,
0 text overflows, 0 containment escapes, identical across runs. Not covered: any other aspect ratio.

### The transferable part

Both guards passed while this was on screen, and correctly so — one asks whether text fits its rect,
the other whether a child rect sits in its container, and this was a layout group overrunning the clip
rect everything is inside. **A guard that reads the content cannot see a defect in the frame.** The
check that did find it reads the PNGs the capture pass already writes and needs no engine access — it is
now `screenshot_edge_check.py` at the repository root, exit 1 on any clipped screen, verified against the
pre-fix set as a negative control. Full record, including the two standing rules it produced, in
`CLAUDE.md`.

---

## 18. The debt-to-zero bimodality — FIXED 2026-08-02 (migrated from the roadmap 2026-08-12)

Migrated intact in the reconciliation pass; the roadmap keeps a pointer. The "successor defect" its
closing line names (the deficit-term thrash) was itself closed by the SWF cause-fix (F1's 98.7%
reduction), whose own successor — the unbounded divergence — is the parked fiscal pass.

## ✅ The debt-to-zero bimodality — FIXED 2026-08-02. Its successor defect is named at the end.

**The floor is gone, debt may go negative, and the 0.00% pinning with it** — debt-swing anomalies fell
60% across the full matrix (6,225 → 2,507, 100 and 500 turns, seed 777, like-for-like before/after).
A symmetric −300% bound was needed to stop an unbounded negative runaway; see Open Questions, because that
bound is my call rather than Elias's.

🔴 **What it was hiding, and what now carries the priority: the rating thrash is the DEFICIT term's.**
Removing the floor moved rating anomalies by 1.6% (1,416 → 1,394) while
`DebtClampDiagnostic` reports the debt stock's own contribution as almost perfectly stable — 0 notch moves
in 117 years for four of six countries. **Step C4's closure waits on the deficit term, not on this.**
Full evidence in `CLAUDE.md`. The original entry follows, kept because the mechanism reasoning is what
made the fix possible.

### Original entry — the defect as it stood before the fix

**What:** Sweden's, France's and Germany's `DebtToGdpRatio` collapses to exactly **0.00%** and, under
stress, spikes back to ~44% and collapses again within a year. Sweden in plain `baseline`: 21.8% (turn 1)
→ 0.90% (turn 25) → **0.00%** from turn 50 on. Full technical history in CLAUDE.md, "SpendingLine Amount
Ceiling — Debt-to-Zero Fix"; this is roadmap failure pattern 4, bimodal attractors.

**Not new. Its PRIORITY is new**, and for two specific reasons:

1. **It now blocks a step.** Step C4's closure waits on it — see `MISSING_PREREQUISITES.md` section F1.
   No previously-known consequence of this defect blocked anything; it was a background modelling
   concern that batch runs reported and nothing acted on.
2. **It is now player-visible.** Until 2026-08-02 this defect was **log-only** — it lived in anomaly
   counts, batch summaries and prose. Step C4's credit rating is the **first instrument that surfaces it
   on screen**: the tile sits in the dashboard grid on every tab and reports its input faithfully, so a
   debt stock swinging 0%↔45% now reads as a rating visibly collapsing and recovering. **The defect did
   not get worse — it got a display.**

**Do not fix this by damping the rating.** That option was raised and explicitly rejected in A1, and
doing it now would return the defect to log-only while making C4 dishonest. A derived stat that stayed
calm while its inputs did this would be the broken one.

**Scope note:** the affected set is exactly the documented one. USA, Italy and Poland have well-behaved
debt trajectories and produce **zero** rating anomalies both before and after the cadence change, which
is itself evidence the rating is reading faithfully rather than misbehaving.

### DECIDED 2026-08-02 (Elias, delegated) — allow net government debt to go NEGATIVE

**Approach: remove the zero floor rather than damp the symptom.** `Mathf.Clamp(debt, 0f, maxDebt)` is
what creates the bounce artifact — a stock driven below zero is held at zero and then released, which is
exactly the shape a bimodal attractor takes.

**Why negative debt is correct rather than a hack.** A country whose sovereign wealth fund exceeds its
debt is a **net creditor**, which is a real fiscal state — and specifically **Norway's**, the country this
project already used to calibrate SWF returns. The game *already displays* "Net Government Position (debt
minus fund assets)"; it is only the simulation that refuses to represent it. Clamping at zero encodes an
assumption the UI has already rejected.

⚠ **DO NOT IMPLEMENT UNTIL THE MECHANISM IS CONFIRMED.** Verify against a real trajectory that the zero
clamp is what produces the 0.00% → ~44% swings, and establish whether it **fully** explains the −135.5% to
+170.8% settled-deficit range or whether something else contributes. **Three wrong theories preceded the
right one on the Unity batch-run hang** — that precedent is why this is gated. Report the mechanism before
proposing an implementation.

**MECHANISM CONFIRMED 2026-08-02 — the gate is satisfied, implementation may be scoped.** Full evidence in
CLAUDE.md. In short: the FLOOR is the mechanism (Sweden 67/120 baseline turns, France 14/120); the
**ceiling is never hit by anyone**, so `MaxDebtToGdpPercent` is not involved; and the affected set is
exactly "countries whose SWF drives net position negative" — which explains Germany, whose anomalies occur
**only in `swfstress`**, where its debt does reach 0.0% repeatedly. Elias's premise holds and is stronger
than stated: Sweden is a net creditor from **turn 1**, reaching a net position of −599 by turn 16, with
single-turn excursions to −64.3% of GDP.

⚠ **It does NOT fully explain the deficit range.** The per-turn budget balance is itself volatile
(Sweden: +79, +16, +48, +0.8, +30, −40 …) and that volatility is upstream of the clamp. Removing the floor
should eliminate the 0.00% pinning and the bounce; whether it eliminates the rating thrash entirely is
**not** established. Re-run `DebtClampDiagnostic` with the floor removed and check year-over-year deltas
against the notch threshold — if they still clear it, the residual is budget-balance volatility and is a
separate defect this one was hiding.

⚠ **Design decision to settle before building:** with debt clamped at zero, interest on debt is zero, so a
net creditor currently earns nothing on its net assets. Removing the floor without deciding how negative
debt interacts with `GetInterestOnDebt` creates either free money or a new asymmetry.

---



## 19. Round 4 — the five-batch stat-and-content arc (Master Sequence item 6)

*Scoped 2026-08-16 (read/map/propose session, six rulings approved). Shipped R4-1 through R4-5,
2026-08-16 → 2026-08-17. CLOSED 2026-08-17.* The five CLAUDE.md "Round 4 batch" records are the
detailed authority; R4-5's record carries the arc verdict, the consolidated write-back ruling
queue, and the post-Round-4 board. This section absorbs the scoped plan the roadmap held while
the arc was live.

**The five batches, as shipped:**

| batch | shipped | content | step-0 finding | coupling |
|---|---|---|---|---|
| R4-1 | 2026-08-16 | **C3: youth unemployment + life expectancy** (EconomyState 28→30) | scoping's "6/6 both" overstated — life expectancy 4/6 + France `p`/Poland `ep` provisional; monthly-class at source, not annual | none |
| R4-2 | 2026-08-16 | **C2: Gini + real wage index** (30→32; the compounding-index class born) | "Gini 6/6, normalize first" wrong on both halves — 5/6 + USA `[ESTIMATED]`, normalization already closed at source; real-wage basis resolved BY RULING (index 100 at epoch) | wage↔inflation as a signed model term |
| R4-3 | 2026-08-16 | **C1: housing** — overburden (EU five), homeownership (USA-primary), HPI (32→35); `Clone()` → MemberwiseClone | CLEAN (streak ends at two) | **the arc's first monetary coupling** — one-way rate READ vs the zone's epoch anchor |
| R4-4 | 2026-08-17 | **Defense, Foreign Affairs, Education** — 9 signed ministers, 18 scenarios, 2 new shock fields; Education competence live on youth-U (the one model term); D1 portraits unblocked | the R4-3 verdict's open RNG question answered at step 0: no shared stream exists | decision shocks at event scale; ONE passive term |
| R4-5 | 2026-08-17 | **C5: productivity** (35→36) — GDP/hour USD PPP, real levels, own-past-only, live-until-sourced | CLEAN (final tally: 2 corrections in 5 batches — step 0 stays mandatory) | none — the PotentialGrowthRate coupling ruled OUT (ruling #4) |

**The standing posture that governed the arc (rule 11):** first pass inputs-only — new stats read
existing state and write nothing back; no new entrant into `PotentialGrowthRate`'s or LFPR's
heavily-stacked ceilings; every coupling is a separately-ruled follow-on with its stacking audit
attached. The one deliberate exception was itself ruled (R4-4's R3: Education competence →
youth-U target, whose audit is empty because nothing reads youth-U back).

**The validation bar, held five times:** fresh pre-batch trajectory baseline + full matrix (every
batch byte-identical on all shared fields with exactly the new fields named — including R4-4's
content-batch form, where byte-identical WAS the criterion and held); aggregation-equivalence
extension with stated enumeration for every new daily model/term (88 → 110 rows across the arc,
all exact-by-construction); bucket asserts or exclusion-with-reason (three exclusion variants
now recorded); save/load round-trip at every new field count (28 → 36, with R4-4's new-portfolio
keys crossing a save); publication wired only where the seed doc records a real release rule —
live-until-sourced otherwise (HPI, Productivity); captures at both sizes with zero overflows,
plus R4-4's pinned-capture pattern for probabilistic content.

**Candidates the scoping excluded, still excluded at close:** C4/credit-rating follow-ons
(behind the stock-vs-flow mechanism report, §F1); item 10's collision-mapped territory (gated
13 Sept); a bond-market mechanic (namespace deliberately left to the mechanism report to claim).

---

### R4-4's pre-report, consumed (2026-08-27 — the retention trigger fired with Progress5)

*`POLISIM_R4_4_PREREPORT.md` (2026-08-17, `dd3ccfc`) was kept on disk by §22's ruling until D1's nine
portraits landed; they did, and the file is deleted — git history holds it in full. What it carried
that existed nowhere else:*

**The name list — ruling 6's deliverable, signed by Elias as R4-4 ruling R1 (checked 2026-08-17).** One
GLOBAL candidate pool per portfolio (Part A's structure — the same nine characters serve whichever
country the player governs), deliberately cross-cultural pairings, all ASCII for the slug rule:
Defense — Katarzyna Ekelund (Reformist), Rafael Iwasaki (Pragmatic), Gunnar Petrakis (Traditionalist);
Foreign Affairs — Camille Adeyemi (Reformist), Zofia Nakamura (Pragmatic), Aleksander Whitfield
(Traditionalist); Education — Yuki Dahlberg (Reformist), Nadia Fitzgerald (Pragmatic), Tobias Marchetti
(Traditionalist). The shipped values are `CabinetSystem.CandidatePool`; the filenames derive from them.

**The collision search — the absence claim's named search, four parts:** (1) by construction, each name
is a cross-cultural pairing structurally unattested in any single country's political class; (2) a
model-knowledge sweep of each full name and each surname within its portfolio against cabinet-level
officeholders past and present of the six simulated countries — no full-name match, and one
prominent-surname overlap caught and REMOVED before the list (the Education Reformist was drafted "Yuki
Andersson"; Andersson is a recent Swedish prime minister's surname); (3) a live web search of each exact
full name, quoted — no officeholder or public figure matches; private-citizen homonyms are unavoidable for
any plausible name and are not the bar; (4) the in-game name space — no overlap with the sixteen existing
portrait names, given names included. **Rule 9's unreversed half governs all nine: original and
fictional.** The seven rulings (R1 the list; R2 the two shock fields; R3 the asymmetric competence
channels; R4 coexist with `ForeignPolicySystem` and the cadence comment corrected; R5 Part A parity; R6
the two-part trajectory criterion; R7 the area colours defaulted) and the batch's record are CLAUDE.md
"Round 4 batch R4-4".

## 20. Playtest 1's package and the law system — shipped 2026-08-18 → 2026-08-25

*A pointer entry, per the three-way test: finished work leaves the live file. The detailed
authority is CLAUDE.md, entry by entry — this section names them so the roadmap can stop.*

**The first real playtest (2026-08-18) produced seven findings: two fixed that day, five scoped
in the Playtest-1 scoping package (consumed to §21, 2026-08-26), and all but one are now closed.**

| item | shipped | CLAUDE.md entry |
|---|---|---|
| The signing ceremony's seal on rejected bills; the Budget tab's dead nested scroll | 2026-08-18 | "First real playtest session" |
| Turn → Year (display boundary, ~39 formatter sites, nothing renamed) | 2026-08-24 | "Turn -> Year" |
| Calendar Panel (replaces the dashboard tile grid; reads existing data, builds no store) | 2026-08-24, `a13dd7b` | "Calendar Panel" |
| Decision density — MEASURED at 50 laws, same method as the scoping: prompts/yr unchanged by construction (≈5), named enactable choices 19 → 69 | 2026-08-25 | "Decision density re-measured" |
| **The law system** — see below | 2026-08-24 → 2026-08-25 | six entries, listed below |
| Portraits (D1) | **NOT closed** — ~~8 of 9 still gated on the Editor register side-by-side~~ **gate CLEARED 2026-08-26** (register side-by-side PASSED); the batch of nine now waits on Design's delivery — `MISSING_PREREQUISITES.md` §D1 | — |

**The law system, 50 of 50, one category.** A law is a NAMED PRESET over the existing dial space —
name, description, one-sentence real-world citation labeled CONFIRMED/DIRECTIONAL/GENRE-IDIOM,
deltas on up to six Crime & Justice dials within a four-tier magnitude scale, an approval cost paid
once on passage — reaching Parliament through the same gated-bill path every other bill uses,
several pending at once, 21-day resolution. **The composition architecture** (the marathon's one
real bug, found at close-out and fixed): the six dials are a PURE FUNCTION of `Country.EnactedLaws`
— every enacted law's delta summed from the 50 baseline and clamped exactly once — never nudged
incrementally, so any history of enactments and repeals in any order lands exactly; proven at 38
(one dial at the ceiling) and re-proven at 50 (four dials clamping at once, full repeal netting
exactly 50.0000 on all six). The browser is Design's board 1i built to its rulings doc: a
list+detail split, status GROUPING (in force first) instead of a status column, a sticky header
sharing one column function with every row, the stepped magnitude rule, the citation surfaced in
the UI for the first time. Byte-identity for the no-law path holds by construction — `LawCatalog.All`
is read only from the UI layer.

Entries, in order: "Law System MVP Slice" (08-24, `ca11f9a`) · "Law content marathon — STOPPED at
38/50" (batches 1–3 + close-out, `555f4cc`) · "Progress4 delivered — the law browser board (§7)"
(`315cca0`) · "Post-Progress4: a Unity hang investigated, and a mockup number caught inside its own
fix" (the rebuild's review, `dddec9f`) · "Law content marathon, resumed and closed: batches 4–5"
(`eb11b78`) · "The detail-pane width, ruled and built" (`6804c6d`).

*Its scoping package, `POLISIM_PLAYTEST1_SCOPING.md`, was consumed to §21 and deleted in the
2026-08-26 consolidation — every scoped item above was dispositioned before the file went.*

**What stays live, and where** *(updated 2026-08-26)*: the category filter's inertness (a content
gap — five of six `LawCategory` slots at zero; the second-category pass is now scheduled in the
roadmap's ruled build order); ~~the fiscal legibility panel (trigger fired, unbuilt)~~ — **SHIPPED
2026-08-25** as `StatTracePanel`'s third section (CLAUDE.md "Step 2's third section ships"); the
courtesy update to Design — rewritten 1j-aware and riding the send package
(`MISSING_PREREQUISITES.md` §S).

---

## 21. The three 2026-08-18 scoping packages — consumed and deleted (2026-08-26)

*Consolidation-pass migration, per the three-way test: every ruling each package asked for landed
and shipped, so the files went the way `VISUAL_REVIEW_BACKLOG.md` did. The build records remain in
`CLAUDE.md`; this section is the packages' own record, with what each left behind.*

### Step 2 — Causality Legibility (`POLISIM_STEP2_LEGIBILITY_SCOPING.md`)

R-S2a–e ruled and built the same day — v1 shipped 2026-08-18: the approval ledger, the trace panel,
the preview-parity diagnostic, the one-period save shape (CLAUDE.md "Step 2 v1 ships"). **The
lasting design law — the four attribution classes** (A boundary formulas / B dated events / C
period stances / D compounding feedback, which renders drivers plus a NAMED RESIDUAL and is
thresholdable but never attributable) **and the honesty rules built on them** (period-true
rendering, event dates, equilibrium framing, the single book) — carried directly into Step 3's
objective grammar and the debt ledger's third section. Deferral dispositions at consolidation:
tooltips-as-pointers (trigger: discoverability feedback from playtesting — unfired, stays);
the **causal-graph screen — trigger DECLARED FIRED 2026-08-26** (the ledger carries a third
stat's terms since 08-25; queued in the roadmap per the fiscal-chain precedent); the narrative
layer (only-if-ever by design, stays as recorded).

### Step 3 — Challenge Mode (`POLISIM_STEP3_CHALLENGE_SCOPING.md`)

R-S3a–f ruled and built (the slice shipped 2026-08-18). The package's central derivation, kept:
**a validation scenario supplies the DECISIONS and holds the world constant; a playable one
supplies the WORLD and lets the player decide** — with the four seams located (entry
`SelectPlayerCountry`, evaluation on the `CheckElection` hook, ending `_isGameOver`/`_gameOverReason`,
persistence as one id + counters) and the published-vs-live finding (`DebtToGdpRatio` is
deliberately never published, so the slate's strongest scenarios structurally cannot be
published-judged). Slate dispositions at consolidation: Inherit the Fund SHIPPED · Italy Debt
Crisis SHIPPED · The Disinflation DROPPED (measured) · Wage Boom Management DROPPED (measured) ·
**Poland convergence and The Unequal Recovery LIVE** — their §2 specs migrated to the roadmap's
Step 3 block before deletion (ruled 2026-08-26: keep as live content backlog, build when elected).
**R-S3e's residue closed by ruling (2026-08-26):** the three-rate FA-cadence playtest is
SUPERSEDED by the 08-25 decision-density ruling and the "does decision density READ as closed"
riding gate; what it was ruled to produce — the per-scenario cadence multiplier — is built
(`ForeignPolicyCadenceMultiplier`, default 1, both shipped scenarios at 1).

### Playtest 1 (`POLISIM_PLAYTEST1_SCOPING.md`)

All five scoped items dispositioned — §20's table is the record. The one non-closed row
(Portraits/D1) is gated in `MISSING_PREREQUISITES.md` §D1: the register gate CLEARED 2026-08-26,
now genuinely waiting on Design's delivery of the nine. §3's decision-density measurement was
re-run at 50 laws with the same method (2026-08-25, CLAUDE.md "Decision density re-measured") —
the method survived the file.

---

## 22. The derivation and measurement reports — consumed and deleted (2026-08-26)

*Ruled 2026-08-26 (the consolidation pass): discharged derivation and measurement reports
migrate here and delete, with every citation repointed — code doc comments included. Git history
preserves each report in full, the §12 precedent; this section carries the rulings, the
consumption record, and the data that existed nowhere else. One deliberate exception:
`POLISIM_R4_4_PREREPORT.md` stays on disk until D1's nine portraits land — its §4 collision
search is actively cited by the in-flight art request — then follows this same rule. (✅ The nine
landed 2026-08-27; the file is consumed to §19 and deleted.)*

### The stock-versus-flow mechanism report (2026-08-17, `bcbba47`)

The fiscal arc's central derivation. Its findings, all dispositioned: **1a** — the whole stock
repriced DAILY at spot + premium (the strongest form of the assumption maturity structure
attacks; the USA's 3.3 override was already a frozen maturity model accepted once). **1b** — the
model's dollars are constant-price units and the stock update never saw inflation, so the
standard identity's **−π·b erosion term was missing** — sized at one to two orders of magnitude
larger than the measured divergence slopes (+0.014…+0.037 pts/turn) it would oppose. **1c** —
primary-surplus rules die on Italy's own evidence (+75 implied primary surplus, diverging
anyway). **F1** — every interrupt-layer `BudgetImpact` reached only the display accumulator,
never the debt stock. Rulings and ships: **R1–R3 the erosion term** (`685ebd5`) — with **R3
ruled SYMMETRIC against the report's positive-debt-only recommendation** (no free money in
either direction; a ruling made ON the report, not a gap in it); **R2** the accounting
convention, restated as the standing property at the top of CLAUDE.md; **R4 maturity** — the
`EffectiveDebtInterestRate` rate-lag (`b05150f`), the USA carve-out generalized; **R5/F1** —
`ApplyOneTimeBudgetImpact` (`720ccee`), interrupt impacts reach the books. §F1 of
`MISSING_PREREQUISITES.md` closed on it 2026-08-17: C4 done, A1 with it, F register zero. *(The
roadmap's own bullet for this arc still read "next: the report… C4 waits on it" nine days after the
report ran — a recorded instance of the cached-status pattern, struck 2026-08-26 and deleted with the
bullet 2026-08-27.)*

### Q1 — Gini → ApprovalRating (2026-08-17; shipped same pass)

Measurement decided the form: **Gini is FLAT at baseline** (±0.15 pts of seed, t1–t1000, all
six), so a change term is inert and a raw level term is a recalibration — the **GAP form**
`−s × (Gini − BaselineGini)` won by elimination and by the formula's own two precedents
(paid leave, welfare). R-Q1a gap form · R-Q1b 1.0 equilibrium pt/Gini pt (s = 0.05, band
0.5–1.5) · R-Q1c no new ceiling, the absence NAMED and handed to the legibility feature.
Matrix bar held: ApprovalRating the only moved field. No file or code cited this report.

### Q2 — real wages → ConsumerConfidence (2026-08-18; shipped `ef7cbf2`)

The §1 measurement, preserved because it exists nowhere else: **RealWageIndex is an unbounded
compounding index** (t1000: Poland ~3.5×10¹⁵, USA ~2.3×10⁹, France ~1.8×10⁴), so a level gap is
structurally impossible; the measured wage-growth gap runs a **persistently positive mean
(+0.011…+0.035 pp/turn, sd 0.035–0.040)**, which **disqualifies any accumulator form** — at any
legible sensitivity it ratchets monotonically into the 1.3 confidence clamp (s = 0.05 exhausts
the 0.3 headroom in ~300 turns). One deterministic transient, named so it is never misread:
Poland's t2 gap of **−0.4841 pp, bit-identical across seeds** — the seed-convergence squeeze,
gone by ~t5. **Form B — the stored-field delta model — was the named alternative, with its three
costs recorded here**: a new `EconomyState` field (save-shape change), rewriting both policy
writers' permanence semantics to target the base, and the stored value turning dynamic under
every reader. R-Q2a form A (stateless effective factor, single-book rider) · R-Q2b 0.5%C/pp
(band 0.25–0.75) · R-Q2c the shared helper (Q5's seam). The bar's own catch became the fifth
fixed reference (`WageGrowthGapAtPeriodOpen`).

### Q3 — productivity ↔ PotentialGrowthRate (2026-08-17; shipped `d1cb1de`)

The derivation surfaced the premise contradiction: **Design B (adjustments flow through
productivity) is the economically true claim AND value-identical at HEAD** — a pure causal
re-rooting whose bar is byte-identical, directly contradicting the brief's "trajectory-moving by
construction" (struck with this report cited). Design A moved trajectories only by asserting
something false about the world. R-Q3a Design B · R-Q3b 1:1 (later amended by R-Q5d — an
amendment, not a correction) · R-MS2 the canonical six steps. Bar: 39/39 byte-identical.

### Q5 — the cyclical pair (2026-08-18; shipped `7321807`)

The §3 measurement table, preserved as the standing evidence (it is also Riksbank option B's
gate-1 evidence — the Taylor-path output-gap distortion; *gate 1 CLEARED 2026-08-26 by pass 4,
which re-measured this table at HEAD: the EU-five figures below predate the recalibration — Poland
reads −6.9, Italy −4.5, Germany −2.7, Sweden −0.8 after it; USA unchanged*): **the output gap is a persistent
per-country LEVEL, not a cycle** — no-policy t1–t1000 means: USA **−14.54%** (sd 0.64 — seeded
PotentialGDP 12.8% above GDP, never converging), Poland −4.52, Sweden +3.86, Germany +0.32,
Italy −2.36, France −0.06. A term on it is a per-country constant — Q1's disqualified raw-level
form on a different variable. **The unemployment gap is the driver**: mean −0.04 pp, sd 0.19,
transients decaying by ~t5. Investment has no stock anywhere and measured I/GDP is flat
19.5–20.9% — nothing cyclical to deepen from. Loop gain derived 0.075×h ≈ 0.03 at h = 0.4,
then MEASURED at 0.0297–0.0300. R-Q5a = B1 (additive force, the model's first closed loop) ·
R-Q5b two channels · R-Q5c h = 0.4 on the U gap · R-Q5d R-Q3b amended (potential reads trend
alone; the stat and wages read trend + cycle) · R-Q5e investment deepening DEFERRED (now
carrying its ruled return trigger in the roadmap: a capital stock ships, or I/GDP measures
cyclical).

### Wage Boom Management — measured and DROPPED (2026-08-18)

`UnemploymentReversionSpeed = 0.7/turn` closes any impulse by t2–t3 and forecloses sustaining
tightness: **every lever tested — including the rate cut to the absolute 0% floor — produced a
max streak of ONE turn** at gap ≥ 1 pp. The §2a methodology correction stands recorded: the
eight legacy `PolicyDecision` float fields are DEAD as inputs for every seeded country
(`SpendingLineChanges` is the real input). §4's independent finding: **the USA is disqualified
for any inflation-management scenario** — TaylorRule's 0.5 × (−14.5%) structural gap term pins
the suggested rate to the 0-floor regardless of realized inflation *(→ AMENDED 2026-08-26 by pass
4: the term is gone and the Fed responds — the rule-based half of the disqualification is lifted;
the reversion-based half, the named pattern above, stands. CLAUDE.md "Pass 4 ships")*. The `Sustained` form was
exercised synthetically (evaluates exactly; survives a save mid-streak; the verdict screen's
generic margin line never read `ConsecutiveTurns` — fixed 2026-08-18 per the sustained
verdict-margin closure).

### The Disinflation — measured and DROPPED (2026-08-18)

The mirror image: the same constant prevents CREATING slack above NAIRU. Ten configurations,
four countries, every lever up to a 5-point hike and the Eurozone's own auto-climb to 8.6% —
**every run ends within a point of where it started**; measured slopes −0.065 (Poland) and
−0.08 (Sweden) pp of terminal inflation per pp of hike (reaching 2% from 10% would need a
~120-point hike against a 15% cap). The player's ±0.75 Eurozone push is invisible to three
decimals. Elevated inflation alone crashes approval below the 35-point losing threshold in
3–7 turns — an independent kill. **Two drops on one root cause became the named model-balance
finding**: as tuned, the constant forecloses the whole "move the unemployment gap off NAIRU and
hold it" scenario class, from either direction.

### Italy Debt Crisis — measured, SURVIVES, shipped (2026-08-18)

Seven same-seed configurations spread **52.63%–109.60%** debt-to-GDP by t30 on instrument choice
alone. **Spending cuts compound** (−0.16 pp/pp at 10% → −0.99 at 20% → −1.90 at 30% — the
stock/interest feedback working for the player); **VAT hikes plateau** (−0.43 → −0.90 pp/pp).
Approval: cuts nearly free (≈−0.017 pts/pt), **VAT ≈90× costlier** (≈−1.50 pts/pt) — the
+6pp-VAT line dips to 39.48 against the 40-streak objective and recovers, 9.5 points clear of
the 30 floor. Eurozone monetary impotence is the scenario's PREMISE, not a gap. Authored as
`ItalyDebtCrisis()`: Terminal + the `Sustained` form's first real exercise + NeverBreach.

---

## 23. The blocked-work register, slimmed — closed sections migrated in full (2026-08-26)

*`MISSING_PREREQUISITES.md` kept only its live entries in the 2026-08-26 consolidation (the send
package §S, D1, E2, E3, and §B's three quality debts). Everything below is the closed half,
migrated whole where a ruling said "kept so none is reopened".*

### A1 — the rating thrash: REVIEW CADENCE, not damping (ruled 2026-08-02; closed 2026-08-17)

**The primary recommendation (cap + multi-turn average) was rejected.** Elias took the
alternative raised almost in passing: the rating updates on a scheduled review cycle. **Why the
review cycle is stronger — four reasons, all recorded:** (1) it is what actually happens —
agencies review sovereigns on a cycle rather than re-rating continuously, so the scheduled review
IS the real-world mechanism that prevents real-world thrash; (2) the machinery already exists —
Step A built the release-calendar system for exactly this shape, a value evolving continuously
underneath and surfacing on a schedule; (3) precedent already in the game — central-bank rate
decisions run on ~8 scheduled meetings a year; (4) it dissolves the problem by construction
rather than tuning it — rating off a settled annual fiscal position keeps the 5-anchor
calibration valid instead of needing re-derivation against a smoothed term. **Why damping was
weaker:** it makes the thrash smaller without removing why it exists, every constant chosen to
suppress it is a number nobody can justify from anything real, and it lands directly on the term
the calibration runs through. Implemented `a4155ca`; **closed 2026-08-17** — the 5-anchor check
at HEAD (erosion + maturity + F1 shipped) held 5 of 6 with Poland the recorded expected-fail;
nothing left to damp. The cap+average recommendation stays recorded as the answer IF the term
ever drifts; the expected-fail row and the anchor check are the standing tripwires.

### A2 — SWF emergency drawdown: a standalone tier-3 bill (ruled AND built 2026-08-02, `b1c077f`)

Recommendation accepted as written: 5d's existing tier-2/3 mechanism, a fifth tier-3 type
alongside Labor / CrimeJustice / Sector / Trade. Not bundled into the annual budget; not fully
exempt like the Fed/Eurozone carve-out. Real governments handle fiscal emergencies through
expedited votes rather than unilateral action; Norway's own GPFG withdrawal is an ordinary
budget-process matter; zero new mechanism. The gap was live when ruled — since 5c, SWF changes
rode the annual omnibus bill, so an emergency could sit behind a fiscal-year vote up to a year
away. Elias's framing: *"a gameplay bug wearing the costume of a design question."*

### A3 — cabinet appointments stay UNILATERAL (ruled 2026-08-02)

**No parliamentary vote to appoint a minister.** The reasoning, recorded because none existed
before: (1) it preserves a distinction the game already makes well — Parliament gates *policy*,
appointments are *executive*, and one gate for both flattens a separation the gated-legislation
model deliberately created; (2) there is already a cost — reshuffling carries an
`ApprovalRating` hit; (3) a vote would make Cabinet worse to play — a multi-week legislative
process in front of every appointment turns a responsive system slow for no gameplay gain;
(4) it is defensible in the real world — confirmation practice varies enormously across the six
modelled countries. **Nothing to build; the ruling confirms the code.** ⚠ The 08-17 gate list's
"A3 at its trigger" re-listing was a two-authors artifact — it re-derived from the pre-resolution
`<details>` block without seeing the resolution above it; struck from the riding-gates row
2026-08-26 (confirmation C1).

### B — database access: emptied 2026-08-02; three quality debts stayed in the register

Every figure that blocked a batch was sourced across three sessions (Eurostat REST, OECD SDMX,
BLS/FRED, and — three items — stated banded estimates under the fallback ladder); values and
bases in the seed doc. Dispositions: **B1** housing closed (Italy 74.4 / Sweden 62.1 / Poland
86.8 `[ESTIMATED]` from a four-country bridge, ±7pp band); **B2** inequality + real wages closed
(USA Gini 39.5 `[ESTIMATED]`; the real-wage row's three-bases debt born here); **B3** youth
unemployment + life expectancy fully sourced (rate-not-ratio guarded); **B4/C5** productivity —
all six sourced exactly from the OECD SDMX API, one basis one vintage, later `[VERIFIED]` via the
DBnomics anchor (Poland's Statista figure had been off by more than 2×); **the C5-keep decision
(Elias, delegated): keep productivity, lowest priority of the four** — a cut that unblocks
nothing is not a simplification, and the OECD's own-past-only caution suits the game; **B5**
Poland's rating A− (a validation anchor, never a seed — and the monotonicity finding: lower debt
than Germany, four notches worse, no penalising term exists → the expected-fail anchor row);
**B6** overburden closed 2026-08-17 by R4-3's re-adoption (whole-population `ilc_lvho07a`
`[VERIFIED]` values 5.1/7.0/5.2; USA homeownership-primary by ruling, the asymmetry deliberate
everywhere). The C1 metric escalation (B1/B6's three options and per-option gap lists, was
`STEP_C1_HOUSING_GAP_REPORT.md`) resolved by C1a/C1b/C1c in §11 and the R4-3 reversal above.

### C · D2 · E1 — one-liners

**C** (visual review): emptied 2026-08-02, all eleven confirmed — §16. **D2** (Round 4 scoping):
released 2026-08-02 when step 5 closed; the arc itself closed 2026-08-17 — §19. **E1**
(`icon_stat_interestrate`): delivered the same day it was recorded as awaiting — §15 carries the
story and the lesson (a delivery is not self-announcing; enumerate the display enum).

### F1 — Step C4's closure (CLOSED 2026-08-17; the F register ends at zero)

The chain, kept whole: the parked fiscal-divergence pass ran and closed (the mechanism report
`bcbba47` → the erosion term `685ebd5` → the maturity rate-lag `b05150f` — two identity terms the
ledger was missing, found by measurement, shipped one per baseline). Elias ruled proceed on A1's
re-run; `CreditRatingAnchorCheck` at HEAD: **5 of 6 PASS, every calibration anchor HELD with no
drift** (USA's AA+ deficit window [4.6%, 7.5%], anchor at 6.4%), Poland the recorded
expected-fail. The anchors were calibrated against a divergence that no longer exists and
survived its removal. **C4 closed; A1 closed with it.** The measurement record that preceded it:
**rating anomalies 1,416 → 19 (98.7%)** across the stages *floor removed 1,394 · SWF returns
inside the multiplier 1,020 · structural draw (smoothing) 19*, with debt swings 6,225 → 140 —
**the decisive change was the double-count fix** (the realised SWF return was added to the fund's
assets AND booked as government revenue; the budget now takes a 3%/year structural draw WITHDRAWN
from the fund, Norway's own fiscal rule). One flag raised then and still worth an eye: Sweden's
debt ratio came out very flat (13.3% → 10.7% across 120 turns) — possibly too quiet, a different
question from the one the fix addressed. The superseded framings (the 2026-08-11 "waits on the
parked pass" reading; the 2026-08-02 original) are in git history with this file's §13/§18.
*(→ 2026-08-26, pass 5: F1's "the writers were exactly two" was wrong by one — the SWF emergency
drawdown bill (A2 above) wrote the display accumulator alone and never lowered debt; found by pass
5's retirement sweep and closed onto F1's own path. CLAUDE.md "Pass 5 ships".)*
*(→ 2026-08-27, pass 6: the free-lever exploit class pass 5 recorded is closed — the change in the
tariff take passes through to prices for a year, partners mirror an override's excess, and overrides
enter the vote as the average tariff; pass 5's Sweden 33.8 → 24.7 is now 33.8 → 27.5 with GDP −5.9%
and the bill failing at seed. CLAUDE.md "Pass 6 ships".)*

---

## 24. The v2.0 design collaboration — §1–§1G, §6, §7/§7.1 consumed from the request doc (2026-08-26)

*`CLAUDE_DESIGN_ASSET_REQUEST.md` returned to its charter ("appended to, then emptied on
delivery"): the answered arc migrated here, git history holding every original section in full.
What survives there: the conventions (§3/§4), the scope rules (§2), and the live asks (§5, §8, §9).*

### The brief and the two chrome passes (§1, §1B, §1C — 2026-08-03)

The v2.0 brief asked Design to research Suzerain directly and say where the idiom would NOT work.
Pass 1: 41 sprites + palette + `DIRECTION.md` + `CANVAS_SPEC.md`. Pass 2 answered every §1B item —
scrollbars with the arrow-button call made explicitly (**styled to NOTHING**, sprite plus zeroed
fixed sizes, both required); the chip judgment came back **"not a pill"** (a delta on paper is
inked text — retire `Pill` at stat-tile delta sites, keep the chip for printed badges); the
Pagella stamp re-cut; the missing companion documents restored. **The delivered thesis: "a
ledger, not a decree" — the idiom adopted at the PERIMETER and refused at the ROW**, with six
recorded refusals (W1 ornament per row · W2 texture under live digits · W3 transitions everywhere
· W4 a mood palette — the eleven hues are DATA INFRASTRUCTURE, aged not reduced · W5 the prose
register · W6 period as age), the eleven load-bearing behaviours each landed (B1–B11), the
hand-off envelope (scrim 0–180ms → swap at 180–240 → document entrance → stamp thunk at 580–700;
round trip ≤1.2s), and the dual-siting answer (frame, title band and plate ship as separate
sprites so the embedded path skips them).

### The eight screen boards and the revision request (§1D — 2026-08-10)

Nine items, Design's calls — the header said D4/D7's reasoning is still consulted, so it lives
here: **D1** the ✎ glyph exists in no shipped font (behaviour 11's failure landing on behaviour
1) — the carrier became the `icon_pencil_draft` sprite, never a text glyph; **D2** the division
bar drew a seat headcount the model does not compute — the diverging lean bar stands (a bug this
codebase had already fixed once), and D2's striking of headcounts was load-bearing again in Board
1j; **D3** the density board tested half the density (19 rows drawn, ~40 real); **D4 — still
consulted:** four data visualisations were never aged, and Design REFUSED to invent 29
distinguishable aged hues — **change the chart form rather than ship a worse palette** (the
eleven-hue floor's own logic one level down); **D5** party inks collided with area inks; **D6**
the third hue tint got its rule *(the snapped `tabTint.*` values were delivered — and are still unwired
at HEAD 2026-08-27; the tab swatch draws the area ink; the wiring is a roadmap item)*; **D7 — still
consulted:** uniform auto-shrink REJECTED — a
column printing at four sizes reads as an error; the answer is the resort ladder (screen spec
§A.9a); **D8** behaviour 6 stated backwards between two documents — the board's version won
(dashed = provisional); **D9** eight sprite names without files — four substituted, four became
§1E. The locale finding (1D.3): the boards' `$29,3T` was the sv-SE dev machine leaking in, not a
decision — the separator belongs to `UiFormat` and behaviour 3.

### Pass-3 follow-ups (§1E — closed 2026-08-10; verified by per-item enumeration 2026-08-11)

Five import blockers, all delivery-not-design, all closed: E1 `emblem_state_seal` →
`ui_seal_state` (the prefix is load-bearing); E2 `canvas_*` → `ui_*` (one namespace inside
`Chrome/`); E3 the two 3-cell strips split into single per-state sprites (the pack's own
established pattern); E4 PNG delivery restored (the pixels' ownership stays on Design's side);
E5 `icon_pencil_draft.png` shipped — D1's agreed carrier made importable. DEVIATIONS declared,
not requested: **V1** the "(current seat composition)" qualifier moved from row to screen header
(the board drew 8 rows; `TaxType` has 13 — the per-row verdict is NOT D2's deleted per-instrument
column: it scores the real standalone bill); **V2** Mandatory/Discretionary kept the build's own
group headers (the boards never addressed the split; a group property wants a heading). Both OPEN
QUESTIONS answered by later work: SHARE stays global (group-scaled bars carry within-group
discrimination); the row pitch re-derived as a DECIDED 36px at board scale.

### The party marks (§1F, §1F.1, §1F.2, §1G — 2026-08-11 → 2026-08-17)

The question — how a real party's identity is drawn without its trademark — came back better than
asked: **BALLOT STAMPS**, diegetic marks one election authority issues, which explains the family
resemblance rather than excusing it. The rules that came with it: silhouette classes unique per
chamber; solid ink, one counter ≥2px at 16px; **never the subject of the party's registered mark**
(no rose for S, no donkey, no elephant — rule 9a); national iconography stays in state chrome;
ink-safe colours constrain the `DisplayColor` seeds. Convention: `mark_party_*` ships
white-on-alpha and tints from seed data at draw time — a rebrand is a data edit, never a
redelivery. Four marks delivered (rep crest · dem torch · se_s banner · se_v star — the fourth
argued-for by Design over our three, and rightly: the S/V red-red collision is untestable with
three). **The DXT5 lesson**: metas copied from the filename-adjacent, treatment-opposite
`emblem_*` family imported all four block-compressed — §3.0a's copy-within-the-rendering-class
rule was born there, and `PartyMarkCoverageCheck` gained its format assertion (a handle coming
back proves the GUID, not the pixels). `mark_party_us_lib` followed 2026-08-17 (§1G); "Other and
independent" is a deliberate non-gap (a residual bucket, not a party). The R5 hex exchange rides
item 10 (`MISSING_PREREQUISITES.md` §E2). §1F.2's outstanding Zone.Identifier check closed
2026-08-26: no MOTW stream on any of the five marks.

### The verification notes (§6 — 2026-08-02/03)

Migrated with the rule kept standing in the surviving header: **a count in prose is a cached
value with no expiry** (rule 12) — re-derive sprite counts from the filesystem and screen
inventories from the enums. Three figures went stale inside two days while believed accurate when
written; the status header itself misled a reader twice.

### The law browser (§7 — OVERTAKEN; §7.1 — ANSWERED AND BUILT)

§7 was written and never sent: Design delivered board 1i + `LAW_BROWSER_BOARD_RULINGS.md` before
the send (`315cca0`), and the browser was rebuilt against them the same day (2026-08-25,
`dddec9f`). §7.1 — the playtest finding that 50 laws read as clutter — was answered by Design's
**Screen 1j ("Law browser at 50")**, drawn the day after the finding was sent, and implemented
same-day 2026-08-26 (`0bb7ebc`) with three explicit deviations: neutral dial-arrow ink (item 6's
ruled honesty), no seat headcounts (the model has none — Design's own D2), no sitting calendar
(the recorded 1i reason; the VOTE-IN countdowns are the real datum). The rulings doc stays at the
repo root as the standing 1i spec with a dated 1j header pointer; CLAUDE.md "Board 1j
implemented" is the build record; §20 carries the law-system story.

---

### The seventh request — sent, answered and imported in one day (2026-08-27)

**§1 the portrait batch:** sent with the register verdict at 21:0x, delivered by Design as
`PoliSim v2 Design Progress5.zip` at 21:36 (`ZoneId=3`, `HostUrl=https://claude.ai/`, SHA-256
`C9B26566…B109F`), verified on the four-pack bar and imported the same evening. **The bar, all six
steps:** (1) provenance and security — every PNG carries the PNG magic and is 512×640 8-bit RGBA; all
eight SVGs read in full, pure `rect`/`path`/`ellipse`/`circle` geometry with fill/stroke/opacity, no
script, handler, href, `foreignObject`, `image`, entity or `data:`; (2) completeness — the delivered
stems diffed programmatically against the names derived from `CabinetSystem.CandidatePool` by the
`Slug()` rule: 0 missing, 0 unexpected, spelling exact; every stem a PNG + SVG pair; the manifest's
pinned sizes matched 16/16; (3) conventions — decoded pixels fully opaque, 0% white, 105–164 distinct
colours per bust (full-colour, the Portraits class, correctly NOT white-on-alpha); `Portraits/` and
`Portraits/Source/` under `Assets/Resources/`; single sprites (textureType Default, `spriteMode 0`, the
PoC's own meta); (4) import — metas copied from the PoC's PNG and SVG metas differing only in the guid
line, 16 fresh GUIDs collision-checked against the 440 existing; copies byte-identical to the pack;
(5) **verified by loading — `PortraitCoverageCheck`, born for this import and added to `CheckSuite`:
25 of 25 pool members resolve through `IconLibrary.GetCabinetPortrait`/`GetFedChairPortrait` (18
ministers across six portfolios + 7 chairs; the eight new ones 512×640 DXT1 under the full-colour
compression ruling; the sitting chair reported, not counted)**; (6) `DeliveredAssetCheck` 0 missing
(Progress5.zip 16/16, no zips at the root), `StatIconCoverageCheck` 19/19, `ChromeV2CoverageCheck`
both directions clean, `ImporterSettingsCheck` 148 sprites, 0 errors, 0 warnings (FullColour 27 → 35).
The zip went straight to `AssetPackArchive/` (Elias's placement); the check covers it there. **The
cabinet set is complete — 18 of 18 ministers + 7 Fed chairs — and NOT yet seen: the roster with the
batch on it is `MISSING_PREREQUISITES.md` §V's.** `POLISIM_R4_4_PREREPORT.md`'s retention trigger
fired with the delivery (§22's ruling) — its §4 name list and collision search are recorded in §19 and
the file is deleted.

**§2 and §3 answered as boards, not files** — screens **1k Calendar panel board** and **1l Graph weight
ruling** in Design's live `PoliSim v2 Screens.dc.html`, read from the project and recorded in
`POLISIM_V2_SCREEN_SPEC.md` §A.16 as the standing rules; both are roadmap live items 8–9, NOT started by
Elias's ruling (§V first). 1k answers the five questions by drawing: the " X" suffix retires for one
diagonal ink stroke through the numeral; the dots-vs-ledger split stands with the ledger row repeating
the grid's dot; the flip stays instant; a saturated day gains a 2px underline beneath the dot row; one
almanac sheet, not three cards; no sprite requested. 1l is a weight-ORDER ruling — the finding was never
"the lines are thin" but that history, projection and threshold all landed within a device pixel of each
other at 2560: R-G1 history 3 buffer px · R-G2 projection 2 px, 3-on/2-off dashes · R-G3 threshold 1 px
amber · R-G4 sparklines `max(2, round(h/34))` · R-G5 the buffer may stand; release markers weight + 2;
the deltas, the badge and the 1px revision frame untouched.

### The screen spec's finished sections, and what the build did with each (added 2026-08-27)

*`POLISIM_V2_SCREEN_SPEC.md` stays at the repo root as the visual reference the code cites by section
(`LedgerRow.cs` §A.9, `GameController.cs` §A.9/§A.11/§A.12) and as the spec of the one unbuilt screen
(1h). Its pass-3 dispositions (§C) and "what is actually buildable" list (§E) were history at HEAD and
were removed 2026-08-27; this is their record, verified section by section against commits and callers.*

**Built as specified, called at HEAD:** §A.1 the surface ladder and the two baked sprites (`4431216`,
2026-08-03 — grain drawn first in `OnGUI`, the scrim at the takeover); §A.3a the party inks (all four
hexes exact, one accessor, both consumers — arc and legend swatch); §A.3b the categorical cap — a THROW
past index 7, not a wrap (`a7bd80d`'s successor `a7bd40d`, `UiPalette.MaxCategoricalSeries`; tax and
spending route to the ranked bar ledger; `SpendingCategory` is 46 members at HEAD, not the 29 pass 3
counted); §A.4 typography (three font constants, every Courier consumer a genuine document artifact);
§A.7 the tab strip (`9497673`, the one §A section with Elias's own sighting, 2026-08-26); §A.8 the
content panel and sub-tab inks (the active sub-tab's 3px area strip RULED OUT by Elias 2026-08-12 —
the main-tab spine carries area identity one level up); **§A.8a published vs live, corrected** —
published-ness is keyed by `PublishedFigure`'s badge chip + reference period + publication date, revision
status by the frame style `GraphRenderer` draws (dashed while provisional, solid once revised); two
orthogonal channels, so a preliminary published figure is badged, dated and dashed; §A.9 the ledger row
(`9705205`; the measures restated in `LedgerRow.cs` as measurements, the row height DERIVED from the font
metric — pass 3's 36px was the value at 1080p, the ninth instance of the fixed-height class and the first
caught in a specification rather than a capture; VOTES deleted, 250px paid for by it); §A.9b the
read-only row and the negative-fill = no-gauge sign rule (one idiom, two methods); §A.9c Parliament's
trailing column = the seat PERCENTAGE with the count as the figure, the party hue reaching `barInk`;
the scroll-view treatment applied globally on `GUI.skin` (the per-view count is beside the point); the
in-row slider (B1's primary carrier, the pencil as `icon_pencil_draft` geometry, never a glyph); §A.9a
the resort ladder in both variants (the 11px floor a measured argument); §A.12's EMBEDDED column
(`drawOwnFrame:false` at both Decisions sites) — the STANDALONE column is superseded by the Canvas-path
ruling (the framed IMGUI modal dies with the rebuild); §A.13's IMGUI half of the hand-off envelope (the
scrim, opacity-only, on four paths; the 100%-cover deviation and the eight seam defect classes recorded
in CLAUDE.md); §A.14's 1f (`14cbad6`) and 1g (`5f64554`) built and seen; §D.2 transitions from the
IMGUI side — the one §D clause that survived contact with the build unchanged.

**The pass-3 findings (§C), all closed:** C.1 division records — backing data (`a7bd40d`) AND a reader
(`ab1b72f`), the absence-claim guard's origin story; C.2 the ladder's numeric variant, absorbed into
§A.9a; C.3 the 36px/type-rescale collision, implemented as the derived row height; C.4
`GetCategoricalColor` fails past eight, implemented as the throw; C.5 `emblem_state_seal` → `ui_seal_state`
(the prefix is load-bearing); C.6 the `canvas_*` namespace retired and the strips split per state; C.7
SVG-only delivery restored to PNGs — **and the coverage check gained a manifest** (`ChromeManifest.txt`),
because a check that enumerates the disk can never answer "is everything specified present?" — the
delivered-vs-reachable lesson from the opposite direction.

**Declared deviations found by the 2026-08-27 sweep, recorded rather than silently kept:** `textOnDesk`
ships `#F0E7D8` (spec `#E8DDC4`); the column layout ships a FRACTION (`LeftColumnWidthFraction = 0.45`),
not the spec's held-constant 430px; the left column's composition moved by decision (the Calendar Panel
replaced the country header + tile grid, `a13dd7b`, the tiles relocated); §D.1's "both sides draw the
hold banner" rule was never implemented — takeovers stop the clock by construction, so no banner is owed
while a Canvas screen is up. **Still unbuilt and live on the roadmap:** the RUNNING status-line plate,
the speed buttons' held-state face (`ui_btn_disabled`), the right-aligned screen caption, the tab-swatch
tints, three ink tokens without a constant, §A.11's urgency-chip border/rotation and generic stamp,
two envelope timing rows.

---

## 25. The macro-overhaul directive — consumed and deleted (2026-08-26)

*`POLISIM_MACRO_OVERHAUL_DIRECTIVE.md` emptied of live content and went, per the register's own
emptied-document rule (the `VISUAL_REVIEW_BACKLOG` precedent): Step A done (A1–A3 2026-08-01, A4
2026-08-02 — §§6/9); Step B done (B1/B2 built and review-confirmed 2026-08-02 — §§6/9/16); Step C
**shipped in full as Round 4's five batches, 2026-08-16→17** (§19 — the directive's sequencing
summary still called C1/C2/C3/C5 merely "buildable… the only outstanding step", stale twice over
at deletion); Step D delivered, imported and wired (§6). The two standing rules it carried live
on where they are enforced: the published/live leak risk — the one-directional rule
(`PublicationSystem` writes `Country.Published`, reads `Country.State`, never the reverse — §6,
restated in the roadmap's Step-A record) — and the split-to-attribute batching principle (rule
0's scale-validation discipline; §19's five-batch bar). The StatTile "9,3" precedent and the
B2-reads-LIVE correction are recorded in §§6/11 and CLAUDE.md. Git history holds the directive in
full.*

---

## 26. Save/load (Master Sequence item 8) — the design record, migrated from the roadmap (2026-08-26)

*Item 8 ran scoped → ruled → built → gate-green → live-verified (2026-08-01 → 2026-08-26). The
roadmap keeps a pointer; CLAUDE.md's three entries ("Save/load mechanism report", "Save/load
BUILT and gate-green", "The saves menu") are the build authority. This section preserves the
design record that lived in the roadmap's ~140-line item-8 block.*

**The scoping finding (2026-08-01): `JsonUtility` fails this state model on four independent
counts, each verified against real code** — `Dictionary` unsupported (`SimulationManager` alone
holds 10+, several NESTED — the 11 standing `UAC1009` warnings); `DateTime` unsupported (and
`CurrentDate` has a private setter, failing twice over); `readonly` collection fields not
serialized (4, including `StatHistory`'s series); nullables unsupported (`DateTime?` throughout
`StatHistory`).

**Decision 1 — serializer: Newtonsoft JSON** (`com.unity.nuget.newtonsoft-json`), Elias's ruling
over a hand-written DTO layer (which meant mirroring a large share of the 33 data types and
re-mirroring each on every future model change). **Decision 2 — first-pass scope: ALL THREE
LAYERS** (Elias): core sim state; pending bills and interrupts WITH their day counters (omitting
these would make a reload silently cancel anything mid-vote); UI draft values (the original 5c
incident's own loss). **Implementation shape:** explicit `CaptureSaveState`/`RestoreSaveState`
pairs rather than reflection — the persisted surface stays reviewable, so an unwired
pending-bill type is an obvious omission rather than a silent half-persist. **Format version
from the very first write.**

**The two save-blocking gaps found by inspection:** (1) `SimulationRandom` — `System.Random`
exposes no position; re-seeding on load REPLAYS the sequence from turn zero (a correctness
failure easy to mistake for save-scumming). **RULED: the counting shim** — record draws per
stream, fast-forward on load; reversible beats permanent, xorshift revisitable once real load
times are known, every recorded baseline preserved. (2) `PeriodClosingValues` keyed by a
`ValueTuple` — Newtonsoft needs string keys; flattened to `{stat, periodStart, value}` records
on capture, rebuilt on restore. It cannot be dropped from the save: revisions converge on it,
and a save omitting it would reintroduce a bug already fixed once (`ea0a6a4`).

**The independent confirmation:** adding `Country.Published` produced `UAC1001: … skipped by
serialization` — Unity's serializer silently DROPS the published series, the exact failure the
serializer decision avoided, demonstrated by the compiler. The warning deliberately stands
unsilenced (baseline 11 → 12): `[System.Serializable]` would remove the warning without making
the type serializable — a false reassurance.

**The chain to done:** mechanism report 2026-08-16 (state surface re-inventoried — 14 pending
structures, ~30 drafts, five serialization hazards named from real call sites;
`persistentDataPath`, atomic write; version policy RULED A — refuse-load with a plain message,
`SaveVersion` bump on model swaps, no migration machinery pre-release) → core + F5/F9 gate-green
the same day (`SaveLoadRoundTripDiagnostic` 12/12; Json.NET's populate-in-place discard of the
tuple-dict surrogate found and fixed with a load-bearing `ObjectCreationHandling.Replace`) → the
saves menu the same day (79 captures × both sizes; loads resume PAUSED; incompatible saves
listed-not-hidden) → **layer 3 live-verified 2026-08-26** (F5/F9 + the saves menu in Elias's
Editor session). Nothing of item 8 remains open. *(The roadmap's "startable today" table still listed
the saves menu and the F5/F9 checklist as remaining until the 2026-08-27 consolidation removed both
copies of item 8 — CLAUDE.md "Save/load BUILT and gate-green" (core, `c1d2810`), "The saves menu"
(`963ee1c`) and the 2026-08-26 Editor-session record are the build chain.)*

---

## 27. The first Master Sequence and the v2.0 overhaul — the roadmap's closed record, migrated (2026-08-27)

*The third consolidation pass (2026-08-27) moved every finished block out of `POLISIM_MASTER_ROADMAP.md`
under the three-way test; git history holds the original prose in full (the roadmap at `d29406f` is the
last version carrying it). Each item below was re-verified against its commit and its callers by the
pass's sweep before it moved; where the roadmap's own wording had gone stale, the corrected fact is what
migrated, with the correction named.*

### The ruled execution order (2026-08-03) and its discharge

Elias reordered the first sequence on 2026-08-03 — **v2.0 (item 9) first, then item 8, then Continuous
Time Phases 4–5, then Round 4** — on the don't-build-it-twice argument: a total visual redirection
rewrites every draw method, so anything built before it is built twice; the same reasoning that had
put Phases 1–5 ahead of Round 4. **The numbering was deliberately NOT changed** — items 1–8 are cited
throughout CLAUDE.md and the code, and renumbering would have broken every citation; that sentence is
the one part of the block still live, kept in the roadmap's standing-constraints block. The consequence
stated at the time — *the game ships v2.0 on a hybrid simulation, a daily calendar over a turn-shaped
macro core* — held until 2026-08-16, when the sequence ran to completion in its ruled order (v2.0 →
item 8 `c1d2810`/`963ee1c` → Phases 4–5 `37c9003`/`22e2b49`, §28) and Round 4 closed the day after
(`a9fb8b7`, §19). The hybrid description is HISTORY; the honesty it asked for (the published/live
distinction carrying the seam) outlived it as a standing rule.

### The closed first sequence — items 1–9

1–5: Part A (§2), Part C (§3), CT Phase 0 (§4), Part B pilot and full rollout 5a–5f (§§5/10/16). 6:
Round 4, five batches, closed 2026-08-17 (§19; the "still OUT" clause about C4/credit-rating follow-ons
resolved with the fiscal arc, §§22/23). 7: Continuous Time Phases 1–5, closed 2026-08-16 (§28 — the
roadmap's item-7 line was the only entry in the list with no DONE marker, written in the future tense
eleven days after it closed). 8: save/load (§26; the second copy of item 8 in the "startable today"
table still listed the saves menu and the F5/F9 checklist as remaining — both closed, 2026-08-16 and
2026-08-26). 9: the macro-overhaul directive (§25). The sequence's ordering rationale — Part B waits on
CT Phase 0 because Parliament's gating and the daily conversion would otherwise touch the same code for
two reasons at once — is a lasting decision and lives with §11.

### The v2.0 architecture — decided and measured (2026-08-03)

**Hybrid at SCREEN granularity, never element granularity.** Elias chose it after the architectural
survey on one finding: the desk metaphor is not continuous — a data screen is *"looking at a document"*,
not a document sliding on a shared desk. Narrative/consequential screens render in Canvas (transitions,
TextMeshPro, masks); data-dense screens stay IMGUI, restyled (9-slice frames, textures, a real font — no
rewrite). A screen is either Canvas or IMGUI, never both interleaved; **any request that violates this
silently is a request to migrate that screen wholesale to Canvas.** The render order was MEASURED, not
assumed: ScreenSpaceCamera Canvas draws below IMGUI as the survey said, and ScreenSpaceOverlay Canvas
**also draws below IMGUI** — there is no Canvas render mode above OnGUI. Two load-bearing consequences:
a Canvas screen is visible only while `GameController.OnGUI` early-returns (the screen-granularity rule
enforced by the renderer itself), and transitions run from the IMGUI side (an IMGUI scrim can fade over
everything; a Canvas overlay fading in over IMGUI is impossible). Re-measured in a built Windows player
the same day, byte-for-byte the Editor's answer (`outer=RED, band=GREEN, centre=GREEN`), so the
architecture rests on a player measurement, not an Editor one. The eight load-bearing behaviours
("WHAT MUST NOT REGRESS") stay in the roadmap's standing-constraints block. Typography: TeX Gyre Pagella
+ Courier Prime, open-licence, owned by `PoliSimTheme`.

### The IMGUI half — the row family, the placement track, the Canvas track (2026-08-10 → 08-16)

- **The Budget screen first, one row type per capture, never batched** — Tax (standing tick at the
  enacted rate; button + slider), Spending (tick at zero, the slider carries a percentage CHANGE),
  Welfare (tick at generosity), Infrastructure (read-only gauge — condition is an output), SWF (tick at
  the standing value on a range spanning zero; the trailing column carries the normalised share). One
  widget, five semantics, and the shape held for all of them. The tax row took three capture rounds and
  each found a defect code review had passed (a shared-style mutation degrading every screen, columns
  overflowing their panel, a button measured in the wrong style) — the rule *one type per capture* is
  not ceremony and stays in the roadmap's constraints.
- **The row family DONE (derived 2026-08-12 from `LedgerRow` call sites):** Budget all five row types;
  Policy/Laws via `DrawDialRow` (`d3cd281`, `df03e97`, `d4083fe`); Statistics/Domestic
  (`DrawDerivedStatRow`, `397d829`); Politics/Parliament (`HemicycleRenderer` legend, `f877915`).
  Residues named, not converted: International's two and the Fed's concatenated labels — the roadmap
  keeps them as a small live item, counts to be re-measured after pass 4 changed the central-bank tab.
- **The placement track CLOSED 2026-08-12:** the "13 unwired sprites" list was WRONG and was corrected
  by tracing call sites — six of the thirteen were never IMGUI placement work. Wired: `ui_subtab_on/off`
  + `ui_slider_tick` (`cbdde4e`), `ui_grain_tile` (`b4108a3`), `ui_banner_hold` + `ui_calendar_pad`
  (`7933696`), `ui_tab_spine` (`a220849`), `ui_folder_dossier` + `ui_portrait_frame` (`fc16304`),
  `ui_stamp_carried/rejected` (`ab1b72f`, the Division Records panel); `ui_chip_outline` was never
  unwired. Canvas-path by ruling: the seals, the scrim, `ui_frame_ornate`, `ui_portrait_frame_oval`.
  **One defect found en route and now rule 15's origin:** `cbdde4e`'s sub-tab face left cream text on
  pale paper — unreadable in every capture, approved by eye — fixed `4192042` by putting the previous
  set beside the new one. **Phase 2's derived statement of every chrome sprite's disposition** (61 on
  disk, 29 wired + 11 Canvas-path + 2 no-state + 8 orphaned + 11 superseded; the 11 removed `10f713e`)
  was the dated 2026-08-12 derivation and is superseded by §33's 2026-08-27 re-derivation — its own
  caveat ("a fresh call-site trace, not this note, is what re-derives them") is the rule.
- **Elias's eight-orphan rulings (2026-08-12):** the folder faces placed as their own pass (B, built
  `9497673` 2026-08-16 — `BuildFolderTabStyle` and the deferred active-tongue paint, tongue-edge
  constants measured from the PNGs' alpha, ink-on-paper labels both ways; **VERIFIED in Elias's live
  Editor session 2026-08-26**: hover face, spine shift, the real click on the deferred-painted tab);
  `ui_frame_ornate` Canvas-path; `ui_frame_double`, `ui_stamp_draft`, `ui_btn_disabled`, `ui_pixel`
  served-by-current-treatment, revivable by ruling. The sub-tab keeps its recorded no-area-tint
  decision (the main-tab spine carries area identity one level up; §A.8's 3px strip would be redundant).
- **Item 1a and the absence-claim lesson:** the 2026-08-12 stamps ruling declared "no resolved-bill
  record exists" two days after `a7bd40d` (2026-08-10) had built `DivisionRecord`/`DivisionLog` as the
  screen spec's §C.1 resolution. The UI half was the missing piece; `ab1b72f` built the Division
  Records panel (six real divisions per set, both sizes, USA and Germany). **A ruling built on a premise
  that was already false** is the canonical absence-claim instance, beside the A4 caller-check lesson.
  What election night still needs is a different record entirely — an `ElectionRecord` — which rides
  item 10 (`MISSING_PREREQUISITES.md` §D).
- **The Canvas track CLOSED 2026-08-12:** the pilot `14cbad6`+`257ed39` (the takeover seam with eight
  named defect classes — one found by the pilot's own first run — `CanvasChrome`, the country selector
  per §A.14, `ui_scrim_takeover`'s call site at last; recorded decisions: scaler 1920×1080 match 0.5,
  the border-order convention, the four-phase seam), then **Signing (1g)** `5f64554`+`38363c6` (takeovers
  stop the clock; CoverIn overlays the live dashboard; ceremonies fire only from play's day tick) and the
  Canvas text guard `6adb7c6` (`CanvasTextGuard`, self-testing both directions, fails at zero enumerated;
  `CanvasChrome.TintedImage`/`AsAuthoredImage`, the tint family forced at construction). §A.14 defines
  THREE Canvas screens, not eight (the eight are the boards): 1f and 1g built; **ELECTION NIGHT (1h) is
  item-10-gated** (ruling R2). Both built screens have been in front of Elias: 1f is unavoidable in every
  live session, and 1g's seal/button branch defect was playtest 1's own finding
  (`SigningStampFixDiagnostic`).
- **Track 3 `10f713e` (2026-08-12):** the eleven superseded pre-v2.0 chrome PNGs removed with their
  metas, `DeliveredAssetCheck` taught the manifest's `!` allowance in the same commit (0 missing / the
  superseded skips logged), ChromeV2 50/50 both directions. ⚠ The eleven SVG sources under
  `Chrome/Source/` were NOT removed (the commit touched no `.svg`) — the roadmap carries the one-line
  correction as live work.
- **The WIN-form election reveal pinned `5eb5dc7`** (`winusa1600_88w`; both reveal forms and game over
  on film in one chain; the FP-meeting search variant found ALREADY BUILT and stated, not re-built).
- **The country-coverage pass (2026-08-12):** 12 runs (6 countries × 2 sizes), 676 captures, every
  automated check clean, then the different-code screens read by eye per country. Its three findings
  were all FIXED the same day, though the roadmap carried "fix not yet applied" on all three to the end:
  the ECB sub-tab label garbling when selected (instance #13, `SubTabRowHeight`, `d072286`), the empty
  Mandatory group leaking `GroupSpendingMax`'s guard as a figure (same commit), and
  `DrawElectionResultsScreen` printing paper-ink on the bare desk. The pass's stale premise corrected:
  all six countries HAVE spending portfolios (`SeedGenericSpendingLines`, 5 lines each); the legacy
  category-delta UI is unreachable for every seeded country. The reachable state axes were pinned the
  same day (`-shotstates`), and two of three "unpinned" claims fell within hours — the FP dossier pinned
  itself through real rolls, and `DrawElectionResultsScreen` existed all along (the driver's turn path
  never ran `CheckElection`): the driver's day tick became `SimulationManager.AdvanceCountryDayTick`,
  one method both callers share.

### The three reconciliation rulings (Elias, 2026-08-12) — lasting

**R1** Step C folds into Round 4's slot (spent; §19). **R2** the `ElectionRecord` waits for item 10's
model, and the Canvas ELECTION NIGHT screen is item-10-gated (the other two Canvas screens were not,
and are built). **R3** the item-10 collision map stands — **no main-side changes until item 10 opens**;
`PartyMarkCoverageCheck`'s reflection over `BuildParties()` should survive the model swap, **to be
VERIFIED when item 10 opens, not trusted now** (carried in `MISSING_PREREQUISITES.md` §D).

### Two lessons the roadmap carried that belong here

- **A next-steps marker is a claim like any other and goes stale the same way** — and worse, because it
  is the first thing read each session. The "NEXT SESSION STARTS HERE" marker of 2026-08-11 had been
  wrong for a day (it offered Policy/Laws as a conversion candidate after `d3cd281`/`df03e97`/`d4083fe`
  had converted it) and was 16 days stale at deletion. There is exactly one session-entry point in the
  roadmap now, and it is the board, re-derived rather than edited forward.
- **The `EndTurn`-as-absolute-turn-number capture artifact is known and driver-only:** `94c` (Inherit the
  Fund) and Italy's own instance show "SCENARIO FAILED" reached at their mid-run start because
  `ScenarioEvaluator` compares `EndTurn` absolutely while the capture driver runs several blocks on one
  continuous clock. Neither a scenario-balance nor an evaluator defect; recorded so it is not
  re-discovered as new.

### The resolved v2.0 design questions

The eleven-hue question — all eleven survive, aged and desaturated, no non-colour carrier (emblems drawn
*instead of* the hemicycle legend's swatch broke the legend's correspondence with its own arcs: **a mark
cannot substitute where the mark is not what the chart is drawn in**; inks live in `UiPalette`). The
font test — answered, and it surfaced that draft amber and the Political hue shared a hex; separated. The
`drawOwnFrame` dual-siting question — absorbed: the IMGUI modals' framed treatment dies with the Canvas
rebuild (`ui_frame_ornate` Canvas-path, 2026-08-12), and the separate-sprites answer was delivered in
pass 2 (§24).

---

## 28. Continuous Time Migration — Phases 1–5 (Master Sequence item 7), migrated (2026-08-27)

*Item 7 CLOSED 2026-08-16 (`22e2b49`). CLAUDE.md's five phase entries are the detailed authority; this
section preserves the plan's lasting decisions — the translation methodology, the validation bar and
the two lessons — and the per-phase closure line. The roadmap's PART ONE, written when a turn was 121
days, is deleted.*

**The translation methodology — do not guess new constants (the taxonomy's final state, completed by
Phase 5).** Identify which mathematical shape a constant is before touching it; denominators are
`SimulationManager.DaysPerTurn` (365 since `d8f55ce`, 2026-08-10 — discontinuity 3), never a typed
number, which is the discipline that made 121→365 a one-line edit:

1. **Linear/additive rates** — `rate_per_day = rate_per_turn / DaysPerTurn`; accumulating terms with no
   target take THIS transform, not the multiplicative one (`ApplyCrimeEffects`).
2. **Multiplicative/compounding rates** — `(1 + r)^(1/DaysPerTurn) − 1`; `MacroSystem.PerDayReversion`
   is the standing implementation for anything shaped `X += speed × (target − X)`.
3. **Probabilities** — `1 − (1 − p)^(1/DaysPerTurn)`.
4. **Hard clamps and ceilings do NOT shrink** — a ceiling bounds the state, not a per-step increment;
   only the speed of approach changes.
5. **Sensitivities and target-shapers take NO transform** — a constant mapping a level to an offset has
   no time dimension; scaling it changes what a policy position *means*.
6. **Annual rates applied to stocks take the POWER slice** `(1+x)^(1/DaysPerTurn)` (Phase 4's
   population factor); **the identity's attractor takes the AFFINE power slice** (Phase 5).
7. **A constant that is a POLICY STANCE stays frozen for its period** (Phase 3's fiscal reaction
   multiplier); **expectations that adapt to a closing print stay AT the boundary** — boundary semantics
   have no faithful daily form (Phase 5). The governing distinction the migration ended on: **linear
   distribution for fixed references, compounding for self-references, boundary residence for boundary
   semantics.** The stance-vs-flow question, asked constant by constant, was the method.

**The validation bar:** simulate `DaysPerTurn` consecutive days and confirm the result within **3%** of
the validated turn-level step for the same inputs — `AggregationEquivalenceCheck` (`TolerancePercent =
3f`; the roadmap's "±3–5%" was looser than the check) FIRST, the full scenario matrix SECOND, one commit
per phase, ambiguous shapes escalated rather than guessed.

**The phases:** 1 Sectors + Infrastructure `321a10e` 2026-08-02 (28/28, max drift 0.0004%; the two
constants took DIFFERENT shapes — a gap-closing fraction multiplicative, a decay rate linear — and the
sensitivities none, which is the distinction the rest turned on; investment stayed a boundary action).
2 Labor + Crime & Justice `275e014` 2026-08-02 (34/34, 0.036%; `PerDayReversion` born and shared). 3 the
fiscal engine — part 1 `42a499f` moved `PovertyRate`, **part 2 `fc657b1` 2026-08-03** moved the money
(39/39, 1.35%; 25 of 30 matrix cells byte-identical). **The constant that failed its first shape:**
recomputing `GetFiscalReactionMultiplier` daily failed outright (Sweden 24.8% drift, Germany 22.7%) —
not a bug: one period moves a debt ratio ten points, so a multiplier re-reading it daily walks down its
own surplus; frozen per period it passes at 0.45%/1.35% **and is the better model, because a stance is
adopted when the budget is set.** The player-visible consequence: a policy change's CASH effect lands
one period after the boundary that made it; the budget RESOLUTION stays on the boundary because a
budget passing is an event on a date. 4 Demographics `37c9003` 2026-08-16 (the throwaway
`Phase4YearsPerTurnDiagnostic` ran FIRST and earned its gate 9/9; 61/61 first try; `History.Append`
had sat on the turn boundary since Phase 0 — moved to `AdvanceDay` with the bucket-divergence assert).
5 the core macro engine `22e2b49` 2026-08-16 (`Phase5NoFeedbackDiagnostic` 4/4 gated first; four
first-try shapes failed the bar, every failure measured and kept, all four resolving into Phase 3's
fixed-period-reference pattern; 81/81 near-exact; the four-country debt signature reproduced within ~1
point; no constant VALUE changed). **The hybrid simulation is over: every economic quantity moves on the
day its history point records; the boundary remains what a boundary is for.**

**The release-cadence ruling that superseded the plan's per-stat list:** six stats publish, not
twenty-nine — only those with a real release rule in the seed data (`POLISIM_SEED_DATA_MACRO_OVERHAUL.md`
Part 1, cited by `ReleaseCalendar.cs`); inventing a cadence for a stat no institution publishes on a
schedule was refused (§6). The real-reporting-lag refinement closed as won't-do 2026-08-11 (§32).

---

## 29. Part B 5e — batches 4–6, the retrospective and the superseded plans, migrated (2026-08-27)

*§10 holds Phases A/B and batches 1–3; §16 holds the live confirmation of all eleven review items
(2026-08-02). The roadmap still carried batches 4–6 as "BUILT — Not yet live-confirmed" 25 days after
Elias confirmed them (§16 items 4, 5 and 9 map to batches 6, 4 and 5 — verified by recovering the deleted
`VISUAL_REVIEW_BACKLOG.md` from history), and its Part B header still read "Only 5e is live".*

**Batch 4 — Policy/Laws (`a1bec98`, 2026-08-01):** four byte-identical live-estimate renderers
collapsed into one `DrawBillLiveEstimate(float direction)` with the diverging lean bar; every bill block
in an area card; `BeginDecisionCard`/`EndDecisionCard` → `BeginAreaCard`/`EndAreaCard`. **The collapse
mattered for correctness:** the `Mathf.Sign(0f)` zero-direction trap has to be handled identically in
every copy, and four copies is four chances — that exact bug had already shipped once. **Batch 5 —
Budget Process (`cdd5a1c`):** `DrawLegislativeSupportEstimate` was a FIFTH copy on the most important
screen in the game; now shares the one renderer, equivalent by construction (`WouldBillPass`'s
`BudgetBill` overload computes the direction and delegates to the float core). **Batch 6 — the amber
draft cue (`78280c8`):** `DrawDraftLabel` at every draft site in one pass (the "25 call sites" was a
2026-08-01 snapshot: 28 lines at the commit, and the Budget conversion of 2026-08-10 moved most of them
into `LedgerRow`'s draft column — one `DrawDraftLabel` site remains, the SWF case).

**The three models of "changed", restated against `LedgerRow` (lasting):** standing/draft pairs compare
the two values; **spending drafts are a percentage CHANGE**, so non-zero is the condition; **the SWF's
own existence is a draft**, compared against whether a fund stands. Two cases stay neutral by design: an
**unimplemented** tax line or welfare program is changed by its own Implement/Remove bill, never by the
slider beside it, so its draft ink must not light up; and the standing labels are precisely what has not
changed. Anything added later gets the same case-by-case treatment.

**The retrospective, worth more than the batches (lasting):** the design pack's components fell into two
groups. `StatTile`, the card/rounded-box primitives and the icon tinting were built against assumptions
this project holds and are in production. `SupportBar` (a seats-based majority the model does not
implement) and `StandingDraftPair`/`DraftTrack` (hardcoded offsets ignoring `rect.width` on the most
fragile screen in the project) encoded mechanics and layout this project does not have and were
rejected only after being checked against the real model — in both cases the roadmap had already
recommended them before that check. `Portrait` was superseded by real art. **Treat a plausible-sounding
component as a proposal to verify against the actual code, never as a fit already established.** The
three rejected widgets sat in `PoliSimWidgets.cs` as dead code with zero callers for 26 days and were
**deleted 2026-08-27 by Elias's ruling** (git history holds them at `483f03e`; the three `GameController`
comments that cited them as the road not taken now say so in the past tense).

**5f** (the aesthetic restyling pass) was folded into 5e on 2026-07-31, and the original step-5 plan
(pilot on Tax Policy, then roll out to the remaining seven with the identical pattern) was superseded
the same day — both shipped inside 5e (§§5/10/16). **The "PoliSim GUI redesign.zip" pack** (Zone.Identifier
`HostUrl=https://claude.ai/`, a Claude Design handoff) was security-reviewed in full before use — both
C# files read line by line and grepped clean for network/IO/reflection/process APIs, all eight SVGs pure
static geometry, all nine PNGs genuine image data with no embedded scripts or URLs; the theme/widget code
imported `b69b0d6` (2026-07-31), the icons under reconciled names, `menu_pattern_tile.png` last
(2026-08-02, §15), the zip archived. The "open tie-in" about SWF in the omnibus bill was unactionable
when written (SWF changes had ridden `BudgetBill` since 5c) and the question it pointed at was ruled AND
built the same day (§23 A2, `b1c077f`).

---

## 30. Master Sequence II — Steps 1, 2, 3 and 5, migrated (2026-08-27)

*Canonical per Elias's enumeration 2026-08-17; Steps 4 (item 10) and 6 (story mode) remain and are
carried in `MISSING_PREREQUISITES.md` §D. Every gate and date below was verified at HEAD during the
2026-08-17 transcription and again by the 2026-08-27 sweep. Visual deliverables in this section that
no record shows Elias seeing are listed in `MISSING_PREREQUISITES.md` §V, not recorded as confirmed here.*

**Step 1 — the coupling graduations, CLOSED 2026-08-18: three variants of the template demonstrated.**
Q3 productivity → potential (`d1cb1de`, the RE-ROOTING kind: byte-identical 39/39, 6/6, zero moved;
trajectory movement deferred to Q5 by design). Q1 Gini → Approval (`ed07333`, the FORCE/containment-clean
kind: gap form −0.05 × (Gini − BaselineGini), 1.0 equilibrium pt per Gini pt, no new ceiling; the
brief's "new Country field snapshotted" was already false at ruling time — `BaselineGini` had existed
since R4-2, so Q1 added no field: **verify against HEAD, not the brief**). Q2 real-wage →
ConsumerConfidence (`ef7cbf2`, the FORCE/baseline-active kind: form A with the single-book rider,
0.5%C/pp on the anchored wage gap, the shared realized-growth helper; **the FIFTH fixed reference
`WageGrowthGapAtPeriodOpen`**, found by the equivalence bar and fixed by the anchor pattern). **Lasting —
the single-book rule:** no stored quantity may diverge from its presented value; the effective confidence
is the only confidence read or displayed, the stored field is the policy-drift base and named as such;
`BusinessConfidence` inherits by default if it ever gains an effective form.

**Step 2 — causality legibility.** Scoped `5084236` and **v1 shipped `092202c` the same day
(2026-08-18)**: the approval ledger (terms recorded at the boundary under the Σ==Δ self-audit; events by
observation at eleven sites), the trace panel on the LedgerRow grammar (click a chip; equilibrium
framing; dated events; the confidence single book as the second section), the preview-parity diagnostic
as standing equipment (7 exact-asserted terms × 6 countries), the ledger in the save shape with explicit
RT assertions; the observation gate's three catches (the one-ulp codegen story, the first-boundary open,
the detector's own false positive). **The third section shipped `7d2a22c` 2026-08-25** — the fiscal
legibility panel Italy Debt Crisis's playtest asked for: a debt ledger (`DebtAttribution`/
`DebtLedgerRecorder`, the approval ledger's shape) observing the daily stock write, so the debt step
decomposes EXACTLY (primary balance · the FRF's revenue effect at the frozen stance · interest at
issuance · the maturity lag · −π·b erosion · clamp/rounding · dated events), the ratio's identity in two
exact terms, the self-audit at every boundary; **600/600 byte-identical, 0 ATTRIB across 600 audits**,
RT 12/12 with the ledger crossing, parity 7/7. Three pre-existing defects the bar surfaced, all fixed:
every law vote had written approval outside the ledger since 08-24 (24 → 0 ATTRIB on the RT harness);
the trace panel never measured against its host's height; the driver's Italy block had drifted past
`EndTurn`. The scoping package's derivation is §21.

**Step 3 — challenge-mode scoping, the slice shipped `cd52461` 2026-08-18** (scoped `deff6dd`, the slate
of six): `ScenarioDefinition` (the deliverable), the four objective forms, `ScenarioEvaluator` on the
`CheckElection` hook, the IMGUI verdict with margins and a legibility-powered epilogue, ledger-style
persistence (id + counters), the per-scenario FA cadence multiplier defaulting to 1.0. **"Inherit the
Fund" closed R3's creditor-branch coverage gap BY EXERCISE** — both arms of the symmetric erosion term in
one run (+6.2 on a negative stock at t1, −1.4 on a positive one at t12), and the measurement corrected
the scenario's own premise (the structural deficit dominates erosion ~7:1). **The ruled headline:**
authored scenario starts with win/lose conditions, NOT an election clock. **The slate's dispositions:**
Inherit the Fund SHIPPED · Italy Debt Crisis SHIPPED (`6d5b000`, below) · The Disinflation DROPPED
(measured, `8460a59`) · Wage Boom Management DROPPED (measured, `b12ccd0`) · Poland convergence and The
Unequal Recovery LIVE (the roadmap's content backlog, ruled keep 2026-08-26). The `Sustained` form was
exercised twice — on a synthetic diagnostic and for real in Italy, where it found and fixed a genuine
non-stickiness defect in `ScenarioEvaluator`. R-S3e's three-rate FA-cadence sweep was SUPERSEDED by
ruling 2026-08-26 (C5): the built per-scenario multiplier is the lever; the felt-pacing question rides
the playtest gate (`MISSING_PREREQUISITES.md` §P).

**Step 5 — Q5, the cyclical pair, DONE `7321807` 2026-08-18:** R-Q5a = B1 (additive cyclical force
through wages), R-Q5b two channels stated separately in code though numerically indistinguishable at
h = 0.4, R-Q5c h = 0.4 pp/pp on the unemployment gap, **R-Q5d = the answer to Q4's revived residual:
potential reads trend alone; the Productivity stat and real wages read trend + cycle**, R-Q5e investment
deepening DEFERRED (no capital stock exists anywhere in the model; I/GDP measured flat at 19.5–20.9% —
nothing cyclical to deepen from either way; the return trigger is on the roadmap's shelf). **The model's
first closed feedback loop, its gain MEASURED rather than trusted:** derivation predicted 0.075×h ≈
0.03; measured 0.0297–0.0300 across three structurally different economies — within 1%, stable by ~20×
against Okun's 0.7/turn reversion, h would need ~13 to threaten it. **The correction that followed within
hours:** the "Wage Boom Management is now authorable" pointer died the same day — `UnemploymentReversionSpeed`
(0.7/turn, unrelated to Q5) forecloses sustained tightness below NAIRU regardless of the loop, and The
Disinflation fell to the same constant from the opposite direction: **two drops on one root cause is a
named model-balance finding** (§22 holds both reports). Corrected, not silently amended.

**Italy Debt Crisis SHIPPED `6d5b000` 2026-08-18 — the third content pass, first of three to survive:**
seven same-seed configurations spread 52.63%–109.60% debt-to-GDP by t30 on the player's instrument
choice, spending cuts compounding (−0.16 → −1.90 pp/pp) while VAT hikes plateau; no debt term in the
misery index, so the approval-survival question that killed Disinflation is cleared with margin.
Authored as `ItalyDebtCrisis()`: Terminal + the `Sustained` form's first real exercise + NeverBreach;
the generic verdict-margin line reports a Sustained streak. **Format verdict: subset, confirmed — no new
`ObjectiveKind` needed.** Two of SIX scenarios remain.

**Option C for Riksbank independence — NAMED (playtest-2 item 5, ruled 2026-08-25):** the player-set
rate as a deliberate gameplay choice, stated in the Federal Reserve tab's text and `PolicyDecision`'s
doc (the Italy-scenario precedent: the premise as authored text, not an apology), keeping Sweden/Poland's
full monetary agency. B — independence with appointment influence — is the destination, behind item 10
(`MISSING_PREREQUISITES.md` §D), and gate 1 (the output-gap distortion) CLEARED with pass 4 (§31). **The
felt verdict from Elias's 2026-08-26 Editor session is recorded there:** "still not independent" — C's
naming does not satisfy in play.

**STEP 5 CLOSES; the spine's remainder is Step 4 (13 Sept 2026) and Step 6 behind it.** The spine was
re-checked 2026-08-25 (the fortnight's work — playtest 1, Turn → Year, the Calendar Panel, the law
system to 50/50, the two-copy consolidation — touched none of the four things Step 4's package names,
and laws entering Parliament reused the gated-bill path verbatim); six passes shipped after that check
(§31), none touching Step 4's four named items either. The check is re-derived at the gate, not narrated
forward.

---

## 31. The 2026-08-24 → 08-27 board — the law system, two playtests, the ruled build order, pass 6 (2026-08-27)

*The roadmap's "Board state, RE-DERIVED 2026-08-25" block and "The ruled build order" section, migrated.
CLAUDE.md holds every ship record; §20 holds playtest 1's package and the law system's first arc.*

### The law system — 100 of 100, two categories (2026-08-24 → 08-26)

MVP slice `ca11f9a` (a law is a NAMED PRESET over the existing dial space, reaching Parliament through
the same gated-bill path every other bill uses) → batches 1–3 (`de34b4b`/`c9e9e16`/`785da64`) →
close-out `555f4cc`, which found and fixed **the composition architecture's one real bug: dials are now
a PURE FUNCTION of `Country.EnactedLaws`** — every enacted law's delta summed from the baseline, clamped
exactly ONCE (`RecomputeCrimeJusticeDialsFromEnactedLaws`), never nudged incrementally, so any
enact/repeal history in any order lands exactly → STOPPED at 38 on the browser's own navigability
condition → Design delivered board 1i + `LAW_BROWSER_BOARD_RULINGS.md` (`315cca0`) → the browser rebuilt
against it, 15 review findings fixed (`dddec9f`) → batches 4–5 to 50 with the saturating composition
re-run (`eb11b78`: 27 of 50 enacted, FOUR dials clamp at once, full repeal nets exactly 50.0000 on all
six) → the detail pane's width (`6804c6d`) → **the second category, Labor Market, 50 laws `e86c79d`
2026-08-26** (below). Byte-identity for the no-law path holds by construction (`LawCatalog.All` is read
only from the UI layer). The residues the roadmap still listed — "the category filter is inert until a
second category exists", "five categories sit at 0" — are false at HEAD: `LawCategory` has exactly two
members, both populated, and the filter genuinely narrows.

### Playtest 1 (2026-08-18) and playtest 2 (2026-08-25, live, Sweden) — dispositions

Playtest 1: the rejected-bill seal and Budget's dead nested scroll fixed 2026-08-18 (CLAUDE.md "First
real playtest session"); Turn → Year 2026-08-24; the Calendar Panel `a13dd7b` 2026-08-24 (replacing the
dashboard tile grid in the left column; **seen — playtest 2's verdict on it is why request §8 exists**);
decision density **CLOSED ON THE NUMBERS, ruled 2026-08-25 (Elias)** — measured at 50 laws (`df4eee0`):
automatic prompts/yr unchanged by construction (≈5; 5.87 at a full six-minister USA cabinet), named
enactable choices 19 → 69, then **119 after the second category**; the 08-18 ruling's own prediction
confirmed — the table did not move, the menu did; whether it READS as closed rides `MISSING_PREREQUISITES.md`
§P. The law system above; portraits D1 in `MISSING_PREREQUISITES.md`.

Playtest 2's seven items: **1** ATTRIB (Sweden 2027-01-01, +1.5000) FIXED `e25ae60` — the first-touch
window class, writer = foreign-policy "Send substantial aid"; the approval recorder now opens at the
pre-write value on every path; reproduced red then proven green by `LedgerFirstTouchDiagnostic` (stays as
coverage), RT 12/12. **2** Surplus display — the hypothesis REFUTED at the formula before anything
changed (the row already showed the net-of-interest real balance); "Primary deficit/surplus … excl.
interest" added as a labelled second line from the same report (`DerivedStats.PrimaryDeficitPercentOfGdp`);
Sweden's outsized surplus was a SEED question, resolved by the recalibration (below). **3** Compass
labels — two iterations (label-vs-label, label-vs-dot, leader lines), captured both sizes. **4** Sweden
budget depth — BUILT (ruled: decomposition now, Sweden first): 24 sourced utgiftsområde lines
(regeringen.se, vårprop 2026, retrieved 08-25), all-discretionary and not-byte-identical deviations stated
with measured reasons; the recalibration shipped as build-order item 1. **5** Riksbank independence — C
named (§30). **6** Law pros/cons — BUILT (ruled: neutral, derived via the declared table)
`CrimeJusticeCouplings`, read by the Apply* formulas themselves; byte-identical 6/6; "Expected effects" in
the detail pane. **7** Law-page clutter at 50 — Design answered with Screen 1j, implemented same day
2026-08-26 (§24). Items 3, 4, 6, 7 and the surplus line are BUILT-BUT-UNCONFIRMED visual surfaces and
sit in `MISSING_PREREQUISITES.md` §V. **The two-copy consolidation (rule 13)** finished 2026-08-25
(`faecdce`, `0c2a747`, `bb6ad14`): G: is the working copy; the C: copy is
`PoliSim-ARCHIVE-DO-NOT-OPEN-2026-08-16` with `ProjectSettings.RETIRED`; the standing habit — every
harness invocation passes the explicit project path — is now in rule 13's text. **The ~23 Aug GitHub GC
gate CLOSED 2026-08-25:** `9221` KB against the 08-16 reading of ~746 MiB; GitHub's own maintenance
collected the unreachable objects; no ticket. **§7 (the law browser request) OVERTAKEN** — never sent;
the board arrived first; consumed to §24.

### The ruled build order — five of five SHIPPED (2026-08-26), and the shelf's first item (2026-08-27)

1. **The fiscal seed recalibration `290d4ee`** — the five EU pairs re-anchored to one-basis real
   figures (Eurostat `gov_10a_taxag` 2024 / ECB / CBO, sourced and dated in the commit), the ~20%-of-GDP
   mandatory transfer block seeded, Sweden's UO10/11/12 flipped mandatory with `PotentialGDP` re-solved
   (614.25), Italy Debt Crisis re-premised to stabilization (≤145 by t30), the SIXTH baseline
   discontinuity. **Two lasting decisions:** mandatory transfers enter as real lines with the identity's
   G untouched (the identity lesson), and a scenario premise is re-derived when its seed moves. The other
   four countries' decomposition passes now decompose CORRECT totals — live, unscheduled.
2. **The Crime & Justice couplings pass `a7e00e3`** — the gap list consumed by four terminal rulings:
   SentencingSeverity → PrisonPopulationRate at S = 1.6 (NRC-2014-anchored); the budget edges
   line-resident AND feeding G (real Justice/HomelandSecurity/Migration/PublicServices lines, the
   incarceration variable cost at 1.0 GDPpc/inmate completing sentencing → prison → budget);
   BorderEnforcement's second sim edge DECLINED with reasons. Wired-inert control 6/6; no-law path 6/6;
   full bar green at four sizes. **The caveat that travels with it:** a pass declared green at four
   sizes still shipped four inverted Policy Web edge signs no guard and no capture could see, because
   `Increases` is a semantic property — fixed by the 2026-08-27 edge sweep (CLAUDE.md "The Policy Web
   edge sweep").
3. **The second LawCategory — Labor Market `e86c79d`** — 50 laws in five charter batches (catalog 100).
   **Lasting — COEXISTENCE by ruling ("keeps sliders"), the deliberate anti-precedent to C&J's read-only
   conversion, shipped as the base+offset two-book split:** bills own `Country.*Base`, laws sum deltas
   on top, one clamp at composition — order invariance, exact repeal-to-bill-base, cross-category
   isolation and the Sweden minimum-wage gate proven (`LaborLawCompositionDiagnostic`). `LaborCouplings`
   the second declared table; per-dial magnitude scales put Kaitz-point and week dials on the shared
   grid. The next category pass must know which convention it inherits — this one.
4. **The Riksbank-B gate-1 fix `513b348` (pass 4)** — the Taylor path reads the unemployment gap
   against NAIRU (`TaylorRule.UnemploymentGapWeight` 1.0, a stated textbook convention) instead of the
   raw level output gap, a per-country CONSTANT (USA −14.5% for a thousand turns) that pinned the USA's
   suggestion at the floor for 95–98 of 101 turns and collapsed five of eight Fed chairs onto one
   trajectory. After: USA suggestion 0.01 → 5.8, floor turns 98 → 0, chair spread 1.50 → 2.78 pp, the
   house-price runaway ended; the SEVENTH discontinuity. **Lasting precedent:** the rulings were taken by
   the pass under rule 4's reversible-call form with the revert point named (`513b348` plus the closing
   commit) — later passes cite it. The honest form of its rejected branch A is the roadmap's G-block
   shelf item.
5. **Tariff-to-stock `ad82104`/`6b93a1c`/`bb5e37e` (pass 5)** — tariff revenue had reached the Budget
   display accumulator alone. **F1's boundary rule as APPLIED: it is a RECURRING flow, so it is the
   budget process's channel** — `FiscalPeriod.PlannedTariffRevenue`, planned at the boundary, accrued
   daily inside the fiscal-reaction multiplier and outside CollectionEfficiency; the parallel book
   retired. Revenue-neutral at seed on pass 1's anchored-primary rule (a closed-form CE decrement per
   country); the perimeter argument recorded UNVERIFIED-EXTERNAL. The moved set EXACTLY {Budget,
   GovernmentDebt, EffectiveDebtRate}. **The trade-war finding** — per-partner overrides at the 50% cap a
   costless 5–11%-of-GDP revenue button — shipped as a recorded exploit class with tariff costs queued.
6. **Tariff costs `4352665`/`4650a76`/`3796c0e` (pass 6, 2026-08-27)** — three forces in one declared
   table `TradeCosts`: the tariff-take change passes through to prices for one year (φ = 1.0 derived from
   static volumes, stakes measured at 0.5, expectations look through the REALIZED clamped part); partners
   mirror an override's excess over the standing rate (computed, never stored; instant, memoryless); the
   Trade bill's direction is the change in the import-weighted average tariff, sign-only, **on the fiscal
   axis by Elias's ruling** (a tariff is a tax on imports; the literal reverts when trade gains its own
   axis). No-policy baselines byte-identical (42/42, 6/6); the eighth discontinuity is policy-path. The
   simulation half is DONE; its four UI surfaces (the bill card's cost line, the partner row's
   retaliation, the stats line, the inert base dial) await eyes (§V). Deferred with reasons on the
   roadmap's shelf: volumes indexed to GDP, base-dial retaliation, retaliation memory, a trade axis.

### The Editor checklist closures (Elias's live session, 2026-08-26)

Folder tongues VERIFIED (§27); **save/load layer 3 + F5/F9 + the saves menu VERIFIED (§26's closure:
nothing of item 8 remains open)**; the §5 portrait register side-by-side PASSED — D1's batch of nine
unblocked (`MISSING_PREREQUISITES.md` §D1). The Access row's remaining half — the capture-set reviews —
moved to `MISSING_PREREQUISITES.md` §V. The FA-cadence row closed by ruling C5 (§30); the creditor
scenario row closed by exercise (§30); Q4 resolved by R-Q5d; A3's re-listing struck (a two-authors
artifact — §23).

---

## 32. Open Questions and the visual-review section — the closed entries, migrated (2026-08-27)

*The roadmap's Open Questions section held no open question at HEAD: every entry was ruled, closed or
migrated, and the "RESOLVED" sub-block was a verbatim duplicate of §23. What follows is what those
entries still needed a home for.*

- **Capture at 1440p as well — RULED 2026-08-11, done the same day, clean** (55 captured, 0 failed, 0
  overflows, 0 escapes, 0 clipped at 2560×1419). Elias's reasoning: a second resolution is a
  capture-config change, not a code change, and it converts three resolution-scoped claims (the 0.35
  squeeze floor "never engages", instance #12's closure, `ScreenEdgeCheck` itself) into real ones. It
  **retired `ledger_geometry_check.py` without porting it** (a render answers what it re-derived
  arithmetically; Python is not installed here). The matrix has run FOUR sizes since 2026-08-26
  (1280×720 / 1640×707 / 1600×950 / 2560×1440, the minimum-window ruling, `4e94eb7`); the 1920×1080
  gap stays live on the roadmap. The squeeze floor is confirmed by render at all four; a geometry
  conclusion drawn from captures is scoped to the capture resolution — rule 14's shape.
- **Real reporting lag — CLOSED AS WON'T-DO 2026-08-11, reopenable.** An optional refinement to
  deferred work that nobody was waiting on: *a question nobody is waiting on should not sit in a list
  people read looking for work.* (Lasting: the reason Open Questions became a record of decisions.)
- **The net-creditor bound, the unbounded-divergence block, the deficit-term defect and C4's rating
  thrash** — all closed and already recorded (§§13/18/22/23). **Two scoping rulings from the divergence
  block STILL BIND** and now live in the roadmap's standing-constraints block: calibration stays at
  turns 100–200 (t1000 is a diagnostic, never a target), and the word "equilibrium" stays banned without
  a run that earns it.
- **SWF emergency drawdown (A2)** ruled and built the same day, `b1c077f` 2026-08-02 (§23 A2) — the
  roadmap asserted both "Still to build" and "DONE" for it in one file. Its SWF-tab surface has been
  pinned on film only (§V).
- **P2 — the currency unit bug (review item 3):** built `628d78e` and SEEN (§16 item 3); the entry
  never closed with it. `UiFormat.Money`'s required unit and `MoneyFormatDiagnostic` (6/6) stand.
- **Instance #12 — the frame — CLOSED on `main` 2026-08-11, the four-commit measurement table** (§17
  holds the narrative; the table lived only in the roadmap):

  | | commit | L | T | R | B | clipped |
  |---|---|---|---|---|---|---|
  | before | — | 0 | 0 | **841** | **663** | 54 |
  | `InnerWidth` 4th term + tab margins | `f3cbea4` | 0 | 0 | 0 | **663** | 54 |
  | two accessors | `b16b816` | 0 | 0 | 0 | 0 / **1508** | 16 (all Budget) |
  | `BudgetProcessHeaderHeight` | `4dbb779` | 0 | 0 | 0 | 0 | **0** |

  **It read as fixed for hours while `main` was broken:** the `clipfix2_*` captures measured a tree
  holding a closed session's uncommitted `InnerHeight`, which then went to `stranded/politics-elections`
  — *a capture is evidence about the tree it was taken from, not about the branch of the same name*
  (rule 15's root). What closed it: three accessors, each replacing a constant that stood in for
  measured content. **Instance #13** (the ECB sub-tab, the first reached through the COUNTRY axis, both
  guards structurally blind again) fixed by `SubTabRowHeight` (`d072286`). **The guard scopes, stated:**
  `UiOverflowGuard` asks whether text fits the rect it was handed; `UiContainmentGuard` whether a child
  rect sits inside its container (three composite widgets); `ScreenEdgeCheck` reads four lines of pixels
  per PNG, right and bottom only, flushness not magnitude, at the resolutions actually captured. **DO NOT
  BUILD A THIRD SITE-SPECIFIC GUARD** (now a standing constraint on the roadmap): a GUILayout-aware check
  would need every `BeginArea`/`BeginScrollView` rect on the stack and every group's requested min/max
  from `GUILayoutUtility.current.topLevel` at the moment layout resolves — unreachable without reflection
  into IMGUI internals; the pixel check is cheaper, exists, works and asks the question the player
  experiences. The label-clipping CLASS stays open as the roadmap's watch item.
- **The stranded branch rulings (2026-08-11) — lasting, now in the roadmap's constraints and
  `MISSING_PREREQUISITES.md` §D:** DO NOT EXTRACT the remaining layout work from
  `stranded/politics-elections` (nothing is pulling it across — every extraction that landed was
  justified by a number that moved); the branch STAYS AS-IS until item 10 is scheduled (merging ~3,500
  unreviewed simulation lines is what it exists to prevent). Its 30-file inventory travels with §D so
  nobody checks the branch out to learn what is on it.
- **`menu_pattern_tile.png`** — DONE 2026-08-02 (§15); the project root has held no zips since, and
  `DeliveredAssetCheck` enforces the signal (§33).

---

## 33. The asset inventory at HEAD — re-derived from the codebase, 2026-08-27

*Rule 12 and rule 14 applied to the whole delivered set: the five Editor checks run on 2026-08-27 (logs
under `..\PoliSim-captures\logs\check_*_20260827_*.log`), the filesystem enumerated, every display enum
walked, and every sprite's call site traced. `CLAUDE_DESIGN_ASSET_REQUEST.md` §0 carries the same
statement as the live request's baseline; this is the record. The 2026-08-12 derivation in §27 is
superseded by this one.*

**The five checks:** `DeliveredAssetCheck` — 0 missing from 0 root zips, 0 missing from the 13 archived
packs (20 superseded-by-ruling entries skipped by the manifest's `!` allowance; one `ref` —
`board_1i_law_browser.png` is reference material). `StatIconCoverageCheck` — 19 of 19 names resolve
(every `StatNodeId` icon + `menu_pattern_tile`; NOT chrome, emblems, marks or portraits).
`ChromeV2CoverageCheck` — 50 of 50 resolve, 50 of 50 specified present, 11 superseded removed; both
directions clean. `ImporterSettingsCheck` — 140 sprites, 0 errors, 0 warnings (112 white-on-alpha
tinted, 27 full-colour, 1 tiling). `PartyMarkCoverageCheck` — PARTY SYSTEM NOT PRESENT, VERIFIED NOTHING
(honest; item 10's gate).

**The filesystem (140 PNGs under `Assets/Resources/Art/UI/`):** Chrome 50 · Emblems 9 (4
`emblem_party_*` + 5 `mark_party_*`) · Flags 6 · Icons 14 (4 `icon_nav_*` + 10 `icon_area_*`) ·
Portraits 17 · Stats 43 · Textures 1.

**Coverage by DISPLAY enum, and what each enum's art actually reaches:**

| enum / pool | art on disk | reachable | verdict |
|---|---|---|---|
| `StatNodeId` (18) × `PolicyScreenStatsRenderer.GetIconName` | 18 of 18 | 18 called on the stat row | closed |
| `ConsolidatedTab` (6) × `icon_nav_*` | 4 nav icons; Budget and Politics draw `icon_area_fiscal`/`_political` by design | 6 of 6 tab buttons draw an icon | closed |
| `UiPalette.SystemArea` (11) × `icon_area_*` | 10 (Neutral has none) | **2 called** (fiscal, political — the tab bar); 8 drawn and unplaced | held stock — the roadmap decides place-or-hold |
| `PartyArchetype` (4) × `emblem_party_*` | 4 of 4 | drawn in the hemicycle legend | closed |
| party marks (`mark_party_*`, 5) | 5 | **0** — no `PoliticalParty` on main | item 10 (`MISSING_PREREQUISITES.md` §E2) |
| `CountryId` (6) × `flag_country_*` | 6 of 6 | two surfaces (Canvas selector, IMGUI fallback) | closed |
| `CabinetSystem.CandidatePool` (18 ministers) | 10 at the sweep (the nine shipped portfolios' ministers + the Defense PoC) → **18 of 18 after Progress5 (2026-08-27)** | drawn via `GetCabinetPortrait`; `PortraitCoverageCheck` 25 of 25 through `Resources.Load` | closed (the roster look is §V's) |
| `FederalReserveSystem` pool (7 chairs) | 7 of 7 | drawn on the selection path | closed |
| the sitting turn-0 chair (Harriet Ellsworth, `WorldFactory.cs`) | none, and no call site asks | — | a design question on the roadmap, not a gap |
| `ChromeManifest.txt` (50) | 50 = 50 | 42 loaded; **7 with no load call:** `ui_frame_double`, `ui_btn_disabled`, `ui_stamp_draft`, `ui_portrait_frame_oval`, `ui_btn_paper_canvas` (+`_hover`, `_pressed`); `ui_pixel` named in comments only | held stock (the 2026-08-12 "revivable by ruling" set + the Canvas paper button) |
| Stats family (43) | 43 | 18 via `IconLibrary.GetStat`; **25 with no call site** — 19 `icon_stat_*` for stats without a `StatNodeId`, plus `icon_trend_up/down/flat`, `badge_preliminary/revised`, `icon_release_marker` (`GraphRenderer` draws markers procedurally) | held stock |
| `Textures/menu_pattern_tile` | 1 | two call sites | closed |
| Fonts (9 files) | 9 | 3 loaded (Pagella Bold/Regular, Courier Prime) | a decision: the four rejected candidates (Literata, Gentium Book Plus, …) stay as the comparison record |

**Lasting decisions carried from the inventory:** delivered ≠ reachable — an asset's status has two
parts and only the first is visible from the inbox (the flags and emblems sat outside `Resources/` for
weeks); a coverage check is evidence only for the names it ENUMERATES (rule 14) — no check enumerated
portraits, area icons or emblems at the sweep (`PortraitCoverageCheck` was built the same evening with
the Progress5 import; `AreaIconCoverageCheck` stays a roadmap item); the manifest is the SPECIFIED side of a coverage
check and a folder listing can never answer "is everything specified present?"; the project root holding
no zips is the standing signal and `DeliveredAssetCheck` + `.gitignore` enforce it. **The archive:** 12
zips in `AssetPackArchive/` (gitignored), every sprite entry accounted for by the check; the folder's
README is a 2026-08-02 snapshot of seven packs — the check, not the README, is the record, and a stray
`trip-2026-08-18.pdf` sits there that is not a delivery. **`POLISIM_R4_4_PREREPORT.md` stayed on disk
until D1's nine landed** (§22's ruling) — they landed the same evening, and it was consumed to §19 and
deleted. **The eleven superseded chrome
SVG sources were deleted 2026-08-27 by Elias's ruling** — Track 3 had removed the PNGs only; the
per-state SVGs Design shipped supersede the strips, and keeping both was the two-tables problem in
another costume. `ChromeManifest.txt`'s `!` rows stay (the archived zips still carry the old `.svg`
entries, matched by stem), which is why the deletion cannot turn `DeliveredAssetCheck` red — and did
not: re-run after the deletion, every pack `ok`, the eleven names' `.svg` entries logged as
superseded-by-ruling beside their `.png` twins, `ChromeV2CoverageCheck` 50/50 both directions.

**The seventh request SENT 2026-08-27** (Elias's instruction; Claude Code via `DesignSync`): the
single codebase-derived request, in place at `uploads/CLAUDE_DESIGN_ASSET_REQUEST.md` and at the new
dated path `send/design_request_2026-08-27/` with its attachments manifest, digests and the eight
captures; both readbacks hash-identical to the repo file (`9a464915…24eec` / `bf7c2263…7cfb`). The
lesson that shaped the two-path send: an in-place overwrite of a file Design has already read produces
nothing that looks new — it cost a round-trip on the previous send.

**Requests fulfilled and retired from the request doc (2026-08-27):** the pre-v2.0 "eleven hues as they
stand today" table (§3.0 — none of its fifteen values exists at HEAD; the aged v2.0 inks are
`PoliSimTheme.cs`'s and the screen spec's §A.3) and the two importer rulings of 2026-08-11 (§3.0b —
mipmaps off on the 44 files that carried them, the check promoted to error; full-colour block compression
ruled acceptable after a visual check on the flags, the worst case; the enforcing check reads the
imported texture, not the meta). The rules survive in the request doc's §3 table; the history is here.

## 34. Playtest 3 — ten surfaces seen, three findings diagnosed and surveyed, nothing fixed (2026-08-27)

**The session.** Elias ran `MISSING_PREREQUISITES.md` §V as the seventeen-item ordered checklist
(printed 2026-08-27, grouped by game advancement, caveats flagged) in one Editor session. **12 of 17
pass.** Items 1, 2, 6, 7, 8, 9, 10, 15, 16, 17 confirmed seen and cleared from §V (rule 15's third
layer — a capture is a harness film, not Elias's eyes; this is the sighting):

- the Canvas country selector's set (the six flags, the scenario entry beside them);
- Turn → Year on the header strip;
- Budget's dead nested scroll (gone);
- Sweden's 24-line budget decomposition;
- the SWF emergency drawdown bill (standalone, its own countdown);
- option C's deliberate-choice paragraph on the Fed/ECB surface;
- pass 6's four Trade surfaces — the inert base-tariff dial (bloc member), the retaliation label on
  the partner row, the bill card's cost line, and the stats line (the last one placement-flagged
  under finding 3, but the line itself seen and passed);
- the rejected-bill seal on the Signing screen;
- Italy Debt Crisis as a playable scenario, with the fiscal trace opened mid-run;
- Step 3's verdict screen with the Sustained streak line, and the scenario's entry on the selector.

Three findings came back, in Elias's priority order. **Each was diagnosed or surveyed and REPORTED;
none was fixed** — the instruction on all three was numbers before a fix, dimensions before a change,
survey before a cut. The fixes are Elias's rulings and are still open in §V.

**Finding 1 — the Compass Y axis: a MODEL cause, not a plot cause.** `Assets/Editor/CompassAxisDiagnostic.cs`
(new, retained, run by `-executeMethod PoliSim.EditorTools.CompassAxisDiagnostic.Run`; log
`PoliSim-captures\logs\compass_axis_20260827_*.log`) builds `WorldFactory.CreateDefault()` and prints,
for each of the six countries at turn 0, every sector's `RegulationLevel`, the implemented-welfare count
and generosity mean, the raw X and Y axis values, and the plotted position under the renderer's own
`PadRange`/`InverseLerp` rule (replicated from its private helpers — the renderer itself untouched).
The numbers: **every country has all eight sectors at regulation 50.0 and 0 of 6 welfare programs
implemented (all six off at generosity 50), so Y = (50 + 0) / 2 = 25.000 for all six**; X is a real
spread (USA 21.6, Sweden 36.5, Germany 36.7, France 42.1, Italy 34.7, Poland 33.7 — range 20.4, padded
18.6…45.1). Y's raw range is 0.000, so the padding rule's flat ±5 gives 20…30, every `ty` is 0.500 and
every dot sits on the plot's mid-line — the horizontal line Elias saw, by construction. The plot is not
at fault: the auto-scale spreads any real range over the full height, as it does for X, and the
"Y: regulation & welfare generosity, 20…30" range label is an honest label of a constant. Why it stays
constant in play: `Sector.RegulationLevel` (default 50, `Sector.cs:47`) has ONE writer,
`ApplySectorPolicyChanges` (`SimulationManager.cs:2796`, the player's own PolicyDecision), and
`WelfareProgram.IsImplemented` has one, `ApplyWelfareProgramBillResult` (`ParliamentSystem.cs:389`,
the player's own bill) — no AI system moves either, so the five AI dots hold Y = 25 for the whole
game and only the player's dot can leave the line, through the sector regulation dials or a welfare
bill. A fix is therefore a MODEL question (a per-country seed spread for regulation and/or
implemented welfare — which moves every no-policy baseline, a discontinuity — or a Y axis re-derived
from data that already varies at seed) and is Elias's ruling.

**Finding 2 — the portrait draw size: one number governs, measured, unchanged.** All three portrait
surfaces (roster `GameController.cs:7755`, candidate card `:7819`, Fed chair card `:3235`) size through
ONE method, `DrawPersonPortrait` (`:3263`): `height = _labelStyle.fontSize × 3.2` (`:3272`),
`width = round(height × 74/92)` (`:3273`, the frame's @1x proportion), art inset 5 px each side inside
the brass frame (`PortraitFrameArtInset`, `:683`); `fontSize = clamp(round(Screen.height × 0.022), 16, 28)`
(`RescaleStylesToScreen`, `:2341`). So the ART draws at **31×41 px at 1280×720 and 1640×707 (font 16),
41×54 px at 1600×900 and at the Editor's recorded 1600×929 operating size (font 20), 62×80 px at
2560×1440 (font 28, clamped from 32)**. The eight new 512×640 sources are minified 12.5× linearly at
1600 and 8.3× at 2560, with mipmaps OFF by the importer ruling — a 12× mip-less minification samples
about one texel in 150, which is the sparkle on top of the smallness; the sixteen 256×256 squares are
minified 4.7×/3.2× and cropped 20% in width by `ScaleAndCrop` at the 74:92 rect. The frame texture
`ui_portrait_frame.png` is 148×184 (the @2x of 74×92) and draws at 0.35× (1600) / 0.49× (2560) of its
texel size — the rect is ≈1× the frame's design size only at 2560. The rows around the portrait are
GUILayout horizontals whose height follows the tallest child, so no second number clamps a taller
portrait; the 3.2 multiplier on line 3272 is the one governing number. Nothing changed. What size to
draw at is Elias's ruling; the same-hand question stays open until the portraits can be judged.

**Finding 3 — the declutter survey, every element classified, nothing cut.** Five surfaces (items 4,
11, 12, 13, 14), the Budget line-item row as the reference (`DrawTaxLineRow`, `GameController.cs:8709`:
"ONE ROW, not four stacked lines … the estimate's prose collapses to the verdict word it was carrying"
— the precedent Elias named). Two host corrections first: the primary-balance line is on
**Statistics › Domestic** (`DrawDerivedStatsRow`, called only from `DrawDomesticStatisticsContent`,
`:5412`), not the Budget tab; the pass-through line is on **Statistics › International**
(`DrawTradeStatsContent`, called only from `DrawInternationalStatisticsContent`, `:5558`), not
Policy/Laws › Trade — §V corrected. The survey itself (every header, caption, paragraph, label and
value, marked (a) needed now / (b) learn-once / (c) restates a neighbour) is in `CLAUDE.md` "Playtest 3
— the Compass Y axis diagnosed, the portrait draw size measured, the declutter survey (2026-08-27)".
Its shape: the law browser carries three (b) paragraphs/captions and five (c) restatements (the "Laws"
box header under the "Laws" sub-tab; the summary line's counts beside the group captions; STATUTE and
CATEGORY as column captions over self-naming cells; the class named twice within three lines of the
detail pane; the enactment cost printed on the row and again in the pane); the trace panel's rows are
(a) almost throughout, its clutter being (b) mechanism parentheticals in the trailing column and the
(c) audit footers that restate the header's delta; the two stats lines are (a) with placement as the
whole finding. Two placement facts the categories cannot carry: on Policy/Laws the trace panel opens
BETWEEN the chip row and the tab's content and may take the whole host height (`MaxShareOfHostHeight 1f`),
so opening a trace shrinks the law browser under it — items 4 and 11/12 interact; and the realized
pass-through figure and its forecast twin ("prices +0.00 pp this year" on the Trade bill card) are
two readings of one quantity on two screens. **B1's amber draft cue** (the hatched span on every
Budget line row, `LedgerRow.Draw`) and **B8's interrupt line** (`DrawFullScreenPendingInterruptBanner`,
`:8358`, on the Budget Process host of the fiscal trace) are load-bearing and were flagged, not
surveyed for cutting. The cut list is Elias's.

**Records.** §V rewritten (the ten cleared, the three findings with their diagnosed/measured/surveyed
status and the two host corrections); this section; the `CLAUDE.md` entry with the numbers and the
element-by-element survey. Boards 1k/1l and the nine unbuilt spec clauses stay unstarted behind §V,
by Elias's ruling.

**The rulings and the build, the same evening (2026-08-27).** Elias ruled on all three findings within
the session. (1) COMPASS — option (i), a per-country seed spread for sector regulation and implemented
welfare programs, from real data: the mechanism is built (`6df94de` — `Sector.BaselineRegulationLevel`,
`Country.BaselineWelfarePrograms`, `MacroSystem.WelfareEffectDelta`, the `WorldFactory` slots), in the
ANCHORED form (the sourced baselines already contain each country's real regulation and programs, so a
seeded value is the zero-gap position, never a live deviation — the reasoning and the live-form revert
are in the `CLAUDE.md` entry); every slot a `[PLACEHOLDER]` because the figures are unsourced, and
Elias's to source — `MISSING_PREREQUISITES.md` §F. The no-policy trajectories are byte-identical (6 of
6) and stay so when the figures land; the ruling's expectation of a discontinuity row is corrected in
the record, with the row drafted for the live form should Elias rule for it. (2) PORTRAITS — 3.2 → 5.5
(`4e5adbf`): art 78×100 px at 1600, 61×78 at the floor, 114×144 at 2560; the card pitch measured
unchanged at 1600 and text-governed at 1280 (cost 0 px). (3) THE CUT — executed by the survey's own
categories on the five surfaces (`4e5adbf`), B1 and B8 untouched, the laws reserve re-derived from the
chrome drawn; captured at the four sizes with every guard silent. Two residuals named on film, both
Elias's call: the law row pitch (`LedgerRow.Height × 1.4` — 4.5 laws per screen at 1600) and the
selected law's name breaking mid-word at 1600 (pre-existing). The three placement findings stand as
moves, reported, not made. The seven §V rows now wait on Elias's eyes (the cut, the portraits) and on
§F's figures (the compass).
