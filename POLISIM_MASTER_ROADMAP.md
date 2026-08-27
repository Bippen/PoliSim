# PoliSim — Master Roadmap

This replaces three previously-separate standing documents (`ROADMAP_BRIEF.md`, `CONTINUOUS_TIME_MIGRATION.md`, `POLITICAL_SYSTEMS_OVERHAUL.md`), which had grown real dependencies on each other without being coordinated in one place. Read this in full before starting anything.

---

## Working Discipline v2 (installed 2026-08-28, Elias's omnibus kickoff — the streamline)

**The rules as they stood at `5f56798` — rules 0–15 with every embedded history, caveat and
correction — are preserved VERBATIM in `COMPLETED.md` §35 ("The discipline record, rules 0–15 as they
stood at `5f56798`").** Numbered references to "rule N" elsewhere in this document and across the
record (rule 4's `RULINGS NEEDED`, rule 12's cached status, rule 13's lock, rule 14's enumeration,
rule 15's diff) refer to THAT numbering; §35 is where they resolve. Rule 10's own requirement —
reversals recorded, never silent — is satisfied by the migration itself: nothing was cut from the
record, only from the standing text.

1. **Truth = real Unity via `BatchSimulationRunner`, explicit project path, validation scaled to
   risk.** Sim math → full matrix at 100 and 500 turns, like-for-like. UI-only → compile + guards +
   captures of the touched screens only. Uncalled data → compile. **The full capture matrix, the
   full trajectory suite and the rule-15 old-beside-new diff run ONCE, at pass end, as the closing
   gate — not per item.** (Amends rule 0's per-item practice; the per-item bar is the touched-screen
   bar.)
2. **Never invent a figure.** Every real-world number carries source, vintage and basis (the
   variant-axis rule); tags `[VERIFIED]`/`[PROVISIONAL]`/`[ESTIMATED]`/`[PLACEHOLDER]` stay in
   force; the API cross-check gate applies to anything sourced.
3. **Visual work is built-not-confirmed until Elias sees it.** Everything visual a pass ships
   lands in `MISSING_PREREQUISITES.md` §V with its capture named. The rule-15 diff (old set beside
   new, structural, by eye) is part of the closing gate.
4. **Forks.** Pre-ruled forks in a kickoff are binding. A new reversible fork → make the call, log
   one line in the report. Stop mid-pass ONLY for an irreversible/expensive fork or a blocking
   validation failure. **One report at pass end** with a `RULINGS NEEDED` block; rulings, once
   given, are written into the owning document or they did not happen. Reversals of standing rules
   are always recorded explicitly, never silent.
5. **Commits.** One unit, one commit, descriptive message; stage by explicit path; confirm staged
   contents match the message (the check runs, the narration doesn't). Verify Unity processes
   exited before batch runs.
6. **A check is evidence only for what it enumerates — name the enumeration when citing it.** Do
   not build a third site-specific guard; the three guard scopes stand as defined.
7. **Load-bearing invariants may not regress:** the eight behaviours (amber draft cue,
   direction-aware `GetDeltaColor`, `MoneyUnit`, `MeasuredLabel` shrink-never-truncate, stable
   control layout, published→live one-directional rule, per-area colour identity, always-visible
   interrupt indicator); combined ceilings audited before adding a contributor; Master Sequence
   numbering stable; screen granularity never element granularity; `[GAP]` figures sourced never
   invented; calibration at turns 100–200, "equilibrium" banned without a run that earns it.
8. **A failing validation stops its DEPENDENCY CHAIN, not the pass.** Fix it, or shelve the item
   with one line and continue with independent tracks; never build on top of a failing step.
   (Amends "never proceed past a failing step": the scope is the chain, not the session.)
9. **A status about the outside world is a cached value.** Re-derive from the filesystem and the
   callers (`DeliveredAssetCheck`, the coverage checks, `git log`) before reporting anything
   outstanding; boards are re-derived, never edited forward.
10. **Stop at coherent boundaries.** When budget runs low, drop whole units in the stated priority
    order — never thin rigor, never leave a unit half-validated.

**Explicitly retired as standing text** (history preserved in §35): the six-failure-pattern essay
(the patterns remain the checklist in rule 1's matrix runs), the verification-integrity numbering
(stays one line per instance), the per-rule reversal narratives, and the paired-detector essay
(operative content now lives in rules 3 and 6). **Amended:** "one row type per capture, never
batched" now applies to NEW row types only — re-captures of already-shipped rows may batch (the
rule existed to catch first-build defects).

---

## Where things stand — re-derived 2026-08-27 (the third consolidation pass)

**This document holds only live work.** Everything finished is in `COMPLETED.md`; everything waiting on a
named party is in `MISSING_PREREQUISITES.md`; the split is the standing pattern at the bottom of this
file. **A task is live here only if someone could start it today.** Built-but-unconfirmed and
built-but-uncalled are neither finished nor live — they wait on Elias's eyes and sit in
`MISSING_PREREQUISITES.md` §V.

| Document | Holds |
|---|---|
| `COMPLETED.md` | Finished work and lasting decisions. This file shrinks into it, never grows |
| `MISSING_PREREQUISITES.md` | Work waiting on a named party — Elias's send, decision, eyes or playtest; Design's delivery; item 10; a raster path |
| `CLAUDE.md` | The detailed technical record for both. **Never superseded** |

**The board, stated once (verified at HEAD `4e5adbf`, 2026-08-27 evening; re-derive it, do not edit it
forward):**

- **DONE** — Master Sequence I (items 1–9) and Master Sequence II Steps 1, 2, 3 and 5; Round 4; the
  fiscal-engine arc; the law system at 100 of 100 in two categories; the ruled build order (five passes)
  and the shelf's first item, tariff costs (pass 6). Records: `COMPLETED.md` §§27–31.
- **WAITING, NOT LIVE** — `MISSING_PREREQUISITES.md`: **§S** the send package (the request SENT
  2026-08-27, hash-verified, and ANSWERED the same day — the portraits delivered and imported, §D1
  closed; boards 1k/1l are live items 8–9 below; the courtesy note alone waits on Elias's send); **§A**
  the ruling queue Q6–Q10 (Elias, at named triggers); **§B** three seed quality debts (a database
  session); **§D** item 10 and everything riding its gate — 13 Sept 2026, Sweden votes — including
  election night (1h), Step 6, Riksbank-B, the stranded branch and the party marks; **§D1** the eight
  outstanding cabinet portraits — ✅ delivered 2026-08-27, `PortraitCoverageCheck` 25/25 (the roster
  look is §V's); **§E2/§E4** the mark accounting and the icon promotion (the raster diff, E3, is NOT
  Design's — it is live item 7 below); **§F** the sourced figures for the seed spread (Elias — OECD PMR
  and SOCX; the mechanism is built at `6df94de`, every slot a placeholder, the trajectories byte-identical);
  **§V** the built-but-unconfirmed surfaces (Elias's eyes — playtest 3 of 2026-08-27 cleared ten of
  seventeen and ruled the three findings the same evening; the portrait size and the cut are built and
  captured at `4e5adbf`, the compass waits on §F); **§P** the three felt verdicts (a playtest).
- **LIVE** — the list below, in order.

---

## Live work — startable today

### 1. Scheduled next: the causal-graph screen (trigger FIRED 2026-08-25 — over-fired)

The original trigger was *"the ledger carries a second stat's terms"*; the ledger now carries THREE
(Approval, Consumer Confidence, Debt — Step 2 v1 and its third section, `COMPLETED.md` §30), and the
term IDs ARE the derived stat → stat edge list. Queued per the fiscal-chain precedent: **derived, never
authored** — which is also the structural fix for the Policy Web's declared edge list, whose signs
drifted once already (the 2026-08-27 edge sweep, CLAUDE.md). Startable by Claude with no external
input; the surface (a screen, a panel section, or the web itself reading the ledger) is the first ruling
the pass takes under rule 4. **The Policy Web gaps below are sequenced behind it.**

### 2. Content backlog — the two remaining scenarios (ruled 2026-08-26: keep, build when elected)

Specs migrated from the Step 3 package before its deletion (the format, the evaluator and the first two
scenarios are shipped — `COMPLETED.md` §30; `ScenarioLibrary.cs` holds exactly two entries today):

- **Poland convergence.** *Deltas:* the seeded 3.0%/turn potential, 59% debt. *Objectives:* sustained
  real-wage growth with inflation in band N consecutive turns. *Fail:* inflation > 6% three turns
  running. **Why hard:** growth is the easy half — the tightness → wage → (Q2) sentiment → consumption
  loop plus the Phillips curve means a convergence boom overheats itself, and the Taylor rule answers
  with rates. ⚠ Measure against `UnemploymentReversionSpeed` FIRST — it dropped two scenarios on one
  root cause (`COMPLETED.md` §§22/30).
- **The Unequal Recovery.** *Deltas:* elevated Gini, a hostile seat composition. *Objectives:* Gini back
  to baseline without losing a confidence vote. *Fail:* approval < 30. **Why hard:** every lever that
  closes the Gini gap (welfare, minimum wage, tax) runs through Parliament, and each failed bill charges
  approval — while the Q1 Gini term is itself pushing approval down until it closes. This is the
  scenario that proves Step 2's output is load-bearing.

### 3. The four remaining budget decompositions — Germany, France, Italy, Poland

Sweden's 24 sourced utgiftsområde lines shipped 2026-08-25 (ruled: decomposition now, Sweden first) and
the recalibration (build-order item 1, `290d4ee`) means the other four now decompose CORRECT totals.
Unscheduled, startable, one country per pass on Sweden's method (real budget documents, retrieval dates,
all-discretionary and not-byte-identical deviations stated with measured reasons — CLAUDE.md "Item 4
BUILT").

### 4. Chrome and UI residues — small, no gate, one pass could take them all

Each is a `POLISIM_V2_SCREEN_SPEC.md` clause the 2026-08-27 sweep found unbuilt or half-built, or a
capture-width item; none needs Design, a ruling, or a playtest first.

- **The status line's RUNNING state (§A.6, B8's second carrier):** only the HELD half is dressed
  (`DrawHoldBannerLabel`); the running branch is a bare label — the `#EDE2CB` plate on `1px #C9BA9B`
  with the `8px #3E8A5F` dot is unbuilt. The "Clock running" copy may be renamed in the same edit.
- **The speed buttons' held-state face (B5):** `DrawSpeedButton` keys on `selected` only; the
  disabled face (`#DDD2B8` / `1px #C9BA9B` / `#9A917D`, rendered never omitted) has no branch, and
  `ui_btn_disabled` has no loader anywhere in `Assets/Scripts` (the `UiPalette.BuildButtonStyle` comment
  that claimed one was corrected 2026-08-27). A third `ButtonKind` branch on one method.
- **The right-aligned screen caption (§A.8):** `DOMESTIC BULLETIN — DESK READINGS, LIVE` — B6's
  live/published carrier at screen level. Not built; nothing calls for it; §A.8a's "live desk reading"
  state is defined as sitting under it.
- **The inactive tab-swatch tints (§A.3, third column):** delivered as snapped values, never wired —
  the tab swatch draws the area ink today.
- **Three §A.2 ink tokens without a constant** (`ruleRow #D5C8AB` — the row-separator weight — among
  them; the sweep found two more; re-derive the list against `PoliSimTheme.cs` before building).
- **§A.11's urgency chip** (its `1.5px` border and `−2°` rotation — today a plain `DrawColoredLabel`)
  and **the generic stamp treatment**; **§A.13's two envelope rows** with no implementation (re-derive
  which two against `GameController`'s takeover seam before building).
- **International's two and the Fed's concatenated labels** — the row family's last residues; the
  Fed's count is stale since pass 4 rebuilt the central-bank tab (`513b348`) — re-measure, then fold
  into whatever next touches those screens.
- **The 2560×1440 Trade bill card wrap** (pass 6, Elias: cosmetic): the cost line wraps after the "+"
  of "+$0/yr" — `UiFormat.MoneyDelta`'s sign glyph read as a break opportunity — while the other three
  sizes wrap at spaces (`p6usa2560_06e_policylaws_trade` beside `p6usa1280_*`). Reorder the sentence
  so the money delta does not sit at a wrap point, or give the label the explicit measured width the
  free-aspect pass gave its siblings.
- **1920×1080 — the one uncovered capture size**, the most common desktop resolution: a command-line
  argument (`-shotwidth=1920 -shotheight=1080`), no code change.

### 5. Delivered art with no call site — place it or hold it knowingly (`COMPLETED.md` §33)

Re-derived 2026-08-27 from every sprite's call site; the counts are cached values with no expiry
(rule 12) — re-run the trace before acting on them.

- **25 of the 43 `Stats/` sprites** have no loader: 19 `icon_stat_*` for stats without a `StatNodeId`,
  plus `icon_trend_up/down/flat`, `badge_preliminary/revised` and `icon_release_marker`
  (`GraphRenderer` draws markers and `PublishedFigure` draws badges procedurally). Either widen the
  display surface or record the 25 as held stock in `IconLibrary.cs`'s doc.
- **8 of the 10 `icon_area_*` icons** are drawn and reachable but nothing asks for them (only fiscal and
  political, on the tab bar). Candidates: the sub-tab rows, the Policy Web wedge heads.
- **7 chrome names with no load call** — `ui_frame_double`, `ui_btn_disabled` (item 4), `ui_stamp_draft`,
  `ui_portrait_frame_oval`, `ui_btn_paper_canvas` (+`_hover`, `_pressed`); the 2026-08-12 "revivable by
  ruling" set plus the Canvas paper button the pilot never needed.
- **One coverage check that does not exist:** `AreaIconCoverageCheck` — no check enumerates area
  icons or emblems, so their coverage is asserted from the filesystem alone (rule 14).
  (`PortraitCoverageCheck` was built 2026-08-27 with the Progress5 import and runs in the suite:
  every `CandidatePool` minister, every Fed chair and the seeded sitting chair, through `IconLibrary`'s
  own accessors.)
- **The sitting turn-0 Fed chair** (Harriet Ellsworth, `WorldFactory.cs`, deliberately outside the
  candidate pool) has no portrait and no call site asks for one: decide the sitting-chair row's
  treatment. If it gains a portrait, that is one new asset and a fresh Design ask.

*(Two items left this list on 2026-08-27 by Elias's ruling: the eleven superseded SVG sources are
deleted, and the three dead widgets are deleted — `COMPLETED.md` §§29/33.)*

### 6. The label-clipping CLASS — open as a watch item (P4)

`PoliSimWidgets.MeasuredLabel` (measure in the rendering style, shrink never truncate) was implemented and
the known sites swept; the class has kept producing instances on NEW AXES since — #12 the frame itself,
#13 the ECB sub-tab through the COUNTRY axis, the 2026-08-26 width-less-label class (`CalcSize` ignores
`wordWrap` without an explicit width — six instances, fixed under the minimum-window ruling), the 2560
wrap above. The sibling survey (constant-sized chrome under wrappable labels) is named-not-fixed in
CLAUDE.md. **The class closes only by a capture-matrix pass at all supported sizes showing no new
instance**; rule 15's paired-detector correction is its standing discipline. Instance history:
`COMPLETED.md` §§17/32.

### 7. The rasterization diff — our half (moved here from the blocked register 2026-08-27, Elias's ruling)

Design asked (2026-08-11) that their strip-cut PNGs be diffed against our own rasterization once before
the pipeline is trusted; **Design's half closed 2026-08-17** (six per-state button PNGs re-rasterized
fresh from SVG, pixel-diffed 6/6 identical). Ours is a tooling pass, not a wait: `StripCutDiffCheck`
exists with the full tolerant-compare machinery, and a rasterizer exists on this machine (Unity's
built-in vectorgraphics module tessellates every `Chrome/Source/` SVG at import) — but the module's
`RenderSpriteToTexture2D` path yields a BLANK texture under the batch harness, probed and viewed rather
than inferred (never attributable to an SVG `<pattern>` parse limit — `ui_slider_track`'s features are
`linearGradient` + `currentColor`; corrected 2026-08-26). **Closes when either a render path in this
repo produces comparable pixels or an external rasterizer is installed here.** It had sat under "Waiting
on Claude Design" — a prerequisite attributed to the wrong supplier is one that lapses.

### 8. Board 1k — the calendar panel as ONE almanac sheet (answered 2026-08-27; NOT STARTED — Elias's call)

Design answered request §2 by drawing (`POLISIM_V2_SCREEN_SPEC.md` §A.16 carries the rulings): the
" X" suffix retires for a single diagonal ink stroke through the numeral (1.5px at 1600 / 2px at
2560, ink at 55%, ≈ −24°, inset 2px); the dots-vs-ledger split stands and the ledger row repeats the
grid's own 5px dot; the month flip stays instant; a saturated day (the 4-dot cap) gains a 2px ink
underline beneath the dot row; header, grid and ledger become one paper sheet separated by rules, one
scroll. No sprite — `RoundedCard`/`Rule`/`Pill` draw everything; measurements stay measurements. A
UI-only pass under rule 0's bar (compile + capture, the calendar panel at the four sizes, the
`capfold_83a` density case re-captured).

