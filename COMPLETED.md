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

## 35. The discipline record — rules 0–15 as they stood at `5f56798` (migrated verbatim 2026-08-28)

**Why this section exists.** Elias's omnibus kickoff of 2026-08-28 installed Working Discipline v2 in `POLISIM_MASTER_ROADMAP.md` — ten rules in place of the fifteen-plus that had accreted since 2026-07-31 — and ruled that the displaced prose, every embedded history, caveat and correction, be migrated here VERBATIM so nothing is lost and rule 10's own requirement (reversals recorded, never silent) is satisfied by the migration itself. Numbered references to "rule N" across the record (rule 4's `RULINGS NEEDED`, rule 12's cached status, rule 13's lock, rule 14's enumeration, rule 15's diff) resolve against THIS text. Below is the section exactly as it stood, heading included.

## Non-negotiable working discipline (applies to everything below, no exceptions)

0. **SCALE VALIDATION TO RISK (added 2026-08-02).** Real Unity stays the standard of truth — rule 1 is
   unchanged — but the *size* of the check matches what the change can actually break:
   - **Simulation math** → full matrix, 100 and 500 turns, like-for-like before/after.
   - **UI-only** → compile check plus a smoke run. A change that cannot reach simulation math cannot move
     a trajectory; `BatchSimulationRunner` never calls `OnGUI`.
   - **Data-layer additions nothing calls yet** → compile check.

   **Three further standing cuts, same date:** stop creating new standing documents (findings go in
   `CLAUDE.md`, status in this file); the verification-integrity log **stops at 10** — later instances get
   one line, not a numbered write-up; and **batch the reporting** — work through several items and report
   once, unless something fails or needs a decision.

   ⚠ **What does NOT get cut**, because each caught a real defect in the last two days: real-Unity
   validation before anything is called done; never inventing a `[GAP]` figure; visual work being
   built-not-confirmed until Elias sees it; verifying against commits and callers rather than summary
   memory; and the API cross-check gate.

1. **Real Unity is the standard of truth, not the standalone harness.** It has been wrong about project state multiple times this project (a stale swing threshold, an interest-rate crash mischaracterized as noise, a debt trajectory that flatly contradicted real Unity). Use it for fast iteration only. Before considering *anything* done, validate via `BatchSimulationRunner` against real Unity (`G:\UNITY\Unity Hub\6000.5.6f1\` - migrated from `6000.5.4f1` on 2026-08-01 after the older install became corrupted; see CLAUDE.md's "Real-Unity Validation is the Standard Path" for the full story) at both 100 and 500 turn horizons (or their day-equivalent, once the continuous-time migration changes the unit).
2. **Watch for the six failure patterns already seen repeatedly**: turn-1 discontinuities, oscillation, unbounded/compounding growth, bimodal attractors, and two new ones (both new as of Continuous Time + Parliament + Cabinet/Foreign-Policy coexisting, both surfaced investigating the SAME reported live-play freeze):
   - **Background/timed state mutation vs. active UI interaction** — a background system (a bill resolving, or any future timed/probabilistic mechanic) mutating live state that a GUILayout control is reading, on a day/frame the player has an active multi-frame drag in progress on that exact control. GUILayout allocates control IDs positionally, not by a stable key, so a control disappearing or a preceding control's count changing mid-drag is a documented Unity IMGUI hang/desync trigger, especially inside a ScrollView — and it's invisible to `BatchSimulationRunner`, which applies policy decisions programmatically and never drives real OnGUI/mouse-drag events, so no batch run can ever catch it. First hypothesized in the Tax Policy tab (Master Sequence step 4 pilot) when a pending TaxBill could resolve while the player was mid-drag on a rate slider; hardened there via the stable-control-layout pattern (see `GameController.DrawTaxPolicy`'s doc comment, commit `adb34ae`) regardless — every control a gated tab can ever draw renders every frame, in the same order, with "not currently applicable" expressed via `GUI.enabled = false` (composed with, not clobbering, any ambient enabled state) rather than by omitting or swapping the control. **Caveat, recorded honestly**: this fix did NOT resolve the reported freeze — Elias reproduced it again under the same conditions after commit `adb34ae`. The pattern and fix are still real and worth keeping (every one of the seven remaining tabs gains this exact same theoretical exposure once Master Sequence step 5 wires them into the draft/bill/vote model), but it was not the actual trigger of the original report. See the next pattern for what the investigation found instead.
   - **A legitimately time-blocking decision with no globally-visible indicator** — Fed Chair term appointment, a Cabinet decision, and a Foreign Policy meeting all correctly pause `GameController.Update`'s day-loop (every gate is checked correctly - this is NOT a simulation bug), but each one's actual resolution UI (the Fed Chair candidate picker, `DrawCabinetDecisionModal`, `DrawForeignPolicyMeetingModal`) renders ONLY inside its own specific tab's draw call - never globally. A player on any other tab (e.g. Tax Policy) when one of these fires sees simulated days silently stop advancing with no visible cause - indistinguishable from a hang. Before the fix, `DrawCalendarAndSpeedControls`'s always-visible status line (the one piece of UI pinned outside the scroll view on every tab) named the reason for Fed Chair and Cabinet only, in a modest, easy-to-miss label style, and said NOTHING for a pending Foreign Policy meeting - the one of the three statistically most likely to fire early in a fresh session, since it rolls per DAY (~1% chance) rather than per TURN (121-day then, 365-day since `d8f55ce`) like the other two. Fixed by escalating that line to the same bold/orange `_eventBannerStyle` used for the dashboard's own BREAKING banner whenever ANY of the three is pending, always naming which one and which tab resolves it - still exactly one Label control either way, per the stable-control-layout pattern above. This is a genuine UX gap, not a code crash: every future interrupt/decision system (gated legislation on the remaining seven tabs very much included) needs its "something needs your attention" state represented somewhere visible from every tab, not only on the tab where it originated.

   Assume a new mechanic is guilty of all six until the full-horizon batch run (for the first four) and direct live-Editor confirmation (for the last two, which batch runs cannot exercise) prove otherwise.
3. **Commit per unit of work.** One feature, one commit, descriptive message. Confirm staged contents match the message before committing.
4. **⚠ REPLACED 2026-08-11 at Elias's direction — ESCALATE TO ELIAS IN THE REPORT, NOT TO A DOCUMENT.**
   Recorded explicitly per rule 10's own requirement that a reversal never look like drift. **Previous
   wording (2026-08-02):** *"make the call, state the reasoning in the commit message, and flag it for
   Elias to overrule; escalate to Open Questions only when undoing it would be expensive or
   irreversible."*

   **Why it changed: Open Questions became a queue nobody drains.** An escalated question sat there
   unruled until work reached it months later and halted — so the escalation deferred the interruption
   instead of preventing it, and did so to the least convenient moment. Deciding-it-yourself still
   applies to everything reversible; what changed is where the genuine forks go.

   **The new standard:**
   - Every report ends with a **`RULINGS NEEDED`** block. One entry per question: the question stated so
     it can be answered **yes/no or A/B** wherever possible; the recommendation with one or two lines of
     reasoning; and **what it blocks**, or *"nothing, decide when convenient."*
   - **If a question blocks the pass in progress, STOP THERE and report it** rather than carrying it to
     the end. *A blocked pass reported at minute 3 is worth more than a finished pass that guessed.*
   - **"I can't call this one" is a legitimate entry.** A genuine coin-flip presented as a recommendation
     is worse than an admitted one.

   ⚠ **ONCE RULED, WRITE IT DOWN in whichever document owns the decision** — request doc, roadmap, or
   `CLAUDE.md`. **A ruling given in chat and not recorded did not happen**: same class as *"a delivery is
   not self-announcing"*, and the same failure mode, since the next session reads documents rather than
   transcripts.

   **Open Questions stops being a queue and becomes a record of decisions made.**
5. **Ground new mechanics in real data.** Label anything stylized honestly — never let a placeholder look like real data.
6. **Scope every new system small on the first pass.** Plumbing plus a few clearly-justified effects, not full theoretical richness.
7. **Update CLAUDE.md after every item**, including validation results, so history stays traceable.
8. **Verify Unity processes actually exited** (`Get-Process Unity*,UnityPackageManager`) before trusting that a closed window means it's safe to run a batch validation — confirmed to cause false failures more than once.
9. **⚠ SPLIT 2026-08-11 at Elias's direction — INSTITUTIONS MAY BE REAL, PEOPLE NEVER ARE.** Recorded here per rule 10's own requirement that *"any FUTURE reversal of a standing rule must be recorded the same explicit way"*, and recorded late: `main` carried four real party marks (`mark_party_us_gop`, `mark_party_us_dem`, `mark_party_se_s`, `mark_party_se_v` — imported, guarded and documented) for several hours while this rule still forbade them outright. The reversal was written down only on `stranded/politics-elections`, so `main` held the consequence without the rule. **That is the cached-status failure of rule 12 applied to a rule rather than to an asset.**

    - **PARTIES AND INSTITUTIONS — REVERSED, may be real.** Real party names, real vote shares, real seat counts, real thresholds, real chamber sizes and real electoral formulas. The Riksdag holds Socialdemokraterna and Sverigedemokraterna, not Progressive Alliance.
    - **PEOPLE — UNCHANGED, and this half is not negotiable.** Cabinet ministers, party leaders, legislators, Fed Chairs and heads of state remain **original and fictional**. The Fed Chair rule stands exactly as written.
    - **The distinction, stated so it survives paraphrase: a real party is an INSTITUTION; a real politician is a PERSON.** Only the first is reversed.

    **The cost this buys, stated so nobody rediscovers it:** real party data goes stale. Sweden votes 13 September 2026 and Italy's replacement electoral law is before the Senato that month. **Seed data is now a cached value with an expiry** — rule 12's shape — so every seeded figure carries its retrieval date.

9a. **NEW 2026-08-11 — TRADEMARK: A PARTY MARK IS ORIGINAL ART, NEVER THE PARTY'S OWN MARK.** Real party *names* are text and are used. Real party *logos* are marks owned by organisations, and reproducing one in a commercial game on Steam is a different proposition entirely. **Every `mark_party_*` sprite is an original abstract drawing** — recognisable by silhouette and by the party's real colour, owned by us, and defensible.

    **This is already load-bearing rather than theoretical.** The delivered pack gave Socialdemokraterna a **banner** rather than a rose *specifically because the rose is the subject of their registered mark*, and the Democrats a **torch** rather than the donkey. That reasoning was recorded once, in a delivery note, and must be stated in **every future party-mark request** rather than re-derived by whoever writes the next one. ⚠ It applies directly to §1G's outstanding `mark_party_us_lib`, whose party's associated imagery carries the same question.
10. **REVERSED (2026-07-31), was a hard rule through Master Sequence step 5d**: visuals are now a MIXED procedural/sprite model, not "all procedural." Elias has explicitly approved imported sprite art for **icons, portraits, and background/menu textures specifically** — see `CLAUDE_DESIGN_ASSET_REQUEST.md` (the single standing asset request; the original 5E/chrome/macro requests were consolidated into it 2026-08-02, all delivered) for the asset work this decision unblocked. **Stays procedural, unchanged, no exception**: all UI chrome/layout (`PoliSimTheme.cs`'s `RoundedBox`/`RoundedCard`/`Pill`/`Rule`/`TopAccent`/`LeftSpine` — pure `GUI.DrawTexture` rounded-rect/line geometry, no art asset, no reason to change) and every existing DATA visualization (`GraphRenderer`, `MapRenderer`, `PolicyWebRenderer`, `PoliticalCompassRenderer`, `HemicycleRenderer`) — none of these draw a "picture," they render real tracked simulation data, which is exactly what rule 5 ("ground new mechanics in real data") already protects; nothing about the icon/portrait decision touches that. **Becomes sprite-based**: one icon per `UiPalette.SystemArea` (policy area), one portrait per Cabinet minister candidate, one emblem per `PartyArchetype`, and background/menu textures — all sourced from Claude Design with the same origin-verification and security-review discipline already established for the first pack (Zone.Identifier mark-of-the-web check, full code/asset read-through before treating anything as trusted). This is a real, deliberate policy reversal, documented as such per this same working-discipline section's own precedent for recording a caveat/correction honestly rather than letting it look like silent drift - any FUTURE reversal of a standing rule must be recorded the same explicit way.
11. **Any new mechanic that nudges an existing tracked variable must fold into that variable's existing combined ceiling**, not add an uncounted new source — audit the actual ceiling code before adding a contributor, don't assume there's room.
12. **NEW (2026-08-02) — "awaiting delivery" is a status that must be RE-DERIVED FROM THE FILESYSTEM, never trusted from a document.** Two separate assets were recorded as outstanding while already sitting in zips at the project root: `icon_stat_interestrate` (registered *"REQUEST SENT, awaiting delivery"* on the day it in fact arrived) and `menu_pattern_tile.png` (delivered, then unimported for weeks while three documents named it as a gap). **Neither register was wrong when written.** Nothing watches the project root, a delivery does not announce itself, and so the status simply outlived the fact — twice, which is what makes it a pattern rather than an oversight. Both gaps were eventually closed only because Elias happened to say the file already existed. **Run `DeliveredAssetCheck` before reporting any asset as outstanding**: it compares every zip's contents against what exists under `Assets/` and fails on any gap, which is the one comparison that cannot go stale. Its companion `StatIconCoverageCheck` asks the runtime half of the same question — that a name the UI hard-codes actually resolves through `Resources.Load`, which a file merely existing on disk does not guarantee when its `.meta` is hand-written. The general form: **a status describing the outside world is a cached value, and needs an expiry.** ⚠ *Amended 2026-08-11: `StatIconCoverageCheck` covers the 19 names it ENUMERATES — every `StatNodeId` icon plus `menu_pattern_tile` — not "a name the UI hard-codes" generally. See rule 14.*

13. **NEW (2026-08-11) — TWO AGENTS IN ONE WORKING TREE NEED A LOCK, not a cleanup afterwards.** On 2026-08-11 two sessions wrote this repo concurrently with no coordination, and it produced three distinct failures, none of which either session could see from inside. **A merged contradiction:** one session recorded "§1E is closed" while the other recorded "I will not file these because §1E's namespace blocker is open" — read together as one agent reasoning badly, when it was two agents' claims merged without attribution. **A silent co-commit:** commit `452bf68` staged three files by explicit path and still carried ~150 lines of the other session's uncommitted §1F prose, because staging by path does not stop a path carrying another author's changes. **A stale lock read as litter:** a 2.2-hour-old `.git/index.lock` with no `git` process alive — correctly diagnosed as stale, but *"no process is running now"* and *"no session owns this"* are different propositions, and only the first is observable.

    **The rules.** Before any commit, run `git status` and confirm every staged path is one this session actually modified — `git show --stat HEAD` afterwards is the backstop, not the check. **Never `git add -A` / `git add .` in a tree that may be shared**; stage by explicit path, and inspect the diff of any path you did not create. **Never clear an `index.lock` without first confirming no live session owns it** — a dead process's litter and a live session mid-operation are indistinguishable from the lock's mtime alone, the same way a closed Unity window is not proof Unity exited. When a document's claims contradict each other, **suspect two authors before suspecting bad reasoning**, and check `git log`/`git blame` for authorship before writing a correction that may be arguing with a version that never existed.

    **Rule 13 at the filesystem level (2026-08-25, the two-copy consolidation — `COMPLETED.md` §31):** G: is
    the working copy; the C: copy is `PoliSim-ARCHIVE-DO-NOT-OPEN-2026-08-16` with `ProjectSettings.RETIRED`
    so it cannot be launched. **Standing habit: every harness or tool invocation passes the explicit project
    path** — the `/code-review` fall-back to `C:\Users\elias` was the instance that earned the rule.

14. **NEW (2026-08-11) — A CHECK IS EVIDENCE ONLY FOR CLAIMS ITS ENUMERATION CONTAINS.** `StatIconCoverageCheck` was cited in two documents as proof that four newly imported party marks resolved through `Resources.Load`. It enumerates `StatNodeId` plus `menu_pattern_tile` and never touches `Emblems/` — it passed **19 of 19** with the marks present, and would have passed with them absent or corrupt. `CLAUDE.md` had the scope stated correctly the whole time; `COMPLETED.md` said *"every name the UI hard-codes"*, and that looser phrasing is what licensed the misuse. **A passing check that cannot fail for the stated reason is worse than no check, because it retires the question.**

    The same defect recurred twice more in one session, which is what makes it a rule. A **57-file byte-identical diff** was read as proof that five import blockers were closed — but two of those blockers are about NAMING, which is not a byte-level property, so the diff structurally could not see them. And `PartyMarkCoverageCheck`'s own first version reported *"4 of 4 resolve at 128×128, the metas are sound"* when it only checked that a handle came back; extended to assert `texture.format`, it immediately found all four imported as **DXT5** — block compression on white-on-alpha, the exact damage vector the settings exist to prevent. **When citing a check, name what it enumerates.** When a check's bar is another artifact ("matches the reference"), it inherits that artifact's defects — the reference emblem was itself DXT5.

15. **NEW (2026-08-12, Elias) — COMPARE AGAINST THE PREVIOUS CAPTURE SET; DO NOT JUST LOOK AT THE NEW
    ONE.** `cbdde4e` shipped the selected sub-tab's label as cream on pale paper — unreadable — and its
    own capture run was **approved by eye with the defect on screen**. It was caught a day later
    (`4192042`) not by looking harder but by putting the pre-conversion `accessors_*` set beside the new
    one: readable white-on-brass next to unreadable cream-on-paper is a finding no single image
    produces. **The three verification layers each answer a different question, and only the third
    answers regression:**

    | Layer | Answers | Cannot answer |
    |---|---|---|
    | Guards (`UiOverflowGuard` / `UiContainmentGuard` / `ScreenEdgeCheck`) | containment — does content fit and stay inside? | composition — does it READ? |
    | A single capture set, by eye | plausibility — does this look like a working screen? | change — is anything worse than before? |
    | The set DIFF, old beside new | **did this change break something?** | — |

    ⚠ One practical limit, measured 2026-08-12: the capture warm-up is **unseeded**, so two sets differ
    in every simulated figure (AA+ vs AAA between consecutive runs) — the comparison is structural and
    by eye, never pixel-wise, until someone decides seeding the warm-up is worth it.

    ⚠ **PAIRED-DETECTOR CORRECTION (2026-08-24, the Calendar Panel pass).** The table above reads
    naturally as three passes over overlapping ground, which invites an assumption neither the table
    nor this rule ever actually claimed: that one layer's blind spot is the other layer's job. **It is
    not, and two findings from the same session sit cleanly on either side of the line.** A day-cell
    height defect (a real overflow, `_calendarDayNumberStyle` sized against a flat guess instead of its
    own metric) was caught by `UiOverflowGuard` **alone** — 2,004 violations, found straight from the
    guard's own count before any image was opened, no eye involved at all. A ledger date-column that
    **wrapped** rather than clipped at 2560px (`"10"` over `"/1"`) was caught by eye **alone** — none of
    the three guards reports it, because a string that wraps instead of overflowing satisfies
    containment, fit, and edge-flushness simultaneously; wrapping-instead-of-clipping is not a
    question any of them asks. **Neither layer backstops the other's blind spot.** A guard's blind
    spot is not safety-netted by looking (the guard-only case above needed no eye at all to be found,
    but nothing guarantees a DIFFERENT guard-blind defect would be visually obvious the way this one's
    count was); an eye's blind spot is not safety-netted by a guard built to answer a narrower question
    than "does this read well" (wrapping is exactly the shape rule 15's original finding — the sub-tab
    ink of `cbdde4e`, fixed `4192042` — also was: a composition question, not a containment one). Read the table's three rows as three
    INDEPENDENT questions with three independent gaps, not three redundant passes that jointly cover
    the ground — the reference-class trap's own lesson (2026-08-11: "adjacency is not sameness")
    applied to verification layers instead of to a lookup.

    **RETENTION (added 2026-08-16, the repository-weight pass).** "Keep old sets to compare against"
    is why captures reached 5,316 PNGs / 5.1 GiB in five days, 2,003 of them committed (~874 MiB of
    git blobs — see CLAUDE.md "The repository weight finding"). The comparison this rule actually
    needs is **one good baseline per axis, plus the run under judgment**:
    - **Keep**: the current baseline set per axis — the main sweep per size, the per-country coverage
      sets, the state-pin sets — and the most recent run per size. A superseded baseline is kept only
      until its successor is confirmed, then it becomes prunable.
    - **Prunable**: every older iteration set, the moment its finding is recorded in `CLAUDE.md`. The
      set is EVIDENCE while the finding is open and a cache once it is written down — rule 12's shape
      applied to pixels.
    - **Mechanics**: captures live OUTSIDE the tree at `../PoliSim-captures/` (driver, capture entry
      point and `ScreenEdgeCheck` all read one shared default, `-shotdir=` still overrides;
      `/screenshots/` is gitignored defensively). Nothing under the capture dir is ever committed.
    - Applying this policy today keeps roughly 1,900 files (~1.9 GiB) and marks ~3.2 GiB prunable —
      ✅ **APPROVED 2026-08-16 (Elias) — and EXECUTED the same day** (the prune ran alongside the
      history rewrite; the execution annotated here 2026-08-26, one word late per its own rule). The history rewrite was ruled YES
      the same day and ✅ **EXECUTED later that day as its own gated pass** — pack 742.03 → 4.92
      MiB, 76 citations swept, fresh clone at 4.89 MiB with all six checks green. Full record in
      CLAUDE.md "The history rewrite — executed 2026-08-16"; backup + commit-map at
      ~~`C:\Users\elias\PoliSim-backup-2026-08-16`~~ **`C:\Users\elias\PoliSim-ARCHIVE-DO-NOT-OPEN-2026-08-16`
      (renamed 2026-08-25 under rule 13, `ProjectSettings` → `ProjectSettings.RETIRED` so it cannot be
      launched; `.git` untouched — the pin and the commit-map are why it is kept).**



## 36. The omnibus pass — every live item of the 2026-08-27 board in one session (2026-08-28)

**Elias's kickoff (2026-08-28): "complete all live items in one pass; streamline the working discipline."**
Ten pre-issued rulings R-K1…R-K10, seven phases, one report at pass end. Anchor `5f56798`; the pass ran
on `main` alone (rule 13). What follows is the record; `CLAUDE.md` "The omnibus pass" carries the detail
per phase and per unit.

**Phase 0 — Working Discipline v2 (`0dc3ed8`).** The ten rules at the head of the roadmap; rules 0–15
as they stood at `5f56798` migrated verbatim to §35 so every "rule N" reference still resolves.

**Phase 1 — the chrome sweep, eleven units in ten commits (`f92e14f` … `476c66c`).** Roadmap item 4 in
full (the RUNNING status plate; B5's disabled speed-button face and the `ui_btn_disabled` loader; §A.8's
right-aligned screen caption; §A.3's inactive tab-swatch tints; the §A.2 tokens without a constant — five,
not three, re-derived per R-K10; §A.11's urgency chip as a rotated bordered stamp; the row-family residues
on the Fed tab, International and the Trade bill card; the 2560 Trade wrap answered structurally), item 5
per R-K6 (the eight `icon_area_*` on the sub-tab rows where the row can carry them; the 25 Stats sprites
and the six chrome names recorded as held stock; the sitting chair's text treatment;
`AreaIconCoverageCheck`, 14/14, in the suite), boards 1k and 1l per R-K3 (the calendar as one almanac
sheet with the diagonal strike; the graph weights), the law browser's two residuals per R-K4/R-K5 (the
ledger pitch measured before → after at three sizes; the name ladder). Each unit compiled and captured at
the touched sizes before its commit; every guard silent.

**Phase 2 — the causal graph on the Policy Web (`a267fd6`, R-K1).** Derived edges from the ledgers'
term IDs, declared edges dashed at 55%, stat → stat chords, per-country sets, the two generic-line folds.
Dumps 6/6 byte-identical; captures clean.

**Phase 3 — the two remaining scenarios measured and dropped (`11c28a2`, R-K2).** Poland convergence to
`UnemploymentReversionSpeed` a third time; The Unequal Recovery to a new root cause in the political
model — the Progressive and Conservative seat targets are identical at every approval level, so no
expansionary bill passes on any drift path except by ±1-seat jitter. `ScenarioLibrary` stays at two. The
political-model fact goes to item 10's file (§D).

**Phase 4 — the seed spread sourced (`915c800`, R-K9).** OECD PMR 2023-24 and SOCX 2021 through §F's
mapping as written, every figure `[PROVISIONAL - session-sourced 2026-08-28, Elias to confirm]`; the
anchored form held (dumps 6/6 identical); the Compass Y axis spreads 19.8 units. Six caveats wait on
Elias's confirmation (`MISSING_PREREQUISITES.md` §F).

**Phase 5 — the rasterization diff closed (`a15c0c1`, R-K8).** The Unity path is blank even windowed;
resvg 0.47.0 (origin and digest verified) as `StripCutDiffCheck`'s external rasterizer; the six buttons
6/6; 77 of 90 within budget; two real findings for Design (the hatch tile's un-rotated tiling; the slider
track's strip) and nine Stats icons over a blind-set budget by an antialiasing margin (RULINGS NEEDED).

**Phase 6 — the four decompositions (`6307dce` Germany, `ad7b240` Italy, `d33e1ae` Poland, `e04f238`
France, R-K7).** Sweden's method with one stated deviation: the four's transfer layer already sits in the
recalibration's `SeedMandatoryTransferLines`, so each decomposition weights G with the state budget's
non-transfer areas and carries no mandatory line. Sources: bundeshaushalt.de's plan CSV; the RGS BDAP
chapter dump; the act's own annex (read out of the PDF); the Assemblée's adopted text (État B). Every
diff noise-level; every capture clean. The scaling distortion measured per country and recorded.

**Phase 7 — the closing gate (`bar_phase7.ps1`, one Unity launch per step, ATTRIB 0 throughout).** The
capture matrix at 1280×720, 1600×900, 1920×1080 (the first set at that size) and 2560×1440
(`omni_final_*`, 64/64 each, both text guards silent); `ScreenEdgeCheck` on every set; the closing dump
byte-identical to the France baseline 6/6 and diffed against `pre_seedspread` (16–18 of 42 fields
identical, the rest the decompositions' last-ulp noise); equivalence 117/117; round-trip 12/12; parity
7/7; delivered assets 0 missing; portraits 25/25; area icons 14/14; the matrix 30/30 with 27 cell counts
identical to the last pre-pass matrix, `welfarestress` moved by the USA's real seeded programs and
`parliamentstress`/500 by one threshold crossing, no new kind. The rule-15 diff (old beside new, ranked by
`capdiff.ps1`, read by eye at 1600 and on the 1920 set): every touched screen as built. **The gate's one
catch, this pass's own — label-clipping instance #14:** the Laws panel 28 px past its frame at every size
(flagged at three sizes, hidden under the margin column at 1920), measured by a width probe to the Laws
box's widths being taken one nesting level short, fixed at the nesting (`a331e82`), the four sizes
re-captured under the same labels; `ScreenEdgeCheck` after the fix 0 clipped at all four. The same commit
closes the class's second member on that screen, R-K5's residual: the detail pane's scroll content ran
wider than its viewport (the MAGNITUDE row and the un-widthed action button past the content width in every
capture back to Phase 2 — a horizontal scrollbar across the pane — and the `FlexibleSpace` pushed the
status past the viewport, "not enac|" at 1280), measured by a second probe and sized to the pane; read by
eye at 1280 and 1600. Detail: `CLAUDE.md` "The omnibus pass, Phase 7".

**Consumed rulings:** R-K1 (Phase 2), R-K2 (Phase 3), R-K3 (1k, 1l), R-K4, R-K5, R-K6 (Phase 1), R-K7
(Phase 6), R-K8 (Phase 5), R-K9 (Phase 4), R-K10 (Phase 1). **Shelved:** nothing. **Deviations, stated:**
the sub-tab icons drop where a row cannot carry them (R-K6, by width); the four decompositions carry no
mandatory line (R-K7, the recalibration's block); resvg v0.47.0 rather than the latest release (no Windows
asset in v0.48.1); the raster budget left as set.

## 37. The continuation kickoff — the queue drained, the one-line row, the diff budgets, the film gaps, the seed debts (2026-08-28)

**Authority:** Elias's continuation kickoff of 2026-08-28 ("drain the queue, close the film gaps"), issued
as the confirmation for R-C3 and R-C4 and pre-issuing R-C1, R-C2, R-C5 (the flagged assumption), R-C6,
R-C7 and R-C8 — none struck. Anchor `dc6f491`; Working Discipline v2; five phases, five commits, staged by
explicit path; nothing dropped (rule 10's order was 4, 3, 2). Detail: `CLAUDE.md` "The continuation
kickoff (2026-08-28)".

**Phase 0 — the queue drained (`2f77bd1`).** R-C1…R-C4 and R-C7 written into their owning documents;
`MISSING_PREREQUISITES.md` §A's A4–A6 tombstoned as ruled, §F tombstoned as CONFIRMED with its sourcing body
moved to `POLISIM_SEED_DATA_MACRO_OVERHAUL.md` §8 (basis notes, two standing notes), §P's R-C7 context line;
the R-C3 confirmation under `CLAUDE.md` "Playtest 3, the rulings" §1; `WorldFactory.cs`'s slot tags
(comments, compile-checked). `RULINGS NEEDED` emptied of everything not Elias-in-person.

**Phase 1 — the one-line law row (R-C1, `a7d877d`).** Both families one-line at board 1i's proportion
(32 px on a 14 px name = 2.29 name-fonts; ours 37 / 43 / 55 px at 1280 / 1600 / 2560 = 2.31 / 2.16 / 1.96):
`LedgerRow.OneLineHeight` + the Budget rows' 10 px gap; the name at full weight through `MeasuredLabel`
with priority on width (the fixed cells give ground toward their floors; the category token steps out only
where the floors cannot carry the widest name at the guard floor — the R-K6 shape). Density on film: 3 → 5,
5 → 8, 7 → 11 laws per viewport. Guards silent, `ScreenEdgeCheck` 0 clipped ×4. R-C8 rode it: the courtesy
note's convergence paragraph (unsent), the rulings doc's dated line, §V's row.

**Phase 2 — the raster budgets (R-C2, `283e4ba`).** The nine inspected by eye with mismatch masks and all
90 pairs measured: every over-budget pixel was EDGE (antialiasing along a stroke silhouette; the share
tracked perimeter). `StripCutDiffCheck` now asserts STRUCTURE ≤ 1.0 % of the canvas and EDGE ≤ 2.0 per
silhouette-boundary pixel, each with its reason in the header (rule 6). Re-sweep 86 of 90 within budget,
3 text-bearing, 1 unrasterizable, 1 FAILED — the hatch tile (§E5), as it should. The flat 2 % is gone; no
new Design ask.

**Phase 3 — the capture states (R-C6, `548a558`).** Seven UI-state-only captures in the driver's main
sweep — the trace panel on Policy/Laws for its three sections, a Policy Web policy node and a stat node
selected, the signing ceremony mid-entrance and settled — the no-policy trajectories byte-identical to `traj_post_omnibus` 6 of 6 (SHA-256 B14824EB / 4B936887 / C9E4F01F / B66B19A5 / 5AB4658A / 6D00383D at both seeds, all three horizons) — a capture state may pose the UI, never move the model; §V's two
⚠ rows gone.

**Phase 4 — the §B debts (R-C5, `e08c8c0`).** The AHD HM1.3 workbook reached on the OECD file host; the
three homeownership estimates replaced by same-basis household figures (Sweden 58.2, Italy 75.2, Poland
84.7, each inside its 95 % band) and France 58.5 → 58.6; the anchors' vintage recorded (the 2024 column,
Switzerland 2023); the real-wage set recorded on one basis (Taxing Wages 2.1 + national CPI, derived,
nothing seeded). The sim-math bar on the seed change: 41 of 42 fields byte-identical on all six diffs (`Homeownership` the one mover, by the seed delta), equivalence 117/117, round-trip 12/12, parity 7/7, the matrix 30/30 like-for-like with not one tuple different.. §B tombstoned.

**Phase 5 — the gate and the records (the records commit following `e08c8c0`).** closing sets USA 73/73 and Sweden 71/71, guards silent, 0 clipped; seven of the eight checks green and `UpstreamCheck` flagging 52 unpushed commits (the push Elias's). §V regenerated as the final review
checklist (every row on film, every row naming its capture).

**Consumed rulings:** R-C1 (Phase 1), R-C2 (Phase 2), R-C3 and R-C4 (Phase 0, confirmations), R-C5 (Phase
4), R-C6 (Phase 3), R-C7 (Phase 0), R-C8 (Phase 1). **Shelved:** nothing. **Deviations, stated:** the
category token steps out of the one-line row by width where the floors cannot carry the widest name (the
R-K6 shape); the derived real-wage set does not reproduce the press-cited Taxing Wages figures and is
recorded, not adopted; the kickoff's "Poland's housing clamp at 90" was read as §F's caveats 5 and 6 as
written. **RULINGS NEEDED:** none — only Elias-in-person items remain (the sends, §V's eyes, §P's play,
13 September).

## 38. The clear-out kickoff — the riders, the Reset click, the push, the send package, the §V index, the playtest saves, the prereqs file live-only (2026-08-28)

**Authority:** Elias's clear-out kickoff of 2026-08-28 ("clear out the rest"): finish what a session can
finish and reduce what is physically Elias's to single gestures. Pre-issued rulings R-D1 (the push — the
flagged assumption, not struck), R-D2 (the Reset click), R-D3 (the deferral), R-D4 (§P staging, bounded),
R-D5 (the §V index). Explicitly not swept: the trigger-queued items whose triggers have not fired. Anchor
`2f42deb`; Working Discipline v2; one unit, one commit, staged by path. Detail: `CLAUDE.md` "The clear-out
kickoff (2026-08-28)".

**Phase 0 — the two riders (`4df1dbc`).** The rulings doc's 2026-08-28 line now reads "this board's 32 px
one-line pitch (~27 rows per screen)", the figure per `a7d877d`'s derivation (the "~27 px" was the omnibus
report's misreading, propagated by the session). R-D3: `StripCutDiffCheck` carries a `DeferredPairs` table
by name — `ui_hatch_draft` with its dated §E5 pointer — measured and printed, marked DEFERRED, never a FAIL;
any other pair over budget still fails; the header says the deferral dies the day Design's §E5 answer
lands. Re-run: exit 0 — 86 of 90 within budget, 3 text-bearing, 1 deferred, 1 unrasterizable, 0 FAILED.

**Phase 1 — the Reset click, draft-only (R-D2, `4e44777`).** "Reset draft" returns the partner's dial to the
standing override through `ResetPartnerTariffDraft` and writes nothing live; the override's rate moves only
through the Trade bill — a cut voted like a rise. Set Override unchanged (inert at the effective rate). The
driver films the pair in the main sweep (`06m` draft moved +10, `06n` draft reset beside the override still
active), assert-own-name on the reset. Bar: `clear_p1c_*` 75/75 at all four sizes, guards silent, 0
clipped; the no-policy trajectories byte-identical 6 of 6. The roadmap's queue entry tombstoned with its
date; the alternative (the click files a reset bill) recorded as one routing change away.

**Phase 2 — the push (R-D1).** `git fetch origin` — the remote tip `e86c79dc9819c11e9ca4e843a79894de9e9c6ace` (2026-08-26, "The second law category ships"), an ancestor of local HEAD with nothing on `origin/main` unknown to local history (`HEAD..origin/main` empty; 55 commits to push, the post-rewrite line intact); `git push --force-with-lease=main:e86c79dc9819c11e9ca4e843a79894de9e9c6ace origin main` → `e86c79d..4e44777  main -> main`; `git fetch origin` again → `origin/main` = `4e4477755f90572d0c862a6087ed7656ccb876fb` = local HEAD, CONFIRMED. No credential prompt (the credential manager held the token; `GIT_TERMINAL_PROMPT=0`, `GCM_INTERACTIVE=never` so a prompt would have failed fast, not hung), no lease rejection, no retry. `UpstreamCheck`'s convention ("the push is Elias's") was amended for this one push by this kickoff and is recorded here; the four commits after it (Phases 3–6) are local and under the check's own 10-commit threshold — the convention is back with Elias.

**Phase 3 — the send package (`d30eb1a`).** `SEND_PACKAGE_2026-08-28.md`: the note and the request doc through
§E5, each with its SHA-256 as on disk and its destination path; the readback-hash glance; what comes back
and where it lands (the import commit that removes the deferral). Sending stays Elias's.

**Phase 4 — the §V index (R-D5, `076273a`).** `Tools/sv_index.ps1` generates `../PoliSim-captures/sv_index.html`
— one section per checklist row, every capture at every size linked, the 1600 one previewed; the
checklist's own shorthand read as written; a token that matches nothing is listed, not dropped. Tooling
in-tree, output out of tree, nothing binary committed.

**Phase 5 — the playtest saves (R-D4, `8c7081b`).** The driver's `-shotsaves` mode stages the three saves
through the real service on the warmed-up game before the sweep's harness drafts go in, each filmed once:
`playtest_1_trade_bill_costs` (USA), `playtest_2_riksbank_rate_decision` (Sweden — a rate decision drafted on the Riksbank tab, option C's naming being the verdict; no appointment can be pending on `main`, Riksbank-B's machinery ships with item 10), `playtest_3_dense_midgame`
(USA). §P is load-play-judge.

**Phase 6 — the gate and the records.** the eight armed checks green — delivered assets 0 missing, importer settings 148 sprites with 0 errors, stat icons 19 of 19, portraits 25 of 25, area icons and emblems 14 of 14, chrome 50 of 50 in both directions, the party-mark check verifying nothing by design, and `UpstreamCheck` exit 0 (four local commits, under its 10-commit threshold); the strip-cut sweep green with its one named deferral (Phase 0); the touched screens captured and edge-checked per unit (`clear_p1c_*` 75/75 at four sizes, 0 clipped); the no-policy trajectories byte-identical to `traj_cont_p4` 6 of 6 (Phase 1 moved no byte); ATTRIB 0 throughout. `MISSING_PREREQUISITES.md` re-derived to LIVE-ONLY — every
tombstoned section's body migrated verbatim to §38a below; what waits: the paste (§S), the coupling queue at
its triggers (§A), item 10 and its riders (§D), Design's §E2/§E4/§E5, §V's sitting, §P's play.

**Consumed rulings:** R-D1 (Phase 2), R-D2 (Phase 1), R-D3 (Phase 0), R-D4 (Phase 5), R-D5 (Phase 4).
**Shelved:** nothing. **Deviations, stated:** the button's second face is labelled "Reset draft" (the
honest label for what it now does); the R-D1 push covered Phases 0–1 as ruled — the commits after it are
local and under `UpstreamCheck`'s own 10-commit threshold, the convention back with Elias. **RULINGS
NEEDED:** none.

### 38a. Tombstones migrated from `MISSING_PREREQUISITES.md` (2026-08-28, verbatim — the prereqs file is live-only from here)

**The omnibus pass's `RULINGS NEEDED` (2026-08-28) — ✅ ALL THREE RULED 2026-08-28 by the continuation
kickoff (pre-issued rulings, each strikeable in the kickoff message; none struck). Tombstone:**

- **A4 → R-C1: build the one-line row type.** "The two-line row is a construction artifact; the boards drew
  one-line rows and density was the original finding." Both law-row families go one-line (AVAILABLE
  three-cell per 1j, IN FORCE / BEFORE THE HOUSE four-cell per 1i), the height derived from the board's
  proportion (the 1i board: rows on a 32 px pitch at 1080p, ~26 in its scroller, a 14 px bold name — 2.29
  name-fonts per row) translated to our px basis through the ledger conventions; the name cell keeps full
  weight with `MeasuredLabel` shrink-never-truncate; the detail pane untouched. Built as the
  continuation's Phase 1 with the density measured before and after — record: `COMPLETED.md` §37,
  `CLAUDE.md` "The continuation kickoff (2026-08-28)". (The 2026-08-28 report's "~27px" was a misreading
  of the rulings doc's "~27 rows per screen"; the board's row is 32 px.)
- **A5 → R-C2: keep the 2% budget; inspect the nine.** Eye-read the nine `Stats/` diff pairs, classify the
  damage per pair, then set per-damage-class budgets in `StripCutDiffCheck` from what the inspection
  finds, each with its reason in the check's header (rule 6); re-run the 90-pair sweep; a pair that turns
  out to be a real Design-side defect joins §E5 as an ask, not a budget. "Raising a blind bar to clear
  observed failures is the rule-14 shape — do not do that." Executed as the continuation's Phase 2 —
  record: `COMPLETED.md` §37, `CLAUDE.md` "The continuation kickoff (2026-08-28)".
- **A6 → R-C3: the anchored seed-spread form is CONFIRMED** with the sourced seeds in — "sourced seeds
  move what the countries are, not what they do unprompted." Recorded where the anchor is documented
  (`CLAUDE.md` "Playtest 3, the rulings" §1, the dated confirmation line); the live-deviation revert note
  stays there as the recorded alternative, unexercised.

#### B. Database access — ✅ the three quality debts SETTLED 2026-08-28 (R-C5; tombstone)

**Every figure that blocked a batch was sourced 2026-08-02** — the sourcing history is `COMPLETED.md`
§23; the values, queries and status flags are `POLISIM_SEED_DATA_MACRO_OVERHAUL.md`. The three quality
debts that survived were attempted by the continuation kickoff's Phase 4 under the API cross-check gate
(same-basis or the debt stands — no invention, no basis-mixing) and all three settled:

| Debt | Outcome (2026-08-28) |
|---|---|
| **The real-wage row mixes THREE bases** (seed §5) | A same-basis set for all six recorded — Taxing Wages 2.1 (single worker, 100% AW, GEBT and NIAT) deflated by the national CPI, both in SDMX with every dimension stated; **derived, nothing seeded** (R4-2: the index opens at 100). It does not reproduce the press-cited figures for Italy/Germany/France, and says so |
| **The AHD vintage behind C1's estimates is unrecorded** (seed §1) | Found: the anchors are the HM1.3 workbook's 2024 column (Switzerland 2023), confirmed to the digit; the workbook itself is reachable on `webfs.oecd.org` |
| **Three homeownership figures are `[ESTIMATED]`** (seed §1) | Replaced by the same-basis OECD household figures from that workbook — Sweden 58.2, Italy 75.2, Poland 84.7 (each inside its 95% band); France 58.5 → 58.6 (the file's 58.56). **A seed change on four countries: the sim-math bar ran** — the continuation's Phase 4 record |

What a database session still owes: the §F seed spread's `[PROVISIONAL]` → `[VERIFIED]` upgrade (seed §8).

**Standing rule, three-for-three (kept live here — it governs any re-sourcing):** for any cross-country
statistic, **assume an undocumented variant axis exists** and record the basis alongside every value —
indicator code, population base, threshold, year. Housing overburden had 8 variants where its warning
implied 3; youth unemployment 4 where it implied 2; homeownership 4+ with no warning at all. A bare
number is unfalsifiable later.

#### C / D2 / E1 / F — tombstones (closed sections, migrated 2026-08-26)

- **C — visual review:** empty since 2026-08-02; all eleven items confirmed. Record: `COMPLETED.md` §16.
  (Its successor is §V below — a different list, the same supplier.)
- **D2 — Round 4 scoping:** released 2026-08-02; the arc closed 2026-08-17. Record: `COMPLETED.md` §19.
- **E1 — `icon_stat_interestrate`:** delivered the same day it was recorded as awaiting. Record: `COMPLETED.md` §15.
- **F — Step C4's closure:** ✅ **CLOSED 2026-08-17 — the F register's count is ZERO.** The closure
  chain, the 1,416 → 19 measurement table and the double-count fix: `COMPLETED.md` §23.

#### D1 — cabinet portraits: ✅ CLOSED 2026-08-27 (tombstone)

**Delivered the same day the verdict was sent** (`PoliSim v2 Design Progress5.zip`, `HostUrl=https://claude.ai/`),
verified on the four-pack bar and imported: 8 PNG + 8 SVG, every name spelled as derived from
`CabinetSystem.CandidatePool` (0 missing, 0 unexpected), every PNG 512×640 full-colour opaque (the
Portraits class), hand-written metas on the PoC's own template with collision-checked GUIDs — and
**verified by loading, not by finding: `PortraitCoverageCheck` (new, in the suite) resolves 25 of 25 pool
members through `IconLibrary`'s own accessors**. The cabinet set is complete: 18 of 18 ministers + 7 Fed
chairs. Record: `COMPLETED.md` §24 (the seventh request's answers) and CLAUDE.md "Progress5". **Not
done in the sense that matters for a portrait:** none of the eight has been in front of Elias — the
roster with the batch on it is a §V item. The history (portfolios authored R4-4 → request sent 08-17 →
the PoC → the register gate 08-26 → the send 08-27 → delivery 08-27) is `COMPLETED.md` §24.

#### E3 — the rasterization diff, our half: MOVED TO THE ROADMAP (tombstone, 2026-08-27)

**Ruled by Elias 2026-08-27: this was never Design's to supply.** Design's half closed 2026-08-17 (they
re-rasterized the six per-state button PNGs fresh from SVG and pixel-diffed 6/6 identical — the
Progress2 manifest); our half needs a tooling pass that makes Unity's vectorgraphics
`RenderSpriteToTexture2D` path produce pixels under the batch harness, or a rasterizer installed on this
machine. Neither is a named external party, so under this register's own admission test it is startable
work — **`POLISIM_MASTER_ROADMAP.md` live item 7** carries it with `StripCutDiffCheck`'s finished compare
machinery and the 2026-08-26 attribution correction. A prerequisite filed under the wrong supplier is
one that lapses, because nobody on either side is waiting for it — the reason this tombstone exists.

#### F — the session-sourced seed spread: ✅ CONFIRMED 2026-08-28 (tombstone)

**R-C4 of the continuation kickoff (Elias, 2026-08-28): the mapping and the six caveats are CONFIRMED.**
The whole sourcing record — sources, SDMX keys, the two tables, the six caveats as basis notes under the
variant-axis rule, and two standing notes (a re-source trigger for the day SOCX publishes a post-pandemic
common year; the childcare-clamp compression, known and accepted) — now lives where seed data lives:
`POLISIM_SEED_DATA_MACRO_OVERHAUL.md` §8. The slot tags in `WorldFactory.cs` read "mapping confirmed by
Elias 2026-08-28"; the `[VERIFIED]` upgrade path remains the §B database session. The trajectories were
byte-identical through the sourcing (the anchored form, confirmed as R-C3); nothing downstream moved.
History: the ruling of playtest 3 (finding 1, option (i)); R-K9 of the omnibus (`915c800`, this section's
own proposal followed as written); the confirmation. The section's sourcing text as it stood at `dc6f491`
is in git history.

**The ruling (playtest 3, finding 1), kept for the record:** option (i), a per-country seed spread for sector
regulation and implemented welfare programs, from real data per the standing rule — "do NOT invent a
spread to make the plot look good; if the figures need sourcing, say so and Elias will source them."

**Still flagged, not this section's to decide:** the same uniform-50 finding holds for the other four
sector dials and the labor/crime dials the seed file lists as uniform placeholders; the Compass Y formula
averages generosity over IMPLEMENTED programs, so a country with one generous program outranks a broad
welfare state.

## 39. UI v3.0 Phase A — the census, the shell and the rail, the instrument inventory, the ask (2026-08-28)

**Authority:** Elias's `Direction.zip` of 2026-08-28 — `POLISIM_UI_V3_DIRECTION.md` (rulings V3-R1…V3-R4; the thesis: *the desk with fewer words, not a different desk*) and the Phase A kickoff (census · shell · inventory · the ask; The Desk itself deliberately not built — the board comes first). Anchor `29801d0`; Working Discipline v2; one unit, one commit by explicit path. Detail: `CLAUDE.md` "UI v3.0 Phase A (2026-08-28)".

**Phase 0 — the direction installed (`c90da2d`).** `POLISIM_UI_V3_DIRECTION.md` at the repo root (CRLF); the roadmap's v3.0 era header with Phases A–C and the item-10 fallback as written there; the zip and the kickoff archived out of tree so `DeliveredAssetCheck` reads no unfinished delivery.

**Phase 1 — the census and the (c) cut (`23cbb84`).** Every text element on the landing screen (Statistics › Domestic) and the OPEN chrome column, from the film, with content, px size at 720/1080, role and class: **65 element kinds — (a) 44 · (b) 18 · (c) 3 kinds (four text elements)**. The pure (c) died at once (the direction's rule): the collapsed tab guide (a button and a paragraph naming ten tabs that no longer exist), the "not a guarantee" hedge, the "compare against" instruction. Every (b) waits for the board. Bar: `v3p1_1600` 75/75, guards silent, 0 clipped. The table is Annex A of the request.

**Phase 2 — the shell and the rail (V3-R2, `8e162b1`).** `ShellFoldState` OPEN/FOLDED, persisted per save as the player's per-screen overrides (`UiDraftState.ShellFoldOverrides`, old saves → defaults), instant flip, per-screen defaults (the landing screen and the Budget ledger FOLDED, everything else OPEN), the toggle in both states ("‹" folds, "›" unfolds; the strip's right end when open, the rail's bottom cell when folded). The rail: six navigation cells with the tongues' own icons (active in area ink behind a spine, inactive in the tab-swatch tint), the calendar chip from the pad's own materials, the status dot carrying B8's two states (HELD amber with the spec's glow; RUNNING green), the toggle — nothing else; its measure derived from the icons' 24-unit grid (`cell = icon × 44/24`: 55 px at 1080p, 39 at 720p, 64 at 1440p) and every cell asserted inside the rail. Budget full-screen reconciled: its column-hiding is the shell's FOLDED state, its interrupt banner now the folded frame's on every screen — **and locked FOLDED (R-A1)**, because its OPEN state is not legal (the smoke film: category labels wrapping mid-word, twenty containment escapes at 1600) and making it legal is the deep-screen redesign the direction rules out; the toggle wears the disabled face there. The harness sweeps the other state on every screen (guards in both) and films one pair per column-layout class (standard: the landing screen unfolded and Parliament folded; Canvas: identical by construction; Budget: one state, said in the log). Three defects the film found were fixed on the way: a tile value printing the skin's pale hover ink under the cursor (`PoliSimWidgets.Sized` inks all four states now); the rail's width narrowing the Budget ledger at 1280 past two of its columns' floors — the label-clipping class's instance #15, closed the class's own way (`LedgerRow`'s figure column asks what it holds; the name cell gains §A.9a's missing rung, two lines at a reduced size before one line at the floor); `ScreenEdgeCheck` reading the desk grain's speckles as a flush edge once the folded frame had no tongues to mask them (flushness is now the longest contiguous run, the measurement in its header). **Bar:** 1280 / 1600 / 2560 — 78 captured each (75 states + 3 pairs), 0 text overflows, 0 containment escapes, 0 canvas text violations, `ScreenEdgeCheck` 0 clipped (78 / 79 / 78); 1920 — the same on the re-run (78, 0 / 0 / 0, 0 clipped) after the first attempt died in a sporadic Editor crash on Mono's finalizer thread after nine captures (crash report `Crash_2026-08-28_114730838`; the code it ran passed at the other three sizes); ATTRIB 0 throughout; the eight armed checks green (delivered assets 0 missing from 0 root zips, importer settings 148 sprites 0 errors, stat icons 19 of 19, portraits 25 of 25, area icons and emblems 14 of 14, chrome 50 of 50 both directions, the party-mark check verifying nothing by design, `UpstreamCheck` exit 0); trajectories IDENTICAL 6 of 6 (SHA-256 `80E0C878` / `85135B71` / `35C0F578` at seed 777, `57C77CC4` / `A76586BC` / `A86A81F7` at 424242; the dump ran on the Phase 2 code, every later edit being UI, harness or editor).

**Phase 3 — the instrument inventory (`5443342`).** A harness-only ladder (`GameController.DrawInstrumentLadder`, `-shotladder`) filmed twenty candidate instruments at descending sizes at 1280×720 and 1920×1080, each rung by its own renderer on live data with its size captioned; the minimum legible size is read from the film and the break stated per row (Annex B of the request). Said plainly: no approval *face* exists (the attribution is a thirteen-row ledger, no gauge by its own ruling), the sparkline carries no 1l weights (those are the graph's), the map is six illustrative nodes, the compass emits its axis captions outside its rect, the hemicycle and the pie are constants that do not shrink. Measured minima, in brief: map 360 wide with names (nodes alone 120); compass 240 (dots alone 90); web 240 (ring alone 180); sparkline 36×10; chip strip 280; tile scale 0.8; graph 240; attribution ledger 360; calendar sheet 420; chip cell 32; sprite stamp 68×20; procedural chip 9-px type; lamp 8 px; stepped rule 32×8; flag 24×16; icons 12 px; ledger row 10-px type; bars 40×14 — the breaks stated per row in Annex B.

**Phase 4 — the ask (`afd95bc`).** `CLAUDE_DESIGN_ASSET_REQUEST.md` §1: two boards — "Screen 0, The Desk, folded" and "the rail" — at 1280×720 first, against the three annexes, with the direction's hard constraints and the three deviation conventions stated; `SEND_PACKAGE_2026-08-28.md` regenerated so one send carries §E5 and the v3 ask with the annex captures, the courtesy note unchanged from its recorded hash. Sending is Elias's.

**Phase 5 — the gate and the records.** The gate, sized to what shipped (UI, harness, editor; no simulation change): the `v3a_*` family at four sizes — every screen in its default fold state plus the three fold pairs, 78 per size, the two text guards and the containment guard silent in BOTH states (the harness sweeps the other state on every screen), `ScreenEdgeCheck` 0 clipped at all four under its run-length rule; the ladder films at two sizes (23 each, 0 failed, exit 0); the trajectories byte-identical to `traj_cont_p4` 6 of 6; the eight armed checks green; ATTRIB 0 throughout; the sporadic 1920 crash re-run clean. The §V index regenerated: 24 rows, 205 `v3a` links, 0 tokens unmatched. §V gains the shell's rows (and the clear-out's merged R-D2 row is unmerged); the roadmap re-derived — Phase A closed, Phase B WAITING on Design's boards, Phase C queued behind it.

**Consumed rulings:** V3-R1…V3-R4. **Reversible calls, one line each:** R-A1 the Budget lock; R-A2 the rail's order (navigation top, the strip's three bottom); R-A3 the toggle as a glyph pair; R-A4 the folded banner's wording; R-A5 the fold keyed by sub-screen except Budget; R-A6 the film-found fixes taken in-pass (the hover ink, the chip's band, instance #15, the edge check's rule). **Shelved:** nothing. **Deviations, stated:** no Budget fold pair on film (R-A1). **RULINGS NEEDED:** none.

## 40. The stage-prep micro-pass — the push amendment, "every reachable state", the second sparkline, the compass's footprint, the map's names on the ladder (2026-08-28)

**Authority:** Elias's kickoff of 2026-08-28, five rulings pre-issued (R-SP1…R-SP5), anchor `e605b25` (tree clean, `main` eleven ahead). **Commits:** `86d9c35` Phase 0 · `a69b2be` Phase 1 · `373ea07` Phase 2 · `c9c3c05` Phase 3 · the records commit Phase 4. Not in scope and not touched: the Desk, the approval face, the sends, the saves, deep-screen redesign, 13 September.

**Phase 0 — the two standing amendments (`86d9c35`).** *R-SP1, the push:* sessions push at pass end, fast-forward only — `git fetch origin`; `origin/main` must be an ancestor of HEAD; `git push origin main` with no force flag of any kind, ever, from a session; re-fetch and confirm `origin/main == HEAD`; any other state (non-fast-forward, a lease or credential surprise, anything that would want `--force*`) stops the session, which hands Elias the exact state — force stays exclusively his. `UpstreamCheck` stays armed as the tripwire (`WarnAheadOf = 10`, now a threshold a session clears rather than a message to Elias). Recorded in the discipline's rule 5 (superseding R-D1's one-push scope), the roadmap's gestures (five, no sixth) and the check's own doc. *R-SP2, "legal in every reachable state":* V3-R4's "legal in both states" now reads legal in every state a player can reach; locking a state (R-A1's Budget precedent) is a legitimate way to make one unreachable, recorded per screen, and the harness sweeps and films only the reachable states. Recorded in the direction doc (the fold bullet and a dated note under V3-R4), the spec's §A.5 and the roadmap.

**Phase 1 — R-SP3, the second sparkline (`a69b2be`).** The strip's chips (`PolicyScreenStatsRenderer.DrawChip`) draw through the one renderer the Statistics graphs use — `GraphRenderer.DrawSparkline` → `BuildSparklinePixels` — which has carried R-G4's floor (`thickness = max(2, round(rectHeight/34))` device px) since the omnibus; there is no bypassed path, so no call site moved. Verified on film with the eye pair the ruling asked for (`sp1_1280_06a_policylaws_labormarket` beside `sp1_1280_02a_statistics_domestic`, and the same at 2560): the strip's line 2 px at both sizes, the graph's 3 px at 2560 and ≈2 at 1280 — the same law at two rect heights. The request doc's Annex B I4 row, which had claimed no 1l weight reached the strip, was corrected and the package digest regenerated.

**Phase 2 — R-SP4, the compass's footprint (`373ea07`).** `PoliticalCompassRenderer.Footprint(countries, plotSize, availableWidth, labelStyle)` declares the honest rect: the plot square plus its caption band at the width the two range captions need (single-line where the width allows, wrapped where it does not — never shrunk, the ruling's forbidden form), and `Draw` lays the plot at the rect's top-left with the captions inside the rect beneath it and containment-asserts all three (`Compass plot`, `Compass caption X`, `Compass caption Y`). The Compass tab reserves the footprint (`CompassScrollGutter` 18 px beside it for the scroll view's own bar) and the ladder's compass rung reserves and captions it (`{w} plot, {W}x{H} footprint, type {size}`). Measured on the closing film: widths 350 / 451 / 526 / 651 px at 1280 / 1600 / 1920 / 2560 (the captions' single-line need at each type size; the plot 280 / 372 / 424 / 520 by `clamp(0.4·h, 260, 520)` on the 699 / 929 / 1059 / 1419 client heights); the height at 1600 measured 428 (372 + a 56 px band), at 2560 ≈ 597 read on the film (520 + the band), and at 1280 the plate runs past the sheet's first page — the Y caption is on `sp4_1280_07b_politics_compass_rows`, the guards silent, because the captions are inside the declared rect and the rect scrolls. On the ladder: `480 plot, 480x528 footprint, type 16` · `360 plot, 360x400, type 12` · `240 plot, 240x278, type 8`.

