# PoliSim — Missing Prerequisites

**What this is:** every task that cannot proceed because something it needs does not exist yet — a
named external party or a named upstream task must act first. Created 2026-08-02 in the first
consolidation pass; slimmed to the live register 2026-08-26; **rebuilt by supplier 2026-08-27** (the
third consolidation), when the roadmap's riding-gates table dissolved into this file and
built-but-unconfirmed work gained its own section (§V) — the home `VISUAL_REVIEW_BACKLOG.md` used to be
before it emptied and was deleted.

**What this is not:** a backlog of work someone could pick up. Nothing here is startable. Work that is
merely *unbuilt* stays in `POLISIM_MASTER_ROADMAP.md`; work that is *waiting* lives here. "Hard",
"large" and "not scoped yet" are not blockers.

**The register, complete:**

| entry | waiting on | gate |
|---|---|---|
| §S — the send package | ✅ **SENT 2026-08-27** (the request, hash-verified, to Design's project); the 1j courtesy note alone still waits on **Elias — send** | the E2 convention: sending is Elias's — this send was on his instruction |
| §A — the ruling queue Q6–Q10; F2 | **Elias — a decision** | each at its own named trigger |
| §B — three seed quality debts | **Elias — database access** (an OECD/Eurostat re-sourcing session) | none blocks anything |
| §D — item 10, the political game, and everything riding it | **Sweden's vote, 13 Sept 2026, then Elias's pricing decision** | the one remaining spine item |
| ~~§D1 — cabinet portraits, eight outstanding~~ | ✅ **DELIVERED AND IMPORTED 2026-08-27** (Progress5; `PortraitCoverageCheck` 25 of 25) | tombstone below; the look is §V's |
| §E2 — mark accounting + the R5 hexes | **item 10** | 13 Sept 2026 |
| ~~§E3 — rasterization diff, our half~~ | **MOVED to the roadmap 2026-08-27 (Elias):** Design delivered; ours to close with a tooling pass or a rasterizer on this machine — not blocked on anyone | tombstone below |
| §E4 — the icon promotion for R4-1's two Society rows | **Claude Design — the next batch**, behind §D1 | two `StatNodeId` members first |
| §V — built, not seen | **Elias — a visual review** | rule 15's third layer |
| §P — three felt verdicts | **Elias — a playtest** | no measurement can answer them |

---

# S. The send — ✅ SENT 2026-08-27 (the request); the courtesy note still waits on Elias

**The asset request went to Claude Design's project on 2026-08-27, on Elias's instruction, by Claude
Code through `DesignSync`** (project `PoliSim v2 Design Progress`, `b3dec27b-620b-452a-9783-e8317cbec4d9`).
Two copies, because an in-place overwrite of a file Design has already read produces nothing that looks
new (it cost a round-trip on the last send): **in place** at `uploads/CLAUDE_DESIGN_ASSET_REQUEST.md` (the
path every earlier send used and Design has read) and **at a new dated path**
`send/design_request_2026-08-27/CLAUDE_DESIGN_ASSET_REQUEST.md`, with `ATTACHMENTS.md` (what the request
asks, what comes back), `SHA256SUMS.txt`, and the eight captures §2/§3 cite under `captures/`. **Readback
hash-verified on both paths:** `get_file` returned each document complete, and the readbacks hash to the
local file's digests exactly — `9a464915…24eec` (CRLF, as uploaded, 29,571 bytes) / `bf7c2263…7cfb`
(LF-normalized) — diff-identical. What was sent: §1 the portrait verdict and the eight outstanding names,
§2 the calendar panel board request, §3 the graph-weight ruling, with §0/§4/§5 for context.

**Still unsent — Elias's:** the 1j-aware courtesy note (`CLAUDE_DESIGN_BOARD_1I_NOTE.md`, corrected
2026-08-27). It was not part of the instruction that sent the request; it is a note, not an ask, and
carries nothing Design is waiting for. §D1 is no longer gated behind this entry — the verdict is in
Design's project.

---

# A. Waiting on Elias — a decision

**The coupling queue's remainder, Q6–Q10**, each ruled at its own named trigger (the Q1–Q10 queue is a
ruling, CLAUDE.md's Master Sequence II record); nothing is startable until a trigger fires and Elias
rules. **F2 — the rate-cap note** stands as a recorded property, not a task (CLAUDE.md's fiscal-arc
register). Q4 was RESOLVED by R-Q5d (2026-08-18, confirmed 2026-08-26) and is not in the queue.

