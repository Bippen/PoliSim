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
| §S — the send package | **Elias — one paste** (`SEND_PACKAGE_2026-08-28.md`: the note, the request doc through §1 — the v3.0 boards — and §E5, and the annex captures, each with its digest and its path) | the E2 convention: sending is Elias's |
| ~~§E6 — the v3.0 Phase A boards~~ **LANDED 2026-08-28** — boards 1m ("Screen 0 — The Desk, folded", 1280×720) and 1n ("the rail") on the live screens file, no gap costed | — | built the same day (v3.0 Phase B, `COMPLETED.md` §41); the row retires with the next re-derivation |
| §A — the coupling queue Q6–Q10; F2 | **Elias — a decision**, each at its own named trigger | no trigger has fired; nothing else waits on a ruling |
| §D — item 10, the political game, and everything riding it (**+ the political-model fact Phase 3 measured**) | **Sweden's vote, 13 Sept 2026, then Elias's pricing decision** | the one remaining spine item |
| §E2 — mark accounting + the R5 hexes | **item 10** | 13 Sept 2026 |
| §E4 — the icon promotion for R4-1's two Society rows | **Claude Design — the next batch** (nothing queues ahead of it since D1 landed 2026-08-27) | two `StatNodeId` members first — ours, before the ask |
| §E5 — the hatch tile's SVG source (the slider strip's half CLOSED 2026-08-28: source-less by Design's account, the legacy pill removed, the model states it) | **Claude Design — one re-cut**: the 2026-08-28 re-export has a 32 px period where the shipped PNG's is 16 (measured on the PNG; the phase is fine, the duty ≈8 px along x) — the ask, with the figures, is in the request doc §E5 | `StripCutDiffCheck` keeps the pair deferred by name with the measurement as its pointer (R-D3); structure 33.4 % after the re-export, against 1 % |
| §V — built, not seen (every surface on film, its capture named; `../PoliSim-captures/sv_index.html` is the one sitting) | **Elias — a visual review** | rule 3's third layer |
| §P — three felt verdicts, each a staged save (`playtest_1_trade_bill_costs`, `playtest_2_riksbank_rate_decision`, `playtest_3_dense_midgame`) | **Elias — load, play, judge** | no measurement can answer them |

---

# S. The send — one paste (`SEND_PACKAGE_2026-08-28.md`)

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

**Still unsent — Elias's, now one gesture (regenerated 2026-08-28 for UI v3.0 Phase A):**
`SEND_PACKAGE_2026-08-28.md` at the repo root names the artifacts — the courtesy note (unchanged from its
recorded hash), the request doc through §1 (the v3.0 boards, with Annex A the census and Annex B the
inventory) and §E5, and the Annex C captures (the landing screen in both shell states, the rail as built,
the ladder films) — each with its SHA-256 as on disk and its destination path; the glance after the paste
is the readback hash. The direction's own sequencing: the request-doc send was held until Phase A landed
so ONE send carries §E5 and the v3 ask. The note is a note, not an ask, and carries nothing Design is
waiting for. §D1 is no longer gated behind this entry — the verdict is in Design's project.

---

# A. Waiting on Elias — a decision (the coupling queue only)

**The coupling queue's remainder, Q6–Q10**, each ruled at its own named trigger (the Q1–Q10 queue is a
ruling, CLAUDE.md's Master Sequence II record); nothing is startable until a trigger fires and Elias
rules. **F2 — the rate-cap note** stands as a recorded property, not a task (CLAUDE.md's fiscal-arc
register). Q4 was RESOLVED by R-Q5d (2026-08-18, confirmed 2026-08-26) and is not in the queue.

**A1/A2/A3 — CLOSED, tombstone (2026-08-26).** The rating thrash (review cadence, not damping; closed in
full 2026-08-17), the SWF emergency drawdown (a standalone tier-3 bill, ruled AND built 2026-08-02,
`b1c077f`), cabinet appointments staying UNILATERAL — all resolved 2026-08-02; the reasoning migrated IN
FULL to `COMPLETED.md` §23 so none is reopened as an unanswered question later.

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

**A political-model fact measured by the omnibus pass, filed here because the re-seeding is what changes
it (2026-08-28, Phase 3, `11c28a2`):** under `PartyArchetypeData` the Progressive and Conservative seat
targets are identical at every approval level (base share 0.32 / 0.32, approval sensitivity 0.35 / 0.35),
so the expected expansionary alignment is −0.0015 × Nationalist seats — negative everywhere (−0.036 at
seed, −0.006 at approval 100, −0.09 at approval 30) — and **no expansionary bill passes on any drift path
except by ±1-seat jitter** (`MaxSeatJitter`). Every tax-raising, spending-raising and welfare-implementing
bill fails in the pre-item-10 game; every cut passes. It killed The Unequal Recovery (the transfer
programs are the only levers strong enough to move Gini, and all of them are expansionary); it is also the
standing state every playtest since step 4 has been played in. Not tuned around (R-K2); the scenario's
return trigger is the real parties.

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

