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
   exited before batch runs. **The push (R-SP1, 2026-08-28, standing — supersedes R-D1's
   one-push scope): sessions push at pass end, fast-forward only.** Procedure: `git fetch origin`;
   if `origin/main` is an ancestor of HEAD, `git push origin main` (no force flag of any kind, ever,
   from a session); re-fetch and confirm `origin/main == HEAD`. Any non-fast-forward state, any
   lease or credential surprise, anything that would want `--force*`: stop and hand Elias the exact
   state — force remains exclusively his. `UpstreamCheck` stays armed as the tripwire; its
   threshold is now one a session clears itself rather than a red Elias inherits.
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

## UI v3.0 — the era (opened 2026-08-28; founding document `POLISIM_UI_V3_DIRECTION.md`)

**The direction is `POLISIM_UI_V3_DIRECTION.md` at the repo root** (installed 2026-08-28, Phase A's first
commit): the thesis ruled as V3-R1 — *the desk with fewer words, not a different desk* — the three pillars
(the fold V3-R2, The Desk V3-R3, the cut), what v3.0 is NOT, and validation continuity V3-R4. Its rulings
bind every v3 pass; the struck alternative (a new visual idiom) is one line to un-strike there, never here.
The sequence, as written there:

- **Phase A (now, one session):** census · shell + rail built and guarded in both states · the instrument
  inventory with measured minimum sizes · the Design request written as the request doc's next ask. *The
  shell builds before the board because it is structure, not aesthetics — it gets re-skinned, not
  re-architected, when the board lands.*
- **Send (Elias, one gesture):** the request doc now carries §E5 + the v3 ask — hold the pending
  request-doc send until Phase A lands so one send carries both; the courtesy note can go any time.