**Phase 3 — R-SP5, the map's names (`c9c3c05`).** §E6 read first, verbatim from the register: *"§E6 — the v3.0 Phase A boards ("Screen 0, The Desk, folded"; "the rail") | Claude Design — two boards at 1280×720, once the package is pasted | the request doc §1 with Annexes A–C; v3.0 Phase B builds against them"* — the boards, not the map collision, so the ladder branch. `MapRenderer.PlaceLabels` settles the six names left to right: a name whose rect comes within `MinLabelSeparationPx` (4) of another label or node takes its ISO 3166-1 alpha-3 code (`Iso3Codes`: USA SWE DEU FRA ITA POL — a standard identifier), one that still collides shrinks to the width that clears (floored at the guard's 8 px), one that still cannot is recorded (`LastLabelViolation`) and left on its node's row — no nudge algorithm exists. `LastMinLabelSeparation` / `LastLabelRung` / `LastLabelRects` are what the last `Draw` laid down; `UiScreenshotDriver.AssertMapLabelSeparation` reads them after `02b_statistics_international`'s capture and a gap under the floor is a `LogError` the run's fold turns red on; the ladder's map rungs caption `rung N gap G`. Measured on the screens (`sp4_*`): 19.5 / 25.8 / 22.8 / 17.7 px at 1280 / 1600 / 1920 / 2560, every name at rung 1. On the ladder film (`sp4_ladder_1280_ladder_map`): 480×288 rung 3 (Germany → DEU) gap 5 · 360×216 rung 3 gap 11 · 240×144 rung 4 gap 0 — the instrument's floor with names is 360×216, now measured by the renderer rather than by eye, and below it the ladder bottoms out and the film says so. The unit did not stop: the assert is on the screens, which clear at every size; the failing rungs are the measurement the ruling asked to have reported. Annex B's I1 row corrected (the first filing's "United States / France touch at every size" was the ladder's small rungs, not the screens); the package digest regenerated.