## 🟡 E5. Two strip-cut findings from our half of the rasterization diff (2026-08-28) — Design's to re-cut or to explain

The diff closed on its own condition (the six per-state canvas buttons 6/6 — `COMPLETED.md` §36, roadmap
item 7) and the full sweep of 90 SVG/PNG pairs surfaced two pairs where the shipped PNG is not a cut of
its source, both viewed rather than inferred (`StripCutDiffCheck` with resvg 0.47.0, `stripcut_resvg2_*.log`;
the check's own renderings beside Design's in `%TEMP%\stripcut_fail_<name>.png`):

1. **`ui_hatch_draft.png`** (Chrome, 32×32): the source's `<pattern width="16" … patternTransform="rotate(45)">`
   gives stripes with a 16/cos45 = 22.6 px horizontal period; Design's PNG has a **16 px** horizontal
   period — the rotation was applied to the stripe content but not to the tiling. Mismatch 60%. Either
   the PNG is re-cut from the SVG, or the SVG is re-authored to what the PNG shows — Elias's call which
   side is the truth (the in-game asset is the PNG).
2. **`ui_slider_track.png`** (Chrome, 256×28): the source is a 24×24 pill with a vertical gradient; the
   PNG is a 256-wide strip. Not comparable as a rasterization of the source (resvg keeps the aspect and
   returns 28×28). Either the strip's real source is a different SVG that should be in `Source/`, or the
   24×24 pill was stretched — say which.

Not Design's: the nine Stats icons that sat 0.06–1.2 pp over the old flat 2% budget were antialiasing along stroke
silhouettes — the check now asserts two damage classes against their own bars (R-C2, `283e4ba`) and passes them;
the hatch pair is deferred by name until the answer lands (R-D3, `4df1dbc`). The three stamps differ only by font
rendering (`TEXT`, named).

## 🟡 E4. The `StatNodeId`/icon promotion for R4-1's two Society rows (youth unemployment, life expectancy)

Both rows exist as plain derived rows with no `StatNodeId` and no icon (`DrawDerivedStatRow` in
`GameController.cs`). Promotion is two enum members and two icon files; **the icon ask joins the next
Design asset batch** (`CLAUDE_DESIGN_ASSET_REQUEST.md` §4 names it as costed, not requested; D1's
batch landed 2026-08-27, so nothing queues ahead of it now — the promotion itself is the first step and
is ours). Waiting on Design once the members exist, so not startable as a request today.

## 🟡 E6. The UI v3.0 Phase A boards — "Screen 0, The Desk, folded" and "the rail" (gate: the §S paste, then Design)

The eighth request (`CLAUDE_DESIGN_ASSET_REQUEST.md` §1, 2026-08-28): two boards at **1280×720 first**,
drawn against three annexes we supply — the census of every text element on the landing screen with its
class (Annex A: (a) 44 · (b) 18 · (c) cut), the inventory of every instrument the game already draws with
its **measured** minimum legible size (Annex B, from the ladder films), and the captures (Annex C, in the
package). The direction's constraints travel with it: the text budget (captions at mono 9.5 and instrument
labels only), no new hues or fonts or Canvas, delivered sprites plus primitives with gaps costed as
follow-ups, the rail's required contents, the instant flip, the floor first; and the three deviation
conventions (neutral valence, no invented data, IMGUI adaptations declared). **What waits on it:** v3.0
Phase B — The Desk built against board one (`v3desk_*`), the rail re-skinned against board two, every (b)
in the census resolved as an instrument or dropped. Election night's fallback stands if the boards have
not landed by the 13 September gate: it builds in the OPEN state and moves to the stage later.

---

# V. Waiting on Elias — a visual review (built, not seen; rule 3's third layer)