### 9. Board 1l — the graph-weight ruling R-G1…R-G5 (answered 2026-08-27; NOT STARTED — Elias's call)

Design answered request §3 as pixel rules, no art (`POLISIM_V2_SCREEN_SPEC.md` §A.16): history 3
buffer px (from 2), solid; projection stays 2 px, lighter, dash cadence re-cut to 3 on / 2 off;
threshold stays 1 px amber — a 3 / 2 / 1 weight order; sparklines `max(2, round(rectHeight / 34))`
device px; the 300×90 buffer may stand (if raised, restate the rule in device px); release-point
markers scale to weight + 2 px; the green/red deltas, the PRELIMINARY badge and the 1px revision
frame do NOT move. Lands as one constant and one cadence change in `BuildSparklinePixels` under the
existing 336-combination regression set; the eye's check is four stacked graphs at 2560 where history
plainly outranks the amber reference — the inverse of `couple2s2560_02a`.

---

## Queued at named triggers — not startable, and no named party owes anything

The roadmap's third category: real work whose trigger has not fired. Two of these share a trigger (a
capital stock), so if one ever ships, two fire together.

- **Per-scenario term accumulation** (the epilogue's named v1 upgrade) — trigger: the first scenario
  whose epilogue reads wrong without it.
- **Investment deepening (R-Q5e)** — return trigger: a capital stock ships, or I/GDP measures cyclical
  (both conditions recorded with the deferral, `COMPLETED.md` §22).
- **The identity's government-consumption block** (queued 2026-08-26 by pass 4's derivation — the
  honest form of its rejected branch A): the national-accounts identity's G is discretionary lines only
  (mandatory transfers excluded, correctly, but general-government consumption is nowhere), so every
  country's level output gap is a share-determined fixed point no seed can close (re-measured at HEAD
  after the recalibration, which turned the EU-five gaps from drifting series into stable levels — USA
  −14.5%, Poland −7, Italy −4.5, Germany −2.7, Sweden −0.8, France −0.5; `COMPLETED.md` §22's Q5 table
  carries the pre-recalibration figures; the map r* = [s + (1−s)·(G+NX)/Pot] / [(1+g) − (1−s)·a]
  reproduces the dumps to ~0.02 pp). Closing it means a government-consumption term in the identity and
  six re-solved potentials — a seventh-scale discontinuity across all six countries and Okun's anchor —
  which is why pass 4 fixed the RULE instead. Trigger: the first mechanic that needs the level output
  gap to mean something (a capital stock, an investment-deepening return, or a displayed "output gap"
  stat).