- **Phase B (on Design's boards):** The Desk built against the board; the (b)-class returns resolved;
  capture family `v3desk_*`. **BUILT 2026-08-28**, the day boards 1m and 1n landed (`COMPLETED.md` §41).
- **Phase C:** per-screen fold defaults tuned on film; §P's density verdict re-read on the folded stage.
  **CLOSED 2026-08-28** — the defaults ruled as a table (R-PC2), the note to 1i–1n, one current paste
  (`COMPLETED.md` §42); §P is now yours, recommended after this pass so the density verdict is read on
  the real stage.
- **Item 10 lands inside v3:** election night is born on the v3 shell — the Desk folded, the map as the
  stage. **Fallback, stated:** if Design's board has not landed by the gate, election night builds in the
  OPEN state (pure v2, fully supported) and moves to the stage later; the shell ships either way, so
  nothing converts twice.

**Phase A CLOSED 2026-08-28** (the kickoff `KICKOFF_V3_PHASE_A_2026-08-28.md`, delivered with the direction
in `Direction.zip`, both archived out of tree at `../PoliSim-captures/inbox/`): the census taken and its
pure decoration cut; the shell and the rail built and guarded in both states at four sizes with the
trajectories byte-identical; the instrument inventory measured on the ladder films; the eighth request
written as the request doc's §1 with its three annexes; the send package regenerated. Record:
`COMPLETED.md` §39, `CLAUDE.md` "UI v3.0 Phase A (2026-08-28)". **Phase B BUILT 2026-08-28** — Design's
two boards (1m "Screen 0 — The Desk, folded", 1n "the rail") landed on the live screens file the same
day and Screen 0 was built against them (`GameController.Desk.cs`; the boards as read into the spec's
§A.17; the build's fourteen reversible calls R-B1…R-B14 in `COMPLETED.md` §41; the `v3desk_*` family on
film at four sizes). **Phase C CLOSED 2026-08-28** — the fold-default table ruled (a screen defaults
FOLDED only if its content is designed for the full-width stage: the Desk and Budget, both locked;
Statistics › Domestic reverted to OPEN and filmed at 1280 and 2560), R-B2/R-B3/R-B4 ratified standing,
the courtesy note rewritten 1i–1n-aware, the send package regenerated as one current paste
(`COMPLETED.md` §42). **v3.0 Phases A–C are closed.**

**UI v3.1 opened and its Phase A CLOSED the same evening (2026-08-28, `COMPLETED.md` §44)** — from Elias's
first live sitting on the v3.0 build: the OPEN state retired on the duty audit (ONE FRAME: the rail and one
full-bleed sheet on every screen — the direction doc's v3.1 section, its table one row), the rail's HOME
cell (the flag, the structural interim), the PAUSE/RUN chip in the fold toggle's freed cell, the game-over
reason on the banner everywhere, six annexes measured (the audit, the icons at the real cells, the paddings
and dead-space shares, the sitting's findings, the Statistics census, the ink-pair contrast table), the
ninth request installed and the paste regenerated. **v3.1 Phase B CLOSED the same night (2026-08-28,
`COMPLETED.md` §45)** — Design answered the ninth request in full the evening it was sent, and the five
answers were built in order against the `v31b_*` matrix: the §E5 close (the hatch's third cut measured at
7.42 %, the residual rasterizer edge coverage — the bar question is Elias's), D6's inks with Annex F
re-measured, D4's tokens mechanically (and the paper sprite's shadow moved outside the box rect — a v2.0
defect the first film exposed), 1n-r2's captioned rail, 1m-r2's Desk with its Year-0 empty states, 2a's
Statistics as instruments; then the OPEN state's residue deleted whole (§44's promise). The dead-space
share re-measured after D4 says the reclaim has nowhere to go on content-short screens until they are
re-composed the way 2a re-composed Domestic — filed for Design's next look in the request doc's Annex C.
**The era's live edge:** nothing of v3.x is out for signature — the next ask starts at the request doc's §4
(redrawn rail glyphs, refused as a costed follow-up) and Annex C's corrections; §P (the three felt
verdicts) and the hatch pair's bar ruling are Elias's; 13 September (item 10, election night born on the
v3 shell). Nothing of v3.x is startable by a session today.

---

## Where things stand — re-derived 2026-08-28 (the omnibus, its continuation and the clear-out; HEAD `076273a` + the closing commits)

**This document holds only live work.** Everything finished is in `COMPLETED.md`; everything waiting on a
named party is in `MISSING_PREREQUISITES.md`; the split is the standing pattern at the bottom of this
file. **A task is live here only if someone could start it today.** Built-but-unconfirmed and
built-but-uncalled are neither finished nor live — they wait on Elias's eyes and sit in
`MISSING_PREREQUISITES.md` §V.

| Document | Holds |
|---|---|
| `COMPLETED.md` | Finished work and lasting decisions. This file shrinks into it, never grows |
| `MISSING_PREREQUISITES.md` | Work waiting on a named party — Elias's send, decision, eyes or playtest; Design's delivery; item 10 |
| `CLAUDE.md` | The detailed technical record for both. **Never superseded** |

**The board, stated once (verified at HEAD `076273a`, 2026-08-28 — the clear-out's Phase 4, the remote holding the tree through Phase 1; re-derive it, do not edit it
forward):**

- **DONE** — Master Sequence I (items 1–9) and Master Sequence II Steps 1, 2, 3 and 5; Round 4; the
  fiscal-engine arc; the law system at 100 of 100 in two categories; the ruled build order (five passes)
  and the shelf's first item, tariff costs (pass 6); **the omnibus pass of 2026-08-28** — every live item
  of the 2026-08-27 board: the causal graph on the Policy Web (item 1), the two remaining scenarios
  measured and dropped (item 2, the §22 precedent), all four budget decompositions (item 3), the chrome
  and UI residues (item 4), the delivered art placed or held knowingly (item 5), the rasterization diff
  (item 7), boards 1k and 1l (items 8–9), the seed spread sourced (§F; confirmed 2026-08-28). Records:
  `COMPLETED.md` §§27–31, 34, 36. **The continuation of 2026-08-28** — the ruling queue drained (R-C1…R-C4,
  R-C7), the one-line law row (R-C1), the raster check's two damage-class budgets (R-C2), the seven film-gap
  captures (R-C6), the three seed quality debts settled (R-C5). Records: `COMPLETED.md` §37. **The
  clear-out of 2026-08-28** — the two riders (R-D3), the Reset click draft-only (R-D2), the push (R-D1),
  the send package, the §V index (R-D5), the three playtest saves (R-D4), the prereqs file live-only.
  Records: `COMPLETED.md` §38. **UI v3.0 Phase A (2026-08-28)** — the direction installed, the landing
  screen's text census with its (c) cut, the fold shell and the icon rail (V3-R2; Budget locked FOLDED,
  R-A1), the instrument inventory with measured minimums, the eighth request (two boards) and the
  regenerated send package. Records: `COMPLETED.md` §39. **The stage-prep micro-pass (2026-08-28)** —
  R-SP1 (sessions push, fast-forward only) and R-SP2 (legal in every reachable state) recorded; R-SP3
  verified on film (one sparkline renderer, R-G4's floor already on it — Annex B corrected); R-SP4 the
  compass's honest footprint, containment-asserted; R-SP5 the map's names on §A.9a's ladder with the
  harness's 4 px separation assert. Records: `COMPLETED.md` §40.
- **WAITING, NOT LIVE** — `MISSING_PREREQUISITES.md`, live-only since the clear-out: **§S** one paste
  (`SEND_PACKAGE_2026-08-28.md`); **§A** the coupling queue Q6–Q10 at their triggers — nothing else;
  **§D** item 10 and everything riding its gate — 13 Sept 2026, Sweden votes — including election night,
  Step 6, Riksbank-B, the stranded branch, the party marks, and the political-model fact (no expansionary
  bill passes on any drift path before the re-seeding); **§E** Design: §E6's boards LANDED and were built
  2026-08-28 (the row retires at the next re-derivation), §E5's Design half CLOSED 2026-08-28 (the hatch
  cut three times, 7.42 % after the third — the bar ruling is Elias's, not an ask), and §E2/§E4; **§V**
  every surface on film — the `v3a_*` family and the ladder films included — one sitting through
  `../PoliSim-captures/sv_index.html`; **§P** the three felt
  verdicts, each a staged save — load, play, judge — read in the no-expansionary-passage context (R-C7).
  Every tombstone the file carried is migrated to `COMPLETED.md` §38a; what a database session still
  owes is seed §8's `[PROVISIONAL]` → `[VERIFIED]` upgrade.
- **LIVE** — the list below.

---

## Live work — startable today

### 1. The label-clipping CLASS — a watch item, not a task (P4)

`PoliSimWidgets.MeasuredLabel` and the sweeps stand. The omnibus closing matrix at 1280×720, 1600×900,
1920×1080 and 2560×1440 (`omni_final_*`, 2026-08-28) produced **instance #14** — the Laws panel's box
widths taken one nesting level short, the panel 28 px past its frame at every size, caught by
`ScreenEdgeCheck` at three of the four sizes (the fourth hid it under the margin column), measured by a
width probe and fixed under the same gate (`COMPLETED.md` §36, `a331e82`) — together with the class's second
member on that screen, the detail pane's scroll content wider than its viewport (a second probe; the
MAGNITUDE row, the action button and the name/status row sized to the pane). After the fix: `ScreenEdgeCheck`
clean at all four, the two text guards silent, the rule-15 diff against `pt3usa*` read by eye. The class stays open
as a watch item under rule 3's discipline — this instance is the thirteenth's lesson again (a width
budget computed against the wrong container) and the gate is what found it; nothing is startable until a
capture shows another. Instance history: `COMPLETED.md` §§17/32/36. The continuation (2026-08-28,
`a7d877d`) rebuilt the law rows ONE-LINE under R-C1 — a new row type, captured on its own round at all
four sizes (`cont_p1b_*`), the two text guards silent, `ScreenEdgeCheck` 0 clipped — and the class did
not reopen; the one width-dependent behaviour it added is stated in the code (the category token steps
out only where the fixed cells' floors cannot carry the widest visible name at the guard floor).

### 2. Nothing else is startable today

Every other line of the 2026-08-27 board shipped in the omnibus pass, its continuation or the clear-out,
or moved to a named party's queue in `MISSING_PREREQUISITES.md`. Nothing waits on a ruling; the machine
side is idle and the remote holds the tree through the clear-out's Phase 1. Elias's side, each one
gesture: paste the send package's two artifacts · sit once through `sv_index.html` · load three saves
and play · answer §E5 when Design does · 13 September. There is no sixth thing.

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
- ~~**The un-voted "Reset to Default" click — a NAMED GAP (Elias, 2026-08-27), not a note.**~~ ✅ **CLEARED
  2026-08-28 (R-D2 of the clear-out kickoff; the trigger below became a dated deadline, 13 September,
  sixteen days out):** the click edits the DRAFT, never the live state — "Reset draft" returns the
  partner's dial to the standing override and the override's rate moves only through the Trade bill, a
  cut voted like a rise; Set Override unchanged (inert at the effective rate). The alternative — the
  click filing a reset bill — is one routing change away if a playtest ever wants it. Bar: trajectories
  byte-identical, the draft-moved / draft-reset pair on film (`clear_p1_<size>_06m/06n_policylaws_trade_*`).
  Record: `COMPLETED.md` §38. The entry as it stood, for the record: It is the
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
- ~~**Policy Web gaps the edge sweep named (2026-08-27)**~~ — ✅ **CONSUMED 2026-08-28 by the omnibus's
  Phase 2 (`a267fd6`, R-K1):** the per-country edge set (`IsLiveFor` — the policy rate's issuance edge
  draws for the five, not the USA), the two generic-line folds, and the derived-from-ledger-terms edge
  list with the declared idiom for the rest. Indirect effects stay undrawn by the web's own convention;
  `GiniEffect` and the wage-sentiment factor have no node and are said so on the stat panel. Record:
  `COMPLETED.md` §36.
- **The Compass Y formula's implemented-average** (flagged by §F, 2026-08-27; live with real seeds since
  `915c800`): Y averages generosity over IMPLEMENTED programs, so a country with one generous program
  outranks a broad welfare state. Trigger: Elias's §F confirmation, or the first play that reads the
  compass against the six seeded portfolios.
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