**Phase 4 — the gate.** `sp4_*` (USA) at four sizes: 78 captured each, 0 text overflows, 0 containment escapes, 0 canvas text violations, `ScreenEdgeCheck` 0 clipped, ATTRIB 0. The 1920 step stalled nine minutes mid-run (the log silent 14:39:03–14:48:34 with two `Unity.exe` alive) and then completed clean, 78/78 — environmental (the licensing client's web request, the class memory item 17 names), not re-run. Ladders `sp4_ladder_1280` / `_1920`: 23 kinds each; the overflows are the v3a film's known smallest rungs (the trace's `· unemployment above NAIRU` at the 8-px floor, the chip's day numeral at the 24-px cell) plus, at 1920, an event row label (`Global Commodity Price Spike`) at the trace's smallest rung — the same class, reported not gated. Trajectories `traj_sp4_*` ≡ `traj_v3a_*` 6/6 by SHA-256 (rendering only moved). Seven checks green; `UpstreamCheck` 15 ahead of `origin/main` above the 10 threshold — the tripwire R-SP1's push clears at this pass's end. Earlier in the pass: the sp3 1600 bar exited 1 through the error fold catching a Unity Connect token-exchange web error (the licensing client, once; the run's own counters clean) — recorded, re-run by the closing matrix. §V gained the compass-footprint and map-ladder rows; `sv_index.html` regenerated (26 rows).

**Rulings consumed:** R-SP1…R-SP5. **Reversible calls, one line each:** R-SP4a `CompassScrollGutter` 18 px reserved beside the footprint on the Compass tab; R-SP5a the ISO 3166-1 alpha-3 codes as the abbreviation rung; R-SP5b the label right of its node, vertically centred (the old fixed rect's convention kept); R-SP5c a name that cannot clear at the floor stays on its node's row and is recorded, never nudged. **RULINGS NEEDED:** none. **The push:** R-SP1's first application at this pass's end, the outcome in the pass report.

## 41. UI v3.0 Phase B — Screen 0, The Desk, built against board 1m; the rail re-skinned against 1n; §E5 answered (2026-08-28)

**Authority:** Elias's instruction of 2026-08-28 — import the Design project (`b3dec27b-620b-452a-9783-e8317cbec4d9`, "PoliSim v2 Design Progress") through the claude_design MCP and implement `PoliSim v2 Screens.dc.html`. Read the same day, the file carried **two new boards and the §E5 answer**: 1m *"Screen 0 — The Desk, folded · v3.0 board one"* (drawn 2026-08-28 against Annex A's census and Annex B's minimums at 1280×720, seven deviations declared, the (b) resolutions named, no gap costed), 1n *"the rail · v3.0 board two"* (a re-skin — "the derivation untouched, only the air moved", deviations none, gaps none) and *"§E5 answered — the two rasterization-diff findings"*. The project's `uploads/` also showed Elias's paste had landed (the request doc in five revisions, the send package, the direction and kickoff). Anchor `c8c8a61` (the stage-prep pass, pushed). The direction's Phase B, verbatim: *"The Desk built against the board; the (b)-class returns resolved; capture family `v3desk_*`."*

**Phase 0 — the boards read into the spec.** `POLISIM_V2_SCREEN_SPEC.md` §A.17 restates what the boards rule so the code can cite a line: 1m's split (D1 — the chrome (a)s land on the stage or the rail, the content rows keep their document, the stage restates only the ten headlines as the strip), its placement at 1280×720 (the sheet's inner 1118×660: masthead 26; columns 420/240/425 with 16 px gaps — map 290 over ledger 222, compass 240 over effects 272, calendar 380 over the event card's 136; the strip 10×~104×56), the seven deviations accepted as built (D1–D7), the (b) resolutions (C3 → the card's bars; C10/C13 dropped; C18 → the horizon control; C26 → the lamp; S1's labels → the icons), the conditionals (the event card while an event is live, HELD above the masthead, GAME OVER as §A.11's stamp), the text-budget audit; 1n's air (nav block top-anchored, utility block bottom-anchored, one breathing gap), its active convention (a 3 px spine, a 12 % area-ink wash), the chip's rule, the lamp's two states, the toggle's glyphs, no spine on Screen 0. The boards costed no gap; two optional notes carried (an approval dial face would be a new instrument, one further board first; the chip sparklines' weight is R-G4's already).

**Phase 1 — Screen 0 built** (`Assets/Scripts/UI/GameController.Desk.cs`, a `partial` companion of `GameController`; the class went `partial` for it). *The stage.* One paper sheet in the folded frame's content column; every instrument draws into a rect that is the board's placement scaled by the sheet's inner area's ratio to the board's 1118×660 — so at 1280×720 the stage IS the board and elsewhere its proportions; type is the board's px sizes scaled from 720 by the window height (`DeskPx`), floored at the guard's 8, captions in caps in the document face. The masthead (C6 the flag and `{COUNTRY} · YEAR {N}`; S4's `DESK READINGS · LIVE`; C28's cluster PAUSE 1× 2× 3× and SAVES as the board's bordered chips — D5); the map plate (`MapRenderer.Draw`, names at the board's 11 px on R-SP5's ladder, read-only on the stage); the approval ledger (D6 — the live approval as a hero numeral over `StatTracePanel.BuildApprovalDeskTerms`, the panel's own terms in its own names: nine non-misery, the four gaps as one total, the clamp when non-zero, the events as one total; no gauge; a "+N more terms" row where the space runs out); the compass on its honest footprint in the board's 240 square (R-SP4's `Footprint`, the captions inside the rect — D3 as the renderer already draws it); the effects card (C16's label, C17's horizons as chips 1D · 1W · 1M · the code's full-turn label, C22's eight figures from the same cached `PreviewTurn` the OPEN panel shows — each a diverging bar in the good/bad ink with its numeral formatted from the scaled figure without the per-row margin, C19's margin and C20's methodology as mono captions — D4); the 1k calendar sheet (`DrawCalendarMonthGrid` in a GUILayout island, the dated ledger's rows rect-drawn beneath at the board's 23 px pitch — C10's "This Month" and C13's empty sentence dropped on this surface); the event card while an event is live (the BREAKING chip as §A.11's urgency chip, the name, the description as its only text, C3's three effects as bars); the chip strip (S6's ten from the same list the tiles build, a sparkline through `GraphRenderer.DrawSparkline` for every reading with a history — four have none and draw none — neutral ink, D7; no area keyline); GAME OVER as the stamp over the dimmed stage with the reason as one caption. *The shell.* `_onDesk` (R-B1: a state above the six documents, not persisted — a loaded game lands on the Desk as a new one does; `SelectPlayerCountry` sets it); `ShellScreenKey` "Desk"; `DefaultShellFold` FOLDED; `ShellFoldLocked` true there (R-B3 — the column's contents live on the stage, OPEN unreachable, the toggle on its disabled face; R-SP2's form); the OnGUI branch `DrawDeskStage` before the tab switch; the folded banner drops its speed-hint clause on the Desk (R-B13). *The rail.* R-B2's ways home — the calendar chip (the sheet collapsed) and the open document's own icon clicked again; `DrawRailNavCell` reads `!_onDesk` for its active state (D2).

**Phase 2 — the rail re-skinned against 1n.** The active cell: a 12 % area-ink wash (`RailActiveWashAlpha`) and a spine at the left edge, full cell height, 3 px at the 39 cell scaled with it (`RailSpineWidthAt39`) — replacing the card-spine call; inactive the tab-swatch tint, no wash; hover the button face's own; the chip's hairline between month and day; the utility block bottom-anchored as Phase A built it (the `FlexibleSpace`). The derivation untouched.

**Phase 3 — §E5.** *The slider strip — CLOSED as Design answered:* the strip is authored raster with no SVG parent; the 24×24 pill under its name in `Source/` was the old pack's leftover and was removed; `StripCutDiffCheck.SourcelessByDesign` lists the strip with Design's account and prints it on every run (a source re-appearing under the name is a FAIL). *The hatch tile — answered, imported, diffed, still outside budget:* Design's re-export (explicit 45° stripes, stated 16 px period / 6 px duty) replaced `Source/ui_hatch_draft.svg`; the diff (resvg, `stripcut_b1_20260828_150949.log`: 86 of 90 in budget, 3 text-bearing, 0 failed) read the pair at structure 33.4 % against 1 % (down from 48.5 %). The residual measured on the shipped PNG's alpha profile along `x + y`: a 16 px period centred within half a pixel of the multiples of 16 (the phase is fine), ink ≈8 px along x (5.7 perpendicular, coverage 50.4 %); the re-export's lines sit at `x + y = 32k` — twice the period — at 6 / 4.243. The period first, the duty second; the stated intent was right, the file off by two. The deferral stays by name with the measurement as its pointer (R-D3); the one re-cut is asked in the request doc's §E5 with the figures (lines at `x + y = 16k`, perpendicular stroke ≈5.7, phase as it is). Sending it is Elias's.

**Phase 4 — the harness.** `UiScreenshotDriver`: `01c_desk` right after the running strip (the landing surface RUNNING at turn 0 — the lamp green, the cluster live, the ledger without a period, no spine); after the warm-up and the sweep's own drafts `01d_desk_held` (HELD above the masthead, the lamp amber, the faces disabled, the ten-row ledger, the sparklines), `01e_desk_event` (the card filled by setting the country's last event to `EventSystem.EventPool[0]` — "Recession in a Trading Partner", GDP −2.0 %, inflation +0.0 pts, approval −3.0 — an authored event, restored after the frame), `01f_desk_gameover` (`_isGameOver` with the election-loss reason in the exact form `CheckElection` prints it from the live turn and approval, restored after); `AssertDeskState` on each (on the Desk, locked, FOLDED — a miss is an error); the Desk left before the tab loop (a tab set by field alone would film the stage under a document's name); the locked-screen messages name Screen 0 beside Budget.

**The two films before the gate.** `v3desk1_1280`: 82 captured, 0 escapes, 0 clipped, **19 text overflows** — every one the effects card's value column at the floor, carrying the OPEN panel's per-row "(±…)" margin (`"-$4.99B (±$478M)" needs 76.8 wide in 51.0 at 8px`); by eye three more: the month grid laid out one pixel wide (IMGUI's 1×1 dummy rect on the Layout event, handed to the calendar island's `BeginArea`), the ledger rows through `LedgerRow.DrawReadOnly`'s gauge lane (a 40 px pitch, "Reversion toward 50" wrapped), the cluster and the horizon chips on the tab-button sprite faces at sizes whose 9-slice borders ate the label. Fixed: the stage's inner rect cached on Repaint for the other events (`_deskInnerRect`); the numerals formatted from the scaled figures (invariant) without the margin — C19 states it once; the rows as two measured labels at the board's 17 px pitch; the controls as bordered plates with a caption over an invisible button (`DrawDeskChipButton` — stock-off plate, brass when active, muted when disabled). `v3desk2_1280`: **82 captured, 0 text overflows, 0 containment escapes, 0 canvas text violations, 0 clipped**; by eye the board — the grid on the sheet, the ten rows, the clean numerals, the chips reading. One residue fixed after it and before the gate: the calendar ledger's pitch (the same gauge-lane height had left one row and "+4 more" under the grid) → the board's 23 px.

**Records.** `POLISIM_UI_V3_DIRECTION.md` (Phase B built), the spec §A.17, `MISSING_PREREQUISITES.md` (§E6 landed — the row retires at the next re-derivation; §E5 half closed with the re-cut ask; §V's intro and four rows for the Desk, its conditionals and the rail), the roadmap (Phase B built, Phase C the next startable v3.0 work), the request doc (§1 migrated here — below, verbatim — the status rewritten, §E5's answer and the re-cut ask), the send package's digest row for the request doc, `sv_index.html`, this section and the `CLAUDE.md` section.

**Rulings consumed:** the boards' own (D1–D7 accepted as built; the (b) resolutions as drawn). **The build's reversible calls, one line each (rule 4 — Elias strikes any):** R-B1 Screen 0 is a state above the six documents, not persisted · R-B2 two ways home — the calendar chip and the open document's own icon clicked again; no spine on the Desk · R-B3 Screen 0 locks FOLDED (R-A1's form) · R-B4 the effects card draws C22's eight (the board's debt-to-GDP and currency rows are not estimates the game holds); horizons 1D · 1W · 1M and the code's full-turn label · R-B5 the strip is S6's ten from the tiles' list, no area keyline, a sparkline only where a history exists · R-B6 the map read-only on the stage, names at the board's 11 px · R-B7 the ledger is `StatTracePanel`'s own terms (nine + misery + clamp-when-non-zero + events), no gauge, the hero numeral the live approval · R-B8 the bars' display ranges declared in code (GDP growth 3 % · unemployment 2 · inflation 2 · approval 5 · poverty 2 · participation 2 · crime 5 · net budget 2 % of GDP; the event card 5 % · 3 · 10), the fill keyed to GOOD, the direction to sign · R-B9 game over as §A.11's stamp over the dimmed stage · R-B10 layout = the board's rects scaled by the sheet's inner ratio, type = the board's px scaled by height, floored at 8 · R-B11 the rail per 1n (12 % wash, 3 px spine at 39, the chip's rule, hover the face's own) · R-B12 Statistics › Domestic keeps its FOLDED default until Phase C · R-B13 the HELD banner drops its speed-hint clause on the Desk · R-B14 §E5 as above (the re-export imported as delivered, the deferral lifted only in budget, the pill removed, the strip modelled source-less). **RULINGS NEEDED:** none — four things worth Elias's eye: R-B2's ways home (the boards name none), R-B3's lock, R-B4's eight, and the §E5 re-cut ask that travels with the next paste.