- **The un-voted "Reset to Default" click — a NAMED GAP (Elias, 2026-08-27), not a note.** It is the
  same shape as the free lever pass 6 priced: resetting a partner override is an immediate, structural
  on/off on the Trade tab (`GameController.DrawTradePartnerRow`, the `TaxLine.IsImplemented` idiom), so a
  player who has taken the take can cut back to the standing rate for free, un-voted, and the partner's
  mirrored tariff lifts at the next boundary — a live path around the pricing (a cut through the bill
  would read negative on the fiscal axis and pass at most compositions anyway, but it would be voted,
  delayed 21 days and visible in the division log). Closing it means either the reset riding the Trade
  bill like the rate does, or the mirror lingering a boundary after a reset (retaliation memory, which
  the model has no state for today). The harness's `PolicyDecision.PartnerTariffOverrides` path also
  bypasses the vote; that is the harness's privilege for every lever, not a player path. **Trigger: the
  first pass that touches the Trade bill's introduce/reset flow, and before item 10 opens the vote to
  real parties.**
- **Pass 6's deferred set** (reasons in CLAUDE.md "Pass 6 ships"): trade volumes indexed to GDP (moves
  NX on every baseline — its own force; when it lands the wedge must become `Δτ̄ × m` with an explicit
  rate anchor, recorded in code); retaliation against a base-dial hike (no excess to mirror — needs a
  seed-anchored base reference; the dial is voted; reach 0.49% of USA GDP); retaliation memory or lag (no
  diplomatic state exists); a trade axis for the vote (item 10's, where real parties land).
- **Policy Web gaps the edge sweep named (2026-08-27; CLAUDE.md "The Policy Web edge sweep" is the
  record of what it FIXED) — sequenced behind item 1 above.** Three real channels the web cannot yet draw
  honestly: `InterestRateDecision → DebtToGdp` (a direct interest-cost channel for five countries, but
  FALSE for the USA whose debt rate is anchored at `BaseDebtInterestRateOverride`; an edge is one truth
  for all six, so it needs a per-country edge set or the widget noting the exception); the two
  generic-line folds — `SpendingCategory.InfrastructureAndDevelopment` onto the Transportation node and
  `HealthcareAndSocialCare` onto HHSDiscretionary — so the five non-USA portfolios' lines draw the growth
  and confidence edges their USA twins draw. Indirect effects (the incarceration cost reaching debt
  through PrisonPopulation, MinimumWage → Approval through Gini, FamilyPolicy → DependencyRatio through
  BirthRate, TariffPolicy → Gdp through TradeBalance) stay undrawn by the web's own convention. The
  causal-graph screen derives edges from the ledger's term IDs rather than authoring them — the honest
  fix for a declared list that drifts.
- **Riksbank-B** is NOT on this shelf: it waits on item 10's appointment machinery, a named task —
  `MISSING_PREREQUISITES.md` §D.

---

## Standing constraints — rules that bind every pass, kept here because they are not tasks

- **Numbering is stable.** Master Sequence items 1–8 and Steps 1–6 are cited throughout CLAUDE.md and
  the code; never renumber. ⚠ TWO items carry "9": the macro overhaul (2026-08-01, cited as "step 9" in
  code comments, `COMPLETED.md` §§6/9/25) and the v2.0 overhaul (2026-08-03, `COMPLETED.md` §27).
  Disambiguate by name when citing.
- **THE CRITICAL CORRECTNESS RISK — published vs live.** The player-facing UI reads the PUBLISHED
  (lagged, possibly-revised) series; every internal system — Okun's Law, the Phillips Curve, the Fiscal
  Reaction Function, sector integration — keeps reading LIVE values. A leak makes the model consume its
  own stale output, and the effect may not appear for hundreds of turns. The one-directional rule
  (`PublicationSystem` writes `Country.Published`, reads `Country.State`, never the reverse) is the
  enforcement; the 55-call-site count is a 2026-08-01 snapshot.
- **`[GAP]` figures are Elias's to source, never to invent.** The seed doc's variant-axis rule
  (`POLISIM_SEED_DATA_MACRO_OVERHAUL.md` Part 4; `MISSING_PREREQUISITES.md` §B) governs any re-sourcing.
- **If a step's own validation fails, fix it before moving to the next** — never proceed past a failing
  step to "make progress" on the next one.
- **Calibration stays at turns 100–200; t1000 is a diagnostic, never a target** — judge a fix by whether
  the mechanism is present and correctly signed. **The word "equilibrium" stays banned without a run that
  earns it** (the unbounded-divergence block's two surviving rulings, `COMPLETED.md` §32).
- **SCREEN GRANULARITY, NEVER ELEMENT GRANULARITY.** IMGUI composites as one flat rectangle and no Canvas
  render mode draws above it; a screen is either Canvas or IMGUI, never both interleaved. **Any request
  that violates this silently is a request to migrate that screen wholesale to Canvas**, and should be
  recognised as such rather than hacked around (`COMPLETED.md` §27).
- **WHAT MUST NOT REGRESS — eight load-bearing behaviours**, each of which fixed a real defect,
  catalogued in CLAUDE.md. The appearance may change completely; the FUNCTION may not: the amber draft
  cue; direction-aware green/red (`GetDeltaColor`, keyed to *good*, not to *up*); the `MoneyUnit`
  formatter (a call site must not be able to render currency without naming a unit); `MeasuredLabel`'s
  shrink-never-truncate; stable control layout; the published/live distinction; per-area colour
  identity; the always-visible interrupt indicator.
- **Spec references:** anything row-shaped is `POLISIM_V2_SCREEN_SPEC.md` §A.9 with §A.9a (the resort
  ladder, numeric variant included), §A.9b (negative fill = no gauge) and §A.9c (Parliament's real
  pointer; §A.10 is Buttons). **Every number the spec supplies is suspect until derived or explicitly
  confirmed as fixed** (the spec's own banner); **declare deviations from the boards rather than
  diverging silently** — the V-series record is `COMPLETED.md` §24.
- **One row type per capture, never batched.** The tax row took three capture rounds and each found a
  defect code review had passed.
- **The guard scopes** (`UiOverflowGuard` — does text fit its rect; `UiContainmentGuard` — does a child
  rect sit inside its container; `ScreenEdgeCheck` — four pixel lines per PNG, right and bottom,
  flushness not magnitude, at the captured resolutions only) **and DO NOT BUILD A THIRD SITE-SPECIFIC
  GUARD** — a GUILayout-aware check needs IMGUI internals; the pixel check is cheaper, exists, and asks
  the question the player experiences. Any reflective guard must be justified against `COMPLETED.md`
  §32's paragraph.
- **`stranded/politics-elections` stays as-is until item 10 is scheduled, and its layout work is not
  extracted** without a failing measurement to justify it (rulings 2026-08-11; `COMPLETED.md` §32;
  the branch's contents inventoried in `MISSING_PREREQUISITES.md` §D).

---

## Open Questions — a record of decisions, not a queue (rule 4)

**No open question at HEAD (2026-08-27).** Every entry this section held was ruled, closed or migrated:
decisions live in `COMPLETED.md` §§11/23/32; questions waiting on a named party live in
`MISSING_PREREQUISITES.md` §A. A new question is written here only until it is ruled, and a ruling given
in chat and not recorded did not happen.

---

## When Elias returns to this document

- Read **The board** above, then the live list — both re-derived, never edited forward. If the board
  disagrees with `git log`, the log is right and the board is stale.
- `MISSING_PREREQUISITES.md` **§V and §P** hold what needs your eyes and your play; **§S** holds the
  send package.
- Review the commit log — each unit of work is its own commit, validation results in the message or
  CLAUDE.md.

---

## Document set and the consolidation rule

**Established 2026-08-02 in the first consolidation pass; run again 2026-08-26 and 2026-08-27. This is
the standing pattern — run it whenever the live documents start describing finished work.**

Ten files at the repo root, each with one job. If a fact belongs in two of them, it belongs in the one
further down the charter table; the four scoped documents below it are not a second home for anything.

| Document | Holds | Grows or shrinks |
|---|---|---|
| `POLISIM_MASTER_ROADMAP.md` | **Live work only** — startable today, plus the trigger shelf and the standing constraints | Shrinks |
| `MISSING_PREREQUISITES.md` | Blocked work, by supplier — including built-but-unconfirmed work waiting on Elias's eyes (§V), the home the deleted `VISUAL_REVIEW_BACKLOG.md` used to be | Shrinks as blockers clear |
| `CLAUDE_DESIGN_ASSET_REQUEST.md` | The single standing asset request, derived from the codebase | Appended to, then emptied on delivery |
| `POLISIM_SEED_DATA_MACRO_OVERHAUL.md` | The Round-4 macro stats' real-world figures and the release schedules, with seven marker kinds (`[VERIFIED]`, `[ESTIMATED]`, `[GAP]`, `[PARTIAL]`, `[PROVISIONAL]`, `[BOUNDED]`, `[DERIVE]`) and the sourcing rules that govern any re-sourcing | Reference; stable |
| `COMPLETED.md` | Finished work + lasting decisions and lessons | Grows |
| `CLAUDE.md` | The detailed technical record. **Never superseded** | Grows |

**Scoped documents (not roadmap material, kept while they are load-bearing):**

| Document | Job | Retires when |
|---|---|---|
| `POLISIM_V2_SCREEN_SPEC.md` | The v2.0 visual conventions the code cites by section (`LedgerRow.cs`, `GameController.cs`), and the spec of the one unbuilt screen (1h) | never as a whole — a spec is a reference; its finished history moved to `COMPLETED.md` §24 |
| `LAW_BROWSER_BOARD_RULINGS.md` | Design's Screen 1i rulings, the build target two `GameController.cs` comments cite | the `board1jc*` eye review closes and the comments are repointed |
| `CLAUDE_DESIGN_BOARD_1I_NOTE.md` | An outbound courtesy note — attachment of the §S send package | the package is sent |
| ~~`POLISIM_R4_4_PREREPORT.md`~~ | The R4-4 ruling package — **consumed to `COMPLETED.md` §19 and deleted 2026-08-27** when D1's portraits landed, per §22's ruling | — |

Deleted under this rule: `VISUAL_REVIEW_BACKLOG.md` (2026-08-02), `POLISIM_MACRO_OVERHAUL_DIRECTIVE.md`
(2026-08-26), the three scoping packages and the derivation reports (2026-08-26, `COMPLETED.md` §§21/22).

### The three-way test every task gets

1. **Finished?** → `COMPLETED.md`, then delete from source.
2. **Waiting on a named party?** → `MISSING_PREREQUISITES.md`, then delete from source.
3. **Neither?** → it stays live.

**"Built but unconfirmed" and "built but uncalled" are case 3, not case 1** — or case 2 when the only
thing missing is Elias's eyes, which is a named party. They are the two states this project keeps
mistaking for done: both were found again in every pass so far, and the 2026-08-27 sweep downgraded 26
of 103 proposed DONEs on exactly this ground.

### Rules learned from doing it three times

- **Verify against the repo and the commit history, not against a summary.** The first pass found Step A
  marked *"DONE, commit `e3a0feb`"* with Tier 0 derived stats folded in — but that commit contains two
  files and neither is `DerivedStats.cs`, which arrived at `70798e9` carrying "NOT trajectory-validated"
  in its own message. A summary is exactly where that error hides.
- **Check callers before believing a feature exists.** A4 validated cleanly and displayed nothing; all
  four new files from 2026-08-01 had zero callers when checked. `grep` for the call sites, do not assume
  the wiring landed with the code.
- **If removing finished items empties a document, delete it.** An empty shell drifts back into use.
- **Do not duplicate a live list into the blocked register** — or the blocked register into this file.
  The 2026-08-27 pass found the register's five rows restated here, D1 restated three times, and item
  10's gate stated three times in one section. Two copies of one list is the drift this pass exists to
  undo.
- **Repoint references before deleting a file, and grep afterwards to prove nothing dangles** — source
  comments included. The 2026-08-26 pass missed five; the 2026-08-27 pass found four more pointing at
  files deleted on 2026-07-30 (`ROADMAP_BRIEF.md`) and sections deleted on 2026-08-26 (the request doc's
  §1F/§7).
- **A document can assert two states of one task at once.** "Still to build" and "DONE 2026-08-02" stood
  197 lines apart in this file for 25 days. When a live document is edited, search it for the task's
  other mentions before saving.
- **A capture is a harness film, not Elias's eyes.** "Pinned on film" and "verified both sizes" are
  containment evidence (rule 15's first layer); a strike-through that closes a visual item on that
  evidence alone is the conflation this rule exists to catch. The record of a sighting names the
  session.