**No open question at HEAD (2026-08-28).** Every entry this section held was ruled, closed or migrated:
decisions live in `COMPLETED.md` §§11/23/32/36; questions waiting on a named party live in
`MISSING_PREREQUISITES.md` §A — the omnibus pass's three `RULINGS NEEDED` (A4 the ledger row's one-line
type, A5 the raster check's budget, A6 the anchored form with real seeds in) were ruled by the continuation
kickoff of 2026-08-28 (R-C1…R-C3) and are tombstoned there; the queue holds Q6–Q10 at their triggers. A new
question is written here only until it is ruled, and a ruling given in chat and not recorded did not
happen.

---

## When Elias returns to this document

- Read **The board** above, then the live list — both re-derived, never edited forward. If the board
  disagrees with `git log`, the log is right and the board is stale.
- Five gestures, no sixth (the push is a session's since R-SP1, 2026-08-28 — fast-forward only, at pass
  end; force stays Elias's): paste `SEND_PACKAGE_2026-08-28.md` (§S — the v3.1 Phase A package, the one
  current paste: the request doc through the ninth request with its six annexes, the note unchanged, the
  two sitting screenshots and the four rail crops from Elias's side; it supersedes the Phase C paste,
  which landed — the dated `…-28d` paths are R-PC4a's hygiene); sit once through `../PoliSim-captures/sv_index.html`
  (§V, the shell's rows, the ladder films, the Desk and the ruled defaults now on it); load the three
  `playtest_*` saves and play (§P — recommended now that the stage is real); 13 September (§D). Nothing waits on a ruling (§A holds only the coupling queue at its triggers), and
  `MISSING_PREREQUISITES.md` is live-only — its tombstones are `COMPLETED.md` §38a.
- Review the commit log — each unit of work is its own commit, validation results in the message or
  CLAUDE.md.

---

## Document set and the consolidation rule

**Established 2026-08-02 in the first consolidation pass; run again 2026-08-26 and 2026-08-27. This is
the standing pattern — run it whenever the live documents start describing finished work.**

Eleven files at the repo root (re-derived 2026-08-28: `ls *.md`), each with one job. If a fact belongs in two of them, it belongs in the one
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
| `POLISIM_UI_V3_DIRECTION.md` | The v3.0 founding document (2026-08-28) — the thesis V3-R1, the three pillars (the fold V3-R2, The Desk V3-R3, the cut), what v3.0 is not, the sequence against 13 September, validation continuity V3-R4 | never as a whole — a direction is a reference; its finished phases move to `COMPLETED.md` |
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
