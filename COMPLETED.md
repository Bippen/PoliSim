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
2026-08-01. **Batches 4–6 confirmed 2026-08-02** — see section 16, which closed step 5 entirely.

**Lasting decisions from the visual work:**
- Tax and Spending merged into one **Budget** tab (`ConsolidatedTab` 7 → 6 values).
- IMGUI 9-slicing uses `style.border`, **not** the sprite's `spriteBorder` — IMGUI never reads the latter.
- Icons are stacked above labels, with space reserved via `style.padding.top` *before* the button draws.

---

## 6. Macro Data & Release Calendar Overhaul — Steps A1–A3, D

*Master Sequence step 9. Partially complete — see the roadmap for what remains.*

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
| Seed file, original | Overburden 4 of 6 | Wrong variant — "two adults" subset |
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