**The gate, on the code as committed (the closing bar re-run after the matrix's two residues were fixed).** `v3desk_*` (USA) at 1280 / 1600 / 1920 / 2560: **82 captured each** (the 78-screen sweep plus Screen 0's four frames), **0 text overflows, 0 containment escapes, 0 canvas text violations, `ScreenEdgeCheck` 0 clipped, ATTRIB 0**; `AssertDeskState` on the Desk, locked, FOLDED at every size; the event card staged from the pool's own first entry at every size; the map assert 19.5 / 25.8 / 22.8 / 17.7 px at rung 1 (unchanged by the pass). Trajectories `traj_v3desk_*` ≡ `traj_sp4_*` **6/6 by SHA-256** (rendering only moved). The eight checks green: `DeliveredAssetCheck` 0 missing (the two archived-pack entries for the removed pill logged `rmvd … on Design's §E5 answer`), `ImporterSettingsCheck` 148 / 0 / 0, `StatIconCoverageCheck` 19/19, `PortraitCoverageCheck` 25/25, `AreaIconCoverageCheck` 14/14, `ChromeV2CoverageCheck` 50/50 + 11 superseded, `PartyMarkCoverageCheck` verified nothing (honest), `UpstreamCheck` quiet after the stage-prep push. The first matrix, before the two fixes, is on record above: 82 at every size, 0 escapes, 0 clipped, and six overflows at 1600 and 2560 (`"0%" needs 11.2 tall in 10.0 at 9px` — the GDP chip's delta rect borrowing the plain caption's height, one pixel short; visible under the selector's scrim too, because the Desk is the surface behind it) plus `DeliveredAssetCheck` red on the removed pill's two pack entries — both fixed, the bar re-run whole. `sv_index.html` regenerated with the Desk's rows.

**The migrated ask — the request doc's §1 as sent, with Annexes A and B verbatim (2026-08-28), answered the same day:**

### 1. The eighth request — two boards for UI v3.0, Phase A (2026-08-28): "Screen 0, The Desk, folded" and "the rail"

**What this asks for, in one sentence:** two boards, drawn at **1280×720 first** (then 1600×900 if you wish) — *"Screen 0, The Desk, folded"* and *"the rail"* — designed against three annexes we supply below: the census of every text element on today's landing screen with its class, the inventory of every instrument the game already draws with its **measured** minimum legible size, and the current landing-screen captures. No sprites are requested by this ask; a gap a board proves becomes a follow-up ask, costed, never an inline invention.

**Why (the direction, one paragraph).** UI v3.0 is *the desk with fewer words, not a different desk* (`POLISIM_UI_V3_DIRECTION.md`, V3-R1). Two altitudes, one idiom: the landing surface becomes an instrument stage — full-bleed, graphical, nearly wordless — while the deep screens (Budget, laws, the statistics ledgers) stay the documents they are. Same paper, inks, fonts, sprites and stamps; your eleven boards, the 96-sprite pack and every capture carry over. The shell that makes room for the stage is **built** (Phase A: the fold — `ShellFoldState` OPEN/FOLDED, the chrome column and the tab tongues collapsing to one icon rail; the mechanism is structure and gets re-skinned, not re-architected, when your board lands). What is **not** built is the stage itself: The Desk is a board first (this ask), built second (Phase B).

#### 1.1 Board one — "Screen 0, The Desk, folded"

The landing surface in the FOLDED shell: the rail at the left (board two), the stage taking the rest — at 1280×720 that is **1229 × 691 px of desk inside the 2 % margin, of which the stage is ≈ 1149 × 691 after the rail (39 px cell + sheet padding) and the 24 px column gap**; at 1920×1080 the stage is ≈ 1751 × 1037 (rail cell 55 px). Composed from the instruments in Annex B — the world map, the compass, the approval attribution, the sparkline strip, the calendar sheet, the stamps, the stepped rule — and nothing authored: **everything on The Desk is derived, attributed and drawn.**

**Hard constraints (V3-R3, binding):**

- **The text budget is absolute:** captions at mono 9.5 (Courier Prime, the document face) and instrument labels only; no sentences, no paragraphs, no restatements. A number appears as an instrument (a dial, a bar, a rule, a sparkline) with the numeral as the instrument's label, never as a text row.
- **The census is the content list:** every class-(a) element in Annex A is required content (as an instrument or a label, not as the prose it is today); class (b) may return **only as an instrument**; class (c) never returns (it is already cut).
- **No new hues, no new fonts, no Canvas** — the eleven area inks, the semantic three, the aged paper set (`PoliSimTheme.cs`, `POLISIM_V2_SCREEN_SPEC.md` §A.3), Pagella and Courier Prime as chosen. The stage is IMGUI like the frame it folds.
- **Delivered sprites plus primitives:** the 148 sprites on disk (§0) and rule 10's procedural marks — axes, lines, dots, bars, rules, the stepped rule, the glow. A gap the board proves (a dial face, a compass rose, a stamp we do not have) becomes a follow-up ask with its cost stated; do not draw one in as if it existed.
- **Instant flip:** the fold does not tween; the board shows one state (folded) and the rail's toggle is the way back.
- **The floor first:** 1280×720 is where graphics-first pays or fails; the measured minimums in Annex B are at that size and at 1080p — an instrument placed below its minimum is a board defect, not a build problem.

**The three deviation conventions you already know, restated because they bind here:** neutral valence (no instrument may look good or bad by its shape alone — `GetDeltaColor` keys to *good*, not to *up*, and the inks carry it); no invented data (every figure on the board must be one the inventory says the game holds; a placeholder numeral is fine, a placeholder *stat* is not); IMGUI adaptations declared (a treatment IMGUI cannot draw — a runtime blur, a non-rectangular mask, a tween — is named on the board as "adapt", never assumed).

#### 1.2 Board two — "the rail"

The FOLDED chrome. Built in Phase A as an icon rail on the paper sheet the column stands on; the board re-skins it. **Required contents, exactly (V3-R2):** the six navigation icons (the tongues' own: `icon_nav_statistics`, `icon_nav_decisions`, `icon_nav_demographics`, `icon_area_fiscal`, `icon_nav_policylaws`, `icon_area_political`; the active one in its area ink behind a spine, the others in the tab-swatch tint — §A.3's third column), the calendar chip (the pad's own materials: month and day numeral), the status dot carrying B8's two states faithfully (HELD amber **with the glow**, `0 0 6px rgba(212,167,44,.7)`; RUNNING green, no glow), and the fold toggle. **Nothing else.** The rail's measure is derived from the icons' own 24-unit grid — a cell is the grid plus 10 units of air each side (55 px at 1080p, 39 px at 720p, 64 px at 1440p) — so the board may move the air, not the derivation. The built rail is on film in Annex C (`v3a_1280_02a_statistics_domestic`, `v3a_1920_…`) so you draw against the thing that exists.

#### 1.3 The annexes

- **Annex A — the census** (below): every text element on the landing screen and the OPEN chrome column, with content, px size at 720/1080, role and class, counted.
- **Annex B — the instrument inventory** (below): renderer, what it takes, data and honesty class, whether it stands alone, and the minimum legible size **measured on film** with the break stated.
- **Annex C — the captures** (in the send package, `captures/`): the landing screen in both shell states at 1280×720 and 1920×1080 (`v3a_<size>_02a_statistics_domestic` folded — the default — and `…_open`), the rail as built, the ladder films behind Annex B (`v3a_ladder_<size>_ladder_<kind>`), and for reference the OPEN chrome column's own text (`v3a_<size>_02a_statistics_domestic_open`, `_rows`, `_deep`).

**What comes back and where it lands:** two boards on the live screens file (1m, 1n, in your numbering) or as PNG at 1280×720; any gap costed in a line under each. The day they land: Phase B builds The Desk against board one (`v3desk_*` capture family), the rail is re-skinned against board two, every (b) in Annex A is resolved as an instrument or dropped, and this section migrates to `COMPLETED.md`.

#### Annex A — the census: every text element on the landing screen (Statistics › Domestic) and the OPEN chrome column, from the film (`clear_p1c_1920_02*`, `clear_p1c_1280_02_statistics`; code at HEAD `23cbb84`)

Classes (the direction's taxonomy): **(a)** load-bearing · **(b)** restating an instrument that exists or could · **(c)** decoration. Classification honesty: unsure between (a) and (b) is (a). Sizes are the rendered font in px at **1280×720 / 1920×1080** (the two ends of the film; 1600×900 and 2560×1440 lie between and above — every style is `Screen.height`-derived and clamped, `GameController.RescaleStylesToScreen`, widget type at `clamp(h/1080, 0.6, 1.5)` × the theme constants). "×n" = the element repeats.

| # | where | element (content) | px 720 / 1080 | role | class |
|---|---|---|---|---|---|
| C1 | chrome · top banner (only while an event is live) | `BREAKING: {event name}` | 20 / 30 | event headline | (a) |
| C2 | | the event's description sentence | 16 / 24 | narrative | (a) — the event's only text |
| C3 | | `Effects: GDP ±x.x%, Inflation ±x.x pts, Approval ±x.x` | 16 / 24 | three deltas as a sentence | (b) — an event stamp with three instruments; the map's event dots already carry the event |
| C4 | chrome · (only at game over) | `GAME OVER` | 20 / 30 | state | (a) |
| C5 | | the game-over reason | 16 / 24 | | (a) |
| C6 | chrome · calendar sheet | `{Country} - Year {N}` | 23 / 35 | country name + elapsed turn | (a) — the name has no other home in the chrome; "Year N" is the turn count, not the pad's calendar year |
| C7 | | `JANUARI 2029` (MMMM yyyy, OS locale) | 23 / 35 | the month page's own label | (a) |
| C8 | | weekday abbreviations ×7 | 13 / 19 | instrument labels | (a) |
| C9 | | day numerals 1–31 (spent days struck; today tinted; up to four area dots) | 15 / 23 | instrument | (a) |
| C10 | | `This Month` | 23 / 35 | section header | (b) — the page above names the month and the rows date themselves (the playtest-3 "Derived" precedent) |
| C11 | | `{m}/{d}` ×n | 16 / 24 | row date | (a) |
| C12 | | marker label in area ink (`Unemployment published`, `Budget bill due`, …) ×n | 16 / 24 | what lands that day | (a) — cross-references the grid dot (that + whose area), says what; not a restatement |
| C13 | | `Nothing scheduled this month.` (only when empty) | 16 / 24 | empty state | (b) — the empty ledger under its rule could carry it |
| C14 | chrome · policy preview | `This Year's Policy` | 23 / 35 | panel header | (b) — `Estimated Effects` and the rows name the subject |
| C15 | | ~~`Show tab guide` / `Hide tab guide` button + its paragraph naming the pre-consolidation ten tabs~~ | (skin) / 16–24 | help text | **(c) — CUT `23cbb84`** |
| C16 | | `Estimated Effects` | 23 / 35 | the list's label | (a) |
| C17 | | horizon buttons `1 Day` `1 Week` `1 Month` `Full Turn` | 18 / 26 | controls | (a) |
| C18 | | `Over the next {horizon}` | 16 / 24 | | (b) — restates the selected horizon button |
| C19 | | `(±5-10% margin of error)` | 16 / 24 | the estimate's error band | (a) — no instrument carries it (a band on the figures could; until then (a)) |
| C20 | | `- a linear/compounding-scaled display estimate from the full 365-day projection, not a simulated sub-year value.` | 16 / 24 | methodology disclosure | (a) — honesty text; unsure → (a) |
| C21 | | ~~`Projection only, not a guarantee.`~~ | | hedge | **(c) — CUT `23cbb84`** |
| C22 | | eight effect rows `GDP Growth: +x%` … `Net Budget Impact: $x` (good/bad ink) | 16 / 24 | the estimate's figures | (a) — the figures; their sentence form is pre-v2 (on the stage they are instruments) |
| C23 | chrome · pinned strip · calendar pad | `JAN.` (MMM, OS locale) | 11 / 16 | | (a) |
| C24 | | `30` | 33 / 50 | | (a) |
| C25 | | `2029` (Courier) | 12 / 17 | | (a) |
| C26 | | `Time running` (RUNNING plate) | 16 / 24 | state | (b) — the green lamp beside it carries the state; the rail keeps the lamp |
| C27 | | `TIME PAUSED: {reasons} to continue.` (HELD plate) | 20 / 30 | the resolving screens named | (a) — B8's load-bearing half |
| C28 | | `Pause` `1x` `2x` `3x` `Saves` | 23 / 35 | controls | (a) |
| S1 | content · tab strip | tongue labels `Statistics` `Decisions` `Demographics` `Budget` `Policy/Laws` `Politics` (over their icons) | 13 / 19 | navigation | (a) OPEN — folded, the icons carry it and the labels are (b) |
| S2 | content · sheet | `Statistics` | 23 / 35 | header | (b) — the pulled-forward tongue says it |
| S3 | sub-tabs | `Domestic` / `International` | 18 / 26 | navigation | (a) |
| S4 | caption | `DOMESTIC BULLETIN — DESK READINGS, LIVE` | 14 / 21 | B6's screen-level live/published carrier | (a) — its first half restates the sub-tab; its second half is the only statement that these are live readings |
| S5 | | `Domestic` | 23 / 35 | header | (b) — the selected sub-tab says it |
| S6 | tiles ×10 | labels `GDP` `UNEMPLOYMENT` `INFLATION` `APPROVAL RATING` `CURRENCY STRENGTH` `POVERTY RATE` `GOVERNMENT DEBT` `DEBT-TO-GDP` `CREDIT RATING` `BUDGET BALANCE` | 7 / 10 | instrument labels | (a) |
| S7 | | values (`$29.8T` `4.37` `2.20` `47.7` `101.4` `18.3` `$38.8T` `130.1` `AAA` `-$5.46T`) | 28 / 42 (shrink to fit, floor 11) | figures | (a) |
| S8 | | unit `%` ×5 | 9 / 13 | units | (a) |
| S9 | | GDP delta `+0.00%`; `OUTLOOK +` / `OUTLOOK -` on Credit Rating when not Stable | 9 / 13 bold | | (a) |
| S10 | derived ledger | row names `GDP per capita` `Tax burden` `Government spending` `Deficit`/`Surplus` `Primary deficit`/`Primary surplus` | 16 / 24 | | (a) |
| S11 | | figures (`$85.8k` `19.3%` …) | 16 / 24 | | (a) |
| S12 | | trailing `of GDP` ×3, `of GDP, excl. interest`; the empty states `no population` / `not yet computed` + `advance a year` | 16 / 24 | units; empty states | (a) the units; (b) the empty-state phrases |
| S13 | | `Sector shares of GDP` | 16 / 24 | group label | (b) — each row's trailing `of GDP` says it and the rows are named sectors |
| S14 | | eight sector rows: names, figures, `of GDP` ×8 | 16 / 24 | | (a) — a unit column read down, not eight restatements |
| S15 | | `Sector shares of GDP: not tracked for this country.` (conditional) | 16 / 24 | empty state | (b) |
| S16 | six graphs | titles `GDP` `Unemployment` `Inflation` `Approval Rating` `Poverty Rate` `Debt-to-GDP` | 16 / 24 | instrument labels | (a) |
| S17 | | the `(dashed = next-year estimate)` suffix ×3 | 16 / 24 | a legend key | (b) — a key could be an instrument (1l drew the line weights; the key is what the suffix restates) |
| S18 | | change label `+2,5%` per graph | 16 / 24 bold | | (a) — prints in the OS culture (`+2,5%`) where the tiles print invariant (`+0.00%`); an existing inconsistency, logged, not v3's |
| S19 | | `< Older` / `Newer >` | 10 / 16 | controls (disabled on one page) | (a) |
| S20 | | range label (blank on one page; `Last 50 years`; `N-M years ago`) | 10 / 16 | | (a) |
| S21 | | axis labels min / mid / max ×3 per graph | 10 / 16 | | (a) |
| S22 | | threshold labels `NAIRU`, `comfortable` | 10 / 16 | | (a) |
| S23 | | `No data yet - advance a year.` (conditional) | 16 / 24 | empty state | (b) |
| S24 | Society box | `Society` | 23 / 35 | header | (b) — the rows name themselves; their area inks carry the grouping |
| S25 | | rows `Youth unemployment` `Life expectancy` `Income inequality (Gini)` `Real wages` `Productivity` `Housing overburden` (EU five only) `Homeownership` `House prices` | 16 / 24 | | (a) |
| S26 | | figures | 16 / 24 | | (a) |
| S27 | | trailing `of youth labor force` `years at birth` `0-100 scale` `index, 100 = start of term` ×2 `$ per hour (PPP), against your own past` `spend >40% of income on housing` `of households` / `of households (primary metric)` | 16 / 24 | units and their definitions | (a) — the two caveats (`against your own past`, `(primary metric)`) are rulings made visible; unsure → (a) |
| S28 | As published | `As published` | 23 / 35 | header | (b) — every title beneath carries "as published" |
| S29 | | `What the public sees: lagged, and revised as later estimates arrive.` | 16 / 24 | | (b) — restates B6's two channels, the badge chip (published) and the dashed frame (preliminary) |
| S30 | | ~~`Compare against the live figures above.`~~ | | instruction | **(c) — CUT `23cbb84`** |
| S31 | three published graphs | `GDP as published` `Unemployment as published` `Inflation as published` + change label, page row, axis labels, date axis, `latest: {value} ({lag})`, the badge chip `PRELIMINARY` / `FINAL` | 16 / 24; 10 / 16 | | (a) |
| S32 | | range buttons `1yr` `5yr` `All` | 10 / 16 | controls | (a) |
| S33 | bulletin | `PRELIMINARY`/`FINAL` chip · `Poverty rate as published: 18.3` · `for Jan 2028 - Dec 2028, released 1 Mar 2029` | 16 / 24 | B6's channel 1 | (a) |
| S34 | | `{label}: not yet published - the first release is still ahead.` / the graph's `Not yet published - the first release is still ahead.` (conditional) | 16 / 24 | empty states | (b) |