**A1/A2/A3 — CLOSED, tombstone (2026-08-26).** The rating thrash (review cadence, not damping; closed in
full 2026-08-17), the SWF emergency drawdown (a standalone tier-3 bill, ruled AND built 2026-08-02,
`b1c077f`), cabinet appointments staying UNILATERAL — all resolved 2026-08-02; the reasoning migrated IN
FULL to `COMPLETED.md` §23 so none is reopened as an unanswered question later.

# B. Database access — three quality debts survive (none blocks anything)

**Every figure that blocked a batch was sourced 2026-08-02** — the sourcing history is `COMPLETED.md`
§23; the values, queries and status flags are `POLISIM_SEED_DATA_MACRO_OVERHAUL.md`. What remains is
**QUALITY DEBT, not gaps**, waiting on a re-sourcing session with database access:

| Debt | Where | What would settle it |
|---|---|---|
| **The real-wage row mixes THREE bases** | seed §5 | Re-source all six from OECD Taxing Wages 2025 (one basis, in SDMX). *Correct figures, incoherent set — the housing-overburden defect again.* The index itself opens at 100 for all six by ruling (R4-2), so nothing is seeded from the row |
| **The AHD vintage behind C1's estimates is unrecorded** | seed §1 | Find the year of the four OECD anchors. **Unrecorded vintage is exactly what made 90.86 undecidable** — the canonical example |
| **Three homeownership figures are `[ESTIMATED]`, not sourced** | seed §1 | Italy 74.4 / Sweden 62.1 / Poland 86.8 — rung 3 of the fallback ladder (a fitted bridge with 95% bands), replaced the moment same-basis OECD household figures exist. *Placeholders that play correctly, not facts.* (The USA Gini `[ESTIMATED]` is NOT this debt: the seed doc records it as unfixable by a better number — a scale-equivalence question, not a lookup) |

**Standing rule, three-for-three (kept live here — it governs any re-sourcing):** for any cross-country
statistic, **assume an undocumented variant axis exists** and record the basis alongside every value —
indicator code, population base, threshold, year. Housing overburden had 8 variants where its warning
implied 3; youth unemployment 4 where it implied 2; homeownership 4+ with no warning at all. A bare
number is unfalsifiable later.

# C / D2 / E1 / F — tombstones (closed sections, migrated 2026-08-26)

- **C — visual review:** empty since 2026-08-02; all eleven items confirmed. Record: `COMPLETED.md` §16.
  (Its successor is §V below — a different list, the same supplier.)
- **D2 — Round 4 scoping:** released 2026-08-02; the arc closed 2026-08-17. Record: `COMPLETED.md` §19.
- **E1 — `icon_stat_interestrate`:** delivered the same day it was recorded as awaiting. Record: `COMPLETED.md` §15.
- **F — Step C4's closure:** ✅ **CLOSED 2026-08-17 — the F register's count is ZERO.** The closure
  chain, the 1,416 → 19 measurement table and the double-count fix: `COMPLETED.md` §23.

---

# D. Waiting on another task

## 🔴 D0. Item 10 — REALISTIC POLITICS AND ELECTIONS (gate: Sweden votes 13 September 2026; priced after)