**The review package of the omnibus pass and its continuation (2026-08-28) — the final review checklist.**
Everything visual the two passes shipped, each row ON FILM with its capture named — the omnibus closing
matrix `omni_final_<size>_<screen>.png` at 1280×720, 1600×900, 1920×1080 and 2560×1440 (USA); the
continuation's sets `cont_p1b_<size>_…` (the law browser) and `cont_p3b_<size>_…` (the seven film-gap
states, every size); the closing sanity sets `cont_final_1600_…` (USA) and `cont_final_swe_1600_…`
(Sweden); plus the per-country sets named per row; **and, from UI v3.0 Phase A (2026-08-28), the `v3a_<size>_…`
family at all four sizes — every screen in its default fold state plus the three fold pairs — and the
ladder films `v3a_ladder_<size>_ladder_<kind>`; from the stage-prep micro-pass (2026-08-28) the
`sp4_<size>_…` family and `sp4_ladder_<size>_ladder_<kind>`, the same sweep on the code after R-SP4 and
R-SP5; and from v3.0 Phase B (2026-08-28) the `v3desk_<size>_…` family — the sweep plus Screen 0's four
frames (`01c_desk`, `01d_desk_held`, `01e_desk_event`, `01f_desk_gameover`) at four sizes** — all under
`G:\UNITY\Projects\PoliSim-captures\`. No row is verified by code alone (R-C6 retired the two ⚠ rows). "Pinned on film" is containment evidence, not a
sighting; each item closes to `COMPLETED.md` with the session named. The three findings of playtest 3 are
closed by their rulings; their surfaces are re-listed here as built.

| surface | built | the capture | what to look for |
|---|---|---|---|
| **The Compass — the Y axis** (Politics › Compass) | `915c800` | `omni_final_*_07b_politics_compass` | six countries on both axes (raw Y 37.5 Poland … 57.3 France); the sourced spread, confirmed 2026-08-28 (seed §8, `[PROVISIONAL]` until the database session) |
| **The Economic Sectors tab at the sourced positions** (Policy/Laws › Economic Sectors) | `915c800` | `omni_final_*_06c_policylaws_sectors` (+`_rows`, `_deep`) | Regulation opens at the country's PMR level (USA 59; Energy 38, Telecoms 56, Retail 76), not 50 |
| **The Welfare tab at the sourced portfolio** (Budget › Welfare) | `915c800` | `omni_final_*_05c_budget_welfare`; Sweden `omni_p4_swe1600_05c_budget_welfare` | the USA opens with means-tested / housing / childcare implemented and no universal healthcare; Sweden with four |
| **The Policy Web's causal graph** (Policy/Laws › Policy Web) | `a267fd6` | `omni_final_*_06d_policylaws_policyweb` | the ring with its always-shown category headers; edge colours from the stat's own perspective |
| **The Policy Web with a node pinned — the derived / declared idiom, the stat chords** (R-C6) | `548a558` | `cont_p3b_*_06k_policylaws_policyweb_node_policy` (+`_rows`), `cont_p3b_*_06l_policylaws_policyweb_node_stat` (+`_rows`) | Income Tax pinned: its chords on the ring, then (scrolled) the readout below — each edge tagged DERIVED (a ledger term) or DECLARED; Approval pinned: the stat chords ("moved by … — ledger: …") |
| **The trace panel on Policy/Laws, three sections** (R-C6) | `548a558` | `cont_p3b_*_06h_policylaws_trace_approval`, `_06i_policylaws_trace_confidence`, `_06j_policylaws_trace_debt` | under the stat row of the Labor Market host: the approval ledger's rows (reversion toward 50, growth vs potential, the misery gaps with their sub-rows); the confidence book; the fiscal chain (primary balance, the reaction, interest at issuance, the maturity lag, erosion, the stock change) |
| **The four budget decompositions** (Budget › Spending, per country) | `6307dce` / `ad7b240` / `d33e1ae` / `e04f238` | `omni_p6_de1600_05b_budget_spending_rows`, `omni_p6_it1600_…`, `omni_p6_pl1600_…`, `omni_p6_fr1600_…` (+`_deep`) | the real areas at the game's G level; the largest line the remainder; the % of GDP column carries the method's distortion (Germany's Defense 5.1%) |
| **The RUNNING status plate and the disabled speed face** (§A.6, B5) | `f92e14f` → `adcb52e` | `omni_final_*_01b_running_strip`, `_90_interrupt_held` | the plate under the label, the lamp, the muted 1×/2×/3× faces while held |
| **The screen captions and the sub-tab icons** (§A.8, R-K6) | `c188b28`, `da6a684` | every `omni_final_*_0[2-7]*` | the caption right-aligned above the rule; icons on Statistics and Budget rows, none on the Policy/Laws row at 1600 (by width) |
| **The inactive tab-swatch tints; the urgency chip as a stamp** (§A.3, §A.11) | `adcb52e` | `omni_final_*_0[2-7]_*`; `_03_decisions` | the folder tongues' icon tint; HOLDS TIME / CAN WAIT rotated −2° |
| **The row family on the Fed tab, International, the Trade bill card** | `f145ba2` | `omni_final_*_07d_politics_federalreserve*`, `_02b_statistics_international*`, `_06e_policylaws_trade*` | rows, not sentence labels; the Trade card's three cost rows |
| **Board 1k — the calendar as one almanac sheet** | `3fa3eb2` | `omni_final_*_0[2-7]` (the left panel) | the diagonal strike, the ledger dot, the 4-dot underline, one sheet |
| **Board 1l — the graph weights** | `2b698b0` | `omni_final_*_02a_statistics_domestic_deep` | history 3px over the projection's 2px dashes |
| **The signing ceremony's entrance — §A.13 rows 4 and 6** (R-C6) | `14740e2` → `548a558` | `cont_p3b_*_89d_signing_entrance`, `cont_p3b_*_89e_signing_settled` | the document caught mid-rise with the SIGN button still invisible (row 4; row 6's first half), then settled with the button faded in (row 6's second half); the harness's own staged division on the paper, named as such |
| **The law browser at fifty — the one-line row** (R-C1) | `476c66c` → `a7d877d` | `cont_p1b_*_06f_policylaws_laws*` (+`_rows`, `_deep`) | one-line rows at board 1i's proportion (37 / 43 / 55 px pitch at 1280 / 1600 / 2560): 3 → 5, 5 → 8, 7 → 11 laws per viewport against the two-line row; the longest statute names shrink, never truncate; the detail pane's content sized to its viewport (`a331e82`) |
| **The Trade tab's Reset click, draft-only** (R-D2) | `4e44777` | `clear_p1c_*_06m_policylaws_trade_draft_moved`, `clear_p1c_*_06n_policylaws_trade_draft_reset` | the first partner's dial drafted +10 (amber, hatched) beside its 3.00 % standing override, then the draft back at 3.00 % with the override still active and "Reset draft" still offered — nothing live moved |
| **The re-sourced homeownership on the housing row** (R-C5) | `e08c8c0` | `cont_final_swe_1600_02a_statistics_domestic_deep` (Sweden); `cont_final_1600_02a_statistics_domestic_deep` (the USA, unchanged at 65.3) | Sweden's Homeownership row reads 58.2 where the estimate read 62.1; Italy 75.2, Poland 84.7, France 58.6 in their own runs — a figure, not a layout |
| **The v3.0 shell — the landing screen FOLDED, the rail** (V3-R2) | `8e162b1` | `v3a_*_02a_statistics_domestic` (the default, folded), `v3a_*_02a_statistics_domestic_open` (the same screen unfolded — the pair) | the rail on its paper sheet: six icon cells (Statistics in its area ink behind a spine, the others in the tab-swatch tint), the calendar chip (month + day), the HELD lamp with its glow, the "›" toggle; the folded banner above the sheet carrying the reasons; the stage taking the width — the cell is 39 px at 1280, 46 at the 1600 view, 55 at 1920, 64 at 2560 |
| **The v3.0 shell — a ledger screen OPEN by default, folded on request** (V3-R2) | `8e162b1` | `v3a_*_07a_politics_parliament` (the default, open — the OPEN strip's "‹" toggle at its right end beside the status line), `v3a_*_07a_politics_parliament_folded` (the pair) | the OPEN frame unchanged but for the toggle; folded, the hemicycle with the whole width |
| **The v3.0 shell — Budget locked FOLDED** (R-A1, the one lock) | `8e162b1` | `v3a_*_05b_budget_spending` (+`_rows`, `_deep`); `v3a_*_91_interrupt_held_budget` | the ledger beside the rail instead of the bare desk; the toggle on its disabled face (B5); the HELD banner above the sheet on 91; at 1280 the ledger's names on two lines and "not implemented" in a column that asks what it holds (instance #15) |
| **The v3.0 shell — the Canvas class is fold-invariant** (V3-R4) | `8e162b1` | `v3a_*_89e_signing_settled`, `v3a_*_89e_signing_settled_open` | identical by construction (a live Canvas suppresses the IMGUI frame); on film so the claim is film, not prose |
| **The stat tile's value under the mouse** (found by the v3a film) | `8e162b1` | `v3a_*_02a_statistics_domestic` (the Credit Rating tile) | "AAA" in TextPrimary whatever the cursor does — the skin's pale hover ink no longer reaches a figure |
| **The instrument ladder** (Phase 3's measurement, not a screen) | `5443342` | `v3a_ladder_1280_ladder_*`, `v3a_ladder_1920_ladder_*` (twenty kinds each) | each instrument at a descending run of sizes with its size captioned; the breaks Annex B states are read from these — the film is the evidence, the table the reading |
| **The compass on its honest footprint** (R-SP4, the stage-prep micro-pass) | `373ea07` | `sp4_*_07b_politics_compass` (+`_rows`, `_deep`); `sp4_ladder_*_ladder_compass` | the plot square and its two range captions on ONE plate, the captions inside the declared rect (containment-asserted, silent at four sizes); on the ladder each rung captions the footprint it was given — the captions no longer stack at the sheet's corner |
| **The map's names on their ladder** (R-SP5) | `c9c3c05` | `sp4_*_02b_statistics_international`; `sp4_ladder_*_ladder_map` | on the screens every name at its first rung (the harness measured the smallest gap per size against the 4 px floor — the log lines are quoted in `COMPLETED.md` §40); on the ladder film each rung's caption carries the rung reached (1 full name, 3 ISO code, 4 shrunk) and the gap measured — the small rungs are where the ladder works |
| **Screen 0 — The Desk** (v3.0 Phase B, board 1m) | §41 | `v3desk_*_01c_desk` (RUNNING, turn 0: the lamp green, the cluster live, the ledger with no period yet); `v3desk_*_01d_desk_held` (the warmed-up game: HELD above the masthead, the lamp amber, the faces disabled, the ten-row ledger, the sparklines) | the masthead, the map plate, the approval ledger, the compass, the effects card, the calendar sheet, the chip strip — each at the board's placement scaled to the size; the rail without a spine (D2); the 1280 film is the board's own frame |
| **The Desk's conditionals** (C1–C5 on the stage) | §41 | `v3desk_*_01e_desk_event` (the card filled with the pool's own "Recession in a Trading Partner": the BREAKING chip, the name, the description, the three bars); `v3desk_*_01f_desk_gameover` (the stamp over the dimmed stage, the election-loss reason as the game prints it) | staged by the harness through the game's own event pool and the game's own reason string — nothing invented for the film — and restored after each frame |
| **The rail re-skinned** (board 1n) | §41 | every `v3desk_*` document capture (the active cell: the 12 % wash and the 3 px spine; the chip's rule); `v3desk_*_01c_desk` (no spine on Screen 0) | the derivation untouched; the air and the active convention as the board draws them |
| **The eight new cabinet portraits at 5.5×** (the roster beside the sixteen squares) | `4e5adbf` | `omni_final_*_07c_politics_cabinet*` | still Elias's eyes (carried from playtest 3) |

**Cleared 2026-08-27 (playtest 3, seen by Elias):** the Canvas country selector's set; Turn → Year; Budget's
dead nested scroll; Sweden's 24-line budget decomposition; the SWF emergency drawdown bill; option C's
deliberate-choice paragraph; pass 6's four Trade surfaces; the rejected-bill seal on the Signing screen;
Italy Debt Crisis as a playable scenario with the fiscal trace mid-run; Step 3's verdict screen with the
Sustained streak line and the scenario entry on the selector. Records: `COMPLETED.md` §34.

Not on this list because a session is on record: the Calendar Panel of playtest 2 (superseded by board
1k above), the Signing screen (playtest 1's seal/button finding), the folder tongues, save/load layer 3,
the portrait register (2026-08-26), and all eleven of the 2026-08-02 review items.

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

**Load, play, judge (R-D4, 2026-08-28):** each verdict has a staged save in the game's own saves folder
(`…\saves\`, listed by the Saves menu) — `playtest_1_trade_bill_costs` (USA; open Policy/Laws › Trade:
a partner override drafted, the bill card's cost rows on screen — verdict 3), `playtest_2_riksbank_rate_decision`
(Sweden; a rate decision drafted on the Riksbank tab — option C's naming is the verdict; no appointment can
be pending on `main`, Riksbank-B's machinery ships with item 10 — verdict 2),
`playtest_3_dense_midgame` (USA; the budget-process pause, pending cabinet decisions and a meeting, one
bill of every type, twelve laws in force — verdict 1). Each save's proof capture at 1600: `clear_p5_usa_p1_trade_bill_costs`, `clear_p5_sweden_p2_riksbank_rate_decision`, `clear_p5_usa_p3_dense_midgame`.

**Context for every verdict on this list (R-C7, 2026-08-28):** every felt verdict to date was played in the
no-expansionary-passage regime §D0 records — no tax-raising, spending-raising or welfare-implementing bill
passes except by ±1-seat jitter until item 10's real parties re-seed the house. The verdicts are read with
that context when item 10 changes it; none is re-opened by it.