**Counts (element kinds, ×n collapsed):** (a) **44** · (b) **18** (C3 C10 C13 C14 C18 C26 S2 S5 S12-part S13 S15 S17 S23 S24 S28 S29 S34, plus S1's labels once folded) · (c) **3 kinds, 4 text elements** — cut at `23cbb84`. Nothing on this screen was cut that a board might have wanted back: every (b) stands and waits for the board.

#### Annex B — the instrument inventory: every self-contained figure the code already draws, with its minimum legible size measured on film

The ladder films are `v3a_ladder_1920_ladder_<kind>` (1920×1080: body type 23 px) and `v3a_ladder_1280_ladder_<kind>` (1280×720: body type 16 px), each rung captioned with its size in Courier under it; the sizes below are absolute pixels and hold at both (the type-bearing instruments were re-read on the 720p film). Read this table with the direction's rule: **candidates only, no new instruments.** "Honesty class" is the data's provenance vocabulary the code already uses — LIVE (`Country.State`, the desk reading), PUBLISHED (`Country.Published`, lagged and revisable — B6's badge and dashed frame), DERIVED (`DerivedStats`, arithmetic on live values), LEDGER (Class A attribution terms, recorded at the boundary and audited), SEED (`WorldFactory` constants, tagged `[VERIFIED]`/`[PROVISIONAL]`), CHROME (delivered art, no data). "Stands alone" = draws correctly outside its screen with only the data named.

| # | instrument (the direction's name) | renderer, entry point | takes | data · honesty class | stands alone? | minimum legible size — MEASURED, and the break |
|---|---|---|---|---|---|---|
| I1 | **the world map** | `MapRenderer.Draw(Rect, countries, playerId, eventMarkers, turn, fadeTurns, labelStyle, out clicked…)` (`MapRenderer.cs:105`) | a Rect (host 260 px tall, full column width) | six GDP-sized nodes at fixed illustrative positions (`CountryMapPositions` — **not geography**: no polygons, no coastlines), trade-volume lines, fading event dots · LIVE (`Country.State.GDP`, `TradePartners`, the event markers) | yes — one call, its own textures | **with names: 360×216 (type 12–17)** — at 240×144 the names collide; **nodes and lines alone: 120×72** (six nodes distinct), merging at 90×54. *(Corrected 2026-08-28, R-SP5: the names now take §A.9a's resort ladder — the full name, then the ISO 3166-1 alpha-3 code, then shrink toward the 8-px floor — and the renderer measures the smallest gap between any two labels or a label and another node; the harness asserts ≥ 4 px on the International screen (25.8 px at 1600×929, every name at its first rung) and the ladder film carries the rung and the gap in each rung's caption. The first filing's "United States / France touch at every size" was the ladder film's small rungs, not the screens.)* |
| I2 | **the compass** | `PoliticalCompassRenderer.Draw(Rect, countries, playerId, labelStyle)` (`:118`) | a Rect (host square, `clamp(0.4·h, 260, 520)`) | one dot per country on two 0–100 axes, `GetFiscalSizeAxisValue` / `GetRegulationWelfareAxisValue` · DERIVED from LIVE (+ the seed portfolios, `[PROVISIONAL]` until the database session) | **yes, since R-SP4 (2026-08-28)** — the renderer declares an honest footprint (`Footprint`: the plot square plus the caption band at the width the captions need, wrapped never shrunk) and containment-asserts the plot and both captions inside the rect it is given; the first filing of this row found the captions loose (GUILayout labels after the plot) | **with names: 240×240 at 8-px type** (crowded), 360 at 12 px comfortable; **dots alone: 90×90**, merging at 64 |
| I3 | **the approval face with its nine-term attribution** | **does not exist as a face or a dial.** What exists: (a) the Approval headline **tile** (`PoliSimWidgets.StatTile`, `:387`); (b) the Approval **graph** (`GraphRenderer.Draw`, `:118`); (c) the attribution **ledger panel** (`StatTracePanel.Draw(country, gapStance, style, style, width, hostHeight)`, `StatTracePanel.cs:149` — GUILayout, no Rect; `MeasureHeight` first) with **13 term rows** (12 Class-A terms + ClampLoss; the nine the direction names are the nine non-misery terms) and up to four dated events, every row `fill = -1` (no gauge: "there is no proportion here") | (a) a Rect + scale; (b) a width; (c) a width + host height | (a) LIVE; (b) LIVE history (`StatHistory.ApprovalRating.Quarterly`); (c) LEDGER (`Country.ApprovalLedgerLastPeriod`, Class A terms + Class B events, the boundary identity audited) | (a) yes; (b) yes (its own texture cache per instance); (c) yes given the country — its section selection is static (`RequestSelection`, committed in `MeasureHeight`) | **tile: label legible to scale 0.8 (264×98, label 8 px), the hero figure alone to scale 0.3 (99×37, 13 px)**; **graph: 240 px wide** at 9-px furniture (title, change, axis), the line alone to 120; **ledger: 360 px wide at 13-px type (the header line, the terms, the indented misery sub-rows all read); at 240 / 9 px the sub-row labels ("· unemployment above NAIRU") overflow their name column at the 8-px floor - the guard's own record on both films**. **A face or dial would be a NEW instrument** — Phase B's, drawn on the board first |
| I4 | **the sparkline strip at 1l's weights** | `GraphRenderer.DrawSparkline(Rect, history, color, maxPoints = 40)` (`:914`); the strip is `PolicyScreenStatsRenderer.Draw(area, country, labelStyle, availableWidth, maxStats = 4)` (`:149`, GUILayout) — one chip per stat: icon 22/16·type, value, trend arrow, sparkline 72×20 per 16 px type | a Rect (the bare line); a width (the strip) | LIVE (`Country.State`, never published — the class doc's ruling); history for the line · **1l's weight law applies to it — R-G4, `thickness = max(2, round(rectHeight/34))` device px, so 2 px at the chip's 20–35 px rects (the graph's 3 px at a 90 px rect is the same law at a taller rect)**; no projection segment (a sparkline has no estimate to dash). *(Corrected 2026-08-28, R-SP3: this row's first filing said no 1l weight reached the sparkline — it does, through the one renderer the strip and the graphs share, `GraphRenderer.BuildSparklinePixels`; the eye pair at 1280 and 2560 is on film.)* | yes (the line); the strip needs `PolicyScreenStats.GetStatsForArea` | **line: the shape reads to 36×10**, comfortable 54×15 (the chip's own 72×20); a dash at 24×7. **strip: 280 px wide at 9-px type** (four chips stacked), 380 at 12 px in two columns; 200 / 8 px is the floor and reads as a footnote |
| I5 | **the calendar sheet** | `GameController.DrawCalendarMonthGrid(monthStart, today, markers)` + `DrawCalendarMonthLedger` (`:2961`, `:3141`; GUILayout) — the month page (weekday header, day cells, struck spent days, up to four area dots per day) and the dated ledger | a width (the column) | markers from `BuildCalendarMonthMarkers` — release days (`ReleaseCalendar`), pending bills' days, the election cycle, divisions, events · LIVE schedule facts | no — a GameController method reading the simulation's calendars; extractable | **420 px wide at the sheet's own type** (16 px at 720p, 23 at 1080p): at 320 the weekday row clips its seventh column; at 240 the month header wraps and the ledger's names break mid-word; below that the numerals overlap |
| I5b | the calendar **pad** / the rail's **chip** | `DrawCalendarPad()` (`:4390`, size from body type: 64/12.5 × label) and `DrawRailCalendarChip(cell, …)` (v3a — the pad's sprite at cell size, month + day) | the pad: none (its own size); the chip: a cell width | `SimulationManager.CurrentDate` · LIVE | the chip yes (a cell width); the pad no | **chip: a 32-px cell** (32×36: month 9 px, day 13 px); at 24 the day numeral has no line box left (the guard's own record, both films); the rail's 39 px at 720p and 55 at 1080p sit above the floor |
| I6 | **the event / alert stamps** | the division verdict stamps: `ui_stamp_carried` / `ui_stamp_rejected` (170×50 @1×, rotation baked) via `UiPalette.DrawTintedIcon` (`GameController.cs` ~`:8605`); the urgency chip: `PoliSimWidgets.Stamp(Rect, text, style, ink, borderInk, borderWidth, rotation)` (`:272`, procedural, −2°); the badge chip `PoliSimWidgets.Badge` (`ui_chip`); the HELD lamp `DrawHeldLamp` (v3a) | a Rect | CHROME (the sprites) · the chip's text is (a) content; the lamp is B8's state | yes | **sprite stamp: 68×20** (the word reads), comfortable 85×25, 51×15 the break; **procedural chip: 9-px type** (83×20), 8 the floor, 7 breaks; **lamp: 8 px with its glow readable**, 6 the dot alone, 4 a speck |
| I7 | **the stepped rule** | `GameController.DrawMagnitudeSteps(Rect, tier, stepWidth, gap)` (`:7538`) — always four steps, filled to the tier, one ink | a Rect | a law's magnitude tier (1–4) · SEED/content | yes | **32×8** (four steps and the fill count read), comfortable 48×12; 24×6 marginal; 16×4 dots |
| I8 | the hemicycle | `HemicycleRenderer.Draw(title, seats, labelStyle)` (`:38`; GUILayout, **fixed `AreaWidth 340 × AreaHeight 190`**, five rows, 10 px dots) + a legend of `LedgerRow.DrawReadOnly` rows | none (its constants) | `Country.ParliamentSeats` · LIVE (seat drift) — re-keys under item 10 | yes at its one size | **does not shrink** — no size parameter; the legend rows need ≥ ~430 px of row width at 23-px type (a 440 px frame clipped "Nationalist Front" by 9 px); a Phase B change if the board wants it smaller |
| I9 | the pie | `PieChartRenderer.Draw(title, slices, labelStyle, valueFormat, moneyUnit)` (`:58`; **fixed `Diameter 120`**, solid — no donut) | none (its constant) | demographics shares · LIVE | yes at its one size | **does not shrink**; and the eight-ink cap makes it a ledger past eight categories (`RankedBarLedgerRenderer`) |
| I10 | the line graph (1l's weights live here) | `GraphRenderer.Draw(title, history, projected, labelStyle, higherIsBetter, moneyUnit, threshold…)` / `DrawPublished(...)` (`:118` / `:214`; GUILayout; texture 300×90, display height `clamp(0.075·h, 50, 90)`) — title + signed change, page row, axis min/mid/max, threshold label; the published form adds the date axis, release markers, the badge, the dashed frame | a width | LIVE history (`StatHistory.*.Quarterly`, 250 entries) or PUBLISHED series | yes (one instance per chart — it caches its texture) | **240 px wide at 9-px furniture**; 320 at 12 px comfortable; the plot line alone reads at 120 |
| I11 | the policy web | `PolicyWebRenderer.Draw(Rect, labelStyle, country, pinnedPolicy, pinnedStat, out…)` (`:731`) — the ring, ~73 nodes sized by degree, edges on hover/pin, solid = DERIVED (a ledger term) / dashed = DECLARED | a Rect (host `clamp(…, 0.5·h, 0.92·h)` square) | the edge set per country · LEDGER (Derived) and DECLARED | yes | **with labels: 240×240 at 9-px type** (crowded), 360 at 13 px comfortable; **the ring alone reads at 180**, a blob at 120 |
| I12 | the flag | `IconLibrary.GetFlag(CountryId)` — full-colour art, never tinted (`:163`) | a Rect (3:2) | CHROME | yes | **24×16** recognisable (stripes and canton), 30×20 comfortable; 18×12 breaks |
| I13 | the area / nav icons | `IconLibrary.GetAreaIcon(area)` / `Get("icon_nav_*")` through `UiPalette.DrawTintedIcon` — white-on-alpha, tinted (`:38`, `:323`) | a Rect (square) | CHROME | yes | **12 px** readable, 16 comfortable, 10 recognisable, 8 a blob (§5.2's 22 px guidance stands) |
| I14 | the read-only ledger row (the gauge lane) | `LedgerRow.DrawReadOnly(Rect, name, fill, figureText, trailingText, barInk, nameStyle, figureStyle)` (`:399`) — name, track + fill, figure, unit; `fill < 0` = no gauge | a Rect (its height from `LedgerRow.Height(style)`) | any proportion · LIVE or DERIVED | yes | **10-px type** (row 33 px tall) reads; 8 px is the floor and reads as a footnote — a document form, not a stage instrument; listed because the direction's "a bar" is this lane |
| I15 | the bars | `UiPalette.DrawDivergingBar(Rect, value, displayRange)` (`:602`, fills outward from centre, green right / red left); `PoliSimWidgets.ThresholdBar(Rect, fraction, thresholdFraction, fill)` (`:537`) | a Rect | a signed alignment; a share with a threshold · LIVE/DERIVED | yes | **40×14** (the fill and the centre line / tick read), comfortable 64×14; 24×14 breaks |
| I16 | the stat tile | `PoliSimWidgets.StatTile(Rect, label, value, suffix, delta, deltaIsGood, subLabel, area, scale, barFraction, thresholdFraction)` (`:387`; height from `StatTileHeight(scale, hasDelta, hasBar)`) — the printed plate, label at 10·scale, hero figure at 42·scale (shrinks to 11), delta at 13·scale | a Rect + a scale | LIVE | yes | **scale 0.8 (264×98)** with its label; the hero figure alone to scale 0.3 (99×37) |
| — | **not instruments, listed so the board does not ask:** the portraits (`DrawPersonPortrait`, a person, not a reading), the country selector and the signing document (Canvas screens, their own class), the ranked bar ledger (a table), the trace panel's chips (I4's strip) | | | | | |

**How the sizes were measured.** `GameController.DrawInstrumentLadder` (harness-only; no player path reaches it) draws one kind per capture on a paper sheet at a descending run of sizes, each rung by the instrument's own renderer on the live game's data, with a Courier caption of the size; instruments that carry type take a label style scaled with the rung and floored at the guard's 8 px, so a 64 px map is not measured with 24 px names. The break is read by eye on the film at both sizes and stated per row; where the code's own guards recorded a rung (the chip's day numeral at the 24-px cell, the sheet's at its narrowest rungs), the log line is the break's second witness (`shot_v3a_ladder_*.log`, reported not gated).

---



## 42. UI v3.0 Phase C — the defaults settled, the note to 1i–1n, one current paste (2026-08-28)

**Authority:** Elias's Phase C kickoff of 2026-08-28 (R-PC1…R-PC4 pre-issued). Anchor `631a9d4` (verified; tree clean; `origin/main == HEAD`). The kickoff's Design import re-read the live screens file: unchanged since the Phase B read but for one block under board 1m — `BUILT 2026-08-28 (631a9d4, v3desk_*) — this board is implemented; pointer, not an edit. Standing corrections from the build, accepted: …` naming R-B4, R-B2 and R-B3 — Design's side of the ratification below; no new board, nothing to implement.

**Phase 0 — R-PC1, the ratifications (`38e164c`).** R-B2 (the ways home), R-B3 (the FOLDED lock) and R-B4 (the eight) written into the direction doc's Desk section and §A.17's margin as standing, with the recorded line: if the debt-to-GDP / currency estimates are ever wanted they are a simulation feature with its own measurement pass (candidate slot: after item 10), never a UI patch. The live file's acceptance block quoted beside them.

**Phase 1 — R-PC2, the fold-default table.** The rule: a screen defaults FOLDED **only if its content is designed for the full-width stage** — today exactly Screen 0 (R-B3) and the Budget ledger (R-A1), both locked; everything else OPEN. The table (screen · default · locked?) is in the direction doc as the single source, with the entry rule for a new screen; the spec's §A.5 paragraph follows it; `GameController.DefaultShellFold` collapsed to the two locked screens, so **Statistics › Domestic reverted to OPEN** the day the Desk took the landing duty it had stood in for since Phase A. Filmed at 1280 and 2560 as `v3c_*`: the sweep in the ruled defaults, the changed screen opening with its column and tongues, its `_folded` pair beside it — the guards and the edge check silent in both states at both sizes (the figures under the gate). §V gained the row.

**Phase 2 — R-PC3, the note to 1i–1n.** `CLAUDE_DESIGN_BOARD_1I_NOTE.md` retitled and rewritten 1i–1n-aware: the 1i / 1j / 1k–1l sections as they were; a new section for 1m and 1n — the boards built the day they landed, the placement as drawn, the seven deviations and the (b) resolutions as drawn, the three calls stated rather than silent (the ways home; the two refused rows and why the refusal is the model's honesty; the FOLDED lock) and noted as standing on both sides, the two things the board could not show (the staged event, the game's own reason string; the sparklines already at 1l's weights), 1n as the re-skin it was, the ruled defaults since; a plain thanks for the same-day boards; the captures section re-pointed at the `v3desk_*` frames. The note keeps its contract — courtesy, no asks; the hatch re-cut stays in the request doc. Earlier versions in git history.

**Phase 3 — R-PC4, the package.** `SEND_PACKAGE_2026-08-28.md` regenerated against the post-Phase-C tree: two documents — the 1i–1n note (a new dated path) and the request doc whose one live ask is §E5's re-cut with the measured figures (to `uploads/` and a new dated copy) — fresh digests, one line each on where it goes and what comes back (the hatch re-cut only, with its import-and-diff steps), and the statement that this paste supersedes every earlier package. The boards ask and its annex captures are out of it, answered and migrated.

**The gate.** The touched screens on film: `v3c_1280_*` and `v3c_2560_*` — **82 captured each, 0 text overflows, 0 containment escapes, 0 canvas text violations, `ScreenEdgeCheck` 0 clipped, ATTRIB 0**; the guards and the edge check in both states of the changed screen (its `_folded` pair filmed beside the OPEN default); Screen 0 asserted on the Desk, locked, FOLDED at both sizes; the Budget ledger and Screen 0 reported as the two locked screens with no pair to film. Trajectories `traj_v3c_*` ≡ `traj_v3desk_*` **6/6 by SHA-256** (nothing here is simulation). `sv_index.html` regenerated (30 rows; §V gained the changed-default row). The roadmap re-derived: v3.0 Phases A–C closed; the era's live edge reads WAITING — Design's hatch re-cut (§E5) · 13 September — with §P recommended post-C so the density verdict is read on the real stage.

**Rulings consumed:** R-PC1…R-PC4 as issued. **Reversible calls, one line each:** R-PC2a the table lists the Canvas screens as "the seam's own — the frame beneath keeps its screen's state" (no default of their own); R-PC2b the table's entry rule (a new screen enters with a line before it ships); R-PC3a the note's captures section re-pointed at `v3desk_*` and the `v3c_*` sheet, the `board1jc*` sets kept as history; R-PC4a the note goes to `send/design_note_2026-08-28c/` and the request doc's dated copy to `send/design_request_2026-08-28c/` (the `c` suffix so a third same-day path shows as new). **RULINGS NEEDED:** none. **The push:** R-SP1's fast-forward at the pass's end, the outcome in the report.

## 43. The consolidation rider — the R-B enumeration, the Phase C ratifications, #17 verified, the two renamed-in-meaning frames, the 1920 watch line (2026-08-28)

**Authority:** Elias's rider of 2026-08-28, five units surfaced by the consolidated bug report; run alone at anchor `256a480` (verified; tree clean; `origin/main == HEAD`); one records sweep, one commit, R-SP1 push at the end.

**1 — the enumeration.** R-B1…R-B14 listed one line each, verbatim from §41, in the rider's report, with R-B2, R-B3 and R-B4 marked ratified (R-PC1). The seeing, not the judging: after the list reaches Elias, silence stands the eleven un-ratified calls per rule 4.

**2 — the Phase C ratifications, recorded.** Ratified standing in conversation on 2026-08-28: **R-PC2a** (the Canvas screens as "the seam's own" — correct layering) and **R-PC2b** (the entry rule — R-PC2 made durable), written into the direction doc's fold-default table; **R-PC3a** (the note's captures re-pointed at the current films, the old sets kept as history) and **R-PC4a** (the dated `…-28c` paste paths) — established hygiene — written into the roadmap's gestures bullet and the send package's "where each goes"; all four dated here. The courtesy note itself is untouched (it carries no rulings).

**3 — #17 verified on the code.** The Desk event card's title goes through `PoliSimWidgets.MeasuredLabel` (`GameController.Desk.cs`, the card's name label — shrink-never-truncate); the chrome banner's `BREAKING: {name}` is a wrapping label (`_eventBannerStyle.wordWrap = true`, `GameController.DrawTopBanner`) — wraps, never truncates; and the source of #17's line — the trace panel's dated event rows (`StatTracePanel`, `Name = e.Label`) — draws through `LedgerRow.DrawReadOnly → DrawNameCell → Cell → MeasuredLabel` (`LedgerRow.cs`). So every dynamic title from the pool already takes the ladder's cheapest rung, and `"Global Commodity Price Spike" needs 109.5 wide in 62.4 at 8px on ladder_trace` (the 1920 ladder film) is a **below-floor measurement** — a 62 px name column at the ladder's smallest rung cannot hold the label even at the guard's floor. **#17 closed as measurement; no code moved.**

**4 — the two renamed-in-meaning frames.** `01a_selector_yielding` and `01b_running_strip` show Screen 0 beneath the scrim since Phase B, so their guard results are the Desk's: an attribution note on §V's Desk row and on the RUNNING-plate row (the plate's own film stays the `omni_final` set), and a comment in the harness above the two captures. `sv_index.html` regenerated so the rows carry it.

**5 — the 1920 watch line.** Three environmental events on 1920×1080 runs in one day — the Mono finalizer crash (`Crash_2026-08-28_114730838`), the licensing fold (the sp3 run's `UnityConnectWebRequestException`), and the nine-minute stall (log silent 14:39:03–14:48:34, two `Unity.exe` alive) that then completed clean. The class is environmental and the response is re-run; the size now has a named pattern, recorded in `CLAUDE.md`'s environmental notes: a fourth event on 1920 is a count, not a surprise, and a 1920 step that goes quiet gets ten minutes before it is killed.

**Not touched, by the rider's scope:** the hatch (the §E5-close pass), everything in the bug report's fixed table, the tooling observations, the sitting, the saves, 13 September. **RULINGS NEEDED:** none.

## 44. UI v3.1 Phase A — one frame, the structural home, every annex measured, the ninth request installed (2026-08-28)

**Authority:** Elias's v3.1 Phase A kickoff of 2026-08-28, from the first live sitting on the v3.0 build (two screenshots — §V's two sitting rows — carrying the verdict: the Desk's frame wins, the OPEN "half screen" dies, density / instruments / icons / contrast go to Design as v3.1). `DESIGN_REQUEST_V3_1.md` delivered with it; both archived verbatim out of tree (`../PoliSim-captures/inbox/`). Anchor `999e47e` (verified; tree clean; `origin/main == HEAD`). Rulings R-E1…R-E4 pre-issued.

**Phase 0 — records (`82a68d1`).** The sitting's two findings into §V with Elias's complaints verbatim in substance (the screenshots are his to attach at the paste — not on disk, in the inbox, or among the Design uploads); the v3.1 section into the direction doc (D1 as a ruling gated on the audit; D2–D6 as Design's; Phase A's scope and R-E4; Phase B = build on the boards).

**Phase 1 — the duty audit (R-E1's gate).** Thirteen duties the OPEN chrome column and the tongues uniquely carried, enumerated from the methods only the OPEN branch called (`DrawTopBanner`, `DrawCalendarPanel`, `DrawPolicyControls` → the preview, `DrawCalendarAndSpeedControls`, `DrawConsolidatedTabs` / `DrawActiveFolderTongue`) against the census rows C1–C28 and S1, each with its home named — oversight and the preview on the Desk; interrupts and game over on the folded banner; time, status and pause on the rail; navigation on the rail. **No orphan.** Two homes were added rather than assumed: the game-over reason on the folded banner on every screen (behaviour #8 applied to the game-over hold, row 2) and the rail's PAUSE / RUN chip in the fold toggle's freed cell (R-E1a, row 9); two interaction costs recorded, not hidden — the interest-rate draft's estimate is read on the Desk (row 5) and Saves is the Desk's (row 10). The table is Annex A of the ninth request.

**Phase 2 — OPEN retires, ONE FRAME (`5353db1`).** `ShellFoldLocked` true and `DefaultShellFold` FOLDED on every screen; the fold toggle's cell → `DrawRailPauseChip` (a desk chip reading PAUSE while the clock runs and RUN while the player holds it, disabled while an interrupt or game over holds the clock — B5; RUN returns to the last speed); `BuildFoldedInterruptText` returns `GAME OVER - {reason}` at game over (the game's own string); the OPEN branch and its methods marked unreachable and kept one pass with the enum and the persisted overrides (session's call — the harness's historical states and the record; v3.1 Phase B deletes them); the direction doc's table collapsed to one row and the spec's §A.5 follows; the harness's lock messages say ONE FRAME. *Fix-forward, found on the first `v31` film by eye:* the held banner's speed hint still said "on the unfolded desk" — reworded to "on the Desk" (the cluster's home), and the matrix re-run whole on that code so the family films the final state.

**Phase 3 — the structural home (`bad840e`, R-E2).** The rail's topmost cell is HOME → the Desk on the player's flag — the session's pick, one line: the most legible existing glyph because it is the rail's one full-colour mark (first-class by contrast alone), 24×16 at the 39 cell growing with it (Annex B I12's "recognisable" floor), and it carries C6's country identity onto every screen now that the OPEN header is gone; first position, a hairline-strong rule beneath, board 1n's convention in the Desk's own brass while on the Desk, containment-asserted with the rail's cells. The calendar chip stays the learned second way (R-B2). Design's 1n-r2 replaces the face.

**Phase 4 — the annexes (R-E3, measured).** A the audit table (13 rows); B the rail's icons at the real cells — the derivation (`cell = round(icon × 44/24)`, icon `round(tab type × 1.15)`: 21/39 · 25/46 · 29→30/55 · 35/64) and the four crops from the `v31_*` matrix; C the paddings and pitches from the code's own tokens at 1280 and 2560 (the frame's fractions, the skin box's fixed 28 px per nesting level, the paper card's 14/14/12/14, the tile's `s`-scaled pads, the ledger lane, the graph's display clamp, the Desk's board values, the rail's derivation, the radii and bars) with the type table (header 22/42 · body 16/28 · tab 18/30 · banner 20/36 · meta 12/20 · the Desk's captions 8–9/16–19 · the tile label 7/13), and the dead-space share per screen at 1280 and 2560 from the film (`deadspace.ps1`: a block is empty when no pixel differs from the sheet's paper by more than the grain tolerance; blocks 16 / 32 px), the Year-0 rows flagged "empty-state, not spacing"; D the sitting's findings in Elias's words with the images his to attach; E the Statistics census (24 dataset rows: 9 levels, 6 shares, 1 distribution, 10 series, 1 relation; the form-vs-shape disagreements named — E4's distribution as eight rows, E3's four shares as unrelated gauges, E14/E15's histories drawn as single numbers, E1's ten levels as plates); F the ink pairs (35 rows: fg, bg, ratio, px, where) with the measured WCAG ratios — TextMuted 3.9 / 3.7 on paper and tile at 7–8 px, Caution 2.5 at 9–10 px, Good 3.4, the selected chip's light-on-brass 3.2, Global 3.1 and Political 2.9 as 16 px ledger inks; the judgment left to D6.

**Phase 5 — the request installed, the paste.** `CLAUDE_DESIGN_ASSET_REQUEST.md` §1 is the ninth request — Elias's six asks as delivered, with Annexes A–F in place — its status rewritten (one live ask; §E5's second re-cut is on the live project, its import the §E5-close micro-pass's); `SEND_PACKAGE_2026-08-28.md` regenerated: the request doc through v3.1 and the note unchanged (already in Design's `uploads/`), the two screenshots and the four rail crops named with their paste paths, supersession stated, what comes back per ask.

**Not in scope, by the kickoff:** density values, type sizes, Statistics forms, icon faces, ink values (D3–D6); the Desk's content; the hatch (its own micro-pass — Design's second re-cut landed on the live project this evening, read but not imported); the saves and the rest of the sitting; 13 September.

**The gate — the full matrix, twice.** The first `v31` matrix (the code at `bad840e`): 79 captured at each of 1280 / 1600 / 1920 / 2560 (the 78-screen sweep plus Screen 0's four frames, less the three fold-pair frames that no longer exist), 0 text overflows, 0 containment escapes, 0 canvas text violations, `ScreenEdgeCheck` 0 clipped, ATTRIB 0; Screen 0 asserted on the Desk, locked, FOLDED; every document reported ONE FRAME with no pair to film; trajectories `traj_v31_*` ≡ `traj_v3c_*` 6/6 by SHA-256; the eight checks green. By eye at 1280 the banner's old clause was caught (above), fixed at `b457be2`, and **the whole bar re-run on that code with the same result line for line** — 79 / 0 / 0 / 0 / 0 clipped at every size, 6/6 identical, eight checks green, `UpstreamCheck` quiet (five commits ahead, under the threshold, cleared by the push). The rule-15 old-beside-new read at 1600 against `v3c_*`: every document lost its column and tongues and gained the rail's HOME and PAUSE cells; nothing else moved — the sheets' content is the `v3c` content in the wider column. `sv_index.html` regenerated (34 rows).

**Rulings consumed:** R-E1…R-E4 as issued. **Reversible calls, one line each:** R-E1a the rail's PAUSE / RUN chip in the fold toggle's freed cell (the audit's home for the player's own hold on every screen; the speed choice stays on the Desk); R-E1b the game-over reason on the folded banner (behaviour #8 for the game-over hold); R-E1c the OPEN branch, the enum and the persisted overrides kept one pass, marked, for the harness's historical states and the record; R-E2a the flag as the home glyph, active in brass on the Desk; R-E3a Annex C's dead-space method (paper by modal colour, blocks of 16 / 32 px, the banner counted as content); R-E3b Annex D carrying Elias's words with the images his to attach. **RULINGS NEEDED: none** — the audit found no orphan. **The push:** R-SP1's fast-forward at the pass's end, the outcome in the report.

## 45. UI v3.1 Phase B — the five answers built: the §E5 close, D6, D4, 1n-r2, 1m-r2, 2a (2026-08-28)

**What it was.** Design answered the ninth request in full the evening it was sent — boards 1n-r2, 1m-r2 and 2a, the D4 density token table and the D6 contrast pass lead the live screens file — and the third hatch re-cut came with it. This pass read the five into the spec (§A.18, the tables verbatim with their read-vs-built notes) and built them in order, one unit one commit, on the boards' own terms: placeholders declared as such and the build drawing from its own data; the two re-measures the tables ask for (Annex F after D6, the dead-space share after D4) filed back as numbers. Anchor `54677db`; rulings consumed R-B2 / R-B3 / R-B4 standing, R-B7 (the ledger's rows are the panel's own), R-B10 (instrument type at the board's px), R-D3 (a deferral is by name, with its pointer), R-SP1 (the push).

**Unit 0 — the records (`eab4edb`).** `POLISIM_V2_SCREEN_SPEC.md` §A.18: the five sections as read — 1n-r2's captioned cells and refusals, 1m-r2's placements at the 1156×680 inner area and its Year-0 empty states, 2a's forms per dataset, the D4 and D6 tables verbatim; the direction doc's v3.1 section opens Phase B; `MISSING_PREREQUISITES.md` §S records the v3.1 paste landed (`…-9a98b00e.md`) and the answer in full, the two sitting screenshots now on disk beside the captures (read back from Design's `uploads/`).

**Unit 1 — the §E5 close (`1d9926d`).** Design's re-cut #3 (`ui_hatch_draft.svg`: nine rects in a `rotate(45 16 16)` group, 5.657 wide on an 11.314 pitch — 16 px period, 8 px duty, centred on x + y = 16k, cut to our own §E5 measurement) imported and diffed with resvg (`stripcut_e5close_20260828_191435.log`): **structure 7.42 % against the 1 % budget, edge 0.02, mismatch 7.81 %** — 60 % → 33.4 % → 7.42 % across the three cuts; period, phase and duty now agree. The residual classified pixel by pixel: 64 of the 76 mismatched px straddle the check's alpha-128 ink threshold (the shipped PNG's edge pixels at alpha 160, resvg's at 96–152 — two rasterizers covering a 45° edge on a 32 px tile), 12 solid-vs-void (1.17 % of the canvas). Not a cut error and not Design's to cut a fourth time on our say-so: the deferral stays by name with the third measurement as its pointer (`StripCutDiffCheck.DeferredPairs`, the class doc amended — the "dies the day the answer lands" rule cannot be applied as written), no classifier change without a ruling. Design's half of §E5 is closed; the request doc §E5 and MISSING §E5 record the measurement.

**Unit 2 — D6 applied (`342d266`).** `TextMuted` #665E4F, `Good` #2E7048, `Caution` #8F6900 (text uses), `Neutral` #5F6672 (text uses; the Neutral AREA ink stands), `Global` #47708E and `Political` #8A6B21 in both `UiPalette.AreaColors` and `PoliSimTheme.AreaAccents`; the selected chip's caption flipped to TextPrimary on brass (`DrawDeskChipButton`). **The text/fill split the palette note promised:** `Draft` is no longer an alias of `Caution` — it keeps #BE8A00 as the FILL amber (a graph's threshold line, the preliminary-release frame, the ledger's draft knob and track, the hatch, the BREAKING banner on the dark desk where the lighter value is the legible one), while the threshold LABEL, the BREAKING chip, the drafted figure in a ledger row and a drafted label take the darkened text ink. **Annex F re-measured** (the sRGB arithmetic on the hex values — the measurement is the fact, as D6 asks): TextMuted 5.22 on Card / 4.98 on Tile, Good 4.86, Neutral 4.72, Global 4.31, Political 4.07 — D6's targets met; **two rows short**: Caution 4.09 / 3.90 (D6 aimed at ≥ 4.5), TextPrimary on brass 4.03 (D6's table said 5.5; up from 3.17, the flip kept as the better assignment). Filed back in Annex F as numbers, not asks.

**Unit 3 — D4 applied (`a47fc1b`), mechanically.** `ScreenMarginFraction` and `ColumnSpacingFraction` 0.02 → 0.012, `SectionSpacingFraction` 0.03 → 0.02; the paper box's padding 10/10/8/10; the dossier card 14+8/14/22/14; the stat tile's pads 12/11/16/6/14 with its label at 11; the tile grid gap 6s; the ledger lane 8s + line, +4s; the graph clamp(0.085h, 56, 110) over the standing 300×90 buffer (R-G5); the calendar cell gap 2s; the radii 16/13/11/10/9; body type clamp(0.024h, 17, 30); the mono meta floor 10; the Desk caption floor 9; headers, banner and tab-derived icons untouched. **Two read-vs-built notes, recorded at the sites and in §A.18:** D4's "skin box, 28 → 16 per level" and "area card padding 14/14/12/14 → 10/10/8/10" name ONE style (`_boxStyle` is the skin's box dressed by `StyleBoxAsPaper`) — the explicit numbers were applied, a nesting level costs 20 px, not 16; and the one-line pitch token's only callers were the law browser's four sites, whose pitch D4 rules STANDS — the token moved (`LedgerRow.OneLineHeight`, +4s) and the browser held (`LawBrowserRowHeight`, frozen at R-C1's +6s). The dead-space re-measure is in the gate below.

**Unit 4 — 1n-r2 built (`ac29e73`).** The rail's width unchanged (39 / 46 / 55 / 64); every cell grows down to carry a caption — `DrawRailCell`: the bare glyph at 22 of the 39 cell (the plate border dropped; 22 / 26 / 31 / 36), a 2-unit gap, the caption in mono at 7.5 of the cell (8 / 9 / 11 / 12; the guard's 8 is the floor), padding — DESK · STATS · DOCKET · PEOPLE · BUDGET · LAWS · POLITICS; active = 1n's wash and spine with the caption in the area ink and bold; inactive = the tab-swatch tint with the caption in TextSecondary; the home face = the flag (24 of the cell, 3:2) over DESK with the brass wash at 0.16 and the brass spine on the Desk. The chip, the lamp and PAUSE/RUN unchanged. Redrawn glyphs refused by the board as a costed follow-up (→ the request doc §4). *Found on the first film:* the bold POLITICS caption is 43 px in the 39 cell at 1280 — the weight now yields before the size (regular in the area ink), never a shrink below the floor or an overflow.

**Unit 5 — 1m-r2 built (`e72646c`).** The Desk's set replaced: the 1156×680 inner area; masthead 28 (flag 26×17, title mono 10.5 bold, LIVE 10, chips mono 9 with padding 3/8 centred in the masthead); columns 440 / 250 / 440 at a 13 gap — map 320, ledger 244, compass 250, effects 314, calendar 420, reservation 144 — the strip integrated into the sheet (a hairline-strong rule at +8, hairline dividers between the cells, no plates; caption 7.5 muted, numeral 17 bold, sparkline 46×10); plate captions 8.5; the ledger's hero 34 with its caption at 9, names 14, figures mono 12 on a 17 pitch; the effects rows 26 (label 12, bar 66×9, value mono 10.5); the calendar ledger on a 22 pitch (date 10, label 12.5). **The Year-0 empty states:** the ledger's nine rows with em-dash figures in the muted ink under a **`FIRST ATTRIBUTION — {date}`** chip in the caution ink that prints the model's own first boundary (`EpochDate + (turn + 1) × DaysPerTurn` — the calendar markers' arithmetic; 1 JAN 2027 at Year 0, the board's JAN 31 being a placeholder; the names from `StatTracePanel.ApprovalDeskTermNames`, one vocabulary); the effects card's zero rows on bare centre-lined tracks (zeros in the neutral ink) under a dashed-frame caption while nothing is drafted — its claim aligned to the model, **`NO DRAFT PENDING — ESTIMATES FOLLOW THE RATE DIAL AS DRAFTED AND EVERY BILL AS IT PASSES · ±5–10% MARGIN · SCALED DISPLAY ESTIMATE, NOT A SIMULATED SUB-YEAR VALUE`** (the preview reads the rate dial as its one draft input; a drafted bill does not move it — R-B4's discipline over the board's wording); a chip without a kept history draws a dotted baseline ending in today's solid dot; the event reservation DRAWN as a dashed frame with `EVENT CARD — DRAWS ONLY WHILE AN EVENT IS LIVE` and `YEAR 0 OPENS QUIET — THE RESERVATION HOLDS ITS GROUND / AND THE CALENDAR ABOVE SAYS WHAT IS COMING INSTEAD` (`YEAR N IS QUIET` at any later turn). Filmed at 1280 for the USA and for Sweden at Year 0 — the board's own frame.

**Unit 6 — 2a built (`96bc38e`; `GameController.Statistics.cs`).** Statistics › Domestic as instruments, top to bottom: the ten headline readings as compact plates in a 5-column grid (caption 7.5 muted, numeral 19 bold, the GDP delta and the outlook beneath; ONE list with the Desk's strip — `BuildHeadlineReadings`); **`FISCAL POSITION — SHARES OF GDP · ONE AXIS TO N%`** — tax burden, spending, the deficit and the primary balance as bars to one printed axis (the group's maximum rounded up to the next 10 %, never below the board's 30 %; the ticks printed beneath), each with its figure, a row no closed year has computed stating so instead of a track at zero, GDP per capita beneath as a bare level (E2 absorbed, §A.9b); **`SECTOR SHARES OF GDP — ONE DISTRIBUTION`** — one stacked bar in the categorical eight (normalised to its own sum) over a two-column legend; **the six live graphs in a 3-column grid** at D4's taller clamp with 12 px bold titles, the dashed next-year note stated once in the section caption; **`SOCIETY`** in two columns — a 70×8 gauge for a share, a 44×13 row-end sparkline for real wages, productivity and house prices (the indices and levels that keep a history — a base-100 index is unbounded, so its history is the honest instrument), nothing for life expectancy; the figure mono 10.5; the unit as a muted caption; the USA's overburden row **drawn as absent by ruling** (`ABSENT BY RULING · NOT ZERO`, the name muted, no figure); **`AS PUBLISHED`** with E19's sentence retired for a **KEY on the rule** — the PRELIMINARY chip in the caution ink, the dashed frame in the fill amber, the FINAL chip in the secondary ink, the same three marks the graphs draw — the three monthly/quarterly series in the 3-column grid, the poverty bulletin (E18) beneath the third. **E24 dropped** from International; the "Domestic" / "International" headers dropped (the sub-tab says it); the sub-tabs keep their delivered faces (one form across the three sub-tabbed screens — the board drew desk chips); instrument type at the board's px scaled from 720 (R-B10's law), not the body clamp. `DrawHeadlineStatTiles` and `DrawDerivedStatsRow` retired; `PoliSimWidgets.StatTile` stays a widget.

**Findings measured on the way, each fixed in its unit:** (1) **the paper sprite's drop shadow was inside the box rect** — `ui_panel_paper` is opaque only from 14 px in at the left and right, 10 at the top and 26 at the bottom (the PNG's alpha, drawn 1:1 by the 9-slice), so every padding token counted from the rect, not the paper: the visible padding was 0/0/2/−12 at 14/14/12/14 and would have been −4/−4/−2/−16 at D4's numbers — content flush with the paper's edge and the last row of every scroll view standing in the bottom shadow, since v2.0. `StyleBoxAsPaper` now sets `overflow` 14/14/10/26, the shadow falls outside the rect as a drop shadow should, no layout width moved, and the tokens mean what Annex C and D4 took them to mean (Annex C's base was wrong by the shadow — filed back to Design in the request doc's status). (2) **The stage took the box reserve twice** (`InnerHeight` on a height the frame had already reduced) — a dark band under the sheet on the first 1m-r2 film; the sheet now stands as tall as the rail. (3) The bold caption wider than its cell (unit 4).

**Reversible calls, one line each.** R-F1 D4's two box rows applied as the explicit 10/10/8/10 (20 px per level); R-F2 the paper's shadow outside the rect (`overflow`, four measured literals); R-F3 the one-line token moved and the law browser held (`LawBrowserRowHeight`); R-F4 `Draft` its own literal (#BE8A00) — fills; text uses take `Caution`; R-F5 the BREAKING banner on the dark desk keeps the fill amber (light on dark); R-F6 the selected chip's caption TextPrimary on brass, kept at the measured 4.03; R-F7 the rail caption at 7.5 of the cell floored at the guard's 8, the active weight yielding before the size, the home's active caption TextPrimary bold (not brass on paper), a faint stock wash under the cursor; R-F8 the no-draft caption's claim aligned to the model; R-F9 `FIRST ATTRIBUTION` prints the boundary from the calendar's own arithmetic; R-F10 `YEAR N IS QUIET` past Year 0; R-F11 2a's sub-tabs keep the delivered faces; R-F12 the fiscal axis = max(30 %, the group's maximum to the next 10 %), printed; R-F13 the "Domestic" / "International" headers dropped and the graph titles shortened with the dashed note in the section caption; R-F14 the overburden row drawn as absent by ruling; R-F15 E24 dropped (the turn log's field stays, written by the loop); R-F16 the stage's box laid out at the reserve plus its padding. **RULINGS NEEDED — one:** the hatch pair's bar (unit 1): name it "diagonal-tile, viewed not counted" with the measurement on record (the recommendation — the three text stamps' treatment), or ask Design a fourth cut at ≈ 8.1 px duty, or refine STRUCTURE to solid-vs-void (which still reads 1.17 % against 1 %).

**Not in scope, by the boards:** redrawn rail glyphs (refused, costed); the saves and the rest of the sitting; 13 September.

**Unit 7 — the OPEN state deleted (§44's promise: "v3.1 Phase B deletes them").** After the boards' bar came back green, the v3.0 shell fold's residue went out whole: in `GameController.cs` the OnGUI OPEN branch (the chrome column at 45 % of the area with `DrawTopBanner`, `DrawCalendarPanel`, `DrawPolicyControls`, `DrawCalendarAndSpeedControls` — with `DrawRunningStatusPlate`, `DrawSpeedButton`, `CalendarAndSpeedControlsHeight`, `BuildTimeStatusText` — and the tongues: `DrawConsolidatedTabs`, `DrawConsolidatedTabButton`, `DrawActiveFolderTongue`, `ConsolidatedTabRowHeight`, the seven `_activeTongue*` fields), the fold toggle (`DrawFoldToggle`, `FoldToggleWidth`, the two glyphs), the fold state itself (`_shellFoldOverrides`, `CaptureShellFoldOverrides`, `ShellScreenKey`, `DefaultShellFold`, `ShellFoldLocked`, `EffectiveShellFold`, `SetShellFold`, `ToggleShellFold`, `LeftColumnWidthFraction`, `_leftColumnScrollPosition`; the Desk's `DeskScreenKey`), the enum `ShellFoldState.cs` with its meta, `UiDraftState.ShellFoldOverrides` (a v3.0 save carrying the key is read past it — `JsonUtility` ignores members the class no longer has; nothing to restore), and in the harness `SweepOtherFoldState`, `CaptureFoldPair` with their six call sites and the "locked FOLDED" half of `AssertDeskState`. 22 members removed by a brace-matching script (`remove_methods.ps1`, the pass's scratch; dry-run first, then the compiler and a grep as the proof), the rest by hand; `DrawPolicyPreview`, `DrawCalendarMonthGrid`, `BuildCalendarMonthMarkers`, `_tabButtonStyle` and the tab-icon constants stay — the Desk, the Budget screen and the rail read them. OnGUI now reads as the frame it draws: the rail, the gap, the banner-measured sheet. Kept on purpose: `_runningPlateStyle` and `RunningLampSize` (styled per frame, drawn by nothing — one accessor the hold banner's padding shares) and the historical comments that name the old methods. The `.meta` Unity generated for `GameController.Statistics.cs` rides this commit (unit 6's path list missed it).

**The gate — the full matrix, twice.** The boards' bar (`v31b_*`, the code at `96bc38e`): 79 captured at each of 1280 / 1600 / 1920 / 2560 (the 78-screen sweep plus Screen 0's four frames), 0 text overflows, 0 containment escapes, 0 canvas text violations, `ScreenEdgeCheck` 0 clipped, ATTRIB 0; trajectories `traj_v31b_*` ≡ `traj_v31_*` ≡ `traj_v3c_*` 6/6 by SHA-256 (nothing here is simulation); the eight checks green. The final bar (`v31bf_*`, the code at `c3b7c63`, after the deletion): **the same result line for line** — 79 / 0 / 0 / 0 / 0 clipped at every size (the 1600 run took ten minutes to a clean exit — the watch-line class, recorded, not touched), 12/12 identical, the eight checks green, `UpstreamCheck` at 8 ahead and tracking (cleared by the push). Between the two families the film differs only where a run differs from itself: the Fed-chair candidate draw (three names on the Decisions and Federal Reserve sheets — a UI-side unseeded roll; the layout identical) and the cursor (842 px in one 66×24 box on the 2560 Desk); the dead-space shares re-measured on `v31bf` sit within 1 pt of `v31b` for that reason (`deadspace_v31bf.md`). *A candidate for the harness, not done: seed the chair draw so two films of one code are pixel-identical.* The single-size probes along the way: `v31b_probe` (D6 + D4 + the rail — the bold POLITICS caught), `v31b_desk` / `v31b_swe2` (the first 1m-r2 film — the shadow and the double reserve caught), `v31b_desk2` / `v31b_swe2` (clean), `v31b_stats` (2a, clean), `v31b7` (the deletion, clean). **The dead-space re-measure (D4's own ask):** at 1280 the Desk 43.9 % (43.5 before), Domestic 42.1 (44.2), Decisions 27.2 (17.6), Demographics 67.6 (58.6), Budget 28.6 (28.8), Laws 42.5 (35.1), Compass 57.9 (55.6); at 2560 the same shape — the spacing cut compacts a fixed quantity of content, so the share of empty paper RISES on content-short screens and falls only where a screen was re-composed (Domestic); filed back in the request doc's Annex C with the candidates for Design's next look (Demographics, Decisions, the short Politics screens). `sv_index.html` regenerated (39 rows). **The push:** R-SP1's fast-forward at the pass's end, the outcome in the report.

## 46. The hatch ruling executed — "diagonal-tile, viewed not counted"; §E5 closed end-to-end, both sides (2026-08-28)

**The ruling (Elias, answering §45's one RULING NEEDED):** the hatch pair takes the three text stamps'
treatment — named **"diagonal-tile, viewed not counted"**, the third cut's measurement standing as the
record, no fourth cut asked, the classifier untouched (the ruling's own condition — no classifier change
without it — honoured by making none).

**Executed in `StripCutDiffCheck`.** The ruling's category exists as a second named table,
`ViewedNotCountedPairs`: pairs whose over-budget reading is MEASURED renderer difference, not drift —
still measured, still printed with their figures every run, marked VIEWED, never a FAIL and never a
pass; the exemption by name (or the stamps' structural `<text>` mark), never by class, so a new defect
anywhere still reads FAIL. `ui_hatch_draft` moved there from `DeferredPairs` carrying re-cut #3's
measurement (7.42 % structure, edge 0.02; 64 of the 76 mismatched px straddle alpha 128 — the two
rasterizers' coverage of a 45° edge on a 32 px tile; 12 solid-vs-void = 1.17 %). `DeferredPairs` stands
EMPTY — the deferral retired, R-D3's mechanism kept for the next ask-on-file case — and the named branch
runs before the deferral check, so a ruled pair can never silently re-enter DEFERRED. The summary line
prints ONE viewed-not-counted figure (text-bearing + ruled by name).

**The suite, verified at HEAD** (resvg 0.47.0, `stripcut_ruling_20260828.log`): **86 of 90 comparable
pairs within budget; 4 viewed not counted (3 text-bearing fonts + 1 ruled by name); 0 deferred; 0
unrasterizable-here; 0 FAILED — exit 0.** Green with four viewed-not-counted and zero deferred — the
ruling's exact shape.

**The records, both sides.** `MISSING_PREREQUISITES.md`: the register row and §E5 read CLOSED
end-to-end (Design's half — three cuts, the slider strip sourceless-by-design — closed in the day's
earlier record; Elias's half, the bar ruling, closed by this pass) and retire at the next re-derivation;
**the pair's eye-diff joined §V** (`%TEMP%\stripcut_fail_ui_hatch_draft.png`, our resvg rendering
rewritten each run, beside `Assets/Resources/Art/UI/Chrome/ui_hatch_draft.png` — a shape difference
visible by eye would contradict the ruling's premise and send the pair back to FAIL). The roadmap's live
edge and §E register carry the close. The request doc is deliberately untouched: the §S package's
recorded digest must keep matching the pasted file, and the bar was Elias's own question, never an ask
to Design.

## 47. The film seeded and the cursor parked — determinism for rule-15 diffs (2026-08-28)

**The ratified candidate** (§45's gate paragraph: *"seed the chair draw so two films of one code are
pixel-identical"*). `UiScreenshotDriver.Start` now calls `SimulationRandom.Seed(777)` — `FilmSeed`, the
trajectory baselines' own standing seed — before the game advances a single day, so the Fed-chair
candidate draw and every other `SimulationRandom` consumer the warm-up touches replays the same sequence
every run; and `ParkCursor()` (the harness's one P/Invoke, `SetCursorPos`) parks the OS cursor at the
primary screen's top-left before the first capture — the Game View is laid out at the screen origin by
`UiScreenshotCapture.ResizeGameView`, so (0,0) is its tab strip, never a game pixel, and under any
future layout it is the SAME spot every run, deterministic either way. The two variance sources the
v31b/v31bf compare named (the chair names on the Decisions and Federal Reserve sheets; the hover box
under the resting mouse — quirk 16's class) are both closed.

**Measured: two full sweeps of one code at 1280×720** (`det_a_*` / `det_b_*`, each 79 captured / 0
failed / exit 0, the seed and the park both on the log): **76 of 79 frames byte-identical by SHA-256.**
The three that differ are WALL-CLOCK frames, each with its cause on record: `01a_selector_yielding`
(the scrim caught mid-envelope — the driver's own comment: alpha varies with frame rate; presence is
what the capture pins, not a pixel value), `89d_signing_entrance` (the document caught mid-rise — the
same staged-envelope class), and `92_saves_menu` (the two `zz_driver_capture_*` saves the harness
itself writes carry the real save timestamp — the A film prints "saved … 19:18 UTC", the B film
"19:19 UTC"; both films viewed, the minute the only difference on the frame). A rule-15 diff on those
three names reads the clock, not the code; on the other 76 it now reads nothing unless the code moved.

**One deliberate family discontinuity:** the first seeded family's random-dependent surfaces (candidate
names, event timing, publication noise) differ ONCE from every unseeded family before it (`v31bf_*` and
back), and never again between themselves. Byte-comparisons of capture families are meaningful from
`det_*` on.

## 48. The Policy Web micro-pass — bigger now, understandable via board 2b (2026-08-28 late night)

**The authority:** Elias, from the sitting — *the Policy Web should be bigger, more understandable, and
use the page's dead space* — split per the R-E2 precedent: scale structural and same-day (R-W1),
comprehension to Design as ask D7 (board 2b) against measured annexes (R-W3), the finding §V's third
row first (phase 0, `c6cd7d3`).

**R-W1 built (`3b85543`).** `DrawPolicyWebTab`: the diagram rect IS the scroll viewport — full width,
the viewport's own height, drawn FIRST, floored at the old half-screen minimum, the 0.92·h ceiling gone;
the explainer paragraph follows the web below the fold; same nodes, same edges, same clicked-node idiom.
Two build calls, one line each: the in-sheet duplicate "Policy Web" title dropped (the frame's caption
names the screen); the explainer relocated, not reworded. `PolicyWebCensus` rode along (Annex G's
counting half; batch, rendering-free, from the same public API the screen draws).

**The proofs.** `pweb_*` filmed at 1280/1600/1920/2560 — 79/0 each; occupancy before → after: 41.1/46.2/
48.2/74.6 → **56.1/59.8/62.1/74.6 %** of window (43.6/48.9/51.0/78.7 → 59.6/63.3/65.6/78.7 % of sheet;
plate 1120×328 → 1120×448 at 1280 — the fold-clip at rest is gone; 2560 unchanged, the old ceiling
already filled it); rule-15 byte-diff vs `det_a`: 68/79 identical, the SEVEN policy-web frames the only
code-caused differences, and `01_country_selector` joined the time-envelope class (the Italy card a few
px lower mid-settle — pixel-diffed to bbox (465,392)–(814,615), both films viewed; an IMGUI-only change
cannot move a Canvas card); trajectories `traj_pweb_*` ≡ `traj_v31bf_*` 6/6 by SHA-256; `ScreenEdgeCheck`
316 captures 0 clipped; the eight checks green (run as eight `-executeMethod` invocations — `CheckSuite`
has no batch entry, a wrong-entry-point detour recorded in the overnight report).

**The census (Annex G.4).** 73 nodes = 55 policy + 18 stat (Fiscal 28 the giant wedge; Trade and
Political 1 each); policy→stat 121 = 73 derived + 48 declared (USA 120 — `IsLiveFor`'s one predicate);
stat→stat 7, all derived; one edge-less node by name (Tariffs (Tax Line)).

**D7 installed (`e30c82b`) and the package regenerated.** The request doc's §2: board 2b at 1280 first,
against Annex G (`7959477`) — the full-sheet composition (the ring's height-bound geometry is the
board's question), a legend (flagged new UI content), weight/arrowheads from the model's own magnitudes,
the clicked-node pane's composition with fixed contents; R-W2's constraints written into the ask. The
status line flipped to ONE LIVE ASK; `SEND_PACKAGE_2026-08-28.md` regenerated (rows n of N, digest
`85690abf…`, 65 004 bytes CRLF; the courtesy note unchanged). Sending stays Elias's, one paste.

**RULINGS NEEDED: none.** The dead-space paradox's other screens (Demographics, Decisions, short
Politics) stay Annex C candidates, exactly as filed.

## 49. Elections Day-1 — the seat rung closed at 5/6, the vote rung opened at 3–7 pp (2026-08-29)

**Phase 0 stopped the spec-dependent half again.** `ELECTIONS_CAMPAIGN_SPEC.md` did not arrive
with the Day-1 kickoff either (0 sections found; searched repo, captures + inbox, Downloads,
Desktop, Documents). Phases 1–2 stayed parked — the 44-section gap table, §7's types, §39's
chain — unguessed, exactly as the overnight pass had left them. Phases 3–5 ran.

**R-EL8 — the USA's real allocation (`ElectoralCollege.cs`, pure, unwired).** Winner-take-all as
the STATE CHOICE 48 states and DC direct, plus Maine's and Nebraska's congressional-district
method, from the statutes (Me. 21-A §802; Neb. §32-710 with §32-1038(1); art. II §1 cl. 2; NARA
538/270). **Trump 312 / Harris 226 — EXACT**, computed from seven sourced ME/NE pluralities
rather than a pre-split column. The counterfactual that justifies the ruling prints beside it:
forced winner-take-all *also* gives 312/226, because ME-2 and NE-2 cancel one each way — the
overnight match had been luck, and now it cannot be mistaken for validation. Three items recorded
rather than quietly fixed: the kickoff's own Nebraska cite (§32-714) is the vacancies section,
not the district method; Nebraska's statute text is an Internet Archive capture (the legislature
host refused connections) corroborated by the SOS canvass book; and **LB3 may repeal the district
method** (`[UNCONFIRMED]`) — do not model Nebraska as permanently district-method. Maine's
presidential race is legally RCV-eligible (§1(27-C)(D)) and was decided in round one only because
Harris cleared §723-A(2)'s inclusive 50 % bar at 51.71 %.

**R-EL9 — Italy's Rosatellum (`Rosatellum.cs`, pure, unwired).** The allocation arithmetic the
overnight pass refused to guess is now sourced (DPR 361/1957 art. 83 consolidated, in force at
25-9-2022) and implemented: **floored Hare with largest remainders, applied twice** — which is
why it needed its own implementation rather than the divisor machinery. **ITALY 2022 CAMERA,
proportional stage, 245 seats: deviation 0 across all eleven lists**, reproducing three statutory
behaviours at once — the 1 % strip (NM, IC discarded), the 1–3 % transfer (+Europa's votes to PD
and AVS), and the minority route (SVP-PATT admitted at 0.42 %). An ambiguous clause was settled
by arithmetic, not preference: lett. g)'s divisor is the ADMITTED lists' sum; the coalition-figure
reading is nine seats out. The two tiers are **parallel — no *scorporo***. Sub-national stages
(lett. h/i, art. 83-bis, art. 84) stated NOT implemented with their reason and data requirement;
the comune-level CSV trap recorded (its sums undershoot the *cifre elettorali* by 2.6–4.6 %).

**The seat table now reads FIVE OF SIX EXACT** — Sweden, Germany, Poland-real, Italy-PR, USA-EC —
with France reported honestly as structurally out of scope (two-round SMD in 577 constituencies
has no national allocation to implement).

**Phase 4 — the vote rung, the day's centerpiece (`VoteModel.cs` + `VoteShareBacktest.cs`).**
Sourced CHES 2024 / GPS-2019 positions and EB105 / Gallup salience through a declared placeholder
instrument — a 2-D Gaussian electorate, proximity choice by softmax, **four parameters and not one
party-specific constant** — against the official returns, campaigns off. Multiparty fields:
**Sweden 3.25, Italy 5.61, Germany 5.78, Poland 6.99 pp** mean absolute deviation calibrated (from
6.4–10.3 at the zero-parameter prior); France 1.16 and USA 0.00 reported as structure, not skill
(4 blocs / 2 parties). **The deviations have two systematic signatures — empty-quadrant inflation
(BSW +10.2, TD +15.9, KD +8.2) and large-party under-prediction (CDU −9.3, AfD −11.7, KO −16.7,
M −10.0) — which are one absence stated twice, and name §8 loyalty damping as the highest-value
unit in the plan; Germany's CSU +7.4 names §27 regional structure as the second.**

**R-N2 held at every part:** four proof runs today (`d1p4`, `d1p3a`, `d1p3b`, plus the closing
one), each with the trajectory dump exit 0, the six baselines byte-identical by SHA-256, and all
eight checks exit 0. Nothing of the election system is wired to anything. Commits `cb17c85`
(Phase 0 + 4) · `bd34c8c` (Phase 3) + the close. Full record, call log and RULINGS NEEDED:
`ELECTIONS_DAY1_REPORT_2026-08-29.md`.

## 50. The spec lands — E-0 closed, the §39 chain's first half built (2026-08-29, later the same day)

**`ELECTIONS_CAMPAIGN_SPEC.md` arrived on the third attempt and passed Phase 0's content check**
(44 sections; §42 the causal chain; §44 the last) — installed verbatim at root before anything was
built on it, exactly as the gate required.

**Phase 1.** `ELECTIONS_GAP_TABLE.md` classifies all 44 sections — **EXISTS 3 · EXTENDS 10 ·
NEW 22 · N/A 9**, every N/A with its one-line reason (R-EL7 live; six are principles or
illustrations, and §40 is the one N/A-by-ruling). Three findings the classification surfaced, each
turning a "NEW" into an "EXTENDS": **§19's actual-vs-perceived split already exists** as
`PublicationSystem`'s preliminary/revised figures; **§31's "why you won" table is the approval
attribution ledger** pointed at a vote share; **§24's regional data is already sourced** for six
countries, so regions are modelling rather than research. **D0 reconciled:** this spec IS item
10's political model — `PartyArchetype`, `TotalSeats = 200` and `ElectionSystem`'s approval
threshold retire; seat drift, bill scoring and the renderers survive; `PublicationSystem` is
promoted, not replaced; wiring stays R-N2-forbidden.
**§7 built** — `ElectionTypes.cs` (§41's field lists as plain value types) + `Compatibility.cs`
(the five-term formula, [AUTHORED-DRAFT] weights logged). Two decisions carry weight: **undefined
axes are skipped, never centred** (CHES fills three of eight; padding the rest with 50 would
invent party positions), and **a missing sub-score's weight is redistributed** so sparse profiles
are not punished. The party-side scalars sum to only 0.30 deliberately — **campaigning cannot
outrun positioning**, which is §44's design question expressed as arithmetic. Harness: **9/9.**
**§40 diverges by ruling, not preference:** the spec's ScriptableObjects and 13-manager
MonoBehaviour tree lose to R-EL1 (catalogs in code) and R-N2 (no wiring); §40's actual point,
modularity, is kept as one concern per pure static class.

**Phase 2 — the §39 chain's first half, all pure and unwired.** `PreferenceModel.cs` (§8:
`λ·prior + (1−λ)·persuaded`, Sharpness 3.0 and a compatibility floor so the hardest targets stay
reachable) · `TurnoutModel.cs` (§26's five-factor product, spans 0.30/0.20/0.15/0.15 — deliberately
unequal so organisation outweighs charisma and §10's offices are worth building; no party-specific
term, so §31 can one day say "you lost on turnout, not persuasion" and mean it) ·
`RegionalAggregation.cs` (§27, with `Final = Expected + Noise` applied REGIONALLY at σ = 1.2 pp).
`SimulationRandom.Stream` gained **`ElectionNoise = 7`, appended** per the enum's own rule — and
the proof that followed confirmed **6/6 byte-identical baselines**, so no existing stream moved.

**Harness: 20/20, and two results carry the day.** **§8 reverses both Phase-4 deviation
signatures**: the empty-quadrant newcomer (which over-predicted BSW by +10.2 pp and TD by
+15.9 pp that morning) falls from **60.19 % to 12.27 %** as loyalty rises, while the large
incumbent rises from **25.22 % to 53.86 %** — a measured defect, a named cause, a built fix, and a
test that the fix moves the number the predicted way. And **regional noise partially cancels
nationally** — 0.9482 pp regional σ against 0.3474 pp national over 400 replays of 8 regions,
within a hair of 1/√8 — so national polling out-predicts constituency forecasting because of the
model's structure, not because a constant was tuned to say so.

**R-N2 held across six proof runs today.** Commits `b88cff5` (Phase 1) · `12662c6` (Phase 2).
The Day-1 report's addendum carries the full record; RULINGS NEEDED #1 (paste the spec) is closed,
the other four stand.

## 51. Elections Day-2 — the four verdicts, §27 built, and the wiring gate FAILED (2026-08-29)

**The headline: R-EL13's gate FAILED, so nothing was wired.** Three countries improved, some
sharply; **Italy regressed 5.61 → 6.69 pp**, and the gate required that none regress. The ruling
worked exactly as written — a model better in most places and worse in one did not go live because
a schedule wanted it to. Wiring waits; R-N2 still stands, unbroken.

| country | Day-1 | best Day-2 | verdict |
|---|---|---|---|
| GERMANY-8 (like-for-like) | 5.78 | **4.66** (+§8) | IMPROVED |
| SWEDEN | 3.25 | **1.75** (+§8) | IMPROVED |
| POLAND | 6.99 | **3.84** (+§8) | IMPROVED |
| **ITALY** | 5.61 | **6.69** (+§8) | **REGRESSED** |

The **seat half passed unchanged** — five chambers still reproduce at deviation 0, the deliberate
national-Poland signature still exactly 70.

**Why Italy regressed, reported rather than tuned away.** Italy 2018 → 2022 is the most volatile
pair in the set: FdI grew **6.7×** (4.35 → 29.27 %) while M5S **halved** (32.68 → 17.38 %). A
uniform loyalty of 60 asserts that ~60 % of voters vote as before, which for that election is
simply false — so §8 damped FdI down (dev −19.25) and held M5S up (+12.10). **The layer is right;
the global constant is wrong**, and the spec agrees: §5/§8 make loyalty a per-voter-group
attribute, not one national number. **The constant was NOT re-fitted to open the gate** — the
kickoff forbids it, and a constant tuned until the gate opens would make the gate meaningless.
`Loyalty = 60` was chosen a priori as the spec's own middle rung and never varied.

**Where the layers did work.** §8 corrected the deviations Day-1 named it for — BSW +10.18 →
**+0.94**, Poland's TD +15.93 → **+2.58**, Sweden's M −9.97 → **−3.54**, KD +8.16 → **+3.86**.
**§27 corrected the CSU deviation from candidacy facts alone** (+7.36 → **−3.68**, no fitted
parameter): the CSU contests one Land of sixteen, and once the model knows that, its predicted
share falls from 13.62 % to 2.57 % against 6.26 % actual. One honest limit found: §27 and §8 do
**not** compose yet — Germany's both-layers run (5.01) is worse than §8 alone (4.55) because the
regional run damps each region toward the **national** prior. Per-region priors are the fix, and
that is a data item, not a model defect.

**A flaw in my own test, found and corrected before reporting.** The first run compared a
nine-party Germany (SSW added for §27's availability case) against Day-1's eight-party 5.78.
Corrected with **GERMANY-8**, Day-1's exact set, whose run A recomputes to **5.78 — matching to
the digit**, which is also the proof the harness reproduces Day-1 rather than quoting it. The
nine-party run is retained as the §27 demonstration and explicitly excluded from the gate.

**Part 1's verdicts.** R-EL10: France recorded **structurally out of scope with its reason**, plus
a named unsized roadmap item; no placeholder, no approximation. R-EL11: Italy's sub-national
stages billed as *before playable, not before trusted*. **R-EL12: Nebraska LB3 RESOLVED — NOT
ENACTED** (cloture failed 31–18 on 8 Apr 2025; indefinitely postponed 17 Apr 2026; LR24CA never
floor-debated; the initiative route withdrawn June 2026). The district method stands, so no
variant was needed — but the ruling's forward half is recorded (a **dated variant** if it ever
changes; `ElectoralCollege.Jurisdiction` is already shaped for it), with its **expiry** (110th
Legislature, January 2027) and its **sourcing gap** (the Legislature's host refused connections
and the Archive is tool-blocked; three agreeing independent lines, not the journal of record).

**Part 2 built** `RegionalVoteModel.cs` (pure, unwired) on a new sourced catalog,
`land_votes_2025.csv` — per-Land absolute Zweitstimmen from the official `kerg2.csv`, sums
cross-checked exactly (valid 49,649,512; CDU 11,196,374; CSU 2,964,028), its zeros carrying the
candidacy facts. Per-region electorate *positions* were deliberately not fitted: that would be
circular in a backtest, so every region runs the national electorate and §27's improvement is
structure, not tuning.

**R-N2 held:** dump exit 0, baselines byte-identical **6/6**, all eight checks exit 0. **Part 4
was skipped by rule and there is no revert handle, because nothing was wired.** Commit `4301bdc`;
full record, call log and four RULINGS NEEDED in `ELECTIONS_DAY2_REPORT_2026-08-29.md`.

## 52. W-E1 — Campaign HQ, the first screen of the Track E class (2026-08-29)

**What shipped.** `Assets/Scripts/UI/GameController.Campaign.cs` — Campaign HQ, drawn in the v3
idiom: the folded rail plus one full-bleed sheet, the Desk's own 1156×680 board and its three
columns at 440 / 250 / 440, type scaled by `DeskPx` and floored at D4's 9 px, plate captions at
8.5, ledger rows at the Desk's pitch. It reuses `DeskCaption` / `DeskCaptionWrapped` / `DeskBody` /
`DeskNumeral` / `DrawDeskChipButton` / `DeskCaptionHeight` directly rather than restating them, so
the two stages share **one** type ladder. Six plates: resources (§9), organisation (§9 staff, §10
offices), the race as polled (§20–§22), today's queue (§12), what the phase permits (§3), and the
campaign-window strip.

**R-N2 is held structurally, not by convention.** The screen draws only when `_campaignScreen` has
a value, and the only setter is `internal void SetCampaignScreen(CampaignSnapshot?)`, called by the
screenshot driver and nothing else. The branch sits beside `_onDesk` inside the frame's content
column — so the rail is the real rail and the sheet is composed in the frame it will ship in — but
there is no rail cell, no tab, no save hook and no gameplay path that reaches it. Wiring at W-G1 is
*adding the rail cell*, and nothing else. Same shape as the `DrawInstrumentLadder` precedent.

**Every figure is derived, and the boundary is stated rather than blurred.** The screen lays out a
`CampaignSnapshot` (new, `Assets/Scripts/Elections/CampaignSnapshot.cs`) and computes nothing of its
own beyond summing what it is handed. In the filmed states: the poll is a real
`PollingSystem.Conduct` draw against Sweden's **SOURCED** 2022 vector (Valmyndigheten's final count,
`ElectionsData/sweden/returns_2022.md`); the ± comes out of that draw; momentum is a real
`MomentumTracker` shock decayed on §22's half-life; every queued action's cost is read from
`CampaignActions.Spec`; the legality list is `CampaignLegality.LegalActions`, derived and never
restated; the perceived-economy index is `PerceivedPerformance.Perceived` read off the **live**
warmed-up country (31.0 / 100). The war chest, volunteer counts and office upkeep are
**[AUTHORED-DRAFT]** staging, logged as such by the pass itself, with W-F5 named as what sources
party finances. No spec illustration ships as data.

**W-B10's rule reaches the view layer through the type.** The screen is handed a `Poll`, which
*cannot express a truth* (proven by reflection in W-B10's harness), so "the UI never sees the truth"
is enforced by what the view is given rather than by care at the call site. The ± is drawn as a
shaded band **under** the point estimate rather than printed beside it: the interval is the width of
what is actually known, not a footnote to a number that looks exact.

**The five `mark_party_*` sprites got their first call site.** `IconLibrary.GetPartyMark` is added
as the one-line wrapper over `Resources.Load` that `PartyMarkCoverageCheck`'s own comment already
promised, and it takes the **full file stem** (`"mark_party_se_s"`) — the same key that check passes
to `Resources.Load` and compares against file names. A suffix-based accessor would have been a
second naming convention beside the check's; when the party system's seeds land on `main`, their
mark names now feed both without translation. Marks are drawn untinted, per rule 9a and the
accessor's contract.

### Two defects the film caught, fixed at the measurement rather than by shrinking the type

1. **`CampaignActions.Spec` throws for §3's preparation verbs.** The staging pass built the queue
   from `CampaignLegality.LegalActions(phase)`, which in the campaign phase legitimately includes
   `RecruitStaff`, `Fundraise` and the rest — but only §12's eight have specs, so the first film
   died mid-capture on `ArgumentException: RecruitStaff is not one of §12's eight campaign actions`.
   The queue is now built from `CampaignActions.TheEight` **intersected with** legality, which is
   what §12's queue actually is; a pre-campaign day therefore shows the empty state, correctly.
2. **Two label-clipping instances of the known class**, both at the caption floor. The momentum
   caption was given a board-derived 9 px slot while its glyph box is 11.2 px at the 9 px floor
   (`lineHeight < CalcSize height` — the recorded IMGUI fact); it now takes `DeskCaptionHeight`, and
   the race row's pitch takes the caption height too, so the caption can neither clip nor collide
   with the next party's name. The poll's methodology line was drawn with `MeasuredLabel`, whose
   overflow guard measures the **one-line** form and so reported a genuinely wrapping caption as
   "needs 503.7 wide in 248.3"; it now follows the Desk's established pattern for a wrapped caption
   (`GUI.Label` plus `UiContainmentGuard.Check` against its own wrapped height), and the block is
   measured **before** the rows are budgeted so the reserve is its real height rather than a board
   figure that stops matching once the caption floor stops scaling.

   The methodology line was **not** trimmed to fit. "(SAMPLING ERROR ONLY)" is the honest scope of
   the ± — it is precisely the thing published margins of error understate — and dropping it to save
   a line would have made the screen overstate what the poll knows.

### Calls logged (R-N1), each strikeable

- **A mark key is the full file stem, not a suffix** (above).
- **Staff rows carry no personal names.** Inventing people is inventing data; the ledger shows the
  post, whether it is filled, and the draft bonus. Names belong to W-B5 with their own sourcing.
- **The race bars scale to the leader's band, not to 100 %** — eight parties none of which clears a
  third would otherwise read as eight stubs — and the axis is **named** in the methodology line,
  because an unlabelled rescaled bar is a lie by omission.
- **The masthead chips are drawn `disabled`.** Nothing is wired, and a chip that looked live while
  doing nothing would be the worse lie.
- **An over-committed queue is shown as over-committed.** `ResourcePool.TrySpend` refuses rather
  than clamping (W-B2), so a queue genuinely can be unaffordable; the screen says so in the caution
  ink instead of printing a plausible total.
- **`-shotcampaign` demands `-shotcountry=Sweden` and fails the run otherwise.** The staged returns
  are sourced *as Swedish*; filming them under another country's frame would put real Valmyndigheten
  figures beside the wrong flag.
- **`PartyMarkCoverageCheck` exits 0 while verifying NOTHING, and that is not evidence for the new
  accessor.** The check enumerates *seeded parties*, and `PoliSim.Data.PoliticalParty` does not
  exist on `main` — it says so loudly ("VERIFIED NOTHING; this is not evidence of coverage") and
  passes, correctly, because there is no claim to falsify. The evidence that `GetPartyMark`'s key
  convention is right is therefore the **film**: `mark_party_se_s` resolves and draws in the
  masthead at all four widths. When the party branch lands, that check becomes the real test of
  the convention, and it will test the same key this accessor takes.

### Two more the film caught on being LOOKED at, not on a guard

3. **Money rendered without thousands separators** — the war chest filmed as `1120000`. `UiFormat.Number`
   is a plain `F0` and `UiFormat.Money` is dollar-prefixed and tiered (`$1.12M`), so neither fits a
   sheet denominated in kronor at full precision; a local `Kronor` helper (`N0`, invariant, matching
   every other numeric site here) now formats all five money sites. A seven-digit hero numeral with
   no separators is illegible at a glance, which is the one job a hero numeral has.
4. **The middle staged day was itself over budget**, so the first film showed the over-committed
   reading twice and the normal one never. It is re-staged at exactly the 12 h `StartDay` grants
   (Rally 4 + Town hall 3 + Door to door 5 = `340,000 kr · 12 of 12 h`, no caution), so the three
   captures now show three distinct readings: nothing yet, a full but affordable day, and a day the
   resource system would refuse.

   Both were found by **looking at the film**, not by a guard — the guards were already silent. That
   is the argument for the screen class filming at four widths rather than asserting at one.

### Findings recorded, not chased

- **The sheet reads bottom-empty in all three columns at every width.** Per the standing dead-space
  ruling this is a recorded finding and a Track H Design line, not a gap to fill with invented
  content; the v3 stage's text budget applies and inventing rows to fill space would break it.
- **`mark_party_se_s` reads faint on paper at 24 px.** The mark is drawn untinted by rule, so this
  is a Design-ask line about the art's value range, not something the screen may correct.
- **§12's `Interview` costs `0 kr · 2 h`** and films that way, which is exactly the asymmetry the
  W-B3 review ruled belongs to W-B9 as *media interest* — a party nobody covers cannot buy its way
  onto the air — rather than as a cost bolted onto the action.

## 53. W-E3 — the action screen, and an estimate that earns its range (2026-08-29)

**The item's bar was "ranges, never false precision", so the range was built before the screen was.**
`CampaignActions.ResolveBand` evaluates §42's chain at the low, mid and high ends of the inputs the
player ACTUALLY measures — polled salience and issue-match. The audience is structural (Sweden's
sourced 2022 electorate), credibility is the party's own record and spend is chosen exactly, so the
only uncertainty in the estimate is the uncertainty the player really has. The band's width is
therefore a propagated measurement, not an authored ±, and buying a bigger sample (§21) visibly
narrows it.

**`ChainBandHarness` proves the shortcut instead of asserting it.** `ResolveBand` evaluates only two
CORNERS of the uncertainty box, which is exact only while persuasion is monotone in both inputs —
true today because salience and match enter in exactly one place (`relevance = salience × match`)
and every later stage multiplies by a non-negative factor. A comment claiming that is worth nothing,
so the harness sweeps a 41×41 grid for each of §12's eight actions — **13 448 interior points, none
outside the band** — and the method's own doc says plainly that if a future stage breaks monotonicity
this must become a sweep. Also asserted: a wider measured interval gives a wider estimate (span
1 034 vs 103), a PERFECT measurement collapses the band to a point (span 0.000E+000), an unpolled
quantity yields **no** estimate rather than a wide one (§36), and W-B3's structural bar survives the
widening — reflection over `ChainBand` still finds no share, no preference, no party.

**The screen.** Same board, same idiom and the same primitives as Campaign HQ, reused rather than
restated so the two campaign screens cannot drift into two dialects. Three columns: §12's actions
with what each costs and what each would buy; **§42's chain, stage by stage** — the first time that
architecture is visible to anything but a harness, with the invariant printed ("zero any stage and
the effect is zero"); and the selected action's estimate with its provenance.

**Every option carries its own band, on ONE shared scale.** The decision this screen serves is
*which* action to run, and that is a comparison — a screen that prices only the row you already
clicked makes the player click all eight to find out. A per-row scale would have made a 5 000 kr
social post look exactly as promising as a 500 000 kr television buy, so the scale is shared and its
top is printed.

### Five defects, and how each was found

**The guards were silent for all five.** Every one passed `UiContainmentGuard`, the overflow guard
and `ScreenEdgeCheck`, because all five fitted their rects perfectly. That is not a failure of the
guards — they answer "does the text fit" — it is the concrete case for why this screen class films
at four widths and why somebody reads the film.

1. **`ENTHUSIASM PRESSURE  3,477 — 3,477`** — a range that is not a range. The band was correctly
   zero-width; rendering it as a span was false precision wearing the costume of honesty. It now
   prints as a POINT with its reason. See the model finding below.
2. **`Reach 94 613`** — this machine's sv-SE culture rendering `{0:N0}` with a non-breaking space.
   **B3's recorded defect class, in code written today.** Every numeric on both screens now goes
   through an `Invariant` helper or an explicit `CultureInfo.InvariantCulture`.
3. **The masthead said "CAMPAIGN HQ" on the action screen.** The title is now a parameter, which is
   also what let the two screens keep sharing one masthead.
4. **The sheet was ~60 % empty** — and that was a DESIGN failure, not a spacing one: the screen could
   not answer the question it exists for. Fixed by adding real derived content (a band per option),
   never by shrinking the board to fit what little it had.
5. **`TOP OF SCALE 1`** on the unmeasured capture. `PersuasionScale` falls back to 1.0 so the bars
   can divide safely, and that safe fallback leaked onto the screen as a fact. On the one screen
   whose entire subject is not printing numbers it cannot justify, a phantom scale had to go; the
   sentence is now printed only when a scale exists.

### A model finding, recorded and NOT papered over

**Enthusiasm carries no measurement uncertainty in the current model.** §42 derives it from exposure
and credibility alone, and neither is polled, so no polling error can reach it — which is why its
band is a point rather than a span. That may well be wrong about the world: it is odd that how much
an electorate CARES about an issue changes how persuaded they are but not how motivated they are to
turn out. **It is not fixed here.** If enthusiasm should depend on salience, that is a change to the
model with its own reason and its own harness — never a width invented at the drawing layer to make
the screen look consistent.

### A finding the screen made visible for the first time

**Interview dominance is now legible.** `Interview` costs 0 kr and 2 h and draws one of the longest
bands on the sheet. This is exactly W-B3's recorded result — that an interviews-only player is
currently optimal — lifted out of a harness log and onto a screen where it can be judged. It is also
precisely the input the W-B9 ruling asks for: earned media is free because *someone else decides
whether to book you*, so the scarce resource is media INTEREST, to be implemented in §13 as
availability driven by newsworthiness rather than as a cost bolted onto the action.

### Calls logged (R-N1), each strikeable

- **`-shotcampaign` films the whole Track E set** rather than gaining a flag per screen: each Unity
  launch costs ~40 s of warm-up and the screens share their staging. The items stay separable by
  capture NAME (`e1_*`, `e3_*`), which is what a reviewer actually reads.
- **Both Track E screens draw from the SAME staged campaign day**, so they cannot disagree about the
  war chest or the hours. Two campaign screens contradicting each other would be worse than either
  being wrong alone.
- **A zero-width band prints as a point with its reason**, never as `x — x`.
- **The estimate's mid is a hairline tick inside the band, not a headline numeral.** The eye must
  take the interval first and the point second; a bold mid with a small ± beside it is the exact
  false precision this item forbids.

### The dead-space class, measured rather than impressionistic

Both Track E screens read bottom-empty at every width, W-E3 worse than W-E1 before the per-option
bands were added and still noticeably so after. Per the standing dead-space ruling this stays a
recorded finding and a Track H Design line — the v3 stage's text budget applies, and filling space
with invented rows would break it.

## 54. W-E4 — the polling screen, and §21's decision made arithmetic (2026-08-29)

**§21 is one sentence, and this screen exists to serve it:** *"The player should have to decide
whether additional information is worth the cost."* A decision needs both sides priced, so the right
column puts kronor against **percentage points of precision** — and every ± on it is DERIVED from
the offer's sample size by `PollingSystem.MarginOfErrorPp`, the same function a conducted poll
reports with. The price list cannot promise an accuracy the polls then fail to deliver, because it
is not making a promise: it is quoting the same arithmetic.

**The ladder, as filmed** (quoted at S's polled 31.6 %):

| offer | n | cost | ± | per point gained |
|---|---|---|---|---|
| Public tracker | 600 | 40 000 kr | ±3.7 | the baseline |
| Standard commission | 1 200 | 120 000 kr | ±2.6 | 73 433 kr |
| Regional breakdown | 2 400 | 260 000 kr | ±1.9 | 118 294 kr |
| Full internal programme | 6 000 | 620 000 kr | ±1.2 | 228 048 kr |

**Each point of precision costs more than the last, and that is arithmetic rather than design** — it
falls out of √n. The sample sizes and prices are **[AUTHORED-DRAFT]** (W-F5 sources real party
finances); the *shape* of the ladder is not authored at all.

**The screen states what money cannot buy, on the same sheet that sells precision.** §20's other
sources of difference — late swings, turnout, undecided voters, tactical voting, and each house's own
lean — are named as NOT being in the ±, which describes sampling error and nothing else (W-B10). A
price list that sold precision without saying so would be selling a false promise.

**§22's momentum gets the centre column**, with the half-life the code actually uses, and states the
two things a player would otherwise have to infer: momentum cannot be made permanent (a lasting gain
is a reputation change, §38 — a different stock), and it shifts where the race *appears* to be
without touching the preference underneath, which is why a poll can move before anything real has.

### The judgement this item turned on

**Regional, demographic and turnout depth are deliberately NOT folded into the cost-per-point
figure.** Doing so would have made a tidier column — one number per row, sortable — but those are
different KINDS of answer, not narrower ones. Averaging a capability into a precision score would
hide exactly the trade §21 says the player should have to make. They are named on the row instead,
and the footnote says why they are not priced per point.

### Two defects, both caught by the guards this time

1. **A caption 446 px wide in a 425 px slot at 1280** — the full programme's `n = 6,000 · REGIONAL ·
   DEMOGRAPHIC · TURNOUT MODEL · 228,048 kr PER POINT OF PRECISION GAINED`. Fixed by splitting the
   row into TWO caption lines rather than abbreviating: what the offer IS and what its extra
   precision COSTS are two facts, and shrinking the item's own subject to fit a tidy row would have
   been the wrong trade.
2. **A 1.1 px height overflow at 1920 only** — `120,000 kr · ± 2.6` needing 21.2 px in 20.1. See the
   class below.

### A durable fact about this codebase's two type faces

**Pagella and the mono document face do not share a line box.** At 1920 an 11 px mono caption
measures 21.2 px tall against a 13 px body's 20.1 px, and because `DeskPx` rounds to integers the two
faces cross over at different points on the ladder. **Any row that measures its height from one face
and then draws a label in the other is a latent clip**, and whether it actually clips depends on the
width. The rule: a row carrying labels in BOTH faces takes `Mathf.Max` of both measured heights.
Three instances this session — W-E1's momentum caption, W-E3's options ledger, W-E4's offer head —
all now do.

**This is also the concrete case for filming at four widths rather than one.** Both genuine overflow
classes today appeared at exactly ONE of the four: the mono/body mismatch only at 1920, the long
caption only at 1280. A single representative size would have shipped both.

### The play-calibration list is now open

`ELECTIONS_PLAY_CALIBRATION.md`, created on the completion of W-E1/E3/E4 exactly as the W-B3/W-B10
review ruled. Six entries, `CampaignPressure.PersuasionPerCompatibilityPoint` first, each stating
what is measured, what would settle it, and **what must not be done in the meantime**. Its governing
rule is written at the top: nothing on the list is tuned to make a gate pass. The two model findings
this session produced — enthusiasm's insensitivity to salience, and earned media's dominance — are
carried there WITH their rulings attached, so neither can be quietly re-solved as a tuning problem
later.

### The three screens are now one loop

Campaign HQ shows where you stand (the race *as polled*, never as it is); the action screen prices
what you could do about it as a range whose width is your own measurement error carried through §42's
chain; the polling screen sells narrower measurement and quotes what each marginal point costs. A
wide band on the action screen is a reason to visit the polling screen, and the polling screen's
prices are meaningful because W-E3 showed what a wide band costs in confidence. Both ends derive from
the same `MarginOfErrorPp`, so they cannot contradict each other.

## 55. W-C1 — AI parties (§32) and expected-value decisions (§33): five personalities on one system, and the environment's dominant strategy measured (2026-08-29)

**What shipped.** `Assets/Scripts/Elections/CampaignAi.cs` — §32's five personalities as
parameters over §33's terms (`PersonalityCatalog`), the `AiView` an AI is allowed to decide from,
`CampaignAi.Evaluate` / `Choose` (the scoring and the softmax), and `CampaignIntelligence` (§21's
commissioned poll applied to issues). `Assets/Scripts/Elections/CampaignRun.cs` — an AI-only
campaign run day by day through W-B1's calendar, W-B2's resources, W-B3's actions and §42 chain,
W-B10's polls, W-A1's derived loyalty and §8's preference model; it is the one place the truth
lives, and the AIs are handed views built from the polls they bought or were published.
`Assets/Editor/CampaignAiHarness.cs` — the proof. `SimulationRandom.Stream.CampaignAi = 8`
APPENDED (the ElectionNoise precedent: nothing live draws from it; the trajectory suite re-proven
byte-identical at the boundary that adds it). All pure, all unwired (R-N2).

**The item's bar, stated as a type.** An AI cannot access hidden state the player cannot buy (§36)
because every decision is a function of an `AiView`, and an `AiView` is built from a `Poll` (which
cannot carry the truth — W-B10), from `IssueMeasurement`s that come out of a commissioned poll with
the ± their sample size buys, and from public facts. The harness reflects over the view and finds
no truth-shaped member; `Evaluate`'s signature is the view, the personality, a pool and the
reserve, nothing else; an AI with no poll gets only BLIND estimates (a flat prior over 0–1, not a
wide guess); the never-polling chaotic party's 284 decisions were every one of them blind.

**§33's scoring, in one unit and with no authored exchange rate.** `score = expectedGain ×
targetImportance × probabilityOfSuccess − cost`, then × a risk factor on the band's relative
width, per hour. The gain is §42's chain through `CampaignActions.ResolveBand` on the MEASURED
salience and issue-match — the same function W-E3's action screen prices with — read at the
personality's optimism point of the band and converted to compatibility points by the model's own
`PersuasionPerCompatibilityPoint`. ⚠ **The first draft priced money as a fraction of a daily
budget against a normalised gain, and that made every money action unaffordable in score terms**
— a 500 000 kr television buy against a 120 000 kr day never scored, for anyone. Replaced with
three ideas that need no kronor-to-votes rate: money is priced at the action's OWN efficiency at
its smallest outlay (§35 is concave, so a bigger outlay is always less efficient, and the
personality's `CostWeight` says how much less it tolerates); money is otherwise a CONSTRAINT — a
reserve the party's `SpendPace` releases over the days left, so a television buy is something a
party saves for; and hours are the binding daily resource (W-B2: they cannot be banked), so
candidates rank by value per hour. The establishment party is now the only one that saves up for
the 500 000 kr buy — three of them over the campaign.

**The done-when, clause by clause — met, met-with-stated-scope, met.**
1. *Deterministic completion.* Seed 777 twice: digest `d7670f735d1b8864` both times, final shares
   bit-identical, 56 of 56 campaign days, eight public polls, no party's money negative.
2. *Five measurably different mixes.* **Met for what the environment can distinguish and PENDING
   for the rest, with the pending lines printed beside their measurements.** Asserted and green:
   the chaotic mix differs from every other's (min L1 0.604); the populist's from every other's
   (0.504); the grassroots party buys the least broadcast (0 kr against the populist's 1.2 m); the
   establishment party buys the most television (3 : 0); the professional buys the most polling (8
   against 3–4 and the chaotic party's 0) and never acts blind; the populist ends with 0 kr while
   the professional keeps 1.44 m (front-loading against pacing); the chaotic mix varies most seed
   to seed (0.161 against ≤ 0.039). PENDING: professional / establishment / grassroots separate
   (measured L1 0.013–0.024 — all three interview all day), the populist's rallies, the grassroots
   party's door-knocking, the establishment's television + interview lead (every rational mix is
   ~100 % interviews).
3. *No hidden state.* Structurally and behaviourally, above.

### The judgement this item turned on

**The rational three collapse onto one strategy, and that is the ENVIRONMENT's fact, not the
AI's.** In W-B3's placeholder environment a free national interview is available six times a day
(the dominance W-B3 and W-E3 recorded; W-B9's media-interest mechanism does not exist yet), and a
local action reaches a fraction of ONE region against a national action's whole electorate (W-B3's
placeholder reach; W-B4/B11 make it volunteer-hours). Any personality that maximises expected value
therefore interviews all day, and the professional, the establishment party and the grassroots
party — which differ in polling, pacing, risk and what they buy — are indistinguishable in what
they DO with their hours. An affinity large enough to make the grassroots party knock doors here
would be a number chosen to make a test pass, which is exactly what the play-calibration list
forbids. So the separation is **recorded as `PEND` with its measurement, riders are placed on
W-B9 and W-B4/B11's done-whens (those lines become assertions and pass with no affinity changed),
and nothing is forced.** The harness exits green on what it can honestly claim and says in its
summary line how many claims are pending and on what.

### Two findings the run produced, beyond the one it was built to measure

- **The chain saturates at the real national audience.** Every rational party delivers **+197 to
  +225 compatibility points** over the campaign, and `ElectionScales` clamps every party at 100 —
  so the final-share column is the clamp's arithmetic, not the campaign's difference. W-B3
  measured +0.19 pp for a hard week at a 100 000 audience; at 6.5 million the same chain is 65×
  that, because reach is linear in audience AND in repetition (the same electorate "reached" six
  times a day). This is **a mechanism question before it is a calibration one** — bounded reach,
  repeated-exposure decay, W-B9's media interest — and the play-calibration list's entry 1 gains
  the measurement as an addendum, not a new value. **The constant was not touched.**
- **Local reach is the placeholder, and it shows.** Door-to-door reaches 2 % of a region per five
  hours — 16 000 doors in Stockholms län — which is not what five hours of door-knocking is. The
  right model is an ABSOLUTE count (volunteer-hours × doors per hour, §10's offices), which is
  W-B4/B11's; noted there, not patched here.

### The staging, with its data classes stated

SOURCED — Sweden 2022 (the prior) and 2018 shares, Valmyndigheten final counts; loyalty derived
from the pair (W-A1: S 93, M 96, SD 85 … C 78); the 29 valkretsar's 2018 valid votes as audiences
(6 476 725 nationally); EB105 Spring 2026 salience for Sweden — climate .26, crime .18, defence
.17, education .16, the "% naming among the two most important" read as salience on 0–1, and
*"threats to democracy"* (joint top) billed because §6 has no slot for it. DERIVED — compatibility
at the fixed point where `PersuadedShares == prior` (`c_i = 70 × (prior_i / max)^(1/3)`), so an
idle campaign reproduces the 2022 result exactly (asserted to 1e-9). `[AUTHORED-DRAFT]` — issue-
match 0.5 flat for every party (W-F2 sources positions per issue), credibility 0.6 flat (W-F6), war
chest 2 400 000 kr each and EQUAL by design so the mixes differ by personality alone (W-F5), the two
polling houses from W-E4's ladder (the public tracker n=600 every seven days, free to all; the
standard commission n=1 200 at 120 000 kr, which measures issues too). Every personality parameter
is `[AUTHORED-DRAFT]`, tabled in `ELECTIONS_PROTOTYPE_LOG.md` and carried as the play-calibration
list's entry 7.

### Not built, by ruling

§33's worked example scores "Attack Opponent" — §12's eight have no attack verb and §11's negative
campaign is W-B8's; nothing invented. The horse-race poll is on the view but not in the score:
"targets swing voters" needs §25's swing index (W-E2) and "reacts quickly to events" needs §18's
events. Pre-campaign days are not simulated (§3's preparation verbs have no price yet). Momentum
takes no shock, because nothing that shocks it exists yet, and the view shows zeros rather than an
invented drift.

**Records.** `ELECTIONS_PROTOTYPE_LOG.md` W-C1 (the calls, the [AUTHORED-DRAFT] table, the riders
on W-B9 / W-B4 / W-B11 / W-E2 / W-B8); `ELECTIONS_PLAY_CALIBRATION.md` (entry 1's addendum, entry
7); `ELECTIONS_GAP_TABLE.md` rows 32 and 33 discharged; the `CLAUDE.md` dated section.

**R-N2 at the boundary that appends a stream.** `traj_wc1_*` ≡ `traj_run_*` (W-E4's family, itself ≡ `traj_wb10_*`) — six of six identical by SHA-256 (`80e0c878…`, `85135b71…`, `35c0f578…`, `57c77cc4…`, `a76586bc…`, `a86a81f7…`), zero ATTRIB in the dump; the eight asset checks exit 0 (`check_*_wc1.log`). Harness log `campaignai_wc1d_20260829.log`. Not one byte of the existing game moved.

## 56. W-B6 — campaign strategy (§11): five trade-offs as modifiers over the whole chain, and a sweep with no dominant strategy (2026-08-29)

**What shipped.** `Assets/Scripts/Elections/CampaignStrategy.cs` — `CampaignStrategy` (None +
§11's five), `StrategyModifiers` (reach, persuasion, enthusiasm, credibility, salience-shift,
media-attention multipliers and an opponent share) and `CampaignStrategyModel` (`Modifiers` per
strategy × group loyalty × whether the group prioritises the focus issue; `Resolve`, which applies
them to §42's chain and returns the same `ChainTrace` every consumer reads; `Prioritises`).
`CampaignPressure.AddAgainst` — the negative campaign's only route to an opponent, still a
compatibility pressure. `Assets/Editor/CampaignStrategyHarness.cs` — the proof. The W-C1 AI given a
strategy per personality and its run applying the modifiers on both sides of the seam. Pure,
unwired (R-N2).

**A strategy is multipliers, not an action and not a vote delta.** Reach and credibility multiply
the chain's inputs, so a zero anywhere still annihilates the effect; persuasion, enthusiasm and
salience-shift multiply its outputs; `None` is the identity and every earlier measurement stands
(asserted). Every multiplier depends on WHO the group is — its loyalty and whether it prioritises
the message's issue — which is what makes each strategy a trade-off rather than a bonus: the same
strategy lifts one group and lowers another, and which groups an electorate contains decides which
strategy wins.

**The five, as the spec's bullets turned into shapes** (magnitudes `[AUTHORED-DRAFT]`, tabled in
the log): Broad Appeal — more reach, less persuasion per head, half the polarisation. Base
Mobilization — enthusiasm up with loyalty, persuasion down with swing. Swing Voter — persuasion up
with swing, enthusiasm down with loyalty. Negative Campaign — 60 % of the message's persuasion
lands AGAINST the targeted opponent, own persuasion ×0.8, credibility ×0.9 (the backlash as an
expected cost; the seeded EVENT is W-B8's), media attention ×1.5 (carried for W-B9, read by nothing
yet), polarisation ×1.5. Populist — ×1.5 persuasion and ×1.3 enthusiasm for a group that
prioritises the focus issue, ×0.6 persuasion for one that does not.

**The done-when.** (1) *Each strategy's stated trade-off* — seven assertions on the same rally
(salience 0.6, match 0.6, credibility 0.7, full spend) for a loyal group at 85 and a swing group at
20, on and off the focus issue: Broad reach 3 793 → 4 362 with persuasion per head 0.0756 → 0.0643;
Base loyal enthusiasm 2 124 → 3 207 with swing persuasion 229 → 138; Swing swing-persuasion 229 →
307 with loyal persuasion 229 → 188 and loyal enthusiasm 2 124 → 1 582; Negative −99 against the
opponent, own persuasion 229 → 165, credibility 0.70 → 0.63, media ×1.5; Populist focus group 229 →
344, other group 229 → 138; `Prioritises` yes/no; `None` identical. (2) *No strategy dominating in
a measured sweep* — thirty electorates (loyal share 0.1–0.9 × focused/diffuse issue weights ×
opponent strength 0.4/1.0/1.6), the W-B3 week run identically under each strategy, the cell's
outcome own persuasion + enthusiasm minus the opponent's in the model's own units: **Base
Mobilization 21, Broad Appeal 6, Populist 3, Swing Voter 0, Negative 0** — no strategy wins every
cell, three win somewhere.

### Two findings, recorded not tuned

- **Swing Voter and Negative Campaign win no electorate.** Swing's loyal-group cost outweighs its
  swing-group gain at every loyal share; Negative's 60 % against an opponent running the identical
  week is worth less than its own 20 % persuasion cut plus the credibility cost. Both are the
  model's statement at these magnitudes and this metric; the play-calibration list's entry 8
  carries them. §32's professional runs Swing Voter regardless, because that is what the spec says
  it does — whether that is a losing choice is now a measured question rather than an assumption.
- **Base Mobilization's 21 of 30 is the enthusiasm conversion** (60 000 per turnout point against
  40 000 per compatibility point, and Base lifts enthusiasm by up to 60 %). The sweep's metric is
  the model's own unit conversion, not the harness's; a different weighting of turnout against
  persuasion would move the table, and that weighting is §26/W-D1's to settle.

### The AI runs strategies now

`PersonalityProfile.Strategy`: professional → Swing Voter, populist → Populist, establishment →
Broad Appeal, grassroots → Base Mobilization, chaotic → Negative Campaign — its target the leading
OTHER party in the latest poll it has seen (`AiView.PolledLeaderOtherThanSelf`: chosen from a
`Poll`, never the truth). The modifiers apply to the AI's own estimate exactly as to the world's
response, so a party cannot mis-price its own strategy. The electorate is ONE group at W-A1's
size-weighted mean loyalty (89.7 for Sweden, a public derivation from past returns) until W-F4's
voter groups exist; "prioritised" for a one-group electorate means the message is on its most
salient issue. W-C1's digest moves (`d7670f73…` → `463560d1…`) and every one of its assertions
still passes, its four `PEND` lines unchanged.

**Riders:** W-B9 reads the media-attention multiplier; W-B8 decides whether the backlash becomes a
seeded event beside or instead of the expected cost; W-F4 retires the one-group electorate and
`CampaignRun` applies modifiers per group.

**Records.** `ELECTIONS_PROTOTYPE_LOG.md` W-B6; `ELECTIONS_PLAY_CALIBRATION.md` entry 8;
`ELECTIONS_GAP_TABLE.md` row 11 discharged; the `CLAUDE.md` dated section.

**R-N2 at the boundary.** `traj_wb6_*` ≡ `traj_run_*` six of six by SHA-256, zero ATTRIB; the eight checks exit 0 (`check_*_wb6.log`); harness logs `strategy_wb6b_20260829.log`, `campaignai_wb6_20260829.log`.

## 57. W-B9 — the media system (§13) and audience segmentation (§14): media interest as availability, coverage that cannot spiral, the same message by outlet (2026-08-29)

**What shipped.** `Assets/Scripts/Elections/MediaSystem.cs` — `MediaOutlet` (reach ceiling, daily
slots, interest threshold, audience composition over voter groups, television flag),
`MediaSystem` (newsworthiness per action, the news-cycle decay, the saturating gain, the interest
function, the per-kind national audience a party can reach through the media landscape, and
`ResolveThroughOutlet` — a message resolved per voter group with that group's own salience and
match, weighted by the outlet's composition), `MediaCoverage` (coverage as a decaying stock with a
saturating daily gain; the gain is the momentum shock), `MediaInterest` with its `BookingLedger`
(the outlets' diary — entitlement carried day to day), `MediaCatalog` (archetype outlets).
`Assets/Editor/MediaHarness.cs` — the proof, 14 of 14. The AI campaign now runs under the media.
Pure, unwired (R-N2).

**The standing ruling, executed.** Earned media is free because someone else decides whether to
book you, so the scarce resource is media INTEREST — availability driven by newsworthiness, never
a cost or a cap on the interview. Each day the outlets allocate their slots in proportion to
`1 − exp(−(coverage + 0.15·|momentum pp| + 0.8·polled share + events))`; a party at 4 % with no
coverage is booked by no outlet whatever it would pay, the same party after a day of news is booked
seven times, a bigger party is booked more on a quiet day, and the interview's spec is still 0 kr
with no cap (asserted). **Bookings are a ledger:** the first allocation let the two most
newsworthy parties take every slot in the country; the second (largest remainder per day) starved
the fourth — a party at 19 % went eight weeks without an interview; the ledger carries each
party's fractional entitlement per outlet across days (82/82/57/38 of 270 slots over a month at
steady interest, the two under every threshold never).

**Coverage cannot spiral, and it creates momentum.** The stock decays on a 3-day news-cycle
half-life (distinct from §22's 7-day momentum half-life — the second mechanism W-B10's doc named
rather than a fudged exponent; the spike follows the declared curve to 1.7e-16 and is at 0.10 %
after a month) and grows only through `1 − exp(−raw)`, so a year of maximal news peaks at exactly
the stated ceiling 4.85 and interest stays under 1. The day's GAIN shocks §22's momentum at 1.5 pp
per unit — bounded because the gain is — and momentum, coverage and the published race feed back
into tomorrow's interest: §13's own chain, with its diminishing returns built in.

**§14 — the same message by outlet.** An outlet is a reach ceiling and an audience composition.
One climate television message resolved for a two-group electorate whose groups differ only in
what they care about: ×1.90 per person reached through the young-urban outlet against the
older-rural one; a crime message ×2.00 the other way; through the general-population outlet the
two are identical to 1e-9 — the audience decides, not the outlet.

**The media landscape bounds what a national action can reach** (`MediaSystem.NationalAudience`):
television = the television outlets' combined reach (0.80 — across them, not through the largest,
which had made television strictly dominated by digital); a digital ad = the platforms' 0.55; a
social post = the party's own following (polled share × 0.30 — a party nobody follows posts to
nobody); a policy announcement = the press's interest in the party. W-B3's placeholder had every
national action address the whole electorate; under the media the AI campaign's compatibility
bonuses fell from +197…+225 (every party clamped at 100) to **+7…+37**. That is the mechanism half
of W-C1's saturation finding, delivered without touching `PersuasionPerCompatibilityPoint`.

### The AI under the media, and what it exposed in the AI

Three defects in W-C1's scoring that six free interviews a day had masked, all fixed and logged:
money priced on the whole spend made every money action score exactly zero at `CostWeight` 1.0
(the increment above the smallest outlay is priced now — seed-to-seed variability fell from 0.652
to 0.029 once the knife-edge went); the reserve spent daily meant no party ever polled again (the
poll's price is kept back once a poll is due — polls back to 6/4/4/3); and two saving rules were
tried for the television buy and both were worse than none (idling; a week of social posts to
afford one buy) — **there is deliberately no saving rule**, because a big-ticket buy needs a budget
plan, which is §9's campaign manager's (W-B5), recorded there.

**What the C1 harness says now** (digest `5152fe7bc2b41c0c`; 20 assertions green, 7 `PEND`):
every party booked 50–81 times; the grassroots parties knock doors (26–33 each) and separate from
both media personalities (L1 0.71 / 0.61 — the W-B9 rider, half discharged); the chaotic party is
distinct from all (0.477) and the most inconsistent day to day (1.035); the professional polls
most and never acts blind; the populist has 80 % of its chest spent by day 34 against the
professional's 44. **Pending, blockers named:** the professional and the establishment converge
(0.101) on what fair bookings, the press's interest and even pacing leave them — separation waits
on a budget plan for television (W-B5); the populist against the rest (0.292) and its rallies wait
on real local reach (W-B4); the advertising claims wait on W-B5 (nobody advertises but the
unbooked); door-to-door's "largest share" on W-B4/B11 (holds early: 12 % against the chaotic's
19 %). Affinities untouched throughout.

### Data classes and what is billed

`[AUTHORED-DRAFT]`: every constant (calibration entry 9), the outlet ARCHETYPES — no real outlet
name carries an authored number. Billed: real Swedish outlet reach (Kantar / Orvesto), real
follower counts (W-F5/F6). Hooks left for their owners: `MediaCoverage.AddShock` for debates,
scandals and events (W-B7/B8/§18); virality (§13) not modelled; the action screen's booking diary
and per-kind audiences (W-E2/E3).

**Records.** `ELECTIONS_PROTOTYPE_LOG.md` W-B9 (the calls, the constants, the C1 state, the riders
on W-B5 / W-B4 / W-B11 / W-B7 / W-B8 / W-F5 / W-F6 / W-E2 / W-E3); `ELECTIONS_PLAY_CALIBRATION.md`
entry 9; `ELECTIONS_GAP_TABLE.md` rows 13 and 14 discharged; the `CLAUDE.md` dated section.

**R-N2 at the boundary.** `traj_wb9_*` ≡ `traj_run_*` six of six by SHA-256, zero ATTRIB; the eight checks exit 0 (`check_*_wb9.log`); harness logs `media_wb9b_20260829.log` (14 of 14), `campaignai_wb9b_20260829.log` (all assertions pass, 7 PEND).

## 58. W-B11 — Get-Out-The-Vote (§26): mobilization as volunteer-bound contacts, per region and per party; targeted regions only, the nation within its history (2026-08-29)

**What shipped.** `Assets/Scripts/Elections/GotvModel.cs` — §26's four operations (phone banking,
door knocking, transport, election-day reminders) as cost, volunteer-hours and weight per contact;
`GotvModel.Contacts` (bounded by money AND hours, whichever runs out first); `Mobilization` (50 +
50 × (1 − exp(−contacts per eligible / 0.5)) — `TurnoutModel`'s neutral 50 for an unworked
region, §35's curve so no budget passes 100); `RegionalMobilization` (every party's weighted
contacts in every region, and the turnout, region votes and national turnout they produce through
`TurnoutModel`). `Assets/Editor/GotvHarness.cs` — 10 of 10 on the 29 valkretsar.
`ElectionsData/sweden/turnout_history.md` — the 2002–2022 series, `[SOURCED] [PROVISIONAL]`. Pure,
unwired (R-N2).

**Per region and per party, through the input.** `TurnoutModel` keeps no party term — turnout is
a property of a group in a context, and that rule stands. GOTV is the one thing that IS
party-specific about turnout ("get SUPPORTERS to vote"), and it enters through the mobilization
INPUT: a party's supporters in a worked region turn out at its own mobilization there, everyone
else's at 50; a region's turnout is the preference-weighted mean; the nation's is eligible-
weighted over regions, never a mean of rates. §31 can later say "you won on turnout" and mean it.

**The done-when.** S door-knocks three valkretsar with 400 000 kr and 20 000 volunteer-hours each
(80 000 doors each): Stockholms län 84.21 → 85.03 %, Skåne läns södra → 86.41 %, Gotlands län →
89.07 % (the same doors are a fifth of a small electorate and a fiftieth of a large one); **the
other 26 valkretsar at exactly base turnout, bit for bit; the other seven parties' supporters'
turnout unchanged in all 29**; S's vote share in Stockholm 30.80 → 31.47 % with SD's votes
unchanged to the vote — the turnout advantage, preference untouched. Every party's whole chest
and 60 000 volunteer-hours on the doors nationwide: **85.26 %**, inside 2002–2022's [80.11, 87.18]
widened by two points; unlimited lifts for everyone everywhere: 100 % and not a vote more — stated,
not hidden. Volunteers bind (1 m kr and 0 hours knocks 0 doors); the first 10 000 doors move
mobilization more than the next 10 000.

**Staging, with its classes.** Base turnout SOURCED (2022, 84.21 %, Valmyndigheten) and uniform
per valkrets; eligible per valkrets DERIVED as 2018 valid votes ÷ 87.18 % (7 429 141 against the
true 7 775 390 — the per-valkrets `Röstberättigade` counts are billed); the 2002–2010 turnouts
written from the recorder's knowledge of val.se's series and marked to be read back (2014/2018/2022
agree with the files already on disk). `[AUTHORED-DRAFT]`: the four operations' per-contact
figures, `MobilizationScale`, 800 volunteers per party — calibration entry 10.

### C1's PEND lines — the clearance the list asked for

W-B11 was named on `PEND 2c` and on the grassroots half of `2a-iii`. **Cleared: none. Changed:
one, honestly for the worse.** Door-to-door in the AI campaign now reaches the doors the
volunteers can knock (`GotvModel.Contacts` on the day's volunteer-hours, for the world's response
and the AI's estimate alike) instead of W-B3's 2 % of a region: ~3 000 doors an action, not
16 000. At W-B3's per-contact persuasion weight (0.55) that is not worth five hours against a post
to the party's whole following, so no rational personality knocks doors and the grassroots
separation W-B9 produced (0.71 / 0.61, asserted as `2a-iv`) is gone (0.20 / 0.17). **`2a-iv` goes
back to `PEND`** with its true blockers — the ground game's scale (W-B4: offices grow volunteers;
800 is a guess) and the persuasion a personal contact is worth (calibration entry 10; the
canvassing literature is the source, billed) — and `2c` stays on the same two. Nothing was raised
to keep a line green; the digest moves to `f0ca739d9c7529c7` and every other assertion holds.

**Riders.** W-D1 runs `RegionalMobilization.RegionVotes` per valkrets on election day with the
campaign's accumulated contacts and gives the AI its election-day plan; W-B4 grows volunteers and
re-measures `2a-iv / 2c`; W-F4 brings per-valkrets eligible counts and per-group base turnout.

**Records.** `ELECTIONS_PROTOTYPE_LOG.md` W-B11; `ELECTIONS_PLAY_CALIBRATION.md` entry 10;
`ELECTIONS_GAP_TABLE.md` row 26 extended; `ElectionsData/sweden/turnout_history.md`; the
`CLAUDE.md` dated section.

**R-N2 at the boundary.** `traj_wb11_*` ≡ `traj_run_*` six of six by SHA-256, zero ATTRIB; the eight checks exit 0 (`check_*_wb11.log`); harness logs `gotv_wb11_20260829.log` (10 of 10), `campaignai_wb11_20260829.log` (all assertions pass, 8 PEND).

## 59. W-D1 — election day (§27): every valkrets counted independently, controlled uncertainty on its own stream, and 1/√n proven on the real 29 (2026-08-29)

**What shipped.** `Assets/Scripts/Elections/ElectionDay.cs` — `Count`: for each region, W-B11's
`RegionVotes` (eligible × preference × each party's supporters' turnout — where the ground game
lands), then §27's `Final Vote = Expected Vote + Election Noise` as Day-1's `ApplyNoise` on the
region's shares at the declared 1.2 pp, turned back into whole votes against the region's votes
cast; national votes and shares as the vote-weighted sum of regions; `EffectiveRegions` (1 / Σ w²);
a digest of every regional count. `Assets/Editor/ElectionDayHarness.cs` — 10 of 10.
`CampaignRun.Result` now carries the campaign's accumulated ground contacts (`Gotv`) and the
valkrets names, so an AI campaign can be counted (the W-B11 rider). Pure, unwired (R-N2); every
draw from the `System.Random` the caller passes — the harness passes `SimulationRandom`'s
`ElectionNoise` stream, so one election re-runs under a seed without re-running the economy.

**The done-when.** Seed 777 twice: digest `7b7ce512348e9941` both times — every regional vote
count identical; seed 778 differs. Over 400 replays the regional share σ is 1.167 pp against the
declared 1.2 (re-normalisation shrinks it by 3 %), and the national σ is **0.259 pp against 0.260
predicted** by σ / √N_eff with N_eff = 20.15 of the 29 valkretsar by eligible weight — Day-1
measured the 1/√n behaviour on eight equal regions (0.95 → 0.35 pp); this is the same law on the
real, unequal ones, matched to a third of a percent. The count is a count: each region's party
votes sum to its votes cast to rounding, the nation's to the sum of regions (6 272 372 of
6 272 383), turnout is W-B11's exactly (84.43 % — the three worked valkretsar lift the base 0.22
pp), and with σ = 0 the count is the expected result to the vote, 29 × 8.

**Not here, by ruling.** Seats (W-D2 — `SeatAllocation` waits for this result); voter groups
(one preference vector per region, the 2022 vector repeated, until W-A2 and W-F4 make them
differ — the API already takes a per-region array); tactical voting (§23, W-A4); election-day
events. The national uncertainty at these magnitudes is a quarter of a point per party — inside
§27's "matters, cannot be perfectly predicted" band; whether that is *felt* is a play question on
Day-1's declared `RegionalNoiseSigmaPp`, recorded beside calibration entry 1.

**Records.** `ELECTIONS_PROTOTYPE_LOG.md` W-D1; `ELECTIONS_GAP_TABLE.md` row 27 extended; the
`CLAUDE.md` dated section.

**R-N2 at the boundary.** `traj_wd1_*` ≡ `traj_run_*` six of six by SHA-256, zero ATTRIB; the eight checks exit 0 (`check_*_wd1.log`); harness logs `electionday_wd1_20260829.log` (10 of 10), `campaignai_wd1_20260829.log` (all assertions pass, 8 PEND).

## 60. W-D2 — vote-to-seat on the live path (§28): Sweden's own procedure end to end, 2022 seat-for-seat, the return branch made to fire (2026-08-29)

**What shipped.** `Assets/Scripts/Elections/SeatConversion.cs` — vallagen 14 kap. as a procedure
over `SeatAllocation`'s exact divisor arithmetic: eligibility (4 % nationally or 12 % in a
valkrets for that valkrets's fixed seats), the statute's distribution of the 310 fixed seats among
the valkretsar by eligible voters (one per 310th part, remainder by largest surplus), the modified
odd-number method within each valkrets, the totalfördelning over 349, återföring (a party over
its total returns its weakest fixed seats, re-allocated within the valkrets under every party's
cap), and the 39 adjustment seats placed where each party's next comparison number is highest;
`Sweden(ElectionDay.Result)` is the live path. `Assets/Editor/SeatConversionHarness.cs` — 12 of
12. Pure, unwired (R-N2); no draw anywhere.

**The done-when.** The exact 2022 national counts regionalised over the 29 valkretsar by the 2018
per-valkrets distribution (national sums exact to the vote), fed as an `ElectionDay.Result` into
the live path: **107 / 73 / 68 / 24 / 24 / 19 / 18 / 16 — seat for seat** — fixed 105 / 69 / 67 /
17 / 23 / 10 / 11 / 8 (= 310) and adjustment 2 / 4 / 1 / 7 / 1 / 9 / 7 / 8 (= 39), no seat
returned, Stockholms län 39 fixed seats and Gotlands län 2 by the 310th-part rule. Not the
backtest's national shortcut: the full procedure.

**The branches, exercised rather than asserted.** The 12 % rule: L at 35 % of Gotland and nothing
anywhere else (0.25 % nationally) takes 1 of Gotland's 2 fixed seats and no other seat, 349 in all.
⚠ **Återföring — the first synthetic exercised nothing.** Fixed seats follow ELIGIBLE voters, so a
party concentrated in one valkrets stays under its total (KD's whole vote in Stockholm: 12 fixed of
19). What pushes fixed seats past a total is a valkrets where few OTHER votes are cast relative to
its electorate: every other party's Stockholm vote cut by 70 % and KD's whole vote there gives KD
24 fixed seats against 21 entitled — **3 returned**, every party at exactly its national
entitlement, Stockholm still holding its 39. Determinism (29 × 8 identical on a second pass) and
W-D1's counted election converting to 349 — S 108 / SD 73 / M 67 / V 24 / C 25 / KD 18 / MP 18 /
L 16 at seed 777: a quarter-point of share noise is three seats, so §27's "cannot be perfectly
predicted" is now visible in seats.

**Derived and billed.** Eligible per valkrets (2018 valid ÷ 87.18 %) and therefore the fixed seats
per valkrets are `[DERIVED] [PROVISIONAL]`; the real 2022 per-valkrets seat table and val.se's
per-valkrets eligible counts are billed (W-F1). The party totals do not depend on either unless a
seat is returned, which is why the live path is exact from a derived regionalisation. Personal
votes (candidate ordering) are not modelled — seats, not names.

**A finding.** The adjustment tier does real work for the small parties — KD 9 of 19, L 8 of 16,
MP 7 of 18, V 7 of 24 come from the 39 — because the 1.2 first divisor under-represents them in
the fixed tier, exactly as the method intends.

**Records.** `ELECTIONS_PROTOTYPE_LOG.md` W-D2; `ELECTIONS_GAP_TABLE.md` row 28 extended; the
`CLAUDE.md` dated section.

**R-N2 at the boundary.** `traj_wd2_*` ≡ `traj_run_*` six of six by SHA-256, zero ATTRIB; the eight checks exit 0 (`check_*_wd2.log`); harness logs `seats_wd2_20260829.log` (12 of 12), `campaignai_wd2_20260829.log` (all assertions pass, 8 PEND).

## 61. P-A1 — the meta-text census and cut: 131 developer-facing strings off the player's surfaces, and `MetaTextCheck` armed as the ninth check (2026-08-29)

**The finding (Playtest 1, finding 1, Elias):** *developer-facing text is leaking into player
surfaces — "COMPLETED" in the laws tab, progress markers, anything addressed to the builder rather
than the player.* The item: census every UI string against a banned-token review, classify, cut
every artifact, arm a guard so the class cannot silently return.

**The guard first, so the census is the guard's own output.** `Assets/Editor/MetaTextCheck.cs`
scans every string literal (plain, interpolated, verbatim) in the player-reachable UI sources —
`Assets/Scripts/UI/*.cs`, `LawCatalog.cs` (the laws tab prints its names, descriptions and
citations verbatim) and `Assets/Scripts/Data/*.cs` (display names) — with comments stripped first,
so a doc comment naming a ruling is not a hit and only text that can reach a label is. The banned
classes, each a pattern stated in the header: completion/progress language addressed to the
builder (`COMPLETED`, `IMPLEMENTED` as a tag, `TODO`, `WIP`, `STUB`, `PLACEHOLDER`); internal
references (`§`, `section N`, `R-XN`, `W-XN`, `board 1m`, `Annex X`); build vocabulary (`Master
Sequence`, `step 5d`, `Phase A`, `this pass`, `harness`, `backtest`, `Design's`, `the spec`);
data-class tags (`[AUTHORED-DRAFT]`, `PROVISIONAL`, `UNCONFIRMED`, `[DERIVED]`, `IS DERIVED`,
`SOURCED`); research-status vocabulary on citations (`CONFIRMED -`, `GENRE-IDIOM`, `DIRECTIONAL`).
The allowlist, enumerated with reasons: `PRELIMINARY` / `REVISED` / `FINAL` on published figures
(a status of the data, addressed to the player); a law's `not implemented` / `implemented`
enactment state; the Policy Web's `DERIVED` / `DECLARED` edge idiom (R-C6); `SCENARIO COMPLETE` on
the verdict screen. Armed in `CheckSuite` as the ninth check (the Editor-open run and the menu),
and in every bar from here on.

**The census, before** (`metatext_before_20260829.log`: 74 files, 2 068 literals, **124 hits**;
the widened second pass found **7 more**):

| class | where | count |
|---|---|---|
| citation status prefix (`CONFIRMED - …`, `CONFIRMED as/for/, …`, `GENRE-IDIOM/DIRECTIONAL - …`) | `LawCatalog.cs` — every citation the laws tab prints | 103 |
| research-status vocabulary inside a citation (`DIRECTIONAL/GENRE-IDIOM elsewhere.`) | `LawCatalog.cs` | 3 |
| section-sign references (`— §9`, `(§36)`, `§42'S CHAIN`, `§20's OTHER`) | the three Track E screens | 17 |
| `EVERY FIGURE ON THIS SHEET IS DERIVED` (masthead) | Campaign HQ | 1 |
| `[AUTHORED-DRAFT]` (the polling ladder's footnote) | the polling screen | 1 |
| `Master Sequence step 5d` / `this pass` (draft-dial explainers) | Tax, Welfare, Trade, Sectors | 6 |
| ruling parentheticals `(R-K1, …)` in node descriptions | `PolicyWebRenderer.cs` | 10 (cut by the script; in comments or text alike) |

**What Elias saw as "COMPLETED" is the citation class:** the laws tab printed every law's research
citation with its status prefix — *CONFIRMED - the US Truth in Sentencing…* — 103 times. The word
he remembered was the class he meant.

**The cut, by class.** Citations lose their status prefix and mid-text status words at the SOURCE
(the research bookkeeping stays in the comments beside each law, where it belongs): *CONFIRMED -
the US First Step Act…* → *The US First Step Act…*, *CONFIRMED as a standing model - Germany's…* →
*As a standing model - Germany's…*. The Track E screens' captions are rewritten in the player's
language — *RESOURCES — §9 · WAR CHEST…* → *RESOURCES — WAR CHEST, THE DAY, THE GROUND*; *§42'S
CHAIN — EVERY STAGE MULTIPLIES* → *HOW A MESSAGE BECOMES VOTES — EVERY STAGE MULTIPLIES*; *AS
PUBLISHED — §19. NO SCREEN HERE READS THE TRUE STATE.* → *AS PUBLISHED — WHAT THE COUNTRY HAS BEEN
TOLD, NOT THE TRUE STATE.*; *OPEN TO YOU TODAY — §3 PHASE GATING* → *… — WHAT THIS PHASE ALLOWS*;
*SAMPLE SIZES AND PRICES ARE [AUTHORED-DRAFT]; THE ± FIGURES ARE DERIVED FROM THEM* → *… ARE
ILLUSTRATIVE; THE ± FIGURES FOLLOW FROM THE SAMPLE SIZES*; *IT IS NOT AN AUTHORED MARGIN* → *THE
MARGIN IS MEASURED, NOT INVENTED*; the masthead's *EVERY FIGURE ON THIS SHEET IS DERIVED* goes (a
claim to the builder, not the player). The draft-dial explainers drop *Master Sequence step 5d:* and
*(Master Sequence step 5d)* and say *for now* instead of *in this pass*. Nothing was shortened to
fit; every rewrite says the same thing to the player that the tag said to the builder.

**The census, after** (`metatext_after2_20260829.log`): 74 files, 2 058 literals, **0 hits**.

**Filmed.** The three Track E screens at 1280 / 1600 / 1920 / 2560 (`pa_campaign_<w>_*`, three
staged days each) and the full sweep at 1280 and 2560 (`pa_sweep_<w>_*` — the laws tab, the
draft-dial sheets, the Policy Web, Statistics), `ScreenEdgeCheck -edgepattern=pa_*.png` clean;
§V's four Playtest-1 rows name these films.

**Records.** `MISSING_PREREQUISITES.md` §P (finding 1 answered, dated) and §V (the row's capture
named); the `CLAUDE.md` dated section; the check-suite doc says nine.

**The film and the guard.** `pa_campaign_<w>_*` (13 frames at each of 1280 / 1600 / 1920 / 2560) and `pa_sweep_<w>_*` (77 at 1280 and at 2560), 0 overflows, 0 escapes, 0 ATTRIB; `ScreenEdgeCheck -edgepattern=pa_*.png` clean over 206 captures; `MetaTextCheck` 0 hits (`metatext_after2_20260829.log`).

## 62. P-A2 — the "as published" graph block dies: a display cut, the mechanism untouched and proven so (2026-08-29)

**The finding (Playtest 1, finding 2, Elias):** *the "as published" graphs at the bottom of
Statistics are redundant.* The item: remove them; **the `PublicationSystem` mechanism is
untouched** — it is load-bearing (the election model's §19 perceived-performance reads Published,
never State) and its honesty conventions stay on the main graphs where they already live.

**What was cut.** `GameController.Statistics.cs` — `DrawStatsPublishedBand`, its KEY
(`DrawStatsPublishedKey`, `StatsPublishedKeyWidth`, the five key strings) and
`PublishedSeriesFor`, and the call that closed `DrawDomesticStatisticsContent`; the three
`GraphRenderer`s that existed only for it (`_gdpPublishedGraph`, `_unemploymentPublishedGraph`,
`_inflationPublishedGraph`). The sheet now ends on the Society rows. `GraphRenderer.DrawPublished`
and `PublishedFigure.Draw` stay (they are the instruments, not the block; the PRELIMINARY / FINAL
chips and the revision frame on the main graphs are theirs).

**What was NOT cut, and the proof.** `PublicationSystem`, every `Published` series, the release
calendar, the revision mechanic — untouched. `PerceivedPerformanceHarness` gains line 5: the
source of `PerceivedPerformance.Perceived` reads `country.Published` and never `country.State`,
asserted on the method body itself so a future edit that quietly reads State fails the harness
rather than passing unnoticed (`perceived_pa2_20260829.log`: 5 of 5). The four earlier lines
(the lag exists, perception tracks the publication, the incumbent's share differs perceived vs
actual, the divergence is reportable) still pass — the display cut changed nothing the election
model reads, which is the claim the commit makes.

**Filmed.** Statistics › Domestic at 1280 and 2560 in the `pa_sweep_<w>_02a_statistics_domestic*`
frames — the sheet ending on the Society rows, the main graphs' PRELIMINARY chips where they were.

**Records.** `MISSING_PREREQUISITES.md` §P (finding 2 answered, dated) and §V (the row's capture
named); the `CLAUDE.md` dated section.
