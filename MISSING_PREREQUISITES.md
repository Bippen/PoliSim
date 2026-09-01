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
| §S — the send package | ✅ **SENT, AND ANSWERED IN FULL — verified 2026-09-01, not assumed.** ⚠ **The repo was wrong about this for a day.** C-F1 recorded *"the honest reading is that the paste was never made"*; that was true of the **D7-era** package (`85690abf…`, which is still absent from `uploads/`) and **false of D9**. `uploads/CLAUDE_DESIGN_ASSET_REQUEST-347e3be8.md` exists in the Design project and its **readback hashes to `347e3be8…` at 77 510 bytes — the package's own glance, passed, both artifacts** (`…BOARD_1I_NOTE-948fd2a6.md` likewise present at its digest). ⚠ **AND ALL ELEVEN D9 ROWS ARE ANSWERED** on `PoliSim v2 Screens.dc.html` — board 2b (row 1), board 3a (rows 2–5, the mark VOCABULARY plus three ink rulings), and the answers card (rows 6–11). ⚠ **One correction to the package's own instructions**: it says the digest is *"as on disk, CRLF"* and warns that *"an LF-normalized readback hashes differently"* — the on-disk file is **LF**, so the published digest IS the LF digest and that warning would have made a CORRECT readback look wrong. Fixed in the regenerated package. What remains is a **RETURN** package, not a send: two columns Design asked for (row 6) and one icon crop (row 9) | Elias's word on the seven marks (row 2's *"say GO"*); the two data returns are CODE's |
| §A — the coupling queue Q6–Q10; F2 | **Elias — a decision**, each at its own named trigger | no trigger has fired (register rows T-7 / T-1…T-6) |
| §D0 — item 10 | **nobody — the core SHIPPED** at `a289e1e`, 2026-08-30 | what remains is three named rows: K-1 the seed refresh (CALENDAR, 13 Sept), S-1 the unmoving electorate, S-2 Germany's threshold cliff |
| §E2 — mark accounting + the R5 hexes | **nobody on our side — DONE** (53 seeded, 1 resolving, 52 gaps, 0 errors; Sweden's eight inks sourced) | the residual is Design's, as rows D-8.1 and D-8.2 |
| §E4 — the icon promotion for R4-1's two Society rows | **Claude Design — the next batch** (nothing queues ahead of it since D1 landed 2026-08-27) | the two `StatNodeId` members are ours and land under **R-CL4** at C-F1, with a missing icon reported as a GAP rather than a check failure |
| §V — built, not seen (every surface on film, its capture named; `../PoliSim-captures/sv_index.html` is the one sitting) | **Elias — a visual review**, 52 rows | rule 3's third layer (register row E-2) |
| §P — three felt verdicts, each a staged save (`playtest_1_trade_bill_costs`, `playtest_2_riksbank_rate_decision`, `playtest_3_dense_midgame`) — **and, from 2026-08-29, Playtest 1's eleven findings** | **Elias — load, play, judge** (verdicts 1 and 3; verdict 2 became P-D1 and is discharged by C-C7). The findings' own queue is `POLISIM_BACKLOG.md` §1 Track C, not this file | no measurement can answer a verdict |

⚠ **Retired at this re-derivation (2026-08-31, C-0.2), both having said "retires with the next
re-derivation" since 2026-08-28:** ~~§E5, the hatch tile's SVG source~~ — CLOSED end-to-end, both sides,
the pair ruled "diagonal-tile, viewed not counted" and executed in `StripCutDiffCheck.ViewedNotCountedPairs`
(`COMPLETED.md` §46); ~~§E6, the v3.0 Phase A boards~~ — LANDED 2026-08-28, boards 1m and 1n on the live
screens file with no gap costed (`COMPLETED.md` §41). Neither is a gesture Elias still owes.

---

# S. The send — one paste (`SEND_PACKAGE.md`, regenerated 2026-08-31 at C-F1)

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
`SEND_PACKAGE.md` at the repo root names the artifacts — the courtesy note (unchanged from its
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

## 🟢 D0. Item 10 — REALISTIC POLITICS AND ELECTIONS: the core SHIPPED (re-derived 2026-08-31, C-0.2)

⚠ **This entry described item 10 as the unbuilt spine and was the anchor four other entries rode. It is
false at HEAD and is corrected here, not edited forward; its full former text is preserved at
`COMPLETED.md` §85.** Four of its claims went untrue on 2026-08-30: that the stranded branch is
*"preserved UNINSPECTED"*; that *"main's documents describe the four-archetype system as current because
it IS current"*; that the five `mark_party_*` sprites are *"drawn by NOTHING on `main`"*; and that
`PartyMarkCoverageCheck` reports *"PARTY SYSTEM NOT PRESENT"*.

**What shipped (W-G1, `a289e1e`, 2026-08-30; `COMPLETED.md` §79).** `PartyArchetype` retired for **53
real parties** across six real chambers (Sweden 349, Germany 630, France 577, Italy 400, Poland 460, USA
435), each carrying the position CHES 2024 (GPS 2019 for the USA) publishes and the seats its own
country's most recent election gave it. `ParliamentConstants.TotalSeats = 200` is gone. The collision map
executed in full: seat drift retired with it (**a parliament's composition does not drift week by week
with the government's approval — it changes at an election**), `GetSeatWeightedAlignment` re-expressed
over the measured chamber, `PublicationSystem` kept as the polling substrate, the renderers re-keyed.
`ElectionRecord` is persisted on `Country` inside `World` and `SaveVersion` bumped 1 → 2. **Two of six
countries hold a real election and the other four state why not.** Ruling R3's verification obligation is
DISCHARGED (§E2 above). The R-N2 invariant retired in the same commit, which is its own revert handle.

**What remains of item 10, exactly.** Three things, and each has exactly one row in
`POLISIM_BACKLOG.md` — this entry queues none of them:

- **The seed refresh from Sweden's real result** — row **K-1**, owner CALENDAR, 13 September 2026.
  Scheduled, not blocked.
- **The electorate does not move with the simulation** — row **S-1**. §8 couples it to the economy and
  nothing does that yet, so a second election in one game returns the first's result. Named rather than
  papered over with a jitter that would look like change without being it.
- **Germany's threshold cliff** — row **S-2**. BSW missed 5 % by 0.02 pp; a model with ~1.5 pp of error
  lands on the wrong side and ninety seats move. Reported, never tuned.

**The four riders this entry carried, disposed:**

- **Step 6, story mode — RE-GATED 2026-08-31 (C-B5). The gate is scoped here; the work is not.**
  Both of its old gates have fired: "item 10 shipped" happened at `a289e1e`, and the player-party
  question was ruled R-CL1. ⚠ **What it now waits on is not a ruling but a BUILD it does not own:**
  authored multi-beat arcs with memory need a protagonist whose party identity persists across an
  election, and that is register row **C-R2** (the party choice, persisted as world state) plus
  **C-D4** (§38's cross-election carry-over — reputation and organisational strength that survive a
  chamber change). Until those land, an arc can remember what the *government* did but not what the
  *party* is, which is the half story mode is actually for. **Neither is scoped here** — this entry
  records the gate and nothing else, per the item's own instruction to scope the gate rather than
  the work.
- **Riksbank-B** — its only gate was "the appointment machinery ships with item 10". ⚠ **Merged, not
  inherited:** Playtest-1's finding 7 (P-D1) specifies the same subject — declared reaction functions plus
  appointment influence **is** Riksbank-B — so the two are ONE item at **C-C7**, carrying the felt verdict
  *"still not independent"* (2026-08-26) with it. Gate 1 (the output-gap distortion) was cleared
  2026-08-26 by pass 4.
- **`stranded/politics-elections`** — inspected once and **disposed at C-0.3**; the ref is kept, the
  obligation retired. Its C# is superseded by `Assets/Scripts/Elections/` and W-G1; the four pieces of its
  roadmap doc that were NOT superseded are migrated to `COMPLETED.md` as history.
- **A trade axis for the Trade bill's vote** — the trigger ("where real parties land") has fired.
  **Ruled 2026-08-30 as R-CL2**: `eu_position` stands in as the openness axis, tagged as the
  approximation it is. Executed at **C-B3**.

**A political-model fact this entry recorded, now HISTORICAL and worth keeping for its lesson
(2026-08-28, Phase 3, `11c28a2`).** Under `PartyArchetypeData` the Progressive and Conservative seat
targets were identical at every approval level (base share 0.32 / 0.32, sensitivity 0.35 / 0.35), so the
expected expansionary alignment was −0.0015 × Nationalist seats — negative everywhere — and **no
expansionary bill passed on any drift path except by ±1-seat jitter**. Every tax-raising, spending-raising
and welfare-implementing bill failed; every cut passed. It killed The Unequal Recovery, and it is the
state every playtest through 2026-08-30 was played in. ⚠ **It was not tuned around (R-K2) and it did not
need to be: the archetypes and the drift are gone.** The measurement now runs over the real chamber's
published `lrecon`, and re-measuring it against the seeded chambers is what the scenario's return trigger
always was.

---

# E. Waiting on Claude Design

## 🟢 E2. The mark accounting — GATE FIRED, the accounting DONE, the residual is Design's (re-derived 2026-08-31, C-0.2)

⚠ **This entry's three load-bearing claims were all false at HEAD and are corrected here rather than
edited forward.** It said the check reports *"PARTY SYSTEM NOT PRESENT… VERIFIED NOTHING"*; that **no
party seeds exist on main**; and that the mark count was **unknown until the seeds land**. W-G1
(`a289e1e`, 2026-08-30) made all three untrue on the same day.

**What is true.** The sprite-side conditions were always met (imported to `Emblems/`, meta from the MARK
family, fresh GUID, `ImporterSettingsCheck` green — the WoA classification read from pixels, not the
label; `COMPLETED.md` §24 and CLAUDE.md's 2026-08-17 import entry). The accounting half now runs for
real: **`PartyMarkCoverageCheck` reports 53 seeded parties, 1 with a resolving mark, 52 without, 0
errors.** ⚠ `MarkName` is deliberately **not** derived from the abbreviation — a derived name would claim
a mark for all 53, and the check treats claimed-but-unresolvable as an ERROR rather than a gap. That is
ruling R3's verification obligation (*the reflection was to be verified then, not trusted now*)
**DISCHARGED**.

**The R5 hex exchange, likewise ungated.** Sweden's eight parties carry Valmyndigheten's published
`fargkod` through `PoliSimTheme` at the desk's own saturation and value; the other five countries have no
ink and `HasPartyInk` returns false, because picking 30 colours by eye for real organisations would be
invention. So the exchange is **Sweden's eight hexes, plus 45 parties named as uncoloured** — not a gate.

**Nothing here is waiting on us any more.** The residual is two rows of the Design ask (D8-1, the 52
undrawn marks; D8-2, the colour ruling for five countries) and it lives, once, as rows **D-8.1** and
**D-8.2** in `POLISIM_BACKLOG.md` §3. This section retires with the next re-derivation.

⚠ **Zone.Identifier check CLOSED (2026-08-26, Windows-side):** all five `mark_party_*.png` carry only the
`:$DATA` stream — no mark-of-the-web ADS exists on any of them. The outstanding item the request doc's
former §1F.2 carried is answered (`COMPLETED.md` §24): nothing to observe, nothing blocked.

## 🟢 E5. CLOSED end-to-end 2026-08-28, both sides — Design's half (the slider strip sourceless-by-design; the hatch cut three times) and Elias's bar ruling: "diagonal-tile, viewed not counted" (the row retires with the next re-derivation)

Both findings of our half of the rasterization diff are settled. (1) **`ui_hatch_draft.png`** (Chrome,
32×32): Design cut the pair three times (60 % → 33.4 % → **7.42 % structure**; re-cut #3 to our own
measurement — 16 px period, 8 px duty, phase on x+y=16k — imported `1d9926d`), and the residual is the
two rasterizers' coverage of a 45° edge on a 32 px tile, not a cut error (64 of the 76 mismatched px
straddle alpha 128; 12 solid-vs-void = 1.17 %). **Elias ruled the same night: "diagonal-tile, viewed
not counted" — the three text stamps' treatment.** Executed: the entry moved from
`StripCutDiffCheck.DeferredPairs` to `ViewedNotCountedPairs` with the measurement on record in the
check's own table; the deferral retired (R-D3's mechanism stands, empty); the classifier untouched,
exactly as the ruling's own condition required. The suite reads green with **4 viewed-not-counted**
(the three text stamps + this pair) **and zero deferred**. The pair's eye-diff is a §V row.
(2) **`ui_slider_track.png`**: authored raster with no SVG source, per Design's own answer —
`SourcelessByDesign` states it by name and a source re-appearing under the name is a FAIL.
Records: `COMPLETED.md` §46; the ask's history in `CLAUDE_DESIGN_ASSET_REQUEST.md` §E5.

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

> ⚠ **THE CANVAS-CAPTURE AUDIT, 2026-08-31 (S-20). READ THIS BEFORE THE SITTING.**
>
> C-D5 found that **every `-shotelectionnight` film ever taken photographed the DESK** under board 1h's
> name — an overlay Canvas draws before IMGUI, so the desk painted over it, at *8 captured, 0 failed, 0
> text overflows, 0 containment escapes, exit 0*. **Nothing in the film bar checked that the screen under
> test was the screen on screen.**
>
> **Every Canvas surface in this project was then re-filmed and checked one at a time:**
>
> | surface | verdict |
> |---|---|
> | **the country selector** (`01_country_selector`) | ✅ **confirmed** — its films always showed the selector |
> | **the signing ceremony** (`89d_signing_entrance`, `89e_signing_settled`) | ✅ **confirmed** — this board is entered through the game's own takeover, so IMGUI was already suppressed |
> | **election night** (`e6_election_night_*`) | ⚠ **VOID and RE-FILMED.** The only board the *harness* built by hand, with no takeover to put the desk away. Its row below is corrected |
>
> **A capture-identity trap is now armed** (`CaptureIdentity`): whichever surface owns the frame stamps a
> token in the corner, and the driver reads it back out of the PNG it just wrote — a capture that claims
> one screen and shows another **fails loudly**. Proven both ways: **81 of 81 captures pass** with the fix,
> and re-introducing the defect makes every election-night frame fail by name.
>
> **Nothing else in this section is affected** — every other row rests on IMGUI captures, which were never
> exposed to this.

**The review package of the omnibus pass and its continuation (2026-08-28) — the final review checklist.**
Everything visual the two passes shipped, each row ON FILM with its capture named — the omnibus closing
matrix `omni_final_<size>_<screen>.png` at 1280×720, 1600×900, 1920×1080 and 2560×1440 (USA); the
continuation's sets `cont_p1b_<size>_…` (the law browser) and `cont_p3b_<size>_…` (the seven film-gap
states, every size); the closing sanity sets `cont_final_1600_…` (USA) and `cont_final_swe_1600_…`
(Sweden); plus the per-country sets named per row; **and, from UI v3.0 Phase A (2026-08-28), the `v3a_<size>_…`
family at all four sizes — every screen in its default fold state plus the three fold pairs — and the
ladder films `v3a_ladder_<size>_ladder_<kind>`; from the stage-prep micro-pass (2026-08-28) the
`sp4_<size>_…` family and `sp4_ladder_<size>_ladder_<kind>`, the same sweep on the code after R-SP4 and
R-SP5; from v3.0 Phase B (2026-08-28) the `v3desk_<size>_…` family — the sweep plus Screen 0's four
frames (`01c_desk`, `01d_desk_held`, `01e_desk_event`, `01f_desk_gameover`) at four sizes; and from
v3.0 Phase C (2026-08-28) the `v3c_<size>_…` family at 1280 and 2560, the sweep on the ruled fold
defaults (Statistics › Domestic OPEN, its `_folded` pair beside it); from v3.1 Phase A (2026-08-28) the
`v31_<size>_…` family — ONE FRAME at four sizes; and from v3.1 Phase B (2026-08-28) the `v31bf_<size>_…`
family at four sizes — the five boards built and the OPEN state deleted — with its working sets
`v31b_<size>_…` (the boards before the deletion), `v31b_swe2_1280_…` (Sweden at Year 0, the Desk's own frame)
and the single-size probes `v31b_probe`, `v31b_desk`, `v31b_desk2`, `v31b_stats`, `v31b7`** — all under
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
| **The RUNNING status plate and the disabled speed face** (§A.6, B5; the plate's own film is the `omni_final` set — from `v3desk_*` on, `01b_running_strip` shows Screen 0's RUNNING masthead instead, see the Desk row's attribution note) | `f92e14f` → `adcb52e` | `omni_final_*_01b_running_strip`, `_90_interrupt_held` | the plate under the label, the lamp, the muted 1×/2×/3× faces while held |
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
| **Screen 0 — The Desk** (v3.0 Phase B, board 1m) | §41 | `v3desk_*_01c_desk` (RUNNING, turn 0: the lamp green, the cluster live, the ledger with no period yet); `v3desk_*_01d_desk_held` (the warmed-up game: HELD above the masthead, the lamp amber, the faces disabled, the ten-row ledger, the sparklines) | the masthead, the map plate, the approval ledger, the compass, the effects card, the calendar sheet, the chip strip — each at the board's placement scaled to the size; the rail without a spine (D2); the 1280 film is the board's own frame. *Attribution note (consolidation rider, 2026-08-28): since Phase B the surface beneath the selector's scrim is Screen 0, so the frames `01a_selector_yielding` and `01b_running_strip` in every family from `v3desk_*` on carry the DESK's guard results — an overflow or escape read under those names belongs to the Desk on first reading, not to the selector or the strip.* |
| **The Desk's conditionals** (C1–C5 on the stage) | §41 | `v3desk_*_01e_desk_event` (the card filled with the pool's own "Recession in a Trading Partner": the BREAKING chip, the name, the description, the three bars); `v3desk_*_01f_desk_gameover` (the stamp over the dimmed stage, the election-loss reason as the game prints it) | staged by the harness through the game's own event pool and the game's own reason string — nothing invented for the film — and restored after each frame |
| **The rail re-skinned** (board 1n) | §41 | every `v3desk_*` document capture (the active cell: the 12 % wash and the 3 px spine; the chip's rule); `v3desk_*_01c_desk` (no spine on Screen 0) | the derivation untouched; the air and the active convention as the board draws them |
| **The first live sitting, finding 1 — the Desk's density** (Elias, 2026-08-28; the sitting's first output) | `999e47e` | `sitting_1_desk_density.png` (642×450 — Elias's screenshot, read back from Design's `uploads/` where he pasted it, saved beside the captures; SHA-256 `c6153449…`) | Elias, verbatim in substance: *too much dead paper, text too small because spacing eats the room.* Part of it is Year-0 empty states never designed (the ledger without a period, the effects card at all-zero, the sparklines without history) — board 1m-r2 (D3) answers it; the measured paddings and dead-space figures are the request's Annex C, the empty states flagged there as "empty-state, not spacing" |
| **The first live sitting, finding 2 — the rail's icons and small-size readability** (Elias, 2026-08-28) | `999e47e` | `sitting_2_rail_icons.png` (644×270 — Elias's screenshot, read back from Design's `uploads/`; SHA-256 `aab13c05…`) | Elias, verbatim in substance: the rail's icons *not readable, not intuitive enough* at the real cells (39/46/55/64 px); *readability suffers at small sizes* (the faint inks). Design's 1n-r2 (D2) and the contrast pass (D6) answer them; Annex B (each icon at each cell) and Annex F (the ink pairs) carry the measurements; the structural home cell ships in v3.1 Phase A (R-E2) because discoverability was the sharpest complaint |
| **ONE FRAME — every document in the Desk's frame** (v3.1 R-E1, the OPEN state retired on the duty audit) | §44 | every `v31_<size>_0[2-7]*` capture (the rail and one full-bleed sheet on Statistics, Decisions, Demographics, Budget, Policy/Laws, Politics); `v31_*_01f_desk_gameover` and any document at game over (the banner's GAME OVER line) | no chrome column, no tongues, anywhere; the rail's bottom cell is the PAUSE/RUN chip (R-E1a — disabled while HELD); the folded banner carries the game-over reason on every screen (#8); the R-PC2 pair captures no longer exist — the `_folded`/`_open` suffixes are history |
| **HOME on the rail** (v3.1 R-E2, the structural interim) | §44 | the rail on every `v31_*` capture: the flag first, a rule beneath, then the six icons; on `01c_desk` / `01d_desk_held` the home cell active in brass | the flag at 24×16 at the 39 cell, growing with the cell; Design's 1n-r2 re-skins the face |
| **The rail captioned** (board 1n-r2, v3.1 Phase B) | §45 | the rail on every `v31bf_<size>_*` capture: DESK · STATS · DOCKET · PEOPLE · BUDGET · LAWS · POLITICS under bare glyphs, the active cell's caption in the area ink and bold, the home cell brass-washed on `01c_desk`; the 1280 set is where the captions sit at the guard's 8 px floor | the width unchanged; the glyphs the delivered set (a redraw refused as costed); the bold POLITICS yields its weight at 1280 — is the caption readable at 8 px, and does the rail now read without learning? |
| **The Desk revised, with its Year-0 empty states** (board 1m-r2) | §45 | `v31bf_<size>_01c_desk` (Year 0: the nine em-dash ledger rows under the FIRST ATTRIBUTION chip, the effects card's bare tracks under the dashed no-draft caption, the dotted baselines with today's dot, the reservation drawn); `v31b_swe2_1280_01c_desk` (Sweden at Year 0 — the board's own frame, the calendar's thirteen January markers); `v31bf_<size>_01d_desk_held` / `01e_desk_event` / `01f_desk_gameover` (the warmed-up states on the new placements) | the 1156×680 inner area, 440/250/440, the strip integrated; the shadow now outside the sheet — the sheet's paper flush with the frame's 15/8 margins; the dead-space share by Annex C's method reads 43.9 % at 1280 (the board expected ≈ 30 %) |
| **Statistics as instruments** (board 2a) | §45 | `v31bf_<size>_02a_statistics_domestic` (the plates, the one-axis fiscal bars, the sector distribution bar); `…_02a_statistics_domestic_rows` / `_deep` (the graphs in three columns, the Society rows with their gauges and row-end sparklines, the published key and bulletin); `v31bf_<size>_02b_statistics_international` (E24 gone) | the axis printed; the USA's overburden row drawn as absent by ruling; the sub-tabs keep their delivered faces (the board drew chips — a build call) |
| **D4's density and D6's inks on every sheet** (the token table and the contrast pass; the paper sprite's shadow moved outside the box rect) | §45 | every `v31bf_<size>_*` capture beside its `v31_<size>_*` twin: the 1.2 % margins, the 10/10/8/10 paper padding counted from the paper's visible edge, the 17 px body at 1280, the darker muted/caution/good/neutral inks, the Global and Political headers, the selected chip's dark caption on brass | the re-measured dead-space shares (request doc Annex C) rose on the content-short screens — Decisions, Demographics, the short Politics screens — and fell only where the screen was re-composed; the density direction's next move is theirs, not the tokens' |
| **Statistics › Domestic OPEN by default** (R-PC2, v3.0 Phase C — the fold-default table ruled; **superseded by ONE FRAME the same evening**) | §42 | `v3c_1280_02a_statistics_domestic`, `v3c_2560_02a_statistics_domestic` (the default, OPEN: the chrome column and the tongues back beside the ledger); `v3c_<size>_02a_statistics_domestic_folded` (the pair) | the screen opens in the state it had before Phase A; the guards and the edge check ran in both states at both sizes; the only FOLDED defaults are the two locked screens (the Desk, Budget) |
| **The eight new cabinet portraits at 5.5×** (the roster beside the sixteen squares) | `4e5adbf` | `omni_final_*_07c_politics_cabinet*` | still Elias's eyes (carried from playtest 3) |
| **The hatch pair's eye-diff — "diagonal-tile, viewed not counted"** (the 2026-08-28 ruling; the residual viewed, not inferred) | §46 | `%TEMP%\stripcut_fail_ui_hatch_draft.png` (our resvg rendering, rewritten by every `StripCutDiffCheck` run) beside `Assets/Resources/Art/UI/Chrome/ui_hatch_draft.png` (Design's cut, the in-game asset) | the 32 px diagonal tile with period, phase and duty agreeing; the only difference should be coverage along the 45° stripe edges (the shipped PNG's edge pixels at alpha 160, resvg's at 96–152) — a visible SHAPE difference by eye would contradict the ruling's premise and sends the pair back to FAIL |
| **The first live sitting, finding 3 — the Policy Web** (Elias, 2026-08-28; the sitting's third output — no screenshot, the words are the finding) | the finding's record; the interim build is the Policy Web micro-pass (R-W1) | `v31bf_*_06d_policylaws_policyweb` (the state the finding was about); the micro-pass's own family films the full-sheet interim | Elias, verbatim in substance: *the Policy Web should be bigger, more understandable, and use the page's dead space.* Split per the R-E2 precedent: SCALE is structural and ships the same day (R-W1 — the web takes the full sheet, same nodes, same edges, same clicked-node idiom); COMPREHENSION is composition and goes to Design as ask D7 (board 2b, "the Policy Web, drawn to be read"), drawn against Annex G's measurements. The web is the first screen whose dead space the paradox's reclaim CAN reach — it holds one scalable instrument, so the room goes to the ring, not to more paper. **Continued by Playtest 1 (2026-08-29), Track P-F:** the structural half of comprehension (P-F1 — focus mode, arrowheads, weight-scaled thickness from the coupling table) ships inside R-W2's fence; board 2b's status is P-F2's report; the paste stays Elias's |
| **Playtest 1, finding 1 — meta-text on player surfaces** (Elias, 2026-08-29, the Sweden session; no screenshot — the words are the finding) | **P-A1, `COMPLETED.md` §61** (2026-08-29) | `pa_sweep_1280_06f_policylaws_laws*` and `pa_sweep_2560_06f_…` (the laws tab — a citation now reads *The US First Step Act (2018)…*, no status prefix); `pa_campaign_<w>_e1_campaign_hq_*`, `_e3_campaign_action_*`, `_e4_campaign_polling_*` at 1280 / 1600 / 1920 / 2560 (the captions in the player's language, no section signs, no tags); `pa_sweep_<w>_06a_policylaws_tax*`, `_06b_…welfare*`, `_06e_…trade*`, `_06c_…sectors*` (the draft explainers without *Master Sequence step 5d*) | Elias, verbatim in substance: *developer-facing text is leaking into player surfaces — "COMPLETED" in the laws tab, progress markers, anything addressed to the builder rather than the player.* What he saw was the citation class (103 of the 131 cut). Look for: any word on any sheet that speaks to the builder — the guard says there is none; the eye confirms or finds the guard's blind spot |
| **Playtest 1, finding 2 — the "as published" graph block** (Elias, 2026-08-29) | **P-A2, `COMPLETED.md` §62** (2026-08-29) | `pa_sweep_1280_02a_statistics_domestic*` and `pa_sweep_2560_02a_…` (the sheet ending on the Society rows; the main graphs' PRELIMINARY chips where they were) | Elias, verbatim in substance: *the "as published" graphs at the bottom of Statistics are redundant.* A DISPLAY cut — `PublicationSystem` untouched and proven so on the model's own source (`PerceivedPerformanceHarness` line 5). Look for: nothing missing that the main graphs did not already say |
| **W-E2 — the campaign map** (the fourth Track E screen, 2026-08-29) | `COMPLETED.md` §63 | `we2_campaign_<w>_e2_campaign_map_unbought` (29 hatched tiles figured "?", the ledger empty with the two offers' prices and the ± each buys), `_regional` (the breakdown: 82 per valkrets, shaded tiles with ±10, 19 dashed too-close-to-call frames, the ledger by index), `_full` (206 per valkrets, ±6, 13 dashed) at 1280 / 1600 / 1920 / 2560 | §36's gate as ABSENCE — is "?" over the hatch read as "unknown" rather than "zero"? Do the dashed frames read as "cannot say" rather than as decoration? Is the bold swing frame distinguishable from the hairline at 1280 (the film suggests barely — a Design line if not)? |
| **W-E1 — Campaign HQ** (the first Track E screen, 2026-08-29) | `COMPLETED.md` §57 | `pa_campaign_<w>_e1_campaign_hq_*` at 1280 / 1600 / 1920 / 2560 | The screen a player will live in. Does the ORGANISATION ledger (staff, offices, their daily cost) read as a going concern rather than a list? Is the money bar's spent share legible at a glance, or does it need a figure? Does the poll block make clear it is a POLL and not the truth? |
| **W-E3 — the action ladder** (the second Track E screen, 2026-08-29) | `COMPLETED.md` §55 | `pa_campaign_<w>_e3_campaign_action_*` at all four widths, plus the `poor` and `nomomentum` states | Fourteen actions on one sheet. Does the ladder read as a CHOICE (cost against reach) or as a menu? When the chest is nearly gone and most rows grey out, does the sheet say why, or does it just look broken? |
| **W-E4 — polling** (the third Track E screen, 2026-08-29) | `COMPLETED.md` §56 | `pa_campaign_<w>_e4_campaign_polling_*` at all four widths | §20-22's honesty is the whole subject: the MoE, the field date, the house effect. Does a player reading this sheet understand the poll can be WRONG, or does the number still read as the answer? |
| **W-E5 — the debate** (the fifth Track E screen, 2026-08-30) | `COMPLETED.md` §72 | `we5_debate_<w>_*` in three states — prep, midway, verdict — at 1280 / 1600 / 1920 / 2560 | Two questions, one of them a live Design ask (D8-6). (1) Is the exchange legible as an EXCHANGE, or does the ledger flatten it into rows? (2) Should this be a STAGE inside the sheet, as built, or a MODAL takeover like election night — the game treats the debate as a set piece and the two read very differently. The verdict box has no stamp (D8-5). |
| **W-E6 — election night** (board 1h, the sixth Track E screen, 2026-08-30) | `COMPLETED.md` §74 | ⚠ **EVIDENCE VOID, RE-FILMED 2026-08-31 (S-20).** ~~`wf1n_<w>_*` at all four widths~~ — **every election-night film ever taken photographed the DESK**, this row's included: the board is an overlay Canvas and IMGUI drew over it, at 0 failed and exit 0. **Use `cd5b_<w>_e6_election_night_*` (four widths, 2026-08-31), the first films in which board 1h is actually visible** — and they also carry C-D5's new swing column | The one board that is a full-bleed Canvas takeover. (1) Does an undeclared constituency read as UNDECLARED — it carries null votes by construction, never zero — or as an empty row? (2) The paper is FLAT here (deviation V-N1, ask D8-4): does the missing gradient and double shadow show against the rest of the game? (3) Calls arrive late by design; does the sheet make the guarantee visible or just feel slow? |
| **W-E7 — results and attribution** (the seventh Track E screen, 2026-08-30) | `COMPLETED.md` §73 | `wf1_<w>_*` (the W-F1 re-film) at all four widths | (1) The turnout is the PUBLISHED 84.21 %, not the 85.88 % the eight parties' own votes would imply — does the footer make clear which basis is on the sheet? (2) §30 asks for a demographic block and it is drawn ABSENT with the reason (blocked on W-F4, whose premise is falsified): does the absence read as deliberate? (3) The "why" column is Shapley over ten sources and sums to the deviation as an identity — is that legible as an explanation or as arithmetic? |
| **W-E8 — the coalition sheet** (the eighth and last Track E screen, 2026-08-30) | `COMPLETED.md` §75 | `wf1_<w>_*` (the W-F1 re-film) in three outcome states — confidence-and-supply (Sweden 2022 as it happened), new election, majority | THE distinction this sheet exists to make: a DECLARED red line is something a party said, with a citation; a DERIVED one is a distance this model measured and nobody uttered. Do the two read as different KINDS of thing, or does the derived column look like a claim about what a party would do? Also: the middle column shows the 120 arithmetic majorities a red line refused — does that land as "arithmetic is not the whole story", which is its point? |
| **Playtest 1, finding 5 — money in the wrong currency** (Elias, 2026-08-29) | the finding's record; **P-C1** answers it, **P-C2** rules the basis | every money surface on the current family (the Desk's masthead, Budget, Trade); the P-C1 film for Sweden and one euro country | Elias, verbatim in substance: *every domestic figure should render in its country's currency — kr, €, zł, $ — and the Desk shows Sweden's GDP as $620B.* Display first (P-C1); the seed basis surfaced and ruled, not smuggled (P-C2); both after W-G1 |
| **Playtest 1, finding 8 — the International tab is empty** (Elias, 2026-08-29) | the finding's record; **P-E1** answers it | `*_02b_statistics_international` on the current family (the map alone on the sheet); the P-E1 film at four sizes for at least three country pages | Elias, verbatim in substance: *the International tab is empty.* Country pages, each of the other five against the player's country, only what the model holds — no invented relations score; the empty-tab dead space measurably consumed; gaps become Design-ask lines |

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
   ("still not independent"). ⚠ **RE-CONFIRMED by Playtest-1's finding 7 and CLOSED as a waiting verdict 2026-08-31 (C-B4): it is no longer "the next play says whether it holds" — it is a BUILD ITEM.** Riksbank-B and P-D1 specify the same thing (declared reaction functions plus appointment influence), item 10's appointment gate has fired, and the two are ONE item at register row **C-C7**, which carries this verdict with it.
3. **The Trade bill's costs felt** (pass 6) — the bill card's cost line, the partner row's retaliation,
   the inflation year, and a hike that now fails at the seed composition.

**Load, play, judge (R-D4, 2026-08-28):** each verdict has a staged save in the game's own saves folder
(`…\saves\`, listed by the Saves menu) — `playtest_1_trade_bill_costs` (USA; open Policy/Laws › Trade:
a partner override drafted, the bill card's cost rows on screen — verdict 3), `playtest_2_riksbank_rate_decision`
(Sweden; a rate decision drafted on the Riksbank tab — option C's naming is the verdict; no appointment can
be pending on `main`; ⚠ since 2026-08-31 (C-B4) Riksbank-B's machinery is register row C-C7, not a gate — verdict 2),
`playtest_3_dense_midgame` (USA; the budget-process pause, pending cabinet decisions and a meeting, one
bill of every type, twelve laws in force — verdict 1). Each save's proof capture at 1600: `clear_p5_usa_p1_trade_bill_costs`, `clear_p5_sweden_p2_riksbank_rate_decision`, `clear_p5_usa_p3_dense_midgame`.

**Context for every verdict on this list (R-C7, 2026-08-28):** every felt verdict to date was played in the
no-expansionary-passage regime §D0 records — no tax-raising, spending-raising or welfare-implementing bill
passes except by ±1-seat jitter until item 10's real parties re-seed the house. The verdicts are read with
that context when item 10 changes it; none is re-opened by it.

## Playtest 1 — Elias's Sweden session (2026-08-29): the eleven findings, §P's first real output

Recorded before any item on his list runs (the list's own rule). Source: the Playtest-1 work list (retired at C-G1, 2026-08-31; every row points at its `COMPLETED.md` section)
at root — Elias's document, verbatim; each finding below is his words in substance, dated
2026-08-29, with the item that answers it and where that item sits in the sequencing (the
elections list keeps priority to its playable milestone; P-A/P-B ride session tails; P-C/P-D/P-E/P-G1–3
follow W-G1 because they move baselines; P-H/P-I are spec-lets first, the next era). Played in the
same no-expansionary-passage regime R-C7 names above.

| # | the finding, in Elias's words | answered by | where it sits |
|---|---|---|---|
| 1 | *Developer-facing text is leaking into player surfaces — "COMPLETED" in the laws tab, progress markers, anything addressed to the builder rather than the player.* | **P-A1 — DONE 2026-08-29** (`COMPLETED.md` §61): 131 strings cut at the source — what read as "COMPLETED" was the laws tab printing every citation's research-status prefix (*CONFIRMED - …*, 103 times), plus 17 section signs and two tags on the Track E screens and six *Master Sequence step 5d* explainers; `MetaTextCheck` armed as the ninth check, 0 hits after | filmed `pa_campaign_<w>_*` (four widths) and `pa_sweep_<w>_*` (1280, 2560) — the §V row waits on Elias's eyes |
| 2 | *The "as published" graph block at the bottom of Statistics is redundant.* | **P-A2 — DONE 2026-08-29** (`COMPLETED.md` §62): the block, its key and its three renderers cut; `PublicationSystem` untouched; `PerceivedPerformanceHarness` line 5 asserts on the model's own source that §19 reads Published and never State | filmed `pa_sweep_<w>_02a_statistics_domestic*` — the §V row waits on Elias's eyes |
| 3 | *Every tax/spending draft change should show its estimated annual fiscal impact before enactment — revenue delta, spending delta, net — as a range, never false precision.* | ✅ **P-B1 — DONE 2026-08-31 (C-C1, `COMPLETED.md` §94).** Revenue / spending / net for the year, from two clone runs of the model own boundary. ⚠ The range is a POINT because the projection is deterministic, with the scope stated instead — and the film caught the first cut printing a randomly rolled ± above a caption saying "no margin" | filmed `cc1b_<w>_*` at four widths, guards silent — the §V row waits on Elias eyes |
| 4 | *Entering office should open the budget process immediately for the first fiscal year — the player lays a budget on arrival instead of waiting for the calendar's next cycle.* | ✅ **P-B2 — DONE 2026-08-31 (C-C2, `COMPLETED.md` §95).** The arrival window opens on day one and once; the annual cycle governs after it. The identity HOLDS and was asserted where the change lives, a trajectory diff being vacuous here. ⚠ Also fixed an off-by-one that made five of six countries wait a full extra year | filmed `cc2_<w>_*` at four widths, open on 31 January — the §V row waits on Elias eyes |
| 5 | *Every domestic figure should render in its country's currency — kr (SEK), €, zł, $ — symbol, placement and formatting per locale.* | **P-C1** — `MoneyUnit` extended, `InvariantCulture` parsing preserved, a unit test per country's format | after W-G1 (also a §V row) |
| 6 | *The Desk shows Sweden's GDP as $620B — a USD basis.* (the basis question, surfaced rather than smuggled) | **P-C2** — determine what unit the seeds store; then **RULED: figures store and display in national units, cross-country views converting at a sourced, vintage-dated rate** (executes as written unless struck); a re-basing is a seed change under the full sim-math bar; if the model is unit-agnostic, say so and close cheap | after W-G1 — it may move baselines |
| 7 | *Rate control should leave the player's hands: the central bank decides rates by its own reaction function; the player keeps appointments and nothing else.* | **P-D1** — declared, tagged reaction functions for all six banks (sourced where a published rule exists, `[AUTHORED-DRAFT]` where not) on the model's own readings; the Fed-chair machinery generalised; pressure mechanics recorded as future, not built; Riksbank first, filmed; the baseline change captured as a new explained family | after W-G1 — it moves baselines. **This re-confirms verdict 2 above** ("still not independent", 2026-08-26): option C's naming did not hold; the felt verdict is now a build item |
| 8 | *The International tab is empty.* | **P-E1** — browsable country pages, each of the other five against the player's country: headline stats side by side, the pair's trade volume (the map's own data), compass positions compared, the relations facts the model actually holds — **only what the model holds**, no invented score; gaps visible, each a Design-ask line | after W-G1 (also a §V row) |
| 9 | *The economy feels disconnected — what did my policies actually do, and when?* (the deep finding) | **P-G1** the shadow baseline ("with your policies" against "without", the counterfactual drawn) · **P-G2** the impact ledger (the live-vs-shadow divergence attributed to enacted changes, the approval ledger's idiom) · **P-G3** the responsiveness audit — the model's implied multipliers measured and tabled against sourced literature, recalibration PROPOSED with its basis, never applied until ruled · **P-G4** enactment markers on the graphs (the release-tick idiom) | P-G4 after the elections screens' first milestone; P-G1/G2/G3 after W-G1 |
| 10 | *The tax system is too shallow — the real revenue instruments per country (Sweden: kommunalskatt, statlig inkomstskatt with its brytpunkt, capital at 30 %, arbetsgivaravgifter, moms tiers, corporate) should be the player's, brackets as data.* | **P-H1** — a spec-let and sourcing bill at root, sized in sessions, **ruled before any code** (revenue needs the income distribution — Track P-I's, or an interim sourced one) | the next era; the document can be written any time |
| 11 | *Demographics need age structure — cohorts driving labour participation, pensions and education, and the election system's voter groups as views over the same substrate: one demography, two consumers.* | **P-I1** the cohort spec-let (5-year cohorts recommended, sourced from Eurostat pyramids; the collision map with today's scalar demographics), ruled before built · **P-I2** the build per the ruled spec-let, with the election backtest re-run to prove nothing regressed | the next era |

**Not counted among the eleven — the Policy Web (P-F1/P-F2).** The list's Track P-F continues the
first live sitting's finding 3 (its §V row above: *bigger, more understandable, use the dead
space*): ✅ **P-F1 DONE 2026-08-31 (C-C3, `COMPLETED.md` §96)** — the structural half of comprehension inside R-W2's fence (focus mode, direction
arrowheads, weight-scaled thickness from the coupling table, DERIVED/DECLARED preserved, no legend,
no invented edge), P-F2 reports whether Design ever received the D7 ask (`85690abf`) — the paste
stays Elias's. *That reading — eleven findings plus one continuation — is the recorder's; strike it
if the Policy Web was meant as the twelfth.*

**The three verdicts above, after this playtest:** verdict 2 is re-confirmed by finding 7 and
becomes P-D1; verdicts 1 and 3 were not spoken to and stay open.

**Rulings Elias owes the list (from the list itself, one line each):** the P-C2 basis ruling
executes as written unless struck · P-G3's recalibrations apply only on his strike-or-bless of each
line · P-H1 and P-I1 spec-lets need his ruling before code · board 2b's paste (P-F2) remains his
gesture.

## ✅ W-C2's re-homed rider — OPENED AS ITS OWN ITEM **W-B12** by ruling 2026-08-30

**What it is.** W-B5 measured that **every party in the AI campaign goes broke before polling day** —
offices, their operations and the payroll are fixed daily costs the spending pace does not see (of
120 staff-days: SD 38 unpaid, V 12, S 10, M 6). W-B5's finding 2 named the fix and wrote it down
against W-C2: *the campaign manager's `BudgetPlan` should cover every fixed cost — pay the
organisation first, release the rest — which is what §9's "campaign manager" means in full.*

**Why it is here and not done.** W-C2's done-when is opponent reactivity, and that is what W-C2
shipped (`COMPLETED.md` §69). The plan still sets money aside for **television only**
(`ManagerFundShare` 0.5 of the day's release). Nothing about the reactivity work touched it, and
closing W-C2 on its own done-when would otherwise have left this rider pointing at a finished item —
so it is recorded here rather than implied.

**Where it went. RULED 2026-08-30 (Elias): its own item, `W-B12`, NOT a rider on W-F5** — because
it is a **playability requirement and must not inherit W-F5's data dependency**: it needs no sourced
funding figures, only a rule over costs the model already charges. Slotted after W-E8 and before
Track F in the elections work list (retired at C-G1, 2026-08-31; its 46 rows are closed and its record is `ELECTIONS_PROTOTYPE_LOG.md`), where its done-when lives. This section stays as the
finding's provenance.

**What it would change.** A managed party would stop starving its own staff; the pace would release
what is left after the organisation is paid; and C1's 2a-iii (professional / establishment 0.061
apart) may or may not separate as a result — that is the point of measuring it rather than assuming.

## 🟡 W-F6's finding — the Green Party has TWO leaders and the model has room for one

**What was found while sourcing the leaders (W-F1/W-F6, 2026-08-30).** Miljöpartiet is led by two
**språkrör** (spokespeople), by the party's own statutes one of each gender, and at the 2022 election
they were **Märta Stenevi and Per Bolund** — both named as *"Språkrör"* on the party's own site
(`mp.se`, Internet Archive capture 2022-09-11). This is not a quirk of one party: it is how MP has
been led since 1984.

**What the model assumes.** One leader per party, everywhere. `CandidateProfile` is a single person;
§15's debate seats one candidate per party; §29's leader compatibility (deferred, but specified)
compares one leader to another. **There is no representation of a shared leadership anywhere.**

**Why it is billed rather than fixed.** Taking "the first one" would silently drop Per Bolund, and
the resulting screen would state something false about a real party with a real name on it — which
is exactly what §0.4 exists to prevent. The fix is a **design question, not an implementation
detail**: does the player face one of the two, both together, or an aggregate — and if the debate
seats one, which one, and on what basis? That question belongs to §15 and §29, not to a data item
whose done-when is "source the names".

**What is true today.** `ElectionsData/sweden/party_leaders_2022.md` names **both**, with the
party's own citation for each, and says plainly that the model carries one. No screen currently
shows MP's leader, so nothing false is displayed — but the first screen that does will need this
answered.

**Where it goes.** A line in the W-H4 Design ask, and a question for §15/§29 when leaders become
player-facing beyond the debate staging.