**The one remaining spine item, and the anchor every entry below rides.** Item 10 IS the work specified
in `POLISIM_POLITICS_ELECTIONS_ROADMAP.md` on `stranded/politics-elections` (commit `ca6c510`,
preserved UNINSPECTED): real parties and institutions under the split rule 9 (institutions may be real;
people never are), per-country chambers and electoral formulas, the hybrid national-swing vote model,
USA as the first vertical slice. **Gate, per Elias 2026-08-12: priced after Sweden votes 13 September
2026** — the branch's own seed data carries retrieval dates for exactly this expiry (rule 9's recorded
cost: seed data is now a cached value with an expiry). The branch doc's §1 maps what item 10 replaces on
`main` (`PartyArchetype`, `TotalSeats = 200`, `ElectionSystem`'s approval threshold) and what it keeps
(seat drift, bill scoring, the renderers, `PublicationSystem` for polling) — main's documents describe
the four-archetype system as current because it IS current; the disposition of the collision is item
10's own work.

**Opens as ONE package:** the seed-data refresh from the real result; the Italy allocator pricing
(constituency D'Hondt — the 70-seat error's fix) with the Sweden 2014 six-seat error explained in the
same pass (branch-side claims, VERIFIED AT STEP 4 — the stranded branch's work is proposals to verify,
never merged as-is; the four allocator findings and their provenance are
`POLISIM_SEED_DATA_MACRO_OVERHAUL.md` Part 5, none independently confirmed, `seat_allocation_check.py`
existing only on the branch); the collision-map disposition executed (`PartyArchetype` retires,
`emblem_*` → `mark_*`, renderers re-key, `PartyMarkCoverageCheck`'s "PARTY SYSTEM NOT PRESENT"
honest-nothing flips to real accounting — **ruling R3's verification obligation: the check's reflection
over `BuildParties()` should survive the model swap, to be VERIFIED then, not trusted now**); the
`ElectionRecord` designed against the real model (elections leave only a transient result today —
scoped in CLAUDE.md "Election-night scoping", not built; ruling R2); **election night, Canvas screen 1h**
(3 of 3 — the spec is `POLISIM_V2_SCREEN_SPEC.md` §A.14, the sole item-10-gated content there; its asset
cost: one mark per seated party, count unknown until the seeds land, five `mark_party_*` on disk; the
verdict stamp is the generic §A.11 treatment unless Design asks for baked art at the 1h board); **the R5
hex exchange and §E2's accounting** (below); **the five `mark_party_*` sprites** (`us_rep`, `us_dem`,
`us_lib`, `se_s`, `se_v` — imported, RGBA32, guarded, and drawn by NOTHING on `main`: no `PoliticalParty`,
no `IconLibrary.GetPartyMark`; the `menu_pattern_tile` shape, an asset ahead of its consumer, recorded up
front). **Standing constraint until then (Elias, 2026-08-12, R3): no main-side changes.**

**Riding the same gate:**

- **Step 6 — Story mode** (gate: item 10 shipped). Scoped fresh on the political layer: authored
  multi-beat arcs with memory on the minister/interrupt/ceremony skeleton. Nothing pre-scoped beyond
  the gate.
- **Riksbank-B — independence with appointment influence, the DESTINATION** (playtest-2 item 5, ruled
  2026-08-25: C now, B the destination). The Fed Chair mechanism is the generalization point
  (`Country.CurrentFedChair` non-null is the entire gate; seeding Sweden a governor enables it
  mechanically today). Gate 1 — the output-gap distortion — ✅ CLEARED 2026-08-26 by pass 4 (the rule
  reads the unemployment gap; the USA's suggestion left the floor; the chairs differentiate). **Gate 2
  — this item's appointment machinery — is B's only gate:** appointment is political-game material
  (candidates, a cadence, the reveal), so it ships with item 10, not before. Option C stands as the ruled
  present state (the player-set rate named as a deliberate choice in the slider's own text). ⚠ **Playtest
  pressure recorded (2026-08-26, Elias's Editor session): C's naming does not satisfy in play — the felt
  verdict was "still not independent."** B rises in priority when item 10 opens.
- **`stranded/politics-elections` STAYS AS-IS until item 10 is scheduled** (ruled 2026-08-11): pushed,
  safe off-machine; merging ~3,500 lines of unreviewed simulation code into `main` is what the branch
  exists to prevent; its remaining layout work is not extracted without a failing measurement. **Full
  contents, so nobody has to check it out** (commit `ca6c510`, 30 files):

  | Group | Files |
  |---|---|
  | **New data model** (6) | `Chamber`, `ElectoralFormula`, `ElectorateCohort`, `PoliticalParty`, `ThresholdRule`, `UnitedStatesSeed` |
  | **New simulation** (4) | `NationalVoteModel`, `SeatAllocation`, `UnitedStatesElectionCycle`, `UnitedStatesElections` |
  | **Modified, layout half now on `main`** (3) | `GameController`, `PoliSimWidgets`, `IconLibrary` |
  | **Modified, not extracted** (1) | `SimulationManager` |
  | **Python** (4) | `seat_allocation_check`, `usa_election_check`, `ledger_geometry_check`, `screenshot_edge_check` — the last two superseded on `main` (`ScreenEdgeCheck`; the 1440p capture ruling); Python is not installed here |
  | **Docs** (6) | `POLISIM_POLITICS_ELECTIONS_ROADMAP` (new), plus branch-side edits to `CLAUDE`, `CLAUDE_DESIGN_ASSET_REQUEST`, `MISSING_PREREQUISITES`, `POLISIM_MASTER_ROADMAP`, `POLISIM_SEED_DATA_MACRO_OVERHAUL` |
  | **Editor, since superseded on `main`** (6) | `CheckSuite`, `DeliveredAssetCheck`, `ImporterSettingsCheck`, `PartyMarkCoverageCheck`, `ScreenEdgeCheck`, `StatIconCoverageCheck`, `UiScreenshotCapture` — differ only because `main` moved on; **nothing on the branch is newer** |

- **A trade axis for the Trade bill's vote** (pass 6's deferred set): the direction reads the fiscal
  axis by Elias's ruling until real parties give trade its own.

## D1 — cabinet portraits: ✅ CLOSED 2026-08-27 (tombstone)

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

---

# E. Waiting on Claude Design

## 🟡 E2. `mark_party_us_lib` — delivered and imported 2026-08-17; the branch-side accounting is the residual (gate: item 10, 13 Sept 2026)

The sprite-side conditions are met: imported to `Emblems/` (meta from the MARK family, fresh GUID,
`ImporterSettingsCheck` green — the WoA classification read from pixels, not the label). ⚠ This entry's
close condition was "PartyMarkCoverageCheck reports it resolving at RGBA32" — and on MAIN that check
honestly reports **"PARTY SYSTEM NOT PRESENT on this branch... VERIFIED NOTHING"**: the party seeds live
on `stranded/politics-elections`, item-10-gated. The accounting half runs when the branch does —
orphan-by-sequencing, the same recorded status as the other four marks (§D0). Delivery story:
`COMPLETED.md` §24 (the §1G record) and CLAUDE.md's 2026-08-17 import entry.

**Riding the same gate: the R5 hex exchange.** Design's flag ("LP gold needs an ink-safe darkened
`DisplayColor` — pass it with Sweden's set") is GATED BY NAME on item 10 — no party seeds exist on main,
so no hexes exist to send. The exchange fires when the gate opens; Design is waiting on a calendar, not
on us. **Election night's legend will need one mark per seated party** — count unknown until the seeds
land, five on disk — named here so it does not arrive as a surprise batch the week the gate opens.

⚠ **Zone.Identifier check CLOSED (2026-08-26, Windows-side):** all five `mark_party_*.png` carry only the
`:$DATA` stream — no mark-of-the-web ADS exists on any of them. The outstanding item the request doc's
former §1F.2 carried is answered (`COMPLETED.md` §24): nothing to observe, nothing blocked.

## E3 — the rasterization diff, our half: MOVED TO THE ROADMAP (tombstone, 2026-08-27)

**Ruled by Elias 2026-08-27: this was never Design's to supply.** Design's half closed 2026-08-17 (they
re-rasterized the six per-state button PNGs fresh from SVG and pixel-diffed 6/6 identical — the
Progress2 manifest); our half needs a tooling pass that makes Unity's vectorgraphics
`RenderSpriteToTexture2D` path produce pixels under the batch harness, or a rasterizer installed on this
machine. Neither is a named external party, so under this register's own admission test it is startable
work — **`POLISIM_MASTER_ROADMAP.md` live item 7** carries it with `StripCutDiffCheck`'s finished compare
machinery and the 2026-08-26 attribution correction. A prerequisite filed under the wrong supplier is
one that lapses, because nobody on either side is waiting for it — the reason this tombstone exists.

## 🟡 E4. The `StatNodeId`/icon promotion for R4-1's two Society rows (youth unemployment, life expectancy)

Both rows exist as plain derived rows with no `StatNodeId` and no icon (`DrawDerivedStatRow` in
`GameController.cs`). Promotion is two enum members and two icon files; **the icon ask joins the next
Design asset batch** (`CLAUDE_DESIGN_ASSET_REQUEST.md` §4 names it as costed, not requested; D1's
batch landed 2026-08-27, so nothing queues ahead of it now — the promotion itself is the first step and
is ours). Waiting on Design once the members exist, so not startable as a request today.

---

# V. Waiting on Elias — a visual review (built, not seen; rule 15's third layer)

**Playtest 3 (2026-08-27, Elias's Editor session against the seventeen-item checklist): 12 of 17 pass;
ten surfaces cleared below to `COMPLETED.md` §34; three findings, in priority order, attached to the
seven that remain.** "Pinned on film" is containment evidence, not a sighting; each item here closes to
`COMPLETED.md` with the session named.

| surface | built | the finding it carries (playtest 3) |
|---|---|---|
| **The Compass — the Y axis** (Politics › Compass; the labels themselves passed) | `e25ae60` | **Finding 1, a real defect:** the compass "only appears to operate on the x-axis". Diagnose before touching: instrument `GetRegulationWelfareAxisValue` for all six countries — the raw values, then the plotted y positions — to separate a MODEL cause (the Y term blends sector regulation with implemented welfare generosity; if no seed implements welfare programs, half of it is constant) from a PLOT cause (auto-scale or clamp collapsing a real spread). Numbers before a fix. **DIAGNOSED 2026-08-27 — MODEL cause, reported, no fix taken:** all six seeds put every sector at regulation 50 and implement no welfare program, so Y = 25.000 for all six (`CompassAxisDiagnostic`; `COMPLETED.md` §34). Waiting on Elias's ruling on which fix. |
| **The eight new cabinet portraits** (Politics › Cabinet) | Progress5 2026-08-27 | **Finding 2:** they render too small to judge the fidelity question (the batch is 512×640 against the older sixteen's 256×256 — more detail than the display shows). Report the dimensions the candidate card and the roster frame actually draw at before changing any number — the row-pitch class cost several rounds by adjusting a number without knowing which one governed. The same-hand question stays open. **MEASURED 2026-08-27, nothing changed:** one sizing site (`DrawPersonPortrait`, `fontSize × 3.2`); the art draws at 41×54 px at 1600×900 and 62×80 px at 2560×1440 (§34). Waiting on Elias's ruling on the size. |
| **The law browser at 50** (Policy/Laws › Laws + the detail pane) | `0bb7ebc` | **Finding 3, the real work — one finding with the three trace surfaces:** "hard to tell due to clutter and poor placement". Instruction: remove unnecessary text and headers, as the Budget screen already does. Survey by category before cutting — (a) needed now, (b) better learned once, (c) restating an adjacent element — because B1's amber cue and B8's interrupt line live on screens in this group. **SURVEYED 2026-08-27, nothing cut:** every drawn element of the five surfaces classified (§34; `CLAUDE.md` "Playtest 3"). Waiting on Elias's cut list. |
| **The approval + confidence trace sections** (click a chip) | `092202c` | Finding 3 (above). ⚠ The panel ending at the tab's bottom under the host-height cap is recorded, not a finding. |
| **The fiscal trace section** (the Debt-to-GDP chip) | `7d2a22c` | Finding 3 (above). |
| **The primary-balance line** (Statistics › Domestic, the "Derived" box — NOT the Budget tab; host corrected 2026-08-27 from the checklist's wording, `DrawDerivedStatsRow` is called only from `DrawDomesticStatisticsContent`) | `e25ae60` | Finding 3 (above): the line itself passed; its placement is the clutter question — the fifth row of a box under six headline tiles, two screens away from the Budget balance it qualifies. |
| **Trade's pass-through stats line** (Statistics › International, under the world map — NOT Policy/Laws › Trade; host corrected 2026-08-27, `DrawTradeStatsContent` is called only from `DrawInternationalStatisticsContent`) | `4650a76` | Finding 3 (above): the line itself passed; its placement is the clutter question — the realized figure sits on the International page while its forecast twin ("prices +0.00 pp this year") sits on the Trade bill card. |

**Cleared 2026-08-27 (playtest 3, seen by Elias):** the Canvas country selector's set; Turn → Year; Budget's
dead nested scroll; Sweden's 24-line budget decomposition; the SWF emergency drawdown bill; option C's
deliberate-choice paragraph; pass 6's four Trade surfaces (the inert dial, the retaliation label, the
bill card's cost line, the stats line — the last placement-flagged under finding 3); the rejected-bill
seal on the Signing screen; Italy Debt Crisis as a playable scenario with the fiscal trace mid-run; Step
3's verdict screen with the Sustained streak line and the scenario entry on the selector. Records:
`COMPLETED.md` §34.

Not on this list because a session is on record: the Calendar Panel (playtest 2's verdict on it is why
request §8 exists), the Signing screen (playtest 1's seal/button finding), the folder tongues, save/load
layer 3, the portrait register (2026-08-26), and all eleven of the 2026-08-02 review items.

# P. Waiting on Elias — a playtest (the felt verdicts)

Three named items no measurement this side of a playtest can answer; each closes when a playtester says
so either way:

1. **Does decision density READ as closed?** (ruled its own item 2026-08-25). The measurable half is
   closed — choices 19 → 69 → 119, prompts unchanged by construction; whether a player FEELS the gap
   closed is a playtester's question, not a constant's. The felt-pacing question (R-S3e's residue,
   superseded by ruling C5) rides here too.
2. **Riksbank-B's felt verdict** — option C's naming did not satisfy in the 2026-08-26 Editor session
   ("still not independent"); the next play says whether it holds until item 10.
3. **The Trade bill's costs felt** (pass 6) — the bill card's cost line, the partner row's retaliation,
   the inflation year, and a hike that now fails at the seed composition.
